from __future__ import annotations

import json
import math
import re
from dataclasses import dataclass
from pathlib import Path

import matplotlib.pyplot as plt
from tensorboard.backend.event_processing.event_accumulator import EventAccumulator


ROOT = Path(__file__).resolve().parents[1]
RUN_ID = "BossPPO_FastClear3HitFixed_1000k_0602_v1"
RUN_DIR = ROOT / "results" / RUN_ID
LOG_PATH = RUN_DIR / "run_logs" / "Player-0.log"
STATUS_PATH = RUN_DIR / "run_logs" / "training_status.json"
OUT_DIR = ROOT / "docs" / "training_visuals_20260602"


EPISODE_RE = re.compile(r"EPISODE_END reason=(?P<reason>\S+) steps=(?P<steps>\d+) survival=(?P<survival>[0-9.]+)s")
REWARD_RE = re.compile(
    r"reward: total=(?P<total>-?[0-9.]+) boss=(?P<boss>-?[0-9.]+) hit=(?P<hit>-?[0-9.]+) "
    r"critical_hp=(?P<critical>-?[0-9.]+) death=(?P<death>-?[0-9.]+) fast_clear=(?P<fast>-?[0-9.]+)"
)
BOSS_RE = re.compile(r"boss: hp_start=(?P<hp_start>\d+) hp_left=(?P<hp_left>\d+) dmg_dealt=(?P<dmg>\d+)")
ATTACK_RE = re.compile(r"attack_quality: actions=(?P<actions>\d+) hits=(?P<hits>\d+) missed=(?P<missed>\d+).*hit_rate=(?P<hit_rate>[0-9.]+)")


@dataclass
class Episode:
    index: int
    train_step: int
    reason: str
    episode_steps: int
    survival: float
    total_reward: float | None = None
    boss_reward: float | None = None
    hit_penalty: float | None = None
    fast_clear_reward: float | None = None
    hp_left: int | None = None
    dmg_dealt: int | None = None
    attacks: int | None = None
    hits: int | None = None
    hit_rate: float | None = None


def parse_episodes(log_path: Path) -> list[Episode]:
    lines = log_path.read_text(encoding="utf-8", errors="replace").splitlines()
    episodes: list[Episode] = []
    cumulative_step = 0
    pending: Episode | None = None

    for line in lines:
        episode_match = EPISODE_RE.search(line)
        if episode_match:
            cumulative_step += int(episode_match.group("steps"))
            pending = Episode(
                index=len(episodes) + 1,
                train_step=cumulative_step,
                reason=episode_match.group("reason"),
                episode_steps=int(episode_match.group("steps")),
                survival=float(episode_match.group("survival")),
            )
            episodes.append(pending)
            continue

        if pending is None:
            continue

        reward_match = REWARD_RE.search(line)
        if reward_match:
            pending.total_reward = float(reward_match.group("total"))
            pending.boss_reward = float(reward_match.group("boss"))
            pending.hit_penalty = float(reward_match.group("hit"))
            pending.fast_clear_reward = float(reward_match.group("fast"))
            continue

        boss_match = BOSS_RE.search(line)
        if boss_match:
            pending.hp_left = int(boss_match.group("hp_left"))
            pending.dmg_dealt = int(boss_match.group("dmg"))
            continue

        attack_match = ATTACK_RE.search(line)
        if attack_match:
            pending.attacks = int(attack_match.group("actions"))
            pending.hits = int(attack_match.group("hits"))
            pending.hit_rate = float(attack_match.group("hit_rate"))

    return episodes


def rolling(values: list[float], window: int) -> list[float]:
    output: list[float] = []
    queue: list[float] = []
    total = 0.0
    for value in values:
        queue.append(value)
        total += value
        if len(queue) > window:
            total -= queue.pop(0)
        output.append(total / len(queue))
    return output


def scalar(value: float | None, default: float = math.nan) -> float:
    return default if value is None else value


def save_line_chart(path: Path, title: str, x: list[int], series: list[tuple[str, list[float]]], ylabel: str) -> None:
    plt.figure(figsize=(12, 6), dpi=160)
    for label, values in series:
        plt.plot(x, values, linewidth=2, label=label)
    plt.title(title, fontsize=14, pad=12)
    plt.xlabel("Training steps")
    plt.ylabel(ylabel)
    plt.grid(True, alpha=0.28)
    plt.legend()
    plt.tight_layout()
    plt.savefig(path)
    plt.close()


