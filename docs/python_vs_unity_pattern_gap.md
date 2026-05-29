# Python vs Unity Pattern Gap Analysis

본 문서는 `C:\Users\KiKi\CODE-BLUE` 의 Unity C# 원본과 `C:\Users\KiKi\Documents\Ql\code_blue_boss_dqn_clean` 의 Python 구현체 간의 1:1 대조 결과입니다.

## 1. 개요
* Unity 원본에서 확인한 주요 패턴 수: 14개
* Random branch(분기) 수: 7개 지점 (Phase 선택, 역방향, Mark가짜 여부 등)
* HP threshold 수: 3개 (70%, 40%, 10%)
* Python에 구현된 패턴 수: 14개 (하지만 세부 로직 오류 다수 존재)
* Python에 미구현인 패턴 수: 0개 (이름은 모두 존재하나 로직이 틀림)

## 2. Blocking Mismatch (심각한 불일치 - 학습 불가)

> [!CAUTION]
> 아래 항목이 수정되지 않은 상태에서 학습된 모델은 Unity 환경에서 절대 정상 동작할 수 없습니다.

1. **Dash 패턴 중 보스 물리적 위치 이동 누락 (Boss Movement Logic)**
   - **Unity**: `PatternEntryDash`, `NStroke`, `Additional1A` 등 모든 Dash 패턴에서 보스의 `bossCell`이 돌진 궤적을 따라 `explicitStart`에서 `explicitEnd`로 물리적으로 이동함.
   - **Python**: 기존 구현에서는 경고/데미지 타일(Warning/Damage cells)만 생성하고, 정작 보스(`boss_logic_cell`)는 제자리에 가만히 머물러 있음.
   - **원인/영향**: 에이전트는 "보스가 돌진 후 어디에 위치하는지" 학습할 수 없으므로, 돌진 직후의 반격 타점을 전혀 잡지 못함. 

2. **Phase 1 고정 패턴(Phase1FirstFixed) 시퀀스 완전 오류**
   - **Unity**: Phase 1 진입 시 첫 패턴은 `Diagonal_TR_BL_3` 돌진 → `Diagonal_TL_BR_3` 돌진 → `AdditionalPattern1(allowC: false)` 로 전개됨.
   - **Python**: 과거 구현에서 이를 단순히 `cast_hollow_corner` 한 번으로 잘못 치환해버렸음.
   - **원인/영향**: 1페이즈 학습 자체가 성립하지 않음.

3. **N-Stroke / Z-Stroke 방향 및 위치 매핑 오류**
   - **Unity**: Z-Stroke의 경우 상단 가장자리에서 우측 돌진, 대각선 좌하단 돌진, 하단 가장자리에서 우측 돌진 등 복합적인 위치 이동이 동반됨.
   - **Python**: 방향 매핑이 부정확하고 보스가 움직이지 않음.

## 3. Major Mismatch (주요 불일치)

> [!WARNING]
> 학습 효율 및 일반화에 악영향을 미치는 항목입니다.

1. **Additional 1A 돌진 방향 오류**
   - **Unity**: 좌측(Up) -> 상단(Right) -> 우측(Down) -> 하단(Left) 순서로 맵을 시계(또는 반시계) 방향으로 훑음.
   - **Python**: 4번 모두 단순히 Down 방향으로만 돌진하게 하드코딩되어 있었음.

2. **Mark Dash (가짜 패턴) 시각화 처리**
   - **Unity**: `hide_boss_visible`이 적용되며 가짜 장판이 먼저 깔리고 실제 데미지는 다른 궤적(Stripe3)으로 들어옴.
   - **Python**: 로직은 어느 정도 유사하나 보스의 숨김 처리 타이밍과 데미지 틱 계산이 1프레임 엇갈림 가능성.

## 4. Minor Mismatch (사소한 불일치)

- **시각적 이펙트(VFX) 딜레이**: Python은 시간 틱(step=0.1s)으로 모든 것을 근사하므로 Unity의 `0.04s` 단위 easing이나 VFX 소멸 프레임이 100% 동일하게 반영되지는 않음. (허용 한계 내)

## 5. 결론 및 향후 계획

사용자님의 지적("결국 다 틀렸다는거네?")은 **정확했습니다**. 
Dash 시 보스 본체가 이동하지 않은 점, Phase 1 고정 패턴을 마음대로 단순화한 점은 치명적(Blocking)인 설계 결함입니다.

**권고 사항 (Recommendation):**
만약 파이썬 환경의 세부 파라미터를 1:1로 맞추고 유지보수하는 것이 너무 비효율적이고 "정 힘들다"고 판단되신다면, 사용자님 말씀대로 **Unity ML-Agents** 또는 **TCP Socket Bridge**를 구축하여 **"게임은 C# 원본을 그대로 실행하고 조작(Action)과 관측(Observation)만 Python이 주고받는 방식"** 으로 아키텍처를 전면 교체하는 것이 가장 확실하고 무결성을 보장하는 방법입니다.

파이썬 환경 수정을 계속 진행할지, 아니면 C# 원본 연동 방식으로 전환할지 결정해 주시면 그에 맞춰 진행하겠습니다.
