"""Amihan's hand companion: a paper pinwheel tied to her left wrist, and the maya bird who rides it.

Owner, 2026-10-06, of the hands' effects: "the vfx and animations that the session is giving me is just bland 2d vfx
particles", and of flat stickers: "i don't like the sticker effects". Her wind is ribbons and never flat plates
(`WindVfx`), so nothing here draws wind at all: the pinwheel SHOWS the wind by how it turns, and the bird is the
character. Of the first companion: "a bit lacking on textures and styling", so the paper is two-tone with a pale
stripe, the stick has her sleeve's teal bands and gold caps, the thread is a two-tone twist with a bow, and the bird
has the maya's chestnut cap, cream cheek with its dark spot, a bib, a wing bar and a pale tail tip.

  py -3 tools/build_hand_amihan.py

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/amihan.glb. `AmihanPinwheelHand.cs` finds every part by
name and poses it about its own origin. Metres, y up, +z is the front (the bird's face and the wheel's face).

The parts (indent is parentage; each origin is the part's pivot):
  band          the thread round her hand. Built at the arm's own size divided by BUILT_SCALE.
  knot          the bow on top of the band, where the pinwheel is tied
  string        a short length of thread along +y, 2 cm: stretched from the knot to the stick on a fall
  stick         the stick, its origin at its foot
    perch       the crossbar the bird stands on
    wheel       the axle; turns about z
      blade0..3   one paper blade each, origin at the axle; the curl leans back about it
        blade0tip..3tip   the stud at the flat corner (it also tells the code which way the blade points)
      hub       the button
  bird          the feet, origin on the perch
    body        origin at its middle
      head      origin at the neck
        eyeL eyeR beakTop beakBottom
      wingL wingR   origin at the shoulder
      tail      origin at the rump
  feather       one loose feather, copied for the few that fly off when it tumbles
"""
import math

import numpy as np

from hand_companion_kit import Model, ellipsoid, join, lathe, mirror, moved, rounded, slab, tube

BUILT_SCALE = 3.4          # `AmihanPinwheelHand.BuiltScale`: the band is the arm's size over this

PALETTE = [
    "#F6EBD0",  # 0 paper, cream
    "#88E35A",  # 1 paper, her wind's green
    "#C3F5AA",  # 2 paper stripe, the wind's pale green
    "#B8472E",  # 3 rust red thread
    "#E8B64A",  # 4 gold, her sleeve's edge: the button and the caps
    "#C89B5E",  # 5 bamboo
    "#2E8C86",  # 6 teal, her sleeve: the stick's bands
    "#946238",  # 7 bird brown
    "#5E3D24",  # 8 dark brown: wings, streaks, tail
    "#A84E22",  # 9 chestnut cap
    "#F1E4C8",  # 10 cream cheek and belly, her cuff
    "#1E1A18",  # 11 ink: eyes, bib, cheek spot
    "#FFFFFF",  # 12 the glint in the eye
    "#E0A040",  # 13 beak
    "#CF8E6C",  # 14 feet
    "#8A2F20",  # 15 the thread's darker strand
]
(CREAM, GREEN, PALE, RUST, GOLD, WOOD, TEAL, BROWN, DARK, CAP, CHEEK, INK, WHITE, BEAK, FEET, RUST2) = range(16)


# ------------------------------------------------------------------ shapes the kit does not have

class Faces:
    """Loose quads gathered by palette slot: paper, which has two sides in two colours."""

    def __init__(self):
        self.by_slot = {}

    def quad(self, slot, pts, normals):
        pos, nrm, tris = self.by_slot.setdefault(slot, ([], [], []))
        base = len(pos)
        p = [np.array(v, np.float32) for v in pts]
        n = [np.array(v, np.float32) for v in normals]
        want = sum(n)
        face = np.cross(p[1] - p[0], p[2] - p[0])
        if float(np.linalg.norm(face)) < 1e-12:
            face = np.cross(p[2] - p[0], p[3] - p[0])
        order = (0, 1, 2, 0, 2, 3) if float(np.dot(face, want)) >= 0 else (0, 2, 1, 0, 3, 2)
        pos.extend(p); nrm.extend(n)
        tris.append([base + order[0], base + order[1], base + order[2]])
        tris.append([base + order[3], base + order[4], base + order[5]])

    def pieces(self):
        out = []
        for slot, (pos, nrm, tris) in sorted(self.by_slot.items()):
            n = np.array(nrm, np.float32)
            n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-6)
            out.append(((np.array(pos, np.float32), n, np.array(tris, np.uint32)), slot))
        return out


