"""PAYO props, SET A: the offering and the feast.   py -3 tools/payo_pieces/prop_set_a.py

An offering table laid with rice cakes, betel and flowers; three tapuy (rice wine) jars with cloth-tied lids, one open
with a ladle; a stack of woven baskets; a winnowing tray (bilao) of rice; a wooden mortar and two pestles; a clay
cooking pot on three hearth stones (its fire a SEPARATE object, hidden by day or lit at night).
Sizes are real; the kit writes them 15 percent larger.
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from prop_kit import *                                 # noqa: E402,F401,F403
from prop_bits import *                                # noqa: E402,F401,F403

S = PropSet("a", ppm=330)
LEAF_PROF = ((1, 0), (0.7, 0.85), (0, 1))


def cloth_lid(P, r, y, col, stripe, key, tie=STRAW):
    """A jar's cloth cover: a dome pulled over the mouth, tied under the lip with cord, its skirt standing out in folds."""
    prof = [(r * 1.34, y - r * 0.42), (r * 1.12, y - r * 0.16), (r * 1.02, y), (r * 1.12, y + r * 0.2), (r * 0.94, y + r * 0.48), (r * 0.5, y + r * 0.62), (0.0, y + r * 0.66)]
    m = lathe(prof, 16)
    lo = y - r * 0.05

    def fold(x, yy, z):
        if yy < lo:
            a = math.atan2(z, x)
            k = 1.0 + 0.16 * math.sin(a * 6) * min(1.0, (lo - yy) / (r * 0.2))
            return x * k, yy + 0.02 * r * math.cos(a * 6), z * k
        return x, yy, z
    m = warp(m, fold)
    t = m["t"]

    def paint(C):
        C.fill(col)
        for k in range(8):
            C.box(k / 8.0 + 0.02, 0, k / 8.0 + 0.06, 1, stripe)
        C.band(0, t[0] + 0.03, shade(col, 0.7))
        C.band(t[4], 1, shade(col, 1.12))
        C.vgrad(0.86, 1.04)
    P.add(m, paint, key=key)
    P.add(torus(r * 1.06, r * 0.085, y, 14, 5), rope(tie, STRAW_D), key=key + "tie")
    P.add(T(ball((r * 0.13, r * 0.11, r * 0.13), 8, 5), at=(0, y, r * 1.14)), flat(tie), key=key + "knot")
    for dx in (-1, 1):
        P.add(tube([(0, y, r * 1.14), (dx * r * 0.2, y - r * 0.2, r * 1.3), (dx * r * 0.26, y - r * 0.5, r * 1.36)], r * 0.06, 6), rope(tie, STRAW_D), key=key + "tie")


def jar_paint(t, body, glaze, accent, i_foot, i_belly, i_shoulder, kind="zig", drips=9, wine=None, i_lip=None):
    def fn(C):
        C.fill(body)
        C.motif(t[i_belly] - 0.07, t[i_belly] + 0.05, kind, accent, bg=shade(body, 0.72), edge=accent)
        C.drips(t[i_shoulder], 0.15, glaze, drips)
        C.band(0, t[i_foot], CLAY)
        C.box(0, t[i_foot] - 0.012, 1, t[i_foot], shade(body, 0.6))
        C.vgrad(0.78, 1.06)
        C.shine(0.20, t[i_belly] + 0.075, t[i_shoulder] - 0.035, mix(body, WHITE, 0.38), 0.035)
        if wine is not None:
            C.band(t[i_lip], 1, shade(body, 0.5)); C.band(t[-1] - 0.001, 1, wine)
            C.band(t[i_lip] - 0.03, t[i_lip] + 0.012, mix(glaze, WHITE, 0.4))
    return fn


def lugs(P, r, y, count, col, key):
    for k in range(count):
        a = 2 * math.pi * (k + 0.5) / count
        c, s = math.cos(a), math.sin(a)
        P.add(tube(bez((c * r * 0.9, y - 0.05, s * r * 0.9), (c * (r + 0.07), y - 0.045, s * (r + 0.07)), (c * (r + 0.07), y + 0.045, s * (r + 0.07)), (c * r * 0.86, y + 0.05, s * r * 0.86), 6), 0.024, 6), flat(col), key=key)


