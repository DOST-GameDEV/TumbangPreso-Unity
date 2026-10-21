"""PAETE'S RUN, THE TWO SLIDES: each one worn water channel, swept along the layout's own chute (2026-10-10).

Owner: "maybe just the slide shutes but try to incorporate them well into the map"; after playing on the blockout's
straight teal slabs: "do the art pass on the slide walls and decks".

A slide in the layout is twelve straight beds (pitched boxes) with a slab wall each side, meeting at angles. Here it is
ONE piece: a cross-section (a bed dished 2 cm, a fillet, a rounded lip whose crown is the wall's top, an outer shoulder)
swept along the chute's centre line (`chutes[].points`), with every bend ROUNDED over the last 0.9 m of each segment.

  * In the straight part of a segment every vertex takes its height from THAT SEGMENT'S SOLIDS' OWN TOP PLANES
    (`plane_z` of the bed, of the wall on that side), so the art is the collision's surface there to the centimetre.
    Inside a bend it blends from one segment's planes to the next (the layout's boxes disagree by up to 13 cm at a
    bend's outer edge: there is no one surface to match; the art takes the middle).
  * Where the massif stands beside the channel its outer side is a rock shoulder (the channel is CUT IN); where the
    ground falls away it is dressed stone on piers and arches (an aqueduct).
  * The water is a separate mesh in `pr_water_running`, 2 cm over the bed (5 mm over the collision), its UV v running
    downstream in units of 4.6 m, so a shader can make it flow.
"""
import math
import random

from mathutils import Matrix, Vector, noise

import author_paete_run_art_kit as K
import author_paete_run_art_world as W
import author_paete_run_art_stages as ST
import author_paete_run_art_solids as SO

UP = Vector((0, 0, 1))
T, frame = K.T, K.frame
CH = K.Buf("run_slides", 41)             # the channels: smooth, never bevelled
WAT = K.Buf("run_slide_water", 43)       # the sheet of water on each bed, and what it runs out into
INFO = {}                                # per slide: what was built, for the report


def plane_z(s, x, y):
    """The height of a (pitched) box's TOP plane over (x, y)."""
    n = s.up
    tc = s.c + s.up * (s.sy / 2)
    return tc.z - (n.x * (x - tc.x) + n.y * (y - tc.y)) / n.z


def smooth(u):
    return u * u * (3 - 2 * u)


def stations(pts, ext=0.15, step=0.5, tan=0.9):
    """(position, tangent, segment, next segment, blend) along the centre line, the bends rounded."""
    n = len(pts) - 1
    P = [Vector((p.x, p.y)) for p in pts]
    d = [(P[i + 1] - P[i]).normalized() for i in range(n)]
    L = [(P[i + 1] - P[i]).length for i in range(n)]
    a = [0.0] * (n + 1)
    for j in range(1, n):
        if abs(d[j - 1].angle_signed(d[j])) > math.radians(0.5):
            a[j] = min(tan, 0.3 * min(L[j - 1], L[j]))
    out = []
    for i in range(n):
        s0 = a[i] if i else -ext
        s1 = L[i] - a[i + 1] if i < n - 1 else L[i] + ext
        m = max(1, math.ceil((s1 - s0) / step))
        for k in range(m + 1):
            pos = P[i] + d[i] * (s0 + (s1 - s0) * k / m)
            if out and (pos - out[-1][0]).length < 1e-4:
                continue
            out.append((pos, d[i], i, i, 0.0))
        if i < n - 1 and a[i + 1] > 0:
            A_, B_, C_ = P[i + 1] - d[i] * a[i + 1], P[i + 1], P[i + 1] + d[i + 1] * a[i + 1]
            m2 = max(2, math.ceil(math.degrees(abs(d[i].angle_signed(d[i + 1]))) / 4.0))
            for k in range(1, m2):
                u = k / m2
                out.append((A_ * (1 - u) ** 2 + B_ * (2 * u * (1 - u)) + C_ * (u * u), ((B_ - A_) * (1 - u) + (C_ - B_) * u).normalized(), i, i + 1, smooth(u)))
    return out


