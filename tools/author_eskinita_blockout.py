"""ESKINITA REBUILD, GREY BLOCKOUTS (2026-10-08). Three layouts for the owner to choose from, nothing more.

Owner: "rebuild from scratch"; "eskinita is a small/tight map to reflect the concept (alleyway)"; "i like that idea that
its a multi level / varied height map"; "climbable ladders or just jump pads to get up ... want something unique"; then,
of three layouts described in words, "give me a block layout for all 3".

  blender -b --python tools/author_eskinita_blockout.py -- --layout=1|2|3 --out=<folder>

Writes <out>/eskinita_L<n>_air.png, _top.png and _eye.png. Blender units are metres; x is across the alley, y along it
(Unity's z), z up. The alley floor is 16 by 35 m as today (wall faces at x 8.1 and y 17.5), the chalk box is 14 m across
with the can at its middle. Colours are a KEY, not art: grey the floor, cream a lower roof (about 2.5 m), terracotta an
upper roof (about 5 m), yellow a way up (ladder, stairs), teal a bounce tarp, brown a bridge or plank, white the chalk
box, red the can, and a 1.3 m figure for scale.
"""
import math
import os
import sys

import bpy
from mathutils import Vector


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


LAYOUT = int(arg("layout", "1"))
OUT = arg("out", ".")
bpy.ops.wm.read_factory_settings(use_empty=True)
MATS = {}


def mat(name, rgb):
    if name not in MATS:
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (*rgb, 1)
        b.inputs["Roughness"].default_value = 0.9
        MATS[name] = m
    return MATS[name]


FLOOR = mat("floor", (0.42, 0.42, 0.44))
LOW = mat("low roof", (0.86, 0.80, 0.62))
HIGH = mat("high roof", (0.72, 0.36, 0.24))
WALL = mat("house wall", (0.62, 0.60, 0.58))
UP = mat("way up", (0.95, 0.80, 0.10))
BOUNCE = mat("bounce", (0.10, 0.70, 0.66))
PLANK = mat("bridge", (0.45, 0.28, 0.14))
CHALK = mat("chalk", (0.97, 0.97, 0.97))
CAN = mat("can", (0.85, 0.12, 0.10))
BODY = mat("figure", (0.15, 0.15, 0.18))
BOUND = mat("bound", (0.30, 0.32, 0.38))


def box(x0, x1, y0, y1, z0, z1, m):
    bpy.ops.mesh.primitive_cube_add(size=1, location=((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2))
    o = bpy.context.object
    o.scale = (abs(x1 - x0), abs(y1 - y0), abs(z1 - z0))
    o.data.materials.append(m)
    return o


def house(x0, x1, y0, y1, height, base=0.0):
    """A solid house: grey walls, its roof coloured by its tier."""
    box(x0, x1, y0, y1, base, base + height - 0.12, WALL)
    box(x0, x1, y0, y1, base + height - 0.12, base + height, LOW if height < 3.6 else HIGH)


def slab(x0, x1, y0, y1, z, m, thick=0.12):
    box(x0, x1, y0, y1, z - thick, z, m)


def stairs(x0, x1, y0, y1, z0, z1, along="y", steps=8):
    for k in range(steps):
        f0, f1 = k / steps, (k + 1) / steps
        if along == "y":
            box(x0, x1, y0 + (y1 - y0) * f0, y0 + (y1 - y0) * f1, z0, z0 + (z1 - z0) * f1, UP)
        else:
            box(x0 + (x1 - x0) * f0, x0 + (x1 - x0) * f1, y0, y1, z0, z0 + (z1 - z0) * f1, UP)


def ladder(x, y, z0, z1, face="x"):
    if face == "x":
        box(x - 0.08, x + 0.08, y - 0.35, y + 0.35, z0, z1, UP)
    else:
        box(x - 0.35, x + 0.35, y - 0.08, y + 0.08, z0, z1, UP)


def figure(x, y, z=0.0):
    box(x - 0.22, x + 0.22, y - 0.16, y + 0.16, z, z + 0.85, BODY)
    box(x - 0.24, x + 0.24, y - 0.22, y + 0.22, z + 0.85, z + 1.30, BODY)


def court(z=0.0, y=0.0):
    """The 14 m chalk box as a ring of thin strips, and the can."""
    r, n = 7.0, 48
    for k in range(n):
        a = 2 * math.pi * k / n
        bpy.ops.mesh.primitive_cube_add(size=1, location=(r * math.cos(a), y + r * math.sin(a), z + 0.02))
        o = bpy.context.object
        o.scale = (0.12, 2 * math.pi * r / n * 1.02, 0.03)
        o.rotation_euler = (0, 0, a)
        o.data.materials.append(CHALK)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=0.28, location=(0, y, z + 0.14))
    bpy.context.object.data.materials.append(CAN)
    figure(0.0, y - 2.5, z)


