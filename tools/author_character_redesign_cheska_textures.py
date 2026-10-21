"""Paint the atlas of the Cheska (displayed: Yasmin) redesign PROTOTYPE, by hand, in the house style.

    py -3 tools/author_character_redesign_cheska_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/cheska/cheska-redesign-atlas.png. The model is
built by tools/author_character_redesign_cheska.py, which imports this file for the LAYOUT only
(PIL is imported inside the brush, Blender's Python has none) and so maps every face onto the
island painted for it here. Paint first, then build.

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 13: the owner asked for the rest of the cast to
follow Dante's rework, one hero in its own files. This is her copy of Dante's textures script,
rewritten for her. Nothing in the game loads these files and team-cheska.glb is not touched.

WHO SHE IS, read off team-cheska.glb and its recipe (build_person_voxel.py) BEFORE anything was
drawn: the smallest kid of the cast in a padded cyan trapper hat with white fur, a pompom and
ski goggles pushed up on its brim; black hair cut in a stepped fringe and hanging in one long
blunt sheet down her back, its ends dipped in frost; two plain black eyes and a cat's "w" of a
mouth; a white short sleeved shirt under cyan overall shorts with a bib pocket, a wooden spatula
handle tucked in it, a bow at the neck; one long striped sock and one short one; white and cyan
sneakers; a sweatband on her left wrist. EVERY PIECE AND EVERY COLOUR BELOW IS ONE OF THOSE.

⚠️ HER COLOURS ARE HERS AND STAY. docs/CAST_CLOTHING_STYLE.md: the owner threw out a dark
recolour of her (*"u changed cheskas colors i dont like it"*). So this hero does NOT take the
cast's dark base garment: she stays pale ice, white and cyan, and the base tone of every family
below is the unchanged hex of a slot of person_cheska.asset's palette.

THE ONE MECHANISM is the beggar's and Dante's: `Toon.shader` remaps a UV to a palette slot only
in Unity atlas rows 0 to 7 and samples the texture itself above them, so every island lives in
the TOP half of the file and the bottom half is left flat.

HOW AN ISLAND IS LAID OUT. A body part is seen from up to six sides (front, back, xpos, xneg, top,
bottom) and each side is one island, a flat orthographic view of the part in MODEL METRES. So a
mark is written where it sits on the body, `(x, z)` on a front or back view, `(y, z)` on a side
view, `(x, y)` on a top view. Loose blocks take a flat SWATCH instead.

THE RULES SHE INHERITS FROM DANTE'S REVIEW (section 13), as they bear on paint:
  * no painted hair shine: hair is three flat tones and nothing is drawn on it;
  * paint stops at its own piece's edges (a sleeve's white must not reach the forearm, a sock's
    stripe must not reach the shin);
  * a sewn or drawn mark is hard edged or it reads as a render fault;
  * every mark is its own hand-set shape with its own numbers; no loop stamps a motif.

ROLE HUES (offence orange #f87020, defence blue #0080e8): every cloth colour is checked below.
Her cyan sits at hue 187, clear of the defence blue's 207. The spatula's wood (#e4a032, palette
slot 10) is near the offence orange in hue but is one 20 mm handle, not an area; it is exempt
and named as such. Skin is exempt, as for the cast.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "cheska"
ATLAS_NAME = "cheska-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is a slot of person_cheska.asset, unchanged.
# ---------------------------------------------------------------------------
SKIN = "f5b894"; SKIN_LIT = "fbcdb0"; SKIN_SHADE = "db9874"; SKIN_DEEP = "bd7d5e"   # slots 13, 14
BLUSH = "ee8f84"
HAIR = "14121a"; HAIR_TOP = "2b2836"; HAIR_DEEP = "0a090d"                          # slot 6
INK = "1f1c24"; EYE_LOW = "3a3348"                                                  # slot 8
CYAN = "48d4e8"; CYAN_LIT = "7fe6f5"; CYAN_MID = "36bcd2"                           # slots 0, 4
TEAL = "2696a8"; TEAL_LIT = "34aabd"; TEAL_DEEP = "1e7a88"                          # slots 1, 5, 7
TRIM = "64e2f6"; TRIM_SHADE = "45c4d2"                                              # slot 2
FROST = "a8f0fa"; FROST_SHADE = "86d6e6"                                            # slot 11
WHITE = "f4faff"; WHITE_SHADE = "d4e2ec"; WHITE_DEEP = "b4c6d6"                     # slots 12, 9
SILVER = "d4e2ec"; SILVER_LIT = "eef5fa"; SILVER_DARK = "a9bccb"                    # slot 9
WOOD = "e4a032"; WOOD_DARK = "b47a1e"                                               # slot 10
SOLE_DIRT = "aeb0ac"

CLOTH_HEXES = [HAIR, HAIR_TOP, CYAN, CYAN_LIT, CYAN_MID, TEAL, TEAL_LIT, TEAL_DEEP, TRIM, TRIM_SHADE, FROST,
               FROST_SHADE, WHITE, WHITE_SHADE, WHITE_DEEP, SILVER_LIT, SILVER_DARK, SOLE_DIRT]

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048). A window is
# (a0, a1, b0, b1) on the view's two axes: front and back are (x, z), xpos and xneg are (y, z),
# top and bottom are (x, y). +x is HER left (the long sock, the sweatband), -y is the way she
# faces, z is up, the feet are on zero.
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.21, 0.21, 0.33, 0.67), 1300),
        "back": ((-0.21, 0.21, 0.33, 0.67), 260),
        "xpos": ((-0.19, 0.19, 0.33, 0.67), 560),
        "xneg": ((-0.19, 0.19, 0.33, 0.67), 560),
        "top": ((-0.21, 0.21, -0.19, 0.19), 200),
        "bottom": ((-0.21, 0.21, -0.19, 0.19), 200),
    },
    # the trapper hat: crown, ear flaps, the mantle over the nape
    "hat": {
        "front": ((-0.27, 0.27, 0.32, 0.80), 600),
        "back": ((-0.27, 0.27, 0.32, 0.80), 640),
        "xpos": ((-0.27, 0.27, 0.32, 0.80), 640),
        "xneg": ((-0.27, 0.27, 0.32, 0.80), 640),
        "top": ((-0.27, 0.27, -0.27, 0.27), 520),
    },
    # the goggles: only what faces forward is drawn (the two lenses and the frame round them)
    "gog": {
        "front": ((-0.20, 0.20, 0.61, 0.71), 1400),
    },
    "torso": {
        "front": ((-0.16, 0.16, 0.16, 0.36), 1300),
        "back": ((-0.16, 0.16, 0.16, 0.36), 1000),
        "xpos": ((-0.13, 0.13, 0.16, 0.36), 950),
        "xneg": ((-0.13, 0.13, 0.16, 0.36), 950),
        "top": ((-0.16, 0.16, -0.13, 0.13), 460),
    },
    "armL": {
        "front": ((0.09, 0.29, 0.21, 0.37), 1000),
        "back": ((0.09, 0.29, 0.21, 0.37), 1000),
        "top": ((0.09, 0.29, -0.07, 0.10), 1000),
        "bottom": ((0.09, 0.29, -0.07, 0.10), 800),
        "xpos": ((-0.07, 0.10, 0.22, 0.36), 700),
    },
    "armR": {
        "front": ((-0.29, -0.09, 0.21, 0.37), 1000),
        "back": ((-0.29, -0.09, 0.21, 0.37), 1000),
        "top": ((-0.29, -0.09, -0.07, 0.10), 1000),
        "bottom": ((-0.29, -0.09, -0.07, 0.10), 800),
        "xneg": ((-0.07, 0.10, 0.22, 0.36), 700),
    },
    "legL": {
        "front": ((0.0, 0.17, 0.0, 0.19), 1000),
        "back": ((0.0, 0.17, 0.0, 0.19), 900),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 1000),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 850),
        "top": ((0.0, 0.17, -0.16, 0.11), 1000),
    },
    "legR": {
        "front": ((-0.17, 0.0, 0.0, 0.19), 1000),
        "back": ((-0.17, 0.0, 0.0, 0.19), 900),
        "xpos": ((-0.16, 0.11, 0.0, 0.19), 850),
        "xneg": ((-0.16, 0.11, 0.0, 0.19), 1000),
        "top": ((-0.17, 0.0, -0.16, 0.11), 1000),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group. The face
# is drawn on the front island, so the head's front reaches round its soft corners.
VIEW_BIAS = {("head", "front"): 1.5, ("head", "top"): 0.8, ("torso", "front"): 1.15, ("torso", "back"): 1.15,
             ("legL", "top"): 0.9, ("legR", "top"): 0.9, ("hat", "top"): 0.9}

# name -> (px wide, px high at 2048, metres wide, metres high). One flat tone each: a block END
# or a small added piece takes one of these instead of landing on another part's drawing.
_TONE = (36, 36, 0.04, 0.04)
SWATCHES = {name: _TONE for name in (
    "hair", "hair_top", "hair_under", "frost", "frost_shade", "trim", "trim_shade", "cyan", "cyan_lit", "cyan_shade",
    "teal", "teal_deep", "white", "white_shade", "white_under", "silver", "silver_lit", "silver_shade",
    "skin_tone", "skin_under", "wood", "wood_shade")}

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin. `head.front`, `hair`, and so on.

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
            _LAYOUT[(size, "scale")] = 1.0 - 0.02 * step
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

    def box(self, colour, a0, a1, b0, b1, feather=0.0, strength=1.0):
        """A plain rectangle between two positions on each axis, hard cornered."""
        return self.mark(colour, [(a0, b0), (a1, b0), (a1, b1), (a0, b1)], feather, strength, curved=False)

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
        """Everything between two heights, the whole island wide: how a hem meets itself."""
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
# THE HEAD. Her face is the cast's plainest and that is the point of her: two black eyes and a
# cat's mouth. They stay exactly that (no glint, no lash, no brow: her original has none). The
# only things added are the stepped shadow her fringe throws and a round blush (rule 12).
# ---------------------------------------------------------------------------
#   where the fringe's five slabs end, read from the model script's FRINGE table: the shadow each
#   throws falls a little below it, and is stepped as they are
FRINGE_SHADOW = [(-0.21, 0.67), (0.21, 0.67), (0.21, 0.492), (0.112, 0.492), (0.112, 0.530), (0.040, 0.530),
                 (0.040, 0.546), (-0.042, 0.546), (-0.042, 0.525), (-0.112, 0.525), (-0.112, 0.486), (-0.21, 0.486)]


#   the original's eye outline about its centre, and the top edge of the original's mouth
EYE = [(-0.0282, -0.0038), (-0.0211, -0.0266), (0.0, -0.0190), (0.0211, -0.0266), (0.0282, -0.0038),
       (0.0211, 0.0191), (0.0, 0.0267), (-0.0211, 0.0191)]
EYE_GROW = 1.15   # 1.33 (rule 12, "a third bigger") was too big in the game; owner: "turn down the eye size"
MOUTH_TOP = [(0.0, 0.4248), (0.0029, 0.4240), (0.0058, 0.4217), (0.0086, 0.4184), (0.0115, 0.4145), (0.0144, 0.4106),
             (0.0172, 0.4075), (0.0201, 0.4055), (0.0230, 0.4051), (0.0259, 0.4064), (0.0288, 0.4093), (0.0316, 0.4134),
             (0.0345, 0.4182), (0.0374, 0.4231), (0.0402, 0.4273), (0.0431, 0.4305), (0.0460, 0.4322)]
MOUTH_DEEP = 0.0135


def paint_head_front(c):
    # A CUTE FACE, NOT A PORTRAIT (docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 12). The first
    # face here was modelled in paint: sockets, a crease under each eye, a nose and its shadow, two
    # steps of jaw shade, a lit chin, a lip shadow, a second tone in the eyes. Owner, 2026-10-05,
    # on the whole redesigned cast: *"the faces look too realistic and look too human, like it
    # lost its charm, the characters have eyebags etc.. they need to be more cutesy"*.
    # Her original face is the plainest and cutest of the cast: two black cut-cornered squares and
    # a cat's "w". So that is ALL that is drawn: flat skin, one very soft lit patch, the fringe's
    # shadow as one flat tone, the two eyes a third bigger in the original's places, a round blush
    # under each, the mouth as one stroke. No nose, no lines, no contour.
    c.blob(SKIN_LIT, (0.0, 0.455), 0.120, 0.085, 26, 0.35)
    c.mark(SKIN_SHADE, FRINGE_SHADOW, 1.4, 0.6, curved=False)
    # the blush: round, under each eye, the one soft thing on the face
    c.blob(BLUSH, (-0.110, 0.424), 0.030, 0.019, 7, 0.62)
    c.blob(BLUSH, (0.110, 0.424), 0.030, 0.019, 7, 0.62)
    # HER EYES, AND THEIR QUIRK. ⚠️ They are NOT plain cut-cornered squares. The first cute face
    # drew them so and the owner saw it at once: *"cheskas face lost the eye quirk"*. Measured off
    # team-cheska.glb (the slot 8 ink polygons): each eye is eight points, a peak at the top and
    # a NOTCH cut 7.6 mm up into the middle of its bottom edge, so it reads as a little smiling
    # arch. EYE is that outline, vertex for vertex, about its own centre (0.0721, 0.4868); it is
    # drawn a third bigger (rule 12), one flat ink, and the notch is simply the skin showing.
    for s in (-1, 1):
        c.mark(INK, [(s * (0.0721 + EYE_GROW * dx), 0.4868 + EYE_GROW * dz) for dx, dz in EYE], 0.3, 1.0, curved=False)
    # HER MOUTH: the original's "w", vertex for vertex. It is a band 13.5 mm deep whose top edge
    # is MOUTH_TOP (her left half; the right is its mirror), 92 mm wide. Drawn as that outline at
    # its own size, so it has the original's weight (a 9 mm stroke read thinner).
    top = [(-x, z) for x, z in reversed(MOUTH_TOP[1:])] + MOUTH_TOP
    c.mark(INK, top + [(x, z - MOUTH_DEEP) for x, z in reversed(top)], 0.3, 1.0, curved=False)


def _head_side(c):
    """A side of the head. The hat's flap covers all of it but the strip beside the cheek."""
    # behind the cheek it is her hair, under the flap
    c.column(HAIR, 0.000, 0.30, 0)


