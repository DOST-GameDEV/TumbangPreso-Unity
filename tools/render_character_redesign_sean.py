"""Render the review set of the Sean (displayed: Rago) redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_sean.py -- v03
    ... -- v03 turn face          (only some shots: orig turn face trio close look clips far)
    py -3 tools/sheet_character_redesign_sean.py v03

Writes single frames into Logs/character-redesign-sean/<version>/; the sheet script stacks and
labels them into the review sheets beside that folder. A NEW VERSION NAME EVERY ITERATION
(docs/CHARACTER_MODEL_METHOD.md section 5), and LOOK at what comes out before believing it.

A copy of tools/render_character_redesign_dante.py rewritten for this hero (docs/
CHARACTER_REDESIGN_DANTE.md section 13: one set of scripts a hero, never another hero's edited).
What differs from Dante's:
  * he is the cast's TALLEST and WIDEST (0.848 to the tip of the flame against Dante's 0.785,
    fists out to 0.405), so every camera is aimed higher and stands further off;
  * `orig` draws team-sean.glb alone from four sides plus the back and above, because the
    original has to be LOOKED at before anything is designed against it;
  * `look` is the neck test rule 6 asks for: the head bone turned 55 degrees and nodded 22, seen
    from the front AND the back. He wears no collar, so what is being checked is the nape tail of
    the mohawk on the traps and the jaw on the vest's shoulder piping;
  * `clips` holds each new clip at its extreme frames from the front, the side and the back.

The look is the same approximation as Dante's script:
  * the two-band toon ramp of `TumbangPreso/Toon`, a warm lit band and a cool shadow band;
  * the inverted-hull ink edge at `ToonSkin.PersonOutlineWidth` (0.0045 in model space);
  * the palette remap of `Toon.shader` baked into pixels for the palette models.
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
BASE_OUT = REPO + "/Logs/character-redesign-sean"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/sean/sean-redesign.glb"
DANTE = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb"
OLD = PERSONS + "/team-sean.glb"
OLD_DANTE = PERSONS + "/team-dante.glb"
MID = 0.44        # the height a whole-body camera aims at


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
    """The head bone alone, turned about the world's up and nodded about the world's side axis."""
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    # `pitch` is degrees of nod DOWN: about the world's +x a positive angle drops the face,
    # which looks down -y. (Checked against the render: the first two versions had it backwards
    # and labelled the look-up as the nod.)
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = rest.inverted() @ world @ rest
    bpy.context.view_layer.update()


SEAN_PAL = "person_sean.asset"

# name -> (target, dist, lens, elev, azim)
CLOSE = {
    "hair_back": ((0.0, 0.1, 0.66), 6.0, 190, 6, 180),
    "hair_above": ((0.0, 0.0, 0.74), 6.0, 190, 40, -18),
    "hair_side": ((0.0, 0.0, 0.68), 6.0, 190, 6, 90),
    "ear_left": ((0.19, 0.0, 0.60), 6.0, 560, 4, 70),
    "ear_right": ((-0.19, 0.0, 0.60), 6.0, 560, 4, -70),
    "hand_left_front": ((0.30, 0.0, 0.19), 6.0, 520, 8, 10),
    "hand_left_back": ((0.30, 0.0, 0.19), 6.0, 520, 8, 170),
    "hand_right_front": ((-0.30, 0.0, 0.19), 6.0, 520, 8, -10),
    "hand_right_back": ((-0.30, 0.0, 0.19), 6.0, 520, 8, -170),
    "hem_left": ((0.16, 0.0, 0.27), 6.0, 420, 10, 40),
    "hem_right": ((-0.16, 0.0, 0.27), 6.0, 420, 10, -40),
    "feet": ((0.0, 0.0, 0.08), 6.0, 240, 14, -25),
    "feet_back": ((0.0, 0.0, 0.08), 6.0, 240, 14, 155),
    "chest": ((0.0, 0.0, 0.37), 6.0, 220, 4, 0),
    "back": ((0.0, 0.0, 0.36), 6.0, 200, 4, 180),
    "side_left": ((0.0, 0.0, MID), 9.0, 170, 4, 90),
    "side_right": ((0.0, 0.0, MID), 9.0, 170, 4, -90),
    "above": ((0.0, 0.0, 0.50), 9.0, 180, 55, -20),
}

if want("orig"):
    scene()
    for i, rot in enumerate((0, 35, 90, 180)):
        put(OLD, -1.35 + 0.90 * i, rot, roster_palette(SEAN_PAL))
    camera((0, 0, MID), 13.0, lens=130, elev=5, azim=0, res=(1920, 640))
    render(f"{OUT}/orig_turn.png")
    scene(); put(OLD, 0, 0, roster_palette(SEAN_PAL))
    camera((0.0, 0, 0.64), 6.0, lens=190, elev=2, azim=0, res=(900, 800)); render(f"{OUT}/orig_face.png")
    camera((0.0, 0, 0.63), 6.0, lens=170, elev=4, azim=-32, res=(900, 800)); render(f"{OUT}/orig_face34.png")
    for name in ("hair_back", "hair_above", "hair_side", "chest", "back", "feet", "above", "hand_left_front", "side_left"):
        t, d, lens, el, az = CLOSE[name]
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700)); render(f"{OUT}/orig_{name}.png")

if want("turn"):
    scene()
    for i, rot in enumerate((0, 35, 90, 180)):
        put(NEW, -1.35 + 0.90 * i, rot)
    camera((0, 0, MID), 13.0, lens=130, elev=5, azim=0, res=(1920, 640))
    render(f"{OUT}/turn.png")
    scene()
    for i, rot in enumerate((215, 270, 325, 145)):
        put(NEW, -1.35 + 0.90 * i, rot)
    camera((0, 0, MID), 13.0, lens=130, elev=5, azim=0, res=(1920, 640))
    render(f"{OUT}/turn2.png")

if want("face"):
    scene(); put(NEW)
    camera((0.0, 0, 0.64), 6.0, lens=190, elev=2, azim=0, res=(900, 800)); render(f"{OUT}/face.png")
    camera((0.0, 0, 0.63), 6.0, lens=170, elev=4, azim=-32, res=(900, 800)); render(f"{OUT}/face34.png")
    camera((0.0, 0, 0.63), 6.0, lens=170, elev=4, azim=32, res=(900, 800)); render(f"{OUT}/face34b.png")
    camera((0.0, 0, 0.63), 6.0, lens=170, elev=4, azim=90, res=(900, 800)); render(f"{OUT}/face_side.png")

if want("facecmp"):
    # rule 12: the face judged BESIDE THE ORIGINAL's, front and three-quarter. As cute, or cuter?
    for tag, rot in (("front", 0), ("34", 32)):
        scene(); put(OLD, -0.30, rot, roster_palette(SEAN_PAL)); put(NEW, 0.30, rot)
        camera((0.0, 0, 0.63), 9.0, lens=190, elev=3, azim=0, res=(1500, 760))
        render(f"{OUT}/facecmp_{tag}.png")

PREV = BASE_OUT + "/prev/sean-redesign.glb"     # a copy of the build before this one, beside its atlas
if want("versus") and os.path.exists(PREV):
    for tag, rots in (("a", (0, 35)), ("b", (90, 180))):
        scene()
        for i, rot in enumerate(rots):
            put(PREV, -1.35 + 1.80 * i, rot); put(NEW, -0.45 + 1.80 * i, rot)
        camera((0, 0, MID), 13.0, lens=130, elev=5, azim=0, res=(1920, 640))
        render(f"{OUT}/versus_{tag}.png")

if want("jump6"):
    for f in range(6):
        scene(); root, arm = put(NEW, pose="jump", frame=float(f))
        camera((0.0, 0, 0.48), 6.0, lens=120, elev=6, azim=0, res=(520, 640))
        render(f"{OUT}/jump6_{f}.png")

if want("trio"):
    # the original, the redesign, and redesigned Dante: does he belong beside both
    scene(); put(OLD, -0.80, 0, roster_palette(SEAN_PAL)); put(NEW, 0.0); put(DANTE, 0.74)
    camera((0, 0, MID), 13.0, lens=175, elev=5, azim=-16, res=(1800, 900))
    render(f"{OUT}/trio.png")
    scene(); put(OLD, -0.80, 180, roster_palette(SEAN_PAL)); put(NEW, 0.0, 180); put(DANTE, 0.74, 180)
    camera((0, 0, MID), 13.0, lens=175, elev=5, azim=-16, res=(1800, 900))
    render(f"{OUT}/trio_back.png")

if want("far"):
    scene(); put(OLD, -0.80, 0, roster_palette(SEAN_PAL)); put(NEW, 0.0); put(DANTE, 0.74)
    camera((0, 0, MID), 13.0, lens=175, elev=5, azim=-16, res=(640, 320))
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

if want("look"):
    # rule 6: 55 degrees of yaw, 22 of nod, from the front AND the back
    for tag, yaw, pitch in (("yaw55", 55, 0), ("yawm55", -55, 0), ("nod22", 0, 22), ("back22", 0, -20), ("yaw55nod22", 55, 22)):
        scene(); root, arm = put(NEW)
        bpy.context.scene.frame_set(0); arm.animation_data.action = None
        set_pose(arm, None)
        # the idle hang of the arms, so the shoulders are where the game shows them
        for b, a in (("arm-left", -50), ("arm-right", 50)):
            pb = arm.pose.bones[b]; pb.rotation_mode = "QUATERNION"
            rest = pb.bone.matrix_local.to_quaternion()
            pb.rotation_quaternion = rest.inverted() @ Quaternion(Vector((0, 1, 0)), math.radians(-a)) @ rest
        turn_head(arm, yaw, pitch)
        for view, az, el in (("front", -14, 6), ("back", 166, 10), ("side", 90, 6)):
            camera((0.0, 0, 0.60), 6.0, lens=230, elev=el, azim=az, res=(700, 700))
            render(f"{OUT}/look_{tag}_{view}.png")

def fist_centre(arm, side):
    """Where a bracer and fist ARE in the current pose: the mean of the posed body vertices that
    rest beyond x 0.26 on that side."""
    dg = bpy.context.evaluated_depsgraph_get()
    body = next(o for o in arm.children if o.type == "MESH" and o.name.startswith("body") and not o.name.endswith("-ink"))
    posed = body.evaluated_get(dg)
    pts = [posed.matrix_world @ posed.data.vertices[v.index].co for v in body.data.vertices
           if side * v.co.x > 0.26 and v.co.z > 0.30]
    return sum(pts, Vector()) / len(pts)


# the mapping check: both fists and bracers from eight angles, hanging and with the elbow bent
HAND_POSES = (("stand", "idle", 0.0), ("walk", "walk", 4.0), ("guard", "idle", 137.0))
if want("hands"):
    for tag, clip, f in HAND_POSES:
        scene(); root, arm = put(NEW, pose=clip, frame=f)
        for side, name in ((1, "left"), (-1, "right")):
            at = fist_centre(arm, side)
            for k in range(8):
                camera(tuple(at), 6.0, lens=430, elev=(14 if k % 2 == 0 else 34), azim=45.0 * k, res=(420, 420))
                render(f"{OUT}/hand_{tag}_{name}_{k}.png")

# clip -> the frames worth holding. ⚠️ BLENDER IMPORTS AT 24 FRAMES A SECOND: walk is 17 frames
# long, not 43. The first three versions asked for frames past the end and were shown the last
# pose again, so only ONE end of each stride had been looked at.
EXTREMES = {"idle": (48, 98, 137, 145, 157), "walk": (0, 4, 9, 13), "sprint": (0, 3, 6, 9), "jump": (0, 1, 3, 11),
            "fall": (0, 2, 4, 6)}
if want("clips"):
    for clip, frames in EXTREMES.items():
        for f in frames:
            scene(); root, arm = put(NEW, pose=clip, frame=float(f))
            for view, az, el in (("front", -12, 8), ("side", 90, 6), ("back", 168, 10)):
                camera((0.0, 0, 0.46), 6.0, lens=115, elev=el, azim=az, res=(560, 620))
                render(f"{OUT}/clip_{clip}_{f:02d}_{view}.png")
