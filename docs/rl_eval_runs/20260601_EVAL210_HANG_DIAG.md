# Eval210 Inference Hang Diagnosis

Date: 2026-06-01

## Scope

- Run: `BossPPO_SafeActionMask_100k_Eval210_v1`
- EXE: `builds/windows/BossPPO_RLTrain_Eval210/BossPPO_RLTrain_Eval210.exe`
- Log: `results/BossPPO_SafeActionMask_100k_Eval210_v1/run_logs/Player-0.log`
- Purpose: diagnose why `episodes=0` repeated for more than 4 minutes during 210s inference evaluation.
- Training was not resumed or started during this diagnosis.
- Reward/action/mask/observation settings were not changed during this diagnosis.

## Runtime Snapshot

- Eval EXE PID file: `results/BossPPO_SafeActionMask_100k_Eval210_v1/run_logs/eval.pid`
- PID: `4452`
- Process alive: yes
- Process responding: yes
- CPU delta over 3 seconds: `0`
- Working set: about `572 MB`
- Command line:

```text
"C:\Users\KiKi\Documents\Ql\AI_FinalTerm_MLAgents_PPO_NEW\builds\windows\BossPPO_RLTrain_Eval210\BossPPO_RLTrain_Eval210.exe" -logFile C:\Users\KiKi\Documents\Ql\AI_FinalTerm_MLAgents_PPO_NEW\results\BossPPO_SafeActionMask_100k_Eval210_v1\run_logs\Player-0.log
```

## Log Snapshot

- `Player-0.log` exists.
- Size: `2052` bytes.
- Line count: `40`.
- Last write time: `2026-06-01 12:22:24`.
- Size delta over 3 seconds: `0`.
- Last write time delta over 3 seconds: none.
- No newer Eval log was found under AppData/LocalLow; the explicit `-logFile` path is the actual log path.

Last meaningful log lines:

```text
Initialize engine version: 6000.3.10f1 (e35f0c77bd8e)
...
<RI> Input initialized.
UnloadTime: 0.662000 ms
Registered Communicator in Agent.
```

Pattern counts in `Player-0.log`:

```text
[BossRL]=0
EPISODE_END=0
episode_end=0
step==0
exception=0
NullReferenceException=0
Behavior=0
Inference=0
model=0
scene loaded=0
Boss01_Elevator_RLTrain_Eval210=0
```

## Scene And Config Checks

Eval scene file:

- `Assets/Project/Scenes/Boss01_Elevator_RLTrain_Eval210.unity`
- `BossPlayerAgent.m_Enabled: 1`
- `BossRLDebugLogger.m_Enabled: 1`
- `BossRLEpisodeResetter.m_Enabled: 1`
- `DecisionRequester.m_Enabled: 1`
- `DecisionPeriod: 5`
- `TakeActionsBetweenDecisions: 0`
- `BehaviorParameters.m_Model`: assigned to `BossPlayer_SafeActionMask_100k_v1.onnx`
- `BehaviorParameters.m_BehaviorType: 2`, which is `InferenceOnly`
- `maxEpisodeSeconds: 210`

Project setting:

- `ProjectSettings/ProjectSettings.asset` has `runInBackground: 0`.

Relevant code behavior:

- `BossRLReward` computes timeout from `elapsedSeconds >= maxEpisodeSeconds`.
- `BossPlayerAgent` calls reward evaluation, step logging, and terminal timeout handling only inside `OnActionReceived`.
- Therefore, if no decisions/actions are processed, no `[BossRL] step` and no `EPISODE_END` can appear even after 210 seconds of wall time.

## Diagnosis

The Eval EXE did not crash and did not produce an exception. It is alive but idle: CPU does not advance, the player log is no longer updated, and no `[BossRL]` step or episode-end logs are emitted.

The most likely cause is the evaluation runner launching the Unity standalone with `Start-Process -WindowStyle Hidden` while the project has `runInBackground: 0`. A hidden or non-focused standalone player can stop advancing its Unity update loop. That prevents the `DecisionRequester` from driving the Agent, so `OnActionReceived` is not called, which also prevents the 210s timeout from being checked.

This is not evidence of a policy failure, reward issue, action-mask issue, observation issue, or boss timeout result. The current run is invalid as an inference evaluation.

## Recommended Fix

For the next diagnostic/eval attempt, fix the evaluation harness only:

- Stop the current idle Eval EXE after preserving the log.
- Do not change reward/action/mask/observation.
- Run the Eval EXE in a mode that keeps the player loop advancing.
- Preferred: update `scripts/run_eval_210_inference_windows.ps1` to pass `-batchmode -nographics -logFile <PlayerLog>` and avoid `-WindowStyle Hidden`, or explicitly set eval-build-only `PlayerSettings.runInBackground = true`.
- Add one eval-only runtime startup log to confirm `scene`, `BehaviorType`, `model assigned`, `maxEpisodeSeconds`, `DecisionRequester`, and the first `OnActionReceived` call before running 5 episodes again.
