# Safe Opportunity MOVE Trace + Reward 50K Report

작성 시각: 2026-06-01 KST

## 1. 범위와 수정 파일

목표는 safe opportunity에서 MOVE가 합리적 회피인지 trace로 분리하고, 의미 없는 MOVE가 대부분일 때만 소폭 reward 조정을 적용하는 것이었다.

- CODE-BLUE 원본 repo/원본 로컬 폴더: 수정하지 않음.
- Boss HP/패턴/데미지/플레이어 공격력: 변경 없음.
- PlayerCombat/GridMover/GridManager 핵심 로직: 변경 없음.
- Action spec / observation vector size / LSTM: 변경 없음.
- SafeActionMask: 유지.
- Terminal reward fix: 유지.
- Timeout: 210s 유지.

수정/추가 파일:

- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
  - safe opportunity MOVE trace 샘플링 및 0.5s/1s/2s 후 결과 분류 metric 추가.
- `unity_project/Assets/Project/Scripts/RL/BossPlayerAgent.cs`
  - trace context 전달, delayed missed-safe-opportunity penalty 적용.
- `unity_project/Assets/Project/Scripts/RL/BossRLStateExtractor.cs`
  - read-only cell/distance/range helper 추가.
- `unity_project/Assets/Project/Scripts/RL/BossRLReward.cs`
  - Case A 판정 후 `SafeInRangeAttackAttemptReward`를 `0.05 -> 0.08`로 소폭 상향.
  - `MissedSafeAttackOpportunityPenalty = -0.003` 유지, 즉시 적용 대신 `danger_nearby=false` 및 1초 안에 피격 없음 조건으로 지연 적용.
- `ml-agents-config/boss_ppo_safe_opp_move_trace_diag_v1.yaml`
- `scripts/run_safe_opp_move_trace_diag_windows.ps1`
- `ml-agents-config/boss_ppo_safe_opp_reward_50k_v1.yaml`
- `scripts/run_safe_opp_reward_50k_windows.ps1`

## 2. Trace Diagnostic

- Run ID: `BossPPO_SafeOppMoveTrace_Diag_v1`
- Max steps: `10,000`
- Reward 변경 여부: trace diagnostic 자체는 reward 변경 전 상태로 실행.
- Trainer exit code: `0`
- Player log: `results/BossPPO_SafeOppMoveTrace_Diag_v1/run_logs/Player-0.log`
- ONNX: `results/BossPPO_SafeOppMoveTrace_Diag_v1/BossPlayer.onnx`
- NaN / exception / communicator error: 0

Trace summary:

| Metric | Value |
| --- | ---: |
| Episodes | 20 |
| player_dead / timeout / boss_dead | 20 / 0 / 0 |
| boss_damage/ep | 6.500 |
| max_boss_damage | 16 / 60 |
| hit_rate | 100.000% |
| attack_out_of_range | 0 |
| safe_opp_total | 690 |
| safe_opp_attack_count | 130 |
| safe_opp_move_count | 425 |
| safe_opp_attack_ratio | 18.841% |
| trace sample count | 50 |
| safe_opp_move_useful_escape_count | 14 / 50 samples |
| safe_opp_move_kept_attack_range_count | 159 / 425 moves |
| safe_opp_move_left_attack_range_count | 266 / 425 moves |
| safe_opp_move_away_without_danger_count | 107 / 425 moves |
| safe_opp_move_then_hit_within_2s_count | 1 / 50 samples |
| safe_opp_move_then_no_hit_no_damage_count | 42 / 50 samples |
| safe_opp_attack_would_have_been_allowed_count | 425 |

판정:

- Case A로 판단했다.
- 근거는 `safe_opp_move_left_attack_range_count = 266 / 425`가 높고, 샘플 50개 중 `42`개가 2초 안에 피격도 없고 0.5초 내 boss damage도 없었다.
- MOVE가 전부 나쁜 것은 아니다. `useful_escape = 14 / 50` 샘플은 존재한다.
- 하지만 다수는 공격 가능 상태에서 공격 범위를 이탈하거나, 딜/회피 이득 없이 턴을 소비했다.

## 3. Reward Adjustment

Case A 기준으로만 reward를 소폭 조정했다.

- `SafeInRangeAttackAttemptReward: 0.05 -> 0.08`
- `MissedSafeAttackOpportunityPenalty: -0.003` 유지
- Missed penalty 조건 제한:
  - safe opportunity
  - action != ATTACK
  - `danger_nearby == false`
  - 이후 1초 안에 player hit 없음
- DeathPenalty, BossDamagePerHp, BossKillReward: 변경 없음.

Build:

- Command: `scripts/build_rl_windows.ps1`
- Build result: exit 0, build succeeded.
- Compile error: 없음.

## 4. Reward 50K Training

- Run ID: `BossPPO_SafeOppReward_50k_v1`
- Max steps: `50,000`
- Trainer exit code: `0`
- Config: `ml-agents-config/boss_ppo_safe_opp_reward_50k_v1.yaml`
- Player log: `results/BossPPO_SafeOppReward_50k_v1/run_logs/Player-0.log`
- ONNX: `results/BossPPO_SafeOppReward_50k_v1/BossPlayer.onnx`
- Checkpoint: `results/BossPPO_SafeOppReward_50k_v1/BossPlayer/checkpoint.pt`
- Exported ONNX: `results/BossPPO_SafeOppReward_50k_v1/BossPlayer/BossPlayer-50056.onnx`
- TensorBoard event: `results/BossPPO_SafeOppReward_50k_v1/BossPlayer/events.out.tfevents.1780304634.KiKi.31608.0`
- NaN / exception / communicator error: 0

