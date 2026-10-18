"""PAYO, the village's things (2026-10-12, second art pass): props, small buildings, people, animals, the catch bird.

Imported by tools/author_payo_art.py. Every function builds ONE piece at the origin (standing on z 0, facing local -Y)
into a mesh buffer; the main script bevels, bakes vertex colour, exports and places them.

House rules (Logs/payo/art/STUDY.md): chunky and organic, few thick members, tapered and a little crooked; detail in the
painted texture; nothing fiddly. Colour that is paint (worn pastels, cloth) is VERTEX TINT on a textured material, so
forty buildings can each have their own colour without forty materials: set `b.tint` before adding faces.
"""
import math
import random

from mathutils import Matrix, Vector

import author_payo_art_kit as P

T = P.T
WHITE = (1.0, 1.0, 1.0)
# worn paint over stone and wood: rose, mint, ochre, coral, turquoise, cream, indigo (the reference's pastels, cooled)
PASTEL = [(0.92, 0.66, 0.64), (0.62, 0.90, 0.76), (0.96, 0.78, 0.44), (0.96, 0.62, 0.50), (0.48, 0.86, 0.84), (0.94, 0.92, 0.80), (0.58, 0.62, 0.90)]
CLOTH = [(0.80, 0.22, 0.20), (0.16, 0.15, 0.18), (0.95, 0.92, 0.84), (0.86, 0.62, 0.20)]
SKINS = [(0.62, 0.42, 0.28), (0.54, 0.36, 0.24), (0.70, 0.48, 0.32)]


def tint(b, c):
    b.tint = tuple(c)


def jar(b, x, y, z, s=1.0, col=(0.62, 0.42, 0.34), lid=True):
    tint(b, col)
    b.lathe(T(x, y, z), [(0.16 * s, 0.0), (0.30 * s, 0.14 * s), (0.34 * s, 0.36 * s), (0.26 * s, 0.56 * s), (0.15 * s, 0.64 * s), (0.18 * s, 0.70 * s)], 9, "py_clay")
    if lid:
        tint(b, (0.86, 0.82, 0.70))
        b.lathe(T(x, y, z), [(0.19 * s, 0.70 * s), (0.20 * s, 0.75 * s), (0.05 * s, 0.80 * s)], 9, "py_weave", cap_top="py_weave")
    tint(b, WHITE)


def basket(b, x, y, z, s=1.0, full=None):
    tint(b, (0.90, 0.80, 0.56))
    b.lathe(T(x, y, z), [(0.20 * s, 0.0), (0.30 * s, 0.30 * s), (0.32 * s, 0.34 * s), (0.27 * s, 0.34 * s)], 9, "py_sawali", cap_bot="py_sawali")
    if full:
        tint(b, full)
        P.ellipsoid(b, T(x, y, z + 0.33 * s), 0.26 * s, 0.26 * s, 0.12 * s, "py_rice", n=8, rings=3)
    tint(b, WHITE)


def bundle(b, x, y, z, s=1.0, lean=0.0):
    """A sheaf of cut rice, tied."""
    tint(b, (0.92, 0.80, 0.44))
    b.lathe(T(x, y, z) @ Matrix.Rotation(lean, 4, "X"), [(0.10 * s, 0.0), (0.07 * s, 0.34 * s), (0.20 * s, 0.62 * s), (0.05 * s, 0.70 * s)], 7, "py_thatch")
    tint(b, CLOTH[0])
    b.lathe(T(x, y, z) @ Matrix.Rotation(lean, 4, "X"), [(0.085 * s, 0.30 * s), (0.085 * s, 0.38 * s)], 7, "py_weave")
    tint(b, WHITE)


def post(b, x, y, z0, z1, r=0.09, mat="py_timber", lean=(0.0, 0.0)):
    b.tube([(x, y, z0), (x + lean[0] * 0.5, y + lean[1] * 0.5, (z0 + z1) / 2), (x + lean[0], y + lean[1], z1)], r, mat, n=6, r1=r * 0.8)


# ------------------------------------------------------------------ props
def offering_table(b, r):
    for sx in (-0.6, 0.6):
        for sy in (-0.3, 0.3):
            post(b, sx, sy, 0, 0.7, 0.07)
    b.box(-0.8, 0.8, -0.42, 0.42, 0.7, 0.8, "py_timber")
    tint(b, CLOTH[0]); b.box(-0.5, 0.5, -0.44, 0.44, 0.8, 0.83, "py_weave"); tint(b, WHITE)
    jar(b, -0.5, 0.05, 0.83, 0.55)
    basket(b, 0.1, -0.05, 0.83, 0.7, full=(1, 1, 1))
    jar(b, 0.55, 0.1, 0.83, 0.42, col=(0.5, 0.4, 0.36))
    for k in range(3):
        bundle(b, -0.1 + k * 0.16, 0.26, 0.83, 0.45, lean=0.2)


def jars(b, r):
    jar(b, 0, 0, 0, 1.3)
    jar(b, 0.62, 0.2, 0, 0.9, col=(0.52, 0.38, 0.32))
    jar(b, -0.5, 0.35, 0, 0.75, col=(0.7, 0.5, 0.4), lid=False)
    jar(b, 0.2, -0.55, 0, 0.6)


def baskets(b, r):
    basket(b, 0, 0, 0, 1.3, full=(1, 1, 1))
    basket(b, 0.7, 0.25, 0, 1.0, full=(1.0, 0.86, 0.44))
    basket(b, -0.55, 0.4, 0, 0.8)
    tint(b, (0.9, 0.8, 0.56))                                # a winnowing tray leaning on them
    b.lathe(T(0.25, -0.7, 0.34) @ Matrix.Rotation(1.1, 4, "X"), [(0.0, 0.0), (0.44, 0.0), (0.48, 0.05), (0.44, 0.06), (0.0, 0.03)], 10, "py_sawali")
    tint(b, WHITE)


def mortar(b, r):
    """Lusong: a rice mortar cut from a log, two pestles leaning in it."""
    b.lathe(T(0, 0, 0), [(0.42, 0.0), (0.44, 0.08), (0.30, 0.2), (0.30, 0.42), (0.46, 0.56), (0.48, 0.74), (0.36, 0.74), (0.22, 0.5)], 9, "py_timber", smooth=False)
    for (a, lean) in ((0.6, 0.5), (2.4, 0.42)):
        b.tube([(0.05 * math.cos(a), 0.05 * math.sin(a), 0.5), (lean * math.cos(a), lean * math.sin(a), 1.9)], 0.07, "py_timber_dark", n=6, r1=0.1)
    basket(b, 0.8, -0.3, 0, 0.9, full=(1, 1, 1))


