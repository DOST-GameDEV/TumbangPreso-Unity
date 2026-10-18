"""PAYO court platform: review renders (Blender 5.0, headless), the calm review's light and ink line.

  blender -b --python tools/payo_pieces/court_review.py -- <court_platform.glb> <shots dir> [names...]
  py tools/payo_pieces/court_review.py sheet <shots dir> <out.png>        (system Python: the sheet)

The piece alone (no land, a flat ground at z -60.5 under it), the calm_review.py sun and sky, the glb's own atlas,
normal map and vertex colour. A 1.75 m figure stands on the paving and on the pit floor for scale.
"""
import math
import os
import sys

SHOTS = [
    # name, eye, look, fov (vertical degrees)
    ("top_ne_corner", (39.5, 50.5, -4.25), (-10, -30, -9), 62),
    ("top_nw_corner", (-39.5, 50.5, -4.25), (10, -30, -9), 62),
    ("top_se_corner", (39.0, -57.0, -4.25), (-6, 20, -6), 62),
    ("top_sw_corner", (-39.0, -57.0, -4.25), (6, 20, -6), 62),
    ("top_steps_west", (-36.0, 4.0, -4.25), (-44, -22, -1), 58),
    ("pit_west_wall", (12.0, 2.0, -26.0), (-30, 0, -16), 62),
    ("pit_east_wall", (-12.0, -2.0, -26.0), (30, 0, -16), 62),
    ("pit_south_wall", (4.0, 14.0, -26.0), (0, -30, -15), 62),
    ("pit_north_edge", (14.0, 12.0, -28.25), (-10, 40, -36), 62),
    ("gorge_north", (8.0, 31.0, -44.6), (-6, 70, -40), 62),
    ("gorge_south", (6.0, 54.0, -44.6), (0, 29, -34), 62),
    ("mouth_corner", (52.0, 74.0, -20.0), (30, 56, -24), 50),
    ("from_valley", (0.0, 260.0, 40.0), (0, 0, -20), 34),
    ("from_above", (75.0, -125.0, 95.0), (0, 0, -18), 42),
    ("straight_down", (0.0, -3.0, 160.0), (0, -3, -20), 52),
    ("niche_close", (-21.0, 4.0, -16.5), (-30, -2, -17), 60),
    ("pier_mouth", (37.0, 46.0, -4.25), (30.8, 55.0, -5.6), 55),
    ("step_pier_close", (-37.5, -9.0, -4.25), (-43.5, -16.0, -2.5), 60),
    ("lip_corner", (37.0, -37.0, -4.25), (30.0, -30.0, -7.0), 55),
    ("parapet_corner_out", (54.0, 66.0, -3.0), (44.0, 56.0, -7.0), 55),
    ("retain_corner_in", (37.0, -53.0, -4.25), (43.5, -61.5, 0.0), 62),
    ("mouth_from_gorge", (0.0, 40.0, -44.6), (30.0, 56.0, -20.0), 62),
    ("inlet_from_plaza", (2.0, -40.0, -4.25), (0.0, -62.0, -5.6), 50),
    ("inlet_from_stair", (0.0, -72.0, -3.2), (0.0, -55.0, -6.2), 58),
    ("inlet_close", (5.5, -55.5, -4.25), (0.0, -60.5, -6.2), 58),
    ("pool_from_valley", (0.0, 140.0, -20.0), (0.0, 60.0, -42.0), 50),
    ("pool_rim_person", (-21.5, 84.0, -42.55), (8.0, 62.0, -45.0), 62),
    ("pool_gorge_view", (0.0, 95.5, -42.55), (0.0, 40.0, -32.0), 62),
    ("pool_from_above", (45.0, 135.0, 25.0), (0.0, 72.0, -46.0), 45),
    ("valley_mid", (60.0, 140.0, -10.0), (0, 20, -25), 40),
]


