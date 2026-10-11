"""Soraya's hand companion (hero id `phaister`): her LEAD MOTH, the stagehand of a stage witch.

    py -3 tools/build_hand_phaister.py

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/phaister.glb. The C# side is
`Runtime/Camera/SorayaMothHand.cs`, which spawns this model twice (the lead, and a helper with no hat).

What it is: a plump fuzzy moth about 8 cm tall as modelled, sitting UPRIGHT like a plush, its face on +z. A big round
head with two big ink eyes rimmed in her magenta, two feathered antennae, a lilac ruff, four wings behind it like a
stage curtain (violet, a lilac margin, a gold eye-spot on each upper wing and a magenta one on each lower), two mitts in
white stage gloves, two feet with pink soles, and its costume: a tiny black top hat with a gold band, and a gold bow tie.

Every moving piece is its own node with its origin at its pivot:
    body                     origin under its feet (it squashes from there)
      head                   origin at the neck
        eye-l, eye-r         origin at the eye's middle (a blink is a scale on y)
        eyex-l, eyex-r       the X of a faint, scaled to nothing until it is needed
        ant-l, ant-r         origin at the root of the antenna
        hat                  origin at the brim's middle
      wing-ul, wing-ur       origin at the shoulder hinge (the hinge is the node's y axis)
      wing-ll, wing-lr
      arm-l, arm-r           origin at the shoulder
      foot-l, foot-r         origin at the ankle
      scale                  one wing scale, hidden in the belly: the C# side copies it for its pool of six

Colours are her existing palette (`PhaisterProp.Palette`) with her witch accent (#e828c5, #f444d4) added.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import ellipsoid, join, lathe, mirror, moved, slab, tube  # noqa: E402

# The sixteen slots. `SorayaMothHand.Palette` carries the same colours in the same order.
FUZZ, RUFF, WING, DARK, INK, GOLD, MAGENTA, PINK, BONE, ACCENT, CRIMSON, VIOLET, NIGHT, ACCENT_LIGHT, SHELL, GOLD_DARK = range(16)
PALETTE = ["9C78C8", "C9A2F0", "6A3AA8", "3E1F6E", "14101C", "F8B824", "E0287E", "FF6AB8",
           "F2E6DA", "E828C5", "8C1424", "9838D8", "1A1020", "F444D4", "4A2A6A", "C88A10"]


def both(mesh):
    """A piece and its mirror across x."""
    return join(mesh, mirror(mesh))


def side(mesh, sign):
    return mesh if sign > 0 else mirror(mesh)


def body():
    belly = ellipsoid((.0170, .0165, .0152), (0, .0165, 0), 10, 7)
    # A paler tummy, and two gold buttons down it: she dresses her stagehands.
    tummy = ellipsoid((.0108, .0112, .0040), (0, .0150, .0124), 8, 4)
    buttons = join(ellipsoid((.0022, .0022, .0014), (0, .0190, .0163), 4, 3), ellipsoid((.0022, .0022, .0014), (0, .0118, .0164), 4, 3))
    # The end of its abdomen, a dark stub behind, with one lilac band.
    bum = ellipsoid((.0095, .0085, .0100), (0, .0100, -.0135), 6, 4)
    # The ruff: fat lilac tufts round the neck, none at the front where the bow tie sits.
    tufts = []
    for degrees in (75, 180, 285):
        a = math.radians(degrees)
        tufts.append(ellipsoid((.0078, .0062, .0078), (.0128 * math.sin(a), .0300, .0128 * math.cos(a)), 6, 3))
    # The bow tie: two gold wedges and a magenta knot.
    wedge = slab([(.0012, -.0020), (.0112, -.0066), (.0124, 0), (.0112, .0066), (.0012, .0020)], .0050, (0, .0292, .0170))
    knot = ellipsoid((.0032, .0038, .0036), (0, .0292, .0172), 6, 4)
    return [(belly, FUZZ), (tummy, RUFF), (buttons, GOLD), (bum, DARK), (join(*tufts), RUFF),
            (both(wedge), GOLD), (knot, MAGENTA)]


def head():
    skull = ellipsoid((.0250, .0212, .0200), (0, .0185, 0), 14, 8)
    cheek = ellipsoid((.0072, .0080, .0070), (.0232, .0118, .0015), 6, 3)        # a fluffy cheek tuft, in the silhouette
    blush = ellipsoid((.0052, .0030, .0020), (.0168, .0098, .0128), 6, 3)
    mouth = join(ellipsoid((.0030, .0020, .0016), (-.0026, .0092, .0188), 6, 3), ellipsoid((.0030, .0020, .0016), (.0026, .0092, .0188), 6, 3))
    return [(skull, FUZZ), (both(cheek), RUFF), (both(blush), PINK), (mouth, INK)]


def eye(sign):
    """One big eye, built round its own middle: a magenta rim, an ink dome and one big light."""
    rim = ellipsoid((.0104, .0116, .0030), (0, 0, -.0014), 8, 3)
    dome = ellipsoid((.0088, .0100, .0046), (0, 0, 0), 10, 4)
    shine = ellipsoid((.0034, .0040, .0020), (-.0030, .0036, .0034), 8, 3)
    return [(rim, ACCENT), (dome, INK), (shine, BONE)]


def eye_x():
    """The X of a faint: two pale bars, crossed."""
    bar = slab([(-.0080, -.0015), (.0080, -.0015), (.0080, .0015), (-.0080, .0015)], .0016)
    return [(join(moved(bar, turn=(0, 0, 45)), moved(bar, at=(0, 0, .0004), turn=(0, 0, -45))), BONE)]


def antenna(sign):
    """A feathered antenna: a dark stem curving out and up, lilac barbs down both sides, a pink bead at the tip."""
    stem_points = [(0, 0, 0), (.0040, .0095, 0), (.0105, .0170, .0008), (.0190, .0215, .0018)]
    stem = tube(stem_points, [.0017, .0015, .0013, .0011], 4)
    barbs = []
    for k, t in enumerate((.26, .54, .82)):
        # A point on the stem, and the stem's own direction there, taken off the typed points.
        f = t * (len(stem_points) - 1)
        i = min(int(f), len(stem_points) - 2)
        a, b = np.array(stem_points[i]), np.array(stem_points[i + 1])
        p = a + (b - a) * (f - i)
        d = (b - a)[:2] / np.linalg.norm((b - a)[:2])
        lean = math.degrees(math.atan2(d[1], d[0]))
        length = .0080 - .0012 * k
        barb = slab([(-.0018, 0), (.0018, 0), (0, length)], .0016)
        barbs.append(moved(barb, at=tuple(p), turn=(0, 0, lean - 90 + 28)))
        barbs.append(moved(barb, at=tuple(p), turn=(0, 0, lean - 90 + 152)))
    bead = ellipsoid((.0026, .0026, .0026), stem_points[-1], 6, 4)
    pieces = [(stem, DARK), (join(*barbs), RUFF), (bead, PINK)]
    return [(side(m, sign), s) for m, s in pieces]


def hat():
    """A tiny black top hat, worn at a tilt, a gold band and a magenta gem on it."""
    crown = lathe([(.0128, 0), (.0132, .0010), (.0128, .0021), (.0080, .0022), (.0076, .0100), (.0090, .0168)], 8)
    band = lathe([(.0083, .0026), (.0086, .0045), (.0082, .0064)], 8)
    gem = ellipsoid((.0024, .0026, .0018), (0, .0045, .0086), 6, 3)
    tilt = (8, 0, -13)
    return [(moved(crown, turn=tilt), INK), (moved(band, turn=tilt), GOLD), (moved(gem, turn=tilt), MAGENTA)]


UPPER = [(0, -.0040), (.0120, -.0115), (.0280, -.0125), (.0420, -.0060), (.0505, .0080), (.0520, .0240), (.0450, .0370), (.0320, .0435), (.0180, .0400), (.0050, .0220)]
LOWER = [(0, .0040), (.0050, -.0110), (.0130, -.0195), (.0240, -.0230), (.0340, -.0190), (.0400, -.0090), (.0390, .0010), (.0300, .0070), (.0120, .0080)]
THICK = .0030


def disc(radius, x, y, z, slot, depth=.0012, seg=12):
    """A round button lying on a wing's face: its flat side on z, proud by `depth` (to the back when z is negative)."""
    button = lathe([(radius, 0), (radius * .8, depth)], seg)
    return moved(button, at=(x, y, z), turn=(90 if z >= 0 else -90, 0, 0)), slot


