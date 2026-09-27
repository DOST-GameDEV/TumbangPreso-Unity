"""Lagoon Court BOAT KIT: the final boat models (geometry and world-scale UVs; no texture).

  blender -b --python tools/author_lagoon_boats.py -- --preview N

Writes ArtSource/lagoon/lagoon_boats.blend and, with --preview N, renders in Logs/lagoon-blender/:
boatkit_lineup_vN.png (flat placeholder colours on flat turquoise water, a 1.6 m pink scale
cylinder), boatkit_close_vN.png (the same, closer), boatkit_beached_vN.png (the beached bangka on
its sand bank), boatkit_far_vN.png (the lineup from 30 m at eye height, the readability test) and
boatkit_checker_vN.png / boatkit_checkerclose_vN.png (a checker on "UVMap" per material, to prove
the world-scale unwrap: square cells, rows along each member). Never overwrites a render.

Importable with no side effects: author_lagoon_cove.py calls `build_boat(kind, seed)` in place of
the blockout `boat()` and gets back a Collection (not linked anywhere; the caller links it)
holding one root empty named by kind with every part parented to it.

KINDS (docs/LAGOON_REWORK_GUIDE.md § 8 step 4):

  * "bangka"          a slim dugout-style outrigger canoe, hull 5.3 to 5.9 m, 0.8 to 0.9 m beam,
                      about 4.4 m across the floats. Raised pointed bow and stern (the bow higher
                      and raked forward), a painted sheer stripe, two curved bamboo katig arms
                      reaching out and down to one slim bamboo float each side, one or two seat
                      planks and a paddle lying inside.
  * "lepa"            a Sama-Bajau houseboat, plank hull 8.0 to 8.8 m and 1.8 to 1.9 m beam, 10.2
                      to 11.2 m overall with its projecting prow and stern board, 0.5 m freeboard,
                      shelter crest about 1.6 m over the water. Lapstrake planking (three laps a
                      side), a low arched shelter amidships (bamboo hoops under a thatch or woven
                      mat cover), a short mast, floorboards, and a little household gear.
  * "bangka_beached"  the SAME bangka as "bangka" with the same seed, rolled onto one float and
                      tipped bow-up a little, as if pulled up on the sand.

  ORIGIN AND DRAFT. The root empty sits on the centre line at midships, bow toward +Y.
  "bangka" and "lepa": z = 0 is the WATERLINE. Place the root at z = water height and the boat
  floats correctly. Draft (keel below the waterline at midships): bangka 0.18 m, lepa 0.30 m;
  the bangka's float axes sit exactly on z = 0, so each float rides half submerged. The root
  carries boat_draft, boat_freeboard, boat_length, boat_beam and boat_overall_width.
  "bangka_beached": z = 0 is the SAND. Its lowest points (the keel and the down-side float) sink
  6 cm below z = 0, so place the root at the sand height under the boat's midships.

MATERIAL SLOTS (exact names, one material per object, so textures swap in later): plank (hull
planking, seats, floorboards, the crate), paint_hull (the painted hull body, cream white),
paint_trim (the sheer stripe), bamboo (arms, floats, hoops, poles), thatch (the shelter cover,
ridge roll and the rolled mat), timber (paddles, mast, the lepa's prow). A lepa's hull is
paint_hull on some seeds and bare plank on the others. One object per slot per boat, parented to
the root, each with a live Bevel modifier. Materials are looked up by name first, so this kit
and the house kit share "plank", "bamboo", "thatch" and "timber" in one file.

  THE STRIPE COLOUR is per boat, not per material: the paint_trim mesh carries a corner colour
  attribute "trim_tint" (exported as FBX vertex colour), and the placeholder material reads it.
  The allowed colours (TRIM_COLOURS) are red, yellow and teal-GREEN: nothing near offence orange
  #f87020 or defence blue #0080e8 (Art_Direction.md § 1). A textured paint_trim should multiply
  its texture by the vertex colour, so one texture serves every boat.

UVs: one map "UVMap", WORLD SCALE, 1 UV unit = 2 m everywhere (UV_METRES):
  * hulls: V along the hull's length (y), U around the girth (keel to sheer), so tiling planks run
    bow to stern; the lapstrake steps and the gunwale top continue the same girth measure.
  * bamboo, timber and every swept member: V along the member, U around it.
  * thatch cover: U along the shelter, V UP the arch from each eave to the crest (mirrored at the
    crest), so thatch courses lie level along the boat. The ridge roll runs U along its length.
  * plank boards and seats: V along the board.
  Every piece gets a random UV offset, so neighbouring members never show the same stretch.

What the owner's references ask for, and so what this builds:

  * CUTE AND CHUNKY (Art_Direction.md § 0): the bangka is stubby rather than racing slim, its bow
    sweeps up in one fat curve, the floats and arms are thick enough to read from the court at
    30 m, and nothing is a survey drawing (arms droop unevenly, floats upturn at the nose).
  * CLEARLY A BANGKA AND CLEARLY A LEPA from a distance: the bangka's silhouette is the outrigger
    spread (about 4.4 m across on a 0.85 m hull), the lepa's is the long, deeper hull, the forked
    projecting prow and the humped shelter.
  * CULTURALLY SPECIFIC MOTIFS ARE NOT COPIED ONTO EVERY BOAT (LAGOON_REWORK_GUIDE.md § 2, and the
    reference notes on the NCCA lepa photograph). The prow is a clean stylized beak, not a copy of
    any real carving, and it varies: the lower fork, the upturned tip and the stern board are each
    per-seed choices, and some lepas are bare planking with no stripe at all.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2). Seats, floorboards, arms, hoops and
the prow all PENETRATE what they meet; things resting on a surface sink 1 to 2 cm into it; the
prow and stern board ride 3 to 5 cm proud of the deck top rather than on it. `check_boat` proves
it (`coplanar` counts overlapping parallel faces of different shells within 4 mm) and proves no
part hangs in the air (`loose`: shells in no chain of intersections to the hull).

⚠️ THE HULL IS ONE CLOSED SHELL, NOT A BOX WITH A LID. A lofted solid (the outer section at every
station, the deck top straight across the sheer) has its open well cut by the EXACT boolean with
an inner loft that follows the outer section one wall thickness in. So the gunwale, the inside of
the hull and the decked ends are all one welded mesh, and the wall thickness is real.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "lagoon"
PREVIEWS = ROOT / "Logs" / "lagoon-blender"

KINDS = ("bangka", "lepa", "bangka_beached")
UV_METRES = 2.0       # 1 UV unit = 2 m: every tiling texture in this kit covers 2 m
Z = Vector((0.0, 0.0, 1.0))
BEACH_SINK = 0.06     # bangka_beached: how far its lowest points sink below the sand (z = 0)

# Placeholder colours, flat; the texture pass replaces them. paint_trim's is only the fallback:
# the material reads the per-boat "trim_tint" attribute.
SLOTS = {
    "plank": (0.60, 0.42, 0.25),
    "paint_hull": (0.93, 0.90, 0.80),
    "paint_trim": (0.70, 0.10, 0.08),
    "bamboo": (0.72, 0.64, 0.36),
    "thatch": (0.66, 0.50, 0.26),
    "timber": (0.40, 0.25, 0.14),
}
# The stripe colours, LINEAR RGB. Red, yellow and a teal-GREEN (hue about 160 degrees, well clear
# of defence blue's 207); no orange anywhere near offence orange's 24 degrees: the yellow sits at
# about 48 degrees and the red at about 3.
TRIM_COLOURS = {
    "red": (0.62, 0.035, 0.03),
    "yellow": (0.92, 0.62, 0.04),
    "teal_green": (0.02, 0.36, 0.20),
}
# Per slot: (bevel width m, angle above which an edge is sharp, harden normals). The hull's 2 cm
# bevel is what rounds the gunwale into a chunky rim; bamboo keeps its eight sides smooth.
FINISH = {
    "plank": (0.010, 30.0, True),
    "paint_hull": (0.020, 30.0, True),
    "paint_trim": (0.008, 30.0, True),
    "bamboo": (0.006, 50.0, False),
    "thatch": (0.020, 40.0, False),
    "timber": (0.010, 30.0, True),
}


# ---------------------------------------------------------------- pieces

class Piece:
    """One closed shell under construction, in its own bmesh with its own UV layer, so every
    primitive can be cut and unwrapped on its own before it joins its slot's mesh."""

    def __init__(self, bm=None):
        self.bm = bm or bmesh.new()
        self.uv = self.bm.loops.layers.uv.get("UVMap") or self.bm.loops.layers.uv.new("UVMap")


