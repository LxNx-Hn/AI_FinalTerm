param(
    [string]$RunId  = "BossPPO_SurvivalStage1_v1",
    [switch]$Force,
    [switch]$Resume
)
$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_player_survival_stage1.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe"; exit 1 }
$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[stage1] Port 5004 in use"; exit 1 }
Write-Host "[stage1] run-id=$RunId  max_steps=15000  survival_stage=1.0 (ATTACK masked)"
$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force)  { $mlaArgs += "--force" }
if ($Resume) { $mlaArgs += "--resume" }
Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs
$exitCode = $LASTEXITCODE
Write-Host "[stage1] trainer exited: $exitCode"
exit $exitCode
