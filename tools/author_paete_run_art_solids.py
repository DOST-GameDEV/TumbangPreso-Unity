"""PAETE'S RUN, EVERY WALKED SURFACE AS ART: one modelled piece for each solid of the gameplay layout (2026-10-10).

Owner, after playing on drawn collision boxes: "fix the grey showing through the art", "remove the placeholder blocks",
"do the art pass on the slide walls and decks", "theres a lot of z-fighting platforms going on".

So nothing here is placed by a stage's formula. `build_all` walks Assets/.../paete_run_layout.json's `solids` (through
author_paete_run_art_world.SOLIDS) and gives each one a piece that REPLACES its look:

  * its UPPER SURFACE is the solid's own, 1.2 cm under it (`DROP`): a body stands on the collision, never inside art;
  * WHERE TWO SOLIDS OVERLAP at one height (a deck over a landing over a plaza) only ONE surface is drawn: every walked
    top is a flat polygon with the footprints of the solids that cover it cut out (`covers`, `top_sheet`), and inlays
    (bands, rings, medallions) are cut INTO the paving, never laid on it. That is what ends the flicker;
  * its sides and underside are finished and something carries it to the ground (`carry`, `dais`, `post`): rock where
    the massif comes up under it, piers and arches where it does not, a bracket from a pier that stands clear where
    another lane passes beneath (`clear` asks the world).

The two slides are author_paete_run_art_chutes.py. The cover check that allows `hide_solids` is _check.py.
"""
import math
import random

from mathutils import Matrix, Vector

import author_paete_run_art_kit as K
import author_paete_run_art_world as W
import author_paete_run_art_stages as ST

UP = Vector((0, 0, 1))
T, frame = K.T, K.frame
DROP = 0.012                    # the art's walked top under the collision's
PAV = K.Buf("run_paving", 31)   # walked tops and their nosings: flat polygons, never bevelled
BUILT = {}                      # solid name -> the builder that drew it
NOTES = ST.NOTES
for _i, _s in enumerate(W.SOLIDS):
    _s.idx = _i
ASHLAR = ["pr_ashlar", "pr_ashlar", "pr_ashlar_dark", "pr_ashlar_rose"]
_J = [0]


def jit():
    """A height of its own for every hidden face (0 to 9 mm), so no two of them ever share a plane."""
    _J[0] = (_J[0] + 37) % 71
    return _J[0] * 0.00013


# ------------------------------------------------------------------ footprints and who covers whom
_POLY = {}


def poly_of(s, grow=None):
    key = (s.name, grow)
    if key not in _POLY:
        if s.shape == "box":
            g = 0.01 if grow is None else grow
            hl, hw = s.sz / 2 * math.hypot(s.fwd.x, s.fwd.y) + g, s.sx / 2 + g
            p = [(s.c.x + s.flat.x * a + s.side.x * b, s.c.y + s.flat.y * a + s.side.y * b) for a, b in ((-hl, -hw), (hl, -hw), (hl, hw), (-hl, hw))]
        else:
            g = 0.04 if grow is None else grow
            p = [(s.c.x + (s.r + g) * math.cos(math.tau * k / 32), s.c.y + (s.r + g) * math.sin(math.tau * k / 32)) for k in range(32)]
        _POLY[key] = K.ccw(p)
    return _POLY[key]


def rank(s):
    """Who is drawn where two walked tops meet at one height (owner: "you can literally see the rectangles intersecting
    with the middle of the circle platform"): a board, then a ROUND platform, then a plaza, then a deck. The lower wins."""
    if s.kind == "wood":
        return 0
    if s.shape != "box":
        return 1
    return 2 if s.name in ("start_plaza", "finish_platform") else 3


def beats(o, s):
    return (rank(o), -o.sx, -o.idx) < (rank(s), -s.sx, -s.idx)       # between two decks the wider wins, then the later in the course


def meets(o, s):
    return math.hypot(o.c.x - s.c.x, o.c.y - s.c.y) < o.rad + s.rad + 0.6 and bool(K.intersect(poly_of(o), poly_of(s)))


def covers(s, zt=None):
    """The footprints this solid's top is NOT drawn in: solids whose bodies stand through it, and, where two tops meet at
    one height (within 3 cm), the one that `beats` it. A round platform is also cut where a slide's bed enters it."""
    zt = s.top if zt is None else zt
    out = []
    for o in W.SOLIDS:
        if o is s:
            continue
        if o.pitched:
            if s.shape != "box" and o.kind == "teal" and "_bed_" in o.name and abs(o.c.z - zt) < 1.5 and meets(o, s):
                out.append(poly_of(o))
            continue
        if o.bot >= zt - 0.002 or o.top < zt - 0.03:
            continue
        if o.top <= zt + 0.03 and not beats(o, s):
            continue
        if math.hypot(o.c.x - s.c.x, o.c.y - s.c.y) > o.rad + s.rad + 0.6:
            continue
        out.append(poly_of(o))
    return out


def sheet_z(s):
    """The height a top is drawn at: 1.2 cm under its solid; but a round platform or a plaza that a deck 2 cm higher runs
    into is drawn IN THE DECK'S PLANE (8 mm over its own solid, inside the 1 cm allowed), so the joint is one flat floor."""
    z = s.top - DROP
    if rank(s) in (1, 2):
        up = [o.top for o in W.SOLIDS if o is not s and not o.pitched and 0.0 < o.top - s.top <= 0.021 and o.shape == "box" and meets(o, s)]
        if up:
            z = min(max(up) - DROP, s.top + 0.009)
    return z


def joints():
    """Every pair of walked solids whose tops meet at one height: (winner, loser)."""
    out = []
    flat = [s for s in W.SOLIDS if not s.pitched and s.kind in ("sand", "rock", "wood") and not s.name.startswith(("tier_", "column_", "drum_", "goat_", "giant_step", "fallen_", "ledge_25", "buttress", "shelf"))]
    for i, a in enumerate(flat):
        for b in flat[i + 1:]:
            if abs(a.top - b.top) <= 0.03 and meets(a, b):
                out.append((a, b) if beats(a, b) else (b, a))
    return out


def on_other(x, y, s, z, reach=0.2, deep=1.0):
    """Is (x, y) on or beside another solid at about height z (so a kerb or a nosing has no business there)."""
    for o in W.SOLIDS:
        if o.name == s.name or o.bot > z + 0.1 or o.top < z - deep:
            continue
        if math.hypot(o.c.x - x, o.c.y - y) < o.rad + reach + 0.1 and o.reach(x, y) < reach:
            return True
    return False


def clear(x, y, z0, z1, r, own, margin=0.5, wade=False):
    """May a support of radius r stand at (x, y) from z0 up to z1 (whatever it carries at z1 does not count)."""
    skip = tuple(o.name for o in W.SOLIDS if o.top >= z1 - 0.45 and o.reach(x, y) < r + 0.35)
    return not W.blocked(x, y, z0, z1, r, own, margin=margin, wade=wade, skip=skip)


# ------------------------------------------------------------------ walked tops
TOPS = []           # (solid, polygon, height): every piece of walked top drawn, for the junction check


def top_sheet(s, mat="pr_paving", inlays=(), z=None, poly=None, nosing=True, radial=False):
    """The solid's top as flat polygons, less what covers it. `inlays`: [(outer polygon, [(piece, material), ...])]:
    pieces that tile `outer`, set INTO the paving in its plane; an earlier inlay is cut out of a later one. `radial`: a
    round platform is paved in wedges about its own centre (its courses run round it; a deck's run along it)."""
    z = sheet_z(s) if z is None else z
    cov = covers(s)
    base = poly or poly_of(s)
    n = len(base)
    pieces = [(base, None)]
    for c in cov + [o for o, _ in inlays]:
        pieces = [(q, k) for (p, k) in pieces for q in K.subtract(p, c)]

    def emit(p, m, k):
        TOPS.append((s.name, p, z))
        gx, gy = sum(q[0] for q in p) / len(p) - s.c.x, sum(q[1] for q in p) / len(p) - s.c.y
        if not radial or math.hypot(gx, gy) < 0.3 * s.rad:
            PAV.flat(p, z, m)
            return
        # A COURSE STONE OF A ROUND PLATFORM: its own flat drawing, turned to its radius, in METRES like every deck (u along
        # the arc, v out along the radius), so the paving is the deck's size and never sheared (owner saw it smeared)
        a0 = math.atan2(gy, gx)
        tx, ty, rx, ry = -math.sin(a0), math.cos(a0), math.cos(a0), math.sin(a0)
        rm = math.hypot(gx, gy)
        uv = [((q[0] - s.c.x - gx) * tx + (q[1] - s.c.y - gy) * ty + a0 * rm + s.idx * 1.7, (q[0] - s.c.x - gx) * rx + (q[1] - s.c.y - gy) * ry + rm) for q in p]
        PAV.quad([(q[0], q[1], z) for q in p], m, uvs=uv)

    for p, k in pieces:
        emit(p, mat, k)
    for i, (outer, parts) in enumerate(inlays):
        for pp, pm in parts:
            qs = [K.intersect(K.ccw(pp), base)]
            for c in cov + [o for o, _ in inlays[:i]]:
                qs = [q for p in qs if p for q in K.subtract(p, c)]
            for q in qs:
                if q:
                    TOPS.append((s.name, q, z))
                    PAV.flat(q, z, pm)                            # an inlaid stone keeps the plain drawing in metres
    if nosing:                                                    # the paving's own edge, 7 cm deep, all the way round
        n = len(base)
        run = 0.0
        for i in range(n):
            a, b = base[i], base[(i + 1) % n]
            ln = math.hypot(b[0] - a[0], b[1] - a[1])
            PAV.quad([(a[0], a[1], z - 0.07), (b[0], b[1], z - 0.07), (b[0], b[1], z), (a[0], a[1], z)], mat,
                     uvs=[(run, z - 0.07), (run + ln, z - 0.07), (run + ln, z), (run, z)])
            run += ln


