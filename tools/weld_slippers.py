"""Joins a slipper's typed pieces into ONE closed skin and writes the game's model (step 2 of tools/build_slippers.py).

  blender -b --python tools/weld_slippers.py -- [id ...] [--out=folder]

Owner, 2026-10-11: "ensure it looks seamless, not multiple parts intersected together". build_slippers.py types a shoe
as closed pieces named NN_add / NN_cut in <out>/parts/<id>.glb. Here, in order, each "add" is UNIONED into the skin and
each "cut" is taken OUT of what has been added so far (exact booleans), so a strap does not pass through a sole and no
face is left inside the shoe. Then: points welded, edges sharper than 42 degrees kept as creases (where a strap meets
the sole), everything else smooth, one material with the painted atlas INSIDE the .glb. Writes <out>/<id>.glb and adds
the shoe to <out>/stats.json (triangles, bytes, open edges: 0 means a closed skin).
"""
import bpy, bmesh, json, math, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


OUT = arg("out", os.path.join(ROOT, "Logs", "slippers", "build"))
words = [a for a in sys.argv[sys.argv.index("--") + 1:] if not a.startswith("--")] if "--" in sys.argv else []
ids = words or sorted(f[:-4] for f in os.listdir(os.path.join(OUT, "parts")) if f.endswith(".glb"))
stats_path = os.path.join(OUT, "stats.json")
stats = json.load(open(stats_path)) if os.path.exists(stats_path) else {}


def clean(obj):
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.00002)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=0.00001)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data); bm.free()


for name in ids:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(OUT, "parts", name + ".glb"))
    pieces = sorted([o for o in bpy.data.objects if o.type == "MESH"], key=lambda o: o.name)
    for o in pieces:
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True); bpy.context.view_layer.objects.active = o
        bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        clean(o)
    skin = pieces[0]
    for o in pieces[1:]:
        m = skin.modifiers.new("b", "BOOLEAN")
        m.operation = "DIFFERENCE" if o.name.endswith("_cut") else "UNION"
        m.solver = "EXACT"; m.object = o
        bpy.ops.object.select_all(action="DESELECT")
        skin.select_set(True); bpy.context.view_layer.objects.active = skin
        bpy.ops.object.modifier_apply(modifier=m.name)
        bpy.data.objects.remove(o, do_unlink=True)
    bm = bmesh.new(); bm.from_mesh(skin.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.00002)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=0.00001)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    open_edges = sum(1 for e in bm.edges if len(e.link_faces) != 2)
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        e.smooth = len(e.link_faces) == 2 and e.calc_face_angle(0.0) < math.radians(42)
    triangles = len(bm.faces)
    bm.to_mesh(skin.data); bm.free()
    skin.name = skin.data.name = name

    mat = bpy.data.materials.new(name); mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(os.path.join(OUT, "parts", name + "-atlas.png"))
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 1.0; bsdf.inputs["Metallic"].default_value = 0.0
    skin.data.materials.clear(); skin.data.materials.append(mat)

    path = os.path.join(OUT, name + ".glb")
    bpy.ops.object.select_all(action="DESELECT")
    skin.select_set(True); bpy.context.view_layer.objects.active = skin
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
                              export_tangents=False, export_animations=False, export_cameras=False, export_lights=False)
    dims = [round(v, 3) for v in skin.dimensions]
    stats.setdefault(name, {}).update({"triangles": triangles, "kb": round(os.path.getsize(path) / 1024), "open_edges": open_edges, "size": dims})
    print("WELDED %s: %d triangles, %d KB, %d open edges, %s" % (name, triangles, stats[name]["kb"], open_edges, dims))

json.dump(stats, open(stats_path, "w"), indent=1)
