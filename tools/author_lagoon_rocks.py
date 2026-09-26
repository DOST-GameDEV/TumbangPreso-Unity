"""Lagoon Court ROCK KIT: the final stone models (geometry, two UV maps, a top mask; no texture).

  blender -b --python tools/author_lagoon_rocks.py -- --preview N

Writes ArtSource/lagoon/lagoon_rocks.blend and, with --preview N, three renders in
Logs/lagoon-blender/: rockkit_lineup_vN.png (plain tan), rockkit_checker_vN.png (a checker on
"UVMap", to prove the world-scale projection does not stretch) and rockkit_edges_vN.png (a
Cycles bevel mask, to prove the plane breaks read as a narrow band for the edge-wear bake).

Importable with no side effects: author_lagoon_cove.py calls `build_rock_kit()` in place of the
old `boulder_mesh()` and gets ROCK_KIT_SIZE mesh datablocks back.

What the owner decided (docs/LAGOON_REWORK_GUIDE.md § 3 and the rock reviews), and so what this
builds:

  * NOT CONES, NOT CRYSTALS, NOT BLOBS. Flat shading made the cove v1 stones read as crystals;
    the soft-clamped pillows of kit v1 to v7 then read as blobs next to the owner's two shape
    references (a stylized rock pack of tall, chunky, ANGULAR stones of many broad flat planes
    with chipped facets, and the fishing village massif of huge stones with broad planes and
    rounded shoulders). So: a rounded body cut by MANY broad planes, a few small chips along the
    top edges, soft shading inside every plane and a narrow soft band at every plane break.
  * "MORE UNIQUE": five families with their own proportions and cut rules (boulders, stacks and
    lumped spires, table slabs, split pairs, cobbles), and each member has its own character.
  * A slight asymmetric lean, near-vertical sides and a near-flat top, like weathered granite.

⚠️ HOW A PLANE BREAK IS MADE, AND WHY IT IS A REAL EDGE NOW. Kit v1 to v7 eased vertices onto
each plane (a soft clamp). That never made an edge: the break fell between grid rows as a smooth
smear, so a baked bevel or curvature mask could only read it as mush. The owner wants EDGE WEAR
baked per stone and following the model's real edges, so every plane is now a true cut
(bisect, the hole capped with one flat face), and every break over 25 degrees then gets a
narrow bevel (two segments; one on the stacked lumps). The result is broad flat planes with a
crisp but softly shaded rim: the bevel steps a 60 degree break into three soft steps, so most
breaks stay under the 38 degree sharp limit and nothing reads as a crystal facet. The caps are
triangulated (beauty) before the UVs so the unwrap and the export see the same triangles.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector, noise

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "lagoon"
PREVIEWS = ROOT / "Logs" / "lagoon-blender"

ACROSS = 2.5          # every stone is normalised to this widest horizontal extent, in metres
BURIED = 0.20         # fraction of each stone's height below z = 0, for sinking into the ground
UV_METRES = 4.0       # 1 UV unit = 4 m at object scale 1: a tiling rock texture covers 4 m
SHARP_DEG = 38.0      # only edges whose faces differ by more than this stay sharp
BREAK_DEG = 25.0      # a plane break steeper than this gets the narrow bevel
BEVEL = 0.035         # bevel width in body units (about 4 cm at scale 1 once normalised)

# The kit, in order: (family, seed). The index into this list is the kit index.
STONES = (
    [("boulder", s) for s in (11, 23, 37, 41, 58)]
    + [("stack", s) for s in (61, 72, 89)]
    + [("slab", s) for s in (104, 117, 125)]
    + [("split", s) for s in (131, 149)]
    + [("cobble", s) for s in (152, 167, 173)]
)
ROCK_KIT_SIZE = len(STONES)
ROCK_FAMILIES = {}
for _i, (_f, _s) in enumerate(STONES):
    ROCK_FAMILIES.setdefault(_f, []).append(_i)


# ---------------------------------------------------------------- body and cuts

def _plane(az, z):
    return Vector((math.cos(az), math.sin(az), z)).normalized()


def _body(bm, rng, cuts, p, half, taper=0.0, amp=0.04, lobes=2, lobe_amp=(0.06, 0.14)):
    """A rounded box: a subdivided cube mapped evenly onto a sphere, then out to a
    superellipsoid of exponent p (2 a sphere, 3 a pillow), scaled to the half-extents `half`,
    tapered toward the top (negative overhangs the foot), swollen by a few broad `lobes` so the
    widest point sits off centre, and given one low octave of noise."""
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    seed = Vector((rng.uniform(-50, 50), rng.uniform(-50, 50), rng.uniform(-50, 50)))
    bulges = [(_plane(rng.uniform(0, math.tau), rng.uniform(-0.4, 0.7)), rng.uniform(*lobe_amp),
               rng.uniform(0.35, 0.6)) for _ in range(lobes)]
    a, b, c = half
    for v in bm.verts:
        x, y, z = (max(-1.0, min(1.0, q)) for q in v.co)
        # Even cube-to-sphere map, so the quads stay near square instead of pinching at corners.
        u = Vector((x * math.sqrt(max(0.0, 1 - y * y / 2 - z * z / 2 + y * y * z * z / 3)),
                    y * math.sqrt(max(0.0, 1 - z * z / 2 - x * x / 2 + z * z * x * x / 3)),
                    z * math.sqrt(max(0.0, 1 - x * x / 2 - y * y / 2 + x * x * y * y / 3)))).normalized()
        r = 1.0 / (abs(u.x) ** p + abs(u.y) ** p + abs(u.z) ** p) ** (1.0 / p)
        for d, amt, w in bulges:
            r *= 1.0 + amt * math.exp(-(1.0 - u.dot(d)) / w)
        q = u * r
        t = (q.z + 1) * 0.5
        q = Vector((q.x * a * (1.0 - taper * t), q.y * b * (1.0 - taper * t), q.z * c))
        q += u * amp * noise.noise(q * 0.8 + seed)
        v.co = q


def _verts_of(bm, verts):
    """Every live vertex of the shell that `verts` belongs to (a split pair has two shells)."""
    seen, stack = set(), [v for v in verts if v.is_valid][:1]
    while stack:
        w = stack.pop()
        if w in seen:
            continue
        seen.add(w)
        stack.extend(e.other_vert(w) for e in w.link_edges)
    return list(seen)


def _cut(bm, verts, n, t):
    """Cut the shell holding `verts` with a plane of normal n placed by t (see `off`), keep the
    inside and cap the hole with one flat face. Returns the shell's surviving vertices."""
    n = n.normalized()
    verts = [v for v in verts if v.is_valid]
    top = max(v.co.dot(n) for v in verts)
    low = min(v.co.dot(n) for v in verts)
    off = low + (top - low) * (0.5 + 0.5 * t)   # t = 1 touches the support, t = 0 halves it
    vs = set(verts)
    edges = list({e for v in verts for e in v.link_edges})
    faces = list({f for v in verts for f in v.link_faces})
    # dist: a vertex within 1 cm of the plane is taken as ON it, so a cut landing a hair from an
    # old vertex snaps to it instead of leaving a sliver face.
    res = bmesh.ops.bisect_plane(bm, geom=verts + edges + faces, plane_co=n * off, plane_no=n,
                                 clear_outer=True, dist=0.01)
    cut_verts = [v for v in res["geom_cut"] if isinstance(v, bmesh.types.BMVert) and v.is_valid]
    alive = _verts_of(bm, [v for v in vs if v.is_valid] + cut_verts)
    boundary = list({e for v in alive for e in v.link_edges if len(e.link_faces) == 1})
    if boundary:
        bmesh.ops.holes_fill(bm, edges=boundary)
    return _verts_of(bm, alive)


