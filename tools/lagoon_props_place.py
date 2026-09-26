"""Lagoon Court: PLACE the prop kits in the cove (docs/LAGOON_REWORK_GUIDE.md, CURRENT STATE, "Next
for the lead"; gap review § 7a item 1: the reference is full of nets, crates, barrels, baskets and
lanterns, and our cove had none).

Called by tools/author_lagoon_cove.py after the planting and its burying pass:

    import lagoon_props_place as PP
    PP.place_props(L.col("Props", root), cove=<the cove module>, plants=plant_col)

HOW A PROP IS PLACED
  * Every (kind, seed) is built ONCE by its kit's `build_prop` into the hidden, excluded
    "Props kit (source, not placed)" collection, and every placement is a LINKED DUPLICATE of it
    (objects copied, meshes shared), the way the house and boat kits are placed.
  * A ground prop is set on the LOWEST ground under its footprint: the kits sink their own feet
    1 to 5 cm, so the lowest corner keeps every corner in contact and none floating. A footprint
    whose corners differ by more than MAX_TILT metres is refused (a rack on a slope reads broken).
  * It is refused if any footprint point is in the water, inside the court's play walls, near a
    walk, stair or pier (`S.near_structure`), within SPACING of another prop, or if a ray cast
    down from 2.4 m over any footprint point hits anything that is not the ground (a boulder, a
    house, a boat, a railing) above it. Candidates are tried in order and the first that passes
    is kept, so a cluster bends around what is there instead of clipping it.
  * Mounted props (a hanging lantern under an eave, a wall sign) are placed by ray casts against
    the building they hang on (the village kit's integration notes).
  * Plants standing inside a placed prop's footprint are removed afterwards: placing the props
    before the planting would change the planting's random stream and shift every plant, and the
    owner's recorded hand edits match plants by their built position.
  * ⚠️ Its own random stream (Random(41)): consuming the cove's would move everything built later.

CLUSTERS (the guide's list):
  beached bangkas on the spit   net spread or rack, oars, anchor stone, rope, bubo traps
  the pier's foot               net pile, rope coil, crates, barrel, baskets, a hanging net
  the pier's deck               crates, a barrel, a water drum near its far end
  the beach west of the court   fish racks with baskets and bubo traps, a banig with shells
  the land houses               a lantern post, pots by the steps, a hanging lantern under the
                                eave, and one yard piece (laundry, firewood, drum, banig, bench)
  the stall                     its sari-sari wall sign, crates, a drum, a barrel
  the court's edge              a table with benches and a lean-to, outside the play walls
  along the sand                driftwood
"""
import importlib
import math
import random

import bpy
from mathutils import Matrix, Vector

