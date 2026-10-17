"""PAYO ASSEMBLY, the checker. Run inside Blender after `payo_assemble.py land`:

  blender -b --python tools/payo_assemble_check.py

It rays down onto the LAND MESHES AS EXPORTED (not the height function) and reads the pieces' real bounds:
  1. floating or buried pieces (ground pieces against the land; terrace walls at three points of their foot;
     houses at their four corners; things on a ledge, a bund, a niche or a block against that surface's height);
  2. overlaps: building against building, a prop or a plant inside a building, prop against prop, a terrace wall
     under a building;
  3. anything inside the play volume (the 44 m square over the court) or hanging over the pit that is not the pit's own;
  4. anything across the sight line from the can (and the three attacker spawns) to the gate portal's face, tested
     against the real triangles of every piece near that line;
  5. two houses of one kind side by side.
Writes Logs/payo/assembly/check.txt and prints the count of failures: it must be 0.
"""
import json
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import payo_assemble_data as D                      # noqa: E402

GEN = os.path.join(D.WORK, "gen")
fails = []
notes = []


def fail(kind, p, text):
    fails.append("%-9s %-24s at (%.1f, %.1f, %.1f): %s" % (kind, p["model"], p["x"], p["y"], p["z"], text))


def source(model):
    for d in [os.path.join(D.APPROVED, f) for f in ("stone", "gate", "bulul", "house", "prop", "terrace")] + [D.LATE_STONE, D.LATE_TERRACE, os.path.join(D.WORK, "kept"), GEN]:
        q = os.path.join(d, model + ".glb")
        if os.path.exists(q):
            return q


_cache = {}


def mesh_tris(model, M):
    if model not in _cache:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=source(model))
        verts, tris = [], []
        for o in [o for o in bpy.data.objects if o not in before]:
            if o.type != "MESH":
                continue
            W = o.matrix_world
            base = len(verts)
            verts += [W @ v.co for v in o.data.vertices]
            o.data.calc_loop_triangles()
            tris += [tuple(base + i for i in t.vertices) for t in o.data.loop_triangles]
        _cache[model] = (verts, tris)
    verts, tris = _cache[model]
    return [M @ v for v in verts], tris


def placed(p):
    return Matrix.Translation((p["x"], p["y"], p["z"])) @ Matrix.Rotation(math.pi - math.radians(p["yaw"]), 4, "Z") @ Matrix.Scale(p["scale"], 4)


bpy.ops.wm.read_factory_settings(use_empty=True)
out = D.build()
P = out["placements"]
verts, tris = [], []
for name in ["payo_land_%s_%s" % (a, b) for a in ("near", "far") for b in ("ne", "se", "sw", "nw")]:
    v, t = mesh_tris(name, Matrix.Rotation(math.pi, 4, "Z"))
    tris += [tuple(len(verts) + i for i in tri) for tri in t]; verts += v
land = BVHTree.FromPolygons([tuple(v) for v in verts], tris)


def land_z(x, y):
    hit = land.ray_cast(Vector((x, y, 900.0)), Vector((0, 0, -1)))
    return hit[0].z if hit[0] is not None else None


def sat(a, b):
    for poly in (a, b):
        for i in range(len(poly)):
            x0, y0 = poly[i]; x1, y1 = poly[(i + 1) % len(poly)]
            nx, ny = y1 - y0, x0 - x1
            pa = [q[0] * nx + q[1] * ny for q in a]; pb = [q[0] * nx + q[1] * ny for q in b]
            if max(pa) <= min(pb) or max(pb) <= min(pa):
                return False
    return True


BUILD = ("house", "gate")
BIG = ("bulul_seated", "bulul_standing", "fall_great")
seated = {"ground": 0, "terrace wall": 0, "by rule": 0}

