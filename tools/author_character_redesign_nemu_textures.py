"""Paint the atlas of the Nemu redesign PROTOTYPE, by hand, in the house style.

    py -3 tools/author_character_redesign_nemu_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/nemu/nemu-redesign-atlas.png. The model is built
by tools/author_character_redesign_nemu.py, which imports this file for the LAYOUT only (PIL is
imported inside the functions, Blender's Python has none). Paint first, then build.

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 13: the owner asked for the rest of the cast to
follow Dante's rework ("the same kid, with a lot more detail"). This is Nemu's copy of Dante's
painter, rewritten for her. Nothing in the game loads these files; team-nemu.glb is not touched.

⚠️⚠️ NEMU IS THE HERO A REWORK ALREADY RUINED ONCE (docs/CAST_CLOTHING_STYLE.md). That pass lowered
her cowl, lifted her fringe and grew her head; the owner: "u removed the jacket that covered half
of nemu's facee / that was on pruposee", "u ruined nemuu". So everything painted here is painted
on HER shapes at HER measurements (tools/build_nemu_voxel.py, read back off team-nemu.glb):
  * the face is the strip between the cowl's rim (z 0.298) and the fringe (0.348 at the outer
    strands, 0.358 at the two middle ones). All that shows in it is her two sleepy eye bars, at
    the original's own place and size (x 0.024 to 0.092, z 0.320 to 0.355). No mouth, no nose,
    no brows: the cowl covers them and the original draws none.
  * the print on her chest (the square C with its eye dot and two flanking squares) and the eye
    on her back are the original's polygons, number for number.
  * the paper talisman keeps the original's five strokes, each redrawn as its own brush mark.

THE ONE MECHANISM is the beggar's and Dante's: `Toon.shader` remaps a UV to a palette slot only
in Unity atlas rows 0 to 7 and samples the texture itself above them, so every island lives in
the TOP half of the file and the bottom half is left flat.

HOW AN ISLAND IS LAID OUT. A body part is seen from up to six sides and each side is one island,
a flat orthographic view of the part in MODEL METRES (front and back are (x, z), the x sides are
(y, z), top and bottom are (x, y)). +x is HER left, -y is the way she faces, z is up, feet on 0.
Loose blocks take a SWATCH instead.

HER COLOURS ARE THE ORIGINAL'S (person_nemu.asset, the 16 colour palette): midnight violet
hoodie 231c34, its crease 181224, lavender aa5cf0, pale lilac d09af8, shoe violet 382856, tag
purple 8a3cd0, hair 1d182e and its lit tone 32284a, ink 120e1c, paper f4faff, silver d0d8e8,
skin e0af84 and its blush d69974. Painted tones are steps of those. ROLE HUES (offence orange
#f87020, defence blue #0080e8): every cloth colour is checked and the build refuses one near
either. Her lavender sits at hue 0.76, well clear of the blue at 0.58.

⚠️ A NEAR-BLACK GARMENT NEEDS WIDER STEPS THAN DANTE'S GREEN. One step of value on 231c34 is
invisible under a two-band ramp; her lit tone is the hair's own lit tone lifted (3a2f56), and
her folds go down to 0f0b18, so a fold reads at game distance instead of only in a close-up.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "nemu"
ATLAS_NAME = "nemu-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is the original palette slot, unchanged.
# ---------------------------------------------------------------------------
SKIN = "e0af84"; SKIN_LIT = "ecc29c"; SKIN_SHADE = "d69974"; SKIN_DEEP = "b9795a"
BLUSH = "dc8f7e"
HAIR = "1d182e"; HAIR_TOP = "32284a"; HAIR_DEEP = "120e1c"
INK = "120e1c"
HOOD = "231c34"; HOOD_LIT = "3a2f56"; HOOD_DARK = "181224"; HOOD_DEEP = "0f0b18"
LAV = "aa5cf0"; LAV_PALE = "d09af8"; LAV_DARK = "8a3cd0"; LAV_DEEP = "5a2a94"; ACCENT = "c87af8"
SHOE = "382856"; SHOE_LIT = "4d3a76"; SHOE_DARK = "271c3d"
SOLE = "f4faff"; SOLE_SHADE = "c8cede"; SOLE_DIRT = "9d98aa"
PAPER = "f4faff"; PAPER_SHADE = "d0d8e8"; PAPER_DEEP = "a9aec4"
SILVER = "d0d8e8"; SILVER_DARK = "8f96ad"

CLOTH_HEXES = [HAIR, HAIR_TOP, HOOD, HOOD_LIT, HOOD_DARK, LAV, LAV_PALE, LAV_DARK, LAV_DEEP, ACCENT,
               SHOE, SHOE_LIT, SHOE_DARK, SOLE, SOLE_SHADE, SOLE_DIRT, PAPER, PAPER_SHADE, PAPER_DEEP,
               SILVER, SILVER_DARK]

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048).
# She is 0.598 tall against Dante's 0.785, so her islands are smaller in metres and take a
# higher density for the same pixels.
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.16, 0.16, 0.26, 0.48), 1500),
        "back": ((-0.16, 0.16, 0.26, 0.48), 600),
        "xpos": ((-0.14, 0.11, 0.26, 0.48), 900),
        "xneg": ((-0.14, 0.11, 0.26, 0.48), 900),
        "top": ((-0.16, 0.16, -0.14, 0.11), 300),
        "bottom": ((-0.16, 0.16, -0.14, 0.11), 300),
    },
    "torso": {
        "front": ((-0.16, 0.16, 0.10, 0.30), 1500),
        "back": ((-0.16, 0.16, 0.10, 0.30), 1500),
        "xpos": ((-0.12, 0.12, 0.10, 0.30), 1100),
        "xneg": ((-0.12, 0.12, 0.10, 0.30), 1100),
        "top": ((-0.16, 0.16, -0.12, 0.12), 500),
        "bottom": ((-0.16, 0.16, -0.12, 0.12), 300),
    },
    "armL": {
        "front": ((0.05, 0.26, 0.09, 0.30), 1200),
        "back": ((0.05, 0.26, 0.09, 0.30), 1200),
        "top": ((0.05, 0.26, -0.12, 0.12), 1100),
        "bottom": ((0.05, 0.26, -0.12, 0.12), 900),
        "xpos": ((-0.12, 0.12, 0.09, 0.28), 1000),
    },
    "armR": {
        "front": ((-0.26, -0.05, 0.09, 0.30), 1200),
        "back": ((-0.26, -0.05, 0.09, 0.30), 1200),
        "top": ((-0.26, -0.05, -0.12, 0.12), 1100),
        "bottom": ((-0.26, -0.05, -0.12, 0.12), 900),
        "xneg": ((-0.12, 0.12, 0.09, 0.28), 1000),
    },
    # ⚠️ A HAND IS ITS OWN GROUP. On Dante the hand shared the arm's islands and the cuff's and the
    # bandage's paint ended up on it ("weird colors on hands"). Here no sleeve paint can reach a
    # hand, because the hand is not on the sleeve's drawing at all.
    "handL": {
        "front": ((0.23, 0.30, 0.15, 0.22), 1500),
        "back": ((0.23, 0.30, 0.15, 0.22), 1500),
        "top": ((0.23, 0.30, -0.03, 0.04), 1500),
        "bottom": ((0.23, 0.30, -0.03, 0.04), 1500),
        "xpos": ((-0.03, 0.04, 0.15, 0.22), 1500),
    },
    "handR": {
        "front": ((-0.30, -0.23, 0.15, 0.22), 1500),
        "back": ((-0.30, -0.23, 0.15, 0.22), 1500),
        "top": ((-0.30, -0.23, -0.03, 0.04), 1500),
        "bottom": ((-0.30, -0.23, -0.03, 0.04), 1500),
        "xneg": ((-0.03, 0.04, 0.15, 0.22), 1500),
    },
    "legL": {
        "front": ((0.0, 0.14, 0.0, 0.17), 1400),
        "back": ((0.0, 0.14, 0.0, 0.17), 1200),
        "xpos": ((-0.12, 0.08, 0.0, 0.17), 1400),
        "xneg": ((-0.12, 0.08, 0.0, 0.17), 1200),
        "top": ((0.0, 0.14, -0.12, 0.08), 1400),
    },
    "legR": {
        "front": ((-0.14, 0.0, 0.0, 0.17), 1400),
        "back": ((-0.14, 0.0, 0.0, 0.17), 1200),
        "xpos": ((-0.12, 0.08, 0.0, 0.17), 1200),
        "xneg": ((-0.12, 0.08, 0.0, 0.17), 1400),
        "top": ((-0.14, 0.0, -0.12, 0.08), 1400),
    },
    # the paper talisman and its clip, seen from the front only (their other faces are flat tones)
    "ofuda": {
        "front": ((-0.136, -0.060, 0.34, 0.55), 2400),
    },
}

# A view takes a face when `dot(normal, VIEW_DIR) * bias` is the largest of its group.
VIEW_BIAS = {("head", "front"): 1.5, ("head", "top"): 0.8, ("torso", "front"): 1.15, ("torso", "back"): 1.15,
             ("legL", "top"): 0.9, ("legR", "top"): 0.9}

# name -> (px wide, px high at 2048, metres wide, metres high). u runs across, v UP.
SWATCHES = {
    "hair": (40, 40, 0.04, 0.04),
    "hair_top": (40, 40, 0.04, 0.04),
    "hair_under": (40, 40, 0.04, 0.04),
    "hood_tone": (40, 40, 0.04, 0.04),
    "hood_lit": (40, 40, 0.04, 0.04),
    "hood_deep": (40, 40, 0.04, 0.04),
    "lav_tone": (40, 40, 0.04, 0.04),
    "lav_pale": (40, 40, 0.04, 0.04),
    "lav_dark": (40, 40, 0.04, 0.04),
    "skin_tone": (40, 40, 0.04, 0.04),
    "shoe": (40, 40, 0.04, 0.04),
    "shoe_lit": (40, 40, 0.04, 0.04),
    "shoe_dark": (40, 40, 0.04, 0.04),
    "paper_edge": (40, 40, 0.04, 0.04),
    "clip_tone": (40, 40, 0.04, 0.04),
    "cowl_out": (900, 96, 0.80, 0.045),
    "cowl_in": (900, 48, 0.80, 0.03),
    "trim": (256, 44, 0.20, 0.02),
    "sole_tone": (40, 40, 0.04, 0.04),
}

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin. A shelf pack into the top half,
    tallest first. Densities shrink together, two per cent at a time, until every island fits."""
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


