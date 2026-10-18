"""PAYO props, SET B: the sacred and the proud.   py -3 tools/payo_pieces/prop_set_b.py

A rack of bronze gongs (gangsa) with beaters; crossed spears and a pronged Kalinga shield; a carved hagabi bench with an
animal head at each end; a carabao skull on a carved post; a tall woven banner on a bamboo pole, HANGING AT REST with
real folds (the owner has rejected stiff boards and banners blown sideways), its pattern in the paint.
Sizes are real; the kit writes them 15 percent larger.
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from prop_kit import *                                 # noqa: E402,F401,F403
from prop_bits import *                                # noqa: E402,F401,F403

S = PropSet("b", ppm=300)
DARK = (104, 60, 46)                                   # the old red-brown of narra


def post(P, x, z, h, r, key, bands=True, base=DARK, seg=8):
    prof = [(r * 1.25, 0.0), (r * 1.3, 0.05), (r, 0.12), (r, h * 0.52), (r * 1.18, h * 0.56), (r * 1.18, h * 0.66), (r, h * 0.70), (r * 0.95, h - 0.16), (r * 1.3, h - 0.12), (r * 1.3, h - 0.05), (r * 0.7, h)]
    m = lathe(prof, seg)
    t = m["t"]

    def paint(C):
        C.fill(base)
        C.grain(shade(base, 0.74), shade(base, 1.2), True, 6)
        C.motif(t[4], t[5], "eye", CREAM, n=4, bg=CL_BLACK, edge=OCHRE)
        C.motif(t[2], t[2] + 0.07, "zig", OCHRE, n=6, bg=shade(base, 0.7))
        C.motif(t[6] + 0.02, t[6] + 0.09, "tri", CREAM, n=6, bg=None)
        C.band(t[8], t[9], OCHRE); C.band(0, t[1], shade(base, 0.7))
        C.vgrad(0.84, 1.05)
    P.add(T(m, at=(x, 0, z)), paint, key=key)


# ------------------------------------------------------------------ the gong rack

P = S.prop("gong_rack", "2.3 wide x 1.75 m tall", at=(-2.6, -0.6), yaw=12)
for sx in (-1, 1):
    post(P, sx * 0.95, 0.0, 1.62, 0.07, "post")
    P.add(T(cbox((0.11, 0.07, 0.36), 0.03), at=(sx * 0.95, 0.07, 0)), wood(DARK, bevel=0.03), key="foot")
beam = tube([(-1.16, 1.40, 0), (0, 1.43, 0), (1.16, 1.40, 0)], 0.052, 7)
P.add(beam, lambda C: (C.fill(WOOD), C.grain(shade(WOOD, 0.74), shade(WOOD, 1.16), True, 5), C.band(0, 0.045, CREAM), C.band(0.955, 1, CREAM),
                       C.motif(0.47, 0.53, "x", CREAM, n=4, bg=CL_BLACK)), key="beam")
P.add(tube([(-0.95, 0.24, 0), (0.95, 0.24, 0)], 0.04, 6), wood(WOOD, grain="v"), key="rail")


def gong_paint(patina):
    def fn(C):
        C.fill(BRONZE_D)
        C.oval(0.5, 0.5, 0.47, 0.47, BRONZE)
        C.oval(0.5, 0.5, 0.47, 0.47, None, shade(BRONZE_D, 0.8), C.lw(0.012))
        C.oval(0.5, 0.5, 0.38, 0.38, None, BRONZE_D, C.lw(0.012))
        C.oval(0.5, 0.5, 0.27, 0.27, None, BRONZE_D, C.lw(0.008))
        for _ in range(46 if not CALM else 0):                             # hammer marks (calm: none)
            a, r = C.rnd.uniform(0, 6.28), C.rnd.uniform(0.17, 0.44)
            C.dot(0.5 + r * math.cos(a), 0.5 + r * math.sin(a), 0.011, C.rnd.choice((BRONZE_L, BRONZE_D, shade(BRONZE, 0.86))))
        for a in patina:                                                   # green where the rain sits
            C.glow(0.5 + 0.40 * math.cos(a), 0.5 + 0.40 * math.sin(a), 0.11, 0.11, TURQ, 0.8)
        C.oval(0.5, 0.5, 0.15, 0.15, BRONZE_L, BRONZE_D, C.lw(0.01))
        C.oval(0.46, 0.54, 0.05, 0.05, (255, 240, 180))
        C.d.arc([C.X(0.14), C.Y(0.86), C.X(0.86), C.Y(0.14)], 200, 262, fill=(255, 236, 170), width=C.lw(0.022))
    return fn


for k, (gx, r, yaw, patina) in enumerate(((-0.52, 0.30, 10, (4.2, 5.2)), (0.12, 0.25, -8, (4.9,)), (0.64, 0.20, 14, (4.0, 5.6)))):
    gy = 1.40 - 0.17 - r
    g = planar(lathe([(0.0, -0.012), (r * 0.9, -0.012), (r * 0.94, -0.06), (r, -0.06), (r, 0.0), (r * 0.93, 0.02), (r * 0.18, 0.03), (0.0, 0.05)], 12), 1)
    P.add(T(g, turn=(90, yaw, 0), at=(gx, gy, 0.0)), gong_paint(patina), key="gong%d" % k, k=1.3)
    for s in (-1, 1):
        a = math.radians(yaw)
        tx, tz = gx + s * r * 0.55 * math.cos(a), -s * r * 0.55 * math.sin(a)
        P.add(tube([(tx, gy + r * 0.8, tz - 0.02), (gx + s * 0.03, 1.36, 0.0), (gx + s * 0.03, 1.46, 0.0)], 0.014, 5), flat(CL_RED, False), key="cord", op="join")
    P.add(T(torus(0.068, 0.018, 0.0, 8, 5), turn=(0, 0, 90), at=(gx + 0.004, 1.412, 0.003)), flat(CL_RED, False), key="cord")


def beater_paint(C):
    C.fill(WOOD_L)
    C.band(0.66, 1.0, CL_RED); C.motif(0.70, 0.80, "x", CL_YELLOW, n=3); C.band(0.62, 0.66, CL_BLACK); C.band(0, 0.08, CL_BLACK)


beater = lathe([(0.018, 0.0), (0.02, 0.36), (0.058, 0.40), (0.066, 0.47), (0.05, 0.54), (0.0, 0.55)], 8)
P.add(T(beater, at=(-0.72, 0.085, 0.16), turn=(-12, 0, -15)), beater_paint, key="beater")
P.add(T(beater, at=(1.08, 1.32, 0.03), turn=(0, 0, 180)), beater_paint, key="beater")
P.add(tube([(1.08, 1.30, 0.03), (1.08, 1.42, 0.0)], 0.012, 5), flat(CL_RED, False), key="cord", op="join")
tie(P, (-0.95, 1.12, 0.0), 0.085, CL_RED, yaw=20, length=0.34)
tie(P, (0.95, 1.12, 0.0), 0.085, CL_RED, yaw=-20, length=0.3)


# ------------------------------------------------------------------ crossed spears and the shield

P = S.prop("spears_shield", "1.2 wide x 2.1 m tall", at=(-0.55, -0.9))
P.add(T(cbox((0.42, 0.09, 0.20), 0.035, taper=0.86), at=(0, 0.09, -0.04)), stone(STONE, bevel=0.035, cracks=1, bands=[(0.3, 0.72, "step", STONE_L, STONE_D)]))


def shaft_paint(C):
    C.fill(DARK)
    C.grain(shade(DARK, 0.74), shade(DARK, 1.2), True, 4)
    for t0 in (0.30, 0.52, 0.80):
        C.band(t0, t0 + 0.012, OCHRE); C.band(t0 + 0.012, t0 + 0.03, CL_BLACK); C.band(t0 + 0.03, t0 + 0.042, OCHRE)
    C.band(0.86, 1.0, (120, 122, 128))


def blade_paint(C):
    C.fill((150, 154, 162))
    C.poly([(0.5, 0.0), (1.0, 0.42), (0.5, 1.0)], (196, 200, 206))
    C.line([(0.5, 0.0), (0.5, 1.0)], (88, 90, 100), C.lw(0.012))
    C.bevel(0.012, (232, 234, 236))


for s, z in ((1, -0.085), (-1, -0.02)):
    foot, tip = np.array((-s * 0.47, 0.14, z)), np.array((s * 0.47, 1.86, z))
    P.add(tube([foot, (foot + tip) / 2, tip], 0.026, 7), shaft_paint, key="shaft")
    ang = math.degrees(math.atan2(tip[1] - foot[1], tip[0] - foot[0])) - 90
    blade = pillow([(0, 0), (0.05, 0.05), (0.075, 0.15), (0.05, 0.28), (0, 0.40), (-0.05, 0.28), (-0.075, 0.15), (-0.05, 0.05)], 0.036, ((1, 0), (0.55, 0.8), (0, 1)), centre=(0, 0.17))
    P.add(T(blade, turn=(0, 0, ang), at=tuple(tip - (tip - foot) / np.linalg.norm(tip - foot) * 0.03)), blade_paint, key="blade")
    d = (tip - foot) / np.linalg.norm(tip - foot)
    c = tip - d * 0.16
    P.add(T(torus(0.034, 0.018, 0, 8, 5), turn=(0, 0, ang), at=tuple(c)), flat(CL_RED, False), key="wrap")
    for k in range(2):                                                      # a tassel of red cloth under the head
        P.add(tube([tuple(c), (c[0] - s * 0.05 + k * 0.05 * s, c[1] - 0.12, z + 0.03), (c[0] - s * 0.03 + k * 0.07 * s, c[1] - 0.27, z + 0.035)], [0.014, 0.024, 0.03], 5),
              lambda C: (C.fill(CL_RED), C.band(0.85, 1, CL_YELLOW)), key="tassel")

# The shield: three prongs up, two down, the walls of the notches running out from the middle so the paint can bevel.
SH = [(0.07, 0.27), (0.032, 0.62), (-0.032, 0.62), (-0.07, 0.27), (-0.175, 0.66), (-0.245, 0.66), (-0.25, 0.27), (-0.26, 0.0), (-0.25, -0.30),
      (-0.225, -0.64), (-0.135, -0.64), (0.0, -0.27), (0.135, -0.64), (0.225, -0.64), (0.25, -0.30), (0.26, 0.0), (0.25, 0.27), (0.245, 0.66), (0.175, 0.66)]


def shield_paint(C):
    X = lambda x: (x + 0.26) / 0.52
    Y = lambda y: (y + 0.64) / 1.30
    C.fill(CL_BLACK)
    C.box(0, Y(0.27), 1, 1, CL_RED); C.box(0, 0, 1, Y(-0.30), CL_RED)                 # the prongs
    C.box(0, Y(0.40), 1, Y(0.46), CL_WHITE); C.box(0, Y(-0.46), 1, Y(-0.40), CL_WHITE)
    C.box(0, Y(0.57), 1, 1, CL_BLACK); C.box(0, 0, 1, Y(-0.56), CL_BLACK)
    C.box(0, Y(0.545), 1, Y(0.57), CL_YELLOW); C.box(0, Y(-0.56), 1, Y(-0.535), CL_YELLOW)
    C.poly([(X(0), Y(0.19)), (X(0.17), Y(0)), (X(0), Y(-0.20)), (X(-0.17), Y(0))], CL_RED)       # the eye of the shield
    C.poly([(X(0), Y(0.12)), (X(0.105), Y(0)), (X(0), Y(-0.125)), (X(-0.105), Y(0))], CL_YELLOW)
    C.poly([(X(0), Y(0.055)), (X(0.045), Y(0)), (X(0), Y(-0.06)), (X(-0.045), Y(0))], CL_BLACK)
    for y0, y1 in ((0.205, 0.26), (-0.29, -0.235)):                                              # the rattan lashings
        C.box(0, Y(y0), 1, Y(y1), RATTAN)
        for k in range(12):
            C.line([(k / 12.0, Y(y0)), (k / 12.0 + 0.05, Y(y1))], RATTAN_D, C.lw(0.008))
    for sx in (-1, 1):
        C.line([(X(sx * 0.215), Y(0.19)), (X(sx * 0.215), Y(-0.22))], CL_WHITE, C.lw(0.014))
        for k in range(4):
            y = 0.15 - k * 0.10
            C.poly([(X(sx * 0.215), Y(y)), (X(sx * 0.15), Y(y - 0.035)), (X(sx * 0.215), Y(y - 0.07))], CL_WHITE)


shield = pillow(SH, 0.075, ((1, 0), (0.93, 0.6), (0.78, 1.0), (0, 1.0)), centre=(0, 0), back=((1, 0), (0.9, 0.8), (0, 1.0)))
shield = warp(shield, lambda x, y, z: (x, y, z - 1.7 * x * x + 0.02 * (1 - (y / 0.64) ** 2)))
P.add(T(shield, at=(0, 0.97, 0.10), turn=(-7, 0, 0)), shield_paint, k=1.6)
P.add(T(ball((0.05, 0.06, 0.03), 8, 5), at=(0, 0.97, 0.15)), flat(OCHRE), key="boss")
field_stone(P, (0.13, 0.09, 0.11), (-0.40, 0.05, 0.19), 3)
field_stone(P, (0.10, 0.07, 0.09), (0.44, 0.05, 0.16), 5, yaw=40)


# ------------------------------------------------------------------ the hagabi

P = S.prop("hagabi_bench", "3.5 long x 0.72 m tall", at=(0.9, 0.9), yaw=-6)
HL = 1.25


def section(x):
    k = 1.0 - (abs(x) / HL) ** 1.6
    top, low, w = 0.42 + 0.30 * k, 0.24, 0.31 - 0.04 * (1 - k)
    pts = [(-w * 0.72, low), (-w * 0.3, low - 0.02), (w * 0.3, low - 0.02), (w * 0.72, low), (w, low + 0.07), (w * 1.05, top - 0.03), (w * 0.9, top + 0.012), (w * 0.3, top + 0.03),
           (-w * 0.3, top + 0.03), (-w * 0.9, top + 0.012), (-w * 1.05, top - 0.03), (-w, low + 0.07)]
    return [(x, y, z) for z, y in pts]


body = loft([section(x) for x in np.linspace(-HL, HL, 11)], nat=(1.3, 2.6))


def hagabi_paint(C):
    C.fill(DARK)
    C.grain(shade(DARK, 0.72), shade(DARK, 1.22), True, 16)
    for t0 in (0.06, 0.84):
        C.motif(t0, t0 + 0.10, "eye", CREAM, n=8, bg=CL_BLACK, edge=OCHRE)
    C.motif(0.455, 0.545, "tri2", OCHRE, n=8, bg=CL_BLACK, edge=CREAM)
    for t0 in (0.27, 0.70):
        C.motif(t0, t0 + 0.035, "dot", OCHRE, n=16)
    C.box(0.49, 0, 0.70, 1, shade(DARK, 1.16)); C.grain(shade(DARK, 0.8), shade(DARK, 1.3), True, 5)
    C.glow(0.59, 0.5, 0.07, 0.34, mix(DARK, WOOD_L, 0.55), 0.55)                       # the seat, polished by sitting


P.add(body, hagabi_paint, k=1.25)
for sx in (-1, 1):
    P.add(T(cbox((0.22, 0.175, 0.24), 0.04, taper=0.72), at=(sx * 0.72, 0.175, 0)), wood(DARK, bevel=0.03, bands=[(0.25, 0.75, "zig", OCHRE, CL_BLACK)], top_plain=True), key="hfoot")
    # the neck and the head: a long-snouted pig, ears up, looking out along the bench
    P.add(tube([(sx * 1.16, 0.36, 0), (sx * 1.36, 0.43, 0), (sx * 1.50, 0.52, 0)], [0.17, 0.14, 0.13], 8), wood(DARK, grain="v", bands=[(0.5, 0.64, "zig", CREAM, CL_BLACK)]), key="neck")
    head = ball((0.27, 0.15, 0.15), 10, 6)
    head = warp(head, lambda x, y, z: (x, y * (1.0 - 0.36 * max(0.0, x / 0.27)) + 0.02 * max(0.0, x / 0.27), z * (1.0 - 0.42 * max(0.0, x / 0.27))))
    P.add(T(head, scale=(sx, 1, 1), at=(sx * 1.66, 0.55, 0), turn=(0, 0, -sx * 14)), wood(DARK, grain="h"), key="head")
    P.add(T(lathe([(0.0, -0.03), (0.075, -0.03), (0.085, 0.0), (0.07, 0.03), (0.0, 0.035)], 8), turn=(0, 0, -sx * 104), at=(sx * 1.91, 0.485, 0)),
          lambda C: (C.fill(mix(DARK, CREAM, 0.5)), C.band(0.0, 0.5, DARK)), key="snout")
    for sz in (-1, 1):
        ear = pillow([(0, 0), (0.08, 0.05), (0.07, 0.18), (0.0, 0.32), (-0.06, 0.16), (-0.06, 0.04)], 0.055, ((1, 0), (0.55, 0.9), (0, 1)), centre=(0, 0.09))
        P.add(T(ear, turn=(sz * 20, 90 + sz * sx * 20, 0), at=(sx * 1.52, 0.63, sz * 0.10)), lambda C: (C.fill(DARK), C.poly([(0.5, 0.1), (0.8, 0.4), (0.5, 0.9), (0.25, 0.4)], mix(DARK, CLAY, 0.6))), key="ear")
        P.add(T(ball((0.034, 0.034, 0.02), 8, 5), at=(sx * 1.70, 0.60, sz * 0.118), turn=(0, sz * 10, 0)), lambda C: (C.fill(CREAM), C.band(0.3, 0.7, CL_BLACK)), key="eye")
        P.add(T(ball((0.018, 0.022, 0.012), 6, 4), at=(sx * 1.925, 0.485 + 0.0, sz * 0.035)), flat(CL_BLACK, False), key="nostril")


def blanket_fn(u, v):
    x = -0.78 + 0.40 * u
    s = (v - 0.5) * 1.16
    k = 1.0 - (abs(x) / HL) ** 1.6
    top, w = 0.45 + 0.30 * k + 0.012, 0.335
    if abs(s) < 0.25:
        return (x, top + 0.012 * math.cos(s / 0.25 * 1.57), s)
    hang = abs(s) - 0.25
    if hang < 0.10:
        a = hang / 0.10 * 1.57
        return (x, top - 0.055 * (1 - math.cos(a)), math.copysign(0.25 + (w - 0.25) * math.sin(a) + 0.012, s))
    return (x + 0.02 * math.sin(u * 7) * (hang - 0.1), top - 0.055 - (hang - 0.10), math.copysign(w + 0.012 + 0.018 * math.sin(u * math.pi * 3 + s) * min(1.0, (hang - 0.1) / 0.08), s))


def blanket_paint(C):
    C.fill(CL_RED)
    cordillera(C, 0.0, 0.24); cordillera(C, 0.76, 1.0)
    for u in (0.1, 0.9):
        C.box(u - 0.06, 0.24, u + 0.06, 0.76, CL_BLACK); C.box(u - 0.012, 0.24, u + 0.012, 0.76, CL_YELLOW)
    C.motif(0.40, 0.60, "eye", CL_WHITE, n=2, bg=CL_BLACK, edge=CL_RED)
    C.band(0.385, 0.40, CL_YELLOW); C.band(0.60, 0.615, CL_YELLOW)


P.add(sheet(blanket_fn, 5, 16, 0.016, (0.40, 1.16)), blanket_paint, k=1.4)
for piece in P.pieces:                                                       # the whole carving stands on its two feet
    if piece["key"] != "hfoot":
        piece["mesh"] = T(piece["mesh"], at=(0, 0.09, 0))


# ------------------------------------------------------------------ the carabao skull on its post

P = S.prop("skull_post", "1.6 wide x 2.2 m tall", at=(2.5, -0.8))
post(P, 0.0, 0.0, 1.95, 0.105, "spost", seg=12)
for k, (a, h) in enumerate(((20, (0.17, 0.11, 0.14)), (140, (0.15, 0.10, 0.12)), (255, (0.14, 0.09, 0.13)))):
    field_stone(P, h, (0.21 * math.cos(math.radians(a)), 0.07, 0.21 * math.sin(math.radians(a))), 20 + k, yaw=a, moss=0.5 if k == 1 else 0.0)
SK = [(0.0, 0.28), (0.12, 0.27), (0.20, 0.20), (0.22, 0.10), (0.16, 0.0), (0.115, -0.12), (0.10, -0.26), (0.075, -0.33), (0.0, -0.35),
      (-0.075, -0.33), (-0.10, -0.26), (-0.115, -0.12), (-0.16, 0.0), (-0.22, 0.10), (-0.20, 0.20), (-0.12, 0.27)]


def skull_paint(C):
    X = lambda x: (x + 0.22) / 0.44
    Y = lambda y: (y + 0.35) / 0.63
    C.fill(BONE)
    C.poly([(X(-0.22), Y(0.28)), (X(0.22), Y(0.28)), (X(0.16), Y(0.20)), (X(-0.16), Y(0.20))], mix(BONE, WHITE, 0.6))
    C.poly([(X(-0.10), Y(-0.12)), (X(0.10), Y(-0.12)), (X(0.08), Y(-0.35)), (X(-0.08), Y(-0.35))], shade(BONE, 0.86))
    for sx in (-1, 1):
        C.oval(X(sx * 0.115), Y(0.085), 0.115, 0.085, (50, 40, 46))                              # the eye sockets
        C.oval(X(sx * 0.115), Y(0.085), 0.115, 0.085, None, shade(BONE, 0.7), C.lw(0.01))
        C.poly([(X(sx * 0.012), Y(-0.15)), (X(sx * 0.06), Y(-0.19)), (X(sx * 0.045), Y(-0.29)), (X(sx * 0.012), Y(-0.30))], (60, 48, 52))   # the nose
    C.line([(X(0), Y(0.27)), (X(0.012), Y(0.12)), (X(-0.008), Y(-0.02)), (X(0), Y(-0.14))], shade(BONE, 0.66), C.lw(0.007))
    C.line([(X(0.05), Y(0.26)), (X(0.07), Y(0.20)), (X(0.05), Y(0.17))], shade(BONE, 0.66), C.lw(0.006))
    for k in range(5):                                                                           # the teeth
        C.line([(X(-0.06 + k * 0.03), Y(-0.33)), (X(-0.06 + k * 0.03), Y(-0.35))], shade(BONE, 0.6), C.lw(0.006))
    C.poly([(X(-0.05), Y(0.245)), (X(0.0), Y(0.20)), (X(0.05), Y(0.245)), (X(0.0), Y(0.275))], CL_RED)   # a painted mark on the brow


skull = pillow(SK, 0.23, ((1, 0), (0.94, 0.5), (0.72, 0.92), (0, 1.0)), centre=(0, 0.04), back=((1, 0), (0.8, 0.6), (0, 0.7)))
skull = warp(skull, lambda x, y, z: (x, y, z * (0.55 + 0.45 * min(1.0, max(0.0, (y + 0.35) / 0.5))) + (0.05 * (1 - min(1.0, max(0.0, (y + 0.35) / 0.4))) if z > 0 else 0)))
P.add(T(skull, at=(0, 1.57, 0.185), turn=(12, 0, 0), scale=1.25), skull_paint, k=1.8)


def horn_paint(C):
    C.fill((92, 80, 78))
    for k in range(9):
        C.band(k / 9.0 * 0.8, k / 9.0 * 0.8 + 0.022, (58, 48, 50))
    C.box(0.1, 0.0, 0.26, 0.8, (132, 120, 114))
    C.band(0.82, 1.0, BONE)


for sx in (-1, 1):
    pts = bez((sx * 0.20, 1.82, 0.16), (sx * 0.62, 1.74, 0.14), (sx * 0.92, 1.92, 0.10), (sx * 0.74, 2.30, 0.02), 9)
    P.add(tube(pts, list(np.linspace(0.085, 0.016, 9)), 8), horn_paint, key="horn")
tie(P, (0, 1.20, 0), 0.125, CL_RED, tails=3, yaw=0, length=0.42, key="stie")
for k, (dx, col) in enumerate(((-0.17, CL_BLACK), (0.17, CL_WHITE))):           # feathers hung under the skull
    f = pillow([(0, 0), (0.04, -0.06), (0.045, -0.2), (0, -0.3), (-0.045, -0.2), (-0.04, -0.06)], 0.018, ((1, 0), (0.5, 0.9), (0, 1)), centre=(0, -0.15))
    P.add(T(f, at=(dx, 1.22, 0.15), turn=(-8, 0, dx * 60)), lambda C, c=col: (C.fill(c), C.line([(0.5, 0), (0.5, 1)], mix(c, (128, 128, 128), 0.5), C.lw(0.008)), C.band(0.0, 0.16, CL_RED)), key="feather%d" % k)


# ------------------------------------------------------------------ the banner

P = S.prop("banner", "1.3 wide x 4.4 m tall", at=(-1.9, 0.7))
pole = tube([(0, 0, 0), (0.0, 2.2, 0), (0.02, 4.25, 0)], [0.058, 0.05, 0.04], 8)
P.add(pole, bamboo(nodes=0.42, vertical=True), key="pole")
arm = tube([(-0.22, 3.92, 0.0), (0.5, 3.95, 0.0), (1.08, 3.97, 0.0)], 0.04, 7)
P.add(arm, bamboo(nodes=0.3, vertical=True), key="arm")
P.add(tube([(0.0, 3.45, 0.0), (0.5, 3.93, 0.0)], 0.022, 6), bamboo(nodes=0.3, vertical=True), key="brace")
P.add(T(ball((0.075, 0.07, 0.075), 8, 5), at=(0.01, 3.93, 0)), rope(STRAW, STRAW_D, 6), key="lash")
P.add(T(ball((0.05, 0.05, 0.05), 8, 5), at=(0.0, 3.45, 0)), rope(STRAW, STRAW_D, 6), key="lash")
for k, (a, h, r) in enumerate(((10, (0.25, 0.17, 0.21), 0.24), (125, (0.22, 0.14, 0.19), 0.23), (240, (0.23, 0.2, 0.2), 0.22))):
    field_stone(P, h, (r * math.cos(math.radians(a)), 0.09 if k < 3 else 0.27, r * math.sin(math.radians(a))), 40 + k, yaw=a, moss=0.5 if k == 2 else 0)
BW, BT, BB = 0.80, 3.90, 1.05
FOLDS = 2.0


def banner_fn(u, v):
    drop = 1.0 - v                                                          # 0 at the arm, 1 at the hem
    wide = BW * (1.0 - 0.24 * drop ** 0.6)                                  # it gathers as it hangs
    x = 0.60 + (u - 0.5) * wide + 0.02 * drop * drop
    amp = 0.02 + 0.125 * drop ** 0.7                                        # deeper folds lower down
    z = amp * math.sin(u * math.pi * 2 * FOLDS + 0.6) + 0.05 * drop * math.sin(u * math.pi + 0.4) + 0.04 * drop * drop
    y = BB + (BT - BB) * v + 0.05 * drop * math.cos(u * math.pi * 2 * FOLDS + 0.6) - 0.10 * drop * (u - 0.3) ** 2    # the hem rides the folds
    return (x, y + 0.05 * v, z * (1.0 - v ** 6))


def banner_paint(C):
    C.fill(CL_RED)
    cordillera(C, 0.05, 0.22); cordillera(C, 0.42, 0.56); cordillera(C, 0.78, 0.93)
    for t0, t1 in ((0.22, 0.42), (0.56, 0.78)):
        C.motif(t0 + 0.02, t1 - 0.02, "eye", CL_WHITE, n=1, bg=CL_RED, edge=CL_BLACK)
        for u in (0.2, 0.8):
            C.box(u - 0.012, t0, u + 0.012, t1, CL_YELLOW)
    for u in (0.045, 0.955):
        C.box(u - 0.045, 0.05, u + 0.045, 0.96, CL_BLACK)
        for k in range(22 if not CALM else 0):                              # calm: the border stays one black block
            C.dot(u, 0.07 + k * 0.04, 0.012, CL_WHITE)
    C.band(0.93, 1.0, CL_BLACK); C.band(0.945, 0.955, CL_YELLOW)
    C.band(0.0, 0.05, CL_BLACK)
    for k in range(12):                                                     # the fringe
        C.box(k / 12.0 + 0.02, 0.0, k / 12.0 + 0.06, 0.045, CL_WHITE if k % 2 else CL_YELLOW)
    a = np.asarray(C.img, np.float32)                                       # the shade of the folds: dark in the valleys, more so lower down
    uu = (np.arange(C.w) + 0.5) / C.w
    tt = 1.0 - (np.arange(C.h) + 0.5) / C.h
    fold = np.sin(uu * math.pi * 2 * FOLDS + 0.6)[None, :]
    deep = (0.25 + 0.75 * (1.0 - tt) ** 0.7)[:, None]
    C.redraw(a * (1.0 + (0.10 * fold - 0.20 * np.clip(-fold, 0, 1)) * deep)[..., None])


P.add(sheet(banner_fn, 12, 16, 0.02, (BW, BT - BB)), banner_paint, k=1.25)
for k in range(5):                                                          # tassels on the hem
    u = 0.1 + k * 0.2
    x, y, z = banner_fn(u, 0.0)
    P.add(tube([(x, y + 0.02, z), (x, y - 0.10, z), (x + 0.005, y - 0.2, z)], [0.012, 0.026, 0.034], 4), lambda C: (C.fill(CL_YELLOW), C.band(0.7, 0.78, CL_RED), C.band(0.78, 1, CL_BLACK)), key="tassel", op="join")
# the top: a carved finial and three feathers
P.add(T(lathe([(0.045, 0.0), (0.085, 0.04), (0.06, 0.10), (0.085, 0.16), (0.0, 0.25)], 8), at=(0.02, 4.22, 0)), wood(DARK, bands=[(0.2, 0.5, "zig", OCHRE, CL_BLACK)]), key="finial")
for k, (ang, col) in enumerate(((-28, CL_BLACK), (0, CL_RED), (26, CL_WHITE))):
    f = pillow([(0, 0), (0.05, 0.08), (0.055, 0.26), (0, 0.40), (-0.055, 0.26), (-0.05, 0.08)], 0.02, ((1, 0), (0.5, 0.9), (0, 1)), centre=(0, 0.2))
    P.add(T(f, at=(0.02, 4.42, 0), turn=(0, 0, ang)), lambda C, c=col: (C.fill(c), C.line([(0.5, 0), (0.5, 1)], mix(c, (128, 128, 128), 0.5), C.lw(0.008)), C.band(0.86, 1.0, CL_YELLOW)), key="plume%d" % k, op="join")
tie(P, (0.0, 1.3, 0.0), 0.065, CL_RED, yaw=0, length=0.3, key="btie")

S.build()
