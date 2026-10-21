"""Paint the atlas of the Payo audience villager `woman` (v2), by hand, in the heroes' style.

    py -3 tools/author_payo_audience_v2_woman_textures.py [--size 1024] [--sheet file.png]

Writes ArtSource/payo/audience/v2/woman/audience_v2_woman-atlas.png. The model is built by
tools/author_payo_audience_v2_woman.py, which imports this file for the LAYOUT only (PIL is
imported inside the painting calls, Blender's Python has none). Paint first, then build.

WHY. docs/reports/character-redesign/payo-audience-brief.md: the Payo basin wall wants a crowd of
precolonial Ifugao villagers, each built EXACTLY like a Classic redesign. This file is a copy of
Inday's textures script (tools/author_character_redesign_inday_textures.py) rewritten for her.

WHO SHE IS. A young Ifugao woman, the loudest supporter in the crowd: hair tied up and wound with
bead strands, a knee-length tapis of horizontal woven bands on an indigo-black ground, a short
wrapped top of undyed cotton with a banded edge, a heavy bead necklace, brass hoops, barefoot.

THE FINISH (owner, on the rejected v1 crowd: "they look noisy. they just dont look like the
models we have at all"). The same finish as Inday's atlas: clean FLAT tones per form, the base,
shade and light steps the Classics use, no noise, no grain, no jitter. Every mark below is a
clean hand-set shape. The woven bands of the tapis, the edge bands, the belt and every bead are
GEOMETRY in the model script; the only drawing on cloth is the tapis's one diamond motif row,
each diamond its own literal polygon, and a lit patch or two.

THE RULES (section 15.3 of docs/CHARACTER_REDESIGN_DANTE.md, the Classic brief):
  * the face: flat skin, a round blush under each eye (she is a woman), two ink eye blocks, one
    stroke for a mouth. No nose, no wrinkles;
  * NO PAINTED HAIR SHINE;
  * a drawing only on a face that squarely faces its view (the model script's `proj_square`).

COLOUR, QUIET (the brief): an indigo-black and undyed-cotton ground, red only in thin bands, a
desaturated madder, natural rattan, brass as the one metal, skin in the heroes' range. Role hues
#f87020 and #0080e8 are checked off every cloth colour below.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "woman"
ATLAS_NAME = "audience_v2_woman-atlas.png"
ATLAS = 2048
GUTTER = 4
SS = 2

# ---------------------------------------------------------------------------
# THE COLOURS: each family a light, a base, a shade, a deep, as a Classic's is.
# ---------------------------------------------------------------------------
SKIN = "b07448"; SKIN_LIT = "c4875a"; SKIN_SHADE = "925d38"; SKIN_DEEP = "6e4428"       # a warm mid brown, inside the heroes' range
BLUSH = "c8584b"
HAIR = "1a181e"; HAIR_TOP = "2e2b38"; HAIR_DEEP = "0e0d12"                               # the heroes' BLACK (Dante's)
INK = "1a1420"
COTTON = "cbbd9e"; COTTON_LIT = "dacdb0"; COTTON_SHADE = "b2a385"; COTTON_DEEP = "958769"  # undyed cotton, her top
INDIGO = "262838"; INDIGO_LIT = "353850"; INDIGO_DARK = "1a1b27"; INDIGO_DEEP = "111219"  # the tapis ground
MADDER = "9c3632"; MADDER_LIT = "b24b45"; MADDER_DARK = "7a2a27"                          # thin bands only
BONE = "ece6d8"; BONE_SHADE = "cfc8b8"; BONE_DEEP = "aea795"                              # white woven bands, white beads
RATTAN = "a68e64"; RATTAN_LIT = "bea37a"; RATTAN_DARK = "7f6c4c"                          # the woven belt
BRASS = "bf9c48"; BRASS_LIT = "d8b965"; BRASS_DARK = "8f7233"                             # the one metal
BEAD_BLACK = "1e1c22"

CLOTH_HEXES = [HAIR, HAIR_TOP, HAIR_DEEP, COTTON, COTTON_LIT, COTTON_SHADE, COTTON_DEEP, INDIGO, INDIGO_LIT, INDIGO_DARK,
               INDIGO_DEEP, MADDER, MADDER_LIT, MADDER_DARK, BONE, BONE_SHADE, BONE_DEEP, RATTAN, RATTAN_LIT, RATTAN_DARK,
               BRASS, BRASS_LIT, BRASS_DARK, BEAD_BLACK]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES.
# ---------------------------------------------------------------------------
SLEEVE_END = 0.138              # along the arm (x): the mouth of her wrapped sleeve
WRIST = 0.240
HAND_END = 0.296
WAIST = 0.222                   # the foot of her top (its banded hem sits on it)
BELT = (0.192, 0.2135)          # the woven belt, under the madder line of the top's hem
SKIRT_TOP = 0.204
SKIRT_HEM = 0.082               # knee length on the cast's short leg
MOTIF = (0.112, 0.140)          # the diamond row on the tapis

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048).
# +x is HER left, -y is the way she faces.
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.20, 0.20, 0.33, 0.67), 1500),
    },
    "torso": {
        "front": ((-0.17, 0.17, 0.20, 0.36), 1200),
        "back": ((-0.17, 0.17, 0.20, 0.36), 1000),
        "xpos": ((-0.11, 0.11, 0.20, 0.36), 800),
        "xneg": ((-0.11, 0.11, 0.20, 0.36), 800),
    },
    "skirt": {
        "front": ((-0.17, 0.17, 0.07, 0.21), 1300),
        "back": ((-0.17, 0.17, 0.07, 0.21), 1100),
        "xpos": ((-0.12, 0.12, 0.07, 0.21), 1000),
        "xneg": ((-0.12, 0.12, 0.07, 0.21), 1000),
    },
    "armL": {
        "front": ((0.09, 0.31, 0.19, 0.385), 1000),
        "back": ((0.09, 0.31, 0.19, 0.385), 800),
        "top": ((0.09, 0.31, -0.10, 0.12), 800),
        "bottom": ((0.09, 0.31, -0.10, 0.12), 600),
    },
    "armR": {
        "front": ((-0.31, -0.09, 0.19, 0.385), 1000),
        "back": ((-0.31, -0.09, 0.19, 0.385), 800),
        "top": ((-0.31, -0.09, -0.10, 0.12), 800),
        "bottom": ((-0.31, -0.09, -0.10, 0.12), 600),
    },
    "footL": {
        "top": ((0.02, 0.15, -0.13, 0.07), 900),
    },
    "footR": {
        "top": ((-0.15, -0.02, -0.13, 0.07), 900),
    },
}

VIEW_BIAS = {}
SWATCHES = {}

FLATS = {
    "hair_top": HAIR_TOP, "hair": HAIR, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "cotton_lit": COTTON_LIT, "cotton": COTTON, "cotton_shade": COTTON_SHADE, "cotton_deep": COTTON_DEEP,
    "indigo_lit": INDIGO_LIT, "indigo": INDIGO, "indigo_dark": INDIGO_DARK, "indigo_deep": INDIGO_DEEP,
    "madder_lit": MADDER_LIT, "madder": MADDER, "madder_dark": MADDER_DARK,
    "bone": BONE, "bone_shade": BONE_SHADE, "bone_deep": BONE_DEEP,
    "rattan_lit": RATTAN_LIT, "rattan": RATTAN, "rattan_dark": RATTAN_DARK,
    "brass_lit": BRASS_LIT, "brass": BRASS, "brass_dark": BRASS_DARK,
    "bead_black": BEAD_BLACK, "ink": INK,
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


# ---------------------------------------------------------------------------
# THE BRUSH: Inday's, cut to what this atlas needs. Clean polygons and ellipses, a fraction of a
# millimetre of edge softening so a line is not stair-stepped, nothing else.
# ---------------------------------------------------------------------------

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

    def mark(self, colour, points, feather=0.25, strength=1.0, curved=False):
        from PIL import Image, ImageDraw
        pts = _spline(points, closed=True) if curved else list(points)
        mask = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(mask).polygon([self.px(p) for p in pts], fill=255)
        return self._lay(colour, mask, feather, strength)

    def blob(self, colour, centre, ra, rb, feather=0.25, strength=1.0):
        from PIL import Image, ImageDraw
        mask = Image.new("L", (self.w, self.h), 0)
        x0, y0 = self.px((centre[0] - ra, centre[1] + rb))
        x1, y1 = self.px((centre[0] + ra, centre[1] - rb))
        ImageDraw.Draw(mask).ellipse([x0, y0, x1, y1], fill=255)
        return self._lay(colour, mask, feather, strength)

    def stroke(self, colour, points, width, feather=0.3, strength=1.0, taper=(0.25, 0.25), curved=True):
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

    def band(self, colour, b0, b1):
        a0, a1 = self.win[0], self.win[1]
        pad = (a1 - a0)
        return self.mark(colour, [(a0 - pad, b0), (a1 + pad, b0), (a1 + pad, b1), (a0 - pad, b1)], 0.0)

    def column(self, colour, a0, a1):
        b0, b1 = self.win[2], self.win[3]
        pad = (b1 - b0)
        return self.mark(colour, [(a0, b0 - pad), (a1, b0 - pad), (a1, b1 + pad), (a0, b1 + pad)], 0.0)

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE HEAD. Her face on the flat front of the block, in the heroes' hand.
#   EYES: the heroes' solid ink block, 52 by 66 mm at (0.077, 0.481). HER CUT: the top OUTER
#   corner taken off on a long diagonal (14 mm across, 14 down), so each eye's top slopes down
#   AWAY from the nose: the open, delighted look of someone cheering. Bebang's and Bayan's slope
#   down TO the nose (fierce), Inday's are half shut under a level lid, Lola Pacing's feet are
#   cut by a smile; nobody's top falls outward.
#   MOUTH: the heroes' 8.6 mm stroke as a WIDE grin, 72 mm across and deep in the middle: she is
#   the loudest supporter on the wall. One stroke.
#   No fringe: her hair is drawn back off her face, so her forehead shows, with one flat shade
#   under the hairline where the crown overhangs.
# ---------------------------------------------------------------------------
EYE_LEFT = [(0.051, 0.508), (0.057, 0.514), (0.089, 0.514), (0.103, 0.500), (0.103, 0.454), (0.097, 0.448),
            (0.057, 0.448), (0.051, 0.454)]
GRIN = [(-0.036, 0.4300), (-0.020, 0.4160), (0.000, 0.4110), (0.020, 0.4160), (0.036, 0.4300)]
MOUTH_WIDTH = 8.6
HAIRLINE = 0.592


def paint_head_front(c):
    c.blob(SKIN_LIT, (0.0, 0.455), 0.118, 0.080, 26, 0.38)
    # the shade under the crown's overhang: one flat tone, cut level, dipping where the two swept
    # locks cross the temples
    c.mark(SKIN_SHADE, [(-0.26, 0.72), (0.26, 0.72), (0.26, HAIRLINE - 0.020), (0.150, HAIRLINE - 0.016),
                        (0.040, HAIRLINE), (-0.040, HAIRLINE), (-0.150, HAIRLINE - 0.016), (-0.26, HAIRLINE - 0.020)], 1.2, 0.62)
    c.blob(BLUSH, (-0.112, 0.426), 0.031, 0.020, 7, 0.62)
    c.blob(BLUSH, (0.112, 0.426), 0.031, 0.020, 7, 0.62)
    c.mark(INK, EYE_LEFT)
    c.mark(INK, [(-x, z) for x, z in EYE_LEFT])
    c.stroke(INK, GRIN, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, GRIN[0], 0.0028, 0.0028)
    c.blob(INK, GRIN[-1], 0.0028, 0.0028)


# ---------------------------------------------------------------------------
# THE TOP: undyed cotton. The wrap's crossing edge, its banded hem, its sleeve bands and the
# necklace are geometry; drawn here is one lit patch on the front and one on the back, flat.
# ---------------------------------------------------------------------------

def paint_torso_front(c):
    c.mark(COTTON_LIT, [(-0.110, 0.322), (-0.064, 0.322), (-0.064, 0.262), (-0.110, 0.258)])
    c.mark(COTTON_LIT, [(0.066, 0.256), (0.108, 0.254), (0.108, 0.236), (0.066, 0.238)])


def paint_torso_back(c):
    c.mark(COTTON_LIT, [(-0.096, 0.316), (0.096, 0.316), (0.092, 0.282), (-0.092, 0.280)])
    # the wrap's end, crossing the back on the same diagonal as the front: a fold in one shade
    c.mark(COTTON_SHADE, [(0.104, 0.322), (0.112, 0.322), (-0.096, 0.236), (-0.106, 0.236)])


def _torso_side(c):
    c.mark(COTTON_SHADE, [(-0.004, 0.300), (0.004, 0.300), (0.004, 0.240), (-0.004, 0.240)])


# ---------------------------------------------------------------------------
# THE TAPIS. Indigo-black; its woven bands are GEOMETRY (the model script). Drawn here, between
# the two bands at MOTIF, is the one motif row of Ifugao weave: small diamonds, alternately
# white and madder, each a white diamond with a madder eye or a madder diamond with none. Each
# side of the skirt has its own row, its own count and spacing (no stamp loop; every diamond is
# a literal centre and half size, and the eye is listed where it is drawn).
# ---------------------------------------------------------------------------
#   (centre x, centre z, half width, half height, colour, eye colour or None)
MOTIF_FRONT = [
    (-0.124, 0.126, 0.0105, 0.0115, BONE, MADDER), (-0.084, 0.126, 0.0105, 0.0115, MADDER, None),
    (-0.042, 0.126, 0.0105, 0.0115, BONE, MADDER), (0.000, 0.126, 0.0110, 0.0120, MADDER, None),
    (0.042, 0.126, 0.0105, 0.0115, BONE, MADDER), (0.084, 0.126, 0.0105, 0.0115, MADDER, None),
    (0.124, 0.126, 0.0105, 0.0115, BONE, MADDER),
]
MOTIF_BACK = [
    (-0.126, 0.126, 0.0105, 0.0115, MADDER, None), (-0.084, 0.126, 0.0105, 0.0115, BONE, MADDER),
    (-0.042, 0.126, 0.0105, 0.0115, MADDER, None), (0.000, 0.126, 0.0110, 0.0120, BONE, MADDER),
    (0.042, 0.126, 0.0105, 0.0115, MADDER, None), (0.084, 0.126, 0.0105, 0.0115, BONE, MADDER),
    (0.126, 0.126, 0.0105, 0.0115, MADDER, None),
]
#   the sides (their window runs along y, the front of her at -y)
MOTIF_SIDE_LEFT = [
    (-0.060, 0.126, 0.0105, 0.0115, MADDER, None), (-0.020, 0.126, 0.0105, 0.0115, BONE, MADDER),
    (0.020, 0.126, 0.0105, 0.0115, MADDER, None), (0.060, 0.126, 0.0105, 0.0115, BONE, MADDER),
]
MOTIF_SIDE_RIGHT = [
    (-0.060, 0.126, 0.0105, 0.0115, BONE, MADDER), (-0.020, 0.126, 0.0105, 0.0115, MADDER, None),
    (0.020, 0.126, 0.0105, 0.0115, BONE, MADDER), (0.060, 0.126, 0.0105, 0.0115, MADDER, None),
]


def _diamonds(c, row):
    for x, z, hw, hh, colour, eye in row:
        c.mark(colour, [(x, z + hh), (x + hw, z), (x, z - hh), (x - hw, z)])
        if eye is not None:
            c.mark(eye, [(x, z + 0.42 * hh), (x + 0.42 * hw, z), (x, z - 0.42 * hh), (x - 0.42 * hw, z)])


def paint_skirt(c, view):
    if view == "front":
        # the wrap's overlap: the outer end of the cloth lies over her left thigh, one lit tone
        c.mark(INDIGO_LIT, [(0.050, 0.200), (0.062, 0.200), (0.068, 0.086), (0.056, 0.086)])
        _diamonds(c, MOTIF_FRONT)
    elif view == "back":
        _diamonds(c, MOTIF_BACK)
    elif view == "xpos":
        _diamonds(c, MOTIF_SIDE_LEFT)
    else:
        _diamonds(c, MOTIF_SIDE_RIGHT)


# ---------------------------------------------------------------------------
# THE ARMS: the cotton sleeve to SLEEVE_END (its banded edge is geometry), then bare skin and the
# fist, drawn as Inday's is.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    if view in ("front", "back"):
        for z in (0.3070, 0.2880, 0.2690):
            c.stroke(SKIN_DEEP, [(s * 0.2640, z), (s * 0.2815, z)], 2.4, 0.3, 0.9, (1.0, 0.5), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.2540, 0.3230), (s * 0.2660, 0.3250), (s * 0.2730, 0.3310)], 2.4, 0.3, 0.9, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.256, -0.026), (s * 0.280, -0.026), (s * 0.280, 0.038), (s * 0.256, 0.038)])


def paint_arm(c, s, view):
    x = lambda v: s * v
    col = lambda colour, a, b: c.column(colour, *sorted((x(a), x(b))))
    col(COTTON, 0.08, SLEEVE_END + 0.004)
    if view in ("front", "top"):
        if view == "front":
            c.mark(COTTON_LIT, [(x(0.108), 0.326), (x(0.128), 0.330), (x(0.128), 0.304), (x(0.108), 0.300)])
        else:
            c.mark(COTTON_LIT, [(x(0.108), -0.030), (x(0.128), -0.034), (x(0.128), 0.044), (x(0.108), 0.040)])
    col(SKIN, SLEEVE_END + 0.004, 0.33)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE FEET, bare. Looking down on a foot: three toe gaps on the toes' level face, short ink-free
# strokes in the deep skin tone, each its own.
# ---------------------------------------------------------------------------

def paint_foot_top(c, s):
    x = lambda v: s * v
    #   (x of the gap, its length): the big toe's gap is the longest, on the inner side
    for gx, length in ((0.0640, 0.019), (0.0860, 0.016), (0.1050, 0.014)):
        c.stroke(SKIN_DEEP, [(x(gx), -0.116), (x(gx), -0.116 + length)], 2.4, 0.3, 0.9, (0.9, 0.4), curved=False)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}
    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    c = I("torso.front", COTTON); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", COTTON); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", COTTON); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", COTTON); _torso_side(c); done[c.name] = c
    for view in GROUPS["skirt"]:
        c = I("skirt." + view, INDIGO); paint_skirt(c, view); done[c.name] = c
    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    for group, s in (("footL", 1), ("footR", -1)):
        c = I(group + ".top", SKIN); paint_foot_top(c, s); done[c.name] = c
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
    atlas = Image.new("RGB", (size, size), _rgb(INDIGO_DARK))
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