# ---------------------------------------------------------------------------
# QUIET CLOTH. Owner, 2026-10-05, on Dante in the game's shader: "lets tone down the details in
# dantes clothes", then "do the same dante clothing treatment for nemu's clothes". Painted
# detail that reads as texture in a close-up reads as noise under the two-band ramp and the ink
# edge. With this on (the default):
#   * no stitch rows at all;
#   * no dirt on the soles, no scuffs on the sneakers, no worn places on the hem stripe;
#   * soft cloth marks (lit patches and shade, anything feathered 3 mm or more) at SOFT_MARK;
#   * fold and seam strokes at FOLD.
# KEPT AT FULL STRENGTH: everything structural (the cowl's shadow on the chest, the dark of the
# sleeve's hollow, the hem's under edge, the sole's turn) and everything that is HER design (the
# chest print, the eye on her back, the lavender hem stripe and cuff bands with their pale lip
# and dark edge, the bead, the talisman and its writing, the clip, the sneaker's toe cap, panel
# lines and strap). HER FACE IS NOT CLOTH and none of this touches it: no skin tone is listed.
# Set QUIET_CLOTH to False to see the busy version again.
# ---------------------------------------------------------------------------
QUIET_CLOTH = True
SOFT_MARK = 0.40      # lit patches and shade, where a mark is feathered 3 mm or more
FOLD = 0.32           # fold and seam strokes


