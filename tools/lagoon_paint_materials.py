"""Lagoon Court: the PAINTED and SMALL surfaces that were still flat colour, textured.

  py -3 tools/lagoon_paint_materials.py                         # paint every texture below
  py -3 tools/lagoon_paint_materials.py hull_paint,capiz        # paint the named ones
  py -3 tools/lagoon_paint_materials.py --sheet N               # swatch sheet vN for review
  blender -b ArtSource/lagoon/lagoon_cove.blend --python tools/lagoon_paint_materials.py -- --on-models N

Writes ArtSource/lagoon/textures/<name>_albedo.png, _height.png and _normal.png (OpenGL, green
up), every one a 2 m tile at 1024 px, read through the kits' world-scale "UVMap" (1 UV unit =
2 m). LAGOON_TEX_OUT redirects the output, as in author_lagoon_textures.py.

WHY (owner, of a boat render: "boat bodies are untextured": a plain white hull, a flat red
stripe). Five material slots in the boat and house kits still carried their placeholder colour:

  slot          texture       what it is
  paint_hull    hull_paint    a painted plank hull: strakes along V (V runs bow to stern), soft
                              seams, cream-white paint, a few worn patches where warm wood shows
  paint_trim    trim_paint    a NEUTRAL near-white painted band; the material MULTIPLIES it by the
                              mesh's per-boat "trim_tint" colour attribute (author_lagoon_boats.py
                              TRIM_COLOURS), so one texture serves the red, yellow and teal-green
                              stripes
  paint_white   lime_plaster  the capilla's lime-washed walls: warm off-white, soft feathered patches
  capiz         capiz         Filipino capiz-shell panes: ~7.7 cm pearly squares in a thin wooden
                              lattice
  cloth         cloth         laundry: a neutral cloth with soft vertical folds; the material
                              multiplies it by a per-PIECE pastel written as a "cloth_tint" colour
                              attribute (see apply_paint_materials)

THE HOUSE STYLE (docs/LAGOON_REWORK_GUIDE.md § 2, KANTO_DESIGN_GUIDE.md § 3): flat fills; value
changes from a few LARGE patches with FEATHERED organic edges; hand-drawn shapes, never ruled;
light as one flat cel band, never a gradient; low contrast; no grain, no noise, no streaks.
⚠️ Every surface is its OWN drawing (owner: "did you just repurpose the brick texture?"). Nothing
here calls a painter from author_lagoon_textures.py or author_kanto_textures.py; the only shared
code is plumbing (the normal-from-height conversion), and the approved rock_a and plank_c are
only READ, to sit beside the new swatches on the review sheet.
⚠️ ROLE HUES (Art_Direction.md § 1): nothing near offence orange #f87020 or defence blue #0080e8.
The cloth pastels are pink, cream, mint, pale yellow and white: no sky blue, and no apricot.

The Blender half (apply_paint_materials) is importable with no side effects; bpy, numpy and PIL
are imported only where they are needed, because Blender's Python has no PIL.
"""
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEX = Path(os.environ.get("LAGOON_TEX_OUT", ROOT / "ArtSource" / "lagoon" / "textures"))
APPROVED = ROOT / "ArtSource" / "lagoon" / "textures"
LOGS = ROOT / "Logs" / "lagoon-blender"
SIZE = 1024
TILE_M = 2.0                 # every kit's UVs: 1 unit = 2 m
PXM = SIZE / TILE_M          # pixels per metre

# Which texture each material slot gets.
SLOT_TEXTURE = {"paint_hull": "hull_paint", "paint_trim": "trim_paint", "paint_white": "lime_plaster",
                "capiz": "capiz", "cloth": "cloth"}
# Normal-map strength per texture (the owner: normal maps must read; 1.0 was near flat on plank).
STRENGTH = {"hull_paint": 2.5, "trim_paint": 1.2, "lime_plaster": 0.8, "capiz": 3.0, "cloth": 4.0}

# The laundry pastels, sRGB. Owner's reference photograph of the Sama-Bajau village: washing on
# lines in soft colours. Chosen away from both role hues: no blue at all, and the pink and the
# yellow kept pale enough that neither drifts toward offence orange.
# On-model v2: under sky light the first set went grey (the mint read sea-grey, the pink mauve),
# so each is a step more saturated, and the mint leans yellow-green, away from teal.
CLOTH_PASTELS = {
    "pink": "f4b3bb",
    "cream": "f5deae",
    "mint": "b8e2aa",
    "pale_yellow": "f8eaa6",    # v3 f6e388 went olive in shade
    "white": "f3efe4",
}
CAPIZ_GLOW = 0.25             # emission strength, see _capiz
CLOTH_LINE = "d8cfbb"        # the line itself: an undyed cord, never a pastel
# The boat stripe colours, copied from author_lagoon_boats.TRIM_COLOURS (linear) so the sheet can
# show trim_paint the way each boat will wear it without importing bpy.
TRIM_PREVIEW = {"red": (0.62, 0.035, 0.03), "yellow": (0.92, 0.62, 0.04), "teal_green": (0.02, 0.36, 0.20)}


