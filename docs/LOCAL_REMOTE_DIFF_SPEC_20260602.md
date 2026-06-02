# Local vs Remote Change Specification - 2026-06-02

## Scope

This document summarizes the local workspace changes relative to `origin/main`.

- Remote: `https://github.com/LxNx-Hn/AI_FinalTerm`
- Branch: `main`
- Comparison state: `HEAD` is the same commit as `origin/main`, but the working tree has uncommitted local changes.
- Primary purpose of local changes: train a Boss01 Elevator ML-Agents PPO policy that dodges enough to avoid 3HP death, attacks safely, and clears the boss quickly.

## Training Outcome

The local 1M training run completed successfully.

- Run ID: `BossPPO_FastClear3HitFixed_1000k_0602_v1`
- Final trainer step: `1,000,000`
- Final trainer exit code: `0`
- Final exported model: `results/BossPPO_FastClear3HitFixed_1000k_0602_v1/BossPlayer.onnx`
- Final checkpoint pair: `BossPlayer-1000040.pt` / `BossPlayer-1000040.onnx`
- 700k foreground evaluation result: `20 / 30` clears, `10 / 30` deaths, `0` timeouts
- First observed clear: approximately `390k-400k` trainer steps, with `94.3s` survival time

## Main Learning-Code Changes

### `unity_project/Assets/Project/Scripts/RL/BossRLReward.cs`

Purpose: reshape rewards toward fast boss clear instead of passive survival.

Changes:

- Increased boss damage reward from `+0.10` per HP to `+0.40` per HP.
- Increased boss kill reward from `+5` to `+30`.
- Added fast clear reward up to `+20`.
- Increased player hit penalty from `-2.0` to `-2.5`.
- Added critical HP penalty `-3.0` when a hit leaves player at 1 HP.
- Increased player death penalty from `-8` to `-10`.
- Increased step penalty from `-0.001` to `-0.002`.
- Increased hazard movement penalties:
  - moved into warning: `-0.08` to `-0.10`
  - moved into recent warning: `-0.10` to `-0.12`
  - moved into damage: `-0.30` to `-0.35`
- Increased safe in-range attack attempt reward from `+0.08` to `+0.18`.
- Increased missed safe attack opportunity penalty from `-0.003` to `-0.006`.
- Added reward breakdown fields for `rewardCriticalHealthPenalty`, `rewardFastClear`, and `playerHpAfter`.

Learning impact:

- This is directly used by training.
- It changes the reward signal received by the agent.
- It is the most important difference between the local training code and the remote original.

### `unity_project/Assets/Project/Scripts/RL/BossPlayerAgent.cs`

Purpose: stabilize episode terminal handling and support muted training runs.

Changes:

- Added `--rl-mute-audio` command-line handling.
- Sets `AudioListener.volume = 0f` when mute flag is present.
- Added `processingAction` / `IsProcessingAction`.
- Wraps `inputBridge.ApplySingleAction(singleAction)` with the processing flag.
- Resets `processingAction` at episode reset.

Learning impact:

- Audio mute does not affect policy learning.
- `processingAction` affects terminal event timing so the final hit/death/kill can be evaluated before the scene reload is queued.

### `unity_project/Assets/Project/Scripts/RL/BossRLEpisodeResetter.cs`

Purpose: prevent terminal events from ending the episode before `BossPlayerAgent.OnActionReceived` records the final reward/log result.

Changes:

- If `agent.IsProcessingAction` is true, immediate `HandleTerminalEvent` / scene reload is skipped.

Learning impact:

- Directly affects training correctness.
- Fixes a case where the final death or kill hit could be missed because `onDead` / `onBossDead` fired during action execution.

### `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`

Purpose: improve monitoring and post-run analysis.

Changes:

- Reduced step-log frequency from `100` to `250`.
- Added reward breakdown output for `critical_hp` and `fast_clear`.
- Changed player hit accumulation to use actual HP delta instead of only counting hit steps.
- Added:
  - `hp_after`
  - `damage_steps`
  - `max_hp_loss_step`
  - `death_source`
  - `death_source_group`
  - `damage_source_hits`
  - `damage_source_group_hits`
- Added per-episode damage source counters.

Learning impact:

- Monitoring only.
- These values are written through `Debug.Log`.
- They are not included in observations.
- They are not used by reward calculation.
- They do not provide answer labels to the policy.

## Boss Damage Source Instrumentation

These changes support death-cause analysis only. They do not expose attack labels to the agent.

### `unity_project/Assets/Project/Scripts/Boss/DamageTile.cs`

Changes:

- Added `damageSource`.
- Added `damageSourceGroup`.
- Calls `PlayerHealth.TakeDamage(damage, damageSource, damageSourceGroup)`.

Learning impact:

- Monitoring only unless later wired into observation or reward, which it is not currently.

### `unity_project/Assets/Project/Scripts/Player/PlayerHealth.cs`

Changes:

