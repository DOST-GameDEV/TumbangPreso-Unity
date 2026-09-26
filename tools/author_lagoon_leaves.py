"""Paint Lagoon Court's leaf cards, in Kanto's approved leaf style, for tropical plants.

  py -3 tools/author_lagoon_leaves.py            # paint every leaf
  py -3 tools/author_lagoon_leaves.py --sheet N  # the review sheet, tinted, vN

OWNER, 2026-09-27: "start texturing the plants", "i really like what was used for leaves in
kanto. just need to ensure it matches this environment". Kanto's leaf (`leaf()` in
tools/author_kanto_textures.py, docs/KANTO_DESIGN_GUIDE.md section 5) is the method, kept exactly:
  * ONE soft-painted greyscale drawing with a crisp ALPHA silhouette, tinted by the material
    (two tints a plant, light on top and dark underneath; variation per plant, never per leaf:
    per-leaf colour was tried in Kanto and reverted by the owner);
  * value rises smoothly from stem (dark) to tip (light), one soft sunlit patch, only a whisper
    of a midrib, NO outline ("sharply outlined and not softly painted" was rejected).
What changes for the tropics is the SHAPE, since a cove of coconut palms and banana plants is
not a street of round-leaved shade trees:
  leaf_round   Kanto's own rounded leaf, redrawn here at the same proportions: bushes, gumamela
  frond        a whole coconut frond: a gently bowed midrib with narrow leaflets combing off
               both sides toward the tip, a few gaps where leaflets have split (drawn along V,
               base at the bottom); the kit bends the card along V so the frond droops
  paddle       a banana/taro leaf: a broad oval, a pale soft midrib, soft parallel vein bands,
               and two tears from the edge (the banana look)
  blade        a grass/pandan blade: long, narrow, tapered, a soft centre fold
Greyscale RGBA PNGs to ArtSource/lagoon/textures/<name>_albedo.png.

The rest of each plant is painted here too, in the same hand (see "the rest of the plant" below):
  palm_trunk     coconut trunk, grey-brown with soft leaf-scar rings; tiles, 2 m a tile; colour,
                 with _height and _normal
  coconut        the nut's husk, wrapped round a UV sphere; colour
  banana_stem    the banana pseudo-stem, lengthwise sheath bands, dry brown sheaths at the base
  stalk          a leaf stalk, greyscale, tinted per plant (STALK_TINTS)
  gumamela       a hibiscus flower card, greyscale + alpha, tinted (FLOWER_TINTS), pale stamen
  bougainvillea  a three-bract cluster card, greyscale + alpha, tinted, pale centre
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "ArtSource" / "lagoon" / "textures"
SHEETS = ROOT / "Logs" / "lagoon-blender"


def _grid(w, h):
    v, u = np.mgrid[0:h, 0:w].astype(np.float32)
    return u / (w - 1), 1 - v / (h - 1)          # u across 0..1, v base 0 .. tip 1


def _save(name, g, alpha):
    OUT.mkdir(parents=True, exist_ok=True)
    g = np.clip(g * 0.92, 0, 1)
    rgba = np.dstack([g, g, g, np.clip(alpha, 0, 1)])
    Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").save(OUT / f"{name}_albedo.png")
    print("[lagoon-leaf]", name)


def _soft_tone(x, v, d, hl_at=(-0.25, 0.62)):
    """Kanto's leaf shading: stem dark to tip light, a soft sunlit patch, a faint midrib."""
    tone = 0.74 + 0.26 * np.clip(v, 0, 1) ** 0.8
    tone = tone + 0.12 * np.exp(-(((x - hl_at[0]) / 0.35) ** 2 + ((v - hl_at[1]) / 0.22) ** 2))
    tone = tone + 0.04 * np.exp(-(x / 0.05) ** 2) * (v > 0.12)
    tone = tone - 0.06 * np.clip(1 - d / 0.25, 0, 1) * (v < 0.5)
    return tone


