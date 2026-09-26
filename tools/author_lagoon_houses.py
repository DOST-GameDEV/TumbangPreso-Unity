"""Lagoon Court STILT HOUSE KIT: the final house models (geometry and world-scale UVs; no texture).

  blender -b --python tools/author_lagoon_houses.py -- --preview N

Writes ArtSource/lagoon/lagoon_houses.blend (the LINEUP, every house checked) and, with --preview N,
six renders in Logs/lagoon-blender/: housekit_lineup_vN.png (flat placeholder colours),
housekit_close_vN.png, _closew_vN.png and _closec_vN.png (a land house, a water home and the
capilla up close), and housekit_checker_vN.png and _checkerclose_vN.png (a checker on "UVMap"
per material, to prove the world-scale unwrap: square cells, thatch rows level). An existing
filename is never overwritten: bump N.

Importable with no side effects: author_lagoon_cove.py calls `build_house(kind, seed)` in place of
the blockout `house()` and `water_home()`, and gets back a Collection (not linked anywhere; the
caller links it) holding one root empty named by kind with every part parented to it.

KINDS AND CONVENTIONS (docs/LAGOON_REWORK_GUIDE.md § 8 step 3):

  * "land"    a house on a pocket. Room 7 to 9 m x 5.5 to 7 m, floor 0.9 m on short bamboo piles,
              a front deck with a bamboo railing and three steps, a steep hip or gable thatch roof.
  * "water"   a small one-room Sama-Bajau home. Room 4.5 to 6 m x 4 to 5 m, floor 1.8 to 2.1 m
              over the water on tall bamboo piles with X bracing, a small deck with a ladder down
              to the water, and (most seeds) a laundry line.
  * "stall"   a sari-sari store: a small room with a wide front counter opening, a propped
              awning shutter over it and a bamboo grille.
  * "capilla" a small white chapel: a one-piece nave shell, a front bell tower with an open
              belfry and a bell, pitched red tin roofs, a white plinth and steps.

  ORIGIN: the root empty sits at the centre of the ROOM's footprint (not the deck) at the
  ground / water contact, z = 0. The front door faces +Y; decks, steps and ladders are on +Y.
  Land, stall and capilla: z = 0 is the ground, and posts, stringers and the plinth sink into it.
  Water: z = 0 is the WATER SURFACE; every pile runs down to z = -PILE_DEPTH (2.0 m), so a home
  placed with its origin on the water plane reaches a seabed up to 2 m down (the cove's is 1.7 m:
  WATER -1.8, SEABED -3.5). The ladder ends 0.35 m under the surface.

MATERIAL SLOTS (exact names, one material per object, so textures swap in later): thatch, sawali,
timber, plank, bamboo, tin, paint_white, capiz, and cloth (laundry only). One object per slot per
house, parented to the root, each with a live Bevel modifier.

UVs: one map "UVMap", WORLD SCALE, 1 UV unit = 2 m everywhere (UV_METRES):
  * thatch: U along the eave, V UP THE SLOPE, per roof face, so thatch rows lie level on every
    face (hip ends included); the ridge and hip rolls run U along their length.
  * sawali: U horizontal, V vertical, in the wall's plane (shutters: V down the shutter).
  * plank, timber, bamboo: V along the member's length, U across it (round members: U around).
  * tin: like thatch, U along the eave, V up the slope. paint_white: box projection, V vertical.
  Every piece gets a random UV offset, so neighbouring planks never show the same stretch of a
  tiling texture.

What the owner's references ask for, and so what this builds:

  * CUTE AND CHUNKY, NOT A SURVEY DRAWING (Art_Direction.md § 0 and the ArtStation fishing
    village): stocky rooms under OVERSIZED deep-eaved roofs, thick members, and nothing ruler
    perfect: piles and posts lean a degree or two, bamboo varies in girth, ridges sag, planks vary
    in width and the thatch hangs in a shaggy wave.
  * THATCH THAT READS AS THATCH BEFORE IT HAS A TEXTURE: three to five stepped courses, each a
    thick slab lying over the one below with a rounded lip and a wavy lower edge, a fat ridge
    roll, and a thick eave course so the roof shows its depth from below.
  * SAWALI IN A TIMBER FRAME: every bay is a woven panel inset behind corner posts, a sill, a mid
    rail and a top plate, the way the photographs' houses are built.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2). Every member that meets another
penetrates it, and every pair of members that could end up in one plane is given a different
depth or height: the walls along X and the walls along Y carry their sills, rails and plates 2 cm
apart, posts are deeper than sills, sills deeper than rails, panels thinner than all of them, and
two members that meet end to end inside a post stop short of each other. `check_house` proves it
(`coplanar` counts overlapping parallel faces from different shells within 4 mm).
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector, noise
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "lagoon"
PREVIEWS = ROOT / "Logs" / "lagoon-blender"

KINDS = ("land", "water", "stall", "capilla")
UV_METRES = 2.0       # 1 UV unit = 2 m: every tiling texture in this kit covers 2 m
PILE_DEPTH = 2.0      # water homes: piles run from the floor down to z = -PILE_DEPTH
GROUND_SINK = 0.35    # land piles, deck piles and stair stringers run this far into the ground
# How far the top thatch course's top skin runs past the ridge line, along the slope. Past the
# ridge each top course stands up as a short fin over the other face; the ridge roll has to
# swallow it, so the overrun stays small.
RIDGE_OVER = 0.1
Z = Vector((0.0, 0.0, 1.0))

# Placeholder colours, one per slot, flat; the texture pass replaces them. Red tin, not teal:
# the guide's role-hue rule keeps roofs away from defence blue.
SLOTS = {
    "thatch": (0.66, 0.50, 0.26),
    "sawali": (0.84, 0.72, 0.48),
    "timber": (0.40, 0.25, 0.14),
    "plank": (0.60, 0.42, 0.25),
    "bamboo": (0.72, 0.64, 0.36),
    "tin": (0.62, 0.22, 0.16),
    "paint_white": (0.94, 0.92, 0.87),
    "capiz": (0.93, 0.89, 0.76),
    "cloth": (0.86, 0.45, 0.40),
}
# Per slot: (bevel width m, angle above which an edge is sharp, harden normals). Bamboo keeps its
# eight sides smooth (45 degrees between faces) so a pile shades round, not as a pencil.
FINISH = {
    "thatch": (0.030, 40.0, False),
    "sawali": (0.008, 30.0, True),
    "timber": (0.016, 30.0, True),
    "plank": (0.012, 30.0, True),
    "bamboo": (0.008, 50.0, False),
    "tin": (0.005, 40.0, False),
    "paint_white": (0.030, 30.0, True),
    "capiz": (0.005, 30.0, True),
    "cloth": (0.004, 30.0, False),
}


# ---------------------------------------------------------------- pieces

class Piece:
    """One closed shell under construction, in its own bmesh with its own UV layer, so every
    primitive can be cut, capped and unwrapped on its own before it joins its slot's mesh."""

    def __init__(self, bm=None):
        self.bm = bm or bmesh.new()
        self.uv = self.bm.loops.layers.uv.get("UVMap") or self.bm.loops.layers.uv.new("UVMap")


class House:
    """Collects pieces per slot for one house. `M` transforms every piece added while it is set
    (the capilla builds its roof with the ridge along X and turns it)."""

    def __init__(self, kind, seed):
        self.kind, self.seed = kind, seed
        self.rng = random.Random(f"lagoon-house:{kind}:{seed}")
        self.pieces = {s: [] for s in SLOTS}
        self.roof = []
        self.M = None
        self.info = {}

    def add(self, slot, pc, roof=False):
        if not pc.bm.faces:
            pc.bm.free()
            return None
        if self.M is not None:
            bmesh.ops.transform(pc.bm, matrix=self.M, verts=pc.bm.verts[:])
        self.pieces[slot].append(pc)
        if roof:
            self.roof.append(pc)
        return pc

    def j(self, a):
        return self.rng.uniform(-a, a)

    def uv_off(self):
        return (self.rng.random(), self.rng.random())


