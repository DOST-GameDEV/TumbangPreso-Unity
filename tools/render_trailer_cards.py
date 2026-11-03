"""Render the gameplay trailer's text cards as alpha overlays (docs/TRAILER_PROMPTS.md, "Text cards").

  python tools/render_trailer_cards.py "C:/Users/<you>/Desktop/TUMP Trailer Shots/cards"
  python tools/render_trailer_cards.py OUT --only 11,20     # just these shots

One ProRes 4444 file with alpha per card, NN-<slug>.mov, 1920x1080 at 60 fps, as long as its shot, plus
contact-sheet.png and a 3 s contact-sheet.mov of every card's held frame on mid grey.

The look: Darumadrop One (the game's display face, Assets/TumbangPreso/Resources/UI/fonts), chalk white #F3F0E7, a
thick black outline (UiTheme.InGameOutline is black) and a 4 px hard black drop shadow straight down. No blue, no
gradient, no glow. Sizes are cap heights measured on the face itself: CENTRE 12% of the frame, HERO 10% with the
skill at 45%, MAP 6%.

The motion, locked to the beat: in on its first frame at 135% and 20% opacity, settling to 100% by frame 13 with one
small overshoot (an ease-out-back), then held; no out. Cards on a later sync point (9 on frame 45, 20 on frame 68) are
empty until then. HERO cards draw a short underline in the hero's UiTheme accent that wipes in from the left over
frames 4 to 12.

Names are the kits' displayed names (Assets/TumbangPreso/Runtime/Abilities/*HeroKit.cs), checked 2026-11-03.
Needs Pillow, numpy and ffmpeg. Deterministic.
"""
import argparse
import math
import shutil
import subprocess
import tempfile
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONT = ROOT / "Assets" / "TumbangPreso" / "Resources" / "UI" / "fonts" / "DarumadropOne-Regular.ttf"
W, H, FPS = 1920, 1080, 60
CHALK = (0xF3, 0xF0, 0xE7, 255)
INK = (0, 0, 0, 255)
SETTLE, OVERSHOOT = 13, 1.25   # frames to land; ease-out-back strength (about a 4% dip under 100%)

# UiTheme's hero accents (Assets/TumbangPreso/Runtime/UI/UiTheme.cs, "HERO ACCENTS").
ACCENT = {
    "DANTE": "3fa65c",     # HeroEarth
    "YASMIN": "5fe8d0",    # HeroIce
    "ILYAS": "6065e6",     # HeroCurrent (deep-water indigo)
    "RAGO": "c9a000",      # HeroMolten
    "ZACK": "e8f53a",      # HeroElectric
    "NEMU": "b44dff",      # HeroSpirit
    "PHAISTER": "e828c5",  # HeroWitch
    "AMIHAN": "88e35a",    # HeroWind
    "PAETE": "4f6b1f",     # HeroGrove
}

# (shot, slug, layout, lines, length in frames, the frame the card enters on)
CARDS = [
    (6, "throw", "CENTRE", ["THROW"], 90, 0),
    (7, "knock-it-down", "CENTRE", ["KNOCK IT DOWN"], 90, 0),
    (8, "run-back-in", "CENTRE", ["RUN BACK IN"], 90, 0),
    (9, "dont-get-caught", "CENTRE", ["DON'T GET CAUGHT"], 65, 45),
    (11, "dante-boulder", "HERO", ["DANTE", "BOULDER"], 90, 0),
    (12, "yasmin-glacial-wall", "HERO", ["YASMIN", "GLACIAL WALL"], 90, 0),
    (13, "ilyas-water-wall", "HERO", ["ILYAS", "WATER WALL"], 90, 0),
    (14, "rago-magma-fist", "HERO", ["RAGO", "MAGMA FIST"], 90, 0),
    (15, "zack-backtrack", "HERO", ["ZACK", "BACKTRACK"], 90, 0),
    (16, "nemu-fetcher", "HERO", ["NEMU", "FETCHER"], 90, 0),
    (17, "phaister-teleport", "HERO", ["PHAISTER", "TELEPORT"], 90, 0),
    (18, "amihan-featherfall", "HERO", ["AMIHAN", "FEATHERFALL"], 90, 0),
    (19, "paete-liana-leap", "HERO", ["PAETE", "LIANA LEAP"], 90, 0),
    (20, "9-heroes", "CENTRE", ["9 HEROES"], 90, 68),
    (21, "tondo", "MAP", ["TONDO"], 90, 0),
    (22, "ermita", "MAP", ["ERMITA"], 90, 0),
    (23, "sitangkai", "MAP", ["SITANGKAI"], 90, 0),
    (24, "banaue", "MAP", ["BANAUE"], 90, 0),
    (25, "bgc", "MAP", ["BGC"], 90, 0),
    (26, "6-streets", "CENTRE", ["6 STREETS"], 90, 0),
    (27, "4-players", "CENTRE", ["4 PLAYERS"], 90, 0),
    (28, "1-taya", "CENTRE", ["1 TAYA"], 90, 0),
    (35, "mayon-awakens", "CENTRE", ["MAYON AWAKENS"], 90, 0),
    (36, "absolute-zero", "CENTRE", ["ABSOLUTE ZERO"], 90, 0),
    (37, "airburst", "CENTRE", ["AIRBURST"], 90, 0),
    (38, "makilings-embrace", "CENTRE", ["MAKILING'S EMBRACE"], 90, 0),
    (39, "overclock", "CENTRE", ["OVERCLOCK"], 45, 0),
    (40, "let-them-in", "CENTRE", ["LET THEM IN"], 45, 0),
    (41, "voodoo-doll", "CENTRE", ["VOODOO DOLL"], 45, 0),
    (42, "continental-drift", "CENTRE", ["CONTINENTAL DRIFT"], 45, 0),
    (43, "baha", "CENTRE", ["BAHA"], 45, 0),
]