def profile(H, cut):
    """Half the cross-section: (metres from the centre line, height over the bed's plane, what the strip from here is)."""
    p = [(0.0, -0.022, "bed"), (0.9, -0.020, "bed"), (1.7, -0.013, "bed"), (2.3, -0.004, "wet"), (2.47, 0.012, "wet"), (2.62, 0.07, "wet"),
         (2.73, 0.21, "lip"), (2.79, 0.46, "lip"), (2.805, H - 0.30, "lip"), (2.83, H - 0.10, "crown"), (2.90, H - 0.022, "crown"), (3.05, H - 0.008, "crown"),
         (3.20, H - 0.022, "crown"), (3.27, H - 0.10, "out"), (3.34, H - 0.34, "out")]
    if cut:
        return p + [(3.7, H - 0.62, "berm"), (4.5, 0.42, "berm"), (5.3, 0.1, "berm"), (5.9, -0.7, "berm")]
    return p + [(3.44, H - 0.8, "out"), (3.62, -0.05, "under"), (3.84, -0.95, "under"), (3.9, -1.7, "under")]


def band_mat(band, run, sd, seed):
    v = noise.noise(Vector((run * 1.3, sd * 7.3 + seed, 0.37)))
    w = noise.noise(Vector((run * 0.19 + 11.0, sd * 3.1 - seed, 4.2)))
    if band == "bed":
        return "pr_stone_wet"
    if band == "wet":
        return "pr_stone_stain"
    if band == "lip":
        return "pr_stone_stain" if v > 0.3 or int(run * 2) % 9 == 4 else "pr_stone_worn"       # a streak where water has run down the lip
    if band == "crown":
        return "pr_moss" if w > 0.22 else "pr_stone"
    if band == "out":
        return "pr_moss" if w > 0.34 else "pr_ashlar" if v > -0.2 else "pr_stone_worn"
    return "pr_rock" if band == "berm" else "pr_ashlar_dark"


