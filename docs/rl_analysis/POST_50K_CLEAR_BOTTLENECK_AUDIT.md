# Post-50K Clear Bottleneck Audit

> Goal of this audit: explain — strictly from logged evidence, no guessing — **why the
> policy completes a full 50,000-step run yet caps at `boss_damage max = 24/60` and never
> reaches Phase 3.** Then decide whether the human and the RL agent operate under the same
> action conditions.

- **Repo / branch / commit**: `AI_FinalTerm_MLAgents_PPO_NEW` · `rl-observation-integration-task1-task2` · `959791d`
- **Run analyzed**: `BossPPO_TargetGate_MarkATKObs_SweepHistory_Fresh50K_v2`
- **Log**: `results/BossPPO_TargetGate_MarkATKObs_SweepHistory_Fresh50K_v2/run_logs/Player-0.log` (7,015 lines, 69 episodes)
- **Date**: 2026-06-02
- **Method**: aggregation over all 69 episode-summary blocks + per-step / per-hit lines. Every number below is summed/averaged directly from the log; items not present in the log are marked **[not logged]**.

---

## 0. TL;DR — Root cause

The agent is **structurally unable to do what a human does: move and attack in the same
moment.** A human reads two independent input channels every frame (keyboard→move via
`PlayerController`, mouse→attack via `PlayerCombat`); the RL agent emits **one** discrete
action per decision (`BranchSizes: 06000000` = Single Discrete [6]) and the attack path
**explicitly clears movement** (`BossRLInputBridge.ApplySingleAction` → `ClearExternalInput()`).

Consequences, measured:

1. **Attack is 1.40 % of all actions** (691 / 49,320). The attack action shares one softmax
   with 5 move/wait options and is starved.
2. **80.3 % of safe, gate-allowed, in-range attack opportunities are not converted** — on
   those steps the agent moves 62.6 % / waits 17.8 % and attacks only 19.7 %.
3. Every attack it *does* fire **hits (691/691 = 100 %)** — the problem is volume, not aim.
4. Reward is **survival-biased**: positive terms (+124) are 7 % of the magnitude of negative
   terms (−1,730); the single largest reward component is **warn-tile dwell (−9.55/ep)**,
   larger than death (−8.0). Boss damage contributes only **+1.0/ep**.

→ Bottleneck #1 (primary, structural): **single-discrete action space ≠ human's two parallel
input channels.** Bottleneck #2 (secondary, compounding): **reward incentivizes not-dying ~15×
more strongly than dealing damage,** so the single action slot is "correctly" spent dodging.

Fixing the action space to **MultiDiscrete [5, 2]** (move branch ∥ attack branch) relieves
*both*: attacking no longer costs a dodge, so the agent can attack on every safe in-range
step while still evading — exactly the human condition.

---

## 1. Episode-level results (checklist items 1, 3)

| Metric | Value |
|---|---|
| Episodes | 69, **all `player_dead`** (boss_dead 0, timeout 0) |
| Survival | avg **71.5 s**, max **166.5 s** |
| Boss damage / ep | avg **10.0**, max **24**, min 1 |
| **Min boss HP reached (whole run)** | **36 / 60** |
| Player hits / ep | avg **2.0** |
| First hit time / ep | avg **21.6 s** |
| Death time / ep | = survival (all deaths) |
| Stability | **0** matches for nan / exception / timeout / stuck / error |

Boss-damage distribution is a smooth hump centred ~6–12, hard-capped at 24 (1 episode):
no episode ever crossed into Phase 3.

**Phase reach (item 3).** Thresholds from `ElevatorBossController` (`phase2HpPercent=70`,
`phase3HpPercent=40`, `finalPhaseHpPercent=10`; boss maxHP = 60):

| Phase | HP gate | Reached? |
|---|---|---|
| Phase 1 | start | ✅ |
| Phase 2 | ≤ 42 HP | ✅ (min HP 36 < 42; `phase2_sweep_history_active_steps = 45,657`) |
| **Phase 3** | **≤ 24 HP** | ❌ **never** (best was 36 HP) |
| Final | ≤ 6 HP | ❌ |

To *touch* Phase 3 the agent must deal **36 damage**; current max is **24**. It is **12 damage
(50 %) short** of even entering Phase 3 once.

**Time-trend of damage (item 2).** Per-hit timestamps (`target_alignment_hit time=…`) show hits
arriving roughly every 8–10 s and **not accelerating** as the episode proceeds; damage accrual
is linear-and-sparse, then the player dies. There is no "burst" phase. (Fine-grained per-second
binning is **[not logged]** as an aggregate, but per-hit times are.)

