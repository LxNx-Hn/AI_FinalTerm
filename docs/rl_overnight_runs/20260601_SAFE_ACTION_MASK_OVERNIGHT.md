# 2026-06-01 SafeActionMask PPO overnight report

## Summary

- 작업 대상: `C:\Users\KiKi\Documents\Ql\AI_FinalTerm_MLAgents_PPO_NEW`
- Git branch: `rl-wrapper-bugfix-danger-memory`
- Git remote: `https://github.com/LxNx-Hn/AI_FinalTerm.git`
- CODE-BLUE 원본 폴더/원격 수정: 없음
- reward/action/mask/observation 코드 수정: 없음
- 최종 decision branch: Phase 1 결과가 좋음(A) -> reward 수정 없이 100K 확장

## Preflight evidence

- `git rev-parse --show-toplevel`: `C:/Users/KiKi/Documents/Ql/AI_FinalTerm_MLAgents_PPO_NEW`
- `git remote -v`: `origin https://github.com/LxNx-Hn/AI_FinalTerm.git`
- 최신 commit: `7caa52b feat: SafeActionMask - danger-level movement mask + ATTACK mask by range/danger`
- Unity version: `6000.3.10f1`
- ML-Agents Unity package: `com.unity.ml-agents` `4.0.3`
- Scene YAML 확인: `Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity`
- BehaviorParameters: VectorObservationSize `193`, Discrete BranchSizes `06000000` (Single Discrete [6])
- DecisionRequester: `TakeActionsBetweenDecisions: 0`
- Reset: `BossRLEpisodeResetter.cs`에서 `SceneManager.LoadSceneAsync(sceneName)` 사용
- Reward 확인: `SafeInRangeAttackAttemptReward = 0.02f`
- EXE: `builds/windows/BossPPO_RLTrain/BossPPO_RLTrain.exe`
- Assembly-CSharp.dll timestamp: `2026-06-01 04:18:10`
- Codex 세션에서 Unity MCP active scene/play mode는 별도 callable Unity MCP tool이 없어 직접 조회하지 못함. 대신 빌드 EXE Player log와 scene YAML로 확인함.

## Runs

### 1. Claude-started 50K attempt

- run-id: `BossPPO_SafeActionMask_50k_v1`
- max_steps: `50,000`
- init-from: `BossPPO_SafeActionMask_v1`
- 결과: 실패, 완료 아님
- trainer exit: 정상 완료 기록 없음
- 원인 근거:
  - `logs/trainer_safe_mask_50k_v1.log`
  - `logs/trainer_safe_mask_50k_v1.err.log`
  - `results/BossPPO_SafeActionMask_50k_v1/run_logs/Player-0.log`
- 증상:
  - 첫 episode 이후 Unity worker 응답 지연 경고
  - `BrokenPipeError [WinError 109]`
  - `EOFError`
  - ONNX/checkpoint/training_status 생성 없음
- 로그 보존: 기존 partial `results/BossPPO_SafeActionMask_50k_v1`을 덮어쓰지 않음

### 2. 50K retry

- run-id: `BossPPO_SafeActionMask_50k_v1_retry1`
- max_steps: `50,000`
- init-from: `BossPPO_SafeActionMask_v1`
- reward/action/mask/observation 변경: 없음
- trainer exit code: `0`
- reward NaN: 없음
- communicator timeout/BrokenPipe/EOFError 재발: 없음
- EXE Not Responding: 없음
- ONNX:
  - `results/BossPPO_SafeActionMask_50k_v1_retry1/BossPlayer.onnx`
  - `results/BossPPO_SafeActionMask_50k_v1_retry1/BossPlayer/BossPlayer-50090.onnx`
- checkpoint:
  - `results/BossPPO_SafeActionMask_50k_v1_retry1/BossPlayer/checkpoint.pt`
  - `results/BossPPO_SafeActionMask_50k_v1_retry1/BossPlayer/BossPlayer-50090.pt`
- trainer mean reward:
  - 5K `-16.768`
  - 10K `-8.621`
  - 15K `-11.203`
  - 20K `-11.460`
  - 25K `-12.111`
  - 30K `-10.927`
  - 35K `-11.240`
  - 40K `-16.401`
  - 45K `-12.089`
  - 50K `-8.420`

