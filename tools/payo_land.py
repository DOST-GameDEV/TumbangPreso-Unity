"""PAYO's land, built CLEAN: the valley of tools/payo_valley_blockout.py (v15, the shape the owner steered) with terraces
whose every edge follows the ground's own contour line, not the grid.

Owner, 2026-10-17: "continue developing the map. i dont want any jagged terraces. be creative in the design of the map,
it should mix a blend of artistic culture while being serene in its view. be critical of you're work, if it looks to
messy or doesnt look good at all, revise it".

WHY THE BLOCKOUT'S TERRACES WERE JAGGED: it stepped the height at each point of a 3 m grid, so a terrace wall, which is
narrower than a grid cell on most slopes, jumped from cell to cell and came out as a saw. Here every triangle of the grid
is CUT along the iso-lines where the walls begin and end (and where the paddies' bunds and the plots' dikes run), and
each cut piece is flat, a wall or a bund as a whole. So every edge a player sees lies on a smooth contour line.

  py -3 tools/payo_land.py [tag]                 builds the land, checks the skyline, writes Logs/payo/terrain/land_<tag>.npz
  blender -b --python tools/payo_land_blend.py -- [tag]     makes the .blend and the pictures from that file

Metres, x east, y north, z up (Blender's axes). The court's top is at z 0, centred on the origin, facing north.
"""
import math
import os
import sys
import time

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Logs", "payo", "terrain")

# The grid is laid so that x = +-30 and y = +-30 (the pit's edges) are grid lines.
HALF, CELL = 1149.0, 3.0
Y0, Y1 = -999.0, 2802.0
NX, NY = int(round(2 * HALF / CELL)) + 1, int(round((Y1 - Y0) / CELL)) + 1
HEAD_Y = -70.0
NOSE_X, TURN_Y, TURN = 30.0, 800.0, 0.9
R_NEAR, R_FAR, FALL_A, FALL_B = 205.0, 112.0, 1000.0, 1900.0

STEP = 5.0            # a terrace's rise ("make the terraces taller")
RISER = 0.80          # how much of the ground's own rise, in metres, one wall takes: walls are steep, paddies wide
BUND = 0.40           # the grass bund along a paddy's outer edge, in metres of the ground's rise
DIKE = 0.9            # half the width of the dike between two plots of one terrace, in metres

# Colours, painted calm: mostly greens, gold where the rice is ripe, a few flooded paddies holding the sky.
PADDY = [((0.56, 0.80, 0.30), 5), ((0.46, 0.74, 0.27), 5), ((0.36, 0.64, 0.25), 3), ((0.70, 0.84, 0.38), 3),
         ((0.88, 0.78, 0.38), 3), ((0.82, 0.70, 0.32), 2), ((0.70, 0.79, 0.85), 1)]
WALL = (0.40, 0.55, 0.31)
BUND_C = (0.31, 0.52, 0.23)
GRASS, FOREST, ROCK = (0.42, 0.63, 0.27), (0.15, 0.38, 0.22), (0.60, 0.57, 0.52)
MOUNT_ROCK = (0.36, 0.40, 0.33)
HEAD_ROCK = (0.44, 0.47, 0.40)
BASIN_STONE = (0.56, 0.57, 0.52)
# THE SACRED POOL: the court's water spills out of the open front of the pit and falls into a round pool at the foot of
# the scarp; the river is born there and runs down the valley. (x, y, flat bottom radius, bank radius, bottom, water)
POOL = (0.0, 60.0, 16.0, 26.0, -48.0, -45.5)
# THE HEAD OF THE VALLEY, behind the gate, on the court's own axis: the GREAT FALL drops off a cliff into a carved stone
# BASIN, and a straight stone WATER STAIR runs from the basin down through the terraces to the gate, falling a little at
# each terrace. So looking south from the court a player sees the gate framed by the stair of water and the fall behind
# it; looking north, the same water leaving the court for the valley. Every edge here is on a grid line (x = -1149 + 3i,
# y = -999 + 3j), so it is straight and clean.
# The head: the great fall drops off a real cliff, a horseshoe of rock round the south side of a ROUND stone plunge pool
# (owner, 2026-10-18: "the basin fall does NOT look blended in seamlessly": the fall stood half way up a smooth slope and
# the basin was a rectangle cut into it). The pool's floor, its bowl and the cliff's face are cut along circles round its
# middle, so every edge is clean.
HEAD_C = (0.0, -208.0)          # the plunge pool's middle
B_RIN, B_ROUT = 14.0, 20.0       # its flat floor, and its stone rim
B_FACE = 0.8                     # the cliff's face: from the rim to the cliff top over this much ground
CLIFF_TOP = 74.0                 # the lip the fall leaves (the hill behind is this high or higher)
CLIFF_REACH = 30.0               # how far behind the face the ground is held up to the lip's height
BASIN = (-20.0, 20.0, -228.0, -188.0, 42.0, 45.0)     # its bounds, floor, water (kept as a box for the dressing)
CLIFF = (-36.0, 36.0, -264.0, -225.0, 100.0)          # x0, x1, y0, y1, the plateau the fall leaves
STAIR = (3.0, 6.0, -189.0, -63.0, 1.8)                # flat bottom half-width, bank half-width, y0, y1, depth
STONE = (0.62, 0.61, 0.58)
PAD_EDGE = 0.5                     # half the gap between two level tiers: the stone wall between them
PAD_TOP, PAD_WALL = (0.70, 0.66, 0.54), (0.50, 0.53, 0.44)   # the platform's earth walls, where they hold back the hill: mossy
PLAT_TOP = (0.72, 0.69, 0.60)
TOWN_WALL = (0.43, 0.56, 0.33)
GARDEN = ((0.50, 0.72, 0.30), (0.44, 0.66, 0.28))

# The court stands on a stone PLATFORM (owner, 2026-10-17: "no organization/cleanliness in the layout"): a level
# rectangle round the pit at the rim's height, with straight stone walls down to the terraces, split at the front by the
# gorge the fall drops through into the sacred pool. x half-width, south edge, north edge; its walls take 3 m.
PLAT = (42.8, -62.0, 54.8)        # 1.2 m inside the stone courses tools/payo_dress.py stands on its edges
PLAT_SCALE = 2.0
_pg = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Logs", "payo", "pieces", "calm", "court", "pool_height_grid.npz")
POOL_GRID = np.load(_pg) if os.path.exists(_pg) else None
SHRINE_PAD = (0.0, -175.0, 0.0, 46.0)      # (none: the shrine moved to the cliff top above the fall)
RIM_Z = -6.35                  # the land under the court piece's paving (z -6.0) sits just below it

