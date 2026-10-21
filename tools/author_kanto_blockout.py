"""Block out the whole Kanto sample map in Blender, at game scale, before detailing buildings.

  blender -b --python tools/author_kanto_blockout.py -- [--preview N]

Writes ArtSource/kanto/kanto_blockout.blend, and with --preview versioned renders to
Logs/kanto-blender/blockout_<shot>_vN.png (plans, aerial, and eye-level views).

WHY A BLOCKOUT. The owner asked for "a block layout set in blender for the whole map just to
make sure the layout is good" before more buildings are modelled. Everything here is a plain,
colour-coded mass except the finished brick corner, which is LINKED from brick_corner.blend as
a collection instance, so it updates when that file is rebuilt and proves the detailed model
fits its lot.

COORDINATES. Blender is Z-up. The plan is Blender X/Y; Unity reads Blender X as X, Blender Z
as Y, and Blender -Y as Z (the glTF convention). All numbers are metres.

THE LAYOUT: A CITY PARK BLOCK. The owner: "make sure the layout includes the same play area
that the other maps currently in the game have, i was thinking the play area is designed as a
park". Measured from the shipped scenes' Bounds colliders (2026-09-24): Bayan Plaza 26 x 26,
Eskinita ~17 x 40, Ilalim ng Tulay 22.4 x 33.4, Sa Bubong 40 x 46, Lagoon 84 x 84. There is no
single size, so the park takes BAYAN PLAZA's, the other open-square map: walls at +/-13, which
leaves 6 m of legal standing room on every side of the box, as Bayan does.
  * The chalk box is a 14 x 14 m SQUARE on a paved court: Balance.ConfinementRadius = 7.
  * Throwing lines at 8 m (box + 1) along Y: Confinement.ThrowingLine().
  * Attacker spawn ring at 9 m: Confinement.AttackerSpawnRing().
  * The court is 20 x 20; lawns, trees and benches fill the band out to the park fence at 14.
    Trees and trip hazards stay at least 7 m out (MapGeometryCheck), clutter under 1.0 m.
  * A ring road 10 m wide surrounds the park (centre lines at +/-22), with 3 m sidewalks both
    sides. Buildings stand across the road from 30 m, FACING the park, the way Tiny Talisman's
    city faces its pond park: corner blocks on the four diagonals (the brick corner's cut
    corner looks at the park's corner), mid blocks on the four sides.
  * The grid streets carry on outward along the same centre lines, lined with blocks.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "kanto"
PREVIEWS = ROOT / "Logs" / "kanto-blender"

BOX = 7.0            # Balance.ConfinementRadius
THROW = BOX + 1.0    # Confinement.ThrowingLine()
SPAWN = BOX + 2.0    # Confinement.AttackerSpawnRing()
HALF = 13.0          # playable half-size, Bayan Plaza's; the walls' faces are here
COURT = 10.0         # paved court half-size
PARK = 14.0          # park edge, fence line just outside the walls
ROAD = 22.0          # grid street centre lines
ROAD_HALF = 5.0      # 10 m carriageway
SIDE = 3.0           # sidewalk width, both sides of every road
FACADE = ROAD + ROAD_HALF + SIDE   # 30: where buildings start
WALK = 0.12
LOT = 18.0           # corner building lot, matching brick_corner's 18 x 18 footprint
QUADS = [(1, 1), (-1, 1), (-1, -1), (1, -1)]   # NE, NW, SW, SE

COLOURS = {
    "road":       (0.20, 0.20, 0.23),
    "sidewalk":   (0.62, 0.58, 0.52),
    "kerb":       (0.80, 0.78, 0.72),
    "paint":      (0.90, 0.90, 0.86),
    "chalk":      (1.00, 1.00, 1.00),
    "throw":      (0.95, 0.85, 0.25),
    "bounds":     (0.90, 0.15, 0.12),
    "spawn":      (0.20, 0.55, 0.95),
    "person":     (0.95, 0.45, 0.70),
    "bld_brick":  (0.56, 0.22, 0.16),
    "bld_deco":   (0.93, 0.85, 0.68),
    "bld_glass":  (0.22, 0.55, 0.55),
    "bld_podium": (0.74, 0.72, 0.66),
    "bld_town":   (0.80, 0.62, 0.55),
    "bld_walkup": (0.58, 0.72, 0.64),
    "bld_filler": (0.66, 0.60, 0.54),
    "skyline":    (0.55, 0.68, 0.72),
    "hill":       (0.40, 0.55, 0.42),
    "tree":       (0.30, 0.60, 0.10),
    "trunk":      (0.40, 0.27, 0.16),
    "pole":       (0.25, 0.25, 0.25),
    "vehicle":    (0.95, 0.75, 0.15),
    "jeepney":    (0.85, 0.25, 0.20),
    "barrier":    (0.95, 0.80, 0.20),
    "bench":      (0.46, 0.29, 0.17),
    "bunting":    (0.90, 0.30, 0.40),
    "lawn":       (0.36, 0.62, 0.20),
    "court":      (0.78, 0.72, 0.62),
    "path":       (0.70, 0.64, 0.55),
    "hedge":      (0.20, 0.42, 0.14),
    "fence":      (0.15, 0.30, 0.25),
    "lamp":       (0.20, 0.24, 0.22),
}


def mat(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    r, g, b = COLOURS[name]
    m.diffuse_color = (r, g, b, 1)
    if m.node_tree is None:
        m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (r, g, b, 1)
    bsdf.inputs["Roughness"].default_value = 0.85
    if name == "bounds":
        bsdf.inputs["Alpha"].default_value = 0.18
        m.diffuse_color = (r, g, b, 0.18)
        if hasattr(m, "surface_render_method"):
            m.surface_render_method = "BLENDED"
    return m


def collection(name, parent):
    c = bpy.data.collections.new(name)
    parent.children.link(c)
    return c


def _obj(col, name, mesh, colour, smooth=False):
    mesh.materials.append(mat(colour))
    if smooth:
        for p in mesh.polygons:
            p.use_smooth = True
    o = bpy.data.objects.new(name, mesh)
    col.objects.link(o)
    return o


def box(col, name, center, size, colour, rot_z=0.0):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((*size, 1)))
    bm.to_mesh(mesh)
    bm.free()
    o = _obj(col, name, mesh, colour)
    o.location = center
    o.rotation_euler = (0, 0, rot_z)
    return o


def cylinder(col, name, center, radius, depth, colour, sides=16):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=sides, radius1=radius, radius2=radius, depth=depth)
    bm.to_mesh(mesh)
    bm.free()
    o = _obj(col, name, mesh, colour)
    o.location = center
    return o


def blob(col, name, center, radius, colour):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=radius)
    bm.to_mesh(mesh)
    bm.free()
    o = _obj(col, name, mesh, colour, smooth=True)
    o.location = center
    return o


def label(col, text, at, size=1.2):
    """A floating viewport label so the plan explains itself. Hidden in renders."""
    curve = bpy.data.curves.new(text, "FONT")
    curve.body = text
    curve.size = size
    curve.align_x = "CENTER"
    o = bpy.data.objects.new("label " + text, curve)
    o.location = at
    o.hide_render = True
    col.objects.link(o)
    return o


def rot(arm):
    return Matrix.Rotation(arm * math.pi / 2, 3, "Z")


# ------------------------------------------------------------------ street grid

def street(root):
    """Two roads each way on the park's four sides, running out to the horizon, sidewalks on
    both sides, and zebra crossings from every park corner to its corner block."""
    col = collection("Street", root)
    # The road's top sits 2 cm BELOW the pavement blocks' undersides. At exactly 0 the two were
    # coplanar and z-fought across the whole city in the far viewport (owner, 2026-09-24).
    box(col, "road", (0, 0, -0.22), (320, 320, 0.4), "road")
    inner, outer = ROAD - ROAD_HALF, ROAD + ROAD_HALF        # 17, 27
    lines = [-160.0, -outer, -inner, inner, outer, 160.0]
    for xi in (0, 2, 4):
        for yi in (0, 2, 4):
            x0, x1, y0, y1 = lines[xi], lines[xi + 1], lines[yi], lines[yi + 1]
            box(col, f"pavement {xi}{yi}", ((x0 + x1) / 2, (y0 + y1) / 2, WALK / 2), (x1 - x0, y1 - y0, WALK), "sidewalk")
            for x in (x0, x1):
                if abs(x) < 159:
                    box(col, f"kerb {xi}{yi}", (x, (y0 + y1) / 2, WALK / 2 + 0.01), (0.3, y1 - y0, WALK + 0.02), "kerb")
            for y in (y0, y1):
                if abs(y) < 159:
                    box(col, f"kerb {xi}{yi}", ((x0 + x1) / 2, y, WALK / 2 + 0.01), (x1 - x0, 0.3, WALK + 0.02), "kerb")
    for sx, sy in QUADS:
        a = inner + 0.8
        while a < outer - 0.6:
            box(col, "zebra", (sx * (inner - 1.8), sy * a, 0.006), (3.2, 0.55, 0.012), "paint")
            box(col, "zebra", (sx * a, sy * (inner - 1.8), 0.006), (0.55, 3.2, 0.012), "paint")
            a += 1.1
    for c in (-ROAD, ROAD):
        d = -150.0
        while d < 150:
            if not (inner < abs(d) < outer):
                box(col, "centre dash", (c, d, 0.006), (0.22, 2.4, 0.012), "throw")
                box(col, "centre dash", (d, c, 0.006), (2.4, 0.22, 0.012), "throw")
            d += 4.5


# ------------------------------------------------------------------ the park (play area)

def park(root):
    """THE PLAY AREA IS A PARK: a paved court for the game, lawns, trees and benches around
    it, a low fence just outside the invisible walls, and a path in from each side."""
    col = collection("Park (play area)", root)
    box(col, "park lawn", (0, 0, WALK + 0.02), (2 * PARK, 2 * PARK, 0.04), "lawn")
    box(col, "court paving", (0, 0, WALK + 0.05), (2 * COURT, 2 * COURT, 0.04), "court")
    for arm in range(4):
        r = rot(arm)
        box(col, f"entry path {arm}", r @ Vector((0, (COURT + PARK) / 2, WALK + 0.045)), (3.0, PARK - COURT, 0.03), "path", rot_z=arm * math.pi / 2)
        # A low fence round the park with a gap at each path. 0.8 m, under the 1.0 m clutter rule.
        for side in (-1, 1):
            c = r @ Vector((side * (1.5 + (PARK - 1.5) / 2), PARK - 0.3, WALK + 0.4))
            box(col, f"park fence {arm}", c, (PARK - 1.5, 0.12, 0.8), "fence", rot_z=arm * math.pi / 2)
    for sx, sy in QUADS:
        # A tree in each lawn corner, 11.6 m out: well past the 7 m trip-hazard floor.
        cylinder(col, "park tree trunk", (sx * 11.6, sy * 11.6, WALK + 2.2), 0.2, 4.2, "trunk", 8)
        blob(col, "park tree canopy", (sx * 11.6, sy * 11.6, WALK + 5.2), 2.4, "tree")
        box(col, "hedge bed", (sx * 11.6, sy * 6.0, WALK + 0.35), (1.2, 4.5, 0.7), "hedge")
        box(col, "hedge bed", (sx * 6.0, sy * 11.6, WALK + 0.35), (4.5, 1.2, 0.7), "hedge")
        box(col, "bench", (sx * (COURT + 0.8), sy * 3.0, WALK + 0.45), (0.55, 1.9, 0.9), "bench")
        box(col, "bench", (sx * 3.0, sy * (COURT + 0.8), WALK + 0.45), (1.9, 0.55, 0.9), "bench")
        cylinder(col, "lamp post", (sx * (PARK - 1.0), sy * 3.4, WALK + 2.2), 0.1, 4.4, "lamp", 8)
    label(col, "PARK = PLAY AREA 26 x 26 (Bayan Plaza's)", (0, -HALF + 1.4, WALK + 0.2), 0.8)


def gameplay(root):
    col = collection("Gameplay markers (not in game art)", root)
    z = WALK + 0.08
    for s in (-1, 1):
        box(col, "chalk", (s * BOX, 0, z), (0.1, 2 * BOX + 0.1, 0.02), "chalk")
        box(col, "chalk", (0, s * BOX, z), (2 * BOX + 0.1, 0.1, 0.02), "chalk")
        box(col, "throwing line", (0, s * THROW, z), (10, 0.12, 0.02), "throw")
        # Shown in the viewport, hidden from renders: at 18 per cent they still tinted every view.
        for w in (box(col, "bounds wall", (s * (HALF + 0.5), 0, 6), (1, 2 * HALF + 2, 12), "bounds"),
                  box(col, "bounds wall", (0, s * (HALF + 0.5), 6), (2 * HALF + 2, 1, 12), "bounds")):
            w.hide_render = True
    cylinder(col, "lata", (0, 0, z + 0.17), 0.14, 0.34, "chalk")
    cylinder(col, "spawn 0 (taya)", (0, 0.9, z + 0.8), 0.4, 1.6, "spawn")
    for i in range(1, 4):
        # Unity spawns attackers at z = -AttackerSpawnRing, which is Blender +Y.
        cylinder(col, f"spawn {i}", ((i - 2) * 3, SPAWN, z + 0.8), 0.4, 1.6, "spawn")
    for at in [(11, 9), (-12, 2), (4, -11.5), (0, -THROW - 1)]:
        cylinder(col, "player 1.6 m", (at[0], at[1], z + 0.8), 0.4, 1.6, "person")
    label(col, "CHALK BOX 14 x 14", (0, -3, z + 0.1))


# ------------------------------------------------------------------ buildings

def link_brick_corner(col):
    """The finished brick corner, LINKED so this blockout always shows the current model. Its
    lot is x in [0, 18], y in [-18, 0] with street faces on -X and +Y and the cut corner at the
    origin; turned 90 degrees and set at (30, 30), those faces front the two roads and the cut
    corner looks across the junction at the park's NE corner."""
    path = SOURCE / "brick_corner.blend"
    if not path.exists():
        return None
    with bpy.data.libraries.load(str(path), link=True, relative=True) as (src, dst):
        dst.collections = [c for c in src.collections if c == "brick_corner"]
    inst = bpy.data.objects.new("brick_corner (linked from brick_corner.blend)", None)
    inst.instance_type = "COLLECTION"
    inst.instance_collection = dst.collections[0]
    inst.location = (FACADE, FACADE, WALK)
    inst.rotation_euler = (0, 0, math.pi / 2)
    col.objects.link(inst)
    return inst


