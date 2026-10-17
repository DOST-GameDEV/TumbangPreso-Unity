"""PAYO props, SET D: work and life.   py -3 tools/payo_pieces/prop_set_d.py

A rice drying rack hung with bundles, a woven bamboo chicken coop (a hen and two chicks beside it), a bamboo ladder, a
back-basket (pasiking), a scarecrow, a 6 m section of rope-and-plank bridge that chains end to end.
THE BRIDGE: its walking surface is y = 0 at both ends (x = -3 and x = +3) and sags 0.14 m between, so sections meet;
its four end posts are a SEPARATE object (rope_bridge__posts) so a chain can keep or drop them.
Sizes are real; the kit writes them 15 percent larger (the bridge too: 6.9 m as written; scale it by 1/1.15 for 6 m).
"""
import math
import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from prop_kit import *                                 # noqa: E402,F401,F403
from prop_bits import *                                # noqa: E402,F401,F403

S = PropSet("d", ppm=280)
DARK = (104, 60, 46)
R = random.Random(4)


def lash(P, at, r=0.05, key="lash", op="add"):
    P.add(T(ball((r, r * 0.9, r), 6, 3), at=at), rope(STRAW, STRAW_D, 5), key=key, op=op)


# ------------------------------------------------------------------ the rice drying rack

P = S.prop("drying_rack", "2.9 wide x 2.05 m tall", at=(-1.6, -1.4))
for sx in (-1, 1):
    for sz in (-1, 1):
        P.add(tube([(sx * 1.2, 0, sz * 0.5), (sx * 1.2, 1.0, sz * 0.22), (sx * 1.2, 2.02, -sz * 0.06)], [0.05, 0.046, 0.04], 6), bamboo(nodes=0.4, vertical=True), key="leg")
    lash(P, (sx * 1.2, 1.80, 0.0), 0.07)
RAILS = [(1.80, 0.0, 6), (1.12, 0.27, 3), (1.12, -0.27, 3)]
for y, z, n in RAILS:
    P.add(tube([(-1.42, y, z), (0, y + 0.02, z), (1.42, y, z)], 0.04, 6), bamboo(nodes=0.44, vertical=True), key="rail")
    for sx in (-1, 1):
        if z:
            lash(P, (sx * 1.2, y, z * 0.83), 0.055)
sheaf = lathe([(0.0, 0.2), (0.05, 0.17), (0.036, 0.06), (0.04, 0.0), (0.035, -0.03), (0.10, -0.14), (0.135, -0.32), (0.11, -0.46), (0.0, -0.50)], 6)
tf = sheaf["t"]


def sheaf_paint(tone):
    def fn(C):
        C.fill(tone)
        for k in range(14):                                                # the stalks, one by one
            u = (k + 0.5) / 14.0
            C.line([(u, tf[3]), (u + C.rnd.uniform(-0.02, 0.02), 1.0)], shade(tone, 0.8), C.lw(0.006))
            C.line([(u, tf[4]), (u + C.rnd.uniform(-0.03, 0.03), tf[7] + 0.04)], shade(tone, 0.78), C.lw(0.006))
        if CALM:                                                           # calm: the ears as one soft golden mass
            C.band(0.0, tf[6] + 0.03, mix(GOLD, tone, 0.45))
        for _ in range(70 if not CALM else 0):                             # the ears: fat grains hanging
            u, t = C.rnd.random(), C.rnd.uniform(0.0, tf[6] + 0.05)
            C.dot(u, t, 0.011, C.rnd.choice((GOLD, (255, 226, 130), STRAW_D)), squash=1.7)
        C.band(tf[3] - 0.005, tf[4] + 0.015, CL_RED)
        C.vgrad(0.86, 1.05)
    return fn


for y, z, n in RAILS:
    for k in range(n):
        x = (k - (n - 1) / 2.0) * (2.05 / n) + R.uniform(-0.04, 0.04)
        s = R.uniform(0.92, 1.12)
        P.add(T(sheaf, scale=(s, s, s * 0.8), turn=(R.uniform(-7, 7), R.uniform(0, 360), R.uniform(-5, 5)), at=(x, y - 0.05, z)), sheaf_paint(STRAW if k % 2 else (236, 202, 110)), key="sheaf%d" % (k % 2))
