"""Shot 48 of the gameplay trailer: the logo over the nine-hero key art (docs/TRAILER_PROMPTS.md, "Shot 48").

  python tools/render_trailer_logo.py Logs/trailer/48_key-art "C:/Users/<you>/Desktop/TUMP Trailer Shots"

Takes the plate `TrailerShots.Shot48KeyArt` records (<folder>/clean/frame_*.png: 139 frames and 12 frames of handles
either side) and lays over it, frame for frame:
  * the owner's logo, Assets/TumbangPreso/Art/ui/brand/tump_logo.png (the newest owner version: the transparent vector
    export of 2026-10-04 the home screen draws), never redrawn: on screen from frame 0 at 110%, settling to 100% by
    frame 10;
  * a white frame under it on frame 0 that fades to the plate by frame 8 (out of shot 47's white);
  * the call to action, COMING SOON for now, in the trailer cards' look (tools/render_trailer_cards.py), fading in
    under the logo over frames 42 to 54.
Writes shot-48.mov (all of it), shot-48-plate.mov (the plate alone), shot-48-cta.mov (the call to action alone, ProRes
4444 with alpha) and shot-48-still.png (frame 60). No stinger: the optional can tip is not in this plate.
"""
import argparse
import importlib.util
import shutil
import subprocess
import tempfile
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
LOGO = ROOT / "Assets" / "TumbangPreso" / "Art" / "ui" / "brand" / "tump_logo.png"
W, H, FPS, HANDLES = 1920, 1080, 60, 12
LOGO_WIDTH = 0.30          # of the frame, at 100%
LOGO_CENTRE = (0.5, 0.205) # high, over the bunting: the nine heads sit at about half height
WHITE_GONE, LOGO_SETTLED, CTA_FROM, CTA_FADE = 8, 10, 42, 12


def ffmpeg():
    exe = shutil.which("ffmpeg")
    if not exe:
        raise SystemExit("ffmpeg is not on PATH")
    return exe


def smooth(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3.0 - 2.0 * t)


def cards():
    spec = importlib.util.spec_from_file_location("cards", ROOT / "tools" / "render_trailer_cards.py")
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def cta_layer(below_y):
    """COMING SOON in the cards' face, outline and shadow, centred under the logo."""
    from PIL import ImageDraw
    m = cards()
    cap = 0.045 * H
    font = m.font_for_cap(cap)
    stroke = max(4, round(cap * 0.085))
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    text = "COMING SOON"
    box = d.textbbox((0, 0), text, font=font, stroke_width=stroke)
    x = (W - (box[2] - box[0])) / 2
    y = below_y - font.getbbox("H")[1]
    d.text((x, y + 4), text, font=font, fill=m.INK, stroke_width=stroke, stroke_fill=m.INK)
    d.text((x, y), text, font=font, fill=m.CHALK, stroke_width=stroke, stroke_fill=m.INK)
    return layer


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("folder")
    parser.add_argument("out")
    args = parser.parse_args()
    plate = Path(args.folder) / "clean"
    frames = sorted(plate.glob("frame_*.png"))
    if not frames:
        raise SystemExit(f"no plate frames in {plate}")
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    logo = Image.open(LOGO).convert("RGBA")
    base_w = W * LOGO_WIDTH
    base_h = base_w * logo.height / logo.width
    cta = cta_layer(H * LOGO_CENTRE[1] + base_h / 2 + H * 0.03)
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        (tmp / "all").mkdir(); (tmp / "cta").mkdir()
        for i, path in enumerate(frames):
            f = i - HANDLES
            picture = Image.open(path).convert("RGBA")
            # The white out of shot 47, gone by frame 8.
            white = 1.0 if f <= 0 else 1.0 - smooth(f / WHITE_GONE)
            if white > 0:
                picture = Image.blend(picture, Image.new("RGBA", (W, H), (255, 255, 255, 255)), white)
            scale = 1.10 - 0.10 * (1.0 - (1.0 - min(1.0, max(0.0, f / LOGO_SETTLED))) ** 3)
            lw, lh = round(base_w * scale), round(base_h * scale)
            mark = logo.resize((lw, lh), Image.LANCZOS)
            picture.alpha_composite(mark, (round(W * LOGO_CENTRE[0] - lw / 2), round(H * LOGO_CENTRE[1] - lh / 2)))
            level = smooth((f - CTA_FROM) / CTA_FADE)
            words = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            if level > 0:
                a = np.array(cta)
                a[:, :, 3] = (a[:, :, 3].astype(np.float32) * level).round().astype(np.uint8)
                words = Image.fromarray(a, "RGBA")
                picture.alpha_composite(words)
            picture.convert("RGB").save(tmp / "all" / f"f_{i:04d}.png")
            words.save(tmp / "cta" / f"f_{i:04d}.png")
            if f == 60:
                picture.convert("RGB").save(out / "shot-48-still.png")
        prores = ["-c:v", "prores_ks", "-profile:v", "3", "-pix_fmt", "yuv422p10le", "-vendor", "apl0", "-r", str(FPS), "-an"]
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(tmp / "all" / "f_%04d.png")] + prores + [str(out / "shot-48.mov")], check=True)
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(plate / "frame_%05d.png")] + prores + [str(out / "shot-48-plate.mov")], check=True)
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(tmp / "cta" / "f_%04d.png"),
                        "-c:v", "prores_ks", "-profile:v", "4", "-pix_fmt", "yuva444p10le", "-alpha_bits", "16", "-vendor", "apl0", "-r", str(FPS), str(out / "shot-48-cta.mov")], check=True)
    print(f"wrote shot-48.mov, shot-48-plate.mov, shot-48-cta.mov and shot-48-still.png in {out} ({len(frames)} frames)")


if __name__ == "__main__":
    main()