def build(leg_n, rng):
    leg = W.LEGS[leg_n - 1]
    pts = leg.pts
    nseg = len(pts) - 1
    beds = [W.BY["chute_%d_bed_%d" % (leg_n, i)] for i in range(nseg)]
    walls = []
    for i in range(nseg):
        f, s = W.side_of(pts[i], pts[i + 1])
        a, b = W.BY["chute_%d_wall_%d_l" % (leg_n, i)], W.BY["chute_%d_wall_%d_r" % (leg_n, i)]
        walls.append({1: a, -1: b} if (a.c - beds[i].c).dot(s) > 0 else {1: b, -1: a})
    sta = stations(pts)
    nst = len(sta)
    runs = [0.0]
    for k in range(1, nst):
        runs.append(runs[-1] + (sta[k][0] - sta[k - 1][0]).length)

    def zb(k, q):
        _, _, i, j, t = sta[k]
        return plane_z(beds[i], q.x, q.y) * (1 - t) + plane_z(beds[j], q.x, q.y) * t

    def zw(k, q, sd):
        _, _, i, j, t = sta[k]
        return plane_z(walls[i][sd], q.x, q.y) * (1 - t) + plane_z(walls[j][sd], q.x, q.y) * t

    # is the massif beside the channel here (a rock shoulder) or does the ground fall away (dressed stone, an aqueduct)?
    cut = {}
    for sd in (-1, 1):
        raw = []
        for k in range(nst):
            pos, f = sta[k][0], sta[k][1]
            r = Vector((f.y, -f.x))
            q = pos + r * (sd * 5.6)
            raw.append(W.ground(q.x, q.y) > zb(k, pos) - 1.0)
        cut[sd] = [sum(raw[max(0, k - 2):k + 3]) * 2 > len(raw[max(0, k - 2):k + 3]) for k in range(nst)]
    rows = {}
    for k in range(nst):
        pos, f = sta[k][0], sta[k][1]
        r = Vector((f.y, -f.x))
        for sd in (-1, 1):
            ql = pos + r * (sd * 3.05)
            H = zw(k, ql, sd) - zb(k, ql)
            row = []
            for (d, dz, bnd) in profile(H, cut[sd][k]):
                q = pos + r * (sd * d)
                row.append((Vector((q.x, q.y, zb(k, q) + dz)), bnd))
            rows[(k, sd)] = row
    npf = len(rows[(0, 1)])
    for k in range(nst - 1):
        for sd in (-1, 1):
            a, b = rows[(k, sd)], rows[(k + 1, sd)]
            lat = 0.0
            for j in range(npf - 1):
                w = (a[j + 1][0] - a[j][0]).length
                quad = [a[j][0], a[j + 1][0], b[j + 1][0], b[j][0]]
                uv = [(runs[k], sd * lat), (runs[k], sd * (lat + w)), (runs[k + 1], sd * (lat + w)), (runs[k + 1], sd * lat)]
                if sd < 0:
                    quad.reverse(); uv.reverse()
                CH.quad(quad, band_mat(a[j][1], runs[k], sd, leg_n), uvs=uv, smooth=True)
                lat += w
        l0, l1, r0, r1 = rows[(k, -1)][-1][0], rows[(k + 1, -1)][-1][0], rows[(k, 1)][-1][0], rows[(k + 1, 1)][-1][0]
        CH.quad([l0, l1, r1, r0], "pr_ashlar_dark")                # the underside
        # the water: three strips across, a hair narrower than the dished bed
        pos0, f0, pos1, f1 = sta[k][0], sta[k][1], sta[k + 1][0], sta[k + 1][1]
        r0v, r1v = Vector((f0.y, -f0.x)), Vector((f1.y, -f1.x))
        for (da, db) in ((-2.36, -0.8), (-0.8, 0.8), (0.8, 2.36)):
            q = [pos0 + r0v * da, pos0 + r0v * db, pos1 + r1v * db, pos1 + r1v * da]
            ks = (k, k, k + 1, k + 1)
            WAT.quad([(p.x, p.y, zb(kk, p) + 0.005) for p, kk in zip(q, ks)], "pr_water_running",
                     uvs=[(da / 4.6 + 0.5, runs[k] / 4.6), (db / 4.6 + 0.5, runs[k] / 4.6), (db / 4.6 + 0.5, runs[k + 1] / 4.6), (da / 4.6 + 0.5, runs[k + 1] / 4.6)], smooth=True)
    for k, flip in ((0, True), (nst - 1, False)):                 # the two ends, closed
        ring = [p for p, _ in reversed(rows[(k, -1)])] + [p for p, _ in rows[(k, 1)][1:]]
        CH.quad(ring if flip else list(reversed(ring)), "pr_stone_worn")
        pos, f = sta[k][0], sta[k][1]
        r = Vector((f.y, -f.x))
        for sd in (-1, 1):                                        # a round end stone on each lip
            q = pos + r * (sd * 3.05) + f * (0.12 if flip else -0.12)
            z0 = zb(k, q)
            H = zw(k, q, sd) - z0
            ST.S.m.lathe(T(q.x, q.y, 0, rng.uniform(0, 1)), [(0.36, z0 - 0.5), (0.42, z0 + H * 0.5), (0.4, z0 + H - 0.1), (0.3, z0 + H - 0.016)], 10, "pr_stone", cap_top="pr_stone_worn")
    # boulders set in the shoulder, so it is not one long extrusion
    placed = 0
    for k in range(5, nst - 5, 9):
        pos, f = sta[k][0], sta[k][1]
        r = Vector((f.y, -f.x))
        for sd in (-1, 1):
            if cut[sd][k] and rng.random() < 0.8:
                q = pos + r * (sd * rng.uniform(4.3, 5.2))
                ST.S.r.rock_at(rng.choice(list(K.COBBLES) + list(K.SLABS) + [K.BOULDERS[2]]), q.x, q.y, zb(k, q) + rng.uniform(0.5, 1.3), rng.uniform(1.8, 3.2), rng.uniform(0, 6.28))
                placed += 1
            if rng.random() < 0.55:                               # a creeper over the lip, hanging down the OUTER face
                q = pos + r * (sd * 3.3)
                z0 = zw(k, q, sd)
                r3 = Vector((r.x, r.y, 0.0))
                ST.vine(ST.S.c, [Vector((q.x, q.y, z0 - 0.06)) - r3 * (sd * 0.2), Vector((q.x, q.y, z0 - 0.5)) + r3 * (sd * 0.16), Vector((q.x, q.y, z0 - 1.3)) + r3 * (sd * 0.3) + Vector((f.x, f.y, 0)) * 0.4], rng, 0.42, 0.045, 0.34)
    # THE AQUEDUCT: a pier under every joint the ground has left, an arch between each two that stand
    piers = []
    for i, p in enumerate(pts):
        f, s = W.side_of(pts[max(0, i - 1)], pts[min(nseg, i + 1)])
        g = min(W.ground(p.x + s.x * o, p.y + s.y * o) for o in (0.0, 3.0, -3.0))
        drop = p.z - 1.7 - g
        ok = drop > 1.1 and SO.clear(p.x, p.y, g, p.z - 1.8, 2.2, leg_n)
        if ok:
            ST.S.m.block(frame(Vector((p.x, p.y, 0)), f), 0.82, 3.5, g - 0.9, p.z - 1.66, rng.choice(SO.ASHLAR), taper=0.92)
            ST.S.m.block(frame(Vector((p.x, p.y, 0)), f), 1.05, 3.72, g - 1.0, g + 0.7, "pr_ashlar_dark")
        piers.append((ok, drop))
    arches = 0
    for i in range(nseg):
        if not (piers[i][0] and piers[i + 1][0]) or min(piers[i][1], piers[i + 1][1]) < 1.9:
            continue
        a, b = pts[i], pts[i + 1]
        f, s = W.side_of(a, b)
        span = math.hypot(b.x - a.x, b.y - a.y) / 2 - 0.8
        rise = min(span * 0.6, min(piers[i][1], piers[i + 1][1]) - 0.9, 2.4)
        low = min(a.z, b.z) - 1.72
        ST.arch(ST.S.m, Vector(((a.x + b.x) / 2, (a.y + b.y) / 2, low - rise - 0.31)), f, span, rise, 3.42, 9, rng)
        for e, q in ((-1, a), (1, b)):                             # the spandrels, up to the trough
            c = Vector(((a.x + b.x) / 2, (a.y + b.y) / 2, 0)) + f * (e * span * 0.8)
            ST.S.m.block(frame(c, f), span * 0.22, 3.3, low - rise * 0.7, (a.z + (b.z - a.z) * (0.5 + e * 0.4 * span / (span + 0.8))) - 1.68, "pr_ashlar_dark")
        arches += 1
    INFO[leg_n] = dict(stations=nst, length=round(runs[-1], 1), shoulder_cut=sum(sum(c) for c in cut.values()), shoulder_stone=2 * nst - sum(sum(c) for c in cut.values()),
                       piers=sum(1 for p in piers if p[0]), arches=arches, shoulder_rocks=placed)
    return sta, zb, zw