def gong_rack(b, r):
    """Gangsa: flat bronze gongs hung in a row."""
    for sx in (-1.3, 1.3):
        post(b, sx, 0, 0, 1.9, 0.09, lean=(sx * 0.04, 0))
    b.tube([(-1.5, 0, 1.78), (0, 0, 1.86), (1.5, 0, 1.78)], 0.07, "py_timber_dark", n=6)
    for k, x in enumerate((-0.86, -0.3, 0.28, 0.86)):
        s = 0.3 - k * 0.025
        tint(b, (0.86, 0.66, 0.30))
        b.lathe(T(x, 0, 1.78 - 0.18 - s) @ Matrix.Rotation(math.pi / 2, 4, "X"), [(0.0, -0.05), (s, -0.05), (s, 0.05), (s * 0.3, 0.07), (0.0, 0.09)], 10, "py_bronze", smooth=False)
        tint(b, WHITE)
        b.tube([(x, 0, 1.8), (x, 0, 1.78 - 0.18)], 0.015, "py_timber_dark", n=4)


def spear_rack(b, r):
    for sx in (-0.7, 0.7):
        post(b, sx, 0.1, 0, 1.3, 0.07)
    b.tube([(-0.85, 0.1, 1.2), (0.85, 0.1, 1.2)], 0.06, "py_timber_dark", n=6)
    for k in range(4):
        x = -0.55 + k * 0.36
        b.tube([(x, -0.25, 0.0), (x + 0.02, 0.1, 1.2), (x + 0.05, 0.42, 2.5)], 0.03, "py_timber", n=5)
        tint(b, (0.80, 0.84, 0.86))
        b.lathe(T(x + 0.05, 0.42, 2.5) @ Matrix.Rotation(-0.28, 4, "X"), [(0.03, 0.0), (0.08, 0.1), (0.0, 0.42)], 4, "py_bronze", smooth=False)
        tint(b, WHITE)
    for sx in (-1.0, 1.0):                                   # a long shield with three points above and two below, as the Kalinga and Bontoc carry
        M = T(sx * 1.05, -0.2, 0.0) @ Matrix.Rotation(-0.22, 4, "X")
        tint(b, CLOTH[1])
        b.block(M, 0.2, 0.035, 0.25, 1.15, "py_timber")
        for px in (-0.15, 0.0, 0.15):
            b.block(M @ T(px, 0, 0), 0.045, 0.03, 1.15, 1.5, "py_timber", taper=0.3)
        for px in (-0.12, 0.12):
            b.block(M @ T(px, 0, 0), 0.05, 0.03, 0.0, 0.25, "py_timber")
        tint(b, CLOTH[0]); b.block(M, 0.21, 0.04, 0.62, 0.74, "py_weave"); tint(b, WHITE)


def banner(b, r, col=0):
    post(b, 0, 0, 0, 4.6, 0.07, lean=(r.uniform(-0.2, 0.2), 0))
    b.tube([(0, 0, 4.3), (1.0, 0, 4.36)], 0.035, "py_timber_dark", n=5)
    tint(b, CLOTH[col])
    n = 5
    for k in range(n):                                       # the cloth in five strips that swing a little out of line
        z1, z0 = 4.3 - k * 0.52, 4.3 - (k + 1) * 0.52
        o0, o1 = 0.08 * math.sin(k * 1.3), 0.08 * math.sin((k + 1) * 1.3)
        for (pts, uv) in (([(0.08, o1, z0), (0.94, o1, z0), (0.94, o0, z1), (0.08, o0, z1)], [(0, k / n), (0.5, k / n), (0.5, (k + 1) / n), (0, (k + 1) / n)]),):
            b.quad(pts, "py_weave", uvs=uv)
            b.quad(list(reversed(pts)), "py_weave", uvs=list(reversed(uv)))
    tint(b, WHITE)


def hagabi(b, r):
    """The carved prestige bench: one long log, a carved head at each end, a low saddle."""
    b.box(-1.6, 1.6, -0.3, 0.3, 0.32, 0.56, "py_timber")
    for sx in (-1, 1):
        b.block(T(sx * 1.25, 0, 0), 0.26, 0.26, 0.0, 0.32, "py_timber_dark", taper=0.8)
        b.block(T(sx * 1.95, 0, 0), 0.34, 0.24, 0.30, 0.72, "py_timber", taper=0.55, skew=(sx * 0.2, 0))
        P.ellipsoid(b, T(sx * 2.25, 0, 0.72), 0.2, 0.16, 0.16, "py_timber_dark", n=7, rings=4)


def skull(b, r):
    """A carabao skull hung on a post: horns wide."""
    tint(b, (0.94, 0.92, 0.84))
    b.block(T(0, 0, 0), 0.14, 0.07, 0.0, 0.42, "py_block_edge", taper=1.7)
    for sx in (-1, 1):
        b.tube([(sx * 0.16, 0, 0.4), (sx * 0.5, 0, 0.46), (sx * 0.74, 0, 0.72), (sx * 0.62, 0, 0.98)], 0.06, "py_block_edge", n=6, r1=0.015)
    tint(b, WHITE)


def rice_rack(b, r):
    for sx in (-1.4, 0.0, 1.4):
        post(b, sx, 0, 0, 2.1, 0.08, lean=(r.uniform(-0.06, 0.06), 0))
    for z in (1.1, 1.95):
        b.tube([(-1.6, 0, z), (1.6, 0, z + 0.04)], 0.055, "py_bamboo", n=6)
        for k in range(8):
            x = -1.4 + k * 0.4
            tint(b, (0.92, 0.80, 0.44))
            b.lathe(T(x, 0, z - 0.62), [(0.16, 0.0), (0.1, 0.3), (0.06, 0.62)], 6, "py_thatch", squash=0.6)
            tint(b, WHITE)


def coop(b, r):
    b.lathe(T(0, 0, 0), [(0.62, 0.0), (0.56, 0.5), (0.3, 0.9), (0.08, 1.0)], 9, "py_sawali", smooth=False)
    b.quad([(-0.2, -0.6, 0.02), (0.2, -0.6, 0.02), (0.2, -0.57, 0.4), (-0.2, -0.57, 0.4)], "py_block_dark")