def _cloth_tone(colour):
    return QUIET_CLOTH and colour in (HOOD_LIT, HOOD_DARK, HOOD_DEEP, SHOE_LIT)


def _grime(colour):
    return QUIET_CLOTH and colour == SOLE_DIRT


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
        if _grime(colour):
            return self
        if _cloth_tone(colour) and feather >= 3:
            strength *= SOFT_MARK
        pts = _spline(points, closed=True) if curved else list(points)
        mask = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(mask).polygon([self.px(p) for p in pts], fill=255)
        return self._lay(colour, mask, feather, strength)

    def box(self, colour, a0, a1, b0, b1, feather=0.0, strength=1.0):
        """A hard rectangle: how her pixel print is drawn."""
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
        if _cloth_tone(colour):
            strength *= FOLD
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
        if QUIET_CLOTH:
            return self
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
        """Everything between two positions along the first axis: a cuff round a sleeve."""
        b0, b1 = self.win[2], self.win[3]
        pad = (b1 - b0)
        return self.mark(colour, [(a0, b0 - pad), (a1, b0 - pad), (a1, b1 + pad), (a0, b1 + pad)],
                         feather, strength, curved=False)

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE FACE. What shows of it is a strip 50 to 60 mm tall: the cowl's rim below, the fringe above.
# Her two eye bars sit in it exactly where the original puts them. FLAT, by rule 12 (see
# `paint_head_front`): no sockets, no creases, no contour.
# ---------------------------------------------------------------------------
EYE_Z = (0.320, 0.355)       # the original's `eye-sleepy-*` boxes
EYE_X = (0.024, 0.092)
COWL_RIM = 0.298             # the original's `hoodie-collar-*` top
FRINGE_OUTER, FRINGE_MID = 0.348, 0.358


def paint_head_front(c):
    # ⚠️⚠️ A CUTE FACE, NOT A PORTRAIT (docs/CHARACTER_REDESIGN_DANTE.md rule 12). The first face
    # here was modelled in paint: sockets round the eyes, a "tired crease" under each, two steps
    # of shade under the fringe, the cowl's shadow thrown up the cheeks, firm shade down both
    # sides. Owner, 2026-10-05, on the whole redesigned cast: "the faces look too realistic and
    # look too human, like it lost its charm, the characters have eyebags etc.. they need to be
    # more cutesy". Her charm is the original's: a flat strip of skin and two plain black bars.
    # So what is left is flat skin with one very soft lit patch, the fringe's shadow as ONE flat
    # tone, her two eye bars as ONE flat fill each, and a round blush under each eye.
    c.blob(SKIN_LIT, (0.0, 0.322), 0.080, 0.030, 14, 0.35)
    # the fringe's cast shadow, stepped like the four strands that throw it (and the paper tag on
    # her right): one flat tone, hard edged, and nothing layered on it
    c.mark(SKIN_SHADE, [(-0.17, 0.49), (0.17, 0.49), (0.17, 0.3395), (0.065, 0.3395), (0.065, 0.349), (0.003, 0.349),
                        (0.003, 0.352), (-0.004, 0.352), (-0.004, 0.3485), (-0.064, 0.3485), (-0.064, 0.3405),
                        (-0.17, 0.3405)], 0.6, 1.0, curved=False)
    # the blush: a ROUND patch under the middle of each eye, the one soft thing on the face
    c.blob(BLUSH, (-0.058, 0.3085), 0.0125, 0.0092, 2.2, 0.72)
    c.blob(BLUSH, (0.058, 0.3085), 0.0125, 0.0092, 2.2, 0.72)
    # HER EYES: the original's `eye-sleepy-*` boxes, to the number, one flat fill. ⚠️ NOT MADE
    # BIGGER, though rule 12 allows a third. They already run from 0.320 up under the fringe;
    # the only way to grow them is down toward the cowl or out toward the locks, and either
    # changes how much skin shows in the strip, which is the thing a rework of her must not move.
    c.box(INK, -EYE_X[1], -EYE_X[0], EYE_Z[0], EYE_Z[1])
    c.box(INK, EYE_X[0], EYE_X[1], EYE_Z[0], EYE_Z[1])
    # under the cowl's rim (0.298), where nothing shows: one flat tone
    c.band(SKIN_SHADE, 0.25, 0.2975, 0.0, 1.0)


def _head_side(c, sign):
    """One side of the head. What shows is a patch between the side lock and the back hair:
    flat skin, the hair's shadow across the top of it as one flat tone, no contour."""
    c.band(SKIN_SHADE, 0.362, 0.50, 0.6, 1.0)
    c.band(SKIN_SHADE, 0.25, 0.2975, 0.0, 1.0)


def paint_head_back(c):
    # the nape strip that shows between the cowl's rim and the blunt cut of her hair: the
    # island's one flat shade (it is wholly in the hair's shadow), nothing drawn on it
    pass


