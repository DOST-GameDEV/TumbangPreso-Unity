"""Render the review set of the Nemu redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_nemu.py -- v01
    ... -- v01 turn face          (only some shots: orig turn face close trio far sil look ext)
    py -3 tools/sheet_character_redesign_nemu.py v01

Writes single frames into Logs/character-redesign-nemu/<version>/; the sheet script stacks and
labels them into the review sheets beside that folder. A NEW VERSION NAME EVERY ITERATION
(docs/CHARACTER_MODEL_METHOD.md section 5), and LOOK at what comes out before believing it.

It draws nemu-redesign.glb (tools/author_character_redesign_nemu.py) beside the original
team-nemu.glb and beside the redesigned Dante, all in the rig's `idle` pose, the way the game
draws them as near as EEVEE allows: the two-band toon ramp of `TumbangPreso/Toon`, the
inverted-hull ink edge at `ToonSkin.PersonOutlineWidth` (0.0045 in model space) on a welded copy
of each mesh, and the palette remap of `Toon.shader` baked into pixels for the palette model.
IT IS AN APPROXIMATION. Nothing here has been through Unity. It is for judging shape, paint and
silhouette, not final colour.

WHY NEMU HAS SHOTS DANTE DOES NOT. docs/CAST_CLOTHING_STYLE.md: a rework once lowered her cowl,
lifted her fringe and grew her head, and the owner said "u ruined nemuu". So this script can
prove the opposite: `sil` renders the original and the redesign as flat silhouettes from the
front, the side and the back through one orthographic camera, and the sheet script lays one
over the other so every pixel that moved is seen. `look` turns the head bone 55 degrees and nods
it 22, close on the cowl, from the front and the back. `ext` holds each new clip at its extremes.
"""
import math
import os
import re
import sys

import bpy
import bmesh
import numpy as np
from mathutils import Quaternion, Vector

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
PERSONS = REPO + "/Assets/TumbangPreso/Art/characters/persons"
OUTLINE = 0.0045  # ToonSkin.PersonOutlineWidth in model space
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["v0"]
V = args[0]
only = set(args[1:])
BASE_OUT = REPO + "/Logs/character-redesign-nemu"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/nemu/nemu-redesign.glb"
OLD = PERSONS + "/team-nemu.glb"
DANTE = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb"


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    sc.render.film_transparent = False
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.36, 0.37, 0.40, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    return sc


def roster_palette(asset):
    txt = open(REPO + "/Assets/TumbangPreso/Resources/Roster/" + asset).read()
    blk = txt.split("Palette:")[1]
    cols = re.findall(r"\{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}", blk)[:16]
    return [tuple(float(x) for x in c) for c in cols]


def palette_image(img, palette, name):
    """Toon.shader row test baked into pixels: Unity rows 0..7 take _Palette, the rest the atlas."""
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    cw, ch = w // 16, h // 16
    for row in range(8):
        for col in range(16):
            slot = col // 2 + (8 if row <= 3 else 0)
            r, g, b, a = palette[slot]
            px[row * ch:(row + 1) * ch, col * cw:(col + 1) * cw, :3] = [r, g, b]
    out = bpy.data.images.new(name, w, h)
    out.pixels = px.ravel(); out.pack()
    return out


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


def import_character(path, palette=None, pose="idle", frame=0.0, toon=True):
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
    if palette is not None:
        img = palette_image(img, palette, "pal-" + arm.name)
    mat = toon_material("toon-" + arm.name, img)
    for o in meshes:
        o.data.materials.clear(); o.data.materials.append(mat)
        if toon: add_outline(o)
    set_pose(arm, pose, frame)
    root = bpy.data.objects.new("holder-" + arm.name, None); bpy.context.scene.collection.objects.link(root)
    arm.parent = root
    return root, arm, meshes


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


def lights():
    sun = bpy.data.lights.new("sun", "SUN"); sun.energy = 3.14; sun.angle = math.radians(1.0)
    so = bpy.data.objects.new("sun", sun); bpy.context.scene.collection.objects.link(so)
    so.rotation_euler = (math.radians(58), 0, math.radians(-38))
    return so


def camera(target, dist, lens=70, elev=8.0, azim=0.0, res=(1280, 720), ortho=None):
    sc = bpy.context.scene
    cam = bpy.data.cameras.new("cam"); co = bpy.data.objects.new("cam", cam); sc.collection.objects.link(co)
    cam.lens = lens
    if ortho: cam.type = "ORTHO"; cam.ortho_scale = ortho
    t = Vector(target)
    e, a = math.radians(elev), math.radians(azim)
    co.location = t + Vector((math.sin(a) * math.cos(e) * dist, -math.cos(a) * math.cos(e) * dist, math.sin(e) * dist))
    co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = co; sc.render.resolution_x, sc.render.resolution_y = res
    cam.clip_start = 0.01
    return co


