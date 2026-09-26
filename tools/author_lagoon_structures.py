"""Lagoon Court STRUCTURES KIT: railings, cliff boardwalks, a beach pier and a broken old pier.

Imported by tools/author_lagoon_cove.py (it has no main of its own; the test scene lives in the
scratchpad). Every builder takes the target collection first and returns the root empty of what
it built, so the lead can drop them into the cove script.

WHAT THE REFERENCE ASKS FOR (docs/LAGOON_REWORK_GUIDE.md § 1, gap review § 7a items 2, 3, 4):
the ArtStation GvJv5a village runs CONTINUOUS wooden railings along every ledge edge, boardwalk
and pier (posts about every 1.6 m, a top rail and a mid rail, never ruler straight); its houses
are joined by plank BOARDWALKS running ALONG the rock face at one level, standing on posts down
to the rock; and its beach has a plank PIER into the water with posts rising above the deck, plus
an old BROKEN pier on the sand. Ours had loose fence stubs and bare walks.

HOW IT IS BUILT, AND WHY:

  * THE SAME CONSTRUCTION AS THE HOUSE KIT. Every member is a closed box or log from
    tools/author_lagoon_houses.py (`beam`, `tube`), collected per material slot into ONE mesh per
    slot per structure with the kit's live Bevel modifier, so a railing reads as the same
    hand-made carpentry as the houses and the plank stairs, and a whole pier is three objects
    rather than three hundred.
  * CHUNKY AND A LITTLE CROOKED (Art_Direction.md § 0, the kit's own "cute and chunky, not a
    survey drawing"): 13 cm posts, a 15 cm cap rail, every post leaning a degree or so and
    standing at its own height, so the rail line wanders by a centimetre or two.
  * NOTHING FLOATS: every post and pile runs SINK (0.4 m) into the ground or rock under it, read
    from the `height_fn` the caller passes (the cove's `height`). Where the ground is lower than
    a deck the post simply gets longer.
  * NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2): rails butt at post centres and
    are narrower than the post, the cap rail sits 1.75 cm down over the post tops, bearers rise
    2 cm up inside the deck, and the X braces lie on opposite faces of their piles.
  * SHARED MATERIALS BY NAME: timber (timber_a) for posts, rails, bearers and piles; plank
    (plank_c) for stair treads, as in the cove's plank_stairs; plank_walk (plank_c_walk, the board
    texture without mid-board joints) for boardwalk decks and pier planks. World-scale UVs, 1 unit
    = 2 m, V along every member's length, boards laid ACROSS a walk as on a real footbridge.
    A pier plank is its own board: its top maps onto exactly ONE of the texture's seven boards, so
    no painted seam runs down the middle of a modelled plank.
  * STEPS ARE REAL SIZE (the cove's stair_profile rule): 0.2 m rise, 0.3 m going, flights of at
    most eight steps with flat landings between.

Coordinates: world metres, the cove's frame (court at (0, 1), lagoon mouth toward (30, -70)).
A heading is an angle in radians from +X, as math.atan2 gives it.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_lagoon_houses as HK      # noqa: E402  beam, tube, _grid_solid, _append

Z = Vector((0.0, 0.0, 1.0))
UV_METRES = 2.0
MOUTH = (30.0, -70.0)          # the lagoon mouth: every pocket's downhill side faces it

RAIL_TOP = 0.95                # top of the cap rail over the deck (the plank stairs: 0.9 + cap)
RAIL_MID = 0.5
POST_W = 0.13
POST_EVERY = 1.6               # the reference's post spacing
SINK = 0.4                     # every post and pile runs this far into the ground under it
WALK_W, WALK_T = 1.4, 0.15     # boardwalk deck: as the water village's walks (walk_path)
STEP_RISE, STEP_GOING, FLIGHT_MAX, LANDING = 0.2, 0.3, 8, 0.9

# The cove's shared material names and the texture each wears in a scene that has not textured
# it yet (the cove's chosen_textures() / walk_material() re-apply the same ones).
SLOT_TEXTURE = {"timber": "timber_a", "plank": "plank_c", "plank_walk": "plank_c_walk", "bamboo": "bamboo_a"}
# (bevel width m, bevel angle limit deg, harden normals): the house kit's finish per slot.
FINISH = {"timber": (0.016, 30.0, True), "plank": (0.012, 30.0, True), "plank_walk": (0.010, 30.0, True),
          "bamboo": (0.008, 50.0, False)}

# Everything this module has built, as 3D centreline segments (a, b, half width). The cove's
# clear_stair_paths() takes the same (a, b) pairs, and ledge railings leave a gap where one meets
# them (see path_segments(), near_structure()).
PATHS = []


# ---------------------------------------------------------------- the collector

class Kit:
    """Pieces for one structure, per material slot. Duck-types the house kit's `House` so
    HK.beam and HK.tube build straight into it."""

    def __init__(self, label, seed=0):
        self.label = label
        self.rng = random.Random(f"lagoon-structure:{label}:{seed}")
        self.pieces = {}
        self.M = None

    def add(self, slot, pc, roof=False):
        if not pc.bm.faces:
            pc.bm.free()
            return None
        self.pieces.setdefault(slot, []).append(pc)
        return pc

    def j(self, a):
        return self.rng.uniform(-a, a)

    def uv_off(self):
        return (self.rng.random(), self.rng.random())

    def finish(self, c, anchor):
        """One mesh per slot, parented to a root empty at `anchor` (so the whole structure moves
        as one), with the house kit's live bevel."""
        anchor = Vector(anchor)
        root = bpy.data.objects.new(self.label, None)
        root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 1.0
        root.location = anchor
        root["lagoon_structure"] = self.label
        c.objects.link(root)
        for slot, pcs in self.pieces.items():
            bm = bmesh.new()
            uv = bm.loops.layers.uv.new("UVMap")
            for pc in pcs:
                HK._append(bm, uv, pc)
                pc.bm.free()
            bmesh.ops.translate(bm, vec=-anchor, verts=bm.verts[:])
            bm.normal_update()
            width, sharp_deg, harden = FINISH[slot]
            lim = math.radians(sharp_deg)
            for f in bm.faces:
                f.smooth = True
            for e in bm.edges:
                e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
            me = bpy.data.meshes.new(f"{self.label} {slot}")
            bm.to_mesh(me)
            bm.free()
            me.materials.append(material(slot))
            ob = bpy.data.objects.new(me.name, me)
            ob.parent = root
            c.objects.link(ob)
            bev = ob.modifiers.new("Bevel", "BEVEL")
            bev.width, bev.segments, bev.limit_method = width, 2, "ANGLE"
            bev.angle_limit = math.radians(sharp_deg)
            bev.harden_normals = harden
            bev.use_clamp_overlap = True
        self.pieces = {}
        return root


def material(slot):
    """The cove's shared material of that name; textured here only if nothing has made it yet."""
    m = bpy.data.materials.get(slot)
    if m is None:
        import render_lagoon_texture_preview as T
        m = bpy.data.materials.new(slot)
        T.uv_material(m, SLOT_TEXTURE[slot])
    return m


