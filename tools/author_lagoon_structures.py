"""Lagoon Court STRUCTURES KIT: modelled plank walks, organic plank stairs, railings, cliff
boardwalks, a beach pier and a broken old pier.

Imported by tools/author_lagoon_cove.py (it has no main of its own; the test scene lives in the
scratchpad). Every builder takes the target collection first and returns the root empty of what
it built, so the lead can drop them into the cove script.

WHAT THE REFERENCE ASKS FOR (docs/LAGOON_REWORK_GUIDE.md § 1, gap review § 7a items 2, 3, 4):
the ArtStation GvJv5a village runs CONTINUOUS wooden railings along every ledge edge, boardwalk
and pier; its houses are joined by plank BOARDWALKS and plank STAIRS; its beach has a plank PIER
with posts rising above the deck, plus an old BROKEN pier on the sand.

⚠️ THE ORGANIC TIMBER TOOLKIT (owner, 2026-09-27). Three things he said, verbatim:
  * "this deck is actually modeled but the main houses walkway is just a texture, fix that":
    the pier had real planks with gaps, the water village walks were one flat box strip with a
    painted plank texture. modelled_walk() lays every plank as its own board.
  * "you should really experiment more with being organic in how you shape things. the railings
    for example are just thin and plain shapes, where as the reference isnt just a straight
    rectangular prism": the reference kit's stairs have SAWTOOTH side boards, thick blocky treads,
    and thick posts with rails that are chunky, rounded and a little crooked. organic_stairs()
    and every railing here are built from member(), which is none of those prisms.
  * "i dont like how details some of the props are. again we're going for a stylized
    semi-cartoony environment style": so organic means FEW, THICK, ROUNDED, slightly irregular
    members. Not more of them, and no fiddly bolts, nails or thin battens.

HOW IT IS BUILT, AND WHY:

  * member() is the one timber builder: a box section LOFTED along a gently bent line (a bend of
    a couple of centimetres, a small twist, a taper, its own width and thickness), so no two posts
    or rails are the same prism and none is ruler straight. Posts 16 cm, rails 12 x 8 cm, beams
    14 x 20 cm. The rounding comes from the live Bevel modifier (3 cm, two segments, hardened
    normals) that Kit.finish() puts on the timber mesh: every long edge and every end is soft.
    pile() is its round sibling (piles, bollards), with a domed top.
  * plank4() is the one plank builder: a board from four top corners (a trapezoid where a walk
    fans round a bend), a slight CUP across its width, its own thickness, and ends that do not
    line up with its neighbours. Its top maps onto exactly ONE of the seven painted boards of the
    plank texture, so no painted seam ever runs down a modelled plank.
  * NOTHING FLOATS: posts, legs and piles run SINK (0.4 m) into the ground under them (the
    `height_fn` the caller passes, the cove's `height`).
  * ⚠️ NOTHING IS FLUSH WITH THE GROUND (owner, 2026-09-27: "z-fighting on some of the stair
    entrances": a bottom landing's plank top lay exactly on the court's surface and flickered).
    Every plank and tread checks the terrain under ALL FOUR of its corners (_ground_clear) and
    stands at least 4 cm clear of it; DECK_TOPS records every top corner so a test can assert
    that no deck or tread top is within 1 cm of the terrain under it.
  * NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2): stringers and bearers rise 2 cm
    up inside the planks, rails bite 2.5 cm into the posts they pass, landing beams are 2 cm
    narrower than the flight stringers they meet, and a branch walk stops at the edge of the walk
    it joins, BRANCH_DROP (3 cm) lower, instead of running on under it.
  * SHARED MATERIALS BY NAME: timber (timber_a) for posts, rails, beams and piles; plank_walk
    (plank_c_walk, the plank_c boards without their painted mid-board joints) for every modelled
    plank and tread, so a tread's painted board has no painted butt joint across it either; plank
    (plank_c) stays available for anything that is a painted surface rather than one board.
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
import author_lagoon_houses as HK      # noqa: E402  Piece, _grid_solid, _perp, _append

Z = Vector((0.0, 0.0, 1.0))
UV_METRES = 2.0
MOUTH = (30.0, -70.0)          # the lagoon mouth: every pocket's downhill side faces it

RAIL_TOP = 0.95                # top of the top rail over the deck; the posts stand ~5 cm proud of it
RAIL_MID = 0.5
POST_W = 0.16                  # chunky (the reference's posts are about a hand wide)
POST_EVERY = 1.6               # the reference's post spacing
SINK = 0.4                     # every post and pile runs this far into the ground under it
WALK_W, WALK_T = 1.4, 0.15     # boardwalk width: as the water village's walks (walk_path)
PLANK_T = 0.08                 # modelled deck plank thickness (each plank varies a little)
TREAD_T = 0.1                  # stair treads are thicker and blockier than deck planks
STRINGER_W, STRINGER_D = 0.12, 0.3      # the sawtooth side boards of a flight
BEAM_W, BEAM_D = 0.14, 0.2              # walk stringers (bearers) under the planks
STEP_RISE, STEP_GOING, FLIGHT_MAX, LANDING = 0.2, 0.3, 8, 0.9
GROUND_CLEAR = 0.04            # a deck or tread top stands at least this far over the terrain
BRANCH_DROP = 0.03             # the cove's convention: a branch walk sits this much lower

# The cove's shared material names and the texture each wears in a scene that has not textured
# it yet (the cove's chosen_textures() / walk_material() re-apply the same ones).
SLOT_TEXTURE = {"timber": "timber_a", "plank": "plank_c", "plank_walk": "plank_c_walk", "bamboo": "bamboo_a"}
# Per mesh slot: (bevel width m, bevel angle limit deg, harden normals, segments). A slot "x:y"
# wears material x. "timber:round" is the piles: their facets meet at 45 degrees, so they take a
# higher angle limit and only their ends get rounded. Planks and piles take ONE bevel segment and
# posts, rails and beams two: measured on the spine (110 m), two segments everywhere made 88k
# triangles, most of them in the bevels of 430 planks nobody sees closer than a metre.
FINISH = {"timber": (0.03, 30.0, True, 2), "timber:round": (0.03, 50.0, True, 1),
          "plank": (0.02, 30.0, True, 1), "plank_walk": (0.02, 30.0, True, 1), "bamboo": (0.008, 50.0, False, 2)}

# Everything this module has built, as 3D centreline segments (a, b, half width). The cove's
# clear_stair_paths() takes the same (a, b) pairs, and ledge railings leave a gap where one meets
# them (see path_segments(), near_structure()).
PATHS = []
# Every modelled walk built so far (its _Line and width), so a branch walk can stop at the edge
# of the walk it joins.
WALKS = []
# Every plank and tread top corner built so far: (x, y, top z). For the ground-clearance test.
DECK_TOPS = []


# ---------------------------------------------------------------- the collector

class Kit:
    """Pieces for one structure, per mesh slot. Duck-types the house kit's `House`."""

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
        as one), with the live bevel that rounds every member."""
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
            width, sharp_deg, harden, segs = FINISH[slot]
            lim = math.radians(sharp_deg)
            for f in bm.faces:
                f.smooth = True
            for e in bm.edges:
                e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
            me = bpy.data.meshes.new(f"{self.label} {slot}")
            bm.to_mesh(me)
            bm.free()
            me.materials.append(material(slot.split(":")[0]))
            ob = bpy.data.objects.new(me.name, me)
            ob.parent = root
            c.objects.link(ob)
            bev = ob.modifiers.new("Bevel", "BEVEL")
            bev.width, bev.segments, bev.limit_method = width, segs, "ANGLE"
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


# ---------------------------------------------------------------- the organic timber toolkit

def _loft(h, slot, sections, arcs, caps=(True, True)):
    """A closed solid through matching rings of points (`sections`, each a closed loop of k
    points), with `arcs` the distance along the member at each ring. UVs: V along the member
    (the grain), U round its perimeter, world scale; the end caps are projected in their plane."""
    pc = HK.Piece()
    bm, uvl = pc.bm, pc.uv
    k = len(sections[0])
    V = [[bm.verts.new(p) for p in sec] for sec in sections]
    s0 = sections[0]
    per = [0.0]
    for i in range(k):
        per.append(per[-1] + (s0[(i + 1) % k] - s0[i]).length)
    ou, ov = h.uv_off()
    sc = 1.0 / UV_METRES
    for j in range(len(sections) - 1):
        for i in range(k):
            i2 = (i + 1) % k
            f = bm.faces.new((V[j][i], V[j][i2], V[j + 1][i2], V[j + 1][i]))
            for loop, (pp, aa) in zip(f.loops, ((per[i], arcs[j]), (per[i + 1], arcs[j]),
                                                (per[i + 1], arcs[j + 1]), (per[i], arcs[j + 1]))):
                loop[uvl].uv = (pp * sc + ou, aa * sc + ov)
    for ring, sec, flip, use in ((V[0], sections[0], True, caps[0]), (V[-1], sections[-1], False, caps[1])):
        if not use:
            continue
        f = bm.faces.new(list(reversed(ring)) if flip else ring)
        ctr = sum(sec, Vector()) / k
        e1 = (sec[0] - ctr).normalized()
        nrm = (sec[1] - sec[0]).cross(sec[2] - sec[1])
        e2 = nrm.cross(e1).normalized() if nrm.length > 1e-9 else Vector((0, 0, 1))
        for loop in f.loops:
            q = loop.vert.co - ctr
            loop[uvl].uv = (q.dot(e1) * sc + ou, q.dot(e2) * sc + ov)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return h.add(slot, pc)


def _frame(d, up):
    side = up.cross(d)
    if side.length < 1e-6:
        side = Vector((1, 0, 0)).cross(d)
    side.normalize()
    return side, d.cross(side)


def member(h, slot, p0, p1, w, t, up=Z, taper=None, bend=None, twist=None, vary=True, rings=None, roof=False):
    """ONE chunky timber from p0 to p1: `w` across (horizontal when `up` is Z), `t` along `up`,
    the house kit's `beam` signature. Organic, deterministic per the kit's seed:
      * its own size: w and t each within +-7 % (vary=False keeps them exact),
      * tapered: the p1 end is `taper` (default 88..97 %) of the p0 end,
      * BENT: the middle bows up to `bend` metres (default ~1.2 cm per metre, at most 3.5 cm) in a
        random direction, while both ends stay exactly on p0 and p1 so joints still land,
      * TWISTED by up to `twist` radians end to end (default +-0.05).
    A box section at every ring; the Bevel modifier of Kit.finish rounds its edges and ends.
    Two to five rings, so a member is a few dozen faces before the bevel."""
    p0, p1 = Vector(p0), Vector(p1)
    L = (p1 - p0).length
    if L < 1e-4:
        return None
    d = (p1 - p0) / L
    side, upv = _frame(d, up)
    rng = h.rng
    if vary:
        w *= rng.uniform(0.93, 1.07)
        t *= rng.uniform(0.93, 1.07)
    taper = rng.uniform(0.88, 0.97) if taper is None else taper
    amp = (min(0.035, 0.012 * L) if bend is None else bend) * rng.uniform(0.5, 1.0)
    ang = rng.uniform(0.0, math.tau)
    bdir = side * math.cos(ang) + upv * math.sin(ang)
    tw = rng.uniform(-0.05, 0.05) if twist is None else twist
    n = rings or max(2, min(5, 1 + math.ceil(L / 0.8)))
    secs, arcs = [], []
    for r in range(n):
        f = r / (n - 1)
        c = p0 + d * (L * f) + bdir * (amp * math.sin(math.pi * f))
        s = 1.0 + (taper - 1.0) * f
        a = tw * (f - 0.5)
        sa = side * math.cos(a) + upv * math.sin(a)
        ua = upv * math.cos(a) - side * math.sin(a)
        hw, ht = w * s / 2, t * s / 2
        secs.append([c - sa * hw - ua * ht, c + sa * hw - ua * ht, c + sa * hw + ua * ht, c - sa * hw + ua * ht])
        arcs.append(L * f)
    return _loft(h, slot, secs, arcs)


def pile(h, foot, top, r, slot="timber:round", sides=8, taper=None, bend=None, dome=True):
    """A ROUND timber (a pile, a bollard, a leg): tapered toward `top`, gently bent (both ends
    stay put), a slightly uneven girth, and a DOMED top (two shrinking rings) so a bollard reads
    as a soft rounded post rather than a sawn-off cylinder."""
    foot, top = Vector(foot), Vector(top)
    L = (top - foot).length
    if L < 1e-4:
        return None
    d = (top - foot) / L
    e1, e2 = HK._perp(d)
    rng = h.rng
    taper = rng.uniform(0.8, 0.92) if taper is None else taper
    amp = (min(0.045, 0.012 * L) if bend is None else bend) * rng.uniform(0.5, 1.0)
    ang = rng.uniform(0.0, math.tau)
    bdir = e1 * math.cos(ang) + e2 * math.sin(ang)
    ph = rng.uniform(0.0, math.tau)
    n = max(2, min(4, 1 + math.ceil(L / 1.6)))
    stations = [(L * i / (n - 1), 1.0 + (taper - 1.0) * i / (n - 1)) for i in range(n)]
    if dome and L > r * 2:
        cap = stations.pop()
        stations += [(L - r * 0.45, cap[1] * 0.93), (L, cap[1] * 0.55)]
    secs, arcs = [], []
    for s, f in stations:
        c = foot + d * s + bdir * (amp * math.sin(math.pi * min(1.0, s / L)))
        g = f * rng.uniform(0.97, 1.03)
        secs.append([c + (e1 * math.cos(a) + e2 * math.sin(a)) * (r * g)
                     for a in (ph + math.tau * k / sides for k in range(sides))])
        arcs.append(s)
    return _loft(h, slot, secs, arcs)


def plank4(h, f0, b0, f1, b1, t, up=Z, cup=0.0, warp=None, slot="plank_walk", record=True):
    """ONE board from its four TOP corners: (f0, b0) is one end (front and back edge), (f1, b1)
    the other end. A trapezoid is fine (a walk fanning round a bend). `t` thick along -`up`.
    WARPED: one diagonal pair of corners rises and the other falls by `warp` (default a random
    +-8 mm), so a board is never a flat slab and a deck of them catches the light unevenly. With
    `cup` the top is also CUPPED (its middle line that much lower than its edges): it costs a
    middle row of faces, so the long decks leave it off (the warp already reads at play distance).
    The top maps onto exactly ONE of the plank texture's seven painted boards (a random one): U
    across the board inside that board's strip, V along it inside 0.05..0.75 of the tile, clear
    of the texture's butt joint at the tile edge; a board longer than 1.4 m is gently stretched
    along its grain."""
    f0, b0, f1, b1 = (Vector(q) for q in (f0, b0, f1, b1))
    wp = h.j(0.008) if warp is None else warp
    f0, b1 = f0 + up * wp, b1 + up * wp
    b0, f1 = b0 - up * wp, f1 - up * wp
    if cup:
        m0, m1 = (f0 + b0) / 2 - up * cup, (f1 + b1) / 2 - up * cup
        top = [[f0, m0, b0], [f1, m1, b1]]
    else:
        top = [[f0, b0], [f1, b1]]
    bot = [[q - up * t for q in row] for row in top]
    pc = HK._grid_solid(top, bot)
    c0, c1 = (f0 + b0) / 2, (f1 + b1) / 2
    L = max((c1 - c0).length, 1e-3)
    d = (c1 - c0) / L
    wv = ((b0 - f0) + (b1 - f1)) / 2
    w = max(wv.length, 0.05)
    side = (wv - d * wv.dot(d)).normalized()
    k = h.rng.randrange(7)
    u0, su = k / 7 + 0.012, (1 / 7 - 0.024) / w
    vs = min(1.0 / UV_METRES, 0.68 / L)
    v0 = 0.05 + h.rng.uniform(0.0, max(0.0, 0.70 - L * vs))
    pc.bm.normal_update()
    origin = f0
    for fc in pc.bm.faces:
        n = fc.normal
        a_up, a_side, a_d = abs(n.dot(up)), abs(n.dot(side)), abs(n.dot(d))
        for loop in fc.loops:
            q = loop.vert.co - origin
            along, across, thick = q.dot(d), q.dot(side), q.dot(up) + t
            if a_d >= a_up and a_d >= a_side:           # the end grain
                uvv = (u0 + across * su, v0 + thick * vs)
            elif a_side > a_up:                          # the long edges
                uvv = (u0 + thick * su, v0 + along * vs)
            else:                                        # top and underside
                uvv = (u0 + across * su, v0 + along * vs)
            loop[pc.uv].uv = uvv
    if record:
        DECK_TOPS.extend((q.x, q.y, q.z) for q in (f0, b0, f1, b1))
    return h.add(slot, pc)


def board(h, p0, p1, w, t, up=Z, slot="plank_walk"):
    """ONE plank from p0 to p1 (its length), `w` wide, `t` thick: plank4 with this board's own
    width (+-6 %), thickness (+-8 %) and ends that are not cut square (each corner +-2.5 cm)."""
    p0, p1 = Vector(p0), Vector(p1)
    L = (p1 - p0).length
    d = (p1 - p0) / L
    side = up.cross(d)
    if side.length < 1e-6:
        side = Vector((1, 0, 0)).cross(d)
    side.normalize()
    w *= h.rng.uniform(0.94, 1.06)
    t *= h.rng.uniform(0.92, 1.08)
    j = h.j
    f0 = p0 - side * (w / 2) + d * j(0.025)
    b0 = p0 + side * (w / 2) + d * j(0.025)
    f1 = p1 - side * (w / 2) + d * j(0.025)
    b1 = p1 + side * (w / 2) + d * j(0.025)
    return plank4(h, f0, b0, f1, b1, t, up=up, slot=slot)


def _ground_clear(height_fn, corners, top, flat=0.08, lift_max=0.06):
    """How far to RAISE a deck or tread whose top is at `top` over the given plan corners so it
    is never flush with the terrain (owner: "z-fighting on some of the stair entrances"). The
    terrain is read at every corner and the centre.
      * top already GROUND_CLEAR over the highest point: 0.
      * short of that by at most `lift_max`: raise it to GROUND_CLEAR over the highest point,
        whatever the slope (a plank lifts a few centimetres; nobody sees that).
      * short by more, on nearly FLAT ground (range under `flat`): the piece is sunk in the
        ground; the raise returned is over GROUND_CLEAR + 1.2 cm, which callers read as "leave
        this piece out" (nobody sees a buried board, and a raised one would stand out of line).
      * short by more on a steep slope: the ground cuts through the piece along a line, which is
        no shared plane, so 0.
    Returns the raise, >= 0."""
    if height_fn is None:
        return 0.0
    pts = list(corners) + [sum((Vector(q) for q in corners), Vector()) / len(corners)]
    gs = [height_fn(q[0], q[1]) for q in pts]
    gmax, gmin = max(gs), min(gs)
    need = gmax + GROUND_CLEAR - top
    if need <= 0.0:
        return 0.0
    if need <= lift_max or gmax - gmin < flat:
        return need
    return 0.0


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
        s.append(s[-1] + (_v2(P[i]) - _v2(P[i - 1])).length)
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


class _Line:
    """A walk's centreline with its BENDS ROUNDED: every corner of the input polyline becomes an
    arc (radius up to `radius`, never under half the width + 0.3 m, so the inner edge never folds
    back), sampled every 10 degrees. Heights ride along linearly. at(s) gives the point (with z),
    the plan tangent and the plan left normal anywhere along it, so planks laid across it FAN round
    each bend as trapezoids: no overlap on the inside, no gap on the outside."""

    def __init__(self, pts, zs, width, radius=2.4, fillet=True):
        P = [_v2(p) for p in pts]
        Zs = [float(z) for z in zs]
        n = len(P)
        Ls = [(P[i + 1] - P[i]).length for i in range(n - 1)]
        out = [Vector((P[0].x, P[0].y, Zs[0]))]
        for i in range(1, n - 1):
            a, b = (P[i] - P[i - 1]).normalized(), (P[i + 1] - P[i]).normalized()
            th = a.angle(b, 0.0)
            if not fillet or th < math.radians(2):
                out.append(Vector((P[i].x, P[i].y, Zs[i])))
                continue
            tmax = min(Ls[i - 1] * (0.5 if i > 1 else 0.9), Ls[i] * (0.5 if i < n - 2 else 0.9))
            T = min(radius * math.tan(th / 2), tmax)
            R = T / math.tan(th / 2)
            if R < width / 2 + 0.3:
                R = width / 2 + 0.3
                T = R * math.tan(th / 2)
            A, Bp = P[i] - a * T, P[i] + b * T
            zA = Zs[i] + (Zs[i - 1] - Zs[i]) * min(1.0, T / max(Ls[i - 1], 1e-6))
            zB = Zs[i] + (Zs[i + 1] - Zs[i]) * min(1.0, T / max(Ls[i], 1e-6))
            turn = 1.0 if a.x * b.y - a.y * b.x > 0 else -1.0
            ctr = A + Vector((-a.y, a.x, 0)) * (R * turn)
            v0 = A - ctr
            k = max(2, math.ceil(th / math.radians(10)))
            for j in range(k + 1):
                ang = turn * th * j / k
                v = Vector((v0.x * math.cos(ang) - v0.y * math.sin(ang), v0.x * math.sin(ang) + v0.y * math.cos(ang), 0))
                q = ctr + v
                out.append(Vector((q.x, q.y, zA + (zB - zA) * j / k)))
        out.append(Vector((P[-1].x, P[-1].y, Zs[-1])))
        # Drop near-duplicates (a fillet that reached a segment's end).
        self.P = [out[0]]
        for q in out[1:]:
            if (_v2(q) - _v2(self.P[-1])).length > 0.02:
                self.P.append(q)
        self.S = _arc(self.P)
        self.T = []
        m = len(self.P)
        for i in range(m):
            a = _v2(self.P[min(i + 1, m - 1)]) - _v2(self.P[max(i - 1, 0)])
            self.T.append(a.normalized())
        self.width = width
        self.length = self.S[-1]

    def at(self, s):
        """(point with z, plan tangent, plan left normal) at arc length s; straight on past
        either end (a beam that tucks 3 cm into the next piece)."""
        S, P = self.S, self.P
        if s < 0.0 or s > self.length:
            e = 0 if s < 0.0 else -1
            tan = self.T[e]
            q = P[e] + tan * (s if s < 0.0 else s - self.length)
            return q, tan, Vector((-tan.y, tan.x, 0))
        for i in range(len(P) - 1):
            if s <= S[i + 1] or i == len(P) - 2:
                seg = S[i + 1] - S[i]
                t = 0.0 if seg < 1e-9 else (s - S[i]) / seg
                tan = self.T[i].lerp(self.T[i + 1], t).normalized()
                return P[i].lerp(P[i + 1], t), tan, Vector((-tan.y, tan.x, 0))
        tan = self.T[-1]
        return P[-1], tan, Vector((-tan.y, tan.x, 0))

    def dist(self, x, y):
        return min(_seg_dist((x, y), a, b) for a, b in zip(self.P, self.P[1:]))


# ---------------------------------------------------------------- railings

def _post(h, base, foot_z, top_z, facing, slot="timber", w=POST_W):
    """A chunky post from foot_z (in the ground) to top_z, leaning a degree or so, turned to its
    run, a little thinner at the top, and bowed a centimetre or two."""
    lean = Vector((h.j(0.025), h.j(0.025), 0.0))
    foot = Vector((base.x, base.y, foot_z)) - lean * 0.5
    top = Vector((base.x, base.y, top_z)) + lean
    fc = Vector((facing.x, facing.y, 0))
    fc = fc.normalized() if fc.length > 1e-6 else Vector((1, 0, 0))
    member(h, slot, foot, top, w, w, up=fc, taper=h.rng.uniform(0.84, 0.94), bend=0.02)
    return top


def _rails(h, tops, deck_zs, outs=None, slot="timber"):
    """Two chunky rails along a run of post tops: a TOP RAIL 12 cm deep whose top sits ~5 cm under
    the post tops (the reference's posts stand proud of their rail), and a MID RAIL at RAIL_MID.
    One board per bay, butting its neighbour at the post with a 6 mm gap (two boards overlapping at
    a post would share their faces), and the first and last boards run 7 cm past the end posts, as
    a hand-built fence's rails do. Each board wanders: its own size, a bow of 1.5 to 3 cm, a twist,
    and its ends up to 2 cm up or down, so no rail is ruler straight (owner: the reference's rails
    are "not just a straight rectangular prism").
    outs  None: the rails pass THROUGH the posts' centres (narrower than the post, so no face is
          shared). Or one outward vector per post: the boards ride on that face of the posts,
          biting 2.5 cm into them."""
    n = len(tops)
    for i in range(n - 1):
        a, b = tops[i], tops[i + 1]
        if (_v2(b) - _v2(a)).length < 0.05:
            continue
        for kind, w, t in (("top", 0.085, 0.12), ("mid", 0.075, 0.1)):
            if kind == "top":
                pa, pb = a - Z * (0.11 + h.j(0.018)), b - Z * (0.11 + h.j(0.018))
            else:
                pa = Vector((a.x, a.y, deck_zs[i] + RAIL_MID + h.j(0.02)))
                pb = Vector((b.x, b.y, deck_zs[i + 1] + RAIL_MID + h.j(0.02)))
            if outs is not None:
                pa = pa + outs[i] * (POST_W / 2 + w / 2 - 0.025)
                pb = pb + outs[i + 1] * (POST_W / 2 + w / 2 - 0.025)
            d = _v2(pb - pa).normalized()
            pa = pa + d * (-0.07 if i == 0 else 0.003)
            pb = pb + d * (0.07 if i == n - 2 else -0.003)
            member(h, slot, pa, pb, w, t, up=Z, bend=0.03, twist=0.06)


def _foot(x, y, deck_z, height_fn, bolt=0.25, max_leg=None):
    """Where a post ends: in the ground (SINK under it) when there is ground, else bolted to the
    deck's edge `bolt` under the deck top. With `max_leg`, a post whose run down to the ground
    would be longer than that stops at the deck instead: review organic_stairs_tall_v1, where
    every rail post of a flight crossing a gully ran five metres down to the grass and the
    flight stood in a forest of 16 cm sticks. The structure's own legs (_stair_legs) carry it."""
    if height_fn is None:
        return deck_z - bolt
    g = height_fn(x, y) - SINK
    if max_leg is not None and deck_z - bolt - g > max_leg:
        return deck_z - bolt
    return min(deck_z - bolt, g)


