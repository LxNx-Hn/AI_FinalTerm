param(
    [string]$RunId  = "BossPPO_ResetTimeout_Diag_v1",
    [switch]$Force,
    [switch]$Resume
)
$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_reset_timeout_diag_v1.yaml"
$EnvExe        = Join-Path $ProjectRoot "unity_project\builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config";                  exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe";                     exit 1 }
$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[reset_diag] Port 5004 in use -- abort"; exit 1 }
Write-Host "[reset_diag] run-id=$RunId"
Write-Host "[reset_diag] max_steps=5000  timeout-wait=300  DIAGNOSTIC: scene reload stability"
Write-Host "[reset_diag] Fix: LoadSceneAsync (non-blocking) + timing logs in BossRLEpisodeResetter"
Write-Host "[reset_diag] Reward unchanged: boss_dmg=0.10 kill=5.0 miss=-0.05 cooldown=-0.02 hit=-2.0 death=-8.0 warn=-0.05 dmg=-0.20 wall=-0.05 step=-0.001 approach=0"
$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1", "--timeout-wait", "300")
if ($Force)  { $mlaArgs += "--force" }
if ($Resume) { $mlaArgs += "--resume" }
Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs
$exitCode = $LASTEXITCODE
Write-Host "[reset_diag] trainer exited: $exitCode"
exit $exitCode
