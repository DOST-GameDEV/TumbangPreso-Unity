"""Paint the atlas of the Payo audience villager `young_man`, by hand, in the Classics' finish.

    py -3 tools/author_payo_audience_v2_young_man_textures.py [--size 1024] [--sheet file.png]

Writes ArtSource/payo/audience/v2/young_man/audience_v2_young_man-atlas.png. The model is built by
tools/author_payo_audience_v2_young_man.py, which imports this file for the LAYOUT only (PIL is
imported inside the painting calls; Blender's Python has none). Paint first, then build.

THE BRIEF: docs/reports/character-redesign/payo-audience-brief.md, on top of classic-brief.md. A
copy of the Kuya Boy redesign's textures script (the base Classic) rewritten for him; that file
is left alone.

WHO HE IS. A young Ifugao man in the crowd on the Payo basin wall, a farmer-warrior: black hair
in the Ifugao bowl crop under a woven rattan band with one upright feather, bare chest with his
earned batok, an indigo-black bahag with thin madder-red and white bands, a wrapped belt, a
small woven pinuhha bag on a cord, a brass armlet, barefoot.

THE FINISH (owner, 2026-10-19, on the rejected v1 audience: "compare the textures to the cast,
they look noisy"). The SAME finish as Kuya Boy's atlas: each form one clean flat tone with the
Classic's base, light and shade steps, one very soft lit patch where the Classic has one, and
hard-edged hand-set shapes for everything drawn. No noise, no grain, no texture strokes, no
jitter. Woven bands and motifs are clean polygons; the belt cords, the flap hems, the armlet,
the bag's lid, the band and the feather are GEOMETRY in the model script, not paint.

WHAT IS TAKEN FROM THE HEROES AND KUYA BOY:
  * INK 1a1420 (Dante's) for eyes and mouth; tattoo ink a near black 17151b (method section 3:
    tattoo ink is black);
  * EYES: solid ink blocks at the heroes' place (|x| 0.048 to 0.120, from z 0.456). HIS CUT: the
    top edge drops 8 mm toward the nose, a level, sure look (the eye's top edge does a brow's
    job; no brows, his fringe sits over the forehead);
  * MOUTH: one thin stroke at the heroes' 7.4 mm, its right end turned up a little: confident;
  * no nose, no wrinkles, no blush (he is male).

HIS COLOURS (QUIET: a crowd never steals a hero's hue; payo-audience-brief "Colour"):
  * skin a5643c, in the heroes' range (Rafi b2764a, Dante a8602c), a step off both;
  * hair Dante's black 1a181e with its top and deep tones: black is BLACK;
  * the bahag: an indigo-black 262a3d ground (hue 229, far from Rafi's sea teal), a desaturated
    madder 8a3238 in THIN bands only, an undyed cotton e6dcc6;
  * natural rattan b8956a for the band, the belt cords' knot and the bag; a dark fibre cord;
  * brass c39b48 is his one metal (the armlet only): no silver, no gold trim, no teal.

HIS BATOK, PLANNED BY ROUTE BEFORE ANY COORDINATE (CHARACTER_MODEL_METHOD section 4). Ifugao and
Kalinga men's chest tattoos run from the shoulders down over the pectorals; his are that
structure, and NOT Rafi's (Rafi: a throat star, level clavicle rails, a diamond chain, a centre
column, oblique wings, teeth). He has no centre line, no throat mark, no diamond and no star.
Each route has its own fill; one line family (solid black at 10 to 12 mm, skin laid back as
negative space); the two sides drawn separately:
  1. LEFT CHAKLAG: a crescent from the top of his left shoulder down and in over the pectoral to
     above the nipple. Fill: a LADDER, two rails and four rungs.
  2. RIGHT CHAKLAG: the same crescent on his right. Fill: ONE SOLID BAND with three big skin
     notches cut into its neck edge.
  3. LEFT SHOULDER AND ARM: a solid deltoid cap that carries route 1 onto the arm; the brass
     armlet below it; under the armlet two bands, the route's line set.
  4. RIGHT SHOULDER AND ARM: a deltoid cap split by one skin stripe; a ladder band round the
     upper arm; one band above the elbow.
  5. BACK, LEFT: three stacked chevrons pointing down the shoulder blade.
  6. BACK, RIGHT: a comb: one upright bar with three rungs to the outside.
The left crescent's curves were laid out on a drafting script (offset rails along a hand-drawn
centreline) and are pasted below as literal polygons; nothing here generates a mark.

ROLE HUES (offence orange #f87020, defence blue #0080e8): the cloth colours are checked in `main`.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "young_man"
ATLAS_NAME = "audience_v2_young_man-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see `main`)
GUTTER = 4
SS = 2

# ---------------------------------------------------------------------------
# THE COLOURS. Each family a base, a light, a shade and a deep tone, as the Classics' are.
# ---------------------------------------------------------------------------
SKIN = "a5643c"; SKIN_LIT = "b8774f"; SKIN_SHADE = "874e2e"; SKIN_DEEP = "6a3b22"
HAIR = "1a181e"; HAIR_TOP = "2b2933"; HAIR_DEEP = "0e0d12"                              # Dante's black
INK = "1a1420"; TATTOO = "17151b"
INDIGO = "262a3d"; INDIGO_LIT = "363b54"; INDIGO_DARK = "1b1e2c"; INDIGO_DEEP = "12141d"
RED = "8a3238"; RED_DARK = "68252a"                                                      # madder, thin bands only
WHITE = "e6dcc6"; WHITE_SHADE = "c6b99e"; WHITE_DEEP = "a29579"                          # undyed cotton
RATTAN = "b8956a"; RATTAN_LIT = "cfae84"; RATTAN_DARK = "7f6448"; RATTAN_DEEP = "5e4a36"
CORD = "4a3b2e"; CORD_LIT = "5e4c3c"; CORD_DARK = "33281f"
BRASS = "c39b48"; BRASS_LIT = "dcbb6c"; BRASS_DARK = "7e6a30"
FEATHER = "ece6d8"; FEATHER_SHADE = "cfc6b2"

CLOTH_HEXES = [INDIGO, INDIGO_LIT, INDIGO_DARK, INDIGO_DEEP, RED, RED_DARK, WHITE, WHITE_SHADE, WHITE_DEEP,
               RATTAN, RATTAN_LIT, RATTAN_DARK, RATTAN_DEEP, CORD, CORD_LIT, CORD_DARK, BRASS, BRASS_LIT, BRASS_DARK]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES. Blender space: x his left, -y the way he faces, z up.
# ---------------------------------------------------------------------------
WAIST = (0.156, 0.202)          # the bahag's wrapped waist, indigo: deep, so the crowd sees a dark mass at his hips
ARM_Z = 0.2878                  # the arm bone's height
ELBOW_X = 0.190
WRIST = 0.252
HAND_END = 0.310
FRINGE_FOOT = 0.566             # the lowest fringe lock's tip, before the head's scale

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048).
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.20, 0.20, 0.33, 0.68), 1500),
    },
    "torso": {
        "front": ((-0.16, 0.16, 0.15, 0.37), 1400),
        "back": ((-0.16, 0.16, 0.15, 0.37), 1300),
        "xpos": ((-0.11, 0.11, 0.15, 0.37), 900),
        "xneg": ((-0.11, 0.11, 0.15, 0.37), 900),
    },
    "armL": {
        "front": ((0.09, 0.33, 0.20, 0.375), 1200),
        "back": ((0.09, 0.33, 0.20, 0.375), 1100),
        "top": ((0.09, 0.33, -0.09, 0.10), 1100),
        "bottom": ((0.09, 0.33, -0.09, 0.10), 700),
    },
    "armR": {
        "front": ((-0.33, -0.09, 0.20, 0.375), 1200),
        "back": ((-0.33, -0.09, 0.20, 0.375), 1100),
        "top": ((-0.33, -0.09, -0.09, 0.10), 1100),
        "bottom": ((-0.33, -0.09, -0.09, 0.10), 700),
    },
}
VIEW_BIAS = {}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). u across, v UP.
SWATCHES = {
    "flap_front": (290, 330, 0.126, 0.144),
    "flap_back": (260, 220, 0.120, 0.100),
    "band": (420, 60, 0.36, 0.034),        # the rattan head band, u round it
    "bag": (150, 160, 0.060, 0.064),       # the pinuhha's front face
}

FLATS = {
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "hair_top": HAIR_TOP, "hair": HAIR, "hair_deep": HAIR_DEEP,
    "indigo_lit": INDIGO_LIT, "indigo": INDIGO, "indigo_dark": INDIGO_DARK, "indigo_deep": INDIGO_DEEP,
    "red": RED, "red_dark": RED_DARK,
    "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "rattan_lit": RATTAN_LIT, "rattan": RATTAN, "rattan_dark": RATTAN_DARK, "rattan_deep": RATTAN_DEEP,
    "cord_lit": CORD_LIT, "cord": CORD, "cord_dark": CORD_DARK,
    "brass_lit": BRASS_LIT, "brass": BRASS, "brass_dark": BRASS_DARK,
    "feather": FEATHER, "feather_shade": FEATHER_SHADE, "ink": INK, "tattoo": TATTOO,
}
FLAT_PX = 40

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin. A shelf pack into the top half."""
    if size in _LAYOUT:
        return _LAYOUT[size]
    k = size / float(ATLAS)
    gap = max(2, int(round((2 * GUTTER + 2) * k)))
    for step in range(40):
        s = k * (1.0 - 0.02 * step)
        items = []
        for group, views in GROUPS.items():
            for view, (win, density) in views.items():
                items.append((group + "." + view, int(round((win[1] - win[0]) * density * s)),
                              int(round((win[3] - win[2]) * density * s))))
        for name, (w, h, _wm, _hm) in SWATCHES.items():
            items.append((name, max(8, int(round(w * s))), max(8, int(round(h * s)))))
        for name in FLATS:
            items.append((name, max(8, int(round(FLAT_PX * k))), max(8, int(round(FLAT_PX * k)))))
        items.sort(key=lambda it: (-it[2], it[0]))
        out, x, y, shelf = {}, gap, gap, 0
        for name, w, h in items:
            if x + w + gap > size:
                x, y, shelf = gap, y + shelf + gap, 0
            out[name] = (x, y, w, h)
            x += w + gap
            shelf = max(shelf, h)
        if y + shelf + gap <= size // 2:
            _LAYOUT[size] = out
            return out
    raise SystemExit("the islands do not fit the top half of the atlas")


