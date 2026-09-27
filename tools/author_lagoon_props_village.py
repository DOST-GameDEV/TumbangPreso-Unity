"""Lagoon Court VILLAGE PROP KIT, SIGNS AND LANTERNS: models AND their own painted textures.

  py -3 tools/author_lagoon_props_village.py --paint                     # paint every pv_ texture
  py -3 tools/author_lagoon_props_village.py --paint pv_signboard        # paint the named ones
  py -3 tools/author_lagoon_props_village.py --sheet N                   # swatch sheet vN
  blender -b --python tools/author_lagoon_props_village.py -- --preview N

The Blender run writes ArtSource/lagoon/lagoon_props_village.blend (the lineup, and a test land
house and sari-sari stall from tools/author_lagoon_houses.py with signs and lanterns on them) and,
with --preview N, Logs/lagoon-blender/propsvillage_{lineup,close,house,stall,swatches}_vN.png. It
paints any missing texture first (through `py -3`: Blender's Python has no PIL). An existing
render is never overwritten: bump N.

SCOPE. The village dressing was split across parallel kits (owner: one agent per prop group);
this file is the SIGNS AND LANTERNS group only. Placement on houses and along the court is the
lead's (the preview's `_preview_mount` is a test fixture, not an API).

WHY (docs/LAGOON_REWORK_GUIDE.md § 7a items 1 and 8, § 8 step 5). The reference (Papaioanou,
"Stylized Fishing Village", ArtStation GvJv5a) hangs painted shop signs (a crab, a fish, an
octopus) off its houses and stands lanterns on posts and by the doors; the cove had none. The
Filipino version: capiz-shell lanterns and a sari-sari sign.

  import author_lagoon_props_village as PV
  col = PV.build_prop(kind, seed)      # a Collection, not linked anywhere; one root empty
  seed = PV.sign_seed("crab", "wall")  # the sign seed for a motif and a mounting
  root = PV.place_prop(collection, kind, seed, matrix)   # a linked duplicate of the kit piece

STYLE (owner, 2026-09-27, on the first renders): "i dont like how details some of the props are.
again we're going for a stylized semi-cartoony environment style", and "experiment more with being
organic in how you shape things ... [not] just a straight rectangular prism". So every piece is a
FEW BIG, ROUND, SLIGHTLY IRREGULAR shapes: tapered, bent members; a thick warped plank with
rounded corners; a soft squircle lantern body; wide three-segment bevels; the painted texture
suggests the detail. v3 to v7 (thin lantern panels, twelve-sided corner posts, drips, finials, cap
strips, slim straight brackets, fine shell and rope patterning) is the rejected look.

KINDS, +Y is always the front:

  * "lantern_post"  ORIGIN AT THE GROUND CONTACT (the post runs 0.35 m into the ground). ONE thick
                    post (22 to 24 cm at the foot), tapered and slightly bent. Odd seeds: 2.3 to
                    2.45 m with one chunky tapered arm along +Y curling up at its tip, the lantern
                    on a short fat rope off it. Even seeds: 1.45 to 1.6 m, the lantern standing on
                    the post's swollen top. 5 to 7 parts.
  * "lantern_hang"  ORIGIN AT THE MOUNT: z = 0 is the underside of the eave or beam it hangs from;
                    its rope runs 5 cm UP into it. A lantern of FOUR parts (a rounded foot, a fat
                    squircle body bulging like a small barrel with one glowing painted panel a
                    side, a thick hat with a deep lip, a fat knob) on a 14 to 26 cm rope, about
                    0.75 m in all, 0.47 m across the hat. For an eave, a porch beam or a post arm.
  * "sign_hanging"  a THICK (8 cm) plank sign 0.9 x 0.5 m with rounded corners and a slight warp
                    (bowed along its length, a little twisted, per seed), a painted panel through
                    it standing 1.2 cm proud of both faces, hung on two fat ropes.
                    ODD seeds, WALL mount: ORIGIN AT THE MOUNT on the wall face (y = 0 is the wall
                    face; the wall plate runs 3.5 cm into the wall behind). A chunky tapered arm
                    1.1 m out along +Y curling up at its tip, one bent brace; the board hangs under
                    it EDGE-ON to the wall, faces toward +X and -X (so on a side wall near the
                    front corner it faces the street).
                    EVEN seeds, HUNG: ORIGIN AT THE MOUNT under an eave (z = 0), ropes 5 cm up into
                    it; the board hangs flat, facing +Y, its bottom 0.85 to 1.0 m under the mount.
                    MOTIF: seed s paints MOTIFS[(s - 1) // 2 % 8]; sign_seed(motif, mount) gives s.
                    fish, crab, shell, bangka, octopus, "SARI-SARI", a striped fish, a crab on
                    green: each a few big shapes with one thick outline.

  THE ROOT EMPTY carries prop_kind, prop_seed, prop_mount ("ground", "wall" or "hang"), prop_box
  (the local box that must stay clear of other geometry, fixings excluded: xmin, ymin, zmin, xmax,
  ymax, zmax; for placement tests) and prop_motif (signs) or prop_variant (posts).

MATERIALS. Shared by NAME with the house and boat kits: plank (plank_c) and timber (timber_a).
When the file already has one it is used as it is; otherwise it is built with
render_lagoon_texture_preview.uv_material around the texture in brackets. Every NEW surface wears
its own drawing (§ 2: never one generator dressed up as another), painted by this file into
ArtSource/lagoon/textures/pv_*_{albedo,height,normal}.png:

  pv_signboard      ATLAS, not world scale: eight 2:1 cells (512 x 256 px of a 1024 atlas), one
                    per motif: a bold motif on a sun-faded ground, a soft painted border, a soft
                    worn corner. The panel's +face maps into its cell, the back face into the same
                    cell mirrored (it reads the right way round), the rim samples the border paint.
                    cell_rect(i) gives cell i's UV rectangle.
  pv_capiz_lantern  ONE WHOLE PANEL, not a tile (each quarter of the lantern body maps it 0..1):
                    warm cream, a big soft glow in the middle, a few big soft shell arcs, a soft
                    darker margin. The material GLOWS: Unity, albedo x LANTERN_GLOW as base colour
                    and in the emission slot at about LANTERN_GLOW_STRENGTH.
  pv_rope           a broad soft two-strand twist of undyed fibre, world scale, V along the rope.

  Other UVs are "UVMap", WORLD SCALE, 1 unit = 2 m, like the house and boat kits (V along every
  member). One object per material slot, each with a live three-segment Bevel modifier (rope
  excepted: 6-sided, smooth shaded).

⚠️ ROLE HUES (Art_Direction.md § 1): nothing near offence orange #f87020 or defence blue #0080e8.
The lantern glows warm cream (hue about 35 degrees, saturation under half #f87020's), not amber;
the sign grounds are cream, crimson, leaf green and mustard; no sign paint is orange or blue.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2). Parts that meet penetrate (the body
into the foot and the hat, ropes into what they hang from and 7 cm into the board, the arm 2 cm
into the wall plate, the knob through the hat), the post sinks into the ground, and every end
inside another part sits at its own depth. The warped board and panel are subdivided before the
warp so their faces stay parallel. `check_prop` proves it per kind: `coplanar` counts overlapping
parallel faces of different shells within 4 mm, `floating` counts shells in no chain of
intersections to the mount (the ground, the wall plane or the eave plane).
"""
import math
import os
import random
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEX = Path(os.environ.get("LAGOON_TEX_OUT", ROOT / "ArtSource" / "lagoon" / "textures"))
APPROVED = ROOT / "ArtSource" / "lagoon" / "textures"
LOGS = ROOT / "Logs" / "lagoon-blender"
SOURCE = ROOT / "ArtSource" / "lagoon"

KINDS = ("sign_hanging", "lantern_post", "lantern_hang")
MOTIFS = ("fish", "crab", "shell", "bangka", "octopus", "sarisari", "fish_slim", "crab_green")
UV_METRES = 2.0
# Preview v3 at (1.0, 0.88, 0.66) x 1.6 read as white paper under AgX: warmer and dimmer now, hue
# about 37 degrees at half the saturation of offence orange #f87020 (hue 22): a candle, not amber.
# v11's sheet showed (1.0, 0.8, 0.52) x the cream panel landing at hue 32, saturation 0.59: amber,
# too near offence orange. Now saturation about 0.48 in the product, still warm cream on screen.
LANTERN_GLOW = (1.0, 0.86, 0.64)
LANTERN_GLOW_STRENGTH = 0.9

# ================================================================ painting (numpy, PIL; py -3)

SIZE = 1024
TILE_M = 2.0


def _np():
    import numpy as np
    return np


def hexcol(h):
    np = _np()
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], dtype=np.float32)


def grid(w=SIZE, h=SIZE, wm=TILE_M, hm=TILE_M):
    """Metre coordinates of every pixel: x across the image (U), y down it (V)."""
    np = _np()
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    return x * wm / w, y * hm / h


def field(sx_m, seed, sy_m=None, w=SIZE, h=SIZE, wm=TILE_M, hm=TILE_M):
    """A periodic smooth random field over the image, unit variance, features about `sx_m` across
    and `sy_m` down (so a fade can hang down a cloth or run along a log)."""
    np = _np()
    sy_m = sx_m if sy_m is None else sy_m
    white = np.random.default_rng(seed).standard_normal((h, w))
    fy = np.fft.fftfreq(h)[:, None] * (h / hm) * sy_m
    fx = np.fft.fftfreq(w)[None, :] * (w / wm) * sx_m
    g = np.exp(-(fx ** 2 + fy ** 2) * 2)
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * g))
    return ((n - n.mean()) / (n.std() + 1e-9)).astype(np.float32)


