"""Paint the atlas of the Phaister (Soraya) redesign PROTOTYPE, by hand, in the house style.

    py -3 tools/author_character_redesign_phaister_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/phaister/phaister-redesign-atlas.png. The model is
built by tools/author_character_redesign_phaister.py, which imports this file for the LAYOUT only
(PIL is imported inside the painting calls, Blender's Python has none) and so maps every face
onto the island painted for it here. Paint first, then build.

WHY. Owner, 2026-10-05: Phaister was "finalized and protected"; he lifted that for a PROTOTYPE
only. This file is a copy of Amihan's textures script (itself a copy of Dante's) rewritten for
her (docs/CHARACTER_REDESIGN_DANTE.md section 13: copies, never an edit of another hero's).
Nothing in the game loads these files and team-phaister.glb is not touched.

THE RULES SHE INHERITS (section 15.3 of that document):
  * the face is drawn for the flat front of the head: flat skin, the fringe's shadow as one
    tone, a round blush under each eye (she is a girl), her eyes and her mouth. No nose, no
    sockets, creases, lids, lip shadow or contour;
  * HER EYES AND MOUTH ARE MEASURED, not redrawn (see EYE_LEFT below);
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points;
  * PAINT STOPS AT ITS OWN PIECE'S EDGES. Every band below is cut to the piece that wears it;
  * A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW. The model script sends every chamfer
    and angled face to a FLAT tone (`proj_square` there), so nothing drawn here is ever smeared
    down a slope. That is why every island below keeps its marks away from the piece's edges;
  * a sewn or woven mark is hard edged with a rim (her moon and stars, her sleeve crosses).

HER COLOURS ARE THE ORIGINAL'S 16 (person_phaister.asset, tools/build_phaister_voxel.py):
coat black 181622, cloth purple 4a1e78, lilac gem 9838d8, gold f8b824 and its shadow b87814,
wand wood 7c3c20, wand wrap b83424, hair magenta d8186e and its highlight e82882, ink 14101c,
crimson 8c1424, white ffffff, skin f4c098 and its shadow e0a078 (which is also the manika's
burlap). The painted tones are steps of those.
ROLE HUES (offence orange #f87020, defence blue #0080e8): every large cloth colour is checked
below. Four of her own slots sit near the orange by hue and are kept, each on a small piece
only (rule 10: "off large areas"): the gold's shadow b87814 (the under edges of buckles and
chain links), the wand wood and its wrap (two wands in the hat band), and the burlap of the
manika at her hip, which is her skin's shadow slot. Skin is exempt, as it is for the cast.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "phaister"
ATLAS_NAME = "phaister-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is the original palette slot, unchanged.
# ---------------------------------------------------------------------------
SKIN = "f4c098"; SKIN_LIT = "fad2b2"; SKIN_SHADE = "e0a078"; SKIN_DEEP = "c4845e"
BLUSH = "f08c8c"
HAIR = "d8186e"; HAIR_LIT = "e82882"; HAIR_DARK = "b4125a"; HAIR_DEEP = "8e0d47"
INK = "14101c"
COAT = "181622"; COAT_LIT = "2c2940"; COAT_MID = "211e30"; COAT_DEEP = "0e0d15"
PURPLE = "4a1e78"; PURPLE_LIT = "6535a0"; PURPLE_DARK = "36155a"
LILAC = "9838d8"; LILAC_LIT = "bb74ea"; LILAC_DARK = "7226ac"
GOLD = "f8b824"; GOLD_LIT = "fcd76c"; GOLD_DARK = "b87814"
WOOD = "7c3c20"; WOOD_DARK = "5a2a16"; WRAP = "b83424"
CRIMSON = "8c1424"; CRIMSON_LIT = "ab2a3a"; CRIMSON_DARK = "640c18"
WHITE = "ffffff"; WHITE_SHADE = "dcd7e6"; WHITE_DEEP = "b4adc6"
BURLAP = "e0a078"; BURLAP_DARK = "bc7e56"

# the four small-piece colours named in the docstring are listed apart
CLOTH_HEXES = [HAIR, HAIR_LIT, HAIR_DARK, HAIR_DEEP, COAT, COAT_LIT, COAT_MID, PURPLE, PURPLE_LIT, PURPLE_DARK,
               LILAC, LILAC_LIT, LILAC_DARK, GOLD, GOLD_LIT, CRIMSON, CRIMSON_LIT, CRIMSON_DARK, WHITE, WHITE_SHADE, WHITE_DEEP]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# All read off team-phaister.glb (the numbers in tools/build_phaister_voxel.py are an older arm).
# ---------------------------------------------------------------------------
BELT = (0.160, 0.208)           # her purple belt
HEM = 0.070                     # the coat skirt's foot; its purple band is geometry
SKIRT_TOP = 0.162
BAND = (0.172, 0.207)           # along the arm (x): the purple sleeve band with the gold cross (it caps the sleeve's mouth at 0.180)
RIM = (0.207, 0.215)            # the gold rim
CUFF = (0.215, 0.238)           # the white cuff
WRIST = 0.236                   # where the hand block starts
HAND_END = 0.285
ANKLE = (0.060, 0.074)          # the crimson ankle band (geometry)
SOLE_TOP = 0.024
SHOE_TOP = 0.060
V_NECK = [(-0.047, 0.345), (0.047, 0.345), (0.045, 0.306), (0.0, 0.274), (-0.045, 0.306)]   # the skin in her coat's neck

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y). +x is HER left (the wands, the manika), -y is the way she faces.
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    # only the face is drawn; every other side of the head block is under hair and takes flat skin
    "head": {
        "front": ((-0.20, 0.20, 0.33, 0.67), 1500),
    },
    "torso": {
        "front": ((-0.21, 0.21, 0.06, 0.36), 1100),
        "back": ((-0.21, 0.21, 0.06, 0.36), 700),
        "xpos": ((-0.13, 0.13, 0.06, 0.36), 800),
        "xneg": ((-0.13, 0.13, 0.06, 0.36), 800),
    },
    # the cape's black back, with her moon and stars
    "cape": {
        "back": ((-0.26, 0.26, 0.02, 0.36), 1150),
    },
    "armL": {
        "front": ((0.09, 0.30, 0.19, 0.385), 1100),
        "back": ((0.09, 0.30, 0.19, 0.385), 900),
        "top": ((0.09, 0.30, -0.10, 0.12), 900),
        "bottom": ((0.09, 0.30, -0.10, 0.12), 700),
    },
    "armR": {
        "front": ((-0.30, -0.09, 0.19, 0.385), 1100),
        "back": ((-0.30, -0.09, 0.19, 0.385), 900),
        "top": ((-0.30, -0.09, -0.10, 0.12), 900),
        "bottom": ((-0.30, -0.09, -0.10, 0.12), 700),
    },
    "legL": {
        "front": ((0.0, 0.17, 0.0, 0.19), 1000),
        "back": ((0.0, 0.17, 0.0, 0.19), 800),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 950),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 800),
        "top": ((0.0, 0.17, -0.16, 0.11), 950),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.19), 1000),
        "back": ((-0.17, 0.0, 0.0, 0.19), 800),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 800),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 950),
        "top": ((-0.17, 0.0, -0.16, 0.11), 950),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group.
VIEW_BIAS = {}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). u across, v UP.
SWATCHES = {
    "ribbon": (256, 100, 0.30, 0.075),      # the hat band: u round the hat, v up it
    "trim": (256, 44, 0.20, 0.02),          # purple piping on the skirt and the cape
    "buckle": (260, 140, 0.124, 0.066),     # the gold buckle, on the hat and on the belt
    "collar": (220, 170, 0.100, 0.076),     # one crimson collar wing
    "manika": (200, 150, 0.048, 0.036),     # the rag doll's face
    "manika_body": (150, 100, 0.036, 0.024),
}

# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    "hair_lit": HAIR_LIT, "hair": HAIR, "hair_dark": HAIR_DARK, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE,
    "coat_lit": COAT_LIT, "coat_mid": COAT_MID, "coat": COAT, "coat_deep": COAT_DEEP,
    "purple_lit": PURPLE_LIT, "purple": PURPLE, "purple_dark": PURPLE_DARK,
    "lilac_lit": LILAC_LIT, "lilac": LILAC, "lilac_dark": LILAC_DARK,
    "gold_lit": GOLD_LIT, "gold": GOLD, "gold_dark": GOLD_DARK,
    "wood": WOOD, "wood_dark": WOOD_DARK, "wrap": WRAP,
    "crimson_lit": CRIMSON_LIT, "crimson": CRIMSON, "crimson_dark": CRIMSON_DARK,
    "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "burlap": BURLAP, "burlap_dark": BURLAP_DARK, "ink": INK,
}
FLAT_PX = 40

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin. `head.front`, `gold`, and so on.

    A shelf pack into the top half, tallest first. Densities shrink together, two per cent at a
    time, until every island fits, so adding an island never needs a rect retyped by hand.
    """
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
    """The metres an island spans: (a0, a1, b0, b1). A swatch spans (0, 1, 0, 1)."""
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


