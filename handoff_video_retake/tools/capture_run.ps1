param(
    [Parameter(Mandatory = $true)][string]$RunId,
    [Parameter(Mandatory = $true)][string]$CaptureName,
    [Parameter(Mandatory = $true)][string]$InitializeFrom,
    [int]$TargetEpisodes = 12,
    [int]$TimeScale = 1,
    [int]$TargetFrameRate = 60,
    [int]$CaptureFrameRate = 0,
    [int]$RecordingFrameRate = 60,
    [int]$MaxWallSeconds = 3600
)

# Screen capture of a trainer-inference run. Selection MUST later be made from
# THIS run's Player log (results/<RunId>/run_logs/Player-0.log), never from a
# different run -- episode indices are not comparable across runs.
$ErrorActionPreference = "Stop"
$ProjectRoot = "C:\Users\KiKi\Documents\Ql\AI_FinalTerm_MLAgents_PPO_NEW"
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Ffmpeg = Join-Path $ProjectRoot ".venv-mlagents\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe"
$Config = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_target_gate_markatk_sweep_1m_c84b1a4.yaml"
$GameExe = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
$RunRoot = Join-Path $ProjectRoot "results\$RunId"
$PlayerLog = Join-Path $RunRoot "run_logs\Player-0.log"
$CaptureDir = Join-Path $PSScriptRoot "raw"
$RawVideo = Join-Path $CaptureDir "$CaptureName.mkv"
$MetaFile = Join-Path $CaptureDir "$CaptureName.json"
$TrainerOut = Join-Path $CaptureDir "$CaptureName.trainer.log"
$TrainerErr = Join-Path $CaptureDir "$CaptureName.trainer.err.log"

foreach ($required in @($MlagentsLearn, $Ffmpeg, $Config, $GameExe)) {
    if (!(Test-Path $required)) { throw "missing required file: $required" }
}
if (Test-Path $RunRoot) { throw "run already exists: $RunRoot" }
if (Test-Path $RawVideo) { throw "capture already exists: $RawVideo" }

$existing = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -eq "BossPPO_RLTrain.exe" -or $_.Name -eq "mlagents-learn.exe"
}
if ($existing) {
    $existing | Select-Object ProcessId, ParentProcessId, Name, CommandLine | Format-List | Out-Host
    throw "an ML-Agents or BossPPO process is already running"
}

New-Item -ItemType Directory -Force -Path $CaptureDir | Out-Null
Remove-Item -LiteralPath $TrainerOut, $TrainerErr -Force -ErrorAction SilentlyContinue

Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class CaptureWindowApi {
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}
'@

$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$width = $bounds.Width
$height = $bounds.Height

$ffmpegArgs = @(
    "-y", "-hide_banner", "-loglevel", "warning",
    "-f", "gdigrab", "-draw_mouse", "0", "-framerate", "$RecordingFrameRate",
    "-offset_x", "$($bounds.X)", "-offset_y", "$($bounds.Y)",
    "-video_size", "${width}x${height}", "-i", "desktop",
    "-vf", "scale=in_range=full:out_range=tv,format=yuv420p",
    "-c:v", "libx264", "-preset", "veryfast", "-crf", "18",
    "-color_range", "tv", "-colorspace", "bt709",
    "-color_primaries", "bt709", "-color_trc", "bt709",
    $RawVideo
)
$ffmpegInfo = New-Object System.Diagnostics.ProcessStartInfo
$ffmpegInfo.FileName = $Ffmpeg
$ffmpegInfo.Arguments = (($ffmpegArgs | ForEach-Object { '"' + ($_ -replace '"', '\"') + '"' }) -join " ")
$ffmpegInfo.UseShellExecute = $false
$ffmpegInfo.RedirectStandardInput = $true
$ffmpegInfo.CreateNoWindow = $true
$ffmpegProc = New-Object System.Diagnostics.Process
$ffmpegProc.StartInfo = $ffmpegInfo
if (!$ffmpegProc.Start()) { throw "failed to start ffmpeg" }
$ffmpegStart = $ffmpegProc.StartTime.ToUniversalTime()

$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"
$trainerArgs = @(
    $Config,
    "--run-id", $RunId,
    "--initialize-from", $InitializeFrom,
    "--inference",
    "--env", $GameExe,
    "--num-envs", "1",
    "--time-scale", "$TimeScale",
    "--width", "$width",
    "--height", "$height",
    "--quality-level", "5",
    "--target-frame-rate", "$TargetFrameRate",
    "--capture-frame-rate", "$CaptureFrameRate",
    "--timeout-wait", "60",
    "--env-args", "-popupwindow", "-screen-fullscreen", "0"
)
$trainerProc = Start-Process -FilePath $MlagentsLearn -ArgumentList $trainerArgs `
    -WorkingDirectory $ProjectRoot -RedirectStandardOutput $TrainerOut `
    -RedirectStandardError $TrainerErr -PassThru -WindowStyle Hidden

