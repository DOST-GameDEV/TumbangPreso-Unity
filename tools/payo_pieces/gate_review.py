"""Four pictures of ONE gate piece, alone on a plain ground (step 3 of tools/payo_pieces/gate_kit.py). Blender, not the game.

  blender -b --python tools/payo_pieces/gate_review.py -- <piece> [...] [--night]

Into Logs/payo/pieces/gate/shots: <piece>_front.png (three-quarter from a person's eye 1.7 m up, a 1.75 m figure by it),
_back.png, _close.png (its best detail: the build script says where), _above.png. Toon-ish: flat light, a little of
its own colour as light, an ink line. The LIGHT is cool (blue sky) over a warm-white sun; the stone itself is grey.
"""
import bpy, json, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SHOTS = os.path.join(ROOT, "Logs", "payo", "pieces", "gate", "shots")
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "gate")
NIGHT = "--night" in sys.argv
ids = [a for a in sys.argv[sys.argv.index("--") + 1:] if not a.startswith("--")]
os.makedirs(SHOTS, exist_ok=True)


def stage():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.05, 0.08, 0.2, 1) if NIGHT else (0.42, 0.62, 0.90, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.5 if NIGHT else 0.9
    for rot, energy, col in (((52, 0, -34), 0.5 if NIGHT else 2.6, (0.6, 0.7, 1.0) if NIGHT else (1.0, 0.95, 0.86)), ((118, 0, 150), 0.2 if NIGHT else 0.8, (0.7, 0.85, 1.0))):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.data.color = col; sun.rotation_euler = tuple(math.radians(a) for a in rot)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.clip_end = 2000
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = 1100, 800
    return sc, cam


def flat(name, rgb):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1); b.inputs["Roughness"].default_value = 1
    b.inputs["Emission Color"].default_value = (*rgb, 1); b.inputs["Emission Strength"].default_value = 0.3
    return m


def ink(o, thick):
    line = bpy.data.materials.get("ink")
    if line is None:
        line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
        b = line.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (0.02, 0.02, 0.03, 1); b.inputs["Roughness"].default_value = 1
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
    for p in o.data.polygons:
        p.use_smooth = True
    return o


def figure(x, y, z=0.0):
    """A 1.75 m person in the cast's manner: box head, block body."""
    skin, shirt, pants, hair = flat("skin", (0.62, 0.40, 0.26)), flat("shirt", (0.90, 0.3, 0.2)), flat("pants", (0.16, 0.16, 0.2)), flat("hair", (0.06, 0.05, 0.05))
    parts = [cube((-0.2, -0.11, 0.0), (-0.03, 0.11, 0.86), pants), cube((0.03, -0.11, 0.0), (0.2, 0.11, 0.86), pants),
             cube((-0.24, -0.14, 0.84), (0.24, 0.14, 1.36), shirt), cube((-0.34, -0.08, 0.86), (-0.25, 0.08, 1.34), skin), cube((0.25, -0.08, 0.86), (0.34, 0.08, 1.34), skin),
             cube((-0.19, -0.18, 1.38), (0.19, 0.18, 1.72), skin, 0.06), cube((-0.2, -0.12, 1.62), (0.2, 0.2, 1.76), hair, 0.05)]
    for o in parts:
        o.location.x += x; o.location.y += y; o.location.z += z
        ink(o, 0.012)


def load(name):
    bpy.ops.import_scene.gltf(filepath=os.path.join(OUT, name + ".glb"))
    meshes = [o for o in bpy.context.selected_objects if o.type == "MESH"]
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    bpy.context.view_layer.update()
    for o in meshes:
        for m in o.data.materials:
            b = m.node_tree.nodes.get("Principled BSDF") if m and m.use_nodes else None
            tex = next((n for n in m.node_tree.nodes if n.type == "TEX_IMAGE"), None) if b else None
            if tex:
                m.node_tree.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
                glow = "glow" in m.name
                b.inputs["Emission Strength"].default_value = (2.5 if NIGHT else 0.9) if glow else (0.1 if NIGHT else 0.3)
        for c in o.bound_box:
            p = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    return meshes, lo, hi


def shoot(sc, cam, eye, look, fov, path):
    cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


def B(p):
    """The build script's (x, y up, z front) as Blender's (x, y, z up)."""
    return Vector((p[0], -p[2], p[1]))


for name in ids:
    meta = json.load(open(os.path.join(OUT, "parts", name + ".json")))
    sc, cam = stage()
    meshes, lo, hi = load(name)
    for o in meshes:
        ink(o, meta.get("ink", 0.03))
    bpy.ops.mesh.primitive_plane_add(size=600, location=(0, 0, 0))
    bpy.context.object.data.materials.append(flat("ground", (0.36, 0.44, 0.32)))
    fx, fz = meta.get("figure", (hi.x + 1.0, 1.0))
    figure(fx, -fz, meta.get("figure_y", 0.0))
    c = (lo + hi) * 0.5
    d = meta.get("distance", max(hi.x - lo.x, hi.z) * 1.5)
    look = Vector((c.x, c.y, hi.z * meta.get("look", 0.45)))
    fov = meta.get("fov", 60)
    sfx = "_night" if NIGHT else ""
    shoot(sc, cam, (c.x + d * 0.55, c.y - d * 0.84, 1.7), look, fov, os.path.join(SHOTS, name + "_front" + sfx + ".png"))
    if not NIGHT:
        shoot(sc, cam, (c.x - d * 0.62, c.y + d * 0.78, 1.7 + meta.get("back_up", 0.0)), look, fov, os.path.join(SHOTS, name + "_back.png"))
        shoot(sc, cam, B(meta["close"][1]), B(meta["close"][0]), meta.get("close_fov", 50), os.path.join(SHOTS, name + "_close.png"))
        h = max(hi.x - lo.x, hi.y - lo.y, hi.z * 0.8)
        shoot(sc, cam, (c.x + h * 0.45, c.y - h * 0.85, hi.z + h * 0.8), (c.x, c.y, hi.z * 0.55), 44, os.path.join(SHOTS, name + "_above.png"))
    for extra, (eye, tgt, f) in meta.get("extra", {}).items():
        shoot(sc, cam, B(eye), B(tgt), f, os.path.join(SHOTS, name + "_" + extra + sfx + ".png"))
print("done")
