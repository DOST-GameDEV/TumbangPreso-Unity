"""Paint the atlas of the Zack (displayed: Isagani) redesign PROTOTYPE, by hand, in the house style.

    py -3 tools/author_character_redesign_zack_textures.py [--size 1024] [--sheet out.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/zack/zack-redesign-atlas.png. The model is built
by tools/author_character_redesign_zack.py, which imports this file for the LAYOUT only (PIL is
imported inside the paint calls, Blender's Python has none) and so maps every face onto the island
painted for it here. Paint first, then build.

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 13: the owner asked for the rest of the cast to
follow Dante's rework, one set of files a hero. This is Zack's own copy of the Dante painter,
rewritten for him. Nothing in the game loads these files and team-zack.glb is not touched.

WHO HE IS, read off team-zack.glb and person_zack.asset before anything was drawn (the builder
tools/build_zack_voxel.py has drifted from the file, so the FILE is the reference):
  * caramel skin, half-lidded ink eyes with no brows, a smirk that rises on his left
  * a spiky black mop with an electric yellow quiff swept over his left brow (his silhouette cue,
    Voxel_Person_Guide.md section 6), a gold hoop and drop in his left ear
  * an open electric yellow short-sleeved jacket trimmed in ochre (hem, cuffs, lapels, pocket
    welts, a back collar, a shoulder tab), a black shirt with a V neck, a crystal pendant on a
    silver chain, a gold pin on his left lapel
  * a black belt with a gold buckle, a silver wallet chain down his right thigh
  * black cargo trousers (thigh strap, side pocket with a flap and a steel buckle, knee panel)
  * yellow skate shoes with white soles, toe caps, heel counters and laces
  * a neon yellow band on each forearm under the cuff
Nothing here is invented. Every colour below is a palette slot of his or a step of one.

THE ONE MECHANISM is the beggar's (tools/build_beggar_voxel.py): `Toon.shader` remaps a UV to a
palette slot only in Unity atlas rows 0 to 7 and samples the texture itself above them, so every
island lives in the TOP half of the file and the bottom half is left flat.

HOW AN ISLAND IS LAID OUT. A body part is seen from up to six sides and each side is one island, a
flat orthographic view of the part in MODEL METRES. A mark is written where it sits on the body:
`(x, z)` on a front or back view, `(y, z)` on a side view, `(x, y)` on a top view. +x is HIS left
(the quiff, the earring, the lapel pin), -y is the way he faces, z is up, the feet are on zero.

THE RULES THIS FILE KEEPS (section 13 of the handoff, each one a thing the owner said):
  * no painted hair shine: hair is flat tones and takes no drawing at all (rule 3)
  * paint stops at its own piece's edges: nothing drawn for a sleeve lands on a hand, nothing
    drawn for hair lands on an ear (rule 4). Cuffs, bands, straps and trims are their own blocks in
    their own flat tones, so there is no band of paint to stray.
  * every mark is its own hand-set shape with its own numbers; nothing loops a motif round a limb
  * role hues (#f87020, #0080e8) stay off cloth; the check is at the bottom
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "zack"
ATLAS_NAME = "zack-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is the original palette slot, unchanged.
# ---------------------------------------------------------------------------
SKIN = "c77a45"; SKIN_LIT = "d88f5a"; SKIN_SHADE = "ad6537"; SKIN_DEEP = "925027"; SKIN_LINE = "6f3a1a"
HAIR = "14121a"; HAIR_TOP = "2a2733"; HAIR_DEEP = "09080c"
CREST = "faf238"; CREST_TOP = "fcf88c"; CREST_UNDER = "cdbd1a"
INK = "1f1f24"
JACKET = "e8d11f"; JACKET_LIT = "f4e455"; JACKET_SHADE = "c8ac16"
OCHRE = "a68014"; OCHRE_LIT = "c59d22"; OCHRE_DARK = "7a5c0c"
CLOTH = "14121a"; CLOTH_LIT = "2b2834"; CLOTH_HI = "423f4e"; CLOTH_DEEP = "0a090d"
DUST = "5d564f"
SHOE = "e0bd1a"; SHOE_LIT = "f0d64c"; SHOE_SHADE = "b3930f"
WHITE = "ffffff"; WHITE_SHADE = "cfd3da"; SCUFF = "a8a39a"
SILVER = "d4e2ec"; SILVER_LIT = "f3f9fd"; SILVER_SHADE = "93a5b3"
STEEL = "525460"
CRYSTAL = "f5eb59"; CRYSTAL_LIT = "fafaa6"; CRYSTAL_DEEP = "c4ad22"
# The buckle, the earring and the lapel pin. #f5a820 is his own palette slot 3 and it does sit
# close to the offence orange; it is three small fastenings and never a cloth area, which is the
# rule as written (role hues off LARGE areas). Kept out of the cloth check on purpose.
GOLD = "f5a820"; GOLD_LIT = "ffd067"; GOLD_DARK = "b9770f"

CLOTH_HEXES = [HAIR, HAIR_TOP, CREST, CREST_TOP, CREST_UNDER, JACKET, JACKET_LIT, JACKET_SHADE, OCHRE,
               OCHRE_LIT, OCHRE_DARK, CLOTH, CLOTH_LIT, CLOTH_HI, DUST, SHOE, SHOE_LIT, SHOE_SHADE,
               WHITE, WHITE_SHADE, SCUFF, SILVER, SILVER_SHADE, STEEL, CRYSTAL, CRYSTAL_LIT, CRYSTAL_DEEP]

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y).
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.26, 0.26, 0.30, 0.70), 1300),
        "back": ((-0.26, 0.26, 0.30, 0.70), 420),
        "xpos": ((-0.20, 0.20, 0.30, 0.70), 800),
        "xneg": ((-0.20, 0.20, 0.30, 0.70), 800),
        "top": ((-0.26, 0.26, -0.20, 0.20), 260),
        "bottom": ((-0.26, 0.26, -0.20, 0.20), 260),
    },
    # the jacket
    "torso": {
        "front": ((-0.20, 0.20, 0.15, 0.37), 1150),
        "back": ((-0.20, 0.20, 0.15, 0.37), 1150),
        "xpos": ((-0.15, 0.15, 0.15, 0.37), 950),
        "xneg": ((-0.15, 0.15, 0.15, 0.37), 950),
        "top": ((-0.20, 0.20, -0.15, 0.15), 560),
    },
    # the black shirt in the jacket's opening: only its front is ever seen
    "shirt": {
        "front": ((-0.12, 0.12, 0.16, 0.36), 1150),
    },
    "armL": {
        "front": ((0.09, 0.30, 0.20, 0.375), 1000),
        "back": ((0.09, 0.30, 0.20, 0.375), 1000),
        "top": ((0.09, 0.30, -0.08, 0.10), 1000),
        "bottom": ((0.09, 0.30, -0.08, 0.10), 900),
        "xpos": ((-0.08, 0.10, 0.21, 0.37), 800),
    },
    "armR": {
        "front": ((-0.30, -0.09, 0.20, 0.375), 1000),
        "back": ((-0.30, -0.09, 0.20, 0.375), 1000),
        "top": ((-0.30, -0.09, -0.08, 0.10), 1000),
        "bottom": ((-0.30, -0.09, -0.08, 0.10), 900),
        "xneg": ((-0.08, 0.10, 0.21, 0.37), 800),
    },
    "legL": {
        "front": ((0.0, 0.18, 0.0, 0.19), 1000),
        "back": ((0.0, 0.18, 0.0, 0.19), 900),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 1000),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 850),
        "top": ((0.0, 0.18, -0.16, 0.11), 1000),
    },
    "legR": {
        "front": ((-0.18, 0.0, 0.0, 0.19), 1000),
        "back": ((-0.18, 0.0, 0.0, 0.19), 900),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 850),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 1000),
        "top": ((-0.18, 0.0, -0.16, 0.11), 1000),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group. The face
# is drawn on the front island, so the head's front reaches round its cut corners toward the ears.
VIEW_BIAS = {("head", "front"): 1.5, ("head", "top"): 0.8, ("torso", "front"): 1.15, ("torso", "back"): 1.15,
             ("legL", "top"): 0.9, ("legR", "top"): 0.9}

# Flat tones: one small square each. A piece that is its own block (hair, cuff, strap, trim, chain)
# takes these and no drawing, which is what keeps one piece's paint off its neighbour.
TONES = {
    "hair": HAIR, "hair_top": HAIR_TOP, "hair_under": HAIR_DEEP,
    "crest": CREST, "crest_top": CREST_TOP, "crest_under": CREST_UNDER,
    "skin_tone": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "jacket_tone": JACKET, "jacket_shade": JACKET_SHADE,
    "ochre": OCHRE, "ochre_lit": OCHRE_LIT, "ochre_dark": OCHRE_DARK,
    "cloth_tone": CLOTH, "cloth_lit": CLOTH_LIT, "cloth_hi": CLOTH_HI, "cloth_deep": CLOTH_DEEP,
    "gold_tone": GOLD, "gold_lit": GOLD_LIT, "gold_dark": GOLD_DARK,
    "silver": SILVER, "silver_lit": SILVER_LIT, "silver_shade": SILVER_SHADE,
    "steel": STEEL,
    "white": WHITE, "white_shade": WHITE_SHADE,
    "shoe_tone": SHOE, "shoe_shade": SHOE_SHADE,
    "crystal": CRYSTAL, "crystal_lit": CRYSTAL_LIT, "crystal_deep": CRYSTAL_DEEP,
}
# name -> (px wide, px high at 2048, metres wide, metres high)
SWATCHES = {name: (36, 36, 0.04, 0.04) for name in TONES}

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
        """A plain rectangle, for a mark that must stop at a piece's edges."""
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
        """Everything between two heights, the whole island wide."""
        a0, a1 = self.win[0], self.win[1]
        pad = (a1 - a0)
        return self.mark(colour, [(a0 - pad, b0), (a1 + pad, b0), (a1 + pad, b1), (a0 - pad, b1)],
                         feather, strength, curved=False)

    def column(self, colour, a0, a1, feather=0.0, strength=1.0):
        """Everything between two positions along the first axis: a sleeve round an arm."""
        b0, b1 = self.win[2], self.win[3]
        pad = (b1 - b0)
        return self.mark(colour, [(a0, b0 - pad), (a1, b0 - pad), (a1, b1 + pad), (a0, b1 + pad)],
                         feather, strength, curved=False)

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE HEAD. Form painted under a flat ramp: flat skin, one soft lit patch, the shadow his mop throws
# on his brow as one flat tone. No blush (the owner keeps it for the girls). The features are the original's
# and nothing more: two solid flat ink eyes, NO brows, NO glints, NO nose, and the smirk that
# rises on his left (rule 12; see `paint_head_front`).
# ---------------------------------------------------------------------------
HAIRLINE = 0.506     # the foot of the hair cap in the model script; the two must agree


