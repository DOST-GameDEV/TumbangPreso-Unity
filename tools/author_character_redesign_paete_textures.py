"""Paint the atlas of the Paete redesign PROTOTYPE, by hand, in the house style.

    py -3 tools/author_character_redesign_paete_textures.py [--size 1024] [--sheet file.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/paete/paete-redesign-atlas.png. The model is
built by tools/author_character_redesign_paete.py, which imports this file for the LAYOUT only
(PIL is imported inside the painting calls, Blender's Python has none). Paint first, then build.

WHY. docs/CHARACTER_REDESIGN_DANTE.md sections 13 and 15: the cast was redesigned the way Dante
was. Paete was held back as finalized and protected; the owner lifted that for a PROTOTYPE only.
This is his own copy of the textures script, rewritten for him. It imports nothing from another
hero's files. Nothing in the game loads what it writes, and team-paete.glb, build_paete_voxel.py,
person_paete.asset and everything under characters/paete-motion are not touched.

WHO HE IS (tools/build_paete_voxel.py, ArtSource/paete/concept-20260925/design-brief.md). The
guardian of Mount Makiling, a tree a woodcarver from Paete cut a face into: "two deep eyes and a
mouth that never quite decided to smile". He is bark planks on a dark core, moss in the seams,
leaves in clusters, branch antlers, root feet, and FOREARMS THAT ARE A BRAID OF POINTED VINES.
There is no cloth on him, so CAST_CLOTHING_STYLE.md does not apply (its own table says so).

HIS FACE IS NOT THE CAST'S INK FACE, AND THAT IS THE OWNER'S OWN BRIEF ("engraved sunked green
eyes and a nonchalant calm expresison"). The original has NO slot 8 ink on the head at all. Each
eye is three polygons, a dark socket (slot 9), a green rim (slot 11) and a bright slit (slot 10),
and the mouth is two short carved cuts in the dark bark colour. All of them are kept, MEASURED:
the eye polygons are read off team-paete.glb vertex for vertex (the head mesh's triangles whose
UVs sit in slots 9, 10 and 11; the left and the right were read separately and are exact mirrors),
the mouth off the builder's two cuts. The eyes are at the original's size (`EYE_SCALE` 1.0, see
below). The slant of the slit's top edge, rising outward, is his calm look and is not redrawn.

⚠️ HE IS THE EXCEPTION TO THE FLAT FACE PLANE AND TO THE BIGGER EYES (owner, 2026-10-05, on v04).
v01 to v04 followed section 15.3 rule 2: the front of the head was one plane and the brow,
cheek, nose-seam and forehead planks were paint on it, with the eyes scaled 1.25. The owner looked
and ruled that a carved mask is not a kid's face: the heavy brow comes back as real geometry over
the eyes, and the eyes go back to the original's measured size, sunk slits under a ledge. So the
mask is built again as the original builds it, a dark core with the face planks standing proud
of it (the `MASK` table below, the builder's own boxes and tilts), and this file paints two
things: the CORE's front, which carries only the eyes (island `head.front`), and the planks'
fronts (island `mask.front`), each plank a flat tone with a few chunky carved lines. The painted
seams, lit edges and cast shadows of v04 are gone: the blocks make their own.

PAINT RULES KEPT (section 15.3 rule 8 and the brief): a face takes a drawing only when it squarely
faces its view or is a plank's own flat side; chamfers and ends take ONE flat tone. Carved marks
are hard edged. No two planks show the same piece of grain (each takes its own window of the wood
swatch, chosen by where the plank sits).

THE ONE MECHANISM is Dante's: `Toon.shader` remaps a UV to a palette slot only in Unity atlas
rows 0 to 7 and samples the texture above them, so every island lives in the TOP half of the file
and the bottom half is left flat.

HIS COLOURS ARE THE ORIGINAL'S (PALETTE in tools/build_paete_voxel.py): bark 8c6440, bark dark
553a22, bark lit b08450, root 6b4a2e, moss 5e7f24 / 3f5a1a / 7fa034, leaf 9cc23f / 6a962e, vine
557a26, socket 1c1109, eye light d8ff6a, eye glow 86c83a. Painted tones are steps between those.
ROLE HUES (#f87020, #0080e8): every green is checked. BARK IS EXEMPT, as skin is for the cast:
it is his skin (the builder keeps it in the protected skin slots 13 to 15 for that reason), it is
the original's own brown, and it sits about as near the orange as Rafi's skin does.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "paete"
ATLAS_NAME = "paete-redesign-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint (see the docstring)
GUTTER = 4            # px of bleed round every island at 2048
SS = 2                # islands are painted at twice their size and filtered down

# ---------------------------------------------------------------------------
# THE COLOURS. The first of each family is the original palette slot, unchanged.
# ---------------------------------------------------------------------------
BARK = "8c6440"; BARK_DARK = "553a22"; BARK_LIT = "b08450"; ROOT_C = "6b4a2e"
MOSS = "5e7f24"; MOSS_DARK = "3f5a1a"; MOSS_LIT = "7fa034"
LEAF = "9cc23f"; LEAF_DARK = "6a962e"; VINE = "557a26"
CARVE = "1e140c"; SOCKET = "1c1109"; EYE = "d8ff6a"; EYE_GLOW = "86c83a"

GREEN_HEXES = [MOSS, MOSS_DARK, MOSS_LIT, LEAF, LEAF_DARK, VINE, EYE, EYE_GLOW]

# THE EYES. The original's own size (owner, 2026-10-05): slits 44 by 9 mm in a green rim 58 by 16.
# v01 to v04 had them at 1.25, which with no brow over them read as wide open.
EYE_SCALE = 1.0
EYE_CENTRE = (0.056, 0.5765)        # of the glow rim's outline, his left; the right mirrors it

# Measured off team-paete.glb (head mesh, slots 9, 11, 10), his LEFT eye; the right is the exact
# mirror in the file. (x, height), metres, the model's own space.
SOCKET_POLY = [(0.016, 0.565), (0.0855, 0.565), (0.0891, 0.5884), (0.016, 0.578)]
GLOW_POLY = [(0.026, 0.568), (0.084, 0.571), (0.083, 0.584), (0.030, 0.577)]
LIGHT_POLY = [(0.034, 0.571), (0.078, 0.573), (0.077, 0.580), (0.037, 0.576)]
# The mouth, the builder's two carved cuts (each 4 mm thick), as the line through their middles:
# his left cut runs (0.008, 0.512) to (0.048, 0.514), his right (-0.048, 0.513) to (-0.008, 0.511).
MOUTH_LINE = [(-0.048, 0.513), (-0.008, 0.511), (0.008, 0.512), (0.048, 0.514)]

# ---------------------------------------------------------------------------
# PLANKS THAT CARRY AN ENGRAVING. name -> (lo, hi, tilt) in the builder's TABLE space (x his
# left, y up, z negative toward the face), the original's own boxes and BOX_TILTS. The model
# script builds these planks from this table, so the cut and the plank cannot drift apart.
# ---------------------------------------------------------------------------
SPECIAL = {
    "boss": ((-0.028, 0.352, -0.140), (0.028, 0.408, -0.108), 45.0),
    "pauldron_spiral": ((0.124, 0.436, -0.112), (0.252, 0.520, -0.090), -6.0),
    "pec_left": ((0.006, 0.382, -0.132), (0.166, 0.478, -0.098), -12.0),
    "pec_right": ((-0.168, 0.378, -0.130), (-0.004, 0.474, -0.096), 11.0),
    "back_left": ((0.004, 0.350, 0.092), (0.158, 0.478, 0.118), 8.0),
    "back_right": ((-0.156, 0.346, 0.094), (-0.006, 0.474, 0.120), -7.0),
    "small_back": ((-0.070, 0.270, 0.096), (0.064, 0.316, 0.114), 0.0),
}


def special_size(name):
    lo, hi, _ = SPECIAL[name]
    return (hi[0] - lo[0], hi[1] - lo[1])


def to_local(name, x, y):
    """A point of the original's front or back view to the plank's own (u, v), 0 to 1."""
    lo, hi, tilt = SPECIAL[name]
    cx, cy = 0.5 * (lo[0] + hi[0]), 0.5 * (lo[1] + hi[1])
    t = math.radians(-tilt)
    dx, dy = x - cx, y - cy
    a, b = dx * math.cos(t) - dy * math.sin(t), dx * math.sin(t) + dy * math.cos(t)
    return (a / (hi[0] - lo[0]) + 0.5, b / (hi[1] - lo[1]) + 0.5)


# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048).
# Only the face is a projected island. Everything else is a plank's own swatch or a flat tone.
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    # the core's front: the eyes, nothing else. THE WINDOW COVERS THE WHOLE CORE FRONT (0.490 to
    # 0.660): v05 cropped it to the eye band, the core's one quad had its corners clamped to the
    # island's edge, and the slits were stretched into tall green wedges.
    "head": {
        "front": ((-0.11, 0.11, 0.48, 0.67), 2400),
    },
    "mask": {       # the fronts of the planks that stand proud of the core
        "front": ((-0.13, 0.13, 0.48, 0.69), 2400),
    },
}

# THE MASK'S PLANKS. name -> (lo, hi, tone, tilt, grain), the builder's HEAD rows and BOX_TILTS in
# its table space. Two changes, both so the mouth can be ONE stroke: the two lower face planks
# meet at the centre (the original leaves 8 mm of core between them) and share one front depth
# (the original's differ by 2 mm). The model script builds the blocks from this table.
MASK = {
    "face-left": ((0.000, 0.490, -0.132), (0.104, 0.552, -0.098), "bark", 0.0, "y"),
    "face-right": ((-0.104, 0.490, -0.132), (0.000, 0.550, -0.098), "bark_lit", 0.0, "y"),
    "cheek-left": ((0.040, 0.548, -0.126), (0.106, 0.566, -0.098), "bark", 0.0, "x"),
    "cheek-right": ((-0.106, 0.548, -0.124), (-0.040, 0.566, -0.098), "bark", 0.0, "x"),
    "nose-ridge": ((-0.012, 0.548, -0.130), (0.012, 0.604, -0.104), "bark_lit", 0.0, "y"),
    "brow-left": ((0.004, 0.584, -0.142), (0.114, 0.608, -0.100), "bark", 4.0, "x"),
    "brow-right": ((-0.114, 0.584, -0.140), (-0.004, 0.608, -0.100), "bark", -4.0, "x"),
    "forehead": ((-0.058, 0.604, -0.126), (0.058, 0.672, -0.098), "bark_lit", 0.0, "y"),
    "forehead-side-left": ((0.060, 0.606, -0.118), (0.102, 0.662, -0.096), "bark", 4.0, "y"),
    "forehead-side-right": ((-0.102, 0.606, -0.116), (-0.060, 0.658, -0.096), "bark", -4.0, "y"),
}
NOSE_FOOT = 0.70    # the seam plank between the eyes narrows to this at its foot (the builder's taper)
VIEW_BIAS = {}

# name -> (px wide, px high at 2048, metres wide, metres high). u runs across, v UP.
_TONE = (36, 36, 0.04, 0.04)
WOOD_M = (0.40, 0.18)        # metres a wood swatch spans: along the grain, across it
STRAND_M = (0.34, 0.17)      # a braid strand unrolled: along it, round it
TONES = ["bark_tone", "bark_dark_tone", "bark_lit_tone", "bark_edge_tone", "bark_end_tone",
         "lit_edge_tone", "lit_end_tone", "core_tone", "root_tone", "root_edge_tone", "root_dark_tone",
         "moss_tone", "moss_dark_tone", "moss_lit_tone", "leaf_tone", "leaf_dark_tone", "leaf_under_tone",
         "vine_tone", "vine_dark_tone", "vine_lit_tone"]
SWATCHES = {name: _TONE for name in TONES}
SWATCHES.update({
    "wood": (720, 324, WOOD_M[0], WOOD_M[1]),
    "wood_lit": (720, 324, WOOD_M[0], WOOD_M[1]),
    "wood_root": (560, 252, WOOD_M[0], WOOD_M[1]),
    "strand_bark": (560, 200, STRAND_M[0], STRAND_M[1]),
    "strand_lit": (560, 200, STRAND_M[0], STRAND_M[1]),
    "strand_vine": (560, 200, STRAND_M[0], STRAND_M[1]),
    "strand_root": (360, 120, STRAND_M[0], STRAND_M[1]),
    "leaf": (150, 96, 0.07, 0.045),
    "leaf_dark": (150, 96, 0.07, 0.045),
    "leaf_moss": (150, 96, 0.07, 0.045),
    "moss_top": (160, 160, 0.08, 0.08),
})
for _name in SPECIAL:
    _w, _h = special_size(_name)
    SWATCHES[_name] = (int(round(_w * 2300)), int(round(_h * 2300)), _w, _h)

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin. A shelf pack into the top half."""
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
        """A filled patch. `points` are hand-set; the edge is feathered `feather` mm."""
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

    def cut(self, points, colour, width, lip=None):
        """A CARVED line: hard edged, square ended, with a thin lit lip under it (the far wall of
        the cut catching light). `points` are corners, joined straight."""
        if lip is not None:
            under = [(x, y - 0.55 * width / 1000.0 / self.unit[1]) for x, y in points]
            self.stroke(lip, under, width, 0.12, 0.9, (1.0, 1.0), curved=False)
        return self.stroke(colour, points, width, 0.12, 1.0, (1.0, 1.0), curved=False)

    unit = (1.0, 1.0)     # metres per island unit along each axis; a swatch sets its own

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE FACE: the core's front with the eyes, and the fronts of the planks in `MASK`.
# ---------------------------------------------------------------------------
CORE = mix(BARK_DARK, CARVE, 0.18)       # the core seen in a gap
GRAIN_W = 2.2                            # mm, an added grain line: few and chunky (owner on v04)