KIT_OF = {
    "net_pile": "author_lagoon_props_beach", "net_spread": "author_lagoon_props_beach",
    "net_rack": "author_lagoon_props_beach", "rope_coil": "author_lagoon_props_beach",
    "sign_hanging": "author_lagoon_props_village", "lantern_post": "author_lagoon_props_village",
    "lantern_hang": "author_lagoon_props_village",
    "table_round": "lagoon_prop_seating", "bench": "lagoon_prop_seating", "lean_to": "lagoon_prop_seating",
    "banga": "lagoon_prop_clay", "pot_cluster": "lagoon_prop_clay", "potted_plant": "lagoon_prop_clay",
    "fish_rack": "lagoon_prop_fishing", "bubo": "lagoon_prop_fishing", "basket": "lagoon_prop_fishing",
    "crate": "lagoon_prop_cargo", "barrel": "lagoon_prop_cargo", "water_drum": "lagoon_prop_cargo",
    "oar_pair": "lagoon_prop_shore", "driftwood": "lagoon_prop_shore", "anchor_stone": "lagoon_prop_shore",
    "firewood": "lagoon_prop_shore",
    "woven_mat": "lagoon_prop_textile", "hanging_net": "lagoon_prop_textile", "laundry_line": "lagoon_prop_textile",
}
# Seeds that give a GROUND variant, per kind (odd/even variants differ in some kits: an even
# hanging_net is a wall net, an odd sign_hanging a wall sign; those are mounted, not listed here).
GROUND_SEEDS = {
    "net_pile": (1, 2, 3), "net_spread": (1, 2, 3), "net_rack": (1, 2), "rope_coil": (1, 2, 3),
    "lantern_post": (1, 2, 3, 4), "table_round": (1, 2), "bench": (1, 2, 3), "lean_to": (1, 2, 3),
    "banga": (1, 2, 3), "pot_cluster": (1, 2, 3), "potted_plant": (1, 2, 3, 4),
    "fish_rack": (1, 2, 3), "bubo": (1, 2, 3), "basket": (1, 2, 3),
    "crate": (1, 2, 3, 4, 5), "barrel": (1, 2, 3, 4), "water_drum": (1, 2, 3, 4, 5),
    "oar_pair": (1, 2, 3, 4), "driftwood": (1, 2, 3, 4), "anchor_stone": (1, 2), "firewood": (1, 2, 3),
    "woven_mat": (1, 2, 3), "hanging_net": (1, 3), "laundry_line": (1, 2, 3),
}
SARI_SARI_SIGN = 11          # the village kit's wall-bracket sari-sari sign (its notes: on a side wall)
MAX_TILT = 0.35              # metres of ground difference allowed across a footprint
# 0.15 (was 0.35): cove v52's piles read as scattered pieces with sand between them, where the
# reference packs its gear against itself.
SPACING = 0.15               # metres between two props' footprint circles
# Low plants give way to a prop (clear_plants removes them afterwards); palms and stones do not.
SOFT_PLANTS = ("tuft", "groundcover", "flower", "croton", "monstera", "broadleaf")
CLEAR_RAY = 2.4              # a ray from this high over the ground must reach the ground first

_SOURCES = {}
REFUSED = {}                 # why candidates were refused, for the build log
PLACED = []                  # (x, y, z, radius, kind) of every prop placed this build


def _holder():
    h = bpy.data.collections.get("Props kit (source, not placed)")
    if h is None:
        h = bpy.data.collections.new("Props kit (source, not placed)")
        bpy.context.scene.collection.children.link(h)
        bpy.context.view_layer.layer_collection.children[h.name].exclude = True
    return h


def source(kind, seed):
    key = (kind, seed)
    if key not in _SOURCES:
        kit = importlib.import_module(KIT_OF[kind])
        col = kit.build_prop(kind, seed)
        _holder().children.link(col)
        root = next(o for o in col.all_objects if o.parent is None)
        _SOURCES[key] = (col, root)
    return _SOURCES[key]


def box_of(kind, seed):
    """The prop's local footprint (x0, y0, x1, y1) and height, from its root's prop_box when the
    kit wrote one, else from its meshes' bounds."""
    col, root = source(kind, seed)
    if "prop_box" in root.keys():
        b = list(root["prop_box"])
        return (b[0], b[1], b[3], b[4]), b[5] - b[2]
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for o in col.all_objects:
        if o.type != "MESH":
            continue
        for c in o.bound_box:
            w = root.matrix_world.inverted() @ o.matrix_world @ Vector(c)
            lo, hi = Vector(map(min, lo, w)), Vector(map(max, hi, w))
    return (lo.x, lo.y, hi.x, hi.y), hi.z - lo.z


def duplicate(c, kind, seed, x, y, z, rz):
    """A linked duplicate of (kind, seed) with its root at (x, y, z), turned rz about Z."""
    col, root = source(kind, seed)
    copies = {}
    for o in col.all_objects:
        d = o.copy()
        c.objects.link(d)
        copies[o] = d
    for o, d in copies.items():
        if o.parent in copies:
            d.parent = copies[o.parent]
            d.matrix_parent_inverse = o.matrix_parent_inverse.copy()
    new_root = copies[root]
    new_root.matrix_world = Matrix.Translation((x, y, z)) @ Matrix.Rotation(rz, 4, "Z") @ root.matrix_world
    return new_root


