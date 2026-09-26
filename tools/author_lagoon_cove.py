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
import author_lagoon_rocks as R        # noqa: E402  the rock kit

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
    # ⚠️ OWNER on cove v9, pointing at the stepped shoreline and ledge walls: "are we able to make
    # these edges less jagged?". Every jag was a JUMP in this function (0.6 m at the waterline,
    # up to several metres at the beach's landward edge and at pocket rims), which the grid can
    # only draw as a staircase of cells. Every transition below is now continuous, so the grid
    # interpolates a smooth slope and the waterline falls wherever that slope crosses the water.
    d = coast_distance(x, y)
    if d < 0:
        # The shelf slopes gently (0.12 m per m) so a wide band of clear shallows shows the sand
        # (the owner's stylized water reference), then drops to the seabed.
        return max(SEABED, WATER + d * 0.12 - max(0.0, -d - 12) * 0.2)
    h = max(peak_height(x, y), BEACH + 3.0 * max(0.0, min(1.0, (d - BEACH_BAND) / 14)))
    h += 1.6 * noise.noise(Vector((x * 0.06, y * 0.06, 0.3)))
    # The beach: flat sand rising gently from the waterline, with an irregular landward edge that
    # BLENDS into the land over 4 m instead of stepping up.
    wob = 2.5 * noise.noise(Vector((x * 0.08, y * 0.08, 7.1)))
    sand = WATER + 0.085 * d   # meets the water exactly ON the coast curve, where the foam line is
    t = _smoothstep(BEACH_BAND + wob - 3.0, BEACH_BAND + wob + 1.0, d)
    return sand + (max(h, sand) - sand) * t


def _smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


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
        if d < 1.6:   # a rim blend so pockets sit INTO the slope, continuous at both ends
            # (the old lower-side blend ended in a vertical step at d = 1.6: the jagged wall)
            t = _smoothstep(1.0, 1.6, d)
            h = pz + (h - pz) * t
    return h


# ---------------------------------------------------------------- boulders

# THE ROCK KIT (docs/LAGOON_REWORK_GUIDE.md § 8 step 2) replaced the blockout pillow stones:
# 16 chiselled stones in five families from tools/author_lagoon_rocks.py, each with its origin
# at the GROUND CONTACT (base centre, ~20 % of its height already below z = 0), a world-scale
# "UVMap" for the tiling rock texture and a unique "UVBake" for the baked edge wear.
KIT = []


def pick(rng, weights):
    """A kit mesh from a weighted family mix, e.g. {"boulder": 6, "stack": 1}."""
    total = sum(weights.values())
    roll = rng.uniform(0, total)
    for family, w in weights.items():
        if roll < w:
            return KIT[rng.choice(R.ROCK_FAMILIES[family])]
        roll -= w
    return KIT[rng.choice(R.ROCK_FAMILIES[family])]


def ground_under(x, y, r):
    """The LOWEST ground under a stone's footprint, so its downhill side never floats."""
    return min(height(x + dx * r, y + dy * r) for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)))


ROCK_LOOK = "rock_a+brush"   # texture + wear variant (see render_lagoon_texture_preview.edge_wear)
MASSIF_MIX = {"boulder": 7, "stack": 0.6, "split": 1.5, "cobble": 1}
RIM_MIX = {"boulder": 3, "cobble": 2, "slab": 1}
SHORE_MIX = {"cobble": 3, "slab": 2, "boulder": 1}


