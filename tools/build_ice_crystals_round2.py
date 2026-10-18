"""ICE CRYSTALS, THE SECOND SET, KEPT ASIDE. Nothing in the game loads what this writes.

The owner looked at this set beside the first and said of the first: "this old one was better". So the first set
is the game's (`tools/build_ice_crystals.py`, `Resources/Models/IceCrystals`, `Shaders/IceCrystal`) and this one is
kept whole, under its own names, in case any of it is wanted: `Resources/Models/IceCrystalsRound2/ice2_*`,
`Shaders/IceCrystalRound2`, bakes in `Logs/ice-crystals/bake_round2`, sheets named `round2_*`. What it tried, against
the first: many more typed faces a crystal, a flood of light at the foot, three separate hues, and edge lines
drawn in the atlas. `IceCrystalFilm` films it when the request line is `<tag> 2`.

What follows is its own description, as it was written.

ICE CRYSTALS, sculpted and baked: three single crystals and one cluster, for effects that need real ice.

  py -3 tools/build_ice_crystals_round2.py forms  v1          the carved forms only, in clay, to look at the cuts
  py -3 tools/build_ice_crystals_round2.py build  v1          carve, sculpt, bake, paint, write the models, then review
  py -3 tools/build_ice_crystals_round2.py paint  v2          repaint the maps from the bakes already on disk, then review
  py -3 tools/build_ice_crystals_round2.py review v2          only the turnaround sheet

WHY IT EXISTS. The owner read an article on making a stylised crystal (sculpt a high detail crystal, bake it down to
a low one, paint the maps from the bakes), asked why our ice was not made that way, and said: "try making an ice
crystals". The ice this game had (Cheska's) is flat typed facets with a painted atlas, and it reads as uniformly pale
frosted plastic: nothing in it knows where the crystal is thin and where it is deep. A bake does know. So this builds
each crystal twice:

  * the FORM: a stretched solid carved by TYPED plane cuts, one line a cut, chosen by hand for each crystal: the
    primary faces first, then small facets cut off typed corners, then secondary cuts along typed edges, each with its
    own depth, lean and taper, then chips bitten out of typed edges. A first pass had four to six big faces a crystal
    and read as a box with a bevelled top; a cut crystal is twenty planes and more, and the bright net of lines that
    makes it look like ice is nothing but the edges between them. Its faces are then cut finer and nudged, so an edge
    does not run dead straight. This is the low model the game draws.
  * the SCULPT: the same form remeshed fine, shallow chisel marks and a little grain laid on it and partly flattened
    again on the main planes (so a face stays a face), and typed cracks. Nobody draws this one. It is baked onto the
    form: a tangent normal map, an edge mask at two widths, ambient occlusion, the cracks, and THICKNESS (how much
    crystal lies behind each point of the surface), which is what lets the shader light the thin parts through.

Then the system's Python paints three maps from the bakes (see `paint`) and Blender renders a turnaround with a
material that imitates `Resources/Shaders/IceCrystalRound2.shader`. It IMITATES it: say "seen in Blender only" of that sheet.

These are our own crystals. The article gave the method; no silhouette here is taken from its crystal.

Everything is typed and seeded, so two runs carve the same forms (the same triangle and vertex counts). They do NOT
write the same bytes: measured on 2026-10-10, two runs of one builder lay the atlas out a little differently (most on
the cluster) and bake with slightly different noise. In the game the two are the same crystal to the eye. So keep the
model and its three maps from ONE run together, never a map from another run. Metres, z up in here, y up in the .glb.

Maps, 1024 square, beside each model in Assets/TumbangPreso/Resources/Models/IceCrystalsRound2:
  <name>_albedo.png   RGB the paint (sRGB), A the sheen's break-up
  <name>_normal.png   RGB tangent normal (y up), linear
  <name>_mask.png     R glow-through, G the soft wide edge, B the thin sharp edge and the cracks, A thickness (1 is
                      deep), linear
"""
import json
import math
import os
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
OUT = ROOT / "Assets" / "TumbangPreso" / "Resources" / "Models" / "IceCrystalsRound2"
LOGS = ROOT / "Logs" / "ice-crystals"
WORK = LOGS / "bake_round2"
BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe"
SIZE = 1024

# ---------------------------------------------------------------------------------------------------------------------
# THE CRYSTALS. A cut is one of three things, and they are made in the order typed:
#   (name, azimuth, elevation, distance, about x, about y, about z)   a PLANE: its outward normal points along that
#       azimuth and elevation (degrees) and it stands `distance` out from the `about` point; what is beyond it goes.
#   (name, "corner", cut, cut, cut, depth)        a small facet sliced off the point where those three faces meet.
#   (name, "edge", cut, cut, depth, lean, taper)  a secondary cut along the edge those two faces share: `depth` in
#       from the edge, leaned `lean` degrees toward one face, and turned `taper` degrees so it is wider at one end.
# A soft edge is (cut, cut, width, segments): that edge is rounded. A chip is (cut, cut, how far along the edge those
# two faces share, size, stretch along the edge): a faceted bite out of that edge, as ice chips, not a pocket in a
# face. A crack is a typed run of points near the surface: straight runs with sharp kinks, the way ice cracks.
# `finer` is how many times each edge of the finished form is cut for the low model.
# ---------------------------------------------------------------------------------------------------------------------

SPIKE = dict(
    cuts=[
        # the long sides, unevenly spaced round it and each leaning in by its own amount
        ("s1", 8, 4, 0.200, 0, 0, 0.60),
        ("s2", 62, 7, 0.172, 0, 0, 0.60),
        ("s3", 118, 3, 0.214, 0, 0, 0.60),
        ("s4", 171, 9, 0.160, 0, 0, 0.60),
        ("s5", 233, 5, 0.205, 0, 0, 0.60),
        ("s6", 296, 6, 0.178, 0, 0, 0.60),
        # one long slice down a side and one across the belly: the two big planes that stop it being a pencil
        ("long", 92, 13, 0.150, 0, 0, 0.95),
        ("belly", 318, -11, 0.186, 0, 0, 0.50),
        # the foot
        ("f1", 35, -32, 0.190, 0, 0, 0.25),
        ("f2", 150, -41, 0.170, 0, 0, 0.25),
        ("f3", 265, -28, 0.200, 0, 0, 0.25),
        ("f4", 330, -52, 0.158, 0, 0, 0.25),
        ("under", 200, -80, 0.235, 0, 0, 0.25),
        # the shoulders, about a point pushed to one side so the tip is not over the middle
        ("u1", 22, 11, 0.150, 0.035, -0.020, 1.02),
        ("u2", 141, 24, 0.120, 0.035, -0.020, 1.20),
        ("u3", 256, 15, 0.134, 0.035, -0.020, 1.20),
        # the tip, chopped twice
        ("t1", 84, 47, 0.050, 0.060, -0.030, 1.50),
        ("t2", 312, 38, 0.046, 0.060, -0.030, 1.52),
        # small facets where three faces meet
        ("c1", "corner", "s1", "s2", "u1", 0.022),
        ("c2", "corner", "s4", "s5", "u3", 0.026),
        ("c3", "corner", "long", "s4", "u2", 0.018),
        ("c5", "corner", "f2", "s3", "s4", 0.024),
        ("c6", "corner", "belly", "s1", "s6", 0.020),
        ("c7", "corner", "f1", "s1", "s2", 0.018),
        ("c8", "corner", "t1", "u1", "u2", 0.012),
        ("c9", "corner", "f3", "s5", "s6", 0.022),
        # secondary cuts along the edges, no two alike
        ("e1", "edge", "s1", "s2", 0.016, 8, 1.6),
        ("e2", "edge", "s4", "s5", 0.020, -10, -1.2),
        ("e3", "edge", "s5", "s6", 0.014, 6, 1.8),
        ("e4", "edge", "long", "s2", 0.012, -12, 1.4),
        ("e5", "edge", "s3", "s4", 0.018, 10, -2.0),
        ("e6", "edge", "s6", "s1", 0.015, -6, 2.2),
        ("e7", "edge", "u3", "s6", 0.012, 8, -2.0),
        ("e9", "edge", "u2", "long", 0.010, 5, 2.5),
        ("e10", "edge", "s1", "belly", 0.014, 10, -2.5),
        ("e11", "edge", "f2", "s4", 0.014, -6, 3.0),
        ("e12", "edge", "f3", "s5", 0.012, 8, -3.0),
        ("e14", "edge", "f4", "f1", 0.012, 0, 3.0),
        ("e15", "edge", "s4", "u2", 0.010, 6, -2.5),
    ],
    soft=[("e2", "s4", 0.010, 4), ("e3", "s5", 0.008, 4)],
    # two real missing chunks, seen in the outline, and three small bites
    chips=[("e2", "s4", 0.62, 0.085, 1.5), ("e1", "s1", 0.40, 0.070, 1.4), ("e3", "s6", 0.30, 0.050, 1.5),
           ("e5", "s3", 0.55, 0.035, 1.3), ("e6", "s1", 0.70, 0.030, 1.2)],
    cracks=[[(0.19, 0.07, 1.00), (0.20, 0.02, 0.86), (0.17, 0.06, 0.72), (0.20, 0.00, 0.55)],
            [(0.20, 0.02, 0.86), (0.19, -0.07, 0.80), (0.16, -0.12, 0.70)],
            [(-0.14, -0.14, 0.95), (-0.16, -0.10, 0.78), (-0.12, -0.16, 0.66)],
            [(-0.16, -0.10, 0.78), (-0.19, -0.03, 0.72)],
            [(0.02, -0.20, 0.45), (0.07, -0.19, 0.33), (0.03, -0.20, 0.22)]],
    voxel=0.0034, finer=1,
)

