param(
    [Parameter(Mandatory = $true)][string]$RunId,
    [Parameter(Mandatory = $true)][string]$InitializeFrom,
    [int]$TargetEpisodes = 12,
    [int]$MaxWallSeconds = 1800
)

# Headless trainer-inference probe: no video, no window. Used to check a
# checkpoint's behavior distribution before committing to a screen capture.
$ErrorActionPreference = "Stop"
$ProjectRoot = "C:\Users\KiKi\Documents\Ql\AI_FinalTerm_MLAgents_PPO_NEW"
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_target_gate_markatk_sweep_1m_c84b1a4.yaml"
$GameExe = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
$RunRoot = Join-Path $ProjectRoot "results\$RunId"
$PlayerLog = Join-Path $RunRoot "run_logs\Player-0.log"
$TrainerOut = Join-Path $PSScriptRoot "$RunId.trainer.log"
$TrainerErr = Join-Path $PSScriptRoot "$RunId.trainer.err.log"

foreach ($required in @($MlagentsLearn, $Config, $GameExe)) {
    if (!(Test-Path $required)) { throw "missing required file: $required" }
}
if (Test-Path $RunRoot) { throw "run already exists: $RunRoot" }

$existing = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -eq "BossPPO_RLTrain.exe" -or $_.Name -eq "mlagents-learn.exe"
}
if ($existing) { throw "an ML-Agents or BossPPO process is already running" }

$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"
$trainerArgs = @(
    $Config,
    "--run-id", $RunId,
    "--initialize-from", $InitializeFrom,
    "--inference",
    "--env", $GameExe,
    "--num-envs", "1",
    "--no-graphics",
    "--timeout-wait", "60"
)
$trainerProc = Start-Process -FilePath $MlagentsLearn -ArgumentList $trainerArgs `
    -WorkingDirectory $ProjectRoot -RedirectStandardOutput $TrainerOut `
    -RedirectStandardError $TrainerErr -PassThru -WindowStyle Hidden

$deadline = (Get-Date).AddSeconds($MaxWallSeconds)
$episodeCount = 0
try {
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 5
        $trainerProc.Refresh()
        if ($trainerProc.HasExited) { throw "trainer exited early: $($trainerProc.ExitCode)" }
        if (Test-Path $PlayerLog) {
            $episodeCount = (Select-String -LiteralPath $PlayerLog -Pattern "\[BossRL\] EPISODE_END" -Encoding UTF8).Count
            Write-Host "[probe] episodes=$episodeCount"
            if ($episodeCount -ge $TargetEpisodes) { break }
        }
    }
    if ($episodeCount -lt $TargetEpisodes) { throw "probe timeout at $episodeCount/$TargetEpisodes episodes" }
}
finally {
    if ($trainerProc -and !$trainerProc.HasExited) {
        & taskkill.exe /PID $trainerProc.Id /T /F | Out-Host
    }
}
Write-Host "[probe] complete: $episodeCount episodes -> $PlayerLog"
