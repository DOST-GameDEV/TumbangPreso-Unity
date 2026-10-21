"""Paint the atlas of the Ate Girlie redesign PROTOTYPE, by hand, in the heroes' style.

    py -3 tools/author_character_redesign_ate_girlie_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/ate_girlie/ate_girlie-redesign-atlas.png. The
model is built by tools/author_character_redesign_ate_girlie.py, which imports this file for the
LAYOUT only (PIL is imported inside the painting calls, Blender's Python has none) and so maps
every face onto the island painted for it here. Paint first, then build.

WHY. Owner, 2026-10-06: the Classic street characters are redrawn as if they were heroes
(docs/reports/character-redesign/classic-brief.md). Her face is DRAWN THE WAY THE HEROES' FACES
ARE and not measured off the original (see EYE_LEFT), and her colours are given the heroes'
depth. The model script's docstring has the whole brief. Nothing in the game loads these files
and character-female-b.glb is not touched.

THE RULES SHE INHERITS (section 15.3 of docs/CHARACTER_REDESIGN_DANTE.md):
  * the face is drawn for the flat front of the head: flat skin, the fringe's shadow as one
    tone, a round blush under each eye (she is a girl), two ink eyes, one stroke for a mouth.
    No nose, no sockets, creases, lids, lip shadow or contour;
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points;
  * PAINT STOPS AT ITS OWN PIECE'S EDGES. Every band below is cut to the piece that wears it;
  * A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW. The model script sends every chamfer
    and angled face to a FLAT tone (`proj_square` there), so nothing drawn here is ever smeared
    down a slope. That is why every island below keeps its marks away from the piece's edges.

HER COLOURS start from HER palette (Resources/Roster/person_ate_girlie.asset): skin f7c9a6
(slot 15), hair e8a07a (13), rose d94f6a (2), denim 2f5a7a (7), navy 22283a (8), white (12),
and three slots no triangle of the original wore, used here on small things: mint 61cb8b (1)
for her garter, her bun ties and a rubber band; sky d0e8ff (6) for the garter's other bands;
grey 868ba1 (9) as the silver of one jeans button. Each family is then given a hero's depth.
ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth and hair colour is
checked below. Two of her own colours sit beside a role hue and were moved for it: her hair
(beside the orange) is a muted strawberry auburn, and her jeans (beside the blue) are a faded
wash. Her SKIN is exempt, as it is for the cast.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "ate_girlie"
ATLAS_NAME = "ate_girlie-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. Hers, at the heroes' richness: each family starts from her palette's slot and is
# given the depth a hero's has (a light, a base, a shade, a deep).
# ---------------------------------------------------------------------------
SKIN = "f5c3a0"; SKIN_LIT = "fbd6b8"; SKIN_SHADE = "e3a585"; SKIN_DEEP = "bf7f60"      # slot 15, fair and warm
BLUSH = "ee7a78"
# slot 13 (e8a07a, the peach the original's hair wore, one step off its skin) made a real hair
# colour: a strawberry auburn, redder and deeper than her skin so the two never read as one, and
# kept under 0.45 saturation in its mid tones because by hue it sits beside the offence orange
HAIR_TOP = "e2a892"; HAIR_LIT = "cb8a76"; HAIR = "b07263"; HAIR_DARK = "8c594e"; HAIR_DEEP = "66362b"
INK = "1c161a"                                                                            # the heroes' near-black ink
BLOUSE = "d94f6a"; BLOUSE_LIT = "ee7a8e"; BLOUSE_MID = "c2405b"; BLOUSE_DEEP = "9f2f4a"  # slot 2, rose
ROSE = "c93f5c"; ROSE_LIT = "ea6f86"; ROSE_MID = "ab3049"; ROSE_DEEP = "86233a"          # her headband, a step deeper than the blouse
# slot 7 (2f5a7a) as FADED denim: by hue it is the defence blue's neighbour, so the mid tones
# are washed out under 0.45 saturation and the dark ones sit under 0.45 value
DENIM = "4d6c88"; DENIM_LIT = "6f8ca6"; DENIM_PALE = "a9bccb"; DENIM_MID = "44617b"; DENIM_DARK = "2f4a63"; DENIM_DEEP = "22364a"
NAVY = "262c42"; NAVY_LIT = "3d4666"; NAVY_DEEP = "171b2b"                                # slot 8, her shoes
WHITE = "ffffff"; WHITE_SHADE = "e4e0da"; WHITE_DEEP = "c2bdb6"                          # slot 12
MINT = "61cb8b"; MINT_LIT = "93e2b0"; MINT_DARK = "3fa56a"                                # slot 1, on small things only
SKY = "d0e8ff"; SKY_DARK = "a3c2e0"                                                       # slot 6, on rubber bands only
SILVER = "c9ccd6"; SILVER_LIT = "eceef4"; SILVER_DARK = "8f93a6"                          # slot 9, one jeans button

CLOTH_HEXES = [HAIR_TOP, HAIR_LIT, HAIR, HAIR_DARK, HAIR_DEEP, BLOUSE, BLOUSE_LIT, BLOUSE_MID, BLOUSE_DEEP, ROSE, ROSE_LIT, ROSE_MID,
               ROSE_DEEP, DENIM, DENIM_LIT, DENIM_PALE, DENIM_MID, DENIM_DARK, DENIM_DEEP, NAVY, NAVY_LIT, NAVY_DEEP, WHITE, WHITE_SHADE,
               WHITE_DEEP, MINT, MINT_LIT, MINT_DARK, SKY, SKY_DARK, SILVER, SILVER_LIT, SILVER_DARK]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES, so a band painted here lands on the block built there.
# ---------------------------------------------------------------------------
SLEEVE_END = 0.178              # along the arm (x): the mouth of the bell sleeve
WRIST = 0.240                   # where the fist block starts
HAND_END = 0.296
BLOUSE_HEM = 0.250              # the foot of her cropped blouse
WAIST_TOP = 0.226               # the top of her jeans
JEANS_HEM = 0.058               # the foot of the rolled hem
SOLE_TOP = 0.022
SHOE_TOP = 0.0595               # below this the leg islands are her shoe; above, her jeans

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
        "front": ((-0.17, 0.17, 0.15, 0.36), 1300),
        "back": ((-0.17, 0.17, 0.15, 0.36), 1000),
        "xpos": ((-0.11, 0.11, 0.15, 0.36), 900),
        "xneg": ((-0.11, 0.11, 0.15, 0.36), 900),
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
        "back": ((0.0, 0.17, 0.0, 0.19), 900),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 950),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 800),
        "top": ((0.0, 0.17, -0.16, 0.11), 800),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.19), 1000),
        "back": ((-0.17, 0.0, 0.0, 0.19), 900),
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
    "rose_lit": ROSE_LIT, "rose": ROSE, "rose_mid": ROSE_MID, "rose_deep": ROSE_DEEP,
    "denim_lit": DENIM_LIT, "denim": DENIM, "denim_pale": DENIM_PALE, "denim_mid": DENIM_MID, "denim_dark": DENIM_DARK, "denim_deep": DENIM_DEEP,
    "navy_lit": NAVY_LIT, "navy": NAVY, "navy_deep": NAVY_DEEP,
    "white": WHITE, "white_shade": WHITE_SHADE, "white_deep": WHITE_DEEP,
    "mint_lit": MINT_LIT, "mint": MINT, "mint_dark": MINT_DARK,
    "sky": SKY, "sky_dark": SKY_DARK,
    "silver_lit": SILVER_LIT, "silver": SILVER, "silver_dark": SILVER_DARK, "ink": INK,
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
#     Bebang's top slants 10 mm down to the inner corner; Lola Pacing's are closed arcs);
#   * MOUTH: one curved stroke 8.6 mm wide, about 60 mm across, centred near 0.420;
#   * a round blush under each eye on the girls; nothing else.
# HERS: the same block, 52 by 62 mm at (0.077, 0.481), its top edge LEVEL. HER CUT is the OUTER
# TOP CORNER, drawn out sideways into one short lash. (v01 let the whole top edge rise to that
# point and she scowled: an edge that slants down to the nose is Bebang's and Bayan's cut and
# reads as set brows.) Level, with the one lash, it reads as bright, neat and a little knowing:
# the big sister who has already seen what you are about to do. (The original's eyes flare at that same
# outer corner; this is that, said in the heroes' block.) HER MOUTH is the heroes' stroke as a
# small closed smile, 54 mm across and level, both corners the same: composed, not a grin.
EYE_CENTRE = (0.077, 0.481)
EYE_LEFT = [(0.057, 0.4500), (0.097, 0.4500), (0.103, 0.4560), (0.103, 0.4985), (0.1170, 0.5165), (0.0990, 0.5120),
            (0.0570, 0.5120), (0.0510, 0.5060), (0.0510, 0.4560)]
SMILE = [(-0.0270, 0.4245), (-0.0150, 0.4170), (0.0, 0.4145), (0.0150, 0.4170), (0.0270, 0.4245)]
MOUTH_WIDTH = 8.6                # mm, the heroes' stroke
#   the fringe's shadow: (from x, to x, the height it falls to), under each lock's own tip
FRINGE_SHADOW = [(-0.200, -0.150, 0.530), (-0.150, -0.086, 0.558), (-0.086, -0.004, 0.580), (-0.004, 0.126, 0.600), (0.126, 0.200, 0.566)]


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT: flat skin with one very soft lit patch, the fringe's shadow as
    # ONE flat tone, a round blush under each eye, her two ink eyes, her mouth as one stroke.
    # No nose, no sockets, creases, lids, lip shadow or contour.
    c.blob(SKIN_LIT, (0.0, 0.455), 0.118, 0.080, 26, 0.30)
    shadow = [(-0.26, 0.72), (0.26, 0.72)]
    for x0, x1, z in reversed(FRINGE_SHADOW):
        shadow += [(x1, z), (x0, z)]
    c.mark(SKIN_SHADE, shadow, 1.4, 0.62, curved=False)
    c.blob(BLUSH, (-0.108, 0.426), 0.030, 0.019, 7, 0.55)
    c.blob(BLUSH, (0.108, 0.426), 0.030, 0.019, 7, 0.55)
    c.mark(INK, EYE_LEFT, 0.25, 1.0, curved=False)
    c.mark(INK, [(-x, z) for x, z in EYE_LEFT], 0.25, 1.0, curved=False)
    c.stroke(INK, SMILE, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, SMILE[0], 0.0028, 0.0028, 0.25, 1.0)
    c.blob(INK, SMILE[-1], 0.0028, 0.0028, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE TORSO. Above BLOUSE_HEM the rose blouse; below WAIST_TOP her jeans' seat (the bare waist
# between them, the hem's roll and knot, the V neck, the waistband, its loops and button are
# geometry and take nothing from here). Drawn the way the heroes' cloth is: a lit patch, a fold
# or two, rows of stitches where it is sewn. Every mark keeps clear of the blocks' chamfers.
# ---------------------------------------------------------------------------
JEANS_TOP_PAINT = 0.234


def _seat(c, view):
    c.band(DENIM, 0.10, JEANS_TOP_PAINT)
    if view == "front":
        # the fly: a sewn J and the seam under it
        c.stroke(DENIM_DARK, [(0.0, 0.204), (0.0, 0.166)], 2.2, 0.3, 0.95, (0.9, 0.9), curved=False)
        c.stitch(DENIM_PALE, [(0.012, 0.204), (0.013, 0.184), (0.004, 0.172)], 1.4, 4.0, 3.0, 0.8)
        # the mouth of each front pocket, a curve from the waistband to the side seam
        for s in (1, -1):
            c.stroke(DENIM_DARK, [(s * 0.050, 0.204), (s * 0.072, 0.190), (s * 0.108, 0.184)], 2.4, 0.3, 0.95, (0.9, 0.7))
            c.stitch(DENIM_PALE, [(s * 0.047, 0.199), (s * 0.070, 0.184), (s * 0.106, 0.178)], 1.4, 4.0, 3.0, 0.8)
    elif view == "back":
        # the yoke: a shallow V under the waistband
        c.stroke(DENIM_DARK, [(-0.116, 0.200), (0.0, 0.188), (0.116, 0.200)], 2.4, 0.3, 0.95, (0.9, 0.9), curved=False)
        c.stitch(DENIM_PALE, [(-0.114, 0.195), (0.0, 0.183), (0.114, 0.195)], 1.4, 4.0, 3.0, 0.8)
    else:
        c.stroke(DENIM_DARK, [(0.002, 0.204), (0.003, 0.160)], 2.2, 0.3, 0.9, (0.8, 0.8), curved=False)


def paint_torso_front(c):
    c.mark(BLOUSE_LIT, [(-0.112, 0.306), (-0.066, 0.300), (-0.062, 0.268), (-0.114, 0.264)], 9, 0.55)
    c.mark(BLOUSE_LIT, [(0.058, 0.286), (0.114, 0.284), (0.116, 0.266), (0.058, 0.266)], 6, 0.40)
    # a fold from each armpit toward the knot
    c.stroke(BLOUSE_DEEP, [(-0.120, 0.298), (-0.100, 0.282), (-0.070, 0.272)], 4.0, 0.7, 0.80, (0.3, 0.8))
    c.stroke(BLOUSE_DEEP, [(0.120, 0.300), (0.104, 0.284), (0.076, 0.272)], 4.0, 0.7, 0.80, (0.3, 0.8))
    c.stroke(BLOUSE_DEEP, [(0.006, 0.284), (0.018, 0.276), (0.024, 0.270)], 3.0, 0.7, 0.75, (0.3, 0.8))
    # the shoulder seams, running out to the sleeves
    c.stitch(BLOUSE_DEEP, [(-0.120, 0.320), (-0.072, 0.326)], 1.5, 4.5, 3.5)
    c.stitch(BLOUSE_DEEP, [(0.072, 0.326), (0.120, 0.320)], 1.5, 4.5, 3.5)
    # THE SEWN FLOWER on her right chest: five white petals round a mint eye, one leaf
    fx, fz = -0.084, 0.288
    for k in range(5):
        a = math.radians(90 + 72 * k)
        c.blob(WHITE, (fx + 0.0100 * math.cos(a), fz + 0.0100 * math.sin(a)), 0.0062, 0.0062, 0.3, 1.0)
    c.blob(MINT, (fx, fz), 0.0048, 0.0048, 0.3, 1.0)
    c.mark(MINT_DARK, [(fx + 0.012, fz - 0.014), (fx + 0.026, fz - 0.016), (fx + 0.020, fz - 0.026), (fx + 0.010, fz - 0.022)], 0.3, 1.0)
    _seat(c, "front")


def paint_torso_back(c):
    c.mark(BLOUSE_LIT, [(-0.098, 0.308), (0.098, 0.308), (0.094, 0.270), (-0.094, 0.268)], 9, 0.50)
    # the back opening: a short slit from the neck with one loop and a white button
    c.stroke(BLOUSE_DEEP, [(0.0, 0.330), (0.0, 0.292)], 2.6, 0.3, 0.95, (0.9, 0.5), curved=False)
    c.blob(BLOUSE_DEEP, (0.0, 0.318), 0.0078, 0.0078, 0.3, 1.0)
    c.blob(WHITE, (0.0, 0.318), 0.0058, 0.0058, 0.3, 1.0)
    # two soft folds falling from the shoulder blades to the hem
    c.stroke(BLOUSE_DEEP, [(-0.060, 0.302), (-0.066, 0.284), (-0.062, 0.272)], 3.4, 0.7, 0.75, (0.3, 0.8))
    c.stroke(BLOUSE_DEEP, [(0.060, 0.302), (0.066, 0.284), (0.062, 0.272)], 3.4, 0.7, 0.75, (0.3, 0.8))
    _seat(c, "back")


def _torso_side(c):
    # the side seam, under the arm
    c.stroke(BLOUSE_DEEP, [(0.002, 0.286), (0.003, 0.270)], 2.4, 0.4, 0.9, (0.6, 0.8), curved=False)
    _seat(c, "side")


# ---------------------------------------------------------------------------
# THE ARMS. A rose bell sleeve (its turned band and white piping are geometry), a bare forearm,
# her fist. Everything past the sleeve's mouth is skin.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    # the fist block's flat faces only: x 0.253 to 0.283 along the arm, z 0.246 to 0.330
    if view in ("front", "back"):
        for z in (0.3050, 0.2880, 0.2710):
            c.stroke(SKIN_DEEP, [(s * 0.2640, z), (s * 0.2815, z)], 2.2, 0.3, 0.85, (1.0, 0.5), curved=False)
        # the thumb, folded along the top of the fist: one drawn hook
        c.stroke(SKIN_DEEP, [(s * 0.2540, 0.3200), (s * 0.2660, 0.3220), (s * 0.2730, 0.3280)], 2.2, 0.3, 0.85, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.256, -0.024), (s * 0.280, -0.024), (s * 0.280, 0.036), (s * 0.256, 0.036)], 4, 0.5)


def paint_arm(c, s, view):
    """`s` is +1 for her left arm. Each mark is cut to its own piece along the arm (x)."""
    x = lambda v: s * v
    col = lambda colour, a, b, f=0.0, k=1.0: c.column(colour, *sorted((x(a), x(b))), f, k)
    col(BLOUSE, 0.08, SLEEVE_END + 0.004)
    if view in ("front", "back"):
        c.mark(BLOUSE_LIT, [(x(0.118), 0.324), (x(0.150), 0.334), (x(0.150), 0.302), (x(0.118), 0.298)], 5, 0.55)
        # two gathers fanning from the shoulder into the flare
        c.stroke(BLOUSE_DEEP, [(x(0.112), 0.292), (x(0.130), 0.270), (x(0.152), 0.250)], 3.2, 0.6, 0.80, (0.3, 0.8))
        c.stroke(BLOUSE_DEEP, [(x(0.114), 0.306), (x(0.134), 0.300), (x(0.154), 0.290)], 2.6, 0.6, 0.65, (0.3, 0.8))
        # the armhole seam
        c.stitch(BLOUSE_DEEP, [(x(0.111), 0.328), (x(0.110), 0.252)], 1.5, 4.5, 3.5)
    elif view == "top":
        c.mark(BLOUSE_LIT, [(x(0.116), -0.030), (x(0.150), -0.036), (x(0.150), 0.046), (x(0.116), 0.040)], 6, 0.55)
        c.stitch(BLOUSE_DEEP, [(x(0.111), -0.038), (x(0.111), 0.048)], 1.5, 4.5, 3.5)
    # everything past the sleeve is skin; the hand is drawn last so no cloth paint is left on it
    col(SKIN, SLEEVE_END + 0.004, 0.33)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Her jeans above; her navy strap shoe below. The rolled hem, the sock, the strap,
# its button, the instep and the sole are flat-toned blocks and take nothing from these islands.
# ---------------------------------------------------------------------------

def paint_leg(c, s, view):
    """`s` is +1 for her left leg, -1 for her right. Each leg's marks are its own."""
    x = lambda v: s * v
    c.band(NAVY, 0.0, SHOE_TOP)
    if view == "back":
        # the heel counter: a paler navy tab up the back of the shoe
        c.mark(NAVY_LIT, [(x(0.070), 0.054), (x(0.097), 0.054), (x(0.097), 0.028), (x(0.070), 0.028)], 0.3, 1.0, curved=False)
    elif view in ("xpos", "xneg"):
        # the side of the shoe: a line of stitches above the sole and a paler toe cap seam
        c.stitch(NAVY_LIT, [(-0.100, 0.028), (0.060, 0.028)], 1.4, 4.0, 3.0, 0.9)
        c.stroke(NAVY_LIT, [(-0.104, 0.040), (-0.098, 0.032), (-0.100, 0.024)], 2.0, 0.3, 0.9, (0.8, 0.6))
    c.band(DENIM, SHOE_TOP, 0.20)
    if view == "front":
        # the faded thigh and the crease pressed down the front of the leg
        c.mark(DENIM_LIT, [(x(0.060), 0.162), (x(0.108), 0.162), (x(0.110), 0.106), (x(0.060), 0.104)], 7, 0.60)
        c.stroke(DENIM_PALE, [(x(0.084), 0.150), (x(0.085), 0.092)], 2.4, 0.9, 0.45, (0.3, 0.3), curved=False)
        # two whiskers at the hip
        c.stroke(DENIM_DARK, [(x(0.056), 0.168), (x(0.076), 0.160), (x(0.096), 0.160)], 2.2, 0.5, 0.70, (0.4, 0.3))
    elif view == "back":
        # HER BACK POCKET: a five-sided patch, stitched round, with a sewn chevron
        px = 0.084
        pocket = [(x(px - 0.030), 0.170), (x(px + 0.030), 0.170), (x(px + 0.030), 0.128), (x(px), 0.114), (x(px - 0.030), 0.128)]
        c.mark(DENIM_MID, pocket, 0.3, 1.0, curved=False)
        ring = pocket + [pocket[0]]
        for a, b in zip(ring, ring[1:]):
            c.stroke(DENIM_DARK, [a, b], 2.0, 0.3, 0.95, (1.0, 1.0), curved=False)
        inner = [(x(px - 0.024), 0.164), (x(px - 0.024), 0.131), (x(px), 0.121), (x(px + 0.024), 0.131), (x(px + 0.024), 0.164)]
        for a, b in zip(inner, inner[1:]):
            c.stitch(DENIM_PALE, [a, b], 1.3, 3.6, 2.8, 0.85)
        c.stitch(DENIM_PALE, [(x(px - 0.022), 0.152), (x(px), 0.142), (x(px + 0.022), 0.152)], 1.3, 3.6, 2.8, 0.85)
    elif view in ("xpos", "xneg"):
        # the side seam of the jeans, sewn
        c.stroke(DENIM_DARK, [(0.000, 0.178), (0.001, 0.086)], 2.2, 0.3, 0.9, (0.8, 0.8), curved=False)
        c.stitch(DENIM_PALE, [(0.008, 0.178), (0.009, 0.086)], 1.4, 4.0, 3.0, 0.8)


def paint_leg_top(c, s):
    """Looking down on a shoe: only its few level faces take this. Plain navy."""


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