def board(h, p0, p1, w, t, up=Z, slot="plank_walk"):
    """ONE plank from p0 to p1 (its length), `w` wide, `t` thick. plank_c_walk draws seven boards
    across a 2 m tile; a modelled plank maps onto exactly one of them (a random one), so its top
    shows one board with its own edges and never a painted seam down its middle. V runs along the
    plank inside 0.05..0.75 of the tile, clear of the texture's one butt joint at the tile edge
    (the rule walk_path follows); a plank longer than 1.4 m is gently stretched along its grain."""
    p0, p1 = Vector(p0), Vector(p1)
    L = (p1 - p0).length
    d = (p1 - p0) / L
    side = up.cross(d)
    if side.length < 1e-6:
        side = Vector((1, 0, 0)).cross(d)
    side.normalize()
    upv = d.cross(side)

    def P(q, i, k):
        return q + side * (i * w / 2) + upv * (k * t / 2)
    top = [[P(p0, -1, 1), P(p0, 1, 1)], [P(p1, -1, 1), P(p1, 1, 1)]]
    bot = [[P(p0, -1, -1), P(p0, 1, -1)], [P(p1, -1, -1), P(p1, 1, -1)]]
    pc = HK._grid_solid(top, bot)
    k = h.rng.randrange(7)
    u0, su = k / 7 + 0.012, (1 / 7 - 0.024) / max(w, 0.05)
    vs = min(1.0 / UV_METRES, 0.68 / L)
    v0 = 0.05 + h.rng.uniform(0.0, max(0.0, 0.70 - L * vs))
    pc.bm.normal_update()
    for f in pc.bm.faces:
        n = f.normal
        a_up, a_side, a_d = abs(n.dot(upv)), abs(n.dot(side)), abs(n.dot(d))
        for loop in f.loops:
            q = loop.vert.co - p0
            along, across, thick = q.dot(d), q.dot(side) + w / 2, q.dot(upv) + t / 2
            if a_d >= a_up and a_d >= a_side:           # the end grain
                uvv = (u0 + across * su, v0 + thick * vs)
            elif a_side > a_up:                          # the long edges
                uvv = (u0 + thick * su, v0 + along * vs)
            else:                                        # top and underside
                uvv = (u0 + across * su, v0 + along * vs)
            loop[pc.uv].uv = uvv
    return h.add(slot, pc)


# ---------------------------------------------------------------- polyline helpers

def _v2(p):
    return Vector((p[0], p[1], 0.0))


def _offset_polyline(pts, off):
    """The polyline moved `off` metres to its LEFT (+normal), mitred at every bend so the
    distance to the line holds across the corner (walk_path's mitre)."""
    P = [_v2(p) for p in pts]
    n = len(P)
    out = []
    for i in range(n):
        a = (P[i] - P[i - 1]) if i > 0 else (P[1] - P[0])
        b = (P[i + 1] - P[i]) if i < n - 1 else (P[i] - P[i - 1])
        a, b = a.normalized(), b.normalized()
        na, nb = Vector((-a.y, a.x, 0)), Vector((-b.y, b.x, 0))
        m = (na + nb).normalized() if (na + nb).length > 1e-6 else na
        out.append(P[i] + m * off / max(0.35, m.dot(na)))
    return out


def _arc(P):
    s = [0.0]
    for i in range(1, len(P)):
        s.append(s[-1] + (P[i] - P[i - 1]).length)
    return s


def _at(P, S, s):
    """Point and unit tangent at arc length s along polyline P (arc table S)."""
    s = max(0.0, min(S[-1], s))
    for i in range(len(P) - 1):
        if s <= S[i + 1] or i == len(P) - 2:
            seg = S[i + 1] - S[i]
            t = 0.0 if seg < 1e-9 else (s - S[i]) / seg
            return P[i].lerp(P[i + 1], t), (P[i + 1] - P[i]).normalized(), i, t
    return P[-1], (P[-1] - P[-2]).normalized(), len(P) - 2, 1.0


def _seg_dist(p, a, b):
    ax, ay, bx, by = a[0], a[1], b[0], b[1]
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((p[0] - ax) * dx + (p[1] - ay) * dy) / max(dx * dx + dy * dy, 1e-9)))
    return math.hypot(p[0] - ax - t * dx, p[1] - ay - t * dy)


def _blocked(p, avoid):
    for item in avoid or ():
        if callable(item):
            if item(p[0], p[1]):
                return True
        else:
            a, b, clear = item
            if _seg_dist(p, a, b) < clear:
                return True
    return False


def _stations(P, spacing, rng, corner_deg=12.0, jitter=0.12):
    """Arc lengths for posts along P: both ends, every bend sharper than corner_deg, and even
    splits of at most `spacing` between them, each nudged a little so the rhythm is hand set."""
    S = _arc(P)
    keys = [0.0]
    for i in range(1, len(P) - 1):
        a, b = (P[i] - P[i - 1]).normalized(), (P[i + 1] - P[i]).normalized()
        if math.degrees(a.angle(b, 0.0)) > corner_deg:
            keys.append(S[i])
    keys.append(S[-1])
    out = []
    for s0, s1 in zip(keys, keys[1:]):
        if s1 - s0 < 0.3:
            continue
        n = max(1, math.ceil((s1 - s0) / spacing - 0.05))
        for k in range(n):
            s = s0 + (s1 - s0) * k / n
            out.append(s + (rng.uniform(-jitter, jitter) if k else 0.0))
    out.append(S[-1])
    return out, S


# ---------------------------------------------------------------- railings

def _post(h, base, foot_z, top_z, facing, slot="timber", w=POST_W):
    """A post from foot_z (in the ground) to top_z, leaning a degree or so, turned to its run."""
    lean = Vector((h.j(0.022), h.j(0.022), 0.0))
    foot = Vector((base.x, base.y, foot_z)) - lean * 0.5
    top = Vector((base.x, base.y, top_z)) + lean
    HK.beam(h, slot, foot, top, w, w, up=facing)
    return top


def _rails(h, tops, deck_zs, slot="timber"):
    """A cap rail over the post tops and a mid rail between them, one piece per bay. Each piece
    butts its neighbour at the post centre: the cap is wider than the post and sits 1.75 cm down
    over its top, the mid rail is narrower than the post, so no joint is ever two faces in one
    plane. Per-post heights make the line wander a little, as a hand-built rail does."""
    for (a, za), (b, zb) in zip(zip(tops, deck_zs), zip(tops[1:], deck_zs[1:])):
        if (b - a).length < 0.05:
            continue
        HK.beam(h, slot, a + Z * 0.02, b + Z * 0.02, 0.15, 0.075)
        ma = Vector((a.x, a.y, za + RAIL_MID + h.j(0.015)))
        mb = Vector((b.x, b.y, zb + RAIL_MID + h.j(0.015)))
        HK.beam(h, slot, ma, mb, 0.075, 0.11)


def _foot(x, y, deck_z, height_fn, bolt=0.25):
    """Where a post ends: in the ground (SINK under it) when there is ground, else bolted to the
    deck's edge `bolt` under the deck top."""
    if height_fn is None:
        return deck_z - bolt
    return min(deck_z - bolt, height_fn(x, y) - SINK)


def _railing_into(h, points, zs, height_fn=None, spacing=POST_EVERY, avoid=(), min_run=0.9, slot="timber",
                  bolt=0.25):
    """Posts and rails along one polyline into kit `h`; returns the post tops. `zs` is a deck
    height per point (or one number); the line splits into runs wherever `avoid` blocks it."""
    P = [_v2(p) for p in points]
    if len(P) < 2:
        return []
    Zs = [float(zs)] * len(P) if isinstance(zs, (int, float)) else [float(z) for z in zs]
    S = _arc(P)

    def deck_z(s):
        _q, _t, i, t = _at(P, S, s)
        return Zs[i] + (Zs[i + 1] - Zs[i]) * t
    # Runs: sample every 0.2 m and cut out whatever `avoid` blocks.
    runs, cur = [], None
    n = max(2, int(S[-1] / 0.2) + 1)
    for k in range(n + 1):
        s = S[-1] * k / n
        q = _at(P, S, s)[0]
        if _blocked(q, avoid):
            if cur is not None:
                runs.append(cur)
                cur = None
        else:
            cur = [cur[0], s] if cur is not None else [s, s]
    if cur is not None:
        runs.append(cur)
    tops_all = []
    for s0, s1 in runs:
        if s1 - s0 < min_run:
            continue
        # Stations inside the run: the run's ends and the polyline's bends between them.
        sub = [s0] + [s for s in S[1:-1] if s0 + 0.3 < s < s1 - 0.3] + [s1]
        Q = [_at(P, S, s)[0] for s in sub]
        st, SQ = _stations(Q, spacing, h.rng)
        tops, dzs = [], []
        for sq in st:
            q, tan, _i, _t = _at(Q, SQ, sq)
            dz = deck_z(s0 + sq)            # Q keeps the line's own bends, so its arc is the line's
            top_z = dz + RAIL_TOP - 0.02 + h.j(0.02)
            tops.append(_post(h, q, _foot(q.x, q.y, dz, height_fn, bolt), top_z, tan, slot))
            dzs.append(dz)
        _rails(h, tops, dzs, slot)
        tops_all += tops
    return tops_all


