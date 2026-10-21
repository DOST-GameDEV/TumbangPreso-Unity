"""Paint the atlas of the Rafi (displayed: Ilyas) redesign PROTOTYPE, by hand, in the house style.

    py -3 tools/author_character_redesign_rafi_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/rafi/rafi-redesign-atlas.png. The model is built
by tools/author_character_redesign_rafi.py, which imports this file for the LAYOUT only (PIL is
imported inside the painting calls, Blender's Python has none). Paint first, then build.

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 13: the owner asked for the rest of the cast to
follow Dante's rework. This is Rafi's own copy of Dante's textures script, rewritten for him. It
imports nothing from Dante's files. Nothing in the game loads what it writes and team-rafi.glb is
not touched.

WHO HE IS (tools/build_rafi_voxel.py, docs/CHARACTER_MODEL_METHOD.md, written from his own
rework). A Visayan islander: bare tattooed skin IS his base garment, a bahag in a muted sea teal
with a sand stripe and a cream thread, one metal (silver), a putong tied round a big mop of black
hair that is gathered into one tail, a shark tooth necklace, tsinelas. His face is ink only: two
flat black eyes and one level bar of a mouth, no brows, no glint. All of that is kept. What is
added is paint ON it: a lit middle and edges that turn away on the face, the band's cast shadow
on the forehead, shade under the pectorals, weave and folds on the cloth.

THE TATTOOS ARE THE ORIGINAL'S OWN ROUTES, MARK FOR MARK. Every polygon below is copied from the
decal tables of tools/build_rafi_voxel.py (CHEST_DECALS, ARM_DECALS_LEFT, CUFF_DECALS,
LEG_DECALS) with its own numbers, in that file's TABLE space, and is carried to this model's
metres by the same remap that builder applies (`RY`, and for the arms `AX`, the shortening the
shipped .glb carries). Nothing is stamped by a loop round a limb, nothing is added, the ink is
black. Two honest notes:
  * the builder's layer-3 polygons are drawn in the SAME ink slot as the mark under them, so in
    the game today they show nothing (a diamond inside a diamond reads as one solid diamond).
    They are left out here, which keeps today's look and invents no negative space;
  * the right arm is the left arm mirrored, because that is what the builder does
    (`ARM_DECALS_RIGHT` is built from the left table); the two cuffs are engraved differently.
Where one box covered another's ink in the original (the pectoral slabs over the chest, the belt
over the lower wing and the top of the thighs), the same thing happens here: the chest is drawn,
the pectoral's skin is laid over it, then the pectoral's own marks.

THE ONE MECHANISM is Dante's and the beggar's: `Toon.shader` remaps a UV to a palette slot only in
Unity atlas rows 0 to 7 and samples the texture above them, so every island lives in the TOP half
of the file and the bottom half is left flat.

HOW AN ISLAND IS LAID OUT. A body part is seen from up to six sides and each side is one island,
a flat orthographic view of the part in MODEL METRES: front and back are (x, z), xpos and xneg
are (y, z), top and bottom are (x, y). +x is HIS left, -y is the way he faces, z is up, the feet
are on zero. Loose blocks (hair, the putong, piping, teeth) take a SWATCH or one flat tone.
⚠️ EACH ARM IS TWO GROUPS, the upper arm and the forearm with its hand. The two blocks overlap at
the elbow; on one shared island the armlet's outer rail would be drawn on both, and a bent elbow
would show it twice. So each block has its own drawing and the joint is clean skin.

HIS COLOURS ARE THE ORIGINAL'S (person_rafi.asset, the `sea` cloth): skin b2764a and 8f5a36, hair
1d191c with the lit tone 4a3a3e, sea teal 2b6664 and 1a4543, putong 357c77, sand bdae84, cream
f2e2bc, silver c9cfd4 and 7e868e, tattoo ink 17161c, face ink 181418, sandal bed 9a6538. Painted
tones are steps of those. ROLE HUES (#f87020, #0080e8): every cloth colour is checked; skin is
exempt as for the cast, and so is the sandal bed, a 9 mm strip in the original's own brown.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "rafi"
ATLAS_NAME = "rafi-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is the original palette slot, unchanged.
# ---------------------------------------------------------------------------
SKIN = "b2764a"; SKIN_LIT = "c58a5c"; SKIN_SHADE = "8f5a36"; SKIN_DEEP = "6d4126"
HAIR = "1d191c"; HAIR_TOP = "4a3a3e"; HAIR_DEEP = "110e10"
INK = "181418"; TATTOO = "17161c"
TEAL = "2b6664"; TEAL_DARK = "1a4543"; TEAL_LIT = "3a807c"; TEAL_DEEP = "112e2d"
PUTONG = "357c77"; PUTONG_LIT = "4a968f"; PUTONG_DARK = "245853"
SAND = "bdae84"; SAND_DARK = "968960"
CREAM = "f2e2bc"; CREAM_SHADE = "cdb98a"; CREAM_DIRT = "a8946a"
SILVER = "c9cfd4"; SILVER_LIT = "eef2f5"; SILVER_DARK = "7e868e"
BED = "9a6538"; BED_DARK = "70461f"

CLOTH_HEXES = [HAIR, HAIR_TOP, TEAL, TEAL_DARK, TEAL_LIT, PUTONG, PUTONG_LIT, PUTONG_DARK, SAND,
               SAND_DARK, CREAM, CREAM_SHADE, CREAM_DIRT, SILVER, SILVER_LIT, SILVER_DARK]

# ---------------------------------------------------------------------------
# THE ORIGINAL'S SPACE. The builder's body tables are authored against joint heights it then
# remaps (`_remap_y`), and the shipped .glb's arms are 0.6525 of the table's length from the
# shoulder (measured: the cuff sits at 0.202 to 0.221, the hand ends at 0.285). A tattoo
# polygon typed in table numbers lands, through these two, exactly where it is in the game.
# ---------------------------------------------------------------------------
WAS_HIPS, WAS_NECK = 0.232, 0.445
NOW_HIPS, NOW_NECK = 0.176, 0.343
SHOULDER_X, ARM_SCALE, ARM_DROP = 0.0999, 0.6525, 0.112


def RY(y):
    """A table height below the neck to this model's height."""
    if y <= WAS_HIPS:
        return y / WAS_HIPS * NOW_HIPS
    return NOW_HIPS + (y - WAS_HIPS) / (WAS_NECK - WAS_HIPS) * (NOW_NECK - NOW_HIPS)


def AX(x):
    """A table distance along an arm to this model's."""
    return math.copysign(SHOULDER_X + (abs(x) - SHOULDER_X) * ARM_SCALE, x)


def body(points):
    return [(a, RY(b)) for a, b in points]


def arm(points):
    return [(AX(a), b - ARM_DROP) for a, b in points]


def arm_top(points):
    return [(AX(a), b) for a, b in points]


