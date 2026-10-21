"""Ilyas's (hero id `rafi`) BARE hands: the sea still clinging to him. A slim band of water at each wrist, a few fat
beads on the skin.

  py -3 tools/build_hands_rafi.py                 the plain model the game loads (run this LAST)
  py -3 tools/build_hands_rafi.py --pose=rest     the same pieces laid on a stand-in arm as he stands, to look at
  py -3 tools/build_hands_rafi.py --pose=fall     ... as he falls: the band trailing up as a ribbon, the beads lifting
  py -3 tools/build_hands_rafi.py --pose=slipper  ... the right arm with a slipper: the band drawn back up the forearm
  py -3 tools/build_hands_rafi.py --pose=land     ... the splash crown of a landing

Owner, 2026-10-06, of round one's creatures: "i like it but we were trying to reserve the pet idea only for nemu...
so i need something for the bare hands". And round one's water lumps came out far too big in the game and hid his
silver cuffs. So there is no fish here and no bangle on the cuff: the water is SMALL and thin, it rides the wrist on
the hand's side of the cuff, and the cuff is left bare.

THE PARTS (`RafiWaterHands.cs` finds them by name and copies them: eight lumps and four beads an arm, six drops):
  lump-a       one lump of the band, about its own middle: +y is its outer side, x runs round the wrist, z along the
               arm. Teal water, a foam cap that slops down one side, a dark keel against the skin, a glint.
  lump-b       the other kind (they alternate round the wrist): a smaller cap and a line of his current's indigo.
  bead-a       a fat bead of water, its ORIGIN AT ITS ROOT on the skin, +y out of the skin. It grows from nothing by
               scaling out from there. Pale, a teal foot, a foam glint.
  bead-b       a longer bead with a foot of the current's indigo.
  drop         one tear, point up (+y): the few that jump out of a splash or let go of a knuckle.
  review-arm   a stand-in forearm, cuff and block hand, ONLY in the posed review models.

Everything is typed at the size it is drawn at divided by `RafiWaterHands.Scale` (4). The poses are laid out in the
arm's own space (y from the elbow to the hand) with the side the player sees turned to +z, so the review's "front" is
near the player's own look down the arm.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from hand_companion_kit import Model, ellipsoid, lathe, moved, rounded  # noqa: E402

# The sixteen colours. The same list, in the same order, is `RafiWaterHands.Palette`.
PALETTE = [
    "#1A5257",  # 0  water, deep: the dark end of RafiWaterVisual's teal ramp
    "#1F8387",  # 1  water, middle
    "#7AD1C7",  # 2  water, pale
    "#CCF0DB",  # 3  foam, the pale end of the ramp
    "#F6FCF8",  # 4  foam white
    "#6065E6",  # 5  the current's indigo
    "#A2A5FF",  # 6  the current's pale indigo
    "#35A9A8",  # 7  water, the bright teal between middle and pale
    "#B9C0CC",  # 8  silver (the review's cuff only)
    "#A8683F",  # 9  skin (the review's arm only)
    "#8A5230",  # 10 skin, shaded (the review's arm only)
    "#2B2A3A",  # 11 tattoo ink (the review's arm only)
    "#FFFFFF",  # 12 spare
    "#FFFFFF",  # 13 spare
    "#FFFFFF",  # 14 spare
    "#FFFFFF",  # 15 spare
]
(DEEP, MID, PALE, FOAM, WHITE, INDIGO, INDIGO_PALE, BRIGHT, SILVER, SKIN, SKIN_SHADE, TATTOO) = range(12)

SCALE = 4.0

# His arm's half-width (x) and half-depth (z) by height, measured off RosterArms/rafi_left: bare forearm, the armlet
# band, forearm, the silver cuff, the block hand tapering to its tip. `RafiWaterHands` carries the same table.
PROFILE_Y = [.20, .295, .315, .345, .365, .435, .455, .545, .565, .596, .78, .84, 1.20]
PROFILE_X = [.2025, .2025, .256, .256, .2025, .2025, .305, .305, .2485, .2485, .1985, .1385, .1385]
PROFILE_Z = [.212, .212, .265, .265, .212, .212, .313, .313, .277, .277, .254, .194, .194]
WRIST_Y, FOREARM_Y = .588, .40
LUMPS = 8
# The beads' roots: height along the arm, degrees round from the side the player sees, size.
BEADS = [(.41, -24, 1.0), (.25, 30, .85), (.69, 20, .9), (.76, -28, .7)]
# Where each lump goes in the ribbon of a fall, counted up from the wrist (the top lump leads).
STACK = [7, 6, 4, 2, 0, 1, 3, 5]


def surface(f, rings, seg, slot_of):
    """A closed skin from `f(t, u)` (t 0..1 from one pole to the other, u round it), split into one mesh a colour by
    `slot_of(ring, column)`. The colours meet flush because the pieces share the same points and normals: a cap or a
    stripe is PAINTED on the form, not a shell laid over it. Every triangle is turned to face out."""
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


# ------------------------------------------------------------------ the water

LUMP = (.128 / SCALE, .050 / SCALE, .046 / SCALE)      # half-size: round the wrist, outward, along the arm


def lump(kind):
    """One lump of the band, its outer side up (+y). A low fat bean, flatter underneath where it lies on the wrist,
    pinched at each end so eight of them read as eight and not as a tube."""
    hx, hy, hz = LUMP
    rings, seg = 8, 12

    def f(t, u):
        v = math.pi * t
        pinch = 1 - .16 * abs(math.cos(u)) ** 3 * math.sin(v)          # narrower along the arm towards each end
        return (hx * math.sin(v) ** .5 * math.cos(u), hy * math.cos(v) * (1.0 if t < .5 else .55), hz * math.sin(v) ** .8 * math.sin(u) * pinch)

    # The foam is a crest along the lump's HAND side (-z), not a spot on its crown: a spot made each lump a gemstone
    # and the band a bracelet. A crest line running lump to lump makes the eight of them one small wave round the wrist.
    def side(s):
        return math.sin(2 * math.pi * (s + .5) / seg)                 # -1 the hand's side, +1 the cuff's side

    if kind == 0:
        # A broad crest that slops over the crown, bright water, a deep foot against the cuff.
        def slot(r, s):
            if r < 6 and (side(s) < -.45 or (r < 2 and side(s) < .3)):
                return FOAM
            if r >= 6 or side(s) > .8:
                return DEEP
            return BRIGHT if r < 4 else MID
        glint = [(ellipsoid((.0046, .0014, .0018), at=(-.011, .0112, .0034), seg=6, rings=3), WHITE)]
    else:
        # A thin crest, pale water, a line of the current's indigo under it, a deep foot.
        def slot(r, s):
            if r < 6 and side(s) < -.75:
                return FOAM
            if r < 6 and side(s) < -.2:
                return INDIGO_PALE
            if r >= 6 or side(s) > .8:
                return DEEP
            return PALE if r < 4 else MID
        glint = [(ellipsoid((.0040, .0014, .0018), at=(.010, .0112, .0030), seg=6, rings=3), WHITE)]
    return surface(f, rings, seg, slot) + glint


def bead(kind):
    """A fat bead of water rooted at the origin, standing out of the skin along +y: a dome with a foot that spreads on
    the skin, the way a drop sits on an oiled arm."""
    r, h = (.052 / SCALE, .046 / SCALE) if kind == 0 else (.046 / SCALE, .040 / SCALE)
    long = 1.0 if kind == 0 else 1.3
    rings, seg = 7, 10

    def f(t, u):
        # From the crown down to the foot, and a small sunk underside so nothing is open against the skin.
        if t < .8:
            v = (t / .8) * (math.pi * .5)
            rad, y = r * math.sin(v) ** .85, h * math.cos(v)
        else:
            k = (t - .8) / .2
            rad, y = r * (1 + .10 * math.sin(k * math.pi)) * (1 - k * k * .999), -h * .10 * k
        return (rad * math.cos(u), y, rad * math.sin(u) * long)

    if kind == 0:
        slot = lambda ring, s: PALE if ring < 3 else (BRIGHT if ring < 5 else MID)
        glint = [(ellipsoid((.0034, .0016, .0024), at=(-.0046, h * .80, .0030), seg=6, rings=3), WHITE),
                 (ellipsoid((.0014, .0010, .0012), at=(.0040, h * .78, -.0040), seg=5, rings=3), FOAM)]
    else:
        slot = lambda ring, s: PALE if ring < 3 else (INDIGO_PALE if ring < 5 else INDIGO)
        glint = [(ellipsoid((.0028, .0015, .0030), at=(.0036, h * .80, .0040), seg=6, rings=3), WHITE)]
    return surface(f, rings, seg, slot) + glint


def drop():
    body = lathe([(0, -.0085), (.0048, -.0068), (.0066, -.003), (.0062, .001), (.004, .0055), (.0016, .0095), (0, .0125)], seg=7)
    return [(body, PALE), (ellipsoid((.0024, .0024, .0016), at=(-.0024, -.0018, .0052), seg=6, rings=3), WHITE)]


# ------------------------------------------------------------------ the review's poses

def half(y):
    return float(np.interp(y, PROFILE_Y, PROFILE_X)) / SCALE, float(np.interp(y, PROFILE_Y, PROFILE_Z)) / SCALE


def reach(y, angle):
    """How far the arm's face is from its middle at height `y`, looking `angle` round from the side the player sees."""
    hx, hz = half(y)
    return min(1.0 / max(abs(math.sin(angle)) / hx, abs(math.cos(angle)) / hz, 1e-6), 1.2 * hz)


