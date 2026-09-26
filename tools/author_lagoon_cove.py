"""Lagoon Court, second blockout: an ORGANIC boulder cove built the way the reference is built.

  blender -b --python tools/author_lagoon_cove.py -- --preview N

Writes ArtSource/lagoon/lagoon_cove.blend and Logs/lagoon-blender/cove_<shot>_vN.png.

Owner on blockout v5: "i want the mountain not to just be one spiky mountain peak", "notice how
the houses are not just on a single level", "experiment more on something organic until it looks
similar to the reference", "the rocks wont be just like that correct? and actually be more
unique?". What the reference (docs/LAGOON_REWORK_GUIDE.md § 1, § 4) actually does, and so what
this builds:

  * THE MASSIF IS A PILE OF BOULDERS. A height field with several peaks, ridges and a saddle
    (not one cone) is the underlying fill, and it is covered in hundreds of placed boulders,
    bigger low down, smaller up high and on the ledges' rims.
  * THE BOULDERS ARE PILLOW STONES, not crystals: a subdivided cube pulled most of the way to a
    sphere, stretched taller than wide, chopped by a few random planes into broad flat faces,
    softly jittered. Twelve unique shapes, shared as linked meshes and each placed at its own
    size, stretch and turn, so no two neighbours read alike.
  * EVERY HOUSE HAS ITS OWN POCKET: a small flat ledge at its own height (beach, low, mid, high),
    cut into the height field, fenced on the drop side, joined to the next by a stair. Nine
    heights, not three shelves.
  * PLANTS GROW IN THE GAPS: palms and bushes where a boulder meets a pocket or the beach.
  * The court is still the biggest clearing (z = 0, 28 x 26), rock round three sides, open south
    down to the beach and the lagoon tongue.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector, noise

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_kanto_blockout as B      # noqa: E402  box, cylinder, blob, label, _obj, lighting
import author_lagoon_blockout as L     # noqa: E402  colours, gameplay markers, horizon, col

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "lagoon"
PREVIEWS = ROOT / "Logs" / "lagoon-blender"
BEACH, WATER, SEABED = -1.2, -1.8, -3.5

B.COLOURS.update({
    "rock": (0.62, 0.46, 0.28), "rock_light": (0.74, 0.60, 0.40), "rock_fill": (0.36, 0.27, 0.17),
    "grass": (0.38, 0.62, 0.18), "sand": (0.95, 0.80, 0.52), "deck": (0.62, 0.38, 0.20),
})

# OWNER, review of cove v4: "could the main beach area be more organic?", "how absurdly
# symmetrical the map is", "i'd like to free up some more space. we need space for boats and
# free-standing stilt houses because badjao tribe isnt particularly land based". So the land is
# now ONE asymmetric island weighted north-west, with a long sand spit curling south on the west,
# rock falling straight into the sea on the east, and the whole south and south-east left as open
# water for the Sama-Bajau style village (docs/LAGOON_REWORK_GUIDE.md).

# The coast, clockwise from the north, as control points of a closed smooth curve. Asymmetric on
# purpose: nothing here mirrors across x = 0.
COAST = [(-24, 112), (26, 108), (58, 86), (66, 52), (54, 30), (44, 12), (33, -6), (20, -19), (6, -25),
         (-8, -23), (-19, -19), (-28, -27), (-36, -45), (-42, -66), (-52, -86), (-63, -95), (-73, -86),
         (-79, -58), (-88, -24), (-96, 12), (-90, 52), (-66, 88)]
BEACH_BAND = 8.0   # land within this distance of the coast is beach sand

# POCKETS: (name, x, y, floor z, radius x, radius y, surface). Each is a flat ledge the height
# field is cut down (or built up) to. The court is the biggest; houses get the rest, at many
# heights and never in mirrored pairs.
POCKETS = [
    ("court", 0, 1, 0.0, 19, 17, "court"),
    ("low west", -40, -6, 2.5, 7, 6, "grass"),
    ("low north-east", 27, 18, 3.0, 7.5, 6, "grass"),
    ("mid north-west", -30, 30, 7.0, 8, 6, "grass"),
    ("mid north", 3, 33, 5.0, 9, 5.5, "grass"),
    ("mid east", 46, 42, 9.5, 6.5, 5.5, "grass"),
    ("west shoulder", -70, 6, 6.0, 7, 6, "grass"),
    ("high west", -52, 42, 13.5, 6.5, 5.5, "grass"),
    ("high north", -12, 54, 14.5, 7, 5, "grass"),
    ("high east", 28, 62, 18.5, 6.5, 5, "grass"),
    ("summit ledge (capilla)", -2, 74, 25.0, 7, 5.5, "grass"),
    ("spit landing", -56, -70, 0.2, 7, 6, "grass"),
]
STAIRS = [("court", "mid north"), ("court", "low west"), ("court", "low north-east"), ("low west", "mid north-west"),
          ("low west", "west shoulder"), ("mid north", "high north"), ("mid north-west", "high west"),
          ("low north-east", "mid east"), ("mid east", "high east"), ("high north", "summit ledge (capilla)")]
# Peaks of the massif, weighted north-west: (x, y, height, radius).
PEAKS = [(-42, 64, 42, 24), (-2, 86, 50, 20), (32, 78, 30, 16), (-72, 32, 26, 18), (52, 48, 16, 13),
         (-14, 50, 22, 20), (-64, -44, 9, 14), (-24, 104, 34, 18), (-84, 60, 22, 16)]


def _coast_curve(steps=12):
    """The coast as a dense closed polyline (Catmull-Rom through COAST)."""
    pts, n = [], len(COAST)
    for i in range(n):
        p0, p1, p2, p3 = (Vector(COAST[(i + k) % n] + (0,)) for k in (-1, 0, 1, 2))
        for k in range(steps):
            t = k / steps
            pts.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t))
    return [(v.x, v.y) for v in pts]


COAST_LINE = _coast_curve()


def coast_distance(x, y):
    """Signed distance to the coast: positive on land, negative in the sea."""
    inside, best = False, 1e9
    n = len(COAST_LINE)
    for i in range(n):
        (ax, ay), (bx, by) = COAST_LINE[i], COAST_LINE[(i + 1) % n]
        if (ay > y) != (by > y) and x < (bx - ax) * (y - ay) / (by - ay) + ax:
            inside = not inside
        dx, dy = bx - ax, by - ay
        t = max(0.0, min(1.0, ((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy)))
        best = min(best, math.hypot(x - ax - t * dx, y - ay - t * dy))
    return best if inside else -best


def in_lagoon(x, y):
    """Open water: anything off the coast."""
    return coast_distance(x, y) < 0


def peak_height(x, y):
    h = SEABED
    for px, py, ph, pr in PEAKS:
        h = max(h, ph * math.exp(-((x - px) ** 2 + (y - py) ** 2) / (pr * pr) * 0.9) + BEACH)
    return h


def massif(x, y):
    d = coast_distance(x, y)
    if d < 0:
        return max(SEABED, BEACH - 0.6 + d * 0.35)          # the shallows shelve into the sea
    h = max(peak_height(x, y), BEACH + 3.0 * min(1.0, (d - BEACH_BAND) / 14))
    h += 1.6 * noise.noise(Vector((x * 0.06, y * 0.06, 0.3)))
    if d < BEACH_BAND:
        # The beach: flat sand rising gently from the waterline, with an irregular landward edge.
        wob = 2.5 * noise.noise(Vector((x * 0.08, y * 0.08, 7.1)))
        if d < BEACH_BAND + wob:
            return min(h, BEACH + 0.08 * d)
    return h


def pocket_at(x, y):
    for p in POCKETS:
        _n, px, py, pz, rx, ry, _s = p
        d = math.hypot((x - px) / rx, (y - py) / ry)
        if d < 1.0:
            return p, d
    return None, None


def height(x, y):
    h = massif(x, y)
    for _n, px, py, pz, rx, ry, _s in POCKETS:
        d = math.hypot((x - px) / rx, (y - py) / ry)
        if d < 1.0:
            return pz
        if d < 1.6:   # a short rim blend so pockets sit INTO the slope
            t = (d - 1.0) / 0.6
            h = pz + (h - pz) * t if h > pz else max(h, pz - 0.2 - (1 - t) * 0.5)
    return h


# ---------------------------------------------------------------- boulders

def boulder_mesh(i):
    """One unique pillow stone. A subdivided cube, 70 per cent of the way to a sphere, stretched,
    chopped by three to five random planes into broad flat faces, then jittered."""
    rng = random.Random(900 + i)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=2, use_grid_fill=True)
    for v in bm.verts:
        s = v.co.normalized()
        v.co = v.co.lerp(s * 1.25, 0.7)
    sx, sy, sz = rng.uniform(0.8, 1.2), rng.uniform(0.7, 1.0), rng.uniform(1.0, 1.6)
    bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=bm.verts[:])
    for k in range(rng.randint(3, 5)):
        n = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.2, 1))).normalized()
        cut = rng.uniform(0.55, 0.85) * max(sx, sy, sz)
        res = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=n * cut, plane_no=n,
                                     clear_outer=True)
        edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if edges:
            bmesh.ops.holes_fill(bm, edges=edges)
    for v in bm.verts:
        v.co += Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))) * 0.04
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    # SOFT PLANES (review v1: flat shading made every stone a crystal). Faces are smooth; only
    # an edge between two faces more than 38 degrees apart stays sharp, so a stone reads as a
    # few broad planes with soft rounded shoulders, like the reference's.
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > math.radians(38):
            e.smooth = False
    me = bpy.data.meshes.new(f"boulder_{i:02d}")
    bm.to_mesh(me)
    bm.free()
    me.materials.append(B.mat("rock"))
    return me


def place_boulders(c, rng):
    kit = [boulder_mesh(i) for i in range(12)]
    placed = 0
    step = 4.8
    y = -100.0
    while y < 100:
        x = -100.0
        while x < 100:
            px, py = x + rng.uniform(-1.6, 1.6), y + rng.uniform(-1.6, 1.6)
            x += step
            pocket, d = pocket_at(px, py)
            if pocket is not None:
                continue
            h = height(px, py)
            if h < BEACH + 0.6 or coast_distance(px, py) < BEACH_BAND - 1:
                continue
            # 1: rock belongs to the massif. Where the peaks contribute little (the outer ring),
            # only one spot in four gets a stone, so the ring reads as sand and grass.
            if peak_height(px, py) < BEACH + 4 and rng.random() < 0.75:
                continue
            # 2: every pocket keeps its DOWNHILL front open (toward the lagoon mouth), so the
            # house on it looks out over the ledge instead of hiding behind a row of stones.
            front = None
            for p in POCKETS:
                dx, dy = (px - p[1]) / p[4], (py - p[2]) / p[5]
                dd = math.hypot(dx, dy)
                if 0.9 < dd < 2.1 and p[0] != "court":
                    to_mouth = Vector((30 - p[1], -70 - p[2], 0)).normalized()
                    if Vector((dx, dy, 0)).normalized().dot(to_mouth) > 0.35:
                        front = p[3] if front is None else min(front, p[3])
            # Rims: the boulders just outside a pocket are smaller, so a ledge edge is a row of
            # stones rather than a wall; the massif's body is bigger stones.
            near = min(math.hypot((px - p[1]) / p[4], (py - p[2]) / p[5]) for p in POCKETS)
            s = rng.uniform(2.2, 3.4) if near < 1.9 else rng.uniform(3.2, 6.0) * (1.15 - min(0.4, h / 80))
            z = h - s * rng.uniform(0.35, 0.7)
            if front is not None:
                # THE LEDGE FACE IS STILL STONE (review v2: clearing it showed smooth dirt cliffs).
                # Stones in front of a pocket stay, but their tops stop 0.3 m under the pocket
                # floor, so the house still looks out over them.
                z = min(z, front - 0.3 - s * 1.35)
            o = bpy.data.objects.new("boulder", kit[rng.randrange(len(kit))])
            o.location = (px, py, z)
            o.scale = (s * rng.uniform(0.85, 1.15), s * rng.uniform(0.85, 1.15), s * rng.uniform(0.8, 1.1))
            o.rotation_euler = (rng.uniform(-0.18, 0.18), rng.uniform(-0.18, 0.18), rng.uniform(0, math.tau))
            c.objects.link(o)
            placed += 1
        y += step
    print("[lagoon-cove] boulders placed:", placed)


# ---------------------------------------------------------------- ground

def ground(c):
    """The height field as one grid mesh (the fill that shows between boulders), with the
    pockets flat. Faces take the pocket's surface colour, else rock fill."""
    n, span = 170, 230.0
    bm = bmesh.new()
    verts = [[bm.verts.new((-span / 2 + i * span / n, -span / 2 + j * span / n, 0)) for i in range(n + 1)] for j in range(n + 1)]
    for row in verts:
        for v in row:
            v.co.z = height(v.co.x, v.co.y)
    mats = ["rock_fill", "court", "grass", "sand"]
    me = bpy.data.meshes.new("ground")
    for j in range(n):
        for i in range(n):
            f = bm.faces.new((verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]))
            f.normal_update()
            cx, cy = f.calc_center_median().x, f.calc_center_median().y
            pocket, _d = pocket_at(cx, cy)
            if pocket is not None:
                f.material_index = mats.index(pocket[6])
            elif f.calc_center_median().z <= BEACH + 0.5:
                f.material_index = 3
            elif f.normal.z < 0.75:
                f.material_index = 0   # steep ground is rock, never grass (review v3: green cliffs)
            elif peak_height(cx, cy) < BEACH + 4:
                f.material_index = 2   # the outer ring: grass, not bare fill (review v2)
            else:
                f.material_index = 0
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(B.mat(m))
    o = bpy.data.objects.new("ground", me)
    c.objects.link(o)
    B.box(c, "sea", (0, 0, WATER - 0.05), (500, 500, 0.1), "water")


