"""Lagoon Court CARGO PROP KIT: fish crates, barrels and water drums, models AND their own textures.

  py -3 tools/lagoon_prop_cargo.py --paint                 # paint every pg_ texture
  py -3 tools/lagoon_prop_cargo.py --paint pg_stave        # paint the named ones
  py -3 tools/lagoon_prop_cargo.py --sheet N               # swatch sheet vN
  py -3 tools/lagoon_prop_cargo.py --compare A B           # lineup vA (before) over vB (after)
  blender -b --python tools/lagoon_prop_cargo.py -- --preview N

With --preview N the Blender run renders Logs/lagoon-blender/pg_{lineup,close,crate,barrel,drum}_vN.png
(every variant of every kind on warm sand beside a 1.6 m scale cylinder). It paints any missing pg_
texture first through `py -3`, since Blender's Python has no PIL. An existing render is never
overwritten: bump N.

WHY THIS KIT EXISTS (docs/LAGOON_REWORK_GUIDE.md § 7a item 1). The reference (Papaioanou, "Stylized
Fishing Village", ArtStation GvJv5a) piles crates and barrels on every pier and at every door; the
cove had boats and laundry only. These are the Filipino versions: a fish crate (the banyera the
catch is landed in, some still full), a barrel bound with abaca rope or bamboo instead of iron, and
the water drum every house without a tap keeps by the door, with its tabo (the long-handled dipper).

⚠️ FEW BIG SHAPES, AND ORGANIC ONES (owner, 2026-09-27, on the first pass of the prop kits: "i dont
like how details some of the props are. again we're going for a stylized semi-cartoony environment
style", then "you should really experiment more with being organic in how you shape things ... the
reference isnt just a straight rectangular prism"). v1 and v2 had three thin slats a side, a hand
cleat, twelve grooved staves, four hoops with lashings and a crate of individually modelled fish,
every member a straight box; Logs/lagoon-blender/pg_lineup_before_after_v6.png shows them over the
rework. Now:
  * a crate is two THICK boards a side on solid end boards, every board tapered toward its ends,
    bowed a few mm and a little uneven in size (board());
  * a barrel is a fat rounded body, a little lumpy round its girth and leaning, with TWO thick bands
    set slightly off level; its staves are PAINTED (pg_stave), not grooved;
  * the catch is one soft lumpy heap wearing a painted drawing of fish (pg_catch);
  * the drum is a moulded body, a lid and a tabo, the tub a lumpy staved body and a round lid.
Bevels are big so every edge reads soft; detail lives in the textures, at low contrast.

  import lagoon_prop_cargo as PG
  col = PG.build_prop(kind, seed)       # a Collection, not linked; one root empty named by kind
  report = PG.check_prop(col)           # tris, nonmanifold, coplanar, floating, bbox

KINDS, every one origin at the ground contact centre, +Y its front, feet sunk 1 to 3 cm:

  * "crate"       seed 1 open with its catch, 2 lidded, 3 a stack of two, 4 a stack of three, 5 an
                  open stack of two (catch on top); then round again with new sizes. About
                  0.74 x 0.5 m, 0.34 m a crate.
  * "barrel"      seed 1 upright with rope bands, 2 upright with bamboo bands, 3 on its side on two
                  chocks with rope, 4 on its side with bamboo. About 0.88 m tall, 0.41 m radius at
                  the belly.
  * "water_drum"  seed 1 plastic drum lidded with the tabo on the lid, 2 plastic drum open with the
                  tabo floating, 3 wooden tub lidded, 4 plastic lidded (another colour), 5 wooden tub
                  open with the tabo floating. Drum about 0.9 m tall; tub about 0.55 m.

THE ROOT EMPTY carries prop_kind, prop_seed, prop_mount ("ground"), prop_variant, prop_box (the local
box that must stay clear: xmin, ymin, zmin, xmax, ymax, zmax, from 5 cm over the ground).

MATERIALS. Shared by NAME with the house, boat and village kits: timber (timber_a), plank (plank_c,
here on the barrel heads and the tub lid, laid flat so its drawn joints are the head's boards) and
bamboo (bamboo_a); and the beach kit's rope, pb_rope (material of the same name). When the file
already has one it is used as it is, otherwise it is built with
render_lagoon_texture_preview.uv_material around that texture. Surfaces nothing covered get their
own drawing, painted here into ArtSource/lagoon/textures/pg_*_{albedo,height,normal}.png:

  pg_slat     crate boards: one sun-bleached tan with two big soft patches, low contrast. PALER than
              timber_a so a crate reads against the dark house timbers and its own dark end boards.
              Multiplied by "prop_tint" (a fresh, a grey and a warm crate).
  pg_stave    barrel and tub sides: broad staves (8 to a 2 m tile, 25 cm each) with soft darker
              joints and a value step between neighbours, no cross joints (plank_c's butt joints
              would cut a stave in two). U is fitted so a whole number of staves goes round.
  pg_catch    the catch in a crate: big simple silver fish shapes, overlapping, heads and tails
              every way, one dark eye each, all within a narrow silver-grey range.
  pg_plastic  NEUTRAL near-white moulded plastic with two soft sun-faded patches, multiplied by the
              "prop_tint" colour: maroon, leaf green, mustard, cream (the drums) and the tabo's.
  pg_water    (material only) a flat deep green; the water in a drum or tub.

UVs are "UVMap", WORLD SCALE, 1 unit = 2 m (the house and boat kits' rule), V along every board and
up every barrel and drum, the catch heap and the heads projected flat from above.

"prop_tint" (pg_slat, pg_plastic) is a CORNER colour attribute stored as sRGB bytes (FBX vertex
colour); Unity multiplies the albedo by it, as trim_tint works on the boats.

⚠️ ROLE HUES (Art_Direction.md § 1): nothing near offence orange #f87020 or defence blue #0080e8.
The real Filipino water drum is almost always BLUE plastic; here it is maroon, leaf green, mustard
or cream, or wood. The tabo is the same four colours. The water is green, not turquoise.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2). Every meeting is a penetration and
every pair of parallel faces that could meet is held at least 6 mm apart (the check flags 4 mm).
Side boards stand 1.5 cm proud of the end boards and bow OUTWARD only; the lid sits 8 mm into the
end boards and 12 mm clear of the side boards, crowned UP only; a stacked crate sits 2.4 cm down
into the one below and turns at least 4 degrees (at under 2.5 its board ends and the lower end
boards came out parallel and a few mm apart). The organic bends are therefore always in the
direction that opens a clearance, never the one that closes it. `check_prop` proves it per kind.
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

KINDS = ("crate", "barrel", "water_drum")
VARIANTS = {
    "crate": ("open", "lidded", "stack2", "stack3", "stack2_open"),
    "barrel": ("rope", "bamboo", "side_rope", "side_bamboo"),
    "water_drum": ("drum_lid", "drum_open", "tub_lid", "drum_lid", "tub_open"),
}
UV_METRES = 2.0
STAVES_PER_TILE = 8                 # pg_stave: 8 staves across its 2 m tile, 25 cm each

# The per-piece colours multiplied into pg_plastic and pg_slat, sRGB. ⚠️ No blue and no orange:
# the village kit's four drum hues (maroon 2, green 105, mustard 44, cream 42 degrees), deeper:
# at the village kit's exact bytes they washed out under AgX to pink, mint and pale butter (v1).
PLASTIC_TINTS = {"maroon": "7a2826", "leaf_green": "467a34", "mustard": "c29a2a", "cream": "e2d3ae"}
SLAT_TINTS = {"fresh": "ffffff", "grey": "dcd8d0", "warm": "f2e4d0"}
WATER = (0.045, 0.2, 0.12)          # linear; a deep green, hue about 150 degrees (v1's teal read pale blue)

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
    """A periodic smooth random field, unit variance, features about `sx_m` across and `sy_m`
    down. Periodic so every tile repeats seamlessly."""
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


def patch(scale_m, coverage, seed, feather=0.5, sy_m=None, **kw):
    """A feathered organic patch mask covering `coverage` of the image (the house style's second
    coat of paint): 0 outside, 1 inside. Big and few, never grain."""
    np = _np()
    n = field(scale_m, seed, sy_m, **kw) + 0.2 * field(scale_m / 2.5, seed + 1, None if sy_m is None else sy_m / 2.5, **kw)
    t = np.quantile(n, 1 - coverage)
    return smooth(clamp01((n - t) / feather + 0.5))


def coat(img, scale, mask):
    return img * (1 - mask[..., None]) + img * scale * mask[..., None]


def over(img, colour, mask):
    return img * (1 - mask[..., None]) + colour * mask[..., None]


# ---------------------------------------------------------------- pg_slat
# Research (the reference's prop sheet, Sea of Thieves and Torchlight crates): one flat board
# colour and two big soft patches, a lighter sun-bleached one and a darker damp one. No grain, no
# knots (v1 had drawn knots; the owner's simplify note cut fine marks), which also keeps it from
# being plank_c or timber_a in another colour.

def pg_slat():
    np = _np()
    img = np.broadcast_to(hexcol("bea784"), (SIZE, SIZE, 3)).astype(np.float32).copy()
    # (v4 at 6 % either way, and v5 at 9 %, rendered as blank plastic on the lids; about 12 %
    # in board-sized patches reads as painted wood while staying soft.)
    img = coat(img, np.array([1.11, 1.1, 1.08]), patch(0.32, 0.32, 501, sy_m=0.8))
    img = coat(img, np.array([0.87, 0.855, 0.84]), patch(0.25, 0.22, 503, sy_m=0.7))
    return img, np.zeros((SIZE, SIZE), np.float32)


# ---------------------------------------------------------------- pg_stave
# Research (stylized hand-painted barrels: the reference's barrels, Sea of Thieves, Genshin's
# Mondstadt barrels): the staves are drawn as broad flat bands with a soft darker line between
# them and a slight value difference from one stave to the next; the geometry stays a smooth
# rounded body. Its own drawing: no board ends (plank_c's), no long patches (timber_a's).

def pg_stave():
    np = _np()
    x, y = grid()
    sw = TILE_M / STAVES_PER_TILE
    k = np.floor(x / sw).astype(int) % STAVES_PER_TILE
    val = np.array([1.0, 0.95, 1.03, 0.97, 1.01, 0.94, 1.04, 0.98], np.float32)[k]
    img = hexcol("8e5e38")[None, None] * val[..., None]
    img = coat(img, np.array([1.07, 1.06, 1.05]), patch(0.5, 0.22, 521, sy_m=0.9))
    img = coat(img, np.array([0.92, 0.91, 0.9]), patch(0.4, 0.16, 523, sy_m=0.8))
    # The joint: a soft dark band about 1.5 cm wide, wobbling a little so it is drawn, not ruled.
    fx = x + 0.004 * field(0.4, 527)
    d = np.abs(((fx / sw + 0.5) % 1.0) - 0.5) * sw
    joint = smooth(clamp01((0.009 - d) / 0.006))
    img = over(img, hexcol("5e3a22"), joint * 0.75)
    return img, -joint


# ---------------------------------------------------------------- pg_catch
# The catch as one drawing, the net-pile idea (the owner's note: "a net pile is a soft lumpy heap
# with a painted net pattern"): big simple fish, a leaf-shaped body and a notched tail each, laid
# overlapping every way on a silver ground, one dark eye each. All the silvers within a narrow
# value range; the only strong mark is the eye.
CATCH_FISH_M = 0.26


def pg_catch():
    np = _np()
    from PIL import Image, ImageDraw
    S = SIZE
    px = S / TILE_M
    L = CATCH_FISH_M * px
    # A warm grey-silver, not white: v5's paler silvers read as broken china in the sun.
    img = Image.new("RGB", (S, S), (150, 154, 142))
    d = ImageDraw.Draw(img)
    rng = random.Random(541)
    # v1 of this drawing outlined every fish in the back colour and read as busy line work: the
    # outline is now only a step darker than the body, and the eye a soft dark grey.
    silvers = [(178, 181, 168), (170, 174, 162), (184, 185, 171), (164, 169, 158)]
    backs = [(138, 146, 128), (144, 148, 130), (132, 141, 126)]
    placed = []
    step = 0.62 * L
    rows = int(S / (0.42 * L))
    for r in range(rows):
        for c in range(int(S / step) + 1):
            cx = (c + 0.5 * (r % 2)) * step + rng.uniform(-0.1, 0.1) * L
            cy = (r + 0.5) * S / rows + rng.uniform(-0.08, 0.08) * L
            placed.append((cx, cy, rng.uniform(0, math.tau), rng.choice(silvers), rng.choice(backs),
                           rng.uniform(0.9, 1.1)))
    rng.shuffle(placed)
    for cx, cy, a, col, back, sc in placed:
        for ox in (-S, 0, S):
            for oy in (-S, 0, S):
                _fish_shape(d, cx + ox, cy + oy, a, L * sc, col, back)
    arr = np.asarray(img, np.float32) / 255
    # One soft patch of wet sheen across the heap.
    arr = coat(arr, np.array([1.04, 1.04, 1.04]), patch(0.5, 0.25, 547))
    grey = arr.mean(axis=2)
    return arr, grey - grey.mean()


def _fish_shape(d, cx, cy, a, L, col, back):
    ca, sa = math.cos(a), math.sin(a)

    def P(u, v):
        return (cx + u * ca - v * sa, cy + u * sa + v * ca)
    h = 0.2 * L
    top = [P(-0.5 * L + L * t, h * math.sin(math.pi * t) ** 0.8) for t in [i / 12 for i in range(13)]]
    bot = [P(-0.5 * L + L * t, -h * math.sin(math.pi * t) ** 0.8) for t in [1 - i / 12 for i in range(13)]]
    tail = [P(0.4 * L, 0), P(0.62 * L, 0.18 * L), P(0.56 * L, 0), P(0.62 * L, -0.18 * L)]
    d.polygon(tail, fill=back)
    d.polygon(top + bot, fill=col, outline=tuple(int(c * 0.93) for c in col), width=max(2, int(L * 0.02)))
    # The back: a darker band along one edge of the body.
    band = [P(-0.38 * L + L * 0.76 * t, h * 0.62 * math.sin(math.pi * (0.12 + 0.76 * t)) ** 0.8) for t in [i / 10 for i in range(11)]]
    d.line(band, fill=back, width=max(2, int(L * 0.07)), joint="curve")
    ex, ey = P(-0.33 * L, 0.02 * L)
    r = 0.045 * L
    d.ellipse((ex - r, ey - r, ex + r, ey + r), fill=(92, 94, 86))


# ---------------------------------------------------------------- pg_plastic
# Moulded plastic reads as ONE clean colour with a soft sheen; its only story is the sun, which
# chalks it in large pale patches. Painted near-white so "prop_tint" sets each drum's colour.

def pg_plastic():
    """⚠️ OWNER, 2026-09-27, on a green drum by the stall: "theres this green untextured barrel too".
    v1's two patches were 3.5 % either way, which the tint and the sun flattened to one flat green.
    Now, still big soft shapes and no grain (the house style), strong enough to read: sun-chalked
    patches 14 % paler, a damp grimy set 16 % darker with a warm cast, and a scatter of fist-sized
    scuffs (lighter, with a shallow dent in the height so the normal map catches the light)."""
    np = _np()
    img = np.broadcast_to(hexcol("ebe7df"), (SIZE, SIZE, 3)).astype(np.float32).copy()
    img = coat(img, np.array([1.14, 1.14, 1.13]), patch(0.55, 0.30, 601))          # sun-chalked
    img = coat(img, np.array([0.84, 0.82, 0.78]), patch(0.35, 0.22, 603, sy_m=0.6))  # grime, warm
    # Round 2 (pg_drum_v7): 7 cm scuffs at 7 % coverage read as a rash of white specks. Fewer,
    # bigger and softer now: a handful of 14 cm rubbed patches a tile, and a shallower dent.
    scuffs = patch(0.14, 0.035, 607, feather=0.6)
    img = coat(img, np.array([1.07, 1.07, 1.06]), scuffs)
    height = 0.5 - 0.15 * scuffs + 0.06 * field(0.5, 609)
    return img, height.astype(np.float32)


# ---------------------------------------------------------------- saving and the sheet

PAINTERS = {"pg_slat": pg_slat, "pg_stave": pg_stave, "pg_catch": pg_catch, "pg_plastic": pg_plastic}
STRENGTH = {"pg_slat": 1.0, "pg_stave": 2.0, "pg_catch": 1.5, "pg_plastic": 1.5}


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
    rgb = (np.clip(albedo, 0, 1) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(rgb).save(TEX / f"{name}_albedo.png")
    r = np.ptp(height)
    h = (height - height.min()) / r if r > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((h * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_height.png")
    Image.fromarray((normal_from_height(h, STRENGTH[name]) * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_normal.png")
    print("[props-cargo] painted", name)


def paint(names=None):
    for n in names or PAINTERS:
        save(n, *PAINTERS[n]())


def _multiplied(img, hexstr):
    """What the material multiplying the texture by a tint shows (linear light), back to sRGB."""
    np = _np()
    from PIL import Image
    a = np.asarray(img.convert("RGB"), np.float32) / 255
    t = np.array([int(hexstr[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)

    def lin(c):
        return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    out = lin(a) * lin(t)
    out = np.where(out <= 0.0031308, out * 12.92, 1.055 * np.power(np.maximum(out, 0), 1 / 2.4) - 0.055)
    return Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8))


def sheet(version):
    """The review sheet: timber_a and plank_c (approved neighbours), then pg_stave, pg_catch, pg_slat
    as each crate tint and pg_plastic as each drum colour."""
    from PIL import Image, ImageDraw, ImageFont
    out = LOGS / f"pg_swatches_v{version}.png"
    if out.exists():
        raise SystemExit(f"{out} exists: renders are never overwritten, pick a new version")
    try:
        font = ImageFont.truetype("arial.ttf", 17)
    except OSError:
        font = ImageFont.load_default()
    cell, gap, head = 260, 14, 30
    slat = Image.open(TEX / "pg_slat_albedo.png")
    plastic = Image.open(TEX / "pg_plastic_albedo.png")
    tiles = [("timber_a (approved)", Image.open(APPROVED / "timber_a_albedo.png").convert("RGB")),
             ("plank_c (approved)", Image.open(APPROVED / "plank_c_albedo.png").convert("RGB")),
             ("pg_stave (2 m)", Image.open(TEX / "pg_stave_albedo.png").convert("RGB")),
             ("pg_catch (2 m)", Image.open(TEX / "pg_catch_albedo.png").convert("RGB"))]
    tiles += [(f"pg_slat x {k}", _multiplied(slat, h)) for k, h in SLAT_TINTS.items()]
    tiles += [("", None)]
    tiles += [(f"pg_plastic x {k}", _multiplied(plastic, h)) for k, h in PLASTIC_TINTS.items()]
    per_row = 4
    rows = [tiles[i:i + per_row] for i in range(0, len(tiles), per_row)]
    W = gap + per_row * (cell + gap)
    H = gap + len(rows) * (head + cell + gap)
    page = Image.new("RGB", (W, H), (245, 241, 234))
    d = ImageDraw.Draw(page)
    y = gap
    for row in rows:
        for i, (label, tile) in enumerate(row):
            if tile is None:
                continue
            x = gap + i * (cell + gap)
            d.text((x, y), label, fill=(40, 36, 32), font=font)
            page.paste(tile.resize((cell, cell), Image.LANCZOS), (x, y + head))
        y += head + cell + gap
    LOGS.mkdir(parents=True, exist_ok=True)
    page.save(out)
    print("[props-cargo] sheet", out)


def compare(before, after):
    """The before/after the owner asked for: lineup v`before` over lineup v`after`, labelled."""
    from PIL import Image, ImageDraw, ImageFont
    out = LOGS / f"pg_lineup_before_after_v{after}.png"
    if out.exists():
        raise SystemExit(f"{out} exists: renders are never overwritten, pick a new version")
    try:
        font = ImageFont.truetype("arial.ttf", 30)
    except OSError:
        font = ImageFont.load_default()
    a = Image.open(LOGS / f"pg_lineup_v{before}.png").convert("RGB")
    b = Image.open(LOGS / f"pg_lineup_v{after}.png").convert("RGB")
    head = 50
    page = Image.new("RGB", (a.width, 2 * (a.height + head)), (245, 241, 234))
    d = ImageDraw.Draw(page)
    d.text((16, 10), f"BEFORE (v{before}): thin slats, grooved staves, four hoops, modelled fish, straight boxes",
           fill=(40, 36, 32), font=font)
    page.paste(a, (0, head))
    d.text((16, a.height + head + 10), f"AFTER (v{after}): thick bowed boards, fat leaning bodies, two bands, painted detail",
           fill=(40, 36, 32), font=font)
    page.paste(b, (0, a.height + 2 * head))
    page.save(out)
    print("[props-cargo] compare", out)


def texture_main(argv):
    if "--sheet" in argv:
        sheet(int(argv[argv.index("--sheet") + 1]))
        return
    if "--compare" in argv:
        i = argv.index("--compare")
        compare(int(argv[i + 1]), int(argv[i + 2]))
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
    tube = H.tube

    # Shared by NAME with the house, boat and village kits.
    SHARED = {"plank": "plank_c", "timber": "timber_a", "bamboo": "bamboo_a"}
    # Per slot: (bevel width m, angle above which an edge is sharp, harden normals). BIG bevels:
    # the soft cartoon edge is most of what makes a box read as a chunky prop. Round bodies are
    # smooth under 35 degrees so a 32-sided barrel shades round.
    FINISH = {"timber": (0.012, 30.0, True), "plank": (0.01, 30.0, True), "bamboo": (0.0, 60.0, False),
              "pg_slat": (0.011, 30.0, True), "pg_plastic": (0.012, 35.0, False), "pg_stave": (0.014, 35.0, False),
              "pg_catch": (0.0, 60.0, False), "pb_rope": (0.0, 60.0, False), "pg_water": (0.0, 40.0, False)}
    TINTED_SLOTS = {"pg_slat", "pg_plastic"}

    class Prop:
        """Collects pieces per slot for one prop (the house kit's House), with a per-piece tint
        for the tinted slots. `M` transforms every piece added while it is set."""

        def __init__(self, kind, seed):
            self.kind, self.seed = kind, seed
            self.rng = random.Random(f"lagoon-prop-cargo:{kind}:{seed}")
            self.pieces = {}
            self.M = None
            self.info = {}
            self.tint = (1.0, 1.0, 1.0)

        def add(self, slot, pc, roof=False):
            if not pc.bm.faces:
                pc.bm.free()
                return None
            if self.M is not None:
                bmesh.ops.transform(pc.bm, matrix=self.M, verts=pc.bm.verts[:])
            self.pieces.setdefault(slot, []).append((pc, self.tint))
            return pc

        def j(self, a):
            return self.rng.uniform(-a, a)

        def uv_off(self):
            return (self.rng.random(), self.rng.random())

        def set_tint(self, hexstr):
            self.tint = tuple(int(hexstr[i:i + 2], 16) / 255 for i in (0, 2, 4))

    # ------------------------------------------------------------ primitives

    def board(h, slot, p0, p1, w, t, up=Z, sway=0.0, crown=0.0, taper=0.07, seg=5):
        """An ORGANIC board from p0 to p1 (the house kit's beam made hand-cut): `w` across (along
        up x the length), `t` along `up`. Five stations along it; the board BOWS by `sway` across
        and `crown` along `up` (a sine, 0 at the ends), narrows by `taper` toward both ends, and
        every station's width and thickness wobble by up to 4 % or 2 mm, whichever is LESS (v4
        wobbled 4 % of a 0.5 m end board, 1 cm, straight into the side boards' clearance). Sign conventions matter: callers pass the
        bow in the direction that opens a clearance (see the module note on planes). One closed
        shell (_grid_solid), V along the board."""
        p0, p1 = Vector(p0), Vector(p1)
        d = (p1 - p0).normalized()
        L = (p1 - p0).length
        side = up.cross(d)
        if side.length < 1e-6:
            side = X.cross(d)
        side.normalize()
        upv = d.cross(side)
        top, bot = [], []
        for i in range(seg + 1):
            s = i / seg
            bow = math.sin(math.pi * s)
            c = p0 + d * (L * s) + side * (sway * bow) + upv * (crown * bow)
            narrow = 1.0 - taper * (2 * s - 1) ** 2
            ww = w * narrow / 2 + h.j(min(0.02 * w, 0.001))
            tt = t / 2 + h.j(min(0.02 * t, 0.001))
            top.append([c - side * ww + upv * tt, c + side * ww + upv * tt])
            bot.append([c - side * ww - upv * tt, c + side * ww - upv * tt])
        pc = H._grid_solid(top, bot)
        H._uv_frame(pc, side, d, upv, h.uv_off())
        return h.add(slot, pc)

    def lathe(h, slot, prof, sides=32, planar=False, u_span=None, wob=0.0, wob_seed=0.0):
        """A surface of revolution about local Z. `prof` is a list of (r, z) that starts and ends ON
        the axis (r = 0), so the shell is closed at both poles and manifold by construction.
        `wob` swells the radius unevenly round the girth (lumpy, hand-made), the same at every
        ring for a given angle and `wob_seed`, so the body is lumpy and not rippled.
        UVs: U round, V along the profile, world scale; `u_span` overrides how many UV units the
        full circle spans (pg_stave: a whole number of staves, so the seam matches). Flat rings
        (an end, a floor) and everything with `planar` take the local XY instead: round, the
        columns converge on the pole and smear into spokes."""
        pc = Piece()
        bm, uv = pc.bm, pc.uv
        ph = h.rng.uniform(0, math.tau)
        rings = []
        for r, z in prof:
            if r < 1e-6:
                rings.append(bm.verts.new((0, 0, z)))
                continue
            ring = []
            for k in range(sides):
                a = ph + math.tau * k / sides
                f = 1.0 + wob * noise.noise(Vector((math.cos(a) * 1.3, math.sin(a) * 1.3, wob_seed + z * 0.8)))
                ring.append(bm.verts.new((math.cos(a) * r * f, math.sin(a) * r * f, z)))
            rings.append(ring)
        acc = [0.0]
        for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
            acc.append(acc[-1] + math.hypot(r1 - r0, z1 - z0))
        s = 1.0 / UV_METRES
        span = u_span if u_span is not None else math.tau * max(r for r, _ in prof) * s
        ou, ov = h.uv_off()
        if u_span is not None:
            ou = 0.0          # keep the fitted staves' seam exactly on a joint

        def tex(k, i, co, flat):
            if planar or flat:
                return (co.x * s + ou, co.y * s + ov)
            return (k / sides * span + ou, acc[i] * s + ov)
        for i in range(len(prof) - 1):
            A, B = rings[i], rings[i + 1]
            fl = abs(prof[i][1] - prof[i + 1][1]) < 1e-6
            for k in range(sides):
                k2 = (k + 1) % sides
                if isinstance(A, list) and isinstance(B, list):
                    f = bm.faces.new((A[k], A[k2], B[k2], B[k]))
                    uvs = (tex(k, i, A[k].co, fl), tex(k + 1, i, A[k2].co, fl), tex(k + 1, i + 1, B[k2].co, fl),
                           tex(k, i + 1, B[k].co, fl))
                elif isinstance(A, list):
                    f = bm.faces.new((A[k], A[k2], B))
                    uvs = (tex(k, i, A[k].co, fl), tex(k + 1, i, A[k2].co, fl), tex(k + 0.5, i + 1, B.co, fl))
                else:
                    f = bm.faces.new((A, B[k2], B[k]))
                    uvs = (tex(k + 0.5, i, A.co, fl), tex(k + 1, i + 1, B[k2].co, fl), tex(k, i + 1, B[k].co, fl))
                for loop, t in zip(f.loops, uvs):
                    loop[uv].uv = t
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return h.add(slot, pc)

    def stave_span(r):
        """U units for a full circle of radius r in pg_stave: the nearest whole number of 25 cm
        staves (at least 6), so the painted joints meet at the seam."""
        n = max(6, round(math.tau * r / (TILE_M / STAVES_PER_TILE)))
        return n / STAVES_PER_TILE

    def torus(h, slot, z, R, r, sides=8, seg=32, tilt=0.0):
        """A fat hoop round local Z at height z (rope, or a bamboo cane bent round), tipped `tilt`
        radians off level about a random horizontal axis: hand-fitted, not machined. V along the
        hoop, U round its tube (the rope's twist and bamboo's nodes run along V)."""
        pc = Piece()
        bm, uv = pc.bm, pc.uv
        ph = h.rng.uniform(0, math.tau)
        T = Matrix.Translation((0, 0, z)) @ Matrix.Rotation(tilt, 4, Vector((math.cos(ph), math.sin(ph), 0)))
        verts = []
        for i in range(seg):
            a = ph + math.tau * i / seg
            d = Vector((math.cos(a), math.sin(a), 0))
            verts.append([bm.verts.new(T @ (d * (R + r * math.cos(math.tau * k / sides)) + Vector((0, 0, r * math.sin(math.tau * k / sides)))))
                          for k in range(sides)])
        s = 1.0 / UV_METRES
        ou, ov = h.uv_off()
        for i in range(seg):
            i2 = (i + 1) % seg
            for k in range(sides):
                k2 = (k + 1) % sides
                f = bm.faces.new((verts[i][k], verts[i2][k], verts[i2][k2], verts[i][k2]))
                for loop, (a, b) in zip(f.loops, ((i, k), (i + 1, k), (i + 1, k + 1), (i, k + 1))):
                    loop[uv].uv = (b / sides * math.tau * r * s + ou, a / seg * math.tau * R * s + ov)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return h.add(slot, pc)

    def heap(h, slot, x0, x1, y0, y1, zb, zt, lumps, nx=10, ny=7):
        """A soft lumpy slab: a flat bottom at zb and a top at zt raised by a few big round lumps
        and eased down toward its edges. One closed shell (the house kit's _grid_solid), UV
        projected flat from above at world scale."""
        top, bot = [], []
        for j in range(ny + 1):
            rt, rb = [], []
            for i in range(nx + 1):
                x = x0 + (x1 - x0) * i / nx
                y = y0 + (y1 - y0) * j / ny
                ease = min(1.0, min(i, nx - i, j, ny - j) / 2.0)
                z = zt + sum(a * math.exp(-((x - lx) ** 2 + (y - ly) ** 2) / (2 * rr * rr)) for lx, ly, rr, a in lumps)
                rt.append(Vector((x, y, zb + (z - zb) * (0.55 + 0.45 * ease))))
                rb.append(Vector((x, y, zb)))
            top.append(rt)
            bot.append(rb)
        pc = H._grid_solid(top, bot)
        s = 1.0 / UV_METRES
        ou, ov = h.uv_off()
        for f in pc.bm.faces:
            for loop in f.loops:
                q = loop.vert.co
                loop[pc.uv].uv = (q.x * s + ou, q.y * s + ov)
        return h.add(slot, pc)

    # ------------------------------------------------------------ crate

    def one_crate(h, W, D, Hc, lid=False, catch=False):
        """A fish crate at the origin (the current h.M places it): two solid END boards, TWO thick
        boards along each long side with a drain gap between, floor boards, and optionally a lid
        of two boards on two battens, or the catch heaped to the rim.

        ⚠️ PLANES: side boards 1.5 cm proud of the end boards and past their ends, bowing OUTWARD;
        the top side board's top 2 cm under the end boards' tops, so the lid's underside (8 mm into
        the end boards, crowned UP) is 12 mm clear of it."""
        rng = h.rng
        te, ts = 0.05, 0.035
        for sx in (-1, 1):
            xc = sx * (W / 2 - te / 2)
            board(h, "timber", (xc, 0, -0.015), (xc, 0, Hc), D, te, up=X, taper=0.04)
        bot, top, gap = 0.015, Hc - 0.02, 0.026
        sh = (top - bot - gap) / 2
        for sy in (-1, 1):
            yc = sy * (D / 2 + 0.002)
            for i in range(2):
                zc = bot + sh / 2 + i * (sh + gap)
                # Side is up x length = Z x X = +Y, so a positive sway bows toward +Y: outward for
                # the +Y side, and sy flips it for the -Y side.
                board(h, "pg_slat", (-(W / 2 + 0.015 + h.j(0.008)), yc, zc), (W / 2 + 0.015 + h.j(0.008), yc, zc), ts, sh,
                      up=Z, sway=sy * rng.uniform(0.004, 0.009), taper=0.08)
        # Floor: two boards, 1 cm off the bottom (a stacked crate's floor would otherwise lie on
        # the lower crate's top side boards). Straight: nothing sees them and they carry clearances.
        fw = (D - 0.05 - 0.012) / 2
        for sy in (-1, 1):
            board(h, "pg_slat", (-(W / 2 - 0.02), sy * (fw / 2 + 0.006), 0.023), (W / 2 - 0.02, sy * (fw / 2 + 0.006), 0.023),
                  fw, 0.026, taper=0.0)
        if lid:
            ox, oy = h.j(0.02), h.j(0.015)
            for sx in (-1, 1):
                xb = ox + sx * (W / 2 - 0.12)
                # Battens 4 mm up into the lid's underside even where the crown lifts it.
                board(h, "timber", (xb, oy - (D / 2 - 0.05), Hc - 0.016), (xb, oy + D / 2 - 0.05, Hc - 0.016), 0.05, 0.04,
                      taper=0.0)
            lw = (D + 0.03 - 0.012) / 2
            for sy in (-1, 1):
                yc = oy + sy * (lw / 2 + 0.006)
                # Along X with up = Z, `crown` bows along +Z: UP only.
                board(h, "pg_slat", (ox - (W / 2 + 0.03 + h.j(0.01)), yc, Hc + 0.0095), (ox + W / 2 + 0.03 + h.j(0.01), yc, Hc + 0.0095),
                      lw, 0.035, crown=rng.uniform(0.002, 0.004), taper=0.06)
        if catch:
            # The catch: one soft heap held by the end and side boards it runs into, its top at
            # about the rim with a few fish-sized lumps rising over it.
            # Many small low lumps, not one dome: v4's three big ones made a cushion.
            lumps = [(rng.uniform(-W / 2.6, W / 2.6), rng.uniform(-D / 3.2, D / 3.2), rng.uniform(0.06, 0.09), rng.uniform(0.018, 0.032))
                     for _ in range(7)]
            # Its long sides at D/2 + 8 mm, inside the side boards: 6 mm off the end boards' edges
            # and 14 mm off the boards' inner faces however far they bow out.
            heap(h, "pg_catch", -(W / 2 - te + 0.01), W / 2 - te + 0.01, -(D / 2 + 0.008), D / 2 + 0.008,
                 Hc * 0.35, Hc - 0.04, lumps, nx=12, ny=8)

    def build_crate(h):
        rng = h.rng
        variant = VARIANTS["crate"][(h.seed - 1) % len(VARIANTS["crate"])]
        W = rng.uniform(0.7, 0.78)
        D = rng.uniform(0.48, 0.52)
        Hc = rng.uniform(0.32, 0.36)
        count = {"open": 1, "lidded": 1, "stack2": 2, "stack3": 3, "stack2_open": 2}[variant]
        z = 0.0
        for i in range(count):
            h.set_tint(rng.choice(list(SLAT_TINTS.values())))
            # A stacked crate turns at least 4 degrees from the one under it: at under 2.5 its
            # board ends and the lower crate's end-board faces came out parallel and a few mm apart
            # (v1). The turns ALTERNATE up a stack, so the second and third crates never match
            # either (v5's stack of three did).
            sign = 1 if (i + h.seed) % 2 else -1
            yaw = 0.0 if i == 0 else sign * rng.uniform(0.07, 0.13)
            h.M = Matrix.Translation((h.j(0.03) if i else 0.0, h.j(0.02) if i else 0.0, z)) @ Matrix.Rotation(yaw, 4, "Z")
            last = i == count - 1
            one_crate(h, W, D, Hc, lid=last and variant in ("lidded", "stack2", "stack3"),
                      catch=last and variant in ("open", "stack2_open"))
            # The next crate sits 2.4 cm down into this one: its bottom boards then clear the end
            # boards' tops by 9 mm and its floor clears the top side boards by 6 mm.
            z += Hc - 0.024
        h.M = None
        h.info.update(variant=variant, count=count)

    # ------------------------------------------------------------ barrel

    def _barrel_r(z, Hb, r_end, r_mid):
        t = 2 * z / Hb - 1
        return r_end + (r_mid - r_end) * (1 - t * t)

    def barrel_body(h, Hb, r_end, r_mid, band):
        """A fat rounded body wearing pg_stave (the staves painted, not grooved), lumpy by 2.5 %
        round its girth, a plank head let in at each end under a rounded chime, and TWO thick
        bands a few degrees off level: abaca rope, or a bamboo cane bent round."""
        ch = 0.045                      # chime: how far the head sits in from the rim, radially
        prof = [(0.0, 0.06), (r_end - ch, 0.06), (r_end - ch, 0.0), (r_end - 0.018, 0.0), (r_end, 0.02)]
        for k in range(1, 8):
            z = 0.02 + (Hb - 0.04) * k / 8
            prof.append((_barrel_r(z, Hb, r_end, r_mid), z))
        prof += [(r_end, Hb - 0.02), (r_end - 0.018, Hb), (r_end - ch, Hb), (r_end - ch, Hb - 0.06), (0.0, Hb - 0.06)]
        lathe(h, "pg_stave", prof, sides=32, u_span=stave_span(r_mid), wob=0.025, wob_seed=h.rng.uniform(0, 50))
        # The heads: plank_c laid flat (its joints are the head's boards), 1 cm into the chime
        # wall (the wall's 2.5 % lumps move it 7 mm at most, inside its 4.5 cm); each 1.5 cm clear
        # of the body's own end floor and 3.5 cm under the rim.
        rh = r_end - ch + 0.01
        lathe(h, "plank", [(0.0, Hb - 0.075), (rh, Hb - 0.075), (rh, Hb - 0.035), (0.0, Hb - 0.035)], sides=24, planar=True)
        lathe(h, "plank", [(0.0, 0.035), (rh, 0.035), (rh, 0.075), (0.0, 0.075)], sides=24, planar=True)
        for z in (Hb * 0.24, Hb * 0.76):
            R = _barrel_r(z, Hb, r_end, r_mid)
            if band == "rope":
                torus(h, "pb_rope", z, R + 0.006, 0.032, sides=8, seg=32, tilt=h.j(0.04))
            else:
                torus(h, "bamboo", z, R + 0.004, 0.03, sides=8, seg=28, tilt=h.j(0.04))

    def _wedge(h, xc, yc, sy):
        """A chunky chock: a triangular block 14 cm long, its tall side toward the barrel."""
        pts = [(yc + sy * 0.1, -0.015), (yc - sy * 0.08, -0.015), (yc - sy * 0.08, 0.16)]
        pc = Piece()
        bm = pc.bm
        a = [bm.verts.new((xc - 0.07, y, z)) for y, z in pts]
        b = [bm.verts.new((xc + 0.07, y, z)) for y, z in pts]
        bm.faces.new(a)
        bm.faces.new(b)
        for i in range(3):
            k = (i + 1) % 3
            bm.faces.new((a[i], a[k], b[k], b[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        H._uv_frame(pc, Y, X, Z, h.uv_off())
        h.add("timber", pc)

    def _lean(h, deg_lo, deg_hi, sink):
        """Sunk `sink` into the ground and leaning a few degrees about a random level axis: a
        thing standing on sand, not on a table. The lean lifts one side of the foot by r x
        sin(lean); the sink covers it (2.5 degrees over a 0.34 m foot is 1.5 cm)."""
        a = h.rng.uniform(0, math.tau)
        lean = math.radians(h.rng.uniform(deg_lo, deg_hi))
        return Matrix.Translation((0, 0, -sink)) @ Matrix.Rotation(lean, 4, Vector((math.cos(a), math.sin(a), 0)))

    def build_barrel(h):
        rng = h.rng
        variant = VARIANTS["barrel"][(h.seed - 1) % len(VARIANTS["barrel"])]
        # Exaggerated: short and fat, a big belly (the reference's barrels are nearly as wide as
        # they are tall).
        Hb = rng.uniform(0.84, 0.92)
        r_mid = rng.uniform(0.39, 0.42)
        r_end = r_mid - rng.uniform(0.07, 0.085)
        band = "rope" if "rope" in variant else "bamboo"
        if variant.startswith("side"):
            # On its side along X: turned about Y, lifted so the belly sinks 2 cm into the ground,
            # turned a little on the ground, and chocked on both sides at opposite ends.
            yaw = h.j(0.3)
            h.M = Matrix.Rotation(yaw, 4, "Z") @ Matrix.Translation((0, 0, r_mid - 0.025)) @ \
                Matrix.Rotation(math.pi / 2, 4, "Y") @ Matrix.Translation((0, 0, -Hb / 2))
            barrel_body(h, Hb, r_end, r_mid, band)
            h.M = Matrix.Rotation(yaw, 4, "Z")
            for sy in (-1, 1):
                _wedge(h, sy * Hb * 0.36 + h.j(0.03), sy * r_mid * 0.72, sy)
            h.M = None
        else:
            h.M = _lean(h, 1.5, 3.0, 0.03)
            barrel_body(h, Hb, r_end, r_mid, band)
            h.M = None
        h.info.update(variant=variant)

    # ------------------------------------------------------------ water drum

    def tabo(h, M, colour):
        """The tabo: a round plastic dipper, a chunky cup with a thick straight handle rising
        from its rim. Built at the local origin (cup base at z = 0), placed by M."""
        h.set_tint(PLASTIC_TINTS[colour])
        saved = h.M
        h.M = M
        rb, rt, hc, t = 0.068, 0.085, 0.11, 0.012
        # The inner floor 2.2 cm up, so a tabo sunk 8 mm into a lid has no face within 4 mm of the
        # lid's top (v2: a 1.1 cm floor sat 3 mm over it).
        lathe(h, "pg_plastic", [(0.0, 0.0), (rb, 0.0), (rt, hc), (rt - t, hc), (rb - t, 0.022), (0.0, 0.022)], sides=20)
        tube(h, "pg_plastic", (rt - 0.025, 0, hc - 0.035), (rt + 0.15, 0, hc + 0.05), (0.024, 0.015), sides=8)
        h.M = saved

    def build_water_drum(h):
        """Both the drum and the tub stand sunk and leaning (BODY); the tabo is placed after h.M is
        cleared, so it takes BODY explicitly (v1 left it hovering 2 cm over the lid)."""
        rng = h.rng
        variant = VARIANTS["water_drum"][(h.seed - 1) % len(VARIANTS["water_drum"])]
        cols = list(PLASTIC_TINTS)
        drum_col = ("maroon", "mustard", "cream", "leaf_green")[(h.seed - 1) % 4]
        tabo_col = rng.choice([c for c in cols if c != drum_col])
        if variant.startswith("drum"):
            h.set_tint(PLASTIC_TINTS[drum_col])
            r = rng.uniform(0.31, 0.33)
            Ht = rng.uniform(0.84, 0.9)
            BODY = _lean(h, 1.0, 2.0, 0.025)
            h.M = BODY
            # A chunky moulded drum: a rounded foot, two fat rolling hoops, a bead at the rim; a
            # little soft round its girth (1 %), as a sun-warmed drum is.
            prof = [(0.0, 0.0), (r - 0.035, 0.0), (r, 0.035)]
            for zc in (Ht * 0.3, Ht * 0.66):
                prof += [(r, zc - 0.06), (r + 0.022, zc - 0.035), (r + 0.022, zc + 0.035), (r, zc + 0.06)]
            open_top = variant == "drum_open"
            floor = Ht - (0.16 if open_top else 0.06)
            prof += [(r, Ht - 0.035), (r + 0.016, Ht - 0.025), (r + 0.016, Ht), (r - 0.022, Ht), (r - 0.022, floor),
                     (0.0, floor)]
            ws = rng.uniform(0, 50)
            lathe(h, "pg_plastic", prof, sides=32, wob=0.01, wob_seed=ws)
            if open_top:
                # Water to 12 cm under the rim; its disc runs 6 mm into the wall.
                wz = Ht - 0.12
                lathe(h, "pg_water", [(0.0, wz - 0.03), (r - 0.016, wz - 0.03), (r - 0.016, wz), (0.0, wz)], sides=32)
                h.M = None
                # Floating: tipped on the water, its rim and handle above the surface.
                tabo(h, BODY @ Matrix.Translation((h.j(0.06), h.j(0.06), wz - 0.05)) @
                     Matrix.Rotation(rng.uniform(0, math.tau), 4, "Z") @ Matrix.Rotation(0.35, 4, "X"), tabo_col)
            else:
                # The lid: a soft dome with a skirt over the bead. Its underside 8 mm down inside
                # the rim, its skirt 8 mm off the bead and 1.5 cm below it.
                lr = r + 0.035
                lid = [(0.0, Ht + 0.06), (r * 0.55, Ht + 0.055), (lr - 0.01, Ht + 0.028), (lr, Ht - 0.04),
                       (r + 0.024, Ht - 0.04), (r + 0.024, Ht - 0.008), (0.0, Ht - 0.008)]
                lathe(h, "pg_plastic", lid, sides=28)
                h.M = None
                # The tabo stands on the lid, off centre, its base 8 mm down into the lid.
                a = rng.uniform(0, math.tau)
                rr = 0.1
                ztop = Ht + 0.06 - 0.005 * rr / (r * 0.55)
                tabo(h, BODY @ Matrix.Translation((math.cos(a) * rr, math.sin(a) * rr, ztop - 0.008)) @
                     Matrix.Rotation(rng.uniform(0, math.tau), 4, "Z"), tabo_col)
            h.info.update(variant=variant, colour=drum_col, tabo=tabo_col)
        else:
            # The wooden tub: short, wide, flaring to the top, lumpy, two bamboo hoops.
            Ht = rng.uniform(0.5, 0.55)
            rb, rt = 0.37, 0.43
            BODY = _lean(h, 1.0, 2.5, 0.03)
            h.M = BODY
            open_top = variant == "tub_open"
            floor = Ht - (0.2 if open_top else 0.08)
            wall = 0.035

            def r_at(z):
                return rb + (rt - rb) * z / Ht
            prof = [(0.0, 0.05), (rb - wall, 0.05), (rb - wall, 0.0), (rb - 0.015, 0.0), (r_at(0.02), 0.02),
                    (r_at(Ht * 0.5) + 0.012, Ht * 0.5), (r_at(Ht - 0.02), Ht - 0.02), (rt - 0.015, Ht), (rt - wall, Ht),
                    (r_at(floor) - wall, floor), (0.0, floor)]
            lathe(h, "pg_stave", prof, sides=32, u_span=stave_span(rt), wob=0.02, wob_seed=rng.uniform(0, 50))
            for z in (0.11, Ht - 0.11):
                torus(h, "bamboo", z, r_at(z) + 0.006 + (0.006 if abs(z - Ht * 0.5) < 0.2 else 0), 0.028, sides=8, seg=28,
                      tilt=h.j(0.035))
            if open_top:
                wz = Ht - 0.14
                rw = r_at(wz) - wall + 0.006
                lathe(h, "pg_water", [(0.0, wz - 0.03), (rw, wz - 0.03), (rw, wz), (0.0, wz)], sides=32)
                h.M = None
                tabo(h, BODY @ Matrix.Translation((h.j(0.08), h.j(0.08), wz - 0.05)) @
                     Matrix.Rotation(rng.uniform(0, math.tau), 4, "Z") @ Matrix.Rotation(-0.35, 4, "X"), tabo_col)
            else:
                # A ROUND lid (v1's three square-cut boards read as a patch laid on a round tub):
                # one disc wearing plank_c laid flat, 8 mm down into the rim and 1.5 cm over it all
                # round, with one thick ARCHED handle across it: its feet 1.2 cm into the lid, its
                # middle 2 cm clear (v4 crowned it 1 cm, which left its middle 2 mm over the lid).
                lr = rt + 0.015
                zl = Ht + 0.028
                lathe(h, "plank", [(0.0, Ht - 0.008), (lr, Ht - 0.008), (lr, zl), (0.0, zl)], sides=32, planar=True)
                half = lr - 0.1
                board(h, "timber", (0, -half, zl + 0.0225 - 0.012), (0, half, zl + 0.0225 - 0.012), 0.065, 0.045, crown=0.035,
                      taper=0.1)
                h.M = None
            h.info.update(variant=variant, tabo=tabo_col if open_top else "")
        h.M = None

    # ------------------------------------------------------------ assembly

    BUILDERS = {"crate": build_crate, "barrel": build_barrel, "water_drum": build_water_drum}

    def _append(dst, duv, dtint, pc, tint):
        vmap = {v: dst.verts.new(v.co) for v in pc.bm.verts}
        for f in pc.bm.faces:
            try:
                nf = dst.faces.new([vmap[v] for v in f.verts])
            except ValueError:
                continue
            for a, b in zip(f.loops, nf.loops):
                b[duv].uv = a[pc.uv].uv
                if dtint is not None:
                    b[dtint] = (*tint, 1.0)

    def build_prop(kind, seed=1):
        """Build one prop of `kind` (see KINDS) from `seed` and return its Collection, NOT linked to
        any scene: every part is parented to one root empty named by kind at the ground contact
        centre, front +Y. One mesh object per material slot, each with a live Bevel modifier
        (rope, bamboo canes, the catch and water excepted: they are round or soft already)."""
        if kind not in BUILDERS:
            raise ValueError(f"unknown prop kind {kind!r}; one of {KINDS}")
        h = Prop(kind, seed)
        BUILDERS[kind](h)
        # "pg_" in the collection and object names: the village kit looks its sources up as
        # "prop_<kind>_<seed>", and it has a water_drum too.
        col = bpy.data.collections.new(f"pg_{kind}_{seed}")
        root = bpy.data.objects.new(kind, None)
        root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.4
        col.objects.link(root)
        root["prop_kind"], root["prop_seed"], root["prop_mount"] = kind, seed, "ground"
        for k, v in h.info.items():
            if isinstance(v, (int, float, str)):
                root[f"prop_{k}"] = v
        lo = Vector((1e9, 1e9, 1e9))
        hi = -lo
        for slot, pcs in h.pieces.items():
            bm = bmesh.new()
            uvl = bm.loops.layers.uv.new("UVMap")
            ctl = bm.loops.layers.float_color.new("prop_tint") if slot in TINTED_SLOTS else None
            for pc, tint in pcs:
                _append(bm, uvl, ctl, pc, tint)
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
            me = bpy.data.meshes.new(f"pg_{kind}_{seed}_{slot}")
            bm.to_mesh(me)
            bm.free()
            if ctl is not None:
                # Stored as the byte sRGB colour the exporter writes (FBX vertex colour).
                src = me.color_attributes.get("prop_tint")
                vals = [0.0] * (len(me.loops) * 4)
                src.data.foreach_get("color", vals)
                me.color_attributes.remove(src)
                dst = me.color_attributes.new("prop_tint", "BYTE_COLOR", "CORNER")
                dst.data.foreach_set("color", vals)
            me.materials.append(material(slot))
            ob = bpy.data.objects.new(me.name, me)
            ob.parent = root
            col.objects.link(ob)
            if width > 0:
                bev = ob.modifiers.new("Bevel", "BEVEL")
                bev.width, bev.segments, bev.limit_method = width, 2, "ANGLE"
                bev.angle_limit = math.radians(30.0)
                bev.harden_normals = harden
                bev.use_clamp_overlap = True
        lo.z = max(lo.z, 0.05)
        root["prop_box"] = [lo.x, lo.y, lo.z, hi.x, hi.y, hi.z]
        return col

    # ------------------------------------------------------------ materials

    def _find(nt, kind):
        return next((n for n in nt.nodes if n.bl_idname == kind), None)

    def _tweak(m, bump, normal=1.0, rough=None):
        nt = m.node_tree
        d = _find(nt, "ShaderNodeDisplacement")
        if d:
            d.inputs["Scale"].default_value = bump
        nm = _find(nt, "ShaderNodeNormalMap")
        if nm:
            nm.inputs["Strength"].default_value = normal
        b = _find(nt, "ShaderNodeBsdfPrincipled")
        if rough is not None:
            b.inputs["Roughness"].default_value = rough
        img = next(n for n in nt.nodes if n.bl_idname == "ShaderNodeTexImage" and n.image and n.image.name.endswith("_albedo.png"))
        return nt, b, img

    def _times_tint(nt, b, img):
        at = nt.nodes.new("ShaderNodeAttribute")
        at.attribute_type = "GEOMETRY"
        at.attribute_name = "prop_tint"
        mix = nt.nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        nt.links.new(img.outputs["Color"], mix.inputs[6])
        nt.links.new(at.outputs["Color"], mix.inputs[7])
        nt.links.new(mix.outputs[2], b.inputs["Base Color"])

    def material(name):
        """A material by name, created once; shared kit names are reused as the file has them."""
        m = bpy.data.materials.get(name)
        if m:
            return m
        m = bpy.data.materials.new(name)
        if name in SHARED:
            RP.uv_material(m, SHARED[name])
            return m
        if name == "pg_water":
            m.use_nodes = True
            b = m.node_tree.nodes["Principled BSDF"]
            b.inputs["Base Color"].default_value = (*WATER, 1)
            b.inputs["Roughness"].default_value = 0.08
            return m
        tex = name
        if name == "pb_rope" and not (APPROVED / "pb_rope_albedo.png").exists():
            tex = "pv_rope"
        RP.uv_material(m, tex)
        bump = {"pg_slat": 0.002, "pg_plastic": 0.001, "pg_stave": 0.004, "pg_catch": 0.004, "pb_rope": 0.006}.get(name, 0.004)
        nt, b, img = _tweak(m, bump, rough={"pg_plastic": 0.42, "pg_catch": 0.4}.get(name, 0.9))
        if name in TINTED_SLOTS:
            _times_tint(nt, b, img)
        return m

    # ------------------------------------------------------------ checks

    def check_prop(col):
        """Tri counts (base and with the bevels), non-manifold edges, COPLANAR overlaps (parallel
        faces of different shells within 4 mm over each other) and FLOATING shells (shells in no
        chain of intersections down to the ground, z <= 0.01). The village kit's check, ground
        props only."""
        root = next(o for o in col.objects if o.parent is None)
        V, P, shell, slot_of = [], [], [], []
        grounded_s = set()
        out = {"tris": 0, "tris_bevelled": 0, "nonmanifold": {}}
        sid = 0
        # An unlinked collection is not in the depsgraph, so its bevels would not evaluate and the
        # bevelled count would equal the base one: link it for the count, then unlink.
        scene_col = bpy.context.scene.collection
        linked = col.name not in scene_col.children
        if linked:
            scene_col.children.link(col)
        bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        for ob in col.objects:
            if ob.type != "MESH":
                continue
            me = ob.data
            out["tris"] += sum(len(p.vertices) - 2 for p in me.polygons)
            ev = ob.evaluated_get(dg)
            em = ev.to_mesh()
            out["tris_bevelled"] += sum(len(p.vertices) - 2 for p in em.polygons)
            ev.to_mesh_clear()
            slot = ob.name.split(f"_{root['prop_seed']}_", 1)[-1]
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
                if any(V[base_v + i].z <= 0.01 for i in p.vertices):
                    grounded_s.add(s)
        if linked:
            scene_col.children.unlink(col)
        out["shells"] = sid
        tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
        normals = []
        for p in P:
            a, b, c = V[p[0]], V[p[1]], V[p[2]]
            normals.append((b - a).cross(c - a).normalized())
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
        out["bbox"] = (round(min(xs), 2), round(min(ys), 2), round(min(zs), 2), round(max(xs), 2), round(max(ys), 2),
                       round(max(zs), 2))
        return out

    # ------------------------------------------------------------ preview

    def _plain_mat(name, colour, rough=0.85):
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (*colour, 1)
        b.inputs["Roughness"].default_value = rough
        return m

    def _preview_scene():
        scene = bpy.context.scene
        g = bpy.data.meshes.new("ground")
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=200)
        bm.to_mesh(g)
        bm.free()
        g.materials.append(_plain_mat("warm_sand", (0.66, 0.55, 0.38)))
        go = bpy.data.objects.new("ground", g)
        scene.collection.objects.link(go)
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

    def _place(coll, src, at, heading=0.0):
        sroot = next(o for o in src.objects if o.parent is None)
        root = bpy.data.objects.new(sroot.name + "_placed", None)
        coll.objects.link(root)
        root.matrix_world = Matrix.Translation(at) @ Matrix.Rotation(heading, 4, "Z")
        for ob in src.objects:
            if ob is sroot:
                continue
            dup = ob.copy()
            coll.objects.link(dup)
            dup.parent = root
            dup.matrix_parent_inverse = Matrix.Identity(4)
        return root

    def main():
        argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
        version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
        missing = [n for n in PAINTERS if not (TEX / f"{n}_albedo.png").exists()]
        if missing:
            subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--paint", ",".join(missing)], check=True)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        scene = bpy.context.scene
        lineup = bpy.data.collections.new("pg_lineup")
        scene.collection.children.link(lineup)
        rows = []
        totals = {"coplanar": 0, "floating": 0, "nonmanifold": 0}
        for r, kind in enumerate(KINDS):
            y = -2.6 * r
            seeds = list(range(1, len(VARIANTS[kind]) + 1)) + [len(VARIANTS[kind]) + 1]
            srcs = [(s, build_prop(kind, s)) for s in seeds]
            widths = []
            for s, src in srcs:
                sroot = next(o for o in src.objects if o.parent is None)
                b = sroot["prop_box"]
                widths.append(max(0.6, b[3] - b[0]))
                c = check_prop(src)
                info = {k[5:]: v for k, v in sroot.items()
                        if k.startswith("prop_") and k not in ("prop_box", "prop_kind", "prop_seed", "prop_mount")}
                print(f"[props-cargo] {kind} {s}: tris {c['tris']} ({c['tris_bevelled']} bevelled), coplanar "
                      f"{c['coplanar']} {c['coplanar_examples']}, floating {c['floating']} {c['floating_examples']}, "
                      f"nonmanifold {c['nonmanifold']}, bbox {c['bbox']}, {info}")
                totals["coplanar"] += c["coplanar"]
                totals["floating"] += c["floating"]
                totals["nonmanifold"] += sum(c["nonmanifold"].values())
            gap = 0.55
            x = -(sum(widths) + gap * (len(widths) - 1)) / 2
            xs = []
            for (s, src), w in zip(srcs, widths):
                _place(lineup, src, (x + w / 2, y, 0.0))
                xs.append(x + w / 2)
                x += w + gap
            rows.append((kind, y, xs))
        print("[props-cargo] totals", totals)
        if not version:
            return
        cam = _preview_scene()
        for kind, y, xs in rows:
            _scale_ref(lineup, (xs[-1] + 1.0, y, 0))
        # The close-up: the first two crates, with the first barrels and drums behind them.
        cx0 = (rows[0][2][0] + rows[0][2][1]) / 2
        shots = [("lineup", (0.0, 9.5, 6.0), (0.0, -2.6, 0.4), 30),
                 ("close", (cx0 + 0.3, 2.3, 1.45), (cx0, -1.2, 0.3), 30)]
        for kind, y, xs in rows:
            cx = (xs[0] + xs[-1]) / 2
            span = xs[-1] - xs[0] + 2.2
            dist = max(3.0, span / 2 / math.tan(math.radians(25)))
            shots.append((kind.split("_")[-1], (cx, y + dist, 0.5 + dist * 0.42), (cx, y, 0.4), 35))
        LOGS.mkdir(parents=True, exist_ok=True)
        for tag, pos, tgt, lens in shots:
            path = LOGS / f"pg_{tag}_v{version}.png"
            if path.exists():
                raise SystemExit(f"[props-cargo] {path} exists: never overwrite a render, bump --preview")
            cam.location = pos
            cam.data.lens = lens
            cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            print("[props-cargo] preview", path)


if __name__ == "__main__":
    if IN_BLENDER:
        main()
    else:
        texture_main(sys.argv[1:])