def window(name):
    if "." in name:
        group, view = name.split(".")
        return GROUPS[group][view][0]
    return (0.0, 1.0, 0.0, 1.0)


def atlas_uv(name, a, b, size=ATLAS):
    """(a, b) on an island to a Blender UV (u right, v up), clamped a hair inside the island."""
    x, y, w, h = layout(size)[name]
    a0, a1, b0, b1 = window(name)
    fa = min(max((a - a0) / (a1 - a0), 0.0), 1.0)
    fb = min(max((b - b0) / (b1 - b0), 0.0), 1.0)
    px = x + 0.5 + fa * (w - 1.0)
    py = y + 0.5 + (1.0 - fb) * (h - 1.0)
    return (px / size, 1.0 - py / size)


def _rgb(hex_str):
    h = hex_str.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def _near_role_hue(hex_str):
    r, g, b = (c / 255.0 for c in _rgb(hex_str))
    h, s, v = colorsys.rgb_to_hsv(r, g, b)
    for role in ("f87020", "0080e8"):
        hr = colorsys.rgb_to_hsv(*(c / 255.0 for c in _rgb(role)))[0]
        dh = abs(h - hr)
        dh = min(dh, 1 - dh)
        if dh < 0.05 and s > 0.45 and v > 0.45:
            return role
    return None


