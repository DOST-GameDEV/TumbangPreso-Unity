"""Block out the Ilalim ng Tulay rebuild in Blender, at game scale, before any detail (ILALIM-1.2).

  blender -b --python tools/author_ilalim_blockout.py -- [--preview N]

Writes ArtSource/ilalim/ilalim_blockout.blend. With --preview it also writes versioned renders
to Logs/ilalim-blender/blockout_<shot>_vN.png: two plans, two aerials and four views from inside
the play area at the game's eye (1.25 m, 95 degrees).

THE PLACE (docs/ILALIM_REWORK_GUIDE.md section 0). Taft Avenue at the Padre Faura corner,
Ermita, under LRT-1. The UP Manila and PGH campus is on the west side, the student shop row is
on the east side, and Rizal Hall stands at the north-west corner across Padre Faura, with its
portico facing south. The blockout takes the proposal's default answers to the open decisions
in section 0.6. Each one is a small edit here if the owner picks the alternative.

COORDINATES. Blender is Z-up. Blender X is the game's x (east), and Blender Y is the game's z
(north, toward UN Avenue). All numbers are metres. The export step owns the glTF axis change.

THE GAMEPLAY CONTRACT (guide section 1). These numbers are copied from the live builder
(Editor/MapKit/IlalimNgTulayBuilder.cs) and must not drift:
  * The chalk box is the 14 m carriageway: road top 0.000, kerb |x| 6.65..7.0 with its top at
    0.150 (painted white, the east and west chalk), and pavements |x| 7..11 with their top at 0.212.
  * The walls are at |x| = 11 and |y| = 16.5. Nothing solid stands inside the box.
  * The live piers are 1.4 m square at (+/-4.45, +/-10), the structural pairs are at +/-19, the
    soffit is 8.0, and the deck is 10.5 wide with tracks at x +/-2.35.
  * The spawns are at y = -9 (x -3, 0, 3), the lata is at the origin, and the throwing lines
    are at |y| = 8.
  * The bridge hoop is at (-8.9, -10), a basketball ring 3.05 m up on the west pavement.
  * The overclock pad is a 1.8 m square. It moves to the EAST pavement at (9.0, 5.5), outside
    PC Express, and mirrors its old west spot.
  * The pisonet, the pares cart at (8.8, -5) and the pisonet cord trip hazard stay on the east
    pavement, against the shopfront edge. The potholes stay flat at |x| = 3.4.
  * Every car stays outside |y| = 16.5.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"

BOX = 7.0             # Balance.ConfinementRadius
THROW = BOX + 1.0     # Confinement.ThrowingLine()
SPAWN = BOX + 2.0     # Confinement.AttackerSpawnRing()
KERB_IN = 6.65
KERB_TOP = 0.150
PAVE_TOP = 0.212
PAVE_OUT = 11.0       # PavementOuterX, the wall faces
WALL_Y = 16.5         # WallHalfZ
SOFFIT = 8.0          # ViaductSoffit
DECK_TOP = SOFFIT + 1.04
DECK_HALF = 10.5 / 2
TRACK_X = 2.35
PIER_X = 4.45
PIER_HALF = 0.70
LIVE_PIERS = (-10.0, 10.0)
STRUCT_PIERS = (-19.0, 19.0, -44.0, 44.0, -69.0, 69.0, -94.0, 94.0)
FAURA = (26.0, 36.0)       # Padre Faura carriageway, y from..to, one-way west
SOUTH_ST = (-36.0, -26.0)  # the side street toward Pedro Gil
EYE = 1.25
LAWN_TOP = 0.26

COLOURS = {
    "road":        (0.22, 0.22, 0.24),
    "road_cross":  (0.26, 0.26, 0.28),
    "kerb":        (0.95, 0.95, 0.92),
    "pavement":    (0.62, 0.58, 0.52),
    "chalk":       (1.00, 1.00, 1.00),
    "throw":       (0.95, 0.85, 0.25),
    "bounds":      (0.90, 0.15, 0.12),
    "spawn":       (0.30, 0.55, 0.80),
    "lata":        (0.80, 0.30, 0.20),
    "pothole":     (0.12, 0.12, 0.13),
    "concrete":    (0.60, 0.60, 0.58),
    "soffit":      (0.40, 0.40, 0.40),
    "rail":        (0.30, 0.28, 0.26),
    "train":       (0.95, 0.80, 0.20),
    "train_band":  (0.16, 0.22, 0.36),
    "lawn":        (0.36, 0.58, 0.22),
    "tree":        (0.24, 0.50, 0.14),
    "trunk":       (0.40, 0.27, 0.16),
    "fence":       (0.12, 0.16, 0.14),
    "heritage":    (0.93, 0.89, 0.78),
    "heritage_w":  (0.97, 0.96, 0.93),
    "red_roof":    (0.62, 0.24, 0.17),
    "rizal":       (0.88, 0.78, 0.58),
    "rizal_type":  (0.45, 0.10, 0.12),
    "hospital":    (0.90, 0.85, 0.74),
    "shop":        (0.80, 0.66, 0.56),
    "shop_b":      (0.70, 0.74, 0.66),
    "shop_c":      (0.86, 0.78, 0.58),
    "pcx":         (0.62, 0.64, 0.70),
    "pad":         (0.55, 0.30, 0.65),
    "pisonet":     (0.40, 0.70, 0.70),
    "pares":       (0.70, 0.45, 0.25),
    "cord":        (0.95, 0.85, 0.15),
    "awning":      (0.55, 0.62, 0.40),
    "midrise":     (0.74, 0.72, 0.68),
    "school":      (0.70, 0.78, 0.84),
    "tower":       (0.94, 0.90, 0.82),
    "tower_band":  (0.86, 0.58, 0.50),
    "skyline":     (0.62, 0.68, 0.74),
    "pole":        (0.28, 0.27, 0.25),
    "wire":        (0.08, 0.08, 0.08),
    "jeepney":     (0.85, 0.30, 0.22),
    "vehicle":     (0.90, 0.90, 0.88),
    "bus":         (0.30, 0.55, 0.45),
    "umbrella":    (0.90, 0.50, 0.30),
    "railing":     (0.95, 0.80, 0.20),
    "hoop":        (0.55, 0.25, 0.60),
    "signal":      (0.15, 0.15, 0.15),
    "street_sign": (0.15, 0.50, 0.30),
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
        bsdf.inputs["Alpha"].default_value = 0.06
        m.diffuse_color = (r, g, b, 0.06)
        if hasattr(m, "surface_render_method"):
            m.surface_render_method = "BLENDED"
    if name == "pad":
        bsdf.inputs["Emission Color"].default_value = (r, g, b, 1)
        bsdf.inputs["Emission Strength"].default_value = 1.5
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


def slab(col, name, x0, x1, y0, y1, top, thick, colour):
    """A box given by its plan extents and its TOP height, which is how the contract is written."""
    return box(col, name, ((x0 + x1) / 2, (y0 + y1) / 2, top - thick / 2), (x1 - x0, y1 - y0, thick), colour)


def cylinder(col, name, center, radius, depth, colour, sides=16, rot=(0, 0, 0)):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=sides, radius1=radius, radius2=radius, depth=depth)
    bm.to_mesh(mesh)
    bm.free()
    o = _obj(col, name, mesh, colour, smooth=sides > 10)
    o.location = center
    o.rotation_euler = rot
    return o


def cone(col, name, center, radius, depth, colour, sides=12):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=sides, radius1=radius, radius2=0.05, depth=depth)
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


def hip_roof(col, name, x0, x1, y0, y1, base, rise, overhang, colour):
    """A hipped roof: a rectangle at the eaves rising to a ridge along the long side."""
    x0, x1, y0, y1 = x0 - overhang, x1 + overhang, y0 - overhang, y1 + overhang
    w, d = x1 - x0, y1 - y0
    inset = min(w, d) / 2
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    if w >= d:
        verts = [(x0, y0, base), (x1, y0, base), (x1, y1, base), (x0, y1, base),
                 (x0 + inset, (y0 + y1) / 2, base + rise), (x1 - inset, (y0 + y1) / 2, base + rise)]
        faces = [(0, 1, 5, 4), (2, 3, 4, 5), (1, 2, 5), (3, 0, 4), (3, 2, 1, 0)]
    else:
        verts = [(x0, y0, base), (x1, y0, base), (x1, y1, base), (x0, y1, base),
                 ((x0 + x1) / 2, y0 + inset, base + rise), ((x0 + x1) / 2, y1 - inset, base + rise)]
        faces = [(1, 2, 5, 4), (3, 0, 4, 5), (0, 1, 4), (2, 3, 5), (3, 2, 1, 0)]
    vs = [bm.verts.new(v) for v in verts]
    for f in faces:
        bm.faces.new([vs[i] for i in f])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return _obj(col, name, mesh, colour)


def text(col, body, at, size, colour, rot=(math.pi / 2, 0, 0), extrude=0.04, render=True):
    curve = bpy.data.curves.new(body, "FONT")
    curve.body = body
    curve.size = size
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.extrude = extrude
    o = bpy.data.objects.new("text " + body, curve)
    o.data.materials.append(mat(colour))
    o.location = at
    o.rotation_euler = rot
    o.hide_render = not render
    col.objects.link(o)
    return o


def label(col, body, at, size=1.4):
    """A viewport label so the plan explains itself. Hidden in renders."""
    return text(col, body, at, size, "chalk", rot=(0, 0, 0), extrude=0.0, render=False)


# ------------------------------------------------------------------ ground

def ground(root):
    col = collection("Ground", root)
    # The road sits 2 cm below everything laid on it, so no two surfaces share a plane.
    slab(col, "Taft carriageway", -KERB_IN, KERB_IN, -140, 140, 0.0, 0.4, "road")
    for s in (-1, 1):
        # Kerb, painted white on top: the east and west chalk (Ilalim_Ng_Tulay.md section 2).
        slab(col, "kerb", *sorted((s * KERB_IN, s * BOX)), -140, 140, KERB_TOP, 0.4, "kerb")
        # The pavements stop at the cross streets and resume beyond them.
        for y0, y1 in ((-140, SOUTH_ST[0]), (SOUTH_ST[1], FAURA[0]), (FAURA[1], 140)):
            slab(col, "pavement", *sorted((s * BOX, s * PAVE_OUT)), y0, y1, PAVE_TOP, 0.4, "pavement")
    # Cross streets, running the full width of the plan.
    for name, (y0, y1) in (("Padre Faura", FAURA), ("side street", SOUTH_ST)):
        for s in (-1, 1):
            slab(col, name, *sorted((s * BOX, s * 140)), y0, y1, -0.02, 0.4, "road_cross")
    # West: the campus lawn behind the fence line. East: the shop row's lots.
    for y0, y1 in ((-140, SOUTH_ST[0]), (SOUTH_ST[1], FAURA[0]), (FAURA[1], 140)):
        slab(col, "campus lawn", -140, -PAVE_OUT, y0, y1, LAWN_TOP, 0.5, "lawn")
        slab(col, "east lots", PAVE_OUT, 140, y0, y1, PAVE_TOP + 0.02, 0.5, "pavement")


# ------------------------------------------------------------------ gameplay markers

def gameplay(root):
    col = collection("Gameplay", root)
    w = 0.12
    for s in (-1, 1):
        # North and south chalk across the road; east and west ride the kerb tops.
        slab(col, "chalk N/S", -BOX, BOX, s * BOX - w / 2, s * BOX + w / 2, 0.012, 0.03, "chalk")
        slab(col, "throwing line", -BOX, BOX, s * THROW - w / 2, s * THROW + w / 2, 0.012, 0.03, "throw")
        # Potholes: flat, off the spawn-to-can line.
        cylinder(col, "pothole", (s * 3.4, -s * 3.0, 0.004), 0.55, 0.01, "pothole", 12)
        # The playable walls, translucent: |x| = 11 and |y| = 16.5.
        box(col, "wall E/W", (s * (PAVE_OUT + 0.2), 0, 2.0), (0.4, 2 * WALL_Y, 4.0), "bounds")
        box(col, "wall N/S", (0, s * (WALL_Y + 0.2), 2.0), (2 * PAVE_OUT, 0.4, 4.0), "bounds")
    cylinder(col, "lata", (0, 0, 0.16), 0.08, 0.32, "lata", 12)
    for x in (-3.0, 0.0, 3.0):
        cylinder(col, "spawn", (x, -SPAWN, 0.01), 0.35, 0.02, "spawn", 12)
    # The bridge hoop on the west pavement, beside the south-west live pier row.
    hx, hy = -8.9, -10.0
    cylinder(col, "hoop post", (hx - 0.9, hy, PAVE_TOP + 1.8), 0.08, 3.6, "pole", 8)
    box(col, "hoop board", (hx - 0.55, hy, PAVE_TOP + 3.3), (0.06, 1.2, 0.8), "heritage_w")
    cylinder(col, "hoop ring", (hx - 0.2, hy, PAVE_TOP + 3.05), 0.25, 0.04, "hoop", 16)
    label(col, "HOOP", (hx, hy - 1.5, 4.5), 0.8)
    # The overclock pad, east pavement, outside PC Express.
    slab(col, "overclock pad", 8.1, 9.9, 4.6, 6.4, PAVE_TOP + 0.03, 0.03, "pad")
    label(col, "PAD", (9.0, 5.5, 1.0), 0.8)


# ------------------------------------------------------------------ LRT-1

def guideway(root):
    col = collection("LRT-1", root)
    length = 280
    slab(col, "deck", -DECK_HALF, DECK_HALF, -length / 2, length / 2, DECK_TOP, DECK_TOP - SOFFIT, "concrete")
    slab(col, "soffit", -DECK_HALF + 0.3, DECK_HALF - 0.3, -length / 2, length / 2, SOFFIT + 0.01, 0.02, "soffit")
    for s in (-1, 1):
        # The projecting parapet lip that gives LRT-1 its silhouette.
        slab(col, "parapet", s * DECK_HALF - 0.25, s * DECK_HALF + 0.25, -length / 2, length / 2, DECK_TOP + 1.1, 1.3, "concrete")
        for rail in (-0.72, 0.72):
            slab(col, "rail", s * TRACK_X + rail - 0.05, s * TRACK_X + rail + 0.05, -length / 2, length / 2, DECK_TOP + 0.18, 0.18, "rail")
    # Catenary masts on the deck's centre line, every 20 m.
    y = -length / 2 + 10
    while y < length / 2:
        cylinder(col, "catenary mast", (0, y, DECK_TOP + 2.6), 0.12, 5.2, "pole", 8)
        box(col, "catenary arm", (0, y, DECK_TOP + 5.0), (6.4, 0.15, 0.15), "pole")
        y += 20
    # Twin-leg piers under one cap. The real LRT-1 stands on single median piers; the game
    # needs two legs per row for the 7.5 m centre lane and the 1.85 m gutters (guide 0.4).
    for y in LIVE_PIERS + STRUCT_PIERS:
        for s in (-1, 1):
            box(col, "pier leg" + (" LIVE" if y in LIVE_PIERS else ""), (s * PIER_X, y, (SOFFIT - 0.9) / 2),
                (2 * PIER_HALF, 2 * PIER_HALF, SOFFIT - 0.9), "concrete")
        slab(col, "pier cap", -PIER_X - 1.0, PIER_X + 1.0, y - 0.9, y + 0.9, SOFFIT, 0.9, "concrete")
    # A parked consist north of the court for scale: 15.6 m, 2.6 m wide (TrainConsistHalfLength).
    ty = 34.0
    for i, (y0, y1) in enumerate(((ty - 7.8, ty - 2.7), (ty - 2.55, ty + 2.55), (ty + 2.7, ty + 7.8))):
        slab(col, f"train car {i}", TRACK_X - 1.3, TRACK_X + 1.3, y0, y1, DECK_TOP + 3.6, 3.3, "train")
        slab(col, f"train band {i}", TRACK_X - 1.32, TRACK_X + 1.32, y0 + 0.1, y1 - 0.1, DECK_TOP + 2.3, 0.7, "train_band")
    label(col, "LRT-1", (0, -30, DECK_TOP + 3), 2.0)


# ------------------------------------------------------------------ west: the campus

def campus(root):
    col = collection("Campus (west)", root)
    rng = random.Random(7)
    # A low iron fence ON the wall line, with gaps at the gates. See-through, so the frame opens.
    for y0, y1 in ((-120, -40), (-36 + 10, -3), (3, FAURA[0]), (FAURA[1], 120)):
        slab(col, "campus fence rail", -PAVE_OUT - 0.05, -PAVE_OUT + 0.05, y0, y1, PAVE_TOP + 1.25, 0.08, "fence")
        y = y0
        while y <= y1:
            box(col, "campus fence post", (-PAVE_OUT, y, PAVE_TOP + 0.62), (0.12, 0.12, 1.25), "fence")
            y += 2.5
    # Vendors against the fence, never mid-pavement: stalls 1.1 m deep with striped umbrellas.
    for y in (12.5, 14.6, -14.2):
        box(col, "vendor stall", (-PAVE_OUT + 0.65, y, PAVE_TOP + 0.45), (1.1, 1.6, 0.9), "shop_c")
        cylinder(col, "umbrella pole", (-PAVE_OUT + 0.65, y, PAVE_TOP + 1.3), 0.03, 2.6, "pole", 6)
        cone(col, "umbrella", (-PAVE_OUT + 0.65, y, PAVE_TOP + 2.55), 1.2, 0.5, "umbrella")

    # The hospital front: three storeys behind a forecourt, south-west. PGH is the real neighbour.
    slab(col, "hospital front", -42, -16, -58, -4, LAWN_TOP + 12.0, 12.0, "hospital")
    hip_roof(col, "hospital roof", -42, -16, -58, -4, LAWN_TOP + 12.0, 4.0, 0.9, "red_roof")
    for y in (-50, -30, -12):
        slab(col, "hospital gate canopy", -16, -12.5, y - 3, y + 3, LAWN_TOP + 3.6, 0.3, "heritage_w")

    # RIZAL HALL at the north-west corner across Padre Faura, portico facing south (guide 0.3).
    # Real distance from Taft ~100 m, pulled in to ~48 m. Footprint 28 x 14, three storeys.
    rx0, rx1, ry0, ry1 = -62.0, -34.0, 44.0, 58.0
    top = LAWN_TOP + 13.5
    slab(col, "RIZAL HALL body", rx0, rx1, ry0, ry1, top, 13.5, "rizal")
    hip_roof(col, "RIZAL HALL roof", rx0, rx1, ry0, ry1, top, 4.5, 1.4, "red_roof")
    # The portico: tall Ionic columns under an entablature, three bays deep of front steps.
    px0, px1 = -54.0, -42.0
    slab(col, "RIZAL HALL portico floor", px0, px1, ry0 - 3.2, ry0, LAWN_TOP + 1.2, 1.2, "heritage")
    for i in range(6):
        x = px0 + 0.8 + i * (px1 - px0 - 1.6) / 5
        cylinder(col, "RIZAL HALL column", (x, ry0 - 2.6, LAWN_TOP + 1.2 + 5.4), 0.45, 10.8, "heritage_w", 16)
    slab(col, "RIZAL HALL entablature", px0 - 0.3, px1 + 0.3, ry0 - 3.4, ry0, LAWN_TOP + 13.5, 1.5, "heritage_w")
    for k in range(3):
        slab(col, "RIZAL HALL step", px0 + 1, px1 - 1, ry0 - 3.2 - 0.45 * (k + 1), ry0 - 3.2 - 0.45 * k,
             LAWN_TOP + 1.2 - 0.4 * (k + 1), 0.3, "heritage")
    text(col, "RIZAL HALL", ((px0 + px1) / 2, ry0 - 3.45, LAWN_TOP + 12.75), 0.9, "rizal_type")
    # The Oblation on its plinth in front of the steps (guide 0.6 decision 3: included by default).
    box(col, "Oblation plinth", ((px0 + px1) / 2, ry0 - 7.2, LAWN_TOP + 0.9), (1.6, 1.6, 1.8), "heritage")
    cylinder(col, "Oblation figure", ((px0 + px1) / 2, ry0 - 7.2, LAWN_TOP + 2.8), 0.28, 2.0, "heritage_w", 10)
    label(col, "RIZAL HALL", ((px0 + px1) / 2, (ry0 + ry1) / 2, top + 7), 2.4)

    # The columned corner hall at Taft and Padre Faura, standing in for the Supreme Court. White,
    # unnamed, with its colonnade on the Padre Faura side.
    slab(col, "corner hall", -29, -14, 40, 58, LAWN_TOP + 14.0, 14.0, "heritage_w")
    hip_roof(col, "corner hall roof", -29, -14, 40, 58, LAWN_TOP + 14.0, 3.0, 0.8, "red_roof")
    for i in range(5):
        cylinder(col, "corner hall column", (-27.5 + i * 3.0, 38.6, LAWN_TOP + 5.5), 0.4, 11.0, "heritage_w", 12)
    slab(col, "corner hall entablature", -29, -14, 37.8, 40, LAWN_TOP + 12.2, 1.2, "heritage_w")

    # Campus wings behind: low cream blocks with red roofs, the "sea of red roofs".
    wings = [(-110, -70, -60, -30), (-110, -80, -20, 20), (-70, -48, -20, 12), (-100, -70, 40, 70),
             (-60, -30, 66, 90), (-110, -70, 80, 110), (-40, -16, 66, 96), (-110, -75, -110, -70),
             (-60, -20, -110, -70)]
    for i, (x0, x1, y0, y1) in enumerate(wings):
        h = 8 + rng.random() * 6
        slab(col, f"campus wing {i}", x0, x1, y0, y1, LAWN_TOP + h, h, "heritage")
        hip_roof(col, f"campus wing roof {i}", x0, x1, y0, y1, LAWN_TOP + h, 3.0, 0.8, "red_roof")

    # Trees: shade trees along the fence and on the lawn, kept off the Rizal Hall view line.
    def on_view_line(x, y):
        ax, ay, bx, by = 2.0, -12.0, -48.0, 41.0
        t = max(0.0, min(1.0, ((x - ax) * (bx - ax) + (y - ay) * (by - ay)) / ((bx - ax) ** 2 + (by - ay) ** 2)))
        return math.hypot(x - (ax + t * (bx - ax)), y - (ay + t * (by - ay))) < 7.0

    spots = [(-14.0, y) for y in (-52, -40, -30, -20, -8, 8, 20)] + [(-14.5, 60), (-14.5, 80), (-14.5, 100)]
    for _ in range(260):
        spots.append((-16 - rng.random() * 90, -100 + rng.random() * 200))
    for x, y in spots:
        if on_view_line(x, y):
            continue
        if -62 - 3 < x < -34 + 3 and 36 < y < 61:       # Rizal Hall and its forecourt
            continue
        if -42 - 2 < x < -12 and -60 < y < -2:           # the hospital front
            continue
        if -30 < x < -13 and 36 < y < 60:                # the corner hall
            continue
        if FAURA[0] - 2 < y < FAURA[1] + 2 or SOUTH_ST[0] - 2 < y < SOUTH_ST[1] + 2:
            continue
        if any(x0 - 2 < x < x1 + 2 and y0 - 2 < y < y1 + 2 for x0, x1, y0, y1 in wings):
            continue
        h = 5 + rng.random() * 4
        cylinder(col, "tree trunk", (x, y, LAWN_TOP + h / 2), 0.25, h, "trunk", 8)
        blob(col, "tree canopy", (x, y, LAWN_TOP + h + 1.2), 2.8 + rng.random() * 1.6, "tree")


# ------------------------------------------------------------------ east: the shop row

def shops(root):
    col = collection("Shop row (east)", root)
    rng = random.Random(3)
    base = PAVE_TOP + 0.02
    # The near row, south to north. Varied widths, heights and setbacks, never mirrored.
    row = [  # (y0, y1, depth, height, colour, name)
        (-26.0, -16.5, 8, 9, "shop_b", "dorm (bedspace)"),
        (-16.5, -9.5, 6, 7, "shop_c", "print xerox bind"),
        (-9.5, -1.5, 7, 6, "shop", "carinderia (pares cart in front)"),
        (-1.5, 9.0, 8, 8, "pcx", "PC EXPRESS"),
        (9.0, 15.0, 6, 6.5, "shop_c", "pisonet"),
        (15.0, 20.5, 7, 10, "shop_b", "medical supply, uniforms"),
        (20.5, 26.0, 9, 14, "shop", "corner mid-rise"),
    ]
    for y0, y1, depth, h, colour, name in row:
        setback = 0.0 if name in ("PC EXPRESS", "pisonet", "carinderia (pares cart in front)") else rng.choice((0.0, 0.6, 1.0))
        x0 = PAVE_OUT + setback
        slab(col, "shop " + name, x0, x0 + depth, y0 + 0.1, y1 - 0.1, base + h, h, colour)
        # Awning at one of three depths over the pavement edge (never below head height).
        slab(col, "awning", PAVE_OUT - rng.choice((0.8, 1.2, 1.6)), x0, y0 + 0.4, y1 - 0.4, base + 3.2, 0.12, "awning")
        label(col, name, (PAVE_OUT + depth / 2, (y0 + y1) / 2, base + h + 1.5), 0.7)
    # PC Express gets its fascia band; the rest of its detail comes in the kit pass.
    slab(col, "PC Express fascia", PAVE_OUT - 0.25, PAVE_OUT, -1.2, 8.7, base + 5.2, 1.2, "train_band")
    # Pisonet: three terminals against the shopfront edge, and the cord that trips.
    for i in range(3):
        box(col, "pisonet terminal", (9.7, 10.0 + i * 1.75, base + 0.8), (0.7, 0.9, 1.6), "pisonet")
    slab(col, "pisonet cord TRIP", 7.7, 9.1, 10.0, 12.6, base + 0.03, 0.03, "cord")
    # The pares cart, south of PC Express, with its A-board.
    box(col, "pares cart", (8.8, -5.0, base + 1.3), (1.72, 2.03, 2.6), "pares")
    box(col, "pares A-board", (8.05, -3.75, base + 0.4), (0.2, 0.9, 0.8), "shop_c")

    # The second row and the district: mid-rises and dorms, stepping up away from Taft.
    for y in range(-120, 121, 12):
        if FAURA[0] - 3 < y < FAURA[1] + 3 or SOUTH_ST[0] - 3 < y < SOUTH_ST[1] + 3:
            continue
        for x0 in (24.0, 40.0, 58.0):
            if 45 < x0 + 8 < 62 and -8 < y < 12:        # the banded tower's lot
                continue
            h = (8 if x0 == 24 else 14) + rng.random() * (16 if x0 == 24 else 30)
            slab(col, "mid-rise", x0, x0 + 12, y + 0.8, y + 11.2, base + h, h, "midrise")
    # The banded residential tower, cream and salmon (the Astral Tower silhouette, unnamed).
    tx0, tx1, ty0, ty1 = 48.0, 60.0, -6.0, 8.0
    slab(col, "banded tower", tx0, tx1, ty0, ty1, base + 62, 62, "tower")
    for k in range(19):
        z = base + 3 + k * 3.2
        slab(col, "tower band", tx0 - 0.25, tx1 + 0.25, ty0 - 0.25, ty1 + 0.25, z + 0.7, 0.7, "tower_band")
    label(col, "BANDED TOWER", ((tx0 + tx1) / 2, (ty0 + ty1) / 2, base + 66), 2.0)
    # North-east corner across Padre Faura: the tall school block (Manila Science HS stand-in).
    slab(col, "school block", 14, 40, 39, 60, base + 34, 34, "school")
    label(col, "SCHOOL", (27, 50, base + 37), 2.0)
    # Wires: poles on the shopfront edge every 12 m, and a sagging cable run between them.
    y = -118.0
    while y < 118:
        if not (FAURA[0] - 1 < y < FAURA[1] + 1 or SOUTH_ST[0] - 1 < y < SOUTH_ST[1] + 1):
            cylinder(col, "power pole", (10.65, y, base + 4.8), 0.16, 9.6, "pole", 8)
        y += 12
    for k, z in enumerate((8.6, 8.1, 7.6, 7.2)):
        slab(col, "cable run", 10.55 + k * 0.08, 10.6 + k * 0.08, -118, 118, base + z, 0.04, "wire")


# ------------------------------------------------------------------ street furniture and traffic

def street(root):
    col = collection("Street", root)
    # Traffic lights on mast arms at the Padre Faura corners.
    for sx in (-1, 1):
        for y in (FAURA[0] - 0.8, FAURA[1] + 0.8):
            cylinder(col, "signal pole", (sx * (BOX + 0.6), y, PAVE_TOP + 3.0), 0.14, 6.0, "signal", 8)
            box(col, "signal arm", (sx * (BOX - 2.4), y, PAVE_TOP + 5.8), (6.0, 0.15, 0.15), "signal")
            box(col, "signal head", (sx * (BOX - 4.8), y, PAVE_TOP + 5.2), (0.35, 0.35, 1.0), "signal")
    # Green street-name blades at the north-west corner.
    cylinder(col, "street sign post", (-BOX - 1.0, FAURA[0] - 1.0, PAVE_TOP + 1.5), 0.05, 3.0, "pole", 6)
    box(col, "blade TAFT AVE", (-BOX - 1.0, FAURA[0] - 1.0, PAVE_TOP + 2.9), (0.04, 1.4, 0.3), "street_sign")
    box(col, "blade PADRE FAURA", (-BOX - 1.0, FAURA[0] - 1.0, PAVE_TOP + 2.55), (1.6, 0.04, 0.3), "street_sign")
    # Yellow steel railing on the Padre Faura kerbs.
    for sx in (-1, 1):
        slab(col, "yellow railing", sx * 7.4 - 0.04, sx * 7.4 + 0.04, FAURA[1] + 0.4, FAURA[1] + 9.0, PAVE_TOP + 1.0, 0.06, "railing")
    # Traffic, all outside |y| = 16.5: jeepneys and a bus on Taft, cars on Padre Faura.
    box(col, "jeepney (Taft, northbound)", (3.5, 23.0, 1.2), (2.2, 6.5, 2.4), "jeepney")
    box(col, "jeepney (Taft, southbound)", (-3.5, -24.0, 1.2), (2.2, 6.5, 2.4), "jeepney")
    box(col, "bus (Taft)", (3.5, 52.0, 1.6), (2.5, 11.0, 3.2), "bus")
    box(col, "UV Express van", (-3.5, -48.0, 1.0), (1.9, 4.8, 2.0), "vehicle")
    box(col, "car (Padre Faura)", (-24.0, 29.0, 0.8), (4.4, 1.9, 1.5), "vehicle")
    box(col, "jeepney (Padre Faura)", (-40.0, 32.5, 1.2), (6.5, 2.2, 2.4), "jeepney")
    box(col, "car (side street)", (22.0, -31.0, 0.8), (4.4, 1.9, 1.5), "vehicle")


def horizon(root):
    """A ring of towers fading into fog: Manila's skyline beyond the district."""
    col = collection("Horizon", root)
    rng = random.Random(11)
    for i in range(56):
        a = rng.random() * math.tau
        d = 150 + rng.random() * 120
        s = 14 + rng.random() * 16
        h = 25 + rng.random() * (60 if d < 210 else 100)
        box(col, f"skyline {i}", (math.cos(a) * d, math.sin(a) * d, h / 2), (s, s, h), "skyline", rot_z=rng.random())


