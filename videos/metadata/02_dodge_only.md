# 영상 2 회피는 하지만 공격은 거의 없는 정책

- 파일: videos/02_dodge_only.mp4
- 캡처: embedded Eval210 (99K ONNX), run-id `VideoRetakeMac_02_99K_v4` (macOS, 2026-06-13)
- 원본: handoff_video_retake/tools/raw/capture_2_99k_v4.mkv (2880x1800)
- 에피소드: 2 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `player_dead`
- survival: 86.7초
- boss_damage: 5 (공격 시도는 있으나 극히 적음)
- player_hits: 2
- escape: 66/2 (회피 위주 플레이)
- 목적: 회피와 생존은 가능하지만 적극적인 공격과 클리어는 아직 어려운 단계
- 화면: 1920x1080 (2880x1800 → 하단 크롭 2880x1620:0:0 → 스케일), H.264 CRF18, 60fps CFR, 원속도, 무음, 커서 없음
- 컷: calib -1.749 / pre 0.5s / post 1.5s (사망 장면 포함)
- 컬러: full-range (pc), bt709
- 검증: 시작(보스 HP 풀)/끝(사망 장면) 프레임 육안 확인, 하트 UI 보임
