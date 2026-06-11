# Sequential capture chain for the six retake videos.
# Stops at the first failure so leftover processes never poison later runs.
$ErrorActionPreference = "Stop"
$Here = $PSScriptRoot
$Root = "C:\Users\KiKi\Documents\Ql\AI_FinalTerm_MLAgents_PPO_NEW"

function Invoke-Step([string]$Name, [scriptblock]$Body) {
    Write-Host "`n===== STEP $Name ====="
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & $Body
    Write-Host "===== STEP $Name done in $([int]$sw.Elapsed.TotalSeconds)s ====="
}

# Stages 1-3: embedded ONNX, Eval210, realtime (helpless/dodge-only profiles
# only reproduce on this path).
Invoke-Step "1 FR995 embedded" {
    powershell -NoProfile -ExecutionPolicy Bypass -File "$Here\capture_run_embedded.ps1" `
        -ModelSource "results\VideoCandidates_FreshRandom_1K_20260611\BossPlayer\BossPlayer-995.onnx" `
        -ModelLabel "Retake01_FR995" `
        -RunId "VideoRetake_01_FR995_20260611" `
        -CaptureName "retake_01_fr995" `
        -TargetEpisodes 12 -MaxWallSeconds 1500
    if ($LASTEXITCODE -ne 0) { throw "step 1 failed" }
}

Invoke-Step "2 PPO99K embedded" {
    powershell -NoProfile -ExecutionPolicy Bypass -File "$Here\capture_run_embedded.ps1" `
        -ModelSource "results\BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1\BossPlayer\BossPlayer-99957.onnx" `
        -ModelLabel "Retake02_PPO99K" `
        -RunId "VideoRetake_02_PPO99K_20260611" `
        -CaptureName "retake_02_ppo99k" `
        -TargetEpisodes 12 -MaxWallSeconds 1800
    if ($LASTEXITCODE -ne 0) { throw "step 2 failed" }
}

Invoke-Step "3 PPO499K embedded" {
    powershell -NoProfile -ExecutionPolicy Bypass -File "$Here\capture_run_embedded.ps1" `
        -ModelSource "results\BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1\BossPlayer\BossPlayer-499996.onnx" `
        -ModelLabel "Retake03_PPO499K" `
        -RunId "VideoRetake_03_PPO499K_20260611" `
        -CaptureName "retake_03_ppo499k" `
        -TargetEpisodes 12 -MaxWallSeconds 1800
    if ($LASTEXITCODE -ne 0) { throw "step 3 failed" }
}

# Stage 3-1: 999K via trainer inference (fails ~7% of episodes after reaching
# phase 3; need enough episodes to land deaths on tape).
Invoke-Step "3-1 PPO999K trainer" {
    powershell -NoProfile -ExecutionPolicy Bypass -File "$Here\capture_run.ps1" `
        -RunId "VideoRetake_31_PPO999K_20260611" `
        -CaptureName "retake_31_ppo999k" `
        -InitializeFrom "VideoStage_999K" `
        -TargetEpisodes 45 -MaxWallSeconds 2700
    if ($LASTEXITCODE -ne 0) { throw "step 3-1 failed" }
}

# Stages 4+5: final 1M policy via trainer inference; mark-exposing clears are
# ~3/30 so 45 episodes gives good odds, fastest clear comes from the same run.
Invoke-Step "4+5 PPO1M trainer" {
    powershell -NoProfile -ExecutionPolicy Bypass -File "$Here\capture_run.ps1" `
        -RunId "VideoRetake_45_PPO1M_20260611" `
        -CaptureName "retake_45_ppo1m" `
        -InitializeFrom "BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1" `
        -TargetEpisodes 45 -MaxWallSeconds 2700
    if ($LASTEXITCODE -ne 0) { throw "step 4+5 failed" }
}

Write-Host "`nALL CAPTURES COMPLETE"