def _grid_solid(top, bot):
    """A closed solid from two matching grids of points, top[r][i] and bot[r][i] (rows r, columns
    i): top and bottom skins, joined round all four sides. Used for boxes, thatch courses and
    corrugated tin alike, so every one of them is manifold by construction."""
    pc = Piece()
    bm = pc.bm
    R, N = len(top), len(top[0])
    T = [[bm.verts.new(q) for q in row] for row in top]
    B = [[bm.verts.new(q) for q in row] for row in bot]
    for r in range(R - 1):
        for i in range(N - 1):
            bm.faces.new((T[r][i], T[r][i + 1], T[r + 1][i + 1], T[r + 1][i]))
            bm.faces.new((B[r][i], B[r + 1][i], B[r + 1][i + 1], B[r][i + 1]))
    for i in range(N - 1):
        bm.faces.new((B[0][i], B[0][i + 1], T[0][i + 1], T[0][i]))
        bm.faces.new((T[R - 1][i], T[R - 1][i + 1], B[R - 1][i + 1], B[R - 1][i]))
    for r in range(R - 1):
        bm.faces.new((T[r][0], T[r + 1][0], B[r + 1][0], B[r][0]))
        bm.faces.new((B[r][N - 1], B[r + 1][N - 1], T[r + 1][N - 1], T[r][N - 1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return pc


def _uv_frame(pc, U, V, W, off=(0.0, 0.0)):
    """Box projection in a LOCAL frame (U, V, W): a face picks the frame axis its normal is
    closest to and is projected on the other two, keeping V on V wherever it can. So a beam built
    along V has V along its length on all four sides, a wall panel built with U horizontal and V
    up is U horizontal and V up on both faces, and a thatch course in its roof's frame has V up
    the slope on its top and its lip. World scale: 1 unit = UV_METRES."""
    s = 1.0 / UV_METRES
    pc.bm.normal_update()
    for f in pc.bm.faces:
        n = f.normal
        cu, cv, cw = n.dot(U), n.dot(V), n.dot(W)
        au, av, aw = abs(cu), abs(cv), abs(cw)
        for loop in f.loops:
            q = loop.vert.co
            if aw >= au and aw >= av:
                u, v = q.dot(U) * (1 if cw >= 0 else -1), q.dot(V)
            elif au >= av:
                u, v = q.dot(W) * (-1 if cu >= 0 else 1), q.dot(V)
            else:
                u, v = q.dot(U) * (-1 if cv >= 0 else 1), q.dot(W)
            loop[pc.uv].uv = (u * s + off[0], v * s + off[1])


def _uv_world(pc, off=(0.0, 0.0)):
    """World box projection with V vertical on walls (the capilla's painted shell)."""
    _uv_frame(pc, Vector((1, 0, 0)), Z, Vector((0, 1, 0)), off)


def _perp(d):
    a = Z if abs(d.z) < 0.9 else Vector((1, 0, 0))
    e1 = a.cross(d).normalized()
    return e1, d.cross(e1).normalized()


def beam(h, slot, p0, p1, w, t, up=Z, roof=False):
    """A box member from p0 to p1: `w` across (horizontal when `up` is Z), `t` along `up`. V runs
    along the member."""
    p0, p1 = Vector(p0), Vector(p1)
    d = (p1 - p0).normalized()
    side = up.cross(d)
    if side.length < 1e-6:
        side = Vector((1, 0, 0)).cross(d)
    side.normalize()
    upv = d.cross(side)

    def P(q, i, k):
        return q + side * (i * w / 2) + upv * (k * t / 2)
    top = [[P(p0, -1, 1), P(p0, 1, 1)], [P(p1, -1, 1), P(p1, 1, 1)]]
    bot = [[P(p0, -1, -1), P(p0, 1, -1)], [P(p1, -1, -1), P(p1, 1, -1)]]
    pc = _grid_solid(top, bot)
    _uv_frame(pc, side, d, upv, h.uv_off())
    return h.add(slot, pc, roof)


def tube(h, slot, p0, p1, r, sides=8, nodes=0.0, wobble=0.0, seg=0.0, taper=1.0, along_u=False,
         roof=False, rings=None):
    """A round member from p0 to p1 (`r` a radius or an (across, up) pair). `nodes` puts a bamboo
    node ring every that many metres; `wobble` and `seg` swell the girth unevenly (the thatch
    rolls); `taper` scales the far end. UVs: V along the length and U around it (or the other
    way with `along_u`, for the rolls, so their thatch rows run along them)."""
    p0, p1 = Vector(p0), Vector(p1)
    axis = p1 - p0
    L = axis.length
    d = axis / L
    e1, e2 = _perp(d)
    rx, ry = (r, r) if not isinstance(r, tuple) else r
    if rings is None:
        rings = [(0.0, 1.0), (L, taper)]
        if nodes:
            k = nodes * (0.5 + 0.3 * h.rng.random())
            while k < L - 0.12:
                f = 1.0 + (taper - 1.0) * k / L
                rings += [(k - 0.04, f), (k, f * 1.13), (k + 0.04, f)]
                k += nodes * h.rng.uniform(0.85, 1.15)
        if seg:
            n = max(2, int(L / seg))
            seed = h.rng.uniform(0, 100)
            for i in range(1, n):
                t = L * i / n
                rings.append((t, 1.0 + wobble * noise.noise(Vector((t * 1.3, seed, 0.5)))))
        rings.sort()
    else:
        rings = [(t * L, f) for t, f in rings]
    pc = Piece()
    bm, uv = pc.bm, pc.uv
    # Vertices near the frame axes (give or take 8 degrees at random), so the facet NORMALS sit
    # half a facet off them and never face straight along X, Y or Z: a facet facing along Y lay
    # flat against a girder side a millimetre away (v3), and two neighbouring tubes with one
    # phase have parallel side faces.
    ph = h.j(0.15) if rx == ry else 0.0
    ring_v = []
    for t, f in rings:
        c = p0 + d * t
        ring_v.append([bm.verts.new(c + e1 * (math.cos(a) * rx * f) + e2 * (math.sin(a) * ry * f))
                       for a in (ph + math.tau * k / sides for k in range(sides))])
    s = 1.0 / UV_METRES
    arc = math.tau * (rx + ry) * 0.5 / sides
    ou, ov = h.uv_off()
    for j in range(len(rings) - 1):
        for k in range(sides):
            k2 = (k + 1) % sides
            f = bm.faces.new((ring_v[j][k], ring_v[j][k2], ring_v[j + 1][k2], ring_v[j + 1][k]))
            corners = ((k, rings[j][0]), (k + 1, rings[j][0]), (k + 1, rings[j + 1][0]), (k, rings[j + 1][0]))
            for loop, (kk, tt) in zip(f.loops, corners):
                a, b = kk * arc * s + ou, tt * s + ov
                loop[uv].uv = (b, a) if along_u else (a, b)
    for ring, flip in ((ring_v[0], True), (ring_v[-1], False)):
        f = bm.faces.new(list(reversed(ring)) if flip else ring)
        for loop in f.loops:
            q = loop.vert.co
            loop[uv].uv = (q.dot(e1) * s + ou, q.dot(e2) * s + ov)
    return h.add(slot, pc, roof)


def extrude(h, slot, pts, vec, frame, roof=False, world=False):
    """A flat polygon `pts` (a planar loop) pushed through `vec`: gable panels, arched panes,
    the capilla's shell. `frame` = (U, V, W) for the unwrap."""
    pc = Piece()
    bm = pc.bm
    a = [bm.verts.new(Vector(p)) for p in pts]
    b = [bm.verts.new(Vector(p) + vec) for p in pts]
    bm.faces.new(a)
    bm.faces.new(b)
    n = len(pts)
    for i in range(n):
        k = (i + 1) % n
        bm.faces.new((a[i], a[k], b[k], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    if world:
        _uv_world(pc, h.uv_off())
    else:
        _uv_frame(pc, *frame, off=h.uv_off())
    return h.add(slot, pc, roof) if slot else pc


def _cut(pc, co, no):
    """Cut a piece with a plane, drop the positive side and cap the hole flat (the hips)."""
    bm = pc.bm
    bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=co, plane_no=no,
                           clear_outer=True, dist=0.002)
    boundary = [e for e in bm.edges if len(e.link_faces) == 1]
    if boundary:
        bmesh.ops.holes_fill(bm, edges=boundary)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])


def _boolean(pc, cutters):
    """Subtract closed cutter pieces from a closed piece with the EXACT boolean, through a
    temporary object that is removed again (build_house leaves nothing in the scene)."""
    scene = bpy.context.scene
    temp = []

    def obj(p, name):
        me = bpy.data.meshes.new(name)
        p.bm.to_mesh(me)
        o = bpy.data.objects.new(name, me)
        scene.collection.objects.link(o)
        temp.append(o)
        return o
    base = obj(pc, "_house_bool_base")
    for k, c in enumerate(cutters):
        co = obj(c, "_house_bool_cut")
        c.bm.free()
        m = base.modifiers.new(f"cut{k}", "BOOLEAN")
        m.operation, m.solver, m.object = "DIFFERENCE", "EXACT", co
    # ⚠️ update(): once anything has evaluated the scene (check_house does, for the bevelled tri
    # count), the depsgraph handed back is the OLD one and does not yet hold these temporary
    # objects, so without this the boolean silently returns the uncut shell (capilla v1: no
    # windows, no door).
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    res = bpy.data.meshes.new_from_object(base.evaluated_get(dg))
    for o in temp:
        me = o.data
        bpy.data.objects.remove(o)
        bpy.data.meshes.remove(me)
    pc.bm.free()
    bm = bmesh.new()
    bm.from_mesh(res)
    bpy.data.meshes.remove(res)
    return Piece(bm)


def arch_poly(w, hgt, n=7):
    """(x, z) of an opening w wide and hgt tall whose top is a half circle, bottom centre at 0."""
    r = w / 2
    pts = [(-r, 0.0), (r, 0.0)]
    for k in range(n + 1):
        a = math.pi * k / n
        pts.append((math.cos(a) * r, hgt - r + math.sin(a) * r))
    return pts


# ---------------------------------------------------------------- piles, floors, decks

def floor_platform(h, W, D, F, piles_to, tall):
    """The raised floor: a plank floor slab, three timber girders along X under it and a grid of
    bamboo piles under the girders, leaning a little each. Water homes (`tall`) get X bracing
    between neighbouring piles round the outside, like the photographs' Bajau homes."""
    beam(h, "plank", (-W / 2 - 0.15, 0, F - 0.07), (W / 2 + 0.15, 0, F - 0.07), D + 0.3, 0.14)
    nx = max(2, round(W / 2.1)) + 1
    xs = [-W / 2 + 0.05 + (W - 0.1) * i / (nx - 1) for i in range(nx)]
    ys = [-D / 2 + 0.05, 0.0, D / 2 - 0.05]
    for y in ys:
        beam(h, "timber", (-W / 2 - 0.12, y, F - 0.23), (W / 2 + 0.12, y, F - 0.23), 0.18, 0.22)
    tops = {}
    for y in ys:
        for x in xs:
            if y == 0.0 and x not in (xs[0], xs[-1]) and not tall:
                continue      # a land house's middle row needs only its end piles
            foot = Vector((x + h.j(0.12), y + h.j(0.12), piles_to))
            top = Vector((x, y, F - 0.28))
            r = (0.11 if tall else 0.10) * h.rng.uniform(0.85, 1.12)
            tube(h, "bamboo", foot, top, r, nodes=0.75, taper=0.9)
            tops[(x, y)] = (foot, top)
    if tall:
        lo, hi = 0.35, F - 0.55
        ring = [(x, ys[0]) for x in xs] + [(xs[-1], ys[1])] + [(x, ys[2]) for x in reversed(xs)] + [(xs[0], ys[1])]
        for a, b in zip(ring, ring[1:] + ring[:1]):
            fa, ta = tops[a]
            fb, tb = tops[b]
            out = Vector((a[0] + b[0], a[1] + b[1], 0)).normalized() * 0.05

            def at(foot, top, z):
                return foot.lerp(top, (z - foot.z) / (top.z - foot.z))
            tube(h, "bamboo", at(fa, ta, lo) + out, at(fb, tb, hi) + out, 0.045)
            tube(h, "bamboo", at(fa, ta, hi) - out, at(fb, tb, lo) - out, 0.045)
    return xs


def deck(h, W, D, F, depth, width, x0, piles_to, gaps, steps=None, ladder=None):
    """A plank deck on the +Y side: timber bearers from the floor girder out, bamboo piles at the
    outer edge, planks along X of uneven width, and a bamboo railing round the three open sides
    with the openings named in `gaps` ("front-left", "front-right", "front-mid")."""
    y0, y1 = D / 2 + 0.15, D / 2 + depth
    xa, xb = x0 - width / 2, x0 + width / 2
    top = F - 0.035
    nb = max(2, round(width / 1.6)) + 1
    bxs = [xa + 0.1 + (width - 0.2) * i / (nb - 1) for i in range(nb)]
    for x in bxs:
        # Bearer tops 2.5 cm up inside the planks: a plank laid ON a bearer puts its underside
        # within millimetres of the bearer's top, one plane (v2's coplanar check found every one).
        beam(h, "timber", (x, D / 2 - 0.1, F - 0.16), (x, y1 + 0.08, F - 0.16), 0.14, 0.18)
        foot = Vector((x + h.j(0.1), y1 - 0.12 + h.j(0.08), piles_to))
        tube(h, "bamboo", foot, (x, y1 - 0.12, F - 0.2), 0.09 * h.rng.uniform(0.9, 1.1), nodes=0.75, taper=0.9)
    y = y0 + 0.012
    while y < y1 - 0.08:
        w = min(h.rng.uniform(0.24, 0.32), y1 - y)
        z = top - 0.03 + h.j(0.006)
        beam(h, "plank", (xa - 0.04 + h.j(0.03), y + w / 2, z), (xb + 0.04 + h.j(0.03), y + w / 2, z), w - 0.018, 0.06)
        y += w
    # Railing: posts round the open sides, a top and a mid rail between neighbours.
    rail_top = F + 0.92
    front = [(xa + 0.05, y1 - 0.05)]
    k = max(1, round(width / 1.3))
    for i in range(1, k):
        front.append((xa + 0.05 + (width - 0.1) * i / k, y1 - 0.05))
    front.append((xb - 0.05, y1 - 0.05))
    left = [(xa + 0.05, y0 + 0.05)]
    right = [(xb - 0.05, y0 + 0.05)]
    posts = left + front + right
    for x, y in posts:
        tube(h, "bamboo", (x, y, F - 0.22), (x + h.j(0.02), y + h.j(0.02), rail_top + 0.08), 0.065, nodes=0.45)
    runs = [(left[0], front[0], "left"), (front[-1], right[0], "right")]
    runs += [(front[i], front[i + 1], f"front-{i}") for i in range(len(front) - 1)]
    mid = (len(front) - 1) // 2
    for a, b, name in runs:
        if name.startswith("front-"):
            i = int(name[6:])
            if ("front-left" in gaps and i == 0) or ("front-right" in gaps and i == len(front) - 2) \
                    or ("front-mid" in gaps and i == mid):
                continue
        # Each rail stops 2 cm short of both post centres: two runs meeting at one post would
        # otherwise put their end caps back to back in one plane inside it.
        va, vb = Vector((a[0], a[1], 0)), Vector((b[0], b[1], 0))
        dv = (vb - va).normalized() * 0.02
        for z in (rail_top, F + 0.46):
            tube(h, "bamboo", va + dv + Z * (z + h.j(0.02)), vb - dv + Z * (z + h.j(0.02)), 0.05)
    h.info["deck"] = (xa, xb, y1, front, mid)
    return front, y1


def stairs(h, x, y_top, F, n, width=1.15):
    """Three steps down from a deck edge to the ground: two timber stringers into the ground, and
    plank treads let into them."""
    run = 0.34
    y_bot = y_top + run * n
    for s in (-1, 1):
        xs = x + s * (width / 2 + 0.03)
        beam(h, "timber", (xs, y_top - 0.12, F - 0.12), (xs, y_bot + 0.1, -0.25), 0.07, 0.24, up=Vector((0, 0.7, 0.7)).normalized())
    for k in range(1, n + 1):
        zt = F * (n + 1 - k) / (n + 1) - 0.02
        yc = y_top + run * (k - 0.5)
        beam(h, "plank", (x - width / 2 - 0.02, yc, zt - 0.03), (x + width / 2 + 0.02, yc, zt - 0.03), run - 0.04, 0.06)


# ---------------------------------------------------------------- walls

def wall_frame(h, W, D, F, H, bays):
    """Sawali panels set in a timber frame, all four walls. `bays[wall]` lists (width, type) with
    type "panel", "window", "capiz", "shutter", "door" or "counter" ("window" picks capiz or a
    shutter). Walls along X are "a" walls, walls along Y "b" walls; their sills, rails and plates
    sit 2 cm apart so no two of them ever share a plane where they cross at a corner."""
    walls = {
        "front": (Vector((-W / 2, D / 2, 0)), Vector((1, 0, 0)), Vector((0, 1, 0)), W, 0),
        "right": (Vector((W / 2, D / 2, 0)), Vector((0, -1, 0)), Vector((1, 0, 0)), D, 1),
        "back": (Vector((W / 2, -D / 2, 0)), Vector((-1, 0, 0)), Vector((0, -1, 0)), W, 0),
        "left": (Vector((-W / 2, -D / 2, 0)), Vector((0, 1, 0)), Vector((-1, 0, 0)), D, 1),
    }
    for sx in (-1, 1):
        for sy in (-1, 1):
            beam(h, "timber", (sx * W / 2, sy * D / 2, F - 0.06), (sx * W / 2 + h.j(0.015), sy * D / 2 + h.j(0.015), F + H + 0.03),
                 0.18, 0.18, up=Vector((0, 1, 0)))
    for name, (A, t, n, L, ax) in walls.items():
        dz = -0.02 * ax
        sill = (F - 0.03 + dz, F + 0.11 + dz)
        rail_c, head_c = F + 0.95 + dz, F + 2.02 + dz
        plate = (F + H - 0.08 + dz, F + H + 0.10 + dz)
        dep = 1.0 - 0.06 * ax    # the "b" walls' members are 6 % shallower

        def P(x, z, nn=0.0):
            return A + t * x + n * nn + Z * z

        def member(x0, x1, z0, z1, depth, slot="timber"):
            beam(h, slot, P(x0, (z0 + z1) / 2), P(x1, (z0 + z1) / 2), depth, z1 - z0, up=Z)

        def panel(x0, x1, z0, z1):
            beam(h, "sawali", P((x0 + x1) / 2, z0), P((x0 + x1) / 2, z1), x1 - x0, 0.05, up=n)
        member(0.02, L - 0.02, *sill, 0.16 * dep)
        member(-0.12, L + 0.12, *plate, 0.20 * dep)
        spec = bays[name]
        total = sum(w for w, _ in spec)
        xs = [0.0]
        for w, _ in spec:
            xs.append(xs[-1] + w * L / total)
        for x in xs[1:-1]:
            # From 8 cm down inside the floor slab: ending at F - 0.05 put the post's foot in the
            # "b" walls' sill underside plane.
            beam(h, "timber", P(x, F - 0.08), P(x + h.j(0.01), F + H + 0.02), 0.15, 0.17, up=n)
        for k, (_w, kind) in enumerate(spec):
            a = xs[k] + (0.09 if k == 0 else 0.075)
            b = xs[k + 1] - (0.09 if k == len(spec) - 1 else 0.075)
            r0, r1 = xs[k] + 0.03, xs[k + 1] - 0.03
            if kind == "window":
                kind = "capiz" if h.rng.random() < 0.5 else "shutter"
            if kind == "panel":
                member(r0, r1, rail_c - 0.06, rail_c + 0.06, 0.13 * dep)
                panel(a - 0.04, b + 0.04, sill[1] - 0.03, rail_c - 0.02)
                panel(a - 0.04, b + 0.04, rail_c + 0.02, plate[0] + 0.03)
                continue
            if kind in ("capiz", "shutter", "counter"):
                rc = rail_c + (0.05 if kind == "counter" else 0.0)
                member(r0, r1, rc - 0.06, rc + 0.06, 0.13 * dep)
                panel(a - 0.04, b + 0.04, sill[1] - 0.03, rc - 0.02)
                lo = rc + 0.06
            else:   # door
                lo = F
            hc = head_c + (0.08 if kind == "counter" else 0.0)
            member(r0, r1, hc - 0.07, hc + 0.07, 0.14 * dep)
            panel(a - 0.04, b + 0.04, hc + 0.02, plate[0] + 0.03)
            hi = hc - 0.07
            if kind == "capiz":
                capiz_sash(h, P, a, (a + b) / 2 + 0.03, lo, hi, n)
            elif kind == "shutter":
                shutter(h, P, a, b, lo, hi, n, t, math.radians(h.rng.uniform(28, 42)), prop_from=rail_c + 0.06)
            elif kind == "counter":
                counter(h, P, a, b, lo, hi, n, t)
            else:
                door_leaf(h, P, a, b, lo, hi, n, t)
    h.info["plate_top"] = F + H + 0.10


def frame_ring(h, slot, o, U, V, W, u0, u1, v0, v1, bar, depth):
    """A rectangular frame as ONE closed shell (a flat ring): outer rectangle [u0, u1] x [v0, v1]
    in the plane (U, V) at `o`, a `bar` wide border, `depth` thick along W. Built as one piece
    rather than four members because four crossing members always leave two of them sharing a
    face plane at the corners (v2 and v3: sash stiles and rails, shutter stiles and rails). UVs:
    V along each bar's own length."""
    pc = Piece()
    bm, uv = pc.bm, pc.uv
    ou = [(u0, v0), (u1, v0), (u1, v1), (u0, v1)]
    iu = [(u0 + bar, v0 + bar), (u1 - bar, v0 + bar), (u1 - bar, v1 - bar), (u0 + bar, v1 - bar)]

    def vert(p, w):
        return bm.verts.new(o + U * p[0] + V * p[1] + W * w)
    fo = [vert(p, depth / 2) for p in ou]
    fi = [vert(p, depth / 2) for p in iu]
    bo = [vert(p, -depth / 2) for p in ou]
    bi = [vert(p, -depth / 2) for p in iu]
    s = 1.0 / UV_METRES
    ou_, ov_ = h.uv_off()
    for k in range(4):
        k2 = (k + 1) % 4
        along_u = k in (0, 2)          # bottom and top bars run along U
        for quad in ((fo[k], fo[k2], fi[k2], fi[k]), (bo[k2], bo[k], bi[k], bi[k2]),
                     (bo[k], bo[k2], fo[k2], fo[k]), (fi[k], fi[k2], bi[k2], bi[k])):
            f = bm.faces.new(quad)
            f.normal_update()
            for loop in f.loops:
                q = loop.vert.co - o
                a, b, c = q.dot(U), q.dot(V), q.dot(W)
                if abs(f.normal.dot(W)) > 0.5:
                    uvv = (b, a) if along_u else (a, b)
                else:
                    uvv = (c, a) if along_u else (c, b)
                loop[uv].uv = (uvv[0] * s + ou_, uvv[1] * s + ov_)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return h.add(slot, pc)


def capiz_sash(h, P, x0, x1, z0, z1, n):
    """A sliding capiz window drawn half open: a timber sash (one ring) with a muntin grid over
    a capiz pane, standing 10 cm inside the wall line with one stile let into the post.

    Every part has its own DEPTH (ring 5.6 cm, upright muntin 3.6, cross muntins 2.6, pane 1),
    and 10 cm in keeps all of them clear of the wall members' faces (posts 17 or 18 cm deep,
    rails 12 to 13, heads 13 to 14): at 4.5 cm in, the sash's faces fell within millimetres of
    the rails' and heads' (v3)."""
    nn = -0.105
    o = P(0, 0, nn)
    t = (P(1, 0, nn) - o)
    frame_ring(h, "timber", o, t, Z, n, x0 - 0.02, x1, z0 - 0.02, z1 + 0.02, 0.06, 0.056)
    beam(h, "capiz", P((x0 + x1) / 2, z0 + 0.015, nn), P((x0 + x1) / 2, z1 - 0.015, nn), x1 - x0 - 0.04, 0.01, up=n)
    beam(h, "timber", P((x0 + x1) / 2 - 0.01, z0 + 0.025, nn), P((x0 + x1) / 2 - 0.01, z1 - 0.025, nn), 0.035, 0.036, up=n)
    for f in (1 / 3, 2 / 3):
        z = z0 + (z1 - z0) * f
        beam(h, "timber", P(x0 + 0.01, z, nn), P(x1 - 0.01, z, nn), 0.026, 0.035, up=Z)   # past the pane's edges


def shutter(h, P, x0, x1, z0, z1, n, t, ang, prop_from):
    """A sawali shutter in a timber frame (one ring), hinged at the head and propped out on a
    bamboo stick (the owner's stilt-village photograph)."""
    hinge_z = z1 + 0.03
    down = (-Z * math.cos(ang) + n * math.sin(ang)).normalized()
    face = t.cross(down).normalized()   # the shutter's own normal
    hgt = z1 - z0 + 0.06
    o = P(0, hinge_z, 0.08)

    def S(x, s):
        return o + t * x + down * s
    xa, xb = x0 - 0.04, x1 + 0.04
    frame_ring(h, "timber", o, t, down, face, xa, xb, -0.035, hgt, 0.065, 0.06)
    beam(h, "sawali", S((xa + xb) / 2, 0.0), S((xa + xb) / 2, hgt - 0.035), xb - xa - 0.07, 0.035, up=face)
    tip = S((xa + xb) / 2 + 0.2, hgt - 0.03) - face * 0.03
    tube(h, "bamboo", P((xa + xb) / 2 + 0.2, prop_from - 0.02, 0.0), tip, 0.022)


def door_leaf(h, P, x0, x1, z0, z1, n, t):
    """A plank door hinged at one jamb and left open into the room, so the doorway reads as a
    doorway (dark inside) from the court."""
    ang = math.radians(h.rng.uniform(55, 75))
    dirv = (t * math.cos(ang) - n * math.sin(ang)).normalized()
    hinge = P(x0, 0, -0.03)
    w = x1 - x0 - 0.03
    k = 3
    pw = w / k
    face = Z.cross(dirv).normalized()
    for i in range(k):
        c = hinge + dirv * (pw * (i + 0.5) - 0.02)
        beam(h, "plank", (c.x, c.y, z0 - 0.012), (c.x, c.y, z1 - 0.03), pw - 0.012, 0.045, up=face)
    for z in (z0 + 0.35, z1 - 0.35):
        a = hinge + dirv * 0.05 - face * 0.03
        b = hinge + dirv * (w - 0.06) - face * 0.03
        beam(h, "timber", (a.x, a.y, z), (b.x, b.y, z), 0.11, 0.035, up=face)


def counter(h, P, x0, x1, z0, z1, n, t):
    """The sari-sari front: a plank counter on the rail, a bamboo grille in the opening and a
    wide awning shutter propped up over it."""
    beam(h, "plank", P(x0 - 0.05, z0 + 0.02, 0.2), P(x1 + 0.05, z0 + 0.02, 0.2), 0.62, 0.06, up=Z)
    k = max(4, round((x1 - x0) / 0.3))
    for i in range(1, k):
        x = x0 + (x1 - x0) * i / k
        tube(h, "bamboo", P(x, z0 + 0.03, 0.0), P(x, z1 + 0.03, 0.0), 0.022)
    beam(h, "bamboo", P(x0 - 0.02, z0 + 0.52, 0.0), P(x1 + 0.02, z0 + 0.52, 0.0), 0.03, 0.03, up=Z)
    ang = math.radians(h.rng.uniform(100, 108))
    shutter(h, P, x0, x1, z0, z1, n, t, ang, prop_from=z0 + 0.05)


def gable_panel(h, x, D, z_bot, underside, roof_ys):
    """The triangle above a gable wall's plate: sawali, following the stepped thatch underside
    7 cm up into it, with a timber king post up the middle."""
    pts = [(x, -D / 2 + 0.02, z_bot), (x, D / 2 - 0.02, z_bot)]
    for y in roof_ys:
        if abs(y) <= D / 2 - 0.02:
            pts.append((x, y, underside(y)))
    pts = [Vector(p) for p in pts]
    pts = [pts[0], pts[1]] + sorted(pts[2:], key=lambda p: -p.y)
    pts[2:2] = [Vector((x, D / 2 - 0.02, underside(D / 2 - 0.02)))]
    pts.append(Vector((x, -D / 2 + 0.02, underside(-D / 2 + 0.02))))
    base = [p - Vector((0.025, 0, 0)) for p in pts]
    extrude(h, "sawali", base, Vector((0.05, 0, 0)), (Vector((0, 1, 0)), Z, Vector((1, 0, 0))), roof=True)
    beam(h, "timber", (x, 0, z_bot - 0.05), (x, 0, underside(0) + 0.02), 0.14, 0.13, up=Vector((1, 0, 0)), roof=True)


# ---------------------------------------------------------------- roofs

def thatch_roof(h, W, D, z_pt, pitch, ov, gov, hip, sag=0.14):
    """THE THATCH ROOF (owner's references: steep, thick, layered, shaggy at the eave).

    Every roof face is built in its own frame: E on the eave line, `a` along the eave, `bd` up
    the slope, `c` out of it. The face is three to five COURSES, each a thick slab from its wavy
    lower edge up past the next course's edge. Each course is tilted a few degrees flatter than
    the roof, so its lower edge stands proud and its upper end tucks under the next, and each is
    lifted just enough (`off`) that its lip rests 4 cm INTO the course below: a stepped roof with
    no gap and no two course faces in one plane. The lower row of every course is pushed down the
    slope by a wave plus noise (the shaggy edge), the lip is rounded (the top skin drops to 55 %
    of the thickness at the edge) and the eave course is the thickest, so the roof shows its
    depth from below. Hip faces are cut by the vertical hip planes 5 to 9 cm past the hip, so the two
    faces interpenetrate under the hip roll rather than meeting on a shared cap."""
    rng = h.rng
    p = math.radians(pitch)
    tp, cp, sp = math.tan(p), math.cos(p), math.sin(p)
    ey = D / 2 + ov
    ex = W / 2 + (ov if hip else gov)
    z_wl = z_pt - 0.12                   # base plane height over the plate's outer edge
    z_e = z_wl - (ov - 0.1) * tp
    z_r = z_e + ey * tp
    S = ey / cp
    n = max(3, min(5, round(S / 1.45)))
    step = S / n
    Lc = step + 0.5
    rise, sink = 0.12, 0.04
    T = [0.42] + [0.32] * (n - 1)
    off = [0.0]
    for i in range(1, n):
        off.append(off[-1] + T[i - 1] - sink - rise * step / Lc)

    def base(i, s):
        s_lo = i * step
        return off[i] + rise * (1 - (s - s_lo) / Lc)

    # The top course must run past the ridge by its own HEIGHT, not by a fixed length: its top
    # skin stands n_top off the base plane, and a point n_top out along the face normal sits
    # n_top * tan(pitch) short of the ridge (v2: the top courses stopped ~0.7 m short of the
    # ridge on both faces, leaving a trench under the roll with sky showing through it).
    n_top = base(n - 1, S) + T[-1]
    s_top = S + n_top * tp + RIDGE_OVER

    faces = [(Vector((0, 1, 0)), ey, ex), (Vector((0, -1, 0)), ey, ex)]
    if hip:
        faces += [(Vector((1, 0, 0)), ex, ey), (Vector((-1, 0, 0)), ex, ey)]
    for fi, (o, dist, half) in enumerate(faces):
        E = o * dist + Z * z_e
        a = Z.cross(o)
        bd = -o * cp + Z * sp
        c = o * sp + Z * cp
        cuts = []
        if hip:
            for o2, dist2, _ in faces:
                if abs(o.dot(o2)) < 0.5:
                    no = (o2 - o).normalized()
                    C = o * dist + o2 * dist2
                    cuts.append((C, no))
        seed = rng.uniform(0, 100)
        for i in range(n):
            s_lo = i * step
            s_hi = s_top if i == n - 1 else s_lo + Lc
            if hip:
                u0, u1 = -half - 0.4, half + 0.4
            else:
                # Gable ends: every course of each face stops at its own distance (at least 8 mm
                # from any other), so no two course ends share the plane x = const (v3).
                e = 0.02 + 0.017 * i + 0.009 * fi
                u0, u1 = -half - e, half + e
            nu = max(2, math.ceil((u1 - u0) / 0.42))
            top, bot = [[], [], []], [[], [], []]
            for k in range(nu + 1):
                u = u0 + (u1 - u0) * k / nu
                wav = 0.06 * math.sin(u * 4.3 + seed + i * 1.7) + 0.08 * noise.noise(Vector((u * 1.6, seed, i * 3.1)))
                if i == 0:
                    wav *= 1.4
                jn = 0.035 * noise.noise(Vector((u * 2.3, seed + 5, i)))
                s0, s1 = s_lo - 0.04 - wav, s_lo + 0.36
                # (s top, s bottom, n bottom, n top): the bottom of the lip is pulled 12 cm back up
                # the slope, so the lip OVERHANGS and throws a shadow line on the course below
                # (v1: on a sunlit face the steps vanished).
                rows = ((s0, s0 + 0.12, base(i, s_lo) - 0.02 + jn, base(i, s_lo) + 0.62 * T[i] + jn),
                        (s1, s1, base(i, s1), base(i, s1) + T[i] + 0.4 * jn),
                        (s_hi, s_hi, base(i, s_hi), base(i, s_hi) + T[i]))
                for r, (st, sb, nb, nt) in enumerate(rows):
                    bot[r].append(E + a * u + bd * sb + c * nb)
                    top[r].append(E + a * u + bd * st + c * nt)
            pc = _grid_solid(top, bot)
            # Each course is cut 5 to 9 cm past the hip, a different distance per course: one
            # plane for all of them made the caps of neighbouring courses one plane (v2).
            for co, no in cuts:
                _cut(pc, co + no * (0.05 + 0.013 * i), no)
            _uv_frame(pc, a, bd, c, (h.rng.random(), 0.0))
            h.add("thatch", pc, roof=True)
    z_roll = z_r + n_top / cp - 0.2
    xr = (ex - ey) if hip else ex + 0.12
    if xr > 0.05:
        tube(h, "thatch", (-xr - (0.15 if hip else 0), 0, z_roll), (xr + (0.15 if hip else 0), 0, z_roll), (0.42, 0.34),
             sides=10, seg=0.5, wobble=0.08, along_u=True, roof=True)
    if hip:
        lip = (base(0, 0) + 0.62 * T[0]) / cp
        for sx in (-1, 1):
            for sy in (-1, 1):
                lo = Vector((sx * (ex + 0.1), sy * (ey + 0.1), z_e + lip - 0.12))
                hi = Vector((sx * max(ex - ey, 0.0), 0, z_roll))
                tube(h, "thatch", lo, hi + (hi - lo).normalized() * 0.2, (0.34, 0.28), sides=10, seg=0.5,
                     wobble=0.08, taper=1.0, along_u=True, roof=True)
    else:
        # The crossed ridge sticks at both gable ends (Filipino thatch houses tie their ridge this way).
        for sx in (-1, 1):
            x = sx * (ex - 0.2)
            for sy in (-1, 1):
                foot_y = sy * 1.0
                foot = Vector((x, foot_y, z_r - 1.0 * tp + n_top / cp - 0.12))
                tip = Vector((x + sx * 0.05, -sy * 0.55, z_roll + 0.85))
                tube(h, "bamboo", foot, tip, 0.05, roof=True)

    def underside(y):
        s = (ey - abs(y)) / cp
        best = None
        for i in range(n):
            s_lo = i * step
            s_hi = s_top if i == n - 1 else s_lo + Lc
            if s_lo <= s <= s_hi:
                v = base(i, s)
                best = v if best is None else min(best, v)
        if best is None:
            best = 0.0
        return z_e + (ey - abs(y)) * tp + (best + 0.07) / cp
    ys = [ey - k * 0.15 for k in range(int(2 * ey / 0.15) + 1)]
    h.info.update(z_eave=z_e, z_ridge=z_r + n_top / cp, roof_S=S, courses=n)
    return underside, ys, (z_r, xr if not hip else ex)


def tin_sheets(h, W, D, z_pt, pitch, ov, gov, purlin_ys=None):
    """A pitched corrugated TIN roof, ridge along X: two sheets whose cross-section is a real wave
    (so the eave shows the corrugation), a folded ridge cap, and timber purlins under the sheets
    that carry them onto the plates and gables (the sheets never touch the walls directly)."""
    p = math.radians(pitch)
    tp, cp, sp = math.tan(p), math.cos(p), math.sin(p)
    ey = D / 2 + ov
    ex = W / 2 + gov
    z_wl = z_pt + 0.08
    z_e = z_wl - ov * tp
    z_r = z_e + ey * tp
    S = ey / cp
    P_, amp, n0, th = 0.38, 0.035, 0.045, 0.025
    for side, o in enumerate((Vector((0, 1, 0)), Vector((0, -1, 0)))):
        E = o * ey + Z * z_e
        a = Z.cross(o)
        bd = -o * cp + Z * sp
        c = o * sp + Z * cp
        j = 0.015 if side else -0.015
        u0, u1 = -ex - j, ex + j
        # Samples on exact quarter periods from the sheet edge (0, crest, 0, trough), so no two
        # neighbouring samples share a height and no sheet face lies parallel to the purlins
        # (v3: a drifting sample phase made near-flat faces a few mm off a purlin top).
        us = [u0 + k * P_ / 4 for k in range(int((u1 - u0) / (P_ / 4)) + 1)]
        if u1 - us[-1] < 0.03:
            us[-1] = u1
        else:
            us.append(u1)
        top, bot = [[], [], []], [[], [], []]
        for u in us:
            w = amp * math.sin(math.tau * (u - u0) / P_)
            for r, s in enumerate((-0.05, S * 0.5, S + 0.06)):
                dip = -0.03 if r == 1 else 0.0
                top[r].append(E + a * u + bd * s + c * (n0 + w + dip))
                bot[r].append(E + a * u + bd * s + c * (n0 + w + dip - th))
        pc = _grid_solid(top, bot)
        _uv_frame(pc, a, bd, c, h.uv_off())
        h.add("tin", pc, roof=True)
        for s in (purlin_ys or (ov / cp + 0.05, S * 0.55, S - 0.25)):
            # Top 5 mm over the troughs' underside, so the troughs sink 2 cm in; and the purlin
            # follows the sheet's mid-slope sag, or it pokes through it (v5: "rivets" on the stall).
            dip = -0.03 * ((s + 0.05) / (S * 0.5 + 0.05) if s <= S * 0.5 else (S + 0.06 - s) / (S * 0.5 + 0.06))
            q = E + bd * s + c * (-0.065 + dip)
            beam(h, "timber", q - Vector((ex - 0.03, 0, 0)), q + Vector((ex - 0.03, 0, 0)), 0.1, 0.14, up=c, roof=True)
    apex = Vector((0, 0, z_r + (n0 + amp + 0.012) / cp))
    top = []
    for y, dz in ((-0.3, -0.3 * tp * 0.8), (0.0, 0.0), (0.3, -0.3 * tp * 0.8)):
        top.append([apex + Vector((x, y, dz)) for x in (-ex - 0.06, ex + 0.06)])
    bot = [[q - Z * 0.03 for q in row] for row in top]
    pc = _grid_solid(top, bot)
    _uv_frame(pc, Vector((1, 0, 0)), Vector((0, 1, 0)), Z, h.uv_off())
    h.add("tin", pc, roof=True)

    def underside(y):
        # 5 cm under the base plane: inside every purlin (their tops run 2.5 cm under to 0.5 cm
        # over it, with the sheet's sag) and never level with one (v6: 5 mm from a purlin top).
        return z_e + (ey - abs(y)) * tp - 0.05 / cp
    h.info.update(z_eave=z_e, z_ridge=apex.z, roof_S=S)
    return underside, [ey - k * 0.2 for k in range(int(2 * ey / 0.2) + 1)], (z_r, ex)


def sag_roof(h, z_pt, z_r, xr, sag):
    """The ridge dips toward the middle (thatch roofs settle; nothing here is ruler perfect).
    Zero at the plate, full at the ridge, zero at the gable ends."""
    for pc in h.roof:
        for v in pc.bm.verts:
            f = max(0.0, min(1.0, (v.co.z - z_pt) / max(0.1, z_r - z_pt)))
            k = max(0.0, 1.0 - (v.co.x / (xr + 0.4)) ** 2)
            v.co.z -= sag * k * f


def roof_for(h, W, D, F, H, roof, pitch, ov, gov, sag):
    z_pt = F + H + 0.10
    if roof == "tin":
        underside, ys, (z_r, xr) = tin_sheets(h, W, D, z_pt, pitch, ov, gov)
        sag = sag * 0.3
    else:
        underside, ys, (z_r, xr) = thatch_roof(h, W, D, z_pt, pitch, ov, gov, hip=(roof == "hip"))
    if roof != "hip":
        for sx in (-1, 1):
            gable_panel(h, sx * W / 2, D, F + H + 0.0, underside, ys)
    sag_roof(h, z_pt, z_r, xr, sag)


# ---------------------------------------------------------------- the four kinds

def _bays(h, L, door=False, window_p=0.6, counter=False):
    """Bay widths in metres (wall_frame rescales them to the wall) and what fills each bay. A
    door bay is always about 1.1 m, whatever the wall, so doors are one size across the kit."""
    if counter:
        return [(1.0, "counter")] * (2 if L > 3.2 else 1)
    n = max(2, round(L / 1.9))
    kinds = ["window" if h.rng.random() < window_p else "panel" for _ in range(n)]
    if not door:
        return [(L / n * (1.0 + h.j(0.08)), k) for k in kinds]
    k = h.rng.choice(range(1, n - 1)) if n > 2 else h.rng.choice((0, 1))
    kinds[k] = "door"
    rest = (L - 1.1) / (n - 1)
    return [(1.1 if i == k else rest * (1.0 + h.j(0.08)), kinds[i]) for i in range(n)]


def _door_x(W, bays):
    total = sum(w for w, _ in bays)
    x = -W / 2
    for w, k in bays:
        span = w * W / total
        if k == "door":
            return x + span / 2
        x += span
    return 0.0


def build_land(h):
    rng = h.rng
    W, D = rng.uniform(7.0, 9.0), rng.uniform(5.5, 6.8)
    W = max(W, D + 1.2)
    F, H = 0.9, rng.uniform(2.6, 2.8)
    roof = rng.choice(("hip", "gable"))    # a seed's identity (size, roof) is drawn first, so it
    pitch, sag = rng.uniform(44, 47), rng.uniform(0.1, 0.18)   # survives changes to the details
    floor_platform(h, W, D, F, -GROUND_SINK, tall=False)
    bays = {"front": _bays(h, W, door=True), "right": _bays(h, D, window_p=0.5),
            "back": _bays(h, W, window_p=0.5), "left": _bays(h, D, window_p=0.5)}
    wall_frame(h, W, D, F, H, bays)
    dx = _door_x(W, bays["front"])
    width = min(W + 0.2, max(4.0, W * rng.uniform(0.65, 1.0)))
    x0 = max(-W / 2 + width / 2 - 0.1, min(W / 2 - width / 2 + 0.1, dx))
    front, y1 = deck(h, W, D, F, rng.uniform(1.9, 2.3), width, x0, -GROUND_SINK, {"front-mid"})
    xa, xb, _, _, mid = h.info["deck"]
    sx = (front[mid][0] + front[mid + 1][0]) / 2
    stairs(h, sx, y1, F, 3)
    roof_for(h, W, D, F, H, roof, pitch, 1.0, 0.85, sag)
    h.info.update(W=W, D=D, F=F, H=H, roof=roof)


def build_water(h):
    rng = h.rng
    W, D = rng.uniform(4.5, 6.0), rng.uniform(4.0, 5.0)
    W = max(W, D + 0.4)
    F, H = rng.uniform(1.8, 2.1), rng.uniform(2.25, 2.45)
    roof = "gable" if rng.random() < 0.7 else "tin"
    pitch, sag = (rng.uniform(44, 47) if roof == "gable" else 30), rng.uniform(0.08, 0.14)
    floor_platform(h, W, D, F, -PILE_DEPTH, tall=True)
    bays = {"front": _bays(h, W, door=True, window_p=1.0), "right": [(1, "window"), (1, "panel")],
            "back": [(1, "panel"), (1, "window")] if rng.random() < 0.5 else [(1, "panel"), (1, "panel")],
            "left": [(1, "panel"), (1, "window")]}
    wall_frame(h, W, D, F, H, bays)
    width = min(W + 0.3, W * rng.uniform(0.75, 1.0) + 0.3)
    dx = _door_x(W, bays["front"])
    x0 = max(-W / 2 + width / 2 - 0.15, min(W / 2 - width / 2 + 0.15, dx))
    ladder_side = rng.choice(("front-left", "front-right"))
    front, y1 = deck(h, W, D, F, rng.uniform(1.3, 1.6), width, x0, -PILE_DEPTH, {ladder_side})
    a, b = (front[0], front[1]) if ladder_side == "front-left" else (front[-2], front[-1])
    lx = (a[0] + b[0]) / 2
    ladder(h, lx, y1, F)
    if rng.random() < 0.75:
        laundry(h, front[0], front[-1], F)
    roof_for(h, W, D, F, H, roof, pitch, 0.75, 0.6, sag)
    h.info.update(W=W, D=D, F=F, H=H, roof=roof, pile_depth=PILE_DEPTH)


def ladder(h, x, y_edge, F):
    """A bamboo ladder from the deck edge down into the water, leaning out a little, its rails
    running on up past the deck as handholds."""
    top_z, bot_z = F + 0.85, -0.35
    lean = 0.42
    rails = []
    for s in (-1, 1):
        a = Vector((x + s * 0.24, y_edge - 0.03, top_z))
        b = Vector((x + s * 0.26, y_edge - 0.03 + lean, bot_z))
        tube(h, "bamboo", b, a, 0.045, nodes=0.5)
        rails.append((a, b))
    z = 0.12
    while z < F - 0.15:
        f = (top_z - z) / (top_z - bot_z)
        p0, p1 = rails[0][0].lerp(rails[0][1], f), rails[1][0].lerp(rails[1][1], f)
        tube(h, "bamboo", p0 - (p1 - p0) * 0.08, p1 + (p1 - p0) * 0.08, 0.028)
        z += 0.3


def laundry(h, pa, pb, F):
    """A line across the deck front between two taller poles, with a few pieces of washing."""
    zl = F + 1.75
    ends = []
    for x, y in (pa, pb):
        tube(h, "bamboo", (x + 0.03, y + 0.06, F - 0.2), (x + 0.05, y + 0.07, zl + 0.15), 0.035)
        ends.append(Vector((x + 0.045, y + 0.065, zl)))
    tube(h, "cloth", ends[0] - (ends[1] - ends[0]).normalized() * 0.05, ends[1] + (ends[1] - ends[0]).normalized() * 0.05,
         0.011, sides=6)
    L = (ends[1] - ends[0]).length
    t = 0.4
    while t < L - 0.5:
        w, hh = h.rng.uniform(0.35, 0.6), h.rng.uniform(0.4, 0.62)   # hems clear the railing tops
        c = ends[0].lerp(ends[1], t / L)
        d = (ends[1] - ends[0]).normalized()
        beam(h, "cloth", c + Z * 0.02, c + Z * (0.02 - hh), w, 0.02,
             up=d.cross(Z).normalized())
        t += w + h.rng.uniform(0.2, 0.45)


def build_stall(h):
    rng = h.rng
    W, D = rng.uniform(3.0, 3.6), rng.uniform(2.4, 2.8)
    # Tall walls and a short overhang, unlike the houses: the awning shutter swings up and out
    # from the counter head, and under a low deep eave it went straight through the roof (v5).
    F, H = 0.35, 2.9
    roof = "gable" if rng.random() < 0.55 else "tin"
    pitch = rng.uniform(40, 44) if roof == "gable" else 28
    floor_platform(h, W, D, F, -GROUND_SINK, tall=False)
    bays = {"front": _bays(h, W, counter=True), "right": [(1, "panel"), (1, "panel")],
            "back": [(1, "panel"), (1, "panel")], "left": [(0.66, "panel"), (1.1, "door"), (0.66, "panel")]}
    wall_frame(h, W, D, F, H, bays)
    # The shop sign: a plank board over the counter, let 1 cm into the posts (the texture pass
    # paints the lettering).
    zs = F + 2.55
    beam(h, "plank", (-W / 2 + 0.05, D / 2 + 0.11, zs), (W / 2 - 0.05, D / 2 + 0.11, zs), 0.07, 0.36, up=Z)
    beam(h, "plank", (-W / 2 - 0.45, 0, F * 0.5 - 0.03), (-W / 2 - 0.45, 0, F * 0.5 + 0.03), 1.1, 0.5,
         up=Vector((1, 0, 0)))
    beam(h, "timber", (-W / 2 - 0.45, 0, -0.15), (-W / 2 - 0.45, 0, F * 0.5 - 0.02), 0.9, 0.3, up=Vector((1, 0, 0)))
    roof_for(h, W, D, F, H, roof, pitch, 0.6, 0.5, 0.06)
    h.info.update(W=W, D=D, F=F, H=H, roof=roof)


def build_capilla(h):
    """The capilla: ONE painted shell for the nave (a pentagon pushed along Y, openings cut in
    by boolean so every recess is part of the wall), a front tower of its own shell holding the
    door and an open belfry, tin roofs on purlins, a white plinth with two steps."""
    rng = h.rng
    w, d = rng.uniform(5.0, 5.6), rng.uniform(7.2, 8.0)
    zb, zw = 0.22, 0.22 + rng.uniform(3.5, 3.8)
    pitch = 33.0
    p = math.radians(pitch)
    zp = zw + (w / 2) * math.tan(p)
    frame_w = (Vector((1, 0, 0)), Z, Vector((0, 1, 0)))
    prof = [Vector((-w / 2, -d / 2, zb)), Vector((w / 2, -d / 2, zb)), Vector((w / 2, -d / 2, zw)),
            Vector((0, -d / 2, zp)), Vector((-w / 2, -d / 2, zw))]
    nave = extrude(h, None, prof, Vector((0, d, 0)), frame_w)
    cutters = []
    for sx in (-1, 1):
        for y in (-d * 0.22, d * 0.22):
            pts = [Vector((sx * (w / 2 - 0.16), y + x, zb + 1.25 + z)) for x, z in arch_poly(0.85, 1.55)]
            cutters.append(extrude(h, None, pts, Vector((sx * 0.4, 0, 0)), frame_w))
    pts = [Vector((x, -d / 2 - 0.2, zw - 1.2 + z)) for x, z in arch_poly(0.9, 1.3)]
    cutters.append(extrude(h, None, pts, Vector((0, 0.34, 0)), frame_w))
    nave = _boolean(nave, cutters)
    _uv_world(nave, h.uv_off())
    h.add("paint_white", nave)
    for sx in (-1, 1):
        for y in (-d * 0.22, d * 0.22):
            # Depths behind the wall face (recess back at 16 cm): pane 14 to 18, upright 9 to 17,
            # cross bar 11.5 to 17.5. All different, each let into the recess back.
            pts = [Vector((sx * (w / 2 - 0.18), y + x, zb + 1.24 + z)) for x, z in arch_poly(0.87, 1.57)]
            extrude(h, "capiz", pts, Vector((sx * 0.04, 0, 0)), (Vector((0, 1, 0)), Z, Vector((1, 0, 0))))
            beam(h, "timber", (sx * (w / 2 - 0.13), y, zb + 1.2), (sx * (w / 2 - 0.13), y, zb + 2.62), 0.05, 0.08,
                 up=Vector((sx, 0, 0)))
            beam(h, "timber", (sx * (w / 2 - 0.145), y - 0.44, zb + 2.0), (sx * (w / 2 - 0.145), y + 0.44, zb + 2.0), 0.05, 0.06,
                 up=Vector((sx, 0, 0)))
    pts = [Vector((x, -d / 2 + 0.15, zw - 1.21 + z)) for x, z in arch_poly(0.92, 1.32)]
    extrude(h, "capiz", pts, Vector((0, -0.04, 0)), frame_w)
    # The tower: square shaft through the facade, a door recess, an open belfry, a pyramid roof.
    tw = 2.3
    ty = d / 2 + 0.75
    th = rng.uniform(7.0, 7.6)
    shaft = extrude(h, None, [Vector((-tw / 2, ty - tw / 2, zb - 0.04)), Vector((tw / 2, ty - tw / 2, zb - 0.04)),
                              Vector((tw / 2, ty + tw / 2, zb - 0.04)), Vector((-tw / 2, ty + tw / 2, zb - 0.04))],
                    Vector((0, 0, th - zb + 0.04)), frame_w)
    cutters = [extrude(h, None, [Vector((x, ty + tw / 2 + 0.2, zb + 0.02 + z)) for x, z in arch_poly(1.15, 2.35)],
                       Vector((0, -0.52, 0)), frame_w)]
    bz = th - 2.0
    cutters.append(extrude(h, None, [Vector((x, ty + tw, bz + z)) for x, z in arch_poly(1.1, 1.55)], Vector((0, -2 * tw, 0)), frame_w))
    cutters.append(extrude(h, None, [Vector((tw, ty + x, bz + z)) for x, z in arch_poly(1.1, 1.55)], Vector((-2 * tw, 0, 0)), frame_w))
    shaft = _boolean(shaft, cutters)
    _uv_world(shaft, h.uv_off())
    h.add("paint_white", shaft)
    for z0, z1, g in ((bz - 0.22, bz + 0.02, 0.16), (th - 0.12, th + 0.16, 0.2)):   # belt and cornice
        beam(h, "paint_white", (0, ty, z0), (0, ty, z1), tw + 2 * g, tw + 2 * g + 0.01, up=Vector((0, 1, 0)))
    # Door leaves in the recess, the bell on its beam, and the cross.
    for s in (-1, 1):
        x = s * 0.29
        beam(h, "plank", (x, ty + tw / 2 - 0.31, zb + 0.03), (x, ty + tw / 2 - 0.31, zb + 1.72), 0.55, 0.05,
             up=Vector((0, 1, 0)))
    # The bell beam lies in the belfry's crossing, its top let 2 cm into the arch crowns.
    beam(h, "timber", (-tw / 2 - 0.05, ty, th - 0.5), (tw / 2 + 0.05, ty, th - 0.5), 0.12, 0.14)
    tube(h, "tin", (0, ty, th - 0.54), (0, ty, th - 1.15), 0.3, sides=12,
         rings=[(0.0, 0.3), (0.12, 0.55), (0.55, 0.8), (0.9, 1.0), (1.0, 1.0)])
    apex = th + 1.9
    pyr = Piece()
    bm = pyr.bm
    g = tw / 2 + 0.38
    base = [bm.verts.new((x, ty + y, th + 0.08)) for x, y in ((-g, -g), (g, -g), (g, g), (-g, g))]
    under = [bm.verts.new((x * 0.8, ty + y * 0.8, th + 0.02)) for x, y in ((-g, -g), (g, -g), (g, g), (-g, g))]
    top = bm.verts.new((0, ty, apex))
    for i in range(4):
        k = (i + 1) % 4
        bm.faces.new((base[i], base[k], top))
        bm.faces.new((under[k], under[i], base[i], base[k]))
    bm.faces.new(list(reversed(under)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.normal_update()
    s = 1.0 / UV_METRES
    for f in bm.faces:
        nrm = f.normal
        U = Z.cross(nrm).normalized() if abs(nrm.z) < 0.99 else Vector((1, 0, 0))
        V = nrm.cross(U)
        if V.z < 0:
            V = -V
        for loop in f.loops:
            loop[pyr.uv].uv = (loop.vert.co.dot(U) * s, loop.vert.co.dot(V) * s)
    h.add("tin", pyr)
    beam(h, "timber", (0, ty, apex - 0.25), (0, ty, apex + 0.85), 0.1, 0.1, up=Vector((0, 1, 0)))
    beam(h, "timber", (-0.3, ty, apex + 0.5), (0.3, ty, apex + 0.5), 0.09, 0.09, up=Vector((0, 1, 0)))
    # The nave roof: the tin kit with its ridge along X, then turned so the ridge runs along Y.
    h.M = Matrix.Rotation(math.pi / 2, 4, "Z")
    # z_pt chosen so the shell's roof slopes lie 6 cm under the tin's base plane, parallel to it:
    # the purlins (base plane -14 to 0 cm) sink 8 cm into the shell and carry the sheets, and the
    # shell never reaches the corrugation (v2: it poked through the sheets).
    tin_sheets(h, d, w, zw - 0.08 + 0.06 / math.cos(p), pitch, 0.45, 0.4)
    h.M = None
    h.roof = []
    # Plinth and steps.
    beam(h, "paint_white", (0, -d / 2 - 0.35, 0.1), (0, ty + tw / 2 + 0.35, 0.1), w + 0.7, 0.5, up=Z)
    # Two steps down from the plinth (top 0.35) in front of the tower door, each wider than the
    # one above and sunk into the ground; each tucks 3 cm under the one above it.
    y_front = ty + tw / 2 + 0.35
    for k, zt in enumerate((0.235, 0.115)):
        y0, y1 = y_front - 0.03 + 0.34 * k, y_front + 0.34 * (k + 1)
        beam(h, "paint_white", (0, y0, (zt - 0.15) / 2), (0, y1, (zt - 0.15) / 2), 1.7 + 0.3 * k, zt + 0.15, up=Z)
    h.info.update(W=w, D=d, F=zb, H=zw - zb, roof="tin", tower=th)


BUILDERS = {"land": build_land, "water": build_water, "stall": build_stall, "capilla": build_capilla}


# ---------------------------------------------------------------- assembly

def material(slot):
    """The slot's material, created once with its flat placeholder colour."""
    m = bpy.data.materials.get(slot)
    if m is None:
        m = bpy.data.materials.new(slot)
        m.use_nodes = True
        bsdf = m.node_tree.nodes["Principled BSDF"]
        bsdf.inputs["Base Color"].default_value = SLOTS[slot] + (1,)
        bsdf.inputs["Roughness"].default_value = 0.6 if slot in ("tin", "capiz") else 0.85
        m.diffuse_color = SLOTS[slot] + (1,)
    return m


def _append(dst, duv, pc):
    vmap = {v: dst.verts.new(v.co) for v in pc.bm.verts}
    for f in pc.bm.faces:
        try:
            nf = dst.faces.new([vmap[v] for v in f.verts])
        except ValueError:
            continue
        for a, b in zip(f.loops, nf.loops):
            b[duv].uv = a[pc.uv].uv


def build_house(kind, seed=1):
    """Build one house of `kind` (see KINDS) from `seed` and return its Collection, NOT linked to
    any scene: every part is parented to one root empty named `kind` at the origin (the room's
    centre at the ground or water contact, door facing +Y). The root carries house_kind,
    house_seed and the dimensions as custom properties."""
    if kind not in BUILDERS:
        raise ValueError(f"unknown house kind {kind!r}; one of {KINDS}")
    h = House(kind, seed)
    BUILDERS[kind](h)
    col = bpy.data.collections.new(f"house_{kind}_{seed}")
    root = bpy.data.objects.new(kind, None)
    root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 1.0
    col.objects.link(root)
    root["house_kind"], root["house_seed"] = kind, seed
    for k, v in h.info.items():
        if isinstance(v, (int, float, str)):
            root[f"house_{k}"] = v
    for slot, pcs in h.pieces.items():
        if not pcs:
            continue
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        for pc in pcs:
            _append(bm, uv, pc)
            pc.bm.free()
        # ⚠️ normals BEFORE triangulating: faces rebuilt by _append carry no normal yet, and the
        # triangulator projects each n-gon along it. Without this, concave n-gons (the capilla
        # walls round their window holes, the hip caps) folded into flipped triangles laid
        # straight over the holes (capilla v4: no windows, wall area 40.8 m² for a 25 m² wall).
        bm.normal_update()
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
        bm.normal_update()
        width, sharp_deg, harden = FINISH[slot]
        lim = math.radians(sharp_deg)
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
        me = bpy.data.meshes.new(f"{kind}_{seed}_{slot}")
        bm.to_mesh(me)
        bm.free()
        me.materials.append(material(slot))
        ob = bpy.data.objects.new(me.name, me)
        ob.parent = root
        col.objects.link(ob)
        bev = ob.modifiers.new("Bevel", "BEVEL")
        bev.width, bev.segments, bev.limit_method = width, 2, "ANGLE"
        bev.angle_limit = math.radians(30.0 if slot != "bamboo" else 50.0)
        bev.harden_normals = harden
        bev.use_clamp_overlap = True
    return col


# ---------------------------------------------------------------- checks

def check_house(col):
    """Tri counts (base and with the bevels), non-manifold edges per slot, COPLANAR overlaps
    (parallel faces of different shells closer than 4 mm over each other's interior: z-fighting)
    and FLOATING shells (shells in no chain of intersections down to z = 0)."""
    V, P, shell, slot_of, zmin = [], [], [], [], {}
    out = {"tris": 0, "tris_bevelled": 0, "nonmanifold": {}, "shells": 0}
    sid = 0
    dg = bpy.context.evaluated_depsgraph_get()
    for ob in col.objects:
        if ob.type != "MESH":
            continue
        me = ob.data
        mw = ob.matrix_world
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
        # shells by vertex connectivity
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
        V.extend(mw @ v.co for v in me.vertices)
        for p in me.polygons:
            r = find(p.vertices[0])
            if r not in roots:
                roots[r] = sid
                sid += 1
            s = roots[r]
            P.append([base_v + i for i in p.vertices])
            shell.append(s)
            slot_of.append(ob.name)
            z = min(V[base_v + i].z for i in p.vertices)
            zmin[s] = min(zmin.get(s, 1e9), z)
    out["shells"] = sid
    tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
    normals = []
    for p in P:
        a, b, c = V[p[0]], V[p[1]], V[p[2]]
        normals.append((b - a).cross(c - a).normalized())
    cop = set()
    for i, p in enumerate(P):
        n = normals[i]
        if n.length < 0.5:
            continue
        c = sum((V[k] for k in p), Vector()) / len(p)
        for q in [c] + [c.lerp(V[k], 0.7) for k in p]:
            for _loc, nrm, idx, _dist in tree.find_nearest_range(q, 0.004):
                if idx is not None and shell[idx] != shell[i] and abs(nrm.dot(n)) > 0.999:
                    cop.add((min(shell[i], shell[idx]), max(shell[i], shell[idx]), slot_of[i], slot_of[idx],
                             tuple(round(x, 2) for x in c) + tuple(round(x, 2) for x in n)))
    pairs = {}
    for sa, sb, a, b, at in cop:
        pairs.setdefault((sa, sb), (a.split("_", 2)[-1], b.split("_", 2)[-1], at))
    out["coplanar"] = len(pairs)
    out["coplanar_examples"] = sorted(pairs.values())[:8]
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
    grounded = {f2(s) for s, z in zmin.items() if z <= 0.01}
    floating = [s for s in range(sid) if f2(s) not in grounded]
    names = {}
    for i, s in enumerate(shell):
        if s in floating:
            names[s] = slot_of[i]
    out["floating"] = len(floating)
    out["floating_examples"] = sorted(set(names.values()))[:6]
    xs = [v.x for v in V]
    ys = [v.y for v in V]
    zs = [v.z for v in V]
    out["bbox"] = (round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(min(zs), 2), round(max(zs), 2))
    return out


# ---------------------------------------------------------------- preview

def _mat(name, colour, rough=0.85):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = colour + (1,)
    bsdf.inputs["Roughness"].default_value = rough
    return m


def _checker_mat(slot):
    """A checker on "UVMap", 8 cells per UV unit (25 cm cells at 1 unit = 2 m), in the slot's own
    colour against a dark tone, so a stretched or rotated unwrap shows at a glance."""
    col = SLOTS[slot]
    m = _mat(f"checker_{slot}", col)
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    uvn = nt.nodes.new("ShaderNodeUVMap")
    uvn.uv_map = "UVMap"
    chk = nt.nodes.new("ShaderNodeTexChecker")
    chk.inputs["Scale"].default_value = 8.0
    chk.inputs["Color1"].default_value = tuple(min(1.0, c * 1.15) for c in col) + (1,)
    chk.inputs["Color2"].default_value = tuple(c * 0.35 for c in col) + (1,)
    nt.links.new(uvn.outputs["UV"], chk.inputs["Vector"])
    nt.links.new(chk.outputs["Color"], bsdf.inputs["Base Color"])
    return m


# Front row (near the camera, which stands on the +Y side, where the doors are) the small ones.
LINEUP = [("land", 1, (-19.0, 0.0)), ("capilla", 1, (0.0, -3.0)), ("land", 2, (19.0, 0.0)),
          ("water", 3, (-15.0, 18.0)), ("stall", 1, (0.0, 17.0)), ("water", 1, (14.0, 18.0))]


def _preview_scene():
    scene = bpy.context.scene
    ground = bpy.data.meshes.new("ground")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=160)
    bm.to_mesh(ground)
    bm.free()
    ground.materials.append(_mat("ground_pale", (0.80, 0.76, 0.66)))
    g = bpy.data.objects.new("ground", ground)
    scene.collection.objects.link(g)
    ref = bpy.data.meshes.new("scale_ref_1m60")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
    bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
    bm.to_mesh(ref)
    bm.free()
    ref.materials.append(_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
    for i, at in enumerate(((6.5, 21.0, 0), (-9.0, 4.5, 0))):
        r = bpy.data.objects.new(f"scale_ref_1m60_{i}", ref)
        r.location = at
        scene.collection.objects.link(r)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color = 4.2, (1.0, 0.88, 0.72)
    sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(52), 0, math.radians(-140))
    scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("world")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.66, 0.7, 0.74, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
    scene.world = world
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
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


# (tag, camera, target, lens, checker). Every camera stands on the +Y side, where the doors are.
SHOTS = [
    ("lineup", (26.0, 55.0, 17.0), (0.0, 8.0, 2.8), 30, False),
    ("close", (-5.0, 15.5, 6.0), (-19.0, 0.5, 3.4), 30, False),
    ("closew", (-3.5, 30.5, 4.5), (-15.0, 18.0, 2.6), 30, False),
    ("closec", (9.5, 12.5, 3.0), (0.0, -2.0, 3.6), 28, False),
    ("checker", (26.0, 55.0, 17.0), (0.0, 8.0, 2.8), 30, True),
    ("checkerclose", (-5.0, 15.5, 6.0), (-19.0, 0.5, 3.4), 30, True),
]


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    top = bpy.data.collections.new("lagoon_houses")
    bpy.context.scene.collection.children.link(top)
    cols = []
    for kind, seed, (x, y) in LINEUP:
        col = build_house(kind, seed)
        top.children.link(col)
        c = check_house(col)
        root = next(o for o in col.objects if o.parent is None)
        info = {k[6:]: (round(v, 2) if isinstance(v, float) else v) for k, v in root.items() if k.startswith("house_")}
        print(f"[lagoon-houses] {kind} {seed}: {info}")
        print(f"[lagoon-houses]   {c}")
        root.location = (x, y, 0)
        cols.append(col)
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "lagoon_houses.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = out.with_suffix(".blend1")
    if backup.exists():
        backup.unlink()
    print("[lagoon-houses] saved", out)
    if not version:
        return
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    cam = _preview_scene()
    scene = bpy.context.scene
    meshes = [o.data for c in cols for o in c.objects if o.type == "MESH"]
    checkers = {s: _checker_mat(s) for s in SLOTS}
    for tag, pos, tgt, lens, check in SHOTS:
        for me in meshes:
            slot = me.name.split("_", 2)[2]
            me.materials[0] = checkers[slot] if check else material(slot)
        cam.location = pos
        cam.data.lens = lens
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        path = PREVIEWS / f"housekit_{tag}_v{version}.png"
        if path.exists():
            raise SystemExit(f"[lagoon-houses] {path} exists: never overwrite a render, bump --preview")
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[lagoon-houses] preview", path)


if __name__ == "__main__":
    main()
