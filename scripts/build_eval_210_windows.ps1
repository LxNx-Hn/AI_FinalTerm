param(
    [switch]$CloseUnity,
    [switch]$HeuristicOnly,
    [string]$ModelSource = "results\BossPPO_SafeActionMask_100k_v1\BossPlayer.onnx",
    [string]$ModelLabel = "100kFinal"
)

$ErrorActionPreference = "Stop"

$ProjectRoot  = Split-Path -Parent $PSScriptRoot
$UnityExe     = "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe"
$UnityProject = Join-Path $ProjectRoot "unity_project"
$OutputExe    = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain_Eval210\BossPPO_RLTrain_Eval210.exe"
$SafeModelLabel = ($ModelLabel -replace '[^A-Za-z0-9_.-]', '_')
$LogFile      = Join-Path $ProjectRoot "logs\build_eval_210_$SafeModelLabel.log"
$ModelPath    = if ([System.IO.Path]::IsPathRooted($ModelSource)) { $ModelSource } else { Join-Path $ProjectRoot $ModelSource }

if (!(Test-Path $UnityExe)) { Write-Error "Unity.exe not found: $UnityExe"; exit 1 }
if (!(Test-Path $UnityProject)) { Write-Error "Unity project not found: $UnityProject"; exit 1 }
if (!$HeuristicOnly -and !(Test-Path $ModelPath)) { Write-Error "ModelSource not found: $ModelPath"; exit 1 }

$LogDir = Split-Path $LogFile -Parent
if (!(Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }

if ($CloseUnity) {
    $escapedProject = [Regex]::Escape($UnityProject)
    $unityProcesses = Get-CimInstance Win32_Process |
        Where-Object { $_.Name -eq "Unity.exe" -and $_.CommandLine -match $escapedProject -and $_.CommandLine -notmatch "AssetImportWorker" }

    foreach ($procInfo in $unityProcesses) {
        Write-Host "[build_eval_210] Closing Unity editor PID=$($procInfo.ProcessId)"
        $proc = Get-Process -Id $procInfo.ProcessId -ErrorAction SilentlyContinue
        if ($proc) {
            [void]$proc.CloseMainWindow()
            if (!$proc.WaitForExit(60000)) {
                Write-Host "[build_eval_210] Unity did not close after 60s; stopping PID=$($proc.Id)"
                Stop-Process -Id $proc.Id -Force
            }
        }
    }
}

$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $UnityProject,
    "-executeMethod", "RLEval210BatchBuild.Build",
    "-logFile", $LogFile
)

if ($HeuristicOnly) {
    $unityArgs += "-heuristicOnly"
} else {
    $unityArgs += @("-evalModelSource", $ModelPath)
}

Write-Host "[build_eval_210] Unity project: $UnityProject"
Write-Host "[build_eval_210] Output EXE:    $OutputExe"
Write-Host "[build_eval_210] Log file:      $LogFile"
Write-Host "[build_eval_210] HeuristicOnly: $HeuristicOnly"
if (!$HeuristicOnly) { Write-Host "[build_eval_210] Model source:  $ModelPath" }
Write-Host "[build_eval_210] Running: `"$UnityExe`" $($unityArgs -join ' ')"

$proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -Wait -PassThru -WindowStyle Hidden
$exitCode = $proc.ExitCode

Write-Host "[build_eval_210] Unity exited with code: $exitCode"
if ($exitCode -eq 0 -and (Test-Path $OutputExe)) {
    Write-Host "[build_eval_210] Build succeeded: $OutputExe"
    exit 0
}

Write-Host "[build_eval_210] Build failed. Last 80 log lines:"
if (Test-Path $LogFile) { Get-Content $LogFile -Tail 80 }
exit 1