def bamboo_pipe(b, r):
    """A split bamboo flume on two trestles, water running out of its end."""
    for x in (-2.2, 1.6):
        for sy in (-0.3, 0.3):
            post(b, x, sy, 0, 1.5 - x * 0.06, 0.05, "py_bamboo", lean=(0, -sy * 0.8))
    b.tube([(-3.2, 0, 1.74), (0, 0, 1.56), (3.0, 0, 1.38)], 0.12, "py_bamboo", n=7)
    b.quad([(-3.1, -0.06, 1.80), (3.0, -0.06, 1.46), (3.0, 0.06, 1.46), (-3.1, 0.06, 1.80)], "py_water")
    b.block(T(3.05, 0, 0), 0.06, 0.03, 0.1, 1.4, "py_fall")


def scarecrow(b, r):
    post(b, 0, 0, 0, 2.0, 0.06, lean=(0.1, 0))
    b.tube([(-0.8, 0, 1.5), (0.9, 0, 1.56)], 0.045, "py_timber_dark", n=5)
    tint(b, CLOTH[r.randrange(4)])
    b.block(T(0.05, 0, 0), 0.34, 0.1, 0.95, 1.6, "py_weave", taper=0.8)
    tint(b, (0.9, 0.8, 0.56))
    b.lathe(T(0.1, 0, 0), [(0.5, 1.92), (0.02, 2.14)], 9, "py_sawali", cap_bot="py_sawali")
    P.ellipsoid(b, T(0.1, 0, 1.82), 0.16, 0.16, 0.16, "py_sawali", n=7, rings=4)
    tint(b, WHITE)


def stone_stair(b, r):
    """Steps from one paddy up to the next: 2.8 m in eight worn stones, climbing toward local +Y."""
    for k in range(8):
        w = 0.62 + r.uniform(-0.06, 0.06)
        b.block(T(r.uniform(-0.05, 0.05), -0.2 + k * 0.42, 0, r.uniform(-0.08, 0.08)), w, 0.3, -0.3, 0.35 * (k + 1), "py_block_side", top="py_block_top")


def cook_fire(b, r):
    for k in range(7):
        a = math.tau * k / 7
        P.ellipsoid(b, T(0.5 * math.cos(a), 0.5 * math.sin(a), 0.1), 0.2, 0.16, 0.13, "py_block_dark", n=6, rings=3)
    for a in (0.3, 2.4, 4.5):
        b.tube([(0.5 * math.cos(a), 0.5 * math.sin(a), 0.05), (0, 0, 0.16)], 0.06, "py_timber_dark", n=5)
    tint(b, (0.3, 0.3, 0.32))
    b.lathe(T(0, 0, 0.34), [(0.14, 0.0), (0.3, 0.12), (0.3, 0.32), (0.22, 0.36)], 9, "py_clay")
    tint(b, WHITE)
    for a in (0.0, 2.1, 4.2):
        b.tube([(0.62 * math.cos(a), 0.62 * math.sin(a), 0), (0.08 * math.cos(a), 0.08 * math.sin(a), 1.25)], 0.035, "py_timber_dark", n=5)


def lantern(b, r):
    """A woven lantern on a bent pole: its panels are py_window, lit at night."""
    b.tube([(0, 0, 0), (0.06, 0, 1.4), (0.3, 0, 2.3), (0.7, 0, 2.45)], 0.06, "py_bamboo", n=6, r1=0.035)
    b.tube([(0.7, 0, 2.45), (0.7, 0, 2.2)], 0.012, "py_timber_dark", n=4)
    b.lathe(T(0.7, 0, 1.78), [(0.08, 0.0), (0.19, 0.08), (0.21, 0.3), (0.12, 0.42), (0.04, 0.44)], 8, "py_window", smooth=False)
    b.lathe(T(0.7, 0, 1.78), [(0.215, 0.14), (0.215, 0.2)], 8, "py_timber_dark", smooth=False)


def firewood(b, r):
    for row in range(3):
        for k in range(5 - row):
            x = -0.5 + k * 0.26 + row * 0.13
            b.tube([(x, -0.5 + r.uniform(-0.05, 0.05), 0.12 + row * 0.22), (x, 0.5, 0.12 + row * 0.22)], 0.12, "py_timber", n=6)


def fallen_block(b, r):
    """A carved stone that has come off something, lying askew, half in the ground."""
    M = T(0, 0, 0.28, r.uniform(0, 3)) @ Matrix.Rotation(r.uniform(0.2, 0.5), 4, "X")
    b.block(M, 0.75, 0.5, -0.5, 0.5, "py_block_side", top="py_block_edge")
    P.band(b, -0.3, 0.3, -0.3, 0.3, 0.0, 0.0, "py_carve") if False else None
    b.block(T(1.2, 0.5, 0, 1.0), 0.4, 0.3, -0.1, 0.34, "py_block_side_dark", taper=0.8)
    P.ellipsoid(b, T(-0.3, -0.6, 0.02), 0.5, 0.4, 0.1, "py_moss", n=7, rings=3)


def leaning_slab(b, r):
    """A tall slab monolith tipped against nothing, cloth strapped round it, the kind that marks a boundary."""
    M = T(0, 0, 0) @ Matrix.Rotation(0.16, 4, "Y")
    b.block(M, 0.7, 0.28, -0.4, 3.6, "py_block_side", top="py_block_edge", taper=0.7, skew=(0.1, 0))
    tint(b, CLOTH[r.randrange(4)])
    b.block(M @ T(0.03, 0, 0), 0.68, 0.31, 1.7, 2.0, "py_weave", taper=0.96)
    tint(b, WHITE)
    b.block(T(1.1, 0.2, 0, 0.5), 0.45, 0.3, -0.2, 0.9, "py_block_side_dark", taper=0.75)


def rubble(b, r):
    for k in range(6):
        s = r.uniform(0.25, 0.6)
        b.block(T(r.uniform(-1, 1), r.uniform(-0.8, 0.8), 0, r.uniform(0, 3)) @ Matrix.Rotation(r.uniform(-0.3, 0.3), 4, "X"), s, s * r.uniform(0.6, 0.9), -0.1, s * r.uniform(0.6, 1.1),
                r.choice(("py_block_side", "py_block_side_dark", "py_block_edge")))
    P.ellipsoid(b, T(0.2, 0.1, 0.02), 0.9, 0.7, 0.08, "py_moss", n=7, rings=3)