# ================================================================ painting (numpy, PIL)

def _np():
    import numpy as np
    return np


def hexcol(h):
    np = _np()
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], dtype=np.float32)


def grid():
    """Metre coordinates of every pixel: x along U (across the image), y along V (down it)."""
    np = _np()
    y, x = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / PXM
    return x, y


def field(sx_m, seed, sy_m=None):
    """A periodic smooth random field over the 2 m tile, unit variance. Features about `sx_m`
    across U and `sy_m` (default the same) along V, so a field can be drawn long in one
    direction (a brush lap along a stripe, a fold hanging down a cloth)."""
    np = _np()
    sy_m = sx_m if sy_m is None else sy_m
    white = np.random.default_rng(seed).standard_normal((SIZE, SIZE))
    f = np.fft.fftfreq(SIZE)
    g = np.exp(-((f[:, None] * sy_m * PXM) ** 2 + (f[None, :] * sx_m * PXM) ** 2) * 2)
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * g))
    return ((n - n.mean()) / (n.std() + 1e-9)).astype(np.float32)


def smooth(a):
    return a * a * (3 - 2 * a)


def patch(scale_m, coverage, seed, feather=0.4, sy_m=None):
    """A feathered organic patch mask covering `coverage` of the tile: 0 outside, 1 inside."""
    np = _np()
    n = field(scale_m, seed, sy_m) + 0.25 * field(scale_m / 3, seed + 1, None if sy_m is None else sy_m / 3)
    t = np.quantile(n, 1 - coverage)
    return smooth(np.clip((n - t) / feather + 0.5, 0, 1))


def tint(img, colour_or_scale, mask):
    """Paint `mask` of the surface toward img * colour_or_scale (a second coat, not a new hue)."""
    return img * (1 - mask[..., None]) + img * colour_or_scale * mask[..., None]


def wrap(d):
    """Signed distance on the 2 m torus."""
    return (d + TILE_M / 2) % TILE_M - TILE_M / 2


def blob(x, y, cx, cy, rx, ry, seed, soft=0.25, wobble=0.18):
    """ONE hand-drawn soft shape: an ellipse whose outline wobbles (never a ruled circle), with a
    feathered edge `soft` of its radius wide. Wraps over the tile edges."""
    np = _np()
    dx, dy = wrap(x - cx) / rx, wrap(y - cy) / ry
    r = np.sqrt(dx * dx + dy * dy)
    ang = np.arctan2(dy, dx)
    rng = np.random.default_rng(seed)
    edge = 1.0
    for k, amp in ((2, wobble), (3, wobble * 0.7), (5, wobble * 0.35)):
        edge = edge + amp * np.sin(k * ang + rng.uniform(0, 2 * np.pi))
    return smooth(np.clip((edge - r) / soft, 0, 1))


# ---------------------------------------------------------------- hull_paint
# Research (hand-painted boat hulls in stylized kits and the owner's bangka photographs): a
# painted outrigger hull reads as a few long BOARDS under one coat of paint; the seams are soft
# and shallow because paint fills them; wear shows as a handful of rubbed patches along a board
# edge where the timber shows warm through the paint, never as scratches or streaks.
# ⚠️ V runs ALONG the hull (author_lagoon_boats.py: "V along the hull's length (y), U around the
# girth"), so the strakes are COLUMNS of this image (constant U) and their seams run down it.
HULL_STRAKES = 10            # 20 cm boards around the girth: a bangka's side shows three or four