def _railing_into(h, points, zs, height_fn=None, spacing=POST_EVERY, avoid=(), min_run=0.9, slot="timber",
                  bolt=0.25, outward=None, max_leg=None):
    """Posts and rails along one polyline into kit `h`; returns the post tops. `zs` is a deck
    height per point (or one number); the line splits into runs wherever `avoid` blocks it.
    outward  None (rails through the posts), +1 / -1 (rails on the posts' left / right face, left
             of the line's travel), or f(q, tangent) -> the outward vector at a post.
    max_leg  see _foot: a post that would run further than this down to the ground hangs from
             the deck instead."""
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
        tops, dzs, outs = [], [], []
        for sq in st:
            q, tan, _i, _t = _at(Q, SQ, sq)
            dz = deck_z(s0 + sq)            # Q keeps the line's own bends, so its arc is the line's
            top_z = dz + RAIL_TOP + 0.05 + h.j(0.02)
            tops.append(_post(h, q, _foot(q.x, q.y, dz, height_fn, bolt, max_leg), top_z, tan, slot))
            dzs.append(dz)
            if callable(outward):
                outs.append(outward(q, tan))
            elif outward:
                outs.append(Vector((-tan.y, tan.x, 0)) * outward)
        _rails(h, tops, dzs, outs if outward else None, slot)
        tops_all += tops
    return tops_all


