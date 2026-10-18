"""Look at the SIMPLE ghosts (tools/build_nemu_ghosts_simple.py): an options board and two lineups.

  blender -b --python tools/review_nemu_ghosts_simple.py -- --version=v6
  py -3 tools/review_nemu_ghosts_simple.py --join --version=v6

Writes Logs/nemu_ghosts/:
  board_<vN>.png      each ghost twice, option A (rounded block) beside option B (sheet drop), three-quarter view.
  lineup_<vN>_A.png   Nemu and Kuro (x2.38, as the game draws them) beside the three option-A ghosts, true size.
  lineup_<vN>_B.png   the same with option B.
Seen in Blender only (flat cells + painted atlas, inverted-hull ink), NOT the game's shader.
"""
import json
import math
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "Logs", "nemu_ghosts")
FOLDER = os.path.join(ROOT, "ArtSource", "nemu-ghosts", "simple")
PALETTES = os.path.join(ROOT, "Logs", "hand-companions")
VERSION = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--version=")), "v6")
JOBS = ("dontlook", "skitter", "lookout")
LABEL = {"dontlook": "DON'T LOOK", "skitter": "SKITTER", "lookout": "LOOKOUT"}

if "--join" in sys.argv and "--v11" in sys.argv:
    from PIL import Image, ImageDraw
    rows = []
    for cs in ("pastel", "warm"):
        tiles = [Image.open(os.path.join(LOGS, "v11_%s_%s_%s.png" % (VERSION, cs, c))).convert("RGB") for c in ("bunny", "front", "quarter", "side")]
        w, h = tiles[0].size
        row = Image.new("RGB", (w * 4, h))
        for k, tl in enumerate(tiles):
            row.paste(tl, (k * w, 0))
        ImageDraw.Draw(row).text((10, 8), "%s set:  bunny | bear front | three-quarter | side" % cs, fill=(255, 255, 255))
        rows.append(row)
    out = Image.new("RGB", (rows[0].size[0], rows[0].size[1] * 2))
    for i, r_ in enumerate(rows):
        out.paste(r_, (0, i * r_.size[1]))
    out.save(os.path.join(LOGS, "board_%s_bear.png" % VERSION))
    for f in os.listdir(LOGS):
        if f.startswith("v11_"):
            os.remove(os.path.join(LOGS, f))
    print("joined")
    raise SystemExit

if "--join" in sys.argv and ("--v9" in sys.argv or "--v10" in sys.argv):
    SNOUTS = ("small", "chunky") if "--v10" in sys.argv else ("short", "long")
    from PIL import Image, ImageDraw
    cols = ("bunny", "front", "quarter", "side")
    rows = []
    for cs in ("pastel", "warm"):
        for sn in SNOUTS:
            tiles = [Image.open(os.path.join(LOGS, "v9_%s_%s_%s_%s.png" % (VERSION, cs, sn, c))).convert("RGB") for c in cols]
            w, h = tiles[0].size
            row = Image.new("RGB", (w * 4, h))
            for k, t in enumerate(tiles):
                row.paste(t, (k * w, 0))
            ImageDraw.Draw(row).text((10, 8), "%s set, %s muzzle:  bunny | croc front | three-quarter | side" % (cs, sn), fill=(255, 255, 255))
            rows.append(row)
    if "--v10" in sys.argv:
        for cs in ("pastel", "warm"):
            tiles = [Image.open(os.path.join(LOGS, "v9_%s_%s_owl_%s.png" % (VERSION, cs, c))).convert("RGB") for c in ("front", "quarter", "side", "close")]
            w, h = tiles[0].size
            row = Image.new("RGB", (w * 4, h))
            for k, tl in enumerate(tiles):
                row.paste(tl, (k * w, 0))
            ImageDraw.Draw(row).text((10, 8), "%s set, LOOKOUT owl, bigger eyes:  front | three-quarter | side | face" % cs, fill=(255, 255, 255))
            rows.append(row)
    out = Image.new("RGB", (rows[0].size[0], rows[0].size[1] * len(rows)))
    for i, r in enumerate(rows):
        out.paste(r, (0, i * r.size[1]))
    out.save(os.path.join(LOGS, "board_%s_croc.png" % VERSION))
    for f in os.listdir(LOGS):
        if f.startswith("v9_"):
            os.remove(os.path.join(LOGS, f))
    print("joined")
    raise SystemExit

