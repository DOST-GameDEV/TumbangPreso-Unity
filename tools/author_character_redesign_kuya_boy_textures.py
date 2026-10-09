"""Paint the atlas of the Kuya Boy redesign, by hand, in the HEROES' style.

    py -3 tools/author_character_redesign_kuya_boy_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/kuya_boy/kuya_boy-redesign-atlas.png. The model
is built by tools/author_character_redesign_kuya_boy.py, which imports this file for the LAYOUT
only (PIL is imported inside the painting calls, Blender's Python has none) and so maps every
face onto the island painted for it here. Paint first, then build.

THE BRIEF (docs/reports/character-redesign/classic-brief.md): KUYA BOY REDRAWN AS IF HE WERE ONE
OF THE NINE HEROES. The same man, his colours and his recognisable pieces, in the heroes' visual
language. A copy of Bayan's textures script (an approved Classic, a man) rewritten for him.

WHO HE IS. "The cool big brother: laid back, leaning back a little, arms low and lazy, head
cocked" (GaitStyles.KuyaBoy). "Eldest of seven. He has been the defender since before he could
count" (ConvertedCharacterSelect). The heaviest hitter of the Classic twelve (LAKAS 5). The
original is character-male-b.glb in person_kuya_boy.asset: a shaved head, a big brown beard
with a moustache, big ears, something tucked behind his left ear, a navy collared shirt, a
brown belt, mustard shorts, brown footwear.

WHAT IS TAKEN FROM THE HEROES, with the numbers read off Dante's and Bayan's scripts:
  * INK 1a1420 (Dante's) for eyes and mouth;
  * EYES: solid ink blocks at the heroes' place (|x| 0.046 to 0.120, from z 0.455). HIS CUT IS A
    HALF-CLOSE: the ink stops at a level lid two thirds of the way up, and the lid itself is one
    flat step of skin shade above it. The lid falls a little toward the OUTER corner, the
    opposite of a scowl: he is relaxed, not cross;
  * MOUTH: one thin stroke, 7.4 mm, lifted at his left end: half a smile, in the window of skin
    between his moustache and his beard;
  * HIS HAIR IS HIS BEARD (his head is shaved, as the original's is). It is built in the model
    script the way the heroes' hair is, in three tones by facing plus a deep one for the mass
    underneath. On this island only the skin UNDER the beard is painted beard-dark, so no sliver
    of skin shows between its blocks. HIS BROWS are drawn here: one thick block each in the
    beard's colour, his left one cocked. A shaved head has no fringe to carry the expression;
  * SKIN AND CLOTH each have a base, a light, a shade and a deep tone.
No nose, no sockets, creases or wrinkles, no blush (he is male).

HIS COLOURS, each from his palette and given depth:
  * skin b0714a (slot 15), inside the heroes' range (Sean's is b87440);
  * beard: slot 13's brown made DARKER (5a2f1c) so it stands off his skin; the original's beard
    was a step off his skin tone and the two ran together;
  * his scalp's shadow: his skin taken a third of the way to his beard (855033);
  * shirt: slot 4's navy (284672), a collared polo with a cream and mustard
    band on each sleeve, a chest pocket, and a big 7 on the back: the eldest of seven;
  * shorts: slot 5's mustard (d8b04a), long walking shorts with a turned cuff and a cargo pocket;
  * belt and sandal straps: brown leather (slot 13's family); a steel buckle (slot 9);
  * a cream towel with two navy stripes over his right shoulder (slots 0 and 4);
  * a yellow carpenter's pencil behind his left ear (slot 2): the thing the original tucks there.

ROLE HUES (offence orange #f87020, defence blue #0080e8): the large cloth colours are checked in
`main`. His navy sits near the blue by hue, so its base is held under 0.45 in value and its
light tone under 0.45 in saturation. The beard and leather browns are held under 0.45 in value.
The mustard is 43 degrees, clear of the orange. His skin is exempt, as the cast's is. The
pencil's yellow is on one piece 12 mm across.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "kuya_boy"
ATLAS_NAME = "kuya_boy-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see `main`)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. Each family is a base, a light, a shade and a deep tone, as the heroes' are.
# ---------------------------------------------------------------------------
SKIN = "b0714a"; SKIN_LIT = "c4855a"; SKIN_SHADE = "935b39"; SKIN_DEEP = "71432a"       # his slot 15
BEARD = "5a2f1c"; BEARD_TOP = "723e24"; BEARD_DARK = "442214"; BEARD_DEEP = "2e160c"    # slot 13, darker
STUBBLE = "855033"; STUBBLE_TOP = "935c3c"; STUBBLE_DARK = "6f4129"                     # his shaved scalp: skin, a step toward the beard
INK = "1a1420"                                                                          # Dante's ink
SHIRT = "284672"; SHIRT_LIT = "536a90"; SHIRT_DARK = "1d3558"; SHIRT_DEEP = "142640"    # his slot 4
MUSTARD = "d8b04a"; MUSTARD_LIT = "e8c666"; MUSTARD_DARK = "b48c34"; MUSTARD_DEEP = "866a28"   # his slot 5
LEATHER = "6a3c22"; LEATHER_LIT = "72462a"; LEATHER_DARK = "4c2916"; LEATHER_DEEP = "341b0d"
STEEL = "868ba1"; STEEL_LIT = "b4b9cc"; STEEL_DARK = "5c6074"                           # his slot 9
DARK = "2b2f38"; DARK_LIT = "3d424e"; DARK_DEEP = "1b1e24"                              # his slot 11: soles
CREAM = "fde4c7"; CREAM_SHADE = "dcc0a0"; CREAM_DEEP = "b89a7a"                         # his slot 0
YELLOW = "ffc044"; YELLOW_DARK = "d0962a"; WOOD = "e8c9a0"                              # his slot 2: the pencil

# the large cloth colours, checked against the role hues in `main`
CLOTH_HEXES = [BEARD, BEARD_TOP, BEARD_DARK, BEARD_DEEP, SHIRT, SHIRT_LIT, SHIRT_DARK, SHIRT_DEEP,
               MUSTARD, MUSTARD_LIT, MUSTARD_DARK, MUSTARD_DEEP, LEATHER, LEATHER_LIT, LEATHER_DARK,
               DARK, DARK_LIT, CREAM, CREAM_SHADE, CREAM_DEEP]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# Blender space: x his left, -y the way he faces, z up.
# ---------------------------------------------------------------------------
BELT = (0.176, 0.208)           # the belt, at the foot of the shirt
ARM_Z = 0.2878                  # the arm bone's height
SLEEVE_END = 0.186              # along the arm (x): the short sleeve ends here
WRIST = 0.252                   # where the fist block starts
HAND_END = 0.310
SHORTS_HEM = 0.092              # the foot of his shorts
SOLE_TOP = 0.022                # the top of his sandal's footbed
TOWEL_X = (-0.134, -0.070)      # the towel over his right shoulder
POCKET = (0.052, 0.110, 0.244, 0.290)   # the chest pocket: x0, x1, z0, z1
#   THE BEARD ON THE FACE. The model script builds its blocks from these.
BEARD_LINE = 0.386              # the top of the jaw mass: the foot of the mouth's window
WINDOW = 0.050                  # the window of skin round his mouth reaches this far either side
CHEEK_TOP = 0.440               # the cheeks rise to here at the sides of the face
HAIRLINE = 0.580                # the foot of his shaved scalp's shadow across his brow

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
        "back": ((-0.17, 0.17, 0.15, 0.37), 1300),
        "xpos": ((-0.11, 0.11, 0.15, 0.37), 900),
        "xneg": ((-0.11, 0.11, 0.15, 0.37), 900),
    },
    "armL": {
        "front": ((0.09, 0.33, 0.19, 0.385), 1200),
        "back": ((0.09, 0.33, 0.19, 0.385), 1000),
        "top": ((0.09, 0.33, -0.10, 0.12), 1000),
        "bottom": ((0.09, 0.33, -0.10, 0.12), 700),
    },
    "armR": {
        "front": ((-0.33, -0.09, 0.19, 0.385), 1200),
        "back": ((-0.33, -0.09, 0.19, 0.385), 1000),
        "top": ((-0.33, -0.09, -0.10, 0.12), 1000),
        "bottom": ((-0.33, -0.09, -0.10, 0.12), 700),
    },
    # the shorts only: his shins and his sandals are loose pieces in flat tones
    "legL": {
        "front": ((0.0, 0.17, 0.07, 0.20), 1200),
        "back": ((0.0, 0.17, 0.07, 0.20), 1100),
        "xpos": ((-0.10, 0.11, 0.07, 0.20), 1100),
        "xneg": ((-0.10, 0.11, 0.07, 0.20), 800),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.07, 0.20), 1200),
        "back": ((-0.17, 0.0, 0.07, 0.20), 1100),
        "xpos": ((-0.10, 0.11, 0.07, 0.20), 800),
        "xneg": ((-0.10, 0.11, 0.07, 0.20), 1100),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group.
VIEW_BIAS = {}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high). u across, v UP.
SWATCHES = {
    "towel": (172, 300, 0.064, 0.112),      # the towel's front end, hanging on his chest
    "towel_back": (172, 220, 0.064, 0.082), # and its back end
    "cargo": (180, 150, 0.068, 0.056),      # the outer face of a cargo pocket
}

# FLAT TONES. One colour, no drawing: a face is sent to the middle of a 40 px square.
FLATS = {
    "beard_top": BEARD_TOP, "beard": BEARD, "beard_dark": BEARD_DARK, "beard_deep": BEARD_DEEP,
    "stubble_top": STUBBLE_TOP, "stubble": STUBBLE, "stubble_dark": STUBBLE_DARK,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "shirt_lit": SHIRT_LIT, "shirt": SHIRT, "shirt_dark": SHIRT_DARK, "shirt_deep": SHIRT_DEEP,
    "mustard_lit": MUSTARD_LIT, "mustard": MUSTARD, "mustard_dark": MUSTARD_DARK, "mustard_deep": MUSTARD_DEEP,
    "leather_lit": LEATHER_LIT, "leather": LEATHER, "leather_dark": LEATHER_DARK, "leather_deep": LEATHER_DEEP,
    "steel_lit": STEEL_LIT, "steel": STEEL, "steel_dark": STEEL_DARK,
    "dark_lit": DARK_LIT, "dark": DARK, "dark_deep": DARK_DEEP,
    "cream": CREAM, "cream_shade": CREAM_SHADE, "cream_deep": CREAM_DEEP,
    "yellow": YELLOW, "yellow_dark": YELLOW_DARK, "wood": WOOD, "ink": INK,
}
FLAT_PX = 40

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin. `head.front`, `skin`, and so on.

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
# HIS LEFT EYE (the right is its mirror). The heroes' block (Dante's: |x| 0.042 to 0.134, z 0.446
# to 0.512), HALF CLOSED: `EYE_BLOCK` is the whole upright block and is painted as his LID, one
# flat step of skin shade; `EYE_LEFT` is the ink, the lower two thirds of it, under a level lid
# that falls 7 mm toward the outer corner. Relaxed, a little sleepy, never cross.
EYE_BLOCK = [(0.046, 0.465), (0.052, 0.458), (0.116, 0.458), (0.122, 0.465), (0.122, 0.515), (0.116, 0.522), (0.052, 0.522), (0.046, 0.515)]
EYE_LEFT = [(0.046, 0.465), (0.052, 0.458), (0.116, 0.458), (0.122, 0.465), (0.122, 0.505), (0.046, 0.505)]
# HIS BROWS: one thick block each, in his beard's colour, on a head with no fringe to do the
# work. His RIGHT one lies level and low; his LEFT one is cocked, higher and tilted up at its
# outer end, over the lifted end of his mouth. (x, z) of each, in his own left and right.
BROW_RIGHT = [(-0.126, 0.5300), (-0.044, 0.5300), (-0.044, 0.5440), (-0.120, 0.5450), (-0.126, 0.5390)]
BROW_LEFT = [(0.042, 0.5350), (0.122, 0.5460), (0.128, 0.5530), (0.124, 0.5610), (0.046, 0.5500), (0.042, 0.5440)]
# HIS MOUTH: one thin stroke at the heroes' width (Dante's is 7.4 mm), lifted at his left end
MOUTH = [(-0.032, 0.4040), (-0.011, 0.4010), (0.012, 0.4030), (0.033, 0.4115)]
MOUTH_WIDTH = 7.4


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT: flat skin with one very soft lit patch, two half-closed ink
    # eyes under a flat lid, the mouth as one stroke. No nose, no sockets, creases or lip shadow.
    c.blob(SKIN_LIT, (0.0, 0.560), 0.120, 0.070, 30, 0.30)
    # under the beard's blocks the skin is beard-dark, so no sliver of skin shows between them:
    # the jaw below the mouth's window, and the cheek each side of it
    c.band(BEARD_DARK, 0.30, BEARD_LINE - 0.004)
    for s in (1, -1):
        c.mark(BEARD_DARK, [(s * (WINDOW + 0.006), 0.30), (s * (WINDOW + 0.006), BEARD_LINE + 0.004), (s * 0.104, CHEEK_TOP - 0.006),
                            (s * 0.26, CHEEK_TOP - 0.006), (s * 0.26, 0.30)], 0.0, 1.0, curved=False)
    # the eyes: the lid, then the ink under it
    for s in (1, -1):
        c.mark(SKIN_SHADE, [(s * x, z) for x, z in EYE_BLOCK], 0.3, 0.75, curved=False)
        c.mark(INK, [(s * x, z) for x, z in EYE_LEFT], 0.3, 1.0, curved=False)
    c.mark(BEARD, BROW_RIGHT, 0.3, 1.0, curved=False)
    c.mark(BEARD, BROW_LEFT, 0.3, 1.0, curved=False)
    # the shadow of his shaved scalp starts at HAIRLINE (the cap is a piece of its own; this is
    # only so no skin shows along its foot)
    c.band(STUBBLE, HAIRLINE + 0.004, 0.70)
    # the mouth: half a smile, one stroke and nothing under it
    c.stroke(INK, MOUTH, MOUTH_WIDTH, 0.3, 1.0, (0.5, 0.6))


# ---------------------------------------------------------------------------
# THE SHIRT. A navy polo, short sleeved, tucked into the belt: an open neck with a short
# placket and two cream buttons, a chest pocket with a mustard top edge on his left, and on the
# back a big mustard 7 with a cream edge (the eldest of seven). The collar wings, the belt, its
# loops and buckle and the towel are blocks; the belt takes its paint from these islands at
# the place it sits.
# ---------------------------------------------------------------------------

def _belt(c, a0, a1):
    c.band(LEATHER, BELT[0] - 0.004, BELT[1])
    c.band(LEATHER_LIT, BELT[1] - 0.0075, BELT[1] - 0.0050, 0.3, 0.9)
    c.band(LEATHER_DARK, BELT[0] - 0.004, BELT[0] + 0.0045, 0.4, 0.95)
    c.stitch(LEATHER_DEEP, [(a0, BELT[1] - 0.0115), (a1, BELT[1] - 0.0115)], 1.4, 5.0, 4.0)
    c.stitch(LEATHER_DEEP, [(a0, BELT[0] + 0.0095), (a1, BELT[0] + 0.0095)], 1.4, 5.0, 4.0)


def _shirt_shade(c, a0, a1):
    """The shirt's form: a shade along its foot, where it blouses over the belt."""
    c.mark(SHIRT_DARK, [(a0, BELT[1]), (a1, BELT[1]), (a1, BELT[1] + 0.020), (a0, BELT[1] + 0.020)], 5, 0.8, curved=False)


