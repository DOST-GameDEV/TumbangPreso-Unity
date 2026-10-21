"""Paint the atlas of the Payo audience v2 villager `elder_man`, the village elder (lakay).

    py -3 tools/author_payo_audience_v2_elder_man_textures.py [--size 2048] [--sheet file.png]

Writes ArtSource/payo/audience/v2/elder_man/audience_v2_elder_man-atlas.png. The model is built
by tools/author_payo_audience_v2_elder_man.py, which imports this file for the LAYOUT only (PIL is
imported inside the painting calls, Blender's Python has none). Paint first, then build.

THE BRIEF: docs/reports/character-redesign/payo-audience-brief.md, on top of the Classic brief.
A copy of Mang Kanor's textures script (an approved Classic, a man) rewritten for the elder.

THE FINISH IS THE CLASSICS' FINISH (owner, 2026-10-19, on the rejected v1 crowd: "they look
noisy. they just dont look like the models we have at all"). So: clean flat tones per form, the
base, light and shade steps Mang Kanor's atlas uses, one very soft lit patch on the face as his
has, and NOTHING ELSE soft. No noise, no grain, no brush texture, no jitter. Woven bands and motifs
are clean hand-set polygons with hard edges. Every trim edge, belt band and hem is GEOMETRY in the
builder, not paint here.

WHO HE IS. The lakay of the terrace village: the elder who has seen every harvest, sat on every
council, and now stands on the basin wall leaning on his spear to watch the young play. Dignified,
a little stooped, quiet. His AGE is said by design only: grey hair, the stoop, the blanket an
elder wears over both shoulders, the spear he leans on. No wrinkles, no sockets, no nose, no
blush (he is a man).

HIS COLOURS, QUIET (the brief: a crowd must never steal a hero's hue):
  * an INDIGO-BLACK woven blanket and bahag, the Ifugao ground colour, the darkest cloth he wears;
  * MADDER RED only in thin bands and in the small head cloth, a desaturated crimson that sits
    well clear of the offence orange and of Sean's red-orange;
  * undyed WHITE cotton bands, and white hand-set motifs (diamonds) on the blanket's back;
  * BRASS, his one metal, in the ear ornaments and the spear's binding ring;
  * grey hair; a hardwood shaft and an iron blade for the spear;
  * skin in the heroes' range, a warm brown.
Black ink (1a1420, Dante's) for the eyes and mouth; the batok tattoo routes on his forearms are
black too (CHARACTER_MODEL_METHOD section 3: tattoo ink is black).
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "elder_man"
ATLAS_NAME = "audience_v2_elder_man-atlas.png"
ATLAS = 2048
GUTTER = 4
SS = 2

# ---------------------------------------------------------------------------
# THE COLOURS. Each family is a base, a light, a shade and a deep tone, as the Classics' are.
# ---------------------------------------------------------------------------
SKIN = "a8694a"; SKIN_LIT = "bc7e5c"; SKIN_SHADE = "8c5338"; SKIN_DEEP = "6c3d27"
HAIR = "a9a59e"; HAIR_TOP = "c9c5bd"; HAIR_DEEP = "7f7b75"                 # grey
INK = "1a1420"
TATTOO = "1d1a20"                                                           # black batok ink
INDIGO = "262a3a"; INDIGO_LIT = "353a4f"; INDIGO_DARK = "1a1d29"; INDIGO_DEEP = "121420"
MADDER = "8a3a42"; MADDER_LIT = "a24b52"; MADDER_DARK = "682a31"
COTTON = "e6dfcf"; COTTON_LIT = "f4eee2"; COTTON_SHADE = "c4bcab"
BRASS = "c8963a"; BRASS_LIT = "e2b55c"; BRASS_DARK = "946a24"
WOOD = "5a3a26"; WOOD_LIT = "6c4e3e"; WOOD_DARK = "3e2718"
IRON = "6e7276"; IRON_LIT = "9a9ea2"; IRON_DARK = "4a4d51"
RATTAN = "b58d55"; RATTAN_LIT = "cba56c"; RATTAN_DARK = "8e6b3c"

CLOTH_HEXES = [HAIR, HAIR_TOP, HAIR_DEEP, INDIGO, INDIGO_LIT, INDIGO_DARK, MADDER, MADDER_LIT, MADDER_DARK,
               COTTON, COTTON_SHADE, WOOD, WOOD_LIT, WOOD_DARK, IRON]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES. Blender space: x his left, -y the way he faces, z up.
# ---------------------------------------------------------------------------
ARM_Z = 0.2878                  # the arm bone's height
ELBOW = 0.178
WRIST = 0.232                   # where the fist block starts
HAND_END = 0.300
#   the forearm's batok canvas, along the arm (|x|)
TAT = (0.184, 0.228)
#   the blanket: its back plate and its two front panels (|x| from the inner edge out), in z
BLANKET_BACK = (0.150, 0.352)
BLANKET_BACK_HALF = (0.140, 0.128)      # half width at the hem and at the shoulders
BLANKET_FRONT = (0.196, 0.350)
BLANKET_FRONT_X = (0.046, 0.128)
#   the bahag's two flaps: (top z, bottom z, half width at the top, half width at the foot)
FLAP_FRONT = (0.198, 0.092, 0.034, 0.040)
FLAP_BACK = (0.198, 0.074, 0.038, 0.044)
#   the face
EYE_CENTRE = (0.077, 0.478)
#   the bowl-cut fringe under the head cloth: (middle x, tip z, half width, tip half width)
FRINGE = [(-0.118, 0.590, 0.034, 0.026), (-0.052, 0.584, 0.036, 0.028), (0.016, 0.586, 0.034, 0.026),
          (0.082, 0.582, 0.036, 0.028), (0.140, 0.592, 0.026, 0.020)]
FRINGE_BASE = 0.618

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
    "armL": {
        "front": ((0.09, 0.32, 0.19, 0.385), 1200),
        "back": ((0.09, 0.32, 0.19, 0.385), 1000),
        "top": ((0.09, 0.32, -0.08, 0.115), 1000),
        "bottom": ((0.09, 0.32, -0.08, 0.115), 700),
    },
    "armR": {
        "front": ((-0.32, -0.09, 0.19, 0.385), 1200),
        "back": ((-0.32, -0.09, 0.19, 0.385), 1000),
        "top": ((-0.32, -0.09, -0.08, 0.115), 1000),
        "bottom": ((-0.32, -0.09, -0.08, 0.115), 700),
    },
}
VIEW_BIAS = {}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). u across, v UP.
SWATCHES = {
    "blanket_back": (420, 300, 0.280, 0.202),     # the blanket across his back, x -0.140..0.140, z BLANKET_BACK
    "blanket_front": (150, 260, 0.082, 0.154),    # one front panel, u from the inner edge out
    "flap_front": (130, 170, 0.080, 0.106),       # the bahag's front flap
    "flap_back": (140, 190, 0.088, 0.124),        # the bahag's back flap
}

FLATS = {
    "hair_top": HAIR_TOP, "hair": HAIR, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "indigo_lit": INDIGO_LIT, "indigo": INDIGO, "indigo_dark": INDIGO_DARK, "indigo_deep": INDIGO_DEEP,
    "madder_lit": MADDER_LIT, "madder": MADDER, "madder_dark": MADDER_DARK,
    "cotton_lit": COTTON_LIT, "cotton": COTTON, "cotton_shade": COTTON_SHADE,
    "brass_lit": BRASS_LIT, "brass": BRASS, "brass_dark": BRASS_DARK,
    "wood_lit": WOOD_LIT, "wood": WOOD, "wood_dark": WOOD_DARK,
    "iron_lit": IRON_LIT, "iron": IRON, "iron_dark": IRON_DARK,
    "rattan_lit": RATTAN_LIT, "rattan": RATTAN, "rattan_dark": RATTAN_DARK,
    "tattoo": TATTOO,
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
        """A filled patch, hard edged (a feather of 0.3 mm only antialiases it)."""
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


HARD = 0.3      # mm: the antialiasing edge every hand-set shape gets, and nothing softer


# ---------------------------------------------------------------------------
# THE HEAD. His face on the flat front of the block, drawn the way the heroes' are.
#   EYES: the heroes' upright ink block at Lola Pacing's place (0.077 out, 0.478 up), 50 mm wide.
#   HIS ONE CUT is a LOW, LEVEL LID: the block is only 38 mm tall and its top edge runs nearly
#   flat, dipping 5 mm toward the nose. A man who is watching, steadily, and has seen it all
#   before. Not Bayan's or Dante's scowl (theirs fall 15 mm and more), not Lola's smile.
#   MOUTH: one straight stroke 48 mm wide at the heroes' weight, no curve: calm, neither pleased
#   nor sad. No nose, no wrinkles, no blush, no beard (Cordillera men plucked theirs).
# ---------------------------------------------------------------------------
#   one LEFT eye about its own middle, (dx, dz) in metres, from the inner foot round; mirrored for the right
EYE = [(-0.0250, -0.0190), (-0.0250, 0.0080), (-0.0215, 0.0130), (-0.0120, 0.0160), (0.0150, 0.0185), (0.0215, 0.0175),
       (0.0250, 0.0130), (0.0250, -0.0190)]
MOUTH = [(-0.024, 0.4170), (0.024, 0.4170)]
MOUTH_WIDTH = 7.8


def paint_head_front(c):
    # the Classics' one very soft lit patch on the face, as Mang Kanor's
    c.blob(SKIN_LIT, (0.0, 0.440), 0.115, 0.070, 26, 0.30)
    # the fringe's cast shadow on the brow: ONE flat tone cut to the locks, hard edged
    poly = [(-0.26, 0.72), (0.26, 0.72), (0.26, FRINGE_BASE - 0.006)]
    for x, tip, half, tip_half in reversed(FRINGE):
        poly += [(x + half, FRINGE_BASE - 0.006), (x + tip_half, tip - 0.006), (x - tip_half, tip - 0.006),
                 (x - half, FRINGE_BASE - 0.006)]
    poly.append((-0.26, FRINGE_BASE - 0.006))
    c.mark(SKIN_SHADE, poly, HARD, 0.85, curved=False)
    for s in (1, -1):
        c.mark(INK, [(s * (EYE_CENTRE[0] + dx), EYE_CENTRE[1] + dz) for dx, dz in EYE], HARD, 1.0, curved=False)
    c.mark(INK, [(-0.024, 0.4205), (0.024, 0.4205), (0.024, 0.4135), (-0.024, 0.4135)], HARD, 1.0, curved=False)


# ---------------------------------------------------------------------------
# THE ARMS. Bare skin from the shoulder to the fist. On each forearm a BATOK route, drawn by
# hand, black, in big shapes (nothing under 10 mm; CHARACTER_MODEL_METHOD section 4): each side
# has its own fill, so nothing repeats between them.
#   HIS LEFT forearm: two RAILS round the arm (one near the elbow, one near the wrist), and
#   between them a different fill on each face: on top two chevrons pointing to the hand, on the
#   front a zigzag run, on the back one diamond; the inside of the arm is bare.
#   HIS RIGHT forearm (the spear hand): ONE wide band at the wrist, and from it toward the elbow
#   a ladder on the front (two rungs between two rails), three slanted bars on top (a fern), a
#   solid wedge on the back.
# Each mark below is its own polygon with its own numbers. The fist's finger lines are the
# Classics' (Mang Kanor's `_hand`).
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    if view in ("front", "back"):
        for z in (ARM_Z + 0.0215, ARM_Z, ARM_Z - 0.0215):
            c.mark(SKIN_DEEP, [(s * (HAND_END - 0.040), z - 0.0013), (s * (HAND_END - 0.016), z - 0.0013),
                               (s * (HAND_END - 0.016), z + 0.0013), (s * (HAND_END - 0.040), z + 0.0013)], HARD, 0.95, curved=False)
        c.stroke(SKIN_DEEP, [(s * (WRIST + 0.016), ARM_Z + 0.0270), (s * (WRIST + 0.029), ARM_Z + 0.0290), (s * (WRIST + 0.037), ARM_Z + 0.0375)],
                 2.6, HARD, 0.95, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * (WRIST + 0.018), -0.016), (s * (HAND_END - 0.018), -0.016),
                          (s * (HAND_END - 0.018), 0.050), (s * (WRIST + 0.018), 0.050)], HARD, 0.6, curved=False)


def _quad(c, s, pts):
    c.mark(TATTOO, [(s * x, z) for x, z in pts], HARD, 1.0, curved=False)


def tattoo_left(c, view):
    s = 1
    if view == "bottom":
        return                      # the inside of the forearm is bare
    # the two rails, round the arm on every outer face: elbow rail 11 mm, wrist rail 9 mm
    c.column(TATTOO, 0.1840, 0.1950, HARD)
    c.column(TATTOO, 0.2175, 0.2265, HARD)
    if view == "top":
        # two chevrons pointing to the hand, each its own shape (seen from above: x along the arm, y across)
        _quad(c, s, [(0.1995, -0.018), (0.2065, 0.017), (0.1995, 0.052), (0.1965, 0.052), (0.2030, 0.017), (0.1965, -0.018)])
        _quad(c, s, [(0.2085, -0.022), (0.2160, 0.019), (0.2085, 0.056), (0.2052, 0.056), (0.2122, 0.019), (0.2052, -0.022)])
    elif view == "front":
        # a zigzag run between the rails, three teeth up and two down
        _quad(c, s, [(0.1950, 0.2700), (0.2005, 0.3080), (0.2060, 0.2700), (0.2115, 0.3080), (0.2175, 0.2700),
                     (0.2175, 0.2800), (0.2120, 0.3180), (0.2060, 0.2800), (0.2005, 0.3180), (0.1950, 0.2800)])
    elif view == "back":
        _quad(c, s, [(0.1955, 0.2880), (0.2060, 0.3150), (0.2170, 0.2880), (0.2060, 0.2600)])


def tattoo_right(c, view):
    s = -1
    if view == "bottom":
        return
    # the wrist band, 14 mm, round the arm
    c.column(TATTOO, -0.2280, -0.2140, HARD)
    if view == "front":
        # a ladder: two rails along the arm, two rungs across
        _quad(c, s, [(0.1840, 0.3010), (0.2140, 0.3010), (0.2140, 0.3090), (0.1840, 0.3090)])
        _quad(c, s, [(0.1840, 0.2660), (0.2140, 0.2660), (0.2140, 0.2740), (0.1840, 0.2740)])
        _quad(c, s, [(0.1885, 0.2740), (0.1965, 0.2740), (0.1965, 0.3010), (0.1885, 0.3010)])
        _quad(c, s, [(0.2025, 0.2740), (0.2100, 0.2740), (0.2100, 0.3010), (0.2025, 0.3010)])
    elif view == "top":
        # three slanted bars off the band, a fern, each a different length
        _quad(c, s, [(0.2060, -0.020), (0.2140, -0.020), (0.1990, 0.016), (0.1910, 0.016)])
        _quad(c, s, [(0.2060, 0.004), (0.2140, 0.004), (0.1960, 0.046), (0.1880, 0.046)])
        _quad(c, s, [(0.2060, 0.028), (0.2140, 0.028), (0.2020, 0.056), (0.1940, 0.056)])
    elif view == "back":
        _quad(c, s, [(0.2140, 0.2680), (0.2140, 0.3080), (0.1880, 0.2880)])


def paint_arm(c, s, view):
    if view == "bottom":
        c.column(SKIN_SHADE, -0.40, 0.40, 0.0, 1.0)
    if s > 0:
        tattoo_left(c, view)
    else:
        tattoo_right(c, view)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE BLANKET. Indigo-black, the stripes FOLLOW THE CLOTH: across his back they run level (the
# cloth's length goes across the shoulders), down each front panel they run down. Clean bands,
# hard edges; the diamonds are hand-set, each its own. The hem and the front edges are GEOMETRY
# in the builder (cotton piping with a madder band), so none of that is painted here.
# ---------------------------------------------------------------------------

def paint_blanket_back(c):
    """x -0.140..0.140 mapped to u, z BLANKET_BACK to v."""
    z0, z1 = BLANKET_BACK
    P = lambda x, z: ((x + 0.140) / 0.280, (z - z0) / (z1 - z0))
    def level(colour, a, b):
        c.mark(colour, [P(-0.30, a), P(0.30, a), P(0.30, b), P(-0.30, b)], HARD, 1.0, curved=False)
    # a lit field across the shoulder blades, one flat step
    c.mark(INDIGO_LIT, [P(-0.112, 0.334), P(0.112, 0.334), P(0.106, 0.300), P(-0.106, 0.300)], HARD, 1.0, curved=False)
    # the stripe group across the middle of his back: cotton 9, madder 7, cotton 9, with indigo between
    level(COTTON, 0.2620, 0.2710)
    level(MADDER, 0.2510, 0.2580)
    level(COTTON, 0.2380, 0.2470)
    # a row of four white diamonds under it, hand-set, the middle two a little larger
    c.mark(COTTON, [P(-0.092, 0.2130), P(-0.076, 0.2260), P(-0.060, 0.2130), P(-0.076, 0.2000)], HARD, 1.0, curved=False)
    c.mark(COTTON, [P(-0.040, 0.2130), P(-0.021, 0.2285), P(-0.002, 0.2130), P(-0.021, 0.1975)], HARD, 1.0, curved=False)
    c.mark(COTTON, [P(0.004, 0.2130), P(0.023, 0.2285), P(0.042, 0.2130), P(0.023, 0.1975)], HARD, 1.0, curved=False)
    c.mark(COTTON, [P(0.060, 0.2130), P(0.076, 0.2260), P(0.092, 0.2130), P(0.076, 0.2000)], HARD, 1.0, curved=False)
    # a second, thinner group near the hem: madder 7 between two cotton 6
    level(COTTON, 0.1880, 0.1940)
    level(MADDER, 0.1790, 0.1860)
    level(COTTON, 0.1710, 0.1770)


def paint_blanket_front(c):
    """One front panel: u 0 at the inner edge (by the chest) to 1 at the outer, v up the panel."""
    z0, z1 = BLANKET_FRONT
    P = lambda u_m, z: (u_m / 0.082, (z - z0) / (z1 - z0))
    def down(colour, a, b):
        c.mark(colour, [P(a, z0 - 0.05), P(b, z0 - 0.05), P(b, z1 + 0.05), P(a, z1 + 0.05)], HARD, 1.0, curved=False)
    # the cloth's lit side, toward the inner edge (one flat step)
    down(INDIGO_LIT, 0.030, 0.050)
    # down the panel: a cotton 7 and a madder 6, the same stripes that run level across his back
    down(COTTON, 0.054, 0.061)
    down(MADDER, 0.064, 0.070)


def paint_flap(c, lo, hi, half, end_motif):
    """A bahag flap in metres mapped to the swatch: x -half..half, z lo..hi. Indigo ground, a lit
    centre panel, and the woven END of the strip near the foot: a madder band between cotton, the
    flap's own small motif in cotton above it. The very foot (the hem) is geometry."""
    P = lambda x, z: ((x + half) / (2.0 * half), (z - lo) / (hi - lo))
    def level(colour, a, b):
        c.mark(colour, [P(-0.2, a), P(0.2, a), P(0.2, b), P(-0.2, b)], HARD, 1.0, curved=False)
    c.mark(INDIGO_LIT, [P(-0.016, hi), P(0.016, hi), P(0.016, lo), P(-0.016, lo)], HARD, 1.0, curved=False)
    level(COTTON, lo + 0.026, lo + 0.034)
    level(MADDER, lo + 0.018, lo + 0.026)
    level(COTTON, lo + 0.012, lo + 0.018)
    end_motif(c, P)