# ---------------------------------------------------------------------------
# THE HOODIE. Midnight violet, oversized, A-line. Its print is the original's, polygon for
# polygon; a print is flat ink on cloth, so it is hard edged and carries a darker rim
# (docs/CHARACTER_REDESIGN_DANTE.md rule 5: a mark this size reads as a made thing or as a fault).
# ---------------------------------------------------------------------------
HEM_TOP = 0.168
STRIPE = (0.126, 0.146)


def _hem(c, k):
    """The flare below the waist: lit where it kicks out, a stitch row over the stripe."""
    c.stroke(HOOD_LIT, [(-0.17, 0.160 + k), (-0.05, 0.1615), (0.06, 0.160 - k), (0.17, 0.1612)], 5.0, 1.2, 0.75, (1, 1))
    c.stitch(HOOD_LIT, [(-0.17, 0.1505), (0.0, 0.1510), (0.17, 0.1505)], 1.3, 5.0, 4.5, 0.8)
    c.band(HOOD_DEEP, 0.09, 0.1255, 0.6, 0.95)


def paint_torso_front(c):
    # two broad lit patches on the chest, each its own shape, clear of the print
    c.mark(HOOD_LIT, [(-0.104, 0.252), (-0.044, 0.256), (-0.038, 0.214), (-0.062, 0.190), (-0.100, 0.198)], 7, 0.7)
    c.mark(HOOD_LIT, [(0.102, 0.250), (0.046, 0.254), (0.042, 0.218), (0.070, 0.196), (0.102, 0.204)], 7, 0.62)
    # folds pulled down from the shoulders toward the flare
    c.stroke(HOOD_DEEP, [(-0.096, 0.246), (-0.084, 0.208), (-0.094, 0.172)], 6.0, 0.8, 0.9, (0.2, 0.7))
    c.stroke(HOOD_DEEP, [(0.098, 0.240), (0.088, 0.204), (0.100, 0.170)], 5.5, 0.8, 0.9, (0.2, 0.7))
    c.stroke(HOOD_DARK, [(-0.050, 0.178), (-0.056, 0.160), (-0.052, 0.140)], 4.5, 0.8, 0.9, (0.2, 0.6))
    c.stroke(HOOD_DARK, [(0.040, 0.166), (0.046, 0.150), (0.043, 0.134)], 4.0, 0.8, 0.9, (0.2, 0.6))
    # the cowl hangs out over the chest: its shadow, hard, deeper in the middle where it hangs furthest
    c.mark(HOOD_DEEP, [(-0.17, 0.31), (0.17, 0.31), (0.17, 0.259), (0.094, 0.2545), (0.0, 0.2500), (-0.094, 0.2545),
                       (-0.17, 0.259)], 1.4, 0.95)
    # THE PRINT: the square C, its eye dot, the two flanking squares. Rim first, then the ink.
    for a0, a1, b0, b1 in ((-0.024, -0.012, 0.170, 0.236), (-0.012, 0.024, 0.222, 0.236), (-0.012, 0.024, 0.170, 0.184),
                           (0.014, 0.024, 0.210, 0.222), (0.014, 0.024, 0.184, 0.196),
                           (-0.065, -0.045, 0.155, 0.175), (0.045, 0.065, 0.155, 0.175)):
        c.box(LAV_DEEP, a0 - 0.0022, a1 + 0.0022, b0 - 0.0022, b1 + 0.0022)
    c.box(LAV, -0.024, -0.012, 0.170, 0.236)
    c.box(LAV, -0.012, 0.024, 0.222, 0.236)
    c.box(LAV, -0.012, 0.024, 0.170, 0.184)
    c.box(LAV, 0.014, 0.024, 0.210, 0.222)
    c.box(LAV, 0.014, 0.024, 0.184, 0.196)
    # where the ink went on thick: a pale edge along the top of each arm, and down the spine
    c.box(LAV_PALE, -0.022, 0.022, 0.2325, 0.2348)
    c.box(LAV_PALE, -0.0225, -0.0205, 0.186, 0.230)
    c.box(LAV_PALE, -0.010, 0.012, 0.1805, 0.1825)
    c.box(LAV_DARK, -0.022, 0.022, 0.1708, 0.1728)
    c.box(ACCENT, -0.003, 0.009, 0.198, 0.208)
    c.box(LAV_PALE, 0.000, 0.006, 0.201, 0.205)
    c.box(LAV, -0.065, -0.045, 0.155, 0.175)
    c.box(ACCENT, -0.060, -0.050, 0.160, 0.170)
    c.box(LAV, 0.045, 0.065, 0.155, 0.175)
    c.box(ACCENT, 0.050, 0.060, 0.160, 0.170)
    _hem(c, 0.0012)


