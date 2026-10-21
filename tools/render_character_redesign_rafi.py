"""Render the review set of the Rafi (displayed: Ilyas) redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_rafi.py -- v03
    ... -- v03 turn face          (only some shots: orig turn face close trio look extremes far)
    py -3 tools/sheet_character_redesign_rafi.py v03

Writes single frames into Logs/character-redesign-rafi/<version>/; the sheet script stacks and
labels them into the review sheets beside that folder. A NEW VERSION NAME EVERY ITERATION
(docs/CHARACTER_MODEL_METHOD.md section 5), and LOOK at what comes out before believing it.

A copy of tools/render_character_redesign_dante.py rewritten for this hero (docs/
CHARACTER_REDESIGN_DANTE.md section 13: every hero works in its own files). It draws
rafi-redesign.glb beside the original team-rafi.glb and beside the redesigned Dante, the way the
game draws them as near as EEVEE allows:
  * the two-band toon ramp of `TumbangPreso/Toon`, a warm lit band and a cool shadow band;
  * the inverted-hull ink edge at `ToonSkin.PersonOutlineWidth` (0.0045 in model space);
  * the palette remap of `Toon.shader` baked into pixels for the palette model (the original).
IT IS AN APPROXIMATION. Nothing here has been through Unity. It is for judging shape, paint,
silhouette and clipping, not final colour.

Shots that Dante's script does not have, because this hero needs them:
  orig      the ORIGINAL alone, five angles, to study before designing anything
  look      the head bone turned 55 degrees each way and nodded 22, from the front AND the back
            (rule 6), close on the neck: his tied tail and the putong's tails hang behind it
  extremes  chosen frames of walk, sprint, jump and fall from the front, the side and the back,
            for limbs or hair passing through cloth
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
BASE_OUT = REPO + "/Logs/character-redesign-rafi"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/rafi/rafi-redesign.glb"
OLD = PERSONS + "/team-rafi.glb"
DANTE = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb"
OLD_PALETTE = "person_rafi.asset"


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


def import_character(path, palette=None, pose="idle", frame=0.0):
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
        add_outline(o)
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


def turn_head(arm, yaw, pitch):
    """The motion script's rig test as a still: the head bone alone, over the idle pose."""
    bpy.context.scene.frame_set(0)
    arm.animation_data.action = None
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = rest.inverted() @ world @ rest
    bpy.context.view_layer.update()


ANGLES = (0, 35, 90, 180, 215)

if want("orig"):
    scene()
    for i, rot in enumerate(ANGLES):
        put(OLD, -1.40 + 0.70 * i, rot, roster_palette(OLD_PALETTE))
    camera((0, 0, 0.40), 13.0, lens=132, elev=5, azim=0, res=(2000, 640))
    render(f"{OUT}/orig_turn.png")
    scene(); put(OLD, 0, 0, roster_palette(OLD_PALETTE))
    camera((0.0, 0, 0.53), 6.0, lens=170, elev=2, azim=0, res=(900, 800)); render(f"{OUT}/orig_face.png")
    camera((0.0, 0, 0.24), 6.0, lens=230, elev=4, azim=0, res=(900, 800)); render(f"{OUT}/orig_chest.png")
    camera((0.0, 0, 0.24), 6.0, lens=230, elev=4, azim=180, res=(900, 800)); render(f"{OUT}/orig_back.png")
    camera((0.0, 0, 0.40), 9.0, lens=170, elev=4, azim=90, res=(900, 800)); render(f"{OUT}/orig_side.png")
    camera((0.0, 0, 0.45), 9.0, lens=190, elev=55, azim=-20, res=(900, 800)); render(f"{OUT}/orig_above.png")
    camera((0.0, 0, 0.09), 6.0, lens=230, elev=14, azim=-25, res=(900, 800)); render(f"{OUT}/orig_legs.png")
    camera((0.0, 0.1, 0.50), 6.0, lens=170, elev=6, azim=180, res=(900, 800)); render(f"{OUT}/orig_hair_back.png")