# THE FACE INK, MEASURED. These are the outlines of the three ink shapes on team-zack.glb's head
# mesh (palette slot 8, the plane at y -0.160), read vertex for vertex, (x, z) with +x his left.
# The owner on two other heroes of this redesign: one "lost the eye quirk", one lost her "slight
# smug look". An eye's outline IS the expression, so these are not redrawn from a render.
#   HIS EYE is a wide flat lozenge with a NOTCH in the middle of its bottom edge (0.4682 between
#   two corners at 0.4644): a heavy lid over a lower lid pushed up, his half-shut look. The two
#   eyes are exact mirrors. v09 to v12 drew a plain lozenge and lost the notch.
#   HIS MOUTH is one slanted wedge, THIN at his right end (3 mm) and THICK at the raised left end
#   (9 mm). v01 to v12 tapered it the other way round.
EYE_LEFT = [(0.0721, 0.4682), (0.0932, 0.4644), (0.1003, 0.4760), (0.0932, 0.4876),
            (0.0721, 0.4915), (0.0510, 0.4876), (0.0439, 0.4760), (0.0510, 0.4644)]
EYE_RIGHT = [(-x, z) for x, z in EYE_LEFT]
EYE_GROW = 1.33      # about a third bigger (rule 12), each about its own centre
MOUTH = [(-0.042, 0.4086), (-0.036, 0.4097), (-0.030, 0.4109), (-0.024, 0.4120), (-0.018, 0.4132), (-0.012, 0.4143),
         (-0.006, 0.4155), (0.000, 0.4166), (0.006, 0.4178), (0.012, 0.4191), (0.018, 0.4208), (0.024, 0.4228),
         (0.030, 0.4251), (0.036, 0.4277), (0.042, 0.4306), (0.042, 0.4214), (0.036, 0.4189), (0.030, 0.4168),
         (0.024, 0.4149), (0.018, 0.4133), (0.012, 0.4121), (0.006, 0.4111), (0.000, 0.4104), (-0.006, 0.4097),
         (-0.012, 0.4090), (-0.018, 0.4083), (-0.024, 0.4075), (-0.030, 0.4068), (-0.036, 0.4061), (-0.042, 0.4054)]