def _tilted(x0, y0, x1, y1, deg):
    """A rectangle turned `deg` about its own centre, a positive angle lifting its +x end."""
    cx, cy = 0.5 * (x0 + x1), 0.5 * (y0 + y1)
    t = math.radians(deg)
    out = []
    for x, y in ((x0, y0), (x1, y0), (x1, y1), (x0, y1)):
        dx, dy = x - cx, y - cy
        out.append((cx + dx * math.cos(t) - dy * math.sin(t), cy + dx * math.sin(t) + dy * math.cos(t)))
    return out


def _scaled(poly, s):
    cx, cy = EYE_CENTRE
    return [(s * (cx + (x - cx) * EYE_SCALE), cy + (y - cy) * EYE_SCALE) for x, y in poly]


def paint_head_front(c):
    """The core between the cheeks and the brow: each eye its dark socket, green rim and bright
    slit, measured off the original, his left and his right. Nothing else is drawn here."""
    for s in (1, -1):
        c.mark(SOCKET, _scaled(SOCKET_POLY, s), 0.12, 1.0, curved=False)
        c.mark(EYE_GLOW, _scaled(GLOW_POLY, s), 0.12, 1.0, curved=False)
        c.mark(EYE, _scaled(LIGHT_POLY, s), 0.12, 1.0, curved=False)


