"""Paint the atlas of the Bayan (display name BERTO) redesign, by hand, in the HEROES' style.

    py -3 tools/author_character_redesign_bayan_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/bayan/bayan-redesign-atlas.png. The model is
built by tools/author_character_redesign_bayan.py, which imports this file for the LAYOUT only
(PIL is imported inside the painting calls, Blender's Python has none) and so maps every face
onto the island painted for it here. Paint first, then build.

THE BRIEF CHANGED AT v05. v01 to v04 were "the original, with more detail": his eyes and mouth
measured off the stock rig, his hair in the palette's own grey, every piece one the stock rig
has. Owner, 2026-10-06, on those: "my issue is they focus on resembling closer to the original
design instead of being reworked to be more in the heroes' style. the eyes and face design are
different from the heros, even the hair on bayan being black doesnt translate to the rework".
So from v05 this is BAYAN REDRAWN AS IF HE WERE ONE OF THE HEROES. Same man, his colours, his
recognisable pieces (black flat-top with a jagged fringe, a scowl, a green shirt, braces, a
belt with a big buckle, a pouch at each hip, rolled cuffs), in the heroes' visual language. For
the Classic cast "nothing invented" and "measure the original's eyes" are LIFTED.

WHAT IS TAKEN FROM THE HEROES, with the numbers read off Dante's and Sean's scripts:
  * INK 1a1420 (Dante's) for eyes and mouth, not the palette's grey 38383d;
  * EYES: solid ink blocks at the heroes' size and height (Dante's are 92 mm wide between z 0.446
    and 0.512; Amihan's are upright blocks 46 by 58). His are upright blocks 81 mm wide with the
    corners cut, between z 0.449 and 0.517, whose TOP EDGE is cut at a slant down toward the
    nose: the scowl, the way Dante's and Sean's attitude is cut into theirs;
  * MOUTH: one thin curved stroke, 7.6 mm (Dante's is 7.4): a short frown;
  * HAIR: Dante's black, 1a181e with 2b2933 on faces turned up and 0e0d12 underneath;
  * SKIN AND CLOTH each have a base, a shade and a light tone, and a deep one for lines.
No nose, no sockets, creases or lids, no blush (he is male).

HIS COLOURS. His skin is still his palette's slot 13 (d98a5f) and his shirt its slot 1 (3f8f5c).
What changed, each decided on purpose:
  * his belt and braces were slot 13 too, the same colour as his skin, and vanished into it.
    They are dark leather now (6e4424), with brass fittings in the buckle's slot 9 family;
  * the peach slot 15 blocks on his forearms were ambiguous. His forearms are BARE, in his own
    skin, under a short sleeve with a rolled edge;
  * his trousers were peach, a step off his skin. They are slate work trousers now (4f5260, his
    palette's slot 10, which the stock rig never wore), with a paler rolled cuff (slot 11);
  * proper shoes: brown work boots with a darker toe cap, cream laces and a dark sole;
  * the pouches and soles keep slot 8's dark grey (38383d), the one place it is still worn;
  * a cream undershirt at his open neck and a cream towel with red stripes hung from the back
    of his belt (slots 0 and 4): the towel a working man of the neighbourhood carries.

ROLE HUES (offence orange #f87020, defence blue #0080e8): the large cloth colours are checked
in `main`. Leather and boot browns are kept under 0.45 in value so they pass. His skin is
exempt, as the cast's is. Brass is small (buckle, clips, adjusters).
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "bayan"
ATLAS_NAME = "bayan-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see `main`)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. Each family is a base, a light, a shade and a deep tone, as the heroes' are.
# ---------------------------------------------------------------------------
SKIN = "d98a5f"; SKIN_LIT = "e8a274"; SKIN_SHADE = "b96f47"; SKIN_DEEP = "965530"       # his slot 13
HAIR = "1a181e"; HAIR_TOP = "2b2933"; HAIR_DEEP = "0e0d12"                              # Dante's black
INK = "1a1420"                                                                          # Dante's ink
SHIRT = "3f8f5c"; SHIRT_LIT = "57ab75"; SHIRT_DARK = "2c6f45"; SHIRT_DEEP = "1f5233"    # his slot 1
LEATHER = "6e4424"; LEATHER_LIT = "8a5a30"; LEATHER_DARK = "4f2f18"; LEATHER_DEEP = "38200f"
BRASS = "e0a838"; BRASS_LIT = "f4cc6a"; BRASS_DARK = "a87420"                           # the buckle's slot 9, polished
SLATE = "4f5260"; SLATE_LIT = "646879"; SLATE_DARK = "3b3e4a"; SLATE_DEEP = "2b2d36"    # his slot 10
ROLL = "8f97b8"; ROLL_LIT = "a9b1d0"; ROLL_DARK = "6f7696"                              # his slot 11, the cuff's inside
BOOT = "70482a"; BOOT_LIT = "8c6038"; BOOT_DARK = "523219"; BOOT_DEEP = "3a2210"
DARK = "38383d"; DARK_LIT = "4c4c53"; DARK_DEEP = "232327"                              # his slot 8: pouches and soles
CREAM = "fde4c7"; CREAM_SHADE = "dcc0a0"; RED = "cf534f"                                # his slots 0 and 4

# the large cloth colours, checked against the role hues in `main`
CLOTH_HEXES = [HAIR, HAIR_TOP, HAIR_DEEP, SHIRT, SHIRT_LIT, SHIRT_DARK, SHIRT_DEEP, SLATE, SLATE_LIT, SLATE_DARK, SLATE_DEEP,
               ROLL, ROLL_LIT, ROLL_DARK, LEATHER, LEATHER_DARK, BOOT, BOOT_DARK, DARK, DARK_LIT, CREAM, CREAM_SHADE]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# Blender space: x his left, -y the way he faces, z up.
# ---------------------------------------------------------------------------
BELT = (0.178, 0.225)           # the belt, at the foot of the shirt
STRAP = (0.052, 0.099)          # each brace, in |x|
STRAP_TOP = 0.352
ARM_Z = 0.2878                  # the arm bone's height
SLEEVE_END = 0.176              # along the arm (x): the short sleeve ends here, under its rolled edge
WRIST = 0.282                   # where the fist block starts
HAND_END = 0.376
BOOT_TOP = 0.064                # the trouser cuff's foot: below this a leg island is boot
TROUSER_Y = 0.0285              # the trouser leg's middle, front to back
#   the fringe's five spikes: (middle x, tip z, half width, half width of the tip). The model
#   script builds them from this list. No two are the same: v05 and v06 had five equal points
#   and they read as a row of teeth.
SPIKES = [(-0.140, 0.566, 0.040, 0.012), (-0.070, 0.540, 0.033, 0.015), (-0.004, 0.575, 0.036, 0.011),
          (0.064, 0.549, 0.035, 0.015), (0.136, 0.563, 0.040, 0.012)]
SLAB_FOOT = 0.626               # the flat-top's slab comes down over the forehead to here
SPIKE_SHOULDER = 0.618          # a spike keeps its full width down to here, so the fringe is one mass above it

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y). +x is HIS left, -y is the way he faces.
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    # only the face is drawn; every other side of the head block takes flat skin
    "head": {
        "front": ((-0.20, 0.20, 0.33, 0.68), 1500),
    },
    "torso": {
        "front": ((-0.17, 0.17, 0.15, 0.37), 1300),
        "back": ((-0.17, 0.17, 0.15, 0.37), 1000),
        "xpos": ((-0.13, 0.19, 0.15, 0.37), 900),
        "xneg": ((-0.13, 0.19, 0.15, 0.37), 900),
    },
    "armL": {
        "front": ((0.09, 0.39, 0.19, 0.385), 1100),
        "back": ((0.09, 0.39, 0.19, 0.385), 900),
        "top": ((0.09, 0.39, -0.08, 0.115), 900),
        "bottom": ((0.09, 0.39, -0.08, 0.115), 700),
    },
    "armR": {
        "front": ((-0.39, -0.09, 0.19, 0.385), 1100),
        "back": ((-0.39, -0.09, 0.19, 0.385), 900),
        "top": ((-0.39, -0.09, -0.08, 0.115), 900),
        "bottom": ((-0.39, -0.09, -0.08, 0.115), 700),
    },
    "legL": {
        "front": ((0.0, 0.17, 0.0, 0.19), 1100),
        "back": ((0.0, 0.17, 0.0, 0.19), 950),
        "xpos": ((-0.13, 0.15, 0.0, 0.19), 950),
        "xneg": ((-0.13, 0.15, 0.0, 0.19), 800),
        "top": ((0.0, 0.17, -0.13, 0.15), 800),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.19), 1100),
        "back": ((-0.17, 0.0, 0.0, 0.19), 950),
        "xpos": ((-0.13, 0.15, 0.0, 0.19), 800),
        "xneg": ((-0.13, 0.15, 0.0, 0.19), 950),
        "top": ((-0.17, 0.0, -0.13, 0.15), 800),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group.
VIEW_BIAS = {}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). u across, v UP.
SWATCHES = {
    "buckle": (220, 190, 0.066, 0.055),     # the belt buckle's face
    "pouch": (190, 200, 0.088, 0.090),      # the outer face of a hip pouch
    "towel": (200, 300, 0.072, 0.112),      # the towel hung from the back of his belt
}

# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    "hair_top": HAIR_TOP, "hair": HAIR, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "shirt_lit": SHIRT_LIT, "shirt": SHIRT, "shirt_dark": SHIRT_DARK, "shirt_deep": SHIRT_DEEP,
    "leather_lit": LEATHER_LIT, "leather": LEATHER, "leather_dark": LEATHER_DARK, "leather_deep": LEATHER_DEEP,
    "brass_lit": BRASS_LIT, "brass": BRASS, "brass_dark": BRASS_DARK,
    "slate_lit": SLATE_LIT, "slate": SLATE, "slate_dark": SLATE_DARK, "slate_deep": SLATE_DEEP,
    "roll_lit": ROLL_LIT, "roll": ROLL, "roll_dark": ROLL_DARK,
    "boot_lit": BOOT_LIT, "boot": BOOT, "boot_dark": BOOT_DARK, "boot_deep": BOOT_DEEP,
    "dark_lit": DARK_LIT, "dark": DARK, "dark_deep": DARK_DEEP,
    "cream": CREAM, "cream_shade": CREAM_SHADE, "red": RED,
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




# A STRAIGHT ROW OF STITCHES. `Island.stitch` counts its dashes segment by segment, so a line
# given as two points comes out as one solid stroke. A two-point line is cut into short runs first.
_stitch_curve = Island.stitch


def _stitch_any(self, colour, points, *args, **kwargs):
    if len(points) == 2:
        (ax, ay), (bx, by) = points
        points = [(ax + (bx - ax) * i / 24.0, ay + (by - ay) * i / 24.0) for i in range(25)]
    return _stitch_curve(self, colour, points, *args, **kwargs)


Island.stitch = _stitch_any


# ---------------------------------------------------------------------------
# THE HEAD. His face on the flat front of the block, drawn the way the heroes' are.
# ---------------------------------------------------------------------------
# HIS LEFT EYE (the right is its mirror): an upright ink block with its corners cut, 81 mm wide,
# whose top edge falls 30 mm from the outer corner to the inner one. That slant is the scowl.
# It sits where the heroes' eyes sit (Dante's: |x| 0.042 to 0.134, z 0.446 to 0.512).
EYE_LEFT = [(0.042, 0.456), (0.049, 0.449), (0.116, 0.449), (0.123, 0.456), (0.123, 0.510), (0.116, 0.517), (0.042, 0.487)]
# HIS MOUTH: one thin stroke, a short frown, at the heroes' width (Dante's is 7.4 mm)
MOUTH = [(-0.034, 0.3940), (-0.014, 0.4045), (0.014, 0.4050), (0.034, 0.3950)]
MOUTH_WIDTH = 7.6


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (rule 12): flat skin with one very soft lit patch, the fringe's
    # shadow as ONE flat tone, two solid ink eyes, the mouth as one stroke. No nose, no sockets,
    # creases, lids, lip shadow or contour, nothing dark below the eyes, no blush.
    c.blob(SKIN_LIT, (0.0, 0.455), 0.115, 0.075, 26, 0.32)
    # the fringe's cast shadow: a band under the flat-top's slab and a point under each spike
    foot = SPIKE_SHOULDER - 0.008
    poly = [(-0.26, 0.72), (0.26, 0.72), (0.26, foot)]
    for x, tip, half, tip_half in reversed(SPIKES):
        poly += [(x + half, foot), (x + tip_half, tip - 0.008), (x - tip_half, tip - 0.008), (x - half, foot)]
    poly.append((-0.26, foot))
    c.mark(SKIN_SHADE, poly, 1.4, 0.70, curved=False)
    # the eyes: solid ink, the heroes' way
    c.mark(INK, EYE_LEFT, 0.3, 1.0, curved=False)
    c.mark(INK, [(-x, z) for x, z in EYE_LEFT], 0.3, 1.0, curved=False)
    # the mouth: a small frown, one stroke and nothing under it
    c.stroke(INK, MOUTH, MOUTH_WIDTH, 0.3, 1.0, (0.5, 0.6))


# ---------------------------------------------------------------------------
# THE SHIRT. A green work shirt, short sleeved, open at the neck over a cream undershirt, tucked
# into the belt. The collar wings, the braces with their brass, the belt, its loops, the buckle,
# the pouches and the towel are blocks; the belt and the braces take their paint from these
# islands at the place they sit, so each band below is cut to its own block.
# ---------------------------------------------------------------------------

def _belt(c, a0, a1):
    c.band(LEATHER, BELT[0] - 0.004, BELT[1])
    c.band(LEATHER_LIT, BELT[1] - 0.0090, BELT[1] - 0.0060, 0.3, 0.9)
    c.band(LEATHER_DARK, BELT[0] - 0.004, BELT[0] + 0.0055, 0.4, 0.95)
    c.stitch(LEATHER_DEEP, [(a0, BELT[1] - 0.0140), (a1, BELT[1] - 0.0140)], 1.5, 5.0, 4.0)
    c.stitch(LEATHER_DEEP, [(a0, BELT[0] + 0.0120), (a1, BELT[0] + 0.0120)], 1.5, 5.0, 4.0)


def _straps(c):
    for s in (1, -1):
        a, b = sorted((s * STRAP[0], s * STRAP[1]))
        c.mark(LEATHER, [(a, BELT[1]), (b, BELT[1]), (b, 0.40), (a, 0.40)], 0.0, 1.0, curved=False)
        # a paler face down the middle of the strap, a dark edge each side with a row of stitches
        c.mark(LEATHER_LIT, [(a + 0.0120, BELT[1]), (b - 0.0120, BELT[1]), (b - 0.0120, 0.40), (a + 0.0120, 0.40)], 0.6, 0.55, curved=False)
        c.stitch(LEATHER_DEEP, [(a + 0.0065, BELT[1] + 0.004), (a + 0.0065, STRAP_TOP)], 1.5, 5.0, 4.0)
        c.stitch(LEATHER_DEEP, [(b - 0.0065, BELT[1] + 0.004), (b - 0.0065, STRAP_TOP)], 1.5, 5.0, 4.0)


def _shirt_shade(c, a0, a1):
    """The shirt's form: a shade along its foot, above the belt, as the heroes' coats carry."""
    c.mark(SHIRT_DARK, [(a0, BELT[1]), (a1, BELT[1]), (a1, BELT[1] + 0.022), (a0, BELT[1] + 0.022)], 5, 0.75, curved=False)


def paint_torso_front(c):
    c.mark(SHIRT_LIT, [(-0.044, 0.326), (0.044, 0.326), (0.042, 0.262), (-0.042, 0.262)], 9, 0.55)
    _shirt_shade(c, -0.2, 0.2)
    # the undershirt at his open neck: a cream V, the shirt's turned edge round it
    c.mark(SHIRT_DEEP, [(-0.030, 0.350), (0.030, 0.350), (0.0, 0.2965)], 0.3, 1.0, curved=False)
    c.mark(CREAM, [(-0.025, 0.350), (0.025, 0.350), (0.0, 0.3040)], 0.3, 1.0, curved=False)
    c.mark(CREAM_SHADE, [(-0.025, 0.350), (0.025, 0.350), (0.017, 0.335), (-0.017, 0.335)], 1.0, 0.8, curved=False)
    # the placket: a darker strip from the V to the belt, a pale edge, three buttons
    c.mark(SHIRT_DARK, [(-0.0135, 0.298), (0.0135, 0.298), (0.0135, BELT[1]), (-0.0135, BELT[1])], 0.3, 0.85, curved=False)
    c.stroke(SHIRT_DEEP, [(0.0135, 0.298), (0.0135, BELT[1])], 1.6, 0.3, 1.0, (1.0, 1.0), curved=False)
    for z in (0.284, 0.262, 0.240):
        c.blob(SHIRT_DEEP, (0.0, z), 0.0062, 0.0062, 0.25, 1.0)
        c.blob(CREAM_SHADE, (0.0, z), 0.0042, 0.0042, 0.25, 1.0)
    # one fold on each side, outside the braces
    c.stroke(SHIRT_DARK, [(-0.110, 0.296), (-0.115, 0.266), (-0.112, 0.240)], 3.4, 0.6, 0.95)
    c.stroke(SHIRT_DARK, [(0.111, 0.292), (0.115, 0.262), (0.111, 0.240)], 3.4, 0.6, 0.95)
    _straps(c)
    _belt(c, -0.150, 0.150)


def paint_torso_back(c):
    c.mark(SHIRT_LIT, [(-0.042, 0.290), (0.042, 0.290), (0.040, 0.250), (-0.040, 0.250)], 9, 0.45)
    _shirt_shade(c, -0.2, 0.2)
    # the yoke: one seam across the shoulders with its row of stitches, and the pleat under it
    c.stroke(SHIRT_DEEP, [(-0.125, 0.322), (0.125, 0.322)], 1.8, 0.3, 1.0, (1.0, 1.0), curved=False)
    c.stitch(SHIRT_DARK, [(-0.120, 0.3175), (0.120, 0.3175)], 1.4, 4.5, 3.5)
    c.stroke(SHIRT_DARK, [(0.0, 0.288), (0.0005, 0.232)], 2.6, 0.4, 0.95, (0.9, 0.5), curved=False)
    _straps(c)
    _belt(c, -0.150, 0.150)


def _torso_side(c):
    _shirt_shade(c, -0.2, 0.2)
    # the side seam, from the armhole to the belt
    c.stroke(SHIRT_DEEP, [(0.029, 0.300), (0.030, 0.232)], 2.0, 0.4, 0.95, (0.7, 0.9), curved=False)
    c.mark(SHIRT_LIT, [(-0.045, 0.268), (0.000, 0.268), (0.002, 0.244), (-0.047, 0.244)], 7, 0.45)
    _belt(c, -0.110, 0.170)


# ---------------------------------------------------------------------------
# THE ARMS. A short green sleeve to 0.176 with a rolled edge (a block), then his BARE arm in his
# own skin: upper arm, forearm, fist. A leather wristband with a brass face on his left wrist
# (a block). Along the arm (x): elbow 0.230, fist 0.282 to 0.376.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    # the fist's flat faces only: x 0.296 to 0.362 along the arm, z 0.246 to 0.329
    if view in ("front", "back"):
        # three finger lines across the end of the fist, and the thumb folded over them: one hook
        for z in (ARM_Z + 0.0215, ARM_Z, ARM_Z - 0.0215):
            c.stroke(SKIN_DEEP, [(s * 0.3330, z), (s * 0.3590, z)], 2.6, 0.3, 0.95, (1.0, 0.5), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.3010, ARM_Z + 0.0270), (s * 0.3170, ARM_Z + 0.0290), (s * 0.3270, ARM_Z + 0.0375)], 2.6, 0.3, 0.95, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.304, -0.016), (s * 0.354, -0.016), (s * 0.354, 0.050), (s * 0.304, 0.050)], 5, 0.6)


def paint_arm(c, s, view):
    """`s` is +1 for his left arm. Each mark is cut to its own piece along the arm (x)."""
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    # the sleeve
    col(SHIRT, 0.08, SLEEVE_END + 0.002)
    if view in ("front", "back"):
        c.mark(SHIRT_LIT, [(x(0.112), 0.334), (x(0.158), 0.336), (x(0.158), 0.308), (x(0.112), 0.306)], 5, 0.55)
        c.stroke(SHIRT_DARK, [(x(0.128), 0.330), (x(0.138), 0.296), (x(0.134), 0.250)], 3.2, 0.6, 0.95)
        c.mark(SHIRT_DARK, [(x(0.08), 0.18), (x(0.178), 0.18), (x(0.178), 0.250), (x(0.08), 0.254)], 2.0, 0.7, curved=False)
    elif view == "top":
        c.mark(SHIRT_LIT, [(x(0.110), -0.024), (x(0.160), -0.026), (x(0.160), 0.058), (x(0.110), 0.056)], 6, 0.55)
    elif view == "bottom":
        col(SHIRT_DARK, 0.08, SLEEVE_END + 0.002, 0, 0.8)
    # his bare arm: skin from the sleeve out, drawn last so no cloth paint is left on it
    col(SKIN, SLEEVE_END + 0.002, 0.40)
    if view in ("front", "back"):
        # the arm's form: lit along its upper half, a shade under it, one crease at the elbow
        c.mark(SKIN_LIT, [(x(0.192), 0.330), (x(0.276), 0.326), (x(0.276), 0.300), (x(0.192), 0.302)], 6, 0.5)
        c.mark(SKIN_SHADE, [(x(0.178), 0.18), (x(0.292), 0.18), (x(0.292), 0.252), (x(0.178), 0.250)], 2.5, 0.6, curved=False)
        c.stroke(SKIN_DEEP, [(x(0.229), 0.322), (x(0.2315), 0.296), (x(0.229), 0.268)], 2.4, 0.4, 0.8, (0.4, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(x(0.190), -0.020), (x(0.278), -0.018), (x(0.278), 0.052), (x(0.190), 0.054)], 6, 0.5)
    elif view == "bottom":
        col(SKIN_SHADE, SLEEVE_END + 0.002, 0.40, 0, 0.85)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Slate work trousers, a paler rolled cuff (a block), brown boots with a darker toe cap,
# cream laces and a dark sole (blocks). Below BOOT_TOP a leg island is boot leather.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for his left leg, -1 for his right. Each leg's marks are its own."""
    x = lambda v: s * v
    c.band(BOOT, 0.0, BOOT_TOP + 0.012)
    c.band(SLATE, BOOT_TOP + 0.012, 0.20)
    # the trouser leg darkens toward the cuff, on every side
    c.band(SLATE_DARK, BOOT_TOP + 0.012, 0.112, 4, 0.6)
    if view == "front":
        c.mark(SLATE_LIT, [(x(0.052), 0.170), (x(0.076), 0.170), (x(0.076), 0.126), (x(0.052), 0.126)], 4, 0.6)
        # the crease pressed down the front of the leg
        c.stroke(SLATE_DEEP, [(x(0.0835), 0.176), (x(0.084), 0.100)], 2.4, 0.4, 0.95, (0.6, 1.0), curved=False)
        # the boot's tongue, and the seam of its toe cap
        c.mark(BOOT_LIT, [(x(0.060), 0.070), (x(0.106), 0.070), (x(0.102), 0.046), (x(0.064), 0.046)], 2.5, 0.6)
    elif view == "back":
        # one patch pocket on the seat of each leg: a hard outline, its top edge turned, stitched
        px0, px1 = sorted((x(0.056), x(0.110)))
        mid = 0.5 * (px0 + px1)
        c.mark(SLATE_DEEP, [(px0, 0.166), (px1, 0.166), (px1, 0.124), (mid, 0.114), (px0, 0.124)], 0.3, 1.0, curved=False)
        c.mark(SLATE_LIT, [(px0 + 0.0024, 0.1636), (px1 - 0.0024, 0.1636), (px1 - 0.0024, 0.1256), (mid, 0.1168), (px0 + 0.0024, 0.1256)],
               0.3, 0.55, curved=False)
        c.stitch(SLATE_DEEP, [(px0 + 0.005, 0.1570), (px1 - 0.005, 0.1570)], 1.4, 4.5, 3.5)
        # the boot's heel tab
        c.mark(BOOT_DARK, [(x(0.070), 0.074), (x(0.096), 0.074), (x(0.096), 0.026), (x(0.070), 0.026)], 0.3, 1.0, curved=False)
    else:
        outer = (view == "xpos") == (s > 0)
        # the side seam of the trouser leg, a row of stitches beside it
        c.stroke(SLATE_DEEP, [(TROUSER_Y, 0.178), (TROUSER_Y + 0.001, 0.090)], 2.2, 0.4, 0.95, (0.8, 1.0), curved=False)
        c.stitch(SLATE_LIT, [(TROUSER_Y - 0.006, 0.172), (TROUSER_Y - 0.005, 0.096)], 1.3, 4.5, 3.5, 0.7)
        if outer:
            # a slant pocket, its mouth one hard line
            c.stroke(SLATE_DEEP, [(TROUSER_Y - 0.006, 0.174), (TROUSER_Y - 0.034, 0.150)], 2.6, 0.3, 1.0, (1.0, 0.8), curved=False)
        # the boot's side: a seam curving from the ankle down to the sole, a paler quarter
        c.mark(BOOT_LIT if outer else BOOT_DARK, [(-0.036, 0.056), (0.050, 0.060), (0.054, 0.034), (-0.046, 0.032)], 4, 0.5)
        c.stroke(BOOT_DEEP, [(-0.030, 0.064), (-0.044, 0.046), (-0.066, 0.034)], 2.4, 0.3, 0.95, (0.8, 0.5))
    c.band(BOOT_DEEP, 0.016, 0.0205, 0.4, 0.9)


