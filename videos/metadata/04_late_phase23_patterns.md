# 영상 4 후기 2 3 페이즈 패턴 노출

- 상태: 생성 완료
- 파일명: videos/04_late_phase23_patterns.mp4
- 영상 목적: 후기 전환부에서 Phase2 sweep과 위험 타일 패턴이 화면에 잘 보이는 구간 제시
- 영상 성격: 클리어 영상 아님
- 원본 소스: videos/03_mid_pattern_learning.mp4 18초부터 48초 추출
- 사용 checkpoint: BossPlayer-799922.onnx
- 근거 로그: results/VideoGameplay_PPO799K_mid_pattern_final_capture/run_logs/Player-0.log
- episode 결과: player_dead
- survival: 69.5초
- boss_damage: 8
- phase 근거: phase2_sweep_history_active_steps=653
- visual 검증: 접촉 시트에서 diagonal warning tile, sweep smoke, boss 이동, 회피 실패 흐름 확인
- 발표 표기: 후기 2 3 페이즈 패턴 노출 또는 후기 phase2 sweep 대응
- 금지 표기: clear, boss_dead, 최종 클리어
