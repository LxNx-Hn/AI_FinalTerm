# Eval210 Background Fix Inference Evaluation

Date: 2026-06-01

## Scope

- Run ID: `BossPPO_SafeActionMask_100k_Eval210_bgfix_v1`
- ONNX: `results/BossPPO_SafeActionMask_100k_v1/BossPlayer.onnx`
- Eval EXE: `builds/windows/BossPPO_RLTrain_Eval210/BossPPO_RLTrain_Eval210.exe`
- Player log: `results/BossPPO_SafeActionMask_100k_Eval210_bgfix_v1/run_logs/Player-0.log`
- Runner logs:
  - `logs/eval210_bgfix_runner.out.log`
  - `logs/eval210_bgfix_runner.err.log`
- Training was not run.
- Reward/action/mask/observation/model/gameplay values were not changed.

## Changes

Modified files/settings:

- `unity_project/Assets/Project/Scripts/Editor/RLEval210BatchBuild.cs`
- `scripts/run_eval_210_inference_windows.ps1`

Generated eval artifacts:

- `unity_project/Assets/Project/Scenes/Boss01_Elevator_RLTrain_Eval210.unity`
- `unity_project/Assets/Project/RLModels/BossPlayer_SafeActionMask_100k_v1.onnx`
- `unity_project/Assets/Project/RLModels/BossPlayer-100045.onnx.data`
- `builds/windows/BossPPO_RLTrain_Eval210/BossPPO_RLTrain_Eval210.exe`

Background freeze fix:

- Previous freeze cause: standalone player was launched hidden while `ProjectSettings.asset` had `runInBackground: 0`.
- Eval build now sets `PlayerSettings.runInBackground = true` only for the build and restores the previous value after build.
- `scripts/run_eval_210_inference_windows.ps1` no longer uses `-WindowStyle Hidden` for the Eval EXE.
- `ProjectSettings.asset` remained `runInBackground: 0` after build; no permanent ProjectSettings content diff was introduced.

## Build Verification

Build log: `logs/build_eval_210_windows.log`

Key lines:

```text
[RLEval210BatchBuild] Copied 1 ONNX external weight file(s).
[RLEval210BatchBuild] Timeout seconds: 210
[RLEval210BatchBuild] BehaviorType: InferenceOnly
[RLEval210BatchBuild] Model asset path: Assets/Project/RLModels/BossPlayer_SafeActionMask_100k_v1.onnx
[RLEval210BatchBuild] old runInBackground: False
[RLEval210BatchBuild] build runInBackground: True
[RLEval210BatchBuild] Build SUCCEEDED. Size: 522201570 bytes
[RLEval210BatchBuild] restored runInBackground: False
```

Eval scene verification:

```text
VectorObservationSize: 193
BranchSizes: 06000000
m_Model: assigned
m_BehaviorType: 2
m_BehaviorName: BossPlayer
maxEpisodeSeconds: 210
```

## Runtime Health

30 second check:

```text
EXE alive: True
CPU delta: 0.891
Player-0.log exists: True
[BossRL] step logs: 21
episode_end logs: 1
```

90 second check:

```text
EXE alive: True
CPU delta: 2.359
[BossRL] step logs: 102
episode_end logs: 3
```

Final runner state:

```text
episodes=5
Target episode count reached.
Closing EXE PID=33292
Finished with episodes=5
```

No `Exception`, `NullReferenceException`, `MissingReferenceException`, `NaN`, or ONNX import/runtime error was found in `Player-0.log`.

## Evaluation Metrics

Episodes: `5`

Terminal reasons:

```text
boss_dead: 0
player_dead: 5
timeout: 0
```

Per-episode survival:

```text
1: 27.2s
2: 64.0s
3: 9.5s
4: 66.9s
5: 22.4s
```

Per-episode boss damage:

```text
1: 4
2: 5
3: 1
4: 12
5: 5
```

Aggregate:

```text
max_boss_damage: 12
average_boss_damage: 5.4
attack_actions: 27
hits: 27
missed: 0
cooldown_attacks: 0
hit_rate: 100.0%
safe_attack_opportunity_steps: 170
safe_attack_taken: 27
safe_attack_taken_ratio: 15.882%
attack_out_of_range: 0
avoidable_hit_total: 1
```

Danger-tile attack checks:

```text
atk_on_warn: 0
atk_on_dmg: 0
atk_on_recent_warn: 0
atk_on_recent_dmg: 0
```

## Judgment

The Eval210 execution loop is fixed. The previous `episodes=0` freeze was caused by the hidden/background standalone player execution path, not by PPO policy quality, ONNX import, reward, action mask, observation, or the 210 second timeout itself.

The 210 second timeout bottleneck was not validated in this run because every episode ended by player death before reaching 210 seconds. The policy did not reach a long survival or timeout state:

- Best survival: `66.9s`
- Best boss damage: `12/60`
- Average boss damage: `5.4/60`
- `boss_dead: 0`
- `timeout: 0`

Based on the requested decision tree, this falls under the survival-collapse branch rather than timeout-bottleneck confirmation. Since boss damage is also below `40`, attack frequency/opportunity utilization may need review later, but this run's dominant blocker is early survival failure in standalone ONNX inference.

## Next Step Recommendation

Do not modify reward/action/mask/observation yet.

Recommended next step:

- First separate why standalone ONNX inference performs much worse than the prior 100K trainer run summary.
- Compare runtime settings between the 100K training EXE path and Eval210 standalone path, especially decision cadence, time scale, deterministic inference, scene reload behavior, model import path, and any trainer-side environment parameters.
- If standalone setup is confirmed equivalent, inspect replay/log excerpts for the deaths and classify whether they are avoidable pattern hits or movement/decision cadence issues.