def leaf_round(size=512):
    u, v = _grid(size, size)
    x = (u - 0.5) * 2
    half = 0.95 * np.clip(np.sin(np.pi * np.clip(v, 0, 1) ** 0.85), 0, 1) ** 0.6 * (1 - 0.25 * v)
    d = half - np.abs(x)
    _save("leaf_round", _soft_tone(x, v, d), d * size / 2.5)


def frond(w=512, h=1024, seed=3):
    """A coconut frond seen flat: a midrib from the base (bottom) to the tip, bowing a little,
    leaflets combing forward off both sides, longest at mid-frond."""
    rng = np.random.default_rng(seed)
    u, v = _grid(w, h)
    x = (u - 0.5) * 2                                       # -1..1 across
    vy = v * 2.0                                            # v in the same units as x (h = 2w)
    mid = 0.06 * np.sin(np.pi * v)                          # the midrib bows gently
    xr = x - mid
    alpha = np.zeros_like(x)
    tone = np.zeros_like(x)
    # Sheet v1 read as a fern: leaflets short and dense. A coconut frond's leaflets are LONG and
    # thin and comb out at ~40 degrees, reaching most of the way to the card edge.
    # OWNER, 2026-09-27: "leaves of palmtrees are also cut off". Sheet v3's upper leaflets ran
    # past the card's top edge and were clipped straight across (104 opaque pixels on the top
    # row), so every frond ended in a blunt comb instead of a point. Now each leaflet is shortened
    # to whatever fits inside the card (top and sides, with a margin), the leaflets shrink to
    # nothing over the last 15 per cent toward the tip, and the midrib narrows to a clean point.
    n = 24
    for side in (-1, 1):
        for k in range(n):
            t = 0.05 + 0.92 * k / n + rng.uniform(-0.006, 0.006)
            if rng.random() < 0.08 and t < 0.8:
                continue                                    # a split, where a leaflet tore off
            # The outline: leaflets grow quickly off the stalk, are longest just below mid-frond,
            # then shorten steadily into the tip and reach nothing over the last 15 per cent, so the
            # frond's silhouette closes to a point (sheet v4 still clamped a flat-topped fan).
            length = 0.98 * min(1.0, t / 0.22) ** 0.5 * np.clip((0.99 - t) / 0.6, 0, 1) ** 0.85
            length *= np.clip((0.99 - t) / 0.15, 0, 1) ** 0.6   # the last 15 per cent: to nothing
            ang = np.radians(rng.uniform(36, 46))           # leaflets comb toward the tip
            dx, dy = side * np.cos(ang), np.sin(ang)
            ox = 0.06 * np.sin(np.pi * t)                   # the midrib's bow at this leaflet
            fit_top = (1.94 - t * 2.0) / dy                 # room to the card's top edge
            fit_side = (0.94 - side * ox) / abs(dx)         # room to its side edge
            length = min(length, fit_top, fit_side)
            if length < 0.02:
                continue
            px, py = xr, vy - t * 2.0
            along = px * dx + py * dy
            across = np.abs(px * dy - py * dx)
            s = np.clip(along / (length + 1e-6), 0, 1)
            width = 0.038 * np.sin(np.pi * s) ** 0.5 * min(1.0, length / 0.25 + 0.3) + 0.004 * (1 - s)
            a = np.clip((width - across) * w / 3.0, 0, 1) * ((along > 0) & (along < length))
            lt = 0.76 + 0.2 * s + 0.1 * np.exp(-((s - 0.55) / 0.2) ** 2)
            tone = np.where(a > alpha, lt, tone)
            alpha = np.maximum(alpha, a)
    # The midrib tapers from the stalk to a point just inside the top edge (v2's rib stopped
    # square at 0.985 at a third of its base width, which read as a snapped stick).
    rib_half = 0.022 * np.clip((0.975 - v) / 0.975, 0, 1) ** 0.7 + 0.002 * (v < 0.975)
    rib = np.clip((rib_half - np.abs(xr)) * w / 3.0, 0, 1) * (v < 0.975)
    tone = np.where(rib > alpha * 0.5, 0.82 + 0.08 * v, tone)
    alpha = np.maximum(alpha, rib)
    _save("frond", tone, alpha)


