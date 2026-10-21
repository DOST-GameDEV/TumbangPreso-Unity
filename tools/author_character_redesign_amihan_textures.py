"""Paint the atlas of the Amihan redesign PROTOTYPE, by hand, in the house style.

    py -3 tools/author_character_redesign_amihan_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/amihan/amihan-redesign-atlas.png. The model is
built by tools/author_character_redesign_amihan.py, which imports this file for the LAYOUT only
(PIL is imported inside the painting calls, Blender's Python has none) and so maps every face
onto the island painted for it here. Paint first, then build.

WHY. Owner, 2026-10-05, after Dante's rework: "following dante's rework, redesign the rest of the
characters". This file is a copy of Dante's textures script rewritten for her (section 13 of
docs/CHARACTER_REDESIGN_DANTE.md: copies, never an edit of Dante's or of another hero's).
Nothing in the game loads these files and team-amihan.glb is not touched.

THE RULES SHE INHERITS, each one a thing the owner said about Dante:
  * the head is the game's BOX head, so the face is drawn for the flat front of a carved block:
    it stays FLAT (rule 12): one soft lit patch, the fringe's shadow as one tone, a blush;
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points (the swatches at
    the foot of this file), and nothing is drawn on it;
  * PAINT STOPS AT ITS OWN PIECE'S EDGES: nothing of the cuff on the hand, nothing of the hair on
    the ears. Every band below is cut to the piece that wears it;
  * a drawn mark is hard edged or it reads as a render fault (her kasikus, her sash diamonds).

HER FEATURES ARE THE ORIGINAL'S (tools/build_amihan_voxel.py): two black eyes and a small smile,
no eyebrows, no nose, no white glint (the cast's ink face), the eyes a third bigger. Rule 12: a
CUTE face, not a portrait. Nothing is modelled in paint on it but a round blush under each eye.

THE ONE MECHANISM is the beggar's (tools/build_beggar_voxel.py): `Toon.shader` remaps a UV to a
palette slot only in Unity atlas rows 0 to 7 and samples the texture itself above them, so every
island lives in the TOP half of the file and the bottom half is left flat.

HOW AN ISLAND IS LAID OUT. A body part is seen from up to six sides (front, back, xpos, xneg, top,
bottom) and each side is one island, a flat orthographic view of the part in MODEL METRES. So a
mark is written where it sits on the body, `(x, z)` on a front or back view, `(y, z)` on a side
view, `(x, y)` on a top view. Loose blocks (the sash, gold piping) take a SWATCH instead, and a
piece with no drawing at all takes a FLAT tone.

HER COLOURS ARE THE ORIGINAL'S 16 (person_amihan.asset): capelet teal 2E8C86, its shadow 1F625E
and light 5FC2B5, the dark teal base 17403D, gold E8B64A, rust A8502E, weave dark 3A2A24, cream
F1E4C8 and its shade D8C6A2, hair 3A241C and its lit tone 6A4330, cotton white FBF8F0, skin
D59A6E and B57A52, ink 181418. The painted tones are steps of those.
ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth colour is checked below.
Her RUST is the original's own slot 3 and sits near the orange by hue; it is kept, darker and
duller than the role colour, and only on the belt, the sandal straps and two sash stripes (rule
10: "off large areas"). Skin is exempt, as it is for the cast.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "amihan"
ATLAS_NAME = "amihan-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is the original palette slot, unchanged.
# ---------------------------------------------------------------------------
SKIN = "d59a6e"; SKIN_LIT = "e4b086"; SKIN_SHADE = "b57a52"; SKIN_DEEP = "8f593c"
BLUSH = "d8786a"
HAIR = "3a241c"; HAIR_MID = "4a2f24"; HAIR_DEEP = "26160f"
HAIR_LIT = "6a4330"; HAIR_LIT_TOP = "7c5b47"
INK = "181418"
CAPE = "2e8c86"; CAPE_LIT = "5fc2b5"; CAPE_DARK = "1f625e"; CAPE_MID = "3fa59b"
BASE = "17403d"; BASE_LIT = "21585a"; BASE_DARK = "0f2d2b"; BASE_DEEP = "0a1f1e"
GOLD = "e8b64a"; GOLD_LIT = "f7dc8e"; GOLD_DARK = "ab8a30"
RUST = "a8502e"; RUST_LIT = "c2694a"; RUST_DARK = "7a3620"
WEAVE = "3a2a24"
CREAM = "f1e4c8"; CREAM_LIT = "fbf3e0"; CREAM_SHADE = "d8c6a2"; CREAM_DEEP = "b7a37e"
COTTON = "fbf8f0"; COTTON_SHADE = "ddd6c6"

# rust is listed apart: see the docstring
CLOTH_HEXES = [HAIR, HAIR_MID, HAIR_LIT, HAIR_LIT_TOP, CAPE, CAPE_LIT, CAPE_DARK, CAPE_MID, BASE, BASE_LIT,
               GOLD, GOLD_LIT, GOLD_DARK, WEAVE, CREAM, CREAM_LIT, CREAM_SHADE, CREAM_DEEP, COTTON, COTTON_SHADE]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# ---------------------------------------------------------------------------
BELT_LOW = (0.1995, 0.2125)     # the two tiers of her belt, as the original's (0.262 and 0.283 in its table)
BELT_HIGH = (0.2170, 0.2300)
HEM = 0.090                     # the coat skirt's foot; its gold band is geometry
CUFF = (0.176, 0.196)           # the cream cuff along the arm (x), and the gold edge before it
CUFF_GOLD = (0.166, 0.176)
WRIST = 0.224                   # where the hand block starts
SOLE_TOP = 0.026
SHORTS = (0.108, 0.200)
STRAP = (-0.094, -0.072)        # the sandal strap, along the foot (y)

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y). +x is HER left (the flowers), -y is the way she faces, z is up.
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.26, 0.26, 0.30, 0.70), 1300),
        "back": ((-0.26, 0.26, 0.30, 0.70), 480),
        "xpos": ((-0.20, 0.20, 0.30, 0.70), 760),
        "xneg": ((-0.20, 0.20, 0.30, 0.70), 760),
        "top": ((-0.26, 0.26, -0.20, 0.20), 240),
        "bottom": ((-0.26, 0.26, -0.20, 0.20), 240),
    },
    "torso": {
        "front": ((-0.20, 0.20, 0.07, 0.36), 1000),
        "back": ((-0.20, 0.20, 0.07, 0.36), 1000),
        "xpos": ((-0.15, 0.15, 0.07, 0.36), 850),
        "xneg": ((-0.15, 0.15, 0.07, 0.36), 850),
    },
    # the capelet, her signature layer, is its own part: it lies over the torso's islands
    "cape": {
        "front": ((-0.20, 0.20, 0.23, 0.37), 1150),
        "back": ((-0.20, 0.20, 0.23, 0.37), 1150),
        "xpos": ((-0.15, 0.15, 0.23, 0.37), 900),
        "xneg": ((-0.15, 0.15, 0.23, 0.37), 900),
        "top": ((-0.20, 0.20, -0.15, 0.15), 800),
    },
    "armL": {
        "front": ((0.09, 0.29, 0.20, 0.375), 950),
        "back": ((0.09, 0.29, 0.20, 0.375), 950),
        "top": ((0.09, 0.29, -0.07, 0.10), 950),
        "bottom": ((0.09, 0.29, -0.07, 0.10), 800),
        "xpos": ((-0.07, 0.10, 0.21, 0.37), 700),
    },
    "armR": {
        "front": ((-0.29, -0.09, 0.20, 0.375), 950),
        "back": ((-0.29, -0.09, 0.20, 0.375), 950),
        "top": ((-0.29, -0.09, -0.07, 0.10), 950),
        "bottom": ((-0.29, -0.09, -0.07, 0.10), 800),
        "xneg": ((-0.07, 0.10, 0.21, 0.37), 700),
    },
    "legL": {
        "front": ((0.0, 0.17, 0.0, 0.21), 1000),
        "back": ((0.0, 0.17, 0.0, 0.21), 850),
        "xpos": ((-0.16, 0.11, 0.0, 0.21), 950),
        "xneg": ((-0.16, 0.11, 0.0, 0.21), 800),
        "top": ((0.0, 0.17, -0.16, 0.11), 1000),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.21), 1000),
        "back": ((-0.17, 0.0, 0.0, 0.21), 850),
        "xpos": ((-0.16, 0.11, 0.0, 0.21), 800),
        "xneg": ((-0.16, 0.11, 0.0, 0.21), 950),
        "top": ((-0.17, 0.0, -0.16, 0.11), 1000),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group. The face
# is drawn on the front island, so the head's front reaches round its cut corners to the ears.
VIEW_BIAS = {("head", "front"): 1.5, ("head", "top"): 0.8, ("torso", "front"): 1.15, ("torso", "back"): 1.15,
             ("cape", "front"): 1.15, ("cape", "back"): 1.15, ("cape", "top"): 1.1,
             ("legL", "top"): 0.9, ("legR", "top"): 0.9}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). u across, v UP.
SWATCHES = {
    "gold": (256, 44, 0.20, 0.02),
    "sash": (150, 372, 0.042, 0.104),
    "lapel": (150, 180, 0.054, 0.064),
    "medallion": (200, 128, 0.072, 0.046),
    "brooch": (128, 100, 0.046, 0.036),
}

# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    # hair: the dark mass and the lit locks each have a top, a side and an under tone
    "hair": HAIR, "hair_mid": HAIR_MID, "hair_under": HAIR_DEEP,
    "hairlit": HAIR_LIT, "hairlit_top": HAIR_LIT_TOP,
    "skin_lit": SKIN_LIT, "skin_tone": SKIN, "skin_shade": SKIN_SHADE,
    "gold_lit": GOLD_LIT, "gold_tone": GOLD, "gold_dark": GOLD_DARK,
    "rust_lit": RUST_LIT, "rust": RUST, "rust_dark": RUST_DARK, "seam": WEAVE,
    "cream_lit": CREAM_LIT, "cream": CREAM, "cream_shade": CREAM_SHADE, "cream_deep": CREAM_DEEP,
    "cotton": COTTON, "cotton_shade": COTTON_SHADE,
    "cape_tone": CAPE, "cape_lit": CAPE_MID, "cape_in": CAPE_DARK,
    "base": BASE, "base_lit": BASE_LIT, "lining": BASE_DARK, "deep": BASE_DEEP,
    "gem": CAPE_LIT, "gem_dark": CAPE,
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
# THE HEAD. Her face on the flat front of the block: a warm lit centre, shade turning away at
# the temples, the cast shadow of her four swept fringe locks cut to their own shapes, two black
# eyes, rosy cheeks, the small smile with its left end hooked up.
# ---------------------------------------------------------------------------
# HER FACE INK, MEASURED, NOT REDRAWN. A first redesign drew upright soft rectangles from looking at
# a render. Owner, 2026-10-05: "amihan also has a slight smug look, notice her eyebrows are slightly
# tilted to look angry/smug in the original". She has no eyebrows; the TOP EDGE of each eye is cut
# on a slant, 4 mm lower at the inner corner than the outer, and does a brow's job
# (CHARACTER_MODEL_METHOD.md section 2). So these are the outlines read vertex for vertex off the
# slot 8 polygons of team-amihan.glb's head mesh, (x, z), her LEFT eye; her right is its exact
# mirror in that file (both were measured).
EYE_OUTLINE = [(0.093, 0.510), (0.099, 0.504), (0.099, 0.458), (0.093, 0.452), (0.059, 0.452), (0.053, 0.458),
               (0.053, 0.500), (0.059, 0.506)]
EYE_CENTRE = (0.076, 0.481)
# 1.33 (rule 12's "about a third bigger") was built first. Owner, after seeing her in the game:
# "turn down the eye size for amihan and cheska". So 1.15: 53 by 67 mm against the original's 46 by 58.
EYE_GROW = 1.15
# the mouth is three bars in the original: a flat middle and an arm rising from each end, each
# 10 mm thick. Measured the same way; the two arms are mirror images.
MOUTH_BAR = [(-0.015, 0.411), (0.015, 0.411), (0.015, 0.421), (-0.015, 0.421)]
MOUTH_ARM = [(0.013, 0.412), (0.029, 0.424), (0.023, 0.432), (0.007, 0.420)]
MOUTH_CENTRE = (0.0, 0.420)
# (kept as the measure the curved mouth in `paint_head_front` is drawn against)


def _about(centre, k, points, s=1):
    return [(s * (centre[0] + (x - centre[0]) * k), centre[1] + (z - centre[1]) * k) for x, z in points]


def _eye(c, s):
    """One eye, `s` +1 for her left: the original's outline, one flat fill of ink."""
    c.mark(INK, _about(EYE_CENTRE, EYE_GROW, EYE_OUTLINE, s), 0.25, 1.0, curved=False)


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (rule 12). The first face here was modelled in paint: eye
    # sockets, a crease under each eye, a lash line with a flick, a brown glow in the eyes, a nose
    # with a lit bridge and a shadow, two steps of jaw shade, a lip shadow. Owner, 2026-10-05, on
    # the whole redesigned cast: "the faces look too realistic and look too human, like it lost
    # its charm, the characters have eyebags etc.. they need to be more cutesy". The cast's charm
    # is a flat face with big simple ink features. So what is left is flat skin with one very soft
    # lit patch, the fringe's shadow as ONE flat tone, her two black eyes, a round blush under
    # each, and her mouth as one stroke.
    # NOTHING DARK BELOW THE FRINGE. Two faint edge shades ran down both sides of the face to the
    # jaw corners; with the toon band on the old tapered jaw they made dark patches the owner read
    # as dirt ("fix this random dark spots on amihan"). The lower face is one skin tone.
    c.blob(SKIN_LIT, (0.0, 0.460), 0.120, 0.085, 26, 0.30)
    # the fringe's cast shadow: four steps, one a lock, dropping toward HER RIGHT as the locks do.
    # ONE flat tone.
    c.mark(SKIN_SHADE, [(-0.26, 0.72), (0.26, 0.72), (0.26, 0.598), (0.142, 0.598), (0.138, 0.590), (0.040, 0.588),
                        (0.036, 0.566), (-0.046, 0.562), (-0.052, 0.534), (-0.116, 0.530), (-0.124, 0.470),
                        (-0.176, 0.466), (-0.180, 0.56), (-0.26, 0.56)], 1.6, 0.7, curved=False)
    # her cheeks: a round blush under each eye, the one soft thing on the face
    c.blob(BLUSH, (-0.108, 0.421), 0.032, 0.022, 7, 0.62)
    c.blob(BLUSH, (0.108, 0.421), 0.032, 0.022, 7, 0.62)
    _eye(c, 1)
    _eye(c, -1)
    # THE MOUTH IS ONE SMOOTH CURVED STROKE. It was the original's three straight bars, copied
    # piece for piece; owner, 2026-10-05: "dante has an updated curved mouth but amihan has a
    # blocky mouth, fix that". Same small smile, same centre, width and weight as the measured
    # bars (MOUTH_BAR, MOUTH_ARM: 58 mm wide, 10 mm thick, a flat-ish middle, both ends turned up),
    # drawn as a continuous curve through five hand-set points, thinning a little to round ends.
    smile = [(-0.0300, 0.4290), (-0.0185, 0.4190), (0.0, 0.4155), (0.0185, 0.4190), (0.0300, 0.4290)]
    c.stroke(INK, smile, 8.6, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, smile[0], 0.0028, 0.0028, 0.25, 1.0)
    c.blob(INK, smile[-1], 0.0028, 0.0028, 0.25, 1.0)
    # (a shaded cup was drawn on the front of each ear; seen from the front it was a brown smudge
    # beside the cheek, one more dark spot. The ear's hook is drawn on the side islands only.)


