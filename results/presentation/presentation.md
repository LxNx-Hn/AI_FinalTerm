# CODE-BLUE Boss Speedrun RL (Clean Reconstruction)

## 1. 개요
- 이전 실험의 환경 한계(단순화된 보스 패턴 및 추적 로직)를 극복하기 위해 Unity 원본 규칙을 그대로 파이썬에 이식한 "Clean Reconstruction" 프로젝트.

## 2. 이전 실험 결과와 재구현 사유
- Tabular Q-learning은 복잡한 상태 공간에서 일반화 한계를 보임.
- DQN 10k 결과는 우수했으나, 환경이 너무 쉬웠던(꼼수가 가능한) 문제 발견.
- Unity 원본 기준: 무한 루프 방식의 동적 추적, 렌더러 OFF시에도 유지되는 피격 판정 등 엄격한 룰 재구현.

## 3. MDP 정의
- State: Player (x, y, hp, facing, cooldowns), Boss (visible, x, y, hp), Spatial Maps (warning, damage, hurtbox)
- Action: NONE, UP, DOWN, LEFT, RIGHT, ATTACK, UP_ATTACK, DOWN_ATTACK, LEFT_ATTACK, RIGHT_ATTACK (동시 처리 지원)
- Reward: 빠른 클리어를 위한 데미지 보상과 시간 페널티, 각종 피격 페널티.

## 4. 모델 훈련 및 평가 결과
- Tabular 30k Baseline: 한계 확인 (단순한 회피 전략에 머뭄).
- DQN 10k (Deterministic): 보스 패턴이 고정된 상태에서는 100전 100승, 29.6초 스피드런 궤적 통째 암기 성공.
- DQN 10k (Randomized Generalization): 무작위 패턴(Phase 2, 3, Fake Dash 등) 활성화 시 승률 0%, 평균 클리어 타임 0.0초, 데미지 1.98. 단일 시드 훈련의 과적합(Overfitting) 문제 극명하게 확인.

## 5. 결론 및 향후 과제
- 정확한 Unity 규칙을 반영한 RL 환경에서, 현재 모델은 결정론적 환경에는 완벽히 적응했으나 무작위 패턴 일반화에는 처참히 실패함.
- 향후 과제: 무작위 보스 패턴(`--randomize-boss-patterns`) 환경 하에서 처음부터 다시 훈련하여, 진짜 100% 클리어 에이전트 달성.