def Q(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def D(cx, cy, rx, ry):
    return [(cx, cy - ry), (cx + rx, cy), (cx, cy + ry), (cx - rx, cy)]


# heights of things both scripts need, in model metres
BELT_LOW = (RY(0.236), RY(0.264))      # the lower tier, sea teal
BELT_SEAM = (RY(0.263), RY(0.269))     # the dark silver seam between the tiers
BELT_HIGH = (RY(0.268), RY(0.296))     # the upper tier, the cloth's shadow tone
CUFF_X = (AX(0.256), AX(0.286))        # the silver cuff along the arm
ELBOW_X = 0.176                        # between the armlet (ends 0.174) and the fern (starts 0.178)

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048).
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.25, 0.25, 0.32, 0.68), 1300),
        "back": ((-0.25, 0.25, 0.32, 0.68), 420),
        "xpos": ((-0.19, 0.19, 0.32, 0.68), 760),
        "xneg": ((-0.19, 0.19, 0.32, 0.68), 760),
        "top": ((-0.25, 0.25, -0.19, 0.19), 240),
        "bottom": ((-0.25, 0.25, -0.19, 0.19), 240),
    },
    "torso": {
        "front": ((-0.16, 0.16, 0.16, 0.36), 1350),
        "back": ((-0.16, 0.16, 0.16, 0.36), 1350),
        "xpos": ((-0.13, 0.13, 0.16, 0.36), 900),
        "xneg": ((-0.13, 0.13, 0.16, 0.36), 900),
        "top": ((-0.16, 0.16, -0.13, 0.13), 520),
    },
    "flap": {
        "front": ((-0.075, 0.075, 0.06, 0.20), 1150),
        "back": ((-0.075, 0.075, 0.06, 0.20), 1150),
    },
    "uarmL": {
        "front": ((0.09, 0.20, 0.21, 0.365), 1300),
        "back": ((0.09, 0.20, 0.21, 0.365), 1300),
        "top": ((0.09, 0.20, -0.08, 0.08), 1300),
        "bottom": ((0.09, 0.20, -0.08, 0.08), 700),
    },
    "uarmR": {
        "front": ((-0.20, -0.09, 0.21, 0.365), 1300),
        "back": ((-0.20, -0.09, 0.21, 0.365), 1300),
        "top": ((-0.20, -0.09, -0.08, 0.08), 1300),
        "bottom": ((-0.20, -0.09, -0.08, 0.08), 700),
    },
    "farmL": {
        "front": ((0.15, 0.30, 0.21, 0.365), 1300),
        "back": ((0.15, 0.30, 0.21, 0.365), 1300),
        "top": ((0.15, 0.30, -0.08, 0.08), 1300),
        "bottom": ((0.15, 0.30, -0.08, 0.08), 700),
    },
    "farmR": {
        "front": ((-0.30, -0.15, 0.21, 0.365), 1300),
        "back": ((-0.30, -0.15, 0.21, 0.365), 1300),
        "top": ((-0.30, -0.15, -0.08, 0.08), 1300),
        "bottom": ((-0.30, -0.15, -0.08, 0.08), 700),
    },
    "legL": {
        "front": ((0.0, 0.17, 0.0, 0.21), 1250),
        "back": ((0.0, 0.17, 0.0, 0.21), 900),
        "xpos": ((-0.15, 0.10, 0.0, 0.21), 1000),
        "xneg": ((-0.15, 0.10, 0.0, 0.21), 800),
        "top": ((0.0, 0.17, -0.15, 0.10), 1100),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.21), 1250),
        "back": ((-0.17, 0.0, 0.0, 0.21), 900),
        "xpos": ((-0.15, 0.10, 0.0, 0.21), 800),
        "xneg": ((-0.15, 0.10, 0.0, 0.21), 1000),
        "top": ((-0.17, 0.0, -0.15, 0.10), 1100),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group. The face
# is drawn on the front island, so the head's front reaches round its cut corners to the ears.
VIEW_BIAS = {("head", "front"): 1.5, ("head", "top"): 0.8, ("torso", "front"): 1.15, ("torso", "back"): 1.15,
             ("legL", "top"): 0.9, ("legR", "top"): 0.9}

# name -> (px wide, px high at 2048, metres wide, metres high). u runs across, v UP.
_TONE = (36, 36, 0.04, 0.04)
SWATCHES = {
    "hair": _TONE, "hair_top": _TONE, "hair_under": _TONE,
    "skin_tone": _TONE, "skin_shade_tone": _TONE, "skin_lit_tone": _TONE,
    "teal_tone": _TONE, "teal_dark_tone": _TONE, "teal_deep_tone": _TONE,
    "putong_tone": _TONE, "putong_lit_tone": _TONE, "putong_dark_tone": _TONE,
    "silver_tone": _TONE, "silver_lit_tone": _TONE, "silver_dark_tone": _TONE,
    "cream_tone": _TONE, "cream_shade_tone": _TONE, "bed_tone": _TONE, "bed_dark_tone": _TONE,
    "putong": (760, 60, 1.52, 0.048),
    "tooth": (64, 96, 0.036, 0.040),
}

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin.

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
            _LAYOUT[(size, "scale")] = 1.0 - 0.02 * step
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

    def ink(self, points, colour=TATTOO):
        """One tattoo mark: a hard-edged polygon in black ink, its corners as typed."""
        return self.mark(colour, points, 0.22, 1.0, curved=False)

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE HEAD. His face is the cast's ink face: two flat black eyes, one level bar of a mouth, no
# brows, no glint (owner, 2026-09-25: "everyone else has just black eyes and just black mouth").
# So the eyes and mouth stay solid ink in the original's own places (the eyes a third bigger,
# rule 12), on flat skin: one soft lit patch, edges that turn away faintly, and the putong's
# cast shadow across the forehead as one flat tone. Nothing else is drawn on the face.
# ---------------------------------------------------------------------------

def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (rule 12). v01 to v07 modelled this face in paint: eye sockets,
    # a dark under each eye, a nose bridge and its shadow, a lip shadow, two steps of jaw shade,
    # shade under the temple locks, and a nose wedge in the mesh. Owner, 2026-10-05, on the whole
    # redesigned cast: "the faces look too realistic and look too human, like it lost its charm,
    # the characters have eyebags etc.. they need to be more cutesy". All of that is gone.
    # What is left: flat skin with one very soft lit patch, the faint turn of the block's two
    # edges, the putong's shadow as ONE flat tone, and his own two ink eyes and level
    # mouth. He has no face tattoo today (the builder's BANGUT is empty).
    side = mix(SKIN, SKIN_SHADE, 0.28)
    c.blob(SKIN_LIT, (0.0, 0.465), 0.120, 0.085, 26, 0.32)
    c.mark(side, [(-0.26, 0.70), (-0.162, 0.70), (-0.152, 0.54), (-0.162, 0.40), (-0.148, 0.33), (-0.26, 0.31)], 10, 0.8)
    c.mark(side, [(0.26, 0.70), (0.162, 0.70), (0.153, 0.55), (0.163, 0.41), (0.149, 0.33), (0.26, 0.31)], 10, 0.8)
    # the putong's cast shadow, one flat tone: it rides up the brow in the middle, drops to the sides
    c.mark(SKIN_SHADE, [(-0.26, 0.70), (0.26, 0.70), (0.26, 0.540), (0.150, 0.551), (0.060, 0.558), (-0.050, 0.559),
                        (-0.150, 0.550), (-0.26, 0.540)], 1.2, 0.72, curved=False)
    # NO BLUSH. v08 had a round one under each eye; the owner: "reserve the blush for the female
    # characters". His cheeks are plain skin.
    # THE EYES. The original's flat ink blocks in the original's places (centred 0.080, 0.475),
    # a third bigger: 64 by 51 mm against 48 by 38, the two lower corners cut as the original
    # cuts them. One flat fill, no glint, no second colour: the original has neither.
    c.mark(INK, [(-0.112, 0.5005), (-0.048, 0.5005), (-0.048, 0.4550), (-0.0535, 0.4495), (-0.1065, 0.4495), (-0.112, 0.4550)],
           0.25, 1.0, curved=False)
    c.mark(INK, [(0.048, 0.5005), (0.112, 0.5005), (0.112, 0.4550), (0.1065, 0.4495), (0.0535, 0.4495), (0.048, 0.4550)],
           0.25, 1.0, curved=False)
    # THE MOUTH: the outline measured off team-rafi.glb (slot 8 on the head mesh), vertex for
    # vertex: x -0.030 to 0.030, 0.405 to 0.415. The eyes above are the measured outline too
    # (x 0.056 to 0.104, 0.456 to 0.494, a 4 mm cut on each lower corner, the same both sides),
    # scaled 4/3 about each eye's own centre. His level top edge IS his calm look; it is kept.
    c.mark(INK, [(-0.030, 0.415), (0.030, 0.415), (0.030, 0.405), (-0.030, 0.405)], 0.25, 1.0, curved=False)


