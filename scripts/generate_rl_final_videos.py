from __future__ import annotations

import json
import math
import re
import shutil
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
VIDEOS_DIR = ROOT / "videos"
FIGURES_DIR = ROOT / "docs" / "rl_final" / "figures"
RUN_DIR = ROOT / "results" / "BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1"
LOG_PATH = RUN_DIR / "run_logs" / "Player-0.TRAINING-1M.log"
STATUS_PATH = RUN_DIR / "run_logs" / "training_status.json"
FFMPEG_PATH_FILE = ROOT / "video_captures" / ".ffmpeg_path"
TMP_DIR = VIDEOS_DIR / "_generated_frames"

W, H = 1280, 720
FPS = 5

BG = "#F7F4ED"
INK = "#1F2933"
MUTED = "#667085"
LINE = "#D6D0C4"
RED = "#C44536"
BLUE = "#2F6FAD"
GREEN = "#317A55"
GOLD = "#B8842C"
PANEL = "#FFFFFF"


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        Path(r"C:\Windows\Fonts\NotoSansKR-VF.ttf"),
        Path(r"C:\Windows\Fonts\malgunbd.ttf" if bold else r"C:\Windows\Fonts\malgun.ttf"),
        Path(r"C:\Windows\Fonts\gulim.ttc"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size=size)
    return ImageFont.load_default()


F_TITLE = font(46, True)
F_HEAD = font(30, True)
F_BODY = font(25)
F_SMALL = font(18)
F_NUM = font(38, True)


def parse_episodes() -> list[dict[str, float | str]]:
    episodes: list[dict[str, float | str]] = []
    current: dict[str, float | str] | None = None
    for line in LOG_PATH.read_text(encoding="utf-8", errors="replace").splitlines():
        if "[BossRL] EPISODE_END" in line:
            match = re.search(r"reason=(\w+) steps=(\d+) survival=([0-9.]+)s", line)
            if match:
                current = {
                    "reason": match.group(1),
                    "steps": float(match.group(2)),
                    "survival": float(match.group(3)),
                }
                episodes.append(current)
            continue
        if current is None:
            continue
        if line.startswith("  boss:"):
            for key in ("hp_left", "dmg_dealt"):
                match = re.search(key + r"=([-0-9.]+)", line)
                if match:
                    current[key] = float(match.group(1))
        elif line.startswith("  attack_quality:"):
            for key in ("actions", "hits", "hit_rate"):
                match = re.search(key + r"=([-0-9.]+)", line)
                if match:
                    current[key] = float(match.group(1))
        elif line.startswith("  attack_range:"):
            match = re.search(r"atk_out_range=([-0-9.]+)", line)
            if match:
                current["atk_out_range"] = float(match.group(1))
        elif line.startswith("  dash_target_alignment:"):
            for key in ("hit_class_hidden_target_count", "hit_class_off_lane_empty_count", "hit_class_stale_bosscell_count"):
                match = re.search(key + r"=([0-9]+)", line)
                if match:
                    current[key] = float(match.group(1))
        elif line.startswith("  markatk_observation:"):
            for key in ("markatk_real_observed_count", "markatk_fake_observed_count", "fake_marker_in_danger_mask_count"):
                match = re.search(key + r"=([0-9]+)", line)
                if match:
                    current[key] = float(match.group(1))
        elif line.startswith("  sweep_history_observation:"):
            for key in ("next_band_direct_observation_count", "sweep_sequence_index_observation_count"):
                match = re.search(key + r"=([0-9]+)", line)
                if match:
                    current[key] = float(match.group(1))
    return episodes


def checkpoint_rewards() -> list[tuple[int, float]]:
    status = json.loads(STATUS_PATH.read_text(encoding="utf-8"))
    checkpoints = status["BossPlayer"]["checkpoints"]
    return [(int(item["steps"]), float(item["reward"])) for item in checkpoints]


def draw_header(draw: ImageDraw.ImageDraw, kicker: str, title: str) -> None:
    draw.text((56, 36), kicker, fill=RED, font=F_SMALL)
    draw.text((56, 64), title, fill=INK, font=F_TITLE)
    draw.rectangle((56, 128, 1224, 130), fill=LINE)


def draw_bullets(draw: ImageDraw.ImageDraw, items: list[str], x: int, y: int, color: str = BLUE) -> None:
    for index, item in enumerate(items):
        yy = y + index * 42
        draw.rectangle((x, yy + 12, x + 10, yy + 22), fill=color)
        draw.text((x + 24, yy), item, fill=INK, font=F_BODY)