# the ink line, as calm_review.py draws it (copied: that script runs on import)
def ink(o, thick):
    """The ink line as a SEPARATE inverted hull (a copy pushed out along its normals, faces reversed, back faces culled):
    a solidify modifier on the piece itself loses its tangent space, and the normal map would not show."""
    import bmesh
    import bpy
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
    if "custom_normal" in h.data.attributes:
        h.data.attributes.remove(h.data.attributes["custom_normal"])
    h.data.materials.clear(); h.data.materials.append(line)
    bm = bmesh.new(); bm.from_mesh(h.data)
    for f in bm.faces:
        f.material_index = 0
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)    # one hull, no cracks at the split corners
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.normal_update()
    for v in bm.verts:
        v.co = v.co - v.normal * thick                    # the normals now point in: push the hull out
    bm.to_mesh(h.data); bm.free()
    h.visible_shadow = False                              # a closed hull casting shadow would put the whole piece in shade
    return h


def blender_main():
    import bpy
    from mathutils import Vector
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    args = sys.argv[sys.argv.index("--") + 1:]
    glbs, shots = [os.path.abspath(g) for g in args[0].split(";") if g], os.path.abspath(args[1])
    only = set(args[2:])
    os.makedirs(shots, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.68, 0.80, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    for rot, energy, col in (((66, 0, -42), 3.4, (1.0, 0.80, 0.58)), ((120, 0, 150), 0.6, (0.62, 0.74, 1.0))):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.data.color = col; sun.rotation_euler = tuple(math.radians(a) for a in rot)
        sun.data.angle = math.radians(3)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.clip_start = 0.1; cam.data.clip_end = 3000
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = 960, 640
    bpy.ops.mesh.primitive_plane_add(size=3000, location=(0, 0, -60.5))
    g = bpy.data.materials.new("ground"); g.use_nodes = True
    b = g.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.40, 0.46, 0.30, 1); b.inputs["Roughness"].default_value = 1
    bpy.context.object.data.materials.append(g)
    # figures for scale
    for loc in ((-36.0, 0.0, -6.0), (8.0, -8.0, -30.0), (34.0, -40.0, -6.0)):
        bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=1.75, location=(loc[0], loc[1], loc[2] + 0.875))
        fm = bpy.data.materials.new("fig"); fm.use_nodes = True
        fm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.75, 0.2, 0.15, 1)
        bpy.context.object.data.materials.append(fm)

    before = set(bpy.data.objects)
    for glb in glbs:
        bpy.ops.import_scene.gltf(filepath=glb)
    meshes = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    for o in meshes:
        attr = o.data.color_attributes[0].name if o.data.color_attributes else None
        for m in o.data.materials:
            nt = m.node_tree
            bs = nt.nodes.get("Principled BSDF")
            tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE" and n.image and "normal" not in n.image.name.lower()), None)
            if tex is None:                                   # the water: flat colour, a little see-through
                continue
            src = tex.outputs["Color"]
            if attr:
                an = nt.nodes.new("ShaderNodeAttribute"); an.attribute_name = attr
                mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"
                mul.inputs[0].default_value = 1.0
                nt.links.new(src, mul.inputs[6]); nt.links.new(an.outputs["Color"], mul.inputs[7])
                src = mul.outputs[2]
            nt.links.new(src, bs.inputs["Base Color"])
            nt.links.new(src, bs.inputs["Emission Color"])
            bs.inputs["Emission Strength"].default_value = 0.22
            bs.inputs["Roughness"].default_value = 1.0
    for o in list(meshes):
        if not any(m and "py_water" in m.name for m in o.data.materials):
            ink(o, 0.035)
    for name, eye, look, fov in SHOTS:
        if only and name not in only:
            continue
        cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
        cam.location = eye
        cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = os.path.join(shots, name + ".png")
        bpy.ops.render.render(write_still=True)
    print("REVIEW done")


def sheet(shots, out):
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import calm_sheet
    names = [s[0] for s in SHOTS]
    rows = []
    for i in range(0, len(names), 4):
        cells = [(n.replace("_", " "), os.path.join(shots, n + ".png")) for n in names[i:i + 4]]
        rows.append(("", cells))
    calm_sheet.grid(rows, "COURT PLATFORM", out, "one stone structure: paving -6, pit floor -30, gorge floor -46.4, walls to -60; calm light, ink line")


if __name__ == "__main__":
    if "--" in sys.argv:
        blender_main()
    else:
        sheet(sys.argv[2], sys.argv[3])
