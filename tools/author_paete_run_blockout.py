"""PAETE'S RUN: the grey 3D blockout of the chosen course, built from tools/paete_run_course.py (2026-10-10).

Owner: "i like b"; "alternative routes / harder shortcuts should also be an option"; then "proceed".

  blender -b --python tools/author_paete_run_blockout.py -- --out=<abs folder> [--v=N]

Writes ArtSource/paete_run/paete_run_blockout.blend and paete_run_<view>_vN.png: two views from the air, a plan, and
four from a racer's eye (1.25 m, 95 degrees across). ⚠️ THE COURSE STANDS ON THIN POSTS, WITH NO ROCK UNDER IT, ON
PURPOSE: the first two 3D tries put a rock mass under every leg and the mass hid the course. This picture is for
reading the course's shape, heights and crossings; the rock face it will be cut into comes after he has run on it.

COLOURS ARE A KEY, NOT ART: sand = run; teal = slide chute; yellow beams = things the vine catches over a gap; blue =
water to swim; grey = parkour hops; red = bounce pad; PURPLE = a shortcut. Dark figures are 1.3 m.
"""
import math
import os
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from paete_run_course import COURSE, SHORTCUTS      # noqa: E402
import paete_run_stages as stages                    # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
OUT = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--out=")), ".")
VER = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--v=")), "1")
bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, rgb):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = 0.9
    return m


SAND, ROCK, GROUND = mat("run", (0.86, 0.76, 0.56)), mat("hops", (0.50, 0.46, 0.43)), mat("ground", (0.30, 0.29, 0.31))
CHUTE, WATER, VINE, PAD = mat("chute", (0.14, 0.66, 0.62)), mat("water", (0.22, 0.56, 0.82)), mat("vine", (0.96, 0.80, 0.10)), mat("pad", (0.86, 0.16, 0.12))
DIVE = mat("dive", (0.93, 0.50, 0.14))
CUT, POST, BODY = mat("shortcut", (0.58, 0.26, 0.72)), mat("post", (0.40, 0.38, 0.38)), mat("body", (0.10, 0.10, 0.13))
WHITE, BLACK = mat("white", (0.96, 0.96, 0.96)), mat("black", (0.06, 0.06, 0.06))
FLOOR = min(min(l[1][2], l[2][2]) for l in COURSE) - 9.0


def mesh(name, verts, faces, m):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.materials.append(m)
    bpy.context.scene.collection.objects.link(bpy.data.objects.new(name, me))


