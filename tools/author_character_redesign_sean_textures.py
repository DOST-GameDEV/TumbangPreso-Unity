"""Paint the atlas of the Sean (displayed: Rago) redesign PROTOTYPE, by hand, in the house style.

    py -3 tools/author_character_redesign_sean_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/sean/sean-redesign-atlas.png. The model is built
by tools/author_character_redesign_sean.py, which imports this file for the LAYOUT only (PIL is
imported inside main, Blender's Python has none) and so maps every face onto the island painted
for it here. Paint first, then build.

WHY. Owner, 2026-10-05, after Dante's rework (docs/CHARACTER_REDESIGN_DANTE.md section 13):
*"following dante's rework, redesign the rest of the characters"*. This is that rework for roster
id `sean`: the same kid with a lot more detail, not another art style. It is a copy of Dante's
painter REWRITTEN for this hero; nothing here is imported from Dante's scripts and nothing in the
game loads these files. team-sean.glb is not touched.

WHO HE IS, read off team-sean.glb and its builder (tools/build_iggy_voxel.py, "the Heavyweight
Fire Brawler"): the cast's tallest and widest kid. Sun-bronze skin, a bare muscled chest under an
open sleeveless red vest piped in gold, a brown belt with a big gold buckle, red trunks with a
gold band at the thigh, red high-top sneakers with an orange tongue on a white sole, red bracers
rimmed in gold, a shaved head with a black mohawk that carries a red, orange and yellow flame fin
and hangs down his nape as a tail, three gold razor slits over each ear, angry slanted eyes and a
one-sided smirk. EVERY ONE OF THOSE IS KEPT AND NOTHING IS ADDED TO THE LIST (rule 2).

THE HEAD IS THE GAME'S BOX HEAD (rule 1) AND THE FACE IS CUTE, NOT A PORTRAIT (rule 12): flat
skin, one soft lit patch, no blush (the owner keeps it for the girls), and the cast's INK-ONLY features as his original has
them, a third bigger: two solid black eyes whose top edge does the brow's job and one black
mouth. No nose, no socket, no crease, no jaw contour, no iris, no glint, no eyebrow.

THE ONE MECHANISM is the beggar's (tools/build_beggar_voxel.py): `Toon.shader` remaps a UV to a
palette slot only in Unity atlas rows 0 to 7 and samples the texture above them, so every island
lives in the TOP half of the file and the bottom half is left flat.

HOW AN ISLAND IS LAID OUT. A body part is seen from up to six sides (front, back, xpos, xneg, top,
bottom) and each side is one island, a flat orthographic view of the part in MODEL METRES. A mark
is written where it sits on the body: `(x, z)` on a front or back view, `(y, z)` on a side view,
`(x, y)` on a top view. Loose blocks take a SWATCH instead.

RULES THIS FILE HAS TO KEEP (section 13):
  3  no painted hair shine: the mohawk and its flame are flat tones chosen by the model script
     from which way a face points. Nothing is drawn on them here.
  4  paint stops at its own piece's edges. Every bracer mark stops at the bracer's two ends and
     the fist is repainted skin LAST; the ears are skin and carry no fade paint.
  5  the one sewn mark (a mended tear on his left vest panel) is hard edged with a dark rim and
     sits wholly in the open, clear of the lapel piping, the armhole trim and the hem band.

HIS COLOURS ARE THE ORIGINAL'S (person_sean.asset, the 16 slot palette, which is
build_iggy_voxel.py's PALETTE). The first of each family below is a palette slot unchanged; the
painted tones are steps of those.

ROLE HUES (offence orange #f87020, defence blue #0080e8). ⚠️ HIS OWN PALETTE HOLDS TWO ORANGES
THAT SIT ON THE OFFENCE HUE: slot 14 `ff6b1a` (the flame's core) and slot 15 `ff8800` (the shoe
tongue and the bracer strap). Rule 2 says keep his colours and rule 10 says keep the role hue off
LARGE areas, so both stay and both are kept SMALL: the original's tongue was a 140 mm wide block
over the whole front of each shoe, here it is a 70 mm riser and a narrow ankle strap. `main`
measures how much of the painted atlas is role orange and refuses to write past ORANGE_BUDGET;
it refuses any blue outright. Skin is exempt, as it is for the cast.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "sean"
ATLAS_NAME = "sean-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down
ORANGE_BUDGET = 0.035  # the share of painted pixels allowed to sit on the offence hue

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is the original palette slot, unchanged.
# ---------------------------------------------------------------------------
SKIN = "b87440"; SKIN_LIT = "cb864e"; SKIN_SHADE = "96592e"; SKIN_DEEP = "74421f"   # slots 0, 2, 1
FADE = "7d4c2c"; FADE_DARK = "5e3a26"          # the buzzed temple: slot 1 pushed toward the hair
RED = "c92a2a"; RED_LIT = "dc2626"; RED_SHADE = "991b1b"; RED_DEEP = "6c1212"       # slots 3, 10, 4
GOLD = "f0a500"; GOLD_LIT = "ffc107"; GOLD_DARK = "b87a00"; GOLD_DEEP = "8a5a00"    # slots 5, 9, 6
HAIR = "1c1a24"; HAIR_TOP = "302d3e"; HAIR_DEEP = "111115"                          # slots 7, 8
INK = "111115"                                                                      # slot 8
FLAME_Y = "ffc107"; FLAME_Y_LIT = "ffd84d"; FLAME_Y_DARK = "d99700"                 # slot 9
FLAME_O = "ff6b1a"; FLAME_O_LIT = "ff8a3c"; FLAME_O_DARK = "cf4d08"                 # slot 14
FLAME_R = "dc2626"; FLAME_R_LIT = "ee4a3a"; FLAME_R_DARK = "a01818"                 # slot 10
BELT = "5c3a21"; BELT_LIT = "7b5232"; BELT_DARK = "3c2413"                          # slot 11
WHITE = "f0f2f5"; WHITE_SHADE = "c8ccd4"; WHITE_DIRT = "9a9388"                     # slot 13
TONGUE = "ff8800"; TONGUE_DARK = "cc6a00"                                           # slot 15
SCUFF = "8e2020"

CLOTH_HEXES = [RED, RED_LIT, RED_SHADE, RED_DEEP, GOLD, GOLD_LIT, GOLD_DARK, GOLD_DEEP, HAIR, HAIR_TOP,
               FLAME_Y, FLAME_Y_LIT, FLAME_Y_DARK, FLAME_R, FLAME_R_LIT, FLAME_R_DARK, BELT, BELT_LIT,
               BELT_DARK, WHITE, WHITE_SHADE, WHITE_DIRT, SCUFF]

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y). +x is HIS left, -y is the way he faces, z is up, feet on zero.
#   torso  the bare body, the belt, the buckle and the seat of the trunks
#   vest   the open vest that lies over it (its own group: the two overlap in every view)
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.27, 0.27, 0.43, 0.83), 1250),
        "back": ((-0.27, 0.27, 0.43, 0.83), 700),
        "xpos": ((-0.20, 0.20, 0.43, 0.83), 860),
        "xneg": ((-0.20, 0.20, 0.43, 0.83), 860),
        "top": ((-0.27, 0.27, -0.20, 0.20), 520),
        "bottom": ((-0.27, 0.27, -0.20, 0.20), 220),
    },
    "torso": {
        "front": ((-0.20, 0.20, 0.18, 0.49), 1150),
        "back": ((-0.20, 0.20, 0.18, 0.49), 900),
        "xpos": ((-0.15, 0.15, 0.18, 0.49), 700),
        "xneg": ((-0.15, 0.15, 0.18, 0.49), 700),
        "top": ((-0.20, 0.20, -0.15, 0.15), 500),
    },
    "vest": {
        "front": ((-0.22, 0.22, 0.27, 0.49), 1150),
        "back": ((-0.22, 0.22, 0.27, 0.49), 1150),
        "xpos": ((-0.15, 0.15, 0.27, 0.49), 900),
        "xneg": ((-0.15, 0.15, 0.27, 0.49), 900),
        "top": ((-0.22, 0.22, -0.15, 0.15), 600),
    },
    "armL": {
        "front": ((0.12, 0.42, 0.33, 0.53), 1000),
        "back": ((0.12, 0.42, 0.33, 0.53), 1000),
        "top": ((0.12, 0.42, -0.10, 0.13), 950),
        "bottom": ((0.12, 0.42, -0.10, 0.13), 800),
        "xpos": ((-0.10, 0.13, 0.33, 0.53), 650),
    },
    "armR": {
        "front": ((-0.42, -0.12, 0.33, 0.53), 1000),
        "back": ((-0.42, -0.12, 0.33, 0.53), 1000),
        "top": ((-0.42, -0.12, -0.10, 0.13), 950),
        "bottom": ((-0.42, -0.12, -0.10, 0.13), 800),
        "xneg": ((-0.10, 0.13, 0.33, 0.53), 650),
    },
    "legL": {
        "front": ((0.0, 0.20, 0.0, 0.27), 1000),
        "back": ((0.0, 0.20, 0.0, 0.27), 900),
        "xpos": ((-0.19, 0.13, 0.0, 0.27), 1000),
        "xneg": ((-0.19, 0.13, 0.0, 0.27), 800),
        "top": ((0.0, 0.20, -0.19, 0.13), 900),
    },
    "legR": {
        "front": ((-0.20, 0.0, 0.0, 0.27), 1000),
        "back": ((-0.20, 0.0, 0.0, 0.27), 900),
        "xpos": ((-0.19, 0.13, 0.0, 0.27), 800),
        "xneg": ((-0.19, 0.13, 0.0, 0.27), 1000),
        "top": ((-0.20, 0.0, -0.19, 0.13), 900),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group. The face
# is drawn on the front island, so the head's front reaches round its cut corners to the ears.
VIEW_BIAS = {("head", "front"): 1.5, ("head", "top"): 0.8, ("torso", "front"): 1.15, ("torso", "back"): 1.15,
             ("vest", "front"): 1.15, ("vest", "back"): 1.15, ("legL", "top"): 0.9, ("legR", "top"): 0.9}

# name -> (px wide, px high at 2048, metres wide, metres high). u runs across, v UP.
_TONE = (40, 40, 0.04, 0.04)
SWATCHES = {
    "hair": _TONE, "hair_top": _TONE, "hair_under": _TONE,
    "flame_y": _TONE, "flame_y_top": _TONE, "flame_y_under": _TONE,
    "flame_o": _TONE, "flame_o_top": _TONE, "flame_o_under": _TONE,
    "flame_r": _TONE, "flame_r_top": _TONE, "flame_r_under": _TONE,
    "gold_tone": _TONE, "gold_dark": _TONE, "skin_tone": _TONE, "skin_deep": _TONE,
    "belt_tone": _TONE, "red_tone": _TONE, "lining": _TONE, "tongue": _TONE, "tongue_dark": _TONE,
    "sole": _TONE, "white": _TONE, "gold_lit": _TONE, "skin": _TONE, "red": _TONE, "red_lit": _TONE,
    "gold": (256, 44, 0.20, 0.02),
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


def _hue_gap(rgb, role):
    h, s, v = colorsys.rgb_to_hsv(*(c / 255.0 for c in rgb))
    hr = colorsys.rgb_to_hsv(*(c / 255.0 for c in _rgb(role)))[0]
    dh = abs(h - hr)
    return min(dh, 1 - dh), s, v


def _near_role_hue(hex_str):
    for role in ("f87020", "0080e8"):
        dh, s, v = _hue_gap(_rgb(hex_str), role)
        # bright and saturated only: his belt's leather browns share the hue and read as brown
        if dh < 0.045 and s > 0.6 and v > 0.6:
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

    def box(self, colour, a0, a1, b0, b1, feather=0.0, strength=1.0):
        """A plain rectangle, hard cornered: a strap, a plate, a band that must stop at an edge."""
        return self.mark(colour, [(a0, b0), (a1, b0), (a1, b1), (a0, b1)], feather, strength, curved=False)

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
        """Everything between two positions along the first axis: a bracer round an arm."""
        b0, b1 = self.win[2], self.win[3]
        pad = (b1 - b0)
        return self.mark(colour, [(a0, b0 - pad), (a1, b0 - pad), (a1, b1 + pad), (a0, b1 + pad)],
                         feather, strength, curved=False)

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE HEAD. z 0.473 to 0.791. Measured off the original's face (slot 8 ink): the eyes sit between
# 0.597 and 0.655 at |x| 0.040 to 0.108, the mouth between 0.552 and 0.586, hooked up on HIS LEFT.
# The model's brow ledge is at 0.660 to 0.672, straight; the SLANT of his glare is the eye's own
# top edge, as the cast does it (CHARACTER_MODEL_METHOD.md section 2).
# ---------------------------------------------------------------------------


def _about(centre, k, points):
    """`points` grown by `k` about `centre`."""
    return [(centre[0] + (x - centre[0]) * k, centre[1] + (z - centre[1]) * k) for x, z in points]


# ⚠️ 1.14, NOT A THIRD. At 1.33 the wedge, its tab and its bar became three big separate black
# pieces, a mask. And NOTHING IS MOVED: v09 lifted the eyes 5 mm and dropped the mouth 6 to make
# room for eyes that size, which took the face apart. Owner: "sean's face looks too weird".
EYE_GROW = 1.14
#   his LEFT eye as the original draws it (the right is its mirror), and the mouth
ORIGINAL_EYE = [(0.040, 0.642), (0.098, 0.662), (0.104, 0.640), (0.096, 0.608), (0.048, 0.608), (0.044, 0.632)]
ORIGINAL_EYE_TAB = [(0.056, 0.602), (0.056, 0.608), (0.088, 0.608), (0.088, 0.602)]
ORIGINAL_EYE_BAR = [(0.104, 0.622), (0.104, 0.648), (0.112, 0.648), (0.112, 0.622)]
ORIGINAL_MOUTH = [
    [(-0.048, 0.550), (-0.048, 0.560), (-0.040, 0.566), (-0.040, 0.556)],
    [(-0.042, 0.556), (-0.042, 0.566), (0.040, 0.574), (0.040, 0.564)],
    [(0.036, 0.564), (0.036, 0.588), (0.048, 0.592), (0.048, 0.570)],
]


def paint_head_front(c):
    # ⚠️⚠️ A CUTE FACE, NOT A PORTRAIT (rule 12). The first three versions modelled his face in
    # paint: a brow shadow in two steps, a crease under each eye, a lit nose with its own shadow,
    # two bands of jaw shade, a lit chin, a lip shadow. Owner, 2026-10-05, on the whole redesigned
    # cast: *"the faces look too realistic and look too human, like it lost its charm, the
    # characters have eyebags etc.. they need to be more cutesy"*. The cast's charm is a flat face
    # with big simple ink features. So what is here is ALL that is here: flat skin with one soft
    # lit patch, the mohawk's shadow as one flat tone, two BIG solid ink eyes in the original's
    # places and the original's shape, and his smirk as one stroke.
    # (and no lit patch: one flat skin tone, so nothing on the face can band)
    # the block's edges turn away, faintly
    # (the edge shade down both sides of the face is gone too: on the jaw's corner in three-quarter
    # view it was one of the "random dark spots". The lower face is one clean skin tone.)
    # (no shadow under the mohawk either: on a bare forehead it read as a smudge, and in the
    # game's shader the face has to be one clean tone)
    # NO BLUSH. Rule 12 as amended, owner 2026-10-05: "reserve the blush for the female characters".
    # ⚠️ THE EYE'S OUTLINE IS THE EXPRESSION, SO IT IS MEASURED, NOT REDRAWN. Owner, 2026-10-05,
    # on two other heroes whose eyes were redrawn from a render: one "lost the eye quirk", one
    # lost her "slight smug look". The first cute face here did the same thing to him: its eyes
    # were cut 27 mm lower at the nose than at the temple, where the original's are cut 20, so he
    # came out angrier than he is. These polygons are the slot 8 ink of team-sean.glb's head
    # mesh, vertex for vertex (x is his left, z up). His two eyes do mirror each other; each is
    # three pieces: the wedge, the small tab under it, and the bar against its outer edge.
    for s in (1, -1):
        centre = (s * 0.076, 0.632)
        for piece in (ORIGINAL_EYE, ORIGINAL_EYE_TAB, ORIGINAL_EYE_BAR):
            grown = _about(centre, EYE_GROW, [(s * x, z) for x, z in piece])
            c.mark(INK, grown, 0.0, 1.0, curved=False)
    # THE MOUTH: his smirk as ONE smooth stroke with round ends. The original's is three voxel
    # bars (ORIGINAL_MOUTH: a tick down at his right end, a bar climbing to his left, a hook up);
    # owner, 2026-10-05, on another hero: the redesign's mouths are curved, "fix that" blocky
    # mouth. The stroke runs down the middle of those three bars, same centre, same 82 mm width,
    # the bar's own 10 mm weight, and keeps the hook on his left.
    c.stroke(INK, [(-0.045, 0.554), (-0.036, 0.5605), (0.000, 0.5650), (0.031, 0.5690), (0.0415, 0.5770), (0.0425, 0.5890)],
             9.6, 0.25, 1.0, (0.8, 0.8))
    # (the ears' front faces land here, at |x| 0.17 to 0.23, and are left plain skin)


def _head_side(c, sign):
    """One side of the head, (y, z). `sign` is +1 for his left (xpos), -1 for his right."""
    # (no jaw bands and no cheek paint: rule 12, no contour on the face or beside it)
    # THE BUZZED TEMPLE the three gold slits sit on (the original's `fade-temple` block, y -0.080
    # to 0.060, z 0.600 to 0.720). Hard along its top and front as a clipper line is, and it stops
    # ABOVE the ear: the ear block wears skin and nothing else (rule 4).
    top = 0.742 if sign > 0 else 0.738
    c.mark(FADE, [(-0.094, 0.652), (-0.094, top - 0.014), (-0.070, top), (0.104, top + 0.004), (0.120, 0.700),
                  (0.116, 0.652)], 1.2, 0.9, curved=False)
    c.mark(FADE_DARK, [(-0.094, 0.652), (-0.094, 0.668), (0.116, 0.672), (0.116, 0.652)], 2.5, 0.55, curved=False)
    # two clipper lines through it, each its own length, lower in front, as the slits climb
    if sign > 0:
        c.stroke(SKIN_SHADE, [(-0.084, 0.690), (-0.030, 0.701)], 2.6, 0.4, 0.9, (0.6, 0.6), curved=False)
        c.stroke(SKIN_SHADE, [(0.020, 0.729), (0.096, 0.741)], 2.4, 0.4, 0.9, (0.6, 0.6), curved=False)
    else:
        c.stroke(SKIN_SHADE, [(-0.086, 0.686), (-0.036, 0.698)], 2.6, 0.4, 0.9, (0.6, 0.6), curved=False)
        c.stroke(SKIN_SHADE, [(0.028, 0.727), (0.100, 0.736)], 2.4, 0.4, 0.9, (0.6, 0.6), curved=False)
    # the ear: a lit rim, the cup dark, drawn as a hook. Painted AFTER the fade so it stays skin.
    c.blob(SKIN, (0.044, 0.5905), 0.050, 0.050, 0.5, 1.0)
    c.stroke(SKIN_SHADE, [(0.028, 0.614), (0.048, 0.609), (0.054, 0.589), (0.044, 0.571)], 5.5, 0.6, 0.95, (0.5, 0.3))


def paint_head_back(c):
    c.mark(SKIN_SHADE, [(-0.28, 0.84), (-0.150, 0.84), (-0.140, 0.62), (-0.150, 0.47), (-0.28, 0.43)], 9, 0.8)
    c.mark(SKIN_SHADE, [(0.28, 0.84), (0.150, 0.84), (0.142, 0.63), (0.150, 0.47), (0.28, 0.43)], 9, 0.8)
    # the shade the mohawk's ridge throws either side of itself, down the back of the skull
    c.mark(SKIN_DEEP, [(-0.072, 0.80), (-0.046, 0.80), (-0.040, 0.60), (-0.034, 0.50), (-0.060, 0.50), (-0.066, 0.62)], 3, 0.5)
    c.mark(SKIN_DEEP, [(0.068, 0.80), (0.046, 0.80), (0.040, 0.61), (0.034, 0.50), (0.056, 0.50), (0.062, 0.63)], 3, 0.5)
    # (two neck folds were drawn at the nape; they went with rule 12's "no wrinkles")
    c.blob(SKIN_LIT, (-0.100, 0.690), 0.036, 0.050, 12, 0.35)
    c.blob(SKIN_LIT, (0.102, 0.684), 0.034, 0.052, 12, 0.35)
    # ears from behind
    c.blob(SKIN_SHADE, (-0.199, 0.593), 0.020, 0.034, 3.0, 0.6)
    c.blob(SKIN_SHADE, (0.199, 0.593), 0.020, 0.034, 3.0, 0.6)


def paint_head_top(c):
    # (x, y), the face at -y. A shaved crown: lit, with the ridge's shadow down the middle.
    c.blob(SKIN_LIT, (0.0, 0.0), 0.20, 0.15, 18, 0.6)
    c.mark(SKIN_SHADE, [(-0.066, -0.17), (0.070, -0.17), (0.064, 0.19), (-0.060, 0.19)], 3.5, 0.7, curved=False)


# ---------------------------------------------------------------------------
# THE BODY UNDER THE VEST. Bare sun-bronze chest and belly, the belt, the buckle, the seat of
# the trunks. The pectorals are two raised slabs (the model script); here they get a lit upper
# half and the shadow they throw on the belly. The belly's muscle is drawn, block by block.
# ---------------------------------------------------------------------------
BELT_Z = (0.249, 0.285)      # the strap; its two gold rims are geometry and overlap it
SEAT_TOP = 0.249


def _belt(c):
    c.band(BELT, BELT_Z[0] - 0.006, BELT_Z[1] + 0.004)
    c.band(BELT_LIT, BELT_Z[1] - 0.013, BELT_Z[1] - 0.008, 0.5, 0.9)
    c.band(BELT_DARK, BELT_Z[0] - 0.006, BELT_Z[0] + 0.006, 0.6, 0.9)


def _seat(c):
    c.band(RED, 0.16, SEAT_TOP - 0.006)
    c.band(RED_SHADE, 0.16, 0.214, 5, 0.6)


def paint_torso_front(c):
    c.mark(SKIN_SHADE, [(-0.21, 0.50), (-0.118, 0.50), (-0.104, 0.36), (-0.118, 0.28), (-0.21, 0.28)], 8, 0.8)
    c.mark(SKIN_SHADE, [(0.21, 0.50), (0.118, 0.50), (0.106, 0.37), (0.118, 0.28), (0.21, 0.28)], 8, 0.8)
    # the neck and the collarbones, in the shade of the jaw
    c.mark(SKIN_SHADE, [(-0.070, 0.50), (0.070, 0.50), (0.056, 0.462), (0.0, 0.452), (-0.056, 0.462)], 3, 0.9)
    c.stroke(SKIN_DEEP, [(-0.092, 0.464), (-0.050, 0.456), (-0.012, 0.450)], 3.4, 0.5, 0.9, (0.3, 0.8))
    c.stroke(SKIN_DEEP, [(0.094, 0.463), (0.052, 0.455), (0.012, 0.450)], 3.4, 0.5, 0.9, (0.3, 0.8))
    # the pectorals: lit on their upper half, a hard shadow thrown under each, a cleft between
    c.mark(SKIN_LIT, [(-0.094, 0.446), (-0.020, 0.448), (-0.018, 0.412), (-0.060, 0.400), (-0.096, 0.412)], 5, 0.75)
    c.mark(SKIN_LIT, [(0.094, 0.445), (0.022, 0.448), (0.019, 0.410), (0.062, 0.399), (0.097, 0.410)], 5, 0.75)
    c.stroke(SKIN_DEEP, [(-0.100, 0.380), (-0.058, 0.366), (-0.012, 0.372)], 6.0, 0.6, 0.95, (0.4, 0.7))
    c.stroke(SKIN_DEEP, [(0.101, 0.379), (0.060, 0.365), (0.012, 0.372)], 6.0, 0.6, 0.95, (0.4, 0.7))
    c.stroke(SKIN_DEEP, [(0.0, 0.452), (0.001, 0.410), (0.0, 0.370)], 4.0, 0.5, 0.95, (0.5, 0.9))
    # the belly: four blocks and the line between them, each block its own size
    c.stroke(SKIN_DEEP, [(0.0005, 0.366), (-0.0005, 0.330), (0.0005, 0.292)], 3.6, 0.5, 0.9, (0.9, 0.5))
    c.mark(SKIN_LIT, [(-0.064, 0.360), (-0.010, 0.361), (-0.010, 0.334), (-0.060, 0.332)], 3, 0.7)
    c.mark(SKIN_LIT, [(0.011, 0.361), (0.066, 0.359), (0.062, 0.333), (0.011, 0.335)], 3, 0.7)
    c.mark(SKIN_LIT, [(-0.058, 0.325), (-0.010, 0.327), (-0.010, 0.300), (-0.054, 0.299)], 3, 0.5)
    c.mark(SKIN_LIT, [(0.011, 0.327), (0.060, 0.325), (0.056, 0.298), (0.011, 0.300)], 3, 0.5)
    c.stroke(SKIN_DEEP, [(-0.068, 0.3305), (-0.036, 0.3290), (-0.008, 0.3310)], 3.0, 0.5, 0.85, (0.3, 0.8))
    c.stroke(SKIN_DEEP, [(0.008, 0.3310), (0.038, 0.3285), (0.069, 0.3300)], 3.0, 0.5, 0.85, (0.8, 0.3))
    c.stroke(SKIN_DEEP, [(-0.074, 0.362), (-0.080, 0.330), (-0.070, 0.296)], 3.0, 0.6, 0.7, (0.4, 0.4))
    c.stroke(SKIN_DEEP, [(0.076, 0.361), (0.081, 0.328), (0.071, 0.296)], 3.0, 0.6, 0.7, (0.4, 0.4))
    c.blob(SKIN_DEEP, (0.0, 0.3005), 0.0045, 0.0055, 0.6, 0.9)
    _seat(c)
    # the trunks' front: a centre seam, a gold welt on each hip (the original's thin gold bar)
    c.stroke(RED_DEEP, [(0.0, 0.244), (0.001, 0.220), (0.0, 0.180)], 5.0, 0.5, 1.0, (0.8, 0.8))
    c.stroke(GOLD, [(-0.150, 0.2265), (-0.060, 0.2285)], 4.2, 0.3, 1.0, (0.7, 0.4), curved=False)
    c.stroke(GOLD, [(0.058, 0.2285), (0.150, 0.2270)], 4.2, 0.3, 1.0, (0.4, 0.7), curved=False)
    _belt(c)
    # THE BUCKLE'S FACE (a gold plate standing off the belt): a bevelled frame round a leather core
    c.box(GOLD, -0.062, 0.062, 0.228, 0.306)
    c.box(GOLD_LIT, -0.058, 0.058, 0.296, 0.303, 0.6, 0.9)
    c.box(GOLD_DARK, -0.062, 0.062, 0.228, 0.238, 0.8, 0.9)
    c.box(GOLD_DEEP, -0.040, 0.040, 0.2455, 0.2895)
    c.box(BELT, -0.036, 0.036, 0.2490, 0.2860)
    c.box(BELT_LIT, -0.032, 0.020, 0.2775, 0.2825, 0.6, 0.9)
    c.box(BELT_DARK, -0.036, 0.036, 0.2490, 0.2550, 0.8, 0.9)


def paint_torso_back(c):
    c.mark(SKIN_SHADE, [(-0.21, 0.50), (-0.120, 0.50), (-0.108, 0.37), (-0.120, 0.28), (-0.21, 0.28)], 8, 0.8)
    c.mark(SKIN_SHADE, [(0.21, 0.50), (0.120, 0.50), (0.108, 0.37), (0.120, 0.28), (0.21, 0.28)], 8, 0.8)
    c.stroke(SKIN_DEEP, [(0.0, 0.47), (0.001, 0.38), (0.0, 0.292)], 4.5, 0.5, 0.9, (0.7, 0.7))
    _seat(c)
    c.stroke(RED_DEEP, [(0.0, 0.244), (-0.001, 0.222), (0.0, 0.180)], 5.0, 0.5, 1.0, (0.8, 0.8))
    c.stroke(RED_SHADE, [(-0.120, 0.240), (-0.090, 0.222), (-0.050, 0.212)], 5.0, 0.8, 0.8, (0.4, 0.8))
    c.stroke(RED_SHADE, [(0.124, 0.240), (0.096, 0.224), (0.060, 0.210)], 5.0, 0.8, 0.8, (0.4, 0.8))
    c.stroke(GOLD, [(-0.146, 0.2275), (-0.050, 0.2285)], 4.2, 0.3, 1.0, (0.7, 0.4), curved=False)
    c.stroke(GOLD, [(0.048, 0.2285), (0.146, 0.2270)], 4.2, 0.3, 1.0, (0.4, 0.7), curved=False)
    _belt(c)


def _torso_side(c):
    c.mark(SKIN_SHADE, [(-0.16, 0.50), (0.16, 0.50), (0.16, 0.28), (-0.16, 0.28)], 0, 0.7, curved=False)
    _seat(c)
    c.box(GOLD, -0.046, 0.046, 0.16, SEAT_TOP - 0.006)
    _belt(c)


def paint_torso_top(c):
    # (x, y). The vest lies over both shoulders (the original's `vest-shoulder-trim` slabs, x from
    # 0.100 out); between them is the bare neck, in the shade of the head that sits on it.
    c.box(RED, -0.21, -0.098, -0.16, 0.16)
    c.box(RED, 0.098, 0.21, -0.16, 0.16)
    c.box(RED_LIT, -0.21, -0.112, -0.060, 0.050, 6.0, 0.7)
    c.box(RED_LIT, 0.112, 0.21, -0.060, 0.050, 6.0, 0.7)
    c.box(GOLD, -0.110, -0.098, -0.16, 0.16)
    c.box(GOLD, 0.098, 0.110, -0.16, 0.16)
    c.box(SKIN_DEEP, -0.098, 0.098, -0.16, 0.16)


# ---------------------------------------------------------------------------
# THE VEST. Red, sleeveless, open on the chest. Its piping (lapels, hem, the edge over the
# shoulders, the armholes) is GEOMETRY in the model script (CAST_CLOTHING_STYLE.md rule 3); what
# is drawn here is the cloth: a lit yoke, shade toward the hem, folds, seams, stitches, and on
# the back the frame of his sun (the gold plate itself is a block; its core is painted on it).
# ---------------------------------------------------------------------------
HEM = (0.288, 0.314)
SUN = (0.348, 0.432)      # the plate on the back, x from -0.042 to 0.042


def paint_vest_front(c):
    c.mark(RED_LIT, [(-0.200, 0.478), (-0.084, 0.480), (-0.078, 0.420), (-0.120, 0.396), (-0.200, 0.404)], 9, 0.8)
    c.mark(RED_LIT, [(0.200, 0.478), (0.086, 0.480), (0.080, 0.416), (0.124, 0.392), (0.200, 0.402)], 9, 0.75)
    c.mark(RED_SHADE, [(-0.22, 0.372), (-0.150, 0.362), (-0.108, 0.330), (-0.100, 0.27), (-0.22, 0.27)], 7, 0.75)
    c.mark(RED_SHADE, [(0.22, 0.368), (0.152, 0.360), (0.110, 0.326), (0.100, 0.27), (0.22, 0.27)], 7, 0.75)
    # folds pulled down and in toward the buckle
    c.stroke(RED_SHADE, [(-0.150, 0.402), (-0.126, 0.360), (-0.108, 0.320)], 6.5, 0.8, 0.85, (0.1, 0.6))
    c.stroke(RED_SHADE, [(0.156, 0.396), (0.130, 0.356), (0.114, 0.322)], 6.0, 0.8, 0.85, (0.1, 0.6))
    c.stroke(RED_LIT, [(-0.172, 0.380), (-0.158, 0.348), (-0.150, 0.322)], 5.0, 1.0, 0.6, (0.3, 0.6))
    # a row of stitches inside each lapel's piping, and one along each shoulder
    c.stitch(RED_DEEP, [(-0.098, 0.468), (-0.106, 0.400), (-0.098, 0.326)], 1.8, 6.0, 4.5)
    c.stitch(RED_DEEP, [(0.100, 0.468), (0.108, 0.398), (0.099, 0.326)], 1.8, 6.0, 4.5)
    c.stitch(RED_DEEP, [(-0.186, 0.452), (-0.150, 0.458), (-0.112, 0.460)], 1.8, 6.0, 4.5)
    c.stitch(RED_DEEP, [(0.186, 0.451), (0.152, 0.457), (0.114, 0.459)], 1.8, 6.0, 4.5)
    # ONE mended tear, on his left panel, in the open: a dark slit, a pale red lip, gold thread
    # across it in four stitches of four lengths (rule 5: hard edged, rimmed, never half hidden)
    c.mark(RED_DEEP, [(0.126, 0.366), (0.164, 0.350), (0.166, 0.343), (0.128, 0.358)], 0.0, 1.0, curved=False)
    c.stroke(RED_LIT, [(0.125, 0.370), (0.165, 0.354)], 1.8, 0.2, 1.0, (0.6, 0.6), curved=False)
    c.stroke(GOLD, [(0.132, 0.371), (0.135, 0.352)], 2.0, 0.2, 1.0, (0.9, 0.9), curved=False)
    c.stroke(GOLD, [(0.142, 0.368), (0.144, 0.347)], 2.0, 0.2, 1.0, (0.9, 0.9), curved=False)
    c.stroke(GOLD, [(0.151, 0.363), (0.154, 0.344)], 2.0, 0.2, 1.0, (0.9, 0.9), curved=False)
    c.stroke(GOLD, [(0.160, 0.359), (0.162, 0.341)], 2.0, 0.2, 1.0, (0.9, 0.9), curved=False)


def paint_vest_back(c):
    c.mark(RED_LIT, [(-0.196, 0.480), (0.196, 0.480), (0.180, 0.436), (0.070, 0.446), (-0.060, 0.442), (-0.182, 0.432)], 9, 0.75)
    c.mark(RED_SHADE, [(-0.22, 0.380), (-0.156, 0.370), (-0.132, 0.320), (-0.22, 0.27)], 7, 0.75)
    c.mark(RED_SHADE, [(0.22, 0.376), (0.158, 0.366), (0.134, 0.318), (0.22, 0.27)], 7, 0.75)
    # the centre back seam, above and below the sun
    c.stroke(RED_SHADE, [(0.0, 0.478), (0.001, 0.456), (0.0, 0.436)], 3.4, 0.5, 0.95, (0.8, 0.8))
    c.stroke(RED_SHADE, [(0.0, 0.344), (-0.001, 0.330), (0.0, 0.316)], 3.4, 0.5, 0.95, (0.8, 0.8))
    # yoke seam across the shoulder blades, stitched
    c.stitch(RED_DEEP, [(-0.176, 0.446), (-0.090, 0.452), (0.0, 0.449), (0.092, 0.452), (0.176, 0.445)], 1.8, 6.5, 4.5)
    # folds
    c.stroke(RED_SHADE, [(-0.110, 0.420), (-0.096, 0.372), (-0.104, 0.326)], 6.0, 0.8, 0.8, (0.2, 0.6))
    c.stroke(RED_SHADE, [(0.120, 0.414), (0.104, 0.368), (0.110, 0.328)], 5.5, 0.8, 0.8, (0.2, 0.6))
    c.stroke(RED_LIT, [(-0.070, 0.342), (-0.062, 0.328), (-0.066, 0.318)], 5.0, 1.0, 0.5, (0.3, 0.6))
    # HIS SUN (the original's `vest-back-solar-*` blocks): a dark seat round the gold plate, then
    # on the plate itself the orange core and the yellow dot, each a square inside the last
    c.box(RED_DEEP, -0.048, 0.048, SUN[0] - 0.006, SUN[1] + 0.006)
    c.box(GOLD, -0.042, 0.042, SUN[0], SUN[1])
    c.box(GOLD_LIT, -0.038, 0.038, SUN[1] - 0.010, SUN[1] - 0.004, 0.5, 0.9)
    c.box(GOLD_DARK, -0.042, 0.042, SUN[0], SUN[0] + 0.008, 0.6, 0.9)
    c.box(GOLD_DEEP, -0.028, 0.028, 0.362, 0.418)
    c.box(FLAME_O, -0.025, 0.025, 0.365, 0.415)
    c.box(FLAME_O_DARK, -0.025, 0.025, 0.365, 0.372, 0.6, 0.8)
    c.box(FLAME_Y, -0.0115, 0.0115, 0.3785, 0.4015)


def _vest_side(c):
    c.mark(RED_SHADE, [(-0.16, 0.36), (0.16, 0.36), (0.16, 0.27), (-0.16, 0.27)], 8, 0.8, curved=False)
    c.stroke(RED_DEEP, [(0.004, 0.350), (0.002, 0.330), (0.004, 0.316)], 2.8, 0.5, 0.9, (0.7, 0.8))


def paint_vest_top(c):
    c.blob(RED_LIT, (0.0, 0.0), 0.21, 0.10, 10, 0.7)


# ---------------------------------------------------------------------------
# THE ARMS. Bare and heavy: a deltoid cap, a bicep slab, then the bracer on the forearm (red,
# an orange strap round it, gold rims and a gold plate as geometry), the wrist and the fist.
# Both arms are drawn from their own numbers. The arm runs along x from 0.14 to 0.405; the
# bracer's cloth shows between its two rims, x 0.256 to 0.318.
# ---------------------------------------------------------------------------
BRACER = (0.246, 0.336)   # it starts 8 mm past the elbow: nearer, its rim dug into the vest's side in the idle
STRAP = (0.279, 0.297)
PLATE_X = (0.266, 0.310)
PLATE_Z = (0.390, 0.470)
FIST = 0.338
ARM_MID = 0.430


def _arm_skin(c, s, view):
    x = lambda v: s * v
    if view == "bottom":
        c.column(SKIN_SHADE, *sorted((x(0.10), x(0.43))), 0, 1.0)   # the flat tone the down-turned chamfers wear
    elif view == "top":
        c.mark(SKIN_LIT, [(x(0.150), -0.050), (x(0.232), -0.046), (x(0.232), 0.070), (x(0.150), 0.076)], 9, 0.7)
        # where the deltoid ends and the arm begins
        c.stroke(SKIN_SHADE, [(x(0.222), -0.070), (x(0.228), 0.012), (x(0.221), 0.092)], 3.4, 0.6, 0.8, (0.5, 0.5))
    else:
        k = 1.0 if view == "front" else 0.8
        c.band(SKIN_SHADE, 0.30, 0.392, 4, 1.0)
        c.mark(SKIN_LIT, [(x(0.150), 0.492), (x(0.214), 0.488), (x(0.210), 0.452), (x(0.154), 0.456)], 7, 0.7 * k)
        # the deltoid's lower edge, a hook that ends on the bicep
        c.stroke(SKIN_DEEP, [(x(0.148), 0.402), (x(0.196), 0.398), (x(0.222), 0.424), (x(0.224), 0.462)], 3.6, 0.5, 0.9,
                 (0.3, 0.7))
        if view == "front":
            # the bicep slab: lit along its top, its belly shaded
            c.mark(SKIN_LIT, [(x(0.190), 0.456), (x(0.236), 0.454), (x(0.236), 0.432), (x(0.192), 0.430)], 4, 0.65)
        else:
            # the tricep's horseshoe, seen from behind
            c.stroke(SKIN_DEEP, [(x(0.196), 0.470), (x(0.226), 0.446), (x(0.232), 0.410)], 3.2, 0.5, 0.85, (0.4, 0.6))


def _plate(c, s):
    """THE PLATE'S FACE, on the front view only: the one drawing the bracer still carries. The
    red cloth, the orange strap and the gold rims are separate pieces in flat tones (the model
    script), so nothing of them is painted on these islands."""
    p0, p1 = sorted((s * PLATE_X[0], s * PLATE_X[1]))
    c.box(GOLD, p0 - 0.004, p1 + 0.004, PLATE_Z[0] - 0.004, PLATE_Z[1] + 0.004)
    c.box(GOLD_DEEP, p0 + 0.009, p1 - 0.009, PLATE_Z[0] + 0.013, PLATE_Z[1] - 0.015)
    c.box(GOLD_DARK, p0 + 0.011, p1 - 0.011, PLATE_Z[0] + 0.015, PLATE_Z[1] - 0.017)


def _fist(c, s):
    """THE FIST'S FRONT FACE, the only face of the hand that is drawn on: flat skin, the three
    folded fingers and the thumb laid over them. Few lines, bold. No shading: the faces round it
    are one flat skin tone and this has to meet them."""
    x = lambda v: s * v
    a0, a1 = sorted((x(BRACER[1]), x(0.45)))
    c.box(SKIN, a0, a1, 0.2, 0.6)
    c.stroke(SKIN_DEEP, [(x(0.366), 0.449), (x(0.396), 0.450)], 3.4, 0.3, 1.0, (0.9, 0.6), curved=False)
    c.stroke(SKIN_DEEP, [(x(0.366), 0.428), (x(0.397), 0.427)], 3.4, 0.3, 1.0, (0.9, 0.6), curved=False)
    c.stroke(SKIN_DEEP, [(x(0.366), 0.407), (x(0.395), 0.405)], 3.4, 0.3, 1.0, (0.9, 0.6), curved=False)
    c.stroke(SKIN_DEEP, [(x(0.350), 0.466), (x(0.374), 0.468), (x(0.382), 0.477)], 3.4, 0.3, 1.0, (0.8, 0.5))


def paint_arm(c, s, view):
    """Only faces that SQUARELY face a view sample these islands (the model script's
    `proj_square` and `proj_one`), so nothing here can be smeared down a chamfer."""
    if view in ("xpos", "xneg"):
        return
    _arm_skin(c, s, view)
    if view == "front":
        _plate(c, s)
        _fist(c, s)


# ---------------------------------------------------------------------------
# THE LEGS, top to bottom: red trunks with a gold stripe down the outside, a wide gold band at
# the thigh with a dark groove in it, a bare shin, the red high-top with its white sole. The
# orange tongue and the ankle strap are geometry (the model script) and wear flat tones.
# ---------------------------------------------------------------------------
SOLE = 0.026
SHOE = 0.094
BAND = (0.108, 0.155)


def paint_leg(c, s, view):
    """`s` is +1 for his left leg, -1 for his right. Each leg's marks are its own."""
    x = lambda v: s * v
    outer = (view == "xpos") == (s > 0)
    # trunks
    c.band(RED, BAND[1], 0.30)
    if view == "front":
        c.mark(RED_LIT, [(x(0.050), 0.236), (x(0.142), 0.238), (x(0.146), 0.190), (x(0.056), 0.186)], 7, 0.7)
        c.stroke(RED_SHADE, [(x(0.030), 0.244), (x(0.044), 0.206), (x(0.034), 0.164)], 5.0, 0.7, 0.85, (0.4, 0.7))
        if s > 0:
            c.stroke(RED_SHADE, [(0.150, 0.240), (0.132, 0.204), (0.140, 0.170)], 4.0, 0.7, 0.7, (0.4, 0.7))
        else:
            c.stroke(RED_SHADE, [(-0.120, 0.196), (-0.090, 0.184), (-0.060, 0.188)], 4.0, 0.7, 0.7, (0.5, 0.5))
    elif view == "back":
        c.mark(RED_SHADE, [(x(0.01), 0.27), (x(0.19), 0.27), (x(0.19), 0.214), (x(0.01), 0.208)], 6, 0.55)
        c.stroke(RED_SHADE, [(x(0.070), 0.236), (x(0.104), 0.200), (x(0.088), 0.164)], 5.0, 0.7, 0.8)
    else:
        if outer:
            # the gold stripe down the outside of the trunks (the original's `shorts-side-gold`)
            c.box(GOLD, -0.050, 0.050, BAND[1], 0.30)
            c.box(GOLD_DARK, -0.050, -0.043, BAND[1], 0.30, 0.5, 0.9)
            c.box(GOLD_LIT, 0.030, 0.040, BAND[1], 0.30, 0.8, 0.7)
        else:
            c.mark(RED_SHADE, [(-0.19, 0.30), (0.13, 0.30), (0.13, BAND[1]), (-0.19, BAND[1])], 0, 0.7, curved=False)
    c.stitch(RED_DEEP, [(-0.25, 0.164), (0.0, 0.1645), (0.25, 0.164)], 1.6, 6.0, 4.5)
    # the thin gold welt across the trunks (the original's gold bar under the belt), front and back
    if view == "front":
        c.stroke(GOLD, [(x(0.034), 0.2255), (x(0.150), 0.2240)], 4.2, 0.3, 1.0, (0.5, 0.8), curved=False)
    elif view == "back":
        c.stroke(GOLD, [(x(0.030), 0.2270), (x(0.146), 0.2250)], 4.2, 0.3, 1.0, (0.5, 0.8), curved=False)
    # the bare shin
    c.band(SKIN, SHOE - 0.03, BAND[0])
    c.band(SKIN_SHADE, SHOE - 0.03, BAND[0], 0, 0.45)
    # the thigh band: gold, a lit top edge, a dark groove (the original's `thigh-trim-inner`)
    c.band(GOLD, BAND[0], BAND[1])
    c.band(GOLD_LIT, BAND[1] - 0.008, BAND[1] - 0.003, 0.5, 0.9)
    c.band(GOLD_DARK, 0.1145, 0.1305, 0.4, 1.0)
    c.band(GOLD_DEEP, 0.1265, 0.1305, 0.4, 0.8)
    # the shoe: red, shaded low, a stitched panel line; then the sole
    c.band(FLAME_R, 0.0, SHOE + 0.004)
    c.band(FLAME_R_DARK, 0.0, 0.038, 3.0, 0.45)
    c.band(FLAME_R_LIT, 0.078, 0.088, 2.0, 0.5)
    if view == "front":
        # the toe box: a seam over it, one scuff on his right shoe
        c.stroke(FLAME_R_DARK, [(x(0.028), 0.047), (x(0.097), 0.056), (x(0.166), 0.047)], 2.6, 0.4, 1.0, (0.6, 0.6))
        if s < 0:
            c.mark(SCUFF, [(-0.052, 0.042), (-0.090, 0.045), (-0.086, 0.033), (-0.056, 0.032)], 1.5, 0.8)
    elif view == "back":
        # the heel counter: a darker panel with a stitched edge, a gold pull at its top
        c.box(FLAME_R_DARK, *sorted((x(0.052), x(0.142))), SOLE, 0.082)
        c.stitch(RED_DEEP, [(x(0.060), 0.034), (x(0.060), 0.076), (x(0.134), 0.076), (x(0.134), 0.034)], 1.6, 5.0, 4.0)
        c.box(GOLD, *sorted((x(0.086), x(0.108))), 0.076, 0.092)
    else:
        # the side: the seam where the toe box meets the quarter, and a panel line along the top
        c.stroke(FLAME_R_DARK, [(-0.092, 0.028), (-0.078, 0.052), (-0.074, 0.084)], 2.6, 0.4, 1.0, (0.7, 0.7))
        c.stitch(RED_DEEP, [(-0.060, 0.050), (0.010, 0.046), (0.080, 0.052)], 1.6, 5.5, 4.0)
        if outer and s > 0:
            c.mark(SCUFF, [(-0.150, 0.040), (-0.112, 0.043), (-0.116, 0.031), (-0.146, 0.030)], 1.5, 0.8)
    c.band(WHITE, -0.02, SOLE)
    c.band(WHITE_SHADE, -0.02, 0.011, 0.4, 0.9)
    c.band(WHITE_DIRT, -0.02, 0.005, 1.0, 0.5)