def _spline(points, closed=False, steps=10):
    pts = list(points)
    n = len(pts)
    if n < 3:
        return pts
    out = []
    last = n if closed else n - 1
    for i in range(last):
        p0 = pts[(i - 1) % n] if closed or i > 0 else pts[0]
        p1 = pts[i]
        p2 = pts[(i + 1) % n]
        p3 = pts[(i + 2) % n] if closed or i + 2 < n else pts[n - 1]
        for k in range(steps):
            t = k / float(steps)
            t2, t3 = t * t, t * t * t
            out.append(tuple(
                0.5 * ((2 * p1[c]) + (-p0[c] + p2[c]) * t + (2 * p0[c] - 5 * p1[c] + 4 * p2[c] - p3[c]) * t2
                       + (-p0[c] + 3 * p1[c] - 3 * p2[c] + p3[c]) * t3) for c in range(2)))
    if not closed:
        out.append(pts[-1])
    return out


class Island:
    def __init__(self, name, base, size, metres=None):
        from PIL import Image
        self.name = name
        x, y, w, h = layout(size)[name]
        self.rect = (x, y, w, h)
        self.w, self.h = w * SS, h * SS
        self.win = window(name)
        a0, a1, b0, b1 = self.win
        span_a, span_b = (a1 - a0, b1 - b0) if metres is None else metres
        self.mm = 0.5 * (self.w / span_a + self.h / span_b) / 1000.0
        self.img = Image.new("RGB", (self.w, self.h), _rgb(base))

    def px(self, p):
        a0, a1, b0, b1 = self.win
        return ((p[0] - a0) / (a1 - a0) * self.w, (b1 - p[1]) / (b1 - b0) * self.h)

    def _lay(self, colour, mask, feather, strength):
        from PIL import Image, ImageFilter
        if feather > 0:
            mask = mask.filter(ImageFilter.GaussianBlur(feather * self.mm))
        if strength < 1.0:
            mask = mask.point(lambda v: int(v * strength))
        self.img.paste(Image.new("RGB", (self.w, self.h), _rgb(colour)), (0, 0), mask)
        return self

    def mark(self, colour, points, feather=0.0, strength=1.0, curved=True):
        from PIL import Image, ImageDraw
        pts = _spline(points, closed=True) if curved else list(points)
        mask = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(mask).polygon([self.px(p) for p in pts], fill=255)
        return self._lay(colour, mask, feather, strength)

    def blob(self, colour, centre, ra, rb, feather=0.0, strength=1.0):
        from PIL import Image, ImageDraw
        mask = Image.new("L", (self.w, self.h), 0)
        x0, y0 = self.px((centre[0] - ra, centre[1] + rb))
        x1, y1 = self.px((centre[0] + ra, centre[1] - rb))
        ImageDraw.Draw(mask).ellipse([x0, y0, x1, y1], fill=255)
        return self._lay(colour, mask, feather, strength)

    def stroke(self, colour, points, width, feather=0.3, strength=1.0, taper=(0.25, 0.25), curved=True):
        """A drawn line `width` mm wide: used only for the fist's finger lines and the mouth, as the
        Classic draws them."""
        from PIL import Image, ImageDraw
        pts = [self.px(p) for p in (_spline(points) if curved else points)]
        n = len(pts)
        left, right = [], []
        for i, (x, y) in enumerate(pts):
            ax, ay = pts[max(i - 1, 0)]
            bx, by = pts[min(i + 1, n - 1)]
            dx, dy = bx - ax, by - ay
            d = math.hypot(dx, dy) or 1.0
            t = i / float(max(n - 1, 1))
            ease = min(1.0, t / 0.3) if t < 0.3 else min(1.0, (1.0 - t) / 0.3)
            end = taper[0] if t < 0.5 else taper[1]
            half = 0.5 * width * self.mm * (end + (1.0 - end) * math.sin(ease * math.pi / 2.0))
            left.append((x - dy / d * half, y + dx / d * half))
            right.append((x + dy / d * half, y - dx / d * half))
        mask = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(mask).polygon(left + right[::-1], fill=255)
        return self._lay(colour, mask, feather, strength)

    def band(self, colour, b0, b1, feather=0.0, strength=1.0):
        a0, a1 = self.win[0], self.win[1]
        pad = (a1 - a0)
        return self.mark(colour, [(a0 - pad, b0), (a1 + pad, b0), (a1 + pad, b1), (a0 - pad, b1)],
                         feather, strength, curved=False)

    def column(self, colour, a0, a1, feather=0.0, strength=1.0):
        b0, b1 = self.win[2], self.win[3]
        pad = (b1 - b0)
        return self.mark(colour, [(a0, b0 - pad), (a1, b0 - pad), (a1, b1 + pad), (a0, b1 + pad)],
                         feather, strength, curved=False)

    def ink(self, points, colour=TATTOO):
        """One tattoo mark: a hard-edged polygon in black ink, its corners as typed."""
        return self.mark(colour, points, 0.22, 1.0, curved=False)

    def skin(self, points, colour=SKIN):
        """Skin laid back over ink (negative space), hard edged."""
        return self.mark(colour, points, 0.22, 1.0, curved=False)

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE HEAD. His face on the flat front of the block, drawn the way the heroes' are.
# ---------------------------------------------------------------------------
# HIS LEFT EYE (the right is its mirror): the heroes' block, its top edge falling 8 mm toward the
# nose. A level, sure look; not a scowl (the fall is a third of Bayan's).
EYE_LEFT = [(0.048, 0.4625), (0.054, 0.4565), (0.114, 0.4565), (0.120, 0.4625), (0.120, 0.5150), (0.114, 0.5180),
            (0.054, 0.5080), (0.048, 0.5040)]
