"""Render the review set of the Dante (displayed: Basilio) redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_dante.py -- v12
    ... -- v12 turn face          (only some shots: turn face far trio close golem heads)
    py -3 tools/sheet_character_redesign_dante.py v12

Writes single frames into Logs/character-redesign-dante/<version>/; the sheet script stacks and
labels them into the review sheets beside that folder. A NEW VERSION NAME EVERY ITERATION
(docs/CHARACTER_MODEL_METHOD.md section 5), and LOOK at what comes out before believing it.

It draws the two prototype .glb files written by tools/author_character_redesign_dante.py (A is
the block hair, B the lock hair) beside the original team-dante.glb and Paete's team-paete.glb,
all in the rig's `idle` pose, the way the game draws them as near as EEVEE allows:
  * the two-band toon ramp of `TumbangPreso/Toon` (shadow band 0.45, a hard step), a warm lit
    band and a cool shadow band, one low sun;
  * the inverted-hull ink edge at `ToonSkin.PersonOutlineWidth` (0.0045 in model space), on a
    welded copy of each mesh, as `OutlineNormals.Weld` does in the game;
  * the palette remap of `Toon.shader` baked into pixels for the two palette models (UV rows 0 to
    7 take the roster entry's 16 colours, the rest the texture).
⚠️ IT IS AN APPROXIMATION. Nothing here has been through Unity; the real shader, tonemap, fog and
world look are not reproduced. It is for judging shape, paint and silhouette, not final colour.

The `heads` shot needs the three head carvings built first, beside a copy of the atlas:
    mkdir -p Logs/character-redesign-dante/variants
    cp Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign-atlas.png Logs/character-redesign-dante/variants/
    for h in block carved shaped; do blender -b --python tools/author_character_redesign_dante.py -- --head $h --out Logs/character-redesign-dante/variants/head_$h.glb; done
"""
import math
import os
import re
import sys

import bpy
import bmesh
import numpy as np
from mathutils import Vector

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
PERSONS = REPO + "/Assets/TumbangPreso/Art/characters/persons"
OUTLINE = 0.0045  # ToonSkin.PersonOutlineWidth in model space
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["v0"]
V = args[0]
only = set(args[1:])
BASE_OUT = REPO + "/Logs/character-redesign-dante"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
D = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/"
GLB = {"A": D + "dante-redesign.glb", "B": D + "dante-redesign-spiky.glb"}
OLD = PERSONS + "/team-dante.glb"
GOLEM = PERSONS + "/team-paete.glb"


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


def srgb2lin(c):
    c = np.asarray(c, dtype=np.float32)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


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


def flat_material(name, image):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = image
    em = nt.nodes.new("ShaderNodeEmission"); out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(tex.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
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
    mat = toon_material("toon-" + arm.name, img) if toon else flat_material("flat-" + arm.name, img)
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
    # a warm LOW sun from the upper left of the camera; the ramp carries the two tints
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


def put(path, x=0.0, rot=0.0, palette=None):
    root, arm, _ = import_character(path, palette, pose="idle")
    root.location.x = x; root.rotation_euler.z = math.radians(rot)


if want("turn"):
    for k in "AB":
        scene()
        for i, rot in enumerate((0, 35, 90, 180)):
            put(GLB[k], -1.05 + 0.70 * i, rot)
        camera((0, 0, 0.40), 13.0, lens=160, elev=5, azim=0, res=(1920, 700))
        render(f"{OUT}/turn_{k}.png")

if want("face"):
    for k in "AB":
        scene(); put(GLB[k])
        camera((0.02, 0, 0.555), 6.0, lens=200, elev=2, azim=0, res=(900, 800))
        render(f"{OUT}/face_{k}.png")
        camera((0.0, 0, 0.53), 6.0, lens=170, elev=4, azim=-32, res=(900, 800))
        render(f"{OUT}/face34_{k}.png")

if want("far"):
    scene(); put(OLD, -0.62, 0, roster_palette("person_dante.asset")); put(GLB["A"], 0.0); put(GLB["B"], 0.62)
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

if want("trio"):
    scene(); put(OLD, -0.62, 0, roster_palette("person_dante.asset")); put(GLB["A"], 0.0); put(GLB["B"], 0.62)
    camera((0, 0, 0.40), 13.0, lens=200, elev=5, azim=-22, res=(1800, 900))
    render(f"{OUT}/trio.png")

# name -> (target, dist, lens, elev, azim)
CLOSE = {
    "hair_back": ((0.0, 0.1, 0.56), 6.0, 190, 6, 180),
    "hair_above": ((0.0, 0.0, 0.64), 6.0, 190, 38, -18),
    "ear_left": ((0.19, 0.0, 0.47), 6.0, 620, 4, 70),
    "ear_right": ((-0.19, 0.0, 0.47), 6.0, 620, 4, -70),
    "ear_left_front": ((0.17, 0.0, 0.48), 6.0, 620, 4, 12),
    "hand_left_front": ((0.23, 0.0, 0.15), 6.0, 620, 8, 10),
    "hand_left_back": ((0.23, 0.0, 0.15), 6.0, 620, 8, 170),
    "hand_right_front": ((-0.23, 0.0, 0.15), 6.0, 620, 8, -10),
    "hand_right_back": ((-0.23, 0.0, 0.15), 6.0, 620, 8, -170),
    "hem_left": ((0.19, 0.0, 0.15), 6.0, 560, 10, 40),
    "hem_right": ((-0.19, 0.0, 0.15), 6.0, 560, 10, -40),
    "feet": ((0.0, 0.0, 0.07), 6.0, 240, 14, -25),
    "chest": ((0.0, 0.0, 0.27), 6.0, 240, 4, 0),
    "back": ((0.0, 0.0, 0.24), 6.0, 200, 4, 180),
    "side_left": ((0.0, 0.0, 0.40), 9.0, 170, 4, 90),
    "side_right": ((0.0, 0.0, 0.40), 9.0, 170, 4, -90),
    "above": ((0.0, 0.0, 0.45), 9.0, 190, 55, -20),
}
if want("close"):
    for k in "AB":
        scene(); put(GLB[k])
        for name, (t, d, lens, el, az) in CLOSE.items():
            if k == "B" and not (name.startswith("hair") or name.startswith("ear") or name.startswith("side") or name == "above"):
                continue
            camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
            render(f"{OUT}/close_{name}_{k}.png")


if want("golem"):
    scene(); put(OLD, -0.72, 0, roster_palette("person_dante.asset")); put(GLB["A"], -0.05)
    put(GOLEM, 0.86, 0, roster_palette("person_paete.asset"))
    camera((0.1, 0, 0.42), 7.2, lens=85, elev=5, azim=-12, res=(1600, 800))
    render(f"{OUT}/golem.png")

if want("heads") and os.path.exists(BASE_OUT + "/variants/head_carved.glb"):
    xs = (-0.84, -0.28, 0.28, 0.84)
    for tag, rot in (("front", 0), ("threequarter", 30), ("side", 90)):
        scene(); put(OLD, xs[0], rot, roster_palette("person_dante.asset"))
        for x, h in zip(xs[1:], ("block", "carved", "shaped")):
            put(f"{BASE_OUT}/variants/head_{h}.glb", x, rot)
        camera((0, 0, 0.50), 14.0, lens=200, elev=3, azim=0, res=(1920, 620))
        render(f"{OUT}/heads_{tag}.png")
