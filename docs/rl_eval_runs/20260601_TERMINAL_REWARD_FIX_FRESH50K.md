# Terminal Reward Fix + Timeout210 Fresh50K Report

작성 시각: 2026-06-01 KST

## 1. 범위와 안전 조건

- 작업 repo: `C:\Users\KiKi\Documents\Ql\AI_FinalTerm_MLAgents_PPO_NEW`
- Git branch: `rl-wrapper-bugfix-danger-memory`
- Git HEAD: `caa56ac161819ac3273f21016b0db972b2259a37`
- Git remote: `origin https://github.com/LxNx-Hn/AI_FinalTerm.git`
- CODE-BLUE 원본 repo/원본 로컬 폴더: 건드리지 않음.
- 이번 단계에서 reward/action/mask/observation 구조 변경: 없음.
- 보스 HP/패턴/데미지/플레이어 공격력 변경: 없음.
- 학습 timeout: 210초.
- 이번 실행 목적: death terminal reward fix가 적용된 fresh 50K PPO 학습 후 Eval210 ONNX inference 5 episodes 평가.

## 2. 적용된 terminal reward fix 상태

이전 진단에서 `death=0.000` 원인은 `PlayerHealth.onDead` / `BossHealth.onDead` terminal event path가 `BossRLReward.Evaluate()`를 거치지 않고 `EndEpisode()`를 호출한 점으로 확인했다. 이번 50K는 아래 수정이 포함된 상태에서 fresh로 실행했다.

- `unity_project/Assets/Project/Scripts/RL/BossRLReward.cs`
  - 기존 상수만 반환하는 `TerminalRewardForReason(reason)` 추가.
  - `player_dead = -8.0`, `boss_dead = +5.0`, `timeout = 0.0`.
- `unity_project/Assets/Project/Scripts/RL/BossPlayerAgent.cs`
  - terminal event 발생 시 보상을 1회 적용하도록 `ApplyTerminalReward(reason)`와 duplicate guard 추가.
- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
  - `[BossRL] terminal_reward reason=... value=...` 로그와 reward bucket 반영 추가.

## 3. 빌드 검증

- RL build command: `scripts/build_rl_windows.ps1`
- RL build log: `logs/build_windows.log`
- RL build result: exit 0, `Build SUCCEEDED`
- Active build scene: `Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity`
- Eval build command: `scripts/build_eval_210_windows.ps1 -CloseUnity -ModelSource "results\BossPPO_TerminalRewardFix_Timeout210_Fresh50k_v1\BossPlayer.onnx" -ModelLabel "TerminalRewardFixFresh50k"`
- Eval build log: `logs/build_eval_210_TerminalRewardFixFresh50k.log`
- Eval build result: exit 0, `Build SUCCEEDED`
- Eval settings verified in build log: `BehaviorType: InferenceOnly`, timeout `210`, `runInBackground: True`
- Build log compile errors: none found. `Exception` matches were package/path text only, not runtime stack traces.

## 4. Fresh50K training run

- Run ID: `BossPPO_TerminalRewardFix_Timeout210_Fresh50k_v1`
- Config: `ml-agents-config/boss_ppo_terminal_reward_fix_timeout210_fresh50k_v1.yaml`
- Runner: `scripts/run_terminal_reward_fix_fresh50k_windows.ps1`
- Max steps: `50,000`
- Resume/init-from: none, fresh run.
- Trainer exit: `0`
- Trainer stdout: `logs/trainer_terminal_reward_fix_fresh50k_v1_stdout.log`
- Trainer stderr: `logs/trainer_terminal_reward_fix_fresh50k_v1_stderr.log`
- Player log: `results/BossPPO_TerminalRewardFix_Timeout210_Fresh50k_v1/run_logs/Player-0.log`
- ONNX: `results/BossPPO_TerminalRewardFix_Timeout210_Fresh50k_v1/BossPlayer.onnx`
- Checkpoint: `results/BossPPO_TerminalRewardFix_Timeout210_Fresh50k_v1/BossPlayer/checkpoint.pt`
- Exported ONNX: `results/BossPPO_TerminalRewardFix_Timeout210_Fresh50k_v1/BossPlayer/BossPlayer-50103.onnx`
- TensorBoard event: `results/BossPPO_TerminalRewardFix_Timeout210_Fresh50k_v1/BossPlayer/events.out.tfevents.1780298369.KiKi.3816.0`