def paint_lines(s):
    """The start and finish lines are PAINT: faces of the paving itself (owner: a line that hovered over the floor)."""
    out = []
    for leg, p in ((W.LEGS[0], W.LEGS[0].A), (W.LEGS[-1], W.LEGS[-1].C)):
        if s.reach(p.x, p.y) < 4.5 and abs(s.top - p.z) < 0.1:
            q = K.ccw([(p.x + leg.f.x * a + leg.s.x * b, p.y + leg.f.y * a + leg.s.y * b) for a, b in ((-0.16, -4.0), (0.16, -4.0), (0.16, 4.0), (-0.16, 4.0))])
            out.append((q, [(q, "pr_paint_white")]))
    return out


def thresholds(s):
    """Where this deck runs into a round platform (or a plaza) at its own height: a band of larger, darker stones 0.35 m
    wide following the platform's edge across the deck, in the deck's own surface. The change of paving is designed."""
    out = []
    for o in W.SOLIDS:
        if o is s or o.pitched or abs(o.top - s.top) > 0.03 or not beats(o, s) or o.kind == "wood" or not meets(o, s):
            continue
        inner = poly_of(o)
        if o.shape == "box":
            outer = poly_of(o, 0.36)
            parts = [([inner[k], inner[(k + 1) % 4], outer[(k + 1) % 4], outer[k]], "pr_stone_worn") for k in range(4)]
        else:
            outer = poly_of(o, 0.39)
            parts = [([inner[k], inner[(k + 1) % 32], outer[(k + 1) % 32], outer[k]], "pr_stone_worn" if k % 2 else "pr_ashlar_dark") for k in range(32)]
        out.append((outer, parts))
    return out


def band(s, al, half=0.2, inset=0.5, mat="pr_ashlar_rose"):
    """An inlaid band across a box, `al` metres along it."""
    hw = s.sx / 2 - inset
    p = [s.at(al + a, b) for a, b in ((-half, -hw), (half, -hw), (half, hw), (-half, hw))]
    p = K.ccw([(q.x, q.y) for q in p])
    return (p, [(p, mat)])


def ring(x, y, r0, r1, mats, n=32, ph=0.0, core=None):
    """An inlaid ring (or, with `core`, a whole disc): n stones round, their materials taken in turn from `mats`."""
    pt = lambda r, k: (x + r * math.cos(math.tau * k / n + ph), y + r * math.sin(math.tau * k / n + ph))
    parts = [([pt(r0, k), pt(r1, k), pt(r1, k + 1), pt(r0, k + 1)], mats[k % len(mats)]) for k in range(n)]
    outer = K.ccw([pt(r1, k) for k in range(n)])
    return outer, parts, K.ccw([pt(r0, k) for k in range(n)])


def ring_inlay(x, y, r0, r1, mats, inner_mat="pr_paving", n=32, ph=0.0):
    outer, parts, core = ring(x, y, r0, r1, mats, n, ph)
    return (outer, parts + [(core, inner_mat)])


def medallion(x, y, R, style=0, n=32):
    """A mosaic floor medallion: a sun of wedge stones in rings, cut into the paving."""
    pal = [["pr_paint_red", "pr_paint_yellow"], ["pr_paint_teal", "pr_paint_white"], ["pr_paint_yellow", "pr_paint_teal"]][style % 3]
    parts = []
    rings = [(0.0, 0.2, [pal[1]]), (0.2, 0.28, ["pr_paint_white"]), (0.28, 0.62, [pal[0], pal[0], "pr_stone", "pr_stone"]),
             (0.62, 0.7, ["pr_paint_black" if style == 2 else "pr_ashlar_dark"]), (0.7, 0.9, [pal[1], "pr_paving", pal[0], "pr_paving"]), (0.9, 1.0, ["pr_paint_white"])]
    outer = None
    for (a, b, mats) in rings:
        if a == 0.0:
            parts.append((K.ccw([(x + R * b * math.cos(math.tau * k / n), y + R * b * math.sin(math.tau * k / n)) for k in range(n)]), mats[0]))
            continue
        outer, pp, _ = ring(x, y, R * a, R * b, mats, n, 0.0)
        parts += pp
    return (outer, parts)


# ------------------------------------------------------------------ what carries a piece
def stand(x, y, top, half, own, rng, round_=False, wade=False, **kw):
    """A pier (or a column) from the ground to `top`, if nothing forbids it. False if it may not stand."""
    g = W.ground_min(x, y, half)
    if top - g < 0.5:
        return True
    if not clear(x, y, g, top, half, own, wade=wade):
        return False
    if round_:
        ST.column(ST.S.m, x, y, top, half, None, rng, force=True, **kw)
    else:
        ST.pier(ST.S.m, x, y, top, half, None, rng, force=True, **kw)
    return True


def bracket(x, y, zb, s, rng, r=1.0):
    """Nothing may stand under (x, y): a pier stands clear to one side and a stone arm and half an arch reach in."""
    best = W.find_clear(Vector((x, y, 0)), 0, zb + 0.8, r, s.own, skip=(s.name,))
    if best is None:
        NOTES.append("%s at (%.0f, %.0f, %.0f): nowhere clear to stand a support; it is carried by its neighbours only" % (s.name, x, y, zb))
        return False
    q, dist = best
    ST.pier(ST.S.m, q.x, q.y, zb - 0.5, r, None, rng, force=True)      # it ends well under the deck: nothing beside a deck reads as floor
    d = (Vector((x, y, 0)) - Vector((q.x, q.y, 0))).normalized()
    ST.beam(ST.S.m, Vector((q.x, q.y, zb - 0.02)), Vector((x, y, zb - 0.02)) + d * 0.6, 0.62, 0.8, "pr_ashlar_dark")
    rise = min(3.0, dist * 0.55)
    ST.arch(ST.S.m, Vector((x, y, zb - 0.9 - rise)), -d, dist - 0.4, rise, 0.6, 7, rng, skip=(4, 5, 6))
    return True


def carry(s, rng, pw=0.8, inset=0.95):
    """Piers, and arches between them, under a level box wherever the ground falls away from its underside."""
    zb, half, L = s.bot, s.sx / 2, s.sz
    n = 0 if L < 7.5 else max(1, round((L - 1.8) / 6.5))
    offs = (-(half - inset), half - inset) if half > 1.9 else (0.0,)
    sts = []
    for k in range(n + 1):
        al = 0.0 if n == 0 else -L / 2 + 0.9 + (L - 1.8) * k / n
        drop = zb - min(W.ground(*s.at(al, off).xy) for off in (0.0, half * 0.7, -half * 0.7))
        got = 0
        if drop > 1.0:
            for off in offs:
                q = s.at(al, off)
                if clear(q.x, q.y, W.ground_min(q.x, q.y, pw), zb, pw, s.own):
                    ST.pier(ST.S.m, q.x, q.y, zb + 0.022 + jit(), pw if len(offs) > 1 else min(pw, half - 0.05), None, rng, yaw=s.yaw, cap=False, force=True)
                    got += 1
            if not got:
                q = s.at(al, 0.0)
                got = -1 if bracket(q.x, q.y, zb, s, rng) else -2
        sts.append((al, got, drop))
    for (u, gu, du), (v, gv, dv) in zip(sts, sts[1:]):
        if gu <= 0 or gv <= 0 or min(du, dv) < 1.6:
            continue
        mid = s.at((u + v) / 2, 0.0)
        span = (v - u) / 2 - 0.75
        rise = min(span * 0.62, max(du, dv) - 0.9, 3.4)
        if rise < 0.8 or not clear(mid.x, mid.y, zb - rise - 0.7, zb, 1.0, s.own):
            continue
        ST.arch(ST.S.m, Vector((mid.x, mid.y, zb - rise - 0.34)), s.flat, span, rise, half - 0.55, 9, rng)
        for sd in (-1, 1):
            q = mid + s.flat * (sd * span * 0.78)
            ST.S.m.block(frame(Vector((q.x, q.y, 0)), s.flat), span * 0.24, half - 0.7, zb - rise * 0.72, zb + 0.012 + jit(), "pr_ashlar_dark")
    return sts