# ------------------------------------------------------------------ the offering table

P = S.prop("offering_table", "1.15 x 0.52 x 0.66 m", at=(0.0, -0.9))
top_paint = wood(WOOD, bevel=0.03, bands=[(0.2, 0.8, "tri2", OCHRE, WOOD_D)], top_plain=True)
P.add(T(cbox((0.575, 0.045, 0.33), 0.026), at=(0, 0.375, 0)), top_paint)
apron = wood(WOOD_R, bevel=0.016, bands=[(0.14, 0.86, "eye", CREAM, WOOD_D)])
P.add(T(cbox((0.50, 0.05, 0.26), 0.014), at=(0, 0.285, 0)), apron)
for sx in (-1, 1):
    for sz in (-1, 1):
        leg = lathe([(0.062, 0.0), (0.082, 0.02), (0.088, 0.06), (0.058, 0.11), (0.074, 0.17), (0.09, 0.22), (0.08, 0.27)], 8)
        tl = leg["t"]
        P.add(T(leg, at=(sx * 0.44, 0, sz * 0.22)), wood(WOOD_D, bands=[(tl[4], tl[5] + 0.06, "dot", OCHRE, None), (tl[0], tl[2], None, None, shade(WOOD_D, 0.8))]), key="leg")


def runner_fn(u, v):
    s = (v - 0.5) * 1.06                                                   # across the table, front to back
    x = (u - 0.5) * 0.46
    edge = 0.336
    if abs(s) <= edge:
        return (x, 0.424 + 0.004 * math.sin(u * 9), s)
    hang = abs(s) - edge
    ripple = 0.022 * math.sin(u * math.pi * 5 + (1.0 if s > 0 else 2.2)) * min(1.0, hang / 0.06)
    return (x * (1.0 + 0.05 * hang / 0.2), 0.424 - hang, math.copysign(edge + 0.012 + 0.04 * min(1.0, hang / 0.2) + ripple, s))


def runner_paint(C):
    C.fill(CL_RED)
    for u in (0.08, 0.92):
        C.box(u - 0.05, 0, u + 0.05, 1, CL_BLACK)
    for u in (0.16, 0.84):
        C.box(u - 0.012, 0, u + 0.012, 1, CL_YELLOW)
    C.motif(0.30, 0.70, "eye", CL_WHITE, n=3, bg=CL_BLACK, edge=CL_RED)
    C.band(0.285, 0.30, CL_YELLOW); C.band(0.70, 0.715, CL_YELLOW)
    cordillera(C, 0.02, 0.21); cordillera(C, 0.79, 0.98)
    for k in range(14):                                                    # the fringe
        C.box(k / 14.0 + 0.02, 0.0, k / 14.0 + 0.05, 0.02, CL_WHITE); C.box(k / 14.0 + 0.02, 0.98, k / 14.0 + 0.05, 1.0, CL_WHITE)


P.add(sheet(runner_fn, 5, 14, 0.014, (0.46, 1.06)), runner_paint, k=1.3)
# the rice cakes on their banana leaf
P.add(T(pillow(leaf_outline(0.46, 0.25), 0.016, LEAF_PROF), at=(-0.31, 0.428, 0.02), turn=(-90, 20, 0)), leaf_paint(), key="bleaf")


def cake_paint(tc):
    def fn(C):
        C.fill(WHITE)
        C.specks(CREAM, 40, 0.004, 0.0, tc[2])
        C.drips(tc[2] - 0.02, 0.12, (150, 86, 44), 7)
        C.specks((226, 160, 80), 26, 0.006, tc[2] + 0.05, 1.0)
        C.band(0, tc[0], shade(CREAM, 0.8))
    return fn


