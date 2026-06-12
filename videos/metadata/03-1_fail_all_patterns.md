# 영상 3-1 실패하지만 주요 패턴을 모두 노출

- 파일: videos/03-1_fail_all_patterns.mp4
- 캡처: trainer inference (499K, torch), run-id `VideoRetakeMac_31_499K_v3` (macOS, 2026-06-12)
- 원본: handoff_video_retake/tools/raw/capture_31_499k.mkv (2880x1800 60fps, 6 에피소드 시점 중단본)
- 에피소드: 3 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `player_dead`
- survival: 171.8초 (macOS trainer 실측 1배속)
- boss_damage: 42 (보스 HP 18까지)
- player_hits: 2
- escape: 51/0
- phase2_sweep_history_active_steps: 1386 (기준 ≥1000)
- markatk: real 2, fake 3 (기준 real ≥2) — t≈140s 부근 마크대쉬 화면 노출 프레임 확인
- 목적: 사망으로 끝나지만 4칸 쓸기와 마크 대시를 모두 노출하는 실패 사례
- 화면: 1920x1080 (2880x1800 → 16:9 하단 크롭 → 스케일), H.264 CRF18, 원속도, 무음, 커서 없음
- 컷: calib +6.15s (HP바 리셋 몽타주 실측 5.85 + 안전마진 0.3) / post -0.35s
- 검증: 시작/끝 프레임 육안 확인, 혼입 없음, 하트 UI 보임, 마크대쉬 가시성 확인
