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
  frond_broad  the reference's CHUNKY frond: eleven wide, pointed, drooping leaflets a side, COLOUR
               (green at the stalk to yellow-green, warm straw leaflet tips); the palms' crown
  croton_leaf  a croton leaf, COLOUR: crimson to coral-pink with a yellow midrib, veins, splashes
  monstera_leaf  a monstera leaf: a broad heart, a notch at the stalk, edge splits and inner holes
Greyscale RGBA PNGs to ArtSource/lagoon/textures/<name>_albedo.png, except the two COLOUR ones
(tinted by COLOUR_TINTS at gain 1.0; see "the reference's chunkier plants" below).

The rest of each plant is painted here too, in the same hand (see "the rest of the plant" below):
  palm_trunk     coconut trunk, warm banded tan and soft orange-brown with soft leaf-scar seams;
                 tiles, 2 m a tile; colour, with _height and _normal
  coconut        the nut's husk, wrapped round a UV sphere; colour
  banana_stem    the banana pseudo-stem, lengthwise sheath bands, dry brown sheaths at the base
  stalk          a leaf stalk, greyscale, tinted per plant (STALK_TINTS: banana, taro, croton,
                 monstera)
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
    """A grass / pandan blade.
    ⚠️ OWNER, 2026-09-27, on the tufts in the cove: "the last types of foliage and fauna added dont
    have much textures on them". v1 was a plain 0.72 to 1.0 ramp with a 5 % step at the fold, which
    the tint flattened to one green. Now, in the same hand as the paddle: a clear fold (a dark crease
    with a pale ridge on its lit side), the lit half a step lighter than the shaded half, two soft
    pale vein bands per half that follow the taper, a darker rim toward the base and a sunlit patch.
    Still no grain or noise: every mark is a broad feathered band."""
    u, v = _grid(w, h)
    x = (u - 0.5) * 2
    half = 0.9 * (1 - v) ** 0.7 * np.clip(v * 8, 0, 1) ** 0.3
    d = half - np.abs(x)
    xn = x / np.maximum(half, 1e-3)                                          # -1..1 across, at any width
    # Round 2 (plants_tuftclose_after_v1): at 5 to 14 % the marks vanished on a 3 cm wide card.
    # Every mark is now about twice as strong and twice as wide; still broad and feathered.
    tone = 0.52 + 0.34 * np.clip(v, 0, 1) ** 0.8                             # stem dark to tip light
    tone = tone + 0.18 * _feather(-xn, -0.1, 0.3)                             # the lit half
    tone = tone - 0.22 * np.exp(-(xn / 0.1) ** 2)                             # the crease
    tone = tone + 0.14 * np.exp(-((xn + 0.22) / 0.1) ** 2)                    # its pale ridge
    for c in (0.58, -0.62):
        tone = tone + 0.10 * np.exp(-((xn - c) / 0.1) ** 2)                   # soft vein bands
    tone = tone - 0.16 * _feather(np.abs(xn), 0.72, 1.0)                      # a darker margin
    tone = tone - 0.12 * (1 - _feather(v, 0.05, 0.3))                         # a darker foot
    tone = tone + 0.12 * np.exp(-(((xn + 0.4) / 0.4) ** 2 + ((v - 0.55) / 0.2) ** 2))   # sunlit patch
    _save("blade", tone, d * w / 3.0)


# ---------------------------------------------------------------- the reference's chunkier plants
#
# docs/LAGOON_REWORK_GUIDE.md section 7a, items 6 and 7, comparing our cove with the reference's
# foliage sheet (ArtStation GvJv5a): its palm fronds are "broad and chunky with a yellow-green
# gradient" where ours were "thin-leafed", and its rock feet carry "red and orange ferny accents
# (croton), monstera-like round leaves, dense low ground cover". Three new drawings follow, still
# in the owner's approved Kanto hand ("i really like what was used for leaves in kanto. just need
# to ensure it matches this environment"): soft value from stem to tip, one sunlit patch, no
# outline, a crisp alpha silhouette.
#
# TWO OF THEM ARE PAINTED IN COLOUR, AND THAT IS A DELIBERATE EXCEPTION. A frond that runs from
# green at the stalk to yellow-green to a warm tip, and a croton leaf that is crimson with yellow
# veins, each carry two or three hues inside ONE leaf, which a single multiplied tint cannot give.
# They keep the rest of the rule: the material still multiplies them by a per-plant pair from
# COLOUR_TINTS (light on top, dark underneath, gain 1.0 because the drawing already carries its
# own colour), and the shader's per-object random still nudges each plant. Per plant, never per
# leaf. The monstera is one green, so it stays greyscale plus TINTS like every Kanto leaf.