# The town is TERRACED as a whole, like the valley: level tiers with straight stone walls between them, so nothing in it
# is a lumpy bank. Every third tier is a street the houses stand along; the two between are gardens.
STREET_Z = (13.2, 35.6, 58.0)     # the town's streets: level terraces at these heights round the valley's head
STREET_HALF = 5.0                      # each is level where the ground was within this of its height
GARDEN_HALF = 2.8
TOWN_GAP = 0.4                         # each wall takes this much of the ground's own rise: steep and narrow
TOWN_LEVELS = (-1.0, 5.0, 13.2, 21.4, 27.4, 35.6, 43.8, 49.8, 58.0, 66.2, 72.2)   # streets 10 m of rise deep, so the long houses fit
STREET_SPACING = 21.0                  # houses along a street, centre to centre (owner: 'too sparse')


# The head is now ONE authored piece (tools/payo_pieces/head_build.py: cliff, lip, fall, plunge pool, kerb, outlet),
# placed by tools/payo_dress.py; the land only steps out of its way: under the pool and kerb, and low to its north.
HEAD_PIECE = True


def head_cliff(X, Y):
    """How much the ground here is held up to the cliff top: the horseshoe behind the plunge pool, strongest due south,
    tapering to nothing at its two ends and toward the hill behind. (Not used with the authored head piece.)"""
    if HEAD_PIECE:
        return np.zeros_like(np.asarray(X, np.float64))
    dx, dy = X - HEAD_C[0], Y - HEAD_C[1]
    d = np.hypot(dx, dy)
    south = -dy / np.maximum(d, 1e-6)
    return smooth(0.10, 0.55, south) * smooth(B_ROUT + CLIFF_REACH, B_ROUT + CLIFF_REACH - 14.0, d) * (d > B_ROUT)


def town_weight(X, Y):
    """How much of the town is here: 1 inside, easing to 0 at its edges so the tiers shrink away instead of stopping."""
    R = np.hypot(X, Y)
    return (smooth(46, 52, R) * smooth(235, 228, R) * smooth(6, 1, Y)
            * (1 - smooth(29.5, 27, np.abs(X)) * smooth(-17, -21, Y))
            * smooth(B_ROUT + 26.0, B_ROUT + 38.0, np.hypot(X - HEAD_C[0], Y - HEAD_C[1])))


def town_breaks():
    """The town's tier profile as (ground height, tier height) points: a level tier, a 1 m (of the slope's rise) wall
    up to the next, and at the bottom and top a wall back to the natural ground."""
    xp, fp = [], []
    lv = list(TOWN_LEVELS)
    hv = [STREET_HALF if l in STREET_Z else GARDEN_HALF for l in lv]
    xp.append(lv[0] - hv[0] - TOWN_GAP); fp.append(lv[0] - hv[0] - TOWN_GAP)
    for l, hh in zip(lv, hv):
        xp += [l - hh, l + hh]; fp += [l, l]
    xp.append(lv[-1] + hv[-1] + TOWN_GAP); fp.append(lv[-1] + hv[-1] + TOWN_GAP)
    return np.array(xp), np.array(fp)


TOWN_XP, TOWN_FP = None, None


def town_step(z):
    xp, fp = town_breaks()
    return np.where((z < xp[0]) | (z > xp[-1]), z, np.interp(z, xp, fp))


def town_level(z):
    """The level of the flat tier the ground height z lies on, or None if it is on a wall."""
    for l in TOWN_LEVELS:
        hh = STREET_HALF if l in STREET_Z else GARDEN_HALF
        if l - hh <= z <= l + hh:
            return l
    return None


def in_town(X, Y):
    """The valley's head behind and beside the court, the gate's axis kept clear."""
    R = np.hypot(X, Y)
    return (R > 58.0) & (R < 235.0) & (Y < 12.0) & ~((np.abs(X) < 27.0) & (Y < -20.0))


def town_sites(Z):
    """Houses along each street's middle line (the contour at its height), evenly spaced, facing down the slope."""
    out = []
    for street, z0 in enumerate(STREET_Z):
        line = []
        for a in np.radians(np.arange(150.0, 390.0, 0.4)):
            r_lo, r_hi = 58.0, 235.0
            zs = []
            for r in np.arange(r_lo, r_hi, 3.0):
                x, y = r * math.cos(a), r * math.sin(a)
                j = int(round((y - Y0) / CELL)); i = int(round((x + HALF) / CELL))
                zs.append((r, Z[j, i]))
            hit = None
            for (r0, z0a), (r1, z1a) in zip(zs, zs[1:]):
                if (z0a - z0) * (z1a - z0) <= 0 and z1a != z0a:
                    hit = r0 + (r1 - r0) * (z0 - z0a) / (z1a - z0a); break
            if hit is None:
                line.append(None); continue
            x, y = hit * math.cos(a), hit * math.sin(a)
            inner = 74.0 < hit < 214.0 and y < -2.0 and (abs(x) > 33.0 or y > -14.0)   # where the tiers are whole
            line.append((x, y) if inner else None)
        run = 0.0; prev = None
        for pt in line:
            if pt is None:
                prev = None; run = STREET_SPACING * (0.5 + 0.25 * (street % 2)); continue
            if prev is not None:
                run += math.hypot(pt[0] - prev[0], pt[1] - prev[1])
            prev = pt
            if run >= STREET_SPACING:
                out.append((round(pt[0], 1), round(pt[1], 1), 8.0, z0)); run = 0.0
    return out


BANK = (0.44, 0.48, 0.40)
BED_D, BED_IN, BED_OUT = 3.0, 5.0, 11.0     # the river's channel: 3 m deep, flat out to 5 m from its line, its bank up to 11 m
HAZE = (0.90, 0.76, 0.66)


