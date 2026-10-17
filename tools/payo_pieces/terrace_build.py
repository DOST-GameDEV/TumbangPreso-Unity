"""PAYO land kit, step 2: THE TERRACE MODULE (Blender).

  blender -b --python tools/payo_pieces/terrace_build.py

Writes Logs/payo/pieces/build/terrace/terrace_<shape>_<paddy>.glb for shape in straight, convex, concave, cap and
paddy in young, ripe, flooded, plus terrace_stats.json.

ONE MODULE: 12 m of one terrace. Pivot: the foot of the wall at its LEFT end as you face it from downhill. Blender
while building: +X along the wall, +Y into the hill, +Z up (in the glb: +x along, +y up, the wall faces +z). The wall
top is 1.15 m up; the next terrace up stands its wall on this one's paddy, 1.15 m higher and 1.2 to 2.5 m back.
The stones of the wall run on past the right end and leave matching gaps at the left, so modules lock together.
Objects in each file: wall (py_terrace_stone), dike and bed (py_terrace_ground), water (py_water), rice and weeds
(py_terrace_plant), spout (py_terrace_stone), spout_fall (py_fall), spout_foam (py_foam).
"""
import json
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrace_layout as TL   # noqa: E402
import terrace_lib as K       # noqa: E402
from mathutils import Vector  # noqa: E402

SB, CB = math.sin(TL.BATTER), math.cos(TL.BATTER)
D0 = TL.T * SB                                  # depth of the wall's top edge behind its foot
BEV = 0.05


class Way:
    """Where the wall runs: a heading that turns with distance."""

    def __init__(self, kind):
        self.kind = kind
        self.length = 6.0 if kind == "cap" else TL.L
        self.step = 0.02
        self.s0 = -1.0
        n = int((self.length + 2.0) / self.step) + 1
        self.tab = []
        x = y = 0.0
        # integrate from 0 backwards to -1 first
        pts = {}
        for sign in (-1, 1):
            x = y = 0.0
            s = 0.0
            pts[0] = (0.0, 0.0)
            for i in range(1, int((1.0 if sign < 0 else self.length + 1.0) / self.step) + 1):
                th = self.heading(s + sign * self.step * 0.5)
                x += sign * math.cos(th) * self.step
                y += sign * math.sin(th) * self.step
                s += sign * self.step
                pts[sign * i] = (x, y)
        self.pts = pts

    def heading(self, s):
        if self.kind == "convex":
            return s / 30.0
        if self.kind == "concave":
            return -s / 30.0
        if self.kind == "cap":
            k = max(0.0, min(1.0, s / self.length))
            return math.radians(78) * k * k
        return 0.0

    def base(self, s):
        """How high the hillside has risen up the wall's foot (the cap dies into the hill)."""
        if self.kind != "cap":
            return 0.0
        k = max(0.0, min(1.0, s / self.length))
        return (TL.T - 0.16) * k ** 1.5

    def paddy(self, s):
        if self.kind != "cap":
            return TL.PADDY
        k = max(0.0, min(1.0, s / self.length))
        return TL.PADDY * (1 - k) ** 1.3 + 0.02

    def at(self, s, d=0.0, z=0.0):
        i = s / self.step
        i0 = math.floor(i)
        a = self.pts.get(i0, self.pts[max(min(self.pts), min(max(self.pts), i0))])
        b = self.pts.get(i0 + 1, a)
        f = i - i0
        x, y = a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f
        th = self.heading(s)
        return Vector((x - math.sin(th) * d, y + math.cos(th) * d, z))

    def face(self, s, t, out=0.0):
        """A point on the leaning wall face: t metres up it, out metres proud of it."""
        return self.at(s, t * SB - out * CB, t * CB + out * SB)

    def uphill(self, s):
        th = self.heading(s)
        return Vector((-math.sin(th), math.cos(th), 0))

    def along(self, s):
        th = self.heading(s)
        return Vector((math.cos(th), math.sin(th), 0))


def uvw(s, t):
    return (s / TL.L, t / TL.TEX_V)