# HIS MOUTH: one thin stroke at the heroes' width, its right end turned up a little
MOUTH = [(-0.031, 0.4085), (-0.010, 0.4060), (0.014, 0.4060), (0.032, 0.4100)]
MOUTH_WIDTH = 7.4


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT: flat skin, one very soft lit patch, two ink eyes, one stroke.
    c.blob(SKIN_LIT, (0.0, 0.480), 0.120, 0.075, 30, 0.30)
    # under the fringe: hair-dark where the hair blocks sit, and ONE flat step of shade below
    # their tips, the fringe's shadow on his brow
    c.band(HAIR_DEEP, FRINGE_FOOT + 0.012, 0.70)
    c.band(SKIN_SHADE, FRINGE_FOOT - 0.012, FRINGE_FOOT + 0.012, 0.0, 0.70)
    for s in (1, -1):
        c.mark(INK, [(s * x, z) for x, z in EYE_LEFT], 0.3, 1.0, curved=False)
    c.stroke(INK, MOUTH, MOUTH_WIDTH, 0.3, 1.0, (0.55, 0.7))


# ---------------------------------------------------------------------------
# THE TORSO. Bare skin with its form steps (lit over the collarbones, shade down the flanks and
# under the pectoral slabs, as Rafi's and Kuya Boy's skin is stepped), then the batok routes,
# then the bahag's wrapped waist with its thin bands.
# ---------------------------------------------------------------------------

def _waist(c):
    """The bahag's wrapped waist as it meets itself round the body (the block is geometry)."""
    lo, hi = WAIST
    c.band(INDIGO, lo - 0.004, hi + 0.002)
    c.band(INDIGO_LIT, hi - 0.004, hi - 0.001)
    c.band(INDIGO_DARK, lo - 0.004, lo + 0.003)
    # the weave's thin bands: white, madder, white, each a few millimetres, centred on the wrap
    c.band(WHITE, 0.1810, 0.1850)
    c.band(RED, 0.1850, 0.1915)
    c.band(WHITE, 0.1915, 0.1955)
    # and one more thin madder band low on the wrap
    c.band(RED, 0.1640, 0.1680)


