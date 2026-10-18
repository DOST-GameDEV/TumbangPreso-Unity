"""PAYO props, SET C: light and water.   py -3 tools/payo_pieces/prop_set_c.py

A stone fire bowl on a carved pedestal, a hanging lantern on a bent bamboo, a standing torch, a bamboo water pipe on
forked posts with a little pour, a stone water trough.
SEPARATE OBJECTS for the night: fire_bowl__flame, torch__flame, lantern_pole__lantern (the lamp by itself, to hang
anywhere or to light), water_pipe__water (the pour and its splash, to move or hide).
⚠️ STONE IS GREY (owner, 2026-10-12): the cool tone of the reference is its light. Only the WATER is turquoise.
Sizes are real; the kit writes them 15 percent larger.
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from prop_kit import *                                 # noqa: E402,F401,F403
from prop_bits import *                                # noqa: E402,F401,F403

S = PropSet("c", ppm=300)
DARK = (104, 60, 46)
WATER, WATER_D, FOAM = (86, 196, 204), (48, 140, 164), (240, 252, 250)


def coals(P, r, y, key="coals"):
    m = lumpy(planar(lathe([(0.0, 0.0), (r, 0.0), (r * 0.9, r * 0.14), (r * 0.5, r * 0.3), (0.0, r * 0.34)], 10), 1), 0.12, 9)
    P.add(T(m, at=(0, y, 0)), lambda C: (C.fill((54, 44, 48)), C.specks((30, 26, 30), 30, 0.03), C.specks(ORANGE, 30, 0.022), C.specks(GOLD, 16, 0.012), C.specks((120, 112, 108), 10, 0.016)), key=key)


def leaf_tuft(P, at, n=3, size=0.3, seed=0, key="tuft"):
    n = min(n, 3)
    for k in range(n):
        yaw = 360.0 * k / n + seed * 37
        lf = pillow(leaf_outline(size, size * 0.34), 0.014, ((1, 0), (0.7, 0.85), (0, 1)))
        lf = warp(lf, lambda x, y, z: (x, y, z - 1.6 * (x + size / 2) ** 2))
        P.add(T(T(T(lf, at=(size / 2, 0, 0)), turn=(90, 0, 52 - 9 * (k % 2))), turn=(0, yaw, 0), at=at), leaf_paint(), key=key)


# ------------------------------------------------------------------ the fire bowl

P = S.prop("fire_bowl", "1.1 wide x 1.15 m tall, the flame 0.65 more", at=(-0.3, -1.0))
P.add(T(cbox((0.46, 0.08, 0.46), 0.04), at=(0, 0.08, 0)), stone(STONE, bevel=0.04, moss=0.5))
P.add(T(cbox((0.37, 0.07, 0.37), 0.035), at=(0, 0.22, 0)), stone(STONE_L, bevel=0.035, bands=[(0.25, 0.75, "step", STONE, STONE_D)], top_light=True))
shaft = lathe([(0.27, 0.28), (0.21, 0.34), (0.20, 0.40), (0.235, 0.44), (0.235, 0.62), (0.20, 0.66), (0.22, 0.72), (0.30, 0.76)], 8)
ts = shaft["t"]


def shaft_paint(C):
    C.fill(STONE)
    C.specks(shade(STONE, 0.86), 30, 0.02); C.specks(shade(STONE, 1.12), 20, 0.016)
    C.motif(ts[3], ts[4], "eye", OCHRE, n=8, bg=RED, edge=TURQ)                       # worn paint in the carving
    C.band(ts[4], ts[4] + 0.03, PALE); C.band(ts[3] - 0.03, ts[3], PALE)
    C.motif(ts[1] - 0.01, ts[2] + 0.01, "dot", TURQ, n=16)
    C.band(ts[6], 1, STONE_L)
    C.vgrad(0.82, 1.06)
    for k in range(8):                                                                # the pale edge of each face
        C.box(k / 8.0 - 0.012, 0, k / 8.0 + 0.012, 1, PALE)


P.add(shaft, shaft_paint, k=1.2)
bowl = lathe([(0.22, 0.74), (0.40, 0.84), (0.52, 0.98), (0.55, 1.08), (0.52, 1.12), (0.46, 1.10), (0.42, 1.02), (0.0, 0.98)], 12)
tb = bowl["t"]


def bowl_paint(C):
    C.fill(STONE)
    C.specks(shade(STONE, 0.86), 40, 0.022); C.specks(shade(STONE, 1.12), 26, 0.016)
    C.motif(tb[1] + 0.02, tb[2] + 0.02, "tri2", STONE_L, n=12, bg=STONE_D, edge=OCHRE)
    for u in (0.13, 0.55, 0.8):
        C.line([(u, tb[2] + 0.1), (u + 0.03, tb[2] + 0.02), (u - 0.02, tb[1] + 0.03)], STONE_D, C.lw(0.01))
    C.vgrad(0.80, 1.05)
    C.band(tb[3] - 0.02, tb[5], PALE)                                                  # the rim
    C.band(tb[5], 1, (58, 50, 52))                                                     # soot inside


P.add(bowl, bowl_paint, k=1.2)
coals(P, 0.43, 0.99)
flames(P, [(0.0, 1.04, 0.10, 0.44, 0.74, 0, 0.12), (0.0, 1.03, 0.22, 0.26, 0.42, 0, -0.14),
           (-0.17, 1.04, -0.10, 0.36, 0.60, 125, -0.1), (-0.25, 1.03, -0.16, 0.2, 0.34, 125, 0.14),
           (0.17, 1.04, -0.10, 0.36, 0.52, 235, 0.12), (0.25, 1.03, -0.16, 0.2, 0.30, 235, -0.12)])
leaf_tuft(P, (0.50, 0.0, 0.42), 4, 0.34, 1)
flower(P, (0.36, 0.30, 0.3), 0.07, RED, turn=(-70, 30, 0))


# ------------------------------------------------------------------ the lantern on its bamboo

P = S.prop("lantern_pole", "1.0 wide x 2.7 m tall (the lamp 0.36 x 0.62 m)", at=(1.6, -1.1))
curve = bez((0, 0, 0), (0.0, 1.9, 0), (0.05, 2.75, 0), (0.74, 2.56, 0), 12)
P.add(tube(curve, list(np.linspace(0.055, 0.026, 12)), 8), bamboo(nodes=0.36, vertical=True), key="lpole")
for k, (a, h) in enumerate(((30, (0.2, 0.14, 0.17)), (150, (0.18, 0.12, 0.16)), (270, (0.19, 0.16, 0.16)))):
    field_stone(P, h, (0.2 * math.cos(math.radians(a)), 0.08, 0.2 * math.sin(math.radians(a))), 60 + k, yaw=a, moss=0.5 if k == 0 else 0)
tie(P, (0.0, 1.5, 0.0), 0.062, CL_RED, yaw=30, length=0.26, key="ltie")
HX, HY = 0.74, 2.56                                                                    # where the lamp hangs from
G = "lantern"
P.add(tube([(HX, HY + 0.02, 0), (HX, HY - 0.16, 0)], 0.012, 5), rope(STRAW, STRAW_D), key="lcord", group=G)
cap = lathe([(0.0, 0.0), (0.21, 0.0), (0.215, 0.03), (0.09, 0.11), (0.035, 0.13), (0.03, 0.17), (0.0, 0.175)], 6)
P.add(T(cap, at=(HX, HY - 0.31, 0)), wood(DARK, bands=[(cap["t"][2], cap["t"][3], "tri", OCHRE, None)]), key="lcap", group=G)
body = lathe([(0.0, 0.0), (0.13, 0.0), (0.185, 0.08), (0.185, 0.26), (0.15, 0.31), (0.0, 0.31)], 6)
tl = body["t"]


def lamp_paint(C):
    C.fill(DARK)
    for k in range(6):                                                                 # six panes of capiz in a wooden lattice
        u0, u1 = k / 6.0 + 0.018, (k + 1) / 6.0 - 0.018
        C.box(u0, tl[1] + 0.02, u1, tl[4] - 0.02, (255, 214, 120))
        C.box(u0 + 0.012, tl[2] - 0.02, u1 - 0.012, tl[3] + 0.02, (255, 240, 180))
        um = (u0 + u1) / 2
        C.box(um - 0.006, tl[1] + 0.02, um + 0.006, tl[4] - 0.02, DARK)
        for t in (tl[2], (tl[2] + tl[3]) / 2, tl[3]):
            C.box(u0, t - 0.008, u1, t + 0.008, DARK)
        C.poly([(um, (tl[2] + tl[3]) / 2 + 0.07), (um + 0.03, (tl[2] + tl[3]) / 2), (um, (tl[2] + tl[3]) / 2 - 0.07), (um - 0.03, (tl[2] + tl[3]) / 2)], ORANGE)
    C.band(0, tl[1], shade(DARK, 0.8))


P.add(T(body, at=(HX, HY - 0.62, 0)), lamp_paint, key="lbody", group=G, k=1.5)
for k in range(3):
    a = math.radians(120 * k + 30)
    P.add(tube([(HX + 0.08 * math.cos(a), HY - 0.62, 0.08 * math.sin(a)), (HX + 0.09 * math.cos(a), HY - 0.74, 0.09 * math.sin(a)), (HX + 0.10 * math.cos(a), HY - 0.86, 0.10 * math.sin(a))], [0.012, 0.022, 0.03], 5),
          lambda C: (C.fill(CL_RED), C.band(0.6, 0.7, CL_YELLOW), C.band(0.7, 1, CL_RED)), key="ltassel", group=G)


# ------------------------------------------------------------------ the standing torch

P = S.prop("torch", "0.5 wide x 1.75 m tall, the flame 0.45 more", at=(-2.0, -0.5))
pole = lathe([(0.075, 0.0), (0.08, 0.06), (0.052, 0.14), (0.05, 0.78), (0.068, 0.82), (0.068, 0.92), (0.05, 0.96), (0.05, 1.36)], 8)
tp = pole["t"]
P.add(pole, lambda C: (C.fill(DARK), C.grain(shade(DARK, 0.74), shade(DARK, 1.2), True, 5), C.motif(tp[4], tp[5], "eye", CREAM, n=4, bg=CL_BLACK, edge=OCHRE),
                       C.motif(tp[2], tp[2] + 0.06, "zig", OCHRE, n=6), C.vgrad(0.84, 1.05)), key="tpole")
for k, (a, h) in enumerate(((80, (0.17, 0.12, 0.15)), (200, (0.16, 0.11, 0.14)), (320, (0.15, 0.13, 0.14)))):
    field_stone(P, h, (0.17 * math.cos(math.radians(a)), 0.07, 0.17 * math.sin(math.radians(a))), 70 + k, yaw=a)
head = lathe([(0.05, 1.30), (0.075, 1.36), (0.15, 1.60), (0.185, 1.72), (0.19, 1.76), (0.16, 1.76), (0.12, 1.70), (0.0, 1.68)], 10)
th = head["t"]


def head_paint(C):
    C.fill(RATTAN_D)
    C.weave(RATTAN, STRAW, 0.04, th[1], th[3], lambda i, j, cols, rows: WOOD_D if j == rows // 2 else None)
    C.band(0, th[1], DARK)
    C.band(th[3], th[5], shade(RATTAN, 1.1))
    C.band(th[5], 1, (50, 42, 46))
    C.vgrad(0.84, 1.05)


P.add(head, head_paint, k=1.4)
coals(P, 0.15, 1.69, "tcoals")
tie(P, (0, 1.36, 0), 0.08, CL_RED, yaw=10, length=0.28, key="ttie")
flames(P, [(0.0, 1.72, 0.03, 0.26, 0.50, 0, 0.14), (0.0, 1.71, 0.10, 0.15, 0.28, 0, -0.14), (-0.03, 1.72, -0.04, 0.22, 0.40, 120, -0.1), (0.03, 1.72, -0.04, 0.2, 0.34, 240, 0.1)])


# ------------------------------------------------------------------ the bamboo water pipe

P = S.prop("water_pipe", "3.2 long x 1.5 m tall", at=(0.3, 0.9))


def bark(C):
    C.fill((128, 88, 62))
    C.grain((88, 58, 46), (168, 124, 88), True, 6)
    C.band(0.95, 1.0, CREAM); C.band(0.93, 0.95, WOOD_L)
    C.vgrad(0.84, 1.04)


def forked(x, h):
    P.add(tube([(x, 0, 0), (x + 0.02, h * 0.5, 0), (x, h, 0)], [0.085, 0.07, 0.065], 7), bark, key="trunk")
    for s in (-1, 1):
        P.add(tube([(x, h - 0.04, 0), (x, h + 0.14, s * 0.12), (x, h + 0.30, s * 0.17)], [0.06, 0.05, 0.042], 6), bark, key="prong")
    P.add(T(torus(0.078, 0.022, 0, 8, 4), at=(x, h - 0.07, 0)), rope(STRAW, STRAW_D), key="wlash")


PX0, PX1, PY0, PY1 = -1.5, 1.16, 1.42, 1.02
forked(-1.0, 1.26)
forked(0.72, 0.99)


def pipe_y(x):
    return PY0 + (PY1 - PY0) * (x - PX0) / (PX1 - PX0)


def pipe_ring(x, ro=0.088, ri=0.064):
    y = pipe_y(x)
    out = [(x, y + ro * math.sin(a), ro * math.cos(a)) for a in np.linspace(math.pi * 0.96, math.pi * 2.04, 7)]
    inn = [(x, y + ri * math.sin(a), ri * math.cos(a)) for a in np.linspace(math.pi * 2.04, math.pi * 0.96, 7)]
    return out + inn


def pipe_paint(C):
    C.fill(BAMBOO)
    C.box(0.10, 0, 0.20, 1, BAMBOO_L); C.box(0.32, 0, 0.5, 1, shade(BAMBOO, 0.82))
    C.box(0.5, 0, 1, 1, CREAM)                                                         # the split face and the hollow
    C.box(0.60, 0, 0.90, 1, WATER); C.box(0.72, 0, 0.78, 1, FOAM)
    for k in range(7):
        t = (k + 0.5) / 7.0
        C.box(0, t - 0.008, 0.5, t, BAMBOO_D); C.box(0, t, 0.5, t + 0.008, BAMBOO_L)
        C.box(0.5, t - 0.006, 1, t + 0.006, mix(CREAM, BAMBOO_D, 0.5))
        C.line([(0.62, t + 0.05), (0.7, t + 0.09)], FOAM, C.lw(0.01))


P.add(loft([pipe_ring(x) for x in np.linspace(PX0, PX1, 6)], nat=(0.5, 2.7)), pipe_paint, k=1.3)
# the catch basin on its stones
basin = lathe([(0.2, 0.0), (0.34, 0.06), (0.42, 0.24), (0.42, 0.32), (0.37, 0.34), (0.33, 0.28), (0.0, 0.27)], 12)
tn = basin["t"]


def basin_paint(C):
    C.fill(STONE)
    C.specks(shade(STONE, 0.86), 30, 0.02); C.specks(shade(STONE, 1.12), 20, 0.016)
    C.motif(tn[1] + 0.03, tn[2] - 0.02, "zig", STONE_L, n=10, bg=STONE_D)
    for k in range(6):
        C.glow(C.rnd.random(), 0.0, 0.1, tn[1] * 1.6, MOSS, 0.9)
    C.vgrad(0.82, 1.05)
    C.band(tn[3] - 0.02, tn[5], PALE)
    C.band(tn[5], 1, WATER_D)
    C.band(tn[6] - 0.002, 1, WATER)
    for k in range(3):
        C.band(tn[6] + (1 - tn[6]) * (0.25 + 0.25 * k), tn[6] + (1 - tn[6]) * (0.25 + 0.25 * k) + 0.012, FOAM)


BX = 1.42
P.add(T(basin, at=(BX, 0, 0)), basin_paint, k=1.2)
field_stone(P, (0.16, 0.11, 0.14), (BX + 0.42, 0.06, 0.2), 81, yaw=30, moss=0.5)
field_stone(P, (0.13, 0.09, 0.12), (BX - 0.1, 0.05, -0.45), 82, yaw=80)
leaf_tuft(P, (-1.0, 0.0, 0.14), 4, 0.36, 2, "tuft1")
leaf_tuft(P, (0.78, 0.0, -0.12), 3, 0.3, 3, "tuft2")
flower(P, (BX + 0.36, 0.17, 0.26), 0.07, GOLD, turn=(-70, 20, 0), heart=ORANGE)


def water_paint(C):
    C.fill(WATER)
    C.box(0.1, 0, 0.3, 1, FOAM); C.box(0.55, 0, 0.62, 1, FOAM); C.box(0.75, 0, 1, 1, WATER_D)
    C.band(0.9, 1, FOAM)


pour = bez((PX1 - 0.03, PY1 - 0.01, 0), (PX1 + 0.2, PY1 - 0.06, 0), (BX - 0.02, 0.8, 0), (BX, 0.26, 0), 8)
P.add(tube(pour, [0.045, 0.045, 0.042, 0.04, 0.04, 0.042, 0.048, 0.06], 7), water_paint, key="pour", group="water")
splash = lathe([(0.0, 0.0), (0.2, 0.0), (0.17, 0.035), (0.08, 0.06), (0.0, 0.07)], 9)
P.add(T(splash, at=(BX, 0.275, 0)), lambda C: (C.fill(FOAM), C.band(0, 0.3, WATER)), key="splash", group="water")
for k in range(3):
    a = math.radians(120 * k + 20)
    P.add(T(ball((0.03, 0.04, 0.03), 6, 4), at=(BX + 0.15 * math.cos(a), 0.40 + 0.04 * k, 0.15 * math.sin(a))), flat(FOAM, False), key="drop", group="water")


# ------------------------------------------------------------------ the stone trough

P = S.prop("water_trough", "1.7 long x 0.72 m tall", at=(-1.9, 1.0), yaw=8)
for sx in (-1, 1):
    P.add(T(cbox((0.17, 0.09, 0.30), 0.035), at=(sx * 0.52, 0.09, 0)), stone(STONE_D, bevel=0.035, moss=0.6), key="tfoot")
P.add(T(cbox((0.80, 0.21, 0.36), 0.05, taper=1.08), at=(0, 0.37, 0)),
      stone(STONE, bevel=0.05, cracks=3, moss=0.22, bands=[(0.30, 0.74, "tri2", OCHRE, STONE_D)], top_light=True), k=1.15)
P.add(T(cbox((0.66, 0.15, 0.24), 0.02), at=(0, 0.50, 0)), flat((70, 66, 68)), op="cut", key="tcut")


def pool_paint(C):
    C.fill(WATER)
    if C.panel == 1:
        C.oval(0.3, 0.5, 0.16, 0.3, None, FOAM, C.lw(0.012)); C.oval(0.3, 0.5, 0.08, 0.15, None, FOAM, C.lw(0.01))
        C.oval(0.72, 0.4, 0.1, 0.2, None, FOAM, C.lw(0.01))
        C.line([(0.5, 0.8), (0.62, 0.84)], FOAM, C.lw(0.012)); C.line([(0.82, 0.74), (0.9, 0.7)], FOAM, C.lw(0.012))
        C.glow(0.5, 0.0, 0.6, 0.3, WATER_D, 0.6)


P.add(T(cbox((0.67, 0.04, 0.25), 0.004), at=(0, 0.50, 0)), pool_paint, key="pool")
# a lily pad and its flower, a dipper, a frog on the rim
pad = lathe([(0.0, 0.0), (0.13, 0.0), (0.125, 0.014), (0.0, 0.018)], 9)
P.add(T(planar(pad, 1), at=(0.36, 0.538, 0.05)), lambda C: (C.oval(0.5, 0.5, 0.6, 0.6, LEAF), C.poly([(0.5, 0.5), (1.0, 0.62), (1.0, 0.38)], shade(LEAF, 0.7)),
                                                         [C.line([(0.5, 0.5), (0.5 + 0.45 * math.cos(a), 0.5 + 0.45 * math.sin(a))], LEAF_L, C.lw(0.008)) for a in (1.2, 2.2, 3.2, 4.2, 5.2)]), key="pad")
flower(P, (0.30, 0.585, 0.0), 0.10, (250, 170, 196), turn=(-90, 0, 0), heart=GOLD)
dip = lathe([(0.0, 0.0), (0.05, 0.012), (0.078, 0.05), (0.082, 0.09), (0.066, 0.09), (0.05, 0.04), (0.0, 0.03)], 9)
P.add(T(dip, at=(-0.5, 0.585, 0.27), turn=(20, 0, 0)), lambda C: (C.fill((120, 78, 50)), C.grain((84, 52, 38), (160, 112, 72), False), C.band(dip["t"][3], 1, CREAM)), key="dip")
P.add(tube([(-0.5, 0.63, 0.30), (-0.62, 0.70, 0.48), (-0.72, 0.74, 0.66)], 0.018, 6), bamboo(nodes=0.14), key="dipstick")
FROG = (110, 190, 80)
fx, fy, fz = 0.60, 0.575, 0.30
P.add(T(ball((0.115, 0.085, 0.12), 8, 5), at=(fx, fy + 0.07, fz)), lambda C: (C.fill(FROG), C.band(0, 0.42, (226, 232, 170)), C.specks(shade(FROG, 0.72), 12, 0.012, 0.5, 1.0)), key="frog")
for s in (-1, 1):
    P.add(T(ball((0.04, 0.04, 0.04), 6, 4), at=(fx + s * 0.06, fy + 0.155, fz + 0.06)), lambda C: (C.fill(WHITE), C.band(0.3, 0.75, CL_BLACK)), key="frogeye")
    P.add(T(ball((0.06, 0.045, 0.08), 6, 4), at=(fx + s * 0.11, fy + 0.03, fz - 0.03)), flat(shade(FROG, 0.85)), key="frogleg")
leaf_tuft(P, (-0.86, 0.0, 0.34), 4, 0.36, 5, "tuft3")
field_stone(P, (0.15, 0.10, 0.13), (0.95, 0.06, 0.3), 91, yaw=50, moss=0.5)

S.build()