def _save_colour(name, rgb, alpha):
    OUT.mkdir(parents=True, exist_ok=True)
    rgba = np.dstack([np.clip(rgb, 0, 1), np.clip(alpha, 0, 1)])
    Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").save(OUT / f"{name}_albedo.png")
    print("[lagoon-leaf]", name)


def _lerp(a, b, f):
    f = np.clip(f, 0, 1)[..., None]
    a = np.asarray(a, np.float32)
    b = np.asarray(b, np.float32)
    return a * (1 - f) + b * f


def frond_broad(w=512, h=1024, seed=7):
    """The reference's CHUNKY coconut frond: eleven leaflets a side (the thin frond has 24), each
    about three times as wide, bowed so it droops toward the stalk, pointed at its end, and
    overlapping its neighbour a little so the frond reads as a few bold shapes and not a comb.
    Colour runs green at the stalk to a sunny yellow-green up the frond, and every leaflet warms
    to a soft straw at its own tip (hue about 45 degrees, far from offence orange's 21). Drawn
    base at the bottom, inside the card with a margin (the owner's "leaves of palmtrees are also
    cut off" rule from the thin frond still holds)."""
    rng = np.random.default_rng(seed)
    u, v = _grid(w, h)
    x = (u - 0.5) * 2
    vy = v * 2.0
    mid = 0.05 * np.sin(np.pi * v)
    xr = x - mid
    alpha = np.zeros_like(x)
    val = np.zeros_like(x)
    warm = np.zeros_like(x)
    n = 11
    for side in (-1, 1):
        for k in range(n):
            # The two sides are staggered by half a leaflet, as on a real frond.
            t = 0.07 + 0.88 * k / n + rng.uniform(-0.01, 0.01) + (0.04 if side > 0 else 0.0)
            # Sheet v7's leaflets stopped halfway to the card edge and the frond read as a fern or
            # an oak leaf. They are LONG now, reaching the card edge, at a shallower comb angle, so
            # the frond is a few wide straps hanging off a rib, the reference's silhouette.
            length = 1.3 * min(1.0, t / 0.18) ** 0.5 * np.clip((0.99 - t) / 0.7, 0, 1) ** 0.7
            length *= np.clip((0.99 - t) / 0.12, 0, 1) ** 0.6
            ang = np.radians(rng.uniform(30, 38))
            dx, dy = side * np.cos(ang), np.sin(ang)
            ox = 0.05 * np.sin(np.pi * t)
            fit_top = (1.9 - t * 2.0) / dy
            fit_side = (0.86 - side * ox) / abs(dx)
            length = min(length, fit_top, fit_side)
            if length < 0.04:
                continue
            px, py = xr, vy - t * 2.0
            along = px * dx + py * dy
            s = np.clip(along / (length + 1e-6), 0, 1)
            # The droop: the leaflet's centre line bows toward the stalk end of the card, more
            # toward its tip, so each leaflet hangs like the reference's rather than sticking out.
            bow = side * 0.2 * length * s ** 2
            across = np.abs(px * dy - py * dx - bow)
            # Pointed ends (v8's rounded ends read as petals), and a little narrower so a sliver of
            # light shows between neighbours.
            width = (0.088 * np.sin(np.pi * s ** 0.8) ** 0.6 * np.clip((1 - s) / 0.4, 0, 1) ** 0.8
                     * min(1.0, length / 0.3 + 0.35) + 0.008 * (1 - s))
            a = np.clip((width - across) * w / 3.0, 0, 1) * ((along > 0) & (along < length))
            # Value: darker where the leaflet leaves the rib, lighter out along it.
            lt = 0.74 + 0.2 * s ** 0.8 + 0.05 * np.exp(-((s - 0.55) / 0.22) ** 2)
            new = a > alpha
            val = np.where(new, lt, val)
            warm = np.where(new, _feather(s, 0.55, 1.0), warm)
            alpha = np.maximum(alpha, a)
    rib_half = 0.03 * np.clip((0.97 - v) / 0.97, 0, 1) ** 0.7 + 0.002 * (v < 0.97)
    rib = np.clip((rib_half - np.abs(xr)) * w / 3.0, 0, 1) * (v < 0.97)
    on_rib = rib > alpha * 0.5
    val = np.where(on_rib, 0.84 + 0.08 * v, val)
    warm = np.where(on_rib, 0.35, warm)
    alpha = np.maximum(alpha, rib)
    # The frond-long gradient, then each leaflet's warm tip, then Kanto's one soft sunlit patch.
    col = _lerp((0.34, 0.58, 0.12), (0.72, 0.84, 0.24), _feather(v, 0.0, 0.8) ** 0.8)
    col = _lerp(col, (0.92, 0.82, 0.36), 0.7 * warm)
    val = val + 0.08 * np.exp(-(((x + 0.3) / 0.4) ** 2 + ((v - 0.6) / 0.25) ** 2))
    _save_colour("frond_broad", col * val[..., None], alpha)