class Boat:
    """Collects pieces per slot for one boat."""

    def __init__(self, kind, seed):
        self.kind, self.seed = kind, seed
        # The beached bangka is the floating one tilted, so both draw from the same stream.
        self.rng = random.Random(f"lagoon-boat:{'bangka' if kind == 'bangka_beached' else kind}:{seed}")
        self.pieces = {s: [] for s in SLOTS}
        self.info = {}
        self.floats = []          # float pieces, for the waterline check
        self.hull_slot = "paint_hull"
        self.trim = "red"

    def add(self, slot, pc):
        if pc is None or not pc.bm.faces:
            return None
        self.pieces[slot].append(pc)
        return pc

    def uv_off(self):
        return (self.rng.random(), self.rng.random())


def _perp(d):
    a = Z if abs(d.z) < 0.9 else Vector((1, 0, 0))
    e1 = a.cross(d).normalized()
    return e1, d.cross(e1).normalized()


def _uv_frame(pc, U, V, W, off=(0.0, 0.0)):
    """Box projection in a LOCAL frame (U, V, W): a face picks the frame axis its normal is
    closest to and is projected on the other two, keeping V on V wherever it can (the house kit's
    rule). A board built along V has V along its length on all four sides."""
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


def loft(rings, uvs, caps=True, off=(0.0, 0.0)):
    """A closed solid through `rings` (each a closed loop of Vectors, all the same length), capped
    at both ends. `uvs[j]` holds len(ring) + 1 (u, v) pairs in METRES: the extra one is the first
    point again at the end of the loop, so a wrap-around seam carries the full girth. The caps are
    projected flat in their own plane."""
    pc = Piece()
    bm, uvl = pc.bm, pc.uv
    s = 1.0 / UV_METRES
    V = [[bm.verts.new(p) for p in ring] for ring in rings]
    n = len(rings[0])
    for j in range(len(rings) - 1):
        for k in range(n):
            k2 = (k + 1) % n
            f = bm.faces.new((V[j][k], V[j][k2], V[j + 1][k2], V[j + 1][k]))
            for loop, (u, v) in zip(f.loops, (uvs[j][k], uvs[j][k + 1], uvs[j + 1][k + 1], uvs[j + 1][k])):
                loop[uvl].uv = (u * s + off[0], v * s + off[1])
    if caps:
        for j, nxt in ((0, 1), (len(rings) - 1, len(rings) - 2)):
            c0 = sum(rings[j], Vector()) / n
            c1 = sum(rings[nxt], Vector()) / n
            e1, e2 = _perp((c1 - c0).normalized())
            f = bm.faces.new(V[j])
            for loop in f.loops:
                q = loop.vert.co
                loop[uvl].uv = (q.dot(e1) * s + off[0], q.dot(e2) * s + off[1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4], quad_method="BEAUTY",
                          ngon_method="BEAUTY")
    return pc


def _catmull(pts, per=8):
    """A smooth polyline through control points (Catmull-Rom, ends clamped)."""
    P = [Vector(p) for p in pts]
    P = [P[0]] + P + [P[-1]]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for k in range(per):
            t = k / per
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(P[-2])
    return out


def sweep(b, slot, pts, radius, sides=8, step=0.14, nodes=0.0, up=None, phase=0.0, dome=False):
    """A round (or elliptical) member along the polyline `pts`. `radius(s, L)` returns (rx, ry)
    at arc length s of L (or pass a number). `nodes` puts a bamboo node ring every that many
    metres (a 12 % swell, irregularly spaced). `up` fixes the section's ry axis toward a world
    direction (a paddle blade or the prow lying flat); otherwise the frame is parallel-transported
    so a bent arm never twists. `phase` turns the section (radians): side-by-side poles are
    given different phases so no two of their flat facets ever face each other in one plane.
    `dome` rounds both ends over their last 5 cm (bamboo floats and arms), so an end reads as a
    cut, eased pole rather than a sawn-off pencil. UVs: V along the length, U around it."""
    P = [Vector(p) for p in pts]
    cum = [0.0]
    for a, c in zip(P, P[1:]):
        cum.append(cum[-1] + (c - a).length)
    L = cum[-1]

    def at(s):
        s = max(0.0, min(L, s))
        for i in range(len(P) - 1):
            if cum[i + 1] >= s:
                seg = cum[i + 1] - cum[i]
                return P[i].lerp(P[i + 1], 0.0 if seg < 1e-9 else (s - cum[i]) / seg)
        return P[-1]
    n = max(2, int(math.ceil(L / step)))
    samples = [(L * i / n, 1.0) for i in range(n + 1)]
    if nodes:
        k = nodes * (0.4 + 0.4 * b.rng.random())
        while k < L - 0.1:
            samples = [x for x in samples if abs(x[0] - k) > 0.05]
            samples += [(k - 0.035, 1.0), (k, 1.12), (k + 0.035, 1.0)]
            k += nodes * b.rng.uniform(0.8, 1.2)
        samples.sort()
    if dome and L > 0.3:
        samples = [x for x in samples if 0.06 < x[0] < L - 0.06]
        samples = ([(0.0, 0.55), (0.02, 0.86), (0.05, 1.0)] + samples
                   + [(L - 0.05, 1.0), (L - 0.02, 0.86), (L, 0.55)])
    rings, uvs = [], []
    prev = None
    ou, ov = b.uv_off()
    for s, fac in samples:
        T = (at(s + 0.01) - at(s - 0.01))
        T = T.normalized() if T.length > 1e-9 else Vector((0, 1, 0))
        if up is not None:
            e1 = T.cross(up)
            e1 = e1.normalized() if e1.length > 1e-6 else _perp(T)[0]
            e2 = e1.cross(T).normalized()
        elif prev is None:
            e1, e2 = _perp(T)
        else:
            e2 = prev - T * prev.dot(T)
            e2 = e2.normalized() if e2.length > 1e-6 else _perp(T)[1]
            e1 = T.cross(e2).normalized()
            e2 = e1.cross(T).normalized()
        prev = e2
        if callable(radius):
            rx, ry = radius(s, L)
        elif isinstance(radius, tuple):
            rx, ry = radius
        else:
            rx = ry = radius
        rx, ry = rx * fac, ry * fac
        c = at(s)
        ring = [c + e1 * (math.cos(a) * rx) + e2 * (math.sin(a) * ry)
                for a in (phase + math.tau * k / sides for k in range(sides))]
        rings.append(ring)
        arc = math.tau * (rx + ry) * 0.5 / sides
        uvs.append([(k * arc, s) for k in range(sides + 1)])
    pc = loft(rings, uvs, off=(ou, ov))
    return b.add(slot, pc)