def paint_torso_back(c):
    c.mark(HOOD_LIT, [(-0.106, 0.254), (0.104, 0.256), (0.100, 0.246), (0.030, 0.2445), (-0.040, 0.2465), (-0.104, 0.244)], 5, 0.7)
    c.stroke(HOOD_DEEP, [(-0.100, 0.236), (-0.094, 0.200), (-0.102, 0.170)], 5.5, 0.8, 0.9, (0.2, 0.7))
    c.stroke(HOOD_DEEP, [(0.102, 0.232), (0.096, 0.198), (0.104, 0.172)], 5.0, 0.8, 0.9, (0.2, 0.7))
    c.mark(HOOD_DEEP, [(-0.17, 0.31), (0.17, 0.31), (0.17, 0.2585), (0.0, 0.2570), (-0.17, 0.2585)], 1.4, 0.95)
    # THE EYE on her back: the original's almond, socket, iris, pupil and glint
    c.mark(LAV_DEEP, [(-0.0885, 0.205), (-0.0462, 0.2425), (0.0462, 0.2425), (0.0885, 0.205), (0.0462, 0.1675),
                      (-0.0462, 0.1675)], 0.0, 1.0, curved=False)
    c.mark(LAV, [(-0.085, 0.205), (-0.045, 0.240), (0.045, 0.240), (0.085, 0.205), (0.045, 0.170), (-0.045, 0.170)],
           0.0, 1.0, curved=False)
    c.mark(LAV_PALE, [(-0.043, 0.2385), (0.043, 0.2385), (0.0455, 0.2362), (-0.0455, 0.2362)], 0.0, 1.0, curved=False)
    c.mark(LAV_DARK, [(-0.047, 0.1738), (0.047, 0.1738), (0.044, 0.1712), (-0.044, 0.1712)], 0.0, 1.0, curved=False)
    c.mark(HOOD, [(-0.068, 0.205), (-0.036, 0.230), (0.036, 0.230), (0.068, 0.205), (0.036, 0.180), (-0.036, 0.180)],
           0.0, 1.0, curved=False)
    c.mark(HOOD_DEEP, [(-0.066, 0.205), (-0.036, 0.2285), (0.036, 0.2285), (0.066, 0.205), (0.050, 0.2175), (-0.050, 0.2175)],
           0.0, 0.9, curved=False)
    c.mark(LAV, [(-0.028, 0.205), (-0.018, 0.226), (0.018, 0.226), (0.028, 0.205), (0.018, 0.184), (-0.018, 0.184)],
           0.0, 1.0, curved=False)
    c.box(HOOD, -0.014, 0.014, 0.196, 0.214)
    c.box(LAV_PALE, -0.007, 0.007, 0.205, 0.214)
    _hem(c, -0.0010)


def _torso_side(c, sign):
    # under the arm: the sleeve's shadow, the side seam, one fold
    c.mark(HOOD_DEEP, [(-0.070, 0.290), (0.074, 0.290), (0.060, 0.214), (-0.054, 0.210)], 6, 0.85)
    c.stroke(HOOD_DARK, [(0.004, 0.280), (0.002, 0.220), (0.005, 0.170)], 2.4, 0.4, 0.95, (0.8, 0.8))
    if sign > 0:
        c.stroke(HOOD_LIT, [(-0.050, 0.200), (-0.058, 0.180), (-0.054, 0.166)], 5.0, 1.0, 0.6, (0.3, 0.6))
    else:
        c.stroke(HOOD_LIT, [(0.046, 0.196), (0.054, 0.178), (0.050, 0.166)], 5.0, 1.0, 0.6, (0.3, 0.6))
    _hem(c, 0.0008 * sign)


# ---------------------------------------------------------------------------
# THE SLEEVES. Three stepped blocks an arm, the original's bell, ending in the lavender band.
# The arm rests straight out along x, so a front view is (x, z).
# ---------------------------------------------------------------------------
BAND = (0.2185, 0.2460)     # the lavender cuff band, along the arm


def paint_sleeve(c, s, view):
    """`s` is +1 for her left sleeve, -1 for her right. Each sleeve's marks are its own."""
    x = lambda v: s * v
    col = lambda a, b: sorted((s * a, s * b))
    if view in ("xpos", "xneg"):
        # looking into the bell: the band's face, and the dark of the sleeve's inside
        c.mark(LAV_DARK, [(-0.13, 0.08), (0.13, 0.08), (0.13, 0.150), (-0.13, 0.150)], 5, 0.55, curved=False)
        c.box(LAV_PALE, -0.104, 0.104, 0.2575, 0.2610)
        c.box(HOOD_DEEP, -0.0925, 0.0925, 0.1175, 0.2525)
        c.mark(HOOD_DARK, [(-0.0925, 0.2525), (0.0925, 0.2525), (0.074, 0.232), (-0.074, 0.232)], 0.0, 1.0, curved=False)
        return
    if view == "top":
        c.mark(HOOD_LIT, [(x(0.066), -0.058), (x(0.126), -0.060), (x(0.176), -0.074), (x(0.214), -0.082),
                          (x(0.214), 0.020 + 0.01 * s), (x(0.170), 0.030), (x(0.120), 0.036), (x(0.066), 0.040)], 8, 0.75)
        c.stitch(HOOD_DARK, [(x(0.074), -0.074), (x(0.0765), 0.0), (x(0.074), 0.074)], 1.5, 5.0, 4.0, 0.9)
        c.stroke(HOOD_DARK, [(x(0.118), 0.060), (x(0.166), 0.072), (x(0.212), 0.090)], 4.5, 0.8, 0.9, (0.2, 0.7))
    elif view == "bottom":
        c.column(HOOD_DARK, *col(0.04, 0.27), 0, 1.0)
        c.stroke(HOOD_DEEP, [(x(0.120), -0.040), (x(0.170), -0.020), (x(0.214), -0.030)], 6.0, 1.0, 0.9, (0.3, 0.7))
    else:
        k = 1.0 if (view == "front") == (s > 0) else -1.0
        # lit along the top of the bell, dark under it, following the three steps
        c.mark(HOOD_LIT, [(x(0.064), 0.2845), (x(0.128), 0.2845), (x(0.178), 0.2690), (x(0.216), 0.2590),
                          (x(0.216), 0.2300 + 0.004 * k), (x(0.172), 0.2420), (x(0.122), 0.2560), (x(0.064), 0.2640)], 5, 0.75)
        c.mark(HOOD_DEEP, [(x(0.064), 0.2000), (x(0.110), 0.1960), (x(0.112), 0.1500), (x(0.160), 0.1480),
                           (x(0.163), 0.0980), (x(0.217), 0.0960), (x(0.217), 0.1420), (x(0.170), 0.1500),
                           (x(0.152), 0.1860), (x(0.112), 0.2100), (x(0.064), 0.2200)], 4, 0.85)
        # folds fanning out toward the cuff, where the bell opens
        c.stroke(HOOD_DEEP, [(x(0.120), 0.2480), (x(0.164), 0.2210 + 0.003 * k), (x(0.210), 0.1760)], 5.0, 0.7, 0.9, (0.2, 0.8))
        c.stroke(HOOD_DEEP, [(x(0.132), 0.2100), (x(0.174), 0.1920), (x(0.212), 0.1480 - 0.004 * k)], 4.2, 0.7, 0.9, (0.2, 0.8))
        c.stroke(HOOD_LIT, [(x(0.150), 0.2420), (x(0.186), 0.2240), (x(0.212), 0.2040)], 3.6, 0.9, 0.6, (0.3, 0.6))
        # the dropped shoulder seam
        c.stitch(HOOD_LIT, [(x(0.0735), 0.2800), (x(0.0765), 0.2420), (x(0.0735), 0.2060)], 1.4, 5.0, 4.0, 0.8)
    # THE BAND: lavender, a pale lip at the mouth of the sleeve, a darker edge where it meets cloth
    c.column(LAV, *col(BAND[0], 0.31))
    c.column(LAV_PALE, *col(0.2395, 0.2428), 0.3, 0.9)
    c.column(LAV_DARK, *col(BAND[0], 0.2222), 0.3, 0.85)
    if view in ("front", "back"):
        c.mark(LAV_DARK, [(x(BAND[0]), 0.08), (x(0.31), 0.08), (x(0.31), 0.150), (x(BAND[0]), 0.150)], 5, 0.55, curved=False)
    if view == "bottom":
        c.column(LAV_DARK, *col(BAND[0], 0.31), 0, 0.6)


