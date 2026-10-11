"""Ilyas's (hero id `rafi`) hand companion: a water bangle round a silver wrist cuff, and the small fat fish in it.

  py -3 tools/build_hand_rafi.py

Owner, 2026-10-06, of the hands' effects: "the vfx and animations that the session is giving me is just bland 2d vfx
particles", and of the first finished companion: "a bit lacking on textures and styling". So nothing here is a quad
or a glow. Ilyas is the current, a Badjao boy off a boat deck, bare tattooed arms with a silver cuff on each wrist:
the water he pulls has gathered round each cuff as a chunky ring of lumps, and a fish has moved in.

THE PARTS (`RafiTideHand.cs` finds them by name):
  lump-0 .. lump-5   the bangle's six water lumps, round the cuff's axis. Each is built about its own middle with its
                     OUTER side turned away from the ring's middle (which is the model's origin), so the C# side reads
                     which way a lump was turned from where it sits. Two kinds alternate: a deep one with a foam cap,
                     and a pale one with a band of his current's indigo.
  fish               the body: fat, orange, a cream belly, two pale bands, a coral crest, pink cheeks. Nose to +z.
    fish-eye-l/-r    big white eyes with a dark pupil and a glint, bulging off the sides and turned half forward.
    fish-mouth       pouting lips round a dark mouth: it gapes to blow a bubble and to gasp.
    fish-tail        two lobes, pivot at the root of the tail.
    fish-fin-l/-r    stub paddles, pivot at the shoulder.
  bubble             the ball he blows.
  drop               one tear of water; the C# side copies it for the few that jump out of a splash.
  review-cuff        a silver block where the cuff is, ONLY so the review pictures show the bangle on something.
                     `RafiTideHand` hides it.

The ring lies in the model's xy plane and the arm runs away along -z, so the review's "front" is near enough the
player's own view of the wrist. Everything is at the size it is drawn at divided by `RafiTideHand.Scale` (4).
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from hand_companion_kit import Model, ellipsoid, join, lathe, mirror, moved, rounded  # noqa: E402

# The sixteen colours. The same list, in the same order, is `RafiTideHand.Palette`.
PALETTE = [
    "#1A5257",  # 0  water, deep: the dark end of RafiWaterVisual's teal ramp
    "#1F8387",  # 1  water, middle
    "#7AD1C7",  # 2  water, pale
    "#CCF0DB",  # 3  foam, the pale end of the ramp
    "#F6FCF8",  # 4  foam white
    "#6065E6",  # 5  the current's indigo
    "#A2A5FF",  # 6  the current's pale indigo
    "#FF8A3C",  # 7  the fish
    "#E2553A",  # 8  the fish's fins and crest
    "#FFE2B0",  # 9  the fish's belly
    "#FFF6E8",  # 10 the fish's bands and fin tips
    "#FFFFFF",  # 11 eye white
    "#1B1630",  # 12 pupil
    "#FF9FA0",  # 13 cheek
    "#7A2338",  # 14 inside the mouth
    "#B9C0CC",  # 15 silver (the review's cuff only)
]
(DEEP, MID, PALE, FOAM, WHITE, INDIGO, INDIGO_PALE, ORANGE, CORAL, BELLY, BAND, EYE, PUPIL, CHEEK, GULLET, SILVER) = range(16)

SCALE = 4.0
# The cuff, measured off RosterArms/rafi_left (0.61 by 0.63 across, 0.09 long), at the model's size.
CUFF_X, CUFF_Y, CUFF_LONG = .305 / SCALE, .313 / SCALE, .046 / SCALE
ROUND, SEAT = .37 / SCALE, .03 / SCALE


def surface(f, rings, seg, slot_of):
    """A closed skin from `f(t, u)` (t 0..1 from one pole to the other, u round it), split into one mesh a colour by
    `slot_of(ring, column)`. The colours meet flush because the pieces share the same points and normals: a band or a
    belly is PAINTED on the form, not a shell laid over it. Normals are measured, and every triangle is turned to face
    out, so no piece can come out inside out."""
    pts = np.zeros((rings + 1, seg + 1, 3), np.float64)
    for r in range(rings + 1):
        for s in range(seg + 1):
            pts[r, s] = f(r / rings, 2 * math.pi * (s % seg) / seg)
    middle = pts.reshape(-1, 3).mean(axis=0)
    nrm = np.zeros_like(pts)
    e = 1e-4
    for r in range(rings + 1):
        for s in range(seg + 1):
            t, u = r / rings, 2 * math.pi * (s % seg) / seg
            if r in (0, rings):
                near = pts[1 if r == 0 else rings - 1, :seg].mean(axis=0)
                n = pts[r, s] - near
            else:
                n = np.cross(np.array(f(t, u + e)) - np.array(f(t, u - e)), np.array(f(t + e, u)) - np.array(f(t - e, u)))
                if np.dot(n, pts[r, s] - middle) < 0:
                    n = -n
            nrm[r, s] = n / max(np.linalg.norm(n), 1e-9)
    by_slot = {}
    for r in range(rings):
        for s in range(seg):
            a, b, c, d = (r, s), (r, s + 1), (r + 1, s), (r + 1, s + 1)
            quads = []
            if r > 0:
                quads.append((a, b, c))
            if r < rings - 1:
                quads.append((b, d, c))
            for tri in quads:
                p = [pts[i] for i in tri]
                face = np.cross(p[1] - p[0], p[2] - p[0])
                if np.dot(face, sum(nrm[i] for i in tri)) < 0:
                    tri = (tri[0], tri[2], tri[1])
                by_slot.setdefault(slot_of(r, s), []).append(tri)
    pieces = []
    for slot, tris in by_slot.items():
        index, pos, nor, out = {}, [], [], []
        for tri in tris:
            row = []
            for i in tri:
                if i not in index:
                    index[i] = len(pos); pos.append(pts[i]); nor.append(nrm[i])
                row.append(index[i])
            out.append(row)
        pieces.append(((np.array(pos, np.float32), np.array(nor, np.float32), np.array(out, np.uint32)), slot))
    return pieces


def turned(pieces, **how):
    return [(moved(mesh, **how), slot) for mesh, slot in pieces]


# ------------------------------------------------------------------ the water

LUMP = (.049, .034, .030)          # half-size: along the ring, outward, along the arm


def lump(kind):
    """One lump of the bangle, about its own middle, its outer side up (+y). A fat bean with a dimple each end so
    six of them read as six and not as a tube."""
    hx, hy, hz = LUMP
    rings, seg = 7, 10

    def f(t, u):
        v = math.pi * t
        swell = 1 + .10 * math.sin(v) ** 2 * math.cos(2 * u)          # a little squarer across the cuff than round
        return (hx * math.sin(v) * math.cos(u) * swell, hy * math.cos(v) * (1.0 if t < .5 else .8), hz * math.sin(v) * math.sin(u))

    if kind == 0:
        # Deep water with a foam cap that slops over one side, and a dark keel.
        slot = lambda r, s: FOAM if r < 2 or (r == 2 and s % 2 == 0) else (MID if r < 5 else DEEP)
        body = surface(f, rings, seg, slot)
        shine = [(ellipsoid((.009, .005, .0055), at=(-.020, .015, .017), seg=6, rings=3), PALE),
                 (ellipsoid((.004, .0035, .0035), at=(.018, .012, .020), seg=6, rings=3), PALE)]
    else:
        # Pale water with a band of the current round it, and a mid-teal keel.
        slot = lambda r, s: PALE if r < 2 else (INDIGO_PALE if r == 2 else (INDIGO if r == 3 else MID))
        body = surface(f, rings, seg, slot)
        shine = [(ellipsoid((.010, .005, .0055), at=(.014, .025, .012), seg=6, rings=3), WHITE),
                 (ellipsoid((.004, .0035, .0035), at=(-.016, .023, .013), seg=6, rings=3), WHITE)]
    return body + shine


def ring_reach(angle):
    """How far the cuff's face is from its middle, looking `angle` round from straight up."""
    sx, cy = abs(math.sin(angle)), abs(math.cos(angle))
    return min(1.0 / max(sx / CUFF_X, cy / CUFF_Y, 1e-6), ROUND)