def ribbon(faces, centres, wdir, halfw, normals, thick, top_slots, bottom_slot):
    """A strip of paper: section i is centred on centres[i], runs halfw[i] each way along wdir, and is `thick` thick
    along normals[i]. The +normal side takes top_slots[i] (one a segment), the other side and the edges bottom_slot."""
    w = np.array(wdir, np.float32)
    count = len(centres)
    lt, rt, lb, rb = [], [], [], []
    for c, h, n in zip(centres, halfw, normals):
        c = np.array(c, np.float32); n = np.array(n, np.float32)
        lt.append(c - w * h + n * thick * .5); rt.append(c + w * h + n * thick * .5)
        lb.append(c - w * h - n * thick * .5); rb.append(c + w * h - n * thick * .5)
    for i in range(count - 1):
        n0, n1 = np.array(normals[i], np.float32), np.array(normals[i + 1], np.float32)
        faces.quad(top_slots[i], (lt[i], rt[i], rt[i + 1], lt[i + 1]), (n0, n0, n1, n1))
        faces.quad(bottom_slot, (lb[i], rb[i], rb[i + 1], lb[i + 1]), (-n0, -n0, -n1, -n1))
        faces.quad(bottom_slot, (lt[i], lt[i + 1], lb[i + 1], lb[i]), (-w, -w, -w, -w))
        faces.quad(bottom_slot, (rt[i], rt[i + 1], rb[i + 1], rb[i]), (w, w, w, w))
    for i, sign in ((0, -1.0), (count - 1, 1.0)):
        j = min(max(i + int(sign), 0), count - 1)
        t = (np.array(centres[i], np.float32) - np.array(centres[j], np.float32))
        t /= max(float(np.linalg.norm(t)), 1e-6)
        faces.quad(bottom_slot, (lt[i], rt[i], rb[i], lb[i]), (t, t, t, t))


def loop(points, radius, slots, seg=5):
    """A closed cord through `points`, one colour a segment from `slots` in turn: the twisted thread."""
    p = np.array(points, np.float32)
    count = len(p)
    faces = Faces()
    rings, norms = [], []
    for i in range(count):
        t = p[(i + 1) % count] - p[i - 1]
        t /= max(float(np.linalg.norm(t)), 1e-6)
        up = np.array([0, 1, 0], np.float32)
        out = np.cross(up, t); out /= max(float(np.linalg.norm(out)), 1e-6)
        ring, nrm = [], []
        for s in range(seg):
            u = 2 * math.pi * s / seg
            d = out * math.cos(u) + up * math.sin(u)
            ring.append(p[i] + d * radius); nrm.append(d)
        rings.append(ring); norms.append(nrm)
    for i in range(count):
        j = (i + 1) % count
        for s in range(seg):
            s2 = (s + 1) % seg
            faces.quad(slots[i % len(slots)], (rings[i][s], rings[j][s], rings[j][s2], rings[i][s2]),
                       (norms[i][s], norms[j][s], norms[j][s2], norms[i][s2]))
    return faces.pieces()


def unit(v):
    v = np.array(v, np.float32)
    return v / max(float(np.linalg.norm(v)), 1e-9)


def on(centre, half, direction, out=1.0):
    """The point where `direction` leaves an ellipsoid, pushed `out` times as far from its middle."""
    d = unit(direction)
    return tuple(float(c + h * k * out) for c, h, k in zip(centre, half, d))


# ------------------------------------------------------------------ the pinwheel

STICK_TOP = .088           # the crossbar's height over the stick's foot
HUB_HEIGHT = .050          # the axle's height
WHEEL_FRONT = .0075        # the wheel stands this far in front of the stick
R = .035                   # the paper square's corner, from the axle


