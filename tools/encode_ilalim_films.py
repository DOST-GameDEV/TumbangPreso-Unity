"""Encodes the Ilalim sidewalk films and composes the taho carry comparison.

    py -3 tools/encode_ilalim_films.py            (videos: frames/<event>/f_*.jpg -> <event>.mp4)
    py -3 tools/encode_ilalim_films.py --compare  (stills/taho_carry_compare.png)
    py -3 tools/encode_ilalim_films.py --keep-frames

The frames come from `IlalimSidewalkFilm.RunVideos` (Unity batch, 30 steps a second, 1280x720).
H.264, yuv420p, 30 fps, through imageio-ffmpeg's bundled ffmpeg when none is on PATH. The frame
folders are deleted after a successful encode unless --keep-frames.
"""
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Logs/ilalim-unity/videos_v1"


def ffmpeg():
    exe = shutil.which("ffmpeg")
    if exe:
        return exe
    import imageio_ffmpeg
    return imageio_ffmpeg.get_ffmpeg_exe()


def encode(keep):
    frames = OUT / "frames"
    for folder in sorted(p for p in frames.iterdir() if p.is_dir()):
        count = len(list(folder.glob("f_*.jpg")))
        if count < 2:
            print(f"{folder.name}: {count} frames, skipped")
            continue
        target = OUT / f"{folder.name}.mp4"
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", "30", "-i", str(folder / "f_%05d.jpg"),
                        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-preset", "slow",
                        "-movflags", "+faststart", str(target)], check=True)
        print(f"{target.relative_to(ROOT)}: {count} frames, {count / 30:.1f} s, {target.stat().st_size // 1024} KB")
        if not keep:
            shutil.rmtree(folder)


def compare():
    from PIL import Image, ImageDraw, ImageFont
    folder = OUT / "stills/taho"
    rows = [("waist", "WAIST (the old look)"), ("shoulder", "SHOULDER (the default now)")]
    cols = ["side", "front", "back", "walk_side"]
    tile_w, tile_h, label = 640, 360, 44
    sheet = Image.new("RGB", (tile_w * len(cols), (tile_h + label) * len(rows)), (24, 24, 28))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("arialbd.ttf", 26)
    except OSError:
        font = ImageFont.load_default()
    for r, (key, title) in enumerate(rows):
        y = r * (tile_h + label)
        draw.text((14, y + 8), f"MAGTATAHO CARRY: {title}", fill=(240, 236, 224), font=font)
        for c, view in enumerate(cols):
            path = folder / f"{key}_{view}.png"
            if not path.exists():
                continue
            tile = Image.open(path).convert("RGB").resize((tile_w, tile_h), Image.LANCZOS)
            sheet.paste(tile, (c * tile_w, y + label))
            draw.text((c * tile_w + 10, y + label + 8), view.replace("_", " "), fill=(255, 255, 255), font=font)
    target = OUT / "stills/taho_carry_compare.png"
    sheet.save(target)
    print(f"wrote {target.relative_to(ROOT)}")


if __name__ == "__main__":
    if "--compare" in sys.argv:
        compare()
    else:
        encode("--keep-frames" in sys.argv)