def kerbs(s, rng, sides=(-1, 1), miss=0.12, deep=None):
    """Kerb stones along a box's long sides: each its own block, outside the footprint, a little under the paving."""
    m = ST.S.m
    for sd in sides:
        al = -s.sz / 2 + 0.04
        while al < s.sz / 2 - 0.5:
            ln = min(rng.uniform(1.3, 3.1), s.sz / 2 - 0.04 - al)
            mid = s.at(al + ln / 2, sd * (s.sx / 2 + 0.115))
            if not on_other(mid.x, mid.y, s, s.top, reach=0.3):
                g = W.ground(mid.x, mid.y)
                low = max(min(g - 0.5, s.top - 0.7), s.top - (deep or 2.4))
                if rng.random() > miss:
                    m.block(T(mid.x, mid.y, 0, s.yaw + rng.uniform(-0.015, 0.015)), ln / 2 - 0.02, 0.11, low, s.top - rng.uniform(0.12, 0.24),
                            rng.choice(["pr_ashlar", "pr_ashlar", "pr_ashlar_dark", "pr_stone", "pr_stone_worn"]), taper=0.985)
                elif rng.random() < 0.6:                          # a broken one: what is left of it, lower, leaning out
                    zc = s.top - rng.uniform(0.32, 0.6)
                    m.block(T(mid.x, mid.y, zc, s.yaw + rng.uniform(-0.1, 0.1)) @ Matrix.Rotation(sd * 0.1, 4, "X"), ln * 0.3, 0.1, low - zc, 0.0, "pr_stone_worn", taper=0.9)
            al += ln


def slab(s, mat="pr_ashlar", inset=0.02, bot=None):
    j = jit()
    ST.S.m.block(frame(Vector((s.c.x, s.c.y, 0)), s.flat), s.sz / 2 - inset - j, s.sx / 2 - inset - j, (s.bot if bot is None else bot) - 0.003 - j, s.top - 0.03 - 0.0024 * (s.idx % 7), mat)


# ------------------------------------------------------------------ the decks
def deck(s, rng):
    inl = []
    if s.name == "start_plaza":
        inl.append(medallion(s.c.x - 7.2, s.c.y, 3.3, 0))
        inl += [ring_inlay(s.c.x - 7.2 + dx, s.c.y + dy, 1.0, 1.35, ["pr_paint_teal", "pr_paint_white"], "pr_paint_yellow", 16) for dx, dy in ((0, 8.4), (0, -8.4))]
    elif s.name == "finish_platform":
        inl.append(medallion(s.c.x, s.c.y, 3.6, 2))
    elif s.name.startswith("deck_21"):
        inl.append(medallion(s.c.x, s.c.y, 2.9, 1))
        inl += [band(s, a, 0.2) for a in (-9.5, -5.2, 5.2, 9.5)]
    elif s.sz > 9 and s.sx >= 5 and not s.name.startswith(("deck_1_", "deck_36")):
        k = max(1, int(s.sz / 7.5))
        inl += [band(s, -s.sz / 2 + s.sz * (i + 0.5) / k + rng.uniform(-0.6, 0.6), rng.choice((0.16, 0.22, 0.3)), 0.45,
                     rng.choice(["pr_ashlar_rose", "pr_ashlar_dark", "pr_stone_worn"])) for i in range(k)]
    if s.name.startswith("deck_1_") or s.name.startswith("deck_36"):          # chalked distance marks down the two straights
        for a in range(int(-s.sz / 2) + 4, int(s.sz / 2) - 1, 5):
            for sd in (-1, 1):
                p = [s.at(a + u, sd * (s.sx / 2 - 0.35 - v)) for u, v in ((-0.06, 0), (0.06, 0), (0.06, 0.7), (-0.06, 0.7))]
                p = K.ccw([(q.x, q.y) for q in p])
                inl.append((p, [(p, "pr_paint_white")]))
    for p in W.LAYOUT["pads"]:                                    # a geyser's vent is set into the deck it throws from
        q = W.B(p["position"])
        if s.reach(q.x, q.y) < 0.01 and abs(q.z - s.top) < 0.4:
            o1, p1, c1 = ring(q.x, q.y, 1.25, 1.7, ["pr_stone_stain", "pr_stone_worn", "pr_stone_stain"], 18)
            o2, p2, c2 = ring(q.x, q.y, 0.8, 1.25, ["pr_stone_wet"], 18)
            inl.append((o1, p1 + p2 + [(c2, "pr_water")]))
    top_sheet(s, "pr_paving", paint_lines(s) + thresholds(s) + inl)
    slab(s)
    plaza = s.name in ("start_plaza", "finish_platform")
    kerbs(s, rng, miss=0.05 if plaza else 0.12)
    if plaza:                                                     # the two ends are kerbed as well
        e = W.Sol(dict(s.d, s=dict(x=s.sz, y=s.sy, z=s.sx), yaw=s.d.get("yaw", 0.0) + 90.0)); e.idx = s.idx
        kerbs(e, rng, miss=0.05)
    carry(s, rng)


def bridge(s, rng):
    """A span of the broken bridge: a thick deck on one pier, corbels under it, its broken ends ragged."""
    top_sheet(s, "pr_paving", thresholds(s) + [band(s, 0.0, 0.25, 0.4, "pr_ashlar_dark")] + [band(s, e * (s.sz / 2 - 0.34), 0.13, 0.3, "pr_paint_red") for e in (-1, 1)])
    slab(s)
    kerbs(s, rng, miss=0.25, deep=1.3)
    m = ST.S.m
    # what says BRIDGE from the racer's eye: a leaning timber post at each corner, a sagging rope between them, the rope
    # torn at the break and hanging into the gap, the deck's last slab snapped and tipped down into it
    for sd in (-1, 1):
        tops = []
        for e in (-1, 1):
            q = s.at(e * (s.sz / 2 - 0.35), sd * (s.sx / 2 + 0.34))
            t = Vector((q.x, q.y, s.top + rng.uniform(1.15, 1.45))) + s.flat * (e * rng.uniform(0.0, 0.22)) + s.side * (sd * rng.uniform(0.0, 0.12))
            m.tube([Vector((q.x, q.y, s.bot - 0.5)), t], 0.11, "pr_timber", n=6, r1=0.085)
            m.lathe(T(t.x, t.y, 0), [(0.12, t.z - 0.02), (0.15, t.z + 0.08), (0.06, t.z + 0.2)], 6, ["pr_paint_red", "pr_paint_yellow", "pr_paint_teal"][(s.idx + e + sd) % 3], cap_top="pr_timber")
            tops.append(t - UP * 0.22)
            g = Vector((t.x, t.y, t.z - 0.3)) + s.flat * (e * 0.5)
            ST.S.r.tube([t - UP * 0.25, g - UP * 0.5, g + s.flat * (e * 0.25) - UP * rng.uniform(1.6, 2.6)], 0.035, "pr_rope", n=4)        # the torn end
        ST.S.r.tube([tops[0].lerp(tops[1], k / 6) - UP * (0.32 * math.sin(math.pi * k / 6)) for k in range(7)], 0.035, "pr_rope", n=4)
    for e in (-1, 1):
        q = s.at(e * (s.sz / 2 + 0.02), 0)
        if not on_other(q.x + s.flat.x * e * 0.7, q.y + s.flat.y * e * 0.7, s, s.top, 0.5, 2.0):
            M = frame(Vector((q.x, q.y, s.top - 0.1)), s.flat * e) @ Matrix.Rotation(rng.uniform(0.45, 0.75), 4, "Y")
            m.block(M @ T(0.5, rng.uniform(-0.5, 0.5), 0), 0.55, s.sx / 2 - rng.uniform(0.7, 1.3), -0.42, -0.02, "pr_ashlar", top="pr_paving")
    mid = s.at(0, 0)
    if not stand(mid.x, mid.y, s.bot + 0.03 + jit(), 1.2, s.own, rng, yaw=s.yaw, cap=False):
        for sd in (-1, 1):
            q = s.at(0, sd * (s.sx / 2 + 0.9))
            if not stand(q.x, q.y, s.bot + 0.5, 0.8, s.own, rng, yaw=s.yaw):
                bracket(q.x, q.y, s.bot, s, rng)
    for sd in (-1, 1):
        for j in range(2):
            q = s.at(sd * (1.31 + j * 0.57 + 0.013 * (s.idx % 5)), 0)
            m.block(frame(Vector((q.x, q.y, 0)), s.flat), 0.36, s.sx / 2 - 0.25 - j * 0.3, s.bot - 0.7 + j * 0.3, s.bot + 0.008 + jit(), "pr_ashlar_dark")
        for j in range(3):                                        # the stumps of the span that fell
            q = s.at(sd * (s.sz / 2 + 0.2 + rng.uniform(0.0, 0.12)), rng.uniform(-s.sx / 2 + 0.6, s.sx / 2 - 0.6))
            m.block(T(q.x, q.y, 0, s.yaw + rng.uniform(-0.4, 0.4)), 0.3, rng.uniform(0.4, 1.0), s.bot + rng.uniform(0, 0.3), s.top - rng.uniform(0.42, 0.75),
                    rng.choice(["pr_ashlar", "pr_ashlar_dark", "pr_stone_worn"]))