def build_wall(way):
    mb = K.MB()
    n = int(round(way.length))
    dark = (0.30, 0.30, 0.30, 1)
    for i in range(n):                                              # the joint-coloured backing
        a, b = float(i), float(i + 1)
        ta, tb = way.base(a), way.base(b)
        mb.face([way.face(a, ta), way.face(b, tb), way.face(b, TL.T), way.face(a, TL.T)],
                [uvw(a, ta), uvw(b, tb), uvw(b, TL.T), uvw(a, TL.T)], dark)
        mb.face([way.at(a, D0, TL.H - 0.04), way.at(b, D0, TL.H - 0.04), way.at(b, D0 + 0.26, TL.H - 0.04), way.at(a, D0 + 0.26, TL.H - 0.04)],
                [uvw(a, TL.T), uvw(b, TL.T), uvw(b, TL.T + 0.26), uvw(a, TL.T + 0.26)], dark)
    for st in TL.stones():
        if st["s0"] > way.length - 0.15:
            continue
        c = TL.corners(st)
        cs = (c[0][0] + c[1][0]) / 2
        tb = max(way.base(c[0][0]), way.base(c[1][0]))
        top_t = TL.T if st["top"] else max(c[2][1], c[3][1])
        if top_t - tb < 0.10:
            continue                                                # buried in the hillside
        o = st["out"]
        cl = lambda p: (p[0], max(p[1], way.base(p[0]) - 0.02))
        b0, b1 = cl(c[0]), cl(c[1])
        ins = lambda p, dx, dy: (p[0] + dx * BEV, p[1] + dy * BEV)
        f0, f1 = ins(b0, 1, 1), ins(b1, -1, 1)
        P = lambda p, out=0.0: way.face(p[0], p[1], out)
        if not st["top"]:
            b2, b3 = c[2], c[3]
            f2, f3 = ins(b2, -1, -1), ins(b3, 1, -1)
            if f2[1] <= f1[1] + 0.02:
                continue
            base, front = [b0, b1, b2, b3], [f0, f1, f2, f3]
            mb.face([P(p, o) for p in front], [uvw(*p) for p in front])
            for i in range(4):
                j = (i + 1) % 4
                mb.face([P(base[i]), P(base[j]), P(front[j], o), P(front[i], o)], [uvw(*base[i]), uvw(*base[j]), uvw(*front[j]), uvw(*front[i])])
            continue
        # a cap stone: its face, a pale chamfer over the top edge, and its top
        sl, sr = c[0][0], c[1][0]
        ztop = TL.H + st["lift"]
        tA = (ztop - 0.05 - o * SB) / CB
        A_l, A_r = (sl + BEV, tA), (sr - BEV, tA)
        btl, btr = (sl, TL.T), (sr, TL.T)
        dB = D0 - o * CB + 0.05
        B = lambda s: way.at(s, dB, ztop)
        C = lambda s: way.at(s, D0 + 0.27, ztop)
        uB = lambda s: uvw(s, TL.T + 0.02)
        uC = lambda s: uvw(s, TL.T + 0.27)
        mb.face([P(f0, o), P(f1, o), P(A_r, o), P(A_l, o)], [uvw(*f0), uvw(*f1), uvw(*A_r), uvw(*A_l)])
        mb.face([P(b0), P(b1), P(f1, o), P(f0, o)], [uvw(*b0), uvw(*b1), uvw(*f1), uvw(*f0)])
        mb.face([P(b1), P(btr), P(A_r, o), P(f1, o)], [uvw(*b1), uvw(*btr), uvw(*A_r), uvw(*f1)])
        mb.face([P(btl), P(b0), P(f0, o), P(A_l, o)], [uvw(*btl), uvw(*b0), uvw(*f0), uvw(*A_l)])
        mb.face([B(sr - BEV), B(sl + BEV), P(A_l, o), P(A_r, o)], [uB(sr - BEV), uB(sl + BEV), uvw(*A_l), uvw(*A_r)])
        mb.face([B(sl + BEV), B(sr - BEV), C(sr), C(sl)], [uB(sl + BEV), uB(sr - BEV), uC(sr), uC(sl)])
        mb.face([P(btr), C(sr), B(sr - BEV), P(A_r, o)], [uvw(*btr), uC(sr), uB(sr - BEV), uvw(*A_r)])
        mb.face([P(btl), P(A_l, o), B(sl + BEV), C(sl)], [uvw(*btl), uvw(*A_l), uB(sl + BEV), uC(sl)])
    return mb