def paint_torso_front(c):
    c.mark(SHIRT_LIT, [(-0.070, 0.318), (0.040, 0.318), (0.038, 0.262), (-0.066, 0.262)], 10, 0.40)
    _shirt_shade(c, -0.2, 0.2)
    # the open neck: a V of his own skin, the shirt's turned edge round it
    c.mark(SHIRT_DEEP, [(-0.034, 0.352), (0.034, 0.352), (0.0, 0.2960)], 0.3, 1.0, curved=False)
    c.mark(SKIN_SHADE, [(-0.028, 0.352), (0.028, 0.352), (0.0, 0.3040)], 0.3, 1.0, curved=False)
    # the placket: a short darker strip under the V, a pale edge, two cream buttons, a bar tack
    c.mark(SHIRT_DARK, [(-0.0125, 0.298), (0.0125, 0.298), (0.0125, 0.250), (-0.0125, 0.250)], 0.3, 0.9, curved=False)
    c.stroke(SHIRT_DEEP, [(0.0125, 0.298), (0.0125, 0.250)], 1.6, 0.3, 1.0, (1.0, 1.0), curved=False)
    c.stroke(SHIRT_DEEP, [(-0.0125, 0.2500), (0.0125, 0.2500)], 2.0, 0.3, 1.0, (1.0, 1.0), curved=False)
    for z in (0.284, 0.264):
        c.blob(SHIRT_DEEP, (0.0, z), 0.0060, 0.0060, 0.25, 1.0)
        c.blob(CREAM, (0.0, z), 0.0040, 0.0040, 0.25, 1.0)
    # the chest pocket on his left: a hard outline, a paler face, a mustard top edge, a stitch row
    x0, x1, z0, z1 = POCKET
    mid = 0.5 * (x0 + x1)
    c.mark(SHIRT_DEEP, [(x0, z1), (x1, z1), (x1, z0 + 0.008), (mid, z0), (x0, z0 + 0.008)], 0.3, 1.0, curved=False)
    c.mark(SHIRT_LIT, [(x0 + 0.0024, z1 - 0.002), (x1 - 0.0024, z1 - 0.002), (x1 - 0.0024, z0 + 0.0094), (mid, z0 + 0.0028), (x0 + 0.0024, z0 + 0.0094)],
           0.3, 0.50, curved=False)
    c.mark(MUSTARD, [(x0, z1), (x1, z1), (x1, z1 - 0.0075), (x0, z1 - 0.0075)], 0.25, 1.0, curved=False)
    c.mark(MUSTARD_DARK, [(x0, z1 - 0.0060), (x1, z1 - 0.0060), (x1, z1 - 0.0080), (x0, z1 - 0.0080)], 0.25, 1.0, curved=False)
    c.stitch(SHIRT_DEEP, [(x0 + 0.006, z1 - 0.0135), (x1 - 0.006, z1 - 0.0135)], 1.3, 4.5, 3.5)
    # one fold on his left side, under the arm
    c.stroke(SHIRT_DARK, [(0.128, 0.300), (0.132, 0.268), (0.128, 0.236)], 3.2, 0.6, 0.95)
    _belt(c, -0.150, 0.150)