def upper_wing(sign):
    """An upper wing: a rounded delta, a lilac margin and the eye-spot (gold, ink, magenta)."""
    plate = slab(UPPER, THICK)
    margin = tube([(x, y, 0) for x, y in UPPER[2:9]], .0018, 3)
    face = THICK * .5
    pieces = [(plate, WING), (margin, RUFF),
              disc(.0095, .0320, .0200, face, GOLD), disc(.0064, .0320, .0200, face + .0008, INK, .0012, 10),
              disc(.0030, .0306, .0214, face + .0016, ACCENT_LIGHT, .0010, 8)]
    return [(side(m, sign), s) for m, s in pieces]


def lower_wing(sign):
    plate = slab(LOWER, THICK)
    margin = tube([(x, y, 0) for x, y in LOWER[2:8]], .0016, 3)
    face = THICK * .5
    pieces = [(plate, VIOLET), (margin, RUFF),
              disc(.0062, .0255, -.0085, face, ACCENT, .0012, 10)]
    return [(side(m, sign), s) for m, s in pieces]


def arm(sign):
    """A mitt hanging from the shoulder, in a white stage glove."""
    sleeve = ellipsoid((.0042, .0062, .0042), (0, -.0048, 0), 6, 3)
    glove = ellipsoid((.0046, .0042, .0046), (0, -.0100, 0), 6, 3)
    return [(sleeve, DARK), (glove, BONE)]