def bounds(half_x, half_y, z0=0.0):
    for sx in (-1, 1):
        box(sx * half_x, sx * (half_x + 0.4), -half_y, half_y, z0, z0 + 0.6, BOUND)
    for sy in (-1, 1):
        box(-half_x, half_x, sy * half_y, sy * (half_y + 0.4), z0, z0 + 0.6, BOUND)


# ---------------------------------------------------------------- 1. THE STACKED ALLEY
def layout_1():
    box(-8, 8, -17.5, 17.5, -0.3, 0.0, FLOOR)
    court()
    # West side, south to north: (y0, y1, depth, height). Lower and upper roofs alternate unevenly.
    for y0, y1, depth, h in [(-17.5, -11.5, 5.0, 5.2), (-11.5, -6.0, 4.2, 2.6), (-6.0, 1.5, 5.5, 5.0), (1.5, 6.5, 4.0, 2.4), (6.5, 12.5, 5.2, 5.4),
                             (12.5, 17.5, 4.4, 2.8)]:
        house(-8 - depth, -8, y0, y1, h)
    for y0, y1, depth, h in [(-17.5, -12.5, 4.4, 2.5), (-12.5, -5.5, 5.4, 5.3), (-5.5, -0.5, 4.2, 2.7), (-0.5, 6.0, 5.6, 5.0), (6.0, 11.0, 4.0, 2.5),
                             (11.0, 17.5, 5.0, 5.2)]:
        house(8, 8 + depth, y0, y1, h)
    # Awnings a step out over the alley, on the lower houses: a ledge to land on and to throw from.
    slab(-8, -6.6, -11.0, -6.6, 2.45, LOW); slab(6.6, 8, -5.0, -1.0, 2.55, LOW); slab(-8, -6.8, 2.0, 6.0, 2.30, LOW)
    slab(6.7, 8, 6.4, 10.6, 2.40, LOW)
    # WAYS UP: a steel ladder, an outside stair, and two bounce tarps (the jump pad of an alley).
    ladder(-7.9, -8.6, 0, 2.6); ladder(7.9, 8.6, 0, 2.5)
    stairs(-8, -6.9, 12.8, 17.2, 0, 2.8, "y", 9)
    stairs(8.0, 9.2, -12.2, -9.0, 2.5, 5.3, "y", 7)                  # roof to roof, on the east side
    stairs(-9.2, -8.0, -6.0, -3.0, 2.6, 5.0, "y", 7)
    slab(6.2, 8, -16.8, -13.6, 0.75, BOUNCE); slab(-8, -6.2, 6.9, 9.6, 0.75, BOUNCE)
    # THE CROSSINGS: two plank bridges roof to roof over the alley, off the court's middle, and one low one.
    slab(-8, 8, -9.4, -8.5, 5.15, PLANK); slab(-8, 8, 8.2, 9.0, 5.1, PLANK)
    slab(-8, 8, 15.2, 15.9, 2.75, PLANK)
    # Clotheslines (thin): things a vine can catch.
    for y, z in ((-3.0, 4.6), (3.6, 4.2), (12.0, 4.8)):
        box(-8, 8, y - 0.03, y + 0.03, z, z + 0.05, CHALK)
    bounds(13.6, 17.5)
    return (0, 0, 2.5), 46


