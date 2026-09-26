"""Lagoon Court BEACH NET AND ROPE PROPS: models, world-scale UVs and their own painted textures.

  py -3 tools/author_lagoon_props_beach.py --paint                  # paint every pb_ texture
  py -3 tools/author_lagoon_props_beach.py --paint pb_net,pb_rope   # paint the named ones
  blender -b --python tools/author_lagoon_props_beach.py -- --preview N

The Blender run writes ArtSource/lagoon/lagoon_props_beach.blend (every kind, two or three seeds
each, checked) and, with --preview N, renders in Logs/lagoon-blender/: propsbeach_lineup_vN.png
(every kind on warm sand beside a 1.6 m pink scale cylinder), propsbeach_close_vN.png (net piles
and rope coils up close), propsbeach_far_vN.png (the lineup from 20 m at the game's eye height:
the readability test) and propsbeach_swatches_vN.png (every new texture flat: a detail crop and a
2 x 2 repeat that shows any tiling seam). A missing pb_ texture is painted first (under py -3,
because Blender's Python has no PIL). An existing render is never overwritten: bump N.

WHY (docs/LAGOON_REWORK_GUIDE.md § 7a item 1, § 8 step 5): the reference village (ArtStation
GvJv5a) has nets drying on the sand and piled on piers, and rope coils everywhere; ours had none.
This file is the NET AND ROPE group of the prop kit (the owner split the kit into one file per
group); the Filipino versions are green nylon nets with bamboo floats (patang) and a bamboo A-frame
drying rack.

IMPORTABLE WITH NO SIDE EFFECTS. author_lagoon_cove.py calls:

  build_prop(kind, seed) -> Collection   one root empty named `kind`, every part parented to it,
                                         NOT linked anywhere (the caller links it)
  drape(root, height_fn)                 net_spread only: bend a PLACED copy onto uneven sand
                                         (see its docstring; the copy needs its own mesh data)

KINDS, origin at the GROUND CONTACT CENTRE (z = 0 is the sand; edges tuck 0.8 to 2 cm under it,
the rack's legs 20 cm), FRONT toward +Y. Typical sizes (they vary per seed):

  net_pile    a soft green heap of two or three big slumped lumps, its edge tucked into the sand, a
              fat float line with three or four big bamboo floats across it. ~2.0 x 1.5 x 0.5 m.
  net_spread  a net laid out flat to dry: a few broad low folds, fat floats along the back (+Y)
              edge, a rope along the front. ~2.6 x 1.8 x 0.07 m. See-through (alpha).
  net_rack    a chunky bamboo A-frame (at each end two thick, slightly bowed legs lashed side by
              side with one fat knot; a sagging ridge pole in the forks) with a net hung over the
              ridge, lower on one side. ~3.6 x 1.6 x 2.2 m; the ridge runs along X, so the net's
              broad faces look to +Y and -Y.
  rope_coil   a few fat loops of manila rope (4.5 cm thick, two and a half turns), one loose loop
              lying on them and a short tail. ~0.9 x 0.7 x 0.16 m.

⚠️ STYLE, owner on v7: "i dont like how details some of the props are. again we're going for a
stylized semi-cartoony environment style" and "experiment more with being organic in how you
shape things". So: FEW, BIG, ROUND shapes, no small parts (no crossbars, no geometric bamboo node
rings, no thin cords), and the painted texture suggests the detail (bold 10 cm net cells, a rope
of a few soft twist bands).

MATERIAL SLOTS (exact names, one object per slot per prop). The shared house and boat material
"bamboo" is REUSED by name (the floats and the rack); if the file has none it is built from
bamboo_a with render_lagoon_texture_preview.uv_material, the cove's provisional choice. New
surfaces get their OWN drawings, prefix "pb_":

  pb_net      heaped net: a bold 10 cm diamond of thick soft cords over the net's own shade (opaque)
  pb_netopen  the same cords ALONE, the holes transparent (alpha in the albedo): spread and hung nets
  pb_rope     a fat manila rope: flat colour with a soft slanted twist band every ~11 cm

  ⚠️ ALPHA: pb_netopen carries alpha. Blender draws it dithered and two-sided. In Unity use alpha
  clip (cutoff ~0.5), Cull Off, and tick "Mip Maps Preserve Coverage" on pb_netopen_albedo, or the
  2 cm cords fade out of the mips and the net thins at range.

UVs: one map "UVMap", WORLD SCALE, 1 UV unit = 2 m (the house and boat kits' rule; every texture
here is a 2 m tile at 1024 px). Rope and bamboo: V along the member, U around it. Nets: box
projected (net_pile), projected from above (net_spread), or U along the ridge and V down the hanging
sheet (net_rack).
Every piece has a random UV offset.

THE HOUSE STYLE (Art_Direction.md § 0, KANTO_DESIGN_GUIDE.md § 2 and § 3, LAGOON_REWORK_GUIDE.md
§ 2): chunky, slightly irregular hand-made props from a few clear forms; nothing ruler straight
(the heap crumples, the rack's legs are cut unevenly, the hung net folds and its hem waves);
textures flat and hand-illustrated, a few LARGE feathered patches, low contrast, no grain, no noise,
no streaks. ⚠️ Every surface here is its own drawing; the painters share only plumbing (the
periodic field, feathered patch and pixel grid of lagoon_paint_materials.py), never a generator.
⚠️ ROLE HUES (Art_Direction.md § 1): nothing near offence orange #f87020 or defence blue #0080e8.
The net is GREEN nylon, common in Philippine fishing and far from both; the rope is manila tan.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2). The A-frame's legs are lashed SIDE
BY SIDE (two tubes crossing in one plane put facets back to back); each heap lump tucks its lip
at its own height; the float rope runs down each float's AXIS; the net's arc over the ridge avoids
the pole's facet angles; neighbouring coil turns press 20 % into each other. `check_prop` proves
it (`coplanar`: parallel faces of different shells within 4 mm over each other) and proves nothing
floats (`loose`: shells in no chain of intersections to one that touches the sand).
"""
import math
import os
import random
import subprocess
import sys
from pathlib import Path

try:
    import bmesh
    import bpy
    from mathutils import Matrix, Vector
    from mathutils.bvhtree import BVHTree
except ImportError:      # plain Python: the texture painters only
    bpy = None

TOOLS = Path(__file__).resolve().parent
ROOT = TOOLS.parent
if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))
SOURCE = ROOT / "ArtSource" / "lagoon"
TEX = Path(os.environ.get("LAGOON_TEX_OUT", SOURCE / "textures"))
PREVIEWS = ROOT / "Logs" / "lagoon-blender"

KINDS = ("net_pile", "net_spread", "net_rack", "rope_coil")
TEXTURES = ("pb_net", "pb_netopen", "pb_rope")
UV_METRES = 2.0       # 1 UV unit = 2 m, as the house and boat kits


# ================================================================ painting (numpy, PIL)
# Run under py -3. Each painter returns (albedo HxWx3 or HxWx4 in 0..1, height HxW).

