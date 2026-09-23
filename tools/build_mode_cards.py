"""Compose the GAMEMODE card posters from real in-engine character poses.

Why (owner, 2026-09-23): the mode cards were placeholders, then "this shit sucks" of the
built cards (a flat colour with cropped portrait heads along the bottom). Each mode gets its
own picture. Method (docs/HOME_SCREEN_ANIMATION_METHOD.md): the REAL models, faces never
drawn or altered. Editor/ModeCardPoseAuthor.cs renders every hero and street kid in every
clip they own through the canonical toon pipeline; this script picks poses and draws only
the setting around them (sky, street, chalk, the can, flying slippers, bunting), in the logo
palette with no blue (CLAUDE.md 6.4), lit and shaded like the rest of the UI.

The card's own title is lettered by the UI at its bottom left, so every poster keeps that
corner calm and darker (a same-hue shade rising from the bottom).

Output: Resources/UI/mode-cards/<CardName>.png at twice the card's canvas size.
Run: python tools/build_mode_cards.py [--sheet review.png]
"""
import colorsys
import glob
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
POSES = os.path.join(ROOT, "Logs", "mode-card-poses")
PORTRAITS = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI", "portraits")
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI", "mode-cards")

INK = (28, 15, 6)


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


GOLD, HONEY, PERSIMMON, RIMRED, DEEPRED, PAPER = (hexc(x) for x in ("F5B521", "FCD39F", "FD8041", "C32E0D", "980715", "FEEBD4"))
CHART = hexc("D6CE01")
MAROON = (91, 15, 15)
NIGHT = hexc("2A160B")
STREET = hexc("5A3A2A")


def shade(c, dv, dh=0.0, ds=0.0):
    h, s, v = colorsys.rgb_to_hsv(*[x / 255 for x in c])
    return tuple(int(x * 255) for x in colorsys.hsv_to_rgb((h + dh) % 1, min(1, max(0, s + ds)), min(1, max(0, v + dv))))


# ------------------------------------------------------------------ poses

def pose(pid, *keywords):
    """The first rendered pose of `pid` whose clip name contains a keyword, else idle."""
    folder = os.path.join(POSES, pid)
    files = sorted(glob.glob(os.path.join(folder, "*.png")))
    if not files:
        raise SystemExit("no poses for %s: run ModeCardPoseAuthor first" % pid)
    for k in keywords:
        for f in files:
            if k in os.path.basename(f).lower():
                return f
    for f in files:
        if "idle" in os.path.basename(f).lower():
            return f
    return files[0]


def figure(path, height, flip=False):
    im = Image.open(path).convert("RGBA")
    bbox = im.getbbox()
    im = im.crop(bbox)
    if flip:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
    scale = height / im.height
    return im.resize((max(1, int(im.width * scale)), int(height)), Image.LANCZOS)


def place(canvas, fig, cx, feet_y, shadow=True):
    """Stand a figure with its feet at (cx, feet_y), wearing the game's ink rim."""
    x, y = int(cx - fig.width / 2), int(feet_y - fig.height)
    if shadow:
        sh = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(sh)
        w = fig.width * 0.62
        d.ellipse([cx - w / 2, feet_y - w * 0.09, cx + w / 2, feet_y + w * 0.09], fill=INK + (120,))
        canvas.alpha_composite(sh.filter(ImageFilter.GaussianBlur(6)))
    alpha = fig.split()[3]
    rim = alpha.filter(ImageFilter.MaxFilter(7))
    rim_img = Image.new("RGBA", fig.size, INK + (255,))
    rim_img.putalpha(rim)
    canvas.alpha_composite(rim_img, (x, y))
    canvas.alpha_composite(fig, (x, y))


