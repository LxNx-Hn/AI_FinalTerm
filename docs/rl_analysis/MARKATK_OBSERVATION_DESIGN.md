# MarkATK Visible Cue Observation Design

작성 시각: 2026-06-01 KST

## 1. 배경

이 문서는 `docs/rl_analysis/BOSS_PATTERN_MDP_ANALYSIS.md`를 기준으로 MarkDash / MarkATK 관측 누락, Phase 2 시계방향 4줄 sweep의 부분관측성, 그리고 보이는 보스 위치와 `BossCell` 타격 기준 불일치 가능성을 설계 관점에서 정리한다.

이번 단계는 구현 단계가 아니다. 코드 수정, scene 수정, build, PPO 학습, heuristic 개선은 하지 않는다. 여기서는 다음 구현 단계에서 어떤 observation channel과 diagnostic metric이 필요한지 정의한다.

## 2. BOSS_PATTERN_MDP_ANALYSIS.md 기준 요약

기준 문서의 결론은 다음과 같다.

- MarkDash / MarkDash4는 일반 `WarningTile` 기반 패턴이 아니다.
- `MarkDash()` 안에서 `spawnLegacyWarningTiles=false`이므로 MarkDash marker cells는 `WarningTile`로 생성되지 않는다.
- visible telegraph는 `MarkATKVFX` / `MarkATKVFX_Fake` prefab으로 표시된다.
- 현재 `BossRLStateExtractor.RefreshHazardMasks()`는 `PatternTile`, `MergedPatternWarningVisual`, `DamageTile`만 관측 source로 읽는다.
- 따라서 현재 vector observation은 red/blue MarkATK visible cue를 직접 포함하지 않는다.
- `current_pattern`, `next_pattern`, coroutine state, hidden hurtbox flag를 observation에 넣는 것은 금지 방향이다.
- 사람이 화면에서 볼 수 있는 warning/damage/marker/boss/player UI 정보는 visible cue encoding으로 넣을 수 있다.

## 3. MarkDash / MarkATK 코드 경로

`ElevatorBossController.MarkDash()`의 구조는 다음과 같다.

- `playerCell = GetPlayerOffsetCell()`로 marker 중심 cell을 잡는다.
- `PickMarkDashVariant()`가 normal/fake, vertical/horizontal variant를 고른다.
- `displayHorizontal`은 화면에 표시되는 marker 방향이다.
- `isFake`이면 `actualHorizontal = !displayHorizontal`이 된다.
- `markCells`는 display 방향 기준 `MarkHorizontal5()` 또는 `MarkVertical5()`다.
- `spawnLegacyWarningTiles=false`라 `warningTilePrefab`은 생성되지 않는다.
- `SpawnMarkDashVfx(playerCell, displayHorizontal, isFake)`만 visible marker를 만든다.
- `SetBossVisible(false)`로 보스 sprite를 숨긴다.
- `markFlashTime` 이후 `damageCells`는 actual 방향의 `HorizontalStripe3()` 또는 `VerticalStripe3()`로 만들어진다.
- 이후 `CastDashDamageOnly()`가 warning 없이 DamageTile과 dash visual을 실행한다.

즉 MarkDash에서 visible marker와 실제 damage stripe는 일반 warning mask pipeline과 분리되어 있다.

## 4. 현재 observation source

현재 `BossRLStateExtractor`의 vector observation size는 193이다.

구성 요약:

- player position: 2
- player facing: 4
- player/boss HP: 2
- boss visible + boss position: 3
- current warning mask: 49
- current damage mask: 49
- attack/move ready + elapsed: 3
- previous warning mask: 49
- boss in range + manhattan distance: 2
- current player cell recent danger flags: 2
- directional features 4 directions x 7: 28

현재 hazard source:

- `PatternTile`
- `MergedPatternWarningVisual`
- `DamageTile`

현재 recent danger memory:

