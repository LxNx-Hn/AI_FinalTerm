#!/usr/bin/env zsh
# Embedded-ONNX Eval210 capture at realtime (macOS).
# Used for early/mid checkpoints (FR995, 99K) — trainer path makes even FR995 evade.
# Usage:
#   capture_run_embedded.sh -m <onnx_path> -l <ModelLabel> -r <RunId> -n <CaptureName> [-e <Episodes>]
#
# Example:
#   ./capture_run_embedded.sh \
#     -m handoff_video_retake/checkpoints/fr995/BossPlayer-995.onnx \
#     -l FR995 -r VideoRetakeMac_01_FR995 -n capture_1_fr995 -e 12

set -euo pipefail

SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h:h}"
CAPTURE_DIR="$SCRIPT_DIR/raw"
UNITY_APP="/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity"
GAME_APP="$PROJECT_ROOT/builds/macos/BossPPO_RLTrain_Eval210.app"
VENV_PYTHON="$PROJECT_ROOT/.venv/bin/python"

TARGET_EPISODES=12
MODEL_SOURCE=""
MODEL_LABEL=""
RUN_ID=""
CAPTURE_NAME=""

usage() {
    echo "Usage: $0 -m <onnx_path> -l <ModelLabel> -r <RunId> -n <CaptureName> [-e <Episodes>]" >&2
    exit 1
}

while getopts "m:l:r:n:e:" opt; do
    case $opt in
        m) MODEL_SOURCE=$OPTARG ;;
        l) MODEL_LABEL=$OPTARG ;;
        r) RUN_ID=$OPTARG ;;
        n) CAPTURE_NAME=$OPTARG ;;
        e) TARGET_EPISODES=$OPTARG ;;
        *) usage ;;
    esac
done
[[ -z "$MODEL_SOURCE" || -z "$MODEL_LABEL" || -z "$RUN_ID" || -z "$CAPTURE_NAME" ]] && usage

