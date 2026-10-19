"""Paint the atlas of the Payo audience v2 GIRL, by hand, in the heroes' style.

    py -3 tools/author_payo_audience_v2_girl_textures.py [--size 1024] [--sheet file.png]

Writes ArtSource/payo/audience/v2/girl/audience_v2_girl-atlas.png. The model is built by
tools/author_payo_audience_v2_girl.py, which imports this file for the LAYOUT only (PIL is
imported inside the painting calls, Blender's Python has none). Paint first, then build.

WHO. A village girl of about nine in the Payo crowd on the basin wall: precolonial Ifugao. The
brief is docs/reports/character-redesign/payo-audience-brief.md; the machinery is the approved
Bebang redesign's textures script, copied (one villager, one set of files) and repainted.

THE RULES SHE INHERITS (classic-brief.md and section 15.3 of docs/CHARACTER_REDESIGN_DANTE.md):
  * the face is drawn for the flat front of the head: flat skin, the fringe's shadow as one
    tone, a round blush under each eye (she is a girl), two ink eyes, one stroke for a mouth.
    No nose, no sockets, creases, lids, lip shadow or contour;
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points;
  * PAINT STOPS AT ITS OWN PIECE'S EDGES; a drawing only on a face that squarely faces its view.

HER COLOURS. QUIET (the audience brief): a crowd must never take a hero's hue. Indigo-black
cloth (her top, the tapis bands, her belt), an undyed cotton ground (her tapis), a madder red
desaturated and only in THIN bands and beads, brass as the one metal (bead ties, the bead at the
middle of her collar, an anklet). Skin in the heroes' range. Black hair is the heroes' black.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "girl"
ATLAS_NAME = "audience_v2_girl-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint
GUTTER = 4
SS = 2

# ---------------------------------------------------------------------------
# THE COLOURS
# ---------------------------------------------------------------------------
SKIN = "c07a4e"; SKIN_LIT = "d18e60"; SKIN_SHADE = "a4633c"; SKIN_DEEP = "84492a"          # a warm brown, between Bebang and Dante
BLUSH = "dc5a54"
HAIR_TOP = "2b2933"; HAIR_LIT = "232129"; HAIR = "1a181e"; HAIR_DARK = "131217"; HAIR_DEEP = "0e0d12"   # the heroes' black (Dante's values)
INK = "1c161a"
INDIGO = "262a3e"; INDIGO_LIT = "363b55"; INDIGO_MID = "1f2233"; INDIGO_DEEP = "15161f"   # indigo-black cloth
COTTON = "ebe3cf"; COTTON_LIT = "f6f0e0"; COTTON_SHADE = "d2c7ae"; COTTON_DEEP = "aea28a"   # the undyed tapis ground
BAND = "2c3254"; BAND_DEEP = "20253f"                                                     # the tapis' thin indigo bands
MADDER = "804e48"; MADDER_DEEP = "653a35"                                                 # desaturated madder, thin bands and beads only
BEAD_WHITE = "f2ede2"; BEAD_SHADE = "d6cfc0"
BRASS = "c9a24a"; BRASS_LIT = "e2c470"; BRASS_DARK = "957431"                             # the one metal

CLOTH_HEXES = [HAIR_TOP, HAIR_LIT, HAIR, HAIR_DARK, HAIR_DEEP, INDIGO, INDIGO_LIT, INDIGO_MID, INDIGO_DEEP,
               COTTON, COTTON_LIT, COTTON_SHADE, COTTON_DEEP, BAND, BAND_DEEP, MADDER, MADDER_DEEP, BEAD_WHITE, BEAD_SHADE]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES
# ---------------------------------------------------------------------------
SLEEVE_END = 0.136              # along the arm (x): the edge of her short cap sleeve
WRIST = 0.240                   # where the fist block starts
HAND_END = 0.296
SKIRT_TOP = 0.222               # the tapis, from the belt
SKIRT_HEM = 0.094               # to just above the knee... of a chibi leg
TOP_HEM = 0.206                 # her wrapped top tucks under the belt

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048)
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.20, 0.20, 0.33, 0.67), 1500),
    },
    "torso": {
        "front": ((-0.15, 0.15, 0.19, 0.36), 1300),
        "back": ((-0.15, 0.15, 0.19, 0.36), 1000),
        "xpos": ((-0.10, 0.10, 0.19, 0.36), 800),
        "xneg": ((-0.10, 0.10, 0.19, 0.36), 800),
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
    "foot": {
        # looking down on her bare feet: the toes are drawn on the level top of the toe step
        "top": ((-0.15, 0.15, -0.16, 0.08), 900),
    },
}
VIEW_BIAS = {}
SWATCHES = {}

FLATS = {
    "hair_top": HAIR_TOP, "hair_lit": HAIR_LIT, "hair": HAIR, "hair_dark": HAIR_DARK, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "indigo_lit": INDIGO_LIT, "indigo": INDIGO, "indigo_mid": INDIGO_MID, "indigo_deep": INDIGO_DEEP,
    "cotton_lit": COTTON_LIT, "cotton": COTTON, "cotton_shade": COTTON_SHADE, "cotton_deep": COTTON_DEEP,
    "band": BAND, "band_deep": BAND_DEEP, "madder": MADDER, "madder_deep": MADDER_DEEP,
    "bead": BEAD_WHITE, "bead_shade": BEAD_SHADE,
    "brass_lit": BRASS_LIT, "brass": BRASS, "brass_dark": BRASS_DARK, "ink": INK,
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
# THE BRUSH (the Bebang script's, unchanged)
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

    def stitch(self, colour, points, width=1.6, dash=6.0, gap=5.0, strength=0.9):
        from PIL import Image, ImageDraw
        pts = [self.px(p) for p in _spline(points, steps=14)]
        mask = Image.new("L", (self.w, self.h), 0)
        draw = ImageDraw.Draw(mask)
        run, on = 0.0, True
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            seg = math.hypot(bx - ax, by - ay)
            run += seg
            if on:
                draw.line([ax, ay, bx, by], fill=255, width=max(1, int(round(width * self.mm))))
            if run >= (dash if on else gap) * self.mm:
                run, on = 0.0, not on
        return self._lay(colour, mask, 0.25, strength)

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

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE HEAD. Her face, IN THE HEROES' HAND, and NOT Bebang's.
#   * EYES: the heroes' upright ink blocks, a touch narrower than Bebang's (48 by 64 mm) and set a
#     little LOWER on the face and closer in (a child's face: the features sit low). HER CUT is the
#     top edge ARCHED: both top corners cut deep and round, so the block reads round-topped and
#     wide open. Bebang's top edge is a flat slant down to the inside (set, tough); hers is the
#     opposite, eyes gone round at the match.
#   * MOUTH: one thin stroke, SHORT (38 mm against Bebang's 84) and set a little to her left, a
#     small closed smile with its left corner tucked up: shy, trying not to grin.
#   * BLUSH: bigger and stronger than the cast's usual (she is shy), a soft round patch under
#     each eye.
# ---------------------------------------------------------------------------
EYE_LEFT = [(0.0915, 0.5010), (0.0960, 0.4930), (0.0960, 0.4440), (0.0905, 0.4380), (0.0555, 0.4380), (0.0500, 0.4440),
            (0.0500, 0.4930), (0.0545, 0.5010), (0.0640, 0.5055), (0.0820, 0.5055)]
SMILE = [(-0.0130, 0.4140), (-0.0010, 0.4080), (0.0120, 0.4070), (0.0240, 0.4110), (0.0310, 0.4175)]
MOUTH_WIDTH = 8.0
#   the fringe's shadow: (from x, to x, the height it falls to), under each lock's own tip
FRINGE_SHADOW = [(-0.200, -0.150, 0.532), (-0.150, -0.085, 0.582), (-0.085, -0.012, 0.570), (-0.012, 0.062, 0.578),
                 (0.062, 0.135, 0.568), (0.135, 0.200, 0.536)]


def paint_head_front(c):
    c.blob(SKIN_LIT, (0.0, 0.445), 0.118, 0.080, 26, 0.30)
    shadow = [(-0.26, 0.72), (0.26, 0.72)]
    for x0, x1, z in reversed(FRINGE_SHADOW):
        shadow += [(x1, z), (x0, z)]
    c.mark(SKIN_SHADE, shadow, 1.4, 0.62, curved=False)
    c.blob(BLUSH, (-0.106, 0.414), 0.036, 0.023, 7, 0.66)
    c.blob(BLUSH, (0.106, 0.414), 0.036, 0.023, 7, 0.66)
    c.mark(INK, EYE_LEFT, 0.25, 1.0, curved=False)
    c.mark(INK, [(-x, z) for x, z in EYE_LEFT], 0.25, 1.0, curved=False)
    c.stroke(INK, SMILE, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, SMILE[0], 0.0026, 0.0026, 0.25, 1.0)
    c.blob(INK, SMILE[-1], 0.0026, 0.0026, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE TOP. A short wrapped top of indigo-black cotton. The wrap's edge, the collar and the belt
# are geometry; what is drawn is the cloth: a lit patch, the fold where the wrap pulls across, and
# on the back a row of three woven diamonds (the Ifugao motif), each big enough to read.
# Every mark keeps clear of the block's chamfers (12 mm).
# ---------------------------------------------------------------------------

def paint_torso_front(c):
    c.mark(INDIGO_LIT, [(0.020, 0.300), (0.090, 0.296), (0.094, 0.246), (0.030, 0.236)], 9, 0.55)
    # the pull of the wrap: two folds running from her left hip up toward her right shoulder
    c.stroke(INDIGO_DEEP, [(0.090, 0.226), (0.040, 0.262), (0.000, 0.296)], 4.0, 0.7, 0.85, (0.3, 0.8))
    c.stroke(INDIGO_DEEP, [(0.104, 0.250), (0.070, 0.280)], 3.4, 0.7, 0.75, (0.3, 0.8))
    # under the wrap's edge, her right side: the under layer, a step darker
    c.mark(INDIGO_MID, [(-0.110, 0.330), (-0.060, 0.330), (0.060, 0.212), (-0.110, 0.212)], 2, 0.75, curved=False)


def paint_torso_back(c):
    c.mark(INDIGO_LIT, [(-0.090, 0.300), (0.090, 0.300), (0.084, 0.250), (-0.086, 0.248)], 9, 0.5)
    # three woven diamonds across the shoulders, white on the indigo, each its own size
    c.mark(COTTON, [(-0.064, 0.292), (-0.048, 0.276), (-0.064, 0.260), (-0.080, 0.276)], 0.3, 0.95, curved=False)
    c.mark(COTTON, [(0.000, 0.298), (0.020, 0.276), (0.000, 0.254), (-0.020, 0.276)], 0.3, 0.95, curved=False)
    c.mark(INDIGO, [(0.000, 0.286), (0.009, 0.276), (0.000, 0.266), (-0.009, 0.276)], 0.3, 1.0, curved=False)
    c.mark(COTTON, [(0.064, 0.290), (0.079, 0.276), (0.064, 0.262), (0.049, 0.276)], 0.3, 0.95, curved=False)
    # a thin madder line under them
    c.stroke(MADDER, [(-0.090, 0.244), (0.090, 0.244)], 3.0, 0.3, 0.9, (0.9, 0.9), curved=False)


def _torso_side(c):
    c.stroke(INDIGO_DEEP, [(0.002, 0.270), (0.003, 0.214)], 2.4, 0.4, 0.9, (0.6, 0.8), curved=False)


# ---------------------------------------------------------------------------
# THE ARMS. A short cap sleeve of the indigo (its white edge is geometry), then bare skin and her
# fist. Everything past the sleeve is skin.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    if view in ("front", "back"):
        for z in (0.3070, 0.2880, 0.2690):
            c.stroke(SKIN_DEEP, [(s * 0.2640, z), (s * 0.2815, z)], 2.2, 0.3, 0.85, (1.0, 0.5), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.2540, 0.3210), (s * 0.2660, 0.3230), (s * 0.2730, 0.3290)], 2.2, 0.3, 0.85, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.256, -0.024), (s * 0.280, -0.024), (s * 0.280, 0.034), (s * 0.256, 0.034)], 4, 0.5)


def paint_arm(c, s, view):
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    col(INDIGO, 0.08, SLEEVE_END + 0.004)
    if view in ("front", "back"):
        c.mark(INDIGO_LIT, [(x(0.106), 0.326), (x(0.128), 0.330), (x(0.128), 0.306), (x(0.106), 0.304)], 4, 0.55)
    col(SKIN, SLEEVE_END + 0.004, 0.33)
    if view in ("front", "back"):
        c.mark(SKIN_LIT, [(x(0.150), 0.318), (x(0.176), 0.320), (x(0.176), 0.304), (x(0.150), 0.302)], 5, 0.35)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# HER FEET, from above: the toe step's level top. Four short toe lines on each, drawn by hand,
# each its own length, so a bare foot reads as a foot and not a block of skin.
# ---------------------------------------------------------------------------

def paint_feet(c):
    for s in (1, -1):
        cx = s * 0.0836
        # the big toe on the inside, then three smaller
        for dx, y0, y1 in ((-0.016, -0.118, -0.140), (0.002, -0.120, -0.136), (0.016, -0.120, -0.134), (0.028, -0.118, -0.130)):
            px = cx + s * dx
            c.stroke(SKIN_DEEP, [(px, y0), (px, y1)], 2.0, 0.3, 0.75, (0.8, 0.6), curved=False)


def build(size=ATLAS):
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}
    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    c = I("torso.front", INDIGO); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", INDIGO); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", INDIGO); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", INDIGO); _torso_side(c); done[c.name] = c
    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    c = I("foot.top", SKIN); paint_feet(c); done[c.name] = c
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
    print("wrote %s  (%d x %d, %d islands)" % (out, size, size, len(islands)))
    if "--sheet" in sys.argv:
        sheet = sys.argv[sys.argv.index("--sheet") + 1]
        atlas.crop((0, 0, size, size // 2)).save(sheet)
        print("wrote %s" % sheet)


if __name__ == "__main__":
    main()