if want("turn"):
    scene()
    for i, rot in enumerate(ANGLES):
        put(NEW, -1.40 + 0.70 * i, rot)
    camera((0, 0, 0.40), 13.0, lens=132, elev=5, azim=0, res=(2000, 640))
    render(f"{OUT}/turn.png")

if want("face"):
    scene(); put(NEW)
    camera((0.0, 0, 0.53), 6.0, lens=180, elev=2, azim=0, res=(900, 800))
    render(f"{OUT}/face.png")
    camera((0.0, 0, 0.53), 6.0, lens=170, elev=4, azim=-32, res=(900, 800))
    render(f"{OUT}/face34.png")
    camera((0.0, 0, 0.53), 6.0, lens=170, elev=4, azim=32, res=(900, 800))
    render(f"{OUT}/face34b.png")

if want("facecmp"):
    # rule 12: the face judged BESIDE the original's, front and three-quarter
    for tag, rot in (("front", 0), ("34", 32)):
        scene(); put(OLD, -0.30, rot, roster_palette(OLD_PALETTE)); put(NEW, 0.30, rot)
        camera((0.0, 0, 0.53), 9.0, lens=180, elev=3, azim=0, res=(1500, 760))
        render(f"{OUT}/facecmp_{tag}.png")

if want("headfam"):
    # the head beside his original and beside redesigned Dante, whose head shape he now shares:
    # front and both three-quarters, one camera
    for tag, rot in (("front", 0), ("34r", 32), ("34l", -32)):
        scene(); put(OLD, -0.62, rot, roster_palette(OLD_PALETTE)); put(NEW, 0.0, rot); put(DANTE, 0.62, rot)
        camera((0.0, 0, 0.53), 11.0, lens=180, elev=3, azim=0, res=(1800, 640))
        render(f"{OUT}/headfam_{tag}.png")

PREV = BASE_OUT + "/prev_full_head/rafi-redesign.glb"   # a copy of the last build before the head was scaled
if want("prev") and os.path.exists(PREV):
    scene()
    for i, rot in enumerate((0, 35, 180)):
        put(PREV, -1.75 + 1.40 * i, rot); put(NEW, -1.05 + 1.40 * i, rot)
    camera((0, 0, 0.40), 13.0, lens=120, elev=5, azim=0, res=(2200, 620))
    render(f"{OUT}/prev_turn.png")

if want("jumpfront"):
    for f in range(6):
        scene(); put(NEW, pose="jump", frame=f)
        camera((0.0, 0.0, 0.42), 6.0, lens=150, elev=8, azim=-12, res=(420, 520))
        render(f"{OUT}/jumpfront_{f}.png")
    for f in (2, 6):
        scene(); put(NEW, pose="fall", frame=f)
        for view, az in (("front", -12), ("back", 168)):
            camera((0.0, 0.0, 0.42), 6.0, lens=150, elev=8, azim=az, res=(420, 520))
            render(f"{OUT}/fall_{f}_{view}.png")
    scene(); put(NEW, pose="idle", frame=144)
    for view, az in (("front", -12), ("side", -90)):
        camera((0.0, 0.0, 0.42), 6.0, lens=150, elev=8, azim=az, res=(420, 520))
        render(f"{OUT}/tooth_{view}.png")

if want("trio"):
    scene(); put(OLD, -0.66, 0, roster_palette(OLD_PALETTE)); put(NEW, 0.0); put(DANTE, 0.66)
    camera((0, 0, 0.40), 13.0, lens=200, elev=5, azim=-22, res=(1800, 900))
    render(f"{OUT}/trio.png")
    scene(); put(OLD, -0.66, 180, roster_palette(OLD_PALETTE)); put(NEW, 0.0, 180); put(DANTE, 0.66, 180)
    camera((0, 0, 0.40), 13.0, lens=200, elev=5, azim=-22, res=(1800, 900))
    render(f"{OUT}/trio_back.png")

if want("far"):
    scene(); put(OLD, -0.62, 0, roster_palette(OLD_PALETTE)); put(NEW, 0.0); put(DANTE, 0.62)
    camera((0, 0, 0.40), 13.0, lens=210, elev=5, azim=-18, res=(640, 300))
    render(f"{OUT}/far.png")

