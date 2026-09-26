"""Paint Lagoon Court's tileable textures in TUMP's illustrated style.

  py -3 tools/author_lagoon_textures.py rock_a,rock_b,rock_c     # paint the named textures
  py -3 tools/author_lagoon_textures.py --sheet N                  # swatch sheet vN for review

Writes ArtSource/lagoon/textures/<name>_albedo.png, _height.png and _normal.png (OpenGL
convention, green up). LAGOON_TEX_OUT redirects output so a swatch is reviewed before it
replaces a texture. Every texture tiles and covers TILE_M metres.

THE HOUSE STYLE (docs/KANTO_DESIGN_GUIDE.md § 3, owner-approved): flat fills; value changes
from a few LARGE patches with FEATHERED organic edges; hand-drawn shapes, never ruled; light
drawn in cel-style as one flat band, not a gradient; low contrast; no grain, no noise, no
streaks. ⚠️ Every surface gets its OWN drawing (the owner rejected bark and roof tiles that were
the brick generator in disguise). This file shares no generator with author_kanto_textures.py;
only the approved Kanto swatches are read, to sit beside the new ones on the sheet.

ROCK (docs/LAGOON_REWORK_GUIDE.md § 8 step 2). Research: stylized painted rock reads from a few
clean BROAD PLANES with light drawn on their upper edges and very little surface detail; the
light-top, dark-base gradient belongs to the MATERIAL (it depends on which way a face points,
which a tiling texture cannot know), so the textures here are the quiet rock FACE only.
  rock_a: warm tan, two feathered patch coats. The minimum.
  rock_b: rock_a plus broad angular PLANES (wobbly Voronoi facets) in three close values, each
          with a thin light band along its upper edge and a soft shade along its lower edge.
  rock_c: rock_b with its patches shifted in colour TEMPERATURE (warm ochre and cool mauve-grey)
          instead of value only.
"""
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(os.environ.get("LAGOON_TEX_OUT", ROOT / "ArtSource" / "lagoon" / "textures"))
KANTO = ROOT / "ArtSource" / "kanto" / "textures"
SHEETS = ROOT / "Logs" / "lagoon-blender"
SIZE = 1024
TILE_M = 4.0            # a boulder is 3 to 6 m, so one tile spans most of a face
PX = SIZE / TILE_M
Y, X = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / PX   # metres, y down the image


def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], dtype=np.float32)


def flat(col):
    return np.broadcast_to(hexcol(col), (SIZE, SIZE, 3)).copy()


def field(scale_m, seed):
    """Periodic smooth random field, features about `scale_m` across, unit variance."""
    white = np.random.default_rng(seed).standard_normal((SIZE, SIZE))
    f = np.fft.fftfreq(SIZE)
    s = scale_m * PX
    g = np.exp(-(f[:, None] ** 2 + f[None, :] ** 2) * s * s * 2)
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * g))
    return ((n - n.mean()) / (n.std() + 1e-9)).astype(np.float32)


def coat(img, tint, scale_m, coverage, seed, feather=0.4):
    """A second coat of paint over part of the surface, with a feathered organic edge."""
    n = field(scale_m, seed) + 0.25 * field(scale_m / 3, seed + 1)
    t = np.quantile(n, 1 - coverage)
    a = np.clip((n - t) / feather + 0.5, 0, 1)
    a = (a * a * (3 - 2 * a))[..., None]
    return img * (1 - a) + (img * tint) * a


