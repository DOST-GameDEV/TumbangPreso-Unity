"""PAETE'S RUN, the art's WORLD: the gameplay layout read as data, and the ground the massif is shaped from (2026-10-10).

Imported by tools/author_paete_run_art.py. ⚠️ THE GAMEPLAY LAYOUT IS THE TRUTH (Assets/TumbangPreso/Art/PaeteRun/
paete_run_layout.json, written by tools/author_paete_run_layout.py, which this file never touches): its solids are what
a body stands on, its movers are what moves, its water is where a body swims. The art is DRAWN FROM those, solid by
solid, so a top that holds a body up looks like it does. The course file gives the shortcuts' lines; the layout gives
every way point (it moved seven of the course's).

What this file decides is the height of the rock at every point, so that:
  * every flat solid (a deck, a landing, a causeway, a plaza) has rock coming up under it where the rock can;
  * a stage of hops or a swing has a chasm under it; a pool sits in a basin; a chute runs in a groove with banks;
  * ⚠️ NOTHING OF THE ROCK STANDS IN A LANE OR OVER A SOLID. The first grey blockouts put rock under every leg and it
    hid the course. Last of all the ground is CAPPED: under every leg's corridor (to well under its jump or swing arc)
    and under every solid and mover (below its top). Where two legs cross at different heights the lower one therefore
    has clear air, and the upper one is carried by masonry. `blocked()` answers the same for a pier or a boulder.

Blender axes: metres, z up. Unity (x, y, z) is Blender (-x + START.x, -z + START.y, y): the start line is Unity's origin.
"""
import json
import math
import sys
from pathlib import Path

import numpy as np
from mathutils import Matrix, Vector, noise

sys.path.insert(0, str(Path(__file__).resolve().parent))
from paete_run_course import COURSE, SHORTCUTS, START      # noqa: E402
import paete_run_stages as stages                          # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
LAYOUT_PATH = ROOT / "Assets/TumbangPreso/Art/PaeteRun/paete_run_layout.json"
LAYOUT = json.loads(LAYOUT_PATH.read_text(encoding="utf-8"))
UP = Vector((0, 0, 1))
OFF = Vector(START)
CELL = 2.0
X0, X1, Y0, Y1 = -176.0, 204.0, -126.0, 276.0
CENTRE = (14.0, 76.0)
BOWL = -13.0                 # the dry bowl's floor
PLAZAS = [(36.0, 204.0, 46.0, 9.0)]      # x, y, floor height, radius: the summit's ruin stands on one

MODE = {
    "plain": ("terrace", 4.0), "pendulums": ("terrace", 1.5),
    "chained_boulders": ("gap", 6.0), "three_beams": ("gap", 6.0), "gapped_bridge": ("gap", 8.0), "colonnade": ("gap", 7.0),
    "mill_wheels": ("gap", 3.5), "rolling_logs": ("gap", 5.0), "tilting_slabs": ("gap", 5.0), "stair_blocks": ("gap", 3.0),
    "wall_ledges": ("gap", 6.0), "floating_rocks": ("pool", 7.0), "crumble": ("gap", 6.0), "drum_stones": ("gap", 4.0),
    "geyser": ("gap", 5.0), "arches": ("gap", 9.0), "long_swing": ("gap", 11.0), "chain_swing": ("gap", 9.0),
    "dive": ("gap", 6.0), "swim": ("pool", 13.0), "chute": ("chute", 0.0),
    "cut_swing": ("gap", 8.0), "cut_hops": ("gap", 4.0),
}
DIP = {"arches": 5.0, "chain_swing": 5.0, "long_swing": 7.5, "cut_swing": 5.5}


def B(u):
    """A Unity point of the layout, in Blender axes."""
    return Vector((-u["x"] + OFF.x, -u["z"] + OFF.y, u["y"] + OFF.z))


def Bdir(u):
    return Vector((-u["x"], -u["z"], u["y"]))


def side_of(a, c):
    d = Vector(c) - Vector(a); d.z = 0
    d = d.normalized() if d.length > 1e-4 else Vector((0, 1, 0))
    return d, Vector((d.y, -d.x, 0))