# a basket of threshed rice at the foot
bk = lathe([(0.15, 0.0), (0.18, 0.03), (0.25, 0.2), (0.25, 0.24), (0.21, 0.24), (0.0, 0.2)], 10)
tk = bk["t"]
P.add(T(bk, at=(1.55, 0, 0.42)), lambda C: (C.fill(RATTAN), C.weave(RATTAN, (176, 124, 72), 0.04, tk[1], tk[2]), C.band(tk[2], tk[4], WOOD_D), C.band(tk[4], 1, WHITE), C.specks(CREAM, 40, 0.008, tk[4], 1, 2.0)), key="rbasket")


# ------------------------------------------------------------------ the chicken coop

P = S.prop("chicken_coop", "1.35 wide x 0.9 m tall", at=(1.2, -1.2))
coop = lathe([(0.40, 0.0), (0.45, 0.05), (0.45, 0.32), (0.36, 0.56), (0.17, 0.74), (0.085, 0.79), (0.10, 0.86), (0.0, 0.87)], 14)
tc = coop["t"]


def coop_paint(C):
    C.fill((78, 56, 50))                                                   # the dark inside, seen through the weave
    n = 14
    for k in range(-2, n + 2):                                             # wide strips crossing both ways
        for d in (1.6, -1.6):
            C.line([(k / n, tc[1]), ((k + d) / n, tc[4] + 0.02)], BAMBOO if d > 0 else (214, 204, 110), C.lw(0.03))
            C.line([(k / n + 0.012, tc[1]), ((k + d) / n + 0.012, tc[4] + 0.02)], shade(BAMBOO, 0.72), C.lw(0.006))
    for t in (tc[1], tc[2], tc[3]):                                        # the hoops
        C.band(t - 0.022, t + 0.022, RATTAN); C.band(t - 0.022, t - 0.012, RATTAN_D)
        for k in range(n * 2 if not CALM else 0):                          # calm: the hoop's lashing goes
            C.box(k / (n * 2.0), t - 0.022, k / (n * 2.0) + 0.008, t + 0.022, RATTAN_D)
    C.band(0, tc[1] - 0.02, RATTAN_D)
    C.band(tc[4], 1, RATTAN); C.motif(tc[4], tc[5], "bar", RATTAN_D, n=n)
    # the door: an arch of dark with a pale frame
    C.d.rounded_rectangle([C.X(0.19), C.Y(tc[2] + 0.04), C.X(0.31), C.Y(tc[1] - 0.06)], radius=C.w * 0.05, fill=(40, 32, 36), outline=RATTAN, width=C.lw(0.02))
    C.vgrad(0.84, 1.05)


P.add(T(coop, turn=(0, 0, 0), at=(-0.15, 0, -0.05)), coop_paint, k=1.25)


def hen(P, at, yaw, s=1.0, body=WHITE, key="hen"):
    def put(mesh, off, paint, k2, turn=(0, 0, 0)):
        P.add(T(T(mesh, scale=s, turn=turn, at=tuple(v * s for v in off)), turn=(0, yaw, 0), at=at), paint, key=key + k2)
    b = warp(ball((0.15, 0.14, 0.20), 10, 6), lambda x, y, z: (x, y + 0.07 * max(0, -z / 0.2) ** 2 + 0.04 * max(0, z / 0.2) ** 2, z))
    put(b, (0, 0.14, 0), lambda C: (C.fill(body), C.band(0, 0.3, shade(body, 0.86)), [C.line([(0.1 + 0.2 * k, 0.5), (0.16 + 0.2 * k, 0.4), (0.22 + 0.2 * k, 0.5)], shade(body, 0.8), C.lw(0.008)) for k in range(5)]), "b")
    put(ball((0.085, 0.10, 0.085), 8, 5), (0, 0.32, 0.15), flat(body, False), "h")
    put(pillow([(0, 0), (0.03, 0.035), (0.06, 0.0), (0.085, 0.04), (0.11, 0.0), (0.12, -0.05), (-0.02, -0.05)], 0.03, ((1, 0), (0.6, 0.9), (0, 1)), centre=(0.05, -0.03)), (0, 0.44, 0.10), flat(CL_RED, False), "c", (0, -90, 0))
    put(lathe([(0.03, 0), (0.0, 0.07)], 5), (0, 0.32, 0.22), flat(OCHRE, False), "k", (90, 0, 0))
    put(ball((0.022, 0.035, 0.02), 6, 4), (0, 0.255, 0.215), flat(CL_RED, False), "w")
    for sx in (-1, 1):
        put(ball((0.016, 0.02, 0.016), 6, 4), (sx * 0.07, 0.345, 0.19), flat(CL_BLACK, False), "e")
        put(ball((0.04, 0.09, 0.13), 7, 4), (sx * 0.14, 0.15, -0.02), flat(shade(body, 0.88)), "wing")
    put(pillow([(0, 0), (0.07, 0.03), (0.10, 0.14), (0.04, 0.22), (-0.03, 0.2), (-0.05, 0.08)], 0.05, ((1, 0), (0.6, 0.9), (0, 1)), centre=(0.02, 0.1)), (0, 0.19, -0.18), flat(shade(body, 0.8)), "t", (0, 90, 0))


