"""Paint the atlas of the Totoy redesign PROTOTYPE, by hand, in the heroes' style.

    py -3 tools/author_character_redesign_totoy_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/totoy/totoy-redesign-atlas.png. The model is
built by tools/author_character_redesign_totoy.py, which imports this file for the LAYOUT only
(PIL is imported inside the painting calls, Blender's Python has none) and so maps every face
onto the island painted for it here. Paint first, then build.

WHY. Owner, 2026-10-06: the twelve Classic street characters are redrawn AS IF THEY WERE HEROES
(docs/CHARACTER_REDESIGN_DANTE.md section 15.9 part D). Bayan, Bebang and Lola Pacing were the
pattern and were approved; this is Totoy, built on Bebang's files. His face is DRAWN THE WAY THE
HEROES' FACES ARE, not measured off the original, and his colours are given the heroes' depth.
The model script's docstring has the whole brief. Nothing in the game loads these files and
character-male-a.glb is not touched.

THE RULES HE INHERITS (section 15.3):
  * the face is drawn for the flat front of the head: flat skin, the fringe's shadow as one
    tone, one stroke for a mouth. No nose, no sockets, creases, lids, lip shadow or contour, and
    no blush (he is a boy). HIS EYES ARE DRAWN ON HIS LENSES (see `paint_lens`): his big round
    glasses are the first thing anyone sees of him on the original, so they are kept, as two
    simple octagons, and the heroes' ink eye blocks are seen through them;
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points;
  * PAINT STOPS AT ITS OWN PIECE'S EDGES. Every band below is cut to the piece that wears it;
  * A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW. The model script sends every chamfer
    and angled face to a FLAT tone (`proj_square` there).

HIS COLOURS start from HIS palette (Resources/Roster/person_totoy.asset): skin 724530 (slot 14),
shirt green 2f7d4f (1), mustard d8b04a (2, his shoes on the original), slate navy 3b4252 (5, his
shorts), the glasses' orange e0702c (11), white (12), cream fde4c7 (0) and red cf534f (4, one
rubber band). His hair was a dark grey 38383d (8) on the original; black hair is BLACK in the
heroes' hand, so it takes Dante's values.
ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth colour is checked below.
The ORANGE OF HIS GLASSES sits on the offence hue and is kept off that list on purpose: it is
his, and it is on two rims 9 mm wide, a bridge and two temples, nothing larger (rule: "off large
areas"). Skin is exempt, as it is for the cast.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "totoy"
ATLAS_NAME = "totoy-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. His, at the heroes' richness: each family starts from his palette's slot and is
# given the depth a hero's has (a light, a base, a shade, a deep).
# ---------------------------------------------------------------------------
SKIN = "74462c"; SKIN_LIT = "8a5636"; SKIN_SHADE = "5d3622"; SKIN_DEEP = "452617"      # slot 14, his deep brown
HAIR = "1a181e"; HAIR_TOP = "2b2933"; HAIR_DEEP = "0e0d12"                              # Dante's black
HAIR_BUZZ = "3a2a24"                                                                      # the clipped nape: black hair cut to the skin
INK = "1a1420"                                                                            # Dante's ink
SHIRT = "2f7d4f"; SHIRT_LIT = "469b69"; SHIRT_MID = "276b43"; SHIRT_DEEP = "1c5033"     # slot 1, his green
GOLD = "d8b04a"; GOLD_LIT = "edcb72"; GOLD_DARK = "ad8730"                                # slot 2, mustard: the shirt's trim, his slippers
NAVY = "3b4252"; NAVY_LIT = "525c74"; NAVY_DARK = "2b313f"                                # slot 5, his shorts
WHITE = "f8f3e8"; WHITE_SHADE = "ddd5c6"; WHITE_DEEP = "bdb4a4"                          # slots 12 and 0, a warm white
LENS = "ecf3f3"; LENS_EDGE = "c3d2d6"                                                     # slot 6's pale blue, nearly white: his lenses
ORANGE = "e0702c"; ORANGE_LIT = "f08c4c"; ORANGE_DARK = "b4531e"                          # slot 11, the rims of his glasses ONLY
RED = "cf534f"; RED_DARK = "a53c3a"                                                       # slot 4, one rubber band

# the orange of his glasses is ON the offence hue and is kept off this list (see the docstring)
CLOTH_HEXES = [HAIR, HAIR_TOP, HAIR_DEEP, HAIR_BUZZ, SHIRT, SHIRT_LIT, SHIRT_MID, SHIRT_DEEP, GOLD, GOLD_LIT, GOLD_DARK, NAVY, NAVY_LIT,
               NAVY_DARK, WHITE, WHITE_SHADE, WHITE_DEEP, LENS, LENS_EDGE, RED, RED_DARK]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# ---------------------------------------------------------------------------
SLEEVE_END = 0.178              # along the arm (x): the mouth of his sleeve
WRIST = 0.240                   # where the fist block starts
HAND_END = 0.296
SHORTS_HEM = 0.094              # the foot of his long shorts, at the knee
SOLE_TOP = 0.021                # the top of his slipper's sole
LENS_R = 0.050                  # half the width of a lens
LENS_AT = (0.081, 0.492)        # the middle of his LEFT lens on the face (x, z); the eye sits in it
TOE_OUT = 4.0                   # degrees his feet are turned out

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

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). His two lenses:
# each is a flat octagon standing off the face, and the eye seen through it is drawn on it.
SWATCHES = {
    "lensL": (170, 170, 2 * LENS_R, 2 * LENS_R),
    "lensR": (170, 170, 2 * LENS_R, 2 * LENS_R),
}

# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    "hair_top": HAIR_TOP, "hair": HAIR, "hair_under": HAIR_DEEP, "hair_buzz": HAIR_BUZZ,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "shirt_lit": SHIRT_LIT, "shirt": SHIRT, "shirt_mid": SHIRT_MID, "shirt_deep": SHIRT_DEEP,
    "gold_lit": GOLD_LIT, "gold": GOLD, "gold_dark": GOLD_DARK,
    "navy_lit": NAVY_LIT, "navy": NAVY, "navy_dark": NAVY_DARK,
    "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "lens": LENS, "lens_edge": LENS_EDGE,
    "orange_lit": ORANGE_LIT, "orange": ORANGE, "orange_dark": ORANGE_DARK,
    "red": RED, "red_dark": RED_DARK, "ink": INK,
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
#     53 by 67 mm centred at (0.076, 0.481), Cheska's the same size at (0.072, 0.487). The only
#     expression in them is ONE CUT (Amihan's top edge slants; Cheska's foot has a notch;
#     Bebang's top edge slants hard; Bayan's too; Lola Pacing's are shut in two arcs);
#   * MOUTH: one curved stroke about 8 mm wide, about 60 mm across;
#   * ink 181418 to 1f1c24; nothing else.
# HIS: the same block, 52 by 64 mm, centred in each lens at (0.081, 0.492). HIS CUT is the FOOT
# of the block, slanted 11 mm UP toward the outside, the way a cheek pushes the outer corner of
# an eye up when a kid grins: wide open and cheeky at once. Nobody else in the row has it
# (Bebang, Bayan and Amihan are cut along the TOP, which reads as a brow; Lola Pacing's are
# shut). (v01 arched the foot in the middle and each eye read as a tooth.)
# HIS MOUTH is the heroes' stroke drawn as a wide open-cornered grin, 76 mm across, level: it
# sits lower than theirs (0.409 against 0.420) because his glasses take the room above it.
#   his LEFT eye about the middle of its lens, +x toward his left ear (the outside)
EYE = [(-0.020, 0.032), (0.020, 0.032), (0.026, 0.026), (0.026, -0.0185), (0.021, -0.0225), (-0.020, -0.032), (-0.026, -0.026), (-0.026, 0.026)]
GRIN = [(-0.0380, 0.4215), (-0.0250, 0.4090), (0.0, 0.4040), (0.0250, 0.4090), (0.0380, 0.4215)]
MOUTH_WIDTH = 8.2                # mm; Amihan's is 8.6, Dante's 7.4
#   the fringe's shadow: (from x, to x, the height it falls to), under each lock's own tip
FRINGE_SHADOW = [(-0.200, -0.156, 0.556), (-0.146, -0.086, 0.584), (-0.040, 0.052, 0.590), (0.092, 0.154, 0.576), (0.160, 0.200, 0.560)]


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT: flat skin with one very soft lit patch on the chin and
    # cheeks, the fringe's shadow as ONE flat tone, his mouth as one stroke. His eyes are on his
    # lenses. No nose, no blush, no sockets, creases, lids, lip shadow or contour.
    c.blob(SKIN_LIT, (0.0, 0.415), 0.120, 0.050, 22, 0.30)
    for x0, x1, z in FRINGE_SHADOW:
        c.mark(SKIN_SHADE, [(x0, 0.72), (x1, 0.72), (x1, z), (x0, z)], 1.2, 0.70, curved=False)
    c.stroke(INK, GRIN, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, GRIN[0], 0.0030, 0.0030, 0.25, 1.0)
    c.blob(INK, GRIN[-1], 0.0030, 0.0030, 0.25, 1.0)


def paint_lens(c, s):
    """One lens, seen from the front: (0..1, 0..1) is the square the octagon is cut from.
    `s` is +1 for his left; the right eye is its mirror."""
    at = lambda p: (0.5 + s * p[0] / (2.0 * LENS_R), 0.5 + p[1] / (2.0 * LENS_R))
    c.mark(INK, [at(p) for p in EYE], 0.25, 1.0, curved=False)


# ---------------------------------------------------------------------------
# THE SHIRT. Green, a liga jersey tee a size too big: the mustard V neck, the sleeve bands and
# the hem are geometry; what is drawn here is the cloth, the way the heroes' cloth is drawn (a
# lit patch, a fold or two, rows of stitches where it is sewn) and HIS NUMBER, 5: small on his
# left chest, big across his back. (Five is his speed in the roster, the top of the scale.)
# Every mark keeps clear of the block's chamfers (14 mm): those take a flat tone.
# ---------------------------------------------------------------------------

def _five(c, cx, cz, h, colour, width, mirror=1.0):
    """The numeral 5, `h` metres tall, centred on (cx, cz): a bar, a stem and a bowl.
    `mirror` -1 draws it for the BACK island, which is seen from behind."""
    k = h / 0.076
    at = lambda x, z: (cx + mirror * x * k, cz + z * k)
    full = (1.0, 1.0)
    c.stroke(colour, [at(0.021, 0.036), at(-0.019, 0.036)], width, 0.25, 1.0, full, curved=False)
    c.stroke(colour, [at(-0.019, 0.036), at(-0.021, 0.004)], width, 0.25, 1.0, full, curved=False)
    c.stroke(colour, [at(-0.021, 0.004), at(-0.004, 0.009), at(0.014, 0.004), at(0.023, -0.012), at(0.015, -0.029), at(-0.003, -0.036),
                      at(-0.022, -0.030)], width, 0.25, 1.0, full)
    for x, z in ((0.021, 0.036), (-0.019, 0.036), (-0.021, 0.004), (-0.022, -0.030)):
        p = at(x, z)
        c.blob(colour, p, 0.0005 * width, 0.0005 * width, 0.25, 1.0)


def paint_torso_front(c):
    c.mark(SHIRT_LIT, [(-0.104, 0.304), (-0.060, 0.300), (-0.056, 0.236), (-0.106, 0.232)], 9, 0.55)
    c.mark(SHIRT_LIT, [(0.050, 0.236), (0.106, 0.234), (0.104, 0.212), (0.048, 0.214)], 6, 0.40)
    # a fold from each armpit toward the waist: the shirt is too big for him and hangs
    c.stroke(SHIRT_DEEP, [(-0.108, 0.296), (-0.092, 0.262), (-0.088, 0.222)], 4.4, 0.7, 0.85, (0.3, 0.8))
    c.stroke(SHIRT_DEEP, [(0.108, 0.296), (0.096, 0.266), (0.100, 0.226)], 4.2, 0.7, 0.85, (0.3, 0.8))
    c.stroke(SHIRT_DEEP, [(-0.030, 0.258), (-0.020, 0.236), (-0.024, 0.212)], 3.4, 0.7, 0.75, (0.3, 0.8))
    # the shoulder seams, running out to the sleeves
    c.stitch(SHIRT_DEEP, [(-0.108, 0.320), (-0.070, 0.326)], 1.5, 4.5, 3.5)
    c.stitch(SHIRT_DEEP, [(0.070, 0.326), (0.108, 0.320)], 1.5, 4.5, 3.5)
    # his number on his left chest: mustard, on a darker patch so it reads on the green
    _five(c, 0.066, 0.268, 0.050, SHIRT_DEEP, 11.5)
    _five(c, 0.066, 0.268, 0.050, GOLD_LIT, 7.0)


def paint_torso_back(c):
    c.mark(SHIRT_LIT, [(-0.096, 0.312), (0.096, 0.312), (0.090, 0.286), (-0.092, 0.284)], 8, 0.50)
    # the yoke across his shoulders, sewn
    c.stroke(SHIRT_DEEP, [(-0.108, 0.316), (0.0, 0.310), (0.108, 0.316)], 2.4, 0.3, 0.9, (0.9, 0.9))
    c.stitch(SHIRT_LIT, [(-0.104, 0.322), (0.0, 0.316), (0.104, 0.322)], 1.5, 4.5, 3.5, 0.8)
    # HIS NUMBER, big: white with a mustard face, the way a liga jersey's is pressed on
    _five(c, 0.0, 0.256, 0.086, SHIRT_DEEP, 22.0, -1.0)
    _five(c, 0.0, 0.256, 0.086, WHITE, 17.0, -1.0)
    _five(c, 0.0, 0.256, 0.086, GOLD, 9.5, -1.0)


def _torso_side(c):
    # the side seam, under the arm
    c.stroke(SHIRT_DEEP, [(0.002, 0.262), (0.003, 0.218)], 2.4, 0.4, 0.9, (0.6, 0.8), curved=False)
    c.stitch(SHIRT_DEEP, [(0.010, 0.262), (0.011, 0.218)], 1.5, 4.5, 3.5)


# ---------------------------------------------------------------------------
# THE ARMS. A green sleeve (its mustard band and white edge are geometry), a bare forearm, his
# fist. Everything past the sleeve's mouth is skin.
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
    col(SHIRT, 0.08, SLEEVE_END + 0.004)
    if view in ("front", "back"):
        c.mark(SHIRT_LIT, [(x(0.118), 0.322), (x(0.148), 0.330), (x(0.148), 0.300), (x(0.118), 0.298)], 5, 0.55)
        c.stroke(SHIRT_DEEP, [(x(0.114), 0.300), (x(0.130), 0.272), (x(0.148), 0.252)], 3.4, 0.6, 0.85, (0.3, 0.8))
        # the armhole seam
        c.stitch(SHIRT_DEEP, [(x(0.112), 0.330), (x(0.110), 0.250)], 1.5, 4.5, 3.5)
    elif view == "top":
        c.mark(SHIRT_LIT, [(x(0.116), -0.030), (x(0.148), -0.036), (x(0.148), 0.046), (x(0.116), 0.040)], 6, 0.55)
        c.stitch(SHIRT_DEEP, [(x(0.111), -0.040), (x(0.111), 0.050)], 1.5, 4.5, 3.5)
    # everything past the sleeve is skin; the hand is drawn last so no cloth paint is left on it
    col(SKIN, SLEEVE_END + 0.004, 0.33)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Only his shorts take these islands: slate navy basketball shorts down to the knee,
# a white stripe with a mustard line beside it down the OUTSIDE of each leg. His bare shins, his
# feet and his slippers are flat-toned blocks.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for his left leg, -1 for his right. Each leg's marks are its own."""
    x = lambda v: s * v
    c.band(NAVY, 0.0, 0.20)
    if view == "front":
        c.mark(NAVY_LIT, [(x(0.060), 0.166), (x(0.112), 0.166), (x(0.112), 0.124), (x(0.060), 0.124)], 5, 0.45)
        # a crease from the hip
        c.stroke(NAVY_DARK, [(x(0.050), 0.172), (x(0.060), 0.146), (x(0.058), 0.122)], 3.0, 0.6, 0.8, (0.3, 0.8))
    elif view == "back":
        # the back pocket: a stitched square
        c.stitch(NAVY_LIT, [(x(0.060), 0.162), (x(0.108), 0.162)], 1.4, 4.0, 3.0, 0.8)
        c.stitch(NAVY_LIT, [(x(0.060), 0.162), (x(0.060), 0.128)], 1.4, 4.0, 3.0, 0.8)
        c.stitch(NAVY_LIT, [(x(0.108), 0.162), (x(0.108), 0.128)], 1.4, 4.0, 3.0, 0.8)
        c.stitch(NAVY_LIT, [(x(0.060), 0.128), (x(0.108), 0.128)], 1.4, 4.0, 3.0, 0.8)
    elif view in ("xpos", "xneg"):
        outer = (view == "xpos") == (s > 0)
        if outer:
            c.mark(WHITE, [(-0.016, 0.20), (0.008, 0.20), (0.008, 0.09), (-0.016, 0.09)], 0.3, 1.0, curved=False)
            c.mark(GOLD, [(0.014, 0.20), (0.021, 0.20), (0.021, 0.09), (0.014, 0.09)], 0.3, 1.0, curved=False)
        else:
            c.stitch(NAVY_DARK, [(0.004, 0.176), (0.005, 0.110)], 1.4, 4.0, 3.0)


def paint_leg_top(c, s):
    """Nothing of his faces up on these islands."""


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    for name, s in (("lensL", 1), ("lensR", -1)):
        c = I(name, LENS, (2 * LENS_R, 2 * LENS_R)); paint_lens(c, s); done[c.name] = c

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
            c = I(group + "." + view, NAVY)
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
    atlas = Image.new("RGB", (size, size), _rgb(SHIRT_DEEP))
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