def draw_metric(draw: ImageDraw.ImageDraw, label: str, value: str, x: int, y: int, accent: str) -> None:
    draw.rounded_rectangle((x, y, x + 230, y + 104), radius=8, fill=PANEL, outline=LINE, width=2)
    draw.text((x + 18, y + 14), value, fill=accent, font=F_NUM)
    draw.text((x + 18, y + 64), label, fill=MUTED, font=F_SMALL)


def draw_bar(draw: ImageDraw.ImageDraw, label: str, value: float, max_value: float, x: int, y: int, accent: str) -> None:
    draw.text((x, y), label, fill=INK, font=F_SMALL)
    bx, by, bw, bh = x + 180, y + 8, 250, 14
    draw.rectangle((bx, by, bx + bw, by + bh), fill="#E3DDD1")
    fill_w = max(4, int(bw * value / max_value)) if max_value else 4
    draw.rectangle((bx, by, bx + fill_w, by + bh), fill=accent)
    draw.text((bx + bw + 16, y - 2), f"{value:g}", fill=MUTED, font=F_SMALL)


def paste_figure(canvas: Image.Image, figure_name: str, box: tuple[int, int, int, int]) -> None:
    src = FIGURES_DIR / figure_name
    if not src.exists():
        return
    image = Image.open(src).convert("RGB")
    image.thumbnail((box[2] - box[0], box[3] - box[1]))
    x = box[0] + ((box[2] - box[0]) - image.width) // 2
    y = box[1] + ((box[3] - box[1]) - image.height) // 2
    canvas.paste(image, (x, y))


def frame_base(kicker: str, title: str) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    canvas = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(canvas)
    draw_header(draw, kicker, title)
    draw.text((56, 666), "CODE BLUE PPO 1M", fill=MUTED, font=F_SMALL)
    return canvas, draw


def repeat(frames: list[Image.Image], frame: Image.Image, seconds: int) -> None:
    for _ in range(seconds * FPS):
        frames.append(frame.copy())


def build_initial_policy_video(episodes: list[dict[str, float | str]]) -> list[Image.Image]:
    frames: list[Image.Image] = []
    first = episodes[:3]

    canvas, draw = frame_base("영상 1 초기 정책", "학습 초반 정책 분석")
    draw_bullets(
        draw,
        [
            "최종 정책 영상 재라벨링 없음",
            "1M run 초반 episode 로그 기반 분석",
            "피격 및 사망 중심 종료",
            "낮은 boss damage와 낮은 hit_rate",
        ],
        76,
        178,
        RED,
    )
    repeat(frames, canvas, 4)

    canvas, draw = frame_base("영상 1 초기 정책", "초반 3 episode 수치")
    labels = ["E1", "E2", "E3"]
    for i, ep in enumerate(first):
        x = 86 + i * 390
        draw_metric(draw, f"{labels[i]} boss damage", f"{int(ep.get('dmg_dealt', 0))}", x, 176, RED)
        draw_metric(draw, f"{labels[i]} hit_rate", f"{ep.get('hit_rate', 0):.1f}%", x, 306, BLUE)
        draw_metric(draw, f"{labels[i]} 종료", "사망", x, 436, GOLD)
    repeat(frames, canvas, 5)

    canvas, draw = frame_base("영상 1 초기 정책", "공격권 회복 실패 지표")
    for i, ep in enumerate(first):
        y = 190 + i * 90
        draw_bar(draw, f"Episode {i + 1} boss damage", float(ep.get("dmg_dealt", 0)), 60, 84, y, RED)
        draw_bar(draw, f"Episode {i + 1} hit_rate", float(ep.get("hit_rate", 0)), 100, 84, y + 34, BLUE)
    draw_bullets(draw, ["attack_out_of_range 0", "hidden/off-lane/stale hit 0", "성공 근거가 아닌 실패 대비 자료"], 660, 210, GREEN)
    repeat(frames, canvas, 5)

    canvas, draw = frame_base("영상 1 초기 정책", "요약")
    draw_bullets(
        draw,
        [
            "초반 정책은 회피 후 공격권 회복이 불안정",
            "첫 3 episode boss damage 5, 3, 4",
            "첫 3 episode 모두 player_dead",
            "PPO-only 실패 결론이 아닌 단기 학습 한계 자료",
        ],
        98,
        196,
        RED,
    )
    repeat(frames, canvas, 4)
    return frames


