"""Audit a dressed Payo blend for pieces that do not meet what they stand on, and for z-fighting.

  blender -b ArtSource/payo/payo_dressed_<tag>.blend --python tools/payo_audit.py -- <out.txt>

For every placed piece (a collection instance): its world bounds from its library geometry, the ground under its
footprint (both land objects, raycast), and:
  FLOAT  its lowest point is above the highest ground under it (it hovers)
  SUNK   the ground under its middle is far above its base (it is buried)
  STRADDLE  the ground under its footprint spans a big height range (it sits across a wall or a slope)
  ZFIGHT  an upward face lies within 3 cm of the ground, or of another piece's upward face, at the same spot
"""
import bpy
import math
import sys
from collections import defaultdict
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

out = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else None
dg = bpy.context.evaluated_depsgraph_get()
bvhs = [BVHTree.FromObject(bpy.data.objects[n], dg) for n in ("Land", "Land terraces") if n in bpy.data.objects]


def ground(x, y):
    best = None
    for b in bvhs:
        hit = b.ray_cast(Vector((x, y, 3000.0)), Vector((0, 0, -1)))
        if hit[0] is not None and (best is None or hit[0].z > best):
            best = hit[0].z
    return best


lib = {}


def lib_geo(col):
    """Library points and upward face centres, in the collection's own space."""
    if col.name in lib:
        return lib[col.name]
    off = Matrix.Translation(-col.instance_offset)
    pts, ups = [], []
    for ob in col.all_objects:
        if ob.type != "MESH":
            continue
        m = off @ ob.matrix_world
        for v in ob.data.vertices:
            pts.append(m @ v.co)
        nm = m.to_3x3()
        for p in ob.data.polygons:
            n = (nm @ p.normal).normalized()
            if n.z > 0.95 and p.area * abs(nm.determinant()) ** (2 / 3) > 0.05:
                ups.append(m @ p.center)
    lib[col.name] = (pts, ups)
    return lib[col.name]


SKIP_FLOAT = {"Water", "Stage", "Pit", "Platform"}    # built into walls, cliffs and water on purpose
lines = []
upmap = defaultdict(list)       # 1 m cell -> (z, instance name)
counts = defaultdict(int)
for o in bpy.data.objects:
    col = o.instance_collection
    if not col or o.users_collection[0].name.startswith("lib"):
        continue
    grp = o.users_collection[0].name
    pts, ups = lib_geo(col)
    if not pts:
        continue
    w = [o.matrix_world @ p for p in pts]
    lo = Vector((min(p.x for p in w), min(p.y for p in w), min(p.z for p in w)))
    hi = Vector((max(p.x for p in w), max(p.y for p in w), max(p.z for p in w)))
    name = "%s %s (%.1f, %.1f, %.2f)" % (grp, col.name[4:], o.location.x, o.location.y, o.location.z)
    if grp != "Trees":
        for u in ups:
            wu = o.matrix_world @ u
            g = ground(wu.x, wu.y)
            if g is not None and abs(wu.z - g) < 0.03:
                lines.append("ZFIGHT ground  %s  face z %.3f ground %.3f" % (name, wu.z, g)); counts["ZFIGHT"] += 1
            upmap[(math.floor(wu.x), math.floor(wu.y))].append((wu.z, name))
    if grp in SKIP_FLOAT:
        continue
    # the ground under the footprint, inset a little
    ix, iy = (hi.x - lo.x) * 0.3, (hi.y - lo.y) * 0.3
    cx, cy = (lo.x + hi.x) / 2, (lo.y + hi.y) / 2
    gs = [ground(x, y) for x, y in ((cx, cy), (lo.x + ix, lo.y + iy), (hi.x - ix, lo.y + iy), (lo.x + ix, hi.y - iy), (hi.x - ix, hi.y - iy))]
    gs = [g for g in gs if g is not None]
    if not gs:
        continue
    tol = 0.35 if grp != "Trees" else 0.6
    if lo.z - max(gs) > tol:
        lines.append("FLOAT %.2f m  %s" % (lo.z - max(gs), name)); counts["FLOAT"] += 1
    if gs[0] - lo.z > (2.5 if grp != "Trees" else 3.5):
        lines.append("SUNK %.2f m  %s" % (gs[0] - lo.z, name)); counts["SUNK"] += 1
    if grp != "Trees" and max(gs) - min(gs) > 1.2:
        lines.append("STRADDLE %.2f m  %s" % (max(gs) - min(gs), name)); counts["STRADDLE"] += 1

for cell, faces in upmap.items():
    faces.sort()
    for (z0, n0), (z1, n1) in zip(faces, faces[1:]):
        if n0 != n1 and z1 - z0 < 0.03:
            lines.append("ZFIGHT pieces  %s  /  %s  z %.3f" % (n0, n1, z0)); counts["ZFIGHT pieces"] += 1
            break
lines.insert(0, "COUNTS " + ", ".join("%s %d" % kv for kv in sorted(counts.items())))
text = "\n".join(lines)
if out:
    open(out, "w", encoding="utf-8").write(text)
print(text[:4000])