# ---------------------------------------------------------------- village

def house(c, rng, name, x, y, z, face_to, small=False):
    """A blockout Filipino stilt house on its pocket: raised floor on short piles, sawali or
    painted walls, a steep thatch or tin roof, a front deck toward `face_to`."""
    w, d, h = rng.uniform(7, 9.5), rng.uniform(5.5, 7), rng.uniform(3.0, 3.6)
    if small:   # Bajau homes are small one-room houses (review v5: water houses too big)
        w, d, h = rng.uniform(4.5, 6), rng.uniform(4, 5), rng.uniform(2.5, 2.9)
    lift = 0.9
    rot = math.atan2(face_to[1] - y, face_to[0] - x) - math.pi / 2
    rm = Matrix.Rotation(rot, 3, "Z")
    at = lambda lx, ly: Vector((x, y, 0)) + rm @ Vector((lx, ly, 0))  # noqa: E731
    wall = rng.choice(("sawali", "sawali", "wall_paint", "deck"))
    B.box(c, f"{name} floor", (x, y, z + lift), (w + 0.4, d + 0.4, 0.2), "deck", rot_z=rot)
    B.box(c, f"{name} walls", (x, y, z + lift + h / 2), (w, d, h), wall, rot_z=rot)
    p = at(0, d / 2 + 1.1)
    B.box(c, f"{name} front deck", (p.x, p.y, z + lift), (w + 0.4, 2.2, 0.2), "deck", rot_z=rot)
    for lx in (-w / 2, w / 2):
        for ly in (-d / 2, d / 2 + 2.1):
            q = at(lx, ly)
            B.cylinder(c, "pile", (q.x, q.y, z + lift / 2 - 0.3), 0.13, lift + 0.6, "bamboo", sides=6)
    roof = "thatch" if rng.random() < 0.72 else rng.choice(("tin", "tin_red"))
    m = bpy.data.meshes.new("roof")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=4, radius1=1.0, radius2=0.02, depth=1.0,
                          matrix=Matrix.Rotation(math.pi / 4, 4, "Z"))
    bm.to_mesh(m)
    bm.free()
    o = B._obj(c, f"{name} roof", m, roof)
    o.location = (x, y, z + lift + h + 1.25)
    o.scale = ((w + 2.8) * 0.72, (d + 2.8) * 0.72, 3.6)
    o.rotation_euler = (0, 0, rot)