def _head_side(c):
    """One side of the head; both are the same skin. -y (her face) is to the island's left.
    One skin tone from the hair down: no shade along the jaw or toward the back (see the front)."""
    c.blob(BLUSH, (-0.128, 0.424), 0.022, 0.020, 7, 0.4)
    # under the hair that hangs on both sides: one flat tone, well above the jaw
    c.mark(SKIN_SHADE, [(-0.20, 0.72), (0.22, 0.72), (0.22, 0.52), (-0.06, 0.52), (-0.16, 0.54)], 3, 0.7)
    # the ear: a lit rim, the cup dark, drawn as a hook
    c.blob(SKIN_LIT, (0.012, 0.458), 0.026, 0.036, 2, 0.5)
    c.stroke(SKIN_DEEP, [(-0.004, 0.480), (0.016, 0.474), (0.020, 0.452), (0.006, 0.438), (-0.004, 0.452)], 6.5, 0.6, 0.9)


def paint_head_back(c):
    # under the hair's back mass; the nape below it is plain skin
    c.band(SKIN_SHADE, 0.46, 0.72, 3, 0.7)


# ---------------------------------------------------------------------------
# THE ROBE. A dark teal base (CAST_CLOTHING_STYLE.md rule 1), wrapped left over right, a two-tier
# rust belt, a coat skirt that flares to a gold hem. The gold, the cream lapels, the medallion and
# the sash are geometry with their own swatches; what is drawn here is the dark cloth itself:
# broad lit patches, folds pulled to the belt and fanning down the skirt, seams, stitches.
# ---------------------------------------------------------------------------