#   the 7, in the VIEWER'S frame from behind (u to the viewer's right, so u = -x)
SEVEN = [(-0.036, 0.316), (0.036, 0.316), (0.036, 0.298), (0.000, 0.230), (-0.024, 0.230), (0.010, 0.2975), (-0.036, 0.2975)]
SEVEN_MIDDLE = (0.004, 0.276)
SEVEN_SHIFT = -0.018     # toward his left, clear of the towel's end on his right shoulder


def paint_torso_back(c):
    c.mark(SHIRT_LIT, [(-0.070, 0.300), (0.070, 0.300), (0.066, 0.250), (-0.066, 0.250)], 10, 0.35)
    _shirt_shade(c, -0.2, 0.2)
    # the yoke: one seam across the shoulders with its row of stitches
    c.stroke(SHIRT_DEEP, [(-0.140, 0.330), (0.140, 0.330)], 1.8, 0.3, 1.0, (1.0, 1.0), curved=False)
    c.stitch(SHIRT_DARK, [(-0.135, 0.3255), (0.135, 0.3255)], 1.4, 4.5, 3.5)
    # THE 7: a cream edge, the mustard figure inside it
    grow = lambda k: [(-(SEVEN_SHIFT + SEVEN_MIDDLE[0] + (u - SEVEN_MIDDLE[0]) * k), SEVEN_MIDDLE[1] + (z - SEVEN_MIDDLE[1]) * k) for u, z in SEVEN]
    edge = 0.0036
    for dx, dz in ((edge, 0), (-edge, 0), (0, edge), (0, -edge), (edge * 0.7, edge * 0.7), (-edge * 0.7, edge * 0.7),
                   (edge * 0.7, -edge * 0.7), (-edge * 0.7, -edge * 0.7)):
        c.mark(CREAM, [(x + dx, z + dz) for x, z in grow(1.0)], 0.25, 1.0, curved=False)
    c.mark(MUSTARD, grow(1.0), 0.25, 1.0, curved=False)
    _belt(c, -0.150, 0.150)


