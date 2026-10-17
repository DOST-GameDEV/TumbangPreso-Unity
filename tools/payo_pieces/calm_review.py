"""The CALM pilot's review renders (Logs/payo/pieces/CALM_STYLE.md). Blender 5.0, headless.

  blender -b --python tools/payo_pieces/calm_review.py -- piece <name> <old.glb> <calm.glb> <lift> <shots dir>
  blender -b --python tools/payo_pieces/calm_review.py -- together <shots dir> <name> <old.glb> <calm.glb> <lift> <x> [...]

Warm late-afternoon light (a low warm sun, a soft blue sky), the reviews' toon-ish finish (a little of the texture as
its own light, an ink line by an inverted hull), the same camera for OLD and CALM. Distances are from the piece's
centre: 8 m (close), 40 m and 120 m (piece), 60 m and 150 m (together). `lift` raises a piece whose origin is not on
the ground (the monolith's origin is the centre of its top, 13.7 m up). The calm glb carries its normal map; one
extra close shot of it has the normal map unplugged and one shows the normal map alone on flat grey.
Then python tools/payo_pieces/calm_sheet.py lays the shots out.
"""
import bpy, math, os, sys
from mathutils import Vector

args = [os.path.abspath(a) if (a.endswith(".glb") or "/" in a) else a for a in sys.argv[sys.argv.index("--") + 1:]]


def stage(res=(960, 640)):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.68, 0.80, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    # the late-afternoon sun: low (24 degrees up), warm, from the front left; a cool soft fill from behind
    for rot, energy, col in (((66, 0, -42), 3.4, (1.0, 0.80, 0.58)), ((120, 0, 150), 0.6, (0.62, 0.74, 1.0))):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.data.color = col; sun.rotation_euler = tuple(math.radians(a) for a in rot)
        sun.data.angle = math.radians(3)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.clip_end = 3000
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = res
    bpy.ops.mesh.primitive_plane_add(size=3000, location=(0, 0, 0))
    g = bpy.data.materials.new("ground"); g.use_nodes = True
    b = g.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.40, 0.46, 0.30, 1); b.inputs["Roughness"].default_value = 1
    bpy.context.object.data.materials.append(g)
    return sc, cam


def ink(o, thick):
    """The ink line as a SEPARATE inverted hull (a copy pushed out along its normals, faces reversed, back faces culled):
    a solidify modifier on the piece itself loses its tangent space, and the normal map would not show."""
    import bmesh
    line = bpy.data.materials.get("ink")
    if line is None:
        line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
        b = line.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (0.03, 0.03, 0.05, 1); b.inputs["Roughness"].default_value = 1
    for m in o.data.materials:
        if m:
            m.use_backface_culling = True
    h = o.copy(); h.data = o.data.copy(); h.name = o.name + "_ink"
    for c in o.users_collection:
        c.objects.link(h)
    if "custom_normal" in h.data.attributes:
        h.data.attributes.remove(h.data.attributes["custom_normal"])
    h.data.materials.clear(); h.data.materials.append(line)
    bm = bmesh.new(); bm.from_mesh(h.data)
    for f in bm.faces:
        f.material_index = 0
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)    # one hull, no cracks at the split corners
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.normal_update()
    for v in bm.verts:
        v.co = v.co - v.normal * thick                    # the normals now point in: push the hull out
    bm.to_mesh(h.data); bm.free()
    h.visible_shadow = False                              # a closed hull casting shadow would put the whole piece in shade
    return h


def load(path, at=(0, 0, 0)):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in new if o.parent is None]
    for o in roots:
        o.location = Vector(o.location) + Vector(at)
    meshes = [o for o in new if o.type == "MESH"]
    for o in meshes:
        for m in o.data.materials:
            if not (m and m.use_nodes):
                continue
            b = m.node_tree.nodes.get("Principled BSDF")
            tex = next((n for n in m.node_tree.nodes if n.type == "TEX_IMAGE" and n.image and "normal" not in n.image.name.lower()), None)
            if b and tex:
                m.node_tree.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
                b.inputs["Emission Strength"].default_value = 0.9 if "glow" in m.name else 0.22
                b.inputs["Roughness"].default_value = 1.0
    bpy.context.view_layer.update()
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for o in meshes:
        for c in o.bound_box:
            p = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    meshes += [ink(o, 0.035) for o in list(meshes)]
    return meshes, lo, hi