def _belt(c):
    for lo, hi in (BELT_LOW, BELT_HIGH):
        c.band(RUST, lo, hi)
        c.band(RUST_LIT, hi - 0.0045, hi - 0.0015, 0.3, 0.9)
        c.band(RUST_DARK, lo, lo + 0.0035, 0.4, 0.9)
    c.band(WEAVE, BELT_LOW[1], BELT_HIGH[0])


def paint_torso_front(c):
    c.mark(BASE_LIT, [(-0.110, 0.300), (-0.040, 0.296), (-0.034, 0.250), (-0.076, 0.236), (-0.116, 0.250)], 8, 0.8)
    c.mark(BASE_LIT, [(0.112, 0.298), (0.046, 0.300), (0.038, 0.252), (0.080, 0.238), (0.118, 0.248)], 8, 0.75)
    # under the capelet the chest is in its shade
    c.band(BASE_DARK, 0.286, 0.37, 5, 0.85)
    # the wrap: her left panel lies over her right, its edge a paler line down to the belt
    c.mark(BASE_DARK, [(-0.050, 0.296), (0.030, 0.296), (-0.018, 0.232), (-0.050, 0.232)], 2.0, 0.6)
    c.stroke(BASE_LIT, [(0.034, 0.296), (0.008, 0.262), (-0.016, 0.232)], 3.4, 0.4, 1.0, (0.8, 0.8))
    c.stitch(CAPE_DARK, [(0.044, 0.292), (0.018, 0.260), (-0.006, 0.234)], 1.5, 4.5, 3.5)
    _belt(c)
    # the skirt: lit down the middle of each panel, folds fanning from the belt to the hem
    c.mark(BASE_LIT, [(-0.110, 0.196), (-0.060, 0.196), (-0.070, 0.120), (-0.128, 0.116)], 9, 0.7)
    c.mark(BASE_LIT, [(0.060, 0.196), (0.112, 0.196), (0.132, 0.118), (0.072, 0.120)], 9, 0.7)
    c.stroke(BASE_DARK, [(-0.092, 0.198), (-0.106, 0.156), (-0.118, 0.106)], 7.0, 0.9, 0.9, (0.3, 0.8))
    c.stroke(BASE_DARK, [(0.122, 0.198), (0.134, 0.158), (0.144, 0.108)], 6.5, 0.9, 0.9, (0.3, 0.8))
    c.stroke(BASE_DARK, [(-0.048, 0.198), (-0.052, 0.150), (-0.054, 0.104)], 5.0, 0.8, 0.8, (0.3, 0.8))
    c.stroke(BASE_DARK, [(0.046, 0.198), (0.050, 0.150), (0.052, 0.104)], 5.0, 0.8, 0.8, (0.3, 0.8))
    # between the skirt's two edges the shorts show: deeper, in the coat's shadow
    c.mark(BASE_DARK, [(-0.036, 0.200), (0.036, 0.200), (0.046, 0.07), (-0.046, 0.07)], 1.0, 0.8, curved=False)
    c.stitch(CAPE_DARK, [(-0.150, 0.108), (-0.060, 0.106)], 1.5, 5.0, 4.0)
    c.stitch(CAPE_DARK, [(0.060, 0.106), (0.150, 0.108)], 1.5, 5.0, 4.0)