cake = lathe([(0.062, 0.0), (0.074, 0.016), (0.074, 0.046), (0.058, 0.062)], 10)
for cx, cz, cy, turn in ((-0.40, 0.05, 0.436, 0), (-0.25, 0.09, 0.436, 40), (-0.31, 0.03, 0.494, 20)):
    P.add(T(cake, at=(cx, cy, cz), turn=(0, turn, 0)), cake_paint(cake["t"]), key="cake")
# the betel bowl: leaves and areca nuts
bowl = lathe([(0.055, 0.0), (0.105, 0.03), (0.125, 0.085), (0.105, 0.085), (0.08, 0.05), (0.0, 0.045)], 12)
tb = bowl["t"]


def bowl_paint(C):
    C.fill(WOOD_D)
    C.motif(tb[1], tb[2] - 0.03, "tri", OCHRE, bg=None)
    C.band(tb[2] - 0.03, tb[3] + 0.02, mix(WOOD_D, CREAM, 0.5))
    C.band(tb[3] + 0.02, 1, shade(WOOD_D, 0.7))


P.add(T(bowl, at=(0.34, 0.42, 0.04)), bowl_paint)
for k, (dx, dz, col) in enumerate(((0.03, 0.02, ORANGE), (-0.04, 0.0, OCHRE), (0.0, -0.04, ORANGE), (0.0, 0.01, (150, 190, 70)))):
    P.add(T(ball((0.036, 0.042, 0.036), 6, 4), at=(0.34 + dx, 0.50 + (0.03 if k == 3 else 0), 0.04 + dz)), flat(col), key="nut%d" % k)
for k, yaw in enumerate((30, 150, 265)):
    P.add(T(pillow(leaf_outline(0.17, 0.10), 0.014, LEAF_PROF), at=(0.34 + 0.115 * math.cos(math.radians(yaw)), 0.505, 0.04 - 0.115 * math.sin(math.radians(yaw))), turn=(-90, 0, 0)), leaf_paint(), key="bleaf2")
    P.pieces[-1]["mesh"] = T(pillow(leaf_outline(0.17, 0.10), 0.014, LEAF_PROF), turn=(-90 + 24, 0, 0))
    P.pieces[-1]["mesh"] = T(P.pieces[-1]["mesh"], turn=(0, yaw + 90, 0), at=(0.34 + 0.105 * math.cos(math.radians(yaw)), 0.512, 0.04 - 0.105 * math.sin(math.radians(yaw))))
flower(P, (0.02, 0.447, 0.20), 0.085, RED)
flower(P, (0.14, 0.447, -0.19), 0.07, GOLD, heart=ORANGE)
flower(P, (-0.02, 0.447, -0.03), 0.06, WHITE, heart=GOLD)


# ------------------------------------------------------------------ the tapuy jars

P = S.prop("tapuy_jar_large", "0.56 wide x 0.82 m tall", at=(-1.75, -0.75))
prof = [(0.14, 0.0), (0.165, 0.025), (0.25, 0.20), (0.285, 0.38), (0.255, 0.55), (0.165, 0.66), (0.128, 0.70), (0.15, 0.735)]
jar = lathe(prof, 16)
P.add(jar, jar_paint(jar["t"], (98, 62, 50), OCHRE, TURQ, 1, 3, 5, "zig"))
lugs(P, 0.19, 0.64, 4, (98, 62, 50), "lug")
cloth_lid(P, 0.15, 0.735, CL_RED, CL_WHITE, "lidL")

