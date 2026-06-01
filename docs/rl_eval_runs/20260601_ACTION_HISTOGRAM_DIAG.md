# Action Histogram Diagnostic Report

작성 시각: 2026-06-01 KST

## 1. 범위와 수정 파일

이번 단계 목적은 성능 개선 학습이 아니라 `safe_attack_taken_ratio` 하락 원인 분리다.

- 작업 repo: `C:\Users\KiKi\Documents\Ql\AI_FinalTerm_MLAgents_PPO_NEW`
- Branch: `rl-wrapper-bugfix-danger-memory`
- Remote: `origin https://github.com/LxNx-Hn/AI_FinalTerm.git`
- CODE-BLUE 원본 repo/원본 로컬 폴더: 수정하지 않음.
- 추가 학습 범위: diagnostic 10K 이하만 수행.
- Reward 값 변경: 없음.
- Action spec 변경: 없음.
- Observation vector size 변경: 없음.
- SafeActionMask 제거/완화: 없음.
- Boss HP/패턴/데미지/플레이어 공격력 변경: 없음.

수정/추가 파일:

- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
  - action histogram, safe opportunity action histogram, distance/positioning, attack-allowed action metric 추가.
- `unity_project/Assets/Project/Scripts/RL/BossPlayerAgent.cs`
  - 선택 action, action 직전 boss distance, move distance delta를 logger에 전달.
- `unity_project/Assets/Project/Scripts/RL/BossRLStateExtractor.cs`
  - 읽기 전용 `ManhattanDistanceToBossAfterMove(Vector2Int dir)` helper 추가.
- `ml-agents-config/boss_ppo_action_histogram_diag_v1.yaml`
  - 기존 PPO 설정 유지, `max_steps: 10000`.
- `scripts/run_action_histogram_diag_windows.ps1`
  - diagnostic runner 추가. fresh only, no resume/no initialize-from.

Reward 값 확인:

- `BossDamagePerHp = 0.10`
- `BossKillReward = 5.0`
- `PlayerDeathPenalty = -8.0`
- `StepPenalty = -0.001`
- `SafeInRangeAttackAttemptReward = 0.05`

## 2. Build / Run

Build:

- Command: `scripts/build_rl_windows.ps1`
- Build log: `logs/build_windows.log`
- Result: exit 0, `Build succeeded`
- Compile error: 없음. `Exception` 검색 결과는 Unity package 파일명 matches뿐이었다.

Training diagnostic:

- Run ID: `BossPPO_ActionHistogram_Diag_v1`
- Config: `ml-agents-config/boss_ppo_action_histogram_diag_v1.yaml`
- Max steps: `10,000`
- Trainer exit code: `0`
- Player log: `results/BossPPO_ActionHistogram_Diag_v1/run_logs/Player-0.log`
- ONNX: `results/BossPPO_ActionHistogram_Diag_v1/BossPlayer.onnx`
- Checkpoint: `results/BossPPO_ActionHistogram_Diag_v1/BossPlayer/checkpoint.pt`
- Mean reward: 5K `-13.114`, 10K `-13.899`
- NaN: 0
- Exception: 0
- Communicator error: 0

Eval210:

- Eval build command: `scripts/build_eval_210_windows.ps1 -CloseUnity -ModelSource "results\BossPPO_ActionHistogram_Diag_v1\BossPlayer.onnx" -ModelLabel "ActionHistogramDiag"`
- Eval build log: `logs/build_eval_210_ActionHistogramDiag.log`
- Eval run ID: `BossPPO_ActionHistogram_Eval210_v1`
- Eval episodes: 5
- Eval player log: `results/BossPPO_ActionHistogram_Eval210_v1/run_logs/Player-0.log`
- Eval exit: 0

## 3. Training Diagnostic Metrics

| Metric | Value |
| --- | ---: |
| Episodes | 18 |
| player_dead / timeout / boss_dead | 18 / 0 / 0 |
| Avg survival | 53.800s |
| Max survival | 98.500s |
| boss_damage/ep | 8.111 |
| max_boss_damage | 18 / 60 |
| Attack actions / hits | 146 / 146 |
| hit_rate | 100.000% |
| attack_out_of_range | 0 |
| terminal_reward_player_dead_count | 18 |
| terminal_reward_player_dead_sum | -144.000 |
| death=0.000 recurrence | 0 |

Safe opportunity:

| Metric | Value |
| --- | ---: |
| safe_opp_total_steps | 679 |
| safe_opp_attack_count | 146 |
| safe_opp_wait_count | 135 |
| safe_opp_move_count | 398 |
| safe_opp_missed_count | 533 |
| safe_opp_attack_ratio | 21.502% |
| safe_opp_wait_ratio | 19.882% |
| safe_opp_move_ratio | 58.616% |
| safe_opp_move_toward_boss_count | 118 |
| safe_opp_move_away_from_boss_count | 127 |
| safe_opp_move_lateral_count | 153 |

Position / mask:

| Metric | Value |
| --- | ---: |
| avg_distance_to_boss episode mean | 3.467 |
| time_in_attack_range_steps | 1048 |
| time_out_of_attack_range_steps | 8636 |
| entered_attack_range_count | 612 |
| left_attack_range_count | 610 |
| attack_allowed_count | 679 |
| chosen_attack_when_allowed_count | 146 |
| missed_allowed_attack_count | 533 |
| attack_masked_not_ready_count | 730 |
| attack_masked_out_of_range_count | 8135 |
| attack_masked_player_on_danger_count | 140 |