SHARD = dict(
    cuts=[
        # a broad blade: wide one way, thin the other, eight uneven sides
        ("a", 0, 4, 0.250, 0, 0, 0.45),
        ("b", 48, 7, 0.200, 0, 0, 0.45),
        ("c", 92, 3, 0.130, 0, 0, 0.45),
        ("d", 138, 8, 0.190, 0, 0, 0.45),
        ("e", 184, 5, 0.240, 0, 0, 0.45),
        ("f", 226, 2, 0.185, 0, 0, 0.45),
        ("g", 272, 6, 0.125, 0, 0, 0.45),
        ("h", 318, 9, 0.195, 0, 0, 0.45),
        # one big slice across a face
        ("slash", 340, 16, 0.205, 0, 0, 0.62),
        # the foot
        ("f1", 20, -30, 0.200, 0, 0, 0.22),
        ("f2", 110, -40, 0.120, 0, 0, 0.22),
        ("f3", 200, -26, 0.210, 0, 0, 0.22),
        ("f4", 290, -44, 0.115, 0, 0, 0.22),
        ("under", 150, -82, 0.150, 0, 0, 0.22),
        # the shoulders, off to one side
        ("u1", 12, 28, 0.150, 0.040, 0, 0.78),
        ("u2", 100, 20, 0.105, 0.040, 0, 0.78),
        ("u3", 190, 36, 0.130, 0.040, 0, 0.78),
        ("u4", 280, 24, 0.100, 0.040, 0, 0.78),
        # the tip
        ("t1", 60, 55, 0.035, 0.080, 0, 0.96),
        ("t2", 230, 48, 0.040, 0.080, 0, 0.94),
        ("c1", "corner", "b", "c", "u2", 0.026),
        ("c2", "corner", "d", "e", "u3", 0.024),
        ("c3", "corner", "g", "h", "u4", 0.028),
        ("c4", "corner", "a", "b", "slash", 0.020),
        ("c5", "corner", "c", "d", "f2", 0.022),
        ("c6", "corner", "f", "g", "u3", 0.018),
        ("c7", "corner", "f4", "g", "h", 0.022),
        ("c8", "corner", "a", "b", "f1", 0.018),
        ("c9", "corner", "d", "e", "f3", 0.016),
        ("e1", "edge", "b", "c", 0.018, 10, 1.8),
        ("e2", "edge", "g", "f", 0.016, -8, -2.0),
        ("e3", "edge", "h", "g", 0.020, 6, 1.6),
        ("e4", "edge", "d", "c", 0.014, -10, 2.2),
        ("e5", "edge", "d", "e", 0.016, 8, -2.4),
        ("e6", "edge", "h", "a", 0.014, -6, 2.6),
        ("e7", "edge", "a", "b", 0.012, 8, -3.0),
        ("e8", "edge", "f", "e", 0.012, -8, 2.5),
        ("e9", "edge", "u2", "u3", 0.012, 6, 3.0),
        ("e10", "edge", "h", "slash", 0.012, -8, -2.5),
        ("e11", "edge", "f4", "g", 0.012, 6, 3.0),
        ("e12", "edge", "f2", "c", 0.012, -6, -3.0),
        ("e13", "edge", "u2", "c", 0.010, 8, 3.0),
    ],
    soft=[("e1", "c", 0.008, 4), ("e3", "g", 0.010, 4)],
    chips=[("e1", "b", 0.60, 0.085, 1.5), ("e3", "h", 0.35, 0.075, 1.4), ("e5", "d", 0.50, 0.045, 1.5),
           ("e2", "f", 0.70, 0.035, 1.3), ("e9", "u2", 0.50, 0.030, 1.2)],
    cracks=[[(0.10, 0.13, 0.80), (0.06, 0.14, 0.62), (0.11, 0.13, 0.48), (0.07, 0.14, 0.30)],
            [(0.06, 0.14, 0.62), (-0.04, 0.14, 0.56), (-0.10, 0.13, 0.60)],
            [(-0.08, -0.13, 0.70), (-0.03, -0.13, 0.52), (-0.09, -0.13, 0.38)],
            [(-0.03, -0.13, 0.52), (0.07, -0.12, 0.46)]],
    voxel=0.0034, finer=1,
)

STUB = dict(
    cuts=[
        # a short stout crystal: six sides, a five faced point
        ("s1", 5, 5, 0.165, 0, 0, 0.25),
        ("s2", 68, 9, 0.150, 0, 0, 0.25),
        ("s3", 125, 4, 0.172, 0, 0, 0.25),
        ("s4", 186, 8, 0.155, 0, 0, 0.25),
        ("s5", 244, 6, 0.168, 0, 0, 0.25),
        ("s6", 305, 3, 0.158, 0, 0, 0.25),
        ("slice", 150, 20, 0.150, 0, 0, 0.30),
        ("f1", 40, -42, 0.120, 0, 0, 0.12),
        ("f2", 160, -36, 0.130, 0, 0, 0.12),
        ("f3", 280, -48, 0.110, 0, 0, 0.12),
        ("under", 90, -85, 0.115, 0, 0, 0.12),
        ("p1", 30, 32, 0.105, 0.030, -0.020, 0.40),
        ("p2", 105, 27, 0.115, 0.030, -0.020, 0.40),
        ("p3", 170, 38, 0.095, 0.030, -0.020, 0.40),
        ("p4", 240, 29, 0.110, 0.030, -0.020, 0.40),
        ("p5", 315, 35, 0.100, 0.030, -0.020, 0.40),
        ("c1", "corner", "p1", "s1", "s2", 0.020),
        ("c2", "corner", "p4", "s4", "s5", 0.022),
        ("c3", "corner", "p5", "s1", "s6", 0.018),
        ("c4", "corner", "p2", "s2", "s3", 0.018),
        ("c5", "corner", "f1", "s1", "s2", 0.018),
        ("c6", "corner", "f2", "s4", "s5", 0.020),
        ("c7", "corner", "f3", "s5", "s6", 0.016),
        ("c8", "corner", "p1", "p2", "p3", 0.012),
        ("c9", "corner", "p3", "s4", "slice", 0.016),
        ("e1", "edge", "s1", "s6", 0.016, 8, 3.0),
        ("e2", "edge", "s4", "s5", 0.018, -8, -3.0),
        ("e3", "edge", "s5", "s6", 0.012, 6, 4.0),
        ("e4", "edge", "s3", "s2", 0.014, -10, 3.5),
        ("e5", "edge", "s2", "s1", 0.012, 8, -4.0),
        ("e6", "edge", "p3", "p4", 0.010, 6, 4.0),
        ("e7", "edge", "p3", "p2", 0.010, -6, -4.0),
        ("e8", "edge", "p2", "s2", 0.010, 8, 4.0),
        ("e9", "edge", "p4", "s5", 0.010, -6, 4.0),
        ("e10", "edge", "p5", "p1", 0.010, 6, -4.0),
        ("e11", "edge", "f2", "s4", 0.012, -6, 4.0),
        ("e12", "edge", "f3", "s6", 0.012, 6, -4.0),
        ("e13", "edge", "f1", "s2", 0.010, 0, 4.0),
        ("e14", "edge", "s4", "slice", 0.010, 8, 4.0),
    ],
    soft=[("e1", "s6", 0.008, 4), ("e2", "s4", 0.008, 4)],
    chips=[("e1", "s1", 0.55, 0.070, 1.4), ("e2", "s5", 0.40, 0.060, 1.4), ("e4", "s3", 0.50, 0.035, 1.3),
           ("e6", "p3", 0.50, 0.028, 1.2)],
    cracks=[[(0.16, 0.04, 0.42), (0.165, -0.01, 0.32), (0.15, 0.03, 0.22), (0.16, -0.02, 0.12)],
            [(0.165, -0.01, 0.32), (0.13, -0.10, 0.28)],
            [(-0.12, 0.10, 0.44), (-0.14, 0.07, 0.32), (-0.11, 0.11, 0.24)]],
    voxel=0.0030, finer=1,
)

# Forms only the cluster has.
LEAN = dict(
    cuts=[
        ("s1", 25, 6, 0.150, 0, 0, 0.45),
        ("s2", 100, 3, 0.128, 0, 0, 0.45),
        ("s3", 160, 8, 0.152, 0, 0, 0.45),
        ("s4", 228, 4, 0.136, 0, 0, 0.45),
        ("s5", 300, 9, 0.146, 0, 0, 0.45),
        ("f1", 60, -36, 0.130, 0, 0, 0.15),
        ("f2", 190, -42, 0.120, 0, 0, 0.15),
        ("f3", 310, -33, 0.134, 0, 0, 0.15),
        ("under", 0, -88, 0.150, 0, 0, 0.15),
        ("u1", 55, 20, 0.098, -0.020, 0.025, 0.86),
        ("u2", 180, 27, 0.090, -0.020, 0.025, 0.86),
        ("u3", 292, 18, 0.102, -0.020, 0.025, 0.86),
        ("t1", 130, 52, 0.030, -0.030, 0.035, 1.00),
        ("split", 10, 33, 0.066, -0.020, 0.025, 0.98),
        ("c1", "corner", "s1", "s2", "u1", 0.018),
        ("c2", "corner", "s4", "s5", "u3", 0.018),
        ("c3", "corner", "s3", "s4", "u2", 0.016),
        ("c4", "corner", "f1", "s1", "s2", 0.014),
        ("c5", "corner", "f3", "s4", "s5", 0.014),
        ("e1", "edge", "s2", "s3", 0.014, 8, 1.6),
        ("e2", "edge", "s1", "s5", 0.014, -8, -1.8),
        ("e3", "edge", "s4", "s3", 0.012, 6, 2.0),
        ("e4", "edge", "s4", "s5", 0.012, -8, 2.2),
        ("e5", "edge", "s1", "s2", 0.010, 8, -2.4),
        ("e6", "edge", "u1", "s2", 0.008, 6, 3.0),
        ("e7", "edge", "s5", "u3", 0.008, -6, 3.0),
    ],
    soft=[],
    chips=[("e1", "s2", 0.50, 0.060, 1.5), ("e2", "s5", 0.35, 0.040, 1.4)],
    cracks=[[(0.14, 0.05, 0.78), (0.15, 0.01, 0.62), (0.13, 0.05, 0.48)],
            [(0.15, 0.01, 0.62), (0.12, -0.07, 0.56)]],
    voxel=0.0040, finer=0,
)