def smooth(a, b, x):
    t = np.clip((np.asarray(x, np.float64) - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def noise(X, Y, wave, seed):
    r = np.random.default_rng(seed)
    n = int((Y1 - Y0) / wave) + 4
    g = r.random((n, n)) * 2 - 1
    u = (X + HALF) / wave + 1
    v = (Y - Y0) / wave + 1
    i, j = np.floor(u).astype(int), np.floor(v).astype(int)
    fu, fv = u - i, v - j
    fu, fv = fu * fu * (3 - 2 * fu), fv * fv * (3 - 2 * fv)
    i, j = np.clip(i, 0, n - 2), np.clip(j, 0, n - 2)
    return (g[j, i] * (1 - fu) + g[j, i + 1] * fu) * (1 - fv) + (g[j + 1, i] * (1 - fu) + g[j + 1, i + 1] * fu) * fv


def fbm(X, Y, wave, seed, octaves=4):
    out, amp, total = 0.0, 1.0, 0.0
    for k in range(octaves):
        out = out + noise(X, Y, wave / (2 ** k), seed + k * 17) * amp
        total += amp
        amp *= 0.5
    return out / total


def fields():
    """Everything on the grid: the ground before terracing, where terraces go, the river, the colours of the rest."""
    xs = np.linspace(-HALF, HALF, NX)
    ys = np.linspace(Y0, Y1, NY)
    X, Y = np.meshgrid(xs, ys)
    Yc = np.maximum(Y, HEAD_Y)
    meander = 32.0 * np.sin((Yc - 485.0) / 86.0) * smooth(60, 240, Yc) * smooth(790, 650, Yc)
    t_turn = np.maximum(Yc - TURN_Y, 0.0)
    line_x = meander + NOSE_X * smooth(600, 780, Yc) - TURN * t_turn * t_turn / (t_turn + 140.0)
    heading = np.gradient(line_x, CELL, axis=0)
    cosf = 1.0 / np.sqrt(1.0 + heading ** 2)
    across = (X - line_x) * cosf
    d = np.sqrt(across ** 2 + np.minimum(Y - HEAD_Y, 0.0) ** 2)
    spurs = 70 * fbm(X, Y, 260, 21, 3) + 30 * fbm(X, Y, 110, 44, 3)
    d_eff = np.maximum(d + spurs * smooth(40, 200, d), 0.0)
    along = Y - HEAD_Y
    side_run = 330 + 0.06 * np.maximum(along, 0.0)
    ridge = (R_FAR + (R_NEAR - R_FAR) * smooth(FALL_B, FALL_A, along)) * (1 + 0.16 * fbm(X, Y, 340, 60, 2))
    ridge = ridge * np.where(across < 0, 1.0 - 0.45 * np.exp(-((Y - 330) / 90.0) ** 2), 1.0)
    s = (d_eff - 9.0) / side_run
    wall = ridge * (0.36 * np.clip(s, 0.0, 1.0) + 0.64 * smooth(0.0, 1.0, s))
    beyond = np.maximum(s - 1.0, 0.0)
    wall = wall * (1.0 - 0.25 * np.clip(beyond, 0, 1.5)) + 22 * fbm(X, Y, 150, 75, 3) * smooth(0.25, 1.0, s)
    gully = np.abs(fbm(X, Y, 85, 91, 3))
    wall = wall - 26 * (1 - smooth(0.0, 0.22, gully)) * smooth(0.15, 0.6, s) * smooth(1.3, 0.8, s)
    for fx, fy, fr, fh in [(-440.0, 500.0, 150.0, 50.0), (-232.0, 856.0, 92.0, 46.0), (236.0, 1040.0, 160.0, 96.0)]:
        wall = wall + fh * np.exp(-((X - fx) ** 2 + (Y - fy) ** 2) / fr ** 2) * smooth(0.08, 0.35, s)
    floor = (-6.0 - 36.0 * smooth(40, 150, along + 0.06 * np.abs(across)) - 56.0 * smooth(150, 1150, along + 0.06 * np.abs(across))
             - 0.010 * np.maximum(along - 1150, 0.0))
    floor = floor + 1.2 * fbm(X, Y, 140, 3, 2)
    far_range = 188.0 * smooth(2250, 2800, Y + 0.10 * np.abs(X)) * (1 + 0.25 * fbm(X, Y, 300, 210, 2))
    Z = floor + np.maximum(wall, far_range)
    R = np.sqrt(X ** 2 + Y ** 2)
    flat = smooth(56, 40, R) * smooth(27, 19, Y)
    Z = Z * (1 - flat) - 6.0 * flat
    basin = np.hypot(X - HEAD_C[0], Y - HEAD_C[1]) < B_ROUT + 3.0
    cliff = head_cliff(X, Y) > 0.02
    # the ground just north of the pool is kept above its water, so the pool holds it
    lip = (np.hypot(X - HEAD_C[0], Y - HEAD_C[1]) < B_ROUT + 10.0) & (Y > HEAD_C[1]) & ~basin
    Z = np.where(lip & (not HEAD_PIECE), np.maximum(Z, BASIN[5] + 1.2), Z)

    rd = np.abs(X - line_x) * cosf
    bed = smooth(118, 140, along) * smooth(0.25, 0.0, s)          # where there is a channel at all; its shape is `channel`

    gy, gx = np.gradient(Z, CELL)
    slope = np.hypot(gx, gy)
    patch = fbm(X, Y, 190, 150, 2)
    T = smooth(1.05, 0.80, slope) * smooth(0.012, 0.035, slope) * smooth(34, 44, R)
    T = T * smooth(0.43, 0.39, s + 0.04 * patch) * smooth(66.0, 60.0, wall + 8.0 * patch)
    pd = np.hypot(X - POOL[0], Y - POOL[1])
    town_w = town_weight(X, Y)
    dA = np.maximum(np.maximum(np.abs(X) - PLAT[0], Y - PLAT[2]), PLAT[1] - Y)
    gorge = np.minimum(31.2 - np.abs(X), Y - 30.0)
    pad = np.maximum(dA, gorge) / PLAT_SCALE
    plat = pad
    pz = np.full_like(Z, RIM_Z)
    # the shrine's landing at the head, across the pool's outlet (tools/payo_dress.py stands the shrine on it)
    dS = np.maximum(np.abs(X) - SHRINE_PAD[0], np.abs(Y - SHRINE_PAD[1]) - SHRINE_PAD[2]) / PLAT_SCALE + (0.0 if SHRINE_PAD[0] > 0 else 1e9)
    pz = np.where(dS < pad, SHRINE_PAD[3], pz)
    pad = np.minimum(pad, dS)
    T = np.where(town_w >= 0.5, 0.0, T)                  # the valley's terraces run whole up to the town's edge
    T = np.where(plat < PAD_EDGE + 0.5, 0.0, T)
    # no paddies on the gate's axis just behind the plaza: the water stair reaches the plaza at the ground's own height,
    # not down a terrace step in a 4 m notch
    T = np.where((np.abs(X) < 27.0) & (Y > -95.0) & (Y < -40.0), 0.0, T)
    T = np.where(basin | cliff, 0.0, T)
    # a narrow edge, so few points are half-terraced (half-terraced ground came out as lumpy facets)
    T = smooth(0.30, 0.70, T)
    T = np.where(T < 0.02, 0.0, T)

    # the rest of the land, painted with soft edges
    woods = smooth(0.0, 0.45, fbm(X, Y, 160, 130, 2) * 0.35 + smooth(0.30, 0.55, s) * 1.2 - 0.10)
    col = np.zeros((NY, NX, 3)) + GRASS
    col = col * (1 - woods[..., None]) + np.array(FOREST) * woods[..., None]
    # bare rock on the steepest faces: a soft sage grey close to the forest's value, faded in over a wide range, so the
    # mountains read as one calm surface from the court (pale rock blotches on dark green read as stains from 300 m)
    rk = smooth(1.70, 2.30, slope) * (1 - smooth(0.2, 0.6, T)) * 0.85
    col = col * (1 - rk[..., None]) + np.array(MOUNT_ROCK) * rk[..., None]
    rim = smooth(40, 34, R) * smooth(22, 16, Y)
    col = col * (1 - rim[..., None]) + np.array([0.60, 0.60, 0.58]) * rim[..., None]
    F = fbm(X, Y, 200, 150, 2)                     # bends each plot's dike a little
    crop_zone = fbm(X, Y, 420, 260, 2)             # which paddies ripen first: in broad regions, not at random
    return dict(X=X, Y=Y, Z=Z, T=T, bed=bed, rd=rd, pd=pd, pad=pad, pz=pz, tw=town_w, sites=town_sites(Z), col=col, F=F, zone=crop_zone, line_x=line_x[:, 0].copy(), s=s)


def stepped(z):
    """The terraced height for the ground's own height z: flat paddies, then a short steep wall up to the next."""
    k = np.floor(z / STEP)
    q = z - k * STEP
    w0 = STEP - RISER
    return np.where(q < w0, k * STEP, k * STEP + STEP * (q - w0) / RISER)


def channel(rd):
    """How deep the river's channel is at a distance rd from its line: flat-bottomed, straight-sided (so its cut edges
    lie on smooth lines)."""
    return BED_D * np.clip((BED_OUT - rd) / (BED_OUT - BED_IN), 0.0, 1.0)


def plot_len(k):
    return 95.0 + 60.0 * ((k * 37) % 5) / 4.0


def plot_val(y, f, k):
    return (y + 23.0 * ((k * 53) % 7) + 26.0 * f) / plot_len(k)


def build(g):
    """Cuts the grid's triangles along the terraces' lines. Returns the vertices, the polygons and each one's colour."""
    t0 = time.time()
    X, Y, Z, T, bed, F, RD, PD, PAD, PZ = (g[k].ravel() for k in ("X", "Y", "Z", "T", "bed", "F", "rd", "pd", "pad", "pz"))
    vx, vy, vz, vt, vb, vf, vr, vp, vpad, vpz = (list(a) for a in (X, Y, Z, T, bed, F, RD, PD, PAD, PZ))
    # distance from the plunge pool's middle, carried like the others so its cuts and its heights agree exactly
    vbd = list(np.hypot(X - HEAD_C[0], Y - HEAD_C[1]))
    idx = np.arange(NX * NY).reshape(NY, NX)
    a, b, c, d = idx[:-1, :-1].ravel(), idx[:-1, 1:].ravel(), idx[1:, 1:].ravel(), idx[1:, :-1].ravel()
    tris = np.concatenate([np.stack([a, b, c], 1), np.stack([a, c, d], 1)])
    tmax = T[tris].max(axis=1)
    near = (RD[tris].min(axis=1) < BED_OUT + 1.0) & (bed[tris].max(axis=1) > 0.0)
    near = near | (PD[tris].min(axis=1) < POOL[3] + 1.0) | (PAD[tris].min(axis=1) < PAD_EDGE + 1.0)
    TW = g["tw"].ravel()
    vtw = list(TW)
    near = near | (TW[tris].max(axis=1) > 0.0)
    BD = np.hypot(X - HEAD_C[0], Y - HEAD_C[1])
    near = near | (BD[tris].min(axis=1) < B_ROUT + B_FACE + 2.0)
    town_xp, _ = town_breaks()
    plain = tris[(tmax <= 0.0) & ~near]
    todo = tris[(tmax > 0.0) | near]
    made = {}

    def point(p, q, va, vb_, t, key):
        k2 = (p, q, key) if p < q else (q, p, key)
        got = made.get(k2)
        if got is not None:
            return got
        al = (t - va) / (vb_ - va)
        for arr in (vx, vy, vz, vt, vb, vf, vr, vp, vpad, vpz, vbd, vtw):
            arr.append(arr[p] + (arr[q] - arr[p]) * al)
        made[k2] = len(vx) - 1
        return made[k2]

    def split(poly, val, t, key):
        lo, hi = [], []
        n = len(poly)
        vals = [val(i) for i in poly]
        for i in range(n):
            p, q = poly[i], poly[(i + 1) % n]
            vp, vq = vals[i], vals[(i + 1) % n]
            (lo if vp < t else hi).append(p)
            if (vp < t) != (vq < t):
                m = point(p, q, vp, vq, t, key)
                lo.append(m); hi.append(m)
        return lo, hi

    pieces, kinds = [], []          # kind: (0 paddy | 1 wall | 2 bund | 3 dike, level, plot)
    zval = vz.__getitem__
    for tri in todo.tolist():
        zs = [vz[i] for i in tri]
        lo_z, hi_z = min(zs), max(zs)
        cuts = []
        for k in range(int(math.floor(lo_z / STEP)) - 1, int(math.floor(hi_z / STEP)) + 2):
            for t in (k * STEP - RISER, k * STEP, k * STEP + BUND):
                if lo_z < t < hi_z:
                    cuts.append(t)
        if TW[tri].max() > 0.0:
            cuts += [t for t in town_xp if lo_z < t < hi_z]
        polys = [tri]
        # the town's edge, where its tiers meet the valley's own terraces: one clean line, a stone wall along it
        # (owner, 2026-10-18: the tiers and terraces blended there into saw teeth)
        tws = [vtw[i] for i in tri]
        if min(tws) < 0.5 < max(tws):
            nxt = []
            for poly in polys:
                lo, hi = split(poly, vtw.__getitem__, 0.5, ("w", 0.5))
                if len(lo) >= 3: nxt.append(lo)
                if len(hi) >= 3: nxt.append(hi)
            polys = nxt
        rs = [vr[i] for i in tri]
        qs = [vpad[i] for i in tri]
        for t in (0.0, PAD_EDGE):                  # a house platform's edge and its wall's foot
            if min(qs) < t < max(qs):
                nxt = []
                for poly in polys:
                    lo, hi = split(poly, vpad.__getitem__, t, ("a", t))
                    if len(lo) >= 3: nxt.append(lo)
                    if len(hi) >= 3: nxt.append(hi)
                polys = nxt
        ps = [vp[i] for i in tri]
        bds = [vbd[i] for i in tri]
        for t in (B_RIN, 0.5 * (B_RIN + B_ROUT), B_ROUT, B_ROUT + B_FACE):   # the plunge pool's floor, bowl, rim, cliff top
            if min(bds) < t < max(bds):
                nxt = []
                bdf = vbd.__getitem__
                for poly in polys:
                    lo, hi = split(poly, bdf, t, ("b", t))
                    if len(lo) >= 3: nxt.append(lo)
                    if len(hi) >= 3: nxt.append(hi)
                polys = nxt
        for t in (POOL[2], 0.5 * (POOL[2] + POOL[3]), POOL[3]):      # the pool's floor, its bank, its rim
            if min(ps) < t < max(ps):
                nxt = []
                for poly in polys:
                    lo, hi = split(poly, vp.__getitem__, t, ("o", t))
                    if len(lo) >= 3: nxt.append(lo)
                    if len(hi) >= 3: nxt.append(hi)
                polys = nxt
        for t in (BED_IN, BED_OUT):                # the channel's bottom edge and its top edge
            if min(rs) < t < max(rs):
                nxt = []
                for poly in polys:
                    lo, hi = split(poly, vr.__getitem__, t, ("r", t))
                    if len(lo) >= 3: nxt.append(lo)
                    if len(hi) >= 3: nxt.append(hi)
                polys = nxt
        for t in sorted(cuts):
            nxt = []
            key = ("z", int(round(t * 1000)))
            for poly in polys:
                lo, hi = split(poly, zval, t, key)
                if len(lo) >= 3: nxt.append(lo)
                if len(hi) >= 3: nxt.append(hi)
            polys = nxt
        for poly in polys:
            ac = sum(vpad[i] for i in poly) / len(poly)
            if ac < PAD_EDGE:
                pieces.append(poly); kinds.append((6 if ac < 0.0 else 7, 0, 0)); continue
            cx_ = sum(vx[i] for i in poly) / len(poly); cy_ = sum(vy[i] for i in poly) / len(poly)
            zc_ = sum(vz[i] for i in poly) / len(poly)
            bc_ = sum(vbd[i] for i in poly) / len(poly)
            if bc_ < B_ROUT:
                pieces.append(poly); kinds.append((10, 0, 0)); continue          # the plunge pool's stone bowl
            if bc_ < B_ROUT + B_FACE and float(head_cliff(np.array(cx_), np.array(cy_))) > 0.05:
                pieces.append(poly); kinds.append((11, 0, 0)); continue          # the cliff's face
            if sum(vtw[i] for i in poly) / len(poly) > 0.5 and town_xp[0] <= zc_ <= town_xp[-1]:
                lvl = town_level(zc_)
                pieces.append(poly); kinds.append((8, int(round(lvl * 10)), 0) if lvl is not None else (9, 0, 0)); continue
            pc = sum(vp[i] for i in poly) / len(poly)
            if pc < POOL[3]:
                pieces.append(poly); kinds.append((5 if pc < 0.5 * (POOL[2] + POOL[3]) else 4, 1, 0)); continue
            rc = sum(vr[i] for i in poly) / len(poly); bc = sum(vb[i] for i in poly) / len(poly)
            if rc < BED_OUT and bc > 0.3:
                pieces.append(poly); kinds.append((4, 0, 0)); continue
            zc = sum(vz[i] for i in poly) / len(poly)
            k = int(math.floor(zc / STEP))
            q = zc - k * STEP
            if q >= STEP - RISER:
                pieces.append(poly); kinds.append((1, k, 0)); continue
            if q < BUND:
                pieces.append(poly); kinds.append((2, k, 0)); continue
            # a paddy: cut it into plots, with a dike between
            pv = (lambda i, k=k: plot_val(vy[i], vf[i], k))
            vals = [pv(i) for i in poly]
            sub = [poly]
            for n in range(int(math.floor(min(vals))), int(math.floor(max(vals))) + 2):
                for t in (n - DIKE / plot_len(k), n + DIKE / plot_len(k)):
                    if min(vals) < t < max(vals):
                        nxt = []
                        key = ("p", k, int(round(t * 100000)))
                        for poly2 in sub:
                            lo, hi = split(poly2, pv, t, key)
                            if len(lo) >= 3: nxt.append(lo)
                            if len(hi) >= 3: nxt.append(hi)
                        sub = nxt
            for poly2 in sub:
                pc = sum(pv(i) for i in poly2) / len(poly2)
                n = int(math.floor(pc + 0.5))
                kinds.append((3, k, 0) if abs(pc - n) * plot_len(k) < DIKE else (0, k, int(math.floor(pc))))
                pieces.append(poly2)
    # split the town's edge: pieces outside it take their own copies of the points on the edge, so the two sides can stand
    # at different heights, and a wall closes the step between them
    on_edge = {i for i in range(len(vtw)) if abs(vtw[i] - 0.5) < 1e-9}
    dup = {}

    def twin(i):
        if i not in dup:
            for arr in (vx, vy, vz, vt, vb, vf, vr, vp, vpad, vpz, vbd, vtw):
                arr.append(arr[i])
            vtw[-1] = 0.49
            dup[i] = len(vx) - 1
        return dup[i]

    outside_edges = set()
    for n, poly in enumerate(pieces):
        if not any(i in on_edge for i in poly) or sum(vtw[i] for i in poly) / len(poly) > 0.5:
            continue
        for a, b in zip(poly, poly[1:] + poly[:1]):
            if a in on_edge and b in on_edge:
                outside_edges.add((min(a, b), max(a, b)))
        pieces[n] = [twin(i) if i in on_edge else i for i in poly]
    walls = 0
    for n in range(len(pieces)):
        poly = pieces[n]
        if not any(i in on_edge for i in poly) or sum(vtw[i] for i in poly) / len(poly) <= 0.5:
            continue
        for a, b in zip(poly, poly[1:] + poly[:1]):
            if a in on_edge and b in on_edge and (min(a, b), max(a, b)) in outside_edges:
                pieces.append([a, b, twin(b), twin(a)]); kinds.append((12, 0, 0)); walls += 1
    print("town edge: %d points, %d wall faces" % (len(on_edge), walls))
    print("cut %d terraced triangles into %d pieces, %d new points (%.0f s)" % (len(todo), len(pieces), len(vx) - NX * NY, time.time() - t0))

    vx, vy, vz, vt, vb, vr, vp, vpad, vpz, vbd, vtw = (np.array(a) for a in (vx, vy, vz, vt, vb, vr, vp, vpad, vpz, vbd, vtw))
    h = vz + vt * (stepped(vz) - vz) - channel(vr) * vb
    w = np.clip((POOL[3] - vp) / (POOL[3] - POOL[2]), 0.0, 1.0)
    rim = POOL[5] + 0.4 + 5.1 * smooth(14.0, 24.0, np.abs(vx - POOL[0]))   # low where the fall comes in and the river leaves
    bowl = POOL[4] + (rim - POOL[4]) * np.clip((vp - POOL[2]) / (POOL[3] - POOL[2]), 0.0, 1.0)
    h = h * (1 - w) + bowl * w
    h = np.where(vtw >= 0.5, town_step(vz), h)
    wp = smooth(PAD_EDGE, 0.0, vpad)              # the platform eases into the slope
    h = h * (1 - wp) + vpz * wp
    stair = (vy >= STAIR[2]) & (vy <= STAIR[3])
    h = np.where(stair, h - STAIR[4] * np.clip((STAIR[1] - np.abs(vx)) / (STAIR[1] - STAIR[0]), 0.0, 1.0), h)
    # the head: the round plunge pool, and the cliff's horseshoe behind it, held up to the lip the fall leaves
    bdv = vbd
    cw = head_cliff(vx, vy)
    rim_h = BASIN[5] + 1.2
    bowl_h = BASIN[4] + (rim_h - BASIN[4]) * np.clip((bdv - B_RIN) / (B_ROUT - B_RIN), 0.0, 1.0)
    held = h * (1 - cw) + np.maximum(h, CLIFF_TOP) * cw
    # a level ledge along the cliff's top, so its edge is one clean curve (the uneven hill behind made it a saw)
    ledge = smooth(B_ROUT + 9.0, B_ROUT + 3.0, bdv) * cw
    held = held * (1 - ledge) + CLIFF_TOP * ledge
    f = np.clip((bdv - B_ROUT) / B_FACE, 0.0, 1.0)
    if HEAD_PIECE:
        # under the piece's pool and kerb (outer radius 20.2, kerb foot 43.6), and the ground north of it no higher
        # than the kerb's outer face (top 46.2), rising away from it
        north = vy > HEAD_C[1] - 4.0
        cap = np.where(bdv <= 20.6, 41.5, np.where(north, 45.9 + np.maximum(bdv - 22.0, 0.0) * 1.2, 1e9))
        h = np.minimum(h, cap)
        # near the pool the land never covers the piece's rock: it stays 0.6 m under the piece's own surface, easing
        # back to the hillside's own height between 32 and 44 m out (out there the hill may bury the piece's skirts)
        hg = os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "head", "head_height_grid.npz")
        if os.path.exists(hg):
            G = np.load(hg)
            gi = (vx - float(G["x0"])) / float(G["step"]); gj = (vy - float(G["y0"])) / float(G["step"])
            Hh = G["h"]
            inside = (gi >= 0) & (gj >= 0) & (gi <= Hh.shape[1] - 1) & (gj <= Hh.shape[0] - 1)
            ii = np.clip(np.round(gi).astype(int), 0, Hh.shape[1] - 1); jj = np.clip(np.round(gj).astype(int), 0, Hh.shape[0] - 1)
            ph = np.where(inside, Hh[jj, ii], np.nan)
            w = smooth(44.0, 32.0, bdv) * np.isfinite(ph)
            h = np.where(w > 0, h * (1 - w) + np.minimum(h, np.nan_to_num(ph, nan=1e9) - 0.6) * w, h)
    else:
        h = np.where(bdv <= B_ROUT, bowl_h, np.where(bdv < B_ROUT + B_FACE, rim_h * (1 - f) + held * f, held))
    # the authored sacred pool (tools/payo_pieces/pool_build.py): the land stays under its stone, and within 4 m of it
    # no higher than its skirt's lower tread (-46.05), rising back to the land's own height over the next 10 m
    if POOL_GRID is not None:
        from scipy.ndimage import distance_transform_edt
        Hp = POOL_GRID["h"]; st = float(POOL_GRID["step"]); x0 = float(POOL_GRID["x0"]); y0 = float(POOL_GRID["y0"])
        dist = distance_transform_edt(~np.isfinite(Hp)) * st
        gi = np.round((vx - x0) / st).astype(int); gj = np.round((vy - y0) / st).astype(int)
        inside = (gi >= 0) & (gj >= 0) & (gi < Hp.shape[1]) & (gj < Hp.shape[0])
        gi = np.clip(gi, 0, Hp.shape[1] - 1); gj = np.clip(gj, 0, Hp.shape[0] - 1)
        ph = np.where(inside, Hp[gj, gi], np.nan); dd = np.where(inside, dist[gj, gi], 1e9)
        cap = np.where(np.isfinite(ph), ph - 0.6, -46.3 + np.maximum(dd - 4.0, 0.0) * 1.2)
        h = np.minimum(h, cap)
    # the points ON the pit's edge go down too, so its earth face lies behind the carved walls, not in front of them
    pit = (np.abs(vx) <= 30.01) & (np.abs(vy) <= 30.01)
    h = np.where(pit, np.minimum(h, -30.35), h)            # under the court piece's pit floor (-30)
    # the pit has no north wall: its floor ends in a cliff, 16 m straight down into the sacred pool (grid lines x = +-30,
    # y = 30 and 33, so its edges are clean)
    scarp = (np.abs(vx) <= 30.0) & (vy >= 30.0) & (vy <= 56.0)      # the whole gorge, wall to wall, one flat floor under its water
    h = np.where(scarp, np.minimum(h, -30.35 - 16.4 * np.clip((vy - 30.0) / 3.0, 0.0, 1.0)), h)   # 46.4: off the courses' ledges
    verts = np.stack([vx, vy, h], 1).astype(np.float32)

    # colours: the land's own soft colour at every point, sampled from the grid
    col = g["col"]
    fi = (vx + HALF) / CELL; fj = (vy - Y0) / CELL
    i0 = np.clip(np.floor(fi).astype(int), 0, NX - 2); j0 = np.clip(np.floor(fj).astype(int), 0, NY - 2)
    ai = (fi - i0)[:, None]; aj = (fj - j0)[:, None]
    vcol = (col[j0, i0] * (1 - ai) * (1 - aj) + col[j0, i0 + 1] * ai * (1 - aj) + col[j0 + 1, i0] * (1 - ai) * aj
            + col[j0 + 1, i0 + 1] * ai * aj)
    vcol = np.where(pit[:, None], np.array([0.10, 0.11, 0.13]), vcol)
    vcol = np.where(scarp[:, None], np.array(ROCK), vcol)
    zone = g["zone"]
    zone_v = (zone[j0, i0] * (1 - ai[:, 0]) * (1 - aj[:, 0]) + zone[j0, i0 + 1] * ai[:, 0] * (1 - aj[:, 0])
              + zone[j0 + 1, i0] * (1 - ai[:, 0]) * aj[:, 0] + zone[j0 + 1, i0 + 1] * ai[:, 0] * aj[:, 0])

    palette = [np.array(c) for c, w in PADDY for _ in range(w)]
    loops, totals, lcol = [], [], []
    # untouched triangles: the land's soft colour
    loops.append(plain.ravel()); totals.append(np.full(len(plain), 3)); lcol.append(vcol[plain.ravel()])
    mats = [np.zeros(len(plain), np.int8)]
    flat_loops, flat_tot, flat_col, flat_mat = [], [], [], []
    for poly, (kind, k, plot) in zip(pieces, kinds):
        tc = sum(vt[i] for i in poly) / len(poly)
        flat_loops.extend(poly); flat_tot.append(len(poly))
        if kind in (6, 7):
            lz = sum(vpz[i] for i in poly) / len(poly)
            if kind == 7:
                # stone walls, a little darker toward their foot
                c = np.array(PAD_WALL)
                flat_col.extend(c * (0.86 + 0.14 * smooth(lz - 12.0, lz, h[i])) for i in poly); flat_mat.append(1); continue
            if abs(lz - RIM_Z) < 0.5:
                c, m = PLAT_TOP, 4
            elif min(abs(lz - z) for z in STREET_Z) < 0.5:
                c, m = PAD_TOP, 4
            else:
                c, m = GARDEN[int(round(lz)) % 2], 0
            flat_col.extend(np.array(c) for i in poly); flat_mat.append(m); continue
        if kind == 10:
            flat_col.extend(np.array(BASIN_STONE) for i in poly); flat_mat.append(4); continue
        if kind == 11:
            flat_col.extend(np.array(HEAD_ROCK) for i in poly); flat_mat.append(3); continue
        if kind == 8:
            lz = k / 10.0
            if min(abs(lz - z) for z in STREET_Z) < 0.5:
                c, m = PAD_TOP, 4
            else:
                c, m = GARDEN[int(round(lz)) % 2], 0
            flat_col.extend(np.array(c) for i in poly); flat_mat.append(m); continue
        if kind == 12:
            hs = [h[i] for i in poly]; top = max(hs)
            flat_col.extend(np.array(TOWN_WALL) * (0.86 + 0.14 * smooth(top - 8.0, top, h[i])) for i in poly); flat_mat.append(1); continue
        if kind == 9:
            hs = [h[i] for i in poly]; top = max(hs)
            c = np.array(TOWN_WALL)
            flat_col.extend(c * (0.86 + 0.14 * smooth(top - 8.0, top, h[i])) for i in poly); flat_mat.append(1); continue
        if tc < 0.8:
            flat_col.extend(vcol[i] for i in poly); flat_mat.append(0); continue
        if kind == 4:
            # the pool's banks are grass (a dark stone mound there read as a hole); the river keeps its stony banks
            flat_col.extend(np.array(BUND_C if k == 1 else BANK) for i in poly); flat_mat.append(0 if k == 1 else 3); continue
        if kind == 5:
            flat_col.extend(np.array((0.24, 0.34, 0.34)) for i in poly); flat_mat.append(3); continue
        m = 2
        if kind == 1:
            c = np.array(WALL); m = 1
        elif kind in (2, 3):
            c = np.array(BUND_C); m = 0
        else:
            zc = sum(zone_v[i] for i in poly) / len(poly)
            pick = (k * 7 + plot * 13 + 1000) % len(palette)
            # ripe and flooded paddies gather where the broad 'zone' says; elsewhere it is green
            c = palette[pick]
            if zc < 0.05 and (c[0] > 0.8):
                c = palette[(pick + 3) % 10]
            if zc > 0.22 and pick % 3 == 0:
                c = np.array(PADDY[4][0]) if pick % 2 else np.array(PADDY[6][0])
            if c is palette[pick] and tuple(c) == PADDY[6][0] or (zc > 0.22 and pick % 3 == 0 and pick % 2 == 0):
                m = 5
        # a terrace that is only half there takes the land's colour in part
        w = min(1.0, (tc - 0.8) / 0.15)
        flat_col.extend(c * w + vcol[i] * (1 - w) for i in poly)
        flat_mat.append(m)
    loops.append(np.array(flat_loops, np.int64)); totals.append(np.array(flat_tot)); lcol.append(np.array(flat_col))
    mats.append(np.array(flat_mat, np.int8))
    mat = np.concatenate(mats)
    loops = np.concatenate(loops).astype(np.int32)
    totals_ = np.concatenate(totals).astype(np.int32)
    lc = np.concatenate(lcol)
    # stone where the head's works are: the basin, its lip, the cliff, the water stair (each face by its middle)
    face = np.repeat(np.arange(len(totals_)), totals_)
    cnt = np.bincount(face).astype(np.float64)
    cx = np.bincount(face, verts[loops, 0]) / cnt; cy = np.bincount(face, verts[loops, 1]) / cnt
    works = (np.abs(cx) <= STAIR[1]) & (cy >= STAIR[2]) & (cy <= STAIR[3])
    rocky = np.zeros_like(works)                   # the cliff's face is coloured as its own kind (11)
    lc = np.where(works[face][:, None], np.array(STONE), lc)
    # the head's cliff in a mossy dark stone: the great fall reads white against it, and from the court it is no pale scar
    lc = np.where(rocky[face][:, None], np.array(HEAD_ROCK), lc)
    mat = np.where(works, 4, np.where(rocky, 3, mat)).astype(np.int8)
    lcol = [lc]
    totals = np.concatenate(totals).astype(np.int32)
    lcol = np.concatenate(lcol)
    # distance haze, toward the sunset
    lv = verts[loops]
    haze = (0.22 * smooth(900, 2800, lv[:, 1]))[:, None]     # the rest of the distance is the air's (Blender: world volume)
    lcol = (lcol * (1 - haze) + np.array(HAZE) * haze).astype(np.float32)
    return verts, loops, totals, lcol, len(plain), mat


def head_water(g):
    """The water stair's ribbon, from the basin down to the gate, falling only toward the court; and the basin."""
    Z, T = g["Z"], g["T"]
    i = int(round((0.0 + HALF) / CELL))
    ys = np.arange(STAIR[2], STAIR[3] + 0.1, 1.0)
    jj = (ys - Y0) / CELL
    j0 = np.clip(np.floor(jj).astype(int), 0, NY - 2); a = jj - j0
    z = Z[j0, i] * (1 - a) + Z[j0 + 1, i] * a
    t = T[j0, i] * (1 - a) + T[j0 + 1, i] * a
    h = z + t * (stepped(z) - z) - STAIR[4]
    zc = np.minimum.accumulate(np.minimum(h + 1.1, BASIN[5]))
    zc = np.maximum(zc, h + 0.5)
    return np.stack([np.zeros_like(ys), ys, zc], 1)


def river(g):
    """The river's water as a ribbon along its line, falling only downstream: where its line crosses a terrace wall it
    drops as a little fall."""
    Y, Z, T, bed = g["Y"], g["Z"], g["T"], g["bed"]
    ys = np.arange(60.0, 2790.0, 2.0)
    lx = np.interp(ys, Y[:, 0], g["line_x"])
    ok = lx > -HALF + 20
    ys, lx = ys[ok], lx[ok]
    fi = (lx + HALF) / CELL; fj = (ys - Y0) / CELL
    i0 = np.clip(np.floor(fi).astype(int), 0, NX - 2); j0 = np.clip(np.floor(fj).astype(int), 0, NY - 2)
    ai, aj = fi - i0, fj - j0

    def bil(a):
        return a[j0, i0] * (1 - ai) * (1 - aj) + a[j0, i0 + 1] * ai * (1 - aj) + a[j0 + 1, i0] * (1 - ai) * aj + a[j0 + 1, i0 + 1] * ai * aj

    z, t, b = bil(Z), bil(T), bil(bed)
    h = z + t * (stepped(z) - z) - BED_D * b
    keep = (b > 0.5) & (np.hypot(lx - POOL[0], ys - POOL[1]) > POOL[3] - 4.0) & (ys > POOL[1])
    if POOL_GRID is not None:
        keep = keep & (ys > 98.2)                     # the pool piece's own water runs to y 98.5
    ys, lx, h = ys[keep], lx[keep], h[keep]
    zc = np.minimum.accumulate(np.minimum(h + 1.9, POOL[5]))
    zc = np.maximum(zc, h + 0.8)                   # never under its own bed
    return np.stack([lx, ys, zc], 1)


SKYLINE = [(-36, 13.5), (-25, 7.0), (-22, 6.0), (-16, 6.0), (-12, 0.5), (-3.5, -1.8), (-2, -2.2), (2.7, 0.9), (8, 2.1), (11.4, 5.9), (15.7, 5.7)]


def check(g):
    hz = g["Z"] + g["T"] * (stepped(g["Z"]) - g["Z"])
    r = np.arange(48.0, 2900.0, CELL)
    worst = 0.0
    for az, want in SKYLINE:
        x = r * math.sin(math.radians(az)); y = r * math.cos(math.radians(az))
        i = np.round((x + HALF) / CELL).astype(int); j = np.round((y - Y0) / CELL).astype(int)
        ok = (i >= 0) & (i < NX) & (j >= 0) & (j < NY)
        got = float(np.degrees(np.arctan2(hz[j[ok], i[ok]] - 1.7, r[ok])).max())
        worst = max(worst, abs(got - want))
    print("skyline against the owner's line: worst miss %.1f degrees" % worst)


if __name__ == "__main__":
    tag = sys.argv[1] if len(sys.argv) > 1 else "v16"
    g = fields()
    check(g)
    verts, loops, totals, lcol, n_plain, mat = build(g)
    water = river(g)
    stair = head_water(g)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, "land_%s.npz" % tag)
    np.savez_compressed(path, verts=verts, loops=loops, totals=totals, lcol=lcol, n_plain=np.array(n_plain), mat=mat, water=water.astype(np.float32), pool=np.array(POOL),
                        stair=stair.astype(np.float32), basin=np.array(BASIN), cliff=np.array(CLIFF),
                        sites=np.array(g['sites']),
                        T=g["T"][::2, ::2].astype(np.float16), S=g["s"][::2, ::2].astype(np.float16), TW=g["tw"][::2, ::2].astype(np.float16),
                        grid=np.array([-HALF, Y0, CELL * 2]))
    print("wrote %s: %d points, %d polygons" % (path, len(verts), len(totals)))