def croton_leaf(w=512, h=1024, seed=29):
    """A croton leaf, the red-and-yellow variegated shrub at every Filipino house front: a long
    pointed oval, deep crimson at the stalk warming to a crimson-rose and a coral-pink tip, with a
    YELLOW midrib and soft yellow side veins combing up toward the tip, and a few soft yellow
    splashes between them the way a real croton is marked. Hues stay crimson (about 350 degrees)
    to coral-pink (about 355) and yellow (about 48): offence orange #f87020 sits at 21 and nothing
    here goes near it."""
    rng = np.random.default_rng(seed)
    u, v = _grid(w, h)
    x = (u - 0.5) * 2
    half = 0.80 * np.clip(np.sin(np.pi * np.clip(v, 0, 1) ** 0.85), 0, 1) ** 0.75 * (1 - 0.22 * v)
    d = half - np.abs(x)
    rel = np.clip(np.abs(x) / np.maximum(half, 1e-3), 0, 1)       # 0 at the midrib, 1 at the edge
    # Brighter than test v2's (0.40 / 0.72 crimson), which went maroon at game distance; the
    # reference's accents are the brightest thing at a rock foot.
    col = _lerp((0.54, 0.08, 0.12), (0.80, 0.13, 0.19), _feather(v, 0.0, 0.4))
    col = _lerp(col, (0.88, 0.34, 0.40), _feather(v, 0.55, 1.0) * 0.8)
    yellow = (0.95, 0.80, 0.24)
    # Side veins: soft lines leaving the midrib and running up toward the edge, fading out there.
    ph = (v - np.abs(x) * 0.45) * 8.0
    fr = ph - np.floor(ph)
    vein = np.exp(-((fr - 0.5) / 0.07) ** 2) * (1 - rel) ** 0.6 * (v > 0.06) * (v < 0.92)
    col = _lerp(col, yellow, 0.8 * vein)
    # Splashes: a few soft round patches of yellow, feathered, only on the leaf's inner half.
    for _ in range(7):
        cx, cv = rng.uniform(-0.45, 0.45), rng.uniform(0.2, 0.8)
        r = rng.uniform(0.07, 0.13)
        sp = _feather(1 - np.hypot(x - cx, (v - cv) * 2.0) / r, 0.0, 0.5) * (1 - rel)
        col = _lerp(col, yellow, 0.55 * sp)
    rib = np.exp(-(x / 0.035) ** 2) * (v > 0.02) * (v < 0.95)
    col = _lerp(col, yellow, 0.9 * rib)
    tone = _soft_tone(x, v, d, hl_at=(-0.3, 0.6))
    _save_colour("croton_leaf", col * tone[..., None], d * w / 2.5)


