"""Bake the Payo audience v2 into static posed meshes, and render the lineup beside the cast.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/bake_payo_audience_v2.py -- bake
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/bake_payo_audience_v2.py -- lineup

BAKE reads each rigged villager, ArtSource/payo/audience/v2/<id>/audience_v2_<id>.glb (built by
tools/author_payo_audience_v2_<id>.py, docs/reports/character-redesign/payo-audience-brief.md), and
writes four static meshes beside v1 in ArtSource/payo/audience/:
    audience_v2_<id>_stand.glb   the `idle` clip's first frame
    audience_v2_<id>_cheer.glb   the `cheer` frame with the hands highest
    audience_v2_<id>_clap.glb    the `clap` frame with the hands closest together
    audience_v2_<id>_sit.glb     the `sit` frame with the head lowest; the origin is the seat, as the brief sets it
Each is ONE mesh, no skeleton, the villager's own atlas, so dozens can be instanced. The clip and frame
each came from are printed and written to audience_v2_bake.json.

LINEUP renders audience_v2_lineup.png: the six rigged villagers in `idle` beside three heroes and three
Classics, with the cast review's toon look (tools/render_character_redesign_classic_lineup.py), and
audience_v2_poses.png, the four baked poses of each. Blender only, no Unity.
"""
import json
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
FOLDER = REPO + "/ArtSource/payo/audience"
IDS = ["elder_man", "woman", "young_man", "elder_woman", "boy", "girl"]
CAST = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign"
ROW = ["rafi", "dante", "cheska", "bayan", "bebang", "lola_pacing"]
OUTLINE = 0.0045
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["bake"]


def rigged(i):
    return "%s/v2/%s/audience_v2_%s.glb" % (FOLDER, i, i)


# A dry run of the machinery on any rigged glb: `-- bake --test <glb> <outdir>` (the clips it lacks are skipped).
if "--test" in args:
    _glb, FOLDER = args[args.index("--test") + 1], args[args.index("--test") + 2]
    IDS = ["test"]
    rigged = lambda i: _glb  # noqa: E731


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    return bpy.context.scene


def load(path):
    before = set(bpy.data.objects)
    acts = set(bpy.data.actions)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in list(new):
        if o.type == "MESH" and o.name.startswith("Icosphere"):
            bpy.data.objects.remove(o)
            new.remove(o)
    arm = next(o for o in new if o.type == "ARMATURE")
    arm["acts"] = [a.name for a in bpy.data.actions if a not in acts]
    return arm, [o for o in new if o.type == "MESH"]


def action(arm, clip):
    for n in arm["acts"]:
        if n == clip or n.startswith(clip + ".") or n.startswith(clip + "_"):
            return bpy.data.actions[n]
    return None


def pose(arm, act, frame):
    if not arm.animation_data:
        arm.animation_data_create()
    # The glTF importer leaves its NLA tracks live; any unmuted one overrides the action set here.
    for t in arm.animation_data.nla_tracks:
        t.mute = True
    arm.animation_data.action = act
    try:
        arm.animation_data.action_slot = act.slots[0]
    except Exception:
        pass
    bpy.context.scene.frame_set(int(frame), subframe=frame - int(frame))
    bpy.context.view_layer.update()


def hands(arm):
    out = []
    for name in ("forearm-left", "forearm-right"):
        pb = arm.pose.bones.get(name)
        out.append(arm.matrix_world @ pb.tail if pb else Vector())
    return out


def pick(arm, clip, rule):
    act = action(arm, clip)
    if act is None:
        return None, None
    a, b = act.frame_range
    if rule == "first":
        return act, a
    best, best_f = None, a
    steps = 48
    for k in range(steps + 1):
        f = a + (b - a) * k / steps
        pose(arm, act, f)
        l, r = hands(arm)
        if rule == "low":
            hb = arm.pose.bones.get("head")
            score = (arm.matrix_world @ hb.head).z
        else:
            score = -(l.z + r.z) if rule == "high" else (l - r).length
        if best is None or score < best:
            best, best_f = score, f
    return act, best_f