def _torso_side(c):
    _shirt_shade(c, -0.2, 0.2)
    # the side seam, from the armhole to the belt
    c.stroke(SHIRT_DEEP, [(-0.001, 0.300), (0.000, 0.214)], 2.0, 0.4, 0.95, (0.7, 0.9), curved=False)
    c.mark(SHIRT_LIT, [(-0.070, 0.262), (-0.020, 0.262), (-0.018, 0.236), (-0.072, 0.236)], 7, 0.35)
    _belt(c, -0.100, 0.100)


# ---------------------------------------------------------------------------
# THE ARMS. A short navy sleeve to 0.176 with a cream and mustard band at its mouth (blocks),
# then his BARE arm in his own skin: forearm, fist. Along the arm (x): elbow 0.180, fist 0.240
# to 0.298.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    # the fist's flat faces only: x 0.253 to 0.285 along the arm
    if view in ("front", "back"):
        # three finger lines across the end of the fist, and the thumb folded over them: one hook
        for z in (ARM_Z + 0.0225, ARM_Z, ARM_Z - 0.0225):
            c.stroke(SKIN_DEEP, [(s * (WRIST + 0.029), z), (s * (WRIST + 0.046), z)], 2.6, 0.3, 0.95, (1.0, 0.5), curved=False)
        c.stroke(SKIN_DEEP, [(s * (WRIST + 0.010), ARM_Z + 0.0280), (s * (WRIST + 0.020), ARM_Z + 0.0300), (s * (WRIST + 0.027), ARM_Z + 0.0390)],
                 2.6, 0.3, 0.95, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * (WRIST + 0.014), -0.026), (s * (WRIST + 0.046), -0.026), (s * (WRIST + 0.046), 0.038), (s * (WRIST + 0.014), 0.038)], 5, 0.6)


