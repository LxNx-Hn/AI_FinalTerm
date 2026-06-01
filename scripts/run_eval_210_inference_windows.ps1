param(
    [string]$RunId = "BossPPO_SafeActionMask_100k_Eval210_v1",
    [int]$TargetEpisodes = 10,
    [int]$MaxWallSeconds = 2700
)

$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$EnvExe      = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain_Eval210\BossPPO_RLTrain_Eval210.exe"
$RunRoot     = Join-Path $ProjectRoot "results\$RunId"
$RunLogDir   = Join-Path $RunRoot "run_logs"
$PlayerLog   = Join-Path $RunLogDir "Player-0.log"
$PidFile     = Join-Path $RunLogDir "eval.pid"

if (!(Test-Path $EnvExe)) { Write-Error "Eval EXE not found: $EnvExe"; exit 1 }
if (Test-Path $RunRoot) { Write-Error "Run already exists: $RunRoot"; exit 1 }

New-Item -ItemType Directory -Path $RunLogDir -Force | Out-Null

Write-Host "[eval210] EXE: $EnvExe"
Write-Host "[eval210] RunId: $RunId"
Write-Host "[eval210] TargetEpisodes: $TargetEpisodes"
Write-Host "[eval210] PlayerLog: $PlayerLog"

$args = @("-logFile", $PlayerLog)
$proc = Start-Process -FilePath $EnvExe -ArgumentList $args -PassThru
Set-Content -Encoding ASCII -Path $PidFile -Value $proc.Id

$deadline = (Get-Date).AddSeconds($MaxWallSeconds)
$episodeCount = 0

try {
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 5

        if ($proc.HasExited) {
            Write-Host "[eval210] EXE exited early with code $($proc.ExitCode)"
            break
        }

        if (Test-Path $PlayerLog) {
            $episodeCount = (Select-String -Path $PlayerLog -Pattern "\[BossRL\] EPISODE_END" -Encoding UTF8).Count
            Write-Host "[eval210] episodes=$episodeCount"
            if ($episodeCount -ge $TargetEpisodes) {
                Write-Host "[eval210] Target episode count reached."
                break
            }
        }
    }
}
finally {
    if (!$proc.HasExited) {
        Write-Host "[eval210] Closing EXE PID=$($proc.Id)"
        [void]$proc.CloseMainWindow()
        if (!$proc.WaitForExit(10000)) {
            Stop-Process -Id $proc.Id -Force
        }
    }
}

Write-Host "[eval210] Finished with episodes=$episodeCount"
exit 0