def railing(c, points, zs, height_fn=None, sides=None, offset=WALK_W / 2 + 0.02, spacing=POST_EVERY, avoid=(),
            label="railing", seed=0, slot="timber"):
    """A railing following a polyline: posts every ~1.6 m (at every bend too), a cap rail and a
    mid rail.

    points     [(x, y), ...] the line (a walk's centreline when `sides` is given)
    zs         the deck height under the rail: one number, or one per point
    height_fn  f(x, y) -> ground z. Given, every post runs SINK into the ground under it (so a
               railing on a ledge or on legs never floats); None, posts bolt to the deck edge.
    sides      None: the rail stands ON the line. ("left",), ("right",) or ("left", "right"):
               the rail stands `offset` to that side of the line (left = +90 degrees from travel).
    avoid      things the rail must leave open: (a_xy, b_xy, clearance) segments and/or
               callables f(x, y) -> True where blocked. A blocked stretch becomes a gap.
    Returns the root empty (one timber mesh under it)."""
    h = Kit(label, seed)
    lines = [points] if not sides else []
    for sd in sides or ():
        lines.append([(q.x, q.y) for q in _offset_polyline(points, offset if sd == "left" else -offset)])
    for line in lines:
        _railing_into(h, line, zs, height_fn, spacing, avoid, slot=slot)
    p0 = points[0]
    z0 = zs if isinstance(zs, (int, float)) else zs[0]
    return h.finish(c, (p0[0], p0[1], z0))


# ---------------------------------------------------------------- ledges

def _cove(cove=None):
    """The cove module's layout data. When the cove script is the running __main__ its own
    globals are used (importing it again would build a second, separate copy)."""
    if cove is not None:
        return cove
    main = sys.modules.get("__main__")
    if main is not None and hasattr(main, "POCKETS") and hasattr(main, "STAIRS"):
        return main
    import author_lagoon_cove as C
    return C


def _pocket(p, cove=None):
    if isinstance(p, str):
        return next(q for q in _cove(cove).POCKETS if q[0] == p)
    return p


def stair_segments(cove=None, clear=None):
    """Each cove stair's run (ea, eb in plan) with its clearance, computed exactly as village()
    places plank_stairs: from 0.85 of one pocket's radius to 0.85 of the other's."""
    C = _cove(cove)
    lookup = {p[0]: p for p in C.POCKETS}
    clear = clear if clear is not None else getattr(C, "STAIR_W", 1.5) / 2 + 0.55
    out = []
    for a, b in C.STAIRS:
        A, Bp = lookup[a], lookup[b]
        d = Vector((Bp[1] - A[1], Bp[2] - A[2], 0)).normalized()
        ea = (A[1] + d.x * A[4] * 0.85, A[2] + d.y * A[5] * 0.85)
        eb = (Bp[1] - d.x * Bp[4] * 0.85, Bp[2] - d.y * Bp[5] * 0.85)
        # Run it on past both ends so the rail stays open where the flight meets the rim.
        e = Vector((eb[0] - ea[0], eb[1] - ea[1], 0)).normalized() * 1.5
        out.append(((ea[0] - e.x, ea[1] - e.y), (eb[0] + e.x, eb[1] + e.y), clear))
    return out


def object_keepout(objects, margin=0.45, z_max=None, skip=("thatch", "tin", "cloth")):
    """A blocker for `avoid` from placed objects (a house's root empty and its children): True
    within `margin` of any of their EDGES in plan, counting only geometry below `z_max` (the
    floor + 1.3 m for a ledge rail). Review v1: whole-object bounding boxes blocked nearly every
    ledge rail, because one house's plank mesh spans its floor, deck and front steps; sampled
    edges let the rail pass a metre in front of a deck and open only where the steps come down.
    Roof slots are skipped: a rail may pass under an eave."""
    cell = 0.25
    hit = set()
    bpy.context.view_layer.update()
    todo = list(objects)
    while todo:
        o = todo.pop()
        todo += list(o.children)
        if o.type != "MESH" or any(sk in o.name for sk in skip):
            continue
        mw = o.matrix_world
        co = [mw @ v.co for v in o.data.vertices]
        for e in o.data.edges:
            a, b = co[e.vertices[0]], co[e.vertices[1]]
            n = max(1, int((a - b).length / 0.2))
            for k in range(n + 1):
                q = a.lerp(b, k / n)
                if z_max is None or q.z < z_max:
                    hit.add((math.floor(q.x / cell), math.floor(q.y / cell)))
    r = int(math.ceil(margin / cell))

    def blocked(x, y):
        cx, cy = math.floor(x / cell), math.floor(y / cell)
        for dx in range(-r, r + 1):
            for dy in range(-r, r + 1):
                if (cx + dx, cy + dy) in hit and math.hypot(dx, dy) * cell <= margin + cell * 0.5:
                    return True
        return False
    return blocked


def ledge_railing(c, pocket, height_fn, open_arc=None, avoid=None, houses=(), radius=1.0, drop=0.6, arc_dot=-0.1,
                  label=None, seed=0, cove=None):
    """A railing round a pocket's DOWNHILL arc: the side facing the lagoon mouth (30, -70), the
    side the stub fence posts and rim stones already mark, and only where the ground really
    drops (at 1.4 x the radius it is at least `drop` under the floor). It stands on the ledge's
    very edge, `radius` 1.0 of the pocket's ellipse, where the floor starts to fall (review v2:
    at 0.94, the stub posts' radius, it stood a hand's width in front of every house deck), and
    its posts run into the ground and the rim stones.

    pocket     a POCKETS row or its name
    open_arc   None: leave a gap wherever a cove stair (STAIRS) or a structure from this module
               (a cliff walk, a pier) meets the rim. Or a list of (angle, half_width) in radians,
               measured like the rim stones (the ellipse parameter), to open instead of that.
    avoid      extra blockers, as railing(): segments and callables.
    houses     placed house roots on this pocket: the rail keeps clear of their decks and front
               steps (object_keepout below the floor + 1.3 m), opening where the steps come down.
    Returns the root empty, or None for the court or a pocket with no drop."""
    name, px, py, pz, rx, ry, _s = _pocket(pocket, cove)
    if name == "court":
        return None
    to_mouth = Vector((MOUTH[0] - px, MOUTH[1] - py, 0)).normalized()
    blockers = list(avoid or ())
    if houses:
        blockers.append(object_keepout(houses, z_max=pz + 1.3))
    if open_arc is None:
        blockers += stair_segments(cove)
        blockers += [((a.x, a.y), (b.x, b.y), hw + 0.5) for a, b, hw in PATHS]
    else:
        for a0, hw in open_arc:
            blockers.append(_arc_blocker(px, py, rx, ry, a0, hw))
    # Keep the arc where it faces the mouth and there is a drop, as runs of angles.
    n = 360
    keep = []
    for k in range(n):
        a = k / n * math.tau
        u = Vector((math.cos(a), math.sin(a), 0))
        ok = u.dot(to_mouth) > arc_dot
        if ok:
            ox, oy = px + u.x * rx * 1.4, py + u.y * ry * 1.4
            ok = height_fn(ox, oy) < pz - drop
        keep.append(ok)
    if not any(keep):
        return None
    start = keep.index(False) if not all(keep) else 0
    runs, cur = [], []
    for k in range(n + 1):
        kk = (start + k) % n
        if keep[kk] and k < n:
            cur.append(kk / n * math.tau)
        elif cur:
            runs.append(cur)
            cur = []
    h = Kit(label or f"ledge railing {name}", seed)
    for run in runs:
        pts = [(px + math.cos(a) * rx * radius, py + math.sin(a) * ry * radius) for a in run[::4] + [run[-1]]]
        _railing_into(h, pts, pz, height_fn, avoid=blockers)
    if not h.pieces:
        return None
    return h.finish(c, (px, py, pz))


