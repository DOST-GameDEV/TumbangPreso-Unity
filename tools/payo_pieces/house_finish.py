"""Payo town buildings: write the glb and shoot the four review pictures. See tools/payo_pieces/house_kit.py.

  blender -b --python tools/payo_pieces/house_finish.py -- <name> [...]
  blender -b --python tools/payo_pieces/house_finish.py -- --street

Review light: a warm white sun under a cool blue sky, so grey stone is seen grey under cool light. Blender, not the
game's shader: an ink line by an inverted hull, a little of the texture as its own light.
"""
import bpy, json, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
WORK = os.path.join(ROOT, "Logs", "payo", "pieces", "house", "work")
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "house")
SHOTS = os.path.join(WORK, "shots")
args = [a for a in sys.argv[sys.argv.index("--") + 1:]]
os.makedirs(SHOTS, exist_ok=True)


def materials(name, glow=0.0):
    img = bpy.data.images.load(os.path.join(OUT, name + ".png"), check_existing=False)
    mats = []
    for suffix in ("_main", "_glow"):
        m = bpy.data.materials.new("payo_" + name + suffix)
        m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        t = m.node_tree.nodes.new("ShaderNodeTexImage"); t.image = img
        m.node_tree.links.new(t.outputs["Color"], b.inputs["Base Color"])
        b.inputs["Roughness"].default_value = 1.0
        b.inputs["Metallic"].default_value = 0.0
        if suffix == "_glow":
            m.node_tree.links.new(t.outputs["Color"], b.inputs["Emission Color"])
            b.inputs["Emission Strength"].default_value = 1.0
        mats.append(m)
    return mats


def export(name):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(WORK, name + ".blend"))
    ob = bpy.data.objects[name]
    mats = materials(name)
    for i, m in enumerate(mats):                                 # set the slots in place: clearing them would lose which faces glow
        ob.data.materials[i] = m
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    path = os.path.join(OUT, name + ".glb")
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_yup=True, export_apply=True,
                              export_image_format="AUTO", export_materials="EXPORT", export_normals=True, export_texcoords=True)
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    lp = os.path.join(WORK, name + ".json")
    L = json.load(open(lp)); L["meta"]["tris_glb"] = tris; json.dump(L, open(lp, "w"))
    print("GLB %s: %d triangles, %d KB" % (name, tris, os.path.getsize(path) // 1024))


def stage(res=(1100, 800)):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.62, 0.86, 1)      # cool blue sky light
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.85
    for rot, energy, col in (((54, 0, -38), 2.9, (1.0, 0.95, 0.86)), ((118, 0, 150), 0.7, (0.6, 0.78, 1.0))):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.data.color = col; sun.rotation_euler = tuple(math.radians(a) for a in rot)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.clip_end = 2000
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = res
    return sc, cam


def flat(name, rgb):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1); b.inputs["Roughness"].default_value = 1
    b.inputs["Emission Color"].default_value = (*rgb, 1); b.inputs["Emission Strength"].default_value = 0.25
    return m


def ink(o, thick):
    line = bpy.data.materials.get("ink")
    if line is None:
        line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
        b = line.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (0.03, 0.03, 0.05, 1); b.inputs["Roughness"].default_value = 1
    for m in o.data.materials:
        if m:
            m.use_backface_culling = True
    o.data.materials.append(line)
    s = o.modifiers.new("ink", "SOLIDIFY")
    s.thickness = thick; s.offset = 1; s.use_flip_normals = True; s.material_offset = len(o.data.materials) - 1; s.use_rim = False


def cube(lo, hi, mat, bevel=0.03):
    bpy.ops.mesh.primitive_cube_add(size=1, location=[(a + b) / 2 for a, b in zip(lo, hi)])
    o = bpy.context.object
    o.scale = [abs(b - a) for a, b in zip(lo, hi)]
    bpy.ops.object.transform_apply(scale=True)
    bv = o.modifiers.new("b", "BEVEL"); bv.width = bevel; bv.segments = 2
    o.data.materials.append(mat)
    return o