# ---------------------------------------------------------------- 2. THE STEPPED ALLEY
# ⚠️ CHOSEN (owner, 2026-10-08, of the three blockouts: "go with 2, fix what you see wrong"). What was wrong with the
# first one, and what changed:
#   * the top terrace stood 2.4 m over the bottom one, so from one end the other could not be seen at all: each step is
#     0.9 m now (1.8 m in all), so a player at the far end shows from the chest up over the two rises;
#   * the 14 m chalk box nearly touched both flights of stairs: the can's terrace is 17 m long (was 15.2), so the whole
#     box and a metre and a half round it are on one flat floor;
#   * the one bridge crossed straight over the can, a free drop onto it: there are two now, each over a flight of
#     stairs at a terrace's edge, outside the box;
#   * the two flights at each rise mirrored each other: they are different widths and offset, with a low wall beside
#     each, and each rise also has a narrow side ramp along one wall (the quiet way round);
#   * only the middle terrace had a way onto the roofs: every terrace has one now (a ladder, a bounce tarp, a stair),
#     and ledges step out over the alley at the lower roofs' height so the roofs can be walked end to end on one side.
STEP = 0.9
MID = 8.5           # the can's terrace runs from -MID to MID


def layout_2():
    z1, z2 = STEP, 2 * STEP
    box(-8, 8, -17.5, -MID, -0.3, 0.0, FLOOR)
    box(-8, 8, -MID, MID, -0.3, z1, FLOOR)
    box(-8, 8, MID, 17.5, -0.3, z2, FLOOR)
    court(z1)
    # THE SOUTH RISE: a broad flight left of middle, a narrow one by the east wall, a ramp along the west wall.
    stairs(-4.6, 0.6, -MID - 1.8, -MID, 0, z1, "y", 5)
    stairs(4.4, 6.8, -MID - 1.4, -MID, 0, z1, "y", 4)
    stairs(-8.0, -6.9, -MID - 4.2, -MID, 0, z1, "y", 12)
    box(0.6, 0.9, -MID - 1.8, -MID + 0.6, 0, z1 + 0.9, WALL); box(4.1, 4.4, -MID - 1.4, -MID + 0.4, 0, z1 + 0.9, WALL)
    # THE NORTH RISE: the broad flight is on the other side here, the ramp along the east wall.
    stairs(-0.4, 5.0, MID, MID + 1.8, z1, z2, "y", 5)
    stairs(-6.6, -4.4, MID, MID + 1.4, z1, z2, "y", 4)
    stairs(6.9, 8.0, MID, MID + 4.2, z1, z2, "y", 12)
    box(-0.7, -0.4, MID - 0.6, MID + 1.8, z1, z2 + 0.9, WALL); box(-4.4, -4.1, MID - 0.4, MID + 1.4, z1, z2 + 0.9, WALL)
    # THE HOUSES. West, south to north: (y0, y1, depth, height above its own floor, floor).
    west = [(-17.5, -13.0, 4.6, 2.6, 0.0), (-13.0, -MID, 5.2, 5.0, 0.0), (-MID, -3.5, 4.2, 2.6, z1), (-3.5, 3.0, 5.6, 4.9, z1), (3.0, MID, 4.4, 2.5, z1),
            (MID, 13.0, 5.0, 4.8, z2), (13.0, 17.5, 4.2, 2.6, z2)]
    east = [(-17.5, -12.0, 4.2, 2.5, 0.0), (-12.0, -MID, 5.2, 5.0, 0.0), (-MID, -2.0, 5.4, 5.0, z1), (-2.0, 3.6, 4.2, 2.6, z1), (3.6, MID, 5.2, 4.7, z1),
            (MID, 12.2, 5.2, 4.8, z2), (12.2, 17.5, 4.2, 2.5, z2)]
    for y0, y1, depth, h, base in west:
        house(-8 - depth, -8, y0, y1, h, base)
    for y0, y1, depth, h, base in east:
        house(8, 8 + depth, y0, y1, h, base)
    # LEDGES over the alley at the lower roofs' height: with the low roofs they make one walk along each side.
    slab(-8, -6.7, -MID, -3.5, z1 + 2.6, LOW); slab(-8, -6.8, 3.0, MID, z1 + 2.5, LOW); slab(-8, -6.7, -3.5, 3.0, z1 + 2.55, LOW)
    slab(6.8, 8, -2.0, 3.6, z1 + 2.6, LOW); slab(6.7, 8, -17.5, -12.0, 2.5, LOW); slab(6.8, 8, 12.2, 17.5, z2 + 2.5, LOW)
    # WAYS UP, one on every terrace: a ladder and a bounce tarp below, a ladder and a stair in the middle, a tarp above.
    ladder(-7.9, -15.2, 0, 2.6); slab(6.1, 8, -16.6, -13.8, 0.75, BOUNCE)
    ladder(7.9, 0.8, z1, z1 + 2.6); stairs(-8, -6.9, -MID + 0.3, -MID + 4.4, z1, z1 + 2.6, "y", 9)
    slab(-8, -6.1, 13.6, 16.4, z2 + 0.75, BOUNCE); ladder(7.9, 14.6, z2, z2 + 2.5)
    # Low roof to high roof, once a side.
    stairs(-9.2, -8.0, -3.5, -1.0, z1 + 2.6, z1 + 4.9, "y", 7); stairs(8.0, 9.2, 3.6, 6.0, z1 + 2.6, z1 + 4.7, "y", 7)
    # ⚠️ NOTHING HANGS IN THE AIR (owner, 2026-10-08, of bridges that ended on a low roof 2.5 m under them and of lines
    # that ended on bare wall: "the planks are floating with no end"). THE TWO BRIDGES, each over a rise and outside the
    # chalk box: the house at EACH end is a high one now, the plank runs a metre onto both roofs, sits on a beam at
    # each roof's edge and has a rail on one side.
    def bridge(y0, y1, z):
        slab(-9.0, 9.0, y0, y1, z + 0.14, PLANK)
        for sx in (-1, 1):
            box(sx * 8.0, sx * 8.5, y0 - 0.25, y1 + 0.25, z - 0.25, z + 0.02, PLANK)       # the beam it rests on
            box(sx * 8.9, sx * 9.0, y0, y1, z + 0.14, z + 0.5, PLANK)                        # its stop on the roof
        box(-9.0, 9.0, y1 - 0.08, y1, z + 0.14, z + 1.0, PLANK)                              # the rail
    bridge(-MID - 1.6, -MID - 0.7, 5.0)
    bridge(MID + 0.7, MID + 1.6, z2 + 4.8)
    # CLOTHESLINES for a vine to catch, none over the can: each is strung between two POLES that stand on what is
    # under them (a roof or the alley floor), so a line has an end to be tied to. (y, height, west foot, east foot).
    for y, z, foot_w, foot_e in ((-5.6, z1 + 4.3, z1 + 2.6, z1), (5.2, z1 + 4.0, z1 + 2.5, z1), (14.6, z2 + 4.2, z2 + 2.6, z2 + 2.5),
                                 (-14.8, 4.2, 2.6, 2.5)):
        box(-8.3, 8.3, y - 0.03, y + 0.03, z, z + 0.05, CHALK)
        box(-8.38, -8.22, y - 0.08, y + 0.08, foot_w, z + 0.25, PLANK)
        box(8.22, 8.38, y - 0.08, y + 0.08, foot_e if foot_e > z1 + 0.5 or y < -MID else foot_e, z + 0.25, PLANK)
    figure(-2.0, -14.0, 0.0); figure(2.0, 14.0, z2)
    bounds(13.6, 17.5, 0.0)
    return (0, 0, 2.2), 46