def paint_mask_front(c):
    """The planks' fronts, laid from the deepest to the proudest so that where two overlap in this
    view the one in front keeps its own tone. Each is ONE flat tone; the blocks give the seams."""
    tone = {"bark": BARK, "bark_lit": BARK_LIT}
    order = sorted(MASK, key=lambda name: -MASK[name][0][2])
    for name in order:
        lo, hi, kind, tilt, _ = MASK[name]
        pad = 0.003
        c.mark(tone[kind], _tilted(lo[0] - pad, lo[1] - pad, hi[0] + pad, hi[1] + pad, tilt), 0.0, 1.0, curved=False)
    grain = mix(BARK, BARK_DARK, 0.62)
    grain_lit = mix(BARK_LIT, BARK_DARK, 0.52)

    # THE CARVED MARKS, the original's own, in the original's places
    # his spiral on the forehead plank (SPIRAL_FOREHEAD, a 5 mm cut), with the lit far wall of the cut
    spiral = [(-0.030, 0.612), (-0.030, 0.662), (0.030, 0.662), (0.030, 0.624), (-0.016, 0.624),
              (-0.016, 0.650), (0.016, 0.650), (0.016, 0.636), (-0.002, 0.636)]
    c.stroke(mix(BARK_LIT, "ffffff", 0.10), [(x + 0.0010, y - 0.0016) for x, y in spiral], 5.0, 0.12, 1.0, (1.0, 1.0), curved=False)
    c.stroke(BARK_DARK, spiral, 5.0, 0.12, 1.0, (1.0, 1.0), curved=False)
    # one upright grain line down each lower face plank, not level with each other
    c.stroke(grain, [(0.0705, 0.502), (0.0725, 0.542)], 5.0, 0.12, 1.0, (1.0, 1.0), curved=False)
    c.stroke(grain_lit, [(-0.0635, 0.506), (-0.0645, 0.540)], 5.0, 0.12, 1.0, (1.0, 1.0), curved=False)
    # ADDED, and kept to a handful (v04's fifteen fine lines read busier than the original):
    # one long cut along each brow, one down each forehead side, one more on each lower plank
    for colour, pts in ((grain, [(0.026, 0.5925), (0.098, 0.5985)]),
                        (grain, [(-0.102, 0.5995), (-0.040, 0.5945)]),
                        (grain, [(0.083, 0.620), (0.085, 0.650)]),
                        (grain, [(-0.080, 0.618), (-0.082, 0.644)]),
                        (grain, [(0.026, 0.524), (0.027, 0.546)]),
                        (grain_lit, [(-0.090, 0.498), (-0.088, 0.530)])):
        c.stroke(colour, pts, GRAIN_W, 0.12, 1.0, (0.8, 0.5), curved=False)
    # the seam where the two lower planks meet, a carved line down the centre
    c.stroke(BARK_DARK, [(0.0, 0.488), (0.0, 0.552)], 2.4, 0.12, 1.0, (1.0, 1.0), curved=False)

    # THE MOUTH: one smooth stroke through the middles of the two carved cuts, 4 mm thick as they
    # are. The cuts do not sit level (his left end is 1 mm higher), which is the mouth "that
    # never quite decided to smile"; the stroke keeps that.
    c.stroke(mix(BARK_DARK, CARVE, 0.5), MOUTH_LINE, 4.4, 0.14, 1.0, (0.55, 0.55))