def paint_torso_back(c):
    c.mark(BASE_LIT, [(-0.100, 0.262), (0.096, 0.264), (0.088, 0.238), (-0.094, 0.236)], 8, 0.7)
    c.band(BASE_DARK, 0.262, 0.37, 5, 0.85)
    c.stroke(BASE_DARK, [(0.0, 0.262), (0.001, 0.232)], 2.8, 0.5, 0.9, (0.8, 0.8), curved=False)
    _belt(c)
    # the skirt's back: a centre seam, three folds each its own length, lit between them
    c.stroke(BASE_DARK, [(0.0, 0.198), (0.002, 0.150), (0.0, 0.104)], 3.2, 0.5, 1.0, (0.6, 1.0))
    c.mark(BASE_LIT, [(-0.100, 0.194), (-0.030, 0.196), (-0.034, 0.126), (-0.112, 0.120)], 9, 0.7)
    c.mark(BASE_LIT, [(0.030, 0.196), (0.092, 0.194), (0.108, 0.124), (0.036, 0.126)], 9, 0.65)
    c.stroke(BASE_DARK, [(-0.066, 0.198), (-0.078, 0.150), (-0.086, 0.104)], 7.0, 0.9, 0.9, (0.3, 0.8))
    c.stroke(BASE_DARK, [(0.074, 0.198), (0.086, 0.158), (0.092, 0.122)], 6.0, 0.9, 0.85, (0.3, 0.8))
    c.stroke(BASE_DARK, [(0.126, 0.196), (0.138, 0.150), (0.146, 0.106)], 6.5, 0.9, 0.9, (0.3, 0.8))
    c.stitch(CAPE_DARK, [(-0.150, 0.108), (0.0, 0.105), (0.150, 0.108)], 1.5, 5.0, 4.0)