def buildings(root):
    col = collection("Buildings", root)
    F = FACADE
    if link_brick_corner(col) is None:
        box(col, "brick corner", (F + LOT / 2, F + LOT / 2, WALK + 10.1), (LOT, LOT, 20.2), "bld_brick")
    label(col, "BRICK CORNER", (F + 6, F + 6, 26))
    box(col, "deco corner (3 storeys, 14.6 m)", (-(F + LOT / 2), F + LOT / 2, WALK + 7.3), (LOT, LOT, 14.6), "bld_deco")
    cylinder(col, "deco turret", (-(F + 4.5), F + 4.5, WALK + 16.5), 4.5, 4.5, "bld_deco", 24)
    label(col, "DECO CORNER", (-(F + 8), F + 8, 22))
    box(col, "tower podium (8.6 m)", (F + LOT / 2, -(F + LOT / 2), WALK + 4.3), (LOT, LOT, 8.6), "bld_podium")
    box(col, "glass tower (10 storeys)", (F + LOT / 2 + 1, -(F + LOT / 2 + 1), WALK + 8.6 + 17.5), (14, 14, 35), "bld_glass")
    label(col, "GLASS TOWER", (F + 10, -(F + 10), 47))
    x = F
    for i, (w, h) in enumerate([(6.2, 9.5), (5.2, 11.0), (6.6, 9.8)]):
        box(col, f"townhouse {i}", (-(x + w / 2), -(F + 5), WALK + h / 2), (w, 10, h), "bld_town")
        x += w
    box(col, "townhouse row back", (-(F + LOT / 2), -(F + 14), WALK + 5.5), (LOT, 8, 11), "bld_town")
    label(col, "TOWNHOUSES", (-(F + 9), -(F + 5), 15))
    # Mid blocks on the four park sides, between the cross streets: walk-ups facing the park.
    inner = ROAD - ROAD_HALF - SIDE   # 14
    k = 0
    for arm in range(4):
        r = rot(arm)
        u = -inner
        for w, h, colour in [(9.5, 12, "bld_walkup"), (9.0, 16, "bld_filler"), (9.5, 13, "bld_walkup")]:
            c = r @ Vector((u + w / 2, F + 7, WALK + h / 2))
            box(col, f"park-side walk-up {k}", c, (w - 0.3, 14, h), colour, rot_z=arm * math.pi / 2)
            u += w
            k += 1
        label(col, "WALK-UPS", r @ Vector((0, F + 7, 20)))
    # Along the grid streets beyond the corner lots: both sides, out to 110 m.
    heights = [11, 14, 9, 16, 12, 20, 13, 10, 18, 15, 11, 22]
    for arm in range(4):
        r = rot(arm)
        for c in (-ROAD, ROAD):
            for side in (-1, 1):
                d = F + LOT + 0.5
                while d < 110:
                    w = [9.0, 7.5, 11.0, 8.0][k % 4]
                    h = heights[k % len(heights)]
                    at = r @ Vector((c + side * (ROAD_HALF + SIDE + 7), d + w / 2, WALK + h / 2))
                    box(col, f"street block {k}", at, (14, w - 0.3, h), "bld_walkup" if k % 3 == 0 else "bld_filler",
                        rot_z=arm * math.pi / 2)
                    d += w
                    k += 1


