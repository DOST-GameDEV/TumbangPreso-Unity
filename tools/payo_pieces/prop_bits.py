"""Small things several sets of PAYO props share: leaves, flowers, layered flame tongues, cloth ties, field stones."""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from prop_kit import *                                 # noqa: E402,F401,F403

def leaf_outline(length, width, n=6):
    top = [(-length / 2 + length * k / n, width / 2 * math.sin(math.pi * k / n) ** 0.7) for k in range(n + 1)]
    return top + [(x, -y) for x, y in reversed(top[1:-1])]


def leaf_paint(base=LEAF, light=LEAF_L):
    def fn(C):
        C.fill(base)
        C.box(0, 0.5, 1, 1, shade(base, 1.1))
        for k in range(9):
            u = 0.08 + k * 0.105
            C.line([(u, 0.5), (u + 0.08, 0.98)], shade(base, 0.74), C.lw(0.006)); C.line([(u, 0.5), (u + 0.08, 0.02)], shade(base, 0.74), C.lw(0.006))
        C.line([(0.0, 0.5), (1.0, 0.5)], light, C.lw(0.014))
    return fn


def flower_outline(r, petals=5, n=20):
    return [(r * (0.52 + 0.48 * abs(math.cos(petals * a / 2.0))) * math.cos(a), r * (0.52 + 0.48 * abs(math.cos(petals * a / 2.0))) * math.sin(a))
            for a in (2 * math.pi * k / n for k in range(n))]


def flower_paint(petal, heart=GOLD, petals=5):
    def fn(C):
        C.fill(petal)
        for k in range(petals):
            a = 2 * math.pi * k / petals
            C.line([(0.5, 0.5), (0.5 + 0.46 * math.cos(a), 0.5 + 0.46 * math.sin(a))], shade(petal, 1.22), C.lw(0.02))
            b = a + math.pi / petals
            C.line([(0.5, 0.5), (0.5 + 0.3 * math.cos(b), 0.5 + 0.3 * math.sin(b))], shade(petal, 0.68), C.lw(0.012))
        C.dot(0.5, 0.5, min(C.nat) * 0.19, heart, outline=shade(heart, 0.66), width=C.lw(0.008))
    return fn


def flower(P, at, r=0.07, col=RED, turn=(-90, 0, 0), heart=GOLD, key=None):
    P.add(T(pillow(flower_outline(r), r * 0.55, prof=((1, 0), (0.62, 0.9), (0, 0.7))), at=at, turn=turn), flower_paint(col, heart), key=key or "flower" + str(col))


def flame_outline(w, h, lean=0.0):
    pts = [(0.0, 0.0), (0.42, 0.06), (0.60, 0.26), (0.56, 0.46), (0.40, 0.66), (0.30, 0.84), (0.16, 1.0), (0.04, 0.82), (-0.18, 0.62), (-0.46, 0.44), (-0.56, 0.24), (-0.40, 0.06)]
    return [(x * w + lean * h * y * y, y * h) for x, y in pts]


def flame_paint(outer=ORANGE, mid=GOLD, core=(255, 244, 176), tip=RED):
    def fn(C):
        C.fill(outer)
        C.band(0.72, 1.0, tip); C.band(0.56, 0.72, mix(outer, tip, 0.5))
        C.poly([(0.5, 0.0), (0.8, 0.14), (0.78, 0.34), (0.6, 0.6), (0.52, 0.74), (0.4, 0.5), (0.22, 0.32), (0.22, 0.12)], mid)
        C.poly([(0.5, 0.0), (0.66, 0.1), (0.62, 0.26), (0.52, 0.44), (0.4, 0.26), (0.36, 0.1)], core)
    return fn


def flames(P, spots, group="flame", key="flame"):
    """Layered tongues: (x, y, z, width, height, yaw, lean)."""
    for x, y, z, w, h, yaw, lean in spots:
        P.add(T(pillow(flame_outline(w, h, lean), w * 0.5, centre=(0, h * 0.3)), at=(x, y, z), turn=(0, yaw, 0)), flame_paint(), key=key, group=group)



def tie(P, at, r, col=CL_RED, tails=2, key="tie", length=0.3, yaw=0.0):
    """A strip of cloth knotted round a post, its ends hanging."""
    x, y, z = at
    P.add(T(torus(r, r * 0.26, 0.0, 10, 4), at=at, scale=(1, 1.7, 1)), flat(col), key=key)
    a = math.radians(yaw)
    fx, fz = math.sin(a), math.cos(a)
    P.add(T(ball((r * 0.36, r * 0.36, r * 0.36), 6, 4), at=(x + fx * r * 1.15, y, z + fz * r * 1.15)), flat(shade(col, 0.85)), key=key + "k")
    for k in range(tails):
        s = (k - (tails - 1) / 2.0) * 0.9
        sx, sz = fz * s, -fx * s
        pts = [(x + fx * r * 1.15, y, z + fz * r * 1.15), (x + fx * r * 1.5 + sx * r, y - length * 0.4, z + fz * r * 1.5 + sz * r), (x + fx * r * 1.35 + sx * r * 1.6, y - length, z + fz * r * 1.35 + sz * r * 1.6)]
        m = tube(pts, [r * 0.2, r * 0.3, r * 0.36], 5)
        P.add(m, lambda C, c=col: (C.fill(c), C.band(0.78, 0.84, CL_YELLOW), C.band(0.86, 1.0, CL_BLACK)), key=key + "t")


def field_stone(P, half, at, seed, yaw=0.0, base=STONE, key=None, moss=0.0):
    st = boxed(lumpy(ball(half, 10, 6), 0.16, seed, 2.2), share=True)
    P.add(T(st, at=at, turn=(0, yaw, 0)), stone(base, bevel=0, cracks=2, moss=moss), key=key or "fstone%d" % seed)
