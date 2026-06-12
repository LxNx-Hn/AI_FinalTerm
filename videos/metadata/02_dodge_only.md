# 영상 2 회피는 하지만 공격은 거의 없는 정책

- 파일: videos/02_dodge_only.mp4
- 캡처: embedded Eval210 (99K ONNX), run-id `VideoRetakeMac_02_99K_v3` (macOS, 2026-06-12)
- 원본: handoff_video_retake/tools/raw/capture_2_99k.mkv (2880x1800 60fps)
- 에피소드: 17 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `player_dead`
- survival: 78.6초
- boss_damage: 10 (기준 ≤12)
- player_hits: 2
- escape: 32/2 (기준 ≥15)
- 목적: 회피와 생존은 가능하지만 적극적인 공격과 클리어는 아직 어려운 단계
- 화면: 1920x1080 (2880x1800 → 16:9 하단 크롭 → 스케일), H.264 CRF18, 원속도, 무음
- 커서: 원본에 고정 커서가 찍혀 delogo(x=1205,y=1430,70x75)로 제거, 출력 무커서 확인
- 컷: calib +0.3s / post -0.35s — 이전/다음 에피소드 혼입 없음
- 검증: 시작/중간/끝 프레임 육안 확인, 하트 UI 보임
