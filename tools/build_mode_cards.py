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

# Plain words for the clips every model shares, so a composition reads as intent.
ALIAS = {
    "throw": "holding-right-shoot@70", "shoot": "holding-right-shoot@70", "cheer": "emote-cheer@70",
    "yes": "emote-yes@70", "wave": "emote-comeon@70", "victory": "emote-tooeasy@70", "dance": "emote-streetdance@70",
    "run": "sprint@35", "sprint": "sprint@35", "jump": "jump@35", "idle": "idle@35",
}


def pose(pid, *keywords):
    """The first rendered pose of `pid` whose clip name contains a keyword, else idle."""
    keywords = tuple(ALIAS.get(k, k) for k in keywords)
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
    rim = alpha.filter(ImageFilter.MaxFilter(9))
    rim_img = Image.new("RGBA", fig.size, INK + (255,))
    rim_img.putalpha(rim)
    canvas.alpha_composite(rim_img, (x, y))
    canvas.alpha_composite(fig, (x, y))


def halo(canvas, cx, cy, radius, colour, strength=150):
    """A soft glow behind a figure, so it lifts off a busy ground (the HOME tiles' warm light)."""
    g = Image.new("L", canvas.size, 0)
    ImageDraw.Draw(g).ellipse([cx - radius, cy - radius, cx + radius, cy + radius], fill=strength)
    g = g.filter(ImageFilter.GaussianBlur(radius * 0.35))
    canvas.alpha_composite(Image.composite(Image.new("RGBA", canvas.size, colour + (255,)), Image.new("RGBA", canvas.size, (0, 0, 0, 0)), g))


def back_row(fig, amount=0.28, tint=MAROON):
    """A figure further back: a little darker and warmer, so the front row reads first."""
    r, g, b, a = fig.split()
    flat = Image.new("RGBA", fig.size, tint + (255,))
    mixed = Image.blend(fig.convert("RGBA"), flat, amount)
    mixed.putalpha(a)
    return mixed


KNIT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI", "handmade-lobby", "tile_hero_face.png")


def knit_grain(canvas, strength=0.10):
    """HOME's knit, laid over the poster faintly, so the card belongs to the same family as its tile."""
    tile = Image.open(KNIT).convert("L")
    tile = tile.crop((40, 40, tile.width - 40, tile.height - 40))
    w, h = canvas.size
    grain = Image.new("L", (w, h))
    for y in range(0, h, tile.height):
        for x in range(0, w, tile.width):
            grain.paste(tile, (x, y))
    lo, hi = grain.getextrema()
    grain = grain.point(lambda v: int(128 + (v - (lo + hi) / 2) * 1.6))
    dark = Image.new("RGBA", (w, h), (40, 20, 10, 255))
    light = Image.new("RGBA", (w, h), (255, 240, 210, 255))
    mask_dark = grain.point(lambda v: int(max(0, 128 - v) * 2 * strength))
    mask_light = grain.point(lambda v: int(max(0, v - 128) * 2 * strength * 0.6))
    out = Image.composite(dark, canvas, mask_dark)
    return Image.composite(light, out, mask_light)


def vignette(canvas, strength=110):
    w, h = canvas.size
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).ellipse([-w * 0.25, -h * 0.2, w * 1.25, h * 1.15], fill=255)
    m = m.filter(ImageFilter.GaussianBlur(min(w, h) * 0.18)).point(lambda v: int((255 - v) * strength / 255))
    return Image.composite(Image.new("RGBA", (w, h), INK + (255,)), canvas, m)


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
#
# v2 (owner 2026-10-28: "replace this all, thoroughly make them better and use the current
# character models"). Every poster is re-composed from poses rendered off today's models
# (`ModeCardPoseAuthor`, 1024 px) and sized to the knit card window it is shown in, so the card
# crops nothing away: the lead is big and in action, a back row gives depth, a warm glow lifts
# the lead off the ground, and HOME's knit grain ties the picture to its tile. The card letters
# its own name under the window, so no title shade is painted any more.