def paddle(w=512, h=1024, seed=5):
    """A banana leaf: LONG oval (sheet v1's round paddle read as a lily pad), a soft pale midrib,
    soft vein bands, and two TEARS from the edge toward the midrib, wide at the edge and closing
    (v1's hairline tears read as scratches)."""
    rng = np.random.default_rng(seed)
    u, v = _grid(w, h)
    x = (u - 0.5) * 2
    half = 0.94 * np.clip(np.sin(np.pi * np.clip(v, 0, 1)), 0, 1) ** 0.4
    d = half - np.abs(x)
    alpha = d * w / 2.5
    for side, t0 in ((1, rng.uniform(0.3, 0.42)), (-1, rng.uniform(0.55, 0.68))):
        vein_v = t0 + np.abs(x) * 0.12
        gap = 0.012 * np.clip((np.abs(x) - 0.12) / 0.8, 0, 1)
        tear = (np.abs(v - vein_v) < gap) & (x * side > 0.12)
        alpha = np.where(tear, 0, alpha)
    tone = _soft_tone(x, v, d, hl_at=(-0.3, 0.55))
    tone = tone + 0.07 * np.exp(-(x / 0.03) ** 2) * (v > 0.03)             # the pale midrib
    veins = np.sin((v - np.abs(x) * 0.12) * np.pi * 40)
    tone = tone + 0.025 * np.clip(veins, 0, 1) ** 4 * (np.abs(x) > 0.05)    # soft vein bands
    _save("paddle", tone, alpha)


def blade(w=256, h=1024):
    u, v = _grid(w, h)
    x = (u - 0.5) * 2
    half = 0.9 * (1 - v) ** 0.7 * np.clip(v * 8, 0, 1) ** 0.3
    d = half - np.abs(x)
    tone = 0.72 + 0.28 * v ** 0.9 - 0.05 * (x > 0)                          # a soft centre fold
    _save("blade", tone, d * w / 3.0)


# Tints: (light, dark) per plant. Sunnier and more saturated than Kanto's street greens
# (0.30, 0.64, 0.05 / 0.10, 0.33, 0.04), leaning yellow-green in the light and deeper green in
# the shade, to sit against the turquoise water and tan rock. Nothing near defence blue.
TINTS = {
    "palm":   ((0.46, 0.70, 0.14), (0.10, 0.36, 0.12)),
    "banana": ((0.56, 0.76, 0.18), (0.18, 0.44, 0.10)),
    "taro":   ((0.30, 0.60, 0.16), (0.08, 0.30, 0.12)),
    "bush":   ((0.34, 0.62, 0.10), (0.10, 0.32, 0.08)),
    "grass":  ((0.50, 0.70, 0.18), (0.20, 0.44, 0.12)),
}


# A leaf stalk wears its plant's own green, a little deeper than the leaf's light tint so the stalk
# reads as the stalk and not as more leaf.
STALK_TINTS = {"banana": (0.42, 0.62, 0.16), "taro": (0.30, 0.56, 0.18)}

# Flowers. The reference's orange accents are CRIMSON and YELLOW here (LAGOON_REWORK_GUIDE section
# 8 step 1): orange sits too close to offence orange #f87020, and nothing may approach defence
# blue #0080e8. The red is the blob colour the owner already saw, kept so a bush is the same
# accent from the court.
# The yellow is a WARM yellow once the material's x 1.25 is applied (1.0, 0.85, 0.12), hue about
# 49 degrees against offence orange's 21; sheet v4's (0.98, 0.80, 0.14) clipped to (1, 1, 0.17),
# a lime that read olive on the shaded petals.
FLOWER_TINTS = {"red": (0.80, 0.12, 0.22), "yellow": (0.82, 0.68, 0.10)}


