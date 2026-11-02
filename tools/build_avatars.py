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

Output: Resources/UI/avatars/avatar_<id>.png, 512 px (256 until the 2026-10-28 sharpness pass). Existing ids are left in place, so a
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
SIZE = 512
W = 768


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


GOLD, HONEY, PERSIMMON, RIMRED, DEEPRED, CHART = (hexc(x) for x in ("F5B521", "FCD39F", "FD8041", "C32E0D", "980715", "D6CE01"))
MAROON = (91, 15, 15)

# (id, ground) in picker order: the heroes first, then the street kids, then objects.
# Grounds are chosen for contrast with each character's own dominant colour.
SET = [
    ("dante", GOLD), ("sean", GOLD), ("cheska", DEEPRED), ("zack", PERSIMMON), ("nemu", GOLD),
    ("phaister", HONEY), ("rafi", HONEY), ("amihan", GOLD), ("paete", HONEY),
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


# ⚠️ EVERY FACE SITS IN THE SAME PLACE (owner, 2026-11-02: "some of them look awkward"). Framing by the silhouette put a wide jacket or a hat in charge of
# the crop, so faces came out at different sizes and heights. Each row is read off that character's portrait, in portrait pixels: the point between the
# eyes and the width of the head with its hair. A hat or antlers are not the head: they run off the top of the tile (owner: "stop using her hat as her head"). Optional fourth: where the eyes sit in the tile, for Rago, whose muscles are the point.
# Re-measured for the 2026-11-02 portrait bake, whose tighter crops moved seven heads.
K = 640 / 290
HEADS = {
    "dante": (150, 185, 190), "sean": (160, 139, 202, 0.50), "cheska": (150, 180, 185), "zack": (170, 182, 190), "nemu": (165, 194, 211),
    "phaister": (150, 236, 185), "rafi": (160, 183, 190), "amihan": (165, 180, 195), "paete": (160, 156, 171), "maring": (160, 185, 216),
    "totoy": (155, 180, 185), "inday": (185, 200, 185), "kuya_boy": (165, 144, 211), "ate_girlie": (170, 195, 185), "tikboy": (170, 205, 190),
    "bebang": (170, 195, 185), "jun_jun": (165, 210, 190), "lola_pacing": (165, 189, 206), "mang_kanor": (165, 175, 190), "aling_nena": (170, 165, 200),
}
FILL = 0.74
EYES_AT = 0.60


def head_middle(p, ey, fw):
    """The middle of the head, measured from the picture: a turned face has its eyes off to one side, so centring those pushes the head off (owner: "not centered").
    Row by row across the face, the middle of the silhouette, taking the median so a raised fist or a hat brim on a few rows cannot pull it."""
    a = np.asarray(p)[..., 3] > 40
    mids = []
    for y in range(int(ey - fw * 0.12), int(ey + fw * 0.22)):
        xs = np.nonzero(a[max(0, min(a.shape[0] - 1, y))])[0]
        if len(xs):
            mids.append((xs.min() + xs.max()) / 2)
    return float(np.median(mids))


def place(pid, img):
    """Scale and position one portrait so its eyes and head land on the set's marks. Falls back to the whole picture for objects."""
    p = Image.open(os.path.join(PORTRAITS, pid + ".png")).convert("RGBA")
    head = HEADS.get(pid)
    if head is None:
        box = p.getbbox()
        p = p.crop(box)
        scale = W * 0.86 / max(p.size)
        x, y = (W - p.width * scale) / 2, (W - p.height * scale) / 2
    else:
        ey, fw = head[1] * K, head[2] * K
        ex = head_middle(p, ey, fw)
        scale = W * FILL / fw
        x, y = W * 0.5 - ex * scale, W * (head[3] if len(head) > 3 else EYES_AT) - ey * scale
        # The bust runs off the bottom of the tile, never stopping short of it.
        bottom = p.getbbox()[3] * scale + y
        if bottom < W + 2:
            y += W + 2 - bottom
    figure = p.resize((int(p.width * scale), int(p.height * scale)), Image.LANCZOS)
    pad = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    pad.alpha_composite(figure.crop((max(0, int(-x)), max(0, int(-y)), figure.width, figure.height)), (max(0, int(x)), max(0, int(y))))
    return pad


def compose(pid, colour):
    img = ground(colour).convert("RGBA")
    figure = place(pid, img)
    # an ink rim around the figure, the black outline the whole game uses
    alpha = figure.split()[3]
    rim = alpha.filter(ImageFilter.MaxFilter(9))
    rim_img = Image.new("RGBA", figure.size, (28, 15, 6, 255))
    rim_img.putalpha(rim)
    drop = Image.new("RGBA", figure.size, (28, 15, 6, 0))
    drop.putalpha(rim.point(lambda v: v * 80 // 255))
    img.alpha_composite(drop, (8, 10))
    img.alpha_composite(rim_img)
    img.alpha_composite(figure)
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