def paint_leg_top(c, s):
    """Looking down on a boot. The toe is at -y."""
    x = lambda v: s * v
    c.mark(BOOT_LIT, [(x(0.052), -0.060), (x(0.114), -0.060), (x(0.110), -0.010), (x(0.056), -0.010)], 4, 0.5)


# ---------------------------------------------------------------------------
# THE SWATCHES. u across, v up, both 0 to 1.
# ---------------------------------------------------------------------------

def paint_buckle(c):
    # his buckle: a brass frame, a dark slot, one prong across it. Hard edged.
    c.mark(BRASS_LIT, [(0.04, 0.96), (0.96, 0.96), (0.96, 0.84), (0.04, 0.84)], 0.5, 0.95, curved=False)
    c.mark(BRASS_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.12), (0.0, 0.12)], 0.5, 0.95, curved=False)
    c.mark(BRASS_DARK, [(0.20, 0.20), (0.80, 0.20), (0.80, 0.80), (0.20, 0.80)], 0.2, 1.0, curved=False)
    c.mark(LEATHER_DARK, [(0.225, 0.235), (0.775, 0.235), (0.775, 0.765), (0.225, 0.765)], 0.2, 1.0, curved=False)
    c.mark(BRASS_DARK, [(0.225, 0.395), (0.700, 0.395), (0.700, 0.605), (0.225, 0.605)], 0.2, 1.0, curved=False)
    c.mark(BRASS_LIT, [(0.225, 0.430), (0.680, 0.430), (0.680, 0.570), (0.225, 0.570)], 0.2, 1.0, curved=False)