def _skin_form_front(c):
    c.mark(SKIN_LIT, [(-0.104, 0.340), (0.104, 0.340), (0.096, 0.322), (0.0, 0.326), (-0.096, 0.322)], 8, 0.45)
    c.mark(SKIN_SHADE, [(-0.17, 0.34), (-0.104, 0.33), (-0.090, 0.26), (-0.094, 0.17), (-0.17, 0.17)], 7, 0.6)
    c.mark(SKIN_SHADE, [(0.17, 0.34), (0.104, 0.33), (0.090, 0.26), (0.094, 0.17), (0.17, 0.17)], 7, 0.6)
    # the shade the pectoral slabs throw on the belly
    c.mark(SKIN_SHADE, [(-0.108, 0.262), (0.108, 0.262), (0.104, 0.250), (0.0, 0.253), (-0.104, 0.250)], 2.0, 0.7)


def paint_torso_front(c):
    _skin_form_front(c)
    # === ROUTE 1, LEFT CHAKLAG: the ladder (drafted along one centreline, pasted literal) ===
    c.ink([(0.1093, 0.3545), (0.0992, 0.3371), (0.0888, 0.3239), (0.0752, 0.3111), (0.0605, 0.3008), (0.0451, 0.2929),
           (0.0262, 0.2860), (0.0300, 0.2756), (0.0491, 0.2827), (0.0663, 0.2915), (0.0823, 0.3027), (0.0968, 0.3163),
           (0.1086, 0.3314), (0.1188, 0.3490)])                                   # neck-side rail
    c.ink([(0.1292, 0.3430), (0.1188, 0.3252), (0.1055, 0.3080), (0.0901, 0.2936), (0.0726, 0.2813), (0.0534, 0.2715),
           (0.0340, 0.2644), (0.0378, 0.2540), (0.0574, 0.2612), (0.0784, 0.2719), (0.0972, 0.2852), (0.1135, 0.3004),
           (0.1282, 0.3195), (0.1387, 0.3375)])                                   # outer rail
    c.ink([(0.1267, 0.3367), (0.1213, 0.3271), (0.1091, 0.3340), (0.1145, 0.3436)])   # four rungs, each its own place
    c.ink([(0.1078, 0.3089), (0.1002, 0.3010), (0.0900, 0.3106), (0.0976, 0.3186)])
    c.ink([(0.0828, 0.2868), (0.0737, 0.2806), (0.0658, 0.2921), (0.0749, 0.2983)])
    c.ink([(0.0535, 0.2705), (0.0433, 0.2665), (0.0382, 0.2796), (0.0485, 0.2835)])
    # === ROUTE 2, RIGHT CHAKLAG: one solid band, three skin notches cut into its neck edge ===
    c.ink([(-0.1093, 0.3545), (-0.0992, 0.3371), (-0.0888, 0.3239), (-0.0752, 0.3111), (-0.0605, 0.3008), (-0.0451, 0.2929),
           (-0.0262, 0.2860), (-0.0378, 0.2540), (-0.0574, 0.2612), (-0.0784, 0.2719), (-0.0972, 0.2852), (-0.1135, 0.3004),
           (-0.1282, 0.3195), (-0.1387, 0.3375)])
    c.skin([(-0.1036, 0.3483), (-0.0922, 0.3294), (-0.1192, 0.3259)])
    c.skin([(-0.0841, 0.3214), (-0.0674, 0.3071), (-0.0920, 0.2952)])
    c.skin([(-0.0560, 0.3010), (-0.0371, 0.2914), (-0.0575, 0.2760)])
    _waist(c)