# ------------------------------------------------------------------ dressing and horizon

def dressing(root):
    col = collection("Street dressing", root)
    inner, outer = ROAD - ROAD_HALF, ROAD + ROAD_HALF
    for sx, sy in QUADS:
        # A traffic signal on each park corner and each corner-block corner.
        for c in (inner + 0.6, outer + 0.6):
            cylinder(col, "signal pole", (sx * c, sy * c, 2.6), 0.14, 5.2, "pole", 8)
        # Street trees along the far sidewalks.
        for d in (6.0, 12.0):
            for p in ((sx * d, sy * (outer + 1.5)), (sx * (outer + 1.5), sy * d)):
                cylinder(col, "street tree trunk", (p[0], p[1], WALK + 1.8), 0.15, 3.4, "trunk", 8)
                blob(col, "street tree canopy", (p[0], p[1], WALK + 4.2), 1.6, "tree")
    # Power poles on the far sidewalks every 16 m, all the way out.
    for c in (-(outer + 0.8), outer + 0.8):
        d = -110.0
        while d < 110:
            cylinder(col, "power pole", (c, d, 4.5), 0.17, 9, "trunk", 8)
            cylinder(col, "power pole", (d, c, 4.5), 0.17, 9, "trunk", 8)
            d += 16
    # Traffic on the ring road: parked and passing, all outside the walls.
    box(col, "jeepney (N road)", (-6, ROAD - 2.4, 1.2), (6.5, 2.2, 2.4), "jeepney")
    box(col, "taxi (N road)", (8, ROAD + 2.4, 0.8), (4.5, 1.9, 1.6), "vehicle")
    box(col, "taxi (E road)", (ROAD + 2.4, -5, 0.8), (1.9, 4.5, 1.6), "vehicle")
    box(col, "jeepney (W road)", (-ROAD + 2.4, 7, 1.2), (2.2, 6.5, 2.4), "jeepney")
    box(col, "taxi (S road)", (-3, -ROAD - 2.4, 0.8), (4.5, 1.9, 1.6), "vehicle")
    for i in range(3):
        box(col, "roadworks barrier", (ROAD - 2.5 + i * 2.5, ROAD + 30, 0.6), (2.2, 0.4, 1.2), "barrier")
    # Bunting across the north road, park fence to the walk-ups, well above any throw arc.
    box(col, "bunting span", (0, ROAD, 6.6), (0.08, 2 * (ROAD_HALF + SIDE) + 6, 0.4), "bunting")


