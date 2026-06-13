# 영상 1 회피도 못 하는 초기 정책

- 파일: videos/01_no_dodge.mp4
- 캡처: embedded Eval210 (FR995 ONNX), run-id `VideoRetakeMac_01_FR995_v4` (macOS, 2026-06-13)
- 원본: handoff_video_retake/tools/raw/capture_1_fr995_v4.mkv (2880x1800)
- 에피소드: 2 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `player_dead`
- survival: 19.2초
- boss_damage: 0 (공격 없음)
- player_hits: 2
- escape: 4/0
- 목적: 경고 타일과 근접 압박에 거의 대응하지 못하는 초기 정책. 공격도 회피도 없이 무력하게 사망
- 화면: 1920x1080 (2880x1800 → 하단 크롭 2880x1620:0:0 → 스케일), H.264 CRF18, 60fps CFR, 원속도, 무음, 커서 없음
- 컷: calib -1.737 / pre 0.5s / post 1.5s (사망 장면 포함)
- 컬러: full-range (pc), bt709
- 검증: 시작(보스 HP 풀)/끝(사망 장면) 프레임 육안 확인, 하트 UI y=0에서 보임
