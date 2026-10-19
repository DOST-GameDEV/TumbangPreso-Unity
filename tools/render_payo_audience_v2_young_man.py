"""Render the review set of the Payo audience villager `young_man`, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_payo_audience_v2_young_man.py -- v01
    ... -- v01 row face          (only some shots: row face turn clips look seat far)
    py -3 tools/sheet_payo_audience_v2_young_man.py v01

Writes single frames into ArtSource/payo/audience/v2/young_man/renders/<version>/; the sheet script
stacks and labels them into renders/<version>_*.png. A NEW VERSION NAME EVERY ITERATION, and LOOK.

A copy of tools/render_character_redesign_kuya_boy.py (the base Classic's) rewritten for him: the
same toon ramp and inverted-hull ink edge, an approximation of the game's look for judging shape,
paint and silhouette. There is no "original" for him (v1 was a rejected box figure), so the
palette-baking of the stock rig is gone.

Shots:
  row     THE TEST (payo-audience-brief): him in a row with three redesigned heroes (Dante, Rafi,
          Cheska) and the three approved Classics (Bayan, Bebang, Lola Pacing), and KUYA BOY, his
          base Classic, right beside him so the finish can be compared (owner note 2026-10-19).
          Front, three-quarter, back.
  far     the same row at crowd distance, and as silhouettes
  face    his face beside Rafi's, Dante's and Kuya Boy's, front and both three-quarters
  turn    his turnaround
  clips   one still each of idle (several stances), cheer, clap and sit (on a ledge block)
  look    the head bone turned 55 degrees each way and nodded 22
  seat    prints where the seat is in the sit clip, relative to the root's origin
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Quaternion, Vector

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
OUTLINE = 0.0045  # ToonSkin.PersonOutlineWidth in model space
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["v0"]
V = args[0]
only = set(a for a in args[1:] if not a.startswith("--"))
BASE_OUT = REPO + "/ArtSource/payo/audience/v2/young_man/renders"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/ArtSource/payo/audience/v2/young_man/audience_v2_young_man.glb"
CR = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign"
DANTE = CR + "/dante/dante-redesign.glb"
RAFI = CR + "/rafi/rafi-redesign.glb"
CHESKA = CR + "/cheska/cheska-redesign.glb"
BAYAN = CR + "/bayan/bayan-redesign.glb"
BEBANG = CR + "/bebang/bebang-redesign.glb"
LOLA = CR + "/lola_pacing/lola_pacing-redesign.glb"
KUYA = CR + "/kuya_boy/kuya_boy-redesign.glb"     # his base Classic, beside him in every row


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


def flat_material(name, colour):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); dif.inputs[0].default_value = (1, 1, 1, 1)
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.10; ramp.color_ramp.elements[0].color = (colour[0] * 0.6, colour[1] * 0.6, colour[2] * 0.7, 1)
    ramp.color_ramp.elements[1].position = 0.16; ramp.color_ramp.elements[1].color = (*colour, 1)
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


def import_character(path, pose="idle", frac=0.0):
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
    set_pose(arm, pose, frac)
    root = bpy.data.objects.new("holder-" + arm.name, None); bpy.context.scene.collection.objects.link(root)
    arm.parent = root
    return root, arm, meshes


def set_pose(arm, pose, frac=0.0):
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
    first, last = act.frame_range
    f = first + (last - first) * frac
    bpy.context.scene.frame_set(int(f), subframe=f - int(f))


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


def ledge(top, front_y, colour=(0.46, 0.43, 0.38)):
    """A stone ledge block, its top at `top` and its front face at `front_y` (he faces -y)."""
    depth = 0.6
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, front_y + depth / 2, top / 2))
    b = bpy.context.active_object
    b.scale = (1.4, depth, top)
    b.data.materials.append(flat_material("ledge", colour))
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


def put(path, x=0.0, rot=0.0, pose="idle", frac=0.0, z=0.0):
    root, arm, meshes = import_character(path, pose, frac)
    root.location = (x, 0.0, z); root.rotation_euler.z = math.radians(rot)
    return root, arm, meshes


ROW = (DANTE, BAYAN, RAFI, KUYA, NEW, BEBANG, CHESKA, LOLA)
if want("row"):
    for tag, rot, az in (("row_front", 0, 0), ("row_34", 0, -28), ("row_back", 180, 0)):
        scene()
        order = ROW if rot == 0 else ROW[::-1]
        for i, path in enumerate(order):
            put(path, -2.52 + 0.72 * i, rot)
        camera((0.0, 0, 0.40), 18.0, lens=118, elev=5, azim=az, res=(2900, 760))
        render(f"{OUT}/{tag}.png")

if want("far"):
    scene()
    for i, path in enumerate(ROW):
        put(path, -2.52 + 0.72 * i, 0)
    camera((0.0, 0, 0.40), 26.0, lens=60, elev=18, azim=-10, res=(900, 300))
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

FACE_VIEWS = (("front", 0), ("right34", -32), ("left34", 32))
if want("face"):
    for tag, path, z in (("new", NEW, 0.475), ("rafi", RAFI, 0.475), ("dante", DANTE, 0.475), ("kuya", KUYA, 0.475)):
        scene(); put(path)
        for vname, az in FACE_VIEWS:
            camera((0.0, 0.0, z), 6.0, lens=330, elev=3, azim=az, res=(900, 900))
            render(f"{OUT}/faces_{tag}_{vname}.png")
    scene(); put(NEW)
    for vname, az, el in (("front", 0, 6), ("right34", -35, 8), ("left", 90, 6), ("back", 180, 8), ("above", -20, 50)):
        camera((0.0, 0.0, 0.52), 6.0, lens=230, elev=el, azim=az, res=(800, 800))
        render(f"{OUT}/head_{vname}.png")

if want("turn"):
    for tag, rots in (("turn", (0, 35, 90, 180)), ("turn2", (-35, -90, 145, -145))):
        scene()
        for i, rot in enumerate(rots):
            put(NEW, -1.5 * 0.82 + 0.82 * i, rot)
        camera((0, 0, 0.38), 13.0, lens=135, elev=5, azim=0, res=(1920, 800))
        render(f"{OUT}/{tag}.png")
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in {"chest": ((0, 0, 0.27), 6.0, 380, 4, 0), "back": ((0, 0, 0.25), 6.0, 300, 4, 180),
                                       "waist": ((0, 0, 0.16), 6.0, 380, 6, -20), "feet": ((0, 0, 0.06), 6.0, 380, 14, -25),
                                       "side": ((0, 0, 0.38), 9.0, 200, 4, 90), "above": ((0, 0, 0.42), 9.0, 230, 55, -20)}.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 800))
        render(f"{OUT}/close_{name}.png")

#   (clip, fraction of its length, label) for the stills
CLIP_FRAMES = [("idle", 0.0, "idle_fold_right"), ("idle", 0.24, "idle_lean_in"), ("idle", 0.432, "idle_pump"),
               ("idle", 0.45, "idle_pull"), ("idle", 0.775, "idle_lean_left"), ("idle", 0.883, "idle_low_pump"),
               ("cheer", 0.5, "cheer_top"), ("cheer", 0.0, "cheer_land"), ("clap", 0.22, "clap_shut"), ("clap", 0.5, "clap_open"),
               ("sit", 0.0, "sit"), ("sit", 0.5, "sit_half")]
LEDGE_TOP = 0.32
LEDGE_FRONT = -0.02
if want("clips"):
    for clip, frac, label in CLIP_FRAMES:
        scene()
        z = 0.0
        if clip == "sit":
            z = LEDGE_TOP
            ledge(LEDGE_TOP, LEDGE_FRONT)
        put(NEW, pose=clip, frac=frac, z=z)
        for vname, az, el in (("front", -14, 8), ("side", 75, 6), ("back", 160, 10)):
            camera((0.0, 0.0, z + (0.22 if clip == "sit" else 0.38)), 6.0, lens=200, elev=el, azim=az, res=(700, 760))
            render(f"{OUT}/clip_{label}_{vname}.png")


def head_pose(arm, yaw, pitch):
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = Quaternion(arm["head_base"]) @ (rest.inverted() @ world @ rest)
    bpy.context.view_layer.update()


if want("look"):
    scene(); root, arm, _ = put(NEW, pose="clap", frac=0.5)
    arm.animation_data.action = None
    arm["head_base"] = list(arm.pose.bones["head"].rotation_quaternion)
    for tag, yaw, pitch in (("yaw55", 55, 0), ("yawm55", -55, 0), ("nod22", 0, -22), ("yaw55_nod22", 55, -22)):
        head_pose(arm, yaw, pitch)
        for vname, az, el in (("front", -10, 8), ("back", 170, 12)):
            camera((0.0, 0.0, 0.42), 6.0, lens=210, elev=el, azim=az, res=(640, 640))
            render(f"{OUT}/look_{tag}_{vname}.png")

if want("seat"):
    # WHERE IS THE SEAT. The sit clip at its first frame with the root at the origin: the lowest
    # point of the hips (vertices that start between z 0.10 and 0.20, the bahag's seat and the
    # tops of the thighs) in world space, and how far forward of the origin the legs drop below it.
    scene(); root, arm, meshes = put(NEW, pose="sit", frac=0.0)
    dg = bpy.context.evaluated_depsgraph_get()
    hips, legs, flap = [], [], []
    for o in meshes:
        ev = o.evaluated_get(dg)
        mesh = ev.to_mesh()
        rest = o.data.vertices
        for v, r in zip(mesh.vertices, rest):
            w = ev.matrix_world @ v.co
            # rest y: the body is within 0.075 of the middle; the flaps and the bag stand off it
            if 0.10 <= r.co.z <= 0.20 and abs(r.co.x) < 0.15 and abs(r.co.y) < 0.075:
                hips.append(w)
            if 0.08 <= r.co.z <= 0.20 and r.co.y > 0.085:
                flap.append(w)
            if r.co.z < 0.20:
                legs.append(w)
        ev.to_mesh_clear()
    low = min(hips, key=lambda p: p.z)
    print("SEAT lowest hip point z %.4f at y %.4f x %.4f" % (low.z, low.y, low.x))
    lowf = min(flap, key=lambda p: p.z)
    print("SEAT the back flap's lowest point z %.4f at y %.4f" % (lowf.z, lowf.y))
    behind = [p for p in legs if p.y > LEDGE_FRONT]
    print("SEAT lowest point behind the ledge front (y > %.3f): z %.4f" % (LEDGE_FRONT, min(p.z for p in behind)))
    for y0 in (0.0, -0.02, -0.04, -0.06):
        sel = [p.z for p in legs if p.y > y0]
        print("   lowest with y > %.2f: %.4f" % (y0, min(sel)))
