"""PAYO sacred pool: its top surface as a height grid (the stone only, not the water), so tools/payo_land.py can sink
the land under it.  blender -b --factory-startup --python tools/payo_pieces/pool_height_grid.py
writes Logs/payo/pieces/calm/court/pool_height_grid.npz: x0, y0, step, h (nan where the piece is not), map metres
(the piece stands at the court's origin, no turn)."""
import os
import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
D = os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "court")
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(D, "pool_basin.glb"))
dg = bpy.context.evaluated_depsgraph_get()
trees = []
for o in bpy.context.scene.objects:
    if o.type == "MESH" and not any(m and "py_water" in m.name for m in o.data.materials):
        bm = BVHTree.FromObject(o, dg)
        trees.append((o.matrix_world.copy(), bm))
step = 1.0
xs = np.arange(-40.0, 40.01, step); ys = np.arange(54.0, 102.01, step)
h = np.full((len(ys), len(xs)), np.nan, np.float32)
for j, y in enumerate(ys):
    for i, x in enumerate(xs):
        for mw, t in trees:
            inv = mw.inverted()
            hit = t.ray_cast(inv @ Vector((x, y, 400.0)), (inv.to_3x3() @ Vector((0, 0, -1))).normalized())
            if hit[0] is not None:
                z = (mw @ hit[0]).z
                if np.isnan(h[j, i]) or z > h[j, i]:
                    h[j, i] = z
np.savez(os.path.join(D, "pool_height_grid.npz"), x0=xs[0], y0=ys[0], step=step, h=h)
print("GRID", h.shape, np.nanmin(h), np.nanmax(h), int(np.isfinite(h).sum()))
