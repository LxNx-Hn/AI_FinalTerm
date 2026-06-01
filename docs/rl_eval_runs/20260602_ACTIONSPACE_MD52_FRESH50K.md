# Action Space [5,2] — Fresh50K result & next-bottleneck diagnosis

- **Repo / branch / commit**: `AI_FinalTerm_MLAgents_PPO_NEW` · `rl-observation-integration-task1-task2` · HEAD `f08cd01` (working tree has the uncommitted [5,2] change)
- **Runs**: smoke `BossPPO_ActionSpace_MD52_5kSmoke_v1` · fresh50k `BossPPO_ActionSpace_MD52_Fresh50K_v1`
- **Date**: 2026-06-02
- **Change under test**: action spec Single Discrete **[6] → MultiDiscrete [5,2]** (move branch ∥ attack branch). Obs **438 unchanged**. No PlayerCombat/BossHealth/reward/boss-pattern change.

---

## TL;DR

The action-space change **did exactly what the audit predicted at the structural level** — attack
is no longer starved, and the agent now moves and attacks in the same decision like a human.
**But total boss damage did not improve** (7.6 vs 10.0/ep) because the change *exposed a reward
flaw*: `SafeInRangeAttackAttemptReward (+0.08)` pays for attack **attempts regardless of whether
they land**. With attacking now "free" (simultaneous with dodging), the policy farms attempt-reward
(**+104** total) over actual damage (**+51** total), so it **spams inaccurate attacks** (hit-rate
**39.4%**, 1300 attacks → only 512 land).

The bottleneck has moved from *"structurally cannot attack"* (hard; required the action-space
change — **done**) to *"reward does not reward accurate attacks"* (one isolated reward edit).
All anti-exploit / integrity guarantees still hold. Phase 3 not reached → **do not scale to 100K**;
do the reward edit next.

---

## Result vs the [6] baseline (Fresh50K v2)

| metric | [6] baseline v2 | **[5,2] v1** | read |
|---|---|---|---|
| episodes / outcome | 69 / all player_dead | 67 / all player_dead | boss_dead 0, timeout 0 |
| boss_damage avg / max | 10.0 / 24 | **7.6 / 22** | ↓ (see cause) |
| min boss HP reached | 36 | **38** | Phase 3 (≤24) not reached |
| **attacks (total)** | 691 | **1300** | ↑↑ attack no longer starved |
| **hits landed (total)** | 691 | 512 | ↓ — accuracy collapsed |
| **hit_rate** | 100%* | **39.4%** | *[6]=100% only because it never moved while attacking |
| attack % of actions | 1.40% | ~3.3% | ↑ |
| **safe_opp_attack_ratio** | 19.7% | **41.4%** | ↑↑ converts far more opportunities |
| **simultaneous move+attack** | 0 (impossible) | **992** (204 landed) | the human capability, realized |
| survival avg / max | 71.5 / 166.5 s | 72.9 / 184.3 s | ~equal |
| mean reward (final) | ~−9 | −17.2 | ↓ (more exposure for inaccurate spam) |

\* The [6] agent's 100% hit-rate was an artifact of *stationary* gated attacks — it physically
could not miss because it never moved while attacking. Its low damage came from attacking only
691 times (starvation). The [5,2] agent attacks 1300× but the reward never makes it aim.

---

## Why damage didn't rise — reward decomposition (per-episode avg, 67 eps)

| term | per-ep | total | note |
|---|---:|---:|---|
| **safe_atk attempt (+0.08/attempt)** | **+1.55** | **+104.0** | paid for *attempts*, land or miss |
| boss_damage (+0.10/HP) | +0.76 | +51.2 | paid only for *landed* HP |
| hit penalty | −4.00 | — | more exposure from spam |
| warn-tile dwell | −9.69 | — | unchanged dominant safety term |

**The agent earns 2× more reward from attempting attacks (+104) than from landing them (+51).**
Since attacking is now free (simultaneous with the dodge), the gradient pulls the attack branch
toward *maximum attempt volume*, not accuracy → 1300 attacks at 39% hit-rate.

---

## Integrity / anti-exploit — ALL still hold (67 eps)

| check | value | verdict |
|---|---:|:--:|
| hidden_target_successful_hit_after_gate | 0 | ✅ |
| off_lane_empty + stale_bosscell hits | 0 | ✅ |
| atk_out_range | 0 | ✅ |
| rl_attack_blocked_hidden_target | 599 | ✅ gate working |
| root_visible_overlap / dash_current_overlap allowed (hits) | 240 / 20 | ✅ not blocked |
| fake_marker_in_danger_mask / used_by_attack_mask | 0 / 0 | ✅ |
| next_band / sweep_sequence_index leak | 0 / 0 | ✅ |
| recent_sweep_history_nonzero_steps | 45,617 | ✅ |
| markatk_real_spawn / observed | 0 / 0 | Phase 3 not reached → expected |
| NaN / exception / timeout | 0 | ✅ (only benign ONNX opset-9 export fallback) |

## Simultaneity metrics (the new instrumentation, summed)

`move_branch=37612 · attack_branch_intent=1300 · attack_applied=1300 ·
simultaneous_move_attack=992 (allowed 992 / blocked 0) ·
simultaneous_move_attack_successful_hit=204 · attack_while_moving=524 ·
diagonal_blindspot_move_attack=547 · dash_current_overlap_move_attack=6 · safe_opp_attack_ratio≈41.4%`

Note: the diagonal blindspot — which in [6] yielded **0** hits from 405 attacks — now produces
real move+attack actions (547) because the agent can attack while repositioning. (Still
logging-only; not in obs/reward.)

---

## Decision & recommendation

- Per the run gate (max damage 22 < 35, Phase 3 not reached): **do NOT scale to 100K.** Re-analyze
  first (done above).
- **Next step (one isolated reward change, needs approval):** make the attack reward contingent on
  **landing**, not attempting. Options, in order of preference:
  1. Move the `+0.08` from "safe in-range attack **attempt**" to the **landed-hit** path (reward the
     hit, e.g. fold into / raise `BossDamagePerHp`), so accuracy — not spam — is what pays.
  2. Or set `SafeInRangeAttackAttemptReward = 0` (remove the attempt bonus entirely) and lean on
     `BossDamagePerHp` for the signal.
  This keeps every constraint: no boss/HP/PlayerCombat/BossHealth change, no internal-pattern
  signal, no danger-attack reward, no diagonal_blindspot reward. It directly converts the now-abundant
  attacks into damage by paying for aim.
- The action-space change is **validated and should stay**: it removed the structural barrier and
  cleanly isolated the remaining lever.