# ---- 1. floating or buried
for p in P:
    fam = p["family"]
    if p.get("on_block"):
        e = out["blocks"][p["on_block"]]
        top = next(iter(e["tops"].values()))
        if abs(p["z"] - top) > 0.02:
            fail("FLOAT", p, "is %.2f m off its block's top" % (p["z"] - top))
        inx = e["sx"] / 2 - abs(p["x"] - e["x"]); iny = e["sz"] / 2 - abs(p["y"] - e["z"])
        r = max(-D.CAT[p["model"]]["lo"][0], D.CAT[p["model"]]["hi"][0]) * p["scale"]
        if min(inx, iny) < r - 0.05:
            fail("BLOCK", p, "overhangs its block's top")
        if max(inx, iny) > 1.6:
            fail("BLOCK", p, "stands %.1f m in from the block's rim: in the way of a run" % max(inx, iny))
        seated["by rule"] += 1
        continue
    if fam == "terrace":
        fx, fy = D.front(p["yaw"]); rx, ry = D.run(p["yaw"])
        bend = 1.0 if "convex" in p["model"] else -1.0 if "concave" in p["model"] else 0.0
        for along in (0.6, 5.5, 10.5) if "cap" not in p["model"] else (0.8,):
            side = -bend * along * along / 60.0                 # the 30 m curve leaves its tangent toward (convex) or away from (concave) the hill
            z = land_z(p["x"] + rx * along + fx * side, p["y"] + ry * along + fy * side)
            if z is None:
                fail("FLOAT", p, "its foot is over no land"); break
            if z < p["z"] - 0.12:
                fail("FLOAT", p, "its foot is %.2f m above the land" % (p["z"] - z)); break
            if z > p["z"] + 0.75:
                fail("BURIED", p, "the land is %.2f m up its 1.15 m wall" % (z - p["z"])); break
        seated["terrace wall"] += 1
        continue
    if p.get("wall") or p.get("on") or fam in ("pit", "cliff"):
        w = p.get("wall")
        if w in ("ledge",) and abs(p["z"] - D.LEDGE_Z) > 0.02:
            fail("FLOAT", p, "is not on the ledge")
        if w in ("spout", "panel", "ledge", "niche") and not (D.SHAFT - 0.5 <= max(abs(p["x"]), abs(p["y"])) <= D.HOLE + 1.3 or abs(abs(p["x"]) - D.NOTCH) < 1.5):
            fail("FLOAT", p, "is not on the pit's wall or ledge")
        seated["by rule"] += 1
        continue
    z = land_z(p["x"], p["y"])
    if z is None:
        fail("FLOAT", p, "stands over no land (the pit, or off the map)"); continue
    d = p["z"] - z
    if p["model"] == "rope_bridge":
        rx, ry = D.run(p["yaw"])
        ends = [land_z(p["x"] + s * 3.5 * rx, p["y"] + s * 3.5 * ry) for s in (-1, 1)]
        mid = z
        if any(e is None or abs(p["z"] - e) > 0.45 for e in ends):
            fail("FLOAT", p, "its ends are %s off the banks" % ["%.2f" % (p["z"] - e) for e in ends])
        if mid > p["z"] - 0.9:
            fail("BURIED", p, "the bed under its deck is only %.2f m down: no gully to span" % (p["z"] - mid))
        seated["ground"] += 1
        continue
    if fam == "water":
        fx, fy = D.front(p["yaw"])
        R = 9.0 if "great" in p["model"] else 3.0
        zs = [land_z(p["x"] + fx * R * t, p["y"] + fy * R * t) for t in (0.3, 0.7, 1.0)]
        if any(q is None or abs(q - p["z"]) > 0.6 for q in zs):
            fail("FLOAT", p, "the land before it is not at its pool: %s" % ["%.2f" % ((q or 999) - p["z"]) for q in zs])
        behind = land_z(p["x"] - fx * 7.0, p["y"] - fy * 7.0)
        tall = {"fall_great": 40.0, "fall_medium": 8.0, "fall_small": 2.0}[p["model"]] * p["scale"]
        if behind is None or not (tall - 1.0 <= behind - p["z"] <= tall + 6.5):
            fail("FLOAT", p, "the cliff behind it is %.1f m, the fall %.1f m" % ((behind or 0) - p["z"], tall))
    elif fam in BUILD or p["model"] in BIG:
        corners = D.footprint(p, -0.3)
        zs = [land_z(*c) for c in corners]
        if any(q is None for q in zs):
            fail("FLOAT", p, "a corner is over no land")
        else:
            foot = 1.5 * p["scale"] if fam == "house" else 0.9 if fam == "water" else 0.25
            if min(zs) < p["z"] - foot:
                fail("FLOAT", p, "the land under a corner is %.2f m below its base" % (p["z"] - min(zs)))
            if max(zs) > p["z"] + (0.6 if fam != "water" else 2.5):
                fail("BURIED", p, "the land at a corner is %.2f m above its base" % (max(zs) - p["z"]))
    elif fam == "tree":
        if p.get("rib"):
            if d > 0.3 or d < -3.2:
                fail("FLOAT" if d > 0 else "BURIED", p, "a rib's tree is %.2f m off the land" % d)
        elif d > 0.3:
            fail("FLOAT", p, "its root is %.2f m above the land" % d)
        elif d < -1.5:
            fail("BURIED", p, "is %.2f m into the land" % -d)
    elif d > 0.13:
        fail("FLOAT", p, "is %.2f m above the land" % d)
    elif d < -0.3:
        fail("BURIED", p, "is %.2f m into the land" % -d)
    seated["ground"] += 1