---

## 2. The damage funnel — where the 60→24 gap is lost (items 4, 5, 8, 9, 10)

```
49,320 total actions
   ├─ move   38,205 (77.5 %)
   ├─ wait   10,424 (21.1 %)
   └─ attack    691 ( 1.40 %)   ← attack is starved

Steps in attack range:  5,261 / 49,320 = 10.7 %     ← 89.3 % of the fight is spent OUT of range
   └─ of those, an attack happens on   691 = 13.1 %  ← even when in range, it mostly dodges

Safe attack opportunities (gate-allowed, in-range, no danger): 3,514
   ├─ converted to ATTACK   691 (19.7 %)
   ├─ chose MOVE          2,199 (62.6 %)   ← of which away 32.1 %, lateral 39.1 %, toward 28.8 %
   └─ chose WAIT            624 (17.8 %)
   → 80.3 % of safe opportunities MISSED

Attacks fired 691 → hits 691 (100 %), missed 0, cooldown-waste 0
```

Read top-to-bottom: the agent (a) is in range only **10.7 %** of the time, (b) attacks only
**13.1 %** of in-range steps, yet (c) **never misses**. Damage is throttled entirely by *how
seldom it chooses to attack*, not by aim, range gating, or cooldown.

The move-direction split inside safe opportunities is the tell: when it is **already safe and
in range**, the agent moves **away (32.1 %)** *more often than toward (28.8 %)* the boss. It is
not repositioning to attack — it is leaving a winning position.

**Range re-entry (item 10).** `entered_attack_range = 3,192`, `left_attack_range = 3,179`. The
agent constantly re-enters and re-leaves range (≈ balanced), i.e. it *can* return to attack
range after evading — it simply doesn't stay to attack. So "can't get back in range" is **ruled
out**; "won't commit an attack once back in range" is the actual behavior.

---

## 3. Attack-gate & target classification (items 6, 7, 15) — all clean

`rl_target_gate` summed over the run:

| Allowed class | count | | Blocked class | count |
|---|---:|---|---|---:|
| normal_visible | 1,629 | | hidden_target | **506** (correctly blocked) |
| root_visible_overlap | 1,316 | | stale_bosscell | 0 |
| dash_current_overlap | 63 | | off_lane_empty | 0 |

Successful hits after the gate, by class: `root_visible_overlap = 305`, `dash_current_overlap = 12`,
**`hidden_target = 0`**, `bosscell_only_empty_space = 0`.

