# CODE-BLUE Boss RL: Pattern and MDP/POMDP Analysis

작성 시각: 2026-06-01 KST

## 1. 목적

이 문서는 CODE-BLUE 보스전을 규칙 기반 heuristic으로 해결하려는 문서가 아니다. 목적은 보스전을 강화학습 문제로 정식화하고, 현재 Unity ML-Agents PPO 구현에서 어떤 상태, 관측, 행동, 보상이 제공되는지와 어떤 정보가 빠져 있는지를 코드 근거로 정리하는 것이다.

이번 단계에서는 추가 학습, heuristic 개선, reward/action mask/observation 코드 변경, scene 변경, build 실행을 하지 않았다. 분석 대상은 `AI_FinalTerm_MLAgents_PPO_NEW` 작업 사본이며 원본 CODE-BLUE repo 및 원본 로컬 폴더는 건드리지 않았다.

## 2. 보스전을 강화학습 문제로 정의

환경은 7x7 arena 안에서 플레이어가 보스 패턴 telegraph를 보고 회피하면서, 공격 가능 구간에 보스 HP를 깎는 episodic POMDP로 볼 수 있다. 실제 게임 내부 상태는 보스 coroutine, phase, pattern timer, 보스 위치, 시각 효과, DamageTile 생존 시간 등을 포함하지만, agent에게는 화면에서 볼 수 있거나 화면 정보를 벡터화한 일부 정보만 주어져야 한다.

현재 구현의 핵심 제약은 다음과 같다.

- Action은 단일 discrete branch `[6]`이다: `WAIT`, `UP`, `DOWN`, `LEFT`, `RIGHT`, `ATTACK`. `BossPlayerAgent.cs:19-20`
- 이동 action은 바라보기와 1칸 이동을 동시에 유발한다. 별도 turn-in-place action은 없다.
- Boss hit 판정은 collider가 아니라 `ElevatorBossController.BossCell`이 player attack cells 안에 있는지로 결정된다. `PlayerCombat.cs:176-183`
- 현재 RL observation은 `current_pattern`, `next_pattern`, coroutine state를 직접 넣지 않는다. 대신 위치, HP, warning/damage mask, recent danger, directional features 등을 넣는다. `BossRLStateExtractor.cs:13-18`

## 3. 전체 패턴 목록

| Phase | 패턴 | 코드 위치 | 요약 |
| --- | --- | --- | --- |
| Phase 1 | X 대각선 패턴 | `Phase1FirstFixed()` | TR-BL 대각선 3폭, TL-BR 대각선 3폭을 순차 dash pattern으로 실행한다. |
| Phase 1 | 2칸 edge 순환 dash | `Additional1A()` | LeftEdge2 -> TopEdge2 -> RightEdge2 -> BottomEdge2 순서로 2칸 폭 edge band를 순환한다. |
| Phase 1 | 십자 dash | `Additional1B()` | 중앙 horizontal stripe와 vertical stripe를 순차 실행한다. |
| Phase 1+ | N / reverse N | `NStroke()` | left edge, diagonal, right edge 또는 역순 edge/diagonal/edge 구조다. |
| Phase 1+ | Z / reverse Z | `ZStroke()` | top edge, diagonal, bottom edge 또는 역순 구조다. |
| Phase 2+ | 시계방향 4방향 sweep | `Phase2SweepOnly()` | LeftBand4, BottomBand4, RightBand4, TopBand4 중 random start로 4개 band를 순환한다. |
| Phase 3 | MarkDash4 | `MarkDash4()`, `MarkDash()` | 4회 표식 dash. real/fake variant가 있고 MarkATK VFX만 telegraph로 사용한다. |
| Final | 찍고 돌진 4회 | `FinalPhaseRoutine()` | 4회 HollowCorner5x5 slam 후 player 위치 기준 dash를 실행한다. |
| Landing | 착지 slam | `LandingSlam()` | phase에 따라 3x3 또는 hollow 5x5 warning 후 boss 등장 및 DamageTile 생성. |

## 4. 패턴별 상세 분석

### 4.1 X 대각선 패턴

