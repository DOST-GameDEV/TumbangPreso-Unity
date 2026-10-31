"""Payo's land tiles get decimated LOD1/LOD2 copies (owner, 2026-10-30, on ~6 million triangles a frame: "maybe simplify
the places that are further from the arena. we've done an LOD for textures, but not for the actual mesh").

Blender, background:
  "/c/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --factory-startup --python tools/payo_land_lods.py -- MODELS_DIR OUT_DIR

For every MODELS_DIR/payo_land_*.glb it writes OUT_DIR/<name>_lod1.glb and _lod2.glb: the same nodes, materials and
attributes, each mesh run through a collapse Decimate (quadric error, so the terrace risers and ridges keep their edges
longest). Ratios below. PayoSceneBuilder.MeshLevels.cs puts them in a LODGroup on the far tiles; the world AO bake then
bakes every level. Absolute paths only.
"""
import os
import sys

import bpy

RATIOS = {"lod1": 0.40, "lod2": 0.14}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    src, out = argv[0], argv[1]
    os.makedirs(out, exist_ok=True)
    names = sorted(n for n in os.listdir(src) if n.startswith("payo_land_") and n.endswith(".glb"))
    for name in names:
        for level, ratio in RATIOS.items():
            dst = os.path.join(out, name[:-4] + "_" + level + ".glb")
            if os.path.exists(dst) and os.path.getmtime(dst) >= os.path.getmtime(os.path.join(src, name)):
                continue
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.gltf(filepath=os.path.join(src, name))
            before = after = 0
            for ob in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
                before += sum(len(p.vertices) - 2 for p in ob.data.polygons)
                mod = ob.modifiers.new("decimate", "DECIMATE")
                mod.decimate_type = "COLLAPSE"
                mod.ratio = ratio
                mod.use_collapse_triangulate = True
                bpy.context.view_layer.objects.active = ob
                bpy.ops.object.modifier_apply(modifier=mod.name)
                after += len(ob.data.polygons)
            bpy.ops.export_scene.gltf(filepath=dst, export_format="GLB", use_selection=False, export_yup=True,
                                      export_apply=False, export_extras=True)
            print(f"[payo_land_lods] {name} {level}: {before} -> {after} tris")


main()
