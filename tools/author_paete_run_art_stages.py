"""PAETE'S RUN, the art's BUILDERS: the masonry members every piece is made of, the things that move, the small props.

Imported by tools/author_paete_run_art.py and by the files that place things (author_paete_run_art_solids.py: every
walked surface, drawn from the gameplay layout solid by solid; _chutes.py: the two slides; _dress.py: the dressing).

THE RULES EVERY PIECE KEEPS: a chain hangs from a beam that rests on piers that stand in the ground (`pier` finds the
ground and refuses to stand in another leg's lane); parts run 2 cm INTO what they meet; a box keeps its own corners; a
top a body stands on is the layout solid's own height, never above it; no two repeated things side by side are the same.

MOVERS are built by `mv_*` at the origin, in their REST pose. Model space: +Z up, the lane's FORWARD is -Y (Unity +Z), the
lane's left is +X (Unity -X). Unity (PaeteRunSceneBuilder.Dress) hangs each on its layout mover; the same numbers place
them in the review pictures (author_paete_run_art_solids.show_movers).
"""
import math
import random

from mathutils import Matrix, Vector

import author_paete_run_art_kit as K
import author_paete_run_art_world as W

UP = Vector((0, 0, 1))
frame, T = K.frame, K.T


class Set:
    """Three buffers that make one piece: m (stone and timber, bevelled), r (rocks, chains, round bars), c (leaves, cloth)."""

    def __init__(self, name, seed=1):
        self.name = name
        self.m, self.r, self.c = K.Buf(name + "_m", seed), K.Buf(name + "_r", seed + 1), K.Buf(name + "_c", seed + 2)

    def at(self, M):
        self.m.xf = self.r.xf = self.c.xf = M


S = None            # the static set, given by the main script
P = None            # the preview set (movers drawn at rest), given by the main script
PROPS = []          # (model, x, y, z, yaw degrees, scale): small things placed by the list
NOTES = []          # what could not be stood where the blockout has it


def rest(leg):
    """Blender yaw that turns a mover's forward (-Y) along the leg."""
    return math.atan2(leg.f.x, -leg.f.y)


def lay(a, b):
    """A matrix at the middle of a..b with local x along it (pitch included), y level, z up-ish; and the length."""
    a, b = Vector(a), Vector(b)
    x = (b - a)
    ln = x.length
    x.normalize()
    y = UP.cross(x)
    if y.length < 1e-4:
        y = Vector((0, 1, 0))
    y.normalize()
    z = x.cross(y)
    m = Matrix((x, y, z)).transposed().to_4x4()
    m.translation = (a + b) / 2
    return m, ln


def beam(B, a, b, hw, th, mat, top=None, lift=0.0):
    """A member whose TOP face runs from a to b."""
    m, ln = lay(a, b)
    B.block(m, ln / 2, hw, -th + lift, lift, mat, top)


def pier(B, x, y, top, half=0.7, owner=None, rng=None, mats=("pr_ashlar", "pr_ashlar", "pr_ashlar_dark", "pr_ashlar_rose"), yaw=0.0, cap=True, foot=None, force=False, wade=False):
    """A masonry pier from the ground to `top`: battered courses, each its own block, a plinth, a cap. False (and nothing
    built) if it would stand in another leg's lane."""
    rng = rng or B.rng
    g = (W.ground_min(x, y, half) if foot is None else foot) - 0.9
    if top - g < 0.4:
        return True
    if not force and W.blocked(x, y, g + 0.9, top, half, owner, wade=wade):
        return False
    h = top - g
    z = g
    n = max(1, round(h / rng.uniform(1.7, 2.6)))
    base_mat = rng.choice(mats)
    B.block(T(x, y, 0, yaw + rng.uniform(-0.04, 0.04)), half * 1.38, half * 1.38, g - 0.07, g + 1.5, "pr_ashlar_dark")
    for k in range(n):
        z1 = g + h * (k + 1) / n
        wide = half * (1.0 + 0.22 * (1 - (k + 0.5) / n)) * rng.uniform(0.97, 1.04)
        mt = base_mat if rng.random() < 0.75 else rng.choice(mats)
        B.block(T(x + rng.uniform(-0.03, 0.03), y + rng.uniform(-0.03, 0.03), 0, yaw + rng.uniform(-0.05, 0.05)), wide, wide,
                z, z1, mt, taper=0.94 if k < n - 1 else 1.0)
        z = z1
    if cap:
        B.block(T(x, y, 0, yaw), half * 1.2, half * 1.2, top - 0.38, top + 0.02, "pr_stone")
    return True