def mix(a, b, t):
    ra, rb = _rgb(a), _rgb(b)
    return "%02x%02x%02x" % tuple(int(round(ra[i] + (rb[i] - ra[i]) * t)) for i in range(3))


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
# THE BRUSH. One island at a time, every length in metres (feathers and widths in MILLIMETRES).
# ---------------------------------------------------------------------------

def _spline(points, closed=False, steps=10):
    """A Catmull-Rom curve through hand-set points, so a patch has a drawn edge, not corners."""
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
        self.mm = 0.5 * (self.w / span_a + self.h / span_b) / 1000.0   # px per millimetre
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
        """A filled patch. `points` are hand-set, in metres; the edge is feathered `feather` mm."""
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
        """A brush stroke `width` mm wide through hand-set points, thinning to `taper` at its ends."""
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
        """A row of hand stitches along a line: short dashes `dash` mm long, `gap` mm apart."""
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
        """Everything between two heights, the whole island wide: how a belt meets itself."""
        a0, a1 = self.win[0], self.win[1]
        pad = (a1 - a0)
        return self.mark(colour, [(a0 - pad, b0), (a1 + pad, b0), (a1 + pad, b1), (a0 - pad, b1)],
                         feather, strength, curved=False)

    def column(self, colour, a0, a1, feather=0.0, strength=1.0):
        """Everything between two positions along the first axis: a cuff round an arm."""
        b0, b1 = self.win[2], self.win[3]
        pad = (b1 - b0)
        return self.mark(colour, [(a0, b0 - pad), (a1, b0 - pad), (a1, b1 + pad), (a0, b1 + pad)],
                         feather, strength, curved=False)

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE HEAD. Her face on the flat front of the block.
# ---------------------------------------------------------------------------
# HER FACE INK, MEASURED, NOT REDRAWN (rule 12: "the eye's outline is the expression"). These are
# the slot 8 polygons of team-phaister.glb's head mesh, vertex for vertex, (x, z), read with
# tools/build_phaister_voxel.py's own reader on 2026-10-05. Each eye is 8 vertices in 6 triangles.
# WHAT THE OUTLINE SAYS: she has no eyebrows and her lids are HALF CLOSED. The top edge is one
# flat line (z 0.4974) that runs on past the eye to a point at the OUTER corner (|x| 0.118), a
# cat's flick; the inner end of the top is cut down 4.6 mm; the bottom is two shallow lobes with
# a notch between them (0.470 between two points at 0.4654). The eye is 74 mm wide and only
# 32 mm tall: sleepy and pleased with herself. Left and right were measured separately; in the
# file they are exact mirrors, and both lists are kept as read.
EYE_LEFT = [(0.0439, 0.4791), (0.0510, 0.4654), (0.0721, 0.4700), (0.0932, 0.4654), (0.1003, 0.4791), (0.1180, 0.4974),
            (0.0721, 0.4974), (0.0510, 0.4928)]
