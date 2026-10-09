"""Paint the atlas of the Mang Kanor redesign, by hand, in the HEROES' style.

    py -3 tools/author_character_redesign_mang_kanor_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/mang_kanor/mang_kanor-redesign-atlas.png. The
model is built by tools/author_character_redesign_mang_kanor.py, which imports this file for the
LAYOUT only (PIL is imported inside the painting calls, Blender's Python has none) and so maps
every face onto the island painted for it here. Paint first, then build.

THE BRIEF (docs/reports/character-redesign/classic-brief.md): MANG KANOR REDRAWN AS IF HE WERE
ONE OF THE NINE HEROES. The same man, his colours and his recognisable pieces, in the heroes'
visual language. A copy of Bayan's textures script (an approved Classic, a man) rewritten for him.

WHO HE IS. "Tricycle driver. He knows every corner of this town by its potholes and he takes
them at speed. Braking was never the strong suit." (ConvertedCharacterSelect). "The
neighbourhood tito: belly first, leaning back, wide and rolling, arms out; a huffing jog"
(GaitStyles.MangKanor). An older man of the street, cheerful, no powers, no gear.

WHAT IS HIS (character-male-e.glb in person_mang_kanor.asset) AND IS KEPT: brown skin (slot 15);
dark brown hair with a point in the middle of the hairline (slot 14); big ears; ROUND GLASSES in
a dark frame; a moustache; a white shirt; blue denim overalls (slot 2) with two straps and a
waistband; dark arms below the white sleeve (slot 8); brown shoes (slots 9 and 13).

WHAT IS TAKEN FROM THE HEROES:
  * INK 1a1420 (Dante's) for eyes and mouth;
  * EYES: solid ink blocks, upright as Amihan's are (hers are 46 by 58 mm), at the heroes' eye
    line (Dante's sit between z 0.446 and 0.512). ONE CUT gives his expression: the top edge
    falls toward the OUTER corner, the kind, easy eyes of a tito (Bayan's and Dante's fall the
    other way, to the nose, which is a scowl). They sit behind his glasses;
  * MOUTH: one thin curved stroke, a small smile under the moustache, at the heroes' width;
  * HAIR in three tones by facing, no drawing on it;
  * SKIN AND CLOTH each have a base, a light, a shade and a deep tone for lines.
No nose, no wrinkles, no sockets, no blush. His AGE is said by design only: grey at the temples
and in the sideburns, a hairline gone back at the corners, the glasses, the moustache, the belly.

HIS COLOURS, each decided on purpose:
  * the lenses were two opaque white octagons with no eyes behind them. Here each is a plate in
    a dark frame, his skin seen through glass one step paler (v01 had them pale, and they read
    as eyeballs), with the heroes' ink eye on it. Opaque white with
    an ink block inside would read as an eyeball and a pupil, which no hero has;
  * the dark arms (slot 8) were ambiguous: skin three steps darker than his face. They are ARM
    SLEEVES now, the sun sleeves every tricycle and jeepney driver wears, in slot 8's dark, and
    his fists are bare in his own skin;
  * the denim is slot 2 a touch greyer (48627c for 4a6a8a) so it clears the defence role hue;
  * the gold-tan stitching of real denim, brass buttons, and one small red (slot 4): a pen in his
    bib pocket and the zip pull of his belt bag;
  * brown leather (slots 13 and 9, kept under 0.45 in value): the belt bag and his loafers, on a
    cream crepe sole, with white socks.

ROLE HUES (offence orange #f87020, defence blue #0080e8): the large cloth colours are checked in
`main`. His skin is exempt, as the cast's is. Brass, stitching and the red are small.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "mang_kanor"
ATLAS_NAME = "mang_kanor-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see `main`)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. Each family is a base, a light, a shade and a deep tone, as the heroes' are.
# ---------------------------------------------------------------------------
SKIN = "b0714a"; SKIN_LIT = "c4875c"; SKIN_SHADE = "93593a"; SKIN_DEEP = "744328"       # his slot 15
HAIR = "3d2318"; HAIR_TOP = "573524"; HAIR_DEEP = "24130c"                              # his slot 14
GREY = "8d8780"; GREY_LIT = "aaa49d"; GREY_DARK = "69635e"                              # the temples: his age
INK = "1a1420"                                                                          # Dante's ink
WHITE = "f4f1ea"; WHITE_LIT = "ffffff"; WHITE_SHADE = "d6d2c8"; WHITE_DEEP = "aaa59b"   # his slot 12
DENIM = "48627c"; DENIM_LIT = "5d7a96"; DENIM_DARK = "374b60"; DENIM_DEEP = "273645"    # his slot 2
STITCH = "d9a55e"                                                                       # denim's own thread
ROLL = "93a7bb"; ROLL_LIT = "aebfd0"; ROLL_DARK = "74889d"                              # the turned cuff: denim's pale inside
SLEEVE = "352c2b"; SLEEVE_LIT = "4d4240"; SLEEVE_DARK = "211b1a"                        # his slot 8: the arm sleeves
LEATHER = "6f4023"; LEATHER_LIT = "8a5330"; LEATHER_DARK = "4f2c17"; LEATHER_DEEP = "361c0e"   # slots 13 and 9
SOLE = "dcc8a4"; SOLE_DARK = "b39d78"
BRASS = "e0a838"; BRASS_LIT = "f4cc6a"; BRASS_DARK = "a87420"
RED = "cf534f"; RED_DARK = "9c3835"                                                     # his slot 4
FRAME = "2a2422"; FRAME_LIT = "473d39"                                                  # his slot 11: the glasses
LENS = "bf8c6c"; LENS_LIT = "d6b29c"                                                    # his skin seen through pale glass

# the large cloth colours, checked against the role hues in `main`
CLOTH_HEXES = [HAIR, HAIR_TOP, HAIR_DEEP, GREY, GREY_LIT, GREY_DARK, WHITE, WHITE_SHADE, WHITE_DEEP, DENIM, DENIM_LIT, DENIM_DARK,
               DENIM_DEEP, ROLL, ROLL_LIT, ROLL_DARK, SLEEVE, SLEEVE_LIT, SLEEVE_DARK, LEATHER, LEATHER_DARK, SOLE, SOLE_DARK]   # LEATHER_LIT is a small highlight only

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# Blender space: x his left, -y the way he faces, z up.
# ---------------------------------------------------------------------------
WAIST = (0.176, 0.224)          # the overalls' waistband
BIB = (0.224, 0.308)            # the bib, from the waistband up
BIB_HALF = (0.072, 0.058)       # its half width at its foot and at its top
STRAP = (0.030, 0.058)          # each strap, in |x|
BACK_PANEL = (0.224, 0.296)     # the high back of the overalls
BACK_HALF = (0.078, 0.060)
ARM_Z = 0.2878                  # the arm bone's height
SLEEVE_END = 0.150              # along the arm (x): the T-shirt's sleeve ends here
WRIST = 0.232                   # where the fist block starts (the arm sleeve ends just short of it)
HAND_END = 0.300
SHOE_TOP = 0.052                # below this a leg island is shoe leather
SOCK_TOP = 0.080                # the trouser cuff's foot
TROUSER_Y = 0.0285              # the trouser leg's middle, front to back
#   the hairline: (middle x, tip z, half width, half width of the tip). A point in the middle, a
#   shorter lock either side, and NOTHING at the corners: his hair has gone back there.
FRINGE = [(-0.086, 0.622, 0.036, 0.016), (0.002, 0.592, 0.050, 0.012), (0.090, 0.618, 0.036, 0.016)]
SLAB_FOOT = 0.644               # the hair's slab comes down over the forehead to here
FRINGE_SHOULDER = 0.640
#   his glasses: each lens is a plate (middle |x|, middle z, half width, half height)
LENS_AT = (0.083, 0.483, 0.053, 0.051)
MOUSTACHE_Z = 0.414

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
        "back": ((-0.17, 0.17, 0.15, 0.37), 1100),
        "xpos": ((-0.13, 0.19, 0.15, 0.37), 900),
        "xneg": ((-0.13, 0.19, 0.15, 0.37), 900),
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
    "legL": {
        "front": ((0.0, 0.17, 0.0, 0.19), 1100),
        "back": ((0.0, 0.17, 0.0, 0.19), 1000),
        "xpos": ((-0.13, 0.15, 0.0, 0.19), 950),
        "xneg": ((-0.13, 0.15, 0.0, 0.19), 800),
        "top": ((0.0, 0.17, -0.13, 0.15), 900),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.19), 1100),
        "back": ((-0.17, 0.0, 0.0, 0.19), 1000),
        "xpos": ((-0.13, 0.15, 0.0, 0.19), 800),
        "xneg": ((-0.13, 0.15, 0.0, 0.19), 950),
        "top": ((-0.17, 0.0, -0.13, 0.15), 900),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group.
VIEW_BIAS = {}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). u across, v UP.
SWATCHES = {
    "lens": (250, 240, 0.106, 0.102),       # one lens of his glasses, his LEFT (the right is its mirror), his eye on it
    "bib": (330, 200, 0.144, 0.084),        # the overalls' bib: its pocket, the pen, the stitching
    "bag": (240, 160, 0.080, 0.052),        # the face of his belt bag
}

# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    "hair_top": HAIR_TOP, "hair": HAIR, "hair_under": HAIR_DEEP,
    "grey_lit": GREY_LIT, "grey": GREY, "grey_dark": GREY_DARK,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "white_lit": WHITE_LIT, "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "denim_lit": DENIM_LIT, "denim": DENIM, "denim_dark": DENIM_DARK, "denim_deep": DENIM_DEEP,
    "roll_lit": ROLL_LIT, "roll": ROLL, "roll_dark": ROLL_DARK,
    "sleeve_lit": SLEEVE_LIT, "sleeve": SLEEVE, "sleeve_dark": SLEEVE_DARK,
    "leather_lit": LEATHER_LIT, "leather": LEATHER, "leather_dark": LEATHER_DARK, "leather_deep": LEATHER_DEEP,
    "sole": SOLE, "sole_dark": SOLE_DARK,
    "brass_lit": BRASS_LIT, "brass": BRASS, "brass_dark": BRASS_DARK,
    "red": RED, "red_dark": RED_DARK,
    "frame": FRAME, "frame_lit": FRAME_LIT,
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
# THE HEAD. His face on the flat front of the block, drawn the way the heroes' are. His eyes are
# NOT here: they are on the lenses of his glasses, which are plates standing off the face
# (`paint_lens`). The moustache is a block too.
# ---------------------------------------------------------------------------
# HIS MOUTH: one thin stroke, a small smile under the moustache (Dante's stroke is 7.4 mm)
MOUTH = [(-0.026, 0.3925), (-0.012, 0.3855), (0.012, 0.3855), (0.026, 0.3925)]
MOUTH_WIDTH = 7.2


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT: flat skin with one very soft lit patch, the hairline's shadow
    # as ONE flat tone, the mouth as one stroke. No nose, no wrinkles, no sockets, no blush.
    c.blob(SKIN_LIT, (0.0, 0.440), 0.115, 0.070, 26, 0.30)
    # the hair's cast shadow: a band under the slab and a point under each lock of the hairline
    foot = FRINGE_SHOULDER - 0.007
    poly = [(-0.26, 0.72), (0.26, 0.72), (0.26, foot)]
    for x, tip, half, tip_half in reversed(FRINGE):
        poly += [(x + half, foot), (x + tip_half, tip - 0.007), (x - tip_half, tip - 0.007), (x - half, foot)]
    poly.append((-0.26, foot))
    c.mark(SKIN_SHADE, poly, 1.4, 0.70, curved=False)
    # under each lens, the plate's own shadow on his cheek: one soft tone, so the glasses sit ON the face
    cx, cz, hw, hh = LENS_AT
    for s in (1, -1):
        c.mark(SKIN_SHADE, [(s * (cx - hw + 0.006), cz - hh - 0.005), (s * (cx + hw - 0.004), cz - hh - 0.005),
                            (s * (cx + hw - 0.010), cz - hh + 0.010), (s * (cx - hw + 0.012), cz - hh + 0.010)], 1.2, 0.55, curved=False)
    # the mouth: a small smile, one stroke and nothing under it
    c.stroke(INK, MOUTH, MOUTH_WIDTH, 0.3, 1.0, (0.5, 0.5))


def paint_lens(c):
    """His LEFT lens, seen from the front: u runs from the bridge OUT to his temple, v up.

    Pale glass over his skin, one flat streak of light across its upper outer corner, and the
    heroes' eye on it: an upright ink block, 52 by 60 mm, whose top edge falls 20 mm toward the
    outer corner. That one cut is his expression."""
    cx, cz, hw, hh = LENS_AT
    P = lambda x, z: ((x - (cx - hw)) / (2.0 * hw), (z - (cz - hh)) / (2.0 * hh))
    c.mark(LENS_LIT, [P(0.104, 0.540), P(0.122, 0.540), P(0.140, 0.518), P(0.140, 0.500)], 0.4, 0.8, curved=False)
    # (v02's fell 10 mm and read as a square; the cut has to be seen from across the street)
    eye = [(0.056, 0.458), (0.061, 0.453), (0.103, 0.453), (0.108, 0.458), (0.108, 0.489), (0.103, 0.4955), (0.062, 0.516), (0.056, 0.510)]
    c.mark(INK, [P(x, z) for x, z in eye], 0.3, 1.0, curved=False)


# ---------------------------------------------------------------------------
# THE SHIRT AND THE OVERALLS. A white T-shirt with a ribbed round neck. Over it denim overalls:
# the waistband, the bib, a strap over each shoulder and the high back are blocks, and each takes
# its paint from these islands (or the bib's own swatch) at the place it sits.
# ---------------------------------------------------------------------------

def _waistband(c, a0, a1):
    c.band(DENIM, WAIST[0] - 0.006, WAIST[1])
    c.band(DENIM_LIT, WAIST[1] - 0.0090, WAIST[1] - 0.0050, 0.3, 0.8)
    c.band(DENIM_DARK, WAIST[0] - 0.006, WAIST[0] + 0.0050, 0.4, 0.95)
    c.stitch(STITCH, [(a0, WAIST[1] - 0.0125), (a1, WAIST[1] - 0.0125)], 1.5, 5.0, 4.0)
    c.stitch(STITCH, [(a0, WAIST[0] + 0.0105), (a1, WAIST[0] + 0.0105)], 1.5, 5.0, 4.0)


def _straps(c, foot):
    for s in (1, -1):
        a, b = sorted((s * STRAP[0], s * STRAP[1]))
        c.mark(DENIM, [(a, foot), (b, foot), (b, 0.40), (a, 0.40)], 0.0, 1.0, curved=False)
        c.mark(DENIM_LIT, [(a + 0.0085, foot), (b - 0.0085, foot), (b - 0.0085, 0.40), (a + 0.0085, 0.40)], 0.6, 0.55, curved=False)
        c.stitch(STITCH, [(a + 0.0048, foot + 0.004), (a + 0.0048, 0.352)], 1.4, 5.0, 4.0)
        c.stitch(STITCH, [(b - 0.0048, foot + 0.004), (b - 0.0048, 0.352)], 1.4, 5.0, 4.0)


def _shirt_shade(c):
    """The shirt's form: a shade along its foot, where it tucks under the waistband."""
    c.mark(WHITE_SHADE, [(-0.2, WAIST[1]), (0.2, WAIST[1]), (0.2, WAIST[1] + 0.020), (-0.2, WAIST[1] + 0.020)], 5, 0.8, curved=False)