# MORE SMUG THAN THE ORIGINAL. Owner, 2026-10-05, on the measured face (v13): "idk about zack,
# needs a more smug look". So for him the measured outlines are the starting point, not the answer,
# and the expression is pushed with the SHAPES alone (rule 12 still holds: flat ink, no lid or brow
# drawn as a line, no nose, no crease, no blush):
#   the eye is bigger and is CUT OFF by a flat top edge, as if a lid sat on it, the edge tilted a
#   little down toward the middle (cocky; a steep tilt would be angry); its bottom is round, and
#   the original's small notch is kept in the middle of it
#   the smirk is a thick stroke, flat on his right, curling UP into a hook at his left end
# Three candidates and the measured face, picked with `--face` or ZACK_FACE:
#   (eye half width, left eye height, right eye height, tilt of the top edge in metres, notch,
#    mouth points, mouth width in mm)
FACES = {
    "A": (0.037, 0.034, 0.034, 0.003, 0.0035,
          [(-0.040, 0.4090), (-0.006, 0.4095), (0.024, 0.4170), (0.044, 0.4320)], 7.6),
    "B": (0.040, 0.039, 0.039, 0.005, 0.0040,
          [(-0.040, 0.4095), (-0.002, 0.4070), (0.028, 0.4140), (0.046, 0.4300), (0.049, 0.4400)], 8.6),
    "C": (0.042, 0.036, 0.042, 0.007, 0.0040,
          [(-0.036, 0.4110), (0.004, 0.4050), (0.036, 0.4120), (0.054, 0.4300), (0.057, 0.4440)], 9.6),
}
FACE = os.environ.get("ZACK_FACE", "measured")   # the owner, 2026-10-05, on A, B and C: "just take v13 on zack. the choices are even worse"
EYE_TOP = 0.4945     # the outer end of the lid edge, 5 mm under the hair's shadow line