BUND = [(0.13, 0.00), (0.16, 0.10), (0.25, 0.17), (0.38, 0.165), (0.49, 0.08), (0.55, -0.05), (0.60, -0.20)]


def bund_at(s, r_phase):
    k = 1 + 0.22 * math.sin(s * 1.7 + r_phase) + 0.12 * math.sin(s * 4.1 + 2 * r_phase)
    w = 0.03 * math.sin(s * 2.6 + r_phase * 3)
    return [(d + (w if i < 3 else 0), z * (k if 0 < i < 5 else 1)) for i, (d, z) in enumerate(BUND)]


def build_ground(way, variant):
    mb = K.MB()
    n = int(round(way.length * 2))
    ph = 1.3
    lens = [0.0]
    for i in range(1, len(BUND)):
        lens.append(lens[-1] + math.dist(BUND[i], BUND[i - 1]))
    for i in range(n):
        a, b = i * 0.5, (i + 1) * 0.5
        pa, pb = bund_at(a, ph), bund_at(b, ph)
        for k in range(len(BUND) - 1):
            va, vb = 1.0 - 0.5 * lens[k] / lens[-1], 1.0 - 0.5 * lens[k + 1] / lens[-1]
            q = [way.at(a, D0 + pa[k][0], TL.H + pa[k][1]), way.at(b, D0 + pb[k][0], TL.H + pb[k][1]),
                 way.at(b, D0 + pb[k + 1][0], TL.H + pb[k + 1][1]), way.at(a, D0 + pa[k + 1][0], TL.H + pa[k + 1][1])]
            mb.face(q, [(a / 4, va), (b / 4, va), (b / 4, vb), (a / 4, vb)], smooth=True)
        # the paddy bed under the water
        zb = TL.WATER_Z - (0.13 if variant != "ripe" else 0.05)
        da, db = D0 + 0.55, D0 + 0.55
        ea, eb = da + way.paddy(a), db + way.paddy(b)
        tint = (0.80, 0.86, 0.84, 1) if variant != "ripe" else (1, 1, 1, 1)
        mb.face([way.at(a, da, zb), way.at(b, db, zb), way.at(b, eb, zb), way.at(a, ea, zb)],
                [(a / 4, 0.48), (b / 4, 0.48), (b / 4, 0.02), (a / 4, 0.02)], tint)
    return mb


def build_water(way):
    mb = K.MB()
    n = int(round(way.length))
    for i in range(n):
        a, b = float(i), float(i + 1)
        q = [way.at(a, D0 + 0.50, TL.WATER_Z), way.at(b, D0 + 0.50, TL.WATER_Z),
             way.at(b, D0 + 0.55 + way.paddy(b), TL.WATER_Z), way.at(a, D0 + 0.55 + way.paddy(a), TL.WATER_Z)]
        mb.face(q, [(p.x / 4, p.y / 4) for p in q])
    return mb


