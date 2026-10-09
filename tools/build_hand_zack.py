"""Zack's (ISAGANI's) hand companion: KISLAP, the spark that lives in the neon bands on his forearms.

  py -3 tools/build_hand_zack.py                 the game's model: every part, every face
  py -3 tools/build_hand_zack.py --show=cocky    a review model: one face only (cocky, shock, grin, dazed)

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/zack.glb. `ZackSparkHand.cs` finds every part by name and
poses it; the top-level parts (spark, fizz, bolt, seg, band-lit, band-dim) are placed by the code every frame, so where
they stand in the file is only a layout for the review sheet.

Kislap is a fat battery-bean in Zack's neon yellow: navy shorts with an ochre belt and two pale rivets, a charcoal terminal
cap with two plug prongs (his ears), navy mitts, charcoal boots, a zigzag bolt for a tail, a "+" stamped on his back,
and a cocky face (one brow up, a smirk). Metres, y up, +z is his front. His origin is under his feet, so a squash
flattens him onto the band he stands on.
"""
import math
import sys

import numpy as np

from hand_companion_kit import Model, ellipsoid, join, mirror, moved, rounded, slab, tube

PALETTE = [
    "#e8f53a",  # 0  neon: his body, a lit band, a bolt
    "#f6ffa0",  # 1  pale neon: the rivets on his shorts, the tip of his tail
    "#fffff0",  # 2  white-hot: cores, tips, a lit band's stripe
    "#c9951c",  # 3  ochre: the belt, a lit band's rivets, the root of his tail (Zack's jacket)
    "#8a6412",  # 4  dark ochre: a dim band's rivets
    "#1c2340",  # 5  navy: shorts, mitts, eyes, brows, mouth
    "#2b2f3a",  # 6  charcoal: terminal cap, boots, a dim band's stripe
    "#5e6426",  # 7  an unlit band
    "#ff9a3c",  # 8  hot orange: the middle of his tail
    "#ffffff",  # 9  white: the glint in his eye, his teeth
    "#8b8f66",  # 10 fizzled: the flat blob he becomes when tagged
    "#55593f",  # 11 fizzled, darker: the puddle under the blob
    "#c2cf1e",  # 12 deep neon: the underside tone of a bolt segment
    "#e0566a",  # 13 the inside of his mouth
    "#d9dcc4",  # 14 steel: his prongs
    "#10131f",  # 15 ink
]
NEON, PALE, HOT, OCHRE, OCHRE_DARK, NAVY, COAL, UNLIT, ORANGE, WHITE, FIZZ, FIZZ_DARK, DEEP, PINK, STEEL, INK = range(16)


def box(half, at=(0, 0, 0)):
    """A hard-edged block: twelve triangles, for rivets, brows and other pieces too small to round."""
    hx, hy, hz = half
    pos, nrm, tris = [], [], []
    for axis in range(3):
        for sign in (-1.0, 1.0):
            n = np.zeros(3, np.float32); n[axis] = sign
            u = np.zeros(3, np.float32); u[(axis + 1) % 3] = 1
            v = np.cross(n, u)                                      # u x v = n: counter-clockwise from outside
            base = len(pos)
            for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                pos.append((n + u * a + v * b) * np.array((hx, hy, hz), np.float32)); nrm.append(n)
            tris += [(base, base + 1, base + 2), (base, base + 2, base + 3)]
    return np.array(pos, np.float32) + np.array(at, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32)


def zigzag(points, widths, depth):
    """A lightning bolt: one slab a stroke between `points` (xy), `widths[i]` half-wide at point i. The strokes overlap
    at the corners, which is what gives a bolt its sharp elbows."""
    out = []
    for i in range(len(points) - 1):
        a, b = np.array(points[i], np.float32), np.array(points[i + 1], np.float32)
        d = (b - a) / np.linalg.norm(b - a)
        p = np.array((-d[1], d[0]), np.float32)
        # Each stroke runs a little past its ends so two strokes share a corner and no gap shows.
        a2, b2 = a - d * widths[i] * .6, b + d * widths[i + 1] * .6
        out.append(slab([tuple(a2 - p * widths[i]), tuple(b2 - p * widths[i + 1]), tuple(b2 + p * widths[i + 1]), tuple(a2 + p * widths[i])], depth))
    return out