def ramp(s, rng):
    """The slab that climbs out of a pool: its pitched top, its body, a stone cheek each side."""
    hx, hy, hz = s.sz / 2, s.sx / 2, s.sy / 2
    PAV.quad([s.M @ Vector(q) for q in ((-hx, -hy - 0.01, hz - DROP), (hx, -hy - 0.01, hz - DROP), (hx, hy + 0.01, hz - DROP), (-hx, hy + 0.01, hz - DROP))], "pr_paving")
    ST.S.m.block(s.M, hx - 0.02, hy - 0.02, -hz - 0.9, hz - 0.04, "pr_ashlar_dark")
    for sd in (-1, 1):
        for k in range(3):
            a = -hx + (k + 0.5) * s.sz / 3
            ST.S.m.block(s.M @ T(a, sd * (hy + 0.115), 0, rng.uniform(-0.02, 0.02)), s.sz / 6 - 0.02, 0.11, -hz - 1.2, hz - rng.uniform(0.12, 0.22), rng.choice(ASHLAR))


# ------------------------------------------------------------------ round platforms
def dais(s, rng):
    x, y, zt, r = s.c.x, s.c.y, s.top, s.r
    m = ST.S.m
    inl = []
    if r >= 2.5:
        inl.append(ring_inlay(x, y, r * 0.60, r * 0.70, [["pr_ashlar_rose"], ["pr_ashlar_dark"], ["pr_stone_worn", "pr_ashlar_rose"]][s.idx % 3]))
    top_sheet(s, "pr_paving", paint_lines(s) + inl)      # ONE plain paving UV in world metres on every walked top: no turned courses (owner)
    g = W.ground_min(x, y, r * 0.85) - 0.8
    mats = [rng.choice(ASHLAR) for _ in range(3)]
    tower = zt - g < 3.4 or (s.h > 2.0 and (s.bot - g < 0.6 or clear(x, y, g + 0.8, s.bot, r * 0.9, s.own, wade=True)))
    if s.h > 2.0 or zt - g < 3.4:
        z, k = min(g, s.bot) if tower else s.bot, 0               # a drum wall, in courses, down into the ground (if a tower may stand there)
        top_j = 0.02 + 0.0024 * (s.idx % 7)
        while z < zt - 0.035 - top_j - 0.0005:
            dh = min(rng.uniform(1.1, 1.9), zt - 0.035 - top_j - z)
            if zt - 0.035 - top_j - (z + dh) < 0.6:
                dh = zt - 0.035 - top_j - z
            fat = 0.035 * max(0.0, s.bot - z)                     # below the solid itself the wall batters out
            m.lathe(T(x, y, 0, rng.uniform(0, 0.2)), [(r - 0.02 + fat + 0.03, z), (r - 0.02 + fat, z + dh)], 24, mats[k % 3], cap_top="pr_ashlar_dark")
            z += dh; k += 1
        if not tower:                                             # another lane runs beneath: a corbelled foot, carried from the side
            m.lathe(T(x, y, 0), [(r * 0.45, s.bot - 1.5), (r * 0.8, s.bot - 0.7), (r - 0.05, s.bot + 0.02)], 24, "pr_ashlar_dark", cap_bot="pr_ashlar_dark")
            for a in (0.0, math.pi):
                bracket(x + math.cos(a + s.idx) * r * 0.5, y + math.sin(a + s.idx) * r * 0.5, s.bot - 0.6, s, rng, 1.1)
    else:                                                         # a thin disc in the air: a corbelled slab on a stout column
        m.lathe(T(x, y, 0), [(r * 0.5, zt - 1.3), (r - 0.3, zt - 0.74), (r - 0.02, zt - 0.6), (r - 0.02, zt - 0.035)], 24, mats[0], cap_top="pr_ashlar_dark", cap_bot="pr_ashlar_dark")
        rc = min(1.3, r * 0.28)
        if not stand(x, y, zt - 1.28, rc, s.own, rng, round_=True, cap_r=r * 0.52, n=14):
            bracket(x, y, zt - 1.3, s, rng, 1.0)
    n = max(8, int(math.tau * r / 1.25))
    ph = rng.uniform(0, 1)
    for k in range(n):                                            # the kerb ring
        a = math.tau * (k + 0.5) / n + ph
        cx, cy = x + (r + 0.13) * math.cos(a), y + (r + 0.13) * math.sin(a)
        if rng.random() < 0.08 or on_other(cx, cy, s, zt, reach=0.3, deep=2.2):
            continue
        m.block(T(cx, cy, 0, a + math.pi / 2), math.pi * (r + 0.13) / n - 0.03, 0.11, zt - rng.uniform(0.7, 1.0), zt - rng.uniform(0.12, 0.24),
                rng.choice(["pr_ashlar", "pr_ashlar_dark", "pr_stone", "pr_stone_worn"]), taper=0.97)


# ------------------------------------------------------------------ the pools
def pool_floor(s, rng):
    top_sheet(s, "pr_pool_floor", [band(s, a, 0.3, 1.2, "pr_stone_wet") for a in (-s.sz * 0.3, 0.0, s.sz * 0.3)], nosing=False)
    slab(s, "pr_ashlar_dark")


def pool_rim(s, rng):
    """A basin's rim: a wall with a worn coping, every coping stone its own, none quite level with the next."""
    m = ST.S.m
    m.block(frame(Vector((s.c.x, s.c.y, 0)), s.flat), s.sz / 2 - 0.01, s.sx / 2 - 0.006, s.bot - 0.3, s.top - 0.3, "pr_ashlar_dark")
    al = -s.sz / 2
    k = 0
    while al < s.sz / 2 - 0.01:
        ln = min(rng.uniform(1.5, 2.7), s.sz / 2 - al)
        if s.sz / 2 - (al + ln) < 0.9:
            ln = s.sz / 2 - al
        if any(abs(al + ln - e * 0.3 * s.sz) < 0.3 for e in (-1, 0, 1)):
            ln = min(ln + 0.62, s.sz / 2 - al)
        q = s.at(al + ln / 2, 0)
        m.block(T(q.x, q.y, 0, s.yaw), ln / 2 - 0.006, s.sx / 2 + 0.07, s.top - 0.34, s.top - 0.006 - 0.007 * ((k * 5) % 3),
                ["pr_stone", "pr_stone_worn", "pr_stone", "pr_ashlar"][k % 4], top="pr_stone_worn" if k % 5 == 2 else None)
        if k % 6 == 3:                                            # a stone that slid off, lying at the foot outside
            w = s.at(al + ln / 2, (1 if k % 4 else -1) * 1.1)
            if not on_other(w.x, w.y, s, s.top, 0.5, 3.0) and W.ground(w.x, w.y) > s.top - 1.2:
                m.block(T(w.x, w.y, W.ground(w.x, w.y), s.yaw + 0.5) @ Matrix.Rotation(0.2, 4, "Y"), 0.5, 0.36, -0.2, 0.22, "pr_stone_worn")
        al += ln; k += 1


# ------------------------------------------------------------------ what a body hops on
def hop_column(s, rng):
    """A standing column: the body lands on its capital."""
    if not clear(s.c.x, s.c.y, W.ground_min(s.c.x, s.c.y, 0.8), s.bot, 0.8, s.own):
        NOTES.append("LAYOUT: %s at (%.0f, %.0f, %.0f) stands in another leg's lane (built as the layout has it)" % (s.name, s.c.x, s.c.y, s.top))
    ST.column(ST.S.m, s.c.x, s.c.y, s.top - DROP, s.r * 0.72, None, rng, cap_r=s.r + 0.035, n=14, force=True)
    if s.idx % 3 == 0:
        ST.hang_vine((s.c.x + s.r * 0.9, s.c.y + 0.5, s.top - 0.35), 2.4 + (s.idx % 2), rng)


def fallen(s, rng):
    """A fallen architrave lying across the lane on a column's stump."""
    m = ST.S.m
    M = frame(Vector((s.c.x, s.c.y, 0)), s.flat)
    m.block(M, s.sz / 2 - 0.01, s.sx / 2 + 0.02, s.bot, s.top - DROP, "pr_stone", top="pr_paving")
    for sd in (-1, 1):                                            # its mouldings, proud of the two long faces
        m.block(M @ T(sd * (s.sz / 2 + 0.03), 0, 0), 0.06, s.sx / 2 - 0.25, s.top - 0.42, s.top - 0.2, "pr_stone_worn")
        m.block(M @ T(sd * (s.sz / 2 + 0.02), 0, 0), 0.05, s.sx / 2 - 0.1, s.bot + 0.06, s.bot + 0.3, "pr_ashlar_dark")
    e = rng.choice((-1, 1))
    q = s.at(0, e * (s.sx / 2 + 0.3))
    m.block(T(q.x, q.y, s.bot, s.yaw + 0.5) @ Matrix.Rotation(0.3, 4, "X"), 0.5, 0.42, 0.1, s.sy - 0.3, "pr_stone_worn", taper=0.8)      # the broken end