def _arc_blocker(px, py, rx, ry, a0, hw):
    def f(x, y):
        a = math.atan2((y - py) / ry, (x - px) / rx)
        return abs((a - a0 + math.pi) % math.tau - math.pi) < hw
    return f


# ---------------------------------------------------------------- steps

def _flight(h, top, fwd, z_top, z_bot, width, height_fn, rails=("left", "right"), ground_stop=True):
    """Real-size steps from `top` (plan point at the deck edge, deck at z_top) along `fwd` down
    to z_bot: timber stringers each side, a plank_c tread per step (boards across the flight, as
    the cove's plank_stairs), flights of at most eight steps with flat LANDING-long landings, and
    a railing each side in `rails`. With `ground_stop` the flight ends early where the ground
    rises to meet a tread. Returns the plan point and height where it lands."""
    top, fwd = _v2(top), _v2(fwd).normalized()
    side = Vector((-fwd.y, fwd.x, 0))
    n = max(1, round((z_top - z_bot) / STEP_RISE))
    dz = (z_top - z_bot) / n
    s, z, k = 0.0, z_top, 0
    profile = [(0.0, z_top)]          # (s, deck z) at every change of slope, for rails and posts
    while k < n:
        batch = min(FLIGHT_MAX, n - k)
        for i in range(batch):
            zt = z - dz * (i + 1)
            c = top + fwd * (s + (i + 0.5) * STEP_GOING)
            if ground_stop and height_fn is not None and height_fn(c.x, c.y) > zt - 0.04:
                batch = i
                n = k + i
                break
            # Treads run 2 cm INTO the stringers (whose inner faces are at width/2 + 0.03): a tread
            # ending exactly on that face would put its end grain in the stringer's plane.
            HK.beam(h, "plank", Vector((c.x, c.y, zt - 0.04)) - side * (width / 2 + 0.05),
                    Vector((c.x, c.y, zt - 0.04)) + side * (width / 2 + 0.05), STEP_GOING + 0.04, 0.08)
        s += batch * STEP_GOING
        z -= batch * dz
        k += batch
        profile.append((s, z))
        if k < n:
            for q in range(3):
                c = top + fwd * (s + LANDING * (q + 0.5) / 3)
                HK.beam(h, "plank", Vector((c.x, c.y, z - 0.04)) - side * (width / 2 + 0.05),
                        Vector((c.x, c.y, z - 0.04)) + side * (width / 2 + 0.05), LANDING / 3 - 0.015, 0.08)
            s += LANDING
            profile.append((s, z))
    # Stringers under each slope, just outside the treads, and posts at every change of slope
    # that carry the rails and run into the ground.
    for (s0, z0), (s1, z1) in zip(profile, profile[1:]):
        for sg in (-1, 1):
            a = top + fwd * s0 + side * sg * (width / 2 + 0.07)
            b = top + fwd * s1 + side * sg * (width / 2 + 0.07)
            HK.beam(h, "timber", Vector((a.x, a.y, z0 - 0.13)), Vector((b.x, b.y, z1 - 0.13)), 0.08, 0.26)
    for sd in rails:
        sg = 1 if sd == "left" else -1
        pts, zs = [], []
        for s_, z_ in profile:
            q = top + fwd * s_ + side * sg * (width / 2 + 0.07)
            pts.append((q.x, q.y))
            zs.append(z_)
        _railing_into(h, pts, zs, height_fn, spacing=POST_EVERY, min_run=0.25)
    end = top + fwd * s
    return end, z


# ---------------------------------------------------------------- cliff boardwalks

def _deck_strip(h, P, width=WALK_W, thick=WALK_T):
    """A walk deck along 3D points P as ONE mitred strip (walk_path's construction): UVs per
    straight run, U along it and V across it inside 0.05..0.75, so boards lie across the walk and
    meet at a clean mitre at each bend."""
    n = len(P)
    sides = _offset_polyline([(p.x, p.y) for p in P], 1.0)
    sides = [sides[i] - _v2(P[i]) for i in range(n)]
    pc = HK.Piece()
    bm, uvl = pc.bm, pc.uv
    rows = []
    for i in range(n):
        top = [bm.verts.new(P[i] + sides[i] * (k * width / 2)) for k in (-1, 1)]
        bot = [bm.verts.new(P[i] + sides[i] * (k * width / 2) - Z * thick) for k in (-1, 1)]
        rows.append((top, bot))
    dist = _arc([_v2(p) for p in P])
    faces = []
    for i in range(n - 1):
        (t0, b0), (t1, b1) = rows[i], rows[i + 1]
        faces.append((bm.faces.new((t0[0], t1[0], t1[1], t0[1])), i, "top"))
        faces.append((bm.faces.new((b0[1], b1[1], b1[0], b0[0])), i, "top"))
        faces.append((bm.faces.new((b0[0], b1[0], t1[0], t0[0])), i, "side"))
        faces.append((bm.faces.new((t0[1], t1[1], b1[1], b0[1])), i, "side"))
    faces.append((bm.faces.new((rows[0][0][1], rows[0][0][0], rows[0][1][0], rows[0][1][1])), 0, "cap"))
    faces.append((bm.faces.new((rows[-1][0][0], rows[-1][0][1], rows[-1][1][1], rows[-1][1][0])), n - 2, "cap"))
    ov = h.rng.random()
    vs = min(1.0, 1.4 / width) / UV_METRES
    for f, i, kind in faces:
        d = _v2(P[i + 1] - P[i]).normalized()
        nrm = Vector((-d.y, d.x, 0))
        for loop in f.loops:
            rel = loop.vert.co - P[i]
            along = dist[i] + rel.dot(d)
            across = rel.dot(nrm) + width / 2
            height_ = loop.vert.co.z - (P[i].z - thick)
            if kind == "side":
                uvv = (along / UV_METRES + ov, 0.05 + height_ * vs)
            elif kind == "cap":
                uvv = (height_ / UV_METRES + ov, 0.05 + across * vs)
            else:
                uvv = (along / UV_METRES + ov, 0.05 + across * vs)
            loop[uvl].uv = uvv
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    h.add("plank_walk", pc)