def normals(meshes, on=True, grey=False):
    for o in meshes:
        for m in o.data.materials:
            if not (m and m.use_nodes) or m.name == "ink":
                continue
            nt = m.node_tree
            b = nt.nodes.get("Principled BSDF")
            nm = next((n for n in nt.nodes if n.type == "NORMAL_MAP"), None)
            if nm is not None:
                if not on:
                    for l in list(b.inputs["Normal"].links):
                        nt.links.remove(l)
                else:
                    nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
            if grey and "glow" not in m.name:
                for l in list(b.inputs["Base Color"].links) + list(b.inputs["Emission Color"].links):
                    nt.links.remove(l)
                b.inputs["Base Color"].default_value = (0.62, 0.60, 0.57, 1)
                b.inputs["Emission Color"].default_value = (0.62, 0.60, 0.57, 1)


def show(meshes, vis):
    for o in meshes:
        o.hide_render = not vis


def aim(cam, eye, look, fov):
    cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()


def shoot(sc, path):
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


DIR = Vector((0.55, -0.835, 0)).normalized()          # the front three-quarter (the pieces' front is -Y)


def piece(name, old, calm, lift, shots):
    os.makedirs(shots, exist_ok=True)
    sc, cam = stage()
    A, lo, hi = load(old, (0, 0, lift))
    B, _, _ = load(calm, (0, 0, lift))
    H = hi.z
    c = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, 0))
    rad = max(hi.x - lo.x, hi.y - lo.y) / 2
    views = {
        "8m": (c + DIR * (rad + 8.0) + Vector((0, 0, max(2.2, H * 0.3))), c + Vector((0, 0, H * 0.5)), 62),
        "40m": (c + DIR * 40.0 + Vector((0, 0, 6.0)), c + Vector((0, 0, H * 0.42)), 50),
        "120m": (c + DIR * 120.0 + Vector((0, 0, 18.0)), c + Vector((0, 0, H * 0.42)), 50),
    }
    for k, (eye, look, fov) in views.items():
        aim(cam, eye, look, fov)
        show(A, True); show(B, False); shoot(sc, os.path.join(shots, "%s_old_%s.png" % (name, k)))
        show(A, False); show(B, True); shoot(sc, os.path.join(shots, "%s_calm_%s.png" % (name, k)))
    eye, look, fov = views["8m"]
    aim(cam, eye, look, fov)
    show(A, False); show(B, True)
    normals(B, on=False); shoot(sc, os.path.join(shots, "%s_calm_8m_nonormal.png" % name))
    normals(B, on=True, grey=True); shoot(sc, os.path.join(shots, "%s_calm_8m_normalonly.png" % name))


def together(shots, items):
    os.makedirs(shots, exist_ok=True)
    sc, cam = stage((1200, 640))
    A, B = [], []
    for name, old, calm, lift, x in items:
        a, _, _ = load(old, (x, 0, lift)); A += a
        b, _, _ = load(calm, (x, 0, lift)); B += b
    for d, up in ((60, 9.0), (150, 24.0)):
        eye = Vector((0, 0, 0)) + Vector((0.25, -1, 0)).normalized() * d + Vector((0, 0, up))
        aim(cam, eye, Vector((0, 0, 7.0)), 44 if d == 60 else 30)
        show(A, True); show(B, False); shoot(sc, os.path.join(shots, "together_old_%dm.png" % d))
        show(A, False); show(B, True); shoot(sc, os.path.join(shots, "together_calm_%dm.png" % d))


if args[0] == "piece":
    piece(args[1], args[2], args[3], float(args[4]), args[5])
else:
    rest = args[2:]
    items = [(rest[i], rest[i + 1], rest[i + 2], float(rest[i + 3]), float(rest[i + 4])) for i in range(0, len(rest), 5)]
    together(args[1], items)
print("done")
