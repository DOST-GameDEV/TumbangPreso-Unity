"""Paint the Kanto sample map's tileable textures in TUMP's illustrated style.

  py -3 tools/author_kanto_textures.py                 # repaint everything (overwrites!)
  py -3 tools/author_kanto_textures.py --normals-only  # rebuild normals from painted heights

Writes ArtSource/kanto/textures/<name>_albedo.png, _height.png and _normal.png (OpenGL
convention, green up, which both Blender and Unity read). Every texture tiles and covers
TILE_M metres, so the models' world-scale UVs put a brick at a real size everywhere.

THE STYLE, and the two rounds that were rejected getting here.
  1. Ready-made stylized packs: "too detailed and more old-rpg like".
  2. A soft painted set (v6): streaked glass "overcrowded", straight rectangular bricks "too
     realistic and rigid", wood "too detailed", roof "too noisy".
The owner's reference is the game's own key art (a flat-fill, gouache-like street) plus PEAK
and Tiny Talisman, all of which paint surfaces as:
  * FLAT fills. Value changes come from a few LARGE patches with FEATHERED, ORGANIC edges, like a
    second coat of paint that did not quite cover, never from grain or blurred noise.
  * Shapes that are wobbly and rounded, not ruled: bricks are hand-drawn lozenges of uneven
    size, their edges drawn freehand.
  * Light drawn in, cel-style: one flat highlight band on the top of a brick, not a gradient.
  * Low contrast between a thing and its joints, so at street distance a wall reads as ONE
    colour with a hint of texture.
If a change adds fine noise, streaks or more than two or three value steps, it is going the
wrong way.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "ArtSource" / "kanto" / "textures"
SIZE = 1024
TILE_M = 2.0
PX = SIZE / TILE_M
Y, X = np.mgrid[0:SIZE, 0:SIZE] / PX   # metres


def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def smooth(scale_m, seed, stretch=(1.0, 1.0)):
    """Periodic smooth noise with features about `scale_m` across. Tiles exactly."""
    white = np.random.default_rng(seed).standard_normal((SIZE, SIZE))
    fy = np.fft.fftfreq(SIZE)[:, None] * stretch[1]
    fx = np.fft.fftfreq(SIZE)[None, :] * stretch[0]
    s = scale_m * PX
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * s * s * 2)))
    return (n - n.mean()) / (n.std() + 1e-9)


def patches(base, shift, scale_m, coverage, seed, feather=0.35):
    """Organic patches: a second coat of `shift` over part of the surface, with a FEATHERED
    edge. `feather` is the edge width in noise standard deviations; the first version's
    near-zero width read as "too harsh" cut-outs. `coverage` 0..1 is how much they take."""
    n = smooth(scale_m, seed) + 0.25 * smooth(scale_m / 3, seed + 1)
    t = np.quantile(n, 1 - coverage)
    a = np.clip((n - t) / feather + 0.5, 0, 1)
    a = (a * a * (3 - 2 * a))[..., None]
    return base * (1 - a) + (base * shift) * a


def flat(col):
    return np.broadcast_to(hexcol(col), (SIZE, SIZE, 3)).copy()


def organic_bricks(rows, cols, mortar_m, wobble_m, round_m, seed):
    """Hand-drawn bricks: a running-bond grid, domain-warped so no edge is ruled, each brick a
    rounded lozenge of slightly different size. Returns id, a 0..1 plateau height, and v (0 at
    the brick's top edge, 1 at its bottom) for the drawn highlight band."""
    rng = np.random.default_rng(seed)
    wx = X + wobble_m * smooth(0.18, seed + 1)
    wy = Y + wobble_m * 0.6 * smooth(0.22, seed + 2)
    rh, cw = TILE_M / rows, TILE_M / cols
    row = np.floor(wy / rh).astype(int) % rows
    shift = (row % 2) * 0.5 * cw
    col = np.floor(((wx + shift) % TILE_M) / cw).astype(int) % cols
    ident = row * 100 + col
    # Per-brick size jitter: each shrinks by its own amount so gaps vary along a course.
    shrink = rng.uniform(0, mortar_m * 0.8, rows * 100 + cols)[ident]
    ly = (wy % TILE_M) - row * rh
    lx = ((wx + shift) % TILE_M) - col * cw
    hx, hy = cw / 2 - mortar_m / 2 - shrink, rh / 2 - mortar_m / 2 - shrink * 0.5
    dx, dy = np.abs(lx - cw / 2) - (hx - round_m), np.abs(ly - rh / 2) - (hy - round_m)
    sdf = np.hypot(np.maximum(dx, 0), np.maximum(dy, 0)) + np.minimum(np.maximum(dx, dy), 0) - round_m
    inside = np.clip(-sdf * PX / 1.5, 0, 1)            # crisp, drawn edge
    height = np.clip(-sdf / (round_m * 1.2), 0, 1)
    height = np.sqrt(height)                            # a pillowy plateau, not a bevel ramp
    v = (ly - (rh / 2 - hy)) / (2 * hy)
    return ident, inside, height, v


def per_brick(ident, values, seed):
    rng = np.random.default_rng(seed)
    table = rng.choice(values, size=ident.max() + 1)
    return table[ident]


def normal_from_height(h, strength):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.dstack([-gx, gy, np.ones_like(h)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


MORTAR = "9c4636"   # the owner kept the brick-toned grout after comparing three greys

STRENGTH = {"brick": 3.0, "stone_blocks": 2.5, "stone": 0.0, "roof": 0.0, "glass": 0.0, "wood": 2.0, "paint": 0.0,
            "asphalt": 0.0, "brick_brown": 3.0, "panel": 1.5, "paving": 2.0, "court": 1.6, "grass": 0.0, "plaster": 0.0,
            "bark": 2.5, "metal": 0.0, "timber": 0.0, "panelg": 1.5, "tiles": 3.0}


def save(name, albedo, height):
    OUT.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    rng_h = np.ptp(height)
    h = (height - height.min()) / rng_h if rng_h > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((albedo * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    Image.fromarray((h * 255).astype(np.uint8)).save(OUT / f"{name}_height.png")
    Image.fromarray((normal_from_height(h, STRENGTH[name]) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
    print("[kanto-tex]", name)


def brick():
    # 4 bricks a course, 12 courses per 2 m: bigger and fewer than real brick, so a wall reads
    # as a painted wall with bricks drawn on it rather than as masonry.
    ident, inside, h, v = organic_bricks(12, 4, mortar_m=0.035, wobble_m=0.018, round_m=0.035, seed=3)
    face = hexcol("b0503c") * per_brick(ident, np.array([0.94, 1.0, 1.0, 1.04, 0.97]), 4)[..., None]
    # The drawn highlight: a flat lighter band along the top of each brick, hard-edged.
    band = (v < 0.28)[..., None]
    face = np.where(band, face * 1.1, face)
    # Grout: after comparing three warm greys the owner kept this brick-toned one (MORTAR).
    import os
    mortar = flat(os.environ.get("KANTO_MORTAR", MORTAR))
    img = mortar * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([1.07, 1.05, 1.02]), 0.55, 0.22, seed=5)
    save("brick", img, h)


def brick_brown():
    # A second brick for variety: brown, slightly smaller courses, darker joints. The owner:
    # "you're reusing the same materials/textures making it all look the same".
    ident, inside, h, v = organic_bricks(14, 4, mortar_m=0.03, wobble_m=0.015, round_m=0.03, seed=7)
    face = hexcol("8a5236") * per_brick(ident, np.array([0.93, 1.0, 1.0, 1.05, 0.96]), 8)[..., None]
    face = np.where((v < 0.28)[..., None], face * 1.1, face)
    img = flat("6a3e2a") * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([1.06, 1.05, 1.03]), 0.55, 0.2, seed=9)
    save("brick_brown", img, h)


def panel():
    # Concrete panels, 1 x 1 m, joints barely darker: the glass mid-rise and panel blocks.
    ident, inside, h, v = organic_bricks(2, 2, mortar_m=0.018, wobble_m=0.004, round_m=0.02, seed=121)
    face = hexcol("e6e1d8") * per_brick(ident, np.array([0.97, 1.0, 1.02]), 122)[..., None]
    img = flat("c4bdb2") * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([0.96, 0.96, 0.95]), 0.7, 0.2, seed=123, feather=0.5)
    save("panel", img, h)


def stone_blocks():
    ident, inside, h, v = organic_bricks(4, 2, mortar_m=0.03, wobble_m=0.02, round_m=0.06, seed=11)
    face = hexcol("ead9b6") * per_brick(ident, np.array([0.97, 1.0, 1.02]), 12)[..., None]
    img = flat("cdb892") * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([0.95, 0.94, 0.92]), 0.6, 0.2, seed=13)
    save("stone_blocks", img, h)


def stone():
    img = patches(flat("ecdfc2"), np.array([0.955, 0.945, 0.93]), 0.5, 0.25, seed=21)
    save("stone", img, np.zeros((SIZE, SIZE)))


def roof():
    # Softer and lower contrast than v7: one broad wash, one faint lighter coat.
    img = patches(flat("67625f"), np.array([0.94, 0.94, 0.95]), 0.9, 0.3, seed=31, feather=0.6)
    img = patches(img, np.array([1.04, 1.04, 1.03]), 0.5, 0.12, seed=32, feather=0.6)
    save("roof", img, np.zeros((SIZE, SIZE)))


def glass():
    # One gentle vertical gradient per storey-ish tile and nothing else: the owner found the
    # painted glints crowded every window.
    g = np.clip(1 - Y / TILE_M, 0, 1)[..., None]
    img = hexcol("3f8b86") * (1 - g * 0.45) + hexcol("7cc2b8") * g * 0.45
    save("glass", img, np.zeros((SIZE, SIZE)))


def wood():
    # Planks as flat colour with ONE drawn line between them and the odd patch. No grain.
    ident, inside, h, v = organic_bricks(8, 1, mortar_m=0.02, wobble_m=0.006, round_m=0.012, seed=51)
    face = hexcol("8a5a36") * per_brick(ident, np.array([0.95, 1.0, 1.05]), 52)[..., None]
    img = flat("5e3a22") * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([1.08, 1.06, 1.03]), 0.4, 0.15, seed=53)
    save("wood", img, h)


def paint():
    # Neutral (about 0.92 grey) so it can be MULTIPLIED by each material's own colour.
    img = patches(flat("ebebeb"), np.array([0.95, 0.95, 0.95]), 0.6, 0.25, seed=61)
    save("paint", img, np.zeros((SIZE, SIZE)))


def asphalt():
    # Roads: a flat blue-grey with two broad, feathered coats, so the carriageway reads as one
    # surface with a little life in it. No aggregate grain.
    img = patches(flat("4a4850"), np.array([0.93, 0.93, 0.95]), 1.0, 0.35, seed=71, feather=0.6)
    img = patches(img, np.array([1.05, 1.05, 1.04]), 0.45, 0.12, seed=72, feather=0.6)
    save("asphalt", img, np.zeros((SIZE, SIZE)))


def paving():
    # Sidewalks: big 1 m slabs, soft joints, the same hand-drawn wobble as the brick.
    ident, inside, h, v = organic_bricks(2, 2, mortar_m=0.03, wobble_m=0.01, round_m=0.05, seed=81)
    face = hexcol("c9bfae") * per_brick(ident, np.array([0.96, 1.0, 1.03]), 82)[..., None]
    img = flat("a89e8e") * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([0.96, 0.95, 0.94]), 0.7, 0.2, seed=83, feather=0.5)
    save("paving", img, h)


def court():
    # The park's play court: lighter, warmer, smaller slabs than the sidewalk, so the ground a
    # match is played on reads as its own place from a first-person view.
    ident, inside, h, v = organic_bricks(4, 4, mortar_m=0.02, wobble_m=0.008, round_m=0.04, seed=91)
    face = hexcol("e3d6bd") * per_brick(ident, np.array([0.97, 1.0, 1.02]), 92)[..., None]
    img = flat("c7b89c") * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([0.97, 0.96, 0.95]), 0.6, 0.2, seed=93, feather=0.5)
    save("court", img, h)


