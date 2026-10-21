"""Paint the atlas of the Jun-Jun redesign PROTOTYPE, by hand, in the heroes' style.

    py -3 tools/author_character_redesign_jun_jun_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/jun_jun/jun_jun-redesign-atlas.png. The model is
built by tools/author_character_redesign_jun_jun.py, which imports this file for the LAYOUT only
(PIL is imported inside the painting calls, Blender's Python has none) and so maps every face
onto the island painted for it here. Paint first, then build.

WHY. Owner, 2026-10-06: the Classic street characters are REDRAWN AS IF THEY WERE HEROES
(docs/reports/character-redesign/classic-brief.md). So his face is drawn the way the heroes'
faces are and is not measured off the original, and his colours are given the heroes' depth.
The layout and the brush below are the approved Bebang prototype's, copied (one character, one
set of files); everything that is drawn is his own. The model script's docstring has the whole
design. Nothing in the game loads these files and character-male-d.glb is not touched.

THE RULES HE INHERITS (section 15.3 of docs/CHARACTER_REDESIGN_DANTE.md):
  * the face is drawn for the flat front of the head: flat skin, the fringe's shadow as one
    tone, two ink eyes, one stroke for a mouth. No nose, no sockets, creases, lids, lip shadow
    or contour, and NO BLUSH (he is a boy);
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points;
  * PAINT STOPS AT ITS OWN PIECE'S EDGES. Every band below is cut to the piece that wears it;
  * A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW. The model script sends every chamfer
    and angled face to a FLAT tone (`proj_square` there), so nothing drawn here is ever smeared
    down a slope. That is why every island below keeps its marks away from the piece's edges.

HIS COLOURS start from HIS palette (Resources/Roster/person_jun_jun.asset): skin f7c9a6 (slot
15), hair e8a07a (13), navy 222839 (8), white (12), the tie's gold e8c24a (4), and slots no
triangle of the original wore, used here on small things: the grey-blues 868ba1, 4f5260 and
a0a8c9 (9, 10, 11) for his shorts and socks, the mint 61cb8b (1) for one sticker.
ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth colour is checked below.
HIS HAIR IS THE HARD ONE. Slot 13 is a peach that sits ON the offence orange by hue, and a head
of hair is a large area. So his hair keeps its hue (a light copper brown, the sandy red a fair
child has) and gives up saturation instead: every tone of it is under the checker's line. His
SKIN sits near the orange by hue too; skin is exempt, as it is for the cast.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "jun_jun"
ATLAS_NAME = "jun_jun-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. His, at the heroes' richness: each family starts from his palette's slot and is
# given the depth a hero's has (a light, a base, a shade, a deep).
# ---------------------------------------------------------------------------
SKIN = "f2c29e"; SKIN_LIT = "fbd6b8"; SKIN_SHADE = "dba27c"; SKIN_DEEP = "b87e5c"       # slot 15, a fair child's skin
# (v01's were a tone lighter, sat too near his skin and his head read bald from across the street)
# (v02's were still pale beside the row; these are as deep as the copper goes under the checker's line)
HAIR_TOP = "c99c80"; HAIR_LIT = "b08065"; HAIR = "986e58"; HAIR_DARK = "765244"; HAIR_DEEP = "573a2f"   # slot 13 as a light copper brown
INK = "1c161a"                                                                            # the heroes' near-black ink
NAVY = "2a3352"; NAVY_LIT = "464f88"; NAVY_MID = "212840"; NAVY_DEEP = "171c2e"          # slot 8, his jacket
GREY = "5b6076"; GREY_LIT = "767c94"; GREY_DARK = "464a5c"                                # slot 10 lifted, his short trousers
SOCK = "c9d2ea"; SOCK_SHADE = "a7b0cc"; SOCK_DEEP = "868ba1"                              # slots 11 and 9, his long socks
WHITE = "ffffff"; WHITE_SHADE = "e2e4ec"; WHITE_DEEP = "bfc3d0"                          # slot 12, his shirt
GOLD = "edc84c"; GOLD_LIT = "f8e08a"; GOLD_DARK = "b8922c"                                # slot 4, his tie and his buttons
SHOE = "2b2b35"; SHOE_LIT = "55586e"; SHOE_DEEP = "17171d"; SOLE = "9499b0"              # black leather school shoes
MINT = "61cb8b"; MINT_DARK = "3f9f66"                                                     # slot 1, the star sticker on his lapel

CLOTH_HEXES = [HAIR_TOP, HAIR_LIT, HAIR, HAIR_DARK, HAIR_DEEP, NAVY, NAVY_LIT, NAVY_MID, NAVY_DEEP, GREY, GREY_LIT, GREY_DARK,
               SOCK, SOCK_SHADE, SOCK_DEEP, WHITE, WHITE_SHADE, WHITE_DEEP, GOLD, GOLD_LIT, GOLD_DARK, SHOE, SHOE_LIT, SHOE_DEEP, SOLE,
               MINT, MINT_DARK]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# ---------------------------------------------------------------------------
SLEEVE_END = 0.226              # along the arm (x): the mouth of the jacket's sleeve
WRIST = 0.240                   # where the fist block starts; between the two, his shirt cuff
HAND_END = 0.296
HEM_TOP = 0.208                 # the jacket's skirt, the part that hangs over his hips
HEM = 0.154
SHORTS_HEM = 0.110              # the foot of his short trousers
SOCK_TOP = 0.084                # both pulled up, the way his mother left them
SOLE_TOP = 0.024
SHOE_TOP = 0.070                # below this the leg islands are his shoe; above, his shorts

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y). +x is HIS left, -y is the way he faces.
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

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). He has none:
# every loose piece of his takes a flat tone.
SWATCHES = {}


# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    "hair_top": HAIR_TOP, "hair_lit": HAIR_LIT, "hair": HAIR, "hair_dark": HAIR_DARK, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE,
    "navy_lit": NAVY_LIT, "navy": NAVY, "navy_mid": NAVY_MID, "navy_deep": NAVY_DEEP,
    "grey_lit": GREY_LIT, "grey": GREY, "grey_dark": GREY_DARK,
    "sock": SOCK, "sock_shade": SOCK_SHADE, "sock_deep": SOCK_DEEP,
    "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "gold_lit": GOLD_LIT, "gold": GOLD, "gold_dark": GOLD_DARK,
    "shoe_lit": SHOE_LIT, "shoe": SHOE, "shoe_deep": SHOE_DEEP, "sole": SOLE,
    "mint": MINT, "mint_dark": MINT_DARK, "ink": INK,
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
# THE HEAD. His face on the flat front of the block, IN THE HEROES' HAND.
# ---------------------------------------------------------------------------
# What the redesigned heroes share, read off their texture scripts:
#   * EYES: two solid ink blocks, upright, taller than wide, corners cut 6 mm; Amihan's are
#     53 by 67 mm centred at (0.076, 0.481). The only expression in them is ONE CUT;
#   * MOUTH: one curved stroke 8.6 mm wide, about 60 mm across, centred near 0.420;
#   * ink 181418 to 1f1c24; a blush on the girls only; nothing else.
# HIS: the same block at the same place, 52 mm wide, its foot and its sides the heroes'. HIS CUT
# IS THE LID: the top of each block is taken off flat, lower at the OUTER corner, so the eyes are
# half shut and drooping, a child who was asleep ten minutes ago ("the sleepy kid dragged out to
# play", GaitStyles.JunJun). The two are not the same: the eye on HIS RIGHT is shut a little
# further than the one on his left. Dante's cut is a scowl and a closed eye, Bayan's and
# Bebang's slant DOWN to the inner corner (brows set); his slants the other way and reads tired,
# not tough. HIS MOUTH is the heroes' stroke, short and off its middle: the small pleased smile
# the original had, kept, on a face that is not quite awake.
#   the eye on his left (+x): outer side at 0.103, inner at 0.051; the right is mirrored and its lid lower
EYE_FOOT = [(0.103, 0.4560), (0.097, 0.4500), (0.057, 0.4500), (0.051, 0.4560)]
# (v01's lids were 12 mm lower and the eyes read as two slots, half the size of a hero's)
LID_LEFT = (0.5125, 0.4965)      # the height of the lid at the inner corner, at the outer
LID_RIGHT = (0.5065, 0.4895)
SMILE = [(-0.0240, 0.4225), (-0.0100, 0.4160), (0.0080, 0.4150), (0.0240, 0.4195), (0.0330, 0.4265)]
MOUTH_WIDTH = 8.6                # mm, the heroes' stroke
#   the fringe's shadow: (from x, to x, the height it falls to), under each lock's own tip
FRINGE_SHADOW = [(-0.200, -0.150, 0.584), (-0.150, 0.016, 0.590), (0.016, 0.092, 0.574), (0.092, 0.160, 0.556), (0.160, 0.200, 0.532)]


def _eye(side, lid):
    inner, outer = lid
    pts = EYE_FOOT + [(0.051, inner - 0.002), (0.054, inner), (0.100, outer + 0.0006), (0.103, outer - 0.002)]
    return [(side * x, z) for x, z in pts]


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (rule 12): flat skin with one very soft lit patch, the fringe's
    # shadow as ONE flat tone, his two ink eyes, his mouth as one stroke. Nothing else.
    c.blob(SKIN_LIT, (0.0, 0.455), 0.118, 0.080, 26, 0.30)
    shadow = [(-0.26, 0.72), (0.26, 0.72)]
    for x0, x1, z in reversed(FRINGE_SHADOW):
        shadow += [(x1, z), (x0, z)]
    c.mark(SKIN_SHADE, shadow, 1.4, 0.62, curved=False)
    c.mark(INK, _eye(1, LID_LEFT), 0.25, 1.0, curved=False)
    c.mark(INK, _eye(-1, LID_RIGHT), 0.25, 1.0, curved=False)
    c.stroke(INK, SMILE, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, SMILE[0], 0.0028, 0.0028, 0.25, 1.0)
    c.blob(INK, SMILE[-1], 0.0028, 0.0028, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE JACKET. Navy. Its lapels, the shirt in the V, the tie, the buttons, the pocket and its
# handkerchief, the sticker and the skirt of the jacket are geometry; what is drawn here is the
# cloth itself, the way the heroes' cloth is drawn: a lit patch, a fold or two, and rows of
# stitches where it is sewn. Every mark keeps clear of the block's chamfers (14 mm).
# ---------------------------------------------------------------------------

def paint_torso_front(c):
    c.mark(NAVY_LIT, [(-0.110, 0.300), (-0.074, 0.296), (-0.066, 0.236), (-0.112, 0.232)], 9, 0.55)
    c.mark(NAVY_LIT, [(0.050, 0.236), (0.112, 0.234), (0.110, 0.214), (0.046, 0.216)], 6, 0.40)
    # a fold from each armpit toward the button: the jacket is a size too big and pulls there
    c.stroke(NAVY_DEEP, [(-0.114, 0.292), (-0.094, 0.262), (-0.060, 0.236)], 4.2, 0.7, 0.9, (0.3, 0.8))
    c.stroke(NAVY_DEEP, [(0.114, 0.292), (0.098, 0.266), (0.066, 0.240)], 4.0, 0.7, 0.9, (0.3, 0.8))
    c.stroke(NAVY_DEEP, [(-0.104, 0.250), (-0.086, 0.232), (-0.060, 0.222)], 3.0, 0.7, 0.8, (0.3, 0.8))
    # the front edge, his left side lapped over his right, from the point of the V to the hem,
    # and the row of stitches that follows it
    c.stroke(NAVY_DEEP, [(0.000, 0.232), (-0.008, 0.196)], 2.8, 0.3, 1.0, (0.9, 0.9), curved=False)
    c.stitch(NAVY_LIT, [(0.008, 0.228), (0.001, 0.196)], 1.5, 4.5, 3.5, 0.9)
    # the shoulder seams, running out to the sleeves
    c.stitch(NAVY_LIT, [(-0.114, 0.320), (-0.094, 0.324)], 1.5, 4.5, 3.5, 0.8)
    c.stitch(NAVY_LIT, [(0.094, 0.324), (0.114, 0.320)], 1.5, 4.5, 3.5, 0.8)


def paint_torso_back(c):
    c.mark(NAVY_LIT, [(-0.096, 0.306), (0.096, 0.306), (0.090, 0.262), (-0.092, 0.260)], 9, 0.50)
    # the centre seam down his back and the shoulder yoke, sewn
    c.stroke(NAVY_DEEP, [(0.0, 0.316), (0.001, 0.196)], 2.6, 0.3, 1.0, (0.9, 0.9), curved=False)
    c.stitch(NAVY_LIT, [(0.008, 0.312), (0.009, 0.252)], 1.5, 4.5, 3.5, 0.8)
    c.stitch(NAVY_LIT, [(-0.112, 0.314), (-0.040, 0.318)], 1.5, 4.5, 3.5, 0.8)
    c.stitch(NAVY_LIT, [(0.040, 0.318), (0.112, 0.314)], 1.5, 4.5, 3.5, 0.8)
    # THE HALF BELT across the small of his back, the one smart thing on a boy's jacket: a band
    # a tone lighter with a dark edge above and below (its two gold buttons are geometry)
    c.mark(NAVY_DEEP, [(-0.072, 0.2495), (0.072, 0.2495), (0.072, 0.2205), (-0.072, 0.2205)], 0.4, 1.0, curved=False)
    c.mark(NAVY_LIT, [(-0.070, 0.2470), (0.070, 0.2470), (0.070, 0.2230), (-0.070, 0.2230)], 0.4, 1.0, curved=False)
    c.stitch(NAVY, [(-0.064, 0.2430), (0.064, 0.2430)], 1.3, 4.0, 3.0, 0.9)
    c.stitch(NAVY, [(-0.064, 0.2270), (0.064, 0.2270)], 1.3, 4.0, 3.0, 0.9)
    # two creases under it, where he has been sitting on the jacket
    c.stroke(NAVY_DEEP, [(-0.050, 0.216), (-0.056, 0.198)], 2.6, 0.5, 0.85, (0.8, 0.3), curved=False)
    c.stroke(NAVY_DEEP, [(0.046, 0.216), (0.054, 0.200)], 2.6, 0.5, 0.85, (0.8, 0.3), curved=False)


def _torso_side(c):
    # the side seam, under the arm
    c.stroke(NAVY_DEEP, [(0.002, 0.262), (0.003, 0.200)], 2.4, 0.4, 0.9, (0.6, 0.8), curved=False)
    c.stitch(NAVY_LIT, [(0.010, 0.262), (0.011, 0.200)], 1.5, 4.5, 3.5, 0.8)


# ---------------------------------------------------------------------------
# THE ARMS. The jacket's sleeve all the way to the wrist (his shirt cuff is a block of its own
# and takes a flat white), then his fist. Everything past the wrist is skin.
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
    """`s` is +1 for his left arm. Each mark is cut to its own piece along the arm (x)."""
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    col(NAVY, 0.08, SLEEVE_END + 0.002)
    if view in ("front", "back"):
        c.mark(NAVY_LIT, [(x(0.118), 0.320), (x(0.150), 0.326), (x(0.150), 0.300), (x(0.118), 0.298)], 5, 0.55)
        c.mark(NAVY_LIT, [(x(0.190), 0.318), (x(0.216), 0.318), (x(0.216), 0.298), (x(0.190), 0.298)], 4, 0.45)
        # the armhole seam and the sewn line round the cuff end of the sleeve
        c.stitch(NAVY_LIT, [(x(0.112), 0.326), (x(0.110), 0.254)], 1.5, 4.5, 3.5, 0.8)
        c.stitch(NAVY_LIT, [(x(0.2165), 0.328), (x(0.2165), 0.250)], 1.4, 4.0, 3.0, 0.8)
        if view == "back":
            # two gold cuff buttons on the back of each sleeve
            for z in (0.300, 0.280):
                c.blob(NAVY_DEEP, (x(0.2055), z), 0.0058, 0.0058, 0.3, 1.0)
                c.blob(GOLD, (x(0.2055), z), 0.0044, 0.0044, 0.3, 1.0)
    elif view == "top":
        c.mark(NAVY_LIT, [(x(0.116), -0.028), (x(0.150), -0.034), (x(0.150), 0.044), (x(0.116), 0.038)], 6, 0.55)
        c.stitch(NAVY_LIT, [(x(0.111), -0.036), (x(0.111), 0.048)], 1.5, 4.5, 3.5, 0.8)
        c.stitch(NAVY_LIT, [(x(0.2165), -0.034), (x(0.2165), 0.046)], 1.4, 4.0, 3.0, 0.8)
    # everything past the sleeve is skin; the hand is drawn last so no cloth paint is left on it
    col(SKIN, SLEEVE_END + 0.002, 0.33)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Grey short trousers above; his black leather school shoe below. His bare knee, his
# socks, the shoe's toe, strap and sole are flat-toned blocks and take nothing from these islands.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for his left leg, -1 for his right. Each leg's marks are its own."""
    x = lambda v: s * v
    # the shoe first, then the shorts above it
    c.band(SHOE, 0.0, SHOE_TOP)
    if view == "back":
        # the heel counter: a stitched patch up the back of the shoe
        c.mark(SHOE_LIT, [(x(0.066), 0.058), (x(0.100), 0.058), (x(0.098), 0.032), (x(0.068), 0.032)], 0.4, 1.0, curved=False)
    elif view in ("xpos", "xneg"):
        # the side of the shoe: the shine of polished leather along the quarter and the seam
        # where the quarter is sewn to the vamp
        c.mark(SHOE_LIT, [(-0.020, 0.056), (0.046, 0.058), (0.050, 0.047), (-0.022, 0.045)], 1.6, 0.75)
        c.stroke(SHOE_DEEP, [(-0.038, 0.062), (-0.044, 0.046), (-0.060, 0.034)], 2.4, 0.3, 1.0, (0.9, 0.5))
        c.stitch(SHOE_LIT, [(-0.010, 0.032), (0.058, 0.032)], 1.3, 4.0, 3.0, 0.9)
    c.band(GREY, SHOE_TOP, 0.20)
    if view == "front":
        # pressed that morning: a sharp crease down the front of each leg, lit on one side of it
        c.mark(GREY_LIT, [(x(0.050), 0.170), (x(0.082), 0.170), (x(0.082), 0.120), (x(0.050), 0.120)], 3, 0.55)
        c.stroke(GREY_DARK, [(x(0.0845), 0.176), (x(0.0845), 0.116)], 2.2, 0.3, 1.0, (0.9, 0.9), curved=False)
    elif view == "back":
        c.stroke(GREY_DARK, [(x(0.0845), 0.176), (x(0.0845), 0.116)], 2.2, 0.3, 0.9, (0.9, 0.9), curved=False)
    elif view in ("xpos", "xneg"):
        # the side seam of the shorts, sewn
        c.stroke(GREY_DARK, [(0.000, 0.176), (0.001, 0.118)], 2.2, 0.3, 0.9, (0.8, 0.8), curved=False)
        c.stitch(GREY_LIT, [(0.008, 0.176), (0.009, 0.118)], 1.4, 4.0, 3.0, 0.8)


def paint_leg_top(c, s):
    """Looking down on a shoe: only its few level faces take this. Plain leather."""


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c

    c = I("torso.front", NAVY); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", NAVY); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", NAVY); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", NAVY); _torso_side(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SHOE)
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
    atlas = Image.new("RGB", (size, size), _rgb(NAVY_DEEP))
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