def paint_arm(c, s, view):
    """`s` is +1 for his left arm. Each mark is cut to its own piece along the arm (x)."""
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    # the sleeve
    col(SHIRT, 0.08, SLEEVE_END + 0.002)
    if view in ("front", "back"):
        c.mark(SHIRT_LIT, [(x(0.158), 0.338), (x(0.170), 0.340), (x(0.170), 0.308), (x(0.158), 0.306)], 4, 0.45)
        c.mark(SHIRT_DARK, [(x(0.08), 0.18), (x(0.190), 0.18), (x(0.190), 0.250), (x(0.08), 0.254)], 2.0, 0.7, curved=False)
    elif view == "top":
        c.mark(SHIRT_LIT, [(x(0.158), -0.034), (x(0.172), -0.036), (x(0.172), 0.046), (x(0.158), 0.044)], 5, 0.45)
    elif view == "bottom":
        col(SHIRT_DARK, 0.08, SLEEVE_END + 0.002, 0, 0.8)
    # his bare arm: skin from the sleeve out, drawn last so no cloth paint is left on it
    col(SKIN, SLEEVE_END + 0.002, 0.40)
    if view in ("front", "back"):
        # the forearm's form: lit along its upper half, a shade under it
        c.mark(SKIN_LIT, [(x(0.200), 0.328), (x(0.248), 0.326), (x(0.248), 0.300), (x(0.200), 0.302)], 5, 0.5)
        c.mark(SKIN_SHADE, [(x(0.188), 0.18), (x(0.258), 0.18), (x(0.258), 0.254), (x(0.188), 0.252)], 2.5, 0.6, curved=False)
    elif view == "top":
        c.mark(SKIN_LIT, [(x(0.200), -0.026), (x(0.250), -0.024), (x(0.250), 0.036), (x(0.200), 0.038)], 5, 0.5)
    elif view == "bottom":
        col(SKIN_SHADE, SLEEVE_END + 0.002, 0.40, 0, 0.85)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE SHORTS. Mustard walking shorts to below the knee, a turned cuff (a block), a cargo