def build_plants(way, variant, seed):
    r = random.Random(seed)
    mb = K.MB()
    zw = TL.WATER_Z - 0.03
    rows = int((TL.PADDY - 0.10) / 0.30)
    if variant in ("young", "ripe"):
        for row in range(rows):
            d = 0.72 + row * 0.30
            s = 0.15
            while s < way.length - 0.05:
                if d - 0.55 < way.paddy(s) - 0.12:
                    p = way.at(s + r.uniform(-0.04, 0.04), D0 + d + r.uniform(-0.03, 0.03), zw)
                    if variant == "young":
                        tall = (0.74 if row == 0 else 0.56 if row == 1 else 0.44) * r.uniform(0.9, 1.1)
                        K.tuft(mb, p, r, n=6 if row == 0 else 5 if row == 1 else 4, h=tall, w=0.085 if row == 0 else 0.07, lean=0.42, cell=0,
                               shade=r.uniform(0.9, 1.0) * (1.0 if row == 0 else 0.92))
                    else:
                        tall = (0.74 if row == 0 else 0.66) * r.uniform(0.9, 1.1)
                        K.tuft(mb, p, r, n=4, h=tall, w=0.058, lean=0.36, cell=1, shade=r.uniform(0.9, 1.0))
                        K.tuft(mb, p, r, n=3, h=tall * 1.05, w=0.075, lean=0.75, cell=2, shade=1.0, droop=0.16, spread=0.02)
                s += (0.22 if row == 0 else 0.28) if variant == "young" else (0.26 if row == 0 else 0.33)
    else:
        # just flooded: bare mirror water, bundles of seedlings waiting to be planted, a few already in
        for _ in range(int(5 * way.length / 12) + 1):
            s, d = r.uniform(0.6, way.length - 0.6), r.uniform(0.75, 1.4)
            if d - 0.55 < way.paddy(s) - 0.2:
                p = way.at(s, D0 + d, zw)
                K.tuft(mb, p, r, n=7, h=0.40, w=0.05, lean=0.22, cell=0, shade=0.95, spread=0.05)
                K.tuft(mb, p, r, n=5, h=0.34, w=0.05, lean=0.5, cell=0, shade=0.85, spread=0.07)
        s = 0.3
        while s < min(way.length, 5.0):                               # one corner already planted, tiny and sparse
            for row in range(2):
                if 0.2 + row * 0.3 < way.paddy(s) - 0.2:
                    K.tuft(mb, way.at(s, D0 + 0.75 + row * 0.30, zw), r, n=3, h=0.20, w=0.04, lean=0.4, cell=0, shade=0.95)
            s += 0.30
    # grass on the dike, hanging over the stone, and standing on the crest
    s = 0.1
    while s < way.length:
        ph = bund_at(s, 1.3)
        if r.random() < 0.8:
            p = way.at(s, D0 + ph[1][0] - 0.01, TL.H + ph[1][1] * 0.6)
            dn = -way.uphill(s)
            for k in range(r.randint(3, 5)):
                o = (dn + way.along(s) * r.uniform(-0.7, 0.7)).normalized()
                K.blade(mb, p + way.along(s) * r.uniform(-0.08, 0.08), o, r.uniform(0.10, 0.20), 0.055, r.uniform(1.2, 2.2), 3, r,
                        shade=r.uniform(0.8, 1.0), droop=r.uniform(0.5, 1.1))
        if r.random() < 0.45:
            p = way.at(s + 0.1, D0 + ph[2][0] + r.uniform(0, 0.1), TL.H + ph[2][1] - 0.02)
            K.tuft(mb, p, r, n=5, h=r.uniform(0.16, 0.28), w=0.045, lean=0.7, cell=3, shade=r.uniform(0.85, 1.0))
            if r.random() < 0.35:                                     # a red flower in the grass
                top = p + Vector((r.uniform(-0.05, 0.05), r.uniform(-0.05, 0.05), r.uniform(0.26, 0.34)))
                K.blade(mb, p, -way.uphill(s), (top - p).z, 0.018, 0.1, 3, r, shade=0.8)
                for k in range(5):
                    a = k * 6.283 / 5
                    K.blade(mb, top, Vector((math.cos(a), math.sin(a), 0)), 0.035, 0.055, 2.4, 6, r)
        s += r.uniform(0.22, 0.42)
    # weeds hanging over the cap stones: the green band reaches down the wall (cheat: thicker than life)
    s = 0.05
    while s < way.length:
        if r.random() < 0.72 and not (3.1 < s < 3.7):
            for k in range(r.randint(1, 3)):
                ss = s + r.uniform(-0.06, 0.06)
                p = way.at(ss, D0 - 0.03, TL.H + 0.06)
                o = (-way.uphill(ss) + way.along(ss) * r.uniform(-0.35, 0.35)).normalized()
                ln = r.uniform(0.14, 0.40)
                K.blade(mb, p, o, 0.09, r.uniform(0.07, 0.10), 0.48, 3, r, shade=r.uniform(0.72, 1.0), droop=ln / 0.09 + 1)
        s += 0.15
    # ferns rooted in the joints of the wall
    sts = [st for st in TL.stones() if st["s0"] < way.length - 0.6 and st["t0"] > 0.05]
    r.shuffle(sts)
    for st in sts[: max(2, int(9 * way.length / 12))]:
        s, t = st["s0"], st["t0"] + r.uniform(0, 0.03)
        if t < way.base(s) + 0.1:
            continue
        p = way.face(s, t, 0.03)
        dn = -way.uphill(s)
        for k in range(r.randint(3, 5)):
            o = (dn + way.along(s) * r.uniform(-1.1, 1.1)).normalized()
            K.leaf(mb, p, o, r.uniform(0.26, 0.42), 0.13, 4, r, up=r.uniform(0.1, 0.5), shade=r.uniform(0.85, 1.0))
    return mb


