"""PAYO: a quick ORGANIC VALLEY BLOCKOUT of the land. Blender only, nothing for the game yet.

Owner, 2026-10-12, in order:
  "before laying anything out. can you give me a terrain rework"
  "i need you to design an organic valley-style blockout in 5-15 minutes" (with photographs of real rice terraces)
  "make the terraces taller. the play area will face the open area of the valley, where we can see the horizon and
   sunset. behind it the play area is a mountain"
  of v3 (a ring of separate rock towers round a mound; tools/payo_valley_blockout_v3.py): "i need a more organic layout.
   an actual valley, not a bunch of mountain hils"

  blender -b --python tools/payo_valley_blockout.py -- [tag]

v4 is ONE VALLEY: two continuous valley sides that run the whole way, folding in and out as interlocking spurs, closed at
its HEAD by the mountain (behind the court) and opening at its MOUTH to the plain and the horizon (in front of it). The
valley winds; its floor widens and falls toward the mouth; a river runs down it. The court is on the top step of the
valley floor at the head. Terraces are the ground itself, stepped in tall steps along its contours: across the floor in
front of the court and up the lower valley sides. Grey boxes mark the court, the pit and the gate.
"""
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TAG = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv and len(sys.argv) > sys.argv.index("--") + 1 else "v4"
OUT = os.path.join(ROOT, "Logs", "payo", "terrain")
os.makedirs(OUT, exist_ok=True)

HALF, CELL = 1150.0, 3.0
Y0, Y1 = -1000.0, 2800.0
NX, NY = int(2 * HALF / CELL) + 1, int((Y1 - Y0) / CELL) + 1
STEP = 5.0                                        # a terrace's wall: tall chunky steps ("make the terraces taller")


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def noise(X, Y, wave, seed):
    """Smooth value noise in -1..1 with features about `wave` metres across."""
    r = np.random.default_rng(seed)
    n = int((Y1 - Y0) / wave) + 4
    g = r.random((n, n)) * 2 - 1
    u = (X + HALF) / wave + 1
    v = (Y - Y0) / wave + 1
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


xs = np.linspace(-HALF, HALF, NX)
ys = np.linspace(Y0, Y1, NY)
X, Y = np.meshgrid(xs, ys)                        # x east, y north. The court at the origin faces NORTH, down the valley

# ---- the valley's line: it starts at the head (just behind the court) and winds away to the north
HEAD_Y = -70.0
Yc = np.maximum(Y, HEAD_Y)
line_x = 340 * np.sin((Yc - HEAD_Y - 480) / 270.0) * smooth(480, 760, Yc - HEAD_Y)       # straight, then it bends east, then west
line_x = line_x + 34 * np.sin((Yc - HEAD_Y - 150) / 90.0) * smooth(170, 330, Yc - HEAD_Y)       # the river's own small loops
across = X - line_x                                # + the east side, - the west side
d = np.sqrt(across ** 2 + np.minimum(Y - HEAD_Y, 0.0) ** 2)      # distance from the valley's line; round at the head

# ---- the two sides fold in and out (interlocking spurs and side gullies), each side its own
spurs = 70 * fbm(X, Y, 260, 21, 3) + 30 * fbm(X, Y, 110, 44, 3)
d_eff = np.maximum(d + spurs * smooth(40, 200, d), 0.0)
along = Y - HEAD_Y
half_floor = 9.0 + 0.0 * along                                     # no flat floor: the sides come down to the river
side_run = 330 + 0.06 * np.maximum(along, 0.0)                    # how far it is from the floor's edge to the ridge
ridge = (215 + 0.04 * np.maximum(along, 0.0)) * (1 + 0.22 * fbm(X, Y, 340, 60, 2))      # ridges fall toward the mouth
ridge = ridge * np.where(across < 0, 1.0 - 0.45 * np.exp(-((Y - 330) / 90.0) ** 2), 1.0)  # a side valley joins from the west
s = (d_eff - half_floor) / side_run
wall = ridge * (0.36 * np.clip(s, 0.0, 1.0) + 0.64 * smooth(0.0, 1.0, s))        # sloping from the river's edge
beyond = np.maximum(s - 1.0, 0.0)
wall = wall * (1.0 - 0.25 * np.clip(beyond, 0, 1.5)) + 22 * fbm(X, Y, 150, 75, 3) * smooth(0.25, 1.0, s)
gully = np.abs(fbm(X, Y, 85, 91, 3))                              # gullies cut down the sides
wall = wall - 26 * (1 - smooth(0.0, 0.22, gully)) * smooth(0.15, 0.6, s) * smooth(1.3, 0.8, s)