def paint_hand(c, s, view):
    """A plain block hand, as the original's is: skin, a shade under it, two drawn finger lines."""
    x = lambda v: s * v
    if view in ("xpos", "xneg"):
        c.blob(SKIN_SHADE, (0.005, 0.185), 0.03, 0.03, 4, 0.5)
        return
    if view == "bottom":
        c.column(SKIN_SHADE, *sorted((x(0.22), x(0.31))), 0, 0.9)
        return
    if view == "top":
        c.blob(SKIN_LIT, (x(0.268), 0.005), 0.018, 0.014, 4, 0.6)
        return
    c.band(SKIN_SHADE, 0.14, 0.177, 1.2, 0.85)
    c.stroke(SKIN_DEEP, [(x(0.268), 0.196), (x(0.287), 0.1955)], 1.8, 0.3, 0.9, (1.0, 0.3), curved=False)
    c.stroke(SKIN_DEEP, [(x(0.268), 0.181), (x(0.287), 0.1818)], 1.8, 0.3, 0.9, (1.0, 0.3), curved=False)


# ---------------------------------------------------------------------------
# THE LEGS. A bare calf under the hem, a chunky violet sneaker on a white sole, the lavender
# ankle strap. The strap, its pale tab and the heel are their own blocks and take flat tones.
# ---------------------------------------------------------------------------
STRAP = (0.052, 0.074)
SOLE_TOP = 0.022


def paint_leg(c, s, view):
    """`s` is +1 for her left leg, -1 for her right. Each leg's marks are its own."""
    x = lambda v: s * v
    # the calf, and the hem's shadow across the top of it (the original's `leg-shadow-*`)
    c.band(SKIN, STRAP[1], 0.20)
    c.band(SKIN_SHADE, 0.113, 0.20, 1.0, 0.95)
    c.band(SKIN_DEEP, 0.128, 0.20, 1.5, 0.7)
    if view == "front":
        c.stroke(SKIN_LIT, [(x(0.062), 0.108), (x(0.064), 0.084)], 6.0, 1.5, 0.6, (0.5, 0.5), curved=False)
    elif view == "back":
        c.band(SKIN_SHADE, STRAP[1], 0.20, 0, 0.6)
    # the sneaker
    c.band(SHOE, 0.0, STRAP[1])
    if view == "front":
        c.mark(SHOE_LIT, [(x(0.034), 0.046), (x(0.106), 0.046), (x(0.102), 0.034), (x(0.038), 0.034)], 2.5, 0.8)
    elif view == "back":
        c.band(SHOE_DARK, SOLE_TOP, 0.034, 1.5, 0.8)
    else:
        outer = (view == "xpos") == (s > 0)
        # the toe cap's line, the panel behind it, one scuff on the outer side of each shoe
        c.stroke(SHOE_DARK, [(-0.064, 0.0235), (-0.060, 0.038), (-0.052, 0.049)], 2.2, 0.4, 1.0, (0.7, 0.7))
        c.mark(SHOE_LIT, [(-0.100, 0.044), (-0.068, 0.046), (-0.070, 0.030), (-0.098, 0.030)], 2.5, 0.75)
        c.stroke(SHOE_DARK, [(0.014, 0.024), (0.018, 0.040), (0.016, 0.051)], 2.0, 0.4, 1.0, (0.7, 0.7))
        c.mark(SHOE_DARK, [(-0.046, 0.034), (0.008, 0.034), (0.008, 0.024), (-0.046, 0.024)], 2.5, 0.6)
        if outer and s > 0 and not QUIET_CLOTH:
            c.mark(SHOE_LIT, [(-0.030, 0.046), (-0.004, 0.047), (-0.008, 0.040), (-0.028, 0.0395)], 1.5, 0.6)
        if outer and s < 0 and not QUIET_CLOTH:
            c.mark(SHOE_LIT, [(0.022, 0.045), (0.040, 0.046), (0.038, 0.037), (0.024, 0.036)], 1.5, 0.6)
    # the strap round the ankle (its own block; this band is what the block's faces sample)
    c.band(LAV, STRAP[0], STRAP[1] + 0.0005)
    c.band(LAV_PALE, 0.0680, 0.0712, 0.3, 0.9)
    c.band(LAV_DARK, STRAP[0], 0.0562, 0.3, 0.85)
    # the white sole: a shade line where it turns under, a tread notch, dirt along the ground
    c.band(SOLE, 0.0, SOLE_TOP)
    c.band(SOLE_SHADE, 0.0, 0.0085, 0.3, 0.95)
    c.band(SOLE_DIRT, 0.0, 0.0035, 0.8, 0.5)
    if view in ("xpos", "xneg"):
        c.stroke(SOLE_SHADE, [(-0.040, 0.0215), (-0.036, 0.0100)], 1.8, 0.3, 1.0, (1, 1), curved=False)
        c.stroke(SOLE_SHADE, [(0.022, 0.0215), (0.020, 0.0100)], 1.8, 0.3, 1.0, (1, 1), curved=False)