def _planes(bm, verts, rng, top_tilt, top_t, sides, side_t, side_z, extra=0, chips=0, bottom=None):
    """The broad planes: one near-flat top, `sides` near-vertical sides spread round the stone,
    `extra` oblique ones, then `chips`: small facets knocked off the top edge (t close to 1, so
    each removes only a corner). `bottom` cuts a flat underside (for stacked lumps)."""
    tilt, az = math.radians(rng.uniform(0, top_tilt)), rng.uniform(0, math.tau)
    top = Vector((math.sin(tilt) * math.cos(az), math.sin(tilt) * math.sin(az), math.cos(tilt)))
    verts = _cut(bm, verts, top, rng.uniform(*top_t))
    base = rng.uniform(0, math.tau)
    for i in range(sides):
        a = base + i * math.tau / sides + rng.uniform(-0.35, 0.35)
        verts = _cut(bm, verts, _plane(a, rng.uniform(*side_z)), rng.uniform(*side_t))
    for _ in range(extra):
        verts = _cut(bm, verts, _plane(rng.uniform(0, math.tau), rng.uniform(0.45, 1.1)), rng.uniform(0.7, 0.82))
    for _ in range(chips):
        verts = _cut(bm, verts, _plane(rng.uniform(0, math.tau), rng.uniform(0.6, 1.4)), rng.uniform(0.86, 0.93))
    if bottom is not None:
        verts = _cut(bm, verts, Vector((0, 0, -1)), bottom)
    return verts


