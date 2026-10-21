"""PAYO land kit, step 3: pictures of a piece on its own (Blender, NOT the game's shader).

  blender -b --python tools/payo_pieces/terrace_review.py -- stack            the terrace test: six rows up a 35 degree slope
  blender -b --python tools/payo_pieces/terrace_review.py -- piece <name> [span]   three-quarter with a 1.75 m figure, close, above
  blender -b --python tools/payo_pieces/terrace_review.py -- row <name> <name> ... a row of pieces at one scale

Pictures go to Logs/payo/pieces/terrace/shots. A toon-ish look: a warm-white sun, a cool blue sky light (the owner:
the blue of reference 238 is the LIGHT, the stone is grey), a little of each texture's own colour as light, an ink line.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BUILD = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "terrace")
SHOTS = os.path.join(ROOT, "Logs", "payo", "pieces", "terrace", "shots")
os.makedirs(SHOTS, exist_ok=True)
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["stack"]
H = 1.15
RUN = H / math.tan(math.radians(35))


_ink = [None]


def stage(res=(1200, 750)):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _ink[0] = None
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    nt = w.node_tree
    bg = nt.nodes["Background"]
    # the sky seen by the camera is a pale aqua haze; the sky that LIGHTS is a cooler blue
    lp = nt.nodes.new("ShaderNodeLightPath"); mixn = nt.nodes.new("ShaderNodeMixRGB")
    mixn.inputs[1].default_value = (0.36, 0.52, 0.86, 1); mixn.inputs[2].default_value = (0.62, 0.84, 0.90, 1)
    nt.links.new(lp.outputs["Is Camera Ray"], mixn.inputs[0]); nt.links.new(mixn.outputs[0], bg.inputs[0])
    bg.inputs[1].default_value = 1.0
    for rot, energy, colour in (((52, 0, -32), 2.7, (1.0, 0.95, 0.84)), ((118, 0, 150), 0.6, (0.6, 0.75, 1.0))):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.data.color = colour; sun.rotation_euler = tuple(math.radians(a) for a in rot)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.clip_end = 3000; cam.data.lens = 26
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = res
    return sc, cam


def look(cam, eye, target, lens=26):
    cam.data.lens = lens
    cam.location = Vector(eye)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()


def shoot(name):
    bpy.context.scene.render.filepath = os.path.join(SHOTS, name + ".png")
    bpy.ops.render.render(write_still=True)


def flat(name, colour, emit=0.3, alpha=1.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = colour + (1,); b.inputs["Roughness"].default_value = 1
    b.inputs["Emission Color"].default_value = colour + (1,); b.inputs["Emission Strength"].default_value = emit
    if alpha < 1:
        b.inputs["Alpha"].default_value = alpha
        m.surface_render_method = "BLENDED"
    return m


def dress(m):
    """Make an imported material look nearer the game: texture times vertex colour, some of it as light."""
    if m.get("dressed"):
        return
    m["dressed"] = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None)
    name = m.name.split(".")[0]
    b.inputs["Roughness"].default_value = 1.0
    b.inputs["Metallic"].default_value = 0.0
    if name == "py_water":
        for l in list(nt.links):
            nt.links.remove(l)
        lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs[0].default_value = 0.22
        mx = nt.nodes.new("ShaderNodeMixRGB")
        mx.inputs[1].default_value = (0.50, 0.82, 0.97, 1)             # the sky in it, at a glance
        mx.inputs[2].default_value = (0.16, 0.46, 0.56, 1)             # looking down into it
        em = nt.nodes.new("ShaderNodeEmission"); tr = nt.nodes.new("ShaderNodeBsdfTransparent"); ms = nt.nodes.new("ShaderNodeMixShader")
        out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
        nt.links.new(lw.outputs["Facing"], mx.inputs[0]); nt.links.new(mx.outputs[0], em.inputs[0])
        nt.links.new(lw.outputs["Facing"], ms.inputs[0]); nt.links.new(em.outputs[0], ms.inputs[1]); nt.links.new(tr.outputs[0], ms.inputs[2])
        # facing 1 = looking straight down: mostly see through to the bed. Keep some water colour.
        ms.inputs[0].default_value = 0.0
        mx2 = nt.nodes.new("ShaderNodeMath"); mx2.operation = "MULTIPLY"; mx2.inputs[1].default_value = 0.35
        nt.links.new(lw.outputs["Facing"], mx2.inputs[0]); nt.links.new(mx2.outputs[0], ms.inputs[0])
        nt.links.new(ms.outputs[0], out.inputs["Surface"])
        m.surface_render_method = "BLENDED"
        return
    col = None
    if tex:
        at = nt.nodes.new("ShaderNodeAttribute"); at.attribute_name = "Col"; at.attribute_type = "GEOMETRY"
        mul = nt.nodes.new("ShaderNodeMixRGB"); mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1.0
        nt.links.new(tex.outputs["Color"], mul.inputs[1]); nt.links.new(at.outputs["Color"], mul.inputs[2])
        col = mul.outputs[0]
        for l in list(b.inputs["Base Color"].links):
            nt.links.remove(l)
        nt.links.new(col, b.inputs["Base Color"]); nt.links.new(col, b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = {"py_terrace_plant": 0.5, "py_fall": 0.6, "py_foam": 0.7, "py_mist": 0.8}.get(name, 0.22)
    if name in ("py_fall", "py_wet", "py_mist"):
        m.surface_render_method = "DITHERED" if name == "py_fall" else "BLENDED"
        if name == "py_mist":
            b.inputs["Alpha"].default_value = 0.72
    m.use_backface_culling = name not in ("py_terrace_plant", "py_fall", "py_wet")


def load(name, at=(0, 0, 0), turn=0.0, ink=0.012):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(BUILD, name + ".glb"))
    obs = [o for o in bpy.data.objects if o not in before]
    root = bpy.data.objects.new(name, None); bpy.context.scene.collection.objects.link(root)
    for o in obs:
        if o.parent is None:
            o.parent = root
        if o.type != "MESH":
            continue
        for ca in o.data.color_attributes:
            ca.name = "Col"; break
        mats = {m.name.split(".")[0] for m in o.data.materials if m}
        for m in o.data.materials:
            if m:
                dress(m)
        if ink and not mats & {"py_terrace_plant", "py_water", "py_fall", "py_wet", "py_mist"}:
            add_ink(o, ink)
    root.location = at; root.rotation_euler = (0, 0, turn)
    return root





def add_ink(o, thick):
    if _ink[0] is None:
        line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
        b = line.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (0.03, 0.03, 0.04, 1); b.inputs["Roughness"].default_value = 1
        _ink[0] = line
    o.data.materials.append(_ink[0])
    s = o.modifiers.new("ink", "SOLIDIFY")
    s.thickness = thick; s.offset = 1; s.use_flip_normals = True; s.material_offset = len(o.data.materials) - 1; s.use_rim = False


def clone(root, at, turn=0.0):
    def cp(o, parent):
        n = o.copy(); bpy.context.scene.collection.objects.link(n); n.parent = parent
        for c in o.children:
            cp(c, n)
        return n
    n = cp(root, None)
    n.location = at; n.rotation_euler = (0, 0, turn)
    return n


def figure(at, turn=0.0):
    """A 1.75 m person, plain, for scale."""
    skin = flat("fig_skin", (0.78, 0.52, 0.36)); cloth = flat("fig_cloth", (0.86, 0.34, 0.22)); dark = flat("fig_dark", (0.16, 0.14, 0.16))
    root = bpy.data.objects.new("figure", None); bpy.context.scene.collection.objects.link(root)

    def part(kind, loc, scale, mat):
        if kind == "s":
            bpy.ops.mesh.primitive_uv_sphere_add(segments=14, ring_count=8, location=loc)
        else:
            bpy.ops.mesh.primitive_cylinder_add(vertices=12, location=loc)
        o = bpy.context.object; o.scale = scale; o.data.materials.append(mat); o.parent = root
        for p in o.data.polygons:
            p.use_smooth = True
        add_ink(o, 0.012)
    part("s", (0, 0, 1.60), (0.13, 0.14, 0.15), skin)
    part("s", (0, 0.01, 1.66), (0.14, 0.15, 0.11), dark)
    part("c", (0, 0, 1.18), (0.19, 0.12, 0.27), cloth)
    part("c", (-0.09, 0, 0.45), (0.075, 0.08, 0.45), dark)
    part("c", (0.09, 0, 0.45), (0.075, 0.08, 0.45), dark)
    part("c", (-0.25, 0, 1.12), (0.055, 0.055, 0.30), skin)
    part("c", (0.25, 0, 1.12), (0.055, 0.055, 0.30), skin)
    root.location = at; root.rotation_euler = (0, 0, turn)
    return root


def plane(name, corners, mat):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(c) for c in corners], [], [tuple(range(len(corners)))])
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o); me.materials.append(mat)
    return o


def stack():
    sc, cam = stage()
    rows = ["young", "young", "ripe", "young", "flooded", "young"]
    roots = {}
    for i, v in enumerate(rows):
        for col in (-1, 0, 1):
            vv = v if col == 0 else rows[(i + col + 6) % 6] if i % 2 else v
            key = "terrace_straight_" + vv
            at = (col * 12.0, i * RUN, i * H)
            if key not in roots:
                roots[key] = load(key, at)
            else:
                clone(roots[key], at)
    grass = flat("hill", (0.05, 0.13, 0.05), 0.0)
    t35 = math.tan(math.radians(35))
    plane("slope", [(-80, -0.0, 0), (80, 0, 0), (80, 60, 60 * t35), (-80, 60, 60 * t35)], grass)
    plane("top", [(-18, 6 * RUN + 0.1, 6 * H), (18, 6 * RUN + 0.1, 6 * H), (18, 6 * RUN + 6, 6 * H + 0.4), (-18, 6 * RUN + 6, 6 * H + 0.4)], grass)
    plane("floor", [(-80, -60, -0.05), (80, -60, -0.05), (80, 0.4, -0.05), (-80, 0.4, -0.05)], flat("floor", (0.07, 0.15, 0.06), 0.0))
    figure((6.9, -0.8, 0), 0.4)
    look(cam, (6, -7.0, 1.7), (6, 3.5, 4.3), 24); shoot("stack_below")
    look(cam, (-13.5, -3.5, 4.2), (4, 4.6, 3.6), 30); shoot("stack_side")
    look(cam, (6, -7.5, 15.5), (6, 4.6, 3.2), 30); shoot("stack_above")
    look(cam, (5.2, -3.4, 1.5), (5.2, 0.5, 0.95), 30); shoot("stack_close")
    look(cam, (6, -22.0, 1.7), (6, 3.5, 4.6), 35); shoot("stack_far")


def bounds(roots):
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    bpy.context.view_layer.update()
    for r in roots:
        for o in [r] + list(r.children_recursive):
            if o.type == "MESH":
                for c in o.bound_box:
                    p = o.matrix_world @ Vector(c)
                    lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    return lo, hi


def piece(name, tag=None):
    sc, cam = stage()
    root = load(name)
    lo, hi = bounds([root])
    if name == "terrace_stair":                                        # shown against the wall it climbs
        load("terrace_straight_young", (-4.0, 0, 0))
    size = hi - lo
    span = max(size.x, size.z * 1.3, size.y, 1.2)
    c = (lo + hi) / 2
    plane("floor", [(-400, -400, -0.01), (400, -400, -0.01), (400, 400, -0.01), (-400, 400, -0.01)], flat("floor", (0.10, 0.17, 0.09), 0.0))
    figure((hi.x + 0.6, lo.y - 0.2, 0), 0.3)
    if name.startswith("fall_"):                                       # a plain cliff behind it, for the picture only
        rockm = flat("cliff", (0.20, 0.20, 0.21), 0.0)
        top = hi.z - 0.25 * min(1.0, size.z / 8)
        w = max(size.x * 1.6, 3.0)
        plane("cliff", [(-w, 0.10, 0), (w, 0.10, 0), (w, 0.10, top), (-w, 0.10, top)], rockm)
        plane("clifftop", [(-w, 0.10, top), (w, 0.10, top), (w, 30, top), (-w, 30, top)], flat("clifftop", (0.10, 0.17, 0.09), 0.0))
    tag = tag or name
    d = span * 1.25 + 1.5
    eye_z = max(1.7, c.z * 0.9)
    look(cam, (c.x - d * 0.55, c.y - d * 0.85, eye_z), (c.x + 0.3, c.y, c.z * 0.95), 30); shoot(tag + "_front")
    k = min(span, 2.6)
    if name.startswith("fall_"):
        k = min(max(size.x * 0.5, 2.2), 14)
        c = Vector((c.x, lo.y + size.y * 0.5, c.z))
    look(cam, (c.x + k * 0.5, lo.y - k * 0.9, min(hi.z, max(0.8, lo.z + k * 0.5))), (c.x, c.y, min(c.z, lo.z + k * 0.45)), 34); shoot(tag + "_close")
    look(cam, (c.x - d * 0.15, c.y - d * 0.35, hi.z + d * 0.9), c, 30); shoot(tag + "_above")
    look(cam, (c.x + d * 0.75, c.y + d * 0.65, eye_z + size.z * 0.2), (c.x, c.y, c.z * 0.95), 30); shoot(tag + "_back")


def row(names, tag):
    sc, cam = stage((1800, 700))
    x = 0.0
    roots = []
    for n in names:
        r = load(n)
        lo, hi = bounds([r])
        r.location.x = x - lo.x
        x += (hi.x - lo.x) + 0.8
        roots.append(r)
    lo, hi = bounds(roots)
    plane("floor", [(-400, -400, -0.01), (400, -400, -0.01), (400, 400, -0.01), (-400, 400, -0.01)], flat("floor", (0.10, 0.17, 0.09), 0.0))
    figure((hi.x + 0.7, 0, 0), 0.2)
    c = (lo + hi) / 2
    d = (hi.x - lo.x + 2) * 0.62 + (hi.z - lo.z) * 0.5
    look(cam, (c.x - d * 0.25, c.y - d, max(1.7, hi.z * 0.7)), (c.x + 0.3, c.y, c.z * 0.9), 30); shoot(tag + "_row")
    look(cam, (c.x, c.y - d * 0.5, hi.z + d * 0.8), c, 30); shoot(tag + "_rowabove")


if args[0] == "stack":
    stack()
elif args[0] == "piece":
    for n in args[1:]:
        piece(n)
elif args[0] == "row":
    row(args[2:], args[1])
print("done")
