"""Shot 1 of the gameplay trailer: the studio badge on black (docs/TRAILER_PROMPTS.md, "Shot 1").

  python tools/render_trailer_badge.py "C:/Users/<you>/Desktop/TUMP Trailer Shots"

The badge is the studio intro's own (SplashScreen.StudioIntro plays Assets/TumbangPreso/Art/video/opening_animation.mp4,
a black "BH studios" mark animating onto white, fitted to the screen). Its settled frame is keyed by luminance (the mark
is the dark part) and drawn in white on pure black, since a black mark on black would not be there at all: centred, at
60% of the size the menu shows it (the clip fitted to 1920x1080). It fades in over frames 0 to 12, holds, fades out
and is gone by frame 53, leaving 6 frames of black before the smash cut on the 0.98 drum hit. 59 frames, with 12
frames of black handles either side; ProRes 422 HQ, no sound. Writes shot-01.mov and shot-01-still.png.
"""
import argparse
import shutil
import subprocess
import tempfile
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
CLIP = ROOT / "Assets" / "TumbangPreso" / "Art" / "video" / "opening_animation.mp4"
W, H, FPS = 1920, 1080, 60
FRAMES, HANDLES = 59, 12
FADE_IN, OUT_FROM, GONE = 12, 45, 53
SCALE = 0.60
SETTLED_AT = 2.0   # seconds into the clip: the mark has landed and holds (the clip is 3.07 s)


def ffmpeg():
    exe = shutil.which("ffmpeg")
    if not exe:
        raise SystemExit("ffmpeg is not on PATH")
    return exe


def smooth(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3.0 - 2.0 * t)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("out")
    args = parser.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        settled = tmp / "settled.png"
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-ss", str(SETTLED_AT), "-i", str(CLIP), "-frames:v", "1", str(settled)], check=True)
        src = Image.open(settled).convert("L")
        # As the menu shows it: the clip fitted to the frame (FitInParent), then this shot's 60%.
        fit = min(W / src.width, H / src.height) * SCALE
        size = (round(src.width * fit), round(src.height * fit))
        lum = np.asarray(src.resize(size, Image.LANCZOS), dtype=np.float32) / 255.0
        # The mark is the dark part of a white card: its coverage is how far below white a pixel is.
        mark = np.clip((1.0 - lum - 0.04) / 0.92, 0.0, 1.0)
        canvas = np.zeros((H, W), dtype=np.float32)
        ox, oy = (W - size[0]) // 2, (H - size[1]) // 2
        canvas[oy:oy + size[1], ox:ox + size[0]] = mark
        seq = tmp / "seq"
        seq.mkdir()
        for i in range(FRAMES + 2 * HANDLES):
            f = i - HANDLES
            level = 0.0 if f < 0 or f >= GONE else smooth((f + 1) / FADE_IN) * (1.0 - smooth((f - OUT_FROM) / (GONE - 1 - OUT_FROM)))
            grey = (canvas * level * 255.0).round().astype(np.uint8)
            Image.fromarray(grey, "L").convert("RGB").save(seq / f"f_{i:04d}.png")
            if f == 30:
                Image.fromarray(grey, "L").convert("RGB").save(out / "shot-01-still.png")
        mov = out / "shot-01.mov"
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(seq / "f_%04d.png"),
                        "-c:v", "prores_ks", "-profile:v", "3", "-pix_fmt", "yuv422p10le", "-vendor", "apl0", "-r", str(FPS), "-an", str(mov)], check=True)
    print(f"wrote {mov}: {FRAMES} frames + {HANDLES} of black either side; in over 0 to {FADE_IN}, gone by {GONE}, black {GONE} to {FRAMES - 1}")


if __name__ == "__main__":
    main()