def grass():
    # Lawns: flat green, a darker and a lighter feathered coat. Blades are the foliage's job.
    img = patches(flat("6fa23a"), np.array([0.88, 0.92, 0.85]), 0.8, 0.3, seed=101, feather=0.55)
    img = patches(img, np.array([1.08, 1.06, 0.95]), 0.4, 0.15, seed=102, feather=0.55)
    save("grass", img, np.zeros((SIZE, SIZE)))


def plaster():
    # Painted render for the walk-ups and the deco corner: neutral, MULTIPLIED by each
    # building's own paint colour, like "paint" but with larger, softer second coats.
    img = patches(flat("f0f0f0"), np.array([0.94, 0.94, 0.94]), 0.9, 0.3, seed=111, feather=0.6)
    img = patches(img, np.array([1.03, 1.03, 1.03]), 0.5, 0.12, seed=112, feather=0.6)
    save("plaster", img, np.zeros((SIZE, SIZE)))


def panelg():
    # The panel texture in NEUTRAL grey, so panel blocks can be painted sand, terracotta or
    # sage. The owner on review v3: "a lot of them are primarily just white".
    ident, inside, h, v = organic_bricks(2, 2, mortar_m=0.018, wobble_m=0.004, round_m=0.02, seed=121)
    face = hexcol("eeeeee") * per_brick(ident, np.array([0.96, 1.0, 1.03]), 122)[..., None]
    img = flat("c6c6c6") * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([0.95, 0.95, 0.95]), 0.7, 0.2, seed=123, feather=0.5)
    save("panelg", img, h)


