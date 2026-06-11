# 영상 2 중기 규칙 학습

- 상태: 생성 완료
- 파일명: videos/02_learning_progression.mp4
- 영상 목적: 중기 정책이 경고 타일과 거리 규칙을 일부 학습한 모습을 제시
- 영상 성격: 499K checkpoint 실제 gameplay 녹화
- 사용 checkpoint: BossPlayer-499996.onnx
- build label: PPO499K_mid_rule_dynamic
- build log: logs/build_eval_210_PPO499K_mid_rule_dynamic.log
- gameplay log: results/VideoGameplay_PPO499K_mid_rule_dynamic_20260611/run_logs/Player-0.log
- 캡처 방식: Unity Eval210 build desktop capture, draw_mouse=0
- 길이: 45초
- boss_dead 여부: false
- visual 검증: 접촉 시트에서 warning tile 회피, boss 이동, safe opportunity 판단, 일부 attack 흐름 확인
- 발표 표기: 중기 규칙 학습 또는 경고 타일 회피 학습
- 금지 표기: 최종 정책 클리어, 패턴 완전 숙련
