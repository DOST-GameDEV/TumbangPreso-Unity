"""Joins a Payo piece's typed parts into ONE closed skin (step 2 of tools/payo_pieces/stone_kit.py), as tools/weld_slippers.py does.

  blender -b --python tools/payo_pieces/stone_weld.py -- <piece> [...] [--out=folder]

Each NNN_add is unioned into the skin and each NNN_cut taken out of what has been added so far (exact booleans). Then:
points welded, edges sharper than 42 degrees kept as creases, everything else smooth, one material with the painted
atlas inside the .glb. Writes <out>/<piece>.glb and its numbers into <out>/stats.json (open edges 0 = a closed skin).
"""
import bpy, bmesh, json, math, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


OUT = arg("out", os.path.join(ROOT, "Logs", "payo", "pieces", "build", "stone"))
ids = [a for a in sys.argv[sys.argv.index("--") + 1:] if not a.startswith("--")]
stats_path = os.path.join(OUT, "stats.json")
stats = json.load(open(stats_path)) if os.path.exists(stats_path) else {}


def clean(obj):
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0004)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=0.0002)
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
    loose = [o for o in pieces if o.name.split('.')[0].endswith('_join')]
    pieces = [o for o in pieces if o not in loose]
    skin = pieces[0]
    failed = 0
    for o in pieces[1:]:
        before = len(skin.data.polygons)
        m = skin.modifiers.new("b", "BOOLEAN")
        m.operation = "DIFFERENCE" if o.name.endswith("_cut") else "UNION"
        m.solver = "EXACT"; m.object = o
        bpy.ops.object.select_all(action="DESELECT")
        skin.select_set(True); bpy.context.view_layer.objects.active = skin
        bpy.ops.object.modifier_apply(modifier=m.name)
        if len(skin.data.polygons) == 0:
            failed += 1
        bpy.data.objects.remove(o, do_unlink=True)
    if loose:
        bpy.ops.object.select_all(action="DESELECT")
        for o in loose:
            o.select_set(True)
        skin.select_set(True); bpy.context.view_layer.objects.active = skin
        bpy.ops.object.join()
    bm = bmesh.new(); bm.from_mesh(skin.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0004)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=0.0002)
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
    try:
        bpy.ops.object.select_all(action="DESELECT")
        skin.select_set(True); bpy.context.view_layer.objects.active = skin
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception:
        pass

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
    dims = [round(v, 2) for v in skin.dimensions]
    stats.setdefault(name, {}).update({"triangles": triangles, "kb": round(os.path.getsize(path) / 1024), "open_edges": open_edges, "size_xyz_blender": dims})
    print("WELDED %s: %d triangles, %d KB, %d open edges, %s" % (name, triangles, stats[name]["kb"], open_edges, dims))

json.dump(stats, open(stats_path, "w"), indent=1)