def paint_torso_back(c):
    c.mark(SKIN_LIT, [(-0.104, 0.340), (-0.026, 0.338), (-0.032, 0.304), (-0.098, 0.308)], 8, 0.40)
    c.mark(SKIN_LIT, [(0.104, 0.340), (0.026, 0.338), (0.032, 0.302), (0.098, 0.306)], 8, 0.40)
    c.mark(SKIN_SHADE, [(-0.17, 0.34), (-0.106, 0.33), (-0.092, 0.26), (-0.096, 0.17), (-0.17, 0.17)], 7, 0.6)
    c.mark(SKIN_SHADE, [(0.17, 0.34), (0.106, 0.33), (0.092, 0.26), (0.096, 0.17), (0.17, 0.17)], 7, 0.6)
    # the spine: one soft flat channel of shade, as Rafi's back has
    c.mark(SKIN_SHADE, [(-0.008, 0.336), (0.008, 0.336), (0.008, 0.214), (-0.008, 0.214)], 3.0, 0.45, curved=False)
    # === ROUTE 5, BACK LEFT: three chevrons pointing down the shoulder blade, each its own size ===
    c.ink([(0.040, 0.3440), (0.074, 0.3250), (0.110, 0.3440), (0.110, 0.3320), (0.074, 0.3130), (0.040, 0.3320)])
    c.ink([(0.044, 0.3180), (0.075, 0.2995), (0.106, 0.3180), (0.106, 0.3060), (0.075, 0.2875), (0.044, 0.3060)])
    c.ink([(0.050, 0.2920), (0.076, 0.2750), (0.100, 0.2920), (0.100, 0.2805), (0.076, 0.2635), (0.050, 0.2805)])
    # === ROUTE 6, BACK RIGHT: the comb, one upright bar and three rungs to the outside ===
    c.ink([(-0.086, 0.3460), (-0.073, 0.3460), (-0.073, 0.2600), (-0.086, 0.2600)])
    c.ink([(-0.114, 0.3400), (-0.086, 0.3400), (-0.086, 0.3285), (-0.112, 0.3280)])
    c.ink([(-0.110, 0.3140), (-0.086, 0.3140), (-0.086, 0.3025), (-0.109, 0.3020)])
    c.ink([(-0.105, 0.2880), (-0.086, 0.2880), (-0.086, 0.2770), (-0.104, 0.2765)])
    _waist(c)


def _torso_side(c):
    c.mark(SKIN_LIT, [(-0.060, 0.330), (0.030, 0.330), (0.026, 0.300), (-0.056, 0.300)], 7, 0.35)
    c.mark(SKIN_SHADE, [(-0.11, 0.26), (0.11, 0.26), (0.11, 0.17), (-0.11, 0.17)], 6, 0.45, curved=False)
    _waist(c)


# ---------------------------------------------------------------------------
# THE ARMS. Bare. Along the arm (x): deltoid 0.096 to 0.150, the armlet on his LEFT at 0.134 to
# 0.157, elbow 0.190, fist 0.252 to 0.310. Routes 3 and 4.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    if view in ("front", "back"):
        for z in (ARM_Z + 0.0225, ARM_Z, ARM_Z - 0.0225):
            c.stroke(SKIN_DEEP, [(s * (WRIST + 0.029), z), (s * (WRIST + 0.046), z)], 2.6, 0.3, 0.95, (1.0, 0.5), curved=False)
        c.stroke(SKIN_DEEP, [(s * (WRIST + 0.010), ARM_Z + 0.0280), (s * (WRIST + 0.020), ARM_Z + 0.0300), (s * (WRIST + 0.027), ARM_Z + 0.0390)],
                 2.6, 0.3, 0.95, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * (WRIST + 0.014), -0.026), (s * (WRIST + 0.046), -0.026), (s * (WRIST + 0.046), 0.038), (s * (WRIST + 0.014), 0.038)], 5, 0.6)


def _arm_form(c, s, view):
    x = lambda v: s * v
    if view in ("front", "back"):
        c.mark(SKIN_LIT, [(x(0.120), 0.336), (x(0.180), 0.334), (x(0.180), 0.310), (x(0.120), 0.312)], 5, 0.45)
        c.mark(SKIN_SHADE, [(x(0.09), 0.20), (x(0.33), 0.20), (x(0.33), 0.258), (x(0.09), 0.262)], 2.5, 0.55, curved=False)
    elif view == "top":
        c.mark(SKIN_LIT, [(x(0.120), -0.026), (x(0.180), -0.024), (x(0.180), 0.036), (x(0.120), 0.038)], 5, 0.45)
    elif view == "bottom":
        c.column(SKIN_SHADE, *sorted((x(0.09), x(0.33))), 0, 0.8)


def paint_arm_left(c, view):
    _arm_form(c, 1, view)
    # === ROUTE 3: the deltoid cap, carrying the left chaklag onto the arm ===
    if view in ("front", "back"):
        c.ink([(0.094, 0.380), (0.126, 0.380), (0.126, 0.2960), (0.118, 0.2700), (0.106, 0.2620), (0.094, 0.2600)])
    elif view == "top":
        c.ink([(0.094, -0.10), (0.126, -0.10), (0.126, 0.11), (0.094, 0.11)])
    # (the armlet, 0.134 to 0.157, is geometry)
    # under it, the route's line set: two bands of their own widths, round the arm
    c.column(TATTOO, 0.1610, 0.1725, 0.22)
    c.column(TATTOO, 0.1790, 0.1870, 0.22)
    _hand(c, 1, view)


