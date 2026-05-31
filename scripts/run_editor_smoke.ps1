param(
    [string]$RunId  = "BossPPO_EditorSmoke",
    [switch]$Force
)

# Unity를 실행하지 않는다.
# Play Mode에 들어가지 않는다.
# Scene을 저장하지 않는다.
# trainer만 실행한다.
#
# 실행 전 compile-stable gate를 반드시 통과할 것:
#   docs/EDITOR_SMOKE_STABLE_GATE.md

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_player_ppo_smoke.yaml"

if (!(Test-Path $MlagentsLearn)) {
    throw "mlagents-learn.exe not found: $MlagentsLearn"
}

if (!(Test-Path $Config)) {
    throw "Config not found: $Config"
}

$trainerArgs = @($Config, "--run-id", $RunId)

if ($Force) {
    $trainerArgs += "--force"
}

Write-Host "[run_editor_smoke] trainer: $MlagentsLearn"
Write-Host "[run_editor_smoke] config:  $Config"
Write-Host "[run_editor_smoke] run-id:  $RunId"
Write-Host "[run_editor_smoke] force:   $Force"
Write-Host ""
Write-Host "[run_editor_smoke] Waiting for Unity Editor Play Mode connection..."
Write-Host "[run_editor_smoke] Start trainer first, then press Play in Unity Editor."
Write-Host ""

& $MlagentsLearn @trainerArgs

$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    Write-Host "[run_editor_smoke] trainer exited with code $exitCode"
}
exit $exitCode
