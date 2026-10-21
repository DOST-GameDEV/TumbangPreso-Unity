"""Lagoon Court SEATING PROPS: somewhere to sit in the fishing village (models only; the shared
approved textures, no new ones).

  blender -b --python tools/lagoon_prop_seating.py -- --preview N

With --preview N it renders Logs/lagoon-blender/pt_lineup_vN.png (every kind, several seeds, on warm
sand with a 1.6 m pink scale cylinder) and pt_close_vN.png (the tables and the huts up close) and pt_benches_vN.png (the three benches). A render
is never overwritten: bump N. Nothing is saved to a .blend: the map assembly calls build_prop.

WHY (docs/LAGOON_REWORK_GUIDE.md § 7a items 1 and 8). The reference (ArtStation GvJv5a, "Stylized
Fishing Village") has a round table with benches and lean-to shades on posts; the cove had nowhere
anybody would sit. These are the Filipino versions: a plank table on one post with low benches
round it, the bangko (a long plank bench), a split-bamboo bench, the papag (a low bamboo daybed
somebody naps on in the afternoon), and a kubo-style rest hut: a roof on four posts, open sides,
a papag or a bench underneath.

  import lagoon_prop_seating as PT
  col = PT.build_prop(kind, seed)      # a Collection, NOT linked; one root empty named by kind
  report = PT.check_prop(col)

KINDS, every one with its origin at the GROUND CONTACT CENTRE, +Y its front, z = 0 the ground
(legs and posts sink 1.5 to 3 cm into it, so nothing hovers on uneven sand):

  STYLE (owner, 2026-09-27): stylized, semi-cartoony. FEW, FAT, SOFT, slightly irregular parts:
  thick slabs that sag and taper, chunky legs that flare and lean, big rounded bevels, fat
  whole bamboo poles with no modelled node rings (the texture paints them). No joinery.

  * "table_round"  a round table (1.10 to 1.26 m across, 0.74 m high) of three or four THICK
                   boards, each tipped a few millimetres its own way, on one soft batten and one
                   fat post that swells toward the ground, over a fat crossed foot; two or three
                   chunky bangko round it, each nudged off the circle so it reads as hand-placed.
  * "bench"        by seed: seed % 3 == 1 the bangko (ONE thick sagging plank seat on two flared
                   slab legs), 2 a bamboo bench (three fat poles on two fat cross poles and four
                   splayed legs), 0 the papag (a 1.8 to 2.0 m bamboo daybed: EIGHT fat split-
                   bamboo slats on two fat cross poles and four fat legs).
  * "lean_to"      a kubo-style rest hut, 2.2 to 2.6 m by 1.8 to 2.1 m: a FAT soft roof on four
                   chunky splayed posts and two dipping beams, open sides, nothing else.
                   By seed: seed % 3 == 1 a steep thatched GABLE (30 cm thatch, a fat ridge roll)
                   over a papag, 2 a TIN mono-pitch roof on square timber posts over a bangko,
                   0 a THATCHED mono-pitch roof on bamboo posts over a bamboo bench. The eaves
                   are about 1.8 to 2.4 m up, so a 1.6 m player walks under them.

THE ROOT EMPTY carries prop_kind, prop_seed, prop_variant and prop_box (xmin, ymin, zmin, xmax,
ymax, zmax: the local box the prop fills, for the dressing pass to keep clear).

MATERIALS, shared BY NAME with the house, boat and prop kits so one file holds one of each: plank
(plank_c), timber (timber_a), bamboo (bamboo_a), thatch (thatch_a) and tin (tin_b). When one of
those already exists in the file it is used as it is; otherwise it is built by
render_lagoon_texture_preview.uv_material around the texture in brackets. Every surface here is one
of those five, so this kit paints NOTHING new (the brief's rule: reuse before painting). tin_b is
the approved rusted red; nothing here comes near offence orange #f87020 or defence blue #0080e8.

UVs: one map "UVMap", WORLD SCALE, 1 unit = 2 m, the house and boat kits' convention: V along
every board, pole and slat (so plank joints, bamboo nodes and the grain run the right way), U
around it; thatch and tin V UP the roof slope (thatch courses lie level along the eave, the tin's
ribs run down the slope). Every piece gets a random UV offset.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2). Every joint PENETRATES: legs 3 cm
into seats, posts 4 cm into beams, the batten 3 cm into the table boards, the roof 4 cm onto its
beams, slats 2 cm into their cross poles. The crossed foot's two halves have different depths and
no wobble, and a leg is always narrower than the seat on it, so no two faces ever lie together. `check_prop` proves it: `coplanar` counts parallel faces of
different shells within 4 mm over each other, `floating` counts shells in no chain of
intersections to the ground.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parent))
# The boat kit's primitives (a closed loft, a swept pole, a box member, the world-scale box
# unwrap): reused rather than rewritten, so every Lagoon prop is built and unwrapped one way.
import author_lagoon_boats as BK                                   # noqa: E402
from render_lagoon_texture_preview import uv_material              # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
PREVIEWS = ROOT / "Logs" / "lagoon-blender"
PREFIX = "pt"

KINDS = ("table_round", "bench", "lean_to")
Z = Vector((0.0, 0.0, 1.0))
X = Vector((1.0, 0.0, 0.0))
Y = Vector((0.0, 1.0, 0.0))

# slot: the approved texture its material is built around when the file has none yet.
TEXTURES = {"plank": "plank_c", "timber": "timber_a", "bamboo": "bamboo_a", "thatch": "thatch_a",
            "tin": "tin_b"}
# Per slot: (bevel width m, angle above which an edge is bevelled, harden normals, segments).
# BIG soft rounds (owner, 2026-09-27: "stylized semi-cartoony"): 2 to 2.5 cm on the wood so a
# member reads as a rounded toy block at 20 m, 7 cm on the thatch so the fat roof looks stuffed,
# 1.5 cm on the 7 cm tin sheet.
FINISH = {
    "plank": (0.020, 30.0, True, 3),
    "timber": (0.025, 30.0, True, 3),
    "bamboo": (0.010, 50.0, False, 2),
    "thatch": (0.070, 30.0, False, 3),
    "tin": (0.015, 30.0, True, 2),
}


class Prop:
    """Collects closed pieces per material slot for one prop. Duck-types the boat kit's Boat
    (rng, add, uv_off), so BK.sweep and BK.board build straight into it."""

    def __init__(self, kind, seed):
        self.kind, self.seed = kind, seed
        self.rng = random.Random(f"lagoon-seating:{kind}:{seed}")
        self.pieces = {s: [] for s in TEXTURES}
        self.info = {}
        self.variant = kind

    def add(self, slot, pc):
        if pc is None or not pc.bm.faces:
            return None
        self.pieces[slot].append(pc)
        return pc

    def uv_off(self):
        return (self.rng.random(), self.rng.random())

    def mark(self):
        return {s: len(v) for s, v in self.pieces.items()}

    def place_since(self, mark, M):
        """Move every piece added since `mark` by M. Sub-assemblies (a bench round the table, the
        papag under a hut) are built at their own origin and placed; a rigid move keeps the
        world-scale UVs true."""
        for s, pcs in self.pieces.items():
            for pc in pcs[mark[s]:]:
                bmesh.ops.transform(pc.bm, matrix=M, verts=pc.bm.verts)


def J(b, a):
    """A small hand-made wobble in [-a, a]."""
    return b.rng.uniform(-a, a)


# ---------------------------------------------------------------- primitives

def slab(b, slot, outline, z0, z1, along=Y, tilt=(0.0, 0.0)):
    """A vertical extrusion of a convex 2D outline [(x, y)] from z0 to z1 (a table board cut to
    the round), tipped by `tilt` (metres of rise per metre along x and y) so neighbouring boards
    never sit dead level with each other. V along `along` (the board's length)."""
    rings = [[Vector((x, y, z + tilt[0] * x + tilt[1] * y)) for x, y in outline] for z in (z0, z1)]
    pc = BK.loft(rings, [[(0, 0)] * (len(outline) + 1)] * 2)
    BK._uv_frame(pc, along.cross(Z).normalized(), along, Z, b.uv_off())
    return b.add(slot, pc)


PLANK_BOARDS = 7      # plank_c paints 7 boards across its tile, joints at u = k / 7 (measured)
# The painted BUTT JOINT of each board, as the image row (0 = top, 1024 tall) where its dark band
# starts, measured off plank_c_albedo.png down each board's centre. Board 2 has two joints only
# 0.76 of a tile apart and is left out; every other board has a clean run of 0.94 tile or more.
PLANK_JOINT_ROW = {0: 213, 1: 452, 3: 1010, 4: 209, 5: 408, 6: 1000}


def fit_board(b, pc, U, c, w):
    """Put ONE painted board of plank_c on a modelled board, with NO painted joint on it. plank_c
    is a floor of 7 boards per 2 m tile, so at plain world scale a table board or a bench seat
    showed a joint line down its middle (v2) and a pair of dark butt joints across it (v6), and
    read as pieced. Across the board (U) the top, bottom and end faces are remapped so its width
    covers the middle 80 % of one painted board; along it (V) the world scale is kept but the
    board starts just past that painted board's joint, so every board up to 1.8 m is one clean
    run. `c` is the board's centre along U in metres, `w` its width."""
    k = (0.8 / PLANK_BOARDS) / w
    board = b.rng.choice(sorted(PLANK_JOINT_ROW))
    ub = (board + 0.5) / PLANK_BOARDS
    pc.bm.normal_update()
    vmin = min(loop[pc.uv].uv[1] for f in pc.bm.faces for loop in f.loops)
    dv = (1.0 - PLANK_JOINT_ROW[board] / 1024 + 0.008) - vmin
    for f in pc.bm.faces:
        side = abs(f.normal.dot(U)) > 0.7     # the long edges keep their world-scale strip
        for loop in f.loops:
            u, v = loop[pc.uv].uv
            if not side:
                u = (loop.vert.co.dot(U) - c) * k + ub
            loop[pc.uv].uv = (u, v + dv)
    return pc


# ---------------------------------------------------------------- organic members
#
# ⚠️ FEW, FAT, SOFT, SLIGHTLY IRREGULAR (owner, 2026-09-27, on the v4 lineup: "i dont like how
# details some of the props are. again we're going for a stylized semi-cartoony environment
# style", and "you should really experiment more with being organic in how you shape things",
# not "just a straight rectangular prism"). v1 to v4 built these like joinery: trestles with
# cross bars and stretchers, 25 split-bamboo slats on a papag, rafters, purlins, knee braces,
# tie beams, king posts, and bamboo node rings modelled as geometry. From v5 every prop is a
# handful of fat, rounded, hand-shaped parts and the painted texture carries the detail (the
# bamboo texture already paints the nodes). Do not add the small parts back.

def _soft_section(w, t, half=False):
    """A cross-section, (across, up) pairs. A fat rounded-off block: an octagon whose corners are
    cut a third of the way in, so even before the Bevel it never reads as a sawn prism. `half`: a
    split-bamboo slat, a flattened half ellipse on a flat base."""
    if half:
        return [(0.5 * w * math.cos(math.pi * k / 6), t * math.sin(math.pi * k / 6)) for k in range(7)]
    a, c = w / 2, t / 2
    ca, cc = a * 0.35, c * 0.35
    return [(a, -c + cc), (a, c - cc), (a - ca, c), (-a + ca, c), (-a, c - cc), (-a, -c + cc),
            (-a + ca, -c), (a - ca, -c)]


def soft_member(b, slot, p0, p1, w, t, taper=(1.0, 1.0), bow=0.0, wobble=0.012, rings=5, up=Z,
                half=False, swell=0.0):
    """A hand-shaped member from p0 to p1: `w` across (horizontal when `up` is Z), `t` along the
    up side. Built from `rings` sections, each scaled from taper[0] at p0 to taper[1] at p1 and
    jittered by `wobble` (a fraction of its size), the middle pushed `bow` metres along the up
    side (a seat that sags, a beam that dips) and swollen by `swell` in the middle (a fat foot),
    so no two members are the same straight prism.
    V along the member, U across it."""
    p0, p1 = Vector(p0), Vector(p1)
    d = p1 - p0
    L = d.length
    d.normalize()
    side = up.cross(d)
    if side.length < 1e-6:
        side = X.cross(d)
    side.normalize()
    upv = d.cross(side)
    rs = []
    for k in range(rings):
        u = k / (rings - 1)
        f = (taper[0] + (taper[1] - taper[0]) * u) * (1 + swell * math.sin(math.pi * u))
        fw = f * (1 + J(b, wobble * 3)) if 0 < k < rings - 1 else f
        ft = f * (1 + J(b, wobble * 3)) if 0 < k < rings - 1 else f
        c = p0 + d * (L * u) + upv * (bow * math.sin(math.pi * u)) + side * (J(b, wobble) * w if 0 < k < rings - 1 else 0)
        rs.append([c + side * a + upv * z for a, z in _soft_section(w * fw, t * ft, half)])
    pc = BK.loft(rs, [[(0, 0)] * (len(rs[0]) + 1)] * rings)
    BK._uv_frame(pc, side, d, upv, b.uv_off())
    b.add(slot, pc)
    return pc, side


def soft_plank(b, p0, p1, w, t, **kw):
    """A soft_member in plank_c, showing ONE painted board across its width (fit_board)."""
    pc, side = soft_member(b, "plank", p0, p1, w, t, **kw)
    return fit_board(b, pc, side, Vector(p0).dot(side), w)


def fat_pole(b, p0, p1, r0, r1=None, mid=None, dome=True):
    """A fat bamboo pole, tapered from r0 to r1, optionally bent through `mid`. NO node rings:
    the bamboo texture paints them."""
    r1 = r0 if r1 is None else r1
    pts = [Vector(p0)] + ([Vector(mid)] if mid is not None else []) + [Vector(p1)]
    return BK.sweep(b, "bamboo", pts, lambda s, L: ((r0 + (r1 - r0) * s / L),) * 2, sides=10, step=0.35,
                    dome=dome, phase=b.rng.uniform(0, math.pi / 5))


def bowed(p0, p1, dz):
    """The midpoint of p0 p1 dropped (or raised) by dz."""
    return (Vector(p0) + Vector(p1)) / 2 + Z * dz


# ---------------------------------------------------------------- seats

def bangko(b, L, h, W):
    """The bangko: ONE thick soft plank seat (9 cm, a slight sag) on two thick slab legs that
    flare toward the ground and lean out a little. Local frame: long along X, centred on the
    origin, z = 0 the ground."""
    t = 0.09 + J(b, 0.008)
    soft_plank(b, (-L / 2, 0.0, h - t / 2), (L / 2, 0.0, h - t / 2), W, t, bow=-0.01,
               taper=(0.94, 0.97 + J(b, 0.02)))
    xl = L / 2 - 0.22 - abs(J(b, 0.03))
    for sx in (-1, 1):
        # the leg is narrower than the seat and runs 3 cm up into it
        head = Vector((sx * xl, J(b, 0.01), h - t + 0.03))
        foot = Vector((sx * (xl + 0.08) + J(b, 0.015), J(b, 0.015), -0.03))
        soft_member(b, "timber", head, foot, W - 0.07, 0.11, taper=(0.9, 1.12), up=X, rings=4)


def bamboo_bench(b, L, h, W):
    """A bamboo bench: three FAT whole poles as the seat, sagging a touch, lying on two fat
    cross poles, on four fat splayed legs."""
    r = 0.058
    n = 3
    pitch = 2 * r + 0.008                  # 8 mm between seat poles: they never touch
    zs = h - r
    for i in range(n):
        y = (i - (n - 1) / 2) * pitch
        p0 = (-L / 2 + J(b, 0.03), y, zs + J(b, 0.004))
        p1 = (L / 2 + J(b, 0.03), y, zs + J(b, 0.004))
        fat_pole(b, p0, p1, r, r * 0.9, mid=bowed(p0, p1, -0.012))
    rc = 0.066
    zc = zs - 0.105                        # centres 10.5 cm apart on radii 5.8 + 6.6: they cut in
    xc = L / 2 - 0.22
    half = (n - 1) / 2 * pitch
    for sx in (-1, 1):
        fat_pole(b, (sx * xc, -half - 0.13, zc), (sx * xc, half + 0.13, zc), rc)
        for sy in (-1, 1):
            y = sy * (half + 0.01)
            fat_pole(b, (sx * (xc + 0.08) + J(b, 0.015), y + sy * 0.03, -0.03), (sx * xc, y, zc), 0.066, 0.056,
                     dome=False)


def papag(b, L, W, h):
    """The papag, a low bamboo daybed: EIGHT fat split-bamboo slats along its length (curved face
    up, each a little bowed and tapered), on two fat cross poles and four fat legs."""
    n = 8
    gap = 0.008
    sw = (W - gap * (n - 1)) / n
    st = 0.05
    zs = h - st
    for i in range(n):
        y = -W / 2 + sw / 2 + i * (sw + gap)
        soft_member(b, "bamboo", (-L / 2 + J(b, 0.03), y, zs + J(b, 0.003)), (L / 2 + J(b, 0.03), y, zs + J(b, 0.003)),
                    sw - abs(J(b, 0.006)), st, bow=0.008, taper=(0.92, 0.95), wobble=0.006, half=True)
    rc = 0.078
    zc = zs - rc + 0.02                    # the slats sit 2 cm down into the cross poles
    xc = L / 2 - 0.24
    for sx in (-1, 1):
        fat_pole(b, (sx * xc, -W / 2 - 0.1, zc), (sx * xc, W / 2 + 0.1, zc), rc)
        for sy in (-1, 1):
            y = sy * (W / 2 - 0.1)
            fat_pole(b, (sx * (xc + 0.06) + J(b, 0.015), y + sy * 0.03, -0.03), (sx * xc, y, zc), 0.078, 0.066,
                     dome=False)


def build_bench(b):
    v = b.seed % 3
    if v == 1:
        b.variant = "bangko"
        L = 1.4 + b.rng.random() * 0.3
        bangko(b, L, 0.45, 0.36 + J(b, 0.02))
    elif v == 2:
        b.variant = "bamboo_bench"
        L = 1.3 + b.rng.random() * 0.3
        bamboo_bench(b, L, 0.45, 0.35)
    else:
        b.variant = "papag"
        L = 1.8 + b.rng.random() * 0.2
        papag(b, L, 0.92 + J(b, 0.04), 0.42)
    b.info.update(length=L)


# ---------------------------------------------------------------- table

def build_table(b):
    """A round table of three or four THICK boards (8 cm, each tipped a few millimetres its own
    way) on one fat post that swells toward the ground, a fat crossed foot, and two or three
    chunky bangko round it."""
    R = 0.55 + b.rng.random() * 0.08
    H = 0.74
    n = b.rng.choice((3, 4))
    gap = 0.012
    pw = (2 * R - 0.03 - gap * (n - 1)) / n
    t = 0.08
    x = -R + 0.015
    for i in range(n):
        x0, x1 = x, x + pw
        pts = []
        steps = 5
        for k in range(steps + 1):
            xx = x0 + (x1 - x0) * k / steps
            pts.append((xx, math.sqrt(max(R * R - xx * xx, 0.0004))))
        for k in range(steps, -1, -1):
            xx = x0 + (x1 - x0) * k / steps
            pts.append((xx, -math.sqrt(max(R * R - xx * xx, 0.0004))))
        pc = slab(b, "plank", pts, H - t - 0.004, H, along=Y, tilt=(J(b, 0.008), J(b, 0.006)))
        fit_board(b, pc, X, (x0 + x1) / 2, x1 - x0)
        x = x1 + gap
    under = H - t - 0.02
    # one soft batten ACROSS every board holds them together, 3 cm up into them (below their
    # lowest tilt); v5 ran it along the boards and the outer ones hung loose
    zc = under + 0.03 - 0.045
    soft_member(b, "timber", (-R + 0.12, 0, zc), (R - 0.12, 0, zc), 0.32, 0.09, rings=4)
    BK.sweep(b, "timber", [Vector((0, 0, 0.05)), Vector((0, 0, zc))],
             lambda s, L: ((0.12 - 0.035 * s / L),) * 2, sides=10, step=0.2, phase=math.radians(9))
    fl = 0.9 + b.rng.random() * 0.1
    # two fat feet crossing, swollen in the middle. Where they cross the first spans z -0.03 to
    # 0.10 and the second -0.02 to 0.08, with no wobble (v7: a wobbled ring put both bottoms
    # in one plane), so no faces lie together
    soft_member(b, "timber", (-fl / 2, 0, 0.035), (fl / 2, 0, 0.035), 0.16, 0.13, taper=(0.75, 0.75),
                swell=0.33, rings=5, wobble=0.0)
    soft_member(b, "timber", (0, -fl / 2, 0.03), (0, fl / 2, 0.03), 0.15, 0.1, taper=(0.75, 0.75),
                swell=0.33, rings=5, wobble=0.0)
    count = 3 if b.rng.random() < 0.5 else 2
    a0 = b.rng.uniform(0, math.tau)
    for i in range(count):
        a = a0 + math.tau * i / count + J(b, 0.12)
        r = R + 0.42 + J(b, 0.04)
        m = b.mark()
        bangko(b, 0.95 + b.rng.random() * 0.2, 0.43, 0.32 + J(b, 0.015))
        M = (Matrix.Translation((r * math.cos(a), r * math.sin(a), 0))
             @ Matrix.Rotation(a + math.pi / 2 + J(b, 0.1), 4, "Z"))
        b.place_since(m, M)
    b.variant = f"{count}_benches"
    b.info.update(diameter=2 * R, height=H, boards=n)


# ---------------------------------------------------------------- lean-to

def roof_slab(b, slot, o, s, n, x0, x1, length, thick, rings=5, droop=0.05):
    """One FAT roof plane: `o` is its underside at the eave at x = 0, `s` the unit direction UP
    the slope, `n` the unit normal out of the roof. Five sections across X; the eave sags between
    the corners and the slab swells a little in the middle, so it reads soft and stuffed, not
    sawn. V up the slope."""
    rs = []
    for k in range(rings):
        x = x0 + (x1 - x0) * k / (rings - 1)
        f = math.sin(math.pi * k / (rings - 1))      # 0 at the corners, 1 in the middle
        sag = droop * f + abs(J(b, droop * 0.3))
        base = o + X * x
        lo = base - n * sag + s * J(b, 0.025)
        hi = base + s * length
        th = thick * (1 + 0.12 * f)
        rs.append([lo, lo + n * th, hi + n * th, hi])
    pc = BK.loft(rs, [[(0, 0)] * 5] * rings)
    BK._uv_frame(pc, X, s, n, b.uv_off())
    return b.add(slot, pc)


def build_lean_to(b):
    """A kubo-style shade: a FAT soft roof on four chunky posts and two beams, and something to
    sit on under it. Nothing else: no rafters, purlins, braces or ties (owner, 2026-09-27)."""
    v = b.seed % 3
    roof = {1: "thatch_gable", 2: "tin_mono", 0: "thatch_mono"}[v]
    b.variant = roof
    W = 2.2 + b.rng.random() * 0.4
    D = 1.8 + b.rng.random() * 0.3
    bamboo = roof != "tin_mono"
    if roof == "thatch_gable":
        Hf = Hb = 2.05 + J(b, 0.04)
    else:
        Hf, Hb = 2.4 + J(b, 0.04), 2.05 + J(b, 0.04)
    heights = {1: Hf, -1: Hb}
    for sy in (-1, 1):
        for sx in (-1, 1):
            top = Vector((sx * W / 2, sy * D / 2, heights[sy]))
            foot = top + Vector((sx * 0.05 + J(b, 0.04), sy * 0.04 + J(b, 0.04), 0)) - Z * (heights[sy] + 0.03)
            if bamboo:
                fat_pole(b, foot, top, 0.115, 0.095, dome=False)
            else:
                soft_member(b, "timber", foot, top, 0.22, 0.22, taper=(1.08, 0.88), up=Y, rings=4)
    # one beam along X on each pair of posts, posts 4 cm up into it; the beam dips a little
    beam_top = {}
    for sy in (-1, 1):
        p0 = (-W / 2 - 0.28, sy * D / 2, 0)
        p1 = (W / 2 + 0.28, sy * D / 2, 0)
        if bamboo:
            zb = heights[sy] + 0.055
            q0, q1 = Vector(p0) + Z * zb, Vector(p1) + Z * zb
            fat_pole(b, q0, q1, 0.095, 0.085, mid=bowed(q0, q1, -0.012))
            beam_top[sy] = zb + 0.09
        else:
            zb = heights[sy] + 0.07
            soft_member(b, "timber", Vector(p0) + Z * zb, Vector(p1) + Z * zb, 0.17, 0.22, bow=-0.012)
            beam_top[sy] = zb + 0.11
    slot = "tin" if roof == "tin_mono" else "thatch"
    thick = 0.07 if slot == "tin" else 0.3        # a thatch roof as fat as a mattress
    x0, x1 = -W / 2 - 0.45, W / 2 + 0.45
    if roof == "thatch_gable":
        pitch = math.radians(40 + J(b, 2))
        e = 0.12                                   # each plane runs 12 cm past the ridge
        for sy in (-1, 1):
            A = Vector((0, sy * D / 2, beam_top[sy] - 0.04))
            C = A + Vector((0, -sy * D / 2, (D / 2) * math.tan(pitch)))
            s = (C - A).normalized()
            n = X.cross(s).normalized()
            n = n if n.z > 0 else -n
            over = 0.58
            roof_slab(b, slot, A - s * over, s, n, x0, x1, (C - A).length + over + e, thick)
        top = beam_top[1] - 0.04 + (D / 2) * math.tan(pitch) + thick / math.cos(pitch)
        # the fat ridge roll hides where the planes cross, sunk 5 cm into them, bowed a touch
        r0, r1 = Vector((x0 - 0.05, 0, top - 0.04)), Vector((x1 + 0.05, 0, top - 0.04))
        BK.sweep(b, "thatch", [r0, bowed(r0, r1, -0.03), r1], (0.26, 0.21), sides=10, step=0.5, dome=True,
                 phase=math.pi / 10)
        b.info.update(ridge=top + 0.17)
    else:
        A = Vector((0, -D / 2, beam_top[-1] - 0.04))
        B = Vector((0, D / 2, beam_top[1] - 0.04))
        s = (B - A).normalized()
        n = X.cross(s).normalized()
        n = n if n.z > 0 else -n
        # the roof falls to the BACK, so the open front is the high, inviting side
        roof_slab(b, slot, A - s * 0.48, s, n, x0, x1, (B - A).length + 0.48 + 0.58, thick,
                  droop=0.05 if slot == "thatch" else 0.02)
    m = b.mark()
    if roof == "thatch_gable":
        papag(b, min(1.75, W - 0.55), min(0.9, D - 0.6), 0.42)
        M = Matrix.Rotation(J(b, 0.05), 4, "Z")
    elif roof == "tin_mono":
        bangko(b, W - 0.7, 0.45, 0.36)
        M = Matrix.Translation((0, -D / 2 + 0.5, 0)) @ Matrix.Rotation(J(b, 0.04), 4, "Z")
    else:
        bamboo_bench(b, W - 0.7, 0.45, 0.35)
        M = Matrix.Translation((0, -D / 2 + 0.5, 0)) @ Matrix.Rotation(J(b, 0.04), 4, "Z")
    b.place_since(m, M)
    b.info.update(width=W, depth=D, eave_front=Hf, eave_back=Hb)


BUILDERS = {"table_round": build_table, "bench": build_bench, "lean_to": build_lean_to}


# ---------------------------------------------------------------- assembly

def material(slot):
    """The shared material by name; built around its approved texture only when the file has
    none (so a file that already holds the house kit's "plank" keeps that one)."""
    m = bpy.data.materials.get(slot)
    if m is None:
        m = bpy.data.materials.new(slot)
        uv_material(m, TEXTURES[slot])
    return m


def build_prop(kind, seed=1):
    """Build one prop of `kind` (see KINDS) from `seed`. Returns a Collection NOT linked to any
    scene: one root empty named `kind` at the ground contact centre (+Y front), every part
    parented to it, one mesh object per material slot with a live Bevel."""
    if kind not in BUILDERS:
        raise ValueError(f"unknown seating kind {kind!r}; one of {KINDS}")
    b = Prop(kind, seed)
    BUILDERS[kind](b)
    col = bpy.data.collections.new(f"{PREFIX}_{kind}_{seed}")
    root = bpy.data.objects.new(kind, None)
    root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.6
    col.objects.link(root)
    root["prop_kind"], root["prop_seed"], root["prop_variant"] = kind, seed, b.variant
    for k, v in b.info.items():
        root[f"prop_{k}"] = round(v, 3) if isinstance(v, float) else v
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for slot, pcs in b.pieces.items():
        if not pcs:
            continue
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        for pc in pcs:
            BK._append(bm, uv, pc)
            pc.bm.free()
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
        bm.normal_update()
        width, sharp_deg, harden, segs = FINISH[slot]
        lim = math.radians(sharp_deg)
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
        for v in bm.verts:
            lo = Vector(map(min, lo, v.co))
            hi = Vector(map(max, hi, v.co))
        me = bpy.data.meshes.new(f"{PREFIX}_{kind}_{seed}_{slot}")
        bm.to_mesh(me)
        bm.free()
        me.materials.append(material(slot))
        ob = bpy.data.objects.new(me.name, me)
        ob.parent = root
        col.objects.link(ob)
        bev = ob.modifiers.new("Bevel", "BEVEL")
        bev.width, bev.segments, bev.limit_method = width, segs, "ANGLE"
        bev.angle_limit = lim
        bev.harden_normals = harden
        bev.use_clamp_overlap = True
    root["prop_box"] = [round(c, 3) for c in (*lo, *hi)]
    return col


# ---------------------------------------------------------------- checks

def check_prop(col):
    """Tri counts (base and bevelled), non-manifold edges per object, COPLANAR overlaps (parallel
    faces of different shells within 4 mm over each other: z-fighting) and FLOATING shells (in no
    chain of intersections to a shell that sinks below the ground, z < -4 mm)."""
    V, P, shell, slot_of = [], [], [], []
    out = {"tris": 0, "tris_bevelled": 0, "nonmanifold": {}, "shells": 0}
    sid = 0
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    shell_zmin = {}
    for ob in col.objects:
        if ob.type != "MESH":
            continue
        me = ob.data
        out["tris"] += sum(len(p.vertices) - 2 for p in me.polygons)
        ev = ob.evaluated_get(dg)
        em = ev.to_mesh()
        out["tris_bevelled"] += sum(len(p.vertices) - 2 for p in em.polygons)
        ev.to_mesh_clear()
        bm = bmesh.new()
        bm.from_mesh(me)
        nm = sum(1 for e in bm.edges if not e.is_manifold)
        if nm:
            out["nonmanifold"][ob.name] = nm
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
        V.extend(ob.matrix_basis @ v.co for v in me.vertices)
        for p in me.polygons:
            r = find(p.vertices[0])
            if r not in roots:
                roots[r] = sid
                sid += 1
            s = roots[r]
            P.append([base_v + i for i in p.vertices])
            shell.append(s)
            slot_of.append(ob.name.rsplit("_", 1)[-1])
            shell_zmin[s] = min(shell_zmin.get(s, 1e9), min(V[base_v + i].z for i in p.vertices))
    out["shells"] = sid
    tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
    normals = []
    for p in P:
        a, c, d = V[p[0]], V[p[1]], V[p[2]]
        nn = (c - a).cross(d - a)
        normals.append(nn.normalized() if nn.length > 1e-12 else nn)
    where = {}
    for i, p in enumerate(P):
        n = normals[i]
        if n.length < 0.5:
            continue
        c = sum((V[k] for k in p), Vector()) / len(p)
        for q in [c] + [c.lerp(V[k], 0.7) for k in p]:
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
    anchored = {f2(s) for s in range(sid) if shell_zmin[s] < -0.004}
    loose = [s for s in range(sid) if f2(s) not in anchored]
    out["floating"] = len(loose)
    out["floating_examples"] = sorted({(slot_of[i], tuple(round(x, 2) for x in V[P[i][0]]))
                                       for i, s in enumerate(shell) if s in loose})[:6]
    xs, ys, zs = [v.x for v in V], [v.y for v in V], [v.z for v in V]
    out["size"] = (round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(max(zs), 2))
    out["zmin"] = round(min(zs), 3)
    return out


# ---------------------------------------------------------------- preview

def _mat(name, colour, rough=0.85):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = colour + (1,)
    bsdf.inputs["Roughness"].default_value = rough
    return m


# (kind, seed, x, y, yaw degrees): two rows seen from -Y. Huts at the back, tables and benches in
# front; the bench gets three seeds so all three of its variants show.
LINEUP = [("lean_to", 1, -5.2, 4.2, 8.0), ("lean_to", 2, -0.6, 4.4, -6.0), ("lean_to", 3, 4.0, 4.2, 4.0),
          ("table_round", 1, -5.6, -0.6, 0.0), ("table_round", 2, -2.6, -0.8, 20.0),
          ("bench", 1, 0.2, -1.2, 6.0), ("bench", 2, 2.4, -1.0, -8.0), ("bench", 3, 4.9, -0.8, 4.0)]


def _preview_scene():
    scene = bpy.context.scene
    sand = bpy.data.meshes.new("sand")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=60)
    bm.to_mesh(sand)
    bm.free()
    sand.materials.append(_mat("sand_warm", (0.80, 0.62, 0.40), 0.95))
    scene.collection.objects.link(bpy.data.objects.new("sand", sand))
    ref = bpy.data.meshes.new("scale_ref_1m60")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.2, radius2=0.2, depth=1.6)
    bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
    bm.to_mesh(ref)
    bm.free()
    ref.materials.append(_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
    for i, at in enumerate(((-7.6, 1.2, 0.0), (1.6, 3.0, 0.0))):
        r = bpy.data.objects.new(f"scale_ref_1m60_{i}", ref)
        r.location = at
        scene.collection.objects.link(r)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color = 4.5, (1.0, 0.88, 0.7)
    sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(48), 0, math.radians(-35))
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
    if hasattr(scene.eevee, "shadow_pool_size"):
        scene.eevee.shadow_pool_size = "1024"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    scene.view_settings.view_transform = "AgX"
    for look in ("AgX - Punchy", "Punchy"):
        try:
            scene.view_settings.look = look
            break
        except TypeError:
            continue
    return cam


