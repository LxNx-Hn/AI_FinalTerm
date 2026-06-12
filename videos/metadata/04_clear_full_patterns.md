# 영상 4 전 패턴을 보여 주는 정상 클리어

- 파일: videos/04_clear_full_patterns.mp4
- 캡처: trainer inference (1M, torch), run-id `VideoRetakeMac_45_1M_v3` (macOS, 2026-06-13, 45 에피소드 단일 런)
- 원본: handoff_video_retake/tools/raw/capture_45_1m.mkv (2880x1800 60fps)
- 에피소드: 5 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `boss_dead`
- survival: 95.4초 (macOS trainer 실측 1배속)
- boss_damage: 59 (클리어)
- player_hits: 2 (피격당하며 싸우는 인간적 플레이)
- escape: 21/1 (회피 동작 다수)
- phase2_sweep_history_active_steps: 528 (기준 ≥150)
- markatk: real 3, fake 1 (기준 real ≥1) — 후반부 마크대쉬 화면 노출 확인
- 목적: 회피와 공격을 모두 수행하면서 후기 패턴까지 보여 주는 대표 클리어
- 화면: 1920x1080 (2880x1800 → 16:9 하단 크롭 → 스케일), H.264 CRF18, 원속도, 무음, 커서 없음
- 컷: calib +4.29s (HP바 리셋 몽타주 실측 3.99 + 안전마진 0.3) / post -0.35s
- 검증: 시작(보스 HP 풀)/끝(보스 HP 0 + 사망 모션) 프레임 육안 확인, 혼입 없음