def tiles():
    # Clay roof tiles for the pitched roofs (Brainchild): overlapping rounded courses with a
    # drawn lighter lip on each, tall joints soft. NEUTRAL: tinted red-brown or green.
    ident, inside, h, v = organic_bricks(10, 7, mortar_m=0.04, wobble_m=0.012, round_m=0.07, seed=161)
    face = hexcol("e4e4e4") * per_brick(ident, np.array([0.92, 0.97, 1.0, 1.05]), 162)[..., None]
    face = np.where((v > 0.72)[..., None], face * 1.12, face)
    img = flat("8a8a8a") * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([0.92, 0.92, 0.92]), 0.6, 0.2, seed=163)
    save("tiles", img, h)


def bark():
    # Tree trunks. The owner: "a lot of models are untextured. like the tree bodies". Tall
    # narrow hand-drawn plates (the brick lozenge turned on end), soft dark joints, one broad
    # lighter coat. NEUTRAL: tinted by each trunk's own colour.
    ident, inside, h, v = organic_bricks(3, 11, mortar_m=0.035, wobble_m=0.03, round_m=0.05, seed=131)
    face = hexcol("d8d8d8") * per_brick(ident, np.array([0.9, 0.97, 1.0, 1.05]), 132)[..., None]
    img = flat("8c8c8c") * (1 - inside[..., None]) + face * inside[..., None]
    img = patches(img, np.array([1.06, 1.06, 1.06]), 0.35, 0.2, seed=133)
    save("bark", img, h)


