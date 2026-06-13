# 영상 3-1 실패하지만 전 패턴 노출

- 파일: videos/03-1_fail_all_patterns.mp4
- 캡처: trainer inference (499K, torch), run-id `VideoRetakeMac_31_499K_v4` (macOS, 2026-06-13)
- 원본: handoff_video_retake/tools/raw/capture_31_499k_v4.mkv (2880x1800)
- 에피소드: 9 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `player_dead`
- survival: 165.4초 (macOS trainer 실측 1배속)
- boss_damage: 43 (클리어 직전까지 근접)
- player_hits: 2
- escape: 45/0
- sweep_steps: 1359 (스윕 패턴 대거 노출)
- mark: 1+4 (real+fake, 마크 패턴 5회 — 최다)
- 목적: 보스의 스윕·마크 전 패턴이 등장하나 끝내 클리어하지 못하는 중기 정책
- 화면: 1920x1080 (2880x1800 → 16:9 하단 크롭 2880x1620:0:0 → 스케일), H.264 CRF18, 원속도, 무음, 커서 없음
- 컷: calib 3.663 / pre 0.5s / post -0.5s
- 검증: 시작(이전 에피소드 끝)/끝(사망 직전) 프레임 확인, 하트 UI 보임
