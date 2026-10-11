"""Basilio's hand companion: three chunky stones with gold veins that float round his left fist (`DanteStoneHand`).

  py -3 tools/build_hand_dante.py            the whole model (what the game loads)
  py -3 tools/build_hand_dante.py --look=sleepy | --look=awake    review only: one face, seams hidden or shown

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/dante.glb. ALWAYS finish with the plain command: the two
`--look` builds leave parts out so a review sheet shows one face at a time, and the game needs every part.

What is in it (each a named node, its origin at its own middle so the C# side can pose it):
  stone-big    the mother stone: a sleepy face (lids, or open eyes), two stone brows, a mouth, a moss cap, a sprout
  stone-mid    a chick: a leather strap with a gold stud (his cord bracelet), dot eyes
  stone-small  the smallest chick: a moss tuft, dot eyes, a vein too big for it
  *-seam       the molten crack of each stone, hidden inside it until a landing or a cast opens it
  pebble       one pebble; the C# side copies it six times for the crumble

A stone is a rounded block with corners knocked off by flat cuts (`stone`): the stepped, cut stone of `GeoVfx`, at the
size of a toy. Its lower third is a darker tone. The veins are fat gold cords pressed half into the surface.
"""
import math
import sys

import numpy as np

import hand_companion_kit as kit
from hand_companion_kit import ellipsoid, join, moved, rounded, tube

# The sixteen colours, the same list as `DanteStoneHand.Palette`.
PALETTE = [
    "#8f8577",  # 0 stone
    "#b9ad9b",  # 1 stone, light (the brows, the middle chick)
    "#62594f",  # 2 stone, dark (the lower third)
    "#46403a",  # 3 stone, deepest (the pebble's foot)
    "#dfb248",  # 4 gold vein
    "#f6dc86",  # 5 gold, light (nuggets, the stud)
    "#ff7a1c",  # 6 molten seam
    "#ffd257",  # 7 molten seam's core
    "#3d6335",  # 8 moss (his shirt's green)
    "#3fa65c",  # 9 earth accent
    "#8fe0a0",  # 10 earth accent, light (leaves)
    "#482f1d",  # 11 leather
    "#1d1a1c",  # 12 the eyes and mouths
    "#efe6d2",  # 13 the glint in an eye (his forearm wrap's cream)
    "#d18a58",  # 14 ember blush on the chicks' cheeks
    "#2b4526",  # 15 moss, in shadow
]
STONE, LIGHT, DARK, DEEP, GOLD, GOLD_LIGHT, MOLTEN, CORE, MOSS, EARTH, LEAF, LEATHER, INK, GLINT, BLUSH, MOSS_DARK = range(16)