def save_bar_chart(path: Path, title: str, labels: list[str], values: list[float], ylabel: str) -> None:
    plt.figure(figsize=(10, 5.5), dpi=160)
    colors = ["#3b82f6", "#22c55e", "#ef4444", "#f59e0b", "#8b5cf6", "#14b8a6"]
    plt.bar(labels, values, color=colors[: len(labels)])
    plt.title(title, fontsize=14, pad=12)
    plt.ylabel(ylabel)
    plt.grid(True, axis="y", alpha=0.25)
    plt.tight_layout()
    plt.savefig(path)
    plt.close()


def load_tensorboard_scalars(run_dir: Path) -> dict[str, list[tuple[int, float]]]:
    event_dir = run_dir / "BossPlayer"
    if not event_dir.exists():
        return {}

    accumulator = EventAccumulator(str(event_dir))
    accumulator.Reload()
    scalars: dict[str, list[tuple[int, float]]] = {}
    for tag in accumulator.Tags().get("scalars", []):
        scalars[tag] = [(event.step, event.value) for event in accumulator.Scalars(tag)]
    return scalars


def save_tensorboard_chart(
    path: Path,
    title: str,
    scalars: dict[str, list[tuple[int, float]]],
    tags: list[str],
    ylabel: str,
) -> None:
    plt.figure(figsize=(12, 6), dpi=160)
    for tag in tags:
        points = scalars.get(tag, [])
        if not points:
            continue
        x = [step for step, _ in points]
        y = [value for _, value in points]
        plt.plot(x, y, linewidth=2, label=tag)
    plt.title(title, fontsize=14, pad=12)
    plt.xlabel("Training steps")
    plt.ylabel(ylabel)
    plt.grid(True, alpha=0.28)
    plt.legend()
    plt.tight_layout()
    plt.savefig(path)
    plt.close()


