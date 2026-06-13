# 영상 3 플레이는 가능하지만 클리어 실패

- 파일: videos/03_play_but_fail.mp4
- 캡처: trainer inference (99K, torch), run-id `VideoRetakeMac_03_99K_v4` (macOS, 2026-06-13)
- 원본: handoff_video_retake/tools/raw/capture_3_99k_v4.mkv (2880x1800)
- 에피소드: 10 (ep2 원본 1.316s 프레임 갭으로 끊김 → ep10으로 교체)
- 결과: `player_dead`
- survival: 194.3초 (macOS trainer 실측 1배속)
- boss_damage: 26 (공격은 하나 클리어 불가)
- player_hits: 2
- escape: 79/1
- sweep_steps: 1782 (보스 스윕 패턴 다수 노출)
- mark: 0+0
- 목적: 공격과 회피를 함께 수행하지만 마무리에는 실패하는 중기 정책
- 화면: 1920x1080 (2880x1800 → 하단 크롭 2880x1620:0:0 → 스케일), H.264 CRF18, 60fps CFR, 원속도, 무음, 커서 없음
- 컷: calib 10.788 / pre 5.0s / post 10.0s (사망 장면 여유 포함)
- 컬러: full-range (pc), bt709, gamma 1.1 (Mac → Windows 밝기 보정)
- 검증: 시작(이전 에피소드 포함)/끝(사망 장면 포함) 여유 확인