def cliff_walk(c, pts, z, height_fn, start_z=None, end_z=None, rail="outer", label="cliff walk", seed=0):
    """A LEVEL plank boardwalk along a polyline at deck height z, the way the reference joins its
    houses along the rock face: a continuous mitred deck (plank_walk), a timber bearer across it
    every ~1.6 m, and at each bearer a leg down each side into the ground wherever the ground is
    below the deck (the outer legs run on up as the railing posts, so a leg is never a separate
    stick beside a post). Tall legs get a diagonal brace across the walk and between bents.

    pts        [(x, y), ...] the centreline
    z          the deck top
    start_z, end_z   floor heights at the two ends: where one differs from z by more than 0.15 m
               the end becomes a real-size flight of steps (the last metres of the line), so a
               walk can land on a pocket a metre or two lower or higher.
    rail       "outer": a railing on the side where the ground falls away (per stretch, only where
               there is a drop); "both"; or None.
    Returns the root empty."""
    h = Kit(label, seed)
    P2 = [_v2(p) for p in pts]
    S = _arc(P2)
    s_a, s_b = 0.0, S[-1]
    flights = []
    for which, zz in (("start", start_z), ("end", end_z)):
        if zz is None or abs(zz - z) <= 0.15:
            continue
        n = round(abs(z - zz) / STEP_RISE)
        length = n * STEP_GOING + (math.ceil(n / FLIGHT_MAX) - 1) * LANDING
        if which == "end":
            s_b = max(s_a + 2.0, S[-1] - length)
            q, t, _i, _t = _at(P2, S, s_b)
            flights.append((q, t, zz))
        else:
            s_a = min(s_b - 2.0, length)
            q, t, _i, _t = _at(P2, S, s_a)
            flights.append((q, -t, zz))
    # The level deck between the flights.
    keep = [s_a] + [s for s in S if s_a + 0.2 < s < s_b - 0.2] + [s_b]
    D2 = [_at(P2, S, s)[0] for s in keep]
    D3 = [Vector((q.x, q.y, z)) for q in D2]
    _deck_strip(h, D3)
    # Which side is OUTER: the side whose ground is lower, summed over the whole walk.
    SD0 = _arc(D2)
    lower = 0.0
    for k in range(21):
        q, t, _i, _t = _at(D2, SD0, SD0[-1] * k / 20)
        nl = Vector((-t.y, t.x, 0)) * 2.5
        lower += height_fn(q.x + nl.x, q.y + nl.y) - height_fn(q.x - nl.x, q.y - nl.y)
    outer = 1 if lower < 0 else -1
    st, SD = _stations(D2, POST_EVERY, h.rng)
    edges = _offset_polyline([(q.x, q.y) for q in D2], 1.0)
    bear_top = z - WALK_T + 0.02
    bear_bot = bear_top - 0.16
    rail_sides = {"outer": (outer,), "both": (-1, 1)}.get(rail, ())
    rail_runs, cur = [], {-1: [], 1: []}
    prev_legs, nbent = None, 0
    for k, s in enumerate(st):
        q, tan, i, t = _at(D2, SD, s)
        m = (edges[i].lerp(edges[i + 1], t) - q)       # the mitred unit normal (scaled at bends)
        if m.length < 1e-6:
            m = Vector((-tan.y, tan.x, 0))
        # A BENT (bearer and legs) at every other station and both ends; the stations between
        # carry only a rail post hanging from the deck edge. Review v1: a pair of legs every
        # 1.6 m read as a forest of sticks under the walk.
        bent = k % 2 == 0 or k == len(st) - 1
        if bent:
            # A bearer across, 2 cm up inside the deck, out past both legs.
            a = q - m * (WALK_W / 2 + 0.16)
            b = q + m * (WALK_W / 2 + 0.16)
            HK.beam(h, "timber", Vector((a.x, a.y, bear_top - 0.08)), Vector((b.x, b.y, bear_top - 0.08)), 0.12, 0.16)
        legs = {}
        for sg in (-1, 1):
            e = q + m * sg * (WALK_W / 2 + 0.05)
            g = height_fn(e.x, e.y)
            beyond = q + m * sg * (WALK_W / 2 + 0.8)
            drop = z - min(g, height_fn(beyond.x, beyond.y))
            railed = sg in rail_sides and drop > 0.5      # a rail only where the ground falls away
            if railed:
                # At a bent the rail post IS the leg, down into the ground; between bents it
                # hangs from the deck edge.
                cur[sg].append((e, g - SINK if bent else z - 0.32))
            elif cur[sg]:
                rail_runs.append(cur[sg])
                cur[sg] = []
            if bent and g < bear_bot - 0.05:
                if not railed:
                    lean = Vector((h.j(0.02), h.j(0.02), 0))
                    HK.beam(h, "timber", Vector((e.x, e.y, g - SINK)) - lean, Vector((e.x, e.y, bear_top - 0.02)),
                            POST_W, POST_W, up=tan)
                legs[sg] = (e, g)
        if not bent:
            continue
        nbent += 1
        # Tall bents: a diagonal brace across the walk, on the bent's far face.
        if len(legs) == 2 and max(bear_bot - legs[-1][1], bear_bot - legs[1][1]) > 1.4:
            (e0, g0), (e1, g1) = legs[-1], legs[1]
            hi, lo = (e0, e1) if nbent % 2 else (e1, e0)
            glo = g1 if lo is e1 else g0
            off = tan * 0.1
            HK.beam(h, "timber", Vector((hi.x, hi.y, bear_bot - 0.1)) + off,
                    Vector((lo.x, lo.y, max(glo + 0.25, bear_bot - 2.4))) + off, 0.16, 0.05, up=tan)
        # ...and along the walk between tall outer legs of neighbouring bents, on their outer face.
        if prev_legs and outer in legs and outer in prev_legs:
            (ea, ga), (eb, gb) = prev_legs[outer], legs[outer]
            if min(bear_bot - ga, bear_bot - gb) > 1.8 and nbent % 2 == 0:
                o = m * outer * 0.1
                HK.beam(h, "timber", Vector((ea.x, ea.y, bear_bot - 0.12)) + o,
                        Vector((eb.x, eb.y, max(gb + 0.3, bear_bot - 1.6))) + o, 0.16, 0.05, up=m * outer)
        prev_legs = legs
    rail_runs += [r for r in cur.values() if r]
    # The railing: posts at the outer edge from the ground (or the deck's edge) up, rails between.
    for run in rail_runs:
        if len(run) < 2:
            continue
        tops, dzs = [], []
        for idx, (e, foot) in enumerate(run):
            nxt = run[min(idx + 1, len(run) - 1)][0] - run[max(idx - 1, 0)][0]
            tops.append(_post(h, e, min(z - 0.3, foot), z + RAIL_TOP - 0.02 + h.j(0.02),
                              nxt.normalized() if nxt.length > 1e-6 else Vector((1, 0, 0))))
            dzs.append(z)
        _rails(h, tops, dzs)
    for q, fwd, zz in flights:
        if zz < z:
            _flight(h, q, fwd, z, zz, WALK_W - 0.1, height_fn)
        else:
            # Up to a higher floor: build the flight from the top down, back toward the deck.
            q2 = q + fwd * (round((zz - z) / STEP_RISE) * STEP_GOING
                            + (math.ceil(round((zz - z) / STEP_RISE) / FLIGHT_MAX) - 1) * LANDING)
            _flight(h, q2, -fwd, zz, z, WALK_W - 0.1, height_fn, ground_stop=False)
    for i in range(len(D3) - 1):
        PATHS.append((D3[i].copy(), D3[i + 1].copy(), WALK_W / 2))
    for q, fwd, zz in flights:
        PATHS.append((Vector((q.x, q.y, z)), Vector((q.x, q.y, zz)) + fwd * 3.0, WALK_W / 2))
    return h.finish(c, D3[0])


# ---------------------------------------------------------------- piers

