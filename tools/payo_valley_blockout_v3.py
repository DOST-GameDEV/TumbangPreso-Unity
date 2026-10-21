"""PAYO: a quick ORGANIC VALLEY BLOCKOUT of the land. Blender only, nothing for the game yet.

Owner, 2026-10-12, of the generated land (a bowl of fan-shaped fields ending in sheer cut faces): "before laying anything
out. can you give me a terrain rework", then, with three photographs of real rice terraces: "i'm also not asking for a
full terrain polished right now. i need you to design an organic valley-style blockout in 5-15 minutes".

  blender -b --python tools/payo_valley_blockout.py -- [tag]

What it is: one continuous valley. A river winds along the valley floor; the land rises on both sides into spurs and
ridges and goes on past every view (nothing is cut off). The court stands on a low shelf on the south bank; the slope
behind it (the gate's side) and the slope across the river are RICE TERRACES, and they are the ground itself: the
hillside's own height is stepped along its contours, so every paddy follows the land's folds, as in his photographs.
Grey boxes mark the court, the pit, the gate. Writes ArtSource/payo/payo_valley_blockout_v3.blend and pictures in
Logs/payo/terrain/.
"""
import math
import os
import sys

import bpy
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TAG = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv and len(sys.argv) > sys.argv.index("--") + 1 else "v1"
OUT = os.path.join(ROOT, "Logs", "payo", "terrain")
os.makedirs(OUT, exist_ok=True)

HALF, CELL = 900.0, 3.0
N = int(2 * HALF / CELL) + 1
STEP = 5.0                                        # owner: "make the terraces taller": big chunky steps, as in his picture
rng = np.random.default_rng(11)


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def noise(X, Y, wave, seed):
    """Smooth value noise in -1..1 with features about `wave` metres across."""
    r = np.random.default_rng(seed)
    n = int(2 * HALF / wave) + 4
    g = r.random((n, n)) * 2 - 1
    u = (X + HALF) / wave + 1
    v = (Y + HALF) / wave + 1
    i, j = np.floor(u).astype(int), np.floor(v).astype(int)
    fu, fv = u - i, v - j
    fu, fv = fu * fu * (3 - 2 * fu), fv * fv * (3 - 2 * fv)
    i, j = np.clip(i, 0, n - 2), np.clip(j, 0, n - 2)
    return (g[j, i] * (1 - fu) + g[j, i + 1] * fu) * (1 - fv) + (g[j + 1, i] * (1 - fu) + g[j + 1, i + 1] * fu) * fv


def fbm(X, Y, wave, seed, octaves=4):
    out, amp, total = 0.0, 1.0, 0.0
    for k in range(octaves):
        out = out + noise(X, Y, wave / (2 ** k), seed + k * 17) * amp
        total += amp
        amp *= 0.5
    return out / total


xs = np.linspace(-HALF, HALF, N)
X, Y = np.meshgrid(xs, xs)                        # x east, y north. The court at the origin FACES NORTH, the open valley

# THE PLAN (owner, 2026-10-12, with a picture of a river valley among karst towers and one terraced mound: "make the
# terraces taller. the play area will face the open area of the valley, where we can see the horizon and sunset. behind
# it the play area is a mountain"). So: a MOUNTAIN stands behind the court (south); the court is on a shelf at its foot;
# from the shelf the ground steps down in big terraces to a wide flat VALLEY FLOOR of fields and a winding river that
# runs away north to an open HORIZON, where the sun sets; rock towers stand along the valley's two sides only.
FLOOR = -22.0
warp = 60 * fbm(X, Y, 260, 7, 2)
Z = FLOOR + 1.5 * fbm(X, Y, 180, 3, 2) - 0.004 * (Y + HALF)          # the valley floor, falling gently to the north

# ---- the mountain behind, and the towers along the two sides (steep rock, forest on top)
PEAKS = [(0, -330, 250, 250), (-270, -360, 190, 200), (290, -380, 200, 215), (-120, -560, 260, 290), (190, -600, 260, 270),
         (-360, -40, 105, 130), (-470, 200, 120, 150), (-390, 420, 100, 110), (-560, -180, 150, 190),
         (350, 40, 110, 140), (480, 260, 115, 135), (400, 470, 95, 100), (570, -140, 150, 180),
         (-620, 560, 130, 120), (650, 600, 130, 115)]
cliff = np.zeros_like(Z)
for k, (px, py, pr, ph) in enumerate(PEAKS):
    r = np.sqrt((X - px + warp * 0.5) ** 2 + (Y - py + 40 * fbm(X, Y, 200, 30 + k, 2)) ** 2)
    r = r * (1 + 0.22 * fbm(X, Y, 90, 60 + k, 3))
    body = smooth(pr, pr * 0.62, r)
    cliff = np.maximum(cliff, ph * body * (0.82 + 0.18 * smooth(pr * 0.6, 0.0, r)) + 16 * fbm(X, Y, 70, 90 + k, 3) * body)
Z = Z + cliff

