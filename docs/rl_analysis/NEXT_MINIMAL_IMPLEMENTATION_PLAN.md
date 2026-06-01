# Next Minimal Implementation Plan

작성 시각: 2026-06-01 KST

기준 문서:

- `docs/rl_analysis/BOSS_PATTERN_MDP_ANALYSIS.md`
- `docs/rl_analysis/MARKATK_OBSERVATION_DESIGN.md`

이번 문서는 구현 계획이다. 코드 수정, build 실행, PPO 학습 실행은 하지 않는다.

## 공통 금지 사항

- `current_pattern` enum observation 추가 금지.
- `next_band` observation 추가 금지.
- `sweep_sequence_index` observation 추가 금지.
- hidden hurtbox active observation 추가 금지.
- reward 변경 금지.
- PlayerCombat 판정 변경 금지.
- PPO 학습 실행 금지.
- build 실행 금지.

## 현재 기준

- Arena grid: 7x7.
- Mask cell count `N = 49`.
- 현재 `BossRLStateExtractor.VectorObservationSize = 193`.
- 현재 포함 mask: current warning `+49`, current damage `+49`, previous warning `+49`.
- 기존 ONNX/checkpoint는 observation size 변경 시 그대로 호환되지 않는다.

## Task 1. MarkATKVFX / MarkATKVFX_Fake Visible Cue Registry

### 목적

MarkDash에서 화면에 보이는 red/blue MarkATK VFX를 observation source로 등록한다. 이는 internal pattern 정답이 아니라 visible cue encoding이다.

### 수정할 파일

- `unity_project/Assets/Project/Scripts/Boss/ElevatorBossController.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLStateExtractor.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
- 필요 시 신규 파일: `unity_project/Assets/Project/Scripts/RL/BossRLVisibleCueRegistry.cs`
- 필요 시 scene: `unity_project/Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity`

### 구현 단위

1. `SpawnMarkDashVfx()`에서 spawned VFX의 visible center cell, display orientation, real/fake visible type을 registry에 등록한다.
2. VFX lifetime 종료 또는 object destroy 시 registry에서 제거한다.
3. `BossRLStateExtractor`는 registry를 읽어 mask를 만든다.
4. fake marker는 danger mask에 넣지 않고 별도 visible cue channel로만 제공한다.

### 새 observation channel

최소안:

- `markatk_real_visible_mask`: `+49`
- `markatk_fake_visible_mask`: `+49`

권장안:

- `markatk_real_visible_mask`: `+49`
- `markatk_fake_visible_mask`: `+49`
- `recent_markatk_real_mask`: `+49`

보류:

- `recent_markatk_fake_mask`: fake memory가 실제로 필요한지 diagnostic 후 결정.
- `markatk_actual_damage_stripe_mask`: damage 전 정답 유출 위험이 있어 금지 또는 매우 주의.

### VectorObservationSize 증가량

- 최소안: `+2N = +98`, `193 -> 291`.
- 권장안: `+3N = +147`, `193 -> 340`.

### BehaviorParameters 변경 필요 여부

필요하다. Vector observation size를 291 또는 340으로 맞춰야 한다.

### 기존 checkpoint/ONNX 호환 여부

호환되지 않는다. observation input shape가 바뀌므로 기존 ONNX/checkpoint는 그대로 inference/resume에 사용할 수 없다. 새 observation spec용 fresh run 또는 별도 initialize 전략이 필요하다.

### Diagnostic metric

- `markatk_real_visible_count`
- `markatk_fake_visible_count`
- `markatk_real_visible_cell_count`
- `markatk_fake_visible_cell_count`
- `markatk_registry_active_count`
- `markatk_registry_stale_count`
- `fake_marker_in_danger_mask_count` must be 0
- `real_marker_in_observation_count`
- `fake_marker_in_observation_count`
- `markatk_visible_during_markdash_count`

### 5K 검증 기준

5K diagnostic run을 실행하는 단계에서 확인한다. 이 계획 작성 단계에서는 실행하지 않는다.

- Compile 성공.
- BehaviorParameters vector size와 `VectorObservationSize` 일치.
- NaN/exception/crash 없음.
- MarkDash가 발생한 episode에서 `markatk_real_visible_count + markatk_fake_visible_count > 0`.
- `fake_marker_in_danger_mask_count = 0`.
- `attack_out_of_range = 0` 유지.
- terminal reward `player_dead=-8`, `boss_dead=+5` 유지.

### 실패 시 rollback 기준

- fake marker가 movement hard mask에 들어가면 rollback.
- registry stale object가 남아 episode 이후 observation에 marker가 계속 남으면 rollback.
- MarkDash가 발생해도 real/fake visible count가 0이면 rollback.
- observation size 불일치로 ML-Agents shape error가 나면 rollback.
- 기존 SafeActionMask 안전성 지표가 무너지면 rollback 또는 Task 1 비활성화 flag 추가.

## Task 2. Phase2 Sweep Visible History Stack Observation

### 목적

Phase2 `Phase2SweepOnly()`의 4줄 sweep에서 현재 warning만 피하다가 3~4번째 line에 갇히는 partial observability를 줄인다. 내부 sequence index가 아니라 화면에 이미 보였던 warning/damage history를 observation으로 제공한다.

### 수정할 파일

- `unity_project/Assets/Project/Scripts/RL/BossRLStateExtractor.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
- 필요 시 신규 파일: `unity_project/Assets/Project/Scripts/RL/BossRLVisibleHistoryBuffer.cs`
- 필요 시 scene: `unity_project/Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity`