- `RecentWarningTTL = 0.65s`
- `RecentDamageTTL = 0.30s`
- 단, full recent mask가 observation에 직접 들어가는 것은 아니고, 현재 player cell 및 4방향 next cell feature로 제한적으로 들어간다.

## 5. MarkATK observation gap

MarkATK gap의 핵심은 다음과 같다.

- red real marker와 blue fake marker는 화면에는 보인다.
- 그러나 현재 vector observation source에는 MarkATK VFX object가 없다.
- fake marker를 단순 danger mask에 넣으면 안 된다. fake는 "그 자리가 위험하다"가 아니라 "반대 orientation이 실제 damage가 된다"는 visual rule을 제공한다.
- real marker도 `markCells` 5칸 자체와 실제 `damageCells` 3열/3행 stripe가 항상 동일한 cell 집합은 아니므로, 단순 warning mask와 동일시하면 안 된다.

따라서 MarkATK는 `warningMask`에 섞기보다 별도 visible cue channel로 제공하는 것이 안전하다.

## 6. Real / Fake marker 구분 방식

현재 prefab/scene 기준 구분은 다음과 같다.

- Scene의 `normalMarkVfxPrefab`은 `MarkATKVFX.prefab`을 가리킨다.
- Scene의 `fakeMarkVfxPrefab`은 `MarkATKVFX_Fake.prefab`을 가리킨다.
- `MarkATKVFX.prefab`은 `MarkATKVFX_Ctrl.controller`를 사용한다.
- `MarkATKVFX_Fake.prefab`은 `fx_Blue_eye_Fake_warning_vertical_640_0.controller`를 사용한다.
- `MARKATK.anim`과 `MARKFAKE.anim`은 서로 다른 sprite asset guid를 사용한다.

구현 단계에서는 prefab identity, component marker, 또는 spawn 시점의 `isFake` flag를 diagnostic용으로 기록할 수 있다. 다만 policy observation에는 내부 enum이 아니라 실제 spawned visible cue의 cell/orientation/type을 encoding해야 한다.

## 7. Visible cue로 넣어야 하는 이유

red/blue marker는 사람이 화면에서 볼 수 있는 정보다. 따라서 이 정보를 vector observation으로 넣는 것은 정답 유출이 아니다. 반대로 `MarkDashVariant.FakeHorizontal` 같은 내부 variant 이름을 직접 넣으면, 사람이 볼 수 없는 coroutine-level answer를 제공하는 것이 되어 금지해야 한다.

허용되는 encoding:

- red marker visible cells
- blue marker visible cells
- marker orientation
- marker center cell
- marker visible lifetime 중 recent marker memory

금지되는 encoding:

- `current_pattern = MarkDash`
- `isFake` 내부 bool 자체를 raw enum처럼 직접 입력
- `actualHorizontal` 직접 입력
- 다음 damage stripe를 미리 계산해 "정답 mask"로 제공

## 8. 정답 유출이 아닌 이유

visible cue encoding은 화면 정보를 벡터화한 것이다. 사람이 red marker와 blue marker를 보고 회피 rule을 학습할 수 있다면, agent도 해당 cue를 관측해야 공정하다.

중요한 경계는 "보이는 원인"과 "숨겨진 정답"을 나누는 것이다.

- `markatk_real_visible_mask`: 허용. 화면에 red marker가 보인다.
- `markatk_fake_visible_mask`: 허용. 화면에 blue marker가 보인다.
- `markatk_actual_damage_stripe_mask`: 주의. damage 전에는 사람이 직접 볼 수 없는 정답이면 금지.
- `current_pattern`, `sweep_sequence_index`, `next_band`: 금지.

## 9. 제안 observation channel

MarkATK 최소 제안:

- `markatk_real_visible_mask`: 49
- `markatk_fake_visible_mask`: 49

MarkATK 권장 제안:

