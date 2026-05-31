param(
    [string]$RunId       = "BossPPO_SafeActionMask_50k_v1",
    [string]$InitFrom    = "BossPPO_SafeActionMask_v1",
    [switch]$Force,
    [switch]$Resume,
    [switch]$NoInitFrom
)

$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"

$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_ppo_safe_mask_50k_v1.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"

if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found: $Config"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found: $EnvExe"; exit 1 }

$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "[run] Port 5004 in use — kill trainer first"; exit 1 }

Write-Host "[run] run-id=$RunId  init-from=$InitFrom  force=$Force  resume=$Resume"
Write-Host "[run] max_steps=50000  EXE=$EnvExe"
Write-Host "[run] Config=$Config"

$mlaArgs = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force)              { $mlaArgs += "--force" }
if ($Resume)             { $mlaArgs += "--resume" }
if (!$NoInitFrom -and !$Resume -and !$Force) {
    $InitPath = Join-Path $ProjectRoot "results\$InitFrom"
    if (Test-Path $InitPath) {
        $mlaArgs += "--initialize-from"
        $mlaArgs += $InitFrom
        Write-Host "[run] Using --initialize-from $InitFrom"
    } else {
        Write-Host "[run] WARNING: $InitPath not found, starting fresh"
    }
}

Set-Location $ProjectRoot
& $MlagentsLearn @mlaArgs

$exitCode = $LASTEXITCODE
Write-Host "[run] trainer exited: $exitCode"
exit $exitCode
