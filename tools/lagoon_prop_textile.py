"""Lagoon Court TEXTILE PROP KIT: woven mats, hanging nets and a laundry line, with their own
painted textures where nothing already drawn covers the surface.

  py -3 tools/lagoon_prop_textile.py --paint                   # paint every pw_ texture
  py -3 tools/lagoon_prop_textile.py --paint pw_banig          # paint the named ones
  py -3 tools/lagoon_prop_textile.py --sheet N                 # swatch sheet vN (+ 3 x 3 repeats)
  py -3 tools/lagoon_prop_textile.py --before-after OLD NEW    # lineup vOLD over lineup vNEW
  blender -b --python tools/lagoon_prop_textile.py -- --preview N

The Blender run writes ArtSource/lagoon/lagoon_props_textile.blend (the lineup) and, with
--preview N, Logs/lagoon-blender/pw_{lineup,close,closeb,closec,far}_vN.png. It paints any
missing pw_ texture first (through `py -3`: Blender's Python has no PIL). An existing render is
never overwritten: bump N.

SCOPE. The village dressing is split across parallel kits, one per prop group; this file is the
TEXTILES group only (docs/LAGOON_REWORK_GUIDE.md § 7a item 1: "nets drying", laundry, and the
Filipino banig; item 8: "nets on the walls"). Placement in the cove is the lead's.

  import lagoon_prop_textile as PW
  col = PW.build_prop(kind, seed)      # a Collection, not linked anywhere; one root empty
  PW.check_prop(col)                   # tris, non-manifold, coplanar overlaps, floating shells

THE STYLE (owner, of the first prop renders: "i dont like how details some of the props are.
again we're going for a stylized semi-cartoony environment style", and "you should really
experiment more with being organic in how you shape things"; Art_Direction.md § 0). So:

  * FEW BIG SHAPES. A net is one soft draped sheet with the net PAINTED on it and its slack in
    one lumpy roll, not strands, open mesh or a row of gathered falls (v1 and v2 had all three).
    A laundry line carries three big pieces, not six small ones, and no clothes pegs. Lashings
    and ties are one fat ring at most.
  * ORGANIC MEMBERS. Every post, pole and peg is chunky, tapers, bends a little along its length,
    leans, and eases off at its ends (`member`); no two are the same size.
  * SOFT TEXTURES. pw_banig is a few wide soft bands over a big soft weave, low contrast; v1's
    4 cm twill with gap lines was fine patterning and is gone.

KINDS. +Y is the front. The root empty is named by kind and carries prop_kind, prop_seed,
prop_mount ("ground" or "wall"), prop_variant and prop_size (x, y, z extents in metres).

  * "woven_mat"     ORIGIN AT THE GROUND, centre of the pair's footprint. A banig sleeping mat
                    laid out (1.0 to 1.15 m by 1.5 to 1.7 m and 2.4 cm thick, its far end still
                    curled from being rolled) and a second mat rolled up (1.0 to 1.1 m long,
                    about 28 cm across, two fat turns of spiral showing at its ends, one cord
                    round it on some seeds). ODD seeds: the roll lies beside the mat. EVEN seeds:
                    across the head of the mat, where a pillow would go.
  * "hanging_net"   ODD seeds, POLE: ORIGIN AT THE GROUND, between the two posts. A green net
                    thrown over a bamboo pole on two bamboo posts (2.3 to 2.8 m apart, the pole
                    about 2 m up, the posts 0.35 m into the ground): one soft sheet hanging in a
                    few big folds, its slack gathered in a lumpy roll along the pole, a fat float
                    line along its front hem threaded with three or four chunky floats.
                    EVEN seeds, WALL: ORIGIN ON THE GROUND AT THE WALL FACE (y = 0 is the wall
                    face; the pegs run 5 cm into the wall behind). The same soft net pinned to
                    three chunky timber pegs about 1.9 m up, swagging between them and bunched
                    out where it is pinned, with its float line and floats along the bottom.
  * "laundry_line"  ORIGIN AT THE GROUND, between the posts. A cord between two bamboo posts
                    (3.0 to 3.4 m apart, about 2.1 m tall, 0.35 m into the ground) with three big
                    pieces of washing in pastels (towels and a malong folded over the line, a
                    shirt or shorts hung by the line through their top edge). EVEN seeds prop the
                    middle of the line on a forked bamboo tukod, the Filipino yard way.

MATERIALS. Shared by NAME with the house and boat kits, used as they are when the file already
has them, else built with render_lagoon_texture_preview.uv_material on their approved texture:
bamboo (bamboo_a), timber (timber_a), cloth (the cloth texture from tools/lagoon_paint_materials.py,
multiplied by its per-piece "cloth_tint" pastel, written with that file's tint_cloth). The beach
kit's drawings are reused under their own names: pb_rope (cord, tie, float line) and pb_net (the
net; tools/author_lagoon_props_beach.py paints it and owns its look). NEW drawings, painted here
into ArtSource/lagoon/textures/pw_*_{albedo,height,normal}.png, world scale (2 m tile, 1024 px):

  pw_banig   a Filipino banig: a big soft weave of 10 cm straps (a 3 per cent cel step, no gap
             lines) with a few wide dyed bands (crimson, mustard, leaf green, magenta) across
             natural straw, the way a Samar mat is banded. V runs along the mat's length, so on
             the roll the bands wrap round it as rings.
  pw_float   a net float: one soft near-white fill with two or three big feathered patches; the
             material MULTIPLIES it by each float's "float_tint" (red, yellow or white), the
             route the boats' trim_tint and the laundry's cloth_tint take, so one texture serves
             every float (Unity: the FBX vertex colour).

UVs: one map "UVMap", world scale, 1 unit = 2 m (the house and boat kits' rule): V along every
member, V down every hanging cloth and net, V along the mat's length. One object per material
slot, each with a live Bevel modifier (rope excepted: six sided, smooth shaded).

⚠️ ROLE HUES (Art_Direction.md § 1): nothing near offence orange #f87020 or defence blue #0080e8.
The net is green (Filipino nylon nets are green or blue; blue is the defence colour), the floats
are red, yellow and white, the banig bands crimson, mustard (hue about 43, the nearest to orange
and 19 degrees clear of it), leaf green and magenta, the washing the cloth pastels (pink, cream,
mint, pale yellow, white).

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2). A towel's fold rides ON its cord
(the cord through the middle of the cloth, the cloth's faces 1 cm either side of it), the net's
fold rides clear of its pole inside the gathered roll, floats are threaded ON their float line,
the roll on the mat sinks to the middle of the mat's thickness, and posts and pegs run into the
ground or the wall. `check_prop` proves it per build: `coplanar` counts overlapping parallel faces
of different shells within 4 mm, `floating` counts shells in no chain of intersections to the
mount (the ground z <= 0.01, or the wall y <= 0.005).
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

KINDS = ("woven_mat", "hanging_net", "laundry_line")
UV_METRES = 2.0

# ================================================================ painting (numpy, PIL; py -3)

SIZE = 1024
TILE_M = 2.0
PXM = SIZE / TILE_M
# Normal-map strength per texture: soft, the weave a gentle pillow per strap.
STRENGTH = {"pw_banig": 1.2, "pw_float": 0.8}

# Banig colours, sRGB. The straw is buri or tikog left natural; the dyes are the Samar ones,
# a step softer than full strength so a band never shouts over the straw.
BANIG_STRAW = "e5cc90"
BANIG_DYES = {"k": "c0444a",  # crimson (hue 357)
              "m": "e2b951",  # mustard (hue 43: 19 degrees clear of offence orange's 24)
              "g": "6ea24f",  # leaf green (hue 98)
              "p": "cc5a8a"}  # magenta (hue 335; more red than blue)
# The dyed bands along V, (from m, to m, dye), over the 2 m tile; straw everywhere else. Two banded
# groups per tile with wide straw between, so a 1.5 to 1.7 m mat always shows one or two groups
# whatever its offset. Widths 6 to 16 cm: v1's 4 cm stripes read as fine patterning.
BANIG_BANDS = [(0.30, 0.46, "k"), (0.52, 0.60, "m"), (0.66, 0.74, "g"),
               (1.22, 1.38, "p"), (1.44, 1.50, "m"), (1.56, 1.66, "p")]
STRAP_M = 0.10

FLOAT_TINTS = {  # linear RGB, the boats' red and yellow, and a warm white
    "red": (0.62, 0.035, 0.03),
    "yellow": (0.92, 0.62, 0.04),
    "white": (0.86, 0.83, 0.76),
}


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


def field(s_m, seed):
    """A periodic smooth random field over the 2 m tile, unit variance, features about s_m."""
    np = _np()
    white = np.random.default_rng(seed).standard_normal((SIZE, SIZE))
    f = np.fft.fftfreq(SIZE)
    g = np.exp(-((f[:, None] * s_m * PXM) ** 2 + (f[None, :] * s_m * PXM) ** 2) * 2)
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * g))
    return ((n - n.mean()) / (n.std() + 1e-9)).astype(np.float32)


def smooth(a):
    return a * a * (3 - 2 * a)


def patch(scale_m, coverage, seed, feather=0.4):
    """A feathered organic patch mask covering `coverage` of the tile: 0 outside, 1 inside."""
    np = _np()
    n = field(scale_m, seed) + 0.25 * field(scale_m / 3, seed + 1)
    t = np.quantile(n, 1 - coverage)
    return smooth(np.clip((n - t) / feather + 0.5, 0, 1))


def banig():
    """A banig as a stylized prop reads through its colour BANDS and a soft sense of weave
    (research: Basey and Samar banig; stylized woven-mat textures in the reference style). So:
    wide dyed bands with soft 1.5 cm edges across flat straw, and under everything a big 2 over 2
    twill of 10 cm straps shown only as a 3 per cent cel step (each strap's upper half a shade
    lighter), no gap lines, no fibre. A couple of big feathered patches, sun-faded, low contrast."""
    np = _np()
    x, y = grid()
    img = np.broadcast_to(hexcol(BANIG_STRAW), (SIZE, SIZE, 3)).astype(np.float32).copy()
    wob = 0.008 * field(0.5, 741)                        # band edges wander a hand's width
    for a, b, key in BANIG_BANDS:
        m = smooth(np.clip((y + wob - a) / 0.015 + 0.5, 0, 1)) * smooth(np.clip((b - y - wob) / 0.015 + 0.5, 0, 1))
        img = img * (1 - m[..., None]) + hexcol(BANIG_DYES[key]) * m[..., None]
    n = int(round(TILE_M / STRAP_M))
    iu = np.floor(x / STRAP_M).astype(int) % n
    iv = np.floor(y / STRAP_M).astype(int) % n
    fu = x / STRAP_M - np.floor(x / STRAP_M)
    fv = y / STRAP_M - np.floor(y / STRAP_M)
    weft_over = (iu + iv) % 4 < 2
    across = np.where(weft_over, fv, fu)
    lit = smooth(np.clip((0.5 - across) / 0.12 + 0.5, 0, 1))      # the strap's upper half
    img = img * (1 + 0.03 * lit - 0.015 * weft_over)[..., None]
    fade = patch(0.6, 0.3, 731, feather=0.8)
    grey = img.mean(axis=2, keepdims=True)
    img = img * (1 - 0.12 * fade[..., None]) + (grey * 1.05) * 0.12 * fade[..., None]
    img = img * (1 - 0.05 * patch(0.5, 0.15, 733, feather=0.7))[..., None]
    height = 0.5 + 0.5 * np.sin(np.pi * np.clip(across, 0, 1)) * (0.8 + 0.2 * weft_over)
    return img, height


def float_tex():
    """A net float: moulded foam or plastic, one flat near-white fill with two or three big
    feathered patches (a shade darker where it has sat in the water, a shade lighter where the
    sun has chalked it). Neutral, because the material multiplies it by each float's colour."""
    np = _np()
    img = np.broadcast_to(hexcol("f4f0e6"), (SIZE, SIZE, 3)).astype(np.float32).copy()
    img = img * (1 - 0.06 * patch(0.4, 0.3, 811, feather=0.7))[..., None]
    img = img * (1 + 0.03 * patch(0.35, 0.2, 813, feather=0.7))[..., None]
    height = 0.5 + 0.2 * patch(0.3, 0.35, 815, feather=0.8) - 0.1 * patch(0.4, 0.3, 811, feather=0.7)
    return img, height