MODEL_SOURCE_ABS="${PROJECT_ROOT}/${MODEL_SOURCE#./}"
[[ "$MODEL_SOURCE" = /* ]] && MODEL_SOURCE_ABS="$MODEL_SOURCE"

RAW_VIDEO="$CAPTURE_DIR/${CAPTURE_NAME}.mkv"
META_FILE="$CAPTURE_DIR/${CAPTURE_NAME}.json"
RUN_LOG_DIR="$PROJECT_ROOT/results/$RUN_ID/run_logs"
PLAYER_LOG="$RUN_LOG_DIR/Player-0.log"

[[ -e "$RAW_VIDEO" ]] && { echo "ERROR: capture already exists: $RAW_VIDEO" >&2; exit 1 }
[[ -e "$PROJECT_ROOT/results/$RUN_ID" ]] && { echo "ERROR: run already exists" >&2; exit 1 }

mkdir -p "$CAPTURE_DIR" "$RUN_LOG_DIR"

FFMPEG=$($VENV_PYTHON -c "import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())" 2>/dev/null || which ffmpeg)
AVFOUNDATION_DEVICE="${AVFOUNDATION_DEVICE:-2:none}"

# Step 1: Build Eval210 app with the specified ONNX
echo "[capture] Building Eval210 with model: $MODEL_LABEL"
# -nographics omitted: this machine's license lacks the headless entitlement
"$UNITY_APP" \
    -batchmode -quit \
    -projectPath "$PROJECT_ROOT/unity_project" \
    -executeMethod RLEval210BatchBuild.Build \
    -evalModelSource "$MODEL_SOURCE_ABS" \
    -logFile "$PROJECT_ROOT/logs/build_eval210_${MODEL_LABEL}.log" \
    2>&1 | tail -5
[[ -d "$GAME_APP" ]] || { echo "ERROR: Eval210 build failed — check logs/build_eval210_${MODEL_LABEL}.log" >&2; exit 1 }
echo "[capture] build complete → $GAME_APP"

# Step 2: Start ffmpeg
echo "[capture] Starting ffmpeg → $RAW_VIDEO"
FFMPEG_START_EPOCH=$(python3 -c "import time; print(time.time())")
# h264_videotoolbox: M1 하드웨어 인코더 — x264 소프트웨어는 2880x1800@60을
# 실시간 처리 못해 프레임을 드랍한다 (실측 ~3fps 사고의 원인)
"$FFMPEG" -y -hide_banner -loglevel warning \
    -f avfoundation -capture_cursor 0 -framerate 60 \
    -i "${AVFOUNDATION_DEVICE}" \
    -c:v h264_videotoolbox -b:v 40000k \
    "$RAW_VIDEO" &
FFMPEG_PID=$!
sleep 2

# Step 3: Launch game directly (no trainer needed)
echo "[capture] Launching Eval210 game..."
GAME_BIN=$(find "$GAME_APP/Contents/MacOS" -type f | head -1)
GAME_START_EPOCH=$(python3 -c "import time; print(time.time())")
"$GAME_BIN" \
    -popupwindow \
    -screen-width 1280 -screen-height 720 \
    -screen-fullscreen 0 \
    -logFile "$PLAYER_LOG" &
GAME_PID=$!
echo "[capture] game pid=$GAME_PID ffmpeg pid=$FFMPEG_PID"

cleanup() {
    echo "[capture] cleaning up..."
    kill "$GAME_PID" 2>/dev/null || true
    sleep 2
    kill -INT "$FFMPEG_PID" 2>/dev/null || true
    wait "$FFMPEG_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

# Poll for episodes
EPISODE_COUNT=0
DEADLINE=$(($(date +%s) + 1800))
echo "[capture] polling for $TARGET_EPISODES episodes..."

while [[ $(date +%s) -lt $DEADLINE ]]; do
    sleep 3
    if ! kill -0 "$GAME_PID" 2>/dev/null; then
        echo "ERROR: game exited early" >&2; exit 1
    fi
    if [[ -f "$PLAYER_LOG" ]]; then
        # grep -c prints "0" and exits 1 on no match — don't append a second 0
        EPISODE_COUNT=$(grep -c '\[BossRL\] EPISODE_END' "$PLAYER_LOG" 2>/dev/null || true)
        EPISODE_COUNT=${EPISODE_COUNT:-0}
        echo "[capture] episodes=$EPISODE_COUNT / $TARGET_EPISODES"
        [[ "$EPISODE_COUNT" -ge "$TARGET_EPISODES" ]] && break
    fi
done

[[ "$EPISODE_COUNT" -lt "$TARGET_EPISODES" ]] && {
    echo "ERROR: timeout at $EPISODE_COUNT/$TARGET_EPISODES" >&2; exit 1
}

sleep 3
kill "$GAME_PID" 2>/dev/null || true
sleep 2
kill -INT "$FFMPEG_PID" 2>/dev/null || true
wait "$FFMPEG_PID" 2>/dev/null || true
trap - EXIT INT TERM

RAW_DIMS=$("$FFMPEG" -i "$RAW_VIDEO" 2>&1 | grep -oE '[0-9]+x[0-9]+' | head -1 || echo "")
RAW_W="${RAW_DIMS%x*}"
RAW_H="${RAW_DIMS#*x}"
GAME_OFFSET=$(python3 -c "print(round($GAME_START_EPOCH - $FFMPEG_START_EPOCH, 3))")

python3 -c "
import json
meta = {
    'capture_name': '$CAPTURE_NAME',
    'model_source': '$MODEL_SOURCE_ABS',
    'model_label': '$MODEL_LABEL',
    'run_id': '$RUN_ID',
    'target_episodes': $TARGET_EPISODES,
    'captured_episodes': $EPISODE_COUNT,
    'recording_frame_rate': 60,
    'raw_width': '${RAW_W}',
    'raw_height': '${RAW_H}',
    'game_offset_seconds': $GAME_OFFSET,
    'player_log': '$PLAYER_LOG',
    'raw_video': '$RAW_VIDEO',
}
print(json.dumps(meta, indent=2))
" > "$META_FILE"

echo "[capture] complete: $RAW_VIDEO"
echo "[capture] metadata: $META_FILE"