def paint_torso_front(c):
    _shirt_shade(c)
    # the round neck: his skin in the opening, a ribbed band round it
    c.mark(WHITE_DEEP, [(-0.040, 0.352), (0.040, 0.352), (0.034, 0.334), (0.014, 0.322), (-0.014, 0.322), (-0.034, 0.334)], 0.3, 1.0)
    c.mark(SKIN_SHADE, [(-0.032, 0.352), (0.032, 0.352), (0.027, 0.337), (0.011, 0.329), (-0.011, 0.329), (-0.027, 0.337)], 0.3, 1.0)
    # one fold on each side, outside the straps, pulled by the belly
    c.stroke(WHITE_SHADE, [(-0.086, 0.300), (-0.094, 0.272), (-0.090, 0.244)], 3.4, 0.6, 0.95)
    c.stroke(WHITE_SHADE, [(0.088, 0.296), (0.095, 0.268), (0.090, 0.244)], 3.4, 0.6, 0.95)
    # denim behind the bib, in case its edge shows
    c.mark(DENIM_DARK, [(-BIB_HALF[0], BIB[0]), (BIB_HALF[0], BIB[0]), (BIB_HALF[1], BIB[1]), (-BIB_HALF[1], BIB[1])], 0.0, 1.0, curved=False)
    _straps(c, BIB[1] - 0.012)
    _waistband(c, -0.150, 0.150)


