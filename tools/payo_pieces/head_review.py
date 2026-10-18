"""Review renders of the valley's head (tools/payo_pieces/head_build.py). Blender 5, headless.

  blender -b --factory-startup --python tools/payo_pieces/head_review.py [-- <tag>]

The calm review stage (calm_review.py / terrace_calm_review.py: a low warm sun, a cool fill, a soft blue sky, a little
of the paint as its own light, the ink line as a separate hull that casts no shadow), with the piece in the map's own
frame (glb origin = pool centre at z 0, so local y = map y + 208) and a PROXY hillside pushed against it at the
grounds the brief gives (north 46 falling toward the court, east 60..80, west 52..56, south 70..90), cut away only
inside the pool and the outlet, so the shots show where land meets rock. Then calm_sheet.grid lays out the sheet.
"""
import math
import os
import sys

import bpy
import bmesh
import numpy as np
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
HEAD = os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "head")
SHOTS = os.path.join(HEAD, "shots")
os.makedirs(SHOTS, exist_ok=True)
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
TAG = args[0] if args else "r"
NOINK = {"py_terrace_plant", "py_water", "py_fall", "py_wet", "py_mist", "py_foam"}


def lin(c):
    return tuple(((v + 0.055) / 1.055) ** 2.4 if v > 0.04045 else v / 12.92 for v in c)


def stage(res=(1280, 720)):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    try:
        sc.eevee.shadow_pool_size = "1024"
    except Exception:
        pass
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.68, 0.80, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    for rot, energy, col in (((66, 0, -42), 3.4, (1.0, 0.80, 0.58)), ((120, 0, 150), 0.6, (0.62, 0.74, 1.0))):
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = energy; sun.data.color = col; sun.rotation_euler = tuple(math.radians(a) for a in rot)
        sun.data.angle = math.radians(5)
        try:
            sun.data.shadow_filter_radius = 3.0
        except AttributeError:
            pass
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
    cam.data.clip_end = 4000; cam.data.clip_start = 0.2
    sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = res
    return sc, cam