def _bevel_breaks(bm, verts, width=BEVEL, segments=2):
    """The narrow soft band at every plane break (see the header)."""
    vs = set(verts)
    lim = math.radians(BREAK_DEG)
    edges = [e for e in bm.edges if e.verts[0] in vs and len(e.link_faces) == 2
             and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim]
    if edges:
        bmesh.ops.bevel(bm, geom=edges, offset=width, offset_type="OFFSET", segments=segments, profile=0.5,
                        affect="EDGES", clamp_overlap=True)
    bm.normal_update()


def _lean(verts, rng, amount):
    """A linear shear that keeps the base where it is: the stone leans, the planes stay flat."""
    az = rng.uniform(0, math.tau)
    lx, ly = math.cos(az) * amount, math.sin(az) * amount
    z0 = min(v.co.z for v in verts)
    for v in verts:
        h = v.co.z - z0
        v.co.x += lx * h
        v.co.y += ly * h


# ---------------------------------------------------------------- families

# SELF-REVIEW v5: drawn from one shared random range, the boulders were five potatoes and the
# slabs three bars of soap. Each member has its own CHARACTER; the seed only varies it.
BOULDER_CHARACTERS = [
    # (half-extents x, y, z; superellipse p; taper; top tilt deg; top cut; sides; chips)
    ((1.1, 0.9, 1.15), 2.4, 0.12, 10, (0.66, 0.74), 4, 3),    # the upright massif stone
    ((1.25, 0.8, 0.75), 2.3, 0.05, 8, (0.6, 0.7), 3, 2),      # a low whale-back
    ((1.0, 0.9, 1.05), 2.3, -0.3, 12, (0.7, 0.78), 4, 3),     # perched: overhangs its own foot
    ((1.05, 0.95, 1.0), 2.6, 0.1, 6, (0.64, 0.72), 5, 2),     # a chunky block, five worn sides
    ((1.15, 0.85, 1.05), 2.3, 0.18, 30, (0.58, 0.66), 3, 3),  # a wedge: one big tilted top plane
]
SLAB_CHARACTERS = [
    # (half-extents; top cut; sides; chips)
    ((1.25, 1.0, 0.5), (0.2, 0.3), 5, 3),    # a table: level top, five worn sides
    ((1.3, 0.8, 0.42), (0.25, 0.35), 4, 2),  # a long step stone
    ((1.2, 1.05, 0.55), (0.2, 0.3), 4, 3),   # a thick plate
]


def _boulder(bm, rng, k):
    """(a) Massif boulder: a rounded body cut by broad planes, chipped along its top edge."""
    (a, b, c), p, taper, tilt, top_t, sides, chips = BOULDER_CHARACTERS[k]
    half = (a, b * rng.uniform(0.92, 1.05), c * rng.uniform(0.92, 1.05))
    _body(bm, rng, 8, p, half, taper=taper, lobes=rng.randint(2, 3))
    # SELF-REVIEW v9: cut to 0.55 the planes met each other and every stone was a polyhedron
    # (a crystal again). Shallower cuts leave rounded body between the planes: the village
    # reference's broad planes on rounded shoulders.
    verts = _planes(bm, bm.verts[:], rng, tilt, top_t, sides, (0.72, 0.82), (-0.03, 0.3),
                    extra=rng.randint(1, 2), chips=chips)
    _bevel_breaks(bm, verts)
    _lean(bm.verts, rng, rng.uniform(0.04, 0.12))


def _lump(bm, rng, half, taper, top_tilt, sides, bottom, chips):
    _body(bm, rng, 4, rng.uniform(2.3, 2.6), half, taper=taper, amp=0.03, lobes=1)
    verts = _planes(bm, bm.verts[:], rng, top_tilt, (0.66, 0.76), sides, (0.72, 0.82), (-0.03, 0.12),
                    extra=1, chips=chips, bottom=bottom)
    # One bevel segment on the lumps: two segments on three lumps put a spire over 1800 tris.
    _bevel_breaks(bm, verts, segments=1)


