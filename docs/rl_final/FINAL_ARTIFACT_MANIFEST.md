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
- videos/03_clean_human_like_clear_all_patterns.mp4
- videos/04_clever_valid_policy_behavior.mp4
- videos/05_bug_or_abnormal_case_excluded.mp4
- videos/metadata/01_initial_policy.md
- videos/metadata/02_learning_progression.md
- videos/metadata/03_clean_human_like_clear_all_patterns.md
- videos/metadata/04_clever_valid_policy_behavior.md
- videos/metadata/05_bug_or_abnormal_case_excluded.md
- git 포함 영상은 01부터 05까지 5개만 사용
- video_captures/clips_final은 local 원본 후보이며 최종 git 산출물 아님

## 영상 검토 상태

- 영상 1 초기 정책: 99K checkpoint 실제 gameplay 녹화 완료
- 영상 2 중기 정책: 499K checkpoint 실제 gameplay 녹화 완료
- 영상 3 클린 클리어 후보: 03_ALL_patterns_long_clear_58s.mp4
- 영상 4 패턴 스킵처럼 보이는 버그성 유효 플레이 후보: 04_SOME_patterns_quick_clear_42s.mp4
- 영상 5 이상 행동 후보: 05_SOME_patterns_quick_clear_43s.mp4
- 초기 정책과 중기 정책 영상은 최종 클립을 재라벨링하지 않음
- 영상 1과 영상 2는 Unity build inference gameplay 기반
- 영상 1과 영상 2는 설명 슬라이드 기반 영상 아님

## 검증 상태

- 보고서/PPT/그래프 범위 git diff --check 통과 필요
- PPT slide count 18 확인 필요
- 50MB 이상 신규 산출물 없음 확인 필요
- 기존 Unity meta trailing whitespace는 별도 확인 필요
- legacy 삭제는 삭제 목록 확인 후 진행 필요