def ground(z=0.0, colour=(0.46, 0.47, 0.50)):
    # ⚠️ PALER THAN DANTE'S. She is midnight violet from hood to hem; on his near-black floor
    # and sky her whole outline vanished and nothing about her shape could be judged.
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, z))
    g = bpy.context.active_object
    m = bpy.data.materials.new("ground"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); dif.inputs[0].default_value = (1, 1, 1, 1)
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.10; ramp.color_ramp.elements[0].color = (colour[0]*0.6, colour[1]*0.6, colour[2]*0.7, 1)
    ramp.color_ramp.elements[1].position = 0.16; ramp.color_ramp.elements[1].color = (*colour, 1)
    em = nt.nodes.new("ShaderNodeEmission"); out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs[0], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
    g.data.materials.append(m)
    return g


def render(path):
    sc = bpy.context.scene
    sc.render.filepath = path; sc.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    print("WROTE", path)


def want(k):
    return not only or k in only


def scene():
    reset(); lights(); ground()


def put(path, x=0.0, rot=0.0, palette=None, pose="idle", frame=0.0):
    root, arm, _ = import_character(path, palette, pose=pose, frame=frame)
    root.location.x = x; root.rotation_euler.z = math.radians(rot)
    return root, arm


def nemu_palette():
    return roster_palette("person_nemu.asset")


def turn_head(arm, yaw, pitch):
    """The motion script's rig test: the head bone alone, turned and nodded over the idle pose."""
    bpy.context.scene.frame_set(0)
    arm.animation_data.action = None
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = rest.inverted() @ world @ rest
    bpy.context.view_layer.update()


# name -> (target, dist, lens, elev, azim)
CLOSE = {
    "face": ((0.0, 0, 0.40), 6.0, 260, 2, 0),
    "face34": ((0.0, 0, 0.40), 6.0, 240, 4, -32),
    "face34_l": ((0.0, 0, 0.40), 6.0, 240, 4, 32),
    "hair_back": ((0.0, 0.05, 0.40), 6.0, 240, 6, 180),
    "hair_above": ((0.0, 0.0, 0.50), 6.0, 240, 42, -18),
    "hair_above_back": ((0.0, 0.0, 0.50), 6.0, 240, 42, 160),
    "ear_left": ((0.13, 0.0, 0.34), 6.0, 560, 4, 70),
    "ear_right": ((-0.13, 0.0, 0.34), 6.0, 560, 4, -70),
    "hand_left": ((0.17, 0.0, 0.11), 6.0, 440, 8, 20),
    "hand_right": ((-0.17, 0.0, 0.11), 6.0, 440, 8, -20),
    "hand_left_back": ((0.17, 0.0, 0.11), 6.0, 440, 8, 160),
    "hem": ((0.0, 0.0, 0.13), 6.0, 300, 8, -25),
    "hem_back": ((0.0, 0.0, 0.13), 6.0, 300, 8, 160),
    "feet": ((0.0, 0.0, 0.05), 6.0, 300, 16, -25),
    "chest": ((0.0, 0.0, 0.22), 6.0, 300, 4, 0),
    "back": ((0.0, 0.0, 0.22), 6.0, 300, 4, 180),
    "cowl_side": ((0.0, 0.0, 0.33), 6.0, 300, 4, 90),
    "cowl_under": ((0.0, 0.0, 0.27), 6.0, 420, -1.5, -24),
    "side_left": ((0.0, 0.0, 0.30), 9.0, 420, 4, 90),
    "side_right": ((0.0, 0.0, 0.30), 9.0, 420, 4, -90),
    "above": ((0.0, 0.0, 0.32), 9.0, 440, 55, -20),
}

if want("orig"):
    scene()
    for i, rot in enumerate((0, 35, 90, 180)):
        put(OLD, -0.90 + 0.60 * i, rot, nemu_palette())
    camera((0, 0, 0.31), 13.0, lens=200, elev=5, azim=0, res=(1920, 700))
    render(f"{OUT}/orig_turn.png")
    scene(); put(OLD, 0, 0, nemu_palette())
    for name in ("face", "face34", "hair_back", "hair_above", "hair_above_back", "hand_left", "hem", "feet", "chest", "back",
                 "cowl_side", "side_left", "above", "ear_left"):
        t, d, lens, el, az = CLOSE[name]
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/orig_{name}.png")

if want("turn"):
    scene()
    for i, rot in enumerate((0, 35, 90, 180)):
        put(NEW, -0.90 + 0.60 * i, rot)
    camera((0, 0, 0.31), 13.0, lens=200, elev=5, azim=0, res=(1920, 700))
    render(f"{OUT}/turn.png")
    scene()
    for i, rot in enumerate((-35, -90, 215, 145)):
        put(NEW, -0.90 + 0.60 * i, rot)
    camera((0, 0, 0.31), 13.0, lens=200, elev=5, azim=0, res=(1920, 700))
    render(f"{OUT}/turn2.png")

