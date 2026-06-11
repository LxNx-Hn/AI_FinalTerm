"""Cross-platform ffmpeg / font resolution for the video tools."""
import os
import shutil
import sys
from pathlib import Path


def find_ffmpeg() -> str:
    env = os.environ.get("FFMPEG_BIN")
    if env and Path(env).exists():
        return env
    try:
        import imageio_ffmpeg

        return imageio_ffmpeg.get_ffmpeg_exe()
    except Exception:
        pass
    on_path = shutil.which("ffmpeg")
    if on_path:
        return on_path
    sys.exit("ffmpeg not found: install ffmpeg, pip install imageio-ffmpeg, or set FFMPEG_BIN")


def find_mono_font() -> str | None:
    candidates = [
        "C:/Windows/Fonts/consola.ttf",
        "/System/Library/Fonts/Menlo.ttc",
        "/System/Library/Fonts/Monaco.ttf",
        "/Library/Fonts/Andale Mono.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
    ]
    for cand in candidates:
        if Path(cand).exists():
            return cand
    return None