# name -> (target, dist, lens, elev, azim)
CLOSE = {
    "hair_back": ((0.0, 0.1, 0.50), 6.0, 170, 6, 180),
    "hair_above": ((0.0, 0.0, 0.64), 6.0, 190, 38, -18),
    "hair_above_back": ((0.0, 0.0, 0.64), 6.0, 190, 42, 160),
    "ear_left": ((0.19, 0.0, 0.46), 6.0, 520, 4, 70),
    "ear_right": ((-0.19, 0.0, 0.46), 6.0, 520, 4, -70),
    "hand_left_front": ((0.20, 0.0, 0.16), 6.0, 520, 8, 10),
    "hand_left_back": ((0.20, 0.0, 0.16), 6.0, 520, 8, 170),
    "hand_right_front": ((-0.20, 0.0, 0.16), 6.0, 520, 8, -10),
    "hand_right_back": ((-0.20, 0.0, 0.16), 6.0, 520, 8, -170),
    "hem_front": ((0.0, 0.0, 0.12), 6.0, 330, 8, 0),
    "hem_back": ((0.0, 0.0, 0.14), 6.0, 330, 8, 180),
    "feet": ((0.0, 0.0, 0.06), 6.0, 250, 30, -25),
    "chest": ((0.0, 0.0, 0.26), 6.0, 250, 4, 0),
    "back": ((0.0, 0.0, 0.26), 6.0, 250, 4, 180),
    "legs_front": ((0.0, 0.0, 0.10), 6.0, 430, 6, 0),
    "side_left": ((0.0, 0.0, 0.40), 9.0, 170, 4, 90),
    "side_right": ((0.0, 0.0, 0.40), 9.0, 170, 4, -90),
    "arm_left": ((0.16, 0.0, 0.22), 6.0, 330, 10, 55),
    "arm_right": ((-0.16, 0.0, 0.22), 6.0, 330, 10, -55),
    "above": ((0.0, 0.0, 0.45), 9.0, 190, 55, -20),
}
if want("close"):
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in CLOSE.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/close_{name}.png")
    # the elbow, bent as the walk bends it, to see the arm tattoos across the joint
    scene(); root, arm = put(NEW, pose="sprint", frame=7)
    camera((0.0, 0.0, 0.26), 6.0, lens=250, elev=6, azim=-60, res=(800, 700)); render(f"{OUT}/close_elbow_right.png")
    camera((0.0, 0.0, 0.26), 6.0, lens=250, elev=6, azim=60, res=(800, 700)); render(f"{OUT}/close_elbow_left.png")

LOOKS = (("yaw+55", 55, 0), ("yaw-55", -55, 0), ("nod-22", 0, -22), ("nod+20", 0, 20), ("yaw55_nod22", 55, -22))
if want("look"):
    scene(); root, arm = put(NEW)
    for tag, yaw, pitch in LOOKS:
        turn_head(arm, yaw, pitch)
        for view, az, el in (("front", -10, 6), ("back", 170, 8), ("side", 90, 6)):
            camera((0.0, 0.0, 0.40), 6.0, lens=210, elev=el, azim=az, res=(700, 760))
            render(f"{OUT}/look_{tag}_{view}.png")

#   clip -> frames (at the importer's 24 fps the clips are short: walk about 17 frames, sprint 12)
EXTREMES = {"walk": (4, 13), "sprint": (3, 9), "jump": (2, 11), "fall": (2, 6), "idle": (60, 106, 144, 163)}
if want("extremes"):
    for clip, frames in EXTREMES.items():
        for f in frames:
            scene(); root, arm = put(NEW, pose=clip, frame=f)
            for view, az, el in (("front", -12, 8), ("side", 90, 6), ("back", 168, 10)):
                camera((0.0, 0.0, 0.42), 6.0, lens=150, elev=el, azim=az, res=(520, 620))
                render(f"{OUT}/ext_{clip}_{f:02d}_{view}.png")