def hull_paint():
    np = _np()
    x, y = grid()
    rng = np.random.default_rng(211)
    paint = hexcol("ebe3cf")                    # cream white, the placeholder's colour made warm
    wood = hexcol("c29a6c")                     # the approved plank light tone (plank_c)
    w = TILE_M / HULL_STRAKES
    # Hand-drawn seams: each wobbles a few millimetres along the hull, never a ruled line.
    u = x + 0.004 * field(0.6, 212, 1.2)
    k = np.floor(u / w).astype(int) % HULL_STRAKES
    a = (u / w) - np.floor(u / w)               # 0..1 across the strake
    values = rng.uniform(0.975, 1.02, HULL_STRAKES)
    img = paint[None, None, :] * values[k][..., None]
    # The seam: paint fills it, so a narrow SOFT dip in value, not a dark line (low contrast
    # between a thing and its joints). Plus one flat cel band of light along the board's upper
    # edge, where the lap catches the sun.
    edge_m = np.minimum(a, 1 - a) * w
    seam = smooth(np.clip(1 - edge_m / 0.009, 0, 1))
    img = img * (1 - 0.13 * seam)[..., None]
    lit = smooth(np.clip((a - 0.70) / 0.03, 0, 1)) * smooth(np.clip((0.93 - a) / 0.03, 0, 1))
    img = img + (np.minimum(img * 1.05, 1) - img) * lit[..., None]
    # Two coats of the paint itself: big feathered patches a shade apart (the house style).
    img = tint(img, np.array([0.965, 0.955, 0.94]), patch(0.45, 0.30, 213, sy_m=0.9))
    img = tint(img, np.array([1.02, 1.02, 1.015]), patch(0.35, 0.18, 214, sy_m=0.7))
    # WORN PATCHES along a seam: a few long soft shapes hugging one board edge, where the paint
    # has rubbed through to the wood. Blended, not stamped, so the contrast stays low.
    wear = np.zeros((SIZE, SIZE), np.float32)
    # Sheet v0 review (scratch): seven slivers with a speckled inner edge read as flaking
    # scratches, which is the streak-and-grain look the house style bans. Now four broader soft
    # shapes, one clean feathered outline each.
    for i in range(4):
        s = rng.integers(HULL_STRAKES)
        side = rng.choice([-1, 1])
        cx = s * w + (0.02 if side > 0 else w - 0.02)
        cy = rng.uniform(0, TILE_M)
        wear = np.maximum(wear, blob(x, y, cx, cy, rng.uniform(0.035, 0.055), rng.uniform(0.14, 0.28),
                                     seed=220 + i, soft=0.5, wobble=0.2))
    img = img * (1 - 0.5 * wear[..., None]) + wood * (0.5 * wear[..., None])
    height = 1 - 0.8 * seam - 0.12 * np.abs(2 * a - 1) ** 2 - 0.25 * wear
    return img, height


# ---------------------------------------------------------------- trim_paint
# The sheer stripe is ONE brushed band of enamel. Neutral near-white so that multiplying by the
# per-boat "trim_tint" gives each boat its own red, yellow or teal-green (author_lagoon_boats.py:
# "A textured paint_trim should multiply its texture by the vertex colour, so one texture serves
# every boat"). What it carries: long soft brush LAPS along the band (V, along the hull), a shade
# apart, and two tiny chips where the enamel has knocked off to the darker wood underneath.


def trim_paint():
    np = _np()
    x, y = grid()
    img = np.broadcast_to(hexcol("f6f3ec"), (SIZE, SIZE, 3)).astype(np.float32).copy()
    # Scratch v0 drew the laps 0.10 x 0.9 m and at 0.93: they read as streaks. Broad soft laps
    # now, only a little longer than wide, and a shade apart.
    laps = patch(0.22, 0.35, 311, feather=0.5, sy_m=0.45)          # a little long along the band
    img = tint(img, np.array([0.955, 0.955, 0.955]), laps)
    img = tint(img, np.array([1.02, 1.02, 1.02]), patch(0.15, 0.12, 313, feather=0.5, sy_m=0.3))
    # Two tiny chips per tile (1 to 2 cm), a warm dark wood that stays wood-brown under any tint.
    chips = np.zeros((SIZE, SIZE), np.float32)
    for i, (cx, cy) in enumerate(((0.37, 0.52), (1.41, 1.63))):
        chips = np.maximum(chips, blob(x, y, cx, cy, 0.011, 0.016, seed=320 + i, soft=0.3, wobble=0.3))
    img = img * (1 - chips[..., None]) + hexcol("9c7a55") * chips[..., None]
    height = 0.5 + 0.12 * laps - 0.5 * chips
    return img, height


# ---------------------------------------------------------------- lime_plaster
# Research (lime-washed chapels in the Visayas and Mindanao, stylized plaster in hand-painted
# kits): lime wash is a soft CHALKY off-white laid in several thin coats, so it reads as cloudy,
# broad, slightly warm-grey and warm-cream areas with soft edges; the trowelled render under it
# gives very gentle undulation, never cracks drawn as lines. A damp band low on the wall would
# need world height, which a tiling texture cannot know, so it is not painted here.


def lime_plaster():
    np = _np()
    img = np.broadcast_to(hexcol("ede5d4"), (SIZE, SIZE, 3)).astype(np.float32).copy()
    # On-model v3: the older coat at 0.955 read as damp stains on the capilla's shaded wall.
    img = tint(img, np.array([0.972, 0.968, 0.96]), patch(0.55, 0.30, 411, feather=0.6))   # older coat
    img = tint(img, np.array([1.015, 1.01, 0.99]), patch(0.40, 0.22, 413, feather=0.5))  # fresh coat
    # (Scratch v0 had a third coat of small dabs: with the other two it read as camouflage.)
    # The trowel: broad and faint. On-model v2 (0.35 m and 0.12 m fields at full bump) read as
    # lumpy stucco, a texture of its own that no lime-washed capilla has.
    height = field(0.6, 417)
    return img, height