### 구현 단위

1. `RefreshHazardMasks()` 이후 current warning/damage mask를 history buffer에 저장한다.
2. 현재 이미 존재하는 `prevWarningMask`를 `previous_warning_mask_1`로 간주한다.
3. 추가로 `previous_warning_mask_2`, `previous_damage_mask_1`을 유지한다.
4. `recent_sweep_band_history_mask`는 pattern identity를 직접 쓰지 않고, visible warning/damage band history로 구성한다. 구현상 sweep 전용 판별이 내부 pattern 의존이면 일반 `visible_telegraph_history_stack`로 대체한다.

### 새 observation channel

권장안:

- `previous_warning_mask_2`: `+49`
- `previous_damage_mask_1`: `+49`
- `recent_sweep_band_history_mask`: `+49`

보수안:

- `previous_warning_mask_1`: `+49`
- `previous_warning_mask_2`: `+49`
- `previous_damage_mask_1`: `+49`
- `recent_sweep_band_history_mask`: `+49`

주의: 현재 `prevWarningMask`가 이미 observation에 있으므로 `previous_warning_mask_1`을 새로 추가하면 중복이다.

### VectorObservationSize 증가량

- 권장안: `+3N = +147`, `193 -> 340`.
- 보수안: `+4N = +196`, `193 -> 389`.
- Task 1 권장안과 합산 시: `193 + 147 + 147 = 487`.
- Task 1 최소안과 Task 2 권장안 합산 시: `193 + 98 + 147 = 438`.

### BehaviorParameters 변경 필요 여부

필요하다. 선택한 history channel 수에 맞춰 vector observation size를 변경해야 한다.

### 기존 checkpoint/ONNX 호환 여부

호환되지 않는다. observation size가 바뀌므로 기존 ONNX/checkpoint는 그대로 사용할 수 없다.

### Diagnostic metric

- `phase2_sweep_history_active_steps`
- `previous_warning_2_nonzero_steps`
- `previous_damage_1_nonzero_steps`
- `recent_sweep_history_nonzero_steps`
- `sweep_visible_history_stack_nonzero_steps`
- `sweep_line_1_hit_count`
- `sweep_line_2_hit_count`
- `sweep_line_3_hit_count`
- `sweep_line_4_hit_count`
- `sweep_escape_then_next_line_hit_count`
- `sweep_history_channels_added_count`
- `next_band_direct_observation_count` must be 0
- `sweep_sequence_index_observation_count` must be 0

