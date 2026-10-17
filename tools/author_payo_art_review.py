"""PAYO, the art's review pictures (2026-10-12): day, and a NIGHT MOCK of the second half.

Called by tools/author_payo_art.py --review N, before the meshes are turned for export. Writes
Logs/payo/art/payo_art_<view>[_night]_vN.png; tools/author_payo_art_sheets.py lays them out as sheets.

A MOCK, NOT THE GAME'S LOOK: Blender's own lights and a haze put into every material by hand (distance from the camera,
depth in the pit and down the valley). It shows the models and their colours together; the game's shaders will differ.
"""
import math

import bpy
from mathutils import Vector


def lin(hexs):
    v = [int(hexs[i:i + 2], 16) / 255.0 for i in (1, 3, 5)]
    return tuple(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in v)


def fog_group(P):
    g = bpy.data.node_groups.new("PayoFog", "ShaderNodeTree")
    for nm, tp in (("dist", "NodeSocketFloat"), ("pit", "NodeSocketFloat"), ("dist_colour", "NodeSocketColor"), ("pit_colour", "NodeSocketColor")):
        g.interface.new_socket(nm, in_out="OUTPUT", socket_type=tp)
    N, L = g.nodes, g.links
    out = N.new("NodeGroupOutput")
    cam, geo, sep = N.new("ShaderNodeCameraData"), N.new("ShaderNodeNewGeometry"), N.new("ShaderNodeSeparateXYZ")
    vals = {}
    for k in ("d0", "k", "cap"):
        vals[k] = N.new("ShaderNodeValue"); vals[k].name = k
    for k in ("dist_colour", "pit_colour"):
        c = N.new("ShaderNodeRGB"); c.name = k
        L.new(c.outputs[0], out.inputs[k])

    def m(op, a, b=None, clamp=False):
        n = N.new("ShaderNodeMath"); n.operation = op; n.use_clamp = clamp
        for i, s in enumerate((a, b)):
            if s is None:
                continue
            if isinstance(s, (int, float)):
                n.inputs[i].default_value = s
            else:
                L.new(s, n.inputs[i])
        return n.outputs[0]

    d = m("MAXIMUM", m("SUBTRACT", cam.outputs["View Distance"], vals["d0"].outputs[0]), 0.0)
    f = m("SUBTRACT", 1.0, m("EXPONENT", m("MULTIPLY", m("MULTIPLY", d, vals["k"].outputs[0]), -1.0)))
    L.new(m("MULTIPLY", f, vals["cap"].outputs[0], clamp=True), out.inputs["dist"])
    L.new(geo.outputs["Position"], sep.inputs[0])
    depth = m("MULTIPLY", sep.outputs["Z"], -1.0)
    inside = m("MULTIPLY", m("LESS_THAN", m("ABSOLUTE", sep.outputs["X"]), P.LIP + 0.4), m("LESS_THAN", m("ABSOLUTE", sep.outputs["Y"]), P.LIP + 0.4))
    pit = m("MULTIPLY", m("POWER", m("DIVIDE", m("SUBTRACT", depth, 16.0), 20.0, clamp=True), 0.8), inside)
    valley = m("DIVIDE", m("SUBTRACT", depth, 30.0), 30.0, clamp=True)
    L.new(m("MULTIPLY", m("MAXIMUM", pit, valley), 0.97, clamp=True), out.inputs["pit"])
    return g


