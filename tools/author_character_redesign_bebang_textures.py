"""Paint the atlas of the Bebang redesign PROTOTYPE, by hand, in the heroes' style.

    py -3 tools/author_character_redesign_bebang_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/bebang/bebang-redesign-atlas.png. The model is
built by tools/author_character_redesign_bebang.py, which imports this file for the LAYOUT only
(PIL is imported inside the painting calls, Blender's Python has none) and so maps every face
onto the island painted for it here. Paint first, then build.

WHY. Owner, 2026-10-06: the Classic street characters get the heroes' rework, and (the same
day, on v01 to v05) they are to be "reworked to be more in the heroes' style", not kept close to
the original: "the eyes and face design are different from the heros". So from v06 her face is
DRAWN THE WAY THE HEROES' FACES ARE and no longer measured off the original (see EYE_LEFT), and
her colours are given the heroes' depth. The model script's docstring has the whole brief.
Nothing in the game loads these files and character-female-c.glb is not touched.

THE RULES SHE INHERITS (section 15.3 of docs/CHARACTER_REDESIGN_DANTE.md):
  * the face is drawn for the flat front of the head: flat skin, the fringe's shadow as one
    tone, a round blush under each eye (she is a girl), two ink eyes, one stroke for a mouth.
    No nose, no sockets, creases, lids, lip shadow or contour;
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points;
  * PAINT STOPS AT ITS OWN PIECE'S EDGES. Every band below is cut to the piece that wears it;
  * A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW. The model script sends every chamfer
    and angled face to a FLAT tone (`proj_square` there), so nothing drawn here is ever smeared
    down a slope. That is why every island below keeps its marks away from the piece's edges.

HER COLOURS start from HER palette (Resources/Roster/person_bebang.asset): skin d98a5f (slot 13),
hair e0c07a (9), green 7ab04a (4), burgundy 8a3a3a (5), white (12), ink (8), and three slots no
triangle of the original wore, used here on small things: gold ffc044 (2) for three buttons,
cream fde4c7 (0) for the plaster on her knee. Each family is then given a hero's depth.
ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth colour is checked below.
Her SKIN sits near the orange by hue; skin is exempt, as it is for the cast. The gold of her
three buttons does too and is on three pieces 13 mm across.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "bebang"
ATLAS_NAME = "bebang-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. Hers, at the heroes' richness: each family starts from her palette's slot and is
# given the depth a hero's has (a light, a base, a shade, a deep).
# ---------------------------------------------------------------------------
SKIN = "d98a5f"; SKIN_LIT = "e6a074"; SKIN_SHADE = "be7449"; SKIN_DEEP = "9c5836"      # slot 13, her golden tan
BLUSH = "e0665a"
HAIR_TOP = "f5df9e"; HAIR_LIT = "e8c96e"; HAIR = "d3ab50"; HAIR_DARK = "ad893e"; HAIR_DEEP = "856a32"   # slot 9 made a real sandy blonde
INK = "1c161a"                                                                            # the heroes' near-black ink
BLOUSE = "93323a"; BLOUSE_LIT = "b24c50"; BLOUSE_MID = "802a33"; BLOUSE_DEEP = "62202a"  # slot 5, burgundy
GREEN = "74b548"; GREEN_LIT = "98d166"; GREEN_MID = "66a53e"; GREEN_DARK = "558f33"; GREEN_DEEP = "3f7127"   # slot 4
WHITE = "ffffff"; WHITE_SHADE = "e4e0da"; WHITE_DEEP = "c2bdb6"                          # slot 12
KHAKI = "c8ab78"; KHAKI_LIT = "dcc294"; KHAKI_DARK = "a48c62"                             # her shorts (slot 9 on the original), now a cloth of their own
GOLD = "ffc044"; GOLD_LIT = "ffdc88"; GOLD_DARK = "c88e24"                                # slot 2, on three buttons only
CREAM = "fde4c7"; CREAM_SHADE = "e3c39c"                                                  # slot 0, the plaster on her knee

# the gold of her three buttons sits near the orange by hue and is kept off this list: it is on
# three pieces 13 mm across (rule 10: "off large areas")
CLOTH_HEXES = [HAIR_TOP, HAIR_LIT, HAIR, HAIR_DARK, HAIR_DEEP, BLOUSE, BLOUSE_LIT, BLOUSE_MID, BLOUSE_DEEP, GREEN, GREEN_LIT, GREEN_MID,
               GREEN_DARK, GREEN_DEEP, WHITE, WHITE_SHADE, WHITE_DEEP, KHAKI, KHAKI_LIT, KHAKI_DARK, CREAM, CREAM_SHADE]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# ---------------------------------------------------------------------------
SLEEVE_END = 0.178              # along the arm (x): the mouth of the rolled sleeve
WRIST = 0.240                   # where the fist block starts
HAND_END = 0.296
SKIRT_TOP = 0.214
SKIRT_HEM = 0.122
SHORTS_HEM = 0.104              # the foot of her shorts
SOCK_TOP = (0.088, 0.068)       # her left sock pulled up, her right one slid down
SOLE_TOP = 0.024
SHOE_TOP = 0.070                # below this the leg islands are her shoe; above, her shorts

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

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). She has none:
# every loose piece of hers takes a flat tone.
SWATCHES = {}

# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    "hair_top": HAIR_TOP, "hair_lit": HAIR_LIT, "hair": HAIR, "hair_dark": HAIR_DARK, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE,
    "blouse_lit": BLOUSE_LIT, "blouse": BLOUSE, "blouse_mid": BLOUSE_MID, "blouse_deep": BLOUSE_DEEP,
    "green_lit": GREEN_LIT, "green": GREEN, "green_mid": GREEN_MID, "green_dark": GREEN_DARK, "green_deep": GREEN_DEEP,
    "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "khaki_lit": KHAKI_LIT, "khaki": KHAKI, "khaki_dark": KHAKI_DARK,
    "gold_lit": GOLD_LIT, "gold": GOLD, "gold_dark": GOLD_DARK,
    "cream": CREAM, "cream_shade": CREAM_SHADE, "ink": INK,
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
# Owner, 2026-10-06, on the first Classic prototypes: "the eyes and face design are different
# from the heros". v01 to v05 measured the original's wide bat-wing eyes and thick filled mouth.
# Those are gone. What the redesigned heroes share, read off their texture scripts:
#   * EYES: two solid ink blocks, upright, taller than wide, corners cut 6 mm; Amihan's are
#     53 by 67 mm centred at (0.076, 0.481), Cheska's the same size at (0.072, 0.487). The only
#     expression in them is ONE CUT (Amihan's top edge slants 4 mm; Cheska's foot has a notch);
#   * MOUTH: one curved stroke 8.6 mm wide, about 60 mm across, centred near 0.420;
#   * a round blush under each eye on the girls; ink 181418 to 1f1c24; nothing else.
# HERS: the same block, 52 by 64 mm at (0.077, 0.482). HER CUT is the top edge, slanted 10 mm
# down to the inner corner: brows set, sure of herself. It is more than twice Amihan's slant, so
# beside her Bebang reads tough where Amihan reads smug. HER MOUTH is the heroes' stroke drawn
# as a wide grin, 84 mm across, its corner on her left hooked higher than the one on her right:
# cheerful, and a little lopsided, the way a kid grins before she flattens your can.
EYE_CENTRE = (0.077, 0.482)
EYE_LEFT = [(0.097, 0.5140), (0.103, 0.5075), (0.103, 0.4560), (0.097, 0.4500), (0.057, 0.4500), (0.051, 0.4560),
            (0.051, 0.4965), (0.056, 0.5028)]
GRIN = [(-0.0410, 0.4270), (-0.0260, 0.4160), (-0.0040, 0.4110), (0.0190, 0.4150), (0.0350, 0.4255), (0.0425, 0.4375)]
MOUTH_WIDTH = 8.6                # mm, the heroes' stroke
#   the fringe's shadow: (from x, to x, the height it falls to), under each lock's own tip
FRINGE_SHADOW = [(-0.200, -0.142, 0.538), (-0.142, -0.060, 0.572), (-0.060, 0.030, 0.542), (0.030, 0.106, 0.582), (0.106, 0.200, 0.556)]


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (rule 12): flat skin with one very soft lit patch, the fringe's
    # shadow as ONE flat tone, a round blush under each eye, her two ink eyes, her mouth as one
    # stroke. No nose, no sockets, creases, lids, lip shadow or contour.
    c.blob(SKIN_LIT, (0.0, 0.455), 0.118, 0.080, 26, 0.30)
    shadow = [(-0.26, 0.72), (0.26, 0.72)]
    for x0, x1, z in reversed(FRINGE_SHADOW):
        shadow += [(x1, z), (x0, z)]
    c.mark(SKIN_SHADE, shadow, 1.4, 0.66, curved=False)
    c.blob(BLUSH, (-0.110, 0.424), 0.031, 0.020, 7, 0.58)
    c.blob(BLUSH, (0.110, 0.424), 0.031, 0.020, 7, 0.58)
    c.mark(INK, EYE_LEFT, 0.25, 1.0, curved=False)
    c.mark(INK, [(-x, z) for x, z in EYE_LEFT], 0.25, 1.0, curved=False)
    c.stroke(INK, GRIN, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, GRIN[0], 0.0028, 0.0028, 0.25, 1.0)
    c.blob(INK, GRIN[-1], 0.0028, 0.0028, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE BLOUSE. Burgundy. The V neck's piping, the placket, the buttons, the pocket, the necklace
# and the skirt are geometry; what is drawn here is the cloth itself, the way the heroes' cloth
# is drawn: a lit patch, a fold or two, and rows of stitches where it is sewn.
# Every mark keeps clear of the block's chamfers (14 mm): those take a flat tone.
# ---------------------------------------------------------------------------

def paint_torso_front(c):
    c.mark(BLOUSE_LIT, [(-0.116, 0.300), (-0.064, 0.296), (-0.058, 0.232), (-0.118, 0.228)], 9, 0.60)
    c.mark(BLOUSE_LIT, [(0.060, 0.230), (0.118, 0.228), (0.116, 0.212), (0.058, 0.214)], 6, 0.45)
    # a fold from each armpit toward the waist, and one under the pocket
    c.stroke(BLOUSE_DEEP, [(-0.122, 0.292), (-0.106, 0.262), (-0.100, 0.228)], 4.2, 0.7, 0.85, (0.3, 0.8))
    c.stroke(BLOUSE_DEEP, [(0.122, 0.292), (0.110, 0.268), (0.114, 0.232)], 4.0, 0.7, 0.85, (0.3, 0.8))
    c.stroke(BLOUSE_DEEP, [(-0.052, 0.250), (-0.044, 0.234), (-0.046, 0.220)], 3.2, 0.7, 0.75, (0.3, 0.8))
    # a row of stitches either side of the placket
    c.stitch(BLOUSE_DEEP, [(-0.0185, 0.288), (-0.0185, 0.218)], 1.5, 4.5, 3.5)
    c.stitch(BLOUSE_DEEP, [(0.0185, 0.288), (0.0185, 0.218)], 1.5, 4.5, 3.5)
    # the shoulder seams, running out to the sleeves
    c.stitch(BLOUSE_DEEP, [(-0.124, 0.318), (-0.070, 0.324)], 1.5, 4.5, 3.5)
    c.stitch(BLOUSE_DEEP, [(0.070, 0.324), (0.124, 0.318)], 1.5, 4.5, 3.5)


def paint_torso_back(c):
    c.mark(BLOUSE_LIT, [(-0.100, 0.300), (0.100, 0.300), (0.094, 0.246), (-0.096, 0.244)], 9, 0.55)
    # the yoke across her shoulders, sewn, and the box pleat under it
    c.stroke(BLOUSE_DEEP, [(-0.122, 0.300), (0.0, 0.294), (0.122, 0.300)], 2.6, 0.3, 0.9, (0.9, 0.9))
    c.stitch(BLOUSE_LIT, [(-0.118, 0.306), (0.0, 0.300), (0.118, 0.306)], 1.5, 4.5, 3.5, 0.8)
    c.stroke(BLOUSE_DEEP, [(-0.014, 0.292), (-0.016, 0.218)], 2.6, 0.4, 0.9, (0.9, 0.6), curved=False)
    c.stroke(BLOUSE_DEEP, [(0.014, 0.292), (0.016, 0.218)], 2.6, 0.4, 0.9, (0.9, 0.6), curved=False)
    c.mark(BLOUSE_LIT, [(-0.010, 0.288), (0.010, 0.288), (0.012, 0.222), (-0.012, 0.222)], 2, 0.5)


def _torso_side(c):
    # the side seam, under the arm
    c.stroke(BLOUSE_DEEP, [(0.002, 0.262), (0.003, 0.218)], 2.4, 0.4, 0.9, (0.6, 0.8), curved=False)
    c.stitch(BLOUSE_DEEP, [(0.010, 0.262), (0.011, 0.218)], 1.5, 4.5, 3.5)


# ---------------------------------------------------------------------------
# THE ARMS. A burgundy sleeve (its rolled cuff and white edge are geometry), a bare forearm,
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
    col(BLOUSE, 0.08, SLEEVE_END + 0.004)
    if view in ("front", "back"):
        c.mark(BLOUSE_LIT, [(x(0.118), 0.322), (x(0.150), 0.330), (x(0.150), 0.300), (x(0.118), 0.298)], 5, 0.6)
        c.stroke(BLOUSE_DEEP, [(x(0.114), 0.300), (x(0.130), 0.272), (x(0.150), 0.252)], 3.4, 0.6, 0.85, (0.3, 0.8))
        # the armhole seam
        c.stitch(BLOUSE_DEEP, [(x(0.112), 0.330), (x(0.110), 0.250)], 1.5, 4.5, 3.5)
    elif view == "top":
        c.mark(BLOUSE_LIT, [(x(0.116), -0.030), (x(0.150), -0.036), (x(0.150), 0.046), (x(0.116), 0.040)], 6, 0.6)
        c.stitch(BLOUSE_DEEP, [(x(0.111), -0.040), (x(0.111), 0.050)], 1.5, 4.5, 3.5)
    # everything past the sleeve is skin; the hand is drawn last so no cloth paint is left on it
    col(SKIN, SLEEVE_END + 0.004, 0.33)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Khaki shorts above; her canvas sneaker below. Her bare knee, her socks, the shoe's
# toe cap, laces and sole are flat-toned blocks and take nothing from these islands.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for her left leg, -1 for her right. Each leg's marks are its own."""
    x = lambda v: s * v
    # the shoe first, then the shorts above it
    c.band(BLOUSE, 0.0, SHOE_TOP)
    if view == "back":
        # the heel: a white tab up the back of the shoe
        c.mark(WHITE, [(x(0.072), 0.060), (x(0.096), 0.060), (x(0.096), 0.030), (x(0.072), 0.030)], 0.3, 1.0, curved=False)
    elif view in ("xpos", "xneg"):
        outer = (view == "xpos") == (s > 0)
        # the side of the shoe: the eyelet stay, a seam curving to the sole, and on the OUTER
        # side a round white ankle patch with a stitched rim
        c.mark(BLOUSE_LIT, [(-0.078, 0.054), (-0.030, 0.058), (-0.034, 0.044), (-0.080, 0.042)], 2, 0.7)
        c.stroke(BLOUSE_DEEP, [(-0.026, 0.060), (-0.034, 0.044), (-0.052, 0.034), (-0.084, 0.030)], 2.4, 0.3, 1.0, (0.9, 0.5))
        c.stitch(BLOUSE_LIT, [(0.010, 0.030), (0.060, 0.030)], 1.4, 4.0, 3.0, 0.8)
        if outer:
            c.blob(BLOUSE_DEEP, (0.030, 0.047), 0.0125, 0.0125, 0.3, 1.0)
            c.blob(WHITE, (0.030, 0.047), 0.0100, 0.0100, 0.3, 1.0)
    c.band(KHAKI, SHOE_TOP, 0.20)
    if view == "front":
        c.mark(KHAKI_LIT, [(x(0.056), 0.166), (x(0.112), 0.166), (x(0.112), 0.130), (x(0.056), 0.130)], 5, 0.5)
    elif view in ("xpos", "xneg"):
        # the side seam of the shorts, sewn
        c.stroke(KHAKI_DARK, [(0.000, 0.176), (0.001, 0.122)], 2.2, 0.3, 0.9, (0.8, 0.8), curved=False)
        c.stitch(KHAKI_DARK, [(0.008, 0.176), (0.009, 0.122)], 1.4, 4.0, 3.0)


def paint_leg_top(c, s):
    """Looking down on a shoe: only its few level faces take this. Plain canvas."""


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c

    c = I("torso.front", BLOUSE); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", BLOUSE); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", BLOUSE); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", BLOUSE); _torso_side(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, BLOUSE)
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
    atlas = Image.new("RGB", (size, size), _rgb(BLOUSE_DEEP))
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