# ---------------------------------------------------------------------------
# THE HAT. Padded cyan cloth. What is drawn is the quilting: the two seams that cross the crown,
# the stitched edge of each flap, the seam between the mantle's two tiers, the strap round the
# crown. The fur, the cushions, the strap and the goggles are GEOMETRY (the model script) and
# only their outward faces land here, on the same marks.
# ---------------------------------------------------------------------------
STRAP = (0.640, 0.672)        # the goggle strap round the crown
CUSHION = (-0.082, 0.128, 0.432, 0.548)   # the dark panel on each flap: y0, y1, z0, z1
FLAP_FUR = (0.350, 0.402)     # the fur along the foot of each flap
MANTLE_FUR = (0.359, 0.411)   # the fur along the foot of the mantle


def _strap(c):
    c.band(TEAL, STRAP[0], STRAP[1])
    c.band(TRIM, STRAP[0] + 0.0095, STRAP[1] - 0.0095)
    c.band(TEAL_DEEP, STRAP[0], STRAP[0] + 0.004, 0.4, 0.9)


def paint_hat_top(c):
    c.blob(CYAN_LIT, (-0.030, -0.040), 0.120, 0.100, 16, 0.75)
    c.mark(CYAN_MID, [(-0.27, 0.27), (0.27, 0.27), (0.27, 0.150), (0.10, 0.176), (-0.08, 0.170), (-0.27, 0.156)], 10, 0.8)
    # the two quilting seams across the crown: a sunk dark line with a row of stitches each side
    c.stroke(TEAL, [(-0.176, 0.001), (0.0, -0.002), (0.176, 0.002)], 7.0, 0.5, 1.0, (0.9, 0.9))
    c.stroke(TEAL, [(0.001, -0.150), (-0.002, 0.020), (0.001, 0.196)], 7.0, 0.5, 1.0, (0.9, 0.9))
    c.stitch(CYAN_LIT, [(-0.166, 0.013), (-0.070, 0.011), (-0.014, 0.012)], 1.6, 7.0, 5.0)
    c.stitch(CYAN_LIT, [(0.016, 0.013), (0.080, 0.012), (0.168, 0.014)], 1.6, 6.5, 5.0)
    c.stitch(CYAN_LIT, [(0.013, -0.140), (0.011, -0.080), (0.012, -0.018)], 1.6, 6.5, 4.5)
    c.stitch(CYAN_LIT, [(-0.013, 0.022), (-0.012, 0.100), (-0.013, 0.186)], 1.6, 7.0, 5.0)