Mean reward by summary step:

| Step | Mean Reward |
| ---: | ---: |
| 5K | -16.308 |
| 10K | -15.319 |
| 15K | -15.597 |
| 20K | -15.414 |
| 25K | -16.938 |
| 30K | -13.193 |
| 35K | -13.112 |
| 40K | -15.004 |
| 45K | -17.801 |
| 50K | -20.691 |

Training metrics:

| Metric | Value |
| --- | ---: |
| Episodes | 70 |
| boss_dead | 0 |
| player_dead | 70 |
| timeout | 0 |
| terminal_reward lines | 70 |
| death=0.000 count | 0 |
| avg death reward | -8.000 |
| avg survival | 71.146s |
| max survival | 148.100s |
| survived >=10s | 70 / 70 |
| survived >=20s | 66 / 70 (94.286%) |
| boss_damage / episode | 10.729 |
| max_boss_damage | 26 / 60 |
| hit_rate | 100.000% |
| total attack actions | 751 |
| total hits | 751 |
| safe_attack_taken_ratio | 22.411% |
| attack_out_of_range | 0 |
| avoidable_hit | 65 |
| move_into_warn | 1690 |
| move_into_dmg | 50 |
| move_into_recent_warn | 1883 |
| move_into_recent_dmg | 137 |
| NaN lines | 0 |
| exception lines | 0 |
| communicator error lines | 0 |

Top damage training episodes:

| boss_damage | survival | terminal |
| ---: | ---: | --- |
| 26 | 124.3s | player_dead / -8 |
| 25 | 147.3s | player_dead / -8 |
| 22 | 115.3s | player_dead / -8 |
| 21 | 148.1s | player_dead / -8 |

해석:

- `death=0.000` 재발 없음. terminal reward fix는 런타임 로그 기준 정상 적용.
- 모든 episode가 `player_dead`로 종료되어 `boss_dead +5` reward는 이번 run에서 실제 발생하지 않았다.
- `attack_out_of_range = 0`으로 SafeActionMask 안전성은 유지.
- `hit_rate = 100%`지만 `safe_attack_taken_ratio = 22.411%`로 공격 기회 활용률이 낮다.
- `boss_damage/ep = 10.729`, `max_boss_damage = 26`으로 100K 확장 기준을 충족하지 못했다.

## 5. Eval210 ONNX inference

- Eval run ID: `BossPPO_TerminalRewardFix_Fresh50k_Eval210_v1`
- Eval command: `scripts/run_eval_210_inference_windows.ps1 -RunId BossPPO_TerminalRewardFix_Fresh50k_Eval210_v1 -TargetEpisodes 5 -MaxWallSeconds 1800`
- Eval episode count: 5
- Eval mode: ONNX InferenceOnly
- Eval timeout: 210초
- Eval ONNX source: `results/BossPPO_TerminalRewardFix_Timeout210_Fresh50k_v1/BossPlayer.onnx`
- Eval player log: `results/BossPPO_TerminalRewardFix_Fresh50k_Eval210_v1/run_logs/Player-0.log`
- Eval exit: 0

Eval metrics:

| Metric | Value |
| --- | ---: |
| Episodes | 5 |
| boss_dead | 0 |
| player_dead | 5 |
| timeout | 0 |
| survival times | 45.4s, 24.2s, 55.8s, 38.9s, 6.1s |
| boss_damage each | 6, 2, 7, 2, 2 |
| avg survival | 34.080s |
| max survival | 55.800s |
| avg boss_damage | 3.800 |
| max_boss_damage | 7 / 60 |
| hit_rate | 100.000% |
| total attack actions | 19 |
| total hits | 19 |
| safe_attack_taken_ratio | 14.844% |
| attack_out_of_range | 0 |
| avoidable_hit | 3 |
| move_into_warn | 56 |
| move_into_dmg | 3 |
| move_into_recent_warn | 68 |
| move_into_recent_dmg | 10 |
| terminal_reward lines | 5 |
| death=0.000 count | 0 |
| avg death reward | -8.000 |
| NaN lines | 0 |
| exception lines | 0 |

Eval episode rows:

| Episode | End reason | Survival | Boss damage | Death reward | Attacks/Hits | Safe opp/taken | OOR |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | player_dead | 45.4s | 6 | -8 | 6/6 | 33/6 | 0 |
| 2 | player_dead | 24.2s | 2 | -8 | 2/2 | 10/2 | 0 |
| 3 | player_dead | 55.8s | 7 | -8 | 7/7 | 45/7 | 0 |
| 4 | player_dead | 38.9s | 2 | -8 | 2/2 | 34/2 | 0 |
| 5 | player_dead | 6.1s | 2 | -8 | 2/2 | 6/2 | 0 |

Eval 해석:

- 210초에서도 `boss_dead`가 나오지 않았다.
- timeout은 발생하지 않았고 전부 `player_dead`다.
- Eval damage는 평균 3.8, 최대 7로 매우 낮다.
- 장판 밖에서 공격하는 `attack_out_of_range` 재발은 없다.
- 공격은 맞을 때만 나가지만, 안전 기회 대비 공격 선택률이 낮고 생존이 무너진다.

## 6. 이전 Fresh100K와 비교

주의: 이전 `BossPPO_Timeout210_Fresh100k_v1`은 death terminal penalty가 실제 terminal path에 적용되지 않은 상태로 학습되었으므로 최종 정책 근거로 쓰지 않는다. 비교는 회귀/방향성 참고용이다.

| Run | Training avg damage | Training max damage | Training safe ratio | Eval avg damage | Eval max damage | Death reward state |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Previous Fresh100K | 15.503 | 30 | 35.415% | 7.000 | 9 | terminal death missing |
| Current TerminalFix Fresh50K | 10.729 | 26 | 22.411% | 3.800 | 7 | death=-8 applied |

해석:

- terminal death penalty 적용 후 생존 평균은 크게 나빠지지 않았지만, 공격 빈도와 보스 데미지가 낮아졌다.
- 현재 결과는 100K 확장 조건을 만족하지 못한다.
- 기존 ONNX mismatch 의심은 여전히 남아 있다. 특히 stderr에 ONNX export warning이 있었고, 이전 run들에서도 training damage 대비 Eval ONNX damage가 낮았다. 다만 이번에는 training 자체도 낮기 때문에 단순 ONNX mismatch만으로 설명할 수 없다.

## 7. Decision tree 판단

적용된 분기: C.

근거:

- Training `boss_damage/ep = 10.729`, 기준 `15` 미만.
- Training `max_boss_damage = 26`, 기준 `30` 미만.
- Eval `avg_boss_damage = 3.800`, `max_boss_damage = 7`.
- `death=0.000`은 재발하지 않았으므로 D는 아님.
- Training이 충분한데 Eval만 낮은 상황이 아니므로 B 단독 판단은 아님.
- `boss_dead`가 없으므로 E는 아님.
- A의 100K 확장 조건인 training avg >=20, Eval avg >=15, Eval max >=25를 충족하지 못함.

결론:

- terminal reward fix는 성공.
- 하지만 terminal reward fix만으로는 성능 회복/개선이 부족하다.
- 지금 상태에서 100K로 바로 확장하지 않는 것이 맞다.
- 다음 단계는 reward/action/mask/observation을 바로 바꾸기 전에, terminal penalty 이후 정책이 안전 기회에서 공격을 덜 선택하는 원인을 metric/log/video로 분리하는 것이다.

## 8. 다음 단계 제안

1. 100K resume 금지.
2. 현재 Fresh50K 결과를 기준으로 safe opportunity 구간별 action histogram을 뽑아 공격 회피/대기/이동으로 흘러가는지 확인.
3. death terminal penalty 이후 `safe_attack_taken_ratio`가 22.411%까지 낮아진 원인을 먼저 분석.
4. ONNX mismatch는 별도 축으로 유지한다. 학습 중 Python policy와 exported ONNX inference의 action distribution 비교가 필요하다.
5. reward 조정이 허용되는 다음 단계가 된다면 우선순위 후보는 공격 기회 활용률 개선이다. 단, 이번 보고서 기준으로는 reward/action/mask/observation을 추가 수정하지 않았다.

## 9. 종료 상태

- Fresh50K trainer process: 종료됨.
- Eval210 EXE process: 종료됨.
- Port 5004/5005: 활성 connection 없음.
- NaN/crash/communicator error: 발견 없음.
- Git commit/push: 수행하지 않음. 현재 worktree에는 이번 결과 산출물과 이전 실험 파일들이 untracked/modified로 남아 있다.
