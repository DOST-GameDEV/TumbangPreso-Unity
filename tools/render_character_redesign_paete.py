"""Render the review set of the Paete redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_paete.py -- v03
    ... -- v03 turn face          (only some shots: orig turn face facecmp trio braid close look extremes)
    py -3 tools/sheet_character_redesign_paete.py v03

Writes single frames into Logs/character-redesign-paete/<version>/; the sheet script stacks and
labels them into the review sheets beside that folder. A NEW VERSION NAME EVERY ITERATION, and
LOOK at what comes out before believing it.

His own copy of the render script (docs/CHARACTER_REDESIGN_DANTE.md section 13: every hero works
in its own files). It draws paete-redesign.glb beside the original team-paete.glb and beside the
redesigned Dante, the way the game draws them as near as EEVEE allows:
  * the two-band toon ramp of `TumbangPreso/Toon`, a warm lit band and a cool shadow band;
  * the inverted-hull ink edge at `ToonSkin.PersonOutlineWidth` (0.0045 in model space);
  * the palette remap of `Toon.shader` baked into pixels for the palette model (the original).
IT IS AN APPROXIMATION. Nothing here has been through Unity. It is for judging shape, paint,
silhouette and clipping, not final colour.

Shots this hero needs that the others do not:
  braid     each forearm braid in the rest pose from four sides, the original's beside it
  look      the head bone turned 55 degrees each way and nodded 22, from the front AND the back:
            his collar leaves, collar moss and nape moss sit round the neck
HE IS WIDE (1.27 m across the arms against 0.74 tall), so a turnaround is one frame an angle.
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
BASE_OUT = REPO + "/Logs/character-redesign-paete"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/paete/paete-redesign.glb"
OLD = PERSONS + "/team-paete.glb"
DANTE = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb"
OLD_PALETTE = "person_paete.asset"


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

# HE IS WIDE. Arms out, the original spans 1.27 m against 0.79 tall, so a five-up row would be
# all arms. Each angle is its own frame and the sheet script lays them side by side.
if want("orig"):
    for rot in ANGLES:
        scene(); put(OLD, 0, rot, roster_palette(OLD_PALETTE))
        camera((0, 0, 0.40), 13.0, lens=118, elev=5, azim=0, res=(900, 640))
        render(f"{OUT}/orig_turn_{rot:03d}.png")
    scene(); put(OLD, 0, 0, roster_palette(OLD_PALETTE))
    camera((0.0, 0, 0.64), 6.0, lens=150, elev=2, azim=0, res=(900, 800)); render(f"{OUT}/orig_face.png")
    camera((0.0, 0, 0.64), 6.0, lens=150, elev=4, azim=-32, res=(900, 800)); render(f"{OUT}/orig_face34.png")
    camera((0.0, 0, 0.36), 6.0, lens=170, elev=4, azim=0, res=(900, 800)); render(f"{OUT}/orig_chest.png")
    camera((0.0, 0, 0.36), 6.0, lens=170, elev=4, azim=180, res=(900, 800)); render(f"{OUT}/orig_back.png")
    camera((0.0, 0, 0.40), 9.0, lens=150, elev=4, azim=90, res=(900, 800)); render(f"{OUT}/orig_side.png")
    camera((0.0, 0, 0.45), 9.0, lens=120, elev=55, azim=-20, res=(900, 800)); render(f"{OUT}/orig_above.png")
    camera((0.0, 0, 0.12), 6.0, lens=170, elev=14, azim=-25, res=(900, 800)); render(f"{OUT}/orig_legs.png")
    camera((0.0, 0.0, 0.64), 6.0, lens=150, elev=6, azim=180, res=(900, 800)); render(f"{OUT}/orig_head_back.png")
    for s_, tag in ((1, "left"), (-1, "right")):
        camera((s_ * 0.42, 0, 0.47), 6.0, lens=170, elev=10, azim=s_ * 12, res=(1000, 700)); render(f"{OUT}/orig_arm_{tag}_front.png")
        camera((s_ * 0.42, 0, 0.47), 6.0, lens=170, elev=50, azim=s_ * 12, res=(1000, 700)); render(f"{OUT}/orig_arm_{tag}_above.png")
        camera((s_ * 0.42, 0, 0.47), 6.0, lens=170, elev=8, azim=180 - s_ * 12, res=(1000, 700)); render(f"{OUT}/orig_arm_{tag}_back.png")
    scene(); put(OLD, 0, 0, roster_palette(OLD_PALETTE), pose=None)
    camera((0, 0, 0.40), 13.0, lens=100, elev=5, azim=0, res=(1400, 800)); render(f"{OUT}/orig_rest_front.png")
    camera((0, 0, 0.40), 13.0, lens=100, elev=60, azim=0, res=(1400, 800)); render(f"{OUT}/orig_rest_above.png")

def fit(width):
    """The lens that frames `width` metres across at 6 m."""
    return 6.0 * 36.0 / width


if want("turn"):
    for rot in ANGLES:
        scene(); put(NEW, 0, rot)
        camera((0, 0, 0.38), 6.0, lens=fit(1.25), elev=5, azim=0, res=(900, 640))
        render(f"{OUT}/turn_{rot:03d}.png")
    for rot in ANGLES:
        scene(); put(OLD, 0, rot, roster_palette(OLD_PALETTE))
        camera((0, 0, 0.38), 6.0, lens=fit(1.25), elev=5, azim=0, res=(900, 640))
        render(f"{OUT}/origturn_{rot:03d}.png")

if want("face"):
    scene(); put(NEW)
    camera((0.0, 0, 0.605), 6.0, lens=fit(0.42), elev=2, azim=0, res=(900, 800)); render(f"{OUT}/face.png")
    camera((0.0, 0, 0.605), 6.0, lens=fit(0.42), elev=4, azim=-32, res=(900, 800)); render(f"{OUT}/face34.png")
    camera((0.0, 0, 0.605), 6.0, lens=fit(0.42), elev=4, azim=32, res=(900, 800)); render(f"{OUT}/face34b.png")

if want("facecmp"):
    # the face judged BESIDE the original's, front and three-quarter, and beside redesigned Dante's
    for tag, rot in (("front", 0), ("34", 32), ("34b", -32)):
        scene(); put(OLD, -0.24, rot, roster_palette(OLD_PALETTE)); put(NEW, 0.24, rot)
        camera((0.0, 0, 0.62), 6.0, lens=fit(0.92), elev=3, azim=0, res=(1500, 700))
        render(f"{OUT}/facecmp_{tag}.png")
    scene(); put(OLD, -0.62, 0, roster_palette(OLD_PALETTE)); put(NEW, 0.0, 0); put(DANTE, 0.62, 0)
    camera((0.0, 0, 0.42), 6.0, lens=fit(2.0), elev=3, azim=0, res=(1800, 800))
    render(f"{OUT}/headfam_front.png")

if want("trio"):
    for tag, rot in (("", 0), ("_back", 180)):
        scene(); put(OLD, -0.95, rot, roster_palette(OLD_PALETTE)); put(NEW, 0.0, rot); put(DANTE, 0.80, rot)
        camera((0, 0, 0.38), 9.0, lens=9.0 * 36.0 / 3.0, elev=5, azim=-16, res=(1800, 760))
        render(f"{OUT}/trio{tag}.png")

# THE BRAIDS, the heart of him: each forearm in the REST pose (arms straight out, as the rig binds
# them) from the front, above and behind, the original's the same way beside it.
if want("braid"):
    for which, path, pal in (("new", NEW, None), ("orig", OLD, roster_palette(OLD_PALETTE))):
        scene(); put(path, 0, 0, pal, pose=None)
        for s_, tag in ((1, "left"), (-1, "right")):
            for view, az, el in (("front", 0, 8), ("above", 0, 62), ("back", 180, 8)):
                camera((s_ * 0.49, 0, 0.47), 6.0, lens=fit(0.40), elev=el, azim=az, res=(900, 520))
                render(f"{OUT}/braid_{which}_{tag}_{view}.png")
    scene(); put(NEW)
    for s_, tag in ((1, "left"), (-1, "right")):
        camera((s_ * 0.36, 0, 0.26), 6.0, lens=fit(0.46), elev=8, azim=s_ * 28, res=(800, 800))
        render(f"{OUT}/braid_idle_{tag}.png")

# name -> (target, width in metres, elev, azim)
CLOSE = {
    "chest": ((0.0, 0.0, 0.38), 0.50, 4, 0),
    "back": ((0.0, 0.0, 0.38), 0.50, 4, 180),
    "legs_front": ((0.0, 0.0, 0.13), 0.46, 6, 0),
    "legs_back": ((0.0, 0.0, 0.13), 0.46, 6, 180),
    "feet": ((0.0, 0.0, 0.06), 0.56, 34, -25),
    "side_left": ((0.0, 0.0, 0.38), 0.95, 4, 90),
    "side_right": ((0.0, 0.0, 0.38), 0.95, 4, -90),
    "shoulder_left": ((0.21, 0.0, 0.48), 0.36, 22, 30),
    "shoulder_right": ((-0.21, 0.0, 0.48), 0.36, 22, -30),
    "head_back": ((0.0, 0.0, 0.60), 0.42, 6, 180),
    "head_above": ((0.0, 0.0, 0.62), 0.46, 48, -18),
    "above": ((0.0, 0.0, 0.42), 1.10, 58, -20),
}
if want("close"):
    scene(); put(NEW)
    for name, (t, width, el, az) in CLOSE.items():
        camera(t, 6.0, lens=fit(width), elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/close_{name}.png")
    scene(); put(OLD, 0, 0, roster_palette(OLD_PALETTE))
    for name in ("chest", "back", "legs_front", "head_back", "shoulder_left"):
        t, width, el, az = CLOSE[name]
        camera(t, 6.0, lens=fit(width), elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/origclose_{name}.png")

LOOKS = (("yaw+55", 55, 0), ("yaw-55", -55, 0), ("nod-22", 0, -22), ("nod+20", 0, 20), ("yaw55_nod22", 55, -22))
if want("look"):
    scene(); root, arm = put(NEW)
    for tag, yaw, pitch in LOOKS:
        turn_head(arm, yaw, pitch)
        for view, az, el in (("front", -10, 6), ("back", 170, 8), ("side", 90, 6)):
            camera((0.0, 0.0, 0.57), 6.0, lens=fit(0.62), elev=el, azim=az, res=(700, 700))
            render(f"{OUT}/look_{tag}_{view}.png")

#   clip -> frames at the importer's 24 fps (walk about 17 frames, sprint 12, idle 192)
EXTREMES = {"idle": (0, 50, 106, 152), "walk": (4, 13), "sprint": (3, 9), "jump": (0, 2, 5, 11), "fall": (2, 6)}
if want("extremes"):
    for clip, frames in EXTREMES.items():
        for f in frames:
            scene(); root, arm = put(NEW, pose=clip, frame=f)
            for view, az, el in (("front", -12, 8), ("side", 90, 6), ("back", 168, 10)):
                camera((0.0, 0.0, 0.42), 6.0, lens=fit(1.30), elev=el, azim=az, res=(560, 520))
                render(f"{OUT}/ext_{clip}_{f:03d}_{view}.png")
