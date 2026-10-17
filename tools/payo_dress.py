"""PAYO, dressed: the owner-approved pieces laid on the clean valley (tools/payo_land.py -> payo_land_<tag>.blend).

  blender -b --python tools/payo_dress.py -- <land tag> <dress tag> [--views=a,b]

Writes ArtSource/payo/payo_dressed_<dress tag>.blend and Logs/payo/terrain/dressed_<dress tag>_<view>.png.

THE DESIGN (owner, 2026-10-17: "be creative in the design of the map, it should mix a blend of artistic culture while being
serene in its view"):
  * THE COURT is the valley's source. Its pit is walled on three sides with carved stone; on the fourth, toward the
    valley, it stands open over a cliff, and the court's water falls from it into a round SACRED POOL. Two guardian heads
    on the cliff spout water beside the fall. The river is born in the pool and runs down through the terraces to the
    sunset. So the view a player faces all match is water leaving the court and winding away through rice.
  * THE GATE stands behind the court on the uphill side, facing it; the TOWN climbs the head of the valley behind and
    beside it on the terrace shelves, every house turned to face the court.
  * Two GIANT BULUL sit on the rim either side of the court, facing it, as its old watchers.
  * HAMLETS of granaries and houses sit on the terrace shelves along the river, with drying racks and scarecrows; lantern
    posts mark the river path; forest and palms fill the slopes the rice does not.
Pieces are LOCKED (ArtSource/payo/pieces-approved-2026-10-12/ and Logs/payo/pieces/build/): placed, never altered.
"""
import json
import math
import os
import random
import sys

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
words = [a for a in args if not a.startswith("--")]
LAND = words[0] if words else "v22"
TAG = words[1] if len(words) > 1 else "d1"
ONLY = next((a.split("=", 1)[1].split(",") for a in args if a.startswith("--views=")), None)
OUT = os.path.join(ROOT, "Logs", "payo", "terrain")
A = os.path.join(ROOT, "ArtSource", "payo", "pieces-approved-2026-10-12")
B = os.path.join(ROOT, "Logs", "payo", "pieces", "build")
M = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Payo", "Models")
data = np.load(os.path.join(OUT, "land_%s.npz" % LAND))
water = data["water"]
Tg, Sg, grid = data["T"].astype(np.float32), data["S"].astype(np.float32), data["grid"]
POOL = data["pool"]

bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "ArtSource", "payo", "payo_land_%s.blend" % LAND))
sc = bpy.context.scene
for name in ("Court (44 m play square)", "Gate (stand-in)", "The pit's fall (stand-in)"):
    ob = bpy.data.objects.get(name)
    if ob:
        bpy.data.objects.remove(ob, do_unlink=True)
dg = bpy.context.evaluated_depsgraph_get()
bvhs = [BVHTree.FromObject(bpy.data.objects[n], dg) for n in ("Land", "Land terraces") if n in bpy.data.objects]


def ground(x, y):
    best = -200.0
    for bvh in bvhs:
        hit = bvh.ray_cast(Vector((x, y, 2000.0)), Vector((0, 0, -1)))
        if hit[0] and hit[0].z > best:
            best = hit[0].z
    return best


def field(arr, x, y):
    i = int(round((x - grid[0]) / grid[2])); j = int(round((y - grid[1]) / grid[2]))
    if 0 <= j < arr.shape[0] and 0 <= i < arr.shape[1]:
        return float(arr[j, i])
    return 0.0


def river_dist(x, y):
    d = np.hypot(water[:, 0] - x, water[:, 1] - y)
    return float(d.min())


# ---- the pieces, each imported once into its own hidden collection and placed as instances
lib_root = bpy.data.collections.new("Piece library (hidden)")
sc.collection.children.link(lib_root)
dressed = bpy.data.collections.new("Dressing")
sc.collection.children.link(dressed)
groups = {}
library = {}


CALM = os.path.join(ROOT, "Logs", "payo", "pieces", "calm")


def find(name):
    # the calm repaint (owner, 2026-10-17: "yes roll it out to everything") wins over the approved originals when present
    if os.path.isdir(CALM) and "--old" not in sys.argv:
        for fam in os.listdir(CALM):
            p = os.path.join(CALM, fam, name + ".glb")
            if os.path.isdir(os.path.join(CALM, fam)) and os.path.exists(p):
                return p
    for folder in (os.path.join(A, d) for d in os.listdir(A)):
        p = os.path.join(folder, name + ".glb")
        if os.path.exists(p):
            return p
    for p in (os.path.join(B, "stone", name + ".glb"), os.path.join(B, "terrace", name + ".glb"), os.path.join(M, name + ".glb"),
              os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "EskinitaAlley", "Models", name + ".glb"),
              os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "LagoonCove", "Models", name + ".glb")):
        if os.path.exists(p):
            return p
    raise FileNotFoundError(name)


def lib(name):
    if name in library:
        return library[name]
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=find(name))
    new = [o for o in bpy.data.objects if o not in before]
    col = bpy.data.collections.new("lib_" + name)
    lib_root.children.link(col)
    for o in new:
        for c in list(o.users_collection):
            c.objects.unlink(o)
        col.objects.link(o)
    library[name] = col
    tint(new)
    for o in new:                     # the head piece's water as its own review lit it (tools/payo_pieces/head_review.py)
        for slot in getattr(o, "material_slots", []):
            m = slot.material
            key = m.name.split(".")[0] if m else ""
            if name != "head_fall" or key not in ("py_fall", "py_mist", "py_foam") or not m.node_tree:
                continue
            b = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
            if b and b.inputs["Base Color"].links:
                m.node_tree.links.new(b.inputs["Base Color"].links[0].from_socket, b.inputs["Emission Color"])
                b.inputs["Emission Strength"].default_value = {"py_fall": 0.6, "py_foam": 0.7, "py_mist": 0.8}[key]
            if key == "py_mist" and b:
                b.inputs["Alpha"].default_value = 0.72
            if key in ("py_fall", "py_mist"):
                m.surface_render_method = "DITHERED" if key == "py_fall" else "BLENDED"
                m.use_backface_culling = False
    return col