def place_boulders(c, rng):
    KIT[:] = R.build_rock_kit(B.mat("rock"))
    kit = KIT
    placed, spots = 0, []
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
            # SELF-REVIEW v6: every stone was 3 to 6 m, so the pile read as one even gravel. The
            # reference mixes a few giants, a body of mid stones and small ones tucked between.
            roll = rng.random()
            if roll < 0.18:
                s *= 0.45                      # small stones in the seams
            elif roll > 0.93 and near > 1.9 and h < 18:
                s *= 1.6                       # the odd big one low on the massif
            me = pick(rng, MASSIF_MIX)
            sz = s * rng.uniform(0.8, 1.1)
            if me["rock_family"] == "stack":
                # A stack is ~5 m tall at scale 1: at massif scale it became a 20 to 30 m chimney
                # (review v17). In the pile it is a short spire.
                s, sz = s * 0.6, sz * 0.45
            # SEATED BETWEEN the centre and the lowest ground under the footprint (review v17:
            # seating on the lowest point sank every stone downhill on the steep massif and bared
            # the fill uphill as smooth brown cones; the centre alone floats the downhill side).
            z = 0.5 * (height(px, py) + ground_under(px, py, 1.25 * s)) - s * rng.uniform(0.05, 0.3)
            if front is not None:
                # THE LEDGE FACE IS STILL STONE (review v2: clearing it showed smooth dirt cliffs).
                # Stones in front of a pocket stay, but their tops stop 0.3 m under the pocket
                # floor, so the house still looks out over them.
                z = min(z, front - 0.3 - me["rock_top_z"] * sz)
            o = bpy.data.objects.new("boulder", me)
            o.location = (px, py, z)
            o.scale = (s * rng.uniform(0.85, 1.15), s * rng.uniform(0.85, 1.15), sz)
            tilt = 0.06 if me["rock_family"] == "slab" else 0.18   # a slab shows its lifted edge
            o.rotation_euler = (rng.uniform(-tilt, tilt), rng.uniform(-tilt, tilt), rng.uniform(0, math.tau))
            c.objects.link(o)
            spots.append((px, py, s * 0.9))
            placed += 1
        y += step
    spots += rim_stones(c, kit, rng)
    spots += feature_boulders(c, kit)
    spots += shore_rocks(c, kit, rng)
    print("[lagoon-cove] boulders placed:", placed, "footprints:", len(spots))
    return spots


# SELF-REVIEW v6 (and the reference): a few HUGE feature stones give the massif its silhouette and
# the coast its landmarks; without them the pile reads as even gravel. Hand placed: (x, y, size,
# kit shape, turn, how far it is sunk as a fraction of its size).
FEATURES = [
    # (x, y, scale, family, index in family, turn). Stacks are ~5 m tall at scale 1, boulders ~2.
    # The plain monolith (stack 0) read as a chimney as a feature (review v17): the lumpy spires
    # (stack 1 and 2, the owner's rock-pack reference) stand in the surf and on the skyline.
    (65, 29, 2.6, "stack", 1, 0.4),       # the east sea cliff: a spire standing in the surf
    (-16, 22, 4.2, "boulder", 0, 1.9),    # behind the court's north-west corner, seen from the court
    (-80, -40, 2.6, "stack", 2, 2.6),     # the spit's root, the west's one tall stone
    (-86, -8, 5.0, "boulder", 3, 0.2),    # its partner a little north, a pair not a twin
    (18, 96, 8.0, "boulder", 4, 1.1),     # on the northern skyline beside the summit
    (-60, 80, 3.2, "stack", 1, 2.2),      # a spire on the north-west skyline
    (-50, -104, 2.0, "stack", 2, 0.9),    # a sea stack off the spit's tip
]


def rim_stones(c, kit, rng):
    """SELF-REVIEW v7 (game's-eye north): the grid's stones missed most ledge fronts, so from the
    court every house pocket stood on a flat brown dirt wall, the fault the owner rejected on
    cove v2 ("smooth dirt cliffs"). Each raised pocket now gets its downhill arc built as a
    retaining face of stones in two rows, the top row's crowns just under the ledge floor, the
    way the reference's houses sit on stacked boulders."""
    spots = []
    for name, px, py, pz, rx, ry, _s in POCKETS:
        if name == "court" or pz < 1.5:
            continue
        to_mouth = Vector((30 - px, -70 - py, 0)).normalized()
        steps = int(math.tau * (rx + ry) / 2 / 3.0)
        for k in range(steps):
            a = k / steps * math.tau + rng.uniform(-0.08, 0.08)
            if Vector((math.cos(a), math.sin(a), 0)).dot(to_mouth) < 0.1:
                continue
            for row, (reach, drop) in enumerate(((1.06, 0.15), (1.32, 1.9))):
                x, y = px + math.cos(a) * rx * reach, py + math.sin(a) * ry * reach
                if pocket_at(x, y)[0] is not None:
                    continue
                s = rng.uniform(1.3, 1.9) * (1.0 + row * 0.25)
                me = pick(rng, RIM_MIX)
                o = bpy.data.objects.new("rim stone", me)
                o.location = (x, y, pz - drop - me["rock_top_z"] * s)
                o.scale = (s * rng.uniform(0.9, 1.15), s * rng.uniform(0.9, 1.15), s)
                o.rotation_euler = (rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1), rng.uniform(0, math.tau))
                c.objects.link(o)
                spots.append((x, y, s * 0.9))
    return spots


