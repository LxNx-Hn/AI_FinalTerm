"""Cut one episode out of a raw capture, no retiming, uniform output size.

Usage:
  python cut_episode.py <capture_meta.json> <episode_number> <out.mp4>
      [--calib SECONDS]   visual calibration offset added on top of
                          game_offset_seconds (engine-init / recorder-init delta;
                          determine once per capture with montage.py)
      [--pre SECONDS]     start this much before episode scene start (default 0.0)
      [--post SECONDS]    keep this much after the episode-end moment
                          (default: min(0.8, 60% of observed reload gap))
      [--crop W:H:X:Y]    crop raw capture to the game window region first
                          (needed when the capture is a full desktop larger
                          than the game window, e.g. Retina displays)
      [--scale WxH]       final output size (default 1920x1080)
      [--frames]          also write start/mid/end verification frames next to out

The episode window comes from THIS capture run's own Player log:
  wall_start = previous episode's after_scene_loaded (fresh scene visible)
  wall_end   = this episode's queue_reload (death / boss-kill moment)
NEVER take episode numbers from a different run's log.

Required meta json fields (written by the capture script):
  raw_video, player_log, game_offset_seconds, capture_name
"""
import argparse
import json
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from ffmpeg_util import find_ffmpeg
from parse_episodes import parse_log


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("meta", type=Path)
    ap.add_argument("episode", type=int)
    ap.add_argument("out", type=Path)
    ap.add_argument("--calib", type=float, default=0.0)
    ap.add_argument("--pre", type=float, default=0.0)
    ap.add_argument("--post", type=float, default=None)
    ap.add_argument("--crop", type=str, default=None)
    ap.add_argument("--scale", type=str, default="1920x1080")
    ap.add_argument("--gamma", type=float, default=1.0,
                    help="luma gamma correction (>1 brightens); 1.0 = off")
    ap.add_argument("--frames", action="store_true")
    args = ap.parse_args()

    ffmpeg = find_ffmpeg()
    meta = json.loads(args.meta.read_text(encoding="utf-8-sig"))
    rows = parse_log(Path(meta["player_log"]))
    row = next(r for r in rows if r["episode"] == args.episode)
    offset = float(meta["game_offset_seconds"]) + args.calib

    post = args.post
    if post is None:
        nxt = next((r for r in rows if r["episode"] == args.episode + 1), None)
        gap = (nxt["wall_start"] - row["wall_end"]) if nxt and nxt["wall_start"] else 1.0
        post = max(0.3, min(0.8, 0.6 * gap))

    start = max(0.0, offset + row["wall_start"] - args.pre)
    duration = (row["wall_end"] - row["wall_start"]) + args.pre + post

    filters = []
    if args.crop:
        filters.append(f"crop={args.crop}")
    width, height = args.scale.lower().split("x")
    filters.append(f"scale={width}:{height}:flags=lanczos")
    if args.gamma != 1.0:
        filters.append(f"eq=gamma={args.gamma:.3f}")
    filters.append("fps=60")
    filters.append("format=yuv420p")

    args.out.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        ffmpeg, "-y", "-hide_banner", "-loglevel", "error",
        "-ss", f"{start:.3f}", "-t", f"{duration:.3f}",
        "-i", meta["raw_video"],
        "-vf", ",".join(filters),
        "-c:v", "libx264", "-preset", "medium", "-crf", "18",
        "-pix_fmt", "yuv420p",
        "-color_range", "pc", "-colorspace", "bt709",
        "-color_primaries", "bt709", "-color_trc", "bt709",
        "-an",
        str(args.out),
    ]
    subprocess.run(cmd, check=True)

    if args.frames:
        for tag, t in (("start", 0.04), ("mid", duration / 2), ("end", duration - 0.1)):
            frame_out = args.out.with_name(f"{args.out.stem}_{tag}.png")
            subprocess.run(
                [
                    ffmpeg, "-y", "-hide_banner", "-loglevel", "error",
                    "-ss", f"{t:.3f}", "-i", str(args.out),
                    "-frames:v", "1", "-vf", "scale=960:-1",
                    str(frame_out),
                ],
                check=True,
            )

    info = {
        "out": str(args.out),
        "capture": meta["capture_name"],
        "episode": args.episode,
        "reason": row["reason"],
        "survival_s": row["survival_s"],
        "boss_damage": row["boss_damage"],
        "sweep_steps": row["sweep_steps"],
        "mark_real": row["mark_real"],
        "mark_fake": row["mark_fake"],
        "escape": f"{row['escape_success']}/{row['escape_fail']}",
        "player_hits": row["player_hits"],
        "source_start": round(start, 3),
        "source_duration": round(duration, 3),
        "calib": args.calib,
    }
    print(json.dumps(info, indent=2))


if __name__ == "__main__":
    main()