def paint_head_side(c):
    """A side of the head, the same drawing for both: hair covers everything above the ear.
    Kept as plain as the face (rule 12): a faint turn toward the back, the band's flat shadow,
    and the ear's cup as one soft dark shape. No jaw contour."""
    c.mark(mix(SKIN, SKIN_SHADE, 0.6), [(0.04, 0.70), (0.20, 0.70), (0.20, 0.31), (0.08, 0.31), (0.05, 0.46)], 14, 0.45)
    c.band(SKIN_SHADE, 0.545, 0.70, 1.2, 0.72)
    c.blob(SKIN_LIT, (0.012, 0.458), 0.026, 0.036, 2, 0.4)
    c.blob(SKIN_SHADE, (0.010, 0.458), 0.011, 0.020, 2.0, 0.85)


# ---------------------------------------------------------------------------
# THE TORSO. Bare skin: lit across the collarbones and the pectorals, shade down the flanks and
# under the pectoral slabs, then the tattoos in the builder's own order, then the belt.
# ---------------------------------------------------------------------------

def _belt(c, thread):
    """The bahag's two-tier waist as it meets itself round the body (the tiers are geometry)."""
    c.band(TEAL, *BELT_LOW)
    c.band(TEAL_LIT, BELT_LOW[1] - 0.0055, BELT_LOW[1] - 0.0025, 0.4, 0.7)
    c.band(TEAL_DARK, BELT_LOW[0], BELT_LOW[0] + 0.004, 0.6, 0.8)
    if thread:
        # the cream thread of the weave, on the front and the back as the original draws it
        c.band(CREAM, RY(0.256), RY(0.261))
    c.band(TEAL_DARK, *BELT_HIGH)
    c.band(TEAL, BELT_HIGH[1] - 0.0045, BELT_HIGH[1] - 0.002, 0.4, 0.55)
    c.band(TEAL_DEEP, BELT_HIGH[0], BELT_HIGH[0] + 0.004, 0.6, 0.7)
    c.band(SILVER_DARK, *BELT_SEAM)


def paint_torso_front(c):
    c.mark(SKIN_LIT, [(-0.120, 0.338), (0.120, 0.338), (0.110, 0.314), (0.0, 0.318), (-0.110, 0.314)], 8, 0.5)
    c.mark(SKIN_SHADE, [(-0.17, 0.34), (-0.112, 0.33), (-0.100, 0.26), (-0.104, 0.17), (-0.17, 0.17)], 7, 0.7)
    c.mark(SKIN_SHADE, [(0.17, 0.34), (0.113, 0.33), (0.101, 0.26), (0.105, 0.17), (0.17, 0.17)], 7, 0.7)
    # the shade the pectoral slabs throw on the belly
    c.mark(SKIN_SHADE, [(-0.124, 0.256), (0.124, 0.256), (0.118, 0.244), (0.0, 0.247), (-0.118, 0.244)], 2.2, 0.75)

    # === the chest box, front (CHEST_DECALS 'chest' 'front'), collar and clavicle ===
    c.ink(body(D(0.000, 0.422, 0.016, 0.016)))                                   # throat solar star
    c.ink(body([(-0.003, 0.438), (0.000, 0.442), (0.003, 0.438)]))               # its four rays
    c.ink(body([(-0.003, 0.406), (0.003, 0.406), (0.000, 0.400)]))
    c.ink(body([(0.016, 0.420), (0.024, 0.422), (0.016, 0.424)]))
    c.ink(body([(-0.016, 0.420), (-0.016, 0.424), (-0.024, 0.422)]))
    c.ink(body([(0.034, 0.424), (0.124, 0.434), (0.124, 0.428), (0.034, 0.418)]))    # clavicle wing rails
    c.ink(body([(0.034, 0.406), (0.124, 0.416), (0.124, 0.410), (0.034, 0.400)]))
    c.ink(body([(-0.034, 0.424), (-0.034, 0.418), (-0.124, 0.428), (-0.124, 0.434)]))
    c.ink(body([(-0.034, 0.406), (-0.034, 0.400), (-0.124, 0.410), (-0.124, 0.416)]))
    c.ink(body(D(0.052, 0.418, 0.008, 0.005)))                                   # the diamond chain between them
    c.ink(body(D(0.076, 0.421, 0.008, 0.005)))
    c.ink(body(D(0.100, 0.424, 0.008, 0.005)))
    c.ink(body(D(-0.052, 0.418, 0.008, 0.005)))
    c.ink(body(D(-0.076, 0.421, 0.008, 0.005)))
    c.ink(body(D(-0.100, 0.424, 0.008, 0.005)))
    c.ink(body([(0.046, 0.425), (0.054, 0.434), (0.062, 0.427)]))                # sawtooth fringe
    c.ink(body([(0.070, 0.428), (0.078, 0.437), (0.086, 0.430)]))
    c.ink(body([(0.094, 0.431), (0.102, 0.440), (0.110, 0.433)]))
    c.ink(body([(-0.062, 0.427), (-0.054, 0.434), (-0.046, 0.425)]))
    c.ink(body([(-0.086, 0.430), (-0.078, 0.437), (-0.070, 0.428)]))
    c.ink(body([(-0.110, 0.433), (-0.102, 0.440), (-0.094, 0.431)]))
    # === sternum and abdomen ===
    c.ink(body(Q(-0.007, 0.250, 0.007, 0.412)))                                  # the central river column
    c.ink(body(D(0.000, 0.362, 0.014, 0.014)))                                   # mid-sternum diamond
    c.ink(body(D(0.000, 0.320, 0.016, 0.016)))                                   # navel sunburst
    c.ink(body([(-0.003, 0.336), (0.000, 0.344), (0.003, 0.336)]))
    c.ink(body([(-0.003, 0.304), (0.003, 0.304), (0.000, 0.296)]))
    c.ink(body([(0.016, 0.318), (0.024, 0.320), (0.016, 0.322)]))
    c.ink(body([(-0.016, 0.318), (-0.016, 0.322), (-0.024, 0.320)]))
    c.ink(body([(0.014, 0.344), (0.116, 0.326), (0.116, 0.336), (0.014, 0.354)]))    # upper oblique wing
    c.ink(body([(-0.014, 0.344), (-0.014, 0.354), (-0.116, 0.336), (-0.116, 0.326)]))
    c.ink(body(D(0.065, 0.340, 0.010, 0.008)))
    c.ink(body(D(-0.065, 0.340, 0.010, 0.008)))
    c.ink(body([(0.014, 0.284), (0.116, 0.266), (0.116, 0.276), (0.014, 0.294)]))    # lower oblique wing (under the belt)
    c.ink(body([(-0.014, 0.284), (-0.014, 0.294), (-0.116, 0.276), (-0.116, 0.266)]))
    c.ink(body(D(0.065, 0.280, 0.010, 0.008)))
    c.ink(body(D(-0.065, 0.280, 0.010, 0.008)))

    # === the pectoral slabs stand over the chest's ink: their own skin first, then their marks ===
    c.mark(SKIN, body(Q(0.008, 0.334, 0.122, 0.404)), 0.0, 1.0, curved=False)
    c.mark(SKIN, body(Q(-0.122, 0.334, -0.008, 0.404)), 0.0, 1.0, curved=False)
    c.mark(SKIN_LIT, [(0.020, 0.306), (0.112, 0.306), (0.108, 0.288), (0.024, 0.286)], 5, 0.55)
    c.mark(SKIN_LIT, [(-0.020, 0.306), (-0.112, 0.306), (-0.108, 0.288), (-0.024, 0.286)], 5, 0.55)
    c.mark(SKIN_SHADE, [(0.010, 0.270), (0.120, 0.272), (0.120, 0.256), (0.010, 0.256)], 3.0, 0.5)
    c.mark(SKIN_SHADE, [(-0.010, 0.270), (-0.120, 0.272), (-0.120, 0.256), (-0.010, 0.256)], 3.0, 0.5)
    # left pectoral: the matmata eye, its rays, the contour crescent, the sternum teeth, the lower teeth
    c.ink(body(D(0.064, 0.360, 0.018, 0.018)))
    c.ink(body([(0.061, 0.378), (0.064, 0.388), (0.067, 0.378)]))
    c.ink(body([(0.061, 0.342), (0.067, 0.342), (0.064, 0.336)]))
    c.ink(body([(0.082, 0.357), (0.092, 0.360), (0.082, 0.363)]))
    c.ink(body([(0.046, 0.357), (0.046, 0.363), (0.036, 0.360)]))
    c.ink(body([(0.038, 0.346), (0.116, 0.390), (0.110, 0.398), (0.030, 0.356)]))
    c.ink(body([(0.012, 0.348), (0.026, 0.358), (0.012, 0.366)]))
    c.ink(body([(0.012, 0.372), (0.026, 0.380), (0.012, 0.388)]))
    c.ink(body([(0.012, 0.392), (0.024, 0.398), (0.012, 0.402)]))
    c.ink(body([(0.040, 0.336), (0.048, 0.344), (0.056, 0.336)]))
    c.ink(body([(0.068, 0.336), (0.076, 0.344), (0.084, 0.336)]))
    # right pectoral
    c.ink(body(D(-0.064, 0.360, 0.018, 0.018)))
    c.ink(body([(-0.067, 0.378), (-0.064, 0.388), (-0.061, 0.378)]))
    c.ink(body([(-0.067, 0.342), (-0.061, 0.342), (-0.064, 0.336)]))
    c.ink(body([(-0.082, 0.357), (-0.082, 0.363), (-0.092, 0.360)]))
    c.ink(body([(-0.046, 0.357), (-0.036, 0.360), (-0.046, 0.363)]))
    c.ink(body([(-0.038, 0.346), (-0.030, 0.356), (-0.110, 0.398), (-0.116, 0.390)]))
    c.ink(body([(-0.012, 0.348), (-0.012, 0.366), (-0.026, 0.358)]))
    c.ink(body([(-0.012, 0.372), (-0.012, 0.388), (-0.026, 0.380)]))
    c.ink(body([(-0.012, 0.392), (-0.012, 0.402), (-0.024, 0.398)]))
    c.ink(body([(-0.056, 0.336), (-0.048, 0.344), (-0.040, 0.336)]))
    c.ink(body([(-0.084, 0.336), (-0.076, 0.344), (-0.068, 0.336)]))

    _belt(c, True)
    # the medallion: a silver plate, lit along its top, worn dark along its foot, a sea teal inset
    lo, hi = RY(0.240), RY(0.300)
    c.mark(SILVER, [(-0.034, lo), (0.034, lo), (0.034, hi), (-0.034, hi)], 0.3, 1.0, curved=False)
    c.mark(SILVER_LIT, [(-0.030, hi - 0.003), (0.018, hi - 0.003), (0.008, hi - 0.009), (-0.030, hi - 0.009)], 0.8, 0.9)
    c.mark(SILVER_DARK, [(-0.034, lo), (0.034, lo), (0.034, lo + 0.007), (-0.034, lo + 0.007)], 1.0, 0.7, curved=False)
    c.mark(TEAL_DARK, [(-0.018, RY(0.254)), (0.018, RY(0.254)), (0.018, RY(0.286)), (-0.018, RY(0.286))], 0.3, 1.0, curved=False)
    c.mark(TEAL, [(-0.015, RY(0.257)), (0.015, RY(0.257)), (0.015, RY(0.283)), (-0.015, RY(0.283))], 0.3, 1.0, curved=False)
    c.mark(TEAL_LIT, [(-0.012, RY(0.281)), (0.004, RY(0.281)), (0.000, RY(0.275)), (-0.012, RY(0.275))], 0.6, 0.8)