def paint_hat_front(c):
    # above the brim the crown catches the light; below the brim only the flaps' front edges show
    c.mark(CYAN_LIT, [(-0.150, 0.764), (0.120, 0.768), (0.150, 0.716), (-0.130, 0.712)], 9, 0.75)
    c.band(CYAN_MID, 0.30, 0.600, 6, 0.9)
    c.stitch(TEAL, [(-0.150, 0.716), (0.0, 0.719), (0.150, 0.716)], 1.8, 7.0, 5.0)
    _strap(c)


def _hat_side(c):
    """One side of the hat, drawn in (y, z): the crown's side, then the flap hanging from it."""
    c.mark(CYAN_LIT, [(-0.130, 0.760), (0.120, 0.764), (0.170, 0.700), (-0.150, 0.694)], 10, 0.75)
    c.mark(CYAN_LIT, [(-0.090, 0.628), (0.060, 0.632), (0.090, 0.566), (-0.094, 0.560)], 9, 0.6)
    c.mark(CYAN_MID, [(-0.27, 0.470), (0.27, 0.476), (0.27, 0.30), (-0.27, 0.30)], 12, 0.8)
    _strap(c)
    # the flap's stitched edge: down its front, along its foot, up its back
    c.stitch(TEAL, [(-0.092, 0.620), (-0.094, 0.500), (-0.088, 0.412)], 1.8, 7.0, 5.0)
    c.stitch(TEAL, [(0.134, 0.622), (0.136, 0.510), (0.130, 0.412)], 1.8, 7.5, 5.0)
    # the seam where the flap is sewn to the crown
    c.stroke(TEAL, [(-0.108, 0.632), (0.020, 0.629), (0.150, 0.633)], 3.4, 0.5, 0.9, (0.8, 0.8))
    # the cushion: a dark quilted panel, its rim stitched, a frost star sewn in its middle
    y0, y1, z0, z1 = CUSHION
    c.box(TEAL, y0, y1, z0, z1)
    c.box(TEAL_LIT, y0 + 0.006, y1 - 0.006, z1 - 0.030, z1 - 0.006, 4, 0.7)
    c.box(TEAL_DEEP, y0, y1, z0, z0 + 0.014, 2.5, 0.8)
    c.stitch(TRIM, [(y0 + 0.010, z0 + 0.010), (y0 + 0.010, z1 - 0.010), (y1 - 0.010, z1 - 0.010), (y1 - 0.010, z0 + 0.010),
                    (y0 + 0.016, z0 + 0.010)], 1.6, 6.0, 4.5)
    #   the star: a diamond with a longer upright, hard edged, drawn point by point
    c.mark(TRIM, [(0.010, 0.519), (0.019, 0.497), (0.039, 0.490), (0.019, 0.483), (0.010, 0.461), (0.001, 0.483),
                  (-0.019, 0.490), (0.001, 0.497)], 0.3, 1.0, curved=False)
    # the fur along the flap's foot: white, with the dips between its tufts drawn in a cool shade
    z0, z1 = FLAP_FUR
    c.band(WHITE, 0.30, z1)
    c.band(WHITE_SHADE, 0.30, z0 + 0.016, 2.5, 0.8)
    c.stroke(WHITE_SHADE, [(-0.078, z1 - 0.002), (-0.070, z1 - 0.022), (-0.058, z1 - 0.004)], 2.6, 0.4, 0.95, (0.5, 0.5))
    c.stroke(WHITE_SHADE, [(-0.024, z1 - 0.002), (-0.014, z1 - 0.026), (-0.002, z1 - 0.004)], 2.8, 0.4, 0.95, (0.5, 0.5))
    c.stroke(WHITE_SHADE, [(0.038, z1 - 0.002), (0.046, z1 - 0.020), (0.060, z1 - 0.004)], 2.6, 0.4, 0.95, (0.5, 0.5))
    c.stroke(WHITE_SHADE, [(0.092, z1 - 0.002), (0.104, z1 - 0.024), (0.114, z1 - 0.004)], 2.8, 0.4, 0.95, (0.5, 0.5))