def flat(name, colour, emit=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = lin(colour) + (1,); b.inputs["Roughness"].default_value = 1
    b.inputs["Emission Color"].default_value = lin(colour) + (1,); b.inputs["Emission Strength"].default_value = emit
    return m


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
        b.inputs["Emission Strength"].default_value = {"py_terrace_plant": 0.30, "py_fall": 0.6, "py_foam": 0.7, "py_mist": 0.8}.get(name, 0.22)
    if name in ("py_fall", "py_mist"):
        m.surface_render_method = "DITHERED" if name == "py_fall" else "BLENDED"
        if name == "py_mist":
            b.inputs["Alpha"].default_value = 0.72
    m.use_backface_culling = name not in ("py_terrace_plant", "py_fall", "py_mist")


_ink = []


def ink(o, thick):
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


def load(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    meshes = [o for o in new if o.type == "MESH"]
    for o in meshes:
        mats = {m.name.split(".")[0] for m in o.data.materials if m}
        for m in o.data.materials:
            if m:
                dress(m)
        if not mats & NOINK:
            ink(o, 0.05)
    return meshes


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def proxy_land():
    """The grounds the brief names, as one height field, cut only where the pool and the outlet are."""
    xs = np.arange(-170.0, 170.01, 1.0)
    ys = np.arange(-130.0, 215.01, 1.0)
    X, Y = np.meshgrid(xs, ys, indexing="xy")
    north = 46.0 - 0.25 * np.clip(Y - 22.0, 0, None)
    side = np.where(X > 0, 60.0 + np.clip((X - 22.0) * 0.7, 0, 20), 54.0 + 0.0 * X)
    ws = smooth(16, 30, np.abs(X)) * (1 - smooth(12, 60, Y))
    G = north + (side - north) * ws
    south = 70.0 + np.clip((-Y - 22.0) * 0.4, 0, 20) + 3.0 * np.sin(X / 23.0)
    G = G + (south - G) * smooth(-12, -32, Y)
    G = G + 0.8 * np.sin(X / 7.3) * np.cos(Y / 9.1)
    r = np.hypot(X, Y)
    G = np.where(r < 20.4, 40.0, G)
    ch = (np.abs(X) < 6.5) & (Y > 13.0)
    stair = 43.2 - 0.25 * np.clip(Y - 19.5, 0, None) + np.clip((np.abs(X) - 3.0) / 3.0, 0, 1) * 1.8
    G = np.where(ch, np.minimum(G, stair), G)
    ny, nx = G.shape
    verts = np.stack([X.ravel(), Y.ravel(), G.ravel()], 1)
    idx = np.arange(nx * ny).reshape(ny, nx)
    quads = np.stack([idx[:-1, :-1].ravel(), idx[:-1, 1:].ravel(), idx[1:, 1:].ravel(), idx[1:, :-1].ravel()], 1)
    me = bpy.data.meshes.new("land"); me.from_pydata(verts.tolist(), [], quads.tolist())
    for p in me.polygons:
        p.use_smooth = True
    o = bpy.data.objects.new("land", me); bpy.context.scene.collection.objects.link(o)
    me.materials.append(flat("land", (0.42, 0.60, 0.28), 0.10))
    # the stair's water, beyond the outlet
    sw = bpy.data.meshes.new("stairwater")
    yy = np.arange(19.0, 200.0, 4.0)
    v = []
    for y in yy:
        z = 44.3 - 0.25 * max(0.0, y - 19.5)
        v += [(-4.9, y, z), (4.9, y, z)]
    f = [(2 * i, 2 * i + 1, 2 * i + 3, 2 * i + 2) for i in range(len(yy) - 1)]
    sw.from_pydata(v, [], f)
    wm = bpy.data.materials.new("py_water"); wm.use_nodes = True; dress(wm)
    sw.materials.append(wm)
    so = bpy.data.objects.new("stairwater", sw); bpy.context.scene.collection.objects.link(so)
    return [o, so]


def aim(cam, eye, look, lens=35.0):
    cam.data.sensor_fit = "HORIZONTAL"; cam.data.sensor_width = 36.0; cam.data.lens = lens
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()


def shoot(sc, name):
    sc.render.filepath = os.path.join(SHOTS, name + ".png")
    bpy.ops.render.render(write_still=True)


M = Vector((0, 208.0, 0))     # map y + 208 = the piece's own y
SHOTS_DEF = [
    ("court_eye", (0, -20, 10), (0, -215, 55), 40),
    ("west_high", (-70, -170, 80), (0, -214, 56), 35),
    ("north_mid", (0, -150, 70), (0, -218, 58), 35),
    ("rim_person", (-9.5, -192.6, 46.2 + 1.75), (0, -222, 60), 24),
    ("above", (0, -196, 235), (0, -214, 50), 35),
    ("east_high", (62, -150, 100), (0, -214, 58), 35),
    ("lip_close", (16, -196, 84), (0, -223.5, 73), 35),
]

def in_map(blend):
    """The piece in the dressed map (ArtSource/payo/payo_dressed_d40.blend, opened read-only, never saved): the old fall
    and basin water removed, the old land cut away only round the pool and the outlet (r < 24 m), the map's own light
    and cameras. The land is NOT yet fitted to the piece; this shows colour and scale in the map's light."""
    import bpy as B
    B.ops.wm.open_mainfile(filepath=blend)
    for o in list(B.data.objects):
        if o.name.startswith("fall_great") or o.name == "Basin water":
            B.data.objects.remove(o, do_unlink=True)
    for name in ("Land", "Land terraces"):
        o = B.data.objects.get(name)
        if o is None:
            continue
        bm = bmesh.new(); bm.from_mesh(o.data)
        mw = o.matrix_world
        cut = []
        for f in bm.faces:
            c = mw @ f.calc_center_median()
            lx, ly = c.x, c.y + 208.0
            if (math.hypot(lx, ly) < 24.0 and ly < 20.0) or (abs(lx) < 7.0 and 12.0 < ly < 20.0):
                cut.append(f)
        bmesh.ops.delete(bm, geom=cut, context="FACES")
        bm.to_mesh(o.data); bm.free()
    before = set(B.data.objects)
    meshes = load(os.path.join(HEAD, "head_fall.glb"))
    for o in B.data.objects:
        if o not in before and o.parent is None:
            o.location = Vector((0.0, -208.0, 0.0))
    sc = B.context.scene
    for cname in ("eye_court_looking_south", "head_from_the_court_rim"):
        c = B.data.objects.get(cname)
        if c is None:
            continue
        sc.camera = c
        shoot(sc, "%s_map_%s" % (TAG, cname))
    hc = B.data.objects.new("hc", B.data.cameras.new("hc")); sc.collection.objects.link(hc)
    hc.data.clip_end = 4000
    aim(hc, Vector((-70, -170, 80)), Vector((0, -214, 56)), 35); sc.camera = hc
    shoot(sc, "%s_map_west_high" % TAG)


if __name__ == "__main__" and "map" in args:
    in_map(os.path.join(ROOT, "ArtSource", "payo", "payo_dressed_d40.blend"))
elif __name__ == "__main__":
    sc, cam = stage()
    land = proxy_land()
    piece = load(os.path.join(HEAD, "head_fall.glb"))
    for name, eye, look, lens in SHOTS_DEF:
        aim(cam, Vector(eye) + M, Vector(look) + M, lens)
        shoot(sc, "%s_%s" % (TAG, name))
    # the normal map alone: every stone surface flat grey, lit, from the person at the rim and from the west
    for o in piece:
        for m in o.data.materials:
            if m and m.name.split(".")[0] == "py_head_rock":
                b = m.node_tree.nodes.get("Principled BSDF")
                for l in list(b.inputs["Base Color"].links) + list(b.inputs["Emission Color"].links):
                    m.node_tree.links.remove(l)
                b.inputs["Base Color"].default_value = (0.36, 0.36, 0.34, 1); b.inputs["Emission Color"].default_value = (0.36, 0.36, 0.34, 1)
    for name, eye, look, lens in SHOTS_DEF:
        if name in ("rim_person", "west_high"):
            aim(cam, Vector(eye) + M, Vector(look) + M, lens)
            shoot(sc, "%s_%s_normalonly" % (TAG, name))
    print("done")