def column(B, x, y, top, r, owner=None, rng=None, cap_r=None, mat="pr_stone", foot=None, n=12, force=False):
    """A column of stacked drums, no two the same, on a base, under a capital whose top is `top`."""
    rng = rng or B.rng
    g = (W.ground_min(x, y, r) if foot is None else foot) - 0.8
    if top - g < 0.3:
        return True
    if not force and W.blocked(x, y, g + 0.8, top - 0.3, r, owner):
        return False
    B.lathe(T(x, y, 0), [(r * 1.42, g), (r * 1.42, g + 1.15), (r * 1.14, g + 1.5)], n, "pr_stone_worn", cap_top="pr_stone_worn")
    shaft_top = top - (0.75 if cap_r else 0.0)
    z = g + 1.4
    while z < shaft_top - 0.05:
        dh = min(rng.uniform(1.3, 2.3), shaft_top - z)
        if shaft_top - (z + dh) < 0.7:
            dh = shaft_top - z
        rr = r * rng.uniform(0.94, 1.03)
        B.lathe(T(x + rng.uniform(-0.04, 0.04), y + rng.uniform(-0.04, 0.04), 0, rng.uniform(0, 1)), [(rr, z), (rr * 0.99, z + dh)], n,
                mat if rng.random() < 0.8 else "pr_stone_worn", cap_top="pr_stone_worn")
        z += dh
    if cap_r:
        B.lathe(T(x, y, 0), [(r * 0.98, shaft_top - 0.02), (r * 1.08, shaft_top + 0.22), (cap_r, top - 0.3), (cap_r, top)], n + 2, "pr_stone", cap_top="pr_paving")
    return True


def spire(B, x, y, top, across, owner=None, rng=None):
    """A natural rock pillar: kit stones stacked, shrinking as they climb, the top one flat at `top`."""
    rng = rng or B.rng
    g = W.ground_min(x, y, across * 0.4) - 0.5
    if W.blocked(x, y, g + 0.5, top - 0.3, across * 0.4, owner):
        return False
    z = top
    k = 0
    while z > g:
        idx = rng.choice(list(K.COBBLES) + list(K.BOULDERS)[1:4]) if k else rng.choice(list(K.SLABS) + [K.COBBLES[0]])
        w = across * (1.0 + 0.16 * k) * rng.uniform(0.92, 1.08)
        rk = K.ROCKS[idx]
        hgt = (rk["top"] - rk["bottom"]) * w / 2.5
        B.rock_at(idx, x + rng.uniform(-0.12, 0.12) * k, y + rng.uniform(-0.12, 0.12) * k, z, w, rng.uniform(0, 6.28))
        z -= hgt * 0.62
        k += 1
        if k > 14:
            break
    return True


def frame_over(leg, p, top, wide, rng, style=0, bw=0.42):
    """Two piers and a beam across the lane: what a chain hangs from. A pier that would stand in another lane is left
    out and the beam is braced from the one that stands."""
    s = leg.s
    stood = []
    for sd in (-1, 1):
        q = p + s * (sd * wide)
        half = 0.62 + 0.1 * rng.random()
        ok = pier(S.m, q.x, q.y, top + 0.55 + (0.5 if (style + sd) % 3 == 0 else 0.0), half, leg.i, rng, yaw=math.atan2(leg.f.y, leg.f.x))
        if ok:
            stood.append(sd)
    a, b = Vector((p.x, p.y, top + bw * 0.5)) - s * (wide + 0.5), Vector((p.x, p.y, top + bw * 0.5)) + s * (wide + 0.5)
    if len(stood) == 1:
        sd = stood[0]
        a, b = (Vector((p.x, p.y, top + bw * 0.5)) - s * (sd * 1.6)), Vector((p.x, p.y, top + bw * 0.5)) + s * (sd * (wide + 0.5))
        root = p + s * (sd * (wide - 0.3))
        S.m.tube([Vector((root.x, root.y, top - 3.2)), Vector((p.x, p.y, top - 0.1)) + s * (sd * 1.2)], 0.2, "pr_timber", n=6)       # the brace
    if not stood:
        NOTES.append("leg %d: a frame at (%.0f, %.0f) has no clear place for either pier" % (leg.i, p.x, p.y))
    beam(S.m, a, b, bw, bw * 1.25, "pr_timber")
    for sd in stood:                                              # iron straps where the beam sits on the piers
        q = Vector((p.x, p.y, top)) + s * (sd * wide)
        m, _ = lay(q - s * 0.2, q + s * 0.2)
        S.m.block(m, 0.11, bw + 0.05, -bw * 0.9, bw * 0.55, "pr_iron")
    return stood


def ring_bolt(B, p, r=0.22):
    pts = [Vector(p) + Vector((r * math.cos(a), 0, r * math.sin(a))) for a in [math.tau * k / 8 for k in range(8)]]
    B.tube(pts, 0.06, "pr_iron", n=4, closed=True)