def feature_boulders(c, kit):
    spots = []
    for x, y, s, family, k, rz in FEATURES:
        o = bpy.data.objects.new("feature boulder", kit[R.ROCK_FAMILIES[family][k]])
        # A stone standing in the sea is measured from the WATER (review: sunk from the seabed
        # the old ones showed as flat slabs awash); the kit's stacks are tall on their own.
        wet = coast_distance(x, y) < 0
        o.location = (x, y, WATER - 0.3 * s if wet else ground_under(x, y, 1.25 * s) - 0.1 * s)
        o.scale = (s, s * 0.9, s)
        o.rotation_euler = (0.08, -0.05, rz)
        c.objects.link(o)
        spots.append((x, y, s * 0.9))
    return spots


def shore_rocks(c, kit, rng):
    """Small stones half buried along the sand and in the shallows, in twos and threes, so the
    beach is not a clean ribbon (the reference scatters them at the waterline)."""
    spots = []
    n = len(COAST_LINE)
    for i in range(0, n, 3):
        if rng.random() > 0.4:
            continue
        (ax, ay), (bx, by) = COAST_LINE[i], COAST_LINE[(i + 1) % n]
        for k in range(rng.randint(1, 3)):
            x, y = ax + rng.uniform(-3, 3), ay + rng.uniform(-3, 3)
            if pocket_at(x, y)[0] is not None or math.hypot(x, y + 12) < 20:
                continue   # never on a pocket, and keep the court's beach front open
            s = rng.uniform(0.35, 0.9)
            o = bpy.data.objects.new("shore rock", pick(rng, SHORE_MIX))
            o.location = (x, y, max(height(x, y), SEABED) - s * 0.1)
            o.scale = (s, s * rng.uniform(0.8, 1.1), s * rng.uniform(0.7, 1.0))
            o.rotation_euler = (0, 0, rng.uniform(0, math.tau))
            c.objects.link(o)
    return spots


# ---------------------------------------------------------------- ground

# ⚠️ HOW THE GROUND IS PAINTED, AND WHY NOT PER-VERTEX COLOURS.
# v7 to v15 gave each grid vertex a finished colour (court, grass, sand, rock). Neighbouring
# vertices then blended across a 1.15 m cell, so every boundary was a soft STAIRCASE following
# the grid (owner on v15, pointing at the court edge: "you'll need to retopologize the edges i
# think. unless you can figure out how to fix this jagged texture/color stuff").
# The fix is not topology. Each vertex now stores CONTINUOUS signed fields in metres (how far
# inside the court, a grass pocket, the sand, the steep ground...), and the material cuts each
# field at zero with a narrow smoothstep. A linearly interpolated continuous field crosses zero
# along a straight line inside every triangle, so the boundary is a smooth curve at any grid
# size, crisp like a painted edge. A little noise added to the fields wobbles the edges by hand.
# The same fields become the splat masks when the ground is textured (guide § 8).
GROUND_FIELDS = ("court_in", "grass_in", "sand_in", "wet_depth", "steep", "ring")
SEABED_SHALLOW, SEABED_DEEP = (0.52, 0.70, 0.52), (0.14, 0.38, 0.32)


def ground_fields(x, y, z, slope_z):
    """Signed fields in metres, positive INSIDE the region they name."""
    court_in, grass_in = -99.0, -99.0
    for name, px, py, pz, rx, ry, surface in POCKETS:
        f = (1.0 - math.hypot((x - px) / rx, (y - py) / ry)) * min(rx, ry)
        if surface == "court":
            court_in = max(court_in, f)
        else:
            grass_in = max(grass_in, f)
    return {
        "court_in": court_in,
        "grass_in": grass_in,
        "sand_in": (BEACH + 0.5 - z) * 4.0,            # the beach, by height (4x: metres of run)
        "wet_depth": WATER - z,                        # under the water: the seabed
        "steep": (0.75 - slope_z) * 12.0,             # steep ground is rock, never grass (v3)
        "ring": (BEACH + 4.0 - peak_height(x, y)),     # the outer ring is grass, not fill (v2)
    }