# The tree and plant models of the earlier art set multiply a white base by their vertex colour (their game shader
# supplies the paint); here each of their materials is given its own colour so they read in Blender.
TINT = {"py_tree_leaf_dark": (0.20, 0.42, 0.22), "py_tree_leaf_light": (0.36, 0.58, 0.27), "py_tree_core": (0.12, 0.30, 0.16),
        "py_tree_trunk": (0.38, 0.29, 0.21), "py_pine": (0.15, 0.34, 0.23), "py_frond_dark": (0.24, 0.46, 0.20),
        "py_frond_light": (0.44, 0.66, 0.26), "py_frond_small": (0.36, 0.58, 0.24), "py_palm_trunk": (0.50, 0.40, 0.30),
        "py_leaf_dark": (0.20, 0.42, 0.22), "py_leaf_light": (0.36, 0.58, 0.27), "py_flower_red": (0.82, 0.30, 0.28),
        "py_banana_stem": (0.42, 0.62, 0.16), "py_paddle_dark": (0.18, 0.44, 0.10), "py_paddle_light": (0.56, 0.76, 0.18)}


# Lagoon Cove's plants carry no paint in their glb: in the game their leaves wear Lagoon's leaf drawings with a light and
# a dark tint per plant (tools/author_lagoon_leaves.py TINTS, STALK_TINTS, COLOUR_TINTS). The same paint for Blender.
LAGOON_TEX = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "LagoonCove", "Textures")
LAGOON_LEAF = {  # material name part: (drawing, light tint, dark tint, gain)
    "round_bush": ("leaf_round", (0.34, 0.62, 0.10), (0.10, 0.32, 0.08), 1.25),
    "round_groundcover": ("leaf_round", (0.40, 0.64, 0.09), (0.12, 0.36, 0.06), 1.25),
    "blade_grass": ("blade", (0.50, 0.70, 0.18), (0.20, 0.44, 0.12), 1.25),
    "monstera": ("monstera_leaf", (0.30, 0.56, 0.11), (0.09, 0.29, 0.08), 1.25),
    "croton": ("croton_leaf", (1.0, 0.98, 0.98), (0.62, 0.52, 0.54), 1.0),
}
LAGOON_STALK = {"croton": (0.50, 0.34, 0.26), "monstera": (0.28, 0.52, 0.16)}


def lagoon_paint(m, key):
    nt = m.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if not bsdf or m.get("tinted"):
        return
    m["tinted"] = 1
    bsdf.inputs["Emission Strength"].default_value = 0.0
    if "flower" in key:
        bsdf.inputs["Base Color"].default_value = (0.70 ** 2.2, 0.10 ** 2.2, 0.12 ** 2.2, 1)
        return
    if key.startswith("lagoon_stalk_"):
        drawing, c, gain = "stalk", LAGOON_STALK.get(key[len("lagoon_stalk_"):], (0.30, 0.52, 0.16)), 1.0
    else:
        part = next((k for k in LAGOON_LEAF if k in key), None)
        if part is None:
            return
        drawing, light, dark, gain = LAGOON_LEAF[part]
        c = dark if key.endswith("_dark") else light
    path = os.path.join(LAGOON_TEX, drawing + "_albedo.png")
    if not os.path.exists(path):
        bsdf.inputs["Base Color"].default_value = (*[v ** 2.2 for v in c], 1)
        return
    im = nt.nodes.new("ShaderNodeTexImage"); im.image = bpy.data.images.load(path, check_existing=True)
    mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1.0
    nt.links.new(im.outputs["Color"], mul.inputs[6])
    mul.inputs[7].default_value = (*[min(1.0, (v * gain)) ** 2.2 for v in c], 1)
    nt.links.new(mul.outputs[2], bsdf.inputs["Base Color"])
    nt.links.new(im.outputs["Alpha"], bsdf.inputs["Alpha"])
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "DITHERED"
    m.use_backface_culling = False


def tint(objects):
    for o in objects:
        for slot in getattr(o, "material_slots", []):
            m = slot.material
            key = m.name.split(".")[0] if m else ""
            if key.startswith("lagoon_") and m.node_tree:
                lagoon_paint(m, key)
                continue
            if key not in TINT or not m.node_tree:
                continue
            nt = m.node_tree
            bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
            vc = next((n for n in nt.nodes if n.type == "VERTEX_COLOR"), None)
            if not bsdf or m.get("tinted"):
                continue
            m["tinted"] = 1
            bsdf.inputs["Emission Strength"].default_value = 0.0     # they arrive glowing white
            mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"
            mul.inputs[0].default_value = 1.0
            c = TINT[key]
            mul.inputs[7].default_value = (c[0] ** 2.2, c[1] ** 2.2, c[2] ** 2.2, 1)
            if vc:
                nt.links.new(vc.outputs["Color"], mul.inputs[6])
            else:
                mul.inputs[6].default_value = (1, 1, 1, 1)
            nt.links.new(mul.outputs[2], bsdf.inputs["Base Color"])


def group(name):
    if name not in groups:
        g = bpy.data.collections.new(name); dressed.children.link(g); groups[name] = g
    return groups[name]


def place(name, x, y, z, face=None, yaw=None, scale=1.0, grp="Props", sz=None):
    """`face`: the world direction (dx, dy) the piece's FRONT should look toward. Pieces are built front +z in glTF,
    which Blender imports as front -y."""
    col = lib(name)
    e = bpy.data.objects.new("%s" % name, None)
    e.instance_type = "COLLECTION"; e.instance_collection = col
    if yaw is None:
        dx, dy = face if face else (0.0, -1.0)
        yaw = math.atan2(dy, dx) + math.pi / 2
    e.location = (x, y, z); e.rotation_euler = (0, 0, yaw)
    e.scale = (scale, scale, scale if sz is None else sz)
    group(grp).objects.link(e)
    return e


def towards(x, y, tx, ty):
    return (tx - x, ty - y)