def village(c, rng):
    for name, px, py, pz, rx, ry, _s in POCKETS:
        if name == "court" or name.startswith("summit"):
            continue
        house(c, rng, f"house {name}", px, py + ry * 0.15, pz, (30, -70))
        for k in range(7):
            a = math.pi + 0.35 + k * (math.pi - 0.7) / 6
            fx, fy = px + math.cos(a) * rx * 0.95, py + math.sin(a) * ry * 0.95
            B.box(c, "fence post", (fx, fy, pz + 0.5), (0.12, 0.12, 1.0), "deck")
    water_village(c, rng)
    lookup = {p[0]: p for p in POCKETS}
    for a, b in STAIRS:
        A, Bp = lookup[a], lookup[b]
        va, vb = Vector((A[1], A[2], A[3])), Vector((Bp[1], Bp[2], Bp[3]))
        flat = Vector(((vb - va).x, (vb - va).y, 0)).normalized()
        ea = va + Vector((flat.x * A[4] * 0.85, flat.y * A[5] * 0.85, 0))
        eb = vb - Vector((flat.x * Bp[4] * 0.85, flat.y * Bp[5] * 0.85, 0))
        mid, run = (ea + eb) / 2, eb - ea
        o = B.box(c, f"stairs {a} to {b}", (0, 0, 0), (1.8, run.length, 0.25), "deck")
        o.matrix_world = Matrix.Translation(mid + Vector((0, 0, 0.2))) @ run.to_track_quat("Y", "Z").to_matrix().to_4x4()
    _n, sx, sy, sz, _rx, _ry, _s = lookup["summit ledge (capilla)"]
    B.box(c, "capilla", (sx, sy, sz + 2.5), (5.5, 7.5, 5), "chapel")
    B.box(c, "capilla roof", (sx, sy, sz + 5.3), (6.5, 8.5, 0.4), "tin_red")
    B.box(c, "bell tower", (sx, sy - 4.6, sz + 4.5), (2.4, 2.4, 9), "chapel")
    o = bpy.data.objects.new("landmark rock", bpy.data.meshes.get("boulder_03"))
    o.location, o.scale = (9, -40, SEABED + 1), (4.2, 3.4, 5.5)
    o.rotation_euler = (0, 0, 0.3)
    c.objects.link(o)


