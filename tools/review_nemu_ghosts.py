"""Look at Nemu's remodelled ghosts (tools/build_nemu_ghosts.py) before the game is asked: turnarounds and a lineup.

  blender -b --python tools/review_nemu_ghosts.py -- --version=v1 [--colours=nemu|astra]
  py -3 tools/review_nemu_ghosts.py --join --version=v1 [--colours=nemu|astra]

Writes to Logs/nemu_ghosts/:
  <ghost>_turnaround_<vN>.png   front, three-quarter, side and back, posed as the game holds it (Skitter with a slipper
                                between its claws, Lookout with its lantern out, Blindfold waiting and then sprung)
  lineup_<vN>.png               Nemu and Kuro (both at CharacterVisual.PersonScale 2.38, as the game draws them) beside
                                the three ghosts at their own true size (metres), on one ground line.
Seen in Blender only: flat palette cells and the painted atlas, an inverted-hull ink line, EEVEE. It imitates the toon
look; it is NOT the game's shader.
"""
import json
import math
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "Logs", "nemu_ghosts")
PALETTES = os.path.join(ROOT, "Logs", "hand-companions")


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


VERSION = arg("version", "v1")
COLOURS = arg("colours", "nemu")
SUFFIX = "" if COLOURS == "nemu" else "_" + COLOURS
FOLDER = os.path.join(ROOT, "ArtSource", "nemu-ghosts", *([] if COLOURS == "nemu" else [COLOURS]))
TAG = VERSION + SUFFIX
GHOSTS = ["blindfold", "blindfold-sprung", "skitter", "lookout"]
VIEWS = (("front", (0, -1, 0.10)), ("quarter", (0.75, -0.8, 0.28)), ("side", (1, 0, 0.12)), ("back", (-0.55, 0.85, 0.25)))

if "--join" in sys.argv:
    from PIL import Image, ImageDraw
    for g in GHOSTS:
        tiles = [Image.open(os.path.join(LOGS, "%s_%s_%s.png" % (g, TAG, v))).convert("RGB") for v, _ in VIEWS]
        w, h = tiles[0].size
        out = Image.new("RGB", (w * len(tiles), h + 22), (28, 24, 34))
        d = ImageDraw.Draw(out)
        for k, (im, (v, _)) in enumerate(zip(tiles, VIEWS)):
            out.paste(im, (k * w, 22))
            d.text((k * w + 8, 5), "%s %s  %s" % (g, TAG, v), fill=(255, 255, 255))
        out.save(os.path.join(LOGS, "%s_turnaround_%s.png" % (g, TAG)))
        for v, _ in VIEWS:
            os.remove(os.path.join(LOGS, "%s_%s_%s.png" % (g, TAG, v)))
        print("wrote", "%s_turnaround_%s.png" % (g, TAG))
    raise SystemExit

import bpy  # noqa: E402
from mathutils import Quaternion, Vector  # noqa: E402

os.makedirs(LOGS, exist_ok=True)
PERSON_SCALE = 2.38