def paint_hat_back(c):
    c.mark(CYAN_LIT, [(-0.150, 0.762), (0.130, 0.766), (0.170, 0.690), (-0.170, 0.684)], 10, 0.7)
    c.mark(CYAN_LIT, [(-0.150, 0.630), (-0.030, 0.634), (-0.036, 0.524), (-0.156, 0.520)], 9, 0.55)
    c.mark(CYAN_MID, [(-0.27, 0.470), (0.27, 0.474), (0.27, 0.30), (-0.27, 0.30)], 10, 0.8)
    _strap(c)
    # the seam between the mantle's two tiers, a row of stitches above it
    c.stroke(TEAL, [(-0.186, 0.4975), (0.0, 0.4955), (0.186, 0.4975)], 6.0, 0.5, 1.0, (0.9, 0.9))
    c.stitch(CYAN_LIT, [(-0.176, 0.510), (-0.090, 0.509), (-0.024, 0.510)], 1.6, 7.0, 5.0)
    c.stitch(CYAN_LIT, [(0.026, 0.510), (0.100, 0.509), (0.178, 0.5105)], 1.6, 6.5, 5.0)
    # the stitched corners of the lower tier
    c.stitch(TEAL, [(-0.166, 0.482), (-0.168, 0.440), (-0.164, 0.418)], 1.8, 7.0, 5.0)
    c.stitch(TEAL, [(0.166, 0.480), (0.168, 0.444), (0.165, 0.418)], 1.8, 6.5, 5.0)
    # the adjusting strap down the middle (it is a raised strip in the model): dark, two eyelets
    c.box(TEAL, -0.015, 0.015, 0.411, 0.640)
    c.box(TEAL_DEEP, -0.015, -0.010, 0.411, 0.640, 0.4, 0.8)
    c.blob(TRIM, (0.0, 0.584), 0.0050, 0.0050, 0.3, 1.0)
    c.blob(TRIM, (0.0, 0.548), 0.0050, 0.0050, 0.3, 1.0)
    # the fur along the mantle's foot
    z0, z1 = MANTLE_FUR
    c.band(WHITE, 0.30, z1)
    c.band(WHITE_SHADE, 0.30, z0 + 0.016, 2.5, 0.8)
    c.stroke(WHITE_SHADE, [(-0.170, z1 - 0.002), (-0.160, z1 - 0.022), (-0.146, z1 - 0.004)], 2.8, 0.4, 0.95, (0.5, 0.5))
    c.stroke(WHITE_SHADE, [(-0.104, z1 - 0.002), (-0.092, z1 - 0.026), (-0.082, z1 - 0.004)], 2.6, 0.4, 0.95, (0.5, 0.5))
    c.stroke(WHITE_SHADE, [(-0.040, z1 - 0.002), (-0.030, z1 - 0.020), (-0.016, z1 - 0.004)], 2.8, 0.4, 0.95, (0.5, 0.5))
    c.stroke(WHITE_SHADE, [(0.030, z1 - 0.002), (0.040, z1 - 0.025), (0.052, z1 - 0.004)], 2.6, 0.4, 0.95, (0.5, 0.5))
    c.stroke(WHITE_SHADE, [(0.094, z1 - 0.002), (0.106, z1 - 0.021), (0.118, z1 - 0.004)], 2.8, 0.4, 0.95, (0.5, 0.5))
    c.stroke(WHITE_SHADE, [(0.152, z1 - 0.002), (0.162, z1 - 0.024), (0.174, z1 - 0.004)], 2.6, 0.4, 0.95, (0.5, 0.5))


# ---------------------------------------------------------------------------
# THE GOGGLES, seen from in front: a pale frame, two lenses, a frost star on the bridge. The
# original's lens is a flat cyan plate with a paler upper half and one white bar of a glint, in
# the same place on both; that is what is drawn, with a rim so the lens sits IN the frame.
# ---------------------------------------------------------------------------
LENS = (0.026, 0.152, 0.636, 0.690)   # one lens: |x| from, to; z from, to


