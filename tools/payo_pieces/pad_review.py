"""Review renders for the meso pads (tools/payo_pieces/pad_meso.py). Blender 5.0, headless.

  blender -b --python tools/payo_pieces/pad_review.py -- <ABS Logs/payo/pieces/pads> <ABS repo root> [prop ...]

Each prop three ways: NEW (pads/<name>.glb), the CURRENT Payo glb (Art/Payo/Models, read only) and the ARENA original
(Art/Arena/Props with its painted props texture, lifted so its foot is on the floor). Three cameras: a player's eye
(1.7 m up, 5 m away), a three-quarter top view, and 10 m away at eye height. A warm low sunset key (the map's look),
a cool sky fill, a warm grey stone floor (the pads stand on the stage blocks), the reviews' ink hull, a 1.75 m figure.
The geyser's NEW and CURRENT shots carry a stand-in for PayoGeyser's resting pool (radius 0.62 m, 2 cm under the floor
point, 6 cm dome), which is the game's, so the water line can be judged.
"""
import bpy, bmesh, math, os, sys
from mathutils import Vector

PADS, ROOT = sys.argv[sys.argv.index("--") + 1:][:2]
ONLY = sys.argv[sys.argv.index("--") + 3:]                 # optional: only these props
SHOTS = os.path.join(PADS, "shots")
os.makedirs(SHOTS, exist_ok=True)
PAYO = os.path.join(ROOT, "Assets/TumbangPreso/Art/Payo/Models")
ARENA = os.path.join(ROOT, "Assets/TumbangPreso/Art/Arena/Props")
ARENA_TEX = os.path.join(ROOT, "Assets/TumbangPreso/Art/Arena/Textures")
PROPS = [("pad_geyser", "arena_jump_pad"), ("pad_speed", "arena_speed_pad"), ("pickup_plinth", "arena_pickup")]


def stage(res=(720, 470)):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.78, 0.62, 0.58, 1)      # a warm dusk sky
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    # the sunset key: low (13 degrees up), deep warm; a cool violet fill from the other side
    for rot, energy, col in (((77, 0, -38), 3.6, (1.0, 0.70, 0.46)), ((115, 0, 150), 0.7, (0.60, 0.66, 1.0))):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.data.color = col; sun.rotation_euler = tuple(math.radians(a) for a in rot)
        sun.data.angle = math.radians(2)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.clip_start = 0.05; cam.data.clip_end = 500
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = res
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, -0.01))       # the block's top, 1 cm under the pad's floor point
    g = bpy.data.materials.new("floor"); g.use_nodes = True
    b = g.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.44, 0.40, 0.35, 1); b.inputs["Roughness"].default_value = 1
    bpy.context.object.data.materials.append(g)
    # a 1.75 m figure for scale
    bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=1.75, location=(-1.9, 0.4, 0.875 - 0.01), vertices=16)
    f = bpy.data.materials.new("figure"); f.use_nodes = True
    f.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.25, 0.32, 0.45, 1)
    bpy.context.object.data.materials.append(f)
    return sc, cam


def ink(o, thick):
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
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.normal_update()
    for v in bm.verts:
        v.co = v.co - v.normal * thick
    bm.to_mesh(h.data); bm.free()
    h.visible_shadow = False
    return h


def load(path, arena=False):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    meshes = [o for o in new if o.type == "MESH"]
    for o in meshes:
        for m in o.data.materials:
            if not (m and m.use_nodes):
                continue
            nt = m.node_tree
            b = nt.nodes.get("Principled BSDF")
            if arena:
                tex = nt.nodes.new("ShaderNodeTexImage")
                tex.image = bpy.data.images.load(os.path.join(ARENA_TEX, "arena_stage_props.png"))
                nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
                em = nt.nodes.new("ShaderNodeTexImage")
                em.image = bpy.data.images.load(os.path.join(ARENA_TEX, "arena_stage_props_emit.png"))
                nt.links.new(em.outputs["Color"], b.inputs["Emission Color"])
                b.inputs["Emission Strength"].default_value = 1.2
                continue
            tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE" and n.image and "normal" not in n.image.name.lower()), None)
            if b and tex:
                nt.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
                b.inputs["Emission Strength"].default_value = 0.22
                b.inputs["Roughness"].default_value = 1.0
    bpy.context.view_layer.update()
    lo = min((o.matrix_world @ Vector(c)).z for o in meshes for c in o.bound_box)
    if arena:                                            # the Arena props are centred on their middle: foot to the floor
        for o in [o for o in new if o.parent is None]:
            o.location.z -= lo
    out = list(meshes) + [ink(o, 0.012) for o in meshes]
    return out