class Rock:
    """A rounded block of half-size `half`, edges rounded to `radius`, with `chips`: (normal, share) flat cuts, each
    taking the block back to `share` of its own reach along that normal."""

    def __init__(self, half, radius, chips):
        self.half = np.array(half, np.float64)
        self.radius = radius
        self.core = np.maximum(self.half - radius, 0)
        self.planes = []
        for normal, share in chips:
            n = np.array(normal, np.float64)
            n /= np.linalg.norm(n)
            reach = float(np.dot(np.abs(n), self.core)) + radius
            self.planes.append((n, reach * share))

    def inside(self, p):
        q = np.abs(p) - self.core
        if np.linalg.norm(np.maximum(q, 0)) > self.radius:
            return False
        return all(float(np.dot(p, n)) <= d for n, d in self.planes)

    def hit(self, origin, direction, far=.1):
        """The surface along a ray that starts inside."""
        lo, hi = 0.0, far
        o, d = np.array(origin, np.float64), np.array(direction, np.float64)
        for _ in range(30):
            mid = (lo + hi) * .5
            if self.inside(o + d * mid):
                lo = mid
            else:
                hi = mid
        return o + d * lo

    def at(self, direction, lift=0.0):
        """The surface in `direction` from the middle, lifted `lift` off it."""
        d = np.array(direction, np.float64)
        d /= np.linalg.norm(d)
        return self.hit((0, 0, 0), d) + d * lift

    def front(self, x, y, lift=0.0):
        """The face (+z side) at x, y."""
        p = self.hit((x, y, 0), (0, 0, 1))
        return (p[0], p[1], p[2] + lift)

    def normal(self, p, direction):
        cut = np.zeros(3)
        for n, d in self.planes:
            if float(np.dot(p, n)) >= d - 2e-5:
                cut += n
        if np.linalg.norm(cut) > 0:
            n = cut / np.linalg.norm(cut)
        else:
            q = np.maximum(np.abs(p) - self.core, 0) * np.sign(p)
            n = q / np.linalg.norm(q) if np.linalg.norm(q) > 1e-9 else direction
        n = n * .8 + direction * .2            # a little of the ball's own roundness, so a cut is not a mirror
        return n / np.linalg.norm(n)

    def mesh(self, seg, rings, body, foot, foot_below=-.38):
        """The stone, in two tones: `foot` where the ball it is cut from is below `foot_below`, `body` above."""
        dirs, tris = kit._sphere(seg, rings)
        pos, nrm = [], []
        for d in dirs.astype(np.float64):
            d = d / np.linalg.norm(d)
            p = self.hit((0, 0, 0), d)
            pos.append(p)
            nrm.append(self.normal(p, d))
        pos, nrm = np.array(pos, np.float32), np.array(nrm, np.float32)
        low = dirs[tris][:, :, 1].max(axis=1) < foot_below      # by the ball's own rings, so the line is clean
        return [((pos, nrm, tris[~low]), body), ((pos, nrm, tris[low]), foot)]

    def cord(self, waypoints, radius, steps=2, sink=.35, seg=4):
        """A cord pressed into the surface through `waypoints` (directions from the middle)."""
        way = [np.array(w, np.float64) / np.linalg.norm(w) for w in waypoints]
        pts = []
        for a, b in zip(way[:-1], way[1:]):
            for k in range(steps):
                pts.append(self.at(a + (b - a) * k / steps, radius * (1 - 2 * sink)))
        pts.append(self.at(way[-1], radius * (1 - 2 * sink)))
        return tube(pts, radius, seg=seg)


def eye(half, glint=True, seg=6, rings=3):
    """One open eye about its own middle: a dark bean, a cream glint high on one side."""
    parts = [(ellipsoid(half, seg=seg, rings=rings), INK)]
    if glint:
        g = half[0] * .36
        parts.append((ellipsoid((g, g, half[2] * .7), at=(-half[0] * .32, half[1] * .38, half[2] * .62), seg=5, rings=3), GLINT))
    return parts


def arc(width, sag, radius, count=5, seg=3):
    """A curved line in the xy plane: ends high, middle `sag` lower (a shut, sleeping eye; a mouth)."""
    return tube([(x * width, -sag * (1 - x * x), 0) for x in np.linspace(-1, 1, count)], radius, seg=seg)