class Placer:
    def __init__(self, c, cove):
        self.c, self.cove, self.S = c, cove, cove.S
        self.scene = bpy.context.scene
        bpy.context.view_layer.update()
        self.depsgraph = bpy.context.evaluated_depsgraph_get()
        self.count = {}

    # ------------------------------------------------------------ tests

    def _footprint(self, kind, seed, x, y, rz, shrink=0.85):
        (x0, y0, x1, y1), _h = box_of(kind, seed)
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        pts = [(cx, cy)] + [(cx + (px - cx) * shrink, cy + (py - cy) * shrink)
                            for px, py in ((x0, y0), (x1, y0), (x1, y1), (x0, y1))]
        cs, sn = math.cos(rz), math.sin(rz)
        world = [(x + px * cs - py * sn, y + px * sn + py * cs) for px, py in pts]
        radius = 0.5 * math.hypot(x1 - x0, y1 - y0)
        return world, radius

    def _ray_down(self, px, py, top, dist):
        return self.scene.ray_cast(self.depsgraph, Vector((px, py, top)), Vector((0, 0, -1)), distance=dist)

    def ground_z(self, kind, seed, x, y, rz, allow_court=False, keep_off=0.5):
        """The z to set a ground prop at, or None when the spot is refused (see the module notes)."""
        C = self.cove
        pts, radius = self._footprint(kind, seed, x, y, rz)
        zs = []
        def refuse(why):
            REFUSED[why] = REFUSED.get(why, 0) + 1
            return None
        for px, py in pts:
            if C.coast_distance(px, py) < 0.8:
                return refuse("water")                        # in (or at the edge of) the water
            wall = 13.6 if allow_court else 15.0              # the play walls stand at +-13
            if abs(px) < wall and abs(py) < wall:
                return refuse("court")
            if self.S.near_structure(px, py, keep_off):
                return refuse("structure")
            zs.append(C.height(px, py))
        if max(zs) - min(zs) > MAX_TILT:
            return refuse("tilt")
        if any(math.hypot(x - q[0], y - q[1]) < radius + q[3] + SPACING for q in PLACED):
            return refuse("spacing")
        # The clearance rays use the FULL outline plus 5 % (owner's screenshot of the stall: a barrel
        # on its side touched the stall's front step, which stood just outside the 85 % outline).
        full, _r = self._footprint(kind, seed, x, y, rz, shrink=1.05)
        for px, py in full:
            gz = C.height(px, py)
            hit, loc, _n, _i, obj, _m = self._ray_down(px, py, gz + CLEAR_RAY, CLEAR_RAY + 0.5)
            if hit and not obj.name.startswith("ground") and not obj.name.startswith(SOFT_PLANTS)                     and loc.z > gz + 0.03:
                return refuse("blocked by " + obj.name.split(".")[0][:18])   # a stone, a house, a boat
        return min(zs)

    def deck_z(self, kind, seed, x, y, rz, top):
        """The z of a flat DECK under the whole footprint (a porch, a walk), casting down from
        `top`; None unless every footprint point meets a level face within 6 cm of the others."""
        # The FULL outline plus 5 % (a ground prop's test shrinks it 15 %): on a porch a railing's
        # rail stands at the deck's edge, and a piece overhanging the deck clipped through it
        # (props_water_home1_v6, a leaning bilao through the bamboo rail).
        pts, radius = self._footprint(kind, seed, x, y, rz, shrink=1.05)
        zs = []
        for px, py in pts:
            hit, loc, n, _i, obj, _m = self._ray_down(px, py, top, 2.0)
            if not hit or n.z < 0.9 or obj.name.startswith(SOFT_PLANTS):
                REFUSED["off deck"] = REFUSED.get("off deck", 0) + 1
                return None
            zs.append(loc.z)
        if max(zs) - min(zs) > 0.06:
            REFUSED["deck uneven"] = REFUSED.get("deck uneven", 0) + 1
            return None
        if any(math.hypot(x - q[0], y - q[1]) < radius + q[3] + SPACING for q in PLACED):
            REFUSED["spacing"] = REFUSED.get("spacing", 0) + 1
            return None
        return max(zs)

    def put(self, kind, seed, x, y, z, rz):
        root = duplicate(self.c, kind, seed, x, y, z, rz)
        _pts, radius = self._footprint(kind, seed, x, y, rz)
        PLACED.append((x, y, z, radius, kind))
        self.count[kind] = self.count.get(kind, 0) + 1
        return root

    def try_ground(self, kind, seed, candidates, **kw):
        """Place (kind, seed) at the first candidate (x, y, rz) that passes; returns its root."""
        for x, y, rz in candidates:
            z = self.ground_z(kind, seed, x, y, rz, **kw)
            if z is not None:
                return self.put(kind, seed, x, y, z, rz)
        return None


