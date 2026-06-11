"""Extract a tiled frame montage from a capture for visual boundary checks.

Usage: python montage.py <video> <start_s> <duration_s> <out.png> [--step 0.25] [--cols 4]
Each tile is stamped with its absolute video timestamp (needs a monospace font;
falls back to no timestamp overlay if none is found).
"""
import argparse
import math
import subprocess
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from ffmpeg_util import find_ffmpeg, find_mono_font


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("video", type=Path)
    ap.add_argument("start", type=float)
    ap.add_argument("duration", type=float)
    ap.add_argument("out", type=Path)
    ap.add_argument("--step", type=float, default=0.25)
    ap.add_argument("--cols", type=int, default=4)
    args = ap.parse_args()

    frames = max(1, int(round(args.duration / args.step)))
    rows = math.ceil(frames / args.cols)
    font = find_mono_font()
    draw = ""
    if font:
        fontfile = font.replace("\\", "/").replace(":", "\\:")
        draw = (
            f"drawtext=text='%{{pts\\:hms\\:{args.start:.3f}}}':x=8:y=8:fontsize=28:"
            f"fontcolor=yellow:box=1:boxcolor=black@0.6:fontfile='{fontfile}',"
        )
    vf = f"fps=1/{args.step},scale=470:-1,{draw}tile={args.cols}x{rows}"
    cmd = [
        find_ffmpeg(), "-y", "-hide_banner", "-loglevel", "error",
        "-ss", f"{args.start:.3f}", "-t", f"{args.duration:.3f}",
        "-i", str(args.video),
        "-vf", vf,
        "-frames:v", "1",
        str(args.out),
    ]
    subprocess.run(cmd, check=True)
    print(args.out)


if __name__ == "__main__":
    main()