EYE_RIGHT = [(-0.0439, 0.4791), (-0.0510, 0.4654), (-0.0721, 0.4700), (-0.0932, 0.4654), (-0.1003, 0.4791), (-0.1180, 0.4974),
             (-0.0721, 0.4974), (-0.0510, 0.4928)]
EYE_CENTRE = (0.0810, 0.4814)    # the middle of the left eye's bounds; the right is at -x
EYE_GROW = 1.15                  # the figure the owner settled on for the girls (Cheska, Amihan)
# HER MOUTH is one thin slanted bar in the original, 60 mm wide, built from 13 stations: it rises
# 7 mm from her right end to her left, thickens from 3.0 mm to 5.6 mm the same way, and its left
# end hooks up. A smirk. These are the middles of that bar at five of its stations, (x, z), and
# the thickness there; the stroke below runs through them.
MOUTH_MID = [(-0.030, 0.4100), (-0.015, 0.4110), (0.000, 0.4120), (0.015, 0.4135), (0.0225, 0.4149), (0.030, 0.4170)]
MOUTH_THICK = (0.0030, 0.0056)   # at her right end, at her left end
MOUTH_CENTRE = (0.0, 0.4135)
MOUTH_GROW = 1.12                # with the eyes, a little less


def _about(centre, k, points):
    return [(centre[0] + (x - centre[0]) * k, centre[1] + (z - centre[1]) * k) for x, z in points]


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (rule 12): flat skin with one very soft lit patch, the fringe's
    # shadow as ONE flat tone, a round blush under each eye, her two measured eyes, her mouth as
    # one stroke. No nose, no sockets, creases, lids, lip shadow or contour, nothing dark below
    # the eyes.
    c.blob(SKIN_LIT, (0.0, 0.445), 0.110, 0.070, 26, 0.30)
    # the fringe's cast shadow, 7 mm deep under each lock's own foot (the locks in the model
    # script: two steps to 0.512, two to 0.532, the notch lock between her eyes to 0.474). It
    # stops 5 mm short of the eyes so it cannot read as a lid.
    c.mark(SKIN_SHADE, [(-0.20, 0.70), (0.20, 0.70), (0.20, 0.505), (0.070, 0.505), (0.069, 0.525), (0.047, 0.525),
                        (0.040, 0.500), (0.031, 0.467), (0.005, 0.467), (-0.004, 0.500), (-0.010, 0.525),
                        (-0.070, 0.525), (-0.071, 0.505), (-0.20, 0.505)], 1.2, 0.62, curved=False)
    # her cheeks: a round blush under each eye, the one soft thing on the face (girls only)
    c.blob(BLUSH, (-0.098, 0.436), 0.029, 0.019, 7, 0.52)
    c.blob(BLUSH, (0.098, 0.436), 0.029, 0.019, 7, 0.52)
    # the eyes: the measured outlines, each grown about its own middle, one flat fill of ink
    c.mark(INK, _about(EYE_CENTRE, EYE_GROW, EYE_LEFT), 0.25, 1.0, curved=False)
    c.mark(INK, _about((-EYE_CENTRE[0], EYE_CENTRE[1]), EYE_GROW, EYE_RIGHT), 0.25, 1.0, curved=False)
    # THE MOUTH IS ONE SMOOTH CURVED STROKE through the measured middles: thin at her right end,
    # full at her left, the left end hooked up. A round dot closes the thick end as a brush would.
    line = _about(MOUTH_CENTRE, MOUTH_GROW, MOUTH_MID)
    width = 1000.0 * MOUTH_THICK[1] * MOUTH_GROW
    c.stroke(INK, line, width, 0.25, 1.0, (MOUTH_THICK[0] / MOUTH_THICK[1], 0.96))
    c.blob(INK, line[-1], 0.0022, 0.0022, 0.2, 1.0)


