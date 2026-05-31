param(
    [switch]$SkipBuildIfExists
)

# Windows EXE batch build for ML-Agents RLTrain smoke.
# Unity를 batch mode로 실행해 EXE를 생성한다.
# Unity Editor를 열지 않으며, Play Mode에 진입하지 않는다.
# CODE-BLUE 원본을 건드리지 않는다.

$ErrorActionPreference = "Stop"

$ProjectRoot  = Split-Path -Parent $PSScriptRoot
$UnityExe     = "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe"
$UnityProject = Join-Path $ProjectRoot "unity_project"
$OutputExe    = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"
$LogFile      = Join-Path $ProjectRoot "logs\build_windows.log"

# 로그 디렉토리 생성
$LogDir = Split-Path $LogFile -Parent
if (!(Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }

# Unity.exe 존재 확인
if (!(Test-Path $UnityExe)) {
    Write-Error "[build_rl_windows] Unity.exe not found: $UnityExe"
    exit 1
}

# 이미 빌드가 있고 -SkipBuildIfExists 플래그가 있으면 건너뜀
if ($SkipBuildIfExists -and (Test-Path $OutputExe)) {
    Write-Host "[build_rl_windows] EXE already exists, skipping build: $OutputExe"
    exit 0
}

Write-Host "[build_rl_windows] Unity project: $UnityProject"
Write-Host "[build_rl_windows] Output EXE:    $OutputExe"
Write-Host "[build_rl_windows] Log file:      $LogFile"
Write-Host "[build_rl_windows] Starting Unity batch build..."
Write-Host ""

# builds 출력 디렉토리 생성 (Unity가 만들지 못할 경우를 대비)
$OutputDir = Split-Path $OutputExe -Parent
if (!(Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }

# Unity batch mode 실행
$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $UnityProject,
    "-executeMethod", "RLTrainBatchBuild.Build",
    "-logFile", $LogFile
)

Write-Host "[build_rl_windows] Running: `"$UnityExe`" $($unityArgs -join ' ')"
Write-Host ""

$proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -Wait -PassThru
$exitCode = $proc.ExitCode

Write-Host ""
Write-Host "[build_rl_windows] Unity exited with code: $exitCode"
Write-Host "[build_rl_windows] Log: $LogFile"

if ($exitCode -eq 0 -and (Test-Path $OutputExe)) {
    $size = (Get-Item $OutputExe).Length
    Write-Host "[build_rl_windows] Build succeeded. EXE: $OutputExe ($size bytes)"
    exit 0
} else {
    Write-Host "[build_rl_windows] Build FAILED or EXE not found."
    Write-Host "[build_rl_windows] Last 30 lines of log:"
    if (Test-Path $LogFile) {
        Get-Content $LogFile -Tail 30
    }
    exit 1
}