def paint_arm_right(c, view):
    _arm_form(c, -1, view)
    # === ROUTE 4: a deltoid cap split by one skin stripe ===
    if view in ("front", "back"):
        c.ink([(-0.094, 0.380), (-0.128, 0.380), (-0.128, 0.2980), (-0.120, 0.2720), (-0.108, 0.2630), (-0.094, 0.2610)])
        c.skin([(-0.1075, 0.381), (-0.1135, 0.381), (-0.1135, 0.2640), (-0.1075, 0.2625)])
    elif view == "top":
        c.ink([(-0.094, -0.10), (-0.128, -0.10), (-0.128, 0.11), (-0.094, 0.11)])
        c.skin([(-0.1075, -0.10), (-0.1135, -0.10), (-0.1135, 0.11), (-0.1075, 0.11)])
    # the ladder band round the upper arm: two rails, rungs on the three sides that show
    c.column(TATTOO, -0.1500, -0.1405, 0.22)
    c.column(TATTOO, -0.1735, -0.1640, 0.22)
    if view in ("front", "back"):
        c.ink([(-0.1640, ARM_Z + 0.0300), (-0.1500, ARM_Z + 0.0300), (-0.1500, ARM_Z + 0.0410), (-0.1640, ARM_Z + 0.0410)])
        c.ink([(-0.1640, ARM_Z - 0.0050), (-0.1500, ARM_Z - 0.0050), (-0.1500, ARM_Z + 0.0060), (-0.1640, ARM_Z + 0.0060)])
        c.ink([(-0.1640, ARM_Z - 0.0400), (-0.1500, ARM_Z - 0.0400), (-0.1500, ARM_Z - 0.0290), (-0.1640, ARM_Z - 0.0290)])
    elif view == "top":
        c.ink([(-0.1640, -0.0330), (-0.1500, -0.0330), (-0.1500, -0.0220), (-0.1640, -0.0220)])
        c.ink([(-0.1640, 0.0050), (-0.1500, 0.0050), (-0.1500, 0.0160), (-0.1640, 0.0160)])
        c.ink([(-0.1640, 0.0390), (-0.1500, 0.0390), (-0.1500, 0.0500), (-0.1640, 0.0500)])
    # one band above the elbow
    c.column(TATTOO, -0.1880, -0.1800, 0.22)
    _hand(c, -1, view)


# ---------------------------------------------------------------------------
# THE SWATCHES. u across, v up, both 0 to 1. Clean hand-set shapes only.
# ---------------------------------------------------------------------------

def paint_flap_front(c):
    """The front flap: the indigo-black ground, a lit step down its middle, and near the hem the
    Ifugao bands: white, madder, white, and a white zigzag between two thin rules. The cream hem
    itself is a block in the model."""
    c.mark(INDIGO_LIT, [(0.30, 1.0), (0.70, 1.0), (0.66, 0.40), (0.34, 0.40)], 0.0, 0.35, curved=False)
    c.mark(INDIGO_DARK, [(0.0, 1.0), (0.10, 1.0), (0.10, 0.0), (0.0, 0.0)], 0.0, 1.0, curved=False)
    c.mark(INDIGO_DARK, [(0.90, 1.0), (1.0, 1.0), (1.0, 0.0), (0.90, 0.0)], 0.0, 1.0, curved=False)
    c.mark(RED, [(0.0, 0.900), (1.0, 0.900), (1.0, 0.930), (0.0, 0.930)], 0.2, 1.0, curved=False)   # one thin band up top
    c.mark(WHITE, [(0.0, 0.120), (1.0, 0.120), (1.0, 0.150), (0.0, 0.150)], 0.2, 1.0, curved=False)
    c.mark(RED, [(0.0, 0.150), (1.0, 0.150), (1.0, 0.205), (0.0, 0.205)], 0.2, 1.0, curved=False)
    c.mark(WHITE, [(0.0, 0.205), (1.0, 0.205), (1.0, 0.235), (0.0, 0.235)], 0.2, 1.0, curved=False)
    c.mark(WHITE, [(0.0, 0.265), (1.0, 0.265), (1.0, 0.280), (0.0, 0.280)], 0.2, 1.0, curved=False)
    c.mark(WHITE, [(0.0, 0.385), (1.0, 0.385), (1.0, 0.400), (0.0, 0.400)], 0.2, 1.0, curved=False)
    # the zigzag: ONE polygon, its corners typed
    c.mark(WHITE, [(0.00, 0.300), (0.125, 0.365), (0.25, 0.300), (0.375, 0.365), (0.50, 0.300), (0.625, 0.365), (0.75, 0.300),
                   (0.875, 0.365), (1.00, 0.300), (1.00, 0.335), (0.875, 0.400 - 0.010), (0.75, 0.335), (0.625, 0.390),
                   (0.50, 0.335), (0.375, 0.390), (0.25, 0.335), (0.125, 0.390), (0.00, 0.335)], 0.2, 1.0, curved=False)


