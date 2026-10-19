"""Render the review set of the Payo audience v2 villager `elder_woman`, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_payo_audience_v2_elder_woman.py -- v01
    ... -- v01 row face           (only some shots: row face turn look clips seat close)
    py -3 tools/sheet_payo_audience_v2_elder_woman.py v01

Writes single frames into ArtSource/payo/audience/v2/elder_woman/renders/<version>/; the sheet
script joins them into ArtSource/payo/audience/v2/elder_woman/renders/<version>_*.png. A NEW VERSION
NAME EVERY ITERATION, and LOOK at what comes out.

A copy of tools/render_character_redesign_lola_pacing.py rewritten for her. THE TEST (the audience
brief): she stands in a row with three redesigned heroes (Rafi, Cheska, Dante) and three approved
Classics (Bayan, Bebang, Lola Pacing), Lola Pacing, her base, RIGHT BESIDE HER so the finish can be
compared directly (owner, 2026-10-19: "compare the textures to the cast, they look noisy").

It draws the two-band toon ramp and the inverted-hull ink edge, as the Classics' render scripts do.
AN APPROXIMATION: nothing here has been through Unity.
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Quaternion, Vector

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
CR = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign"
OUTLINE = 0.0045
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["v0"]
V = args[0]
only = set(a for a in args[1:] if not a.startswith("--"))
BASE_OUT = REPO + "/ArtSource/payo/audience/v2/elder_woman/renders"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/ArtSource/payo/audience/v2/elder_woman/audience_v2_elder_woman.glb"
HEROES = {"rafi": CR + "/rafi/rafi-redesign.glb", "cheska": CR + "/cheska/cheska-redesign.glb", "dante": CR + "/dante/dante-redesign.glb"}
CLASSICS = {"bayan": CR + "/bayan/bayan-redesign.glb", "bebang": CR + "/bebang/bebang-redesign.glb",
            "lola_pacing": CR + "/lola_pacing/lola_pacing-redesign.glb"}
EYE_Z_NEW = 0.457
MID_Z = 0.36
LEDGE_EDGE = -0.005     # the ledge's front edge (Blender y; she faces -y) in the sit shots


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
    names = list(arm.get("acts", []))
    act = next((bpy.data.actions[n] for n in names if n == pose or n.startswith(pose + ".")
                or n.startswith(pose + "_")), None)
    if act is None:
        act = next((bpy.data.actions[n] for n in names if n.split(".")[0].split("_")[0] == pose), None)
    if act is None: raise SystemExit("no action %s in %s" % (pose, names))
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


def toon_flat(colour, name):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); dif.inputs[0].default_value = (1, 1, 1, 1)
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.10; ramp.color_ramp.elements[0].color = (colour[0]*0.6, colour[1]*0.6, colour[2]*0.7, 1)
    ramp.color_ramp.elements[1].position = 0.16; ramp.color_ramp.elements[1].color = (*colour, 1)
    em = nt.nodes.new("ShaderNodeEmission"); out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs[0], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
    return m


def ground(z=0.0, colour=(0.20, 0.205, 0.235)):
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, z))
    g = bpy.context.active_object
    g.data.materials.append(toon_flat(colour, "ground"))
    return g


def ledge(height=0.40, colour=(0.42, 0.40, 0.36)):
    """A stone ledge whose top is z 0, its front edge at LEDGE_EDGE, the floor `height` below."""
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    b = bpy.context.active_object
    b.scale = (1.4, 0.6, height)
    b.location = (0.0, LEDGE_EDGE + 0.3, -height / 2.0)
    b.data.materials.append(toon_flat(colour, "ledge"))
    return b


def render(path):
    sc = bpy.context.scene
    sc.render.filepath = path; sc.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    print("WROTE", path)


def want(k):
    return not only or k in only


def scene(z=0.0):
    reset(); lights(); ground(z)


def put(path, x=0.0, rot=0.0, pose="idle", frac=0.0, z=0.0):
    root, arm, _ = import_character(path, pose=pose, frac=frac)
    root.location.x = x; root.location.z = z; root.rotation_euler.z = math.radians(rot)
    bpy.context.view_layer.update()
    return root, arm


# THE ROW: three heroes, two Classics, Lola Pacing (her base) right beside her, and her
ROW = [("rafi", HEROES["rafi"]), ("cheska", HEROES["cheska"]), ("dante", HEROES["dante"]), ("bayan", CLASSICS["bayan"]),
       ("bebang", CLASSICS["bebang"]), ("lola_pacing", CLASSICS["lola_pacing"]), ("ELDER_WOMAN", NEW)]
if want("row"):
    step = 0.62
    for tag, rot in (("row_front", 0), ("row_34", -30), ("row_back", 180)):
        scene()
        for i, (name, path) in enumerate(ROW):
            put(path, (i - 3) * step, rot)
        camera((0.0, 0, 0.40), 16.0, lens=105, elev=5, azim=0, res=(2600, 640))
        render(f"{OUT}/{tag}.png")
    # the crowd distance test: the same row, small
    scene()
    for i, (name, path) in enumerate(ROW):
        put(path, (i - 3) * step, -20)
    camera((0.0, 0, 0.38), 30.0, lens=90, elev=12, azim=0, res=(1400, 360))
    render(f"{OUT}/row_far.png")
    # her beside her base, close, for the finish
    for tag, rot in (("pair_front", 0), ("pair_34", -30), ("pair_back", 180)):
        scene(); put(CLASSICS["lola_pacing"], -0.36, rot); put(NEW, 0.36, rot)
        camera((0.0, 0, 0.36), 9.0, lens=150, elev=5, azim=0, res=(1300, 900))
        render(f"{OUT}/{tag}.png")

if want("face"):
    for tag, path, z in (("new", NEW, EYE_Z_NEW), ("rafi", HEROES["rafi"], 0.475), ("cheska", HEROES["cheska"], 0.475),
                         ("lola", CLASSICS["lola_pacing"], 0.465)):
        scene(); put(path)
        for vname, az in (("front", 0), ("right34", -32)):
            camera((0.0, 0.0, z + 0.02), 6.0, lens=300, elev=3, azim=az, res=(900, 900))
            render(f"{OUT}/faces_{tag}_{vname}.png")

if want("turn"):
    scene()
    for i, rot in enumerate((0, 45, 90, 135, 180, -135, -90, -45)):
        put(NEW, (i - 3.5) * 0.62, rot)
    camera((0, 0, MID_Z), 16.0, lens=120, elev=5, azim=0, res=(2600, 640))
    render(f"{OUT}/turn.png")
    scene(); put(NEW)
    for vname, az, el in (("head_front", 0, 6), ("head_34", -35, 8), ("head_side", 90, 6), ("head_back", 180, 8),
                          ("head_rear34", 145, 12), ("head_above", -20, 50)):
        camera((0.0, 0.0, 0.50), 6.0, lens=220, elev=el, azim=az, res=(800, 760))
        render(f"{OUT}/{vname}.png")


def head_pose(arm, yaw, pitch):
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = Quaternion(arm["head_base"]) @ (rest.inverted() @ world @ rest)
    bpy.context.view_layer.update()


if want("look"):
    scene(); root, arm = put(NEW)
    arm.animation_data.action = None
    arm["head_base"] = list(arm.pose.bones["head"].rotation_quaternion)
    for tag, yaw, pitch in (("yaw55", 55, 0), ("yawm55", -55, 0), ("nod22", 0, -22), ("yaw55_nod22", 55, -22)):
        head_pose(arm, yaw, pitch)
        for vname, az, el in (("front", -10, 8), ("back", 170, 12)):
            camera((0.0, 0.0, 0.44), 6.0, lens=200, elev=el, azim=az, res=(640, 640))
            render(f"{OUT}/look_{tag}_{vname}.png")

#   clip -> (fractions, views); the sit is on a ledge block
CLIPS = {"idle": ((0.0, 0.18, 0.36, 0.50, 0.66, 0.85), (("front", -12, 8), ("side", 90, 6), ("back", 150, 10))),
         "cheer": ((0.0, 0.25, 0.5), (("front", -12, 8), ("side", 90, 6))),
         "clap": ((0.0, 0.15, 0.40), (("front", -12, 8), ("side", 90, 6))),
         "sit": ((0.0, 0.55), (("front", -20, 8), ("side", 90, 6), ("back", 150, 12))),
         "walk": ((0.25, 0.75), (("front", -20, 8), ("side", 90, 6))),
         "sprint": ((0.25,), (("front", -20, 8), ("side", 90, 6))),
         "jump": ((0.3,), (("front", -20, 8),)),
         "fall": ((0.25,), (("front", -20, 8),))}
if want("clips"):
    for clip, (fracs, views) in CLIPS.items():
        for fr in fracs:
            if clip == "sit":
                scene(-0.40); ledge()
            else:
                scene()
            root, arm = put(NEW, pose=clip, frac=fr)
            for vname, az, el in views:
                camera((0.0, -0.03, 0.14 if clip == "sit" else 0.34), 7.0, lens=290, elev=el, azim=az, res=(560, 640))
                render(f"{OUT}/clip_{clip}_{int(round(fr * 100)):02d}_{vname}.png")

if want("seat"):
    # WHERE THE SEAT IS: in the sit, the lowest point of her body behind the ledge's edge, and the basket's foot
    scene(-0.40); ledge()
    root, arm, meshes = import_character(NEW, pose="sit", frac=0.0)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    for o in meshes:
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        pts = [ev.matrix_world @ v.co for v in me.vertices]
        if not pts:
            continue
        behind = [p for p in pts if p.y > LEDGE_EDGE]
        print("SEAT %s  all z %.4f..%.4f  y %.4f..%.4f;  behind the edge lowest z %.4f (at y %.4f, x %.4f)" % (
            o.name, min(p.z for p in pts), max(p.z for p in pts), min(p.y for p in pts), max(p.y for p in pts),
            min(p.z for p in behind) if behind else 9, min(behind, key=lambda p: p.z).y if behind else 9,
            min(behind, key=lambda p: p.z).x if behind else 9))
        ev.to_mesh_clear()
    for vname, az, el in (("side", 90, 4), ("front", 0, 4), ("below", 60, -15)):
        camera((0.0, 0.0, 0.10), 6.0, lens=200, elev=el, azim=az, res=(700, 640))
        render(f"{OUT}/seat_{vname}.png")

if want("close"):
    scene(); put(NEW)
    for name, (t, d, lens, el, az) in {"chest": ((0, 0, 0.28), 6.0, 380, 4, 0), "chest34": ((0, 0, 0.28), 6.0, 380, 6, -35),
                                       "back": ((0, 0, 0.26), 6.0, 300, 4, 180), "skirt": ((0, 0, 0.13), 6.0, 380, 6, -15),
                                       "feet": ((0.1, 0, 0.06), 6.0, 300, 14, -25), "basket": ((0.31, 0, 0.09), 6.0, 380, 12, -25)}.items():
        camera(t, d, lens=lens, elev=el, azim=az, res=(800, 700))
        render(f"{OUT}/close_{name}.png")