# ---- the court's shelf at the mountain's foot, and the terraced fan that steps down from it to the valley floor
R = np.sqrt(X ** 2 + Y ** 2) * (1 + 0.16 * fbm(X, Y, 150, 120, 2))
mound = (0.0 - FLOOR) * smooth(230, 62, R)                                                  # a fan, open to the north
apron = 26.0 * smooth(-40, -110, Y) * smooth(300, 120, np.abs(X))                           # rising to the cliff's foot
foot = (0.0 - FLOOR) * smooth(-30, -60, Y) * smooth(330, 200, np.abs(X))                    # the ground behind is not low
base = FLOOR + np.maximum(mound, foot) + apron
Z = np.maximum(Z, base + 1.5 * fbm(X, Y, 180, 3, 2))
flat = smooth(62, 46, np.sqrt(X ** 2 + Y ** 2))
Z = Z * (1 - flat)

# ---- the river: out from beside the mountain, round the foot of the terraced fan, then away north in loops
river_x = 250 * np.exp(-((Y - 20) / 190.0) ** 2) + 95 * np.sin(Y / 120.0 + 0.4) + 45 * np.sin(Y / 52.0 + 2.0) - 30
d = X - river_x
on_floor = smooth(9.0, 1.0, Z - FLOOR)
bed = smooth(13.0, 5.0, np.abs(d)) * on_floor * smooth(-200, -120, Y)

# ---- rice terraces: the ground's own height, stepped along its contours, in tall steps
gy, gx = np.gradient(Z, CELL)
slope = np.hypot(gx, gy)
terr = smooth(1.0, 0.75, slope) * smooth(0.03, 0.09, slope) * smooth(2.0, 5.0, Z - FLOOR) * smooth(46, 60, np.sqrt(X ** 2 + Y ** 2))
terr = terr * smooth(60, 34, cliff)
level = np.floor(Z / STEP)
frac = Z / STEP - level
stepped = (level + smooth(0.84, 1.0, frac)) * STEP
Zt = Z * (1 - terr) + stepped * terr
terr_on = terr > 0.5
Zt = Zt - 2.5 * bed
pit = (np.abs(X) < 30) & (np.abs(Y) < 30)
Zt = np.where(pit, -26.0, Zt)

# ---- colour
gy2, gx2 = np.gradient(Zt, CELL)
slope2 = np.hypot(gx2, gy2)
col = np.zeros((N, N, 3), np.float32)
grass = np.array([0.36, 0.62, 0.22]); forest = np.array([0.10, 0.36, 0.18]); rock = np.array([0.56, 0.55, 0.62])
col[:] = grass
woods = smooth(0.05, 0.5, fbm(X, Y, 80, 130, 3) + smooth(20, 80, cliff) * 1.2 - 0.2)
col = col * (1 - woods[..., None]) + forest * woods[..., None]
rk = smooth(0.9, 1.5, slope2) * (1 - terr)
col = col * (1 - rk[..., None]) + rock * rk[..., None]
# the valley floor's fields: a patchwork, in clusters, as in his picture
ang = 0.5
U, V = (X * math.cos(ang) + Y * math.sin(ang)) / 34.0, (-X * math.sin(ang) + Y * math.cos(ang)) / 26.0
cell = (np.floor(U) * 7 + np.floor(V) * 13).astype(int)
fields = np.array([[0.62, 0.80, 0.30], [0.86, 0.80, 0.42], [0.86, 0.44, 0.30], [0.40, 0.70, 0.26], [0.74, 0.86, 0.46], [0.90, 0.72, 0.30]])
edge = (np.abs(U - np.floor(U) - 0.5) > 0.44) | (np.abs(V - np.floor(V) - 0.5) > 0.44)
farm = (smooth(0.0, 0.25, fbm(X, Y, 150, 170, 2)) > 0.5) & (Zt - FLOOR < 2.5) & (bed < 0.1) & (cliff < 2)
fcol = np.where(edge[..., None], grass * 0.8, fields[cell % len(fields)])
col = np.where(farm[..., None], fcol, col)
crops = np.array([[0.50, 0.80, 0.24], [0.34, 0.68, 0.22], [0.88, 0.74, 0.26], [0.58, 0.78, 0.80], [0.44, 0.74, 0.26]])
field = (np.floor(level + 40).astype(int)) % len(crops)
field = (field + (fbm(X, Y, 160, 150, 2) * 2.5).astype(int)) % len(crops)
wall = np.array([0.42, 0.36, 0.40])
tcol = np.where((frac > 0.84)[..., None], wall, crops[field])
col = np.where(terr_on[..., None], tcol, col)
col = np.where((bed > 0.4)[..., None], np.array([0.26, 0.56, 0.84]), col)
col = np.where(((np.sqrt(X ** 2 + Y ** 2) < 47) & ~pit)[..., None], np.array([0.56, 0.57, 0.55]), col)
col = np.where(pit[..., None], np.array([0.10, 0.11, 0.13]), col)