def _torso_side(c):
    c.band(BASE_DARK, 0.270, 0.37, 5, 0.85)
    c.stroke(BASE_DARK, [(0.004, 0.270), (0.003, 0.232)], 2.6, 0.5, 0.9, (0.6, 0.8), curved=False)
    _belt(c)
    c.mark(BASE_LIT, [(-0.060, 0.196), (0.050, 0.196), (0.062, 0.122), (-0.074, 0.120)], 10, 0.6)
    c.stroke(BASE_DARK, [(0.002, 0.198), (0.004, 0.150), (0.002, 0.104)], 3.0, 0.5, 0.95, (0.6, 1.0))
    c.stroke(BASE_DARK, [(-0.050, 0.198), (-0.064, 0.154), (-0.074, 0.108)], 6.5, 0.9, 0.85, (0.3, 0.8))
    c.stroke(BASE_DARK, [(0.052, 0.198), (0.066, 0.156), (0.076, 0.110)], 6.0, 0.9, 0.85, (0.3, 0.8))
    c.stitch(CAPE_DARK, [(-0.14, 0.108), (0.14, 0.108)], 1.5, 5.0, 4.0)


# ---------------------------------------------------------------------------
# THE CAPELET. Her signature teal, and the KASIKUS on its back: the binakol weave's whirlwind,
# diamonds one inside the other round one centre, cream on teal, hard edged (it is a woven
# figure; a soft one would read as a stain). The gold edge is geometry.
# ---------------------------------------------------------------------------
KASIKUS = (0.0, 0.300)


def _diamond(c, colour, hx, hz):
    x, z = KASIKUS
    c.mark(colour, [(x, z + hz), (x + hx, z), (x, z - hz), (x - hx, z)], 0.0, 1.0, curved=False)


def paint_cape_front(c):
    c.mark(CAPE_MID, [(-0.160, 0.350), (-0.070, 0.350), (-0.060, 0.318), (-0.150, 0.312)], 7, 0.75)
    c.mark(CAPE_MID, [(0.160, 0.350), (0.072, 0.350), (0.064, 0.316), (0.152, 0.312)], 7, 0.7)
    # under her chin the cloth is in the head's shade
    c.mark(CAPE_DARK, [(-0.100, 0.372), (0.100, 0.372), (0.074, 0.334), (0.0, 0.326), (-0.074, 0.334)], 3.5, 0.8)
    # folds running down into the two points, and one off each shoulder
    c.stroke(CAPE_DARK, [(-0.050, 0.334), (-0.056, 0.310), (-0.056, 0.290)], 4.0, 0.6, 0.85, (0.3, 0.7))
    c.stroke(CAPE_DARK, [(0.052, 0.334), (0.057, 0.310), (0.057, 0.290)], 4.0, 0.6, 0.85, (0.3, 0.7))
    c.stroke(CAPE_DARK, [(-0.122, 0.340), (-0.130, 0.320), (-0.132, 0.304)], 3.6, 0.6, 0.8, (0.3, 0.7))
    c.stroke(CAPE_DARK, [(0.118, 0.342), (0.126, 0.322), (0.130, 0.306)], 3.6, 0.6, 0.8, (0.3, 0.7))
    # a row of stitches above the gold edge, following the hem down to each point and back
    c.stitch(CAPE_DARK, [(-0.158, 0.310), (-0.110, 0.310), (-0.082, 0.303), (-0.056, 0.293), (-0.030, 0.306), (-0.014, 0.318)], 1.5, 4.5, 3.5)
    c.stitch(CAPE_DARK, [(0.158, 0.310), (0.110, 0.310), (0.082, 0.303), (0.056, 0.293), (0.030, 0.306), (0.014, 0.318)], 1.5, 4.5, 3.5)


def paint_cape_back(c):
    c.mark(CAPE_MID, [(-0.150, 0.350), (0.150, 0.350), (0.140, 0.334), (-0.140, 0.334)], 5, 0.75)
    # under the hair's back mass the top of it is in shade
    c.mark(CAPE_DARK, [(-0.120, 0.372), (0.120, 0.372), (0.090, 0.340), (-0.090, 0.340)], 3.5, 0.6)
    c.stroke(CAPE_DARK, [(-0.108, 0.338), (-0.112, 0.304), (-0.106, 0.276)], 4.0, 0.6, 0.85, (0.3, 0.7))
    c.stroke(CAPE_DARK, [(0.104, 0.338), (0.110, 0.300), (0.104, 0.272)], 4.0, 0.6, 0.85, (0.3, 0.7))
    # the kasikus: three diamonds round one centre, each bar 7 mm, the heart solid
    _diamond(c, CREAM, 0.0420, 0.0300)
    _diamond(c, CAPE, 0.0322, 0.0230)
    _diamond(c, CREAM, 0.0240, 0.0172)
    _diamond(c, CAPE, 0.0150, 0.0108)
    _diamond(c, CREAM, 0.0082, 0.0060)
    c.stitch(CAPE_DARK, [(-0.158, 0.306), (-0.126, 0.292), (-0.100, 0.277), (-0.060, 0.271), (0.0, 0.270), (0.060, 0.271),
                         (0.100, 0.277), (0.126, 0.292), (0.158, 0.306)], 1.5, 4.5, 3.5)


