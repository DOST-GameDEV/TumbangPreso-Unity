"""Render the review set of the Amihan redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_amihan.py -- v01
    ... -- v01 turn face          (only some shots: orig turn face pair far close look extremes)
    py -3 tools/sheet_character_redesign_amihan.py v01

Writes single frames into Logs/character-redesign-amihan/<version>/; the sheet script stacks and
labels them into the review sheets beside that folder. A NEW VERSION NAME EVERY ITERATION
(docs/CHARACTER_MODEL_METHOD.md section 5), and LOOK at what comes out before believing it.

A copy of tools/render_character_redesign_dante.py, rewritten for her (section 13 of
docs/CHARACTER_REDESIGN_DANTE.md: one hero, one set of files, nobody edits another hero's).
It draws the prototype .glb written by tools/author_character_redesign_amihan.py beside the
original team-amihan.glb and the redesigned Dante, in the rig's `idle` pose, the way the game
draws them as near as EEVEE allows:
  * the two-band toon ramp of `TumbangPreso/Toon`, a warm lit band and a cool shadow band;
  * the inverted-hull ink edge at `ToonSkin.PersonOutlineWidth` (0.0045 in model space), on a
    welded copy of each mesh;
  * the palette remap of `Toon.shader` baked into pixels for the palette model (UV rows 0 to 7
    take the roster entry's 16 colours, the rest the texture).
IT IS AN APPROXIMATION. Nothing here has been through Unity. It is for judging shape, paint and
silhouette, not final colour.

Shots that Dante's script does not have, because her cloth asks for them:
  look      the head bone alone turned 55 degrees each way and nodded 22, seen from the front
            AND the back: the capelet's neck sits round the jaw and the hair hangs over its back
  extremes  the far frames of walk, sprint, jump and fall from the front, the side and the back:
            the coat skirt takes the legs, the capelet's shoulders take the arms
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
only = set(a for a in args[1:] if not a.startswith("--") and "/" not in a and "\\" not in a)
BASE_OUT = REPO + "/Logs/character-redesign-amihan"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/amihan/amihan-redesign.glb"
if "--glb" in args:
    NEW = args[args.index("--glb") + 1]
    only.discard(NEW)
OLD = PERSONS + "/team-amihan.glb"
DANTE = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb"


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
    return roster_palette("person_amihan.asset")


# THE ORIGINAL FIRST. Front, three-quarter, side, back, then her face, her back and her head from
# above, so the redesign is drawn from what she has and not from what Dante has.
if only and "orig" in only:
    scene()
    for i, rot in enumerate((0, 35, 90, 180)):
        put(OLD, -1.05 + 0.70 * i, rot, old_palette())
    camera((0, 0, 0.40), 13.0, lens=160, elev=5, azim=0, res=(1920, 700))
    render(f"{OUT}/orig_turn.png")
    scene(); put(OLD, 0, 0, old_palette())
    for name, (t, d, lens, el, az) in {
        "face": ((0.02, 0, 0.555), 6.0, 200, 2, 0), "face34": ((0.0, 0, 0.53), 6.0, 170, 4, -32),
        "face34_left": ((0.0, 0, 0.53), 6.0, 170, 4, 32),
        "back": ((0.0, 0, 0.40), 6.0, 120, 6, 180), "above": ((0.0, 0, 0.45), 9.0, 190, 55, -20),
        "chest": ((0.0, 0, 0.24), 6.0, 200, 4, 0), "side_left": ((0, 0, 0.40), 9.0, 170, 4, 90),
        "side_right": ((0, 0, 0.40), 9.0, 170, 4, -90), "rear34": ((0, 0, 0.40), 9.0, 170, 8, 145),
    }.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/orig_{name}.png")

if want("turn"):
    scene()
    for i, rot in enumerate((0, 35, 90, 180)):
        put(NEW, -1.05 + 0.70 * i, rot)
    camera((0, 0, 0.40), 13.0, lens=160, elev=5, azim=0, res=(1920, 700))
    render(f"{OUT}/turn.png")
    scene()
    for i, rot in enumerate((-35, -90, 145, -145)):
        put(NEW, -1.05 + 0.70 * i, rot)
    camera((0, 0, 0.40), 13.0, lens=160, elev=5, azim=0, res=(1920, 700))
    render(f"{OUT}/turn2.png")

if want("face"):
    scene(); put(NEW)
    camera((0.02, 0, 0.555), 6.0, lens=200, elev=2, azim=0, res=(900, 800))
    render(f"{OUT}/face.png")
    camera((0.0, 0, 0.53), 6.0, lens=170, elev=4, azim=-32, res=(900, 800))
    render(f"{OUT}/face34.png")
    camera((0.0, 0, 0.53), 6.0, lens=170, elev=4, azim=32, res=(900, 800))
    render(f"{OUT}/face34_left.png")
    scene(); put(OLD, 0, 0, old_palette())
    camera((0.02, 0, 0.555), 6.0, lens=200, elev=2, azim=0, res=(900, 800))
    render(f"{OUT}/face_orig.png")
    camera((0.0, 0, 0.53), 6.0, lens=170, elev=4, azim=-32, res=(900, 800))
    render(f"{OUT}/face34_orig.png")
    camera((0.0, 0, 0.53), 6.0, lens=170, elev=4, azim=32, res=(900, 800))
    render(f"{OUT}/face34_left_orig.png")

if want("pair"):
    scene(); put(OLD, -0.66, 0, old_palette()); put(NEW, 0.0); put(DANTE, 0.66)
    camera((0, 0, 0.40), 13.0, lens=200, elev=5, azim=-22, res=(1800, 900))
    render(f"{OUT}/pair.png")
    scene(); put(OLD, -0.66, 180, old_palette()); put(NEW, 0.0, 180); put(DANTE, 0.66, 180)
    camera((0, 0, 0.40), 13.0, lens=200, elev=5, azim=-22, res=(1800, 900))
    render(f"{OUT}/pair_back.png")

if want("far"):
    scene(); put(OLD, -0.62, 0, old_palette()); put(NEW, 0.0); put(DANTE, 0.62)
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

# name -> (target, dist, lens, elev, azim)
CLOSE = {
    "hair_back": ((0.0, 0.1, 0.56), 6.0, 190, 6, 180),
    "hair_above": ((0.0, 0.0, 0.64), 6.0, 190, 38, -18),
    "hair_above_back": ((0.0, 0.0, 0.64), 6.0, 190, 42, 160),
    "ear_left": ((0.19, 0.0, 0.47), 6.0, 520, 4, 70),
    "ear_right": ((-0.19, 0.0, 0.47), 6.0, 520, 4, -70),
    "flowers": ((0.19, -0.10, 0.60), 6.0, 520, 6, 30),
    "hand_left_front": ((0.20, 0.0, 0.17), 6.0, 520, 8, 10),
    "hand_left_back": ((0.20, 0.0, 0.17), 6.0, 520, 8, 170),
    "hand_right_front": ((-0.20, 0.0, 0.17), 6.0, 520, 8, -10),
    "hand_right_back": ((-0.20, 0.0, 0.17), 6.0, 520, 8, -170),
    "hem_left": ((0.17, 0.0, 0.13), 6.0, 480, 10, 40),
    "hem_right": ((-0.17, 0.0, 0.13), 6.0, 480, 10, -40),
    "feet": ((0.0, 0.0, 0.07), 6.0, 240, 14, -25),
    "chest": ((0.0, 0.0, 0.25), 6.0, 240, 4, 0),
    "neck": ((0.0, 0.0, 0.36), 6.0, 380, 6, -18),
    "back": ((0.0, 0.0, 0.24), 6.0, 200, 4, 180),
    "cape_back": ((0.0, 0.0, 0.33), 6.0, 330, 10, 180),
    "side_left": ((0.0, 0.0, 0.40), 9.0, 170, 4, 90),
    "side_right": ((0.0, 0.0, 0.40), 9.0, 170, 4, -90),
    "above": ((0.0, 0.0, 0.45), 9.0, 190, 55, -20),
    "below": ((0.0, 0.0, 0.20), 6.0, 200, -28, -25),
}
if want("close"):
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in CLOSE.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/close_{name}.png")


def head_pose(arm, yaw, pitch):
    """The head bone alone, turned and nodded over the idle pose (the motion script's own test)."""
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = arm["head_base"] and Quaternion(arm["head_base"]) @ (rest.inverted() @ world @ rest)
    bpy.context.view_layer.update()


if want("look"):
    scene(); root, arm = put(NEW)
    bpy.context.scene.frame_set(0)
    arm.animation_data.action = None
    arm["head_base"] = list(arm.pose.bones["head"].rotation_quaternion)
    LOOKS = (("yaw55", 55, 0), ("yawm55", -55, 0), ("nod22", 0, -22), ("up20", 0, 20), ("yaw55_nod22", 55, -22), ("yawm55_up20", -55, 20))
    VIEWS = (("front", -10, 8), ("back", 170, 12), ("side", 90, 6), ("top", 150, 48))
    for tag, yaw, pitch in LOOKS:
        head_pose(arm, yaw, pitch)
        for vname, az, el in VIEWS:
            camera((0.0, 0.0, 0.47), 6.0, lens=250, elev=el, azim=az, res=(640, 640))
            render(f"{OUT}/look_{tag}_{vname}.png")

if only and "faces" in only:
    # the face alone, large, the redesign and the original from the same three cameras, to compare
    # the eye outlines corner by corner
    for tag, path, pal in (("new", NEW, None), ("orig", OLD, old_palette())):
        scene(); put(path, 0, 0, pal)
        for vname, az in (("front", 0), ("right34", -32), ("left34", 32)):
            camera((0.0, 0.0, 0.50), 6.0, lens=330, elev=3, azim=az, res=(900, 760))
            render(f"{OUT}/faces_{tag}_{vname}.png")

if only and "prev" in only:
    # the turnaround beside the version before it (a copy of that .glb, kept by hand beside its atlas)
    prev = args[args.index("--prev") + 1]
    for tag, path in (("new", NEW), ("prev", prev)):
        scene()
        for i, rot in enumerate((0, 35, 90, 180)):
            put(path, -1.05 + 0.70 * i, rot)
        camera((0, 0, 0.40), 13.0, lens=160, elev=5, azim=0, res=(1920, 700))
        render(f"{OUT}/prev_{tag}.png")

if only and "jump6" in only:
    # the first six frames of the jump from the front: do the arms read as UP
    scene(); root, arm = put(NEW, pose="jump")
    first, last = arm.animation_data.action.frame_range
    for i in range(6):
        f = first + i * 24.0 / 60.0      # the clip is keyed at 60 a second, the scene runs at 24
        bpy.context.scene.frame_set(int(f), subframe=f - int(f))
        for vname, az in (("front", 0), ("rear", 180)):
            camera((0.0, 0.0, 0.40), 6.0, lens=150, elev=6, azim=az, res=(420, 520))
            render(f"{OUT}/jump6_{i}_{vname}.png")

if only and "family" in only:
    # her head beside redesigned Dante's and beside her original, the same three cameras
    for tag, path, pal in (("new", NEW, None), ("dante", DANTE, None), ("orig", OLD, old_palette())):
        scene(); put(path, 0, 0, pal)
        for vname, az in (("front", 0), ("right34", -32), ("left34", 32)):
            camera((0.0, 0.0, 0.50), 6.0, lens=300, elev=3, azim=az, res=(760, 700))
            render(f"{OUT}/family_{tag}_{vname}.png")

if only and "jaw" in only:
    # the lower face, where the owner circled dark patches: the head straight, then turned 30 and
    # nodded 10 each way, from the front and both three-quarters
    scene(); root, arm = put(NEW)
    bpy.context.scene.frame_set(0)
    arm.animation_data.action = None
    arm["head_base"] = list(arm.pose.bones["head"].rotation_quaternion)
    for tag, yaw, pitch in (("rest", 0, 0), ("nod10", 0, -10), ("yaw30_nod10", 30, -10), ("yawm30_nod10", -30, -10)):
        head_pose(arm, yaw, pitch)
        for vname, az in (("front", 0), ("right34", -35), ("left34", 35)):
            camera((0.0, 0.0, 0.47), 6.0, lens=300, elev=5, azim=az, res=(640, 640))
            render(f"{OUT}/jaw_{tag}_{vname}.png")

if want("extremes"):
    # (clip, fraction of its length): the contact and passing frames of the strides, the launch and
    # the held tuck of the jump, both ends of the fall's kick
    FRAMES = {"walk": (0.25, 0.75, 0.5), "sprint": (0.25, 0.75, 0.5), "jump": (0.06, 0.3, 0.95), "fall": (0.25, 0.75),
              # the idle is acted: the neutral stand, then one frame inside each stance
              "idle": (0.0, 0.22, 0.49, 0.79)}
    VIEWS = (("front", -12, 8), ("side", 90, 6), ("back", 148, 10), ("rear", 180, 8))
    for clip, fracs in FRAMES.items():
        scene(); root, arm = put(NEW, pose=clip)
        first, last = arm.animation_data.action.frame_range
        for fr in fracs:
            f = first + (last - first) * fr
            bpy.context.scene.frame_set(int(f), subframe=f - int(f))
            for vname, az, el in VIEWS:
                camera((0.0, 0.0, 0.42), 6.0, lens=150, elev=el, azim=az, res=(480, 600))
                render(f"{OUT}/ext_{clip}_{int(fr * 100):02d}_{vname}.png")