def drum_stack(s, rng):
    """Stone drums stacked to a wild height, a painted band under the rim."""
    m = ST.S.m
    x, y, top, r = s.c.x, s.c.y, s.top - DROP, s.r + 0.03
    z = min(W.ground_min(x, y, r) - 0.8, s.bot)
    j = 0
    while z < top - 0.05:
        dh = min(rng.uniform(1.0, 1.7), top - z)
        if top - (z + dh) < 0.6:
            dh = top - z
        last = z + dh >= top - 0.01
        rr = r * (1.0 if last else rng.uniform(1.0, 1.08))
        m.lathe(T(x + (0 if last else rng.uniform(-0.05, 0.05)), y + (0 if last else rng.uniform(-0.05, 0.05)), 0, rng.uniform(0, 1)),
                [(rr, z), (rr, z + dh)], 16, ["pr_stone", "pr_ashlar", "pr_stone_worn"][(j + s.idx) % 3], cap_top="pr_paving")
        if last:
            m.lathe(T(x, y, 0), [(rr + 0.02, top - 0.62), (rr + 0.02, top - 0.3)], 16, ["pr_paint_red", "pr_paint_yellow", "pr_paint_teal", "pr_paint_white"][s.idx % 4])
        z += dh; j += 1


def needle(s, rng):
    """The Goat Track's hops: a thin leaning stack of rough-hewn stones, a flat cap for the foot."""
    m = ST.S.m
    x, y, top, r = s.c.x, s.c.y, s.top - DROP, s.r + 0.03
    g = min(W.ground_min(x, y, 1.0) - 0.6, s.bot)
    if not clear(x, y, g + 0.6, s.bot, 0.7, s.own):
        NOTES.append("LAYOUT: the Goat Track's hop %s at (%.0f, %.0f, %.0f) stands in another leg's lane or under its swing (built as the layout has it)" % (s.name, x, y, s.top))
    z, j = g, 0
    while z < top - 0.31:
        dh = min(rng.uniform(1.0, 2.1), top - 0.3 - z)
        if top - 0.3 - (z + dh) < 0.7:
            dh = top - 0.3 - z
        fat = 1.0 + 0.5 * max(0.0, (s.bot - z)) / max(1.0, s.bot - g)
        rr = r * 0.86 * fat * rng.uniform(0.92, 1.06)
        m.lathe(T(x + rng.uniform(-0.06, 0.06), y + rng.uniform(-0.06, 0.06), 0, rng.uniform(0, 1)), [(rr * 1.04, z), (rr * 0.95, z + dh)], 6,
                ["pr_stone_worn", "pr_ashlar_dark", "pr_stone"][(j + s.idx) % 3], cap_top="pr_stone_worn", smooth=False)
        z += dh; j += 1
    m.lathe(T(x, y, 0, rng.uniform(0, 1)), [(r * 0.8, top - 0.34), (r, top - 0.2), (r, top)], 12, "pr_stone", cap_top="pr_paving", cap_bot="pr_stone_worn")
    if s.idx % 2 == 0:
        ST.S.r.rock_at(K.COBBLES[s.idx % 3], x + rng.uniform(-0.7, 0.7), y + rng.uniform(-0.7, 0.7), W.ground(x, y) + 0.5, rng.uniform(1.5, 2.3), rng.uniform(0, 6))


def giant_step(s, rng):
    m = ST.S.m
    k = s.idx
    top_sheet(s, "pr_paving")
    g = min(W.ground_min(s.c.x, s.c.y, 3.0) - 0.7, s.bot)
    M = frame(Vector((s.c.x, s.c.y, 0)), s.flat)
    j = jit()
    m.block(M, s.sz / 2 - 0.02 - j, s.sx / 2 - 0.02 - j, g + 0.9, s.top - 0.075 - 0.0024 * (k % 7), ASHLAR[k % 4])
    m.block(M, s.sz / 2 - 0.07, s.sx / 2 + 0.2 + 0.03 * (k % 3), g - 10 * jit(), min(g + 1.6, s.top - 1.2) + 10 * jit(), "pr_ashlar_dark")
    for sd in (-1, 1):                                            # a painted band on each end face
        m.block(M @ T(0, sd * (s.sx / 2 - 0.02), 0), s.sz / 2 - 0.3, 0.035, s.top - 1.0, s.top - 0.68, "pr_paint_teal" if k % 2 else "pr_paint_red")


def island(s, rng):
    """A take-off island: one great stone on a pier, its edges chipped."""
    m = ST.S.m
    top_sheet(s, "pr_paving", [ring_inlay(s.c.x, s.c.y, min(s.sx, s.sz) * 0.26, min(s.sx, s.sz) * 0.34, ["pr_paint_yellow", "pr_paint_teal"], n=16)])
    M = frame(Vector((s.c.x, s.c.y, 0)), s.flat)
    m.block(M, s.sz / 2 - 0.02, s.sx / 2 - 0.02, s.bot + 0.45, s.top - 0.035, "pr_stone")
    m.block(M, s.sz / 2 - 0.3, s.sx / 2 - 0.3, s.bot - 0.004, s.bot + 0.47, "pr_stone_worn", taper=1.4 if False else 1.0)
    for sx_, sy_ in ((-1, -1), (1, 1), (1, -1)):                    # corner stones, lower: the worn edge
        if rng.random() < 0.7:
            q = s.at(sx_ * (s.sz / 2 + 0.12), sy_ * (s.sx / 2 - 0.6))
            m.block(T(q.x, q.y, 0, s.yaw + rng.uniform(-0.3, 0.3)), 0.22, 0.5, s.bot + 0.5, s.top - rng.uniform(0.2, 0.5), "pr_stone_worn", taper=0.85)


def bastion(s, rng):
    """A tall shelf of the Chimney: a stepped tower of courses with a paved top."""
    m = ST.S.m
    top_sheet(s, "pr_paving", [band(s, 0.0, 0.25, 0.4, "pr_ashlar_rose")] if s.sz > 6 else ())
    g = min(W.ground_min(s.c.x, s.c.y, 3.0) - 0.8, s.bot)
    M = frame(Vector((s.c.x, s.c.y, 0)), s.flat)
    n = max(2, round((s.top - g) / 2.6))
    for k in range(n):
        z0, z1 = g + (s.top - g) * k / n, g + (s.top - g) * (k + 1) / n
        fat = 0.07 * (n - 1 - k)
        m.block(M, s.sz / 2 - 0.02 + fat, s.sx / 2 - 0.02 + fat, z0, z1 - (0.095 + 0.0024 * (s.idx % 7) if k == n - 1 else 0.0), ASHLAR[(k + s.idx) % 4])
    kerbs(s, rng, miss=0.3, deep=0.9)
    for k in range(3):
        q = s.at(rng.uniform(-s.sz / 2, s.sz / 2), rng.choice((-1, 1)) * (s.sx / 2 + 0.05))
        ST.hang_vine((q.x, q.y, s.top - 0.3), rng.uniform(2.5, 5.0), rng)


def shelf(s, rng, cliff):
    """A wall ledge: a slab let into the cliff on two corbels and a strut."""
    m = ST.S.m
    M = frame(Vector((s.c.x, s.c.y, 0)), s.flat)
    m.block(M, s.sz / 2 - 0.01, s.sx / 2 - 0.01, s.bot, s.top - DROP, "pr_stone" if s.idx % 2 else "pr_ashlar", top="pr_paving")
    sd = 1.0 if (cliff.c - s.c).dot(s.side) > 0 else -1.0
    dist = abs((cliff.c - s.c).dot(s.side))
    for e in (-1, 1):
        a = s.at(e * s.sz * 0.28, -sd * (s.sx / 2 - 0.25), s.bot + 0.02)
        b = s.at(e * s.sz * 0.28, sd * (dist + 0.2), s.bot + 0.02)
        ST.beam(m, a, b, 0.2, 0.4, "pr_ashlar_dark")
        ST.S.m.tube([s.at(e * s.sz * 0.28, -sd * (s.sx / 2 - 0.5), s.bot - 0.36), s.at(e * s.sz * 0.28, sd * (dist - 0.45), s.bot - 2.0)], 0.12, "pr_timber", n=6)