if want("face"):
    scene(); put(OLD, -0.19, 0, nemu_palette()); put(NEW, 0.19, 0)
    camera((0.0, 0, 0.42), 9.0, lens=330, elev=2, azim=0, res=(1500, 800))
    render(f"{OUT}/face_pair.png")
    # her head is 0.84 of the original's now: the same pair with the ORIGINAL shrunk to 0.84
    # about its own head joint height, so the two faces can be laid side by side at one size
    scene(); root, _ = put(OLD, -0.16, 0, nemu_palette()); put(NEW, 0.16, 0)
    root.scale = (0.84, 0.84, 0.84); root.location.z = 0.28 * (1.0 - 0.84)
    camera((0.0, 0, 0.40), 9.0, lens=390, elev=2, azim=0, res=(1500, 800))
    render(f"{OUT}/face_pair_same_size.png")
    scene(); root, _ = put(OLD, -0.16, 32, nemu_palette()); put(NEW, 0.16, 32)
    root.scale = (0.84, 0.84, 0.84); root.location.z = 0.28 * (1.0 - 0.84)
    camera((0.0, 0, 0.40), 9.0, lens=390, elev=4, azim=0, res=(1500, 800))
    render(f"{OUT}/face34_pair_same_size.png")
    scene(); put(OLD, -0.19, 32, nemu_palette()); put(NEW, 0.19, 32)
    camera((0.0, 0, 0.42), 9.0, lens=330, elev=4, azim=0, res=(1500, 800))
    render(f"{OUT}/face34_pair.png")
    scene(); put(OLD, -0.19, 90, nemu_palette()); put(NEW, 0.19, 90)
    camera((0.0, 0, 0.42), 9.0, lens=330, elev=2, azim=0, res=(1500, 800))
    render(f"{OUT}/faceside_pair.png")

if want("close"):
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in CLOSE.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/close_{name}.png")

if want("trio"):
    scene(); put(OLD, -0.58, 0, nemu_palette()); put(NEW, 0.0); put(DANTE, 0.68)
    camera((0.05, 0, 0.36), 13.0, lens=190, elev=5, azim=-20, res=(1800, 900))
    render(f"{OUT}/trio.png")
    scene(); put(OLD, -0.58, 180, nemu_palette()); put(NEW, 0.0, 180); put(DANTE, 0.68, 180)
    camera((0.05, 0, 0.36), 13.0, lens=190, elev=5, azim=-20, res=(1800, 900))
    render(f"{OUT}/trio_back.png")

if want("far"):
    scene(); put(OLD, -0.5, 0, nemu_palette()); put(NEW, 0.0); put(DANTE, 0.56)
    camera((0, 0, 0.36), 13.0, lens=210, elev=5, azim=-18, res=(640, 300))
    render(f"{OUT}/far.png")

if want("sil"):
    # one orthographic camera, each model alone, flat black on white, in the REST pose (arms out)
    # and in idle: the sheet script lays the original over the redesign
    for tag, path, pal in (("old", OLD, nemu_palette()), ("new", NEW, None)):
        for pose in (None, "idle"):
            reset()
            root, arm, meshes = import_character(path, pal, pose="idle", toon=False)
            if pose is None:
                set_pose(arm, None)
            ink_material().use_backface_culling = False
            for o in meshes:
                o.data.materials.clear(); o.data.materials.append(ink_material())
            bpy.context.scene.world.node_tree.nodes["Background"].inputs[0].default_value = (1, 1, 1, 1)
            for view, az in (("front", 0), ("side", 90), ("back", 180)):
                camera((0, 0, 0.31), 6.0, elev=0, azim=az, res=(900, 900), ortho=0.72)
                render(f"{OUT}/sil_{tag}_{'rest' if pose is None else 'idle'}_{view}.png")

if want("look"):
    for tag, yaw, pitch in (("yaw55", 55, 0), ("yawm55", -55, 0), ("nod22", 0, -22), ("up20", 0, 20), ("yaw55nod", 55, -22)):
        scene(); root, arm = put(NEW)
        turn_head(arm, yaw, pitch)
        for view, az, el in (("front", -12, 6), ("side", 90, 4), ("back", 168, 8), ("above", 20, 50)):
            camera((0.0, 0, 0.36), 6.0, lens=250, elev=el, azim=az, res=(700, 700))
            render(f"{OUT}/look_{tag}_{view}.png")

# clip -> the frames worth holding. ⚠️ SCENE FRAMES, 24 A SECOND (the importer's default), not
# the clips' 60: idle 60 is 2.5 s in (asleep), 70 the jolt, 79 the look round, 118 the yawn,
# 150 and 161 the two ends of the sleeve swish.
EXTREMES = {"idle": (0, 60, 70, 79, 118, 150, 161), "walk": (0, 4, 9, 13), "sprint": (0, 3, 6, 9), "jump": (0, 1, 3, 6, 11),
            "fall": (0, 2, 4, 6)}
if want("ext"):
    for clip, frames in EXTREMES.items():
        for f in frames:
            scene(); root, arm = put(NEW, pose=clip, frame=f)
            for view, az, el in (("front", -12, 8), ("side", 90, 6), ("back", 148, 10)):
                camera((0.0, 0, 0.33), 6.0, lens=270, elev=el, azim=az, res=(460, 520))
                render(f"{OUT}/ext_{clip}_{f:03d}_{view}.png")