50K metrics from Player log:

| metric | value |
| --- | ---: |
| episodes | 74 |
| player_dead | 46 |
| TIMEOUT | 28 |
| boss_dead | 0 |
| avg_survival | 67.615s |
| survived10s | 100.0% |
| survived20s | 98.649% |
| attack_action_count | 887 |
| successful_hit_count | 887 |
| hit_rate | 100.0% |
| attack_out_of_range_count | 0 |
| attack_on_cooldown | 0 |
| attack_masked_out_of_range_count | 41456 |
| safe_in_range_attack_allowed_count | 3506 |
| safe_attack_taken_count | 887 |
| safe_attack_taken_ratio | 25.299% |
| boss_damage_total | 887 |
| boss_damage_per_episode | 11.986 |
| max_boss_damage_episode | 26 |
| avoidable_hit_count | 57 |
| avoidable_hit_per_episode | 0.770 |
| moved_into_boss_warning | 1661 |
| moved_into_boss_damage | 48 |
| hit_on_boss_warning_ratio | 53.731% |
| warning_to_damage_hit | 59 |
| recent_warning_reentry | 1869 |

50K decision:

- hit_rate `100%`
- survived20s `98.649%`
- boss_damage/ep `11.986`
- TIMEOUT 지속
- avoidable_hit/ep `0.770`
- attack_out_of_range `0`
- movement/action 붕괴 로그 없음

따라서 decision tree A 조건으로 판정했고 reward 수정 없이 100K로 확장했다.

### 3. 100K extension

- run-id: `BossPPO_SafeActionMask_100k_v1`
- max_steps: `100,000`
- init-from: `BossPPO_SafeActionMask_50k_v1_retry1`
- reward/action/mask/observation 변경: 없음
- trainer exit code: `0`
- reward NaN: 없음
- communicator timeout/BrokenPipe/EOFError: 없음
- EXE Not Responding: 없음
- ONNX:
  - `results/BossPPO_SafeActionMask_100k_v1/BossPlayer.onnx`
  - `results/BossPPO_SafeActionMask_100k_v1/BossPlayer/BossPlayer-100045.onnx`
- checkpoint:
  - `results/BossPPO_SafeActionMask_100k_v1/BossPlayer/checkpoint.pt`
  - `results/BossPPO_SafeActionMask_100k_v1/BossPlayer/BossPlayer-100045.pt`
- trainer_status final reward: `-12.308099653385579`
- trainer mean reward:
  - 5K `-12.374`
  - 10K `-15.937`
  - 15K `-15.565`
  - 20K `-12.890`
  - 25K `-10.443`
  - 30K `-10.754`
  - 35K `-10.554`
  - 40K `-11.905`
  - 45K `-14.672`
  - 50K `-10.707`
  - 55K `-10.141`
  - 60K `-13.185`
  - 65K `-12.467`
  - 70K `-9.559`
  - 75K `-13.887`
  - 80K `-15.676`
  - 85K `-10.749`
  - 90K `-14.723`
  - 95K `-10.682`
  - 100K `-12.974`

100K metrics from Player log:

| metric | value |
| --- | ---: |
| episodes | 125 |
| player_dead | 66 |
| TIMEOUT | 59 |
| boss_dead | 0 |
| avg_survival | 79.450s |
| survived10s | 100.0% |
| survived20s | 100.0% |
| attack_action_count | 2866 |
| successful_hit_count | 2865 |
| hit_rate | 99.965% |
| attack_out_of_range_count | 0 |
| attack_on_cooldown | 0 |
| attack_masked_out_of_range_count | 78976 |
| safe_in_range_attack_allowed_count | 5264 |
| safe_attack_taken_count | 2866 |
| safe_attack_taken_ratio | 54.445% |
| boss_damage_total | 2865 |
| boss_damage_per_episode | 22.920 |
| max_boss_damage_episode | 36 |
| avoidable_hit_count | 89 |
| avoidable_hit_per_episode | 0.712 |
| moved_into_boss_warning | 3308 |
| moved_into_boss_damage | 75 |
| hit_on_boss_warning_ratio | 28.899% |
| warning_to_damage_hit | 154 |
| recent_warning_reentry | 3680 |