def placed(pieces, at, up, back, size=1.0, shape=(1, 1, 1)):
    """`pieces` (typed with +y out and +z down the arm) with +y laid along `up` and +z along `back`, scaled, moved."""
    up = np.array(up, np.float64); up /= np.linalg.norm(up)
    back = np.array(back, np.float64); back = back - up * np.dot(back, up); back /= np.linalg.norm(back)
    m = np.array([np.cross(up, back), up, back]).T
    s = np.array(shape, np.float64)
    out = []
    for (p, n, t), slot in pieces:
        n2 = (n / s) @ m.T
        n2 /= np.maximum(np.linalg.norm(n2, axis=1, keepdims=True), 1e-9)
        out.append((((((p * s * size) @ m.T) + np.array(at)).astype(np.float32), n2.astype(np.float32), t), slot))
    return out


def review_arm():
    q = 1 / SCALE
    forearm = rounded((.2025 * q, .19 * q, .212 * q), .012, at=(0, .30 * q, 0), seg=10, rings=6)
    armlet = rounded((.256 * q, .022 * q, .265 * q), .004, at=(0, .33 * q, 0), seg=10, rings=4)
    cuff = rounded((.305 * q, .046 * q, .313 * q), .007, at=(0, .50 * q, 0), seg=10, rings=4)
    wrist = rounded((.19 * q, .03 * q, .21 * q), .006, at=(0, .565 * q, 0), seg=8, rings=4)
    hand = rounded((.235 * q, .125 * q, .268 * q), .022, at=(0, .715 * q, 0), seg=10, rings=6)
    return [(forearm, SKIN), (armlet, TATTOO), (cuff, SILVER), (wrist, SKIN_SHADE), (hand, SKIN)]