if "--join" in sys.argv and "--v8" in sys.argv:
    from PIL import Image, ImageDraw
    SETS = ("pastel", "ghost", "warm")
    rows = [Image.open(os.path.join(LOGS, "v8row_%s_%s.png" % (VERSION, cs))).convert("RGB") for cs in SETS]
    W, H = rows[0].size
    out = Image.new("RGB", (W, H * 3), (0, 0, 0))
    for i, (im, cs) in enumerate(zip(rows, SETS)):
        out.paste(im, (0, i * H))
        ImageDraw.Draw(out).text((12, i * H + 10), "%s set:  Kuro | DON'T LOOK mouse | SKITTER bunny | SKITTER croc (alt) | LOOKOUT owl" % cs, fill=(255, 255, 255))
    out.save(os.path.join(LOGS, "board_%s_colours.png" % VERSION))
    tiles = []
    for cs in SETS:
        q = Image.open(os.path.join(LOGS, "v8croc_%s_%s_q.png" % (VERSION, cs))).convert("RGB")
        sd = Image.open(os.path.join(LOGS, "v8croc_%s_%s_s.png" % (VERSION, cs))).convert("RGB")
        row = Image.new("RGB", (q.size[0] + sd.size[0], q.size[1]))
        row.paste(q, (0, 0)); row.paste(sd, (q.size[0], 0))
        ImageDraw.Draw(row).text((12, 10), "%s set:  bunny | croc | croc side" % cs, fill=(255, 255, 255))
        tiles.append(row)
    out = Image.new("RGB", (tiles[0].size[0], tiles[0].size[1] * 3))
    for i, t in enumerate(tiles):
        out.paste(t, (0, i * t.size[1]))
    out.save(os.path.join(LOGS, "board_%s_croc.png" % VERSION))
    for f in os.listdir(LOGS):
        if f.startswith(("v8row_", "v8croc_")):
            os.remove(os.path.join(LOGS, f))
    print("joined")
    raise SystemExit

if "--join" in sys.argv:
    from PIL import Image, ImageDraw
    path = os.path.join(LOGS, "board_%s.png" % VERSION)
    im = Image.open(path).convert("RGB")
    d = ImageDraw.Draw(im)
    w = im.size[0] / 6
    V7 = {"dontlook": ("bat", "mouse"), "skitter": ("puppy", "bunny", "croc"), "lookout": ("owl", "frog")}
    if "--v7" in sys.argv:
        entries = [(j, a) for j in JOBS for a in V7[j]]
        w = im.size[0] / len(entries)
        for n, (job, animal) in enumerate(entries):
            d.text((int(n * w + 12), 12), "%s  %s" % (LABEL[job], animal), fill=(255, 255, 255))
        im.save(path); print("labelled", path); raise SystemExit
    for i, job in enumerate(JOBS):
        for k, opt in enumerate("AB"):
            text = "%s  %s" % (LABEL[job], V7[job][k]) if "--v7" in sys.argv else "%s  option %s (%s)" % (LABEL[job], opt, "block" if opt == "A" else "sheet drop")
            d.text((int((2 * i + k) * w + 12), 12), text, fill=(255, 255, 255))
    im.save(path)
    print("labelled", path)
    raise SystemExit

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

os.makedirs(LOGS, exist_ok=True)
PERSON_SCALE = 2.38


def lin(h):
    h = h.lstrip("#")
    return [(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4) for v in (int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))]


def flat_material(name, rgb, glow=.35):
    m = bpy.data.materials.new(name); m.use_nodes = True; m.use_backface_culling = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*rgb, 1); p.inputs["Roughness"].default_value = .9
    p.inputs["Emission Color"].default_value = (*rgb, 1); p.inputs["Emission Strength"].default_value = glow
    return m


def ink(o, t):
    o.data.materials.append(INK)
    s = o.modifiers.new("ink", "SOLIDIFY")
    s.thickness = t; s.offset = 1; s.use_flip_normals = True; s.use_rim = False; s.material_offset = len(o.data.materials) - 1