- Added `LastDamageSource`.
- Added `LastDamageSourceGroup`.
- Added overloaded `TakeDamage(int damage, string source, string sourceGroup)`.
- Existing `TakeDamage(int damage)` remains and defaults to `unknown`.
- Resets last damage source to `none` on start.

Learning impact:

- Monitoring only.
- Stores labels for debug logger to print later.

### `unity_project/Assets/Project/Scripts/Boss/BossPatternCaster.cs`

Changes:

- Added optional `damageSource` and `damageSourceGroup` parameters to:
  - `CastCellsWithBeforeDamage`
  - `SpawnDamageCells`
  - `CastDamageOnly`
- Propagates source labels into spawned `DamageTile`.
- Propagates source labels through direct player damage fallback.
- Replaced direct `SendMessage("TakeDamage", ...)` fallback with typed `PlayerHealth.TakeDamage(...)`.

Learning impact:

- Monitoring only for source labels.
- Direct damage fallback behavior remains, but source information is now retained.

### `unity_project/Assets/Project/Scripts/Boss/ElevatorBossController.cs`

Changes:

- Adds readable source names for boss attacks:
  - `BasicScratch`
  - `BasicScratch_EnhancedPhase`
  - `EnhancedScratchDash`
  - `LandingSlam`
  - `LandingSlam_Big`
  - `FinalSlam`
  - `DashPattern_<Direction>`
  - `FinalDashAfterSlam_<Direction>`
  - `FinalRandomNoWarningDash_<Direction>`
  - `PacifistFinalHit`
- Adds `DirectionLabel(Vector2Int dir)` for source naming.

Learning impact:

- Monitoring only for source labels.
- Does not alter observations or reward.

## Training Config and Run Scripts

### `ml-agents-config/boss_ppo_fast_clear_1000k_v1.yaml`

Status: new local file.

Purpose: 1M PPO training config for fast boss clear.

Key settings:

- `trainer_type: ppo`
- `batch_size: 1024`
- `buffer_size: 10240`
- `learning_rate: 0.0003`
- `hidden_units: 512`
- `num_layers: 3`
- `max_steps: 1000000`
- `checkpoint_interval: 100000`
- `keep_checkpoints: 10`
- `summary_freq: 10000`
- `time_horizon: 128`

Learning impact:

- Directly used by the completed 1M training run.

### `scripts/run_fast_clear_1000k_windows.ps1`

Status: new local file.

Purpose: launch the 1M fast-clear training run.

Key behavior:

- Uses `boss_ppo_fast_clear_1000k_v1.yaml`.
- Launches `builds/windows/BossPPO_RLTrain/BossPPO_RLTrain.exe`.
- Uses `--time-scale 40`.
- Uses `--no-graphics`.
- Uses `--target-frame-rate -1`.
- Uses low render settings and `84x84` resolution.
- Passes `--rl-mute-audio` to the Unity player.

Learning impact:

- Does not change the policy logic directly.
- Controls training execution speed and environment runtime options.

### `scripts/run_fast_clear_50k_windows.ps1`

Status: new local file.

Purpose: launch a fast 50k diagnostic run with similar runtime settings.

Key behavior:

- Uses `boss_ppo_safe_opp_reward_50k_v1.yaml`.
- Uses `--time-scale 40`.
- Uses `--no-graphics`.
- Passes `--rl-mute-audio`.

Learning impact:

- Execution helper only.

### `scripts/run_safe_opp_reward_50k_windows.ps1`

Status: modified local file.

Purpose: earlier diagnostic/training script for safe opportunity reward experiments.

Learning impact:

- Script-level execution change only.
- Does not change model logic unless this script is used for training.

## Model and Asset Handling

### `unity_project/Assets/Project/Prefabs/Player/Player.prefab`

Purpose: Unity prefab cleanup during ML-Agents stabilization.

Observed local status:

- Modified relative to remote.
- Used during the RL training environment setup.

Risk:

- This should be reviewed in Unity before committing because prefab diffs can include serialized object reference changes.

### `unity_project/Assets/Project/RLModels/BossPlayer_SafeActionMask_100k_v1.onnx`

Status: deleted locally.

Purpose:

- Old embedded/eval model asset was removed or quarantined during import stabilization.

Risk:

- If any scene or prefab still references this model, it must be replaced before committing.

### `unity_project/Assets/Project/RLModels/BossPlayer_SafeActionMask_100k_v1.onnx.meta`

Status: deleted locally.

Purpose:

- Meta file corresponding to the removed ONNX asset.

### `unity_project/Assets/Project/RLModels/BossPlayer_Eval210_Current.onnx.meta`

Status: modified locally.

Purpose:

- Unity metadata changed during model import/eval setup.

Risk:

- Review before committing because Unity `.meta` changes can alter asset GUID/import settings.

### `unity_project/_Quarantine/`

Status: new untracked local directory.

Purpose:

- Holds broken or disabled RL model import files moved out of active Unity asset import paths.