def leaf_clump(B, c, r, n=16, rng=None, mat=None, squash=0.8):
    """The house leaf pattern: cards shingled on a ball, facing out, tips running down it. Two tints a plant, per plant."""
    rng = rng or B.rng
    c = Vector(c)
    light, dark = ("pr_leaf_light", "pr_leaf_dark")
    for k in range(n):
        zf = 1 - 2 * (k + 0.5) / n
        rad = math.sqrt(max(0.0, 1 - zf * zf))
        th = k * 2.39996 + rng.uniform(-0.2, 0.2)
        d = Vector((rad * math.cos(th), rad * math.sin(th), zf))
        down = (-UP + d * d.z)
        if down.length < 0.05:
            down = Vector((math.cos(th), math.sin(th), 0))
        down.normalize()
        right = d.cross(down).normalized()
        size = r * rng.uniform(0.5, 0.68)
        base = c + Vector((d.x, d.y, d.z * squash)) * (r * 0.72) - down * (size * 0.6)
        B.card(base, right * size * 0.55, down * size * 1.25 + d * (size * 0.18), mat or (light if d.z > -0.1 else dark))


def vine(B, pts, rng, leaves=0.55, r=0.07, size=0.5):
    """A stem with leaf clumps along it."""
    S.r.tube(pts, r, "pr_vine", n=5, r1=r * 0.5)
    run = 0.0
    for a, b in zip(pts, pts[1:]):
        a, b = Vector(a), Vector(b)
        seg = (b - a).length
        t = 0.0
        while run + (seg - t) >= leaves:
            t += leaves - run
            run = 0.0
            leaf_clump(B, a.lerp(b, t / seg) + Vector((rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1), 0)), size * rng.uniform(0.7, 1.15), 7, rng)
        run += seg - t


def hang_vine(p, length, rng, sway=0.5):
    pts = [Vector(p) + Vector((sway * math.sin(k * 0.9 + p[0]) * k / 6, sway * math.cos(k * 1.3 + p[1]) * k / 6, -length * k / 6)) for k in range(7)]
    vine(S.c, pts, rng, 0.6, 0.06, 0.42)


def bunting(a, b, sag, rng, n=None):
    """A sagging rope of pennants: crimson, yellow, teal, white (never orange or blue: those are the teams')."""
    a, b = Vector(a), Vector(b)
    ln = (b - a).length
    n = n or max(4, int(ln / 0.85))
    pts = [a.lerp(b, k / n) - UP * (sag * math.sin(math.pi * k / n)) for k in range(n + 1)]
    S.r.tube(pts, 0.025, "pr_rope", n=4)
    side = (b - a).normalized()
    cols = ["pr_cloth_red", "pr_cloth_yellow", "pr_cloth_teal", "pr_cloth_white"]
    for k in range(n):
        p, q = pts[k], pts[k + 1]
        mid = (p + q) / 2
        w = (q - p).length * 0.42
        nrm = side.cross(UP)                                      # each pennant hangs with a curl of its own, never a flat card
        tip = mid - UP * rng.uniform(0.5, 0.62) + nrm * rng.uniform(-0.16, 0.16) + side * rng.uniform(-0.05, 0.05)
        belly = nrm * rng.uniform(-0.05, 0.05)
        l, r_, lm, rm = mid - side * w, mid + side * w, (mid - side * w + tip) / 2 + belly, (mid + side * w + tip) / 2 + belly
        mt = cols[k % 4]
        for pq, uv in (([l, r_, rm, lm], [(0, 0), (0.4, 0), (0.3, 0.25), (0.1, 0.25)]), ([lm, rm, tip], [(0.1, 0.25), (0.3, 0.25), (0.2, 0.5)])):
            S.c.quad(pq, mt, uvs=uv); S.c.quad([v - nrm * 0.008 for v in pq[::-1]], mt, uvs=uv[::-1])       # 8 mm thick: two sides, never one plane


BANNERS = []        # every banner placed: (model, x, y, z, yaw degrees, scale). ONE settled cloth mesh each, on its own rod.
_BV = [0]


def banner(p, facing, h, w, mat, rng, wall=None):
    """A banner hung from a short rod at p (the rod's middle). `wall`: the way to the masonry behind it; the rod then
    stands off it on two bracket arms, so the cloth hangs free. The cloth is a MODEL (author_paete_run_art_dress.cloth_models:
    a real cloth simulation settled and baked, four shapes a colour), placed by the list: one per rod, never two."""
    p = Vector(p)
    side = Vector((-facing.y, facing.x, 0)).normalized()
    S.m.tube([p - side * (w * 0.62), p + side * (w * 0.62)], 0.05, "pr_timber", n=5)
    if wall is not None:
        for e in (-1, 1):
            a = p + side * (e * w * 0.56)
            S.m.tube([a, a + Vector(wall).normalized() * 0.55 - UP * 0.12], 0.045, "pr_timber", n=5)
    col = {"pr_cloth_red": "red", "pr_cloth_teal": "teal", "pr_cloth_yellow": "yellow"}.get(mat, "teal")
    row = ("banner_%s_%s" % (col, "abcd"[_BV[0] % 4]), p.x, p.y, p.z - 0.03, math.degrees(math.atan2(side.y, side.x)), h / 2.7)
    _BV[0] += 1
    PROPS.append(row); BANNERS.append(row)