def water_village(c, rng):
    """THE SAMA-BAJAU WATER VILLAGE (owner: "badjao tribe isnt particularly land based"; photograph
    of a Bajau village: free-standing stilt houses over clear shallow water, narrow plank walks
    and ladders, laundry lines, and boats everywhere, both moored and paddled).
    Houses stand free in the open sea in loose clusters; some clusters are joined by a narrow
    walk, some houses stand alone. One long walk reaches the beach. Many boats in between:
    lepa houseboats (a long hull with a small shelter) and slim bangka outriggers."""
    clusters = [(26, -44, 3), (52, -52, 2), (40, -76, 3), (78, -42, 2), (70, -84, 3), (14, -86, 1),
                (100, -66, 2), (-10, -64, 1), (92, -100, 1), (46, -110, 2)]
    homes = []
    for cx, cy, n in clusters:
        prev = None
        # A cluster strings out along a wandering line, 11 to 14 m house to house, so houses
        # never overlap (review v5) and the plank walk between them shows.
        heading = rng.uniform(0, math.tau)
        x, y = cx, cy
        for k in range(n):
            if k:
                heading += rng.uniform(-0.8, 0.8)
                step = rng.uniform(11, 14)
                x, y = x + math.cos(heading) * step, y + math.sin(heading) * step
            floor = WATER + 1.0 + rng.uniform(0.0, 0.4)   # decks about 1.9 m over the water
            house(c, rng, "bajau house", x, y, floor, (x + rng.uniform(-20, 20), y + rng.uniform(-20, 20)), small=True)
            for lx in (-2.4, 0.0, 2.4):
                for ly in (-2.2, 2.2):
                    B.cylinder(c, "stilt", (x + lx, y + ly, (floor + 0.9 + SEABED) / 2), 0.12,
                               floor + 0.9 - SEABED, "bamboo", sides=6)
            if rng.random() < 0.6:   # a laundry line on two poles off the deck
                B.box(c, "laundry", (x + 3.6, y, floor + 2.4), (0.05, 4.0, 1.0), "net")
            if prev is not None:
                walk(c, prev, (x, y), floor + 0.95)
            prev = (x, y)
            homes.append((x, y))
    # The one long walk to the shore, from the nearest cluster to the beach by the court.
    walk(c, homes[0], (16, -21), WALK_Z)
    # Boats: lepa houseboats and bangka outriggers, moored by houses and out on open water.
    for i in range(22):
        if i < len(homes):
            hx, hy = homes[i]
            x, y = hx + rng.choice((-6, 6)), hy + rng.uniform(-3, 3)
        else:
            x, y = rng.uniform(-30, 115), rng.uniform(-35, -120)
            if coast_distance(x, y) > -4:
                continue
        rz = rng.uniform(0, math.pi)
        if rng.random() < 0.35:   # a lepa: longer hull, a small arched shelter amidships
            B.box(c, "lepa hull", (x, y, WATER + 0.3), (8.0, 1.4, 0.7), "boat", rot_z=rz)
            B.box(c, "lepa shelter", (x, y, WATER + 1.2), (3.0, 1.5, 1.1), "thatch", rot_z=rz)
        else:
            B.box(c, "bangka hull", (x, y, WATER + 0.25), (5.5, 0.9, 0.5), "boat", rot_z=rz)
            for side in (-1, 1):
                B.box(c, "outrigger", (x - math.sin(rz) * side * 2.2, y + math.cos(rz) * side * 2.2, WATER + 0.12),
                      (4.2, 0.18, 0.18), "bamboo", rot_z=rz)


