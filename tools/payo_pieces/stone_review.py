"""Four pictures of ONE Payo piece, alone on a plain ground (step 3 of tools/payo_pieces/stone_kit.py). Blender, not the game.

  blender -b --python tools/payo_pieces/stone_review.py -- <piece> [...] [--out=folder] [--all=name]

Per piece, into Logs/payo/pieces/shots: <piece>_front.png (three-quarter, from a person's eye 1.7 m up, a 1.75 m
figure standing by it), _back.png, _close.png (its best detail; the build script says where), _above.png. A toon-ish
look (flat light, a little of its own colour as light) with an ink line, as tools/review_slippers.py.
With --all=name every piece named is stood in a row at one scale and shot once: <name>_all.png.
"""
import bpy, json, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SHOTS = os.path.join(ROOT, "Logs", "payo", "pieces", "stone", "shots")


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


OUT = arg("out", os.path.join(ROOT, "Logs", "payo", "pieces", "build", "stone"))
ALL = arg("all")
ids = [a for a in sys.argv[sys.argv.index("--") + 1:] if not a.startswith("--")]
os.makedirs(SHOTS, exist_ok=True)


def stage():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.62, 0.90, 1)     # a cool blue sky fill: grey stone under cool light
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    for rot, energy in (((52, 0, -34), 2.7), ((118, 0, 150), 1.0)):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.rotation_euler = tuple(math.radians(a) for a in rot)
        sun.data.color = (1.0, 0.95, 0.86) if energy > 2 else (0.7, 0.82, 1.0)   # a warm-white sun, a cool fill
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
        b.inputs["Base Color"].default_value = (0.01, 0.03, 0.05, 1); b.inputs["Roughness"].default_value = 1
    for m in o.data.materials:
        if m:
            m.use_backface_culling = True
    o.data.materials.append(line)
    s = o.modifiers.new("ink", "SOLIDIFY")
    s.thickness = thick; s.offset = 1; s.use_flip_normals = True; s.material_offset = len(o.data.materials) - 1; s.use_rim = False


def cube(sc, lo, hi, mat, bevel=0.03):
    bpy.ops.mesh.primitive_cube_add(size=1, location=[(a + b) / 2 for a, b in zip(lo, hi)])
    o = bpy.context.object
    o.scale = [abs(b - a) for a, b in zip(lo, hi)]
    bpy.ops.object.transform_apply(scale=True)
    bv = o.modifiers.new("b", "BEVEL"); bv.width = bevel; bv.segments = 2
    o.data.materials.append(mat)
    for p in o.data.polygons:
        p.use_smooth = True
    return o


def figure(sc, x, y, z=0.0):
    """A 1.75 m person in the cast's manner: box head, block body."""
    skin, shirt, pants, hair = flat("skin", (0.62, 0.40, 0.26)), flat("shirt", (0.90, 0.86, 0.74)), flat("pants", (0.16, 0.16, 0.2)), flat("hair", (0.06, 0.05, 0.05))
    parts = [cube(sc, (-0.2, -0.11, 0.0), (-0.03, 0.11, 0.86), pants), cube(sc, (0.03, -0.11, 0.0), (0.2, 0.11, 0.86), pants),
             cube(sc, (-0.24, -0.14, 0.84), (0.24, 0.14, 1.36), shirt), cube(sc, (-0.34, -0.08, 0.86), (-0.25, 0.08, 1.34), skin), cube(sc, (0.25, -0.08, 0.86), (0.34, 0.08, 1.34), skin),
             cube(sc, (-0.19, -0.18, 1.38), (0.19, 0.18, 1.72), skin, 0.06), cube(sc, (-0.2, -0.12, 1.62), (0.2, 0.2, 1.76), hair, 0.05)]
    for o in parts:
        o.location.x += x; o.location.y += y; o.location.z += z
        ink(o, 0.012)
    return parts


