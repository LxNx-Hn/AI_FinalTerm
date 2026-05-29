# Unity Boss Pattern Specification

해당 명세는 `ElevatorBossController.cs` 및 `BossPatternCaster.cs`를 기준으로 작성되었습니다.

## 1. Phase Flow 및 HP Threshold

| Phase | HP Threshold | 고정 패턴 (First Fixed) | 이후 반복 (Loop) | Random Branch |
|---|---|---|---|---|
| **Phase 1** | 100% ~ 70% | `Diagonal_TR_BL_3` 돌진 → gap → `Diagonal_TL_BR_3` 돌진 → `AdditionalPattern1` | `PatternEntryDash` → `RunTwoAdditionalPatterns(0~3)` → `LandingSlam` | Pool: 0(1A), 1(1B), 2(N), 3(Z) 중 중복 없이 2개 |
| **Phase 2** | 70% ~ 40% | `Phase2SweepOnly` → gap → `AdditionalPhase2Plus` | `PatternEntryDash` → `RunTwoAdditionalPatterns(4, 6)` → `LandingSlam` | Pool: 6(AdditionalPhase2Plus), 4(Sweep) 중 2개 |
| **Phase 3** | 40% ~ 10% | `MarkDash4` → gap → `RunAdditionalByIndex(4 or 6)` | `PatternEntryDash` → `RunTwoAdditionalPatterns(6, 4, 5)` → `LandingSlam` | Pool: 6(Add2Plus), 4(Sweep), 5(MarkDash4) 중 2개 |
| **Final** | 10% 미만 | (Phase 3 강제 진입 시그널 포함) | `PatternEntryDash` → `FinalPhaseRoutine(HollowCorner 연속 4번)` → `Phase 3 Loop 복귀` | 없음 |

## 2. Detailed Pattern Specs

| 패턴명 | appears_in_phase | source_method | warning_duration | damage_duration | recovery_duration | boss_visible_condition | boss_hurtbox_condition | random_function | selection_rule | next_routine |
|---|---|---|---|---|---|---|---|---|---|---|
| PatternEntryDash | All | `BossPatternCaster.ForwardStripe3FromCell` | 0.70s | 0.15s | 0.65s (gap) | warning 중 노출, damage 후 hide | dash 중 1칸 단위 이동 | 없음 (Targeting 기반) | BossLoop 첫 패턴 | RunTwoAdditional |
| LandingSlam | Phase3/Final | `LandingSlamCoroutine` | 0.64s | 0.15s | 0.5s | 타격 시 렌더러 복구 | 현재 BossCell 타겟 | 없음 (Player tracking) | BossLoop 마지막 패턴 | BasicAttackLoop |
| Additional 1A | Phase1 | `RunAdditionalByIndex(0)` | 0.70s | 0.15s | 0.5s | warning 노출, damage 후 hide | dash 중 1칸 단위 이동 | 없음 | 순차적(Left->Top->Right->Bottom) | Next Pattern |
| Additional 1B | Phase1/2 | `RunAdditionalByIndex(1)` | 0.70s | 0.15s | 0.5s | warning 노출, damage 후 hide | dash 중 1칸 단위 이동 | 없음 | 순차적(Right->Up) | Next Pattern |
| N-Stroke | Phase1/2 | `RunAdditionalByIndex(2)` | 0.70s | 0.15s | 0.05s | warning 노출, damage 후 hide | dash 중 1칸 단위 이동 | `Random.value < 0.5` | 시작 위치 좌우 반전 결정 | Next Pattern |
| Z-Stroke | Phase1/2 | `RunAdditionalByIndex(3)` | 0.70s | 0.15s | 0.05s | warning 노출, damage 후 hide | dash 중 1칸 단위 이동 | `Random.value < 0.5` | 시작 위치 상하 반전 결정 | Next Pattern |
| Phase2Sweep | Phase2/3 | `RunAdditionalByIndex(4)` | 0.70+0.2s | 0.15s | 0.5s | warning 노출, damage 후 hide | dash 중 1칸 단위 이동 | `Random.Range(0,4)` | 시작 인덱스 무작위 선택 | Next Pattern |
| MarkDash | Phase3 | `RunAdditionalByIndex(5)` | 0.68s | 0.15s | 0.60s | Warning 시 hide | 제자리 타격 | `Random.value < 0.5` | Fake 여부 50% 확률 | Next Pattern |

> **Note**: `movement bounds`와 `pattern bounds`는 다를 수 있습니다. 현재 Player 이동 범위는 `y=-4~4`이나, Boss 패턴 장판 최대 범위는 BossPatternCaster Inside 기준 `x=-3~3, y=-3~3`로 제한됩니다. Dash 이동 시 Boss hurtbox는 경로 전체가 아니라 **보스가 현재 지나가는 1칸 단위**로 진행 상황(`t`)에 따라 업데이트됩니다.