def chick(P, at, yaw, key):
    c, s_ = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    P.add(T(ball((0.06, 0.055, 0.07), 7, 4), at=(at[0], at[1] + 0.055, at[2])), flat(GOLD), key=key)
    P.add(T(ball((0.042, 0.042, 0.042), 7, 4), at=(at[0] + s_ * 0.05, at[1] + 0.115, at[2] + c * 0.05)), flat((255, 220, 100), False), key=key + "h")
    P.add(T(lathe([(0.016, 0), (0.0, 0.035)], 4), turn=(90, yaw, 0), at=(at[0] + s_ * 0.085, at[1] + 0.11, at[2] + c * 0.085)), flat(ORANGE, False), key=key + "k")
    for sx in (-1, 1):
        P.add(T(ball((0.009, 0.011, 0.009), 5, 3), at=(at[0] + s_ * 0.075 + c * sx * 0.025, at[1] + 0.13, at[2] + c * 0.075 - s_ * sx * 0.025)), flat(CL_BLACK, False), key=key + "e")


hen(P, (0.47, 0, 0.18), 35, 1.0)
chick(P, (0.36, 0, 0.47), 60, "chick")
chick(P, (0.14, 0, 0.52), -40, "chick")
P.add(T(lumpy(planar(lathe([(0.0, 0.0), (0.12, 0.0), (0.09, 0.02), (0.0, 0.03)], 8), 1), 0.1, 2), at=(0.26, 0.0, 0.66)), lambda C: (C.fill(STRAW), C.specks(WHITE, 30, 0.012), C.specks(STRAW_D, 20, 0.01)), key="feed")


# ------------------------------------------------------------------ the bamboo ladder

P = S.prop("bamboo_ladder", "0.75 wide x 3.0 m long, leaning 14 degrees", at=(2.7, -1.5))
LEAN = math.radians(14)


def lean(y, z=0.0):
    return (y * math.cos(LEAN) - z * math.sin(LEAN), -(y * math.sin(LEAN)) + 0.42 + z)


for sx in (-1, 1):
    pts = [(sx * (0.30 - 0.03 * y / 3.0), *lean(y)) for y in (0.0, 1.5, 3.0)]
    P.add(tube(pts, [0.05, 0.046, 0.04], 6), bamboo(nodes=0.42, vertical=True), key="lrail")
for k in range(7):
    y = 0.30 + k * 0.40
    hy, hz = lean(y, 0.045)
    w = 0.38 - 0.03 * y / 3.0
    P.add(tube([(-w, hy, hz), (w, hy, hz)], 0.032, 5), bamboo((206, 182, 96), nodes=0.3, vertical=True), key="rung")
    for sx in (-1, 1):
        lash(P, (sx * (0.30 - 0.03 * y / 3.0), hy, hz - 0.02), 0.058, key="llash", op="join")