# ---------------------------------------------------------------- the rest of the plant
#
# OWNER, 2026-09-27, reviewing the plants in Blender: "stem part of this leafy plant is
# untextured", "coconut and trunks of palm tree are untextured", "is the pink stuff supposed to
# look like this or did u forget to texture". Every part of a plant now carries its own painted
# drawing in the same hand as the leaves: flat fills, a few broad FEATHERED patches, soft bands,
# no grain, no noise, no streaks (docs/LAGOON_REWORK_GUIDE.md section 2). Each is its OWN drawing:
# the trunk is not the bamboo generator and the stems are not the plank one ("did you just
# repurpose the brick texture?").
# The trunk, the nut and the banana stem are painted in COLOUR (a dry brown sheath on a green
# stem is two hues, which one tint cannot give). The stalk and the two flowers are greyscale and
# tinted per plant like the leaves, so a taro stalk matches its taro leaf and one flower drawing
# serves the crimson and the yellow bushes.


def _mix(img, col, f):
    f = np.clip(f, 0, 1)[..., None]
    return img * (1 - f) + np.array(col, np.float32) * f


def _feather(x, lo, hi):
    """0 below lo, 1 above hi, a smooth feathered edge between."""
    t = np.clip((x - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


def _broad_field(u, v, seed, terms=4, fu=(1, 3), fv=(1, 2)):
    """A few LARGE soft value shapes that tile in U and V: a sum of low whole-number waves, so it
    has no grain at all, only broad patches. Returns 0..1."""
    rng = np.random.default_rng(seed)
    f = np.zeros_like(u)
    for _ in range(terms):
        a, b = rng.integers(fu[0], fu[1] + 1), rng.integers(fv[0], fv[1] + 1)
        # A PRODUCT of a wave round and a wave along, so the shapes are blobs; v4 summed plane
        # waves in (a u + b v), which drew diagonal bands that read as streaks.
        f += (np.cos(2 * np.pi * a * u + rng.uniform(0, 2 * np.pi))
              * np.cos(2 * np.pi * b * v + rng.uniform(0, 2 * np.pi)) * rng.uniform(0.5, 1))
    return (f - f.min()) / (f.max() - f.min() + 1e-6)


def _save_rgb(name, rgb, height=None, strength=3.0):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8), "RGB").save(OUT / f"{name}_albedo.png")
    if height is not None:
        # The house surfaces' rule (owner: "make sure it has depth/normal maps"): the trunk's
        # rings are real ridges, so it ships a height and an OpenGL normal map too.
        h = np.clip(height, 0, 1)
        Image.fromarray((h * 255).astype(np.uint8), "L").save(OUT / f"{name}_height.png")
        gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
        gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
        nrm = np.dstack([-gx, gy, np.ones_like(h)])
        nrm /= np.linalg.norm(nrm, axis=2, keepdims=True)
        Image.fromarray(((nrm * 0.5 + 0.5) * 255).astype(np.uint8), "RGB").save(OUT / f"{name}_normal.png")
    print("[lagoon-leaf]", name)


