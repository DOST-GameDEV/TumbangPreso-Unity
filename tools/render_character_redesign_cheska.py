"""Render the review set of the Cheska (displayed: Yasmin) redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_cheska.py -- v01
    ... -- v01 turn face          (only some shots: orig turn face trio far close look clips)
    py -3 tools/sheet_character_redesign_cheska.py v01

Writes single frames into Logs/character-redesign-cheska/<version>/; the sheet script stacks and
labels them into the review sheets beside that folder. A NEW VERSION NAME EVERY ITERATION
(docs/CHARACTER_MODEL_METHOD.md section 5), and LOOK at what comes out before believing it.

A copy of tools/render_character_redesign_dante.py rewritten for this hero (docs/
CHARACTER_REDESIGN_DANTE.md section 13: one hero, one set of files, never another hero's). The
shading is the same approximation, so she and Dante can be judged side by side:
  * the two-band toon ramp of `TumbangPreso/Toon`, a warm lit band and a cool shadow band;
  * the inverted-hull ink edge at `ToonSkin.PersonOutlineWidth` (0.0045 in model space), on a
    welded copy of each mesh, as `OutlineNormals.Weld` does in the game;
  * the palette remap of `Toon.shader` baked into pixels for the ORIGINAL palette model.
It is an APPROXIMATION. Nothing here has been through Unity. It is for judging shape, paint and
silhouette, not final colour.

SHOTS
  orig    the model in the game today, four angles and close views: looked at BEFORE designing
  turn    the redesign: front, three-quarter, side, back
  face    the face straight on and at three-quarter
  facepair  the original's face and the redesign's side by side, front and three-quarter
  trio    the original, the redesign, and the redesigned Dante, in one line
  far     the same line at about 150 px tall, and as silhouettes
  close   hair from behind and above, ears, hands, hems, feet, chest, back, sides
  look    a RIG TEST: the head turned 55 degrees and nodded 22, from the front and the back
  clips   the extreme frames of walk, sprint, jump and fall from the front, side and back
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
BASE_OUT = REPO + "/Logs/character-redesign-cheska"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/cheska/cheska-redesign.glb"
OLD = PERSONS + "/team-cheska.glb"
DANTE = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb"
ASSET = "person_cheska.asset"


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    sc.render.film_transparent = False
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    # a warmer, darker ground than Dante's sheet: her whites and pale ice vanish on a cool grey
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.150, 0.135, 0.150, 1)
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
    return act


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


def ground(z=0.0, colour=(0.215, 0.195, 0.215)):
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
    """The head bone alone, over the idle pose: the game drives it in code, no clip does this."""
    bpy.context.scene.frame_set(0)
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    base = pb.rotation_quaternion.copy()
    arm.animation_data.action = None
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = base @ (rest.inverted() @ world @ rest)
    bpy.context.view_layer.update()


# name -> (target, dist, lens, elev, azim)
CLOSE = {
    "hair_back": ((0.0, 0.1, 0.50), 6.0, 150, 6, 180),
    "hair_above": ((0.0, 0.0, 0.64), 6.0, 170, 40, -18),
    "hair_above_back": ((0.0, 0.0, 0.62), 6.0, 170, 40, 160),
    "ear_left": ((0.19, 0.0, 0.47), 6.0, 520, 4, 70),
    "ear_right": ((-0.19, 0.0, 0.47), 6.0, 520, 4, -70),
    "hand_left_front": ((0.22, 0.0, 0.17), 6.0, 520, 8, 10),
    "hand_left_back": ((0.22, 0.0, 0.17), 6.0, 520, 8, 170),
    "hand_right_front": ((-0.22, 0.0, 0.17), 6.0, 520, 8, -10),
    "hand_right_back": ((-0.22, 0.0, 0.17), 6.0, 520, 8, -170),
    "hem_left": ((0.14, 0.0, 0.17), 6.0, 440, 10, 40),
    "hem_right": ((-0.14, 0.0, 0.17), 6.0, 440, 10, -40),
    "feet": ((0.0, 0.0, 0.07), 6.0, 240, 14, -25),
    "feet_back": ((0.0, 0.0, 0.07), 6.0, 240, 14, 160),
    "chest": ((0.0, 0.0, 0.27), 6.0, 260, 4, 0),
    "back": ((0.0, 0.0, 0.26), 6.0, 220, 4, 180),
    "side_left": ((0.0, 0.0, 0.40), 9.0, 170, 4, 90),
    "side_right": ((0.0, 0.0, 0.40), 9.0, 170, 4, -90),
    "above": ((0.0, 0.0, 0.45), 9.0, 190, 55, -20),
    "below_chin": ((0.0, 0.0, 0.36), 6.0, 300, 1, -25),
}

if want("orig"):
    pal = roster_palette(ASSET)
    scene()
    for i, rot in enumerate((0, 35, 90, 180)):
        put(OLD, -1.05 + 0.70 * i, rot, pal)
    camera((0, 0, 0.40), 13.0, lens=160, elev=5, azim=0, res=(1920, 700))
    render(f"{OUT}/orig_turn.png")
    scene(); put(OLD, 0, 0, pal)
    camera((0.0, 0, 0.52), 6.0, lens=170, elev=2, azim=0, res=(900, 800)); render(f"{OUT}/orig_face.png")
    camera((0.0, 0, 0.52), 6.0, lens=150, elev=4, azim=-32, res=(900, 800)); render(f"{OUT}/orig_face34.png")
    for name in ("hair_back", "hair_above", "hair_above_back", "chest", "back", "feet", "side_left", "side_right", "above",
                 "hand_left_front", "hand_right_front", "hem_left", "below_chin"):
        t, d, lens, el, az = CLOSE[name]
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/orig_{name}.png")

if want("turn"):
    scene()
    for i, rot in enumerate((0, 35, 90, 180)):
        put(NEW, -1.05 + 0.70 * i, rot)
    camera((0, 0, 0.40), 13.0, lens=160, elev=5, azim=0, res=(1920, 700))
    render(f"{OUT}/turn.png")
    scene()
    for i, rot in enumerate((-35, -90, 145, 215)):
        put(NEW, -1.05 + 0.70 * i, rot)
    camera((0, 0, 0.40), 13.0, lens=160, elev=5, azim=0, res=(1920, 700))
    render(f"{OUT}/turn2.png")

if want("face"):
    scene(); put(NEW)
    camera((0.0, 0, 0.52), 6.0, lens=170, elev=2, azim=0, res=(900, 800)); render(f"{OUT}/face.png")
    camera((0.0, 0, 0.52), 6.0, lens=150, elev=4, azim=-32, res=(900, 800)); render(f"{OUT}/face34.png")
    camera((0.0, 0, 0.52), 6.0, lens=150, elev=4, azim=32, res=(900, 800)); render(f"{OUT}/face34_left.png")
    camera((0.0, 0, 0.52), 6.0, lens=150, elev=2, azim=90, res=(900, 800)); render(f"{OUT}/face_side.png")

# the redesign's face BESIDE the original's (rule 12: "as cute, or cuter" is judged here)
if want("facepair"):
    scene(); put(OLD, -0.27, 0, roster_palette(ASSET)); put(NEW, 0.27)
    camera((0.0, 0, 0.51), 9.0, lens=200, elev=2, azim=0, res=(1500, 760)); render(f"{OUT}/facepair.png")
    scene(); put(OLD, -0.27, 28, roster_palette(ASSET)); put(NEW, 0.27, 28)
    camera((0.0, 0, 0.51), 9.0, lens=200, elev=4, azim=0, res=(1500, 760)); render(f"{OUT}/facepair34.png")
    scene(); put(OLD, -0.19, 0, roster_palette(ASSET)); put(NEW, 0.19)
    camera((0.0, 0, 0.47), 9.0, lens=420, elev=1, azim=0, res=(1500, 520)); render(f"{OUT}/facepair_eyes.png")

# her head beside her original's and beside redesigned Dante's: one camera, front and three-quarter
if want("headpair"):
    for tag, rot in (("front", 0), ("34", 28), ("34_left", -28)):
        scene(); put(OLD, -0.52, rot, roster_palette(ASSET)); put(NEW, 0.0, rot)
        if os.path.exists(DANTE): put(DANTE, 0.52, rot)
        camera((0.0, 0, 0.53), 12.0, lens=235, elev=3, azim=0, res=(2000, 760)); render(f"{OUT}/headpair_{tag}.png")

# this version beside the one before it (a copy kept in Logs/character-redesign-cheska/variants/)
PREV = BASE_OUT + "/variants/previous.glb"
if want("prev") and os.path.exists(PREV):
    for tag, rots in (("a", (0, 35)), ("b", (90, 180))):
        scene()
        for i, rot in enumerate(rots):
            put(PREV, -1.05 + 1.40 * i, rot); put(NEW, -0.35 + 1.40 * i, rot)
        camera((0, 0, 0.40), 13.0, lens=160, elev=5, azim=0, res=(1920, 700))
        render(f"{OUT}/prev_{tag}.png")

# the jump's first six frames from the front: do the arms read as UP?
if want("jump6"):
    for i in range(6):
        scene(); root, arm = put(NEW, pose="jump")
        first, last = arm.animation_data.action.frame_range
        bpy.context.scene.frame_set(int(first) + i)
        camera((0.0, 0, 0.42), 6.0, lens=150, elev=8, azim=-12, res=(400, 500))
        render(f"{OUT}/jump6_{i}.png")

# the acted idle at the moments its stances are held: (name, seconds into the clip)
STANCES = (("warm", 1.8), ("hop", 4.0), ("tug_hold", 5.55), ("tug_left", 6.05), ("tug_right", 6.95))
if want("idle"):
    for name, t in STANCES:
        scene(); root, arm = put(NEW, pose="idle")
        f = arm.animation_data.action.frame_range[0] + t * bpy.context.scene.render.fps
        bpy.context.scene.frame_set(int(f), subframe=f - int(f))
        for view, az, el in (("front", -12, 8), ("side", 90, 6), ("back", 148, 10), ("close", 20, 6)):
            if view == "close":
                camera((0.0, 0, 0.36), 6.0, lens=300, elev=el, azim=az, res=(640, 640))
            else:
                camera((0.0, 0, 0.40), 6.0, lens=170, elev=el, azim=az, res=(520, 640))
            render(f"{OUT}/idle_{name}_{view}.png")

if want("trio"):
    scene(); put(OLD, -0.66, 0, roster_palette(ASSET)); put(NEW, 0.0)
    if os.path.exists(DANTE): put(DANTE, 0.66)
    camera((0, 0, 0.40), 13.0, lens=190, elev=5, azim=-22, res=(1800, 900))
    render(f"{OUT}/trio.png")
    camera((0, 0, 0.40), 13.0, lens=190, elev=5, azim=180, res=(1800, 900))
    render(f"{OUT}/trio_back.png")

if want("far"):
    scene(); put(OLD, -0.66, 0, roster_palette(ASSET)); put(NEW, 0.0)
    if os.path.exists(DANTE): put(DANTE, 0.66)
    camera((0, 0, 0.40), 13.0, lens=210, elev=5, azim=-18, res=(640, 300))
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

if want("close"):
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in CLOSE.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/close_{name}.png")

# the head turned as far as the motion test turns it, each from the front and from behind
LOOK = (("yaw55", 55, 0), ("yaw-55", -55, 0), ("nod22", 0, -22), ("up20", 0, 20), ("yaw55_nod22", 55, -22))
if want("look"):
    for tag, yaw, pitch in LOOK:
        scene(); root, arm = put(NEW)
        turn_head(arm, yaw, pitch)
        for view, az, el in (("front", -14, 6), ("back", 166, 8), ("side", 90, 4)):
            camera((0.0, 0.02, 0.44), 6.0, lens=190, elev=el, azim=az, res=(640, 640))
            render(f"{OUT}/look_{tag}_{view}.png")

# each new clip at its two or three extreme frames (fractions of its length)
CLIPS = {"walk": (0.25, 0.5, 0.75), "sprint": (0.25, 0.5, 0.75), "jump": (0.06, 0.3, 0.95), "fall": (0.25, 0.75)}
if want("clips"):
    for clip, fracs in CLIPS.items():
        for frac in fracs:
            scene(); root, arm = put(NEW, pose=clip)
            first, last = arm.animation_data.action.frame_range
            f = first + (last - first) * frac
            bpy.context.scene.frame_set(int(f), subframe=f - int(f))
            for view, az, el in (("front", -12, 8), ("side", 90, 6), ("back", 148, 10)):
                camera((0.0, 0, 0.42), 6.0, lens=150, elev=el, azim=az, res=(400, 500))
                render(f"{OUT}/clip_{clip}_{int(frac * 100):02d}_{view}.png")