# ---------------------------------------------------------------- capiz
# Research (Filipino capiz windows, ventanillas and sliding sashes in bahay na bato and bahay
# kubo): flattened windowpane-oyster shells cut into squares of about 3 inches (7.6 cm), set in a
# lattice of thin wooden strips about 1 cm wide. By day each shell reads PEARLY: milky off-white,
# some a touch warmer, some a touch pinker or greener, with a soft sheen that catches one corner.
# Stylized: flat per-square fills in a narrow pearly range, ONE flat cel band of sheen across
# each square, a slightly darker rim just inside the lattice, a warm mid-wood lattice.
CAPIZ_N = 26                  # squares per 2 m tile: 7.7 cm each
CAPIZ_BAR = 0.15              # the lattice strip, as a share of a square: ~1.2 cm (v2 at 8 mm read as a wire screen)


def capiz():
    np = _np()
    x, y = grid()
    rng = np.random.default_rng(511)
    s = TILE_M / CAPIZ_N
    u = x + 0.0015 * field(0.3, 512)            # hand wobble, a millimetre or two
    v = y + 0.0015 * field(0.3, 513)
    i = np.floor(u / s).astype(int) % CAPIZ_N
    j = np.floor(v / s).astype(int) % CAPIZ_N
    a, b = u / s - np.floor(u / s), v / s - np.floor(v / s)
    # Scratch v0 shifted the shells pink, green and yellow a full step apart and read as a
    # bathroom mosaic; the pearly shift is now a whisper and the sheen is rarer and fainter.
    shells = np.array([hexcol("efe9da"), hexcol("f0e7de"), hexcol("ebebdd"), hexcol("f1ebd8")])
    pick = rng.integers(0, len(shells), (CAPIZ_N, CAPIZ_N))
    value = rng.uniform(0.975, 1.015, (CAPIZ_N, CAPIZ_N))
    img = shells[pick[j, i]] * value[j, i][..., None]
    # The sheen: one flat light band across the square's diagonal, soft-edged, its position and
    # width varying per shell; about a third of the shells have none (they face away).
    off = rng.uniform(0.25, 0.75, (CAPIZ_N, CAPIZ_N))[j, i]
    wid = rng.uniform(0.10, 0.22, (CAPIZ_N, CAPIZ_N))[j, i]
    has = (rng.random((CAPIZ_N, CAPIZ_N)) < 0.4)[j, i]
    d = np.abs((a + (1 - b)) / 2 - off)
    sheen = smooth(np.clip((wid - d) / 0.06, 0, 1)) * has
    img = img + (np.minimum(img * 1.035, 1) - img) * sheen[..., None]
    # A darker rim just inside the lattice (the shell sits in a groove), then the strip itself.
    e = np.minimum(np.minimum(a, 1 - a), np.minimum(b, 1 - b))
    rim = smooth(np.clip(1 - (e - CAPIZ_BAR / 2) / 0.07, 0, 1))
    img = img * (1 - 0.07 * rim)[..., None]
    bar = smooth(np.clip((CAPIZ_BAR / 2 - e) / 0.02 + 0.5, 0, 1))
    # A LIGHT warm wood, close in value to the shells (low contrast between a thing and its
    # joints): on-model v3 with a dark lattice still read as a wire window screen from 8 m.
    wood = hexcol("bc9f7a") * rng.uniform(0.96, 1.04, (CAPIZ_N, CAPIZ_N))[j, i][..., None]
    img = img * (1 - bar[..., None]) + wood * bar[..., None]
    # Height: the lattice stands proud, each shell slightly dished below it.
    height = bar + (1 - bar) * (0.35 + 0.1 * smooth(np.clip(e / 0.3, 0, 1)))
    return img, height


# ---------------------------------------------------------------- cloth
# Research (stylized hanging cloth, the owner's Bajau photograph): washing on a line reads as a
# light flat colour with a few soft VERTICAL folds hanging from the pegs, never a weave. The cloth
# pieces' UVs run V down the hang (author_lagoon_houses.laundry: each piece a beam from the line
# down), so the folds are columns of this image. Neutral near-white: the material multiplies it
# by each piece's pastel.