# ---------------------------------------------------------------------------
# WOOD. One long swatch a tone; the grain runs along u. A plank's face takes its own window of it,
# so no two planks show the same lines. Every line, streak and knot is set by hand.
# ---------------------------------------------------------------------------

def paint_wood(c, base, dark, lit):
    line = mix(base, dark, 0.62)
    soft = mix(base, dark, 0.30)
    pale = mix(base, lit, 0.55)
    # broad streaks first: the plank's own lighter and darker bands, hard edged
    c.mark(soft, [(0.00, 0.12), (0.34, 0.10), (0.62, 0.14), (1.00, 0.11), (1.00, 0.00), (0.00, 0.00)], 0.15, 0.55, curved=False)
    c.mark(pale, [(0.06, 0.40), (0.30, 0.37), (0.52, 0.41), (0.50, 0.47), (0.26, 0.44), (0.07, 0.46)], 0.15, 0.6)
    c.mark(pale, [(0.58, 0.72), (0.80, 0.69), (0.97, 0.73), (0.96, 0.78), (0.78, 0.76), (0.59, 0.79)], 0.15, 0.6)
    c.mark(soft, [(0.36, 0.84), (0.66, 0.87), (0.90, 0.85), (0.90, 0.90), (0.64, 0.93), (0.37, 0.90)], 0.15, 0.5)
    c.mark(pale, [(0.02, 0.63), (0.20, 0.61), (0.34, 0.64), (0.33, 0.68), (0.19, 0.66), (0.03, 0.68)], 0.15, 0.5)
    # the grain: long cuts that waver and break, each its own run
    for pts, w in (
            ([(0.00, 0.060), (0.22, 0.066), (0.47, 0.054), (0.71, 0.062), (1.00, 0.056)], 1.6),
            ([(0.04, 0.185), (0.26, 0.196), (0.44, 0.182)], 1.9),
            ([(0.55, 0.205), (0.76, 0.192), (0.98, 0.204)], 1.6),
            ([(0.00, 0.300), (0.18, 0.292), (0.37, 0.306), (0.60, 0.296)], 2.0),
            ([(0.70, 0.322), (0.84, 0.330), (1.00, 0.318)], 1.5),
            ([(0.12, 0.520), (0.33, 0.532), (0.58, 0.516), (0.80, 0.528)], 2.0),
            ([(0.86, 0.560), (1.00, 0.552)], 1.5),
            ([(0.00, 0.585), (0.10, 0.592)], 1.5),
            ([(0.40, 0.610), (0.62, 0.622), (0.78, 0.612)], 1.7),
            ([(0.00, 0.760), (0.21, 0.748), (0.44, 0.764)], 1.9),
            ([(0.26, 0.960), (0.50, 0.952), (0.74, 0.964), (1.00, 0.955)], 1.6),
            ([(0.00, 0.925), (0.16, 0.934)], 1.5)):
        c.stroke(line, pts, w, 0.12, 1.0, (0.35, 0.2))
    # a split, short and dark, and three knots of different sizes with the grain bending round them
    c.stroke(dark, [(0.640, 0.430), (0.700, 0.436), (0.770, 0.428)], 2.6, 0.12, 1.0, (0.2, 0.1))
    for (kx, ky), r in (((0.215, 0.250), 0.020), ((0.885, 0.455), 0.015), ((0.500, 0.720), 0.024)):
        ra, rb = r, r * WOOD_M[0] / WOOD_M[1] * 0.78
        c.blob(line, (kx, ky), ra * 1.5, rb * 1.5, 0.12, 1.0)
        c.blob(base, (kx, ky), ra * 1.12, rb * 1.12, 0.12, 1.0)
        c.blob(line, (kx, ky), ra * 0.72, rb * 0.72, 0.12, 1.0)
        c.blob(dark, (kx, ky), ra * 0.36, rb * 0.36, 0.12, 1.0)