def drum(b, r):
    tint(b, (0.8, 0.7, 0.6))
    b.lathe(T(0, 0, 0), [(0.2, 0.0), (0.24, 0.3), (0.3, 0.86), (0.31, 0.9), (0.0, 0.9)], 9, "py_timber", smooth=False)
    tint(b, CLOTH[0]); b.lathe(T(0, 0, 0), [(0.262, 0.5), (0.276, 0.62)], 9, "py_weave", smooth=False); tint(b, WHITE)


def spout_head(b, r):
    """A carved head for a wall: water runs out of its mouth. Its back is at local y 0, it looks along -Y."""
    b.block(T(0, -0.45, 0), 0.7, 0.45, -0.7, 0.7, "py_block_side", taper=0.85)
    b.block(T(0, -0.5, 0), 0.8, 0.5, 0.5, 0.9, "py_block_edge", taper=0.9)                 # the brow
    for sx in (-1, 1):
        b.lathe(T(sx * 0.32, -0.86, 0.2) @ Matrix.Rotation(math.pi / 2, 4, "X"), [(0.1, 0.0), (0.22, 0.0), (0.24, 0.1), (0.12, 0.12), (0.1, 0.0)], 9, "py_block_edge", smooth=False)
        b.lathe(T(sx * 0.32, -0.87, 0.2) @ Matrix.Rotation(math.pi / 2, 4, "X"), [(0.0, 0.0), (0.1, 0.0), (0.1, 0.05), (0.0, 0.05)], 8, "py_inlay", smooth=False)
        b.block(T(sx * 0.86, -0.3, 0), 0.14, 0.3, -0.3, 0.6, "py_block_side_dark", taper=0.6)   # ears
    b.block(T(0, -0.98, 0), 0.13, 0.14, -0.2, 0.34, "py_block_edge", taper=0.6)             # the nose
    b.box(-0.4, 0.4, -1.0, -0.86, -0.56, -0.3, "py_block_dark")                              # the mouth


def cloud_puff(b, r):
    """Mist standing at a fall's foot: three soft lobes."""
    for (x, y, z, s) in ((0, 0, 0.5, 1.0), (0.8, 0.2, 0.3, 0.7), (-0.7, -0.1, 0.35, 0.75), (0.2, 0.5, 0.8, 0.6)):
        P.ellipsoid(b, T(x, y, z), s, s * 0.9, s * 0.6, "py_cloud", n=8, rings=4)


def rope_bridge(b, r):
    """18 m of plank bridge sagging between two posts at each end, along local X."""
    L = 18.0
    for sx in (-1, 1):
        for sy in (-0.7, 0.7):
            post(b, sx * L / 2, sy, -0.3, 1.6, 0.11, "py_timber_dark", lean=(sx * 0.08, 0))
    n = 26
    sag = lambda t: -1.5 * math.sin(math.pi * t)
    for k in range(n):
        t = (k + 0.5) / n
        x = -L / 2 + L * t
        b.block(T(x, 0, sag(t), 0) @ Matrix.Rotation(-math.cos(math.pi * t) * 0.26, 4, "Y"), L / n * 0.42, 0.6, -0.04, 0.04, "py_timber")
    for sy in (-0.7, 0.7):
        b.tube([(-L / 2 + L * k / 12, sy, sag(k / 12) + 1.1 + 0.3 * abs(math.cos(math.pi * k / 12))) for k in range(13)], 0.035, "py_timber_dark", n=5)
        b.tube([(-L / 2 + L * k / 12, sy * 0.92, sag(k / 12) - 0.02) for k in range(13)], 0.03, "py_timber_dark", n=5)


def flame(b, r):
    """NIGHT: a flame as layered tongues, an outer body and a paler core."""
    for k in range(5):
        a = math.tau * k / 5 + 0.3
        P.limb(b, T(0, 0, 0), (0.1 * math.cos(a), 0.1 * math.sin(a), 0.0), (0.2 * math.cos(a + 0.9), 0.2 * math.sin(a + 0.9), 0.5 + 0.12 * (k % 2)), 0.13, 0.015, "py_fire", n=6)
    P.limb(b, T(0, 0, 0), (0, 0, -0.02), (0.05, 0.02, 0.86), 0.2, 0.01, "py_fire", n=7)
    P.limb(b, T(0, 0, 0), (0, 0, 0.0), (0.02, 0.0, 0.52), 0.11, 0.01, "py_fire_core", n=6)


# ------------------------------------------------------------------ small buildings, no two alike
def thatch_roof(b, M, hx, hy, z, height, courses=4, ridge=0.3, tint_=WHITE):
    for k in range(courses):
        t0, t1 = k / courses, (k + 1) / courses
        ax, ay = hx + (ridge - hx) * t0, hy + (ridge - hy) * t0
        bx = hx + (ridge - hx) * t1 - 0.08
        tint(b, tuple(c * (0.86 if k % 2 else 1.0) for c in tint_))
        v0 = len(b.v)
        b.block(M, ax, ay, z + height * t0, z + height * t1, "py_thatch", taper=max(0.05, bx / ax))
        by = hy + (ridge - hy) * t1 - 0.08
        Mi = M.inverted()
        for i in range(v0 + 4, v0 + 8):
            v = Mi @ Vector(b.v[i])
            b.v[i] = tuple(M @ Vector((v.x, by if v.y > 0 else -by, v.z)))
    tint(b, WHITE)


def stilts(b, s, n=4, h=1.9, r=0.18, spread=1.3):
    pts = [(-spread, -spread), (spread, -spread), (spread, spread), (-spread, spread)] if n == 4 else [(-spread * 1.6, -spread), (0, -spread), (spread * 1.6, -spread), (spread * 1.6, spread), (0, spread), (-spread * 1.6, spread)]
    for (x, y) in pts:
        b.lathe(T(x * s, y * s, 0), [(r * s, 0.0), (r * 0.85 * s, h * s)], 8, "py_timber_dark")
        b.lathe(T(x * s, y * s, 0), [(r * 0.85 * s, h * 0.9 * s), (0.6 * s, h * 0.94 * s), (0.6 * s, h * s)], 10, "py_timber", cap_top="py_timber", smooth=False)


def door(b, x0, x1, y, z0, z1):
    b.quad([(x0, y, z0), (x1, y, z0), (x1, y, z1), (x0, y, z1)], "py_window", uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])


