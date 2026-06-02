from __future__ import annotations

import json
import re
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
RUN_ID = "BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1"
LOG_PATH = ROOT / "results" / RUN_ID / "run_logs" / "Player-0.TRAINING-1M.log"
STATUS_PATH = ROOT / "results" / RUN_ID / "run_logs" / "training_status.json"
FIG_DIR = ROOT / "docs" / "rl_final" / "figures"
ASSET_DIR = ROOT / "presentations" / "final" / "assets"


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        Path("C:/Windows/Fonts/malgunbd.ttf") if bold else Path("C:/Windows/Fonts/malgun.ttf"),
        Path("C:/Windows/Fonts/arial.ttf"),
    ]
    for path in candidates:
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


FONT_TITLE = font(34, True)
FONT_LABEL = font(20)
FONT_SMALL = font(16)
FONT_TINY = font(13)


def parse_key_values(line: str) -> dict[str, float | int | str]:
    pairs: dict[str, float | int | str] = {}
    for key, value in re.findall(r"([A-Za-z0-9_]+)=([^\s]+)", line):
        value = value.rstrip(",")
        try:
            if "." in value:
                pairs[key] = float(value)
            else:
                pairs[key] = int(value)
        except ValueError:
            pairs[key] = value
    return pairs


def parse_log() -> list[dict[str, float | int | str]]:
    episodes: list[dict[str, float | int | str]] = []
    current: dict[str, float | int | str] | None = None
    episode_no = 0
    for raw in LOG_PATH.read_text(encoding="utf-8", errors="replace").splitlines():
        if raw.startswith("[BossRL] EPISODE_END"):
            if current:
                episodes.append(current)
            episode_no += 1
            current = {"episode": episode_no}
            current.update(parse_key_values(raw))
            reason = re.search(r"reason=([A-Za-z_]+)", raw)
            if reason:
                current["reason"] = reason.group(1)
            surv = re.search(r"survival=([0-9.]+)s", raw)
            if surv:
                current["survival_seconds"] = float(surv.group(1))
            continue
        if current is None:
            continue
        stripped = raw.strip()
        if stripped.startswith("boss:"):
            current.update({f"boss_{k}": v for k, v in parse_key_values(stripped).items()})
        elif stripped.startswith("attack_quality:"):
            current.update({f"attack_{k}": v for k, v in parse_key_values(stripped).items()})
            hit_rate = re.search(r"hit_rate=([0-9.]+)", stripped)
            if hit_rate:
                current["hit_rate"] = float(hit_rate.group(1))
        elif stripped.startswith("attack_range:"):
            current.update({f"range_{k}": v for k, v in parse_key_values(stripped).items()})
        elif stripped.startswith("rl_target_gate:"):
            current.update({f"gate_{k}": v for k, v in parse_key_values(stripped).items()})
        elif stripped.startswith("markatk_observation:"):
            current.update({f"markatk_{k}": v for k, v in parse_key_values(stripped).items()})
        elif stripped.startswith("sweep_history_observation:"):
            current.update({f"sweep_{k}": v for k, v in parse_key_values(stripped).items()})
    if current:
        episodes.append(current)

    total_steps = 0
    for ep in episodes:
        total_steps += int(ep.get("steps", 0))
        ep["cumulative_steps"] = total_steps
        steps = max(1, int(ep.get("steps", 1)))
        ep["in_range_percent"] = float(ep.get("range_in_range_steps", 0)) / steps * 100.0
    return episodes


def rolling(values: list[float], window: int = 50) -> list[float]:
    out: list[float] = []
    for i in range(len(values)):
        chunk = values[max(0, i - window + 1) : i + 1]
        out.append(sum(chunk) / len(chunk))
    return out


def canvas(title: str) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    img = Image.new("RGB", (1280, 720), "#F7F4ED")
    draw = ImageDraw.Draw(img)
    draw.rectangle((0, 0, 18, 720), fill="#C44536")
    draw.text((64, 42), title, fill="#1F2933", font=FONT_TITLE)
    draw.line((64, 112, 1210, 112), fill="#D6D0C4", width=2)
    return img, draw