def owner_of(tag):
    if tag.isdigit():
        return int(tag)
    if tag.startswith("wp") and tag[2:].isdigit():
        k = int(tag[2:])
        return (k, k + 1)
    if tag.startswith("s") and "." in tag:
        a, b = tag[1:].split(".")
        return (100 + int(a) * 2 + int(b),)
    return None


class Sol:
    """A solid (or a mover) of the layout, in Blender axes. Local x runs ALONG it (Unity z), y to its left, z up."""

    def __init__(self, d):
        self.d = d
        self.name, self.kind, self.tag, self.shape = d["name"], d["kind"], str(d.get("leg", "")), d["shape"]
        self.c = B(d["c"])
        s = d["s"]
        self.sx, self.sy, self.sz = s["x"], s["y"], s["z"]           # across, up, along
        self.r, self.h = d.get("r", 0.0), d.get("h", 0.0)
        yaw, pitch = math.radians(d.get("yaw", 0.0)), math.radians(d.get("pitch", 0.0) or 0.0)
        fu = (math.sin(yaw) * math.cos(pitch), -math.sin(pitch), math.cos(yaw) * math.cos(pitch))
        uu = (math.sin(yaw) * math.sin(pitch), math.cos(pitch), math.cos(yaw) * math.sin(pitch))
        self.fwd = Vector((-fu[0], -fu[2], fu[1])); self.up = Vector((-uu[0], -uu[2], uu[1]))
        self.left = self.up.cross(self.fwd).normalized()
        self.flat = Vector((self.fwd.x, self.fwd.y, 0)).normalized() if abs(self.fwd.z) < 0.99 else Vector((1, 0, 0))
        self.side = Vector((self.flat.y, -self.flat.x, 0))          # to its right, level
        self.M = Matrix((self.fwd, self.left, self.up)).transposed().to_4x4()
        self.M.translation = self.c
        self.pitched = abs(pitch) > 1e-3
        hh = self.h if self.shape in ("cyl", "capsule") and self.h else self.sy
        if self.shape == "sphere":
            hh = 2 * self.r
        self.top, self.bot = self.c.z + hh / 2, self.c.z - hh / 2
        self.rad = self.r if self.shape in ("cyl", "sphere") else math.hypot(self.sx, self.sz) / 2
        self.pivot = B(d["pivot"]) if "pivot" in d else None
        self.axis = Bdir(d["axis"]) if "axis" in d else None
        self.yaw = math.atan2(self.flat.y, self.flat.x)
        self.own = owner_of(self.tag)

    def reach(self, x, y):
        """How far (x, y) is outside its footprint: 0 inside."""
        dx, dy = x - self.c.x, y - self.c.y
        if self.shape != "box":
            return max(0.0, math.hypot(dx, dy) - self.r)
        al, ac = dx * self.flat.x + dy * self.flat.y, dx * self.side.x + dy * self.side.y
        return math.hypot(max(abs(al) - self.sz / 2, 0.0), max(abs(ac) - self.sx / 2, 0.0))

    def grid(self):
        dx, dy = GX - self.c.x, GY - self.c.y
        if self.shape != "box":
            return np.maximum(np.hypot(dx, dy) - self.r, 0.0)
        al, ac = dx * self.flat.x + dy * self.flat.y, dx * self.side.x + dy * self.side.y
        return np.hypot(np.maximum(np.abs(al) - self.sz / 2, 0.0), np.maximum(np.abs(ac) - self.sx / 2, 0.0))

    def at(self, along, across=0.0, z=None):
        """A point on it: `along` metres forward of its middle, `across` metres to its RIGHT, at its top unless z is given."""
        p = self.c + self.flat * along + self.side * across
        p.z = self.top if z is None else z
        return p


class Leg:
    def __init__(self, i, kind, a, c, name, cut=None, pts=None):
        self.i, self.kind, self.name, self.cut = i, kind, name, cut
        self.A, self.C = Vector(a), Vector(c)
        self.f, self.s = side_of(a, c)
        self.ln = (self.C - self.A).length
        self.flat = math.hypot(self.C.x - self.A.x, self.C.y - self.A.y)
        self.pts = pts or [self.A, self.C]
        self.mode, self.num = MODE[name]
        self.half = self.num if self.mode == "terrace" else (4.5 if self.mode == "pool" else 3.4 if self.mode == "chute" else 4.0)
        if cut:
            self.half = 2.0

    def at(self, t):
        return self.A.lerp(self.C, t)