- **No exploit leak**: zero hidden-target or empty-space hits passed the gate.
- **`atk_out_of_range = 0`** for the whole run — the out-of-range swing problem is fully solved.
- **dash_current_overlap (item 15)**: the 12 successful dash-overlap hits occur *while the boss is
  dashing through the player's attack rectangle* (`dashing=True dash_current_in_attack=True
  active_damage_overlap_attack=True`). These are legitimate, visually-cued hits on a moving boss —
  rare (12) because the agent is rarely positioned on the dash line with attack ready.

The Target Gate is doing its job. It is **not** the bottleneck; it correctly permits visible
overlaps and blocks invisible ones.

---

## 4. Diagonal-blindspot behavior (items 12, 13, 14) — survives, deals 0 damage

`diagonal_blindspot_wiggle_diag` summed:

- `diagonal_blindspot_attack_count = 405` → **`diagonal_blindspot_attack_hit_count = 0`**
- `diagonal_blindspot_move_count = 4,660`, `attack_after_diagonal_wiggle = 0`
- per-ep `axis_vs_diagonal_hit_rate_delta ≈ −0.006` (negligible), `player_in_boss_diagonal_blindspot`
  is the single most-occupied relative position.

Interpretation: the agent has discovered the **diagonal blindspot as a survival pocket** (boss
melee whiffs there) and wiggles in it — but the player attack rectangle is axis-aligned forward,
so from a diagonal cell the boss cell is **not** in the attack area: **405 attacks there → 0 hits.**
The blindspot buys survival, not damage. This is consistent with the whole-run thesis: the agent
optimizes for not-dying and parks in a spot from which it *cannot* attack.

> Constraint honored: `diagonal_blindspot` remains **logging-only** — it is **not** in any
> observation and **not** in any reward, and this audit does **not** propose adding it.

---

## 5. Reward contribution audit (Q3 / item: return decomposition)

Summed over 69 episodes (per-episode average in parentheses):

| Component | Sum | Avg/ep | Sign |
|---|---:|---:|:--:|
| **boss_damage** | **+69.1** | **+1.00** | + |
| **safe_atk attempt** | **+55.3** | **+0.80** | + |
| warn_tile dwell | −659.1 | **−9.55** | − |
| death | −552.0 | −8.00 | − |
| player hit | −276.0 | −4.00 | − |
| moved_into_danger | −198.1 | −2.87 | − |
| dmg_tile | −38.8 | −0.56 | − |
| missed_safe_opp | −5.7 | −0.08 | − |
| **TOTAL** | **−1,654.7** | **−23.98** | − |

**Positive total +124.4 vs negative total −1,729.7 → ratio 0.072.** Damage-positive reward is
**7 %** of the safety-negative reward magnitude.

Key observations:

1. **The dominant single term is warn-tile dwell (−9.55/ep), larger than death (−8.0).** The
   policy's strongest gradient is "get off / stay off warning tiles." Since the boss generates
   warning tiles *around itself* when it attacks, this term **pushes the agent away from the boss**,
   directly suppressing time-in-range (measured at 10.7 %).
2. **A single player hit (−2.0) costs as much as 20 boss-HP of damage (+0.10 each) or 25 safe-attack
   attempts (+0.08).** A death (−8.0) outweighs dealing the *entire* boss bar (60 × 0.10 = +6.0 plus
   +5.0 kill). Under this scaling the return-optimal policy is "dodge maximally, attack only when
   completely free" — which is **exactly the observed policy.**
3. `missed_safe_opp` is tiny (−0.08/ep) — it is **not** a meaningful counter-pressure against
   skipping attacks. `missed_atk` and `cooldown` are 0 (gate already prevents wasteful swings).

This confirms Q3: **reward is survival-biased.** But note the structural coupling — even with a
balanced reward, a single-discrete agent must still *trade* a dodge for each attack. The reward
bias and the action-space limit are the same coin: the reward makes "spend the one action slot on
dodging" correct, and the action space makes that the only way to dodge.

---

## 6. MarkATK / sweep observation integrity (success-criteria items)

| Check | Value | Verdict |
|---|---:|:--:|
| `markatk_real_spawn` | 0 | Phase 3 never reached → MarkATK is Phase-3 content, so 0 is **expected, not a bug** |
| `markatk_real_observed` | 0 | same |
| `next_band_direct_observation` (leak) | **0** | ✅ no leak |
| `sweep_sequence_index_observation` (leak) | **0** | ✅ no leak |
| `phase2_sweep_history_active_steps` | 45,657 | ✅ sweep history populated in Phase 2 |
| fake-marker in danger/attack mask | 0 | ✅ no fake leak |

MarkATK observation quality **cannot be validated yet** because Phase 3 is unreachable at the
current damage output. **Reaching Phase 3 is a prerequisite for testing MarkATK** — which is
itself gated on dealing ≥ 36 damage, i.e. on fixing the bottleneck above.

---

## 7. Direct answer — why boss_damage caps at 24 (item 16)

1. The agent attacks on only **691 of 49,320 steps (1.40 %)**; each lands (100 %), each deals 1 HP.
   ~10 attacks/episode → ~10 damage/episode, best-case 24.
2. It attacks so rarely because **every attack forfeits a dodge** (single action channel), and the
   **reward punishes exposure ~15× more than it rewards damage**. So the policy spends 99 % of
   actions moving/waiting and is in range only 10.7 % of the time, converting only 13.1 % of those.
3. It is **not** limited by aim (100 % hit), range gating (`atk_out_range=0`), cooldown (0 waste),
   or the Target Gate (blocks only invisible targets). It is limited by **attack frequency**, which
   is a direct product of the action-space structure and the reward balance.
4. 24 < 36, so Phase 3 (and therefore MarkATK validation) is never reached.

---

## 8. Action-Space Audit (Section 5) — Human vs RL: result = **A (mismatch)**

**Human input path (code-verified):**

- `PlayerController.Update()` reads the keyboard *every frame* and moves (`mover.Move`).
- `PlayerCombat.Update()` reads the mouse *every frame*, independently, and attacks
  (`attackRequested = externalAttackRequested || Input.GetMouseButtonDown(0)`); `Attack()` uses
  `controller.Facing` and `occupant.CurrentCell`.
- These are **two separate components, two separate `Update()` loops, two separate input devices.**
  Nothing serializes them → **a human can move and attack in the same frame.**
- `GridMover.MoveRoutine` sets `occupant.SetCell(target)` at move **start** (before the 0.23 s lerp),
  so a human attacking mid-move strikes **from the destination cell** — i.e. "attack while advancing"
  is natively available.

**RL input path (code-verified):**

- Scene `BehaviorParameters`: `VectorObservationSize: 438`, `BranchSizes: 06000000` (**Single
  Discrete [6]**), `DecisionPeriod: 5`, `TakeActionsBetweenDecisions: 0`.
- `BossPlayerAgent.OnActionReceived` reads **one** int and calls `inputBridge.ApplySingleAction`.
- `BossRLInputBridge.ApplySingleAction`: a move action calls `SetExternalInput(dir)` **only**; an
  attack action calls **`ClearExternalInput()` then** `RequestExternalAttack()`. → The agent does
  **exactly one** of {move, attack, wait} per decision, and attacking **actively cancels movement.**
- Because an attack step calls `ClearExternalInput()`, `PlayerController.ReadInput()` returns zero
  and **`Facing` does not update on attack steps** — the agent attacks in its *last-moved*
  direction.

| | Human | RL agent |
|---|---|---|
| Move + attack in same frame | **Yes** (2 input channels) | **No** (1 discrete slot; attack clears move) |
| Attack while moving / advancing | Yes (`Attack()` ignores `IsMoving`; cell already = dest) | Only on a *separate later* decision |
| Facing used for attack | updated by same-frame move input | frozen at last move's direction |
| Channels per step | **2 (parallel)** | **1 (mutually exclusive)** |

**Conclusion: Result A.** The human and RL agent do **not** share the same action conditions. The
human has two parallel action channels; the RL agent has one mutually-exclusive choice. This
asymmetry is the structural mechanism behind the measured symptoms (attack 1.40 % of actions,
80.3 % of safe opportunities skipped, time-in-range 10.7 %). Per Section 6 of the task brief, the
next implementation step is therefore to change the action space to **MultiDiscrete [5, 2]**.

---

## 9. Recommendation

**Primary fix — match the human's two input channels: `MultiDiscrete [5, 2]`.**

- Branch 0 (move intent): `0 none · 1 up · 2 down · 3 left · 4 right`
- Branch 1 (attack intent): `0 no-attack · 1 attack`
- Movement danger mask → Branch 0; attack gate (`IsRLAttackAllowed`) + range/danger mask → Branch 1.
- When both fire: `SetExternalInput(dir)` **and** (if gate-allowed) `RequestExternalAttack()` — the
  same primitives the human path uses; **no change** to `PlayerCombat`, `BossHealth`, `GridMover`,
  boss patterns, HP, or damage. Observation size stays **438**.
- This is a structural-equivalence change, **not** answer leakage: it grants the agent the move+attack
  simultaneity the human already has, nothing about boss internals.

**Why this should lift the cap:** on a safe in-range step the attack branch can output "attack"
*independently* of the move branch choosing a dodge. The agent no longer pays mobility for damage,
so (a) staying in range stops being punished by lost dodges and (b) the 80.3 % missed-opportunity
rate should collapse. Target the intermediate success bar: **boss_damage max ≥ 35 and/or Phase 3
reached.**

**Secondary (only if [5,2] alone is insufficient): reward rebalance, one change at a time.** The
audit points first at **warn-tile dwell (−9.55/ep)** as the term most responsible for pushing the
agent off the boss; reducing it (or capping its per-episode accumulation) is the highest-leverage,
lowest-risk reward edit *because it does not reward danger* — it only stops over-penalizing
proximity. Do **not** touch damage-side scaling until the action-space change is measured. Do
**not** add any internal-pattern signal to reward.

### Constraints honored by this audit
No CODE-BLUE edits · no boss difficulty / HP / damage change · no `PlayerCombat`/`BossHealth`/
`GridMover` logic change · no `current_pattern` / `next_pattern` / `next_band` /
`sweep_sequence_index` / hidden-hurtbox / internal-timer / `diagonal_blindspot` observation or
reward proposed · no heuristic clear · no blind 100K.

---

## 10. Open items / next steps

1. Implement `MultiDiscrete [5, 2]` (`BossRLInputBridge`, `BossPlayerAgent`, `BossRLDebugLogger`
   metrics, scene `BehaviorParameters`, trainer config, run script). Fresh training required —
   existing checkpoint/ONNX are action-spec-incompatible.
2. Add metrics: simultaneous move+attack count (allowed/blocked), attack-while-moving,
   safe-opp attack ratio after the change, dash_current_overlap move+attack.
3. 5K smoke → verify action spec `[5,2]`, simultaneous move+attack appears in logs,
   `atk_out_of_range=0`, hidden-after-gate=0, no NaN/timeout.
4. If smoke clean → Fresh50K. If `boss_damage max ≥ 35` or Phase 3 reached → Fresh100K.
   If still ~24 → re-analyze before scaling (likely move to the warn-tile reward edit).