def build_spout(way, s=3.4):
    """A carved stone trough through the dike, and the thin fall it drops to the paddy below."""
    stone, fall, foam = K.MB(), K.MB(), K.MB()
    al, dn = way.along(s), -way.uphill(s)
    z0 = TL.WATER_Z - 0.06
    o = way.at(s, D0 + 0.62, z0)
    st = next(x for x in TL.stones() if not x["top"] and x["s1"] - x["s0"] > 0.5)   # a plain stone's paint for the trough
    c = TL.corners(st)
    u0, v0 = uvw(c[0][0] + 0.03, c[0][1] + 0.03); u1, v1 = uvw(c[2][0] - 0.03, c[2][1] - 0.03)

    def pt(a, f, z):                                                 # a across, f forward (downhill), z up from the trough floor
        return o + al * a + dn * f + Vector((0, 0, z - f * 0.07))

    length = 1.12
    for a0, a1, zt, zb in ((-0.17, -0.07, 0.15, -0.09), (0.07, 0.17, 0.15, -0.09), (-0.07, 0.07, 0.0, -0.09)):
        corners = [pt(a0, 0, zb), pt(a1, 0, zb), pt(a1, length, zb), pt(a0, length, zb), pt(a0, 0, zt), pt(a1, 0, zt), pt(a1, length, zt), pt(a0, length, zt)]
        # faces wound for a box whose "forward" axis runs downhill
        for idx, shade in (((0, 3, 2, 1), 0.7), ((4, 5, 6, 7), 1.12), ((3, 7, 6, 2), 1.0), ((0, 4, 7, 3), 0.9), ((1, 2, 6, 5), 0.9), ((0, 1, 5, 4), 0.8)):
            stone.face([corners[i] for i in reversed(idx)], [(u0, v0), (u1, v0), (u1, v1), (u0, v1)], (shade, shade, shade, 1))
    # the water lying in the trough, then leaving its lip
    w0 = 0.07
    fall.face([pt(-w0, length, 0.05), pt(w0, length, 0.05), pt(w0, 0, 0.05), pt(-w0, 0, 0.05)], [(0.30, 1.0), (0.42, 1.0), (0.42, 0.5), (0.30, 0.5)])
    tip = pt(0, length, 0.05)
    drop = tip.z + 0.03
    prev, pw, pv = tip, w0, 0.5
    steps = 6
    for i in range(1, steps + 1):
        k = i / steps
        p = tip + dn * (0.30 * k ** 0.6) + Vector((0, 0, -drop * k ** 1.7))
        w = w0 + 0.05 * k
        v = 0.5 - 0.45 * k
        fall.face([prev - al * pw, prev + al * pw, p + al * w, p - al * w], [(0.30, pv), (0.42, pv), (0.42, v), (0.30, v)], smooth=True)
        fall.face([prev - dn * pw * 0.6, prev + dn * pw * 0.6, p + dn * w * 0.6, p - dn * w * 0.6], [(0.55, pv), (0.62, pv), (0.62, v), (0.55, v)], smooth=True)
        prev, pw, pv = p, w, v
    r = random.Random(4)
    for k in range(7):                                                # a little ring of foam blobs where it lands
        a = k * 6.283 / 7
        q = prev + Vector((math.cos(a) * 0.16, math.sin(a) * 0.16, 0.0))
        K.blob(foam, (q.x, q.y, 0.0), (r.uniform(0.07, 0.11), r.uniform(0.07, 0.11), 0.05), r, seg=6, rings=3, jitter=0.1,
               colour=lambda n, p: (1, 1, 1, 1) if n.z > 0 else (0.75, 0.9, 0.92, 1), flat_bottom=-0.02)
    return stone, fall, foam