- Phase: Phase 1 첫 고정 패턴.
- Code method: `ElevatorBossController.Phase1FirstFixed()`.
- Telegraph source: `CastDashPattern()`이 `BossPatternCaster.CastCellsWithBeforeDamage()`를 호출하고, 이 함수가 warning visual을 생성한다.
- Damage source: 같은 `cells` list로 DamageTile을 생성하고 direct damage monitor를 돌린다. `BossPatternCaster.cs:168-225`
- Boss movement: `beforeDamage`에서 `DashBossAlongArenaLine()`을 시작해 boss root가 start에서 end로 이동한다. `ElevatorBossController.cs:1737-1758`
- Facing 사용: dash direction으로 boss facing/visual을 설정한다.
- Warning/damage 일치: `CastCellsWithBeforeDamage()` 경로에서는 warning과 damage가 같은 `cells` list를 쓴다.
- Visible cue: 대각선 3폭 warning/damage visual, dash scratch VFX, boss dash visual.
- Current observation coverage: warning mask, damage mask, previous warning mask, recent danger에 포함된다.
- Missing observation: pattern name, future second diagonal, internal dash timer는 없다.
- Expected avoidance policy: 현재/이전 warning mask를 보고 대각선 band 밖으로 이동한다.
- Expected attack opportunity: dash 후 boss cell이 attack cells 안에 있고 danger가 없을 때 공격한다.

### 4.2 N / Reverse N

- Phase: Phase 1 additional 또는 Phase 2+ additional.
- Code method: `NStroke()`.
- Telegraph source: edge dash와 diagonal dash가 각각 `CastCenteredDashPattern()` 또는 `CastDashPattern()`을 통해 warning을 만든다.
- Damage source: 각 stroke의 damage cells.
- Boss movement: 각 stroke마다 dash visual과 boss root 이동이 동반된다.
- Facing 사용: stroke별 dash direction에 따라 사용된다.
- Warning/damage 일치: 각 stroke 단위로 `CastCellsWithBeforeDamage()`는 같은 cells를 사용한다.
- Visible cue: left/right edge 2열 warning과 diagonal 3폭 warning.
- Current observation coverage: 개별 stroke의 warning/damage는 mask로 들어간다.
- Missing observation: "지금 N의 몇 번째 stroke인지", "reverse인지", 다음 stroke 방향은 직접 제공되지 않는다.
- Expected avoidance policy: 현재 stroke를 피하면서 다음 edge/diagonal로 몰리지 않도록 위치를 잡아야 한다.
- Expected attack opportunity: stroke 사이 짧은 gap과 dash 후 boss position을 이용한다.

### 4.3 Z / Reverse Z

- Phase: Phase 1 additional 또는 Phase 2+ additional.
- Code method: `ZStroke()`.
- Telegraph source: top/bottom edge와 diagonal stroke warning.
- Damage source: 각 stroke의 damage cells.
- Boss movement: dash pattern마다 boss root가 line을 따라 이동한다.
- Facing 사용: stroke별 dash direction.
- Warning/damage 일치: `CastCellsWithBeforeDamage()` 경로에서는 일치한다.
- Visible cue: top/bottom edge 2행 warning과 diagonal 3폭 warning.
- Current observation coverage: 현재 warning/damage/recent danger.
- Missing observation: N과 동일하게 stroke index와 reverse state.
- Expected avoidance policy: 단발 회피가 아니라 sequence를 고려한 위치 선정.
- Expected attack opportunity: stroke 사이 안전 구간에서 boss cell 접근 및 attack.

### 4.4 2칸 edge 순환 dash

- Phase: Phase 1 additional.
- Code method: `Additional1A()`.
- Telegraph source: `LeftEdge2()`, `TopEdge2()`, `RightEdge2()`, `BottomEdge2()`가 순서대로 warning을 만든다. `ElevatorBossController.cs:1347-1361`
- Damage source: 같은 edge cells로 DamageTile.
- Boss movement: 각 edge band 중심선 방향으로 dash visual.
- Facing 사용: up/right/down/left 순서.
- Warning/damage 일치: stroke 단위로 일치.
- Visible cue: 가장자리 2칸 폭 warning.
- Current observation coverage: 현재 edge warning과 recent danger.
- Missing observation: 순환 sequence index가 직접 없다.
- Expected avoidance policy: 현재 edge만 피하는 것이 아니라 다음 edge에 갇히지 않는 중앙/반대쪽 위치를 선택한다.
- Expected attack opportunity: edge dash 후 boss end cell이 공격 범위에 들어오는 순간.