def paint_strand(c, base, dark, lit, vine=False):
    """A braid strand unrolled: u from the elbow to the point, v once round it. Long fibres that
    run the strand's length and lean a little, so they read as a twist; a strand tapers, so the
    fibres crowd toward the point on their own."""
    line = mix(base, dark, 0.60)
    pale = mix(base, lit, 0.60)
    c.mark(pale, [(0.00, 0.58), (0.50, 0.66), (1.00, 0.74), (1.00, 0.86), (0.50, 0.78), (0.00, 0.70)], 0.15, 0.7, curved=False)
    c.mark(mix(base, dark, 0.28), [(0.00, 0.06), (0.50, 0.14), (1.00, 0.22), (1.00, 0.32), (0.50, 0.24), (0.00, 0.16)], 0.15, 0.7, curved=False)
    for pts, w in (
            ([(0.00, 0.02), (0.30, 0.07), (0.62, 0.12), (1.00, 0.18)], 1.8),
            ([(0.00, 0.36), (0.24, 0.40), (0.50, 0.44)], 1.6),
            ([(0.58, 0.455), (0.80, 0.49), (1.00, 0.52)], 1.6),
            ([(0.06, 0.52), (0.36, 0.565), (0.70, 0.62), (1.00, 0.665)], 1.8),
            ([(0.00, 0.88), (0.34, 0.935), (0.60, 0.975)], 1.6),
            ([(0.14, 0.20), (0.40, 0.245)], 1.4),
            ([(0.66, 0.90), (0.90, 0.94)], 1.4)):
        c.stroke(line, pts, w, 0.12, 1.0, (0.35, 0.2))
    if vine:
        # a green vine: leaf scars, small dark eyes where a leaf has dropped, each its own place
        for (kx, ky) in ((0.12, 0.30), (0.33, 0.80), (0.52, 0.18), (0.71, 0.70), (0.88, 0.34)):
            c.blob(dark, (kx, ky), 0.012, 0.034, 0.12, 1.0)
            c.blob(lit, (kx + 0.004, ky + 0.012), 0.005, 0.013, 0.12, 1.0)
    else:
        # bark: two growth rings round the strand and a knot
        c.stroke(line, [(0.300, 0.00), (0.306, 0.50), (0.300, 1.00)], 2.0, 0.12, 0.9, (1.0, 1.0))
        c.stroke(line, [(0.660, 0.00), (0.654, 0.50), (0.660, 1.00)], 1.8, 0.12, 0.9, (1.0, 1.0))
        c.blob(line, (0.460, 0.300), 0.020, 0.050, 0.12, 1.0)
        c.blob(dark, (0.460, 0.300), 0.008, 0.020, 0.12, 1.0)