def cloth():
    np = _np()
    x, y = grid()
    rng = np.random.default_rng(611)
    # Fold columns of 11 to 24 cm, scaled to fill the 2 m tile exactly so it wraps.
    widths = []
    while sum(widths) < TILE_M:
        widths.append(rng.uniform(0.11, 0.24))   # scratch v0 at 6-14 cm: pinstripes
    widths = np.array(widths) * TILE_M / sum(widths)
    starts = np.concatenate([[0.0], np.cumsum(widths)[:-1]])
    u = (x + 0.012 * field(0.25, 612, 0.8)) % TILE_M   # folds drift a little as they hang
    k = np.searchsorted(starts, u, side="right") - 1
    t = (u - starts[k]) / widths[k]                    # 0..1 across the fold
    # A fold's profile, CEL style: a flat lit face, a soft turn into a flat shaded face.
    lit_share = rng.uniform(0.45, 0.7, len(widths))[k]
    shade = smooth(np.clip((t - lit_share) / 0.12, 0, 1)) * smooth(np.clip((1.0 - t) / 0.12, 0, 1))
    deep = rng.uniform(0.05, 0.085, len(widths))[k]
    img = np.broadcast_to(hexcol("f4f1ea"), (SIZE, SIZE, 3)).astype(np.float32).copy()
    img = img * (1 - deep * shade)[..., None]
    img = tint(img, np.array([0.975, 0.975, 0.97]), patch(0.4, 0.2, 613, feather=0.6, sy_m=0.6))
    height = np.sin(np.pi * np.clip(t / np.maximum(lit_share, 1e-3), 0, 1)) * (1 - shade)
    return img, height


