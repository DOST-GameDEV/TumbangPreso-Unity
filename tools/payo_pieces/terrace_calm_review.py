"""PAYO land kit, the CALM pass, step 3: review renders (Blender 5.0, headless).

  blender -b --python tools/payo_pieces/terrace_calm_review.py -- piece <name> [<name> ...]
  blender -b --python tools/payo_pieces/terrace_calm_review.py -- kit

Same stage as the pilot (tools/payo_pieces/calm_review.py: the warm late-afternoon sun, the soft blue sky, a little of
the paint as its own light, the ink line as a separate hull that casts no shadow), OLD = Logs/payo/pieces/build/terrace,
CALM = Logs/payo/pieces/calm/terrace, the same camera for both. Unlike the pilot's pieces these are painted by vertex
colour times picture (the glTF importer multiplies them), so the emission takes that product too; water, falls and
mist get the land kit's review treatment (terrace_review.py). Shots go to Logs/payo/pieces/calm/terrace/shots.

piece: 8 m, 40 m, 120 m from the piece's centre (front three-quarter), then 8 m with the normal map unplugged and
       8 m with the normal map alone on flat grey.  Then calm_sheet.py piece lays them out.
kit:   six terrace rows up a slope, a pit wall to the left, a cliff with the medium fall to the right, ground tiles,
       path, boulders and cover in front; from 1.7 m below, from 40 m and from 150 m, OLD and CALM.
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OLD = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "terrace")
CALM = os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "terrace")
SHOTS = os.path.join(CALM, "shots")
os.makedirs(SHOTS, exist_ok=True)
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["kit"]
_ink = []
NOINK = {"py_terrace_plant", "py_water", "py_fall", "py_wet", "py_mist", "py_terrace_far", "py_foam"}


def stage(res=(960, 640)):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _ink.clear()
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
    cam.data.clip_end = 4000
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = res
    return sc, cam


def flat(name, colour, emit=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = colour + (1,); b.inputs["Roughness"].default_value = 1
    b.inputs["Emission Color"].default_value = colour + (1,); b.inputs["Emission Strength"].default_value = emit
    return m


def plane(name, corners, mat):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(c) for c in corners], [], [tuple(range(len(corners)))])
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o); me.materials.append(mat)
    return o


def ground_plane():
    plane("ground", [(-3000, -3000, -0.02), (3000, -3000, -0.02), (3000, 3000, -0.02), (-3000, 3000, -0.02)], flat("ground", (0.40, 0.46, 0.30)))


def dress(m):
    if m.get("dressed") or not m.use_nodes:
        return
    m["dressed"] = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    name = m.name.split(".")[0]
    b.inputs["Roughness"].default_value = 1.0
    b.inputs["Metallic"].default_value = 0.0
    if name == "py_water":
        for l in list(nt.links):
            nt.links.remove(l)
        lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs[0].default_value = 0.22
        mx = nt.nodes.new("ShaderNodeMixRGB")
        mx.inputs[1].default_value = (0.50, 0.82, 0.97, 1); mx.inputs[2].default_value = (0.16, 0.46, 0.56, 1)
        em = nt.nodes.new("ShaderNodeEmission"); tr = nt.nodes.new("ShaderNodeBsdfTransparent"); ms = nt.nodes.new("ShaderNodeMixShader")
        out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
        nt.links.new(lw.outputs["Facing"], mx.inputs[0]); nt.links.new(mx.outputs[0], em.inputs[0])
        mx2 = nt.nodes.new("ShaderNodeMath"); mx2.operation = "MULTIPLY"; mx2.inputs[1].default_value = 0.35
        nt.links.new(lw.outputs["Facing"], mx2.inputs[0]); nt.links.new(mx2.outputs[0], ms.inputs[0])
        nt.links.new(em.outputs[0], ms.inputs[1]); nt.links.new(tr.outputs[0], ms.inputs[2])
        nt.links.new(ms.outputs[0], out.inputs["Surface"])
        m.surface_render_method = "BLENDED"
        return
    src = b.inputs["Base Color"].links[0].from_socket if b.inputs["Base Color"].links else None
    if src is not None:
        nt.links.new(src, b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = {"py_terrace_plant": 0.30, "py_fall": 0.6, "py_foam": 0.7, "py_mist": 0.8, "py_terrace_far": 0.30}.get(name, 0.22)
    if name in ("py_fall", "py_wet", "py_mist", "py_terrace_far"):
        m.surface_render_method = "DITHERED" if name in ("py_fall", "py_terrace_far") else "BLENDED"
        if name == "py_mist":
            b.inputs["Alpha"].default_value = 0.72
    m.use_backface_culling = name not in ("py_terrace_plant", "py_fall", "py_wet", "py_terrace_far")


def ink(o, thick):
    """The pilot's ink: a separate inverted hull (calm_review.ink), so the piece keeps its tangent space."""
    if not _ink:
        line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
        b = line.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (0.03, 0.03, 0.05, 1); b.inputs["Roughness"].default_value = 1
        _ink.append(line)
    h = o.copy(); h.data = o.data.copy(); h.name = o.name + "_ink"
    for c in o.users_collection:
        c.objects.link(h)
    h.data.materials.clear(); h.data.materials.append(_ink[0])
    bm = bmesh.new(); bm.from_mesh(h.data)
    for f in bm.faces:
        f.material_index = 0
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.normal_update()
    for v in bm.verts:
        v.co = v.co - v.normal * thick
    bm.to_mesh(h.data); bm.free()
    h.visible_shadow = False
    return h


