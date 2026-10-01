"""Model the corner sari-sari store at Taft and Padre Faura (ILALIM-1.3, sari-sari kit).

  py -3 tools/author_ilalim_textures_sarisari.py [--sheet N]     # paint the sari_ textures first
  blender -b --python tools/author_ilalim_sarisari.py -- [--preview N] [--only a,b]

Writes ArtSource/ilalim/sarisari.blend. With --preview it writes versioned close-up renders
(against plain stand-in ground) to Logs/ilalim-blender/sari_<shot>_vN.png; the views from the court
come from tools/author_ilalim_city.py, which links this kit.

Owner, 2026-09-29: "can you put a sari sari store somewhere". The props kit already has a
sidewalk sari-sari STAND by the PGH fence; this is a proper store: a two-storey corner house
whose ground-floor front room is the tindahan, selling through a grilled counter window.

THE LOT. The free north-east corner of Taft and Padre Faura, checked against the assembled city
(every kit linked): the lot surface (top 0.24) runs x 11.8..22.8, y 38.3..47.3, between the Taft
east pavement, the Padre Faura north pavement (y 35..38.3, top 0.212) and the 4-storey block at
x 22.8.., y 47.3... Nothing stands on it. Around it, and left clear: the Taft signal pole at
(11.46, 39.2) with its arm at 6.5..7.2 up, the street pole at (10.58, 38.98), and the fig tree
at (21.1, 36.3) whose canopy (2.5..7.1 up) overhangs the house's south-east corner. From the
court the store's south face is in view from the spawn (0, -9) and the taya spot (0, 4), at
about 49 m; the piers hide it from the centre line.

THE HOUSE (Blender X = game x east, Y = game z north; heights absolute):
  ground floor   plastered hollow block, faded rose, walls x 12.3..19.3, y 38.9..45.9, 0.30..3.31.
                 The store room is the south-west corner, x 12.45..15.8, y 39.05..40.6, with a
                 back wall of shelves.
  floor band     a slab x 12.10..19.3, y 38.40..45.9, 3.30..3.58: the upper floor overhangs the
                 pavement side by half a metre, as Manila houses do.
  upper floor    cream lap siding, x 12.15..19.3, y 38.45..45.9, 3.57..6.10, gables east and west
                 up to a ridge at 7.22 (y 42.18). Jalousie windows, a window aircon on the Taft side.
  roof           galvanised iron, eaves 0.5 m out (y 37.95..46.4, x 11.80..19.75), fascia and
                 barge boards, a ridge roll.
  the tindahan   the counter window x 12.75..15.55, 1.24..2.40: a plank ledge, a green steel grille
                 with a pass-through gap over the ledge, candy jars inside, sachet strips hanging
                 behind the bars, the BAWAL ANG UTANG card on the grille, stocked shelves, a fluorescent tube.
                 Over it a green and cream striped awning to y 37.45 (2.78 up at its front edge) on
                 two steel arms, snack strips hanging at its corners, and the painted signboard
                 BEBANG'S SARI-SARI STORE (4.2 x 0.75) across the floor band.
  outside        a plank bench and a red cooler (the MAY YELO card on its lid) against the front
                 wall, the house door, crates of empty bottles on the Taft side, a tin sign
                 BIGAS / ASUKAL / MANTIKA, the electric meter with its service drop from the
                 street pole, and a side gate (maroon, NO PARKING) in a hollow-block wall to x 22.75.
  apron          concrete, top 0.30, along the south and west faces (over the lot, never on the
                 pavement).

THE HOUSE STYLE (KANTO_DESIGN_GUIDE.md section 2, LAGOON_REWORK_GUIDE.md section 2), through the
prop kit's PBuf (tools/author_ilalim_props.py): chunky members, live bevels with hardened normals,
no coplanar faces (parts sink 0.5 to 2 cm into what carries them), world UVs at 2 m, and the drawn
positional grime maps UVGrime (below each object's top) and UVSplash (above the ground).

Every object's ORIGIN is the store anchor ORIGIN below (the counter's centre on the front wall, at
lot level), so the Unity builder can place the whole store with one position.

ROLE HUES: nothing near #f87020 or #0080e8. The reds are crimson and brick, the walls a dusty rose
and cream, the greens bottle green (kept yellow-green, never teal).
"""
import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_props as P                          # noqa: E402  (PBuf, materials, small props)
from author_ilalim_props import PBuf, crate, facing, catmull, collection   # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"