TEAL, LIME, WINE, ORANGE, HOMEGOLD = hexc("469577"), hexc("B4C444"), hexc("55071D"), hexc("E8722D"), hexc("F3BC49")


def cast(c, people, ground_y):
    """people: (pid, keywords, x fraction, height fraction, flip, back). The back row is drawn first."""
    w, h = c.size
    for pid, keys, fx, fh, flip, back in sorted(people, key=lambda p: not p[5]):
        fig = figure(pose(pid, *keys), h * fh, flip=flip)
        if back:
            fig = back_row(fig)
        place(c, fig, w * fx, ground_y - (h * 0.06 if back else 0))


def finish(c, grain=0.09, vig=90):
    return vignette(knit_grain(c, grain), vig)


def practice(size=(720, 452)):
    w, h = size
    c = sunburst(size, HOMEGOLD, (w * 0.62, h * 0.34), rays=18, strength=0.11)
    street(c, int(h * 0.70), shade(HOMEGOLD, -0.40, -0.03, 0.05))
    chalk_circle(c, w * 0.72, h * 0.86, w * 0.10)
    prop(c, "metal", h * 0.22, w * 0.72, h * 0.87)
    prop(c, "tsinelas", h * 0.13, w * 0.52, h * 0.40, angle=35)
    speed_lines(c, w * 0.34, h * 0.52, w * 0.47, h * 0.44)
    halo(c, w * 0.24, h * 0.55, h * 0.42, shade(HOMEGOLD, 0.2, 0, -0.3))
    cast(c, [("sean", ("hero-sean-ignite@70", "throw"), 0.26, 0.80, False, False)], h * 0.99)
    return finish(c)


def custom(size=(720, 452)):
    w, h = size
    c = sunburst(size, ORANGE, (w * 0.5, h * 0.30), rays=20)
    bunting(c, int(h * 0.06), [HOMEGOLD, LIME, TEAL, PAPER, RIMRED], sag=22)
    confetti(c, 34, [HOMEGOLD, HONEY, LIME, TEAL])
    street(c, int(h * 0.74), shade(ORANGE, -0.45, -0.03, 0.05))
    halo(c, w * 0.5, h * 0.55, h * 0.45, shade(ORANGE, 0.25, 0.02, -0.35))
    cast(c, [("nemu", ("cheer", "yes"), 0.13, 0.50, False, True),
             ("amihan", ("dance", "cheer"), 0.87, 0.50, True, True),
             ("zack", ("yes",), 0.36, 0.64, False, False),
             ("cheska", ("yes",), 0.64, 0.64, True, False)], h * 0.99)
    return finish(c)


