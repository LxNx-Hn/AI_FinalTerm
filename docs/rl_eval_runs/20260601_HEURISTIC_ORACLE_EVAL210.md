# Heuristic Oracle Eval210 Report

작성 시각: 2026-06-01 KST

## 1. 범위와 수정 파일

이번 단계는 학습이 아니라, 현재 action/mask/observation 구조로 보스전이 실제 가능한지 확인하는 Heuristic Oracle 진단이다.

금지사항 준수:

- CODE-BLUE 원본 repo/원본 로컬 폴더: 수정하지 않음.
- 보스 HP/패턴/난이도/플레이어 공격력: 변경 없음.
- WarningTile / DamageTile 판정: 변경 없음.
- PlayerCombat / GridMover / GridManager 핵심 로직: 변경 없음.
- SafeActionMask / terminal reward fix: 유지.
- observation에 `current_pattern`, `boss_hurtbox_active` 추가: 없음.
- PPO 50K/100K 추가 학습: 없음.
- reward 추가 튜닝: 없음.
- LSTM 추가: 없음.

수정 파일:

- `unity_project/Assets/Project/Scripts/RL/BossPlayerAgent.cs`
  - `Heuristic()`에 Heuristic Oracle action rule 추가.
  - 학습/Inference 경로는 그대로 두고 BehaviorType=HeuristicOnly에서만 실제 사용.
- `unity_project/Assets/Project/Scripts/RL/BossRLStateExtractor.cs`
  - read-only helper 추가: `BossFacing`, `PlayerWorldCell`, `BossWorldCell`, `IsBossInAttackRangeAfterMove`.
- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
  - warning-on-player 대응 metric과 boss-facing diagnostic summary 추가.
- `unity_project/Assets/Project/Scripts/Editor/RLEval210BatchBuild.cs`
  - `-heuristicOnly` 인자로 Eval scene을 `BehaviorType.HeuristicOnly`로 빌드 가능하게 확장.
- `scripts/build_eval_210_windows.ps1`
  - `-HeuristicOnly` switch 추가.

## 2. Heuristic Rule

Heuristic Oracle rule:

1. 현재 플레이어 칸이 warning / damage / recent danger이면 ATTACK 금지.
2. danger 위에 있고 이동 가능하면 가장 낮은 danger level의 move를 선택.
3. 현재 안전하고 `attack_ready && boss_in_attack_range`이면 ATTACK.
4. 공격 불가능하면 safe move 중 이동 후 `boss_in_attack_range`가 되는 칸을 우선.
5. 그다음 boss distance를 개선하는 safe move 선택.
6. WAIT은 최후 선택.
7. 사거리 밖 공격, warning 위 공격, safe alternative가 있는데 danger move 선택은 피하도록 구성.

## 3. Build / Eval

- Build command: `scripts/build_eval_210_windows.ps1 -CloseUnity -HeuristicOnly -ModelLabel "HeuristicOracle"`
- Build log: `logs/build_eval_210_HeuristicOracle.log`
- Build result: exit 0, `Build SUCCEEDED`
- BehaviorType 확인: `HeuristicOnly`
- Eval run ID: `BossPPO_HeuristicOracle_Eval210_v1`
- Eval episodes: 10
- Eval player log: `results/BossPPO_HeuristicOracle_Eval210_v1/run_logs/Player-0.log`
- Eval exit: 0
- NaN / exception / communicator error: 0

## 4. Heuristic Oracle Result

| Metric | Value |
| --- | ---: |
| Episodes | 10 |
| boss_dead / player_dead / timeout | 0 / 10 / 0 |
| Survival times | 62.2, 47.4, 63.4, 64.3, 50.8, 61.6, 62.3, 43.4, 61.4, 49.0 |
| Avg survival | 56.580s |
| Max survival | 64.300s |
| Boss damage each | 38, 27, 34, 41, 35, 40, 40, 28, 40, 28 |
| Avg boss_damage | 35.100 |
| Max boss_damage | 41 / 60 |
| Attack actions / hits | 351 / 351 |
| hit_rate | 100.000% |
| attack_out_of_range | 0 |
| moved_into_warning | 329 |
| moved_into_damage | 13 |
| moved_into_recent_warning | 386 |
| moved_into_recent_damage | 39 |
| avoidable_hit | 2 |
| death=0.000 recurrence | 0 |

Heuristic 판단:

- Heuristic은 PPO보다 훨씬 잘 때린다. 최근 PPO Eval210은 평균 damage 3.8 수준이었지만, Heuristic은 평균 35.1, 최대 41까지 도달했다.
- 다만 `boss_dead` 또는 50+ damage에는 도달하지 못했다.
- 따라서 “환경이 완전히 불가능”하다고 보기는 어렵지만, 현재 단순 action/mask/observation 구조와 반응 규칙만으로 안정 클리어 가능한지는 아직 증명되지 않았다.
- 판정은 A와 B의 중간이다. 30 이하 실패는 아니지만, 50+ 또는 클리어 성공도 아니다. 즉, PPO formulation 문제는 강하지만, 후반/패턴/facing/timing 해석 문제도 남아 있다.

## 5. Warning-On-Player Diagnostic

| Metric | Value |
| --- | ---: |
| warning_spawn_on_player_count | 308 |
| warning_spawn_on_player_safe_move_available_count | 46 |
| warning_spawn_on_player_escape_success_count | 286 |
| warning_spawn_on_player_escape_fail_count | 17 |
| warning_spawn_on_player_hit_count | 17 |
| warning_spawn_on_player_chosen_wait_count | 120 |
| warning_spawn_on_player_chosen_attack_count | 0 |
| warning_spawn_on_player_chosen_move_count | 188 |
| warning_spawn_to_damage_frames_avg | 66.100 |
| decision_available_before_damage_count | 17 |
| no_decision_before_damage_count | 0 |

