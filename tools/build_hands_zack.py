"""Zack's (ISAGANI's) bare first-person hands: the neon bands on his forearms are live, and he wears the current.

  py -3 tools/build_hands_zack.py                the game's model: every part, laid out apart
  py -3 tools/build_hands_zack.py --pose=rest    a review model: the parts on a stand-in left forearm, as `ZackArcHands`
                                                 poses them (rest, tick, climb, fall, cast, dead, sprint)

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/zack_hands.glb. `ZackArcHands.cs` finds every part by name
and places it on the arm every frame, so where a part stands in the plain file is only a layout for the review sheet.
A review pose is NOT the game's model: rebuild without `--pose` last.

No creature. What is here is jewellery on his own arm: a band ring in three tones (dead, softly lit, bright; the code
shows one and hides the other two), a row of three contact studs on a bar across his knuckles, a live cap for a stud,
one link of an arc, a pointed prong in two mirrored zigzags, and a small spark that pops off a knuckle.

Everything is modelled in the LEFT arm's own frame at 1/4.4 of its size (the arm runs up +y from the elbow, 0.84 long;
its +z face is the one the first-person camera sees, measured off `RosterArms/zack_left` and the arms' rest pose), and
every part is the same on both sides of x about its own origin, so the right arm takes the same pieces.
"""
import math
import sys

import numpy as np

from hand_companion_kit import Model, ellipsoid, join, moved, rounded, slab

PALETTE = [
    "#e8f53a",  # 0  neon: a bright band, an arc, a live stud
    "#f6ffa0",  # 1  pale neon: a bright band's rivets
    "#fffff0",  # 2  white-hot: an arc's core, a bright band's stripe
    "#c9951c",  # 3  ochre: rivets, stud collars, the contact plate (his jacket)
    "#8a6412",  # 4  dark ochre: the knuckle bar, a dead band's rivets
    "#1c2340",  # 5  navy: the slot in a stud's cap
    "#2b2f3a",  # 6  charcoal: a dead band's stripe
    "#5e6426",  # 7  a dead band
    "#ff9a3c",  # 8  hot orange: the root of a prong, the heart of the spark
    "#a3b31a",  # 9  a softly lit band
    "#b4b9a2",  # 10 steel: a stud's cap
    "#8e927c",  # 11 dull steel: a dead band's contact
    "#d4e22a",  # 12 a softly lit band's stripe
    "#c8713c",  # 13 his skin (the review's stand-in arm only)
    "#e2b81c",  # 14 his jacket (the review's stand-in arm only)
    "#10131f",  # 15 ink
]
NEON, PALE, HOT, OCHRE, OCHRE_DARK, NAVY, COAL, UNLIT, ORANGE, SOFT, STEEL, STEEL_DARK, SOFT_STRIPE, SKIN, JACKET, INK = range(16)

S = 1 / 4.4                 # the arm's own units to model metres
CX = .034                   # the arm's middle line in x (the left arm; the right is its mirror)
BAND_Y = .52                # the arm's own neon band runs .48 to .56
STUD_Y, FACE_Z = .755, .254  # the knuckle row on the fist's +z face
STUD_GAP = .13
LINK = .050                 # one link of an arc, and one prong, as modelled


def box(half, at=(0, 0, 0)):
    """A hard-edged block: twelve triangles, for rivets and slots too small to round."""
    hx, hy, hz = half
    pos, nrm, tris = [], [], []
    for axis in range(3):
        for sign in (-1.0, 1.0):
            n = np.zeros(3, np.float32); n[axis] = sign
            u = np.zeros(3, np.float32); u[(axis + 1) % 3] = 1
            v = np.cross(n, u)
            base = len(pos)
            for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                pos.append((n + u * a + v * b) * np.array((hx, hy, hz), np.float32)); nrm.append(n)
            tris += [(base, base + 1, base + 2), (base, base + 2, base + 3)]
    return np.array(pos, np.float32) + np.array(at, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32)


def zigzag(points, widths, depth):
    """A bolt: one slab a stroke between `points` (xy), `widths[i]` half-wide at point i. The strokes run a little past
    their ends so two strokes share a corner: that overlap is the bolt's sharp elbow."""
    out = []
    for i in range(len(points) - 1):
        a, b = np.array(points[i], np.float32), np.array(points[i + 1], np.float32)
        d = (b - a) / np.linalg.norm(b - a)
        p = np.array((-d[1], d[0]), np.float32)
        a2, b2 = a - d * widths[i] * .6, b + d * widths[i + 1] * .6
        out.append(slab([tuple(a2 - p * widths[i]), tuple(b2 - p * widths[i + 1]), tuple(b2 + p * widths[i + 1]), tuple(a2 + p * widths[i])], depth))
    return out