def foot(sign):
    shoe = ellipsoid((.0058, .0038, .0080), (0, 0, .0038), 6, 3)
    sole = ellipsoid((.0038, .0013, .0054), (0, -.0030, .0044), 6, 3)             # a pink sole: seen when it faints, legs up
    return [(shoe, DARK), (sole, PINK)]


def scale():
    """One wing scale, the only garnish: a lilac diamond with a gold middle."""
    diamond = slab([(0, -.0062), (.0044, 0), (0, .0062), (-.0044, 0)], .0022)
    middle = ellipsoid((.0018, .0026, .0016), (0, 0, 0), 6, 3)
    return [(diamond, RUFF), (middle, GOLD)]


def build():
    m = kit.Model("phaister")
    m.add("body", body())
    m.add("head", head(), at=(0, .0310, .0020), parent="body")
    for name, sign in (("l", 1), ("r", -1)):
        m.add("eye-" + name, eye(sign), at=(sign * .0110, .0188, .0168), parent="head")
        if "--no-faint" not in sys.argv:                                # the review looks at the open eyes without the X over them
            m.add("eyex-" + name, eye_x(), at=(sign * .0110, .0188, .0222), parent="head")
        m.add("ant-" + name, antenna(sign), at=(sign * .0105, .0362, -.0030), parent="head")
        m.add("wing-u" + name, upper_wing(sign), at=(sign * .0110, .0270, -.0125), parent="body")
        m.add("wing-l" + name, lower_wing(sign), at=(sign * .0100, .0200, -.0140), parent="body")
        m.add("arm-" + name, arm(sign), at=(sign * .0158, .0250, .0080), parent="body")
        m.add("foot-" + name, foot(sign), at=(sign * .0088, .0036, .0040), parent="body")
    m.add("hat", hat(), at=(.0050, .0378, .0010), parent="head")
    m.add("scale", scale(), at=(0, .0165, 0), parent="body")
    m.write(PALETTE)


if __name__ == "__main__":
    build()