def board(b, slot, p0, p1, w, t, up=Z):
    """A box member from p0 to p1: `w` across (horizontal when `up` is Z), `t` along `up`. V runs
    along the member."""
    p0, p1 = Vector(p0), Vector(p1)
    d = (p1 - p0).normalized()
    side = up.cross(d)
    if side.length < 1e-6:
        side = Vector((1, 0, 0)).cross(d)
    side.normalize()
    upv = d.cross(side)
    rings = []
    for q in (p0, p1):
        rings.append([q + side * (i * w / 2) + upv * (k * t / 2) for i, k in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
    pc = loft(rings, [[(0, 0)] * 5, [(0, 0)] * 5])
    _uv_frame(pc, side, d, upv, b.uv_off())
    return b.add(slot, pc)


def _boolean(pc, cutter):
    """Subtract a closed cutter piece from a closed piece with the EXACT boolean, through
    temporary objects that are removed again (build_boat leaves nothing in the scene)."""
    scene = bpy.context.scene
    temp = []

    def obj(p, name):
        me = bpy.data.meshes.new(name)
        p.bm.to_mesh(me)
        o = bpy.data.objects.new(name, me)
        scene.collection.objects.link(o)
        temp.append(o)
        return o
    base = obj(pc, "_boat_bool_base")
    co = obj(cutter, "_boat_bool_cut")
    cutter.bm.free()
    co.hide_render = co.hide_viewport = True
    m = base.modifiers.new("well", "BOOLEAN")
    m.operation, m.solver, m.object = "DIFFERENCE", "EXACT", co
    # ⚠️ update(): once anything has evaluated the scene, the depsgraph handed back is the OLD one
    # and does not yet hold these temporary objects (the house kit's capilla lost its windows).
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
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4], quad_method="BEAUTY",
                          ngon_method="BEAUTY")
    return Piece(bm)


# ---------------------------------------------------------------- the hull

class Hull:
    """One hull's shape, as functions of t (0 at the stern, 1 at the bow) and th (the angle round
    the section, 0 at the starboard sheer, pi/2 at the keel, pi at the port sheer).

    The section is a superellipse of exponent n (2 an ellipse, 3 a flat-bottomed box), narrowed
    toward the keel by `flare` so the sides lean out. The sheer rises toward both ends as |u|^3
    (u = 2t - 1), the keel as |u|^4, so the middle stays flat and the ends sweep up in one curve.
    Near the ends the whole section is RAKED: pushed along y in proportion to its height above
    the keel, so the stem leans forward over the water instead of standing as a flat wall."""

    def __init__(self, **kw):
        self.__dict__.update(kw)

    def B(self, t):
        u = abs(2 * t - 1)
        return max(self.bend, self.bmax * max(0.0, 1.0 - u ** 2.2) ** 0.5)

    def Zs(self, t):
        u = 2 * t - 1
        return self.zs + (self.bow_rise if u > 0 else self.stern_rise) * abs(u) ** 3

    def Zk(self, t):
        u = 2 * t - 1
        return self.zk + (self.bow_keel if u > 0 else self.stern_keel) * abs(u) ** 4

    def rake(self, t):
        u = 2 * t - 1
        a = max(0.0, (abs(u) - 0.55) / 0.45)
        a = a * a * (3 - 2 * a)
        return (self.rake_bow if u > 0 else -self.rake_stern) * a

    def cav_e(self, t):
        """The well's plan shape: full width in the middle, rounding off to a broad, near-square
        end wall. SELF-REVIEW v5: floored at 16 % of the width, the well's last station pinched
        into a narrow slot that cut a notch into the deck; at 55 % the end is a bulkhead."""
        t0, t1 = self.cav
        c = (t - t0) / (t1 - t0)
        return max(0.55, (1.0 - abs(2 * c - 1) ** 6) ** 0.35)

    def _xz(self, t, th, inner):
        B, zs, zk = self.B(t), self.Zs(t), self.Zk(t)
        if inner:
            B, zk = (B - self.tw) * self.cav_e(t), zk + self.tb
        c, s = math.cos(th), math.sin(th)
        x = B * math.copysign(abs(c) ** (2 / self.n), c)
        z = zs - (zs - zk) * abs(s) ** (2 / self.n)
        x *= 1.0 - self.flare * (1.0 - (z - zk) / (zs - zk))
        return x, z

    def point(self, t, th, off=0.0, inner=False):
        """A point of the section at station t, angle th, pushed `off` metres along the outward
        section normal (the lapstrake laps and the stripe)."""
        x, z = self._xz(t, th, inner)
        if off:
            x1, z1 = self._xz(t, th - 1e-3, inner)
            x2, z2 = self._xz(t, th + 1e-3, inner)
            tx, tz = x2 - x1, z2 - z1
            ln = math.hypot(tx, tz) or 1.0
            nx, nz = tz / ln, -tx / ln
            if nx * x + nz * (z - (self.Zs(t) + self.Zk(t)) / 2) < 0:
                nx, nz = -nx, -nz
            x, z = x + nx * off, z + nz * off
        y = (t - 0.5) * self.L + self.rake(t) * (z - self.Zk(t))
        return Vector((x, y, z))

    def th_at_depth(self, t, d):
        """The starboard angle whose outer section point lies `d` metres below the sheer."""
        H = self.Zs(t) - self.Zk(t)
        return math.asin(min(1.0, max(0.0, d / H)) ** (self.n / 2))

    def half_width_at(self, t, z, inner=True):
        """Half the section's width at height z (the well by default), for seats and boards."""
        lo, hi = 0.0, math.pi / 2
        for _ in range(40):
            mid = (lo + hi) / 2
            if self._xz(t, mid, inner)[1] > z:
                lo = mid
            else:
                hi = mid
        return self._xz(t, lo, inner)[0]


def _hull_ths(h, K):
    """(th, lap offset) samples round the section. With laps, each strake boundary appears twice:
    the keel side of the step, then the upper strake one lap further out, so the upper strake's
    lower edge overlaps the one below like real clinker planking."""
    base = [math.pi * k / K for k in range(K + 1)]
    out = []
    bounds = sorted({math.pi / 2 + sg * d for d in h.laps for sg in (-1, 1)})

    def laps_out(th):
        return sum(1 for d in h.laps if abs(th - math.pi / 2) > d + 1e-9)
    for th in base:
        out.append((th, laps_out(th) * h.lip))
    for bt in bounds:
        out = [o for o in out if abs(o[0] - bt) > 0.05]
        lo_n = sum(1 for d in h.laps if abs(bt - math.pi / 2) > d + 1e-9)
        hi_n = lo_n + 1
        # keel side first when walking from th = 0 is the UPPER strake (th < pi/2 is starboard,
        # walking toward the keel the laps decrease).
        if bt < math.pi / 2:
            out += [(bt - 1e-4, hi_n * h.lip), (bt + 1e-4, lo_n * h.lip)]
        else:
            out += [(bt - 1e-4, lo_n * h.lip), (bt + 1e-4, hi_n * h.lip)]
    out.sort(key=lambda o: o[0])
    return out