def render(sc, P, logs, version, only, lay, night_lights, inst, static):
    g = fog_group(P)
    P.fog_all(g)
    ids = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in ids else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    for k, v in (("use_shadows", True), ("use_raytracing", True), ("taa_render_samples", 32)):
        try:
            setattr(sc.eevee, k, v)
        except Exception:
            pass
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    WN, WL = w.node_tree.nodes, w.node_tree.links
    tc, sp, ramp = WN.new("ShaderNodeTexCoord"), WN.new("ShaderNodeSeparateXYZ"), WN.new("ShaderNodeValToRGB")
    WL.new(tc.outputs["Generated"], sp.inputs[0]); WL.new(sp.outputs["Z"], ramp.inputs[0])
    ramp.color_ramp.elements[1].position = 0.6
    mid = ramp.color_ramp.elements.new(0.14)
    bg = WN["Background"]
    WL.new(ramp.outputs[0], bg.inputs[0])
    vor, lt, ab, mul, mu2, bg2, add = (WN.new("ShaderNodeTexVoronoi"), WN.new("ShaderNodeMath"), WN.new("ShaderNodeMath"), WN.new("ShaderNodeMath"), WN.new("ShaderNodeMath"),
                                       WN.new("ShaderNodeBackground"), WN.new("ShaderNodeAddShader"))
    vor.inputs["Scale"].default_value = 170.0
    WL.new(tc.outputs["Generated"], vor.inputs["Vector"])
    lt.operation = "LESS_THAN"; WL.new(vor.outputs["Distance"], lt.inputs[0]); lt.inputs[1].default_value = 0.045
    ab.operation = "GREATER_THAN"; WL.new(sp.outputs["Z"], ab.inputs[0]); ab.inputs[1].default_value = 0.015
    mul.operation = "MULTIPLY"; WL.new(lt.outputs[0], mul.inputs[0]); WL.new(ab.outputs[0], mul.inputs[1])
    mu2.operation = "MULTIPLY"; WL.new(mul.outputs[0], mu2.inputs[0])
    bg2.inputs[0].default_value = (*lin("#dfe8ff"), 1); WL.new(mu2.outputs[0], bg2.inputs[1])
    WL.new(bg.outputs[0], add.inputs[0]); WL.new(bg2.outputs[0], add.inputs[1])
    WL.new(add.outputs[0], WN["World Output"].inputs[0])
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
    lamps = []
    for i, (x, y, z, power, rad) in enumerate(night_lights):
        ld = bpy.data.lights.new("fire%d" % i, "POINT")
        ld.energy, ld.color, ld.shadow_soft_size = power, lin("#ffb060"), rad
        ld.use_shadow = power >= 1400.0
        lo = bpy.data.objects.new("fire%d" % i, ld); sc.collection.objects.link(lo); lo.location = (x, y, z)
        lamps.append(lo)
    sc.render.resolution_x, sc.render.resolution_y = 1280, 720
    sc.render.image_settings.file_format = "PNG"
    tops = {p["id"]: p["top"] for p in lay["pieces"]}
    ch = lay["canHeight"]
    FAR, AIR = (110, 0.00070, 0.90), (260, 0.00055, 0.9)
    views = [
        ("air_a", Vector((250, 300, 150)), Vector((0, -20, -8)), 44, AIR),
        ("air_b", Vector((-330, 170, 120)), Vector((0, -10, -6)), 42, AIR),
        ("stage", Vector((-52, 74, 40)), Vector((2, -6, -9)), 48, (80, 0.0010, 0.9)),
        ("eye_north", Vector((0.0, -1.4, ch + 1.7)), Vector((9.0, 90, 1.0)), 66, FAR),
        ("eye_east", Vector((10.3, -2.0, tops.get("slabE", ch) + 1.7)), Vector((90, -10, -7.0)), 64, FAR),
        ("eye_south", Vector((0.0, 8.0, tops.get("slabN", 0.0) + 1.7)), Vector((0, -40, 9.5)), 66, FAR),
        ("eye_west", Vector((-2.0, 0.5, ch + 1.7)), Vector((-90, -8, 6.0)), 66, FAR),
        ("outer_ne", Vector((20.9, 20.9, tops.get("towerNE", 0.6) + 1.7)), Vector((-8, -16, 1.5)), 66, FAR),
        ("outer_s", Vector((1.5, -20.6, tops.get("midS", 0.0) + 1.7)), Vector((-6, -60, 4.0)), 66, FAR),
        ("outer_w", Vector((-20.8, -2.0, tops.get("midW", 0.0) + 1.7)), Vector((-80, -30, 8.0)), 66, FAR),
        ("close_block", Vector((-13.2, -12.6, 1.2)), Vector((-17.5, -17.5, -3.4)), 58, (80, 0.0010, 0.9)),
        ("close_pit", Vector((12.5, -3.0, 1.4)), Vector((30, -16, -13.0)), 60, (80, 0.0010, 0.9)),
        ("close_forecourt", Vector((2.0, -33.6, -3.6)), Vector((-12, -44, -4.8)), 62, (80, 0.0010, 0.9)),
        ("close_forecourt_b", Vector((-3.0, -35.0, -3.2)), Vector((16, -46, -4.4)), 62, (80, 0.0010, 0.9)),
        ("close_terrace", Vector((-40.0, -40.0, -1.5)), Vector((-66, -64, 2.0)), 60, (80, 0.0010, 0.9)),
        ("close_shelf", Vector((38.0, 0.0, 1.6)), Vector((70, 8, -2.0)), 58, (80, 0.0010, 0.9)),
        ("close_bird", Vector((1.0, 12.0, 2.2)), Vector((6.0, 27.0, 9.5)), 52, (80, 0.0010, 0.9)),
    ]
    for (name, eye, look, fov, f) in views:
        cd = bpy.data.cameras.new(name); co = bpy.data.objects.new("cam_" + name, cd); sc.collection.objects.link(co)
        cd.clip_start, cd.clip_end = 0.1, 9000
        cd.sensor_fit, cd.angle_y = "VERTICAL", math.radians(fov)
        co.location = eye
        co.rotation_euler = (look - eye).to_track_quat("-Z", "Y").to_euler()
    logs.mkdir(parents=True, exist_ok=True)
    may = static.get("payo_sky_mayari")
    fire_m, win_m, may_m, cloud_m = (bpy.data.materials.get(n) for n in ("py_fire", "py_window", "py_mayari", "py_cloud"))
    core_m, inlay_m, spill_m = (bpy.data.materials.get(n) for n in ("py_fire_core", "py_inlay", "py_spill"))

    def emis(m, strength, colour=None):
        if m is None:
            return
        b = m.node_tree.nodes.get("Principled BSDF")
        if b is None:
            return
        if colour is not None and not b.inputs["Emission Color"].links:
            b.inputs["Emission Color"].default_value = (*colour, 1)
        b.inputs["Emission Strength"].default_value = strength

    for night in (False, True):
        wanted = [v for v in views if (not only or v[0] in only)]
        if night:
            wanted = [v for v in wanted if v[0] in ("eye_north", "stage", "eye_south", "close_forecourt", "outer_s")]
        sky = ("#22365a", "#0e1a36", "#050914") if night else ("#e2f3e6", "#a6dcd6", "#58adc0")       # day: the reference's teal air
        ramp.color_ramp.elements[0].color = (*lin(sky[0]), 1)
        mid.color = (*lin(sky[1]), 1)
        ramp.color_ramp.elements[2].color = (*lin(sky[2]), 1)
        bg.inputs[1].default_value = 1.0 if night else 0.8
        mu2.inputs[1].default_value = 3.2 if night else 0.0
        g.nodes["dist_colour"].outputs[0].default_value = (*(lin("#0f1a30") if night else lin("#b4e0dc")), 1)
        g.nodes["pit_colour"].outputs[0].default_value = (*(lin("#31445f") if night else lin("#bfe6e0")), 1)
        if night:
            sun.data.energy, sun.data.color, sun.data.angle = 1.15, lin("#a9c4ff"), math.radians(3.0)
            d = Vector((-0.20, -0.84, -0.50)).normalized()
        else:
            sun.data.energy, sun.data.color, sun.data.angle = 4.6, lin("#fff0d8"), math.radians(2.0)
            d = Vector((-0.62, -0.52, -0.52)).normalized()
        sun.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
        for lo in lamps:
            lo.hide_render = not night
        for (o, pl) in inst:
            if pl["group"] == "night":
                o.hide_render = not night
        if may is not None:
            may.hide_render = not night
        emis(fire_m, 2.4 if night else 0.0, lin("#ff8f24"))
        emis(core_m, 4.0 if night else 0.0, lin("#ffe9a0"))
        emis(inlay_m, 1.4 if night else 0.25, lin("#48dcc8"))
        emis(spill_m, 1.6 if night else 0.0)
        emis(win_m, 1.8 if night else 0.0)
        emis(may_m, 1.5)
        emis(cloud_m, 0.0 if night else 0.25, lin("#eef2f4"))
        for (name, eye, look, fov, f) in wanted:
            if night and name == "eye_north":
                bpy.data.objects["cam_" + name].rotation_euler = (Vector((9.0, 90, 12.0)) - eye).to_track_quat("-Z", "Y").to_euler()
            for key, val in zip(("d0", "k", "cap"), f):
                g.nodes[key].outputs[0].default_value = val
            sc.camera = bpy.data.objects["cam_" + name]
            sc.render.filepath = str(logs / ("payo_art_%s%s_v%d.png" % (name, "_night" if night else "", version)))
            bpy.ops.render.render(write_still=True)
    print("[art] review pictures written, v%d" % version)