# a cloth left to dry over a rung
ry, rz = lean(0.30 + 3 * 0.40, 0.045)
P.add(sheet(lambda u, v: (-0.17 + 0.30 * u + 0.012 * math.sin(v * 9), ry + 0.035 - abs(v - 0.45) * 0.62 + (0.035 if abs(v - 0.45) < 0.04 else 0), rz + (0.05 if v > 0.45 else -0.05) + 0.02 * math.sin(u * 7) * abs(v - 0.45)),
              4, 8, 0.014, (0.3, 0.62)), lambda C: (C.fill(CL_WHITE), cordillera(C, 0.0, 0.3), cordillera(C, 0.7, 1.0), C.band(0.3, 0.33, CL_BLACK), C.band(0.67, 0.7, CL_BLACK)), key="lcloth")


# ------------------------------------------------------------------ the pasiking

P = S.prop("pasiking", "0.52 wide x 0.86 m tall", at=(0.3, 0.2))


def rrect(hx, hz, y, r=0.06, n=3):
    pts = []
    for cx, cz, a0 in ((hx - r, hz - r, 0), (-(hx - r), hz - r, 90), (-(hx - r), -(hz - r), 180), (hx - r, -(hz - r), 270)):
        for k in range(n):
            a = math.radians(a0 + 90.0 * k / (n - 1))
            pts.append((cx + r * math.cos(a), y, cz + r * math.sin(a)))
    return pts


pack = loft([rrect(0.19, 0.125, 0.07), rrect(0.235, 0.155, 0.12), rrect(0.225, 0.15, 0.36), rrect(0.185, 0.125, 0.60), rrect(0.17, 0.115, 0.62)], nat=(1.3, 0.62))


