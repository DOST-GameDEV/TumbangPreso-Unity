"""Render the review set of the Payo audience v2 villager `elder_man`, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_payo_audience_v2_elder_man.py -- v01
    ... -- v01 row face        (only some shots: row near face turn clips sit seat look)
    py -3 tools/sheet_payo_audience_v2_elder_man.py v01

Writes single frames into ArtSource/payo/audience/v2/elder_man/renders/<version>/; the sheet script
stacks them into ArtSource/payo/audience/v2/elder_man/renders/<version>_*.png. A NEW VERSION NAME
EVERY ITERATION, and LOOK at what comes out before believing it.

A copy of tools/render_character_redesign_mang_kanor.py rewritten for the elder: the same toon
look (the ramp of `TumbangPreso/Toon`, the inverted-hull ink edge at 0.0045), an approximation of
the game for judging shape, paint and silhouette.

THE TEST (payo-audience-brief.md): him in a row with three redesigned heroes (Rafi, Dante,
Cheska) and the three approved Classics (Bayan, Bebang, Lola Pacing), and (owner, 2026-10-19,
on the noisy v1 textures) HIS BASE CLASSIC, MANG KANOR, RIGHT BESIDE HIM so the finish of the
paint can be compared directly. Front, three-quarter and back.

Shots:
  row     the test row, front, three-quarter, back; and `near`, the three nearest him, larger
  face    his face beside Dante's and Rafi's (two heroes) and Mang Kanor's, front and three-quarter
  turn    his turnaround
  clips   one still each of idle (every stance), cheer, clap and sit; the sit on a 0.2 m ledge block
  seat    prints where the underside of his hips is in the sit clip's frame 0, against the origin
  look    the head bone alone turned 55 and nodded 22 (the Classic rig test)
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
only = set(args[1:])
OUT = REPO + "/ArtSource/payo/audience/v2/elder_man/renders/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/ArtSource/payo/audience/v2/elder_man/audience_v2_elder_man.glb"
CR = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign"
DANTE = CR + "/dante/dante-redesign.glb"
RAFI = CR + "/rafi/rafi-redesign.glb"
CHESKA = CR + "/cheska/cheska-redesign.glb"
BAYAN = CR + "/bayan/bayan-redesign.glb"
BEBANG = CR + "/bebang/bebang-redesign.glb"
LOLA = CR + "/lola_pacing/lola_pacing-redesign.glb"
KANOR = CR + "/mang_kanor/mang_kanor-redesign.glb"


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


def toon_material(name, image):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = image; tex.interpolation = "Linear"
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


def import_character(path, pose="idle", frame=None):
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
    set_pose(arm, pose, frame)
    root = bpy.data.objects.new("holder-" + arm.name, None); bpy.context.scene.collection.objects.link(root)
    arm.parent = root
    return root, arm, meshes


def action(arm, pose):
    return next((bpy.data.actions[n] for n in arm.get("acts", []) if n == pose or n.startswith(pose + ".")), None)


def set_pose(arm, pose, frame=None):
    if pose is None:
        if arm.animation_data: arm.animation_data.action = None
        for pb in arm.pose.bones:
            pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.scale = (1, 1, 1)
        return
    act = action(arm, pose)
    if act is None: raise SystemExit("no action " + pose)
    if not arm.animation_data: arm.animation_data_create()
    arm.animation_data.action = act
    try:
        arm.animation_data.action_slot = act.slots[0]
    except Exception as e:
        print("slot", e)
    if frame is None:
        frame = act.frame_range[0]
    bpy.context.scene.frame_set(int(frame), subframe=frame - int(frame))


def at_fraction(arm, fr):
    a, b = arm.animation_data.action.frame_range
    f = a + (b - a) * fr
    bpy.context.scene.frame_set(int(f), subframe=f - int(f))


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


def flat_material(name, colour):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); dif.inputs[0].default_value = (1, 1, 1, 1)
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.10; ramp.color_ramp.elements[0].color = (colour[0]*0.6, colour[1]*0.6, colour[2]*0.7, 1)
    ramp.color_ramp.elements[1].position = 0.16; ramp.color_ramp.elements[1].color = (*colour, 1)
    em = nt.nodes.new("ShaderNodeEmission"); out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs[0], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
    return m


def ground(z=0.0):
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, z))
    bpy.context.active_object.data.materials.append(flat_material("ground", (0.20, 0.205, 0.235)))


def ledge(x, h=0.2):
    """The bake's ledge (tools/bake_payo_audience_v2.py): 0.42 wide, 0.3 deep, its front edge
    0.02 ahead of the seat."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x, 0.13, h / 2))
    b = bpy.context.object
    b.scale = (0.42, 0.3, h)
    b.data.materials.append(flat_material("ledge", (0.42, 0.40, 0.36)))


def render(path):
    sc = bpy.context.scene
    sc.render.filepath = path; sc.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    print("WROTE", path)


def want(k):
    return not only or k in only


def scene():
    reset(); lights(); ground()


def put(path, x=0.0, rot=0.0, pose="idle", frame=None, z=0.0):
    root, arm, meshes = import_character(path, pose=pose, frame=frame)
    root.location = (x, 0.0, z); root.rotation_euler.z = math.radians(rot)
    return root, arm


ROW = (RAFI, DANTE, BAYAN, KANOR, NEW, LOLA, BEBANG, CHESKA)
ROW_NAMES = "RAFI, DANTE, BAYAN, MANG KANOR (his base), ELDER, LOLA PACING, BEBANG, CHESKA"
if want("row"):
    for tag, rot, az in (("row_front", 0, 0), ("row_34", 0, -28), ("row_back", 180, 0)):
        scene()
        order = ROW if rot == 0 else ROW[::-1]
        for i, path in enumerate(order):
            put(path, 0.70 * (i - 3.5), rot)
        camera((0.0, 0, 0.42), 13.0, lens=78, elev=5, azim=az, res=(2600, 700))
        render(f"{OUT}/{tag}.png")