def walk(c, a, b, z):
    """A narrow plank walk on thin piles between two points."""
    va, vb = Vector((a[0], a[1], z)), Vector((b[0], b[1], z))
    run = vb - va
    o = B.box(c, "plank walk", (0, 0, 0), (1.3, run.length, 0.15), "deck")
    o.matrix_world = Matrix.Translation((va + vb) / 2) @ run.to_track_quat("Y", "Z").to_matrix().to_4x4()
    for k in range(int(run.length / 3.5) + 1):
        p = va.lerp(vb, k / max(1, int(run.length / 3.5)))
        B.cylinder(c, "walk pile", (p.x, p.y, (z + SEABED) / 2), 0.09, z - SEABED, "bamboo", sides=5)


WALK_Z = -0.6


def planting(c, rng):
    """Palms and bushes where rock meets a pocket or the beach, and in the massif's gaps."""
    for p in POCKETS:
        name, px, py, pz, rx, ry, _s = p
        n = 3 if name == "court" else 2
        for k in range(n + rng.randint(0, 2)):
            a = rng.uniform(0, math.tau)
            if name == "court" and math.sin(a) < -0.3:
                continue   # keep the court's open south side clear
            x, y = px + math.cos(a) * rx * 1.02, py + math.sin(a) * ry * 1.02
            h = rng.uniform(5, 8)
            lean = rng.uniform(-0.25, 0.25)
            t = B.cylinder(c, "palm trunk", (x, y, pz + h / 2), 0.2, h, "bamboo", sides=5)
            t.rotation_euler = (lean, rng.uniform(-0.25, 0.25), 0)
            B.blob(c, "palm crown", (x + math.sin(lean) * -h / 2, y, pz + h), 1.9, "palm")
            B.blob(c, "rock-foot bush", (x + rng.uniform(-1.5, 1.5), y + rng.uniform(-1.5, 1.5), pz + 0.4),
                   rng.uniform(0.8, 1.3), "palm")
    for i in range(90):
        x, y = rng.uniform(-95, 70), rng.uniform(-95, 110)
        if pocket_at(x, y)[0] is not None or coast_distance(x, y) < BEACH_BAND:
            continue
        h = height(x, y)
        if h < BEACH:
            continue
        B.blob(c, "crevice bush", (x, y, h + 0.8), rng.uniform(0.9, 1.6), "palm")