def _np():
    import numpy as np
    return np


def _P():
    """lagoon_paint_materials: PLUMBING only (the 2 m periodic field, the feathered patch, the
    pixel grid in metres, normal from height). None of its painters is called."""
    import lagoon_paint_materials as P
    return P


# Normal strength per texture: the owner asked that normal maps READ ("make sure it has
# depth/normal maps"); fine patterns need more than broad ones.
STRENGTH = {"pb_net": 1.5, "pb_netopen": 1.5, "pb_rope": 2.5}


def _mix(img, col, m):
    return img * (1 - m[..., None]) + col * m[..., None]


# ---------------------------------------------------------------- pb_net, pb_netopen
# Research (stylized fishing nets in hand-painted kits and the reference's net piles): a net at
# game distance reads as a DIAMOND mesh of chunky strands with swollen KNOTS at the crossings; a
# heap reads darker inside, where layers of net overlap, with the top layer lighter. Real mesh is
# 2 to 5 cm; stylized it is drawn chunkier (6.4 cm diamonds, 1.1 cm strands) so it survives
# mipmapping and reads from the court. Lines are wobbled by a smooth field: never ruled.
# ⚠️ OWNER, on v7: "i dont like how details some of the props are. again we're going for a
# stylized semi-cartoony environment style". The mesh is now BOLD and SOFT: 10 cm diamonds of
# thick 2.2 cm cords with soft edges (the lead's brief: 8 to 10 cm cells, few per metre), knots only
# a gentle swelling, so the net reads as a few broad painted cords, never a fine pattern. v8 tried
# 16 cm cells: too open, the heap read as a plaid blanket.
NET_N = 14            # diagonal lines per 2 m tile each way: a 10.1 cm diamond


def _net_layer(x, y, seed, shift=0.0, strand=0.011, knot=0.016):
    np, P = _np(), _P()
    c = 2.0 / NET_N
    wx = x + 0.02 * P.field(0.6, seed) + shift
    wy = y + 0.02 * P.field(0.6, seed + 1)
    # The x + y and x - y families are periodic in x and in y with period c, and 2 m / c is
    # whole, so the diamond mesh tiles.
    da = np.abs((wx + wy + c / 2) % c - c / 2) / math.sqrt(2)
    db = np.abs((wx - wy + c / 2) % c - c / 2) / math.sqrt(2)
    d = np.minimum(da, db)
    soft = 0.006
    line = P.smooth(np.clip((strand - d) / soft + 0.5, 0, 1))
    knot_m = P.smooth(np.clip((knot - np.hypot(da, db)) / soft + 0.5, 0, 1))
    core = P.smooth(np.clip((strand * 0.4 - d) / soft + 0.5, 0, 1))
    return np.maximum(line, knot_m), knot_m, core


