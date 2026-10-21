"""PAYO pit wall: the numbers the painter and the modeller agree on (pure Python).

The pit wall module is 12 m wide and 12 m tall, vertical, and its picture is the whole face: u = along / 12,
v = up / 12 (2048 x 2048 px, 170.7 px a metre, the same grain as the terrace wall). Every course ends flush at 0 and
at 12 so modules butt together and stack; the seam is covered by the pilaster each module carries on its left edge.
From the foot: six courses, the LEDGE course (4.4 to 4.8 m), four courses, the CARVED BAND (8.0 to 8.9 m), four courses.
"""
import random

W = 12.0
Hh = 12.0
JOINT = 0.045
LEDGE = (4.4, 4.8)
BAND = (8.0, 8.9)
STONES = [(156, 154, 148), (170, 167, 160), (148, 146, 142), (184, 180, 170), (162, 158, 150), (150, 147, 142),
          (190, 186, 178), (144, 142, 140), (176, 170, 158), (158, 154, 146)]
WARM = [(176, 168, 148), (182, 172, 150)]


def course_edges():
    r = random.Random(21)

    def split(a, b, n):
        hs = [r.uniform(0.85, 1.15) for _ in range(n)]
        k = (b - a) / sum(hs)
        out, z = [], a
        for h in hs:
            out.append((z, z + h * k)); z += h * k
        return out
    return split(0, LEDGE[0], 3) + [LEDGE] + split(LEDGE[1], BAND[0], 4) + [BAND] + split(BAND[1], Hh, 3)


def stones(seed=5):
    r = random.Random(seed)
    out = []
    for ci, (t0, t1) in enumerate(course_edges()):
        if (t0, t1) == BAND:
            for k in range(6):                                   # the carved band: six 2 m blocks
                out.append(dict(s0=k * 2.0, s1=k * 2.0 + 2.0, t0=t0, t1=t1, kind="band", out=0.17, bev=0.035, tone=(176, 172, 162), id=len(out)))
            continue
        s = 0.0
        first = True
        while s < W - 1e-6:
            big = (t1 - t0) > 1.2
            w = r.uniform(1.7, 3.1) if big else r.uniform(1.0, 2.1) if (t1 - t0) > 0.9 else r.uniform(0.85, 1.75)
            if first and ci % 2:
                w *= 0.55                                        # running bond: every other course starts with a short stone
            first = False
            if W - (s + w) < 0.7:
                w = W - s
            tone = r.choice(WARM) if r.random() < 0.05 else r.choice(STONES)
            f = r.uniform(0.88, 1.0)
            out.append(dict(s0=s, s1=s + w, t0=t0, t1=t1, kind="stone", out=r.uniform(0.12, 0.24) if big else r.uniform(0.07, 0.15), bev=0.09 if big else 0.06,
                            tone=tuple(min(255, int(c * f)) for c in tone), id=len(out)))
            s += w
    return out


def corners(st, seed=5):
    r = random.Random(seed * 1000 + st["id"])
    g = JOINT / 2
    j = (lambda: 0.0) if st["kind"] == "band" else (lambda: r.uniform(-0.03, 0.03))
    return [(st["s0"] + g + j(), st["t0"] + g + j()), (st["s1"] - g + j(), st["t0"] + g + j()),
            (st["s1"] - g + j(), st["t1"] - g + j()), (st["s0"] + g + j(), st["t1"] - g + j())]


def hole(variant):
    """The opening a variant leaves in the face: (s0, s1, t0, t1), or None."""
    if variant == "niche":
        return (4.5, 7.5, 4.85, 7.95)
    if variant == "spout":
        return (5.3, 6.7, 5.6, 7.0)
    return None