# ---------------------------------------------------------------------------
# THE COAT. Black (her base garment, CAST_CLOTHING_STYLE.md rule 1), a V of skin at the neck, the
# purple belt, a skirt that flares to a purple band. The collar, the chain, the pendant, the
# buckle and the skirt's piping are geometry; what is drawn here is the black cloth itself.
# Every mark keeps clear of the block's chamfers (14 mm): those take a flat tone.
# ---------------------------------------------------------------------------

def _belt(c):
    c.band(PURPLE, *BELT)
    c.band(PURPLE_LIT, BELT[1] - 0.0080, BELT[1] - 0.0055, 0.3, 0.9)
    c.band(PURPLE_DARK, BELT[0], BELT[0] + 0.0050, 0.4, 0.9)


def _belt_stitch(c, a0, a1):
    c.stitch(PURPLE_DARK, [(a0, BELT[1] - 0.0125), (a1, BELT[1] - 0.0125)], 1.5, 5.0, 4.0)
    c.stitch(PURPLE_DARK, [(a0, BELT[0] + 0.0100), (a1, BELT[0] + 0.0100)], 1.5, 5.0, 4.0)


def paint_torso_front(c):
    c.mark(COAT_LIT, [(-0.112, 0.268), (-0.044, 0.262), (-0.040, 0.224), (-0.110, 0.220)], 8, 0.8)
    c.mark(COAT_LIT, [(0.114, 0.266), (0.048, 0.264), (0.042, 0.226), (0.112, 0.222)], 8, 0.75)
    # the coat closes down the middle: one tonal edge from the point of the neck to the belt
    c.stroke(COAT_LIT, [(0.002, 0.270), (0.003, 0.240), (0.002, 0.212)], 2.4, 0.3, 1.0, (0.8, 0.8))
    c.stitch(COAT_LIT, [(0.011, 0.262), (0.012, 0.214)], 1.4, 4.5, 3.5, 0.8)
    # the V of skin in the neck, hard edged, her chin's shade across the top of it, and the
    # coat's turned edge round it
    c.mark(COAT_LIT, _about((0.0, 0.320), 1.14, V_NECK), 0.3, 1.0, curved=False)
    c.mark(SKIN, V_NECK, 0.25, 1.0, curved=False)
    # (her chin's shade across the top of the V is a block of its own, `throat` in the model script)
    _belt(c)
    _belt_stitch(c, -0.150, 0.150)
    # the skirt: lit down the middle of each panel, folds fanning from the belt to the hem
    c.mark(COAT_LIT, [(-0.118, 0.152), (-0.058, 0.152), (-0.064, 0.100), (-0.130, 0.098)], 8, 0.75)
    c.mark(COAT_LIT, [(0.062, 0.152), (0.116, 0.152), (0.132, 0.100), (0.068, 0.100)], 8, 0.7)
    c.stroke(COAT_DEEP, [(-0.088, 0.158), (-0.098, 0.128), (-0.106, 0.094)], 5.5, 0.8, 0.95, (0.3, 0.8))
    c.stroke(COAT_DEEP, [(0.046, 0.158), (0.050, 0.126), (0.052, 0.096)], 4.5, 0.8, 0.9, (0.3, 0.8))
    c.stroke(COAT_DEEP, [(-0.044, 0.158), (-0.047, 0.130), (-0.048, 0.098)], 4.0, 0.8, 0.9, (0.3, 0.8))
    c.stroke(COAT_DEEP, [(0.128, 0.156), (0.138, 0.126), (0.146, 0.098)], 5.0, 0.8, 0.9, (0.3, 0.8))
    c.stitch(PURPLE, [(-0.160, 0.0935), (-0.040, 0.0935)], 1.4, 4.5, 3.5)
    c.stitch(PURPLE, [(0.040, 0.0935), (0.160, 0.0935)], 1.4, 4.5, 3.5)


def paint_torso_back(c):
    # under the cape: plain, with the belt carried round
    c.mark(COAT_LIT, [(-0.090, 0.300), (0.090, 0.300), (0.084, 0.240), (-0.086, 0.238)], 8, 0.6)
    _belt(c)
    _belt_stitch(c, -0.150, 0.150)
    c.stroke(COAT_DEEP, [(0.0, 0.158), (0.001, 0.100)], 3.0, 0.5, 0.9, (0.6, 1.0), curved=False)