def _union(meshes):
    """Boolean union (exact solver) of closed meshes into one closed mesh; temp objects are
    created and removed, the scene is left as found."""
    objs = []
    for me in meshes:
        o = bpy.data.objects.new("_rock_union_tmp", me)
        bpy.context.scene.collection.objects.link(o)
        objs.append(o)
    base = objs[0]
    for o in objs[1:]:
        mod = base.modifiers.new("union", "BOOLEAN")
        mod.operation, mod.solver, mod.object = "UNION", "EXACT", o
        o.hide_render = o.hide_viewport = True
    dg = bpy.context.evaluated_depsgraph_get()
    out = bpy.data.meshes.new_from_object(base.evaluated_get(dg))
    for o in objs:
        bpy.data.objects.remove(o)
    for me in meshes:
        bpy.data.meshes.remove(me)
    return out


def _stack(bm, rng, k):
    """(b) Standing stones. The owner's rock-pack reference has tall spires STACKED from two or
    three lumps, so two of the three are lumps (each cut flat underneath, sunk a little into
    the one below, narrower and turned as they climb) unioned into one closed mesh; the third
    is a single tapering monolith with a steep slanted top."""
    if k == 0:
        _body(bm, rng, 8, rng.uniform(2.5, 2.9), (0.95, 0.8, 2.3), taper=0.28, amp=0.06, lobes=2)
        # SELF-REVIEW v11: a shallow top left a narrow crown that the chips turned into a crystal
        # point. A deep top cut gives the monolith a broad, blunt, slanted top.
        verts = _planes(bm, bm.verts[:], rng, 22, (0.55, 0.65), 5, (0.66, 0.78), (-0.03, 0.1), extra=1, chips=2)
        _bevel_breaks(bm, verts)
        _lean(bm.verts, rng, rng.uniform(0.08, 0.14))
        return
    tiers = [((0.95, 0.85, 1.3), 0.15), ((0.75, 0.68, 1.1), 0.18), ((0.55, 0.5, 0.95), 0.25)] if k == 1 \
        else [((1.0, 0.9, 1.5), 0.22), ((0.72, 0.65, 1.2), 0.25)]
    parts, z, drift = [], 0.0, Vector((0, 0, 0))
    for n, (half, taper) in enumerate(tiers):
        lb = bmesh.new()
        last = n == len(tiers) - 1
        _lump(lb, rng, half, taper, 18 if last else 8, rng.randint(4, 5),
              bottom=None if n == 0 else 0.55, chips=2 if last else 0)
        lo = min(v.co.z for v in lb.verts)
        hi = max(v.co.z for v in lb.verts)
        bmesh.ops.rotate(lb, cent=(0, 0, 0), matrix=Matrix.Rotation(rng.uniform(0, math.tau), 3, "Z"),
                         verts=lb.verts)
        sink = 0.0 if n == 0 else 0.25 * (hi - lo)
        bmesh.ops.translate(lb, vec=Vector((drift.x, drift.y, z - lo - sink)), verts=lb.verts)
        z = max(v.co.z for v in lb.verts) - 0.12 * (hi - lo)   # the next sits on this one's shoulders
        drift += Vector((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), 0))
        me = bpy.data.meshes.new("_lump")
        lb.to_mesh(me)
        lb.free()
        parts.append(me)
    union = _union(parts)
    bm.from_mesh(union)
    bpy.data.meshes.remove(union)
    _lean(bm.verts, rng, rng.uniform(0.05, 0.1))


def _slab(bm, rng, k):
    """(c) Table slab: wide and low with a big level top someone could stand on."""
    half, top_t, sides, chips = SLAB_CHARACTERS[k]
    _body(bm, rng, 8, rng.uniform(2.6, 3.0), half, amp=0.04, lobes=2, lobe_amp=(0.08, 0.16))
    verts = _planes(bm, bm.verts[:], rng, 3, top_t, sides, (0.66, 0.8), (-0.03, 0.25), chips=chips)
    _bevel_breaks(bm, verts)
    _lean(bm.verts, rng, rng.uniform(0.0, 0.04))