def paint_net(open_=False):
    np, P = _np(), _P()
    x, y = P.grid()
    top, kn, core = _net_layer(x, y, 21)
    # Render v1: bright strands over a near-black base read as a green cabbage, not a net. The base
    # is lifted and the strands brought a step closer to it. Alone on sand (pb_netopen) the strands
    # are a shade DARKER, or the spread net was a faint haze.
    if open_:
        strand, light, knotc = P.hexcol("5f7f45"), P.hexcol("75955a"), P.hexcol("56763f")
    else:
        # Render v2: still a plaid at 3 m; the heap's strands sit only a few steps off its base.
        strand, light, knotc = P.hexcol("6e8a55"), P.hexcol("7c9861"), P.hexcol("66834f")
    col = np.broadcast_to(strand, (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    col = _mix(col, light, core * 0.45)            # the lit crown of each strand
    col = _mix(col, knotc, kn * 0.45)              # knots a shade darker: they read as knots
    # Sheet v1: 0.5 m patches at 1.09 and 0.8 read as camouflage over a 4 m repeat; v6: 0.8 m ones
    # made ONE blob per tile, which the 4 m view repeated as a pattern. Several soft coats, a
    # shade apart: the house style's feathered patches without either fault.
    col = P.tint(col, np.array([1.04, 1.04, 1.03]), P.patch(0.5, 0.30, 31, feather=0.8))   # sun-faded coat
    shade = P.patch(0.45, 0.22, 33, feather=0.8)
    if open_:
        col = P.tint(col, np.array([0.95, 0.95, 0.95]), shade)
        # The holes keep the strand colour under alpha 0, so filtering never pulls in a dark
        # fringe round the strands.
        return np.dstack([col, top]), top + 0.4 * kn
    under, _, _ = _net_layer(x, y, 11, shift=0.37 * 2.0 / NET_N)
    img = np.broadcast_to(P.hexcol("56744a"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    img = _mix(img, P.hexcol("5c7a50"), under * 0.35)     # the layer beneath: barely there
    img = img * (1 - top[..., None]) + col * top[..., None]
    img = P.tint(img, np.array([0.94, 0.94, 0.94]), shade)    # folds in shade
    return img, 0.4 * under + top + 0.4 * kn


# ---------------------------------------------------------------- pb_rope
# Research (hand-painted rope in stylized kits): three fat strands laid diagonally, each a rounded
# band lit on one side (one cel band) with a dark groove between; colour per strand shifts a
# shade; no fibres. V runs along the rope, U around it (the sweep's rule); ku and kv are whole
# cycles per tile, so it tiles, and ku : kv lays the strands at ~35 degrees to the rope's length.


def paint_rope():
    np, P = _np(), _P()
    x, y = P.grid()
    # Owner, on v7 (too detailed), and v8 read as a barber pole: a FLAT rope colour with a soft
    # painted TWIST BAND every ~11 cm along it (slanted, so it reads as a lay), one light band
    # beside each; no strands.
    ku, kv = 8, 18
    s = (x * ku + y * kv) / 2.0 + 0.06 * P.field(0.6, 41)
    ph = s - np.floor(s)
    base, light, groove = P.hexcol("c9a86f"), P.hexcol("d9bc88"), P.hexcol("a6844f")
    img = np.broadcast_to(base, (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    # v9: narrow dark bands read as a striped snake; broad soft ones, a shade apart.
    g = P.smooth(np.clip((0.2 - np.abs(ph - 0.5)) / 0.15, 0, 1))           # the twist band
    lit = P.smooth(np.clip((0.15 - np.abs(ph - 0.82)) / 0.12, 0, 1))       # its lit shoulder
    img = _mix(img, groove, g * 0.4)
    img = _mix(img, light, lit * 0.4)
    prof = 1 - g
    # Sheet v1: 0.4 m coats at 0.86 read as blotches; v6: 0.8 m ones repeated as one blob per tile.
    # Long soft coats along the rope, a shade apart.
    img = P.tint(img, np.array([0.96, 0.95, 0.94]), P.patch(0.4, 0.22, 43, feather=0.8))  # handled, darker
    return img, prof


PAINTERS = {
    "pb_net": lambda: paint_net(False),
    "pb_netopen": lambda: paint_net(True),
    "pb_rope": paint_rope,
}


def save_texture(name, albedo, height):
    """<name>_albedo.png (RGBA where the painter gave alpha), _height.png, _normal.png (OpenGL,
    green up): the same three files and conventions as the other Lagoon texture scripts."""
    np, P = _np(), _P()
    from PIL import Image
    TEX.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    r = np.ptp(height)
    h = (height - height.min()) / r if r > 1e-9 else np.full_like(height, 0.5)
    mode = "RGBA" if albedo.shape[2] == 4 else "RGB"
    Image.fromarray((albedo * 255 + 0.5).astype(np.uint8), mode).save(TEX / f"{name}_albedo.png")
    Image.fromarray((h * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_height.png")
    Image.fromarray((P.normal_from_height(h, STRENGTH[name]) * 255 + 0.5).astype(np.uint8)).save(
        TEX / f"{name}_normal.png")
    print("[props-beach] painted", name)


def paint(names=None):
    for n in names or TEXTURES:
        save_texture(n, *PAINTERS[n]())


# ================================================================ modelling (bpy)

if bpy is not None:
    import author_lagoon_boats as BK      # noqa: E402  Piece, loft, sweep, _uv_frame, _catmull, _append
    Z = Vector((0.0, 0.0, 1.0))
    X = Vector((1.0, 0.0, 0.0))
    Y = Vector((0.0, 1.0, 0.0))

# The shared kit material this file reuses by name, and the texture it is built from when the file
# has none yet (the cove's CHOSEN: bamboo_a, provisional).
SHARED = {"bamboo": "bamboo_a"}
ALPHA = ("pb_netopen",)
# Surfaces rather than solids: the non-manifold count is not asked of them.
OPEN = ("pb_netopen", "pb_net")
# Per slot: (bevel width m or None, angle above which an edge is sharp, harden normals). Rope and
# net are soft forms with no hard edges to round, so they carry no Bevel.
FINISH = {
    "bamboo": (0.005, 50.0, False),
    "pb_rope": (None, 60.0, False),
    "pb_net": (None, 60.0, False),
    "pb_netopen": (None, 60.0, False),
}


class Prop:
    """Collects pieces per material slot for one prop. Duck-types the boat kit's `Boat`
    (rng, add, uv_off), so author_lagoon_boats.sweep builds straight into it."""

    def __init__(self, kind, seed):
        self.kind, self.seed = kind, seed
        self.rng = random.Random(f"lagoon-propbeach:{kind}:{seed}")
        self.pieces = {}
        self.info = {}

    def add(self, slot, pc):
        if pc is None or not pc.bm.faces:
            return None
        self.pieces.setdefault(slot, []).append(pc)
        return pc

    def uv_off(self):
        return (self.rng.random(), self.rng.random())


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def _uv_top(pc, off):
    """Project from above at world scale: nets lie in broad sheets whose pattern has no
    direction, so a top projection keeps the mesh square where it is seen (from above)."""
    s = 1.0 / UV_METRES
    for f in pc.bm.faces:
        for lp in f.loops:
            lp[pc.uv].uv = (lp.vert.co.x * s + off[0], lp.vert.co.y * s + off[1])


def blob(p, slot, c, radii, subdiv=1):
    """A small squashed ball: a lashing knot."""
    pc = BK.Piece()
    bmesh.ops.create_icosphere(pc.bm, subdivisions=subdiv, radius=1.0)
    bmesh.ops.scale(pc.bm, vec=radii, verts=pc.bm.verts)
    bmesh.ops.translate(pc.bm, vec=c, verts=pc.bm.verts)
    BK._uv_frame(pc, X, Y, Z, p.uv_off())
    return p.add(slot, pc)


# ---------------------------------------------------------------- nets

def heap(p, slot, cx, cy, R, H, spin, lip=0.01):
    """One lump of heaped net: a crumpled dome over a wobbly outline, its edge running out onto
    the sand as a lip at `lip` that then tucks 2.5 cm under. Folds are RIDGES (1 - |sin|)^2 in
    five directions, fading out toward the rim, so the heap reads as crumpled cloth, not a
    boulder. Each lump gets its own lip height: two lips at one height overlap in one plane.
    Render v2: a smooth dome with 2 to 4 cm folds read as a green cushion; the heap now SLUMPS
    (a flatter crown), its outline lobes more, and the ridges are up to 8 cm. Render v4: cubed
    ridges at 5 to 10 per metre on a 30 x 12 grid went spiky and faceted, like green rock; the
    folds are now squared (rounder), broader (4 to 7 per metre) and drawn on a 40 x 16 grid."""
    rng = p.rng
    ph = [rng.uniform(0, math.tau) for _ in range(3)]
    # Owner, on v7: a net pile is "a soft lumpy heap with a painted net pattern". Three broad,
    # rounded folds, not six crumpled ridges.
    folds = [(rng.uniform(0, math.pi), rng.uniform(2.5, 4.0), rng.uniform(0, math.tau), rng.uniform(0.04, 0.06))
             for _ in range(3)]
    ex = rng.uniform(1.05, 1.3)

    def Rth(a):
        return R * (1 + 0.16 * math.sin(2 * a + ph[0]) + 0.1 * math.sin(3 * a + ph[1]) + 0.05 * math.sin(5 * a + ph[2]))

    def fold(x, y):
        s = 0.0
        for d, f, q, amp in folds:
            s += amp * math.sin((x * math.cos(d) + y * math.sin(d)) * f + q)
        return s
    # v9: the flat apron round each lump showed as a thin wire hoop at the heap's edge once the
    # heap slumped; the edge now simply tucks 2 cm into the sand.
    fs = [0.03, 0.13, 0.24, 0.35, 0.45, 0.54, 0.63, 0.71, 0.78, 0.84, 0.89, 0.93, 0.97, 1.0]
    seg = 40
    rings = []
    ca, sa = math.cos(spin), math.sin(spin)
    for f in fs:
        ring = []
        for k in range(seg):
            a = math.tau * k / seg
            rr = Rth(a) * f
            lx, ly = rr * math.cos(a) * ex, rr * math.sin(a)
            x, y = cx + lx * ca - ly * sa, cy + lx * sa + ly * ca
            if f < 1.0:
                fade = min(1.0, (1.0 - f) / 0.18)
                # v8: exponent 0.7 made a hard shoulder and a helmet rim; 1.3 spreads the heap
                # softly out to the sand, the way cloth slumps.
                z = H * max(0.0, 1 - f ** 1.8) ** 1.3 + fold(x, y) * fade + (lip + 0.004) * (1 - fade)
            else:
                z = -0.02
            ring.append(Vector((x, y, z)))
        rings.append(ring)
    uvs = [[(0, 0)] * (seg + 1) for _ in rings]
    pc = BK.loft(rings, uvs)
    # BOX projection (render v2: a top projection stretched the diamonds into a plaid down the
    # heap's steep sides). The switch between faces is lost in a pattern this dense.
    BK._uv_frame(pc, X, Y, Z, p.uv_off())
    return p.add(slot, pc)


def _tree(pieces):
    V, P = [], []
    for pc in pieces:
        base = len(V)
        V.extend(v.co.copy() for v in pc.bm.verts)
        idx = {v: i for i, v in enumerate(pc.bm.verts)}
        P.extend([base + idx[v] for v in f.verts] for f in pc.bm.faces)
    return BVHTree.FromPolygons(V, P)


def _drape_line(tree, pts2d, lift, ground_z):
    """Points laid over a surface from above: each (x, y) gets the surface point plus `lift`
    along its normal, or `ground_z` where the surface is missed (the sand)."""
    out = []
    for x, y in pts2d:
        hit = tree.ray_cast(Vector((x, y, 5.0)), Vector((0, 0, -1)))
        # On the net wherever it is over any of it, aprons included: dropped to the sand's
        # height over an apron, the rope lay a few millimetres off it (the check, v3).
        if hit[0] is not None and hit[0].z + lift > ground_z:
            out.append(hit[0] + hit[1] * lift)
        else:
            out.append(Vector((x, y, ground_z)))
    return out


def _flat_against(pc, tree):
    """True if any face of `pc` lies within 4 mm of, and parallel to, a face in `tree`: the
    check_prop z-fight test, run on one piece as it is placed."""
    pc.bm.normal_update()
    for f in pc.bm.faces:
        loc, nrm, _i, _d = tree.find_nearest(f.calc_center_median(), 0.004)
        if loc is not None and abs(nrm.dot(f.normal)) > 0.999:
            return True
    return False


def _part(pc, tree, gap=0.01):
    """Push `pc`'s vertices that come within `gap` of the surface in `tree` straight away from it.
    Two smooth heap lumps meeting at a shallow angle are nearly parallel along their crossing,
    which is a z-fight sliver (render v5's check); pushed apart there, they cross steeply."""
    for v in pc.bm.verts:
        loc, _n, _i, dist = tree.find_nearest(v.co, gap)
        if loc is not None and dist > 1e-6:
            v.co += (v.co - loc).normalized() * (gap - dist)
    pc.bm.normal_update()


def float_line(p, pts, rr=0.02, every=0.5, fr=0.07, flen=0.3, phase=None, seat=0.0, tree=None, rope=True):
    """A float rope with short bamboo floats threaded on it (the patang of a Philippine net). The
    rope runs down each float's AXIS (a thin tube inside a fat one: no facets within millimetres
    of each other), so each float sinks into whatever the rope lies on. Render v1: 3.4 cm floats
    were invisible from the court; 4.5 cm by 26 cm now. `seat` lowers each float that much below
    the rope, pressing it into a soft heap (a rigid float laid exactly on the rope over a crumpled
    fold came within 4 mm of the net, parallel, on one seed). With `tree` (the net under it) each
    float is TESTED as it is placed and, if one of its facets lies flat on the net, turned and
    pressed 4 mm deeper, up to six times: a crumpled surface has too many faces to reason about."""
    if rope:
        BK.sweep(p, "pb_rope", pts, rr, sides=7, step=0.06)
    P = [Vector(q) for q in pts]
    cum = [0.0]
    for a, b in zip(P, P[1:]):
        cum.append(cum[-1] + (b - a).length)
    s = every * 0.5
    while s < cum[-1] - 0.1:
        i = min(max(k for k in range(len(cum)) if cum[k] <= s), len(P) - 2)
        t = (s - cum[i]) / max(1e-6, cum[i + 1] - cum[i])
        c = P[i].lerp(P[i + 1], t)
        d = (P[i + 1] - P[i]).normalized()
        ph0 = p.rng.uniform(0, 0.7) if phase is None else phase
        for attempt in range(6):
            cc = c - Z * (seat + 0.004 * attempt)
            pc = BK.sweep(p, "bamboo", [cc - d * flen / 2, cc + d * flen / 2], fr, sides=10, dome=True,
                          phase=ph0 + 0.23 * attempt)
            if tree is None or not _flat_against(pc, tree):
                break
            p.pieces["bamboo"].remove(pc)
            pc.bm.free()
        s += every * p.rng.uniform(0.85, 1.15)


def build_net_pile(p):
    rng = p.rng
    lumps = [(0.0, 0.0, rng.uniform(0.55, 0.62), rng.uniform(0.44, 0.52), rng.uniform(0, math.tau)),
             (rng.uniform(0.38, 0.5), rng.uniform(0.15, 0.3), rng.uniform(0.34, 0.42), rng.uniform(0.26, 0.33),
              rng.uniform(0, math.tau))]
    if rng.random() < 0.5:        # a third, smaller lump: still few big shapes
        lumps.append((rng.uniform(-0.5, -0.35), rng.uniform(0.2, 0.35), rng.uniform(0.3, 0.36),
                      rng.uniform(0.2, 0.26), rng.uniform(0, math.tau)))
    # Aprons 9 mm apart, each 4 mm thick in height: v3's 7 mm steps over a 6 mm apron put two
    # lumps' aprons 3.5 mm apart where they overlap.
    pieces = []
    for k, lp in enumerate(lumps):
        pc = heap(p, "pb_net", *lp, lip=0.004 + 0.009 * k)
        for _ in range(4):
            if not pieces:
                break
            under = _tree(pieces)
            if not _flat_against(pc, under):
                break
            _part(pc, under)
        pieces.append(pc)
    tree = _tree(pieces)
    # The float line: across the heap and out onto the sand at both ends, a lazy S in plan.
    a0 = rng.uniform(-0.5, 0.5)
    d = Vector((math.cos(a0), math.sin(a0), 0))
    side = Vector((-d.y, d.x, 0))
    pts2d = []
    for i in range(41):          # dense, so the line follows the folds instead of chording them
        f = i / 40
        q = d * (-0.8 + 1.6 * f) + side * (0.1 * math.sin(f * 4.0 + a0))
        pts2d.append((q.x, q.y))
    rr = 0.022                    # owner v7: fat, few parts
    # 0.15 of the rope's radius over the net: at 0.4 one of its six facets lay within 4 mm of,
    # and parallel to, the net under it (the check).
    # Tested as placed: if a facet of the rope lies flat on the net anywhere, the rope's section
    # is turned (a quarter of a facet at a time) and, failing that, lifted a little.
    for lift, rot in ((0.15, 0.0), (0.15, 0.26), (0.15, 0.52), (0.3, 0.13), (0.3, 0.39), (0.45, 0.2)):
        pts = _drape_line(tree, pts2d, rr * lift, rr - 0.004)
        rope = BK.sweep(p, "pb_rope", pts, rr, sides=7, step=0.06, phase=rot)
        if not _flat_against(rope, tree):
            break
        p.pieces["pb_rope"].remove(rope)
        rope.bm.free()
    float_line(p, pts, rr=rr, every=0.5, fr=0.075, flen=0.3, seat=0.03, tree=tree, rope=False)
    p.info.update(lumps=len(lumps))


def build_net_spread(p):
    """A net laid flat on the sand to dry. Its border tucks 8 mm under the sand; inside, low folds
    up to ~6 cm, long ones running with the net and a few across. See-through."""
    rng = p.rng
    W, D = rng.uniform(2.4, 2.9), rng.uniform(1.4, 1.75)
    nx, ny = 30, 18
    ph = [rng.uniform(0, math.tau) for _ in range(6)]
    pc = BK.Piece()
    bm = pc.bm
    grid = []
    for j in range(ny + 1):
        t = j / ny
        row = []
        for i in range(nx + 1):
            s = i / nx
            x = (s - 0.5) * W * (1 + 0.05 * math.sin(math.tau * t * 1.5 + ph[0]))
            y = (t - 0.5) * D * (1 + 0.07 * math.sin(math.tau * s * 2 + ph[1])) + 0.05 * math.sin(3 * math.pi * s + ph[2])
            edge = min(i, nx - i, j, ny - j)
            if edge == 0:
                z = -0.008
            else:
                fade = min(1.0, edge / 3.0)
                fold = (0.05 * (0.5 + 0.5 * math.sin(y * 3.0 + 0.5 * math.sin(x * 1.1 + ph[3]) + ph[4])) ** 2
                        + 0.02 * (0.5 + 0.5 * math.sin(x * 2.0 + ph[5])) ** 2)
                z = 0.006 + fold * fade
            row.append(bm.verts.new((x, y, z)))
        grid.append(row)
    for j in range(ny):
        for i in range(nx):
            bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
    _uv_top(pc, p.uv_off())
    p.add("pb_netopen", pc)
    rr, fr = 0.02, 0.065             # owner v7: fat and few
    # The floats lie ON the sand (sunk 1.2 cm), the rope through their axes; phase 0 puts a VERTEX
    # under each float, so no float facet lies flat over the net's tucked border (the check found
    # one 2 mm over it).
    # 5 cm out from the net's edge, clear of it: a float lying over the tucked border put a facet
    # 2 mm over the rising net on one seed.
    top = [Vector((v.co.x, v.co.y + 0.08, fr - 0.015)) for v in grid[-1]]
    bot = [Vector((v.co.x, v.co.y - 0.01, rr - 0.004)) for v in grid[0]]
    float_line(p, top, rr=rr, every=0.7, fr=fr, flen=0.3, phase=0.0)
    BK.sweep(p, "pb_rope", bot, rr, sides=7, step=0.08)
    p.info.update(width=W, depth=D)


def build_net_rack(p):
    """A bamboo A-frame: at each end two legs LASHED SIDE BY SIDE (3.4 cm either side of the
    frame's plane) crossing just under the ridge, a crossbar outside them, a lashing knot; a ridge
    pole in the forks; a net over the ridge, hanging lower on one side than the other."""
    rng = p.rng
    L, H, spread = rng.uniform(2.6, 3.0), rng.uniform(1.75, 1.95), rng.uniform(0.72, 0.82)
    rp, rl = 0.065, 0.058           # owner v7: chunky poles, few parts
    zc = H + 0.1
    for sx in (-1, 1):
        x0 = sx * L / 2 + rng.uniform(-0.03, 0.03)
        for side in (-1, 1):
            dx = 0.05 * side
            foot = Vector((x0 + dx, side * spread, -0.2))
            cross = Vector((x0 + dx, 0.0, H))
            top = cross + (cross - foot).normalized() * rng.uniform(0.18, 0.28)   # cut unevenly
            # Owner: "experiment more with being organic". Each leg BOWS 3 to 6 cm in the frame's
            # plane, as real bamboo does; the two legs of an A stay 10 cm apart across it.
            ax = (top - foot).normalized()
            bow = Vector((0.0, -ax.z, ax.y)) * rng.uniform(0.03, 0.06) * rng.choice((-1, 1))
            mid = foot.lerp(top, 0.5) + bow
            BK.sweep(p, "bamboo", BK._catmull([foot, mid, top], per=5), rl, sides=8, dome=True,
                     phase=0.2 * side + 0.1)
        # The crossbars and bamboo node rings are gone (owner v7: too detailed); the bamboo
        # texture draws the nodes. One fat lashing knot where the legs cross.
        blob(p, "pb_rope", (x0, 0.0, H + 0.01), (0.11, 0.1, 0.1))
    dz = rng.uniform(0.0, 0.04)                  # the ridge pole droops toward one end

    sag = rng.uniform(0.03, 0.05)                # and SAGS between the frames (organic, owner)

    def zc_at(x):
        """The pole's axis height at x: the net rides the pole's slope and sag (v2 kept one height
        and hung up to 1.8 cm clear of it at the low end)."""
        t = (x + L / 2 + 0.28) / (L + 0.56)
        return zc - dz * t - sag * (1 - (2 * t - 1) ** 2)
    ridge = [(x, 0.0, zc_at(x)) for x in (-L / 2 - 0.28 + (L + 0.56) * i / 8 for i in range(9))]
    BK.sweep(p, "bamboo", ridge, rp, sides=8, dome=True, phase=0.0)
    # THE NET: rows across the ridge, one per x; each row runs up one side, over the pole (radius
    # 0.95 of the pole, so the pole's eight corners press through: it rests ON the pole; the arc's
    # chords sit at 18 and 54 degrees, never parallel to the pole's 22.5 + 45k facets), and down
    # the other. Hanging sides bulge a little, fold vertically (more toward the hem) and end in a
    # wavy hem; one side 0.25 to 0.45 m off the sand, the other 0.5 to 0.8 m.
    nx, ns = 22, 12
    Rn = rp * 0.95
    hems = {-1: rng.uniform(0.25, 0.45), 1: rng.uniform(0.5, 0.8)}
    if rng.random() < 0.5:
        hems = {-1: hems[1], 1: hems[-1]}
    ph = {s: [rng.uniform(0, math.tau) for _ in range(4)] for s in (-1, 1)}
    kf = math.tau / rng.uniform(0.6, 0.8)      # few broad folds
    x0n, x1n = -L / 2 + 0.2, L / 2 - 0.2
    pc = BK.Piece()
    bm, uvl = pc.bm, pc.uv
    ou, ov = p.uv_off()
    rows, rows_v = [], []
    xs = [x0n + (x1n - x0n) * i / nx for i in range(nx + 1)]
    for x in xs:
        pts = []
        for side in (-1, 1):
            hem = hems[side] + 0.1 * math.sin(x * 1.8 + ph[side][0])
            zx = zc_at(x)
            drop = zx - hem
            side_pts = []
            for m in range(ns, 0, -1):
                f = m / ns
                out = (0.06 * math.sin(math.pi * f) + 0.04 * f * f
                       + 0.05 * f * math.sin(x * kf + ph[side][2]))
                side_pts.append(Vector((x, side * (Rn + max(0.0, out)), zx - drop * f)))
            if side < 0:
                pts += side_pts
                pts += [Vector((x, math.sin(a) * Rn, zx + math.cos(a) * Rn))
                        for a in (math.radians(d) for d in (-90, -54, -18, 18, 54, 90))]
            else:
                pts += list(reversed(side_pts))
        vcum = [0.0]
        for a, b in zip(pts, pts[1:]):
            vcum.append(vcum[-1] + (b - a).length)
        rows.append([bm.verts.new(q) for q in pts])
        rows_v.append(vcum)
    s = 1.0 / UV_METRES
    for i in range(nx):
        for k in range(len(rows[i]) - 1):
            f = bm.faces.new((rows[i][k], rows[i + 1][k], rows[i + 1][k + 1], rows[i][k + 1]))
            for lp, (u, v) in zip(f.loops, ((xs[i], rows_v[i][k]), (xs[i + 1], rows_v[i + 1][k]),
                                            (xs[i + 1], rows_v[i + 1][k + 1]), (xs[i], rows_v[i][k + 1]))):
                lp[uvl].uv = (u * s + ou, v * s + ov)
    p.add("pb_netopen", pc)
    p.info.update(length=L, height=H)


# ---------------------------------------------------------------- rope

def build_rope_coil(p):
    """A flat Flemish coil (turns 80 % of a rope apart, so neighbours press into each other and no
    facets meet back to back), one loose loop lying ON the coil, and the tail run out on the sand.
    Render v1: a 2.2 cm rope read as string from 20 m; 2.6 cm now."""
    rng = p.rng
    # Owner, on v7: "a rope coil a few fat loops". A 4.5 cm rope, two and a half turns.
    rr = 0.045
    turns = rng.uniform(2.3, 2.8)
    r_in, pitch = 0.1, 2 * rr * 0.8
    z1 = rr - 0.006
    a0 = rng.uniform(0, math.tau)
    N = int(turns * 40)
    pts = []
    for i in range(N + 1):
        th = a0 + math.tau * turns * i / N
        r = r_in + pitch * turns * i / N
        pts.append(Vector((r * math.cos(th), r * math.sin(th), z1)))
    r_out = r_in + pitch * turns
    th_end = a0 + math.tau * turns
    z2 = z1 + 2 * rr * 0.85
    lt = rng.uniform(0.9, 1.1)
    M = int(lt * 40)
    for i in range(1, M + 1):
        f = i / M
        th = th_end + math.tau * lt * f
        r = r_out * 0.72 + 0.025 * math.sin(3 * th)
        z = z1 + (z2 - z1) * smooth(f / 0.12)
        if f > 0.8:
            q = smooth((f - 0.8) / 0.2)
            z = z2 - (z2 - z1) * q
            r = r + (r_out + 0.1 - r) * q
        elif f < 0.12:
            r = r_out + (r - r_out) * smooth(f / 0.12)
        pts.append(Vector((r * math.cos(th), r * math.sin(th), z)))
    last = pts[-1]
    out = Vector((last.x, last.y, 0)).normalized()
    side = Vector((-out.y, out.x, 0))
    ctrl = [last]
    for dist, lat in ((0.2, 0.06), (0.42, -0.03)):
        q = last + out * dist + side * (lat + rng.uniform(-0.03, 0.03))
        q.z = z1
        ctrl.append(q)
    pts += BK._catmull(ctrl, per=6)[1:]
    BK.sweep(p, "pb_rope", pts, rr, sides=10, step=0.06, dome=True)
    p.info.update(turns=turns, radius=r_out)


BUILDERS = {"net_pile": build_net_pile, "net_spread": build_net_spread, "net_rack": build_net_rack,
            "rope_coil": build_rope_coil}


# ---------------------------------------------------------------- materials and assembly

def _ensure_textures():
    missing = [t for t in TEXTURES if not (TEX / f"{t}_albedo.png").exists()]
    if missing:
        print("[props-beach] painting missing textures:", missing)
        subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--paint", ",".join(missing)], check=True)


def prop_material(slot):
    """The slot's material, created once. The shared bamboo is reused as the file has it (the cove
    textures it itself); new ones are built by the Lagoon UV material (albedo, normal, height
    bump) around this file's own texture. The alpha net reads the albedo's alpha, dithered and
    two-sided."""
    m = bpy.data.materials.get(slot)
    if m is not None:
        return m
    import render_lagoon_texture_preview as T
    m = bpy.data.materials.new(slot)
    T.uv_material(m, SHARED.get(slot, slot))
    nt = m.node_tree
    if slot in ("pb_net", "pb_netopen"):
        # Render v4: the Lagoon UV material's 3 cm bump (set for planks and thatch) over a 6 cm
        # net diamond embossed the knots like reptile skin. A net is a thin mesh: 8 mm.
        for n in nt.nodes:
            if n.type == "DISPLACEMENT":
                n.inputs["Scale"].default_value = 0.008
    if slot in ALPHA:
        bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        img = next(n for n in nt.nodes if n.type == "TEX_IMAGE" and n.image.name.endswith("_albedo.png"))
        nt.links.new(img.outputs["Alpha"], bsdf.inputs["Alpha"])
        if hasattr(m, "surface_render_method"):
            m.surface_render_method = "DITHERED"
        m.use_backface_culling = False
    return m


def build_prop(kind, seed=1):
    """Build one prop of `kind` (see KINDS) from `seed` and return its Collection, NOT linked to any
    scene: every part is parented to one root empty named `kind` at the ground contact centre,
    front toward +Y. The root carries prop_kind, prop_seed, prop_radius (the footprint's radius:
    ground a placed prop at the LOWEST sand under that circle so no edge floats on a slope),
    prop_top, prop_size and the builder's dimensions."""
    if kind not in BUILDERS:
        raise ValueError(f"unknown prop kind {kind!r}; one of {KINDS}")
    p = Prop(kind, seed)
    BUILDERS[kind](p)
    col = bpy.data.collections.new(f"propbeach_{kind}_{seed}")
    root = bpy.data.objects.new(kind, None)
    root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.5
    col.objects.link(root)
    root["prop_kind"], root["prop_seed"] = kind, seed
    for k, v in p.info.items():
        if isinstance(v, (int, float, str)):
            root[f"prop_{k}"] = v
    zmax, rmax, xs, ys = 0.0, 0.0, [], []
    for slot, pcs in p.pieces.items():
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        for pc in pcs:
            BK._append(bm, uv, pc)
            pc.bm.free()
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
        bm.normal_update()
        width, sharp_deg, harden = FINISH[slot]
        lim = math.radians(sharp_deg)
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
        for v in bm.verts:
            zmax = max(zmax, v.co.z)
            rmax = max(rmax, math.hypot(v.co.x, v.co.y))
            xs.append(v.co.x)
            ys.append(v.co.y)
        me = bpy.data.meshes.new(f"pb_{kind}_{seed}_{slot}")
        bm.to_mesh(me)
        bm.free()
        me.materials.append(prop_material(slot))
        ob = bpy.data.objects.new(me.name, me)
        ob.parent = root
        col.objects.link(ob)
        if width:
            bev = ob.modifiers.new("Bevel", "BEVEL")
            bev.width, bev.segments, bev.limit_method = width, 2, "ANGLE"
            bev.angle_limit = math.radians(sharp_deg)
            bev.harden_normals = harden
            bev.use_clamp_overlap = True
    root["prop_radius"] = round(rmax, 3)
    root["prop_top"] = round(zmax, 3)
    root["prop_size"] = (round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(zmax, 2))
    return col


def drape(root, height_fn):
    """Bend a PLACED net_spread onto uneven sand: every vertex of every part moves by the ground
    height under it minus the root's z, so the net follows the sand the way a laid net does
    (grounding it at one height would bury half of it or float the rest on a slope). Call it after
    setting root.location and rotation (about Z only). Each part must own its mesh data: give a
    linked duplicate its own copy first (o.data = o.data.copy())."""
    M = Matrix.Translation(root.location) @ Matrix.Rotation(root.rotation_euler.z, 4, "Z")
    for o in root.children:
        if o.type != "MESH":
            continue
        for v in o.data.vertices:
            w = M @ (o.matrix_parent_inverse @ o.matrix_basis @ v.co)
            v.co.z += height_fn(w.x, w.y) - root.location.z
        o.data.update()


# ---------------------------------------------------------------- checks

def check_prop(col):
    """Tri counts (base and with the bevels), non-manifold edges of the solid slots, COPLANAR
    overlaps (parallel faces of different shells within 4 mm over each other's interior: the
    z-fight), and LOOSE shells: in no chain of intersections to a shell that touches the sand
    (its lowest point at or under z = 4 mm)."""
    V, P, shell, slot_of, shell_min = [], [], [], [], {}
    out = {"tris": 0, "tris_bevelled": 0, "nonmanifold": {}, "shells": 0}
    sid = 0
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    for ob in col.objects:
        if ob.type != "MESH":
            continue
        me = ob.data
        out["tris"] += sum(len(q.vertices) - 2 for q in me.polygons)
        ev = ob.evaluated_get(dg)
        em = ev.to_mesh()
        out["tris_bevelled"] += sum(len(q.vertices) - 2 for q in em.polygons)
        ev.to_mesh_clear()
        slot = me.materials[0].name if me.materials else "?"
        if slot not in OPEN:
            bm = bmesh.new()
            bm.from_mesh(me)
            nm = sum(1 for e in bm.edges if not e.is_manifold)
            bm.free()
            if nm:
                out["nonmanifold"][slot] = nm
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
        for q in me.polygons:
            r = find(q.vertices[0])
            if r not in roots:
                roots[r] = sid
                sid += 1
            s = roots[r]
            P.append([base_v + i for i in q.vertices])
            shell.append(s)
            slot_of.append(slot)
            shell_min[s] = min(shell_min.get(s, 9.0), min(V[base_v + i].z for i in q.vertices))
    out["shells"] = sid
    tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
    normals = []
    for q in P:
        a, c, d = V[q[0]], V[q[1]], V[q[2]]
        normals.append((c - a).cross(d - a).normalized())
    where = {}
    for i, q in enumerate(P):
        n = normals[i]
        if n.length < 0.5:
            continue
        c = sum((V[k] for k in q), Vector()) / len(q)
        if max(V[k].z for k in q) < -0.004:
            continue             # wholly under the sand: never seen, so it cannot z-fight
        for pt in [c] + [c.lerp(V[k], 0.7) for k in q]:
            for _loc, nrm, idx, _dist in tree.find_nearest_range(pt, 0.004):
                if idx is not None and shell[idx] != shell[i] and abs(nrm.dot(n)) > 0.999:
                    key = (min(shell[i], shell[idx]), max(shell[i], shell[idx]))
                    where.setdefault(key, (slot_of[i], slot_of[idx], tuple(round(x, 3) for x in c)))
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
    grounded = {f2(s) for s in range(sid) if shell_min[s] <= 0.004}
    loose = [s for s in range(sid) if f2(s) not in grounded]
    out["loose"] = len(loose)
    out["loose_examples"] = sorted({(slot_of[i], round(shell_min[s], 3)) for i, s in enumerate(shell) if s in loose})[:6]
    xs, ys, zs = [v.x for v in V], [v.y for v in V], [v.z for v in V]
    out["bbox"] = (round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(min(zs), 3), round(max(zs), 2))
    return out


# ---------------------------------------------------------------- preview

def _flat_mat(name, colour, rough=0.8):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = colour + (1,)
    bsdf.inputs["Roughness"].default_value = rough
    return m


def _sand_mat():
    """The approved sand_a, 4 m a tile (the ground's UVs are world / 4 m). Preview v1: its bump,
    lit this low, drew ripples that shouted over the props, so the backdrop keeps the albedo only;
    the cove's own ground material is untouched."""
    import render_lagoon_texture_preview as T
    m = bpy.data.materials.new("preview_sand")
    T.uv_material(m, "sand_a")
    out = next(n for n in m.node_tree.nodes if n.type == "OUTPUT_MATERIAL")
    for lk in list(out.inputs["Displacement"].links):
        m.node_tree.links.remove(lk)
    return m


# (kind, seed, x, y, yaw degrees). The camera looks from +Y (the props' fronts), so +X is on the
# image's LEFT: rope coils and piles in front, spread nets and racks behind.
LINEUP = [("rope_coil", 1, -3.2, 1.8, 0), ("rope_coil", 2, -2.0, 1.9, 0), ("rope_coil", 3, -0.8, 1.8, 0),
          ("net_pile", 1, 1.0, 1.6, 0), ("net_pile", 2, 3.3, 1.6, 0),
          ("net_spread", 1, -3.4, -1.4, 0), ("net_spread", 2, -0.3, -1.4, 0),
          ("net_rack", 1, 3.6, -1.9, 0), ("net_rack", 2, 7.6, -1.9, 0)]
SHOTS = [
    # (tag, camera, target, lens)
    ("lineup", (1.8, 9.5, 3.6), (1.8, -0.6, 0.4), 28),
    ("close", (0.6, 4.6, 1.5), (0.4, 1.5, 0.2), 30),
    ("far", (1.8, 21.0, 1.25), (1.8, -0.5, 0.6), 35),
]


def _preview_scene():
    scene = bpy.context.scene
    ground = bpy.data.meshes.new("sand")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=90)
    uvl = bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        for lp in f.loops:
            lp[uvl].uv = (lp.vert.co.x / 4.0, lp.vert.co.y / 4.0)
    bm.to_mesh(ground)
    bm.free()
    ground.materials.append(_sand_mat())
    scene.collection.objects.link(bpy.data.objects.new("sand", ground))
    ref = bpy.data.meshes.new("scale_ref_1m60")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
    bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
    bm.to_mesh(ref)
    bm.free()
    ref.materials.append(_flat_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
    r = bpy.data.objects.new("scale_ref_1m60", ref)
    r.location = (-4.8, 1.2, -0.01)
    scene.collection.objects.link(r)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color = 4.5, (1.0, 0.9, 0.74)
    sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(48), 0, math.radians(150))     # from the camera side (+Y), upper left
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
    _look(scene, "AgX", ("AgX - Punchy", "Punchy"))
    return cam


def _look(scene, transform, looks):
    scene.view_settings.view_transform = transform
    for look in looks:
        try:
            scene.view_settings.look = look
            return
        except TypeError:
            continue


def _aim(cam, pos, tgt):
    cam.location = pos
    cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()


def _render(path):
    if path.exists():
        raise SystemExit(f"[props-beach] {path} exists: never overwrite a render, bump --preview")
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    print("[props-beach] preview", path)


# (texture, metres shown in the detail crop)
SWATCH_DETAIL = {"pb_net": 0.6, "pb_netopen": 0.6, "pb_rope": 0.2}


def _swatch_mat(tex, bg=(0.93, 0.86, 0.72)):
    """Unlit: exactly the albedo (alpha composited over a sand colour), for the swatch sheet."""
    m = bpy.data.materials.new(f"swatch_{tex}")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    uv = nt.nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(str(TEX / f"{tex}_albedo.png"), check_existing=True)
    img.interpolation = "Cubic"
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.inputs["A"].default_value = bg + (1,)
    nt.links.new(uv.outputs["UV"], img.inputs["Vector"])
    nt.links.new(img.outputs["Alpha"], mix.inputs["Factor"])
    nt.links.new(img.outputs["Color"], mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], em.inputs["Color"])
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return m


def _emit_mat(name, colour):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = colour + (1,)
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return m


def _quad(name, x, y, w, h, uv_span, mat, z=0.0):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new("UVMap")
    vs = [bm.verts.new((x + dx * w, y + dy * h, z)) for dx, dy in ((0, 0), (1, 0), (1, 1), (0, 1))]
    f = bm.faces.new(vs)
    for lp, (u, v) in zip(f.loops, ((0, 1), (1, 1), (1, 0), (0, 0))):
        lp[uvl].uv = (u * uv_span[0], v * uv_span[1])
    bm.to_mesh(me)
    bm.free()
    me.materials.append(mat)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def _label(text, x, y, size, mat):
    cu = bpy.data.curves.new(f"lbl_{text[:12]}", "FONT")
    cu.body = text
    cu.size = size
    cu.align_x = "CENTER"
    ob = bpy.data.objects.new(cu.name, cu)
    ob.location = (x, y, 0.01)
    cu.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def swatch_sheet(path):
    """Every pb_ texture flat and unlit, over a warm board: a DETAIL crop (the metres in the label)
    and a 2 x 2 REPEAT of the whole 2 m tile under it, so any seam in the tiling shows."""
    scene = bpy.context.scene
    for o in scene.objects:
        o.hide_render = True
    ox = 1000.0
    board = _emit_mat("swatch_board", (0.95, 0.93, 0.88))
    ink = _emit_mat("swatch_ink", (0.2, 0.13, 0.08))
    cell, gap = 1.0, 0.25
    n = len(TEXTURES)
    Wd = n * (cell + gap) + gap
    Ht = 2 * (cell + gap + 0.15) + gap
    _quad("swatch_bg", ox - 0.2, -0.2, Wd + 0.4, Ht + 0.4, (1, 1), board, z=-0.01)
    for i, tex in enumerate(TEXTURES):
        x0 = ox + gap + i * (cell + gap)
        m = _swatch_mat(tex)
        span = SWATCH_DETAIL[tex] / UV_METRES
        y1 = Ht - (cell + gap + 0.1)
        _quad(f"sw_{tex}_d", x0, y1, cell, cell, (span, span), m)
        _label(f"{tex}  ({SWATCH_DETAIL[tex]:g} m crop)", x0 + cell / 2, y1 - 0.12, 0.075, ink)
        y2 = y1 - (cell + gap + 0.1)
        _quad(f"sw_{tex}_r", x0, y2, cell, cell, (2, 2), m)
        _label("2 x 2 tiles (4 m)", x0 + cell / 2, y2 - 0.12, 0.075, ink)
    cam = bpy.data.objects.new("swatch_cam", bpy.data.cameras.new("swatch_cam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = max(Wd, Ht) + 0.6
    scene.collection.objects.link(cam)
    cam.location = (ox + Wd / 2, Ht / 2 - 0.15, 10.0)
    cam.rotation_euler = (0, 0, 0)
    scene.camera = cam
    scene.render.resolution_x = 1600
    scene.render.resolution_y = int(1600 * (Ht + 0.6) / (Wd + 0.6))
    _look(scene, "Standard", ("None",))
    _render(path)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    _ensure_textures()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    top = bpy.data.collections.new("lagoon_props_beach")
    bpy.context.scene.collection.children.link(top)
    for kind, seed, x, y, yaw in LINEUP:
        col = build_prop(kind, seed)
        top.children.link(col)
        c = check_prop(col)
        root = next(o for o in col.objects if o.parent is None)
        info = {k[5:]: (round(v, 2) if isinstance(v, float) else (tuple(v) if hasattr(v, "__len__") and not isinstance(v, str) else v))
                for k, v in root.items() if k.startswith("prop_")}
        print(f"[props-beach] {kind} {seed}: {info}")
        print(f"[props-beach]   {c}")
        root.location = (x, y, 0.0)
        root.rotation_euler = (0, 0, math.radians(yaw))
    # Every seed 1..8 of every kind is checked too, not only the lineup's.
    bad, tris = [], {}
    for kind in KINDS:
        for seed in range(1, 9):
            col = build_prop(kind, seed)
            c = check_prop(col)
            tris.setdefault(kind, []).append(c["tris"])
            if c["coplanar"] or c["loose"] or c["nonmanifold"]:
                bad.append((kind, seed, c["coplanar"], c["coplanar_examples"][:2], c["loose"], c["loose_examples"][:2],
                            c["nonmanifold"]))
            for o in list(col.objects):
                me = o.data if o.type == "MESH" else None
                bpy.data.objects.remove(o)
                if me is not None:
                    bpy.data.meshes.remove(me)
            bpy.data.collections.remove(col)
    # (unlinked collections are not evaluated, so these are the base meshes' tris, before bevels)
    print("[props-beach] base tris, seeds 1..8:", {k: (min(v), max(v)) for k, v in tris.items()})
    print("[props-beach] seed sweep 1..8, failures:", bad if bad else "none")
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "lagoon_props_beach.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = out.with_suffix(".blend1")
    if backup.exists():
        backup.unlink()
    print("[props-beach] saved", out)
    if not version:
        return
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    cam = _preview_scene()
    for tag, pos, tgt, lens in SHOTS:
        cam.data.lens = lens
        _aim(cam, pos, tgt)
        _render(PREVIEWS / f"propsbeach_{tag}_v{version}.png")
    swatch_sheet(PREVIEWS / f"propsbeach_swatches_v{version}.png")


if __name__ == "__main__":
    if bpy is None:
        args = sys.argv[1:]
        if "--paint" in args:
            i = args.index("--paint")
            names = args[i + 1].split(",") if i + 1 < len(args) and not args[i + 1].startswith("--") else None
            paint(names)
        else:
            print(__doc__)
    else:
        main()