def street_scene(size, crowd):
    """The street game at dusk: rooftops, bunting, the can in its chalk ring, kids all round it."""
    w, h = size
    c = sky(size, shade(ORANGE, 0.05, 0.02, -0.1), HONEY)
    sun = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(sun).ellipse([w * 0.60, h * 0.07, w * 0.95, h * 0.07 + w * 0.35], fill=HOMEGOLD + (235,))
    c.alpha_composite(sun.filter(ImageFilter.GaussianBlur(3)))
    d = ImageDraw.Draw(c)
    rnd = random.Random(7)
    x = -20
    horizon = h * (0.44 if h > w else 0.48)
    while x < w:
        bw = rnd.randint(int(w * 0.13), int(w * 0.21)); bh = rnd.randint(int(h * 0.10), int(h * 0.20))
        top = horizon - bh
        col = shade(WINE, 0.06 + rnd.uniform(-0.05, 0.05), 0.0, -0.15)
        d.rectangle([x, top, x + bw, horizon + h * 0.04], fill=col + (255,))
        d.polygon([(x - 12, top), (x + bw / 2, top - h * 0.035), (x + bw + 12, top)], fill=shade(col, -0.08) + (255,))
        wy = top + h * 0.02
        while wy < horizon - h * 0.02:
            d.rectangle([x + bw * 0.18, wy, x + bw * 0.34, wy + h * 0.018], fill=HOMEGOLD + (210,))
            wy += h * 0.04
        x += bw + rnd.randint(4, 20)
    bunting(c, int(h * 0.20), [HOMEGOLD, LIME, ORANGE, PAPER, RIMRED, TEAL], sag=int(h * 0.03))
    street(c, int(horizon + h * 0.04), STREET)
    ring_y = horizon + h * (0.18 if h > w else 0.20)
    chalk_circle(c, w * 0.5, ring_y, w * 0.13)
    prop(c, "metal", h * (0.11 if h > w else 0.17), w * 0.5, ring_y + 4)
    prop(c, "tsinelas", h * 0.07, w * 0.30, horizon - h * 0.05, angle=-30)
    prop(c, "sandals", h * 0.06, w * 0.72, horizon - h * 0.12, angle=40)
    tall = h > w
    spots = [(0.15, horizon + h * 0.16, 0.20 if tall else 0.30, True), (0.85, horizon + h * 0.16, 0.20 if tall else 0.30, True),
             (0.10, h * 0.995, 0.33 if tall else 0.46, False), (0.90, h * 0.995, 0.33 if tall else 0.46, False),
             (0.34, h * 1.005, 0.36 if tall else 0.50, False), (0.66, h * 1.005, 0.36 if tall else 0.50, False)]
    people = []
    for (pid, keys), (fx, fy, fh, back) in zip(crowd, spots):
        people.append((pid, keys + ("idle",), fx, fy, fh, fx > 0.5, back))
    for pid, keys, fx, fy, fh, flip, back in sorted(people, key=lambda p: not p[6]):
        fig = figure(pose(pid, *keys), h * fh, flip=flip)
        if back:
            fig = back_row(fig, 0.22)
        place(c, fig, w * fx, fy)
    return finish(c)


def classic(size=(840, 1232)):
    return street_scene(size, (("maring", ("throw", "shoot")), ("totoy", ("sprint", "run")),
                               ("kuya_boy", ("jump",)), ("ate_girlie", ("cheer", "idle")),
                               ("tikboy", ("run", "sprint")), ("inday", ("cheer", "idle"))))


def classic_choice(size=(960, 672)):
    return street_scene(size, (("bebang", ("cheer", "idle")), ("jun_jun", ("run",)),
                               ("totoy", ("sprint", "run")), ("inday", ("idle",)),
                               ("maring", ("throw", "shoot")), ("kuya_boy", ("jump",))))


def ranked(size=(840, 1232), heroes=(("cheska", ("hero-cheska-nova@70",), 0.20, 0.42, False, True),
                                      ("sean", ("hero-sean-supernova@70",), 0.80, 0.42, True, True),
                                      ("dante", ("hero-dante-roar@70", "hero-dante-stomp@70"), 0.5, 0.54, False, False))):
    w, h = size
    c = sunburst(size, WINE, (w * 0.5, h * 0.28), rays=22, strength=0.10)
    halo(c, w * 0.5, h * 0.24, w * 0.34, HOMEGOLD, 120)
    trophy = Image.new("RGBA", size, (0, 0, 0, 0))
    td = ImageDraw.Draw(trophy)
    cx, top, k = w * 0.5, h * 0.08, w / 920
    td.polygon([(cx - 150 * k, top), (cx + 150 * k, top), (cx + 110 * k, top + 200 * k), (cx + 30 * k, top + 250 * k), (cx + 30 * k, top + 320 * k),
                (cx + 110 * k, top + 350 * k), (cx - 110 * k, top + 350 * k), (cx - 30 * k, top + 320 * k), (cx - 30 * k, top + 250 * k), (cx - 110 * k, top + 200 * k)],
               fill=HOMEGOLD + (255,), outline=INK + (255,))
    td.arc([cx - 230 * k, top + 10 * k, cx - 90 * k, top + 170 * k], 90, 270, fill=HOMEGOLD + (255,), width=int(26 * k))
    td.arc([cx + 90 * k, top + 10 * k, cx + 230 * k, top + 170 * k], -90, 90, fill=HOMEGOLD + (255,), width=int(26 * k))
    c.alpha_composite(trophy.filter(ImageFilter.GaussianBlur(1)))
    street(c, int(h * 0.60), shade(WINE, -0.25, 0.0, 0.0))
    for pid, keys, fx, fh, flip, back in sorted(heroes, key=lambda p: not p[5]):
        fig = figure(pose(pid, *keys), h * fh, flip=flip)
        if back:
            fig = back_row(fig, 0.18)
        place(c, fig, w * fx, h * (0.84 if back else 1.0))
    return finish(c, 0.08, 110)


