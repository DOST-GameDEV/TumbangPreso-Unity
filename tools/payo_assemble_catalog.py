"""PAYO assembly: a catalogue of every piece (bounds in Blender axes after import, objects, materials, textures, triangles).
Run: blender -b --python tools/payo_assemble_catalog.py -- <out.json> <dir> [<dir> ...]"""
import bpy, json, sys, os
from mathutils import Vector
args = sys.argv[sys.argv.index("--") + 1:]
out, dirs = args[0], args[1:]
cat = {}
for d in dirs:
    for f in sorted(os.listdir(d)):
        if not f.endswith(".glb"):
            continue
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=os.path.join(d, f))
        lo = [1e9] * 3; hi = [-1e9] * 3; objs = []; tris = 0
        for o in bpy.data.objects:
            if o.type != "MESH":
                continue
            olo = [1e9] * 3; ohi = [-1e9] * 3
            for c in o.bound_box:
                p = o.matrix_world @ Vector(c)
                for i in range(3):
                    olo[i] = min(olo[i], p[i]); ohi[i] = max(ohi[i], p[i])
            t = sum(len(p.vertices) - 2 for p in o.data.polygons)
            tris += t
            objs.append({"name": o.name, "tris": t, "lo": [round(v, 3) for v in olo], "hi": [round(v, 3) for v in ohi],
                         "materials": [m.name for m in o.data.materials if m], "vcol": len(o.data.color_attributes) > 0})
            for i in range(3):
                lo[i] = min(lo[i], olo[i]); hi[i] = max(hi[i], ohi[i])
        imgs = sorted({(i.name, i.size[0], i.size[1]) for i in bpy.data.images if i.size[0] > 0})
        mats = {}
        for m in bpy.data.materials:
            tex = None
            if m.node_tree:
                for n in m.node_tree.nodes:
                    if n.type == "TEX_IMAGE" and n.image:
                        tex = n.image.name
            mats[m.name] = {"texture": tex, "blend": getattr(m, "blend_method", ""), "emission": any(n.type == "EMISSION" for n in m.node_tree.nodes) if m.node_tree else False}
        cat[f[:-4]] = {"dir": d.replace("\\", "/"), "tris": tris, "lo": [round(v, 3) for v in lo], "hi": [round(v, 3) for v in hi], "objects": objs, "images": imgs, "materials": mats, "bytes": os.path.getsize(os.path.join(d, f))}
json.dump(cat, open(out, "w"), indent=1)
print("CATALOG", len(cat))