top = np.zeros_like(wall)

# ---- the valley floor: level at the head (the court), then stepping down toward the mouth
floor = (-6.0 - 36.0 * smooth(40, 150, along + 0.06 * np.abs(across)) - 56.0 * smooth(150, 1150, along + 0.06 * np.abs(across))
         - 0.010 * np.maximum(along - 1150, 0.0))
floor = floor + 1.2 * fbm(X, Y, 140, 3, 2)
far_range = 330.0 * smooth(2150, 2750, Y + 0.25 * np.abs(X)) * (1 + 0.25 * fbm(X, Y, 300, 210, 2))
Z = floor + np.maximum(wall, far_range)
flat = smooth(56, 40, np.sqrt(X ** 2 + Y ** 2)) * smooth(27, 19, Y)
Z = Z * (1 - flat) - 6.0 * flat

# ---- the river: from the fall at the head, past the court's east side, then loops that grow as the floor widens
river_x = line_x
rd = np.abs(X - river_x)
bed = smooth(10.0, 4.0, rd) * smooth(118, 140, along) * smooth(0.25, 0.0, s)

# ---- rice terraces: the ground's own height, stepped along its contours
gy, gx = np.gradient(Z, CELL)
slope = np.hypot(gx, gy)
patch = fbm(X, Y, 190, 150, 2)
terr = smooth(1.05, 0.80, slope) * smooth(0.012, 0.035, slope) * smooth(34, 44, np.sqrt(X ** 2 + Y ** 2))
terr = terr * smooth(0.74, 0.62, s + 0.05 * patch)                # the floor and the lower sides; forest above
level = np.floor(Z / STEP)
frac = Z / STEP - level
stepped = (level + smooth(0.85, 1.0, frac)) * STEP
Zt = Z * (1 - terr) + stepped * terr
terr_on = terr > 0.5
Zt = Zt - 3.0 * bed
pit = (np.abs(X) < 30) & (np.abs(Y) < 30)
Zt = np.where(pit, np.minimum(Zt, -30.0), Zt)

# ---- colour
gy2, gx2 = np.gradient(Zt, CELL)
slope2 = np.hypot(gx2, gy2)
col = np.zeros((NY, NX, 3), np.float32)
grass = np.array([0.38, 0.62, 0.22]); forest = np.array([0.11, 0.36, 0.18]); rock = np.array([0.55, 0.56, 0.58])
col[:] = grass
woods = smooth(0.0, 0.5, fbm(X, Y, 80, 130, 3) * 0.8 + smooth(0.3, 0.8, s) * 1.1 - 0.15)
col = col * (1 - woods[..., None]) + forest * woods[..., None]
rk = smooth(1.0, 1.6, slope2) * (1 - terr)
col = col * (1 - rk[..., None]) + rock * rk[..., None]
ang = 0.5
U, V = (X * math.cos(ang) + Y * math.sin(ang)) / 34.0, (-X * math.sin(ang) + Y * math.cos(ang)) / 26.0
cell = (np.floor(U) * 7 + np.floor(V) * 13).astype(int)
fields = np.array([[0.62, 0.80, 0.30], [0.86, 0.80, 0.42], [0.86, 0.46, 0.30], [0.40, 0.70, 0.26], [0.74, 0.86, 0.46], [0.90, 0.72, 0.30]])
edge = (np.abs(U - np.floor(U) - 0.5) > 0.44) | (np.abs(V - np.floor(V) - 0.5) > 0.44)
farm = (fbm(X, Y, 150, 170, 2) > -0.25) & (s < 0.03) & (bed < 0.1) & ~terr_on & (along > 99999)
col = np.where(farm[..., None], np.where(edge[..., None], grass * 0.8, fields[cell % len(fields)]), col)
crops = np.array([[0.50, 0.80, 0.24], [0.34, 0.68, 0.22], [0.90, 0.80, 0.40], [0.86, 0.48, 0.30], [0.60, 0.80, 0.82],
                  [0.72, 0.86, 0.44], [0.92, 0.70, 0.28], [0.42, 0.74, 0.26]])
