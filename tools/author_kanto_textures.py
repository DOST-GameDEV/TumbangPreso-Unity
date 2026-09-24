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
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
# KANTO_TEX_OUT redirects output, so a swatch can be reviewed before it replaces a texture.
OUT = Path(os.environ.get("KANTO_TEX_OUT", ROOT / "ArtSource" / "kanto" / "textures"))
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
            "bark": 1.2, "metal": 0.0, "timber": 0.0, "panelg": 1.5, "tiles": 1.5,
            "tiles_clay": 1.2, "tiles_slate": 1.0, "tiles_teal": 1.2}


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
    # Clay roof tiles for the pitched roofs (Brainchild). DRAWN AS COURSES, NOT PEBBLES.
    # The first version ran the brick lozenge generator at 10 x 7 and the owner called it
    # "harsh garbage ... did you just repurpose the brick texture?". It had: a blob per tile,
    # a dark joint all round every blob, and a contrast of about 0.6. A painted roof reads as
    # ONE colour with ROWS drawn on it, so here:
    #   * each course is a flat band, 0.25 m tall, running along the ridge (texture X);
    #   * its lower edge is a soft scallop (one arc per 0.32 m tile), staggered per course;
    #   * the lip just above that edge is a touch lighter, and a thin feathered shadow falls
    #     just below it onto the next course. Nothing else: no joint outlines, no per-tile
    #     colour, total value range about 0.82 to 1.05.
    course, tile_w = 0.25, 0.32
    wob = 0.012 * smooth(0.3, 171)
    yy = Y + wob
    row = np.floor(yy / course)
    fy = yy / course - row                       # 0 at the top of a course, 1 at its lower edge
    shift = (row % 2) * 0.5 * tile_w
    fx = ((X + shift) % tile_w) / tile_w         # 0..1 across one tile
    edge = 0.8 + 0.18 * np.sqrt(np.clip(1 - (2 * fx - 1) ** 2, 0, 1))   # the scallop: lowest mid-tile
    below = np.clip((fy - edge) / 0.05 + 0.5, 0, 1)      # past the scallop: the next course's top
    lip = np.clip(1 - np.abs(fy - (edge - 0.1)) / 0.12, 0, 1)
    shade = np.clip(1 - (fy - edge + 0.0) / 0.1, 0, 1) * below          # shadow under the lip
    val = 1.0 + 0.05 * lip * (1 - below) - 0.16 * shade
    split = np.clip(1 - np.minimum(fx, 1 - fx) / 0.035, 0, 1) * (fy < edge) * 0.05
    val = val - split
    img = flat("dedede") * val[..., None]
    img = patches(img, np.array([0.95, 0.95, 0.95]), 0.8, 0.25, seed=173, feather=0.6)
    height = np.clip(fy / np.maximum(edge, 1e-3), 0, 1) * (1 - below)
    save("tiles", img, height)


def _courses(course_m, tile_w, wobble_m, seed):
    """Hand-drawn tile COURSES: rows running along texture X, domain-warped like the brick so
    no line is ruled. Returns the course index, fy (0 at a course's top, 1 at its lower edge),
    fx (0..1 across one tile, staggered by half a tile per course) and a per-tile id."""
    wx = X + wobble_m * smooth(0.25, seed + 1)
    wy = Y + wobble_m * 0.7 * smooth(0.3, seed + 2)
    rows = int(round(TILE_M / course_m))
    cols = int(round(TILE_M / tile_w))
    rh, cw = TILE_M / rows, TILE_M / cols
    row = np.floor(wy / rh).astype(int) % rows
    fy = (wy % TILE_M) / rh - np.floor((wy % TILE_M) / rh)
    shift = (row % 2) * 0.5 * cw
    col = np.floor(((wx + shift) % TILE_M) / cw).astype(int) % cols
    fx = (((wx + shift) % TILE_M) / cw) % 1.0
    return row, fy, fx, row * 100 + col


def tiles_clay():
    # RED CLAY ROOF TILES, drawn as overlapping COURSES (owner: "overlapping tile COURSES in
    # rows, each course with a soft darker shadow band under its lower edge, courses slightly
    # wobbly and hand-drawn like the brick ... far fewer, bigger tiles").
    #   * 5 courses per 2 m, 4 tiles across: big and few, so the repeat is not the read.
    #   * a course is ONE flat colour; its lower edge is a gentle round tile end;
    #   * the drawn highlight: a flat lighter band along the lip, like the brick's top band;
    #   * a soft, feathered shadow band falls from that edge onto the course below;
    #   * joints between tiles in a course: a faint soft line, barely darker.
    # Contrast is the brick's: every mark sits within about 0.88..1.08 of the base.
    row, fy, fx, ident = _courses(0.4, 0.5, 0.03, 181)
    base = hexcol("a9503a") * per_brick(ident, np.array([0.97, 1.0, 1.0, 1.03]), 182)[..., None]
    edge = 0.84 + 0.12 * np.sqrt(np.clip(1 - (2 * fx - 1) ** 2, 0, 1))      # round tile ends
    past = np.clip((fy - edge) / 0.025 + 0.5, 0, 1)                         # below this course's end
    lip = ((fy > edge - 0.14) & (fy < edge)).astype(float)
    shadow = np.clip(1 - np.abs(fy - 0.08) / 0.12, 0, 1) * (1 - past)       # under the course above
    joint = np.clip(1 - np.minimum(fx, 1 - fx) / 0.03, 0, 1) * (fy < edge)
    val = 1 + 0.07 * lip * (1 - past) - 0.12 * shadow - 0.06 * joint - 0.1 * past
    img = base * val[..., None]
    img = patches(img, np.array([1.04, 1.03, 1.02]), 0.7, 0.2, seed=183, feather=0.6)
    save("tiles_clay", img, np.clip(fy, 0, 1) * (1 - past))