def _smug_eye(side, half_w, height, tilt, notch):
    """One eye of a candidate: a flat tilted top edge, a round bottom, a notch in its middle."""
    cx = side * 0.072
    pts = [(cx - side * half_w, EYE_TOP - tilt), (cx + side * half_w, EYE_TOP)]     # inner end, outer end
    n = 16
    for k in range(1, n):
        t = math.pi * k / n
        x = cx + side * half_w * math.cos(t)
        top = EYE_TOP - tilt * 0.5 * (1.0 - math.cos(t))
        drop = height * math.sin(t) ** 0.75
        if k == n // 2:
            drop -= notch
        pts.append((x, top - drop))
    return pts


def _about(points, grow):
    """`points` scaled about the middle of their own bounds."""
    cx = 0.5 * (min(p[0] for p in points) + max(p[0] for p in points))
    cz = 0.5 * (min(p[1] for p in points) + max(p[1] for p in points))
    return [(cx + (x - cx) * grow, cz + (z - cz) * grow) for x, z in points]


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (rule 12). v01 to v08 modelled this face in paint: eye sockets,
    # a heavy lid line over each eye and a crease under it, a nose with a lit bridge and a shadow,
    # two steps of jaw shade, a dimple, a lip shadow. Owner, 2026-10-05, on the whole redesigned
    # cast: "the faces look too realistic and look too human, like it lost its charm, the
    # characters have eyebags etc.. they need to be more cutesy". All of that is gone.
    # WHAT IS LEFT: flat skin with one soft lit patch,
    # the mop's shadow as ONE flat tone, two solid ink eyes, the smirk.
    # THE LOWER FACE IS ONE CLEAN SKIN TONE. No edge shade down the sides and nothing along the
    # jaw: on another hero the owner read exactly that as "random dark spots".
    c.blob(SKIN_LIT, (0.0, 0.440), 0.120, 0.070, 26, 0.3)
    # the mop's shadow on his brow: one flat tone, one straight edge. Below the hairline it stops
    # inside the face (x 0.156): the ears' front faces read this island too and must stay skin.
    c.box(SKIN_SHADE, -0.156, 0.156, 0.500, 0.72, 1.2, 0.8)
    c.band(SKIN_SHADE, HAIRLINE + 0.002, 0.75, 1.2, 0.8)
    # NO BLUSH. v09 and v10 had a round patch under each eye; the owner: "reserve the blush for
    # the female characters".
    # the eyes and the mouth: the original's own outlines (see EYE_LEFT, MOUTH), one flat ink fill
    # each, hard edged, the eyes a third bigger. Nothing over, under or beside them.
    if FACE == "measured":
        c.mark(INK, _about(EYE_RIGHT, EYE_GROW), 0.25, 1.0, curved=False)
        c.mark(INK, _about(EYE_LEFT, EYE_GROW), 0.25, 1.0, curved=False)
        c.mark(INK, MOUTH, 0.25, 1.0, curved=False)
        return
    half_w, h_left, h_right, tilt, notch, mouth, width = FACES[FACE]
    c.mark(INK, _smug_eye(1, half_w, h_left, tilt, notch), 0.25, 1.0, curved=False)
    c.mark(INK, _smug_eye(-1, half_w, h_right, tilt, notch), 0.25, 1.0, curved=False)
    c.stroke(INK, mouth, width, 0.25, 1.0, (0.75, 0.55))


def _head_side(c, sign):
    """One side of the head. `sign` is +1 for his left (xpos), -1 for his right. Axes are (y, z)."""
    c.band(SKIN_SHADE, HAIRLINE + 0.002, 0.75, 1.2, 0.8)
    # the ear, drawn where the ear block sits (y 0.006 to 0.088): one dark hook for its cup
    c.stroke(SKIN_DEEP, [(0.030, 0.482), (0.052, 0.478), (0.060, 0.458), (0.046, 0.442)], 6.0, 0.6, 0.9)


def paint_head_back(c):
    pass


# ---------------------------------------------------------------------------
# THE JACKET. Electric yellow, open, short sleeved. Its trims (hem band, cuffs, lapels, collar,
# shoulder tabs) are geometry in ochre tones; the pocket welts are blocks whose fronts are drawn
# here. What is painted is the cloth: two sun patches, shade under the arms, folds pulled toward
# the hem, the seams and their stitches. He is a condo kid in a clean jacket: no grime, no patches.
# ---------------------------------------------------------------------------