def lin(hexed):
    hexed = hexed.lstrip("#")
    c = [int(hexed[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    return [v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c]


def flat_material(name, rgb, glow=.35):
    m = bpy.data.materials.new(name); m.use_nodes = True; m.use_backface_culling = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*rgb, 1); p.inputs["Roughness"].default_value = .9
    p.inputs["Emission Color"].default_value = (*rgb, 1); p.inputs["Emission Strength"].default_value = glow
    return m


INK = flat_material("ink", (0.012, 0.009, 0.02), 0.0)


def ink(o, thickness):
    o.data.materials.append(INK)
    s = o.modifiers.new("ink", "SOLIDIFY")
    s.thickness = thickness; s.offset = 1; s.use_flip_normals = True; s.use_rim = False
    s.material_offset = len(o.data.materials) - 1


def import_group(path, name):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    grp = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(grp)
    for o in new:
        if o.parent is None:
            o.parent = grp
    return grp, new


def dress_ghost(objs, name):
    pal = json.load(open(os.path.join(PALETTES, name + ".palette.json")))
    atlas = bpy.data.images.load(os.path.join(FOLDER, name + "-atlas.png"))
    paint = bpy.data.materials.new("paint-" + name); paint.use_nodes = True; paint.use_backface_culling = True
    tex = paint.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = atlas
    pb = paint.node_tree.nodes["Principled BSDF"]; pb.inputs["Roughness"].default_value = .9
    paint.node_tree.links.new(tex.outputs["Color"], pb.inputs["Base Color"])
    paint.node_tree.links.new(tex.outputs["Color"], pb.inputs["Emission Color"]); pb.inputs["Emission Strength"].default_value = .35
    cells = {}
    for o in objs:
        if o.type != "MESH":
            continue
        me = o.data
        uv = me.uv_layers.active.data
        me.materials.clear()
        slots = {}
        for poly in me.polygons:
            poly.use_smooth = True
            if max(1.0 - uv[i].uv[1] for i in poly.loop_indices) < 0.5:
                key, mat = "paint", paint
            else:
                u, v = uv[poly.loop_start].uv
                slot = int(u * 16) // 2 + (8 if (1.0 - v) > 0.75 else 0)
                key = slot
                if slot not in cells:
                    # The glow cell (5) lights itself, as the game's ghosts' eyes do.
                    cells[slot] = flat_material("%s-%d" % (name, slot), lin(pal[slot]), 2.0 if slot == 5 else .35)
                mat = cells[slot]
            if key not in slots:
                me.materials.append(mat); slots[key] = len(me.materials) - 1
            poly.material_index = slots[key]
        ink(o, 0.0055 if max(o.dimensions) > 0.08 else 0.0025)


def ghost(kind, at=(0, 0, 0)):
    """Imports one ghost, posed as the game holds it, its feet on `at` (Blender space)."""
    base = {"blindfold": "nemu_ghost_blindfold", "blindfold-sprung": "nemu_ghost_blindfold", "skitter": "nemu_ghost_skitter",
            "lookout": "nemu_ghost_lookout"}[kind] + SUFFIX
    grp, objs = import_group(os.path.join(FOLDER, base + ".glb"), kind)
    dress_ghost(objs, base)
    by = {o.name.split(".")[0]: o for o in objs}
    if kind.startswith("blindfold"):
        hide = "waiting-" if kind == "blindfold-sprung" else "sprung-"
        for n, o in by.items():
            if n.startswith(hide):
                o.hide_render = True
    bpy.context.view_layer.update()

    def b(p):            # model space (y up, +z front) to Blender's
        return Vector((p[0], -p[2], p[1]))

    def aim(arm, hand_model, reach):
        o = by[arm]
        shoulder = o.matrix_world.translation.copy()
        target = grp.matrix_world @ b(hand_model)
        d = target - shoulder
        o.rotation_mode = "QUATERNION"
        o.rotation_quaternion = Vector((0, -1, 0)).rotation_difference(d.normalized())
        o.scale = (1, max(.5, d.length / reach), 1)

    if kind == "skitter":
        aim("arm-l", (-0.145, 0.225, 0.30), 0.325)
        aim("arm-r", (0.145, 0.225, 0.30), 0.325)
        for o in slipper(b((0.0, 0.225, 0.30))):
            o.parent = grp
    if kind == "lookout":
        aim("arm-l", (-0.285, 0.21, 0.065), 0.225)
        aim("arm-r", (0.365, 0.40, 0.145), 0.225)
        lg, lobjs = import_group(os.path.join(FOLDER, "nemu_ghost_lantern" + SUFFIX + ".glb"), "lantern")
        dress_ghost(lobjs, "nemu_ghost_lantern" + SUFFIX)
        lg.location = b((0.365, 0.40, 0.145))
        lg.parent = grp
        # The glass lights the ghost's side the way ASTRA's Lookout puts a point light at LookoutGlass.
        lamp = bpy.data.objects.new("lantern-light", bpy.data.lights.new("lantern-light", "POINT"))
        lamp.data.energy = 6; lamp.data.color = (1.0, 0.78, 0.42); lamp.data.shadow_soft_size = .05
        bpy.context.scene.collection.objects.link(lamp); lamp.parent = lg; lamp.location = (0, 0, -0.20)
    grp.location = at
    return grp


SLIPPER = []


def slipper(centre):
    """A tsinelas stand-in held across Skitter's front (the game hands it the real one): a sole and a V strap."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=centre)
    sole = bpy.context.active_object
    sole.scale = (0.25, 0.10, 0.022)
    bev = sole.modifiers.new("b", "BEVEL"); bev.width = 0.02; bev.segments = 4
    sole.data.materials.append(flat_material("sole", lin("#3E7FB8")))
    bpy.ops.object.modifier_apply(modifier="b")
    ink(sole, 0.004)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.035, minor_radius=0.008, location=centre + Vector((0.03, 0, 0.022)))
    strap = bpy.context.active_object
    strap.scale = (1.3, 1.0, 0.7)
    strap.data.materials.append(flat_material("strap", lin("#F2EEE6")))
    ink(strap, 0.003)
    return sole, strap


def nemu_and_kuro(at_nemu, at_kuro):
    grp, objs = import_group(os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/nemu/nemu-redesign.glb"), "nemu")
    for o in objs:
        if o.type == "MESH":
            if o.name.startswith("Icosphere"):
                o.hide_render = True; continue
            for poly in o.data.polygons:
                poly.use_smooth = True
            for mat in o.data.materials:
                if mat and mat.use_nodes:
                    p = mat.node_tree.nodes.get("Principled BSDF")
                    tex = next((n for n in mat.node_tree.nodes if n.type == "TEX_IMAGE"), None)
                    if p and tex:
                        mat.node_tree.links.new(tex.outputs["Color"], p.inputs["Emission Color"]); p.inputs["Emission Strength"].default_value = .35
                        p.inputs["Roughness"].default_value = .9
            ink(o, 0.0022)
    grp.scale = (PERSON_SCALE,) * 3
    grp.location = at_nemu
    # Kuro, calm form only, his cells in the stand-in colours tools/review_kuro_model.py uses.
    plum, shade, lav, pale, white, peach, eye = (0.105, 0.075, 0.17), (0.17, 0.12, 0.27), (0.67, 0.36, 0.94), (0.90, 0.80, 1.0), (1, 1, 1), (1.0, 0.62, 0.45), (0.80, 0.57, 1.0)
    colour = {"ghost-body-core": plum, "ghost-body-top-rim": shade, "ghost-body-bot-bevel": shade, "ghost-eye-l": eye, "ghost-eye-r": eye,
              "ghost-eye-glint-l": white, "ghost-eye-glint-r": white, "ghost-eye-pupil-l": pale, "ghost-eye-pupil-r": pale, "ghost-mouth-dot": pale,
              "ghost-blush-l": peach, "ghost-blush-r": peach, "ghost-tail-tier1": lav, "ghost-tail-tier2": lav, "ghost-tail-tier3": pale,
              "ghost-tail-tip-wisp": pale, "ghost-tail-tip-glint": white}
    kg, kobjs = import_group(os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb"), "kuro")
    for o in kobjs:
        n = o.name.split(".")[0]
        if o.type != "MESH":
            continue
        if n not in colour:
            o.hide_render = True; continue
        o.data.materials.clear()
        o.data.materials.append(flat_material("kuro-" + n, [c for c in colour[n]], .45))
        for poly in o.data.polygons:
            poly.use_smooth = True; poly.material_index = 0
        ink(o, 0.0012)
    kg.scale = (PERSON_SCALE,) * 3
    kg.location = at_kuro


def stage(size, centre, res=(560, 640)):
    sc = bpy.context.scene
    eng = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in eng else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    sc.render.resolution_x, sc.render.resolution_y = res
    if sc.world is None:
        w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
        w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.40, 0.46, 1)
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = 2.6; sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
        cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    sc.camera.data.type = "ORTHO"; sc.camera.data.ortho_scale = size
    return sc


def shoot(path, centre, direction, size, res=(560, 640)):
    sc = stage(size, centre, res)
    cam = sc.camera
    cam.location = centre + Vector(direction).normalized() * 6.0
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    global INK
    INK = flat_material("ink", (0.012, 0.009, 0.02), 0.0)


def ground(size=8):
    bpy.ops.mesh.primitive_plane_add(size=size, location=(0, 0, 0))
    g = bpy.context.active_object
    g.data.materials.append(flat_material("ground", lin("#8E8577"), .15))


# ---- turnarounds
for kind in GHOSTS:
    clear()
    ghost(kind)
    ground(3)
    height = {"blindfold": .86, "blindfold-sprung": .86, "skitter": .66, "lookout": .80}[kind]
    for view, d in VIEWS:
        shoot(os.path.join(LOGS, "%s_%s_%s.png" % (kind, TAG, view)), Vector((0, 0, height * .5)), d, height * 1.45)

# ---- the lineup, true scale: Nemu, Kuro by her shoulder, then the three ghosts.
clear()
ground(12)
nemu_and_kuro((-1.55, 0, 0), (-0.95, 0.0, 1.05))
ghost("blindfold", (-0.25, 0, 0))
ghost("skitter", (0.55, 0, 0))
ghost("lookout", (1.45, 0, 0))
shoot(os.path.join(LOGS, "lineup_%s.png" % TAG), Vector((-0.20, 0, 0.70)), (0.22, -1, 0.14), 4.5, res=(1600, 820))
print("done")