def figure(x, y, z=0.0):
    skin, shirt, pants, hair = flat("skin", (0.62, 0.40, 0.26)), flat("shirt", (0.92, 0.88, 0.78)), flat("pants", (0.18, 0.18, 0.24)), flat("hair", (0.06, 0.05, 0.05))
    parts = [cube((-0.2, -0.11, 0.0), (-0.03, 0.11, 0.86), pants), cube((0.03, -0.11, 0.0), (0.2, 0.11, 0.86), pants),
             cube((-0.24, -0.14, 0.84), (0.24, 0.14, 1.36), shirt), cube((-0.34, -0.08, 0.86), (-0.25, 0.08, 1.34), skin), cube((0.25, -0.08, 0.86), (0.34, 0.08, 1.34), skin),
             cube((-0.19, -0.18, 1.38), (0.19, 0.18, 1.72), skin, 0.06), cube((-0.2, -0.12, 1.62), (0.2, 0.2, 1.76), hair, 0.05)]
    for o in parts:
        o.location.x += x; o.location.y += y; o.location.z += z
        ink(o, 0.012)


def load(name, at=(0, 0, 0), rz=0.0, inkw=0.03):
    bpy.ops.wm.append(directory=os.path.join(WORK, name + ".blend", "Object"), filename=name)
    ob = bpy.data.objects[name]
    mats = materials(name)
    for m in mats:
        b = m.node_tree.nodes["Principled BSDF"]
        t = next(n for n in m.node_tree.nodes if n.type == "TEX_IMAGE")
        m.node_tree.links.new(t.outputs["Color"], b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = 1.1 if m.name.endswith("_glow") else 0.28
    for i, m in enumerate(mats):
        ob.data.materials[i] = m
    ob.location = at; ob.rotation_euler = (0, 0, math.radians(rz))
    ink(ob, inkw)
    return ob


def shoot(sc, cam, eye, look, fov, path):
    cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


def review(name):
    L = json.load(open(os.path.join(WORK, name + ".json")))
    m = L["meta"]
    lo, hi = Vector(m["lo"]), Vector(m["hi"])
    sc, cam = stage()
    load(name)
    bpy.ops.mesh.primitive_plane_add(size=600, location=(0, 0, 0))
    bpy.context.object.data.materials.append(flat("ground", (0.47, 0.50, 0.40)))
    fx, fy = m.get("figure", (hi.x * 0.6 + 0.6, lo.y - 1.2))
    figure(fx, fy)
    H = hi.z
    d = m.get("distance", max(hi.x - lo.x, hi.y - lo.y, H) * 1.25)
    look = Vector((0, 0, H * m.get("look", 0.47)))
    fov = m.get("fov", 58)
    shoot(sc, cam, (d * 0.60, -d * 0.80, 1.7), look, fov, os.path.join(SHOTS, name + "_front.png"))
    shoot(sc, cam, (-d * 0.66, d * 0.75, 1.7 + m.get("back_up", 0.0)), look, fov, os.path.join(SHOTS, name + "_back.png"))
    ct, ce = m["close"]
    shoot(sc, cam, ce, ct, m.get("close_fov", 48), os.path.join(SHOTS, name + "_close.png"))
    h = max(hi.x - lo.x, hi.y - lo.y, H * 0.8)
    shoot(sc, cam, (h * 0.55, -h * 0.95, H + h * 0.75), (0, 0, H * 0.55), 44, os.path.join(SHOTS, name + "_above.png"))


def street():
    plan = json.load(open(os.path.join(WORK, "street.json")))
    sc, cam = stage((1400, 900))
    for it in plan["houses"]:
        load(it["name"], it["at"], it.get("rz", 0.0), 0.035)
    for it in plan.get("figures", []):
        figure(*it)
    # the slope: stepped terraces
    g = flat("ground", (0.47, 0.50, 0.40)); st = flat("terrace", (0.52, 0.50, 0.47))
    for lo, hi in plan["ground"]:
        o = cube(lo, hi, st if hi[2] - lo[2] < 30 else g, 0.12)
        ink(o, 0.03)
    bpy.ops.mesh.primitive_plane_add(size=900, location=(0, 0, plan.get("floor", -6)))
    bpy.context.object.data.materials.append(g)
    for k in ("a", "b"):
        eye, look, fov = plan["cam_" + k]
        shoot(sc, cam, eye, look, fov, os.path.join(SHOTS, "street_%s.png" % k))


if "--street" in args:
    street()
else:
    for n in args:
        export(n)
        review(n)
print("done")
