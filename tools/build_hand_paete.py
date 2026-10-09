"""Paete's first-person hand companion: what GROWS on his braided forearms (`PaeteSproutHand.cs`).

Owner, 2026-10-06: "unique per-hero touches such as Paete's vines moving". He has no fist: his forearms are four
braided vine strands that end in points. So his arms themselves are the companion, and this is what is alive on them:

  * a TENDRIL near each braid's tip that works like a finger: five short tapered segments, each its own part with its
    origin at its joint, so the C# curls and uncurls it joint after joint with a lag. A bark socket where it leaves
    the braid, dark joint bands, a leaflet, and a pale young bulb for a fingertip;
  * a KNOT of bark at each wrist with a vine band and two moss studs, and a PAIR OF LEAVES hinged at it (one young
    green, one deep green, each with a rib in the other tone);
  * on the left braid one SAMPAGUITA BUD, the little character: a two-segment stalk, a green calyx, five rounded white
    petals with a cream shade (each hinged at the heart's rim), and a yellow heart with an amber rim, blush dots and a
    tiny face (open eyes, sleeping eyes, a mouth).

  py -3 tools/build_hand_paete.py                 the model the game loads (every part unturned, laid out tidy)
  py -3 tools/build_hand_paete.py --pose=idle     the SAME parts posed on two stand-in braids, to LOOK at only
                                                  (poses: idle, fall, tagged, coil, sprint). Run without --pose after.

A .glb node written by the kit carries a place and no turn, so the game's model is every part in its unturned rest.
The poses here bake the same joint sums `PaeteSproutHand.Step` does, so the review shows what the code will ask for.
Everything is typed in the ARMS' units (a fist is 0.3 across) and written at 1 / 4.4 (the class spawns it at 4.4).
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from hand_companion_kit import Model, ellipsoid, join, lathe, moved, slab, tube  # noqa: E402

K = 1.0 / 4.4
GROW = 5.6 / 4.4          # the class spawns it at 5.6, so in the arms' space every length here is this much longer

# The sixteen colours: the same list, in the same order, as `PaeteSproutHand.Palette`.
PALETTE = ["#8C6440", "#553A22", "#B08450", "#557A26", "#4F6B1F", "#A8CC52", "#C9E27A", "#6A962E",
           "#3F5F2C", "#FFFDF6", "#EBDCB4", "#F4C531", "#D98E2B", "#3A2414", "#5E7F24", "#F2A65E"]
BARK, BARK_DARK, BARK_LIT, VINE, VINE_DARK, YOUNG, YOUNG_LIT, DEEP = range(8)
DEEP_DARK, PETAL, CREAM, HEART, AMBER, INK, MOSS, BLUSH = range(8, 16)

# ------------------------------------------------------------------ the tendril: five segments, a finger

SEG_LEN = [.074, .066, .058, .050, .043]
SEG_RAD = [.034, .030, .0255, .0215, .018, .0145]
BEND_SHARE = [.35, .7, 1.0, 1.25, 1.5]          # how much of the curl each joint takes: the tip curls tightest


def ring(radius, half, bulge, seg=5, y=0.0):
    """A band round the y axis: a joint ring, a socket."""
    return lathe([(radius * .9, y - half), (radius * bulge, y), (radius * .9, y + half)], seg=seg)


def small_leaf(length, width, thick, seg=4):
    """A leaf blade along +y from its stem at the origin, flat in z."""
    prof = [(0, 0), (1.0, .45), (.7, .78), (0, 1.0)]
    return lathe([(r * width * .5, y * length) for r, y in prof], seg=seg, squash_z=thick / width)


def segment(i):
    length, r0, r1 = SEG_LEN[i], SEG_RAD[i], SEG_RAD[i + 1]
    body = tube([(0, 0, 0), (0, length, 0)], [r0, r1], seg=5)
    pieces = [(body, VINE if i % 2 == 0 else MOSS)]
    if i == 0:
        # The socket the tendril grows out of: bark with a pale lip.
        pieces.append((ring(r0 * 1.55, .016, 1.1, seg=6, y=-.004), BARK_DARK))
    elif i in (2, 4):
        pieces.append((ring(r0 * 1.14, .0075, 1.18), VINE_DARK))
    if i == 1:
        pieces.append((moved(small_leaf(.062, .036, .012), at=(r0 * .7, length * .35, 0), turn=(0, 0, -58)), YOUNG))
    if i == 3:
        # A thorn nub on the other side, the way his vines carry them.
        pieces.append((moved(lathe([(.010, 0), (.006, .012), (0, .024)], seg=4), at=(-r0 * .8, length * .5, 0), turn=(0, 0, 72)), BARK_LIT))
    if i == 4:
        # The fingertip: a pale young bulb with a paler pad.
        pieces.append((ellipsoid((.0235, .026, .0235), at=(0, length + .006, 0), seg=6, rings=4), YOUNG))
        pieces.append((ellipsoid((.012, .010, .012), at=(0, length + .027, 0), seg=4, rings=2), YOUNG_LIT))
    return pieces


# ------------------------------------------------------------------ the wrist: a knot and two leaves

LEAF_LEN = .235


def leaf(tone, rib):
    """A wrist leaf along +y from its hinge, flat in z, with a rib standing proud of both faces and two side veins."""
    prof = [(0, 0), (.012, .006), (.013, .034), (.050, .090), (.058, .135), (.036, .190), (0, LEAF_LEN)]
    blade = lathe(prof, seg=6, squash_z=.19)
    spine = moved(lathe([(0, .012), (.058, .110), (.02, .200), (0, .226)], seg=4), scale=(.14, 1, .31))
    pieces = [(blade, tone), (spine, rib)]
    for side in (-1, 1):
        vein = lathe([(.0075, 0), (0, .046)], seg=3, squash_z=1.7)
        pieces.append((moved(vein, at=(0, .092 + .02 * side, 0), turn=(0, 0, -52 * side)), rib))
    return pieces


def knot():
    """A burl of bark on the braid (its y is OUT of the braid) with a vine band and two moss studs."""
    return [(ellipsoid((.066, .046, .062), seg=7, rings=5), BARK),
            (ring(.070, .011, 1.1, seg=7, y=.004), VINE),
            (ellipsoid((.019, .015, .019), at=(.034, .032, -.026), seg=5, rings=3), YOUNG),
            (ellipsoid((.024, .014, .024), at=(0, .040, 0), seg=5, rings=3), BARK_LIT)]


# ------------------------------------------------------------------ the sampaguita bud (its face looks along +z)

PETAL_LEN, PETAL_ROOT = .092, .036
STALK_LEN = [.070, .062]
PETAL_ANGLE = [90 + 72 * k for k in range(5)]


def petal(angle):
    blade = lathe([(0, 0), (.022, .010), (.040, .046), (.034, .076), (0, PETAL_LEN)], seg=6, squash_z=.30)
    shade = ellipsoid((.019, .024, .0055), at=(0, .030, .0085), seg=5, rings=3)
    out = []
    for mesh, slot in ((blade, PETAL), (shade, CREAM)):
        out.append((moved(moved(mesh, turn=(10, 0, 0)), turn=(0, 0, angle - 90)), slot))
    return out


def petal_root(angle):
    a = math.radians(angle)
    return np.array([math.cos(a) * PETAL_ROOT, math.sin(a) * PETAL_ROOT, -.006])


def heart():
    rim = moved(ring(.046, .010, 1.1, seg=8), turn=(90, 0, 0), at=(0, 0, -.004))
    mouth = ellipsoid((.0062, .0042, .003), at=(0, -.012, .0305), seg=5, rings=3)
    return [(ellipsoid((.043, .043, .032), seg=8, rings=5), HEART), (rim, AMBER), (mouth, INK),
            (ellipsoid((.008, .0055, .003), at=(.026, -.006, .0245), seg=5, rings=3), BLUSH),
            (ellipsoid((.008, .0055, .003), at=(-.026, -.006, .0245), seg=5, rings=3), BLUSH)]


EYES_AT = np.array([0, .007, .028])


def eyes_open():
    out = []
    for s in (-1, 1):
        out.append((ellipsoid((.0095, .013, .005), at=(s * .0165, 0, 0), seg=5, rings=3), INK))
        out.append((ellipsoid((.0034, .004, .0022), at=(s * .0165 + .002, .005, .004), seg=4, rings=2), PETAL))
    return out


def eyes_shut():
    """Two sleeping lids: little arcs that hang like a smile."""
    out = []
    for s in (-1, 1):
        arc = tube([(s * .0165 - .010, .002, 0), (s * .0165, -.005, .002), (s * .0165 + .010, .002, 0)], .0034, seg=3)
        out.append((arc, INK))
    return out


def calyx():
    star = []
    for k in range(10):
        a = math.radians(90 + 36 + 36 * k)
        r = .072 if k % 2 == 0 else .040
        star.append((math.cos(a) * r, math.sin(a) * r))
    return [(slab(star, .012, at=(0, 0, -.021)), DEEP), (ellipsoid((.038, .038, .026), at=(0, 0, -.028), seg=6, rings=4), VINE_DARK)]


def stalk(i):
    r0, r1 = (.019, .016) if i == 0 else (.016, .0145)
    pieces = [(tube([(0, 0, 0), (0, STALK_LEN[i], 0)], [r0, r1], seg=5), VINE)]
    if i == 0:
        pieces.append((ring(r0 * 1.5, .011, 1.1), BARK_DARK))
        pieces.append((moved(small_leaf(.058, .032, .011), at=(-r0 * .6, .03, 0), turn=(0, 0, 62)), DEEP))
    else:
        pieces.append((ring(r0 * 1.14, .006, 1.16), VINE_DARK))
    return pieces


# ------------------------------------------------------------------ every part, and where it rests in the model

def parts():
    """name -> (pieces, rest place in the arms' units). The bud's parts rest ASSEMBLED round the heart, because the
    class reads each petal's side of the heart, and which way the face looks, from these places."""
    out = {}
    for s, x in (("l", -.30), ("r", .30)):
        y = .30
        for i in range(5):
            out["tendril_%s_%d" % (s, i)] = (segment(i), (x, y, 0))
            y += SEG_LEN[i]
        out["knot_" + s] = (knot(), (x, -.10, 0))
        out["leaf_%s_0" % s] = (leaf(YOUNG, DEEP), (x - .07, -.04, 0))
        out["leaf_%s_1" % s] = (leaf(DEEP, YOUNG), (x + .07, -.04, 0))
    h = np.array([0.0, .34, 0.0])
    out["bud_stalk_0"] = (stalk(0), (0, .05, 0))
    out["bud_stalk_1"] = (stalk(1), (0, .05 + STALK_LEN[0], 0))
    out["bud_heart"] = (heart(), tuple(h))
    out["bud_calyx"] = (calyx(), tuple(h))
    out["bud_eyes_open"] = (eyes_open(), tuple(h + EYES_AT))
    out["bud_eyes_shut"] = (eyes_shut(), tuple(h + EYES_AT))
    for k, angle in enumerate(PETAL_ANGLE):
        out["bud_petal_%d" % k] = (petal(angle), tuple(h + petal_root(angle)))
    return out


# ------------------------------------------------------------------ the review poses (the sums PaeteSproutHand makes)

def unit(v):
    v = np.array(v, float)
    return v / max(np.linalg.norm(v), 1e-9)


def frame(z, y):
    """Columns x, y, z of a turn whose z is `z` and whose y is as near `y` as it can be (Unity's LookRotation)."""
    z = unit(z)
    x = unit(np.cross(y, z))
    return np.stack([x, np.cross(z, x), z], axis=1)


def about(axis, degrees):
    a = unit(axis)
    c, s = math.cos(math.radians(degrees)), math.sin(math.radians(degrees))
    k = np.array([[0, -a[2], a[1]], [a[2], 0, -a[0]], [-a[1], a[0], 0]])
    return np.eye(3) * c + s * k + (1 - c) * np.outer(a, a)


POSES = {
    #          curl  long  tendril way            sweep lift  open  droop nod   shut
    "idle":   (40,   1.0,  "rest",                48,   22,   1.0,  8,    0,    False),
    "fall":   (-3,   1.35, "up",                  90,   6,    0.0,  0,    0,    False),
    "tagged": (9,    1.0,  "hang",                72,   -38,  0.0,  82,   0,    False),
    "coil":   (100,  .8,   "rest",                30,   2,    .2,   20,   0,    False),
    "sprint": (9,    1.05, "stream",              152,  10,   0.0,  -35,  0,    False),
    "sleep":  (52,   1.0,  "rest",                48,   16,   .45,  14,   30,   True),
}


def posed(model, pose):
    curl, long, way, sweep, lift, opened, droop, nod, shut = POSES[pose]
    up, back = np.array([0.0, 1, 0]), np.array([0.0, 0, 1])           # in the review the player is at +z
    table = parts()

    def put(name, at, turn, scale=(1, 1, 1)):
        pieces = []
        for mesh, slot in table[name][0]:
            p, n, t = moved(mesh, scale=tuple(v * GROW for v in scale))
            pieces.append(((((p @ turn.T + at) * K).astype(np.float32), (n @ turn.T).astype(np.float32), t), slot))
        model.add(name, pieces)

    for s, side in (("l", -1.0), ("r", 1.0)):
        elbow, tip = np.array([side * .52, -.50, .30]), np.array([side * .17, .20, -.10])
        along = unit(tip - elbow)

        def on_arm(y, axis=elbow, to=tip):
            return axis + (to - axis) * (y / .84)
        normal = up + back * .7
        normal = unit(normal - along * float(normal @ along))
        out = np.array([side, 0.0, 0])
        # the stand-in braid: review only, the game never sees it
        ys = [.36, .5, .65, .75, .80, .86]
        rs = [.25, .24, .19, .13, .065, .01]
        model.add("review_braid_" + s, [(moved(tube([on_arm(y) for y in ys], rs, seg=8), scale=(K, K, K)), BARK_DARK)])

        # the tendril
        way_dir = {"rest": along * .5 + up * .8 + out * .15, "up": up + along * .15, "hang": out * .8 - up * .6 + along * .2,
                   "stream": -along * .9 + up * .45 + back * .3}[way]
        e1 = unit(way_dir)
        toward = out + up * .25
        e2 = unit(toward - e1 * float(toward @ e1))
        w = np.cross(e1, e2)
        at, angle = on_arm(.79) + normal * .02, 0.0
        for i in range(5):
            angle += max(-30, min(150, curl * BEND_SHARE[i]))
            d = e1 * math.cos(math.radians(angle)) + e2 * math.sin(math.radians(angle))
            put("tendril_%s_%d" % (s, i), at, frame(w, d), (1, long, 1))
            at = at + d * SEG_LEN[i] * long * GROW

        # the knot and its leaves
        seat = on_arm(.46) + normal * .23
        put("knot_" + s, seat, frame(along, normal))
        across = unit(np.cross(normal, along))
        for j, sign in ((0, -1.0), (1, 1.0)):
            sw, lf = math.radians(sweep - 10 * j), math.radians(lift + 5 * j)
            flat = along * math.cos(sw) + across * sign * math.sin(sw)
            d = flat * math.cos(lf) + normal * math.sin(lf)
            face = normal * math.cos(lf) - flat * math.sin(lf)
            put("leaf_%s_%d" % (s, j), seat + normal * .036 + flat * .04, frame(face, d), (1.25 if pose == "fall" else 1, 1, 1))

        if s != "l":
            continue
        # the bud
        e1 = unit(normal + up * .5)
        toward = out * .7 - up + along * .2                            # it droops out over the braid's side, where it is seen
        e2 = unit(toward - e1 * float(toward @ e1))
        w = np.cross(e1, e2)
        at = on_arm(.66) + normal * .16
        d = e1
        for i in range(2):
            angle = droop * (.4 if i == 0 else 1.0)
            d = e1 * math.cos(math.radians(angle)) + e2 * math.sin(math.radians(angle))
            put("bud_stalk_%d" % i, at, frame(w, d))
            at = at + d * STALK_LEN[i] * GROW
        head = at + d * .030 * GROW
        look = about([1, 0, 0], nod) @ unit(back * .75 + up * .6)      # in the review a nod towards +z is a turn the other way
        hang = min(1.0, max(0.0, (droop - 40) / 40.0))
        look = unit(look * (1 - hang) + d * hang)
        turn = frame(look, up + along * .3)
        put("bud_heart", head, turn)
        put("bud_calyx", head, turn)
        put("bud_eyes_shut" if shut else "bud_eyes_open", head + turn @ EYES_AT * GROW, turn)
        for k, a in enumerate(PETAL_ANGLE):
            root = petal_root(a)
            r = unit([root[0], root[1], 0])
            shutting = (1 - opened) * 100.0
            put("bud_petal_%d" % k, head + turn @ root * GROW, turn @ about(np.cross(r, [0, 0, 1]), shutting))


def main():
    pose = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--pose=")), None)
    model = Model("paete")
    if pose:
        posed(model, pose)
        print("REVIEW POSE '%s': run again without --pose before the game loads it." % pose)
    else:
        for name, (pieces, at) in parts().items():
            model.add(name, [(moved(mesh, scale=(K, K, K)), slot) for mesh, slot in pieces], at=tuple(v * K for v in at))
    model.write(PALETTE)


if __name__ == "__main__":
    main()