def metal():
    # Painted street metal: poles, railings, signal housings, bench frames. Seen on 10 cm
    # posts, so the marks are SMALL: a few feathered chips of lighter paint and darker
    # scuffs a few centimetres across, on a flat coat. NEUTRAL: tinted per material.
    img = patches(flat("e2e2e2"), np.array([0.86, 0.86, 0.86]), 0.09, 0.14, seed=141, feather=0.45)
    img = patches(img, np.array([1.1, 1.1, 1.1]), 0.04, 0.06, seed=142, feather=0.35)
    img = patches(img, np.array([0.93, 0.93, 0.93]), 0.5, 0.25, seed=143, feather=0.6)
    save("metal", img, np.zeros((SIZE, SIZE)))


def timber():
    # Timber power poles: long soft vertical bands, no grain lines. NEUTRAL.
    n = smooth(0.12, 151, stretch=(1.0, 0.08))
    a = np.clip(n * 0.6 + 0.5, 0, 1)[..., None]
    img = flat("c8c8c8") * (1 - a * 0.14) + flat("e8e8e8") * a * 0.14
    img = patches(img, np.array([0.9, 0.9, 0.9]), 0.4, 0.15, seed=152)
    save("timber", img, np.zeros((SIZE, SIZE)))


def leaf(size=256):
    """ONE drawn leaf with a transparent background, for foliage cards. Greyscale, so each
    foliage material tints it (light leaves on top, dark underneath).

    Tiny Talisman's trees are clumps of individually shaped leaves, and the owner wants that
    to be the pattern for every plant: "more transparent/shaped leaves ... instead of just 1
    plain model".

    SOFTLY PAINTED, NOT OUTLINED. The first drawing had a dark drawn rim and a hard midrib and
    was rejected as "sharply outlined and not softly painted like the reference". The
    reference's leaves are a rounded shape whose value rises smoothly from the stem to the
    tip, with a soft light patch where the sun catches them and only a whisper of a midrib.
    The silhouette stays crisp (it is a cut-out); everything inside it is soft."""
    v, u = np.mgrid[0:size, 0:size] / (size - 1)     # u across (0..1), v along (0 base .. 1 tip)
    v = 1 - v
    x = (u - 0.5) * 2
    # A rounder leaf than before: widest a little below the middle, blunt-pointed tip.
    half = 0.95 * np.sin(np.pi * np.clip(v, 0, 1) ** 0.85) ** 0.6 * (1 - 0.25 * v)
    d = half - np.abs(x)
    alpha = np.clip(d * size / 2.5, 0, 1)
    # (A flat one-tone leaf in a stepped clump gradient was tried and reverted by the owner.)
    tone = 0.74 + 0.26 * np.clip(v, 0, 1) ** 0.8                     # stem dark to tip light
    hl = np.exp(-(((x + 0.25) / 0.35) ** 2 + ((v - 0.62) / 0.22) ** 2))
    tone = tone + 0.12 * hl                                           # soft sunlit patch
    tone = tone + 0.04 * np.exp(-(x / 0.05) ** 2) * (v > 0.12)        # faint midrib
    tone = tone - 0.06 * np.clip(1 - d / 0.25, 0, 1) * (v < 0.5)      # gentle darkening to the base edge
    g = np.clip(tone * 0.92, 0, 1)
    rgba = np.dstack([g, g, g, alpha])
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").save(OUT / "leaf_albedo.png")
    print("[kanto-tex] leaf")


