# 영상 5 최단 클리어

- 파일: videos/05_fastest_clear.mp4
- 캡처: trainer inference (1M, torch), run-id `VideoRetakeMac_45_1M_v4` (macOS, 2026-06-13, 영상 4와 같은 런)
- 원본: handoff_video_retake/tools/raw/capture_45_1m_v4.mkv (2880x1800)
- 에피소드: 43 (캡처 런 자체 Player-0.log 기준 — 45 에피소드 중 실제 최단 클리어)
- 결과: `boss_dead`
- survival: 42.8초 (macOS trainer 실측 1배속)
- boss_damage: 59 (클리어)
- player_hits: 0 (무피격)
- escape: 1/0
- sweep_steps: 22 (패턴 전개 전 속전속결)
- mark: 0+0
- 목적: 이번 45 에피소드 캡처 런에서 가장 빠르게 끝난 실제 클리어 사례
- 화면: 1920x1080 (2880x1800 → 하단 크롭 2880x1620:0:0 → 스케일), H.264 CRF18, 60fps CFR, 원속도, 무음, 커서 없음
- 컷: calib 3.673 / pre 5.0s / post 10.0s (보스 사망 장면 여유 포함)
- 컬러: full-range (pc), bt709, gamma 1.1 (Mac → Windows 밝기 보정)
- 검증: 시작(이전 에피소드 포함)/끝(보스 사망 장면 포함) 여유 확인