def build_seeps(way, at=(8.3,)):
    """Thin overflow trickles down the face of the wall: the glint of water you see from BELOW, where the paddy
    itself is hidden above your eye. A painter's cheat; the object is separate (seep) so it can be left out."""
    mb = K.MB()
    for n, s in enumerate(at):
        w0 = 0.035
        steps = 4
        for i in range(steps):
            ta, tb = TL.T + 0.02 - (TL.T + 0.02) * i / steps, TL.T + 0.02 - (TL.T + 0.02) * (i + 1) / steps
            wa, wb = w0 + 0.03 * i / steps, w0 + 0.03 * (i + 1) / steps
            oa, ob = (0.16, 0.15)
            u0 = 0.70 + 0.1 * n
            mb.face([way.face(s - wa, ta, oa), way.face(s - wb, tb, ob), way.face(s + wb, tb, ob), way.face(s + wa, ta, oa)],
                    [(u0, 0.9 - ta / 2), (u0, 0.9 - tb / 2), (u0 + 0.07, 0.9 - tb / 2), (u0 + 0.07, 0.9 - ta / 2)][::1])
    return mb


def make(kind, variant, seed=3):
    way = Way(kind)
    m_stone = K.material("py_terrace_stone", "py_terrace_wall.png")
    m_ground = K.material("py_terrace_ground", "py_terrace_ground.png")
    m_plant = K.material("py_terrace_plant", "py_terrace_plant.png")
    m_water = K.material("py_water", None, colour=(0.30, 0.62, 0.66, 0.85))
    m_fall = K.material("py_fall", "py_fall.png", alpha=True)
    m_foam = K.material("py_foam", "py_foam.png")
    name = "terrace_%s_%s" % (kind, variant)
    obs = [build_wall(way).obj(name + "_wall", m_stone), build_ground(way, variant).obj(name + "_dike", m_ground),
           build_water(way).obj(name + "_water", m_water), build_plants(way, variant, seed).obj(name + "_plants", m_plant)]
    if kind != "cap":
        st, fa, fo = build_spout(way)
        obs += [st.obj(name + "_spout", m_stone), fa.obj(name + "_spout_fall", m_fall), fo.obj(name + "_spout_foam", m_foam)]
        obs.append(build_seeps(way).obj(name + "_seep", m_fall))
    end = way.at(way.length)
    meta = dict(pivot="foot of the wall, left end", end=[round(end.x, 3), round(end.y, 3)], end_heading_deg=round(math.degrees(way.heading(way.length)), 2),
                wall_height=TL.H, parts={o.name.replace(name + "_", ""): sum(len(p.vertices) - 2 for p in o.data.polygons) for o in obs})
    return name, obs, meta


if __name__ == "__main__":
    stats = {}
    for kind in ("straight", "convex", "concave", "cap"):
        for variant in ("young", "ripe", "flooded"):
            K.reset()
            name, obs, meta = make(kind, variant)
            stats[name] = K.export(name, obs, meta)
    path = os.path.join(K.OUT, "terrace_stats.json")
    old = json.load(open(path)) if os.path.exists(path) else {}
    old.update(stats)
    json.dump(old, open(path, "w"), indent=1)
    print("done")
