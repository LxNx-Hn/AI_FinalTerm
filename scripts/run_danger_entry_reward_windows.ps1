param(
    [string]$RunId  = "BossPPO_DangerEntryReward_v1",
    [switch]$Force,
    [switch]$Resume
)
$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"
$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_danger_entry_reward_v1.yaml"
$EnvExe        = Join-Path $ProjectRoot "unity_project\builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config";                  exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe";                     exit 1 }
$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[danger_reward] Port 5004 in use -- abort"; exit 1 }
Write-Host "[danger_reward] run-id=$RunId"
Write-Host "[danger_reward] max_steps=20000  Reward changes applied"
Write-Host "[danger_reward] WarningTile=-0.10  Missed=-0.08  MovedIntoWarn=-0.08  MovedIntoRecentWarn=-0.10  MovedIntoDmg=-0.30  SafeAttack=+0.02"
$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1", "--timeout-wait", "300")
if ($Force)  { $mlaArgs += "--force" }
if ($Resume) { $mlaArgs += "--resume" }
Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs
$exitCode = $LASTEXITCODE
Write-Host "[danger_reward] trainer exited: $exitCode"
exit $exitCode