### 4.5 십자 dash

- Phase: Phase 1 additional 및 Phase 2+ additional 후보.
- Code method: `Additional1B()`.
- Telegraph source: `HorizontalStripe3(0)` 후 `VerticalStripe3(0)`. `ElevatorBossController.cs:1363-1369`
- Damage source: horizontal/vertical stripe DamageTile.
- Boss movement: centered dash.
- Facing 사용: right, up.
- Warning/damage 일치: stroke 단위로 일치.
- Visible cue: 중앙 가로 3행, 중앙 세로 3열.
- Current observation coverage: warning/damage mask.
- Missing observation: 두 번째 stripe가 올 것이라는 sequence memory.
- Expected avoidance policy: 첫 stripe 회피 후 두 번째 stripe 안전 위치까지 고려한다.
- Expected attack opportunity: stripe 후 boss가 edge/end cell에 있을 때.

### 4.6 Phase 2 시계방향 4방향 sweep

- Phase: Phase 2 첫 고정 및 Phase 2/3 additional 후보.
- Code method: `Phase2SweepOnly()`.
- Telegraph source: `LeftBand4`, `BottomBand4`, `RightBand4`, `TopBand4` 중 random start에서 4개를 순환한다. `ElevatorBossController.cs:1229-1248`
- Damage source: 각 band의 DamageTile.
- Boss movement: centered dash pattern.
- Facing 사용: switch case별 dash direction.
- Warning/damage 일치: stroke 단위로는 일치한다.
- Visible cue: arena 절반 이상을 덮는 4칸 폭 band warning.
- Current observation coverage: 현재 band warning/damage와 recent danger.
- Missing observation: random start index와 i=1~4 sequence index가 직접 없다.
- Expected avoidance policy: one-step greedy 회피가 아니라 다음 band가 생길 위치를 피하는 multi-step positioning.
- Expected attack opportunity: 넓은 band sweep 중 안전한 side에서 boss cell 접근.
- 실패 원인 가설: 기존 관찰에서 3~4번째 line hit가 잦다면, 현재 mask는 현재/최근 danger에는 강하지만 "다음 sweep line" 예측을 직접 제공하지 않으므로 sequence memory 또는 관측 설계가 병목일 수 있다.

### 4.7 Phase 3 MarkDash4

- Phase: Phase 3 첫 고정 및 Phase 3 additional 후보.
- Code method: `MarkDash4()`, `MarkDash()`.
- Telegraph source: `SpawnMarkDashVfx()`가 player cell 중심에 MarkATK VFX를 spawn한다. `spawnLegacyWarningTiles=false`라 WarningTile은 만들지 않는다. `ElevatorBossController.cs:1474-1502`
- Damage source: real이면 display 방향과 같은 horizontal/vertical stripe, fake이면 display 방향의 반대 orientation stripe가 DamageTile이 된다. `ElevatorBossController.cs:1463-1516`
- Boss movement: damage 시점에 `CastDashDamageOnly()`가 warning 없이 DamageTile과 dash visual을 실행한다.
- Facing 사용: actual orientation에 따라 random cardinal dash direction.
- Warning/damage 일치: MarkDash는 일반 warning tile 경로가 아니다. display marker cells와 damage cells는 real/fake에 따라 같거나 다르게 해석된다.
- Visible cue: red real marker와 blue fake marker VFX. Prefab은 `MarkATKVFX`와 `MarkATKVFX_Fake`가 분리되어 있고 scene도 normal/fake prefab을 각각 연결한다. `Boss01_Elevator_RLTrain.unity:6462-6468`
- Current observation coverage: 현재 `BossRLStateExtractor.RefreshHazardMasks()`는 `PatternTile`, `MergedPatternWarningVisual`, `DamageTile`만 읽는다. MarkATK VFX prefab은 이 셋에 포함되지 않는다. `BossRLStateExtractor.cs:475-498`
- Missing observation: MarkATK real/fake marker 자체가 warning mask에 들어가지 않는다. 따라서 red/blue가 화면에 보이더라도 현재 vector observation만으로는 구분할 수 없다.
- Expected avoidance policy: real red marker는 표시 방향을 피하고, fake blue marker는 반대 orientation damage를 예측해야 한다.
- Expected attack opportunity: mark flash 동안 boss가 숨겨지고 damage dash 뒤 다시 boss cell이 잡히는 타이밍.

