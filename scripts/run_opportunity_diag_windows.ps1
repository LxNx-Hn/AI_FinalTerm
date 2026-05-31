param(
    [string]$RunId  = "BossPPO_OpportunityMetricDiag_v1",
    [switch]$Force,
    [switch]$Resume
)
$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"
$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_opportunity_diag_v1.yaml"
$EnvExe        = Join-Path $ProjectRoot "unity_project\builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config";                  exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe";                     exit 1 }
$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[opp_diag] Port 5004 in use -- abort"; exit 1 }
Write-Host "[opp_diag] run-id=$RunId"
Write-Host "[opp_diag] max_steps=10000  DIAGNOSTIC: opportunity/danger metrics"
Write-Host "[opp_diag] No reward/mask changes. New metrics: safe_attack_opp, danger_nearby, avoidable_hit, etc."
$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1", "--timeout-wait", "300")
if ($Force)  { $mlaArgs += "--force" }
if ($Resume) { $mlaArgs += "--resume" }
Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs
$exitCode = $LASTEXITCODE
Write-Host "[opp_diag] trainer exited: $exitCode"
exit $exitCode
