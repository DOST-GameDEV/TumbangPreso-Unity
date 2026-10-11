"""Paint the atlas of the Lola Pacing redesign PROTOTYPE, by hand, in the heroes' style.

    py -3 tools/author_character_redesign_lola_pacing_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/lola_pacing/lola_pacing-redesign-atlas.png. The
model is built by tools/author_character_redesign_lola_pacing.py, which imports this file for the
LAYOUT only (PIL is imported inside the painting calls, Blender's Python has none) and so maps
every face onto the island painted for it here. Paint first, then build.

THE BRIEF CHANGED AT v05 (owner, 2026-10-06, on v04 of the three Classic patterns): "head size
stays the same as the heroes. my issue is they focus on resembling closer to the original design
instead of being reworked to be more in the heroes' style. the eyes and face design are
different from the heros". So this is no longer the original with more detail. It is LOLA
PACING REDRAWN AS IF SHE WERE ONE OF THE NINE HEROES: the same person and her recognisable
pieces (the big bun, the earrings, a neat taupe jacket over white, bell sleeves with white
cuffs), designed in the heroes' visual language. For the Classic cast the rules "nothing
invented" and "eyes measured off the original" are LIFTED. HEAD_SCALE stays 0.84.
v01 to v04 followed the first brief (the original's ink shapes, the original's peach hair, a
plain jacket); their notes are kept where they explain a shape that is still here.

HER COLOURS. Her original is the shared rig character-female-d.glb recoloured by the 16 colours
of Resources/Roster/person_lola_pacing.asset. Those 16 are still the only source:
    slot 9   8a7a6a  taupe: her jacket and trousers, the base of the outfit
    slot 12  ffffff  white: her blouse, collar, cuffs, earrings
    slot 15  f7c9a6  her skin
    slot 8   38383d  the soles of her slippers
    slot 11  a0a8c9  SILVER: HER HAIR. ⚠️ A DECISION MADE ON 2026-10-06, PENDING THE OWNER'S
                     WORD. The rig wears slot 13 (e8a07a, a warm peach) on its hair and that is
                     what the game has shown; slot 11 is in her palette and no triangle of the
                     rig wears it. Silver reads as a grandmother at once and it clears the
                     offence orange's hue, which the peach shared (21 degrees against 22).
    slot 4   cf534f  her ONE ACCENT, a soft red: the piping, her comb, her fan, her brooch, the
                     print, her slippers. Nothing of the rig wears it either.
The ink of the face is the HEROES' ink (a near black, 1a1620), not her slot 8 grey: Amihan's is
181418, Phaister's 14101c, Dante's 1a1420.

HER FACE IS DRAWN THE WAY THE HEROES' ARE (see THE HEAD below): one solid ink block for each
eye at the heroes' size and height, one thin curved stroke for the mouth, a round blush.
AGE WITHOUT REALISM: no wrinkle, bag, fold or sag anywhere. She is a grandmother by her silver
set hair and its bun, her earrings and comb, her clothes, how she stands and how she moves.

STILL TRUE: no painted hair shine (hair is flat tones by facing); paint stops at its own
piece's edges; a drawing only on a face that squarely faces its view (`proj_square` in the
model script), every chamfer a flat tone.

ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth and hair colour is checked
in `main`. Her accent red is 20 degrees of hue from the orange and passes; her palette's blue
(slot 5) and orange (slot 3) sit on the role hues and are not used.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "lola_pacing"
ATLAS_NAME = "lola_pacing-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is her palette slot, unchanged; the rest are steps of it,
# spread as far apart as the heroes' steps are so a piece reads against the one it lies on.
# ---------------------------------------------------------------------------
SKIN = "f7c9a6"; SKIN_LIT = "fddcc2"; SKIN_SHADE = "e3a987"; SKIN_DEEP = "c98c69"
BLUSH = "ee8a82"
HAIR = "a0a8c9"; HAIR_LIT = "d4d8ea"; HAIR_DARK = "7a82aa"; HAIR_DEEP = "585f86"
INK = "1a1620"
TAUPE = "8a7a6a"; TAUPE_LIT = "a8957f"; TAUPE_MID = "74655a"; TAUPE_DEEP = "594c44"
WHITE = "ffffff"; WHITE_SHADE = "e6dfd6"; WHITE_DEEP = "c4b9ad"
ROSE = "cf534f"; ROSE_LIT = "e57a7e"; ROSE_DARK = "a23b3d"
SHOE = "38383d"; SHOE_LIT = "505058"; SHOE_DEEP = "26262a"

CLOTH_HEXES = [TAUPE, TAUPE_LIT, TAUPE_MID, TAUPE_DEEP, WHITE, WHITE_SHADE, WHITE_DEEP, ROSE, ROSE_LIT, ROSE_DARK,
               SHOE, SHOE_LIT, SHOE_DEEP, HAIR, HAIR_LIT, HAIR_DARK, HAIR_DEEP]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# All read off character-female-d.glb (Blender space: +x her left, -y the way she faces, z up).
# ---------------------------------------------------------------------------
FRONT_Y = -0.0804               # the flat front of her jacket
V_NECK = [(-0.0999, 0.3432), (0.0999, 0.3432), (0.0, 0.2147)]     # the white blouse in her neck, the original's one triangle
WAIST = 0.2147                  # where the jacket's skirt starts (the point of the V is on it)
HEM = 0.1378                    # the foot of the jacket's skirt
ELBOW = 0.2097                  # along the arm (x): where the narrow sleeve meets the bell
BELL = (0.2097, 0.2724)         # the wide mouth of her sleeve
CUFF = (0.2724, 0.3124)         # the white cuff
WRIST = 0.3440                  # where the fist block starts (the far 14 per cent of the arm)
HAND_END = 0.3834
ARM_Y_PAINT = 0.01725             # the arm's own line, for marks on its top and underside
TROUSER_FOOT = 0.0775
SHOE_TOP = 0.0575

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y).
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    # only the face is drawn; every other side of the head block is under hair or is flat skin
    "head": {
        "front": ((-0.19, 0.19, 0.33, 0.68), 1500),
    },
    "torso": {
        "front": ((-0.19, 0.19, 0.12, 0.36), 1300),
        "back": ((-0.19, 0.19, 0.12, 0.36), 800),
        "xpos": ((-0.14, 0.20, 0.12, 0.36), 800),
        "xneg": ((-0.14, 0.20, 0.12, 0.36), 800),
    },
    "armL": {
        "front": ((0.09, 0.40, 0.19, 0.385), 1000),
        "back": ((0.09, 0.40, 0.19, 0.385), 900),
        "top": ((0.09, 0.40, -0.08, 0.12), 900),
        "bottom": ((0.09, 0.40, -0.08, 0.12), 600),
    },
    "armR": {
        "front": ((-0.40, -0.09, 0.19, 0.385), 1000),
        "back": ((-0.40, -0.09, 0.19, 0.385), 900),
        "top": ((-0.40, -0.09, -0.08, 0.12), 900),
        "bottom": ((-0.40, -0.09, -0.08, 0.12), 600),
    },
    "legL": {
        "front": ((0.0, 0.17, 0.0, 0.19), 1000),
        "back": ((0.0, 0.17, 0.0, 0.19), 800),
        "xpos": ((-0.13, 0.14, 0.0, 0.19), 900),
        "xneg": ((-0.13, 0.14, 0.0, 0.19), 800),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.19), 1000),
        "back": ((-0.17, 0.0, 0.0, 0.19), 800),
        "xpos": ((-0.13, 0.14, 0.0, 0.19), 800),
        "xneg": ((-0.13, 0.14, 0.0, 0.19), 900),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group.
VIEW_BIAS = {}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). u across, v UP.
SWATCHES = {}      # she has none: her lapels and hems are blocks in flat tones

# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    "hair_lit": HAIR_LIT, "hair": HAIR, "hair_dark": HAIR_DARK, "hair_under": HAIR_DEEP,
    "rose_lit": ROSE_LIT, "rose": ROSE, "rose_dark": ROSE_DARK,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE,
    "taupe_lit": TAUPE_LIT, "taupe": TAUPE, "taupe_mid": TAUPE_MID, "taupe_deep": TAUPE_DEEP,
    "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "shoe_lit": SHOE_LIT, "shoe": SHOE, "shoe_deep": SHOE_DEEP,
}
FLAT_PX = 40

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin. `head.front`, `taupe`, and so on.

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
        """Everything between two heights, the whole island wide."""
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
# THE HEAD. Her face on the flat front of the block, DRAWN AS THE HEROES' FACES ARE.
# What the heroes have, read off their texture scripts (all on this same head box, 0.343 to 0.661):
#   Amihan    each eye one upright ink block, corners cut 6 mm: 46 by 58 mm about (0.076, 0.481),
#             grown 1.15; the mouth one curved stroke 60 mm wide and 8.6 mm thick at 0.416 to
#             0.429; a round blush 64 by 44 mm at (0.108, 0.421)
#   Cheska    the same block at 1.15 with a notch in its foot
#   Phaister  the block HALF CLOSED: 74 by 32 mm, a flat top with a flick at the outer corner
#   Dante     a block at 1.28; the mouth a 7.4 mm stroke
# HERS: the same block at the same place (the middle of each eye 0.077 out and 0.481 up, 52 mm
# wide), CURVED LIKE A SMILE: its top corners are cut well back and its foot is scooped up in
# the middle, so each eye is a solid block standing on two feet, 44 mm tall at the feet and
# 30 mm thick over the scoop. A grandmother who is pleased to see you, and who has seen what you are about to try.
# v01 to v04 used the original's own ink shapes (slanted bat wings with a notch) and a thick
# filled mouth; the owner: "the eyes and face design are different from the heros".
# ---------------------------------------------------------------------------
EYE_CENTRE = (0.077, 0.481)
#   one eye about its own middle, (dx, dz) in metres, clockwise from the inner foot; mirrored for the right
EYE = [(-0.0260, -0.0200), (-0.0260, 0.0100), (-0.0220, 0.0180), (-0.0140, 0.0235), (0.0140, 0.0235), (0.0220, 0.0180),
       (0.0260, 0.0100), (0.0260, -0.0200), (0.0170, -0.0200), (0.0130, -0.0130), (0.0070, -0.0085), (0.0, -0.0070),
       (-0.0070, -0.0085), (-0.0130, -0.0130), (-0.0170, -0.0200)]
#   (v05 ran a curve through a deeper scoop and the eye came out a thin arc, a closed eye, not a
#   block. The outline is drawn as it is listed now: 52 by 44 mm, 30 mm thick over the scoop.)
#   her mouth: one thin curved stroke at the heroes' weight, 56 mm wide, both ends turned up
MOUTH = [(-0.0280, 0.4275), (-0.0170, 0.4185), (0.0, 0.4155), (0.0170, 0.4185), (0.0280, 0.4275)]
MOUTH_WIDTH = 8.0                # mm; Amihan's is 8.6, Dante's 7.4
HAIRLINE = 0.5544                # where her hair starts on her brow


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (rule 12): flat skin with one very soft lit patch, a round blush
    # under each eye, two ink blocks, one stroke. No nose, no sockets, creases, lids, lip shadow or
    # contour, and no mark of age: nothing dark below the eyes, nothing beside the mouth.
    c.blob(SKIN_LIT, (0.0, 0.452), 0.105, 0.065, 26, 0.30)
    # (no shadow is drawn under her fringe: only the middle facets of the head take this drawing,
    # so a band of shade stopped dead at each temple, v01. The locks shade the brow themselves.)
    c.blob(BLUSH, (-0.106, 0.427), 0.030, 0.020, 7, 0.58)
    c.blob(BLUSH, (0.106, 0.427), 0.030, 0.020, 7, 0.58)
    for s in (1, -1):
        c.mark(INK, [(s * (EYE_CENTRE[0] + dx), EYE_CENTRE[1] + dz) for dx, dz in EYE], 0.3, 1.0, curved=False)
    c.stroke(INK, MOUTH, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    for end in (MOUTH[0], MOUTH[-1]):
        c.blob(INK, end, 0.0026, 0.0026, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE JACKET. Taupe, with a short flared skirt from the waist. The blouse, its collar, the
# lapels, the waistband, the piping, her brooch and her fan are geometry; what is drawn here is
# the cloth itself: a few folds, the seams, and HER PRINT, small red flowers with a white heart
# scattered on the skirt of the jacket (and on the bell of each sleeve), the print of a house
# dress. Every mark keeps clear of the block's chamfers: those take a flat tone.
# ---------------------------------------------------------------------------

def _flower(c, at, r=0.0078):
    """One printed flower: five round petals and a white heart, hard edged (rule 5)."""
    x, z = at
    for k in range(5):
        a = math.radians(90.0 + 72.0 * k)
        c.blob(ROSE, (x + 1.05 * r * math.cos(a), z + 1.05 * r * math.sin(a)), 0.72 * r, 0.72 * r, 0.2, 1.0)
    c.blob(ROSE, (x, z), 0.8 * r, 0.8 * r, 0.2, 1.0)
    c.blob(WHITE, (x, z), 0.42 * r, 0.42 * r, 0.2, 1.0)


def _print(c, a0, a1, flip=False):
    """Two staggered rows of flowers across the skirt of the jacket, from `a0` to `a1`."""
    n = max(2, int(round((a1 - a0) / 0.050)))
    for k in range(n + 1):
        x = a0 + (a1 - a0) * k / n
        high = (k % 2 == 0) != flip
        _flower(c, (x, 0.1920 if high else 0.1660))


def paint_torso_front(c):
    c.mark(TAUPE_LIT, [(-0.110, 0.300), (-0.060, 0.300), (-0.052, 0.232), (-0.110, 0.232)], 7, 0.6)
    c.mark(TAUPE_LIT, [(0.110, 0.300), (0.060, 0.300), (0.052, 0.232), (0.110, 0.232)], 7, 0.6)
    # the skirt of the jacket: where it closes, one fold a side, and the print
    c.stroke(TAUPE_DEEP, [(0.0, WAIST - 0.010), (0.001, HEM + 0.016)], 2.6, 0.35, 0.95, (0.9, 0.9), curved=False)
    c.stroke(TAUPE_DEEP, [(-0.058, 0.204), (-0.062, 0.180), (-0.066, 0.156)], 3.4, 0.7, 0.8, (0.3, 0.8))
    c.stroke(TAUPE_DEEP, [(0.058, 0.204), (0.062, 0.180), (0.066, 0.156)], 3.4, 0.7, 0.8, (0.3, 0.8))
    _print(c, -0.124, -0.022)
    _print(c, 0.022, 0.124, flip=True)


def paint_torso_back(c):
    c.mark(TAUPE_LIT, [(-0.086, 0.318), (0.086, 0.318), (0.092, 0.240), (-0.092, 0.240)], 9, 0.5)
    # the seam down the middle of her back
    c.stroke(TAUPE_DEEP, [(0.0, 0.334), (0.0, WAIST + 0.010)], 2.4, 0.35, 0.9, (0.6, 0.9), curved=False)
    c.stroke(TAUPE_DEEP, [(0.0, WAIST - 0.010), (0.0, HEM + 0.016)], 3.0, 0.4, 0.9, (0.9, 0.9), curved=False)
    _print(c, -0.124, -0.022)
    _print(c, 0.022, 0.124, flip=True)


def _torso_side(c):
    # the side seam, under her arm to the waist, and the print carried round
    c.stroke(TAUPE_DEEP, [(0.030, 0.250), (0.030, WAIST + 0.010)], 2.2, 0.35, 0.85, (0.6, 0.9), curved=False)
    _print(c, -0.086, 0.142)


# ---------------------------------------------------------------------------
# THE ARMS. A sleeve that opens from the elbow into a wide bell (a cone now, so it reads as a
# sleeve and not as a ring), red piping round its mouth (geometry), a white cuff with a lace edge,
# then her bare wrist and her fist.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    # the fist's flat faces only: x 0.357 to 0.370 along the arm, z 0.245 to 0.330
    if view in ("front", "back"):
        for z in (0.3050, 0.2877, 0.2704):
            c.stroke(SKIN_DEEP, [(s * 0.3600, z), (s * 0.3700, z)], 2.2, 0.3, 0.85, (1.0, 0.5), curved=False)
        # the thumb, folded along the top of the fist: one drawn hook
        c.stroke(SKIN_DEEP, [(s * 0.3565, 0.3205), (s * 0.3640, 0.3225), (s * 0.3700, 0.3285)], 2.2, 0.3, 0.85, (0.8, 0.4))


def paint_arm(c, s, view):
    """`s` is +1 for her left arm. Each mark is cut to its own piece along the arm (x)."""
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    col(TAUPE, 0.08, CUFF[0])
    if view in ("front", "back"):
        c.stroke(TAUPE_DEEP, [(x(0.150), 0.330), (x(0.158), 0.290), (x(0.152), 0.250)], 3.2, 0.6, 0.8)
        # the print, on the bell: one flower on each flat side of it
        _flower(c, (x(0.2400), 0.2877), 0.0072)
    elif view == "top":
        c.mark(TAUPE_LIT, [(x(0.118), -0.020), (x(0.196), -0.020), (x(0.196), 0.054), (x(0.118), 0.054)], 6, 0.6)
        _flower(c, (x(0.2400), ARM_Y_PAINT), 0.0072)
    # the cuff: white, with a lace edge at its far end (a line and a row of small holes)
    col(WHITE, CUFF[0], CUFF[1] + 0.0005)
    col(WHITE_SHADE, CUFF[1] - 0.0118, CUFF[1] - 0.0102, 0.3, 0.95)
    if view in ("front", "back"):
        for k in range(7):
            c.blob(WHITE_DEEP, (x(CUFF[1] - 0.0058), 0.2502 + 0.0125 * k), 0.0022, 0.0022, 0.25, 0.9)
    elif view in ("top", "bottom"):
        for k in range(7):
            c.blob(WHITE_DEEP, (x(CUFF[1] - 0.0058), ARM_Y_PAINT - 0.0375 + 0.0125 * k), 0.0022, 0.0022, 0.25, 0.9)
    # everything past the cuff is skin; the hand is drawn last so no cloth paint is left on it
    col(SKIN, CUFF[1] + 0.0005, 0.41)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Taupe trousers with a pressed crease; the flared hem and her slippers are blocks in
# flat tones, so only the trouser leg is drawn.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for her left leg, -1 for her right."""
    x = lambda v: s * v
    if view in ("front", "back"):
        c.stroke(TAUPE_DEEP, [(x(0.083), 0.170), (x(0.083), 0.084)], 2.4, 0.4, 0.9, (0.5, 0.9), curved=False)
    else:
        c.mark(TAUPE_LIT, [(-0.012, 0.164), (0.066, 0.164), (0.066, 0.118), (-0.012, 0.118)], 6, 0.45)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        return I(name, base, SWATCHES[name][2:4])

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c

    c = I("torso.front", TAUPE); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", TAUPE); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", TAUPE); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", TAUPE); _torso_side(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, TAUPE)
            paint_leg(c, s, view)
            done[c.name] = c

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
    atlas = Image.new("RGB", (size, size), _rgb(TAUPE_DEEP))
    bleed = max(1, int(round(GUTTER * size / float(ATLAS))))
    finished = {name: isl.finished() for name, isl in islands.items()}
    for name, img in finished.items():
        x, y, w, h = islands[name].rect
        atlas.paste(img.resize((w + 2 * bleed, h + 2 * bleed), Image.BILINEAR), (x - bleed, y - bleed))
    for name, img in finished.items():
        x, y, w, h = islands[name].rect
        atlas.paste(img, (x, y))

    out_dir = OUT_DIR
    if "--out-dir" in sys.argv:
        out_dir = Path(sys.argv[sys.argv.index("--out-dir") + 1])
    os.makedirs(out_dir, exist_ok=True)
    out = out_dir / ATLAS_NAME
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