LOT = 0.24                     # lot surface
APRON = 0.30                   # the concrete apron and the store floor's threshold
FLOOR = 0.36                   # inside floor
X0, X1, Y0, Y1 = 12.3, 19.3, 38.9, 45.9
WALL = 0.15
GF_TOP = 3.30
SLAB_TOP = 3.58
EAVE = 6.10
YR = (38.45 + Y1) / 2          # ridge line
RIDGE = EAVE + 0.3 * (YR - 38.45)
WIN = (12.75, 15.55, 1.24, 2.40)      # the counter window: x0, x1, z0, z1
DOOR = (18.1, 19.0)
ORIGIN = Vector(((WIN[0] + WIN[1]) / 2, Y0, LOT))

# This kit's materials, in the prop kit's format:
# name: (texture or None, tint, roughness, metallic, emission, grime)
P.M.update({
    "sari_wall_rose":  ("sari_plaster", (0.84, 0.60, 0.57), 0.9, 0.0, 0, True),
    "sari_wall_grey":  ("sari_plaster", (0.80, 0.79, 0.75), 0.9, 0.0, 0, True),
    "sari_interior":   ("sari_plaster", (0.70, 0.62, 0.58), 0.9, 0.0, 0, False),
    "sari_siding":     ("sari_siding", (0.94, 0.86, 0.64), 0.85, 0.0, 0, True),
    "sari_trim":       ("prop_paint", (0.93, 0.91, 0.85), 0.6, 0.0, 0, True),
    "sari_grille":     ("prop_paint", (0.10, 0.24, 0.15), 0.55, 0.1, 0, True),
    "sari_gi":         ("sari_giroof", None, 0.5, 0.3, 0, False),
    "sari_floor":      ("prop_concrete", (0.90, 0.84, 0.76), 0.9, 0.0, 0, False),
    "sari_apron":      ("prop_concrete", None, 0.9, 0.0, 0, True),
    "sari_sign_main":  ("sari_sign_main", None, 0.8, 0.0, 0, False),
    "sari_sign_tin":   ("sari_sign_tin", None, 0.45, 0.2, 0, False),
    "sari_sign_yelo":  ("sari_sign_yelo", None, 0.9, 0.0, 0, False),
    "sari_sign_utang": ("sari_sign_utang", None, 0.8, 0.0, 0, False),
    "sari_stock":      ("sari_stock", None, 0.7, 0.0, 0, False),
    "sari_snacks":     ("sari_snacks", None, 0.35, 0.0, 0, False),
    "sari_jalousie":   ("sari_jalousie", None, 0.25, 0.0, 0, False),
    "sari_door":       ("sari_door", None, 0.6, 0.0, 0, False),
    "sari_gate":       ("sari_gate", None, 0.6, 0.2, 0, False),
    "sari_bottle":     (None, (0.30, 0.45, 0.32), 0.2, 0.0, 0, False),
    "sari_meter_face": (None, (0.90, 0.90, 0.86), 0.2, 0.0, 0, False),
})


def box(b, x0, x1, y0, y1, z0, z1, mat, r=0.02):
    b.rbox(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), mat, r=r)


def roof_z(y):
    """The top line of the upper walls under the roof (the gable edge)."""
    return RIDGE - 0.3 * abs(y - YR)


# ------------------------------------------------------------------ the shell