def star(outer, inner, depth, points=4, turn=0.0):
    ring = []
    for k in range(points * 2):
        a = turn + math.pi * k / points
        r = outer if k % 2 == 0 else inner
        ring.append((r * math.sin(-a), r * math.cos(a)))
    return slab(ring, depth)


# ------------------------------------------------------------------ the pieces (each about its own origin)

def band(main, stripe, rivet, plate, dome):
    """The ring round the forearm (the arm runs through it along y). Its contact plate is on the +z face, the side the
    player sees: arcs leave from there. Rivets on the other three sides."""
    pieces = [(rounded((.0580, .0095, .0670), .0095, seg=12, rings=4), main),
              (rounded((.0592, .0032, .0682), .0032, seg=12, rings=4), stripe)]
    for sx, sz in ((1, 0), (-1, 0), (0, -1)):
        pieces.append((box((.0030 if sx else .0090, .0050, .0030 if sz else .0090), at=(sx * .0590, 0, sz * .0680)), rivet))
    pieces.append((rounded((.0150, .0088, .0040), .0034, at=(0, 0, .0690), seg=8, rings=4), plate))
    pieces.append((ellipsoid((.0062, .0050, .0040), at=(0, 0, .0732), seg=8, rings=4), dome))
    return pieces


def studs():
    """Three contact studs on a bar across the knuckles. The origin is under the middle stud, on the skin."""
    gap = STUD_GAP * S
    pieces = [(rounded((gap + .0075, .0042, .0022), .0020, at=(0, 0, .0016), seg=8, rings=4), OCHRE_DARK)]
    for k in (-1, 0, 1):
        x = k * gap
        pieces.append((rounded((.0086, .0086, .0032), .0030, at=(x, 0, .0034), seg=10, rings=4), OCHRE))
        pieces.append((ellipsoid((.0060, .0060, .0050), at=(x, 0, .0066), seg=10, rings=4), STEEL))
        pieces.append((box((.0036, .0009, .0008), at=(x, 0, .0116)), NAVY))
    return pieces


def stud_live():
    """A stud with the current on it: the same cap, lit, a size up, with a white-hot bead. Origin on the skin."""
    return [(ellipsoid((.0070, .0070, .0058), at=(0, 0, .0068), seg=10, rings=4), NEON),
            (ellipsoid((.0034, .0034, .0030), at=(0, 0, .0114), seg=8, rings=4), HOT)]


def link():
    """One link of an arc: origin at one END, it runs up +y, flat in z. A dart, fat near its root and pointed ahead, with a white-hot core."""
    half = .0110
    body = slab([(0, 0), (half, LINK * .30), (half * .30, LINK * .86), (0, LINK), (-half * .30, LINK * .86), (-half, LINK * .30)], .0105)
    core = slab([(0, LINK * .12), (half * .42, LINK * .32), (half * .10, LINK * .84), (-half * .10, LINK * .84), (-half * .42, LINK * .32)], .0140)
    return [(body, NEON), (core, HOT)]


def prong(side):
    """A pointed zigzag standing on its root (the origin), `LINK` tall. `side` is +1 or -1: the two mirrored draws the
    code swaps between, which is the redraw of a held bolt."""
    pts = [(0, 0), (.0090 * side, .020), (-.0065 * side, .029), (.0015 * side, LINK)]
    outer = zigzag(pts, [.0080, .0072, .0060, .0008], .0100)
    inner = zigzag(pts, [.0030, .0027, .0022, .0003], .0134)
    return [(outer[0], ORANGE), (outer[1], NEON), (outer[2], NEON)] + [(s, HOT) for s in inner]


def pop():
    """The spark he flicks off a knuckle: a chunky four-point star, a second one turned across it, a hot heart."""
    return [(star(.0125, .0048, .0075), NEON),
            (star(.0085, .0040, .0100, turn=math.pi / 4), ORANGE),
            (star(.0062, .0026, .0125), HOT)]


BANDS = {"band-dead": (UNLIT, COAL, OCHRE_DARK, OCHRE_DARK, STEEL_DARK),
         "band-soft": (SOFT, SOFT_STRIPE, OCHRE, OCHRE, STEEL),
         "band-hot": (NEON, HOT, PALE, OCHRE, HOT)}


