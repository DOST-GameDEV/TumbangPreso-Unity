"""Render the review set of the Payo audience v2 GIRL, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_payo_audience_v2_girl.py -- v01
    ... -- v01 row face          (only some shots: row face turn clips look far motion)
    py -3 tools/sheet_payo_audience_v2_girl.py v01

Writes single frames into ArtSource/payo/audience/v2/girl/renders/<version>/; the sheet script
stacks and labels them into renders/<version>_*.png. A NEW VERSION NAME EVERY ITERATION.

A copy of tools/render_character_redesign_bebang.py's look (the two-band toon ramp of
`TumbangPreso/Toon`, the inverted-hull ink edge at ToonSkin.PersonOutlineWidth), rewritten for the
audience brief's test: the girl in a row with three redesigned heroes (Rafi, Dante, Cheska) and
the three approved Classics (Bebang, her base, RIGHT BESIDE HER so the finish compares directly,
then Bayan and Lola Pacing). AN APPROXIMATION: nothing here has been through Unity.

Shots:
  row     the row, front, three-quarter and back, and the same row seen small (lineup distance)
  face    her face beside Cheska's and Rafi's, front and three-quarter
  turn    the turnaround
  clips   one still each of idle (its stances), cheer, clap and sit (on a ledge block)
  look    the head turned 55 and nodded 22, front and back
  motion  walk, jump and fall, a frame each
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Quaternion, Vector

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
OUTLINE = 0.0045
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["v0"]
V = args[0]
only = set(a for a in args[1:] if not a.startswith("--"))
BASE_OUT = REPO + "/ArtSource/payo/audience/v2/girl/renders"
OUT = BASE_OUT + "/" + V
os.makedirs(OUT, exist_ok=True)
NEW = REPO + "/ArtSource/payo/audience/v2/girl/audience_v2_girl.glb"
CAST = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign"
RAFI, DANTE, CHESKA = (CAST + "/%s/%s-redesign.glb" % (n, n) for n in ("rafi", "dante", "cheska"))
BEBANG, BAYAN, LOLA = (CAST + "/%s/%s-redesign.glb" % (n, n) for n in ("bebang", "bayan", "lola_pacing"))
# the row: three heroes, the girl, Bebang (her base) beside her, then the other two Classics
ROW = [("Rafi", RAFI), ("Dante", DANTE), ("Cheska", CHESKA), ("GIRL", NEW), ("Bebang (base)", BEBANG), ("Bayan", BAYAN),
       ("Lola Pacing", LOLA)]


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
    ramp.color_ramp.elements[0].position = 0.10; ramp.color_ramp.elements[0].color = (colour[0] * 0.62, colour[1] * 0.62, colour[2] * 0.70, 1)
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


def set_pose(arm, pose, frac=0.0):
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


def put(path, x=0.0, rot=0.0, pose="idle", frac=0.0, y=0.0, z=0.0):
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
    root.location = (x, y, z); root.rotation_euler.z = math.radians(rot)
    return root, arm


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


def ground():
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, 0))
    g = bpy.context.active_object
    g.data.materials.append(flat_material("ground", (0.20, 0.205, 0.235)))


def ledge(top, front_y, x=0.0, width=0.6, depth=0.5):
    """A stone ledge block, its top at `top`, its front face at `front_y` (forward is -y)."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x, front_y + 0.5 * depth, 0.5 * top))
    b = bpy.context.active_object
    b.scale = (width, depth, top)
    b.data.materials.append(flat_material("stone", (0.52, 0.50, 0.46)))
    add_outline(b, 0.003)
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


if want("row"):
    step = 0.70
    for tag, turn, az in (("front", 0, 0), ("34", 0, -32), ("back", 180, 0)):
        scene()
        for i, (_label, path) in enumerate(ROW):
            put(path, (i - 3) * step, turn)
        camera((0.0, 0, 0.36), 16.0, lens=128, elev=5, azim=az, res=(2400, 720))
        render(f"{OUT}/row_{tag}.png")
    # the same row seen SMALL, the way a crowd figure is seen: silhouette and colour masses first
    scene()
    for i, (_label, path) in enumerate(ROW):
        put(path, (i - 3) * step, 0)
    camera((0.0, 0, 0.36), 16.0, lens=128, elev=12, azim=-20, res=(600, 180))
    render(f"{OUT}/row_small.png")
    # and her beside Bebang, her base, close: the finish compared directly
    scene()
    put(BEBANG, -0.36, 0); put(NEW, 0.36, 0)
    camera((0.0, 0, 0.36), 12.0, lens=190, elev=5, azim=-14, res=(1400, 900))
    render(f"{OUT}/base_pair.png")
    scene()
    put(BEBANG, -0.36, 180); put(NEW, 0.36, 180)
    camera((0.0, 0, 0.36), 12.0, lens=190, elev=5, azim=14, res=(1400, 900))
    render(f"{OUT}/base_pair_back.png")