- `markatk_real_visible_mask`: 49
- `markatk_fake_visible_mask`: 49
- `recent_markatk_real_mask`: 49

선택 후보:

- `recent_markatk_fake_mask`: fake cue memory가 필요한 경우만 검토.
- `markatk_orientation`: mask로 충분하지 않을 경우 center/orientation scalar 또는 directional one-hot을 별도 검토.

주의:

- fake marker는 movement danger mask에 hard danger로 넣지 않는다.
- real marker도 실제 damage stripe와 cell 집합이 다르므로 먼저 feature로만 넣고, hard mask 편입은 별도 검증 뒤 결정한다.

## 10. Phase 2 Clockwise Sweep Observation Gap

### 패턴 구조

Phase 2 시계방향 4줄 sweep은 `ElevatorBossController.Phase2SweepOnly()`에서 실행된다.

코드 구조:

- `startIndex = Random.Range(0, 4)`로 시작 band를 고른다.
- `for (int i = 0; i < 4; i++)`로 4개 stroke를 순환한다.
- `idx = (startIndex + i) % 4`로 현재 band를 결정한다.
- `idx=0`: `LeftBand4()`, dash direction `down`
- `idx=1`: `BottomBand4()`, dash direction `right`
- `idx=2`: `RightBand4()`, dash direction `up`
- `idx=3`: `TopBand4()`, dash direction `right`
- 첫 stroke는 `sweepFirstDashExtraWarning` 때문에 warning이 더 길고, 이후 stroke는 기본 dash warning time을 쓴다.
- 각 stroke는 `CastCenteredDashPattern()` -> `CastDashPattern()` -> `CastCellsWithBeforeDamage()` 경로를 탄다.

warning / damage 생성:

- 각 stroke의 warning은 `CastCellsWithBeforeDamage()`에서 `SpawnWarningVisuals()`로 생성된다.
- damage는 같은 `damageCells` list로 DamageTile을 생성한다.
- 즉 stroke 단위 warning/damage cell list는 일치한다.

boss movement / facing:

- `CastDashPattern()`의 `beforeDamage` callback에서 `DashBossAlongArenaLine()`을 시작한다.
- dash direction으로 boss facing과 visual 이동 방향이 정해진다.
- boss root는 start cell에서 end cell로 lerp 이동하고, 종료 시 `bossCell=endCell`로 갱신된다.

### 현재 RL observation coverage

현재 observation은 다음을 포함한다.

- current warning mask: 있음.
- current damage mask: 있음.
- previous warning mask: 1개 있음.
- current player cell recent warning/damage flag: 있음.
- 4방향 next cell의 warning/damage/recent warning/recent damage flag: 있음.

하지만 다음은 직접 포함하지 않는다.

- random start index.
- 현재 sweep sequence index `i`.
- 다음 band identity.
- full previous damage mask.
- full 2-step warning history.
- full recent sweep band history mask.

현재 recent memory duration:

- warning TTL: 0.65초.
- damage TTL: 0.30초.
- 이 TTL은 현재/인접 cell의 recent danger feature에 반영되지만, arena 전체의 recent sweep history mask는 아니다.

### 왜 1-step 회피로 3~4번째 line에서 맞는가

사용자 관찰상 앞의 2줄은 피하지만 3~4번째 줄에서 거의 확정으로 맞는 장면이 있다. 이 현상은 다음과 같이 해석할 수 있다.

- 각 stroke의 현재 warning/damage는 보이므로 단발 회피는 가능하다.
- 하지만 random start에서 4개 band를 순환하므로, 현재 band만 보고 가장 가까운 safe cell로 피하면 다음 band의 위험 영역으로 들어갈 수 있다.
- 특히 band가 arena 절반 이상을 덮기 때문에, "지금 안전한 곳"과 "다음 stroke까지 안전한 곳"이 다를 수 있다.
- 현재 observation에는 previous warning 1개는 있지만, sequence index와 다음 band가 없고 full history도 제한적이다.
- 따라서 policy가 3~4번째 line까지 안정적으로 피하려면 visible history에서 sweep 순서를 추론해야 한다.