def _pocket(c, x0, x1, z0, z1, button):
    """A welt pocket's front, drawn inside the pocket block's own face and nowhere else."""
    c.box(OCHRE, x0, x1, z0, z1)
    c.box(OCHRE_LIT, x0, x1, z1 - 0.007, z1, 0.5, 0.9)
    c.box(OCHRE_DARK, x0, x1, z0, z0 + 0.005, 0.6, 0.8)
    c.stroke(OCHRE_DARK, [(x0 + 0.002, z1 - 0.011), (0.5 * (x0 + x1), z1 - 0.0125), (x1 - 0.002, z1 - 0.011)], 2.2, 0.4, 1.0, (0.8, 0.8))
    c.blob(OCHRE_DARK, button, 0.0052, 0.0052, 0.3, 1.0)
    c.blob(OCHRE_LIT, (button[0] - 0.0012, button[1] + 0.0012), 0.0022, 0.0022, 0.3, 1.0)


def paint_torso_front(c):
    c.mark(JACKET_LIT, [(-0.134, 0.340), (-0.062, 0.343), (-0.058, 0.300), (-0.092, 0.270), (-0.134, 0.280)], 8, 0.75)
    c.mark(JACKET_LIT, [(0.134, 0.340), (0.064, 0.343), (0.060, 0.296), (0.096, 0.266), (0.134, 0.278)], 8, 0.7)
    c.mark(JACKET_SHADE, [(-0.20, 0.292), (-0.130, 0.286), (-0.114, 0.244), (-0.126, 0.196), (-0.20, 0.196)], 7, 0.7)
    c.mark(JACKET_SHADE, [(0.20, 0.288), (0.132, 0.282), (0.116, 0.242), (0.128, 0.196), (0.20, 0.196)], 7, 0.7)
    # folds pulled to the hem, one a side, not a pair
    c.stroke(JACKET_SHADE, [(-0.108, 0.268), (-0.098, 0.256), (-0.100, 0.246)], 5.0, 0.8, 0.85, (0.1, 0.6))
    c.stroke(JACKET_SHADE, [(0.132, 0.262), (0.128, 0.232), (0.132, 0.202)], 4.5, 0.8, 0.8, (0.2, 0.6))
    # the front edges: a row of ochre stitches down each, and the shoulder seams
    c.stitch(OCHRE, [(-0.057, 0.198), (-0.0565, 0.240), (-0.057, 0.282)], 1.7, 5.0, 4.0)
    c.stitch(OCHRE, [(0.057, 0.200), (0.0575, 0.242), (0.057, 0.280)], 1.7, 5.0, 4.0)
    c.stitch(OCHRE, [(-0.130, 0.300), (-0.114, 0.322), (-0.094, 0.338)], 1.7, 6.0, 4.0)
    c.stitch(OCHRE, [(0.130, 0.298), (0.116, 0.320), (0.096, 0.338)], 1.7, 6.0, 4.0)
    # the two welt pockets (their blocks stand 9 mm off the jacket; see POCKET in the model script)
    _pocket(c, -0.126, -0.062, 0.204, 0.242, (-0.094, 0.2215))
    _pocket(c, 0.062, 0.126, 0.204, 0.242, (0.0945, 0.2205))


def paint_torso_back(c):
    c.mark(JACKET_LIT, [(-0.134, 0.338), (0.134, 0.338), (0.120, 0.306), (0.050, 0.314), (-0.046, 0.310), (-0.122, 0.304)], 9, 0.7)
    c.mark(JACKET_SHADE, [(-0.20, 0.270), (-0.128, 0.264), (-0.110, 0.226), (-0.20, 0.196)], 7, 0.7)
    c.mark(JACKET_SHADE, [(0.20, 0.266), (0.130, 0.262), (0.114, 0.224), (0.20, 0.196)], 7, 0.7)
    # the yoke: a seam across the shoulders with a row of stitches under it, then the centre pleat
    c.stroke(JACKET_SHADE, [(-0.136, 0.300), (-0.060, 0.292), (0.0, 0.288), (0.062, 0.292), (0.136, 0.300)], 3.0, 0.5, 1.0, (0.8, 0.8))
    c.stitch(OCHRE, [(-0.132, 0.2925), (-0.060, 0.2845), (0.0, 0.2805), (0.062, 0.2845), (0.132, 0.2925)], 1.7, 6.0, 4.0)
    c.stroke(JACKET_SHADE, [(0.001, 0.286), (-0.001, 0.244), (0.001, 0.198)], 3.6, 0.5, 0.9, (0.7, 0.9))
    c.stroke(JACKET_LIT, [(0.008, 0.280), (0.007, 0.244), (0.009, 0.204)], 3.0, 0.8, 0.7, (0.5, 0.7))
    c.stroke(JACKET_SHADE, [(-0.078, 0.270), (-0.090, 0.236), (-0.086, 0.202)], 6.0, 0.9, 0.8, (0.2, 0.7))
    c.stroke(JACKET_SHADE, [(0.094, 0.262), (0.100, 0.232), (0.092, 0.204)], 5.0, 0.9, 0.75, (0.2, 0.7))
    # under the collar and the hair's overhang
    c.band(JACKET_SHADE, 0.328, 0.38, 3, 0.7)


