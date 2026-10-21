"""Render the review set of the Ate Girlie redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_ate_girlie.py -- v01
    ... -- v01 turn face          (only some shots: orig turn face pair far close look extremes)
    py -3 tools/sheet_character_redesign_ate_girlie.py v01      (the labelled review sheets, Logs/character-redesign-ate_girlie/v01_*.png)

Writes single frames into Logs/character-redesign-ate_girlie/<version>/; the sheet script stacks and
labels them into the review sheets beside that folder. A NEW VERSION NAME EVERY ITERATION
(docs/CHARACTER_MODEL_METHOD.md section 5), and LOOK at what comes out before believing it.

A copy of tools/render_character_redesign_phaister.py, rewritten for her (section 13 of
docs/CHARACTER_REDESIGN_DANTE.md: one character, one set of files, nobody edits another's).
It draws the prototype .glb written by tools/author_character_redesign_ate_girlie.py beside the
original character-female-b.glb WEARING HER PALETTE (person_ate_girlie.asset; the shared Classic rig
is nothing without it) and the redesigned Amihan, in the rig's `idle` pose, the way the game
draws them as near as EEVEE allows:
  * the two-band toon ramp of `TumbangPreso/Toon`, a warm lit band and a cool shadow band;
  * the inverted-hull ink edge at `ToonSkin.PersonOutlineWidth` (0.0045 in model space), on a
    welded copy of each mesh;
  * the palette remap of `Toon.shader` baked into pixels for the palette model (UV rows 0 to 7
    take the roster entry's 16 colours, the rest the texture).
IT IS AN APPROXIMATION. Nothing here has been through Unity. It is for judging shape, paint and
silhouette, not final colour.

Shots:
  look      the head bone alone turned 55 degrees each way and nodded 22, seen from the front
            AND the back: her buns, her long side locks and her nape hair are what could be turned through
  extremes  the far frames of walk, sprint, jump and fall from the front, the side and the back:
            her jeans, her garter and her strap shoes
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
BASE_OUT = REPO + "/Logs/character-redesign-ate_girlie"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/ate_girlie/ate_girlie-redesign.glb"
if "--glb" in args:
    NEW = args[args.index("--glb") + 1]
    only.discard(NEW)
OLD = PERSONS + "/character-female-b.glb"


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
    return roster_palette("person_ate_girlie.asset")


AMIHAN = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/amihan/amihan-redesign.glb"
CHESKA = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/cheska/cheska-redesign.glb"
DANTE = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb"
# the three approved Classic redesigns: she must belong beside them and copy none of them
BEBANG = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/bebang/bebang-redesign.glb"
BAYAN = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/bayan/bayan-redesign.glb"
LOLA = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/lola_pacing/lola_pacing-redesign.glb"
ROW = (AMIHAN, CHESKA, DANTE, NEW, BEBANG, BAYAN, LOLA)
TURN = dict(dist=13.0, lens=135, elev=5, azim=0, res=(1920, 780))
TURN_AT = (0, 0, 0.40)
STEP = 0.82
# one frame inside each idle stance, as fractions of the 8 s (the clips script says which is which)
IDLE_FRAMES = (0.0, 0.16, 0.25, 0.44, 0.58, 0.78, 0.90)   # the stand; the ate, looking left (1.3 s) and right (2.0 s); a piko hop (3.5 s); both feet apart (4.6 s); the garter kick (6.2 s); hands together (7.2 s)
clips_only = set(a for a in only if a in ("idle", "walk", "sprint", "jump", "fall"))
if clips_only:
    only.add("extremes")


def row(path, rots, palette=None, pose="idle", frame=0.0):
    scene()
    for i, rot in enumerate(rots):
        put(path, -1.5 * STEP + STEP * i, rot, palette, pose=pose, frame=frame)
    camera(TURN_AT, **TURN)


# THE ORIGINAL FIRST. Front, three-quarter, side, back, then her face, her back and her buns from
# above, so the redesign is drawn from what she has and not from what Dante has.
ORIG_VIEWS = {
    "face": ((0.0, 0, 0.50), 6.0, 420, 3, 0), "face34": ((0.0, 0, 0.50), 6.0, 400, 4, -32),
    "face34_left": ((0.0, 0, 0.50), 6.0, 400, 4, 32),
    "head": ((0.0, 0, 0.66), 6.0, 230, 6, 0), "head_back": ((0.0, 0, 0.60), 6.0, 230, 6, 180),
    "head_left": ((0.0, 0, 0.66), 6.0, 230, 6, 90), "head_right": ((0.0, 0, 0.66), 6.0, 230, 6, -90),
    "back": ((0.0, 0, 0.22), 6.0, 300, 6, 180), "above": ((0.0, 0, 0.60), 9.0, 250, 55, -20),
    "chest": ((0.0, 0, 0.24), 6.0, 380, 4, 0), "side_left": ((0, 0, 0.47), 9.0, 190, 4, 90),
    "side_right": ((0, 0, 0.47), 9.0, 190, 4, -90), "rear34": ((0, 0, 0.47), 9.0, 190, 8, 145),
    "feet": ((0.0, 0.0, 0.07), 6.0, 380, 14, -25), "below": ((0.0, 0.0, 0.30), 6.0, 240, -28, -25),
}
if only and "orig" in only:
    row(OLD, (0, 35, 90, 180), old_palette())
    render(f"{OUT}/orig_turn.png")
    row(OLD, (-35, -90, 145, -145), old_palette())
    render(f"{OUT}/orig_turn2.png")
    scene(); put(OLD, 0, 0, old_palette())
    for name, (t, d, lens, el, az) in ORIG_VIEWS.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/orig_{name}.png")
    # the rest pose, arms straight out: where her hands really are on the rig
    scene(); put(OLD, 0, 0, old_palette(), pose=None)
    for name, (t, d, lens, el, az) in {"rest_front": ((0, 0, 0.47), 9.0, 190, 4, 0), "rest_top": ((0, 0, 0.3), 9.0, 190, 80, 0),
                                       "rest_hand_left": ((0.25, 0.0, 0.29), 6.0, 620, 8, 12),
                                       "rest_hand_right": ((-0.25, 0.0, 0.29), 6.0, 620, 8, -12)}.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/orig_{name}.png")

if want("turn"):
    row(NEW, (0, 35, 90, 180))
    render(f"{OUT}/turn.png")
    row(NEW, (-35, -90, 145, -145))
    render(f"{OUT}/turn2.png")

FACE_VIEWS = (("front", 0), ("right34", -32), ("left34", 32))
if want("face"):
    # the face alone, large: the redesign, the original and the redesigned Amihan from the same
    # three cameras, each aimed at its own eye line, so the outlines can be compared corner by corner
    for tag, path, pal, z in (("new", NEW, None, 0.470), ("orig", OLD, old_palette(), 0.482), ("amihan", AMIHAN, None, 0.475),
                              ("cheska", CHESKA, None, 0.475)):
        scene(); put(path, 0, 0, pal)
        for vname, az in FACE_VIEWS:
            camera((0.0, 0.0, z), 6.0, lens=420, elev=3, azim=az, res=(900, 760))
            render(f"{OUT}/faces_{tag}_{vname}.png")
    # and the whole head with its bun, the redesign beside the original
    for tag, path, pal in (("new", NEW, None), ("orig", OLD, old_palette())):
        scene(); put(path, 0, 0, pal)
        for vname, az, el in (("front", 0, 6), ("right34", -35, 8), ("left", 90, 6), ("back", 180, 8), ("above", -20, 50)):
            camera((0.0, 0.04, 0.54), 6.0, lens=215, elev=el, azim=az, res=(800, 760))
            render(f"{OUT}/head_{tag}_{vname}.png")

if want("pair"):
    # her between the original and the redesigned Amihan, front and back
    scene(); put(OLD, -0.86, 0, old_palette()); put(NEW, 0.0); put(AMIHAN, 0.82)
    camera((0.0, 0, 0.40), 13.0, lens=165, elev=5, azim=-14, res=(1900, 860))
    render(f"{OUT}/pair.png")
    scene(); put(OLD, -0.86, 180, old_palette()); put(NEW, 0.0, 180); put(AMIHAN, 0.82, 180)
    camera((0.0, 0, 0.40), 13.0, lens=165, elev=5, azim=-14, res=(1900, 860))
    render(f"{OUT}/pair_back.png")

# THE TEST (the brief, every round): her in a row with three redesigned heroes (Amihan, Cheska,
# Dante) AND the three approved Classic redesigns (Bebang, Bayan, Lola Pacing), from the front,
# three-quarter and behind. She must belong in that row and must not be a copy of anyone in it.
if want("row"):
    for tag, turn, az in (("front", 0, 0), ("34", 0, -32), ("back", 180, 0)):
        scene()
        for i, path in enumerate(ROW):
            put(path, -2.34 + 0.78 * i, turn)
        camera((0.0, 0, 0.36), 13.0, lens=84, elev=5, azim=az, res=(3000, 700))
        render(f"{OUT}/row_{tag}.png")
    # her nearest neighbours larger: the two girls either side of her
    for tag, turn, az in (("front", 0, 0), ("34", 0, -32), ("back", 180, 0)):
        scene()
        for i, path in enumerate((CHESKA, NEW, BEBANG, AMIHAN)):
            put(path, -1.11 + 0.74 * i, turn)
        camera((0.0, 0, 0.36), 13.0, lens=150, elev=5, azim=az, res=(1900, 760))
        render(f"{OUT}/near_{tag}.png")
    # and the seven heads alone, close
    scene()
    for i, path in enumerate(ROW):
        put(path, -1.80 + 0.60 * i, 0)
    camera((0.0, 0, 0.50), 13.0, lens=118, elev=3, azim=0, res=(3000, 440))
    render(f"{OUT}/row_heads.png")

if want("far"):
    scene(); put(OLD, -0.80, 0, old_palette()); put(NEW, 0.0); put(AMIHAN, 0.80)
    camera((0, 0, 0.47), 13.0, lens=170, elev=5, azim=-18, res=(700, 330))
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

# name -> (target, dist, lens, elev, azim), in the idle's first frame
CLOSE = {
    "head_front": ((0.0, 0.04, 0.53), 6.0, 250, 6, 0),
    "head_left": ((0.0, 0.04, 0.53), 6.0, 250, 6, 80),
    "head_right": ((0.0, 0.04, 0.53), 6.0, 250, 6, -80),
    "head_above": ((0.0, 0.04, 0.55), 6.0, 250, 50, -18),
    "hair_back": ((0.0, 0.1, 0.52), 6.0, 250, 6, 180),
    "hair_rear34": ((0.0, 0.05, 0.52), 6.0, 250, 10, 140),
    "chest": ((0.0, 0.0, 0.27), 6.0, 420, 4, 0),
    "neck": ((0.0, 0.0, 0.33), 6.0, 520, 6, -18),
    "waist": ((0.0, 0.0, 0.20), 6.0, 420, 6, 0),
    "waist34": ((0.0, 0.0, 0.20), 6.0, 420, 10, 35),
    "hip_right": ((0.0, 0.0, 0.19), 6.0, 420, 8, -80),
    "back": ((0.0, 0.0, 0.20), 6.0, 300, 4, 180),
    "feet": ((0.0, 0.0, 0.07), 6.0, 380, 14, -25),
    "feet_back": ((0.0, 0.0, 0.07), 6.0, 380, 14, 155),
    "side_left": ((0.0, 0.0, 0.47), 9.0, 190, 4, 90),
    "side_right": ((0.0, 0.0, 0.47), 9.0, 190, 4, -90),
    "above": ((0.0, 0.0, 0.50), 9.0, 220, 55, -20),
    "below": ((0.0, 0.0, 0.30), 6.0, 240, -28, -25),
}
if want("close"):
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in CLOSE.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        bpy.data.objects["Plane"].hide_render = el < 0     # the camera is under the floor
        render(f"{OUT}/close_{name}.png")

# THE HANDS, in the rest pose (arms straight out, as the rig binds them), so every side of the
# fist block and the forearm can be seen: front, back, above, below and end on. The game cuts
# first-person arms from this mesh and sizes them by that fist.
if want("hands"):
    scene(); put(NEW, pose=None)
    for side, s in (("left", 1), ("right", -1)):
        for vname, az, el in (("front", 12 * s, 8), ("back", 180 - 12 * s, 8), ("above", 10 * s, 62), ("below", 10 * s, -50), ("end", 78 * s, 6)):
            camera((s * 0.235, 0.006, 0.288), 6.0, lens=620, elev=el, azim=az, res=(700, 620))
            bpy.data.objects["Plane"].hide_render = el < 0     # the camera is under the floor
            render(f"{OUT}/hand_{side}_{vname}.png")
    bpy.data.objects["Plane"].hide_render = False
    camera((0, 0, 0.40), 9.0, lens=150, elev=4, azim=0, res=(800, 700))
    render(f"{OUT}/rest_front.png")
    camera((0, 0, 0.3), 9.0, lens=150, elev=80, azim=0, res=(800, 700))
    render(f"{OUT}/rest_top.png")
    # and as they hang in the idle's first frame
    scene(); put(NEW)
    for side, s in (("left", 1), ("right", -1)):
        for vname, az in (("front", 10 * s), ("back", 180 - 10 * s), ("out", 80 * s)):
            camera((s * 0.20, -0.02, 0.17), 6.0, lens=520, elev=8, azim=az, res=(700, 620))
            render(f"{OUT}/handidle_{side}_{vname}.png")


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
    LOOKS = (("yaw55", 55, 0), ("yawm55", -55, 0), ("nod22", 0, -22), ("up20", 0, 20), ("yaw55_nod22", 55, -22), ("yawm55_nod22", -55, -22))
    VIEWS = (("front", -10, 8), ("back", 170, 12), ("side", 90, 6), ("top", 150, 48))
    for tag, yaw, pitch in LOOKS:
        head_pose(arm, yaw, pitch)
        for vname, az, el in VIEWS:
            camera((0.0, 0.0, 0.50), 6.0, lens=190, elev=el, azim=az, res=(640, 640))
            render(f"{OUT}/look_{tag}_{vname}.png")
        # the neck alone, close: does the head turn through the collar
        for vname, az, el in (("front", -10, 8), ("back", 170, 12)):
            camera((0.0, 0.0, 0.36), 6.0, lens=440, elev=el, azim=az, res=(640, 520))
            render(f"{OUT}/neck_{tag}_{vname}.png")

if only and "prev" in only:
    # the turnaround beside the version before it (a copy of that .glb, kept by hand beside its atlas)
    prev = args[args.index("--prev") + 1]
    for tag, path in (("new", NEW), ("prev", prev)):
        row(path, (0, 35, 90, 180))
        render(f"{OUT}/prev_{tag}.png")

if only and "jump6" in only:
    # the first six frames of the jump from the front: do the arms read as UP
    scene(); root, arm = put(NEW, pose="jump")
    first, last = arm.animation_data.action.frame_range
    for i in range(6):
        f = first + i * 24.0 / 60.0      # the clip is keyed at 60 a second, the scene runs at 24
        bpy.context.scene.frame_set(int(f), subframe=f - int(f))
        for vname, az in (("front", 0), ("rear", 180)):
            camera((0.0, 0.0, 0.50), 6.0, lens=120, elev=6, azim=az, res=(420, 520))
            render(f"{OUT}/jump6_{i}_{vname}.png")

if want("extremes"):
    # (clip, fraction of its length): the contact and passing frames of the strides, the launch and
    # the held frames of the jump, both ends of the fall; one frame inside each idle stance
    FRAMES = {"walk": (0.25, 0.75, 0.5), "sprint": (0.25, 0.75, 0.5), "jump": (0.06, 0.3, 0.95), "fall": (0.25, 0.75),
              "idle": IDLE_FRAMES}
    VIEWS = (("front", -12, 8), ("side", 90, 6), ("back", 148, 10), ("rear", 180, 8))
    for clip, fracs in FRAMES.items():
        if clips_only and clip not in clips_only:
            continue
        scene(); root, arm = put(NEW, pose=clip)
        first, last = arm.animation_data.action.frame_range
        for fr in fracs:
            f = first + (last - first) * fr
            bpy.context.scene.frame_set(int(f), subframe=f - int(f))
            for vname, az, el in VIEWS:
                camera((0.0, 0.0, 0.40), 6.0, lens=165, elev=el, azim=az, res=(480, 620))
                render(f"{OUT}/ext_{clip}_{int(round(fr * 100)):02d}_{vname}.png")
