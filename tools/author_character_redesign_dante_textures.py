"""Paint the atlas of the Dante (displayed: Basilio) redesign PROTOTYPE, by hand, in the house style.

    py -3 tools/author_character_redesign_dante_textures.py [--size 1024]

Writes Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign-atlas.png. The model is
built by tools/author_character_redesign_dante.py, which imports this file for the LAYOUT only
(PIL is imported inside main, Blender's Python has none) and so maps every face onto the island
painted for it here. Paint first, then build.

WHY. Owner, 2026-10-05: *"can you try redesigning one of the normal blocky character models? add
texture, more unique shapes and be less oriented around the whole blocky aesthetic"*, on a cast
whose faces have *"no shading, texture or any of that stylized character"*. This is a TRY. Nothing
in the game loads these files and team-dante.glb is not touched.

THE HEAD IS STILL THE GAME'S BOX HEAD. A first pass modelled a round skull and the owner turned it
down the same day: *"i dont like the deviation from the boxy head.. i know i said stray further
from the original boxy design, but its somewhat part of our game's identity"*, then, on how far
back to go, *"not necessarily, just not entirely a cube"*. So the face here is drawn for the
front of a carved block: the depth a flat ramp cannot give is PAINTED (a lit middle, edges that
turn away, shade under the fringe, round the eyes and along the jaw), and the features are the
original's own (the squint, the gold slit eye, the scar, the frown).

THE ONE MECHANISM, and it is the beggar's (tools/build_beggar_voxel.py): `Toon.shader` remaps a
UV to a palette slot only in Unity atlas rows 0 to 7 and samples the texture itself above them
(`if (row <= 7) base = _Palette[...]`), so every island lives in the TOP half of the file and the
bottom half is left flat. A roster entry with or without a 16 colour palette draws this the same.

HOW AN ISLAND IS LAID OUT. A body part is seen from up to six sides (front, back, xpos, xneg, top,
bottom) and each side is one island, a flat orthographic view of the part in MODEL METRES. So a
mark is written where it sits on the body, `(x, z)` on a front or back view, `(y, z)` on a side
view, `(x, y)` on a top view, and a belt drawn at one height meets itself round all four sides
with no seam work. Loose blocks (hair clumps, the horn, the collar, gold piping) take a SWATCH
instead, u round or along the piece and v up it.

THE STYLE is Kanto's, the Lagoon's and the beggar's (KANTO_DESIGN_GUIDE.md section 3,
LAGOON_REWORK_GUIDE.md section 2): flat fills, a few LARGE patches with feathered edges, drawn
seams and folds, no grain, no noise, no photo. Every mark is its own hand-set shape with its own
numbers (CHARACTER_MODEL_METHOD.md section 4); nothing below loops a motif round a limb.

HIS COLOURS ARE THE ORIGINAL'S (tools/build_bayan_voxel.py PALETTE): bronze skin, charcoal hair,
forest green, gold, leather brown, white soles, a gold left eye. The painted tones are steps of
those. ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth colour is checked
below and the build refuses one that sits near either. Skin is exempt, as it is for the cast,
and is kept brown, its blush a red brown and never an orange.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "dante"
ATLAS_NAME = "dante-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is the original palette slot, unchanged.
# ---------------------------------------------------------------------------
SKIN = "a8602c"; SKIN_LIT = "bc7238"; SKIN_SHADE = "8b4a24"; SKIN_DEEP = "6c371c"
BLUSH = "b3523a"; SCAR_DARK = "6b3018"; SCAR_LIT = "cf8c62"
STUBBLE = "3d2a20"; STUBBLE_LIT = "5a4030"
HAIR = "1a181e"; HAIR_TOP = "2b2933"; HAIR_DEEP = "0e0d12"
INK = "1a1420"; EYE_GOLD = "ffd700"; EYE_AMBER = "c98a00"; WHITE = "f4faff"
GREEN = "3d6335"; GREEN_LIT = "52804a"; GREEN_DARK = "243e1f"; GREEN_DEEP = "172a15"
GOLD = "dfb248"; GOLD_LIT = "f3d483"; GOLD_DARK = "a8862c"
BROWN = "482f1d"; BROWN_LIT = "634329"; BROWN_DARK = "2f1e12"; BROWN_DUST = "7b654c"
# ⚠️ A patch is a scrap of the OTHER garment, hard edged, with a dark rim and pale thread. The
# first ones were a soft grey olive at low contrast on both cloths; the owner read them as
# z-fighting (2026-10-05). A mark this size either reads as a sewn thing or as a render fault.
PATCH = "58603a"; PATCH_EDGE = "2e351f"; THREAD = "d8c79a"
JADE = "38b848"; JADE_LIT = "68e878"; JADE_DARK = "1e7a35"
SHOE = "f4faff"; SHOE_SHADE = "c6ccd3"; SHOE_SCUFF = "9d9a93"; SHOE_DIRT = "8a7a66"
BANDAGE = "ddd5c0"; BANDAGE_SHADE = "b3a78e"; BANDAGE_DIRT = "8f8169"
HORN = "272a33"; HORN_LIT = "4d5464"; HORN_VIOLET = "4e3458"
SILVER = "d4e2ec"

CLOTH_HEXES = [HAIR, HAIR_TOP, GREEN, GREEN_LIT, GREEN_DARK, GOLD, GOLD_LIT, GOLD_DARK, BROWN,
               BROWN_LIT, BROWN_DUST, PATCH, JADE, JADE_LIT, SHOE, SHOE_SHADE, SHOE_SCUFF,
               SHOE_DIRT, BANDAGE, BANDAGE_SHADE, BANDAGE_DIRT, HORN, HORN_LIT, HORN_VIOLET]

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y). +x is HIS left (the gold eye, the scar, the horn, the bare arm),
# -y is the way he faces, z is up, the feet are on zero.
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.26, 0.26, 0.30, 0.70), 1300),
        "back": ((-0.26, 0.26, 0.30, 0.70), 560),
        "xpos": ((-0.20, 0.20, 0.30, 0.70), 800),
        "xneg": ((-0.20, 0.20, 0.30, 0.70), 800),
        "top": ((-0.26, 0.26, -0.20, 0.20), 260),
        "bottom": ((-0.26, 0.26, -0.20, 0.20), 260),
    },
    "torso": {
        "front": ((-0.20, 0.20, 0.09, 0.36), 1100),
        "back": ((-0.20, 0.20, 0.09, 0.36), 1100),
        "xpos": ((-0.15, 0.16, 0.09, 0.36), 950),
        "xneg": ((-0.15, 0.16, 0.09, 0.36), 950),
        "top": ((-0.20, 0.20, -0.15, 0.16), 560),
    },
    "armL": {
        "front": ((0.09, 0.30, 0.17, 0.36), 1000),
        "back": ((0.09, 0.30, 0.17, 0.36), 1000),
        "top": ((0.09, 0.30, -0.08, 0.09), 1000),
        "bottom": ((0.09, 0.30, -0.08, 0.09), 900),
        "xpos": ((-0.08, 0.09, 0.22, 0.36), 800),
    },
    "armR": {
        "front": ((-0.30, -0.09, 0.215, 0.36), 1000),
        "back": ((-0.30, -0.09, 0.215, 0.36), 1000),
        "top": ((-0.30, -0.09, -0.08, 0.09), 1000),
        "bottom": ((-0.30, -0.09, -0.08, 0.09), 900),
        "xneg": ((-0.08, 0.09, 0.22, 0.36), 800),
    },
    "legL": {
        "front": ((0.0, 0.17, 0.0, 0.19), 1000),
        "back": ((0.0, 0.17, 0.0, 0.19), 900),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 1000),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 850),
        "top": ((0.0, 0.17, -0.16, 0.11), 1000),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.19), 1000),
        "back": ((-0.17, 0.0, 0.0, 0.19), 900),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 850),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 1000),
        "top": ((-0.17, 0.0, -0.16, 0.11), 1000),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group. The face
# is drawn on the front island, so the head's front reaches round its cut corners to the ears.
VIEW_BIAS = {("head", "front"): 1.5, ("head", "top"): 0.8, ("torso", "front"): 1.15, ("torso", "back"): 1.15,
             ("legL", "top"): 0.9, ("legR", "top"): 0.9}

# name -> (px wide, px high at 2048, metres wide, metres high). u runs across, v UP.
SWATCHES = {
    "hair": (40, 40, 0.04, 0.04),
    "hair_top": (40, 40, 0.04, 0.04),
    "hair_under": (40, 40, 0.04, 0.04),
    "gold_tone": (40, 40, 0.04, 0.04),
    "skin_tone": (40, 40, 0.04, 0.04),
    "belt_tone": (40, 40, 0.04, 0.04),
    "cuff_tone": (40, 40, 0.04, 0.04),
    "wrap_tone": (40, 40, 0.04, 0.04),
    "horn": (128, 288, 0.16, 0.36),
    "collar_out": (640, 150, 0.60, 0.13),
    "collar_in": (640, 96, 0.60, 0.13),
    "gold": (256, 44, 0.20, 0.02),
    "lining": (40, 40, 0.04, 0.04),
    "sole": (40, 40, 0.04, 0.04),
    "jade": (64, 64, 0.03, 0.03),
}

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin. `head.front`, `hair`, and so on.

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


# ⚠️ THE CLOTHES ARE QUIET. Owner, 2026-10-05, seeing him in the game: *"lets tone down the details
# in dantes clothes"*. Every cloth island was drawn with sun patches, folds, stitch rows, a
# pocket, sewn patches and dust; in the game's flat two-band shader, beside a cast in plain
# colour, that reads as busy. So the soft cloth marks are laid at well under half strength, the
# fold strokes fainter still, and the stitches, the dust and both sewn patches are not drawn.
# The structure (belt, buckle, undershirt, cuffs, piping) is untouched: it is geometry or hard
# edged. Set QUIET_CLOTH to False to see the busy version again.
QUIET_CLOTH = True
SOFT_MARK = 0.40      # sun patches and shade, where a mark is feathered 3 mm or more
FOLD = 0.32           # fold strokes


def _cloth_tone(colour):
    return QUIET_CLOTH and colour in (GREEN_LIT, GREEN_DARK, GREEN_DEEP, BROWN_LIT, BROWN_DARK, BANDAGE_SHADE, BANDAGE_DIRT)


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
        if QUIET_CLOTH and colour in (BROWN_DUST, SHOE_DIRT):
            return self
        if _cloth_tone(colour) and feather >= 3:
            strength *= SOFT_MARK
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
        if _cloth_tone(colour):
            strength *= FOLD
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
            # full width through the middle, easing to each end's own taper
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
        if QUIET_CLOTH:
            return self
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
        if QUIET_CLOTH and colour in (BROWN_DUST, SHOE_DIRT):
            return self
        if _cloth_tone(colour) and feather >= 3:
            strength *= SOFT_MARK
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
# THE HEAD. The face is where the owner's note lands: form painted under a flat ramp. A warm lit
# centre, shade turning away at the temples, a cast shadow under the fringe cut to the fringe's
# own shape, sockets, a blush, a jaw that goes dark under the chin.
# ---------------------------------------------------------------------------

def _about(centre, k, points):
    """`points` grown `k` times about `centre`."""
    return [(centre[0] + (x - centre[0]) * k, centre[1] + (y - centre[1]) * k) for x, y in points]


def paint_head_front(c):
    # ⚠️⚠️ A CUTE FACE, NOT A PORTRAIT. The first face was modelled in paint: eye sockets, a crease
    # under the squint, a nose with its own shadow, two steps of jaw shade, a lip shadow. Owner,
    # 2026-10-05, on the whole redesigned cast: *"the faces look too realistic and look too human,
    # like it lost its charm, the characters have eyebags etc.. they need to be more cutesy"*.
    # The cast's charm is a flat face with big simple ink features (CHARACTER_MODEL_METHOD.md
    # section 2: eyes and a mouth, nothing else). So: NO nose, NO sockets, NO creases or lids drawn
    # as lines, NO lip shadow, NO jaw contour. What is left is flat skin with one soft lit patch,
    # the fringe's flat shadow, BIG eyes, a small mouth, and his scar. (Blush is for the girls.)
    side = mix(SKIN, SKIN_SHADE, 0.30)
    c.blob(SKIN_LIT, (0.0, 0.470), 0.120, 0.090, 26, 0.35)
    # the block's edges turn away, faintly
    c.mark(side, [(-0.27, 0.72), (-0.160, 0.72), (-0.150, 0.54), (-0.160, 0.40), (-0.146, 0.33), (-0.27, 0.30)], 10, 0.8)
    c.mark(side, [(0.27, 0.72), (0.160, 0.72), (0.152, 0.55), (0.162, 0.41), (0.148, 0.33), (0.27, 0.30)], 10, 0.8)
    # the fringe's cast shadow, stepped like the four slabs that throw it: ONE flat tone
    c.mark(SKIN_SHADE, [(-0.26, 0.72), (0.26, 0.72), (0.26, 0.560), (0.142, 0.560), (0.142, 0.522), (0.064, 0.522),
                        (0.064, 0.540), (-0.010, 0.540), (-0.010, 0.510), (-0.090, 0.510), (-0.090, 0.494),
                        (-0.180, 0.494), (-0.180, 0.56), (-0.26, 0.56)], 1.6, 0.75, curved=False)
    # NO BLUSH ON HIM. Owner, 2026-10-05: *"reserve the blush for the female characters"*.
    # THE SCAR, forehead to jaw through the gold eye: one dark line with a pale core. The cross
    # ticks are gone with the rest of the fine drawing.
    scar = [(0.040, 0.664), (0.044, 0.610), (0.052, 0.552), (0.066, 0.508), (0.086, 0.468),
            (0.088, 0.432), (0.080, 0.396), (0.066, 0.354)]
    c.stroke(SCAR_DARK, scar, 10.5, 0.5, 1.0, (0.2, 0.15))
    c.stroke(SCAR_LIT, scar, 3.6, 0.4, 0.95, (0.1, 0.1))
    # THE EYES ARE THE ORIGINAL'S, MEASURED OFF team-dante.glb AND GROWN 1.28 TIMES: there both
    # are ink shapes 72 mm wide set 40 mm either side of the middle, his right 60 tall, his left
    # 75, and the gold is a small iris INSIDE the left one (32 by 26). The first cute pass made
    # the gold eye a third bigger than the other, a round disc with a rim and a glint on a black
    # plate. Owner, 2026-10-05: *"make dantes yellow eye the same size and make it look less like
    # a sticker"*. So: one silhouette for both eyes, mirrored, and the gold is an angular iris cut
    # off by the eye's own slanted top edge, so it sits IN the eye. No rim, no glint, no disc.
    # HIS RIGHT EYE, the squint: one solid ink wedge, its top edge slanted down toward the middle
    c.mark(INK, [(-0.132, 0.502), (-0.042, 0.480), (-0.040, 0.452), (-0.112, 0.446), (-0.134, 0.466)], 0.3, 1.0,
           curved=False)
    # HIS LEFT EYE: the same wedge mirrored, a little taller at its outer end as the original's is
    c.mark(INK, [(0.134, 0.512), (0.042, 0.480), (0.040, 0.452), (0.112, 0.446), (0.136, 0.466)], 0.3, 1.0,
           curved=False)
    c.mark(EYE_GOLD, [(0.064, 0.4565), (0.110, 0.4535), (0.114, 0.4975), (0.064, 0.4805)], 0.25, 1.0, curved=False)
    c.mark(EYE_AMBER, [(0.064, 0.4565), (0.110, 0.4535), (0.111, 0.4625), (0.064, 0.4645)], 0.4, 0.55, curved=False)
    c.mark(INK, [(0.0845, 0.4555), (0.0915, 0.4550), (0.0925, 0.4895), (0.0855, 0.4875)], 0.2, 1.0, curved=False)
    # the mouth: a small frown, one heavy stroke and nothing under it
    c.stroke(INK, [(-0.030, 0.392), (-0.012, 0.4015), (0.010, 0.402), (0.028, 0.3945)], 7.4, 0.3, 1.0, (0.5, 0.65))
    # the shaved temple on the horn's side
    c.mark(STUBBLE, [(0.132, 0.72), (0.26, 0.72), (0.26, 0.512), (0.160, 0.512), (0.142, 0.566)], 1.5, 0.92)
    # ears: the cup of each, in shade
    c.blob(SKIN_DEEP, (-0.196, 0.455), 0.012, 0.024, 2.0, 0.7)
    c.blob(SKIN_DEEP, (0.196, 0.455), 0.012, 0.024, 2.0, 0.7)


def _head_side(c, sign):
    """One side of the head. `sign` is +1 for his left (xpos, the shaved temple), -1 for his right."""
    c.mark(SKIN_SHADE, [(0.04, 0.72), (0.22, 0.72), (0.22, 0.30), (0.08, 0.30), (0.05, 0.46)], 14, 0.35)
    # the ear: a lit rim, the cup dark, drawn as a hook, and his stud under it
    c.blob(SKIN_LIT, (0.012, 0.458), 0.026, 0.036, 2, 0.5)
    c.stroke(SKIN_DEEP, [(-0.004, 0.480), (0.016, 0.474), (0.020, 0.452), (0.006, 0.438), (-0.004, 0.452)], 7.0, 0.6, 0.95)
    c.blob(SILVER, (0.008, 0.4255), 0.0070, 0.0070, 0.3, 1.0)
    if sign > 0:
        # the undercut: dark stubble, three lighter razor lines, the horn's root in shadow
        c.mark(STUBBLE, [(-0.20, 0.72), (0.22, 0.72), (0.22, 0.520), (0.070, 0.512), (-0.02, 0.520),
                         (-0.088, 0.508), (-0.146, 0.560), (-0.20, 0.56)], 2.5, 0.92)
        c.stroke(STUBBLE_LIT, [(-0.120, 0.596), (-0.070, 0.590)], 3.4, 0.6, 0.7, curved=False)
        c.stroke(STUBBLE_LIT, [(0.050, 0.566), (0.130, 0.574)], 4.0, 0.6, 0.8, curved=False)
        c.stroke(STUBBLE_LIT, [(0.040, 0.610), (0.120, 0.620)], 3.4, 0.6, 0.7, curved=False)
        c.stroke(STUBBLE_LIT, [(-0.070, 0.536), (0.050, 0.538)], 3.0, 0.6, 0.6, curved=False)
    else:
        c.mark(SKIN_SHADE, [(-0.20, 0.72), (0.22, 0.72), (0.22, 0.50), (-0.06, 0.50), (-0.16, 0.47)], 4, 0.85)


def paint_head_back(c):
    c.band(SKIN_DEEP, 0.28, 0.40, 8, 0.6)
    # the shaved side shows past the hair's edge; it stops above the ear, which stays skin
    c.mark(STUBBLE, [(0.03, 0.72), (0.26, 0.72), (0.26, 0.514), (0.10, 0.514)], 1.5, 0.9, curved=False)


# ---------------------------------------------------------------------------
# THE JACKET. Forest green, open on a brown undershirt, piped in gold. The piping itself is
# geometry (CAST_CLOTHING_STYLE.md rule 3); what is drawn here is the cloth: two broad sun
# patches, folds pulled toward the belt, seams, stitches, a chest pocket, grime at the hem.
# ---------------------------------------------------------------------------
BELT = (0.203, 0.234)


def _belt(c):
    c.band(BROWN, BELT[0], BELT[1])
    c.band(BROWN_LIT, BELT[1] - 0.008, BELT[1] - 0.0035, 0.4, 0.9)
    c.band(BROWN_DARK, BELT[0], BELT[0] + 0.006, 0.6, 0.9)


def paint_torso_front(c):
    c.mark(GREEN_LIT, [(-0.150, 0.345), (-0.050, 0.350), (-0.040, 0.300), (-0.085, 0.268), (-0.150, 0.280)], 9, 0.75)
    c.mark(GREEN_LIT, [(0.150, 0.345), (0.052, 0.350), (0.044, 0.296), (0.090, 0.262), (0.150, 0.276)], 9, 0.7)
    c.mark(GREEN_DARK, [(-0.20, 0.300), (-0.128, 0.292), (-0.108, 0.250), (-0.126, 0.235), (-0.20, 0.235)], 7, 0.75)
    c.mark(GREEN_DARK, [(0.20, 0.296), (0.130, 0.288), (0.110, 0.248), (0.128, 0.235), (0.20, 0.235)], 7, 0.75)
    # folds pulled to the belt
    c.stroke(GREEN_DARK, [(-0.112, 0.286), (-0.090, 0.262), (-0.078, 0.238)], 6.5, 0.8, 0.85, (0.1, 0.6))
    c.stroke(GREEN_DARK, [(0.118, 0.280), (0.094, 0.258), (0.084, 0.238)], 6.0, 0.8, 0.85, (0.1, 0.6))
    c.stroke(GREEN_DARK, [(-0.058, 0.262), (-0.052, 0.246), (-0.054, 0.236)], 4.5, 0.8, 0.7, (0.1, 0.5))
    # the undershirt in the opening, the bare neck above it, a collarbone
    c.mark(BROWN, [(-0.066, 0.362), (0.066, 0.362), (0.052, 0.320), (0.030, 0.236), (-0.030, 0.236), (-0.052, 0.320)],
           0.4, 1.0, curved=False)
    c.mark(BROWN_LIT, [(-0.020, 0.300), (0.022, 0.302), (0.016, 0.246), (-0.014, 0.244)], 6, 0.6)
    c.mark(SKIN_SHADE, [(-0.046, 0.362), (0.046, 0.362), (0.034, 0.326), (0.0, 0.310), (-0.034, 0.326)], 0.5, 1.0)
    c.stroke(SKIN_DEEP, [(-0.030, 0.334), (-0.008, 0.326), (0.0, 0.330)], 3.2, 0.5, 0.9)
    c.stroke(BROWN_DARK, [(-0.036, 0.326), (0.0, 0.308), (0.036, 0.326)], 4.0, 0.4, 1.0, (0.6, 0.6))
    # the chest pocket on his right, its flap and its stitches
    c.mark(GREEN_DARK, [(-0.124, 0.303), (-0.072, 0.305), (-0.073, 0.262), (-0.123, 0.260)], 0.6, 0.55, curved=False)
    c.stroke(GREEN_DEEP, [(-0.126, 0.292), (-0.098, 0.286), (-0.070, 0.294)], 3.0, 0.4, 1.0, (0.7, 0.7))
    c.stitch(GREEN_LIT, [(-0.121, 0.286), (-0.120, 0.264), (-0.076, 0.266), (-0.076, 0.288)], 1.6, 5.0, 4.0)
    # shoulder seams
    c.stitch(GREEN_DEEP, [(-0.134, 0.302), (-0.116, 0.324), (-0.090, 0.338)], 1.8, 6.0, 4.0)
    c.stitch(GREEN_DEEP, [(0.134, 0.300), (0.118, 0.322), (0.092, 0.338)], 1.8, 6.0, 4.0)
    _belt(c)
    # the buckle plate: gold, a jade stone, one glint
    c.mark(GOLD, [(-0.040, 0.250), (0.040, 0.250), (0.040, 0.188), (-0.040, 0.188)], 0.4, 1.0, curved=False)
    c.mark(GOLD_DARK, [(-0.040, 0.200), (0.040, 0.200), (0.040, 0.188), (-0.040, 0.188)], 1.0, 0.8, curved=False)
    c.mark(GOLD_LIT, [(-0.036, 0.246), (0.020, 0.246), (0.008, 0.239), (-0.036, 0.239)], 0.8, 0.85)
    c.blob(JADE_DARK, (0.0, 0.2185), 0.0215, 0.0185, 0.3, 1.0)
    c.blob(JADE, (0.0, 0.2195), 0.0170, 0.0145, 0.4, 1.0)
    c.blob(JADE_LIT, (-0.006, 0.2245), 0.0065, 0.0045, 0.5, 1.0)
    # the tails below the belt: folds fanning down, dust and grime gathering at the hem
    c.stroke(GREEN_DARK, [(-0.100, 0.200), (-0.118, 0.165), (-0.128, 0.128)], 8.0, 0.9, 0.85, (0.3, 0.8))
    c.stroke(GREEN_DARK, [(0.092, 0.200), (0.112, 0.168), (0.126, 0.130)], 7.5, 0.9, 0.85, (0.3, 0.8))
    c.stroke(GREEN_LIT, [(-0.066, 0.198), (-0.082, 0.166), (-0.090, 0.140)], 7.0, 1.2, 0.6, (0.3, 0.6))
    c.stroke(GREEN_LIT, [(0.060, 0.198), (0.074, 0.168), (0.084, 0.144)], 6.5, 1.2, 0.6, (0.3, 0.6))
    c.mark(GREEN_DARK, [(-0.20, 0.150), (-0.140, 0.146), (-0.090, 0.156), (-0.040, 0.168), (-0.040, 0.09), (-0.20, 0.09)], 6, 0.55)
    c.mark(GREEN_DARK, [(0.20, 0.148), (0.138, 0.150), (0.092, 0.158), (0.040, 0.170), (0.040, 0.09), (0.20, 0.09)], 6, 0.55)
    c.mark(BROWN_DUST, [(-0.20, 0.132), (-0.150, 0.138), (-0.110, 0.130), (-0.080, 0.136), (-0.080, 0.09), (-0.20, 0.09)], 4, 0.4)
    c.mark(BROWN_DUST, [(0.20, 0.134), (0.156, 0.130), (0.116, 0.138), (0.084, 0.132), (0.084, 0.09), (0.20, 0.09)], 4, 0.4)


def paint_torso_back(c):
    c.mark(GREEN_LIT, [(-0.150, 0.348), (0.150, 0.348), (0.132, 0.300), (0.060, 0.312), (-0.050, 0.306), (-0.134, 0.296)], 9, 0.7)
    c.mark(GREEN_DARK, [(-0.20, 0.292), (-0.130, 0.286), (-0.112, 0.246), (-0.20, 0.236)], 7, 0.75)
    c.mark(GREEN_DARK, [(0.20, 0.290), (0.132, 0.284), (0.114, 0.246), (0.20, 0.236)], 7, 0.75)
    # the centre back seam
    c.stroke(GREEN_DARK, [(0.001, 0.346), (-0.001, 0.300), (0.001, 0.236)], 3.2, 0.5, 0.9, (0.8, 0.8))
    # the emblem off his old cape, brushed on by hand in gold: a diamond, its core, a bar under it
    c.mark(GOLD, [(0.0, 0.333), (0.047, 0.288), (0.001, 0.245), (-0.046, 0.289)], 0.5, 1.0, curved=False)
    c.mark(GREEN, [(0.0, 0.317), (0.031, 0.288), (0.001, 0.261), (-0.030, 0.289)], 0.5, 1.0, curved=False)
    c.mark(GOLD, [(0.0, 0.301), (0.013, 0.289), (0.001, 0.277), (-0.012, 0.289)], 0.4, 1.0, curved=False)
    c.mark(GOLD_DARK, [(0.004, 0.249), (0.047, 0.288), (0.040, 0.289), (0.002, 0.256)], 0.6, 0.8, curved=False)
    c.stroke(GOLD, [(-0.060, 0.2415), (0.058, 0.2425)], 5.0, 0.4, 1.0, (0.5, 0.3), curved=False)
    c.stitch(GREEN_DEEP, [(-0.134, 0.300), (-0.114, 0.322), (-0.088, 0.338)], 1.8, 6.0, 4.0)
    c.stitch(GREEN_DEEP, [(0.134, 0.298), (0.116, 0.320), (0.090, 0.338)], 1.8, 6.0, 4.0)
    _belt(c)
    # the long tail: a centre vent, folds, a sewn patch on his left tail, dust low down
    c.stroke(GREEN_DEEP, [(0.0, 0.202), (0.002, 0.160), (0.0, 0.118)], 5.0, 0.5, 1.0, (0.5, 1.0))
    c.stroke(GREEN_DARK, [(-0.070, 0.200), (-0.086, 0.160), (-0.094, 0.112)], 8.0, 0.9, 0.85, (0.3, 0.8))
    c.stroke(GREEN_DARK, [(0.118, 0.200), (0.134, 0.162), (0.140, 0.124)], 7.0, 0.9, 0.8, (0.3, 0.8))
    c.stroke(GREEN_LIT, [(-0.034, 0.198), (-0.044, 0.160), (-0.048, 0.124)], 7.5, 1.2, 0.6, (0.3, 0.6))
    if not QUIET_CLOTH: c.mark(GREEN_DEEP, [(0.036, 0.186), (0.096, 0.190), (0.101, 0.136), (0.040, 0.130)], 0.0, 1.0, curved=False)
    if not QUIET_CLOTH: c.mark(BROWN_LIT, [(0.040, 0.182), (0.092, 0.186), (0.097, 0.140), (0.044, 0.134)], 0.0, 1.0, curved=False)
    c.stitch(THREAD, [(0.047, 0.175), (0.086, 0.178), (0.090, 0.147), (0.051, 0.142), (0.047, 0.170)], 2.0, 5.5, 4.0, 1.0)
    c.mark(BROWN_DUST, [(-0.20, 0.126), (-0.120, 0.132), (-0.050, 0.120), (0.030, 0.128), (0.120, 0.120),
                        (0.20, 0.128), (0.20, 0.09), (-0.20, 0.09)], 4, 0.45)


def _torso_side(c):
    c.mark(GREEN_DARK, [(-0.050, 0.300), (0.060, 0.300), (0.050, 0.240), (-0.040, 0.240)], 10, 0.75)
    c.stroke(GREEN_DEEP, [(0.006, 0.290), (0.004, 0.260), (0.006, 0.236)], 2.6, 0.5, 0.9, (0.6, 0.8))
    _belt(c)
    c.stroke(GREEN_DARK, [(-0.030, 0.200), (-0.040, 0.160), (-0.044, 0.118)], 8.0, 0.9, 0.8, (0.3, 0.8))
    c.stroke(GREEN_LIT, [(0.036, 0.198), (0.048, 0.160), (0.054, 0.124)], 7.5, 1.2, 0.6, (0.3, 0.6))
    c.mark(BROWN_DUST, [(-0.15, 0.128), (-0.060, 0.134), (0.020, 0.124), (0.16, 0.130), (0.16, 0.09), (-0.15, 0.09)], 4, 0.45)


def paint_torso_top(c):
    c.blob(GREEN_LIT, (0.0, 0.0), 0.17, 0.085, 12, 0.6)
    c.blob(SKIN_DEEP, (0.0, 0.0), 0.062, 0.058, 2, 1.0)


# ---------------------------------------------------------------------------
# THE ARMS. His left is bare, the jacket's sleeve torn off at the shoulder, the forearm bound in
# a cloth wrap. His right keeps its sleeve, rolled to a gold cuff. Mitten hands: three drawn
# finger lines each, dirt at the tips.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    """Finger lines and grime on a hand. `s` is +1 for the left arm, -1 for the right."""
    if view in ("front", "back"):
        c.stroke(SKIN_DEEP, [(s * 0.262, 0.300), (s * 0.289, 0.302)], 2.6, 0.4, 0.95, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.262, 0.282), (s * 0.290, 0.281)], 2.6, 0.4, 0.95, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.262, 0.262), (s * 0.288, 0.259)], 2.6, 0.4, 0.95, (1.0, 0.3), curved=False)
        c.stroke(SKIN_SHADE, [(s * 0.246, 0.334), (s * 0.247, 0.240)], 3.0, 1.0, 0.7, (0.6, 0.6), curved=False)
        # the thumb, folded along the top of the fist: one drawn hook
        c.stroke(SKIN_DEEP, [(s * 0.236, 0.318), (s * 0.262, 0.320), (s * 0.270, 0.330)], 2.6, 0.4, 0.95, (0.8, 0.4))
    c.column(SKIN_SHADE, *sorted((s * 0.284, s * 0.31)), 2.5, 0.6)


def _arm_skin_hand(c, view):
    """The under half of a hand, in shade. Drawn after the sleeve, the cuff and the wrap, so none
    of their paint is left on it."""
    if view in ("front", "back"):
        for s in (1, -1):
            c.mark(SKIN_SHADE, [(s * 0.226, 0.16), (s * 0.31, 0.16), (s * 0.31, 0.258), (s * 0.226, 0.258)], 0, 0.7, curved=False)


def _arm_skin(c, s, view):
    if view == "bottom":
        c.column(SKIN_SHADE, *sorted((s * 0.08, s * 0.31)), 0, 0.75)
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.115, -0.03), (s * 0.20, -0.028), (s * 0.20, 0.03), (s * 0.115, 0.034)], 8, 0.6)
    elif view in ("front", "back"):
        c.band(SKIN_SHADE, 0.16, 0.262, 5, 0.7)
        c.mark(SKIN_LIT, [(s * 0.112, 0.338), (s * 0.168, 0.332), (s * 0.160, 0.306), (s * 0.114, 0.310)], 6, 0.6)


def _fingertips(c):
    """The end-on view of a hand: only the tips show, and they are grubby."""
    c.blob(SKIN_DEEP, (0.005, 0.288), 0.05, 0.05, 6, 0.5)


def paint_arm_left(c, view):
    if view == "xpos":
        return _fingertips(c)
    _arm_skin(c, 1, view)
    # (the torn sleeve stub on the shoulder is its own block and wears the jacket's islands)
    # the wrap: off-white cloth, wound on a slant, grubby at both ends. Every mark stops at the
    # wrap's two edges: a band drawn the island's whole width put bandage cream on the hand.
    c.column(BANDAGE, 0.166, 0.224)
    if view in ("front", "back"):
        k = 1.0 if view == "front" else -1.0
        c.mark(BANDAGE_SHADE, [(0.166, 0.16), (0.224, 0.16), (0.224, 0.256), (0.166, 0.256)], 0, 0.6, curved=False)
        c.stroke(BANDAGE_SHADE, [(0.172, 0.288 + k * 0.050), (0.184, 0.288 - k * 0.050)], 3.0, 0.4, 1.0, (1, 1), curved=False)
        c.stroke(BANDAGE_SHADE, [(0.189, 0.288 + k * 0.050), (0.202, 0.288 - k * 0.050)], 3.2, 0.4, 1.0, (1, 1), curved=False)
        c.stroke(BANDAGE_SHADE, [(0.206, 0.288 + k * 0.050), (0.218, 0.288 - k * 0.050)], 2.8, 0.4, 1.0, (1, 1), curved=False)
    else:
        if view == "bottom":
            c.column(BANDAGE_SHADE, 0.166, 0.224, 0, 0.6)
        c.stroke(BANDAGE_SHADE, [(0.173, -0.060), (0.185, 0.070)], 3.0, 0.4, 1.0, (1, 1), curved=False)
        c.stroke(BANDAGE_SHADE, [(0.190, -0.060), (0.203, 0.070)], 3.2, 0.4, 1.0, (1, 1), curved=False)
        c.stroke(BANDAGE_SHADE, [(0.207, -0.060), (0.218, 0.070)], 2.8, 0.4, 1.0, (1, 1), curved=False)
    c.column(BANDAGE_DIRT, 0.2185, 0.224, 0.8, 0.6)
    c.column(BANDAGE_DIRT, 0.166, 0.1705, 0.8, 0.45)
    c.column(SKIN, 0.2245, 0.31)
    _arm_skin_hand(c, view)
    _hand(c, 1, view)


def paint_arm_right(c, view):
    if view == "xneg":
        return _fingertips(c)
    _arm_skin(c, -1, view)
    # the sleeve, with the folds a rolled cuff pushes up the arm
    c.column(GREEN, -0.172, -0.08)
    if view in ("front", "back"):
        c.mark(GREEN_DARK, [(-0.172, 0.16), (-0.08, 0.16), (-0.08, 0.258), (-0.172, 0.258)], 0, 0.7, curved=False)
        c.mark(GREEN_LIT, [(-0.116, 0.350), (-0.166, 0.346), (-0.164, 0.318), (-0.118, 0.316)], 6, 0.65)
        c.stroke(GREEN_DARK, [(-0.150, 0.342), (-0.142, 0.300), (-0.152, 0.244)], 4.5, 0.6, 0.9)
        c.stroke(GREEN_DARK, [(-0.126, 0.330), (-0.120, 0.290), (-0.128, 0.250)], 3.6, 0.6, 0.8)
    elif view == "top":
        c.mark(GREEN_LIT, [(-0.116, -0.036), (-0.166, -0.034), (-0.166, 0.036), (-0.116, 0.040)], 7, 0.6)
    else:
        c.column(GREEN_DARK, -0.172, -0.08, 0, 0.7)
    c.column(GOLD, -0.200, -0.170)
    c.column(GOLD_DARK, -0.200, -0.194, 0.6, 0.85)
    c.column(GOLD_LIT, -0.184, -0.178, 0.8, 0.8)
    if view in ("front", "back"):
        c.mark(GOLD_DARK, [(-0.200, 0.16), (-0.170, 0.16), (-0.170, 0.250), (-0.200, 0.250)], 0, 0.55, curved=False)
    if view == "bottom":
        c.column(GOLD_DARK, -0.200, -0.170, 0, 0.5)
    # a cord bracelet at the wrist, jade green with a gold knot
    c.column(JADE_DARK, -0.2185, -0.2125)
    if view == "front":
        c.blob(GOLD, (-0.2155, 0.300), 0.0055, 0.0075, 0.3, 1.0)
    _arm_skin_hand(c, view)
    _hand(c, -1, view)


# ---------------------------------------------------------------------------
# THE LEGS. Leather-brown trousers rolled at the ankle, worn pale at the knee, the left knee
# patched with a scrap of the jacket's green. White rubber shoes with a toe cap, laces, scuffs.
# ---------------------------------------------------------------------------
CUFF = (0.046, 0.076)


def paint_leg(c, s, view):
    """`s` is +1 for his left leg, -1 for his right. Each leg's marks are its own."""
    x = lambda v: s * v
    # trousers
    c.band(BROWN, CUFF[1], 0.20)
    if view == "front":
        c.mark(BROWN_LIT, [(x(0.045), 0.150), (x(0.122), 0.152), (x(0.126), 0.100), (x(0.050), 0.096)], 6, 0.75)
        c.stroke(BROWN_DARK, [(x(0.036), 0.170), (x(0.050), 0.136), (x(0.040), 0.090)], 4.0, 0.7, 0.8)
        if s > 0:
            # wholly BELOW the coat hem (0.134 in front) and above the rolled cuff, so it is never
            # half hidden: under the hem it showed as a smudge
            if not QUIET_CLOTH: c.mark(BROWN_DARK, [(0.056, 0.127), (0.112, 0.130), (0.116, 0.084), (0.060, 0.081)], 0.0, 1.0, curved=False)
            if not QUIET_CLOTH: c.mark(GREEN, [(0.060, 0.123), (0.108, 0.126), (0.112, 0.088), (0.064, 0.085)], 0.0, 1.0, curved=False)
            c.stitch(THREAD, [(0.066, 0.117), (0.103, 0.119), (0.106, 0.094), (0.069, 0.091), (0.066, 0.113)], 2.0, 5.0, 4.0, 1.0)
        else:
            c.mark(BROWN_DUST, [(-0.058, 0.136), (-0.112, 0.140), (-0.116, 0.104), (-0.066, 0.100)], 5, 0.5)
    elif view == "back":
        c.stroke(BROWN_DARK, [(x(0.060), 0.168), (x(0.090), 0.130), (x(0.076), 0.092)], 5.0, 0.7, 0.8)
        c.mark(BROWN_DARK, [(x(0.02), 0.20), (x(0.15), 0.20), (x(0.15), 0.150), (x(0.02), 0.146)], 6, 0.5)
    else:
        c.stroke(BROWN_DARK, [(-0.004, 0.180), (0.002, 0.130), (-0.004, 0.082)], 2.6, 0.5, 0.9, (0.8, 0.8))
        c.mark(BROWN_LIT, [(-0.060, 0.140), (-0.020, 0.146), (-0.016, 0.100), (-0.056, 0.098)], 6, 0.5)
    c.band(BROWN_DUST, CUFF[1], 0.094, 3.5, 0.5)
    # the rolled cuff shows the cloth's paler inside
    c.band(BROWN_LIT, CUFF[0], CUFF[1])
    c.band(BROWN_DARK, CUFF[1] - 0.004, CUFF[1] + 0.003, 0.5, 0.9)
    c.band(BROWN_DUST, CUFF[0], CUFF[0] + 0.012, 2.5, 0.6)
    if view == "front":
        c.stroke(BROWN, [(x(0.060), 0.074), (x(0.068), 0.050)], 2.4, 0.4, 0.9, (0.8, 0.5), curved=False)
        c.stroke(BROWN, [(x(0.104), 0.073), (x(0.098), 0.052)], 2.2, 0.4, 0.9, (0.8, 0.5), curved=False)
    # shoes
    c.band(SHOE, 0.0, CUFF[0])
    c.band(SHOE_SHADE, 0.0, 0.019, 0.4, 0.9)
    c.band(SHOE_DIRT, 0.0, 0.008, 1.2, 0.45)
    if view == "front":
        c.stroke(SHOE_SHADE, [(x(0.030), 0.040), (x(0.084), 0.047), (x(0.138), 0.040)], 2.2, 0.4, 1.0, (0.6, 0.6))
        if s < 0:
            c.mark(SHOE_SCUFF, [(-0.044, 0.034), (-0.078, 0.036), (-0.074, 0.024), (-0.048, 0.023)], 2.0, 0.5)
    elif view == "back":
        c.mark(GREEN, [(x(0.066), 0.062), (x(0.102), 0.062), (x(0.100), 0.024), (x(0.068), 0.024)], 0.5, 1.0, curved=False)
        c.stroke(GREEN_DARK, [(x(0.084), 0.060), (x(0.084), 0.026)], 2.0, 0.4, 1.0, (1, 1), curved=False)
    else:
        outer = (view == "xpos") == (s > 0)
        if not outer and s > 0:
            c.mark(SHOE_SCUFF, [(-0.060, 0.040), (-0.020, 0.042), (-0.024, 0.028), (-0.058, 0.026)], 2.6, 0.45)
        # ahead of the cuff the shoe stands higher than the cuff starts: keep that white, or the
        # cuff band lands on the toe box as a brown dart
        c.mark(SHOE, [(-0.17, 0.019), (-0.087, 0.019), (-0.087, 0.075), (-0.17, 0.075)], 0, 1.0, curved=False)
        c.stroke(SHOE_SHADE, [(-0.118, 0.0235), (-0.092, 0.043), (-0.086, 0.055)], 2.0, 0.4, 1.0, (0.7, 0.7))