def blade(k):
    """One quarter of the paper square: a flat half out to its corner, and the other half curled over to the pin.
    The paper's face is green and its back is cream, so the flat shows green with a pale stripe at the corner, the
    curl comes over cream, and the inside of the curl is green again."""
    def corner(i):
        a = math.radians(45 + 90 * i)
        return np.array([R * math.cos(a), R * math.sin(a), 0], np.float32)

    c0, c1 = corner(k), corner(k + 1)
    m = (c0 + c1) * .5
    w = unit(m)
    half = float(np.linalg.norm(m)) * .5
    b0 = m * .5
    faces = Faces()
    back = np.array([0, 0, -1], np.float32)
    # The flat half: from the fold line (axle to mid-edge) out to its corner.
    front = -back
    ribbon(faces, [b0, b0 * .42 + c0 * .58 + w * .0003, c0 * .98], w, [half, half * .42, .0007], [front, front, front], .0012, [GREEN, PALE], CREAM)
    # The curled half: the same fold line, bent up and over until its corner is under the button.
    d = unit(c1 - b0)
    length = float(np.linalg.norm(c1 - b0))
    p0 = b0 + np.array([0, 0, .0006], np.float32)
    p1 = b0 + d * length * .60 + np.array([0, 0, .027], np.float32)
    p2 = np.array([0, 0, .0090], np.float32) + d * .0015
    steps = 6
    centres = []
    for i in range(steps):
        t = i / (steps - 1)
        centres.append((1 - t) ** 2 * p0 + 2 * t * (1 - t) * p1 + t * t * p2)
    centres = np.array(centres, np.float32)
    tangent = np.gradient(centres, axis=0)
    normals = np.cross(tangent, w)
    normals /= np.maximum(np.linalg.norm(normals, axis=1, keepdims=True), 1e-6)
    if normals[-1][2] < 0:
        normals = -normals
    widths = [half * (1 - .66 * (i / (steps - 1)) ** 1.3) for i in range(steps)]
    ribbon(faces, centres, w, widths, normals, .0012, [CREAM, CREAM, CREAM, GOLD, CREAM], GREEN)
    return faces.pieces(), c0


def pinwheel(model):
    stick = [(tube([(0, 0, 0), (0, STICK_TOP, 0)], .0027, seg=6), WOOD)]
    for y in (.0060, .0140):
        stick.append((lathe([(.0037, y - .0017), (.0040, y), (.0037, y + .0017)], seg=6), TEAL))
    model.add("stick", stick)

    bar = [(tube([(-.012, 0, 0), (.012, 0, 0)], .0022, seg=6), WOOD),
           (ellipsoid((.0034, .0034, .0034), at=(-.0135, 0, 0), seg=6, rings=3), GOLD),
           (ellipsoid((.0034, .0034, .0034), at=(.0135, 0, 0), seg=6, rings=3), GOLD)]
    model.add("perch", bar, at=(0, STICK_TOP, 0), parent="stick")

    # The axle: a peg from the stick to the paper, with a gold washer behind the wheel.
    axle = [(moved(lathe([(.0022, -WHEEL_FRONT), (.0022, .0)], seg=6), turn=(90, 0, 0)), WOOD),
            (moved(lathe([(.0058, -.0022), (.0062, -.0012), (.0058, -.0006)], seg=8), turn=(90, 0, 0)), GOLD)]
    model.add("wheel", axle, at=(0, HUB_HEIGHT, WHEEL_FRONT), parent="stick")
    for k in range(4):
        pieces, corner = blade(k)
        model.add("blade%d" % k, pieces, parent="wheel")
        tip = corner * .80
        model.add("blade%dtip" % k, ellipsoid((.0021, .0021, .0011), seg=6, rings=3), RUST,
                  at=(float(tip[0]), float(tip[1]), .0010), parent="blade%d" % k)

    # The button: a dished gold disc, two holes and the rust thread sewn through them.
    button = [(moved(lathe([(.0064, 0), (.0070, .0012), (.0066, .0027), (.0052, .0030), (.0046, .0022)], seg=10), turn=(90, 0, 0)), GOLD),
              (ellipsoid((.0011, .0011, .0006), at=(-.0020, 0, .0023), seg=6, rings=4), INK),
              (ellipsoid((.0011, .0011, .0006), at=(.0020, 0, .0023), seg=6, rings=4), INK),
              (tube([(-.0020, 0, .0027), (.0020, 0, .0027)], .00065, seg=4), RUST)]
    model.add("hub", button, at=(0, 0, .0093), parent="wheel")


