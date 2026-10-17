"""Pictures for the slippers' comparison sheet (step 3 of tools/build_slippers.py). Blender only: NOT the game's shader.

  blender -b --python tools/review_slippers.py -- [id ...] [--out=folder] [--old=0]
  py -3 tools/build_slippers.py sheet <tag>

Per shoe, into Logs/slippers/shots: <id>_top.png (isometric, from above the toe end), <id>_bottom.png (isometric, from
under the HEEL end: the opposite corner), and <id>_old.png (the model it replaces, from the top view), each with an ink
line. The old model's triangle count and size are added to <out>/stats.json.
"""
import bpy, bmesh, json, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MODELS = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "models")
SHOTS = os.path.join(ROOT, "Logs", "slippers", "shots")


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


OUT = arg("out", os.path.join(ROOT, "Logs", "slippers", "build"))
words = [a for a in sys.argv[sys.argv.index("--") + 1:] if not a.startswith("--")] if "--" in sys.argv else []
ids = words or sorted(f[:-4] for f in os.listdir(OUT) if f.endswith(".glb"))
stats_path = os.path.join(OUT, "stats.json")
stats = json.load(open(stats_path)) if os.path.exists(stats_path) else {}
os.makedirs(SHOTS, exist_ok=True)


def stage():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.48, 0.58, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    for rot, energy in (((50, 0, -30), 2.6), ((120, 0, 150), 1.2)):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.rotation_euler = tuple(math.radians(a) for a in rot)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.type = "ORTHO"; cam.data.ortho_scale = 0.60
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = 800, 600
    return sc, cam


def ink(objects):
    line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
    b = line.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.02, 0.02, 0.03, 1); b.inputs["Roughness"].default_value = 1
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for o in objects:
        for m in o.data.materials:
            if m:
                m.use_backface_culling = True
        o.data.materials.append(line)
        s = o.modifiers.new("ink", "SOLIDIFY")
        s.thickness = 0.0026; s.offset = 1; s.use_flip_normals = True; s.material_offset = len(o.data.materials) - 1; s.use_rim = False
        for corner in o.bound_box:
            p = o.matrix_world @ Vector(corner)
            lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    return (lo + hi) * 0.5


def shoot(cam, centre, direction, path):
    cam.location = centre + Vector(direction).normalized() * 2.0
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


# The toe is +X. Above the toe end, to one side; then under the heel end, from the other side.
TOP, BOTTOM = (0.72, -0.78, 0.62), (-0.72, 0.78, -0.62)

for name in ids:
    sc, cam = stage()
    bpy.ops.import_scene.gltf(filepath=os.path.join(OUT, name + ".glb"))
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    for o in meshes:
        for m in o.data.materials:
            b = m.node_tree.nodes.get("Principled BSDF") if m and m.use_nodes else None
            tex = next((n for n in m.node_tree.nodes if n.type == "TEX_IMAGE"), None) if b else None
            if tex:                                               # a little of its own colour as light: nearer the toon look
                m.node_tree.links.new(tex.outputs["Color"], b.inputs["Emission Color"]); b.inputs["Emission Strength"].default_value = 0.35
    centre = ink(meshes)
    shoot(cam, centre, TOP, os.path.join(SHOTS, name + "_top.png"))
    shoot(cam, centre, BOTTOM, os.path.join(SHOTS, name + "_bottom.png"))

    if arg("old", "1") != "0":
        old = next((os.path.join(MODELS, "tsinelas_" + name + ext) for ext in (".glb", ".obj") if os.path.exists(os.path.join(MODELS, "tsinelas_" + name + ext))), None)
        if old:
            sc, cam = stage()
            if old.endswith(".glb"):
                bpy.ops.import_scene.gltf(filepath=old)
            else:
                # The game's .obj is y up with the toe along +x, as the .glb files are once imported.
                bpy.ops.wm.obj_import(filepath=old, forward_axis="NEGATIVE_Z", up_axis="Y")
            meshes = [o for o in bpy.data.objects if o.type == "MESH"]
            triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
            kb = os.path.getsize(old)
            if old.endswith(".obj") and os.path.exists(old[:-4] + ".mtl"):
                kb += os.path.getsize(old[:-4] + ".mtl")
            stats.setdefault(name, {}).update({"old_triangles": triangles, "old_kb": round(kb / 1024)})
            centre = ink(meshes)
            shoot(cam, centre, TOP, os.path.join(SHOTS, name + "_old.png"))
            print("OLD %s: %d triangles, %d KB" % (name, triangles, round(kb / 1024)))

json.dump(stats, open(stats_path, "w"), indent=1)
print("done")