# ---------------------------------------------------------------- 3. THE COURTYARD (LOOBAN)
def layout_3():
    # The alley (7 m wide) opens into a square yard in the middle, 18 m across.
    box(-3.5, 3.5, -17.5, 17.5, -0.3, 0.0, FLOOR)
    box(-9, 9, -9, 9, -0.3, 0.0, FLOOR)
    court()
    # Houses round the yard: a balcony ring at 2.5 m on all four sides, upper roofs behind it.
    for (x0, x1, y0, y1, h) in [(-14, -9, -9, -2.5, 5.2), (-14, -9, -2.5, 3.5, 2.6), (-14, -9, 3.5, 9, 5.0),
                                (9, 14, -9, -4.0, 2.6), (9, 14, -4.0, 2.0, 5.3), (9, 14, 2.0, 9, 2.5)]:
        house(x0, x1, y0, y1, h)
    for (x0, x1, y0, y1, h) in [(-9, -3.5, 9, 13.5, 5.0), (3.5, 9, 9, 13.5, 2.6), (-9, -3.5, -13.5, -9, 2.5), (3.5, 9, -13.5, -9, 5.2),
                                (-8, -3.5, 13.5, 17.5, 2.6), (3.5, 8, 13.5, 17.5, 4.8), (-8, -3.5, -17.5, -13.5, 4.8), (3.5, 8, -17.5, -13.5, 2.6)]:
        house(x0, x1, y0, y1, h)
    # The balcony ring, 1.4 m deep, broken at the alley's two mouths.
    slab(-9, -7.6, -9, 9, 2.55, LOW); slab(7.6, 9, -9, 9, 2.55, LOW)
    slab(-9, -3.5, 7.6, 9, 2.55, LOW); slab(3.5, 9, 7.6, 9, 2.55, LOW)
    slab(-9, -3.5, -9, -7.6, 2.55, LOW); slab(3.5, 9, -9, -7.6, 2.55, LOW)
    # Ways up: a stair in two opposite corners, a ladder in the others, a bounce tarp at each alley mouth.
    stairs(-9, -7.6, -7.4, -3.0, 0, 2.55, "y", 8); stairs(7.6, 9, 3.0, 7.4, 2.55, 0, "y", 8)
    ladder(-7.5, 6.0, 0, 2.55); ladder(7.5, -6.0, 0, 2.55)
    slab(-3.4, -1.4, 10.2, 12.6, 0.75, BOUNCE); slab(1.4, 3.4, -12.6, -10.2, 0.75, BOUNCE)
    # Bridges over each alley mouth join the ring into a full loop; one plank crosses the yard corner to corner, high.
    slab(-3.5, 3.5, 7.8, 8.6, 2.6, PLANK); slab(-3.5, 3.5, -8.6, -7.8, 2.6, PLANK)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 5.2)); o = bpy.context.object
    o.scale = (25.5, 0.8, 0.12); o.rotation_euler = (0, 0, math.radians(45)); o.data.materials.append(PLANK)
    bounds(14, 17.5)
    return (0, 0, 2.5), 46


