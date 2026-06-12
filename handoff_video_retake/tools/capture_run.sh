#!/usr/bin/env zsh
# Trainer-inference screen capture for one capture run (macOS).
# Selection MUST later be made from THIS run's own Player log.
# Usage:
#   capture_run.sh -r <RunId> -n <CaptureName> -f <InitializeFrom> [-e <Episodes>]
#
# Example:
#   ./capture_run.sh -r VideoRetakeMac_03_99K -n capture_3_99k -f VideoStage_ppo99k -e 10

set -euo pipefail

SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h:h}"    # handoff_video_retake/tools → project root
CAPTURE_DIR="$SCRIPT_DIR/raw"
CONFIG="$PROJECT_ROOT/ml-agents-config/boss_ppo_target_gate_markatk_sweep_1m_c84b1a4.yaml"
GAME_APP="$PROJECT_ROOT/builds/macos/BossPPO_RLTrain.app"
VENV_PYTHON="$PROJECT_ROOT/.venv/bin/python"
MLAGENTS="$PROJECT_ROOT/.venv/bin/mlagents-learn"

TARGET_EPISODES=12
RUN_ID=""
CAPTURE_NAME=""
INITIALIZE_FROM=""

usage() {
    echo "Usage: $0 -r <RunId> -n <CaptureName> -f <InitializeFrom> [-e <Episodes>]" >&2
    exit 1
}

while getopts "r:n:f:e:" opt; do
    case $opt in
        r) RUN_ID=$OPTARG ;;
        n) CAPTURE_NAME=$OPTARG ;;
        f) INITIALIZE_FROM=$OPTARG ;;
        e) TARGET_EPISODES=$OPTARG ;;
        *) usage ;;
    esac
done
[[ -z "$RUN_ID" || -z "$CAPTURE_NAME" || -z "$INITIALIZE_FROM" ]] && usage

RAW_VIDEO="$CAPTURE_DIR/${CAPTURE_NAME}.mkv"
META_FILE="$CAPTURE_DIR/${CAPTURE_NAME}.json"
RUN_ROOT="$PROJECT_ROOT/results/$RUN_ID"
PLAYER_LOG="$RUN_ROOT/run_logs/Player-0.log"
TRAINER_LOG="$CAPTURE_DIR/${CAPTURE_NAME}.trainer.log"

for f in "$CONFIG" "$GAME_APP" "$MLAGENTS"; do
    [[ -e "$f" ]] || { echo "ERROR: missing $f" >&2; exit 1 }
done
[[ -e "$RUN_ROOT" ]] && { echo "ERROR: run already exists: $RUN_ROOT" >&2; exit 1 }
[[ -e "$RAW_VIDEO" ]] && { echo "ERROR: capture already exists: $RAW_VIDEO" >&2; exit 1 }

mkdir -p "$CAPTURE_DIR"

# Detect ffmpeg
FFMPEG=$($VENV_PYTHON -c "import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())" 2>/dev/null || which ffmpeg)

# avfoundation display device index ("Capture screen 0" = 2 on this machine)
AVFOUNDATION_DEVICE="${AVFOUNDATION_DEVICE:-2:none}"

echo "[capture] Starting ffmpeg avfoundation capture → $RAW_VIDEO"
# -capture_cursor 0: no cursor in capture
# Device "1:none" = built-in display, no audio
# Capture at native resolution (Retina on MacBook → 2x)
FFMPEG_START_EPOCH=$(python3 -c "import time; print(time.time())")
"$FFMPEG" -y -hide_banner -loglevel warning \
    -f avfoundation -capture_cursor 0 -framerate 60 \
    -i "${AVFOUNDATION_DEVICE}" \
    -vf "format=yuv420p" \
    -c:v libx264 -preset veryfast -crf 18 \
    -color_range tv -colorspace bt709 \
    -color_primaries bt709 -color_trc bt709 \
    "$RAW_VIDEO" &
FFMPEG_PID=$!
echo "[capture] ffmpeg pid=$FFMPEG_PID"

# Give ffmpeg 2s to initialize before starting the game
sleep 2