LEGS = []
_chute = {}
for _c in LAYOUT["chutes"]:
    _p = [B(q) for q in _c["points"]]
    _chute[int(_c["leg"])] = [_p[0]] + [q for k, q in enumerate(_p[1:], 1) if (q - _p[k - 1]).length > 0.01]
for _l in LAYOUT["legs"]:
    _n = _l["stage"]
    if _n == "plain":
        _n = {"run": "plain", "swim": "swim", "dive": "dive", "chute": "chute"}.get(_l["kind"], "plain")
    LEGS.append(Leg(_l["n"], _l["kind"], B(_l["a"]), B(_l["c"]), _n, pts=_chute.get(_l["n"])))
CUTS = []
for (_nm, _ask, _after, _before, _legs) in SHORTCUTS:
    for (_k, _a, _c2) in _legs:
        CUTS.append(Leg(100 + len(CUTS), _k, _a, _c2, "cut_swing" if _k == "swing" else "cut_hops", cut=_nm))
ALL = LEGS + CUTS


def leg_of(i):
    if isinstance(i, (tuple, list)):
        i = i[0]
    return LEGS[i - 1] if i < 100 else CUTS[i - 100]


# ------------------------------------------------------------------ the grid
NX, NY = int((X1 - X0) / CELL) + 1, int((Y1 - Y0) / CELL) + 1
GX, GY = np.meshgrid(np.linspace(X0, X1, NX), np.linspace(Y0, Y1, NY), indexing="ij")
SOLIDS = [Sol(d) for d in LAYOUT["solids"]]
MOVERS = [Sol(d) for d in LAYOUT["movers"]]
WATERS = [Sol(dict(d, kind="water", shape="box")) for d in LAYOUT["water"]]
BY = {s.name: s for s in SOLIDS}
FLAT = ("deck", "lead", "causeway", "start_plaza", "finish_platform", "landing", "shortcut_node")


def is_flat(s):
    return s.name.startswith(FLAT) or (s.name.startswith("ledge_") and s.name.count("_") == 1)


def sstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def _noise(scale, seed):
    out = np.empty(GX.shape)
    for i in range(NX):
        for j in range(NY):
            out[i, j] = noise.noise(Vector((GX[i, j] * scale + seed, GY[i, j] * scale - seed * 0.7, seed * 0.31)))
    return out


def bowl_height(x, y):
    """The dry bowl without the massif: dunes on a floor that climbs to a rim. The horizon mesh uses the same."""
    r = math.hypot((x - CENTRE[0]) / 175.0, (y - CENTRE[1]) / 190.0)
    t = min(1.0, max(0.0, (r - 0.72) / 0.5)); t = t * t * (3 - 2 * t)
    dune = 1.3 * noise.noise(Vector((x * 0.035 + 3.1, y * 0.035 - 2.17, 0.961))) + 0.5 * noise.noise(Vector((x * 0.09 + 7.7, y * 0.09 - 5.39, 2.387)))
    return BOWL + dune + 15.0 * t


def rect(leg, a0=None, a1=None):
    a0 = 0.0 if a0 is None else a0
    a1 = leg.flat if a1 is None else a1
    dx, dy = GX - leg.A.x, GY - leg.A.y
    al = dx * leg.f.x + dy * leg.f.y
    lat = np.abs(dx * leg.s.x + dy * leg.s.y)
    zl = leg.A.z + (leg.C.z - leg.A.z) * np.clip(al / max(leg.flat, 1e-6), 0, 1)
    over = np.maximum(np.maximum(a0 - al, al - a1), 0.0)
    return al, lat, zl, over


def poly(pts):
    best = np.full(GX.shape, 1e9); zl = np.zeros(GX.shape)
    for p, q in zip(pts, pts[1:]):
        ex, ey = q.x - p.x, q.y - p.y
        l2 = ex * ex + ey * ey
        t = np.clip(((GX - p.x) * ex + (GY - p.y) * ey) / l2, 0, 1)
        d = np.hypot(GX - (p.x + ex * t), GY - (p.y + ey * t))
        m = d < best
        best = np.where(m, d, best); zl = np.where(m, p.z + (q.z - p.z) * t, zl)
    return best, zl