# ------------------------------------------------------------------ helpers

def _face(x, y, tx, ty):
    """rz that turns a prop's +Y front from (x, y) toward (tx, ty)."""
    return math.atan2(ty - y, tx - x) - math.pi / 2


def _ring(cx, cy, r0, r1, rng, n=14, face=None):
    """Candidates round (cx, cy) between radii r0 and r1, nearest first, each facing `face`
    (a point) or a random heading."""
    out = []
    for k in range(n):
        r = r0 + (r1 - r0) * k / max(1, n - 1)
        a = rng.uniform(0, math.tau)
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r
        rz = _face(x, y, *face) if face else rng.uniform(0, math.tau)
        out.append((x, y, rz))
    return out


def cluster_piece(P, rng, kind, anchor, face=None, r0=0.0, r1=3.2, **kw):
    """One piece of a tight pile round `anchor`: candidates nearest first, so each piece packs in
    against the ones already down (SPACING keeps them from overlapping)."""
    cands = []
    for k in range(18):
        r = r0 + (r1 - r0) * (k / 17) ** 0.8
        a = rng.uniform(0, math.tau)
        x, y = anchor.x + math.cos(a) * r, anchor.y + math.sin(a) * r
        rz = (_face(x, y, *face) + rng.uniform(-0.5, 0.5)) if face else rng.uniform(0, math.tau)
        cands.append((x, y, rz))
    return P.try_ground(kind, rng.choice(GROUND_SEEDS[kind]), cands, **kw)


def _roots(c_names, key, value):
    for col_name in c_names:
        col = bpy.data.collections.get(col_name)
        if col is None:
            continue
        for o in col.all_objects:
            if o.parent is None and o.get(key) == value:
                yield o


def _eave_point(P, root, fwd, side, ground):
    """Where a lantern hangs under a house's front eave: cast UP from 0.3 m over the ground at
    growing distances in front of the house; the farthest cast that still meets the roof's
    underside (a downward face, 2.3 to 6 m up) is the eave edge. Returns (point, z) or None."""
    best = None
    for k in range(4, 40):
        d = 0.25 * k
        p = root.matrix_world.translation + fwd * d + side
        o = Vector((p.x, p.y, ground + 0.3))
        hit, loc, n, _i, obj, _m = P.scene.ray_cast(P.depsgraph, o, Vector((0, 0, 1)), distance=7.0)
        if hit and n.z < -0.3 and ground + 2.3 < loc.z < ground + 6.0:
            best = (Vector((p.x, p.y, 0)), loc.z, d)
    if best is None:
        return None
    p, z, d = best
    p = p - fwd * 0.45                                          # tucked a hand under the eave edge
    hit, loc, n, _i, _o, _m = P.scene.ray_cast(P.depsgraph, Vector((p.x, p.y, ground + 0.3)),
                                               Vector((0, 0, 1)), distance=7.0)
    return (p, loc.z) if hit and n.z < -0.3 else (Vector((best[0].x, best[0].y, 0)), z)