def build_hull(b, h, slot):
    """The hull: an outer loft, capped (deck straight across the sheer), minus the well."""
    ths = _hull_ths(h, h.K)
    rings, uvs = [], []
    for j in range(h.stations + 1):
        t = j / h.stations
        ring = [h.point(t, th, off) for th, off in ths]
        rings.append(ring)
        u, row = 0.0, [(0.0, ring[0].y)]
        for a, c in zip(ring, ring[1:] + ring[:1]):
            u += (c - a).length
            row.append((u, c.y))
        uvs.append(row)
    off = b.uv_off()
    hull = loft(rings, uvs, off=off)
    # The well: the inner section one wall thickness in, carried straight up past the deck.
    t0, t1 = h.cav
    top = max(h.Zs(t) for t in (t0, t1, 0.5)) + 0.4
    K = h.K
    rings, uvs = [], []
    for j in range(h.cav_stations + 1):
        t = t0 + (t1 - t0) * j / h.cav_stations
        ring = [h.point(t, math.pi * k / K, inner=True) for k in range(K + 1)]
        yl = ring[-1].y
        ring += [Vector((ring[-1].x, yl, top)), Vector((ring[0].x, ring[0].y, top))]
        rings.append(ring)
        u, row = 0.0, [(0.0, ring[0].y)]
        for a, c in zip(ring, ring[1:] + ring[:1]):
            u += (c - a).length
            row.append((u, c.y))
        uvs.append(row)
    cutter = loft(rings, uvs, off=off)
    pc = _boolean(hull, cutter)
    b.add(slot, pc)
    return pc


def stripe(b, h, d0, d1, t0, t1, lap_off, proud=0.022):
    """The painted sheer stripe: a band from d0 to d1 metres below the sheer, standing `proud`
    out of the hull and embedded 3 cm into the wall, one per side. Its ends dive back into the hull
    over their last 25 cm, so it never ends in a cut-off block. ⚠️ The dive is a fixed length, not
    a share of the hull: v3 spread it over 6 % of a lepa (half a metre), and on the bow's curve the
    band's face met the hull's at under 2.5 degrees, which the coplanar check rightly calls a
    z-fight."""
    for side in (1, -1):
        rings, uvs = [], []
        n = int((t1 - t0) * h.stations * 1.5) + 2
        for j in range(n + 1):
            t = t0 + (t1 - t0) * j / n
            e = max(0.0, min(1.0, (t - t0) * h.L / 0.25, (t1 - t) * h.L / 0.25))
            e = e * e * (3 - 2 * e)
            out = lap_off - 0.02 + (proud + 0.02) * e
            inn = lap_off - 0.03
            ring = []
            for d, o in ((d0, out), (d1, out), (d1, inn), (d0, inn)):
                th = h.th_at_depth(t, d)
                if side < 0:
                    th = math.pi - th
                ring.append(h.point(t, th, o))
            rings.append(ring)
            y = ring[0].y
            dz = abs(ring[0].z - ring[1].z)
            uvs.append([(0.0, y), (dz, y), (dz + 0.04, y), (2 * dz + 0.04, y), (2 * dz + 0.08, y)])
        b.add("paint_trim", loft(rings, uvs, off=b.uv_off()))


# ---------------------------------------------------------------- the bangka

def build_bangka(b):
    rng = b.rng
    L = rng.uniform(5.3, 5.9)
    h = Hull(L=L, bmax=rng.uniform(0.40, 0.45), bend=0.05, zk=-0.18, zs=0.30 + rng.uniform(-0.01, 0.03),
             bow_rise=rng.uniform(0.44, 0.56), stern_rise=rng.uniform(0.26, 0.34), bow_keel=0.30,
             stern_keel=0.22, n=2.3, flare=0.14, rake_bow=0.55, rake_stern=0.35, laps=(), lip=0.0,
             tw=0.06, tb=0.07, cav=(0.14, 0.86), K=18, stations=44, cav_stations=30)
    build_hull(b, h, "paint_hull")
    stripe(b, h, 0.035, 0.15, 0.04, 0.96, 0.0)
    b.trim = rng.choice(list(TRIM_COLOURS))
    # SEATS: one or two thwarts across the well 12 cm under the sheer, their ends run 3 cm into
    # the wall (the wall is 6 cm, so they never show outside).
    zseat = min(h.Zs(0.3), h.Zs(0.6)) - 0.13
    seat_ts = [0.28, 0.58] if rng.random() < 0.75 else [0.30]
    seats = []
    for t in seat_ts:
        t += rng.uniform(-0.02, 0.02)
        y = (t - 0.5) * L
        hw = h.half_width_at(t, zseat) + 0.03
        board(b, "plank", (-hw, y, zseat), (hw, y, zseat), 0.24, 0.045, up=Z)
        seats.append((t, y))
    # KATIG ARMS: two curved bamboo crossbeams lying across the gunwale, drooping out and down to
    # the floats in one smooth bend (cos^0.7), each a little different.
    # SELF-REVIEW v1: at 8.5 cm floats and 5.5 cm arms the outrigger vanished from 30 m (the far
    # shot showed a white canoe with faint threads). Chunkier, as the style asks.
    S = rng.uniform(2.05, 2.3)
    rf = 0.11
    arms = []
    for t in (0.5 - 0.21 + rng.uniform(-0.02, 0.02), 0.5 + 0.2 + rng.uniform(-0.02, 0.02)):
        y = (t - 0.5) * L
        zc = h.Zs(t) + 0.04           # centre 4 cm over the sheer: a 6.5 cm arm sinks 2.5 cm in
        zf = rf * 0.45                # over the float: the arm sinks into the float's top
        droop = rng.uniform(0.6, 0.8)
        pts = []
        for i in range(25):
            x = -(S + 0.1) + 2 * (S + 0.1) * i / 24
            u = min(1.0, abs(x) / S)
            z = zf + (zc - zf) * math.cos(u * math.pi / 2) ** droop if abs(x) <= S else zf - (abs(x) - S) * 0.3
            pts.append((x, y, z))
        sweep(b, "bamboo", pts, 0.065, sides=8, nodes=0.55, dome=True)
        arms.append(y)
    # FLOATS: slim bamboo poles each side, the axis ON the waterline so each rides half under;
    # the nose (bow end) upturns 12 cm so it reads as a float and not a log. SELF-REVIEW v5: a 5 cm
    # rear upturn plus the end taper kinked the last segment into what looked like a snapped-off
    # cap; the rear is straight now and both ends are domed.
    Lf = L * rng.uniform(0.62, 0.7)
    yc = rng.uniform(0.0, 0.15)
    for sx in (-S, S):
        pts = []
        for i in range(21):
            f = i / 20
            y = yc - Lf / 2 + Lf * f
            z = 0.0
            if f > 0.85:
                z += 0.12 * ((f - 0.85) / 0.15) ** 2
            pts.append((sx, y, z))

        def rad(s, Ln):
            e = min(1.0, s / 0.35, (Ln - s) / 0.45)
            e = e * e * (3 - 2 * e)
            r = rf * (0.8 + 0.2 * e)
            return r, r
        pc = sweep(b, "bamboo", pts, rad, sides=8, nodes=0.5, dome=True)
        b.floats.append(pc)
    # THE PADDLE: grip resting on the aft edge of the forward seat (sunk 1 cm), blade lying down
    # the well on the floor behind it. Tilted, so it never lies parallel to the seat or the floor
    # (a flat blade laid ON a seat top is exactly the coplanar pair this kit forbids). SELF-REVIEW
    # v5: rested mid-seat, the descending shaft ran down through the plank.
    # With a single seat (well aft) the blade would reach into the solid stern deck, so the paddle
    # then lies forward from the seat's front edge instead.
    ta, ya = seats[-1]
    aft = ta - 1.3 / L > h.cav[0] + 0.06
    a = Vector((rng.uniform(-0.1, 0.1), ya - 0.09 if aft else ya + 0.09, zseat + 0.0225 + 0.012))
    bt = ta - 1.3 / L if aft else ta + 1.3 / L
    c = Vector((rng.uniform(-0.03, 0.03), (bt - 0.5) * L, h.Zk(bt) + h.tb + 0.006))
    paddle(b, a, c)
    b.info.update(length=L, beam=2 * h.B(0.5), overall_width=2 * (S + 0.1), draft=-h.zk,
                  freeboard=h.Zs(0.5), float_offset=S, float_radius=rf)
    return h