# ------------------------------------------------------------------ the decks
def arch(B, c, axis, span, rise, depth, n, rng, mat="pr_ashlar", skip=(), th=0.62, plain=False):
    """A ring of voussoirs: springs at c -/+ axis * span, crown `rise` above c. `depth` is its half thickness across."""
    axis = Vector((axis.x, axis.y, 0)).normalized()
    across = UP.cross(axis)
    for j in range(n):
        if j in skip:
            continue
        a0, a1 = math.pi * j / n, math.pi * (j + 1) / n
        p0 = c + axis * (span * math.cos(a0)) + UP * (rise * math.sin(a0))
        p1 = c + axis * (span * math.cos(a1)) + UP * (rise * math.sin(a1))
        x = (p1 - p0)
        ln = x.length
        x.normalize()
        zz = x.cross(across).normalized()
        if zz.z < 0 and abs(zz.z) > 0.2:
            pass
        m = Matrix((x, across, zz)).transposed().to_4x4()
        m.translation = (p0 + p1) / 2
        key = j == n // 2
        # neighbours never share a depth (they run 3 cm into each other, and equal ones shared a face there)
        B.block(m, ln / 2 + 0.03, depth * (1.04 if key else 0.955 + 0.03 * (j % 2) + 0.004 * (j % 3)), -th * (0.62 if key else 0.5), th * (0.62 if key else 0.5),
                mat if plain else "pr_stone" if key else (mat if rng.random() < 0.8 else "pr_ashlar_dark"))


# ------------------------------------------------------------------ MOVERS (origin at the pivot, forward -Y)
def mv_boulder_chain(Q, r=1.5, drop=9.0, kit=13, seed=0):
    """A boulder hung on a chain. Origin: the hanging point at the top of the chain. The boulder's top, where a body
    lands, is drop - 0.05 r under it (the blockout's ball)."""
    rng = random.Random(seed)
    top = -(drop - 0.05 * r)
    Q.r.rock_at(kit, 0, 0, top, r * 2.0, rng.uniform(0, 6.28), squash=min(1.0, 0.8 * 2.5 * r * 2 / (r * 2.0 * (K.ROCKS[kit]["top"] - K.ROCKS[kit]["bottom"]))))
    Q.r.chain((0, 0, 0), (0, 0, top + 0.32), link=0.66, r=0.08)
    ring_bolt(Q.r, (0, 0, top + 0.2), 0.26)
    Q.m.lathe(T(0, 0, 0), [(0.2, top - 0.05), (0.2, top + 0.1)], 8, "pr_iron", cap_top="pr_iron")
    Q.m.block(T(0, 0, 0), 0.2, 0.3, -0.14, 0.2, "pr_iron")       # the shackle on the beam


def mv_pendulum(Q, arm=8.2, r=1.5, kit=0, seed=0):
    """A boulder on ONE chain that swings ACROSS the lane (about the forward axis). Origin: the pivot. The stone is one
    clean rock in an iron harness that stands PROUD of it (owner: dark slits where a band sat half inside the stone)."""
    rng = random.Random(seed)
    rk = K.ROCKS[kit]
    sq = min(1.2, 2 * r / ((rk["top"] - rk["bottom"]) * (2 * r) / 2.5))
    Q.r.rock_at(kit, 0, 0, -arm + r, r * 2.0, rng.uniform(0, 6.28), squash=sq)
    R = r * 1.14
    Q.r.tube([Vector((math.cos(a) * R, math.sin(a) * R, -arm)) for a in [math.tau * k / 16 for k in range(16)]], 0.1, "pr_iron", n=5, closed=True)
    for k in range(4):                                            # four straps from the band up to the ring the chain holds
        a = math.tau * k / 4 + 0.4
        Q.r.tube([Vector((math.cos(a) * R * math.cos(e), math.sin(a) * R * math.cos(e), -arm + R * math.sin(e))) for e in [math.pi / 2 * j / 5 for j in range(6)]], 0.075, "pr_iron", n=4)
    ring_bolt(Q.r, (0, 0, -arm + R + 0.16), 0.3)
    Q.r.chain((0, 0, -0.1), (0, 0, -arm + R + 0.3), link=0.7, r=0.09)
    Q.m.lathe(T(0, 0, 0) @ Matrix.Rotation(math.pi / 2, 4, "X"), [(0.26, -0.5), (0.26, 0.5)], 8, "pr_iron", cap_top="pr_iron", cap_bot="pr_iron")    # the axle sleeve