Commit recommendation:

- Usually do not commit quarantine folders unless the project intentionally tracks disabled assets.

## Generated / Diagnostic Artifacts

### `unity_project/Assets/ML-Agents/Timers/Boss01_Elevator_RLTrain_timers.json`

Status: modified locally.

Purpose:

- Generated ML-Agents timer/profiling output.

Commit recommendation:

- Usually do not commit unless timing evidence is intentionally tracked.

### `unity_project/Assets/Screenshots/smoke_stuck_check.png`

Status: new untracked file.

Purpose:

- Diagnostic screenshot from smoke/stuck checks.

Commit recommendation:

- Usually do not commit unless documenting a specific issue.

### `unity_project/Assets/Screenshots/smoke_stuck_check.png.meta`

Status: new untracked file.

Purpose:

- Unity meta file for the diagnostic screenshot.

Commit recommendation:

- Usually do not commit if the screenshot is not committed.

## Unity Project Settings and Rendering Files

The following files are modified locally:

- `unity_project/Assets/Settings/UniversalRP.asset`
- `unity_project/Assets/UniversalRenderPipelineGlobalSettings.asset`
- `unity_project/ProjectSettings/EditorBuildSettings.asset`
- `unity_project/ProjectSettings/GraphicsSettings.asset`
- `unity_project/ProjectSettings/ProjectSettings.asset`
- `unity_project/ProjectSettings/ShaderGraphSettings.asset`

Observed note:

- `git diff --name-only` currently lists `ProjectSettings.asset` among tracked text diffs.
- `git status` also reports several rendering/settings files.

Purpose:

- These changes likely came from Unity editor/build/import activity while preparing the RL build and scenes.

Commit recommendation:

- Review carefully in Unity before committing.
- Keep only settings that are required for the RL training scene/build to work.
- Revert or exclude unrelated editor/rendering churn if not intentionally changed.

## Documentation

### `docs/MANUAL_UNITY_SETUP_CHECKLIST.md`

Status: modified locally.

Purpose:

- Updated during manual Unity/ML-Agents setup and stabilization notes.

Commit recommendation:

- Safe to commit if the checklist reflects the final intended workflow.

## Final Training Checkpoints

Main run checkpoint directory:

`results/BossPPO_FastClear3HitFixed_1000k_0602_v1/BossPlayer`

Saved checkpoint/model pairs:

- `BossPlayer-99947.pt` / `BossPlayer-99947.onnx`
- `BossPlayer-199879.pt` / `BossPlayer-199879.onnx`
- `BossPlayer-299963.pt` / `BossPlayer-299963.onnx`
- `BossPlayer-399915.pt` / `BossPlayer-399915.onnx`
- `BossPlayer-499986.pt` / `BossPlayer-499986.onnx`
- `BossPlayer-599962.pt` / `BossPlayer-599962.onnx`
- `BossPlayer-699999.pt` / `BossPlayer-699999.onnx`
- `BossPlayer-799886.pt` / `BossPlayer-799886.onnx`
- `BossPlayer-899917.pt` / `BossPlayer-899917.onnx`
- `BossPlayer-999929.pt` / `BossPlayer-999929.onnx`
- `BossPlayer-1000040.pt` / `BossPlayer-1000040.onnx`
- `BossPlayer.onnx` final exported model

Commit recommendation:

- Large training outputs are usually not committed to source control.
- If a final model must be submitted, prefer committing/copying only the final required `.onnx` model and its `.meta` through Unity, not the full `results` tree.

## Commit Planning Recommendation

Recommended commit groups:

1. Learning logic
   - `BossRLReward.cs`
   - `BossPlayerAgent.cs`
   - `BossRLEpisodeResetter.cs`

2. Monitoring and death-source diagnostics
   - `BossRLDebugLogger.cs`
   - `DamageTile.cs`
   - `PlayerHealth.cs`
   - `BossPatternCaster.cs`
   - `ElevatorBossController.cs`

3. Training config/scripts
   - `ml-agents-config/boss_ppo_fast_clear_1000k_v1.yaml`
   - `scripts/run_fast_clear_1000k_windows.ps1`
   - `scripts/run_fast_clear_50k_windows.ps1`

4. Unity asset/settings cleanup
   - Review prefab, project settings, RL model asset deletions, quarantine folder, and generated diagnostic artifacts separately.

## Important Distinction

Training-affecting changes:

- Reward changes in `BossRLReward.cs`.
- Terminal timing fix in `BossPlayerAgent.cs` and `BossRLEpisodeResetter.cs`.
- Runtime training config in `boss_ppo_fast_clear_1000k_v1.yaml`.

Monitoring-only changes:

- `death_source`
- `damage_source_hits`
- `damage_source_group_hits`
- `LastDamageSource`
- `DamageTile.damageSource`
- attack source labels in `ElevatorBossController`

The monitoring-only values are not observations and are not reward inputs. They are logged for analysis and do not give the policy answer labels.