def paddle(b, a, c):
    """A paddle from the grip at `a` to the blade tip at `c`: one swept piece whose section grows
    from a round shaft into a flat, round-ended blade over the last 40 %, lying flat."""
    L = (c - a).length
    d = (c - a).normalized()

    def rad(s, Ln):
        f = s / Ln
        if f < 0.04:                         # a small grip knob
            return 0.03, 0.03
        if f < 0.6:
            return 0.022, 0.022
        g = min(1.0, (f - 0.6) / 0.1)
        tip = min(1.0, (1.0 - f) / 0.12)
        w = 0.022 + (0.085 - 0.022) * g
        w *= math.sqrt(max(0.05, tip)) if tip < 1 else 1.0
        return w, max(0.012, 0.022 - 0.01 * g)
    pts = [a + d * (L * i / 16) for i in range(17)]
    sweep(b, "timber", pts, rad, sides=10, step=0.08, up=Z)


# ---------------------------------------------------------------- the lepa

def build_lepa(b):
    rng = b.rng
    L = rng.uniform(8.0, 8.8)
    # SELF-REVIEW v4 (profile view): at 0.42 m freeboard with a gentle end rise the lepa was a long
    # slim canoe, a bangka without its outriggers. Deeper and with a stronger sheer it reads as the
    # bigger, heavier household boat.
    h = Hull(L=L, bmax=rng.uniform(0.78, 0.86), bend=0.08, zk=-0.30, zs=0.52 + rng.uniform(-0.02, 0.03),
             bow_rise=rng.uniform(0.46, 0.54), stern_rise=rng.uniform(0.34, 0.40), bow_keel=0.34,
             stern_keel=0.30, n=3.0, flare=0.2, rake_bow=0.25, rake_stern=0.3, laps=(0.55, 0.95, 1.28),
             lip=0.04, tw=0.07, tb=0.08, cav=(0.11, 0.89), K=22, stations=56, cav_stations=40)
    painted = rng.random() < 0.5
    b.hull_slot = "paint_hull" if painted else "plank"
    build_hull(b, h, b.hull_slot)
    top_lap = len(h.laps) * h.lip
    striped = painted or rng.random() < 0.5
    if striped:
        stripe(b, h, 0.03, 0.13, 0.05, 0.95, top_lap)
    trim = rng.choice(list(TRIM_COLOURS))
    b.trim = trim if striped else "none"
    zfloor = h.Zk(0.5) + h.tb + 0.24
    # FLOORBOARDS across the well, their ends 2.5 cm into the 7 cm wall; boards where the well is
    # too narrow to stand in are left out, so the decked ends stay clean.
    t = h.cav[0] + 0.02
    edges = []
    while t < h.cav[1] - 0.02:
        y = (t - 0.5) * L
        hw = h.half_width_at(t, zfloor)
        if hw > 0.22:
            board(b, "plank", (-hw - 0.025, y, zfloor), (hw + 0.025, y, zfloor), 0.25, 0.04)
            edges += [y - 0.125, y + 0.125]
        t += 0.27 / L

    def clear(y):
        """Nudge a gear end off any board edge: an end cap landing within a few mm of a board's
        side face is a pair of parallel faces sharing a plane (v3: a pole end and a paddle tip)."""
        for e in edges:
            if abs(y - e) < 0.02:
                return e + (0.03 if y >= e else -0.03)
        return y
    # THE SHELTER: bamboo hoops standing in the gunwales under an arched cover whose eaves hang
    # just outside the hull. A woven cover (no ridge roll) on some seeds, thatch on the others.
    Ls = rng.uniform(2.6, 3.3)
    ys0 = rng.uniform(-0.14, -0.04) * L - Ls / 2
    ha = rng.uniform(0.95, 1.12)
    thick, eave = 0.06, 0.1

    def xa_of(t):
        """The middle of the gunwale's top at station t: where each hoop is stepped."""
        return h.B(t) + (top_lap - h.tw) / 2

    ymid = ys0 + Ls / 2

    def ha_at(t):
        """The arch's height at station t: highest in the middle, 16 % lower at the cover's ends.
        SELF-REVIEW v4: an arch of one height is a flat-topped box seen from the side (the view
        from the court); the dip gives the shelter a rounded hump in profile too."""
        u = ((t - 0.5) * L - ymid) / (Ls / 2 + 0.12)
        return ha * (1.0 - 0.16 * u * u)

    def cover_profile(t, d):
        """(x, z) of the cover's arch at station t, pushed d outward (0 is its inner face). Its
        springing rests on the gunwale 3 cm up; the eave then hangs out past the hull's side."""
        zg = h.Zs(t)
        Rx, Rz, zb = xa_of(t) + 0.023 + d, ha_at(t) + d, zg + 0.03
        return [(Rx * math.cos(p), zb + Rz * math.sin(p) ** 0.7) for p in
                (math.pi * k / 16 for k in range(17))], (Rx, zb)
    rings, uvs = [], []
    ncov = int(Ls / 0.22) + 2
    wob = rng.uniform(0, 10)
    for j in range(ncov + 1):
        y = ys0 - 0.12 + (Ls + 0.24) * j / ncov
        t = y / L + 0.5
        inner, (Rx, zb) = cover_profile(t, 0.0)
        outer, _ = cover_profile(t, thick)
        dz = 0.03 * math.sin(y * 3.1 + wob)          # a gently wavy eave, not a ruled line
        tip_o = [(Rx + thick + eave, zb - 0.12 + dz)]
        tip_i = [(Rx + eave - 0.01, zb - 0.13 + dz)]
        prof = (tip_o + outer + [(-x, z) for x, z in tip_o]
                + [(-x, z) for x, z in tip_i] + list(reversed(inner)) + tip_i)
        ring = [Vector((x, y, z)) for x, z in prof]
        rings.append(ring)
        # V up from each eave, mirrored at the crest; U along the boat. The inner face runs the
        # same points in reverse, so it takes the outer face's V in reverse.
        m = len(tip_o) * 2 + len(outer)
        acc, cum = 0.0, [0.0]
        for a, c in zip(ring[:m - 1], ring[1:m]):
            acc += (c - a).length
            cum.append(acc)
        vo = [min(v, acc - v) for v in cum]
        row = [(y, v) for v in vo + list(reversed(vo))]
        row.append((y, vo[0]))
        uvs.append(row)
    b.add("thatch", loft(rings, uvs, off=b.uv_off()))
    woven = rng.random() < 0.4
    if not woven:
        # the ridge roll follows the crest's dip, sunk 5 cm into the cover
        ridge = []
        for k in range(13):
            y = ys0 - 0.2 + (Ls + 0.4) * k / 12
            t = y / L + 0.5
            ridge.append((0, y, h.Zs(t) + 0.03 + ha_at(t) + thick + 0.02))
        sweep(b, "thatch", ridge, (0.09, 0.07), sides=10, step=0.2)
    nh = 4 if Ls < 3.0 else 5
    for i in range(nh):
        y = ys0 + 0.12 + (Ls - 0.24) * i / (nh - 1)
        t = y / L + 0.5
        # Hoop centre 1 cm inside the cover's inner face, so the hoop runs 2.5 cm INTO the 6 cm
        # cover: v1 sat it 1.2 cm in and a hoop facet lay within 4 mm of the cover's face.
        inner, (Rx, zb) = cover_profile(t, -0.01)
        xa = xa_of(t) + 0.013
        pts = [(xa, y, h.Zs(t) - 0.07)] + [(x, y, z) for x, z in inner] + [(-xa, y, h.Zs(t) - 0.07)]
        sweep(b, "bamboo", pts, 0.035, sides=8, step=0.1)
    # THE MAST: a short pole ahead of the shelter, stepped on the floor, leaning back a little.
    ym = ys0 + Ls + rng.uniform(0.5, 0.9)
    tm = ym / L + 0.5
    lean = rng.uniform(0.04, 0.09)
    zb0 = h.Zk(tm) + h.tb - 0.03
    mh = rng.uniform(2.4, 2.9)

    def mrad(s, Ln):
        r = 0.08 - 0.03 * s / Ln
        return r, r
    sweep(b, "timber", [(0, ym, zb0), (0, ym - lean * mh, zb0 + mh)], mrad, sides=8, step=0.3)
    # THE PROW: a clean stylized beak running out of the bow deck and projecting forward, riding
    # 5 cm proud of the deck; its tip upturns on some seeds. A lower fork just above the water on
    # others. Neither is a copy of a real carving (LAGOON_REWORK_GUIDE.md § 2).
    # SELF-REVIEW v1: started 1.1 m back, over the open well, the beak read as a pole lying in the
    # boat. It now starts on the bow deck, 25 cm clear of the well, as a broad flat tongue that
    # narrows forward: a carved piece, not a stick.
    zse = h.Zs(1.0)
    ybow = h.point(1.0, 0.0).y
    # Over the deck its centre follows the hull's own sheer (and rake) 1 cm up, so its underside
    # stays 6 cm under the deck until it clears the stem: v2 ran a straight chord over the rising
    # deck and the underside grazed the deck top within 4 mm (a z-fight the check caught).
    t0 = h.cav[1] + 0.25 / L
    curl = rng.random() < 0.6
    # The root starts 6 cm down in the deck and rises out of it (v5: it began at full height and
    # its end cap stood on the deck as a block).
    ctrl = [(0, h.point(t0, 0.0).y, h.Zs(t0) - 0.06)]
    ctrl += [(0, h.point(t, 0.0).y, h.Zs(t) + 0.01) for t in ((t0 + 1) / 2, 0.985)]
    ctrl += [(0, ybow + 0.6, zse + 0.06), (0, ybow + 1.15, zse + 0.1)]
    ctrl += [(0, ybow + 1.4, zse + 0.36)] if curl else [(0, ybow + 1.4, zse + 0.14)]
    pts = _catmull(ctrl, per=6)

    def brad(s, Ln):
        f = s / Ln
        return 0.24 - 0.15 * f, 0.075 - 0.03 * f
    sweep(b, "timber", pts, brad, sides=10, step=0.12, up=Z)
    reach = 1.5
    if rng.random() < 0.6:
        z0 = h.Zk(0.97) + 0.12
        pts = _catmull([(0, ybow - 0.8, z0 + 0.02), (0, ybow + 0.2, z0), (0, ybow + 0.85, z0 + 0.03),
                        (0, ybow + 1.1, z0 + 0.2)], per=6)

        # SELF-REVIEW v3: full width from its first station, the fork's buried root was wider than
        # the stem it sits in and showed through the hull as a small block. It now grows from a
        # slim root inside the stem to its widest just past the bow, then tapers to the tip.
        def lrad(s, Ln):
            f = s / Ln
            g = min(1.0, f / 0.45)
            g = g * g * (3 - 2 * g)
            w = 0.045 + 0.075 * g - 0.05 * max(0.0, (f - 0.45) / 0.55)
            return w, 0.04 + 0.022 * g - 0.015 * max(0.0, (f - 0.45) / 0.55)
        sweep(b, "timber", pts, lrad, sides=8, step=0.12, up=Z)
    # THE STERN BOARD: a short projecting platform. SELF-REVIEW v5: a 1.25 m box laid on the stern
    # read as a loose plank; it is now a flat, round-edged tongue that grows out of the stern deck
    # along its sheer, narrowing to a rounded end half a metre past the stern.
    if rng.random() < 0.7:
        ys = h.point(0.0, 0.0).y
        z0 = h.Zs(0.0)
        tb0 = 0.6 / L         # the root stays on the stern deck, clear of the well
        pts = [(0, h.point(tb0, 0.0).y, h.Zs(tb0) - 0.05)]
        pts += [(0, h.point(t, 0.0).y, h.Zs(t) + 0.005) for t in (tb0 * 0.5, 0.012)]
        pts += [(0, ys - 0.25, z0 + 0.03), (0, ys - 0.5, z0 + 0.05)]

        def srad(s, Ln):
            f = s / Ln
            return 0.22 - 0.07 * f, 0.036
        sweep(b, "plank", _catmull(pts, per=5), srad, sides=10, step=0.1, up=Z)
    # GEAR on the floorboards: a bundle of bamboo poles, a rolled mat, a crate; each sinks 1.5 cm.
    zf = zfloor + 0.02 - 0.015
    yg = ys0 + Ls + 0.15
    for i, (dx, dz) in enumerate(((-0.05, 0.0), (0.02, 0.0), (-0.015, 0.05))):
        r = 0.035
        x0 = 0.38 + dx
        ya, yb = clear(yg - 0.1 + i * 0.07), clear(yg + 2.2 + i * 0.07)
        sweep(b, "bamboo", [(x0, ya, zf + r + dz), (x0 - 0.06, yb, zf + r + dz)],
              r, sides=8, nodes=0.45, phase=math.radians((0.0, 22.5, 11.25)[i]))
    if rng.random() < 0.8:
        yr = ys0 - rng.uniform(0.5, 0.8)
        sweep(b, "thatch", [(-0.45, yr, zf + 0.12), (0.45, yr + 0.05, zf + 0.12)], 0.12, sides=12, step=0.3)
    if rng.random() < 0.7:
        yc = ys0 - rng.uniform(1.2, 1.5)
        board(b, "plank", (0.1, clear(yc - 0.2), zf + 0.2), (0.1, clear(yc + 0.2), zf + 0.2), 0.5, 0.4)
    # a paddle lying along the floor on the port side
    paddle(b, Vector((-0.42, clear(ys0 + 0.2), zf + 0.018)), Vector((-0.38, clear(ys0 + 2.1), zf + 0.012)))
    b.info.update(length=L, overall_length=L + reach + 0.4, beam=2 * (h.B(0.5) + top_lap),
                  overall_width=2 * (h.B(0.5) + top_lap + 0.06 + thick + eave), draft=-h.zk,
                  freeboard=h.Zs(0.5), painted=int(painted), shelter_length=Ls, woven=int(woven))
    return h