def walls(col):
    b = PBuf("sarisari_walls", top=GF_TOP)
    m = "sari_wall_rose"
    # South wall, around the counter window and the door. The pieces under and over the window
    # sit 1 cm back so no two front faces share a plane; the trim frame covers the joints.
    wx0, wx1, wz0, wz1 = WIN
    box(b, X0, wx0, Y0, Y0 + WALL, APRON - 0.02, GF_TOP + 0.01, m)
    box(b, wx0 - 0.01, wx1 + 0.01, Y0 + 0.01, Y0 + WALL, APRON - 0.02, wz0, m)
    box(b, wx0 - 0.01, wx1 + 0.01, Y0 + 0.01, Y0 + WALL, wz1, GF_TOP + 0.01, m)
    box(b, wx1, DOOR[0], Y0, Y0 + WALL, APRON - 0.02, GF_TOP + 0.01, m)
    box(b, DOOR[0] - 0.01, DOOR[1] + 0.01, Y0 + 0.01, Y0 + WALL, 2.40, GF_TOP + 0.01, m)
    box(b, DOOR[1], X1, Y0, Y0 + WALL, APRON - 0.02, GF_TOP + 0.01, m)
    # West (Taft), east and north walls, whole.
    box(b, X0, X0 + WALL, Y0 + 0.01, Y1, APRON - 0.02, GF_TOP + 0.01, m)
    box(b, X1 - WALL, X1, Y0 + 0.01, Y1, APRON - 0.02, GF_TOP + 0.01, m)
    box(b, X0 + 0.01, X1 - 0.01, Y1 - WALL, Y1, APRON - 0.02, GF_TOP + 0.01, m)
    # The store room: a partition east of it and a back wall carrying the shelves.
    box(b, 15.75, 15.87, Y0 + WALL - 0.01, Y1 - WALL + 0.01, FLOOR - 0.02, GF_TOP + 0.01, "sari_interior")
    box(b, X0 + WALL - 0.01, 15.76, 40.6, 40.72, FLOOR - 0.02, GF_TOP + 0.01, "sari_interior")
    # Floors: the apron outside (south and west faces), the tiled floor inside.
    box(b, 11.85, X1 + 0.02, 38.25, Y0 + 0.04, LOT - 0.06, APRON, "sari_apron", r=0.03)
    box(b, 11.85, X0 + 0.04, Y0 + 0.03, Y1, LOT - 0.06, APRON - 0.005, "sari_apron", r=0.03)
    box(b, X0 + 0.05, X1 - 0.05, Y0 + 0.05, Y1 - 0.05, LOT - 0.05, FLOOR, "sari_floor", r=0.01)
    # The floor band over everything, proud of both floors on the street sides.
    box(b, 12.10, X1 + 0.03, 38.40, Y1 + 0.03, GF_TOP, SLAB_TOP, "sari_wall_grey", r=0.03)
    b.finish(col, origin=ORIGIN, bevel=0.02)


def upper(col):
    b = PBuf("sarisari_upper", top=EAVE)
    ys, ye = 38.45, Y1
    prof = [(ys, SLAB_TOP - 0.01), (ye, SLAB_TOP - 0.01), (ye, EAVE), (YR, RIDGE), (ys, EAVE)]
    rings = [[Vector((x, y, z)) for y, z in prof] for x in (12.15, X1)]
    b.loft(rings, "sari_siding")
    # Jalousie windows: a trim frame proud of the wall, the glass panel set just inside it, and a
    # deep sill.
    def window(face, cx, cz, w=1.4, h=1.2):
        wall = {"-y": ys, "-x": 12.15}[face]
        n = {"-y": Vector((0, -1, 0)), "-x": Vector((-1, 0, 0))}[face]
        along = Vector((1, 0, 0)) if face == "-y" else Vector((0, -1, 0))

        def at(u, z, out):
            base = Vector((cx, wall, 0)) if face == "-y" else Vector((wall, cx, 0))
            return base + along * u + n * out + Vector((0, 0, z))

        c = at(0, cz, 0.02)
        b.panel(facing(face, c), w - 0.1, h - 0.1, 0.05, "sari_trim", "sari_jalousie", r=0.01)
        t = 0.09
        for u in (-w / 2 + t / 2, w / 2 - t / 2):
            p = at(u, cz, 0.04)
            size = (t, 0.1, h) if face == "-y" else (0.1, t, h)
            b.rbox(p, size, "sari_trim", r=0.015)
        for z, depth in ((cz + h / 2 - t / 2, 0.1), (cz - h / 2 - 0.02, 0.16)):
            p = at(0, z, depth / 2 - 0.01)
            size = (w + 0.12, depth, t) if face == "-y" else (depth, w + 0.12, t)
            b.rbox(p, size, "sari_trim", r=0.015)

    window("-y", 13.75, 4.95)
    window("-y", 17.55, 4.95)
    window("-x", 41.8, 4.95)
    # A window aircon through the Taft wall: a chunky cream box with a dark vent and a drip tray.
    ac = Vector((12.15 - 0.2, 43.9, 4.35))
    b.rbox(ac, (0.46, 0.62, 0.42), "prop_plastic_cream", r=0.04)
    for k in range(4):
        b.rbox(ac + Vector((-0.235, 0, 0.12 - k * 0.065)), (0.03, 0.5, 0.028), "prop_slot", r=0.01)
    b.rbox(Vector((11.93, 43.9, 4.11)), (0.36, 0.5, 0.06), "prop_steel_dark", r=0.01)
    b.finish(col, origin=ORIGIN, bevel=0.02)