### current_pattern을 넣으면 안 되는 이유

`current_pattern=Phase2SweepOnly`, `sweep_sequence_index=2`, `next_band=RightBand4` 같은 정보는 사람이 화면에서 직접 보는 정보가 아니라 내부 coroutine state다. 이를 observation에 넣으면 POMDP를 내부 정답이 노출된 MDP로 바꾸는 효과가 있다.

금지:

- `current_pattern` enum.
- `sweep_sequence_index`.
- `next_band`.
- `startIndex`.
- 다음 stroke damage mask를 선계산해서 제공.

### visible history channel이 정답 유출이 아닌 이유

과거 warning/damage mask는 이미 화면에 보였던 정보다. 사람이 직전 1~2개 line을 기억해 "다음은 어디일 가능성이 높은가"를 추론할 수 있으므로, agent에게 visible history stack을 제공하는 것은 정답 유출이 아니다.

허용 후보:

- `previous_warning_mask_1`
- `previous_warning_mask_2`
- `previous_damage_mask_1`
- `recent_sweep_band_history_mask`
- `visible_telegraph_history_stack`

핵심은 다음 line을 직접 알려주는 것이 아니라, policy가 보였던 telegraph history에서 sweep rule을 학습하게 두는 것이다.

### MarkATK gap과의 차이

MarkATK gap은 visible cue 자체가 observation source에 누락된 문제다. 즉 red/blue marker가 화면에는 보이지만 vector에는 없다.

Phase 2 sweep gap은 current warning/damage는 이미 observation에 있지만, sequence memory가 부족한 partial observability 문제다. 즉 정보가 완전히 없는 것이 아니라, 시간 축 history가 충분하지 않을 수 있다.

### 향후 구현 시 필요한 observation channel

권장 sweep history channel:

- `previous_warning_mask_1`: 현재 구현의 `prevWarningMask`를 명시적으로 유지.
- `previous_warning_mask_2`: 한 단계 더 오래된 warning.
- `previous_damage_mask_1`: 직전 damage 위치.
- `recent_sweep_band_history_mask`: 최근 sweep-type band가 지나간 전체 cell 집합.

선택 설계:

- 일반화된 `visible_telegraph_history_stack`: pattern 종류와 무관하게 최근 K개의 visible warning/damage mask를 쌓는다.
- 이 방식은 MarkDash와 sweep을 모두 처리하기 쉽지만 observation size가 더 커진다.

## 11. VectorObservationSize 변경 계산

현재 기준:

- Arena grid: 7x7.
- `N = 49`.
- 현재 `VectorObservationSize = 193`.

현재 이미 포함된 mask:

- current warning mask: `+N`
- current damage mask: `+N`
- previous warning mask: `+N`

MarkATK 권장 추가:

- `markatk_real_visible_mask`: `+N`
- `markatk_fake_visible_mask`: `+N`
- `recent_markatk_real_mask`: `+N`
- MarkATK subtotal: `+3N = +147`

Phase2 sweep 권장 추가:

- `previous_warning_mask_2`: `+N`
- `previous_damage_mask_1`: `+N`
- `recent_sweep_band_history_mask`: `+N`
- Sweep subtotal: `+3N = +147`

권장 합산:

- 기존 193 + MarkATK 147 + Sweep 147 = `487`.

사용자 제안 channel을 모두 별도 channel로 추가하는 보수안:

- `previous_warning_mask_1`: `+N`
- `previous_warning_mask_2`: `+N`
- `previous_damage_mask_1`: `+N`
- `recent_sweep_band_history_mask`: `+N`
- Sweep subtotal: `+4N = +196`
- 단, 현재 `prevWarningMask`가 이미 `previous_warning_mask_1` 역할을 하므로 중복 가능성이 있다.
- 기존 193 + MarkATK 147 + Sweep 196 = `536`.

