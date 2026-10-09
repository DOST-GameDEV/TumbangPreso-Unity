"""Look at a hero's hand companion model before the game is asked: five views in flat colour with an ink line.

  blender -b --python tools/review_hand_companion.py -- --hero=sean --version=v1 [--size=0.25] [--centre=0,0.03,0]
  py -3 tools/hand_companion_kit.py sheet sean v1

Reads Assets/TumbangPreso/Resources/Models/HandCompanions/<hero>.glb and Logs/hand-companions/<hero>.palette.json (both
written by the hero's build script), colours every face by its palette cell, and writes
Logs/hand-companions/<hero>_<vN>_<view>.png; the second command joins them into <hero>_<vN>.png.
`--size` is how many metres the picture is across (default: fitted to the model). Backfaces are culled, so a piece
wound inside out shows as a hole. This imitates the toon look; it is NOT the game's shader. Say "seen in Blender only".
"""
import bpy, bmesh, json, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "Logs", "hand-companions")


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


HERO, VERSION = arg("hero"), arg("version", "v1")
palette = json.load(open(os.path.join(LOGS, HERO + ".palette.json")))


def rgb(hexed):
    hexed = hexed.lstrip("#")
    c = [int(hexed[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    return [v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c]


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=arg("file") or os.path.join(ROOT, "Assets/TumbangPreso/Resources/Models", arg("folder", "HandCompanions"), HERO + ".glb"))
ink = bpy.data.materials.new("ink"); ink.use_nodes = True; ink.use_backface_culling = True
b = ink.node_tree.nodes["Principled BSDF"]
b.inputs["Base Color"].default_value = (0.02, 0.02, 0.03, 1); b.inputs["Roughness"].default_value = 1
flat = {}
ATLAS, PAINT = arg("atlas"), None
if ATLAS:
    PAINT = bpy.data.materials.new("paint"); PAINT.use_nodes = True; PAINT.use_backface_culling = True
    tex = PAINT.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(ATLAS)
    pb = PAINT.node_tree.nodes["Principled BSDF"]; pb.inputs["Roughness"].default_value = .9
    PAINT.node_tree.links.new(tex.outputs["Color"], pb.inputs["Base Color"])
    PAINT.node_tree.links.new(tex.outputs["Color"], pb.inputs["Emission Color"]); pb.inputs["Emission Strength"].default_value = .35
lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
for o in [o for o in bpy.data.objects if o.type == "MESH"]:
    me = o.data
    uv = me.uv_layers.active.data if me.uv_layers.active else None
    me.materials.clear()
    slots = {}
    for poly in me.polygons:
        slot = 0
        if uv:
            u, v = uv[poly.loop_start].uv
            v = 1.0 - v                                              # Blender flips glTF's v back
            if ATLAS and max(1.0 - uv[i].uv[1] for i in poly.loop_indices) < 0.5:
                # The atlas's upper half is paint, not palette (`--atlas=`).
                if "paint" not in slots:
                    me.materials.append(PAINT); slots["paint"] = len(me.materials) - 1
                poly.material_index = slots["paint"]; poly.use_smooth = True
                continue
            slot = int(u * 16) // 2 + (8 if v > 0.75 else 0)
        if slot not in slots:
            if slot not in flat:
                m = bpy.data.materials.new("slot%d" % slot); m.use_nodes = True; m.use_backface_culling = True
                c = rgb(palette[slot])
                p = m.node_tree.nodes["Principled BSDF"]
                p.inputs["Base Color"].default_value = (*c, 1); p.inputs["Roughness"].default_value = .9
                p.inputs["Emission Color"].default_value = (*c, 1); p.inputs["Emission Strength"].default_value = .35
                flat[slot] = m
            me.materials.append(flat[slot]); slots[slot] = len(me.materials) - 1
        poly.material_index = slots[slot]
        poly.use_smooth = True
    me.materials.append(ink)
    s = o.modifiers.new("ink", "SOLIDIFY")
    for corner in o.bound_box:
        w = o.matrix_world @ Vector(corner)
        lo = Vector(map(min, lo, w)); hi = Vector(map(max, hi, w))
    extent = max(o.dimensions) or .01
    s.thickness = min(float(arg("ink", .0022)), extent * .12); s.offset = 1; s.use_flip_normals = True; s.material_offset = len(me.materials) - 1; s.use_rim = False

sc = bpy.context.scene
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.40, 0.46, 0.56, 1)
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 2.5; sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
centre = (lo + hi) * .5
if arg("centre"):
    x, y, z = (float(v) for v in arg("centre").split(","))
    centre = Vector((x, -z, y))                                      # given in the model's own y-up, +z-front space
cam.data.type = "ORTHO"; cam.data.ortho_scale = float(arg("size", max(hi - lo) * 1.45))
sc.camera = cam; sc.render.resolution_x = sc.render.resolution_y = 520
# The model's +z (its front) is Blender's -y.
for view, at in (("front", (0, -1, 0)), ("quarter", (.6, -.8, .35)), ("side", (1, 0, 0)), ("back", (-.5, .85, .3)), ("under", (.25, -.5, -1))):
    cam.location = centre + Vector(at).normalized() * 2.0
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(LOGS, "%s_%s_%s.png" % (HERO, VERSION, view))
    bpy.ops.render.render(write_still=True)
print("done")