def paint_torso_back(c):
    c.mark(SKIN_LIT, [(-0.118, 0.338), (-0.030, 0.336), (-0.036, 0.300), (-0.112, 0.304)], 8, 0.45)
    c.mark(SKIN_LIT, [(0.118, 0.338), (0.030, 0.336), (0.036, 0.298), (0.112, 0.302)], 8, 0.45)
    c.mark(SKIN_SHADE, [(-0.17, 0.34), (-0.114, 0.33), (-0.102, 0.26), (-0.106, 0.17), (-0.17, 0.17)], 7, 0.7)
    c.mark(SKIN_SHADE, [(0.17, 0.34), (0.115, 0.33), (0.103, 0.26), (0.107, 0.17), (0.17, 0.17)], 7, 0.7)
    c.stroke(SKIN_SHADE, [(0.0, 0.336), (0.001, 0.280), (0.0, 0.228)], 16, 3.0, 0.5, (0.8, 0.8))
    # === the dakag: the spine column and the two stacked diamond shields ===
    c.ink(body(Q(-0.006, 0.246, 0.006, 0.438)))
    c.ink(body([(0.000, 0.434), (0.038, 0.392), (0.026, 0.392), (0.000, 0.420)]))
    c.ink(body([(0.038, 0.392), (0.000, 0.350), (0.000, 0.362), (0.026, 0.392)]))
    c.ink(body([(0.000, 0.434), (0.000, 0.420), (-0.026, 0.392), (-0.038, 0.392)]))
    c.ink(body([(-0.038, 0.392), (-0.026, 0.392), (0.000, 0.362), (0.000, 0.350)]))
    c.ink(body([(0.000, 0.410), (0.018, 0.392), (0.012, 0.392), (0.000, 0.400)]))
    c.ink(body([(0.018, 0.392), (0.000, 0.372), (0.000, 0.380), (0.012, 0.392)]))
    c.ink(body([(0.000, 0.410), (0.000, 0.400), (-0.012, 0.392), (-0.018, 0.392)]))
    c.ink(body([(-0.018, 0.392), (-0.012, 0.392), (0.000, 0.380), (0.000, 0.372)]))
    c.ink(body([(0.000, 0.344), (0.034, 0.298), (0.024, 0.298), (0.000, 0.330)]))
    c.ink(body([(0.034, 0.298), (0.000, 0.252), (0.000, 0.266), (0.024, 0.298)]))
    c.ink(body([(0.000, 0.344), (0.000, 0.330), (-0.024, 0.298), (-0.034, 0.298)]))
    c.ink(body([(-0.034, 0.298), (-0.024, 0.298), (0.000, 0.266), (0.000, 0.252)]))
    # trapezius wedges
    c.ink(body([(0.038, 0.418), (0.126, 0.438), (0.126, 0.424), (0.038, 0.410)]))
    c.ink(body([(-0.126, 0.438), (-0.038, 0.418), (-0.038, 0.410), (-0.126, 0.424)]))
    # scapular wing bands: two rails and a diamond chain, his left then his right
    c.ink(body([(0.036, 0.408), (0.126, 0.418), (0.126, 0.412), (0.036, 0.402)]))
    c.ink(body([(0.036, 0.384), (0.126, 0.394), (0.126, 0.388), (0.036, 0.378)]))
    c.ink(body(D(0.054, 0.396, 0.008, 0.006)))
    c.ink(body(D(0.081, 0.399, 0.008, 0.006)))
    c.ink(body(D(0.108, 0.402, 0.008, 0.006)))
    c.ink(body([(-0.036, 0.408), (-0.036, 0.402), (-0.126, 0.412), (-0.126, 0.418)]))
    c.ink(body([(-0.036, 0.384), (-0.036, 0.378), (-0.126, 0.388), (-0.126, 0.394)]))
    c.ink(body(D(-0.054, 0.396, 0.008, 0.006)))
    c.ink(body(D(-0.081, 0.399, 0.008, 0.006)))
    c.ink(body(D(-0.108, 0.402, 0.008, 0.006)))
    # the hawk wings, two tiers
    c.ink(body([(0.034, 0.366), (0.124, 0.344), (0.124, 0.334), (0.034, 0.356)]))
    c.ink(body([(-0.034, 0.366), (-0.034, 0.356), (-0.124, 0.334), (-0.124, 0.344)]))
    c.ink(body(D(0.075, 0.350, 0.010, 0.007)))
    c.ink(body(D(-0.075, 0.350, 0.010, 0.007)))
    c.ink(body([(0.034, 0.312), (0.124, 0.290), (0.124, 0.280), (0.034, 0.302)]))
    c.ink(body([(-0.034, 0.312), (-0.034, 0.302), (-0.124, 0.280), (-0.124, 0.290)]))
    c.ink(body(D(0.075, 0.296, 0.010, 0.007)))
    c.ink(body(D(-0.075, 0.296, 0.010, 0.007)))
    _belt(c, True)