# ---- 1. the stage: Liwasan's blocks, each turned so its carving looks outward
layout = json.load(open(os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Payo", "payo_layout.json")))
blocks = {b["id"]: b for b in json.load(open(os.path.join(B, "stone", "blocks.json")))["blocks"]}
for p in layout["layouts"][0]["pieces"]:
    if p.get("kind") != "block" or p["id"] not in blocks:
        continue
    b = blocks[p["id"]]
    x, y = float(p["x"]), float(p["z"])
    face = (x, y) if abs(x) + abs(y) > 0.1 else (0.0, 1.0)
    if abs(x) > abs(y) * 1.5:
        face = (math.copysign(1, x), 0.0)
    elif abs(y) > abs(x) * 1.5:
        face = (0.0, math.copysign(1, y))
    # neighbouring blocks overlap where their tops meet at one height: each block stands a few millimetres apart in
    # height from the next (invisible at play distance, but no two tops share a plane: they flickered, owner 2026-10-18),
    # and every extension sits under its block
    k_ = stage_k = globals().get("stage_k", -1) + 1
    globals()["stage_k"] = stage_k
    dz = 0.004 * (k_ % 7)
    place(b["glb"][:-4], x, y, float(p["top"]) - 0.01 - dz, face=face, grp="Stage")
    place(b["extension"][:-4], x, y, float(p["top"]) - 0.06 - dz, face=face, grp="Stage")

# ---- 2. the pit's three carved walls (west, east, south), two courses high, a kerb along the rim
RIM, FLOOR = -6.0, -30.0


def wall_run(piece, start, along, normal, n, z, grp, sz=None, length=None):
    """n 12 m modules covering start .. start + 12 n along. A module is built from its origin toward its own right as
    seen from the front, which is (-normal.y, normal.x); when `along` runs the other way its origin is the far end."""
    ext = (-normal[1], normal[0])
    back = along[0] * ext[0] + along[1] * ext[1] < 0
    for k in range(n):
        j = k + 1 if back else k
        x = start[0] + along[0] * 12.0 * j; y = start[1] + along[1] * 12.0 * j
        e = place(piece, x, y, z, face=normal, grp=grp, sz=sz if sz is not None else (1.006 if grp == "Platform" else None))
        if length is not None:                               # a run that is not a whole number of modules: stretch them
            e.scale.x *= length / (12.0 * n)


# The court platform, pit and gorge as ONE authored stone piece (tools/payo_pieces/court_build.py), when it exists:
# the owner found the modules lined up against bent terrain "unpolished". Without it, the old modules below.
COURT_PIECE = os.path.exists(os.path.join(CALM, "court", "court_platform.glb")) and "--old-court" not in sys.argv
if COURT_PIECE:
    place("court_platform", 0.0, 0.0, 0.0, yaw=0.0, grp="Platform")
for z in ((FLOOR, FLOOR + 12.0) if not COURT_PIECE else ()):
    # facing into the pit; each module runs 12 m to its own right (seen from the front)
    wall_run("pit_wall_niche" if z == FLOOR else "pit_wall", (-30.0, 30.0), (0, -1), (1, 0), 5, z, "Pit")
    wall_run("pit_wall_spout" if z == FLOOR else "pit_wall", (30.0, -30.0), (0, 1), (-1, 0), 5, z, "Pit")
    wall_run("pit_wall", (-30.0, -30.0), (1, 0), (0, 1), 5, z, "Pit")
if not COURT_PIECE:
    wall_run("pit_kerb", (-30.0, 30.0), (0, -1), (1, 0), 5, RIM - 0.6, "Pit")
    wall_run("pit_kerb", (30.0, -30.0), (0, 1), (-1, 0), 5, RIM - 0.6, "Pit")
    wall_run("pit_kerb", (-30.0, -30.0), (1, 0), (0, 1), 5, RIM - 0.6, "Pit")

# ---- 3. the open front: a rock cliff, the fall, two guardian heads spouting, the sacred pool
CLIFF_FOOT = -46.5
wall_run("cliff_wall", (30.0, 31.0), (-1, 0), (0, 1), 5, CLIFF_FOOT, "Pit", sz=1.35)
place("fall_medium", 0.0, 32.5, POOL[5] - 0.2, face=(0, 1), scale=1.8, grp="Water")
for sx in (-1, 1):
    place("bulul_spout", sx * 13.0, 32.0, -39.0, face=(0, 1), scale=1.6, grp="Water")
    place("fall_small", sx * 13.0, 33.0, POOL[5] - 0.2, face=(0, 1), scale=1.6, grp="Water")
# the gorge opens into the authored sacred pool (tools/payo_pieces/pool_build.py: stone rim, outlet, its own water)
POOL_PIECE = os.path.exists(os.path.join(CALM, "court", "pool_basin.glb")) and "--old-pool" not in sys.argv
if POOL_PIECE:
    place("pool_basin", 0.0, 0.0, 0.0, yaw=0.0, grp="Water")
    if bpy.data.objects.get("Sacred pool"):
        bpy.data.objects.remove(bpy.data.objects["Sacred pool"], do_unlink=True)
for k, ang in enumerate(range(200, 341, 28) if not POOL_PIECE else ()):
    a = math.radians(ang)
    x, y = POOL[0] + 24.5 * math.cos(a), POOL[1] + 24.5 * math.sin(a)
    if abs(x) > 20 or y > POOL[1]:
        place("boulder_%d" % (1 + k % 5), x, y, ground(x, y) - 0.4, yaw=a, scale=1.4, grp="Water")

# ---- 4. the gate behind the court, facing it; its guardians at the foot of its stair
# The shrine stands HIGH and LARGE at the valley's head (owner, 2026-10-18: "the shrine should be placed taller than the
# play area, and larger"), on the cliff top above the great fall, the stream leaving from under it over the lip.
SHRINE_Y, SHRINE_SCALE = -239.0, 1.6
place("gate_guardians", 0.0, -39.0, RIM, face=(0, 1), grp="Gate")
for sx in (-1, 1):
    place("banner", sx * 13.5, -42.0, RIM, face=(0, 1), grp="Gate")
    place("fire_bowl", sx * 33.5, -33.5, RIM, face=(0, 1), grp="Gate")
    place("fire_bowl", sx * 33.5, 22.0, RIM, face=(0, 1), grp="Gate")
    place("torch", sx * 7.5, -36.0, RIM, face=(0, 1), grp="Gate")
place("gong_rack", 20.0, -40.0, RIM, face=(-0.3, 1), grp="Gate")
place("offering_table", -19.0, -39.0, RIM, face=(0.3, 1), grp="Gate")
place("tapuy_jar_large", -22.5, -40.5, RIM, face=(0.3, 1), grp="Gate")
place("tapuy_jar_small", -16.0, -41.0, RIM, face=(0, 1), grp="Gate")


# ---- 5. the old watchers: two giant bulul on the rim either side of the court, facing it
for sx in (-1, 1):
    place("bulul_seated", sx * 41.0, 6.0, RIM, face=(-sx, -0.15), grp="Guardians")
    place("lantern_pole", sx * 38.0, 16.0, RIM, face=(-sx, 0), grp="Guardians")

# ---- 6. the town: houses on the terrace shelves at the valley's head, behind and beside the court, facing it
placed = [(0.0, -50.0, 14.0), (0.0, 0.0, 44.0), (POOL[0], POOL[1], 30.0)]
KINDS = ["house_tower", "house_shrine", "house_tiered", "house_long", "house_gong", "house_granary"]
# ---- 3b. the court's PLATFORM (cut level by tools/payo_land.py): its outer faces dressed in the same carved stone
# courses as the pit, so from the valley the court reads as one built thing; the gorge the fall drops through too.
PLAT_X, PLAT_S, PLAT_N = 44.0, -40.0, 56.0
if not COURT_PIECE:
    for z in (RIM - 12.0, RIM - 24.0, RIM - 36.0):
        wall_run("pit_wall", (PLAT_X, PLAT_N), (0, -1), (1, 0), 8, z, "Platform")          # east face
        wall_run("pit_wall", (-PLAT_X, PLAT_S), (0, 1), (-1, 0), 8, z, "Platform")         # west face
        wall_run("pit_wall", (30.0, PLAT_N), (1, 0), (0, 1), 1, z, "Platform", length=14.0)  # the two front faces, gorge to corner
        wall_run("pit_wall", (-PLAT_X, PLAT_N), (1, 0), (0, 1), 1, z, "Platform", length=14.0)
    for z in (RIM - 12.0, RIM - 24.0, RIM - 36.0, RIM - 48.0):
        wall_run("pit_wall", (30.0, 32.0), (0, 1), (-1, 0), 2, z, "Platform")              # the gorge's sides
        wall_run("pit_wall", (-30.0, PLAT_N), (0, -1), (1, 0), 2, z, "Platform")
    wall_run("pit_kerb", (PLAT_X, PLAT_N), (0, -1), (1, 0), 8, RIM - 0.6, "Platform")
    wall_run("pit_kerb", (-PLAT_X, PLAT_S), (0, 1), (-1, 0), 8, RIM - 0.6, "Platform")
    wall_run("pit_kerb", (30.0, PLAT_N), (1, 0), (0, 1), 1, RIM - 0.6, "Platform", length=14.0)
    wall_run("pit_kerb", (-PLAT_X, PLAT_N), (1, 0), (0, 1), 1, RIM - 0.6, "Platform", length=14.0)
    wall_run("pit_kerb", (30.0, 32.0), (0, 1), (-1, 0), 2, RIM - 0.6, "Platform")
    wall_run("pit_kerb", (-30.0, PLAT_N), (0, -1), (1, 0), 2, RIM - 0.6, "Platform")
# two standing guardians at the platform's front corners, looking out down the valley, and lanterns along its sides
for sx in (-1, 1):
    place("bulul_standing", sx * 37.5, 49.0, RIM, face=(0, 1), grp="Platform")
    for y in (36.0, -14.0):
        place("lantern_pole", sx * 41.0, y, RIM, face=(-sx, 0), grp="Platform")

sinks = {}


def sink_for(name):
    """How far a piece stands into flat ground so that none of its upward faces near its foot (floors, plinths, stilt
    pads) lies within 4 cm of the ground: two faces that close flicker (z-fighting). Worked out from its own geometry."""
    if name in sinks:
        return sinks[name]
    zs = []
    for o in lib(name).all_objects:
        if o.type != "MESH":
            continue
        nm = o.matrix_world.to_3x3()
        for pf in o.data.polygons:
            if (nm @ pf.normal).normalized().z > 0.95:
                z = (o.matrix_world @ pf.center).z
                if -0.6 < z < 0.6:
                    zs.append(z)
    sinks[name] = next((sv for sv in np.arange(0.04, 0.5, 0.01) if all(abs(z - sv) > 0.04 for z in zs)), 0.06)
    return sinks[name]



def level_under(x, y, r, z=None):
    """True when the ground is level (within 0.3 m) all round a circle of radius r, and at height z if given."""
    hs = [ground(x + r * math.cos(a), y + r * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 9)[:-1]] + [ground(x, y)]
    if max(hs) - min(hs) > 0.3:
        return False
    return z is None or abs(hs[-1] - z) < 0.3


HOUSE_SCALE = 1.45    # owner: the houses "feel too sparse and small"
RADIUS = {"house_tower": 6.5, "house_shrine": 8.5, "house_tiered": 7.5, "house_long": 9.5, "house_gong": 5.0, "house_granary": 5.0}


def free(x, y, r):
    return all(math.hypot(x - px, y - py) > r + pr + 2.0 for px, py, pr in placed)


def flat_spot(tx, ty, r, search=26.0, step=3.0, avoid_river=16.0):
    best = None
    for dx in np.arange(-search, search + 0.1, step):
        for dy in np.arange(-search, search + 0.1, step):
            x, y = tx + dx, ty + dy
            if not free(x, y, r) or river_dist(x, y) < avoid_river + r:
                continue
            hs = [ground(x + r * 0.7 * math.cos(a), y + r * 0.7 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 9)[:-1]]
            hs.append(ground(x, y))
            spread = max(hs) - min(hs)
            score = spread + 0.02 * math.hypot(dx, dy)
            if spread < 0.5 and (best is None or score < best[0]):
                best = (score, x, y, max(hs) - min(0.3, spread * 0.5))
    return best


def house(kind, tx, ty, look_at=(0.0, 0.0), grp="Town", **kw):
    r = RADIUS[kind] * HOUSE_SCALE
    spot = flat_spot(tx, ty, r, **kw)
    if spot is None:
        print("NO SPOT for", kind, "near", tx, ty)
        return None
    _, x, y, z = spot
    placed.append((x, y, r))
    sc_h = HOUSE_SCALE * random.uniform(0.95, 1.05)
    place(kind, x, y, z - sink_for(kind) * sc_h, face=towards(x, y, *look_at), scale=sc_h, grp=grp)
    return x, y, z


random.seed(7)
# The town stands on its own level platforms (cut by tools/payo_land.py), every house facing the court; the kinds take
# turns so no two neighbours match, and each yard gets a tree and a thing of daily life.
town = 0
# (owner, 2026-10-17: "there's also no organization/cleanliness in the layout of the different structures. they're not
# looking good together".) The houses stand in ROWS along the town's streets (level terraces round the valley's head, cut
# by tools/payo_land.py), evenly spaced, each facing straight down the slope to the court, in a set order of kinds so a
# street reads as one designed thing. No scattered yard clutter: one tidy item at a doorstep now and then, and a tree
# between every second pair of houses on the street's back edge.
STREET_KINDS = [["house_tiered", "house_granary"], ["house_tower", "house_granary"], ["house_long", "house_gong"]]
STREETS = sorted({round(float(s_[3]), 1) for s_ in data["sites"]})
STREET_KIND = {z: STREET_KINDS[min(i, 2)] for i, z in enumerate(STREETS)}
for k, (sx, sy, sr, sz_) in enumerate(data["sites"]):
    sx, sy, sz_ = float(sx), float(sy), float(sz_)
    down = (-sx, -sy)                                       # every house faces the court
    n = math.hypot(*down) or 1.0
    down = (down[0] / n, down[1] / n)
    if abs(sx) < 48.0 and sy < -150.0:                      # the head's works (basin, guardians) keep their ground
        continue
    kinds = STREET_KIND.get(round(sz_, 1), ["house_tower", "house_granary"])
    # the house must stand wholly on its street: where the street is too narrow for this kind, a smaller one, else none
    kind = next((kd for kd in (kinds[k % 2], kinds[(k + 1) % 2], "house_granary", "house_gong")
                 if level_under(sx, sy, RADIUS[kd] * HOUSE_SCALE * 0.62, sz_)), None)
    if kind is None:
        print("street too narrow for a house at", round(sx), round(sy), sz_)
        continue
    placed.append((sx, sy, RADIUS[kind] * HOUSE_SCALE))
    place(kind, sx, sy, sz_ - sink_for(kind) * HOUSE_SCALE, face=down, scale=HOUSE_SCALE, grp="Town")
    town += 1
    if k % 2 == 0:                                           # a tree on the street's back edge, between houses
        tx, ty = sx - down[0] * 7.0 + down[1] * 11.5, sy - down[1] * 7.0 - down[0] * 11.5
        if level_under(tx, ty, 1.5, None):
            place(["tree_round", "tree_lean_olive", "tree_broad_lime"][k % 3], tx, ty, ground(tx, ty) - 0.2, yaw=k * 1.7, scale=1.15, grp="Town")
    if k % 3 == 1:                                           # one tidy thing by the door, on the street
        dx, dy = sx + down[0] * 6.5 + down[1] * 3.0, sy + down[1] * 6.5 - down[0] * 3.0
        if level_under(dx, dy, 1.2, sz_):
            item = ["tapuy_jar_large", "basket_stack", "drying_rack"][k % 3]
            place(item, dx, dy, sz_ - sink_for(item), face=down, grp="Town")
print("town houses", town)

# the great fall drops off the head's cliff (a horseshoe of rock, tools/payo_land.py) into the round plunge pool, on the
# court's axis behind the gate: its back sits in the cliff, its lip on the cliff top, its foam out in the pool
BASIN, CLIFF = data["basin"], data["cliff"]
HC = ((float(BASIN[0]) + float(BASIN[1])) / 2, (float(BASIN[2]) + float(BASIN[3])) / 2)
BR = (float(BASIN[1]) - float(BASIN[0])) / 2                 # the pool's rim radius
HEAD_PIECE = os.path.exists(os.path.join(CALM, "head", "head_fall.glb")) and "--old-head" not in sys.argv
if HEAD_PIECE:
    # the authored head (cliff, fall, pool, kerb, outlet; origin at the pool's middle, heights are the map's own)
    place("head_fall", HC[0], HC[1], 0.0, yaw=0.0, grp="Water")
    if bpy.data.objects.get("Basin water"):
        bpy.data.objects.remove(bpy.data.objects["Basin water"], do_unlink=True)    # the piece has its own pool
else:
    lip_z = ground(HC[0], HC[1] - BR - 5.0)
    place("fall_great", HC[0], HC[1] - BR + 0.6, float(BASIN[4]) + 0.06, face=(0, 1), scale=1.0,
          sz=(lip_z + 0.6 - float(BASIN[4])) / 43.4, grp="Water")
for sx in (-1, 1):                                           # two guardians watch the pool from its north rim
    gx, gy = next(((sx * gx_, float(BASIN[3]) + gy_) for gy_ in (4.0, 7.0, 1.0, 10.0) for gx_ in (24.0, 20.0, 28.0, 16.0, 32.0)
                   if level_under(sx * gx_, float(BASIN[3]) + gy_, 2.0)), (sx * 24.0, float(BASIN[3]) + 4.0))
    place("bulul_standing", gx, gy, ground(gx, gy) - sink_for("bulul_standing"), face=(0, 1), grp="Guardians")
for a_ in (25.0, 55.0, 125.0, 155.0):                         # lanterns round the pool's open (north) rim
    lx, ly = HC[0] + (BR + 2.5) * math.cos(math.radians(a_)), HC[1] + (BR + 2.5) * math.sin(math.radians(a_))
    place("lantern_pole", lx, ly, ground(lx, ly), face=(HC[0] - lx, HC[1] - ly), grp="Water")
for k, y in enumerate(np.arange(-184.0, -70.0, 24.0)):       # lanterns along the water stair
    for sx in (-1, 1):
        place("lantern_pole", sx * 8.0, y, ground(sx * 8.0, y), face=(-sx, 0), grp="Water")
placed.append((0.0, -130.0, 10.0)); placed.append((HC[0], HC[1], BR + 6.0))
# the shrine, on the cliff top behind the lip
sz_shrine = 74.1                             # on the cliff top behind the lip: the stream runs out from under it and over the fall
place("gate_portal", 0.0, SHRINE_Y, sz_shrine, face=(0, 1), scale=SHRINE_SCALE, grp="Gate")
placed.append((0.0, SHRINE_Y, 22.0))
# the water stair ends in a stone-lipped pool at the plaza's back edge, behind the gate
if not COURT_PIECE:
    wall_run("pit_kerb", (-6.0, -62.2), (1, 0), (0, -1), 1, RIM - 0.45, "Water")
# the water stair spills over the court piece's south sill into its sunken inlet basin (water at -7.6)
if COURT_PIECE:
    iv = [(-4.0, -60.2, -7.6), (4.0, -60.2, -7.6), (4.0, -57.5, -7.6), (-4.0, -57.5, -7.6)]
    im_ = bpy.data.meshes.new("inlet water"); im_.from_pydata(iv, [], [(0, 1, 2, 3)]); im_.update()
    im_.materials.append(bpy.data.materials["river"])
    group("Water").objects.link(bpy.data.objects.new("Inlet water", im_))
    # the thin sheet over the weir, from its lip down into the pool
    wv = [(-2.9, -62.5, -6.92), (2.9, -62.5, -6.92), (2.9, -60.9, -6.93), (-2.9, -60.9, -6.93),
          (-2.9, -60.75, -7.25), (2.9, -60.75, -7.25), (2.9, -60.62, -7.62), (-2.9, -60.62, -7.62)]
    wm_ = bpy.data.meshes.new("weir water"); wm_.from_pydata(wv, [], [(0, 1, 2, 3), (3, 2, 5, 4), (4, 5, 6, 7)]); wm_.update()
    wm_.materials.append(bpy.data.materials.get("river rapids") or bpy.data.materials["river"])
    group("Water").objects.link(bpy.data.objects.new("Weir water", wm_))
# the gorge holds the pool's water wall to wall (a disc alone left dark earth banks between it and the carved walls)
if not POOL_PIECE:
    gv = [(-29.6, 30.4, POOL[5] - 0.06), (29.6, 30.4, POOL[5] - 0.06), (29.6, 56.0, POOL[5] - 0.06), (-29.6, 56.0, POOL[5] - 0.06)]
    gm = bpy.data.meshes.new("gorge water"); gm.from_pydata(gv, [], [(0, 1, 2, 3)]); gm.update()
    gm.materials.append(bpy.data.materials["river"])
    group("Water").objects.link(bpy.data.objects.new("Gorge water", gm))

# ---- 7. hamlets along the river, on the terrace shelves, facing the water
HAMLETS = [(-80, 230), (88, 300), (-96, 400), (96, 520), (-80, 640), (120, 700), (-150, 820), (40, 930)]
for k, (tx, ty) in enumerate(HAMLETS):
    near = water[np.argmin(np.hypot(water[:, 0] - tx, water[:, 1] - ty))]
    for j, (ox, oy) in enumerate(((0, 0), (14, 10), (-12, 12))):
        kind = ["house_granary", "house_long", "house_tiered", "house_tower"][(k + j) % 4] if j else ["house_long", "house_granary"][k % 2]
        got = house(kind, tx + ox, ty + oy, look_at=(near[0], near[1]), grp="Hamlets", search=40.0)
        if got and j == 0:
            x, y, z = got
            for prop, off in (("scarecrow", (10, -8)), ("drying_rack", (-9, -9))):
                px, py = x + off[0], y + off[1]
                if free(px, py, 2.0) and level_under(px, py, 1.5):
                    place(prop, px, py, ground(px, py), face=(near[0] - px, near[1] - py), grp="Hamlets")

# ---- 8. lanterns along the river path, every 60 m, on alternate banks
for k in range(4, len(water), 30):
    x, y, z = water[k]
    if y > 900:
        break
    nx, ny = (water[k + 1][1] - y, -(water[k + 1][0] - x)) if k + 1 < len(water) else (1, 0)
    n = math.hypot(nx, ny) or 1.0
    side = 1 if (k // 30) % 2 else -1
    px, py = x + side * nx / n * 14.0, y + side * ny / n * 14.0
    place("lantern_pole", px, py, ground(px, py), face=(x - px, y - py), grp="River")

# ---- 9. forest, palms and bushes on the slopes the rice does not take
rng = random.Random(11)
trees = 0
# Groves, not a sprinkle: trees stand where a broad noise says forest, close together, with open glades between; pines
# on the high ground, round broadleaf trees on the middle slopes, palms low near the paddies (the earlier maps' own trees).
for _ in range(26000):
    x, y = rng.uniform(-720.0, 720.0), rng.uniform(-520.0, 1500.0)
    r = math.hypot(x, y)
    if r < 62.0:
        continue
    t, s_ = field(Tg, x, y), field(Sg, x, y)
    if t > 0.05 or s_ > 1.25 or (s_ < 0.30 and r > 260.0):
        continue
    grove = math.sin(x / 83.0 + 1.3) * math.cos(y / 71.0) + 0.55 * math.sin((x - y) / 47.0) + 0.3 * math.cos(x / 23.0 + y / 29.0)
    if grove < 0.35:
        continue
    if not free(x, y, 2.6) or river_dist(x, y) < 18.0:
        continue
    placed.append((x, y, 1.2))
    z = ground(x, y)
    if s_ > 0.72:
        kind, sc_ = "tree_tall_deep", rng.uniform(2.0, 2.8)
    elif s_ > 0.45:
        kind, sc_ = rng.choice(["tree_round", "tree_broad_lime", "tree_lean_olive", "tree_tall_deep"]), rng.uniform(1.7, 2.4)
    else:
        kind, sc_ = rng.choice(["payo_plant_palm_0", "tree_round", "tree_broad_lime"]), rng.uniform(1.3, 1.9)
    place(kind, x, y, z - 0.3, yaw=rng.uniform(0, 6.28), scale=sc_, grp="Trees")
    trees += 1
# Woods round the court's slopes: wherever the ground near the court is neither terraced nor town, it is forest, and the
# forest runs thick along the terraces' ends so they finish in trees (as real terraces do), not in a ragged edge.
TWg = data["TW"].astype(np.float32) if "TW" in data else np.zeros_like(Tg)
for _ in range(16000):
    x, y = rng.uniform(-460.0, 460.0), rng.uniform(-320.0, 460.0)
    r = math.hypot(x, y)
    if r < 60.0 or (abs(x) < 48.0 and -44.0 < y < 62.0):
        continue
    t, s_, tw = field(Tg, x, y), field(Sg, x, y), field(TWg, x, y)
    if t > 0.05 or tw > 0.7 or s_ > 1.25:
        continue
    edge = max(field(Tg, x + dx, y + dy) for dx, dy in ((8, 0), (-8, 0), (0, 8), (0, -8), (6, 6), (-6, -6), (6, -6), (-6, 6)))
    edge = max(edge, tw * 2.0, max(field(TWg, x + dx, y + dy) for dx, dy in ((8, 0), (-8, 0), (0, 8), (0, -8))))  # and along the town's edge
    wood = math.sin(x / 61.0 + 0.4) * math.cos(y / 53.0 + 1.1) + 0.4 * math.sin((x + y) / 37.0)
    if edge < 0.3 and wood < 0.35:
        continue
    if not free(x, y, 2.3) or river_dist(x, y) < 14.0:
        continue
    placed.append((x, y, 1.0))
    kind = rng.choice(["tree_round", "tree_broad_lime", "tree_lean_olive", "tree_round", "tree_tall_deep"])
    place(kind, x, y, ground(x, y) - 0.3, yaw=rng.uniform(0, 6.28), scale=rng.uniform(1.6, 2.3), grp="Trees")
    trees += 1
print("placed %d trees" % trees)

# ---- 9. where things meet the ground (owner, 2026-10-18: "it all looks so unpolished compared to the maps we've made
# in the past"). The other maps never let a wall, a rim or a house stand on bare ground: plants and stones gather at
# their feet. Clumps of the land kit's own ground cover (fern, taro, grass) and a stone now and then, in small groups.
crng = random.Random(23)
COVER = ["cover_fern", "cover_taro", "cover_grass", "cover_fern", "cover_grass"]


def clump(x, y, n=3, spread=1.6, scale=(1.4, 2.2), stone=0.15, grp="Cover"):
    """A small group of ground cover round (x, y), each on the ground; now and then a stone among them."""
    for _ in range(n):
        px, py = x + crng.uniform(-spread, spread), y + crng.uniform(-spread, spread)
        if river_dist(px, py) < 9.0:
            continue
        g = ground(px, py)
        if crng.random() < stone:
            place("boulder_%d" % crng.randint(1, 5), px, py, g - 0.35, yaw=crng.uniform(0, 6.28), scale=crng.uniform(0.5, 0.9), grp=grp)
        else:
            place(crng.choice(COVER), px, py, g - 0.05, yaw=crng.uniform(0, 6.28), scale=crng.uniform(*scale), grp=grp)


covers = 0
# the platform's outer walls, at their foot in the terraces
for (x0, y0, x1, y1, nx, ny) in [(44.0, -62.0, 44.0, 56.0, 1, 0), (-44.0, -62.0, -44.0, 56.0, -1, 0),
                                 (30.0, 56.0, 44.0, 56.0, 0, 1), (-44.0, 56.0, -30.0, 56.0, 0, 1)]:
    L_ = math.hypot(x1 - x0, y1 - y0)
    for t in np.arange(1.5, L_, 4.5):
        fx, fy = x0 + (x1 - x0) * t / L_ + nx * 2.2, y0 + (y1 - y0) * t / L_ + ny * 2.2
        if ground(fx, fy) < RIM - 1.0 and crng.random() < 0.8:
            clump(fx, fy, n=crng.randint(2, 4)); covers += 1
# round the two pools' rims
for (cx_, cy_, rr) in [(float(POOL[0]), float(POOL[1]), float(POOL[3]) + 1.5), (HC[0], HC[1], BR + 2.0)]:
    for a_ in np.arange(0.0, 360.0, 14.0):
        fx, fy = cx_ + rr * math.cos(math.radians(a_)), cy_ + rr * math.sin(math.radians(a_))
        if abs(fx) < 31.0 and fy < cy_ and cy_ > 0:      # the sacred pool's gorge side is the cliff, not ground
            continue
        if crng.random() < 0.7:
            clump(fx, fy, n=crng.randint(2, 3), spread=1.2); covers += 1
# at the feet of houses, two clumps beside each, toward its sides
for o in list(group("Town").objects) + list(group("Hamlets").objects):
    if not o.instance_collection or not o.instance_collection.name[4:].startswith("house_"):
        continue
    r_ = RADIUS.get(o.instance_collection.name[4:], 6.0)
    for side in (-1, 1):
        a_ = o.rotation_euler.z + side * 1.3
        fx, fy = o.location.x + math.cos(a_) * r_ * 0.9, o.location.y + math.sin(a_) * r_ * 0.9
        if level_under(fx, fy, 0.8):
            clump(fx, fy, n=2, spread=0.9, scale=(1.1, 1.7), stone=0.0); covers += 1
print("ground cover clumps", covers)

# ---- 10. the platform's side terraces, where people would watch the game: carved posts, benches, jars, between the
# lanterns and the seated guardians; the court's own edge (the pit) stays clear
for sx in (-1, 1):
    for k, y in enumerate((-24.0, -4.0, 18.0, 30.0)):
        x = sx * 37.5
        if k % 2 == 0:
            place("hagabi_bench", x, y, RIM, face=(-sx, 0), grp="Platform")
        else:
            place("bulul_post", x - sx * 1.5, y, RIM, face=(-sx, 0), grp="Platform")
        place(["tapuy_jar_large", "tapuy_jar_small", "basket_stack", "tapuy_jar_open"][k], x + sx * 3.2, y + 2.5, RIM, face=(-sx, 0), grp="Platform")

# ---- 11. flora (owner, 2026-10-18: "i think this place should have more foliage and flora"). The earlier maps' own
# plants: Lagoon's gumamela bushes, croton, monstera, grass tufts and ground cover, Payo's banana and taro, the fig trees.
frng = random.Random(41)
UNDER = ["plant_bush_0", "plant_bush_1", "plant_bush_2", "plant_bush_3", "plant_croton_0", "plant_croton_2",
         "plant_monstera_0", "plant_monstera_2", "plant_tuft_0", "plant_tuft_1", "plant_tuft_2", "plant_groundcover_1",
         "plant_groundcover_3", "payo_plant_broadleaf_0", "cover_taro", "cover_fern", "plant_tuft_3", "plant_bush_1"]
GARDEN = ["payo_plant_broadleaf_0", "payo_plant_broadleaf_0", "plant_bush_0", "plant_bush_2", "plant_croton_1",
          "plant_croton_3", "plant_monstera_1", "cover_taro", "plant_groundcover_0", "plant_tuft_1"]
taken = set()


def room(x, y, cell=2.2):
    """A coarse grid of taken spots for the many small plants (the house list check is for big things)."""
    k = (int(math.floor(x / cell)), int(math.floor(y / cell)))
    if k in taken:
        return False
    taken.add(k)
    return True


def near_built(x, y, r):
    return any(abs(x - px) < r + pr and abs(y - py) < r + pr and math.hypot(x - px, y - py) < r + pr for px, py, pr in placed if pr >= 3.0)


flora = 0
# a) understorey on the unterraced slopes round the court, the town and the valley, in drifts, thickest at the forest
for _ in range(36000):
    x, y = frng.uniform(-540.0, 540.0), frng.uniform(-340.0, 950.0)
    if math.hypot(x, y) < 52.0 or (abs(x) < 47.0 and -66.0 < y < 60.0):
        continue
    t, s_, tw = field(Tg, x, y), field(Sg, x, y), field(TWg, x, y)
    if t > 0.05 or tw > 0.3 or s_ > 1.15:
        continue
    drift = math.sin(x / 37.0 + 0.7) * math.cos(y / 41.0) + 0.5 * math.sin((x + 2.0 * y) / 29.0)
    edge = max(field(Tg, x + 7, y), field(Tg, x - 7, y), field(Tg, x, y + 7), field(Tg, x, y - 7))
    if drift < 0.15 and edge < 0.3:
        continue
    if river_dist(x, y) < 10.0 or not room(x, y) or near_built(x, y, 1.5):
        continue
    kind = frng.choice(UNDER)
    place(kind, x, y, ground(x, y) - 0.08, yaw=frng.uniform(0, 6.28), scale=frng.uniform(1.2, 2.1), grp="Flora")
    flora += 1
# b) the town's garden tiers: banana, bushes, croton, taro, a fig tree now and then
STREET_LEVELS = sorted({round(float(s_[3]), 1) for s_ in data["sites"]})
figs = 0
for _ in range(9000):
    x, y = frng.uniform(-240.0, 240.0), frng.uniform(-240.0, 10.0)
    if field(TWg, x, y) < 0.8 or (abs(x) < 47.0 and -66.0 < y < 60.0):     # not on the court's own platform
        continue
    g = ground(x, y)
    if min(abs(g - z) for z in STREET_LEVELS) < 0.6 or not level_under(x, y, 1.6):
        continue
    if not room(x, y, 2.6) or near_built(x, y, 2.0):
        continue
    if frng.random() < 0.035 and level_under(x, y, 4.0) and not near_built(x, y, 6.0):
        place(frng.choice(["tree_fig_a", "tree_fig_b", "tree_fig_c"]), x, y, g - 0.2, yaw=frng.uniform(0, 6.28), scale=frng.uniform(0.9, 1.2), grp="Flora")
        placed.append((x, y, 3.5)); figs += 1
    else:
        place(frng.choice(GARDEN), x, y, g - 0.08, yaw=frng.uniform(0, 6.28), scale=frng.uniform(1.1, 1.8), grp="Flora")
    flora += 1
# c) a flowering bush by every house's door, and two on each of the platform's side terraces' outer corners
for o in list(group("Town").objects) + list(group("Hamlets").objects):
    if not o.instance_collection or not o.instance_collection.name[4:].startswith("house_"):
        continue
    r_ = RADIUS.get(o.instance_collection.name[4:], 6.0)
    a_ = o.rotation_euler.z - math.pi / 2 + frng.choice((-0.55, 0.55))      # in front, to one side of the door
    fx, fy = o.location.x + math.cos(a_) * (r_ + 1.2), o.location.y + math.sin(a_) * (r_ + 1.2)
    if level_under(fx, fy, 0.8):
        place(frng.choice(["plant_bush_0", "plant_bush_2"]), fx, fy, ground(fx, fy) - 0.08, yaw=frng.uniform(0, 6.28), scale=1.3, grp="Flora")
        flora += 1
print("flora", flora, "figs", figs)

# ---- cameras for this pass
cams = {}


def camera(name, at, to, lens=24):
    cam = bpy.data.objects.new(name, bpy.data.cameras.new(name)); sc.collection.objects.link(cam)
    cam.location = at
    cam.rotation_euler = (Vector(to) - Vector(at)).to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = lens; cam.data.clip_end = 14000
    cams[name] = cam


for o in list(bpy.data.objects):
    if o.type == "CAMERA":
        cams[o.name] = o
camera("eye_court_looking_south", (0, 12, 1.7), (0, -80, 10), 20)
camera("eye_gate_stair_to_court", (0, -40, -2.5), (0, 40, -10), 20)
camera("town_from_the_east", (170, 70, 170), (-20, -100, 20), 26)
camera("valley_to_court", (0, 260, 40), (0, 0, -10), 28)
camera("corner_from_valley", (-90, 110, 5), (-30, 25, -20), 24)
camera("pit_front_and_pool", (-40, 120, -20), (0, 40, -36), 24)
camera("eye_court_looking_east", (-8, 0, 1.7), (160, 40, -10), 18)
camera("eye_court_looking_west", (8, 0, 1.7), (-160, 40, -10), 18)
cam = bpy.data.objects.new("top_court", bpy.data.cameras.new("top_court")); sc.collection.objects.link(cam)
cam.location = (0, -40, 600); cam.data.type = "ORTHO"; cam.data.ortho_scale = 440; cam.data.clip_end = 3000; cams["top_court"] = cam
camera("town_from_above_south", (0, -420, 420), (0, -60, -10), 30)
camera("head_from_the_court_rim", (0, -36, -4.3), (0, -230, 70), 22)
sc.camera = cams.get("eye_court_front_edge") or list(cams.values())[0]

bpy.context.view_layer.layer_collection.children[lib_root.name].exclude = True    # the originals stay out of view; their instances show
path = os.path.join(ROOT, "ArtSource", "payo", "payo_dressed_%s.blend" % TAG)
bpy.ops.wm.save_as_mainfile(filepath=path, compress=True)
for name, cam in cams.items():
    if ONLY and name not in ONLY:
        continue
    sc.camera = cam
    sc.render.filepath = os.path.join(OUT, "dressed_%s_%s.png" % (TAG, name))
    bpy.ops.render.render(write_still=True)
print("SAVED", path)