def mv_mill_wheel(Q, seed=0):
    """A millstone lying flat. UNIT RADIUS (scale x and z in Unity by its radius), 0.6 m thick whatever the scale.
    Origin: the hub, at the middle of its thickness; its top is 0.3 over that."""
    Q.m.lathe(T(0, 0, 0), [(0.90, -0.3), (1.0, -0.24), (1.0, 0.24), (0.965, 0.3)], 20, "pr_stone", cap_top="pr_paving", cap_bot="pr_stone_worn")
    Q.m.lathe(T(0, 0, 0), [(1.012, -0.11), (1.012, 0.11)], 20, "pr_iron")                              # the tyre
    Q.m.lathe(T(0, 0, 0), [(0.2, 0.28), (0.2, 0.62), (0.13, 0.7)], 8, "pr_timber", cap_top="pr_timber")   # the axle's head
    Q.m.lathe(T(0, 0, 0), [(0.27, 0.28), (0.27, 0.36)], 8, "pr_iron", cap_top="pr_iron")
    for k in range(4):                                             # four painted furrows: they show it turning
        a = math.tau * k / 4 + 0.3
        m = T(0, 0, 0, a)
        Q.m.block(m @ T(0.6, 0, 0), 0.3, 0.05 if k else 0.09, 0.29, 0.318, "pr_paint_red" if k == 0 else "pr_stone_worn")


def mv_log(Q, r=0.75, ln=8.5, seed=0):
    """A log that turns underfoot. Origin: the middle of its axis. The axis is X (across the lane)."""
    rng = random.Random(seed)
    rot = Matrix.Rotation(math.pi / 2, 4, "Y")
    prof = [(r * 0.86, -ln / 2), (r, -ln / 2 + 0.25)] + [(r * (1 + 0.035 * math.sin(k * 2.1 + seed)), -ln / 2 + ln * k / 6) for k in range(1, 6)] + [(r, ln / 2 - 0.25), (r * 0.86, ln / 2)]
    Q.m.lathe(rot, prof, 12, "pr_timber", cap_top="pr_plank", cap_bot="pr_plank", phase=rng.uniform(0, 1))
    for sd in (-1, 1):
        Q.m.lathe(rot, [(r * 1.035, sd * (ln / 2 - 0.9) - 0.13), (r * 1.035, sd * (ln / 2 - 0.9) + 0.13)], 12, "pr_iron")
        Q.m.lathe(rot, [(0.16, sd * ln / 2 - 0.05), (0.16, sd * ln / 2 + 0.42)] if sd > 0 else [(0.16, -ln / 2 - 0.42), (0.16, -ln / 2 + 0.05)], 8, "pr_iron", cap_top="pr_iron", cap_bot="pr_iron")
    for k in range(3):                                             # knots, so its turning shows
        a = rng.uniform(0, 6.28)
        x = rng.uniform(-ln / 2 + 1.4, ln / 2 - 1.4)
        Q.m.lathe(T(x, 0, 0) @ Matrix.Rotation(a, 4, "X"), [(0.2, r - 0.05), (0.15, r + 0.07)], 7, "pr_timber", cap_top="pr_plank")


def mv_seesaw(Q, seed=0, half_l=2.4, half_w=1.9, th=0.45, pivot=0.7):
    """A slab on a pivot, LEVEL at rest. Origin: the pivot axis (X, across the lane), 0.7 under the slab's top."""
    rng = random.Random(seed)
    Q.m.block(T(0, 0, 0), half_w, half_l, pivot - th, pivot, "pr_stone", top="pr_paving")
    for sd in (-1, 1):                                            # the cheeks that hug the axle
        Q.m.block(T(sd * (half_w - 0.5), 0, 0), 0.16, 0.5, -0.32, pivot - th + 0.02, "pr_timber")
        Q.m.block(T(sd * (half_w - 0.5), 0, 0), 0.19, 0.2, -0.36, -0.1, "pr_iron")
    for sy in (-1, 1):                                            # iron straps across the ends
        Q.m.block(T(0, sy * (half_l - 0.45), 0), half_w + 0.03, 0.09, pivot - th - 0.03, pivot + 0.012, "pr_iron")
    e = rng.choice((-1, 1))
    Q.m.block(T(e * (half_w - 0.35), -(half_l - 0.3), 0, 0.5), 0.3, 0.3, pivot - th - 0.04, pivot - 0.07, "pr_stone_worn")   # a chipped corner


def mv_crumble_slab(Q, seed=0, half_l=1.5, half_w=3.6, th=0.5):
    """A slab of the crumbling walk. Origin: the middle of its TOP face."""
    rng = random.Random(seed)
    cut = rng.uniform(-0.8, 0.8)
    Q.m.block(T((cut - half_w) / 2, 0, 0), (cut + half_w) / 2 - 0.0, half_l, -th, 0.0, "pr_ashlar", top="pr_paving")
    Q.m.block(T((cut + half_w) / 2, 0, 0, 0.0), (half_w - cut) / 2, half_l - 0.03, -th + 0.03, -0.012, "pr_ashlar_dark", top="pr_paving")   # a second flag, a hair lower: a crack
    for k in range(3):
        Q.m.block(T(-half_w + 0.9 + k * (half_w - 0.9), (1 if k % 2 else -1) * (half_l - 0.1), 0, rng.uniform(0, 3)), 0.24, 0.2, -th - 0.12 - 0.01 * k, -0.2 - 0.012 * k, "pr_stone_worn")