def build_ground():
    n1, n2 = _noise(0.035, 3.1), _noise(0.09, 7.7)
    r = np.hypot((GX - CENTRE[0]) / 175.0, (GY - CENTRE[1]) / 190.0)
    floor = BOWL + 1.3 * n1 + 0.5 * n2 + 15.0 * sstep(0.72, 1.22, r)

    # 1. The massif's body: the course's own heights, spread and lowered, so the rock follows the course like a mould.
    sx, sy, sz = [], [], []
    for leg in LEGS:
        drop = 3.0 if leg.mode == "terrace" else (leg.num + 1.0 if leg.mode == "gap" else 4.5)
        n = max(1, int(leg.ln / 3))
        for p in (leg.pts if leg.kind == "chute" else [leg.at(k / n) for k in range(n + 1)]):
            sx.append(p.x); sy.append(p.y); sz.append(p.z - drop)
    sx, sy, sz = np.array(sx), np.array(sy), np.array(sz)
    num = np.zeros(GX.shape); den = np.zeros(GX.shape); near = np.full(GX.shape, 1e9)
    for k in range(len(sx)):
        d2 = (GX - sx[k]) ** 2 + (GY - sy[k]) ** 2
        w = np.exp(-d2 / (2 * 12.0 ** 2)) + 1e-9 * np.exp(-d2 / (2 * 60.0 ** 2))
        num += w * sz[k]; den += w
        near = np.minimum(near, d2)
    near = np.sqrt(near)
    body = num / den
    q = body / 4.0                                                 # quarry benches: 4 m tiers, risers steep, treads near level
    bench = (np.floor(q) + sstep(0.62, 0.98, q - np.floor(q))) * 4.0
    body = body * 0.4 + bench * 0.6 + 0.9 * _noise(0.05, 11.3)
    env = 1.0 - sstep(13.0, 44.0, near + 9.0 * n1)
    H = np.maximum(floor + np.maximum(body - floor, 0.0) * env, floor)

    def set_to(H, target, d, d0, d1, lower_only=False):
        s = sstep(d0, d1, d)
        new = target * (1 - s) + H * s
        return np.where(new < H, new, H) if lower_only else new

    # 2. chasms under the hop and swing stages
    for leg in ALL:
        if leg.mode == "gap" and not (leg.cut and leg.kind == "swing"):
            al, lat, zl, over = rect(leg)
            H = set_to(H, zl - leg.num, np.hypot(lat, over), 4.5, 12.0, lower_only=True)
    # 3. terraces: rock comes up under every flat solid
    path = np.full(GX.shape, -9.0)
    for s in SOLIDS:
        tgt = None
        if is_flat(s):
            tgt = s.top - 0.35
        elif s.name.startswith(("giant_step", "shelf_", "buttress_")):
            tgt = s.top - 1.2
        elif s.name.startswith("tier_"):
            tgt = s.top - 1.5
        if tgt is None:
            continue
        d = s.grid()
        H = set_to(H, tgt, np.maximum(d - 0.7, 0.0), 0.0, 6.0)
        if is_flat(s):
            path = np.maximum(path, 2.4 - d)
    for (px, py, pz, pr) in PLAZAS:
        d = np.maximum(np.hypot(GX - px, GY - py) - pr, 0.0)
        H = set_to(H, pz - 0.35, d, 0.0, 6.0)
        path = np.maximum(path, 1.0 - d)
    # 4. the cliff the wall ledges step down: rock behind the wall
    for s in SOLIDS:
        if s.name.startswith("cliff_"):
            leg = leg_of(s.own)
            away = 1.0 if (s.c - leg.at(0.5)).dot(s.side) > 0 else -1.0
            dx, dy = GX - s.c.x, GY - s.c.y
            al = dx * s.flat.x + dy * s.flat.y
            sl = (dx * s.side.x + dy * s.side.y) * away
            over = np.maximum(np.abs(al) - s.sz / 2 - 2.0, 0.0)
            w = sstep(-0.2, 1.2, sl) * (1 - sstep(10.0, 22.0, sl)) * (1 - sstep(0.0, 8.0, over))
            zt = leg.A.z + (leg.C.z - leg.A.z) * np.clip((al + s.sz / 2) / s.sz, 0, 1) + 7.5
            H = np.maximum(H, np.minimum(zt, s.top - 0.4) * w + H * (1 - w))
    # 5. basins: the pools are cisterns, straight-sided
    wet = np.full(GX.shape, -2.0)
    for wv in WATERS:
        d = wv.grid()
        surf, fl = wv.d["surface"], wv.d["floor"]
        # ⚠️ every grid vertex within a cell's diagonal of the water is on the basin's floor: the bank begins beyond it
        # (a bank that began at the water's edge sent slivers of sand up through the pool)
        H = np.where(d < 2.9, fl - 0.5, np.maximum(H, surf + 0.45 - np.maximum(d - 4.2, 0.0) * 0.8))
        wet = np.where(d < 2.9, np.maximum(wet, surf - fl + 0.5), wet)
    # 6. grooves for the chutes
    for leg in LEGS:
        if leg.mode == "chute":
            d, zc = poly(leg.pts)
            inner = (zc - 1.3) + 1.6 * sstep(4.2, 5.0, d)
            H = np.where(d < 5.0, inner, np.clip(H, zc + 0.3 - np.maximum(d - 6.0, 0.0) * 0.9, zc + 0.3 + np.maximum(d - 5.0, 0.0) * 1.3))
    # 7. ⚠️ THE CAPS, LAST: no leg has rock in its lane or under its arc, and no solid or mover has rock over its top.
    for leg in ALL:
        if leg.mode != "gap":
            continue
        trim = min(7.0, leg.flat * 0.25)
        al, lat, zl, over = rect(leg, trim, leg.flat - trim)
        cap = zl - max(2.5, DIP.get(leg.name, 0.0) + 2.0)
        s = sstep(0.0, 3.5, np.hypot(np.maximum(lat - 5.0, 0.0), over))
        H = np.where(cap < H, cap * (1 - s) + H * s, H)
    for s in SOLIDS + MOVERS:
        if s.name.startswith("pool_"):
            continue
        d = s.grid() if s.shape == "box" else np.maximum(np.hypot(GX - s.c.x, GY - s.c.y) - max(s.rad, 0.5), 0.0)
        low = s.top - 0.3 if s in SOLIDS else s.bot - 1.2
        if s.pitched:
            low = s.bot - 1.0
        # every grid vertex within a cell's diagonal of a walked solid is under it (owner: a sand sheet flickered through a deck)
        sk = sstep(0.4, 2.4, d) if s.kind == "post" else sstep(0.4, 3.0, d) if (s.pitched or s.kind == "teal") else sstep(3.0, 5.2, d)
        H = np.where(low < H, low * (1 - sk) + H * sk, H)
    for wv in WATERS:
        d = wv.grid()
        H = np.where(d < 2.9, wv.d["floor"] - 0.5, H)
    for leg in LEGS:
        if leg.mode == "chute":
            d, zc = poly(leg.pts)
            H = np.where(d < 4.2, np.minimum(H, zc - 1.3), np.where(d < 9.0, np.minimum(H, zc + 0.3 + np.maximum(d - 5.0, 0.0) * 1.3), H))
    gx, gy = np.gradient(H, CELL)
    slope = np.hypot(gx, gy)
    grass = (_noise(0.045, 21.0) - 0.34) * 9.0 - np.maximum(slope - 0.35, 0) * 8.0 - 6.0 * (1.0 - env) - 3.0 * sstep(BOWL + 5.0, BOWL + 1.5, H)
    fields = dict(court=np.clip(path, -4, 3), grass=np.clip(grass, -4, 3), sand=np.clip((0.62 - slope) * 3.0, -3, 3),
                  wet=np.clip(wet, -2, 4), steep=np.clip((slope - 0.72) * 3.0, -3, 3), ring=np.full(GX.shape, -1.0))
    return H, slope, fields


