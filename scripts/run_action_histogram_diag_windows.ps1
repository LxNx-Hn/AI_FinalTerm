param(
    [string]$RunId = "BossPPO_ActionHistogram_Diag_v1",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"

$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_action_histogram_diag_v1.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"

if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe"; exit 1 }

$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[action_histogram_diag] Port 5004 in use; stop trainer first."; exit 1 }

$runRoot = Join-Path $ProjectRoot "results\$RunId"
if ((Test-Path $runRoot) -and !$Force) {
    Write-Error "Run already exists: $runRoot"
    exit 1
}

Write-Host "[action_histogram_diag] run-id=$RunId force=$Force"
Write-Host "[action_histogram_diag] max_steps=10000 timeout=210s EXE=$EnvExe"
Write-Host "[action_histogram_diag] fresh diagnostic only: no --resume and no --initialize-from"
Write-Host "[action_histogram_diag] reward values unchanged; action/mask/observation unchanged"

$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force) { $mlaArgs += "--force" }

Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs

$exitCode = $LASTEXITCODE
Write-Host "[action_histogram_diag] trainer exited: $exitCode"
exit $exitCode