if want("face"):
    for tag, path, z in (("new", NEW, 0.458), ("cheska", CHESKA, 0.475), ("rafi", RAFI, 0.475), ("bebang", BEBANG, 0.475)):
        scene(); put(path)
        for vname, az in (("front", 0), ("right34", -32)):
            camera((0.0, 0.0, z + 0.04), 6.0, lens=300, elev=3, azim=az, res=(900, 900))
            render(f"{OUT}/faces_{tag}_{vname}.png")

if want("turn"):
    for name, rots in (("turn", (0, 35, 90, 180)), ("turn2", (-35, -90, 145, -145))):
        scene()
        for i, rot in enumerate(rots):
            put(NEW, -1.5 * 0.70 + 0.70 * i, rot)
        camera((0, 0, 0.36), 13.0, lens=135, elev=5, azim=0, res=(1920, 760))
        render(f"{OUT}/{name}.png")

# (clip, fraction of its length, tag)
CLIP_FRAMES = [("idle", 0.0, "idle_hold"), ("idle", 0.16, "idle_rock"), ("idle", 0.50, "idle_peek"), ("idle", 0.745, "idle_chin"),
               ("cheer", 0.125, "cheer_top"), ("cheer", 0.375, "cheer_land"), ("clap", 0.0, "clap_shut"), ("clap", 0.167, "clap_open"),
               ("sit", 0.0, "sit_lap"), ("sit", 0.541, "sit_clap")]
LEDGE_TOP = 0.30
LEDGE_FRONT = -0.045    # the ledge's front face, this far ahead of the root's origin (forward is -y)
if want("clips"):
    for clip, frac, tag in CLIP_FRAMES:
        scene()
        z = 0.0
        if clip == "sit":
            ledge(LEDGE_TOP, LEDGE_FRONT)
            z = LEDGE_TOP
        put(NEW, pose=clip, frac=frac, z=z)
        for vname, az, el in (("front", -14, 8), ("side", 90, 6), ("back", 160, 10)):
            camera((0.0, 0.0, 0.36 + z * 0.8), 7.0, lens=150 if clip != "sit" else 120, elev=el, azim=az, res=(560, 700))
            render(f"{OUT}/clip_{tag}_{vname}.png")


def head_pose(arm, yaw, pitch):
    pb = arm.pose.bones["head"]
    pb.rotation_mode = "QUATERNION"
    rest = pb.bone.matrix_local.to_quaternion()
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = Quaternion(arm["head_base"]) @ (rest.inverted() @ world @ rest)
    bpy.context.view_layer.update()


if want("look"):
    scene(); root, arm = put(NEW, pose="walk")
    arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.scale = (1, 1, 1)
    # arms down for the look test
    arm["head_base"] = [1.0, 0.0, 0.0, 0.0]
    for tag, yaw, pitch in (("yaw55", 55, 0), ("yawm55_nod22", -55, -22), ("nod22", 0, -22), ("up20", 0, 20)):
        head_pose(arm, yaw, pitch)
        for vname, az, el in (("front", -10, 8), ("back", 170, 12)):
            camera((0.0, 0.0, 0.45), 6.0, lens=170, elev=el, azim=az, res=(600, 600))
            render(f"{OUT}/look_{tag}_{vname}.png")

if want("motion"):
    for clip, frac in (("walk", 0.25), ("walk", 0.75), ("sprint", 0.25), ("jump", 0.3), ("fall", 0.25)):
        scene(); put(NEW, pose=clip, frac=frac)
        for vname, az, el in (("front", -12, 8), ("side", 90, 6)):
            camera((0.0, 0.0, 0.40), 6.0, lens=165, elev=el, azim=az, res=(480, 620))
            render(f"{OUT}/motion_{clip}_{int(round(frac * 100)):02d}_{vname}.png")
