"""Render the review set of the Payo audience v2 villager `boy`, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_payo_audience_v2_boy.py -- v01
    ... -- v01 row face turn clips look close      (only some shots)
    py -3 tools/sheet_payo_audience_v2_boy.py v01  (the labelled review sheets)

Writes single frames into ArtSource/payo/audience/v2/boy/renders/<version>/; the sheet script
stacks and labels them into ArtSource/payo/audience/v2/boy/renders/<version>_*.png. A NEW VERSION
NAME EVERY ITERATION, and LOOK at what comes out before believing it.

A copy of tools/render_character_redesign_totoy.py (his base), rewritten for him: the same toon
approximation of `TumbangPreso/Toon` (a two-band ramp, the inverted-hull ink edge at 0.0045). IT IS
AN APPROXIMATION, for judging shape, paint, silhouette and finish, not final colour.

Shots:
  row     THE TEST (payo-audience-brief.md): him in a row with three redesigned heroes (Rafi, Dante,
          Cheska) and the three approved Classics (Bayan, Bebang, Lola Pacing), with his BASE
          CLASSIC TOTOY RIGHT BESIDE HIM so the finish can be compared (the owner's note on v1:
          "they look noisy"). Front, three-quarter, back, the heads close, and a far small view
  face    his face beside Dante's and Cheska's, front and three-quarter
  turn    the turnaround
  clips   stills of `idle` (one inside each stance), `cheer`, `clap`, and `sit` ON A LEDGE BLOCK
  look    the head turned 55 degrees each way and nodded 22
  close   the pieces close
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Quaternion, Vector

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
OUTLINE = 0.0045
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["v0"]
V = args[0]
only = set(a for a in args[1:])
BASE_OUT = REPO + "/ArtSource/payo/audience/v2/boy/renders"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/ArtSource/payo/audience/v2/boy/audience_v2_boy.glb"
CR = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/"
RAFI = CR + "rafi/rafi-redesign.glb"
DANTE = CR + "dante/dante-redesign.glb"
CHESKA = CR + "cheska/cheska-redesign.glb"
BAYAN = CR + "bayan/bayan-redesign.glb"
BEBANG = CR + "bebang/bebang-redesign.glb"
LOLA = CR + "lola_pacing/lola_pacing-redesign.glb"
TOTOY = CR + "totoy/totoy-redesign.glb"
# THE ROW: heroes and Classics, his base Totoy right beside him
ROW = (RAFI, DANTE, BAYAN, TOTOY, NEW, BEBANG, CHESKA, LOLA)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    sc.render.film_transparent = False
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.135, 0.14, 0.17, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    return sc


def toon_material(name, image, interp="Linear"):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = image; tex.interpolation = interp
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); dif.inputs[0].default_value = (1, 1, 1, 1)
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB"); ramp.color_ramp.interpolation = "LINEAR"
    ramp.color_ramp.elements[0].position = 0.10; ramp.color_ramp.elements[0].color = (0.45, 0.47, 0.56, 1)
    ramp.color_ramp.elements[1].position = 0.14; ramp.color_ramp.elements[1].color = (1.0, 0.97, 0.90, 1)
    mul = nt.nodes.new("ShaderNodeMixRGB"); mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1
    em = nt.nodes.new("ShaderNodeEmission")
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs[0], ramp.inputs[0])
    nt.links.new(tex.outputs[0], mul.inputs[1]); nt.links.new(ramp.outputs[0], mul.inputs[2])
    nt.links.new(mul.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
    return m


def flat_toon(name, colour):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); dif.inputs[0].default_value = (1, 1, 1, 1)
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.10; ramp.color_ramp.elements[0].color = (colour[0] * 0.6, colour[1] * 0.6, colour[2] * 0.7, 1)
    ramp.color_ramp.elements[1].position = 0.16; ramp.color_ramp.elements[1].color = (*colour, 1)
    em = nt.nodes.new("ShaderNodeEmission"); out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs[0], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
    return m


def ink_material():
    m = bpy.data.materials.get("ink")
    if m: return m
    m = bpy.data.materials.new("ink"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission"); em.inputs[0].default_value = (0.002, 0.002, 0.003, 1)
    out = nt.nodes.new("ShaderNodeOutputMaterial"); nt.links.new(em.outputs[0], out.inputs[0])
    m.use_backface_culling = True
    return m


def add_outline(obj, width=OUTLINE):
    hull = obj.copy(); hull.data = obj.data.copy(); hull.name = obj.name + "-ink"
    for c in obj.users_collection: c.objects.link(hull)
    bm = bmesh.new(); bm.from_mesh(hull.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    for f in bm.faces: f.normal_flip(); f.smooth = True
    bm.to_mesh(hull.data); bm.free()
    hull.data.materials.clear(); hull.data.materials.append(ink_material())
    d = hull.modifiers.new("push", "DISPLACE"); d.strength = -width; d.mid_level = 0.0
    hull.visible_shadow = False
    return hull


def set_pose(arm, pose, frac=0.0):
    if pose is None:
        if arm.animation_data: arm.animation_data.action = None
        for pb in arm.pose.bones:
            pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.scale = (1, 1, 1)
        return
    names = list(arm.get("acts", []))
    act = next((bpy.data.actions[n] for n in names if n == pose or n.startswith(pose + ".")), None)
    if act is None: raise SystemExit("no action " + pose)
    if not arm.animation_data: arm.animation_data_create()
    arm.animation_data.action = act
    try:
        arm.animation_data.action_slot = act.slots[0]
    except Exception as e:
        print("slot", e)
    first, last = act.frame_range
    f = first + (last - first) * frac
    bpy.context.scene.frame_set(int(f), subframe=f - int(f))


def put(path, x=0.0, rot=0.0, pose="idle", frac=0.0, y=0.0, z=0.0):
    before = set(bpy.data.objects)
    acts = set(bpy.data.actions)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in list(new):
        if o.type == "MESH" and o.name.startswith("Icosphere"):
            bpy.data.objects.remove(o); new.remove(o)
    arm = next(o for o in new if o.type == "ARMATURE")
    arm["acts"] = [a.name for a in bpy.data.actions if a not in acts]
    meshes = [o for o in new if o.type == "MESH"]
    img = None
    for o in meshes:
        for s in o.material_slots:
            for n in s.material.node_tree.nodes:
                if n.type == "TEX_IMAGE" and n.image: img = n.image
    mat = toon_material("toon-" + arm.name, img)
    for o in meshes:
        o.data.materials.clear(); o.data.materials.append(mat)
        add_outline(o)
    set_pose(arm, pose, frac)
    root = bpy.data.objects.new("holder-" + arm.name, None); bpy.context.scene.collection.objects.link(root)
    arm.parent = root
    root.location = (x, y, z); root.rotation_euler.z = math.radians(rot)
    return root, arm


def lights():
    sun = bpy.data.lights.new("sun", "SUN"); sun.energy = 3.14; sun.angle = math.radians(1.0)
    so = bpy.data.objects.new("sun", sun); bpy.context.scene.collection.objects.link(so)
    so.rotation_euler = (math.radians(58), 0, math.radians(-38))


def camera(target, dist, lens=70, elev=8.0, azim=0.0, res=(1280, 720)):
    sc = bpy.context.scene
    cam = bpy.data.cameras.new("cam"); co = bpy.data.objects.new("cam", cam); sc.collection.objects.link(co)
    cam.lens = lens
    t = Vector(target)
    e, a = math.radians(elev), math.radians(azim)
    co.location = t + Vector((math.sin(a) * math.cos(e) * dist, -math.cos(a) * math.cos(e) * dist, math.sin(e) * dist))
    co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = co; sc.render.resolution_x, sc.render.resolution_y = res
    cam.clip_start = 0.01
    return co


def ground(z=0.0, colour=(0.20, 0.205, 0.235)):
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, z))
    g = bpy.context.active_object
    g.data.materials.append(flat_toon("ground", colour))
    return g


def ledge(edge_y, top=0.0, depth=0.42, colour=(0.42, 0.40, 0.36)):
    """A stone ledge whose top is at z `top` and whose front edge is at y `edge_y` (he faces -y)."""
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    b = bpy.context.active_object
    b.scale = (0.9, 0.6, depth)
    b.location = (0.0, edge_y + 0.3, top - depth / 2.0)
    b.data.materials.append(flat_toon("ledge", colour))
    add_outline(b)
    return b


def render(path):
    sc = bpy.context.scene
    sc.render.filepath = path; sc.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    print("WROTE", path)


def want(k):
    return not only or k in only


def scene(floor=0.0):
    reset(); lights(); ground(floor)


if want("row"):
    for tag, turn, az in (("front", 0, 0), ("34", 0, -32), ("back", 180, 0)):
        scene()
        for i, path in enumerate(ROW):
            put(path, -2.45 + 0.70 * i, turn)
        camera((0.0, 0, 0.36), 13.0, lens=82, elev=5, azim=az, res=(2800, 620))
        render(f"{OUT}/row_{tag}.png")
    scene()
    for i, path in enumerate(ROW):
        put(path, -2.03 + 0.58 * i, 0)
    camera((0.0, 0, 0.50), 13.0, lens=102, elev=3, azim=0, res=(2900, 500))
    render(f"{OUT}/row_heads.png")
    # far and small, from above, as a crowd figure is seen from the court
    scene()
    for i, path in enumerate(ROW):
        put(path, -2.45 + 0.70 * i, 0)
    camera((0.0, 0, 0.36), 40.0, lens=60, elev=22, azim=-15, res=(900, 260))
    render(f"{OUT}/row_far.png")

FACE_VIEWS = (("front", 0), ("34", -32))
if want("face"):
    for tag, path, z in (("new", NEW, 0.462), ("dante", DANTE, 0.475), ("cheska", CHESKA, 0.475), ("totoy", TOTOY, 0.470)):
        scene(); put(path)
        for vname, az in FACE_VIEWS:
            camera((0.0, 0.0, z), 6.0, lens=380, elev=3, azim=az, res=(900, 760))
            render(f"{OUT}/face_{tag}_{vname}.png")

if want("turn"):
    for name, rots in (("turn", (0, 35, 90, 180)), ("turn2", (-35, -90, 145, -145))):
        scene()
        for i, rot in enumerate(rots):
            put(NEW, -1.23 + 0.82 * i, rot)
        camera((0, 0, 0.36), 13.0, lens=135, elev=5, azim=0, res=(1920, 780))
        render(f"{OUT}/{name}.png")

# one frame inside each idle stance (fractions of the 8 s), and the key frames of the loops
IDLE = (("stand", 0.0), ("watch", 0.14), ("point", 0.35), ("wave_a", 0.62), ("wave_b", 0.655), ("scratch", 0.89))
LOOPS = (("cheer", (("crouch", 0.0), ("top", 0.34), ("land", 0.60))), ("clap", (("apart", 0.0), ("clap", 0.15))),
         ("sit", (("a", 0.0), ("b", 0.25), ("c", 0.75))))
VIEWS = (("front", -12, 8), ("side", 90, 6), ("back", 160, 10))
if want("clips"):
    scene(); root, arm = put(NEW, pose="idle")
    for tag, fr in IDLE:
        set_pose(arm, "idle", fr)
        for vname, az, el in VIEWS:
            camera((0.0, 0.0, 0.38), 6.0, lens=150, elev=el, azim=az, res=(520, 640))
            render(f"{OUT}/idle_{tag}_{vname}.png")
    for clip, frames in LOOPS:
        views = VIEWS + (("34", -40, 12),)
        if clip == "sit":
            scene(-0.42); ledge(-0.015)
            root, arm = put(NEW, pose="sit")
            target = (0.0, 0.0, 0.12)
            # low cameras: his shins are short (a child on the cast's rig) and hang only 0.03 under
            # the ledge top, so a camera above the ledge top hides them behind its front face
            views = (("front", -12, 1), ("side", 90, 1), ("back", 160, 10), ("34", -40, 4))
        else:
            scene(); root, arm = put(NEW, pose=clip)
            target = (0.0, 0.0, 0.40)
        for tag, fr in frames:
            set_pose(arm, clip, fr)
            for vname, az, el in views:
                camera(target, 6.0, lens=135, elev=el, azim=az, res=(520, 640))
                render(f"{OUT}/{clip}_{tag}_{vname}.png")
    # the locomotion, one frame each
    scene(); root, arm = put(NEW, pose="walk")
    for clip, fr in (("walk", 0.25), ("sprint", 0.25), ("jump", 0.3), ("fall", 0.25)):
        set_pose(arm, clip, fr)
        for vname, az, el in (("front", -12, 8), ("side", 90, 6)):
            camera((0.0, 0.0, 0.40), 6.0, lens=150, elev=el, azim=az, res=(480, 600))
            render(f"{OUT}/loco_{clip}_{vname}.png")


def head_pose(arm, yaw, pitch):
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = rest.inverted() @ world @ rest
    bpy.context.view_layer.update()


if want("look"):
    scene(); root, arm = put(NEW, pose=None)
    set_pose(arm, None)
    for tag, yaw, pitch in (("yaw55", 55, 0), ("yawm55", -55, 0), ("nod22", 0, -22), ("yaw55_nod22", 55, -22)):
        head_pose(arm, yaw, pitch)
        for vname, az, el in (("front", -10, 8), ("back", 170, 12)):
            camera((0.0, 0.0, 0.46), 6.0, lens=190, elev=el, azim=az, res=(640, 640))
            render(f"{OUT}/look_{tag}_{vname}.png")

CLOSE = {
    "head_front": ((0.0, 0.04, 0.50), 6.0, 250, 6, 0),
    "head_left": ((0.0, 0.04, 0.50), 6.0, 250, 6, 80),
    "head_back": ((0.0, 0.1, 0.50), 6.0, 250, 6, 180),
    "head_above": ((0.0, 0.04, 0.52), 6.0, 250, 50, -18),
    "chest": ((0.0, 0.0, 0.25), 6.0, 380, 4, 0),
    "bahag34": ((0.0, 0.0, 0.15), 6.0, 380, 10, -35),
    "back": ((0.0, 0.0, 0.18), 6.0, 300, 4, 180),
    "feet": ((0.0, 0.0, 0.06), 6.0, 380, 14, -25),
}
if want("close"):
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in CLOSE.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/close_{name}.png")