def paint_torso_back(c):
    _shirt_shade(c)
    c.stroke(WHITE_SHADE, [(-0.100, 0.318), (-0.092, 0.286), (-0.098, 0.250)], 3.2, 0.6, 0.9)
    c.stroke(WHITE_SHADE, [(0.100, 0.318), (0.092, 0.286), (0.098, 0.250)], 3.2, 0.6, 0.9)
    _straps(c, BACK_PANEL[1] - 0.010)
    # the high back: a panel narrowing to the straps, its edge stitched twice, one seam down its middle
    z0, z1 = BACK_PANEL
    w0, w1 = BACK_HALF
    c.mark(DENIM, [(-w0, z0), (w0, z0), (w1, z1), (-w1, z1)], 0.0, 1.0, curved=False)
    c.mark(DENIM_LIT, [(-0.040, z0 + 0.012), (0.040, z0 + 0.012), (0.034, z1 - 0.014), (-0.034, z1 - 0.014)], 6, 0.5)
    for inset in (0.0055, 0.0105):
        c.stitch(STITCH, [(-w0 + inset, z0 + 0.003), (-w1 + inset, z1 - inset), (w1 - inset, z1 - inset), (w0 - inset, z0 + 0.003)],
                 1.3, 4.5, 3.5)
    c.stroke(DENIM_DEEP, [(0.0, z1 - 0.014), (0.0, z0)], 1.8, 0.3, 0.9, (1.0, 1.0), curved=False)
    _waistband(c, -0.150, 0.150)
    # the maker's patch on the waistband: leather, stitched, two bars burned into it
    c.mark(LEATHER_DEEP, [(-0.027, 0.214), (0.027, 0.214), (0.027, 0.184), (-0.027, 0.184)], 0.3, 1.0, curved=False)
    c.mark(LEATHER_LIT, [(-0.0245, 0.2115), (0.0245, 0.2115), (0.0245, 0.1865), (-0.0245, 0.1865)], 0.3, 1.0, curved=False)
    c.mark(LEATHER_DARK, [(-0.016, 0.2045), (0.016, 0.2045), (0.016, 0.2005), (-0.016, 0.2005)], 0.2, 1.0, curved=False)
    c.mark(LEATHER_DARK, [(-0.011, 0.1965), (0.011, 0.1965), (0.011, 0.1930), (-0.011, 0.1930)], 0.2, 1.0, curved=False)


