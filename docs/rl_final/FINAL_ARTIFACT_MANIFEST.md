# 최종 산출물 manifest

## 기준 run

- run-id: BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1
- 기준 commit: 9448e2b
- max steps: 1,000,000
- action spec: [5,2]
- observation size: 438
- final ONNX: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1/BossPlayer.onnx
- final checkpoint: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1/BossPlayer/BossPlayer-1000001.pt

## 보고서

- docs/rl_final/PPO_1M_CLEAR_FINAL_REPORT.md
- 한국어 개조식
- 마침표 없는 형식
- 10개 장 구성 반영

## PPT

- presentations/final/CODE_BLUE_RL_FINAL.pptx
- slide count 18
- previews: presentations/final/previews
- 생성 스크립트: presentations/final/build_code_blue_final_ppt.mjs
- 게임 소개 이미지 포함: presentations/final/assets/source_images/game_warning_tile.png
- 패턴 소개 이미지 포함: presentations/final/assets/source_images/boss_close_range.png
- 영상 썸네일 포함: presentations/final/assets/video_thumbnails
- 마침표 없는 개조식 본문으로 재생성
- 게임 소개 및 패턴 소개 사진 포함
- MDP 상태공간 정의 포함
- PPO 기법 소개 포함
- 보상 설계 및 학습 설계 포함
- 학습 결과 그래프 포함

## 학습 추이 그래프

- docs/rl_final/figures/boss_damage_by_step.png
- docs/rl_final/figures/clear_rate_by_step.png
- docs/rl_final/figures/reward_by_step.png
- docs/rl_final/figures/hit_rate_by_step.png
- docs/rl_final/figures/in_range_by_step.png
- docs/rl_final/figures/clear_time_distribution.png
- docs/rl_final/figures/phase_milestone_timeline.png
- docs/rl_final/figures/integrity_leak_table.png
- docs/rl_final/figures/integrity_leak_table.md
- docs/rl_final/figures/figure_generation_summary.json
- presentations/final/assets에 동일 PNG 복사 완료

## 영상 후보

- videos/01_initial_policy.mp4
- videos/02_learning_progression.mp4
- videos/03_mid_pattern_learning.mp4
- videos/04_late_phase23_clear.mp4
- videos/05_late_clever_clear.mp4
- videos/metadata/01_initial_policy.md
- videos/metadata/02_learning_progression.md
- videos/metadata/03_mid_pattern_learning.md
- videos/metadata/04_late_phase23_clear.md
- videos/metadata/05_late_clever_clear.md
- git 포함 영상은 01부터 05까지 5개만 사용

## 영상 검토 상태

- 영상 1 초기 정책: 99K checkpoint 실제 gameplay 녹화 완료
- 영상 2 중기 규칙 학습: 499K checkpoint 실제 gameplay 녹화 완료
- 영상 3 중기 패턴 학습: 799K checkpoint 실제 gameplay 본캡처 완료
- 영상 3 로그 근거: results/VideoGameplay_PPO799K_mid_pattern_final_capture/run_logs/Player-0.log
- 영상 3 결과: player_dead, survival 69.5초, boss_damage 8, phase2 sweep history 653 step
- 영상 4 후기 2 3 페이즈: 04_late_phase23_clear.mp4 사용
- 영상 5 후기 꼼수성 클리어: 05_late_clever_clear.mp4 사용
- 초기 정책과 중기 정책 영상은 최종 클립을 재라벨링하지 않음
- 영상 1, 영상 2, 영상 3은 Unity build inference gameplay 기반
- 영상 1, 영상 2, 영상 3은 설명 슬라이드 기반 영상 아님
- 후기 clip과 episode log 직접 매핑은 미확정
- 영상 4와 영상 5는 exploit 확정 표현 없이 후보로 설명

## 검증 상태

- 보고서/PPT/그래프 범위 git diff --check 통과 필요
- PPT slide count 18 확인 완료
- 50MB 이상 신규 산출물 없음 확인 필요
- 기존 Unity meta trailing whitespace는 별도 확인 필요
- legacy 삭제는 삭제 목록 확인 후 진행 필요