def mv_floating_rock(Q, r=1.6, kit=1, seed=0):
    """A boulder that floats, held down by its chain. Origin: the middle of its TOP (where a body lands). The blockout's
    ball tops out 0.7 r - 1.0 over the leg's line."""
    rng = random.Random(seed)
    rk = K.ROCKS[kit]
    sq = min(1.0, 1.4 * r / ((rk["top"] - rk["bottom"]) * (2 * r) / 2.5))
    Q.r.rock_at(kit, 0, 0, 0.0, r * 2.0, rng.uniform(0, 6.28), squash=sq)
    bot = -1.4 * r * 0.92
    ring_bolt(Q.r, (0, 0, bot - 0.05), 0.26)
    for k in range(5):                                            # Paete's roots hanging under it
        a = math.tau * k / 5 + seed
        p0 = Vector((math.cos(a) * r * 0.55, math.sin(a) * r * 0.55, bot * 0.72))
        Q.r.tube([p0, p0 + Vector((math.cos(a) * 0.25, math.sin(a) * 0.25, -0.7)), p0 + Vector((math.cos(a + 0.6) * 0.3, math.sin(a + 0.6) * 0.3, -1.3 - 0.4 * (k % 2)))], 0.07, "pr_vine", n=5, r1=0.02)
    for k in range(4):
        a = math.tau * k / 4 + 0.9 + seed
        leaf_clump(Q.c, (math.cos(a) * r * 0.86, math.sin(a) * r * 0.86, -0.42 - 0.25 * (k % 2)), 0.42, 8, rng)


def mv_chain_anchor(Q, ln=4.5, seed=0):
    """A hanging chain the vine catches: a bronze ring wound in leaves at its end. Origin: the top of the chain."""
    rng = random.Random(seed)
    Q.r.chain((0, 0, 0), (0, 0, -ln + 0.5), link=0.6, r=0.075)
    Q.r.tube([Vector((math.cos(a) * 0.5, 0, -ln + math.sin(a) * 0.5)) for a in [math.tau * k / 10 for k in range(10)]], 0.085, "pr_bronze", n=5, closed=True)
    for k in range(5):
        a = math.tau * k / 5
        leaf_clump(Q.c, (math.cos(a) * 0.48, 0.05 * (k % 2), -ln + math.sin(a) * 0.48), 0.34, 7, rng)
    Q.m.block(T(0, 0, 0), 0.18, 0.26, -0.12, 0.2, "pr_iron")


# ------------------------------------------------------------------ the set pieces
def gate(p, f, rng, finish=False):
    """A carved gate across the lane: two towers, a lintel with a bronze sun, banners and bunting. The finish wears the
    chequers."""
    p = Vector(p)
    s = Vector((f.y, -f.x, 0))
    yaw = math.atan2(f.y, f.x)
    z = p.z
    for sd in (-1, 1):
        q = p + s * (sd * 5.3)
        for j, (hh, w) in enumerate(((2.4, 1.25), (2.2, 1.1), (1.9, 0.98))):
            z0 = z - 0.5 + sum(x[0] for x in ((2.4, 0), (2.2, 0), (1.9, 0))[:j])
            S.m.block(T(q.x, q.y, 0, yaw), w, w, z0 - 0.02, z0 + hh, ["pr_ashlar", "pr_ashlar_rose", "pr_ashlar"][j], taper=0.95)
            S.m.block(T(q.x, q.y, 0, yaw), w + 0.12, w + 0.12, z0 + hh - 0.24, z0 + hh + 0.06, "pr_stone")
        S.m.block(T(q.x, q.y, 0, yaw), 0.7, 0.7, z + 6.0, z + 6.9, "pr_ashlar_dark", taper=0.6)
        S.m.lathe(T(q.x, q.y, 0), [(0.2, z + 6.85), (0.42, z + 7.15), (0.5, z + 7.5), (0.3, z + 7.9), (0.34, z + 8.0)], 10, "pr_clay", cap_top="pr_clay")
        banner(Vector((q.x, q.y, z + 5.5)) - f * 1.72, f, 3.6, 1.3, ("pr_cloth_teal" if finish else "pr_cloth_red") if sd < 0 else "pr_cloth_yellow", rng, wall=f)
        leaf_clump(S.c, (q.x + 0.5, q.y, z + 8.2), 0.7, 12, rng)
        hang_vine(Vector((q.x, q.y, z + 6.0)) + s * (sd * 1.2), 3.0, rng)
    a, b = Vector((p.x, p.y, z + 6.05)) - s * 6.3, Vector((p.x, p.y, z + 6.05)) + s * 6.3
    beam(S.m, a, b, 0.62, 1.1, "pr_stone")
    beam(S.m, a + s * 0.4 + UP * 0.38, b - s * 0.4 + UP * 0.38, 0.5, 0.4, "pr_ashlar_dark")
    for k in range(12):                                           # the frieze: painted blocks, chequered at the finish
        c = Vector((p.x, p.y, 0)) + s * (-5.1 + k * 0.927)
        mt = ("pr_paint_black" if k % 2 else "pr_paint_white") if finish else ["pr_paint_red", "pr_paint_yellow", "pr_paint_teal", "pr_paint_white"][k % 4]
        S.m.block(T(c.x, c.y, 0, yaw), 0.66, 0.4, z + 5.18, z + 5.82, mt)
        if finish:
            S.m.block(T(c.x, c.y, 0, yaw), 0.66, 0.4, z + 4.56, z + 5.2, "pr_paint_white" if k % 2 else "pr_paint_black")
    m = frame(Vector((p.x, p.y, z + 6.9)), f) @ Matrix.Rotation(math.pi / 2, 4, "Y")
    S.m.lathe(m, [(1.0, -0.2), (1.05, 0.0), (0.8, 0.2), (0.3, 0.3)], 12, "pr_bronze", cap_top="pr_bronze", cap_bot="pr_bronze")
    for k in range(8):                                            # the sun's rays
        a2 = math.tau * k / 8
        d = s * math.cos(a2) + UP * math.sin(a2)
        c = Vector((p.x, p.y, z + 6.9)) + d * 1.35
        S.m.tube([c - d * 0.3, c + d * 0.3], 0.13, "pr_bronze", n=4, r1=0.03)
    # the pennants hang 60 cm in front of the painted frieze, never through it
    for sd in (-1, 1):
        e = Vector((p.x, p.y, z + 7.0)) + s * (sd * 5.3)
        S.m.tube([e - f * 0.3, e - f * 1.36], 0.04, "pr_timber", n=5)
    bunting(Vector((p.x, p.y, z + 6.95)) - s * 5.3 - f * 1.3, Vector((p.x, p.y, z + 6.95)) + s * 5.3 - f * 1.3, 1.1, rng)