def paint_leg_top(c, s):
    """Looking down on a foot: the toe cap line, the green tongue, one scuff. The toe is at -y."""
    x = lambda v: s * v
    c.mark(SHOE, [(x(0.0), 0.12), (x(0.17), 0.12), (x(0.17), -0.17), (x(0.0), -0.17)], 0, 1.0, curved=False)
    c.stroke(SHOE_SHADE, [(x(0.034), -0.098), (x(0.084), -0.106), (x(0.136), -0.098)], 2.4, 0.4, 1.0, (0.6, 0.6))
    c.mark(GREEN, [(x(0.068), -0.050), (x(0.100), -0.050), (x(0.098), -0.090), (x(0.070), -0.090)], 0.5, 1.0, curved=False)
    if s > 0:
        c.mark(SHOE_SCUFF, [(0.050, -0.118), (0.092, -0.122), (0.086, -0.136), (0.056, -0.132)], 2.5, 0.45)


# ---------------------------------------------------------------------------
# THE SWATCHES. u across, v up (0 is a lock's root or a collar's base, 1 its tip or its top).
# ---------------------------------------------------------------------------

def paint_horn(c):
    # obsidian: a slate lit plane up one side, a violet edge up the other, growth rings near the root
    c.stroke(HORN_LIT, [(0.30, 0.02), (0.34, 0.50), (0.32, 0.96)], 36, 2.0, 0.9, (0.9, 0.3))
    c.stroke(HORN_VIOLET, [(0.74, 0.0), (0.76, 0.48), (0.74, 0.92)], 26, 2.5, 0.85, (0.9, 0.3))
    c.stroke(HAIR_DEEP, [(0.0, 0.085), (0.5, 0.100), (1.0, 0.085)], 9, 0.6, 0.9, (1, 1))
    c.stroke(HAIR_DEEP, [(0.0, 0.215), (0.5, 0.235), (1.0, 0.215)], 8, 0.6, 0.85, (1, 1))
    c.stroke(HAIR_DEEP, [(0.0, 0.370), (0.5, 0.395), (1.0, 0.370)], 6.5, 0.6, 0.8, (1, 1))
    c.stroke(HAIR_DEEP, [(0.0, 0.545), (0.5, 0.570), (1.0, 0.545)], 5, 0.6, 0.7, (1, 1))