선택 확장:

- `recent_markatk_fake_mask`: `+N`
- `markatk_visible_orientation` scalar/one-hot: 별도 계산 필요.
- full `visible_telegraph_history_stack` K개를 쓰면 `+K*N`이다.

권장 결론:

- 중복을 피하는 기본안은 `VectorObservationSize = 487`.
- 문서/실험 비교용 보수안은 `VectorObservationSize = 536`.
- observation size가 바뀌면 기존 ONNX/checkpoint와 호환되지 않으며 fresh training 또는 observation-compatible 재초기화가 필요하다.
- BehaviorParameters의 vector observation size도 동일하게 업데이트되어야 한다.

## 12. Movement / action mask 적용 원칙

Movement mask:

- 미래 line을 직접 mask하지 않는다.
- 현재 `DamageTile`: hard danger.
- 현재 visible/current warning: 현재 구현처럼 danger feature/mask 후보.
- recent visible danger: 보수적 soft/hard 후보.
- fake blue marker: hard danger mask에 넣지 않는다.
- sweep 다음 band 예측 결과: hard mask에 넣지 않는다.

Attack mask:

- player가 current damage 위에 있으면 attack 금지.
- player가 current real danger 위에 있으면 attack 금지 후보.
- fake blue marker 위에 있다는 이유만으로 attack 금지하지 않는다.
- `boss_in_attack_range && player safe`이면 attack 가능.
- MarkATK VFX가 attack range와 겹친다는 이유만으로 attack을 막지 않는다.

원칙:

- mask는 현재/visible/recent danger 기반이어야 한다.
- 다음 line 예측은 policy가 visible history를 통해 학습하도록 둔다.
- mask가 미래 정답을 대신 계산하면 PPO가 회피 rule을 학습했다는 설명력이 약해진다.

## 13. Boss Sprite Visibility and Target Alignment

### 현재 타격 기준

현재 `PlayerCombat`의 보스 타격 판정은 collider hit가 아니다. 공격 시 `GetAttackCells(origin, direction)`로 2칸 전방 x 3폭 attack cells를 만들고, `cells.Contains(cachedBossCtrl.BossCell)`이면 `BossHealth.TakeDamage()`를 호출한다.

`ElevatorBossController.BossCell`은 `GridManager.Instance.WorldToCell(transform.position)`을 반환한다. 즉 `BossCell`은 보스 root transform의 world cell 기준이다.

### 왜 visible boss 기준과 어긋날 수 있는가

보스 visual은 `bossSpriteTransform` 자식 object로 관리된다. `SetBossVisible(false)`는 `bossSpriteTransform.gameObject.SetActive(false)`를 호출하지만, 주석상 BossHealth damageable 여부를 끄지는 않는다.

MarkDash에서는 다음이 동시에 일어난다.

- MarkATK VFX를 spawn한다.
- `SetBossVisible(false)`로 boss sprite를 숨긴다.
- 이후 damage dash에서 다시 `DashBossAlongArenaLine()`이 `SetBossVisible(true)`를 호출한다.

따라서 사용자가 보는 target은 "보이는 boss sprite"지만, 현재 hit/reward 기준은 "root transform 기반 BossCell"이다. 두 기준이 다르면 agent는 사람이 볼 수 없는 target을 때려 reward를 받는 정책을 학습할 수 있다.

### visible 판단 기준

진단 기준은 collider가 아니라 boss sprite visibility여야 한다.

사용할 기준:

- boss visual root activeSelf.
- boss visual root activeInHierarchy.
- SpriteRenderer enabled.
- SpriteRenderer.gameObject.activeInHierarchy.
- SpriteRenderer.sprite != null.
- SpriteRenderer.color.a > 0.01.
- 여러 boss body/visual SpriteRenderer 중 하나라도 visible인지.

