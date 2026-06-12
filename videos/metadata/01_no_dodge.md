# 영상 1 회피도 못 하는 초기 정책

- 파일: videos/01_no_dodge.mp4
- 캡처: embedded Eval210 (FR995 ONNX), run-id `VideoRetakeMac_01_FR995_v3` (macOS, 2026-06-12)
- 원본: handoff_video_retake/tools/raw/capture_1_fr995.mkv (2880x1800 60fps)
- 에피소드: 11 (캡처 런 자체 Player-0.log 기준 선정)
- 결과: `player_dead`
- survival: 20.0초
- boss_damage: 0
- player_hits: 2
- escape: 4/2
- attack: 0/0 (공격 시도 없음)
- 목적: 경고 타일과 근접 압박에 거의 대응하지 못하는 초기 정책. 공격도 회피도 없이 무력하게 사망
- 화면: 1920x1080 (2880x1800 → 16:9 하단 크롭 2880x1620 → 스케일), H.264 CRF18, 원속도, 무음, 커서 없음
- 컷: calib +0.3s / post -0.35s — 이전/다음 에피소드 혼입 없음
- 검증: 시작(보스 HP 풀·스폰 위치)/끝(사망 연기) 프레임 육안 확인, 하트 UI 보임
