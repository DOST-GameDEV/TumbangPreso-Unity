"""PAYO, THE GUARDIANS: export the finished glb and take the four review pictures. Blender only, NOT the game's shader.

  blender -b --python tools/payo_pieces/bulul_review.py -- <name> [--variant=stone|wood] [--noexport]

Opens Logs/payo/pieces/bulul/work/<name>_low.blend, gives the skin ONE material with its painted atlas, writes
Logs/payo/pieces/build/bulul/<name>.glb (y up, +z front, pivot on the ground), then renders into
Logs/payo/pieces/bulul/work/shots: <name>_front.png (three-quarter, from a standing person's eye, a 1.75 m figure
beside it), _back.png, _face.png, _top.png. Grey stone under a cool blue sky light and a warm-white sun, an ink line.
"""
import json
import math
import os
import sys

import bpy
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
WORK = os.path.join(ROOT, "Logs", "payo", "pieces", "bulul", "work")
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "bulul")
SHOTS = os.path.join(WORK, "shots")
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
name = [a for a in argv if not a.startswith("--")][0]
variant = next((a.split("=")[1] for a in argv if a.startswith("--variant=")), "stone")
tag = "" if variant == "stone" else "_" + variant
marks = json.load(open(os.path.join(WORK, name + ".json")))
os.makedirs(SHOTS, exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=os.path.join(WORK, name + "_low.blend"))
sc = bpy.context.scene
obj = bpy.data.objects[name]
primary = marks.get("primary", "stone")
if not any(a.startswith("--variant=") for a in argv):
    variant = primary
tag = "" if variant == primary else "_" + variant
atlas = os.path.join(OUT, name + ("-atlas.png" if variant == primary else "-atlas-%s.png" % variant))
img = bpy.data.images.load(atlas)

# ---- the shipped material and the glb
final = bpy.data.materials.new(name)
final.use_nodes = True
bsdf = final.node_tree.nodes["Principled BSDF"]
tex = final.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = img
final.node_tree.links.new(tex.outputs[0], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 1.0; bsdf.inputs["Metallic"].default_value = 0.0
obj.data.materials.clear(); obj.data.materials.append(final)
for p in obj.data.polygons:
    p.material_index = 0
if "--noexport" not in argv and variant == primary:
    for o in bpy.data.objects:
        o.select_set(o == obj)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, name + ".glb"), export_format="GLB", use_selection=True, export_yup=True,
                              export_apply=True, export_image_format="AUTO")
    marks["glb_kb"] = round(os.path.getsize(os.path.join(OUT, name + ".glb")) / 1024)
    json.dump(marks, open(os.path.join(WORK, name + ".json"), "w"), indent=1)

# ---- the review look
engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.46, 0.60, 0.86, 1)      # the cool blue sky light
w.node_tree.nodes["Background"].inputs[1].default_value = 0.62
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 3.8; sun.data.color = (1.0, 0.93, 0.80); sun.data.angle = math.radians(4)
sun.rotation_euler = (math.radians(52), 0, math.radians(38))