H = SLOPE = FIELDS = None


def make():
    global H, SLOPE, FIELDS
    H, SLOPE, FIELDS = build_ground()


def _bil(A, x, y):
    fx = min(max((x - X0) / CELL, 0.0), NX - 1.001); fy = min(max((y - Y0) / CELL, 0.0), NY - 1.001)
    i, j = int(fx), int(fy)
    u, v = fx - i, fy - j
    return A[i, j] * (1 - u) * (1 - v) + A[i + 1, j] * u * (1 - v) + A[i, j + 1] * (1 - u) * v + A[i + 1, j + 1] * u * v


def ground(x, y):
    return float(_bil(H, x, y))


def slope(x, y):
    return float(_bil(SLOPE, x, y))


def ground_min(x, y, r):
    return min(ground(x + dx * r, y + dy * r) for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (0.7, 0.7), (-0.7, 0.7), (0.7, -0.7), (-0.7, -0.7)))


# ------------------------------------------------------------------ what a pier or a boulder may not stand in
_SAMP = []      # x, y, z, half, below, above, leg index, is pool
for _leg in ALL:
    _n = max(2, int(_leg.ln / 1.5))
    _pts = [_leg.at(k / _n) for k in range(_n + 1)]
    if _leg.kind == "chute":
        _pts = [p.lerp(q, t) for p, q in zip(_leg.pts, _leg.pts[1:]) for t in (0.0, 0.33, 0.66)] + [_leg.pts[-1]]
    for _p in _pts:
        _below = {"terrace": 0.4, "pool": 3.0, "chute": 0.6}.get(_leg.mode, DIP.get(_leg.name, 1.0) + 1.0)
        _SAMP.append((_p.x, _p.y, _p.z, _leg.half, _below, 4.5, _leg.i, 1.0 if _leg.mode == "pool" else 0.0))
