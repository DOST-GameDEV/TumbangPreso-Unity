"""Joins a gate piece's typed parts into its finished model (step 2 of tools/payo_pieces/gate_kit.py).

  blender -b --python tools/payo_pieces/gate_weld.py -- <piece> [...]

Every NNNN_add part is unioned into ONE closed skin (exact booleans, self intersection on), every NNNN_cut is taken out,
every NNNN_add2 is unioned in after the cuts. NNNN_loose parts (leaves, vines) are joined to the skin's mesh as they are.
NNNN_glow parts become a second mesh, <piece>_glow, with its own material <piece>_glow (emissive), so the turquoise
inlay can be lit at night. Edges sharper than 58 degrees stay creases, the rest is smooth with face-weighted normals
(a bevel reads round, a flat face stays flat). Writes Logs/payo/pieces/build/gate/<piece>.glb with the atlas inside,
copies the atlas beside it, and its numbers into stats.json (open edges 0 = the stone skin is closed).
"""
import bpy, bmesh, json, math, os, shutil, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "gate")
ids = [a for a in sys.argv[sys.argv.index("--") + 1:] if not a.startswith("--")]
NOWELD = "--noweld" in sys.argv
stats_path = os.path.join(OUT, "stats.json")


def active(o):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True); bpy.context.view_layer.objects.active = o


def joined(objs):
    if not objs:
        return None
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    return bpy.context.view_layer.objects.active


def boolean(skin, other, op):
    m = skin.modifiers.new("b", "BOOLEAN")
    m.operation = op; m.solver = "EXACT"; m.object = other
    try:
        m.use_self = True
    except Exception:
        pass
    active(skin)
    bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(other, do_unlink=True)


def tidy(obj, smooth_deg=58):
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=0.0003)
    if smooth_deg > 50:                                  # flat regions the booleans cut into slivers become few faces again
        bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.8), verts=bm.verts, edges=bm.edges, delimit={"UV"})
    bmesh.ops.triangulate(bm, faces=bm.faces)
    open_edges = sum(1 for e in bm.edges if len(e.link_faces) != 2)
    for e in [e for e in bm.edges if len(e.link_faces) != 2][:4]:
        print("OPEN edge at", [round(v, 2) for v in e.verts[0].co], [round(v, 2) for v in e.verts[1].co], len(e.link_faces))
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        e.smooth = len(e.link_faces) == 2 and e.calc_face_angle(0.0) < math.radians(smooth_deg)
    n = len(bm.faces)
    bm.to_mesh(obj.data); bm.free()
    return n, open_edges


def material(name, atlas, glow=False):
    mat = bpy.data.materials.new(name); mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = atlas
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 1.0; bsdf.inputs["Metallic"].default_value = 0.0
    if glow:
        mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 1.0
    return mat


for name in ids:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(OUT, "parts", name + ".glb"))
    parts = sorted([o for o in bpy.data.objects if o.type == "MESH"], key=lambda o: o.name)
    for o in parts:
        active(o)
        bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in [o for o in bpy.data.objects if o.type != "MESH"]:
        bpy.data.objects.remove(o, do_unlink=True)
    kind = lambda o: o.name.split("_", 1)[1].split(".")[0]
    groups = {k: [o for o in parts if kind(o) == k] for k in ("add", "cut", "add2", "glow", "loose")}
    if NOWELD:
        skin = joined(groups["add"] + groups["add2"])
        for o in groups["cut"]:
            bpy.data.objects.remove(o, do_unlink=True)
    else:
        skin = groups["add"][0]
        rest = joined(groups["add"][1:])
        if rest:
            boolean(skin, rest, "UNION")
        cuts = joined(groups["cut"])
        if cuts:
            boolean(skin, cuts, "DIFFERENCE")
        more = joined(groups["add2"])
        if more:
            boolean(skin, more, "UNION")
    triangles, open_edges = tidy(skin)
    loose = joined(groups["loose"])
    if loose:
        t2, _ = tidy(loose, 40)
        triangles += t2
        skin = joined([skin, loose])
    skin.name = skin.data.name = name
    atlas = bpy.data.images.load(os.path.join(OUT, "parts", name + "-atlas.png"))
    skin.data.materials.clear(); skin.data.materials.append(material(name, atlas))
    wn = skin.modifiers.new("wn", "WEIGHTED_NORMAL"); wn.keep_sharp = True; wn.weight = 60
    out = [skin]
    glow = joined(groups["glow"])
    glow_tris = 0
    if glow:
        glow_tris, _ = tidy(glow, 40)
        glow.name = glow.data.name = name + "_glow"
        glow.data.materials.clear(); glow.data.materials.append(material(name + "_glow", atlas, True))
        out.append(glow)
    path = os.path.join(OUT, name + ".glb")
    bpy.ops.object.select_all(action="DESELECT")
    for o in out:
        o.select_set(True)
    bpy.context.view_layer.objects.active = skin
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
                              export_tangents=False, export_animations=False, export_cameras=False, export_lights=False)
    shutil.copyfile(os.path.join(OUT, "parts", name + "-atlas.png"), os.path.join(OUT, name + "-atlas.png"))
    dims = skin.dimensions
    stats = json.load(open(stats_path)) if os.path.exists(stats_path) else {}
    stats.setdefault(name, {}).update({"triangles": triangles + glow_tris, "glow_triangles": glow_tris, "kb": round(os.path.getsize(path) / 1024), "open_edges": open_edges,
                                       "size": "%.1f wide, %.1f tall, %.1f deep (m)" % (dims.x, dims.z, dims.y), "atlas": "%d px" % atlas.size[0]})
    json.dump(stats, open(stats_path, "w"), indent=1)
    print("WELDED %s: %d triangles (%d glow), %d KB, %d open edges, %s" % (name, triangles + glow_tris, glow_tris, stats[name]["kb"], open_edges, stats[name]["size"]))