def sluice(leg_n, rng):
    """The old sluice gate the summit slide starts under: two stone posts, a windlass beam, the board hauled up clear."""
    leg = W.LEGS[leg_n - 1]
    m = ST.S.m
    p = leg.pts[0].lerp(leg.pts[1], 0.42)
    f, s = W.side_of(leg.pts[0], leg.pts[1])
    yaw = math.atan2(f.y, f.x)
    z = p.z
    for sd in (-1, 1):
        q = p + s * (sd * 4.75)
        ST.pier(m, q.x, q.y, z + 6.2, 0.8, None, rng, yaw=yaw, foot=min(z + 0.3, W.ground(q.x, q.y) + 0.3), force=True)
    a, b = Vector((p.x, p.y, z + 6.5)) - s * 5.8, Vector((p.x, p.y, z + 6.5)) + s * 5.8
    ST.beam(m, a, b, 0.4, 0.55, "pr_timber")
    mm, _ = ST.lay(Vector((p.x, p.y, z + 7.0)) - s * 4.0, Vector((p.x, p.y, z + 7.0)) + s * 4.0)
    m.lathe(mm @ Matrix.Rotation(math.pi / 2, 4, "Y"), [(0.3, -4.0), (0.3, 4.0)], 8, "pr_timber", cap_top="pr_plank", cap_bot="pr_plank")
    for k in range(5):
        c = Vector((p.x, p.y, 0)) + s * (-2.6 + k * 1.3)
        m.block(T(c.x, c.y, 0, yaw), 0.09, 0.63, z + 3.9 + 0.05 * (k % 2), z + 6.1, "pr_plank")
    for sd in (-1, 1):
        c = Vector((p.x, p.y, 0)) + s * (sd * 2.0)
        ST.S.r.chain((c.x, c.y, z + 6.8), (c.x, c.y, z + 6.0), link=0.5, r=0.06)
        m.block(T(c.x, c.y, 0, yaw), 0.13, 0.16, z + 4.4, z + 5.9, "pr_iron")
    m.block(frame(Vector((p.x, p.y, 0)), s), 3.1, 0.12, z + 4.6, z + 4.84, "pr_iron")
    ST.bunting(a + UP * 0.2 + f * 1.3 + s * 2.0, b + UP * 0.2 + f * 1.3 - s * 2.0, 1.0, rng)
    ST.hang_vine(Vector((p.x, p.y, z + 6.0)) + s * 4.2, 3.5, rng)