def box(x0, x1, y0, y1, z0, z1, m):
    bpy.ops.mesh.primitive_cube_add(size=1, location=((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2))
    o = bpy.context.object
    o.scale = (max(0.02, abs(x1 - x0)), max(0.02, abs(y1 - y0)), max(0.02, abs(z1 - z0)))
    o.data.materials.append(m)


def block(p, half, z0, z1, m):
    box(p[0] - half, p[0] + half, p[1] - half, p[1] + half, z0, z1, m)


def figure(p):
    x, y, z = p
    box(x - 0.2, x + 0.2, y - 0.15, y + 0.15, z, z + 0.85, BODY); box(x - 0.23, x + 0.23, y - 0.2, y + 0.2, z + 0.85, z + 1.3, BODY)


def side_of(a, c):
    d = Vector(c) - Vector(a); d.z = 0
    d = d.normalized() if d.length > 1e-4 else Vector((0, 1, 0))
    return d, Vector((d.y, -d.x, 0))


def bowed(a, c, kind, n=24):
    """The leg's line: a chute bends (the same bend the map draws), the rest are straight."""
    a, c = Vector(a), Vector(c)
    f, s = side_of(a, c)
    flat = math.hypot(c.x - a.x, c.y - a.y)
    amp = 0.22 * flat if kind == "chute" else 0.0
    return [a.lerp(c, k / n) + s * (amp * math.sin(math.pi * k / n)) for k in range(n + 1)]


def strip(pts, half, m, thick=0.5):
    verts, faces = [], []
    n = len(pts)
    for i, p in enumerate(pts):
        f, s = side_of(pts[max(0, i - 1)], pts[min(n - 1, i + 1)])
        for dz in (0.0, -thick):
            verts += [tuple(p - s * half + Vector((0, 0, dz))), tuple(p + s * half + Vector((0, 0, dz)))]
    for i in range(n - 1):
        a = i * 4
        faces += [(a, a + 1, a + 5, a + 4), (a + 6, a + 7, a + 3, a + 2), (a + 2, a, a + 4, a + 6), (a + 1, a + 3, a + 7, a + 5)]
    mesh("strip", verts, faces, m)


def posts(pts, every=14.0):
    run = 0.0
    for a, c in zip(pts, pts[1:]):
        run += (c - a).length
        if run >= every:
            run = 0.0
            block(c, 0.3, FLOOR, c.z - 0.4, POST)


def trough(pts, m, half=2.8, wall=1.4):
    verts, faces = [], []
    n = len(pts)
    for i, p in enumerate(pts):
        a, c = pts[max(0, i - 1)], pts[min(n - 1, i + 1)]
        f, s = side_of(a, c)
        turn = 0.0
        if 0 < i < n - 1:
            d0, d1 = p - a, c - p; d0.z = d1.z = 0
            if d0.length > 1e-4 and d1.length > 1e-4:
                turn = d0.normalized().cross(d1.normalized()).z
        bank = max(-0.6, min(0.6, turn * 7.0))
        for (off, up) in ((-half - 1.0, wall), (-half, 0.0), (half, 0.0), (half + 1.0, wall)):
            verts.append(tuple(p + s * off + Vector((0, 0, up + bank * off * 0.3))))
    for i in range(n - 1):
        for k in range(3):
            a = i * 4 + k
            faces.append((a, a + 1, a + 5, a + 4)); faces.append((a + 4, a + 5, a + 1, a))
    mesh("chute", verts, faces, m)


def leg(kind, a, c, cut=False):
    A, C = Vector(a), Vector(c)
    f, s = side_of(A, C)
    pts = bowed(A, C, kind)
    if kind == "run":
        strip(pts, 2.0 if cut else 4.0, CUT if cut else SAND); posts(pts)
    elif kind == "chute":
        trough(pts, CUT if cut else CHUTE); posts(pts, 16.0)
    elif kind == "swim":
        z = min(A.z, C.z) - 0.4
        flat = [Vector((p.x, p.y, z)) for p in pts]
        strip(flat, 4.5, WATER, thick=0.15); strip([p - Vector((0, 0, 0.15)) for p in flat], 5.2, POST, thick=2.6); posts(flat, 18.0)
    elif kind == "hops":
        n = max(3, int((C - A).length / (2.6 if cut else 3.4)))
        for k in range(n):
            p = A.lerp(C, (k + 0.5) / n) + s * ((1.3 if k % 2 else -1.3) * (0.5 if cut else 1.0))
            h = 0.55 if cut else 1.0
            block(p, h, p.z - 0.6, p.z, CUT if cut else ROCK); block(p, 0.18, FLOOR, p.z - 0.6, POST)
    elif kind == "pad":
        strip([A - f * 2.5, A + f * 2.5], 3.0, SAND); block(A, 1.2, A.z, A.z + 0.25, PAD); block(A, 0.3, FLOOR, A.z - 0.5, POST)
        for k in range(1, 9):                                   # the arc a body flies, as dots
            t = k / 9
            p = A.lerp(C, t) + Vector((0, 0, 4.0 * math.sin(math.pi * t)))
            block(p, 0.16, p.z + 1.0, p.z + 1.3, PAD)
    elif kind == "dive":
        strip([A - f * 3.0, A + f * 2.0], 4.0, SAND); block(A + f * 2.0, 0.3, FLOOR, A.z - 0.5, POST)
        strip([A + f * 2.0, A + f * 5.0], 1.2, DIVE, thick=0.3)                     # the board
        for k in range(1, 12):                                  # the fall, as dots
            t = k / 12
            p = (A + f * 5.0).lerp(C, t)
            p.z = A.z + (C.z - A.z) * t * t
            block(p, 0.2, p.z + 1.0, p.z + 1.4, DIVE)
    elif kind == "swing":
        n = max(1, round((C - A).length / 20.0))
        top = max(A.z, C.z) + 8.0
        for k in range(n):
            p = A.lerp(C, (k + 0.5) / n)
            for sd in (-1, 1):
                q = p + s * (sd * 5.0)
                block(q, 0.3, FLOOR, top, POST)
            strip([Vector((p.x, p.y, top)) - s * 5.3, Vector((p.x, p.y, top)) + s * 5.3], 0.45, CUT if cut else VINE, thick=0.6)
            for j in range(9):                                  # the swing's arc under that beam
                t = (k + j / 8) / n
                q = A.lerp(C, t)
                dip = 3.2 * math.sin(math.pi * j / 8)
                block((q.x, q.y, q.z), 0.14, q.z + 1.1 - dip, q.z + 1.4 - dip, CUT if cut else VINE)


CHAIN, WOOD = mat("chain", (0.20, 0.20, 0.22)), mat("log", (0.62, 0.44, 0.28))


def ball(p, r, m, squash=0.8):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=r, location=(p[0], p[1], p[2]))
    o = bpy.context.object
    o.scale = (1, 1, squash); o.data.materials.append(m)


def drum(p, r, h, m, lie=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=14, radius=r, depth=max(0.05, h), location=(p[0], p[1], p[2]))
    o = bpy.context.object
    if lie is not None:                                   # lying on its side, its axis along `lie`
        o.rotation_euler = Vector((0, 0, 1)).rotation_difference(Vector(lie).normalized()).to_euler()
    o.data.materials.append(m)


def rod(a, c, r, m):
    a, c = Vector(a), Vector(c)
    drum((a + c) / 2, r, (c - a).length, m, lie=c - a)


def frame_over(p, s, top, wide=5.0):
    """Two posts and a beam: what a chain hangs from."""
    for sd in (-1, 1):
        block(p + s * (sd * wide), 0.3, FLOOR, top, POST)
    strip([Vector((p.x, p.y, top)) - s * (wide + 0.3), Vector((p.x, p.y, top)) + s * (wide + 0.3)], 0.4, POST, thick=0.5)