P = S.prop("tapuy_jar_open", "0.46 wide x 0.86 m tall with its ladle", at=(-1.2, -0.25))
prof = [(0.115, 0.0), (0.135, 0.02), (0.205, 0.16), (0.23, 0.29), (0.205, 0.42), (0.14, 0.50), (0.125, 0.535), (0.15, 0.56), (0.122, 0.56), (0.105, 0.52), (0.0, 0.515)]
jar = lathe(prof, 16)
P.add(jar, jar_paint(jar["t"], (70, 128, 118), CREAM, (40, 70, 78), 1, 3, 5, "dia", wine=(238, 214, 150), i_lip=8))
lugs(P, 0.155, 0.485, 2, (70, 128, 118), "lug")
P.add(torus(0.137, 0.014, 0.515, 14, 5), rope(STRAW, STRAW_D), key="tie")
# the cloth, untied, lying over the shoulder
P.add(sheet(lambda u, v: ((0.145 + 0.10 * v + 0.012 * math.sin(u * 14)) * math.cos(2.2 + u * 1.5), 0.50 - 0.20 * v * v - 0.012 * math.sin(u * 9) * v, (0.145 + 0.10 * v + 0.012 * math.sin(u * 14)) * math.sin(2.2 + u * 1.5)),
              8, 6, 0.014, (0.30, 0.26)), lambda C: (C.fill(CL_WHITE), [C.box(k / 6.0, 0, k / 6.0 + 0.06, 1, CL_RED) for k in range(6)], C.band(0.0, 0.08, CL_RED)), key="cloth")
# the ladle: half a coconut shell on a bamboo stick, leaning in the mouth
cup = lathe([(0.0, 0.0), (0.05, 0.012), (0.074, 0.05), (0.078, 0.085), (0.064, 0.085), (0.05, 0.04), (0.0, 0.03)], 10)
P.add(T(cup, at=(0.035, 0.515, 0.0), turn=(0, 0, -24), scale=1.15), lambda C: (C.fill((120, 78, 50)), C.grain((84, 52, 38), (160, 112, 72), False), C.band(cup["t"][3], 1, CREAM)), key="cup")
P.add(tube([(0.07, 0.56, 0.0), (0.2, 0.74, 0.02), (0.34, 0.90, 0.03)], 0.017, 6), bamboo(nodes=0.16), key="stick")

P = S.prop("tapuy_jar_small", "0.40 wide x 0.50 m tall", at=(-2.15, 0.05))
prof = [(0.105, 0.0), (0.125, 0.02), (0.185, 0.12), (0.20, 0.21), (0.175, 0.31), (0.118, 0.375), (0.10, 0.405), (0.118, 0.43)]
jar = lathe(prof, 14)
P.add(jar, jar_paint(jar["t"], OCHRE, (120, 62, 44), BLACK, 1, 3, 5, "tri", drips=7))
lugs(P, 0.14, 0.36, 2, OCHRE, "lug")
cloth_lid(P, 0.118, 0.43, (240, 232, 210), CL_RED, "lidS", tie=CL_BLACK)


# ------------------------------------------------------------------ the baskets and the bilao