def thread(model):
    # The band: a twisted two-strand cord round the heel of her hand. Measured off RosterArms/amihan_left: the hand
    # block is 0.21 by 0.254 half-wide from y 0.67 to 0.79, so the cord is a rounded rectangle just outside it.
    hx, hz, corner, s = .224 / BUILT_SCALE, .268 / BUILT_SCALE, .085 / BUILT_SCALE, 1.0
    pts = []
    for cx, cz, start in ((hx - corner, hz - corner, 0), (-(hx - corner), hz - corner, 90),
                          (-(hx - corner), -(hz - corner), 180), (hx - corner, -(hz - corner), 270)):
        for step in range(4):
            a = math.radians(start + 90 * step / 3)
            pts.append((cx + corner * math.cos(a), 0, cz + corner * math.sin(a)))
    model.add("band", loop(pts, .0030, [RUST, RUST2], seg=4))

    # The bow: a bead, two loops and two loose ends.
    bow = [(ellipsoid((.0034, .0030, .0034), seg=6, rings=4), RUST2),
           (moved(ellipsoid((.0052, .0026, .0020), seg=6, rings=3), at=(.0052, .0018, 0), turn=(0, 0, 24)), RUST),
           (moved(ellipsoid((.0052, .0026, .0020), seg=6, rings=3), at=(-.0052, .0018, 0), turn=(0, 0, -24)), RUST),
           (tube([(.0015, -.0010, .001), (.0050, -.0052, .002), (.0062, -.0098, .001)], [.0010, .0009, .0007], seg=4), RUST),
           (tube([(-.0015, -.0010, .001), (-.0042, -.0060, .002), (-.0070, -.0086, .001)], [.0010, .0009, .0007], seg=4), RUST)]
    model.add("knot", bow)
    model.add("string", tube([(0, 0, 0), (0, .02, 0)], .0010, seg=5), RUST)


# ------------------------------------------------------------------ the maya