BLADE = dict(
    cuts=[
        ("a", 15, 5, 0.125, 0, 0, 0.40),
        ("b", 95, 3, 0.068, 0, 0, 0.40),
        ("c", 165, 8, 0.120, 0, 0, 0.40),
        ("d", 215, 4, 0.110, 0, 0, 0.40),
        ("e", 275, 6, 0.072, 0, 0, 0.40),
        ("f", 330, 3, 0.115, 0, 0, 0.40),
        ("f1", 60, -38, 0.090, 0, 0, 0.12),
        ("f2", 200, -42, 0.085, 0, 0, 0.12),
        ("f3", 310, -35, 0.090, 0, 0, 0.12),
        ("under", 0, -88, 0.120, 0, 0, 0.12),
        ("u1", 40, 30, 0.085, 0, 0, 0.70),
        ("u2", 150, 22, 0.080, 0, 0, 0.70),
        ("u3", 265, 34, 0.060, 0, 0, 0.70),
        ("t1", 330, 50, 0.025, 0, 0, 0.80),
        ("c1", "corner", "a", "b", "u1", 0.014),
        ("c2", "corner", "c", "d", "u3", 0.014),
        ("c4", "corner", "b", "c", "f2", 0.012),
        ("e1", "edge", "a", "f", 0.012, 8, 1.6),
        ("e2", "edge", "c", "d", 0.012, -8, -1.8),
        ("e3", "edge", "c", "b", 0.008, 6, 2.0),
        ("e4", "edge", "f", "e", 0.008, -6, 2.0),
        ("e5", "edge", "b", "a", 0.008, 6, -2.2),
        ("e6", "edge", "e", "d", 0.008, -6, -2.0),
        ("e7", "edge", "b", "u1", 0.007, 0, 3.0),
    ],
    soft=[],
    chips=[("e1", "a", 0.55, 0.045, 1.5), ("e2", "c", 0.30, 0.035, 1.4)],
    cracks=[[(0.11, 0.03, 0.66), (0.12, 0.00, 0.50), (0.10, 0.04, 0.38)]],
    voxel=0.0040, finer=0,
)

PRISM = dict(
    cuts=[
        ("s1", 20, 3, 0.120, 0, 0, 0.30),
        ("s2", 82, 6, 0.105, 0, 0, 0.30),
        ("s3", 140, 2, 0.125, 0, 0, 0.30),
        ("s4", 200, 7, 0.110, 0, 0, 0.30),
        ("s5", 262, 4, 0.118, 0, 0, 0.30),
        ("s6", 322, 5, 0.108, 0, 0, 0.30),
        ("f1", 50, -40, 0.090, 0, 0, 0.10),
        ("f2", 170, -36, 0.095, 0, 0, 0.10),
        ("f3", 290, -44, 0.085, 0, 0, 0.10),
        ("under", 0, -88, 0.100, 0, 0, 0.10),
        # one broad slanted top and two lesser cuts against it
        ("slant", 60, 58, 0.110, 0, 0, 0.48),
        ("t2", 200, 38, 0.090, 0, 0, 0.50),
        ("t3", 300, 44, 0.085, 0, 0, 0.50),
        ("c1", "corner", "s1", "s2", "slant", 0.014),
        ("c2", "corner", "s4", "s5", "t2", 0.014),
        ("c3", "corner", "s5", "s6", "t3", 0.012),
        ("c4", "corner", "slant", "t2", "t3", 0.010),
        ("c5", "corner", "f1", "s1", "s2", 0.012),
        ("e1", "edge", "s3", "s2", 0.012, 8, 2.0),
        ("e2", "edge", "s1", "s6", 0.012, -8, -2.0),
        ("e3", "edge", "s5", "s4", 0.010, 6, 2.4),
        ("e4", "edge", "s1", "s2", 0.010, -6, 2.4),
        ("e5", "edge", "s4", "s3", 0.009, 6, -2.6),
        ("e6", "edge", "s5", "s6", 0.009, -6, 2.6),
        ("e7", "edge", "slant", "s2", 0.008, 0, 3.0),
        ("e8", "edge", "t3", "slant", 0.007, 0, 3.0),
    ],
    soft=[],
    chips=[("e1", "s3", 0.50, 0.040, 1.4), ("e2", "s1", 0.60, 0.030, 1.3)],
    cracks=[[(0.11, 0.04, 0.44), (0.12, 0.00, 0.32), (0.10, 0.04, 0.20)]],
    voxel=0.0040, finer=0,
)

NUB = dict(
    cuts=[
        ("s1", 10, 14, 0.150, 0, 0, 0.14),
        ("s2", 95, 8, 0.130, 0, 0, 0.14),
        ("s3", 170, 16, 0.156, 0, 0, 0.14),
        ("s4", 262, 10, 0.138, 0, 0, 0.14),
        ("f1", 45, -45, 0.110, 0, 0, 0.08),
        ("f2", 200, -40, 0.115, 0, 0, 0.08),
        ("under", 0, -88, 0.080, 0, 0, 0.08),
        ("top1", 60, 44, 0.094, 0.010, 0, 0.24),
        ("top2", 205, 50, 0.088, 0.010, 0, 0.24),
        ("top3", 320, 38, 0.100, 0.010, 0, 0.24),
        ("c1", "corner", "s1", "top1", "top3", 0.012),
        ("c2", "corner", "s3", "s4", "top2", 0.012),
        ("c3", "corner", "s2", "s3", "top2", 0.012),
        ("e1", "edge", "top3", "s1", 0.010, 6, 3.0),
        ("e2", "edge", "top3", "s4", 0.010, -6, -3.0),
        ("e3", "edge", "s3", "s2", 0.010, 6, 3.0),
        ("e4", "edge", "top2", "s3", 0.008, -6, 3.0),
        ("e5", "edge", "s2", "top1", 0.008, 6, -3.0),
        ("e6", "edge", "top1", "top2", 0.007, 0, 3.0),
    ],
    soft=[],
    chips=[("e3", "s3", 0.50, 0.030, 1.3)],
    cracks=[[(0.13, 0.05, 0.24), (0.14, 0.02, 0.16), (0.13, 0.05, 0.09)]],
    voxel=0.0035, finer=0,
)

# What the cluster grows out of: a low broken mound of ice, so the crystals share a root and are not stood on a floor.
BASE = dict(
    cuts=[
        ("m1", 20, 55, 0.270, 0, 0, 0),
        ("m2", 95, 62, 0.240, 0, 0, 0),
        ("m3", 160, 50, 0.290, 0, 0, 0),
        ("m4", 230, 60, 0.250, 0, 0, 0),
        ("m5", 300, 52, 0.275, 0, 0, 0),
        ("top1", 40, 80, 0.120, 0, 0, 0),
        ("top2", 220, 76, 0.105, 0, 0, 0),
        ("under", 0, -90, 0.200, 0, 0, 0),
        ("c1", "corner", "m1", "m2", "top1", 0.020),
        ("c2", "corner", "m3", "m4", "top2", 0.020),
        ("c3", "corner", "m5", "top1", "top2", 0.016),
        ("e1", "edge", "m3", "m2", 0.020, 6, 2.0),
        ("e2", "edge", "m5", "m1", 0.018, -6, -2.0),
        ("e3", "edge", "top2", "top1", 0.012, 0, 2.0),
        ("e4", "edge", "top2", "m3", 0.014, 6, 2.0),
        ("e5", "edge", "top1", "m1", 0.014, -6, -2.0),
        ("e6", "edge", "m4", "m5", 0.014, 0, 3.0),
    ],
    soft=[],
    chips=[],
    cracks=[],
    voxel=0.0040, finer=0,
)

# The cluster: each piece is (form, where its foot stands, lean about x and about y in degrees, turn about z, scale
# x y z). Set out by hand as ONE growth: every piece leans away from the middle of the mound, the further out the
# more; seven crystals of different heights, widths and leans, then small shards lying low round the base.
CLUSTER_PIECES = [
    ("BASE", (0.000, 0.000, -0.035), (0, 0), 0, (0.62, 0.62, 0.85)),
    ("SPIKE", (-0.030, 0.040, -0.220), (-3, -4), 40, (1.00, 1.00, 1.00)),
    ("LEAN", (0.200, -0.040, -0.120), (4, 22), 200, (1.15, 1.15, 1.25)),
    ("BLADE", (-0.220, -0.100, -0.100), (10, -26), 320, (1.20, 1.20, 1.10)),
    ("SHARD", (0.040, -0.260, -0.250), (24, 6), 115, (0.62, 0.62, 0.72)),
    ("STUB", (-0.140, 0.270, -0.100), (-28, -12), 75, (0.95, 0.95, 0.95)),
    ("PRISM", (0.280, 0.200, -0.080), (-18, 30), 10, (1.00, 1.00, 1.00)),
    ("BLADE", (0.100, 0.160, -0.100), (-12, 8), 150, (0.90, 0.90, 1.25)),
    # the small shards at the base
    ("NUB", (0.400, -0.220, -0.030), (30, 55), 250, (0.80, 0.80, 1.20)),
    ("NUB", (-0.420, 0.120, -0.030), (-15, -60), 30, (0.70, 0.70, 1.35)),
    ("PRISM", (0.100, 0.440, -0.040), (-58, 10), 200, (0.60, 0.60, 0.70)),
    ("NUB", (-0.250, -0.380, -0.020), (48, -35), 120, (0.60, 0.60, 0.90)),
]

FORMS = {"SPIKE": SPIKE, "SHARD": SHARD, "STUB": STUB, "LEAN": LEAN, "BLADE": BLADE, "PRISM": PRISM, "NUB": NUB, "BASE": BASE}
SINGLES = [("ice2_spike", "SPIKE"), ("ice2_shard", "SHARD"), ("ice2_stub", "STUB")]
NAMES = [n for n, _ in SINGLES] + ["ice2_cluster"]

# The look, shared by `IceCrystalRound2.shader` (its property defaults are these same numbers) and the review material.
# Colours are sRGB, as a Unity colour property is typed. THREE HUES, kept apart: the body is navy to deep blue, the
# light coming through is aqua to cyan (clearly greener than the body), and the faces the key lights are a pale
# frosty grey-white. A first pass was one royal blue for all three and read as blue plastic.
LOOK = dict(
    key=(-0.62, 0.60, -0.22),        # toward the key light in Unity's axes: high, to the left, a little in front
    deep=(0.010, 0.040, 0.165),      # the navy a face turned away, and a deep part looked into, goes to
    shade=0.40,
    deep_amount=0.50,
    lit_colour=(0.72, 0.86, 0.95),   # the pale frost the two bright steps are pushed toward
    lit_amount=0.52,
    depth=0.70,
    glow_deep=(0.00, 0.50, 0.85),    # light coming through, where there is little of it
    glow_mid=(0.00, 0.95, 0.95),     # cyan, where there is a lot
    glow_hot=(0.80, 1.00, 0.98),     # and near white where it floods
    glow=1.30, glow_base=0.62, glow_back=0.45, glow_through=0.55,
    edge_soft=(0.05, 0.75, 0.95), edge_sharp=(0.80, 1.00, 1.00),
    edge_soft_amount=0.40, edge_sharp_amount=1.05,
    rim_amount=0.55,
    glint=(0.85, 0.93, 0.97), glint_power=12.0, glint_amount=0.22,
    bump=0.8,
)