def monstera_leaf(size=1024, seed=31):
    """A monstera leaf seen flat: a broad heart, the stalk meeting it in a notch at the bottom, a
    soft pale midrib to a gently pointed tip, and the SPLITS it is known by, running in from the
    edge along the side veins, wider at the edge and closing toward the rib, with a few rounded
    holes in an inner row between them. Greyscale for the plant's tint (one green)."""
    rng = np.random.default_rng(seed)
    u, v = _grid(size, size)
    X, Y = u - 0.5, v - 0.48
    r, th = np.hypot(X, Y), np.arctan2(Y, X)
    R = (0.44 - 0.13 * np.exp(-((th + np.pi / 2) / 0.16) ** 2)        # the notch at the stalk
         + 0.03 * np.exp(-((th - np.pi / 2) / 0.28) ** 2))             # the pointed tip
    d = R - r
    alpha = d * size / 2.0
    rib_x = 0.012 * np.sin(np.pi * v)
    tone = _soft_tone((X - rib_x) * 2, v, d * 2, hl_at=(-0.3, 0.6))
    tone = tone + 0.05 * np.exp(-((X - rib_x) / 0.012) ** 2) * (v > 0.14) * (v < 0.95)
    for side in (-1, 1):
        ks = 5
        for k in range(ks):
            y0 = -0.26 + 0.5 * k / (ks - 1) + rng.uniform(-0.02, 0.02) + (0.03 if side > 0 else 0)
            ang = np.radians(rng.uniform(22, 34) + 14 * (1 - k / ks))
            dx, dy = side * np.cos(ang), np.sin(ang)
            px, py = X - rib_x, Y - y0
            along = px * dx + py * dy
            across = px * dy - py * dx
            tone = tone + 0.03 * np.exp(-(across / 0.006) ** 2) * (along > 0)     # the side vein
            # The split: begins part of the way out and widens to the edge.
            start = 0.17 + rng.uniform(-0.02, 0.03)
            gap = 0.004 + 0.022 * np.clip((along - start) / 0.25, 0, 1)
            split = (np.abs(across) < gap) & (along > start)
            alpha = np.where(split, np.minimum(alpha, (np.abs(across) - gap) * size / 2.0), alpha)
            # A rounded hole in the inner row, between this vein and the next.
            if k < ks - 1 and rng.random() < 0.75:
                hx, hy = side * rng.uniform(0.08, 0.11), y0 + 0.07
                hr = rng.uniform(0.016, 0.026)
                hole = np.hypot(X - hx, (Y - hy) / 1.6) - hr
                alpha = np.minimum(alpha, hole * size / 2.0)
    _save("monstera_leaf", tone, alpha)


# Tints: (light, dark) per plant. Sunnier and more saturated than Kanto's street greens
# (0.30, 0.64, 0.05 / 0.10, 0.33, 0.04), leaning yellow-green in the light and deeper green in
# the shade, to sit against the turquoise water and tan rock. Nothing near defence blue.
# "palm" is now the YOUNG fronds' pair (the thin frond drawing); it moved warmer and yellower
# (was (0.46, 0.70, 0.14) / (0.10, 0.36, 0.12)) to sit with the broad fronds' sunny colour, per the
# reference's yellow-green palms (LAGOON_REWORK_GUIDE section 7a item 6).
# "monstera" is a deep glossy green, darker than the taro's, so a monstera at a rock foot is not
# read as another taro. "groundcover" is a fresher, slightly yellower green than the bushes, since
# it lies in the sun on sand and rock feet; it wears Kanto's own round leaf.
TINTS = {
    "palm":   ((0.56, 0.72, 0.14), (0.20, 0.42, 0.10)),
    "monstera": ((0.30, 0.56, 0.11), (0.09, 0.29, 0.08)),
    "groundcover": ((0.40, 0.64, 0.09), (0.12, 0.36, 0.06)),
    "banana": ((0.56, 0.76, 0.18), (0.18, 0.44, 0.10)),
    "taro":   ((0.30, 0.60, 0.16), (0.08, 0.30, 0.12)),
    "bush":   ((0.34, 0.62, 0.10), (0.10, 0.32, 0.08)),
    "grass":  ((0.50, 0.70, 0.18), (0.20, 0.44, 0.12)),
}


# A leaf stalk wears its plant's own green, a little deeper than the leaf's light tint so the stalk
# reads as the stalk and not as more leaf.
STALK_TINTS = {"banana": (0.42, 0.62, 0.16), "taro": (0.30, 0.56, 0.18),
               # A croton's short stems are woody, a dull red-brown; a monstera stalk is its green.
               "croton": (0.50, 0.34, 0.26), "monstera": (0.28, 0.52, 0.16)}

