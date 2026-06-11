param(
    [Parameter(Mandatory = $true)][string]$ModelSource,
    [Parameter(Mandatory = $true)][string]$ModelLabel,
    [Parameter(Mandatory = $true)][string]$RunId,
    [Parameter(Mandatory = $true)][string]$CaptureName,
    [Parameter(Mandatory = $true)][int]$TargetEpisodes,
    [int]$RecordingFrameRate = 60,
    [int]$MaxWallSeconds = 1800
)

# Embedded-ONNX Eval210 capture at realtime. Used for early/mid checkpoints
# whose helpless/dodge-only behavior only shows on this inference path
# (trainer-inference makes even a fresh policy evade via mask+sampling).
$ErrorActionPreference = "Stop"
$ProjectRoot = "C:\Users\KiKi\Documents\Ql\AI_FinalTerm_MLAgents_PPO_NEW"
$Ffmpeg = Join-Path $ProjectRoot ".venv-mlagents\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe"
$BuildScript = Join-Path $ProjectRoot "scripts\build_eval_210_windows.ps1"
$GameExe = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain_Eval210\BossPPO_RLTrain_Eval210.exe"
$RunRoot = Join-Path $ProjectRoot "results\$RunId"
$RunLogDir = Join-Path $RunRoot "run_logs"
$PlayerLog = Join-Path $RunLogDir "Player-0.log"
$CaptureDir = Join-Path $PSScriptRoot "raw"
$RawVideo = Join-Path $CaptureDir "$CaptureName.mkv"
$MetaFile = Join-Path $CaptureDir "$CaptureName.json"

if (!(Test-Path $Ffmpeg)) { throw "ffmpeg missing: $Ffmpeg" }
if (!(Test-Path $BuildScript)) { throw "build script missing: $BuildScript" }
if (Test-Path $RunRoot) { throw "run already exists: $RunRoot" }
if (Test-Path $RawVideo) { throw "capture already exists: $RawVideo" }

$existing = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -in @("BossPPO_RLTrain.exe", "BossPPO_RLTrain_Eval210.exe", "mlagents-learn.exe")
}
if ($existing) { throw "a BossPPO/ML-Agents process is already running" }

New-Item -ItemType Directory -Force -Path $CaptureDir, $RunLogDir | Out-Null

Write-Host "[capture] building Eval210 with $ModelLabel"
& $BuildScript -CloseUnity -ModelSource $ModelSource -ModelLabel $ModelLabel
if ($LASTEXITCODE -ne 0) { throw "build failed: $ModelLabel" }

Add-Type -AssemblyName System.Windows.Forms
if (-not ("CaptureWindowApi" -as [type])) {
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class CaptureWindowApi {
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}
'@
}

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
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $Ffmpeg
$psi.Arguments = (($ffmpegArgs | ForEach-Object { '"' + ($_ -replace '"', '\"') + '"' }) -join ' ')
$psi.UseShellExecute = $false
$psi.RedirectStandardInput = $true
$psi.CreateNoWindow = $true
$ffmpegProc = New-Object System.Diagnostics.Process
$ffmpegProc.StartInfo = $psi
if (!$ffmpegProc.Start()) { throw "failed to start ffmpeg" }
$ffmpegStart = $ffmpegProc.StartTime.ToUniversalTime()

Start-Sleep -Seconds 2
$gameArgs = @(
    "-popupwindow", "-screen-width", "$width", "-screen-height", "$height",
    "-logFile", $PlayerLog
)
$gameProc = Start-Process -FilePath $GameExe -ArgumentList $gameArgs -PassThru
$gameStart = $gameProc.StartTime.ToUniversalTime()

$windowDeadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $windowDeadline -and $gameProc.MainWindowHandle -eq [IntPtr]::Zero) {
    Start-Sleep -Milliseconds 500
    $gameProc.Refresh()
}
if ($gameProc.MainWindowHandle -ne [IntPtr]::Zero) {
    $topmost = [IntPtr](-1)
    [CaptureWindowApi]::ShowWindow($gameProc.MainWindowHandle, 9) | Out-Null
    [CaptureWindowApi]::SetWindowPos(
        $gameProc.MainWindowHandle, $topmost,
        $bounds.X, $bounds.Y, $width, $height, 0x0040
    ) | Out-Null
    [CaptureWindowApi]::SetForegroundWindow($gameProc.MainWindowHandle) | Out-Null
}

Write-Host "[capture] ffmpeg pid=$($ffmpegProc.Id) game pid=$($gameProc.Id) target=$TargetEpisodes rec_fps=$RecordingFrameRate"
$deadline = (Get-Date).AddSeconds($MaxWallSeconds)
$episodeCount = 0

try {
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 3
        if ($gameProc.HasExited) { throw "game exited early: $($gameProc.ExitCode)" }
        if (Test-Path $PlayerLog) {
            $episodeCount = (Select-String -LiteralPath $PlayerLog -Pattern "\[BossRL\] EPISODE_END" -Encoding UTF8).Count
            Write-Host "[capture] episodes=$episodeCount"
            if ($episodeCount -ge $TargetEpisodes) { break }
        }
    }
    if ($episodeCount -lt $TargetEpisodes) { throw "capture timeout at $episodeCount/$TargetEpisodes episodes" }
    Start-Sleep -Seconds 3
}
finally {
    if (!$gameProc.HasExited) {
        [void]$gameProc.CloseMainWindow()
        if (!$gameProc.WaitForExit(10000)) { Stop-Process -Id $gameProc.Id -Force }
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
    model_source = $ModelSource
    model_label = $ModelLabel
    run_id = $RunId
    target_episodes = $TargetEpisodes
    captured_episodes = $episodeCount
    recording_frame_rate = $RecordingFrameRate
    screen_width = $width
    screen_height = $height
    ffmpeg_start_utc = $ffmpegStart.ToString("o")
    game_start_utc = $gameStart.ToString("o")
    game_offset_seconds = ($gameStart - $ffmpegStart).TotalSeconds
    player_log = $PlayerLog
    raw_video = $RawVideo
}
$meta | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $MetaFile -Encoding UTF8
Write-Host "[capture] complete: $RawVideo"
Write-Host "[capture] metadata: $MetaFile"