def smooth(a):
    return a * a * (3 - 2 * a)


def clamp01(a):
    return _np().clip(a, 0, 1)


def patch(scale_m, coverage, seed, feather=0.4, sy_m=None, **kw):
    """A feathered organic patch mask covering `coverage` of the image (the house style's second
    coat of paint): 0 outside, 1 inside."""
    np = _np()
    n = field(scale_m, seed, sy_m, **kw) + 0.25 * field(scale_m / 3, seed + 1, None if sy_m is None else sy_m / 3, **kw)
    t = np.quantile(n, 1 - coverage)
    return smooth(clamp01((n - t) / feather + 0.5))


def coat(img, scale, mask):
    """Paint `mask` of the surface toward img * scale."""
    return img * (1 - mask[..., None]) + img * scale * mask[..., None]


def over(img, colour, mask):
    return img * (1 - mask[..., None]) + colour * mask[..., None]


# ---------------------------------------------------------------- pv_capiz_lantern
# ONE WHOLE PANEL, not a tile: each face of the lantern's body maps it 0..1. OWNER, 2026-09-27: "a
# capiz lantern is a chunky rounded box with a soft glowing painted panel". So: warm cream, a big
# soft brighter glow in the middle, six big rounded shell shapes suggested by soft low-contrast
# arcs, a soft darker margin toward the frame. (v1 to v3 painted 2,700 little scales a tile: fine
# patterning the owner rejected.)
LANTERN_PX = 512


def pv_capiz_lantern():
    np = _np()
    n = LANTERN_PX
    x, y = grid(n, n, 1.0, 1.0)
    img = np.broadcast_to(hexcol("efdcbc"), (n, n, 3)).astype(np.float32).copy()
    glow = smooth(clamp01(1 - np.hypot((x - 0.5) / 0.42, (y - 0.52) / 0.48)))
    img = img + (np.minimum(img * 1.09, 1) - img) * glow[..., None]
    shade = np.zeros((n, n), np.float32)
    for r, cy in enumerate((0.3, 0.62)):
        for cx in ((0.2, 0.5, 0.8) if r == 0 else (0.35, 0.65)):
            d = np.hypot((x - cx) / 0.2, (y - cy) / 0.2)
            arc = smooth(clamp01(1 - np.abs(d - 1.0) / 0.12)) * (y > cy)
            shade = np.maximum(shade, arc)
    img = img * (1 - 0.06 * shade)[..., None]
    e = np.minimum(np.minimum(x, 1 - x), np.minimum(y, 1 - y))
    margin = smooth(clamp01(1 - e / 0.12))
    img = img * (1 - 0.1 * margin)[..., None]
    height = 0.5 + 0.3 * glow - 0.3 * shade - 0.2 * margin
    return img, height


# ---------------------------------------------------------------- pv_rope
# Two strands twisting round the rope: diagonal bands (V along the rope), each strand one flat
# lit face turning into a shaded one, a soft groove between.

def pv_rope():
    np = _np()
    x, y = grid()
    period = TILE_M / 30         # broad soft twists (v3: 2.5 cm, fine patterning)
    s = ((x * 0.9 + y) / period) % 1.0
    lit = smooth(clamp01((0.45 - np.abs(s - 0.35)) / 0.12))
    groove = smooth(clamp01(1 - np.minimum(s, 1 - s) / 0.16))
    img = np.broadcast_to(hexcol("c2a472"), (SIZE, SIZE, 3)).astype(np.float32).copy()
    img = img + (np.minimum(img * 1.1, 1) - img) * lit[..., None]
    img = img * (1 - 0.14 * groove)[..., None]
    img = coat(img, np.array([0.95, 0.94, 0.92]), patch(0.4, 0.2, 421))
    height = 1 - groove
    return img, height


# ---------------------------------------------------------------- pv_signboard (atlas)
# Research (hand-painted shop signs in stylized villages, Filipino carinderia and sari-sari
# signs): a sun-faded painted ground, a thick hand-drawn motif in one or two flat colours with a
# dark brown outline and one light band on its top, a painted border line inset from the edge,
# and the paint worn off at a corner or two. Everything wobbles a little: drawn, not ruled.
CELL_W, CELL_H = 512, 256
ATLAS = 1024
SIGN_CELLS = {  # motif: (ground, motif colour, outline)
    "fish": ("e8d8b0", "a8423a", "4a2a18"),
    "crab": ("a8423a", "efe0bc", "4a2a18"),
    "shell": ("5f8a4a", "efe0bc", "3a2a18"),
    "bangka": ("d4a73a", "5a3a24", "3a2414"),
    "octopus": ("e8d8b0", "8a3f6e", "4a2a18"),
    "sarisari": ("a8423a", "f2e4c0", "3a2014"),
    "fish_slim": ("d4a73a", "efe0bc", "3a2414"),
    "crab_green": ("5f8a4a", "b8453c", "2e2014"),
}


def cell_rect(index):
    """(u0, v0, u1, v1) of atlas cell `index` in UV space (v up, as Blender's UVs)."""
    c, r = index % 2, index // 2
    u0, u1 = c * 0.5, (c + 1) * 0.5
    v1 = 1 - r * 0.25
    return u0, v1 - 0.25, u1, v1


def _wobbly(pts, amp, seed):
    rng = random.Random(seed)
    ph = [rng.uniform(0, 6.28) for _ in range(3)]
    n = len(pts)
    out = []
    for i, (px, py) in enumerate(pts):
        t = i / max(1, n)
        out.append((px + amp * math.sin(6.28 * 2 * t + ph[0]) + amp * 0.5 * math.sin(6.28 * 5 * t + ph[1]),
                    py + amp * math.sin(6.28 * 3 * t + ph[2]) + amp * 0.4 * math.sin(6.28 * 7 * t + ph[0])))
    return out


def _ellipse(cx, cy, rx, ry, n=48, a0=0.0, a1=6.2832, squash=None):
    pts = []
    for k in range(n):
        t = a0 + (a1 - a0) * k / (n - 1 if a1 - a0 < 6.28 else n)
        yy = math.sin(t) * ry
        if squash:
            yy *= 1 - squash * math.cos(t)
        pts.append((cx + math.cos(t) * rx, cy + yy))
    return pts