def palm_trunk(w=512, h=1024, seed=11):
    """A coconut trunk, one tile = 2 m of trunk (V) by once round it (U); tiles both ways.
    Grey-brown, with the leaf-scar RINGS a coconut trunk is known by: irregularly spaced, gently
    wavy, each a soft dark groove with a paler lip just above it (where the old frond base sat),
    some rings fading out part of the way round. Under them, a few broad feathered patches of a
    lighter and a darker grey-brown. No bark grain, no vertical streaks."""
    rng = np.random.default_rng(seed)
    u, v = _grid(w, h)
    img = np.ones((h, w, 3), np.float32) * np.array((0.56, 0.50, 0.43), np.float32)
    img = _mix(img, (0.64, 0.59, 0.51), _feather(_broad_field(u, v, seed, 4), 0.62, 0.78) * 0.8)
    img = _mix(img, (0.46, 0.40, 0.33), _feather(_broad_field(u, v, seed + 1, 4), 0.66, 0.82) * 0.7)
    height = np.full((h, w), 0.5, np.float32)
    # Real rings sit 5 to 15 cm apart; 15 in 2 m, jittered so they never read as a ruled scale.
    n = 15
    pos = (np.arange(n) + rng.uniform(-0.3, 0.3, n)) / n
    for p0 in pos:
        wav = (0.004 * np.sin(2 * np.pi * u + rng.uniform(0, 6.3))
               + 0.0025 * np.sin(4 * np.pi * u + rng.uniform(0, 6.3)))
        d = (v - p0 - wav + 0.5) % 1.0 - 0.5                 # V distance to this ring, wrapped
        fade = np.clip(0.55 + 0.6 * np.cos(2 * np.pi * u + rng.uniform(0, 6.3)), 0.15, 1.0)
        amp = rng.uniform(0.7, 1.0) * fade
        groove = np.exp(-(d / 0.0055) ** 2) * amp            # ~1 cm soft dark groove
        lip = np.exp(-((d - 0.013) / 0.007) ** 2) * amp      # the paler lip just above it
        img = _mix(img, (0.34, 0.29, 0.24), groove * 0.75)
        img = _mix(img, (0.70, 0.65, 0.57), lip * 0.45)
        height += 0.35 * lip - 0.45 * groove
    _save_rgb("palm_trunk", img, height * 0.8 + 0.1, strength=4.0)


def coconut(size=256):
    """One coconut, wrapped round a UV sphere (U around, V bottom 0 to top 1). A green-brown husk,
    browner low down, three soft lighter ridges (the nut is faintly three-sided), a soft darker
    CAP where it hangs from the bunch, and one broad soft highlight at U 0.25 that the kit turns
    to face out of the crown."""
    u, v = _grid(size, size)
    img = np.ones((size, size, 3), np.float32) * np.array((0.50, 0.53, 0.20), np.float32)
    img = _mix(img, (0.52, 0.42, 0.18), _feather(0.5 - v, 0.0, 0.45) * 0.8)
    img = _mix(img, (0.60, 0.62, 0.26), (0.5 + 0.5 * np.cos(2 * np.pi * 3 * u)) ** 3 * 0.35 * (v > 0.1))
    img = _mix(img, (0.30, 0.24, 0.12), _feather(v, 0.78, 0.9))
    du = (u - 0.25 + 0.5) % 1.0 - 0.5
    img = _mix(img, (0.70, 0.72, 0.36), np.exp(-((du / 0.12) ** 2 + ((v - 0.6) / 0.16) ** 2)) * 0.6)
    _save_rgb("coconut", img)