def paint_collar_out(c):
    # the popped collar from outside: lit along its top, creased where it folds up off the
    # shoulders, a row of stitches under the piping. u = 0 is his right tip, 1 his left tip.
    c.mark(GREEN_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.62), (0.70, 0.52), (0.45, 0.60), (0.20, 0.50), (0.0, 0.60)], 9, 0.7)
    c.mark(GREEN_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.20), (0.74, 0.28), (0.50, 0.20), (0.26, 0.30), (0.0, 0.22)], 7, 0.8)
    c.stroke(GREEN_DARK, [(0.22, 0.10), (0.235, 0.50), (0.225, 0.86)], 6, 0.8, 0.8, (0.8, 0.2))
    c.stroke(GREEN_DARK, [(0.50, 0.08), (0.505, 0.46), (0.50, 0.80)], 5, 0.8, 0.8, (0.8, 0.2))
    c.stroke(GREEN_DARK, [(0.775, 0.10), (0.765, 0.52), (0.78, 0.88)], 6, 0.8, 0.8, (0.8, 0.2))
    c.stitch(GREEN_DEEP, [(0.02, 0.86), (0.50, 0.87), (0.98, 0.86)], 1.8, 7.0, 5.0)


def paint_collar_in(c):
    c.mark(GREEN_DEEP, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.55), (0.5, 0.45), (0.0, 0.55)], 8, 0.9)
    c.stroke(GREEN, [(0.02, 0.84), (0.50, 0.80), (0.98, 0.84)], 12, 2.0, 0.6, (1, 1))