def pack_paint(C):
    C.fill(RATTAN)
    C.weave(RATTAN, (188, 136, 80), 0.034, 0.10, 0.90, lambda i, j, cols, rows: (WOOD_D if (abs((i % 10) - 5) + abs(j - rows // 2)) in (3, 4) else (CL_RED if (abs((i % 10) - 5) + abs(j - rows // 2)) < 2 else None)))
    C.band(0.0, 0.10, WOOD_D); C.band(0.90, 1.0, WOOD_D)
    C.motif(0.82, 0.90, "zig", CREAM, n=14, bg=WOOD_D); C.motif(0.10, 0.17, "zig", CREAM, n=14, bg=WOOD_D)
    C.vgrad(0.84, 1.05)


P.add(pack, pack_paint, k=1.3)
P.add(T(cbox((0.21, 0.035, 0.14), 0.02), at=(0, 0.045, 0)), wood(DARK, bevel=0.02), key="pbase")
for sx in (-1, 1):
    for sz in (-1, 1):
        P.add(T(ball((0.04, 0.035, 0.04), 6, 4), at=(sx * 0.15, 0.03, sz * 0.09)), flat(DARK), key="pfoot")
lidp = loft([rrect(0.20, 0.135, 0.585, 0.05), rrect(0.205, 0.14, 0.64, 0.05), rrect(0.16, 0.10, 0.70, 0.05)], nat=(1.2, 0.16))
P.add(lidp, lambda C: (C.fill(WOOD_D), C.weave((120, 84, 60), RATTAN_D, 0.03, 0.15, 0.85, None, True), C.band(0.15, 0.24, CL_RED)), key="plid")
for sx in (-1, 1):                                                         # the shoulder straps, on the back
    P.add(tube(bez((sx * 0.11, 0.60, -0.115), (sx * 0.17, 0.52, -0.25), (sx * 0.17, 0.26, -0.25), (sx * 0.13, 0.14, -0.15), 7), 0.022, 6, ),
          lambda C: (C.fill((150, 104, 70)), C.rope((170, 122, 82), (112, 74, 54), 12)), key="strap")
# the blanket rolled and tied on top
roll = tube([(-0.25, 0.775, 0.0), (0.0, 0.785, 0.0), (0.25, 0.775, 0.0)], 0.082, 8)
P.add(roll, lambda C: (C.fill(CL_RED), cordillera(C, 0.08, 0.36), cordillera(C, 0.64, 0.92), C.band(0, 0.08, CL_WHITE), C.band(0.92, 1, CL_WHITE)), key="roll", k=1.3)
for sx in (-1, 1):
    P.add(T(torus(0.086, 0.012, 0, 8, 4), turn=(0, 0, 90), at=(sx * 0.11, 0.78, 0)), rope(STRAW, STRAW_D), key="rollcord")
# a bolo in its wooden sheath on the side
P.add(T(cbox((0.035, 0.19, 0.016), 0.012, taper=1.25), at=(0.245, 0.33, 0.02), turn=(0, 0, 6)), wood(WOOD_L, bevel=0.01, bands=[(0.2, 0.3, None, None, CL_RED), (0.7, 0.8, None, None, CL_RED)]), key="sheath")
P.add(tube([(0.262, 0.52, 0.02), (0.275, 0.62, 0.02), (0.30, 0.67, 0.02)], [0.02, 0.022, 0.026], 6), wood(DARK, grain="v"), key="hilt")


# ------------------------------------------------------------------ the scarecrow

P = S.prop("scarecrow", "1.9 wide x 2.15 m tall", at=(-1.3, 0.5), yaw=10)
P.add(tube([(0, 0, 0), (0.02, 1.0, 0), (0.0, 1.78, 0)], [0.05, 0.045, 0.04], 7), bamboo(nodes=0.4, vertical=True), key="spole")
P.add(tube([(-0.92, 1.36, 0), (0, 1.40, 0), (0.92, 1.33, 0)], 0.036, 7), bamboo(nodes=0.4, vertical=True), key="sarm")
SHIRT = [(-0.74, 0.30), (-0.74, 0.08), (-0.34, 0.04), (-0.38, -0.50), (-0.20, -0.56), (0.0, -0.52), (0.20, -0.56), (0.38, -0.50), (0.34, 0.04), (0.74, 0.02), (0.74, 0.24), (0.30, 0.34), (0.0, 0.30), (-0.30, 0.34)]


def shirt_paint(C):
    X = lambda x: (x + 0.74) / 1.48
    Y = lambda y: (y + 0.56) / 0.90
    C.fill(CL_RED)
    for k in range(9):
        C.box(X(-0.36 + k * 0.09) - 0.008, 0, X(-0.36 + k * 0.09) + 0.008, Y(0.04), CL_BLACK)
    cordillera(C, Y(-0.50), Y(-0.28))
    for sx in (-1, 1):
        u0, u1 = sorted((X(sx * 0.74), X(sx * 0.52)))
        C.box(u0, 0, u1, 1, CL_BLACK); C.box(X(sx * 0.57) - 0.01, 0, X(sx * 0.57) + 0.01, 1, CL_YELLOW)
    C.poly([(X(-0.14), Y(0.34)), (X(0), Y(0.10)), (X(0.14), Y(0.34))], CL_BLACK)                 # the neck
    C.box(X(0.10), Y(-0.20), X(0.26), Y(-0.04), STRAW, outline=CL_BLACK, width=C.lw(0.01))       # a patch, stitched
    for k in range(4):
        C.line([(X(0.12 + k * 0.04), Y(-0.22)), (X(0.12 + k * 0.04), Y(-0.18))], CL_BLACK, C.lw(0.008))
    C.vgrad(0.86, 1.04)


shirt = pillow(SHIRT, 0.20, ((1, 0), (0.93, 0.7), (0.6, 1.0), (0, 1.0)), centre=(0, -0.05))
shirt = warp(shirt, lambda x, y, z: (x, y - 0.06 * (abs(x) / 0.74) ** 2 + (0.03 * math.sin(x * 16) if y < -0.3 else 0), z * (1.0 if abs(x) < 0.36 else 0.55) + (0.03 * math.sin(x * 22) if y < -0.3 else 0)))
P.add(T(shirt, at=(0, 1.14, 0.0)), shirt_paint, k=1.2)
head = planar(lumpy(ball((0.185, 0.20, 0.17), 10, 6), 0.07, 8), 2)


def face_paint(C):
    C.fill((226, 190, 120))
    C.specks(shade((226, 190, 120), 0.86), 30, 0.012, long=2.4)
    for sx in (-1, 1):                                                     # two crossed stitches for eyes
        u = 0.5 + sx * 0.2
        C.line([(u - 0.07, 0.62), (u + 0.07, 0.48)], CL_BLACK, C.lw(0.018)); C.line([(u - 0.07, 0.48), (u + 0.07, 0.62)], CL_BLACK, C.lw(0.018))
    C.line([(0.26, 0.30), (0.38, 0.24), (0.5, 0.22), (0.62, 0.24), (0.74, 0.30)], CL_BLACK, C.lw(0.016))
    for k in range(5):
        u = 0.30 + k * 0.10
        C.line([(u, 0.30 - 0.03 * (1 - abs(k - 2) / 2.0) - 0.035), (u, 0.30 - 0.03 * (1 - abs(k - 2) / 2.0) + 0.02)], CL_BLACK, C.lw(0.012))
    C.oval(0.25, 0.38, 0.07, 0.06, (236, 140, 110)); C.oval(0.75, 0.38, 0.07, 0.06, (236, 140, 110))
    C.band(0.0, 0.08, STRAW_D)


P.add(T(head, at=(0, 1.72, 0.02)), face_paint, k=1.6)
hat = lathe([(0.0, 0.0), (0.40, 0.0), (0.41, 0.025), (0.22, 0.14), (0.06, 0.24), (0.05, 0.29), (0.0, 0.30)], 12)
tt = hat["t"]
P.add(T(hat, at=(0, 1.84, 0.02), turn=(-8, 0, 6)), lambda C: (C.fill(STRAW), C.weave(STRAW, RATTAN, 0.045, tt[2], tt[4], lambda i, j, cols, rows: CL_RED if j == rows // 2 else None, True),
                                                             C.band(tt[1], tt[2], WOOD_D), C.band(0, tt[1], RATTAN_D), C.band(tt[4], 1, WOOD_D), C.vgrad(0.86, 1.05)), key="hat", k=1.2)
tuft = lathe([(0.035, 0.0), (0.05, 0.05), (0.10, 0.20), (0.07, 0.24), (0.0, 0.25)], 7)
tuft_paint = lambda C: (C.fill(STRAW), [C.line([((k + 0.5) / 12.0, 0.0), ((k + 0.5) / 12.0 + 0.02, 1.0)], STRAW_D if k % 2 else (255, 226, 130), C.lw(0.008)) for k in range(12)], C.band(0.12, 0.2, CL_BLACK))
P.add(T(tuft, turn=(0, 0, 96), at=(-0.72, 1.31, 0)), tuft_paint, key="tuft")
P.add(T(tuft, turn=(0, 0, -98), at=(0.72, 1.28, 0)), tuft_paint, key="tuft")
P.add(T(tuft, turn=(0, 0, 178), at=(-0.06, 0.66, 0), scale=(1.5, 1.2, 1.2)), tuft_paint, key="tuft")
P.add(T(tuft, turn=(0, 0, 184), at=(0.14, 0.66, 0.02), scale=(1.4, 1.0, 1.2)), tuft_paint, key="tuft")
for k, (a, h) in enumerate(((40, (0.17, 0.12, 0.15)), (170, (0.16, 0.11, 0.14)), (290, (0.15, 0.12, 0.14)))):
    field_stone(P, h, (0.16 * math.cos(math.radians(a)), 0.07, 0.16 * math.sin(math.radians(a))), 30 + k, yaw=a)
# the bird that is not scared
bx, by = 0.50, 1.385
P.add(T(ball((0.06, 0.07, 0.10), 8, 5), at=(bx, by + 0.075, 0.0), turn=(-20, 0, 0)), lambda C: (C.fill((60, 52, 60)), C.band(0, 0.4, (150, 96, 70))), key="bird")
P.add(T(ball((0.05, 0.05, 0.05), 7, 4), at=(bx, by + 0.16, 0.06)), lambda C: (C.fill((40, 36, 44)), C.band(0.0, 0.45, WHITE)), key="birdh")
P.add(T(lathe([(0.02, 0), (0.0, 0.05)], 4), turn=(90, 0, 0), at=(bx, by + 0.155, 0.10)), flat(OCHRE, False), key="birdk")
P.add(T(pillow([(0, 0), (0.035, 0.02), (0.03, 0.16), (-0.03, 0.16), (-0.035, 0.02)], 0.02, ((1, 0), (0.6, 0.9), (0, 1))), turn=(-115, 0, 0), at=(bx, by + 0.07, -0.07)), flat((50, 44, 52), False), key="birdt")
for sx in (-1, 1):
    P.add(T(ball((0.012, 0.014, 0.012), 5, 3), at=(bx + sx * 0.036, by + 0.175, 0.085)), flat(WHITE, False), key="birde")


# ------------------------------------------------------------------ the rope bridge

P = S.prop("rope_bridge", "6.0 long x 1.4 wide x 1.3 m tall", at=(0.0, 2.6))
P.clip = False
P.lift = 0.7
SAG = 0.14


def deck(x):
    return -SAG * (1.0 - (x / 3.0) ** 2)


def hand(x):
    return 1.05 - 0.36 * (1.0 - (x / 3.0) ** 2) + deck(x)


TONES = [WOOD, (176, 124, 82), (136, 96, 70), (150, 132, 112)]
n_planks = 14
for k in range(n_planks):
    x = -3.0 + (k + 0.5) * 6.0 / n_planks
    slope = math.degrees(math.atan(2 * SAG * x / 9.0))
    wid = 0.145 + R.uniform(-0.02, 0.025)
    tone = TONES[k % 4]
    P.add(T(cbox((wid, 0.035, 0.55 + R.uniform(-0.03, 0.06)), 0.018), at=(x, deck(x) - 0.035, R.uniform(-0.03, 0.03)), turn=(R.uniform(-2, 2), R.uniform(-3, 3), slope + R.uniform(-1.5, 1.5))),
          wood(tone, bevel=0.018, grain="v"), key="plank%d" % (k % 4))
xs = np.linspace(-3.0, 3.0, 7)
for sz in (-1, 1):
    P.add(tube([(x, deck(x) - 0.085, sz * 0.40) for x in xs], 0.04, 5), rope(STRAW, STRAW_D), key="rope")
    P.add(tube([(x, hand(x), sz * 0.66) for x in xs], 0.034, 5), rope(STRAW, STRAW_D), key="rope")
    for k in range(1, n_planks, 2):                                        # the hangers
        x = -3.0 + k * 6.0 / n_planks
        P.add(tube([(x, hand(x), sz * 0.66), (x, (hand(x) + deck(x)) / 2, sz * 0.62), (x, deck(x) - 0.07, sz * 0.56)], 0.019, 3), rope(STRAW, STRAW_D), key="hanger", op="join")
# strips of cloth tied to the hand ropes
for k, (x, sz, col) in enumerate(((-1.5, 1, CL_RED), (0.4, 1, CL_YELLOW), (1.9, 1, CL_WHITE), (-0.5, -1, CL_RED), (2.3, -1, CL_YELLOW))):
    P.add(sheet(lambda u, v, x=x, sz=sz: (x - 0.09 + 0.18 * u + 0.02 * (1 - v), hand(x) + 0.02 - 0.42 * (1 - v), sz * 0.66 + 0.03 * math.sin(u * 5 + v * 3) * (1 - v) + 0.04 * sz), 2, 4, 0.014, (0.18, 0.42)),
          lambda C, c=col: (C.fill(c), C.band(0.0, 0.16, CL_BLACK), C.band(0.2, 0.26, CL_BLACK if c != CL_RED else CL_YELLOW)), key="flag%d" % (k % 3), op="join")
for sx in (-1, 1):
    for sz in (-1, 1):
        pr = lathe([(0.085, -0.5), (0.085, 0.80), (0.11, 0.86), (0.11, 0.98), (0.08, 1.04), (0.085, 1.20), (0.05, 1.26)], 8)
        tq = pr["t"]
        P.add(T(pr, at=(sx * 3.0, 0, sz * 0.66)), lambda C: (C.fill(DARK), C.grain(shade(DARK, 0.74), shade(DARK, 1.2), True, 6), C.motif(tq[2], tq[3], "eye", CREAM, n=4, bg=CL_BLACK, edge=OCHRE),
                                                           C.rope(STRAW, STRAW_D) if False else None, C.band(tq[3] + 0.015, tq[3] + 0.05, STRAW), C.band(tq[5], 1, OCHRE), C.vgrad(0.84, 1.05)), key="bpost", group="posts")

S.build()