def _split(bm, rng, k):
    """(d) Split pair: one boulder cracked in two, the halves leaning apart so the crack opens
    toward the top. Two closed shells in one mesh; each half is a copy of the SAME body, so the
    crack faces match like a real break."""
    half = (1.1, rng.uniform(0.85, 0.95), rng.uniform(1.0, 1.15))
    _body(bm, rng, 7, rng.uniform(2.3, 2.7), half, lobes=2)
    verts = _planes(bm, bm.verts[:], rng, 10, (0.66, 0.76), 4, (0.72, 0.82), (-0.03, 0.25), extra=1, chips=2)
    _lean(bm.verts, rng, rng.uniform(0.03, 0.07))
    s = _plane(rng.uniform(0, math.tau), rng.uniform(-0.08, 0.08))
    centre = s * rng.uniform(-0.18, 0.18)
    geom_b = bmesh.ops.duplicate(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:])["geom"]
    set_b = {g for g in geom_b if isinstance(g, bmesh.types.BMVert)}
    verts_a = [v for v in bm.verts if v not in set_b]
    halves = []
    for vs, normal in ((verts_a, s), (list(set_b), -s)):
        support = max(v.co.dot(normal) for v in vs)
        low = min(v.co.dot(normal) for v in vs)
        # _cut measures t across the shell; place the plane through `centre`.
        t = ((centre.dot(normal) - low) / (support - low) - 0.5) / 0.5
        halves.append(_cut(bm, vs, normal, t))
    for vs in halves:
        _bevel_breaks(bm, vs)
    halves = [_verts_of(bm, vs) for vs in halves]
    gap = rng.uniform(0.07, 0.11)
    axis = s.cross(Vector((0, 0, 1))).normalized()
    z0 = min(v.co.z for v in bm.verts)
    ang = math.radians(rng.uniform(6.0, 8.5))
    for vs, sign in ((halves[0], -1), (halves[1], 1)):
        bmesh.ops.translate(bm, vec=s * sign * gap, verts=vs)
        pivot = centre + s * sign * gap
        pivot.z = z0
        bmesh.ops.rotate(bm, cent=pivot, matrix=Matrix.Rotation(-sign * ang, 3, axis), verts=vs)


def _cobble(bm, rng, k):
    """(e) Chunky cobble: a squarer body with more planes than anything else in the kit."""
    half = (1.0, rng.uniform(0.8, 0.95), rng.uniform(0.7, 0.85))
    _body(bm, rng, 6, rng.uniform(2.8, 3.3), half, amp=0.04, lobes=1)
    # SELF-REVIEW v6: a side plane leaning downward undercut the top into a mushroom lip; no side
    # plane faces down.
    # SELF-REVIEW v11: a tilted shallow top plus chips peaked one cobble into a pyramid. A level,
    # deeper top keeps every cobble a chunky block.
    verts = _planes(bm, bm.verts[:], rng, 12, (0.55, 0.65), 5, (0.66, 0.78), (0.0, 0.35), extra=1, chips=2)
    _bevel_breaks(bm, verts)
    _lean(bm.verts, rng, rng.uniform(0.02, 0.08))


BUILDERS = {"boulder": _boulder, "stack": _stack, "slab": _slab, "split": _split, "cobble": _cobble}


# ---------------------------------------------------------------- finishing

def _normalise(bm):
    """Widest horizontal extent to ACROSS, origin at the base centre, BURIED of the height below
    z = 0 so a stone placed at ground height is already sunk into it."""
    xs, ys, zs = ([v.co[i] for v in bm.verts] for i in range(3))
    f = ACROSS / max(max(xs) - min(xs), max(ys) - min(ys))
    cx, cy = (max(xs) + min(xs)) * 0.5, (max(ys) + min(ys)) * 0.5
    zlo, h = min(zs), (max(zs) - min(zs)) * f
    for v in bm.verts:
        v.co = Vector(((v.co.x - cx) * f, (v.co.y - cy) * f, (v.co.z - zlo) * f - BURIED * h))


def _sharp_edges(bm):
    lim = math.radians(SHARP_DEG)
    sharp = 0
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        e.smooth = True
        if len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim:
            e.smooth = False
            sharp += 1
    return sharp


def _box_uvs(bm):
    """World-scale box projection chosen per face by the dominant axis of its normal, mirrored
    per side so the texture never reads backwards: 1 UV unit = UV_METRES at scale 1."""
    uv = bm.loops.layers.uv.new("UVMap")
    s = 1.0 / UV_METRES
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for loop in f.loops:
            x, y, z = loop.vert.co
            if ax == 2:
                u, v = (x, y) if n.z > 0 else (-x, y)
            elif ax == 0:
                u, v = (y, z) if n.x > 0 else (-y, z)
            else:
                u, v = (-x, z) if n.y > 0 else (x, z)
            loop[uv].uv = (u * s, v * s)


def _rock_top(bm, passes=3):
    """Per-vertex normal z, smoothed over neighbours, for the material's lighter tops."""
    val = {v: v.normal.z for v in bm.verts}
    for _ in range(passes):
        nxt = {}
        for v in bm.verts:
            ns = [e.other_vert(v) for e in v.link_edges]
            nxt[v] = (val[v] * 2 + sum(val[w] for w in ns)) / (2 + len(ns))
        val = nxt
    return [val[v] for v in bm.verts]