def railing(c, points, zs, height_fn=None, sides=None, offset=WALK_W / 2 + 0.02, spacing=POST_EVERY, avoid=(),
            label="railing", seed=0, slot="timber"):
    """A railing following a polyline: chunky posts every ~1.6 m (at every bend too), a top rail
    and a mid rail.

    points     [(x, y), ...] the line (a walk's centreline when `sides` is given)
    zs         the deck height under the rail: one number, or one per point
    height_fn  f(x, y) -> ground z. Given, every post runs SINK into the ground under it (so a
               railing on a ledge or on legs never floats); None, posts bolt to the deck edge.
    sides      None: the rail stands ON the line, its rails through the posts. ("left",),
               ("right",) or ("left", "right"): the rail stands `offset` to that side of the line
               (left = +90 degrees from travel), its rails on the posts' OUTER face.
    avoid      things the rail must leave open: (a_xy, b_xy, clearance) segments and/or
               callables f(x, y) -> True where blocked. A blocked stretch becomes a gap.
    Returns the root empty (one timber mesh under it)."""
    h = Kit(label, seed)
    lines = [(points, None)] if not sides else []
    for sd in sides or ():
        sg = 1 if sd == "left" else -1
        lines.append(([(q.x, q.y) for q in _offset_polyline(points, offset * sg)], sg))
    for line, sg in lines:
        _railing_into(h, line, zs, height_fn, spacing, avoid, slot=slot, outward=sg)
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
        # The rails ride on the posts' OUTER (downhill) face, away from the pocket's centre.
        _railing_into(h, pts, pz, height_fn, avoid=blockers,
                      outward=lambda q, tan: Vector((q.x - px, q.y - py, 0)).normalized())
    if not h.pieces:
        return None
    return h.finish(c, (px, py, pz))


