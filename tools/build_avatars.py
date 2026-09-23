"""Build the profile pictures from the game's own character renders.

Why (owner, 2026-09-23: "generate profile pics too"). The picker held fifteen drawn faces
(tools/build_avatar_art.py): one generic face with a headband in twelve tints, plus three
objects. They did not show a single character the game actually has. The approved roster
thumbnails in Resources/UI/portraits are real in-engine renders of every street kid and hero,
made by TumpPortraitAuthor through the canonical toon pipeline, all framed by the same camera
rule, so the framing problem that sank the old cut-out attempt (Avatars.cs header) is gone.
The faces are never redrawn or altered (HOME_SCREEN_ANIMATION_METHOD.md: the real models).

Each picture: a square sticker in a warm logo colour, soft sunburst rays one step lighter
(the Brawl Stars player-icon ground, inspected in research.md), the character's bust scaled so
the head fills the upper two thirds, and a same-hue shade at the bottom so the bust sits IN
the tile rather than pasted on it. No blue (CLAUDE.md 6.4).

Output: Resources/UI/avatars/avatar_<id>.png, 256 px. Existing ids are left in place, so a
saved choice still loads its old picture until the player picks again.

Run: python tools/build_avatars.py [--sheet review.png]
"""
import colorsys
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PORTRAITS = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI", "portraits")
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI", "avatars")
SIZE = 256
W = 768


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


GOLD, HONEY, PERSIMMON, RIMRED, DEEPRED, CHART = (hexc(x) for x in ("F5B521", "FCD39F", "FD8041", "C32E0D", "980715", "D6CE01"))
MAROON = (91, 15, 15)

# (id, ground) in picker order: the heroes first, then the street kids, then objects.
# Grounds are chosen for contrast with each character's own dominant colour.
SET = [
    ("dante", GOLD), ("sean", GOLD), ("cheska", PERSIMMON), ("zack", DEEPRED), ("nemu", GOLD),
    ("phaister", HONEY), ("rafi", PERSIMMON),
    ("maring", PERSIMMON), ("totoy", GOLD), ("inday", PERSIMMON), ("kuya_boy", GOLD), ("ate_girlie", HONEY),
    ("tikboy", PERSIMMON), ("bebang", RIMRED), ("jun_jun", GOLD), ("lola_pacing", PERSIMMON),
    ("mang_kanor", GOLD), ("aling_nena", RIMRED),
    ("tsinelas", RIMRED), ("boyben", PERSIMMON),
]


def shade(c, dv, dh=0.0, ds=0.0):
    h, s, v = colorsys.rgb_to_hsv(*[x / 255 for x in c])
    h = (h + dh) % 1.0
    s = min(1, max(0, s + ds))
    v = min(1, max(0, v + dv))
    return tuple(int(x * 255) for x in colorsys.hsv_to_rgb(h, s, v))


def ground(colour):
    img = Image.new("RGB", (W, W), colour)
    d = ImageDraw.Draw(img)
    light = shade(colour, 0.10, 0.015, -0.10)
    cx, cy = W * 0.5, W * 0.42
    for k in range(16):                        # sunburst rays
        if k % 2:
            continue
        a0, a1 = math.radians(k * 22.5 - 6), math.radians(k * 22.5 + 6)
        d.polygon([(cx, cy), (cx + W * 1.2 * math.cos(a0), cy + W * 1.2 * math.sin(a0)),
                   (cx + W * 1.2 * math.cos(a1), cy + W * 1.2 * math.sin(a1))], fill=light)
    glow = Image.new("L", (W, W), 0)
    ImageDraw.Draw(glow).ellipse([cx - W * 0.34, cy - W * 0.34, cx + W * 0.34, cy + W * 0.34], fill=150)
    glow = glow.filter(ImageFilter.GaussianBlur(W * 0.08))
    img = Image.composite(Image.new("RGB", (W, W), shade(colour, 0.16, 0.02, -0.18)), img, glow)
    # a same-hue shade rising from the bottom, so the bust sits in the tile
    grad = Image.new("L", (W, W), 0)
    gd = ImageDraw.Draw(grad)
    for y in range(W):
        t = max(0.0, (y / W - 0.55) / 0.45)
        gd.line([(0, y), (W, y)], fill=int(150 * t ** 1.4))
    img = Image.composite(Image.new("RGB", (W, W), shade(colour, -0.35, -0.02, 0.10)), img, grad)
    return img


def bust(pid):
    p = Image.open(os.path.join(PORTRAITS, pid + ".png")).convert("RGBA")
    a = np.asarray(p)[..., 3]
    ys, xs = np.nonzero(a > 20)
    top, bottom, left, right = ys.min(), ys.max(), xs.min(), xs.max()
    width = right - left
    # the head is the top of the silhouette; frame a square from just above it
    side = int(max(width * 1.02, (bottom - top) * 0.92))
    cx = (left + right) / 2
    box = (int(cx - side / 2), int(top - side * 0.12), int(cx + side / 2), int(top - side * 0.12 + side))
    return p.crop(box)


def compose(pid, colour):
    img = ground(colour).convert("RGBA")
    figure = bust(pid).resize((int(W * 0.96), int(W * 0.96)), Image.LANCZOS)
    # an ink rim around the figure, the black outline the whole game uses
    alpha = figure.split()[3]
    rim = alpha.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.MaxFilter(9))
    rim_img = Image.new("RGBA", figure.size, (28, 15, 6, 255))
    rim_img.putalpha(rim)
    drop = Image.new("RGBA", figure.size, (28, 15, 6, 0))
    drop.putalpha(rim.point(lambda v: v * 90 // 255))
    x = (W - figure.width) // 2
    y = int(W * 0.10)
    img.alpha_composite(drop, (x + 10, y + 14))
    img.alpha_composite(rim_img, (x, y))
    img.alpha_composite(figure, (x, y))
    return img.convert("RGB").resize((SIZE, SIZE), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    made = []
    for pid, colour in SET:
        if not os.path.exists(os.path.join(PORTRAITS, pid + ".png")):
            print("skip (no portrait)", pid)
            continue
        im = compose(pid, colour)
        im.save(os.path.join(OUT, "avatar_" + pid + ".png"))
        made.append((pid, im))
    if "--sheet" in sys.argv:
        path = sys.argv[sys.argv.index("--sheet") + 1]
        cols = 7
        sheet = Image.new("RGB", (cols * 180, ((len(made) + cols - 1) // cols) * 200), (91, 15, 15))
        d = ImageDraw.Draw(sheet)
        for k, (pid, im) in enumerate(made):
            x, y = (k % cols) * 180 + 10, (k // cols) * 200 + 10
            sheet.paste(im.resize((160, 160), Image.LANCZOS), (x, y))
            d.text((x, y + 164), pid, fill=(255, 240, 220))
        sheet.save(path)
    print("wrote", len(made), "avatars")


if __name__ == "__main__":
    main()
