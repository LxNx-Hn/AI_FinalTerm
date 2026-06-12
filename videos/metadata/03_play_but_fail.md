# 영상 3 플레이는 가능하지만 클리어 실패

- 파일: videos/03_play_but_fail.mp4
- 캡처: trainer inference (99K, torch), run-id `VideoRetakeMac_03_99K_v3` (macOS, 2026-06-12)
- 원본: handoff_video_retake/tools/raw/capture_3_99k.mkv (2880x1800 60fps)
- 에피소드: 6 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `player_dead`
- survival: 102.9초 (macOS trainer는 실측 1배속 — 영상 길이 ≈ 게임타임)
- boss_damage: 18 (기준 18~25)
- player_hits: 2
- escape: 39/2
- phase2_sweep_history_active_steps: 928 (페이즈2 진입)
- mark: 0+0 (마크 미노출 — 3번 프로필)
- 목적: 공격과 회피를 함께 수행하지만 마무리에는 실패하는 중기 정책
- 화면: 1920x1080 (2880x1800 → 16:9 하단 크롭 → 스케일), H.264 CRF18, 원속도, 무음, 커서 없음
- 컷: calib -0.45s (HP바 리셋 몽타주 실측 -0.75 + 안전마진 0.3) / post -0.35s
- 검증: 시작(보스 HP 풀)/끝(사망) 프레임 육안 확인, 혼입 없음, 하트 UI 보임
