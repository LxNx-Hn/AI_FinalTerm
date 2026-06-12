#!/usr/bin/env zsh
# Phase 2: Cut final videos from raw captures using chosen episode numbers.
# Run from project root after run_all_captures.sh completes.
#
# Usage:
#   zsh handoff_video_retake/tools/select_and_cut.sh \
#     -1 <ep_fr995>   -2 <ep_99k_embedded> \
#     -3 <ep_99k_trainer>  -x <ep_499k>  \
#     -4 <ep_1m_patterns>  -5 <ep_1m_fastest>
#
# Selection criteria (from HANDOFF §2-3):
#   Video 1: shortest player_dead, boss_damage 0-2, no dodge
#   Video 2: player_dead, survival > 40s, escape_success >= 15, boss_damage <= 12
#   Video 3: player_dead, boss_damage 18-25, sweep_steps > 200
#   Video 3-1: player_dead, mark_real >= 2, sweep_steps >= 1000
#   Video 4: boss_dead, mark_real >= 1, sweep_steps >= 150
#   Video 5: boss_dead, shortest survival in 1M run (same capture as video 4)
#
# Crop note:
#   CROP_Y=0 → no y-offset (hearts safe, menu bar may appear at top ~18px in output)
#   To measure exact game window y-offset: use montage.py on a test frame first.

set -euo pipefail
SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h:h}"
TOOLS="$SCRIPT_DIR"
RAW_DIR="$SCRIPT_DIR/raw"
VIDEOS_DIR="$PROJECT_ROOT/videos"
VENV_PYTHON="$PROJECT_ROOT/.venv/bin/python"

# Default crop: full frame, no y offset (fixes the hearts-cut bug from v2)
CROP_Y="${CROP_Y:-0}"

EP1="" EP2="" EP3="" EPX="" EP4="" EP5=""
CALIB="${CALIB:-0.0}"

usage() {
    echo "Usage: $0 -1 <ep> -2 <ep> -3 <ep> -x <ep> -4 <ep> -5 <ep> [-c <calib>]" >&2
    echo "  Calib is a per-capture constant from montage.py (usually 0.0 if not measured)" >&2
    exit 1
}

while getopts "1:2:3:x:4:5:c:" opt; do
    case $opt in
        1) EP1=$OPTARG ;; 2) EP2=$OPTARG ;; 3) EP3=$OPTARG ;;
        x) EPX=$OPTARG ;; 4) EP4=$OPTARG ;; 5) EP5=$OPTARG ;;
        c) CALIB=$OPTARG ;; *) usage ;;
    esac
done
[[ -z "$EP1" || -z "$EP2" || -z "$EP3" || -z "$EPX" || -z "$EP4" || -z "$EP5" ]] && usage

log() { echo "[cut] $*" }

cut_one() {
    local meta=$1 episode=$2 out=$3 calib=${4:-$CALIB}
    local raw_w raw_h
    raw_w=$(python3 -c "import json; m=json.load(open('$meta')); print(m.get('raw_width') or '2560')")
    raw_h=$(python3 -c "import json; m=json.load(open('$meta')); print(m.get('raw_height') or '1440')")
    local crop="${raw_w}:${raw_h}:0:${CROP_Y}"
    log "Cutting $out  (ep=$episode crop=$crop calib=$calib)"
    "$VENV_PYTHON" "$TOOLS/cut_episode.py" "$meta" "$episode" "$out" \
        --calib "$calib" --crop "$crop" --scale "1920x1080" --frames
}

mkdir -p "$VIDEOS_DIR"

log "Video 1 — no_dodge (FR995 ep$EP1)"
cut_one "$RAW_DIR/capture_1_fr995.json" "$EP1" "$VIDEOS_DIR/01_no_dodge.mp4"

log "Video 2 — dodge_only (99K ep$EP2)"
cut_one "$RAW_DIR/capture_2_99k.json" "$EP2" "$VIDEOS_DIR/02_dodge_only.mp4"

log "Video 3 — play_but_fail (99K trainer ep$EP3)"
cut_one "$RAW_DIR/capture_3_99k.json" "$EP3" "$VIDEOS_DIR/03_play_but_fail.mp4"

log "Video 3-1 — fail_all_patterns (499K ep$EPX)"
cut_one "$RAW_DIR/capture_31_499k.json" "$EPX" "$VIDEOS_DIR/03-1_fail_all_patterns.mp4"

log "Video 4 — clear_full_patterns (1M ep$EP4)"
cut_one "$RAW_DIR/capture_45_1m.json" "$EP4" "$VIDEOS_DIR/04_clear_full_patterns.mp4"

log "Video 5 — fastest_clear (1M ep$EP5)"
cut_one "$RAW_DIR/capture_45_1m.json" "$EP5" "$VIDEOS_DIR/05_fastest_clear.mp4"

# ── Verify frames ─────────────────────────────────────────────────────────────
log "=== Verification ==="
ALL_OK=1
for v in 01_no_dodge 02_dodge_only 03_play_but_fail 03-1_fail_all_patterns 04_clear_full_patterns 05_fastest_clear; do
    f="$VIDEOS_DIR/${v}.mp4"
    if [[ -f "$f" ]]; then
        dims=$(ffprobe -v quiet -select_streams v:0 \
            -show_entries stream=width,height -of csv=p=0 "$f" 2>/dev/null || echo "?")
        log "  ✓ $v  ($dims)"
    else
        log "  ✗ MISSING: $v"
        ALL_OK=0
    fi
done

if [[ "$ALL_OK" == "1" ]]; then
    echo ""
    log "All 6 videos OK."
    log "Check the *_start.png / *_end.png frames next to each video to verify:"
    log "  - start: boss HP full, player spawned (no previous episode visible)"
    log "  - end: player_dead or boss_dead moment (no next episode)"
    log "  - hearts visible top-left (if not, adjust CROP_Y and rerun)"
    echo ""
    log "When satisfied, delete old videos:"
    log "  zsh handoff_video_retake/tools/cleanup_old_videos.sh"
fi