# =====================================================================================================================
# THE BLENDER HALF
# =====================================================================================================================

def in_blender():
    import bmesh
    import bpy
    import numpy as np
    from mathutils import Euler, Matrix, Vector, noise
    from mathutils.bvhtree import BVHTree

    argv = sys.argv[sys.argv.index("--") + 1:]
    stage, tag = argv[0], argv[1]
    WORK.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)

    def direction(az, el):
        a, e = math.radians(az), math.radians(el)
        return Vector((math.cos(e) * math.cos(a), math.cos(e) * math.sin(a), math.sin(e)))

    def linear(c):
        return tuple(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c)

    def activate(ob):
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob

    def to_object(name, mesh):
        ob = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(ob)
        return ob

    def apply_modifier(ob, kind, **settings):
        mod = ob.modifiers.new(kind.lower(), kind)
        for key, value in settings.items():
            setattr(mod, key, value)
        activate(ob)
        bpy.ops.object.modifier_apply(modifier=mod.name)

    # ---- the form --------------------------------------------------------------------------------------------------
    def carve(name, recipe, report=False):
        """The typed cuts, the typed bevels and the typed chips. A closed, flat faced solid."""
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        bmesh.ops.scale(bm, vec=(4.0, 4.0, 8.0), verts=bm.verts)
        cut = bm.faces.layers.int.new("cut")
        for f in bm.faces:
            f[cut] = 0
        index, outward, label_of = {}, {}, {}

        def shared(a, b):
            want = {index[a], index[b]}
            return [e for e in bm.edges if len(e.link_faces) == 2 and {e.link_faces[0][cut], e.link_faces[1][cut]} == want]

        def slice_off(label, p, n):
            i = len(index) + 1
            index[label], outward[label], label_of[i] = i, n.copy(), label
            res = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-6,
                                         plane_co=p, plane_no=n, clear_outer=True, clear_inner=False)
            edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
            if not edges:
                print("WARNING %s: cut %s touches nothing" % (name, label))
                return
            for f in bmesh.ops.edgeloop_fill(bm, edges=edges)["faces"]:
                f[cut] = i

        for entry in recipe["cuts"]:
            label = entry[0]
            if entry[1] == "corner":
                a, b, c, depth = entry[2:]
                want = {index[a], index[b], index[c]}
                verts = [v for v in bm.verts if want <= {f[cut] for f in v.link_faces}]
                if not verts:
                    print("WARNING %s: %s, %s and %s do not meet at a point (%s)" % (name, a, b, c, label))
                    index[label] = -100 - len(index)
                    continue
                n = (outward[a] + outward[b] + outward[c]).normalized()
                slice_off(label, verts[0].co - n * depth, n)
            elif entry[1] == "edge":
                a, b, depth, lean, taper = entry[2:]
                edges = shared(a, b)
                if not edges:
                    print("WARNING %s: no edge between %s and %s (%s)" % (name, a, b, label))
                    index[label] = -100 - len(index)
                    continue
                edge = max(edges, key=lambda e: e.calc_length())
                lower, upper = sorted((v.co.copy() for v in edge.verts), key=lambda c: c.z)
                run = (upper - lower).normalized()
                n = (outward[a] + outward[b]).normalized()
                n = Matrix.Rotation(math.radians(lean), 3, run) @ n
                n = Matrix.Rotation(math.radians(taper), 3, run.cross(n).normalized()) @ n
                slice_off(label, (lower + upper) * 0.5 - n * depth, n)
            else:
                az, el, d, cx, cy, cz = entry[1:]
                n = direction(az, el)
                slice_off(label, Vector((cx, cy, cz)) + n * d, n)
        left = sum(1 for f in bm.faces if f[cut] == 0)
        if left:
            print("WARNING %s: %d faces of the starting block were never carved away" % (name, left))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        if report:
            # Which faces touch which, to type the corner and edge cuts from. Read it, do not guess.
            pairs = sorted("%s-%s:%.2f" % (label_of.get(e.link_faces[0][cut], "?"), label_of.get(e.link_faces[1][cut], "?"), e.calc_length())
                           for e in bm.edges if len(e.link_faces) == 2)
            points = sorted("+".join(sorted({label_of.get(f[cut], "?") for f in v.link_faces})) for v in bm.verts)
            print("ADJ %s EDGES %s" % (name, " ".join(pairs)))
            print("ADJ %s POINTS %s" % (name, " ".join(points)))

        # Where each chip bites is read off the edges BEFORE any is bevelled, so a chip can sit on a softened edge too.
        bites = []
        for k, (a, b, along, size, stretch) in enumerate(recipe["chips"]):
            edges = shared(a, b) if index.get(a, -1) > 0 and index.get(b, -1) > 0 else []
            if not edges:
                print("WARNING %s: no edge between %s and %s to chip" % (name, a, b))
                continue
            edge = max(edges, key=lambda e: e.calc_length())
            lower, upper = sorted((v.co.copy() for v in edge.verts), key=lambda c: c.z)
            run = (upper - lower).normalized()
            out = (outward[a] + outward[b]).normalized()
            # A different twist for each chip, so no two bites are cut by the same tool held the same way.
            twist = Matrix.Rotation(math.radians(37.0 * (k + 1)), 3, run)
            frame = twist @ Matrix((run, out, run.cross(out))).transposed()
            bites.append((lower.lerp(upper, along) + out * size * 0.30, frame, size, stretch))
        for a, b, width, segments in recipe["soft"]:
            edges = shared(a, b) if index.get(a, -1) > 0 and index.get(b, -1) > 0 else []
            if not edges:
                print("WARNING %s: no edge between %s and %s to soften" % (name, a, b))
                continue
            made = bmesh.ops.bevel(bm, geom=edges, offset=width, offset_type="OFFSET", segments=segments, profile=0.5,
                                   affect="EDGES", clamp_overlap=True)
            for f in made["faces"]:
                f[cut] = -1
        mesh = bpy.data.meshes.new(name + "_form")
        bm.to_mesh(mesh)
        bm.free()
        ob = to_object(name + "_form", mesh)
        for k, (centre, frame, size, stretch) in enumerate(bites):
            # The tool is a twenty sided ball, stretched along the edge: what it leaves is a faceted scoop.
            block = bmesh.new()
            bmesh.ops.create_icosphere(block, subdivisions=1, radius=size)
            bmesh.ops.scale(block, vec=(stretch, 1.0, 0.8), verts=block.verts)
            bmesh.ops.rotate(block, cent=(0, 0, 0), matrix=frame, verts=block.verts)
            bmesh.ops.translate(block, vec=centre, verts=block.verts)
            cm = bpy.data.meshes.new("chip")
            block.to_mesh(cm)
            block.free()
            cutter = to_object("chip", cm)
            before, kept = len(ob.data.polygons), ob.data.copy()
            apply_modifier(ob, "BOOLEAN", operation="DIFFERENCE", solver="EXACT", object=cutter)
            if len(ob.data.polygons) < 4:
                # The solver has been seen to hand back nothing at all. A chip is not worth a crystal.
                print("WARNING %s: chip %d emptied the form and was left out" % (name, k))
                ob.data = kept
            elif len(ob.data.polygons) == before:
                print("WARNING %s: chip %d cut nothing" % (name, k))
            bpy.data.objects.remove(cutter)
        return ob

    def crack_points(recipe, matrix=None):
        runs = []
        for run in recipe["cracks"]:
            pts = [Vector(p) for p in run]
            runs.append([matrix @ p for p in pts] if matrix is not None else pts)
        return runs

    def piece_matrix(foot, lean, turn, scale):
        return (Matrix.Translation(foot) @ Euler((math.radians(lean[0]), math.radians(lean[1]), 0.0)).to_matrix().to_4x4()
                @ Matrix.Rotation(math.radians(turn), 4, "Z") @ Matrix.Diagonal((scale[0], scale[1], scale[2], 1.0)))

    def cluster_form():
        """The pieces, each carved as itself, stood where it is typed and joined into one closed solid."""
        whole, cracks = None, []
        for k, (form, foot, lean, turn, scale) in enumerate(CLUSTER_PIECES):
            ob = carve("cluster_%d" % k, FORMS[form])
            m = piece_matrix(foot, lean, turn, scale)
            ob.data.transform(m)
            cracks += crack_points(FORMS[form], m)
            if whole is None:
                whole = ob
                continue
            apply_modifier(whole, "BOOLEAN", operation="UNION", solver="EXACT", object=ob)
            bpy.data.objects.remove(ob)
        whole.name = "ice2_cluster_form"
        return whole, cracks

    def roughen(name, form, finer, seed):
        """The form's faces cut finer and nudged, so a plane is not dead flat and an edge does not run dead straight.

        ⚠️ Which edges are CRISP is decided here, on the flat form, where the angle between two faces is exact, and it
        is carried through the finer cut: after the nudge an angle test would call a rough face an edge."""
        bm = bmesh.new()
        bm.from_mesh(form.data)
        bmesh.ops.dissolve_degenerate(bm, dist=1e-5, edges=bm.edges[:])
        bmesh.ops.triangulate(bm, faces=bm.faces[:])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        bm.to_mesh(form.data)
        bm.free()
        me = form.data
        for poly in me.polygons:
            poly.use_smooth = True
        me.set_sharp_from_angle(angle=math.radians(14.0))
        bm = bmesh.new()
        bm.from_mesh(me)
        # ⚠️ THE INSIDE OF A CHIP IS ONE SCOOP. The tool that bites a chip out has twenty faces, and with every one of
        # their edges crisp each became its own island with its own lit border: a chip read as a little wire cage
        # drawn on the crystal. The tool's faces carry no cut number (the solver hands them over as 0), so an edge
        # between two of them is smoothed; the chip keeps its rim, which is the only line a real chip has.
        #
        # ⚠️ AND A SOFTENED EDGE HAS NO LINE AT ALL. A bevel's strips are a couple of millimetres wide: left crisp,
        # each became an island under one texel across, and the edge line drawn round it came out as a row of dashes.
        # Every edge of a bevel's strip (they carry -1) is smoothed, so the two faces run into each other round it.
        cut = bm.faces.layers.int.get("cut")
        if cut is not None:
            inside = soft = 0
            for e in bm.edges:
                if len(e.link_faces) != 2:
                    continue
                one, two = e.link_faces[0][cut], e.link_faces[1][cut]
                if one == 0 and two == 0:
                    e.smooth = True
                    inside += 1
                elif one == -1 or two == -1:
                    e.smooth = True
                    soft += 1
            print("FORM %s: %d edges inside chips and %d on softened edges smoothed" % (name, inside, soft))
        if finer:
            bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=finer, use_grid_fill=True)
            bmesh.ops.triangulate(bm, faces=bm.faces[:])
        bm.normal_update()
        off = Vector((seed * 2.3, seed * 4.1, seed * 1.7))
        moves = []
        for v in bm.verts:
            crisp = any(not e.smooth for e in v.link_edges)
            d = noise.noise(v.co * 5.0 + off) * 0.0022
            if crisp:
                d += noise.noise(v.co * 13.0 - off) * 0.0045
            moves.append(v.normal * d)
        for v, move in zip(bm.verts, moves):
            v.co += move
        bm.to_mesh(me)
        bm.free()
        me.update()

    # ---- the sculpt ------------------------------------------------------------------------------------------------
    def sculpt(name, form, voxel, cracks, seed):
        """The form remeshed fine, chisel marked, grained, partly flattened on its planes, and cracked."""
        high = form.copy()
        high.data = form.data.copy()
        high.name = name + "_high"
        bpy.context.scene.collection.objects.link(high)
        apply_modifier(high, "REMESH", mode="VOXEL", voxel_size=voxel, use_smooth_shade=True)
        me = high.data
        count = len(me.vertices)
        dg = bpy.context.evaluated_depsgraph_get()
        tree = BVHTree.FromObject(form, dg)
        co = np.empty(count * 3, dtype=np.float64)
        no = np.empty(count * 3, dtype=np.float64)
        me.vertices.foreach_get("co", co)
        me.vertices.foreach_get("normal", no)
        co, no = co.reshape(-1, 3), no.reshape(-1, 3)
        push = np.zeros(count)
        off = Vector((seed * 3.7, seed * 1.3, seed * 2.1))
        for i in range(count):
            p, n = Vector(co[i]), Vector(no[i])
            hit = tree.find_nearest(p)
            # How squarely this point sits on one of the form's planes: 1 in the middle of a face, 0 on an edge.
            flat = 0.0
            if hit[0] is not None:
                flat = min(1.0, max(0.0, (n.dot(hit[1]) - 0.955) / 0.04))
            # Broad shallow chisel marks: the ridges between big cells. A first pass had these small and a grain
            # twice as strong, and the lit faces read as stucco.
            cells = noise.voronoi(p * 5.5 + off, distance_metric="DISTANCE", exponent=2.5)[0]
            marks = (cells[1] - cells[0]) * 0.0034 - 0.0010
            grain = noise.fractal(p * 30.0 + off, 1.0, 2.0, 2) * 0.0006
            # PARTLY flattened on the main planes: the marks stay strong along the edges, which is where ice is rough.
            push[i] = (marks + grain) * (1.0 - 0.55 * flat)
        # The cracks: each typed run is laid on the surface and pressed in as a hair groove, and how bright it is at
        # each point is kept, for the bake. Straight between its typed points: a crack kinks, it does not wander.
        glow = np.zeros(count)
        for k, run in enumerate(cracks):
            fine = []
            for a, b in zip(run[:-1], run[1:]):
                steps = max(2, int((b - a).length / 0.010))
                for s in range(steps):
                    hit = tree.find_nearest(a.lerp(b, s / steps))
                    fine.append(np.array(hit[0] if hit[0] is not None else a))
            hit = tree.find_nearest(run[-1])
            fine.append(np.array(hit[0] if hit[0] is not None else run[-1]))
            fine = np.array(fine)
            lo, hi = fine.min(0) - 0.03, fine.max(0) + 0.03
            near = np.where(np.all((co > lo) & (co < hi), axis=1))[0]
            if len(near) == 0:
                continue
            pts = co[near]
            best = np.full(len(near), 1e9)
            along = np.zeros(len(near))
            total = len(fine) - 1
            for s in range(total):
                a, b = fine[s], fine[s + 1]
                ab = b - a
                t = np.clip(((pts - a) @ ab) / max(1e-12, ab @ ab), 0.0, 1.0)
                d = np.linalg.norm(pts - (a + t[:, None] * ab), axis=1)
                closer = d < best
                best[closer] = d[closer]
                along[closer] = (s + t[closer]) / total
            # Brightest where it starts (a crack starts at an edge or a chip) and dying away along its length; and it
            # does not glow evenly: it catches the light in runs. ⚠️ Its WIDTH is never under two of the sculpt's cells:
            # a first pass let it taper to a hair, which is finer than the sculpt can hold, and it baked as a row of
            # beads. It is drawn thin by squaring its profile instead.
            taper = np.clip((1.0 - along) * 1.6, 0.25, 1.0)
            width = max(voxel * 2.2, 0.0070)
            line = np.clip(1.0 - best / width, 0.0, 1.0) ** 2.0
            bright = np.array([0.40 + 0.60 * abs(noise.noise(Vector((a * 9.0, k * 3.1, seed * 1.9)))) * 1.8 for a in along])
            glow[near] = np.maximum(glow[near], line * np.clip(bright, 0.3, 1.0) * (0.45 + 0.55 * taper))
            push[near] -= line ** 1.4 * 0.0032 * taper
        co += no * push[:, None]
        me.vertices.foreach_set("co", co.reshape(-1))
        attribute = me.attributes.new("crack", "FLOAT", "POINT")
        attribute.data.foreach_set("value", glow.astype(np.float32))
        me.update()
        return high

    # ---- the low model ---------------------------------------------------------------------------------------------
    def lowpoly(name, form, open_below=None):
        low = form.copy()
        low.data = form.data.copy()
        low.name = name
        low.data.name = name
        bpy.context.scene.collection.objects.link(low)
        if open_below is not None:
            # A cluster stands on the ground: what is under it is never seen and is not drawn.
            bm = bmesh.new()
            bm.from_mesh(low.data)
            bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-6, plane_co=(0, 0, open_below),
                                   plane_no=(0, 0, -1), clear_outer=True, clear_inner=False)
            bmesh.ops.triangulate(bm, faces=bm.faces[:])
            bm.to_mesh(low.data)
            bm.free()
        me = low.data
        # Crisp where `roughen` found the form turning sharply, and every crisp edge is a seam: so each face of the
        # crystal is its own island, and the painter can find every edge as an island's border.
        for edge in me.edges:
            edge.use_seam = edge.use_edge_sharp
        me.uv_layers.new(name="UVMap")
        activate(low)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.unwrap(method="ANGLE_BASED", margin=0.012)
        bpy.ops.uv.select_all(action="SELECT")
        bpy.ops.uv.average_islands_scale()
        bpy.ops.uv.pack_islands(margin=0.012)
        bpy.ops.object.mode_set(mode="OBJECT")
        # For the painter: every triangle in the atlas, and the points where three or more crisp edges meet.
        uv = me.uv_layers.active.data
        tris = np.array([[tuple(uv[l].uv) for l in poly.loop_indices] for poly in me.polygons if len(poly.loop_indices) == 3], dtype=np.float32)
        np.save(str(WORK / (name + "_uv.npy")), tris)
        crisp = [0] * len(me.vertices)
        for edge in me.edges:
            if edge.use_edge_sharp:
                crisp[edge.vertices[0]] += 1
                crisp[edge.vertices[1]] += 1
        meets = [tuple(uv[l.index].uv) for l in me.loops if crisp[l.vertex_index] >= 3]
        np.save(str(WORK / (name + "_meets.npy")), np.array(meets, dtype=np.float32).reshape(-1, 2))
        return low

    # ---- the bakes -------------------------------------------------------------------------------------------------
    def emit_material(name, build):
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        nt = mat.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        em = nt.nodes.new("ShaderNodeEmission")
        nt.links.new(em.outputs[0], out.inputs["Surface"])
        combine = nt.nodes.new("ShaderNodeCombineXYZ")
        nt.links.new(combine.outputs[0], em.inputs["Color"])
        r, g, b = build(nt)
        for socket, value in zip(combine.inputs, (r, g, b)):
            nt.links.new(value, socket)
        return mat

    def ao_node(nt, distance, inside, samples=12):
        node = nt.nodes.new("ShaderNodeAmbientOcclusion")
        node.inside, node.samples, node.only_local = inside, samples, True
        node.inputs["Distance"].default_value = distance
        return node.outputs["AO"]

    def edge_node(nt, radius, gain):
        """How far the surface's direction, averaged over `radius`, has turned from the direction right here."""
        geo = nt.nodes.new("ShaderNodeNewGeometry")
        bevel = nt.nodes.new("ShaderNodeBevel")
        bevel.samples = 16
        bevel.inputs["Radius"].default_value = radius
        dot = nt.nodes.new("ShaderNodeVectorMath")
        dot.operation = "DOT_PRODUCT"
        nt.links.new(bevel.outputs[0], dot.inputs[0])
        nt.links.new(geo.outputs["Normal"], dot.inputs[1])
        turn = nt.nodes.new("ShaderNodeMath")
        turn.operation = "SUBTRACT"
        turn.inputs[0].default_value = 1.0
        nt.links.new(dot.outputs["Value"], turn.inputs[1])
        scale = nt.nodes.new("ShaderNodeMath")
        scale.operation = "MULTIPLY"
        scale.use_clamp = True
        nt.links.new(turn.outputs[0], scale.inputs[0])
        scale.inputs[1].default_value = gain
        return scale.outputs[0]

    def noise_node(nt, scale, detail, distortion, offset, squash=(1.0, 1.0, 1.0)):
        """A noise in the sculpt's own space, so no seam of the atlas shows in it. `squash` under 1 on an axis draws it
        out along that axis: a patch becomes a stroke."""
        coord = nt.nodes.new("ShaderNodeTexCoord")
        stretch = nt.nodes.new("ShaderNodeVectorMath")
        stretch.operation = "MULTIPLY"
        nt.links.new(coord.outputs["Object"], stretch.inputs[0])
        stretch.inputs[1].default_value = squash
        move = nt.nodes.new("ShaderNodeVectorMath")
        move.operation = "ADD"
        nt.links.new(stretch.outputs[0], move.inputs[0])
        move.inputs[1].default_value = offset
        tex = nt.nodes.new("ShaderNodeTexNoise")
        tex.inputs["Scale"].default_value = scale
        tex.inputs["Detail"].default_value = detail
        tex.inputs["Distortion"].default_value = distortion
        nt.links.new(move.outputs[0], tex.inputs["Vector"])
        return tex.outputs[0]

    def bake_all(name, low, high, seed):
        zs = [v.co.z for v in low.data.vertices]
        z0, z1 = min(zs), max(zs)

        def surface(nt):       # R ambient occlusion, G the edge over a wide radius, B the edge over a thin one
            return ao_node(nt, 0.22, False), edge_node(nt, 0.050, 5.0), edge_node(nt, 0.0140, 9.0)

        def inside(nt):        # R thickness over a long reach, G height, B thickness over a short reach
            coord = nt.nodes.new("ShaderNodeTexCoord")
            split = nt.nodes.new("ShaderNodeSeparateXYZ")
            nt.links.new(coord.outputs["Object"], split.inputs[0])
            span = nt.nodes.new("ShaderNodeMapRange")
            span.inputs["From Min"].default_value, span.inputs["From Max"].default_value = z0, z1
            nt.links.new(split.outputs["Z"], span.inputs["Value"])
            return ao_node(nt, 0.30, True, 16), span.outputs[0], ao_node(nt, 0.10, True, 16)

        o = (seed * 1.7, seed * 0.9, seed * 2.3)

        def paint(nt):         # R big slow blotches, G upright brush strokes, B a fine speckle
            return noise_node(nt, 2.0, 1.0, 0.4, o), noise_node(nt, 4.2, 1.2, 0.9, o, (1.0, 1.0, 0.30)), noise_node(nt, 30.0, 3.0, 0.4, o)

        def extra(nt):         # R the cracks, G a slow cloud, B a second set of strokes, slanted the other way
            crack = nt.nodes.new("ShaderNodeAttribute")
            crack.attribute_name = "crack"
            return crack.outputs["Fac"], noise_node(nt, 1.3, 0.5, 0.2, (o[2], o[0], o[1])), noise_node(nt, 3.0, 1.0, 1.4, (o[1], o[2], o[0]), (1.0, 0.45, 0.40))

        target = bpy.data.materials.new(name + "_target")
        target.use_nodes = True
        node = target.node_tree.nodes.new("ShaderNodeTexImage")
        target.node_tree.nodes.active = node
        low.data.materials.clear()
        low.data.materials.append(target)
        scene = bpy.context.scene
        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = 32
        scene.cycles.use_denoising = False
        scene.cycles.seed = 7
        bake = scene.render.bake
        bake.margin, bake.margin_type = 8, "EXTEND"
        bake.use_selected_to_active, bake.use_clear = True, True
        # ⚠️ THE CAGE. Rays leave along the low model's normals, and at a crisp edge those are split in two, so the
        # rays fan apart and the sculpt's edge, the very thing the edge masks are for, falls in the gap: it baked as a
        # dotted line. A copy of the low model with every edge smooth, swollen a little, has no gaps.
        cage = low.copy()
        cage.data = low.data.copy()
        cage.name = name + "_cage"
        bpy.context.scene.collection.objects.link(cage)
        for edge in cage.data.edges:
            edge.use_edge_sharp = False
        cage.data.update()
        for vert in cage.data.vertices:
            vert.co = vert.co + vert.normal * 0.022
        cage.hide_render = True
        bake.use_cage, bake.cage_object = True, cage
        bake.cage_extrusion, bake.max_ray_distance = 0.0, 0.0
        bake.normal_space = "TANGENT"
        for label, kind, build in (("normal", "NORMAL", None), ("surface", "EMIT", surface), ("inside", "EMIT", inside),
                                   ("paint", "EMIT", paint), ("extra", "EMIT", extra)):
            image = bpy.data.images.new(name + "_" + label, SIZE, SIZE, alpha=True, float_buffer=True)
            image.colorspace_settings.name = "Non-Color"
            node.image = image
            high.data.materials.clear()
            if build is not None:
                high.data.materials.append(emit_material(name + "_" + label, build))
            bpy.ops.object.select_all(action="DESELECT")
            high.select_set(True)
            low.select_set(True)
            bpy.context.view_layer.objects.active = low
            bpy.ops.object.bake(type=kind)
            px = np.empty(SIZE * SIZE * 4, dtype=np.float32)
            image.pixels.foreach_get(px)
            np.save(str(WORK / (name + "_" + label + ".npy")), px.reshape(SIZE, SIZE, 4)[::-1])
            print("baked", name, label)
        low.data.materials.clear()
        plain = bpy.data.materials.new("ice")
        low.data.materials.append(plain)

    def export(name, low):
        activate(low)
        bpy.ops.export_scene.gltf(filepath=str(OUT / (name + ".glb")), export_format="GLB", use_selection=True,
                                  export_tangents=True, export_yup=True, export_animations=False)
        tris = len(low.data.polygons)
        box = [list(low.bound_box[0]), list(low.bound_box[6])]
        top = max(low.data.vertices, key=lambda v: v.co.z).co
        json.dump({"triangles": tris, "vertices": len(low.data.vertices), "box": box, "tip": list(top)},
                  open(WORK / (name + ".json"), "w"))
        print("wrote", name, tris, "triangles")

    def forms(report=False):
        made = []
        for k, (name, form) in enumerate(SINGLES):
            ob = carve(name, FORMS[form], report)
            roughen(name, ob, FORMS[form]["finer"], k + 1)
            made.append((name, ob, crack_points(FORMS[form]), FORMS[form]["voxel"], None, k + 1))
        if report:
            for form in ("LEAN", "BLADE", "PRISM", "NUB", "BASE"):
                bpy.data.objects.remove(carve(form, FORMS[form], True))
        whole, cracks = cluster_form()
        roughen("ice2_cluster", whole, 0, 9)
        made.append(("ice2_cluster", whole, cracks, 0.0050, 0.0, 9))
        return made

    # ---- looking at it ---------------------------------------------------------------------------------------------
    VIEWS = [("front", 0, 10), ("quarter", 45, 10), ("side", 90, 10), ("back", 180, 10), ("top", 20, 68)]

    def studio():
        scene = bpy.context.scene
        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = 12
        scene.cycles.use_denoising = False
        scene.view_settings.view_transform = "Standard"
        scene.render.film_transparent = False
        world = bpy.data.worlds.new("dark")
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs[0].default_value = (0.018, 0.020, 0.026, 1)
        scene.world = world
        cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
        scene.collection.objects.link(cam)
        scene.camera = cam
        cam.data.type = "ORTHO"
        return scene, cam

    def shoot(scene, cam, ob, prefix, width, height):
        pts = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
        lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
        hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
        mid, tall, wide = (lo + hi) * 0.5, hi.z - lo.z, max(hi.x - lo.x, hi.y - lo.y)
        scene.render.resolution_x, scene.render.resolution_y = width, height
        cam.data.ortho_scale = max(wide * 1.25 * max(1.0, width / height), (tall + wide * 0.4) * 1.12 * max(1.0, width / height))
        for label, yaw, pitch in VIEWS:
            # The crystal turns and the lens and the light stay, as they do in the game's film.
            ob.rotation_mode = "XYZ"
            ob.rotation_euler = (0, 0, math.radians(yaw))
            e = math.radians(pitch)
            cam.location = mid + Vector((0, -math.cos(e), math.sin(e))) * 6.0
            cam.rotation_euler = (math.radians(90) - e, 0, 0)
            scene.render.filepath = str(WORK / ("%s_%s.png" % (prefix, label)))
            bpy.ops.render.render(write_still=True)
        ob.rotation_euler = (0, 0, 0)

    def game_material(name):
        """`IceCrystalRound2.shader`, written again in nodes. Emission only: nothing here is ray traced through the crystal.
        It leaves out the shader's inner layer and its shimmer."""
        mat = bpy.data.materials.new(name + "_game")
        mat.use_nodes = True
        nt = mat.node_tree
        nt.nodes.clear()
        L = LOOK

        def image(suffix, colour):
            node = nt.nodes.new("ShaderNodeTexImage")
            node.image = bpy.data.images.load(str(OUT / (name + suffix)))
            node.image.colorspace_settings.name = "sRGB" if colour else "Non-Color"
            # The sheen and the thickness ride in alpha and are not a coverage.
            node.image.alpha_mode = "CHANNEL_PACKED"
            return node

        def put(socket, value):
            if hasattr(value, "is_output"):
                nt.links.new(value, socket)
            elif isinstance(value, (tuple, list)):
                socket.default_value = tuple(value)[:len(socket.default_value)] if hasattr(socket.default_value, "__len__") else value[0]
            else:
                socket.default_value = value

        def m(op, a, b=0.0, c=0.0, clamp=False):
            node = nt.nodes.new("ShaderNodeMath")
            node.operation, node.use_clamp = op, clamp
            put(node.inputs[0], a), put(node.inputs[1], b), put(node.inputs[2], c)
            return node.outputs[0]

        def v(op, a, b=(0, 0, 0), scale=None):
            node = nt.nodes.new("ShaderNodeVectorMath")
            node.operation = op
            put(node.inputs[0], a)
            put(node.inputs[1], b)
            if scale is not None:
                put(node.inputs["Scale"], scale)
            return node.outputs["Value"] if op in ("DOT_PRODUCT", "LENGTH") else node.outputs[0]

        def step(lo, hi, x):
            node = nt.nodes.new("ShaderNodeMapRange")
            node.interpolation_type = "SMOOTHSTEP"
            put(node.inputs["Value"], x)
            node.inputs["From Min"].default_value, node.inputs["From Max"].default_value = lo, hi
            return node.outputs[0]

        def mix(a, b, f):      # a + (b - a) * f, on vectors
            return v("ADD", a, v("SCALE", v("SUBTRACT", b, a), scale=f))

        def sat(x):
            return m("MAXIMUM", m("MINIMUM", x, 1.0), 0.0)

        albedo, mask, bump = image("_albedo.png", True), image("_mask.png", False), image("_normal.png", False)
        split = nt.nodes.new("ShaderNodeSeparateXYZ")
        nt.links.new(mask.outputs["Color"], split.inputs[0])
        glow, wide, thin, thick = split.outputs[0], split.outputs[1], split.outputs[2], mask.outputs["Alpha"]
        sheen = albedo.outputs["Alpha"]
        nmap = nt.nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = L["bump"]
        nt.links.new(bump.outputs["Color"], nmap.inputs["Color"])
        geo = nt.nodes.new("ShaderNodeNewGeometry")
        N, V = nmap.outputs[0], geo.outputs["Incoming"]
        kx, ky, kz = L["key"]
        key = Vector((kx, kz, ky)).normalized()          # Unity's y up to Blender's z up; the lens looks along +y here
        ndl = v("DOT_PRODUCT", N, tuple(key))
        top, mid = step(0.58, 0.68, ndl), step(0.18, 0.28, ndl)
        lit = m("ADD", m("ADD", m("MULTIPLY", step(-0.25, -0.15, ndl), 0.35), m("MULTIPLY", mid, 0.35)),
                m("MULTIPLY", top, 0.30))
        deep = linear(L["deep"])
        col = v("SCALE", albedo.outputs["Color"], scale=m("ADD", L["shade"], m("MULTIPLY", lit, 1.0 - L["shade"])))
        col = mix(col, deep, m("MULTIPLY", m("SUBTRACT", 1.0, lit), L["deep_amount"]))
        frost = v("SCALE", linear(L["lit_colour"]), scale=m("ADD", 0.82, m("MULTIPLY", sheen, 0.30)))
        col = mix(col, frost, m("MULTIPLY", m("ADD", m("MULTIPLY", mid, 0.12), m("MULTIPLY", top, 0.7)), L["lit_amount"]))
        facing = sat(v("DOT_PRODUCT", N, V))
        col = mix(col, tuple(c * 0.6 for c in deep), m("MULTIPLY", m("MULTIPLY", thick, m("MULTIPLY", facing, facing)), L["depth"]))
        back = sat(m("ADD", m("MULTIPLY", v("DOT_PRODUCT", V, tuple(-key)), 0.5), 0.5))
        through = sat(m("ADD", m("MULTIPLY", ndl, -0.5), 0.5))
        fres = m("POWER", m("SUBTRACT", 1.0, facing), 3.0)
        g = m("MULTIPLY", glow, m("ADD", m("ADD", L["glow_base"], m("MULTIPLY", m("MULTIPLY", back, back), L["glow_back"])),
                                  m("MULTIPLY", through, L["glow_through"])))
        g = m("ADD", g, m("MULTIPLY", m("MULTIPLY", fres, L["rim_amount"]), m("SUBTRACT", 1.0, m("MULTIPLY", thick, 0.6))))
        glow_colour = mix(linear(L["glow_deep"]), linear(L["glow_mid"]), sat(m("MULTIPLY", g, 1.5)))
        glow_colour = mix(glow_colour, linear(L["glow_hot"]), step(0.85, 1.50, g))
        col = v("ADD", col, v("SCALE", glow_colour, scale=m("MULTIPLY", g, L["glow"])))
        col = v("ADD", col, v("SCALE", linear(L["edge_soft"]), scale=m("MULTIPLY", m("MULTIPLY", wide, L["edge_soft_amount"]),
                                                                          m("ADD", 0.6, m("MULTIPLY", lit, 0.4)))))
        col = v("ADD", col, v("SCALE", linear(L["edge_sharp"]), scale=m("MULTIPLY", m("MULTIPLY", thin, L["edge_sharp_amount"]),
                                                                           m("ADD", 0.55, m("MULTIPLY", lit, 0.45)))))
        half = v("NORMALIZE", v("ADD", V, tuple(key)))
        spec = m("POWER", sat(v("DOT_PRODUCT", N, half)), L["glint_power"])
        soft = m("MULTIPLY", m("MULTIPLY", step(0.20, 0.90, spec), sat(m("ADD", 0.25, m("MULTIPLY", sheen, 0.9)))),
                 m("MULTIPLY", m("ADD", 0.3, m("MULTIPLY", lit, 0.7)), L["glint_amount"]))
        col = v("ADD", col, v("SCALE", linear(L["glint"]), scale=soft))
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        em = nt.nodes.new("ShaderNodeEmission")
        nt.links.new(col, em.inputs["Color"])
        nt.links.new(em.outputs[0], out.inputs["Surface"])
        return mat

    bpy.ops.wm.read_factory_settings(use_empty=True)
    if stage == "forms":
        made = forms(report=True)
        scene, cam = studio()
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
        sun.data.energy, sun.data.angle = 3.0, math.radians(8)
        sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
        scene.collection.objects.link(sun)
        fill = bpy.data.objects.new("fill", bpy.data.lights.new("fill", "SUN"))
        fill.data.energy, fill.data.color = 0.8, (0.6, 0.8, 1.0)
        fill.rotation_euler = (math.radians(70), 0, math.radians(150))
        scene.collection.objects.link(fill)
        clay = bpy.data.materials.new("clay")
        clay.use_nodes = True
        clay.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.45, 0.6, 0.75, 1)
        for name, form, cracks, voxel, cut, seed in made:
            for other in made:
                other[1].hide_render = other[1] is not form
            form.data.materials.append(clay)
            print("FORM", name, len(form.data.polygons), "triangles")
            shoot(scene, cam, form, "forms_" + name, 360, 480 if name != "ice2_cluster" else 400)
    elif stage == "bake":
        only = argv[2:]
        for name, form, cracks, voxel, cut, seed in forms():
            if only and name not in only:
                continue
            high = sculpt(name, form, voxel, cracks, seed)
            low = lowpoly(name, form, cut)
            form.hide_render = True
            print("SCULPT", name, len(high.data.polygons), "faces; low", len(low.data.polygons), "triangles")
            bake_all(name, low, high, seed)
            export(name, low)
            high.hide_render = True
            low.hide_render = True
    elif stage == "review":
        scene, cam = studio()
        for name in NAMES:
            before = set(bpy.data.objects)
            bpy.ops.import_scene.gltf(filepath=str(OUT / (name + ".glb")))
            ob = [o for o in set(bpy.data.objects) - before if o.type == "MESH"][0]
            ob.data.materials.clear()
            ob.data.materials.append(game_material(name))
            for other in bpy.data.objects:
                if other.type == "MESH":
                    other.hide_render = other is not ob
            shoot(scene, cam, ob, "review_" + name, 400, 560 if name != "ice2_cluster" else 440)


