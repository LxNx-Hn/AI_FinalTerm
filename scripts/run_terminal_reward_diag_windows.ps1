param(
    [string]$RunId = "BossPPO_TerminalReward_Diag_v1",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"

$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_terminal_reward_diag_v1.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"

if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe"; exit 1 }

$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[terminal_reward_diag] Port 5004 in use; stop trainer first."; exit 1 }

$runRoot = Join-Path $ProjectRoot "results\$RunId"
if ((Test-Path $runRoot) -and !$Force) {
    Write-Error "Run already exists: $runRoot"
    exit 1
}

Write-Host "[terminal_reward_diag] run-id=$RunId force=$Force"
Write-Host "[terminal_reward_diag] max_steps=5000 timeout=210s EXE=$EnvExe"
Write-Host "[terminal_reward_diag] fresh diagnostic only: no --resume and no --initialize-from"

$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force) { $mlaArgs += "--force" }

Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs

$exitCode = $LASTEXITCODE
Write-Host "[terminal_reward_diag] trainer exited: $exitCode"
exit $exitCode