$gameProc = $null
$episodeCount = 0
$gameStart = $null
$deadline = (Get-Date).AddSeconds($MaxWallSeconds)

try {
    $launchDeadline = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $launchDeadline -and !$trainerProc.HasExited) {
        Start-Sleep -Seconds 1
        $candidate = Get-CimInstance Win32_Process | Where-Object {
            $_.Name -eq "BossPPO_RLTrain.exe" -and $_.CommandLine -like "*$RunId*"
        } | Select-Object -First 1
        if ($candidate) {
            $gameProc = Get-Process -Id $candidate.ProcessId
            break
        }
    }
    if (!$gameProc) { throw "game process did not start" }
    $gameStart = $gameProc.StartTime.ToUniversalTime()

    $windowDeadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $windowDeadline -and $gameProc.MainWindowHandle -eq [IntPtr]::Zero) {
        Start-Sleep -Milliseconds 500
        $gameProc.Refresh()
    }
    if ($gameProc.MainWindowHandle -eq [IntPtr]::Zero) { throw "game window did not appear" }

    $topmost = [IntPtr](-1)
    [CaptureWindowApi]::ShowWindow($gameProc.MainWindowHandle, 9) | Out-Null
    [CaptureWindowApi]::SetWindowPos(
        $gameProc.MainWindowHandle, $topmost,
        $bounds.X, $bounds.Y, $width, $height, 0x0040
    ) | Out-Null
    [CaptureWindowApi]::SetForegroundWindow($gameProc.MainWindowHandle) | Out-Null

    Write-Host "[capture] trainer pid=$($trainerProc.Id) game pid=$($gameProc.Id) ffmpeg pid=$($ffmpegProc.Id)"
    Write-Host "[capture] target=$TargetEpisodes time_scale=$TimeScale screen=${width}x${height} rec_fps=$RecordingFrameRate"

    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 5
        $trainerProc.Refresh()
        $gameProc.Refresh()
        if ($trainerProc.HasExited) { throw "trainer exited early: $($trainerProc.ExitCode)" }
        if ($gameProc.HasExited) { throw "game exited early: $($gameProc.ExitCode)" }
        if (Test-Path $PlayerLog) {
            $episodeCount = (Select-String -LiteralPath $PlayerLog -Pattern "\[BossRL\] EPISODE_END" -Encoding UTF8).Count
            Write-Host "[capture] episodes=$episodeCount"
            if ($episodeCount -ge $TargetEpisodes) { break }
        }
    }
    if ($episodeCount -lt $TargetEpisodes) {
        throw "capture timeout at $episodeCount/$TargetEpisodes episodes"
    }
    # let the last episode's reload settle on screen before stopping
    Start-Sleep -Seconds 3
}
finally {
    if ($trainerProc -and !$trainerProc.HasExited) {
        & taskkill.exe /PID $trainerProc.Id /T /F | Out-Host
    }
    Start-Sleep -Seconds 2
    if (!$ffmpegProc.HasExited) {
        $ffmpegProc.StandardInput.WriteLine("q")
        $ffmpegProc.StandardInput.Flush()
        if (!$ffmpegProc.WaitForExit(30000)) { Stop-Process -Id $ffmpegProc.Id -Force }
    }
}

$meta = [ordered]@{
    capture_name = $CaptureName
    initialize_from = $InitializeFrom
    run_id = $RunId
    target_episodes = $TargetEpisodes
    captured_episodes = $episodeCount
    time_scale = $TimeScale
    target_frame_rate = $TargetFrameRate
    capture_frame_rate = $CaptureFrameRate
    recording_frame_rate = $RecordingFrameRate
    screen_width = $width
    screen_height = $height
    ffmpeg_start_utc = $ffmpegStart.ToString("o")
    game_start_utc = $gameStart.ToString("o")
    game_offset_seconds = ($gameStart - $ffmpegStart).TotalSeconds
    player_log = $PlayerLog
    trainer_log = $TrainerOut
    trainer_error_log = $TrainerErr
    raw_video = $RawVideo
}
$meta | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $MetaFile -Encoding UTF8
Write-Host "[capture] complete: $RawVideo"
Write-Host "[capture] metadata: $MetaFile"