def _torso_side(c):
    _shirt_shade(c)
    # the side seam of the shirt, from the armhole down
    c.stroke(WHITE_DEEP, [(0.024, 0.300), (0.025, 0.232)], 1.8, 0.4, 0.8, (0.7, 0.9), curved=False)
    _waistband(c, -0.110, 0.170)
    # the overalls button at the hip: a placket line and two brass buttons
    c.stroke(DENIM_DEEP, [(0.036, WAIST[1]), (0.036, WAIST[0] - 0.004)], 1.8, 0.3, 1.0, (1.0, 1.0), curved=False)
    for z in (0.211, 0.190):
        c.blob(BRASS_DARK, (0.024, z), 0.0068, 0.0068, 0.25, 1.0)
        c.blob(BRASS_LIT, (0.0235, z + 0.0008), 0.0042, 0.0042, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE ARMS. A short white sleeve to 0.150 with a turned hem (a block), then the ARM SLEEVE in
# slot 8's dark from under it to the wrist, with a grey grip band at its top and its wrist
# (blocks), then his bare fist. Along the arm (x): elbow 0.178, fist 0.232 to 0.300.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    # the fist's flat faces only
    if view in ("front", "back"):
        # three finger lines across the end of the fist, and the thumb folded over them: one hook
        for z in (ARM_Z + 0.0215, ARM_Z, ARM_Z - 0.0215):
            c.stroke(SKIN_DEEP, [(s * (HAND_END - 0.039), z), (s * (HAND_END - 0.016), z)], 2.6, 0.3, 0.95, (1.0, 0.5), curved=False)
        c.stroke(SKIN_DEEP, [(s * (WRIST + 0.016), ARM_Z + 0.0270), (s * (WRIST + 0.029), ARM_Z + 0.0290), (s * (WRIST + 0.037), ARM_Z + 0.0375)],
                 2.6, 0.3, 0.95, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * (WRIST + 0.018), -0.016), (s * (HAND_END - 0.018), -0.016), (s * (HAND_END - 0.018), 0.050), (s * (WRIST + 0.018), 0.050)], 5, 0.6)