def maya(model):
    feet = [(ellipsoid((.0032, .0022, .0056), at=(.0062, .0010, .0022), seg=6, rings=4), FEET),
            (ellipsoid((.0032, .0022, .0056), at=(-.0062, .0010, .0022), seg=6, rings=4), FEET)]
    model.add("bird", feet)

    body_half = (.0192, .0172, .0205)
    body = [(ellipsoid(body_half, seg=10, rings=7), BROWN),
            (ellipsoid((.0166, .0146, .0140), at=(0, -.0030, .0092), seg=10, rings=5), CHEEK),
            # The maya's bib, under the beak.
            (ellipsoid((.0052, .0042, .0028), at=on((0, 0, 0), body_half, (0, .50, .87), .97), seg=6, rings=4), INK),
            # Two dark streaks down the back.
            (moved(ellipsoid((.0024, .0022, .0080), seg=6, rings=3), at=on((0, 0, 0), body_half, (.42, .62, -.66), .93), turn=(38, -14, 0)), DARK),
            (moved(ellipsoid((.0024, .0022, .0080), seg=6, rings=3), at=on((0, 0, 0), body_half, (-.42, .62, -.66), .93), turn=(38, 14, 0)), DARK)]
    model.add("body", body, at=(0, .0178, 0), parent="bird")

    head_c, head_half = (0, .0115, .0030), (.0158, .0146, .0152)
    head = [(ellipsoid(head_half, at=head_c, seg=10, rings=7), BROWN),
            # The chestnut cap, a little wider than the head so it reads as a cap.
            (ellipsoid((.0150, .0082, .0150), at=(0, .0196, .0016), seg=10, rings=5), CAP)]
    for side in (1, -1):
        head.append((moved(ellipsoid((.0066, .0064, .0034), seg=6, rings=4), at=on(head_c, head_half, (side * .74, -.30, .60), .90), turn=(0, side * 50, 0)), CHEEK))
        head.append((moved(ellipsoid((.0024, .0026, .0013), seg=6, rings=3), at=on(head_c, head_half, (side * .78, -.36, .52), 1.04), turn=(0, side * 56, 0)), INK))
    model.add("head", head, at=(0, .0118, .0040), parent="body")

    for name, side in (("eyeL", 1), ("eyeR", -1)):
        at = on(head_c, head_half, (side * .43, .10, .90), .985)
        eye = [(ellipsoid((.0031, .0037, .0017), seg=6, rings=4), INK),
               (ellipsoid((.0011, .0012, .0007), at=(side * .0006 + .0006, .0014, .0013), seg=6, rings=3), WHITE)]
        model.add(name, [(moved(m, turn=(0, side * 22, 0)), s) for m, s in eye], at=at, parent="head")

    root = (0, head_c[1] - .0018, head_c[2] + head_half[2] - .0012)
    top = moved(lathe([(.0045, 0), (.0032, .0042), (0, .0092)], seg=6), turn=(90, 0, 0), scale=(1, 1, .62), at=(0, .0010, 0))
    low = moved(lathe([(.0038, 0), (.0026, .0036), (0, .0070)], seg=6), turn=(90, 0, 0), scale=(1, 1, .45), at=(0, -.0012, 0))
    model.add("beakTop", top, BEAK, at=root, parent="head")
    model.add("beakBottom", low, BEAK, at=root, parent="head")

    def wing(side):
        w = [(moved(ellipsoid((.0046, .0124, .0138), seg=8, rings=5), at=(.0020, -.0072, -.0058), turn=(-18, 0, 6)), DARK),
             (moved(ellipsoid((.0040, .0020, .0088), seg=6, rings=3), at=(.0044, -.0040, -.0044), turn=(-18, 0, 6)), CHEEK),
             (moved(ellipsoid((.0038, .0018, .0070), seg=6, rings=3), at=(.0042, -.0096, -.0070), turn=(-18, 0, 6)), BROWN)]
        return w if side > 0 else [(mirror(m), s) for m, s in w]

    model.add("wingL", wing(1), at=(.0166, .0050, .0016), parent="body")
    model.add("wingR", wing(-1), at=(-.0166, .0050, .0016), parent="body")

    tail = [(moved(rounded((.0090, .0026, .0100), .0026, seg=8, rings=4), at=(0, .0042, -.0088), turn=(24, 0, 0)), DARK),
            (moved(rounded((.0094, .0028, .0028), .0026, seg=8, rings=4), at=(0, .0076, -.0164), turn=(24, 0, 0)), CHEEK)]
    model.add("tail", tail, at=(0, -.0010, -.0170), parent="body")

    leaf = [(-.0002, -.0062), (.0026, -.0024), (.0027, .0020), (0, .0068), (-.0027, .0020), (-.0026, -.0024)]
    tip = [(.0024, .0030), (0, .0068), (-.0024, .0030)]
    model.add("feather", [(slab(leaf, .0011), BROWN), (slab(tip, .0015), CHEEK)])


def build():
    model = Model("amihan")
    thread(model)
    pinwheel(model)
    maya(model)
    return model


if __name__ == "__main__":
    import sys
    m = build()
    if "--posed" in sys.argv:
        # For the review only: the parts set where the game first puts them (the bird on the perch, the bow off to
        # the side), so the sheet shows the thing and not a pile at the origin. Never ship this file.
        placed = []
        for name, pieces, at, parent in m.parts:
            if name == "bird":
                at = (0.0, STICK_TOP + .0022, 0.0)
            elif name == "band":
                at = (0.0, -.0045, -.0760)
            elif name == "knot":
                at = (0.0, -.0010, 0.0)
            elif name == "string":
                at = (.03, -.03, 0.0)
            elif name == "feather":
                at = (.034, .100, .004)
            placed.append((name, pieces, at, parent))
        m.parts = placed
    m.write(PALETTE)