def load(path, at=(0, 0, 0), turn=0.0, thick=0.03):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.parent is None:
            o.location = Vector(o.location) + Vector(at)
            o.rotation_euler.z += turn
    meshes = [o for o in new if o.type == "MESH"]
    bpy.context.view_layer.update()
    hulls = []
    for o in meshes:
        mats = {m.name.split(".")[0] for m in o.data.materials if m}
        for m in o.data.materials:
            if m:
                dress(m)
        if thick and not mats & NOINK:
            hulls.append(ink(o, thick))
    return meshes + hulls


def bounds(meshes):
    bpy.context.view_layer.update()
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for o in meshes:
        if o.name.endswith("_ink"):
            continue
        for c in o.bound_box:
            p = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    return lo, hi


def normals(meshes, on=True, grey=False):
    for o in meshes:
        for m in o.data.materials:
            if not (m and m.use_nodes) or m.name == "ink":
                continue
            nt = m.node_tree
            b = nt.nodes.get("Principled BSDF")
            if b is None:
                continue
            nm = next((n for n in nt.nodes if n.type == "NORMAL_MAP"), None)
            if nm is not None:
                if not on:
                    for l in list(b.inputs["Normal"].links):
                        nt.links.remove(l)
                else:
                    nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
            if grey and m.name.split(".")[0] not in ("py_water", "py_fall", "py_wet", "py_mist", "py_terrace_far"):
                for l in list(b.inputs["Base Color"].links) + list(b.inputs["Emission Color"].links):
                    nt.links.remove(l)
                b.inputs["Base Color"].default_value = (0.62, 0.60, 0.57, 1)
                b.inputs["Emission Color"].default_value = (0.62, 0.60, 0.57, 1)


def show(meshes, vis):
    for o in meshes:
        o.hide_render = not vis


def aim(cam, eye, look, fov):
    cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()


def shoot(sc, name):
    sc.render.filepath = os.path.join(SHOTS, name + ".png")
    bpy.ops.render.render(write_still=True)


DIR = Vector((0.55, -0.835, 0)).normalized()


def piece(name):
    sc, cam = stage()
    ground_plane()
    A = load(os.path.join(OLD, name + ".glb"))
    B = load(os.path.join(CALM, name + ".glb"))
    lo, hi = bounds(A)
    H = hi.z
    c = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, 0))
    rad = max(hi.x - lo.x, hi.y - lo.y) / 2
    ext = abs(DIR.x) * (hi.x - lo.x) / 2 + abs(DIR.y) * (hi.y - lo.y) / 2      # centre to the box's edge, along the view
    near = ext + 8.0 if H < 20 else ext + 26.0
    up = (5.0, 14.0, 40.0) if H < 1.5 else (max(2.2, H * 0.3), 6.0, 18.0)       # flat pieces (ground) are seen from higher
    views = {
        "8m": (c + DIR * near + Vector((0, 0, up[0])), c + Vector((0, 0, H * 0.45)), 62),
        "40m": (c + DIR * max(40.0, rad * 1.2) + Vector((0, 0, up[1])), c + Vector((0, 0, H * 0.42)), 50),
        "120m": (c + DIR * 120.0 + Vector((0, 0, up[2])), c + Vector((0, 0, H * 0.42)), 50),
    }
    for k, (eye, look, fov) in views.items():
        aim(cam, eye, look, fov)
        show(A, True); show(B, False); shoot(sc, "%s_old_%s" % (name, k))
        show(A, False); show(B, True); shoot(sc, "%s_calm_%s" % (name, k))
    eye, look, fov = views["8m"]
    aim(cam, eye, look, fov)
    show(A, False); show(B, True)
    normals(B, on=False); shoot(sc, "%s_calm_8m_nonormal" % name)
    normals(B, on=True, grey=True); shoot(sc, "%s_calm_8m_normalonly" % name)