lev = level.astype(int)
plot_len = 70.0 + 45.0 * ((lev * 37) % 5) / 4.0                    # each terrace is cut into plots of its own length
plot_u = (Y + 23.0 * ((lev * 53) % 7) + 26.0 * fbm(X, Y, 200, 150, 2)) / plot_len
field = (lev * 3 + np.floor(plot_u).astype(int) * 5 + 400) % len(crops)
dike = np.abs(plot_u - np.floor(plot_u) - 0.5) > 0.47
tcol = np.where((frac > 0.85)[..., None], np.array([0.42, 0.40, 0.38]), np.where(dike[..., None], np.array([0.30, 0.50, 0.20]), crops[field]))
col = np.where(terr_on[..., None], tcol, col)
col = np.where((bed > 0.4)[..., None], np.array([0.26, 0.56, 0.84]), col)
col = np.where(((np.sqrt(X ** 2 + Y ** 2) < 36) & ~pit & (Y < 20))[..., None], np.array([0.56, 0.57, 0.55]), col)
col = np.where(pit[..., None], np.array([0.10, 0.11, 0.13]), col)
haze = (0.50 * smooth(500, 2600, Y))[..., None]
col = col * (1 - haze) + np.array([0.86, 0.70, 0.62]) * haze

# ---- Blender
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
me = bpy.data.meshes.new("valley")
verts = np.stack([X.ravel(), Y.ravel(), Zt.ravel()], axis=1).astype(np.float32)
idx = np.arange(NX * NY).reshape(NY, NX)
quads = np.stack([idx[:-1, :-1].ravel(), idx[:-1, 1:].ravel(), idx[1:, 1:].ravel(), idx[1:, :-1].ravel()], axis=1).astype(np.int32)
me.vertices.add(len(verts)); me.vertices.foreach_set("co", verts.ravel())
me.loops.add(quads.size); me.loops.foreach_set("vertex_index", quads.ravel())
me.polygons.add(len(quads))
me.polygons.foreach_set("loop_start", np.arange(0, quads.size, 4, dtype=np.int32))
me.polygons.foreach_set("loop_total", np.full(len(quads), 4, np.int32))
me.update(); me.validate()
ca = me.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
rgba = np.concatenate([col.reshape(-1, 3) ** 2.2, np.ones((NX * NY, 1), np.float32)], axis=1).astype(np.float32)
ca.data.foreach_set("color", rgba.ravel())
land = bpy.data.objects.new("Valley land", me); sc.collection.objects.link(land)


