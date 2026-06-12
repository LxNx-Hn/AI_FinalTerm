# CODE BLUE 보스전 PPO 강화학습

## 최종 산출물

- 보고서: `docs/rl_final/PPO_1M_CLEAR_FINAL_REPORT.md`
- 산출물 목록: `docs/rl_final/FINAL_ARTIFACT_MANIFEST.md`
- PPT: `presentations/final/CODE_BLUE_RL_FINAL.pptx`
- PPT 미리보기: `presentations/final/previews`
- PPT 생성 스크립트: `presentations/final/build_code_blue_final_ppt.mjs`
- PPT 이미지 자산: `presentations/final/assets`
- 학습 결과 그래프: `docs/rl_final/figures`

## 발표 영상 (2026-06-13 macOS 재녹화)

- 영상 1 회피도 못 하는 초기 정책: `videos/01_no_dodge.mp4`
- 영상 2 회피만 하는 정책: `videos/02_dodge_only.mp4`
- 영상 3 플레이 가능하지만 클리어 실패: `videos/03_play_but_fail.mp4`
- 영상 3-1 실패하지만 전 패턴 노출: `videos/03-1_fail_all_patterns.mp4`
- 영상 4 전 패턴을 보여 주는 정상 클리어: `videos/04_clear_full_patterns.mp4`
- 영상 5 최단 클리어: `videos/05_fastest_clear.mp4`

전 편 1920x1080, 원속도, 무음, H.264 CRF18. 편당 선정 근거는 `videos/metadata/` 참고.

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
- 최종 발표 영상은 영상 1, 2, 3, 3-1, 4, 5 위 6개 경로만 사용