PAINTERS = {"hull_paint": hull_paint, "trim_paint": trim_paint, "lime_plaster": lime_plaster,
            "capiz": capiz, "cloth": cloth}


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
    albedo = np.clip(albedo, 0, 1)
    r = np.ptp(height)
    h = (height - height.min()) / r if r > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((albedo * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_albedo.png")
    Image.fromarray((h * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_height.png")
    Image.fromarray((normal_from_height(h, STRENGTH[name]) * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_normal.png")
    print("[lagoon-paint]", name)


def _srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _lin_to_srgb(c):
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def _multiplied(img, rgb_srgb):
    """What a material multiplying the texture by a colour shows, done in linear light the way
    the shader does it, back to sRGB for the sheet."""
    np = _np()
    a = np.asarray(img, np.float32) / 255
    lin = np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)
    tint_lin = np.array([_srgb_to_lin(c) for c in rgb_srgb], np.float32)
    out = lin * tint_lin
    out = np.where(out <= 0.0031308, out * 12.92, 1.055 * np.power(np.maximum(out, 0), 1 / 2.4) - 0.055)
    from PIL import Image
    return Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8))


def sheet(version):
    """The review sheet: the approved rock_a and plank_c first, then every new texture at one
    tile and as a 3 x 3 repeat (so any seam in the tiling shows), then trim_paint as each boat
    wears it and cloth in each pastel, multiplied the way the materials do it."""
    from PIL import Image, ImageDraw, ImageFont
    out = LOGS / f"paint_swatches_v{version}.png"
    if out.exists():
        raise SystemExit(f"{out} exists: renders are never overwritten, pick a new version")
    cols = [("rock_a (approved, 4 m)", Image.open(APPROVED / "rock_a_albedo.png").convert("RGB")),
            ("plank_c (approved, 2 m)", Image.open(APPROVED / "plank_c_albedo.png").convert("RGB"))]
    cols += [(f"{n} (2 m)", Image.open(TEX / f"{n}_albedo.png").convert("RGB")) for n in PAINTERS]
    trim = Image.open(TEX / "trim_paint_albedo.png").convert("RGB")
    cl = Image.open(TEX / "cloth_albedo.png").convert("RGB")
    tinted = [(f"trim x {k}", _multiplied(trim, [_lin_to_srgb(c) for c in v])) for k, v in TRIM_PREVIEW.items()]
    tinted += [(f"cloth x {k}", _multiplied(cl, [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]))
               for k, h in CLOTH_PASTELS.items()]
    cell, gap, head = 330, 16, 40
    per_row = len(cols)
    W = gap + per_row * (cell + gap)
    H = 2 * (head + cell) + gap * 2 + 2 * (head + cell // 2 + gap)
    page = Image.new("RGB", (W, H), (245, 241, 234))
    draw = ImageDraw.Draw(page)
    try:
        font = ImageFont.truetype("arial.ttf", 17)
    except OSError:
        font = ImageFont.load_default()
    for i, (label, tile) in enumerate(cols):
        x = gap + i * (cell + gap)
        draw.text((x, 12), label, fill=(40, 36, 32), font=font)
        page.paste(tile.resize((cell, cell), Image.LANCZOS), (x, head))
        draw.text((x, head + cell + gap + 12), "3 x 3 repeat", fill=(40, 36, 32), font=font)
        rep = Image.new("RGB", (tile.width * 3, tile.height * 3))
        for a in range(3):
            for b in range(3):
                rep.paste(tile, (a * tile.width, b * tile.height))
        page.paste(rep.resize((cell, cell), Image.LANCZOS), (x, 2 * head + cell + gap))
    y0 = 2 * (head + cell) + gap * 2
    small = (cell - gap) // 2 + cell // 2
    for i, (label, img) in enumerate(tinted):
        r, c = divmod(i, per_row)
        x = gap + c * (cell + gap)
        y = y0 + r * (head + cell // 2 + gap)
        draw.text((x, y + 10), label, fill=(40, 36, 32), font=font)
        # Half a tile, at twice the scale of the row above, so the chips and folds read.
        crop = img.crop((0, 0, SIZE // 2, SIZE // 4))
        page.paste(crop.resize((cell, cell // 2), Image.LANCZOS), (x, y + head))
    LOGS.mkdir(parents=True, exist_ok=True)
    page.save(out)
    print("[lagoon-paint] sheet", out)


# ================================================================ Blender materials

def _image(nt, name, uv, colour=True):
    import bpy
    path = TEX / name
    if not path.exists():
        return None
    node = nt.nodes.new("ShaderNodeTexImage")
    node.image = bpy.data.images.load(str(path), check_existing=True)
    if not colour:
        node.image.colorspace_settings.name = "Non-Color"
    nt.links.new(uv, node.inputs["Vector"])
    return node


def _uv_textured(m, texture, roughness, bump=0.02, normal=1.0):
    """Rebuild `m` in place around one texture read through the kit's world-scale "UVMap": albedo
    to Base Color, the normal map, and the painted HEIGHT map as bump displacement (the owner:
    "make sure it has depth/normal maps"). This mirrors render_lagoon_texture_preview.uv_material
    rather than importing it, because that file is the lead's and is being edited. Returns the
    node tree, the BSDF and the albedo colour socket so a caller can tint it."""
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = roughness
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    uvn = nt.nodes.new("ShaderNodeUVMap")
    uvn.uv_map = "UVMap"
    uv = uvn.outputs["UV"]
    alb = _image(nt, f"{texture}_albedo.png", uv)
    colour = alb.outputs["Color"] if alb else None
    nimg = _image(nt, f"{texture}_normal.png", uv, colour=False)
    if nimg:
        nmap = nt.nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = normal
        nt.links.new(nimg.outputs["Color"], nmap.inputs["Color"])
        nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    himg = _image(nt, f"{texture}_height.png", uv, colour=False)
    if himg:
        disp = nt.nodes.new("ShaderNodeDisplacement")
        disp.inputs["Midlevel"].default_value = 0.5
        disp.inputs["Scale"].default_value = bump
        nt.links.new(himg.outputs["Color"], disp.inputs["Height"])
        nt.links.new(disp.outputs["Displacement"], out.inputs["Displacement"])
        if hasattr(m, "displacement_method"):
            m.displacement_method = "BUMP"
    return nt, bsdf, colour


def _times_attribute(nt, colour, attribute):
    """colour x the mesh's corner colour attribute `attribute` (FBX: the vertex colour)."""
    at = nt.nodes.new("ShaderNodeAttribute")
    at.attribute_type = "GEOMETRY"
    at.attribute_name = attribute
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    nt.links.new(colour, mix.inputs["A"])
    nt.links.new(at.outputs["Color"], mix.inputs["B"])
    return mix.outputs["Result"]


def _plain(m, texture, roughness, **kw):
    nt, bsdf, colour = _uv_textured(m, texture, roughness, **kw)
    if colour is not None:
        nt.links.new(colour, bsdf.inputs["Base Color"])


def _hull(m):
    _plain(m, "hull_paint", 0.6, bump=0.015, normal=0.8)


def _trim(m):
    # ⚠️ THE STRIPE COLOUR IS THE BOAT'S, NOT THE MATERIAL'S. Every boat's paint_trim mesh carries
    # "trim_tint" (author_lagoon_boats.build_boat); the texture is near-white and is MULTIPLIED by
    # it, so red, yellow and teal-green boats share one material and one texture. Unity: the same
    # attribute arrives as the FBX vertex colour; multiply albedo by it.
    nt, bsdf, colour = _uv_textured(m, "trim_paint", 0.5, bump=0.01, normal=0.6)
    if colour is not None:
        nt.links.new(_times_attribute(nt, colour, "trim_tint"), bsdf.inputs["Base Color"])


def _plaster(m):
    _plain(m, "lime_plaster", 0.92, bump=0.006, normal=0.4)


def _capiz(m):
    # A little glossier than the walls: a shell has a sheen. No emission: by day, seen from
    # outside, a capiz pane is a pearly surface, not a lamp.
    # On-model v2: in the shade of a window recess the panes went grey and the lattice read as
    # a wire screen. A capiz pane is TRANSLUCENT: it passes daylight through, so it never goes
    # as dark as the wall around it. A faint emission of its own colour stands in for that
    # (Unity: the same albedo at low strength in the emission slot).
    nt, bsdf, colour = _uv_textured(m, "capiz", 0.38, bump=0.01, normal=1.0)
    if colour is not None:
        nt.links.new(colour, bsdf.inputs["Base Color"])
        nt.links.new(colour, bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = CAPIZ_GLOW


def _cloth(m):
    # Per-PIECE pastels, not per object. Owner's reference: a line of washing in several colours.
    # One colour per object would dress every piece on one line alike, so apply_paint_materials
    # writes a "cloth_tint" corner colour per connected piece (the same route as the boats'
    # trim_tint, and it reaches Unity as the FBX vertex colour).
    nt, bsdf, colour = _uv_textured(m, "cloth", 0.95, bump=0.02, normal=1.0)
    if colour is not None:
        nt.links.new(_times_attribute(nt, colour, "cloth_tint"), bsdf.inputs["Base Color"])


BUILDERS = {"paint_hull": _hull, "paint_trim": _trim, "paint_white": _plaster, "capiz": _capiz, "cloth": _cloth}


def _islands(me):
    """Connected pieces of a mesh: a piece index per polygon (union-find over shared vertices)."""
    parent = list(range(len(me.vertices)))

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a

    for p in me.polygons:
        vs = p.vertices
        r0 = find(vs[0])
        for v in vs[1:]:
            r = find(v)
            if r != r0:
                parent[r] = r0
    roots = {}
    return [roots.setdefault(find(p.vertices[0]), len(roots)) for p in me.polygons]


def tint_cloth(me, seed=0):
    """Write "cloth_tint" onto a laundry mesh: each hanging piece one pastel (neighbours never
    repeat), the long thin piece that is the LINE an undyed cord."""
    import random
    piece = _islands(me)
    n = max(piece) + 1 if piece else 0
    lo = [[1e9] * 3 for _ in range(n)]
    hi = [[-1e9] * 3 for _ in range(n)]
    for p, k in zip(me.polygons, piece):
        for v in p.vertices:
            co = me.vertices[v].co
            for c in range(3):
                lo[k][c] = min(lo[k][c], co[c])
                hi[k][c] = max(hi[k][c], co[c])
    names = list(CLOTH_PASTELS)
    rng = random.Random(f"{me.name}:{seed}")
    colour, last = [], None
    for k in range(n):
        ext = sorted(hi[k][c] - lo[k][c] for c in range(3))
        if ext[2] > 1.0 and ext[1] < 0.06:          # over a metre long and a few cm thick: the line
            hx = CLOTH_LINE
        else:
            name = rng.choice([c for c in names if c != last])
            last = name
            hx = CLOTH_PASTELS[name]
        colour.append(tuple(int(hx[i:i + 2], 16) / 255 for i in (0, 2, 4)) + (1.0,))
    attr = me.color_attributes.get("cloth_tint")
    if attr is not None:
        me.color_attributes.remove(attr)
    attr = me.color_attributes.new("cloth_tint", "BYTE_COLOR", "CORNER")
    flat = []
    for p, k in zip(me.polygons, piece):
        flat.extend(colour[k] * p.loop_total)
    attr.data.foreach_set("color_srgb", flat)       # a byte colour stores sRGB


def apply_paint_materials(verbose=True):
    """Rebuild the materials paint_hull, paint_trim, paint_white, capiz and cloth IN PLACE (every
    object keeps its slot) around their painted textures, and write the per-piece "cloth_tint"
    onto every mesh that uses cloth. A material that is not in the file is skipped, so this is
    safe on a file holding only boats or only houses. Returns the names it rebuilt."""
    import bpy
    done = []
    for slot, build in BUILDERS.items():
        m = bpy.data.materials.get(slot)
        if m is None:
            continue
        build(m)
        done.append(slot)
    if "cloth" in done:
        cloth_m = bpy.data.materials["cloth"]
        for me in bpy.data.meshes:
            if any(mm is cloth_m for mm in me.materials):
                tint_cloth(me)
    if verbose:
        print("[lagoon-paint] rebuilt", done)
    return done


# ================================================================ on-model review (Blender)

def _root_frame(ob):
    """World position of an object and its heading (the local +Y axis, the boat's bow, the
    house's door) and its local +X."""
    from mathutils import Vector
    mw = ob.matrix_world
    return mw.translation.copy(), (mw.to_3x3() @ Vector((0, 1, 0))).normalized(), \
        (mw.to_3x3() @ Vector((1, 0, 0))).normalized()


def _bbox_centre(obs):
    from mathutils import Vector
    pts = [o.matrix_world @ Vector(c) for o in obs for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return (lo + hi) / 2, hi - lo


def _placed(name_prefix):
    import bpy
    return [o for o in bpy.data.objects if o.type == "MESH" and o.name.startswith(name_prefix)
            and any(c.name == "Village" for c in o.users_collection)]


def _clear_view(target, own, offsets):
    """The first target + offset from which nothing but `own` stands between camera and target."""
    import bpy
    scene = bpy.context.scene
    dg = bpy.context.evaluated_depsgraph_get()
    own = {o.name for o in own}
    for off in offsets:
        pos = target + off
        d = (pos - target)
        hit, loc, _n, _i, ob, _m = scene.ray_cast(dg, target + d.normalized() * 0.3, d.normalized(),
                                                  distance=d.length - 0.3)
        if not hit or ob.name in own:
            return pos
    return target + offsets[0]


def shots():
    """The four close-ups, placed from the file: each camera sits off a placed object along its
    own axes, so a rebuilt village with the same seeds keeps its framing."""
    from mathutils import Vector
    out = []
    # A bangka: the first placed yellow bangka (seed 9919), from its side, low over the water.
    hull = [o for o in _placed("bangka_9919_paint_hull")][0]
    c, fwd, side = _root_frame(hull)
    ctr, _ext = _bbox_centre([hull])
    out.append(("bangka", ctr + side * 3.6 + fwd * 1.6 + Vector((0, 0, 1.1)), ctr + fwd * 0.4, 30))
    # A lepa: the teal-green lepa (seed 2000), three-quarter from the bow.
    hull = _placed("lepa_2000_paint_hull")[0]
    c, fwd, side = _root_frame(hull)
    ctr, _ext = _bbox_centre([hull])
    out.append(("lepa", ctr + side * 5.5 + fwd * 4.0 + Vector((0, 0, 1.6)), ctr + fwd * 0.8, 30))
    # The capilla: a side wall and its capiz windows. v1 placed the camera by the capilla's axes
    # alone and it landed inside a summit boulder, so the camera now swings round the wall and
    # takes the first position with a clear line of sight to it.
    shell = _placed("capilla_1000_paint_white")[0]
    c, fwd, side = _root_frame(shell)
    wall = c + side * 2.6 + Vector((0, 0, 2.2))
    pos = _clear_view(wall, [shell], [side * 8.0 + fwd * f + Vector((0, 0, z))
                                      for f in (-3.0, 3.0, -5.5, 5.5, 0.0) for z in (1.0, 3.0)]
                      + [-side * 8.0 + fwd * f + Vector((0, 0, 2.0)) for f in (-3.0, 3.0)])
    out.append(("capilla", pos, wall, 30))
    # A water home: the first placed seed 1000 (capiz sash and a laundry line on its front deck).
    cl = _placed("water_1000_cloth")[0]
    c, fwd, side = _root_frame(cl)
    ctr, _ext = _bbox_centre([cl])
    # v1 framed the whole front and the capiz panes were too small to judge: closer, on the
    # half of the front that holds both the capiz sash and the washing.
    cz = _placed("water_1000_capiz")[0]
    wctr, _e = _bbox_centre([cz])
    tgt = (ctr + wctr) / 2
    out.append(("home", tgt + fwd * 3.6 + side * 0.6 + Vector((0, 0, 0.2)), tgt, 30))
    return out


def on_models(version, only=None):
    import bpy
    from mathutils import Vector
    paths = {n: LOGS / f"paint_on_models_{n}_v{version}.png" for n in ("bangka", "lepa", "capilla", "home")}
    for p in paths.values():
        if p.exists():
            raise SystemExit(f"{p} exists: renders are never overwritten, pick a new version")
    apply_paint_materials()
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Punchy"
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    if hasattr(scene.eevee, "shadow_pool_size"):
        scene.eevee.shadow_pool_size = "1024"     # the largest; the default overflowed ("Shadow buffer full")
    cam = bpy.data.objects.new("paint cam", bpy.data.cameras.new("paint cam"))
    cam.data.sensor_fit = "HORIZONTAL"
    cam.data.clip_start = 0.05
    cam.data.clip_end = 2000
    scene.collection.objects.link(cam)
    scene.camera = cam
    for name, pos, tgt, lens in shots():
        if only and name not in only:
            continue
        cam.location = pos
        cam.data.lens = lens
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(paths[name])
        bpy.ops.render.render(write_still=True)
        print("[lagoon-paint] render", paths[name])
    # ⚠️ READ-ONLY: the cove file is the lead's build output. Nothing here saves it.


def main():
    try:
        import bpy  # noqa: F401
        in_blender = True
    except ImportError:
        in_blender = False
    if in_blender:
        argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
        if "--on-models" in argv:
            only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
            on_models(int(argv[argv.index("--on-models") + 1]), only)
        else:
            apply_paint_materials()
        return
    args = sys.argv[1:]
    if "--sheet" in args:
        sheet(int(args[args.index("--sheet") + 1]))
        return
    names = args[0].split(",") if args else list(PAINTERS)
    for n in names:
        save(n, *PAINTERS[n]())


if __name__ == "__main__":
    main()