# Multipliers for the two COLOUR drawings (frond_broad, croton_leaf), applied at gain 1.0 (the
# drawing already carries its colour; the greyscale leaves' x 1.25 would blow it out). Light on
# top, dark underneath, per plant: the hanging lower tier of a palm is the dark one, as before.
COLOUR_TINTS = {
    "palm_broad": ((1.0, 1.0, 0.96), (0.58, 0.66, 0.50)),
    "croton":     ((1.0, 0.98, 0.98), (0.62, 0.52, 0.54)),
}

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
    REPAINTED 2026-09-27 against the reference's foliage sheet (docs/LAGOON_REWORK_GUIDE.md section
    7a item 6: "warm banded orange-brown bark"; the grey-brown v1 read as concrete beside warm
    sand). It is now a stack of soft horizontal BANDS, one per old frond base: each band is warm
    tan at its top edge (the fresh lip where the frond sat) settling into a soft orange-brown
    toward its foot, the bands jittered in height and hue so they never read as a ruled scale.
    The leaf-scar RING between two bands is kept but softer and narrower than before (a gentle
    darker seam, not a groove), wavy and fading part of the way round. A few broad feathered
    patches sit on top so a tall trunk is not one repeated stripe. No grain, no vertical streaks.
    The browns stay low in saturation (hue about 28 to 34 degrees, saturation under 0.5), well
    clear of offence orange #f87020, which is a saturated 21 degrees."""
    rng = np.random.default_rng(seed)
    u, v = _grid(w, h)
    tan = np.array((0.80, 0.66, 0.47), np.float32)
    brown = np.array((0.63, 0.45, 0.30), np.float32)
    img = np.ones((h, w, 3), np.float32) * brown
    height = np.full((h, w), 0.5, np.float32)
    # Eleven bands in 2 m (about 18 cm each): the reference's bands are bold enough to read from
    # across the cove, where the old 15 thin rings merged into a grey smear.
    n = 11
    pos = np.sort((np.arange(n) + rng.uniform(-0.22, 0.22, n)) / n)
    waves = []
    for p0 in pos:
        wav = (0.005 * np.sin(2 * np.pi * u + rng.uniform(0, 6.3))
               + 0.003 * np.sin(4 * np.pi * u + rng.uniform(0, 6.3)))
        waves.append(wav)
    for k, p0 in enumerate(pos):
        p1 = pos[(k + 1) % n] + (1.0 if k == n - 1 else 0.0)
        span = p1 - p0
        # s: 0 just above this band's lower ring, 1 at the next ring up (wrapped, so it tiles).
        s = ((v - p0 - waves[k]) % 1.0) / span
        inside = s < 1.0
        # Brown at the foot of the band rising to tan under the ring above (the lip).
        f = _feather(s, 0.15, 0.95)
        shade = rng.uniform(-0.03, 0.03)
        col = brown[None, None, :] * (1 - f[..., None]) + tan[None, None, :] * f[..., None] + shade
        img = np.where(inside[..., None], col, img)
        height = np.where(inside, 0.45 + 0.25 * f, height)
    for k, p0 in enumerate(pos):
        d = (v - p0 - waves[k] + 0.5) % 1.0 - 0.5
        fade = np.clip(0.6 + 0.55 * np.cos(2 * np.pi * u + rng.uniform(0, 6.3)), 0.25, 1.0)
        seam = np.exp(-(d / 0.0045) ** 2) * fade             # a soft, narrow seam, not a groove
        img = _mix(img, (0.46, 0.32, 0.21), seam * 0.55)
        height -= 0.3 * seam
    img = _mix(img, (0.84, 0.72, 0.54), _feather(_broad_field(u, v, seed, 4), 0.66, 0.82) * 0.35)
    img = _mix(img, (0.55, 0.39, 0.27), _feather(_broad_field(u, v, seed + 1, 4), 0.68, 0.84) * 0.35)
    _save_rgb("palm_trunk", img, np.clip(height, 0, 1) * 0.8 + 0.1, strength=3.0)


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
    # ⚠️ OWNER, 2026-09-27: "the last types of foliage and fauna added dont have much textures on
    # them", circling the banana and taro stalks. v1 was a 0.70 to 0.92 ramp with 6 % stripes: one
    # flat green once tinted. Now the stalk has a real CHANNEL (a broad pale groove edged by two
    # soft dark lines, and a dip in the height map), a shaded back, a darker clasping foot, and a
    # few broad feathered patches. Same hand as the leaves: no grain, no noise, no streaks.
    u, v = _grid(w, h)

    def ring(c, s):
        return np.exp(-(((u - c + 0.5) % 1.0 - 0.5) / s) ** 2)
    # Round 2 (plants_stalkclose_after_v1): a 6-sided tube 3 to 4.5 cm across shows only one
    # or two faces, so the marks must be broad and strong to read at all.
    g = 0.50 + 0.36 * v ** 0.8
    g = g + 0.24 * ring(0.25, 0.12)                                      # the channel's pale floor
    g = g - 0.18 * (ring(0.10, 0.04) + ring(0.40, 0.04))                 # its two dark edges
    g = g - 0.20 * ring(0.75, 0.18)                                      # the shaded back
    g = g + 0.16 * np.exp(-((v - 0.97) / 0.03) ** 2)                     # a pale collar at the leaf
    g = g - 0.22 * (1 - _feather(v, 0.04, 0.2))                          # the darker clasping foot
    g = g + 0.14 * (_broad_field(u, v, 41, terms=3, fu=(1, 2), fv=(2, 4)) - 0.5)
    # Round 3 (plants_stalkclose_after_v2): the taro stalks still read plain, so four soft
    # lengthwise stripes run round the stalk, fading out toward the leaf, the way a taro petiole
    # is lined. Four across the whole circumference, so each is a band a face wide, not a hairline.
    g = g - 0.12 * np.clip(np.cos(2 * np.pi * 4 * (u + 0.06)), 0, 1) ** 2 * (1 - 0.6 * v)
    height = 0.6 - 0.35 * ring(0.25, 0.08) + 0.1 * ring(0.75, 0.2)
    _save_rgb("stalk", np.dstack([g, g, g]) * 0.92, height=height, strength=2.0)


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