def _cape_side(c):
    c.mark(CAPE_MID, [(-0.090, 0.350), (0.090, 0.350), (0.082, 0.330), (-0.082, 0.330)], 5, 0.75)
    c.stroke(CAPE_DARK, [(-0.040, 0.340), (-0.046, 0.320), (-0.044, 0.304)], 3.6, 0.6, 0.8, (0.3, 0.7))
    c.stroke(CAPE_DARK, [(0.046, 0.340), (0.052, 0.318), (0.048, 0.302)], 3.6, 0.6, 0.8, (0.3, 0.7))
    c.stitch(CAPE_DARK, [(-0.12, 0.310), (0.12, 0.308)], 1.5, 4.5, 3.5)


def paint_cape_top(c):
    # seen from above: lit on the shoulders, in the head's shade round the neck
    c.mark(CAPE_MID, [(-0.150, -0.070), (-0.090, -0.090), (-0.086, 0.090), (-0.150, 0.076)], 8, 0.75)
    c.mark(CAPE_MID, [(0.150, -0.070), (0.090, -0.090), (0.086, 0.090), (0.150, 0.076)], 8, 0.75)
    c.blob(CAPE_DARK, (0.0, 0.0), 0.100, 0.104, 5, 0.9)
    c.stroke(CAPE_DARK, [(-0.150, 0.004), (-0.110, 0.006)], 3.0, 0.5, 0.8, (0.7, 0.3), curved=False)
    c.stroke(CAPE_DARK, [(0.150, 0.004), (0.110, 0.006)], 3.0, 0.5, 0.8, (0.7, 0.3), curved=False)


# ---------------------------------------------------------------------------
# THE ARMS. A teal sleeve that widens to a gold-edged cream cuff, then the bare forearm and a
# block hand with three drawn finger lines. The forearm is a flat tone (it slides out of the
# cuff when the elbow folds, and must not carry the cuff's paint with it).
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    if view in ("front", "back"):
        c.mark(SKIN_SHADE, [(s * WRIST, 0.19), (s * 0.30, 0.19), (s * 0.30, 0.262), (s * WRIST, 0.262)], 0, 0.7, curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.252, 0.302), (s * 0.274, 0.304)], 2.4, 0.4, 0.9, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.252, 0.284), (s * 0.275, 0.283)], 2.4, 0.4, 0.9, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.252, 0.265), (s * 0.273, 0.262)], 2.4, 0.4, 0.9, (1.0, 0.3), curved=False)
        # the thumb, folded along the top of the fist: one drawn hook
        c.stroke(SKIN_DEEP, [(s * 0.234, 0.322), (s * 0.254, 0.324), (s * 0.262, 0.334)], 2.4, 0.4, 0.9, (0.8, 0.4))
    elif view == "bottom":
        c.column(SKIN_SHADE, *sorted((s * WRIST, s * 0.31)), 0, 0.7)
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.232, -0.030), (s * 0.266, -0.028), (s * 0.266, 0.034), (s * 0.232, 0.036)], 5, 0.6)
    c.column(SKIN_SHADE, *sorted((s * 0.270, s * 0.31)), 2.0, 0.5)


def paint_arm(c, s, view):
    """`s` is +1 for her left arm. Each mark is cut to its own piece along the arm (x)."""
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    # the sleeve
    col(CAPE, 0.08, CUFF_GOLD[0])
    if view in ("front", "back"):
        c.mark(CAPE_DARK, [(x(0.08), 0.19), (x(CUFF_GOLD[0]), 0.19), (x(CUFF_GOLD[0]), 0.262), (x(0.08), 0.268)], 0, 0.8, curved=False)
        c.mark(CAPE_MID, [(x(0.118), 0.336), (x(0.160), 0.346), (x(0.160), 0.316), (x(0.118), 0.312)], 5, 0.7)
        # folds: the sleeve hangs from the shoulder and belles out to the cuff
        c.stroke(CAPE_DARK, [(x(0.126), 0.330), (x(0.136), 0.290), (x(0.130), 0.250)], 3.8, 0.6, 0.85)
        c.stroke(CAPE_DARK, [(x(0.150), 0.344), (x(0.156), 0.300), (x(0.152), 0.240)], 3.2, 0.6, 0.8)
    elif view == "top":
        c.mark(CAPE_MID, [(x(0.110), -0.034), (x(0.160), -0.044), (x(0.160), 0.050), (x(0.110), 0.040)], 6, 0.7)
    elif view == "bottom":
        col(CAPE_DARK, 0.08, CUFF_GOLD[0], 0, 0.8)
    # the cuff: a gold edge, then cream cloth, a weave line along it, its far rim in shade
    col(GOLD, CUFF_GOLD[0], CUFF_GOLD[1])
    col(GOLD_LIT, CUFF_GOLD[0] + 0.0035, CUFF_GOLD[0] + 0.0060, 0.3, 0.8)
    col(CREAM, CUFF[0], CUFF[1])
    col(CREAM_SHADE, CUFF[0] + 0.0085, CUFF[0] + 0.0105, 0.3, 0.9)
    col(CREAM_SHADE, CUFF[1] - 0.0030, CUFF[1], 0.4, 0.9)
    if view in ("front", "back"):
        c.mark(GOLD_DARK, [(x(CUFF_GOLD[0]), 0.19), (x(CUFF_GOLD[1]), 0.19), (x(CUFF_GOLD[1]), 0.256), (x(CUFF_GOLD[0]), 0.256)], 0, 0.6, curved=False)
        c.mark(CREAM_SHADE, [(x(CUFF[0]), 0.19), (x(CUFF[1]), 0.19), (x(CUFF[1]), 0.254), (x(CUFF[0]), 0.254)], 0, 0.8, curved=False)
    elif view == "bottom":
        col(GOLD_DARK, CUFF_GOLD[0], CUFF_GOLD[1], 0, 0.6)
        col(CREAM_SHADE, CUFF[0], CUFF[1], 0, 0.8)
    # everything past the cuff is skin; the hand is drawn last so no cloth paint is left on it
    col(SKIN, CUFF[1] + 0.0005, 0.31)
    _hand(c, s, view)


