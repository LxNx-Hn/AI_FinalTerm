# Eval210 ONNX Sweep

Date: 2026-06-01

## Scope

- Purpose: find which existing policy artifact performs best under the same Eval210 foreground inference conditions.
- Training was not run.
- Reward/action mask/observation/boss/player gameplay logic was not changed.
- Timeout remained `210s`.
- Eval conditions: `InferenceOnly`, foreground standalone player, Eval build `runInBackground=true`, 5 episodes per model.

## Harness Changes

Modified eval-only files:

- `unity_project/Assets/Project/Scripts/Editor/RLEval210BatchBuild.cs`
- `scripts/build_eval_210_windows.ps1`

What changed:

- Eval build now accepts `-ModelSource` / `-evalModelSource` so each existing ONNX can be connected to the Eval210 scene.
- ONNX sidecar `*.onnx.data` files are copied from the selected run root.
- Stale Eval model sidecars are removed before each import.
- Build still sets `PlayerSettings.runInBackground = true` only during build and restores the previous value afterward.

Unchanged:

- `scripts/run_eval_210_inference_windows.ps1` still runs the Eval EXE without `-WindowStyle Hidden`.
- `ProjectSettings.asset` remains `runInBackground: 0`; no permanent project setting content diff was introduced.

## Model Inventory

| Label | Run | ONNX | Export step | ONNX size | Sidecar | Checkpoint |
|---|---|---|---:|---:|---|---|
| 20kModel | `BossPPO_SafeActionMask_v1` | `results/BossPPO_SafeActionMask_v1/BossPlayer.onnx` | 20053 | 48,274 | yes, 2,512,392 bytes | `results/BossPPO_SafeActionMask_v1/BossPlayer/checkpoint.pt` |
| 50kModel | `BossPPO_SafeActionMask_50k_v1_retry1` | `results/BossPPO_SafeActionMask_50k_v1_retry1/BossPlayer.onnx` | 50090 | 48,274 | yes, 2,512,392 bytes | `results/BossPPO_SafeActionMask_50k_v1_retry1/BossPlayer/checkpoint.pt` |
| 100kFinal | `BossPPO_SafeActionMask_100k_v1` | `results/BossPPO_SafeActionMask_100k_v1/BossPlayer.onnx` | 100045 | 48,283 | yes, 2,512,392 bytes | `results/BossPPO_SafeActionMask_100k_v1/BossPlayer/checkpoint.pt` |

Hash check:

- `BossPlayer.onnx` and the internal `BossPlayer-*.onnx` are identical for each run.
- Therefore there was no separate middle ONNX candidate available inside these result folders.

## Training-Log Baseline

| Model | Episodes | player_dead | timeout | boss_dead | Avg Survival | Max Survival | Avg Boss Dmg | Max Boss Dmg | Hit Rate | Safe Taken Ratio | OOR Atk | Avoidable |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 20kModel | 37 | 29 | 8 | 0 | 54.13s | 90.1s | 7.92 | 17 | 100.000% | 20.096% | 0 | 27 |
| 50kModel | 74 | 46 | 28 | 0 | 67.61s | 90.1s | 11.99 | 26 | 100.000% | 25.299% | 0 | 57 |
| 100kFinal | 125 | 66 | 59 | 0 | 79.45s | 90.1s | 22.92 | 36 | 99.965% | 54.445% | 0 | 89 |

Training-log candidate:

- Best checkpoint candidate by training logs is `BossPPO_SafeActionMask_100k_v1/BossPlayer/checkpoint.pt`.
- Best ONNX by training logs is `BossPPO_SafeActionMask_100k_v1/BossPlayer.onnx`.
- Best episode in training log: 100kFinal episode 73 or 125, boss damage `36/60`, reason `timeout`.

## Eval210 ONNX Sweep

| Eval Run | Model | Episodes | player_dead | timeout | boss_dead | Avg Survival | Max Survival | Boss Damage Per Episode | Avg Boss Dmg | Max Boss Dmg | Hit Rate | Attack Actions / Hits | Safe Taken Ratio | OOR Atk | Avoidable | Move Warn | Move Dmg | Warn Reentry |
|---|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|
| `Eval210_Sweep_20kModel_v1` | 20kModel | 5 | 5 | 0 | 0 | 37.98s | 55.2s | 6, 9, 3, 9, 3 | 6.00 | 9 | 100.000% | 30 / 30 | 21.739% | 0 | 5 | 77 | 4 | 4 |
| `Eval210_Sweep_50kModel_v1` | 50kModel | 5 | 5 | 0 | 0 | 38.98s | 54.7s | 1, 7, 6, 9, 6 | 5.80 | 9 | 100.000% | 29 / 29 | 16.292% | 0 | 3 | 74 | 5 | 5 |
| `Eval210_Sweep_100kFinal_v1` | 100kFinal | 5 | 5 | 0 | 0 | 26.02s | 37.9s | 3, 5, 5, 3, 2 | 3.60 | 5 | 100.000% | 18 / 18 | 23.077% | 0 | 3 | 64 | 4 | 5 |