def prop(canvas, pid, height, cx, feet_y, angle=0):
    im = Image.open(os.path.join(PORTRAITS, pid + ".png")).convert("RGBA")
    im = im.crop(im.getbbox())
    if angle:
        im = im.rotate(angle, expand=True, resample=Image.BICUBIC)
    scale = height / im.height
    im = im.resize((max(1, int(im.width * scale)), int(height)), Image.LANCZOS)
    place(canvas, im, cx, feet_y, shadow=angle == 0)


# ------------------------------------------------------------------ settings

def sunburst(size, colour, centre, rays=18, strength=0.10):
    w, h = size
    img = Image.new("RGBA", size, colour + (255,))
    d = ImageDraw.Draw(img)
    light = shade(colour, strength, 0.015, -0.10)
    cx, cy = centre
    r = max(w, h) * 1.6
    for k in range(rays):
        if k % 2:
            continue
        a0 = math.radians(k * 360 / rays)
        a1 = math.radians((k + 1) * 360 / rays)
        d.polygon([(cx, cy), (cx + r * math.cos(a0), cy + r * math.sin(a0)), (cx + r * math.cos(a1), cy + r * math.sin(a1))], fill=light + (255,))
    glow = Image.new("L", size, 0)
    ImageDraw.Draw(glow).ellipse([cx - w * 0.45, cy - w * 0.45, cx + w * 0.45, cy + w * 0.45], fill=160)
    glow = glow.filter(ImageFilter.GaussianBlur(w * 0.12))
    img = Image.composite(Image.new("RGBA", size, shade(colour, 0.18, 0.02, -0.2) + (255,)), img, glow)
    return img


def sky(size, top, bottom):
    w, h = size
    img = Image.new("RGBA", size)
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / max(1, h - 1)
        c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        d.line([(0, y), (w, y)], fill=c + (255,))
    return img


def street(canvas, horizon, colour=STREET):
    w, h = canvas.size
    d = ImageDraw.Draw(canvas)
    d.rectangle([0, horizon, w, h], fill=colour + (255,))
    d.rectangle([0, horizon, w, horizon + 10], fill=shade(colour, 0.12) + (255,))
    # chalk box lines in perspective
    chalk = PAPER + (170,)
    for k in range(-3, 4):
        x0 = w / 2 + k * w * 0.10
        x1 = w / 2 + k * w * 0.42
        d.line([(x0, horizon + 14), (x1, h)], fill=shade(colour, 0.05) + (255,), width=2)
    return chalk


def chalk_circle(canvas, cx, cy, rx):
    d = ImageDraw.Draw(canvas)
    for k in range(3):
        d.ellipse([cx - rx + k, cy - rx * 0.28 + k, cx + rx - k, cy + rx * 0.28 - k], outline=PAPER + (210,), width=5)


def bunting(canvas, y, colours, sag=40):
    w, _ = canvas.size
    d = ImageDraw.Draw(canvas)
    pts = [(x, y + sag * math.sin(math.pi * x / w)) for x in range(0, w + 1, 8)]
    d.line(pts, fill=INK + (255,), width=4)
    n = int(w / 70)
    for k in range(n):
        x = (k + 0.5) * w / n
        yy = y + sag * math.sin(math.pi * x / w)
        c = colours[k % len(colours)]
        d.polygon([(x - 24, yy), (x + 24, yy), (x, yy + 56)], fill=c + (255,), outline=INK + (255,))


def speed_lines(canvas, x0, y0, x1, y1, n=3, colour=PAPER):
    d = ImageDraw.Draw(canvas)
    for k in range(n):
        off = (k - (n - 1) / 2) * 22
        d.line([(x0, y0 + off), (x1, y1 + off)], fill=colour + (220,), width=10)