### 4.8 Final 찍고 돌진 4회

- Phase: Final.
- Code method: `FinalPhaseRoutine()`, `FinalOriginalDashAfterSlam()`, optional `FinalRandomNoWarningDash()`.
- Telegraph source: 먼저 `HollowCorner5x5(slamTarget)` warning/damage가 있고, 기본 설정에서는 slamTarget에서 player 방향으로 dash warning이 이어진다. `ElevatorBossController.cs:1643-1687`, `2934-2950`
- Damage source: slam DamageTile, 이후 `ForwardStripe3FromCell()` dash DamageTile.
- Boss movement: slam 때 boss root가 target으로 이동하고, dash 때 line을 따라 이동한다.
- Facing 사용: player 방향 또는 random cardinal direction.
- Warning/damage 일치: 기본 dash 경로는 `CastDashPattern()`이라 warning/damage cells가 일치한다. `FinalRandomNoWarningDash()`가 켜지면 warning 없이 damage만 발생한다.
- Visible cue: slam warning, boss appear, dash warning 또는 no-warning dash option.
- Current observation coverage: 기본 경로의 warning/damage는 mask에 들어간다.
- Missing observation: final sequence index 1~4, 다음 dash의 target/future line은 직접 없다.
- Expected avoidance policy: slam area에서 벗어난 뒤 이어지는 dash direction까지 고려한다.
- Expected attack opportunity: final 반복 사이 boss가 visible/in-range가 되는 구간.

## 5. MDP 구성

실제 환경 상태 `S_t`는 agent에게 모두 공개되지 않는 내부 상태까지 포함한다.

- Player: world/arena cell, facing, HP, attack cooldown, 이동 중 여부.
- Boss: boss cell, visual/root transform, facing, HP, phase, damageable 여부, visible 여부.
- Pattern: current coroutine, pattern sequence index, warning timer, damage timer, active telegraph cells, active damage cells.
- MarkDash: real/fake variant, display orientation, actual damage orientation, MarkATK VFX lifetime.
- Memory: recent warning/damage cells, previous warning mask.
- Termination: boss_dead, player_dead, episode timeout.

Transition `P(S_{t+1}|S_t,A_t)`는 player action과 boss coroutine progression이 함께 결정한다. Agent action은 1 decision마다 wait/move/attack 중 하나만 고르며, boss pattern은 시간과 phase/HP에 따라 독립적으로 진행된다. 즉 같은 action이라도 현재 hidden timer나 다음 pattern sequence에 따라 결과가 달라질 수 있어 POMDP 성격이 강하다.

## 6. Agent observation 구성

현재 구현의 vector observation은 총 193개다. 코드 주석 기준 구성은 player position/facing, player/boss HP, boss visible/position, warning mask, damage mask, attack/move ready, elapsed, previous warning mask, boss-in-range, manhattan distance, recent danger, 4방향 directional features다. `BossRLStateExtractor.cs:13-18`

현재 관측에 들어가는 위험 정보는 다음 source에서 온다.

- `PatternTile`
- `MergedPatternWarningVisual`
- `DamageTile`

이는 `RefreshHazardMasks()`에서 직접 확인된다. `BossRLStateExtractor.cs:475-498`

관측에 없는 주요 정보는 다음과 같다.

