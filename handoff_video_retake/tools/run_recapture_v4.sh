#!/usr/bin/env zsh
# v4 재캡처 드라이버 — VideoToolbox HW 인코더로 5개 런 순차 실행.
# nohup으로 띄워서 모니터/세션 종료와 무관하게 끝까지 돈다.
set -uo pipefail

SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h:h}"
TOOLS="$SCRIPT_DIR"
cd "$PROJECT_ROOT"

log() { echo "[recapture_v4] $(date '+%H:%M:%S') $*" }

park_cursor() {
    .venv/bin/python -c "import Quartz; Quartz.CGWarpMouseCursorPosition((1439, 899))" 2>/dev/null || true
}

run_step() {
    local name=$1; shift
    log "=== START $name ==="
    park_cursor
    if "$@"; then
        log "=== OK $name ==="
    else
        log "=== FAILED $name (exit $?) — 다음 단계 계속 ==="
    fi
}

run_step "1/5 FR995 embedded" zsh "$TOOLS/capture_run_embedded.sh" \
    -m "handoff_video_retake/checkpoints/fr995/BossPlayer-995.onnx" \
    -l "FR995" -r "VideoRetakeMac_01_FR995_v4" -n "capture_1_fr995_v4" -e 12

run_step "2/5 99K embedded" zsh "$TOOLS/capture_run_embedded.sh" \
    -m "handoff_video_retake/checkpoints/ppo99k/BossPlayer-99957.onnx" \
    -l "99K" -r "VideoRetakeMac_02_99K_v4" -n "capture_2_99k_v4" -e 12

run_step "3/5 99K trainer" zsh "$TOOLS/capture_run.sh" \
    -r "VideoRetakeMac_03_99K_v4" -n "capture_3_99k_v4" -f "VideoStage_ppo99k" -e 10

run_step "4/5 499K trainer" zsh "$TOOLS/capture_run.sh" \
    -r "VideoRetakeMac_31_499K_v4" -n "capture_31_499k_v4" -f "VideoStage_ppo499k" -e 10

run_step "5/5 1M trainer 45ep" zsh "$TOOLS/capture_run.sh" \
    -r "VideoRetakeMac_45_1M_v4" -n "capture_45_1m_v4" -f "VideoStage_ppo1m" -e 45

log "=== ALL DONE — 에피소드 표 ==="
for cap in capture_1_fr995_v4 capture_2_99k_v4 capture_3_99k_v4 capture_31_499k_v4 capture_45_1m_v4; do
    meta="$TOOLS/raw/${cap}.json"
    [[ -f "$meta" ]] || { echo "(meta 없음: $cap)"; continue }
    log_path=$(python3 -c "import json; print(json.load(open('$meta'))['player_log'])")
    echo "── $cap ──"
    .venv/bin/python "$TOOLS/parse_episodes.py" "$log_path" 2>/dev/null || echo "(parse 실패)"
done
log "RECAPTURE_V4_COMPLETE"