def roof(col):
    b = PBuf("sarisari_roof")
    xa, xb = 11.80, 19.75
    t = 0.05
    for ya, yb in ((37.95, YR), (46.4, YR)):
        bottom = [Vector((x, y, roof_z(y) - 0.012)) for x, y in ((xa, ya), (xb, ya), (xb, yb), (xa, yb))]
        top = [v + Vector((0, 0, t)) for v in bottom]
        b.loft([bottom, top], "sari_gi")
    # The ridge roll, fascia along the eaves, barge boards up the gables.
    b.tube([Vector((xa - 0.02, YR, RIDGE + 0.03)), Vector((xb + 0.02, YR, RIDGE + 0.03))], 0.075, "sari_gi", sides=10)
    for y in (37.95, 46.4):
        z = roof_z(y)
        box(b, xa - 0.03, xb + 0.03, y - 0.02 if y < YR else y - 0.02, y + 0.02, z - 0.2, z + 0.02, "sari_trim", r=0.012)
    for x in (xa - 0.01, xb + 0.01):
        for ya, yb in ((37.95, YR), (46.4, YR)):
            p0 = Vector((x, ya, roof_z(ya) - 0.08))
            p1 = Vector((x, yb, roof_z(yb) - 0.08))
            b.prism(p0, p1, 0.035, 0.035, "sari_trim", r=0.012)
            b.prism(p0 + Vector((0, 0, -0.07)), p1 + Vector((0, 0, -0.07)), 0.03, 0.03, "sari_trim", r=0.012)
    b.finish(col, origin=ORIGIN, bevel=0.015)


# ------------------------------------------------------------------ the tindahan front