# ------------------------------------------------------------------ the clusters

def beached_boat_clusters(P, rng):
    for root in list(_roots(["Village"], "boat_kind", "bangka_beached"))[:4]:
        m = root.matrix_world
        bow = (m.to_3x3() @ Vector((0, 1, 0))).normalized()     # bow to the sea
        side = Vector((-bow.y, bow.x, 0))
        at = m.translation
        s = rng.choice((-1, 1))
        lay = [("net_spread", side * s * 3.4 - bow * 0.8), ("oar_pair", -side * s * 3.2 + bow * 0.4),
               ("anchor_stone", bow * 3.6 + side * s * 1.2), ("rope_coil", side * s * 2.6 - bow * 3.0),
               ("bubo", -side * s * 3.0 - bow * 2.4), ("basket", -side * s * 3.6 - bow * 1.0),
               ("net_rack", side * s * 4.2 - bow * 4.5), ("crate", -side * s * 2.8 - bow * 3.6)]
        for kind, off in lay:
            seed = rng.choice(GROUND_SEEDS[kind])
            cands = [(at.x + off.x + rng.uniform(-0.8, 0.8), at.y + off.y + rng.uniform(-0.8, 0.8),
                      math.atan2(bow.y, bow.x) + rng.uniform(-0.6, 0.6)) for _ in range(10)]
            P.try_ground(kind, seed, cands)


def pier_clusters(P, rng):
    sug = P.S.suggest_pier()["pier"]
    (sx, sy), head = sug["start_xy"], sug["heading"]
    out = Vector((math.cos(head), math.sin(head), 0))           # along the pier, to sea
    side = Vector((-out.y, out.x, 0))
    # Two tight piles on the sand either side of the pier's start (v1 scattered them over 6 m, which
    # read as litter, not as a working jetty): nets and rope on one side, cargo on the other.
    # v2 of the placement still read sparse from the reference angle: seven pieces a pile, packed
    # within 2.4 m.
    for sd, kinds in ((1, ("net_pile", "rope_coil", "basket", "bubo", "basket", "hanging_net", "oar_pair")),
                      (-1, ("crate", "barrel", "crate", "water_drum", "crate", "barrel", "fish_rack"))):
        anchor = Vector((sx, sy, 0)) + side * sd * 3.2 - out * 1.5
        for kind in kinds:
            cluster_piece(P, rng, kind, anchor, face=(sx + out.x * 8, sy + out.y * 8), r1=2.4)
    # On the deck, toward the far end: cast down on the deck itself.
    length = sug["length"]
    for kind, t, s in (("crate", 0.72, -1), ("barrel", 0.8, -1), ("water_drum", 0.86, 1), ("crate", 0.55, 1)):
        seed = rng.choice(GROUND_SEEDS[kind])
        p = Vector((sx, sy, 0)) + out * length * t + side * s * 0.55
        hit, loc, n, _i, obj, _m = P._ray_down(p.x, p.y, sug["deck_z"] + 3.0, 4.0)
        if not hit or n.z < 0.9:
            continue
        if any(math.hypot(p.x - q[0], p.y - q[1]) < 0.9 for q in PLACED):
            continue
        P.put(kind, seed, p.x, p.y, loc.z, math.atan2(out.y, out.x) + rng.uniform(-0.3, 0.3))


