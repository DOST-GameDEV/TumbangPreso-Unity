"""Kuro's calm form as the model file has it, from four sides, to check a remodel before Unity is asked.

  blender -b --python tools/review_kuro_model.py -- --version=v1

Writes Logs/kuro-cute/kuro_model_<vN>_<view>.png. Colours are stand-ins for Nemu's palette cells (the file holds
cells, not colours); backfaces are culled, so a part wound inside out shows as a hole.
"""
import bpy, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "Logs", "kuro-cute")
VERSION = next((a.split("=")[1] for a in sys.argv if a.startswith("--version=")), "v1")
os.makedirs(LOGS, exist_ok=True)

PLUM, SHADE, LAV, PALE, WHITE, PEACH, EYE = (0.105, 0.075, 0.17), (0.17, 0.12, 0.27), (0.67, 0.36, 0.94), (0.90, 0.80, 1.0), (1, 1, 1), (1.0, 0.62, 0.45), (0.80, 0.57, 1.0)
COLOUR = {"ghost-body-core": PLUM, "ghost-body-top-rim": SHADE, "ghost-body-bot-bevel": SHADE, "ghost-eye-l": EYE, "ghost-eye-r": EYE,
          "ghost-eye-glint-l": WHITE, "ghost-eye-glint-r": WHITE, "ghost-eye-pupil-l": PALE, "ghost-eye-pupil-r": PALE, "ghost-mouth-dot": PALE,
          "ghost-blush-l": PEACH, "ghost-blush-r": PEACH, "ghost-tail-tier1": LAV, "ghost-tail-tier2": LAV, "ghost-tail-tier3": PALE,
          "ghost-tail-tip-wisp": PALE, "ghost-tail-tip-glint": WHITE}

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb"))
ink = bpy.data.materials.new("ink"); ink.use_nodes = True; ink.use_backface_culling = True
ink.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.02, 0.02, 0.03, 1)
for o in list(bpy.data.objects):
    name = o.name.split(".")[0]
    if name not in COLOUR:
        if o.type == "MESH":
            o.hide_render = True
        continue
    m = bpy.data.materials.new(name); m.use_nodes = True; m.use_backface_culling = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*COLOUR[name], 1); b.inputs["Roughness"].default_value = .9
    b.inputs["Emission Color"].default_value = (*COLOUR[name], 1); b.inputs["Emission Strength"].default_value = .35
    o.data.materials.clear(); o.data.materials.append(m)
    if "eye" not in name and "mouth" not in name:
        o.data.materials.append(ink)
        s = o.modifiers.new("ink", "SOLIDIFY"); s.thickness = .0016; s.offset = 1; s.use_flip_normals = True; s.material_offset = 1; s.use_rim = False

s = bpy.context.scene
s.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
s.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); s.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.40, 0.46, 0.56, 1)
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); s.collection.objects.link(sun)
sun.data.energy = 2.5; sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); s.collection.objects.link(cam)
cam.data.type = "ORTHO"; cam.data.ortho_scale = .24; s.camera = cam
s.render.resolution_x = s.render.resolution_y = 600
# The glb's +z (his face) is Blender's -y.
for view, at in (("under", (.25, -.5, -1)), ("front", (0, -1, .0)), ("quarter", (.6, -.8, .35)), ("side", (1, 0, 0)), ("back", (-.5, .85, .3))):
    look = Vector((0, 0, -.035))
    cam.location = look + Vector(at).normalized() * 1.0
    cam.rotation_euler = (look - cam.location).to_track_quat("-Z", "Y").to_euler()
    s.render.filepath = os.path.join(LOGS, "kuro_model_%s_%s.png" % (VERSION, view))
    bpy.ops.render.render(write_still=True)
print("done")