def drop():
    body = lathe([(0, -.011), (.006, -.009), (.0085, -.004), (.008, .001), (.005, .007), (.002, .012), (0, .016)], seg=6)
    return [(body, PALE), (ellipsoid((.003, .003, .002), at=(-.003, -.002, .0068), seg=6, rings=3), WHITE)]


def bubble():
    r = .0125

    def f(t, u):
        v = math.pi * t
        return (r * math.sin(v) * math.cos(u), r * math.cos(v), r * math.sin(v) * math.sin(u))

    # A pale ball with a darker floor and one ring of his indigo: a bubble drawn in flat colour.
    body = surface(f, 7, 8, lambda ring, s: FOAM if ring < 4 else (INDIGO_PALE if ring == 4 else PALE))
    return body + [(ellipsoid((.0045, .0035, .0025), at=(-.0045, .005, .0098), seg=6, rings=3), WHITE)]


# ------------------------------------------------------------------ the fish

LONG, WIDE, TALL = .031, .0235, .0255


def fish_body():
    rings, seg = 14, 12

    def f(t, u):
        v = math.pi * t
        fat = math.sin(v) ** .68
        taper = 1 - .46 * (max(0.0, (t - .5) / .5) ** 1.4)                 # narrows to the root of the tail
        arch = .0028 * math.sin(v)                                        # a humped back
        y = TALL * fat * taper * math.sin(u)
        if y < 0:
            y *= .9                                                        # a flatter belly: he sits, he does not roll
        return (WIDE * fat * taper * math.cos(u), y + arch, LONG * math.cos(v))

    def slot(r, s):
        t = (r + .5) / rings
        low = math.sin(2 * math.pi * (s + .5) / seg) < -.42
        if r in (6, 7):
            return BAND
        if r == 11:
            return BAND
        if r in (5, 8):
            return CORAL if not low else BELLY                             # the band's dark edges
        if low and t < .8:
            return BELLY
        return ORANGE

    body = surface(f, rings, seg, slot)
    # The crest: three coral humps down his back, the tallest first.
    for k, (z, h) in enumerate(((.006, .011), (-.004, .0095), (-.013, .007))):
        body.append((moved(ellipsoid((.0036, h, .0068), seg=6, rings=4), at=(0, .0235 - k * .0018, z), turn=(-24, 0, 0)), CORAL))
        body.append((moved(ellipsoid((.0026, .0032, .0034), seg=6, rings=3), at=(0, .0235 - k * .0018 + h * .78, z - h * .36)), BAND))
    # Cheeks, under the eyes.
    cheek = ellipsoid((.0028, .0042, .0052), at=(.0212, -.0055, .0125), seg=6, rings=3)
    body += [(cheek, CHEEK), (mirror(cheek), CHEEK)]
    return body