def normals_only():
    """Rebuild every *_normal.png from its *_height.png, leaving the albedo alone.

    THE PHOTOSHOP LOOP: paint over <name>_albedo.png, and paint depth into <name>_height.png
    in greyscale (white raised, black recessed; soft brushes give soft bevels). Run this and
    the normal map follows. Nothing painted by hand is overwritten."""
    for name, strength in STRENGTH.items():
        path = OUT / f"{name}_height.png"
        if not path.exists():
            continue
        h = np.asarray(Image.open(path).convert("L"), dtype=float) / 255
        Image.fromarray((normal_from_height(h, strength) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
        print("[kanto-tex] normals from painted height:", name)


if __name__ == "__main__":
    ALL = (brick, brick_brown, panel, stone_blocks, stone, roof, glass, wood, paint, asphalt, paving, court, grass,
           plaster, panelg, tiles, bark, metal, timber, leaf)
    if "--normals-only" in sys.argv:
        normals_only()
    elif "--only" in sys.argv:
        # Paint just the named textures, so hand-painted ones are never overwritten:
        #   py -3 tools/author_kanto_textures.py --only bark,metal,timber
        wanted = sys.argv[sys.argv.index("--only") + 1].split(",")
        for f in ALL:
            if f.__name__ in wanted:
                f()
    else:
        for f in ALL:
            f()