def house_bale(b, r, s=1.0, paint=None):
    """The Ifugao house: four posts with rat guards, a box of adzed boards with a painted band, a steep roof, a ladder."""
    stilts(b, s)
    b.box(-1.9 * s, 1.9 * s, -1.9 * s, 1.9 * s, 1.9 * s, 2.08 * s, "py_timber_dark")
    tint(b, paint or WHITE)
    b.box(-1.75 * s, 1.75 * s, -1.75 * s, 1.75 * s, 2.08 * s, 3.3 * s, "py_timber")
    tint(b, WHITE)
    P.band(b, -1.75 * s, 1.75 * s, -1.75 * s, 1.75 * s, 2.5 * s, 2.95 * s, "py_weave", period=1.75 * s, proud=0.012)
    door(b, -0.45 * s, 0.45 * s, -1.765 * s, 2.12 * s, 3.2 * s)
    thatch_roof(b, T(0, 0, 0), 3.1 * s, 3.1 * s, 2.75 * s, 4.6 * s)
    b.box(-0.34 * s, 0.34 * s, -0.34 * s, 0.34 * s, 7.3 * s, 7.62 * s, "py_timber_dark")
    for sx in (-0.42, 0.42):
        b.tube([(sx * s, -1.8 * s, 2.1 * s), (sx * s, -2.9 * s, 0.0)], 0.055 * s, "py_timber_dark", n=5)
    for k in range(5):
        t = (k + 0.5) / 5
        b.tube([(-0.42 * s, (-1.8 - 1.1 * t) * s, 2.1 * s * (1 - t)), (0.42 * s, (-1.8 - 1.1 * t) * s, 2.1 * s * (1 - t))], 0.045 * s, "py_timber_dark", n=5)
    M = T(1.3 * s, -1.34 * s, 1.0 * s)
    b.xf = M
    skull(b, r)
    b.xf = Matrix.Identity(4)


def house_alang(b, r, s=0.82):
    """The granary: narrower, taller in the roof, no window, a carved figure by its door."""
    stilts(b, s, h=2.3, spread=1.0)
    b.box(-1.5 * s, 1.5 * s, -1.5 * s, 1.5 * s, 2.3 * s, 2.46 * s, "py_timber_dark")
    b.block(T(0, 0, 0), 1.3 * s, 1.3 * s, 2.46 * s, 3.5 * s, "py_timber", taper=1.12)
    door(b, -0.3 * s, 0.3 * s, -1.31 * s, 2.5 * s, 3.2 * s)
    thatch_roof(b, T(0, 0, 0), 2.5 * s, 2.5 * s, 3.1 * s, 5.4 * s, courses=5, ridge=0.2)


def house_long(b, r, paint=None):
    """A long house on six posts with a saddle roof and crossed horns at each gable."""
    stilts(b, 1.0, n=6, h=1.7)
    b.box(-3.2, 3.2, -1.8, 1.8, 1.7, 1.9, "py_timber_dark")
    tint(b, paint or WHITE); b.box(-3.0, 3.0, -1.6, 1.6, 1.9, 3.2, "py_timber"); tint(b, WHITE)
    P.band(b, -3.0, 3.0, -1.6, 1.6, 2.3, 2.8, "py_weave", period=2.0, proud=0.012)
    door(b, -0.5, 0.5, -1.615, 1.95, 3.1)
    door(b, 1.6, 2.4, -1.615, 2.4, 3.0)
    thatch_roof(b, T(0, 0, 0), 4.0, 2.7, 2.8, 3.3, courses=4, ridge=0.12)
    v = b.v
    b.box(-3.4, 3.4, -0.16, 0.16, 6.0, 6.22, "py_timber_dark")
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.tube([(sx * 3.3, 0, 5.9), (sx * 3.7, sy * 0.9, 7.1)], 0.1, "py_timber_dark", n=5, r1=0.03)
    for i in range(len(v)):
        pass


def shrine_tower(b, r, paint):
    """A tapering stone tower in three tiers, each a size down and a little turned, painted bands, a niche, a thatch cap."""
    z, hx = 0.0, 1.9
    for k in range(3):
        h = 2.6 - k * 0.4
        M = T(r.uniform(-0.08, 0.08), r.uniform(-0.08, 0.08), 0, r.uniform(-0.07, 0.07))
        tint(b, tuple(0.55 + 0.45 * c for c in paint) if k == 1 else WHITE)
        b.block(M, hx, hx, z, z + h, "py_block_side" if k != 1 else "py_block_paint", taper=0.84)
        tint(b, WHITE)
        b.block(M, hx * 0.84 + 0.14, hx * 0.84 + 0.14, z + h, z + h + 0.3, "py_block_edge", taper=0.96)
        if k == 0:
            b.box(-0.5, 0.5, -hx - 0.02, -hx + 0.5, 0.5, 1.9, "py_block_dark")              # the niche
            jar(b, 0.0, -hx + 0.1, 0.5, 0.6)
        z += h + 0.3
        hx = hx * 0.84 - 0.12
    P.band(b, -1.42, 1.42, -1.42, 1.42, 3.4, 4.3, "py_carve", period=5.68, proud=0.03)
    thatch_roof(b, T(0, 0, 0), hx + 0.7, hx + 0.7, z - 0.1, 2.6, courses=3, ridge=0.1)


def shrine_step(b, r, paint):
    """A small stepped pyramid with a painted stair up its front and a spirit house on top."""
    hx = 3.4
    z = 0.0
    for k in range(4):
        b.block(T(0, 0, 0), hx, hx, z, z + 0.9, "py_block_side" if k % 2 else "py_block_side_dark", top="py_block_top", taper=0.94)
        b.block(T(0, 0, 0), hx * 0.94 + 0.06, hx * 0.94 + 0.06, z + 0.9, z + 1.02, "py_block_edge")
        z += 1.02
        hx -= 0.62
    tint(b, tuple(0.5 + 0.5 * c for c in paint))
    for k in range(10):                                      # the stair: one painted ramp of steps
        b.box(-0.7, 0.7, -3.5 + k * 0.25, -3.5 + (k + 1) * 0.25 + 0.01, -0.02 - 0.001 * k, 0.408 * (k + 1), "py_block_paint")
    tint(b, WHITE)
    for sx in (-1, 1):
        b.block(T(sx * 0.95, -2.3, 0), 0.22, 1.2, 0.0, 2.2, "py_block_edge", taper=0.4, skew=(0, 0.8))
    b.xf = T(0, 0.2, z) @ Matrix.Scale(0.55, 4)
    house_alang(b, r, 0.82)
    b.xf = Matrix.Identity(4)