def horizon(root):
    """PEAK's layered distance: a ring of towers, then hills, both fading into the fog."""
    col = collection("Horizon", root)
    rng = random.Random(11)
    for i in range(46):
        a = rng.random() * math.tau
        d = 140 + rng.random() * 110
        s = 12 + rng.random() * 14
        h = 30 + rng.random() * (55 if d < 190 else 85)
        box(col, f"skyline {i}", (math.cos(a) * d, math.sin(a) * d, h / 2), (s, s, h), "skyline", rot_z=rng.random())
    for i in range(26):
        a = i / 26 * math.tau
        d = 380 + rng.random() * 120
        mesh = bpy.data.meshes.new(f"hill {i}")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=False, segments=7, radius1=60 + rng.random() * 60, radius2=0,
                              depth=40 + rng.random() * (120 if i % 5 == 0 else 60))
        bm.to_mesh(mesh)
        bm.free()
        o = _obj(col, f"hill {i}", mesh, "hill")
        o.location = (math.cos(a) * d, math.sin(a) * d, 20)


def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.62, 0.78, 0.95, 1)
    bg.inputs["Strength"].default_value = 0.6
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle = 4.0, math.radians(3)
    sun.rotation_euler = Euler((math.radians(42), 0, math.radians(-35)))
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        space.clip_end = 2000
                        space.clip_start = 0.1   # depth precision over a 2 km clip range