def _tinted(g, tint, key=False, gain=1.25):
    """What the material does to a drawing: multiply by tint x gain (1.25 for a greyscale leaf, the
    leaf rule; 1.0 for a colour one), and for a flower, give way to FLOWER_PALE above FLOWER_KEY."""
    rgb = g[..., :3] * np.array([min(1.0, c * gain) for c in tint])
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

    for leaf, plant, gain, tints in [("frond_broad", "palm", 1.0, COLOUR_TINTS["palm_broad"]),
                                     ("croton_leaf", "croton", 1.0, COLOUR_TINTS["croton"]),
                                     ("monstera_leaf", "monstera", 1.25, TINTS["monstera"]),
                                     ("leaf_round", "groundcover", 1.25, TINTS["groundcover"])]:
        g = np.asarray(Image.open(OUT / f"{leaf}_albedo.png").convert("RGBA")).astype(np.float32) / 255
        for which, tint in zip(("light", "dark"), tints):
            cells.append((1, f"{leaf} / {plant} {which}", np.dstack([_tinted(g, tint, gain=gain), g[..., 3]])))

    def rgb_of(name):
        a = np.asarray(Image.open(OUT / f"{name}_albedo.png").convert("RGBA")).astype(np.float32) / 255
        return a
    trunk = rgb_of("palm_trunk")
    cells.append((2, "palm_trunk (2 tiles = 4 m)", np.concatenate([trunk, trunk], axis=0)))
    cells.append((2, "coconut (unwrapped)", rgb_of("coconut")))
    cells.append((2, "banana_stem (base at bottom)", rgb_of("banana_stem")))
    st = rgb_of("stalk")
    for plant in ("banana", "taro", "croton", "monstera"):
        cells.append((2, f"stalk / {plant}", np.dstack([_tinted(st, STALK_TINTS[plant]), st[..., 3]])))
    for fl in ("gumamela", "bougainvillea"):
        g = rgb_of(fl)
        for col, tint in FLOWER_TINTS.items():
            cells.append((2, f"{fl} / {col}", np.dstack([_tinted(g, tint, key=True), g[..., 3]])))
    fr = rgb_of("frond")
    tip = fr[: fr.shape[0] // 4]
    cells.append((2, "frond tip x2 (top quarter)", np.dstack([_tinted(tip, TINTS["palm"][0]), tip[..., 3]])))

    cw, ch, pad, head = 260, 420, 14, 34
    rows = 1 + max(r for r, *_ in cells)
    per_row = max(sum(1 for r, *_ in cells if r == k) for k in range(rows))
    page = Image.new("RGB", (pad + per_row * (cw + pad), rows * (ch + head + pad) + pad), (232, 214, 170))
    d = ImageDraw.Draw(page)
    try:
        font = ImageFont.truetype("arial.ttf", 15)
    except OSError:
        font = ImageFont.load_default()
    col_of = {k: 0 for k in range(rows)}
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
    frond_broad()
    croton_leaf()
    monstera_leaf()
    palm_trunk()
    coconut()
    banana_stem()
    stalk()
    gumamela()
    bougainvillea()


if __name__ == "__main__":
    main()