def paint_torso_side(c):
    c.mark(SKIN_SHADE, [(-0.060, 0.330), (0.064, 0.330), (0.056, 0.180), (-0.052, 0.180)], 10, 0.6)
    # the lateral flank wraps (the lower one is under the belt, as in the original)
    c.ink(body(Q(-0.060, 0.336, 0.060, 0.348)))
    c.ink(body(Q(-0.055, 0.272, 0.055, 0.284)))
    _belt(c, False)


def paint_torso_top(c):
    c.blob(SKIN_LIT, (0.0, 0.0), 0.15, 0.075, 12, 0.5)
    c.blob(SKIN_DEEP, (0.0, 0.004), 0.085, 0.075, 6, 0.8)


# ---------------------------------------------------------------------------
# THE FLAPS. Sea teal cloth, the original's sand stripes and cream thread, then what a painted
# cloth can add: a lit fold down one side, a dark fold down the other, shade under the belt.
# The silver piping round them is geometry (CAST_CLOTHING_STYLE.md rule 3).
# ---------------------------------------------------------------------------

def paint_flap_front(c):
    c.mark(TEAL_DARK, [(-0.08, 0.21), (0.08, 0.21), (0.08, 0.168), (0.0, 0.160), (-0.08, 0.168)], 5, 0.75)
    c.stroke(TEAL_LIT, [(-0.022, 0.176), (-0.026, 0.140), (-0.024, 0.120)], 9, 1.4, 0.55, (0.3, 0.5))
    c.stroke(TEAL_DARK, [(0.020, 0.180), (0.026, 0.146), (0.024, 0.120)], 6, 1.0, 0.7, (0.3, 0.6))
    c.stroke(TEAL_DARK, [(-0.004, 0.170), (-0.002, 0.150), (-0.004, 0.132)], 3.5, 0.8, 0.6, (0.3, 0.6))
    c.band(SAND, RY(0.112), RY(0.126))
    c.band(SAND_DARK, RY(0.112), RY(0.112) + 0.002, 0.3, 0.7)
    c.band(CREAM, RY(0.132), RY(0.138))
    c.band(SAND, RY(0.146), RY(0.152))


def paint_flap_back(c):
    c.mark(TEAL_DARK, [(-0.08, 0.21), (0.08, 0.21), (0.08, 0.172), (0.0, 0.164), (-0.08, 0.172)], 5, 0.75)
    c.stroke(TEAL_LIT, [(0.028, 0.176), (0.034, 0.150), (0.032, 0.132)], 9, 1.4, 0.5, (0.3, 0.5))
    c.stroke(TEAL_DARK, [(-0.026, 0.180), (-0.034, 0.154), (-0.032, 0.134)], 6, 1.0, 0.7, (0.3, 0.6))
    c.band(SAND, RY(0.142), RY(0.156))
    c.band(SAND_DARK, RY(0.142), RY(0.142) + 0.002, 0.3, 0.7)
    c.band(CREAM, RY(0.162), RY(0.168))


# ---------------------------------------------------------------------------
# THE ARMS. Bare and inked, a silver cuff at each wrist. `s` is +1 for his left, -1 for his
# right; the right arm is the left mirrored, as the builder's ARM_DECALS_RIGHT is. Every mark
# stops at its own piece: the cuff's silver stops at the cuff, and the hand is painted last.
# ---------------------------------------------------------------------------

def _side(points, s):
    return [(s * a, b) for a, b in points]


def _arm_skin(c, s, view):
    if view == "bottom":
        c.column(SKIN_SHADE, *sorted((s * 0.08, s * 0.31)), 0, 0.75)
    elif view == "top":
        c.mark(SKIN_LIT, _side([(0.105, -0.034), (0.165, -0.030), (0.165, 0.030), (0.105, 0.036)], s), 8, 0.5)
    else:
        c.band(SKIN_SHADE, 0.16, 0.262, 5, 0.7)
        c.mark(SKIN_LIT, _side([(0.108, 0.340), (0.160, 0.336), (0.154, 0.312), (0.110, 0.314)], s), 6, 0.5)


def paint_upper_arm(c, s, view):
    _arm_skin(c, s, view)
    if view == "top":
        # the deltoid's top plate and crest
        c.ink(_side(arm_top(Q(0.100, -0.052, 0.170, -0.024)), s))
        c.ink(_side(arm_top(Q(0.100, 0.024, 0.170, 0.052)), s))
        c.ink(_side(arm_top(D(0.135, 0.000, 0.015, 0.016)), s))
        # the outer arm's boundary rails (they run on down the forearm)
        c.ink(_side(arm_top(Q(0.170, -0.052, 0.252, -0.046)), s))
        c.ink(_side(arm_top(Q(0.170, 0.046, 0.252, 0.052)), s))
        # the dayadaya armlet's two rails, over the top
        c.ink(_side(arm_top(Q(0.180, -0.058, 0.186, 0.058)), s))
        c.ink(_side(arm_top(Q(0.208, -0.058, 0.214, 0.058)), s))
    elif view in ("front", "back"):
        c.ink(_side(arm([(0.110, 0.350), (0.160, 0.380), (0.110, 0.410)]), s))   # the deltoid chevron
        c.ink(_side(arm(Q(0.180, 0.338, 0.186, 0.462)), s))                       # the armlet's rails
        c.ink(_side(arm(Q(0.208, 0.338, 0.214, 0.462)), s))
        c.ink(_side(arm(D(0.197, 0.370, 0.008, 0.012)), s))                       # its diamond chain
        c.ink(_side(arm(D(0.197, 0.400, 0.008, 0.012)), s))
        c.ink(_side(arm(D(0.197, 0.430, 0.008, 0.012)), s))