SAMP = np.array(_SAMP, dtype=float)


def _own(owner):
    if owner is None:
        return ()
    if isinstance(owner, (list, tuple, set)):
        return tuple(owner)
    return (owner - 1, owner, owner + 1) if owner < 100 else (owner,)


def blocked(x, y, z0, z1, r=0.6, owner=None, margin=0.6, wade=False, skip=()):
    """True if a thing of radius r standing from z0 to z1 at (x, y) is in a leg's corridor, in a pool's water, or runs
    through one of the layout's solids or movers. `owner`: the leg(s) it belongs to (an int also clears the leg before
    and after it). `wade`: it may stand in a pool. `skip`: names of solids it is part of."""
    d = np.hypot(SAMP[:, 0] - x, SAMP[:, 1] - y)
    reach = SAMP[:, 3] + r + margin
    if wade:
        reach = np.where(SAMP[:, 7] > 0.5, -1.0, reach)
    m = (d < reach) & (z1 > SAMP[:, 2] - SAMP[:, 4]) & (z0 < SAMP[:, 2] + SAMP[:, 5])
    for o in _own(owner):
        m &= SAMP[:, 6] != o
    if m.any():
        return True
    for s in SOLIDS + MOVERS:
        if s.name in skip or s.name.startswith("pool_"):
            continue
        if z1 > s.bot + 0.05 and z0 < s.top + 2.3 and s.reach(x, y) < r + 0.25:
            return True
    if not wade:
        for wv in WATERS:
            if z0 < wv.d["surface"] and wv.reach(x, y) < r + 0.3:
                return True
    return False


def find_clear(p, z0, z1, r, owner, dists=(5.0, 6.5, 8.0, 10.0, 12.0, 14.0), prefer=None, n=16, skip=()):
    """The nearest spot round p where a thing of radius r from the ground up to z1 stands in nobody's lane. None if none."""
    best = None
    for dist in dists:
        for k in range(n):
            a = math.tau * k / n
            q = Vector((p[0] + math.cos(a) * dist, p[1] + math.sin(a) * dist, 0))
            if blocked(q.x, q.y, ground_min(q.x, q.y, r), z1, r, owner, skip=skip):
                continue
            score = dist - (1.5 * (Vector((math.cos(a), math.sin(a), 0)).dot(prefer)) if prefer is not None else 0.0)
            if best is None or score < best[0]:
                best = (score, q, dist)
        if best is not None:
            return best[1], best[2]
    return None


def near_legs(x, y, reach):
    d = np.hypot(SAMP[:, 0] - x, SAMP[:, 1] - y)
    m = d < reach
    return SAMP[m], d[m]