def pier(c, start_xy, heading, length, width=2.2, deck_z=-0.4, seabed_z=-3.5, ground_fn=None, rail_side="left",
         label="beach pier", seed=0):
    """A plank pier from the sand into the water.

    start_xy   the landward end of the deck (on the sand)
    heading    radians from +X, the direction it runs out to sea
    deck_z     the deck top (the cove's water is -1.8, so -0.4 stands 1.4 m clear)
    seabed_z   where piles end when no ground_fn is given
    ground_fn  f(x, y) -> ground or seabed z (the cove's `height`), so every pile ends SINK in
               the sand or seabed actually under it
    rail_side  "left", "right" or None: a railing along that edge, open over the last bay so a
               boat can tie up at the end

    Construction: a bent of two round timber piles every ~2.5 m under a cap beam; three joists
    along the pier (the edge joists pass THROUGH the piles, bolted like a wale); plank_c_walk
    planks across it, each its own board with a 2 cm gap. Piles at the start, the end and every
    other bent rise 0.6 m over the deck as BOLLARDS (the reference); on the railed edge they rise
    on as its posts. X bracing between the two piles of a tall bent and diagonals along the
    sides; a ladder down into the water at the end; steps down to the sand at the start.
    Returns the root empty."""
    h = Kit(label, seed)
    d = Vector((math.cos(heading), math.sin(heading), 0))
    sd = Vector((-d.y, d.x, 0))
    O = _v2(start_xy)

    def gz(p):
        return ground_fn(p.x, p.y) if ground_fn is not None else seabed_z

    def at(s, u):
        return O + d * s + sd * u
    pile_u = width / 2 - 0.1
    joist_top = deck_z - 0.07 + 0.02          # 2 cm up inside the planks
    joist_bot = joist_top - 0.2
    cap_top = joist_bot + 0.03
    cap_bot = cap_top - 0.22
    nb = max(2, round((length - 0.6) / 2.5))
    bents = [0.3 + (length - 0.6) * i / nb for i in range(nb + 1)]
    rail_sg = {"left": 1, "right": -1}.get(rail_side)
    rail_end = bents[-2] if len(bents) > 2 else bents[-1]
    rail_tops = []
    pile_feet = []
    for bi, s in enumerate(bents):
        feet = {}
        for sg in (-1, 1):
            p = at(s, sg * pile_u)
            g = gz(p)
            railed = rail_sg == sg and s <= rail_end + 0.01
            bollard = bi in (0, len(bents) - 1) or bi % 2 == 0
            top_z = deck_z + RAIL_TOP - 0.02 + h.j(0.02) if railed else (
                deck_z + 0.6 + h.j(0.05) if bollard else cap_top + 0.05)
            lean = Vector((h.j(0.03), h.j(0.03), 0))
            foot = Vector((p.x, p.y, g - SINK - 0.2)) - lean
            topv = Vector((p.x, p.y, top_z)) + lean
            HK.tube(h, "timber", foot, topv, 0.12 * h.rng.uniform(0.92, 1.08), sides=9, taper=0.9)
            if railed:
                rail_tops.append((s, topv))
            feet[sg] = (p, g)
        pile_feet.append(feet)
        # The cap beam across the bent, out past both piles.
        a, b = at(s, -pile_u - 0.3), at(s, pile_u + 0.3)
        HK.beam(h, "timber", Vector((a.x, a.y, (cap_top + cap_bot) / 2)), Vector((b.x, b.y, (cap_top + cap_bot) / 2)),
                0.2, cap_top - cap_bot, up=Z)
        # X bracing between the two piles, one diagonal on each face of the bent.
        (p0, g0), (p1, g1) = feet[-1], feet[1]
        low = max(max(g0, g1) + 0.25, cap_bot - 2.6)
        if cap_bot - 0.1 - low > 0.7:
            for face, (pa, pb) in ((-0.15, (p0, p1)), (0.15, (p1, p0))):
                o = d * face
                HK.beam(h, "timber", Vector((pa.x, pa.y, cap_bot - 0.08)) + o, Vector((pb.x, pb.y, low)) + o,
                        0.16, 0.05, up=d)
    # Diagonals along both sides, on the piles' outer faces, alternate bays.
    for bi in range(len(bents) - 1):
        if bi % 2:
            continue
        for sg in (-1, 1):
            (pa, ga), (pb, gb) = pile_feet[bi][sg], pile_feet[bi + 1][sg]
            low = max(max(ga, gb) + 0.3, cap_bot - 2.2)
            if cap_bot - low > 0.7:
                o = sd * sg * 0.15
                HK.beam(h, "timber", Vector((pa.x, pa.y, cap_bot - 0.1)) + o, Vector((pb.x, pb.y, low)) + o,
                        0.16, 0.05, up=sd * sg)
    # Joists: both edge joists run through the piles, the middle one under the plank centres.
    for u, w in ((-pile_u, 0.12), (0.0, 0.14), (pile_u, 0.12)):
        a, b = at(0.0, u), at(length, u)
        zc = (joist_top + joist_bot) / 2
        HK.beam(h, "timber", Vector((a.x, a.y, zc)), Vector((b.x, b.y, zc)), w, joist_top - joist_bot, up=Z)
    # Planks across, each its own board, 2 cm apart, ends a little uneven.
    s = 0.02
    while s < length - 0.1:
        w = min(h.rng.uniform(0.24, 0.3), length - s)
        c_ = at(s + w / 2, 0.0)
        zc = deck_z - 0.035 + h.j(0.006)
        tilt = (Z + d * h.j(0.012)).normalized()
        a = c_ - sd * (width / 2 + 0.04 + h.j(0.04))
        b = c_ + sd * (width / 2 + 0.04 + h.j(0.04))
        board(h, Vector((a.x, a.y, zc)), Vector((b.x, b.y, zc)), w - 0.02, 0.07, up=tilt)
        s += w
    # The railing along one edge: the railed piles are its main posts, with a square post
    # between each pair, bolted to the edge joist.
    if rail_sg is not None and len(rail_tops) >= 2:
        tops = []
        for (sa, ta), (sb, tb) in zip(rail_tops, rail_tops[1:]):
            tops.append(ta)
            m = at((sa + sb) / 2, rail_sg * pile_u)
            tops.append(_post(h, m, joist_bot - 0.02, deck_z + RAIL_TOP - 0.02 + h.j(0.02), d))
        tops.append(rail_tops[-1][1])
        _rails(h, tops, [deck_z] * len(tops))
    # A ladder down into the water off the end of the deck.
    top_z, bot_z = deck_z + 0.8, max(gz(at(length + 0.3, 0.0)) + 0.1, -2.3)
    rails = []
    for sg in (-1, 1):
        a = at(length + 0.07, sg * 0.28)
        b = at(length + 0.4, sg * 0.3)
        HK.beam(h, "timber", Vector((b.x, b.y, bot_z)), Vector((a.x, a.y, top_z)), 0.07, 0.09, up=d)
        rails.append((Vector((a.x, a.y, top_z)), Vector((b.x, b.y, bot_z))))
    zr = deck_z - 0.3
    while zr > bot_z + 0.15:
        f = (top_z - zr) / (top_z - bot_z)
        p0, p1 = rails[0][0].lerp(rails[0][1], f), rails[1][0].lerp(rails[1][1], f)
        HK.tube(h, "timber", p0 - (p1 - p0) * 0.1, p1 + (p1 - p0) * 0.1, 0.03, sides=7)
        zr -= 0.3
    # Steps down to the sand at the landward end when the deck stands clear of it.
    g0 = gz(at(-0.4, 0.0))
    if deck_z - g0 > 0.25:
        _flight(h, at(0.0, 0.0), -d, deck_z, g0, width - 0.5, ground_fn, rails=())
    PATHS.append((Vector((O.x, O.y, deck_z)), Vector((*(O + d * length).xy, deck_z)), width / 2 + 0.2))
    return h.finish(c, (O.x, O.y, deck_z))