def ffmpeg():
    exe = shutil.which("ffmpeg")
    if not exe:
        raise SystemExit("ffmpeg is not on PATH")
    return exe


def font_for_cap(cap_px):
    """The face at the size whose capital H is `cap_px` tall."""
    probe = ImageFont.truetype(str(FONT), 200)
    box = probe.getbbox("H")
    return ImageFont.truetype(str(FONT), max(8, round(200 * cap_px / (box[3] - box[1]))))


def text_layer(lines, layout, accent):
    """The card at 100%, on a transparent full frame, and the point it scales about (its block centre)."""
    cap = {"CENTRE": 0.12, "HERO": 0.10, "MAP": 0.06}[layout] * H
    big = font_for_cap(cap)
    small = font_for_cap(cap * 0.45)
    stroke = max(4, round(cap * 0.085))
    shadow = 4
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)

    def width(text, font, s):
        b = draw.textbbox((0, 0), text, font=font, stroke_width=s)
        return b[2] - b[0]

    if layout == "CENTRE":
        # One line where it fits in 86% of the width, else two, broken at the space nearest the middle.
        rows = lines
        if len(rows) == 1 and width(rows[0], big, stroke) > W * 0.86 and " " in rows[0]:
            words = rows[0].split(" ")
            best = min(range(1, len(words)), key=lambda k: abs(len(" ".join(words[:k])) - len(" ".join(words[k:]))))
            rows = [" ".join(words[:best]), " ".join(words[best:])]
        gap = cap * 0.42
        total = len(rows) * cap + (len(rows) - 1) * gap
        top = (H - total) / 2
        placed = [(row, big, (W - width(row, big, stroke)) / 2, top + i * (cap + gap)) for i, row in enumerate(rows)]
        sizes = [(big, stroke)] * len(placed)
    elif layout == "HERO":
        small_cap = cap * 0.45
        left, bottom = W * 0.065, H * 0.80
        name_top = bottom - small_cap - cap * 0.55 - cap
        placed = [(lines[0], big, left, name_top), (lines[1], small, left + cap * 0.04, name_top + cap + cap * 0.55)]
        sizes = [(big, stroke), (small, max(3, round(stroke * 0.6)))]
    else:  # MAP
        left, bottom = W * 0.065, H * 0.86
        placed = [(lines[0], big, left, bottom - cap)]
        sizes = [(big, stroke)]

    # Each line's top sits at its cap line: offset by where the face puts the top of a capital.
    def cap_top(font):
        return font.getbbox("H")[1]

    for (row, font, x, y), (f, s) in zip(placed, sizes):
        draw.text((x, y - cap_top(font) + shadow), row, font=font, fill=INK, stroke_width=s, stroke_fill=INK)
    for (row, font, x, y), (f, s) in zip(placed, sizes):
        draw.text((x, y - cap_top(font)), row, font=font, fill=CHALK, stroke_width=s, stroke_fill=INK)

    underline = None
    if layout == "HERO":
        _, font, x, y = placed[1]
        skill_h = cap * 0.45
        underline = (x, y + skill_h + cap * 0.22, max(cap * 1.6, width(placed[0][0], big, stroke) * 0.55), max(8, round(cap * 0.11)), accent)
    alpha = np.array(layer)[:, :, 3]
    ys, xs = np.nonzero(alpha)
    centre = ((xs.min() + xs.max()) / 2, (ys.min() + ys.max()) / 2)
    return layer, centre, underline


