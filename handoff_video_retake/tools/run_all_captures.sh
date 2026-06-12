#!/usr/bin/env zsh
# Phase 1: Build Unity apps and run all 5 screen captures.
# After this finishes, run select_and_cut.sh to choose episodes and produce final videos.
#
# Run from project root:
#   zsh handoff_video_retake/tools/run_all_captures.sh
#
# Skip builds if apps already exist:
#   SKIP_BUILD=1 zsh handoff_video_retake/tools/run_all_captures.sh

set -euo pipefail
SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h:h}"
TOOLS="$SCRIPT_DIR"
UNITY="/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity"
CHECKPOINTS="$SCRIPT_DIR/../checkpoints"
RAW_DIR="$SCRIPT_DIR/raw"
SKIP_BUILD="${SKIP_BUILD:-0}"
VENV_PYTHON="$PROJECT_ROOT/.venv/bin/python"

log() { echo "[run_all] $(date '+%H:%M:%S') $*" }

# ── Sanity checks ─────────────────────────────────────────────────────────────
log "Checking environment..."
[[ -x "$VENV_PYTHON" ]] || {
    echo "ERROR: .venv not found. Run:" >&2
    echo "  cd '$PROJECT_ROOT' && python3 -m venv .venv && .venv/bin/pip install mlagents imageio-ffmpeg" >&2
    exit 1
}
"$VENV_PYTHON" -c "import mlagents" 2>/dev/null || {
    echo "ERROR: mlagents not installed. Run: .venv/bin/pip install mlagents imageio-ffmpeg" >&2
    exit 1
}
mkdir -p "$RAW_DIR" "$PROJECT_ROOT/logs" "$PROJECT_ROOT/results"

# ── Unity Builds ─────────────────────────────────────────────────────────────
if [[ "$SKIP_BUILD" != "1" ]]; then
    log "Building RLTrain.app..."
    "$UNITY" -batchmode -nographics -quit \
        -projectPath "$PROJECT_ROOT/unity_project" \
        -executeMethod RLTrainBatchBuild.Build \
        -logFile "$PROJECT_ROOT/logs/build_rltrain_mac.log"
    log "✓ RLTrain.app → builds/macos/BossPPO_RLTrain.app"

    log "Building Eval210.app with FR995 model..."
    "$UNITY" -batchmode -nographics -quit \
        -projectPath "$PROJECT_ROOT/unity_project" \
        -executeMethod RLEval210BatchBuild.Build \
        -evalModelSource "$CHECKPOINTS/fr995/BossPlayer-995.onnx" \
        -logFile "$PROJECT_ROOT/logs/build_eval210_fr995.log"
    log "✓ Eval210 FR995 built"

    log "Building Eval210.app with 99K model..."
    "$UNITY" -batchmode -nographics -quit \
        -projectPath "$PROJECT_ROOT/unity_project" \
        -executeMethod RLEval210BatchBuild.Build \
        -evalModelSource "$CHECKPOINTS/ppo99k/BossPlayer-99957.onnx" \
        -logFile "$PROJECT_ROOT/logs/build_eval210_99k.log"
    log "✓ Eval210 99K built"
else
    log "SKIP_BUILD=1 — skipping Unity builds"
fi

# ── Captures ─────────────────────────────────────────────────────────────────
# Eval210 빌드는 99K를 마지막으로 덮어쓰므로, 99K 캡처를 먼저 해야 함.
# FR995 캡처를 위해서는 FR995 빌드를 앞에서 해두고 캡처 → 다시 99K 빌드 → 캡처 순.
# 여기서는 script가 내부적으로 build를 하므로 embedded 스크립트를 그대로 호출.

log "=== [1/5] Capture: FR995 embedded (12 episodes) ==="
zsh "$TOOLS/capture_run_embedded.sh" \
    -m "handoff_video_retake/checkpoints/fr995/BossPlayer-995.onnx" \
    -l "FR995" -r "VideoRetakeMac_01_FR995_v3" -n "capture_1_fr995" -e 12

log "=== [2/5] Capture: 99K embedded (12 episodes) ==="
zsh "$TOOLS/capture_run_embedded.sh" \
    -m "handoff_video_retake/checkpoints/ppo99k/BossPlayer-99957.onnx" \
    -l "99K" -r "VideoRetakeMac_02_99K_v3" -n "capture_2_99k" -e 12

log "=== [3/5] Capture: 99K trainer (10 episodes, for video 3) ==="
zsh "$TOOLS/capture_run.sh" \
    -r "VideoRetakeMac_03_99K_v3" -n "capture_3_99k" -f "VideoStage_ppo99k" -e 10

log "=== [4/5] Capture: 499K trainer (10 episodes, for video 3-1) ==="
zsh "$TOOLS/capture_run.sh" \
    -r "VideoRetakeMac_31_499K_v3" -n "capture_31_499k" -f "VideoStage_ppo499k" -e 10

log "=== [5/5] Capture: 1M trainer (45 episodes, for videos 4 & 5) ==="
zsh "$TOOLS/capture_run.sh" \
    -r "VideoRetakeMac_45_1M_v3" -n "capture_45_1m" -f "VideoStage_ppo1m" -e 45

# ── Episode tables ────────────────────────────────────────────────────────────
log "=== All captures complete. Episode tables: ==="

for cap in capture_1_fr995 capture_2_99k capture_3_99k capture_31_499k capture_45_1m; do
    meta="$RAW_DIR/${cap}.json"
    log_path=$(python3 -c "import json; print(json.load(open('$meta'))['player_log'])")
    echo ""
    echo "── $cap ──────────────────────────────────"
    "$VENV_PYTHON" "$TOOLS/parse_episodes.py" "$log_path" 2>/dev/null || echo "(parse failed)"
done

echo ""
log "Now choose episodes and cut — run:"
log "  zsh handoff_video_retake/tools/select_and_cut.sh \\"
log "    -1 <ep> -2 <ep> -3 <ep> -x <ep> -4 <ep> -5 <ep>"
log "  (use the episode tables above to choose; see HANDOFF.md §2-3 for criteria)"
