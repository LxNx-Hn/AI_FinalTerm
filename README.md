# CODE BLUE 보스전 PPO 강화학습

## 최종 산출물

- 보고서: `docs/rl_final/PPO_1M_CLEAR_FINAL_REPORT.md`
- 산출물 목록: `docs/rl_final/FINAL_ARTIFACT_MANIFEST.md`
- PPT: `presentations/final/CODE_BLUE_RL_FINAL.pptx`
- PPT 미리보기: `presentations/final/previews`
- PPT 생성 스크립트: `presentations/final/build_code_blue_final_ppt.mjs`
- PPT 이미지 자산: `presentations/final/assets`
- 학습 결과 그래프: `docs/rl_final/figures`

## 발표 영상

- 영상 1 초기 정책: `videos/01_initial_policy.mp4`
- 영상 2 중기 규칙 학습: `videos/02_learning_progression.mp4`
- 영상 3 중기 패턴 학습: `videos/03_mid_pattern_learning.mp4`
- 영상 4 후기 2 3 페이즈: `videos/04_late_phase23_clear.mp4`
- 영상 5 후기 꼼수성 클리어 후보: `videos/05_late_clever_clear.mp4`

## 기준 결과

- run-id: `BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1`
- algorithm: ML-Agents PPO
- action spec: `[5,2]`
- observation size: `438`
- max steps: `1,000,000`
- boss_dead: `199회`
- 전체 클리어율: `22.3%`
- 최근 100 episode 클리어율: `80%`
- 무결성 leak: `0`

## repo 정리 기준

- 최종 제출 문서는 `docs/rl_final`에만 보관
- 내부 작업 문서와 중간 진단 문서는 repo에서 제외
- 최종 발표 영상은 영상 1부터 영상 5까지 위 5개 경로만 사용