def kit_scene(root):
    """Everything placed for the kit picture, from one folder (OLD or CALM). Blender axes: the pieces face -Y."""
    p = lambda n: os.path.join(root, n + ".glb")
    obs = []
    H, RUN = 1.15, 1.15 / math.tan(math.radians(35))
    rows = ["young", "young", "ripe", "young", "flooded", "young"]
    for i, v in enumerate(rows):
        for col in (0, 1, 2):
            vv = v if col == 1 else rows[(i + col + 5) % 6] if i % 2 else v
            obs += load(p("terrace_straight_" + vv), (col * 12.0 - 6.0, i * RUN, i * H))
    # the pit wall to the left (two modules, one niche), the cliff and the medium fall to the right
    obs += load(p("pit_wall"), (-30.0, -2.0, 0)) + load(p("pit_wall_niche"), (-18.0, -2.0, 0))
    obs += load(p("cliff_wall"), (30.0, 1.0, 0)) + load(p("fall_medium"), (36.0, 0.2, 0))
    # open ground in front: grass tiles, a path, boulders and cover
    for gx in (-30.0, -18.0, -6.0, 6.0, 18.0, 30.0):
        obs += load(p("ground_grass_tile" if gx < 18 else "ground_forest_tile"), (gx, -14.0, -0.03), thick=0.0)
    obs += load(p("path_stones"), (4.0, -14.0, 0.0)) + load(p("path_stones"), (4.0, -8.0, 0.0))
    obs += load(p("boulder_3"), (-8.0, -6.0, 0)) + load(p("boulder_5"), (24.0, -8.0, 0)) + load(p("boulder_2"), (12.0, -4.5, 0))
    obs += load(p("cover_fern"), (-4.0, -4.0, 0)) + load(p("cover_taro"), (16.0, -3.0, 0)) + load(p("cover_grass"), (1.0, -3.5, 0))
    obs += load(p("terrace_scarecrow"), (9.0, 1.2 * RUN + 0.9, 1.15 + 0.05))
    return obs


def kit():
    sc, cam = stage((1200, 680))
    ground_plane()
    t35 = math.tan(math.radians(35))
    hill = flat("hill", (0.30, 0.38, 0.22))
    RUN = 1.15 / t35
    plane("slope", [(-6, -0.2, 0), (30, -0.2, 0), (30, 6 * RUN + 0.2, 6 * 1.15), (-6, 6 * RUN + 0.2, 6 * 1.15)], hill)
    plane("top", [(-6, 6 * RUN + 0.2, 6 * 1.15), (30, 6 * RUN + 0.2, 6 * 1.15), (30, 6 * RUN + 8, 6 * 1.15 + 0.6), (-6, 6 * RUN + 8, 6 * 1.15 + 0.6)], hill)
    A = kit_scene(OLD)
    B = kit_scene(CALM)
    views = {
        "below": ((12.0, -11.0, 1.7), (12.0, 4.0, 4.2), 52),
        "40m": ((12.0 + 40 * 0.25, -40.0, 9.0), (6.0, 2.0, 4.0), 46),
        "150m": ((6.0 + 150 * 0.25, -150 * 0.97, 30.0), (6.0, 2.0, 4.0), 22),
    }
    for k, (eye, look, fov) in views.items():
        aim(cam, Vector(eye), look, fov)
        show(A, True); show(B, False); shoot(sc, "kit_old_" + k)
        show(A, False); show(B, True); shoot(sc, "kit_calm_" + k)


if args[0] == "piece":
    for n in args[1:]:
        piece(n)
else:
    kit()
print("done")