## Decision result

100K 결과는 안정적이고 50K 대비 공격 기회 활용률과 boss damage가 크게 증가했다.

- safe_attack_taken_ratio: `25.299%` -> `54.445%`
- boss_damage/ep: `11.986` -> `22.920`
- max_boss_damage_episode: `26` -> `36`
- avg_survival: `67.615s` -> `79.450s`
- survived20s: `98.649%` -> `100.0%`
- hit_rate: `100.0%` -> `99.965%`
- avoidable_hit/ep: `0.770` -> `0.712`
- boss_dead: `0` 유지

Decision tree상 "100K 결과가 안정적이지만 boss kill이 없음"에 해당한다. 밤새 자동 수정은 하지 않고, 추가 reward/action/mask/observation 수정 없이 중단한다.

## Best policy recommendation

현재 기준 추천 best policy:

- `results/BossPPO_SafeActionMask_100k_v1/BossPlayer.onnx`
- 보조 checkpoint: `results/BossPPO_SafeActionMask_100k_v1/BossPlayer/checkpoint.pt`

이유:

- 100K가 50K보다 boss_damage/ep와 safe_attack_taken_ratio가 크게 개선됨
- hit_rate, out_of_range, cooldown 안정성 유지
- survival이 개선되고 TIMEOUT도 지속됨

## Remaining risks and next checks

- boss_dead가 여전히 `0`이므로 "클리어 학습 성공"으로 보고하면 안 된다.
- 평균 보스 딜은 올랐지만 60 HP boss 기준 평균 `22.92` HP, 최대 `36` HP라 kill까지는 아직 차이가 있다.
- ONNX export 중 opset conversion 관련 warning/RuntimeError stack이 stderr에 출력되지만 trainer exit code는 `0`이고 ONNX 파일은 생성/복사되었다. 실제 Unity inference 로드 검증이 필요하다.
- `BossPPO_SafeActionMask_50k_v1` 원 run은 BrokenPipe/EOFError로 실패했으므로 해당 결과는 성능 평가에 쓰면 안 된다.
- Codex에서 Unity MCP active scene/play mode를 직접 조회하지 못했으므로, 사람이 Unity Editor에서 active scene과 model inference smoke를 확인하는 것이 좋다.
- 다음 권장 검증:
  - 100K ONNX를 BehaviorParameters에 연결해 inference smoke
  - 5~10 episode 영상/로그 확인
  - boss HP near-kill episode가 있는지 episode별 boss damage max 분석
  - 이후에는 자동 수정 없이 episode timeout 연장, reward scaling, evaluation protocol을 별도 논의

## Files and logs

- 50K failed logs:
  - `logs/trainer_safe_mask_50k_v1.log`
  - `logs/trainer_safe_mask_50k_v1.err.log`
  - `results/BossPPO_SafeActionMask_50k_v1/run_logs/Player-0.log`
- 50K retry logs:
  - `logs/trainer_safe_mask_50k_v1_retry1.log`
  - `logs/trainer_safe_mask_50k_v1_retry1.err.log`
  - `results/BossPPO_SafeActionMask_50k_v1_retry1/run_logs/Player-0.log`
  - `results/BossPPO_SafeActionMask_50k_v1_retry1/run_logs/training_status.json`
- 100K logs:
  - `logs/trainer_safe_mask_100k_v1.log`
  - `logs/trainer_safe_mask_100k_v1.err.log`
  - `results/BossPPO_SafeActionMask_100k_v1/run_logs/Player-0.log`
  - `results/BossPPO_SafeActionMask_100k_v1/run_logs/training_status.json`

## Prohibition checklist

- CODE-BLUE 원본 폴더 수정: 없음
- CODE-BLUE 원격 push/pull/reset/clean: 없음
- reward/action/mask/observation 수정: 없음
- SafeInRangeAttackAttemptReward 변경: 없음, `0.02` 유지
- 보스 패턴/판정/PlayerCombat/GridMover/GridManager 수정: 없음
- LSTM/current_pattern/boss_hurtbox_active observation 추가: 없음
- ClearRun 이름 사용: 없음
- 성능 과장: boss_dead `0`으로 명시
