# Unity Boss Phase Flow

본 문서는 `ElevatorBossController.cs`에 정의된 보스전의 전체 페이즈 흐름과 각 패턴의 출현 조건을 분석한 결과입니다.

## 1. 전체 루프 구조 (Boss Loop)

보스전은 기본적으로 아래의 무한 루프로 동작합니다 (HP가 0이 되면 종료):
1. **BasicAttackLoop**: 플레이어를 추적하며 기본 평타(`NormalScratch` 또는 Phase 3 이상부터는 `EnhancedScratch` 포함)를 N회 반복 (Phase 1, 2는 5회, Phase 3 이상은 3회)
2. **EvaluatePhase**: 현재 HP를 확인하여 Phase 전환 여부 결정.
3. **PatternEntryDash**: 보스가 현재 바라보는 방향으로 무한 직선 돌진 (무조건 실행됨).
4. **Phase Cycle**: 현재 Phase에 해당하는 특정 시퀀스 실행.
   - 처음 진입한 Phase라면 **FirstFixed** 시퀀스 우선 실행.
   - 두 번째 이상이라면 **RunTwoAdditionalPatterns** (후보군 중 랜덤 2개) 실행.
   - 단, Final Phase는 매 루프마다 **FinalPhaseRoutine** 실행.
5. **LandingSlam**: 플레이어 위치로 텔레포트하며 찍기 공격. (Phase 1, 2는 3칸 평타 범위, Phase 3 이상은 5x5 Hollow Corner 패턴)

## 2. 페이즈별 세부 전개 (Phase Cycle)

### Phase 1 (100% ~ 70%)
- **Phase1FirstFixed** (최초 1회)
  - `Diagonal_TR_BL_3` (대각선 돌진) -> `Diagonal_TL_BR_3` (대각선 돌진) -> `AdditionalPattern1(allowC: false)` (즉, 1-A 또는 1-B 중 랜덤 1개)
- **RunTwoAdditionalPatterns (Phase 1)** (이후 루프)
  - 후보 풀: `[0(1-A), 1(1-B), 2(N-Stroke), 3(Z-Stroke)]`
  - 위 4개 중 랜덤으로 중복 없이 2개를 뽑아 순차 실행.

### Phase 2 (70% ~ 40%)
- **Phase2FirstFixed** (최초 1회)
  - `Phase2SweepOnly` (가장자리 4방향 쓸기) -> `AdditionalPhase2Plus` (즉, 1-B, N-Stroke, Z-Stroke 중 랜덤 1개)
- **RunTwoAdditionalPatterns (Phase 2)** (이후 루프)
  - 후보 풀: `[6(AdditionalPhase2Plus), 4(Sweep)]`
  - 위 2개를 순서만 랜덤으로 섞어 모두 실행.

### Phase 3 (40% ~ 10%)
- **Phase3FirstFixed** (최초 1회)
  - `MarkDash4` (십자 가짜/진짜 장판 4연속) -> `RunAdditionalByIndex(4 or 6)` (Sweep 또는 AdditionalPhase2Plus 중 랜덤 1개)
- **RunTwoAdditionalPatterns (Phase 3)** (이후 루프)
  - 후보 풀: `[6(AdditionalPhase2Plus), 4(Sweep), 5(MarkDash4)]`
  - 위 3개 중 랜덤으로 중복 없이 2개를 뽑아 순차 실행.

### Final Phase (10% 미만)
- **FinalPhaseRoutine** (매 루프)
  - 플레이어 위치를 향해 `HollowCorner5x5` 장판 공격을 4연속 실행.
  - 이후 `FinalRandomNoWarningDash` 또는 `FinalOriginalDashAfterSlam`(Stripe3 돌진) 1회 실행.
  - 완료 후 즉시 **Phase 3** 로 상태가 변경되어 다음 루프부터는 Phase 3 로직을 따름.

## 3. Hollow Corner 패턴의 정확한 등장 조건

**"Hollow Corner"는 독립된 추가 패턴(Additional Pattern) 후보군이 아닙니다.**
오직 다음 두 가지 상황에서만 특정 타겟(플레이어 위치)을 향해 발동됩니다:
1. **LandingSlam (강화 버전)**: Phase Cycle이 끝나고 다음 Basic Loop로 넘어가기 직전 실행되는 착지 공격에서, **현재 Phase가 3 이상일 때만** 5x5 Hollow Corner가 사용됩니다. (Phase 1, 2일 때는 일반적인 전방 3칸 긁기 범위가 사용됩니다.)
2. **FinalPhaseRoutine**: HP 10% 미만 진입 시, 플레이어를 쫓아다니며 연속 4번 발동하는 찍기 공격으로 사용됩니다.

> [!IMPORTANT]
> **Python 구현체의 오류 발견**:
> 기존 파이썬 구현에서는 `Phase1FirstFixed`의 시작 패턴으로 `cast_hollow_corner`를 맵핑하고, 보스 중앙 좌표 `(0,0)`에 하드코딩하여 사용했습니다. 이는 Unity 원본 명세와 완전히 틀린 것이며, Hollow Corner는 Phase 1에 절대 등장하지 않습니다.