def broken_pier(c, start_xy, heading, length, ground_fn, rng, width=2.0, label="broken pier"):
    """An old wrecked pier on the sand (the reference's beach): a few bents still standing, some
    piles leaning or snapped off at odd heights, one or two gone; over the landward bays the caps
    and joists survive with planks MISSING, TILTED (dropped off a joist at one end) or skewed;
    further out a joist has fallen with one end on the sand; loose planks lie half buried around
    it. Deck about 1 m over the sand at the start, level, so the far end stands over the
    shallows. `rng` is a random.Random, so the lead's seed decides the wreck. Returns the root."""
    h = Kit(label, rng.randrange(1 << 30))
    h.rng = rng
    d = Vector((math.cos(heading), math.sin(heading), 0))
    sd = Vector((-d.y, d.x, 0))
    O = _v2(start_xy)

    def at(s, u):
        return O + d * s + sd * u
    deck_z = ground_fn(O.x, O.y) + 1.0
    pile_u = width / 2 - 0.1
    cap_top = deck_z - 0.07 + 0.02 - 0.2 + 0.03
    nb = max(2, round(length / 2.4))
    bents = [0.3 + (length - 0.6) * i / nb for i in range(nb + 1)]
    # Bents 0..intact_to-1 still carry the deck. Review v1: at 55 % with 28 % of planks gone the
    # surviving deck read as a small table, not a pier.
    intact_to = max(2, int(len(bents) * 0.6) + 1)
    caps = {}
    for bi, s in enumerate(bents):
        standing = bi < intact_to
        tops = {}
        for sg in (-1, 1):
            if not standing and rng.random() < 0.2:
                continue                                  # this pile is gone
            p = at(s, sg * pile_u)
            g = ground_fn(p.x, p.y)
            lean_amt = rng.uniform(0.02, 0.09) if standing else rng.uniform(0.08, 0.22)
            ang = rng.uniform(0, math.tau)
            full = cap_top + 0.04 if standing else rng.uniform(g + 0.5, cap_top - 0.1)
            if standing and bi and rng.random() < 0.35:
                # Review v2: every surviving bent stood level and the wreck read as a table.
                # Some piles have settled into the sand, so their cap and its planks slope.
                full -= rng.uniform(0.12, 0.3)
            if not standing and rng.random() < 0.3:
                full = deck_z + 0.5 + rng.uniform(0, 0.3)   # an old bollard still standing tall
            hgt = full - g
            top = Vector((p.x + math.cos(ang) * lean_amt * hgt, p.y + math.sin(ang) * lean_amt * hgt, full))
            foot = Vector((p.x, p.y, g - SINK))
            HK.tube(h, "timber", foot, top, 0.11 * rng.uniform(0.85, 1.05), sides=9, taper=rng.uniform(0.7, 0.9))
            tops[sg] = top
        if standing and len(tops) == 2:
            a, b = tops[-1], tops[1]
            ext = (b - a).normalized() * 0.25
            HK.beam(h, "timber", a - ext - Z * 0.08, b + ext - Z * 0.08, 0.18, 0.2, up=Z)
            caps[bi] = (a - Z * 0.02, b - Z * 0.02)
    # Joists over the intact bays, following whatever the leaning caps give them.
    js = sorted(caps)
    joist_z = {}
    for bi, bj in zip(js, js[1:]):
        if bj != bi + 1:
            continue
        for k, f in enumerate((0.0, 0.5, 1.0)):
            a = caps[bi][0].lerp(caps[bi][1], f) + Z * 0.1
            b = caps[bj][0].lerp(caps[bj][1], f) + Z * 0.1
            HK.beam(h, "timber", a, b, 0.12, 0.2, up=Z)
            joist_z[(bi, k)] = (a, b)
    # One joist fallen off the last intact cap, its far end on the sand.
    if js:
        last = caps[js[-1]]
        a = last[0].lerp(last[1], rng.uniform(0.2, 0.8)) + Z * 0.1
        far = a + d * rng.uniform(2.5, 3.5) + sd * rng.uniform(-0.6, 0.6)
        far.z = ground_fn(far.x, far.y) + 0.06
        HK.beam(h, "timber", a, far, 0.12, 0.2, up=Z)
    # Planks over the intact bays: some gone, some dropped at one end, the rest skewed.
    for bi, bj in zip(js, js[1:]):
        if bj != bi + 1:
            continue
        (la, lb), (ra, rb) = joist_z[(bi, 0)], joist_z[(bi, 2)]
        s, span = 0.0, (lb - la).length
        while s < span - 0.2:
            w = rng.uniform(0.24, 0.3)
            f = (s + w / 2) / span
            s += w + rng.uniform(0.015, 0.05)
            roll = rng.random()
            if roll < 0.2:
                continue                                 # missing
            # Joist tops are 0.1 over these centre lines: a plank centred 0.125 up sinks 1 cm into
            # them (0.135 put its underside exactly on the joist top, one plane).
            L, R = la.lerp(lb, f) + Z * 0.125, ra.lerp(rb, f) + Z * 0.125
            ext = (R - L).normalized() * rng.uniform(0.02, 0.14)
            L, R = L - ext, R + ext
            if roll < 0.34:
                # Dropped: one end still on its joist, the other slid off and down.
                drop_end = rng.choice((0, 1))
                if drop_end:
                    R = R + d * rng.uniform(-0.2, 0.2) - Z * rng.uniform(0.35, 0.6)
                else:
                    L = L + d * rng.uniform(-0.2, 0.2) - Z * rng.uniform(0.35, 0.6)
                up = (R - L).cross(d).cross(R - L).normalized()
                up = up if up.z > 0 else -up
                board(h, L, R, w - 0.02, 0.07, up=up)
            else:
                skew = d * rng.uniform(-0.05, 0.05)
                board(h, L + skew, R - skew, w - 0.02, 0.07, up=(Z + d * rng.uniform(-0.03, 0.03)).normalized())
    # Loose planks half buried in the sand around the wreck.
    for _ in range(rng.randint(3, 5)):
        c_ = at(rng.uniform(0.5, length), rng.uniform(-width, width))
        g = ground_fn(c_.x, c_.y)
        ang = rng.uniform(0, math.tau)
        u = Vector((math.cos(ang), math.sin(ang), 0))
        L = rng.uniform(1.4, 2.2)
        a = c_ - u * L / 2
        b = c_ + u * L / 2
        a.z, b.z = ground_fn(a.x, a.y) + 0.01, ground_fn(b.x, b.y) + 0.01
        board(h, a, b, rng.uniform(0.24, 0.3), 0.07, up=(Z + Vector((rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1), 0))).normalized())
    PATHS.append((Vector((O.x, O.y, deck_z)), Vector((*(O + d * length).xy, deck_z)), width / 2 + 0.3))
    return h.finish(c, (O.x, O.y, deck_z))


# ---------------------------------------------------------------- placement suggestions

def _in_pocket(C, x, y, grow=1.15, skip=()):
    for p in C.POCKETS:
        if p[0] in skip:
            continue
        if math.hypot((x - p[1]) / p[4], (y - p[2]) / p[5]) < grow:
            return True
    return False


def _rim_point(p, toward, f=0.93):
    """The point at fraction f of pocket p's ellipse in the direction of `toward`."""
    u = Vector((toward[0] - p[1], toward[1] - p[2], 0)).normalized()
    t = f / math.sqrt((u.x / p[4]) ** 2 + (u.y / p[5]) ** 2)
    return Vector((p[1] + u.x * t, p[2] + u.y * t, 0))


def _route_level(C, a, b, z, skip, step=2.0, max_off=14.0, off_step=1.0):
    """A level path from a to b that holds the ground about 0.9 m under the deck: dynamic
    programming over sideways offsets from the straight line, so the walk bends along the rock
    face instead of cutting through the hill or leaping a gully. Stays off other pockets, the
    beach, the court and the stairs."""
    A, Bv = _v2(a), _v2(b)
    L = (Bv - A).length
    n = max(3, int(L / step))
    d = (Bv - A) / L
    nrm = Vector((-d.y, d.x, 0))
    M = int(max_off / off_step)
    offs = [k * off_step for k in range(-M, M + 1)]
    stairs = stair_segments(C, clear=2.2)
    target = z - 0.9

    def cost(p):
        g = C.height(p.x, p.y)
        c = 0.4 * (g - target) ** 2
        if g > z - 0.3:
            c += 30.0 * (g - (z - 0.3)) ** 2 + 10.0      # the deck would be in the hill
        if _in_pocket(C, p.x, p.y, 1.1, skip) or C.coast_distance(p.x, p.y) < C.BEACH_BAND + 2:
            c += 400.0
        if _blocked((p.x, p.y), stairs):
            c += 200.0
        return c
    INF = 1e18
    best = [[INF] * len(offs) for _ in range(n + 1)]
    back = [[0] * len(offs) for _ in range(n + 1)]
    best[0][M] = 0.0
    for i in range(1, n + 1):
        base = A + d * (L * i / n)
        edge = min(i, n - i)                          # taper to the ends: both are fixed
        for k, o in enumerate(offs):
            if abs(o) > edge * 2.2 + 0.01:
                continue
            here = cost(base + nrm * o) if i < n else 0.0
            for dk in range(-2, 3):
                kk = k + dk
                if 0 <= kk < len(offs) and best[i - 1][kk] < INF:
                    v = best[i - 1][kk] + here + 1.5 * (dk * off_step) ** 2
                    if v < best[i][k]:
                        best[i][k], back[i][k] = v, kk
    k = M
    path = []
    for i in range(n, -1, -1):
        path.append(A + d * (L * i / n) + nrm * offs[k])
        k = back[i][k]
    path.reverse()
    return _simplify(path, 0.7), best[n][M]


def _simplify(P, tol):
    """Douglas-Peucker in plan, keeping both ends."""
    if len(P) < 3:
        return P
    a, b = P[0], P[-1]
    far, idx = -1.0, 0
    for i in range(1, len(P) - 1):
        dd = _seg_dist(P[i], a, b)
        if dd > far:
            far, idx = dd, i
    if far < tol:
        return [a, b]
    return _simplify(P[:idx + 1], tol)[:-1] + _simplify(P[idx:], tol)