def rotunda(c, z, rng):
    """THE SUMMIT'S RUIN: a ring of columns round the cistern the sluice once drew from, half its ring beam fallen."""
    cx, cy = c
    S.m.lathe(T(cx, cy, 0), [(4.3, z - 0.6), (4.3, z + 0.45), (3.7, z + 0.45), (3.6, z - 0.5)], 18, "pr_ashlar", cap_bot=None)
    S.m.lathe(T(cx, cy, 0), [(3.72, z - 0.4), (3.72, z + 0.18)], 18, "pr_stone_wet", cap_top="pr_water")
    n = 9
    tops = []
    for k in range(n):
        a = math.tau * k / n + 0.2
        x, y = cx + math.cos(a) * 6.6, cy + math.sin(a) * 6.6
        h = [6.2, 6.2, 6.2, 3.1, 6.2, 1.6, 6.2, 6.2, 4.4][k]
        if W.blocked(x, y, z, z + h, 0.8):
            tops.append(None); continue
        column(S.m, x, y, z + h, 0.55, None, rng, cap_r=0.82 if h > 6 else None, foot=z + 0.3, n=10)
        tops.append((x, y, h))
    for k in range(n):
        a, b = tops[k], tops[(k + 1) % n]
        if a and b and a[2] > 6 and b[2] > 6:
            beam(S.m, Vector((a[0], a[1], z + 6.9 + 0.03 * k)), Vector((b[0], b[1], z + 6.9 + 0.03 * k)), 0.5 - 0.01 * (k % 3), 0.72, "pr_stone")
            bunting(Vector((a[0], a[1], z + 6.1)), Vector((b[0], b[1], z + 6.1)), 0.8, rng)
    hang_vine((cx + 6.4, cy + 1.4, z + 6.3), 4.0, rng); hang_vine((cx - 5.2, cy + 4.0, z + 6.3), 3.0, rng)
    leaf_clump(S.c, (cx + 6.5, cy + 1.2, z + 7.3), 0.9, 14, rng)


# ------------------------------------------------------------------ small props (their own models, placed by the list)
def prop_checkpoint(Q, seed=0):
    """A small standing stone with a painted band: a checkpoint."""
    Q.r.rock_at(K.STACKS[1], 0, 0, 2.1, 1.05, 0.4)
    Q.m.lathe(T(0, 0, 0), [(0.5, 1.02), (0.47, 1.36)], 10, "pr_paint_teal")
    Q.m.lathe(T(0, 0, 0), [(0.49, 1.36), (0.46, 1.48)], 10, "pr_paint_yellow")
    Q.m.lathe(T(0, 0, 0), [(0.75, -0.3), (0.62, 0.16)], 10, "pr_stone_worn", cap_top="pr_stone_worn")
    leaf_clump(Q.c, (0.3, 0.2, 2.15), 0.3, 8, random.Random(seed))


