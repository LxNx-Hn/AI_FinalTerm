# Python vs Unity Pattern Gap Analysis

| Unity Pattern ID | Method | Python File | Status | Severity | Unity Source | Python Source | Verified | Test File | Notes |
|---|---|---|---|---|---|---|---|---|---|
| PATTERN_ENTRY_DASH | PatternEntryDash | boss_director.py | resolved | blocking | BossPatternCaster.ForwardStripe3FromCell | forward_stripe3_from_cell | yes | test_pattern_entry_dash_path.py | Dash interpolation implemented |
| HOLLOW_CORNER | LandingSlam | boss_director.py | resolved | blocking | LandingSlamCoroutine | hollow_corner_5x5 | yes | test_hollow_corner_pattern.py | Fixed to appear only in Phase 3+ |
| ADDITIONAL_1A | Additional1A | boss_director.py | resolved | major | RunAdditionalByIndex(0) | left_edge2 etc | yes | test_additional_1a_geometry.py | Fixed circle sequence |
| PHASE1_FIRST_FIXED | Phase1FirstFixed | boss_director.py | resolved | blocking | Phase1FirstFixed | phase1_first_fixed | yes | test_phase1_first_fixed_sequence.py | Sequence corrected |
| PHASE2_FIRST_FIXED | Phase2FirstFixed | boss_director.py | resolved | major | Phase2FirstFixed | phase2_first_fixed | yes | test_phase2_sequence.py | Candidate pool corrected |
| MARK_DASH | MarkDash | boss_director.py | resolved | major | RunAdditionalByIndex(5) | mark_horizontal5 | yes | test_mark_dash_visibility_hurtbox.py | Visibility synced |
| NORMAL_SCRATCH | NormalScratchDirectional | boss_patterns.py | resolved | major | NormalScratchDirectional | normal_scratch_directional | yes | test_normal_scratch_range.py | Range set to 0~2 |
| DASH_HURTBOX | CastDashPattern | boss_env.py | resolved | blocking | ElevatorBossController.DashBossAlongArenaLine | boss_logic_cell interpolation | yes | test_dash_hurtbox_current_cell_vs_full_path.py | Hurtbox limited to 1 cell progressing per tick |
| ARENA_BOUNDS | BossPatternCaster.Inside | boss_patterns.py | resolved | blocking | x=-3~3, y=-3~3 | PATTERN_BOUNDS | yes | test_movement_bounds_vs_pattern_bounds.py | Bounds separated from movement |
| RANDOM_FLAG | ElevatorBossController | boss_director.py | resolved | major | Random.Range / Random.value | rng.seed() logic | yes | test_randomized_patterns_flag_reaches_director.py | Deterministic if False, varying if True |