def paint_arm(c, s, view):
    """`s` is +1 for his left arm. Each mark is cut to its own piece along the arm (x)."""
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    # the T-shirt's sleeve
    col(WHITE, 0.08, SLEEVE_END + 0.002)
    if view in ("front", "back"):
        c.stroke(WHITE_SHADE, [(x(0.118), 0.332), (x(0.128), 0.300), (x(0.124), 0.256)], 3.0, 0.6, 0.95)
        c.mark(WHITE_SHADE, [(x(0.08), 0.18), (x(0.152), 0.18), (x(0.152), 0.252), (x(0.08), 0.256)], 2.0, 0.8, curved=False)
    elif view == "bottom":
        col(WHITE_SHADE, 0.08, SLEEVE_END + 0.002, 0, 0.9)
    # the arm sleeve: dark, from under the shirt's sleeve to the wrist
    col(SLEEVE, SLEEVE_END + 0.002, WRIST + 0.004)
    if view in ("front", "back"):
        c.mark(SLEEVE_LIT, [(x(0.160), 0.330), (x(0.224), 0.326), (x(0.224), 0.304), (x(0.160), 0.306)], 5, 0.55)
        c.mark(SLEEVE_DARK, [(x(0.152), 0.18), (x(0.236), 0.18), (x(0.236), 0.254), (x(0.152), 0.252)], 2.5, 0.7, curved=False)
        # two gathers where it bunches at the elbow
        c.stroke(SLEEVE_DARK, [(x(0.174), 0.326), (x(0.178), 0.296), (x(0.175), 0.262)], 2.2, 0.4, 0.95, (0.4, 0.4))
        c.stroke(SLEEVE_DARK, [(x(0.186), 0.320), (x(0.189), 0.298), (x(0.187), 0.272)], 1.8, 0.4, 0.9, (0.4, 0.4))
    elif view == "top":
        c.mark(SLEEVE_LIT, [(x(0.160), -0.020), (x(0.224), -0.018), (x(0.224), 0.052), (x(0.160), 0.054)], 6, 0.5)
        # the seam that runs the length of a sleeve, stitched pale
        c.stitch(GREY, [(x(0.160), 0.017), (x(0.226), 0.017)], 1.3, 4.5, 3.5, 0.8)
    elif view == "bottom":
        col(SLEEVE_DARK, SLEEVE_END + 0.002, WRIST + 0.004, 0, 0.85)
    # his bare fist, drawn last so no cloth paint is left on it
    col(SKIN, WRIST + 0.004, 0.40)
    if view == "bottom":
        col(SKIN_SHADE, WRIST + 0.004, 0.40, 0, 0.85)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Denim, a paler turned cuff (a block), a white sock (a block), a brown loafer with a