def _torso_side(c):
    c.stroke(COAT_DEEP, [(0.004, 0.300), (0.003, 0.214)], 2.6, 0.5, 0.9, (0.6, 0.8), curved=False)
    _belt(c)
    _belt_stitch(c, -0.080, 0.080)
    c.mark(COAT_LIT, [(-0.050, 0.152), (0.030, 0.152), (0.036, 0.100), (-0.058, 0.100)], 8, 0.65)
    c.stroke(COAT_DEEP, [(0.040, 0.158), (0.046, 0.126), (0.050, 0.096)], 4.5, 0.8, 0.9, (0.3, 0.8))
    c.stroke(COAT_DEEP, [(-0.058, 0.158), (-0.066, 0.128), (-0.072, 0.096)], 4.5, 0.8, 0.9, (0.3, 0.8))
    c.stitch(PURPLE, [(-0.10, 0.0935), (0.10, 0.0935)], 1.4, 4.5, 3.5)


# ---------------------------------------------------------------------------
# THE CAPE'S BACK. Black, falling in two swallowtails, with HER MOON AND TWO STARS in gold. In the
# original those are boxes 10 mm proud: a fat C of three bars and two plus signs. Here they are
# sewn on, so they are drawn hard edged with a darker rim (rule 5), the moon as a true crescent
# of the same height and place (72 mm tall, its horns to her left), the stars as four points
# of the plus signs' own width and height (54 by 33 mm).
# ---------------------------------------------------------------------------
MOON = ((0.006, 0.226), 0.036, (0.025, 0.226), 0.0275)   # outer centre and radius, the bite's centre and radius
STARS = ((-0.109, 0.2115), (0.109, 0.2115))


def _crescent(centre, radius, bite, bite_radius, grow=0.0, steps=40):
    """The outline of a circle with a bite out of its +x side, as a polygon. `grow` fattens it."""
    R, r = radius + grow, bite_radius - grow
    d = bite[0] - centre[0]
    alpha = math.acos((d * d + R * R - r * r) / (2.0 * d * R))
    beta = math.acos((R * R - d * d - r * r) / (2.0 * d * r))
    pts = []
    for k in range(steps + 1):
        t = alpha + (2.0 * math.pi - 2.0 * alpha) * k / steps
        pts.append((centre[0] + R * math.cos(t), centre[1] + R * math.sin(t)))
    for k in range(steps + 1):
        t = (2.0 * math.pi - beta) - (2.0 * math.pi - 2.0 * beta) * k / steps
        pts.append((bite[0] + r * math.cos(t), bite[1] + r * math.sin(t)))
    return pts


def _star(at, k=1.0):
    x, z = at
    return [(x, z + 0.0190 * k), (x + 0.0058 * k, z + 0.0052 * k), (x + 0.0270 * k, z), (x + 0.0058 * k, z - 0.0052 * k),
            (x, z - 0.0190 * k), (x - 0.0058 * k, z - 0.0052 * k), (x - 0.0270 * k, z), (x - 0.0058 * k, z + 0.0052 * k)]


def paint_cape_back(c):
    c.mark(COAT_LIT, [(-0.150, 0.300), (-0.050, 0.304), (-0.060, 0.170), (-0.170, 0.150)], 10, 0.6)
    c.mark(COAT_LIT, [(0.050, 0.304), (0.150, 0.300), (0.172, 0.150), (0.062, 0.170)], 10, 0.55)
    # folds: the cloth hangs from the shoulders and each tail carries its own
    c.stroke(COAT_DEEP, [(0.0, 0.330), (0.001, 0.280)], 2.6, 0.5, 0.9, (0.5, 0.9), curved=False)
    c.stroke(COAT_DEEP, [(-0.062, 0.186), (-0.070, 0.160), (-0.076, 0.134)], 5.0, 0.8, 0.9, (0.3, 0.8))
    c.stroke(COAT_DEEP, [(0.060, 0.184), (0.070, 0.158), (0.078, 0.136)], 5.0, 0.8, 0.9, (0.3, 0.8))
    c.stroke(COAT_DEEP, [(-0.150, 0.300), (-0.160, 0.200), (-0.176, 0.090)], 6.5, 0.9, 0.9, (0.3, 0.8))
    c.stroke(COAT_DEEP, [(0.152, 0.296), (0.164, 0.196), (0.180, 0.092)], 6.5, 0.9, 0.9, (0.3, 0.8))
    c.stroke(COAT_DEEP, [(-0.120, 0.170), (-0.128, 0.140), (-0.134, 0.108)], 4.5, 0.8, 0.85, (0.3, 0.8))
    c.stroke(COAT_DEEP, [(0.118, 0.170), (0.128, 0.140), (0.136, 0.110)], 4.5, 0.8, 0.85, (0.3, 0.8))
    # the moon: a rim, then the gold, both hard
    c.mark(GOLD_DARK, _crescent(*MOON, grow=0.0026), 0.2, 1.0, curved=False)
    c.mark(GOLD, _crescent(*MOON), 0.2, 1.0, curved=False)
    for at in STARS:
        c.mark(GOLD_DARK, _star(at, 1.16), 0.2, 1.0, curved=False)
        c.mark(GOLD, _star(at), 0.2, 1.0, curved=False)