def _fingertips(c):
    """The end-on view of a hand: only the tips show, a little in shade."""
    c.blob(SKIN_SHADE, (0.012, 0.288), 0.05, 0.05, 6, 0.5)


# ---------------------------------------------------------------------------
# THE LEGS. Dark teal shorts under the coat, bare legs, a thick cream sandal with one rust
# strap. The strap and the sole are their own blocks; the foot under them is skin with toes.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for her left leg, -1 for her right. Each leg's marks are its own."""
    x = lambda v: s * v
    # skin first, top to bottom, then the shorts above and the sole below
    if view == "front":
        c.mark(SKIN_LIT, [(x(0.058), 0.102), (x(0.108), 0.102), (x(0.106), 0.066), (x(0.060), 0.066)], 6, 0.6)
        # the knee: one short soft crease, each leg's its own
        if s > 0:
            c.stroke(SKIN_SHADE, [(0.064, 0.094), (0.084, 0.090), (0.102, 0.095)], 2.4, 0.6, 0.8)
        else:
            c.stroke(SKIN_SHADE, [(-0.066, 0.092), (-0.086, 0.089), (-0.100, 0.093)], 2.4, 0.6, 0.8)
        # toes: four short lines on the foot's front edge
        for tx, tz in ((0.050, 0.038), (0.070, 0.040), (0.092, 0.040), (0.112, 0.037)):
            c.stroke(SKIN_DEEP, [(x(tx), tz), (x(tx + 0.001), tz - 0.011)], 2.0, 0.4, 0.85, (1, 0.4), curved=False)
    elif view == "back":
        c.band(SKIN_SHADE, SOLE_TOP, 0.13, 0, 0.55)
        c.stroke(SKIN_DEEP, [(x(0.084), 0.060), (x(0.085), 0.034)], 2.4, 0.6, 0.6, (0.4, 0.8), curved=False)
    else:
        outer = (view == "xpos") == (s > 0)
        if not outer:
            c.band(SKIN_SHADE, SOLE_TOP, 0.13, 0, 0.5)
        # (an ankle bone was drawn here as one small blob; on a bare leg it read as a bruise. Rule 5.)
    c.band(SKIN_SHADE, 0.100, 0.124, 4, 0.75)       # the coat's shadow on the thigh
    if view != "front":
        c.band(SKIN_SHADE, SOLE_TOP, SOLE_TOP + 0.008, 1.5, 0.6)   # not on the toes: beside the rust strap a shaded toe read as more strap
    # the shorts
    c.band(BASE, SHORTS[0], 0.22)
    c.band(BASE_DARK, SHORTS[0], SHORTS[0] + 0.006, 0.5, 0.9)
    if view == "front":
        c.mark(BASE_LIT, [(x(0.050), 0.180), (x(0.110), 0.180), (x(0.112), 0.130), (x(0.052), 0.128)], 6, 0.6)
    # the sole: cream in the robe's shade, a darker foot, one line where the sole's two layers meet
    c.band(CREAM_SHADE, 0.0, SOLE_TOP)
    c.band(CREAM, SOLE_TOP - 0.006, SOLE_TOP - 0.0015, 0.3, 0.9)
    c.band(CREAM_DEEP, 0.010, 0.0122, 0.3, 0.9)
    c.band(CREAM_DEEP, 0.0, 0.0045, 0.8, 0.7)


def paint_leg_top(c, s):
    """Looking down on a foot: skin, four toe lines, the sole's rim round it. The toe is at -y."""
    x = lambda v: s * v
    c.mark(SKIN_LIT, [(x(0.050), -0.060), (x(0.116), -0.060), (x(0.114), -0.006), (x(0.052), -0.006)], 6, 0.5)
    for tx in (0.048, 0.070, 0.094, 0.116):
        c.stroke(SKIN_DEEP, [(x(tx), -0.119), (x(tx), -0.106)], 2.0, 0.4, 0.85, (0.4, 1), curved=False)
    c.stroke(SKIN_SHADE, [(x(0.036), -0.104), (x(0.084), -0.101), (x(0.132), -0.104)], 2.0, 0.5, 0.8)
    # the strap's shadow on the foot, either side of it
    c.mark(SKIN_SHADE, [(x(0.0), STRAP[1]), (x(0.17), STRAP[1]), (x(0.17), STRAP[1] + 0.008), (x(0.0), STRAP[1] + 0.008)], 1.2, 0.8, curved=False)


# ---------------------------------------------------------------------------
# THE SWATCHES. u across, v up.
# ---------------------------------------------------------------------------

def paint_gold(c):
    # piping: a pale top edge, a dark under edge, three worn dull places, each its own length
    c.mark(GOLD_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.70), (0.0, 0.70)], 1.2, 0.85, curved=False)
    c.mark(GOLD_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.24), (0.0, 0.24)], 1.2, 0.9, curved=False)
    c.mark(GOLD_DARK, [(0.12, 0.30), (0.22, 0.30), (0.21, 0.74), (0.13, 0.72)], 2.0, 0.35)
    c.mark(GOLD_DARK, [(0.48, 0.28), (0.63, 0.30), (0.62, 0.70), (0.50, 0.72)], 2.0, 0.3)
    c.mark(GOLD_DARK, [(0.83, 0.30), (0.90, 0.30), (0.90, 0.74), (0.84, 0.70)], 2.0, 0.35)