def _torso_side(c):
    c.mark(JACKET_SHADE, [(-0.060, 0.300), (0.060, 0.300), (0.052, 0.200), (-0.050, 0.200)], 10, 0.75)
    c.stroke(OCHRE, [(0.004, 0.290), (0.002, 0.246), (0.004, 0.198)], 2.0, 0.5, 0.8, (0.6, 0.8))


def paint_torso_top(c):
    c.blob(JACKET_LIT, (0.0, 0.0), 0.17, 0.085, 12, 0.6)


def paint_shirt_front(c):
    """The black shirt in the jacket's opening, and the bare V at his throat."""
    c.mark(CLOTH_LIT, [(-0.030, 0.290), (0.030, 0.292), (0.036, 0.224), (-0.034, 0.218)], 8, 0.8)
    c.stroke(CLOTH_DEEP, [(-0.030, 0.262), (-0.020, 0.236), (-0.024, 0.206)], 4.5, 0.8, 0.9, (0.2, 0.6))
    c.stroke(CLOTH_DEEP, [(0.034, 0.250), (0.026, 0.228), (0.030, 0.206)], 4.0, 0.8, 0.9, (0.2, 0.6))
    c.mark(SKIN_SHADE, [(-0.043, 0.37), (0.043, 0.37), (0.037, 0.318), (0.0, 0.2915), (-0.037, 0.318)], 0.4, 1.0, curved=False)
    c.mark(SKIN, [(-0.018, 0.328), (0.018, 0.328), (0.0, 0.302)], 3.0, 0.7)
    c.stroke(SKIN_DEEP, [(-0.030, 0.332), (-0.010, 0.324), (0.0, 0.3275)], 3.0, 0.5, 0.9)
    c.stroke(SKIN_DEEP, [(0.031, 0.3315), (0.012, 0.324), (0.0, 0.3275)], 3.0, 0.5, 0.9)
    # the neckline's rib, one line
    c.stroke(CLOTH_HI, [(-0.043, 0.346), (-0.037, 0.318), (0.0, 0.2915), (0.037, 0.318), (0.043, 0.346)], 3.6, 0.4, 1.0, (0.8, 0.8), curved=False)


# ---------------------------------------------------------------------------
# THE ARMS. A short yellow sleeve to just past the elbow, then bare forearm and a block hand.
# The cuff, the shoulder tab and the neon band are blocks in flat tones and are NOT drawn here,
# so nothing of theirs can land on the hand (rule 4). Mitten hands: three drawn finger lines.
# ---------------------------------------------------------------------------
SLEEVE_END = 0.170     # the sleeve block ends at 0.188 under the cuff, which covers 0.181 to 0.198
HAND_FROM = 0.226


def _hand(c, s, view):
    if view in ("front", "back"):
        c.box(SKIN_SHADE, *sorted((s * 0.196, s * 0.31)), 0.16, 0.262, 0, 0.7)
        c.stroke(SKIN_DEEP, [(s * 0.258, 0.3075), (s * 0.283, 0.3090)], 2.6, 0.4, 0.95, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.258, 0.2880), (s * 0.284, 0.2875)], 2.6, 0.4, 0.95, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.258, 0.2680), (s * 0.282, 0.2655)], 2.6, 0.4, 0.95, (1.0, 0.3), curved=False)
        c.stroke(SKIN_SHADE, [(s * 0.2455, 0.340), (s * 0.2465, 0.240)], 3.0, 1.0, 0.7, (0.6, 0.6), curved=False)
        # the thumb, folded along the top of the fist: one drawn hook
        c.stroke(SKIN_DEEP, [(s * 0.234, 0.3260), (s * 0.259, 0.3280), (s * 0.267, 0.3370)], 2.6, 0.4, 0.95, (0.8, 0.4))
    c.column(SKIN_SHADE, *sorted((s * 0.279, s * 0.31)), 2.5, 0.6)


