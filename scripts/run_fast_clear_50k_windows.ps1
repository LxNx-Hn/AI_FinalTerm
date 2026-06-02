param(
    [string]$RunId = "BossPPO_FastClear_50k",
    [switch]$Force,
    [int]$BasePort = 5015,
    [double]$TimeScale = 40
)

$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"

$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_safe_opp_reward_50k_v1.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"

if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe"; exit 1 }

$runRoot = Join-Path $ProjectRoot "results\$RunId"
if ((Test-Path $runRoot) -and !$Force) {
    Write-Error "Run already exists: $runRoot"
    exit 1
}

Write-Host "[fast_clear_50k] run-id=$RunId force=$Force"
Write-Host "[fast_clear_50k] max_steps=50000 base_port=$BasePort time_scale=$TimeScale no_graphics=true"
Write-Host "[fast_clear_50k] objective: dodge enough to avoid 3HP death, attack safely, clear boss quickly"

$mlaArgs = @(
    $Config,
    "--run-id", $RunId,
    "--env", $EnvExe,
    "--num-envs", "1",
    "--base-port", "$BasePort",
    "--timeout-wait", "300",
    "--time-scale", "$TimeScale",
    "--target-frame-rate", "-1",
    "--quality-level", "0",
    "--width", "84",
    "--height", "84",
    "--no-graphics",
    "--env-args", "--rl-mute-audio"
)
if ($Force) { $mlaArgs += "--force" }

Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs

$exitCode = $LASTEXITCODE
Write-Host "[fast_clear_50k] trainer exited: $exitCode"
exit $exitCode