def paint_sash(c):
    # the original's patterned sash: three dark diamonds down a cream strip, two rust stripes at
    # its foot. Each diamond typed on its own; they are a weave, so hard edged.
    c.mark(CREAM_SHADE, [(0.0, 0.0), (0.10, 0.0), (0.10, 1.0), (0.0, 1.0)], 0.6, 0.9, curved=False)
    c.mark(CREAM_SHADE, [(0.90, 0.0), (1.0, 0.0), (1.0, 1.0), (0.90, 1.0)], 0.6, 0.9, curved=False)
    c.mark(CREAM_SHADE, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.90), (0.0, 0.93)], 1.5, 0.8, curved=False)
    c.mark(WEAVE, [(0.50, 0.842), (0.76, 0.742), (0.50, 0.640), (0.24, 0.742)], 0.0, 1.0, curved=False)
    c.mark(WEAVE, [(0.50, 0.612), (0.77, 0.512), (0.50, 0.408), (0.23, 0.512)], 0.0, 1.0, curved=False)
    c.mark(WEAVE, [(0.50, 0.382), (0.75, 0.286), (0.50, 0.190), (0.25, 0.286)], 0.0, 1.0, curved=False)
    c.mark(RUST, [(0.0, 0.066), (1.0, 0.066), (1.0, 0.134), (0.0, 0.134)], 0.0, 1.0, curved=False)
    c.mark(RUST, [(0.0, 0.016), (1.0, 0.016), (1.0, 0.042), (0.0, 0.042)], 0.0, 1.0, curved=False)


def paint_lapel(c):
    # undyed abel cotton: a lit upper half, the capelet's shadow across its top, two weft lines
    c.mark(CREAM_LIT, [(0.0, 0.76), (1.0, 0.76), (1.0, 0.30), (0.0, 0.36)], 3.0, 0.7, curved=False)
    c.mark(CREAM_SHADE, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.80), (0.0, 0.86)], 1.5, 0.95, curved=False)
    c.stroke(CREAM_SHADE, [(0.0, 0.50), (1.0, 0.52)], 1.2, 0.3, 0.9, (1, 1), curved=False)
    c.stroke(CREAM_SHADE, [(0.0, 0.24), (1.0, 0.25)], 1.2, 0.3, 0.9, (1, 1), curved=False)
    c.mark(CREAM_SHADE, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.10), (0.0, 0.10)], 1.0, 0.8, curved=False)


def paint_medallion(c):
    # her belt's gold plate, the kasikus engraved in it round the stone: one diamond line, a
    # bright upper edge, a dark foot
    c.mark(GOLD_LIT, [(0.04, 0.96), (0.96, 0.96), (0.96, 0.82), (0.04, 0.82)], 0.8, 0.9, curved=False)
    c.mark(GOLD_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.16), (0.0, 0.16)], 0.8, 0.9, curved=False)
    ring = [(0.50, 0.94), (0.86, 0.50), (0.50, 0.06), (0.14, 0.50), (0.50, 0.94)]
    c.stroke(GOLD_DARK, ring, 2.4, 0.2, 1.0, (1, 1), curved=False)


def paint_brooch(c):
    c.mark(GOLD_LIT, [(0.06, 0.94), (0.94, 0.94), (0.94, 0.78), (0.06, 0.78)], 0.8, 0.9, curved=False)
    c.mark(GOLD_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.18), (0.0, 0.18)], 0.8, 0.9, curved=False)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        return I(name, base, SWATCHES[name][2:4])

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    c = I("head.xpos", SKIN); _head_side(c); done[c.name] = c
    c = I("head.xneg", SKIN); _head_side(c); done[c.name] = c
    c = I("head.back", SKIN); paint_head_back(c); done[c.name] = c
    # seen from above the head block is under the hair; what shows is the tops of the ears and
    # of the nose, which are SKIN and nothing else
    c = I("head.top", mix(SKIN, SKIN_LIT, 0.5)); done[c.name] = c
    c = I("head.bottom", SKIN); done[c.name] = c

    c = I("torso.front", BASE); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", BASE); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", BASE); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", BASE); _torso_side(c); done[c.name] = c

    c = I("cape.front", CAPE); paint_cape_front(c); done[c.name] = c
    c = I("cape.back", CAPE); paint_cape_back(c); done[c.name] = c
    c = I("cape.xpos", CAPE); _cape_side(c); done[c.name] = c
    c = I("cape.xneg", CAPE); _cape_side(c); done[c.name] = c
    c = I("cape.top", CAPE); paint_cape_top(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            if view in ("xpos", "xneg"):
                _fingertips(c)
            else:
                paint_arm(c, s, view)
            done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            if view == "top":
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c

    c = swatch("gold", GOLD); paint_gold(c); done[c.name] = c
    c = swatch("sash", CREAM); paint_sash(c); done[c.name] = c
    c = swatch("lapel", CREAM); paint_lapel(c); done[c.name] = c
    c = swatch("medallion", GOLD); paint_medallion(c); done[c.name] = c
    c = swatch("brooch", GOLD); paint_brooch(c); done[c.name] = c
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
    atlas = Image.new("RGB", (size, size), _rgb(BASE_DARK))
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
    atlas.save(out)
    used = sum(r[2] * r[3] for r in layout(size).values())
    print("wrote %s  (%d x %d, %d islands, %.0f%% of the top half painted)"
          % (out, size, size, len(islands), 100.0 * used / (size * size / 2)))
    if "--sheet" in sys.argv:
        sheet = sys.argv[sys.argv.index("--sheet") + 1]
        atlas.crop((0, 0, size, size // 2)).save(sheet)
        print("wrote %s" % sheet)


if __name__ == "__main__":
    main()