def beach_drying(P, rng):
    """Fish racks with baskets and traps on the sand west of the court (the court's own beach
    front, within 22 m of (0, -12), stays open as the planting keeps it)."""
    C = P.cove
    n = len(C.COAST_LINE)
    spots = 0
    for i in rng.sample(range(n), n):
        ax, ay = C.COAST_LINE[i]
        if not (-58 < ax < -20 and -60 < ay < -14):
            continue
        bx, by = C.COAST_LINE[(i + 1) % n]
        nx, ny = -(by - ay), bx - ax
        if C.coast_distance(ax + nx * 0.1, ay + ny * 0.1) < 0:
            nx, ny = -nx, -ny
        ln = math.hypot(nx, ny)
        nx, ny = nx / ln, ny / ln
        x, y = ax + nx * rng.uniform(4.0, 6.5), ay + ny * rng.uniform(4.0, 6.5)
        if math.hypot(x, y + 12) < 22 or any(math.hypot(x - q[0], y - q[1]) < 9 for q in PLACED):
            continue
        rz = math.atan2(ny, nx) - math.pi / 2 + rng.uniform(-0.3, 0.3)
        rack = P.try_ground("fish_rack", rng.choice(GROUND_SEEDS["fish_rack"]),
                            [(x + rng.uniform(-1, 1), y + rng.uniform(-1, 1), rz) for _ in range(6)])
        if rack is None:
            continue
        rx, ry = rack.matrix_world.translation.xy
        for kind in ("basket", "bubo", "woven_mat" if spots % 2 else "basket", "crate", "basket", "net_pile"):
            cluster_piece(P, rng, kind, Vector((rx, ry, 0)), r0=1.2, r1=3.4)
        spots += 1
        if spots >= 4:
            break


def houses(P, rng):
    yard = ["laundry_line", "firewood", "water_drum", "woven_mat", "bench", "laundry_line", "firewood",
            "water_drum", "pot_cluster"]
    rng.shuffle(yard)
    for i, root in enumerate(_roots(["Village"], "house_kind", "land")):
        m = root.matrix_world
        at = m.translation
        fwd = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
        side = Vector((-fwd.y, fwd.x, 0))
        ground = P.cove.height(at.x, at.y)
        facing = rz0 = math.atan2(fwd.y, fwd.x) - math.pi / 2
        s = rng.choice((-1, 1))
        # A lantern post at a front corner of the yard.
        P.try_ground("lantern_post", rng.choice(GROUND_SEEDS["lantern_post"]),
                     [((at + fwd * f + side * s * l).x, (at + fwd * f + side * s * l).y, rz0)
                      for f in (4.5, 5.5, 3.8, 6.5) for l in (3.0, 3.8, 2.4)])
        # Pots by the steps.
        kind = rng.choice(("potted_plant", "banga", "pot_cluster"))
        P.try_ground(kind, rng.choice(GROUND_SEEDS[kind]),
                     [((at + fwd * f - side * s * l).x, (at + fwd * f - side * s * l).y, rz0 + rng.uniform(-0.5, 0.5))
                      for f in (3.4, 4.2, 5.0) for l in (1.4, 2.0, 2.6)])
        # Two yard pieces beside the house (v2 had one, and the pockets read empty).
        for kind in (yard[(2 * i) % len(yard)], yard[(2 * i + 1) % len(yard)]):
            _yard_piece(P, rng, kind, at, fwd, side, s, facing)
        # A capiz lantern hung under the front eave.
        eave = _eave_point(P, root, fwd, side * s * 1.2, ground)
        if eave is not None:
            p, z = eave
            P.put("lantern_hang", 1 + 2 * rng.randrange(2), p.x, p.y, z, facing)


def _yard_piece(P, rng, kind, at, fwd, side, s, facing):
    """One yard piece beside a house, nearest spot first; a laundry line runs front to back."""
    cands = []
    for f in (0.0, 1.5, -1.5, 3.0):
        for l in (5.0, 6.0, 4.2, 7.0):
            for sd in (s, -s):
                p = at + fwd * f + side * sd * l
                cands.append((p.x, p.y, facing + (math.pi / 2 if kind == "laundry_line" else rng.uniform(-0.4, 0.4))))
    P.try_ground(kind, rng.choice(GROUND_SEEDS[kind]), cands)


