param(
    [string]$RunId  = "BossPPO_ActionMaskDangerTriage_v1",
    [switch]$Force,
    [switch]$Resume
)

$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"

$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_player_ppo_action_mask_triage.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"

if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found at: $EnvExe"; exit 1 }

$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[action_mask_triage] Port 5004 in use — kill and retry"; exit 1 }

Write-Host "[action_mask_triage] run-id=$RunId  force=$Force  resume=$Resume"
Write-Host "[action_mask_triage] max_steps=20000  EXE=$EnvExe"

$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force)  { $mlaArgs += "--force" }
if ($Resume) { $mlaArgs += "--resume" }

Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs

$exitCode = $LASTEXITCODE
Write-Host "[action_mask_triage] trainer exited: $exitCode"
exit $exitCode