def cliff_wall(s, rng):
    """The quarried face the ledges step down: courses of great blocks, each its own, the top a coping."""
    m = ST.S.m
    nb = max(5, round(s.sz / 4.6)) | 1                             # an odd count: no joint under the wall's middle
    g = min(W.ground_min(s.c.x, s.c.y, 2.0) - 1.0, s.bot)
    for k in range(nb):
        al = -s.sz / 2 + s.sz * (k + 0.5) / nb
        q = s.at(al, 0)
        g2 = min(g, W.ground(q.x, q.y) - 1.0)
        nc = max(3, round((s.top - g2) / 3.3))
        for j in range(nc):
            z0, z1 = g2 + (s.top - g2) * j / nc, g2 + (s.top - g2) * (j + 1) / nc
            m.block(T(q.x, q.y, 0, s.yaw), s.sz / nb / 2 - 0.004, s.sx / 2 - 0.004 - 0.002 * ((k + j) % 3), z0, z1 - (DROP if j == nc - 1 else 0.0),
                    ["pr_ashlar", "pr_ashlar_dark", "pr_ashlar_rose", "pr_ashlar"][(k + j) % 4], top="pr_stone" if j == nc - 1 else None)
        if k % 3 == 1:
            ST.hang_vine(s.at(al, -0.56, s.top - 0.2), 4.5, rng); ST.hang_vine(s.at(al + 1.2, 0.56, s.top - 0.2), 3.0, rng)


# ------------------------------------------------------------------ posts
def post(s, rng):
    m = ST.S.m
    x, y = s.c.x, s.c.y
    n = s.name
    g = W.ground_min(x, y, s.r) - 0.9
    if n.startswith(("arch_",)):
        return                                                    # the gate that carries its anchor builds it
    if n.startswith("spur_"):
        # THE SPUR: an eight-sided tower of courses, boulders heaped at its foot
        z, k = min(g, s.bot), 0
        while z < s.top - DROP - 0.01:
            dh = min(rng.uniform(1.8, 2.8), s.top - DROP - z)
            if s.top - DROP - (z + dh) < 1.0:
                dh = s.top - DROP - z
            fat = 1.0 + 0.5 * max(0.0, s.bot - 1.0 - z) / max(1.0, s.bot - 1.0 - g)
            m.lathe(T(x, y, 0, math.pi / 8), [(s.r * fat * 1.07 + 0.03, z), (s.r * fat * 1.07, z + dh)], 8, ASHLAR[(k + s.idx) % 4], cap_top="pr_stone", smooth=False)
            z += dh; k += 1
        m.lathe(T(x, y, 0, math.pi / 8), [(s.r * 1.3, s.top - 1.5), (s.r * 1.3, s.top - 1.15)], 8, "pr_stone", cap_top="pr_stone", cap_bot="pr_stone", smooth=False)
        for k in range(5):
            a = math.tau * k / 5 + s.idx
            if on_other(x + math.cos(a) * 2.2, y + math.sin(a) * 2.2, s, W.ground(x + math.cos(a) * 2.2, y + math.sin(a) * 2.2) + 1.0, reach=2.0, deep=4.0):
                continue
            ST.S.r.rock_at(rng.choice(list(K.BOULDERS) + list(K.COBBLES)), x + math.cos(a) * 2.2, y + math.sin(a) * 2.2, W.ground(x + math.cos(a) * 2.2, y + math.sin(a) * 2.2) + rng.uniform(0.8, 1.9),
                           rng.uniform(2.4, 3.6), rng.uniform(0, 6))
        return
    cap = 1.5 if n.startswith("island_post") else 0.62 if n.startswith("wheel_post") else 0.72 if n.startswith("fallen_post") else None
    if n.startswith("seesaw_post"):
        ST.pier(m, x, y, s.top - DROP - 0.02, s.r + 0.02, None, rng, yaw=rng.uniform(0, 1.5), cap=True, force=True)
        return
    ST.column(m, x, y, s.top - DROP, s.r, None, rng, cap_r=cap, mat="pr_stone_worn" if s.idx % 2 else "pr_stone", n=12 if s.r > 0.6 else 9, force=True)


def gate_of(s, rng):
    """A vine's anchor over the lane: a lintel on the two posts the layout stands beside it (or a boom off a spur)."""
    m = ST.S.m
    base = s.name[len("anchor_"):]
    pl, pr = W.BY.get("arch_%s_l" % base), W.BY.get("arch_%s_r" % base)
    M = frame(Vector((s.c.x, s.c.y, 0)), s.flat)
    top = s.top - DROP
    stone = not s.tag.startswith("s")
    if pl is None:                                                # the long swing: a timber boom let into the spur
        spur = W.BY["spur_%s" % s.tag]
        sd = 1.0 if (spur.c - s.c).dot(s.side) > 0 else -1.0
        dist = abs((spur.c - s.c).dot(s.side))
        m.block(M @ T(0, -sd * (dist - s.sx / 2) / 2, 0), s.sz / 2 - 0.02, (s.sx / 2 + dist) / 2, s.bot, top, "pr_timber")
        for a in (-s.sx / 2 + 0.5, 0.0, s.sx / 2 - 1.3):          # iron straps round it
            m.block(M @ T(0, -sd * a, 0), s.sz / 2 + 0.03, 0.09, s.bot - 0.03, top - 0.004, "pr_iron")
        p0 = s.at(0, sd * (dist - 0.4), s.bot - 3.4)
        m.tube([p0, s.at(0, sd * (dist - 3.4), s.bot + 0.1)], 0.2, "pr_timber", n=6)       # the strut, on the spur's side only
        tip = s.at(0, -sd * (s.sx / 2 - 0.5), s.bot)
        ST.ring_bolt(ST.S.r, tip - UP * 0.2, 0.3)
        ST.hang_vine(tip - UP * 0.3, 2.0, rng); ST.hang_vine(s.at(0.3, sd * 1.0, s.bot - 0.05), 3.2, rng)
        ST.leaf_clump(ST.S.c, s.at(0.56, sd * (dist - 1.2), s.top - 0.3), 0.7, 12, rng)
        return
    half = abs((pl.c - s.c).dot(s.side)) + 0.55
    for p in (pl, pr):                                            # the two posts, from the ground
        ST.pier(m, p.c.x, p.c.y, s.bot + 0.02, p.r + 0.02, None, rng, yaw=s.yaw, cap=False, force=True)
        m.block(T(p.c.x, p.c.y, 0, s.yaw), p.r + 0.14, p.r + 0.14, s.bot - 0.5, s.bot - 0.02, "pr_stone")            # the impost under the lintel
    if stone:
        cuts = [-half, -half * 0.36, -0.42, 0.42, half * 0.36, half]
        for k in range(5):                                        # five stones: two springers, two haunches, a keystone
            a, b = cuts[k], cuts[k + 1]
            key = k == 2
            m.block(M @ T(0, (a + b) / 2, 0), s.sz / 2 - 0.02 + (0.05 if key else 0.0), (b - a) / 2 - 0.004, s.bot, top, "pr_stone" if key else ASHLAR[(k + s.idx) % 4], top="pr_stone")
        for sd in (-1, 1):                                        # a painted frieze on each face, proud of it
            for k in range(8):
                a = -half + 0.8 + (2 * half - 1.6) * (k + 0.5) / 8
                if abs(a) < 0.5:
                    continue
                m.block(M @ T(sd * (s.sz / 2 + 0.0), a, 0), 0.05, (half - 0.8) / 8 - 0.07, s.bot + 0.3, s.bot + 0.7, ["pr_paint_red", "pr_paint_yellow", "pr_paint_teal", "pr_paint_white"][(k + s.idx) % 4])
    else:
        m.block(M, s.sz / 2 - 0.05, half, s.bot, top, "pr_timber")
        for a in (-half + 0.5, half - 0.5):
            m.block(M @ T(0, a, 0), s.sz / 2 - 0.02, 0.1, s.bot - 0.03, top - 0.004, "pr_iron")
    aim = s.at(0, 0, s.bot)
    ST.ring_bolt(ST.S.r, aim - UP * 0.2, 0.3)
    ST.hang_vine(aim - UP * 0.3 + s.side * 0.3, 1.8 + (s.idx % 2) * 0.6, rng)
    ST.hang_vine(s.at(0.3, (2.4 if s.idx % 2 else -2.7), s.bot - 0.05), 3.0, rng)
    ST.leaf_clump(ST.S.c, s.at(0.56, 0.6, s.top - 0.35), 0.6, 10, rng); ST.leaf_clump(ST.S.c, s.at(-0.56, -1.6, s.top - 0.45), 0.5, 9, rng)
    # what is left of the wall the arch carried: broken stubs OUTSIDE the posts, where nobody stands
    for sd in (-1, 1):
        hgt = [2.6, 1.4, 0.0][(s.idx + (sd > 0)) % 3]
        q = s.at(0, sd * (half + 0.75))
        if hgt and clear(q.x, q.y, s.bot - 3.0, s.top + hgt, 0.7, s.own):
            for j in range(2):
                m.block(T(q.x, q.y, 0, s.yaw) @ T(0, sd * j * 0.35, 0), s.sz / 2 - 0.08, 0.7 - j * 0.3, s.bot - 3.0 + j * 2.6, s.bot - 0.4 + hgt * (j + 1) / 2 + j, ASHLAR[(j + s.idx) % 4], taper=0.9)
            m.tube([Vector((q.x, q.y, s.bot - 3.0)) + s.side * (sd * 0.3), s.at(0, sd * (half - 0.5), s.bot - 0.6)], 0.14, "pr_timber", n=6)
    return