def banana_stem(w=256, h=1024, seed=17):
    """A banana pseudo-stem, U once round it, V base 0 to top 1 (NOT tiled: the dry sheaths belong
    at the base). Soft long SHEATH BANDS lengthwise, a pale yellow-green and a green, each band's
    overlapping edge a soft darker seam with a lighter rim; a few dry brown sheath patches near the
    base, long and feathered, fewer as they climb; the foot darkened where it meets the soil."""
    rng = np.random.default_rng(seed)
    u, v = _grid(w, h)
    img = np.ones((h, w, 3), np.float32) * np.array((0.56, 0.66, 0.30), np.float32)
    edges = np.sort((np.arange(5) + rng.uniform(-0.25, 0.25, 5)) / 5)
    tones = [(0.58, 0.68, 0.32), (0.50, 0.62, 0.26), (0.62, 0.70, 0.36), (0.52, 0.63, 0.28), (0.57, 0.67, 0.31)]
    for k, e in enumerate(edges):
        wob = 0.012 * np.sin(2 * np.pi * (1.3 * v) + rng.uniform(0, 6.3))
        d = (u - e - wob) % 1.0                              # distance past seam k, round the stem
        span = (edges[(k + 1) % 5] - e) % 1.0
        img = _mix(img, tones[k], (d < span) * 0.9)
        ds = (d + 0.5) % 1.0 - 0.5
        img = _mix(img, (0.36, 0.46, 0.18), np.exp(-(ds / 0.008) ** 2) * 0.55)          # the seam
        img = _mix(img, (0.70, 0.76, 0.42), np.exp(-((ds - 0.016) / 0.008) ** 2) * 0.35)  # its rim
    img = _mix(img, (0.66, 0.74, 0.40), _feather(v, 0.5, 1.0) * 0.35)             # lighter up top
    for _ in range(4):
        cu, top = rng.uniform(0, 1), rng.uniform(0.2, 0.42)
        du = (u - cu + 0.5) % 1.0 - 0.5
        wid = rng.uniform(0.06, 0.11)
        shape = np.clip(1 - (du / wid) ** 2 - np.clip((v - top * 0.4) / (top * 0.6), 0, 1) ** 2, 0, 1)
        img = _mix(img, (0.50, 0.38, 0.22), _feather(shape, 0.0, 0.35) * 0.9)
        img = _mix(img, (0.40, 0.29, 0.16), _feather(shape, 0.45, 0.8) * 0.5)
    img = _mix(img, (0.36, 0.30, 0.18), _feather(0.1 - v, 0.0, 0.1))
    _save_rgb("banana_stem", img)


def stalk(w=128, h=512):
    """A leaf stalk (banana petiole, taro stalk), greyscale for the plant's tint: a plain soft
    gradient, darker at the foot and lighter toward the leaf, with one soft pale stripe down the
    side the channel runs along. U once round, V foot 0 to leaf 1."""
    u, v = _grid(w, h)
    g = 0.70 + 0.22 * v ** 0.8
    g = g + 0.06 * np.exp(-(((u - 0.25 + 0.5) % 1.0 - 0.5) / 0.08) ** 2)
    g = g - 0.05 * np.exp(-(((u - 0.75 + 0.5) % 1.0 - 0.5) / 0.1) ** 2)
    _save_rgb("stalk", np.dstack([g, g, g]) * 0.92)


# A flower drawing is greyscale for the plant's tint, and its pale parts (the gumamela's stamen,
# the bougainvillea's tiny true flowers) are painted at FULL WHITE while every petal stays at or
# under FLOWER_PETAL_MAX. The flower material (lagoon_cove_planting.flower_material) and the sheet
# both key on that: above FLOWER_KEY the tint gives way to FLOWER_PALE, so one tinted drawing still
# carries a cream stamen on a crimson flower. Values are the PNG's own (sRGB) numbers.
# 0.86, still under the key: at 0.80 (test v4) the shaded petals went a dull mustard on the yellow
# bushes and a dark magenta on the red ones.
FLOWER_PETAL_MAX = 0.86
FLOWER_KEY = (0.88, 0.97)
FLOWER_PALE = (0.98, 0.93, 0.70)


def _save_flower(name, g, alpha):
    OUT.mkdir(parents=True, exist_ok=True)
    rgba = np.dstack([g, g, g, np.clip(alpha, 0, 1)])
    Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA").save(OUT / f"{name}_albedo.png")
    print("[lagoon-leaf]", name)