# ---- 2. overlaps
solid = [p for p in P if p["family"] in BUILD or p["model"] in BIG]
foot = {id(p): D.footprint(p, -0.35) for p in solid}
for i, a in enumerate(solid):
    for b in solid[i + 1:]:
        if sat(foot[id(a)], foot[id(b)]):
            fail("OVERLAP", a, "its footprint crosses %s at (%.1f, %.1f)" % (b["model"], b["x"], b["y"]))
small = [p for p in P if p["family"] in ("prop", "cover", "path", "terrace_prop") or (p["family"] == "bulul" and p["model"] not in BIG and not p.get("wall"))]
tight = {id(p): D.footprint(p, 0.0) for p in solid}
# a small thing against the REAL triangles of the buildings: rays down from just over its own top must reach the ground
sv, st, so = [], [], []
for b in solid:
    v, t = mesh_tris(b["model"], placed(b))
    st += [tuple(len(sv) + i for i in tri) for tri in t]; so += [b] * len(t); sv += v
solid_tree = BVHTree.FromPolygons([tuple(v) for v in sv], st)
for p in small:
    if p.get("wall") or p.get("on_block"):
        continue
    c = D.CAT[p["model"]]
    top = p["z"] + c["hi"][2] * p["scale"] + 0.25
    pts = [(p["x"], p["y"])] + [((q[0] - p["x"]) * 0.6 + p["x"], (q[1] - p["y"]) * 0.6 + p["y"]) for q in D.footprint(p)]
    for (x, y) in pts:
        hit = solid_tree.ray_cast(Vector((x, y, top)), Vector((0, 0, -1)), top - p["z"] - 0.12)
        if hit[0] is not None:
            b = so[hit[2]]
            if p["family"] == "path" and b["model"] == "gate_stair":
                continue
            fail("OVERLAP", p, "stands in %s at (%.1f, %.1f)" % (b["model"], b["x"], b["y"]))
            break
things = [p for p in small if p["family"] in ("prop", "terrace_prop", "bulul") and not p.get("on")]
tf = {id(p): [((q[0] - p["x"]) * 0.8 + p["x"], (q[1] - p["y"]) * 0.8 + p["y"]) for q in D.footprint(p)] for p in things}
for i, a in enumerate(things):
    for b in things[i + 1:]:
        if abs(a["x"] - b["x"]) < 6 and abs(a["y"] - b["y"]) < 6 and abs(a["z"] - b["z"]) < 2.0 and sat(tf[id(a)], tf[id(b)]):
            fail("OVERLAP", a, "its footprint crosses %s at (%.1f, %.1f)" % (b["model"], b["x"], b["y"]))
