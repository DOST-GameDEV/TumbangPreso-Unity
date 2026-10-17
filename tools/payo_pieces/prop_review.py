"""Pictures for a set of props' review sheet (step 3). Blender only: NOT the game's shader.

  blender -b --python tools/payo_pieces/prop_review.py -- <set letter> [prop ...] [--set=0]

Per prop, into Logs/payo/pieces/prop/shots: <prop>_top.png (three-quarter, from above the front) and <prop>_low.png
(from the opposite corner, low), each with an ink line, on no background (the sheet lays them on neutral grey). Then
set<letter>_all.png: the whole set stood as it would be in the village, a 1.75 m figure beside it.
LIGHT (owner, 2026-10-12: the cool tone of reference 238 is its LIGHTING, the stone is grey): a cool blue sky over a
warm-white sun.
"""
import bpy, json, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "prop")
SHOTS = os.path.join(ROOT, "Logs", "payo", "pieces", "prop", "shots")
os.makedirs(SHOTS, exist_ok=True)
words = [a for a in sys.argv[sys.argv.index("--") + 1:] if not a.startswith("--")]
letter = words[0]
meta = json.load(open(os.path.join(OUT, "parts", "set%s.json" % letter)))
names = words[1:] or [p["name"] for p in meta["props"]]
DO_SET = "--set=0" not in sys.argv


def stage(w=640, h=560):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    sc.render.film_transparent = True
    wd = bpy.data.worlds.new("w"); sc.world = wd; wd.use_nodes = True
    wd.node_tree.nodes["Background"].inputs[0].default_value = (0.50, 0.62, 0.86, 1)      # the cool sky
    wd.node_tree.nodes["Background"].inputs[1].default_value = 0.75
    for rot, energy, col in (((50, 0, -30), 2.7, (1.0, 0.95, 0.84)), ((120, 0, 150), 0.9, (0.7, 0.82, 1.0))):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.data.color = col; sun.rotation_euler = tuple(math.radians(a) for a in rot)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.type = "ORTHO"; cam.data.clip_end = 500
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = w, h
    return sc, cam


_ink = {}


def ink(o, thick):
    line = _ink.get(id(bpy.context.scene))
    if line is None or line.name not in bpy.data.materials:
        line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
        b = line.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (0.03, 0.02, 0.03, 1); b.inputs["Roughness"].default_value = 1
        _ink.clear(); _ink[id(bpy.context.scene)] = line
    for m in o.data.materials:
        if m:
            m.use_backface_culling = True
    o.data.materials.append(line)
    s = o.modifiers.new("ink", "SOLIDIFY")
    s.thickness = thick; s.offset = 1; s.use_flip_normals = True; s.material_offset = len(o.data.materials) - 1; s.use_rim = False


def load(name, at=(0.0, 0.0), yaw=0.0):
    bpy.ops.import_scene.gltf(filepath=os.path.join(OUT, name + ".glb"))
    meshes = [o for o in bpy.context.selected_objects if o.type == "MESH"]
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for o in meshes:
        o.rotation_mode = "XYZ"
        o.rotation_euler = (0, 0, math.radians(yaw)); o.location = (at[0], at[1], 0)
        for m in o.data.materials:
            b = m.node_tree.nodes.get("Principled BSDF") if m and m.use_nodes else None
            tex = next((n for n in m.node_tree.nodes if n.type == "TEX_IMAGE"), None) if b else None
            if tex:
                m.node_tree.links.new(tex.outputs["Color"], b.inputs["Emission Color"]); b.inputs["Emission Strength"].default_value = 0.34
    bpy.context.view_layer.update()
    for o in meshes:
        for c in o.bound_box:
            p = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    return meshes, lo, hi


def shoot(cam, centre, direction, scale, path):
    cam.data.ortho_scale = scale
    cam.location = centre + Vector(direction).normalized() * 60.0
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def flat(name, rgb):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1); b.inputs["Roughness"].default_value = 1
    b.inputs["Emission Color"].default_value = (*rgb, 1); b.inputs["Emission Strength"].default_value = 0.3
    return m


def cube(lo, hi, mat, bevel=0.03):
    bpy.ops.mesh.primitive_cube_add(size=1, location=((lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, (lo[2] + hi[2]) / 2))
    o = bpy.context.object
    o.scale = (hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2])
    bpy.ops.object.transform_apply(scale=True)
    bv = o.modifiers.new("bv", "BEVEL"); bv.width = bevel; bv.segments = 2
    o.data.materials.append(mat)
    return o


def figure(x, y):
    """A 1.75 m person in the cast's manner: box head, block body."""
    skin, shirt, pants, hair = flat("skin", (0.62, 0.40, 0.26)), flat("shirt", (0.90, 0.86, 0.74)), flat("pants", (0.16, 0.16, 0.2)), flat("hair", (0.06, 0.05, 0.05))
    parts = [cube((-0.2, -0.11, 0.0), (-0.03, 0.11, 0.86), pants), cube((0.03, -0.11, 0.0), (0.2, 0.11, 0.86), pants),
             cube((-0.24, -0.14, 0.84), (0.24, 0.14, 1.36), shirt), cube((-0.34, -0.08, 0.86), (-0.25, 0.08, 1.34), skin), cube((0.25, -0.08, 0.86), (0.34, 0.08, 1.34), skin),
             cube((-0.19, -0.18, 1.38), (0.19, 0.18, 1.72), skin, 0.06), cube((-0.2, -0.12, 1.62), (0.2, 0.2, 1.76), hair, 0.05)]
    for o in parts:
        o.location.x += x; o.location.y += y
        ink(o, 0.012)


TOP, LOW = (0.70, -0.80, 0.60), (-0.74, 0.76, 0.20)

for name in names:
    sc, cam = stage()
    meshes, lo, hi = load(name)
    size = hi - lo
    big = max(size.x, size.y, size.z)
    for o in meshes:
        ink(o, min(0.016, max(0.004, big * 0.006)))
    centre = (lo + hi) * 0.5
    scale = max(big * 1.28, size.length * 0.92)
    shoot(cam, centre, TOP, scale, os.path.join(SHOTS, name + "_top.png"))
    shoot(cam, centre, LOW, scale, os.path.join(SHOTS, name + "_low.png"))

if DO_SET:
    sc, cam = stage(1600, 900)
    sc.render.film_transparent = False
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for p in meta["props"]:
        meshes, a, b = load(p["name"], (p["at"][0], -p["at"][1]), p["yaw"])
        for o in meshes:
            o.location.z += p.get("lift", 0.0)
            ink(o, 0.011)
        lo = Vector(map(min, lo, a)); hi = Vector(map(max, hi, b))
    fx, fy = meta.get("figure", (hi.x + 0.6, -0.4))
    figure(fx, -fy)
    hi.x = max(hi.x, fx + 0.4); hi.z = max(hi.z, 1.8)
    bpy.ops.mesh.primitive_plane_add(size=300, location=(0, 0, 0))
    bpy.context.object.data.materials.append(flat("ground", (0.36, 0.33, 0.27)))
    centre = (lo + hi) * 0.5
    centre.z = hi.z * 0.42
    size = hi - lo
    scale = max(size.x * 0.95 + size.y * 0.45, (size.z * 0.95 + size.y * 0.5) * 16 / 9) * 1.06
    shoot(cam, centre, (0.42, -0.86, 0.46), scale, os.path.join(SHOTS, "set%s_all.png" % letter))
print("done")