def paint_leg_top(c, s):
    """Looking down on a foot: the toe cap line, the strap's top, the toe at -y."""
    x = lambda v: s * v
    c.mark(SHOE_LIT, [(x(0.034), -0.098), (x(0.106), -0.098), (x(0.104), -0.070), (x(0.036), -0.070)], 3, 0.8)
    c.stroke(SHOE_DARK, [(x(0.028), -0.0645), (x(0.070), -0.0600), (x(0.112), -0.0645)], 2.2, 0.4, 1.0, (0.6, 0.6))
    c.box(LAV, x(0.0) if s > 0 else x(0.14), x(0.14) if s > 0 else x(0.0), -0.0815, 0.0495)
    c.mark(SHOE_DARK, [(x(0.0), 0.0500), (x(0.14), 0.0500), (x(0.14), 0.09), (x(0.0), 0.09)], 0, 0.5, curved=False)


# ---------------------------------------------------------------------------
# THE PAPER TALISMAN on her fringe, and the purple clip that holds it. The original writes it
# with a dot and four strokes; they are redrawn one by one as brush marks, in the same places.
# ---------------------------------------------------------------------------

def paint_ofuda(c):
    # the paper: x -0.126 to -0.070, z 0.350 to 0.495
    c.box(PAPER, -0.14, -0.05, 0.33, 0.4955)
    c.mark(PAPER_SHADE, [(-0.14, 0.33), (-0.05, 0.33), (-0.05, 0.3640), (-0.090, 0.3600), (-0.14, 0.3660)], 3, 0.8)
    c.stroke(PAPER_SHADE, [(-0.126, 0.4605), (-0.098, 0.4590), (-0.070, 0.4610)], 1.4, 0.3, 0.9, (1, 1))     # a crease
    c.band(PAPER_DEEP, 0.4885, 0.4955, 0.6, 0.9)                                                             # the clip's shadow
    c.blob(LAV_DARK, (-0.098, 0.4750), 0.0062, 0.0070, 0.25, 1.0)                                            # the dot
    c.stroke(LAV_DARK, [(-0.1185, 0.4455), (-0.098, 0.4440), (-0.0775, 0.4462)], 7.0, 0.25, 1.0, (0.85, 0.45))   # top bar
    c.stroke(LAV_DARK, [(-0.1105, 0.4325), (-0.1095, 0.4130), (-0.1110, 0.3945)], 8.0, 0.25, 1.0, (0.9, 0.5), curved=False)
    c.mark(LAV_DARK, [(-0.0985, 0.4320), (-0.0780, 0.4325), (-0.0778, 0.3900), (-0.0980, 0.3905)], 0.25, 1.0, curved=False)
    c.box(PAPER, -0.0930, -0.0832, 0.4060, 0.4175)                                                           # its window
    c.stroke(LAV_DARK, [(-0.1185, 0.3775), (-0.098, 0.3760), (-0.0778, 0.3770)], 11.0, 0.25, 1.0, (0.9, 0.55))   # foot bar
    # the clip: x -0.124 to -0.072, z 0.490 to 0.542. A pale top edge, the silver pin, the pupil.
    c.box(LAV_DARK, -0.14, -0.05, 0.4955, 0.56)
    c.box(LAV, -0.14, -0.05, 0.5340, 0.56)
    c.box(LAV_DEEP, -0.14, -0.05, 0.4955, 0.4990)
    c.box(SILVER_DARK, -0.1090, -0.0870, 0.5172, 0.5290)
    c.box(SILVER, -0.1080, -0.0880, 0.5180, 0.5280)
    c.box(INK, -0.1060, -0.0900, 0.4980, 0.5100)
    c.box(LAV_PALE, -0.1030, -0.0985, 0.5050, 0.5085)


# ---------------------------------------------------------------------------
# THE SWATCHES. u across, v up.
# ---------------------------------------------------------------------------