def paint_leg_top(c, s):
    """Looking down on a foot and on the top of the thigh band. The toe is at -y."""
    x = lambda v: s * v
    c.mark(FLAME_R, [(x(0.0), 0.14), (x(0.20), 0.14), (x(0.20), -0.20), (x(0.0), -0.20)], 0, 1.0, curved=False)
    c.mark(FLAME_R_LIT, [(x(0.050), -0.150), (x(0.144), -0.150), (x(0.148), -0.118), (x(0.046), -0.118)], 4, 0.6)
    # the toe box seam, bowed, and the white rim of the sole showing round it
    c.stroke(FLAME_R_DARK, [(x(0.022), -0.110), (x(0.097), -0.122), (x(0.172), -0.110)], 2.8, 0.4, 1.0, (0.6, 0.6))
    if s > 0:
        c.mark(SCUFF, [(0.118, -0.136), (0.150, -0.130), (0.150, -0.142), (0.124, -0.147)], 1.0, 0.7)


# ---------------------------------------------------------------------------
# THE SWATCHES
# ---------------------------------------------------------------------------

def paint_gold(c):
    # piping: a pale top edge, a dark under edge, three worn dull places, each its own length
    c.mark(GOLD_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.70), (0.0, 0.70)], 1.2, 0.85, curved=False)
    c.mark(GOLD_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.24), (0.0, 0.24)], 1.2, 0.9, curved=False)
    c.mark(GOLD_DARK, [(0.10, 0.30), (0.19, 0.30), (0.18, 0.74), (0.11, 0.72)], 2.0, 0.4)
    c.mark(GOLD_DARK, [(0.44, 0.28), (0.61, 0.30), (0.60, 0.70), (0.46, 0.72)], 2.0, 0.35)
    c.mark(GOLD_DARK, [(0.80, 0.30), (0.89, 0.30), (0.89, 0.74), (0.81, 0.70)], 2.0, 0.4)