BUILDERS = {"bangka": build_bangka, "lepa": build_lepa, "bangka_beached": build_bangka}


# ---------------------------------------------------------------- assembly

def material(slot):
    """The slot's material, created once with its flat placeholder colour. paint_trim reads the
    per-boat "trim_tint" colour attribute instead."""
    m = bpy.data.materials.get(slot)
    if m is None:
        m = bpy.data.materials.new(slot)
        m.use_nodes = True
        nt = m.node_tree
        bsdf = nt.nodes["Principled BSDF"]
        bsdf.inputs["Base Color"].default_value = SLOTS[slot] + (1,)
        bsdf.inputs["Roughness"].default_value = 0.55 if slot.startswith("paint") else 0.85
        m.diffuse_color = SLOTS[slot] + (1,)
        if slot == "paint_trim":
            ca = nt.nodes.new("ShaderNodeVertexColor")
            ca.layer_name = "trim_tint"
            nt.links.new(ca.outputs["Color"], bsdf.inputs["Base Color"])
    return m


def _append(dst, duv, pc):
    vmap = {v: dst.verts.new(v.co) for v in pc.bm.verts}
    for f in pc.bm.faces:
        try:
            nf = dst.faces.new([vmap[v] for v in f.verts])
        except ValueError:
            continue
        for a, c in zip(f.loops, nf.loops):
            c[duv].uv = a[pc.uv].uv