for p in P:
    if p["family"] == "terrace":
        rx, ry = D.run(p["yaw"])
        for along in ((1.0, 6.0, 11.0) if "cap" not in p["model"] else (1.0, 2.5, 4.5)):
            q = (p["x"] + rx * along, p["y"] + ry * along)
            for b in solid:
                if D._inside(tight[id(b)], *q):
                    fail("OVERLAP", p, "runs under %s" % b["model"]); break

# ---- 3. the play volume and the pit
for p in P:
    if p.get("on_block"):
        continue
    f = D.footprint(p)
    if any(abs(x) < D.PLAY and abs(y) < D.PLAY for (x, y) in f) or (abs(p["x"]) < D.PLAY and abs(p["y"]) < D.PLAY):
        fail("PLAY", p, "is inside the 44 m play square")
    if p["family"] not in ("pit",) and not p.get("wall"):
        c = [(q[0] * 0.9 + p["x"] * 0.1, q[1] * 0.9 + p["y"] * 0.1) for q in f]
        if any(abs(x) < D.HOLE - 0.05 and abs(y) < D.HOLE - 0.05 for (x, y) in c):
            fail("PIT", p, "hangs over the pit's edge")
        if p["family"] != "terrace" and any(abs(x) < D.NOTCH - 0.05 and 0 < y < 60 for (x, y) in c):
            fail("PIT", p, "hangs over the notch")

# ---- 4. the sight line to the gate's face
portal = next(p for p in P if p["model"] == "gate_portal")
target = Vector((0.0, portal["y"] + 3.0, D.RIM_Z + 11.5))                 # the sun disc over the doorway
door = Vector((0.0, portal["y"] + 4.0, D.RIM_Z + 4.5))                    # the doorway itself
near = [p for p in P if p is not portal and abs(p["x"]) < 14 and -60 < p["y"] < 12 and not p.get("wall") and p["family"] != "pit"]
bv, bt, owner = [], [], []
for p in near:
    v, t = mesh_tris(p["model"], placed(p))
    bt += [tuple(len(bv) + i for i in tri) for tri in t]; owner += [p] * len(t); bv += v
tree = BVHTree.FromPolygons([tuple(v) for v in bv], bt) if bt else None
eyes = [Vector((0.0, 0.0, 1.7))] + [Vector((a[0], a[2], a[1] + 1.7)) for a in out["layout"]["spawns"]["attackers"]]
for eye in eyes:
    for tgt, what in ((target, "sun disc"), (door, "doorway")):
        d = tgt - eye
        if tree:
            hit = tree.ray_cast(eye, d.normalized(), d.length)
            if hit[0] is not None:
                fail("SIGHT", owner[hit[2]], "crosses the line from (%.1f, %.1f) to the gate's %s" % (eye.x, eye.y, what))
notes.append("sight lines tested against the real triangles of %d pieces near the line" % len(near))

# ---- 5. two of a kind side by side
houses = [p for p in P if p["family"] == "house"]
for a in houses:
    others = sorted(((math.hypot(a["x"] - b["x"], a["y"] - b["y"]), b) for b in houses if b is not a), key=lambda t: t[0])
    if others[0][1]["model"] == a["model"]:
        fail("TWINS", a, "its nearest house, %.1f m away, is the same kind" % others[0][0])

text = ["PAYO ASSEMBLY CHECK", "placements %d; seated: %s" % (len(P), seated)] + notes + ["FAILURES %d" % len(fails)] + fails
open(os.path.join(D.ROOT, "Logs/payo/assembly/check.txt"), "w").write("\n".join(text) + "\n")
kinds = {}
for f in fails:
    kinds[f.split()[0]] = kinds.get(f.split()[0], 0) + 1
print("CHECK failures", len(fails), kinds)
for f in fails[:60]:
    print("CHECK", f)