# =====================================================================================================================
# THE PAINTING HALF (the system's Python)
# =====================================================================================================================

def paint(name):
    """The three maps, painted from the bakes. Every mask is levelled against its own crystal's range."""
    import cv2
    import numpy as np
    from PIL import Image

    def load(label):
        return np.load(str(WORK / (name + "_" + label + ".npy"))).astype(np.float32)

    def levels(x, lo, hi, gamma=1.0):
        return np.clip((x - lo) / max(1e-6, hi - lo), 0.0, 1.0) ** gamma

    def smooth(x):
        return x * x * (3.0 - 2.0 * x)

    def blur(x, sigma):
        return cv2.GaussianBlur(x, (0, 0), sigma)

    def spread(x, where, lo, hi):
        a, b = np.percentile(x[where], [lo, hi])
        return levels(x, a, b)

    def mixc(a, b, f):
        return a + (np.array(b, dtype=np.float32) - a) * f[..., None]

    normal, surface, inside, noise, extra = load("normal"), load("surface"), load("inside"), load("paint"), load("extra")
    # What the bake reached (its margin included). The noise is never exactly nothing where it was written.
    cover = noise[..., 0] > 1e-4
    ao = np.clip(blur(surface[..., 0], 1.0), 0, 1)
    wide_raw, thin_raw = surface[..., 1], surface[..., 2]
    thick_long, height, thick_short = blur(inside[..., 0], 1.6), inside[..., 1], blur(inside[..., 2], 1.2)
    blotch, stroke, speck = spread(noise[..., 0], cover, 2, 98), spread(noise[..., 1], cover, 2, 98), spread(noise[..., 2], cover, 2, 98)
    crack, cloud, slant = np.clip(extra[..., 0], 0, 1), spread(extra[..., 1], cover, 2, 98), spread(extra[..., 2], cover, 2, 98)

    # ⚠️ THE EDGES ARE DRAWN HERE, IN THE ATLAS, NOT BAKED. Every crisp edge of the low model is a seam, so every face
    # of the crystal is its own island and an island's border IS an edge of the crystal. A baked edge mask has its
    # line exactly on the seam, where the bake is at its worst: it came out dashed. The distance from an island's
    # border is exact, the same width everywhere (the islands are all at one scale), and is painted on both sides of
    # the seam, so no filter can find a gap in it.
    # The cluster's faces are many and small in the atlas, so its lines are drawn finer; and it is thin everywhere,
    # so less of it is let glow for being thin, and its flood stays low, at the root the pieces share.
    cluster = name == "ice2_cluster"
    line, flood_from, thin_share = (0.55, 0.78, 0.40) if cluster else (1.0, 0.60, 0.62)
    tris = np.load(str(WORK / (name + "_uv.npy")))
    face = np.zeros((SIZE, SIZE), np.uint8)
    pts = np.stack([tris[..., 0] * SIZE, (1.0 - tris[..., 1]) * SIZE], axis=-1)
    cv2.fillPoly(face, [np.round(p).astype(np.int32) for p in pts], 255)
    within = cv2.distanceTransform(face, cv2.DIST_L2, 5)
    beyond = cv2.distanceTransform(255 - face, cv2.DIST_L2, 5)
    on_face = face > 0
    hair = np.where(on_face, np.exp(-np.maximum(within - 0.6, 0.0) / (1.5 * line)), (beyond < 9).astype(np.float32))
    band = np.where(on_face, np.exp(-within / (10.0 * line)), (beyond < 9).astype(np.float32))
    # Brighter where edges meet: a small hot point at every corner three crisp edges share.
    meets = np.load(str(WORK / (name + "_meets.npy")))
    points = np.zeros((SIZE, SIZE), np.float32)
    for u, v in meets:
        x, y = int(round(u * SIZE)), int(round((1.0 - v) * SIZE))
        if 0 <= x < SIZE and 0 <= y < SIZE:
            points[y, x] = 1.0
    points = np.clip(blur(points, 3.0) * 60.0, 0.0, 1.0)

    # THICKNESS: 1 where a lot of crystal lies behind the surface.
    thick = spread(0.65 * thick_long + 0.35 * thick_short, cover, 6, 94)

    # GLOW-THROUGH. ⚠️ A first pass lit only the thin band round each face, and the crystal read as a set of outlined
    # panes. Light in ice FLOODS: the foot and the thin end are full of it, near white at the hottest, and it grades
    # up into the dark core; and a whole facet glows where the crystal behind it is thin. So this is a volume, not an
    # outline: a flood up from the foot, broken by painted strokes so it is not an even film, the thin facets, the
    # tip, and the cracks.
    strokes = 0.28 + 0.72 * smooth(levels(0.6 * stroke + 0.4 * slant, 0.32, 0.62))
    flood = smooth(levels(1.0 - height, flood_from, 0.93)) * (1.0 - 0.30 * thick) * strokes
    hot_foot = smooth(levels(1.0 - height, 0.84, 0.98))
    thin_part = smooth(levels(1.0 - thick, 0.46, 0.94)) * (0.35 + 0.65 * smooth(levels(blotch, 0.30, 0.65)))
    tip = levels(height, 0.80, 1.0, 1.5)
    glow = np.clip(np.maximum(flood, 0.85 * hot_foot) + thin_share * thin_part * (1.0 - 0.5 * flood) + 0.55 * tip + 0.45 * crack
                   + 0.25 * points, 0.0, 1.0)
    glow[~cover] = 0.0

    # Specks and scratches: a few pale marks on the faces, scattered once from a fixed seed.
    rng = np.random.default_rng(NAMES.index(name) + 11)
    marks = np.zeros((SIZE, SIZE), np.float32)
    ys, xs = np.where(within > 6)
    for k in rng.choice(len(ys), 260, replace=False):
        cv2.circle(marks, (int(xs[k]), int(ys[k])), 0, float(rng.uniform(0.3, 1.0)), -1)
    for k in rng.choice(len(ys), 18, replace=False):
        a, l = rng.uniform(0, math.pi), rng.uniform(8, 34)
        p = (int(xs[k]), int(ys[k]))
        q = (int(p[0] + math.cos(a) * l), int(p[1] + math.sin(a) * l))
        cv2.line(marks, p, q, float(rng.uniform(0.25, 0.7)), 1, cv2.LINE_AA)
    marks = np.clip(blur(marks, 0.7) * 1.6, 0, 1) * (within > 3)

    # EDGES. The thin sharp pass is the hair line on every edge, not quite even along its length, hot where edges
    # meet, plus what the bake found on the softened edges and chips, plus the cracks. The wide pass is its soft bed.
    uneven = 0.62 + 0.38 * smooth(levels(slant, 0.25, 0.75))
    baked_thin = smooth(levels(blur(thin_raw, 1.6), 0.035, 0.30)) * on_face * (within > 2.5)
    thin = np.clip(np.maximum(np.maximum(hair * uneven, 0.45 * baked_thin), 0.85 * crack) + 0.6 * points + 0.12 * marks, 0.0, 1.0)
    baked_wide = smooth(levels(blur(wide_raw, 2.0), 0.03, 0.55))
    wide = np.clip(np.maximum(band * (0.55 + 0.45 * blotch), 0.8 * baked_wide) + 0.5 * blur(crack, 2.5), 0.0, 1.0)

    # ALBEDO. A clear gradient up the crystal (deep at the foot, lighter at the top), big soft upright strokes over
    # it, the deep parts navy, a slow cloud, a little frost where the edges are. No small swirls: a first pass had a
    # marbling that read as a noise function, not a brush.
    navy, low_blue, high_blue = (0.012, 0.045, 0.170), (0.025, 0.100, 0.360), (0.075, 0.300, 0.680)
    lighter_blue, pale = (0.110, 0.400, 0.760), (0.700, 0.880, 0.960)
    low, high = np.array(low_blue, dtype=np.float32), np.array(high_blue, dtype=np.float32)
    col = low[None, None, :] + (high - low) * smooth(levels(height, 0.05, 0.95))[..., None]
    soft_stroke = smooth(levels(blur(stroke, 3.0), 0.25, 0.75))
    col = col * (0.72 + 0.50 * soft_stroke)[..., None]
    col = mixc(col, lighter_blue, 0.38 * smooth(levels(blur(slant, 3.0), 0.55, 0.80)))
    col = mixc(col, navy, 0.40 * smooth(levels(1.0 - blur(blotch, 3.0), 0.55, 0.85)))
    col = mixc(col, navy, 0.60 * smooth(thick))
    col = col * (0.84 + 0.32 * cloud)[..., None]
    col = col * (0.50 + 0.50 * ao)[..., None]
    col = mixc(col, pale, np.clip(0.22 * wide + 0.34 * thin, 0, 1))
    # THE SHEEN'S BREAK-UP: big soft patches of more and less, a little grit in it. Never hard islands: a first pass
    # cut this mask hard and the glint came out as white flakes, like peeling paint.
    sheen = np.clip(smooth(levels(blur(0.55 * cloud + 0.45 * stroke, 4.0), 0.25, 0.80)) * (0.80 + 0.30 * speck) + 0.35 * marks, 0, 1)

    def save(path, planes):
        arr = (np.clip(np.dstack(planes), 0, 1) * 255.0 + 0.5).astype(np.uint8)
        Image.fromarray(arr, "RGBA" if arr.shape[2] == 4 else "RGB").save(path)

    save(OUT / (name + "_albedo.png"), [col[..., 0], col[..., 1], col[..., 2], sheen])
    nrm = normal[..., :3].copy()
    nrm[~(normal[..., 3] > 0.5)] = (0.5, 0.5, 1.0)
    save(OUT / (name + "_normal.png"), [nrm[..., 0], nrm[..., 1], nrm[..., 2]])
    save(OUT / (name + "_mask.png"), [glow, wide, thin, thick])
    # What each map looks like alone, for the contact sheet: a flat grey thickness or a speckled edge means a bad bake.
    tiles = [col, np.dstack([sheen] * 3), nrm, np.dstack([thick] * 3), np.dstack([glow] * 3), np.dstack([wide] * 3),
             np.dstack([thin] * 3), np.dstack([ao] * 3)]
    return [cv2.resize((np.clip(t, 0, 1) * 255).astype(np.uint8)[..., ::-1], (320, 320), interpolation=cv2.INTER_AREA) for t in tiles]


