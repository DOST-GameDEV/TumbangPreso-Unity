"""Joins each prop's typed pieces into ONE closed skin and writes its model (step 2 of the props' pipeline).

  blender -b --python tools/payo_pieces/prop_weld.py -- <set letter> [prop ...]

After tools/weld_slippers.py: every "add" piece is UNIONED into the skin and every "cut" is taken out of what has been
added so far (exact booleans), so nothing passes through anything and no face is left inside. A prop's pieces named for
another group (a flame, a pour of water) become a SECOND object of the same model, <prop>__<group>, so the game can hide
it by day. Edges sharper than 40 degrees stay creases, the rest is smooth, normals weighted by face area so a chamfered
board keeps flat faces. Writes Logs/payo/pieces/build/prop/<prop>.glb (the set's atlas inside) and stats-<set>.json.
"""
import bpy, bmesh, json, math, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "prop")
words = [a for a in sys.argv[sys.argv.index("--") + 1:] if not a.startswith("--")] if "--" in sys.argv else []
letter = words[0]
meta = json.load(open(os.path.join(OUT, "parts", "set%s.json" % letter)))
names = words[1:] or [p["name"] for p in meta["props"]]
stats_path = os.path.join(OUT, "stats-%s.json" % letter)
stats = json.load(open(stats_path)) if os.path.exists(stats_path) else {}


def clean(obj):
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.00004)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=0.00002)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data); bm.free()


def only(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True); bpy.context.view_layer.objects.active = obj


for name in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(OUT, "parts", name + ".glb"))
    pieces = sorted([o for o in bpy.data.objects if o.type == "MESH"], key=lambda o: o.name)
    for o in pieces:
        only(o)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        clean(o)
    groups = {}
    for o in pieces:
        groups.setdefault(o.name.split("|")[0], []).append(o)
    mat = bpy.data.materials.new("payo_props_" + letter); mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(os.path.join(OUT, "parts", meta["atlas"]))
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 1.0; bsdf.inputs["Metallic"].default_value = 0.0
    done, triangles, open_edges = [], 0, 0
    for group in sorted(groups, key=lambda g: g != "main"):
        members = groups[group]
        loose = [o for o in members[1:] if o.name.split(".")[0].endswith("_join")]
        skin = members[0]
        for o in members[1:]:
            if o in loose:
                continue
            m = skin.modifiers.new("b", "BOOLEAN")
            m.operation = "DIFFERENCE" if o.name.split(".")[0].endswith("_cut") else "UNION"
            m.solver = "EXACT"; m.object = o
            only(skin)
            bpy.ops.object.modifier_apply(modifier=m.name)
            if "--debug" in sys.argv:
                bm = bmesh.new(); bm.from_mesh(skin.data)
                print("  after %s: %d open" % (o.name, sum(1 for e in bm.edges if len(e.link_faces) != 2))); bm.free()
            bpy.data.objects.remove(o, do_unlink=True)
        if loose:                                        # small closed things laid on the skin as they are (a tassel on a hem)
            bpy.ops.object.select_all(action="DESELECT")
            for o in loose:
                o.select_set(True)
            skin.select_set(True); bpy.context.view_layer.objects.active = skin
            bpy.ops.object.join()
        bm = bmesh.new(); bm.from_mesh(skin.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.00004)
        bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=0.00002)
        bmesh.ops.triangulate(bm, faces=bm.faces)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        open_edges += sum(1 for e in bm.edges if len(e.link_faces) != 2)
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            e.smooth = len(e.link_faces) == 2 and e.calc_face_angle(0.0) < math.radians(40)
        triangles += len(bm.faces)
        bm.to_mesh(skin.data); bm.free()
        skin.name = skin.data.name = name if group == "main" else name + "__" + group
        skin.data.materials.clear(); skin.data.materials.append(mat)
        w = skin.modifiers.new("wn", "WEIGHTED_NORMAL"); w.keep_sharp = True; w.weight = 60
        done.append(skin)
    bpy.ops.object.select_all(action="DESELECT")
    for o in done:
        o.select_set(True)
    bpy.context.view_layer.objects.active = done[0]
    path = os.path.join(OUT, name + ".glb")
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
                              export_tangents=False, export_animations=False, export_cameras=False, export_lights=False)
    lo = [min((o.matrix_world @ v.co)[k] for o in done for v in o.data.vertices) for k in range(3)]
    hi = [max((o.matrix_world @ v.co)[k] for o in done for v in o.data.vertices) for k in range(3)]
    dims = [round(hi[0] - lo[0], 2), round(hi[2] - lo[2], 2), round(hi[1] - lo[1], 2)]              # wide, tall, deep
    stats[name] = {"triangles": triangles, "kb": round(os.path.getsize(path) / 1024), "open_edges": open_edges, "size": dims,
                   "ground": round(lo[2], 3), "objects": [o.name for o in done]}
    print("WELDED %s: %d triangles, %d open edges, %s, ground %.3f, %s" % (name, triangles, open_edges, dims, lo[2], [o.name for o in done]))

json.dump(stats, open(stats_path, "w"), indent=1)