# strap across the instep (a block) on a cream sole (a block). Below SHOE_TOP a leg island is
# shoe leather.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for his left leg, -1 for his right. Each leg's marks are its own."""
    x = lambda v: s * v
    c.band(LEATHER, 0.0, SHOE_TOP + 0.010)
    c.band(DENIM, SHOE_TOP + 0.010, 0.20)
    # the trouser leg darkens toward the cuff, on every side
    c.band(DENIM_DARK, SHOE_TOP + 0.010, 0.118, 4, 0.55)
    if view == "front":
        # the knee, worn pale, with two whisker lines above it
        c.blob(DENIM_LIT, (x(0.083), 0.136), 0.026, 0.020, 5, 0.75)
        c.stroke(DENIM_DARK, [(x(0.052), 0.170), (x(0.070), 0.166), (x(0.084), 0.168)], 1.8, 0.4, 0.8, (0.3, 0.3))
        c.stroke(DENIM_DARK, [(x(0.114), 0.172), (x(0.100), 0.165), (x(0.088), 0.163)], 1.8, 0.4, 0.8, (0.3, 0.3))
        c.mark(LEATHER_LIT, [(x(0.062), 0.050), (x(0.104), 0.050), (x(0.100), 0.030), (x(0.066), 0.030)], 2.5, 0.5)
    elif view == "back":
        # one patch pocket on the seat of each leg: a hard outline, stitched, the maker's arc across it
        px0, px1 = sorted((x(0.050), x(0.114)))
        mid = 0.5 * (px0 + px1)
        c.mark(DENIM_DEEP, [(px0, 0.172), (px1, 0.172), (px1, 0.128), (mid, 0.116), (px0, 0.128)], 0.3, 1.0, curved=False)
        c.mark(DENIM_LIT, [(px0 + 0.0024, 0.1696), (px1 - 0.0024, 0.1696), (px1 - 0.0024, 0.1296), (mid, 0.1188), (px0 + 0.0024, 0.1296)],
               0.3, 0.6, curved=False)
        c.stitch(STITCH, [(px0 + 0.006, 0.1640), (px1 - 0.006, 0.1640)], 1.4, 4.5, 3.5)
        c.stitch(STITCH, [(px0 + 0.008, 0.150), (mid, 0.137), (px1 - 0.008, 0.150)], 1.4, 4.0, 3.0)
        # the loafer's heel: a darker counter
        c.mark(LEATHER_DARK, [(x(0.060), 0.052), (x(0.106), 0.052), (x(0.106), 0.014), (x(0.060), 0.014)], 0.3, 1.0, curved=False)
    else:
        outer = (view == "xpos") == (s > 0)
        # the side seam, stitched twice in denim's own thread
        c.stroke(DENIM_DEEP, [(TROUSER_Y, 0.180), (TROUSER_Y + 0.001, 0.100)], 2.0, 0.4, 0.95, (0.8, 1.0), curved=False)
        c.stitch(STITCH, [(TROUSER_Y - 0.0055, 0.176), (TROUSER_Y - 0.0045, 0.104)], 1.3, 4.5, 3.5, 0.9)
        if outer:
            # a slant pocket, its mouth one hard line with a brass rivet at its foot
            c.stroke(DENIM_DEEP, [(TROUSER_Y - 0.006, 0.176), (TROUSER_Y - 0.036, 0.150)], 2.6, 0.3, 1.0, (1.0, 0.8), curved=False)
            c.blob(BRASS, (TROUSER_Y - 0.037, 0.1485), 0.0036, 0.0036, 0.2, 1.0)
        # the loafer's side: paler on the outside, one seam from the instep back to the heel
        c.mark(LEATHER_LIT if outer else LEATHER_DARK, [(-0.040, 0.042), (0.050, 0.044), (0.054, 0.026), (-0.050, 0.024)], 4, 0.5)
        c.stroke(LEATHER_DEEP, [(-0.036, 0.046), (0.010, 0.036), (0.070, 0.038)], 2.0, 0.3, 0.9, (0.8, 0.5))
    c.band(LEATHER_DEEP, 0.012, 0.0160, 0.4, 0.9)


