param(
    [string]$RunId  = "BossPPO_MaskRelaxTriage_v1",
    [switch]$Force,
    [switch]$Resume
)
$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_player_mask_relax_triage.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe"; exit 1 }
$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[mask_relax] Port 5004 in use"; exit 1 }
Write-Host "[mask_relax] run-id=$RunId  max_steps=15000  survival_stage=0.0 (ATTACK allowed)"
Write-Host "[mask_relax] Movement mask: wall+active_damage only (warning/recent REMOVED)"
Write-Host "[mask_relax] Attack mask: !AttackReady only"
$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force)  { $mlaArgs += "--force" }
if ($Resume) { $mlaArgs += "--resume" }
Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs
$exitCode = $LASTEXITCODE
Write-Host "[mask_relax] trainer exited: $exitCode"
exit $exitCode