def paint_cowl_out(c):
    # the cowl from outside, u going round her from the front centre. Lit along the roll of its
    # top, dark where it tucks under, creases set by hand at unequal gaps.
    c.mark(HOOD_LIT, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.62), (0.80, 0.54), (0.56, 0.64), (0.30, 0.52), (0.12, 0.60), (0.0, 0.62)], 5, 0.8)
    c.mark(HOOD_DEEP, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.16), (0.72, 0.24), (0.46, 0.16), (0.20, 0.26), (0.0, 0.16)], 4, 0.9)
    c.stroke(HOOD_DEEP, [(0.085, 0.10), (0.092, 0.50), (0.086, 0.88)], 4.5, 0.6, 0.9, (0.8, 0.2))
    c.stroke(HOOD_DEEP, [(0.300, 0.08), (0.306, 0.46), (0.298, 0.80)], 4.0, 0.6, 0.9, (0.8, 0.2))
    c.stroke(HOOD_DEEP, [(0.520, 0.10), (0.512, 0.52), (0.522, 0.90)], 4.5, 0.6, 0.9, (0.8, 0.2))
    c.stroke(HOOD_DEEP, [(0.735, 0.08), (0.742, 0.44), (0.734, 0.78)], 3.8, 0.6, 0.9, (0.8, 0.2))
    c.stroke(HOOD_DEEP, [(0.925, 0.10), (0.918, 0.50), (0.927, 0.86)], 4.2, 0.6, 0.9, (0.8, 0.2))
    c.stitch(HOOD_DARK, [(0.0, 0.84), (0.5, 0.85), (1.0, 0.84)], 1.3, 5.0, 4.0, 0.9)


def paint_cowl_in(c):
    c.mark(HOOD_DEEP, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.70), (0.5, 0.60), (0.0, 0.70)], 3, 0.95)


def paint_trim(c):
    # the hem stripe: a pale top edge, a darker under edge, two worn places of unequal length
    c.mark(LAV_PALE, [(0.0, 1.0), (1.0, 1.0), (1.0, 0.76), (0.0, 0.76)], 1.0, 0.85, curved=False)
    c.mark(LAV_DARK, [(0.0, 0.0), (1.0, 0.0), (1.0, 0.22), (0.0, 0.22)], 1.0, 0.9, curved=False)
    if not QUIET_CLOTH:
        c.mark(LAV_DARK, [(0.16, 0.30), (0.25, 0.30), (0.24, 0.70), (0.17, 0.68)], 2.0, 0.3)
        c.mark(LAV_DARK, [(0.61, 0.28), (0.74, 0.30), (0.73, 0.66), (0.62, 0.70)], 2.0, 0.28)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        c = I(name, base, SWATCHES[name][2:4])
        done[name] = c
        return c

    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    # the sides are the face's own flat skin: a darker side tone was contour (rule 12), and the
    # toon ramp already turns a side plane when the light leaves it
    c = I("head.xpos", SKIN); _head_side(c, 1); done[c.name] = c
    c = I("head.xneg", SKIN); _head_side(c, -1); done[c.name] = c
    c = I("head.back", SKIN_SHADE); paint_head_back(c); done[c.name] = c
    c = I("head.top", SKIN_DEEP); done[c.name] = c          # wholly under the hair
    c = I("head.bottom", SKIN_DEEP); done[c.name] = c

    c = I("torso.front", HOOD); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", HOOD); paint_torso_back(c); done[c.name] = c
    c = I("torso.xpos", HOOD); _torso_side(c, 1); done[c.name] = c
    c = I("torso.xneg", HOOD); _torso_side(c, -1); done[c.name] = c
    c = I("torso.top", HOOD_DARK); done[c.name] = c         # the shoulders, under the cowl
    c = I("torso.bottom", HOOD_DEEP); done[c.name] = c

    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, LAV if view in ("xpos", "xneg") else HOOD)
            paint_sleeve(c, s, view); done[c.name] = c
    for group, s in (("handL", 1), ("handR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN); paint_hand(c, s, view); done[c.name] = c
    for group, s in (("legL", 1), ("legR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SHOE)
            if view == "top":
                paint_leg_top(c, s)
            else:
                paint_leg(c, s, view)
            done[c.name] = c
    c = I("ofuda.front", PAPER); paint_ofuda(c); done[c.name] = c

    # hair is three flat tones and no drawing (docs/CHARACTER_REDESIGN_DANTE.md rule 3)
    swatch("hair", HAIR); swatch("hair_top", HAIR_TOP); swatch("hair_under", HAIR_DEEP)
    swatch("hood_tone", HOOD_DARK); swatch("hood_lit", HOOD_LIT); swatch("hood_deep", HOOD_DEEP)
    swatch("lav_tone", LAV); swatch("lav_pale", LAV_PALE); swatch("lav_dark", LAV_DARK)
    swatch("skin_tone", SKIN_SHADE)
    swatch("shoe", SHOE); swatch("shoe_lit", SHOE_LIT); swatch("shoe_dark", SHOE_DARK)
    swatch("paper_edge", PAPER_SHADE); swatch("clip_tone", LAV_DEEP)
    paint_cowl_out(swatch("cowl_out", HOOD))
    paint_cowl_in(swatch("cowl_in", HOOD_DARK))
    paint_trim(swatch("trim", LAV))
    swatch("sole_tone", SOLE)

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
    atlas = Image.new("RGB", (size, size), _rgb(HOOD_DEEP))
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
    used = sum(r[2] * r[3] for r in layout(size).values())
    print("wrote %s  (%d x %d, %d islands, %.0f%% of the top half painted)"
          % (out, size, size, len(islands), 100.0 * used / (size * size / 2)))
    if "--sheet" in sys.argv:
        sheet = sys.argv[sys.argv.index("--sheet") + 1]
        atlas.crop((0, 0, size, size // 2)).save(sheet)
        print("wrote %s" % sheet)


if __name__ == "__main__":
    main()