def _hand(c, s, view):
    """Finger lines on a block hand, and the gulot on its back."""
    c.column(SKIN, *sorted((s * CUFF_X[1], s * 0.31)))
    if view in ("front", "back"):
        c.mark(SKIN_SHADE, _side([(CUFF_X[1], 0.16), (0.31, 0.16), (0.31, 0.258), (CUFF_X[1], 0.258)], s), 0, 0.7, curved=False)
        c.stroke(SKIN_DEEP, _side([(0.258, 0.303), (0.283, 0.305)], s), 2.6, 0.4, 0.95, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, _side([(0.258, 0.284), (0.284, 0.283)], s), 2.6, 0.4, 0.95, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, _side([(0.258, 0.264), (0.282, 0.261)], s), 2.6, 0.4, 0.95, (1.0, 0.3), curved=False)
        # the thumb, folded along the top of the fist: one drawn hook
        c.stroke(SKIN_DEEP, _side([(0.232, 0.322), (0.256, 0.324), (0.264, 0.333)], s), 2.6, 0.4, 0.95, (0.8, 0.4))
    elif view == "top":
        # the gulot: two stripes across the back of the hand
        c.ink(_side(arm_top(Q(0.296, -0.046, 0.306, 0.054)), s))
        c.ink(_side(arm_top(Q(0.316, -0.044, 0.326, 0.052)), s))
    else:
        c.column(SKIN_SHADE, *sorted((s * CUFF_X[1], s * 0.31)), 0, 0.75)
    c.column(SKIN_SHADE, *sorted((s * 0.279, s * 0.31)), 2.5, 0.55)


def paint_forearm(c, s, view):
    _arm_skin(c, s, view)
    if view == "top":
        c.ink(_side(arm_top(Q(0.170, -0.052, 0.252, -0.046)), s))                # the boundary rails, carried on
        c.ink(_side(arm_top(Q(0.170, 0.046, 0.252, 0.052)), s))
        c.ink(_side(arm_top(Q(0.246, -0.058, 0.254, 0.058)), s))                 # the wrist gauntlet band
    elif view in ("front", "back"):
        # the pako fern: a spine and four fronds, alternating
        c.ink(_side(arm(Q(0.220, 0.397, 0.248, 0.403)), s))
        c.ink(_side(arm([(0.222, 0.403), (0.228, 0.418), (0.224, 0.418), (0.218, 0.403)]), s))
        c.ink(_side(arm([(0.226, 0.397), (0.232, 0.382), (0.228, 0.382), (0.222, 0.397)]), s))
        c.ink(_side(arm([(0.234, 0.403), (0.240, 0.418), (0.236, 0.418), (0.230, 0.403)]), s))
        c.ink(_side(arm([(0.238, 0.397), (0.244, 0.382), (0.240, 0.382), (0.234, 0.397)]), s))
        # the wrist gauntlet band and its three spear notches
        c.ink(_side(arm(Q(0.246, 0.345, 0.254, 0.455)), s))
        c.ink(_side(arm([(0.246, 0.365), (0.238, 0.375), (0.246, 0.385)]), s))
        c.ink(_side(arm([(0.246, 0.395), (0.238, 0.405), (0.246, 0.415)]), s))
        c.ink(_side(arm([(0.246, 0.425), (0.238, 0.435), (0.246, 0.445)]), s))
    # THE CUFF. Silver, lit along one edge. Each wrist's is engraved its own way (CUFF_DECALS):
    # his left carries two grooves, his right one groove with three square studs on its face.
    a0, a1 = CUFF_X
    c.column(SILVER, *sorted((s * a0, s * a1)))
    if view in ("front", "back"):
        c.mark(SILVER_DARK, _side([(a0, 0.16), (a1, 0.16), (a1, 0.250), (a0, 0.250)], s), 0, 0.5, curved=False)
        c.mark(SILVER_LIT, _side([(a0, 0.37), (a1, 0.37), (a1, 0.336), (a0, 0.336)], s), 1.5, 0.7, curved=False)
    elif view == "top":
        c.column(SILVER_LIT, *sorted((s * (a0 + 0.004), s * (a0 + 0.009))), 0.8, 0.8)
    else:
        c.column(SILVER_DARK, *sorted((s * a0, s * a1)), 0, 0.55)
    if view != "bottom":
        if s > 0:
            c.column(SILVER_DARK, AX(0.260), AX(0.264))
            c.column(SILVER_DARK, AX(0.278), AX(0.282))
        else:
            c.column(SILVER_DARK, AX(-0.273), AX(-0.269))
            if view == "front":
                c.mark(SILVER_DARK, arm(Q(-0.278, 0.360, -0.264, 0.374)), 0.2, 1.0, curved=False)
                c.mark(SILVER_DARK, arm(Q(-0.278, 0.393, -0.264, 0.407)), 0.2, 1.0, curved=False)
                c.mark(SILVER_DARK, arm(Q(-0.278, 0.426, -0.264, 0.440)), 0.2, 1.0, curved=False)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Bare, the labid up each shin, the sunburst on each thigh, chevrons on each foot.
# The tsinelas (sole, bed, strap) are geometry in flat tones. Both legs are written out, as the
# builder writes them: the left's numbers, then the right's.
# ---------------------------------------------------------------------------
FOOT_TOP = RY(0.084)