- `current_pattern` enum 또는 coroutine name.
- `next_pattern` 또는 random choice result.
- 내부 pattern timer 자체.
- MarkATK VFX object의 real/fake 종류와 orientation.
- Hidden hurtbox active flag.
- Phase enum 자체. 다만 boss HP, phase3 visual cue, pattern cue를 통해 간접 추론 가능하다.

## 7. Action space

현재 action space는 다음 6개다.

| Action | 의미 |
| --- | --- |
| 0 | WAIT |
| 1 | UP |
| 2 | DOWN |
| 3 | LEFT |
| 4 | RIGHT |
| 5 | ATTACK |

주의할 점은 방향 action이 단순 facing 변경이 아니라 이동 action이라는 점이다. 따라서 공격 방향을 맞추기 위해 제자리 회전만 하는 전략은 현재 action space에서 표현되지 않는다. 이 구조는 "공격하고 싶은데 방향만 틀고 싶다"는 상황에서 이동과 공격 기회를 trade-off하게 만든다.

## 8. Reward 설계

현재 reward 항목은 다음과 같이 코드에 정의되어 있다. `BossRLReward.cs:11-28`

- `BossDamagePerHp = +0.10`
- `BossKillReward = +5.0`
- `PlayerHitPenalty = -2.0`
- `PlayerDeathPenalty = -8.0`
- `StepPenalty = -0.001`
- `WarningTilePenalty = -0.10`
- `DamageTilePenalty = -0.20`
- `WallBlockedMovePenalty = -0.05`
- `MovedIntoWarningPenalty = -0.08`
- `MovedIntoRecentWarningPenalty = -0.10`
- `MovedIntoDamagePenalty = -0.30`
- `SafeInRangeAttackAttemptReward = +0.08`
- `MissedSafeAttackOpportunityPenalty = -0.003`

중요한 과거 버그는 terminal reward 누락이었다. 현재 `TerminalRewardForReason()`은 `player_dead=-8`, `boss_dead=+5`, 그 외 `0`을 반환한다. `BossRLReward.cs:30-35`

Reward 해석상 유의점은 다음과 같다.

- 실제 boss HP 감소가 boss damage reward의 핵심이다.
- Safe in-range attack attempt reward는 "안전하고 in-range인 상태에서 공격 선택"을 보조하지만 kill을 보장하지 않는다.
- Warning/damage/recent danger penalty는 생존을 강화하지만, 넓은 sequence pattern에서 장기 위치 선정까지 직접 가르치지는 않는다.

## 9. Episode termination

Episode는 boss dead, player dead, timeout으로 끝난다. 현재 `BossPlayerAgent`의 `maxEpisodeSeconds`는 210초다. `BossPlayerAgent.cs:23`

`BossRLReward.Evaluate()`는 `bossDead`, `playerDead`, `timedOut`를 계산하고, `BossPlayerAgent.OnActionReceived()`가 reason을 `boss_dead`, `player_dead`, `timeout`으로 분기해 terminal handling을 수행한다. `BossRLReward.cs:180-185`, `BossPlayerAgent.cs:309-315`

Scene reload는 `BossRLEpisodeResetter.QueueSceneReload()` 경로를 사용한다.

## 10. 정답 추출 기준

학습에 넣어도 되는 정보는 사람이 화면에서 볼 수 있는 telegraph/marker/damage/boss/player UI 정보를 벡터화한 것이다.

허용 가능한 observation 후보:

- Visible red real MarkATK marker position/orientation.
- Visible blue fake MarkATK marker position/orientation.
- Visible warning/damage cells.
- Visible boss relative position/facing.
- Player HP, boss HP, attack ready/cooldown UI에 대응되는 정보.
- Recent visible danger memory.

금지 또는 주의할 observation:

- `current_pattern` enum 직접 입력.
- `next_pattern` 직접 입력.
- coroutine name 직접 입력.
- hidden hurtbox active flag.
- 화면에서 알 수 없는 exact damage timer.

따라서 red/blue MarkATK marker를 observation에 넣는 것은 cheat가 아니라 visual cue encoding이다. 반대로 `MarkDashVariant.FakeHorizontal` 같은 내부 enum을 직접 넣는 것은 권장되지 않는다.