# ---------------------------------------------------------------------------
# THE ARMS. A black sleeve that widens to a purple band, a gold rim and a white cuff, then her
# hand. The gold cross is on the FRONT of the band only, as the original's is (its second cross
# sits on the band's end face, inside the rim, and has never been visible).
# ---------------------------------------------------------------------------
CROSS_Z = 0.285


def _cross(c, s, grow=0.0):
    x = s * 0.5 * (BAND[0] + BAND[1])
    a, b = 0.0046 + grow, 0.0400 + grow        # the upright: 9 by 80 mm
    e, f = 0.0108 + grow, 0.0118 + grow        # the bar: 22 by 24 mm, a third of the way up
    zb = CROSS_Z + 0.006
    return [(x - a, CROSS_Z + b), (x + a, CROSS_Z + b), (x + a, zb + f), (x + e, zb + f), (x + e, zb - f), (x + a, zb - f),
            (x + a, CROSS_Z - b), (x - a, CROSS_Z - b), (x - a, zb - f), (x - e, zb - f), (x - e, zb + f), (x - a, zb + f)]


def _hand(c, s, view):
    # the block's flat faces only: x 0.250 to 0.271 along the arm, z 0.240 to 0.336
    if view in ("front", "back"):
        # (a shade across the lower third of the fist was drawn here; on the one flat face that
        # takes a drawing it showed as a small dark square, v02. The chamfer under it is the shade.)
        for z in (0.3050, 0.2870, 0.2690):
            c.stroke(SKIN_DEEP, [(s * 0.2565, z), (s * 0.2700, z)], 2.2, 0.3, 0.9, (1.0, 0.5), curved=False)
        # the thumb, folded along the top of the fist: one drawn hook
        c.stroke(SKIN_DEEP, [(s * 0.2500, 0.3230), (s * 0.2610, 0.3250), (s * 0.2670, 0.3320)], 2.2, 0.3, 0.9, (0.8, 0.4))
    elif view == "bottom":
        c.column(SKIN_SHADE, *sorted((s * WRIST, s * 0.31)), 0, 0.75)
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.252, -0.022), (s * 0.270, -0.022), (s * 0.270, 0.022), (s * 0.252, 0.022)], 4, 0.6)


def paint_arm(c, s, view):
    """`s` is +1 for her left arm. Each mark is cut to its own piece along the arm (x)."""
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    # the sleeve
    col(COAT, 0.08, BAND[0])
    if view in ("front", "back"):
        c.mark(COAT_DEEP, [(x(0.08), 0.18), (x(BAND[0]), 0.18), (x(BAND[0]), 0.262), (x(0.08), 0.268)], 0, 0.8, curved=False)
        c.mark(COAT_LIT, [(x(0.120), 0.326), (x(0.166), 0.340), (x(0.166), 0.306), (x(0.120), 0.302)], 5, 0.75)
        c.stroke(COAT_DEEP, [(x(0.128), 0.322), (x(0.138), 0.290), (x(0.134), 0.258)], 3.6, 0.6, 0.9)
        c.stroke(COAT_DEEP, [(x(0.156), 0.336), (x(0.162), 0.296), (x(0.158), 0.250)], 3.0, 0.6, 0.85)
    elif view == "top":
        c.mark(COAT_LIT, [(x(0.112), -0.030), (x(0.168), -0.044), (x(0.168), 0.054), (x(0.112), 0.040)], 6, 0.75)
    elif view == "bottom":
        col(COAT_DEEP, 0.08, BAND[0], 0, 0.8)
    # the band: purple, a pale line along its near edge
    col(PURPLE, *BAND)
    col(PURPLE_LIT, BAND[0] + 0.0030, BAND[0] + 0.0050, 0.3, 0.8)
    # the rim: gold, a bright line along it
    col(GOLD, *RIM)
    col(GOLD_LIT, RIM[0] + 0.0022, RIM[0] + 0.0044, 0.3, 0.8)
    # the cuff: white, one fold line along it
    col(WHITE, CUFF[0], CUFF[1])
    col(WHITE_SHADE, CUFF[0] + 0.0095, CUFF[0] + 0.0112, 0.3, 0.9)
    if view in ("front", "back"):
        c.mark(PURPLE_DARK, [(x(BAND[0]), 0.18), (x(BAND[1]), 0.18), (x(BAND[1]), 0.240), (x(BAND[0]), 0.240)], 0, 0.8, curved=False)
        c.mark(GOLD_DARK, [(x(RIM[0]), 0.18), (x(RIM[1]), 0.18), (x(RIM[1]), 0.238), (x(RIM[0]), 0.238)], 0, 0.6, curved=False)
        c.mark(WHITE_SHADE, [(x(CUFF[0]), 0.18), (x(CUFF[1]), 0.18), (x(CUFF[1]), 0.236), (x(CUFF[0]), 0.236)], 0, 0.9, curved=False)
    elif view == "bottom":
        col(PURPLE_DARK, BAND[0], BAND[1], 0, 0.8)
        col(GOLD_DARK, RIM[0], RIM[1], 0, 0.6)
        col(WHITE_SHADE, CUFF[0], CUFF[1], 0, 0.9)
    if view == "front":
        # her cross, sewn on the band: a rim, then the gold, both hard
        c.mark(GOLD_DARK, _cross(c, s, 0.0016), 0.2, 1.0, curved=False)
        c.mark(GOLD, _cross(c, s), 0.2, 1.0, curved=False)
    # everything past the cuff is skin; the hand is drawn last so no cloth paint is left on it
    col(SKIN, CUFF[1] + 0.0005, 0.31)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Black trousers, wide, with a turned cuff; a crimson ankle band and a white sole that