def _arc_blocker(px, py, rx, ry, a0, hw):
    def f(x, y):
        a = math.atan2((y - py) / ry, (x - px) / rx)
        return abs((a - a0 + math.pi) % math.tau - math.pi) < hw
    return f




# ---------------------------------------------------------------- modelled decks

def _walk_deck(h, L, s0, s1, width, height_fn=None, beam_u=None, beam_w=BEAM_W, beam_d=BEAM_D, beam_s=None,
               plank_t=PLANK_T, slot="plank_walk"):
    """A deck of INDIVIDUAL planks laid ACROSS line L from s0 to s1, on two chunky stringers.

    ⚠️ Owner, 2026-09-27: "this deck is actually modeled but the main houses walkway is just a
    texture, fix that". Each plank is its own board (plank4): 24..31 cm wide with a 2.4 cm gap,
    its own thickness, its ends 3 cm past the stringers give or take 3.5 cm (so the deck edge is
    ragged, as a hand-laid walk's is), its top a few millimetres up or down, cupped. Round a bend
    the planks FAN as trapezoids across the rounded centreline (_Line), so there is no overlap on
    the inside and no gap on the outside. On a slope (the jetty ramp) each plank tilts with it.
    A plank whose top would lie flush with flat terrain is lifted GROUND_CLEAR over it, and one
    buried in flat terrain is left out (_ground_clear).

    The two stringers run the whole deck under the planks, `beam_u` either side of the centre
    (default: 24 cm in from the deck edge), rising 3.5 cm up inside the planks (more than any
    plank's warp and thickness change can take back, so no plank's underside ever shares a plane
    with a stringer's top). `beam_s` = (from, to) along L, which may run a
    few centimetres past the planks (a landing's beams tuck into the flights beside it)."""
    j = h.j
    total = s1 - s0
    if total < 0.05:
        return
    widths, acc = [], 0.0
    while acc < total - 0.12:
        w = h.rng.uniform(0.24, 0.31)
        widths.append(w)
        acc += w
    if not widths:
        widths, acc = [total], total
    k = total / acc
    s, gap = s0, 0.024
    for w in widths:
        w *= k
        sa, sb = s + gap / 2, s + w - gap / 2
        s += w
        (pa, _ta, na), (pb, _tb, nb) = L.at(sa), L.at(sb)
        hl, hr = width / 2 + 0.03 + j(0.035), width / 2 + 0.03 + j(0.035)
        rock = j(0.004)
        f0, b0 = pa + na * hl + Z * rock, pb + nb * hl + Z * rock
        f1, b1 = pa - na * hr - Z * rock, pb - nb * hr - Z * rock
        along = pb - pa
        up = along.cross(na).normalized()
        if up.z < 0:
            up = -up
        top = min(q.z for q in (f0, b0, f1, b1))
        rz = _ground_clear(height_fn, [f0, b0, f1, b1], top)
        if rz > GROUND_CLEAR + 0.012:
            continue                                  # sunk in flat ground: nobody sees it
        if rz:
            f0, b0, f1, b1 = (q + Z * rz for q in (f0, b0, f1, b1))
        plank4(h, f0, b0, f1, b1, plank_t * h.rng.uniform(0.9, 1.08), up=up, slot=slot)
    if beam_u is None:
        beam_u = width / 2 - 0.24
    bs0, bs1 = beam_s or (s0, s1)
    for sg in (-1, 1):
        ss = [bs0] + [x for x in L.S if bs0 + 0.05 < x < bs1 - 0.05] + [bs1]
        # Extra rings on long straights so the stringer can wander a little.
        dense = [ss[0]]
        for a, b in zip(ss, ss[1:]):
            n = max(1, int((b - a) / 1.6))
            dense += [a + (b - a) * i / n for i in range(1, n + 1)]
        secs = []
        for x in dense:
            p, _t, nrm = L.at(x)
            c = p + nrm * (sg * beam_u + j(0.008)) - Z * (plank_t - 0.035 + beam_d / 2 + j(0.006))
            hw, hd = nrm * (beam_w / 2), Z * (beam_d / 2)
            secs.append([c - hw - hd, c + hw - hd, c + hw + hd, c - hw + hd])
        _loft(h, "timber", secs, [x - dense[0] for x in dense])


# ---------------------------------------------------------------- steps