### 5K 검증 기준

5K diagnostic run을 실행하는 단계에서 확인한다. 이 계획 작성 단계에서는 실행하지 않는다.

- Compile 성공.
- BehaviorParameters vector size 일치.
- `previous_warning_2_nonzero_steps > 0` in episodes with repeated warnings.
- `previous_damage_1_nonzero_steps > 0` after damage events.
- `next_band_direct_observation_count = 0`.
- `sweep_sequence_index_observation_count = 0`.
- NaN/exception/crash 없음.
- 기존 danger mask 규칙 유지.

### 실패 시 rollback 기준

- 내부 `Phase2SweepOnly` enum, `startIndex`, `idx`, `i`, next band를 observation에 넣게 되면 rollback.
- next line을 hard mask로 직접 막으면 rollback.
- history buffer가 episode reset 뒤 남아 첫 step observation을 오염시키면 rollback.
- `prevWarningMask` ordering이 깨져 기존 observation semantics가 바뀌면 rollback.
- observation size mismatch가 나면 rollback.

## Task 3. Boss Sprite Visibility vs BossCell Alignment Diagnostic

### 목적

PlayerCombat의 boss hit 기준인 `BossCell`과 사람이 보는 boss sprite 위치/visibility가 얼마나 일치하는지 수치로 증명한다. 이 task는 observation 추가가 아니라 diagnostic logging이다.

### 수정할 파일