def import_group(path, name):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    grp = bpy.data.objects.new(name, None); bpy.context.scene.collection.objects.link(grp)
    for o in new:
        if o.parent is None:
            o.parent = grp
    return grp, new


def dress(objs, name):
    pal = json.load(open(os.path.join(PALETTES, name + ".palette.json")))
    paint = bpy.data.materials.new("paint"); paint.use_nodes = True; paint.use_backface_culling = True
    tex = paint.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(os.path.join(FOLDER, name + "-atlas.png"))
    pb = paint.node_tree.nodes["Principled BSDF"]; pb.inputs["Roughness"].default_value = .9
    paint.node_tree.links.new(tex.outputs["Color"], pb.inputs["Base Color"])
    paint.node_tree.links.new(tex.outputs["Color"], pb.inputs["Emission Color"]); pb.inputs["Emission Strength"].default_value = .35
    cells = {}
    for o in objs:
        if o.type != "MESH":
            continue
        me = o.data; uv = me.uv_layers.active.data; me.materials.clear(); slots = {}
        for poly in me.polygons:
            poly.use_smooth = True
            if max(1.0 - uv[i].uv[1] for i in poly.loop_indices) < 0.5:
                key, mat = "paint", paint
            else:
                u, v = uv[poly.loop_start].uv
                key = int(u * 16) // 2 + (8 if (1.0 - v) > 0.75 else 0)
                if key not in cells:
                    cells[key] = flat_material("c%d" % key, lin(pal[key]), 2.0 if key == 2 else .2)
                mat = cells[key]
            if key not in slots:
                me.materials.append(mat); slots[key] = len(me.materials) - 1
            poly.material_index = slots[key]
        ink(o, 0.0045 if max(o.dimensions) > 0.06 else 0.002)


def b(p):
    return Vector((p[0], -p[2], p[1]))


def ghost(job, opt, at):
    name = "nemu_ghost_%s_%s" % (job, opt)
    grp, objs = import_group(os.path.join(FOLDER, name + ".glb"), name)
    dress(objs, name)
    by = {o.name.split(".")[0]: o for o in objs}
    bpy.context.view_layer.update()

    def aim(arm, target_model):
        o = by[arm]
        d = (grp.matrix_world @ b(target_model)) - o.matrix_world.translation
        o.rotation_mode = "QUATERNION"; o.rotation_quaternion = Vector((0, -1, 0)).rotation_difference(d.normalized())
        return o.matrix_world.translation + d.normalized() * 0.07

    if job == "skitter" and "croc" in opt:
        # The croc carries it crosswise in its snout's tip.
        hold = (0.0, 0.249, 0.171) if "croc_small" in opt else (0.0, 0.237, 0.182) if "croc_chunky" in opt else (0.0, 0.258, 0.171) if "croc_short" in opt else (0.0, 0.258, 0.206) if "croc_long" in opt else (0.0, 0.283, 0.215)
        for o in slipper(b(hold)):
            o.parent = grp
    elif job == "skitter" and "arm-r" not in by:
        # The puppy and the croc fetch with their mouths: the slipper crosswise under the snout's tip.
        for o in slipper(b((0.0, 0.180, 0.19) if opt == "puppy" else (0.0, 0.205, 0.40))):
            o.parent = grp
    elif job == "skitter":
        sh = by["arm-r"].matrix_world.translation
        hold = (0.0, sh.z - 0.03, -sh.y + 0.10)
        aim("arm-l", (-0.12, hold[1], hold[2])); aim("arm-r", (0.12, hold[1], hold[2]))
        for o in slipper(b(hold)):
            o.parent = grp
    if job == "lookout":
        sh = by["arm-r"].matrix_world.translation
        tip = aim("arm-r", (sh.x + 0.10, sh.z - 0.03, -sh.y + 0.08))
        lg, lo = import_group(os.path.join(FOLDER, "nemu_ghost_lantern_simple.glb"), "lantern")
        dress(lo, "nemu_ghost_lantern_simple")
        lg.location = tip; lg.parent = grp
        lamp = bpy.data.objects.new("lamp", bpy.data.lights.new("lamp", "POINT"))
        lamp.data.energy = 3; lamp.data.color = (1.0, .8, .45); bpy.context.scene.collection.objects.link(lamp)
        lamp.parent = lg; lamp.location = (0, 0, -0.11)
    grp.location = at
    return grp