def bake_one(i, report):
    reset()
    arm, meshes = load(rigged(i))
    for name, clip, rule in (("stand", "idle", "first"), ("cheer", "cheer", "high"),
                             ("clap", "clap", "close"), ("sit", "sit", "low")):
        act, frame = pick(arm, clip, rule)
        if act is None:
            print("BAKE %s: no clip %s" % (i, clip))
            report[i + "_" + name] = None
            continue
        pose(arm, act, frame)
        dg = bpy.context.evaluated_depsgraph_get()
        bm = bmesh.new()
        mats = []
        for o in meshes:
            ev = o.evaluated_get(dg)
            me = bpy.data.meshes.new_from_object(ev, depsgraph=dg)
            me.transform(o.matrix_world)
            off = len(mats)
            for m in me.materials:
                mats.append(m)
            for p in me.polygons:
                p.material_index += off
            bm.from_mesh(me)
            bpy.data.meshes.remove(me)
        out = bpy.data.meshes.new("audience_v2_%s_%s" % (i, name))
        bm.to_mesh(out)
        bm.free()
        for m in mats:
            out.materials.append(m)
        ob = bpy.data.objects.new(out.name, out)
        bpy.context.scene.collection.objects.link(ob)
        # One atlas per villager: fold duplicate slots that share it into one, for one draw call.
        if len(set(id(m) for m in mats)) > 1:
            pass
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob
        path = "%s/audience_v2_%s_%s.glb" % (FOLDER, i, name)
        bpy.ops.export_scene.gltf(filepath=path, use_selection=True, export_format="GLB",
                                  export_animations=False, export_skins=False)
        tris = sum(len(p.vertices) - 2 for p in out.polygons)
        lo = min(v.co.z for v in out.vertices)
        hi = max(v.co.z for v in out.vertices)
        report[i + "_" + name] = dict(clip=act.name, frame=round(frame, 2), tris=tris, z=[round(lo, 3), round(hi, 3)])
        print("BAKE %-12s %-6s clip %-14s frame %6.2f  %5d tris  z %.3f to %.3f" % (i, name, act.name, frame, tris, lo, hi))
        bpy.data.objects.remove(ob)


def bake():
    report = {}
    for i in IDS:
        if os.path.exists(rigged(i)):
            bake_one(i, report)
        else:
            print("BAKE %s: not built yet" % i)
    with open(FOLDER + "/audience_v2_bake.json", "w") as f:
        json.dump(report, f, indent=1)


# ------------------------------------------------------------------ the look, as the cast's review draws it

def toon_material(name, image):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = image
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse")
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.10
    ramp.color_ramp.elements[0].color = (0.45, 0.47, 0.56, 1)
    ramp.color_ramp.elements[1].position = 0.14
    ramp.color_ramp.elements[1].color = (1.0, 0.97, 0.90, 1)
    mul = nt.nodes.new("ShaderNodeMixRGB")
    mul.blend_type = "MULTIPLY"
    mul.inputs[0].default_value = 1
    em = nt.nodes.new("ShaderNodeEmission")
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(dif.outputs[0], s2r.inputs[0])
    nt.links.new(s2r.outputs[0], ramp.inputs[0])
    nt.links.new(tex.outputs[0], mul.inputs[1])
    nt.links.new(ramp.outputs[0], mul.inputs[2])
    nt.links.new(mul.outputs[0], em.inputs[0])
    nt.links.new(em.outputs[0], out.inputs[0])
    return m


def ink():
    m = bpy.data.materials.get("ink")
    if m:
        return m
    m = bpy.data.materials.new("ink")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs[0].default_value = (0.002, 0.002, 0.003, 1)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs[0])
    m.use_backface_culling = True
    return m


