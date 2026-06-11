"""Parse BossRL Player-0.log into per-episode rows (stats + wall-clock window).

Usage: python parse_episodes.py <player_log> [--csv out.csv]
Prints a compact table to stdout; wall_start/wall_end come from the
[BossRLReset] realtime markers so rows can be mapped onto a screen capture.
"""
import argparse
import csv
import json
import re
import sys
from pathlib import Path


def metric(pattern, block, default=0, cast=int):
    found = re.search(pattern, block)
    return cast(found.group(1)) if found else default


def parse_log(path: Path):
    text = path.read_text(encoding="utf-8", errors="replace")
    ends = list(
        re.finditer(
            r"\[BossRL\] EPISODE_END reason=(\w+) steps=(\d+) survival=([0-9.]+)s",
            text,
        )
    )
    rows = []
    previous_after_scene = None
    for index, match in enumerate(ends, 1):
        block_end = ends[index].start() if index < len(ends) else len(text)
        block = text[match.start():block_end]
        queue = re.search(r"\[BossRLReset\] queue_reload reload_count=\d+ time=([0-9.]+)", block)
        after_scene = re.search(r"\[BossRLReset\] after_scene_loaded reload_count=\d+ time=([0-9.]+)", block)

        reason, steps, survival_text = match.groups()
        survival = float(survival_text)
        wall_end = float(queue.group(1)) if queue else None
        if previous_after_scene is not None:
            wall_start = previous_after_scene
        elif wall_end is not None:
            wall_start = max(0.0, wall_end - survival)
        else:
            wall_start = None

        rows.append(
            {
                "episode": index,
                "reason": reason,
                "steps": int(steps),
                "survival_s": survival,
                "wall_start": wall_start,
                "wall_end": wall_end,
                "boss_damage": metric(r"dmg_dealt=(\d+)", block),
                "boss_hp_left": metric(r"boss: hp_start=\d+ hp_left=(\d+)", block),
                "attack_actions": metric(r"attack_quality: actions=(\d+)", block),
                "attack_hits": metric(r"attack_quality: actions=\d+ hits=(\d+)", block),
                "escape_success": metric(r"warning_on_player:.*?escape_success=(\d+)", block),
                "escape_fail": metric(r"warning_on_player:.*?escape_fail=(\d+)", block),
                "player_hits": metric(r"player: hits=(\d+)", block),
                "sweep_steps": metric(r"phase2_sweep_history_active_steps=(\d+)", block),
                "mark_real": metric(r"markatk_real_spawn_count=(\d+)", block),
                "mark_fake": metric(r"markatk_fake_spawn_count=(\d+)", block),
                "safe_atk_pct": metric(r"safe_opp_attack_ratio=([0-9.]+) %", block, 0.0, float),
            }
        )
        previous_after_scene = float(after_scene.group(1)) if after_scene else wall_end
    return rows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("player_log", type=Path)
    ap.add_argument("--csv", type=Path)
    ap.add_argument("--json", type=Path)
    args = ap.parse_args()

    rows = parse_log(args.player_log)
    if not rows:
        print("no episodes found", file=sys.stderr)
        sys.exit(1)

    if args.csv:
        with args.csv.open("w", newline="", encoding="utf-8-sig") as handle:
            writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
            writer.writeheader()
            writer.writerows(rows)
    if args.json:
        args.json.write_text(json.dumps(rows, indent=2), encoding="utf-8")

    header = (
        "ep  reason       surv    dmg hpL atk(h/a) esc(s/f) hit sweep mark(r+f) safe%"
    )
    print(header)
    for r in rows:
        print(
            f"E{r['episode']:02d} {r['reason']:<12} {r['survival_s']:6.1f}s "
            f"{r['boss_damage']:3d} {r['boss_hp_left']:3d} "
            f"{r['attack_hits']:3d}/{r['attack_actions']:<3d} "
            f"{r['escape_success']:3d}/{r['escape_fail']:<3d} "
            f"{r['player_hits']:3d} {r['sweep_steps']:5d} "
            f"{r['mark_real']}+{r['mark_fake']:<4d} {r['safe_atk_pct']:5.1f}"
        )


if __name__ == "__main__":
    main()