# pocket on the outside of each leg (a block with its own swatch). The islands carry the
# shorts only: his shins and his sandals are loose pieces in flat tones.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for his left leg, -1 for his right. Each leg's marks are its own."""
    x = lambda v: s * v
    # the leg darkens toward the cuff, on every side
    c.band(MUSTARD_DARK, SHORTS_HEM, SHORTS_HEM + 0.030, 4, 0.55)
    if view == "front":
        c.mark(MUSTARD_LIT, [(x(0.050), 0.172), (x(0.076), 0.172), (x(0.076), 0.132), (x(0.050), 0.132)], 4, 0.6)
        # the crease pressed down the front of the leg
        c.stroke(MUSTARD_DEEP, [(x(0.0835), 0.178), (x(0.0840), 0.106)], 2.2, 0.4, 0.9, (0.6, 1.0), curved=False)
    elif view == "back":
        if s < 0:
            # one patch pocket on the seat, his right: a hard outline, a paler face, a button
            px0, px1 = sorted((x(0.058), x(0.110)))
            mid = 0.5 * (px0 + px1)
            c.mark(MUSTARD_DEEP, [(px0, 0.172), (px1, 0.172), (px1, 0.132), (mid, 0.123), (px0, 0.132)], 0.3, 1.0, curved=False)
            c.mark(MUSTARD_LIT, [(px0 + 0.0024, 0.1696), (px1 - 0.0024, 0.1696), (px1 - 0.0024, 0.1336), (mid, 0.1258), (px0 + 0.0024, 0.1336)],
                   0.3, 0.75, curved=False)
            c.stitch(MUSTARD_DEEP, [(px0 + 0.005, 0.1630), (px1 - 0.005, 0.1630)], 1.4, 4.5, 3.5)
            c.blob(MUSTARD_DEEP, (mid, 0.1560), 0.0052, 0.0052, 0.25, 1.0)
            c.blob(CREAM_SHADE, (mid, 0.1560), 0.0034, 0.0034, 0.25, 1.0)
        else:
            c.stroke(MUSTARD_DEEP, [(x(0.060), 0.166), (x(0.106), 0.166)], 2.2, 0.3, 0.9, (0.8, 0.8), curved=False)
    else:
        # the side seam, a row of stitches beside it
        c.stroke(MUSTARD_DEEP, [(0.004, 0.186), (0.005, 0.098)], 2.0, 0.4, 0.9, (0.8, 1.0), curved=False)
        c.stitch(MUSTARD_LIT, [(-0.002, 0.182), (-0.001, 0.102)], 1.3, 4.5, 3.5, 0.8)