# are their own blocks; a purple shoe between them.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for her left leg, -1 for her right. Each leg's marks are its own."""
    x = lambda v: s * v
    # the shoe first, then the trousers above it
    c.band(PURPLE, 0.0, 0.08)
    if view == "front":
        # the toe cap: one seam across the toe, the cap a little paler
        c.mark(PURPLE_LIT, [(x(0.034), 0.046), (x(0.134), 0.046), (x(0.130), 0.030), (x(0.038), 0.030)], 3, 0.6)
    elif view == "back":
        # the heel tab: a darker strip up the back of the shoe
        c.mark(PURPLE_DARK, [(x(0.070), 0.058), (x(0.098), 0.058), (x(0.098), 0.026), (x(0.070), 0.026)], 0.3, 1.0, curved=False)
    else:
        outer = (view == "xpos") == (s > 0)
        # the side of the shoe: a seam curving from the ankle to the sole, a paler toe
        c.stroke(PURPLE_DARK, [(-0.052, 0.058), (-0.064, 0.044), (-0.084, 0.034), (-0.108, 0.030)], 2.6, 0.3, 1.0, (0.9, 0.5))
        c.stroke(PURPLE_DARK, [(0.040, 0.058), (0.046, 0.042), (0.044, 0.028)], 2.6, 0.3, 1.0, (0.9, 0.6))
        c.mark(PURPLE_LIT if outer else PURPLE_DARK, [(-0.040, 0.054), (0.030, 0.054), (0.034, 0.034), (-0.052, 0.034)], 4, 0.55)
    c.band(PURPLE_DARK, SOLE_TOP, SOLE_TOP + 0.0045, 0.5, 0.9)
    # the trousers
    c.band(COAT, ANKLE[0], 0.20)
    if view == "front":
        c.mark(COAT_LIT, [(x(0.052), 0.160), (x(0.112), 0.160), (x(0.114), 0.116), (x(0.054), 0.114)], 6, 0.7)
        # the pressed crease down the front of each leg, each its own line
        if s > 0:
            c.stroke(COAT_DEEP, [(0.086, 0.166), (0.085, 0.130), (0.087, 0.108)], 2.6, 0.4, 0.95, (0.5, 0.9))
        else:
            c.stroke(COAT_DEEP, [(-0.082, 0.166), (-0.084, 0.132), (-0.083, 0.108)], 2.6, 0.4, 0.95, (0.5, 0.9))
    elif view == "back":
        c.stroke(COAT_DEEP, [(x(0.084), 0.166), (x(0.085), 0.108)], 2.6, 0.4, 0.9, (0.5, 0.9), curved=False)
    else:
        c.mark(COAT_LIT, [(-0.040, 0.158), (0.030, 0.158), (0.032, 0.118), (-0.044, 0.116)], 6, 0.5)
    # the turned cuff of the trouser leg: a pale edge with its shadow under it
    c.band(COAT_LIT, 0.1000, 0.1026, 0.3, 0.9)
    c.band(COAT_DEEP, 0.0966, 0.1000, 0.4, 0.9)


def paint_leg_top(c, s):
    """Looking down on a shoe. The toe is at -y."""
    x = lambda v: s * v
    c.mark(PURPLE_LIT, [(x(0.040), -0.118), (x(0.128), -0.118), (x(0.130), -0.094), (x(0.038), -0.094)], 4, 0.7)
    c.stroke(PURPLE_DARK, [(x(0.022), -0.090), (x(0.084), -0.084), (x(0.146), -0.090)], 2.6, 0.3, 1.0, (0.6, 0.6))


# ---------------------------------------------------------------------------
# THE SWATCHES. u across, v up, both 0 to 1.
# ---------------------------------------------------------------------------

def paint_ribbon(c):
    # the hat band: a ribbon, pale along its upper edge, dark where it meets the brim, one row of
    # stitches low on it
    c.mark(PURPLE_LIT, [(0.0, 0.90), (1.0, 0.90), (1.0, 0.80), (0.0, 0.80)], 0.5, 0.9, curved=False)
    c.mark(PURPLE_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.13), (0.0, 0.13)], 0.8, 0.95, curved=False)
    c.stitch(PURPLE_DARK, [(0.0, 0.27), (1.0, 0.27)], 1.6, 6.0, 5.0)