def _lin_to_srgb(c):
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def build_boat(kind, seed=1):
    """Build one boat of `kind` (see KINDS) from `seed` and return its Collection, NOT linked to
    any scene: every part is parented to one root empty named `kind` at the origin (midships on
    the centre line at the waterline, or at the sand for bangka_beached; bow toward +Y). The root
    carries boat_kind, boat_seed, boat_trim and the dimensions as custom properties."""
    if kind not in BUILDERS:
        raise ValueError(f"unknown boat kind {kind!r}; one of {KINDS}")
    b = Boat(kind, seed)
    BUILDERS[kind](b)
    col = bpy.data.collections.new(f"boat_{kind}_{seed}")
    root = bpy.data.objects.new(kind, None)
    root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 1.0
    col.objects.link(root)
    root["boat_kind"], root["boat_seed"], root["boat_trim"] = kind, seed, b.trim
    root["boat_hull_slot"] = b.hull_slot
    zs = [v.co.z for pc in b.floats for v in pc.bm.verts]
    if zs:
        b.info.update(float_zmin=min(zs), float_zmax=max(zs))
    for k, v in b.info.items():
        if isinstance(v, (int, float, str)):
            root[f"boat_{k}"] = v
    for slot, pcs in b.pieces.items():
        if not pcs:
            continue
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        for pc in pcs:
            _append(bm, uv, pc)
            pc.bm.free()
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
        if slot == "paint_trim":
            tint = me.color_attributes.new("trim_tint", "BYTE_COLOR", "CORNER")
            rgb = TRIM_COLOURS[b.trim]
            # A byte colour attribute stores sRGB; Blender hands it back linear to the shader.
            srgb = tuple(_lin_to_srgb(c) for c in rgb) + (1.0,)
            tint.data.foreach_set("color_srgb", srgb * len(me.loops))
        ob = bpy.data.objects.new(me.name, me)
        ob.parent = root
        col.objects.link(ob)
        bev = ob.modifiers.new("Bevel", "BEVEL")
        bev.width, bev.segments, bev.limit_method = width, 2, "ANGLE"
        bev.angle_limit = math.radians(30.0 if slot != "bamboo" else 50.0)
        bev.harden_normals = harden
        bev.use_clamp_overlap = True
    if kind == "bangka_beached":
        _beach(col, root, b)
    return col


def _beach(col, root, b):
    """Roll the bangka onto its starboard float and tip the bow up 3 degrees, then drop it so the
    lowest points sink BEACH_SINK into the sand at z = 0. The roll is solved so the keel and the
    down-side float bottom touch together: that is how a pulled-up outrigger actually rests."""
    parts = [o for o in col.objects if o.type == "MESH"]
    hull = next(o for o in parts if o.name.endswith("_" + b.hull_slot))
    bam = next(o for o in parts if o.name.endswith("_bamboo"))
    S = b.info["float_offset"]
    hull_v = [v.co.copy() for v in hull.data.vertices]
    float_v = [v.co.copy() for v in bam.data.vertices if v.co.x > S - 0.2 and v.co.z < 0.2]
    pitch = Matrix.Rotation(math.radians(3.0), 4, "X")

    def gap(a):
        M = pitch @ Matrix.Rotation(a, 4, "Y")
        return min((M @ v).z for v in float_v) - min((M @ v).z for v in hull_v)
    lo, hi = 0.0, math.radians(25)   # a positive turn about Y lowers the +X (starboard) side
    if gap(hi) > 0:
        lo = hi
    for _ in range(40):
        mid = (lo + hi) / 2
        if gap(mid) > 0:
            lo = mid
        else:
            hi = mid
    M = pitch @ Matrix.Rotation(hi, 4, "Y")
    zmin = min(min((M @ v).z for v in hull_v), min((M @ v).z for v in float_v))
    M = Matrix.Translation((0, 0, -zmin - BEACH_SINK)) @ M
    for o in parts:
        o.matrix_basis = M
    root["boat_roll_deg"] = round(math.degrees(hi), 2)
    root["boat_sink"] = BEACH_SINK


# ---------------------------------------------------------------- checks

