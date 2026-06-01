param(
    [Parameter(Mandatory = $true)][string]$RunId,
    [ValidateSet("smoke", "fresh50k")][string]$Mode = "fresh50k",
    [switch]$Force
)

# Headless launcher for the Target Gate + MarkATK + Sweep History integration.
#
# WHY --no-graphics:
#   The earlier Fresh50K_v1 run timed out ("Workers {0} stuck in waiting state")
#   at ~2683 steps with NO Unity crash/exception. Root cause (see
#   docs/rl_eval_runs/20260601_EVAL210_HANG_DIAG.md): ProjectSettings has
#   runInBackground=0, so a windowed standalone player STOPS advancing its
#   update loop when the window loses focus -> DecisionRequester stops ->
#   OnActionReceived never fires -> trainer worker times out.
#   Running the env headless (--no-graphics, no window) removes the focus
#   dependency entirely. The agent uses VECTOR observations only and the
#   boss-visibility checks read SpriteRenderer state (enabled / activeInHierarchy
#   / alpha), NOT rendered pixels, so no-graphics does not change behavior.
#
# This script does NOT change code/reward/observation/action-spec/gameplay.
# Fresh PPO only: no --resume, no --initialize-from.

$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"

$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"

if ($Mode -eq "smoke") {
    $Config = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_target_gate_markatk_sweep_5k_smoke_v1.yaml"
} else {
    $Config = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_target_gate_markatk_sweep_fresh50k_v1.yaml"
}

if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe"; exit 1 }

$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[target_gate] Port 5004 in use; stop trainer first."; exit 1 }

$runRoot = Join-Path $ProjectRoot "results\$RunId"
if ((Test-Path $runRoot) -and !$Force) {
    Write-Error "Run already exists: $runRoot (use -Force to overwrite, or pick a new run-id)"
    exit 1
}

Write-Host "[target_gate] run-id=$RunId mode=$Mode"
Write-Host "[target_gate] config=$Config"
Write-Host "[target_gate] EXE=$EnvExe"
Write-Host "[target_gate] HEADLESS --no-graphics (fixes runInBackground=0 focus-loss hang)"
Write-Host "[target_gate] fresh PPO: no --resume, no --initialize-from"

$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1", "--no-graphics")
if ($Force) { $mlaArgs += "--force" }

Set-Location $ProjectRoot
# Native stderr from mlagents (its normal logging) must not abort the script.
$ErrorActionPreference = "Continue"
& $MlagentsLearn @mlaArgs

$exitCode = $LASTEXITCODE
Write-Host "[target_gate] trainer exited: $exitCode"
exit $exitCode
