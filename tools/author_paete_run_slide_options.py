"""PAETE'S RUN: what the two kinds of sliding LOOK like, side by side, in grey (2026-10-10). A picture, nothing more.

Owner, asked whether surf should be Counter-Strike's or a lighter slide: "i was thinking of cs surf, but i wanna see what
the 2nd option looks like".

  blender -b --python tools/author_paete_run_slide_options.py -- --out=<abs folder>

LEFT (teal), SLIDE CHUTES: a shallow trough (about 20 degrees downhill) that a body slides DOWN facing forward, like a
luge or a water slide: banked bends, a drop, a jump across a gap. RIGHT (orange), SURF: steep planes (about 55 degrees)
that a body cannot stand on; it hangs on the face and flies ALONG them sideways, hopping the gaps from one to the next.
Blender x across, y along the course (downhill is +y), z up. The dark figures are 1.3 m.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

OUT = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--out=")), ".")
bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, rgb):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = 0.9
    return m


SAND, ROCK = mat("sand", (0.80, 0.70, 0.52)), mat("rock", (0.55, 0.50, 0.46))
CHUTE, SURF, BODY, WATER = mat("chute", (0.18, 0.66, 0.62)), mat("surf", (0.93, 0.56, 0.22)), mat("body", (0.14, 0.14, 0.17)), mat("water", (0.25, 0.62, 0.78))


def mesh(name, verts, faces, m):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.materials.append(m)
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    return o


def box(x0, x1, y0, y1, z0, z1, m):
    bpy.ops.mesh.primitive_cube_add(size=1, location=((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2))
    o = bpy.context.object
    o.scale = (abs(x1 - x0), abs(y1 - y0), abs(z1 - z0))
    o.data.materials.append(m)


def figure(p, lean=0.0):
    x, y, z = p
    box(x - 0.2, x + 0.2, y - 0.15, y + 0.15, z, z + 0.85, BODY)
    box(x - 0.23 + lean, x + 0.23 + lean, y - 0.2, y + 0.2, z + 0.85, z + 1.3, BODY)


# ---------------------------------------------------------------- the ground: a sand slope with rock shoulders
box(-30, 30, -6, 84, -30, -22.5, SAND)
box(-30, -24, -6, 84, -22.5, -8, ROCK); box(24, 30, -6, 84, -22.5, -8, ROCK); box(-1.0, 1.0, -6, 84, -22.5, -6.0, ROCK)
box(-22, 22, 70, 84, -22.5, -21.6, WATER)

# ---------------------------------------------------------------- LEFT: a slide chute, a trough swept down an S
def chute(path, width=3.2, wall=1.1, m=CHUTE):
    """A U trough along the points: flat bed, two leaning walls, banked by how hard the path turns."""
    verts, faces = [], []
    n = len(path)
    for i, p in enumerate(path):
        a, c = Vector(path[max(0, i - 1)]), Vector(path[min(n - 1, i + 1)])
        fwd = (c - a).normalized()
        side = fwd.cross(Vector((0, 0, 1))).normalized()
        turn = 0.0
        if 0 < i < n - 1:
            d0, d1 = (Vector(p) - a).normalized(), (c - Vector(p)).normalized()
            turn = d0.cross(d1).z
        bank = max(-0.5, min(0.5, turn * 5.0))
        up = Vector((0, 0, 1))
        P = Vector(p)
        h = width / 2
        for (s, lift) in ((-h - 0.9, wall), (-h, 0.0), (h, 0.0), (h + 0.9, wall)):
            verts.append(tuple(P + side * s + up * (lift + bank * s * 0.35)))
    for i in range(n - 1):
        for k in range(3):
            a = i * 4 + k
            faces.append((a, a + 1, a + 5, a + 4))
    mesh("chute", verts, faces, m)


def s_path(x0, y0, y1, z0, drop_per_m, amp, wave, steps):
    pts = []
    for k in range(steps + 1):
        t = k / steps
        y = y0 + (y1 - y0) * t
        pts.append((x0 + amp * math.sin(t * math.pi * 2 * wave), y, z0 - (y - y0) * drop_per_m))
    return pts


A = s_path(-11.5, 0.0, 30.0, 0.0, math.tan(math.radians(20)), 5.5, 1.0, 40)
chute(A)
# a drop and a gap: the first run ends in a lip, the second starts lower and further on
lip = A[-1]
B = s_path(-11.5, 36.0, 66.0, lip[2] - 3.2, math.tan(math.radians(18)), -5.0, 1.0, 40)
chute(B)
box(-14.0, -9.0, 30.0, 31.2, lip[2] - 0.2, lip[2] + 0.55, CHUTE)          # the kicker at the lip
figure((A[12][0], A[12][1], A[12][2]), 0.0); figure((B[20][0], B[20][1], B[20][2]), 0.0)
figure((-11.5, 33.0, lip[2] + 0.9), 0.0)                                   # in the air over the gap
box(-21.0, -1.5, -5.0, 0.0, -0.3, 0.0, SAND)                               # the start pad

# ---------------------------------------------------------------- RIGHT: surf ramps, steep planes flown along
def surf_ramp(xc, y0, y1, z0, z1, wide=4.2, tall=5.6, m=SURF):
    """A long ridge: two faces at about 55 degrees meeting in a crest, running downhill from (y0, z0) to (y1, z1)."""
    v = [(xc - wide, y0, z0 - tall), (xc, y0, z0), (xc + wide, y0, z0 - tall), (xc - wide, y1, z1 - tall), (xc, y1, z1), (xc + wide, y1, z1 - tall)]
    mesh("surf", v, [(0, 1, 4, 3), (1, 2, 5, 4), (0, 3, 5, 2), (0, 2, 1), (3, 4, 5)], m)


runs = [(11.5, 1.0, 17.0, -0.5, -3.0), (11.5, 22.0, 38.0, -5.5, -8.0), (11.5, 43.0, 57.0, -10.5, -12.6), (11.5, 61.0, 69.0, -15.0, -16.2)]
for (xc, y0, y1, z0, z1) in runs:
    surf_ramp(xc, y0, y1, z0, z1)
figure((11.5 + 2.0, 9.0, -1.75 - 2.7 - 0.2), 0.3); figure((11.5 - 2.0, 30.0, -6.75 - 2.7 - 0.2), -0.3); figure((11.5 + 1.2, 40.5, -8.2), 0.0)
box(1.5, 21.0, -5.0, 0.0, -0.3, 0.0, SAND)

# ---------------------------------------------------------------- light and pictures
sc = bpy.context.scene
ids = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in ids else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.66, 0.76, 0.90, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.6
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 3.4; sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.clip_end = 500
sc.render.resolution_x, sc.render.resolution_y = 1600, 900


def shoot(name, eye, look, fov):
    cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(OUT, "slide_options_%s.png" % name)
    bpy.ops.render.render(write_still=True)


shoot("air", (0, -30, 30), (0, 30, -12), 46)
shoot("chute_eye", (-11.5, -0.6, 1.9), (A[16][0], A[16][1], A[16][2]), 63)
shoot("surf_eye", (11.5 + 3.4, -0.4, 1.9), (11.5, 30.0, -9.0), 63)
shoot("chute_side", (-26, 20, 6), (-10, 30, -9), 50)
shoot("surf_side", (30, 8, 2), (11, 28, -8), 50)
print("done")
