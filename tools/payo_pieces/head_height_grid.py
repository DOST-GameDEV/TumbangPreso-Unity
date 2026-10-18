"""The head piece's top surface as a height grid, so tools/payo_land.py can keep the land under it near the pool.

  blender -b --factory-startup --python tools/payo_pieces/head_height_grid.py
writes Logs/payo/pieces/calm/head/head_height_grid.npz: x0, y0, step, h (nan where the piece is not), in map metres
(the piece stands at (0, -208, 0)). Water, foam, mist and plants are left out: only rock and stone count.
"""
import os
import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
D = os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "head")
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(D, "head_fall.glb"))
dg = bpy.context.evaluated_depsgraph_get()
trees = []
for o in bpy.context.scene.objects:
    if o.type != "MESH":
        continue
    names = " ".join(m.name for m in o.data.materials if m)
    if any(k in names for k in ("py_fall", "py_foam", "py_mist", "py_water", "py_terrace_plant")):
        continue
    trees.append(BVHTree.FromObject(o, dg))
step = 1.5
xs = np.arange(-48.0, 48.01, step); ys = np.arange(-274.0, -182.99, step)
h = np.full((len(ys), len(xs)), np.nan, np.float32)
for j, y in enumerate(ys):
    for i, x in enumerate(xs):
        for t in trees:
            hit = t.ray_cast(Vector((x, y + 208.0, 400.0)), Vector((0, 0, -1)))   # the tree is in the piece's own space
            if hit[0] is not None and (np.isnan(h[j, i]) or hit[0].z > h[j, i]):
                h[j, i] = hit[0].z
np.savez(os.path.join(D, "head_height_grid.npz"), x0=xs[0], y0=ys[0], step=step, h=h)
print("GRID", h.shape, np.nanmin(h), np.nanmax(h), int(np.isfinite(h).sum()))