def water_homes(P, rng):
    """The Bajau homes' porches (cove v52: the whole water village carried no gear at all; the
    owner's photograph shows drums, basins, baskets and nets on every platform). The kit lifts a
    floor about 1.9 m over the water; the porch is found by casting down from just under the eaves,
    and each piece must sit flat on it."""
    kinds = ("water_drum", "basket", "crate", "banga", "pot_cluster", "net_pile", "bubo", "barrel")
    for root in _roots(["Village"], "house_kind", "water"):
        m = root.matrix_world
        at = m.translation
        fwd = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
        side = Vector((-fwd.y, fwd.x, 0))
        hit, loc, n, _i, _o, _m = P._ray_down(at.x, at.y, at.z + 3.0, 3.5)
        if not hit:
            continue
        floor = loc.z
        for kind in rng.sample(kinds, 3):
            seed = rng.choice(GROUND_SEEDS[kind])
            done = False
            for f in (2.2, 2.8, 1.6, 3.4, 4.0):
                for l in (1.1, -1.1, 0.5, -0.5, 1.6, -1.6):
                    p = at + fwd * f + side * l
                    if not _outside(P, at, p, floor):
                        continue                     # inside the room (v6 put a crate and a drum there)
                    rz = math.atan2(fwd.y, fwd.x) - math.pi / 2 + rng.uniform(-0.6, 0.6)
                    z = P.deck_z(kind, seed, p.x, p.y, rz, floor + 1.2)
                    if z is not None and abs(z - floor) < 0.3:
                        P.put(kind, seed, p.x, p.y, z, rz)
                        done = True
                        break
                if done:
                    break
        eave = _eave_point(P, root, fwd, side * rng.choice((-1.0, 1.0)), floor)
        if eave is not None and rng.random() < 0.7:
            p, z = eave
            P.put("lantern_hang", 1 + 2 * rng.randrange(2), p.x, p.y, z, math.atan2(fwd.y, fwd.x) - math.pi / 2)


def _outside(P, centre, p, floor):
    """True when a level ray at waist height from the house's middle toward p meets a wall first."""
    o = Vector((centre.x, centre.y, floor + 1.0))
    d = Vector((p.x - centre.x, p.y - centre.y, 0))
    if d.length < 1e-3:
        return False
    hit, loc, _n, _i, _o, _m = P.scene.ray_cast(P.depsgraph, o, d.normalized(), distance=d.length)
    return hit


def stair_feet(P, rng):
    """Pots and a jar either side of each stair's foot (the reference dresses every flight's foot)."""
    for ea, eb in list(getattr(P.cove, "_STAIR_SEGMENTS", [])):
        if abs(ea.z - eb.z) < 1.5:
            continue                                    # a walk segment, not a flight
        foot, top = (ea, eb) if ea.z < eb.z else (eb, ea)
        d = Vector((top.x - foot.x, top.y - foot.y, 0))
        if d.length < 1e-3:
            continue
        d.normalize()
        side = Vector((-d.y, d.x, 0))
        for sgn in (1, -1):
            if rng.random() < 0.3:
                continue
            kind = rng.choice(("potted_plant", "banga", "pot_cluster", "potted_plant"))
            cands = [((foot - d * b + side * sgn * l).x, (foot - d * b + side * sgn * l).y, rng.uniform(0, math.tau))
                     for b in (0.8, 1.4, 0.3, 2.0) for l in (1.3, 1.7, 2.2)]
            P.try_ground(kind, rng.choice(GROUND_SEEDS[kind]), cands, allow_court=True, keep_off=0.2)