def hero_choice(size=(960, 672)):
    w, h = size
    c = sunburst(size, HOMEGOLD, (w * 0.5, h * 0.30), rays=20)
    street(c, int(h * 0.70), shade(HOMEGOLD, -0.42, -0.03, 0.05))
    halo(c, w * 0.5, h * 0.50, h * 0.48, shade(HOMEGOLD, 0.2, 0, -0.3))
    cast(c, [("phaister", ("hero-phaister-eclipse@70",), 0.10, 0.48, False, True),
             ("paete", ("hero-paete-command@70",), 0.90, 0.56, True, True),
             ("zack", ("hero-zack-throttle@70", "hero-zack-overclock@70"), 0.27, 0.60, False, False),
             ("rafi", ("hero-rafi-breakwater@70",), 0.73, 0.60, True, False),
             ("sean", ("hero-sean-ignite@70",), 0.5, 0.72, False, False)], h * 0.995)
    return finish(c)


def tutorial_choice(size=(960, 672)):
    w, h = size
    c = sunburst(size, TEAL, (w * 0.5, h * 0.32), rays=18, strength=0.09)
    street(c, int(h * 0.70), shade(TEAL, -0.35, 0.0, 0.0))
    chalk_circle(c, w * 0.5, h * 0.84, w * 0.11)
    prop(c, "metal", h * 0.20, w * 0.5, h * 0.85)
    halo(c, w * 0.5, h * 0.55, h * 0.45, shade(TEAL, 0.25, 0, -0.3))
    cast(c, [("totoy", ("cheer", "wave", "idle"), 0.26, 0.74, False, False),
             ("maring", ("throw", "shoot", "idle"), 0.74, 0.74, True, False)], h * 0.995)
    return finish(c)


def training_choice(size=(960, 672)):
    w, h = size
    c = sunburst(size, HOMEGOLD, (w * 0.5, h * 0.32), rays=18)
    street(c, int(h * 0.70), shade(HOMEGOLD, -0.42, -0.03, 0.05))
    halo(c, w * 0.5, h * 0.55, h * 0.45, shade(HOMEGOLD, 0.2, 0, -0.3))
    prop(c, "tsinelas", h * 0.12, w * 0.5, h * 0.36, angle=30)
    speed_lines(c, w * 0.36, h * 0.46, w * 0.46, h * 0.40)
    cast(c, [("cheska", ("throw",), 0.28, 0.78, False, False),
             ("phaister", ("hero-phaister-hex@70",), 0.73, 0.78, True, False)], h * 0.995)
    return finish(c)


CARDS = {
    "PracticeCard": practice, "CustomCard": custom, "ClassicCard": classic, "RankedCard": ranked,
    "ClassicChoice": classic_choice, "HeroStrikeChoice": hero_choice,
    "TutorialChoice": tutorial_choice, "TrainingChoice": training_choice,
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