def build_progression_video(episodes: list[dict[str, float | str]]) -> list[Image.Image]:
    frames: list[Image.Image] = []
    rewards = checkpoint_rewards()

    canvas, draw = frame_base("영상 2 학습 진행", "checkpoint별 정책 변화")
    draw_bullets(
        draw,
        [
            "100K에서 1M까지 reward 상승",
            "최근 100 episode 클리어율 80퍼센트",
            "최근 100 episode hit_rate 97.3퍼센트",
            "최근 100 episode in_range 27.5퍼센트",
        ],
        78,
        184,
        BLUE,
    )
    repeat(frames, canvas, 4)

    canvas, draw = frame_base("영상 2 학습 진행", "checkpoint reward 추이")
    min_reward = min(value for _, value in rewards)
    max_reward = max(value for _, value in rewards)
    chart = (96, 182, 1180, 560)
    draw.rectangle(chart, fill=PANEL, outline=LINE, width=2)
    points = []
    for step, value in rewards:
        x = chart[0] + int((step - rewards[0][0]) / (rewards[-1][0] - rewards[0][0]) * (chart[2] - chart[0] - 70)) + 34
        y = chart[3] - int((value - min_reward) / (max_reward - min_reward) * (chart[3] - chart[1] - 70)) - 34
        points.append((x, y, step, value))
    for a, b in zip(points, points[1:]):
        draw.line((a[0], a[1], b[0], b[1]), fill=BLUE, width=5)
    for x, y, step, value in points:
        draw.ellipse((x - 7, y - 7, x + 7, y + 7), fill=RED)
        if step in (99957, 499996, 799922, 1000001):
            draw.text((x - 32, y - 34), f"{step//1000}K", fill=INK, font=F_SMALL)
    draw.text((118, 586), f"reward {min_reward:.1f}에서 {max_reward:.1f}로 상승", fill=MUTED, font=F_SMALL)
    repeat(frames, canvas, 6)

    for figure, title in [
        ("boss_damage_by_step.png", "boss damage 증가"),
        ("clear_rate_by_step.png", "clear rate 상승"),
        ("hit_rate_by_step.png", "hit_rate 안정화"),
        ("in_range_by_step.png", "in_range 증가"),
    ]:
        canvas, draw = frame_base("영상 2 학습 진행", title)
        paste_figure(canvas, figure, (96, 152, 1184, 630))
        repeat(frames, canvas, 3)

    canvas, draw = frame_base("영상 2 학습 진행", "요약")
    draw_bullets(
        draw,
        [
            "50K 단기 학습에서는 클리어 0",
            "1M long-run에서 boss_dead 199회",
            "전체 클리어율 22.3퍼센트",
            "최근 100 episode 클리어율 80퍼센트",
        ],
        98,
        196,
        GREEN,
    )
    repeat(frames, canvas, 4)
    return frames


def encode_video(frames: list[Image.Image], output: Path, prefix: str) -> None:
    frames_dir = TMP_DIR / prefix
    if frames_dir.exists():
        shutil.rmtree(frames_dir)
    frames_dir.mkdir(parents=True, exist_ok=True)
    for index, frame in enumerate(frames):
        frame.save(frames_dir / f"{prefix}_{index:04d}.png")

    ffmpeg = FFMPEG_PATH_FILE.read_text(encoding="utf-8").strip() if FFMPEG_PATH_FILE.exists() else "ffmpeg"
    cmd = [
        ffmpeg,
        "-y",
        "-framerate",
        str(FPS),
        "-i",
        str(frames_dir / f"{prefix}_%04d.png"),
        "-c:v",
        "libx264",
        "-pix_fmt",
        "yuv420p",
        "-movflags",
        "+faststart",
        str(output),
    ]
    subprocess.run(cmd, check=True)


def main() -> None:
    VIDEOS_DIR.mkdir(parents=True, exist_ok=True)
    episodes = parse_episodes()
    encode_video(build_initial_policy_video(episodes), VIDEOS_DIR / "01_initial_policy.mp4", "initial")
    encode_video(build_progression_video(episodes), VIDEOS_DIR / "02_learning_progression.mp4", "progress")
    if TMP_DIR.exists():
        shutil.rmtree(TMP_DIR)
    print(json.dumps({"videos": ["01_initial_policy.mp4", "02_learning_progression.mp4"]}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