def suggest_cliff_walks(cove=None, max_walks=4, max_dz=3.0, max_dist=52.0):
    """Two to four LEVEL boardwalks linking nearby house pockets of similar height that no stair
    already joins (the reference's houses are joined along the rock face as well as by stairs).
    Each walk runs at the higher pocket's floor + 0.1 m (a deck top ON a floor would share its
    plane), routed along the slope by _route_level, and drops to the lower pocket by a real
    flight at that end. Returns [{"from", "to", "pts", "z", "start_z", "end_z", "cost"}], best
    first; pass pts, z, start_z and end_z straight to cliff_walk()."""
    C = _cove(cove)
    linked = {frozenset(s) for s in C.STAIRS}
    cand = []
    P = [p for p in C.POCKETS if p[0] != "court"]
    for i, A in enumerate(P):
        for Bp in P[i + 1:]:
            if frozenset((A[0], Bp[0])) in linked:
                continue
            dz = abs(A[3] - Bp[3])
            dist = math.hypot(A[1] - Bp[1], A[2] - Bp[2])
            if dz > max_dz or dist > max_dist:
                continue
            cand.append((dist + 8 * dz, A, Bp))
    cand.sort(key=lambda t: t[0])
    out, used = [], {}
    for _score, A, Bp in cand:
        if len(out) >= max_walks:
            break
        # A pocket takes at most two walks, so the walks spread over the massif.
        if used.get(A[0], 0) >= 2 or used.get(Bp[0], 0) >= 2:
            continue
        z = max(A[3], Bp[3]) + 0.1
        a = _rim_point(A, (Bp[1], Bp[2]))
        b = _rim_point(Bp, (A[1], A[2]))
        pts, cost = _route_level(C, a, b, z, skip=(A[0], Bp[0]))
        # Review v2: high west to high north routed at cost 1418 and its deck ran INTO the hill
        # for its last metres. A cost over 300 means some stretch is buried; no walk there.
        if cost > 300:
            continue
        # Cross an earlier walk? Skip it.
        if any(_cross(pts, w["pts"]) for w in out):
            continue
        out.append({"from": A[0], "to": Bp[0], "pts": [(round(q.x, 2), round(q.y, 2)) for q in pts], "z": z,
                    "start_z": A[3], "end_z": Bp[3], "cost": round(cost, 1)})
        for nm in (A[0], Bp[0]):
            used[nm] = used.get(nm, 0) + 1
    return out


def _cross(P, Q):
    def ccw(a, b, c):
        return (c[1] - a[1]) * (b[0] - a[0]) > (b[1] - a[1]) * (c[0] - a[0])
    for a, b in zip(P, P[1:]):
        for c, d in zip(Q, Q[1:]):
            if ccw(a, c, d) != ccw(b, c, d) and ccw(a, b, c) != ccw(a, b, d):
                return True
    return False


def suggest_pier(cove=None, length=16.0, prefer_heading=math.radians(225)):
    """The beach pier off the sand WEST of the court's front (x -20..-32), so the court's own
    beach front stays open for play: a start 2 m up the sand from the waterline and the heading
    nearest south-west that still runs OUT to sea (within 35 degrees of the shore's normal; this
    shore faces south-south-east, so that is about south) and whose whole run stays in open
    water, clear of the landmark rock, the Bajau spine, the free-standing homes and the spit.
    Also proposes the BROKEN pier on the sand of the spit's root, 9 m or more clear of it. Returns {"pier": {...}, "broken": {...}} with the
    keyword arguments for pier() and broken_pier()."""
    C = _cove(cove)
    best = None
    n = len(C.COAST_LINE)
    for i in range(n):
        x, y = C.COAST_LINE[i]
        if not (-32 < x < -18 and y < -12):
            continue
        bx, by = C.COAST_LINE[(i + 1) % n]
        t = Vector((bx - x, by - y, 0)).normalized()
        land = Vector((-t.y, t.x, 0))
        if C.coast_distance(x + land.x * 2, y + land.y * 2) < 0:
            land = -land
        start = Vector((x, y, 0)) + land * 2.0
        if not -30 <= start.x <= -20:
            continue
        sea = math.atan2(-land.y, -land.x)
        for k in range(-45, 46):
            hd = prefer_heading + math.radians(4 * k)
            # Review v1 and v2: "nearest to south-west" laid the pier ALONG the beach, half of it
            # on the sand, because this stretch of shore faces south-south-east and a true
            # south-west heading runs parallel to it. The pier runs out to sea (within 35 degrees
            # of the coast's normal), as far toward south-west as that allows.
            off_normal = abs((hd - sea + math.pi) % math.tau - math.pi)
            if off_normal > math.radians(35):
                continue
            d = Vector((math.cos(hd), math.sin(hd), 0))
            pts = [start + d * s for s in (4.0, length * 0.5, length, length + 3)]
            if any(C.coast_distance(q.x, q.y) > -0.5 for q in pts):
                continue
            if any(q.x > -16 for q in pts):
                continue                                  # keep the court's beach front open
            clear = min(math.hypot(q.x - C.LANDMARK[0], q.y - C.LANDMARK[1]) for q in pts)
            spine = min(_seg_dist(q, a, b) for q in pts for a, b in zip(C.SPINE, C.SPINE[1:]))
            homes = min(math.hypot(q.x - hx, q.y - hy) for q in pts for hx, hy in C.FREE_HOMES)
            if clear < 9 or spine < 12 or homes < 12:
                continue
            score = abs((hd - prefer_heading + math.pi) % math.tau - math.pi)
            if best is None or score < best[0]:
                best = (score, start, hd)
    out = {}
    if best:
        _s, start, hd = best
        out["pier"] = {"start_xy": (round(start.x, 2), round(start.y, 2)), "heading": round(hd, 4), "length": length,
                       "width": 2.2, "deck_z": C.WATER + 1.4, "seabed_z": C.SEABED}
    # The broken pier: on the east shore of the spit's root, its landward half on dry sand,
    # pointing at the water. Review v2: 14 m from the pier's start it stood right beside the
    # pier and pointed at it; it now keeps 9 m clear of the pier's whole run, and stays east of
    # x = -36, where the beached bangkas begin.
    if "pier" in out:
        p0 = Vector((*out["pier"]["start_xy"], 0))
        pd = Vector((math.cos(out["pier"]["heading"]), math.sin(out["pier"]["heading"]), 0))
        pier_run = [(p0 + pd * s)[:2] for s in (0.0, length + 2)]
    else:
        pier_run = [(-24.0, -22.0), (-24.0, -36.0)]
    cand = None
    for i in range(n):
        x, y = C.COAST_LINE[i]
        if not (-36 < x < -26 and -47 < y < -30):
            continue
        bx, by = C.COAST_LINE[(i + 1) % n]
        t = Vector((bx - x, by - y, 0)).normalized()
        land = Vector((-t.y, t.x, 0))
        if C.coast_distance(x + land.x * 2, y + land.y * 2) < 0:
            land = -land
        start = Vector((x, y, 0)) + land * 5.5
        hd = math.atan2(-land.y, -land.x)
        d = Vector((math.cos(hd), math.sin(hd), 0))
        gap = min(_seg_dist(start + d * s, *pier_run) for s in (0.0, 3.0, 6.0, 9.0))
        if gap < 9 or _in_pocket(C, start.x, start.y, 1.3):
            continue
        sc = abs(gap - 12)
        if cand is None or sc < cand[0]:
            cand = (sc, start, hd)
    if cand:
        out["broken"] = {"start_xy": (round(cand[1].x, 2), round(cand[1].y, 2)), "heading": round(cand[2], 4),
                         "length": 9.0}
    return out


# ---------------------------------------------------------------- integration helpers

def path_segments():
    """Every walk, pier and broken pier built so far as (a, b) 3D segments at deck height, in the
    form the cove's _STAIR_SEGMENTS holds: extend that list with these before clear_stair_paths()
    and the small stones standing in a walk are cleared the same way as in a flight."""
    return [(a.copy(), b.copy()) for a, b, _hw in PATHS]


def near_structure(x, y, margin=1.0):
    """True within `margin` of anything this module built (for the planting's avoid())."""
    return any(_seg_dist((x, y), a.xy, b.xy) < hw + margin for a, b, hw in PATHS)