def _bake_uvs(me):
    """"UVBake": a unique, non-overlapping unwrap (smart project, 0.02 margin) for the baked
    per-stone edge-wear mask. "UVMap" stays first, active and the render UV. A temporary object
    is linked for the operator and removed; selection, active object and mode are restored."""
    layer = me.uv_layers.new(name="UVBake")
    me.uv_layers.active = layer
    vl = bpy.context.view_layer
    was_active = vl.objects.active
    was_selected = [o for o in vl.objects if o.select_get()]
    for o in was_selected:
        o.select_set(False)
    tmp = bpy.data.objects.new("_rock_uv_tmp", me)
    bpy.context.scene.collection.objects.link(tmp)
    vl.objects.active = tmp
    tmp.select_set(True)
    # ⚠️ Smart project can lay one island over itself where the surface folds back on the
    # projection axis (the lumped spires' seams did, kit v13). Each result is rasterised and, if
    # anything overlaps, unwrapped again with a tighter angle limit (smaller, flatter islands).
    for limit in (60, 45, 33, 22, 12):
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=math.radians(limit), island_margin=0.02, area_weight=0.0,
                                 correct_aspect=True, scale_to_bounds=False)
        bpy.ops.object.mode_set(mode="OBJECT")
        if uv_overlap(me)[0] == 0:
            break
    me["rock_uvbake_angle"] = limit
    bpy.data.objects.remove(tmp)
    for o in was_selected:
        o.select_set(True)
    vl.objects.active = was_active
    me.uv_layers.active_index = 0
    me.uv_layers["UVMap"].active_render = True


def uv_overlap(me, layer="UVBake", res=1024):
    """Rasterise every UV triangle of `layer` at res x res (pixel centres, strictly inside, so
    triangles sharing an edge never count twice) and return (pixels covered twice or more,
    pixels covered, out-of-bounds UVs)."""
    uv = me.uv_layers[layer].data
    me.calc_loop_triangles()
    hits = {}
    oob = 0
    for tri in me.loop_triangles:
        pts = [uv[i].uv * res for i in tri.loops]
        oob += sum(1 for p in pts if not (-1e-4 <= p.x <= res + 1e-4 and -1e-4 <= p.y <= res + 1e-4))
        (x0, y0), (x1, y1), (x2, y2) = pts
        area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)
        if abs(area) < 1e-12:
            continue
        sgn = 1 if area > 0 else -1
        for py in range(max(0, int(min(y0, y1, y2))), min(res, int(max(y0, y1, y2)) + 1)):
            cy = py + 0.5
            for px in range(max(0, int(min(x0, x1, x2))), min(res, int(max(x0, x1, x2)) + 1)):
                cx = px + 0.5
                w0 = ((x1 - cx) * (y2 - cy) - (x2 - cx) * (y1 - cy)) * sgn
                w1 = ((x2 - cx) * (y0 - cy) - (x0 - cx) * (y2 - cy)) * sgn
                w2 = ((x0 - cx) * (y1 - cy) - (x1 - cx) * (y0 - cy)) * sgn
                if w0 > 1e-9 and w1 > 1e-9 and w2 > 1e-9:
                    hits[(px, py)] = hits.get((px, py), 0) + 1
    return sum(1 for c in hits.values() if c > 1), len(hits), oob


def rock_mesh(index, material=None):
    """Build kit stone `index` as a mesh datablock named rock_<family>_<nn>.

    ⚠️ A stone is only accepted if it passes `check_mesh` (closed manifold, no face through
    another, no UVBake overlap). The cuts and bevels are exact, but a bevel meeting a chip at a
    near-parallel angle can still fold a sliver through its neighbour (kit v13, one cobble), so a
    failing build is thrown away and rebuilt from the next seed in a fixed sequence. The seed used
    is stored as me["rock_seed"], so every build of the kit is the same."""
    family, seed = STONES[index]
    for attempt in range(8):
        me = _rock_mesh_once(index, seed + 1009 * attempt, material)
        c = check_mesh(me)
        if c["nonmanifold"] == 0 and c["self_hits"] == 0 and c["uvbake_overlap_px"] == 0 and c["volume"] > 0:
            me["rock_seed"] = seed + 1009 * attempt
            return me
        bpy.data.meshes.remove(me)
    raise RuntimeError(f"rock kit stone {index} failed its checks on every seed")