def paint_arm(c, s, view):
    """`s` is +1 for his left arm, -1 for his right. Each arm's folds are its own."""
    if view in ("xpos", "xneg"):
        # the end-on view of a hand: only the fingertips show
        c.blob(SKIN_DEEP, (0.009, 0.288), 0.05, 0.05, 6, 0.5)
        return
    lo, hi = sorted((s * 0.08, s * SLEEVE_END))
    # bare forearm: lit on top, in shade under
    if view == "bottom":
        c.column(SKIN_SHADE, *sorted((s * SLEEVE_END, s * 0.31)), 0, 0.75)
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.200, -0.030), (s * 0.250, -0.028), (s * 0.250, 0.044), (s * 0.200, 0.046)], 7, 0.5)
    # the forearm inside the cuff's mouth is in the sleeve's shadow (it shows when the elbow bends)
    c.column(SKIN_DEEP, *sorted((s * 0.170, s * 0.199)))
    # the sleeve
    c.column(JACKET, lo, hi)
    if view in ("front", "back"):
        c.box(JACKET_SHADE, lo, hi, 0.16, 0.262, 0, 0.75)
        c.mark(JACKET_LIT, [(s * 0.112, 0.352), (s * 0.172, 0.350), (s * 0.170, 0.322), (s * 0.114, 0.320)], 6, 0.65)
        if s > 0:
            c.stroke(JACKET_SHADE, [(0.148, 0.346), (0.140, 0.304), (0.150, 0.250)], 4.5, 0.6, 0.9)
            c.stroke(JACKET_SHADE, [(0.122, 0.332), (0.117, 0.296), (0.124, 0.256)], 3.4, 0.6, 0.8)
        else:
            c.stroke(JACKET_SHADE, [(-0.158, 0.342), (-0.150, 0.300), (-0.158, 0.246)], 4.2, 0.6, 0.9)
            c.stroke(JACKET_SHADE, [(-0.128, 0.336), (-0.122, 0.292), (-0.130, 0.260)], 3.6, 0.6, 0.8)
        # the armhole seam
        c.stitch(OCHRE, [(s * 0.108, 0.350), (s * 0.106, 0.290), (s * 0.108, 0.226)], 1.6, 5.0, 4.0)
    elif view == "top":
        c.mark(JACKET_LIT, [(s * 0.112, -0.040), (s * 0.176, -0.038), (s * 0.176, 0.060), (s * 0.112, 0.064)], 7, 0.6)
    else:
        c.column(JACKET_SHADE, lo, hi, 0, 0.75)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Black cargo trousers: worn paler on the thigh and the shin, one crease, seams, the
# pocket's face. Yellow skate shoes: the upper drawn here, the white sole, toe cap, heel counter
# and laces are blocks. The strap, knee panel, pocket flap and buckle are blocks too.
# ---------------------------------------------------------------------------
SHOE_TOP = 0.064
POCKET = (-0.045, 0.045, 0.083, 0.129)     # y0, y1, z0, z1 on the outer side of each thigh


def paint_leg(c, s, view):
    """`s` is +1 for his left leg, -1 for his right. Each leg's marks are its own."""
    x = lambda v: s * v
    outer = (view == "xpos") == (s > 0)
    if view == "front":
        c.mark(CLOTH_LIT, [(x(0.040), 0.166), (x(0.126), 0.168), (x(0.130), 0.132), (x(0.044), 0.128)], 6, 0.8)
        c.mark(CLOTH_LIT, [(x(0.046), 0.092), (x(0.124), 0.094), (x(0.126), 0.072), (x(0.050), 0.070)], 5, 0.7)
        if s > 0:
            c.stroke(CLOTH_DEEP, [(0.038, 0.170), (0.050, 0.146), (0.042, 0.126)], 3.6, 0.7, 0.9)
        else:
            c.stroke(CLOTH_DEEP, [(-0.126, 0.168), (-0.112, 0.150), (-0.120, 0.128)], 3.4, 0.7, 0.9)
    elif view == "back":
        c.mark(CLOTH_LIT, [(x(0.044), 0.160), (x(0.124), 0.162), (x(0.120), 0.120), (x(0.050), 0.118)], 7, 0.6)
        c.stroke(CLOTH_DEEP, [(x(0.060), 0.166), (x(0.088), 0.132), (x(0.074), 0.090)], 4.6, 0.7, 0.9)
        # a seat pocket, one line of stitches each
        c.stitch(CLOTH_HI, [(x(0.052), 0.168), (x(0.054), 0.146), (x(0.112), 0.148), (x(0.114), 0.168)], 1.6, 5.0, 4.0)
    else:
        c.mark(CLOTH_LIT, [(-0.050, 0.170), (0.050, 0.172), (0.048, 0.138), (-0.048, 0.136)], 6, 0.6)
        if outer:
            # the cargo pocket's face (its block stands off the thigh): a paler panel, a centre
            # pleat, stitches round it. Kept inside POCKET so none of it shows on the trouser.
            y0, y1, z0, z1 = POCKET
            c.box(CLOTH_LIT, y0, y1, z0, z1)
            c.stroke(CLOTH_DEEP, [(0.001, z1 - 0.004), (0.0, 0.104), (0.001, z0 + 0.003)], 3.0, 0.4, 1.0, (0.9, 0.9), curved=False)
            c.stitch(CLOTH_HI, [(y0 + 0.006, z1 - 0.008), (y0 + 0.006, z0 + 0.006), (y1 - 0.006, z0 + 0.006), (y1 - 0.006, z1 - 0.008)], 1.6, 5.0, 4.0)
        else:
            c.stroke(CLOTH_HI, [(-0.004, 0.172), (0.002, 0.124), (-0.004, 0.072)], 2.2, 0.5, 0.7, (0.8, 0.8))
    # dust low on the trouser, where the cuff sits
    c.band(DUST, SHOE_TOP, 0.084, 3.0, 0.35)
    # the shoe's upper
    c.band(SHOE, 0.0, SHOE_TOP)
    c.band(SHOE_SHADE, 0.049, SHOE_TOP, 0.6, 0.85)            # the padded collar round the ankle
    c.band(SHOE_SHADE, 0.0, 0.0225, 0.5, 0.9)                 # the shadow line over the sole
    if view == "front":
        c.mark(SHOE_LIT, [(x(0.060), 0.060), (x(0.104), 0.060), (x(0.100), 0.036), (x(0.064), 0.036)], 2.0, 0.8)
    elif view == "back":
        c.stroke(SHOE_SHADE, [(x(0.082), 0.060), (x(0.082), 0.044)], 3.0, 0.4, 1.0, (1, 1), curved=False)
    else:
        # the side panel: one curved seam from the toe cap to the collar, eyelets ahead of it
        c.stroke(SHOE_SHADE, [(-0.098, 0.0235), (-0.060, 0.030), (-0.030, 0.046)], 2.4, 0.4, 1.0, (0.7, 0.7))
        c.stroke(SHOE_LIT, [(-0.020, 0.040), (0.030, 0.042), (0.052, 0.036)], 3.4, 0.8, 0.7, (0.5, 0.5))
        c.blob(SHOE_SHADE, (-0.066, 0.043), 0.0032, 0.0032, 0.3, 1.0)
        c.blob(SHOE_SHADE, (-0.054, 0.047), 0.0032, 0.0032, 0.3, 1.0)
        if outer and s < 0:
            c.mark(SCUFF, [(0.016, 0.034), (0.046, 0.036), (0.042, 0.026), (0.020, 0.025)], 2.0, 0.4)