def eye():
    """One eye, looking along +z, about its own middle. A fat white ball, a big pupil set a little up and in (he is
    always eyeing something), two glints."""
    return [(ellipsoid((.0098, .0104, .0088), seg=10, rings=6), EYE),
            (ellipsoid((.0060, .0070, .0034), at=(.0012, .0004, .0062), seg=8, rings=4), PUPIL),
            (ellipsoid((.0026, .0028, .0016), at=(-.0018, .0038, .0092), seg=6, rings=3), EYE),
            # the heavy upper lid that makes the look cheeky rather than startled
            (moved(ellipsoid((.0104, .0048, .0092), seg=6, rings=4), at=(0, .0074, 0), turn=(0, 0, -12)), ORANGE)]


def mouth():
    lips = ellipsoid((.0076, .0052, .0040), seg=8, rings=4)
    return [(lips, CORAL), (ellipsoid((.0042, .0024, .0022), at=(0, -.0002, .0024), seg=6, rings=3), GULLET)]


def tail():
    """Two fat lobes off the root of the tail (the part's origin), each tipped pale."""
    out = []
    for sign in (1, -1):
        lobe = moved(ellipsoid((.0046, .0078, .0148), seg=6, rings=4), at=(0, sign * .0092, -.0118), turn=(sign * 38, 0, 0))
        tip = moved(ellipsoid((.0036, .0056, .0062), seg=6, rings=3), at=(0, sign * .0158, -.0202), turn=(sign * 38, 0, 0))
        out += [(lobe, CORAL), (tip, BAND)]
    out.append((ellipsoid((.0052, .0064, .0056), at=(0, 0, -.001), seg=6, rings=3), ORANGE))
    return out


def fin():
    """The left paddle (+x), about its shoulder."""
    blade = moved(ellipsoid((.0098, .0034, .0066), seg=6, rings=4), at=(.0072, -.0022, -.0028), turn=(0, 22, -24))
    tip = moved(ellipsoid((.0044, .0028, .0046), seg=6, rings=3), at=(.0136, -.0052, -.0056), turn=(0, 22, -24))
    return [(blade, CORAL), (tip, BAND)]


def flip(pieces):
    return [(mirror(mesh), slot) for mesh, slot in pieces]


def build():
    model = Model("rafi")
    cuff = moved(rounded((CUFF_X, CUFF_Y, CUFF_LONG), .012, seg=8, rings=4), at=(0, 0, 0))
    model.add("review-cuff", [(cuff, SILVER)])

    top = 0.0
    for k in range(6):
        angle = math.radians(60 * k)
        reach = ring_reach(angle) + SEAT
        at = (math.sin(angle) * reach, math.cos(angle) * reach, 0.0)
        if k == 0:
            top = at[1] + LUMP[1]
        # Turned about the arm's axis so its outer side points away from the ring's middle.
        model.add("lump-%d" % k, turned(lump(k % 2), turn=(0, 0, -60 * k)), at=at)

    # He floats in the top lump, half out of it.
    home = (0.0, top + .008, 0.0)
    model.add("fish", fish_body(), at=home)
    left = turned(eye(), turn=(-8, 58, 0))
    model.add("fish-eye-l", left, at=(.0168, .0085, .0170), parent="fish")
    model.add("fish-eye-r", flip(left), at=(-.0168, .0085, .0170), parent="fish")
    model.add("fish-mouth", mouth(), at=(0, -.0052, .0292), parent="fish")
    model.add("fish-tail", tail(), at=(0, .0012, -.0288), parent="fish")
    model.add("fish-fin-l", fin(), at=(.0196, -.0092, .0016), parent="fish")
    model.add("fish-fin-r", flip(fin()), at=(-.0196, -.0092, .0016), parent="fish")

    model.add("bubble", bubble(), at=(.004, home[1] + .058, .034))
    model.add("drop", drop(), at=(.118, .066, 0))
    model.write(PALETTE)


if __name__ == "__main__":
    build()