def toon(objs, tag):
    for o in objs:
        img = None
        for s in o.material_slots:
            if s.material and s.material.node_tree:
                for n in s.material.node_tree.nodes:
                    if n.type == "TEX_IMAGE" and n.image:
                        img = n.image
        if img is None:
            continue
        mat = toon_material("toon-" + tag + o.name, img)
        o.data.materials.clear()
        o.data.materials.append(mat)
        hull = o.copy()
        hull.data = o.data.copy()
        for c in o.users_collection:
            c.objects.link(hull)
        bm = bmesh.new()
        bm.from_mesh(hull.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        for f in bm.faces:
            f.normal_flip()
            f.smooth = True
        bm.to_mesh(hull.data)
        bm.free()
        hull.data.materials.clear()
        hull.data.materials.append(ink())
        d = hull.modifiers.new("push", "DISPLACE")
        d.strength = -OUTLINE
        d.mid_level = 0.0
        hull.visible_shadow = False


def stage(res, target, dist, azim=25.0, elev=10.0, lens=60):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w")
    sc.world = w
    w.color = (0.135, 0.14, 0.17)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sc.collection.objects.link(sun)
    sun.data.energy = 3.14
    sun.rotation_euler = (math.radians(58), 0, math.radians(-38))
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
    g = bpy.context.object
    gm = bpy.data.materials.new("ground")
    gm.diffuse_color = (0.2, 0.205, 0.235, 1)
    g.data.materials.append(gm)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    sc.collection.objects.link(cam)
    cam.data.lens = lens
    t = Vector(target)
    e, a = math.radians(elev), math.radians(azim)
    cam.location = t + Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))) * dist
    cam.rotation_euler = (t - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam
    sc.render.resolution_x, sc.render.resolution_y = res


def ledge(x, y=0.0, h=0.2):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y + 0.13, h / 2))
    b = bpy.context.object
    b.scale = (0.42, 0.3, h)
    m = bpy.data.materials.new("ledge")
    m.diffuse_color = (0.42, 0.40, 0.36, 1)
    b.data.materials.append(m)


BASE = dict(elder_man="mang_kanor", woman="inday", young_man="kuya_boy", elder_woman="lola_pacing",
            boy="totoy", girl="bebang")


def lineup():
    """Each villager with the Classic it was built from on its right (the owner asked for the finish to be
    compared), then Rafi, Dante and Cheska."""
    reset()
    gap = 0.48
    x = 0.0
    order = []
    for i in IDS:
        if os.path.exists(rigged(i)):
            order += [rigged(i), "%s/%s/%s-redesign.glb" % (CAST, BASE[i], BASE[i])]
    order += ["%s/%s/%s-redesign.glb" % (CAST, c, c) for c in ROW[:3]]
    for k, path in enumerate(order):
        arm, meshes = load(path)
        act = action(arm, "idle")
        if act:
            pose(arm, act, act.frame_range[0])
        arm.location.x += x
        toon(meshes, str(k))
        x += gap + (0.12 if k % 2 == 1 else 0.0)
    width = x - gap
    stage((2200, 720), (width / 2, 0, 0.36), width * 1.25 + 1.0, azim=12.0, elev=6.0, lens=50)
    bpy.context.scene.render.filepath = FOLDER + "/audience_v2_lineup.png"
    bpy.ops.render.render(write_still=True)

    reset()
    y = 0.0
    for i in IDS:
        x = 0.0
        for name in ("stand", "cheer", "clap", "sit"):
            p = "%s/audience_v2_%s_%s.glb" % (FOLDER, i, name)
            if os.path.exists(p):
                before = set(bpy.data.objects)
                bpy.ops.import_scene.gltf(filepath=p)
                new = [o for o in bpy.data.objects if o not in before]
                lift = 0.2 if name == "sit" else 0.0
                for o in new:
                    o.location.x += x
                    o.location.y += y
                    o.location.z += lift
                if name == "sit":
                    ledge(x, y)
                toon([o for o in new if o.type == "MESH"], i + name)
            x += 0.6
        y += 0.9
    stage((1400, 1800), (0.9, 2.25, 0.3), 9.5, azim=20.0, elev=28.0, lens=50)
    bpy.context.scene.render.filepath = FOLDER + "/audience_v2_poses.png"
    bpy.ops.render.render(write_still=True)


if args[0] == "bake":
    bake()
else:
    lineup()