## 11. 현재 구현의 observation gap

가장 큰 gap은 MarkDash다. `MarkDash()`는 `spawnLegacyWarningTiles=false`로 WarningTile을 만들지 않고, `SpawnMarkDashVfx()`만 호출한다. `BossRLStateExtractor`는 MarkATK VFX를 수집하지 않는다. 따라서 현재 agent는 MarkDash의 red/blue visual cue를 vector observation에서 직접 볼 수 없다.

두 번째 gap은 sequence memory다. N/Z, edge cycle, phase2 sweep, final 4회 반복은 현재 warning/damage mask만으로 단발 회피는 가능하지만, 다음 stroke를 예측한 위치 선정은 어렵다. 현재 `prevWarningMask`와 recent danger가 일부 memory 역할을 하지만, sequence index나 visual marker type은 없다.

세 번째 gap은 boss hit 기준과 visual 기준의 차이 가능성이다. `PlayerCombat`은 boss collider/renderer가 아니라 `BossCell` 기준으로 공격 성공을 판정한다. `BossCell`은 `GridManager.WorldToCell(transform.position)` 기준이다. 따라서 boss visual이 숨겨져 있거나 root/renderer가 어긋나는 순간에는 "화면상 빈 공간 공격이 맞는 것처럼" 보일 여지가 있다. 다만 이번 단계에서는 logging/build를 하지 않았으므로 실제 빈 공간 hit 빈도는 확정하지 않았다.

## 12. 실험 중 발견된 주요 버그 및 수정 이력 요약

기존 문서와 로그 요약 기준 주요 이력은 다음과 같다.

- ML-Agents/EXE 안정화 전에는 Editor training 불안정이 있었고, 이후 EXE build smoke 중심으로 안정화했다.
- Wrapper 계층에서 movement mask가 busy를 wall처럼 오판하는 문제, `TakeActionsBetweenDecisions=true`로 인한 attack replay, scene reload timeout, terminal reward 누락이 있었다.
- 이후 SafeActionMask, `TakeActionsBetweenDecisions=false`, `LoadSceneAsync`, terminal reward fix가 적용되었다.
- 현재 좋은 신호: hit_rate는 거의 100%, attack_out_of_range는 0으로 SafeActionMask는 의도대로 작동한다.
- 남은 문제: safe opportunity에서 MOVE 과다 선택, MarkATK VFX observation gap, hidden/stale hurtbox 가능성, 4방향 sweep/final sequence의 multi-step 회피 난도.

## 13. PPO 학습 결과 요약

주요 결과만 요약하면 다음과 같다.

- 100K 결과: hit_rate 99.965%, safe_attack_taken_ratio 54.445%, boss_damage/ep 22.920, max_boss_damage 36/60, boss_dead 0.
- 90초 timeout은 사람 플레이 기준 약 170초 clear time과 맞지 않아 timeout 병목 가능성이 제기되었다.
- 50K reward adjustment 이후 training은 avg boss_damage 12.015, max 25/60, hit_rate 100%, attack_out_of_range 0이었으나 Eval210은 avg boss_damage 3.8, max 6/60에 머물렀다.
- Heuristic Oracle Eval210은 avg boss_damage 35.1, max 41/60, hit_rate 100%, attack_out_of_range 0이었지만 10/10 player_dead였다.

해석은 다음과 같다.

- PPO가 전혀 공격을 못 하는 상태는 아니다. SafeActionMask 이후 공격 품질은 좋아졌다.
- 그러나 boss kill까지 필요한 장기 생존, sequence 회피, MarkDash 해석, 공격 기회 우선순위가 아직 충분하지 않다.
- Heuristic이 41/60까지 가도 죽는다는 점은 단순 공격 우선순위만으로는 해결되지 않고, 후반 패턴 관측/회피 문제가 남아 있음을 시사한다.

## 14. 실패/한계 원인

### PPO formulation 문제

현재 policy는 safe opportunity에서도 MOVE를 과다 선택한 기록이 있다. 이는 보상만의 문제가 아니라 action/value formulation이 "지금 공격 가능한 안전한 순간"을 충분히 우선시하지 못한다는 신호다.

