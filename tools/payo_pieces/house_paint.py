"""Payo town buildings: the painter (system Python, numpy and Pillow). See tools/payo_pieces/house_kit.py.

  python tools/payo_pieces/house_paint.py <name>             paints Logs/payo/pieces/build/house/<name>.png
  python tools/payo_pieces/house_paint.py sheet <name> <vN>  lays the four review pictures out as one sheet
  python tools/payo_pieces/house_paint.py street <vN>        the street picture beside reference 239

Every pixel knows its place in the building in metres, so stones are laid in real courses, paint is worn where rain
runs, moss sits in the joints and at the foot, and the pale bevel is drawn along the mesh's own sharp edges.
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
WORK = os.path.join(ROOT, "Logs", "payo", "pieces", "house", "work")
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "house")
SHEETS = os.path.join(ROOT, "Logs", "payo", "pieces", "house")

JOINT = np.array([0.17, 0.155, 0.15])
PALE = np.array([0.95, 0.93, 0.88])
MOSS = [np.array([0.22, 0.34, 0.20]), np.array([0.33, 0.47, 0.25]), np.array([0.50, 0.60, 0.30])]
STONE = [[0.56, 0.54, 0.51], [0.47, 0.455, 0.44], [0.64, 0.62, 0.58], [0.52, 0.50, 0.46], [0.60, 0.57, 0.55]]   # grey stone: the cool tone is the light


# ------------------------------------------------------------------ noise
def hashf(a, b, c, seed=0):
    h = (a.astype(np.int64) * 374761393 + b.astype(np.int64) * 668265263 + c.astype(np.int64) * 1442695041 + seed * 1274126177) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF) / float(0x1000000)


def vnoise(x, y, z, seed=0):
    x0, y0, z0 = np.floor(x), np.floor(y), np.floor(z)
    fx, fy, fz = x - x0, y - y0, z - z0
    fx, fy, fz = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy), fz * fz * (3 - 2 * fz)
    out = 0
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                w = (fx if dx else 1 - fx) * (fy if dy else 1 - fy) * (fz if dz else 1 - fz)
                out = out + w * hashf(x0 + dx, y0 + dy, z0 + dz, seed)
    return out


def fbm(x, y, z, seed=0, octaves=3):
    out, amp, tot = 0, 1.0, 0
    for o in range(octaves):
        out = out + amp * vnoise(x * 2 ** o, y * 2 ** o, z * 2 ** o, seed + o * 17)
        tot += amp; amp *= 0.5
    return out / tot


def mix(a, b, t):
    t = np.asarray(t, dtype=np.float32)
    if t.ndim == 2:
        t = t[..., None]
    return a * (1 - t) + b * t


def C(c):
    return np.array(c, dtype=np.float32)


# ------------------------------------------------------------------ pixel to world
class G:
    def __init__(self, part, g, pi):
        self.part, self.g, self.seed = part, g, pi * 7 + 3
        self.w, self.h, self.dir = g["pw"], g["ph"], g["dir"]
        pad = PAD
        i = (np.arange(self.w) - pad + 0.5)[None, :].repeat(self.h, 0)
        j = (np.arange(self.h) - pad + 0.5)[:, None].repeat(self.w, 1)
        self.a = g["u0"] + i / g["ppu"]
        self.b = g["v1"] - j / g["ppv"]
        dep = np.full_like(self.a, g["depth"])
        d = self.dir
        if d == "y-": self.X, self.Y, self.Z = self.a, dep, self.b
        elif d == "y+": self.X, self.Y, self.Z = -self.a, dep, self.b
        elif d == "x+": self.X, self.Y, self.Z = dep, self.a, self.b
        elif d == "x-": self.X, self.Y, self.Z = dep, -self.a, self.b
        elif d == "z+": self.X, self.Y, self.Z = self.a, self.b, dep
        else: self.X, self.Y, self.Z = self.a, -self.b, dep
        self.side = d[0] != "z"
        self.lo, self.hi = part["lo"], part["hi"]
        self.wm, self.hm = (self.w - 2 * pad) / g["ppu"], (self.h - 2 * pad) / g["ppv"]
        self.ac, self.bc = g["u0"] + self.wm / 2, g["v1"] - self.hm / 2

    def n(self, s, seed=0, octaves=3):
        return fbm(self.X * s, self.Y * s, self.Z * s, self.seed * 0 + seed, octaves)

    def to_px(self, pt):
        x, y, z = pt
        d = self.dir
        u = {"y-": x, "y+": -x, "x+": y, "x-": -y, "z+": x, "z-": x}[d]
        v = z if self.side else (y if d == "z+" else -y)
        return (PAD + (u - self.g["u0"]) * self.g["ppu"], PAD + (self.g["v1"] - v) * self.g["ppv"])


# ------------------------------------------------------------------ stone
def blocks(a, b, bw, ch, seed):
    """Irregular coursed blocks. Returns sdf (metres, negative inside), light side mask value, block ids."""
    tb = b / ch
    k = np.floor(tb)
    jb = lambda k: (hashf(k, k * 0 + 5, k * 0, seed) - 0.5) * 0.36
    k = np.where(tb < k + jb(k), k - 1, k)
    k = np.where(tb >= k + 1 + jb(k + 1), k + 1, k)
    bot, top = (k + jb(k)) * ch, (k + 1 + jb(k + 1)) * ch
    wid = bw * (0.7 + 0.7 * hashf(k, k * 0 + 9, k * 0, seed))
    ta = a / wid + hashf(k, k * 0 + 2, k * 0, seed) * 7
    m = np.floor(ta)
    ja = lambda m: (hashf(m, k, m * 0 + 1, seed) - 0.5) * 0.5
    m = np.where(ta < m + ja(m), m - 1, m)
    m = np.where(ta >= m + 1 + ja(m + 1), m + 1, m)
    left, right = (m + ja(m)) * wid, (m + 1 + ja(m + 1)) * wid
    aa = ta * wid
    hw, hh = (right - left) / 2, (top - bot) / 2
    px, pz = aa - (left + right) / 2, b - (bot + top) / 2
    return rsdf(px, pz, hw, hh, 0.09), px / np.maximum(hw, 1e-3), pz / np.maximum(hh, 1e-3), m, k


def rsdf(px, pz, hw, hh, r):
    r = np.minimum(r, np.minimum(hw, hh) * 0.6)
    qx, qz = np.abs(px) - (hw - r), np.abs(pz) - (hh - r)
    return np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qz, 0) ** 2) + np.minimum(np.maximum(qx, qz), 0) - r


FRET = np.array([[1, 1, 1, 1, 1, 0], [1, 0, 0, 0, 0, 0], [1, 0, 1, 1, 1, 0], [1, 0, 1, 0, 1, 0], [1, 0, 0, 0, 1, 0], [1, 1, 1, 1, 1, 0]])
STEP = np.array([[0, 0, 1, 1, 0, 0], [0, 1, 1, 1, 1, 0], [1, 1, 0, 0, 1, 1], [1, 1, 0, 0, 1, 1], [0, 1, 1, 1, 1, 0], [0, 0, 1, 1, 0, 0]])


def pattern(pat, a, fz, period):
    t = (a / period) % 1.0
    if pat == "zig":
        tri = np.abs(t * 2 - 1)
        return np.abs(fz - (0.2 + tri * 0.6)) < 0.14
    if pat == "dia":
        d = np.abs(t - 0.5) * 2 + np.abs(fz - 0.5) * 2
        return (d < 0.82) & ((d > 0.5) | (d < 0.22))
    if pat == "tri":
        return fz < 1 - np.abs(t * 2 - 1)
    if pat == "dot":
        return (t - 0.5) ** 2 + ((fz - 0.5) * 0.9) ** 2 < 0.07
    if pat == "stripe":
        return t < 0.5
    tab = FRET if pat == "fret" else STEP
    ci = np.clip((t * tab.shape[1]).astype(int), 0, tab.shape[1] - 1)
    ri = np.clip(((1 - fz) * tab.shape[0]).astype(int), 0, tab.shape[0] - 1)
    return (tab[ri, ci] == 1) & (fz > 0.02) & (fz < 0.98)


def stone(g, single=False):
    P = g.part["params"]
    pal = [C(c) for c in P.get("base", STONE)]
    seed = g.seed
    a, b = g.a, g.b
    if single:
        hw, hh = g.wm / 2, g.hm / 2
        px, pz = a - g.ac, b - g.bc
        sdf = rsdf(px, pz, hw, hh, 0.10) - 0.03
        nx, nz = px / hw, pz / hh
        idm, idk = a * 0 + seed, a * 0 + 3
        sub = P.get("sub")
        if sub:                                                   # a slab drawn as a few big stones
            sdf2, nx, nz, idm, idk = blocks(a, b, sub, max(g.hm, 0.2) * 1.02 if g.side else sub * 0.8, seed)
            sdf = np.maximum(sdf, sdf2)
    else:
        sdf, nx, nz, idm, idk = blocks(a, b, P.get("bw", 0.95), P.get("course", 0.52), seed)
    hb = hashf(idm, idk, idm * 0 + 4, seed)
    hb2 = hashf(idm, idk, idm * 0 + 8, seed)
    col = np.zeros(a.shape + (3,), np.float32)
    idx = (hb * len(pal)).astype(int) % len(pal)
    for i, c in enumerate(pal):
        col[idx == i] = c
    col *= (0.90 + 0.18 * hb2)[..., None]
    low = g.n(0.35, 11)
    col *= (1 + 0.10 * (np.round(low * 4) / 4 - 0.5))[..., None]
    # dabs of a second tone inside the block: brush marks, not noise
    dab = g.n(2.6, 21, 2)
    col = mix(col, col * C([0.90, 0.90, 0.91]), dab > 0.60)
    col = mix(col, col * 1.10 + 0.02, dab < 0.36)

    paint = None
    pt = P.get("paint")
    if pt:
        pm = g.n(pt.get("scale", 0.55), 31) * 0.65 + 0.35 * hb
        ramp = 0
        if g.side and pt.get("fade", 1.0) > 0:
            ramp = np.clip((g.Z - g.lo[2]) / max(g.hi[2] - g.lo[2], 0.1), 0, 1) * 0.22 * pt.get("fade", 1.0) - 0.11
        pm = pm + ramp
        thr = 1 - pt.get("cover", 0.6)
        paint = pm > thr * 0.9
        pc = C(pt["col"]) * (0.93 + 0.12 * hb2)[..., None]
        pc = mix(pc, pc * C([0.91, 0.91, 0.93]), dab > 0.62)
        pc = mix(pc, pc * 1.07 + 0.02, dab < 0.33)
        wash = g.n(0.9, 37)
        pc = mix(pc, mix(pc, PALE, 0.30), wash > 0.60)
        pc = mix(pc, pc * 0.88, wash < 0.36)
        rim = paint & (pm < thr * 0.9 + 0.028)
        col = mix(col, pc, paint)
        col = mix(col, col * 0.70, rim)
        col = mix(col, np.minimum(col * 1.25, 1), (~paint) & (pm > thr * 0.9 - 0.02))

    for bd in P.get("bands", []):
        if not g.side:
            continue
        fz = (g.Z - bd["z0"]) / (bd["z1"] - bd["z0"])
        inb = (fz >= 0) & (fz <= 1)
        worn = g.n(1.1, 41) > bd.get("wear", 0.30)
        bc = C(bd["col"]) * (0.94 + 0.10 * hb2)[..., None]
        col = mix(col, bc, inb & worn)
        if bd.get("pat"):
            on = pattern(bd["pat"], a + bd.get("off", 0), np.clip(fz * 1.3 - 0.15, 0, 1), bd.get("period", 0.8)) & inb & (fz > 0.12) & (fz < 0.88)
            col = mix(col, C(bd["col2"]), on & (g.n(1.7, 43) > bd.get("wear", 0.30) - 0.05))
        line = inb & ((fz < 0.07) | (fz > 0.93))
        col = mix(col, col * 0.62, line & worn)

    # the block's own painted bevel: light from the upper left, shade lower right, a dark joint
    lit = (-nx + nz)
    bev = P.get("bev", 0.075)
    rimz = sdf > -bev
    pk = 0.0 if paint is None else paint.astype(np.float32)[..., None]      # under paint the stones only ghost through
    col = mix(col, mix(col, PALE, 0.42 - 0.24 * pk), rimz & (lit > 0.1))
    col = mix(col, col * mix(C([0.72, 0.71, 0.72]), C([0.90, 0.89, 0.90]), pk), rimz & (lit < -0.1))
    jw = P.get("joint", 0.022)
    ghost = hb2 > 0.35
    col = mix(col, mix(JOINT + col * 0, col * np.where(ghost, 0.74, 0.93)[..., None], pk), sdf > -jw)

    # cracks
    cr = hb2 < P.get("cracks", 0.16)
    wob = (vnoise(nz * 2.5 + hb * 9, idm * 0.37, idk * 0.71, seed) - 0.5) * 1.1 + (hb - 0.5) * 0.8
    crack = cr & (np.abs(nx - wob) < 0.035 / np.maximum(g.wm if single else P.get("bw", 0.95), 0.3) * 1.6) & (nz > -0.2 - hb) & (sdf < -0.02)
    col = mix(col, JOINT * 1.1, crack)

    # moss: out of the joints, thick at the foot and on anything that faces the sky
    amt = P.get("moss", 0.5)
    mm = g.n(1.25, 51) * 0.75 + 0.25 * g.n(4.0, 52, 2)
    mm = mm + 0.10 * np.clip((sdf + 0.10) / 0.10, 0, 1)
    if g.side:
        mm = mm + 0.20 * np.clip(1 - (g.Z - max(g.lo[2], P.get("ground", -9))) / 0.7, 0, 1) * (1 if P.get("foot", True) else 0)
        mm = mm + 0.16 * np.clip(1 - (g.hi[2] - g.Z) / 0.35, 0, 1) * P.get("topmoss", 0.6)
    else:
        mm = mm + 0.10
    thr = 0.805 - amt * 0.22
    col = mix(col, MOSS[0], mm > thr)
    col = mix(col, MOSS[1], mm > thr + 0.045)
    col = mix(col, MOSS[2], mm > thr + 0.115)

    if g.side:
        ev = P.get("eave")
        if ev:
            dz = ev - g.Z
            col *= mix(C([0.68, 0.67, 0.69]), C([1, 1, 1]), np.clip(dz / P.get("eave_len", 0.7), 0, 1))
            s = vnoise(a * 2.2, a * 0 + seed, a * 0, 61)
            ln = 0.5 + 2.4 * vnoise(a * 1.1, a * 0 + 3, a * 0, 62)
            col = mix(col, col * C([0.80, 0.79, 0.78]), (s > 0.60) & (dz < ln) & (dz > 0))
        gz = P.get("ground")
        if gz is not None:
            col *= mix(C([0.74, 0.74, 0.70]), C([1, 1, 1]), np.clip((g.Z - gz) / 0.5, 0, 1))
    return col


# ------------------------------------------------------------------ thatch, timber, weave, cloth
def thatch(g):
    P = g.part["params"]
    base = C(P.get("col", [0.80, 0.62, 0.30]))
    dark = C(P.get("dark", [0.42, 0.30, 0.20]))
    lh = P.get("layer", 0.55)
    if g.dir == "z-":                                             # the underside: dark, ribbed with rafters
        ang = np.arctan2(g.Y - P.get("cy", 0), g.X - P.get("cx", 0))
        rad = np.hypot(g.X - P.get("cx", 0), g.Y - P.get("cy", 0))
        rib = np.abs(((ang / (2 * math.pi) * 24) % 1) - 0.5) < 0.10
        purlin = np.abs(((rad / 0.9) % 1) - 0.5) < 0.07
        col = np.zeros(g.a.shape + (3,), np.float32) + dark * 1.05
        col = mix(col, dark * 0.85, vnoise(ang * 30, rad * 2.0, rad * 0, g.seed) > 0.6)
        col = mix(col, dark * 1.3, purlin)
        return mix(col, C([0.58, 0.43, 0.28]), rib)
    a = g.a if g.side else np.arctan2(g.Y - P.get("cy", 0), g.X - P.get("cx", 0)) * 2.0
    z = g.Z if g.side else -np.hypot(g.X - P.get("cx", 0), g.Y - P.get("cy", 0)) * 0.8
    rag = (vnoise(a * 5.0, a * 0 + 1, a * 0, g.seed) - 0.5) * 0.30 + (vnoise(a * 17.0, a * 0 + 2, a * 0, g.seed) - 0.5) * 0.12
    t = (z + rag * lh) / lh
    li = np.floor(t)
    f = t - li
    strand = vnoise(a * 22.0, li * 3.1, a * 0, g.seed + 1)
    clump = vnoise(a * 3.0, li * 1.7, a * 0, g.seed + 2)
    col = np.zeros(a.shape + (3,), np.float32) + base
    col = mix(col, base * C([0.84, 0.80, 0.74]), strand > 0.62)
    col = mix(col, np.minimum(base * 1.16 + 0.03, 1), strand < 0.30)
    col *= (0.88 + 0.22 * np.round(clump * 3) / 3)[..., None]
    col = mix(col, dark, (f > 0.80) | ((f > 0.66) & (strand > 0.5)))        # tucked under the layer above
    col = mix(col, np.minimum(base * 1.28 + 0.05, 1), (f < 0.16) & (strand < 0.66))   # sunlit tips
    col = mix(col, dark * 0.8, (f < 0.045))
    old = g.n(0.6, 71) > P.get("age", 0.62)
    col = mix(col, col * C([0.70, 0.78, 0.66]), old)
    mm = g.n(1.0, 72) * 0.8 + 0.2 * g.n(4.0, 73, 2) + (0.10 if not g.side else 0.0)
    thr = 0.78 - P.get("moss", 0.4) * 0.2
    col = mix(col, MOSS[1] * 0.9, mm > thr)
    col = mix(col, MOSS[2], mm > thr + 0.06)
    for bd in P.get("bands", []):                                          # a dyed or painted band across the thatch
        fz = (g.Z - bd["z0"]) / (bd["z1"] - bd["z0"])
        inb = (fz >= 0) & (fz <= 1) & g.side
        col = mix(col, C(bd["col"]) * (0.86 + 0.24 * strand)[..., None], inb & (g.n(1.4, 74) > 0.25))
        if bd.get("pat"):
            col = mix(col, C(bd["col2"]), inb & pattern(bd["pat"], g.a, fz, bd.get("period", 0.7)))
    return col


def wood(g):
    P = g.part["params"]
    base = C(P.get("col", [0.46, 0.30, 0.20]))
    ex = [g.hi[i] - g.lo[i] for i in range(3)]
    if g.side:
        horiz = ex[0] if g.dir[0] == "y" else ex[1]
        along_z = ex[2] >= horiz
    else:
        along_z = False
    grain = P.get("grain")
    if grain == "z": along_z = True
    if grain == "h": along_z = False
    if g.side:
        L, A = (g.b, g.a) if along_z else (g.a, g.b)
    else:
        L, A = (g.a, g.b) if ex[0] >= ex[1] else (g.b, g.a)
    plank = P.get("plank")
    col = np.zeros(g.a.shape + (3,), np.float32) + base
    pid = L * 0
    if plank:
        pid = np.floor(A / plank + 0.3)
        fa = A / plank + 0.3 - pid
        col *= (0.86 + 0.26 * hashf(pid, pid * 0 + g.seed, pid * 0))[..., None]
        col = mix(col, col * 0.45, (fa < 0.07))
        col = mix(col, np.minimum(col * 1.25, 1), (fa > 0.07) & (fa < 0.17))
    gr = vnoise(A * 26.0 + pid * 3.3, L * 0.9, L * 0 + g.seed, 5)
    col = mix(col, col * 0.80, gr > 0.62)
    col = mix(col, np.minimum(col * 1.14 + 0.02, 1), gr < 0.28)
    kn = vnoise(A * 3.2 + pid * 1.7, L * 1.3, L * 0 + g.seed, 6)
    col = mix(col, col * 0.62, (kn > 0.80))
    col = mix(col, col * 0.80, (kn > 0.74) & (kn <= 0.80))
    pt = P.get("paint")
    if pt:
        pm = g.n(pt.get("scale", 1.6), 33) * 0.7 + 0.3 * gr
        on = pm > 1 - pt.get("cover", 0.6)
        col = mix(col, C(pt["col"]) * (0.90 + 0.2 * gr)[..., None], on)
    for bd in P.get("bands", []):
        fz = (L - bd["z0"]) / (bd["z1"] - bd["z0"])
        inb = (fz >= 0) & (fz <= 1)
        col = mix(col, C(bd["col"]), inb & (g.n(2.0, 34) > 0.28))
        if bd.get("pat"):
            col = mix(col, C(bd["col2"]), inb & pattern(bd["pat"], A, fz, bd.get("period", 0.3)) & (g.n(2.0, 35) > 0.25))
    mm = g.n(1.6, 81)
    col = mix(col, MOSS[1] * 0.85, mm > 0.90 - P.get("moss", 0.2) * 0.3)
    if not g.side:
        col = np.minimum(col * 1.08, 1)
    return col


def weave(g):
    P = g.part["params"]
    c1, c2 = C(P.get("col", [0.82, 0.68, 0.40])), C(P.get("col2", [0.60, 0.44, 0.24]))
    s = P.get("strip", 0.16)
    i, j = np.floor(g.a / s), np.floor(g.b / s)
    fa, fb = g.a / s - i, g.b / s - j
    twill = ((i + j * 1) % 4) < 2
    col = np.where(twill[..., None], c1, c2) + np.zeros(g.a.shape + (3,), np.float32)
    acr = np.where(twill, fb, fa)
    col = col * (0.82 + 0.34 * np.sin(np.clip(acr, 0, 1) * math.pi))[..., None]
    col = mix(col, col * 0.5, (acr < 0.08) | (acr > 0.94))
    col *= (0.9 + 0.2 * hashf(i, j, i * 0, g.seed))[..., None]
    dp = P.get("diamond")
    if dp and g.side:
        d = np.abs(((g.a - g.ac) / dp) % 2 - 1) + np.abs(((g.b - g.bc) / dp) % 2 - 1)
        on = (np.abs(d - 1.0) < P.get("dline", 0.08))
        col = mix(col, C(P.get("col3", [0.72, 0.24, 0.18])) * (0.8 + 0.3 * np.sin(np.clip(acr, 0, 1) * math.pi))[..., None], on)
    col = mix(col, col * C([0.78, 0.77, 0.74]), g.n(0.9, 91) > 0.66)
    return col


def cloth(g):
    P = g.part["params"]
    base = C(P.get("col", [0.78, 0.20, 0.18]))
    stripes = P.get("stripes", [[0.10, 0.16, [0.95, 0.90, 0.78]], [0.20, 0.30, [0.12, 0.12, 0.16]], [0.70, 0.80, [0.95, 0.76, 0.24]]])
    f = np.clip((g.hi[2] - g.Z) / max(g.hi[2] - g.lo[2], 0.01), 0, 1) if g.side else g.a * 0 + 0.5
    col = np.zeros(g.a.shape + (3,), np.float32) + base
    for s0, s1, c in stripes:
        col = mix(col, C(c), (f > s0) & (f < s1))
    zz = pattern("zig", g.a, np.clip((f - 0.38) / 0.24, 0, 1), 0.22) & (f > 0.40) & (f < 0.60)
    col = mix(col, C(P.get("col2", [0.96, 0.90, 0.76])), zz)
    fold = np.sin(g.a * 14.0 + g.seed) * 0.5 + 0.5
    col *= (0.84 + 0.22 * np.round(fold * 3) / 3)[..., None]
    col = mix(col, col * 0.7, f > 0.965)
    return col


def flat(g):
    P = g.part["params"]
    base = C(P.get("col", [0.5, 0.5, 0.5]))
    col = np.zeros(g.a.shape + (3,), np.float32) + base
    n = g.n(P.get("scale", 3.0), 95)
    col = mix(col, col * 0.80, n > 0.60)
    col = mix(col, np.minimum(col * 1.18 + 0.03, 1), n < 0.34)
    if P.get("ringw"):
        r = np.hypot(g.a - g.ac, g.b - g.bc)
        col = mix(col, col * 0.62, (np.abs((r / P["ringw"]) % 1 - 0.5) < 0.10))
        col = mix(col, np.minimum(col * 1.5, 1), r < P["ringw"] * 0.9)
        col = mix(col, MOSS[1] * C([0.5, 1.0, 1.1]), g.n(1.8, 96) > 0.70)      # verdigris
    return col


def leaf(g):
    P = g.part["params"]
    base = C(P.get("col", [0.3, 0.6, 0.36]))
    col = np.zeros(g.a.shape + (3,), np.float32) + base
    n = g.n(2.2, 97)
    col = mix(col, col * C([0.78, 0.86, 0.80]), n > 0.58)
    col = mix(col, np.minimum(col * 1.22 + 0.03, 1), n < 0.36)
    return col


def glow(g):
    P = g.part["params"]
    hot, warm = C(P.get("hot", [1.0, 0.92, 0.55])), C(P.get("col", [0.98, 0.62, 0.20]))
    fx, fz = (g.a - g.ac) / (g.wm / 2), (g.b - g.bc) / (g.hm / 2)
    r = np.sqrt(fx ** 2 * 0.8 + (fz + 0.25) ** 2)
    col = mix(hot, warm, np.clip(r, 0, 1))
    lat = P.get("lattice", "grid")
    bar = C(P.get("bar", [0.20, 0.13, 0.10]))
    if lat == "grid":
        nx, nz = P.get("nx", 2), P.get("nz", 3)
        on = (np.abs(((fx + 1) / 2 * nx) % 1 - 0.0) < 0.10) | (np.abs(((fx + 1) / 2 * nx) % 1 - 1.0) < 0.10) | (np.abs(((fz + 1) / 2 * nz) % 1) < 0.09) | (np.abs(((fz + 1) / 2 * nz) % 1 - 1.0) < 0.09)
        col = mix(col, bar, on)
    elif lat == "dia":
        d = (np.abs(((fx * g.wm / 2 + fz * g.hm / 2) / 0.28) % 1 - 0.5) < 0.12) | (np.abs(((fx * g.wm / 2 - fz * g.hm / 2) / 0.28) % 1 - 0.5) < 0.12)
        col = mix(col, bar, d)
    elif lat == "slit":
        col = mix(col, bar, np.abs(fx) > 0.82)
    return col


def door(g):
    P = g.part["params"]
    if g.dir != P.get("face", "y-"):
        g.part["params"].setdefault("col_side", P.get("col"))
    c = C(P.get("col", [0.20, 0.62, 0.62]))
    pw = P.get("plank", 0.30)
    pid = np.floor((g.a - g.ac) / pw + 0.5)
    fa = (g.a - g.ac) / pw + 0.5 - pid
    col = np.zeros(g.a.shape + (3,), np.float32) + c
    col *= (0.86 + 0.24 * hashf(pid, pid * 0 + g.seed, pid * 0))[..., None]
    gr = vnoise(g.a * 24.0, g.b * 0.8, g.a * 0 + g.seed, 5)
    col = mix(col, col * 0.84, gr > 0.62)
    col = mix(col, np.minimum(col * 1.12 + 0.02, 1), gr < 0.3)
    worn = g.n(1.5, 36) * 0.7 + 0.3 * gr
    col = mix(col, C([0.44, 0.30, 0.20]) * (0.85 + 0.3 * gr)[..., None], worn > P.get("wear", 0.70))
    col = mix(col, col * 0.40, fa < 0.06)
    col = mix(col, np.minimum(col * 1.22, 1), (fa > 0.06) & (fa < 0.14))
    fz = (g.b - g.bc) / (g.hm / 2)
    for zc in P.get("straps", [-0.62, 0.62]):                               # iron straps with studs
        on = np.abs(fz - zc) < 0.06 * 2.0 / max(g.hm, 0.5)
        col = mix(col, C([0.16, 0.18, 0.22]), on)
        stud = on & (np.abs(fa - 0.55) < 0.12)
        col = mix(col, C([0.70, 0.62, 0.36]), stud)
    return col


PAINTERS = dict(stone=stone, block=lambda g: stone(g, True), thatch=thatch, wood=wood, weave=weave, cloth=cloth, flat=flat, leaf=leaf, glow=glow, door=door)


# ------------------------------------------------------------------ CALM mode (Logs/payo/pieces/CALM_STYLE.md)
# python tools/payo_pieces/house_paint.py --calm <name>: the same UV layout re-painted as a few big colour areas.
# One base colour per surface with one soft gradient over the whole part (lighter on top, a little darker and cooler
# at the foot), contrast between parts not inside them, a few LARGE stones with faint joints, no moss dots, cracks,
# dabs, wear or outlines, only the biggest motif on a face drawn bold and flat, bevels as a subtle lighter edge.
# Glow regions are painted exactly as before. The default (no flag) path above is untouched.
CALM = False
BLD = dict(lo=[-5, -5, 0], hi=[5, 5, 10])
CMOSS = np.array([0.50, 0.57, 0.36], np.float32)
NSTRENGTH = 3.0                                           # the relief exaggerated for the toon light


def soften(c, sat=0.80, warm=0.05):
    """The calm palette: the same hue, less saturated, a breath warmer (dusty rose, terracotta, mint, ochre)."""
    c = C(c)
    lum = float(c @ C([0.30, 0.55, 0.15]))
    c = lum + (c - lum) * sat
    return np.clip(mix(c, C([0.96, 0.90, 0.80]), warm), 0, 1)


def calm_stone_col(pal):
    m = np.mean(np.array(pal, np.float32), 0)
    return np.clip(m * 0.8 + C([0.21, 0.185, 0.14]), 0, 1)       # soft warm grey to pale sand, values drawn close together


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def calm_light(g, col, amp=0.13):
    """The one soft gradient: across the whole part (scaled by its height) and a gentle one across the building."""
    hz = max(g.hi[2] - g.lo[2], 0.05)
    a = amp * float(np.clip(hz / 2.5, 0.25, 1.0))
    if g.side:
        t = np.clip((g.Z - g.lo[2]) / hz, 0, 1)
        f = (1 - a * 0.75) + a * smooth(0, 1, t)
        col = col * f[..., None]
        col = mix(col, col * C([0.95, 0.97, 1.03]), (1 - t) * 0.6)
    elif g.dir == "z+":
        col = col * 1.05
    tb = np.clip((g.Z - BLD["lo"][2]) / max(BLD["hi"][2] - BLD["lo"][2], 1), 0, 1)
    return col * (0.95 + 0.07 * tb)[..., None]


def calm_band_pattern(pat, a, fz, period):
    if pat == "dot":
        return None                                       # small repeated dots go: the band stays one solid colour
    if pat == "dia":
        t = (a / period) % 1.0
        return np.abs(t - 0.5) * 2 + np.abs(fz - 0.5) * 2 < 0.62
    return pattern(pat, a, fz, period)


def calm_bands(g, col, bands, A=None, L=None):
    for bd in bands:
        if L is None and not g.side:
            continue
        z = g.Z if L is None else L
        fz = (z - bd["z0"]) / (bd["z1"] - bd["z0"])
        inb = (fz >= 0) & (fz <= 1)
        col = mix(col, soften(bd["col"]) * (0.98 + 0.04 * fz)[..., None], inb)
        if bd.get("pat"):
            on = calm_band_pattern(bd["pat"], (g.a if A is None else A) + bd.get("off", 0), np.clip(fz * 1.3 - 0.15, 0, 1), bd.get("period", 0.8) * 1.5)
            if on is not None:
                on = on & inb & (fz > 0.12) & (fz < 0.88)
                col = mix(col, soften(bd["col2"]), on)
                g.H = g.H + 0.012 * on                      # the band's figure stands a little proud
        col = mix(col, col * 0.90, inb & ((fz < 0.06) | (fz > 0.94)))
    return col


def calm_stone(g, single=False):
    P = g.part["params"]
    base = calm_stone_col(P.get("base", STONE)) * (0.93 if single else 1.0)     # trim stones a step down so they never read as white blocks
    a, b = g.a, g.b
    col = np.zeros(a.shape + (3,), np.float32) + base
    pt = P.get("paint")
    pk = np.zeros(a.shape, np.float32)
    if pt:
        cover, fade = pt.get("cover", 0.6), pt.get("fade", 1.0)
        if cover >= 0.6:
            pk = pk + 1.0
        elif cover >= 0.4:                                   # trim stones half painted: a stone tinted by the paint, not a white block
            pk = pk + 0.35
        elif fade > 0 and g.side:                            # an old coat: one big shape over the upper wall, a soft wavy edge
            hz = max(g.hi[2] - g.lo[2], 0.1)
            line = g.lo[2] + hz * (1 - cover * 1.6) + 0.18 * np.sin(g.a / 1.7 + 1.3) + 0.10 * np.sin(g.a / 0.9)
            pk = smooth(-0.06, 0.06, g.Z - line)
        col = mix(col, soften(pt["col"]), pk)
    # a few large stones with faint soft joints (fainter still under paint)
    if single:
        sub = P.get("sub")
        if sub:
            sdf, *_ = blocks(a, b, sub * 1.6, max(g.hm, 0.2) * 1.02 if g.side else sub * 1.3, g.seed)
        else:
            sdf = None
    else:
        sdf, nx, nz, idm, idk = blocks(a, b, P.get("bw", 0.95) * 2.0, P.get("course", 0.52) * 1.8, g.seed)
        hb = hashf(idm, idk, idm * 0 + 4, g.seed)
        col = col * (1 + 0.05 * (hb - 0.5) * (1 - pk))[..., None]
    if sdf is not None:
        js = smooth(-0.07, -0.01, sdf)
        col = col * (1 - js * (0.08 - 0.05 * pk))[..., None]
        g.H = g.H - js * (0.022 - 0.012 * pk)                 # the joints as gentle grooves
    col = calm_bands(g, col, P.get("bands", []))
    col = calm_light(g, col)
    if g.side:
        ev = P.get("eave")
        if ev:                                                # the eave's soft shadow, one shape, no streaks
            col = col * (1 - 0.12 * (1 - smooth(0, P.get("eave_len", 0.7) * 1.2, ev - g.Z)))[..., None]
        gz = P.get("ground")
        if gz is not None:
            col = col * (1 - 0.10 * (1 - smooth(0, 0.6, g.Z - gz)))[..., None]
    # moss: only a few big soft shapes along top edges and on tops, muted
    amt = P.get("moss", 0.5)
    if not single and amt >= 0.4:
        lo_n = vnoise(g.X * 0.55 + g.Y * 0.55, g.X * 0 + g.seed, g.Z * 0.2, 53)
        if g.side and P.get("topmoss", 0.6) > 0:
            reach = 0.35 + 0.55 * lo_n
            m = smooth(0.62, 0.70, lo_n) * (1 - smooth(reach * 0.7, reach, g.hi[2] - g.Z))
        elif g.dir == "z+":
            m = smooth(0.58, 0.66, lo_n)
        else:
            m = 0
        col = mix(col, CMOSS * (0.94 + 0.08 * smooth(0, 1, lo_n))[..., None], np.asarray(m, np.float32) * 0.92)
        g.H = g.H + 0.018 * np.asarray(m, np.float32)
    return col


def calm_thatch(g):
    P = g.part["params"]
    base = soften(P.get("col", [0.80, 0.62, 0.30]), 0.85, 0.03)
    dark = C(P.get("dark", [0.42, 0.30, 0.20]))
    lh = P.get("layer", 0.55)
    if g.dir == "z-":
        ang = np.arctan2(g.Y - P.get("cy", 0), g.X - P.get("cx", 0))
        rib = np.abs(((ang / (2 * math.pi) * 24) % 1) - 0.5) < 0.12
        col = np.zeros(g.a.shape + (3,), np.float32) + dark * 1.12
        return mix(col, dark * 1.30, rib)
    a = g.a if g.side else np.arctan2(g.Y - P.get("cy", 0), g.X - P.get("cx", 0)) * 2.0
    z = g.Z if g.side else -np.hypot(g.X - P.get("cx", 0), g.Y - P.get("cy", 0)) * 0.8
    rag = (vnoise(a * 2.2, a * 0 + 1, a * 0, g.seed) - 0.5) * 0.35
    t = (z + rag * lh) / lh
    f = t - np.floor(t)
    group = vnoise(a * 1.6, np.floor(t) * 1.3, a * 0, g.seed + 2)
    col = np.zeros(a.shape + (3,), np.float32) + base
    col = col * (0.96 + 0.08 * group)[..., None]                         # broad strand groups, not single strands
    col = mix(col, np.minimum(base * 1.10 + 0.02, 1), (1 - smooth(0.0, 0.30, f)) * 0.7)        # the sunlit lip
    col = mix(col, mix(base, dark, 0.45), smooth(0.70, 0.96, f) * 0.55)                       # tucked under the next
    g.H = 0.10 * (1 - f) + 0.025 * group                  # each layer's lip proud, tucked under the next: broad bundles
    zz = np.clip((g.Z - g.lo[2]) / max(g.hi[2] - g.lo[2], 0.1), 0, 1)
    col = col * (0.93 + 0.10 * zz)[..., None]
    if WEATHER:
        col = weather_thatch(g, col, base)
    for bd in P.get("bands", []):
        fz = (g.Z - bd["z0"]) / (bd["z1"] - bd["z0"])
        inb = (fz >= 0) & (fz <= 1) & g.side
        col = mix(col, soften(bd["col"]) * (0.96 + 0.06 * group)[..., None], inb)
        if bd.get("pat"):
            col = mix(col, soften(bd["col2"]), inb & pattern(bd["pat"], g.a, fz, bd.get("period", 0.7) * 1.5))
    return col


def calm_wood(g):
    P = g.part["params"]
    base = C(P.get("col", [0.46, 0.30, 0.20]))
    ex = [g.hi[i] - g.lo[i] for i in range(3)]
    if g.side:
        horiz = ex[0] if g.dir[0] == "y" else ex[1]
        along_z = ex[2] >= horiz
    else:
        along_z = False
    if P.get("grain") == "z": along_z = True
    if P.get("grain") == "h": along_z = False
    if g.side:
        L, A = (g.b, g.a) if along_z else (g.a, g.b)
    else:
        L, A = (g.a, g.b) if ex[0] >= ex[1] else (g.b, g.a)
    col = np.zeros(g.a.shape + (3,), np.float32) + base
    plank = P.get("plank")
    if plank:                                             # planks only as faint soft joints
        fa = A / plank + 0.3 - np.floor(A / plank + 0.3)
        col = col * (1 - 0.10 * (1 - smooth(0.0, 0.08, fa)))[..., None]
        g.H = g.H - 0.012 * (1 - smooth(0.0, 0.10, fa))
    pt = P.get("paint")
    if pt and pt.get("cover", 0.6) >= 0.4:
        col = col * 0 + soften(pt["col"])
    for bd in P.get("bands", []):
        fz = (L - bd["z0"]) / (bd["z1"] - bd["z0"])
        inb = (fz >= 0) & (fz <= 1)
        col = mix(col, soften(bd["col"]), inb)
        if bd.get("pat"):
            col = mix(col, soften(bd["col2"]), inb & pattern(bd["pat"], A, fz, bd.get("period", 0.3) * 1.5))
    col = calm_light(g, col, 0.10)
    if not g.side:
        col = np.minimum(col * 1.06, 1)
    return col


def calm_weave(g):
    P = g.part["params"]
    c1, c2 = C(P.get("col", [0.82, 0.68, 0.40])), C(P.get("col2", [0.60, 0.44, 0.24]))
    col = np.zeros(g.a.shape + (3,), np.float32) + mix(c1, c2, 0.3)
    dp = P.get("diamond")
    if dp and g.side:
        d = np.abs(((g.a - g.ac) / dp) % 2 - 1) + np.abs(((g.b - g.bc) / dp) % 2 - 1)
        col = mix(col, soften(P.get("col3", [0.72, 0.24, 0.18])), np.abs(d - 1.0) < P.get("dline", 0.08) * 1.8)
    return calm_light(g, col, 0.10)


def calm_cloth(g):
    P = g.part["params"]
    base = soften(P.get("col", [0.78, 0.20, 0.18]))
    stripes = P.get("stripes", [[0.10, 0.16, [0.95, 0.90, 0.78]], [0.20, 0.30, [0.12, 0.12, 0.16]], [0.70, 0.80, [0.95, 0.76, 0.24]]])
    f = np.clip((g.hi[2] - g.Z) / max(g.hi[2] - g.lo[2], 0.01), 0, 1) if g.side else g.a * 0 + 0.5
    col = np.zeros(g.a.shape + (3,), np.float32) + base
    for s0, s1, c in stripes:
        col = mix(col, soften(c), (f > s0) & (f < s1))
    zz = pattern("zig", g.a, np.clip((f - 0.38) / 0.24, 0, 1), 0.40) & (f > 0.40) & (f < 0.60)
    col = mix(col, soften(P.get("col2", [0.96, 0.90, 0.76])), zz)
    col = col * (0.95 + 0.06 * np.sin(g.a * 5.0 + g.seed))[..., None]
    g.H = 0.02 * np.sin(g.a * 5.0 + g.seed)
    return col * (1.03 - 0.08 * f)[..., None]


def calm_flat(g):
    P = g.part["params"]
    col = np.zeros(g.a.shape + (3,), np.float32) + soften(P.get("col", [0.5, 0.5, 0.5]), 0.88, 0.03)
    if P.get("ringw"):
        r = np.hypot(g.a - g.ac, g.b - g.bc)
        col = mix(col, np.minimum(col * 1.18, 1), 1 - smooth(P["ringw"] * 0.8, P["ringw"] * 0.95, r))
    return calm_light(g, col, 0.10)


def calm_leaf(g):
    P = g.part["params"]
    col = np.zeros(g.a.shape + (3,), np.float32) + C(P.get("col", [0.3, 0.6, 0.36]))
    return col * (0.96 + 0.08 * g.n(0.8, 97, 1))[..., None]


def calm_door(g):
    P = g.part["params"]
    c = soften(P.get("col", [0.20, 0.62, 0.62]))
    pw = P.get("plank", 0.30)
    fa = (g.a - g.ac) / pw + 0.5 - np.floor((g.a - g.ac) / pw + 0.5)
    col = np.zeros(g.a.shape + (3,), np.float32) + c
    col = col * (1 - 0.10 * (1 - smooth(0.0, 0.07, fa)))[..., None]
    g.H = g.H - 0.012 * (1 - smooth(0.0, 0.09, fa))
    fz = (g.b - g.bc) / (g.hm / 2)
    for zc in P.get("straps", [-0.62, 0.62]):
        on = np.abs(fz - zc) < 0.09 * 2.0 / max(g.hm, 0.5)
        col = mix(col, C([0.24, 0.24, 0.28]), on)
        g.H = g.H + 0.012 * on
    return calm_light(g, col, 0.10)


CALM_PAINTERS = dict(stone=calm_stone, block=lambda g: calm_stone(g, True), thatch=calm_thatch, wood=calm_wood, weave=calm_weave, cloth=calm_cloth,
                     flat=calm_flat, leaf=calm_leaf, glow=glow, door=calm_door)


# ------------------------------------------------------------------ painted motifs
def motif_mask(name, n, calm=False):
    """A white on black drawing in a square n pixels wide (drawn 3x and shrunk). calm: no fine linework (toes)."""
    S = n * 3
    im = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(im)
    X = lambda x: S / 2 + x * S / 2
    Y = lambda y: S / 2 - y * S / 2
    P = lambda pts: [(X(x), Y(y)) for x, y in pts]
    W = lambda w: max(1, int(w * S / 2))

    def ell(cx, cy, rx, ry, fill=255):
        d.ellipse([X(cx - rx), Y(cy + ry), X(cx + rx), Y(cy - ry)], fill=fill)

    def line(pts, w, fill=255):
        d.line(P(pts), fill=fill, width=W(w), joint="curve")
        for x, y in pts:
            ell(x, y, w / 2, w / 2, fill)

    if name == "sun":
        for i in range(8):
            a = i * math.pi / 4
            b0, b1 = a - 0.24, a + 0.24
            d.polygon(P([(math.cos(b0) * 0.50, math.sin(b0) * 0.50), (math.cos(a) * 0.96, math.sin(a) * 0.96), (math.cos(b1) * 0.50, math.sin(b1) * 0.50)]), fill=255)
        ell(0, 0, 0.44, 0.44)
        ell(0, 0, 0.30, 0.30, 0)
        ell(0, 0, 0.19, 0.19)
    elif name == "lizard":
        line([(0, 0.52), (0.03, 0.2), (0, -0.12)], 0.26)
        ell(0, 0.70, 0.17, 0.20)
        line([(0, -0.12), (0.10, -0.40), (-0.06, -0.62), (0.10, -0.80), (0.30, -0.78)], 0.11)
        for sx in (-1, 1):
            line([(0.08 * sx, 0.40), (0.42 * sx, 0.50), (0.50 * sx, 0.78)], 0.10)
            line([(0.08 * sx, 0.0), (0.44 * sx, -0.08), (0.56 * sx, -0.36)], 0.10)
            for t in ((-0.10, 0, 0.10) if not calm else ()):
                line([(0.50 * sx, 0.78), (0.50 * sx + t * 1.3, 0.93)], 0.045)
                line([(0.56 * sx, -0.36), (0.56 * sx + t * 1.3, -0.52)], 0.045)
        ell(-0.07, 0.74, 0.04, 0.04, 0); ell(0.07, 0.74, 0.04, 0.04, 0)
    elif name == "rice":
        line([(-0.12, -0.95), (-0.06, -0.3), (0.10, 0.35), (0.34, 0.78)], 0.07)
        line([(-0.10, -0.6), (-0.42, -0.1), (-0.62, 0.05)], 0.06)
        line([(-0.08, -0.5), (0.34, -0.3), (0.58, -0.36)], 0.06)
        for i in range(7):
            t = i / 6
            x, y = -0.02 + 0.34 * t ** 1.4, 0.0 + 0.78 * t
            s = -1 if i % 2 else 1
            cx, cy = x + 0.17 * s, y + 0.05
            d.polygon(P([(cx - 0.11, cy - 0.10), (cx + 0.02 * s, cy + 0.17), (cx + 0.12, cy - 0.04)]), fill=255)
            ell(cx, cy, 0.09, 0.12)
    elif name == "face":
        d.rounded_rectangle([X(-0.62), Y(0.92), X(0.62), Y(-0.92)], radius=W(0.30), fill=255)
        ell(-0.70, 0.1, 0.14, 0.26); ell(0.70, 0.1, 0.14, 0.26)
        ell(-0.28, 0.30, 0.19, 0.15, 0); ell(0.28, 0.30, 0.19, 0.15, 0)
        ell(-0.28, 0.30, 0.07, 0.07); ell(0.28, 0.30, 0.07, 0.07)
        d.polygon(P([(-0.07, 0.34), (0.07, 0.34), (0.17, -0.20), (-0.17, -0.20)]), fill=0)
        d.rounded_rectangle([X(-0.36), Y(-0.38), X(0.36), Y(-0.66)], radius=W(0.06), fill=0)
        for i in range(4):
            x = -0.27 + i * 0.18
            d.rectangle([X(x - 0.05), Y(-0.40), X(x + 0.05), Y(-0.64)], fill=255)
        line([(-0.44, 0.60), (-0.12, 0.52)], 0.07, 0); line([(0.44, 0.60), (0.12, 0.52)], 0.07, 0)
        d.polygon(P([(-0.40, 0.92), (0, 0.72), (0.40, 0.92)]), fill=0)
    elif name == "moon":
        ell(0, 0, 0.86, 0.86)
        ell(0.36, 0.16, 0.70, 0.70, 0)
        for a in (0.5, 1.2, 1.9):
            ell(0.38 + 0.0, 0.16, 0.0, 0.0)
        ell(0.50, 0.10, 0.12, 0.12)
        ell(0.80, 0.62, 0.07, 0.07); ell(0.84, -0.42, 0.06, 0.06)
    elif name == "spiral":
        pts = []
        for i in range(90):
            t = i / 89
            a = t * 4.6 * math.pi
            r = 0.10 + 0.78 * t
            pts.append((math.cos(a) * r, math.sin(a) * r))
        line(pts, 0.13)
    elif name == "weave":
        for r, f in ((0.95, 255), (0.72, 0), (0.50, 255), (0.28, 0), (0.12, 255)):
            d.polygon(P([(0, r), (r, 0), (0, -r), (-r, 0)]), fill=f)
    elif name == "hand":
        d.rounded_rectangle([X(-0.36), Y(0.10), X(0.36), Y(-0.80)], radius=W(0.18), fill=255)
        for x in (-0.28, -0.09, 0.10, 0.29):
            d.rounded_rectangle([X(x - 0.075), Y(0.90 - abs(x) * 0.6), X(x + 0.075), Y(-0.1)], radius=W(0.07), fill=255)
        line([(-0.36, -0.40), (-0.70, 0.0)], 0.16)
    elif name == "step":                                                    # a stepped mountain with a doorway
        for i in range(4):
            w = 0.95 - i * 0.22
            d.rectangle([X(-w), Y(-0.9 + (i + 1) * 0.42), X(w), Y(-0.9 + i * 0.42)], fill=255)
        d.rectangle([X(-0.14), Y(-0.36), X(0.14), Y(-0.9)], fill=0)
    return np.asarray(im.resize((n, n), Image.LANCZOS), dtype=np.float32) / 255.0


def decals(g, col):
    for dc in g.part["params"].get("decals", []):
        if dc.get("dir", "y-") != g.dir:
            continue
        cx, cy = g.to_px(dc["at"])
        sw = dc["size"] * g.g["ppu"]
        sh = dc["size"] * g.g["ppv"] * dc.get("tall", 1.0)
        n = int(max(sw, sh))
        if n < 6:
            continue
        m = Image.fromarray((motif_mask(dc["motif"], n) * 255).astype(np.uint8)).resize((max(2, int(sw)), max(2, int(sh))), Image.LANCZOS)
        if dc.get("rot"):
            m = m.rotate(dc["rot"], expand=True, resample=Image.BICUBIC)
        full = Image.new("L", (g.w, g.h), 0)
        full.paste(m, (int(cx - m.width / 2), int(cy - m.height / 2)))
        ow = max(1, int(0.035 * g.g["ppu"]))
        outline = np.asarray(full.filter(ImageFilter.MaxFilter(ow * 2 + 1)), dtype=np.float32) / 255
        mask = np.asarray(full, dtype=np.float32) / 255
        worn = (g.n(1.6, 44) * 0.7 + 0.3 * g.n(5.0, 45, 2)) > dc.get("wear", 0.30)
        worn = worn.astype(np.float32)
        col[:] = mix(col, col * C(dc.get("line", [0.45, 0.44, 0.46])), outline * worn * 0.85)
        dab = g.n(3.0, 46, 2)
        cc = C(dc["col"]) * (0.92 + 0.16 * (dab > 0.5))[..., None]
        col[:] = mix(col, cc, mask * worn)
    return col


def calm_decals(g, col):
    """Only the biggest motif on each face, at least a metre across, drawn flat and bold in one colour: no outline,
    no wear. It sits a little proud in the height map."""
    ds = [dc for dc in g.part["params"].get("decals", []) if dc.get("dir", "y-") == g.dir and dc["size"] >= 0.9 and dc["motif"] not in ("rice", "hand")]
    if not ds:
        return col
    dc = max(ds, key=lambda d: d["size"])
    cx, cy = g.to_px(dc["at"])
    sw = dc["size"] * g.g["ppu"]
    sh = dc["size"] * g.g["ppv"] * dc.get("tall", 1.0)
    n = int(max(sw, sh))
    if n < 6:
        return col
    m = Image.fromarray((motif_mask(dc["motif"], n, calm=True) * 255).astype(np.uint8)).resize((max(2, int(sw)), max(2, int(sh))), Image.LANCZOS)
    if dc.get("rot"):
        m = m.rotate(dc["rot"], expand=True, resample=Image.BICUBIC)
    full = Image.new("L", (g.w, g.h), 0)
    full.paste(m, (int(cx - m.width / 2), int(cy - m.height / 2)))
    mask = np.asarray(full, dtype=np.float32) / 255
    lum = col @ C([0.30, 0.55, 0.15])
    f = np.clip(lum / max(float(lum.mean()), 1e-3), 0.85, 1.12)
    col = mix(col, soften(dc["col"]) * f[..., None], mask)
    g.H = g.H + 0.016 * mask
    return col


# ------------------------------------------------------------------ CALM roll-out: one main wall colour per house
# The pilot (house_tower) has no entry here and so paints exactly as it did. Every other house gets ONE main wall
# colour from the calm palette, so a street of them reads varied but calm, plus one accent; and its thatch is weathered
# (a broad soft darker band at the eaves, a gentle colour shift across the roof, no speckle).
# Entries: part index -> overrides of that part's params ("paint" colour, "bands" as [(col, col2)], "decals" colours,
# "cover" for trim stones). Colours are the build scripts' own (house_kit palette), softened by the calm painter.
_C = dict(ROSE=[0.84, 0.50, 0.52], MINT=[0.56, 0.78, 0.66], OCHRE=[0.90, 0.68, 0.28], CORAL=[0.88, 0.46, 0.34],
          TURQ=[0.20, 0.64, 0.68], CREAM=[0.95, 0.90, 0.78], RED=[0.66, 0.22, 0.18],
          SAND=[0.88, 0.79, 0.62])                       # cream for a whole wall: a step down so it never reads white
CALM_SCHEMES = {
    # terracotta shaft, a cream storey and cornices under the hat, turquoise as the accent
    "house_gong": dict(main="CORAL", parts={
        3: dict(decals={"step": "CREAM"}),
        4: dict(decals={"sun": "CREAM"}),
        5: dict(paint="SAND"),
        6: dict(paint="SAND", bands=[("SAND", "CORAL")]),
        66: dict(paint="SAND", bands=[("SAND", "CORAL")])}),
    # ochre body, red as the accent (as built)
    "house_granary": dict(main="OCHRE", parts={}),
    # woven walls; terracotta gables and plinth coat, the turquoise fret band as the accent
    "house_long": dict(main="CORAL", parts={
        0: dict(paint="CORAL"),
        155: dict(paint="CORAL", decals={"face": "CREAM"}),
        159: dict(paint="CORAL")}),
    # a pale cream temple: every tier cream, terracotta bands and the shrine roof as the accent
    "house_shrine": dict(main="SAND", parts={
        0: dict(paint="SAND"),
        2: dict(paint="SAND", bands=[("CORAL", "CREAM")], moss=0.3),      # tiers only 1.5 m tall: their top moss
        4: dict(paint="SAND", moss=0.3),                                    # read as green stripes, so it goes
        6: dict(paint="SAND", moss=0.3),
        16: dict(paint="CORAL"),
        17: dict(paint="SAND"), 34: dict(paint="SAND"),
        64: dict(paint="SAND", bands=[("CORAL", "CREAM")]),
        69: dict(paint="SAND", bands=[("CORAL", "CREAM")]),
        70: dict(paint="CORAL"),
        71: dict(decals={"sun": "CREAM"})}),
    # mint wall, terracotta drums and bands as the accent, cream trim; its old grey cogon brought toward the one
    # golden straw (CALM_STYLE rule 7), kept a little older and greyer than the others
    "house_tiered": dict(main="MINT", thatch=([0.66, 0.55, 0.40], [0.78, 0.62, 0.36]), parts={
        1: dict(bands=[("CORAL", "CREAM"), ("CORAL", None)], decals={"weave": "CREAM"}),
        53: dict(paint="SAND", bands=[("CORAL", "CREAM")]),
        **{i: dict(paint="SAND") for i in range(2, 15) if i != 10}}),
}
WEATHER = False                                           # set per house in paint_calm: True for every CALM_SCHEMES house
FORCE_WEATHER = "--weather" in sys.argv                   # weather a house with no scheme too (the pilot tower, as an option)


def apply_scheme(name, L):
    sc = CALM_SCHEMES.get(name)
    if not sc:
        return
    if sc.get("thatch"):                                  # (old straw colour, calm straw colour): every part of that straw
        was, now = sc["thatch"]
        for part in L["parts"]:
            P = part["params"]
            if part["kind"] == "thatch" and np.allclose(P.get("col", [0, 0, 0]), was):
                P["col"] = now
    for pi, ov in sc["parts"].items():
        P = L["parts"][pi]["params"]
        if "moss" in ov:
            P["moss"] = ov["moss"]
        if "paint" in ov and P.get("paint"):
            P["paint"] = dict(P["paint"], col=_C[ov["paint"]])
        if "bands" in ov:
            for bd, (c1, c2) in zip(P.get("bands", []), ov["bands"]):
                bd["col"] = _C[c1]
                if c2 and bd.get("col2"):
                    bd["col2"] = _C[c2]
        for dc in P.get("decals", []):
            if dc["motif"] in ov.get("decals", {}):
                dc["col"] = _C[ov["decals"][dc["motif"]]]


def weather_thatch(g, col, base):
    """The pilot's note: the thatch looked a little too new. One broad soft darker band at the eave (the lowest metre
    or so of the part, greyer and a touch cooler, rain-darkened) and one gentle colour shift across the whole roof
    (a sun-bleached paler gold toward the crown, a warmer browner straw in big soft areas). Low frequency only."""
    if not g.side:
        return col
    hz = max(g.hi[2] - g.lo[2], 0.1)
    up = np.clip((g.Z - g.lo[2]) / hz, 0, 1)
    lum = (col @ C([0.30, 0.55, 0.15]))[..., None]
    col = lum + (col - lum) * 0.84                                              # a season old: a little less yellow
    eave = (1 - smooth(0.0, min(2.0, hz * 0.45), g.Z - g.lo[2])) ** 0.8         # 1 at the eave, gone a metre or two up
    col = mix(col, col * C([0.64, 0.62, 0.60]) + C([0.03, 0.03, 0.035]), eave * 0.85)   # rain-darkened, greyer straw
    big = vnoise(g.X * 0.22 + 3.1, g.Y * 0.22, g.Z * 0.18, 77)                   # areas metres across
    col = mix(col, col * C([0.98, 0.91, 0.82]), smooth(0.40, 0.80, big) * 0.55)  # a warmer, browner patch of straw
    return mix(col, np.minimum(col * C([1.05, 1.05, 1.07]) + 0.015, 1), smooth(0.55, 1.0, up) * 0.4)   # bleached toward the crown


def paint_calm(name, out):
    """CALM mode: the same layout re-painted per CALM_STYLE.md, plus a height map and the normal map baked from it."""
    global PAD, WEATHER
    import calm_glb
    L = json.load(open(os.path.join(WORK, name + ".json")))
    apply_scheme(name, L)
    WEATHER = name in CALM_SCHEMES or FORCE_WEATHER
    T, PAD = L["tex"], L["pad"]
    BLD["lo"], BLD["hi"] = L["meta"]["lo"], L["meta"]["hi"]
    img = np.zeros((T, T, 3), np.float32) + C([0.40, 0.39, 0.38])
    H = np.zeros((T, T), np.float32)
    rects, glowing = [], np.zeros((T, T), bool)
    kinds = []
    for pi, part in enumerate(L["parts"]):
        kinds.append(part["kind"])
        for gg in part["groups"]:
            g = G(part, gg, pi + part["params"].get("seed", 0))
            g.H = np.zeros(g.a.shape, np.float32)
            col = CALM_PAINTERS[part["kind"]](g)
            col = calm_decals(g, np.ascontiguousarray(col, dtype=np.float32)) if part["kind"] != "glow" else decals(g, np.ascontiguousarray(col, dtype=np.float32))
            tint = part["params"].get("tint")
            if tint:
                col = col * C(tint)
            if gg["dir"] == "z-" and part["kind"] != "thatch":
                col = col * C([0.74, 0.73, 0.75])
            sl = (slice(gg["py"], gg["py"] + g.h), slice(gg["px"], gg["px"] + g.w))
            img[sl] = np.clip(col, 0, 1)
            H[sl] = g.H if part["kind"] != "glow" else 0
            if part["kind"] == "glow":
                glowing[sl] = True
            rects.append((gg["px"], gg["py"], gg["px"] + g.w, gg["py"] + g.h, g.g["ppu"]))
    # the bevels: a subtle lighter edge in colour, a soft rounded fall-off in the height map
    S = 2
    hi = Image.new("L", (T * S, T * S), 0); lo = Image.new("L", (T * S, T * S), 0)
    dh, dl = ImageDraw.Draw(hi), ImageDraw.Draw(lo)
    ppm = L["ppm"]
    widths = dict(stone=0.12, block=0.11, wood=0.06, door=0.05, weave=0.04, thatch=0.0, cloth=0.0, leaf=0.0, flat=0.03, glow=0.0)
    for pi, x0, y0, x1, y1, convex, up in L["edges"]:
        wm = widths.get(kinds[pi], 0.04)
        if wm <= 0:
            continue
        w = max(2, int(wm * ppm * L["parts"][pi].get("dens", 1.0) * S))
        (dh if convex else dl).line([x0 * S, y0 * S, x1 * S, y1 * S], fill=255, width=w)
    hi = np.asarray(hi.resize((T, T), Image.LANCZOS).filter(ImageFilter.GaussianBlur(2.0)), dtype=np.float32) / 255
    lo = np.asarray(lo.resize((T, T), Image.LANCZOS).filter(ImageFilter.GaussianBlur(1.5)), dtype=np.float32) / 255
    hi[glowing] = 0; lo[glowing] = 0
    img = mix(img, np.minimum(img * 1.10 + 0.03, 1), hi * 0.55)
    img = mix(img, img * 0.88, lo * 0.6)
    H = H - 0.035 * hi - 0.025 * lo
    os.makedirs(out, exist_ok=True)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(os.path.join(out, name + ".png"))
    calm_glb.save_height(H, os.path.join(out, name + "_height.png"))
    Image.fromarray(calm_glb.height_to_normal(H, rects, strength=NSTRENGTH, blur=1.6)).save(os.path.join(out, name + "_normal.png"))
    print("PAINTED CALM", name, T, out)


# ------------------------------------------------------------------ the atlas
def paint(name):
    global PAD
    L = json.load(open(os.path.join(WORK, name + ".json")))
    T, PAD = L["tex"], L["pad"]
    img = np.zeros((T, T, 3), np.float32) + C([0.40, 0.39, 0.38])
    kinds = []
    for pi, part in enumerate(L["parts"]):
        kinds.append(part["kind"])
        for gg in part["groups"]:
            g = G(part, gg, pi + part["params"].get("seed", 0))
            col = PAINTERS[part["kind"]](g)
            col = decals(g, np.ascontiguousarray(col, dtype=np.float32))
            tint = part["params"].get("tint")
            if tint:
                col = col * C(tint)
            if gg["dir"] == "z-" and part["kind"] != "thatch":
                col = col * C([0.68, 0.67, 0.69])
            img[gg["py"]:gg["py"] + g.h, gg["px"]:gg["px"] + g.w] = np.clip(col, 0, 1)
    # the pale bevel and the dark crease, drawn along the mesh's own edges
    S = 2
    hi = Image.new("L", (T * S, T * S), 0); lo = Image.new("L", (T * S, T * S), 0)
    dh, dl = ImageDraw.Draw(hi), ImageDraw.Draw(lo)
    ppm = L["ppm"]
    widths = dict(stone=0.085, block=0.075, wood=0.045, door=0.04, weave=0.03, thatch=0.0, cloth=0.0, leaf=0.03, flat=0.02, glow=0.0)
    for pi, x0, y0, x1, y1, convex, up in L["edges"]:
        k = kinds[pi]
        wm = L["parts"][pi]["params"].get("edge", widths.get(k, 0.04))
        if wm <= 0:
            continue
        w = max(2, int(wm * ppm * L["parts"][pi].get("dens", 1.0) * S))
        if convex:
            (dh if up > -0.45 else dl).line([x0 * S, y0 * S, x1 * S, y1 * S], fill=255 if up > -0.45 else 150, width=w)
        else:
            dl.line([x0 * S, y0 * S, x1 * S, y1 * S], fill=255, width=w + 2)
    hi = np.asarray(hi.resize((T, T), Image.LANCZOS), dtype=np.float32)[..., None] / 255
    lo = np.asarray(lo.resize((T, T), Image.LANCZOS), dtype=np.float32)[..., None] / 255
    pale = np.clip(img * 0.55 + PALE * 0.52, 0, 1)
    img = img * (1 - hi * 0.80) + pale * hi * 0.80
    img = img * (1 - lo * 0.50) + img * C([0.46, 0.45, 0.47]) * lo * 0.50
    os.makedirs(OUT, exist_ok=True)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(os.path.join(OUT, name + ".png"))
    print("PAINTED", name, T)


def font(size):
    for f in ("arialbd.ttf", "arial.ttf"):
        try:
            return ImageFont.truetype(f, size)
        except OSError:
            pass
    return ImageFont.load_default()


def sheet(name, tag):
    shots = os.path.join(WORK, "shots")
    L = json.load(open(os.path.join(WORK, name + ".json")))
    m = L["meta"]
    labels = [("front", "front three-quarter, eye height, 1.75 m figure"), ("back", "back three-quarter"), ("close", "close-up: " + m.get("close_label", "best detail")), ("above", "from above")]
    ims = [Image.open(os.path.join(shots, "%s_%s.png" % (name, k))).convert("RGB") for k, _ in labels]
    w, h = ims[0].size
    bar = 34
    out = Image.new("RGB", (w * 2, (h + bar) * 2 + bar), (22, 26, 30))
    d = ImageDraw.Draw(out)
    d.text((10, 7), "%s %s   %.1f x %.1f x %.1f m above ground, foot %.1f m below   %d triangles   atlas %d   %s" % (
        name, tag, m["hi"][0] - m["lo"][0], m["hi"][1] - m["lo"][1], m["hi"][2], -m["lo"][2], m.get("tris_glb", m["tris"]), m["tex"], m.get("note", "")), fill=(236, 240, 236), font=font(18))
    for i, (im, (k, lab)) in enumerate(zip(ims, labels)):
        x, y = (i % 2) * w, bar + (i // 2) * (h + bar)
        d.text((x + 10, y + 8), lab, fill=(200, 214, 210), font=font(16))
        out.paste(im, (x, y + bar))
    p = os.path.join(SHEETS, "%s_%s.png" % (name, tag))
    out.save(p)
    print("SHEET", p)


def street(tag):
    shots = os.path.join(WORK, "shots")
    a = Image.open(os.path.join(shots, "street_a.png")).convert("RGB")
    b = Image.open(os.path.join(shots, "street_b.png")).convert("RGB")
    ref = Image.open(sys.argv[3]).convert("RGB") if len(sys.argv) > 3 else None
    w, h = a.size
    bar = 34
    cols = [a, b] + ([ref.resize((int(ref.width * h / ref.height), h), Image.LANCZOS)] if ref else [])
    out = Image.new("RGB", (max(w * 2, 10), (h + bar) * (2 if ref else 1)), (22, 26, 30))
    d = ImageDraw.Draw(out)
    d.text((10, 8), "PAYO town: the six buildings crowded on a slope, from the street", fill=(236, 240, 236), font=font(18))
    d.text((w + 10, 8), "the same, from above the lower end", fill=(236, 240, 236), font=font(18))
    out.paste(a, (0, bar)); out.paste(b, (w, bar))
    if ref:
        r = cols[2]
        d.text((10, h + bar + 8), "the owner's reference 239", fill=(236, 240, 236), font=font(18))
        out.paste(r, (0, h + bar * 2))
    p = os.path.join(SHEETS, "house_street_%s.png" % tag)
    out.save(p)
    print("SHEET", p)


if __name__ == "__main__":
    if "--calm" in sys.argv:                                  # python house_paint.py --calm [--out DIR] <name> [...]
        CALM = True
        a = sys.argv[1:]
        out = a[a.index("--out") + 1] if "--out" in a else os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "house")
        names = [x for i, x in enumerate(a) if not x.startswith("--") and not (i and a[i - 1] == "--out")]   # --weather: see FORCE_WEATHER
        for n in names:
            paint_calm(n, out)
        raise SystemExit(0)
    if "--out" in sys.argv:                                   # the default painter into another folder (to check it reproduces)
        i = sys.argv.index("--out")
        OUT = sys.argv[i + 1]
        del sys.argv[i:i + 2]
    if sys.argv[1] == "sheet":
        sheet(sys.argv[2], sys.argv[3])
    elif sys.argv[1] == "street":
        street(sys.argv[2])
    else:
        for n in sys.argv[1:]:
            paint(n)
