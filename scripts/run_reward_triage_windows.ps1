param(
    [string]$RunId = "BossPPO_RewardTriage",
    [switch]$Force
)

# Reward triage run: EXE + mlagents-learn, max 20k steps.
# 목적: reward 수정 후 공격 편향 개선 여부 확인.
# Unity Editor를 제어하지 않는다.
# CODE-BLUE 원본을 건드리지 않는다.

$ErrorActionPreference = "Stop"

# Fix: Windows cp949 codepage can't encode emoji in torch.onnx logs → ONNX export crash
$env:PYTHONUTF8 = "1"

$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_player_ppo_reward_triage.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"

if (!(Test-Path $MlagentsLearn)) { Write-Error "[triage] mlagents-learn.exe not found: $MlagentsLearn"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "[triage] config not found: $Config"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "[triage] EXE not found: $EnvExe`n  Run scripts\build_rl_windows.ps1 first."; exit 1 }

$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) {
    Write-Host "[triage] WARNING: Port 5004 may be in use. Kill the process and retry."
    exit 1
}

Write-Host "[triage] trainer:  $MlagentsLearn"
Write-Host "[triage] config:   $Config"
Write-Host "[triage] env EXE:  $EnvExe"
Write-Host "[triage] run-id:   $RunId"
Write-Host "[triage] force:    $Force"
Write-Host ""
Write-Host "[triage] Reward changes vs prev smoke:"
Write-Host "  warning_tile: -0.005 → -0.03"
Write-Host "  damage_tile:  -0.05  → -0.20"
Write-Host "  player_hit:   -1.0x  → -2.0x per HP delta"
Write-Host "  player_death: -5.0   → -8.0"
Write-Host "  max_steps:    50000  → 20000"
Write-Host ""
Write-Host "[triage] Press Ctrl+C to stop early."
Write-Host ""

$args = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force) { $args += "--force" }

Set-Location $ProjectRoot
& $MlagentsLearn @args

$exitCode = $LASTEXITCODE
Write-Host ""
Write-Host "[triage] trainer exited with code $exitCode"
exit $exitCode
