"""Render the review set of the Zack (displayed: Isagani) redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_zack.py -- v01
    ... -- v01 turn face          (only some shots: orig turn face facebig close trio far neck idle clips)
    py -3 tools/sheet_character_redesign_zack.py v01

Writes single frames into Logs/character-redesign-zack/<version>/; the sheet script stacks and
labels them into the review sheets beside that folder. A NEW VERSION NAME EVERY ITERATION
(docs/CHARACTER_MODEL_METHOD.md section 5), and LOOK at what comes out before believing it.

A copy of the Dante render script (docs/CHARACTER_REDESIGN_DANTE.md section 13: one set of files a
hero, nothing imported from another hero's), with this hero's own shots:
  orig    the ORIGINAL team-zack.glb alone, turnaround and head close-ups. Rendered and looked at
          before anything was designed, because the builder that made him has drifted from the file.
  turn    the redesign from six sides (he is not symmetric: the dyed lock, the earring and the
          wristband are on his left, the wallet chain on his right)
  face    the face, head on and from both three-quarters
  close   part by part, the views that caught Dante's faults (hair from behind and above, ears,
          hands, hems, feet)
  trio    the original, the redesign, and redesigned Dante, side by side
  far     the same at lineup distance, and as silhouettes
  neck    a RIG TEST: the head bone turned 55 degrees each way and nodded 22, front AND back
  clips   the extreme frames of walk, sprint, jump and fall from the front, the side and behind

The toon look is the Dante script's approximation of `TumbangPreso/Toon`: a two-band ramp, a warm
lit band and a cool shadow band, the inverted-hull ink edge at `ToonSkin.PersonOutlineWidth` on a
welded copy, and the palette remap baked into pixels for the palette models.
IT IS AN APPROXIMATION. Nothing here has been through Unity.
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
BASE_OUT = REPO + "/Logs/character-redesign-zack"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/zack/zack-redesign.glb"
OLD = PERSONS + "/team-zack.glb"
DANTE = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb"
OLD_PALETTE = "person_zack.asset"


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


def old_palette():
    return roster_palette(OLD_PALETTE)


# name -> (target, dist, lens, elev, azim)
HEAD_VIEWS = {
    "head_front": ((0.0, 0.0, 0.56), 6.0, 190, 3, 0),
    "head_34_left": ((0.0, 0.0, 0.56), 6.0, 190, 5, 35),
    "head_34_right": ((0.0, 0.0, 0.56), 6.0, 190, 5, -35),
    "head_side_left": ((0.0, 0.0, 0.56), 6.0, 190, 4, 90),
    "head_side_right": ((0.0, 0.0, 0.56), 6.0, 190, 4, -90),
    "head_back": ((0.0, 0.0, 0.56), 6.0, 190, 6, 180),
    "head_above": ((0.0, 0.0, 0.62), 6.0, 190, 42, -18),
    "head_above_back": ((0.0, 0.0, 0.62), 6.0, 190, 42, 160),
}
BODY_VIEWS = {
    "chest": ((0.0, 0.0, 0.26), 6.0, 240, 4, 0),
    "back": ((0.0, 0.0, 0.25), 6.0, 220, 4, 180),
    "feet": ((0.0, 0.0, 0.07), 6.0, 240, 14, -25),
    "feet_back": ((0.0, 0.0, 0.07), 6.0, 240, 14, 160),
    "side_left": ((0.0, 0.0, 0.40), 9.0, 170, 4, 90),
    "side_right": ((0.0, 0.0, 0.40), 9.0, 170, 4, -90),
    "hand_left_front": ((0.20, 0.0, 0.17), 6.0, 560, 8, 10),
    "hand_left_back": ((0.20, 0.0, 0.17), 6.0, 560, 8, 170),
    "hand_right_front": ((-0.20, 0.0, 0.17), 6.0, 560, 8, -10),
    "hand_right_back": ((-0.20, 0.0, 0.17), 6.0, 560, 8, -170),
    "hem_left": ((0.15, 0.0, 0.18), 6.0, 480, 10, 40),
    "hem_right": ((-0.15, 0.0, 0.18), 6.0, 480, 10, -40),
    "ear_left": ((0.19, 0.0, 0.46), 6.0, 560, 4, 70),
    "ear_right": ((-0.19, 0.0, 0.46), 6.0, 560, 4, -70),
    "above": ((0.0, 0.0, 0.45), 9.0, 190, 55, -20),
    "shoe_left": ((0.09, -0.04, 0.04), 6.0, 620, 12, 50),
    "shoe_right_in": ((-0.07, -0.04, 0.04), 6.0, 620, 12, 30),
    "elbow_left_back": ((0.18, 0.02, 0.20), 6.0, 620, 8, 150),
    "neck_front": ((0.0, 0.0, 0.35), 6.0, 380, 2, 0),
    "neck_back": ((0.0, 0.0, 0.36), 6.0, 380, 6, 180),
}

if want("orig"):
    scene()
    for i, rot in enumerate((0, 35, 90, 180, -90, -35)):
        put(OLD, -1.75 + 0.70 * i, rot, old_palette())
    camera((0, 0, 0.40), 13.0, lens=120, elev=5, azim=0, res=(2400, 660))
    render(f"{OUT}/orig_turn.png")
    scene(); put(OLD, 0.0, 0, old_palette())
    for name, (t, d, lens, el, az) in list(HEAD_VIEWS.items()) + list(BODY_VIEWS.items()):
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/orig_{name}.png")

if want("turn"):
    scene()
    for i, rot in enumerate((0, 35, 90, 180, -90, -35)):
        put(NEW, -1.75 + 0.70 * i, rot)
    camera((0, 0, 0.40), 13.0, lens=120, elev=5, azim=0, res=(2400, 660))
    render(f"{OUT}/turn.png")
    # the same six views of the build before the head came in to 0.84, kept as a copy beside its
    # atlas under Logs/character-redesign-zack/variants/head100/
    PREV = BASE_OUT + "/variants/head100/zack-redesign.glb"
    if os.path.exists(PREV):
        scene()
        for i, rot in enumerate((0, 35, 90, 180, -90, -35)):
            put(PREV, -1.75 + 0.70 * i, rot)
        camera((0, 0, 0.40), 13.0, lens=120, elev=5, azim=0, res=(2400, 660))
        render(f"{OUT}/turn_prev.png")

if want("face"):
    scene(); put(NEW)
    for name in ("head_front", "head_34_left", "head_34_right", "head_side_left", "head_side_right"):
        t, d, lens, el, az = HEAD_VIEWS[name]
        camera(t, d, lens=lens, elev=el, azim=az, res=(900, 800))
        render(f"{OUT}/face_{name}.png")
    scene(); put(OLD, 0.0, 0, old_palette())
    for name, tag in (("head_front", "face_orig"), ("head_34_left", "face_orig_34_left"), ("head_34_right", "face_orig_34_right")):
        t, d, lens, el, az = HEAD_VIEWS[name]
        camera(t, d, lens=lens, elev=el, azim=az, res=(900, 800))
        render(f"{OUT}/{tag}.png")

# the face alone, large, the redesign and the original from the same three cameras (rule 12)
if want("facebig"):
    for tag, path, pal in (("new", NEW, None), ("orig", OLD, old_palette())):
        scene(); put(path, 0.0, 0, pal)
        for view, azim in (("front", 0), ("34_left", 32), ("34_right", -32)):
            camera((0.0, 0.0, 0.50), 6.0, lens=330, elev=3, azim=azim, res=(900, 760))
            render(f"{OUT}/facebig_{tag}_{view}.png")

# the smug candidates (owner: "needs a more smug look"): the original, the measured face and A, B, C,
# each a copy of the .glb beside its own atlas under Logs/character-redesign-zack/variants/<name>/
# (see the sheet script's header for how they are made). Large, and at lineup size.
if "smug" in only:
    VAR = BASE_OUT + "/variants"
    for tag, path, pal in [("orig", OLD, old_palette())] + [(k, f"{VAR}/{k}/zack-redesign.glb", None) for k in ("measured", "A", "B", "C")]:
        scene(); put(path, 0.0, 0, pal)
        for view, azim in (("front", 0), ("34", 32)):
            camera((0.0, 0.0, 0.47), 6.0, lens=300, elev=3, azim=azim, res=(760, 700))
            render(f"{OUT}/smug_{tag}_{view}.png")
        # lineup size: the head about 90 px across, as it is in play
        camera((0.0, 0.0, 0.46), 6.0, lens=440, elev=5, azim=-18, res=(220, 230))
        render(f"{OUT}/smug_{tag}_far.png")

# the head beside Dante's and beside his own original, same camera (the head-shape family check)
if "family" in only:
    for tag, path, pal in (("new", NEW, None), ("dante", DANTE, None), ("orig", OLD, old_palette())):
        scene(); put(path, 0.0, 0, pal)
        for view, azim in (("front", 0), ("34_right", -32), ("34_left", 32)):
            camera((0.0, 0.0, 0.49), 6.0, lens=300, elev=3, azim=azim, res=(760, 700))
            render(f"{OUT}/family_{tag}_{view}.png")

if want("close"):
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in list(HEAD_VIEWS.items()) + list(BODY_VIEWS.items()):
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/close_{name}.png")

if want("trio"):
    for tag, azim in (("front", -20), ("back", 160)):
        scene(); put(OLD, -0.66, 0, old_palette()); put(NEW, 0.0)
        if os.path.exists(DANTE):
            put(DANTE, 0.66)
        camera((0, 0, 0.40), 13.0, lens=200, elev=5, azim=azim, res=(1800, 900))
        render(f"{OUT}/trio_{tag}.png")

if want("far"):
    scene(); put(OLD, -0.62, 0, old_palette()); put(NEW, 0.0)
    if os.path.exists(DANTE):
        put(DANTE, 0.62)
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


def turn_head(arm, yaw, pitch):
    """The head bone alone, over the idle pose: the test of rule 6."""
    bpy.context.scene.frame_set(0)
    arm.animation_data.action = None
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = rest.inverted() @ world @ rest
    bpy.context.view_layer.update()


if want("neck"):
    scene(); root, arm = put(NEW)
    for tag, yaw, pitch in (("yaw_left", 55, 0), ("yaw_right", -55, 0), ("nod_down", 0, -22), ("nod_up", 0, 22)):
        turn_head(arm, yaw, pitch)
        for view, azim, elev in (("front", -10, 6), ("back", 170, 8), ("side", 90, 4)):
            camera((0.0, 0.0, 0.47), 6.0, lens=150, elev=elev, azim=azim, res=(700, 760))
            render(f"{OUT}/neck_{tag}_{view}.png")

# the idle's three stances, each at the middle of its hold (seconds into the clip)
STANCES = {"pockets": 1.9, "quiff": 4.45, "behind": 6.9}
if want("idle"):
    scene(); root, arm = put(NEW, pose="idle")
    fps = bpy.context.scene.render.fps
    for name, at in STANCES.items():
        f = at * fps
        bpy.context.scene.frame_set(int(f), subframe=f - int(f))
        for view, azim, elev in (("front", -12, 8), ("side_left", 90, 6), ("side_right", -90, 6), ("back", 168, 10)):
            camera((0.0, 0.0, 0.42), 6.0, lens=120, elev=elev, azim=azim, res=(560, 640))
            render(f"{OUT}/idle_{name}_{view}.png")

# the frames where a limb is furthest from rest, as fractions of each clip's length
EXTREMES = {"walk": (0.25, 0.75), "sprint": (0.25, 0.75), "jump": (0.12, 0.95), "fall": (0.25, 0.75)}
if want("clips"):
    for clip, fractions in EXTREMES.items():
        scene(); root, arm = put(NEW, pose=clip)
        act = arm.animation_data.action
        first, last = act.frame_range
        for k, fr in enumerate(fractions):
            f = first + (last - first) * fr
            bpy.context.scene.frame_set(int(f), subframe=f - int(f))
            for view, azim, elev in (("front", -12, 8), ("side", 90, 6), ("back", 168, 10)):
                camera((0.0, 0.0, 0.42), 6.0, lens=120, elev=elev, azim=azim, res=(560, 640))
                render(f"{OUT}/clip_{clip}_{k}_{view}.png")
    # the first six frames of the jump, front and three-quarter: do the arms read as UP and clear
    # of the face (rule 8 as amended)
    scene(); root, arm = put(NEW, pose="jump")
    first = arm.animation_data.action.frame_range[0]
    for i in range(6):
        bpy.context.scene.frame_set(int(first) + i)
        for view, azim in (("front", 0), ("34", 35)):
            camera((0.0, 0.0, 0.44), 6.0, lens=130, elev=6, azim=azim, res=(480, 560))
            render(f"{OUT}/jumpstart_{view}_{i}.png")