PAINTERS = {"pw_banig": banig, "pw_float": float_tex}


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
    print("[textile-paint]", name)


def paint(names=None):
    for name in names or PAINTERS:
        img, height = PAINTERS[name]()
        save(name, img, height)


def _lin_to_srgb(c):
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def sheet(version):
    """Each new texture beside the reused pb_net and the approved plank_c, one tile and a 3 x 3
    repeat each (the repeat is what shows a seam or a stripe that stacks), and pw_float as each
    float colour will wear it."""
    np = _np()
    from PIL import Image, ImageDraw
    cell = 300
    names = ["pw_banig", "pw_float", "pb_net", "plank_c"]
    out = Image.new("RGB", (cell * 4 + 50, cell * 2 + 250), (236, 228, 212))
    d = ImageDraw.Draw(out)
    for i, n in enumerate(names):
        im = Image.open(APPROVED / f"{n}_albedo.png").convert("RGB")
        x0 = 10 + i * (cell + 10)
        small = im.resize((cell, cell))
        out.paste(small, (x0, 30))
        rep = Image.new("RGB", (cell * 3, cell * 3))
        for a in range(3):
            for b in range(3):
                rep.paste(small, (a * cell, b * cell))
        out.paste(rep.resize((cell, cell)), (x0, cell + 45))
        tag = "approved" if n == "plank_c" else "reused" if n.startswith("pb") else "new"
        d.text((x0, 10), f"{n}  ({tag})", fill=(40, 25, 10))
    fl = np.asarray(Image.open(APPROVED / "pw_float_albedo.png").convert("RGB"), np.float32) / 255
    lin = np.where(fl <= 0.04045, fl / 12.92, ((fl + 0.055) / 1.055) ** 2.4)
    for i, (k, rgb) in enumerate(FLOAT_TINTS.items()):
        o = lin * np.array(rgb, np.float32)
        o = np.where(o <= 0.0031308, o * 12.92, 1.055 * np.power(np.maximum(o, 0), 1 / 2.4) - 0.055)
        im = Image.fromarray((np.clip(o, 0, 1) * 255 + 0.5).astype(np.uint8)).resize((cell // 2, cell // 2))
        x0 = 10 + i * (cell // 2 + 10)
        out.paste(im, (x0, 2 * cell + 80))
        d.text((x0, 2 * cell + 62), f"pw_float x {k}", fill=(40, 25, 10))
    LOGS.mkdir(parents=True, exist_ok=True)
    path = LOGS / f"pw_swatches_v{version}.png"
    if path.exists():
        raise SystemExit(f"{path} exists: never overwrite, bump N")
    out.save(path)
    print("[textile-paint] sheet", path)


def before_after(old, new):
    """The owner asked to see before and after: lineup vOLD above lineup vNEW, labelled."""
    from PIL import Image, ImageDraw
    a = Image.open(LOGS / f"pw_lineup_v{old}.png").convert("RGB")
    b = Image.open(LOGS / f"pw_lineup_v{new}.png").convert("RGB")
    w = 1200
    a = a.resize((w, int(a.height * w / a.width)))
    b = b.resize((w, int(b.height * w / b.width)))
    out = Image.new("RGB", (w, a.height + b.height + 60), (236, 228, 212))
    d = ImageDraw.Draw(out)
    d.text((10, 8), f"BEFORE: pw_lineup_v{old}", fill=(40, 25, 10))
    out.paste(a, (0, 26))
    d.text((10, a.height + 34), f"AFTER: pw_lineup_v{new} (simplified: few big shapes, organic members, soft textures)",
           fill=(40, 25, 10))
    out.paste(b, (0, a.height + 56))
    path = LOGS / f"pw_beforeafter_v{new}.png"
    if path.exists():
        raise SystemExit(f"{path} exists: never overwrite")
    out.save(path)
    print("[textile-paint] before/after", path)


# ================================================================ models (Blender)

try:
    import bmesh
    import bpy
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
except ImportError:          # py -3: the painting half only
    bpy = None

if bpy is not None:
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import author_lagoon_boats as BK             # noqa: E402  Piece, loft, sweep, _append, _catmull
    import render_lagoon_texture_preview as RP   # noqa: E402  uv_material
    import lagoon_paint_materials as LPM         # noqa: E402  _cloth, tint_cloth, _islands

    Z = Vector((0.0, 0.0, 1.0))
    # material slot -> (material name, texture). Kit-shared names are reused exactly as found.
    SLOTS = {
        "bamboo": ("bamboo", "bamboo_a"),
        "timber": ("timber", "timber_a"),
        "cloth": ("cloth", "cloth"),
        "rope": ("pb_rope", "pb_rope"),
        "net": ("pb_net", "pb_net"),
        "banig": ("pw_banig", "pw_banig"),
        "float": ("pw_float", "pw_float"),
    }
    # (bevel width m, sharp angle degrees, harden normals); None: no bevel, smooth (rope).
    FINISH = {
        "bamboo": (0.008, 50.0, False),
        "timber": (0.010, 35.0, False),
        "cloth": (0.005, 40.0, False),
        "rope": None,
        "net": (0.008, 45.0, False),
        "banig": (0.008, 40.0, False),
        "float": (0.014, 40.0, False),
    }
    # Bump depth for small props: uv_material's 3 cm is a roof's.
    BUMP = {"pw_banig": 0.004, "pw_float": 0.002, "pb_rope": 0.004, "pb_net": 0.006}

    class Kit:
        """One prop under construction: closed shells per material slot (the boat kit's pieces),
        a seeded stream, and what the root empty will carry."""

        def __init__(self, kind, seed):
            self.kind, self.seed = kind, seed
            self.rng = random.Random(f"lagoon-textile:{kind}:{seed}")
            self.pieces = {}
            self.mount = "ground"
            self.variant = ""

        def add(self, slot, pc):
            if pc is None or not pc.bm.faces:
                return None
            self.pieces.setdefault(slot, []).append(pc)
            return pc

        def uv_off(self):
            return (self.rng.random(), self.rng.random())

    # ------------------------------------------------------------ primitives

    def member(k, slot, p0, p1, r0, r1, bend=0.025, sides=8, nodes=0.0, dome=True):
        """An ORGANIC member from p0 to p1 (owner: "the railings are just thin and plain shapes,
        where as the reference isnt just a straight rectangular prism"): it tapers from r0 to r1,
        bows by up to `bend` of its length sideways in a seeded direction, its girth swells
        softly along it, and its ends ease off (`dome`). Bamboo gets its node rings."""
        p0, p1 = Vector(p0), Vector(p1)
        d = p1 - p0
        L = d.length
        side = d.cross(Z)
        side = side.normalized() if side.length > 1e-6 else Vector((1, 0, 0))
        up = side.cross(d).normalized()
        b1, b2 = (k.rng.uniform(-1, 1) * bend * L for _ in range(2))
        c1, c2 = (k.rng.uniform(-0.5, 0.5) * bend * L for _ in range(2))
        pts = [p0, p0 + d * 0.33 + side * b1 + up * c1, p0 + d * 0.67 + side * b2 + up * c2, p1]
        ph = k.rng.uniform(0, math.tau)

        def rad(s, Lt):
            r = r0 + (r1 - r0) * s / Lt
            r *= 1 + 0.05 * math.sin(math.tau * s / 0.7 + ph)
            return (r, r)
        path = BK._catmull(pts, per=5)
        BK.sweep(k, slot, path, rad, sides=sides, step=0.12, nodes=nodes, dome=dome, phase=k.rng.random())
        return path

    def at_z(path, z):
        """The point of a (roughly upright) member's centreline at height z, so whatever is tied
        to a bent post is tied to the post where it actually is."""
        for p, q in zip(path, path[1:]):
            if (p.z - z) * (q.z - z) <= 0 and abs(q.z - p.z) > 1e-9:
                return p.lerp(q, (z - p.z) / (q.z - p.z))
        return min(path, key=lambda v: abs(v.z - z)).copy()

    def slab(k, slot, P, UV, thick):
        """A thin closed sheet through the grid of points P[i][j] (i across, j along the hang),
        `thick` through, both faces and the rim welded into one shell. UV[i][j] is (u, v) in
        metres on the mid-surface; both faces take it, the rim its own vertex's."""
        nu, nv = len(P), len(P[0])
        N = []
        for i in range(nu):
            row = []
            for j in range(nv):
                du = P[min(i + 1, nu - 1)][j] - P[max(i - 1, 0)][j]
                dv = P[i][min(j + 1, nv - 1)] - P[i][max(j - 1, 0)]
                n = du.cross(dv)
                row.append(n.normalized() if n.length > 1e-9 else Z.copy())
            N.append(row)
        pc = BK.Piece()
        bm, uvl = pc.bm, pc.uv
        s = 1.0 / UV_METRES
        ou, ov = k.uv_off()
        T = [[bm.verts.new(P[i][j] + N[i][j] * (thick / 2)) for j in range(nv)] for i in range(nu)]
        B = [[bm.verts.new(P[i][j] - N[i][j] * (thick / 2)) for j in range(nv)] for i in range(nu)]

        def put(f, ijs):
            for loop, (i, j) in zip(f.loops, ijs):
                u, v = UV[i][j]
                loop[uvl].uv = (u * s + ou, v * s + ov)
        for i in range(nu - 1):
            for j in range(nv - 1):
                ij = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
                put(bm.faces.new([T[a][b] for a, b in ij]), ij)
                put(bm.faces.new([B[a][b] for a, b in reversed(ij)]), list(reversed(ij)))
        rim = ([(i, 0) for i in range(nu)] + [(nu - 1, j) for j in range(1, nv)]
               + [(i, nv - 1) for i in range(nu - 2, -1, -1)] + [(0, j) for j in range(nv - 2, 0, -1)])
        for a in range(len(rim)):
            p, q = rim[a], rim[(a + 1) % len(rim)]
            f = bm.faces.new((T[p[0]][p[1]], T[q[0]][q[1]], B[q[0]][q[1]], B[p[0]][p[1]]))
            put(f, [p, q, q, p])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return k.add(slot, pc)

    def outline_slab(k, slot, pts, thick, wave=None):
        """A flat cut piece (a shirt, shorts): the closed outline `pts` in the XZ plane,
        extruded `thick` through along Y, centred on y = 0, subdivided so `wave(x, z)` (pushing
        both faces along Y together, so the thickness holds) bends its faces into soft folds.
        UVs planar, U along x and V DOWN the hang, so the cloth texture's folds hang vertically."""
        pc = BK.Piece()
        bm = pc.bm
        f = bm.faces.new([bm.verts.new((x, thick / 2, z)) for x, z in pts])
        bmesh.ops.triangulate(bm, faces=[f])
        for _ in range(2):
            bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if e.calc_length() > 0.12], cuts=1)
            bmesh.ops.triangulate(bm, faces=bm.faces[:])
        ex = bmesh.ops.extrude_face_region(bm, geom=bm.faces[:])
        moved = [e for e in ex["geom"] if isinstance(e, bmesh.types.BMVert)]
        bmesh.ops.translate(bm, vec=(0, -thick, 0), verts=moved)
        if wave:
            for v in bm.verts:
                v.co.y += wave(v.co.x, v.co.z)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        BK._uv_frame(pc, Vector((1, 0, 0)), Vector((0, 0, -1)), Vector((0, 1, 0)), k.uv_off())
        return k.add(slot, pc)

    def ring(k, slot, centre, a1, a2, R, r, sides=6, segs=16, phase=0.0):
        """A closed torus of tube radius r round `centre` in the plane of the axes a1, a2 (a
        tie, a cord round a post). No caps: the tube closes on itself. V runs round the ring."""
        pc = BK.Piece()
        bm, uvl = pc.bm, pc.uv
        c = Vector(centre)
        a1, a2 = Vector(a1).normalized(), Vector(a2).normalized()
        nrm = a1.cross(a2).normalized()
        V = []
        for i in range(segs):
            t = math.tau * i / segs
            d = a1 * math.cos(t) + a2 * math.sin(t)
            V.append([bm.verts.new(c + d * (R + r * math.cos(phase + math.tau * j / sides))
                                   + nrm * (r * math.sin(phase + math.tau * j / sides))) for j in range(sides)])
        s = 1.0 / UV_METRES
        ou, ov = k.uv_off()
        arc_v, arc_u = math.tau * R / segs, math.tau * r / sides
        for i in range(segs):
            for j in range(sides):
                i2, j2 = (i + 1) % segs, (j + 1) % sides
                f = bm.faces.new((V[i][j], V[i][j2], V[i2][j2], V[i2][j]))
                for loop, (a, b) in zip(f.loops, ((i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j))):
                    loop[uvl].uv = (b * arc_u * s + ou, a * arc_v * s + ov)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return k.add(slot, pc)

    def sag_curve(a, b, sag, n=16):
        """Points from a to b hanging `sag` below the chord at its middle (a parabola is a
        catenary at these sags)."""
        a, b = Vector(a), Vector(b)
        return [a.lerp(b, t) - Z * (4 * sag * t * (1 - t)) for t in (i / n for i in range(n + 1))]

    def drape(k, slot, x0, x1, top, Df, Db, ra, thick, amp=(0.004, 0.08), lam=0.8, du=0.08, flare=(0.18, 0.12),
              hem=0.1, ends=0.0):
        """A soft sheet thrown over a horizontal bar along X (a net over a pole, a towel over a
        line): its middle wraps over the bar at radius `ra` round the bar's axis (`top(x)` its
        height) and it hangs Df in front (+Y) and Db behind. A FEW BIG FOLDS: one long wave along
        X (`lam`, 0.8 m for a net) with a weaker second, pushed along the sheet's normal and
        growing from amp[0] at the bar to amp[1] at the hem. The hem wanders by `hem`, each side
        flares out toward its hem by `flare`, and `ends` shortens and rounds the side edges (the
        corners of a thrown net fall short). Returns the front hem, one point per column."""
        nu = max(3, int(math.ceil((x1 - x0) / du)) + 1)
        ph1, ph2, ph3 = (k.rng.uniform(0, math.tau) for _ in range(3))
        P, UV, hem_pts = [], [], []
        na, nf, nb = 6, 8, 7
        for i in range(nu):
            x = x0 + (x1 - x0) * i / (nu - 1)
            e = (1.0 - min(1.0, min(x - x0, x1 - x) / 0.3)) ** 2 * ends
            zc = top(x)
            df = (Df + hem * math.sin(math.tau * x / (lam * 1.3) + ph2)) * (1 - 0.3 * e)
            db = (Db + hem * math.sin(math.tau * x / (lam * 1.1) + ph3)) * (1 - 0.3 * e)
            path = []                         # (y, z, t): back hem up to the bar, over, front hem
            for j in range(nb, 0, -1):
                t = j / nb
                path.append((-ra - flare[1] * t * t, zc - db * t, t))
            for j in range(na + 1):
                al = math.pi * (1 - j / na)
                path.append((ra * math.cos(al), zc + ra * math.sin(al), 0.0))
            for j in range(1, nf + 1):
                t = j / nf
                path.append((ra + flare[0] * t * t, zc - df * t, t))
            col, uvc, sacc = [], [], 0.0
            for j, (y, z, t) in enumerate(path):
                ya, za, _ = path[max(j - 1, 0)]
                yb, zb, _ = path[min(j + 1, len(path) - 1)]
                ty, tz = yb - ya, zb - za
                ln = math.hypot(ty, tz) or 1.0
                ny, nz = tz / ln, -ty / ln
                a = amp[0] + (amp[1] - amp[0]) * t
                w = a * (math.sin(math.tau * x / lam + ph1) + 0.3 * math.sin(math.tau * x / (lam * 0.45) + ph2))
                if j:
                    sacc += math.hypot(y - path[j - 1][0], z - path[j - 1][1])
                col.append(Vector((x, y + ny * w, z + nz * w)))
                uvc.append((x, sacc))
            P.append(col)
            UV.append(uvc)
            hem_pts.append(col[-1])
        slab(k, slot, P, UV, thick)
        return hem_pts

    def lumpy_roll(k, pts, fat, sides=9, lump=0.45):
        """The net's slack gathered into ONE soft lumpy roll: fat in the middle, eased at the
        ends, its girth wandering in long lumps (`lump` metres). Many layers of net: the painted
        pb_net suggests the mesh, the shape stays one soft mass."""
        ph1, ph2 = k.rng.uniform(0, math.tau), k.rng.uniform(0, math.tau)

        def rad(s, L):
            b = math.sin(math.pi * min(1.0, 0.06 + s / L * 0.94)) ** 0.5
            w = 1 + 0.14 * math.sin(math.tau * s / lump + ph1) + 0.06 * math.sin(math.tau * s / (lump * 0.4) + ph2)
            r = fat * (0.5 + 0.5 * b) * w
            return (r, r * 0.82)
        return BK.sweep(k, "net", BK._catmull(pts, per=4), rad, sides=sides, step=0.06, dome=True, up=Z)

    def float_on(k, centre, axis, kind):
        """One chunky float threaded on its line along `axis`: a fat barrel (24 cm by 16 cm
        across, bulging) or a squat foam block (20 cm, rounded hard by its bevel), its middle ON
        the line so the line runs into both ends."""
        c, d = Vector(centre), Vector(axis).normalized()
        if kind == "barrel":
            L, r = 0.24, 0.08
            return BK.sweep(k, "float", [c - d * (L / 2), c + d * (L / 2)],
                            lambda s, Lt: (r * (0.78 + 0.22 * math.sin(math.pi * s / Lt)),) * 2,
                            sides=10, step=0.04, dome=True)
        L = 0.2
        return BK.sweep(k, "float", [c - d * (L / 2), c + d * (L / 2)],
                        lambda s, Lt: (0.1 * (0.9 + 0.1 * math.sin(math.pi * s / Lt)), 0.07), sides=8, step=0.05,
                        dome=True, up=Z)

    def float_line(k, hem, rr=0.021):
        """The float line along a hem (a fat cord through the hem's points, so it runs in the
        sheet's edge) and three or four chunky floats threaded on it."""
        # ⚠️ the line runs 9 cm past each end of the hem and droops (a loose tail): ending it AT
        # the hem put its end cap against the sheet's side rim, parallel and within 4 mm.
        pts = [p.copy() for p in hem]
        for end, nxt in ((0, 1), (-1, -2)):
            d = (pts[end] - pts[nxt]).normalized()
            tail = pts[end] + d * 0.09 - Z * 0.05
            if end == 0:
                pts.insert(0, tail)
            else:
                pts.append(tail)
        # straight through the hem's own points (a smoothed curve drifted 3 mm off the sheet's
        # faceted hem and brought a rope facet within 4 mm of a sheet face on two seeds)
        BK.sweep(k, "rope", pts, rr, sides=6, step=0.1)
        pts = pts[1:-1]
        n = len(pts)
        count = max(3, min(4, int((pts[-1] - pts[0]).length / 0.55)))
        style = ("barrel", "block")[k.seed // 2 % 2]
        for f in range(count):
            i = min(max(int(round((f + 0.5 + k.rng.uniform(-0.15, 0.15)) / count * (n - 1))), 1), n - 2)
            float_on(k, pts[i], pts[i + 1] - pts[i - 1], style)

    # ------------------------------------------------------------ woven_mat

    MAT_T = 0.024       # the mat's thickness: chunky, a sleeping mat you can see from the court

    def _laid_mat(k, W, L, curl_r, curl_a, y0):
        """The laid-out banig: W across (X), its flat part L long from y0 toward -Y, then the
        far (-Y) end curling up and back over itself by `curl_a` radians at radius `curl_r` (a
        mat that spent the day rolled does not lie flat at its ends). Its underside sinks 4 mm
        into the ground; a slow ripple and the near corners lift it off in places."""
        nu, ns_flat = 7, 10
        ns_curl = max(6, int(curl_a / 0.4))
        ph = k.rng.uniform(0, math.tau)
        P, UV = [], []
        ew = [k.rng.uniform(-1, 1) for _ in range(4)]
        for i in range(nu):
            col, uvc = [], []
            for j in range(ns_flat + ns_curl + 1):
                # organic edges: the side edges wander a couple of centimetres and pull in at the
                # near corners, so the mat is a hand-woven thing and not a ruled rectangle
                sj = j / (ns_flat + ns_curl)
                edge = 2 * i / (nu - 1) - 1
                x = edge * (W / 2 - 0.02 * max(0.0, 1 - sj / 0.08)) + abs(edge) ** 3 * 0.015 * (
                    ew[0] * math.sin(math.pi * sj * 1.3 + ew[1]) + ew[2] * math.sin(math.pi * sj * 2.7 + ew[3]))
                if j <= ns_flat:
                    s = L * j / ns_flat
                    y = y0 - s
                    z = 0.004 * (1 + math.sin(math.tau * s / 1.1 + ph + 0.6 * x))
                    z += 0.02 * max(0.0, 1 - s / 0.25) * (abs(x) / (W / 2)) ** 3
                else:
                    a = curl_a * (j - ns_flat) / ns_curl
                    s = L + curl_r * a
                    y = y0 - L - curl_r * math.sin(a)
                    z = curl_r * (1 - math.cos(a))
                col.append(Vector((x, y, z + MAT_T / 2 - 0.004)))
                uvc.append((x, s))
            P.append(col)
            UV.append(uvc)
        slab(k, "banig", P, UV, MAT_T)

    def _rolled_mat(k, length, turns, centre, axis_y, tie, lift=0.0):
        """A banig rolled up: an Archimedean spiral of the 2.4 cm strip, two fat turns 3.6 cm
        apart (so the spiral shows at the ends), lofted along the roll's axis; the loose outer
        end lands as a lip near the bottom. U runs along the axis, V along the strip, so the
        mat's bands wrap round the roll as rings, as on a real rolled mat. Its lowest point sits
        6 mm below `lift`."""
        t, a, r0 = MAT_T, 0.036, 0.05
        th_max = turns * math.tau
        lip = k.rng.uniform(-0.4, 0.3)

        def spiral(th, off):
            r = r0 + a * th / math.tau + off
            ang = th + lip - th_max - math.pi / 2 + 0.35
            return r * math.cos(ang), r * math.sin(ang)
        n = int(turns * 18)
        ths = [th_max * i / n for i in range(n + 1)]
        outline = [spiral(th, t / 2) for th in ths] + [spiral(th, -t / 2) for th in reversed(ths)]
        sarc = [0.0]
        for p, q in zip(outline, outline[1:] + outline[:1]):
            sarc.append(sarc[-1] + math.hypot(q[0] - p[0], q[1] - p[1]))
        zmin = min(p[1] for p in outline)
        cx, cy = centre

        def world(u, p):
            if axis_y:
                return Vector((cx + p[0], cy + u, lift + p[1] - zmin - 0.006))
            return Vector((cx + u, cy - p[0], lift + p[1] - zmin - 0.006))
        rings, uvs = [], []
        for u in (-length / 2, -length / 6, length / 6, length / 2):
            sl = 1.0 - 0.04 * (1 - abs(u) / (length / 2))   # the middle slumps a little
            rings.append([world(u, (p[0] * sl, zmin + (p[1] - zmin) * sl)) for p in outline])
            uvs.append([(u, s) for s in sarc])
        k.add("banig", BK.loft(rings, uvs, off=k.uv_off()))
        ro = r0 + a * turns + t / 2
        if tie:
            # ONE fat cord round the middle, its ring a touch smaller than the roll so it bites in
            c = world(k.rng.uniform(-0.1, 0.1) * length, (0.0, 0.0))
            a1 = Vector((1, 0, 0)) if axis_y else Vector((0, 1, 0))
            ring(k, "rope", c, a1, Z, ro - 0.006, 0.014, sides=6, segs=20, phase=0.3)

    def build_woven_mat(k):
        W = k.rng.uniform(1.0, 1.15)
        L = k.rng.uniform(1.5, 1.7)
        cr = k.rng.uniform(0.085, 0.11)
        ca = k.rng.uniform(3.6, 4.3)
        y0 = L / 2 + 0.12
        _laid_mat(k, W, L, cr, ca, y0)
        length = k.rng.uniform(1.0, 1.1)
        tie = k.seed % 3 != 0
        if k.seed % 2:
            k.variant = "roll_beside"
            _rolled_mat(k, length, 2.2, (W / 2 + 0.24, y0 - L / 2 - 0.05), True, tie)
        else:
            k.variant = "roll_at_head"
            # across the head, resting ON the mat: its lowest point 1.4 cm up, inside the mat's
            # 2.4 cm and 6 mm or more from both of its faces wherever the ripple puts them
            _rolled_mat(k, min(length, W - 0.05), 2.2, (0.0, y0 - L + 0.34), False, tie, lift=0.02)

    # ------------------------------------------------------------ hanging_net

    def build_net_pole(k):
        """v1 hung a stiff sheet with fine mesh and it read as a blanket; v2 made it an open mesh
        with gathered falls, and the owner found the kit too detailed. Now: ONE soft sheet with
        the net painted on (pb_net), a few big folds, its slack in one lumpy roll along the pole,
        a fat float line and three or four chunky floats."""
        k.variant = "pole"
        S = k.rng.uniform(2.3, 2.8)
        H = k.rng.uniform(1.95, 2.1)
        rp = 0.055
        tops = []
        for sx in (-1, 1):
            base = (sx * S / 2 + k.rng.uniform(-0.03, 0.03), k.rng.uniform(-0.03, 0.03), -0.35)
            path = member(k, "bamboo", base, (sx * S / 2 + sx * k.rng.uniform(0.0, 0.06), 0.0, H + 0.22),
                          0.08, 0.062, bend=0.022, nodes=0.6)
            tops.append(at_z(path, H))
        # the pole BOWS DOWN under the net's weight and never sideways: a sideways bow pushed its
        # middle into the net's fold (v2's one coplanar hit). It rests through both posts.
        a0 = Vector((tops[0].x - 0.3, 0.0, H))
        a1 = Vector((tops[1].x + 0.26, 0.0, H - 0.02))
        pole = sag_curve(a0, a1, k.rng.uniform(0.025, 0.04), 10)
        BK.sweep(k, "bamboo", pole, lambda s_, L_: ((rp * (1 - 0.12 * s_ / L_)),) * 2, sides=8, step=0.12,
                 nodes=0.7, dome=True, phase=0.2)

        def pole_z(x):
            for p, q in zip(pole, pole[1:]):
                if p.x <= x <= q.x:
                    return p.z + (q.z - p.z) * (x - p.x) / (q.x - p.x)
            return pole[0].z if x < pole[0].x else pole[-1].z
        # one fat lashing at each joint, round both members on the diagonal
        for c, sx in zip(tops, (-1, 1)):
            cc = Vector((c.x, 0.0, pole_z(c.x)))
            ring(k, "rope", cc, Vector((0, 1, 0)), Vector((sx, 0, 1)), 0.075, 0.02, sides=6, segs=14)
        span = S - 0.4
        cover = k.rng.uniform(0.78, 0.92) * span
        x0 = -span / 2 + k.rng.uniform(0, span - cover)
        x1 = x0 + cover
        top = pole_z
        Df = H - k.rng.uniform(0.4, 0.6)
        Db = Df * k.rng.uniform(0.6, 0.8)
        # ⚠️ the sheet's fold rides 2.5 cm clear of the pole's surface, inside the gathered roll
        # (which hides it and joins it to the pole): at the pole's own radius its faces would lie
        # within 4 mm of the pole's and z-fight wherever the roll is thin.
        hem = drape(k, "net", x0, x1, top, Df, Db, rp + 0.035, 0.02, amp=(0.006, 0.09), lam=0.8, du=0.08,
                    flare=(0.2, 0.12), hem=0.11, ends=1.0)
        n = max(4, int(cover / 0.2))
        pts = [(x0 - 0.04 + (cover + 0.08) * i / n, 0.012 * math.sin(i * 1.7),
                top(x0 + cover * i / n) + 0.045) for i in range(n + 1)]
        lumpy_roll(k, pts, k.rng.uniform(0.15, 0.17), lump=0.55)
        float_line(k, hem)

    def build_net_wall(k):
        """The same soft net pinned to three chunky pegs on a wall: one sheet swagging between the
        pegs and bunched OUT where it is pinned (the bunching is the sheet's own shape, not
        separate gathered falls), its float line along the bottom."""
        k.variant = "wall"
        k.mount = "wall"
        S = k.rng.uniform(1.5, 1.8)
        H = k.rng.uniform(1.85, 2.0)
        pegs = [-S / 2 + S * i / 2 + k.rng.uniform(-0.05, 0.05) for i in range(3)]
        for x in pegs:
            # a chunky peg driven into the wall, tipped up so the net cannot slide off
            member(k, "timber", (x, -0.05, H - 0.02), (x + k.rng.uniform(-0.02, 0.02), 0.27, H + 0.05),
                   0.046, 0.036, bend=0.04, sides=7)
        sag = k.rng.uniform(0.16, 0.24)

        def top_z(x):
            for a, b in zip(pegs, pegs[1:]):
                if a <= x <= b:
                    return H + 0.045 - sag * math.sin(math.pi * (x - a) / (b - a)) ** 0.8
            return H + 0.045 - 0.35 * min(abs(x - pegs[0]), abs(x - pegs[-1]))
        x0, x1 = pegs[0] - 0.18, pegs[-1] + 0.18
        bottom = k.rng.uniform(0.35, 0.5)
        ph = k.rng.uniform(0, math.tau)
        nu = int((x1 - x0) / 0.07) + 1
        nv = 10
        P, UV, hem = [], [], []
        for i in range(nu):
            x = x0 + (x1 - x0) * i / (nu - 1)
            zt = top_z(x)
            zb = bottom + 0.08 * math.sin(math.tau * x / 0.9 + ph)
            e = 1.0 - min(1.0, min(x - x0, x1 - x) / 0.25)
            zb += 0.15 * e * e                          # the corners fall short
            bunch = sum(math.exp(-((x - p) / 0.15) ** 2) for p in pegs)
            col, uvc = [], []
            for j in range(nv + 1):
                t = j / nv
                z = zt + (zb - zt) * t
                y = (0.09 + 0.08 * t * t + 0.07 * bunch * (1 - 0.5 * t)
                     + 0.025 * t * math.sin(math.tau * x / 0.8 + ph * 2))
                col.append(Vector((x, y, z)))
                uvc.append((x, zt - z))
            P.append(col)
            UV.append(uvc)
            hem.append(col[-1])
        slab(k, "net", P, UV, 0.02)
        float_line(k, hem)

    def build_hanging_net(k):
        (build_net_pole if k.seed % 2 else build_net_wall)(k)

    # ------------------------------------------------------------ laundry_line

    def build_laundry_line(k):
        S = k.rng.uniform(3.0, 3.4)
        Hp = k.rng.uniform(2.05, 2.2)
        lr = 0.013
        za = Hp - 0.14
        posts = []
        for sx in (-1, 1):
            lean = sx * k.rng.uniform(0.03, 0.08)
            base = Vector((sx * S / 2, k.rng.uniform(-0.03, 0.03), -0.35))
            tip = Vector((sx * S / 2 + lean, 0.0, Hp))
            path = member(k, "bamboo", base, tip, 0.078, 0.054, bend=0.022, nodes=0.6)
            p = at_z(path, za)
            posts.append(Vector((p.x, 0.0, za)))
            # the cord's tie: one fat ring round the post where it really is, biting into it
            r_here = 0.054 + 0.024 * (1 - (za + 0.35) / (Hp + 0.35))
            ring(k, "rope", p, Vector((1, 0, 0)), Vector((0, 1, 0)), r_here + 0.008, 0.018, sides=6, segs=14)
        a, b = posts
        m = None
        if k.seed % 2 == 0:
            k.variant = "tukod"
            xm = k.rng.uniform(-0.25, 0.25)
            m = Vector((xm, 0.0, za - 0.03))
            line = sag_curve(a, m, 0.05, 10) + sag_curve(m, b, 0.05, 10)[1:]
            # the forked prop: a chunky pole leaning in from the front, its foot 0.3 m in the
            # ground, the line resting in its fork (two fat stubs)
            foot = Vector((xm + 0.3, 0.5, -0.3))
            crotch = m - Z * (lr + 0.008)
            member(k, "bamboo", foot, crotch, 0.045, 0.036, bend=0.02, nodes=0.7)
            d = (crotch - foot).normalized()
            for sy in (-1, 1):
                tip = crotch + d * 0.04 + Vector((0, sy * 0.07, 0.1))
                member(k, "bamboo", crotch - d * 0.07, tip, 0.03, 0.024, bend=0.05, sides=7)
        else:
            k.variant = "plain"
            line = sag_curve(a, b, k.rng.uniform(0.1, 0.15), 20)
        BK.sweep(k, "rope", line, lr, sides=6, step=0.12)

        def line_z(x):
            for p, q in zip(line, line[1:]):
                if p.x <= x <= q.x:
                    return p.z + (q.z - p.z) * (x - p.x) / (q.x - p.x)
            return line[0].z if x < line[0].x else line[-1].z
        # THREE BIG PIECES (owner: few big readable shapes; v1 had up to six and clothes pegs):
        # one of the two long drapes, one cut piece, and a towel, in a seeded order.
        widths = {"towel": (0.62, 0.74), "malong": (0.8, 0.92), "shirt": (1.0, 1.0), "shorts": (0.56, 0.56)}
        kinds = [k.rng.choice(["malong", "towel"]), k.rng.choice(["shirt", "shorts"]), "towel"]
        if kinds[0] == "towel":
            kinds[2] = "malong"
        k.rng.shuffle(kinds)
        ws = [k.rng.uniform(*widths[n]) for n in kinds]
        free = (b.x - a.x) - 0.5 - sum(ws)
        if m is not None:
            free -= 0.24
        gaps = [max(0.05, free / 2 * k.rng.uniform(0.6, 1.4)) for _ in range(2)]
        x = a.x + 0.25
        for i, (kind, w) in enumerate(zip(kinds, ws)):
            if m is not None and x < m.x + 0.12 and x + w > m.x - 0.12:
                x = m.x + 0.12                           # clear of the fork
            if x + w > b.x - 0.2:
                continue
            _washing(k, kind, x, x + w, line_z, lr)
            x += w + (gaps[i] if i < 2 else 0)
        k.variant += "_" + "_".join(kinds)

    def _washing(k, kind, x0, x1, line_z, lr):
        if kind in ("towel", "malong"):
            Df = {"towel": 0.62, "malong": 0.88}[kind] * k.rng.uniform(0.92, 1.08)
            Db = Df * k.rng.uniform(0.55, 0.85)
            # ⚠️ the fold's mid-surface rides ON the cord's surface (ra = the cord's radius), so the
            # cloth's faces stand 1 cm either side of it: never tangent to it (a face within 4 mm
            # of the cord and parallel to it z-fights), and the cord runs through the fold.
            drape(k, "cloth", x0, x1, line_z, Df, Db, lr, 0.02, amp=(0.001, 0.035), lam=0.55, du=0.07,
                  flare=(0.03, 0.02), hem=0.03)
            return
        xc = (x0 + x1) / 2
        zt = line_z(xc) + lr + 0.012
        if kind == "shirt":
            h, sl, L = 0.3, 0.2, 0.72
            pts = [(-h - sl, 0.0), (-0.1, 0.0), (-0.06, -0.06), (0.06, -0.06), (0.1, 0.0), (h + sl, 0.0),
                   (h + sl + 0.03, -0.2), (h, -0.25), (h + 0.02, -L), (-h - 0.02, -L), (-h, -0.25),
                   (-h - sl - 0.03, -0.2)]
        else:  # shorts, hung by the waistband
            h, L = 0.26, 0.5
            pts = [(-h, 0.0), (h, 0.0), (h + 0.03, -L), (0.04, -L + 0.03), (0.0, -0.24), (-0.04, -L + 0.03),
                   (-h - 0.03, -L)]
        # ⚠️ 3.6 cm thick against a 2.6 cm cord through its top edge: the cloth's faces stand
        # 5 mm clear of the cord's side facets, and the fold wave is zero along the top edge so
        # it never pushes a face back toward the cord.
        thick = 0.036
        ph = k.rng.uniform(0, math.tau)
        tilt = k.rng.uniform(-0.04, 0.04)

        def wave(x, z):
            d = max(0.0, -z / L)
            return 0.03 * d * math.sin(math.tau * x / 0.5 + ph) + tilt * d
        pts = [(xc + x, zt + z + (line_z(xc + x) - line_z(xc) if z > -0.05 else 0.0)) for x, z in pts]
        outline_slab(k, "cloth", pts, thick, wave=lambda x, z: wave(x - xc, z - zt))

    BUILDERS = {"woven_mat": build_woven_mat, "hanging_net": build_hanging_net, "laundry_line": build_laundry_line}

    # ------------------------------------------------------------ assembly

    def material(slot):
        """The slot's material, looked up by NAME first (a kit-shared material is used exactly as
        the file has it), else built around its texture."""
        name, tex = SLOTS[slot]
        m = bpy.data.materials.get(name)
        if m is not None:
            return m
        m = bpy.data.materials.new(name)
        if slot == "cloth":
            LPM._cloth(m)                      # the cloth texture x the per-piece "cloth_tint"
            return m
        RP.uv_material(m, tex)
        nt = m.node_tree
        disp = next((n for n in nt.nodes if n.bl_idname == "ShaderNodeDisplacement"), None)
        if disp is not None and tex in BUMP:
            disp.inputs["Scale"].default_value = BUMP[tex]
        if slot == "float":
            # the float's colour is the FLOAT's, not the material's: albedo x "float_tint"
            bsdf = next(n for n in nt.nodes if n.bl_idname == "ShaderNodeBsdfPrincipled")
            img = next(n for n in nt.nodes if n.bl_idname == "ShaderNodeTexImage"
                       and n.image.name.startswith(f"{tex}_albedo"))
            at = nt.nodes.new("ShaderNodeAttribute")
            at.attribute_type, at.attribute_name = "GEOMETRY", "float_tint"
            mix = nt.nodes.new("ShaderNodeMix")
            mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
            mix.inputs["Factor"].default_value = 1.0
            nt.links.new(img.outputs["Color"], mix.inputs["A"])
            nt.links.new(at.outputs["Color"], mix.inputs["B"])
            nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
            bsdf.inputs["Roughness"].default_value = 0.6
        return m

    def _tint_floats(me, seed):
        """Write "float_tint" per float (per connected piece): a seed's floats are mostly one
        colour with the odd one of a second, the way a fisherman's floats come in batches."""
        piece = LPM._islands(me)
        n = max(piece) + 1 if piece else 0
        rng = random.Random(f"floats:{me.name}:{seed}")
        pair = rng.sample(list(FLOAT_TINTS), 2)
        cols = [FLOAT_TINTS[pair[0] if rng.random() < 0.7 else pair[1]] for _ in range(n)]
        attr = me.color_attributes.new("float_tint", "BYTE_COLOR", "CORNER")
        flat = []
        for p, kk in zip(me.polygons, piece):
            flat.extend((tuple(_lin_to_srgb(c) for c in cols[kk]) + (1.0,)) * p.loop_total)
        attr.data.foreach_set("color_srgb", flat)          # a byte colour stores sRGB

    def build_prop(kind, seed=1):
        """Build one prop of `kind` (see KINDS) from `seed` and return its Collection, NOT linked
        to any scene: every part parented to one root empty named `kind` at the origin (the ground
        contact centre; for a wall net, on the ground at the wall face), +Y the front."""
        if kind not in BUILDERS:
            raise ValueError(f"unknown textile prop {kind!r}; one of {KINDS}")
        k = Kit(kind, seed)
        BUILDERS[kind](k)
        vs = [v.co for pcs in k.pieces.values() for pc in pcs for v in pc.bm.verts]
        lo = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
        hi = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
        # recentre on the footprint: x always, y too unless the back is a wall
        shift = Vector((-(lo.x + hi.x) / 2, 0.0 if k.mount == "wall" else -(lo.y + hi.y) / 2, 0.0))
        for pcs in k.pieces.values():
            for pc in pcs:
                bmesh.ops.translate(pc.bm, vec=shift, verts=pc.bm.verts)
        col = bpy.data.collections.new(f"prop_{kind}_{seed}")
        root = bpy.data.objects.new(kind, None)
        root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.5
        col.objects.link(root)
        root["prop_kind"], root["prop_seed"], root["prop_mount"] = kind, seed, k.mount
        root["prop_variant"] = k.variant
        root["prop_size"] = [round(hi.x - lo.x, 3), round(hi.y - lo.y, 3), round(hi.z, 3)]
        for slot, pcs in k.pieces.items():
            bm = bmesh.new()
            uv = bm.loops.layers.uv.new("UVMap")
            for pc in pcs:
                BK._append(bm, uv, pc)
                pc.bm.free()
            bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
            bm.normal_update()
            fin = FINISH[slot]
            lim = math.radians(fin[1] if fin else 60.0)
            for f in bm.faces:
                f.smooth = True
            for e in bm.edges:
                e.smooth = fin is None or not (len(e.link_faces) == 2
                                               and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
            me = bpy.data.meshes.new(f"{kind}_{seed}_{slot}")
            bm.to_mesh(me)
            bm.free()
            me.materials.append(material(slot))
            if slot == "cloth":
                LPM.tint_cloth(me, seed)
            if slot == "float":
                _tint_floats(me, seed)
            ob = bpy.data.objects.new(me.name, me)
            ob.parent = root
            col.objects.link(ob)
            if fin:
                bev = ob.modifiers.new("Bevel", "BEVEL")
                bev.width, bev.segments, bev.limit_method = fin[0], 2, "ANGLE"
                bev.angle_limit = math.radians(fin[1])
                bev.harden_normals = fin[2]
                bev.use_clamp_overlap = True
        return col

    # ------------------------------------------------------------ checks

    def check_prop(col):
        """Tri counts (base and with the bevels), non-manifold edges per slot, COPLANAR overlaps
        (parallel faces of different shells closer than 4 mm over each other's interior:
        z-fighting) and FLOATING shells (in no chain of intersections to the mount: the ground
        z <= 0.01, or for a wall net the wall y <= 0.005)."""
        root = next(o for o in col.objects if o.parent is None)
        wall = root.get("prop_mount") == "wall"
        V, P, shell, slot_of = [], [], [], []
        anchored_s = set()
        out = {"tris": 0, "tris_bevelled": 0, "nonmanifold": {}}
        sid = 0
        dg = bpy.context.evaluated_depsgraph_get()
        dg.update()
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
                P.append([base_v + i for i in p.vertices])
                shell.append(roots[r])
                slot_of.append(slot)
            for v in me.vertices:
                if v.co.z <= 0.01 or (wall and v.co.y <= 0.005):
                    anchored_s.add(roots[find(v.index)])
        out["shells"] = sid
        tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
        normals = []
        for p in P:
            a, c, d = V[p[0]], V[p[1]], V[p[2]]
            n = (c - a).cross(d - a)
            normals.append(n.normalized() if n.length > 1e-12 else Vector())
        where = {}
        for i, p in enumerate(P):
            n = normals[i]
            if n.length < 0.5:
                continue
            c = sum((V[kk] for kk in p), Vector()) / len(p)
            for q in [c] + [c.lerp(V[kk], 0.7) for kk in p]:
                for _loc, nrm, idx, _dist in tree.find_nearest_range(q, 0.004):
                    if idx is not None and shell[idx] != shell[i] and abs(nrm.dot(n)) > 0.999:
                        key = (min(shell[i], shell[idx]), max(shell[i], shell[idx]))
                        where.setdefault(key, (slot_of[i], slot_of[idx], tuple(round(x, 2) for x in c)))
        out["coplanar"] = len(where)
        out["coplanar_examples"] = sorted(where.values())[:6]
        par = list(range(sid))

        def f2(a):
            while par[a] != a:
                par[a] = par[par[a]]
                a = par[a]
            return a
        for a, c in tree.overlap(tree):
            sa, sb = shell[a], shell[c]
            if sa != sb:
                ra, rb = f2(sa), f2(sb)
                if ra != rb:
                    par[ra] = rb
        anchored = {f2(s) for s in anchored_s}
        loose = {s for s in range(sid) if f2(s) not in anchored}
        out["floating"] = len(loose)
        out["floating_examples"] = sorted({slot_of[i] for i, s in enumerate(shell) if s in loose})[:6]
        xs, ys, zs = [v.x for v in V], [v.y for v in V], [v.z for v in V]
        out["bbox"] = (round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(min(zs), 2), round(max(zs), 2))
        return out

    # ------------------------------------------------------------ preview

    def _plain_mat(name, colour, rough=0.85):
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = colour + (1,)
        b.inputs["Roughness"].default_value = rough
        return m

    def _preview_scene():
        scene = bpy.context.scene
        me = bpy.data.meshes.new("sand")
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=40)
        uvl = bm.loops.layers.uv.new("UVMap")
        for f in bm.faces:
            for loop in f.loops:
                loop[uvl].uv = (loop.vert.co.x / UV_METRES, loop.vert.co.y / UV_METRES)
        bm.to_mesh(me)
        bm.free()
        if (APPROVED / "sand_a_albedo.png").exists():
            sand = bpy.data.materials.new("sand")
            RP.uv_material(sand, "sand_a")
        else:
            sand = _plain_mat("sand_flat", (0.86, 0.72, 0.48))
        me.materials.append(sand)
        scene.collection.objects.link(bpy.data.objects.new("sand", me))
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
        sun.data.energy, sun.data.color = 4.5, (1.0, 0.9, 0.74)
        sun.data.angle = math.radians(3)
        sun.rotation_euler = (math.radians(48), 0, math.radians(150))   # from the camera side (+Y)
        scene.collection.objects.link(sun)
        world = bpy.data.worlds.new("world")
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.6, 0.66, 1)
        world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
        scene.world = world
        cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
        cam.data.clip_end = 500
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

    def _scale_ref(col, at):
        me = bpy.data.meshes.new("scale_ref_1m60")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
        bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
        bm.to_mesh(me)
        bm.free()
        me.materials.append(_plain_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
        o = bpy.data.objects.new("scale_ref_1m60", me)
        o.location = at
        col.objects.link(o)

    # (kind, seed, x, y, yaw degrees). Fronts toward +Y, where the cameras stand (so +X is on the
    # LEFT of every shot). The wall net hangs on a display wall standing in for a house wall.
    WALL_Y = -1.6
    WALL_X = 6.3
    LINEUP = [("laundry_line", 1, -6.2, -1.0, 0.0), ("laundry_line", 2, -1.9, -1.0, 0.0),
              ("hanging_net", 1, 2.3, -1.0, 0.0), ("hanging_net", 2, WALL_X, WALL_Y, 0.0),
              ("woven_mat", 1, -2.9, 2.6, 0.0), ("woven_mat", 2, -0.4, 2.6, 0.0)]
    REFS = [(4.3, 0.3, 0.0), (-8.6, -0.6, 0.0)]
    SHOTS = [("lineup", (0.0, 11.5, 3.4), (0.0, -0.4, 0.8), 24),
             ("close", (-1.6, 6.4, 2.3), (-1.7, 2.3, 0.2), 30),
             ("closeb", (4.3, 4.6, 1.8), (4.3, -1.3, 1.15), 26),
             ("closec", (-4.0, 4.2, 1.7), (-4.0, -1.0, 1.35), 26),
             ("far", (0.0, 19.0, 1.6), (0.0, -0.4, 1.0), 35)]

    def main():
        argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
        version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
        missing = [n for n in PAINTERS if not (TEX / f"{n}_albedo.png").exists()]
        if missing:
            subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--paint", ",".join(missing)], check=True)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        scene = bpy.context.scene
        top = bpy.data.collections.new("props_textile_lineup")
        scene.collection.children.link(top)
        tot = {"coplanar": 0, "floating": 0, "nonmanifold": 0}
        for kind, seed, x, y, yaw in LINEUP:
            col = build_prop(kind, seed)
            top.children.link(col)
            c = check_prop(col)
            root = next(o for o in col.objects if o.parent is None)
            print(f"[textile] {kind} {seed} ({root['prop_variant']}, {root['prop_mount']}): size "
                  f"{list(root['prop_size'])}, tris {c['tris']} ({c['tris_bevelled']} bevelled), shells {c['shells']}, "
                  f"coplanar {c['coplanar']} {c['coplanar_examples']}, floating {c['floating']} "
                  f"{c['floating_examples']}, nonmanifold {c['nonmanifold']}, bbox {c['bbox']}")
            tot["coplanar"] += c["coplanar"]
            tot["floating"] += c["floating"]
            tot["nonmanifold"] += sum(c["nonmanifold"].values())
            root.location = (x, y, 0.0)
            root.rotation_euler = (0, 0, math.radians(yaw))
        print("[textile] totals", tot)
        wall = bpy.data.meshes.new("display_wall")
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        bmesh.ops.scale(bm, vec=(3.2, 0.3, 3.0), verts=bm.verts)
        bmesh.ops.translate(bm, vec=(WALL_X, WALL_Y - 0.15, 1.5), verts=bm.verts)
        bm.to_mesh(wall)
        bm.free()
        wall.materials.append(_plain_mat("display_wall", (0.78, 0.72, 0.62)))
        top.objects.link(bpy.data.objects.new("display_wall", wall))
        for at in REFS:
            _scale_ref(top, at)
        SOURCE.mkdir(parents=True, exist_ok=True)
        out = SOURCE / "lagoon_props_textile.blend"
        bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
        bk = out.with_suffix(".blend1")
        if bk.exists():
            bk.unlink()
        print("[textile] saved", out)
        if not version:
            return
        cam = _preview_scene()
        LOGS.mkdir(parents=True, exist_ok=True)
        for tag, pos, tgt, lens in SHOTS:
            path = LOGS / f"pw_{tag}_v{version}.png"
            if path.exists():
                raise SystemExit(f"[textile] {path} exists: never overwrite a render, bump --preview")
            cam.location = pos
            cam.data.lens = lens
            cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            print("[textile] preview", path)


if __name__ == "__main__":
    if bpy is None:
        args = sys.argv[1:]
        if "--sheet" in args:
            sheet(int(args[args.index("--sheet") + 1]))
        elif "--before-after" in args:
            i = args.index("--before-after")
            before_after(int(args[i + 1]), int(args[i + 2]))
        else:
            i = args.index("--paint") if "--paint" in args else -1
            names = args[i + 1].split(",") if 0 <= i < len(args) - 1 else None
            paint(names)
    else:
        main()