def paint_leg(c, s, view):
    x = lambda v: s * v
    if view == "front":
        c.mark(SKIN_LIT, [(x(0.060), 0.146), (x(0.116), 0.146), (x(0.112), 0.084), (x(0.064), 0.084)], 7, 0.4)
        c.mark(SKIN_SHADE, [(x(0.0), 0.21), (x(0.036), 0.21), (x(0.040), 0.06), (x(0.0), 0.06)], 5, 0.7)
        c.band(SKIN_SHADE, 0.172, 0.21, 4, 0.7)           # under the belt and the flap
        if s > 0:
            c.ink(body(Q(0.030, 0.078, 0.138, 0.086)))                           # ankle double band
            c.ink(body(Q(0.030, 0.090, 0.138, 0.098)))
            c.ink(body(Q(0.076, 0.098, 0.084, 0.196)))                           # the shin's two guide rails
            c.ink(body(Q(0.116, 0.098, 0.124, 0.196)))
            c.ink(body(D(0.100, 0.118, 0.012, 0.010)))                           # the stacked diamond chain
            c.ink(body(D(0.100, 0.147, 0.012, 0.010)))
            c.ink(body(D(0.100, 0.176, 0.012, 0.010)))
            c.mark(SKIN, body(Q(0.020, 0.180, 0.148, 0.262)), 0.0, 1.0, curved=False)   # the thigh stands over the shin
            c.band(SKIN_SHADE, 0.172, 0.21, 4, 0.7)
            c.ink(body(Q(0.026, 0.250, 0.144, 0.258)))                           # upper framing bands (under the belt)
            c.ink(body(Q(0.026, 0.238, 0.144, 0.244)))
            c.ink(body(D(0.048, 0.244, 0.007, 0.005)))
            c.ink(body(D(0.072, 0.244, 0.007, 0.005)))
            c.ink(body(D(0.096, 0.244, 0.007, 0.005)))
            c.ink(body(D(0.120, 0.244, 0.007, 0.005)))
            c.ink(body(Q(0.026, 0.184, 0.144, 0.190)))                           # lower framing bands
            c.ink(body(Q(0.026, 0.194, 0.144, 0.200)))
            c.ink(body(D(0.048, 0.192, 0.007, 0.005)))
            c.ink(body(D(0.072, 0.192, 0.007, 0.005)))
            c.ink(body(D(0.096, 0.192, 0.007, 0.005)))
            c.ink(body(D(0.120, 0.192, 0.007, 0.005)))
            c.ink(body([(0.092, 0.206), (0.106, 0.220), (0.092, 0.234), (0.078, 0.220)]))   # the eight-ray solar star
            c.ink(body([(0.090, 0.234), (0.092, 0.246), (0.094, 0.234)]))
            c.ink(body([(0.090, 0.206), (0.092, 0.194), (0.094, 0.206)]))
            c.ink(body([(0.078, 0.218), (0.058, 0.220), (0.078, 0.222)]))
            c.ink(body([(0.106, 0.218), (0.126, 0.220), (0.106, 0.222)]))
            c.ink(body([(0.082, 0.229), (0.068, 0.238), (0.078, 0.225)]))
            c.ink(body([(0.102, 0.229), (0.116, 0.238), (0.106, 0.225)]))
            c.ink(body([(0.082, 0.211), (0.068, 0.202), (0.078, 0.215)]))
            c.ink(body([(0.102, 0.211), (0.116, 0.202), (0.106, 0.215)]))
            c.ink(body([(0.046, 0.216), (0.050, 0.220), (0.046, 0.224), (0.042, 0.220)]))   # satellite diamonds
            c.ink(body([(0.134, 0.216), (0.138, 0.220), (0.134, 0.224), (0.130, 0.220)]))
        else:
            c.ink(body(Q(-0.138, 0.078, -0.030, 0.086)))
            c.ink(body(Q(-0.138, 0.090, -0.030, 0.098)))
            c.ink(body(Q(-0.124, 0.098, -0.116, 0.196)))
            c.ink(body(Q(-0.084, 0.098, -0.076, 0.196)))
            c.ink(body(D(-0.100, 0.118, 0.012, 0.010)))
            c.ink(body(D(-0.100, 0.147, 0.012, 0.010)))
            c.ink(body(D(-0.100, 0.176, 0.012, 0.010)))
            c.mark(SKIN, body(Q(-0.148, 0.180, -0.020, 0.262)), 0.0, 1.0, curved=False)
            c.band(SKIN_SHADE, 0.172, 0.21, 4, 0.7)
            c.ink(body(Q(-0.144, 0.250, -0.026, 0.258)))
            c.ink(body(Q(-0.144, 0.238, -0.026, 0.244)))
            c.ink(body(D(-0.048, 0.244, 0.007, 0.005)))
            c.ink(body(D(-0.072, 0.244, 0.007, 0.005)))
            c.ink(body(D(-0.096, 0.244, 0.007, 0.005)))
            c.ink(body(D(-0.120, 0.244, 0.007, 0.005)))
            c.ink(body(Q(-0.144, 0.184, -0.026, 0.190)))
            c.ink(body(Q(-0.144, 0.194, -0.026, 0.200)))
            c.ink(body(D(-0.048, 0.192, 0.007, 0.005)))
            c.ink(body(D(-0.072, 0.192, 0.007, 0.005)))
            c.ink(body(D(-0.096, 0.192, 0.007, 0.005)))
            c.ink(body(D(-0.120, 0.192, 0.007, 0.005)))
            c.ink(body([(-0.092, 0.206), (-0.078, 0.220), (-0.092, 0.234), (-0.106, 0.220)]))
            c.ink(body([(-0.094, 0.234), (-0.092, 0.246), (-0.090, 0.234)]))
            c.ink(body([(-0.094, 0.206), (-0.092, 0.194), (-0.090, 0.206)]))
            c.ink(body([(-0.106, 0.218), (-0.126, 0.220), (-0.106, 0.222)]))
            c.ink(body([(-0.078, 0.218), (-0.058, 0.220), (-0.078, 0.222)]))
            c.ink(body([(-0.106, 0.225), (-0.116, 0.238), (-0.102, 0.229)]))
            c.ink(body([(-0.078, 0.225), (-0.068, 0.238), (-0.082, 0.229)]))
            c.ink(body([(-0.106, 0.215), (-0.116, 0.202), (-0.102, 0.211)]))
            c.ink(body([(-0.078, 0.215), (-0.068, 0.202), (-0.082, 0.211)]))
            c.ink(body([(-0.046, 0.216), (-0.042, 0.220), (-0.046, 0.224), (-0.050, 0.220)]))
            c.ink(body([(-0.134, 0.216), (-0.130, 0.220), (-0.134, 0.224), (-0.138, 0.220)]))
        # THE FOOT, LAST. Its block stands in front of the shin, so the ankle band's ink must not
        # land on it (rule 4). Toes: four short lines over the front of the foot.
        c.band(SKIN, 0.0, FOOT_TOP)
        c.stroke(SKIN_DEEP, [(x(0.052), 0.058), (x(0.052), 0.036)], 2.4, 0.4, 0.9, (0.4, 1.0), curved=False)
        c.stroke(SKIN_DEEP, [(x(0.078), 0.060), (x(0.078), 0.036)], 2.4, 0.4, 0.9, (0.4, 1.0), curved=False)
        c.stroke(SKIN_DEEP, [(x(0.102), 0.059), (x(0.102), 0.036)], 2.4, 0.4, 0.9, (0.4, 1.0), curved=False)
        c.stroke(SKIN_DEEP, [(x(0.124), 0.056), (x(0.124), 0.036)], 2.4, 0.4, 0.9, (0.4, 1.0), curved=False)
    elif view == "back":
        c.mark(SKIN_SHADE, [(x(0.02), 0.21), (x(0.15), 0.21), (x(0.15), 0.150), (x(0.02), 0.146)], 6, 0.6)
        c.stroke(SKIN_SHADE, [(x(0.084), 0.140), (x(0.086), 0.110), (x(0.084), 0.080)], 14, 3.0, 0.45, (0.6, 0.6))
        if s > 0:
            c.ink(body(Q(0.030, 0.078, 0.138, 0.086)))
            c.ink(body(Q(0.030, 0.090, 0.138, 0.098)))
        else:
            c.ink(body(Q(-0.138, 0.078, -0.030, 0.086)))
            c.ink(body(Q(-0.138, 0.090, -0.030, 0.098)))
        c.band(mix(SKIN, SKIN_SHADE, 0.6), 0.0, FOOT_TOP)     # the heel, clear of the ankle band's ink
    else:
        outer = (view == "xpos") == (s > 0)
        c.mark(SKIN_SHADE, [(-0.050, 0.21), (0.060, 0.21), (0.060, 0.156), (-0.050, 0.156)], 5, 0.5)
        c.band(SKIN_SHADE, 0.0, 0.034, 2, 0.5)
        if outer:
            # the lateral shin wraps: three bars round the outside of the calf
            c.ink(body(Q(-0.040, 0.110, 0.040, 0.118)))
            c.ink(body(Q(-0.040, 0.146, 0.040, 0.154)))
            c.ink(body(Q(-0.040, 0.178, 0.040, 0.186)))
            # the ankle bone, one drawn curve
            c.stroke(SKIN_DEEP, [(0.006, 0.058), (0.018, 0.050), (0.012, 0.040)], 2.4, 0.5, 0.8)
        else:
            c.mark(SKIN_SHADE, [(-0.15, 0.21), (0.10, 0.21), (0.10, 0.0), (-0.15, 0.0)], 0, 0.55, curved=False)


