"""Render the review set of the Payo audience villager `woman` (v2), in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_payo_audience_v2_woman.py -- v01
    ... -- v01 row face        (only some shots: row face turn close look clips far)
    py -3 tools/sheet_payo_audience_v2_woman.py v01     (the labelled sheets, renders/v01_*.png)

Writes single frames into ArtSource/payo/audience/v2/woman/renders/<version>/; the sheet script
stacks and labels them. A NEW VERSION NAME EVERY ITERATION, and LOOK at what comes out.

A copy of tools/render_character_redesign_inday.py (her base) cut to what the audience brief asks
for. The game's look as near as EEVEE allows: the two-band toon ramp of `TumbangPreso/Toon`, the
inverted-hull ink edge at 0.0045, flat emission. AN APPROXIMATION, for shape, paint and silhouette.

Shots:
  row     THE TEST (docs/reports/character-redesign/payo-audience-brief.md): her in one row with
          three redesigned heroes (Rafi, Dante, Cheska) and three approved Classics (Bayan, Bebang,
          Lola Pacing), with INDAY, HER BASE CLASSIC, RIGHT BESIDE HER so the finish of the atlas can
          be compared directly (owner, on the v1 crowd: "they look noisy"); front, three-quarter, back
  face    her face beside Cheska's and Dante's, front and three-quarter
  turn    a turnaround, 0 35 90 180 and -35 -90 145 -145
  close   the pieces close
  look    the head turned 55 each way and nodded 22, front and back
  clips   frames of idle, cheer, clap and sit (the sit on a ledge block whose top is z 0 at the
          root's origin), front, three-quarter and side
  far     the row small, at crowd distance, and as a silhouette
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
OUT = REPO + "/ArtSource/payo/audience/v2/woman/renders/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/ArtSource/payo/audience/v2/woman/audience_v2_woman.glb"
CAST = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign"
RAFI = CAST + "/rafi/rafi-redesign.glb"
DANTE = CAST + "/dante/dante-redesign.glb"
CHESKA = CAST + "/cheska/cheska-redesign.glb"
INDAY = CAST + "/inday/inday-redesign.glb"
BAYAN = CAST + "/bayan/bayan-redesign.glb"
BEBANG = CAST + "/bebang/bebang-redesign.glb"
LOLA = CAST + "/lola_pacing/lola_pacing-redesign.glb"
ROW = (RAFI, DANTE, CHESKA, NEW, INDAY, BAYAN, BEBANG, LOLA)


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


def flat_material(name, colour):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse")
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.10; ramp.color_ramp.elements[0].color = (colour[0] * 0.62, colour[1] * 0.62, colour[2] * 0.70, 1)
    ramp.color_ramp.elements[1].position = 0.14; ramp.color_ramp.elements[1].color = (*colour, 1)
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


def set_pose(arm, pose, frame=0.0):
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
    bpy.context.scene.frame_set(int(frame), subframe=frame - int(frame))


def put(path, x=0.0, rot=0.0, pose="idle", frac=0.0, y=0.0):
    """One character, posed at `frac` of the clip `pose` (None is the rest pose)."""
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
    set_pose(arm, pose)
    if pose is not None:
        first, last = arm.animation_data.action.frame_range
        f = first + (last - first) * frac
        bpy.context.scene.frame_set(int(f), subframe=f - int(f))
    root = bpy.data.objects.new("holder-" + arm.name, None); bpy.context.scene.collection.objects.link(root)
    arm.parent = root
    root.location = (x, y, 0.0); root.rotation_euler.z = math.radians(rot)
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
    g.data.materials.append(flat_material("ground", colour))
    return g


def ledge(top=0.0, front=-0.020, colour=(0.50, 0.48, 0.44)):
    """A stone ledge: its top at `top`, its front edge at y `front` (ahead of her is -y)."""
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    b = bpy.context.active_object
    b.scale = (1.0, 0.40, 0.45)
    b.location = (0.0, front + 0.20, top - 0.225)
    b.data.materials.append(flat_material("stone", colour))
    return b


def render(path):
    sc = bpy.context.scene
    sc.render.filepath = path; sc.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    print("WROTE", path)


def want(k):
    return not only or k in only


def scene():
    reset(); lights(); ground()


if want("row"):
    for tag, turn in (("front", 0), ("34", -32), ("back", 180)):
        scene()
        for i, path in enumerate(ROW):
            put(path, -2.66 + 0.76 * i, turn)
        camera((0.0, 0, 0.38), 16.0, lens=92, elev=5, res=(3400, 760))
        render(f"{OUT}/row_{tag}.png")
    scene()
    for i, path in enumerate(ROW):
        put(path, -2.03 + 0.58 * i, 0)
    camera((0.0, 0, 0.52), 16.0, lens=128, elev=3, res=(3400, 560))
    render(f"{OUT}/row_heads.png")

if want("face"):
    for tag, path, z in (("new", NEW, 0.470), ("cheska", CHESKA, 0.475), ("dante", DANTE, 0.475), ("inday", INDAY, 0.470)):
        scene(); put(path)
        for vname, az in (("front", 0), ("right34", -32)):
            camera((0.0, 0.0, z), 6.0, lens=380, elev=3, azim=az, res=(900, 820))
            render(f"{OUT}/faces_{tag}_{vname}.png")

if want("turn"):
    for tag, rots in (("turn", (0, 35, 90, 180)), ("turn2", (-35, -90, 145, -145))):
        scene()
        for i, rot in enumerate(rots):
            put(NEW, -1.5 * 0.82 + 0.82 * i, rot)
        camera((0, 0, 0.40), 13.0, lens=135, elev=5, res=(1920, 780))
        render(f"{OUT}/{tag}.png")

CLOSE = {
    "head_front": ((0.0, 0.0, 0.55), 6.0, 230, 6, 0), "head_34": ((0.0, 0.0, 0.55), 6.0, 230, 8, -35),
    "head_back": ((0.0, 0.05, 0.55), 6.0, 230, 10, 180), "head_above": ((0.0, 0.03, 0.58), 6.0, 230, 50, -18),
    "chest": ((0.0, 0.0, 0.28), 6.0, 400, 5, 0), "chest34": ((0.0, 0.0, 0.28), 6.0, 400, 8, -35),
    "tapis": ((0.0, 0.0, 0.15), 6.0, 380, 6, 0), "tapis34": ((0.0, 0.0, 0.15), 6.0, 380, 8, 35),
    "back": ((0.0, 0.0, 0.22), 6.0, 300, 5, 180), "feet": ((0.0, 0.0, 0.06), 6.0, 380, 18, -25),
    "side_left": ((0.0, 0.0, 0.40), 9.0, 190, 4, 90), "side_right": ((0.0, 0.0, 0.40), 9.0, 190, 4, -90),
}
if want("close"):
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in CLOSE.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/close_{name}.png")


def head_pose(arm, yaw, pitch):
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = Quaternion(arm["head_base"]) @ (rest.inverted() @ world @ rest)
    bpy.context.view_layer.update()


if want("look"):
    scene(); root, arm = put(NEW)
    arm.animation_data.action = None
    arm["head_base"] = list(arm.pose.bones["head"].rotation_quaternion)
    for tag, yaw, pitch in (("yaw55", 55, 0), ("yawm55", -55, 0), ("nod22", 0, -22), ("yaw55_nod22", 55, -22)):
        head_pose(arm, yaw, pitch)
        for vname, az, el in (("front", -10, 8), ("back", 170, 12)):
            camera((0.0, 0.0, 0.48), 6.0, lens=160, elev=el, azim=az, res=(640, 640))
            render(f"{OUT}/look_{tag}_{vname}.png")

#   (clip, fraction of its length, label) for the clip stills
CLIP_FRAMES = [("idle", 0.0, "stand"), ("idle", 0.219, "shout"), ("idle", 0.306, "shout, the third yell"),
               ("idle", 0.506, "nudge, the jab"), ("idle", 0.79, "clasp, bounce"), ("idle", 0.894, "the pump"),
               ("cheer", 0.0, "cheer, landing"), ("cheer", 0.42, "cheer, the top of the hop"),
               ("clap", 0.0, "clap, open"), ("clap", 0.13, "clap, hands meet"),
               ("sit", 0.0, "sit, 0 s"), ("sit", 0.25, "sit, 0.5 s")]
if want("clips"):
    for clip, frac, _label in CLIP_FRAMES:
        seated = clip == "sit"
        reset(); lights(); ground(z=-0.45 if seated else 0.0)
        if seated:
            ledge()
        put(NEW, pose=clip, frac=frac)
        tag = "%s_%03d" % (clip, int(round(frac * 1000)))
        for vname, az, el in (("front", -8, 6), ("34", -40, 10), ("side", -90, 4)):
            camera((0.0, 0.0, 0.26 if seated else 0.40), 6.0, lens=150, elev=el, azim=az, res=(560, 680))
            render(f"{OUT}/clip_{tag}_{vname}.png")

if want("far"):
    scene()
    for i, path in enumerate(ROW):
        put(path, -2.66 + 0.76 * i, 0)
    camera((0.0, 0, 0.38), 40.0, lens=92, elev=12, res=(1200, 280))
    render(f"{OUT}/far.png")
    for o in bpy.data.objects:
        if o.type == "MESH" and o.name != "Plane":
            o.data.materials.clear(); o.data.materials.append(ink_material())
            for m in o.modifiers:
                if m.type == "DISPLACE": m.strength = 0
    ink_material().use_backface_culling = False
    bpy.data.objects["Plane"].hide_render = True
    bpy.context.scene.world.node_tree.nodes["Background"].inputs[0].default_value = (1, 1, 1, 1)
    render(f"{OUT}/far_sil.png")