def facets(cells, seed, warp_m=0.015, stretch=0.55):
    """Broad rock PLANES: a periodic Voronoi of `cells` sites over the tile, its edges bent by a
    smooth warp so no edge is ruled. Returns the cell id, the distance to the nearest edge (m),
    and +1/-1 for whether the neighbour across that edge lies ABOVE (+1: this pixel is at its
    plane's upper edge, where light catches) or BELOW (-1: the lower edge, in shade).
    ⚠️ Sheet v1 bent the edges 0.12 m and outlined every one: it read as crazy paving, a cousin
    of stone_blocks, not rock. Rock planes are ANGULAR: nearly straight edges (0.015 m warp),
    wider than tall (`stretch` shrinks x distance, so cells run horizontally like weathered
    sheets of granite), told apart by value, with light only on the upper edges."""
    rng = np.random.default_rng(seed)
    sites = rng.uniform(0, TILE_M, (cells, 2)).astype(np.float32)
    wx = X + warp_m * field(0.5, seed + 1)
    wy = Y + warp_m * field(0.5, seed + 2)
    d1 = np.full((SIZE, SIZE), 1e9, np.float32)
    d2 = np.full((SIZE, SIZE), 1e9, np.float32)
    id1 = np.zeros((SIZE, SIZE), np.int32)
    y1 = np.zeros((SIZE, SIZE), np.float32)
    y2 = np.zeros((SIZE, SIZE), np.float32)
    for k, (sx, sy) in enumerate(sites):
        for ox in (-TILE_M, 0, TILE_M):
            for oy in (-TILE_M, 0, TILE_M):
                d = np.hypot((wx - sx - ox) * stretch, wy - sy - oy)
                closer = d < d1
                second = (~closer) & (d < d2)
                d2 = np.where(closer, d1, np.where(second, d, d2))
                y2 = np.where(closer, y1, np.where(second, sy + oy, y2))
                d1 = np.where(closer, d, d1)
                y1 = np.where(closer, sy + oy, y1)
                id1 = np.where(closer, k, id1)
    edge = (d2 - d1) * 0.5
    side = np.where(y2 < y1, 1.0, -1.0).astype(np.float32)   # neighbour above = upper edge
    return id1, edge, side


def band(edge, width_m, soft_m=0.012):
    """1 inside a drawn band of `width_m` along a facet edge, crisp with a hand-soft falloff."""
    return np.clip((width_m - edge) / soft_m + 0.5, 0, 1)


def rock_face(temperature=False, planes=True):
    img = flat("c29f76")                                            # warm tan
    if temperature:
        img = coat(img, np.array([1.05, 1.0, 0.88]), 1.1, 0.35, seed=31)   # warm ochre
        img = coat(img, np.array([0.92, 0.92, 0.97]), 0.9, 0.25, seed=33)  # cool mauve-grey
    else:
        img = coat(img, np.array([0.93, 0.92, 0.90]), 1.1, 0.35, seed=31)
        img = coat(img, np.array([1.05, 1.04, 1.02]), 0.8, 0.22, seed=33)
    height = np.zeros((SIZE, SIZE), np.float32)
    if planes:
        ident, edge, side = facets(8, seed=41)
        values = np.random.default_rng(42).choice(np.array([0.94, 1.0, 1.06], np.float32), 8)
        img = img * values[ident][..., None]
        light = band(edge, 0.03) * (side > 0)                           # upper edges only
        img = img * (1 - light[..., None]) + np.minimum(img * 1.13, 1) * light[..., None]
        height = np.clip(edge / 0.12, 0, 1) ** 0.5                   # each plane a soft plateau
    return img, height