def toon(image=None, colour=(1, 1, 1)):
    m = bpy.data.materials.new("toon"); m.use_nodes = True; m.use_backface_culling = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial"); em = nt.nodes.new("ShaderNodeEmission")
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); s2 = nt.nodes.new("ShaderNodeShaderToRGB")
    mul = nt.nodes.new("ShaderNodeMixRGB"); mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1.0
    nt.links.new(dif.outputs[0], s2.inputs[0]); nt.links.new(s2.outputs[0], mul.inputs[1])
    if image:
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = image; nt.links.new(t.outputs[0], mul.inputs[2])
    else:
        mul.inputs[2].default_value = (*colour, 1)
    nt.links.new(mul.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
    return m


obj.data.materials.clear(); obj.data.materials.append(toon(img))
bb = [Vector(c) for c in obj.bound_box]
H = max(p.z for p in bb); W = max(max(p.x for p in bb) - min(p.x for p in bb), 0.1)
D = max(p.y for p in bb) - min(p.y for p in bb)
big = max(H, W, D)

line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
nt = line.node_tree; nt.nodes.clear()
o2 = nt.nodes.new("ShaderNodeOutputMaterial"); e2 = nt.nodes.new("ShaderNodeEmission"); e2.inputs[0].default_value = (0.03, 0.03, 0.04, 1)
nt.links.new(e2.outputs[0], o2.inputs[0])


def ink(o, thick):
    o.data.materials.append(line)
    s = o.modifiers.new("ink", "SOLIDIFY")
    s.thickness = thick; s.offset = 1; s.use_flip_normals = True; s.material_offset = len(o.data.materials) - 1; s.use_rim = False


ink(obj, big * 0.0032)

# plain ground
bpy.ops.mesh.primitive_plane_add(size=400, location=(0, 0, -0.002))
g = bpy.context.object; g.data.materials.append(toon(None, (0.50, 0.50, 0.47)))

# the 1.75 m figure: legs, a body, a head. Front-right of the piece.
fx, fy = W * 0.5 + 0.9, -(D * 0.1)
small = big < 1.9
LENS = 44 if small else 28
skin = toon(None, (0.95, 0.48, 0.22))
parts = []
for dx in (-0.11, 0.11):
    bpy.ops.mesh.primitive_cylinder_add(radius=0.085, depth=0.85, location=(fx + dx, fy, 0.425), vertices=10); parts.append(bpy.context.object)
bpy.ops.mesh.primitive_cylinder_add(radius=0.2, depth=0.62, location=(fx, fy, 1.16), vertices=12); parts.append(bpy.context.object)
bpy.ops.mesh.primitive_uv_sphere_add(radius=0.14, location=(fx, fy, 1.61), segments=12, ring_count=8); parts.append(bpy.context.object)
for dx in (-0.26, 0.26):
    bpy.ops.mesh.primitive_cylinder_add(radius=0.06, depth=0.62, location=(fx + dx, fy, 1.12), vertices=8); parts.append(bpy.context.object)
for p in parts:
    p.data.materials.append(skin); ink(p, 0.012)

cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam); sc.camera = cam
sc.render.resolution_x, sc.render.resolution_y = 1000, 1000


def shoot(pos, target, lens, path, ortho=None):
    cam.data.type = "ORTHO" if ortho else "PERSP"
    if ortho:
        cam.data.ortho_scale = ortho
    cam.data.lens = lens; cam.data.clip_end = 1000
    cam.location = Vector(pos)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


d = 1.12 * max(H, W * 1.15, D * 1.15) + 1.2
wall = marks.get("wall", False)
a1, a2 = math.radians(marks.get("az_front", 27)), math.radians(180 + 33 if not wall else -42)
eye = 1.6
mid = marks.get("mid_y", H * 0.5)
shoot((math.sin(a1) * d, -math.cos(a1) * d, eye), (W * 0.12 + (0.35 if small else 0), 0, mid), LENS, os.path.join(SHOTS, name + tag + "_front.png"))
shoot((math.sin(a2) * d, -math.cos(a2) * d, eye if not wall else mid), (0, 0, mid), LENS, os.path.join(SHOTS, name + tag + "_back.png"))
f = marks.get("face")
if f:
    c = Vector((f["c"][0], -f["c"][2], f["c"][1])); s = f["size"]
    shoot(c + Vector((s * 0.75, -s * 2.3, -s * 0.22)), c, 50, os.path.join(SHOTS, name + tag + "_face.png"))
el, az = math.radians(58), math.radians(18)
dd = d * 1.5
shoot((math.sin(az) * math.cos(el) * dd, -math.cos(az) * math.cos(el) * dd, mid + math.sin(el) * dd), (0, 0, mid), 35,
      os.path.join(SHOTS, name + tag + "_top.png"))
print("REVIEWED", name, variant)