def material(name, colour=None, vertex=False, glow=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 1.0
    if vertex:
        n = m.node_tree.nodes.new("ShaderNodeVertexColor"); n.layer_name = "Col"
        m.node_tree.links.new(n.outputs["Color"], bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = (*[v ** 2.2 for v in colour], 1)
        if glow:
            bsdf.inputs["Emission Color"].default_value = (*[v ** 2.2 for v in colour], 1)
            bsdf.inputs["Emission Strength"].default_value = glow
    return m


me.materials.append(material("land", vertex=True))


def box(name, size, at, colour):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at)
    o = bpy.context.object; o.name = name; o.scale = size
    o.data.materials.append(material(name, colour))
    return o


def ground(x, y):
    return float(Zt[int(round((y - Y0) / CELL)), int(round((x + HALF) / CELL))])


box("Court (44 m play square)", (44, 44, 60), (0, 0, -30), (0.62, 0.63, 0.62))
box("Gate (stand-in)", (18, 5, 15), (0, -54, ground(0, -54) + 7.5), (0.78, 0.60, 0.36))
box("Person 1.75 m", (0.5, 0.3, 1.75), (7, 4, 0.875), (0.9, 0.3, 0.2))
box("Great fall (stand-in)", (8, 3, 70), (60, -215, ground(60, -215) - 25), (0.80, 0.92, 0.98))
for k, (x, y) in enumerate([(-58, -62), (56, -70), (-30, -92), (36, -96), (84, -30), (-88, -24), (-120, 90), (140, 130)]):
    box("House stand-in %d" % k, (8, 8, 9), (x, y, ground(x, y) + 4.5), (0.80, 0.50, 0.40))
bpy.ops.mesh.primitive_plane_add(size=16000, location=(0, 4000, float(Zt.min()) - 4.0))
plain = bpy.context.object; plain.name = "Low ground under everything (not seen from the court)"
plain.data.materials.append(material("plain", (0.30, 0.42, 0.30)))
bpy.ops.mesh.primitive_uv_sphere_add(radius=150, location=(0, 5200, 900))
sd = bpy.context.object; sd.name = "Setting sun"; sd.data.materials.append(material("sun", (1.0, 0.80, 0.45), glow=8))

sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.98, 0.70, 0.46, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 3.4; sun.data.color = (1.0, 0.82, 0.60); sun.rotation_euler = (math.radians(66), 0, math.radians(215))
sc.render.resolution_x, sc.render.resolution_y = 1280, 720

cams = {}


def camera(name, at, to, lens=28, ortho=None):
    cam = bpy.data.objects.new(name, bpy.data.cameras.new(name)); sc.collection.objects.link(cam)
    cam.location = at
    cam.rotation_euler = (Vector(to) - Vector(at)).to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = lens; cam.data.clip_end = 14000
    if ortho:
        cam.data.type = "ORTHO"; cam.data.ortho_scale = ortho
    cams[name] = cam
    return cam


camera("Top", (0, 900, 2500), (0, 900.01, 0), ortho=3900)
camera("Air up the valley (from the mouth, looking at the head)", (120, 980, 300), (0, 120, -20), 30)
camera("Air down the valley (from over the head, to the horizon)", (-30, -330, 420), (30, 700, -70), 22)
camera("Air from the east side", (900, 330, 440), (-40, 250, -20), 28)
camera("Eye court looking north (down the valley, the sunset)", (0, -10, 1.7), (0, 300, 22), 16)
camera("Eye court front edge looking north", (0, 20, 1.7), (0, 300, -40), 16)
camera("Over the court looking down the valley", (0, -60, 34), (0, 400, -70), 22)
camera("Eye court looking south (the gate and the mountain)", (0, 10, 1.7), (0, -120, 60), 16)
camera("Air the head and its mountain (from the west side)", (-520, 330, 300), (40, -160, 60), 28)
camera("Eye court looking east", (-8, 0, 1.7), (160, 60, 40), 16)
camera("Eye court looking west", (8, 0, 1.7), (-160, 60, 40), 16)
sc.camera = cams["Air up the valley (from the mouth, looking at the head)"]

path = os.path.join(ROOT, "ArtSource", "payo", "payo_valley_blockout_%s.blend" % TAG)
bpy.ops.wm.save_as_mainfile(filepath=path, compress=True)
for name, cam in cams.items():
    sc.camera = cam
    sc.render.filepath = os.path.join(OUT, "valley_%s_%s.png" % (TAG, name.split(" (")[0].lower().replace(" ", "_")))
    bpy.ops.render.render(write_still=True)
print("SAVED", path, "relief %.0f to %.0f m" % (Zt.min(), Zt.max()))