def _rock_mesh_once(index, seed, material):
    family = STONES[index][0]
    rng = random.Random(seed)
    bm = bmesh.new()
    BUILDERS[family](bm, rng, ROCK_FAMILIES[family].index(index))
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges[:], dist=1e-4)
    # The caps and the union seams are n-gons, some concave. Triangulate them here (beauty) so the
    # UVBake unwrap, the overlap check and the FBX export all see the same triangles: left to the
    # exporter, a concave n-gon folded three UVBake triangles over their neighbours (kit v12).
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4],
                          quad_method="BEAUTY", ngon_method="BEAUTY")
    _normalise(bm)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.normal_update()
    sharp = _sharp_edges(bm)
    _box_uvs(bm)
    bm.verts.index_update()
    tops = _rock_top(bm)
    tris = sum(len(f.verts) - 2 for f in bm.faces)
    n_in_family = ROCK_FAMILIES[family].index(index) + 1
    me = bpy.data.meshes.new(f"rock_{family}_{n_in_family:02d}")
    bm.to_mesh(me)
    bm.free()
    attr = me.attributes.new("rock_top", "FLOAT", "POINT")
    attr.data.foreach_set("value", tops)
    _bake_uvs(me)
    zs = [v.co.z for v in me.vertices]
    # Placement helpers the cove script reads off the datablock: the stone's top and bottom
    # relative to its origin (the ground contact point), in metres at scale 1. A stone whose
    # crown must sit just under a ledge floor goes at z = floor - gap - me["rock_top_z"] * sz.
    me["rock_family"] = family
    me["rock_top_z"] = max(zs)
    me["rock_bottom_z"] = min(zs)
    me["rock_tris"] = tris
    me["rock_sharp_edges"] = sharp
    if material is not None:
        me.materials.append(material)
    return me


def build_rock_kit(material=None):
    """Every kit stone, in STONES order, as mesh datablocks (no objects left in the scene)."""
    return [rock_mesh(i, material) for i in range(ROCK_KIT_SIZE)]


# ---------------------------------------------------------------- checks

def check_mesh(me):
    """Manifold, outward normals, no self-intersections, no sliver faces, and the UVBake
    overlap raster."""
    from mathutils.bvhtree import BVHTree
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.normal_update()
    nonman = sum(1 for e in bm.edges if not e.is_manifold)
    vol = bm.calc_volume(signed=True)
    tree = BVHTree.FromBMesh(bm)
    bm.faces.ensure_lookup_table()
    hits = 0
    for a, b in tree.overlap(tree):
        if a >= b:
            continue
        if set(bm.faces[a].verts) & set(bm.faces[b].verts):
            continue
        hits += 1
    min_area = min(f.calc_area() for f in bm.faces)
    shells, seen = 0, set()
    for v in bm.verts:
        if v in seen:
            continue
        shells += 1
        seen.update(_verts_of(bm, [v]))
    bm.free()
    over, covered, oob = uv_overlap(me)
    return {"nonmanifold": nonman, "volume": round(vol, 3), "self_hits": hits,
            "min_area": round(min_area, 6), "shells": shells,
            "uvbake_overlap_px": over, "uvbake_fill": round(covered / 1024 ** 2, 3), "uvbake_oob": oob}


# ---------------------------------------------------------------- preview

def _mat(name, colour, rough=0.85):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = colour + (1,)
    bsdf.inputs["Roughness"].default_value = rough
    return m


def _checker_mat():
    m = _mat("rock_checker", (0.5, 0.5, 0.5))
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    uvn = nt.nodes.new("ShaderNodeUVMap")
    uvn.uv_map = "UVMap"
    chk = nt.nodes.new("ShaderNodeTexChecker")
    chk.inputs["Scale"].default_value = 8.0   # 8 cells per UV unit: 0.5 m cells at scale 1
    chk.inputs["Color1"].default_value = (0.85, 0.66, 0.40, 1)
    chk.inputs["Color2"].default_value = (0.30, 0.18, 0.10, 1)
    nt.links.new(uvn.outputs["UV"], chk.inputs["Vector"])
    nt.links.new(chk.outputs["Color"], bsdf.inputs["Base Color"])
    return m