def paint_trim(c):
    c.mark(PURPLE_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.72), (0.0, 0.72)], 1.0, 0.85, curved=False)
    c.mark(PURPLE_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.22), (0.0, 0.22)], 1.0, 0.9, curved=False)


def paint_buckle(c):
    # her buckle, the same on the hat and on the belt: a gold frame, a dark slot, one prong
    # across it (the original's three boxes, drawn hard)
    c.mark(GOLD_LIT, [(0.04, 0.96), (0.96, 0.96), (0.96, 0.86), (0.04, 0.86)], 0.6, 0.9, curved=False)
    c.mark(GOLD_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.10), (0.0, 0.10)], 0.6, 0.9, curved=False)
    c.mark(GOLD_DARK, [(0.205, 0.175), (0.795, 0.175), (0.795, 0.825), (0.205, 0.825)], 0.2, 1.0, curved=False)
    c.mark(COAT, [(0.225, 0.205), (0.775, 0.205), (0.775, 0.795), (0.225, 0.795)], 0.2, 1.0, curved=False)
    c.mark(GOLD_DARK, [(0.225, 0.385), (0.700, 0.385), (0.700, 0.635), (0.225, 0.635)], 0.2, 1.0, curved=False)
    c.mark(GOLD, [(0.225, 0.415), (0.680, 0.415), (0.680, 0.605), (0.225, 0.605)], 0.2, 1.0, curved=False)


def paint_collar(c):
    # one crimson collar wing, its outer edge at u 1: turned and lit along the top, its inner
    # edge (beside the skin) in shade, one row of stitches following the outer edge
    c.mark(CRIMSON_LIT, [(0.10, 0.94), (0.96, 0.94), (0.92, 0.70), (0.14, 0.76)], 3.0, 0.8)
    c.mark(CRIMSON_DARK, [(0.0, 0.0), (0.16, 0.0), (0.12, 1.0), (0.0, 1.0)], 1.0, 0.9, curved=False)
    c.mark(CRIMSON_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.10), (0.0, 0.10)], 1.0, 0.8, curved=False)
    c.stitch(CRIMSON_DARK, [(0.80, 0.08), (0.84, 0.50), (0.90, 0.90)], 1.4, 4.5, 3.5)


def _cross_stitch(c, at, r, width):
    x, z = at
    c.stroke(INK, [(x - r, z + r * 1.33), (x + r, z - r * 1.33)], width, 0.15, 1.0, (1, 1), curved=False)
    c.stroke(INK, [(x - r, z - r * 1.33), (x + r, z + r * 1.33)], width, 0.15, 1.0, (1, 1), curved=False)


def paint_manika(c):
    # the rag doll's face, the original's: two stitched X eyes of two sizes, a straight stitched
    # mouth. Hard ink on burlap, a darker foot where the head turns under.
    c.mark(BURLAP_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.14), (0.0, 0.14)], 1.0, 0.8, curved=False)
    _cross_stitch(c, (0.690, 0.585), 0.105, 2.2)     # her left of the doll: 10 mm
    _cross_stitch(c, (0.292, 0.610), 0.085, 2.2)     # the smaller one: 8 mm
    c.stroke(INK, [(0.25, 0.21), (0.75, 0.21)], 2.2, 0.15, 1.0, (1, 1), curved=False)
    for u in (0.36, 0.50, 0.64):
        c.stroke(INK, [(u, 0.13), (u, 0.29)], 1.6, 0.15, 1.0, (1, 1), curved=False)


def paint_manika_body(c):
    # the doll's body: one seam up the middle with three cross stitches
    c.mark(BURLAP_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.16), (0.0, 0.16)], 1.0, 0.8, curved=False)
    c.stroke(INK, [(0.50, 0.08), (0.50, 0.92)], 1.8, 0.15, 1.0, (1, 1), curved=False)
    for v in (0.26, 0.50, 0.74):
        c.stroke(INK, [(0.40, v), (0.60, v)], 1.5, 0.15, 1.0, (1, 1), curved=False)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        return I(name, base, SWATCHES[name][2:4])

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c

    c = I("torso.front", COAT); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", COAT); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", COAT); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", COAT); _torso_side(c); done[c.name] = c

    c = I("cape.back", COAT); paint_cape_back(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, COAT)
            if view == "top":
                c.img.paste(_rgb(PURPLE), (0, 0, c.w, c.h))
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c

    c = swatch("ribbon", PURPLE); paint_ribbon(c); done[c.name] = c
    c = swatch("trim", PURPLE); paint_trim(c); done[c.name] = c
    c = swatch("buckle", GOLD); paint_buckle(c); done[c.name] = c
    c = swatch("collar", CRIMSON); paint_collar(c); done[c.name] = c
    c = swatch("manika", BURLAP); paint_manika(c); done[c.name] = c
    c = swatch("manika_body", BURLAP); paint_manika_body(c); done[c.name] = c
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
    # The bottom half is where Toon.shader reads the palette instead of this file. Left one flat tone.
    atlas = Image.new("RGB", (size, size), _rgb(COAT_DEEP))
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
        except OSError:      # Windows sometimes refuses the write while the editor holds the file
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