def normal_from_height(h, strength):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.dstack([-gx, gy, np.ones_like(h)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


STRENGTH = {"rock_a": 0.0, "rock_b": 1.2, "rock_c": 1.2, "thatch_a": 1.6, "thatch_b": 1.6, "thatch_c": 1.6,
            "sawali_a": 1.4, "sawali_b": 1.4, "sawali_c": 1.4,
            "bamboo_a": 4.0, "bamboo_b": 4.0, "bamboo_c": 4.0,
            "plank_a": 1.2, "plank_b": 1.2, "plank_c": 1.2, "plank_c_walk": 1.2,
            "tin_a": 1.5, "tin_b": 1.5, "tin_c": 1.5,
            "timber_a": 6.0, "timber_b": 6.0, "timber_c": 6.0}   # owner: normal maps must read; 1.0 was near flat
TILE = {"rock_a": 4.0, "rock_b": 4.0, "rock_c": 4.0, "thatch_a": 2.0, "thatch_b": 2.0, "thatch_c": 2.0,
        "sawali_a": 2.0, "sawali_b": 2.0, "sawali_c": 2.0,
        "bamboo_a": 2.0, "bamboo_b": 2.0, "bamboo_c": 2.0,
        "plank_a": 2.0, "plank_b": 2.0, "plank_c": 2.0,
        "tin_a": 2.0, "tin_b": 2.0, "tin_c": 2.0,
        "timber_a": 2.0, "timber_b": 2.0, "timber_c": 2.0}


# ---------------------------------------------------------------- thatch
# NIPA / COGON THATCH (docs/LAGOON_REWORK_GUIDE.md § 8 step 3), its OWN drawing: no generator
# here or in author_kanto_textures.py is reused. Research (stylized thatch materials on
# 3dtextures.me and ArtStation, polycount's hand-painted thatch advice): thatch reads from
# horizontal COURSES of vertical BUNDLES with ragged pointed TIPS hanging over the course below;
# the bulk of each bundle is bright, darker only where it tucks under the course above and at
# its tip; a soft shadow sits under each row of tips; bundles, not single strands, are the unit.
# Layout: 1 UV unit = 2 m, V UP THE ROOF (image up = up the slope), U along the eave, so the
# house kit's roofs lay the courses horizontal. Courses are drawn from the bottom of the image
# up, each over the one below, exactly as a roof is laid; tips wrap vertically so it tiles.
THATCH_COURSE_M = 0.4


def thatch(base, light, dark, seed, bundle_m=(0.05, 0.12), tip_m=(0.05, 0.16)):
    """Sheet v1 review: dark triangular gaps read as HOLES and the whole was too contrasty for
    the house style (the gaps are now the course's own shaded tone, and the tip shadow is
    softer); and the tile showed a straight SEAM where it repeats vertically, because the
    bottom course's tips belong ON TOP of the next tile's top course (they are drawn last)."""
    px = SIZE / 2.0                                     # 2 m a tile
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / px
    img = np.zeros((SIZE, SIZE, 3), np.float32)
    img[:] = base * 0.72 + dark * 0.28                  # depth between bundles: shaded, not a hole
    height = np.zeros((SIZE, SIZE), np.float32)
    shade = np.zeros((SIZE, SIZE), np.float32)          # shadow cast under the tips above
    rows = int(round(2.0 / THATCH_COURSE_M))
    warp = 0.012 * field(0.3, seed + 9)                 # nothing ruled
    courses = []
    for r in range(rows):
        bundles, x = [], rng.uniform(0, 0.1)
        while x < 2.0 + 0.1:
            w = rng.uniform(*bundle_m)
            bundles.append((x, w, rng.uniform(*tip_m), rng.uniform(-0.25, 0.25), rng.uniform(0.92, 1.08)))
            x += w * rng.uniform(0.7, 0.9)              # bundles overlap a little sideways
        courses.append(bundles)

    def draw(r, oy):
        y0 = r * THATCH_COURSE_M
        y1 = y0 + THATCH_COURSE_M
        cy = yy + warp - oy
        for x, w, tip, lean, value in courses[r]:
            cx = (xx - x - lean * (cy - y0) + 1.0) % 2.0 - 1.0          # wrap in x
            u = cx / w
            inside_x = (u >= 0) & (u <= 1)
            point = 1 - np.abs(2 * u - 1) ** 1.4                        # 1 mid, 0 at the sides
            bottom = y1 + tip * point
            top = max(y0 - 0.06, 0.0) if oy == 0 else y0 - 0.06
            m = inside_x & (cy >= top) & (cy <= bottom)
            if not m.any():
                continue
            t = np.clip((cy - (y0 - 0.06)) / (bottom - (y0 - 0.06) + 1e-6), 0, 1)
            lum = np.interp(t, [0.0, 0.18, 0.35, 0.8, 1.0], [0.72, 0.93, 1.0, 0.98, 0.86])
            col = base * (1 - 0.3 * (1 - lum))[..., None] * value
            col = col + (light - base) * np.clip((lum - 0.96) * 10, 0, 1)[..., None] * 0.5
            img[m] = col[m]
            height[m] = (0.4 + 0.6 * np.sin(np.pi * np.clip(u, 0, 1)) * lum)[m]
            shade[m] = 0.0
            sh = inside_x & (cy > bottom) & (cy < bottom + 0.045)
            shade[sh] = np.maximum(shade[sh], (1 - (cy[sh] - bottom[sh]) / 0.045) * 0.28)

    for r in reversed(range(rows)):                     # bottom course first, each over the one below
        draw(r, 0.0)
    draw(rows - 1, -2.0)                                # the bottom course's tips, over the next tile's top
    img = img * (1 - shade[..., None])
    img = coat(img, np.array([1.04, 1.03, 1.0]), 0.7, 0.3, seed + 3)   # a few soft sun patches
    return img, height


# ---------------------------------------------------------------- sawali
# SAWALI, woven split-bamboo wall panels (docs/LAGOON_REWORK_GUIDE.md § 8 step 3), its OWN
# drawing. Real sawali is split bamboo woven 2-over-2 (twill), often in a herringbone, sometimes
# a plain checker; neighbouring strips show the darker outer SKIN or the paler inner face.
# Stylized: strips ~6 cm (wider than real, so the weave reads from the court), each visible run
# a soft lozenge, brightest mid-run and darker where it dives under the crossing strip; low
# contrast, a hand wobble, per-strip value. 2 m a tile, U horizontal, V vertical.
SAWALI_N = 20                                           # strips per 2 m tile each way: 10 cm.
# Sheet v1 at 6 cm read as fine tweed from a distance (the house style bans fine noise).


def sawali(pattern, skin, inner, gap, seed):
    px = SIZE / 2.0
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / px
    wob = 0.004 * field(0.5, seed + 1)                  # v1 0.008 at 0.25 m: a melting grid
    s_m = 2.0 / SAWALI_N
    fx, fy = (xx + wob) / s_m, (yy - wob) / s_m
    i, j = np.floor(fx).astype(int) % SAWALI_N, np.floor(fy).astype(int) % SAWALI_N
    ux, uy = fx - np.floor(fx), fy - np.floor(fy)       # 0..1 within the cell
    # A twill run covers TWO cells (v1 shaded every cell as its own run): the position along
    # the run is ((i + j) % 2 + u) / 2 for the 2-over-2 weave.
    if pattern == "twill":
        horiz = ((i + j) // 2) % 2 == 0
        k = (i + j) % 2
    elif pattern == "herringbone":
        flip = (i // 6) % 2 == 0                        # the twill turns every 6 strips
        horiz = np.where(flip, ((i + j) // 2) % 2 == 0, ((i - j) // 2) % 2 == 0)
        k = np.where(flip, (i + j) % 2, (i - j) % 2)
    else:                                               # checker: plain over-under, 1-cell runs
        horiz = (i + j) % 2 == 0
        k = None
    if k is None:
        along = np.where(horiz, ux, uy)
    else:
        along = np.where(horiz, (k + ux) / 2, (k + uy) / 2)
    across = np.where(horiz, uy, ux)
    tone_h = rng.uniform(0.95, 1.04, SAWALI_N)[j]      # each horizontal strip its own value
    tone_v = rng.uniform(0.95, 1.04, SAWALI_N)[i]
    colour = np.where(horiz[..., None], inner * tone_h[..., None], skin * tone_v[..., None])
    ends = np.minimum(along, 1 - along)                 # 0 at the cell edge
    run = 0.86 + 0.14 * np.clip(ends / 0.2, 0, 1)       # darker where it dives under
    side = np.clip(np.minimum(across, 1 - across) / 0.12, 0, 1)   # rounded strip edges
    lum = run * (0.78 + 0.22 * side)
    img = colour * lum[..., None]
    edge = np.minimum(across, 1 - across) < 0.05        # the thin gap between strips
    img[edge] = (img[edge] * 0.7 + gap * 0.3)
    height = (side * run).astype(np.float32)
    img = coat(img, np.array([0.95, 0.94, 0.92]), 0.6, 0.25, seed + 3)  # weathering patches
    return img, height


SAWALI_PAINTERS = {
    "sawali_a": lambda: sawali("twill", hexcol("c9a872"), hexcol("d6bd8c"), hexcol("8a7050"), 61),
    "sawali_b": lambda: sawali("herringbone", hexcol("c9a872"), hexcol("d6bd8c"), hexcol("8a7050"), 61),
    "sawali_c": lambda: sawali("checker", hexcol("c9a872"), hexcol("d6bd8c"), hexcol("8a7050"), 61),
}


# ---------------------------------------------------------------- bamboo
# BAMBOO for piles, bracing, railings and ladders (§ 8 step 3), its OWN drawing: a culm with
# raised NODES at irregular spacing (0.3 to 0.5 m) and a few broad soft lengthwise streaks, not
# grain. V runs along the member (the kit's convention), U around it; a pile's whole
# circumference (~0.4 m) fits in one 2 m tile width, so the nodes run straight across the tile.


def bamboo(base, light, dark, seed):
    px = SIZE / 2.0
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / px
    img = flat_rgb(base)
    # Broad soft lengthwise TONE BANDS (sheet v1's fine streaks read as wood grain, which the
    # house style bans): a wide field across U, smeared along V.
    streak = field(0.25, seed + 1)
    streak = np.real(np.fft.ifft2(np.fft.fft2(streak) * np.exp(-(np.fft.fftfreq(SIZE)[:, None] ** 2)
                                                                 * (0.6 * px) ** 2 * 2)))
    streak = (streak - streak.mean()) / (streak.std() + 1e-9)
    img = img * (1 + 0.05 * np.clip(streak, -1.5, 1.5))[..., None]
    height = np.zeros((SIZE, SIZE), np.float32)
    y = rng.uniform(0, 0.2)
    nodes = []
    while y < 2.0:
        nodes.append(y)
        y += rng.uniform(0.3, 0.5)
    for n in nodes:
        d = (yy - n + 1.0) % 2.0 - 1.0                       # signed distance to the node, wrapped
        ring = np.exp(-(d / 0.02) ** 2)                      # the raised ridge (v1 0.012: a pinstripe)
        below = np.exp(-((d - 0.04) / 0.03) ** 2)            # a soft darker band under it
        above = np.exp(-((d + 0.05) / 0.05) ** 2)            # the swelling just above: lighter
        img = img * (1 - 0.18 * below)[..., None] + (light - img) * (0.35 * ring + 0.12 * above)[..., None]
        img = img * (1 - 0.25 * np.exp(-((d - 0.006) / 0.004) ** 2))[..., None]   # the joint line
        height = height + ring + 0.3 * above
    # Weathering: faint and sparse (v1's full-strength coat made dark camouflage blobs).
    img = coat(img, 1 - 0.35 * (1 - dark / np.maximum(base, 1e-3)), 0.35, 0.12, seed + 3, feather=0.6)
    return img, height


def flat_rgb(col):
    return np.broadcast_to(col, (SIZE, SIZE, 3)).astype(np.float32).copy()


BAMBOO_PAINTERS = {
    "bamboo_a": lambda: bamboo(hexcol("cbb06c"), hexcol("e6d49a"), hexcol("a88c52"), 71),
    "bamboo_b": lambda: bamboo(hexcol("b09d7c"), hexcol("d2c3a4"), hexcol("8a7a60"), 71),
    "bamboo_c": lambda: bamboo(hexcol("a9ad62"), hexcol("cfd08c"), hexcol("868a48"), 71),
}


# ---------------------------------------------------------------- planks
# PLANKS for floors, decks, steps, walks and doors (§ 8 step 3), their OWN drawing (the Kanto
# timber is a neutral pole grey for tinting, not boards). Boards run along V, narrow dark seams,
# BUTT JOINTS at staggered lengths, each board its own value, one flat light band along an edge
# (cel light, not a gradient), soft weathering; NO grain (Kanto's detailed wood was rejected as
# "too detailed"). 2 m a tile.


def planks(base, light, seam, seed, board_m=0.18, jitter=0.02, joints=True):
    px = SIZE / 2.0
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / px
    wob = 0.003 * field(0.8, seed + 1)                  # v1 0.006: boards read as melting
    n = int(round(2.0 / board_m))
    bw = 2.0 / n
    fx = (xx + wob) / bw
    col = np.floor(fx).astype(int) % n
    u = fx - np.floor(fx)                                # 0..1 across the board
    img = np.zeros((SIZE, SIZE, 3), np.float32)
    height = np.ones((SIZE, SIZE), np.float32)
    for c in range(n):
        m = col == c
        # Butt joints: 1 to 2 per board per tile, staggered.
        cuts = sorted(rng.uniform(0, 2.0, rng.integers(1, 3)))
        if not joints:
            cuts = cuts[:1]         # one joint kept for the tiling seam, moved to the tile edge below
            cuts[0] = 0.0
        seg = np.searchsorted(np.array(cuts), yy[m])
        values = rng.uniform(0.9, 1.08, len(cuts) + 1)
        values[-1] = values[0]      # the stretch past the last joint IS the first one, wrapped (v1 seam)
        v = values[seg % len(values)]
        img[m] = base * v[:, None]
        for cut in cuts:
            d = np.abs(((yy[m] - cut + 1.0) % 2.0) - 1.0)
            j = d < 0.008
            img[m][j] = seam
            sub = img[m]
            sub[j] = seam
            img[m] = sub
            h = height[m]
            h[j] = 0.2
            height[m] = h
    lightband = (u > 0.08) & (u < 0.18)                  # the cel-lit edge of every board
    img[lightband] = img[lightband] * 0.8 + light * 0.2
    gap = (u < 0.045) | (u > 1 - 0.03)
    img[gap] = img[gap] * 0.35 + seam * 0.65
    height[gap] = 0.0
    img = coat(img, np.array([0.95, 0.94, 0.92]), 0.5, 0.14, seed + 3)
    return img, height


PLANK_PAINTERS = {
    "plank_a": lambda: planks(hexcol("9a6d45"), hexcol("c29a6c"), hexcol("4a3320"), 81),
    "plank_b": lambda: planks(hexcol("998d7a"), hexcol("c2b8a4"), hexcol("4d463c"), 81),
    "plank_c": lambda: planks(hexcol("9a6d45"), hexcol("c29a6c"), hexcol("4a3320"), 81, board_m=0.28),
    # plank_c for WALKWAYS: boards laid across a 1.4 m walk span it in one piece, so no butt
    # joint may land mid-walk (owner, 2026-09-27, marking broken boards: "fix the textures").
    # The only joint sits on the tile edge, which the walk UVs keep outside the walk's width.
    "plank_c_walk": lambda: planks(hexcol("9a6d45"), hexcol("c29a6c"), hexcol("4a3320"), 81, board_m=0.28,
                                   joints=False),
}


# ---------------------------------------------------------------- tin
# CORRUGATED TIN roofs (§ 8 step 3), their OWN drawing: ridges running DOWN the slope (V up the
# roof, as for thatch), each ridge a soft cel band (light crest, darker trough), sheets that
# overlap every ~0.9 m across and every ~2 m down with a thin lap line, a few soft rust or
# weathering patches. Colour rule (§ 2): teal-GREEN or red, never near defence blue #0080e8.
TIN_PITCH_M = 0.076                                     # one corrugation, crest to crest


def tin(paint, rust, seed, rust_amount=0.07):
    px = SIZE / 2.0
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / px
    n = int(round(2.0 / TIN_PITCH_M))
    phase = xx / (2.0 / n) * 2 * np.pi
    wave = np.sin(phase)                                # +1 crest facing the light, -1 trough
    band = np.clip(wave * 1.6, -1, 1)                   # flattened into cel bands, not a gradient
    img = paint[None, None, :] * (1 + 0.10 * band)[..., None]
    height = (wave * 0.5 + 0.5).astype(np.float32)
    # Sheet laps: across every ~0.9 m (2.0/2 sheets) and one down the slope per tile.
    lap_x = np.abs(((xx + 0.02) % 1.0) - 0.5) > 0.495
    lap_y = np.abs(((yy - 0.3) % 2.0) - 1.0) > 0.994
    img[lap_x | lap_y] *= 0.8
    rng = np.random.default_rng(seed)
    # Rust: small, soft, HALF-strength patches (sheet v1: full-strength rust over a fifth of the
    # sheet read as camouflage blotches; the paint must dominate).
    img = coat(img, 1 - 0.5 * (1 - rust / np.maximum(paint, 1e-3)), 0.25, rust_amount, seed + 1, feather=0.8)
    img = coat(img, np.array([1.05, 1.05, 1.03]), 0.6, 0.15, seed + 2)   # sun-faded patches
    return img, height


TIN_PAINTERS = {
    "tin_a": lambda: tin(hexcol("4f8a70"), hexcol("8a5a3a"), 91),                    # teal-green
    "tin_b": lambda: tin(hexcol("a64a36"), hexcol("7a4028"), 91),                    # red oxide
    "tin_c": lambda: tin(hexcol("9aa09c"), hexcol("8a6a4a"), 91, rust_amount=0.14),  # bare, weathered
}


# ---------------------------------------------------------------- timber
# HEWN TIMBER for posts, sills, plates and frames (§ 8 step 3), its OWN drawing: a hand-hewn
# beam face, long soft ADZE FACETS along V (elongated flat patches a shade apart, their ends
# rounded), no seams, no grain; darker and warmer than the planks so frames read against them.


def timber(base, seed, facets=26):
    px = SIZE / 2.0
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / px
    img = flat_rgb(base)
    height = np.zeros((SIZE, SIZE), np.float32)
    for _ in range(facets):
        cx, cy = rng.uniform(0, 2), rng.uniform(0, 2)
        w, l = rng.uniform(0.05, 0.12), rng.uniform(0.25, 0.6)
        v = rng.choice([0.94, 1.05])
        for ox in (-2, 0, 2):
            for oy in (-2, 0, 2):
                d = ((xx - cx - ox) / w) ** 2 + ((yy - cy - oy) / l) ** 4
                a = np.clip((1.0 - d) / 0.25, 0, 1)
                img = img * (1 + (v - 1) * a)[..., None]
                height = height + a * (v - 1)
    img = coat(img, np.array([0.95, 0.94, 0.93]), 0.5, 0.12, seed + 3)
    return img, height


TIMBER_PAINTERS = {
    "timber_a": lambda: timber(hexcol("7c5536"), 101),     # warm dark
    "timber_b": lambda: timber(hexcol("857868"), 101),     # sun-greyed
    "timber_c": lambda: timber(hexcol("6a3f2a"), 101),     # red-brown, oiled
}


THATCH_PAINTERS = {
    "thatch_a": lambda: thatch(hexcol("c9a35e"), hexcol("e3c888"), hexcol("5e4526"), seed=51),
    "thatch_b": lambda: thatch(hexcol("a98f6a"), hexcol("c9b491"), hexcol("4f4130"), seed=51),
    "thatch_c": lambda: thatch(hexcol("c9a35e"), hexcol("e3c888"), hexcol("5e4526"), seed=51,
                               bundle_m=(0.10, 0.20), tip_m=(0.08, 0.2)),
}


def save(name, albedo, height):
    OUT.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    r = np.ptp(height)
    h = (height - height.min()) / r if r > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((albedo * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    Image.fromarray((h * 255).astype(np.uint8)).save(OUT / f"{name}_height.png")
    Image.fromarray((normal_from_height(h, STRENGTH[name]) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
    print("[lagoon-tex]", name)


PAINTERS = {
    "rock_a": lambda: rock_face(planes=False),
    "rock_b": lambda: rock_face(),
    "rock_c": lambda: rock_face(temperature=True),
    **THATCH_PAINTERS,
    **SAWALI_PAINTERS,
    **BAMBOO_PAINTERS,
    **PLANK_PAINTERS,
    **TIN_PAINTERS,
    **TIMBER_PAINTERS,
}


def sheet(version, names, prefix="rock"):
    """The review sheet: approved Kanto swatches first, then each new one, at one tile (4 m for
    rock, 2 m for the Kanto pair) and as a 3 x 3 repeat so any tiling shows."""
    cols = [("brick (approved, 2 m)", KANTO / "brick_albedo.png"),
            ("rock_a (approved, 4 m)", OUT / "rock_a_albedo.png")]
    cols += [(f"{n} ({TILE.get(n, 2.0):g} m)", OUT / f"{n}_albedo.png") for n in names]
    cell, gap, head = 360, 16, 40
    W = gap + len(cols) * (cell + gap)
    H = 2 * (head + cell) + gap * 2
    page = Image.new("RGB", (W, H), (245, 241, 234))
    draw = ImageDraw.Draw(page)
    try:
        font = ImageFont.truetype("arial.ttf", 18)
    except OSError:
        font = ImageFont.load_default()
    for i, (label, path) in enumerate(cols):
        x = gap + i * (cell + gap)
        tile = Image.open(path).convert("RGB")
        draw.text((x, 12), label, fill=(40, 36, 32), font=font)
        page.paste(tile.resize((cell, cell), Image.LANCZOS), (x, head))
        draw.text((x, head + cell + gap + 12), "3 x 3 repeat", fill=(40, 36, 32), font=font)
        rep = Image.new("RGB", (tile.width * 3, tile.height * 3))
        for a in range(3):
            for b in range(3):
                rep.paste(tile, (a * tile.width, b * tile.height))
        page.paste(rep.resize((cell, cell), Image.LANCZOS), (x, 2 * head + cell + gap))
    SHEETS.mkdir(parents=True, exist_ok=True)
    out = SHEETS / f"{prefix}_swatches_v{version}.png"
    page.save(out)
    print("[lagoon-tex] sheet", out)


def main():
    args = sys.argv[1:]
    if "--sheet" in args:
        names = [a for a in args if a in PAINTERS] or list(PAINTERS)
        prefix = args[args.index("--prefix") + 1] if "--prefix" in args else "rock"
        sheet(int(args[args.index("--sheet") + 1]), names, prefix)
        return
    names = args[0].split(",") if args else list(PAINTERS)
    for n in names:
        save(n, *PAINTERS[n]())


if __name__ == "__main__":
    main()
