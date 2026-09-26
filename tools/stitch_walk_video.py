"""Stitches `WalkArmsProbe`'s per-body walk frames into one grid clip per mode.

    python tools/stitch_walk_video.py Logs/cloud9/walk-arms/walk-video Logs/walk-share/walk_v3 --loops 3

Each body's folder (`HeroStrike-sean/000.jpg` ...) is two seconds at 30 frames a second, front and side side by side.
Every body becomes one labelled cell, looped `--loops` times so the rhythm can be watched, and each mode becomes one
mp4 (`<out>_HeroStrike.mp4`, `<out>_Classic.mp4`). Needs ffmpeg on PATH.
"""
import argparse
import math
import os
import shutil
import subprocess


def font():
    for path in ("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                 "C:/Windows/Fonts/arialbd.ttf", "/System/Library/Fonts/Supplemental/Arial Bold.ttf"):
        if os.path.exists(path):
            return path
    return None


def stitch(folders, out, loops, cell_w, cell_h):
    cols = min(3, len(folders))
    rows = math.ceil(len(folders) / cols)
    inputs, filters, labels = [], [], []
    face = font()
    for i, (name, path) in enumerate(folders):
        inputs += ["-stream_loop", str(loops - 1), "-framerate", "30", "-i", os.path.join(path, "%03d.jpg")]
        text = ""
        if face:
            text = (f",drawtext=fontfile='{face}':text='{name}':x=10:y=10:fontsize=22:fontcolor=white"
                    f":box=1:boxcolor=0x1c0f06@0.7:boxborderw=6")
        filters.append(f"[{i}:v]scale={cell_w}:{cell_h}{text}[c{i}]")
        labels.append(f"[c{i}]")
    # Pad the grid with black cells so xstack has a full rectangle.
    for i in range(len(folders), cols * rows):
        filters.append(f"color=c=black:s={cell_w}x{cell_h}:r=30[c{i}]")
        labels.append(f"[c{i}]")
    layout = "|".join(f"{(i % cols) * cell_w}_{(i // cols) * cell_h}" for i in range(cols * rows))
    filters.append(f"{''.join(labels)}xstack=inputs={cols * rows}:layout={layout}:shortest=1[v]")
    cmd = ["ffmpeg", "-loglevel", "error", "-y", *inputs, "-filter_complex", ";".join(filters),
           "-map", "[v]", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "22", out]
    subprocess.run(cmd, check=True)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("frames")
    ap.add_argument("out")
    ap.add_argument("--loops", type=int, default=3)
    ap.add_argument("--cell", default="640x320")
    args = ap.parse_args()
    if shutil.which("ffmpeg") is None:
        raise SystemExit("ffmpeg is not on PATH")
    cell_w, cell_h = (int(v) for v in args.cell.split("x"))
    groups = {}
    for name in sorted(os.listdir(args.frames)):
        path = os.path.join(args.frames, name)
        if not os.path.isdir(path) or "-" not in name:
            continue
        mode, body = name.split("-", 1)
        groups.setdefault(mode, []).append((body.upper().replace("_", " "), path))
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    for mode, folders in groups.items():
        print(stitch(folders, f"{args.out}_{mode}.mp4", args.loops, cell_w, cell_h))


if __name__ == "__main__":
    main()