def _front_motif(c, P):
    # front flap: one cotton diamond with a madder heart, a hand-set focal on the strip
    c.mark(COTTON, [P(0.0, 0.1480), P(0.0170, 0.1300), P(0.0, 0.1120), P(-0.0170, 0.1300)], HARD, 1.0, curved=False)
    c.mark(MADDER, [P(0.0, 0.1385), P(0.0075, 0.1300), P(0.0, 0.1215), P(-0.0075, 0.1300)], HARD, 1.0, curved=False)


def _back_motif(c, P):
    # back flap: a cotton zigzag across it, two teeth
    c.mark(COTTON, [P(-0.030, 0.1170), P(-0.015, 0.1360), P(0.0, 0.1170), P(0.015, 0.1360), P(0.030, 0.1170),
                    P(0.030, 0.1060), P(0.015, 0.1250), P(0.0, 0.1060), P(-0.015, 0.1250), P(-0.030, 0.1060)], HARD, 1.0, curved=False)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        return I(name, base, SWATCHES[name][2:4])

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    c = swatch("blanket_back", INDIGO); paint_blanket_back(c); done[c.name] = c
    c = swatch("blanket_front", INDIGO); paint_blanket_front(c); done[c.name] = c
    top, foot, h0, h1 = FLAP_FRONT
    c = swatch("flap_front", INDIGO); paint_flap(c, foot, top, max(h0, h1), _front_motif); done[c.name] = c
    top, foot, h0, h1 = FLAP_BACK
    c = swatch("flap_back", INDIGO); paint_flap(c, foot, top, max(h0, h1), _back_motif); done[c.name] = c
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
    for hex_str in CLOTH_HEXES + [MADDER_LIT]:
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