if want("near"):
    for tag, rot, az in (("near_front", 0, 0), ("near_34", 0, -28), ("near_back", 180, 0)):
        scene()
        order = (KANOR, NEW, LOLA) if rot == 0 else (LOLA, NEW, KANOR)
        for i, path in enumerate(order):
            put(path, 0.74 * (i - 1), rot)
        camera((0.0, 0, 0.42), 13.0, lens=165, elev=5, azim=az, res=(1700, 900))
        render(f"{OUT}/{tag}.png")

if want("face"):
    for tag, path, z in (("new", NEW, 0.462), ("dante", DANTE, 0.475), ("rafi", RAFI, 0.475), ("kanor", KANOR, 0.475)):
        scene(); put(path, 0, 0)
        for vname, az in (("front", 0), ("right34", -32)):
            camera((0.0, 0.0, z), 6.0, lens=380, elev=3, azim=az, res=(900, 800))
            render(f"{OUT}/faces_{tag}_{vname}.png")

if want("turn"):
    scene()
    for i, rot in enumerate((0, 35, 90, 145, 180)):
        put(NEW, 0.85 * (i - 2), rot)
    camera((0, 0, 0.42), 13.0, lens=120, elev=5, azim=0, res=(2200, 820))
    render(f"{OUT}/turn.png")
    scene()
    for i, rot in enumerate((-35, -90, -145)):
        put(NEW, 0.85 * (i - 1), rot)
    camera((0, 0, 0.42), 13.0, lens=150, elev=5, azim=0, res=(1600, 820))
    render(f"{OUT}/turn2.png")

if want("close"):
    scene(); put(NEW)
    for vname, az, el, t in (("front", 0, 6, 0.30), ("34", -35, 8, 0.30), ("left", 70, 6, 0.30), ("back", 180, 8, 0.30),
                             ("head34", -30, 8, 0.50), ("above", -20, 50, 0.45), ("feet", -25, 14, 0.07)):
        camera((0.0, 0.0, t), 6.0, lens=200 if t < 0.4 else 300, elev=el, azim=az, res=(900, 900))
        render(f"{OUT}/close_{vname}.png")

# one frame inside each idle stance, as a fraction of 8 s
IDLE_FRAMES = (("00", 0.0), ("watch", 0.07), ("shade", 0.47), ("thump", 0.68), ("point", 0.88))
CLIP_FRAMES = (("cheer", "cheer", 0.5), ("clap", "clap", 8 / 60.0), ("clap_apart", "clap", 0.42))
if want("clips"):
    for tag, fr in IDLE_FRAMES:
        scene(); root, arm = put(NEW, pose="idle")
        at_fraction(arm, fr)
        for vname, az, el in (("front", -14, 8), ("34", -40, 8), ("side", 90, 5)):
            camera((0.0, 0.0, 0.46), 6.0, lens=128, elev=el, azim=az, res=(640, 760))
            render(f"{OUT}/idle_{tag}_{vname}.png")
    for tag, clip, fr in CLIP_FRAMES:
        scene(); root, arm = put(NEW, pose=clip)
        at_fraction(arm, fr)
        for vname, az, el in (("front", -14, 8), ("34", -40, 8), ("side", 90, 5)):
            camera((0.0, 0.0, 0.50), 6.0, lens=118, elev=el, azim=az, res=(640, 820))
            render(f"{OUT}/{tag}_{vname}.png")
if want("clips") or want("sit"):
    scene()
    ledge(0.0)
    root, arm = put(NEW, pose="sit", z=0.2)
    for vname, az, el in (("front", -14, 8), ("34", -40, 10), ("side", 90, 5), ("back34", 150, 12)):
        camera((0.0, 0.0, 0.46), 6.0, lens=118, elev=el, azim=az, res=(640, 820))
        render(f"{OUT}/sit_{vname}.png")

if want("seat"):
    # where is the underside of his hips in the sit clip's frame 0
    reset()
    root, arm, meshes = import_character(NEW, pose="sit")
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    lo_all, lo_hips, lo_blanket = 9.0, 9.0, 9.0
    for o in meshes:
        if "spear" in o.name:
            continue
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        for v in me.vertices:
            w = o.matrix_world @ v.co
            lo_all = min(lo_all, w.z)
            if abs(w.x) < 0.14 and -0.04 < w.y < 0.10:
                lo_hips = min(lo_hips, w.z)
        ev.to_mesh_clear()
    print("SEAT lowest point of the figure %.4f; lowest under the hips (|x|<0.14, y -0.04..0.10) %.4f" % (lo_all, lo_hips))
    with open(OUT + "/seat.txt", "w") as f:
        f.write("lowest %.4f hips %.4f\n" % (lo_all, lo_hips))


def head_pose(arm, yaw, pitch, base):
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = Quaternion(base) @ (rest.inverted() @ world @ rest)
    bpy.context.view_layer.update()


if want("look"):
    scene(); root, arm = put(NEW)
    base = list(arm.pose.bones["head"].rotation_quaternion)
    arm.animation_data.action = None
    for tag, yaw, pitch in (("yaw55", 55, 0), ("yawm55", -55, 0), ("nod22", 0, -22), ("yaw55_nod22", 55, -22)):
        head_pose(arm, yaw, pitch, base)
        for vname, az, el in (("front", -10, 8), ("back", 170, 12)):
            camera((0.0, 0.0, 0.44), 6.0, lens=210, elev=el, azim=az, res=(640, 640))
            render(f"{OUT}/look_{tag}_{vname}.png")