### Observation gap

MarkATK real/fake visual cue는 현재 vector observation에 없다. MarkDash가 Phase 3에서 핵심이라면 agent는 화면상 marker를 실제로 볼 수 있어야 한다.

### Multi-step 회피 문제

Phase2 sweep, N/Z, final 4회 반복은 현재 warning만 피하면 다음 warning에 갇힐 수 있다. 현재 observation은 단발 hazard mask 중심이라 sequence planning에 약하다.

### Hitbox/visual alignment 미확정

코드상 boss hit는 `BossCell` 기반이고 visual/collider 기반이 아니다. 사용자 관찰처럼 빈 공간 hit처럼 보이는 현상은 이 구조와 맞닿아 있을 수 있으나, 실제 빈도는 event logging이 있어야 확정 가능하다.

## 15. 다음 개선 방향

우선순위는 다음과 같다.

1. MarkATK VFX observation 설계: red/blue marker, orientation, 중심 cell을 visual cue로 encoding한다. 내부 enum이 아니라 실제 prefab/visual object 기반이어야 한다.
2. Pattern event diagnostic: 코드 변경이 허용되는 다음 단계에서 pattern event id, warning/damage cell hash, MarkDash display/damage orientation, boss visual/root cell alignment를 logging한다.
3. Sequence memory 보강: LSTM 또는 visual history stack, 혹은 previous/current marker mask 확장으로 N/Z/sweep/final sequence의 부분관측성을 줄인다.
4. Action space 검토: turn-in-place 부재가 공격 방향 정렬을 방해하는지 분리한다.
5. PPO 재학습 전 검증: observation gap과 hitbox alignment를 먼저 확인한 뒤 reward를 다시 조정한다.

## 16. 이번 단계에서 읽은 파일

- `unity_project/Assets/Project/Scripts/Boss/ElevatorBossController.cs`
- `unity_project/Assets/Project/Scripts/Boss/BossPatternCaster.cs`
- `unity_project/Assets/Project/Scripts/Boss/BossHealth.cs`
- `unity_project/Assets/Project/Scripts/Boss/DamageTile.cs`
- `unity_project/Assets/Project/Scripts/Player/PlayerCombat.cs`
- `unity_project/Assets/Project/Scripts/Player/PlayerController.cs`
- `unity_project/Assets/Project/Scripts/Grid/GridMover.cs`
- `unity_project/Assets/Project/Scripts/Grid/GridManager.cs`
- `unity_project/Assets/Project/Scripts/Grid/GridOccupant.cs`
- `unity_project/Assets/Project/Scripts/RL/BossPlayerAgent.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLStateExtractor.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLReward.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLEpisodeResetter.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
- `unity_project/Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity`
- `unity_project/Assets/Project/Prefabs/Boss/MarkATKVFX.prefab`
- `unity_project/Assets/Project/Prefabs/Boss/MarkATKVFX_Fake.prefab`
- `unity_project/Assets/Project/Prefabs/Boss/MarkATKVFX_Ctrl.controller`
- `unity_project/Assets/Project/Resources/Boss/BOSSASSET/BOSS/ANIME/MARKATK.anim`
- `unity_project/Assets/Project/Resources/Boss/BOSSASSET/BOSS/ANIME/MARKFAKE.anim`
- `docs/rl_eval_runs/20260601_ACTION_HISTOGRAM_DIAG.md`
- `docs/rl_eval_runs/20260601_SAFE_OPP_MOVE_TRACE_AND_REWARD_50K.md`
- `docs/rl_eval_runs/20260601_HEURISTIC_ORACLE_EVAL210.md`

## 17. 금지 사항 준수 여부

- 추가 PPO 학습: 하지 않음.
- Heuristic 개선: 하지 않음.
- Reward 변경: 하지 않음.
- Action mask 변경: 하지 않음.
- Observation 코드 변경: 하지 않음.
- Scene 변경: 하지 않음.
- Build 실행: 하지 않음.
- 원본 CODE-BLUE 수정: 하지 않음.
