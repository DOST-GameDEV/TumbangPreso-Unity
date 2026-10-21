"""Paint the atlas of the Aling Nena redesign PROTOTYPE, by hand, in the heroes' style.

    py -3 tools/author_character_redesign_aling_nena_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/aling_nena/aling_nena-redesign-atlas.png. The model is
built by tools/author_character_redesign_aling_nena.py, which imports this file for the LAYOUT only
(PIL is imported inside the painting calls, Blender's Python has none) and so maps every face
onto the island painted for it here. Paint first, then build.

WHY. Owner, 2026-10-06: the Classic street characters are REDRAWN AS IF THEY WERE HEROES
(docs/reports/character-redesign/classic-brief.md). Her face is drawn the way the heroes' faces
are, her black hair is the heroes' black, and her colours are given the heroes' depth. The model
script's docstring has the whole design. Nothing in the game loads these files and
character-female-e.glb is not touched. Started as a copy of Bebang's textures script (the
approved pattern nearest her), rewritten for her.

THE RULES SHE INHERITS (section 15.3 of docs/CHARACTER_REDESIGN_DANTE.md):
  * the face is drawn for the flat front of the head: flat skin, the fringe's shadow as one
    tone, a round blush under each eye (she is a woman), two ink eyes, one stroke for a mouth.
    No nose, no sockets, creases, lids, lip shadow or contour, and NOTHING OF AGE;
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points;
  * PAINT STOPS AT ITS OWN PIECE'S EDGES. Every band below is cut to the piece that wears it;
  * A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW. The model script sends every chamfer
    and angled face to a FLAT tone (`proj_square` there).

HER COLOURS start from HER palette (Resources/Roster/person_aling_nena.asset) as the original
wears it: skin d99a6c (slot 15), hair 31212b (8, a plum black, here the heroes' BLACK), her
white jacket (12), the bottle green of her top and her soles 3a5c4a (11), the dark of her
trousers (8, here a plum of its own so they are not the hair), the brown of her two buttons
b9714a (13), and the orange e07a3a (5) the original's hands wore. Slot 2's gold ffc044, which no
triangle of the original wore, is on her earrings, her bangle and her pencil.
ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth colour is checked below.
Her ORANGE is one: on the original it covered both hands and forearms. Here it is kept as her
accent and taken OFF large areas: her scrunchie, the cover of the notebook in her pocket and the
strap stripe of each slipper, each under 60 mm across. The gold and the brown sit near it by hue
too and are on pieces under 30 mm. Her SKIN is exempt, as it is for the cast.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "aling_nena"
ATLAS_NAME = "aling_nena-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. Hers, at the heroes' richness: each family starts from her palette's slot and is
# given the depth a hero's has (a light, a base, a shade, a deep).
# ---------------------------------------------------------------------------
SKIN = "d99a6c"; SKIN_LIT = "e7b085"; SKIN_SHADE = "ba7b52"; SKIN_DEEP = "935a3b"      # slot 15; within a few points of Amihan's
BLUSH = "dd7466"
HAIR_TOP = "403c4a"; HAIR_LIT = "2b2833"; HAIR = "1a181e"; HAIR_DARK = "121016"; HAIR_DEEP = "0c0b10"   # the heroes' black (Dante's 1a181e)
INK = "1a1420"                                                                            # Dante's ink
WHITE = "fdfbf6"; WHITE_LIT = "ffffff"; WHITE_SHADE = "e6e1d6"; WHITE_DEEP = "c4bdb0"    # slot 12, her blouse
GREEN = "3f6f56"; GREEN_LIT = "5b9474"; GREEN_MID = "376149"; GREEN_DARK = "2c4f3c"; GREEN_DEEP = "1f3a2c"   # slot 11, bottle green
MINT = "8fd3a8"; MINT_SHADE = "6cb489"                                                    # slot 1, the piping and trims on the green
PLUM = "4a3442"; PLUM_LIT = "634859"; PLUM_DARK = "372530"; PLUM_DEEP = "261822"          # slot 8, her trousers
ORANGE = "e07a3a"; ORANGE_LIT = "f09a5c"; ORANGE_DARK = "b85c24"                          # slot 5, small things only
GOLD = "ffc044"; GOLD_LIT = "ffdc88"; GOLD_DARK = "c88e24"                                # slot 2
BROWN = "b9714a"; BROWN_LIT = "d08e66"; BROWN_DARK = "8e5232"                             # slot 13, her two buttons
CREAM = "fde4c7"; CREAM_SHADE = "e3c39c"                                                  # slot 0, the pages of her notebook

# the orange, the gold and the brown sit near the offence orange by hue and are kept off this
# list: each is on pieces under 60 mm across (rule 10: "off large areas"; see the docstring)
CLOTH_HEXES = [HAIR_TOP, HAIR_LIT, HAIR, HAIR_DARK, HAIR_DEEP, WHITE, WHITE_SHADE, WHITE_DEEP, GREEN, GREEN_LIT, GREEN_MID,
               GREEN_DARK, GREEN_DEEP, MINT, MINT_SHADE, PLUM, PLUM_LIT, PLUM_DARK, PLUM_DEEP, CREAM, CREAM_SHADE]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# ---------------------------------------------------------------------------
SLEEVE_END = 0.176              # along the arm (x): the mouth of her short sleeve
WRIST = 0.240                   # where the fist block starts
HAND_END = 0.296
WAIST = 0.214                   # the apron's band
TAIL_HEM = 0.160                # the foot of her blouse's tail
APRON_HEM = 0.102               # the foot of her apron
TROUSER_HEM = 0.058             # the foot of her trousers
SOLE_TOP = 0.020

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
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.19), 1000),
        "back": ((-0.17, 0.0, 0.0, 0.19), 800),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 800),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 950),
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
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "white_lit": WHITE_LIT, "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "green_lit": GREEN_LIT, "green": GREEN, "green_mid": GREEN_MID, "green_dark": GREEN_DARK, "green_deep": GREEN_DEEP,
    "mint": MINT, "mint_shade": MINT_SHADE,
    "plum_lit": PLUM_LIT, "plum": PLUM, "plum_dark": PLUM_DARK, "plum_deep": PLUM_DEEP,
    "orange_lit": ORANGE_LIT, "orange": ORANGE, "orange_dark": ORANGE_DARK,
    "gold_lit": GOLD_LIT, "gold": GOLD, "gold_dark": GOLD_DARK,
    "brown_lit": BROWN_LIT, "brown": BROWN, "brown_dark": BROWN_DARK,
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
# What the redesigned heroes share, read off their texture scripts:
#   * EYES: two solid ink blocks, upright, corners cut 6 mm; Amihan's are 53 by 67 mm centred at
#     (0.076, 0.481). The only expression in them is ONE CUT;
#   * MOUTH: one curved stroke 8.6 mm wide, about 60 mm across, centred near 0.420;
#   * a round blush under each eye on the women; nothing else.
# HERS: the same two blocks at the same place, width and foot, and HER CUT IS ONE LID. Her right
# eye is the heroes' whole block, 52 by 64 mm. Her LEFT eye has its top taken off level, 24 mm
# down: one eye narrowed at you. It is the look across the counter of a woman who has heard
# this excuse before, and with the corner of her mouth pulled up on that same side it is half
# a wink. Nobody else in the cast has two eyes of different heights by a lid (Dante's differ by
# colour and a scar; Bebang's and Bayan's lids slant, Lola Pacing's arch, Phaister's are slits).
# (v01 and v02 took the top third off BOTH: two small dashes, sleepy and blank. v03 let both
# lids fall to the outer corner: sad and worried. v04 cut both level at 53 mm: calm, but beside
# Amihan it was Amihan's face.)
# HER MOUTH is the heroes' stroke as a small sure smile, 60 mm across, its corner on her LEFT
# pulled higher: she has already decided the call.
EYE_CENTRE = (0.077, 0.482)
EYE_RIGHT = [(0.097, 0.5140), (0.103, 0.5080), (0.103, 0.4560), (0.097, 0.4500), (0.057, 0.4500), (0.051, 0.4560), (0.051, 0.5080), (0.057, 0.5140)]
EYE_LEFT = [(0.103, 0.4900), (0.103, 0.4560), (0.097, 0.4500), (0.057, 0.4500), (0.051, 0.4560), (0.051, 0.4900)]
SMILE = [(-0.0290, 0.4200), (-0.0150, 0.4140), (0.0030, 0.4125), (0.0190, 0.4170), (0.0310, 0.4285)]
MOUTH_WIDTH = 8.6                # mm, the heroes' stroke
#   the fringe's shadow: (from x, to x, the height it falls to), under each lock's own tip. Her
#   hair is parted over her LEFT eye and swept to her right, so the shadow steps down that way.
FRINGE_SHADOW = [(-0.200, -0.150, 0.520), (-0.150, -0.080, 0.560), (-0.080, 0.006, 0.588), (0.006, 0.090, 0.610), (0.090, 0.140, 0.640),
                 (0.140, 0.200, 0.580)]


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (rule 12): flat skin with one very soft lit patch, the fringe's
    # shadow as ONE flat tone, a round blush under each eye, her two ink eyes, her mouth as one
    # stroke. No nose, no sockets, creases, lids, lip shadow or contour. Nothing of her age.
    c.blob(SKIN_LIT, (0.0, 0.455), 0.118, 0.080, 26, 0.30)
    shadow = [(-0.26, 0.72), (0.26, 0.72)]
    for x0, x1, z in reversed(FRINGE_SHADOW):
        shadow += [(x1, z), (x0, z)]
    c.mark(SKIN_SHADE, shadow, 1.4, 0.66, curved=False)
    c.blob(BLUSH, (-0.110, 0.424), 0.031, 0.020, 7, 0.55)
    c.blob(BLUSH, (0.110, 0.424), 0.031, 0.020, 7, 0.55)
    c.mark(INK, EYE_LEFT, 0.25, 1.0, curved=False)
    c.mark(INK, [(-x, z) for x, z in EYE_RIGHT], 0.25, 1.0, curved=False)
    c.stroke(INK, SMILE, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, SMILE[0], 0.0028, 0.0028, 0.25, 1.0)
    c.blob(INK, SMILE[-1], 0.0028, 0.0028, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE BLOUSE. White. Its front bands, their mint piping, the green top in its opening, the
# buttons, the tail and the apron are geometry; what is drawn here is the cloth itself, the way
# the heroes' cloth is drawn: a soft shade low on each side, a fold or two, rows of stitches.
# Every mark keeps clear of the block's chamfers (14 mm): those take a flat tone.
# ---------------------------------------------------------------------------

def paint_torso_front(c):
    # white cloth is drawn with its SHADE, not a lit patch: under each arm and above the apron
    c.mark(WHITE_SHADE, [(-0.132, 0.270), (-0.094, 0.262), (-0.086, 0.222), (-0.132, 0.220)], 7, 0.75)
    c.mark(WHITE_SHADE, [(0.132, 0.270), (0.096, 0.264), (0.090, 0.222), (0.132, 0.220)], 7, 0.75)
    # a fold from each armpit toward the waist
    c.stroke(WHITE_DEEP, [(-0.124, 0.294), (-0.106, 0.264), (-0.098, 0.230)], 3.6, 0.7, 0.80, (0.3, 0.8))
    c.stroke(WHITE_DEEP, [(0.124, 0.294), (0.110, 0.268), (0.106, 0.232)], 3.4, 0.7, 0.80, (0.3, 0.8))
    # a breast pocket on her right, sewn: three sides of stitches and a turned top
    c.stroke(WHITE_DEEP, [(-0.108, 0.296), (-0.072, 0.296)], 2.6, 0.3, 0.85, (0.9, 0.9), curved=False)
    c.stitch(GREEN_LIT, [(-0.108, 0.290), (-0.108, 0.262), (-0.090, 0.254), (-0.072, 0.262), (-0.072, 0.290)], 1.5, 4.5, 3.5, 0.85)
    # the shoulder seams, running out to the sleeves
    c.stitch(WHITE_DEEP, [(-0.126, 0.318), (-0.078, 0.324)], 1.5, 4.5, 3.5)
    c.stitch(WHITE_DEEP, [(0.078, 0.324), (0.126, 0.318)], 1.5, 4.5, 3.5)


def paint_torso_back(c):
    c.mark(WHITE_SHADE, [(-0.128, 0.250), (0.128, 0.250), (0.128, 0.218), (-0.128, 0.218)], 8, 0.70)
    # the yoke across her shoulders, sewn in green thread, and the box pleat under it
    c.stroke(WHITE_DEEP, [(-0.124, 0.304), (0.0, 0.296), (0.124, 0.304)], 2.6, 0.3, 0.9, (0.9, 0.9))
    c.stitch(GREEN_LIT, [(-0.120, 0.311), (0.0, 0.303), (0.120, 0.311)], 1.5, 4.5, 3.5, 0.85)
    c.stroke(WHITE_DEEP, [(-0.016, 0.292), (-0.019, 0.224)], 2.6, 0.4, 0.9, (0.9, 0.6), curved=False)
    c.stroke(WHITE_DEEP, [(0.016, 0.292), (0.019, 0.224)], 2.6, 0.4, 0.9, (0.9, 0.6), curved=False)


def _torso_side(c):
    # the side seam, under the arm
    c.stroke(WHITE_DEEP, [(0.002, 0.262), (0.003, 0.222)], 2.4, 0.4, 0.9, (0.6, 0.8), curved=False)
    c.stitch(WHITE_DEEP, [(0.010, 0.262), (0.011, 0.222)], 1.5, 4.5, 3.5)


# ---------------------------------------------------------------------------
# THE ARMS. A short white sleeve (its green band and mint piping are geometry), a bare forearm,
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
    col(WHITE, 0.08, SLEEVE_END + 0.004)
    if view in ("front", "back"):
        c.mark(WHITE_SHADE, [(x(0.114), 0.268), (x(0.152), 0.262), (x(0.152), 0.238), (x(0.114), 0.240)], 5, 0.75)
        c.stroke(WHITE_DEEP, [(x(0.114), 0.302), (x(0.130), 0.276), (x(0.150), 0.258)], 3.2, 0.6, 0.80, (0.3, 0.8))
        # the armhole seam
        c.stitch(WHITE_DEEP, [(x(0.112), 0.330), (x(0.110), 0.250)], 1.5, 4.5, 3.5)
    elif view == "top":
        c.stitch(WHITE_DEEP, [(x(0.111), -0.040), (x(0.111), 0.050)], 1.5, 4.5, 3.5)
    elif view == "bottom":
        col(WHITE_SHADE, 0.08, SLEEVE_END + 0.004)
    # everything past the sleeve is skin; the hand is drawn last so no cloth paint is left on it
    col(SKIN, SLEEVE_END + 0.004, 0.33)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Her plum trousers. Her ankles, her feet and her slippers are flat-toned blocks and
# take nothing from these islands.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for her left leg, -1 for her right. Each leg's marks are its own."""
    x = lambda v: s * v
    if view == "front":
        # the pressed crease down the front of each leg, and a lit strip beside it
        c.mark(PLUM_LIT, [(x(0.060), 0.150), (x(0.078), 0.150), (x(0.078), 0.076), (x(0.060), 0.076)], 4, 0.55)
        c.stroke(PLUM_DARK, [(x(0.084), 0.170), (x(0.084), 0.072)], 2.4, 0.3, 0.9, (0.8, 0.8), curved=False)
    elif view == "back":
        c.stroke(PLUM_DARK, [(x(0.084), 0.170), (x(0.084), 0.072)], 2.4, 0.3, 0.9, (0.8, 0.8), curved=False)
    elif view in ("xpos", "xneg"):
        # the side seam, sewn
        c.stroke(PLUM_DARK, [(0.000, 0.176), (0.001, 0.074)], 2.2, 0.3, 0.9, (0.8, 0.8), curved=False)
        c.stitch(PLUM_LIT, [(0.008, 0.176), (0.009, 0.074)], 1.4, 4.0, 3.0, 0.8)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

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
            c = I(group + "." + view, PLUM)
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