def prop_jar(Q, seed=0, tall=False):
    """A clay water jar, fat and cute."""
    h = 1.2 if tall else 0.8
    r = 0.36 if tall else 0.42
    Q.m.lathe(T(0, 0, 0), [(r * 0.55, 0.0), (r * 0.95, h * 0.22), (r, h * 0.5), (r * 0.8, h * 0.78), (r * 0.48, h * 0.9), (r * 0.6, h)], 10, "pr_clay", cap_bot="pr_clay")
    Q.m.lathe(T(0, 0, 0), [(r * 0.5, h * 0.88), (r * 0.44, h * 0.9)], 10, "pr_clay", cap_top="pr_paint_black")
    Q.m.lathe(T(0, 0, 0), [(r * 1.01, h * 0.44), (r * 1.01, h * 0.56)], 10, "pr_paint_white" if tall else "pr_paint_teal")


def prop_column_stub(Q, seed=0):
    rng = random.Random(seed)
    Q.m.lathe(T(0, 0, 0), [(0.78, -0.3), (0.78, 0.3), (0.62, 0.5)], 12, "pr_stone_worn", cap_top="pr_stone_worn")
    h = rng.uniform(1.0, 1.9)
    Q.m.lathe(T(0, 0, 0), [(0.55, 0.45), (0.54, 0.45 + h)], 12, "pr_stone", cap_top="pr_stone_worn")
    Q.m.block(T(0.2, 0.1, 0, 0.6), 0.3, 0.26, 0.4 + h, 0.62 + h, "pr_stone_worn", taper=0.7)
    Q.m.lathe(T(1.5, 0.4, 0.5) @ Matrix.Rotation(1.45, 4, "Y"), [(0.54, -0.6), (0.54, 0.6)], 12, "pr_stone", cap_top="pr_stone_worn", cap_bot="pr_stone_worn")   # a fallen drum beside it


def prop_block(Q, seed=0):
    rng = random.Random(seed)
    Q.m.block(T(0, 0, 0, 0.2) @ Matrix.Rotation(0.12, 4, "X"), 0.75, 0.5, -0.25, 0.62, "pr_ashlar")
    Q.m.block(T(0.9, 0.7, 0, 1.1) @ Matrix.Rotation(-0.2, 4, "Y"), 0.45, 0.4, -0.2, 0.4, "pr_ashlar_dark")


def prop_lantern(Q, seed=0):
    """A capiz lantern on a bamboo-thick post."""
    Q.m.tube([(0, 0, -0.3), (0.05, 0, 2.6), (0.5, 0, 2.95)], 0.07, "pr_timber", n=6)
    Q.m.lathe(T(0.5, 0, 0), [(0.1, 2.2), (0.3, 2.34), (0.34, 2.6), (0.22, 2.8), (0.06, 2.86)], 8, "pr_capiz", cap_bot="pr_capiz")
    Q.r.tube([(0.5, 0, 2.84), (0.5, 0, 2.97)], 0.02, "pr_rope", n=4)


def plant_tuft(Q, seed=0):
    """Dry grass: a fan of blade cards, two straw tints."""
    rng = random.Random(seed)
    for k in range(9):
        a = math.tau * k / 9 + rng.uniform(-0.2, 0.2)
        out = Vector((math.cos(a), math.sin(a), 0))
        h = rng.uniform(0.6, 1.05)
        lean = rng.uniform(0.15, 0.5)
        Q.c.card(out * 0.08 - UP * 0.05, Vector((-out.y, out.x, 0)) * 0.11, UP * h + out * (h * lean), "pr_grass_light" if k % 3 else "pr_grass_dark")


def plant_shrub(Q, seed=0):
    rng = random.Random(seed)
    for k in range(3):
        a = math.tau * k / 3 + seed
        r = rng.uniform(0.45, 0.7)
        c = Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, r * 0.8 + 0.1 * k))
        Q.r.tube([(0, 0, -0.2), c * 0.5, c], 0.05, "pr_vine", n=5, r1=0.02)
        leaf_clump(Q.c, c, r, 14, rng)


PROP_FN = {"prop_checkpoint": prop_checkpoint, "prop_jar_a": lambda Q, seed=0: prop_jar(Q, seed, False), "prop_jar_b": lambda Q, seed=0: prop_jar(Q, seed, True),
           "prop_column_stub": prop_column_stub, "prop_block": prop_block, "prop_lantern": prop_lantern,
           "plant_tuft_a": lambda Q, seed=0: plant_tuft(Q, 1), "plant_tuft_b": lambda Q, seed=0: plant_tuft(Q, 5),
           "plant_shrub_a": lambda Q, seed=0: plant_shrub(Q, 2), "plant_shrub_b": lambda Q, seed=0: plant_shrub(Q, 7)}
MOVER_FN = {"boulder_chain": mv_boulder_chain, "pendulum": mv_pendulum, "mill_wheel": mv_mill_wheel, "log": mv_log, "seesaw": mv_seesaw,
            "crumble_slab": mv_crumble_slab, "floating_rock": mv_floating_rock, "chain_anchor": mv_chain_anchor}