Danger-tile attack checks:

- All sweep runs had `atk_on_warn=0`, `atk_on_dmg=0`, `atk_on_recent_warn=0`, `atk_on_recent_dmg=0`.
- `attack_out_of_range` did not recur in any sweep run.
- No `Exception`, `NullReferenceException`, `MissingReferenceException`, `NaN`, or ONNX runtime error was found in the sweep player logs.

Build logs:

- `logs/build_eval_210_20kModel.log`
- `logs/build_eval_210_50kModel.log`
- `logs/build_eval_210_100kFinal.log`

Each build log confirms:

- selected `Model source`
- `Copied 1 ONNX external weight file(s).`
- `Timeout seconds: 210`
- `BehaviorType: InferenceOnly`
- `Model asset path: Assets/Project/RLModels/BossPlayer_Eval210_Current.onnx`
- `old runInBackground: False`
- `build runInBackground: True`
- `Build SUCCEEDED`
- `restored runInBackground: False`

Machine-readable parse output:

- `logs/eval210_sweep_summary_utf8.json`

## Comparison And Judgment

Best Eval210 ONNX:

- `20kModel` is the best by average boss damage (`6.00`) and tied best by max boss damage (`9`).
- `50kModel` is very close (`avg 5.80`, `max 9`).
- `100kFinal` is the worst in this 5-episode Eval210 ONNX sweep (`avg 3.60`, `max 5`).

Is 100K final actually best?

- In training logs: yes, clearly best (`avg 22.92`, `max 36`).
- In Unity ONNX `InferenceOnly` Eval210 sweep: no, it is the worst among the three tested ONNX files.

Timeout bottleneck:

- Not confirmed by this sweep.
- Every ONNX model died before 210s in all 15 evaluated episodes.
- No model reached `50+` boss damage or `boss_dead`.

Attack opportunity bottleneck:

- In Eval210 ONNX, all models are far below the training damage rate.
- All attacks still hit and no out-of-range attacks occur, so the immediate symptom is not invalid attacks.
- The models die early and generate far fewer attack actions than the 100K training baseline.

Policy collapse / overtraining:

- Possible if judging only ONNX Eval results, since 100K final underperforms 20K/50K.
- But training logs show 100K improved strongly through the final episode, so the stronger explanation is not simple policy collapse.

ONNX export / action-selection mismatch:

- Strongly suspected.
- The same 100K policy family that produced `36/60` damage at 90s in trainer-time logs only produced max `5/60` in ONNX `InferenceOnly` sweep.
- This points to a mismatch between training-time policy execution and Unity ONNX `InferenceOnly` execution, such as deterministic-vs-stochastic action selection, policy export/runtime behavior, or trainer-side settings not reproduced in standalone inference.

## Trainer-Side Evaluation

Not executed.

Reason:

- `mlagents-learn --inference --resume` exists, but the current Eval210 EXE is built as `BehaviorType=InferenceOnly` with an embedded ONNX, so it would not isolate Python checkpoint inference.
- The original training EXE is `BehaviorType=Default` but has the old 90s timeout, so it is not the same Eval210 condition.

Candidate command once a Default-behavior Eval210 checkpoint-eval EXE exists:

```powershell
.venv-mlagents\Scripts\mlagents-learn.exe ml-agents-config\boss_ppo_safe_mask_100k_v1.yaml --run-id BossPPO_SafeActionMask_100k_v1 --resume --inference --env builds\windows\BossPPO_RLTrain_Eval210_Default\BossPPO_RLTrain_Eval210_Default.exe --num-envs 1
```

The required missing piece is a separate Eval210 build that keeps `maxEpisodeSeconds=210` but uses `BehaviorType=Default` and no embedded ONNX. That would still be eval-only, but it was not created in this sweep to avoid expanding scope.

## Recommendation

Do not modify reward/action/mask/observation yet.

Recommended next step:

1. Build a separate Eval210 checkpoint-eval EXE with `BehaviorType=Default`, no embedded ONNX, and `runInBackground=true`.
2. Run one `mlagents-learn --inference --resume` evaluation from `BossPPO_SafeActionMask_100k_v1`.
3. If Python checkpoint inference matches training logs, the ONNX `InferenceOnly` path is the culprit.
4. If Python checkpoint inference also collapses, compare trainer-time environment args such as `--time-scale`, deterministic setting, decision cadence, and reset/load behavior.
5. Only after resolving checkpoint-vs-ONNX mismatch should timeout or reward tuning be reconsidered.
