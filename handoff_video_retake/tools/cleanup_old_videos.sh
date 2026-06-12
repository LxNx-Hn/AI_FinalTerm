#!/usr/bin/env zsh
# Delete old/duplicate videos once new ones are verified.
# Only runs if all 6 new videos exist and are 1920x1080.

set -euo pipefail
SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h:h}"
VIDEOS_DIR="$PROJECT_ROOT/videos"

NEW_VIDEOS=(
    01_no_dodge 02_dodge_only 03_play_but_fail
    03-1_fail_all_patterns 04_clear_full_patterns 05_fastest_clear
)

OLD_VIDEOS=(
    01_initial_policy 02_learning_progression 03_mid_pattern_learning
    04_late_phase23_patterns 05_late_clever_clear
)

echo "=== Verifying new videos before cleanup ==="
ALL_OK=1
for v in "${NEW_VIDEOS[@]}"; do
    f="$VIDEOS_DIR/${v}.mp4"
    if [[ ! -f "$f" ]]; then
        echo "  ✗ MISSING: $v.mp4 — aborting"
        ALL_OK=0
    else
        dims=$(ffprobe -v quiet -select_streams v:0 \
            -show_entries stream=width,height -of csv=p=0 "$f" 2>/dev/null || echo "?")
        if [[ "$dims" != "1920,1080" ]]; then
            echo "  ✗ WRONG DIMENSIONS ($dims): $v.mp4 — aborting"
            ALL_OK=0
        else
            echo "  ✓ $v.mp4  (1920x1080)"
        fi
    fi
done

[[ "$ALL_OK" == "0" ]] && { echo "Aborting — fix issues above first." >&2; exit 1 }

echo ""
echo "=== Deleting old videos ==="
for v in "${OLD_VIDEOS[@]}"; do
    f="$VIDEOS_DIR/${v}.mp4"
    if [[ -f "$f" ]]; then
        rm "$f"
        echo "  deleted: $v.mp4"
    fi
done

# candidates 폴더도 정리 (선택)
if [[ -d "$VIDEOS_DIR/candidates_20260611" ]]; then
    rm -rf "$VIDEOS_DIR/candidates_20260611"
    echo "  deleted: candidates_20260611/"
fi

# 구버전 메타데이터도 업데이트
METADATA_DIR="$VIDEOS_DIR/metadata"
for v in "${OLD_VIDEOS[@]}"; do
    f="$METADATA_DIR/${v}.md"
    [[ -f "$f" ]] && { rm "$f"; echo "  deleted metadata: ${v}.md" }
done

echo ""
echo "Done. Remaining videos:"
ls "$VIDEOS_DIR"/*.mp4 2>/dev/null | while read f; do echo "  $(basename $f)"; done