def front(col):
    wx0, wx1, wz0, wz1 = WIN
    b = PBuf("sarisari_front", top=GF_TOP)
    ym = Y0 + WALL / 2
    # The trim frame round the window (proud of the wall), and the plank ledge on steel brackets.
    t = 0.09
    box(b, wx0 - t, wx1 + t, Y0 - 0.03, Y0 + 0.02, wz1, wz1 + t, "sari_trim", r=0.012)
    for x in (wx0 - t, wx1):
        box(b, x, x + t, Y0 - 0.03, Y0 + 0.02, wz0 - 0.05, wz1 + t, "sari_trim", r=0.012)
    box(b, wx0 - 0.06, wx1 + 0.06, 38.60, Y0 + WALL + 0.12, wz0, wz0 + 0.055, "prop_wood", r=0.012)
    for x in (wx0 + 0.25, wx1 - 0.25):
        b.prism((x, Y0 + 0.01, wz0 - 0.28), (x, 38.66, wz0 + 0.005), 0.018, 0.018, "prop_steel_dark", r=0.006)
    # The grille: a flat-bar frame, chunky square bars, and the pass-through gap over the ledge.
    gz0, gz1 = wz0 + 0.05, wz1
    gap = (13.85, 14.45, gz0 + 0.30)
    box(b, wx0, wx1, ym - 0.02, ym + 0.02, gz1 - 0.05, gz1 + 0.005, "sari_grille", r=0.008)
    box(b, wx0, wx1, ym - 0.02, ym + 0.02, gz0 - 0.005, gz0 + 0.04, "sari_grille", r=0.008)
    for x in (wx0 + 0.02, wx1 - 0.02):
        box(b, x - 0.025, x + 0.025, ym - 0.02, ym + 0.02, gz0, gz1, "sari_grille", r=0.008)
    for zc in (1.72, 2.06):
        box(b, wx0 + 0.01, wx1 - 0.01, ym - 0.012, ym + 0.012, zc - 0.018, zc + 0.018, "sari_grille", r=0.006)
    box(b, gap[0] - 0.03, gap[1] + 0.03, ym - 0.014, ym + 0.014, gap[2] - 0.02, gap[2] + 0.02, "sari_grille", r=0.006)
    n = 17
    for k in range(1, n):
        x = wx0 + (wx1 - wx0) * k / n
        z0 = gap[2] if gap[0] - 0.02 < x < gap[1] + 0.02 else gz0 + 0.02
        box(b, x - 0.013, x + 0.013, ym - 0.013, ym + 0.013, z0, gz1 - 0.03, "sari_grille", r=0.005)
    # Behind the bars: candy jars on the inner ledge and sachet strips hung from the top bar. The
    # BAWAL ANG UTANG card is taped to the street side of the grille, over the gap.
    rng = random.Random(7)
    for k, x in enumerate((12.95, 13.3, 13.65, 14.75, 15.1)):
        base = Vector((x, Y0 + WALL + 0.05, wz0 + 0.05))
        b.lathe(base, [(0.06, 0.0), (0.072, 0.03), (0.072, 0.18), (0.052, 0.21), (0.045, 0.21)],
                "prop_glass", sides=14)
        b.lathe(base + Vector((0, 0, 0.012)), [(0.062, 0.0), (0.064, 0.15), (0.0, 0.16)],
                ("prop_candy_a", "prop_candy_b", "prop_candy_c", "prop_candy_d", "prop_candy_b")[k], sides=14)
        b.lathe(base + Vector((0, 0, 0.205)), [(0.05, 0.0), (0.054, 0.04), (0.01, 0.046)],
                ("prop_plastic_red", "prop_plastic_green", "prop_plastic_yellow")[k % 3], sides=14)
    for k, x in enumerate((12.93, 13.09, 13.25, 15.05, 15.21, 15.37)):
        h = 0.6 - 0.08 * (k % 2)
        c = Vector((x, ym + 0.035, gz1 - 0.04 - h / 2))
        b.panel(facing("-y", c, tilt_deg=rng.uniform(-3, 3)), 0.12, h, 0.006, "prop_plastic_white",
                "prop_sachet_strip", (0.0, 1 - h / 0.6, 1.0, 1.0), r=0.01)
    b.panel(facing("-y", Vector((14.15, ym - 0.022, 1.89))), 0.40, 0.26, 0.004, "prop_plastic_white",
            "sari_sign_utang", r=0.005)
    # Shelves on the back wall: the drawn stock panel, three planks on its lines, and the lamp.
    b.panel(facing("-y", Vector((14.105, 40.595, FLOOR + 1.0))), 3.29, 2.0, 0.02, "prop_wood", "sari_stock", r=0.005)
    for k in (1, 2, 3):
        z = FLOOR + 0.5 * k
        box(b, X0 + WALL - 0.005, 15.755, 40.22, 40.59, z - 0.035, z - 0.002, "prop_wood", r=0.01)
    box(b, 13.0, 15.3, 39.85, 39.93, GF_TOP - 0.08, GF_TOP + 0.005, "sari_trim", r=0.01)
    b.tube([Vector((13.1, 39.89, GF_TOP - 0.11)), Vector((15.2, 39.89, GF_TOP - 0.11))], 0.02, "prop_lamp", sides=10)
    # The signboard across the floor band, on two battens.
    sc = Vector((14.4, 38.37, 3.575))
    for x in (13.2, 15.6):
        box(b, x - 0.05, x + 0.05, 38.385, 38.415, 3.3, 3.85, "prop_wood_dark", r=0.01)
    b.panel(facing("-y", sc), 4.2, 0.75, 0.04, "prop_wood", "sari_sign_main", r=0.02, jitter=0.01, seed=11)
    # The awning: striped canvas on two steel arms from the floor band to a front rail.
    ax0, ax1, y_in, y_out, z_in, z_out = 12.45, 15.95, 38.40, 37.45, 3.17, 2.78
    rows, cols = 5, 15
    grid = []
    for i in range(rows):
        f = i / (rows - 1)
        y = y_in + (y_out - y_in) * f
        row = []
        for j in range(cols):
            g = j / (cols - 1)
            sag = 0.035 * math.sin(math.pi * g) * math.sin(math.pi * f)
            row.append(Vector((ax0 + (ax1 - ax0) * g, y, z_in + (z_out - z_in) * f - sag)))
        grid.append(row)
    b.sheet(grid, 0.012, lambda i, j: "prop_canvas_green" if (j // 2) % 2 == 0 else "prop_canvas_cream")
    for x in (ax0 + 0.08, ax1 - 0.08):
        b.tube([Vector((x, 38.42, z_in - 0.03)), Vector((x, y_out + 0.02, z_out - 0.03))], 0.02, "prop_steel_dark")
    b.tube([Vector((ax0 - 0.02, y_out + 0.01, z_out - 0.035)), Vector((ax1 + 0.02, y_out + 0.01, z_out - 0.035))],
           0.022, "prop_steel_dark")
    # Snack strips hanging from the front rail at both corners.
    for k, x in enumerate((12.62, 12.84, 15.56, 15.78)):
        c = Vector((x, y_out + 0.02, z_out - 0.06 - 0.45))
        b.panel(facing("-y", c, tilt_deg=rng.uniform(-4, 4)), 0.2, 0.9, 0.012, "prop_plastic_white", "sari_snacks",
                r=0.02)
    # The house door, set into its opening, with a trim frame.
    b.panel(facing("-y", Vector((sum(DOOR) / 2, Y0 + 0.06, APRON + 1.04))), DOOR[1] - DOOR[0] + 0.02, 2.08, 0.05,
            "prop_wood_dark", "sari_door", r=0.01)
    for x in (DOOR[0] - 0.07, DOOR[1] - 0.01):
        box(b, x, x + 0.08, Y0 - 0.03, Y0 + 0.02, APRON - 0.01, 2.48, "sari_trim", r=0.012)
    box(b, DOOR[0] - 0.07, DOOR[1] + 0.07, Y0 - 0.03, Y0 + 0.02, 2.40, 2.48, "sari_trim", r=0.012)
    # A small grilled window on the Taft wall of the ground floor, curtained.
    wy0, wy1, wz0, wz1 = 43.2, 44.5, 1.30, 2.30
    b.panel(facing("-x", Vector((X0 + 0.02, (wy0 + wy1) / 2, (wz0 + wz1) / 2))), wy1 - wy0, wz1 - wz0, 0.04,
            "sari_trim", "sari_jalousie", r=0.01)
    box(b, X0 - 0.04, X0 + 0.02, wy0 - 0.08, wy1 + 0.08, wz1, wz1 + 0.08, "sari_trim", r=0.012)
    box(b, X0 - 0.1, X0 + 0.02, wy0 - 0.1, wy1 + 0.1, wz0 - 0.08, wz0, "sari_trim", r=0.012)
    for y in (wy0 - 0.04, wy1 + 0.04):
        box(b, X0 - 0.04, X0 + 0.02, y - 0.04, y + 0.04, wz0, wz1, "sari_trim", r=0.012)
    gx = X0 - 0.07
    box(b, gx - 0.015, gx + 0.015, wy0 - 0.02, wy1 + 0.02, wz1 - 0.04, wz1 - 0.01, "sari_grille", r=0.006)
    box(b, gx - 0.015, gx + 0.015, wy0 - 0.02, wy1 + 0.02, wz0 + 0.01, wz0 + 0.04, "sari_grille", r=0.006)
    for k in range(1, 7):
        y = wy0 + (wy1 - wy0) * k / 7
        box(b, gx - 0.012, gx + 0.012, y - 0.012, y + 0.012, wz0 + 0.02, wz1 - 0.02, "sari_grille", r=0.005)
    for y in (wy0 - 0.02, wy1 + 0.02):
        for z in (wz0 + 0.1, wz1 - 0.1):
            b.prism((X0 + 0.01, y, z), (gx, y, z), 0.014, 0.014, "sari_grille", r=0.005)
    # The tin sign on the Taft wall.
    b.panel(facing("-x", Vector((X0 - 0.003, 42.3, 1.85))), 0.9, 0.6, 0.012, "sari_trim", "sari_sign_tin", r=0.01)
    b.finish(col, origin=ORIGIN, bevel=0.006)


def furniture(col):
    b = PBuf("sarisari_furniture", top=APRON + 0.45)
    # A plank bench against the front wall (seat 0.44 above the apron).
    bx0, bx1 = 15.97, 17.28
    box(b, bx0, bx1, 38.50, 38.84, APRON + 0.40, APRON + 0.445, "prop_wood", r=0.012)
    for x in (bx0 + 0.12, bx1 - 0.12):
        for y in (38.56, 38.78):
            b.prism((x, y, APRON - 0.01), (x, y, APRON + 0.405), 0.03, 0.028, "prop_wood_dark", r=0.01)
        box(b, x - 0.025, x + 0.025, 38.55, 38.79, APRON + 0.12, APRON + 0.16, "prop_wood_dark", r=0.008)
    # The red cooler with its white lid, a handle, and the MAY YELO card leaning on the wall.
    cx, cy = 17.66, 38.66
    b.rbox((cx, cy, APRON + 0.19), (0.54, 0.38, 0.40), "prop_plastic_red", r=0.05)
    b.rbox((cx, cy, APRON + 0.41), (0.57, 0.41, 0.06), "prop_plastic_white", r=0.03)
    b.rbox((cx, cy - 0.2, APRON + 0.33), (0.2, 0.04, 0.035), "prop_plastic_white", r=0.015)
    b.panel(facing("-y", Vector((cx, 38.80, APRON + 0.6)), tilt_deg=14), 0.45, 0.3, 0.008, "prop_wood", "sari_sign_yelo",
            r=0.01, jitter=0.01, seed=3)
    # Two crates of empty bottles on the Taft side of the store.
    for k, (y, z) in enumerate(((39.45, APRON), (39.45, APRON + 0.29), (39.95, APRON))):
        crate(b, Vector((12.04, y, z)), (0.3, 0.45, 0.28), ("prop_plastic_yellow", "prop_plastic_red")[k % 2])
        if k != 0:
            for i in range(2):
                for j in range(3):
                    base = Vector((12.04 - 0.065 + 0.13 * i, y - 0.14 + 0.14 * j, z + 0.025))
                    b.lathe(base, [(0.03, 0.0), (0.032, 0.18), (0.012, 0.25), (0.013, 0.3), (0.0, 0.301)],
                            "sari_bottle", sides=10)
    b.finish(col, origin=ORIGIN, bevel=0.008)


def gate(col):
    b = PBuf("sarisari_gate", top=2.3)
    for x0, x1 in ((X1 - 0.02, 19.7), (21.92, 22.75)):
        box(b, x0, x1, Y0, Y0 + 0.16, LOT - 0.05, 2.30, "sari_wall_grey", r=0.02)
        box(b, x0 - 0.02, x1 + 0.02, Y0 - 0.02, Y0 + 0.18, 2.29, 2.36, "sari_wall_grey", r=0.02)
    box(b, 19.7, 21.92, Y0 + 0.03, Y0 + 0.13, 2.08, 2.14, "prop_steel_dark", r=0.01)
    b.panel(facing("-y", Vector((20.81, Y0 + 0.08, LOT + 1.08))), 2.2, 1.8, 0.04, "prop_steel_red", "sari_gate", r=0.01)
    b.finish(col, origin=ORIGIN, bevel=0.012)


def service(col):
    """The electric meter on the Taft wall and the service drop from the street pole."""
    b = PBuf("sarisari_service", top=3.0)
    mx, my, mz = X0 - 0.06, 40.25, 2.25
    b.rbox((mx, my, mz), (0.14, 0.28, 0.4), "prop_steel_dark", r=0.02)
    b.blob(Vector((mx - 0.07, my, mz + 0.06)), (0.02, 0.085, 0.085), "sari_meter_face")
    drop = catmull([Vector((10.8, 38.98, 7.7)), Vector((11.45, 39.6, 6.9)), Vector((12.08, 40.25, 5.5))], per=8)
    b.tube(drop, 0.012, "prop_rubber", sides=6)
    b.tube([Vector((12.10, 40.25, 5.52)), Vector((12.10, 40.25, SLAB_TOP + 0.05)), Vector((12.03, 40.25, 3.5)),
            Vector((12.03, 40.25, 3.35)), Vector((X0 - 0.03, my, 3.2)), Vector((X0 - 0.03, my, mz + 0.2))],
           0.022, "prop_steel_dark", sides=8)
    b.blob(Vector((12.1, 40.25, 5.58)), (0.04, 0.04, 0.05), "prop_steel_dark")
    b.finish(col, origin=ORIGIN, bevel=0.0)


# ------------------------------------------------------------------ review

def stand_ins(parent):
    col = collection("review stand-ins", parent)

    def slab(name, x0, x1, y0, y1, top, colour):
        me = bpy.data.meshes.new(name)
        z0 = top - 0.4
        vs = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, top), (x1, y0, top), (x1, y1, top),
              (x0, y1, top)]
        fs = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
        me.from_pydata(vs, [], fs)
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*colour, 1)
        if mat.node_tree is None:
            mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*colour, 1)
        me.materials.append(mat)
        o = bpy.data.objects.new(name, me)
        col.objects.link(o)

    slab("stand-in road", -10, 60, 0, 90, 0.0, (0.32, 0.32, 0.33))
    slab("stand-in faura pavement", 7, 40, 35.0, 38.3, 0.212, (0.62, 0.60, 0.56))
    slab("stand-in taft pavement", 7, 11.8, 38.3, 70, 0.212, (0.62, 0.60, 0.56))
    slab("stand-in lot", 11.8, 40, 38.3, 70, 0.24, (0.55, 0.52, 0.46))
    return col


