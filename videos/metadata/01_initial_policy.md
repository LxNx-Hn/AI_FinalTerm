# 영상 1 학습 초기 정책

- 상태: 생성 완료
- 파일명: videos/01_initial_policy.mp4
- 영상 목적: 학습 초기에 이동과 회피가 불안정하지만 실제 정책이 행동하는 모습 제시
- 영상 성격: 199K checkpoint 실제 gameplay 녹화
- 사용 checkpoint: BossPlayer-199919.onnx
- build label: PPO199K_initial_dynamic
- build log: logs/build_eval_210_PPO199K_initial_dynamic.log
- gameplay log: results/VideoGameplay_PPO199K_initial_dynamic_20260611/run_logs/Player-0.log
- 캡처 방식: Unity Eval210 build desktop capture, draw_mouse=0
- 길이: 45초
- boss_dead 여부: false
- visual 검증: 접촉 시트에서 player 이동, warning tile, boss 이동, 일부 attack/dash 흐름 확인
- 발표 표기: 학습 초기 정책 또는 초기 실패/불안정 행동
- 금지 표기: 최종 정책 클리어, 설명 슬라이드 대체 영상