def paint_leaf(c, base, vein, rim):
    """A leaf, stem at u 0 and point at u 1: a hard rim, a midrib, three veins a side, no two alike."""
    c.mark(rim, [(0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0)], 0.0, 1.0, curved=False)
    c.mark(base, [(0.02, 0.50), (0.26, 0.88), (0.66, 0.96), (0.97, 0.50), (0.66, 0.04), (0.26, 0.12)], 0.12, 1.0, curved=False)
    c.stroke(vein, [(0.02, 0.50), (0.50, 0.505), (0.95, 0.50)], 2.2, 0.12, 1.0, (1.0, 0.3))
    for pts in ([(0.22, 0.50), (0.34, 0.76)], [(0.44, 0.50), (0.58, 0.82)], [(0.64, 0.50), (0.78, 0.74)],
                [(0.26, 0.50), (0.40, 0.22)], [(0.48, 0.50), (0.62, 0.16)], [(0.68, 0.50), (0.80, 0.28)]):
        c.stroke(vein, pts, 1.3, 0.12, 1.0, (1.0, 0.3))


def paint_moss_top(c):
    """The top of a moss clump: tufts, hard edged dabs of the lit and the dark green, each placed."""
    for (x, y), r, tone in (((0.16, 0.20), 0.10, MOSS_LIT), ((0.48, 0.14), 0.08, MOSS_DARK), ((0.80, 0.24), 0.11, MOSS_LIT),
                            ((0.30, 0.52), 0.09, MOSS_DARK), ((0.64, 0.50), 0.12, MOSS_LIT), ((0.90, 0.62), 0.07, MOSS_DARK),
                            ((0.12, 0.80), 0.09, MOSS_LIT), ((0.44, 0.84), 0.10, MOSS_LIT), ((0.74, 0.86), 0.08, MOSS_DARK)):
        c.blob(tone, (x, y), r, r, 0.12, 1.0)


# ---------------------------------------------------------------------------
# THE ENGRAVED PLANKS. Each is its plank's own face, painted in the plank's own frame (`to_local`),
# with the original's cuts in the original's places (ENGRAVE_BODY in tools/build_paete_voxel.py).
# ---------------------------------------------------------------------------

def _grain_rows(c, base, dark, rows, upright=False):
    """Hand-set grain on a plank face. `rows` are (start, end, level) along the plank's grain."""
    line = mix(base, dark, 0.58)
    for a0, a1, b, w in rows:
        pts = [(a0, b), (0.5 * (a0 + a1), b + 0.012), (a1, b - 0.006)]
        if upright:
            pts = [(q, p) for p, q in pts]
        c.stroke(line, pts, w, 0.12, 1.0, (0.4, 0.25))


