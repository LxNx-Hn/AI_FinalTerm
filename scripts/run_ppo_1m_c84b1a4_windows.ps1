param(
    [string]$RunId = "BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1",
    [switch]$Force
)

# Headless 1,000,000-step PPO long-run reproduction from stable commit c84b1a4.
#
# WHY: a friend reportedly cleared the boss at ~1M steps on this same code. This run
# tests whether PPO-only escapes the 50K local optimum given enough timesteps.
# This is NOT a reward experiment — game/reward/observation/action are identical to
# c84b1a4 (VectorObservationSize 438, Action Spec [5,2], hit-gated reward, Target Gate,
# MarkATK obs, Phase2 sweep history; NO stationary lever, NO warn-tile relax, NO demo/BC).
#
# Headless --no-graphics: ProjectSettings runInBackground=0 makes a windowed player stall
# on focus loss -> trainer "Workers stuck in waiting state" timeout. -nographics removes the
# window/focus dependency entirely. Vector observations only, so -nographics is behavior-safe.
# Fresh PPO only: no --resume, no --initialize-from.

$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"

$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_target_gate_markatk_sweep_1m_c84b1a4.yaml"

if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found (build c84b1a4 first): $EnvExe"; exit 1 }

$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[ppo_1m] Port 5004 in use; stop the running trainer first."; exit 1 }

$runRoot = Join-Path $ProjectRoot "results\$RunId"
if ((Test-Path $runRoot) -and !$Force) {
    Write-Error "Run already exists: $runRoot (use -Force to overwrite, or pick a new run-id)"
    exit 1
}

Write-Host "[ppo_1m] run-id=$RunId  max_steps=1,000,000  HEADLESS --no-graphics  fresh PPO (no resume)"
Write-Host "[ppo_1m] config=$Config"
Write-Host "[ppo_1m] EXE=$EnvExe"

$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1", "--no-graphics")
if ($Force) { $mlaArgs += "--force" }

Set-Location $ProjectRoot
# Native stderr from mlagents (normal logging) must not abort the script.
$ErrorActionPreference = "Continue"
& $MlagentsLearn @mlaArgs

$exitCode = $LASTEXITCODE
Write-Host "[ppo_1m] trainer exited: $exitCode"
exit $exitCode