def join(prefix, rows, columns, out, labels=True):
    import cv2
    sheet = []
    for row in rows:
        ims = []
        for column in columns:
            im = cv2.imread(str(WORK / ("%s_%s_%s.png" % (prefix, row, column))))
            if labels:
                cv2.putText(im, row.replace("ice2_", "") + " " + column, (8, 20), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (235, 235, 235), 1)
            ims.append(im)
        sheet.append(ims)
    width = max(sum(im.shape[1] for im in ims) for ims in sheet)
    lines = []
    for ims in sheet:
        line = cv2.hconcat(ims)
        if line.shape[1] < width:
            line = cv2.copyMakeBorder(line, 0, 0, 0, width - line.shape[1], cv2.BORDER_CONSTANT, value=(6, 5, 5))
        lines.append(line)
    cv2.imwrite(str(out), cv2.vconcat(lines), [cv2.IMWRITE_JPEG_QUALITY, 90])
    print("wrote", out)


def blender(*words):
    done = subprocess.run([BLENDER, "-b", "--python", str(Path(__file__).resolve()), "--"] + list(words),
                          capture_output=True, text=True)
    keep = [l for l in done.stdout.splitlines() if l.startswith(("WARNING", "FORM", "SCULPT", "baked", "wrote", "Error", "Traceback", "ADJ"))]
    print("\n".join(keep))
    if done.returncode != 0 or "Traceback" in done.stdout + done.stderr:
        print(done.stdout[-3000:], done.stderr[-3000:])
        raise SystemExit("Blender failed at " + " ".join(words))