def pool():
    """A stand-in for PayoGeyser's resting pool: its lathe (0.06 at the axis to 0 at the rim), radius 0.62, at -0.02."""
    prof = [(0.0, 0.06), (0.55, 0.045), (0.9, 0.015), (1.0, 0.0)]
    bm = bmesh.new()
    seg = 40
    rings = []
    for (r, y) in prof:
        rings.append([bm.verts.new((0.62 * r * math.cos(2 * math.pi * k / seg), 0.62 * r * math.sin(2 * math.pi * k / seg), -0.02 + y)) for k in range(seg)])
    for a, b in zip(rings, rings[1:]):
        for k in range(seg):
            bm.faces.new((a[k], b[k], b[(k + 1) % seg], a[(k + 1) % seg]))
    me = bpy.data.meshes.new("pool"); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new("pool", me); bpy.context.scene.collection.objects.link(o)
    m = bpy.data.materials.new("water"); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.36, 0.76, 0.82, 1); b.inputs["Roughness"].default_value = 0.15
    b.inputs["Emission Color"].default_value = (0.36, 0.76, 0.82, 1); b.inputs["Emission Strength"].default_value = 0.25
    try:
        b.inputs["Alpha"].default_value = 0.82
        m.surface_render_method = "BLENDED"
    except Exception:
        pass
    me.materials.append(m)
    for f in me.polygons:
        f.use_smooth = True
    return [o]


def show(objs, vis):
    for o in objs:
        o.hide_render = not vis


def aim(cam, eye, look, fov):
    cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()


def shoot(sc, path):
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("SHOT", path)


for name, arena_name in PROPS:
    if ONLY and name not in ONLY:
        continue
    sc, cam = stage()
    sets = {"new": load(os.path.join(PADS, name + ".glb")),
            "payo": load(os.path.join(PAYO, name + ".glb")),
            "arena": load(os.path.join(ARENA, arena_name + ".glb"), arena=True)}
    if name == "pad_geyser":
        p = pool()
        sets["new"] += p
        sets["payo"] += [o.copy() for o in p]
        for o in sets["payo"][-1:]:
            sc.collection.objects.link(o)
    H = {"pad_geyser": 0.17, "pad_speed": 0.05, "pickup_plinth": 0.4}[name]
    # glTF +z (the speed pad's travel) is Blender -Y: the player walks onto it from +Y, looking down -Y
    if name == "pad_speed":
        views = {"eye5": (Vector((1.3, 5.0, 1.7)), Vector((0, -0.3, 0.0)), 42),
                 "top34": (Vector((2.6, 2.6, 3.6)), Vector((0, 0, 0.0)), 50),
                 "far10": (Vector((2.0, 10.0, 1.7)), Vector((0, 0, 0.0)), 30)}
    else:
        views = {"eye5": (Vector((2.2, -4.5, 1.7)), Vector((0, 0, H * 0.5)), 38),
                 "top34": (Vector((1.9, -2.0, 2.6 + H)), Vector((0, 0, H * 0.3)), 50),
                 "far10": (Vector((4.0, -9.2, 1.7)), Vector((0, 0, H * 0.5)), 26)}
    for vname, (eye, look, fov) in views.items():
        aim(cam, eye, look, fov)
        for k, objs in sets.items():
            for kk, oo in sets.items():
                show(oo, kk == k)
            shoot(sc, os.path.join(SHOTS, "%s_%s_%s.png" % (name, k, vname)))
print("done")