KIT = dict(Vector=Vector, side_of=side_of, ball=ball, drum=drum, rod=rod, block=block, strip=strip, box=box, posts=posts, frame_over=frame_over,
           mesh=mesh, ROCK=ROCK, SAND=SAND, CHAIN=CHAIN, POST=POST, WATER=WATER, PAD=PAD, VINE=VINE, WOOD=WOOD, FLOOR=FLOOR)


def gate(p, toward, finish=False):
    f, s = side_of(p, toward)
    P = Vector(p)
    for sd in (-1, 1):
        q = P + s * (sd * 3.6)
        block(q, 0.25, P.z, P.z + 4.6, BLACK if finish else WHITE)
    for k in range(8):
        q = P + s * (-3.15 + k * 0.9)
        block(q, 0.45, P.z + 4.0, P.z + 4.7, (BLACK if k % 2 else WHITE) if finish else WHITE)


for i, (kind, a, c) in enumerate(COURSE):
    if not stages.build(stages.STAGE.get(i + 1, ("plain", ""))[0], Vector(a), Vector(c), KIT):
        leg(kind, a, c)
for (name, asks, after, before, legs) in SHORTCUTS:
    for (kind, a, c) in legs:
        leg(kind, a, c, cut=True)
gate(COURSE[0][1], COURSE[0][2]); gate(COURSE[-1][2], Vector(COURSE[-1][2]) * 2 - Vector(COURSE[-1][1]), True)
for i in (0, 2, 4, 7, 9, 15, 17, 20, 21, 22, 25, 29, 30, 35):
    kind, a, c = COURSE[i]
    figure(bowed(a, c, kind)[10])
xs = [p[0] for l in COURSE for p in (l[1], l[2])]; ys = [p[1] for l in COURSE for p in (l[1], l[2])]
box(min(xs) - 60, max(xs) + 60, min(ys) - 60, max(ys) + 60, FLOOR - 1, FLOOR, GROUND)

sc = bpy.context.scene
ids = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in ids else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.66, 0.76, 0.90, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.75
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 3.2; sun.rotation_euler = (math.radians(42), 0, math.radians(-30))
sc.render.resolution_x, sc.render.resolution_y = 1600, 900
c = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, 10))
EYE = Vector((0, 0, 1.25))


def at(i, t=0.0):
    kind, a, b = COURSE[i - 1]
    return Vector(a).lerp(Vector(b), t)


VIEWS = [
    ("air_south", c + Vector((110, -290, 190)), c, 36, None), ("air_north", c + Vector((-140, 300, 170)), c, 36, None),
    ("plan", Vector((c.x, c.y + 0.01, 500)), Vector((c.x, c.y, 0)), None, 330),
    ("eye_boulders", at(2, 0.0) + EYE + Vector((-5, -1, 0.5)), at(2, 0.7) + Vector((0, 0, 2)), 63, None),
    ("eye_gauntlet", at(5, 0.0) + EYE + Vector((-3, -4, 0.5)), at(5, 0.8) + Vector((0, 0, 2)), 63, None),
    ("close_boulders", at(2, 0.5) + Vector((4, -30, 14)), at(2, 0.5) + Vector((0, 0, 3)), 40, None),
    ("close_colonnade", at(9, 0.5) + Vector((6, -28, 14)), at(9, 0.5), 40, None),
    ("close_wheels", at(13, 0.5) + Vector((-30, -8, 14)), at(13, 0.5), 40, None),
    ("close_seesaws", at(17, 0.5) + Vector((-6, -28, 14)), at(17, 0.5), 40, None),
    ("close_floating", at(29, 0.5) + Vector((-8, -28, 12)), at(29, 0.5), 40, None),
    ("eye_dive", at(11, 0.0) + EYE + Vector((4, -3, 0.6)), at(12, 0.5), 63, None),
    ("eye_summit", at(22, 0.0) + EYE + Vector((4, 1, 0.8)), at(22, 0.6), 63, None),
    ("eye_last_slide", at(31, 0.0) + EYE + Vector((2, 3, 0.8)), at(31, 0.6), 63, None),
]
for (name, eye, look, fov, ortho) in VIEWS:
    cd = bpy.data.cameras.new(name); co = bpy.data.objects.new("cam_" + name, cd); sc.collection.objects.link(co)
    cd.clip_end = 2000
    if ortho:
        cd.type, cd.ortho_scale = "ORTHO", ortho
    else:
        cd.sensor_fit, cd.angle_y = "VERTICAL", math.radians(fov)
    co.location = eye
    co.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
src = ROOT / "ArtSource/paete_run"
src.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(src / "paete_run_blockout.blend"), compress=True)
for (name, eye, look, fov, ortho) in VIEWS:
    sc.camera = bpy.data.objects["cam_" + name]
    sc.render.filepath = os.path.join(OUT, "paete_run_%s_v%s.png" % (name, VER))
    bpy.ops.render.render(write_still=True)
print("done")