def ground_material():
    """Layers, lowest first: rock fill, ring grass, steep rock, sand, seabed, pocket grass,
    court. Each is a Mix whose factor is smoothstep(-w, w, field + wobble)."""
    m = bpy.data.materials.get("ground_painted")
    if m:
        return m
    m = bpy.data.materials.new("ground_painted")
    if m.node_tree is None:
        m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.9
    C = B.COLOURS
    coords = nt.nodes.new("ShaderNodeTexCoord")
    wob = nt.nodes.new("ShaderNodeTexNoise")
    wob.inputs["Scale"].default_value = 0.35
    nt.links.new(coords.outputs["Object"], wob.inputs["Vector"])
    wob_m = nt.nodes.new("ShaderNodeMapRange")           # noise 0..1 to -0.6..0.6 m
    wob_m.inputs["To Min"].default_value, wob_m.inputs["To Max"].default_value = -0.6, 0.6
    nt.links.new(wob.outputs["Fac"], wob_m.inputs["Value"])

    def mask(field, width=0.12, wobble=True):
        a = nt.nodes.new("ShaderNodeAttribute")
        a.attribute_name = field
        src = a.outputs["Fac"]
        if wobble:
            add = nt.nodes.new("ShaderNodeMath")
            add.operation = "ADD"
            nt.links.new(src, add.inputs[0])
            nt.links.new(wob_m.outputs["Result"], add.inputs[1])
            src = add.outputs["Value"]
        mr = nt.nodes.new("ShaderNodeMapRange")
        mr.interpolation_type = "SMOOTHSTEP"
        mr.inputs["From Min"].default_value, mr.inputs["From Max"].default_value = -width, width
        nt.links.new(src, mr.inputs["Value"])
        return mr.outputs["Result"], a

    def layer(under, colour, fac):
        mix = nt.nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        mix.inputs["B"].default_value = (*colour, 1.0)
        if isinstance(under, tuple):
            mix.inputs["A"].default_value = (*under, 1.0)
        else:
            nt.links.new(under, mix.inputs["A"])
        nt.links.new(fac, mix.inputs["Factor"])
        return mix.outputs["Result"]

    out = layer(C["rock_fill"], C["grass"], mask("ring")[0])
    out = layer(out, C["rock_fill"], mask("steep", 0.3, False)[0])
    out = layer(out, C["sand"], mask("sand_in")[0])
    # The seabed: wet sand tinted teal at the waterline, darker and greener with depth.
    wet, wet_attr = mask("wet_depth", 0.05, False)
    deep = nt.nodes.new("ShaderNodeMapRange")
    deep.interpolation_type = "SMOOTHSTEP"
    deep.inputs["From Min"].default_value, deep.inputs["From Max"].default_value = 0.0, 1.7
    nt.links.new(wet_attr.outputs["Fac"], deep.inputs["Value"])
    seabed = layer(SEABED_SHALLOW, SEABED_DEEP, deep.outputs["Result"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    nt.links.new(out, mix.inputs["A"])
    nt.links.new(seabed, mix.inputs["B"])
    nt.links.new(wet, mix.inputs["Factor"])
    out = mix.outputs["Result"]
    out = layer(out, C["grass"], mask("grass_in")[0])
    out = layer(out, C["court"], mask("court_in", 0.08)[0])
    nt.links.new(out, bsdf.inputs["Base Color"])
    return m


def ground(c):
    """The height field as one grid mesh (the fill that shows between boulders), with the
    pockets flat, painted from per-vertex fields (see GROUND_FIELDS above)."""
    n, span = 240, 276.0   # 1.15 m cells; wide enough that the shelf reaches the seabed inside it
    cell = span / n
    hs = [[height(-span / 2 + i * cell, -span / 2 + j * cell) for i in range(n + 1)] for j in range(n + 1)]
    bm = bmesh.new()
    verts = [[bm.verts.new((-span / 2 + i * cell, -span / 2 + j * cell, hs[j][i])) for i in range(n + 1)]
             for j in range(n + 1)]
    for j in range(n):
        for i in range(n):
            bm.faces.new((verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]))
    me = bpy.data.meshes.new("ground")
    bm.to_mesh(me)
    bm.free()
    values = {f: [] for f in GROUND_FIELDS}
    for j in range(n + 1):
        for i in range(n + 1):
            gx = hs[j][min(n, i + 1)] - hs[j][max(0, i - 1)]
            gy = hs[min(n, j + 1)][i] - hs[max(0, j - 1)][i]
            slope_z = 1.0 / math.sqrt(1 + (gx / (2 * cell)) ** 2 + (gy / (2 * cell)) ** 2)
            x, y = -span / 2 + i * cell, -span / 2 + j * cell
            for f, v in ground_fields(x, y, hs[j][i], slope_z).items():
                values[f].append(v)
    for f in GROUND_FIELDS:
        a = me.attributes.new(f, "FLOAT", "POINT")
        a.data.foreach_set("value", values[f])
    me.materials.append(ground_material())
    for poly in me.polygons:
        poly.use_smooth = True   # soft slopes, not faceted terraces (owner: "less jagged")
    o = bpy.data.objects.new("ground", me)
    c.objects.link(o)


# ---------------------------------------------------------------- village

def house(c, rng, name, x, y, z, face_to, small=False, pile_foot=None):
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
            foot = z - 0.6 if pile_foot is None else pile_foot   # water homes stand on the seabed
            B.cylinder(c, "pile", (q.x, q.y, (z + lift + foot) / 2), 0.13, z + lift - foot, "bamboo", sides=6)
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
    beached_boats(c, rng)
    lookup = {p[0]: p for p in POCKETS}
    for a, b in STAIRS:
        A, Bp = lookup[a], lookup[b]
        va, vb = Vector((A[1], A[2], A[3])), Vector((Bp[1], Bp[2], Bp[3]))
        flat = Vector(((vb - va).x, (vb - va).y, 0)).normalized()
        ea = va + Vector((flat.x * A[4] * 0.85, flat.y * A[5] * 0.85, 0))
        eb = vb - Vector((flat.x * Bp[4] * 0.85, flat.y * Bp[5] * 0.85, 0))
        # STONE STEPS, not a plank (self-review v7: a tilted board read as a fallen plank). One
        # block per ~0.35 m of rise, each sitting on the ground under it so none floats.
        run = eb - ea
        flat_run = Vector((run.x, run.y, 0))
        n = max(3, int(abs(run.z) / 0.35))
        heading = math.atan2(flat_run.y, flat_run.x) - math.pi / 2   # box +Y along the run
        for k in range(n):
            p = ea + run * ((k + 0.5) / n)
            top = ea.z + run.z * ((k + 1) / n if run.z > 0 else k / n)
            bottom = min(top - 0.3, height(p.x, p.y) - 0.3)
            B.box(c, f"step {a} to {b}", (p.x, p.y, (top + bottom) / 2),
                  (2.0, flat_run.length / n + 0.15, top - bottom), "rock_light", rot_z=heading)
    _n, sx, sy, sz, _rx, _ry, _s = lookup["summit ledge (capilla)"]
    B.box(c, "capilla", (sx, sy, sz + 2.5), (5.5, 7.5, 5), "chapel")
    B.box(c, "capilla roof", (sx, sy, sz + 5.3), (6.5, 8.5, 0.4), "tin_red")
    B.box(c, "bell tower", (sx, sy - 4.6, sz + 4.5), (2.4, 2.4, 9), "chapel")
    o = bpy.data.objects.new("landmark rock", KIT[R.ROCK_FAMILIES["boulder"][0]])
    o.location, o.scale = (LANDMARK[0], LANDMARK[1], WATER - 1.0), (3.4, 3.0, 3.0)
    o.rotation_euler = (0, 0, 0.3)
    c.objects.link(o)


# SELF-REVIEW v6: the water village read as a scattered blob (ten loose clusters on random
# headings), and from the court you saw none of it. A real Bajau village grows along a MAIN
# WALKWAY out from the shore, homes branching off it on short spurs, with a few houses standing
# alone further out that only a boat reaches. So: one spine from the beach beside the court out to
# the south-east, a secondary walk to the east under the sea cliff, and free-standing homes.
SPINE = [(15, -22), (17, -34), (24, -47), (36, -57), (52, -63), (68, -72), (80, -88), (86, -106)]
EAST_WALK = [(52, -63), (66, -50), (82, -44), (98, -46)]
FREE_HOMES = [(-16, -62), (2, -86), (50, -114), (108, -64), (112, -92), (70, -120), (-30, -84)]
SPINE_Z = WATER + 1.9          # plank walks stand about 1.9 m over the water
LANDMARK = (-9, -44)           # the painted rock: left of the spine as seen from the court


def _polyline_points(pts, every):
    """Points every `every` metres along a polyline, with the local heading."""
    out, carry = [], every * 0.6
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        seg = math.hypot(bx - ax, by - ay)
        head = math.atan2(by - ay, bx - ax)
        t = carry
        while t < seg:
            out.append((ax + math.cos(head) * t, ay + math.sin(head) * t, head))
            t += every
        carry = t - seg
    return out


def water_home(c, rng, x, y, face_to):
    """A small one-room Bajau home on stilts reaching the seabed."""
    floor = WATER + 1.0 + rng.uniform(0.0, 0.4)
    house(c, rng, "bajau house", x, y, floor, face_to, small=True, pile_foot=SEABED)
    if rng.random() < 0.6:   # a laundry line off the deck
        B.box(c, "laundry", (x + 3.6, y, floor + 2.4), (0.05, 4.0, 1.0), "net")


def boat(c, rng, x, y, z, rz, lepa=None):
    """A lepa houseboat (long hull, small shelter amidships) or a slim bangka with outriggers."""
    if lepa is None:
        lepa = rng.random() < 0.35
    if lepa:
        B.box(c, "lepa hull", (x, y, z + 0.3), (8.0, 1.4, 0.7), "boat", rot_z=rz)
        B.box(c, "lepa shelter", (x, y, z + 1.2), (3.0, 1.5, 1.1), "thatch", rot_z=rz)
    else:
        B.box(c, "bangka hull", (x, y, z + 0.25), (5.5, 0.9, 0.5), "boat", rot_z=rz)
        B.box(c, "bangka trim", (x, y, z + 0.52), (5.6, 0.95, 0.08), "boat_trim", rot_z=rz)
        for side in (-1, 1):
            B.box(c, "outrigger", (x - math.sin(rz) * side * 2.2, y + math.cos(rz) * side * 2.2, z + 0.12),
                  (4.2, 0.18, 0.18), "bamboo", rot_z=rz)


def water_village(c, rng):
    """THE SAMA-BAJAU WATER VILLAGE (owner: "badjao tribe isnt particularly land based"; photograph
    of a Bajau village: free-standing stilt houses over clear shallow water, narrow plank walks
    and ladders, laundry lines, and boats everywhere, both moored and paddled)."""
    # The spine climbs from the sand to walk height over its first segment, like a jetty.
    walk(c, SPINE[0], SPINE[1], SPINE_Z, za=BEACH + 0.3)
    for a, b in zip(SPINE[1:], SPINE[2:]):
        walk(c, a, b, SPINE_Z)
    for a, b in zip(EAST_WALK, EAST_WALK[1:]):
        walk(c, a, b, SPINE_Z)
    homes = []
    # Homes branch off both walks on short spurs, alternating sides with some irregularity.
    side = 1
    for walk_pts, every in ((SPINE[1:], 12.5), (EAST_WALK, 12.0)):
        for x, y, head in _polyline_points(walk_pts, every):
            if rng.random() < 0.12:
                side = -side
                continue           # a gap now and then, so the rhythm is not a comb
            spur = rng.uniform(5.5, 8.5)
            nx, ny = math.cos(head + side * math.pi / 2), math.sin(head + side * math.pi / 2)
            hx, hy = x + nx * (spur + 3.2), y + ny * (spur + 3.2)
            crowded = any(math.hypot(hx - h[0], hy - h[1]) < 10 for h in homes)   # v8: two spurs met
            if crowded or coast_distance(hx, hy) > -4 or math.hypot(hx - LANDMARK[0], hy - LANDMARK[1]) < 9:
                side = -side
                continue
            walk(c, (x, y), (x + nx * spur, y + ny * spur), SPINE_Z)
            water_home(c, rng, hx, hy, (x, y))
            homes.append((hx, hy, head))
            side = -side
    for x, y in FREE_HOMES:     # the boat-only homes, facing wherever the family chose
        water_home(c, rng, x, y, (x + rng.uniform(-20, 20), y + rng.uniform(-20, 20)))
        homes.append((x, y, rng.uniform(0, math.tau)))
    # Boats: moored along the spine and by homes, and out on open water being paddled.
    for x, y, head in _polyline_points(SPINE[1:] + [], 9.0):
        if rng.random() < 0.55:
            s = rng.choice((-1, 1))
            boat(c, rng, x + math.cos(head + s * math.pi / 2) * 2.4, y + math.sin(head + s * math.pi / 2) * 2.4,
                 WATER, head)
    for hx, hy, head in homes:
        if rng.random() < 0.5:
            boat(c, rng, hx + rng.choice((-5.5, 5.5)), hy + rng.uniform(-3, 3), WATER, head + rng.uniform(-0.4, 0.4))
    placed = 0
    while placed < 9:
        x, y = rng.uniform(-40, 120), rng.uniform(-35, -125)
        if coast_distance(x, y) > -8 or any(math.hypot(x - h[0], y - h[1]) < 9 for h in homes):
            continue
        boat(c, rng, x, y, WATER, rng.uniform(0, math.tau))
        placed += 1


def beached_boats(c, rng):
    """Bangkas pulled up on the spit's sand, bows to the sea (the owner's stilt-village photo)."""
    n, placed = len(COAST_LINE), 0
    for i in range(0, n, 5):
        (ax, ay), (bx, by) = COAST_LINE[i], COAST_LINE[(i + 1) % n]
        if not (ax < -36 and ay < -30) or rng.random() < 0.35:
            continue
        tx, ty = (bx - ax), (by - ay)
        ln = math.hypot(tx, ty)
        tx, ty = tx / ln, ty / ln
        # Inward normal: test which side is land.
        nx, ny = -ty, tx
        if coast_distance(ax + nx * 3, ay + ny * 3) < 0:
            nx, ny = -nx, -ny
        x, y = ax + nx * rng.uniform(2.5, 4.5), ay + ny * rng.uniform(2.5, 4.5)
        if pocket_at(x, y)[0] is not None:
            continue
        boat(c, rng, x, y, height(x, y) - 0.15, math.atan2(ny, nx) + rng.uniform(-0.3, 0.3), lepa=False)
        placed += 1
    print("[lagoon-cove] beached boats:", placed)


def walk(c, a, b, z, za=None):
    """A narrow plank walk on thin piles between two points; `za` lets the first end start lower
    (a jetty climbing off the sand)."""
    za = z if za is None else za
    va, vb = Vector((a[0], a[1], za)), Vector((b[0], b[1], z))
    run = vb - va
    o = B.box(c, "plank walk", (0, 0, 0), (1.4, run.length, 0.15), "deck")
    o.matrix_world = Matrix.Translation((va + vb) / 2) @ run.to_track_quat("Y", "Z").to_matrix().to_4x4()
    steps = max(1, int(run.length / 3.5))
    for k in range(steps + 1):
        p = va.lerp(vb, k / steps)
        foot = max(SEABED, height(p.x, p.y)) if coast_distance(p.x, p.y) > 0 else SEABED
        if p.z - foot > 0.3:
            B.cylinder(c, "walk pile", (p.x, p.y, (p.z + foot) / 2), 0.09, p.z - foot, "bamboo", sides=5)

WALK_Z = -0.6


def planting(c, rng, spots):
    """Palms at the pockets' rims and along the sand, and plants in the GAPS at boulder feet
    (SELF-REVIEW v6: the massif was bare stone with a few lollipops; the reference is full of
    grass tufts, broad leaves and red flowers wherever a stone meets another stone or the ground).
    The plant shapes are the blockout kit in lagoon_cove_planting.py."""
    import lagoon_cove_planting as P
    for p in POCKETS:
        name, px, py, pz, rx, ry, _s = p
        n = 3 if name == "court" else 2
        for k in range(n + rng.randint(0, 2)):
            a = rng.uniform(0, math.tau)
            # Self-review v7: rim palms in FRONT of a ledge walled its house off from the court, and
            # the court's north rim hid the stairs. Court palms stand on its east and west sides
            # only; a house pocket's palms stand beside and behind the house, never in front.
            if name == "court" and abs(math.cos(a)) < 0.55:
                continue
            front = Vector((30 - px, -70 - py, 0)).normalized()
            if name != "court" and Vector((math.cos(a), math.sin(a), 0)).dot(front) > 0.2:
                continue
            x, y = px + math.cos(a) * rx * 1.02, py + math.sin(a) * ry * 1.02
            P.palm(c, rng, x, y, height(x, y), lean=a)          # leaning out, over the drop
            P.tuft(c, rng, x + rng.uniform(-1.5, 1.5), y + rng.uniform(-1.5, 1.5), pz)
    # THE SPIT AND THE SAND: palms along the beach leaning to the sea (the owner's stilt-village
    # photograph has a palm line behind every beach), thickest on the western spit.
    n = len(COAST_LINE)
    for i in range(0, n, 2):
        ax, ay = COAST_LINE[i]
        spit = ax < -30 and ay < -20
        if rng.random() > (0.75 if spit else 0.22):
            continue
        bx, by = COAST_LINE[(i + 1) % n]
        tx, ty = bx - ax, by - ay
        nx, ny = -ty, tx
        if coast_distance(ax + nx * 0.2, ay + ny * 0.2) < 0:
            nx, ny = -nx, -ny
        ln = math.hypot(nx, ny)
        nx, ny = nx / ln, ny / ln
        inset = rng.uniform(4.5, 9.0)
        x, y = ax + nx * inset, ay + ny * inset
        if pocket_at(x, y)[0] is not None or math.hypot(x, y + 12) < 22:
            continue   # not on a pocket, and the court's beach front stays open
        P.palm(c, rng, x, y, height(x, y), lean=math.atan2(-ny, -nx) + rng.uniform(-0.5, 0.5))
        if rng.random() < 0.5:
            P.tuft(c, rng, x + rng.uniform(-2, 2), y + rng.uniform(-2, 2), height(x, y))

    def avoid(x, y):
        if pocket_at(x, y)[0] is not None or coast_distance(x, y) < BEACH_BAND - 1:
            return True
        return abs(x) < 16 and abs(y) < 15    # never inside the court's play walls

    # ring 1.35: a sunk pillow stone meets the ground at about 1.2 x its size, and `spots` carry
    # 0.9 x, so the ring lands the plants just outside the stone rather than inside it.
    # scale 1.6 and more flowers: at 1x the low plants vanished beside 3 to 6 m stones and the
    # red accents barely showed (review of the first cove render with the kit).
    mix = (("tuft", 0.30), ("broadleaf", 0.34), ("flower_bush", 0.31), ("palm", 0.05))
    placed = P.plant_gaps(c, rng, spots, height, avoid, ring=1.35, scale=1.6, mix=mix)
    print("[lagoon-cove] gap plants:", placed)


def preview(version):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    if hasattr(scene.eevee, "shadow_pool_size"):
        scene.eevee.shadow_pool_size = "1024"   # the default pool overflowed (missing shadows)
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
        ("shore_close", "PERSP", (46, -34, 14), (22, -6, -1), 26),   # owner: "less jagged" edges
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


def sky(world):
    """SELF-REVIEW v6: one flat grey-blue behind everything, nothing like the reference's bright
    tropical sky. The CAMERA now sees a vertical gradient (pale warm haze at the horizon, clear
    blue overhead) while the LIGHTING keeps the old flat fill, so the scene's exposure does not
    move. Painted clouds come with the light pass (docs/LAGOON_REWORK_GUIDE.md § 8 step 7)."""
    nt = world.node_tree
    bg = nt.nodes["Background"]
    bg.inputs["Color"].default_value = (0.36, 0.6, 0.92, 1)
    out = nt.nodes["World Output"]
    coords = nt.nodes.new("ShaderNodeTexCoord")
    split = nt.nodes.new("ShaderNodeSeparateXYZ")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position, ramp.color_ramp.elements[0].color = 0.0, (0.86, 0.90, 0.84, 1)
    ramp.color_ramp.elements[1].position, ramp.color_ramp.elements[1].color = 0.45, (0.16, 0.46, 0.92, 1)
    mid = ramp.color_ramp.elements.new(0.12)
    mid.color = (0.52, 0.76, 0.96, 1)
    seen = nt.nodes.new("ShaderNodeBackground")
    seen.inputs["Strength"].default_value = 1.0
    path = nt.nodes.new("ShaderNodeLightPath")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(coords.outputs["Generated"], split.inputs[0])
    nt.links.new(split.outputs["Z"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], seen.inputs["Color"])
    nt.links.new(path.outputs["Is Camera Ray"], mix.inputs["Fac"])
    nt.links.new(bg.outputs["Background"], mix.inputs[1])
    nt.links.new(seen.outputs["Background"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    rng = random.Random(2026)
    root = bpy.data.collections.new("lagoon_cove")
    bpy.context.scene.collection.children.link(root)
    ground_col = L.col("Ground", root)
    ground(ground_col)
    import lagoon_cove_water as W
    W.build_sea(ground_col, coast_distance, WATER, SEABED, span=2400.0)   # to the horizon: at 520 its edge showed
    # NO MODELLED FOAM (owner, 2026-09-26: "we'll be implementing moving shore white foam using
    # shaders in unity"). W.build_foam stays in the module only as a reference for the band's width.
    spots = place_boulders(L.col("Boulders", root), rng)
    village(L.col("Village", root), rng)
    planting(L.col("Planting", root), rng, spots)
    # THE ROCK MATERIAL (§ 8 step 2, owner-chosen): rock_a by world box projection, light tops,
    # and edge wear baked from each kit stone's own geometry into one atlas.
    import bake_lagoon_rock_edges as E
    import render_lagoon_texture_preview as T
    E.bake("rock")
    T.rock_material(bpy.data.materials["rock"], ROCK_LOOK)
    L.gameplay(root)
    B.lighting()
    scene = bpy.context.scene
    try:
        scene.view_settings.look = "AgX - Punchy"   # 4: the reference's saturated warmth
    except TypeError:
        pass
    sun = next(o for o in scene.objects if o.type == "LIGHT")
    sun.data.color, sun.data.energy = (1.0, 0.92, 0.8), 4.5
    sky(scene.world)
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
