"""PAYO ASSEMBLY, the placement tables (2026-10-12). Pure Python and numpy: no bpy.

The map is ASSEMBLED from the owner-approved single pieces in ArtSource/payo/pieces-approved-2026-10-12 and the later
pieces of the same builders in Logs/payo/pieces/build/{stone,terrace}. Nothing here draws art: it says where each
piece stands. tools/payo_assemble.py builds the land under it, copies the pieces and writes the game's json;
tools/payo_assemble_check.py checks it; `python tools/payo_assemble_data.py` prints the counts.

FRAME USED IN HERE ("layout frame"): x = Unity x (east), y = Unity z (NORTH), z = Unity y (up). The can is the origin,
the court's top is z 0, the gate is SOUTH (y negative), the valley is NORTH.
YAW is the UNITY yaw in degrees, clockwise from above. A piece's front (+z in its glTF, still +z in Unity after
glTFast, which mirrors x) faces (sin yaw, cos yaw) in (x, y): yaw 0 faces north, 90 east, 180 south, 270 west.
A piece's glTF +x lands on the LEFT of a man standing behind it and looking the way it faces (west, at yaw 0).

THE LAND is a set of FIELDS round the flat rim. In a field every contour line is the SAME curve moved along the
field's uphill axis, and that curve is made of terrace modules (12 m straights and the 30 m radius curves), so every
terrace row is the one chain of modules moved up the hill and fits its contour exactly. Between two fields the land
blends over a few degrees of bearing: those seams carry the falls, the stream and the rocky ribs, and no rows.
"""
import json
import math
import os

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APPROVED = os.path.join(ROOT, "ArtSource/payo/pieces-approved-2026-10-12")
LATE_STONE = os.path.join(ROOT, "Logs/payo/pieces/build/stone")
LATE_TERRACE = os.path.join(ROOT, "Logs/payo/pieces/build/terrace")
WORK = os.path.join(ROOT, "Logs/payo/assembly/work")
LAYOUT = os.path.join(ROOT, "Assets/TumbangPreso/Art/Payo/payo_layout.json")

# ------------------------------------------------------------------ the place, in numbers
RIM_Z = -6.0          # the pit's lip: the court stands 6 m above it (the layout's law)
HOLE = 30.0           # the land's square hole, half: five pit wall modules a side (the layout's pit)
SHAFT = 26.0          # the shaft below the ledge, half
LEDGE_Z = -18.0       # the ledge between them, one module (12 m) under the lip
NOTCH = 12.0          # the north wall is open this far each side of x 0 (24 m)
PLAY = 22.0           # the play walls, half: nothing but block art stands over the court
STEP = 1.15           # a terrace wall's height
SLOPE = 0.75          # the terraced mountainside: steep, so the amphitheatre stands up behind the town
DS = STEP / SLOPE     # a row stands this far up the hill from the one below
BENCH_RISE = 2.3      # the town's bench is two terrace walls above the rim (a gate_stair at 0.92 climbs it)
BENCH_S = (0.0, BENCH_RISE / SLOPE, 18.0)        # rise, then the bench, then the terraces (metres up the south field)
# THE TOWN'S THREE BENCHES up the south field (metres up the field, height over the rim): each rise is a whole number
# of terrace walls at the terraces' own slope, so the rows on a rise are ordinary rows.
TIERS = [(0.0, 0.0), (BENCH_RISE / SLOPE, BENCH_RISE), (18.0, BENCH_RISE), (18.0 + 4.6 / SLOPE, 6.9), (37.133, 6.9), (37.133 + 4.6 / SLOPE, 11.5), (54.267, 11.5)]
TIER_MID = {1: (TIERS[1][0] + TIERS[2][0]) / 2.0, 2: (TIERS[3][0] + TIERS[4][0]) / 2.0, 3: (TIERS[5][0] + TIERS[6][0]) / 2.0}
TIER_Z = {1: BENCH_RISE, 2: 6.9, 3: 11.5}
RIM_R = 56.0