centre, span = {1: layout_1, 2: layout_2, 3: layout_3}[LAYOUT]()

sc = bpy.context.scene
ids = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in ids else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.72, 0.84, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.45
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 3.0; sun.rotation_euler = (math.radians(48), 0, math.radians(-38))
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
sc.render.resolution_x, sc.render.resolution_y = 1100, 720
c = Vector(centre)


def shoot(name, eye, look, ortho=None, fov=None):
    cam.data.type = "ORTHO" if ortho else "PERSP"
    if ortho:
        cam.data.ortho_scale = ortho
    if fov:
        cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(OUT, "eskinita_L%d_%s.png" % (LAYOUT, name))
    bpy.ops.render.render(write_still=True)


shoot("air", (24, -34, 30), c, fov=38)
shoot("top", (0, 0.01, 60), (0, 0, 0), ortho=span)
# The player's eye: 1.25 m up, 95 degrees, standing at the south end of the alley looking up it.
if LAYOUT == 2:
    shoot("eye", (0.6, -15.5, 1.25), (0, 0, 2.4), fov=95)                              # the low end, looking up the alley
    shoot("eyetop", (-3.0, 16.0, 2 * STEP + 1.25), (0, 0, 1.6), fov=95)                # the high end, looking down it
    shoot("eyecan", (1.6, -1.0, STEP + 1.25), (0, 9, STEP + 2.2), fov=95)              # the taya's view from the can
    shoot("roof", (-9.0, -11.0, 5.0 + 1.25), (2, 4, 1.4), fov=95)                      # from a high roof by the south bridge
else:
    shoot("eye", (0.6, -15.5, 1.25), (0, 0, 2.2), fov=95)
print("done")