def confetti(canvas, count, colours, seed=4):
    rnd = random.Random(seed)
    w, h = canvas.size
    d = ImageDraw.Draw(canvas)
    for _ in range(count):
        x, y = rnd.uniform(0, w), rnd.uniform(0, h * 0.6)
        s = rnd.uniform(10, 22)
        a = rnd.uniform(0, math.pi)
        pts = [(x + s * math.cos(a + t), y + s * 0.5 * math.sin(a + t)) for t in (0, math.pi / 2, math.pi, 3 * math.pi / 2)]
        d.polygon(pts, fill=rnd.choice(colours) + (255,), outline=INK + (255,))


def title_shade(canvas, colour, start=0.62, strength=190):
    w, h = canvas.size
    grad = Image.new("L", canvas.size, 0)
    gd = ImageDraw.Draw(grad)
    for y in range(h):
        t = max(0.0, (y / h - start) / (1 - start))
        gd.line([(0, y), (w, y)], fill=int(strength * t ** 1.3))
    dark = Image.new("RGBA", canvas.size, shade(colour, -0.45, -0.02, 0.1) + (255,))
    return Image.composite(dark, canvas, grad)


# ------------------------------------------------------------------ the cards

def practice():
    size = (800, 700)
    c = sunburst(size, GOLD, (560, 300))
    street(c, 470, shade(GOLD, -0.42, -0.03, 0.05))
    chalk_circle(c, 590, 560, 120)
    prop(c, "metal", 150, 590, 565)
    prop(c, "tsinelas", 90, 430, 330, angle=35)
    speed_lines(c, 250, 380, 380, 340)
    place(c, figure(pose("sean", "hero-sean-ignite@70"), 440), 220, 650)
    return title_shade(c, GOLD)


def custom():
    size = (800, 700)
    c = sunburst(size, PERSIMMON, (400, 260))
    confetti(c, 40, [GOLD, HONEY, CHART, RIMRED])
    street(c, 500, shade(PERSIMMON, -0.45, -0.03, 0.05))
    place(c, figure(pose("zack", "hero-zack-charge@70"), 380, flip=True), 620, 650)
    place(c, figure(pose("totoy", "jump@35"), 370), 180, 650)
    place(c, figure(pose("maring", "emote-yes@35"), 400), 400, 670)
    return title_shade(c, PERSIMMON)


def classic(size=(920, 1480), crowd=("maring", "totoy", "kuya_boy", "ate_girlie"), scale=1.0):
    w, h = size
    c = sky(size, shade(PERSIMMON, 0.05, 0.02, -0.1), HONEY)
    sun = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(sun).ellipse([w * 0.55, h * 0.12, w * 0.95, h * 0.12 + w * 0.4], fill=GOLD + (230,))
    c.alpha_composite(sun.filter(ImageFilter.GaussianBlur(3)))
    # rooftops in silhouette
    d = ImageDraw.Draw(c)
    rnd = random.Random(7)
    x = 0
    while x < w:
        bw = rnd.randint(110, 190); bh = rnd.randint(int(h * 0.12), int(h * 0.22))
        top = h * 0.52 - bh
        col = shade(DEEPRED, -0.1 + rnd.uniform(-0.05, 0.05), 0.0, -0.2)
        d.rectangle([x, top, x + bw, h * 0.56], fill=col + (255,))
        d.polygon([(x - 12, top), (x + bw / 2, top - 50), (x + bw + 12, top)], fill=shade(col, -0.08) + (255,))
        for wy in range(int(top + 30), int(h * 0.52), 55):
            d.rectangle([x + 22, wy, x + 46, wy + 26], fill=GOLD + (200,))
        x += bw + rnd.randint(4, 20)
    bunting(c, int(h * 0.30), [GOLD, CHART, PERSIMMON, PAPER, RIMRED])
    street(c, int(h * 0.56), STREET)
    chalk_circle(c, w * 0.5, h * 0.72, 150)
    prop(c, "metal", 190, w * 0.5, h * 0.72)
    # The can is the subject: the kids stand around it, small enough that it stays in view.
    spots = [(0.17, 0.83, 0.235 * h * scale), (0.83, 0.83, 0.235 * h * scale), (0.30, 0.98, 0.27 * h * scale), (0.71, 0.98, 0.27 * h * scale)]
    moves = ("holding-right-shoot@70", "sprint@35", "jump@35", "idle@35")
    for (pid, move), (fx, fy, fh) in zip(zip(crowd, moves), spots):
        place(c, figure(pose(pid, move, "idle"), fh, flip=fx > 0.5), w * fx, h * fy)
    return title_shade(c, HONEY, start=0.7, strength=170)