def _draw_motif(d, name, fill, outline, S, seed, bg, mask=False):
    """Draw one motif into ImageDraw `d` at scale S (pixels of the 2x canvas per unit; the cell
    is 2 x 1 units, centre (1, 0.5)). Every shape: outline first (thick), fill inside, then one
    light band. `hi` is the fill lightened for the cel band."""
    if mask:
        fill, outline, bg, hi, pale = 255, 255, 0, 255, 255
    else:
        hi = tuple(min(255, int(c * 1.18 + 12)) for c in fill)
        pale = (240, 228, 200)
    lw = int(0.045 * S)      # one thick outline (v3: 0.028, thin and fussy)

    def poly(pts, col, amp=0.008, sd=0, width=lw):
        p = [(x * S, y * S) for x, y in _wobbly(pts, amp, seed + sd)]
        d.polygon(p, fill=col, outline=outline, width=width)

    def line(pts, width, col=outline, sd=0):
        p = [(x * S, y * S) for x, y in _wobbly(pts, 0.004, seed + sd)]
        d.line(p, fill=col, width=int(width * S), joint="curve")
        r = width * S / 2
        for x, y in (p[0], p[-1]):
            d.ellipse((x - r, y - r, x + r, y + r), fill=col)

    def dot(cx, cy, r, col):
        d.ellipse(((cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S), fill=col)

    # OWNER, 2026-09-27: "i dont like how details some of the props are ... stylized semi-cartoony".
    # So every motif is a FEW BIG SHAPES with one thick outline: no gill lines, leg joints, claw
    # notches, eye stalks, waves or thin rigging (v1 to v3 had all of those).
    if name in ("fish", "fish_slim"):
        slim = name == "fish_slim"
        ry = 0.24 if slim else 0.3
        poly([(1.38, 0.5), (1.74, 0.24), (1.74, 0.76)], fill, sd=3)
        poly(_ellipse(0.98, 0.5, 0.5, ry, squash=0.2), fill, sd=1)
        band = _ellipse(0.96, 0.5 - ry * 0.35, 0.34, ry * 0.3, n=32, a0=3.5, a1=5.9)
        d.line([(x * S, y * S) for x, y in band], fill=hi, width=int(0.07 * S), joint="curve")
        if slim:
            for k in range(2):
                x0 = 0.95 + k * 0.2
                line([(x0, 0.5 - ry * 0.75), (x0 + 0.04, 0.5 + ry * 0.75)], 0.06, sd=10 + k)
        dot(0.66, 0.46, 0.065, outline)
        dot(0.65, 0.445, 0.022, pale)
    elif name in ("crab", "crab_green"):
        # v10 read as a skull and crossbones: a pale oval with two dark eye dots ON it, legs
        # crossing under it. Now: a wide flat body with no face, two eye bumps standing ON TOP of
        # it, legs splayed out sideways and down, big raised claws.
        for s in (-1, 1):
            for k in range(3):
                x0 = 1.0 + s * (0.2 + 0.1 * k)
                line([(x0, 0.6), (x0 + s * 0.2, 0.66), (x0 + s * 0.24, 0.8)], 0.07, sd=20 + k)
            line([(1.0 + s * 0.3, 0.46), (1.0 + s * 0.46, 0.34), (1.0 + s * 0.5, 0.26)], 0.08, sd=24)
            poly(_ellipse(1.0 + s * 0.52, 0.2, 0.15, 0.12, n=24), fill, sd=30 + s)
            line([(1.0 + s * 0.1, 0.42), (1.0 + s * 0.12, 0.3)], 0.05, sd=40 + s)
            dot(1.0 + s * 0.12, 0.28, 0.055, outline)
            dot(1.0 + s * 0.12, 0.28, 0.035, pale)
        poly(_ellipse(1.0, 0.56, 0.4, 0.19, squash=-0.2), fill, sd=5)
        band = _ellipse(1.0, 0.5, 0.28, 0.09, n=24, a0=3.5, a1=5.9)
        d.line([(x * S, y * S) for x, y in band], fill=hi, width=int(0.06 * S), joint="curve")
    elif name == "shell":
        pts = [(1.0, 0.88)]
        for k in range(33):
            t = math.radians(200 + 140 * k / 32)
            r = 0.4 * (1 + 0.05 * abs(math.sin(k * math.pi / 6.4)))
            pts.append((1.0 + math.cos(t) * r * 1.15, 0.8 + math.sin(t) * r * 1.6))
        poly(pts, fill, sd=50)
        for k in range(4):
            t = math.radians(225 + 90 * k / 3)
            line([(1.0, 0.8), (1.0 + math.cos(t) * 0.3 * 1.15, 0.8 + math.sin(t) * 0.3 * 1.6)], 0.045, sd=60 + k)
    elif name == "bangka":
        line([(0.34, 0.7), (1.0, 0.62), (1.66, 0.7)], 0.07, sd=77)
        poly([(0.38, 0.4), (0.56, 0.62), (1.0, 0.7), (1.44, 0.62), (1.62, 0.4), (1.0, 0.54)], fill, sd=71)
        line([(1.0, 0.56), (1.0, 0.1)], 0.055, sd=73)
        poly([(1.05, 0.12), (1.42, 0.5), (1.05, 0.5)], pale, sd=74)
    elif name == "octopus":
        for k in range(4):
            x0 = 0.74 + k * 0.17
            curl = -1 if k < 2 else 1
            pts = [(x0, 0.5), (x0 - curl * 0.05, 0.7), (x0 + curl * 0.06, 0.86)]
            line(pts, 0.11, col=outline, sd=80 + k)
            line(pts, 0.07, col=fill, sd=80 + k)
        poly(_ellipse(1.0, 0.38, 0.34, 0.3, squash=-0.1), fill, sd=81)
        band = _ellipse(0.96, 0.29, 0.22, 0.14, n=24, a0=3.4, a1=5.8)
        d.line([(x * S, y * S) for x, y in band], fill=hi, width=int(0.07 * S), joint="curve")
        for s in (-1, 1):
            dot(1.0 + s * 0.12, 0.44, 0.07, pale)
            dot(1.0 + s * 0.12, 0.455, 0.035, outline)


def pv_signboard():
    np = _np()
    from PIL import Image, ImageDraw, ImageFont
    SC = 2
    atlas = np.zeros((ATLAS, ATLAS, 3), np.float32)
    hmap = np.zeros((ATLAS, ATLAS), np.float32)
    for i, name in enumerate(MOTIFS):
        bg, fg, ol = (tuple(int(h[k:k + 2], 16) for k in (0, 2, 4)) for h in SIGN_CELLS[name])
        W, H = CELL_W * SC, CELL_H * SC
        S = W / 2.0
        im = Image.new("RGB", (W, H), bg)
        mk = Image.new("L", (W, H), 0)
        d = ImageDraw.Draw(im)
        dm = ImageDraw.Draw(mk)
        # The inset border line: a lighter or darker tone of the ground, 6 % in from the edge.
        # A soft border a step off the ground (v3: 0.72 / 1.35, too contrasty).
        tone = tuple(int(c * 0.84) for c in bg) if sum(bg) > 400 else tuple(min(255, int(c * 1.18 + 14)) for c in bg)
        m = int(0.06 * H)
        d.rounded_rectangle((m, m, W - m, H - m), radius=int(0.08 * H), outline=tone, width=int(0.045 * H))
        dm.rounded_rectangle((m, m, W - m, H - m), radius=int(0.08 * H), outline=160, width=int(0.045 * H))
        if name == "sarisari":
            text = "SARI-SARI"
            layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            dl = ImageDraw.Draw(layer)
            # Sheet v1 overflowed the board: shrink the lettering until it fits 78 % of the width.
            size = int(H * 0.4)
            while True:
                font = None
                for f in ("segoeprb.ttf", "impact.ttf", "arialbd.ttf"):
                    try:
                        font = ImageFont.truetype(f, size)
                        break
                    except OSError:
                        continue
                bb = dl.textbbox((0, 0), text, font=font, stroke_width=int(H * 0.025))
                if bb[2] - bb[0] < 0.78 * W or size < 20:
                    break
                size -= 4
            tx, ty = (W - (bb[2] - bb[0])) / 2 - bb[0], (H - (bb[3] - bb[1])) / 2 - bb[1]
            dl.text((tx, ty), text, font=font, fill=fg + (255,), stroke_width=int(H * 0.025),
                    stroke_fill=ol + (255,))
            layer = layer.rotate(-2.5, resample=Image.BICUBIC, center=(W / 2, H / 2))
            im.paste(layer, (0, 0), layer)
            mk.paste(255, (0, 0), layer)
        else:
            _draw_motif(d, name, fg, ol, S, 900 + i * 17, bg)
            # the height mask: the same motif in white (raised paint) on the mask canvas
            _draw_motif(dm, name, fg, ol, S, 900 + i * 17, bg, mask=True)
        cell = np.asarray(im.resize((CELL_W, CELL_H), Image.LANCZOS), np.float32) / 255
        hm = np.asarray(mk.resize((CELL_W, CELL_H), Image.LANCZOS), np.float32) / 255
        # Paint coats and wear, in cell metres (the board is 0.8 x 0.4 m).
        kw = dict(w=CELL_W, h=CELL_H, wm=0.8, hm=0.4)
        cell = coat(cell, np.array([1.04, 1.035, 1.02]), patch(0.25, 0.3, 700 + i, feather=0.9, **kw))
        cell = coat(cell, np.array([0.95, 0.94, 0.93]), patch(0.2, 0.18, 720 + i, feather=0.9, **kw))
        # Worn to the wood at a corner or two: a feathered patch weighted to the board's edges.
        yy, xx = np.mgrid[0:CELL_H, 0:CELL_W].astype(np.float32)
        ex = np.minimum(xx, CELL_W - 1 - xx) / CELL_W
        ey = np.minimum(yy, CELL_H - 1 - yy) / CELL_H
        near = clamp01(1 - np.minimum(ex * 4, ey * 2) * 3)
        wear = patch(0.1, 0.07, 740 + i, feather=0.8, **kw) * near     # fewer, bigger, softer
        cell = over(cell, hexcol("b08a60"), wear * 0.6)
        hm = hm * (1 - wear) * 0.5 + 0.4 - 0.25 * wear
        c, r = i % 2, i // 2
        atlas[r * CELL_H:(r + 1) * CELL_H, c * CELL_W:(c + 1) * CELL_W] = cell
        hmap[r * CELL_H:(r + 1) * CELL_H, c * CELL_W:(c + 1) * CELL_W] = hm
    return atlas, hmap


# ---------------------------------------------------------------- saving and the sheet

PAINTERS = {"pv_signboard": pv_signboard, "pv_capiz_lantern": pv_capiz_lantern, "pv_rope": pv_rope}
# Normal-map strength per texture (the owner: depth and normal maps must read).
STRENGTH = {"pv_signboard": 2.5, "pv_capiz_lantern": 3.0, "pv_rope": 5.0}


def normal_from_height(h, strength):
    np = _np()
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.dstack([-gx, gy, np.ones_like(h)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def save(name, albedo, height):
    np = _np()
    from PIL import Image
    TEX.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(albedo, 0, 1) * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_albedo.png")
    r = np.ptp(height)
    h = (height - height.min()) / r if r > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((h * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_height.png")
    Image.fromarray((normal_from_height(h, STRENGTH[name]) * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_normal.png")
    print("[props-village] painted", name)


def paint(names=None):
    for n in names or PAINTERS:
        save(n, *PAINTERS[n]())


def sheet(version):
    """The review sheet: two approved neighbours (plank_c, timber_a) and the two tiling textures
    at one tile, a 3 x 3 repeat and a close crop; then the sign atlas whole, big; then the lantern
    shells as the material shows them lit (albedo x the glow colour)."""
    np = _np()
    from PIL import Image, ImageDraw, ImageFont
    out = LOGS / f"propsvillage_swatches_v{version}.png"
    if out.exists():
        raise SystemExit(f"{out} exists: renders are never overwritten, pick a new version")
    try:
        font = ImageFont.truetype("arial.ttf", 17)
    except OSError:
        font = ImageFont.load_default()
    cell, gap, head = 300, 16, 36
    tiles = [("plank_c (approved)", Image.open(APPROVED / "plank_c_albedo.png").convert("RGB")),
             ("timber_a (approved)", Image.open(APPROVED / "timber_a_albedo.png").convert("RGB")),
             ("pv_capiz_lantern (one whole panel)", Image.open(TEX / "pv_capiz_lantern_albedo.png").convert("RGB")),
             ("pv_rope (2 m)", Image.open(TEX / "pv_rope_albedo.png").convert("RGB"))]
    W = gap + 4 * (cell + gap)
    atlas = Image.open(TEX / "pv_signboard_albedo.png").convert("RGB")
    aw = W - 2 * gap
    ah = aw // 2
    H = gap + 3 * (head + cell) + head + ah + gap
    page = Image.new("RGB", (W, H), (245, 241, 234))
    draw = ImageDraw.Draw(page)
    for i, (label, tile) in enumerate(tiles):
        x = gap + i * (cell + gap)
        draw.text((x, gap), label, fill=(40, 36, 32), font=font)
        page.paste(tile.resize((cell, cell), Image.LANCZOS), (x, gap + head - 8))
        draw.text((x, gap + head + cell), "3 x 3 repeat", fill=(40, 36, 32), font=font)
        r3 = Image.new("RGB", (tile.width * 3, tile.height * 3))
        for a in range(3):
            for b in range(3):
                r3.paste(tile, (a * tile.width, b * tile.height))
        page.paste(r3.resize((cell, cell), Image.LANCZOS), (x, gap + 2 * head + cell - 8))
        crop = tile.crop((0, 0, tile.width // 8, tile.height // 8))
        if "capiz" in label:
            a = np.asarray(crop, np.float32) / 255
            crop = Image.fromarray((np.clip(a * np.array(LANTERN_GLOW) * 1.05, 0, 1) * 255).astype(np.uint8))
            lab = "25 cm crop, lit"
        else:
            lab = "25 cm crop"
        draw.text((x, gap + 2 * (head + cell)), lab, fill=(40, 36, 32), font=font)
        page.paste(crop.resize((cell, cell), Image.NEAREST if crop.width < 60 else Image.LANCZOS),
                   (x, gap + 3 * head + 2 * cell - 8))
    y = gap + 3 * (head + cell)
    draw.text((gap, y), "pv_signboard: atlas of 8 sign faces (a board is 0.8 x 0.4 m)", fill=(40, 36, 32), font=font)
    page.paste(atlas.resize((aw, ah), Image.LANCZOS), (gap, y + head - 8))
    LOGS.mkdir(parents=True, exist_ok=True)
    page.save(out)
    print("[props-village] sheet", out)


def texture_main(argv):
    if "--sheet" in argv:
        sheet(int(argv[argv.index("--sheet") + 1]))
        return
    if "--paint" in argv:
        rest = argv[argv.index("--paint") + 1:]
        paint(rest[0].split(",") if rest and not rest[0].startswith("--") else None)
        return
    print(__doc__)


# ================================================================ models (Blender)

try:
    import bpy
    import bmesh
    from mathutils import Matrix, Vector, noise
    from mathutils.bvhtree import BVHTree
    IN_BLENDER = True
except ImportError:
    IN_BLENDER = False

if IN_BLENDER:
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import author_lagoon_houses as H
    import render_lagoon_texture_preview as RP

    Z = Vector((0.0, 0.0, 1.0))
    X = Vector((1.0, 0.0, 0.0))
    Y = Vector((0.0, 1.0, 0.0))
    Piece = H.Piece
    beam, tube = H.beam, H.tube

    # Shared by NAME with the house and boat kits, and the texture each is built around when the
    # file does not have it yet (the cove's CHOSEN, LAGOON_REWORK_GUIDE.md § 8 step 3).
    SHARED = {"plank": "plank_c", "timber": "timber_a"}
    # Per slot: (bevel width m, angle above which an edge is sharp, harden normals).
    FINISH = dict(H.FINISH)
    # OWNER, 2026-09-27: "stylized semi-cartoony": chunky and ROUNDED. So this kit's timber and
    # plank carry wide three-segment bevels (the house kit's are 1.2 to 1.6 cm, two segments), and
    # the lantern body is a soft pillow.
    FINISH.update({"timber": (0.025, 30.0, False), "plank": (0.022, 30.0, False),
                   "pv_capiz_lantern": (0.035, 30.0, False), "pv_signboard": (0.01, 30.0, False),
                   "pv_rope": (0.0, 60.0, False)})
    SEGMENTS = 3

    class Prop:
        """Collects pieces per slot for one prop: the house kit's House, so the house kit's own
        primitives (beam, tube) build into it unchanged."""

        def __init__(self, kind, seed):
            self.kind, self.seed = kind, seed
            self.rng = random.Random(f"lagoon-prop-village:{kind}:{seed}")
            self.pieces = {}
            self.roof = []
            self.M = None
            self.info = {}
            self.mount = "ground"

        def add(self, slot, pc, roof=False):
            if not pc.bm.faces:
                pc.bm.free()
                return None
            if self.M is not None:
                bmesh.ops.transform(pc.bm, matrix=self.M, verts=pc.bm.verts[:])
            self.pieces.setdefault(slot, []).append(pc)
            return pc

        def j(self, a):
            return self.rng.uniform(-a, a)

        def uv_off(self):
            return (self.rng.random(), self.rng.random())

    # ------------------------------------------------------------ primitives

    def lathe(h, slot, prof, sides=16, c=(0, 0, 0), phase=None):
        """A surface of revolution about the local Z through `c`: `prof` is a list of (r, z) that
        starts and ends ON the axis (r = 0), so the shell closes at both poles and is manifold by
        construction. sides = 4 or 6 makes the lantern's square or hexagonal plates and hat.
        UVs: U round at the widest girth, V along the profile, both in metres / 2."""
        pc = Piece()
        bm, uv = pc.bm, pc.uv
        c = Vector(c)
        ph = h.rng.uniform(0, math.tau) if phase is None else phase
        rings = []
        for r, z in prof:
            if r < 1e-6:
                rings.append(bm.verts.new(c + Vector((0, 0, z))))
            else:
                rings.append([bm.verts.new(c + Vector((math.cos(ph + math.tau * k / sides) * r,
                                                       math.sin(ph + math.tau * k / sides) * r, z)))
                              for k in range(sides)])
        acc = [0.0]
        for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
            acc.append(acc[-1] + math.hypot(r1 - r0, z1 - z0))
        circ = math.tau * max(r for r, _ in prof) / UV_METRES
        ou, ov = h.uv_off()
        for i in range(len(prof) - 1):
            A, B = rings[i], rings[i + 1]
            v0, v1 = acc[i] / UV_METRES + ov, acc[i + 1] / UV_METRES + ov
            for k in range(sides):
                k2 = (k + 1) % sides
                u0, u1 = k / sides * circ + ou, (k + 1) / sides * circ + ou
                if isinstance(A, list) and isinstance(B, list):
                    f = bm.faces.new((A[k], A[k2], B[k2], B[k]))
                    uvs = ((u0, v0), (u1, v0), (u1, v1), (u0, v1))
                elif isinstance(A, list):
                    f = bm.faces.new((A[k], A[k2], B))
                    uvs = ((u0, v0), (u1, v0), ((u0 + u1) / 2, v1))
                else:
                    f = bm.faces.new((A, B[k2], B[k]))
                    uvs = (((u0 + u1) / 2, v0), (u1, v1), (u0, v1))
                for loop, t in zip(f.loops, uvs):
                    loop[uv].uv = t
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return h.add(slot, pc)

    def rope(h, p0, p1, r=0.02):     # a fat cartoon rope (v3 to v7: 1.1 cm)
        return tube(h, "pv_rope", p0, p1, r, sides=6)

    # OWNER, 2026-09-27: "i dont like how details some of the props are ... stylized semi-cartoony",
    # and "experiment more with being organic in how you shape things ... [not] just a straight
    # rectangular prism". So the kit is built from three ORGANIC primitives: a bent, tapered swept
    # member (posts, arms, braces), a rounded, slightly warped slab (the sign), and a squircle
    # lathe (the lantern: a soft fat box that bulges like a small barrel). Few parts, big, round,
    # slightly irregular.

    def swept(h, slot, pts, radii, sides=8):
        """A tapered member along a curved path: rings of `sides` round `pts` with `radii`, closed
        at both ends. Eight sides under the slot's wide bevel read as a soft, chunky round-square.
        UVs: V along the path, U round it, world scale."""
        pts = [Vector(p) for p in pts]
        pc = Piece()
        bm, uv = pc.bm, pc.uv
        n = len(pts)
        rings = []
        prev = None
        for i, p in enumerate(pts):
            t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
            side = H._perp(t)[0] if prev is None else prev - t * prev.dot(t)
            side.normalize()
            prev = side
            other = t.cross(side).normalized()
            r = radii[i]
            rings.append([bm.verts.new(p + side * (math.cos(math.tau * (k + 0.5) / sides) * r)
                                       + other * (math.sin(math.tau * (k + 0.5) / sides) * r))
                          for k in range(sides)])
        acc = [0.0]
        for a, b in zip(pts, pts[1:]):
            acc.append(acc[-1] + (b - a).length)
        s = 1.0 / UV_METRES
        circ = math.tau * max(radii) * s
        ou, ov = h.uv_off()
        for i in range(n - 1):
            for k in range(sides):
                k2 = (k + 1) % sides
                f = bm.faces.new((rings[i][k], rings[i][k2], rings[i + 1][k2], rings[i + 1][k]))
                for loop, (kk, ii) in zip(f.loops, ((k, i), (k + 1, i), (k + 1, i + 1), (k, i + 1))):
                    loop[uv].uv = (kk / sides * circ + ou, acc[ii] * s + ov)
        for ring in (rings[0], rings[-1]):
            f = bm.faces.new(ring)
            for loop in f.loops:
                loop[uv].uv = (ou, ov)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return h.add(slot, pc)

    def curve(p0, p1, bend, n=6):
        """Points from p0 to p1 bowed by the vector `bend` at the middle (a parabola)."""
        p0, p1, bend = Vector(p0), Vector(p1), Vector(bend)
        return [p0.lerp(p1, i / (n - 1)) + bend * (4 * (i / (n - 1)) * (1 - i / (n - 1))) for i in range(n)]

    def rounded_slab(h, slot, c, U, V, W, w, hgt, t, rad, warp, uvfn=None):
        """A slab w x hgt x t (along U, V, W) with ROUNDED corners of radius `rad`, then WARPED:
        `warp(u, v)` gives an offset along W for every vertex, so two slabs built with the same warp
        stay parallel (the sign's board and its painted panel). `uvfn(u, v, side)` maps the two
        big faces and the rim; without it, the house kit's world box projection."""
        c = Vector(c)
        outline = []
        for (cx, cy), a0 in (((w / 2 - rad, hgt / 2 - rad), 0), ((-w / 2 + rad, hgt / 2 - rad), 90),
                             ((-w / 2 + rad, -hgt / 2 + rad), 180), ((w / 2 - rad, -hgt / 2 + rad), 270)):
            for k in range(5):
                a = math.radians(a0 + 90 * k / 4)
                outline.append((cx + math.cos(a) * rad, cy + math.sin(a) * rad))
        pc = Piece()
        bm, uv = pc.bm, pc.uv
        front = [bm.verts.new(c + U * u + V * v + W * (t / 2)) for u, v in outline]
        back = [bm.verts.new(c + U * u + V * v - W * (t / 2)) for u, v in outline]
        ff = bm.faces.new(front)
        fb = bm.faces.new(list(reversed(back)))
        m = len(outline)
        rim = [bm.faces.new((front[i], back[i], back[(i + 1) % m], front[(i + 1) % m])) for i in range(m)]
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        if uvfn is None:
            H._uv_frame(pc, U, V, W, h.uv_off())
        else:
            for f, sd in ((ff, 1), (fb, -1)):
                for loop in f.loops:
                    q = loop.vert.co - c
                    loop[uv].uv = uvfn(q.dot(U), q.dot(V), sd)
            eu = uvfn(-w / 2 + 0.004, 0.0, 1)
            for f in rim:
                for loop in f.loops:
                    loop[uv].uv = eu
        # Triangulate the two big faces and cut every edge across them twice BEFORE warping:
        # warped as long outline-to-outline triangles, the faces were chords that sagged up to a
        # centimetre off the true bow, and the painted panel met the board (v8 check, 4 mm).
        big = [f for f in (ff, fb)]
        tri = bmesh.ops.triangulate(bm, faces=big)["faces"]
        inner = {e for f in tri for e in f.edges if all(g in tri for g in e.link_faces)}
        bmesh.ops.subdivide_edges(bm, edges=list(inner), cuts=2, use_grid_fill=False)
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
        for vtx in bm.verts:
            q = vtx.co - c
            vtx.co += W * warp(q.dot(U), q.dot(V))
        return h.add(slot, pc)

    def squircle_lathe(h, slot, prof, sides=16, c=(0, 0, 0), n=3.2, panel=False):
        """`lathe` with a SQUIRCLE section (|x|^n + |y|^n = r^n): a soft fat box, round at the
        corners, flat-ish on its faces. With `panel`, each quarter maps the whole painted panel
        once (U 0..1 across a face, V 0..1 up the profile), one glowing panel per side."""
        pc = Piece()
        bm, uv = pc.bm, pc.uv
        c = Vector(c)
        rings = []
        for r, z in prof:
            if r < 1e-6:
                rings.append(bm.verts.new(c + Vector((0, 0, z))))
                continue
            ring = []
            for k in range(sides):
                a = -math.pi / 4 + math.tau * k / sides
                ca, sa = math.cos(a), math.sin(a)
                f = r / (abs(ca) ** n + abs(sa) ** n) ** (1 / n)
                ring.append(bm.verts.new(c + Vector((ca * f, sa * f, z))))
            rings.append(ring)
        zs = [z for r, z in prof if r > 1e-6]
        zlo, zhi = min(zs), max(zs)
        acc = [0.0]
        for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
            acc.append(acc[-1] + math.hypot(r1 - r0, z1 - z0))
        circ = math.tau * max(r for r, _ in prof) / UV_METRES
        ou, ov = h.uv_off()
        q = sides // 4
        for i in range(len(prof) - 1):
            A, B = rings[i], rings[i + 1]
            for k in range(sides):
                k2 = (k + 1) % sides
                if panel:
                    u0, u1 = (k % q) / q, (k % q + 1) / q
                    v0 = min(1.0, max(0.0, (prof[i][1] - zlo) / (zhi - zlo)))
                    v1 = min(1.0, max(0.0, (prof[i + 1][1] - zlo) / (zhi - zlo)))
                else:
                    u0, u1 = k / sides * circ + ou, (k + 1) / sides * circ + ou
                    v0, v1 = acc[i] / UV_METRES + ov, acc[i + 1] / UV_METRES + ov
                if isinstance(A, list) and isinstance(B, list):
                    f = bm.faces.new((A[k], A[k2], B[k2], B[k]))
                    uvs = ((u0, v0), (u1, v0), (u1, v1), (u0, v1))
                elif isinstance(A, list):
                    f = bm.faces.new((A[k], A[k2], B))
                    uvs = ((u0, v0), (u1, v0), ((u0 + u1) / 2, v1))
                else:
                    f = bm.faces.new((A, B[k2], B[k]))
                    uvs = (((u0 + u1) / 2, v0), (u1, v1), (u0, v1))
                for loop, t in zip(f.loops, uvs):
                    loop[uv].uv = t
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return h.add(slot, pc)

    # ------------------------------------------------------------ lanterns

    def lantern(h, top, bh=None, foot=True):
        """A CHUNKY capiz lantern hanging from `top` (the top of its knob). FOUR parts: a thick
        rounded foot, a fat squircle body bulging like a small barrel and glowing with one painted
        panel a side, a thick rounded hat with a deep lip, and a fat knob. (v3 to v7: a hexagon of
        thin panels between plates, twelve-sided posts and a drip, fourteen parts: too detailed.)
        Returns the foot's underside z."""
        rng = h.rng
        R = rng.uniform(0.15, 0.165)            # half the body's width
        bh = bh or rng.uniform(0.3, 0.34)
        top = Vector(top)
        c = Vector((top.x, top.y, 0))
        z_tip = top.z - 0.07                    # the hat's tip, 7 cm under the knob's top
        zt = z_tip - 0.16                       # the hat lip's underside
        zb = zt - bh                            # the body's bottom
        z0 = zb - 0.035                         # the foot's underside
        # The body bulges 7 % at mid height; it runs 3 cm down into the foot and 3.5 cm up into
        # the hat, its ends clear of every face there.
        squircle_lathe(h, "pv_capiz_lantern", [(0, zb + 0.005), (R * 0.96, zb + 0.005), (R * 1.07, zb + bh * 0.5),
                                                (R * 0.96, zt + 0.035), (0, zt + 0.035)], c=c, panel=True)
        if foot:
            squircle_lathe(h, "timber", [(0, z0), (R + 0.035, z0), (R + 0.045, z0 + 0.03), (R + 0.03, zb + 0.03),
                                         (0, zb + 0.03)], c=c)
        # The hat: a deep rounded lip flaring 5 to 7 cm past the body, then a soft pyramid.
        squircle_lathe(h, "timber", [(0, zt), (R + 0.05, zt), (R + 0.075, zt + 0.05), (R * 0.55, z_tip - 0.035),
                                     (0.035, z_tip), (0, z_tip + 0.004)], c=c, n=2.6)
        lathe(h, "timber", [(0, z_tip - 0.04), (0.045, z_tip - 0.02), (0.05, z_tip + 0.03), (0.03, top.z - 0.005),
                            (0, top.z)], sides=10, c=c)
        return z0

    def build_lantern_hang(h):
        h.mount = "hang"
        L = h.rng.uniform(0.14, 0.26)
        rope(h, (0, 0, 0.05), (0, 0, -L - 0.03))
        z0 = lantern(h, (0, 0, -L))
        h.info.update(drop=-z0)

    def build_lantern_post(h):
        """ONE thick post, tapered and slightly bent, with a chunky arm curling up at its tip
        (odd seeds; the lantern on a short rope off it) or the lantern standing on its top (even
        seeds)."""
        rng = h.rng
        variant = "arm" if h.seed % 2 else "top"
        lean = Vector((h.j(1), h.j(1), 0)).normalized() * rng.uniform(0.04, 0.08)
        if variant == "arm":
            ph = rng.uniform(2.3, 2.45)
            path = curve((0, 0, -0.35), (0, 0, ph), lean, 7)
            swept(h, "timber", path, [0.11, 0.105, 0.1, 0.095, 0.09, 0.085, 0.08])
            a0 = path[-1] + Vector((0, -0.02, -0.2))
            arm = [a0, a0 + Vector((0, 0.25, 0.01)), a0 + Vector((0, 0.5, 0.0)), a0 + Vector((0, 0.66, 0.05)),
                   a0 + Vector((0, 0.74, 0.12))]
            swept(h, "timber", arm, [0.075, 0.07, 0.065, 0.06, 0.055])
            hang = a0 + Vector((0, 0.52, -0.01))
            rope(h, hang + Vector((0, 0, 0.03)), hang - Vector((0, 0, 0.27)))
            lantern(h, hang - Vector((0, 0, 0.24)))
        else:
            ph = rng.uniform(1.45, 1.6)
            path = curve((0, 0, -0.35), (0, 0, ph), lean, 6)
            swept(h, "timber", path, [0.12, 0.115, 0.11, 0.105, 0.1, 0.11])
            # The lantern stands on the post, its foot 3 cm down in the post's swollen top.
            top = path[-1]
            bh = rng.uniform(0.3, 0.34)
            z0 = top.z - 0.03
            lantern(h, (top.x, top.y, z0 + 0.035 + bh + 0.16 + 0.07), bh=bh)
        h.info.update(variant=variant, height=ph)

    # ------------------------------------------------------------ sign

    def sign_seed(motif, mount):
        """The sign_hanging seed that paints `motif` with `mount` "wall" (bracket) or "hang"."""
        return 2 * MOTIFS.index(motif) + (1 if mount == "wall" else 2)

    def build_sign(h):
        """A THICK plank sign (8 cm) with rounded corners and a slight warp (bowed along its
        length, a little twisted), one bold motif on a painted panel warped with it, on two fat
        ropes. Wall seeds: a chunky tapered arm curling up at the tip and one bent brace off a
        wall plate. (v3 to v7: a thin flat board, a cap strip, a finial, a slim straight bracket.)"""
        rng = h.rng
        motif = MOTIFS[((h.seed - 1) // 2) % len(MOTIFS)]
        cell = MOTIFS.index(motif)
        bw, bhh, bt = 0.9, 0.5, 0.08
        bow = rng.uniform(0.012, 0.022) * rng.choice((-1, 1))
        twist = rng.uniform(-0.03, 0.03)

        def warp(u, v):
            return bow * (1 - (2 * u / bw) ** 2) + twist * (u / bw) * (v / bhh) * 2
        if h.seed % 2:
            h.mount = "wall"
            beam(h, "timber", (0, 0.02, -0.5), (0, 0.02, 0.16), 0.18, 0.11, up=Y)
            # The arm starts 2 cm INSIDE the wall plate (v8: its end cap 5 mm off the plate's back).
            swept(h, "timber", [(0, 0.0, 0.0), (0, 0.35, 0.01), (0, 0.75, 0.0), (0, 1.02, 0.04), (0, 1.12, 0.11)],
                  [0.08, 0.075, 0.07, 0.065, 0.06])
            swept(h, "timber", curve((0, 0.05, -0.44), (0, 0.62, -0.02), (0, -0.05, 0.06), 5),
                  [0.055, 0.052, 0.05, 0.048, 0.046])
            top = -0.3
            for y in (0.36, 0.9):
                rope(h, (0, y, 0.03), (0, y, top - 0.07))
            c = Vector((0, 0.63, top - bhh / 2))
            U, V, W = Y, Z, X
        else:
            h.mount = "hang"
            top = -rng.uniform(0.35, 0.48)
            for x in (-0.32, 0.32):
                rope(h, (x, 0, 0.05), (x, 0, top - 0.07))
            c = Vector((0, 0, top - bhh / 2))
            U, V, W = X, Z, -Y
        # The board, and the painted panel through it, a bold 1.2 cm proud of both faces (at 6 mm the
        # warped faces' chords brushed the board, v9), warped alike.
        rounded_slab(h, "plank", c, U, V, W, bw, bhh, bt, 0.09, warp)
        u0, v0, u1, v1 = cell_rect(cell)
        pw, pht = bw - 0.1, bhh - 0.1
        du, dv = (u1 - u0) * 0.03, (v1 - v0) * 0.03

        def uvfn(u, v, sd):
            fu = (u / pw + 0.5) if sd > 0 else (0.5 - u / pw)
            fv = v / pht + 0.5
            return (u0 + du + fu * (u1 - u0 - 2 * du), v0 + dv + fv * (v1 - v0 - 2 * dv))
        rounded_slab(h, "pv_signboard", c, U, V, W, pw, pht, bt + 0.024, 0.06, warp, uvfn)
        h.info.update(motif=motif)

    # ------------------------------------------------------------ assembly

    BUILDERS = {"sign_hanging": build_sign, "lantern_post": build_lantern_post, "lantern_hang": build_lantern_hang}

    def _append(dst, duv, pc):
        vmap = {v: dst.verts.new(v.co) for v in pc.bm.verts}
        for f in pc.bm.faces:
            try:
                nf = dst.faces.new([vmap[v] for v in f.verts])
            except ValueError:
                continue
            for a, b in zip(f.loops, nf.loops):
                b[duv].uv = a[pc.uv].uv

    def build_prop(kind, seed=1):
        """Build one prop of `kind` (see KINDS) from `seed` and return its Collection, NOT linked to
        any scene: every part is parented to one root empty named by kind at the origin (the
        ground contact, or the mount for wall and hung props; front +Y). One mesh object per
        material slot, each with a live Bevel modifier (the rope excepted)."""
        if kind not in BUILDERS:
            raise ValueError(f"unknown prop kind {kind!r}; one of {KINDS}")
        h = Prop(kind, seed)
        BUILDERS[kind](h)
        col = bpy.data.collections.new(f"prop_{kind}_{seed}")
        root = bpy.data.objects.new(kind, None)
        root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.4
        col.objects.link(root)
        root["prop_kind"], root["prop_seed"], root["prop_mount"] = kind, seed, h.mount
        for k, v in h.info.items():
            if isinstance(v, (int, float, str)):
                root[f"prop_{k}"] = v
        lo = Vector((1e9, 1e9, 1e9))
        hi = -lo
        for slot, pcs in h.pieces.items():
            bm = bmesh.new()
            uvl = bm.loops.layers.uv.new("UVMap")
            for pc in pcs:
                _append(bm, uvl, pc)
                pc.bm.free()
            bm.normal_update()
            bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
            bm.normal_update()
            width, sharp_deg, harden = FINISH[slot]
            lim = math.radians(sharp_deg)
            for f in bm.faces:
                f.smooth = True
            for e in bm.edges:
                e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
            for v in bm.verts:
                for i in range(3):
                    lo[i] = min(lo[i], v.co[i])
                    hi[i] = max(hi[i], v.co[i])
            me = bpy.data.meshes.new(f"prop_{kind}_{seed}_{slot}")
            bm.to_mesh(me)
            bm.free()
            me.materials.append(material(slot))
            ob = bpy.data.objects.new(me.name, me)
            ob.parent = root
            col.objects.link(ob)
            if width > 0:
                bev = ob.modifiers.new("Bevel", "BEVEL")
                bev.width, bev.segments, bev.limit_method = width, SEGMENTS, "ANGLE"
                bev.angle_limit = math.radians(30.0)
                bev.harden_normals = harden
                bev.use_clamp_overlap = True
        # The clear box: the prop without its fixings (wall props from 1 cm off the wall, hung
        # props from 8 cm under the mount, ground props from 5 cm over the ground).
        if h.mount == "wall":
            lo.y = max(lo.y, 0.01)
        elif h.mount == "hang":
            hi.z = min(hi.z, -0.08)
        else:
            lo.z = max(lo.z, 0.05)
        root["prop_box"] = [lo.x, lo.y, lo.z, hi.x, hi.y, hi.z]
        return col

    # ------------------------------------------------------------ materials

    def _find(nt, kind):
        return next((n for n in nt.nodes if n.bl_idname == kind), None)

    def material(name):
        """A material by name, created once. Shared kit names are reused as they are when the file
        has them, else built on their chosen texture; pv_ ones on their own drawing, through the
        same uv_material the house kit's review uses (albedo, normal, height as bump)."""
        m = bpy.data.materials.get(name)
        if m:
            return m
        m = bpy.data.materials.new(name)
        if name in SHARED:
            RP.uv_material(m, SHARED[name])
            return m
        RP.uv_material(m, name)
        nt = m.node_tree
        # Bump depth for small props: uv_material's 3 cm is a roof's; a sign's paint is 3 mm.
        _find(nt, "ShaderNodeDisplacement").inputs["Scale"].default_value = \
            {"pv_signboard": 0.003, "pv_capiz_lantern": 0.003, "pv_rope": 0.006}[name]
        b = _find(nt, "ShaderNodeBsdfPrincipled")
        img = next(n for n in nt.nodes if n.bl_idname == "ShaderNodeTexImage" and n.image.name.endswith("_albedo.png"))
        b.inputs["Roughness"].default_value = 0.5 if name == "pv_capiz_lantern" else 0.85
        if name == "pv_capiz_lantern":
            mix = nt.nodes.new("ShaderNodeMix")
            mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
            mix.inputs["Factor"].default_value = 1.0
            nt.links.new(img.outputs["Color"], mix.inputs[6])
            mix.inputs[7].default_value = (*LANTERN_GLOW, 1)
            nt.links.new(mix.outputs[2], b.inputs["Emission Color"])
            # Lit from inside, the shell itself looks warm too (v4 read as white paper by day).
            nt.links.new(mix.outputs[2], b.inputs["Base Color"])
            b.inputs["Emission Strength"].default_value = LANTERN_GLOW_STRENGTH
        return m

    # ------------------------------------------------------------ checks

    def check_prop(col):
        """Tri counts (base and with the bevels), non-manifold edges, COPLANAR overlaps (parallel
        faces of different shells within 4 mm over each other's interior) and FLOATING shells
        (shells in no chain of intersections to the mount: the ground z <= 0.01, the wall plane
        y <= 0.005, or the eave plane z >= -0.005)."""
        root = next(o for o in col.objects if o.parent is None)
        mount = root.get("prop_mount", "ground")
        V, P, shell, slot_of = [], [], [], []
        grounded_s = set()
        out = {"tris": 0, "tris_bevelled": 0, "nonmanifold": {}}
        sid = 0
        dg = bpy.context.evaluated_depsgraph_get()
        for ob in col.objects:
            if ob.type != "MESH":
                continue
            me = ob.data
            slot = ob.name.split(f"_{root['prop_seed']}_", 1)[-1]
            out["tris"] += sum(len(p.vertices) - 2 for p in me.polygons)
            ev = ob.evaluated_get(dg)
            em = ev.to_mesh()
            out["tris_bevelled"] += sum(len(p.vertices) - 2 for p in em.polygons)
            ev.to_mesh_clear()
            bm = bmesh.new()
            bm.from_mesh(me)
            nm = sum(1 for e in bm.edges if not e.is_manifold)
            if nm:
                out["nonmanifold"][slot] = nm
            bm.free()
            parent = list(range(len(me.vertices)))

            def find(a):
                while parent[a] != a:
                    parent[a] = parent[parent[a]]
                    a = parent[a]
                return a
            for e in me.edges:
                ra, rb = find(e.vertices[0]), find(e.vertices[1])
                if ra != rb:
                    parent[ra] = rb
            roots = {}
            base_v = len(V)
            V.extend(v.co.copy() for v in me.vertices)
            for p in me.polygons:
                r = find(p.vertices[0])
                if r not in roots:
                    roots[r] = sid
                    sid += 1
                s = roots[r]
                P.append([base_v + i for i in p.vertices])
                shell.append(s)
                slot_of.append(slot)
                for i in p.vertices:
                    q = V[base_v + i]
                    if (mount == "ground" and q.z <= 0.01) or (mount == "wall" and q.y <= 0.005) or \
                            (mount == "hang" and q.z >= -0.005):
                        grounded_s.add(s)
        out["shells"] = sid
        tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
        normals = [(V[p[1]] - V[p[0]]).cross(V[p[2]] - V[p[0]]).normalized() for p in P]
        pairs = {}
        for i, p in enumerate(P):
            n = normals[i]
            if n.length < 0.5:
                continue
            c = sum((V[k] for k in p), Vector()) / len(p)
            for q in [c] + [c.lerp(V[k], 0.7) for k in p]:
                for _loc, nrm, idx, _dist in tree.find_nearest_range(q, 0.004):
                    if idx is not None and shell[idx] != shell[i] and abs(nrm.dot(n)) > 0.999:
                        key = (min(shell[i], shell[idx]), max(shell[i], shell[idx]))
                        pairs.setdefault(key, (slot_of[i], slot_of[idx], tuple(round(x, 3) for x in c)))
        out["coplanar"] = len(pairs)
        out["coplanar_examples"] = sorted(pairs.values())[:6]
        par = list(range(sid))

        def f2(a):
            while par[a] != a:
                par[a] = par[par[a]]
                a = par[a]
            return a
        for a, b in tree.overlap(tree):
            sa, sb = shell[a], shell[b]
            if sa != sb:
                ra, rb = f2(sa), f2(sb)
                if ra != rb:
                    par[ra] = rb
        grounded = {f2(s) for s in grounded_s}
        floating = [s for s in range(sid) if f2(s) not in grounded]
        out["floating"] = len(floating)
        out["floating_examples"] = sorted({slot_of[i] for i, s in enumerate(shell) if s in floating})[:6]
        xs, ys, zs = [v.x for v in V], [v.y for v in V], [v.z for v in V]
        out["bbox"] = tuple(round(v, 2) for v in (min(xs), min(ys), min(zs), max(xs), max(ys), max(zs)))
        return out

    # ------------------------------------------------------------ kit and linked duplicates

    KIT_NAME = "Props village kit (source, not placed)"
    _SOURCES = {}

    def kit_collection():
        """The hidden collection holding one built source per (kind, seed); placed props are
        LINKED DUPLICATES of these (shared meshes, editable: the cove's house pattern)."""
        col = bpy.data.collections.get(KIT_NAME)
        if col is None:
            col = bpy.data.collections.new(KIT_NAME)
            col.hide_render = True
            col.hide_viewport = True
        if col.name not in bpy.context.scene.collection.children:
            bpy.context.scene.collection.children.link(col)
        return col

    def prop_source(kind, seed):
        key = (kind, seed)
        col = _SOURCES.get(key)
        if col is None:
            col = bpy.data.collections.get(f"prop_{kind}_{seed}") or build_prop(kind, seed)
            kit = kit_collection()
            if col.name not in kit.children:
                kit.children.link(col)
            _SOURCES[key] = col
        return col

    def place_prop(collection, kind, seed, matrix):
        """A linked duplicate of prop (kind, seed) under a new root at world `matrix`, built into
        the hidden kit collection on first use."""
        src = prop_source(kind, seed)
        sroot = next(o for o in src.objects if o.parent is None)
        root = bpy.data.objects.new(kind, None)
        root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.4
        for k, v in sroot.items():
            root[k] = v
        collection.objects.link(root)
        root.matrix_world = matrix
        for ob in src.objects:
            if ob is sroot:
                continue
            dup = ob.copy()
            collection.objects.link(dup)
            dup.parent = root
            dup.matrix_parent_inverse = Matrix.Identity(4)
            dup.matrix_basis = ob.matrix_basis.copy()
        return root

    # ------------------------------------------------------------ preview

    def _plain_mat(name, colour, rough=0.85):
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (*colour, 1)
        b.inputs["Roughness"].default_value = rough
        return m

    def _box_obj(name, lo, hi, mat, coll):
        me = bpy.data.meshes.new(name)
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co = Vector(tuple(lo[i] + (v.co[i] + 0.5) * (hi[i] - lo[i]) for i in range(3)))
        bm.to_mesh(me)
        bm.free()
        me.materials.append(mat)
        o = bpy.data.objects.new(name, me)
        coll.objects.link(o)
        return o

    def _scale_ref(coll, at):
        me = bpy.data.meshes.get("scale_ref_1m60")
        if me is None:
            me = bpy.data.meshes.new("scale_ref_1m60")
            bm = bmesh.new()
            bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
            bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
            bm.to_mesh(me)
            bm.free()
            me.materials.append(_plain_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
        o = bpy.data.objects.new("scale_ref_1m60", me)
        o.location = at
        coll.objects.link(o)

    def _preview_scene():
        scene = bpy.context.scene
        g = bpy.data.meshes.new("ground")
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=200)
        bm.to_mesh(g)
        bm.free()
        g.materials.append(_plain_mat("ground_pale", (0.62, 0.55, 0.42)))
        scene.collection.objects.link(bpy.data.objects.new("ground", g))
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
        sun.data.energy, sun.data.color = 4.2, (1.0, 0.88, 0.72)
        sun.data.angle = math.radians(3)
        sun.rotation_euler = (math.radians(50), 0, math.radians(-145))
        scene.collection.objects.link(sun)
        world = bpy.data.worlds.new("world")
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.6, 0.66, 1)
        world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
        scene.world = world
        cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
        scene.collection.objects.link(cam)
        scene.camera = cam
        scene.render.engine = "BLENDER_EEVEE"
        scene.render.resolution_x, scene.render.resolution_y = 1600, 900
        scene.view_settings.view_transform = "AgX"
        for look in ("AgX - Punchy", "Punchy"):
            try:
                scene.view_settings.look = look
                break
            except TypeError:
                continue
        return cam

    def _house_tree(house_root):
        V, P = [], []
        for ob in house_root.children_recursive:
            if ob.type != "MESH":
                continue
            base = len(V)
            V.extend(ob.matrix_world @ v.co for v in ob.data.vertices)
            P.extend([base + i for i in p.vertices] for p in ob.data.polygons)
        return BVHTree.FromPolygons(V, P, epsilon=0.0)

    def _preview_mount(coll, house_root, tree, kind, seed, at, direction, heading):
        """TEST FIXTURE for the render, not the placement API (the lead writes placement): cast
        from `at` (house-local) along `direction` (house-local), mount the prop on the first
        surface hit, turned by `heading` in the house's frame."""
        M = house_root.matrix_world
        R3 = M.to_3x3().normalized()
        hit = tree.ray_cast(M @ Vector(at), R3 @ Vector(direction), 5.0)
        if hit[0] is None:
            print(f"[props-village] preview mount missed: {kind} {seed} at {at}")
            return None
        m = Matrix.Translation(hit[0] - (R3 @ Vector(direction)).normalized() * 0.002) @ \
            Matrix.Rotation(M.to_euler().z + heading, 4, "Z")
        return place_prop(coll, kind, seed, m)

    # The lineup, fronts toward +Y where every camera stands (the house kit's rule). Wall signs
    # stand against a display wall (its face at y = WALL_Y) and hung props under a display beam
    # (its underside at z = 2.6): preview furniture standing in for a house's wall and eave.
    WALL_Y = -3.0
    LINE_WALL = [("sign_hanging", sign_seed(m, "wall")) for m in MOTIFS]
    LINE_HANG = [("sign_hanging", sign_seed("sarisari", "hang")), ("lantern_hang", 1),
                 ("sign_hanging", sign_seed("crab", "hang")), ("lantern_hang", 2),
                 ("sign_hanging", sign_seed("octopus", "hang")), ("lantern_hang", 3)]
    LINE_POST = [("lantern_post", 1), ("lantern_post", 2), ("lantern_post", 3), ("lantern_post", 4)]

    def main():
        argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
        version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
        missing = [n for n in PAINTERS if not (TEX / f"{n}_albedo.png").exists()]
        if missing:
            subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--paint", ",".join(missing)], check=True)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        # Textured house materials first: the house kit reuses materials by name, so the test
        # houses wear the chosen textures too.
        for slot, tex in (("plank", "plank_c"), ("timber", "timber_a"), ("bamboo", "bamboo_a"), ("thatch", "thatch_a"),
                          ("tin", "tin_b"), ("sawali", "sawali_a"), ("capiz", "capiz"), ("paint_white", "lime_plaster"),
                          ("cloth", "cloth")):
            if not bpy.data.materials.get(slot) and (APPROVED / f"{tex}_albedo.png").exists():
                RP.uv_material(bpy.data.materials.new(slot), tex)
        scene = bpy.context.scene
        lineup = bpy.data.collections.new("props_village_lineup")
        scene.collection.children.link(lineup)
        # Bracket signs on display pillars facing +X (turned so each board faces the camera, as on
        # a side wall near a house's front corner); v3 showed them edge-on against one wall.
        wall_m = _plain_mat("display_wall", (0.78, 0.72, 0.62))
        beam_m = _plain_mat("display_beam", (0.45, 0.32, 0.2))
        for i, (kind, seed) in enumerate(LINE_WALL):
            x = -10.6 + i * 1.4
            _box_obj("display_pillar", (x - 0.3, WALL_Y, 0), (x, WALL_Y + 0.8, 2.9), wall_m, lineup)
            place_prop(lineup, kind, seed, Matrix.Translation((x, WALL_Y + 0.4, 2.1)) @ Matrix.Rotation(-math.pi / 2, 4, "Z"))
        # Hung signs and lanterns under a display beam, lantern posts in front of it.
        for i, (kind, seed) in enumerate(LINE_HANG):
            place_prop(lineup, kind, seed, Matrix.Translation((2.4 + i * 1.3, WALL_Y + 0.4, 2.6)))
        _box_obj("display_beam", (1.5, WALL_Y + 0.15, 2.6), (9.8, WALL_Y + 0.65, 2.78), beam_m, lineup)
        for x in (1.6, 9.7):
            _box_obj("display_beam_post", (x - 0.08, WALL_Y + 0.32, 0), (x + 0.08, WALL_Y + 0.48, 2.62), beam_m, lineup)
        for i, (kind, seed) in enumerate(LINE_POST):
            place_prop(lineup, kind, seed, Matrix.Translation((3.0 + i * 1.8, WALL_Y + 2.6, 0.0)))
        _scale_ref(lineup, (0.6, WALL_Y + 1.2, 0))
        print("[props-village] checks:")
        tot = {"coplanar": 0, "floating": 0, "nonmanifold": 0}
        for (kind, seed), src in sorted(_SOURCES.items()):
            c = check_prop(src)
            r = next(o for o in src.objects if o.parent is None)
            info = {k[5:]: (round(v, 2) if isinstance(v, float) else v) for k, v in r.items()
                    if k.startswith("prop_") and k not in ("prop_box", "prop_kind", "prop_seed")}
            print(f"[props-village] {kind} {seed}: tris {c['tris']} ({c['tris_bevelled']} bevelled), shells "
                  f"{c['shells']}, coplanar {c['coplanar']} {c['coplanar_examples']}, floating {c['floating']} "
                  f"{c['floating_examples']}, nonmanifold {c['nonmanifold']}, bbox {c['bbox']}, {info}")
            tot["coplanar"] += c["coplanar"]
            tot["floating"] += c["floating"]
            tot["nonmanifold"] += sum(c["nonmanifold"].values())
        print("[props-village] totals", tot)
        # The test houses: a land house and a sari-sari stall, with signs and lanterns mounted by
        # ray casting against their own meshes (so the mounts sit on real surfaces).
        test = bpy.data.collections.new("props_village_house_test")
        scene.collection.children.link(test)
        dressed = bpy.data.collections.new("props_village_house_props")
        scene.collection.children.link(dressed)
        houses = {}
        for kind, seed, at in (("land", 2, (0.0, -30.0, 0.0)), ("stall", 1, (-14.0, -30.0, 0.0))):
            hc = H.build_house(kind, seed)
            test.children.link(hc)
            root = next(o for o in hc.objects if o.parent is None)
            root.location = at
            houses[kind] = root
        bpy.context.view_layer.update()
        land = houses["land"]
        W, D, F = (float(land[k]) for k in ("house_W", "house_D", "house_F"))
        t = _house_tree(land)
        placed = [
            # a bracket sign on the right side wall near the front corner, facing the street
            _preview_mount(dressed, land, t, "sign_hanging", sign_seed("crab", "wall"), (W / 2 + 2, D / 2 - 0.4, F + 1.55),
                           (-1, 0, 0), -math.pi / 2),
            # lanterns under the front eave, either side of the deck
            _preview_mount(dressed, land, t, "lantern_hang", 2, (-1.2, D / 2 + 0.5, F + 1.2), (0, 0, 1), 0.0),
            _preview_mount(dressed, land, t, "lantern_hang", 3, (1.6, D / 2 + 0.5, F + 1.2), (0, 0, 1), 0.3),
        ]
        placed.append(place_prop(dressed, "lantern_post", 1, Matrix.Translation(land.matrix_world @ Vector((W / 2 + 0.3, D / 2 + 3.2, 0)))))
        placed.append(place_prop(dressed, "lantern_post", 2, Matrix.Translation(land.matrix_world @ Vector((-W / 2 - 0.2, D / 2 + 2.9, 0)))))
        stall = houses["stall"]
        W2, D2, F2 = (float(stall[k]) for k in ("house_W", "house_D", "house_F"))
        t2 = _house_tree(stall)
        placed.append(_preview_mount(dressed, stall, t2, "sign_hanging", sign_seed("sarisari", "wall"),
                                     (W2 / 2 + 2, D2 / 2 - 0.35, F2 + 1.9), (-1, 0, 0), -math.pi / 2))
        placed.append(_preview_mount(dressed, stall, t2, "lantern_hang", 1, (-W2 / 2 - 0.25, D2 / 2 + 0.2, F2 + 1.5),
                                     (0, 0, 1), 0.0))
        print("[props-village] test house props:", [p["prop_kind"] for p in placed if p])
        SOURCE.mkdir(parents=True, exist_ok=True)
        out = SOURCE / "lagoon_props_village.blend"
        bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
        bk = out.with_suffix(".blend1")
        if bk.exists():
            bk.unlink()
        print("[props-village] saved", out)
        if not version:
            return
        cam = _preview_scene()
        _scale_ref(test, (5.0, -22.5, 0))
        _scale_ref(test, (-9.6, -27.6, 0))
        shots = [("lineup", (-0.5, 13.0, 4.0), (-0.5, WALL_Y, 1.5), 24),
                 ("close", (-7.3, 1.6, 2.1), (-7.3, WALL_Y, 1.75), 30),
                 ("closeb", (-1.7, 1.6, 2.1), (-1.7, WALL_Y, 1.75), 30),
                 ("closec", (5.8, 3.6, 2.2), (5.8, WALL_Y, 1.6), 30),
                 ("house", (7.5, -17.5, 3.0), (0.8, -27.5, 2.0), 30),
                 ("stall", (-9.0, -23.0, 2.4), (-13.8, -29.0, 1.8), 30)]
        LOGS.mkdir(parents=True, exist_ok=True)
        for tag, pos, tgt, lens in shots:
            path = LOGS / f"propsvillage_{tag}_v{version}.png"
            if path.exists():
                raise SystemExit(f"[props-village] {path} exists: never overwrite a render, bump --preview")
            cam.location = pos
            cam.data.lens = lens
            cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            print("[props-village] preview", path)
        subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--sheet", str(version)], check=False)


if __name__ == "__main__":
    if IN_BLENDER:
        main()
    else:
        texture_main(sys.argv[1:])