def paint_flap_back(c):
    c.mark(INDIGO_LIT, [(0.30, 1.0), (0.70, 1.0), (0.66, 0.40), (0.34, 0.40)], 0.0, 0.30, curved=False)
    c.mark(WHITE, [(0.0, 0.150), (1.0, 0.150), (1.0, 0.190), (0.0, 0.190)], 0.2, 1.0, curved=False)
    c.mark(RED, [(0.0, 0.190), (1.0, 0.190), (1.0, 0.260), (0.0, 0.260)], 0.2, 1.0, curved=False)
    c.mark(WHITE, [(0.0, 0.260), (1.0, 0.260), (1.0, 0.300), (0.0, 0.300)], 0.2, 1.0, curved=False)


def paint_band(c):
    """The head band: rattan, a lit top edge and a dark foot (tone steps by facing, as the
    Classic does them), and one madder thread round its middle."""
    c.mark(RATTAN_LIT, [(0.0, 0.78), (1.0, 0.78), (1.0, 1.0), (0.0, 1.0)], 0.0, 1.0, curved=False)
    c.mark(RATTAN_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.20), (0.0, 0.20)], 0.0, 1.0, curved=False)
    c.mark(RED, [(0.0, 0.44), (1.0, 0.44), (1.0, 0.56), (0.0, 0.56)], 0.2, 1.0, curved=False)


def paint_bag(c):
    """The pinuhha's front: woven rattan in two rows (a clean dark seam between them), one madder
    band, one woven diamond in the middle in dark rattan. Its lid is a block."""
    c.mark(RATTAN_LIT, [(0.0, 0.80), (1.0, 0.80), (1.0, 1.0), (0.0, 1.0)], 0.0, 0.8, curved=False)
    c.mark(RATTAN_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.10), (0.0, 0.10)], 0.0, 1.0, curved=False)
    c.mark(RED, [(0.0, 0.30), (1.0, 0.30), (1.0, 0.38), (0.0, 0.38)], 0.2, 1.0, curved=False)
    c.mark(RATTAN_DARK, [(0.50, 0.70), (0.66, 0.54), (0.50, 0.40), (0.34, 0.54)], 0.2, 1.0, curved=False)
    c.mark(RATTAN, [(0.50, 0.63), (0.59, 0.54), (0.50, 0.47), (0.41, 0.54)], 0.2, 1.0, curved=False)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        return I(name, base, SWATCHES[name][2:4])

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    c = I("torso.front", SKIN); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", SKIN); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", SKIN); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", SKIN); _torso_side(c); done[c.name] = c
    for view in GROUPS["armL"]:
        c = I("armL." + view, SKIN); paint_arm_left(c, view); done[c.name] = c
    for view in GROUPS["armR"]:
        c = I("armR." + view, SKIN); paint_arm_right(c, view); done[c.name] = c
    c = swatch("flap_front", INDIGO); paint_flap_front(c); done[c.name] = c
    c = swatch("flap_back", INDIGO); paint_flap_back(c); done[c.name] = c
    c = swatch("band", RATTAN); paint_band(c); done[c.name] = c
    c = swatch("bag", RATTAN); paint_bag(c); done[c.name] = c
    for name, colour in FLATS.items():
        done[name] = I(name, colour, (0.04, 0.04))
    missing = set(layout(size)) - set(done)
    if missing:
        raise SystemExit("islands not painted: %s" % sorted(missing))
    return done


def main():
    from PIL import Image

    size = ATLAS
    if "--size" in sys.argv:
        size = int(sys.argv[sys.argv.index("--size") + 1])
    for hex_str in CLOTH_HEXES:
        if _near_role_hue(hex_str):
            raise SystemExit("#%s sits near role hue #%s" % (hex_str, _near_role_hue(hex_str)))

    islands = build(size)
    atlas = Image.new("RGB", (size, size), _rgb(INDIGO_DEEP))
    bleed = max(1, int(round(GUTTER * size / float(ATLAS))))
    finished = {name: isl.finished() for name, isl in islands.items()}
    for name, img in finished.items():
        x, y, w, h = islands[name].rect
        atlas.paste(img.resize((w + 2 * bleed, h + 2 * bleed), Image.BILINEAR), (x - bleed, y - bleed))
    for name, img in finished.items():
        x, y, w, h = islands[name].rect
        atlas.paste(img, (x, y))

    os.makedirs(OUT_DIR, exist_ok=True)
    out = OUT_DIR / ATLAS_NAME
    for attempt in range(5):
        try:
            atlas.save(out)
            break
        except OSError:
            import time
            time.sleep(1.0)
    else:
        raise SystemExit("could not write %s" % out)
    used = sum(r[2] * r[3] for r in layout(size).values())
    print("wrote %s  (%d x %d, %d islands, %.0f%% of the top half painted)"
          % (out, size, size, len(islands), 100.0 * used / (size * size / 2)))
    if "--sheet" in sys.argv:
        sheet = sys.argv[sys.argv.index("--sheet") + 1]
        atlas.crop((0, 0, size, size // 2)).save(sheet)
        print("wrote %s" % sheet)


if __name__ == "__main__":
    main()