def check_boat(col):
    """Tri counts (base and with the bevels), non-manifold edges per slot, COPLANAR overlaps
    (parallel faces of different shells closer than 4 mm over each other's interior: z-fighting),
    LOOSE shells (in no chain of intersections to the hull), and the waterline: the floats' and
    keel's extent in z and the hull's displaced volume below z = 0."""
    V, P, shell, slot_of = [], [], [], []
    hull_shells = set()
    out = {"tris": 0, "tris_bevelled": 0, "nonmanifold": {}, "shells": 0}
    sid = 0
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    root = next(o for o in col.objects if o.parent is None)
    for ob in col.objects:
        if ob.type != "MESH":
            continue
        me = ob.data
        mw = ob.matrix_basis        # in the root's frame: the beached tilt is on the parts
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
        if ob.name.endswith(("_paint_hull", "_plank")) and "hull_volume" not in out:
            if ob.name.endswith("_paint_hull") or root.get("boat_hull_slot") == "plank":
                out["hull_volume"] = round(bm.calc_volume(signed=True), 3)
                sub = bm.copy()
                bmesh.ops.bisect_plane(sub, geom=sub.verts[:] + sub.edges[:] + sub.faces[:],
                                       plane_co=(0, 0, 0), plane_no=(0, 0, 1), clear_outer=True)
                edges = [e for e in sub.edges if len(e.link_faces) == 1]
                if edges:
                    bmesh.ops.holes_fill(sub, edges=edges)
                out["displaced_m3"] = round(abs(sub.calc_volume(signed=True)), 3)
                sub.free()
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
        V.extend(mw @ v.co for v in me.vertices)
        is_hull = ob.name.endswith("_paint_hull") or (ob.name.endswith("_plank")
                                                      and root.get("boat_hull_slot") == "plank")
        for p in me.polygons:
            r = find(p.vertices[0])
            if r not in roots:
                roots[r] = sid
                sid += 1
            s = roots[r]
            P.append([base_v + i for i in p.vertices])
            shell.append(s)
            slot_of.append(ob.name)
        if is_hull:
            # the hull is the biggest shell of its object (plank also holds boards)
            counts = {}
            for p in me.polygons:
                counts[roots[find(p.vertices[0])]] = counts.get(roots[find(p.vertices[0])], 0) + 1
            hull_shells.add(max(counts, key=counts.get))
    out["shells"] = sid
    tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
    normals = []
    for p in P:
        a, c, d = V[p[0]], V[p[1]], V[p[2]]
        normals.append((c - a).cross(d - a).normalized())
    cop, where = set(), {}
    for i, p in enumerate(P):
        n = normals[i]
        if n.length < 0.5:
            continue
        c = sum((V[k] for k in p), Vector()) / len(p)
        for q in [c] + [c.lerp(V[k], 0.7) for k in p]:
            for _loc, nrm, idx, _dist in tree.find_nearest_range(q, 0.004):
                if idx is not None and shell[idx] != shell[i] and abs(nrm.dot(n)) > 0.999:
                    key = (min(shell[i], shell[idx]), max(shell[i], shell[idx]), slot_of[i], slot_of[idx])
                    cop.add(key)
                    where.setdefault(key[:2], (slot_of[i], slot_of[idx], tuple(round(x, 2) for x in c)))
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
    anchored = {f2(s) for s in hull_shells}
    loose = [s for s in range(sid) if f2(s) not in anchored]
    names = {slot_of[i] for i, s in enumerate(shell) if s in loose}
    out["loose"] = len(loose)
    out["loose_examples"] = sorted(names)[:6]
    xs = [v.x for v in V]
    ys = [v.y for v in V]
    zs = [v.z for v in V]
    out["bbox"] = (round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(min(zs), 2), round(max(zs), 2))
    if root.get("boat_float_zmin") is not None:
        out["floats_z"] = (round(root["boat_float_zmin"], 3), round(root["boat_float_zmax"], 3))
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


# (kind, seed, x, y, yaw degrees). Two rows seen from -Y, bows to the right and a little toward
# the camera: the bangkas and the beached one in front, the lepas behind. v1 to v3 stood them in one
# 40 m row seen bow-on and stern-on, too small to judge.
LINEUP = [("bangka", 1, -8.0, -4.0, -70.0), ("bangka", 2, 2.0, -4.5, -62.0), ("bangka_beached", 3, 12.5, -3.5, -75.0),
          ("lepa", 4, -6.0, 5.0, -72.0), ("lepa", 2, 7.0, 5.5, -66.0)]
SAND_TOP = 0.3


def _preview_scene():
    scene = bpy.context.scene
    water = bpy.data.meshes.new("water")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=300)
    bm.to_mesh(water)
    bm.free()
    water.materials.append(_mat("water_turquoise", (0.0, 0.40, 0.36), 0.45))
    scene.collection.objects.link(bpy.data.objects.new("water", water))
    sand = bpy.data.meshes.new("sand_bank")
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(10.0, 7.0, 1.2), verts=bm.verts)
    bmesh.ops.translate(bm, vec=(13.0, -3.8, SAND_TOP - 0.6), verts=bm.verts)
    bm.to_mesh(sand)
    bm.free()
    sand.materials.append(_mat("sand", (0.92, 0.78, 0.52)))
    scene.collection.objects.link(bpy.data.objects.new("sand_bank", sand))
    ref = bpy.data.meshes.new("scale_ref_1m60")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
    bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
    bm.to_mesh(ref)
    bm.free()
    ref.materials.append(_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
    for i, at in enumerate(((16.8, -5.6, SAND_TOP - 0.02), (-2.5, -1.0, -0.02))):
        r = bpy.data.objects.new(f"scale_ref_1m60_{i}", ref)
        r.location = at
        scene.collection.objects.link(r)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color = 5.0, (1.0, 0.9, 0.74)
    sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(50), 0, math.radians(35))   # from the camera side (-Y), upper right
    scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("world")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.42, 0.62, 0.9, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.9
    scene.world = world
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 1000
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


# (tag, camera, target, lens, checker)
SHOTS = [
    ("lineup", (2.5, -24.0, 9.0), (2.5, 0.5, 0.0), 30, False),
    ("close", (-1.0, -12.0, 4.2), (-6.5, 1.5, 0.6), 30, False),
    ("beached", (17.5, -11.0, 3.2), (12.5, -3.5, 0.5), 32, False),
    ("far", (2.5, -30.0, 1.6), (2.5, 0.5, 0.8), 35, False),
    ("checker", (2.5, -24.0, 9.0), (2.5, 0.5, 0.0), 30, True),
    ("checkerclose", (-1.0, -12.0, 4.2), (-6.5, 1.5, 0.6), 30, True),
]


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    top = bpy.data.collections.new("lagoon_boats")
    bpy.context.scene.collection.children.link(top)
    cols = []
    for kind, seed, x, y, yaw in LINEUP:
        col = build_boat(kind, seed)
        top.children.link(col)
        c = check_boat(col)
        root = next(o for o in col.objects if o.parent is None)
        info = {k[5:]: (round(v, 2) if isinstance(v, float) else v) for k, v in root.items() if k.startswith("boat_")}
        print(f"[lagoon-boats] {kind} {seed}: {info}")
        print(f"[lagoon-boats]   {c}")
        root.location = (x, y, SAND_TOP if kind == "bangka_beached" else 0.0)
        root.rotation_euler = (0, 0, math.radians(yaw))
        cols.append(col)
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "lagoon_boats.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = out.with_suffix(".blend1")
    if backup.exists():
        backup.unlink()
    print("[lagoon-boats] saved", out)
    if not version:
        return
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    cam = _preview_scene()
    scene = bpy.context.scene
    meshes = [o.data for c in cols for o in c.objects if o.type == "MESH"]
    checkers = {s: _checker_mat(s) for s in SLOTS}
    for tag, pos, tgt, lens, check in SHOTS:
        for me in meshes:
            slot = next(s for s in SLOTS if me.name.endswith("_" + s))
            me.materials[0] = checkers[slot] if check else material(slot)
        cam.location = pos
        cam.data.lens = lens
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        path = PREVIEWS / f"boatkit_{tag}_v{version}.png"
        if path.exists():
            raise SystemExit(f"[lagoon-boats] {path} exists: never overwrite a render, bump --preview")
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[lagoon-boats] preview", path)


if __name__ == "__main__":
    main()