def stall(P, rng):
    root = next(_roots(["Village"], "house_kind", "stall"), None)
    if root is None:
        return
    m = root.matrix_world
    at = m.translation
    fwd = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
    side = Vector((-fwd.y, fwd.x, 0))
    ground = P.cove.height(at.x, at.y)
    # The sari-sari sign on the side wall that faces away from the court (the kit's note: a hung
    # sign over the counter collides with the propped awning). Cast in toward the wall.
    for sgn in (1, -1):
        o = at + side * sgn * 6.0 + fwd * 0.6
        o.z = ground + 1.9
        hit, loc, n, _i, obj, _m = P.scene.ray_cast(P.depsgraph, o, -side * sgn, distance=6.0)
        if hit and abs(n.z) < 0.3:
            P.put("sign_hanging", SARI_SARI_SIGN, loc.x, loc.y, loc.z, math.atan2(n.y, n.x) - math.pi / 2)
            break
    for kind in ("crate", "crate", "water_drum", "barrel"):
        cands = []
        for _ in range(16):
            sd = rng.choice((-1, 1))
            p = at + side * sd * rng.uniform(2.6, 4.5) + fwd * rng.uniform(-2.0, 2.5)
            cands.append((p.x, p.y, rng.uniform(0, math.tau)))
        P.try_ground(kind, rng.choice(GROUND_SEEDS[kind]), cands, allow_court=True)


def court_edge(P, rng):
    """A table with its benches and a lean-to on the court's edge, just outside the play walls
    (the walls stand at +-13 m; props start at 15.5), facing the play."""
    spots = [(-17.0, 3.0), (-17.0, -4.0), (-16.5, 9.5), (16.5, -3.5), (8.5, 16.0), (-9.0, 15.8)]
    rng.shuffle(spots)
    placed = 0
    for kind in ("table_round", "lean_to", "bench", "table_round"):
        for (x, y) in list(spots):
            cands = [(x + rng.uniform(-1, 1), y + rng.uniform(-1, 1), _face(x, y, 0, 1)) for _ in range(6)]
            if P.try_ground(kind, rng.choice(GROUND_SEEDS[kind]), cands, allow_court=True) is not None:
                spots.remove((x, y))
                placed += 1
                break


def driftwood(P, rng, n=10):
    C = P.cove
    m = len(C.COAST_LINE)
    done = 0
    for i in rng.sample(range(m), m):
        if done >= n:
            break
        ax, ay = C.COAST_LINE[i]
        bx, by = C.COAST_LINE[(i + 1) % m]
        nx, ny = -(by - ay), bx - ax
        if C.coast_distance(ax + nx * 0.1, ay + ny * 0.1) < 0:
            nx, ny = -nx, -ny
        ln = math.hypot(nx, ny)
        nx, ny = nx / ln, ny / ln
        x, y = ax + nx * rng.uniform(2.0, 4.5), ay + ny * rng.uniform(2.0, 4.5)
        if math.hypot(x, y + 12) < 22:
            continue
        cands = [(x + rng.uniform(-1.5, 1.5), y + rng.uniform(-1.5, 1.5), math.atan2(ny, nx) + rng.uniform(-1.2, 1.2))
                 for _ in range(6)]
        if P.try_ground("driftwood", rng.choice(GROUND_SEEDS["driftwood"]), cands) is not None:
            done += 1


def clear_plants(plants):
    """Remove plants standing inside a placed prop's footprint (see the module notes)."""
    gone = 0
    if plants is None:
        return gone
    for o in list(plants.all_objects):
        p = o.matrix_world.translation
        for x, y, z, r, _k in PLACED:
            if math.hypot(p.x - x, p.y - y) < r * 0.9 + 0.3 and abs(p.z - z) < 2.0:
                bpy.data.objects.remove(o, do_unlink=True)
                gone += 1
                break
    return gone


STEPS = None


def place_props(c, cove, plants=None):
    PLACED.clear()
    REFUSED.clear()
    rng = random.Random(41)
    P = Placer(c, cove)
    for step in STEPS:
        step(P, rng)
    gone = clear_plants(plants)
    print("[lagoon-props] placed", sum(P.count.values()), dict(sorted(P.count.items())),
          "| plants cleared:", gone, "| refused:", dict(sorted(REFUSED.items(), key=lambda kv: -kv[1])[:12]))
    return P.count


STEPS = (pier_clusters, beached_boat_clusters, stall, court_edge, houses, water_homes, stair_feet,
         beach_drying, driftwood)