def mouths(leg_n, sta, zb, rng):
    """Where a slide ends: the water fans out over the landing it meets, and spills into a stone bowl each side."""
    m = ST.S.m
    pos, f = sta[-1][0], sta[-1][1]
    r = Vector((f.y, -f.x))
    pts_z_end = W.LEGS[leg_n - 1].pts[-1].z
    bowls = 0
    for sd in (-1, 1):
        q = pos + r * (sd * 5.0) + f * 0.6
        g = W.ground(q.x, q.y)
        if SO.on_other(q.x, q.y, W.SOLIDS[0], g + 0.5, reach=1.5, deep=3.0) or W.blocked(q.x, q.y, g, g + 1.0, 1.2, leg_n, margin=0.0):
            continue
        zr = min(g + 0.34, pts_z_end - 0.55)
        m.lathe(T(q.x, q.y, 0), [(0.9, zr - 0.9), (1.3, zr - 0.3), (1.3, zr), (1.1, zr), (1.02, zr - 0.22)], 14, "pr_stone", cap_bot=None)
        m.lathe(T(q.x, q.y, 0), [(1.04, zr - 0.5), (1.04, zr - 0.13)], 14, "pr_stone_wet", cap_top="pr_water")
        bowls += 1
    land = None
    end = Vector((pos.x, pos.y)) + f * 1.6
    for o in W.SOLIDS:
        if o.name.startswith("landing_") and o.reach(end.x, end.y) < 0.01:
            land = o
    if land is not None:
        z = SO.sheet_z(land) + 0.008
        a, b = pos + r * 2.36, pos - r * 2.36
        c, d = pos + f * 2.5 - r * 3.0, pos + f * 2.5 + r * 3.0
        e = pos + f * 3.1
        WAT.quad([(a.x, a.y, z), (d.x, d.y, z), (e.x, e.y, z), (c.x, c.y, z), (b.x, b.y, z)], "pr_water_running", uvs=[(1, 0), (1.1, 0.5), (0.5, 0.7), (-0.1, 0.5), (0, 0)])
    INFO[leg_n].update(bowls=bowls, runs_out_on=land.name if land is not None else "the pool")


def build_all():
    rng = random.Random(2231)
    for leg in W.LEGS:
        if leg.kind == "chute":
            sta, zb, zw = build(leg.i, rng)
            mouths(leg.i, sta, zb, rng)
            for i in range(len(leg.pts) - 1):
                for nm in ("chute_%d_bed_%d", "chute_%d_wall_%d_l", "chute_%d_wall_%d_r"):
                    SO.BUILT[nm % (leg.i, i)] = "chute"
    sluice(22, rng)
