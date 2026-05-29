import os

def main():
    os.makedirs("results/presentation", exist_ok=True)
    ppt_content = """# CODE-BLUE Boss Speedrun RL (Clean Reconstruction)

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

## 4. 모델 훈련 결과 (Natural Discovery)
- Tabular 30k Baseline: 한계 확인 (단순한 피하기 전략에 머뭄).
- DQN 10k: 딥러닝 기반 상태 일반화로 고난도 회피 기동 및 타격 성공.

## 5. 결론 및 향후 과제
- 정확한 Unity 규칙을 반영한 RL 환경에서도 DQN은 매우 빠르고 효율적인 Speedrun 경로를 찾아냄.
- 향후 Unity 연동형 ONNX 실시간 추론 또는 Trace Replay를 통해 실제 게임 엔진에서의 시연 진행.
"""
    with open("results/presentation/presentation.md", "w", encoding='utf-8') as f:
        f.write(ppt_content)
        
    print("Presentation markdown generated in results/presentation/")

if __name__ == "__main__":
    main()