echo "[capture] Starting mlagents-learn inference run-id=$RUN_ID"
GAME_START_EPOCH=$(python3 -c "import time; print(time.time())")
"$MLAGENTS" "$CONFIG" \
    --run-id "$RUN_ID" \
    --initialize-from "$INITIALIZE_FROM" \
    --inference \
    --env "$GAME_APP" \
    --num-envs 1 \
    --time-scale 1 \
    --width 1280 --height 720 \
    --quality-level 5 \
    --target-frame-rate 60 \
    --capture-frame-rate 0 \
    --timeout-wait 60 \
    --env-args -popupwindow -screen-fullscreen 0 \
    > "$TRAINER_LOG" 2>&1 &
TRAINER_PID=$!
echo "[capture] trainer pid=$TRAINER_PID"

cleanup() {
    echo "[capture] cleaning up..."
    kill "$TRAINER_PID" 2>/dev/null || true
    # Kill entire trainer process group (spawned game subprocess)
    pkill -f "BossPPO_RLTrain" 2>/dev/null || true
    sleep 2
    # Send 'q' to ffmpeg stdin via kill -2 (SIGINT) for clean stop
    kill -INT "$FFMPEG_PID" 2>/dev/null || true
    wait "$FFMPEG_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

# Poll for episode count
EPISODE_COUNT=0
DEADLINE=$(($(date +%s) + 3600))
echo "[capture] polling for $TARGET_EPISODES episodes..."

while [[ $(date +%s) -lt $DEADLINE ]]; do
    sleep 5
    if ! kill -0 "$TRAINER_PID" 2>/dev/null; then
        echo "ERROR: trainer exited early" >&2
        exit 1
    fi
    if [[ -f "$PLAYER_LOG" ]]; then
        # grep -c prints "0" and exits 1 on no match — don't append a second 0
        EPISODE_COUNT=$(grep -c '\[BossRL\] EPISODE_END' "$PLAYER_LOG" 2>/dev/null || true)
        EPISODE_COUNT=${EPISODE_COUNT:-0}
        echo "[capture] episodes=$EPISODE_COUNT / $TARGET_EPISODES"
        if [[ "$EPISODE_COUNT" -ge "$TARGET_EPISODES" ]]; then
            break
        fi
    fi
done

if [[ "$EPISODE_COUNT" -lt "$TARGET_EPISODES" ]]; then
    echo "ERROR: timeout at $EPISODE_COUNT/$TARGET_EPISODES episodes" >&2
    exit 1
fi

echo "[capture] target reached — letting last episode settle (3s)..."
sleep 3

# Graceful shutdown
echo "[capture] stopping trainer..."
kill "$TRAINER_PID" 2>/dev/null || true
pkill -f "BossPPO_RLTrain" 2>/dev/null || true
sleep 2
echo "[capture] stopping ffmpeg..."
kill -INT "$FFMPEG_PID" 2>/dev/null || true
wait "$FFMPEG_PID" 2>/dev/null || true
trap - EXIT INT TERM

# Detect actual capture dimensions from raw video
RAW_DIMS=$("$FFMPEG" -i "$RAW_VIDEO" 2>&1 | grep -oE '[0-9]+x[0-9]+' | head -1 || echo "")
RAW_W="${RAW_DIMS%x*}"
RAW_H="${RAW_DIMS#*x}"

# game_offset_seconds = game_start - ffmpeg_start
GAME_OFFSET=$(python3 -c "print(round($GAME_START_EPOCH - $FFMPEG_START_EPOCH, 3))")

python3 -c "
import json, sys
meta = {
    'capture_name': '$CAPTURE_NAME',
    'run_id': '$RUN_ID',
    'initialize_from': '$INITIALIZE_FROM',
    'target_episodes': $TARGET_EPISODES,
    'captured_episodes': $EPISODE_COUNT,
    'time_scale': 1,
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
echo "[capture] raw dimensions: ${RAW_W}x${RAW_H}"
echo ""
echo "Next: check dimensions and run calibration:"
echo "  python handoff_video_retake/tools/parse_episodes.py $PLAYER_LOG"
echo "  python handoff_video_retake/tools/montage.py $RAW_VIDEO <wall_end_of_E1 - 3> 6 check.png --step 0.25"