# ------------------------------------------------------------------ timber
def timber_beam(s, rng):
    """One of the three beams: baulks end to end, strapped at the joints, a post under every joint."""
    m = ST.S.m
    al, k = -s.sz / 2, 0
    joints = [al]
    while al < s.sz / 2 - 0.01:
        ln = min(rng.uniform(4.2, 6.2), s.sz / 2 - al)
        if s.sz / 2 - (al + ln) < 2.2:
            ln = s.sz / 2 - al
        q = s.at(al + ln / 2, 0)
        m.block(T(q.x, q.y, 0, s.yaw), ln / 2 - 0.004, s.sx / 2 - 0.01 - 0.004 * (k % 2), s.bot, s.top - DROP, "pr_timber", top="pr_plank")
        al += ln; k += 1
        joints.append(al)
    miss = 0
    for i, a in enumerate(joints):
        q = s.at(min(max(a, -s.sz / 2 + 0.5), s.sz / 2 - 0.5), 0)
        m.block(T(q.x, q.y, 0, s.yaw), 0.1, s.sx / 2 + 0.03, s.bot - 0.035, s.top - DROP - 0.004, "pr_iron")
        if stand(q.x, q.y, s.bot + 0.02, 0.4, s.own, rng, round_=True, cap_r=0.6, n=9):
            miss = 0
        else:
            miss += 1
            if miss > 1:
                bracket(q.x, q.y, s.bot, s, rng, 0.7); miss = 0
    for a, sd in ((-s.sz / 2, -1), (s.sz / 2, 1)):                 # an end that hangs in the air is splintered
        q = s.at(a + sd * 0.25, 0)
        if not on_other(q.x, q.y, s, s.top, 0.4):
            m.block(T(q.x, q.y, 0, s.yaw + 0.3), 0.22, 0.24, s.bot - 0.25, s.top - 0.42, "pr_timber", taper=0.6)


def dive_board(s, rng):
    """The board: three planks on two joists run back under the ledge, strutted off the cliff, a flag each side."""
    m = ST.S.m
    for j, off in enumerate((-1.0, 0.0, 1.0)):
        q = s.at(0.0 - (0.0 if j == 1 else 0.12 * j), off)
        m.block(T(q.x, q.y, 0, s.yaw + (0.006 * (j - 1))), s.sz / 2 - 0.01 - (0.0 if j == 1 else 0.12 * j), 0.49, s.top - 0.17, s.top - DROP - 0.003 * (j % 2), "pr_plank")
    for off in (-0.8, 0.8):
        q = s.at(-0.4, off)
        m.block(T(q.x, q.y, 0, s.yaw), s.sz / 2 - 0.3, 0.14, s.bot - 0.12, s.top - 0.16, "pr_timber")
        a, b = s.at(-s.sz / 2 + 0.6, off, s.bot - 3.0), s.at(s.sz / 2 - 1.6, off, s.bot - 0.1)
        m.tube([a, b], 0.12, "pr_timber", n=6)
    for sd in (-1, 1):
        q = s.at(-s.sz / 2 + 0.3, sd * 2.6)
        if not W.blocked(q.x, q.y, s.top, s.top + 4.0, 0.2, s.own, margin=-3.0):
            m.tube([Vector((q.x, q.y, s.top - 0.6)), Vector((q.x, q.y, s.top + 4.3))], 0.09, "pr_timber", n=6, r1=0.06)
            ST.banner((q.x, q.y, s.top + 4.1), s.flat, 2.4, 0.9, "pr_cloth_red" if sd < 0 else "pr_cloth_teal", rng)


# ------------------------------------------------------------------ the things that hold the movers
def gantry(p, across, top, wide, own, rng, bw=0.44, style=0):
    """Two piers and a timber beam whose UNDERSIDE middle is at `top` (a chain's top). Returns how many piers stand."""
    m = ST.S.m
    across = Vector((across.x, across.y, 0)).normalized()
    stood = []
    for sd in (-1, 1):
        q = Vector((p.x, p.y, 0)) + across * (sd * wide)
        g = W.ground_min(q.x, q.y, 0.7)
        if clear(q.x, q.y, g, top + 0.5, 0.66, own):
            ST.pier(m, q.x, q.y, top + 0.02 + (0.5 if (style + sd) % 3 == 0 else 0.0), 0.62 + 0.08 * rng.random(), None, rng, yaw=math.atan2(across.y, across.x), force=True)
            stood.append(sd)
    c = Vector((p.x, p.y, top + bw * 1.25))
    a, b = c - across * (wide + 0.5), c + across * (wide + 0.5)
    if len(stood) == 1:
        sd = stood[0]
        a, b = c - across * (sd * 1.6), c + across * (sd * (wide + 0.5))
        root = Vector((p.x, p.y, 0)) + across * (sd * (wide - 0.4))
        m.tube([Vector((root.x, root.y, top - 3.4)), Vector((p.x, p.y, top + 0.1)) + across * (sd * 1.0)], 0.2, "pr_timber", n=6)
    if not stood:
        NOTES.append("a gantry at (%.0f, %.0f, %.0f) has no clear place for either pier: NOT BUILT (the chain hangs from nothing)" % (p.x, p.y, top))
        return 0
    ST.beam(m, a, b, bw, bw * 1.25, "pr_timber")
    for sd in stood:
        q = Vector((p.x, p.y, top + bw * 0.6)) + across * (sd * wide)
        mm, _ = ST.lay(q - across * 0.12, q + across * 0.12)
        m.block(mm, 0.11, bw + 0.05, -bw * 0.75, bw * 0.72, "pr_iron")
    return len(stood)