Mean reward:

| Step | Mean Reward |
| ---: | ---: |
| 5K | -17.394 |
| 10K | -13.115 |
| 15K | -13.052 |
| 20K | -14.126 |
| 25K | -15.969 |
| 30K | -13.687 |
| 35K | -17.027 |
| 40K | -14.383 |
| 45K | -18.917 |
| 50K | -19.323 |

Training metrics:

| Metric | Value |
| --- | ---: |
| Episodes | 68 |
| player_dead / timeout / boss_dead | 68 / 0 / 0 |
| Avg survival | 73.549s |
| Max survival | 168.600s |
| survived20s | 67 / 68 (98.529%) |
| boss_damage/ep | 12.015 |
| max_boss_damage | 25 / 60 |
| attack actions / hits | 817 / 817 |
| hit_rate | 100.000% |
| safe_opp_total | 3295 |
| safe_opp_attack_count | 817 |
| safe_opp_wait_count | 600 |
| safe_opp_move_count | 1878 |
| safe_opp_attack_ratio | 24.795% |
| attack_out_of_range | 0 |
| avoidable_hit | 56 |
| death=-8 유지 | yes |
| death=0.000 recurrence | 0 |

Training 해석:

- 안전성은 유지됐다. `hit_rate=100%`, `attack_out_of_range=0`, `death=-8` 유지.
- `safe_opp_attack_ratio`는 trace diagnostic 18.841% 및 이전 TerminalFix Fresh50K 22.411%보다 약간 높다.
- `boss_damage/ep=12.015`는 TerminalFix Fresh50K 10.729보다 상승했지만, 목표 수준인 20+에는 한참 부족하다.
- `max_boss_damage=25`는 이전 TerminalFix Fresh50K max 26과 유사하거나 약간 낮다.
- 생존은 무너지지 않았다. `survived20s=98.529%`.

## 5. Eval210

- Eval build: `logs/build_eval_210_SafeOppReward50k.log`
- Eval run ID: `BossPPO_SafeOppReward_50k_Eval210_v1`
- Eval ONNX source: `results/BossPPO_SafeOppReward_50k_v1/BossPlayer.onnx`
- Eval player log: `results/BossPPO_SafeOppReward_50k_Eval210_v1/run_logs/Player-0.log`
- Eval episodes: 5
- Eval exit: 0

Eval metrics:

| Metric | Value |
| --- | ---: |
| player_dead / timeout / boss_dead | 5 / 0 / 0 |
| Survival times | 24.3s, 53.9s, 72.2s, 33.2s, 43.6s |
| boss_damage each | 3, 2, 6, 2, 6 |
| Avg boss_damage | 3.800 |
| Max boss_damage | 6 / 60 |
| attack actions / hits | 19 / 19 |
| hit_rate | 100.000% |
| safe_opp_total | 147 |
| safe_opp_attack_count | 19 |
| safe_opp_wait_count | 46 |
| safe_opp_move_count | 82 |
| safe_opp_attack_ratio | 12.925% |
| attack_out_of_range | 0 |
| avoidable_hit | 5 |
| death=0.000 recurrence | 0 |

Eval 해석:

- Eval은 개선되지 않았다.
- Training에서는 safe attack ratio가 약간 상승했지만, Eval ONNX에서는 `12.925%`로 낮다.
- Boss damage도 평균 3.8, 최대 6이라 이전 Fresh50K Eval 수준과 유사하게 낮다.
- 이번 결과는 reward 조정만으로는 ONNX inference 정책 성능이 살아나지 않는다는 증거다.

## 6. 결론과 다음 단계

결론:

- Trace로 확인한 원인은 Case A다. safe opportunity MOVE 중 상당수가 공격 범위 이탈 또는 no-hit/no-damage 턴 소비였다.
- 소폭 reward 조정은 training 안전성을 해치지 않았지만, 성능 개선 폭은 작았다.
- 50K training 기준 `boss_damage/ep`는 10.729 -> 12.015로 소폭 상승했지만, `max_boss_damage`는 26 -> 25로 개선되지 않았다.
- Eval210 기준 성능은 개선되지 않았다. `avg boss_damage=3.8`, `max=6`.
- 따라서 지금 상태에서 100K 확장은 권장하지 않는다.

다음 단계 제안:

1. ONNX inference에서 safe_opp_attack_ratio가 training보다 낮게 무너지는 원인을 우선 분리한다.
2. 가능하면 동일 checkpoint를 Python/PyTorch policy로 평가하거나, ONNX export/eval mode 문제를 별도 확인한다.
3. reward를 추가로 더 키우기 전에, deterministic ONNX action distribution과 trainer-side action distribution을 비교한다.
4. 추가 reward 변경은 보류한다. 현재 소폭 조정만으로는 boss_damage 목표에 도달하지 못했다.

종료 상태:

- Trainer/EXE process: 종료됨.
- Port 5004/5005: 활성 connection 없음.
- Git commit/push: 수행하지 않음.
