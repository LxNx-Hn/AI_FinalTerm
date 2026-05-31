param(
    [string]$RunId  = "BossPPO_MaskGeometry_CooldownReplayFix_v1",
    [switch]$Force,
    [switch]$Resume
)
$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_mask_geometry_cooldown_v1.yaml"
$EnvExe        = Join-Path $ProjectRoot "unity_project\builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config";                  exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe";                     exit 1 }
$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[mask_geometry] Port 5004 in use — abort"; exit 1 }
Write-Host "[mask_geometry] run-id=$RunId"
Write-Host "[mask_geometry] max_steps=5000  network=512x3  normalize=true  DIAGNOSTIC ONLY"
Write-Host "[mask_geometry] Fixes: (A) geometry-only movement mask  (B) boss cell passable  (C) TakeActionsBetweenDecisions=false"
Write-Host "[mask_geometry] Reward: boss_dmg=0.10 kill=5.0 miss=-0.05 cooldown=-0.02 hit=-2.0 death=-8.0 warn=-0.05 dmg=-0.20 wall=-0.05 step=-0.001 approach=0"
$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force)  { $mlaArgs += "--force" }
if ($Resume) { $mlaArgs += "--resume" }
Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs
$exitCode = $LASTEXITCODE
Write-Host "[mask_geometry] trainer exited: $exitCode"
exit $exitCode
