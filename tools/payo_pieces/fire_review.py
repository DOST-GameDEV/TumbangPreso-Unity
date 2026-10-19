"""Review renders for the Payo night fires (tools/payo_pieces/fire_kit.py). Blender 5.0, headless.

  blender -b --python tools/payo_pieces/fire_review.py -- <ABS Logs/payo/pieces/fire> <tag>

Pieces ALONE first: each piece on a grey floor with a 1.75 m figure, turned through four angles (0, 15, 30 and 45
degrees: both are four- and eight-fold round), a close look into its bowl or head, and the pair together at 12 m and 40 m. A warm low sunset key
and a cool fill (the map's look), the reviews' ink hull (an inverted hull 1.2 cm out). fire_sheet.py lays them out.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT, TAG = ARGS[0], (ARGS[1] if len(ARGS) > 1 else "v1")
SHOTS = os.path.join(OUT, "shots")
os.makedirs(SHOTS, exist_ok=True)


def stage(res=(700, 820)):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.78, 0.66, 0.60, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.6
    for rot, energy, col in (((68, 0, -38), 3.4, (1.0, 0.76, 0.52)), ((115, 0, 150), 0.8, (0.60, 0.66, 1.0))):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.data.color = col; sun.rotation_euler = tuple(math.radians(a) for a in rot)
        sun.data.angle = math.radians(2)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.clip_start = 0.05; cam.data.clip_end = 500
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = res
    bpy.ops.mesh.primitive_plane_add(size=80, location=(0, 0, 0))
    g = bpy.data.materials.new("floor"); g.use_nodes = True
    b = g.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.44, 0.40, 0.35, 1); b.inputs["Roughness"].default_value = 1
    bpy.context.object.data.materials.append(g)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=1.75, location=(-1.3, 0.9, 0.875), vertices=16)
    f = bpy.data.materials.new("figure"); f.use_nodes = True
    f.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.25, 0.32, 0.45, 1)
    bpy.context.object.data.materials.append(f)
    return sc, cam


def ink(o, thick):
    line = bpy.data.materials.get("ink")
    if line is None:
        line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
        b = line.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (0.03, 0.03, 0.05, 1); b.inputs["Roughness"].default_value = 1
    for m in o.data.materials:
        if m:
            m.use_backface_culling = True
    h = o.copy(); h.data = o.data.copy(); h.name = o.name + "_ink"
    for c in o.users_collection:
        c.objects.link(h)
    h.data.materials.clear(); h.data.materials.append(line)
    bm = bmesh.new(); bm.from_mesh(h.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.normal_update()
    for v in bm.verts:
        v.co = v.co - v.normal * thick
    bm.to_mesh(h.data); bm.free()
    h.visible_shadow = False
    return h


def load(path, at=(0, 0, 0), yaw=0.0):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.parent is None:
            o.location = Vector(at); o.rotation_mode = "XYZ"; o.rotation_euler = (0, 0, math.radians(yaw))
    meshes = [o for o in new if o.type == "MESH"]
    for o in meshes:
        for m in o.data.materials:
            nt = m.node_tree if m and m.use_nodes else None
            b = nt.nodes.get("Principled BSDF") if nt else None
            tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE" and n.image and "normal" not in n.image.name.lower()), None) if nt else None
            if b and tex:                                      # a little of the paint as its own light (the toon-ish finish)
                nt.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
                b.inputs["Emission Strength"].default_value = 0.22
                b.inputs["Roughness"].default_value = 1.0
    bpy.context.view_layer.update()
    for o in meshes:
        h = ink(o, 0.012)
        h.parent = o.parent; h.matrix_world = o.matrix_world.copy()
    return new


def aim(cam, eye, look, fov):
    cam.location = Vector(eye)
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    cam.data.angle = math.radians(fov)


def shoot(sc, name):
    sc.render.filepath = os.path.join(SHOTS, "%s_%s.png" % (TAG, name))
    bpy.ops.render.render(write_still=True)
    print("SHOT", sc.render.filepath)


for piece, mid, dist, up in (("fire_pedestal", 0.7, 3.6, 1.7), ("fire_torch", 1.1, 4.2, 1.9)):
    for k, yaw in enumerate((0, 15, 30, 45)):
        sc, cam = stage()
        load(os.path.join(OUT, piece + ".glb"), yaw=yaw + 20)
        aim(cam, (0.0, -dist, up), (0.0, 0.0, mid), 34)
        shoot(sc, "%s_turn%d" % (piece, k))
    sc, cam = stage((900, 700))
    load(os.path.join(OUT, piece + ".glb"))
    top = 1.1 if piece == "fire_pedestal" else 1.96
    aim(cam, (0.5, -1.0, top + 0.9), (0.0, 0.0, top), 40)
    shoot(sc, piece + "_into")

sc, cam = stage((1200, 700))
load(os.path.join(OUT, "fire_pedestal.glb"), at=(0.9, 0, 0))
load(os.path.join(OUT, "fire_torch.glb"), at=(-0.4, 0.2, 0))
aim(cam, (3.0, -11.5, 2.4), (0.0, 0.0, 0.9), 22)
shoot(sc, "pair_12m")
aim(cam, (9.0, -39.0, 6.0), (0.0, 0.0, 0.9), 8)
shoot(sc, "pair_40m")
print("done")