FLAT = {
    "hair": HAIR, "hair_top": HAIR_TOP, "hair_under": HAIR_DEEP,
    "flame_y": FLAME_Y, "flame_y_top": FLAME_Y_LIT, "flame_y_under": FLAME_Y_DARK,
    "flame_o": FLAME_O, "flame_o_top": FLAME_O_LIT, "flame_o_under": FLAME_O_DARK,
    "flame_r": FLAME_R, "flame_r_top": FLAME_R_LIT, "flame_r_under": FLAME_R_DARK,
    "gold_tone": GOLD, "gold_dark": GOLD_DARK, "skin_tone": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "belt_tone": BELT_DARK, "red_tone": RED_SHADE, "lining": RED_DEEP, "tongue": TONGUE,
    "tongue_dark": TONGUE_DARK, "sole": WHITE_SHADE, "white": WHITE, "gold_lit": GOLD_LIT, "skin": SKIN, "red": RED, "red_lit": RED_LIT,
}


def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    c = I("head.xpos", SKIN); _head_side(c, 1); done[c.name] = c
    c = I("head.xneg", SKIN); _head_side(c, -1); done[c.name] = c
    c = I("head.back", mix(SKIN, SKIN_SHADE, 0.6)); paint_head_back(c); done[c.name] = c
    c = I("head.top", SKIN); paint_head_top(c); done[c.name] = c
    c = I("head.bottom", SKIN_SHADE); done[c.name] = c

    c = I("torso.front", SKIN); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", SKIN); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", SKIN); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", SKIN); _torso_side(c); done[c.name] = c
    c = I("torso.top", SKIN); paint_torso_top(c); done[c.name] = c

    c = I("vest.front", RED); paint_vest_front(c); done[c.name] = c
    c = I("vest.back", RED); paint_vest_back(c); done[c.name] = c
    c = I("vest.xpos", RED); _vest_side(c); done[c.name] = c
    c = I("vest.xneg", RED); _vest_side(c); done[c.name] = c
    c = I("vest.top", RED); paint_vest_top(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN); paint_arm(c, s, view); done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, RED)
            if view == "top":
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c

    for name, colour in FLAT.items():
        c = I(name, colour, SWATCHES[name][2:4]); done[c.name] = c
    c = I("gold", GOLD, SWATCHES["gold"][2:4]); paint_gold(c); done[c.name] = c

    missing = set(layout(size)) - set(done)
    if missing:
        raise SystemExit("islands not painted: %s" % sorted(missing))
    return done