def _edge_mat():
    """What an edge-wear bake would see: 1 - dot(bevelled normal, true normal), Cycles only."""
    m = _mat("rock_edges", (0.62, 0.46, 0.28))
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bev = nt.nodes.new("ShaderNodeBevel")
    bev.samples = 8
    bev.inputs["Radius"].default_value = 0.06
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    dot = nt.nodes.new("ShaderNodeVectorMath")
    dot.operation = "DOT_PRODUCT"
    ramp = nt.nodes.new("ShaderNodeMapRange")
    ramp.inputs["From Min"].default_value = 0.985
    ramp.inputs["From Max"].default_value = 0.93
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.inputs[6].default_value = (0.62, 0.46, 0.28, 1)
    mix.inputs[7].default_value = (1.0, 0.95, 0.85, 1)
    nt.links.new(bev.outputs["Normal"], dot.inputs[0])
    nt.links.new(geo.outputs["Normal"], dot.inputs[1])
    nt.links.new(dot.outputs["Value"], ramp.inputs["Value"])
    nt.links.new(ramp.outputs["Result"], mix.inputs["Factor"])
    nt.links.new(mix.outputs[2], bsdf.inputs["Base Color"])
    return m


def _lineup(kit, tan):
    col = bpy.data.collections.new("rock_kit")
    bpy.context.scene.collection.children.link(col)
    # Front row low stones, back row the tall ones, so nothing hides behind a stack.
    order = ROCK_FAMILIES["cobble"] + ROCK_FAMILIES["slab"] + ROCK_FAMILIES["split"] \
        + ROCK_FAMILIES["boulder"] + ROCK_FAMILIES["stack"]
    for n, i in enumerate(order):
        me = kit[i]
        me.materials.clear()
        me.materials.append(tan)
        o = bpy.data.objects.new(me.name, me)
        o.location = ((n % 4) * 4.4, (n // 4) * 4.6, 0)
        col.objects.link(o)
    return col


def _preview_scene():
    scene = bpy.context.scene
    ground = bpy.data.meshes.new("ground")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=160)
    bm.to_mesh(ground)
    bm.free()
    ground.materials.append(_mat("ground_pale", (0.80, 0.74, 0.62)))
    g = bpy.data.objects.new("ground", ground)
    g.location = (6.6, 8, 0)
    scene.collection.objects.link(g)
    ref = bpy.data.meshes.new("scale_ref_1m60")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
    bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
    bm.to_mesh(ref)
    bm.free()
    ref.materials.append(_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
    r = bpy.data.objects.new("scale_ref_1m60", ref)
    r.location = (-3.2, -1.0, 0)
    scene.collection.objects.link(r)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color = 4.2, (1.0, 0.9, 0.76)
    sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(55), 0, math.radians(-130))   # raking from the left
    scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("world")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.62, 0.62, 0.6, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
    scene.world = world
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.lens = 36
    cam.location = (-7.5, -15.5, 11.5)
    target = Vector((6.4, 7.6, 0.6))
    cam.rotation_euler = (target - Vector(cam.location)).to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(cam)
    scene.camera = cam
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    scene.view_settings.view_transform = "AgX"
    for look in ("AgX - Punchy", "Punchy"):
        try:
            scene.view_settings.look = look
            break
        except TypeError:
            continue


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    tan = _mat("rock_tan", (0.62, 0.46, 0.28))
    kit = build_rock_kit()
    for me in kit:
        c = check_mesh(me)
        print(f"[lagoon-rocks] {me.name}: seed {me['rock_seed']}, uvbake angle {me['rock_uvbake_angle']}, tris {me['rock_tris']}, sharp edges {me['rock_sharp_edges']}, "
              f"top {me['rock_top_z']:.2f}, bottom {me['rock_bottom_z']:.2f}, {c}")
    _lineup(kit, tan)
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "lagoon_rocks.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = out.with_suffix(".blend1")
    if backup.exists():
        backup.unlink()
    print("[lagoon-rocks] saved", out)
    if not version:
        return
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    _preview_scene()
    scene = bpy.context.scene
    for tag, mat, engine in (("lineup", tan, "BLENDER_EEVEE"), ("checker", _checker_mat(), "BLENDER_EEVEE"),
                             ("edges", _edge_mat(), "CYCLES")):
        for me in kit:
            me.materials[0] = mat
        scene.render.engine = engine
        if engine == "CYCLES":
            scene.cycles.samples = 24
            scene.cycles.use_denoising = True
        path = PREVIEWS / f"rockkit_{tag}_v{version}.png"
        if path.exists():
            raise SystemExit(f"[lagoon-rocks] {path} exists: never overwrite a render, bump --preview")
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[lagoon-rocks] preview", path)


if __name__ == "__main__":
    main()