def preview(version):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 2000
    cam.data.sensor_fit = "HORIZONTAL"
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    E = 1.3
    shots = [
        ("plan", "ORTHO", (0, 0, 250), (0, 0, 0), 190),
        ("ref_angle", "PERSP", (40, -100, 20), (-6, 14, 8), 24),    # the reference's framing
        ("village", "PERSP", (95, -20, 14), (40, -75, 0), 28),      # over the water village
        ("aerial", "PERSP", (60, -170, 110), (0, -10, 0), 26),
        ("eye_north", "PERSP", (0, -9, E), (0, 45, 8), eye),
        ("eye_east", "PERSP", (-9, 0, E), (45, 0, 4), eye),
        ("eye_south", "PERSP", (0, 9, E), (0, -45, -1), eye),
        ("eye_west", "PERSP", (9, 0, E), (-45, 0, 4), eye),
    ]
    for name, kind, pos, tgt, lens in shots:
        cam.data.type = kind
        if kind == "ORTHO":
            cam.data.ortho_scale = lens
        else:
            cam.data.lens = lens
        cam.location = pos
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"cove_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[lagoon-cove] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    rng = random.Random(2026)
    root = bpy.data.collections.new("lagoon_cove")
    bpy.context.scene.collection.children.link(root)
    ground(L.col("Ground", root))
    place_boulders(L.col("Boulders", root), rng)
    village(L.col("Village", root), rng)
    planting(L.col("Planting", root), rng)
    L.gameplay(root)
    B.lighting()
    scene = bpy.context.scene
    try:
        scene.view_settings.look = "AgX - Punchy"   # 4: the reference's saturated warmth
    except TypeError:
        pass
    sun = next(o for o in scene.objects if o.type == "LIGHT")
    sun.data.color, sun.data.energy = (1.0, 0.92, 0.8), 4.5
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.36, 0.6, 0.92, 1)
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "lagoon_cove.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = out.with_suffix(".blend1")
    if backup.exists():
        backup.unlink()
    print("[lagoon-cove] saved", out)
    if version:
        preview(version)


if __name__ == "__main__":
    main()
