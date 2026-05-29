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

| 패턴명 | appears_in_phase | appears_in_routine | call_order_in_routine | random_candidate_group | candidate_index | excluded_when | parameter_values | next_routine_after_pattern | confidence |
|---|---|---|---|---|---|---|---|---|---|
| PatternEntryDash | All | BossLoop | 1 | None | None | None | None | PhaseCycle | confirmed |
| LandingSlam | Phase3/Final | BossLoop | 5 | None | None | Phase < 3 | None | BasicAttackLoop | confirmed |
| Additional 1A | Phase1 | RunTwoAdditional | 1 | AdditionalPattern1 | 0 | None | None | Next Pattern | confirmed |
| Additional 1B | Phase1/2 | RunTwoAdditional | 1 | AddPattern1 / Phase2Plus | 1 or 0 | None | None | Next Pattern | confirmed |
| N-Stroke | Phase1/2 | RunTwoAdditional | 2 | AddPattern1 / Phase2Plus | 2 or 1 | None | reverse=Random | Next Pattern | confirmed |
| Z-Stroke | Phase1/2 | RunTwoAdditional | 2 | AddPattern1 / Phase2Plus | 3 or 2 | None | reverse=Random | Next Pattern | confirmed |
| Phase2Sweep | Phase2/3 | RunTwoAdditional | 1 | Phase2/3 Pool | Sweep | None | start=Random | Next Pattern | confirmed |
| MarkDash | Phase3 | RunTwoAdditional | 1 | Phase3 Pool | MarkDash4 | None | isFake=Random | Next Pattern | confirmed |