def build(show=None):
    m = Model("zack")
    faces = lambda *names: show is None or show in names

    # ---------------------------------------------------------------- Kislap himself (origin under his feet)
    body = [
        (rounded((.030, .031, .026), .022, at=(0, .040, 0), seg=16, rings=10), NEON),
        (rounded((.0312, .0095, .0272), .0090, at=(0, .0175, 0), seg=12, rings=6), NAVY),            # shorts
        (rounded((.0320, .0026, .0280), .0026, at=(0, .0275, 0), seg=12, rings=4), OCHRE),           # belt
        (box((.0030, .0030, .0014), at=(.0125, .0170, .0275)), PALE),                                # two rivets
        (box((.0030, .0030, .0014), at=(-.0125, .0170, .0275)), PALE),
        (rounded((.019, .0062, .016), .006, at=(0, .0722, 0), seg=12, rings=4), COAL),               # terminal cap
        (ellipsoid((.011, .0062, .014), at=(.014, .0052, .005), seg=8, rings=4), COAL),              # boots
        (ellipsoid((.011, .0062, .014), at=(-.014, .0052, .005), seg=8, rings=4), COAL),
        (box((.0085, .0026, .0014), at=(0, .050, -.0262)), NAVY),                                     # the "+" on his back
        (box((.0026, .0085, .0014), at=(0, .050, -.0262)), NAVY),
    ]
    m.add("spark", body, at=(0, 0, 0))

    eye = [(ellipsoid((.0062, .0085, .0035), seg=10, rings=6), NAVY),
           (ellipsoid((.0022, .0028, .0015), at=(.0018, .0034, .0030), seg=6, rings=4), WHITE)]
    m.add("eye-l", eye, at=(-.0125, .0475, .0252), parent="spark")
    m.add("eye-r", eye, at=(.0125, .0475, .0252), parent="spark")
    m.add("brow-l", box((.0078, .0019, .0016)), NAVY, at=(-.0125, .0605, .0232), parent="spark")
    m.add("brow-r", box((.0078, .0019, .0016)), NAVY, at=(.0125, .0605, .0232), parent="spark")

    # Dazed eyes: "> <", two strokes a chevron.
    chevron = join(moved(box((.0052, .0015, .0015)), at=(0, .0024, 0), turn=(0, 0, -27)),
                   moved(box((.0052, .0015, .0015)), at=(0, -.0024, 0), turn=(0, 0, 27)))
    if faces("dazed"):
        m.add("squint-l", chevron, NAVY, at=(-.0125, .0475, .0262), parent="spark")
        m.add("squint-r", mirror(chevron), NAVY, at=(.0125, .0475, .0262), parent="spark")

    if faces("cocky"):
        m.add("mouth-smirk", tube([(-.0075, .0005, 0), (-.002, -.0012, .0004), (.004, -.0002, .0002), (.0088, .0042, -.0008)], .0016, seg=6),
              NAVY, at=(0, .0360, .0262), parent="spark")
    if faces("shock", "dazed"):
        m.add("mouth-o", [(ellipsoid((.0040, .0048, .0024), seg=8, rings=4), NAVY),
                          (ellipsoid((.0023, .0028, .0014), at=(0, -.0004, .0016), seg=6, rings=4), PINK)],
              at=(0, .0355, .0262), parent="spark")
    if faces("grin"):
        arc = [(-.0095, .003)] + [(.0095 * math.cos(a), .003 + .0085 * math.sin(a)) for a in np.linspace(math.pi * 1.08, math.pi * 1.92, 5)] + [(.0095, .003)]
        m.add("mouth-grin", [(slab(arc, .0030), NAVY),
                             (slab([(-.0045, -.0050), (0, -.0058), (.0045, -.0050), (.0038, -.0020), (-.0038, -.0020)], .0042), PINK),
                             (box((.0068, .0011, .0020), at=(0, .0017, 0)), WHITE)],
              at=(0, .0368, .0262), parent="spark")

    # His prongs are his ears: each pivots at its foot on the cap.
    prong = [(rounded((.0042, .0095, .0028), .0026, at=(0, .0085, 0), seg=8, rings=4), STEEL),
             (rounded((.0045, .0030, .0031), .0026, at=(0, .0165, 0), seg=8, rings=4), HOT)]
    m.add("prong-l", prong, at=(-.0098, .0770, 0), parent="spark")
    m.add("prong-r", prong, at=(.0098, .0770, 0), parent="spark")

    # His tail: a bolt out past his hip and up (a stroke, a short jog back, a stroke), ochre at the root, orange, then a pale tip. It pivots at its root.
    strokes = zigzag([(0, 0), (.025, .015), (.015, .025), (.030, .040), (.041, .053)], [.0080, .0080, .0075, .0050, .0008], .010)
    m.add("tail", [(strokes[0], OCHRE), (strokes[1], ORANGE), (strokes[2], ORANGE), (strokes[3], PALE)], at=(.020, .016, -.013), parent="spark")

    mitt = ellipsoid((.0078, .0074, .0078), seg=8, rings=4)
    m.add("mitt-l", mitt, NAVY, at=(-.0362, .0370, .0050), parent="spark")
    m.add("mitt-r", mitt, NAVY, at=(.0362, .0370, .0050), parent="spark")

    # ---------------------------------------------------------------- the little bolt he juggles and fires
    little = zigzag([(-.0030, .0150), (.0050, .0020), (-.0050, -.0020), (.0030, -.0150)], [.0018, .0060, .0060, .0018], .0080)
    m.add("bolt", [(s, NEON) for s in little] + [(moved(s, scale=(.5, .62, 1.45)), HOT) for s in little], at=(.075, .095, 0))

    # ---------------------------------------------------------------- one link of the big bolt (origin at one END; it runs up +y)
    length, half = .050, .0115
    link = slab([(0, 0), (half, length * .22), (half, length * .78), (0, length), (-half, length * .78), (-half, length * .22)], .0125)
    core = slab([(0, length * .14), (half * .42, length * .3), (half * .42, length * .7), (0, length * .86), (-half * .42, length * .7), (-half * .42, length * .3)], .0165)
    m.add("seg", [(link, NEON), (core, HOT)], at=(.075, .020, 0))

    # ---------------------------------------------------------------- a forearm band, lit and unlit (the arm runs through it along y)
    def band(main, stripe, rivet):
        pieces = [(rounded((.0580, .0095, .0670), .0095, seg=12, rings=4), main),
                  (rounded((.0592, .0032, .0682), .0032, seg=12, rings=4), stripe)]
        for sx, sz in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            pieces.append((box((.0030 if sx else .0090, .0050, .0030 if sz else .0090), at=(sx * .0590, 0, sz * .0680)), rivet))
        return pieces
    m.add("band-lit", band(NEON, HOT, OCHRE), at=(-.135, .075, 0))
    m.add("band-dim", band(UNLIT, COAL, OCHRE_DARK), at=(-.135, .020, 0))

    # ---------------------------------------------------------------- tagged: a dim flat blob with X eyes (origin under it)
    cross = join(moved(box((.0075, .0018, .0014)), turn=(0, 0, 45)), moved(box((.0075, .0018, .0014)), turn=(0, 0, -45)))
    limp = rounded((.0042, .0095, .0028), .0026, seg=8, rings=4)
    m.add("fizz", [(ellipsoid((.036, .0115, .030), at=(0, .0105, 0), seg=12, rings=4), FIZZ),
                   (ellipsoid((.043, .0040, .037), at=(0, .0030, 0), seg=12, rings=4), FIZZ_DARK),
                   (moved(cross, at=(-.0140, .0180, .0185), turn=(-52, 0, 0)), NAVY),
                   (moved(cross, at=(.0140, .0180, .0185), turn=(-52, 0, 0)), NAVY),
                   (moved(box((.0065, .0014, .0012)), at=(0, .0118, .0285), turn=(-70, 0, 8)), NAVY),
                   (moved(limp, at=(-.0300, .0190, -.0100), turn=(0, 0, 72)), STEEL),
                   (moved(limp, at=(.0300, .0190, -.0100), turn=(0, 0, -72)), STEEL),
                   (box((.0085, .0012, .0026), at=(0, .0218, -.0050)), COAL),
                   (box((.0026, .0012, .0085), at=(0, .0218, -.0050)), COAL)],
          at=(.085, -.045, 0))
    return m


if __name__ == "__main__":
    shown = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--show=")), None)
    build(shown).write(PALETTE)