def _role_share(finished):
    """The share of painted pixels that sit on a role hue: (orange, blue)."""
    total = orange = blue = 0
    for img in finished.values():
        for count, rgb in img.getcolors(maxcolors=1 << 24):
            total += count
            dh, s, v = _hue_gap(rgb, "f87020")
            if dh < 0.035 and s > 0.8 and v > 0.8:
                orange += count
            dh, s, v = _hue_gap(rgb, "0080e8")
            if dh < 0.06 and s > 0.4 and v > 0.4:
                blue += count
    return orange / float(total), blue / float(total)


def main():
    from PIL import Image

    size = ATLAS
    if "--size" in sys.argv:
        size = int(sys.argv[sys.argv.index("--size") + 1])
    for hex_str in CLOTH_HEXES:
        if _near_role_hue(hex_str):
            raise SystemExit("#%s sits near role hue #%s" % (hex_str, _near_role_hue(hex_str)))

    islands = build(size)
    finished = {name: isl.finished() for name, isl in islands.items()}
    orange, blue = _role_share(finished)
    print("role hue share of the painted atlas: orange %.2f%% (budget %.1f%%), blue %.2f%%"
          % (100 * orange, 100 * ORANGE_BUDGET, 100 * blue))
    if orange > ORANGE_BUDGET or blue > 0.0:
        raise SystemExit("a role hue covers too much of him")

    # The bottom half is where Toon.shader reads the palette instead of this file. Left one flat tone.
    atlas = Image.new("RGB", (size, size), _rgb(RED_DEEP))
    bleed = max(1, int(round(GUTTER * size / float(ATLAS))))
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