def mover_frames(rng):
    m = ST.S.m
    by = {}
    for mv in W.MOVERS:
        by.setdefault((mv.kind, mv.tag), []).append(mv)
    for (kind, tag), lst in by.items():
        leg = W.LEGS[int(tag) - 1]
        f, s_ = leg.f, leg.s
        own = int(tag)
        if kind == "boulder_chain":
            # twelve chains: two sloping purlins down the lane on four frames, a tie across them over every boulder
            tops = [Vector((v.c.x, v.c.y, v.c.z + v.h / 2 + (9.0 - 0.05 * 1.1) * (v.r / 1.1))) for v in lst]
            A, C = tops[0], tops[-1]
            ax = (C - A); run = math.hypot(ax.x, ax.y)
            zat = lambda q: A.z + (C.z - A.z) * ((q - A).x * f.x + (q - A).y * f.y) / max(1e-6, (C - A).x * f.x + (C - A).y * f.y)
            lat0 = sum((t - leg.A).dot(s_) for t in tops) / len(tops)
            line = lambda al, off, dz=0.0: Vector((leg.A.x + f.x * al + s_.x * (lat0 + off), leg.A.y + f.y * al + s_.y * (lat0 + off), 0)) + UP * dz
            al0, al1 = (A - leg.A).dot(f) - 1.6, (C - leg.A).dot(f) + 1.6
            for sd in (-1, 1):
                a, b = line(al0, sd * 2.1), line(al1, sd * 2.1)
                a.z, b.z = zat(a) + 0.5, zat(b) + 0.5
                ST.beam(m, a, b, 0.2, 0.42, "pr_timber")
            for k, t in enumerate(tops):
                al = (t - leg.A).dot(f)
                a, b = line(al, -2.6), line(al, 2.6)
                a.z = b.z = t.z + 0.36
                ST.beam(m, a, b, 0.17, 0.34, "pr_timber")
                m.block(T(t.x, t.y, 0, math.atan2(s_.y, s_.x)), 0.2, 0.2, t.z - 0.12, t.z + 0.04, "pr_iron")
            nfr = 4
            for k in range(nfr):
                al = al0 + 0.5 + (al1 - al0 - 1.0) * k / (nfr - 1)
                c = line(al, 0.0); c.z = zat(c) + 0.5 - 0.42 - 0.55
                gantry(c, s_, c.z, 5.6, own, rng, style=k)
        elif kind == "pendulum":
            # an axle beam down the lane through every pivot, on A-frames between the swings
            piv = [v.pivot for v in lst]
            d = (piv[-1] - piv[0]).normalized() if len(piv) > 1 else f
            gap = (piv[1] - piv[0]).length if len(piv) > 1 else 5.0
            a, b = piv[0] - d * (gap / 2 + 0.5), piv[-1] + d * (gap / 2 + 0.5)
            ST.beam(m, a + UP * 0.55, b + UP * 0.55, 0.3, 0.52, "pr_timber")
            for k in range(len(piv) + 1):
                c = piv[0] + d * (gap * (k - 0.5))
                for sd in (-1, 1):
                    foot = Vector((c.x, c.y, 0)) + s_ * (sd * 5.2)
                    g = W.ground_min(foot.x, foot.y, 0.5)
                    if clear(foot.x, foot.y, g, g + 3.0, 0.5, own, margin=0.0):
                        m.tube([Vector((foot.x, foot.y, g - 0.5)), c + UP * 0.2 + s_ * (sd * 0.15)], 0.24, "pr_timber", n=7, r1=0.19)
                        m.block(T(foot.x, foot.y, 0, math.atan2(f.y, f.x)), 0.6, 0.6, g - 0.7, g + 0.45, "pr_ashlar_dark", taper=0.8)
                    else:
                        NOTES.append("the gauntlet's A-frame leg at (%.0f, %.0f) has no clear footing: left out" % (foot.x, foot.y))
                tie = c + UP * (-(c.z - leg.A.z) * 0.35)
                m.tube([tie - s_ * (5.2 * 0.35), tie + s_ * (5.2 * 0.35)], 0.13, "pr_timber", n=6)
                m.block(T(c.x, c.y, 0, math.atan2(f.y, f.x)), 0.36, 0.4, c.z - 0.1, c.z + 0.62, "pr_iron")
        elif kind == "log":
            # two rails the logs' axle pins rest in, on trestle posts
            ends = [lst[0].c - f * 1.0, lst[-1].c + f * 1.0]
            for sd in (-1, 1):
                a, b = [e + s_ * (sd * (lst[0].sx / 2 + 0.2)) for e in ends]
                ST.beam(m, Vector((a.x, a.y, lst[0].c.z + 0.12)), Vector((b.x, b.y, lst[0].c.z + 0.12)), 0.2, 0.42, "pr_timber")
                for k, v in enumerate(lst):
                    if k % 2 == 0:
                        q = v.c + s_ * (sd * (v.sx / 2 + 0.2))
                        if stand(q.x, q.y, v.c.z - 0.28, 0.42, own, rng, yaw=math.atan2(f.y, f.x), cap=False):
                            m.block(T(q.x, q.y, 0, math.atan2(f.y, f.x)), 0.5, 0.36, v.c.z - 0.34, v.c.z + 0.3, "pr_iron" if k % 4 == 0 else "pr_timber")
        elif kind == "floating_rock":
            for k, v in enumerate(lst):
                bed = Vector((v.c.x + 0.6, v.c.y, W.ground(v.c.x + 0.6, v.c.y)))
                ST.S.r.chain((v.c.x, v.c.y, v.c.z - v.h / 2 - 0.2), bed + UP * 0.25, link=0.62, r=0.075)
                m.lathe(T(bed.x, bed.y, 0), [(0.75, bed.z - 0.4), (0.62, bed.z + 0.32)], 9, "pr_stone_worn", cap_top="pr_stone_worn")
                ST.ring_bolt(ST.S.r, bed + UP * 0.42, 0.24)
        elif kind == "crumble_slab":
            z = lst[0].c.z + lst[0].sy / 2                        # the slabs' top; they rest on cross beams on pier pairs
            hw = lst[0].sx / 2
            stations = [lst[0].c - f * (lst[0].sz / 2)] + [v.c + f * (v.sz / 2) for v in lst]
            for k, c in enumerate(stations):
                a, b = Vector((c.x, c.y, z - lst[0].sy - 0.02)) - s_ * (hw + 0.3), Vector((c.x, c.y, z - lst[0].sy - 0.02)) + s_ * (hw + 0.3)
                ST.beam(m, a, b, 0.3 if k % 2 == 0 else 0.2, 0.46 if k % 2 == 0 else 0.34, "pr_ashlar_dark" if k % 2 == 0 else "pr_timber")
                if k % 2 == 0:
                    got = sum(stand((c + s_ * (sd * (hw - 0.4))).x, (c + s_ * (sd * (hw - 0.4))).y, z - lst[0].sy - 0.45, 0.6, own, rng, yaw=math.atan2(f.y, f.x), cap=False, wade=True) for sd in (-1, 1))
                    if not got:
                        NOTES.append("leg %d: no pier clear under the crumbling walk's joint %d" % (own, k))
            for sd in (-1, 1):
                a, b = stations[0] + s_ * (sd * (hw - 0.4)), stations[-1] + s_ * (sd * (hw - 0.4))
                ST.beam(m, Vector((a.x, a.y, z - lst[0].sy - 0.4)), Vector((b.x, b.y, z - lst[0].sy - 0.4)), 0.2, 0.38, "pr_timber")
        elif kind == "chain_anchor":
            for k, v in enumerate(lst):
                gantry(Vector((v.c.x, v.c.y, v.c.z + 4.0)), s_, v.c.z + 4.0, 5.2, own, rng, bw=0.46, style=k + 2)
                ST.leaf_clump(ST.S.c, (v.c.x + s_.x * 1.4, v.c.y + s_.y * 1.4, v.c.z + 4.9), 0.6, 10, rng)


def show_movers():
    """The movers at rest, for the review pictures only: placed exactly as PaeteRunSceneBuilder.Dress places them."""
    P = ST.P
    count = {}
    for v in W.MOVERS:
        i = count.get(v.kind, 0); count[v.kind] = i + 1
        d = v.d
        yaw = -math.radians(d.get("yaw", 0.0))
        at, sc, fn, kw = 0.0, (1, 1, 1), ST.MOVER_FN[v.kind], {}
        if v.kind == "boulder_chain":
            made, kw = 1.5, dict(r=1.5, kit=13, seed=0)
            if v.r <= 1.3:
                made, kw = 1.1, dict(r=1.1, kit=15, seed=5)
            elif v.r >= 1.7:
                made, kw = 1.9, dict(r=1.9, kit=3, seed=4)
            k = v.r / made
            sc, at = (k, k, k), v.h / 2 + (9.0 - 0.05 * made) * k
        elif v.kind == "pendulum":
            kw = dict(kit=3, seed=2) if i % 2 else {}
            k = d["arm"] / 8.2 if d["arm"] > 0.1 else 1.0
            sc, at = (k, k, k), d["arm"]
        elif v.kind == "mill_wheel":
            sc = (v.r, v.r, 1)
        elif v.kind == "log":
            sc = (v.sx / 8.5, v.r / 0.75, v.r / 0.75)
        elif v.kind == "seesaw":
            sc = (v.sx / 3.8, v.sz / 4.8, 1)
        elif v.kind == "crumble_slab":
            sc, at = (v.sx / 7.2, v.sz / 3.0, v.sy / 0.5), v.sy / 2
        elif v.kind == "floating_rock":
            made, kw = (1.9, dict(r=1.9, kit=4, seed=3)) if i % 2 else (1.6, {})
            k = v.r / made
            sc, at = (k, k, k), v.h / 2
        elif v.kind == "chain_anchor":
            at = 4.0
        P.at(T(v.c.x, v.c.y, v.c.z + at, yaw) @ Matrix.Diagonal((sc[0], sc[1], sc[2], 1)))
        fn(P, **kw)
    P.at(Matrix.Identity(4))


# ------------------------------------------------------------------ every solid
def build_all():
    cliff = W.BY.get("cliff_25")
    for s in W.SOLIDS:
        rng = random.Random(7000 + s.idx * 13)
        n = s.name
        if n.startswith("chute_"):
            continue                                              # author_paete_run_art_chutes.py
        if n.startswith(("deck_", "lead_", "causeway_", "start_plaza", "finish_platform")) or n == "ledge_5":
            fn = deck
        elif n.startswith("bridge_"):
            fn = bridge
        elif n.startswith("ramp_"):
            fn = ramp
        elif n.startswith(("landing_", "shortcut_node_", "tier_")):
            fn = dais
        elif n.startswith("pool_"):
            fn = pool_floor if n.endswith("floor") else pool_rim
        elif n.startswith("column_"):
            fn = hop_column
        elif n.startswith("fallen_post") or s.kind == "post" and s.shape == "cyl":
            fn = post
        elif n.startswith("fallen_"):
            fn = fallen
        elif n.startswith("drum_"):
            fn = drum_stack
        elif n.startswith("goat_"):
            fn = needle
        elif n.startswith("giant_step"):
            fn = giant_step
        elif n.startswith("island_"):
            fn = island
        elif n.startswith(("shelf_", "buttress_")):
            fn = bastion
        elif n.startswith("ledge_"):
            fn = lambda s_, r_: shelf(s_, r_, cliff)
        elif n.startswith("cliff_"):
            fn = cliff_wall
        elif n.startswith("anchor_"):
            fn = gate_of
        elif n.startswith("beam_"):
            fn = timber_beam
        elif n.startswith("board_"):
            fn = dive_board
        else:
            NOTES.append("no builder for solid %s (kind %s): NOT DRAWN" % (n, s.kind)); continue
        fn(s, rng)
        BUILT[n] = getattr(fn, "__name__", "shelf")
    mover_frames(random.Random(515))