# ---- Blender
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
me = bpy.data.meshes.new("valley")
verts = np.stack([X.ravel(), Y.ravel(), Zt.ravel()], axis=1).astype(np.float32)
i = np.arange(N * N).reshape(N, N)
a, b, c, e = i[:-1, :-1].ravel(), i[:-1, 1:].ravel(), i[1:, 1:].ravel(), i[1:, :-1].ravel()
quads = np.stack([a, b, c, e], axis=1).astype(np.int32)
me.vertices.add(len(verts)); me.vertices.foreach_set("co", verts.ravel())
me.loops.add(quads.size); me.loops.foreach_set("vertex_index", quads.ravel())
me.polygons.add(len(quads))
me.polygons.foreach_set("loop_start", np.arange(0, quads.size, 4, dtype=np.int32))
me.polygons.foreach_set("loop_total", np.full(len(quads), 4, np.int32))
me.update(); me.validate()
ca = me.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
rgba = np.concatenate([col.reshape(-1, 3) ** 2.2, np.ones((N * N, 1), np.float32)], axis=1).astype(np.float32)
ca.data.foreach_set("color", rgba.ravel())
land = bpy.data.objects.new("Valley land", me); sc.collection.objects.link(land)


def material(name, colour=None, vertex=False):
    m = bpy.data.materials.new(name); m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 1.0
    if vertex:
        n = m.node_tree.nodes.new("ShaderNodeVertexColor"); n.layer_name = "Col"
        m.node_tree.links.new(n.outputs["Color"], bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = (*[v ** 2.2 for v in colour], 1)
    return m


me.materials.append(material("land", vertex=True))


def box(name, size, at, colour):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at)
    o = bpy.context.object; o.name = name; o.scale = size
    o.data.materials.append(material(name, colour))
    return o


def ground(x, y):
    return float(Zt[int(round((y + HALF) / CELL)), int(round((x + HALF) / CELL))])


box("Court (44 m play square)", (44, 44, 26), (0, 0, -13), (0.62, 0.63, 0.62))
box("Gate (stand-in)", (18, 5, 15), (0, -54, ground(0, -54) + 7.5), (0.78, 0.60, 0.36))
box("Person 1.75 m", (0.5, 0.3, 1.75), (7, 4, 0.875), (0.9, 0.3, 0.2))
box("Great fall (stand-in)", (9, 3, 120), (-70, -150, ground(-70, -150) - 50), (0.80, 0.92, 0.98))
for k, (x, y) in enumerate([(-60, -70), (58, -74), (-28, -92), (34, -96), (88, -50), (-92, -44), (-110, 60), (120, 80)]):
    box("House stand-in %d" % k, (8, 8, 9), (x, y, ground(x, y) + 4.5), (0.80, 0.50, 0.40))

sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.98, 0.66, 0.42, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 3.4; sun.data.color = (1.0, 0.80, 0.58); sun.rotation_euler = (math.radians(72), 0, math.radians(205))
sc.render.resolution_x, sc.render.resolution_y = 1280, 720

from mathutils import Vector  # noqa: E402

cams = {}


def camera(name, at, to, lens=28, ortho=None):
    cam = bpy.data.objects.new(name, bpy.data.cameras.new(name)); sc.collection.objects.link(cam)
    cam.location = at
    cam.rotation_euler = (Vector(to) - Vector(at)).to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = lens; cam.data.clip_end = 8000
    if ortho:
        cam.data.type = "ORTHO"; cam.data.ortho_scale = ortho
    cams[name] = cam
    return cam


camera("Top", (0, 0, 1500), (0, 0.01, 0), ortho=1500)
camera("Air from the valley looking at the mountain", (120, 520, 230), (0, -80, 30), 30)
camera("Air from behind the mountain looking down the valley", (-80, -230, 330), (20, 420, -20), 24)
camera("Air from the east side", (560, 120, 260), (-40, -20, 0), 30)
camera("Eye court looking north (the open valley and the sunset)", (0, -10, 1.7), (0, 300, -4), 20)
camera("Eye court looking south (the gate and the mountain)", (0, 10, 1.7), (0, -120, 60), 20)
camera("Eye court looking east", (-8, 0, 1.7), (160, 30, 0), 20)
camera("Eye court looking west", (8, 0, 1.7), (-160, 30, 0), 20)
bpy.ops.mesh.primitive_uv_sphere_add(radius=70, location=(-260, 2600, 150))
sd = bpy.context.object; sd.name = "Setting sun"
sm = bpy.data.materials.new("sun"); sm.use_nodes = True
sm.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (1.0, 0.75, 0.35, 1)
sm.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 8
sd.data.materials.append(sm)
sc.camera = cams["Air from the valley looking at the mountain"]

path = os.path.join(ROOT, "ArtSource", "payo", "payo_valley_blockout_v3.blend")
for area in bpy.context.screen.areas if bpy.context.screen else []:
    pass
bpy.ops.wm.save_as_mainfile(filepath=path, compress=True)
for name, cam in cams.items():
    sc.camera = cam
    sc.render.filepath = os.path.join(OUT, "valley_%s_%s.png" % (TAG, name.split(" (")[0].lower().replace(" ", "_")))
    bpy.ops.render.render(write_still=True)
print("SAVED", path, "relief %.0f to %.0f m" % (Zt.min(), Zt.max()))