def lighting():
    # The city's light (tools/author_ilalim_city.py): late afternoon, low from the west.
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.60, 0.74, 0.92, 1)
    bg.inputs["Strength"].default_value = 0.7
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 4.6, math.radians(2.5), (1.0, 0.86, 0.68)
    sun.rotation_euler = Vector((0.86, 0.22, -0.46)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass


def preview(version, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 1000
    scene.collection.objects.link(cam)
    scene.camera = cam
    shots = [
        ("front", (15.2, 30.0, 1.6), (15.0, 39.5, 2.6), 24),
        ("counter_close", (14.1, 36.0, 1.6), (14.15, 39.2, 1.75), 26),
        ("corner", (6.5, 32.5, 1.7), (15.0, 41.0, 3.0), 22),
        ("bench_close", (18.6, 36.3, 1.25), (16.9, 38.7, 0.7), 24),
        ("taft_side", (8.5, 47.0, 1.7), (12.2, 41.5, 2.8), 22),
        ("aerial", (3.0, 25.0, 15.0), (16.0, 42.0, 2.0), 22),
    ]
    for name, pos, tgt, lens in shots:
        if only and name not in only:
            continue
        path = PREVIEWS / f"sari_{name}_v{version}.png"
        if path.exists():
            print("[ilalim-sari] exists, skipped", path)
            continue
        pos, tgt = Vector(pos), Vector(tgt)
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[ilalim-sari] preview", path)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    placed = collection("sarisari (placed)")
    walls(placed)
    upper(placed)
    roof(placed)
    front(placed)
    furniture(placed)
    gate(placed)
    service(placed)
    stand_ins(collection("review"))
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "sarisari.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True, relative_remap=True)
    backup = SOURCE / "sarisari.blend1"
    if backup.exists():
        backup.unlink()
    faces = sum(len(o.data.polygons) for o in placed.all_objects if o.type == "MESH")
    print(f"[ilalim-sari] saved {out}; {len(list(placed.all_objects))} objects, {faces} faces before bevel")
    if version:
        preview(version, only)


if __name__ == "__main__":
    main()