def paint_goggles(c):
    c.band(SILVER_LIT, 0.686, 0.72, 0.6, 0.9)
    c.band(SILVER_DARK, 0.60, 0.634, 0.6, 0.85)
    x0, x1, z0, z1 = LENS
    # her right lens
    c.box(TEAL, -x1 - 0.004, -x0 + 0.004, z0 - 0.004, z1 + 0.004)
    c.box(TRIM, -x1, -x0, z0, z1)
    c.mark(FROST, [(-x1, z1), (-x0, z1), (-x0, 0.668), (-0.090, 0.662), (-x1, 0.658)], 0.5, 1.0, curved=False)
    c.mark(WHITE, [(-0.139, 0.685), (-0.094, 0.685), (-0.100, 0.670), (-0.139, 0.670)], 0.3, 1.0, curved=False)
    c.box(TRIM_SHADE, -x1, -x0, z0, z0 + 0.008, 0.6, 0.9)
    # her left lens: the glint sits on the same side of it, as light from one sun does
    c.box(TEAL, x0 - 0.004, x1 + 0.004, z0 - 0.004, z1 + 0.004)
    c.box(TRIM, x0, x1, z0, z1)
    c.mark(FROST, [(x0, z1), (x1, z1), (x1, 0.666), (0.086, 0.660), (x0, 0.657)], 0.5, 1.0, curved=False)
    c.mark(WHITE, [(0.040, 0.685), (0.087, 0.685), (0.081, 0.669), (0.040, 0.669)], 0.3, 1.0, curved=False)
    c.box(TRIM_SHADE, x0, x1, z0, z0 + 0.008, 0.6, 0.9)
    # the star on the bridge (a raised block in the model): cyan, a frost core
    c.box(TRIM, -0.022, 0.022, 0.647, 0.681)
    c.mark(FROST, [(0.0, 0.677), (0.011, 0.664), (0.0, 0.651), (-0.011, 0.664)], 0.3, 1.0, curved=False)


# ---------------------------------------------------------------------------
# THE CLOTHES. A white shirt under cyan overall shorts. The bib, its pocket and rim, the straps,
# their buckles, the bow, the collar wings, the hem band and the spatula are GEOMETRY; what is
# drawn is the cloth: where the light sits, where it folds, seams, stitch rows, the frost mark
# sewn on the pocket.
# ---------------------------------------------------------------------------
HEM = (0.171, 0.190)       # the shorts' hem band
SHORTS_TOP = 0.246
BIB = (0.243, 0.320)
POCKET = (-0.075, 0.075, 0.250, 0.292)


def _hem(c):
    c.band(TRIM, 0.16, HEM[1])
    c.band(FROST, HEM[1] - 0.006, HEM[1] - 0.0025, 0.3, 0.9)
    c.band(TRIM_SHADE, 0.16, HEM[0] + 0.006, 0.5, 0.9)


def paint_torso_front(c):
    # the shirt: white, cool shade where the bib's edge and the arms keep the light off it
    c.mark(WHITE_SHADE, [(-0.16, 0.330), (-0.118, 0.326), (-0.106, 0.282), (-0.110, 0.246), (-0.16, 0.246)], 5, 0.8)
    c.mark(WHITE_SHADE, [(0.16, 0.328), (0.120, 0.324), (0.107, 0.280), (0.111, 0.246), (0.16, 0.246)], 5, 0.8)
    c.stroke(WHITE_DEEP, [(-0.116, 0.322), (-0.110, 0.290), (-0.114, 0.256)], 2.6, 0.5, 0.8)
    c.stroke(WHITE_DEEP, [(0.118, 0.318), (0.111, 0.286), (0.115, 0.254)], 2.4, 0.5, 0.8)
    c.mark(WHITE_SHADE, [(-0.100, 0.36), (0.100, 0.36), (0.090, 0.336), (0.0, 0.330), (-0.090, 0.336)], 3, 0.7)
    # her neck in the collar's V, in the chin's shadow
    c.mark(SKIN_SHADE, [(-0.031, 0.36), (0.031, 0.36), (0.024, 0.306), (0.0, 0.292), (-0.024, 0.306)], 0.4, 1.0, curved=False)
    c.stroke(SKIN_DEEP, [(-0.020, 0.316), (-0.004, 0.309), (0.004, 0.312)], 2.6, 0.5, 0.8)
    # the bib: lit high on her right, deeper toward the waist, a stitch row inside each edge
    c.box(CYAN, -0.106, 0.106, BIB[0], BIB[1] + 0.004)
    c.mark(CYAN_LIT, [(-0.096, 0.314), (-0.020, 0.316), (-0.030, 0.296), (-0.094, 0.292)], 6, 0.75)
    c.mark(CYAN_MID, [(-0.106, 0.262), (0.106, 0.266), (0.106, 0.243), (-0.106, 0.243)], 6, 0.75)
    c.stitch(TEAL, [(-0.096, 0.308), (-0.098, 0.276), (-0.100, 0.248)], 1.6, 6.0, 4.5)
    c.stitch(TEAL, [(0.095, 0.306), (0.098, 0.274), (0.100, 0.248)], 1.6, 6.5, 4.5)
    # the straps above it
    c.box(CYAN, -0.096, -0.061, BIB[1], 0.36)
    c.box(CYAN, 0.061, 0.096, BIB[1], 0.36)
    c.stitch(TEAL, [(-0.089, 0.326), (-0.089, 0.342)], 1.4, 5.0, 4.0)
    c.stitch(TEAL, [(0.089, 0.326), (0.089, 0.342)], 1.4, 5.0, 4.0)
    # the pocket: a darker pouch, stitched round, the frost mark sewn on it by hand
    x0, x1, z0, z1 = POCKET
    c.box(TEAL, x0, x1, z0, z1)
    c.box(TEAL_LIT, x0 + 0.006, x1 - 0.030, z1 - 0.016, z1 - 0.004, 3, 0.7)
    c.box(TEAL_DEEP, x0, x1, z0, z0 + 0.007, 1.5, 0.8)
    c.stitch(TRIM, [(x0 + 0.007, z1 - 0.006), (x0 + 0.007, z0 + 0.007), (x1 - 0.007, z0 + 0.007), (x1 - 0.007, z1 - 0.006)],
             1.5, 5.5, 4.0)
    #   the frost mark: three crossed strokes, each its own length, and a white dot (the original's
    #   "frost icon" and its dot, 40 by 16 mm, drawn as the snowflake it stands for)
    c.stroke(FROST, [(-0.019, 0.2695), (0.019, 0.2705)], 3.0, 0.3, 1.0, (0.7, 0.7), curved=False)
    c.stroke(FROST, [(-0.009, 0.2595), (0.010, 0.2805)], 2.8, 0.3, 1.0, (0.7, 0.7), curved=False)
    c.stroke(FROST, [(0.009, 0.2590), (-0.010, 0.2810)], 2.8, 0.3, 1.0, (0.7, 0.7), curved=False)
    c.blob(WHITE, (0.0, 0.2700), 0.0042, 0.0042, 0.3, 1.0)
    # the shorts: the bib's cloth carried down. A fly seam, a pocket seam each side, the hem band
    c.band(CYAN, 0.16, SHORTS_TOP)
    c.band(TEAL, SHORTS_TOP - 0.006, SHORTS_TOP - 0.001, 0.5, 0.8)
    c.mark(CYAN_LIT, [(-0.110, 0.236), (-0.040, 0.238), (-0.046, 0.200), (-0.108, 0.198)], 7, 0.7)
    c.mark(CYAN_MID, [(0.030, 0.232), (0.120, 0.228), (0.126, 0.192), (0.036, 0.194)], 8, 0.7)
    c.stroke(TEAL, [(0.001, 0.240), (-0.001, 0.214), (0.001, 0.192)], 3.0, 0.4, 0.95, (0.8, 0.9))
    c.stroke(TEAL, [(-0.118, 0.238), (-0.094, 0.228), (-0.082, 0.208)], 2.6, 0.4, 0.9, (0.7, 0.5))
    c.stroke(TEAL, [(0.118, 0.238), (0.096, 0.227), (0.084, 0.206)], 2.6, 0.4, 0.9, (0.7, 0.5))
    c.stitch(TEAL, [(-0.012, 0.238), (-0.014, 0.214), (-0.010, 0.196)], 1.4, 5.0, 4.0)
    _hem(c)
    # between the two leg cuffs of the hem band the dark under-band shows (the original's centre gap)
    c.box(TEAL, -0.010, 0.010, 0.16, HEM[1])