def _local_cut(c, name, world, width, colour, lip):
    pts = [to_local(name, x, y) for x, y in world]
    w, h = special_size(name)
    c.unit = (w, h)
    c.stroke(lip, [(u + 0.0008 / w, v - 0.0016 / h) for u, v in pts], width, 0.12, 1.0, (1.0, 1.0), curved=False)
    c.stroke(colour, pts, width, 0.12, 1.0, (1.0, 1.0), curved=False)


def paint_special(c, name):
    lit_plank = name in ("pec_left", "back_right", "small_back")
    base = BARK_LIT if lit_plank else BARK
    lip = mix(base, "ffffff", 0.14) if lit_plank else mix(BARK, BARK_LIT, 0.8)
    cut = BARK_DARK
    if name == "boss":
        # the diamond boss: the plank is turned 45 degrees, so its spiral is drawn in the WORLD's
        # upright and carried into the plank's frame (SPIRAL_BOSS, a 4 mm cut)
        _local_cut(c, name, [(-0.014, 0.372), (-0.014, 0.390), (0.012, 0.390), (0.012, 0.376), (-0.006, 0.376),
                             (-0.006, 0.384), (0.004, 0.384)], 4.0, cut, lip)
        return
    if name == "pauldron_spiral":
        _grain_rows(c, base, BARK_DARK, ((0.04, 0.30, 0.14, 1.5), (0.52, 0.96, 0.10, 1.5), (0.70, 0.98, 0.86, 1.5), (0.02, 0.22, 0.84, 1.5)))
        _local_cut(c, name, [(0.166, 0.460), (0.166, 0.496), (0.210, 0.496), (0.210, 0.468), (0.178, 0.468),
                             (0.178, 0.486), (0.198, 0.486)], 5.0, cut, lip)
        return
    if name == "pec_left":
        _grain_rows(c, base, BARK_DARK, ((0.04, 0.46, 0.16, 1.6), (0.56, 0.96, 0.22, 1.6), (0.10, 0.40, 0.86, 1.6), (0.62, 0.94, 0.80, 1.6),
                                         (0.30, 0.70, 0.36, 1.3)))
        c.blob(mix(base, BARK_DARK, 0.58), (0.80, 0.50), 0.045, 0.070, 0.12, 1.0)
        c.blob(base, (0.80, 0.50), 0.028, 0.044, 0.12, 1.0)
        _local_cut(c, name, [(0.0305, 0.455), (0.1505, 0.433)], 6.0, cut, lip)
        return
    if name == "pec_right":
        _grain_rows(c, base, BARK_DARK, ((0.06, 0.40, 0.84, 1.6), (0.50, 0.92, 0.88, 1.6), (0.44, 0.94, 0.14, 1.6), (0.04, 0.30, 0.42, 1.3)))
        _local_cut(c, name, [(-0.1525, 0.431), (-0.0325, 0.453)], 6.0, cut, lip)
        _local_cut(c, name, [(-0.129, 0.399), (-0.099, 0.391)], 6.0, cut, lip)
        return
    if name == "back_left":
        _grain_rows(c, base, BARK_DARK, ((0.08, 0.44, 0.20, 1.6), (0.56, 0.94, 0.30, 1.6), (0.60, 0.92, 0.82, 1.6), (0.06, 0.40, 0.46, 1.4)), upright=True)
        _local_cut(c, name, [(0.024, 0.423), (0.140, 0.431)], 6.0, cut, lip)
        return
    if name == "back_right":
        _grain_rows(c, base, BARK_DARK, ((0.10, 0.52, 0.16, 1.6), (0.62, 0.94, 0.24, 1.6), (0.52, 0.90, 0.78, 1.6), (0.08, 0.36, 0.66, 1.4)), upright=True)
        c.blob(mix(base, BARK_DARK, 0.58), (0.50, 0.78), 0.050, 0.055, 0.12, 1.0)
        c.blob(base, (0.50, 0.78), 0.030, 0.034, 0.12, 1.0)
        _local_cut(c, name, [(-0.136, 0.395), (-0.030, 0.389)], 6.0, cut, lip)
        return
    if name == "small_back":
        _grain_rows(c, base, BARK_DARK, ((0.04, 0.42, 0.18, 1.5), (0.56, 0.96, 0.84, 1.5)))
        _local_cut(c, name, [(-0.050, 0.2925), (0.040, 0.2965)], 5.0, cut, lip)
        return
    raise SystemExit("no drawing for " + name)


# ---------------------------------------------------------------------------