SHOTS = [("lineup", (-0.4, -15.5, 6.2), (-0.4, 1.6, 0.9), 30),
         ("close", (-7.2, -5.2, 2.6), (-3.6, 2.0, 1.0), 30),
         ("benches", (2.6, -6.6, 2.3), (2.6, -0.9, 0.25), 28)]


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    top = bpy.data.collections.new("lagoon_seating")
    bpy.context.scene.collection.children.link(top)
    for kind, seed, x, y, yaw in LINEUP:
        col = build_prop(kind, seed)
        top.children.link(col)
        c = check_prop(col)
        root = next(o for o in col.objects if o.parent is None)
        print(f"[{PREFIX}] {kind} {seed} {root['prop_variant']}: {c}")
        root.location = (x, y, 0.0)
        root.rotation_euler = (0, 0, math.radians(yaw))
    if not version:
        return
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    cam = _preview_scene()
    scene = bpy.context.scene
    for tag, pos, tgt, lens in SHOTS:
        cam.location = pos
        cam.data.lens = lens
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        path = PREVIEWS / f"{PREFIX}_{tag}_v{version}.png"
        if path.exists():
            raise SystemExit(f"[{PREFIX}] {path} exists: never overwrite a render, bump --preview")
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print(f"[{PREFIX}] preview", path)


if __name__ == "__main__":
    main()