def paint_pouch(c):
    # a hip pouch's outer face: a flap over the top two fifths, its edge one hard line, a brass stud
    c.mark(DARK_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.60), (0.5, 0.50), (0.0, 0.60)], 0.3, 1.0, curved=False)
    c.mark(DARK_DEEP, [(0.0, 0.60), (0.5, 0.50), (1.0, 0.60), (1.0, 0.565), (0.5, 0.465), (0.0, 0.565)], 0.3, 1.0, curved=False)
    c.stitch(DARK_DEEP, [(0.10, 0.92), (0.90, 0.92)], 1.4, 4.5, 3.5)
    c.blob(BRASS_DARK, (0.5, 0.62), 0.085, 0.083, 0.2, 1.0)
    c.blob(BRASS, (0.5, 0.62), 0.058, 0.057, 0.2, 1.0)


def paint_towel(c):
    # the towel: cream terry, a pair of red stripes near its foot, a shade where it folds over the belt
    c.mark(CREAM_SHADE, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.86), (0.0, 0.86)], 2.0, 0.9, curved=False)
    c.mark(CREAM_SHADE, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.05), (0.0, 0.05)], 0.6, 0.9, curved=False)
    c.mark(RED, [(0.0, 0.26), (1.0, 0.26), (1.0, 0.20), (0.0, 0.20)], 0.25, 1.0, curved=False)
    c.mark(RED, [(0.0, 0.16), (1.0, 0.16), (1.0, 0.13), (0.0, 0.13)], 0.25, 1.0, curved=False)
    c.stroke(CREAM_SHADE, [(0.36, 0.84), (0.34, 0.50), (0.37, 0.30)], 3.0, 0.6, 0.8, (0.4, 0.6))


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        return I(name, base, SWATCHES[name][2:4])

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c

    c = I("torso.front", SHIRT); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", SHIRT); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", SHIRT); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", SHIRT); _torso_side(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, BOOT if view == "top" else SLATE)
            if view == "top":
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c

    c = swatch("buckle", BRASS); paint_buckle(c); done[c.name] = c
    c = swatch("pouch", DARK); paint_pouch(c); done[c.name] = c
    c = swatch("towel", CREAM); paint_towel(c); done[c.name] = c
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
    atlas = Image.new("RGB", (size, size), _rgb(HAIR_DEEP))
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