Warning 대응 판단:

- `no_decision_before_damage_count=0`이므로, 이번 로그 기준으로는 warning이 뜨고 decision을 전혀 못 받아서 맞는 구조적 DecisionPeriod 문제는 강하지 않다.
- warning_spawn_on_player 308건 중 escape_success 286건으로 대부분은 대응 가능했다.
- hit 17건은 전부 decision 기회가 있었던 뒤에 발생했다.
- safe_move_available은 46건으로 제한적이다. 즉, 현재 칸에 warning이 생겼을 때 항상 깔끔한 safe escape가 있는 것은 아니다.
- Heuristic이 warning 위에서 공격한 케이스는 0이다.

## 6. Boss Facing / Pattern Diagnostic

| Metric | Value |
| --- | ---: |
| boss_facing_changed_between_warning_and_damage_count | 16 |
| hit_when_boss_facing_changed_count | 16 |
| hit_by_original_warning_direction_count | 1 |
| hit_by_rotated_damage_direction_count | 16 |
| pattern_hit_count_by_type | unknown=17 |

해석:

- 현재 diagnostic은 별도 패턴 이벤트 ID를 추가한 것이 아니라, 플레이어 칸 warning을 처음 관측한 decision 시점의 boss facing과 damage/hit 시점의 boss facing을 비교한다.
- 이 기준에서는 hit 17건 중 16건이 facing changed 케이스였다.
- 코드 확인 결과 `BossPatternCaster.CastCellsWithBeforeDamage()`는 같은 `cells` 리스트로 warning visual을 만들고, warning 후 같은 `cells` 리스트로 DamageTile을 생성한다. 즉, 이 함수 경로 자체는 warning tile과 damage tile cell list가 일치한다.
- `ElevatorBossController.BasicAttackLoop()`의 normal scratch는 `caster.NormalScratchDirectional(bossCell, bossFacing)`를 warning 전에 계산해서 `CastCellsWithBeforeDamage()`에 넘긴다.
- `DashAndEnhancedScratch()`도 `enhancedCells`를 warning 전에 계산해서 같은 list를 damage에 사용한다.
- 따라서 이번 로그의 “rotated damage”는 실제 DamageTile이 warning과 다른 cell list로 회전했다는 확정 증거가 아니라, warning 관측부터 hit까지 사이에 boss facing이 바뀌었고 그 케이스에서 맞았다는 런타임 상관 지표다.

Warning tile과 damage tile 일치 여부:

- 확인한 `CastCellsWithBeforeDamage()` 경로에서는 warning과 damage가 같은 `cells`를 사용한다.
- 다만 모든 특수 패턴을 이벤트 ID별로 완전 검증한 것은 아니다. 패턴별 타입 로깅이 아직 없으므로 `pattern_hit_count_by_type`은 `unknown`으로 남는다.

## 7. 원인 판단

PPO가 못 하는 원인:

- Heuristic이 평균 35.1 damage까지 가므로, PPO가 평균 3~12 damage에 머무르는 것은 단순 환경 불가능보다는 policy/formulation 문제가 크다.
- 하지만 Heuristic도 10/10 player_dead이고 max 41/60에 그쳤으므로, 단순 공격 우선 정책만으로 클리어 가능하다는 증명도 아직 없다.
- 경고 대응은 대부분 가능하지만, hit가 발생하는 케이스는 boss facing 변화와 강하게 같이 나타난다.
- DecisionPeriod 자체가 완전히 막는 문제는 현재 로그 기준 약하다. `no_decision_before_damage=0`.

BC / AttackPriorityMask 판단:

- Heuristic이 50+ 또는 boss_dead에 도달하지 못했으므로, 바로 BC나 AttackPriorityMask를 확정하기에는 이르다.
- 다만 Heuristic이 PPO Eval보다 훨씬 높은 damage를 내므로, PPO에는 공격 우선/접근 우선 inductive bias가 필요하다.
- 다음 후보는 AttackPriorityMask 적용 전, Heuristic rule을 한 단계 개선해 후반 hit 원인을 줄일 수 있는지 확인하는 것이다.

다음 단계 제안:

1. 패턴 타입/이벤트 ID logging을 추가해 `pattern_hit_count_by_type`을 unknown에서 실제 패턴별 count로 분리한다.
2. warning cell list와 damage cell list hash를 pattern event 단위로 기록해 모든 패턴에서 일치 여부를 확정한다.
3. Heuristic에서 current danger 탈출 시 WAIT 비율이 높은 이유를 분리한다. safe move가 없어서 WAIT인지, least-danger move가 busy/wall로 막힌 것인지 확인한다.
4. Heuristic 개선 후 50+ damage 또는 boss_dead가 나오면, 그때 BC warm-start 또는 AttackPriorityMask를 검토한다.
5. 추가 PPO 장시간 학습은 아직 하지 않는다.

## 8. 종료 상태와 리스크

- Eval EXE: 종료됨.
- Port 5004/5005: 활성 connection 없음.
- Git commit/push: 수행하지 않음.

남은 리스크:

- boss-facing diagnostic은 decision-level 관측 기반이라 패턴 이벤트 단위의 완전한 causality가 아니다.
- pattern type logging은 아직 실제 pattern ID를 기록하지 않아 `unknown`이다.
- Heuristic은 사람이 하는 최적 플레이가 아니라 단순 규칙 oracle이므로, Heuristic 실패가 곧 환경 불가능을 의미하지는 않는다.