def back_out(p):
    """Ease-out-back: 0 to 1 with one dip past 1 before it settles."""
    p = min(max(p, 0.0), 1.0) - 1.0
    return 1.0 + (OVERSHOOT + 1.0) * p ** 3 + OVERSHOOT * p ** 2


def frame(layer, centre, underline, since):
    """The card `since` frames after it entered (negative: not yet)."""
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    if since < 0:
        return out
    p = since / SETTLE
    scale = 1.35 - 0.35 * back_out(p)
    opacity = 0.2 + 0.8 * min(1.0, max(0.0, p / 0.62)) ** 0.8
    card = layer.copy()
    if underline is not None:
        x, y, length, thick, colour = underline
        wipe = 1.0 - (1.0 - min(1.0, max(0.0, (since - 4) / 8.0))) ** 3
        if wipe > 0:
            d = ImageDraw.Draw(card)
            rgb = tuple(int(colour[i:i + 2], 16) for i in (0, 2, 4)) + (255,)
            ink = max(3, round(thick * 0.35))
            right = x + length * wipe
            d.rectangle([x - ink, y - ink + 4, right + ink, y + thick + ink + 4], fill=INK)   # its shadow
            d.rectangle([x - ink, y - ink, right + ink, y + thick + ink], fill=INK)
            d.rectangle([x, y, right, y + thick], fill=rgb)
    if abs(scale - 1.0) > 1e-4:
        size = (max(1, round(W * scale)), max(1, round(H * scale)))
        scaled = card.resize(size, Image.LANCZOS)
        ox, oy = round(centre[0] - centre[0] * scale), round(centre[1] - centre[1] * scale)
        card = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        card.paste(scaled, (ox, oy), scaled)
    if opacity < 1.0:
        a = np.array(card)
        a[:, :, 3] = (a[:, :, 3].astype(np.float32) * opacity).round().astype(np.uint8)
        card = Image.fromarray(a, "RGBA")
    out.alpha_composite(card)
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("out")
    parser.add_argument("--only", default="")
    args = parser.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    only = {int(x) for x in args.only.split(",") if x.strip()}
    held = []
    with tempfile.TemporaryDirectory() as tmp:
        for shot, slug, layout, lines, length, enters in CARDS:
            if only and shot not in only:
                continue
            accent = ACCENT.get(lines[0], "ffffff") if layout == "HERO" else None
            layer, centre, underline = text_layer(lines, layout, accent)
            seq = Path(tmp) / f"{shot:02d}"
            seq.mkdir()
            last = None
            for f in range(length):
                since = f - enters
                path = seq / f"f_{f:04d}.png"
                if since > SETTLE and last is not None:
                    shutil.copyfile(last, path)   # held: the same frame
                else:
                    frame(layer, centre, underline, since).save(path)
                    last = path
            mov = out / f"{shot:02d}-{slug}.mov"
            subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(seq / "f_%04d.png"),
                            "-c:v", "prores_ks", "-profile:v", "4", "-pix_fmt", "yuva444p10le", "-alpha_bits", "16",
                            "-vendor", "apl0", "-r", str(FPS), str(mov)], check=True)
            held.append((shot, Image.open(seq / f"f_{length - 1:04d}.png").copy()))
            print(f"{mov.name}: {length} frames, enters on frame {enters}, lands by frame {enters + SETTLE}")
            shutil.rmtree(seq)

        if not only:
            # The contact sheet: every card's held frame over mid grey, 6 across.
            cols, tw = 6, W // 6
            th = round(tw * H / W)
            rows = math.ceil(len(held) / cols)
            sheet = Image.new("RGBA", (W, th * rows), (128, 128, 128, 255))
            for i, (shot, img) in enumerate(held):
                tile = Image.new("RGBA", (W, H), (128, 128, 128, 255))
                tile.alpha_composite(img)
                tile = tile.resize((tw, th), Image.LANCZOS)
                ImageDraw.Draw(tile).text((8, 6), str(shot), fill=(30, 30, 30, 255))
                sheet.paste(tile, ((i % cols) * tw, (i // cols) * th))
            sheet = sheet.convert("RGB")
            sheet_png = out / "contact-sheet.png"
            sheet.save(sheet_png)
            subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-loop", "1", "-framerate", str(FPS), "-i", str(sheet_png),
                            "-t", "3", "-vf", "scale=1920:-2,pad=1920:1080:(ow-iw)/2:(oh-ih)/2:color=0x808080",
                            "-c:v", "prores_ks", "-profile:v", "3", "-pix_fmt", "yuv422p10le", str(out / "contact-sheet.mov")], check=True)
            print(f"contact sheet: {sheet_png}")


if __name__ == "__main__":
    main()
