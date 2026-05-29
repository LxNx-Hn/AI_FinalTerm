# Unity Boss Phase Flow

| Phase | HP Condition | Routine Name | Fixed Sequence | Random Candidate Group | Exclusions | Repeat Count | Next Routine | Source Evidence |
|---|---|---|---|---|---|---|---|---|
| **Phase 1** | 100% ~ 70% | Phase1FirstFixed | Diagonal_TR_BL_3 -> Diagonal_TL_BR_3 -> AdditionalPattern1 | None | None | 1 | BossLoop | ElevatorBossController.cs:Phase1FirstFixed |
| **Phase 1 Loop** | 100% ~ 70% | BossLoop | PatternEntryDash -> LandingSlam | Pool 0, 1, 2, 3 | 중복 픽 불가 | Loop | BossLoop | ElevatorBossController.cs:BossLoop |
| **Phase 2** | 70% ~ 40% | Phase2FirstFixed | Phase2SweepOnly -> AdditionalPhase2Plus | None | None | 1 | BossLoop | ElevatorBossController.cs:Phase2FirstFixed |
| **Phase 2 Loop** | 70% ~ 40% | BossLoop | PatternEntryDash -> LandingSlam | Pool 6, 4 | 중복 픽 불가 | Loop | BossLoop | ElevatorBossController.cs:BossLoop |
| **Phase 3** | 40% ~ 10% | Phase3FirstFixed | MarkDash4 -> RunAdditionalByIndex(4 or 6) | None | None | 1 | BossLoop | ElevatorBossController.cs:Phase3FirstFixed |
| **Phase 3 Loop** | 40% ~ 10% | BossLoop | PatternEntryDash -> LandingSlam | Pool 6, 4, 5 | 중복 픽 불가 | Loop | BossLoop | ElevatorBossController.cs:BossLoop |
| **Final** | < 10% | FinalPhaseRoutine | PatternEntryDash -> HollowCorner (4회 연속) | None | None | 1 (진입 시 1회만) | BossLoop (Phase 3 Pool) | ElevatorBossController.cs:FinalPhaseRoutine |