# ---------------------------------------------------------------------------
# THE SWATCHES. u across, v up, both 0 to 1.
# ---------------------------------------------------------------------------

def paint_towel(c, back=False):
    # the towel: cream terry, two navy stripes above a fringed end, a soft fold down its length
    c.stroke(CREAM_SHADE, [(0.30, 0.98), (0.27, 0.55), (0.31, 0.24)], 3.0, 0.6, 0.85, (0.4, 0.6))
    low = 0.30 if back else 0.22
    c.mark(SHIRT, [(0.0, low), (1.0, low), (1.0, low - 0.075), (0.0, low - 0.075)], 0.25, 1.0, curved=False)
    c.mark(SHIRT, [(0.0, low - 0.105), (1.0, low - 0.105), (1.0, low - 0.135), (0.0, low - 0.135)], 0.25, 1.0, curved=False)
    # the fringe: a darker foot cut into tassels
    c.mark(CREAM_SHADE, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.055), (0.0, 0.055)], 0.3, 1.0, curved=False)
    for k in range(1, 7):
        u = k / 7.0
        c.stroke(CREAM_DEEP, [(u, 0.0), (u, 0.052)], 1.3, 0.2, 1.0, (1.0, 1.0), curved=False)


def paint_cargo(c):
    # a cargo pocket's outer face: a flap over the top third, its edge one hard line, a button
    c.mark(MUSTARD_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.66), (0.0, 0.66)], 0.3, 0.9, curved=False)
    c.mark(MUSTARD_DEEP, [(0.0, 0.66), (1.0, 0.66), (1.0, 0.615), (0.0, 0.615)], 0.3, 1.0, curved=False)
    c.stitch(MUSTARD_DEEP, [(0.07, 0.90), (0.93, 0.90)], 1.3, 4.5, 3.5)
    c.stroke(MUSTARD_DARK, [(0.5, 0.58), (0.5, 0.06)], 2.4, 0.4, 0.9, (0.9, 0.5), curved=False)
    c.blob(MUSTARD_DEEP, (0.5, 0.775), 0.074, 0.090, 0.2, 1.0)
    c.blob(CREAM_SHADE, (0.5, 0.775), 0.048, 0.058, 0.2, 1.0)


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
            c = I(group + "." + view, MUSTARD)
            paint_leg(c, s, view)
            done[c.name] = c

    c = swatch("towel", CREAM); paint_towel(c); done[c.name] = c
    c = swatch("towel_back", CREAM); paint_towel(c, back=True); done[c.name] = c
    c = swatch("cargo", MUSTARD); paint_cargo(c); done[c.name] = c
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
    atlas = Image.new("RGB", (size, size), _rgb(BEARD_DEEP))
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