주의:

- `Renderer.isVisible`만 단독 기준으로 쓰지 않는다. camera/frustum/background 조건에 따라 불안정할 수 있다.
- collider/trigger bounds를 주 판단 기준으로 쓰지 않는다. 이번 문제는 hitbox physics가 아니라 sprite ON/OFF와 BossCell target representation의 불일치다.

### 기록할 metric

다음 metric을 diagnostic logging 단계에서 기록한다.

- `hit_visible_boss_in_attack_range_count`
- `hit_bosscell_in_attack_range_but_visual_out_count`
- `boss_cell_visual_mismatch_count`
- `boss_health_visual_mismatch_count`
- `boss_cell_stale_during_dash_count`
- `boss_cell_stale_during_markdash_count`
- `hidden_boss_hit_count`
- `invisible_or_empty_space_hit_count`
- `boss_visible_false_but_hit_allowed_count`
- `boss_visible_true_but_bosscell_mismatch_count`
- `boss_sprite_visible_hit_count`
- `boss_sprite_invisible_hit_count`
- `boss_sprite_off_but_bosscell_hit_count`
- `boss_visual_root_inactive_hit_count`
- `boss_renderer_disabled_hit_count`
- `boss_alpha_zero_hit_count`
- `bosscell_in_attack_range_but_sprite_not_visible_count`
- `bosscell_in_attack_range_but_sprite_cell_out_of_range_count`
- `sprite_visible_cell_in_attack_range_count`
- `sprite_visible_cell_out_of_attack_range_count`
- `hidden_boss_attack_reward_count`

### Successful hit마다 기록할 항목

다음 구현 단계에서 successful boss hit마다 기록해야 한다.

- episode
- step
- time
- player cell
- player facing
- attack cells
- `ElevatorBossController.BossCell`
- `BossHealth` transform position
- boss visual/root transform position
- boss `GridOccupant` cell, 존재할 경우
- boss visible renderer bounds center
- boss sprite visible 여부
- boss visual root activeSelf / activeInHierarchy
- boss SpriteRenderer enabled 여부
- boss SpriteRenderer alpha
- boss sprite object name
- boss currently dashing/hidden/markdash 추정 상태
- BossCell inside attack cells 여부
- visual boss cell inside attack cells 여부
- boss damage amount

### 판정 기준

A. BossCell과 visible boss cell이 거의 항상 일치하면:

- 빈 공간 공격은 시각적 착시 또는 VFX 위치 문제일 가능성이 크다.
- BossCell 기준과 visual 기준은 대체로 일치한다고 볼 수 있다.

B. BossCell은 attack range 안인데 visible boss는 range 밖이면:

- RL이 hidden/stale target을 때리는 정책을 학습할 수 있다.
- `boss_in_attack_range` observation이 사람 기준 visible target과 불일치한다.

C. boss hidden 상태에서도 hit가 허용되면:

- 게임 규칙상 의도인지 확인해야 한다.
- 의도라면 "공격 대상은 visible boss가 아니라 BossCell state"라고 문서에 명시해야 한다.
- 의도가 아니라면 RL target representation bug로 분리해야 한다.

D. dash 중 BossCell 갱신이 늦거나 stale이면:

- 패턴 중 빈 공간 공격의 직접 원인일 수 있다.
- PlayerCombat 판정을 바로 수정하지 말고 metric으로 먼저 확정해야 한다.

### MarkATK gap과의 관계

이 문제는 MarkATK observation gap과 별개다.

- MarkATK gap: 화면에 보이는 red/blue marker가 vector observation에 없다.
- Target alignment gap: 보스 sprite가 보이는 위치와 `BossCell` hit/reward 기준이 다를 수 있다.

둘은 연결될 수 있지만 원인은 다르다. MarkDash 중 boss sprite가 숨겨진 상태에서 BossCell hit/reward가 허용된다면, MarkATK visible cue gap과 hidden target reward 문제가 동시에 나타날 수 있다.