def paint_leg_top(c, s):
    """Looking down on a foot: the upper, the tongue between the laces. The toe is at -y."""
    x = lambda v: s * v
    c.box(SHOE, *sorted((x(0.0), x(0.18))), -0.17, 0.12)
    c.mark(SHOE_LIT, [(x(0.060), -0.058), (x(0.104), -0.058), (x(0.102), -0.106), (x(0.062), -0.106)], 1.5, 0.9)
    c.stroke(SHOE_SHADE, [(x(0.036), -0.100), (x(0.082), -0.108), (x(0.128), -0.100)], 2.4, 0.4, 1.0, (0.6, 0.6))


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    side = SKIN     # the same tone as the front, so the jaw corner has no painted step
    c = I("head.xpos", side); _head_side(c, 1); done[c.name] = c
    c = I("head.xneg", side); _head_side(c, -1); done[c.name] = c
    c = I("head.back", SKIN); paint_head_back(c); done[c.name] = c
    # Seen from above the head block is under the mop; what shows is the tops of the ears and the
    # nose, which are SKIN. Hair paint here would blacken them (Dante's fault d).
    c = I("head.top", mix(SKIN, SKIN_LIT, 0.5)); done[c.name] = c
    c = I("head.bottom", SKIN); done[c.name] = c

    c = I("torso.front", JACKET); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", JACKET); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", JACKET); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", JACKET); _torso_side(c); done[c.name] = c
    c = I("torso.top", JACKET); paint_torso_top(c); done[c.name] = c
    c = I("shirt.front", CLOTH); paint_shirt_front(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN); paint_arm(c, s, view); done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, CLOTH)
            if view == "top":
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c

    for name, hex_str in TONES.items():
        c = I(name, hex_str, SWATCHES[name][2:4]); done[c.name] = c

    missing = set(layout(size)) - set(done)
    if missing:
        raise SystemExit("islands not painted: %s" % sorted(missing))
    return done


def main():
    from PIL import Image

    global FACE
    if "--face" in sys.argv:
        FACE = sys.argv[sys.argv.index("--face") + 1]
    size = ATLAS
    if "--size" in sys.argv:
        size = int(sys.argv[sys.argv.index("--size") + 1])
    for hex_str in CLOTH_HEXES:
        if _near_role_hue(hex_str):
            raise SystemExit("#%s sits near role hue #%s" % (hex_str, _near_role_hue(hex_str)))

    islands = build(size)
    # The bottom half is where Toon.shader reads the palette instead of this file. Left one flat tone.
    atlas = Image.new("RGB", (size, size), _rgb(CLOTH))
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
