# 영상 5 후기 꼼수성 클리어

- 상태: 생성 완료
- 파일명: videos/05_late_clever_clear.mp4
- 영상 목적: 보스에 붙어서 연속 공격하는 꼼수성 클리어 흐름 제시
- 영상 성격: 실제 1M PPO inference 플레이 녹화
- 캡처 방식: 데스크톱 캡처, draw_mouse=0, Windows 업데이트 팝업 제거 후 녹화
- 사용 checkpoint: BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1
- 근거 로그: results/VideoGameplay_PPO1M_clever_clear_desktop_20260611105711/run_logs/Player-0.log
- episode 결과: boss_dead
- survival: 61.2초
- boss_damage: 60
- target_alignment: logs=59, hidden_boss_attack_reward_count=0
- sweep_history_observation: phase2_sweep_history_active_steps=180
- diagonal_blindspot_wiggle_diag: diagonal_blindspot_bosscell_steps=313
- visual 검증: 접촉 시트에서 실제 전투, 근접 연속 공격, 경고 타일, 클리어 직전 HP 감소 확인
- 발표 표기: 후기 꼼수성 클리어 또는 diagonal blindspot 활용처럼 보이는 클리어
- 금지 표기: 엔진 버그 확정, leak 확정