def diamond(colour, period=8, mid=0.5):
    def fn(i, j, cols, rows):
        d = abs((i % period) - period // 2) + abs(j - int(rows * mid))
        return colour if d in (1, 2) else (shade(colour, 0.7) if d == 0 else None)
    return fn


def zigrow(colour, mid=0.5):
    def fn(i, j, cols, rows):
        return colour if (j - int(rows * mid) + (i % 4 if i % 8 < 4 else 4 - i % 4)) in (0, 1) else None
    return fn


def basket(P, r, h, y, tilt, a, b, pattern, key, lid=False, at=(0, 0)):
    prof = [(r * 0.70, 0.0), (r * 0.80, 0.03), (r * 1.0, h * 0.55), (r * 0.96, h)] + ([] if lid else [(r * 0.84, h), (r * 0.8, h * 0.82), (0.0, h * 0.8)])
    if lid:
        prof += [(r * 1.04, h + 0.012), (r * 1.02, h + 0.05), (r * 0.6, h + 0.12), (r * 0.16, h + 0.15), (r * 0.2, h + 0.20), (0.0, h + 0.21)]
    m = lathe(prof, 16)
    t = m["t"]

    def paint(C):
        C.fill(a)
        C.weave(a, b, 0.036, t[1], t[3], pattern)
        C.band(0, t[1], shade(b, 0.8))
        if lid:
            C.weave(b, a, 0.036, t[4], t[6], None, twill=True)
            C.band(t[6], 1, shade(b, 0.75))
        else:
            C.band(t[3], t[4], RATTAN); C.band(t[4], 1, shade(b, 0.55))
        C.vgrad(0.84, 1.05)
    P.add(T(m, at=(at[0], y, at[1]), turn=tilt), paint, key=key)
    rim = torus(r * 0.97, 0.022, h, 16, 6)
    P.add(T(rim, at=(at[0], y, at[1]), turn=tilt), rope(RATTAN, RATTAN_D, 22), key=key + "rim")


P = S.prop("basket_stack", "0.72 wide x 0.92 m tall", at=(1.75, -0.8))
basket(P, 0.33, 0.30, 0.0, (0, 0, 0), RATTAN, (176, 124, 72), diamond(WOOD_D), "b1")
basket(P, 0.27, 0.26, 0.255, (4, 0, 3), STRAW, (200, 150, 70), zigrow(CL_RED), "b2", at=(0.02, 0.0))
basket(P, 0.20, 0.20, 0.475, (-5, 30, -6), (120, 84, 60), RATTAN, diamond(CREAM, 6), "b3", lid=True, at=(-0.01, 0.02))

P = S.prop("bilao_rice", "0.74 m across x 0.16 m", at=(1.45, 0.25))
tray = planar(lathe([(0.0, 0.0), (0.31, 0.0), (0.36, 0.03), (0.372, 0.065), (0.352, 0.085), (0.322, 0.065), (0.30, 0.032), (0.0, 0.032)], 20), 1)


def bilao_paint(C):
    C.fill(RATTAN_D)
    cx = cy = 0.5
    n = int(C.w / C.px(0.034 if not CALM else 0.09))                       # calm: a few broad cells, not strands
    for j in range(n):                                                       # a herringbone, strand by strand
        for i in range(n):
            u, t = (i + 0.5) / n, (j + 0.5) / n
            if (u - cx) ** 2 + (t - cy) ** 2 > 0.185:
                continue
            over = ((i + j // 2) % 4) < 2
            col = RATTAN if over else STRAW
            C.box(i / n, j / n, (i + 1) / n, (j + 1) / n, shade(col, 0.66))
            C.box(i / n + 0.004, j / n + 0.004, (i + 1) / n - 0.004, (j + 1) / n - 0.004, col)
    r_in, r_out = 0.415, 0.5
    C.d.ellipse([C.X(0.5 - r_out), C.Y(0.5 + r_out), C.X(0.5 + r_out), C.Y(0.5 - r_out)], outline=WOOD_R, width=int(C.w * (r_out - r_in)))
    for k in range(28):                                                      # the rim's lashing
        a = 2 * math.pi * k / 28
        C.line([(0.5 + 0.42 * math.cos(a), 0.5 + 0.42 * math.sin(a)), (0.5 + 0.5 * math.cos(a + 0.06), 0.5 + 0.5 * math.sin(a + 0.06))], STRAW, C.lw(0.011))


P.add(tray, bilao_paint, k=1.4)


def rice_paint(C):
    C.fill(WHITE)
    C.specks(shade(CREAM, 0.86), 150, 0.006, long=2.2)
    C.specks((255, 255, 250), 60, 0.005, long=2.0)
    C.glow(0.38, 0.62, 0.3, 0.3, (255, 255, 252), 0.5)


heap = lathe([(0.0, 0.0), (0.215, 0.0), (0.20, 0.03), (0.14, 0.08), (0.06, 0.112), (0.0, 0.12)], 14)
P.add(T(lumpy(planar(heap, 1), 0.07, 3), at=(-0.04, 0.03, -0.02)), rice_paint, k=1.3)
scoop = lathe([(0.0, 0.0), (0.045, 0.01), (0.07, 0.045), (0.074, 0.08), (0.06, 0.08), (0.046, 0.04), (0.0, 0.03)], 10)
P.add(T(scoop, at=(0.19, 0.04, 0.15), turn=(14, 0, -10)), lambda C: (C.fill((120, 78, 50)), C.grain((84, 52, 38), (160, 112, 72), False), C.band(scoop["t"][3], 1, CREAM)), key="scoop")
for k, (x, z, yaw) in enumerate(((-0.10, 0.06, 20), (-0.03, -0.10, 110))):   # two ears of rice laid on the heap
    P.add(T(tube([(-0.13, 0, 0), (0, 0.012, 0), (0.13, 0, 0)], [0.012, 0.026, 0.02], 6), at=(x, 0.132, z), turn=(0, yaw, 0)), lambda C: (C.fill(STRAW), C.specks(STRAW_D, 30, 0.006, long=2.0)), key="ear")


# ------------------------------------------------------------------ the mortar and its pestles

P = S.prop("mortar_pestles", "0.62 wide x 1.5 m tall with the pestles", at=(-0.55, 0.75))
prof = [(0.23, 0.0), (0.26, 0.03), (0.255, 0.09), (0.165, 0.19), (0.16, 0.25), (0.275, 0.44), (0.295, 0.54), (0.27, 0.575), (0.215, 0.56), (0.14, 0.42), (0.0, 0.39)]
mortar = T(lathe(prof, 16), scale=1.22)
mortar["t"] = lathe(prof, 16)["t"]
tm = mortar["t"]


def mortar_paint(C):
    C.fill(WOOD)
    C.grain(shade(WOOD, 0.76), shade(WOOD, 1.16), True, 14)
    C.motif(tm[5] + 0.005, tm[6] - 0.005, "tri2", CREAM, bg=WOOD_D, edge=OCHRE)
    C.motif(tm[1], tm[2], "zig", OCHRE, bg=WOOD_D, edge=None)
    C.band(tm[3], tm[4], WOOD_D)
    C.vgrad(0.84, 1.04)
    C.band(tm[6], tm[8], mix(WOOD, CREAM, 0.6))
    C.band(tm[8], 1, shade(WOOD, 0.62))
    C.band(tm[9], 1, CREAM)
    C.specks(STRAW, 60, 0.008, tm[9], 1.0, long=2.0)


P.add(mortar, mortar_paint, k=1.2)
pestle = lathe([(0.04, 0.0), (0.058, 0.04), (0.06, 0.34), (0.03, 0.50), (0.03, 0.90), (0.06, 1.06), (0.058, 1.36), (0.04, 1.40)], 8)
tp = pestle["t"]


def pestle_paint(C):
    C.fill(WOOD_L)
    C.grain(shade(WOOD_L, 0.76), shade(WOOD_L, 1.12), True, 5)
    C.band(tp[3], tp[4], WOOD); C.band(tp[2] - 0.02, tp[2], OCHRE); C.band(tp[5], tp[5] + 0.02, OCHRE)
    C.band(0, tp[1], CREAM); C.band(tp[6], 1, CREAM)
    C.vgrad(0.88, 1.04)


P.add(T(pestle, at=(0.03, 0.49, 0.0), turn=(0, 0, -11)), pestle_paint, key="pestle")
P.add(T(pestle, at=(-0.66, 0.0, 0.14), turn=(8, 0, -20)), pestle_paint, key="pestle")
# rice that has jumped out, and a little heap of husk at the foot
P.add(T(lumpy(planar(lathe([(0.0, 0.0), (0.15, 0.0), (0.12, 0.03), (0.05, 0.06), (0.0, 0.066)], 10), 1), 0.1, 5), at=(0.36, 0.0, 0.30)), rice_paint, key="husk")


# ------------------------------------------------------------------ the cooking pot on its hearth

P = S.prop("hearth_pot", "0.86 wide x 0.74 m tall", at=(0.7, 0.85))
for k in range(3):
    a = math.radians(90 + 120 * k)
    st = boxed(lumpy(ball((0.15, 0.125, 0.13), 10, 6), 0.16, 11 + k, 2.2), share=True)
    P.add(T(st, at=(0.265 * math.cos(a), 0.11, 0.265 * math.sin(a)), turn=(0, -math.degrees(a) + 90, 0)), stone(STONE, bevel=0, cracks=2), key="hstone%d" % k)


def log_paint(C):
    C.fill((122, 82, 58))
    C.grain((84, 54, 42), (160, 116, 80), True, 5)
    C.band(0.0, 0.10, CREAM); C.band(0.02, 0.045, WOOD_L)
    C.band(0.62, 1.0, (44, 36, 40))
    C.specks(ORANGE, 14, 0.012, 0.66, 0.98); C.specks(GOLD, 8, 0.007, 0.7, 0.98)


for k in range(3):
    a = math.radians(30 + 120 * k)
    c, s = math.cos(a), math.sin(a)
    P.add(tube([(c * 0.52, 0.05, s * 0.52), (c * 0.25, 0.062, s * 0.25), (c * 0.04, 0.085, s * 0.04)], [0.052, 0.05, 0.044], 7), log_paint, key="log")
P.add(T(lathe([(0.0, 0.0), (0.2, 0.0), (0.16, 0.035), (0.0, 0.05)], 10), at=(0, 0.0, 0)), lambda C: (C.fill((60, 52, 54)), C.specks((210, 206, 196), 30, 0.012), C.specks(ORANGE, 16, 0.01)), key="ash")
prof = [(0.07, 0.21), (0.19, 0.27), (0.245, 0.39), (0.225, 0.52), (0.15, 0.585), (0.158, 0.615), (0.20, 0.64), (0.19, 0.655)]
pot = lathe(prof, 16)
tq = pot["t"]


def pot_paint(C):
    C.fill(CLAY)
    C.motif(tq[3] - 0.02, tq[4] - 0.02, "zig", CREAM, bg=CLAY_D, edge=CREAM)
    C.band(tq[5], 1, shade(CLAY, 1.1))
    C.glow(0.30, tq[2] + 0.06, 0.06, 0.12, (244, 160, 110), 0.8); C.glow(0.80, tq[2] + 0.06, 0.06, 0.12, (244, 160, 110), 0.8)
    C.vgrad(0.86, 1.05)
    soot = (46, 38, 42)                                                     # licked black from below, in tongues
    C.band(0, tq[1], soot)
    for k in range(9):
        u = (k + 0.5) / 9.0
        hgt = (0.10, 0.2, 0.13, 0.24, 0.09, 0.18, 0.22, 0.12, 0.16)[k]
        for du in (0,):
            C.poly([(u - 0.07, tq[1] - 0.01), (u - 0.035, tq[1] + hgt * 0.6), (u, tq[1] + hgt), (u + 0.04, tq[1] + hgt * 0.5), (u + 0.07, tq[1] - 0.01)], soot)


P.add(pot, pot_paint, k=1.2)
lid = lathe([(0.0, 0.0), (0.18, 0.0), (0.185, 0.02), (0.11, 0.055), (0.035, 0.07), (0.03, 0.095), (0.05, 0.115), (0.0, 0.125)], 14)
P.add(T(lid, at=(0.035, 0.655, 0.0), turn=(0, 0, 9)), lambda C: (C.fill(shade(CLAY, 1.06)), C.motif(lid["t"][2], lid["t"][3], "tri", CLAY_D, bg=None), C.band(lid["t"][4], 1, CLAY_D), C.vgrad(0.9, 1.05)), key="lid")
# a wooden spoon over the rim
P.add(tube([(-0.3, 0.80, 0.08), (-0.2, 0.70, 0.05), (-0.14, 0.66, 0.03)], [0.016, 0.016, 0.02], 6), flat(WOOD_L), key="spoon")
flames(P, [(0.0, 0.03, 0.30, 0.26, 0.44, 0, 0.1), (0.0, 0.02, 0.385, 0.15, 0.25, 0, -0.12),
           (-0.26, 0.03, -0.15, 0.26, 0.46, 120, -0.1), (-0.335, 0.02, -0.19, 0.15, 0.26, 120, 0.12),
           (0.26, 0.03, -0.15, 0.26, 0.40, 240, 0.1), (0.335, 0.02, -0.19, 0.15, 0.23, 240, -0.1)])

S.build()