def pct(value: float) -> str:
    return f"{value * 100:.1f}%"


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    episodes = parse_episodes(LOG_PATH)
    if not episodes:
        raise SystemExit(f"No episodes parsed from {LOG_PATH}")

    checkpoint_steps: list[int] = []
    checkpoint_rewards: list[float] = []
    final_checkpoint_step = 1_000_040
    if STATUS_PATH.exists():
        status = json.loads(STATUS_PATH.read_text(encoding="utf-8"))
        for checkpoint in status["BossPlayer"]["checkpoints"]:
            reward = checkpoint.get("reward")
            if reward is None:
                continue
            checkpoint_steps.append(int(checkpoint["steps"]))
            checkpoint_rewards.append(float(reward))
        final = status["BossPlayer"]["final_checkpoint"]
        final_checkpoint_step = int(final["steps"])
        if final.get("reward") is not None and int(final["steps"]) not in checkpoint_steps:
            checkpoint_steps.append(int(final["steps"]))
            checkpoint_rewards.append(float(final["reward"]))

    # Player-0.log contains per-episode internal action steps, while ML-Agents
    # reports trainer steps. Scale episode points onto the final checkpoint step
    # so these charts line up with TensorBoard and checkpoint metadata.
    final_episode_timeline_step = max(1, episodes[-1].train_step)
    x = [
        round(episode.train_step / final_episode_timeline_step * final_checkpoint_step)
        for episode in episodes
    ]
    clear_flags = [1.0 if episode.reason == "boss_dead" else 0.0 for episode in episodes]
    death_flags = [1.0 if episode.reason == "player_dead" else 0.0 for episode in episodes]
    total_rewards = [scalar(episode.total_reward) for episode in episodes]
    hp_left = [scalar(float(episode.hp_left)) for episode in episodes]
    dmg_dealt = [scalar(float(episode.dmg_dealt)) for episode in episodes]
    survival = [episode.survival for episode in episodes]
    hit_rates = [scalar(episode.hit_rate) for episode in episodes]

    window = 50
    save_line_chart(
        OUT_DIR / "01_clear_death_rate_by_step.png",
        f"{RUN_ID}: rolling clear/death rate ({window} episodes)",
        x,
        [
            ("Clear rate", rolling(clear_flags, window)),
            ("Death rate", rolling(death_flags, window)),
        ],
        "Rate",
    )
    save_line_chart(
        OUT_DIR / "02_reward_by_step.png",
        f"{RUN_ID}: episode reward by training step",
        x,
        [
            ("Episode total reward", total_rewards),
            (f"Rolling avg ({window})", rolling([0.0 if math.isnan(v) else v for v in total_rewards], window)),
        ],
        "Reward",
    )
    save_line_chart(
        OUT_DIR / "03_boss_damage_by_step.png",
        f"{RUN_ID}: boss HP left / damage dealt",
        x,
        [
            ("Boss HP left", hp_left),
            ("Damage dealt", dmg_dealt),
            (f"Damage rolling avg ({window})", rolling([0.0 if math.isnan(v) else v for v in dmg_dealt], window)),
        ],
        "HP / damage",
    )
    save_line_chart(
        OUT_DIR / "04_survival_and_hit_rate_by_step.png",
        f"{RUN_ID}: survival time and hit rate",
        x,
        [
            ("Survival seconds", survival),
            ("Hit rate %", hit_rates),
        ],
        "Seconds / percent",
    )

    if checkpoint_steps:
        save_line_chart(
            OUT_DIR / "05_checkpoint_reward_by_step.png",
            f"{RUN_ID}: checkpoint reward trend",
            checkpoint_steps,
            [("Checkpoint reward", checkpoint_rewards)],
            "Reward",
        )

    tensorboard_scalars = load_tensorboard_scalars(RUN_DIR)
    if tensorboard_scalars:
        save_tensorboard_chart(
            OUT_DIR / "07_tensorboard_reward_length.png",
            f"{RUN_ID}: TensorBoard reward and episode length",
            tensorboard_scalars,
            ["Environment/Cumulative Reward", "Environment/Episode Length"],
            "Reward / length",
        )
        save_tensorboard_chart(
            OUT_DIR / "08_tensorboard_losses_policy.png",
            f"{RUN_ID}: TensorBoard losses and policy stats",
            tensorboard_scalars,
            ["Losses/Policy Loss", "Losses/Value Loss", "Policy/Entropy"],
            "Scalar value",
        )

    ranges = [
        ("0-200k", 0, 200_000),
        ("200-400k", 200_000, 400_000),
        ("400-600k", 400_000, 600_000),
        ("600-800k", 600_000, 800_000),
        ("800-1000k", 800_000, 1_050_000),
    ]
    range_labels: list[str] = []
    range_rates: list[float] = []
    for label, start, end in ranges:
        chunk = [episode for episode in episodes if start < episode.train_step <= end]
        if not chunk:
            continue
        range_labels.append(label)
        range_rates.append(sum(1 for episode in chunk if episode.reason == "boss_dead") / len(chunk))

    save_bar_chart(
        OUT_DIR / "06_clear_rate_by_step_band.png",
        f"{RUN_ID}: clear rate by training-step band",
        range_labels,
        range_rates,
        "Clear rate",
    )

    last_30 = episodes[-30:]
    summary = {
        "run_id": RUN_ID,
        "episodes": len(episodes),
        "clears": sum(1 for episode in episodes if episode.reason == "boss_dead"),
        "deaths": sum(1 for episode in episodes if episode.reason == "player_dead"),
        "timeouts": sum(1 for episode in episodes if episode.reason == "timeout"),
        "last_30_clears": sum(1 for episode in last_30 if episode.reason == "boss_dead"),
        "last_30_deaths": sum(1 for episode in last_30 if episode.reason == "player_dead"),
        "last_30_clear_rate": pct(sum(1 for episode in last_30 if episode.reason == "boss_dead") / len(last_30)),
        "final_checkpoint_step": final_checkpoint_step,
        "cumulative_episode_action_steps": episodes[-1].train_step,
        "checkpoint_steps": checkpoint_steps,
        "checkpoint_rewards": checkpoint_rewards,
        "tensorboard_scalar_tags": sorted(tensorboard_scalars.keys()),
    }
    (OUT_DIR / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")

    report = f"""# Training Step Visuals - {RUN_ID}

Generated from:

- `{LOG_PATH.relative_to(ROOT).as_posix()}`
- `{STATUS_PATH.relative_to(ROOT).as_posix()}`

## Summary

- Total parsed training episodes: {summary["episodes"]}
- Clears / deaths / timeouts: {summary["clears"]} / {summary["deaths"]} / {summary["timeouts"]}
- Last 30 training episodes: {summary["last_30_clears"]}/30 clears, {summary["last_30_deaths"]}/30 deaths
- Last 30 clear rate: {summary["last_30_clear_rate"]}
- Final checkpoint step: {summary["final_checkpoint_step"]:,}
- Episode-log x-axis: episode action timeline scaled to final checkpoint step for comparison with TensorBoard

## Figures

![Rolling clear and death rate](01_clear_death_rate_by_step.png)

![Episode reward by step](02_reward_by_step.png)

![Boss damage by step](03_boss_damage_by_step.png)

![Survival and hit rate by step](04_survival_and_hit_rate_by_step.png)

![Checkpoint reward by step](05_checkpoint_reward_by_step.png)

![Clear rate by step band](06_clear_rate_by_step_band.png)

![TensorBoard reward and episode length](07_tensorboard_reward_length.png)

![TensorBoard losses and policy stats](08_tensorboard_losses_policy.png)
"""
    (OUT_DIR / "README.md").write_text(report, encoding="utf-8")

    print(f"Wrote visuals to {OUT_DIR}")


if __name__ == "__main__":
    main()
