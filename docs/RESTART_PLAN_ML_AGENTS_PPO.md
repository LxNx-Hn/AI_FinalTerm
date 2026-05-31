# CODE-BLUE ML-Agents PPO 재시작 계획

## 1. 목표

CODE-BLUE 보스전 환경을 Unity ML-Agents PPO 기반 강화학습 환경으로 재구성한다.

이번 재시작의 목적은 다음과 같다.

- Python `boss_env` 복제 방식을 사용하지 않는다.
- Unity 원본 보스전 로직을 직접 환경으로 사용한다.
- DQN은 메인 알고리즘으로 사용하지 않는다.
- PPO를 메인 강화학습 알고리즘으로 사용한다.
- 사람 플레이 기록은 사용하지 않는다.
- Imitation Learning은 사용하지 않는다.
- Random Agent / Rule-based Agent / PPO Agent를 비교한다.

## 2. 재시작 전제

이전 Gemini/Antigravity 작업 산출물 중 코드 파일은 신뢰하지 않는다.

재사용 가능한 것은 다음뿐이다.

- 보스 패턴 분석
- 보상 설계 방향
- scene cleanup 방향
- observation/action 설계 방향
- evaluation protocol

## 3. 새 프로젝트 구조

추천 구조:

```text
AI_FinalTerm_MLAgents_PPO_NEW/
├─ README.md
├─ docs/
├─ ml-agents-config/
├─ scripts/
├─ results/
└─ unity_project/
   └─ CODE-BLUE-RL/
```

`unity_project/CODE-BLUE-RL`은 `.git`이 없어야 한다.

## 4. Unity 복사 원칙

복사 대상:

```text
Assets
Packages
ProjectSettings
UserSettings
```

복사 금지:

```text
.git
Library
Temp
Logs
obj
Builds
Screenshots
_Recovery
*.zip
```

복사 후 확인:

```powershell
dir -Force
```

`.git`이 있으면 즉시 중단한다.

## 5. Unity 환경 원칙

학습용 씬은 원본 보스전 씬을 복사해 만든다.

권장:

```text
Boss01_Elevator_RLTrain
```

원본 보스전 씬은 보존한다.

학습용 씬에서 가능한 정리:

* 컷씬 스킵
* BGM 비활성화
* 대화 UI 비활성화
* 사망 후 R 키 대기 대신 자동 reset 설계
* 보스전과 무관한 scene 제거

보존해야 할 것:

* Boss
* Player
* Grid
* DamageTile
* WarningTile
* BossPatternCaster
* ElevatorBossController
* BossHealth
* PlayerHealth
* PlayerController
* PlayerCombat
* GridMover
* 보스 패턴 VFX
* warning/damage VFX

## 6. Observation 설계

우선 후보:

* player cell x/y
* player facing x/y
* player HP
* boss HP
* boss visible
* boss cell x/y, visible일 때만 유효
* warning grid mask
* damage grid mask
* attack cooldown 또는 attack ready
* move cooldown 또는 move ready
* elapsed time / normalized time
* boss phase 사용 여부는 검토

원칙:

* `current_pattern`은 observation에 넣지 않는다.
* `boss_hurtbox_active`는 logging metric 우선이다.
* 사람이 화면에서 얻을 수 있는 정보 중심으로 설계한다.

## 7. Action 설계

Multi-discrete action 권장.

Branch 0: Movement

```text
0 NONE
1 UP
2 DOWN
3 LEFT
4 RIGHT
```

Branch 1: Attack

```text
0 NO_ATTACK
1 ATTACK
```

원칙:

* Agent가 transform.position을 직접 변경하지 않는다.
* action은 기존 PlayerController / PlayerCombat / GridMover 흐름으로 전달한다.
* 이동+공격 동시 입력은 branch 조합으로 처리한다.
* action masking은 초기에는 강제하지 않는다.

## 8. Reward 설계

목표는 빠른 클리어다.

초기 후보:

```text
boss damage: +0.1 ~ +0.25 per HP 또는 deltaHp/maxHp 기반
boss kill: +5.0
remaining time bonus: 0 ~ +3.0
player hit: -1.0
death: -5.0
step penalty: -0.001 ~ -0.005
warning tile: -0.005 ~ -0.02
damage tile: -0.05 ~ -0.1
missed attack: -0.01
```

직접 보상 금지:

* no-hit dash attack bonus
* damage during dash bonus
* phase skip bonus
* pattern skip bonus

이유:

특정 고난도 기술에 직접 보상을 주면 “스스로 발견했다”는 해석이 약해진다. 빠른 클리어와 boss HP 감소 보상으로 자연스럽게 유도한다.

## 9. Reset 설계

done 조건:

* boss dead
* player dead
* max time exceeded

초기 reset은 scene reload 방식을 우선한다.

manual reset은 다음 조건을 만족할 때만 고려한다.

* 20~50회 reset stability test 통과
* warning/damage tile 잔여 없음
* coroutine 중복 없음
* player/boss HP 초기화 확인
* player/boss position 초기화 확인

## 10. Baseline 설계

최소 baseline:

1. Random Agent
2. Rule-based Agent
3. PPO Agent

Rule-based Agent는 강화학습이 아니라 비교군이다.

Rule-based 후보:

* damage tile 위에 있으면 피하기
* warning tile 위에 있으면 피하기
* boss가 공격 범위에 있으면 attack
* 아니면 boss 쪽으로 접근
* HP가 낮으면 보수적으로 회피

## 11. 학습 실행 순서

긴 학습 금지. 아래 순서로 진행한다.

1. Unity 프로젝트 열림 확인
2. compile error 없음 확인
3. 학습용 scene play 수동 확인
4. ML-Agents 설치
5. Agent script 작성
6. 컴포넌트 수동 부착
7. Editor smoke test
8. 짧은 PPO smoke 학습, 10k~50k steps
9. Windows EXE smoke
10. longer training은 별도 승인 후

## 12. 평가 프로토콜

최종 평가는 100 episodes 기준.

지표:

* boss_kill_rate
* avg_clear_time_seconds
* best_clear_time_seconds
* worst_clear_time_seconds
* std_clear_time_seconds
* avg_player_hit_count
* death_rate
* avg_boss_damage
* max_boss_damage
* damage_avoidance_rate
* pattern_sequence_count
* unique_pattern_sequences
* 사람 기록 170초 대비 여부

주의:

`unique_pattern_sequences == 1`이면 랜덤 패턴 일반화 평가로 보기 어렵다.

## 13. 성공/실패 해석

PPO가 클리어하면:

* Unity 원본 보스전 환경에서 PPO Agent가 회피/공격 정책을 학습했다고 기록한다.
* 사람 기록 170초와 비교한다.

PPO가 클리어하지 못하면:

* avg_boss_damage
* survival time
* hit count
* reward curve
* Random/Rule-based 대비 개선

위 지표로 학습 경향을 보고한다.

실패를 숨기지 않는다.

## 14. Codex 첫 구현 전 확인

Codex는 구현 전 다음을 확인한다.

* 새 프로젝트 폴더에 `.git` 없음
* unity_project 내부에 `.git` 없음
* origin이 CODE-BLUE가 아님
* ProjectVersion.txt 확인
* manifest.json 확인
* Boss01_Elevator 씬 존재 확인
* 위험 Editor script 없음
* Gemini 작성 코드 없음