- `unity_project/Assets/Project/Scripts/Player/PlayerCombat.cs`
- `unity_project/Assets/Project/Scripts/Boss/ElevatorBossController.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
- 필요 시 신규 파일: `unity_project/Assets/Project/Scripts/RL/BossRLTargetAlignmentDiagnostics.cs`

### 구현 단위

1. `PlayerCombat`의 boss successful hit 직후 diagnostic hook을 호출한다.
2. hook은 damage 판정을 바꾸지 않고, hit 순간의 player cell, facing, attack cells, `BossCell`, boss root position, boss sprite renderer state를 기록한다.
3. `ElevatorBossController`에는 read-only diagnostic accessor 또는 helper만 추가한다.
4. `BossHealth`, `PlayerCombat`, `GridMover`, `GridManager` gameplay behavior는 변경하지 않는다.

### 새 observation channel

없음. 이 task는 diagnostic-only다.

### VectorObservationSize 증가량

`+0`. `VectorObservationSize` 변경 없음.

### BehaviorParameters 변경 필요 여부

필요 없다.

### 기존 checkpoint/ONNX 호환 여부

호환된다. observation/action/reward를 바꾸지 않는 logging-only 변경이므로 기존 ONNX/checkpoint input shape와 호환된다.

### Diagnostic metric

- `hit_visible_boss_in_attack_range_count`
- `hit_bosscell_in_attack_range_but_visual_out_count`
- `boss_cell_visual_mismatch_count`
- `boss_health_visual_mismatch_count`
- `boss_cell_stale_during_dash_count`
- `boss_cell_stale_during_markdash_count`
- `hidden_boss_hit_count`
- `invisible_or_empty_space_hit_count`
- `boss_visible_false_but_hit_allowed_count`
- `boss_visible_true_but_bosscell_mismatch_count`
- `boss_sprite_visible_hit_count`
- `boss_sprite_invisible_hit_count`
- `boss_sprite_off_but_bosscell_hit_count`
- `boss_visual_root_inactive_hit_count`
- `boss_renderer_disabled_hit_count`
- `boss_alpha_zero_hit_count`
- `bosscell_in_attack_range_but_sprite_not_visible_count`
- `bosscell_in_attack_range_but_sprite_cell_out_of_range_count`
- `sprite_visible_cell_in_attack_range_count`
- `sprite_visible_cell_out_of_attack_range_count`
- `hidden_boss_attack_reward_count`

Successful hit마다 기록:

- episode
- step
- time
- player cell
- player facing
- attack cells
- `ElevatorBossController.BossCell`
- `BossHealth` transform position
- boss root transform position
- boss visual transform position
- boss `GridOccupant` cell, 있으면
- boss visible renderer bounds center
- boss sprite visible 여부
- boss visual root activeSelf / activeInHierarchy
- boss SpriteRenderer enabled 여부
- boss SpriteRenderer alpha
- boss sprite object name
- boss currently dashing/hidden/markdash 추정 상태
- BossCell inside attack cells 여부
- visual boss cell inside attack cells 여부
- boss damage amount

### 5K 검증 기준

5K diagnostic run 또는 5 episode eval diagnostic에서 확인한다. 이 계획 작성 단계에서는 실행하지 않는다.

- Compile 성공.
- observation size unchanged.
- 기존 ONNX inference 가능.
- successful boss hit가 발생한 episode에서 alignment log가 hit 수와 같은 수 또는 의도한 sampling 수만큼 기록된다.
- diagnostic logging이 reward/action/mask를 바꾸지 않음.
- NaN/exception/crash 없음.
- `attack_out_of_range = 0` 유지.

### 실패 시 rollback 기준

- PlayerCombat hit 판정이 바뀌면 rollback.
- BossHealth damageable/visibility gameplay behavior가 바뀌면 rollback.
- logging 때문에 frame drop, log 폭증, timeout이 발생하면 rollback 또는 sampling rate 제한.
- diagnostic helper가 collider 기준만으로 visible을 판단하면 rollback.
- hit 수와 alignment log 수가 맞지 않으면 hook 위치 재검토 또는 rollback.

## 권장 구현 순서

1. Task 3 먼저 수행한다.
2. Task 1을 수행한다.
3. Task 2를 수행한다.

이 순서가 안전한 이유:

- Task 3은 observation size를 바꾸지 않아 기존 ONNX/checkpoint와 호환된다.
- visible boss / BossCell alignment를 먼저 확인하면 MarkDash observation gap과 hidden target reward 문제를 분리할 수 있다.
- Task 1과 Task 2는 observation size를 바꾸므로 fresh diagnostic/training branch로 분리하는 것이 좋다.

## 합산 VectorObservationSize 후보

단독:

- Task 1 최소안: 291.
- Task 1 권장안: 340.
- Task 2 권장안: 340.
- Task 2 보수안: 389.
- Task 3: 193 유지.

조합:

- Task 1 권장안 + Task 2 권장안: `487`.
- Task 1 최소안 + Task 2 권장안: `438`.
- Task 1 권장안 + Task 2 보수안: `536`.

권장:

- 진단 우선: Task 3 only, size 193 유지.
- observation 구현 1차: Task 1 최소안 + Task 2 권장안, size 438.
- 논문/분석 설명 우선: Task 1 권장안 + Task 2 권장안, size 487.

## 5K 공통 검증 기준

5K 검증은 구현 허가 이후에만 수행한다.

- Unity compile 성공.
- BehaviorParameters vector size 일치.
- Trainer/EXE crash 없음.
- NaN 없음.
- exception 없음.
- terminal reward 유지: `player_dead=-8`, `boss_dead=+5`.
- `attack_out_of_range = 0` 유지.
- fake marker가 hard danger mask에 들어가지 않음.
- current/next pattern 직접 observation 없음.
- next band 직접 observation 없음.
- hidden hurtbox active observation 없음.

## 공통 rollback 기준

- 금지 observation이 추가되면 즉시 rollback.
- reward 값이 변경되면 rollback.
- PlayerCombat 판정이 변경되면 rollback.
- BehaviorParameters와 `VectorObservationSize`가 불일치하면 rollback.
- 기존 SafeActionMask 안정성, terminal reward, attack_out_of_range 0이 깨지면 rollback.
- EXE crash, NaN, timeout, log 폭증이 있으면 rollback 또는 해당 task 비활성화.