def granary_drum(b, r, paint):
    """A round stone granary on a ring of boulders, a wide conical thatch hat, a painted band."""
    for k in range(7):
        a = math.tau * k / 7
        P.ellipsoid(b, T(1.5 * math.cos(a), 1.5 * math.sin(a), 0.25), 0.55, 0.45, 0.35, "py_block_side_dark", n=7, rings=4)
    b.lathe(T(0, 0, 0), [(1.7, 0.4), (1.9, 1.4), (1.8, 2.8), (1.5, 3.2)], 10, "py_block_side", smooth=False)
    tint(b, tuple(0.5 + 0.5 * c for c in paint))
    b.lathe(T(0, 0, 0), [(1.93, 1.6), (1.9, 2.2)], 10, "py_block_paint", smooth=False)
    tint(b, WHITE)
    b.quad([(-0.45, -1.92, 0.6), (0.45, -1.92, 0.6), (0.45, -1.9, 1.9), (-0.45, -1.9, 1.9)], "py_window", uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    for k in range(4):
        t0, t1 = k / 4, (k + 1) / 4
        tint(b, (0.86, 0.86, 0.86) if k % 2 else WHITE)
        b.lathe(T(0, 0, 0), [(2.9 - 2.7 * t0, 2.9 + 3.0 * t0), (2.9 - 2.7 * t1 - 0.1, 2.9 + 3.0 * t1)], 10, "py_thatch", cap_top="py_thatch", smooth=False)
    tint(b, WHITE)


def slab_gate(b, r, paint):
    """Two leaning slab monoliths and a lintel, cloth strapped round them, a lantern hung under."""
    for sx in (-1, 1):
        M = T(sx * 1.7, 0, 0) @ Matrix.Rotation(sx * 0.1, 4, "Y")
        b.block(M, 0.6, 0.5, -0.4, 4.4 + sx * 0.3, "py_block_side", top="py_block_edge", taper=0.78, skew=(-sx * 0.1, 0))
        tint(b, CLOTH[0 if sx < 0 else 3])
        b.block(M @ T(-sx * 0.04, 0, 0), 0.6, 0.53, 2.2, 2.6, "py_weave", taper=0.95)
        tint(b, WHITE)
    b.block(T(0, 0, 0) @ Matrix.Rotation(0.04, 4, "Y"), 2.7, 0.55, 4.3, 5.0, "py_block_side_dark", top="py_block_edge", taper=0.9)
    tint(b, tuple(0.5 + 0.5 * c for c in paint))
    b.block(T(0, -0.02, 0), 0.7, 0.58, 4.36, 4.94, "py_block_paint")
    tint(b, WHITE)
    b.tube([(0, 0, 4.3), (0, 0, 3.7)], 0.02, "py_timber_dark", n=4)
    b.lathe(T(0, 0, 3.3), [(0.08, 0.0), (0.2, 0.08), (0.22, 0.3), (0.12, 0.4)], 8, "py_window", smooth=False)


def watch_hut(b, r, paint):
    """A field hut on tall crooked legs, where the birds are watched from: a platform, a lean roof."""
    for (x, y) in ((-0.9, -0.9), (0.9, -0.8), (0.95, 0.9), (-0.85, 0.85)):
        post(b, x, y, 0, 3.0, 0.08, "py_bamboo", lean=(-x * 0.12, -y * 0.1))
    b.box(-1.1, 1.1, -1.1, 1.1, 2.9, 3.02, "py_timber")
    thatch_roof(b, T(0, 0.2, 0) @ Matrix.Rotation(0.12, 4, "X"), 1.6, 1.6, 4.2, 1.3, courses=3, ridge=0.15)
    for (x, y) in ((-0.9, -0.9), (0.9, -0.9), (0.9, 0.9), (-0.9, 0.9)):
        post(b, x, y, 3.0, 4.3, 0.05, "py_bamboo")
    b.tube([(0.2, -1.2, 0.0), (0.2, -1.05, 2.9)], 0.05, "py_bamboo", n=5)


# ------------------------------------------------------------------ people and animals, in the cast's manner (box heads)
def person(b, r, pose="stand", s=1.0, skin=None, wrap=None, hat=False, carry=None, child=False):
    """A villager, 1.6 m (a child 1.05): the cast's soft box head, block body, a woven wrap. Faces local -Y."""
    skin = skin or r.choice(SKINS)
    wrap = wrap or r.choice(CLOTH[:2])
    k = (0.66 if child else 1.0) * s
    lean = {"stand": 0.0, "carry": 0.0, "plant": 1.0, "sit": 0.0, "wave": 0.0, "watch": 0.0}[pose]
    hip = 0.42 * k if pose == "sit" else 0.78 * k
    M = T(0, 0, 0)
    tint(b, skin)
    if pose == "sit":
        for sx in (-0.1, 0.1):
            b.block(M @ T(sx * k, -0.22 * k, 0), 0.07 * k, 0.24 * k, hip - 0.08 * k, hip + 0.08 * k, "py_skin")
            b.block(M @ T(sx * k, -0.44 * k, 0), 0.065 * k, 0.07 * k, 0.0, hip, "py_skin")
    else:
        for sx in (-0.1, 0.1):
            b.block(M @ T(sx * k, 0.04 * k * lean, 0), 0.075 * k, 0.085 * k, 0.0, hip, "py_skin", taper=1.1)
    B = M @ T(0, 0, hip) @ Matrix.Rotation(-lean * 1.05, 4, "X")
    tint(b, wrap)
    b.block(B, 0.2 * k, 0.13 * k, -0.14 * k, 0.2 * k, "py_weave", taper=0.9)                  # the wrap at the hips
    tint(b, skin if not child else CLOTH[2])
    b.block(B, 0.19 * k, 0.12 * k, 0.2 * k, 0.56 * k, "py_skin" if not child else "py_weave", taper=1.08)
    tint(b, CLOTH[0])
    b.block(B, 0.2 * k, 0.125 * k, 0.4 * k, 0.5 * k, "py_weave")                              # a band across the chest
    tint(b, skin)
    hz = 0.60 * k
    b.block(B @ T(0, -0.01 * k, 0), 0.17 * k * (1.2 if child else 1.0), 0.16 * k * (1.2 if child else 1.0), hz, hz + 0.31 * k * (1.15 if child else 1.0), "py_skin", taper=0.94)
    tint(b, (0.12, 0.1, 0.1))
    b.block(B @ T(0, 0.03 * k, 0), 0.18 * k * (1.2 if child else 1.0), 0.16 * k * (1.2 if child else 1.0), hz + 0.2 * k, hz + 0.37 * k * (1.12 if child else 1.0), "py_skin", taper=0.9)   # hair
    for sx in (-1, 1):                                       # the cast's eyes: two dark blocks
        b.block(B @ T(sx * 0.07 * k, -0.165 * k * (1.2 if child else 1.0), 0), 0.022 * k, 0.012 * k, hz + 0.12 * k, hz + 0.19 * k, "py_skin")
    tint(b, skin)
    arms = {"stand": ((0.0, -0.1), (0.0, -0.1)), "watch": ((0.0, -0.1), (0.9, -0.2)), "carry": ((2.6, 0.0), (2.6, 0.0)), "plant": ((0.2, -0.3), (0.2, -0.3)),
            "sit": ((0.5, -0.3), (0.5, -0.3)), "wave": ((0.0, -0.1), (2.9, 0.3))}[pose]
    for sx, (up, fwd) in zip((-1, 1), arms):
        sh = B @ Vector((sx * 0.23 * k, 0, 0.5 * k))
        d = (B.to_3x3() @ Vector((sx * 0.12 * math.cos(up) + sx * 0.0, fwd, -math.cos(up) * 0.9 + 0.0))).normalized() if up < 1.5 else (B.to_3x3() @ Vector((sx * 0.25, fwd, 1.0))).normalized()
        b.tube([tuple(sh), tuple(sh + d * 0.44 * k)], 0.055 * k, "py_skin", n=5, r1=0.045 * k)
    tint(b, WHITE)
    if hat:
        tint(b, (0.9, 0.8, 0.56))
        b.lathe(B @ T(0, 0, hz + 0.34 * k), [(0.36 * k, 0.0), (0.03 * k, 0.2 * k)], 9, "py_sawali", cap_bot="py_sawali", smooth=False)
        tint(b, WHITE)
    if pose == "carry":
        b.xf = B @ T(0, -0.02 * k, hz + 0.37 * k) @ Matrix.Scale(0.9 * k, 4)
        basket(b, 0, 0, 0, 1.0, full=(1, 1, 1))
        b.xf = Matrix.Identity(4)


def carabao(b, r, wallow=False):
    z = -0.45 if wallow else 0.0
    tint(b, (0.26, 0.26, 0.29))
    b.block(T(0, 0, z), 0.42, 0.85, 0.55, 1.25, "py_hide", taper=0.86)
    b.block(T(0, -1.05, z), 0.26, 0.34, 0.75, 1.3, "py_hide", taper=0.8, skew=(0, -0.1))
    b.block(T(0, -1.42, z), 0.17, 0.16, 0.74, 1.02, "py_hide", taper=0.9)
    if not wallow:
        for sx in (-0.26, 0.26):
            for sy in (-0.6, 0.6):
                b.block(T(sx, sy, 0), 0.1, 0.12, 0.0, 0.6, "py_hide", taper=1.2)
    b.tube([(0, 0.85, z + 1.1), (0.05, 1.0, z + 0.7), (0.0, 1.02, z + 0.4)], 0.035, "py_hide", n=5)
    tint(b, (0.90, 0.88, 0.80))
    for sx in (-1, 1):
        b.tube([(sx * 0.2, -1.0, z + 1.28), (sx * 0.62, -0.86, z + 1.4), (sx * 0.86, -0.5, z + 1.62), (sx * 0.74, -0.2, z + 1.74)], 0.075, "py_hide", n=6, r1=0.015)
    tint(b, WHITE)
    if wallow:
        P.ellipsoid(b, T(0, -0.1, 0.03), 1.3, 1.6, 0.05, "py_water", n=10, rings=3)


def chicken(b, r, col=(0.86, 0.5, 0.26)):
    tint(b, col)
    b.block(T(0, 0, 0), 0.1, 0.15, 0.14, 0.34, "py_hide", taper=0.8)
    b.block(T(0, -0.14, 0), 0.06, 0.06, 0.3, 0.46, "py_hide", taper=0.9)
    b.block(T(0, 0.16, 0), 0.06, 0.05, 0.28, 0.48, "py_hide", taper=0.5, skew=(0, 0.06))
    tint(b, (0.86, 0.2, 0.18)); b.block(T(0, -0.14, 0), 0.02, 0.05, 0.46, 0.52, "py_hide")
    tint(b, (0.95, 0.75, 0.3)); b.block(T(0, -0.22, 0), 0.02, 0.035, 0.36, 0.4, "py_hide")
    for sx in (-0.04, 0.04):
        b.block(T(sx, 0, 0), 0.012, 0.012, 0.0, 0.14, "py_hide")
    tint(b, WHITE)


def dog(b, r):
    tint(b, (0.80, 0.62, 0.40))
    b.block(T(0, 0, 0), 0.13, 0.32, 0.3, 0.52, "py_hide", taper=0.9)
    b.block(T(0, -0.4, 0), 0.11, 0.12, 0.42, 0.66, "py_hide", taper=0.9)
    b.block(T(0, -0.55, 0), 0.06, 0.08, 0.44, 0.54, "py_hide")
    for sx in (-0.08, 0.08):
        b.block(T(sx, -0.38, 0), 0.03, 0.03, 0.64, 0.76, "py_hide", taper=0.4)
        for sy in (-0.24, 0.24):
            b.block(T(sx, sy, 0), 0.04, 0.045, 0.0, 0.32, "py_hide")
    b.tube([(0, 0.32, 0.5), (0, 0.46, 0.62), (0, 0.44, 0.78)], 0.03, "py_hide", n=5, r1=0.015)
    tint(b, WHITE)


def egret(b, r):
    tint(b, (0.97, 0.97, 0.94))
    P.ellipsoid(b, T(0, 0.05, 0.62), 0.11, 0.2, 0.12, "py_hide", n=7, rings=4)
    b.tube([(0, -0.08, 0.68), (0, -0.16, 0.9), (0, -0.08, 1.06), (0, -0.16, 1.16)], 0.035, "py_hide", n=5)
    P.ellipsoid(b, T(0, -0.18, 1.17), 0.045, 0.07, 0.045, "py_hide", n=6, rings=3)
    tint(b, (0.95, 0.78, 0.28)); b.tube([(0, -0.22, 1.17), (0, -0.4, 1.14)], 0.018, "py_hide", n=4, r1=0.004)
    tint(b, (0.2, 0.2, 0.2))
    for sx in (-0.04, 0.04):
        b.tube([(sx, 0.05, 0.54), (sx, 0.08, 0.0)], 0.012, "py_hide", n=4)
    tint(b, WHITE)


def bird_body(b, s, body=(0.55, 0.36, 0.22), head=(0.95, 0.93, 0.86), beak=(0.96, 0.76, 0.24)):
    """A hawk or eagle's body, flying along local -Y, belly at z 0: chunky, a pale head, a hooked beak, talons hanging."""
    tint(b, body)
    b.block(T(0, 0, 0), 0.34 * s, 0.8 * s, 0.0, 0.5 * s, "py_feather", taper=0.8)
    b.block(T(0, 0.95 * s, 0.1 * s), 0.3 * s, 0.4 * s, 0.0, 0.12 * s, "py_feather", taper=1.5)              # the tail, a fan
    tint(b, head)
    b.block(T(0, -0.86 * s, 0.14 * s), 0.24 * s, 0.26 * s, 0.0, 0.44 * s, "py_feather", taper=0.9)
    tint(b, beak)
    b.block(T(0, -1.16 * s, 0.2 * s), 0.08 * s, 0.14 * s, 0.0, 0.16 * s, "py_feather", taper=0.5, skew=(0, -0.06 * s))
    tint(b, (0.1, 0.1, 0.12))
    for sx in (-1, 1):
        b.block(T(sx * 0.2 * s, -0.96 * s, 0.38 * s), 0.035 * s, 0.05 * s, 0.0, 0.08 * s, "py_feather")
    tint(b, beak)
    for sx in (-1, 1):                                       # the talons: open, to carry
        b.tube([(sx * 0.2 * s, 0.1 * s, 0.02 * s), (sx * 0.22 * s, 0.05 * s, -0.4 * s)], 0.07 * s, "py_feather", n=5)
        for a in (-0.5, 0.0, 0.5):
            b.tube([(sx * 0.22 * s, 0.05 * s, -0.4 * s), (sx * 0.22 * s + math.sin(a) * 0.2 * s, -0.16 * s, -0.62 * s)], 0.04 * s, "py_feather", n=4, r1=0.01 * s)
    tint(b, WHITE)


def bird_wing(b, s, side, body=(0.55, 0.36, 0.22), tip=(0.20, 0.16, 0.14), bar=(0.92, 0.88, 0.76)):
    """One wing, its shoulder at the origin, spread along local X (side +1 or -1): an arm, three rows of fat feathers."""
    tint(b, body)
    b.block(T(side * 0.9 * s, 0, 0), 0.9 * s, 0.34 * s, -0.06 * s, 0.1 * s, "py_feather", taper=0.9)
    for k in range(7):
        t = k / 6
        x = side * (0.25 + 1.75 * t) * s
        ln = (0.9 - 0.25 * abs(t - 0.5)) * s
        tint(b, bar if k % 2 else body)
        b.block(T(x, (0.3 + ln / 2) * s * 1.0, -0.02 * s, -side * 0.12 * t), 0.15 * s, ln / 2, 0.0, 0.05 * s, "py_feather", taper=0.7)
    for k in range(5):                                       # the long dark primaries at the tip
        a = side * (0.25 + k * 0.22)
        tint(b, tip)
        b.block(T(side * 1.95 * s, 0.1 * s, -0.02 * s, -a) @ T(0, 0.75 * s, 0), 0.11 * s, 0.75 * s, 0.0, 0.045 * s, "py_feather", taper=0.5)
    tint(b, WHITE)


PROPS = {
    "prop_offering_table": offering_table, "prop_jars": jars, "prop_baskets": baskets, "prop_mortar": mortar, "prop_gong_rack": gong_rack, "prop_spear_rack": spear_rack,
    "prop_banner_red": lambda b, r: banner(b, r, 0), "prop_banner_black": lambda b, r: banner(b, r, 1), "prop_banner_white": lambda b, r: banner(b, r, 2),
    "prop_hagabi": hagabi, "prop_skull": skull, "prop_rice_rack": rice_rack, "prop_coop": coop, "prop_bamboo_pipe": bamboo_pipe, "prop_scarecrow": scarecrow,
    "prop_stone_stair": stone_stair, "prop_cook_fire": cook_fire, "prop_lantern": lantern, "prop_firewood": firewood, "prop_fallen_block": fallen_block,
    "prop_leaning_slab": leaning_slab, "prop_rubble": rubble, "prop_drum": drum, "prop_spout_head": spout_head, "prop_mist_puff": cloud_puff, "prop_rope_bridge": rope_bridge,
    "fire_flame": flame,
}
BUILDINGS = {
    "house_bale": lambda b, r: house_bale(b, r, 1.0), "house_bale_mint": lambda b, r: house_bale(b, r, 1.1, PASTEL[1]), "house_bale_rose": lambda b, r: house_bale(b, r, 0.92, PASTEL[0]),
    "house_alang": house_alang, "house_long": lambda b, r: house_long(b, r, PASTEL[5]), "house_long_coral": lambda b, r: house_long(b, r, PASTEL[3]),
    "shrine_tower_ochre": lambda b, r: shrine_tower(b, r, PASTEL[2]), "shrine_tower_turq": lambda b, r: shrine_tower(b, r, PASTEL[4]),
    "shrine_step_coral": lambda b, r: shrine_step(b, r, PASTEL[3]), "shrine_step_mint": lambda b, r: shrine_step(b, r, PASTEL[1]),
    "granary_drum_rose": lambda b, r: granary_drum(b, r, PASTEL[0]), "granary_drum_ochre": lambda b, r: granary_drum(b, r, PASTEL[2]),
    "slab_gate": lambda b, r: slab_gate(b, r, PASTEL[4]), "watch_hut": lambda b, r: watch_hut(b, r, PASTEL[2]),
}
LIFE = {
    "villager_stand": lambda b, r: person(b, r, "stand"), "villager_watch": lambda b, r: person(b, r, "watch", hat=True), "villager_carry": lambda b, r: person(b, r, "carry"),
    "villager_plant": lambda b, r: person(b, r, "plant", hat=True), "villager_sit": lambda b, r: person(b, r, "sit"), "child_wave": lambda b, r: person(b, r, "wave", child=True),
    "carabao_stand": lambda b, r: carabao(b, r), "carabao_wallow": lambda b, r: carabao(b, r, wallow=True),
    "chicken": chicken, "chicken_white": lambda b, r: chicken(b, r, (0.95, 0.94, 0.9)), "dog": dog, "egret": egret,
}
