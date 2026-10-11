"""Paint the atlas of the Inday redesign PROTOTYPE, by hand, in the heroes' style.

    py -3 tools/author_character_redesign_inday_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/inday/inday-redesign-atlas.png. The model is
built by tools/author_character_redesign_inday.py, which imports this file for the LAYOUT only
(PIL is imported inside the painting calls, Blender's Python has none) and so maps every face
onto the island painted for it here. Paint first, then build.

WHY. Owner, 2026-10-06: the twelve Classic street characters are REDRAWN AS IF THEY WERE HEROES
(docs/CHARACTER_REDESIGN_DANTE.md section 15.9 part D). Three were approved as the pattern
(bayan, bebang, lola_pacing); this file is a copy of Bebang's rewritten for Inday. Her face is
DRAWN THE WAY THE HEROES' FACES ARE (see EYE_LEFT) and her colours are given the heroes' depth.
Nothing in the game loads these files and character-female-a.glb is not touched.

THE RULES SHE INHERITS (section 15.3 of that document):
  * the face is drawn for the flat front of the head: flat skin, the fringe's shadow as one
    tone, a round blush under each eye (she is a girl), two ink eyes, one stroke for a mouth.
    No nose, no sockets, creases, lip shadow or contour;
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points;
  * PAINT STOPS AT ITS OWN PIECE'S EDGES. Every band below is cut to the piece that wears it;
  * A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW. The model script sends every chamfer
    and angled face to a FLAT tone (`proj_square` there), so nothing drawn here is ever smeared
    down a slope. That is why every island below keeps its marks away from the piece's edges.

HER COLOURS start from HER palette (Resources/Roster/person_inday.asset): deep brown skin 5e3721
(slot 14), black hair (slot 8, the cast's ink slot: so the heroes' BLACK), a gold-yellow top
e0b43c (7), red shorts cf534f (4, see below), a clay waistband b9714a (13), tan shoes d99a6c
(15), white soles (12), and four slots no triangle of the original wore, used on small things:
cream fde4c7 (0) for her apron, pale blue d0e8ff (6) for her ribbon and the towel's stripe,
mint 61cb8b (1) for a bracelet, gold ffc044 (2) for two buttons.
WHERE A COLOUR WAS MOVED, AND WHY:
  * SKIN. Her slot is 5e3721. Black hair and ink eyes on that have almost no step (Dante's
    DEEPEST skin tone is 6c371c). So her slot is kept as her SHADE and the base is one step up,
    7c4829: she is still by far the darkest girl in any row, and her eyes can be read.
  * THE SHORTS. The original's sample slot 5, c2543f, and the waistband's b9714a both sit
    within 18 degrees of the offence orange #f87020. The shorts take her palette's own red
    (slot 4) and the waistband is dulled to a clay; the tan of her shoes is dulled the same way.
ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth colour is checked below.
Skin is exempt, as it is for the cast. The gold of two buttons is on pieces 13 mm across.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "inday"
ATLAS_NAME = "inday-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. Hers, at the heroes' richness: each family starts from her palette's slot and is
# given the depth a hero's has (a light, a base, a shade, a deep).
# ---------------------------------------------------------------------------
SKIN = "7c4829"; SKIN_LIT = "91572f"; SKIN_SHADE = "5e3721"; SKIN_DEEP = "43240f"       # slot 14 is her SHADE (see the docstring)
BLUSH = "b8473c"
HAIR = "1a181e"; HAIR_TOP = "2e2b38"; HAIR_DEEP = "0e0d12"                               # slot 8: Dante's black
INK = "1a1420"                                                                            # the heroes' ink
TOP = "e2b33a"; TOP_LIT = "f4d064"; TOP_MID = "cc9c2c"; TOP_DEEP = "a87c22"              # slot 7, her gold-yellow top
RED = "c94a48"; RED_LIT = "df6a62"; RED_DARK = "a63a3c"; RED_DEEP = "832c32"             # slot 4, her shorts
CLAY = "a26f5a"; CLAY_LIT = "bd8a72"; CLAY_DARK = "7a5546"                                # slot 13 dulled: the apron's tie and trim
CREAM = "fbe6cc"; CREAM_LIT = "fff4e4"; CREAM_SHADE = "e3c8a6"; CREAM_DEEP = "c4a680"     # slot 0, the apron
BLUE = "bcdcf7"; BLUE_LIT = "e2f1ff"; BLUE_DARK = "8fb8dd"; BLUE_DEEP = "7397be"          # slot 6, her ribbon
MINT = "61cb8b"; MINT_DARK = "45a36c"                                                     # slot 1, a bracelet
TAN = "c4926f"; TAN_LIT = "d9ad8c"; TAN_DARK = "a47a5c"; TAN_DEEP = "83614a"              # slot 15 dulled: her shoes
WHITE = "ffffff"; WHITE_SHADE = "e4e0da"; WHITE_DEEP = "c2bdb6"                          # slot 12
GOLD = "ffc044"; GOLD_LIT = "ffdc88"; GOLD_DARK = "c88e24"                                # slot 2, on two buttons only

# the gold of her buttons sits near the orange by hue and is kept off this list: it is on
# pieces 13 mm across ("off large areas")
CLOTH_HEXES = [HAIR, HAIR_TOP, HAIR_DEEP, TOP, TOP_LIT, TOP_MID, TOP_DEEP, RED, RED_LIT, RED_DARK, RED_DEEP, CLAY, CLAY_LIT, CLAY_DARK,
               CREAM, CREAM_LIT, CREAM_SHADE, CREAM_DEEP, BLUE, BLUE_LIT, BLUE_DARK, BLUE_DEEP, MINT, MINT_DARK, TAN, TAN_LIT, TAN_DARK,
               TAN_DEEP, WHITE, WHITE_SHADE, WHITE_DEEP]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# ---------------------------------------------------------------------------
SLEEVE_END = 0.142              # along the arm (x): the mouth of her cap sleeve
WRIST = 0.240                   # where the fist block starts
HAND_END = 0.296
WAIST = 0.216                   # the foot of her top; under it the shorts' seat
APRON_TOP = 0.222               # the top of the apron's tie
APRON_HEM = 0.116
APRON_HALF = 0.112              # half the apron's width at its hem
SHORTS_HEM = 0.104              # the foot of her shorts
SOCK_TOP = 0.070
SOLE_TOP = 0.022
SHOE_TOP = 0.064                # below this the leg islands are her shoe; above, her shorts

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y). +x is HER left, -y is the way she faces.
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
        "front": ((-0.17, 0.17, 0.16, 0.36), 1300),
        "back": ((-0.17, 0.17, 0.16, 0.36), 1000),
        "xpos": ((-0.11, 0.11, 0.16, 0.36), 900),
        "xneg": ((-0.11, 0.11, 0.16, 0.36), 900),
    },
    "armL": {
        "front": ((0.09, 0.31, 0.19, 0.385), 1100),
        "back": ((0.09, 0.31, 0.19, 0.385), 900),
        "top": ((0.09, 0.31, -0.10, 0.12), 900),
        "bottom": ((0.09, 0.31, -0.10, 0.12), 700),
    },
    "armR": {
        "front": ((-0.31, -0.09, 0.19, 0.385), 1100),
        "back": ((-0.31, -0.09, 0.19, 0.385), 900),
        "top": ((-0.31, -0.09, -0.10, 0.12), 900),
        "bottom": ((-0.31, -0.09, -0.10, 0.12), 700),
    },
    "legL": {
        "front": ((0.0, 0.17, 0.0, 0.19), 1000),
        "back": ((0.0, 0.17, 0.0, 0.19), 800),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 950),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 800),
        "top": ((0.0, 0.17, -0.16, 0.11), 800),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.19), 1000),
        "back": ((-0.17, 0.0, 0.0, 0.19), 800),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 800),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 950),
        "top": ((-0.17, 0.0, -0.16, 0.11), 800),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group.
VIEW_BIAS = {}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). One: the front
# of her apron, a flat plate that squarely faces the front.
APRON_WINDOW = (-0.125, 0.125, APRON_HEM - 0.004, APRON_TOP - 0.016)     # (x0, x1, z0, z1) in metres
SWATCHES = {"apron": (300, 118, APRON_WINDOW[1] - APRON_WINDOW[0], APRON_WINDOW[3] - APRON_WINDOW[2])}

# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    "hair_top": HAIR_TOP, "hair": HAIR, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "top_lit": TOP_LIT, "top": TOP, "top_mid": TOP_MID, "top_deep": TOP_DEEP,
    "red_lit": RED_LIT, "red": RED, "red_dark": RED_DARK, "red_deep": RED_DEEP,
    "clay_lit": CLAY_LIT, "clay": CLAY, "clay_dark": CLAY_DARK,
    "cream_lit": CREAM_LIT, "cream": CREAM, "cream_shade": CREAM_SHADE, "cream_deep": CREAM_DEEP,
    "blue_lit": BLUE_LIT, "blue": BLUE, "blue_dark": BLUE_DARK, "blue_deep": BLUE_DEEP,
    "mint": MINT, "mint_dark": MINT_DARK,
    "tan_lit": TAN_LIT, "tan": TAN, "tan_dark": TAN_DARK, "tan_deep": TAN_DEEP,
    "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "gold_lit": GOLD_LIT, "gold": GOLD, "gold_dark": GOLD_DARK, "ink": INK,
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
# THE HEAD. Her face on the flat front of the block, IN THE HEROES' HAND.
# ---------------------------------------------------------------------------
# What the redesigned heroes share, read off their texture scripts:
#   * EYES: two solid ink blocks, upright, taller than wide, corners cut 6 mm; Amihan's are
#     53 by 67 mm centred at (0.076, 0.481), Cheska's the same size at (0.072, 0.487). The only
#     expression in them is ONE CUT (Amihan's top edge slants 4 mm; Cheska's foot has a notch;
#     of the approved Classics, Bebang's and Bayan's tops slant down to the nose and Lola
#     Pacing's feet are cut by a smile's arc);
#   * MOUTH: one curved stroke 8.6 mm wide, about 60 mm across, centred near 0.420;
#   * a round blush under each eye on the girls; nothing else.
# HERS: the same block at the same place, 52 mm wide, HALF CLOSED. HER CUT is the lid: the top
# of the block is taken off LEVEL, 8 mm down, and the lid line runs 10 mm past the outer corner
# as one lash. Nobody else in the cast has it. It is the look of the girl who "minds the corner
# stall and is afraid of absolutely nothing that walks past it" (the character select) and is
# "ice-cold" (her tagline): unbothered, a little amused. The original's eyes were shut in a
# smile; these are the same ease, drawn the heroes' way.
# HER MOUTH is the heroes' stroke drawn as her "cheeky cat smirk" (the tagline): it runs level
# from her right and hooks up at her left end only. One stroke, NOT Cheska's drawn "w".
EYE_CENTRE = (0.077, 0.474)
LID = 0.5060                     # the level her lids are shut to (a hero's open eye tops out at 0.514; 0.4985 in v01 read as sleepy)
EYE_LEFT = [(0.103, LID), (0.103, 0.4560), (0.097, 0.4500), (0.057, 0.4500), (0.051, 0.4560), (0.051, LID)]
LASH_LEFT = [(0.090, LID + 0.0005), (0.1135, LID + 0.0095), (0.103, LID - 0.0105)]     # the lid line, carried out past the corner
SMIRK = [(-0.0300, 0.4170), (-0.0120, 0.4140), (0.0080, 0.4150), (0.0240, 0.4215), (0.0330, 0.4330)]
MOUTH_WIDTH = 8.6                # mm, the heroes' stroke
#   the fringe's shadow: (from x, to x, the height it falls to), under each lock's own tip
FRINGE_SHADOW = [(-0.200, -0.128, 0.600), (-0.128, -0.036, 0.580), (-0.036, 0.062, 0.558), (0.062, 0.138, 0.536), (0.138, 0.200, 0.514)]


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT: flat skin with one very soft lit patch, the fringe's shadow
    # as ONE flat tone, a round blush under each eye, her two ink eyes, her mouth as one stroke.
    c.blob(SKIN_LIT, (0.0, 0.450), 0.118, 0.080, 26, 0.42)
    shadow = [(-0.26, 0.72), (0.26, 0.72)]
    for x0, x1, z in reversed(FRINGE_SHADOW):
        shadow += [(x1, z), (x0, z)]
    c.mark(SKIN_SHADE, shadow, 1.4, 0.62, curved=False)
    c.blob(BLUSH, (-0.110, 0.424), 0.031, 0.020, 7, 0.60)
    c.blob(BLUSH, (0.110, 0.424), 0.031, 0.020, 7, 0.60)
    for s in (1, -1):
        c.mark(INK, [(s * x, z) for x, z in EYE_LEFT], 0.25, 1.0, curved=False)
        c.mark(INK, [(s * x, z) for x, z in LASH_LEFT], 0.25, 1.0, curved=False)
    c.stroke(INK, SMIRK, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, SMIRK[0], 0.0028, 0.0028, 0.25, 1.0)
    c.blob(INK, SMIRK[-1], 0.0028, 0.0028, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE TOP. Gold-yellow, sleeveless under two cap sleeves. The V neck's white binding, the
# buttons, the apron and its tie are geometry; what is drawn here is the cloth itself, the way
# the heroes' cloth is drawn: a lit patch, a fold or two, and rows of stitches where it is sewn.
# Every mark keeps clear of the block's chamfers (14 mm): those take a flat tone.
# The torso block is the top only (the shorts' seat under it is a block of its own).
# ---------------------------------------------------------------------------

def paint_torso_front(c):
    c.mark(TOP_LIT, [(-0.106, 0.302), (-0.060, 0.298), (-0.056, 0.246), (-0.108, 0.242)], 9, 0.60)
    c.mark(TOP_LIT, [(0.050, 0.262), (0.104, 0.262), (0.102, 0.238), (0.048, 0.240)], 6, 0.45)
    # a fold from each armpit toward the waist
    c.stroke(TOP_DEEP, [(-0.112, 0.296), (-0.098, 0.268), (-0.094, 0.240)], 3.8, 0.7, 0.80, (0.3, 0.8))
    c.stroke(TOP_DEEP, [(0.112, 0.296), (0.102, 0.272), (0.104, 0.242)], 3.6, 0.7, 0.80, (0.3, 0.8))
    # the button band below the V: a row of stitches either side (the two buttons are geometry)
    c.stitch(TOP_DEEP, [(-0.0165, 0.284), (-0.0165, 0.232)], 1.5, 4.5, 3.5)
    c.stitch(TOP_DEEP, [(0.0165, 0.284), (0.0165, 0.232)], 1.5, 4.5, 3.5)
    # the shoulder seams, running out to the sleeves
    c.stitch(TOP_DEEP, [(-0.114, 0.320), (-0.072, 0.325)], 1.5, 4.5, 3.5)
    c.stitch(TOP_DEEP, [(0.072, 0.325), (0.114, 0.320)], 1.5, 4.5, 3.5)


def paint_torso_back(c):
    c.mark(TOP_LIT, [(-0.092, 0.304), (0.092, 0.304), (0.086, 0.258), (-0.088, 0.256)], 9, 0.55)
    # a keyhole at the back of the neck, closed by one loop: a slit and its stitched edge
    c.stroke(TOP_DEEP, [(0.0, 0.312), (0.0, 0.272)], 3.4, 0.3, 1.0, (0.9, 0.3), curved=False)
    c.stitch(TOP_DEEP, [(-0.011, 0.326), (-0.011, 0.272), (0.0, 0.262), (0.011, 0.272), (0.011, 0.326)], 1.5, 4.0, 3.0)
    # two darts running down to the waist
    c.stroke(TOP_DEEP, [(-0.058, 0.276), (-0.052, 0.236)], 2.4, 0.4, 0.85, (0.3, 0.9), curved=False)
    c.stroke(TOP_DEEP, [(0.058, 0.276), (0.052, 0.236)], 2.4, 0.4, 0.85, (0.3, 0.9), curved=False)


def _torso_side(c):
    # the side seam, under the arm
    c.stroke(TOP_DEEP, [(0.002, 0.266), (0.003, 0.232)], 2.4, 0.4, 0.9, (0.6, 0.8), curved=False)
    c.stitch(TOP_DEEP, [(0.010, 0.266), (0.011, 0.232)], 1.5, 4.5, 3.5)


# ---------------------------------------------------------------------------
# THE APRON, the one painted swatch: a cream half apron, the kind a girl minding a bakery stall
# ties on. Geometry carries its tie, its hem band, its pocket and the towel; drawn here are the
# cloth, a row of stitches inside its edge, and ONE embroidered mark on her right side: a
# pandesal, the roll the corner bakery sells, in clay thread. Lengths below are METRES on the
# apron (x across her, z up), turned into the swatch's 0..1 by `at`.
# ---------------------------------------------------------------------------

def paint_apron(c):
    x0, x1, z0, z1 = APRON_WINDOW
    at = lambda x, z: ((x - x0) / (x1 - x0), (z - z0) / (z1 - z0))
    pts = lambda rows: [at(x, z) for x, z in rows]
    c.mark(CREAM_LIT, pts([(-0.090, 0.196), (0.030, 0.196), (0.026, 0.150), (-0.094, 0.146)]), 8, 0.6)
    # two soft folds falling from the tie
    c.stroke(CREAM_SHADE, pts([(-0.020, 0.200), (-0.016, 0.168), (-0.020, 0.134)]), 3.0, 0.8, 0.9, (0.3, 0.8))
    c.stroke(CREAM_SHADE, pts([(-0.096, 0.200), (-0.100, 0.170), (-0.097, 0.140)]), 2.6, 0.8, 0.8, (0.3, 0.8))
    # the stitched edge, down each side
    c.stitch(CLAY, pts([(-0.102, 0.200), (-0.104, 0.136)]), 1.5, 4.5, 3.5)
    c.stitch(CLAY, pts([(0.102, 0.200), (0.104, 0.136)]), 1.5, 4.5, 3.5)
    # the pandesal: an oval roll with a paler top and its split
    cx, cz = -0.060, 0.164
    c.blob(CLAY_DARK, at(cx, cz), 0.0215 / (x1 - x0), 0.0155 / (z1 - z0), 0.3, 1.0)
    c.blob(CLAY_LIT, at(cx, cz), 0.0185 / (x1 - x0), 0.0125 / (z1 - z0), 0.3, 1.0)
    c.blob(CREAM_LIT, at(cx - 0.003, cz + 0.004), 0.0110 / (x1 - x0), 0.0050 / (z1 - z0), 1.2, 0.75)
    c.stroke(CLAY_DARK, pts([(cx - 0.011, cz - 0.003), (cx, cz - 0.0005), (cx + 0.011, cz - 0.003)]), 2.0, 0.3, 1.0, (0.4, 0.4))


# ---------------------------------------------------------------------------
# THE ARMS. A yellow cap sleeve to 0.142 (its turned edge is geometry), then her bare arm and
# her fist. Everything past the sleeve's mouth is skin.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    # the fist block's flat faces only: x 0.253 to 0.283 along the arm, z 0.243 to 0.333
    if view in ("front", "back"):
        for z in (0.3070, 0.2880, 0.2690):
            c.stroke(SKIN_DEEP, [(s * 0.2640, z), (s * 0.2815, z)], 2.4, 0.3, 0.9, (1.0, 0.5), curved=False)
        # the thumb, folded along the top of the fist: one drawn hook
        c.stroke(SKIN_DEEP, [(s * 0.2540, 0.3230), (s * 0.2660, 0.3250), (s * 0.2730, 0.3310)], 2.4, 0.3, 0.9, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.256, -0.026), (s * 0.280, -0.026), (s * 0.280, 0.038), (s * 0.256, 0.038)], 4, 0.5)


def paint_arm(c, s, view):
    """`s` is +1 for her left arm. Each mark is cut to its own piece along the arm (x)."""
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    col(TOP, 0.08, SLEEVE_END + 0.004)
    if view in ("front", "back"):
        c.mark(TOP_LIT, [(x(0.112), 0.322), (x(0.132), 0.328), (x(0.132), 0.300), (x(0.112), 0.298)], 4, 0.6)
        # the armhole seam
        c.stitch(TOP_DEEP, [(x(0.110), 0.330), (x(0.108), 0.250)], 1.5, 4.5, 3.5)
    elif view == "top":
        c.mark(TOP_LIT, [(x(0.112), -0.030), (x(0.132), -0.034), (x(0.132), 0.044), (x(0.112), 0.040)], 5, 0.6)
        c.stitch(TOP_DEEP, [(x(0.109), -0.040), (x(0.109), 0.050)], 1.5, 4.5, 3.5)
    # everything past the sleeve is skin; the hand is drawn last so no cloth paint is left on it
    col(SKIN, SLEEVE_END + 0.004, 0.33)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Red shorts above; her strap shoe below. Her bare knee, her sock, the shoe's toe,
# strap, button and sole are flat-toned blocks and take nothing from these islands.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for her left leg, -1 for her right. Each leg's marks are its own."""
    x = lambda v: s * v
    # the shoe first, then the shorts above it
    c.band(TAN, 0.0, SHOE_TOP)
    if view == "back":
        # the heel counter: a darker tab up the back of the shoe, stitched
        c.mark(TAN_DARK, [(x(0.066), 0.052), (x(0.102), 0.052), (x(0.102), 0.026), (x(0.066), 0.026)], 0.3, 1.0, curved=False)
        c.stitch(TAN_LIT, [(x(0.070), 0.047), (x(0.098), 0.047)], 1.3, 3.5, 2.5, 0.9)
    elif view in ("xpos", "xneg"):
        # the side of the shoe: a seam curving down from the strap to the sole, and a lit quarter
        c.mark(TAN_LIT, [(-0.070, 0.046), (-0.026, 0.048), (-0.030, 0.036), (-0.072, 0.034)], 2, 0.7)
        c.stroke(TAN_DARK, [(-0.014, 0.052), (-0.022, 0.040), (-0.044, 0.030), (-0.078, 0.027)], 2.2, 0.3, 1.0, (0.9, 0.5))
        c.stitch(TAN_DARK, [(0.008, 0.029), (0.058, 0.029)], 1.3, 4.0, 3.0, 0.9)
    c.band(RED, SHOE_TOP, 0.20)
    if view == "front":
        c.mark(RED_LIT, [(x(0.056), 0.172), (x(0.112), 0.172), (x(0.112), 0.136), (x(0.056), 0.136)], 5, 0.5)
    elif view == "back":
        # a patch pocket on the seat of each leg, stitched
        c.stitch(RED_DEEP, [(x(0.052), 0.176), (x(0.052), 0.142), (x(0.083), 0.132), (x(0.114), 0.142), (x(0.114), 0.176)], 1.5, 4.0, 3.0)
    elif view in ("xpos", "xneg"):
        # the side seam of the shorts, sewn
        c.stroke(RED_DEEP, [(0.000, 0.176), (0.001, 0.124)], 2.2, 0.3, 0.9, (0.8, 0.8), curved=False)
        c.stitch(RED_DEEP, [(0.008, 0.176), (0.009, 0.124)], 1.4, 4.0, 3.0)


def paint_leg_top(c, s):
    """Looking down on a shoe: only its few level faces take this. Plain leather."""


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c

    c = I("torso.front", TOP); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", TOP); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", TOP); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", TOP); _torso_side(c); done[c.name] = c
    c = I("apron", CREAM, SWATCHES["apron"][2:]); paint_apron(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, TAN)
            if view == "top":
                paint_leg_top(c, s)
            else:
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
    atlas = Image.new("RGB", (size, size), _rgb(TOP_DEEP))
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