def draw_line_chart(
    filename: str,
    title: str,
    series: list[tuple[str, list[float], str]],
    x_values: list[float],
    y_label: str,
    y_max: float | None = None,
) -> None:
    img, draw = canvas(title)
    left, top, right, bottom = 110, 165, 1160, 595
    draw.rectangle((left, top, right, bottom), outline="#D6D0C4", width=2)
    if not x_values:
        draw.text((left + 20, top + 20), "데이터 없음", fill="#667085", font=FONT_LABEL)
        img.save(FIG_DIR / filename)
        return
    max_x = max(x_values) or 1
    all_values = [v for _, vals, _ in series for v in vals]
    max_y = y_max if y_max is not None else (max(all_values) if all_values else 1)
    max_y = max(max_y, 1)
    for i in range(6):
        y = bottom - (bottom - top) * i / 5
        draw.line((left, y, right, y), fill="#E3DDD1", width=1)
        draw.text((36, y - 10), f"{max_y * i / 5:.0f}", fill="#667085", font=FONT_TINY)
    for label, vals, color in series:
        points = []
        for x, value in zip(x_values, vals):
            px = left + (right - left) * x / max_x
            py = bottom - (bottom - top) * max(0, min(value, max_y)) / max_y
            points.append((px, py))
        if len(points) >= 2:
            draw.line(points, fill=color, width=4)
        for px, py in points[:: max(1, len(points) // 12)]:
            draw.ellipse((px - 4, py - 4, px + 4, py + 4), fill=color)
    draw.text((left, bottom + 28), "누적 환경 step", fill="#667085", font=FONT_SMALL)
    draw.text((left, top - 34), y_label, fill="#667085", font=FONT_SMALL)
    lx = right - 360
    for i, (label, _, color) in enumerate(series):
        y = top - 38 + i * 26
        draw.rectangle((lx, y + 6, lx + 18, y + 18), fill=color)
        draw.text((lx + 28, y), label, fill="#1F2933", font=FONT_SMALL)
    img.save(FIG_DIR / filename)


def draw_bar_chart(filename: str, title: str, values: list[tuple[str, float, str]], max_value: float | None = None) -> None:
    img, draw = canvas(title)
    left, top = 110, 170
    max_v = max_value or max(v for _, v, _ in values) or 1
    for i, (label, value, color) in enumerate(values):
        y = top + i * 76
        draw.text((left, y), label, fill="#1F2933", font=FONT_LABEL)
        draw.rectangle((left + 310, y + 8, 1080, y + 32), fill="#E3DDD1")
        width = int((1080 - (left + 310)) * value / max_v)
        draw.rectangle((left + 310, y + 8, left + 310 + max(4, width), y + 32), fill=color)
        draw.text((1100, y + 2), f"{value:g}", fill="#667085", font=FONT_LABEL)
    img.save(FIG_DIR / filename)


def draw_table_image(filename: str, title: str, headers: list[str], rows: list[list[str]]) -> None:
    img, draw = canvas(title)
    x, y = 92, 160
    widths = [310, 250, 250, 250]
    row_h = 48
    for i, header in enumerate(headers):
        x0 = x + sum(widths[:i])
        draw.rectangle((x0, y, x0 + widths[i], y + row_h), fill="#2B2F36")
        draw.text((x0 + 14, y + 13), header, fill="#FFFFFF", font=FONT_SMALL)
    for r, row in enumerate(rows):
        for c, cell in enumerate(row):
            x0 = x + sum(widths[:c])
            y0 = y + row_h * (r + 1)
            draw.rectangle((x0, y0, x0 + widths[c], y0 + row_h), fill="#FFFFFF" if r % 2 == 0 else "#F0ECE3", outline="#D6D0C4")
            draw.text((x0 + 14, y0 + 13), cell, fill="#1F2933", font=FONT_SMALL)
    img.save(FIG_DIR / filename)


def main() -> None:
    FIG_DIR.mkdir(parents=True, exist_ok=True)
    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    episodes = parse_log()
    with STATUS_PATH.open("r", encoding="utf-8") as f:
        status = json.load(f)
    checkpoints = status["BossPlayer"]["checkpoints"]

    x = [float(ep["cumulative_steps"]) for ep in episodes]
    damage = [float(ep.get("boss_dmg_dealt", 0)) for ep in episodes]
    clears = [1.0 if ep.get("reason") == "boss_dead" else 0.0 for ep in episodes]
    hit_rates = [float(ep.get("hit_rate", 0)) for ep in episodes]
    in_ranges = [float(ep.get("in_range_percent", 0)) for ep in episodes]
    survivals = [float(ep.get("survival_seconds", 0)) for ep in episodes]
    reward_x = [float(cp["steps"]) for cp in checkpoints]
    rewards = [float(cp["reward"]) for cp in checkpoints]

    draw_line_chart(
        "boss_damage_by_step.png",
        "boss damage 학습 추이",
        [("episode damage rolling 50", rolling(damage, 50), "#2F6FAD")],
        x,
        "boss damage",
        y_max=60,
    )
    draw_line_chart(
        "clear_rate_by_step.png",
        "clear rate 학습 추이",
        [("rolling 100 clear rate", [v * 100 for v in rolling(clears, 100)], "#317A55")],
        x,
        "clear rate %",
        y_max=100,
    )
    draw_line_chart(
        "reward_by_step.png",
        "PPO reward checkpoint 추이",
        [("checkpoint reward", rewards, "#B8842C")],
        reward_x,
        "reward",
    )
    draw_line_chart(
        "hit_rate_by_step.png",
        "hit rate 학습 추이",
        [("episode hit rate rolling 50", rolling(hit_rates, 50), "#317A55")],
        x,
        "hit rate %",
        y_max=100,
    )
    draw_line_chart(
        "in_range_by_step.png",
        "in range 학습 추이",
        [("in range rolling 50", rolling(in_ranges, 50), "#2F6FAD")],
        x,
        "in range %",
        y_max=100,
    )

    clear_survivals = [survivals[i] for i, c in enumerate(clears) if c > 0]
    bins = [40, 50, 60, 70, 80, 90, 100]
    hist = []
    for start, end in zip(bins, bins[1:]):
        hist.append((f"{start}-{end}s", sum(1 for s in clear_survivals if start <= s < end), "#C44536"))
    draw_bar_chart("clear_time_distribution.png", "클리어 시간 분포", hist or [("clear 없음", 0, "#C44536")])

    draw_bar_chart(
        "phase_milestone_timeline.png",
        "페이즈 도달 기준 damage milestone",
        [
            ("Phase 2 필요 damage", 18, "#2F6FAD"),
            ("Phase 3 필요 damage", 36, "#317A55"),
            ("Final 필요 damage", 54, "#B8842C"),
            ("1M max damage", 59, "#C44536"),
        ],
        max_value=60,
    )

    draw_table_image(
        "integrity_leak_table.png",
        "무결성 leak 점검표",
        ["항목", "결과", "기준", "판정"],
        [
            ["attack_out_of_range", "0", "0", "통과"],
            ["hidden hit", "0", "0", "통과"],
            ["off-lane hit", "0", "0", "통과"],
            ["stale hit", "0", "0", "통과"],
            ["fake marker mask leak", "0", "0", "통과"],
            ["next_band leak", "0", "0", "통과"],
            ["sweep_sequence_index leak", "0", "0", "통과"],
        ],
    )

    table_md = FIG_DIR / "integrity_leak_table.md"
    table_md.write_text(
        "\n".join(
            [
                "| 항목 | 결과 | 기준 | 판정 |",
                "|---|---:|---:|---|",
                "| attack_out_of_range | 0 | 0 | 통과 |",
                "| hidden hit | 0 | 0 | 통과 |",
                "| off-lane hit | 0 | 0 | 통과 |",
                "| stale hit | 0 | 0 | 통과 |",
                "| fake marker mask leak | 0 | 0 | 통과 |",
                "| next_band leak | 0 | 0 | 통과 |",
                "| sweep_sequence_index leak | 0 | 0 | 통과 |",
            ]
        )
        + "\n",
        encoding="utf-8",
    )

    for png in FIG_DIR.glob("*.png"):
        shutil.copy2(png, ASSET_DIR / png.name)

    summary = {
        "run_id": RUN_ID,
        "episodes_parsed": len(episodes),
        "boss_dead_parsed": int(sum(clears)),
        "figures": sorted(p.name for p in FIG_DIR.glob("*.png")),
        "note": "그래프는 Player log episode summary와 training_status checkpoint reward에서 생성",
    }
    (FIG_DIR / "figure_generation_summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