def smooth(e0, e1, x):
    t = np.clip((np.asarray(x, dtype=float) - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def wrap(a):
    return (np.asarray(a, dtype=float) + 180.0) % 360.0 - 180.0


def bearing(x, y):
    """Degrees clockwise from north: 0 north, 90 east, 180 south (the gate), 270 west."""
    return np.degrees(np.arctan2(x, y)) % 360.0


def _f(u):
    """The mountain's profile: a straight terraced slope, a rounded shoulder, a slow fall behind the crest."""
    u = np.asarray(u, dtype=float)
    mid = 0.6 + (u - 0.6) - (u - 0.6) ** 2 / 1.6
    return np.where(u <= 0.6, u, np.where(u <= 1.4, mid, 1.0 - 0.3 * (u - 1.4)))


MODULE = {"straight": (12.0, 0.0, 0.0), "convex": (11.683, 2.368, 22.92), "concave": (11.683, -2.368, -22.92)}   # end along, end toward the hill, turn
CAP_END = (4.979, 2.383)


class Field:
    """A hillside whose contour lines are one chain of terrace modules moved along the uphill axis."""

    def __init__(self, name, axis, r0, chain, kind, K, extra=0, base=0.0):
        self.name, self.kind, self.K, self.base = name, kind, K, base
        a = math.radians(axis)
        out = np.array([math.sin(a), math.cos(a)])
        self.A = out if kind != "down" else -out            # uphill
        self.t = np.array([self.A[1], -self.A[0]])          # travel, with the hill on the left
        O = out * r0
        turn = sum(MODULE[s][2] for s in chain)
        phi = -turn / 2.0
        u = c = 0.0
        self.mods = []
        us, cs = [0.0], [0.0]
        for shape in chain:
            ex, ey, tn = MODULE[shape]
            p = math.radians(phi)
            self.mods.append([u, c, phi, shape])
            if tn == 0.0:
                u += ex * math.cos(p); c += ex * math.sin(p)
                us.append(u); cs.append(c)
            else:
                sgn = 1.0 if tn > 0 else -1.0
                u0, c0 = u, c
                for k in range(1, 7):
                    aa = math.radians(22.92 * k / 6.0)
                    lx, ly = 30.0 * math.sin(aa), sgn * 30.0 * (1.0 - math.cos(aa))
                    us.append(u0 + lx * math.cos(p) - ly * math.sin(p)); cs.append(c0 + lx * math.sin(p) + ly * math.cos(p))
                u, c = us[-1], cs[-1]
            phi += tn
        mid = u / 2.0
        c_mid = float(np.interp(mid, us, cs))
        us = [x - mid for x in us]; cs = [x - c_mid for x in cs]
        for m in self.mods:
            m[0] -= mid; m[1] -= c_mid
        p0, p1 = math.radians(-turn / 2.0), math.radians(turn / 2.0)
        self.us = np.array([us[0] - 600.0 * math.cos(p0)] + us + [us[-1] + 600.0 * math.cos(p1)])
        self.cs = np.array([cs[0] - 600.0 * math.sin(p0)] + cs + [cs[-1] + 600.0 * math.sin(p1)])
        self.u0, self.u1 = us[0], us[-1]
        self.O = O
        e0, e1 = self.point(self.u0, 0.0), self.point(self.u1, 0.0)
        b0, b1 = float(bearing(*e0)), float(bearing(*e1))
        centre = axis
        lo, hi = sorted((centre + float(wrap(b0 - centre)), centre + float(wrap(b1 - centre))))
        self.core = [lo + 1.5, hi - 1.5]
        self.centre = centre
        # Higher up the hill the field is wider than its chain: the rows run on, straight, along the chain's two end
        # tangents (which is how the land continues there), as far as the field's own sector allows.
        first, last = list(self.mods[0]), list(self.mods[-1])
        ex, ey, tn = MODULE[last[3]]
        pl = math.radians(last[2])
        end_u = last[0] + ex * math.cos(pl) - ey * math.sin(pl); end_c = last[1] + ex * math.sin(pl) + ey * math.cos(pl)
        pe = last[2] + tn
        before = [[first[0] - 12.0 * i * math.cos(math.radians(first[2])), first[1] - 12.0 * i * math.sin(math.radians(first[2])), first[2], "straight"] for i in range(extra, 0, -1)]
        after = [[end_u + 12.0 * i * math.cos(math.radians(pe)), end_c + 12.0 * i * math.sin(math.radians(pe)), pe, "straight"] for i in range(extra)]
        self.mods = before + self.mods + after

    def point(self, u, s):
        c = float(np.interp(u, self.us, self.cs))
        p = self.O + self.t * u + self.A * (c + s)
        return float(p[0]), float(p[1])

    def us_of(self, x, y):
        dx = np.asarray(x, dtype=float) - self.O[0]; dy = np.asarray(y, dtype=float) - self.O[1]
        u = dx * self.t[0] + dy * self.t[1]
        s = dx * self.A[0] + dy * self.A[1] - np.interp(u, self.us, self.cs)
        return u, s

    def prof(self, s):
        s = np.asarray(s, dtype=float)
        if self.kind == "down":
            return -SLOPE * self.K * _f(np.maximum(-s, 0.0) / self.K)
        sp = np.maximum(s, 0.0)
        hill = lambda q: SLOPE * self.K * _f(q / self.K)
        if self.kind == "bench":
            g = np.interp(sp, [t[0] for t in TIERS], [t[1] for t in TIERS])
            return np.where(sp > TIERS[-1][0], TIERS[-1][1] + hill(sp - TIERS[-1][0]), g)
        if self.kind == "knoll":
            return np.where(s >= 0.0, hill(sp), 1.3 * s)
        return hill(sp)

    def z(self, x, y):
        if self.kind == "knoll":
            u, sv = self.us_of(x, y)
            side = np.maximum(0.0, np.maximum(self.u0 - u, u - self.u1))
            return RIM_Z + self.base + self.prof(sv) - 1.3 * side - 1.3 * np.maximum(0.0, sv - 1.5 * self.K)
        return RIM_Z + self.prof(self.us_of(x, y)[1])

    def phi_at(self, u):
        """How far the contour is turned toward the hill at u (degrees)."""
        phi = self.mods[0][2]
        for (mu, mc, mphi, shape) in self.mods:
            if u >= mu:
                phi = mphi + MODULE[shape][2] / 2.0
        return phi

    def heading(self, phi):
        """The Unity yaw of a module whose run is turned phi degrees toward the hill."""
        p = math.radians(phi)
        d = self.t * math.cos(p) + self.A * math.sin(p)
        return math.degrees(math.atan2(d[1], -d[0])) % 360.0, d

    def rows(self):
        """Where the foot of each row stands up the field: (s, level). The foot is on the contour 10 cm above its
        level, so a wall is 15 cm into the land and never floats."""
        out = []
        ss = np.arange(0.0, 1.38 * self.K, 0.05) if self.kind != "down" else -np.arange(0.0, 1.38 * self.K, 0.05)
        pr = self.prof(ss)
        k = 0 if self.kind != "down" else 1
        while True:
            level = STEP * k if self.kind != "down" else -STEP * k
            target = level + 0.10
            level += self.base
            if self.kind == "down":
                if target < pr.min() + 1.0:
                    break
                i = int(np.argmax(pr <= target))
            else:
                if target > pr.max() - 1.0:
                    break
                i = int(np.argmax(pr >= target))
            out.append((float(ss[i]), RIM_Z + level))
            k += 1
        return out


FIELDS = [
    Field("south", 180.0, 62.0, ["straight", "concave", "straight", "straight", "straight", "straight", "straight", "straight", "concave", "straight"], "bench", 280.0, 10),
    Field("east", 86.0, 57.5, ["straight", "concave", "straight", "concave", "straight", "concave", "straight"], "up", 180.0, 9),
    Field("west", 274.0, 57.5, ["straight", "concave", "straight", "concave", "straight", "concave", "straight"], "up", 180.0, 9),
    Field("northeast", 31.0, 57.0, ["straight", "convex", "straight", "convex"], "down", 120.0, 4),
    Field("northwest", 329.0, 57.0, ["convex", "straight", "convex", "straight"], "down", 120.0, 4),
]
F = {f.name: f for f in FIELDS}
# THE KNOLL: a terraced hill standing out of the cloud north of the notch, its near face turned to the court (the
# valley's own slopes fall away under the sight line). It is laid over the valley, not blended by bearing.
KNOLL = Field("knoll", 0.0, 128.0, ["straight", "convex", "straight", "straight", "convex", "straight", "straight", "convex", "straight"], "knoll", 42.0, 0, base=-14 * STEP)
ROW_FIELDS = FIELDS + [KNOLL]
NORTH_CORE = [-5.0, 5.0]          # the valley's mouth: the land drops away under the notch


def _north(x, y):
    r = np.hypot(x, y)
    return RIM_Z - 0.95 * np.maximum(0.0, r - RIM_R)


def _sectors():
    sec = [(NORTH_CORE[0], NORTH_CORE[1], _north, 0.0)]
    for f in sorted(FIELDS, key=lambda f: f.centre):
        sec.append((f.core[0], f.core[1], f.z, f.centre))
    for i in range(1, len(sec)):                                # a seam is never narrower than 5 degrees
        gap = sec[i][0] - sec[i - 1][1]
        if gap < 5.0:
            cut = (5.0 - gap) / 2.0
            sec[i - 1] = (sec[i - 1][0], sec[i - 1][1] - cut, sec[i - 1][2], sec[i - 1][3])
            sec[i] = (sec[i][0] + cut, sec[i][1], sec[i][2], sec[i][3])
    return sec


SECTORS = _sectors()


def seam(x, y):
    """1 on the ribs between two fields (outside every field's own bearings, beyond the rim), 0 inside a field."""
    x = np.asarray(x, dtype=float); y = np.asarray(y, dtype=float)
    th = bearing(x, y)
    inside = np.zeros_like(x)
    for (a0, a1, fn, centre) in SECTORS:
        mid = (a0 + a1) / 2.0
        t = mid + wrap(th - mid)
        inside = np.maximum(inside, smooth(a0 - 1.5, a0 + 0.5, t) * (1.0 - smooth(a1 - 0.5, a1 + 1.5, t)))
    knoll = (y > 120.0) & (np.abs(x) < 80.0)
    return np.where((np.hypot(x, y) > RIM_R + 2.0) & ~knoll, 1.0 - inside, 0.0)

# Cliffs for the falls. Where the fall's pivot stands, the way it faces (down the hill), the cliff's half width, soft
# edge and height. The cliff is a straight scarp behind the pivot.
SCARPS = [
    dict(name="medium_e", at=F["east"].point(-22.0, 30.0), yaw=266.0, half=7.0, soft=6.0, h=8.05),
    dict(name="medium_w", at=F["west"].point(18.0, 28.0), yaw=94.0, half=7.0, soft=6.0, h=8.05),
]
GULLY = dict(field="east", u=-22.0, s0=0.6, s1=22.0)          # the stream bed runs straight down the east field from its fall to the rim


def _fields(x, y):
    x = np.asarray(x, dtype=float); y = np.asarray(y, dtype=float)
    th = bearing(x, y)
    z = np.zeros_like(x); wsum = np.zeros_like(x)
    n = len(SECTORS)
    for i, (a0, a1, fn, centre) in enumerate(SECTORS):
        prev_end = SECTORS[i - 1][1] - (360.0 if i == 0 else 0.0)
        next_start = SECTORS[(i + 1) % n][0] + (360.0 if i == n - 1 else 0.0)
        mid = (a0 + a1) / 2.0
        t = mid + wrap(th - mid)
        w = smooth(prev_end, a0, t) * (1.0 - smooth(a1, next_start, t))
        if np.any(w > 0):
            z = z + w * fn(x, y); wsum = wsum + w
    return z / np.maximum(wsum, 1e-6)


def _scarp_frame(s, x, y):
    yaw = math.radians(s["yaw"])
    fx, fy = math.sin(yaw), math.cos(yaw)
    dx = np.asarray(x, dtype=float) - s["at"][0]; dy = np.asarray(y, dtype=float) - s["at"][1]
    return -(dx * fx + dy * fy), dx * fy - dy * fx              # into the hill, sideways


def _raw(x, y):
    z = _fields(x, y)
    z = np.maximum(z, KNOLL.z(x, y))
    gf = F[GULLY["field"]]
    gu, gs = gf.us_of(x, y)
    near = np.abs(wrap(bearing(x, y) - gf.centre)) < 40.0
    cut = (1.0 - smooth(1.6, 3.6, np.abs(gu - GULLY["u"]))) * smooth(GULLY["s0"], GULLY["s0"] + 2.5, gs) * (1.0 - smooth(GULLY["s1"] - 4.0, GULLY["s1"], gs)) * near
    return z - 2.4 * cut


# ------------------------------------------------------------------ THE GREAT FALL'S GORGE AND ITS STREAM
# One straight line from the can: bearing 202.92, which is the way the south field's western slope faces, so the gorge
# is cut straight up that slope and is seen end on from the court. Down it: the great fall at the gorge's head, two
# small falls in the gorge, three medium falls stepping down the hillside to the town, a fall at each of the town's
# three benches, a runnel across the rim to the pit's own spout in its south wall. No stream piece exists: the water
# is the falls' own pools, one after another.
STREAM_B = 202.92
SU = (math.sin(math.radians(STREAM_B)), math.cos(math.radians(STREAM_B)))      # up the stream, away from the can
SL = (SU[1], -SU[0])                                                           # across it, to the west
STREAM_YAW = (STREAM_B - 180.0) % 360.0                                        # a fall on it faces the can


def stream_point(r, b=0.0):
    return r * SU[0] + b * SL[0], r * SU[1] + b * SL[1]


def _nat_line(level, r0, r1=260.0):
    """Where the uncut land along the stream first reaches a level, going up."""
    rr = np.arange(r0, r1, 0.05)
    zz = _raw(rr * SU[0], rr * SU[1])
    return float(rr[int(np.argmax(zz >= level))])


RUNNEL_Z = RIM_Z - 0.45
BED = {1: RIM_Z + TIER_Z[1] - 1.4, 2: RIM_Z + TIER_Z[2] - 1.4, 3: RIM_Z + TIER_Z[3] - 1.4}     # deep enough for a rope bridge to span
R_END = 31.8 / abs(SU[1])                    # the runnel stops short of the pit's kerb
STREAM = []                                  # the falls, from the bottom: dict(model, scale, r, lower, upper)
_lv = RUNNEL_Z
for (_m, _drop, _where) in (("fall_small", BED[1] - RUNNEL_Z, 62.0), ("fall_medium", BED[2] - BED[1], 80.0), ("fall_medium", BED[3] - BED[2], 100.0),
                            ("fall_medium", 8.05 * 0.7, 118.0), ("fall_medium", 8.05, None), ("fall_medium", 8.05, None), ("fall_small", 3.0, 13.0), ("fall_small", 3.0, 12.0)):
    _up = _lv + _drop
    if _m == "fall_small" and _where in (13.0, 12.0):
        _r = STREAM[-1]["r"] + _where
    else:
        _r = _nat_line(_up + 0.25, _where if _where else STREAM[-1]["r"] + 2.0)
    STREAM.append(dict(model=_m, scale=_drop / (8.05 if _m == "fall_medium" else 2.0), r=_r, lower=_lv, upper=_up))
    _lv = _up
POOL_Z = _lv                                 # the great fall's plunge pool
GREAT_H = 40.25
R_HEAD = _nat_line(POOL_Z + GREAT_H, STREAM[-1]["r"] + 10.0)
R_GORGE = STREAM[5]["r"]                      # the gorge proper begins at the highest medium fall
_SR = [s_["r"] for s_ in STREAM]
# the bed's half width and the banks' steepness along it
_HW_R = [R_END, _SR[0] - 0.5, _SR[0], _SR[1] - 5.0, _SR[1] - 4.0, _SR[1], _SR[1] + 1.0, _SR[2] - 5.0, _SR[2] - 4.0, _SR[2], _SR[2] + 1.0, _SR[3] - 6.0, _SR[3] - 4.5, _SR[5] + 1.0, _SR[6], _SR[7] + 3.0, R_HEAD - 22.0, R_HEAD - 17.0, R_HEAD + 5.0]
_HW_W = [1.2, 1.2, 1.5, 1.5, 3.4, 3.4, 1.8, 1.8, 3.2, 3.2, 2.0, 2.4, 5.6, 5.6, 6.5, 8.0, 10.0, 17.5, 17.5]
_BK_R = [R_END, _SR[0], _SR[0] + 1.0, _SR[2] + 6.0, _SR[3] + 2.0, _SR[5], R_HEAD]
_BK_K = [1.2, 1.2, 2.0, 2.0, 3.0, 4.0, 4.0]


def stream_bed(r):
    r = np.asarray(r, dtype=float)
    z = np.full(r.shape, RUNNEL_Z)
    for s_ in STREAM:
        z = z + (s_["upper"] - s_["lower"]) * smooth(s_["r"] + 0.65, s_["r"] + 1.15, r)
    return z


def stream_half(r):
    return np.interp(r, _HW_R, _HW_W)


def stream_frame(x, y):
    x = np.asarray(x, dtype=float); y = np.asarray(y, dtype=float)
    return x * SU[0] + y * SU[1], x * SL[0] + y * SL[1]


def _stream_cut(x, y, z):
    r, b = stream_frame(x, y)
    k = np.interp(r, _BK_R, _BK_K)
    cut = stream_bed(np.minimum(r, R_HEAD + 0.6)) + k * np.maximum(0.0, np.abs(b) - stream_half(r))
    cut = cut + 60.0 * np.maximum(0.0, r - R_HEAD - 0.6) + 3.0 * np.maximum(0.0, R_END - r)
    return np.where(r > 20.0, np.minimum(z, cut), z)


def stream_wide(r):
    """How far from the stream's line its cut reaches at r (bed and bank), from the uncut land beside it."""
    rc = min(max(float(r), R_END), R_HEAD)
    k = float(np.interp(rc, _BK_R, _BK_K))
    h = float(stream_half(rc)); bed = float(stream_bed(rc))
    w = h
    for _ in range(4):
        rim = max(float(_raw(*stream_point(rc, w + 0.5))), float(_raw(*stream_point(rc, -w - 0.5))))
        w = h + max(0.0, rim - bed) / k
    return w


def in_stream(x, y, margin=0.0):
    """Inside the stream's cut (bed and banks), or within `margin` of it."""
    r, b = stream_frame(x, y)
    r = float(r); b = abs(float(b))
    if not (R_END - margin < r < R_HEAD + margin + 4.0):
        return False
    return b < stream_wide(r) + margin


def wet(x, y):
    """0..1: how much of the stream's bed a point is (the land is painted wet rock there)."""
    r, b = stream_frame(x, y)
    inside = (r > R_END - 1.0) & (r < R_HEAD + 0.5)
    return np.where(inside, 1.0 - smooth(0.0, 1.6, np.abs(b) - stream_half(r)), 0.0)


PADS = []          # a flat for each fall's pool: (x, y, radius, blend, level, scarp)
HPADS = []         # (x, y, radius, blend, level): small flats for what stands off the benches
CUT = 8.05 / SLOPE  # a medium fall's pool is cut this far into the slope: 13 m at 0.62 leaves the 8 m cliff behind it
for _s in SCARPS:
    _fx, _fy = math.sin(math.radians(_s["yaw"])), math.cos(math.radians(_s["yaw"]))
    if _s["h"] > 20:
        _R = 9.0                                           # the pool is CUT 9 m into the slope at the spur's foot: 6.75 m more cliff, 40 m in all
        PADS.append((_s["at"][0] + _fx * _R * 0.5, _s["at"][1] + _fy * _R * 0.5, 12.0, 4.0, float(_raw(_s["at"][0] + _fx * _R, _s["at"][1] + _fy * _R)), _s))
    else:
        PADS.append((_s["at"][0] + _fx * CUT * 0.5, _s["at"][1] + _fy * CUT * 0.5, 9.0, 3.0, float(_raw(_s["at"][0], _s["at"][1])) - _s["h"], _s))
    _s["level"] = PADS[-1][4]


def ground(x, y):
    """The land's height. The SAME function shapes the land mesh and seats every piece."""
    x = np.asarray(x, dtype=float); y = np.asarray(y, dtype=float)
    z = _stream_cut(x, y, _raw(x, y))
    for (hx, hy, hr, hb, hl) in HPADS:                             # a small flat under a house that stands on a slope
        t = 1.0 - smooth(hr, hr + hb, np.hypot(x - hx, y - hy))
        z = z * (1.0 - t) + hl * t
    for (px, py, R, B, level, s) in PADS:
        into, side = _scarp_frame(s, x, y)
        if s["h"] > 20:
            t = (1.0 - smooth(R, R + B, np.abs(side))) * (1.0 - smooth(-0.6, 0.6, into)) * (into > -40.0)
            z = np.where(t > 0, np.minimum(z, z * (1.0 - t) + level * t), z)
        else:
            t = (1.0 - smooth(7.0, 10.0, np.abs(side))) * (1.0 - smooth(0.6, 1.8, into)) * (into > -40.0)
            z = np.where(t > 0, np.minimum(z, z * (1.0 - t) + level * t), z)
    return z


def g(x, y):
    return float(ground(x, y))


def gradient(x, y, e=0.4):
    q = ground(np.array([x + e, x - e, x, x]), np.array([y, y, y + e, y - e]))
    return float(q[0] - q[1]) / (2 * e), float(q[2] - q[3]) / (2 * e)


def face(x, y, tx=0.0, ty=0.0):
    """The yaw that turns a piece at (x, y) to face (tx, ty)."""
    return math.degrees(math.atan2(tx - x, ty - y)) % 360.0


def front(yaw):
    return math.sin(math.radians(yaw)), math.cos(math.radians(yaw))


def right(yaw):
    """A quarter turn clockwise from the piece's front."""
    return math.cos(math.radians(yaw)), -math.sin(math.radians(yaw))


def run(yaw):
    """The way a wall module (terrace, pit wall, cliff) runs from its pivot: its glTF +x."""
    return -math.cos(math.radians(yaw)), math.sin(math.radians(yaw))


# ------------------------------------------------------------------ the catalogue (written by payo_assemble_catalog.py)
def catalogue():
    cat = {}
    for name in ("catalog_kept.json", "catalog_approved.json", "catalog_late.json"):
        p = os.path.join(ROOT, "Logs/payo/assembly", name)
        if os.path.exists(p):
            cat.update(json.load(open(p)))
    return cat


CAT = catalogue()


def footprint(p, grow=0.0):
    """The four corners of a placement's bounding box on the ground. A piece imported into Blender has its front at
    -Y; the assembly turns it (pi - yaw) about Z, which is what Unity's yaw does to the glTFast import."""
    c = CAT[p["model"]]
    s = p.get("scale", 1.0)
    lo, hi = c["lo"], c["hi"]
    phi = math.pi - math.radians(p["yaw"])
    cs, sn = math.cos(phi), math.sin(phi)
    out = []
    for bx, by in ((lo[0] * s - grow, lo[1] * s - grow), (hi[0] * s + grow, lo[1] * s - grow), (hi[0] * s + grow, hi[1] * s + grow), (lo[0] * s - grow, hi[1] * s + grow)):
        out.append((p["x"] + bx * cs - by * sn, p["y"] + bx * sn + by * cs))
    return out


P = []            # every placement


def put(model, x, y, yaw=0.0, scale=1.0, family="prop", group="static", z=None, dz=0.0, **more):
    row = dict(model=model, x=round(float(x), 3), y=round(float(y), 3), z=round((g(x, y) if z is None else float(z)) + dz, 3),
               yaw=round(float(yaw) % 360.0, 2), scale=round(float(scale), 4), family=family, group=group)
    row.update(more)
    P.append(row)
    return row


def local(row, a, b):
    """A point `a` metres to the piece's right and `b` metres in front of its pivot."""
    fx, fy = front(row["yaw"]); rx, ry = right(row["yaw"])
    return row["x"] + rx * a + fx * b, row["y"] + ry * a + fy * b


def size(model):
    c = CAT[model]
    return c["lo"], c["hi"]


S_ = F["south"]


def bench(u, level=1, ds=0.0):
    """A point on the south field's town bench: u metres WEST of the gate's axis."""
    s = TIER_MID[level] + ds
    return S_.point(u, s)


# ================================================================== 1. THE GATE (south, facing the court)
GATE_Y = -49.0


def gate():
    put("gate_portal", 0.0, GATE_Y, 0.0, family="gate", note="faces the court; its own stair comes down to the rim at y -39, 9 m from the pit")
    put("gate_guardians", 0.0, GATE_Y + 13.7, 0.0, family="gate", note="the carabao pair at the stair's foot; the way to the court passes between them")
    x, y = S_.point(0.0, BENCH_S[1] - 1.0)
    put("gate_stair", x, y, 0.0, 0.92, family="gate", z=RIM_Z, wall="rise", note="the flight up to the town's bench behind the gate")
    for u, model in ((13.5, "gate_totem_a"), (-13.5, "gate_totem_b")):   # the free totems, on the bench, seen past the portal's shoulders
        x, y = bench(u, 1, -1.0)
        put(model, x, y, 0.0, family="gate")


# ================================================================== 2. GUARDIANS and the pit's walls
def pit():
    """The pit wall kit: an upper tier from the lip down to the ledge (five modules a side), the shaft under it (four,
    at 13 m each), a kerb on the lip, spout heads with a fall under each, niches with a small pair or jars, lizard panels."""
    walls = [(0.0, (1, -1)), (90.0, (-1, -1)), (180.0, (-1, 1)), (270.0, (1, 1))]       # the yaw a wall faces, the corner its run starts from
    upper = {0.0: ["pit_wall_niche", "pit_wall_spout", "pit_wall", "pit_wall_spout", "pit_wall_niche"],
             90.0: ["pit_wall", "pit_wall_niche", "pit_wall_spout", "pit_wall", "pit_wall_niche"],
             270.0: ["pit_wall_niche", "pit_wall", "pit_wall_spout", "pit_wall_niche", "pit_wall"],
             180.0: ["pit_wall_spout", "pit_wall_niche:half", None, "pit_wall_niche:half2", "pit_wall_spout"]}
    lower = {0.0: ["pit_wall"] * 4, 90.0: ["pit_wall"] * 4, 270.0: ["pit_wall"] * 4, 180.0: ["pit_wall", None, None, "pit_wall"]}
    k = 0
    for yaw, (cx, cy) in walls:
        rx, ry = run(yaw)
        fx, fy = front(yaw)
        for tier, half, foot, mods, sc in (("upper", HOLE, LEDGE_Z, upper[yaw], 1.0),):
            x0, y0 = cx * half, cy * half
            for i, model in enumerate(mods):
                if model is None or ":" in model:
                    continue                                    # the notch (the two half modules beside it are left to the cheeks' corners)
                x, y = x0 + rx * 12.0 * sc * i, y0 + ry * 12.0 * sc * i
                put(model, x, y, yaw, sc, family="pit", z=foot, wall=tier)
                mx, my = x + rx * 6.0, y + ry * 6.0
                if tier == "upper":
                    put("pit_kerb", x, y, yaw, family="pit", z=RIM_Z, wall="kerb")
                if model == "pit_wall_spout" and tier == "upper":
                    put("bulul_spout", mx, my, yaw, 1.0, family="bulul", z=foot + 6.47 - 1.15, wall="spout", note="in the wall's spout hole")
                    put("fall_medium", mx + fx * 1.15, my + fy * 1.15, yaw, 0.62, family="water", z=foot, wall="ledge", note="under the spout, its pool on the ledge")
                elif model == "pit_wall_niche":
                    nz = foot + 4.86
                    if k % 4 == 0:
                        put("bulul_small_m", mx - rx * 0.6 - fx * 0.35, my - ry * 0.6 - fy * 0.35, yaw, 1.15, family="bulul", z=nz, wall="niche")
                        put("bulul_small_f", mx + rx * 0.6 - fx * 0.35, my + ry * 0.6 - fy * 0.35, yaw, 1.15, family="bulul", z=nz, wall="niche")
                    else:
                        put("tapuy_jar_large", mx - rx * 0.55 - fx * 0.3, my - ry * 0.55 - fy * 0.3, yaw, 1.1, z=nz, wall="niche")
                        put("tapuy_jar_small", mx + rx * 0.45 - fx * 0.25, my + ry * 0.45 - fy * 0.25, yaw + 40, 1.1, z=nz, wall="niche")
                    k += 1
                elif model == "pit_wall" and tier == "upper":
                    put("bulul_lizard", mx + fx * 0.06, my + fy * 0.06, yaw, 1.0, family="bulul", z=foot + 5.0, wall="panel", note="a lizard panel on the plain wall")
    for (cx, cy, yaw) in ((1, -1, 270.0), (-1, -1, 0.0), (-1, 1, 90.0), (1, 1, 180.0)):
        put("pit_corner_in", cx * HOLE, cy * HOLE, yaw, family="pit", z=LEDGE_Z, wall="upper")

    # the notch's two cheeks, running north from the pit to the rim's edge
    for i in range(2):
        put("pit_wall" if i else "pit_wall_niche", -NOTCH, HOLE + 12.0 * i, 90.0, family="pit", z=LEDGE_Z, wall="upper")
        put("pit_wall", NOTCH, HOLE + 12.0 * (i + 1), 270.0, family="pit", z=LEDGE_Z, wall="upper")
        put("pit_kerb", -NOTCH, HOLE + 12.0 * i, 90.0, family="pit", z=RIM_Z, wall="kerb")
        put("pit_kerb", NOTCH, HOLE + 12.0 * (i + 1), 270.0, family="pit", z=RIM_Z, wall="kerb")

    put("pit_corner_out", -NOTCH, HOLE, 180.0, family="pit", z=LEDGE_Z, wall="upper")
    put("pit_corner_out", NOTCH, HOLE, 270.0, family="pit", z=LEDGE_Z, wall="upper")
    put("pit_wall_broken", -NOTCH, HOLE + 24.0, 90.0, family="pit", z=LEDGE_Z, wall="upper", note="the west cheek breaks down toward the valley")


def guardians():
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * 35.0, sy * 35.0
            put("bulul_seated", x, y, face(x, y), family="bulul", note="a giant at the pit's corner, facing the court")
    put("bulul_standing", -19.5, 36.5, face(-19.5, 36.5, 0, 0), family="bulul", note="the standing giant on the notch's west cheek")
    for name in ("east", "west"):
        q = SUBJECT[name]
        put(q["model"], q["x"], q["y"], face(q["x"], q["y"]), 1.25, family="bulul", z=q["z"], note="the giant over the %s rim, on its own platform ten walls up the terraces" % name)
    for sgn in (-1, 1):                                               # hornbill posts along the approach
        x, y = bench(sgn * 7.0, 1, 4.6)
        put("bulul_post", x, y, face(x, y, 0.0, y), family="bulul")
        x, y = bench(sgn * 7.0, 1, -3.0)
        put("bulul_post", x, y, face(x, y, 0.0, y), family="bulul")
        put("bulul_post", sgn * 9.0, GATE_Y - 8.8, 0.0, family="bulul")


# ================================================================== 3. THE TOWN
# kind, x, y, yaw (None = face the court), scale
HOUSES = [
    ("house_granary", 43.5, -21.0, None, 1.00),          # the rim, east
    ("house_shrine", 44.5, 0.0, 270.0, 1.00),
    ("house_gong", 43.5, 21.0, None, 1.05),
    ("house_granary", -43.5, -21.0, None, 1.06),          # the rim, west
    ("house_long", -44.0, 0.0, 90.0, 0.95),
    ("house_tiered", -42.5, 21.5, None, 0.92),
    ("house_gong", -27.0, 41.5, 180.0, 0.95),             # the rim, north, either side of the notch
    ("house_tower", 26.5, 41.0, 180.0, 1.0),
    ("house_tiered", -38.0, -49.5, None, 0.90),           # the rim, south, either side of the gate (the west one stands clear of the stream's line)
    ("house_shrine", 27.5, -47.0, 0.0, 0.86),
]
# on the three benches behind the gate: kind, metres WEST of the gate's axis, bench, scale. Never two of a kind side by
# side; only low roofs (granary, long house) stand between the court and the great fall (bearings 197 to 209).
BENCH_HOUSES = [
    ("house_tower", -25.0, 1, 1.08), ("house_gong", -37.0, 1, 1.00), ("house_granary", -48.0, 1, 1.05), ("house_tiered", -58.0, 1, 0.90),
    ("house_granary", 21.5, 1, 0.90), ("house_long", 45.5, 1, 0.90), ("house_gong", 59.5, 1, 0.95),
    ("house_granary", -54.0, 2, 1.00), ("house_tiered", -41.0, 2, 0.95), ("house_gong", -28.0, 2, 1.05), ("house_long", -13.5, 2, 0.90),
    ("house_tower", 11.0, 2, 0.95), ("house_gong", 22.0, 2, 0.92), ("house_granary", 31.0, 2, 0.92), ("house_long", 53.0, 2, 0.90),
    ("house_tower", -46.0, 3, 1.00), ("house_long", -30.0, 3, 0.90), ("house_granary", -13.5, 3, 1.08), ("house_gong", 0.0, 3, 1.10),
    ("house_tower", 13.5, 3, 0.92), ("house_long", 29.0, 3, 0.90), ("house_granary", 56.0, 3, 1.00),
]
# the hamlet on the knoll: kind, metres east of its axis, metres up it, scale. Each stands on its own small flat.
KNOLL_HOUSES = [("house_granary", -31.0, 47.0, 1.0), ("house_tiered", -15.0, 50.0, 1.0), ("house_tower", 1.0, 55.0, 1.1), ("house_gong", 16.5, 50.0, 1.05), ("house_granary", 31.0, 47.0, 1.08)]
for (_k, _u, _s, _sc) in KNOLL_HOUSES:
    _x, _y = KNOLL.point(_u, _s)
    HPADS.append((_x, _y, 7.5, 3.0, RIM_Z + STEP * round((float(_raw(_x, _y)) - RIM_Z) / STEP)))
# THE SUBJECTS over the east and west rims: a giant on its own platform ten walls up the terraces, over the house below
SUBJECT = {}
for _name, _model in (("east", "bulul_seated"), ("west", "bulul_standing")):
    _x, _y = F[_name].point(0.0, 10 * STEP / SLOPE + 3.0)
    SUBJECT[_name] = dict(model=_model, x=_x, y=_y, z=RIM_Z + 10 * STEP)
    HPADS.append((_x, _y, 7.5, 2.5, RIM_Z + 10 * STEP))
# houses on their own flats up the east and west terraces, either side of the giant: field, kind, metres along, walls up, scale
SLOPE_HOUSES = [("east", "house_tiered", -38.0, 5, 0.92), ("east", "house_tower", 20.0, 6, 1.0), ("west", "house_gong", -20.0, 6, 1.05), ("west", "house_tower", 37.0, 5, 1.05)]
SLOPE_AT = {}
for (_name, _k, _u, _w, _sc) in SLOPE_HOUSES:
    _x, _y = F[_name].point(_u, _w * STEP / SLOPE + 3.0)
    SLOPE_AT[(_name, _u)] = (_x, _y, RIM_Z + _w * STEP)
    HPADS.append((_x, _y, 8.6, 3.0, RIM_Z + _w * STEP))


def bench_yaw(u):
    """Facing straight down the hill from the bench at u."""
    phi = S_.mods[0][2]
    for (mu, mc, mphi, shape) in S_.mods:
        if u >= mu:
            phi = mphi + (MODULE[shape][2] / 2.0)
    yaw, d = S_.heading(phi)
    return math.degrees(math.atan2(d[1], -d[0])) % 360.0


def town():
    rows = []
    for kind, x, y, yaw, sc in HOUSES:
        rows.append(put(kind, x, y, face(x, y) if yaw is None else yaw, sc, family="house", where="rim"))
    for i, (kind, u, lv, sc) in enumerate(BENCH_HOUSES):
        x, y = bench(u, lv, 0.4 if lv == 1 else 0.2 if lv == 2 else -0.5)
        rows.append(put(kind, x, y, bench_yaw(u) + (_h(i, lv, 41) - 0.5) * (12.0 if lv < 3 else 5.0), sc, family="house", where="bench%d" % lv))
    for i, (kind, u, sv, sc) in enumerate(KNOLL_HOUSES):
        x, y = KNOLL.point(u, sv)
        rows.append(put(kind, x, y, face(x, y) + (i - 2) * 7.0, sc, family="house", where="knoll"))
    for (name, kind, u, walls, sc) in SLOPE_HOUSES:
        x, y, z = SLOPE_AT[(name, u)]
        rows.append(put(kind, x, y, face(x, y), sc, family="house", where="slope"))
    return rows


# What stands in a house's yard. (model, a, b, turn, scale): a metres to the house's right, b metres in FRONT OF ITS
# FRONT EDGE. Nothing taller than a jar stands before a facade; the lights stand at the corners, clear of every window.
YARDS = [
    [("offering_table", -3.4, 2.3, 0, 1), ("tapuy_jar_large", -4.9, 1.5, 30, 1), ("tapuy_jar_small", -2.3, 3.5, 80, 1), ("hagabi_bench", 4.0, 2.2, 10, 1)],
    [("hearth_pot", 2.9, 2.9, 0, 1), ("mortar_pestles", 5.3, 0.9, 20, 1), ("bilao_rice", 2.2, 3.9, 0, 1), ("basket_stack", -3.4, 1.3, 0, 1), ("pasiking", -4.4, 2.1, 160, 1)],
    [("chicken_coop", -3.9, 2.4, 20, 1), ("water_trough", 3.9, 2.2, 90, 1), ("basket_stack", 2.0, 3.6, 0, 0.95)],
    [("spears_shield", -3.0, 1.2, 0, 1), ("skull_post", 3.2, 1.3, 0, 1), ("tapuy_jar_large", 4.6, 2.4, 0, 1), ("tapuy_jar_open", 5.3, 1.5, 140, 1)],
    [("mortar_pestles", -3.3, 2.0, 0, 1), ("bilao_rice", -4.6, 2.9, 0, 1), ("pasiking", 3.0, 1.4, 200, 1), ("tapuy_jar_small", 2.4, 2.6, 0, 1)],
]
LIGHTS = ["lantern_pole", "torch", "lantern_pole", "banner", "lantern_pole", "torch"]
BRIDGES = []       # (bench, metres west of the axis, metres up the field): where a rope bridge crosses the stream
COVER = ["cover_fern", "cover_taro", "cover_grass"]


_NEIGHBOURS = []


def clear_of_pit(x, y, m=1.6):
    """Clear of the pit, and not inside a house (a yard's things never stand in the house next door)."""
    if any(_inside(poly, x, y) for poly in _NEIGHBOURS):
        return False
    return max(abs(x), abs(y)) > HOLE + m and not (abs(x) < NOTCH + m and y > 0)


def flat(x, y, z, r=1.3, tol=0.03):
    q = ground(np.array([x, x + r, x - r, x, x]), np.array([y, y, y, y + r, y - r]))
    return bool(np.all(np.abs(q - z) < tol))


def yards(houses):
    del _NEIGHBOURS[:]
    _NEIGHBOURS.extend(footprint(o, 0.25) for o in houses)
    for i, h in enumerate(houses):
        near = h.get("where") in ("rim", "bench1")
        lo, hi = size(h["model"])
        s = h["scale"]
        fr = -lo[1] * s
        half = max(-lo[0], hi[0]) * s
        back = hi[1] * s
        deep = h["model"] == "house_shrine"                 # its stair runs 10 m out: dress beside the stair
        for (model, a, b, turn, sc) in (YARDS[i % len(YARDS)] if near else YARDS[i % len(YARDS)][:2]):
            if deep:
                a = math.copysign(abs(a) + 4.6, a); b = b - 7.5
            a = max(-half - 1.5, min(half + 1.5, a))
            x, y = local(h, a, fr + b)
            if clear_of_pit(x, y) and flat(x, y, h["z"]):
                put(model, x, y, h["yaw"] + turn, sc, family="prop", owner=i)
        for side, model in (((-1 if i % 2 else 1), LIGHTS[i % len(LIGHTS)]),):
            x, y = local(h, side * (half + 1.1), fr - (8.2 if deep else 0.4))
            if clear_of_pit(x, y) and flat(x, y, h["z"]):
                put(model, x, y, (h["yaw"] + (90 if side < 0 else -90)) if model == "lantern_pole" else h["yaw"], family="prop", owner=i)
        if h["model"] in ("house_granary", "house_shrine") and near:  # a small pair at the granaries and the shrines
            for side, model in ((-1, "bulul_small_m"), (1, "bulul_small_f")):
                x, y = local(h, side * (2.3 if not deep else 4.1), fr + (0.7 if not deep else -1.2))
                if clear_of_pit(x, y) and flat(x, y, h["z"]):
                    put(model, x, y, h["yaw"], 1.0, family="bulul", owner=i)
        for j, (sa, sb) in enumerate(((-1, 1), (1, 1), (-1, -1), (1, -1))):       # green at the four corners
            x, y = local(h, sa * (half + 0.7), (fr + 0.5) if sb > 0 else -(back + 0.6))
            if clear_of_pit(x, y, 0.8) and flat(x, y, h["z"]):
                put(COVER[(i + j) % 3], x, y, 47.0 * (i + j), 1.0 + 0.1 * ((i + j) % 3), family="cover", owner=i)


# ================================================================== 4. PROPS that belong to the place, not to a house
def kit(x, y, yaw, items):
    fx, fy = front(yaw); rx, ry = right(yaw)
    for (m, a, b, t) in items:
        put(m, x + rx * a + fx * b, y + ry * a + fy * b, yaw + t)


def props():
    gy = GATE_Y
    put("gong_rack", -12.8, gy + 11.0, 15.0)
    put("spears_shield", 12.6, gy + 10.8, -15.0)
    put("hagabi_bench", -20.5, gy + 12.5, 30.0)
    put("offering_table", 12.2, gy + 14.4, 0.0)
    put("tapuy_jar_large", 13.8, gy + 13.6, 30.0)
    put("tapuy_jar_small", 10.9, gy + 13.4, 0.0)
    for sx in (-1, 1):
        put("fire_bowl", sx * 7.9, gy + 11.4, 0.0)
        put("banner", sx * 11.6, gy + 8.0, 0.0)
        put("banner", sx * 9.4, gy + 16.6, 0.0)
        put("fire_bowl", sx * 7.6, gy - 8.0, 0.0)             # the foot of the flight behind the gate
        x, y = bench(sx * 6.4, 1, 0.2)
        put("torch", x, y, 0.0)
    for sx in (-1, 1):                                       # two fire bowls before each giant
        for sy in (-1, 1):
            put("fire_bowl", sx * 40.2, sy * 32.6, face(sx * 40.2, sy * 32.6))
            put("fire_bowl", sx * 32.6, sy * 40.2, face(sx * 32.6, sy * 40.2))
    e = HOLE + 2.0                                           # lanterns along the pit's edge, their lamps hanging toward the next one
    for t in (-22.0, -8.0, 8.0, 22.0):
        put("lantern_pole", e, t, 180.0 if t > 0 else 0.0)
        put("lantern_pole", -e, t, 180.0 if t > 0 else 0.0)
    for t in (-24.0, -15.5, 15.5, 24.0):
        put("lantern_pole", t if t != -15.5 else -18.0, -e, 270.0 if t > 0 else 90.0)      # the stream's runnel comes to the kerb at x -13.5
        put("lantern_pole", t, e, 270.0 if t > 0 else 90.0)
    for sx in (-1, 1):                                       # the notch: banners and skulls on its two cheeks
        put("banner", sx * 15.0, 33.0, 180.0)
        put("skull_post", sx * 15.4, 49.0, 180.0 + sx * 90.0)
    put("gong_rack", 18.8, 35.5, 200.0)
    put("spears_shield", 16.4, 41.5, 270.0)
    # the rope bridge over the stream bed
    gf = F[GULLY["field"]]
    bx, by = gf.point(GULLY["u"], 11.0)
    byaw, side = gf.heading(gf.phi_at(GULLY["u"]))
    put("rope_bridge", bx, by, byaw, 1.0, z=(g(bx + 3.55 * side[0], by + 3.55 * side[1]) + g(bx - 3.55 * side[0], by - 3.55 * side[1])) / 2.0 - 0.45,
        note="spans the stream bed on the east terraces; its deck sags 0.58 m below its ends")
    # THE SUBJECTS over the east and west rims: fire and banners on the giant's platform, a gong and an offering below
    for name in ("east", "west"):
        q = SUBJECT[name]
        yaw = face(q["x"], q["y"])
        fx, fy = front(yaw); rx, ry = right(yaw)
        for sgn in (-1, 1):
            put("fire_bowl", q["x"] + rx * sgn * 4.4 + fx * 4.6, q["y"] + ry * sgn * 4.4 + fy * 4.6, yaw, z=q["z"])
            put("banner", q["x"] + rx * sgn * 5.8 + fx * 1.2, q["y"] + ry * sgn * 5.8 + fy * 1.2, yaw, z=q["z"])
        sx = 1.0 if name == "east" else -1.0
        hy = 270.0 if name == "east" else 90.0
        put("gong_rack", sx * 37.6, 12.6, hy)
        put("offering_table", sx * 37.4, -12.2, hy)
        put("tapuy_jar_large", sx * 38.9, -13.6, hy + 30.0)
        put("tapuy_jar_open", sx * 36.2, -13.9, hy + 150.0)
        for sy in (-1, 1):
            put("banner", sx * 36.2, sy * 16.4, hy)
            put("fire_bowl", sx * 39.6, sy * 9.6, hy)
        put("spears_shield", sx * 39.4, 15.2, hy)
    # the stream through the town: a rope bridge where each bench's path crosses it
    for lv in (1, 2, 3):
        want = TIER_MID[lv] - (5.6 if lv == 1 else 4.6 if lv == 2 else 3.6)
        rr = np.arange(60.0, 130.0, 0.1)
        sv = S_.us_of(rr * SU[0], rr * SU[1])[1]
        r = float(rr[int(np.argmax(sv >= want))])
        bx, by = stream_point(r)
        put("rope_bridge", bx, by, STREAM_YAW, 1.0, z=(g(*stream_point(r, 3.55)) + g(*stream_point(r, -3.55))) / 2.0 - 0.38,
            note="spans the stream on bench %d" % lv)
        BRIDGES.append((lv, float(S_.us_of(bx, by)[0]), want))
    # work yards on the bench's open ground
    feast = (("offering_table", 0, 0, 0), ("hagabi_bench", 0, -1.6, 0), ("tapuy_jar_large", -1.6, 0.3, 0), ("tapuy_jar_open", 1.6, 0.4, 150),
             ("hearth_pot", 2.9, -0.6, 0), ("bilao_rice", -1.3, 1.3, 0), ("basket_stack", -2.9, -0.7, 0))
    wet = (("water_pipe", 0, 0, 90), ("water_trough", 3.2, 0.8, 0), ("chicken_coop", -3.0, 0.4, 0), ("tapuy_jar_small", 1.9, 2.0, 0))
    for u, ds, items in ((-15.5, -4.9, wet), (16.0, -4.9, feast)):
        x, y = bench(u, 1, ds)
        kit(x, y, bench_yaw(u), items)
    # the footpath: round the pit behind the lanterns, and across the bench behind the gate
    ring = HOLE + 4.6
    for i in range(8):
        t = -24.0 + 6.0 * i
        put("path_stones", ring, t, 180.0, family="path")
        put("path_stones", -ring, t + 6.0, 0.0, family="path")
        if not (-12.0 <= t < 6.0):
            put("path_stones", t, -ring, 270.0, family="path")
        if t < -18.0 or t >= 18.0:
            put("path_stones", t + 6.0, ring, 90.0, family="path")
    for k in range(2):
        x, y = bench(0.0, 1, -6.0 + 6.0 * k)
        put("path_stones", x, y, 0.0, family="path")
    for lv in (2, 3):                                                # up the axis across the upper benches
        for k in range(2):
            x, y = S_.point(0.0, TIERS[2 * lv - 1][0] + 0.4 + 6.0 * (k + 1) - (0.0 if lv == 2 else 2.0 * k + 1.0))
            put("path_stones", x, y, 0.0, family="path")
    for (lv, bu, sv) in BRIDGES:                                     # along each bench's front, from the axis to the bridge and on
        for u0 in np.arange(-44.0, 50.0, 6.0):
            if abs(u0 + 3.0) < 8.5 or abs(u0 + 3.0 - bu) < 6.4:
                continue
            x, y = S_.point(u0, sv)
            put("path_stones", x, y, 270.0, family="path")
    # grass at the kerb, between the lanterns
    for i, t in enumerate((-27.0, -18.0, -12.0, -3.0, 4.0, 12.5, 19.0, 27.5)):
        for (x, y) in ((HOLE + 1.3, t), (-HOLE - 1.3, t), (t, -HOLE - 1.3), (t, HOLE + 1.3)):
            if clear_of_pit(x, y, 0.9) and not (abs(x) < 9.0 and y < 0):
                put(COVER[(i + int(abs(x) + abs(y))) % 3], x, y, 61.0 * i, 0.95, family="cover")
                i += 1


# ================================================================== 5. WATER and its cliffs
def water():
    for s in SCARPS:
        x, y = s["at"]
        yaw = s["yaw"]
        fx, fy = front(yaw); rx, ry = run(yaw)
        great = s["h"] > 20
        put("fall_great" if great else "fall_medium", x, y, yaw, 1.0, family="water", z=s["level"], note=s["name"])
        if great:
            continue
        base = s["level"] - 4.4
        for i in range(2):
            put("cliff_wall", x - fx * 1.25 + rx * 12.0 * (i - 1.0), y - fy * 1.25 + ry * 12.0 * (i - 1.0), yaw, 1.0, family="cliff", z=base, wall="cliff")
    # THE STREAM: its falls, each facing the can
    yaw = STREAM_YAW
    for i, s_ in enumerate(STREAM):
        x, y = stream_point(s_["r"])
        put(s_["model"], x, y, yaw, s_["scale"], family="water", z=s_["lower"], note="stream %d, %.2f m" % (i, s_["upper"] - s_["lower"]))
    x, y = stream_point(R_HEAD)
    put("fall_great", x, y, yaw, 1.0, family="water", z=POOL_Z, note="great")
    # THE GORGE'S WALLS: the cliff kit along both sides from the highest medium fall to the head, one module over
    # another up to the land's own edge, and across the head behind the great fall.
    starts = [R_GORGE, STREAM[6]["r"], STREAM[7]["r"], STREAM[7]["r"] + 12.0, R_HEAD - 12.3]
    for r0 in starts:
        floor = float(stream_bed(r0 + 0.6))
        half = float(stream_half(r0 + 6.0))
        w = stream_wide(r0 + 4.0)
        for sg in (1.0, -1.0):
            rim = float(_raw(*stream_point(r0 + 4.0, sg * (w + 0.5))))
            px, py = stream_point(r0 + 12.6 if sg > 0 else r0 - 0.6, sg * (half + 0.9))
            _cliff_stack(px, py, (STREAM_B - 90.0 * sg) % 360.0, floor, rim, (SL[0] * sg, SL[1] * sg), 0.25)
    for j, (dz, cols) in enumerate(((-1.2, (-19.0, -6.5, 6.0)), (13.2, (-22.9, -11.6, -0.3, 11.0)), (GREAT_H + 0.6 - 13.8, (-26.8, -13.3, 0.2, 13.7)))):
        for b0 in cols:
            px, py = stream_point(R_HEAD + 1.25 + 0.4 * j, b0)
            put("cliff_wall", px, py, yaw, 1.0, family="cliff", z=POOL_Z + dz, wall="cliff")
    for s_ in STREAM[3:6]:                                           # a wall of rock behind each of the hillside's three falls
        px, py = stream_point(s_["r"] + 1.25, -6.4)                   # one module, centred behind the fall (the budget)
        put("cliff_wall", px, py, yaw, 1.0, family="cliff", z=s_["upper"] + 1.2 - 13.8, wall="cliff")
    # its bed and banks: boulders at every fall's foot and along the gorge's floor, fern and taro on the banks
    k = 0
    for s_ in STREAM:
        for sg in (-1.0, 1.0):
            h = float(stream_half(s_["r"] - 1.0))
            x, y = stream_point(s_["r"] - 0.6 - 1.4 * (k % 2), sg * (h - 0.2))
            put("boulder_%d" % (3 + k % 3), x, y, k * 71.0, 0.9 + 0.3 * (k % 3) / 2.0, family="cover", on="stream bed", z=g(x, y) - 0.05)
            x, y = stream_point(s_["r"] + 1.2, sg * (h + 0.8))
            put(COVER[k % 3], x, y, k * 47.0, 1.2, family="cover", on="stream bed", z=g(x, y))
            k += 1
    r = R_END + 2.0
    while r < R_HEAD - 16.0:
        h = float(stream_half(r))
        sg = 1.0 if k % 2 else -1.0
        big = r > STREAM[3]["r"]
        if not any(-5.0 * s_["scale"] - 1.0 < r - s_["r"] < 2.5 for s_ in STREAM):
            x, y = stream_point(r, sg * h * (0.55 if big else 0.9))
            put("boulder_%d" % ((1 + k % 3) if not big else (3 + k % 3)), x, y, k * 53.0, 1.0 + (0.4 if big else 0.0) * _h(k, 3, 61), family="cover", on="stream bed", z=g(x, y) - 0.05)
            x, y = stream_point(r + 1.3, -sg * (h + 0.3))
            put(COVER[(k + 1) % 3], x, y, k * 29.0, 1.1, family="cover", on="stream bed", z=g(x, y))
        r += 2.6 if not big else 3.4
        k += 1
    # the stream bed: boulders and green down the gully
    gf = F[GULLY["field"]]
    k = 0
    for sv in (3.0, 6.5, 15.0, 18.5):
        x, y = gf.point(GULLY["u"], sv)
        put("boulder_%d" % (1 + (k * 2) % 4), x, y, k * 67.0, 1.0, family="cover", dz=-0.05, on="stream bed")
        x, y = gf.point(GULLY["u"] + 0.3, sv + 1.6)
        put(COVER[k % 2], x, y, k * 40.0, family="cover", on="stream bed")
        k += 1


def _cliff_stack(px, py, yaw, floor, rim, back, lean):
    """Cliff modules one over another from a floor up to the land's edge; each higher one leans `lean` back into the hill."""
    H = rim - floor
    if H < 3.5:
        return
    m = max(1, int(math.ceil((H + 1.0) / 14.0)))
    for j in range(m):
        base = (rim + 0.6 - 13.8) if m == 1 else floor - 1.2 + j * (H + 1.8 - 13.8) / (m - 1)
        off = max(0.0, base - floor) * lean
        put("cliff_wall", px + back[0] * off, py + back[1] * off, yaw, 1.0, family="cliff", z=base, wall="cliff")


def _h(i, j, k=0):
    """A fixed scatter number in 0..1 for a grid cell (no random module: the same map every run)."""
    n = (i * 73856093) ^ (j * 19349663) ^ (k * 83492791)
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFF) / 65535.0


TREE_KEEP = []


def trees():
    """Forest where no rice grows: the seams between the fields, the ground above and beside the rows, the brow and
    flanks of the rock band, the ridges. Pines high, figs lower, palms and banana among the houses. (The trees are the
    project's own from the earlier Payo set; the approved pieces have none.)"""
    occ = {}
    for r in ROWS:                                              # the rows, sampled every 3 m: no tree within 4 m of one
        (x0, y0), (x1, y1) = (r["x"], r["y"]), r["end"]
        for k in range(5):
            qx, qy = x0 + (x1 - x0) * k / 4.0, y0 + (y1 - y0) * k / 4.0
            qx, qy = qx - r["t"][1] * 1.3, qy + r["t"][0] * 1.3          # the paddy's middle, not the wall's foot
            occ.setdefault((int(qx // 4), int(qy // 4)), []).append((qx, qy))

    def on_rows(x, y, d):
        cx, cy = int(x // 4), int(y // 4)
        m = int(d // 4) + 1
        for a in range(-m, m + 1):
            for b in range(-m, m + 1):
                for (qx, qy) in occ.get((cx + a, cy + b), ()):
                    if (qx - x) ** 2 + (qy - y) ** 2 < d * d:
                        return True
        return False

    solids = [footprint(q, 3.0) for q in P if q["family"] in ("house", "gate", "cliff") or q["model"] in ("bulul_seated", "bulul_standing", "fall_great", "fall_medium")]
    things = [(q["x"], q["y"]) for q in P if q["family"] in ("prop", "bulul", "path", "water", "terrace_prop")]

    def near_thing(x, y, d):
        return any((x - a) ** 2 + (y - b) ** 2 < d * d for (a, b) in things)

    n = 0
    cell = 4.8
    for i in range(-50, 51):
        for j in range(-50, 52):
            x, y = (i + 0.05 + 0.9 * _h(i, j, 1)) * cell, (j + 0.05 + 0.9 * _h(i, j, 2)) * cell
            r = math.hypot(x, y)
            knoll = y > 122.0 and abs(x) < 78.0
            if r < RIM_R + 3.0 or (r > 235.0 and not knoll) or (y > 125.0 and not knoll):
                continue
            if abs(x) < 9.0 and y < 0 and r < 120:
                continue                                        # the approach, and the way up the benches
            sr0, sb0 = stream_frame(x, y)
            if abs(float(sb0)) < 10.0 and 20.0 < float(sr0) < STREAM[3]["r"]:
                continue                                        # the stream through the town stays in view from the court
            if y > 0 and abs(x) < 22.0 and not knoll:
                continue
            # the forest stands in clumps: thick where this slow pattern is high, thin between, thinner far off
            clump = 0.5 + 0.5 * math.sin(x * 0.047 + 1.7 * math.sin(y * 0.033)) * math.cos(y * 0.051 + 1.3 * math.sin(x * 0.029))
            want = 0.40 + 0.60 * float(smooth(0.30, 0.62, clump))
            if r > 150.0 and not knoll:
                want *= 0.42
            if _h(i, j, 5) > want:
                continue
            sr, sb = stream_frame(x, y)
            if abs(float(sb)) < 34.0 and R_END - 4.0 < float(sr) < R_HEAD + 6.0 and in_stream(x, y, 1.2):
                continue
            if on_rows(x, y, 4.2):
                continue
            z = g(x, y)
            gx, gy = gradient(x, y, 2.0)
            sl = math.hypot(gx, gy)
            rib = float(seam(x, y)) > 0.5 and abs(x) < 122.0 and -164.0 < y < 122.0 and sl < 4.4
            if rib and sl > 1.3 and z > RIM_Z - 34.0 and not any(_inside(poly, x, y) for poly in solids) and bearing(x, y) > 60.0 and bearing(x, y) < 300.0:
                # the steep forest of a rib: trees stand thick on it, their roots well into the slope
                t = _h(i, j, 3)
                model = ("tree_fig_a", "tree_fig_b", "tree_fig_c", "tree_pine_b", "tree_pine_a")[int(t * 4.999)] if z < 40.0 else ("tree_pine_a", "tree_pine_b")[int(t * 1.999)]
                put(model, x, y, 360.0 * _h(i, j, 4), 1.2 + 0.7 * _h(i, j, 6), family="tree", group="plant", z=z - 1.2, rib=True)
                continue
            if sl > 1.3 or z < RIM_Z - 34.0 or abs(z - RIM_Z) < 0.05:
                continue
            if any(abs(z - (RIM_Z + tz)) < 0.05 for tz in TIER_Z.values()):
                continue                                        # the town's benches are the houses' ground
            ring = ground(np.array([x + 6.0, x - 6.0, x, x]), np.array([y, y, y + 6.0, y - 6.0]))
            if abs(float(ring.mean()) - z) > 0.9:
                continue                                        # a sharp rib or hollow: the far land mesh is too coarse to seat a tree there
            if any(_inside(poly, x, y) for poly in solids) or any((x - kx) ** 2 + (y - ky) ** 2 < kr * kr for (kx, ky, kr) in KEEP):
                continue
            t = _h(i, j, 3)
            if z > 45.0 or (z > 22.0 and t < 0.5):
                model = "tree_pine_a" if t < 0.55 else "tree_pine_b"
            else:
                model = ("tree_fig_a", "tree_fig_b", "tree_fig_c")[int(t * 2.999)]
            put(model, x, y, 360.0 * _h(i, j, 4), 1.0 + 0.8 * _h(i, j, 6) ** 1.5, family="tree", group="plant", z=z - (0.15 if r < 118.0 else 0.6))
            n += 1
            if r < 150.0 or knoll:                              # undergrowth at its foot
                if _h(i, j, 7) < 0.55:
                    ax, ay = x + 2.2 * (_h(i, j, 8) - 0.5) * 2.0, y + 2.2 * (_h(i, j, 9) - 0.5) * 2.0
                    if not on_rows(ax, ay, 2.5) and abs(g(ax, ay) - (z + (ax - x) * gx + (ay - y) * gy)) < 0.5:
                        put(COVER[int(_h(i, j, 10) * 2.999)], ax, ay, 360.0 * _h(i, j, 11), 1.6 + 0.9 * _h(i, j, 12), family="cover", on="forest floor", z=g(ax, ay) - 0.05)
                if _h(i, j, 13) < 0.13:
                    ax, ay = x - 2.6, y + 1.9
                    if not on_rows(ax, ay, 2.5):
                        put("boulder_%d" % (3 + int(_h(i, j, 14) * 2.999)), ax, ay, 360.0 * _h(i, j, 15), 1.0 + 0.6 * _h(i, j, 16), family="cover", on="forest floor", z=g(ax, ay) - 0.1)
    # the gorge's rims and the hillside falls' banks: trees crowd both sides, leaning in
    k = 0
    r = STREAM[3]["r"] - 4.0
    while r < R_HEAD + 10.0:
        for sg in (-1.0, 1.0):
            w = stream_wide(min(r, R_HEAD)) + 2.4 + 3.2 * _h(k, 2, 71)
            x, y = stream_point(r + 2.0 * (_h(k, 3, 72) - 0.5), sg * w) if r < R_HEAD + 2.0 else stream_point(r, sg * 26.0 * _h(k, 4, 73))
            k += 1
            if on_rows(x, y, 3.2) or any(_inside(poly, x, y) for poly in solids):
                continue
            z = g(x, y)
            if abs(float(_raw(x, y)) - z) > 0.2:
                continue
            model = ("tree_fig_a", "tree_fig_b", "tree_fig_c", "tree_pine_b")[k % 4] if z < 46.0 else ("tree_pine_a", "tree_pine_b", "tree_fig_b")[k % 3]
            put(model, x, y, 67.0 * k, 1.1 + 0.6 * _h(k, 5, 74), family="tree", group="plant", z=z - 0.3)
            ax, ay = stream_point(r + 1.5, sg * (w - 1.6))
            if abs(float(_raw(ax, ay)) - g(ax, ay)) < 0.2 and not on_rows(ax, ay, 2.5):
                put(COVER[k % 3], ax, ay, 31.0 * k, 2.0, family="cover", on="forest floor", z=g(ax, ay) - 0.05)
        r += 4.2
    # THE RIM'S LAWN and the benches: fern, taro and grass in drifts, a bush now and then, clear of every path and prop
    for i in range(-22, 23):
        for j in range(-40, 23):
            x, y = (i + 0.1 + 0.8 * _h(i, j, 81)) * 3.0, (j + 0.1 + 0.8 * _h(i, j, 82)) * 3.0
            z = g(x, y)
            on_bench = any(abs(z - (RIM_Z + tz)) < 0.02 for tz in TIER_Z.values())
            if not (abs(z - RIM_Z) < 0.02 or on_bench) or not clear_of_pit(x, y, 2.4):
                continue
            if not flat(x, y, z, 1.0):
                continue
            if abs(x) < 9.5 and y < 0:
                continue                                        # the gate's forecourt and the way through it stay open
            drift = 0.5 + 0.5 * math.sin(x * 0.21 + 1.1 * math.sin(y * 0.17)) * math.cos(y * 0.23 + 0.7)
            if _h(i, j, 83) > 0.12 + 0.5 * float(smooth(0.45, 0.8, drift)):
                continue
            if any(_inside(poly, x, y) for poly in solids) or near_thing(x, y, 2.3) or any(_inside(poly, x, y) for poly in _NEIGHBOURS):
                continue
            t = _h(i, j, 84)
            if t < 0.07 and max(abs(x), abs(y)) > HOLE + 9.0:
                put(("payo_plant_bush_0", "payo_plant_bush_2", "payo_plant_broadleaf_1")[int(_h(i, j, 85) * 2.999)], x, y, 360.0 * t, 1.0 + 0.4 * _h(i, j, 86), family="tree", group="plant")
            else:
                put(COVER[int(t * 2.999)], x, y, 360.0 * _h(i, j, 85), 1.0 + 0.8 * _h(i, j, 86), family="cover", z=z)
    # palms, banana and bushes among the houses, clear of doors and windows: behind and beside each house
    houses = [q for q in P if q["family"] == "house"]
    for i, h in enumerate(houses):
        lo, hi = size(h["model"])
        sc = h["scale"]
        half, back = max(-lo[0], hi[0]) * sc, hi[1] * sc
        spots = (((half + 2.6), -(back - 1.0), ("payo_plant_palm_0", "payo_plant_palm_2", "payo_plant_palm_4")[i % 3]),
                 (-(half + 2.2), -(back + 0.5), "payo_plant_broadleaf_0"), ((half + 1.6), 1.0, ("payo_plant_bush_0", "payo_plant_bush_2")[i % 2]))
        for (a, b, model) in (spots if i % 3 == 0 else spots[1:]):
            x, y = local(h, a, b)
            if not clear_of_pit(x, y, 2.0) or any(_inside(poly, x, y) for poly in solids if poly is not None and not _inside(footprint(h, 3.0), x, y)):
                continue
            if any(_inside(footprint(o, 1.5), x, y) for o in houses):
                continue
            if abs(g(x, y) - h["z"]) > 0.3:
                continue
            put(model, x, y, 77.0 * i, 1.0, family="tree", group="plant")


# ================================================================== 6. TERRACES: every row is its field's chain, moved up the hill
CROPS = ["flooded", "young", "young", "ripe", "flooded", "ripe", "young", "flooded"]
KEEP = []          # (x, y, radius): no terrace wall here
BOXES = []         # footprints no terrace wall may enter
FULL_D, MID_D = 29.5, 46.0          # metres from where a player can stand (the 44 m square): full, mid, then far (far from about 82 m off the can)
ROWS = []


def _inside(poly, x, y):
    n = len(poly); inside = False
    for i in range(n):
        x0, y0 = poly[i]; x1, y1 = poly[(i + 1) % n]
        if (y0 > y) != (y1 > y) and x < (x1 - x0) * (y - y0) / (y1 - y0) + x0:
            inside = not inside
    return inside


def from_play(x, y):
    return math.hypot(max(0.0, abs(x) - PLAY), max(0.0, abs(y) - PLAY))


_FIELD = [""]


def allowed(x, y):
    for sc in SCARPS:
        into, side = _scarp_frame(sc, x, y)
        into, side = float(into), abs(float(side))
        if sc["h"] > 20:
            continue
        elif -CUT - 5.0 < into < 5.0 and side < 14.0:                                          # the cut pool and its banks
            return False
    knoll = _FIELD[0] == "knoll"
    if not knoll and (math.hypot(x, y) > 205.0 or y > 100.0 or (y > 80.0 and abs(x) > 95.0)):
        return False
    if abs(x) < 7.8 and y < 0 and math.hypot(x, y) < 81.0:  # the approach behind the gate, up to the first bench
        return False
    if y > 0 and abs(x) < 19.0 and not knoll:               # the notch and the valley's mouth
        return False
    sr, sb = stream_frame(x, y)
    if abs(float(sb)) < 34.0 and R_END - 4.0 < float(sr) < R_HEAD + 8.0 and in_stream(x, y, 2.6):
        return False
    gu, gs = F[GULLY["field"]].us_of(x, y)
    if abs(float(gu) - GULLY["u"]) < 4.9 and -1.0 < float(gs) < GULLY["s1"] + 3.0:
        return False
    for (kx, ky, kr) in KEEP:
        if (x - kx) ** 2 + (y - ky) ** 2 < kr * kr:
            return False
    for poly in BOXES:
        if _inside(poly, x, y):
            return False
    return True


def terraces():
    run_id = 0
    for f in ROW_FIELDS:
        _FIELD[0] = f.kind
        rows = f.rows()
        for k, (s, level0) in enumerate(rows):
            if f.kind == "bench" and k < 6 and False:
                continue
            seg = 0
            open_run = False
            for j, (u, c, phi, shape) in enumerate(f.mods):
                O = f.O + f.t * u + f.A * (c + s)
                yaw, d = f.heading(phi)
                ex, ey, tn = MODULE[shape]
                n = np.array([-d[1], d[0]])
                end = O + d * ex + n * ey
                mid = O + d * ex * 0.5 + n * ey * 0.25
                ok = True
                # above a fall's cliff the land is a whole number of walls higher: the row keeps its line and takes that level
                lift = round((g(mid[0], mid[1]) - (level0 + 0.10)) / STEP) * STEP
                level = level0 + lift
                if f.kind == "knoll" and abs(lift) > 0.01:
                    ok = False
                for q in (O + d * 0.4, O * 0.6 + mid * 0.4 + (end - O) * 0.02, mid, end * 0.6 + mid * 0.4, end - d * 0.4):
                    if not allowed(q[0], q[1]) or abs(g(q[0], q[1]) - (level + 0.10)) > 0.22:
                        ok = False
                # a row keeps 8 m inside its own field (the seams between fields are bare) and off any sudden slope
                for q in (O, mid, end):
                    rq = math.hypot(q[0], q[1])
                    off = float(wrap(float(bearing(q[0], q[1])) - f.centre))
                    edge = math.degrees((8.0 if rq < 110.0 else 19.0) / max(rq, 30.0))
                    if f.kind != "knoll" and not (f.core[0] - f.centre + edge <= off <= f.core[1] - f.centre - edge):
                        ok = False
                if ok:
                    gx3, gy3 = gradient(mid[0], mid[1], 3.0)
                    if not (0.33 <= math.hypot(gx3, gy3) <= 1.0):
                        ok = False
                if open_run and abs(level - ROWS[-1]["level"]) > 0.01:
                    _cap(f, k, ROWS[-1]["level"], ROWS[-1]); open_run = False
                # ragged ends: a row may start one module late or stop one early
                if _h(k // 2, j // 3, 21) < 0.06 and from_play(mid[0], mid[1]) > 50.0:
                    ok = False                                  # a break in the rows now and then
                if _h((k + 3) // 7, (j + 1) // 2, 22) < 0.13 and from_play(mid[0], mid[1]) > 62.0 and f.kind != "knoll":
                    ok = False                                  # an island of trees among the far rows: seven rows deep, two modules long
                if not ok:
                    if open_run:
                        _cap(f, k, ROWS[-1]["level"], ROWS[-1])
                    open_run = False
                    continue
                if not open_run:
                    run_id += 1; seg = 0
                gx, gy = gradient(mid[0], mid[1])
                dist = from_play(mid[0], mid[1])
                lod = "full" if dist < FULL_D else "mid" if dist < MID_D else "far"
                if lod == "far" and f.kind == "bench" and k <= 5 and abs(float(u)) < 40.0:
                    lod = "mid"                                 # the rows between the town's benches are among the houses
                shift = int(_h(k // 3, 5, 77) * 4.0)            # each band of rows starts its patches somewhere else along the hill
                crop = CROPS[int(_h(k // 3, (j + shift) // 4, 9 + len(f.name)) * 7.999)]      # patches of several modules and a few rows
                if lod == "far" and crop == "flooded" and _h(k // 3, (j + shift) // 4, 33) < 0.72:
                    crop = "young" if _h(k // 3, (j + shift) // 4, 34) < 0.5 else "ripe"       # water is a minority far off: its pale brim reads as a line
                ROWS.append(dict(field=f.name, level=level, k=k, x=float(O[0]), y=float(O[1]), yaw=yaw, shape=shape, crop=crop, lod=lod, run=run_id, seg=seg,
                                 end=(float(end[0]), float(end[1])), mid=(float(mid[0]), float(mid[1])), t=(float(d[0]), float(d[1])), end_phi=phi + tn))
                open_run = True; seg += 1
            if open_run:
                _cap(f, k, ROWS[-1]["level"], ROWS[-1])


def _cap(f, k, level, last):
    """A row's end is closed by the cap, which turns into the hill (there is no cap for a far row: it just stops)."""
    if last["lod"] == "far" or from_play(*last["end"]) > 44.0:
        return
    x, y = last["end"]
    yaw, d = f.heading(last["end_phi"])
    q = (x + d[0] * 2.5, y + d[1] * 2.5)
    if not allowed(*q) or abs(g(*q) - (level + 0.1)) > 0.5:
        return
    ROWS.append(dict(field=f.name, level=level, k=k, x=x, y=y, yaw=yaw, shape="cap", crop=last["crop"], lod="full", run=last["run"], seg=last["seg"] + 1,
                     end=q, mid=q, t=(float(d[0]), float(d[1])), end_phi=last["end_phi"]))


def terrace_rows():
    """The rows as placements. Far off, a run of straight modules of one crop is ONE long module (24 or 48 m)."""
    i = 0
    while i < len(ROWS):
        r = ROWS[i]
        span = 1
        if r["lod"] == "far" and r["shape"] == "straight":
            while span < 4 and i + span < len(ROWS):
                q = ROWS[i + span]
                if not (q["run"] == r["run"] and q["seg"] == r["seg"] + span and q["lod"] == "far" and q["shape"] == "straight" and q["crop"] == r["crop"]
                        and abs(q["level"] - r["level"]) < 0.01 and abs(q["yaw"] - r["yaw"]) < 0.01):
                    break
                span += 1
            if span == 3:
                span = 2
        model = "terrace_%s_%s" % (r["shape"], r["crop"])
        if r["lod"] == "mid":
            model += "_mid" if _h(r["k"], r["seg"] + 7 * r["run"], 51) < 0.2 else "_mid_plain"       # one in five carries the spout's fall
        elif r["lod"] == "far":
            model += {1: "_far", 2: "_far24", 4: "_far48"}[span]
        put(model, r["x"], r["y"], r["yaw"], 1.0, family="terrace", group="terrace", z=r["level"] - 0.05, lod=r["lod"], far=r["lod"] == "far", row=r["k"], run=r["run"],
            field=r["field"], span=12 * span)
        i += span


def terrace_dressing():
    """What stands on the nearer rows: scarecrows in the growing paddies, the wall stair, bamboo pipes along a bund,
    drying racks, and a boulder with green at every row's open end."""
    near = [r for r in ROWS if r["lod"] != "far" and r["shape"] != "cap"]
    for i, r in enumerate(near):
        tx, ty = r["t"]; nx, ny = -ty, tx
        foot = r["level"] - 0.05
        if r["seg"] == 0 and (r["lod"] == "full" or i % 2 == 0):
            put("boulder_%d" % (3 + (i % 3)), r["x"] - tx * 0.2 + nx * 0.7, r["y"] - ty * 0.2 + ny * 0.7, i * 53.0, 1.0, family="cover", z=foot + 0.25, on="row end")
            put(COVER[i % 3], r["x"] - tx * 1.0 - nx * 0.6, r["y"] - ty * 1.0 - ny * 0.6, i * 31.0, family="cover", z=foot + 0.1, on="row end")
        if r["crop"] != "flooded" and i % 5 == 2:
            put("terrace_scarecrow", r["mid"][0] + nx * 1.45, r["mid"][1] + ny * 1.45, r["yaw"], family="terrace_prop", z=foot + STEP - 0.12, on="paddy")
        if i % 11 == 4 and r["shape"] == "straight":
            put("terrace_stair", r["x"] + tx * 3.0, r["y"] + ty * 3.0, r["yaw"], family="terrace_prop", z=foot, on="wall")
        if i % 9 == 7 and r["shape"] == "straight":
            for s in range(3):
                put("terrace_pipe", r["x"] + tx * (1.5 + 3.0 * s) + nx * 0.45, r["y"] + ty * (1.5 + 3.0 * s) + ny * 0.45, r["yaw"], family="terrace_prop",
                    z=foot + STEP + 0.03 - 0.12 * s, on="bund")
        if i % 17 == 9 and r["shape"] == "straight":
            put("terrace_rack", r["mid"][0] + nx * 0.36, r["mid"][1] + ny * 0.36, r["yaw"], 0.9, family="terrace_prop", z=foot + STEP + 0.03, on="bund")
    for r in ROWS:                                               # the way up the axis between the benches: a wall stair on every row there
        if r["field"] == "south" and 2 <= r["k"] <= 9 and r["shape"] == "straight" and abs(r["x"]) < 0.5:
            tx, ty = r["t"]
            put("terrace_stair", r["x"] + tx * (0.8 + 2.4 * (r["k"] % 2)), r["y"] + ty * (0.8 + 2.4 * (r["k"] % 2)), r["yaw"], family="terrace_prop", z=r["level"] - 0.05, on="wall")


# ================================================================== 7. THE STAGE
def stage():
    """The block table. A block's glb is drawn by Unity (glTFast) with its glTF +x on the WEST at yaw 0, so each
    block gets the yaw (a half or quarter turn its footprint allows) that puts its deepest carving on its OUTER side."""
    lay = json.load(open(LAYOUT))
    late = json.load(open(os.path.join(LATE_STONE, "blocks.json"))) if os.path.exists(os.path.join(LATE_STONE, "blocks.json")) else {}
    by_id = {b["id"]: b for b in late.get("blocks", [])}
    ids = {}
    for L in lay["layouts"]:
        for p in L["pieces"]:
            e = ids.setdefault(p["id"], dict(x=p["x"], z=p["z"], sx=p["sx"], sz=p["sz"], tops={}, stair={}))
            e["tops"][L["name"]] = p["top"]
            if p.get("stairDown"):
                e["stair"][L["name"]] = dict(down=p["stairDown"], drop=p["top"] - p.get("low", p["top"]))
    table = {}
    name = lambda f: os.path.splitext(f)[0] if f else None
    for bid, e in ids.items():
        b = by_id.get(bid)
        model = name(b["glb"]) if b else None
        ext = name(b.get("extension")) if b else None
        if model not in CAT:
            model = None
        yaw = 0.0
        if model:
            lo, hi = CAT[model]["lo"], CAT[model]["hi"]
            # overhang beyond the footprint on each glTF side: +x, -x, +z (Blender -y), -z
            hx, hz = e["sx"] / 2.0, e["sz"] / 2.0
            best = None
            for cand in ((0.0, 90.0, 180.0, 270.0) if e["sx"] == e["sz"] else (0.0, 180.0)):
                hx_, hz_ = (hx, hz) if cand in (0.0, 180.0) else (hz, hx)
                over = {"+x": hi[0] - hx_, "-x": -lo[0] - hx_, "+z": -lo[1] - hz_, "-z": hi[1] - hz_}
                score = 0.0
                for side, amount in over.items():
                    v = {"+x": (-1.0, 0.0), "-x": (1.0, 0.0), "+z": (0.0, 1.0), "-z": (0.0, -1.0)}[side]          # at yaw 0, in (east, north)
                    c, s = math.cos(math.radians(cand)), math.sin(math.radians(cand))
                    w = (v[0] * c + v[1] * s, -v[0] * s + v[1] * c)                                               # turned clockwise by the yaw
                    score += amount * (w[0] * np.sign(e["x"]) + w[1] * np.sign(e["z"]))
                if bid == "core":
                    score = 1.0 if cand == 180.0 else 0.0                                                         # its face to the gate
                if best is None or score > best[0] + 1e-6:
                    best = (score, cand)
            yaw = best[1]
        table[bid] = dict(e, model=model, yaw=yaw, extension=ext if ext in CAT else None)
    stairs = {}
    for bid, v in (late.get("stairs") or {}).items():
        if isinstance(v, dict) and name(v.get("glb")) in CAT:
            stairs[bid] = dict(model=name(v["glb"]), down=v["down"], drop=v["drop"], yaw=180.0)                    # glTF +x lands west at yaw 0: a half turn puts the ramp right
    movers = {k: name(v) for k, v in (late.get("movers") or {}).items() if k != "note" and name(v) in CAT}
    pads = {k: name(v) for k, v in (late.get("pads") or {}).items() if k != "note" and name(v) in CAT}
    return lay, table, stairs, movers, pads


def stage_fires(table):
    """A fire bowl at each corner tower's outer corner, where nobody runs: it rides the block."""
    for bid, e in table.items():
        if not bid.startswith("tower"):
            continue
        sx, sy = (1 if e["x"] > 0 else -1), (1 if e["z"] > 0 else -1)
        x, y = e["x"] + sx * (e["sx"] / 2 - 0.8), e["z"] + sy * (e["sz"] / 2 - 0.8)
        put("fire_bowl", x, y, face(x, y), 0.85, family="prop", z=next(iter(e["tops"].values())), on_block=bid, note="rides the block; z is its top in the first layout that has it")


# ================================================================== 8. LIFE (the rejected pass's 60 figures, their kinds kept, re-placed on this land)
def life(houses):
    old = json.load(open(os.path.join(WORK, "old_life.json")))
    out = []
    counts = {}
    paddies = [r for r in ROWS if r["lod"] != "far" and r["shape"] != "cap"]
    flooded = [r for r in paddies if r["crop"] == "flooded"] or paddies
    growing = [r for r in paddies if r["crop"] != "flooded"] or paddies
    slot = dict(villager_stand=0, villager_sit=1, villager_watch=2, villager_carry=3, child_wave=4, chicken=2, chicken_white=6, dog=7, carabao_stand=9)
    side = dict(villager_stand=1.2, villager_sit=-1.4, villager_watch=0.0, villager_carry=2.4, child_wave=-0.6, chicken=-2.9, chicken_white=-2.3, dog=1.9, carabao_stand=6.5)
    ahead = dict(villager_stand=4.6, villager_sit=4.9, villager_watch=5.6, villager_carry=5.2, child_wave=5.9, chicken=4.4, chicken_white=4.9, dog=5.4, carabao_stand=5.0)
    for e in old:
        kind = e["kind"]
        i = counts.get(kind, 0); counts[kind] = i + 1
        row = dict(kind=kind, model=e["model"], do=e.get("do", ""))
        if kind in ("villager_plant", "egret", "carabao_wallow"):
            src = growing if kind == "villager_plant" else flooded
            r = src[(i * 7 + (3 if kind == "egret" else 0)) % len(src)]
            tx, ty = r["t"]; nx, ny = -ty, tx
            off = -3.0 + 2.0 * (i % 4)
            x, y = r["mid"][0] + nx * 1.35 + tx * off, r["mid"][1] + ny * 1.35 + ty * off
            z = r["level"] - 0.05 + STEP - (0.14 if kind != "egret" else 0.06)
            yaw = (r["yaw"] + 90 + 40 * i) % 360
        elif kind == "hawk":
            x, y, z, yaw = -40.0 + 90.0 * i, 70.0 + 30.0 * i, 34.0 + 10 * i, 120.0 * i
        else:
            h = houses[(i * 5 + slot.get(kind, 0)) % len(houses)]
            lo, hi = size(h["model"])
            fr = -lo[1] * h["scale"]
            a = side.get(kind, 0.0) + 0.5 * (i % 3)
            b = fr + ahead.get(kind, 5.0)
            if h["model"] == "house_shrine":
                b -= 6.5; a += math.copysign(6.0, a if a else 1)
            x, y = local(h, a, b)
            if not clear_of_pit(x, y, 1.2):
                x, y = local(h, a, fr + 0.9)
            z = g(x, y)
            yaw = face(x, y) if kind in ("villager_watch", "child_wave", "villager_stand") else (h["yaw"] + 70 * i) % 360
        row.update(position=[round(x, 3), round(z, 3), round(y, 3)], yaw=round(yaw, 2))
        out.append(row)
    return out


def declash():
    """Nothing small stands in a giant, in a fall's pool or on top of another prop: the place's own props win over a yard's."""
    big = [footprint(q, 0.6) for q in P if q["model"] in ("bulul_seated", "bulul_standing", "fall_great")]
    kept = [(q, 0.5) for q in P if q["family"] == "bulul" and q["model"].startswith("bulul_small") and not q.get("wall")]
    for q in sorted([q for q in P if q["family"] in ("prop", "cover") and not q.get("wall") and not q.get("on_block") and not q.get("on") == "stream bed"], key=lambda q: "owner" in q):
        lo, hi = size(q["model"])
        rad = 0.5 * max(hi[0] - lo[0], hi[1] - lo[1]) * q["scale"]
        bad = any(_inside(poly, q["x"], q["y"]) for poly in big)
        if q["family"] == "prop":
            for (o, orad) in kept:
                if abs(o["z"] - q["z"]) < 2.0 and math.hypot(o["x"] - q["x"], o["y"] - q["y"]) < (rad + orad) * 1.02:
                    bad = True; break
        if bad:
            P.remove(q)
        elif q["family"] == "prop":
            kept.append((q, rad))


# ================================================================== everything
def build():
    del P[:]; del ROWS[:]; del KEEP[:]; del BOXES[:]; del BRIDGES[:]
    gate()
    pit()
    guardians()
    houses = town()
    for h in houses:
        BOXES.append(footprint(h, 1.2))
    for p in P:
        if p["family"] == "gate":
            BOXES.append(footprint(p, 1.0))
    for (px, py, R, B, level, s) in PADS:
        KEEP.append((px, py, R + 2.0))
    for (hx, hy, hr, hb, hl) in HPADS:
        KEEP.append((hx, hy, hr + hb + 1.0))
    yards(houses)
    props()
    declash()
    solids = [footprint(q, 0.3) for q in P if q["family"] in ("house", "gate") or q["model"] in ("bulul_seated", "bulul_standing")]
    for q in [q for q in P if q["family"] == "path"]:                 # a path stone never runs under a building or a giant
        bx, by = front(q["yaw"])
        if any(_inside(poly, q["x"] - bx * t, q["y"] - by * t) for poly in solids for t in (0.5, 3.0, 5.5)) and q["x"] != 0.0:
            P.remove(q)
    water()
    declash()
    terraces()
    terrace_rows()
    terrace_dressing()
    trees()
    lay, table, stairs, movers, pads = stage()
    stage_fires(table)
    return dict(placements=P, rows=ROWS, houses=houses, layout=lay, blocks=table, stairs=stairs, movers=movers, pads=pads, life=life(houses))


if __name__ == "__main__":
    out = build()
    fam = {}
    tri = {}
    for p in P:
        fam[p["family"]] = fam.get(p["family"], 0) + 1
        tri[p["family"]] = tri.get(p["family"], 0) + CAT[p["model"]]["tris"]
    print("placements by family:", fam)
    print("triangles by family:", tri, "total", sum(tri.values()))
    lods = {}
    for r in ROWS:
        lods[r["lod"]] = lods.get(r["lod"], 0) + 1
    print("terrace modules:", lods, "runs", len({r["run"] for r in ROWS}))
    for f in FIELDS:
        print(" field", f.name, "core bearings %.0f..%.0f" % tuple(f.core), "rows", len(f.rows()))
    print("blocks:", {k: (v["model"], v["yaw"]) for k, v in out["blocks"].items()})
    print("stairs", out["stairs"], "movers", out["movers"], "pads", out["pads"])
    os.makedirs(WORK, exist_ok=True)
    json.dump(dict(placements=P, rows=[dict(r, end=list(r["end"]), mid=list(r["mid"]), t=list(r["t"])) for r in ROWS], life=out["life"]),
              open(os.path.join(WORK, "placements.json"), "w"))