def gumamela(size=512, seed=19):
    """A gumamela (hibiscus) seen face on: five broad soft petals turning like a pinwheel, each
    overlapping the next with a soft shadow on the overlap, shallow notches between them and a
    slight ruffle; a darker EYE at the centre; the long pale staminal column sweeping out past
    the eye with a soft pale tuft on its end. Everything inside the card with a margin."""
    rng = np.random.default_rng(seed)
    u, v = _grid(size, size)
    x, y = (u - 0.5) * 2, (v - 0.5) * 2
    r, th = np.hypot(x, y), np.arctan2(y, x)
    rot = rng.uniform(0, 2 * np.pi)
    ph = ((th - rot) / (2 * np.pi / 5)) % 1.0                # 0..1 across each petal
    edge = 0.90 - 0.13 * np.exp(-((np.minimum(ph, 1 - ph)) / 0.07) ** 2) + 0.015 * np.sin(th * 23)
    d = edge - r
    g = FLOWER_PETAL_MAX * (0.80 + 0.2 * _feather(r, 0.2, 0.85))
    g = g - 0.12 * np.exp(-((ph - 0.05) / 0.06) ** 2) * _feather(r, 0.15, 0.4)   # overlap shadow
    g = g + 0.05 * np.exp(-(((x + 0.3) / 0.35) ** 2 + ((y - 0.3) / 0.3) ** 2))      # sunlit patch
    g = g - 0.34 * (1 - _feather(r, 0.12, 0.32))                                  # the dark eye
    g = np.minimum(g, FLOWER_PETAL_MAX)
    # The staminal column: a soft pale stroke from the eye out to 0.6, and its pollen tuft.
    ca = rot + np.pi / 5 + 0.35
    cx, cy = np.cos(ca), np.sin(ca)
    along, across = x * cx + y * cy, np.abs(-x * cy + y * cx)
    col = _feather(0.022 - across, 0, 0.012) * (along > 0.05) * (along < 0.6)
    tuft = _feather(0.075 - np.hypot(x - cx * 0.63, y - cy * 0.63), 0, 0.04)
    pale = np.maximum(col, tuft)
    g = g * (1 - pale) + pale
    _save_flower("gumamela", g, np.maximum(d * size / 2.5, pale))


def bougainvillea(size=512, seed=23):
    """A bougainvillea bract cluster: three papery BRACTS, broad ovals with a pointed tip, radiating
    from the centre at thirds, each its own soft value (papery, light at the tip, darker in at the
    base) with a faint lighter midline; three tiny pale true flowers in the middle."""
    rng = np.random.default_rng(seed)
    u, v = _grid(size, size)
    x, y = (u - 0.5) * 2, (v - 0.5) * 2
    alpha = np.zeros_like(x)
    g = np.zeros_like(x)
    rot = rng.uniform(0, 2 * np.pi)
    tones = (0.84, 0.76, 0.80)
    for k in range(3):
        a = rot + k * 2 * np.pi / 3 + rng.uniform(-0.12, 0.12)
        ax, ay = np.cos(a), np.sin(a)
        along, across = x * ax + y * ay, -x * ay + y * ax
        s = np.clip(along / rng.uniform(0.84, 0.9), 0, 1)
        half = 0.46 * np.sin(np.pi * s ** 0.75) ** 0.8 * (1 - 0.15 * s)
        a_k = np.clip((half - np.abs(across)) * (along > -0.02) * size / 2.5, 0, 1)
        t = tones[k] * (0.82 + 0.18 * s) + 0.04 * np.exp(-(across / 0.02) ** 2) * (s > 0.1)
        g = np.where(a_k > alpha * 0.9, np.minimum(t, FLOWER_PETAL_MAX), g)   # later bracts on top
        alpha = np.maximum(alpha, a_k)
    pale = np.zeros_like(x)
    for k in range(3):
        a = rot + k * 2 * np.pi / 3 + np.pi / 3
        ax, ay = np.cos(a), np.sin(a)
        along, across = x * ax + y * ay, np.abs(-x * ay + y * ax)
        tube = _feather(0.028 - across, 0, 0.012) * (along > 0) * (along < 0.2)
        star = _feather(0.05 - np.hypot(x - ax * 0.22, y - ay * 0.22), 0, 0.03)
        pale = np.maximum(pale, np.maximum(tube, star))
    g = g * (1 - pale) + pale
    _save_flower("bougainvillea", g, np.maximum(alpha, pale))


