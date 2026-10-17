"""PAYO land kit: the review scenes for the pit wall, the cliff and the ground (called by terrace_review2.py)."""
import math

import bpy
from mathutils import Vector

import terrace_review as R
import terrace_review2 as R2

Q = -math.pi / 2


def box(name, lo, hi, colour):
    x0, y0, z0 = lo; x1, y1, z1 = hi
    m = R.flat(name, colour, 0.0)
    for c in ([(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], [(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)],
              [(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], [(x0, y1, z0), (x0, y0, z0), (x0, y0, z1), (x0, y1, z1)],
              [(x1, y1, z0), (x0, y1, z0), (x0, y1, z1), (x1, y1, z1)]):
        R.plane(name, c, m)


def pit():
    sc, cam = R.stage()
    made = {}

    def put(name, at, turn=0.0):
        if name not in made:
            made[name] = R2.load(name, at, turn, ink=0.02)
        else:
            R.clone(made[name], at, turn)

    grid = [["pit_wall", "pit_wall_niche", "pit_wall_spout", "pit_wall"],
            ["pit_wall_niche", "pit_wall", "pit_wall", "pit_wall_spout"],
            ["pit_wall", "pit_wall", "pit_wall_niche", "pit_wall"],
            ["pit_wall", "pit_wall_spout", "pit_wall", "pit_wall"]]
    for row in range(4):
        for col in range(4):
            put(grid[row][col], (col * 12.0, 0, row * 12.0))
        put("pit_corner_in", (48.0, 0, row * 12.0))
        put("pit_wall" if row != 1 else "pit_wall_niche", (48.0, 0, row * 12.0), Q)
        put("pit_wall" if row < 3 else "pit_wall_broken", (48.0, -12.0, row * 12.0), Q)
    for col in range(4):
        put("pit_kerb", (col * 12.0, 0, 48.0))
    put("pit_kerb", (48.0, 0, 48.0), Q)
    R.plane("top", [(-20, 0.7, 48.3), (70, 0.7, 48.3), (70, 40, 48.3), (-20, 40, 48.3)], R.flat("top", R2.GRASS, 0.0))
    R.plane("topb", [(48.7, -40, 48.3), (70, -40, 48.3), (70, 0.7, 48.3), (48.7, 0.7, 48.3)], R.flat("topb", R2.GRASS, 0.0))
    R.plane("floor", [(-20, -80, 0), (48, -80, 0), (48, 0, 0), (-20, 0, 0)], R.flat("pitfloor", (0.05, 0.07, 0.08), 0.0))
    box("blockA", (18, -14, 0), (26, -8, 18.0), (0.20, 0.20, 0.21))
    R.figure((24.8, -8.6, 18.0), 2.6)
    R.figure((27.0, -0.32, 4.8), 0.3)
    R.figure((20.0, 0.3, 48.42), 0.3)
    R.look(cam, (21.0, -9.2, 19.7), (30.0, 0, 19.0), 24); R.shoot("pit_eye")
    R.look(cam, (21.0, -9.2, 19.7), (36.0, 0, 38.0), 24); R.shoot("pit_eye_up")
    R.look(cam, (21.0, -9.2, 19.7), (36.0, 0, 3.0), 24); R.shoot("pit_eye_down")
    R.look(cam, (14.0, -62.0, 22.0), (27.0, 0, 24.0), 26); R.shoot("pit_wide")
    R.look(cam, (20.0, -34.0, 82.0), (28.0, -4.0, 28.0), 30); R.shoot("pit_above")
    R.look(cam, (24.0, -5.5, 6.2), (26.6, 0, 6.6), 28); R.shoot("pit_close")


def pit_parts():
    sc, cam = R.stage()
    R2.load("pit_wall", (-12, 0, 0), 0.0, ink=0.02)
    R2.load("pit_corner_out", (0, 0, 0), 0.0, ink=0.02)
    R2.load("pit_wall_spout", (0, 0, 0), -Q, ink=0.02)               # wall B of a jutting corner: it leaves toward +y, facing +x
    R.plane("floor", [(-80, -80, 0), (80, -80, 0), (80, 80, 0), (-80, 80, 0)], R.flat("pitfloor", (0.07, 0.15, 0.06), 0.0))
    R.figure((1.5, -1.5, 0), 0.5)
    R.look(cam, (15.0, -15.0, 5.0), (-1.0, 3.0, 5.8), 24); R.shoot("pit_corner_out")
    sc, cam = R.stage()
    R2.load("pit_wall", (-12, 0, 0), 0.0, ink=0.02)
    R2.load("pit_wall_broken", (0, 0, 0), 0.0, ink=0.02)
    R2.load("pit_kerb", (-12, 0, 12), 0.0, ink=0.02)
    R.plane("floor", [(-80, -80, 0), (80, -80, 0), (80, 80, 0), (-80, 80, 0)], R.flat("pitfloor", (0.07, 0.15, 0.06), 0.0))
    R.figure((6.5, -1.2, 0), 0.5)
    R.look(cam, (4.0, -17.0, 4.0), (0.5, 0, 5.8), 24); R.shoot("pit_broken")
    R.look(cam, (-6.0, -5.0, 14.5), (-6.0, 0.3, 12.2), 24); R.shoot("pit_kerb")


def cliff():
    sc, cam = R.stage()
    made = {}
    for row in range(2):
        for col in range(2):
            at = (col * 12.0, 0, row * 12.0)
            if "c" not in made:
                made["c"] = R2.load("cliff_wall", at, 0.0, ink=0.03)
            else:
                R.clone(made["c"], at)
        at = (24.0, 0, row * 12.0)
        if "k" not in made:
            made["k"] = R2.load("cliff_corner", at, 0.0, ink=0.03)
        else:
            R.clone(made["k"], at)
        at = (24.0, 12.0, row * 12.0)                                    # the cliff going on round the corner, facing +x
        R.clone(made["c"], at, Q)
    R2.load("fall_medium", (9.0, -1.0, 0.0), 0.0, ink=0.02)
    R2.load("fall_small", (18.5, -1.3, 0.0), 0.0, ink=0.012)
    R.plane("floor", [(-80, -80, 0), (80, -80, 0), (80, 80, 0), (-80, 80, 0)], R.flat("floor", (0.07, 0.15, 0.06), 0.0))
    R.plane("top", [(-10, 0.8, 23.2), (24.5, 0.8, 23.2), (24.5, 40, 23.2), (-10, 40, 23.2)], R.flat("top", R2.GRASS, 0.0))
    R.figure((14.5, -4.0, 0), 0.4)
    R.look(cam, (4.0, -30.0, 1.7), (13.0, 0, 10.5), 24); R.shoot("cliff_front")
    R.look(cam, (16.0, -9.0, 1.7), (11.0, 0, 5.0), 24); R.shoot("cliff_close")
    R.look(cam, (44.0, -30.0, 14.0), (20.0, 2.0, 10.0), 26); R.shoot("cliff_corner")
    R.look(cam, (12.0, -26.0, 44.0), (13.0, 0, 10.0), 26); R.shoot("cliff_above")


def ground():
    sc, cam = R.stage()
    R2.load("ground_grass_tile", (0, 0, 0), 0.0, ink=0.012)
    R2.load("ground_forest_tile", (12, 0, 0), 0.0, ink=0.012)
    g = R2.load("ground_grass_tile", (0, 12, 0), 0.0, ink=0.012)
    R2.load("ground_forest_tile", (12, 12, 0), 0.0, ink=0.012)
    p = R2.load("path_stones", (6.0, 0.0, 0.04), 0.0, ink=0.012)
    R.clone(p, (6.0, 6.0, 0.04))
    R.clone(p, (6.0, 12.0, 0.04), 0.25)
    R.plane("floor", [(-80, -80, -0.02), (80, -80, -0.02), (80, 80, -0.02), (-80, 80, -0.02)], R.flat("floor", (0.07, 0.15, 0.06), 0.0))
    R.figure((7.4, 5.0, 0), 0.5)
    R.look(cam, (5.2, -3.5, 1.7), (7.5, 9.0, 0.3), 24); R.shoot("ground_eye")
    R.look(cam, (15.0, -3.0, 1.7), (17.0, 8.0, 0.2), 24); R.shoot("ground_forest_eye")
    R.look(cam, (12.0, -6.0, 22.0), (12.0, 10.0, 0), 26); R.shoot("ground_above")
    R.look(cam, (6.0, 2.2, 1.1), (6.2, 4.6, 0.1), 30); R.shoot("ground_path_close")