def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.78, 0.80, 0.86, 1)
    bg.inputs["Strength"].default_value = 0.55
    # Late afternoon (guide 0.4): a warm sun low from the west, through the campus trees, so the
    # viaduct lays its shade band across the court.
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle = 3.6, math.radians(3)
    sun.data.color = (1.0, 0.86, 0.68)
    direction = Vector((0.80, -0.25, -0.55)).normalized()
    sun.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        space.clip_end = 2000
                        space.clip_start = 0.1


def preview(version):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 2000
    scene.collection.objects.link(cam)
    scene.camera = cam
    # The game's eye: 1.25 m above the pavement or road, 95 degrees horizontal.
    eye = 18 / math.tan(math.radians(95 / 2))
    e = EYE
    shots = [
        ("plan", "ORTHO", Vector((0, 5, 250)), Vector((0, 5, 0)), 150),
        ("plan_court", "ORTHO", Vector((0, 0, 250)), Vector((0, 0, 0)), 42),
        ("aerial_campus", "PERSP", Vector((55, -70, 55)), Vector((-20, 15, 4)), 26),
        ("aerial_shops", "PERSP", Vector((-60, -55, 45)), Vector((10, 5, 4)), 26),
        ("spawn_north", "PERSP", Vector((0.0, -SPAWN, e)), Vector((0, 20, 4)), eye),
        ("rizal_view", "PERSP", Vector((2.0, -12.0, e)), Vector((-48, 41, 8)), eye),
        ("west_pavement_east", "PERSP", Vector((-9.5, -4.0, PAVE_TOP + e)), Vector((12, 6, 3)), eye),
        ("taya_south", "PERSP", Vector((0.0, 4.0, e)), Vector((0, -30, 2)), eye),
    ]
    for name, kind, pos, tgt, lens in shots:
        cam.data.type = kind
        if kind == "ORTHO":
            cam.data.ortho_scale = lens
        else:
            cam.data.lens = lens
        cam.location = pos
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        # The court plan is taken without the viaduct, which would otherwise cover the box.
        bpy.data.collections["LRT-1"].hide_render = name == "plan_court"
        scene.render.filepath = str(PREVIEWS / f"blockout_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[ilalim] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    root = bpy.data.collections.new("ilalim_blockout")
    bpy.context.scene.collection.children.link(root)
    ground(root)
    gameplay(root)
    guideway(root)
    campus(root)
    shops(root)
    street(root)
    horizon(root)
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "ilalim_blockout.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = SOURCE / "ilalim_blockout.blend1"
    if backup.exists():
        backup.unlink()
    print("[ilalim] blockout saved", out)
    if version:
        preview(version)


if __name__ == "__main__":
    main()
