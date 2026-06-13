# 영상 3 플레이는 가능하지만 클리어 실패

- 파일: videos/03_play_but_fail.mp4
- 캡처: trainer inference (99K, torch), run-id `VideoRetakeMac_03_99K_v4` (macOS, 2026-06-13)
- 원본: handoff_video_retake/tools/raw/capture_3_99k_v4.mkv (2880x1800)
- 에피소드: 2 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `player_dead`
- survival: 153.9초 (macOS trainer 실측 1배속)
- boss_damage: 29 (공격은 하나 클리어 불가)
- player_hits: 2
- escape: 58/2
- sweep_steps: 1392 (보스 스윕 패턴 다수 노출)
- mark: 0+0
- 목적: 공격과 회피를 함께 수행하지만 마무리에는 실패하는 중기 정책
- 화면: 1920x1080 (2880x1800 → 16:9 하단 크롭 2880x1620:0:0 → 스케일), H.264 CRF18, 원속도, 무음, 커서 없음
- 컷: calib 10.788 / pre 0.5s / post -0.5s
- 검증: 시작(이전 에피소드 끝)/끝(사망 직전) 프레임 확인, 하트 UI 보임