def preview(version):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 2000
    scene.collection.objects.link(cam)
    scene.camera = cam
    # The game's eye: 1.25 m up, 95 degrees horizontal FOV (a 16.5 mm lens on 36 mm film).
    # That view decides whether the layout reads; the plans only check the numbers.
    eye = 18 / math.tan(math.radians(95 / 2))
    shots = [
        ("plan", "ORTHO", Vector((0, 0, 200)), Vector((0, 0, 0)), 110),
        ("plan_park", "ORTHO", Vector((0, 0, 200)), Vector((0, 0, 0)), 36),
        ("aerial", "PERSP", Vector((-62, -70, 55)), Vector((4, 4, 4)), 30),
        ("court_north", "PERSP", Vector((1.2, -THROW - 2.5, WALK + 1.25)), Vector((0, 20, 4)), eye),
        ("court_ne", "PERSP", Vector((-6, -6, WALK + 1.25)), Vector((30, 30, 8)), eye),
        ("park_edge", "PERSP", Vector((11, -3, WALK + 1.25)), Vector((-12, 3, 2)), eye),
    ]
    for name, kind, pos, tgt, lens in shots:
        cam.data.type = kind
        if kind == "ORTHO":
            cam.data.ortho_scale = lens
        else:
            cam.data.lens = lens
        cam.location = pos
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"blockout_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[kanto] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    root = bpy.data.collections.new("kanto_blockout")
    bpy.context.scene.collection.children.link(root)
    street(root)
    park(root)
    gameplay(root)
    buildings(root)
    dressing(root)
    horizon(root)
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "kanto_blockout.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = SOURCE / "kanto_blockout.blend1"
    if backup.exists():
        backup.unlink()
    print("[kanto] blockout saved", out)
    if version:
        preview(version)


if __name__ == "__main__":
    main()