def _tinted(g, tint, key=False):
    """What the material does to a greyscale drawing: multiply by tint x 1.25 (the leaf rule), and
    for a flower, give way to FLOWER_PALE above FLOWER_KEY."""
    rgb = g[..., :3] * np.array([min(1.0, c * 1.25) for c in tint])
    if key:
        k = _feather(g[..., 0], *FLOWER_KEY)[..., None]
        rgb = rgb * (1 - k) + np.array(FLOWER_PALE) * k
    return rgb


def sheet(version):
    """Each drawing on a sand-coloured card as its material shows it: the leaves tinted light and
    dark for their plant (row 1); the trunk (two tiles tall, so the ring tiling shows), coconut,
    banana stem, both stalks, both flowers in both colours, and the frond's TIP enlarged, the part
    the owner saw cut off (row 2)."""
    cells = []
    for leaf, plant in [("leaf_round", "bush"), ("frond", "palm"), ("paddle", "banana"), ("paddle", "taro"),
                        ("blade", "grass")]:
        g = np.asarray(Image.open(OUT / f"{leaf}_albedo.png").convert("RGBA")).astype(np.float32) / 255
        for which, tint in zip(("light", "dark"), TINTS[plant]):
            cells.append((0, f"{leaf} / {plant} {which}", np.dstack([_tinted(g, tint), g[..., 3]])))

    def rgb_of(name):
        a = np.asarray(Image.open(OUT / f"{name}_albedo.png").convert("RGBA")).astype(np.float32) / 255
        return a
    trunk = rgb_of("palm_trunk")
    cells.append((1, "palm_trunk (2 tiles = 4 m)", np.concatenate([trunk, trunk], axis=0)))
    cells.append((1, "coconut (unwrapped)", rgb_of("coconut")))
    cells.append((1, "banana_stem (base at bottom)", rgb_of("banana_stem")))
    st = rgb_of("stalk")
    for plant in ("banana", "taro"):
        cells.append((1, f"stalk / {plant}", np.dstack([_tinted(st, STALK_TINTS[plant]), st[..., 3]])))
    for fl in ("gumamela", "bougainvillea"):
        g = rgb_of(fl)
        for col, tint in FLOWER_TINTS.items():
            cells.append((1, f"{fl} / {col}", np.dstack([_tinted(g, tint, key=True), g[..., 3]])))
    fr = rgb_of("frond")
    tip = fr[: fr.shape[0] // 4]
    cells.append((1, "frond tip x2 (top quarter)", np.dstack([_tinted(tip, TINTS["palm"][0]), tip[..., 3]])))

    cw, ch, pad, head = 260, 420, 14, 34
    per_row = max(sum(1 for r, *_ in cells if r == 0), sum(1 for r, *_ in cells if r == 1))
    page = Image.new("RGB", (pad + per_row * (cw + pad), 2 * (ch + head + pad) + pad), (232, 214, 170))
    d = ImageDraw.Draw(page)
    try:
        font = ImageFont.truetype("arial.ttf", 15)
    except OSError:
        font = ImageFont.load_default()
    col_of = {0: 0, 1: 0}
    for row, label, arr in cells:
        img = Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8), "RGBA")
        scale = min(cw / img.width, ch / img.height)
        img = img.resize((max(1, int(img.width * scale)), max(1, int(img.height * scale))), Image.LANCZOS)
        x = pad + col_of[row] * (cw + pad)
        y0 = pad + row * (ch + head + pad)
        page.paste(img, (x + (cw - img.width) // 2, y0 + head), img)
        d.text((x, y0 + 8), label, fill=(40, 36, 32), font=font)
        col_of[row] += 1
    out = SHEETS / f"leaves_swatches_v{version}.png"
    page.save(out)
    print("[lagoon-leaf] sheet", out)


def main():
    if "--sheet" in sys.argv:
        sheet(int(sys.argv[sys.argv.index("--sheet") + 1]))
        return
    leaf_round()
    frond()
    paddle()
    blade()
    palm_trunk()
    coconut()
    banana_stem()
    stalk()
    gumamela()
    bougainvillea()


if __name__ == "__main__":
    main()