def load(name, at=(0.0, 0.0)):
    bpy.ops.import_scene.gltf(filepath=os.path.join(OUT, name + ".glb"))
    meshes = [o for o in bpy.context.selected_objects if o.type == "MESH"]
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for o in meshes:
        o.location.x += at[0]; o.location.y += at[1]
        bpy.context.view_layer.update()
        for m in o.data.materials:
            b = m.node_tree.nodes.get("Principled BSDF") if m and m.use_nodes else None
            tex = next((n for n in m.node_tree.nodes if n.type == "TEX_IMAGE"), None) if b else None
            if tex:
                m.node_tree.links.new(tex.outputs["Color"], b.inputs["Emission Color"]); b.inputs["Emission Strength"].default_value = 0.22
        for c in o.bound_box:
            p = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    return meshes, lo, hi


def ground(sc, z, size=400):
    bpy.ops.mesh.primitive_plane_add(size=size, location=(0, 0, z))
    bpy.context.object.data.materials.append(flat("ground", (0.42, 0.44, 0.46)))


def shoot(sc, cam, eye, look, fov, path):
    cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


def B(p):
    """The build script's (x, y up, z front) as Blender's (x, y, z up)."""
    return Vector((p[0], -p[2], p[1]))


if ALL:
    sc, cam = stage()
    x = 0.0
    spans = []
    for name in ids:
        meta = json.load(open(os.path.join(OUT, "parts", name + ".json")))
        w = meta.get("width", 8.0)
        meshes, lo, hi = load(name, (x + w / 2, 0.0))
        gz = meta.get("ground", 0.0)
        for o in meshes:
            o.location.z -= gz
            ink(o, 0.035)
        spans.append((x, x + w))
        figure(sc, x + w + 0.8, -1.0, 0.0)
        x += w + 4.0
    ground(sc, 0.0)
    total = x - 4.0
    shoot(sc, cam, (total / 2, -total * 0.95, 9.0), (total / 2, 0, 7.5), 46, os.path.join(SHOTS, ALL + "_all.png"))
else:
    for name in ids:
        meta = json.load(open(os.path.join(OUT, "parts", name + ".json")))
        sc, cam = stage()
        meshes, lo, hi = load(name)
        gz = meta.get("ground", lo.z)
        for o in meshes:
            ink(o, meta.get("ink", 0.03))
        ground(sc, gz)
        fx, fy = meta.get("figure", (hi.x + 1.2, lo.y - 1.0))
        figure(sc, fx, fy, meta.get("figure_z", gz))
        c = (lo + hi) * 0.5
        d = meta.get("distance", max(hi.x - lo.x, hi.z - lo.z) * 1.5)
        look = Vector((c.x, c.y, meta.get("look_z", gz + (hi.z - gz) * meta.get("look", 0.45))))
        ez = meta.get("eye_z", gz + 1.7)
        fov = meta.get("fov", 62)
        shoot(sc, cam, (c.x + d * 0.62, c.y - d * 0.78, ez), look, fov, os.path.join(SHOTS, name + "_front.png"))
        shoot(sc, cam, (c.x - d * 0.66, c.y + d * 0.75, ez + meta.get("back_up", 0.0)), look, fov, os.path.join(SHOTS, name + "_back.png"))
        ct, ce = B(meta["close"][0]), B(meta["close"][1])
        shoot(sc, cam, ce, ct, 50, os.path.join(SHOTS, name + "_close.png"))
        h = max(hi.x - lo.x, hi.y - lo.y, (hi.z - gz) * 0.8)
        shoot(sc, cam, (c.x + h * 0.5, c.y - h * 0.9, hi.z + h * 0.9), (c.x, c.y, gz + (hi.z - gz) * 0.6), 44, os.path.join(SHOTS, name + "_above.png"))
        for extra, (eye, tgt, f) in meta.get("extra", {}).items():
            shoot(sc, cam, B(eye), B(tgt), f, os.path.join(SHOTS, name + "_" + extra + ".png"))
print("done")