def paint_gold(c):
    # piping: a pale top edge, a dark under edge, three worn dull places, each its own length
    c.mark(GOLD_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.70), (0.0, 0.70)], 1.2, 0.85, curved=False)
    c.mark(GOLD_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.24), (0.0, 0.24)], 1.2, 0.9, curved=False)
    c.mark(GOLD_DARK, [(0.12, 0.30), (0.22, 0.30), (0.21, 0.74), (0.13, 0.72)], 2.0, 0.4)
    c.mark(GOLD_DARK, [(0.48, 0.28), (0.63, 0.30), (0.62, 0.70), (0.50, 0.72)], 2.0, 0.35)
    c.mark(GOLD_DARK, [(0.83, 0.30), (0.90, 0.30), (0.90, 0.74), (0.84, 0.70)], 2.0, 0.4)


def paint_jade(c):
    c.blob(JADE_DARK, (0.5, 0.42), 0.6, 0.34, 2.0, 0.8)
    c.blob(JADE_LIT, (0.38, 0.66), 0.16, 0.12, 1.0, 0.95)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        return I(name, base, SWATCHES[name][2:4])

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    side = mix(SKIN, SKIN_SHADE, 0.45)
    c = I("head.xpos", side); _head_side(c, 1); done[c.name] = c
    c = I("head.xneg", side); _head_side(c, -1); done[c.name] = c
    c = I("head.back", SKIN_SHADE); paint_head_back(c); done[c.name] = c
    # Seen from above the head block is under the hair; what shows is the shaved strip on his
    # left and the tops of the ears and the nose, which are SKIN (hair paint here blackened them).
    c = I("head.top", mix(SKIN, SKIN_LIT, 0.5))
    c.mark(STUBBLE, [(0.130, -0.21), (0.161, -0.21), (0.161, 0.21), (0.130, 0.21)], 0.5, 1.0, curved=False)
    done[c.name] = c
    c = I("head.bottom", SKIN_DEEP); done[c.name] = c

    c = I("torso.front", GREEN); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", GREEN); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", GREEN); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", GREEN); _torso_side(c); done[c.name] = c
    c = I("torso.top", GREEN); paint_torso_top(c); done[c.name] = c

    for view in GROUPS["armL"]:
        c = I("armL." + view, SKIN); paint_arm_left(c, view); done[c.name] = c
    for view in GROUPS["armR"]:
        c = I("armR." + view, SKIN); paint_arm_right(c, view); done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, BROWN)
            if view == "top":
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c

    # hair is three flat tones and no drawing (see build_hair in the model script)
    c = swatch("hair", HAIR); done[c.name] = c
    c = swatch("hair_top", HAIR_TOP); done[c.name] = c
    c = swatch("hair_under", HAIR_DEEP); done[c.name] = c
    c = swatch("gold_tone", GOLD_DARK); done[c.name] = c
    c = swatch("skin_tone", SKIN_SHADE); done[c.name] = c
    c = swatch("belt_tone", BROWN_DARK); done[c.name] = c
    c = swatch("cuff_tone", BROWN_LIT); done[c.name] = c
    c = swatch("wrap_tone", BANDAGE_SHADE); done[c.name] = c
    c = swatch("horn", HORN); paint_horn(c); done[c.name] = c
    c = swatch("collar_out", GREEN); paint_collar_out(c); done[c.name] = c
    c = swatch("collar_in", GREEN_DARK); paint_collar_in(c); done[c.name] = c
    c = swatch("gold", GOLD); paint_gold(c); done[c.name] = c
    c = swatch("lining", GREEN_DEEP); done[c.name] = c
    c = swatch("sole", SHOE_SCUFF); done[c.name] = c
    c = swatch("jade", JADE); paint_jade(c); done[c.name] = c

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
    atlas = Image.new("RGB", (size, size), _rgb(GREEN_DEEP))
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