def paint_leg_top(c, s):
    """Looking down on a loafer. The toe is at -y. The apron seam: a U round the top of the toe."""
    x = lambda v: s * v
    c.mark(LEATHER_LIT, [(x(0.050), -0.074), (x(0.116), -0.074), (x(0.112), -0.020), (x(0.054), -0.020)], 4, 0.5)
    c.stitch(LEATHER_DEEP, [(x(0.050), -0.046), (x(0.054), -0.080), (x(0.083), -0.094), (x(0.112), -0.080), (x(0.116), -0.046)], 1.6, 4.0, 3.0)


# ---------------------------------------------------------------------------
# THE SWATCHES. u across, v up, both 0 to 1.
# ---------------------------------------------------------------------------

def paint_bib(c):
    """The bib, in metres mapped to the swatch: x -0.072 to 0.072, z BIB[0] to BIB[1]."""
    P = lambda px, pz: ((px + 0.072) / 0.144, (pz - BIB[0]) / (BIB[1] - BIB[0]))
    z0, z1 = BIB
    w0, w1 = BIB_HALF
    c.mark(DENIM_LIT, [P(-0.040, z0 + 0.016), P(0.040, z0 + 0.016), P(0.034, z1 - 0.016), P(-0.034, z1 - 0.016)], 7, 0.5)
    # a turned top edge, and the edge stitched twice all round
    c.mark(DENIM_DARK, [P(-0.08, z1), P(0.08, z1), P(0.08, z1 - 0.0075), P(-0.08, z1 - 0.0075)], 0.3, 0.9, curved=False)
    for inset in (0.0050, 0.0095):
        c.stitch(STITCH, [P(-w0 + inset, z0 + 0.002), P(-w1 + inset, z1 - 0.010 - inset * 0.4), P(w1 - inset, z1 - 0.010 - inset * 0.4),
                          P(w0 - inset, z0 + 0.002)], 1.3, 4.5, 3.5)
    # the chest pocket: a hard outline, a point at its foot, its mouth stitched
    pk = [(-0.031, 0.290), (0.031, 0.290), (0.031, 0.252), (0.0, 0.240), (-0.031, 0.252)]
    c.mark(DENIM_DEEP, [P(*p) for p in pk], 0.3, 1.0, curved=False)
    inner = [(-0.0288, 0.2878), (0.0288, 0.2878), (0.0288, 0.2534), (0.0, 0.2424), (-0.0288, 0.2534)]
    c.mark(DENIM, [P(*p) for p in inner], 0.3, 1.0, curved=False)
    c.mark(DENIM_LIT, [P(*p) for p in inner], 0.3, 0.35, curved=False)
    c.stitch(STITCH, [P(-0.026, 0.2825), P(0.026, 0.2825)], 1.3, 4.5, 3.5)
    # a division for the pen, and the pen in it: a red cap with a brass clip, standing out of the pocket
    c.stroke(DENIM_DEEP, [P(0.012, 0.2878), P(0.012, 0.250)], 1.4, 0.3, 0.9, (1.0, 1.0), curved=False)
    c.mark(RED_DARK, [P(0.0165, 0.300), P(0.0255, 0.300), P(0.0255, 0.2878), P(0.0165, 0.2878)], 0.2, 1.0, curved=False)
    c.mark(RED, [P(0.0180, 0.2990), P(0.0240, 0.2990), P(0.0240, 0.2878), P(0.0180, 0.2878)], 0.2, 1.0, curved=False)
    c.mark(BRASS_LIT, [P(0.0200, 0.2975), P(0.0222, 0.2975), P(0.0222, 0.2790), P(0.0200, 0.2790)], 0.2, 1.0, curved=False)