def slipper(centre):
    bpy.ops.mesh.primitive_cube_add(size=1, location=centre)
    sole = bpy.context.active_object; sole.scale = (0.24, 0.095, 0.022)
    bev = sole.modifiers.new("b", "BEVEL"); bev.width = .02; bev.segments = 4
    sole.data.materials.append(flat_material("sole", lin("#3E7FB8"))); bpy.ops.object.modifier_apply(modifier="b"); ink(sole, .004)
    bpy.ops.mesh.primitive_torus_add(major_radius=.032, minor_radius=.008, location=centre + Vector((.03, 0, .02)))
    strap = bpy.context.active_object; strap.scale = (1.3, 1, .7)
    strap.data.materials.append(flat_material("strap", lin("#F2EEE6"))); ink(strap, .003)
    return sole, strap


def nemu_and_kuro(at_nemu, at_kuro):
    objs = []
    if at_nemu is not None:
        grp, objs = import_group(os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/nemu/nemu-redesign.glb"), "nemu")
    for o in objs:
        if o.type != "MESH":
            continue
        if o.name.startswith("Icosphere"):
            o.hide_render = True; continue
        for mat in o.data.materials:
            p = mat.node_tree.nodes.get("Principled BSDF") if mat and mat.use_nodes else None
            tex = next((n for n in mat.node_tree.nodes if n.type == "TEX_IMAGE"), None) if p else None
            if tex:
                mat.node_tree.links.new(tex.outputs["Color"], p.inputs["Emission Color"]); p.inputs["Emission Strength"].default_value = .35
        ink(o, .0022)
    if at_nemu is not None:
        grp.scale = (PERSON_SCALE,) * 3; grp.location = at_nemu
    plum, shade, lav, pale, white, peach, eye = (0.105, 0.075, 0.17), (0.17, 0.12, 0.27), (0.67, 0.36, 0.94), (0.90, 0.80, 1.0), (1, 1, 1), (1.0, 0.62, 0.45), (0.80, 0.57, 1.0)
    colour = {"ghost-body-core": plum, "ghost-body-top-rim": shade, "ghost-body-bot-bevel": shade, "ghost-eye-l": eye, "ghost-eye-r": eye,
              "ghost-eye-glint-l": white, "ghost-eye-glint-r": white, "ghost-eye-pupil-l": pale, "ghost-eye-pupil-r": pale, "ghost-mouth-dot": pale,
              "ghost-blush-l": peach, "ghost-blush-r": peach, "ghost-tail-tier1": lav, "ghost-tail-tier2": lav, "ghost-tail-tier3": pale,
              "ghost-tail-tip-wisp": pale, "ghost-tail-tip-glint": white}
    kg, ko = import_group(os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb"), "kuro")
    for o in ko:
        n = o.name.split(".")[0]
        if o.type != "MESH":
            continue
        if n not in colour:
            o.hide_render = True; continue
        o.data.materials.clear(); o.data.materials.append(flat_material("k" + n, list(colour[n]), .45))
        for poly in o.data.polygons:
            poly.use_smooth = True; poly.material_index = 0
        ink(o, .0012)
    kg.scale = (PERSON_SCALE,) * 3; kg.location = at_kuro


def setup():
    global INK
    bpy.ops.wm.read_factory_settings(use_empty=True)
    INK = flat_material("ink", (0.012, 0.009, 0.02), 0.0)
    sc = bpy.context.scene
    eng = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in eng else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.40, 0.46, 1)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
    sun.data.energy = 2.6; sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = "ORTHO"
    bpy.ops.mesh.primitive_plane_add(size=14)
    bpy.context.active_object.data.materials.append(flat_material("ground", lin("#8E8577"), .15))
    return sc


def shoot(sc, path, centre, direction, size, res):
    sc.render.resolution_x, sc.render.resolution_y = res
    cam = sc.camera; cam.data.ortho_scale = size
    cam.location = centre + Vector(direction).normalized() * 8
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


if "--v11b" in sys.argv:
    for bc in ("cocoa", "lilac"):
        sc = setup()
        nemu_and_kuro((-1.25, 0, 0), (-0.68, 0, 1.05))
        for n, name in enumerate((("dontlook", "mouse_pastel"), ("skitter", "bunny_pastel"), ("skitter", "bear_" + bc), ("lookout", "owl_pastel"))):
            ghost(name[0], name[1], (-0.10 + n * 0.55, 0, 0))
        shoot(sc, os.path.join(LOGS, "lineup_%s_bear_%s.png" % (VERSION, bc)), Vector((-0.05, 0, 0.66)), (0.22, -1, 0.14), 3.6, (1500, 820))
    print("done")
    raise SystemExit

if "--v11" in sys.argv:
    V = (("front", (0, -1, 0.10)), ("quarter", (0.65, -1, 0.22)), ("side", (1, 0, 0.10)))
    for cs in ("pastel", "warm"):
        sc = setup()
        ghost("skitter", "bunny_" + cs, (0, 0, 0))
        shoot(sc, os.path.join(LOGS, "v11_%s_%s_bunny.png" % (VERSION, cs)), Vector((0, 0, 0.30)), (0.45, -1, 0.18), 0.80, (440, 520))
        sc = setup()
        ghost("skitter", "bear_" + cs, (0, 0, 0))
        for view, d in V:
            shoot(sc, os.path.join(LOGS, "v11_%s_%s_%s.png" % (VERSION, cs, view)), Vector((0, -0.05, 0.28)), d, 0.80, (440, 520))
        sc = setup()
        nemu_and_kuro((-1.25, 0, 0), (-0.68, 0, 1.05))
        for n, (job, animal) in enumerate((("dontlook", "mouse"), ("skitter", "bunny"), ("skitter", "bear"), ("lookout", "owl"))):
            ghost(job, "%s_%s" % (animal, cs), (-0.10 + n * 0.55, 0, 0))
        shoot(sc, os.path.join(LOGS, "lineup_%s%s.png" % (VERSION, "" if cs == "pastel" else "_" + cs)), Vector((-0.05, 0, 0.66)), (0.22, -1, 0.14), 3.6, (1500, 820))
    print("done")
    raise SystemExit

if "--v9" in sys.argv or "--v10" in sys.argv:
    SNOUTS = ("small", "chunky") if "--v10" in sys.argv else ("short", "long")
    VIEWS9 = (("front", (0, -1, 0.10)), ("quarter", (0.65, -1, 0.22)), ("side", (1, 0, 0.10)))
    for cs in ("pastel", "warm"):
        for sn in SNOUTS:
            sc = setup()
            ghost("skitter", "bunny_" + cs, (0, 0, 0))
            shoot(sc, os.path.join(LOGS, "v9_%s_%s_%s_bunny.png" % (VERSION, cs, sn)), Vector((0, 0, 0.30)), (0.45, -1, 0.18), 0.80, (440, 520))
            sc = setup()
            ghost("skitter", "croc_%s_%s" % (sn, cs), (0, 0, 0))
            for view, d in VIEWS9:
                shoot(sc, os.path.join(LOGS, "v9_%s_%s_%s_%s.png" % (VERSION, cs, sn, view)), Vector((0, -0.05, 0.28)), d, 0.80, (440, 520))
    if "--v10" in sys.argv:
        for cs in ("pastel", "warm"):
            sc = setup()
            ghost("lookout", "owl_" + cs, (0, 0, 0))
            for view, d in VIEWS9 + (("close", (0, -1, 0.05)),):
                shoot(sc, os.path.join(LOGS, "v9_%s_%s_owl_%s.png" % (VERSION, cs, view)), Vector((0.03, -0.05, 0.30 if view != "close" else 0.33)), d, 0.80 if view != "close" else 0.45, (440, 520))
    print("done")
    raise SystemExit

if "--v8" in sys.argv:
    SETS = ("pastel", "ghost", "warm")
    ROW = (("dontlook", "mouse"), ("skitter", "bunny"), ("skitter", "croc"), ("lookout", "owl"))
    for cs in SETS:
        sc = setup()
        nemu_and_kuro(None, (-1.10, 0, 0.32))
        for n, (job, animal) in enumerate(ROW):
            ghost(job, "%s_%s" % (animal, cs), (-0.45 + n * 0.55, 0, 0))
        shoot(sc, os.path.join(LOGS, "v8row_%s_%s.png" % (VERSION, cs)), Vector((0.05, 0, 0.30)), (0.30, -1, 0.20), 3.0, (1650, 520))
        sc = setup()
        for n, (view, d) in enumerate((("quarter", (0.55, -1, 0.20)), ("side", (1, 0, 0.12)), ("front", (0, -1, 0.10)))):
            pass
        ghost("skitter", "bunny_" + cs, (-0.55, 0, 0))
        ghost("skitter", "croc_" + cs, (0.10, 0, 0))
        shoot(sc, os.path.join(LOGS, "v8croc_%s_%s_q.png" % (VERSION, cs)), Vector((-0.22, 0, 0.28)), (0.45, -1, 0.20), 1.45, (900, 560))
        sc = setup()
        ghost("skitter", "croc_" + cs, (0, 0, 0))
        shoot(sc, os.path.join(LOGS, "v8croc_%s_%s_s.png" % (VERSION, cs)), Vector((0, 0.05, 0.28)), (1, 0.05, 0.12), 0.85, (520, 560))
        sc = setup()
        nemu_and_kuro((-1.25, 0, 0), (-0.68, 0, 1.05))
        for n, (job, animal) in enumerate(ROW):
            ghost(job, "%s_%s" % (animal, cs), (-0.10 + n * 0.55, 0, 0))
        shoot(sc, os.path.join(LOGS, "lineup_%s_%s.png" % (VERSION, cs)), Vector((-0.05, 0, 0.66)), (0.22, -1, 0.14), 3.6, (1500, 820))
    print("done")
    raise SystemExit

if "--v7" in sys.argv:
    ANIMALS = {"dontlook": ("bat", "mouse"), "skitter": ("puppy", "bunny", "croc"), "lookout": ("owl", "frog")}
    PICK = {"dontlook": "bat", "skitter": "puppy", "lookout": "owl"}
    sc = setup()
    entries = [(j, a) for j in JOBS for a in ANIMALS[j]]
    for n, (job, animal) in enumerate(entries):
        ghost(job, animal, (n * 0.55 - 0.55 * (len(entries) - 1) / 2, 0, 0))
    shoot(sc, os.path.join(LOGS, "board_%s.png" % VERSION), Vector((0, 0, 0.28)), (0.35, -1, 0.22), 0.55 * len(entries) + 0.1, (300 * len(entries), 560))
    sc = setup()
    nemu_and_kuro((-1.25, 0, 0), (-0.68, 0, 1.05))
    for i, job in enumerate(JOBS):
        ghost(job, PICK[job], (-0.05 + i * 0.62, 0, 0))
    shoot(sc, os.path.join(LOGS, "lineup_%s.png" % VERSION), Vector((-0.15, 0, 0.66)), (0.22, -1, 0.14), 3.4, (1500, 860))
    print("done")
    raise SystemExit

# The board: A and B of each job, side by side.
sc = setup()
for i, job in enumerate(JOBS):
    for k, opt in enumerate("ab"):
        ghost(job, opt, ((2 * i + k) * 0.55 - 1.375, 0, 0))
shoot(sc, os.path.join(LOGS, "board_%s.png" % VERSION), Vector((0, 0, 0.28)), (0.35, -1, 0.22), 3.4, (2100, 560))

for opt in "ab":
    sc = setup()
    nemu_and_kuro((-1.25, 0, 0), (-0.68, 0, 1.05))
    for i, job in enumerate(JOBS):
        ghost(job, opt, (-0.05 + i * 0.62, 0, 0))
    shoot(sc, os.path.join(LOGS, "lineup_%s_%s.png" % (VERSION, opt.upper())), Vector((-0.15, 0, 0.66)), (0.22, -1, 0.14), 3.4, (1500, 860))
print("done")