def pose(model, which):
    q = 1 / SCALE
    model.add("review-arm", review_arm())
    axis = np.array((0.0, 1.0, 0.0))
    skyward = np.array((0.0, .5, .85)); skyward /= np.linalg.norm(skyward)        # up the player's screen
    band_y = FOREARM_Y if which == "slipper" else WRIST_Y
    tight = .55 if which == "slipper" else 0.0
    for k in range(LUMPS):
        a = math.radians(45 * k)
        out = np.array((math.sin(a), 0.0, math.cos(a)))
        swell = math.sin(-a + .6)
        size = (1 + .14 * swell) * (1 - .4 * tight)
        lift = .012 * q + .008 * q * swell
        shape = (1, 1, 1)
        if which == "land":
            lift += (.13 + .05 * math.sin(k * 2.3)) * q
            shape = (.9, 1.2, .9)
        at = axis * band_y * q + out * (reach(band_y, a) + lift)
        up, back = out, -axis
        if which == "fall":
            j = STACK[k]; share = j / (LUMPS - 1)
            foot = axis * band_y * q + np.array((0, 0, reach(band_y, 0)))
            at = foot + skyward * ((.05 + j * .038) * q) + np.array((math.sin(-j * .9) * .045 * share * q, 0, 0))
            up, back = (0, -.85, .5), (1, 0, 0)                 # its crown to the eye, its length up the ribbon
            size *= .95 + (.5 - .95) * share
            shape = (.55, 1.0, .9)
        model.add("pose-lump-%d" % k, placed(lump(k % 2), at, up, back, size, shape))
    for i, (y, turn, size) in enumerate(BEADS):
        if which == "slipper" and i >= 2:
            continue                                                             # the grip is clear
        a = math.radians(turn)
        out = np.array((math.sin(a), 0.0, math.cos(a)))
        at = axis * y * q + out * reach(y, a)
        shape = (1, 1, 1)
        if which == "fall":
            at = at + skyward * ((.07 + .03 * i) * q); shape = (.8, 1.35, .8)
        if which == "land" and i > 0:
            size *= (.55, .25, 0.0)[i - 1]                                       # re-forming one by one
            if size <= 0:
                continue
        model.add("pose-bead-%d" % i, placed(bead(i % 2), at, out, -axis, size, shape))
    if which == "land":
        for i, (dx, dy) in enumerate(((-.32, .1), (.05, .26), (.34, .14))):
            model.add("pose-drop-%d" % i, moved(drop()[0][0], at=(dx * q, WRIST_Y * q + dy * q, .34 * q)), slot=PALE)


def build(which=None):
    model = Model("rafi_hands")
    if which:
        pose(model, which)
    else:
        model.add("lump-a", lump(0), at=(-.05, 0, 0))
        model.add("lump-b", lump(1), at=(.05, 0, 0))
        model.add("bead-a", bead(0), at=(-.02, .04, 0))
        model.add("bead-b", bead(1), at=(.02, .04, 0))
        model.add("drop", drop(), at=(0, .08, 0))
    model.write(PALETTE)


if __name__ == "__main__":
    build(next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--pose=")), None))