def paint_bag(c):
    """The face of his belt bag: leather, a zip across its upper third with a red pull, a pocket seam below."""
    c.mark(LEATHER_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.86), (0.0, 0.86)], 0.5, 0.9, curved=False)
    c.mark(LEATHER_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.14), (0.0, 0.14)], 0.8, 0.9, curved=False)
    c.mark(LEATHER_DEEP, [(0.07, 0.74), (0.93, 0.74), (0.93, 0.62), (0.07, 0.62)], 0.2, 1.0, curved=False)
    c.stitch(BRASS, [(0.09, 0.68), (0.80, 0.68)], 1.6, 2.2, 1.8, 1.0)
    c.mark(RED_DARK, [(0.79, 0.76), (0.90, 0.76), (0.90, 0.44), (0.79, 0.44)], 0.2, 1.0, curved=False)
    c.mark(RED, [(0.805, 0.74), (0.885, 0.74), (0.885, 0.47), (0.805, 0.47)], 0.2, 1.0, curved=False)
    c.stroke(LEATHER_DEEP, [(0.10, 0.42), (0.16, 0.24), (0.50, 0.20), (0.84, 0.24), (0.90, 0.36)], 1.6, 0.3, 0.95, (0.8, 0.8))
    c.stitch(LEATHER_LIT, [(0.14, 0.44), (0.20, 0.30), (0.50, 0.27), (0.70, 0.29)], 1.2, 3.5, 3.0, 0.8)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        return I(name, base, SWATCHES[name][2:4])

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c

    c = I("torso.front", WHITE); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", WHITE); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", WHITE); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", WHITE); _torso_side(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, LEATHER if view == "top" else DENIM)
            if view == "top":
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c

    c = swatch("lens", LENS); paint_lens(c); done[c.name] = c
    c = swatch("bib", DENIM); paint_bib(c); done[c.name] = c
    c = swatch("bag", LEATHER); paint_bag(c); done[c.name] = c
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
