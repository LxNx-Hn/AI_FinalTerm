param(
    [string]$RunId  = "BossPPO_WrapperBugfix_DangerTTL_v1",
    [switch]$Force,
    [switch]$Resume
)
$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_player_ppo_wrapper_bugfix.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config";                  exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe";                     exit 1 }
$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[wrapper_bugfix] Port 5004 in use — abort"; exit 1 }
Write-Host "[wrapper_bugfix] run-id=$RunId"
Write-Host "[wrapper_bugfix] max_steps=10000  network=512x3  normalize=true"
Write-Host "[wrapper_bugfix] Fixes: (1) external_input one-shot  (2) approach_reward=0  (3) recent_danger TTL-based"
Write-Host "[wrapper_bugfix] Reward: boss_dmg=0.10 kill=5.0 miss=-0.05 cooldown=-0.02 hit=-2.0 death=-8.0 warn=-0.05 dmg=-0.20 wall=-0.05 step=-0.001 approach=0"
$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force)  { $mlaArgs += "--force" }
if ($Resume) { $mlaArgs += "--resume" }
Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs
$exitCode = $LASTEXITCODE
Write-Host "[wrapper_bugfix] trainer exited: $exitCode"
exit $exitCode