def build_plain():
    m = Model("zack_hands")
    for k, (name, tones) in enumerate(BANDS.items()):
        m.add(name, band(*tones), at=(-.15 + .15 * k, .0, 0))
    m.add("studs", studs(), at=(-.08, .07, 0))
    m.add("stud-live", stud_live(), at=(.02, .07, 0))
    m.add("seg", link(), at=(.07, .05, 0))
    m.add("prong-a", prong(1), at=(.11, .05, 0))
    m.add("prong-b", prong(-1), at=(.15, .05, 0))
    m.add("pop", pop(), at=(.19, .075, 0))
    return m


# ------------------------------------------------------------------ review poses on a stand-in left forearm

def at_arm(x, y, z):
    return (x * S, y * S, z * S)


def laid(pieces, a, b, thick=1.0):
    """A link's pieces laid from arm point `a` to arm point `b` (flat to +z), as `ZackArcHands.Draw` lays one."""
    a, b = np.array(a, np.float32) * S, np.array(b, np.float32) * S
    run = b - a
    length = float(np.linalg.norm(run))
    tilt = math.degrees(math.atan2(-run[0], run[1]))
    lean = math.degrees(math.asin(max(-1.0, min(1.0, run[2] / length))))
    return [(moved(mesh, at=tuple(a), turn=(lean, 0, tilt), scale=(thick, length / LINK, thick)), slot) for mesh, slot in pieces]


def build_pose(pose):
    m = Model("zack_hands")
    arm = [(rounded((.30 * S, .15 * S, .29 * S), .03, at=at_arm(CX, .26, 0), seg=12, rings=6), JACKET),
           (rounded((.33 * S, .04 * S, .31 * S), .008, at=at_arm(CX, .445, 0), seg=12, rings=4), OCHRE),
           (rounded((.16 * S, .06 * S, .20 * S), .012, at=at_arm(CX, .60, 0), seg=12, rings=4), SKIN),
           (rounded((.2125 * S, .095 * S, FACE_Z * S), .014, at=at_arm(CX, .745, 0), seg=12, rings=6), SKIN)]
    m.add("stand-in-arm", arm)
    tone = {"rest": "band-soft", "tick": "band-hot", "climb": "band-hot", "fall": "band-hot", "cast": "band-hot",
            "sprint": "band-hot", "dead": "band-dead"}[pose]
    swell = 1.08 if pose in ("cast", "climb") else 1.0
    m.add(tone, [(moved(mesh, scale=(swell, 1, swell)), slot) for mesh, slot in band(*BANDS[tone])], at=at_arm(CX, BAND_Y, 0))
    m.add("studs", studs(), at=at_arm(CX, STUD_Y, FACE_Z))
    top = lambda k: (CX + k * STUD_GAP, STUD_Y, FACE_Z + .05)
    live = {"tick": (-1, 0), "climb": (-1, 0, 1), "fall": (-1, 0, 1), "cast": (-1, 0, 1)}.get(pose, ())
    for k in live:
        m.add("stud-live %d" % k, stud_live(), at=at_arm(CX + k * STUD_GAP, STUD_Y, FACE_Z))
    arcs = []
    if pose == "tick":
        mid = (CX - STUD_GAP * .45, STUD_Y + .075, FACE_Z + .09)
        arcs += laid(link(), top(-1), mid, .8) + laid(link(), mid, top(0), .8)
    if pose == "climb":
        pts = [(CX, BAND_Y + .035, .315), (CX + .07, .62, .30), (CX - .06, .69, .33), top(0)]
        for a, b in zip(pts, pts[1:]):
            arcs += laid(link(), a, b, .9)
    if pose == "sprint":
        pts = [(CX, BAND_Y - .03, .33), (CX + .07, .40, .37), (CX - .05, .29, .36)]
        for a, b in zip(pts, pts[1:]):
            arcs += laid(link(), a, b, .85)
    if arcs:
        m.add("arcs", arcs)
    if pose in ("fall", "cast"):
        tall = 1.0 if pose == "fall" else 1.4
        for k in (-1, 0, 1):
            draw = prong(1 if k != 0 else -1)
            high = tall * (1.0 if k == 0 else .8)
            m.add("prong %d" % k, [(moved(mesh, turn=(0, 0, -24 * k), scale=(1, high, 1)), slot) for mesh, slot in draw], at=at_arm(*top(k)))
    return m


if __name__ == "__main__":
    posed = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--pose=")), None)
    (build_pose(posed) if posed else build_plain()).write(PALETTE)