def ranked(size=(920, 1480), heroes=(("cheska", 0.24, 0.78, 560, False), ("sean", 0.78, 0.80, 580, True), ("dante", 0.5, 0.95, 700, False))):
    w, h = size
    c = sunburst(size, DEEPRED, (w * 0.5, h * 0.36), rays=22, strength=0.12)
    trophy = Image.new("RGBA", size, (0, 0, 0, 0))
    td = ImageDraw.Draw(trophy)
    cx, top = w * 0.5, h * 0.12
    td.polygon([(cx - 150, top), (cx + 150, top), (cx + 110, top + 200), (cx + 30, top + 250), (cx + 30, top + 320),
                (cx + 110, top + 350), (cx - 110, top + 350), (cx - 30, top + 320), (cx - 30, top + 250), (cx - 110, top + 200)],
               fill=GOLD + (255,), outline=INK + (255,))
    td.arc([cx - 230, top + 10, cx - 90, top + 170], 90, 270, fill=GOLD + (255,), width=26)
    td.arc([cx + 90, top + 10, cx + 230, top + 170], -90, 90, fill=GOLD + (255,), width=26)
    c.alpha_composite(trophy.filter(ImageFilter.GaussianBlur(1)))
    street(c, int(h * 0.62), shade(DEEPRED, -0.5, 0.0, 0.0))
    moves = {"cheska": "hero-cheska-nova@70", "sean": "hero-sean-supernova@70", "dante": "hero-dante-stomp@70"}
    for pid, fx, fy, fh, flip in heroes:
        place(c, figure(pose(pid, moves.get(pid, "hero-")), fh, flip=flip), w * fx, h * fy)
    return title_shade(c, DEEPRED, start=0.68)


def classic_choice():
    return classic((1040, 920), ("totoy", "maring", "kuya_boy", "inday"), scale=1.25)


def hero_choice():
    w, h = 1040, 920
    c = sunburst((w, h), GOLD, (w * 0.5, h * 0.32), rays=20)
    street(c, int(h * 0.62), shade(GOLD, -0.45, -0.03, 0.05))
    for pid, move, fx, fh, flip in (("zack", "hero-zack-summon@70", 0.25, 380, False), ("rafi", "hero-rafi-breakwater@70", 0.75, 380, True),
                                    ("cheska", "hero-cheska-raise@70", 0.5, 520, False)):
        place(c, figure(pose(pid, move), fh, flip=flip), w * fx, h * 0.97)
    return title_shade(c, GOLD, start=0.66)


CARDS = {
    "PracticeCard": practice, "CustomCard": custom, "ClassicCard": classic, "RankedCard": ranked,
    "ClassicChoice": classic_choice, "HeroStrikeChoice": hero_choice,
}


def main():
    os.makedirs(OUT, exist_ok=True)
    made = {}
    for name, fn in CARDS.items():
        im = fn().convert("RGB")
        im.save(os.path.join(OUT, name + ".png"))
        made[name] = im
    if "--sheet" in sys.argv:
        path = sys.argv[sys.argv.index("--sheet") + 1]
        sheet = Image.new("RGB", (2000, 900), MAROON)
        x = 10
        for name, im in made.items():
            t = im.copy(); t.thumbnail((330, 880))
            sheet.paste(t, (x, 10)); x += t.width + 12
        sheet.save(path)
    print("wrote", len(made), "posters")


if __name__ == "__main__":
    main()