Danger context action histogram:

| Metric | Value |
| --- | ---: |
| danger_nearby_steps | 4323 |
| danger_nearby_wait_count | 1028 |
| danger_nearby_safe_move_count | 1515 |
| danger_nearby_danger_move_count | 388 |
| danger_nearby_attack_count | 50 |
| avoidable_hit | 16 |
| move_into_warning | 349 |
| move_into_damage | 15 |
| move_into_recent_warning | 388 |
| move_into_recent_damage | 33 |

Overall action histogram:

| Action | Count |
| --- | ---: |
| WAIT | 2251 |
| MOVE_UP | 1726 |
| MOVE_DOWN | 1827 |
| MOVE_LEFT | 1921 |
| MOVE_RIGHT | 1813 |
| ATTACK | 146 |

## 4. Eval210 Metrics

| Metric | Value |
| --- | ---: |
| Episodes | 5 |
| player_dead / timeout / boss_dead | 5 / 0 / 0 |
| Survival times | 27.4s, 75.4s, 16.4s, 13.4s, 23.5s |
| boss_damage each | 7, 33, 4, 6, 9 |
| Avg boss_damage | 11.800 |
| Max boss_damage | 33 / 60 |
| Attack actions / hits | 59 / 59 |
| hit_rate | 100.000% |
| attack_out_of_range | 0 |
| terminal_reward_player_dead_count | 5 |
| terminal_reward_player_dead_sum | -40.000 |
| death=0.000 recurrence | 0 |

Eval safe opportunity:

| Metric | Value |
| --- | ---: |
| safe_opp_total_steps | 125 |
| safe_opp_attack_count | 59 |
| safe_opp_wait_count | 1 |
| safe_opp_move_count | 65 |
| safe_opp_missed_count | 66 |
| safe_opp_attack_ratio | 47.200% |
| safe_opp_wait_ratio | 0.800% |
| safe_opp_move_ratio | 52.000% |
| move_toward_boss | 21 |
| move_away_from_boss | 13 |
| move_lateral | 31 |

Eval mask / positioning:

| Metric | Value |
| --- | ---: |
| avg_distance_to_boss episode mean | 3.052 |
| time_in_attack_range_steps | 281 |
| time_out_of_attack_range_steps | 1283 |
| attack_allowed_count | 125 |
| chosen_attack_when_allowed_count | 59 |
| missed_allowed_attack_count | 66 |
| attack_masked_not_ready_count | 295 |
| attack_masked_out_of_range_count | 1112 |
| attack_masked_player_on_danger_count | 32 |

## 5. 원인 판단

핵심 결론:

- `safe opportunity`는 존재한다. Training diagnostic에서 679회, Eval210에서 125회 확인됐다.
- 공격이 mask에 의해 막혀서 못 하는 상황이 주 원인은 아니다. `attack_allowed_count == safe_opp_total_steps`이고, 그중 선택 공격은 training 146/679, Eval 59/125다.
- Training diagnostic에서 safe opportunity missed 533회 중 MOVE가 398회로 가장 크다. WAIT 135회보다 MOVE 선택 문제가 더 크다.
- MOVE 방향은 training 기준 lateral 153, away 127, toward 118로 흩어져 있다. 순수하게 도망만 가는 것은 아니지만, 공격 가능 상태에서도 포지셔닝/회피 move value가 공격보다 높다.
- `attack_out_of_range = 0`, `hit_rate = 100%`이므로 SafeActionMask의 안전성은 유지되고 있다.
- Eval ONNX는 training보다 safe_opp_attack_ratio가 낮지 않다. Training 21.5%, Eval 47.2%로 이번 diagnostic 기준 F, 즉 ONNX가 공격을 더 안 한다는 패턴은 아니다.

Decision 기준 적용:

- B에 가장 가깝다: safe opportunity가 많고 MOVE 비율이 높다.
- D도 일부 해당한다: attack_allowed_count는 높은데 chosen_attack_when_allowed가 낮다.
- A는 아님: WAIT보다 MOVE가 더 큰 병목이다.
- C는 아님: safe opportunity 자체가 0이거나 매우 희소한 상태는 아니다.
- E는 주 원인이 아님: attack_allowed_count 자체는 충분히 잡힌다. mask가 공격을 과도하게 막는 증거는 약하다.
- F는 이번 diagnostic에서는 아님: ONNX Eval safe_opp_attack_ratio가 training보다 더 높다.

## 6. 다음 단계 제안

1. 현재 상태에서 100K 확장하지 않는다.
2. reward를 즉시 바꾸기 전에, safe opportunity에서 MOVE를 고른 직후 실제로 위험을 피했는지 또는 공격 타이밍만 잃었는지 episode trace/영상으로 5~10개 샘플링한다.
3. reward 조정이 허용되는 다음 단계라면 `SafeInRangeAttackAttemptReward` 강화 또는 `missed_safe_attack_opportunity_penalty` 후보가 타당하다. 현재 수치상 문제는 mask가 아니라 allowed attack을 policy가 선택하지 않는 쪽이다.
4. 다만 이번 단계에서는 reward/action/mask/observation 값을 변경하지 않았고, 추가 학습도 diagnostic 10K 이하만 수행했다.