def main():
    import cv2
    stage = sys.argv[1] if len(sys.argv) > 1 else "build"
    tag = sys.argv[2] if len(sys.argv) > 2 else "v1"
    only = sys.argv[3:]
    LOGS.mkdir(parents=True, exist_ok=True)
    views = ["front", "quarter", "side", "back", "top"]
    if stage == "forms":
        blender("forms", tag)
        join("forms", NAMES, views, LOGS / ("round2_forms_%s.jpg" % tag))
        return
    if stage == "build":
        blender("bake", tag, *only)
    if stage in ("build", "paint"):
        rows = [cv2.hconcat(paint(name)) for name in NAMES]
        cv2.imwrite(str(LOGS / ("round2_maps_%s.jpg" % tag)), cv2.vconcat(rows), [cv2.IMWRITE_JPEG_QUALITY, 88])
        print("wrote", LOGS / ("round2_maps_%s.jpg" % tag), "(albedo, sheen, normal, thickness, glow, wide edge, thin edge, occlusion)")
    blender("review", tag)
    join("review", NAMES, views, LOGS / ("round2_sheet_%s.jpg" % tag))
    for name in NAMES:
        info = json.load(open(WORK / (name + ".json")))
        print(name, info["triangles"], "triangles,", info["vertices"], "vertices")


if __name__ == "__main__":
    try:
        import bpy  # noqa: F401
        inside_blender = True
    except ImportError:
        inside_blender = False
    if inside_blender:
        in_blender()
    else:
        main()
