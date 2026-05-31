param(
    [string]$RunId  = "BossPPO_ClearRun_v1",
    [switch]$Force,
    [switch]$Resume
)

$ErrorActionPreference = "Stop"
$env:PYTHONUTF8 = "1"

$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$MlagentsLearn = Join-Path $ProjectRoot ".venv-mlagents\Scripts\mlagents-learn.exe"
$Config        = Join-Path $ProjectRoot "ml-agents-config\boss_player_ppo_clearrun.yaml"
$EnvExe        = Join-Path $ProjectRoot "builds\windows\BossPPO_RLTrain\BossPPO_RLTrain.exe"

if (!(Test-Path $MlagentsLearn)) { Write-Error "mlagents-learn not found"; exit 1 }
if (!(Test-Path $Config))        { Write-Error "config not found"; exit 1 }
if (!(Test-Path $EnvExe))        { Write-Error "EXE not found"; exit 1 }

$portCheck = netstat -ano | Select-String ":5004 "
if ($portCheck) { Write-Host "Port 5004 in use — kill and retry"; exit 1 }

Write-Host "[clearrun] run-id=$RunId  force=$Force  resume=$Resume"
Write-Host "[clearrun] max_steps=50000  EXE=$EnvExe"

$args = @($Config, "--run-id", $RunId, "--env", $EnvExe, "--num-envs", "1")
if ($Force)  { $args += "--force" }
if ($Resume) { $args += "--resume" }

Set-Location $ProjectRoot
& $MlagentsLearn @args

$exitCode = $LASTEXITCODE
Write-Host "[clearrun] trainer exited: $exitCode"
exit $exitCode
