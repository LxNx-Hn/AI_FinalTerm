param(
    [string]$RunId = "BossPPO_BuildSmoke",
    [switch]$Force
)

# EXE build smoke: mlagents-learn --env 로 Windows EXE와 연결한다.
# Unity Editor를 제어하지 않는다.
# CODE-BLUE 원본을 건드리지 않는다.
# 1~3 episode 또는 최대 5분 smoke 목적이다.
# 장시간 학습이 아니다.

$ErrorActionPreference = "Stop"

$env:PYTHONUTF8 = "1"

$ProjectRoot    = Split-Path -Parent $PSScriptRoot
$MlagentsLearn  = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config         = Join-Path $ProjectRoot "ml-agents-config\boss_player_ppo_smoke.yaml"
$EnvExe         = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"

# 사전 확인
if (!(Test-Path $MlagentsLearn)) {
    Write-Error "[run_build_smoke] mlagents-learn.exe not found: $MlagentsLearn"
    exit 1
}

if (!(Test-Path $Config)) {
    Write-Error "[run_build_smoke] Config not found: $Config"
    exit 1
}

if (!(Test-Path $EnvExe)) {
    Write-Error "[run_build_smoke] EXE not found: $EnvExe"
    Write-Host "[run_build_smoke] Run scripts\build_rl_windows.ps1 first."
    exit 1
}

# 포트 5004 점유 확인
$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) {
    Write-Host "[run_build_smoke] WARNING: Port 5004 may be in use:"
    $portCheck | ForEach-Object { Write-Host "  $_" }
    Write-Host "[run_build_smoke] Kill the process or wait, then retry."
    exit 1
}

Write-Host "[run_build_smoke] trainer:  $MlagentsLearn"
Write-Host "[run_build_smoke] config:   $Config"
Write-Host "[run_build_smoke] env EXE:  $EnvExe"
Write-Host "[run_build_smoke] run-id:   $RunId"
Write-Host "[run_build_smoke] force:    $Force"
Write-Host ""
Write-Host "[run_build_smoke] Starting EXE smoke. 1~3 episodes or max 5 minutes."
Write-Host "[run_build_smoke] Press Ctrl+C to stop early."
Write-Host ""

$trainerArgs = @(
    $Config,
    "--run-id", $RunId,
    "--env", $EnvExe,
    "--num-envs", "1"
)

if ($Force) {
    $trainerArgs += "--force"
}

Set-Location $ProjectRoot

& $MlagentsLearn @trainerArgs

$exitCode = $LASTEXITCODE
Write-Host ""
Write-Host "[run_build_smoke] trainer exited with code $exitCode"
exit $exitCode