def build(look=None):
    m = kit.Model("dante")
    sleepy, awake = look in (None, "sleepy"), look in (None, "awake")

    # ---------------------------------------------------------------- the mother stone
    big = Rock((.0255, .0215, .0225), .0085, [
        ((-1, 1, .15), .86), ((1, .9, -.4), .84), ((.9, -1, .2), .86), ((-1, -.7, -.5), .84),
        ((.2, 1, -1), .86), ((-.3, -1, .9), .90), ((1, .1, 1), .93), ((-1, .05, .9), .94), ((0, 1, .1), .95)])
    pieces = big.mesh(16, 8, STONE, DARK, foot_below=-.3)
    # Gold: one vein down the right cheek with a branch, one low on the left, a nugget where the first forks.
    pieces.append((big.cord([(.55, .9, .25), (.8, .45, .5), (.95, .05, .5), (.8, -.35, .65)], .0021), GOLD))
    pieces.append((big.cord([(.95, .05, .5), (1, .0, .0), (.9, -.2, -.5)], .0017), GOLD))
    pieces.append((big.cord([(-1, -.1, .55), (-.75, -.45, .7), (-.35, -.7, .8)], .0019), GOLD))
    pieces.append((ellipsoid((.0031, .0031, .0022), at=big.at((.95, .05, .5), .0006), seg=5, rings=3), GOLD_LIGHT))
    m.add("stone-big", pieces)

    # Its moss: a cap that sits on the head like a nightcap gone flat, three lobes creeping down, a paler patch.
    top = big.at((0, 1, 0))[1]
    moss = [(ellipsoid((.0185, .0052, .0165), at=(-.002, top - .0018, -.002), seg=10, rings=5), MOSS)]
    for x, z, r in ((-.015, .010, .0058), (.004, .0145, .0062), (.0155, .004, .005), (-.013, -.012, .006)):
        p = big.at((x, .0165, z), -.0012)
        moss.append((ellipsoid((r, r * .78, r), at=p, seg=5, rings=3), MOSS))
    moss.append((ellipsoid((.0075, .0024, .006), at=(.004, top + .0024, -.004), seg=6, rings=3), EARTH))
    m.add("big-moss", moss, parent="stone-big")
    # A sprout on the cap, its own part so it can lag and wag: a stem and two fat leaves.
    sprout = [(tube([(0, 0, 0), (.0006, .0045, 0), (0, .0085, 0)], [.0012, .001, .0009], seg=5), EARTH),
              (moved(ellipsoid((.0046, .0013, .0027), seg=5, rings=3), at=(.0042, .0098, 0), turn=(0, 0, 28)), LEAF),
              (moved(ellipsoid((.0038, .0012, .0023), seg=5, rings=3), at=(-.0036, .0088, 0), turn=(0, 0, -34)), LEAF)]
    m.add("big-sprout", sprout, at=(-.006, top + .0028, -.002), parent="stone-big")

    # Its face. Eyes wide apart and low, heavy brows: a stone that has been asleep a hundred years.
    ex, ey = .0098, .0008
    for side, s in (("l", -1), ("r", 1)):
        p = big.front(s * ex, ey, .0004)
        if sleepy:
            m.add("big-lid-" + side, arc(.0052, .0027, .00125), INK, at=(p[0], p[1] - .0006, p[2] + .0006), parent="stone-big")
        if awake:
            m.add("big-eye-" + side, eye((.0037, .0046, .0016), rings=6), at=p, parent="stone-big")
        b = big.front(s * (ex + .0004), ey + .0082, .0006)
        m.add("big-brow-" + side, rounded((.0068, .0024, .0026), .0016, seg=8, rings=5), LIGHT, at=b, parent="stone-big")
    p = big.front(0, -.0078, .0005)
    m.add("big-mouth", arc(.0042, -.0006, .0013), INK, at=p, parent="stone-big")
    if awake:
        m.add("big-seam", [(big.cord([(-.15, 1, .35), (.12, .8, .75), (-.2, .45, 1), (.1, .1, 1), (-.12, -.25, 1), (.15, -.7, .8)], .002, steps=1, sink=-.1), MOLTEN),
                           (big.cord([(-.15, 1, .35), (.12, .8, .75), (-.2, .45, 1), (.1, .1, 1), (-.12, -.25, 1), (.15, -.7, .8)], .001, steps=1, sink=-1.1, seg=3), CORE),
                           (big.cord([(.12, .8, .75), (.6, .72, .5)], .0016, steps=1, sink=-.1), MOLTEN)],
              parent="stone-big")

    # ---------------------------------------------------------------- the middle chick: strapped like his wrist
    mid = Rock((.0172, .0152, .0156), .0068, [
        ((1, 1, .2), .85), ((-1, .8, -.3), .86), ((-.9, -1, .3), .87), ((1, -.8, -.6), .85), ((.1, 1, -1), .88), ((-1, .2, 1), .94)])
    pieces = mid.mesh(12, 7, LIGHT, STONE, foot_below=-.3)
    pieces.append((mid.cord([(-.5, .9, .3), (-.85, .4, .45), (-.9, -.1, .5)], .0017), GOLD))
    # The strap: a leather cord slung round it on a slant, knotted at the front with a gold stud.
    ring = [(math.cos(a), .3 + .3 * math.sin(a + .5), math.sin(a)) for a in np.linspace(0, 2 * math.pi, 11)]
    pieces.append((mid.cord(ring, .00185, steps=1, sink=.2, seg=4), LEATHER))
    knot = mid.at((.62, .3 + .3 * math.sin(.9 + .5), .78), .0008)
    pieces.append((ellipsoid((.0027, .0027, .002), at=knot, seg=5, rings=3), GOLD_LIGHT))
    for s in (-1, 1):
        pieces.append((ellipsoid((.0024, .0015, .0008), at=mid.front(s * .0086, -.0042, .0001), seg=5, rings=3), BLUSH))
    pieces.append((ellipsoid((.0013, .001, .0008), at=mid.front(0, -.0046, .0003), seg=5, rings=3), INK))
    m.add("stone-mid", pieces, at=(-.052, -.006, 0))
    p = mid.front(0, -.0006, .0004)
    m.add("mid-eyes", [(moved(e, at=(dx, 0, 0)), sl) for dx in (-.0056, .0056) for e, sl in eye((.0025, .0031, .0013))],
          at=p, parent="stone-mid")
    if awake:
        m.add("mid-seam", [(mid.cord([(.2, 1, .3), (-.1, .7, .8), (.2, .2, 1), (-.15, -.4, 1)], .0017, steps=1, sink=-.1), MOLTEN),
                           (mid.cord([(.2, 1, .3), (-.1, .7, .8), (.2, .2, 1), (-.15, -.4, 1)], .0009, steps=1, sink=-1.1, seg=3), CORE)],
              parent="stone-mid")

    # ---------------------------------------------------------------- the smallest: a tuft, a vein too big for it
    small = Rock((.0136, .0124, .0126), .0058, [
        ((-1, 1, .3), .84), ((1, .9, -.2), .87), ((1, -1, .4), .86), ((-1, -.9, -.5), .86), ((-.2, .8, -1), .88)])
    pieces = small.mesh(10, 6, STONE, DARK, foot_below=-.3)
    pieces.append((small.cord([(.3, 1, .2), (.8, .5, .45), (.95, -.1, .4), (.6, -.6, .6)], .0018), GOLD))
    pieces.append((ellipsoid((.0022, .0022, .0016), at=small.at((.8, .5, .45), .0005), seg=5, rings=3), GOLD_LIGHT))
    crown = small.at((-.25, 1, 0))
    pieces.append((ellipsoid((.0062, .0026, .0056), at=(crown[0], crown[1] - .0004, crown[2]), seg=5, rings=3), EARTH))
    pieces.append((moved(ellipsoid((.0032, .001, .0019), seg=5, rings=3), at=(crown[0] - .002, crown[1] + .0036, crown[2]), turn=(0, 0, -40)), LEAF))
    for s in (-1, 1):
        pieces.append((ellipsoid((.002, .0013, .0008), at=small.front(s * .007, -.0036, .0001), seg=5, rings=3), BLUSH))
    pieces.append((ellipsoid((.0011, .0009, .0008), at=small.front(0, -.004, .0003), seg=5, rings=3), INK))
    m.add("stone-small", pieces, at=(.048, -.011, 0))
    p = small.front(0, -.0004, .0004)
    m.add("small-eyes", [(moved(e, at=(dx, 0, 0)), sl) for dx in (-.0046, .0046) for e, sl in eye((.0022, .0027, .0012))],
          at=p, parent="stone-small")
    if awake:
        m.add("small-seam", [(small.cord([(-.2, 1, .3), (.15, .6, .85), (-.15, .1, 1), (.1, -.5, .9)], .0015, steps=1, sink=-.1), MOLTEN),
                             (small.cord([(-.2, 1, .3), (.15, .6, .85), (-.15, .1, 1), (.1, -.5, .9)], .0008, steps=1, sink=-1.1, seg=3), CORE)],
              parent="stone-small")

    # ---------------------------------------------------------------- one pebble (copied six times in the game)
    peb = Rock((.0066, .0052, .0058), .0026, [((1, 1, .3), .82), ((-1, .6, -.5), .85), ((-.6, -1, .6), .86), ((.5, -.3, -1), .86)])
    pieces = peb.mesh(7, 5, STONE, DEEP, foot_below=-.1)
    pieces.append((ellipsoid((.0017, .0013, .0012), at=peb.at((-.4, .5, .8), .0002), seg=5, rings=3), GOLD))
    m.add("pebble", pieces, at=(-.004, -.04, 0))

    m.write(PALETTE)


if __name__ == "__main__":
    build(next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--look=")), None))