## 14. 기존 checkpoint / ONNX 호환성

observation size가 변경되면 기존 ONNX/checkpoint는 그대로 호환되지 않는다.

필요한 작업:

- `BossRLStateExtractor.VectorObservationSize` 업데이트.
- BehaviorParameters vector observation size 업데이트.
- model input shape 변경 확인.
- 기존 ONNX inference 평가와 새 observation 설계 학습을 분리.
- 새 observation으로 fresh PPO 또는 호환 가능한 initialize-from 전략 재검토.

## 15. 구현 시 수정 파일 후보

실제 구현 허가 단계에서 수정 후보는 다음과 같다.

- `unity_project/Assets/Project/Scripts/RL/BossRLStateExtractor.cs`
- `unity_project/Assets/Project/Scripts/RL/BossPlayerAgent.cs`
- `unity_project/Assets/Project/Scripts/RL/BossRLDebugLogger.cs`
- `unity_project/Assets/Project/Scripts/Boss/ElevatorBossController.cs`
- `unity_project/Assets/Project/Scripts/Player/PlayerCombat.cs`
- `unity_project/Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity`
- `ml-agents-config/*.yaml`

주의:

- PlayerCombat 판정 자체는 바로 수정하지 않는다.
- 먼저 diagnostic metric으로 visible boss 기준과 BossCell 기준이 얼마나 어긋나는지 증명한다.
- reward/mask 변경은 observation gap과 target alignment gap을 분리한 뒤 결정한다.

## 16. 구현 순서 제안

1. Diagnostic logging만 추가한다.
2. boss sprite visibility helper를 만든다. 이 helper는 판정 변경이 아니라 logging 기준이다.
3. successful hit마다 BossCell/visual cell/visibility/attack cells를 기록한다.
4. MarkATK spawned VFX를 real/fake visible cue로 기록한다.
5. Phase2 sweep line history를 warning/damage history로 기록한다.
6. 짧은 Heuristic/Eval diagnostic으로 metric을 수집한다.
7. metric으로 hidden target reward와 sweep memory gap을 분리한다.
8. 그 뒤 observation size 변경 구현 여부를 결정한다.

## 17. 검증 계획

구현 허가 후 검증 항목:

- compile check.
- scene BehaviorParameters vector size 확인.
- MarkATK real/fake visible cue count logging.
- fake marker가 danger mask에 잘못 들어가지 않는지 확인.
- real marker가 observation에 들어오는지 확인.
- Phase2 sweep에서 previous warning/damage history가 기록되는지 확인.
- successful hit 시 boss sprite visible 여부 기록.
- `attack_out_of_range = 0` 유지.
- terminal reward `player_dead=-8`, `boss_dead=+5` 유지.
- 5K diagnostic 또는 5 episode eval로 NaN/exception/crash 없음 확인.

## 18. 리스크

- Observation size 증가는 기존 모델 호환성을 깨뜨린다.
- MarkATK marker를 hard danger로 넣으면 fake marker 때문에 잘못된 mask가 생길 수 있다.
- Sweep history를 너무 직접적으로 설계하면 next band 정답 유출에 가까워질 수 있다.
- BossCell/visual mismatch를 확인하지 않고 PlayerCombat을 수정하면 기존 게임 규칙을 깨뜨릴 수 있다.
- Diagnostic logging 없이 "빈 공간 hit"를 확정하면 착시와 실제 desync를 구분하지 못한다.

## 19. 이번 단계 금지 사항 준수

- 코드 수정: 하지 않음.
- scene 수정: 하지 않음.
- build 실행: 하지 않음.
- PPO 학습 실행: 하지 않음.
- heuristic 개선: 하지 않음.
- PlayerCombat 판정 수정: 하지 않음.
- `current_pattern`, `sweep_sequence_index`, `next_band` observation 제안: 하지 않음.