def _stair_profile(length, rise):
    """The flight's profile along its horizontal length: a list of (s0, s1, z0, z1, kind), the
    cove's stair_profile() rule (owner, 2026-09-27: "some stair steps are too long. if the slope
    is too low, just do smth like a set of stairs then a flat walkway, then a set of stairs
    again"). Steps are always a real size (0.2 m rise, 0.3 m going); a shallower slope is split
    into flights of at most 8 steps with FLAT landings, the spare length shared out as landings.
    Only a slope steeper than the steps themselves compresses the going."""
    n = max(1, round(abs(rise) / STEP_RISE))
    going = STEP_GOING
    if n * going > length:
        going = length / n
    flights = [FLIGHT_MAX] * (n // FLIGHT_MAX) + ([n % FLIGHT_MAX] if n % FLIGHT_MAX else [])
    spare = length - n * going
    landing = spare / (len(flights) + 1)
    segs, s_, z = [], 0.0, 0.0
    dz = rise / n
    for k in flights:
        if landing > 0.05:
            segs.append((s_, s_ + landing, z, z, "landing"))
            s_ += landing
        segs.append((s_, s_ + k * going, z, z + k * dz, "flight"))
        s_ += k * going
        z += k * dz
    if landing > 0.05:
        segs.append((s_, s_ + landing, z, z, "landing"))
    return segs, going, dz


def _prism(h, slot, O, ax, side, u, th, outline, cells, jit=0.008):
    """A flat board cut to `outline` [(s, z), ...] (counter-clockwise, s along plan axis `ax`
    from plan point O, z world height), `th` thick across `side`, centred `u` along it. `cells`
    splits the outline into convex faces (lists of outline indices) so the sawtooth's notches
    triangulate cleanly in any exporter. Every outline point moves up to `jit` so no edge is ruler
    straight. UVs: the grain runs along the board's overall pitch."""
    pc = HK.Piece()
    bm, uvl = pc.bm, pc.uv
    pts = [(s, z + h.j(jit)) for s, z in outline]
    (sa, za), (sb, zb) = pts[0], pts[len(pts) // 2]
    g = Vector((sb - sa, zb - za, 0)).normalized() if abs(sb - sa) + abs(zb - za) > 1e-6 else Vector((1, 0, 0))
    if g.x < 0:
        g = -g

    def W(s, z, uu):
        return O + ax * s + side * uu + Z * z
    F = [bm.verts.new(W(s, z, u - th / 2)) for s, z in pts]
    Bk = [bm.verts.new(W(s, z, u + th / 2)) for s, z in pts]
    ou, ov = h.uv_off()
    sc = 1.0 / UV_METRES

    def face_uv(fc, verts_sz):
        for loop, (s, z) in zip(fc.loops, verts_sz):
            loop[uvl].uv = ((-s * g.y + z * g.x) * sc + ou, (s * g.x + z * g.y) * sc + ov)
    for cell in cells:
        fc = bm.faces.new([F[i] for i in cell])
        face_uv(fc, [pts[i] for i in cell])
        fc = bm.faces.new([Bk[i] for i in reversed(cell)])
        face_uv(fc, [pts[i] for i in reversed(cell)])
    n = len(pts)
    run = 0.0
    for i in range(n):
        i2 = (i + 1) % n
        seg = math.hypot(pts[i2][0] - pts[i][0], pts[i2][1] - pts[i][1])
        fc = bm.faces.new((F[i], F[i2], Bk[i2], Bk[i]))
        for loop, (a, b) in zip(fc.loops, ((0, run), (0, run + seg), (th, run + seg), (th, run))):
            loop[uvl].uv = (a * sc + ou, b * sc + ov)
        run += seg
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return h.add(slot, pc)


def _sawtooth(h, O, ax, steps, lo_z, width, hi_z=None, height_fn=None, tread_t=TREAD_T, sw=STRINGER_W,
              depth=STRINGER_D):
    """ONE ASCENDING flight: thick blocky treads on two SAWTOOTH stringers.

    ⚠️ The reference kit's stairs (ArtStation GvJv5a, "Stairs" sheet) are carried by solid
    STEPPED SIDE BOARDS, not by a thin sloping beam: owner, 2026-09-27, "the railings for example
    are just thin and plain shapes, where as the reference isnt just a straight rectangular
    prism". Each stringer here is one chunky board (12 cm thick, 30 cm deep under the notches)
    whose top edge is cut into the steps' notches, so from the side the flight reads as a
    staircase outline. The treads (10 cm thick) sit IN the notches, 2.5 cm down into them, run
    3.5 cm past the stringers' outer faces with ragged ends, overhang the riser in front by 3 cm
    (a nosing), and each is turned a little (yaw) so the flight is hand built.

    O, ax      plan point and plan direction of travel UP the flight
    steps      [(s_front, s_back, top z), ...] from the bottom, along ax from O
    lo_z       the level under the first riser (the ground or landing the flight starts from)
    hi_z       the level above the last tread, when that is a deck the flight hangs from (a walk
               or a pier): the stringers then run 14 cm on under it
    A tread whose top would lie flush with flat terrain is lifted GROUND_CLEAR over it; one buried
    in flat terrain is left out."""
    O, ax = _v2(O), _v2(ax).normalized()
    side = Vector((-ax.y, ax.x, 0))
    j = h.j
    n = len(steps)
    if n == 0:
        return
    for sa, sb, zt in steps:
        yaw = j(0.014)
        hl, hr = width / 2 + 0.035 + j(0.03), width / 2 + 0.035 + j(0.03)
        s_f, s_b = sa - 0.03, sb - 0.012
        z = zt + j(0.004)
        fL, bL = O + ax * (s_f + yaw) + side * hl, O + ax * (s_b + yaw) + side * hl
        fR, bR = O + ax * (s_f - yaw) - side * hr, O + ax * (s_b - yaw) - side * hr
        rz = _ground_clear(height_fn, [fL, bL, fR, bR], z)
        if rz > GROUND_CLEAR + 0.012:
            continue
        z += rz
        plank4(h, fL + Z * z, bL + Z * z, fR + Z * z, bR + Z * z, tread_t * h.rng.uniform(0.92, 1.08), cup=0.006,
               warp=h.j(0.005))
    S = [st[0] for st in steps] + [steps[-1][1]]
    tt = tread_t - 0.025
    N = [zt - tt for _sa, _sb, zt in steps]
    m = (steps[-1][2] - lo_z) / max(S[-1] - S[0], 1e-6)

    def zb(s):
        return lo_z - tt + (s - S[0]) * m - depth
    outline = [(S[k], zb(S[k])) for k in range(n + 1)]
    ext = None
    if hi_z is not None and hi_z - 0.1 > N[-1] + 0.02:
        e = 0.14
        ext = len(outline)
        outline += [(S[n] + e, zb(S[n] + e)), (S[n] + e, hi_z - 0.1), (S[n], hi_z - 0.1)]
    R, Lf = {}, {}
    for k in range(n - 1, -1, -1):
        R[k] = len(outline)
        outline.append((S[k + 1], N[k]))
        Lf[k] = len(outline)
        outline.append((S[k], N[k]))
    cells = []
    for k in range(n):
        cell = [k, k + 1, R[k], Lf[k]]
        if k > 0:
            cell.append(R[k - 1])
        cells.append(cell)
    if ext is not None:
        cells.append([n, ext, ext + 1, ext + 2, R[n - 1]])
    for sg in (-1, 1):
        _prism(h, "timber", O, ax, side, sg * (width / 2 - sw / 2), sw * h.rng.uniform(0.95, 1.05), outline, cells)


MAX_POST_LEG = 1.3             # a stair's rail post runs to the ground only when it is this close


def _stair_legs(h, O, fwd, spans, width, height_fn, every=2.6, min_clear=0.95):
    """LEGS under a stair: a PAIR of chunky 17 cm timbers under the two stringers (or landing
    beams) at every landing end, every change of slope, and every `every` metres along a long
    run, wherever the stair stands more than `min_clear` over the ground; a cross tie between the
    pair when it is over a metre tall, and a diagonal brace when it is over 2.2 m. A tall leg is
    thicker (17 cm, up to 24 cm at six metres), so it keeps the chunky proportions.
    Nearer the ground than `min_clear` the rail posts already run down into it (MAX_POST_LEG),
    and a leg beside each of them read as a doubled post (review organic_stairs_v2).
    Review organic_stairs_tall_v1: with every rail post run down to the grass, a flight across a
    gully stood on a forest of thin sticks. The owner asked for "few, thick" members, so the rail
    posts now stop at the stringers (MAX_POST_LEG) and these few thick legs carry the stair.
    spans  [(s0, s1, underside(s))]: plan runs along fwd from O with a function giving the
           underside of the stringers or beams there."""
    O, fwd = _v2(O), _v2(fwd).normalized()
    side = Vector((-fwd.y, fwd.x, 0))
    lat = width / 2 - STRINGER_W / 2
    todo = []
    for s0, s1, under in spans:
        n = max(1, math.ceil((s1 - s0) / every - 0.05))
        for i in range(n + 1):
            s = s0 + 0.15 + (s1 - s0 - 0.3) * i / n
            todo.append((s, under(s)))
    todo.sort()
    kept = []
    for s, zu in todo:
        if kept and s - kept[-1][0] < 0.6:
            if zu < kept[-1][1]:
                kept[-1] = (s, zu)
            continue
        kept.append((s, zu))
    for k, (s, zu) in enumerate(kept):
        feet = []
        for sg in (-1, 1):
            q = O + fwd * s + side * (sg * lat)
            feet.append((q, height_fn(q.x, q.y)))
        if min(zu - g for _q, g in feet) < min_clear:
            continue
        (qa, ga), (qb, gb) = feet if k % 2 else feet[::-1]
        tall = zu - max(ga, gb)
        lw = 0.17 + min(0.07, 0.018 * max(0.0, zu - min(ga, gb) - 2.0))
        for q, g in feet:
            member(h, "timber", Vector((q.x, q.y, g - SINK)), Vector((q.x, q.y, zu + 0.03)), lw, lw, up=fwd,
                   taper=h.rng.uniform(0.8, 0.9), bend=0.035)
        o = fwd * 0.12
        if tall > 1.0:
            zt = zu - 0.32
            member(h, "timber", Vector((qa.x, qa.y, zt)) + o - (qb - qa).normalized() * 0.12,
                   Vector((qb.x, qb.y, zt + h.j(0.03))) + o + (qb - qa).normalized() * 0.12, 0.08, 0.14, up=Z)
        if tall > 2.2:
            member(h, "timber", Vector((qa.x, qa.y, zu - 0.4)) - o,
                   Vector((qb.x, qb.y, max(gb + 0.4, zu - 2.4))) - o, 0.14, 0.07, up=fwd)


def _flight(h, top, fwd, z_top, z_bot, width, height_fn, rails=("left", "right"), ground_stop=True):
    """Real-size steps from `top` (plan point at a deck's edge, deck at z_top) along `fwd` DOWN
    to z_bot: sawtooth flights (_sawtooth) of at most eight steps with flat modelled LANDINGs
    between, and a chunky railing each side in `rails`. With `ground_stop` the flight ends early
    where the terrain under any corner of the next tread comes within GROUND_CLEAR of its top, so
    the last tread always stands clear of the ground rather than flush with it. Returns the plan
    point and height where it lands."""
    top, fwd = _v2(top), _v2(fwd).normalized()
    side = Vector((-fwd.y, fwd.x, 0))
    n = max(1, round((z_top - z_bot) / STEP_RISE))
    dz = (z_top - z_bot) / n
    s, z, k = 0.0, z_top, 0
    profile = [(0.0, z_top)]
    parts = []
    while k < n:
        batch = min(FLIGHT_MAX, n - k)
        for i in range(batch):
            zt = z - dz * (i + 1)
            if ground_stop and height_fn is not None:
                a, b = top + fwd * (s + i * STEP_GOING - 0.03), top + fwd * (s + (i + 1) * STEP_GOING)
                hw = side * (width / 2 + 0.07)
                if max(height_fn(q.x, q.y) for q in (a + hw, a - hw, b + hw, b - hw)) > zt - GROUND_CLEAR:
                    batch = i
                    n = k + i
                    break
        if batch:
            parts.append(("flight", s, z, batch))
        s += batch * STEP_GOING
        z -= batch * dz
        k += batch
        profile.append((s, z))
        if k < n:
            parts.append(("landing", s, z, 0))
            s += LANDING
            profile.append((s, z))
    for kind, s0, zs, cnt in parts:
        if kind == "flight":
            low = top + fwd * (s0 + cnt * STEP_GOING)
            steps = [(i * STEP_GOING, (i + 1) * STEP_GOING, zs - dz * (cnt - i)) for i in range(cnt)]
            _sawtooth(h, low, -fwd, steps, zs - dz * (cnt + 1), width, hi_z=zs, height_fn=height_fn)
        else:
            line = _Line([top + fwd * s0, top + fwd * (s0 + LANDING)], [zs, zs], width, fillet=False)
            _walk_deck(h, line, 0.0, LANDING, width, height_fn, beam_u=width / 2 - STRINGER_W / 2,
                       beam_w=STRINGER_W - 0.02, beam_d=0.24, beam_s=(-0.03, LANDING + 0.03))
    # The legs: the underside of the stringers along each flight, of the beams under each landing.
    tt = TREAD_T - 0.025
    spans = []
    for kind, s0, zs, cnt in parts:
        if kind == "flight":
            spans.append((s0, s0 + cnt * STEP_GOING,
                          lambda s, s0=s0, zs=zs: zs - dz - tt - STRINGER_D - (s - s0) * dz / STEP_GOING))
        else:
            spans.append((s0, s0 + LANDING, lambda s, zs=zs: zs - PLANK_T + 0.035 - 0.24))
    _stair_legs(h, top, fwd, spans, width, height_fn, min_clear=0.95 if rails else 0.25)
    if s > 0.2:
        for sd in rails:
            sg = 1 if sd == "left" else -1
            pts, zs_ = [], []
            for s_, z_ in profile:
                q = top + fwd * s_ + side * sg * (width / 2 + POST_W / 2 - 0.015)
                pts.append((q.x, q.y))
                zs_.append(z_)
            _railing_into(h, pts, zs_, height_fn, spacing=POST_EVERY, min_run=0.25, bolt=0.45, outward=sg,
                          max_leg=MAX_POST_LEG)
    end = top + fwd * s
    return end, z


def organic_stairs(c, ea, eb, label, height_fn=None, seed=0, width=None, lift=0.05, rails=("left", "right"),
                   cove=None):
    """A plank STAIR between two points, the replacement for the cove's plank_stairs(c, ea, eb,
    label): the same real-size profile (0.2 m rise, 0.3 m going, flights of at most eight steps,
    flat landings sharing the spare length), built as the reference kit builds its stairs:
      * SAWTOOTH stringers: solid stepped side boards, 12 cm thick (_sawtooth),
      * thick blocky treads, 10 cm, each a little turned, ragged at the ends, one painted board,
      * LANDINGS as modelled plank decks on side beams (_walk_deck),
      * a chunky organic railing BOTH sides following every flight and landing: 16 cm posts
        standing just outside the stringers (biting 1.5 cm into them), two rails on the posts'
        outer face; a post runs SINK into the ground when the ground is near, else it stops under
        the stringers,
      * LEGS: pairs of 17 cm timbers under the stringers at the landings and every ~2.6 m where
        the stair stands clear of the ground, tied and braced when tall (_stair_legs).

    ea, eb     3D points (Vector or (x, y, z)): the two floors it joins, either order
    height_fn  f(x, y) -> ground z; default the cove's `height`
    lift       the whole stair stands this far over ea.z / eb.z, so a landing on a pocket floor
               is never flush with it (owner: "z-fighting on some of the stair entrances")
    Appends (ea, eb) to the cove's _STAIR_SEGMENTS as plank_stairs did, so clear_stair_paths()
    still clears the small stones standing in the flight. Returns the root empty."""
    C = _cove(cove)
    height_fn = height_fn or C.height
    width = width or getattr(C, "STAIR_W", 1.5)
    ea0, eb0 = Vector(ea), Vector(eb)
    lo, hi = (ea0, eb0) if ea0.z <= eb0.z else (eb0, ea0)
    flat = _v2(hi - lo)
    length = flat.length
    fwd = flat / length
    side = Vector((-fwd.y, fwd.x, 0))
    segs, going, dz = _stair_profile(length, hi.z - lo.z)
    O = _v2(lo)
    z0 = lo.z + lift
    h = Kit(label, seed)
    for s0, s1, za, zb_, kind in segs:
        za, zb_ = za + z0, zb_ + z0
        if kind == "landing":
            line = _Line([O + fwd * s0, O + fwd * s1], [za, za], width, fillet=False)
            _walk_deck(h, line, 0.0, s1 - s0, width, height_fn, beam_u=width / 2 - STRINGER_W / 2,
                       beam_w=STRINGER_W - 0.02, beam_d=0.24, beam_s=(-0.03, s1 - s0 + 0.03))
        else:
            nst = max(1, round((zb_ - za) / dz)) if dz else 1
            steps = [(s0 + k * going, s0 + (k + 1) * going, za + (k + 1) * dz) for k in range(nst)]
            _sawtooth(h, O, fwd, steps, za, width, height_fn=height_fn)

    def zline(s):
        for s0, s1, za, zb_, _k in segs:
            if s <= s1 + 1e-6:
                t = 0.0 if s1 - s0 < 1e-9 else max(0.0, (s - s0) / (s1 - s0))
                return z0 + za + (zb_ - za) * t
        return z0 + segs[-1][3]
    keys = sorted({round(x, 4) for sg_ in segs for x in (sg_[0], sg_[1])})
    stations = []
    for a, b in zip(keys, keys[1:]):
        if b - a < 0.2:
            continue
        nb = max(1, math.ceil((b - a) / POST_EVERY - 0.05))
        stations += [a + (b - a) * i / nb + (h.j(0.08) if i else 0.0) for i in range(nb)]
    stations.append(keys[-1])
    for sd in rails:
        sg = 1 if sd == "left" else -1
        off = width / 2 + POST_W / 2 - 0.015
        tops, dzs, outs = [], [], []
        for s in stations:
            q = O + fwd * s + side * (sg * off)
            dzv = zline(s)
            foot = _foot(q.x, q.y, dzv, height_fn, 0.42, MAX_POST_LEG)
            tops.append(_post(h, q, foot, dzv + RAIL_TOP + 0.05 + h.j(0.02), fwd))
            dzs.append(dzv)
            outs.append(side * sg)
        _rails(h, tops, dzs, outs)
    tt = TREAD_T - 0.025
    spans = []
    for s0, s1, za, zb_, kind in segs:
        if kind == "landing":
            spans.append((s0, s1, lambda s, za=za: z0 + za - PLANK_T + 0.035 - 0.24))
        else:
            spans.append((s0, s1, lambda s: zline(s) - tt - STRINGER_D))
    _stair_legs(h, O, fwd, spans, width, height_fn, min_clear=0.95 if rails else 0.25)
    if hasattr(C, "_STAIR_SEGMENTS"):
        C._STAIR_SEGMENTS.append((ea0.copy(), eb0.copy()))
    return h.finish(c, (lo.x, lo.y, z0))


# ---------------------------------------------------------------- modelled walks

def modelled_walk(c, pts, zs, width=WALK_W, label="plank walk", seed=0, height_fn=None, rails=None,
                  bent_every=2.6, register_paths=False, cove=None):
    """A plank walk along a polyline: the replacement for the cove's walk_path(c, pts, zs) and
    walk(c, a, b, z) (owner, 2026-09-27: "the main houses walkway is just a texture, fix that").

    pts, zs    [(x, y), ...] and the deck top per point (one number for a level walk), exactly as
               walk_path takes them, including the spine's jetty ramp up from the sand
    width      deck width (1.4 m, walk_path's)
    height_fn  f(x, y) -> the terrain or seabed z under a pile; default the cove's `height` (in
               the lagoon that is the visible seabed shelf)
    rails      None, ("left",), ("right",) or both: a chunky railing on that edge

    Construction: INDIVIDUAL planks laid across the run with 2.4 cm gaps (_walk_deck), on two
    chunky stringers; BENTS every ~2.6 m, each a headstock log across under the stringers and a
    round pile each side down into the seabed or sand, with one diagonal brace on a tall bent.
    Bends are ROUNDED (_Line) and the planks fan round them, so a bend has neither a wedge gap
    outside nor an overlap inside. An end that stands on the ground (the jetty's foot on the sand)
    is raised until its deck is GROUND_CLEAR + 1 cm over the terrain within 1.5 m of it.

    ⚠️ BRANCHES: a walk whose end lies on a walk built before it (a spur, the east walk) STOPS
    there: its planks start 2.5 cm clear of the other walk's ragged plank ends, and its stringers
    run on only to bite 2 cm into the other walk's stringer. With the cove's BRANCH_DROP (the
    branch's z 3 cm lower) nothing of the branch lies inside or flush with the walk it joins; the
    old strip ran on under the spine and showed through its plank gaps.

    Appends its segments to the cove's _WALK_SEGMENTS (the boats keep clear of them) as walk_path
    did; with register_paths it also joins PATHS (ledge railing gaps, near_structure). Returns
    the root empty."""
    C = _cove(cove)
    height_fn = height_fn or C.height
    zs = [float(zs)] * len(pts) if isinstance(zs, (int, float)) else [float(z) for z in zs]
    # Ends on the ground stand clear of it (the owner's z-fighting rule).
    for end, nxt in ((0, 1), (len(pts) - 1, len(pts) - 2)):
        p = _v2(pts[end])
        d = (_v2(pts[nxt]) - p).normalized()
        nrm = Vector((-d.y, d.x, 0))
        g = max(height_fn(*(p + d * a + nrm * b).xy) for a in (0.0, 0.5, 1.0, 1.5)
                for b in (-width / 2 - 0.07, 0.0, width / 2 + 0.07))
        if g - 0.3 < zs[end] < g + GROUND_CLEAR + 0.01:
            zs[end] = g + GROUND_CLEAR + 0.01
    L = _Line(pts, zs, width)
    h = Kit(label, seed)
    s_a, s_b = 0.0, L.length
    b_a, b_b = 0.0, L.length
    joined = [False, False]
    for oL, ow in WALKS:
        for e in (0, 1):
            s_end = 0.0 if e == 0 else L.length
            p = L.at(s_end)[0]
            if oL.dist(p.x, p.y) > ow / 2 + 0.1:
                continue
            joined[e] = True
            sgn = 1 if e == 0 else -1

            def walk_out(margin):
                s = s_end
                while 0.0 <= s <= L.length and abs(s - s_end) < L.length / 2:
                    q = L.at(s)[0]
                    if oL.dist(q.x, q.y) >= margin:
                        break
                    s += 0.02 * sgn
                return s
            sp = walk_out(ow / 2 + 0.065 + 0.025)
            sb = walk_out(ow / 2 - 0.24 + BEAM_W / 2 - 0.02)
            if e == 0:
                s_a, b_a = max(s_a, sp), max(b_a, sb)
            else:
                s_b, b_b = min(s_b, sp), min(b_b, sb)
    _walk_deck(h, L, s_a, s_b, width, height_fn, beam_s=(b_a, b_b))
    # Bents: a headstock under the stringers and a pile each side, every ~bent_every metres.
    first = (b_a + 0.45) if joined[0] else 0.25
    last = (b_b - 0.45) if joined[1] else L.length - 0.25
    nb = max(1, math.ceil((last - first) / bent_every))
    bents = [first + (last - first) * i / nb + (h.j(0.15) if 0 < i < nb else 0.0) for i in range(nb + 1)]
    for bi, s in enumerate(bents):
        p, tan, nrm = L.at(s)
        str_bot = p.z - PLANK_T + 0.035 - BEAM_D
        cap_top = str_bot + 0.02
        cap_bot = cap_top - 0.18
        feet = []
        for sg in (-1, 1):
            q = p + nrm * (sg * (width / 2 - 0.05))
            feet.append((q, height_fn(q.x, q.y)))
        if max(g for _q, g in feet) > cap_bot - 0.12:
            continue                      # the deck is down on the sand here: the stringers bear on it
        a, b = p - nrm * (width / 2 + 0.14), p + nrm * (width / 2 + 0.14)
        zc = (cap_top + cap_bot) / 2
        member(h, "timber", Vector((a.x, a.y, zc)), Vector((b.x, b.y, zc)), 0.17, cap_top - cap_bot, up=Z)
        for q, g in feet:
            pile(h, Vector((q.x, q.y, g - SINK)), Vector((q.x, q.y, p.z - PLANK_T - 0.02)),
                 0.1 * h.rng.uniform(0.9, 1.1))
        # One diagonal on a tall bent, on its face, alternating direction bent to bent.
        (qa, ga), (qb, gb) = feet if bi % 2 else feet[::-1]
        low = max(gb + 0.35, cap_bot - 1.5)
        if cap_bot - 0.08 - low > 0.8:
            o = tan * 0.13
            member(h, "timber", Vector((qa.x, qa.y, cap_bot - 0.06)) + o, Vector((qb.x, qb.y, low)) + o,
                   0.14, 0.07, up=tan)
    for sd in rails or ():
        sg = 1 if sd == "left" else -1
        off = width / 2 + POST_W / 2 - 0.01
        line, lz = [], []
        for x in [s_a] + [x for x in L.S if s_a + 0.2 < x < s_b - 0.2] + [s_b]:
            q, _t, nrm = L.at(x)
            e = q + nrm * (sg * off)
            line.append((e.x, e.y))
            lz.append(q.z)
        # A post over the sand reaches down into it; over water it hangs from the deck edge.
        _railing_into(h, line, lz, height_fn, bolt=0.34, outward=sg, max_leg=1.3)
    WALKS.append((L, width))
    if hasattr(C, "_WALK_SEGMENTS"):
        C._WALK_SEGMENTS.extend(((pts[i][0], pts[i][1]), (pts[i + 1][0], pts[i + 1][1])) for i in range(len(pts) - 1))
    if register_paths:
        for i in range(len(pts) - 1):
            PATHS.append((Vector((pts[i][0], pts[i][1], zs[i])), Vector((pts[i + 1][0], pts[i + 1][1], zs[i + 1])),
                          width / 2))
    return h.finish(c, L.at(0.0)[0])


# ---------------------------------------------------------------- cliff boardwalks

def cliff_walk(c, pts, z, height_fn, start_z=None, end_z=None, rail="outer", label="cliff walk", seed=0):
    """A LEVEL plank boardwalk along a polyline at deck height z, the way the reference joins its
    houses along the rock face: a MODELLED plank deck on two stringers (_walk_deck), a bearer
    across under them every ~3.2 m, and at each bearer a chunky leg down each side into the
    ground wherever the ground is below the deck (the outer legs run on up as the railing posts,
    so a leg is never a separate stick beside a post). Tall legs get a diagonal brace.

    pts        [(x, y), ...] the centreline
    z          the deck top
    start_z, end_z   floor heights at the two ends: where one differs from z by more than 0.15 m
               the end becomes a real-size sawtooth flight of steps (the last metres of the line),
               so a walk can land on a pocket a metre or two lower or higher.
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
    L = _Line([(q.x, q.y) for q in D2], [z] * len(D2), WALK_W)
    _walk_deck(h, L, 0.0, L.length, WALK_W, height_fn)
    # Which side is OUTER: the side whose ground is lower, summed over the whole walk.
    lower = 0.0
    for k in range(21):
        q, t, nl = L.at(L.length * k / 20)
        nl = nl * 2.5
        lower += height_fn(q.x + nl.x, q.y + nl.y) - height_fn(q.x - nl.x, q.y - nl.y)
    outer = 1 if lower < 0 else -1
    st, _SD = _stations([Vector((q.x, q.y, 0)) for q in L.P], POST_EVERY, h.rng)
    str_bot = z - PLANK_T + 0.035 - BEAM_D
    bear_top = str_bot + 0.02
    bear_bot = bear_top - 0.17
    rail_sides = {"outer": (outer,), "both": (-1, 1)}.get(rail, ())
    rail_runs, cur = [], {-1: [], 1: []}
    prev_legs, nbent = None, 0
    for k, s in enumerate(st):
        q, tan, m = L.at(s)
        # A BENT (bearer and legs) at every other station and both ends; the stations between
        # carry only a rail post hanging from the deck edge. Review v1: a pair of legs every
        # 1.6 m read as a forest of sticks under the walk.
        bent = k % 2 == 0 or k == len(st) - 1
        if bent:
            a = q - m * (WALK_W / 2 + 0.16)
            b = q + m * (WALK_W / 2 + 0.16)
            zc = (bear_top + bear_bot) / 2
            member(h, "timber", Vector((a.x, a.y, zc)), Vector((b.x, b.y, zc)), 0.16, bear_top - bear_bot)
        legs = {}
        for sg in (-1, 1):
            e = q + m * sg * (WALK_W / 2 + POST_W / 2 - 0.01)
            g = height_fn(e.x, e.y)
            beyond = q + m * sg * (WALK_W / 2 + 0.8)
            drop = z - min(g, height_fn(beyond.x, beyond.y))
            railed = sg in rail_sides and drop > 0.5      # a rail only where the ground falls away
            if railed:
                # At a bent the rail post IS the leg, down into the ground; between bents it
                # hangs from the deck edge, below the stringers.
                cur[sg].append((e, g - SINK if bent else z - 0.34, m * sg))
            elif cur[sg]:
                rail_runs.append(cur[sg])
                cur[sg] = []
            if bent and g < bear_bot - 0.05:
                if not railed:
                    lean = Vector((h.j(0.02), h.j(0.02), 0))
                    member(h, "timber", Vector((e.x, e.y, g - SINK)) - lean, Vector((e.x, e.y, bear_top - 0.02)),
                           0.18, 0.18, up=tan, taper=h.rng.uniform(0.82, 0.92), bend=0.03)
                legs[sg] = (e, g)
        if not bent:
            continue
        nbent += 1
        # Tall bents: a diagonal brace across the walk, on the bent's far face.
        if len(legs) == 2 and max(bear_bot - legs[-1][1], bear_bot - legs[1][1]) > 1.4:
            (e0, g0), (e1, g1) = legs[-1], legs[1]
            hi, lo = (e0, e1) if nbent % 2 else (e1, e0)
            glo = g1 if lo is e1 else g0
            off = tan * 0.12
            member(h, "timber", Vector((hi.x, hi.y, bear_bot - 0.1)) + off,
                   Vector((lo.x, lo.y, max(glo + 0.25, bear_bot - 2.4))) + off, 0.14, 0.07, up=tan)
        # ...and along the walk between tall outer legs of neighbouring bents, on their outer face.
        if prev_legs and outer in legs and outer in prev_legs:
            (ea, ga), (eb, gb) = prev_legs[outer], legs[outer]
            if min(bear_bot - ga, bear_bot - gb) > 1.8 and nbent % 2 == 0:
                o = m * outer * 0.12
                member(h, "timber", Vector((ea.x, ea.y, bear_bot - 0.12)) + o,
                       Vector((eb.x, eb.y, max(gb + 0.3, bear_bot - 1.6))) + o, 0.14, 0.07, up=m * outer)
        prev_legs = legs
    rail_runs += [r for r in cur.values() if r]
    # The railing: posts at the outer edge from the ground (or under the stringers) up, rails on
    # the posts' outer face.
    for run in rail_runs:
        if len(run) < 2:
            continue
        tops, dzs, outs = [], [], []
        for idx, (e, foot, o) in enumerate(run):
            nxt = run[min(idx + 1, len(run) - 1)][0] - run[max(idx - 1, 0)][0]
            tops.append(_post(h, e, min(z - 0.34, foot), z + RAIL_TOP + 0.05 + h.j(0.02),
                              nxt.normalized() if nxt.length > 1e-6 else Vector((1, 0, 0))))
            dzs.append(z)
            outs.append(o)
        _rails(h, tops, dzs, outs)
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

    Construction: a bent of two ROUND piles (pile(): tapered, a little bent, domed tops) every
    ~2.5 m under a chunky cap log; three joists along the pier (the edge joists pass THROUGH the
    piles, bolted like a wale); organic planks across it, each its own board with a 2 cm gap.
    Piles at the start, the end and every other bent rise 0.6 m over the deck as BOLLARDS (the
    reference); on the railed edge they rise on as its posts. X bracing between the two piles of
    a tall bent and diagonals along the sides; a chunky ladder into the water at the end;
    sawtooth steps down to the sand at the start. Returns the root empty."""
    h = Kit(label, seed)
    d = Vector((math.cos(heading), math.sin(heading), 0))
    sd = Vector((-d.y, d.x, 0))
    O = _v2(start_xy)

    def gz(p):
        return ground_fn(p.x, p.y) if ground_fn is not None else seabed_z

    def at(s, u):
        return O + d * s + sd * u
    pile_u = width / 2 - 0.1
    joist_top = deck_z - PLANK_T + 0.035         # 3.5 cm up inside the (warped) planks
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
            top_z = deck_z + RAIL_TOP + 0.05 + h.j(0.02) if railed else (
                deck_z + 0.6 + h.j(0.05) if bollard else cap_top + 0.05)
            lean = Vector((h.j(0.03), h.j(0.03), 0))
            foot = Vector((p.x, p.y, g - SINK - 0.2)) - lean
            topv = Vector((p.x, p.y, top_z)) + lean
            pile(h, foot, topv, 0.14 * h.rng.uniform(0.92, 1.08), taper=h.rng.uniform(0.84, 0.92),
                 dome=railed or bollard)
            if railed:
                rail_tops.append((s, topv))
            feet[sg] = (p, g)
        pile_feet.append(feet)
        # The cap log across the bent, out past both piles.
        a, b = at(s, -pile_u - 0.3), at(s, pile_u + 0.3)
        member(h, "timber", Vector((a.x, a.y, (cap_top + cap_bot) / 2)), Vector((b.x, b.y, (cap_top + cap_bot) / 2)),
               0.22, cap_top - cap_bot, up=Z)
        # X bracing between the two piles, one diagonal on each face of the bent.
        (p0, g0), (p1, g1) = feet[-1], feet[1]
        low = max(max(g0, g1) + 0.25, cap_bot - 2.6)
        if cap_bot - 0.1 - low > 0.7:
            for face, (pa, pb) in ((-0.17, (p0, p1)), (0.17, (p1, p0))):
                o = d * face
                member(h, "timber", Vector((pa.x, pa.y, cap_bot - 0.08)) + o, Vector((pb.x, pb.y, low)) + o,
                       0.15, 0.07, up=d)
    # Diagonals along both sides, on the piles' outer faces, alternate bays.
    for bi in range(len(bents) - 1):
        if bi % 2:
            continue
        for sg in (-1, 1):
            (pa, ga), (pb, gb) = pile_feet[bi][sg], pile_feet[bi + 1][sg]
            low = max(max(ga, gb) + 0.3, cap_bot - 2.2)
            if cap_bot - low > 0.7:
                o = sd * sg * 0.17
                member(h, "timber", Vector((pa.x, pa.y, cap_bot - 0.1)) + o, Vector((pb.x, pb.y, low)) + o,
                       0.15, 0.07, up=sd * sg)
    # Joists: both edge joists run through the piles, the middle one under the plank centres.
    for u, w in ((-pile_u, 0.13), (0.0, 0.15), (pile_u, 0.13)):
        a, b = at(0.0, u), at(length, u)
        zc = (joist_top + joist_bot) / 2
        member(h, "timber", Vector((a.x, a.y, zc)), Vector((b.x, b.y, zc)), w, joist_top - joist_bot, up=Z,
               bend=0.012)
    # Planks across, each its own board, 2 cm apart, ends ragged (board() jitters them).
    s = 0.02
    while s < length - 0.1:
        w = min(h.rng.uniform(0.24, 0.3), length - s)
        c_ = at(s + w / 2, 0.0)
        zc = deck_z + h.j(0.004)
        tilt = (Z + d * h.j(0.012)).normalized()
        a = c_ - sd * (width / 2 + 0.04 + h.j(0.04))
        b = c_ + sd * (width / 2 + 0.04 + h.j(0.04))
        board(h, Vector((a.x, a.y, zc)), Vector((b.x, b.y, zc)), w - 0.02, PLANK_T, up=tilt)
        s += w
    # The railing along one edge: the railed piles are its main posts, with a square post
    # between each pair, bolted to the edge joist; rails on the outer face.
    if rail_sg is not None and len(rail_tops) >= 2:
        tops = []
        for (sa, ta), (sb, tb) in zip(rail_tops, rail_tops[1:]):
            tops.append(ta)
            m = at((sa + sb) / 2, rail_sg * pile_u)
            tops.append(_post(h, m, joist_bot - 0.02, deck_z + RAIL_TOP + 0.05 + h.j(0.02), d))
        tops.append(rail_tops[-1][1])
        _rails(h, tops, [deck_z] * len(tops), [sd * rail_sg] * len(tops))
    # A chunky ladder down into the water off the end of the deck.
    top_z, bot_z = deck_z + 0.8, gz(at(length + 0.4, 0.0)) - 0.15      # its feet on the seabed
    stiles = []
    for sg in (-1, 1):
        a = at(length + 0.07, sg * 0.3)
        b = at(length + 0.4, sg * 0.32)
        member(h, "timber", Vector((b.x, b.y, bot_z)), Vector((a.x, a.y, top_z)), 0.09, 0.11, up=d)
        stiles.append((Vector((a.x, a.y, top_z)), Vector((b.x, b.y, bot_z))))
    zr = deck_z - 0.3
    while zr > bot_z + 0.15:
        f = (top_z - zr) / (top_z - bot_z)
        p0, p1 = stiles[0][0].lerp(stiles[0][1], f), stiles[1][0].lerp(stiles[1][1], f)
        member(h, "timber", p0 - (p1 - p0) * 0.12, p1 + (p1 - p0) * 0.12, 0.07, 0.06, up=Z)
        zr -= 0.32
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
    shallows. Built from the same organic members as the pier. `rng` is a random.Random, so the
    lead's seed decides the wreck. Returns the root."""
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
            # A standing pile under a cap ends flat (the cap sits on it); a snapped or free one is
            # worn round.
            pile(h, foot, top, 0.13 * rng.uniform(0.85, 1.05), taper=rng.uniform(0.7, 0.9), dome=not standing)
            tops[sg] = top
        if standing and len(tops) == 2:
            a, b = tops[-1], tops[1]
            ext = (b - a).normalized() * 0.25
            member(h, "timber", a - ext - Z * 0.08, b + ext - Z * 0.08, 0.2, 0.2, up=Z)
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
            member(h, "timber", a, b, 0.13, 0.2, up=Z)
            joist_z[(bi, k)] = (a, b)
    # One joist fallen off the last intact cap, its far end on the sand.
    if js:
        last = caps[js[-1]]
        a = last[0].lerp(last[1], rng.uniform(0.2, 0.8)) + Z * 0.1
        far = a + d * rng.uniform(2.5, 3.5) + sd * rng.uniform(-0.6, 0.6)
        far.z = ground_fn(far.x, far.y) + 0.08
        member(h, "timber", a, far, 0.13, 0.2, up=Z)
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
            # Joist tops are 0.1 over these centre lines: a plank TOP 0.155 up puts its underside
            # (0.08 thick give or take, warped up to 8 mm) at least 1 cm down into them.
            L, R = la.lerp(lb, f) + Z * 0.155, ra.lerp(rb, f) + Z * 0.155
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
                board(h, L, R, w - 0.02, PLANK_T, up=up)
            else:
                skew = d * rng.uniform(-0.05, 0.05)
                board(h, L + skew, R - skew, w - 0.02, PLANK_T, up=(Z + d * rng.uniform(-0.03, 0.03)).normalized())
    # Loose planks half buried in the sand around the wreck: one end 10 cm up, the other sunk,
    # tilted, so a flat board never lies flush with the sand.
    for _ in range(rng.randint(3, 5)):
        c_ = at(rng.uniform(0.5, length), rng.uniform(-width, width))
        ang = rng.uniform(0, math.tau)
        u = Vector((math.cos(ang), math.sin(ang), 0))
        L = rng.uniform(1.4, 2.2)
        a = c_ - u * L / 2
        b = c_ + u * L / 2
        a.z, b.z = ground_fn(a.x, a.y) + 0.1, ground_fn(b.x, b.y) - 0.05
        board(h, a, b, rng.uniform(0.24, 0.3), PLANK_T,
              up=(Z + Vector((rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1), 0))).normalized())
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