def build(size=ATLAS):
    """Every island, painted, as {name: Island}."""
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}

    def swatch(name, base):
        c = I(name, base, SWATCHES[name][2:4])
        done[name] = c
        return c

    c = I("head.front", CORE); paint_head_front(c); done[c.name] = c
    c = I("mask.front", BARK); paint_mask_front(c); done[c.name] = c

    tones = {
        "bark_tone": BARK, "bark_dark_tone": BARK_DARK, "bark_lit_tone": BARK_LIT,
        # a plank's chamfer is one flat tone, a step toward the light: the worn edge of cut wood
        "bark_edge_tone": mix(BARK, BARK_LIT, 0.62), "lit_edge_tone": mix(BARK_LIT, "ffffff", 0.13),
        # and its sawn end is a step toward the dark
        "bark_end_tone": mix(BARK, BARK_DARK, 0.42), "lit_end_tone": mix(BARK_LIT, BARK, 0.62),
        "core_tone": CORE,
        "root_tone": ROOT_C, "root_edge_tone": mix(ROOT_C, BARK, 0.6), "root_dark_tone": mix(ROOT_C, CARVE, 0.35),
        "moss_tone": MOSS, "moss_dark_tone": MOSS_DARK, "moss_lit_tone": MOSS_LIT,
        "leaf_tone": LEAF, "leaf_dark_tone": LEAF_DARK, "leaf_under_tone": mix(LEAF_DARK, MOSS_DARK, 0.55),
        "vine_tone": VINE, "vine_dark_tone": mix(VINE, MOSS_DARK, 0.7), "vine_lit_tone": mix(VINE, MOSS_LIT, 0.6),
    }
    for name, tone in tones.items():
        swatch(name, tone)

    paint_wood(swatch("wood", BARK), BARK, BARK_DARK, BARK_LIT)
    paint_wood(swatch("wood_lit", BARK_LIT), BARK_LIT, BARK, mix(BARK_LIT, "ffffff", 0.2))
    paint_wood(swatch("wood_root", ROOT_C), ROOT_C, mix(ROOT_C, CARVE, 0.5), BARK)
    paint_strand(swatch("strand_bark", BARK), BARK, BARK_DARK, BARK_LIT)
    paint_strand(swatch("strand_lit", BARK_LIT), BARK_LIT, BARK, mix(BARK_LIT, "ffffff", 0.2))
    paint_strand(swatch("strand_vine", VINE), VINE, MOSS_DARK, MOSS_LIT, vine=True)
    paint_strand(swatch("strand_root", ROOT_C), ROOT_C, mix(ROOT_C, CARVE, 0.5), BARK)
    paint_leaf(swatch("leaf", LEAF), LEAF, LEAF_DARK, mix(LEAF_DARK, MOSS_DARK, 0.3))
    paint_leaf(swatch("leaf_dark", LEAF_DARK), LEAF_DARK, mix(LEAF_DARK, LEAF, 0.7), MOSS_DARK)
    paint_leaf(swatch("leaf_moss", MOSS_LIT), MOSS_LIT, MOSS_DARK, MOSS_DARK)
    paint_moss_top(swatch("moss_top", MOSS))
    for name in SPECIAL:
        base = BARK_LIT if name in ("pec_left", "back_right", "small_back") else BARK
        paint_special(swatch(name, base), name)

    missing = set(n for n in layout(size) if isinstance(n, str)) - set(done)
    if missing:
        raise SystemExit("islands not painted: %s" % sorted(missing))
    return done


def main():
    from PIL import Image

    size = ATLAS
    if "--size" in sys.argv:
        size = int(sys.argv[sys.argv.index("--size") + 1])
    for hex_str in GREEN_HEXES:
        if _near_role_hue(hex_str):
            raise SystemExit("#%s sits near role hue #%s" % (hex_str, _near_role_hue(hex_str)))

    islands = build(size)
    # The bottom half is where Toon.shader reads the palette instead of this file. Left one flat tone.
    atlas = Image.new("RGB", (size, size), _rgb(BARK_DARK))
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
        except OSError:
            import time
            time.sleep(1.0)
    used = sum(r[2] * r[3] for n, r in layout(size).items() if isinstance(n, str))
    print("wrote %s  (%d x %d, %d islands, densities at %.0f%%, %.0f%% of the top half painted)"
          % (out, size, size, len(islands), 100.0 * _LAYOUT[(size, "scale")], 100.0 * used / (size * size / 2)))
    if "--sheet" in sys.argv:
        sheet = sys.argv[sys.argv.index("--sheet") + 1]
        atlas.crop((0, 0, size, size // 2)).save(sheet)
        print("wrote %s" % sheet)


if __name__ == "__main__":
    main()
