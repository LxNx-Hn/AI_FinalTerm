# Terminal Reward Diagnostic Report

Date: 2026-06-01

## Scope

- No extra 50K/100K training was run.
- No timeout value was changed in this step; current RLTrain timeout remains `210s`.
- No reward value was tuned in this step.
- SafeActionMask, observation size, and action spec were not changed.
- Original CODE-BLUE repository/folder was not modified.
- Boss pattern/HP/player attack/DamageTile/WarningTile/PlayerCombat/GridMover/GridManager were not modified.

## Files Reviewed

- `unity_project/Assets/Project/Scripts/RL/BossPlayerAgent.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLEpisodeResetter.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLReward.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
- `unity_project/Assets/Project/Scripts/Player/PlayerHealth.cs`
- `unity_project/Assets/Project/Scripts/Boss/BossHealth.cs`

## Event Flow Findings

player_dead event flow:

1. `PlayerHealth.TakeDamage()` reduces `currentHp`.
2. When `currentHp <= 0`, `PlayerHealth.IsDead = true`.
3. `PlayerHealth.onDead.Invoke()` fires immediately.
4. `BossRLEpisodeResetter.HandlePlayerDead()` calls `HandleTerminalEvent("player_dead")`.
5. `BossRLEpisodeResetter.HandleTerminalEvent()` calls `agent?.HandleTerminalEvent(reason)`.
6. Before this fix, `BossPlayerAgent.HandleTerminalEvent()` only logged summary, called `EndEpisode()`, and queued scene reload.
7. Because this event path can happen between `OnActionReceived()` decisions, `BossRLReward.Evaluate()` was bypassed and `PlayerDeathPenalty` was not applied.

boss_dead event flow:

1. `BossHealth.TakeDamage()` reduces `currentHp`.
2. When `currentHp <= 0`, `BossHealth.deadInvoked = true`.
3. `BossHealth.onDead.Invoke()` fires immediately.
4. `BossRLEpisodeResetter.HandleBossDead()` calls `HandleTerminalEvent("boss_dead")`.
5. Same event terminal path reaches `BossPlayerAgent.HandleTerminalEvent(reason)`.
6. Before this fix, `BossKillReward` could be missed if the terminal was handled through the event path before a later `Evaluate()` step.

## Root Cause

`PlayerDeathPenalty=-8.0` and `BossKillReward=+5.0` existed in `BossRLReward.Evaluate()`, but terminal events from `PlayerHealth.onDead` / `BossHealth.onDead` could bypass `Evaluate()`.

The old `BossPlayerAgent.HandleTerminalEvent(reason)` did not call `AddReward()` before `EndEpisode()`. Therefore `death=0.000` was not just a logging bug for the event path; it indicated an actual terminal reward application bug, and the logger was accurately showing that the reward accumulator had never received the death terminal reward.

## Modified Files

- `unity_project/Assets/Project/Scripts/RL/BossRLReward.cs`
  - Added `TerminalRewardForReason(reason)` helper.
  - Uses existing constants only:
    - `player_dead -> PlayerDeathPenalty (-8.0)`
    - `boss_dead -> BossKillReward (+5.0)`
    - `timeout -> 0`
- `unity_project/Assets/Project/Scripts/RL/BossPlayerAgent.cs`
  - Added `terminalRewardApplied` duplicate guard.
  - Added `ApplyTerminalReward(reason)`.
  - Applies terminal reward before `EndEpisode()` when terminal is event-driven.
  - If `Evaluate()` already applied boss/player terminal reward, `HandleTerminalEvent(reason, terminalRewardAlreadyApplied: true)` prevents duplicate reward.
  - After `inputBridge.ApplySingleAction()`, returns immediately if an onDead event already ended the episode, preventing post-terminal `Evaluate()` double counting.
- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
  - Added `RecordTerminalReward(reason, reward)`.
  - Event-path terminal reward is reflected in `rewardTotal`, `death`, or `boss`.
  - Adds explicit log line: `[BossRL] terminal_reward reason=... value=...`.
- `ml-agents-config/boss_ppo_terminal_reward_diag_v1.yaml`
  - Diagnostic-only config with `max_steps: 5000`.
- `scripts/run_terminal_reward_diag_windows.ps1`
  - Diagnostic-only runner for `BossPPO_TerminalReward_Diag_v1`.

## Verification

Compile / build:

- Command: `scripts/build_rl_windows.ps1`
- Unity batch build exit code: `0`
- Build log: `logs/build_windows.log`
- Build result line: `[RLTrainBatchBuild] Build SUCCEEDED. Size: 519684706 bytes`
- No C# compile error found.
- Unity licensing access-token messages appeared in the build log, but the build succeeded and they were not compile/runtime failures.

Diagnostic run:

- Run ID: `BossPPO_TerminalReward_Diag_v1`
- max_steps: `5000`
- Trainer stdout: `logs/trainer_terminal_reward_diag_v1_retry_stdout.log`
- Trainer stderr: `logs/trainer_terminal_reward_diag_v1_retry_stderr.log`
- Player log: `results/BossPPO_TerminalReward_Diag_v1/run_logs/Player-0.log`
- Trainer exit code: `0`
- ONNX: `results/BossPPO_TerminalReward_Diag_v1/BossPlayer.onnx`
- Checkpoint: `results/BossPPO_TerminalReward_Diag_v1/BossPlayer/checkpoint.pt`

Diagnostic aggregate from final clean run:

| Metric | Value |
|---|---:|
| Episodes | 8 |
| player_dead | 8 |
| boss_dead | 0 |
| timeout | 0 |
| `terminal_reward` lines | 8 |
| terminal reward total | -64.000 |
| player_dead terminal reward total | -64.000 |
| `death=0.000` count | 0 |
| average `death` reward | -8.000 |
| average total reward | -22.873 |
| average boss_damage | 9.000 |
| max boss_damage | 18 |
| average survival | 61.713s |
| max survival | 110.4s |
| hit_rate | 100.000% |
| safe_attack_taken_ratio | 20.282% |
| attack_out_of_range | 0 |
| avoidable_hit | 4 |
| NaN lines | 0 |
| exception lines | 0 |
| communicator error/timeout lines | 0 |

Episode terminal reward evidence:

| Episode | reason | survival | boss_damage | total | death |
|---:|---|---:|---:|---:|---:|
| 1 | player_dead | 72.5s | 9 | -25.875 | -8.000 |
| 2 | player_dead | 53.2s | 5 | -22.384 | -8.000 |
| 3 | player_dead | 65.8s | 11 | -22.637 | -8.000 |
| 4 | player_dead | 60.4s | 13 | -22.044 | -8.000 |
| 5 | player_dead | 110.4s | 18 | -26.802 | -8.000 |
| 6 | player_dead | 51.1s | 4 | -23.174 | -8.000 |
| 7 | player_dead | 48.3s | 7 | -21.850 | -8.000 |
| 8 | player_dead | 32.0s | 5 | -18.221 | -8.000 |

Example log pattern now present:

```text
[BossRL] terminal_reward reason=player_dead value=-8.000
[BossRL] EPISODE_END reason=player_dead ...
  reward: ... death=-8.000 ...
```

## Judgment

Result branch: A.

- The death terminal reward was actually missing on the event terminal path.
- The fix applied `-8.0` before `EndEpisode()`.
- The diagnostic run confirmed `death=-8.000` for all observed player deaths.
- `death=0.000` did not recur.
- SafeActionMask stayed healthy in the diagnostic evidence: `attack_out_of_range=0`, hit_rate `100%`.

Next training can proceed, but should not jump directly to 100K. Per the requested decision tree, the next safe learning step is a 210s timeout 50K fresh/resume candidate after this terminal reward fix, then compare stability and attack opportunity metrics before scaling.

## Remaining Risks

- `boss_dead` did not occur in the 5K diagnostic and was not forced, so the +5 boss terminal event path is implemented but not empirically observed in this run.
- ONNX export warnings still appear on diagnostic export, including training-mode export and opset conversion fallback warnings. This is unrelated to terminal reward application but remains relevant for later ONNX `InferenceOnly` mismatch work.
- The diagnostic was intentionally short and should not be interpreted as policy performance evaluation.