def paint_torso_back(c):
    c.mark(WHITE_SHADE, [(-0.16, 0.300), (-0.060, 0.296), (-0.050, 0.250), (-0.16, 0.246)], 6, 0.7)
    c.mark(WHITE_SHADE, [(0.16, 0.302), (0.058, 0.298), (0.052, 0.252), (0.16, 0.246)], 6, 0.7)
    c.stroke(WHITE_DEEP, [(-0.030, 0.330), (-0.022, 0.296), (-0.030, 0.256)], 2.6, 0.5, 0.8)
    c.stroke(WHITE_DEEP, [(0.026, 0.326), (0.020, 0.292), (0.028, 0.254)], 2.4, 0.5, 0.8)
    # the straps down her back and the brace across them
    c.box(CYAN, -0.096, -0.061, SHORTS_TOP - 0.004, 0.36)
    c.box(CYAN, 0.061, 0.096, SHORTS_TOP - 0.004, 0.36)
    c.box(TEAL, -0.086, 0.086, 0.276, 0.296)
    c.stitch(TRIM, [(-0.080, 0.2905), (0.0, 0.2910), (0.080, 0.2905)], 1.4, 5.5, 4.0)
    # the shorts: a centre seam, two patch pockets each stitched by its own line
    c.band(CYAN, 0.16, SHORTS_TOP)
    c.band(TEAL, SHORTS_TOP - 0.006, SHORTS_TOP - 0.001, 0.5, 0.8)
    c.mark(CYAN_MID, [(-0.130, 0.222), (0.130, 0.226), (0.130, 0.190), (-0.130, 0.190)], 8, 0.6)
    c.stroke(TEAL, [(0.001, 0.240), (-0.001, 0.214), (0.001, 0.192)], 3.0, 0.4, 0.95, (0.8, 0.9))
    c.stitch(TEAL, [(-0.100, 0.232), (-0.102, 0.204), (-0.070, 0.198), (-0.040, 0.204), (-0.040, 0.232)], 1.5, 5.5, 4.0)
    c.stitch(TEAL, [(0.042, 0.232), (0.042, 0.203), (0.072, 0.197), (0.100, 0.203), (0.099, 0.232)], 1.5, 5.0, 4.0)
    _hem(c)
    c.box(TEAL, -0.010, 0.010, 0.16, HEM[1])


def _torso_side(c):
    # under the arm the shirt is in shade; the shorts' side seam runs down from it
    c.mark(WHITE_SHADE, [(-0.060, 0.310), (0.060, 0.312), (0.050, 0.246), (-0.052, 0.246)], 8, 0.8)
    c.stroke(WHITE_DEEP, [(0.004, 0.300), (0.001, 0.272), (0.004, 0.250)], 2.4, 0.5, 0.8)
    c.band(CYAN, 0.16, SHORTS_TOP)
    c.band(TEAL, SHORTS_TOP - 0.006, SHORTS_TOP - 0.001, 0.5, 0.8)
    c.stroke(TEAL, [(0.003, 0.240), (0.000, 0.214), (0.003, 0.192)], 2.6, 0.4, 0.9, (0.8, 0.8))
    c.mark(CYAN_MID, [(0.020, 0.236), (0.100, 0.234), (0.104, 0.194), (0.024, 0.194)], 7, 0.6)
    _hem(c)


def paint_torso_top(c):
    c.blob(WHITE_SHADE, (0.0, 0.0), 0.070, 0.066, 6, 0.9)
    c.blob(SKIN_DEEP, (0.0, 0.0), 0.050, 0.050, 2, 1.0)


# ---------------------------------------------------------------------------
# THE ARMS. A white short sleeve to the elbow, its teal cuff a block of its own, then her bare
# forearm and a plain block hand with drawn fingers. Her LEFT wrist wears the sweatband.
# ⚠️ every mark stops where its piece stops (Dante's review, rule 4): the sleeve's white ends at
# SLEEVE_END, inside the cuff block, and nothing of the sweatband is drawn past its two edges.
# ---------------------------------------------------------------------------
SLEEVE_END = 0.1765
BAND = (0.214, 0.238)      # the sweatband on her left forearm
HAND = 0.236


