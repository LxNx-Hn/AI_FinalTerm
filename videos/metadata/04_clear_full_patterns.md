# 영상 4 전 패턴을 보여 주는 정상 클리어

- 파일: videos/04_clear_full_patterns.mp4
- 캡처: trainer inference (1M, torch), run-id `VideoRetakeMac_45_1M_v4` (macOS, 2026-06-13, 45 에피소드 단일 런)
- 원본: handoff_video_retake/tools/raw/capture_45_1m_v4.mkv (2880x1800)
- 에피소드: 25 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `boss_dead`
- survival: 98.6초 (macOS trainer 실측 1배속)
- boss_damage: 59 (클리어)
- player_hits: 2
- escape: 20/1 (회피 다수)
- sweep_steps: 527 (전 캡처 런 boss_dead 에피소드 중 최다)
- mark: 2+2 (real+fake, 마크 패턴 양쪽 노출)
- 목적: 회피와 공격을 모두 수행하면서 스윕·마크 패턴까지 보여 주는 대표 클리어
- 화면: 1920x1080 (2880x1800 → 하단 크롭 2880x1620:0:0 → 스케일), H.264 CRF18, 60fps CFR, 원속도, 무음, 커서 없음
- 컷: calib 3.673 / pre 5.0s / post 10.0s (보스 사망 장면 여유 포함)
- 컬러: full-range (pc), bt709, gamma 1.1 (Mac → Windows 밝기 보정)
- 검증: 시작(이전 에피소드 포함)/끝(보스 사망 장면 포함) 여유 확인