def tiles_slate():
    # GREY SLATE: a different DRAWING, not the clay recoloured. Slates are thin flat plates, so:
    # narrower courses (6 per 2 m), square-cut ends with softened corners and a slight random
    # length per slate (a ragged, hand-laid line rather than a scallop), a thin highlight on
    # the cut edge, and a narrow soft shadow. Joints between slates are a hairline, faint.
    row, fy, fx, ident = _courses(0.333, 0.36, 0.02, 191)
    rng = np.random.default_rng(192)
    ends = rng.uniform(0.9, 0.99, ident.max() + 1)[ident]
    corner = np.clip((np.abs(2 * fx - 1) - 0.82) / 0.18, 0, 1) ** 2 * 0.06
    edge = ends - corner
    past = np.clip((fy - edge) / 0.02 + 0.5, 0, 1)
    lip = ((fy > edge - 0.07) & (fy < edge)).astype(float)
    shadow = np.clip(1 - np.abs(fy - 0.05) / 0.09, 0, 1) * (1 - past)
    joint = np.clip(1 - np.minimum(fx, 1 - fx) / 0.018, 0, 1) * (fy < edge)
    base = hexcol("6c6a6e") * per_brick(ident, np.array([0.96, 1.0, 1.0, 1.04]), 193)[..., None]
    val = 1 + 0.08 * lip * (1 - past) - 0.1 * shadow - 0.05 * joint - 0.08 * past
    img = base * val[..., None]
    img = patches(img, np.array([1.05, 1.05, 1.06]), 0.8, 0.2, seed=194, feather=0.6)
    save("tiles_slate", img, np.clip(fy, 0, 1) * (1 - past))


def tiles_teal():
    # GREEN-TEAL GLAZED BARREL TILES (owner: "maybe also a green/teal roof tile set too").
    # Its own drawing, not the clay recoloured: rounded BARRELS running down the slope in
    # vertical rows (texture Y), each with a soft lit crown and a soft shaded trough beside it,
    # broken every 45 cm by a course end with a soft shadow band under it. Glaze reads as a
    # gentle sheen on the crown, never a hard highlight. Contrast stays at the brick's.
    row, fy, fx, ident = _courses(0.5, 0.4, 0.012, 211)
    crown = np.cos((fx - 0.5) * np.pi) ** 2                     # 1 on the barrel's crown, 0 in the trough
    base = hexcol("2f7466") * per_brick(ident, np.array([0.97, 1.0, 1.0, 1.03]), 212)[..., None]
    shadow = np.clip(1 - np.abs(fy - 0.07) / 0.1, 0, 1)         # under the course above
    lip = np.clip((fy - 0.86) / 0.1, 0, 1)                     # the rounded end of each tile
    val = 0.93 + 0.08 * crown - 0.1 * shadow + 0.04 * lip * crown
    img = base * val[..., None]
    img = patches(img, np.array([1.04, 1.04, 1.03]), 0.8, 0.2, seed=213, feather=0.6)
    save("tiles_teal", img, crown * 0.8 + fy * 0.2)


def bark():
    # BARK, second attempt. Owner: "flat brown with two or three long, soft, feathered
    # vertical strokes, the same simplicity as the soft painted leaves. No grain." So: one flat
    # brown and three long strokes across the 2 m tile (1.5 to 1.8 m long, 10 to 14 cm wide,
    # tapered at both ends, a slow gentle lean), two a touch darker and one a touch lighter.
    # Nothing else. The strokes wrap in x and y, so a trunk's seam and the tile edge are
    # invisible.
    rng = np.random.default_rng(201)
    img = flat("7a5238")
    for k, (x0, shift) in enumerate(((0.3, 0.9), (0.95, 1.06), (1.55, 0.92))):
        y0 = rng.uniform(0, TILE_M)
        length, width = rng.uniform(1.5, 1.8), rng.uniform(0.1, 0.14)
        dy = ((Y - y0 + TILE_M / 2) % TILE_M) - TILE_M / 2           # periodic, centred on the stroke
        t = np.clip(1 - np.abs(dy) / (length / 2), 0, 1)             # 1 mid-stroke, 0 at its tips
        cx = x0 + 0.04 * np.sin(Y / TILE_M * 2 * np.pi + k * 2.1)     # one slow sway per tile
        dx = ((X - cx + TILE_M / 2) % TILE_M) - TILE_M / 2
        w = width * np.sin(np.pi * t / 2)                             # tapered ends
        a = np.clip(1 - np.abs(dx) / np.maximum(w, 1e-4), 0, 1) * (t > 0)
        a = a * a * (3 - 2 * a)                                       # feathered edges
        img = img * (1 - a[..., None]) + img * shift * a[..., None]
    save("bark", img, np.zeros((SIZE, SIZE)))


def metal():
    # Painted street metal, second attempt. The first had dark scuff blotches that read as
    # camouflage on signal housings, bins and poles (owner, review v8). PEAK paints props as a
    # FLAT coat, so: one flat coat, one very broad soft lighter patch, and a sprinkle of a
    # few faint, soft, lighter chips a few centimetres across. Nothing darker than 0.97.
    # NEUTRAL: tinted per material.
    img = patches(flat("e4e4e4"), np.array([1.035, 1.035, 1.035]), 0.7, 0.25, seed=141, feather=0.7)
    img = patches(img, np.array([1.05, 1.05, 1.05]), 0.04, 0.03, seed=142, feather=0.6)
    img = patches(img, np.array([0.97, 0.97, 0.97]), 0.9, 0.2, seed=143, feather=0.8)
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
           plaster, panelg, tiles, tiles_clay, tiles_slate, tiles_teal, bark, metal, timber, leaf)
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