def _hand(c, s, view):
    """Finger lines and shade on a hand. `s` is +1 for the left arm, -1 for the right."""
    if view in ("front", "back"):
        c.box(SKIN_SHADE, *sorted((s * 0.226, s * 0.30)), 0.20, 0.260, 0, 0.7)
        c.stroke(SKIN_DEEP, [(s * 0.258, 0.3035), (s * 0.2785, 0.305)], 2.4, 0.4, 0.9, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.257, 0.2865), (s * 0.279, 0.286)], 2.4, 0.4, 0.9, (1.0, 0.3), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.258, 0.2690), (s * 0.2775, 0.2665)], 2.4, 0.4, 0.9, (1.0, 0.3), curved=False)
        # the thumb, folded along the top of the fist: one drawn hook
        c.stroke(SKIN_DEEP, [(s * 0.240, 0.321), (s * 0.259, 0.323), (s * 0.266, 0.332)], 2.4, 0.4, 0.9, (0.8, 0.4))
    c.column(SKIN_SHADE, *sorted((s * 0.2745, s * 0.30)), 2.0, 0.55)


def paint_arm(c, s, view):
    """`s` is +1 for her left arm, -1 for her right."""
    x = lambda v: s * v
    col = lambda colour, a, b, feather=0.0, strength=1.0: c.column(colour, *sorted((x(a), x(b))), feather, strength)
    if view in ("xpos", "xneg"):
        # end on: the fingertips, a little shaded
        c.blob(SKIN_SHADE, (0.017, 0.288), 0.05, 0.05, 6, 0.5)
        return
    # skin first: lit along the top of the forearm, in shade under it
    if view == "bottom":
        col(SKIN_SHADE, 0.08, 0.30, 0, 0.8)
    elif view == "top":
        c.mark(SKIN_LIT, [(x(0.186), -0.014), (x(0.232), -0.010), (x(0.232), 0.040), (x(0.186), 0.046)], 6, 0.6)
    else:
        c.box(SKIN_SHADE, *sorted((x(0.17), x(0.30))), 0.20, 0.262, 4, 0.7)
    # the sleeve
    col(WHITE, 0.08, SLEEVE_END)
    if view in ("front", "back"):
        c.box(WHITE_SHADE, *sorted((x(0.08), x(SLEEVE_END))), 0.20, 0.262, 0, 1.0)
        c.stroke(WHITE_SHADE, [(x(0.122), 0.348), (x(0.130), 0.312), (x(0.124), 0.270)], 3.4, 0.6, 0.9)
        c.stroke(WHITE_SHADE, [(x(0.150), 0.340), (x(0.156), 0.306), (x(0.150), 0.272)], 3.0, 0.6, 0.85)
        c.stroke(WHITE_DEEP, [(x(0.104), 0.300), (x(0.110), 0.270), (x(0.106), 0.240)], 2.6, 0.5, 0.7)
    elif view == "bottom":
        col(WHITE_DEEP, 0.08, SLEEVE_END, 0, 0.8)
    else:
        c.stroke(WHITE_SHADE, [(x(0.126), -0.040), (x(0.132), 0.010), (x(0.126), 0.066)], 3.0, 0.6, 0.8)
    if s > 0:
        # the sweatband: cyan with the one white stripe round its middle
        col(TRIM, BAND[0], BAND[1])
        col(WHITE, 0.2225, 0.2295)
        if view in ("front", "back"):
            c.box(TRIM_SHADE, BAND[0], BAND[1], 0.20, 0.262, 0, 0.8)
            c.box(WHITE_SHADE, 0.2225, 0.2295, 0.20, 0.262, 0, 0.9)
        elif view == "bottom":
            col(TRIM_SHADE, BAND[0], BAND[1], 0, 0.8)
            col(WHITE_SHADE, 0.2225, 0.2295, 0, 0.9)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE LEGS. Bare, then socks that do not match (hers by design: a long one with two frost
# stripes on her left, a short one with a turned cuff on her right), then white sneakers with a
# cyan band round the upper, a white toe cap and heel, a pull tab at the back.
# ---------------------------------------------------------------------------
SOCK_BASE = 0.050
SOCK_TOP = {1: 0.126, -1: 0.072}
SOCK_CUFF = (0.068, 0.084)        # the right sock's turned cuff, a block of its own
STRIPES = ((0.097, 0.106), (0.111, 0.120))
SOLE_TOP = 0.018
UPPER = (0.018, 0.042)            # the cyan band round the shoe


def paint_leg(c, s, view):
    """`s` is +1 for her left leg, -1 for her right. Each leg's marks are its own."""
    x = lambda v: s * v
    # skin: the shorts' hem throws a shade on the thigh; the knee catches a little light
    c.band(SKIN_SHADE, 0.158, 0.20, 3.5, 0.75)
    if view == "front":
        c.mark(SKIN_LIT, [(x(0.050), 0.150), (x(0.112), 0.152), (x(0.114), 0.132), (x(0.052), 0.130)], 6, 0.6)
        c.stroke(SKIN_SHADE, [(x(0.060), 0.139), (x(0.082), 0.135), (x(0.104), 0.139)], 2.2, 0.5, 0.7)
    elif view == "back":
        c.band(SKIN_SHADE, 0.04, 0.20, 0, 0.45)
    else:
        inner = (view == "xneg") == (s > 0)
        if inner:
            c.band(SKIN_SHADE, 0.04, 0.20, 0, 0.6)
    # the sock
    top = SOCK_TOP[s]
    c.band(WHITE, SOCK_BASE - 0.01, top)
    if s > 0:
        c.band(TRIM, *STRIPES[0])
        c.band(TRIM, *STRIPES[1])
        c.band(WHITE_SHADE, SOCK_BASE, 0.066, 2.5, 0.7)
        if view == "front":
            c.stroke(WHITE_SHADE, [(0.030, 0.084), (0.082, 0.079), (0.134, 0.085)], 2.4, 0.5, 0.85)
            c.stroke(WHITE_SHADE, [(0.040, 0.070), (0.090, 0.066), (0.130, 0.071)], 2.0, 0.5, 0.75)
        elif view == "back":
            c.stroke(WHITE_SHADE, [(0.036, 0.086), (0.086, 0.081), (0.132, 0.087)], 2.4, 0.5, 0.85)
            c.band(WHITE_SHADE, SOCK_BASE, 0.094, 0, 0.35)
    else:
        # the turned cuff (a raised block): cyan, a frost line along its top
        c.band(TRIM, SOCK_CUFF[0], SOCK_CUFF[1] + 0.002)
        c.band(FROST, SOCK_CUFF[1] - 0.006, SOCK_CUFF[1] - 0.0025, 0.3, 0.9)
        c.band(TRIM_SHADE, SOCK_CUFF[0], SOCK_CUFF[0] + 0.004, 0.4, 0.9)
    # the shoe: white, the cyan band round the upper, grime along the foot of the sole
    c.band(WHITE, 0.0, 0.056)
    c.band(CYAN, UPPER[0], UPPER[1])
    c.band(TEAL, UPPER[0], UPPER[0] + 0.0035, 0.3, 0.9)
    c.band(WHITE_SHADE, 0.0, 0.010, 0.5, 0.9)
    c.band(SOLE_DIRT, 0.0, 0.0045, 1.0, 0.5)
    if view == "front":
        # the toe cap, seen from in front: all white, one stitched line across it
        c.box(WHITE, *sorted((x(0.0), x(0.17))), SOLE_TOP, 0.056)
        c.stroke(WHITE_DEEP, [(x(0.030), 0.031), (x(0.083), 0.036), (x(0.136), 0.031)], 2.0, 0.4, 0.9, (0.6, 0.6))
    elif view == "back":
        # the heel counter: white. (The pull tab is a block of its own.)
        c.box(WHITE, *sorted((x(0.0), x(0.17))), SOLE_TOP, 0.056)
        c.stroke(WHITE_DEEP, [(x(0.050), 0.046), (x(0.083), 0.049), (x(0.118), 0.046)], 2.0, 0.4, 0.9, (0.6, 0.6))
    else:
        # from the side (the toe is at -y): white toe cap, white heel counter, the band between
        c.mark(WHITE, [(-0.17, SOLE_TOP), (-0.100, SOLE_TOP), (-0.094, 0.060), (-0.17, 0.060)], 0, 1.0, curved=False)
        c.mark(WHITE, [(0.050, SOLE_TOP), (0.12, SOLE_TOP), (0.12, 0.060), (0.056, 0.060)], 0, 1.0, curved=False)
        c.stroke(WHITE_DEEP, [(-0.100, 0.020), (-0.097, 0.040)], 1.8, 0.4, 0.9, (0.8, 0.8), curved=False)


def paint_leg_top(c, s):
    """Looking down on a foot: the toe cap, the cyan vamp with its laces, the collar. Toe at -y."""
    x = lambda v: s * v
    c.box(WHITE, *sorted((x(0.0), x(0.17))), -0.17, 0.12)
    c.mark(CYAN, [(x(0.030), -0.098), (x(0.136), -0.098), (x(0.132), -0.040), (x(0.034), -0.040)], 0.4, 1.0, curved=False)
    c.stroke(WHITE_DEEP, [(x(0.026), -0.102), (x(0.083), -0.108), (x(0.140), -0.102)], 2.0, 0.4, 0.9, (0.6, 0.6))
    # three lace bars, each set by hand
    c.stroke(WHITE, [(x(0.050), -0.0885), (x(0.116), -0.0875)], 4.2, 0.3, 1.0, (0.9, 0.9), curved=False)
    c.stroke(WHITE, [(x(0.051), -0.0735), (x(0.115), -0.0745)], 4.2, 0.3, 1.0, (0.9, 0.9), curved=False)
    c.stroke(WHITE, [(x(0.052), -0.0590), (x(0.114), -0.0580)], 4.0, 0.3, 1.0, (0.9, 0.9), curved=False)
    c.stroke(TEAL, [(x(0.083), -0.094), (x(0.083), -0.046)], 1.6, 0.3, 0.8, (1, 1), curved=False)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    c = I("head.xpos", SKIN); _head_side(c); done[c.name] = c
    c = I("head.xneg", SKIN); _head_side(c); done[c.name] = c
    # the back and the top of the head block are under her hair and her hat
    c = I("head.back", HAIR_DEEP); done[c.name] = c
    c = I("head.top", HAIR); done[c.name] = c
    c = I("head.bottom", SKIN_SHADE); done[c.name] = c

    c = I("hat.top", CYAN); paint_hat_top(c); done[c.name] = c
    c = I("hat.front", CYAN); paint_hat_front(c); done[c.name] = c
    c = I("hat.xpos", CYAN); _hat_side(c); done[c.name] = c
    c = I("hat.xneg", CYAN); _hat_side(c); done[c.name] = c
    c = I("hat.back", CYAN); paint_hat_back(c); done[c.name] = c
    c = I("gog.front", SILVER); paint_goggles(c); done[c.name] = c

    c = I("torso.front", WHITE); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", WHITE); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", WHITE); _torso_side(c); done[c.name] = c
    c = I("torso.xneg", WHITE); _torso_side(c); done[c.name] = c
    c = I("torso.top", WHITE); paint_torso_top(c); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN); paint_arm(c, s, view); done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            if view == "top":
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c

    flat = {
        # hair is three flat tones and no drawing (rule 3; see build_hair in the model script)
        "hair": HAIR, "hair_top": HAIR_TOP, "hair_under": HAIR_DEEP,
        "frost": FROST, "frost_shade": FROST_SHADE, "trim": TRIM, "trim_shade": TRIM_SHADE,
        "cyan": CYAN, "cyan_lit": CYAN_LIT, "cyan_shade": CYAN_MID, "teal": TEAL, "teal_deep": TEAL_DEEP,
        "white": WHITE, "white_shade": WHITE_SHADE, "white_under": WHITE_DEEP,
        "silver": SILVER, "silver_lit": SILVER_LIT, "silver_shade": SILVER_DARK,
        "skin_tone": SKIN_SHADE, "skin_under": SKIN_DEEP, "wood": WOOD, "wood_shade": WOOD_DARK,
    }
    for name, colour in flat.items():
        done[name] = I(name, colour, SWATCHES[name][2:4])

    missing = set(n for n in layout(size) if isinstance(n, str)) - set(done)
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
    atlas = Image.new("RGB", (size, size), _rgb(TEAL_DEEP))
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
    atlas.save(out)
    rects = [r for n, r in layout(size).items() if isinstance(n, str)]
    used = sum(r[2] * r[3] for r in rects)
    print("wrote %s  (%d x %d, %d islands, densities at %.0f%%, %.0f%% of the top half painted)"
          % (out, size, size, len(islands), 100.0 * _LAYOUT[(size, "scale")],
             100.0 * used / (size * size / 2)))
    if "--sheet" in sys.argv:
        sheet = sys.argv[sys.argv.index("--sheet") + 1]
        atlas.crop((0, 0, size, size // 2)).save(sheet)
        print("wrote %s" % sheet)


if __name__ == "__main__":
    main()