def paint_leg_top(c, s):
    """Looking down on a foot. The toe is at -y. The chevron plates are the builder's, per foot."""
    x = lambda v: s * v
    c.mark(SKIN_SHADE, [(x(0.0), 0.10), (x(0.17), 0.10), (x(0.17), 0.030), (x(0.0), 0.030)], 6, 0.7)
    # the toe lines, where the block's front edge turns down
    c.stroke(SKIN_DEEP, [(x(0.052), -0.106), (x(0.052), -0.126)], 2.4, 0.4, 0.9, (0.3, 1.0), curved=False)
    c.stroke(SKIN_DEEP, [(x(0.078), -0.104), (x(0.078), -0.126)], 2.4, 0.4, 0.9, (0.3, 1.0), curved=False)
    c.stroke(SKIN_DEEP, [(x(0.102), -0.105), (x(0.102), -0.126)], 2.4, 0.4, 0.9, (0.3, 1.0), curved=False)
    c.stroke(SKIN_DEEP, [(x(0.124), -0.108), (x(0.124), -0.126)], 2.4, 0.4, 0.9, (0.3, 1.0), curved=False)
    if s > 0:
        c.ink([(0.046, -0.030), (0.082, -0.054), (0.082, -0.042), (0.054, -0.018)])
        c.ink([(0.082, -0.054), (0.118, -0.030), (0.110, -0.018), (0.082, -0.042)])
        c.ink([(0.058, -0.006), (0.082, -0.022), (0.082, -0.010), (0.066, 0.006)])
        c.ink([(0.082, -0.022), (0.106, -0.006), (0.098, 0.006), (0.082, -0.010)])
    else:
        c.ink([(-0.082, -0.054), (-0.046, -0.030), (-0.054, -0.018), (-0.082, -0.042)])
        c.ink([(-0.118, -0.030), (-0.082, -0.054), (-0.082, -0.042), (-0.110, -0.018)])
        c.ink([(-0.082, -0.022), (-0.058, -0.006), (-0.066, 0.006), (-0.082, -0.010)])
        c.ink([(-0.106, -0.006), (-0.082, -0.022), (-0.082, -0.010), (-0.098, 0.006)])


# ---------------------------------------------------------------------------
# THE SWATCHES. u across, v up.
# ---------------------------------------------------------------------------

def paint_putong(c):
    """The headcloth, unrolled: u runs once round the head from the knot. A lit upper edge, a
    dark under edge, and five creases where a tied cloth pulls, each its own slant and length."""
    c.mark(PUTONG_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.74), (0.0, 0.74)], 1.2, 0.75, curved=False)
    c.mark(PUTONG_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.20), (0.0, 0.20)], 1.2, 0.85, curved=False)
    c.stroke(PUTONG_DARK, [(0.060, 0.22), (0.072, 0.50), (0.066, 0.82)], 5.0, 0.6, 0.8, (0.6, 0.2))
    c.stroke(PUTONG_DARK, [(0.215, 0.18), (0.232, 0.46), (0.244, 0.70)], 4.0, 0.6, 0.7, (0.6, 0.2))
    c.stroke(PUTONG_DARK, [(0.470, 0.24), (0.462, 0.52), (0.474, 0.80)], 4.5, 0.6, 0.7, (0.6, 0.2))
    c.stroke(PUTONG_DARK, [(0.745, 0.20), (0.730, 0.48), (0.722, 0.74)], 4.0, 0.6, 0.7, (0.6, 0.2))
    c.stroke(PUTONG_DARK, [(0.930, 0.22), (0.920, 0.52), (0.926, 0.84)], 5.0, 0.6, 0.8, (0.6, 0.2))


def paint_silver(c):
    """Piping: a pale top edge, a dark under edge, three worn dull places, each its own length."""
    c.mark(SILVER_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.68), (0.0, 0.68)], 1.0, 0.85, curved=False)
    c.mark(SILVER_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.22), (0.0, 0.22)], 1.0, 0.85, curved=False)
    c.mark(SILVER_DARK, [(0.10, 0.30), (0.19, 0.30), (0.18, 0.70), (0.11, 0.68)], 1.6, 0.3)
    c.mark(SILVER_DARK, [(0.46, 0.28), (0.60, 0.30), (0.59, 0.66), (0.48, 0.68)], 1.6, 0.25)
    c.mark(SILVER_DARK, [(0.82, 0.30), (0.88, 0.30), (0.88, 0.70), (0.83, 0.66)], 1.6, 0.3)


def paint_tooth(c):
    """A shark tooth: cream, a shaded edge, a darker root line under the bail."""
    c.mark(CREAM_SHADE, [(0.62, 0.0), (1.0, 0.0), (1.0, 1.0), (0.70, 1.0)], 2.0, 0.8, curved=False)
    c.mark(CREAM_DIRT, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.10), (0.0, 0.10)], 1.0, 0.6, curved=False)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        c = I(name, base, SWATCHES[name][2:4])
        done[name] = c
        return c

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    side = mix(SKIN, SKIN_SHADE, 0.28)
    c = I("head.xpos", side); paint_head_side(c); done[c.name] = c
    c = I("head.xneg", side); paint_head_side(c); done[c.name] = c
    done["head.back"] = I("head.back", SKIN_SHADE)
    done["head.top"] = I("head.top", mix(SKIN, SKIN_LIT, 0.5))
    # under the chin: one flat step darker, no drawn jaw line
    done["head.bottom"] = I("head.bottom", SKIN_SHADE)

    c = I("torso.front", SKIN); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", SKIN); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", SKIN); paint_torso_side(c); done[c.name] = c
    c = I("torso.xneg", SKIN); paint_torso_side(c); done[c.name] = c
    c = I("torso.top", SKIN); paint_torso_top(c); done[c.name] = c
    c = I("flap.front", TEAL); paint_flap_front(c); done[c.name] = c
    c = I("flap.back", TEAL); paint_flap_back(c); done[c.name] = c

    for group, s in (("uarmL", 1), ("uarmR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN); paint_upper_arm(c, s, view); done[c.name] = c
    for group, s in (("farmL", 1), ("farmR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN); paint_forearm(c, s, view); done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            if view == "top":
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c

    # hair is three flat tones and no drawing (rule 3: no painted hair shine)
    swatch("hair", HAIR); swatch("hair_top", HAIR_TOP); swatch("hair_under", HAIR_DEEP)
    swatch("skin_tone", SKIN); swatch("skin_shade_tone", SKIN_SHADE); swatch("skin_lit_tone", mix(SKIN, SKIN_LIT, 0.6))
    swatch("teal_tone", TEAL); swatch("teal_dark_tone", TEAL_DARK); swatch("teal_deep_tone", TEAL_DEEP)
    swatch("putong_tone", PUTONG); swatch("putong_lit_tone", PUTONG_LIT); swatch("putong_dark_tone", PUTONG_DARK)
    swatch("silver_tone", SILVER); swatch("silver_lit_tone", SILVER_LIT); swatch("silver_dark_tone", SILVER_DARK)
    swatch("cream_tone", CREAM); swatch("cream_shade_tone", CREAM_SHADE)
    swatch("bed_tone", BED); swatch("bed_dark_tone", BED_DARK)
    paint_putong(swatch("putong", PUTONG))
    paint_tooth(swatch("tooth", CREAM))

    missing = set(n for n in layout(size) if isinstance(n, str)) - set(done)
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
    atlas = Image.new("RGB", (size, size), _rgb(TEAL_DEEP))
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
    used = sum(r[2] * r[3] for n, r in layout(size).items() if isinstance(n, str))
    print("wrote %s  (%d x %d, %d islands, densities at %.0f%%, %.0f%% of the top half painted)"
          % (out, size, size, len(islands), 100.0 * _LAYOUT[(size, "scale")], 100.0 * used / (size * size / 2)))
    if "--sheet" in sys.argv:
        sheet = sys.argv[sys.argv.index("--sheet") + 1]
        atlas.crop((0, 0, size, size // 2)).save(sheet)
        print("wrote %s" % sheet)


if __name__ == "__main__":
    main()
