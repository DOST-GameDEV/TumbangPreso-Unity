"""Amihan's bare hands: the wind dresses her, and you read it in her clothes.

Owner, 2026-10-06, of round one's creatures on every hand: "i like it but we were trying to reserve the pet idea only
for nemu... so i need something for the bare hands". And of the first bare hands (Paete's): "its kinda gross looking
because they're kinda just flailing around like tentacles". So nothing here is a character and nothing writhes. These
are pieces of HER OWN CLOTHING, and `AmihanWindHands.cs` lets the air move them:

  * a rust-red thread wrapped twice round the heel of each hand, tied in a small bow, with two loose tails. Each tail
    is four short tapered lengths, each its own part with its origin at its joint, and ends in a gold bead and a tuft;
  * two loose cloth tabs sewn to the edge of each bell sleeve: her sleeve's teal, a gold tip, a cream stitch, a rust
    thread where they are sewn on. Each is one hinged part with its origin on the hinge;
  * two solid wind ribbons a hand, hidden at rest (the code scales them from nothing at their root): a long streamer,
    and a coil that rings the wrist. Her wind is ribbons and never flat plates (`WindVfx`), so each is a round tapered
    swept tube in her wind's pale tone with a thin green strand beside it.

  py -3 tools/build_hands_amihan.py                 the model the game loads
  py -3 tools/build_hands_amihan.py --pose=rest     FOR THE REVIEW ONLY: posed on a stand-in arm (also fall, sprint,
                                                    glide, cast, slipper). Rebuild the plain model last.

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/amihan_hands.glb: ONE hand's set. The code spawns it twice
and places every part itself, mirrored for the right arm. Every part is symmetric from side to side and from front to
back (or only ever turned about its own long axis), so it does not matter which axis the importer mirrors.

Everything below is typed in THE ARM'S OWN UNITS (the arm mesh runs along +y from the elbow at 0 to the fingertips at
0.84; measured off `RosterArms/amihan_left`: the hand block is 0.21 by 0.254 half-wide from 0.67 to 0.79, the sleeve's
rim is 0.31 by 0.33 half-wide at 0.48 to 0.51) and written at 1/K, so the model is a real small thing in metres and
the code scales it back up by `AmihanWindHands.Scale`.

The parts (all children of the root; each origin is the part's pivot):
  wrap               the thread's two turns, centred on the arm's axis; y is the arm's axis
  knot               the bow, origin where it sits on the wrap
  tailA0..3 tailB0..3   one length of a loose tail each, along +y from its joint; the last carries the bead
  tabA tabB          a cloth tab along +y from its hinge, thin along z
  ribbonA            the streamer, along +y from its root (0.5 long)
  ribbonB            the coil, round the y axis (0.30 across its radius), its middle at the origin
"""
import math
import sys

import numpy as np

from hand_companion_kit import Model, ellipsoid, join, lathe, mirror, moved, rounded, tube

K = 4.0                    # `AmihanWindHands.Scale`

PALETTE = [
    "#B8472E",  # 0 rust red thread
    "#8A2F20",  # 1 the thread's darker strand
    "#2E8C86",  # 2 teal, her sleeve
    "#1F6B68",  # 3 deep teal (kept for the code's use; the stand-in sleeve's shade)
    "#E8B64A",  # 4 gold, her sleeve's edge
    "#F1E4C8",  # 5 cream, her cuff
    "#ECFBDF",  # 6 the wind's pale core
    "#A6EC84",  # 7 the wind's green body
    "#D08C63",  # 8 her skin (the review's stand-in arm only)
    "#C99A3A",  # 9 gold, shaded
    "#1E1A18",  # 10 ink
    "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFFFFF",
]
RUST, RUST2, TEAL, TEAL2, GOLD, CREAM, PALE, GREEN, SKIN, GOLD2, INK = range(11)

# ---- the numbers the C# shares (arm units). Keep them the same in `AmihanWindHands.cs`.
WRAP_ALONG = .70
WRAP_HALF = (.226, .270)
KNOT = (-.200, .70, .245)            # the left arm's; the right arm's is the mirror in x
TAIL_SEG = (.078, .066)              # one length of tail A, of tail B
TAB_HINGE = ((.135, .503, .318), (-.135, .503, .318))
TAB_LENGTH = (.190, .165)
RIBBON_A_LENGTH = .50
RIBBON_B_RADIUS = .30


# ------------------------------------------------------------------ a swept tube in several colours

def sweep(points, radii, slots, seg=4):
    """A round tube along `points` with one radius a point and one palette slot a SEGMENT, closed to a point at both
    ends. The frame is carried along the path so it does not twist. Returns [(mesh, slot)]."""
    p = np.array(points, np.float64)
    count = len(p)
    tangent = np.gradient(p, axis=0)
    tangent /= np.maximum(np.linalg.norm(tangent, axis=1, keepdims=True), 1e-9)
    ref = np.array([0, 0, 1.0]) if abs(tangent[0][2]) < .9 else np.array([1.0, 0, 0])
    side = np.cross(tangent[0], ref)
    side /= np.linalg.norm(side)
    rings, norms = [], []
    for i in range(count):
        side = side - tangent[i] * float(np.dot(side, tangent[i]))
        side /= max(float(np.linalg.norm(side)), 1e-9)
        other = np.cross(tangent[i], side)
        ring, nrm = [], []
        for s in range(seg):
            u = 2 * math.pi * s / seg
            d = side * math.cos(u) + other * math.sin(u)
            ring.append(p[i] + d * radii[i]); nrm.append(d)
        rings.append(ring); norms.append(nrm)
    by_slot = {}

    def tri(slot, pts, nrms, outward):
        pos, nrm, tris = by_slot.setdefault(slot, ([], [], []))
        base = len(pos)
        face = np.cross(pts[1] - pts[0], pts[2] - pts[0])
        order = (0, 1, 2) if float(np.dot(face, outward)) >= 0 else (0, 2, 1)
        pos.extend(pts); nrm.extend(nrms)
        tris.append([base + order[0], base + order[1], base + order[2]])

    for i in range(count - 1):
        slot = slots[i % len(slots)]
        for s in range(seg):
            s2 = (s + 1) % seg
            a, b, c, d = rings[i][s], rings[i + 1][s], rings[i + 1][s2], rings[i][s2]
            na, nb, nc, nd = norms[i][s], norms[i + 1][s], norms[i + 1][s2], norms[i][s2]
            out = na + nb + nc + nd
            tri(slot, [a, b, c], [na, nb, nc], out)
            tri(slot, [a, c, d], [na, nc, nd], out)
    for i, sign in ((0, -1.0), (count - 1, 1.0)):
        slot = slots[min(i, count - 2) % len(slots)]
        t = tangent[i] * sign
        tip = p[i] + t * radii[i] * 1.2
        for s in range(seg):
            s2 = (s + 1) % seg
            tri(slot, [rings[i][s], rings[i][s2], tip], [norms[i][s], norms[i][s2], t], t + norms[i][s] + norms[i][s2])
    out = []
    for slot, (pos, nrm, tris) in sorted(by_slot.items()):
        n = np.array(nrm, np.float32)
        n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-6)
        out.append(((np.array(pos, np.float32), n, np.array(tris, np.uint32)), slot))
    return out


# ------------------------------------------------------------------ the thread

def wrap_part(model):
    # Two turns round the heel of the hand, a rounded rectangle just outside the block, rising a little as it goes:
    # a twisted two-strand cord (the colour changes every length).
    hx, hz, corner = WRAP_HALF[0], WRAP_HALF[1], .075
    loop = []
    for cx, cz, start in ((hx - corner, hz - corner, 0), (-(hx - corner), hz - corner, 90),
                          (-(hx - corner), -(hz - corner), 180), (hx - corner, -(hz - corner), 270)):
        for step in range(3):
            a = math.radians(start + 90 * step / 2)
            loop.append((cx + corner * math.cos(a), cz + corner * math.sin(a)))
    pts, turns = [], 2
    total = len(loop) * turns
    for i in range(total + 1):
        x, z = loop[i % len(loop)]
        pts.append((x, -.019 + .038 * i / total, z))
    radii = [.0155] * len(pts)
    radii[0] = radii[-1] = .011
    model.add("wrap", sweep(pts, radii, [RUST, RUST2], seg=4))


def knot_part(model):
    # The bow where it is tied: a bead and two loops. The loose tails are their own parts.
    bow = [(ellipsoid((.030, .026, .028), seg=6, rings=4), RUST2),
           (moved(ellipsoid((.040, .020, .016), seg=5, rings=3), at=(.034, .020, 0), turn=(0, 0, 32)), RUST),
           (moved(ellipsoid((.040, .020, .016), seg=5, rings=3), at=(-.034, .020, 0), turn=(0, 0, -32)), RUST)]
    model.add("knot", bow)


def tail_parts(model):
    for name, length, thick in (("tailA", TAIL_SEG[0], .0215), ("tailB", TAIL_SEG[1], .0195)):
        for k in range(4):
            r0, r1 = thick * (1 - .14 * k), thick * (1 - .14 * (k + 1))
            # A slight belly, and a point at each end that sits inside the next length, so a bent joint shows no gap.
            pieces = sweep([(0, 0, 0), (0, length * .5, 0), (0, length, 0)], [r0, (r0 + r1) * .54, r1],
                           [RUST if k % 2 == 0 else RUST2], seg=4)
            if k == 3:
                # The weight on the end: a gold bead, a darker collar and a short tuft of the thread.
                pieces.append((ellipsoid((.033, .030, .033), at=(0, length + .016, 0), seg=6, rings=4), GOLD))
                pieces.append((lathe([(.017, length + .038), (.023, length + .058), (.005, length + .086)], seg=5), RUST))
            model.add("%s%d" % (name, k), pieces)


# ------------------------------------------------------------------ the sleeve's tabs

def tab_parts(model):
    for name, length, half in (("tabA", TAB_LENGTH[0], .068), ("tabB", TAB_LENGTH[1], .060)):
        body = length - .046
        pieces = [(rounded((half, body * .5, .0135), .0135, at=(0, body * .5, 0), seg=8, rings=4), TEAL),
                  # The gold tip, a little wider and thicker than the cloth: her sleeve's own edge again.
                  (rounded((half + .006, .032, .0185), .018, at=(0, length - .032, 0), seg=8, rings=4), GOLD),
                  # A cream stitch across it, proud on both faces.
                  (rounded((half - .012, .0065, .0165), .006, at=(0, body * .56, 0), seg=6, rings=4), CREAM),
                  # The rust thread it is sewn on with.
                  (tube([(-(half - .014), .004, 0), (half - .014, .004, 0)], .0115, seg=3), RUST)]
        model.add(name, pieces)


# ------------------------------------------------------------------ the wind's ribbons

def swell(u, power=.6):
    return max(math.sin(math.pi * u), 0.0) ** power


def ribbon_parts(model):
    # The streamer: a long loose curl about +y that starts ON its root and ends in a point. Turned about its own
    # axis by the code, the curl travels along it.
    steps, turns = 10, 1.25
    main, strand = [], []
    for i in range(steps + 1):
        u = i / steps
        a = 2 * math.pi * turns * u
        rho = .052 * math.sin(math.pi * min(u * 1.15, 1.0) * .5) * (1 - .35 * u)
        main.append((rho * math.cos(a), RIBBON_A_LENGTH * u, rho * math.sin(a)))
    radii = [max(.004, .033 * swell(.06 + .9 * i / steps)) for i in range(steps + 1)]
    for i in range(6):
        u = .16 + .62 * i / 5
        a = 2 * math.pi * turns * u + math.radians(125)
        rho = .052 * math.sin(math.pi * min(u * 1.15, 1.0) * .5) * (1 - .35 * u) + .030
        strand.append((rho * math.cos(a), RIBBON_A_LENGTH * u, rho * math.sin(a)))
    model.add("ribbonA", sweep(main, radii, [PALE], seg=4)
              + sweep(strand, [.003, .010, .012, .011, .008, .003], [GREEN], seg=3))

    # The coil: a turn and a half round the y axis, wide enough to ring her wrist, thick in the middle of its run.
    steps, turns = 22, 1.55
    coil = []
    for i in range(steps + 1):
        u = i / steps
        a = 2 * math.pi * turns * u
        coil.append((RIBBON_B_RADIUS * math.cos(a), -.065 + .13 * u, RIBBON_B_RADIUS * math.sin(a)))
    radii = [max(.004, .031 * swell(.05 + .9 * i / steps)) for i in range(steps + 1)]
    model.add("ribbonB", sweep(coil, radii, [PALE], seg=4))


def build():
    model = Model("amihan_hands")
    wrap_part(model)
    knot_part(model)
    tail_parts(model)
    tab_parts(model)
    ribbon_parts(model)
    return model


def shrink(model):
    """Arm units to metres: every mesh and every origin at 1/K."""
    out = []
    for name, pieces, at, parent in model.parts:
        out.append((name, [((m[0] / K, m[1], m[2]), slot) for m, slot in pieces], tuple(v / K for v in at), parent))
    model.parts = out


# ------------------------------------------------------------------ FOR THE REVIEW ONLY: a pose on a stand-in arm

# The left arm's own axes in the view's space at its natural rest (`ViewmodelArms.SetRestPlacement`): +x right,
# +y up the screen, +z away from the player.
AX = np.array([-.7605, .6127, -.2148])
AY = np.array([.0630, .3991, .9147])
AZ = np.array([.6461, .6823, -.3422])
ARM = np.stack([AX, AY, AZ], axis=1)            # arm space to view space
UP, DOWN, BACK, INB = np.array([0, 1.0, 0]), np.array([0, -1.0, 0]), np.array([0, 0, -1.0]), np.array([1.0, 0, 0])


def unit(v):
    v = np.array(v, np.float64)
    return v / max(float(np.linalg.norm(v)), 1e-9)


def from_up(direction):
    """The shortest turn that takes +y onto `direction` (Unity's `Quaternion.FromToRotation(Vector3.up, d)`)."""
    d = unit(direction)
    axis = np.cross(UP, d)
    s, c = float(np.linalg.norm(axis)), float(np.dot(UP, d))
    if s < 1e-8:
        return np.eye(3) if c > 0 else np.diag([1.0, -1.0, -1.0])
    k = axis / s
    kx = np.array([[0, -k[2], k[1]], [k[2], 0, -k[0]], [-k[1], k[0], 0]])
    return np.eye(3) + kx * s + kx @ kx * (1 - c)


def about(axis, degrees):
    a = math.radians(degrees)
    c, s = math.cos(a), math.sin(a)
    if axis == "x":
        return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])
    if axis == "y":
        return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])


def placed(pieces, turn, at, scale=(1, 1, 1)):
    """A part's pieces scaled, turned by the matrix `turn` and moved to `at`, all in the view's space."""
    out = []
    s = np.array(scale, np.float64)
    for (p, n, t), slot in pieces:
        p2 = (p * s) @ turn.T + np.array(at)
        n2 = (n / s) @ turn.T
        n2 /= np.maximum(np.linalg.norm(n2, axis=1, keepdims=True), 1e-6)
        out.append(((p2.astype(np.float32), n2.astype(np.float32), t), slot))
    return out


def rim(theta, height, hx=.246, hz=.290):
    """A point on the rounded outline of the hand's heel (`AmihanWindHands.Rim`), in arm space."""
    c, s = math.cos(theta), math.sin(theta)
    r = (abs(c / hx) ** 4 + abs(s / hz) ** 4) ** -.25
    return np.array([r * c, height, r * s])


def pose(model, name):
    """What `AmihanWindHands` asks of the parts once its springs have settled in one state, for the LEFT hand."""
    parts = {n: pieces for n, pieces, _, _ in model.parts}
    view = lambda p: ARM @ np.array(p, np.float64)
    out = []

    def add(part, turn, at, scale=(1, 1, 1)):
        out.extend(placed(parts[part], turn, at, scale))

    # The stand-in arm: sleeve, gold edge, cream cuff, forearm, hand block.
    arm = [(rounded((.21, .16, .21), .06, at=(0, .25, 0)), TEAL), (rounded((.295, .075, .312), .05, at=(0, .415, 0)), TEAL),
           (rounded((.313, .024, .330), .02, at=(0, .470, 0)), GOLD), (rounded((.290, .014, .302), .012, at=(0, .498, 0)), CREAM),
           (rounded((.168, .10, .182), .05, at=(0, .575, 0)), SKIN), (rounded((.210, .090, .254), .055, at=(0, .755, 0)), SKIN)]
    out.extend(placed(arm, ARM, (0, 0, 0)))

    add("wrap", ARM, view((0, WRAP_ALONG, 0)))
    anchor = view(KNOT)
    add("knot", ARM @ about("y", -39), anchor)

    # ---- the tails: where each length points, by state
    radial = unit(ARM @ np.array([-.63, 0, .77]))
    arm_back, arm_fwd = -AY, AY
    wrap_tails = name == "slipper"
    for t, (tail, length) in enumerate((("tailA", TAIL_SEG[0]), ("tailB", TAIL_SEG[1]))):
        spread = INB * (.10 if t == 0 else -.06)
        nodes = [anchor]
        for k in range(4):
            if name == "rest":
                d = DOWN + INB * .12 + spread
            elif name == "fall":
                d = UP * 1.1 + INB * .10 + spread + np.array([math.sin(k * 1.3 + t), 0, 0]) * .06
            elif name == "sprint":
                d = BACK * .8 + DOWN * (.35 + .04 * k) + INB * .25 + spread + UP * math.sin(k * 1.4 + t * 2) * .05
            elif name == "glide":
                d = INB * .35 + BACK * .9 + UP * .08 + spread * .5
            elif name == "cast":
                d = radial + UP * .35 + spread * 2
            else:
                d = DOWN + INB * .12 + spread
            nodes.append(nodes[-1] + unit(d) * length)
        if wrap_tails:
            # Wound round the heel of the hand from the knot, across the face the player sees.
            theta = math.atan2(KNOT[2], KNOT[0])
            nodes = [anchor]
            for k in range(1, 5):
                at = rim(theta, WRAP_ALONG + (.040 if t == 0 else -.040))
                theta -= length / float(np.linalg.norm(at[[0, 2]]))
                at = rim(theta, WRAP_ALONG + (.040 if t == 0 else -.040))
                nodes.append(view(at))
        for k in range(4):
            run = nodes[k + 1] - nodes[k]
            add("%s%d" % (tail, k), from_up(run), nodes[k], (1, float(np.linalg.norm(run)) / length, 1))

    # ---- the tabs: one angle each about the hinge (0 along the arm, + lifting off it toward the player)
    angle = {"rest": -8, "fall": 64, "sprint": 150, "glide": 125, "cast": 88, "slipper": -8}[name]
    for j, tab in enumerate(("tabA", "tabB")):
        splay = (14 if name == "cast" else 0) * (1 if j == 0 else -1)
        add(tab, ARM @ about("z", -splay) @ about("x", angle + (5 if j else 0)), view(TAB_HINGE[j]))

    # ---- the ribbons: hidden unless the air is really moving
    if name == "sprint":
        add("ribbonA", ARM @ from_up((0, -.95, .30)), view((0, .80, .285)))
    elif name == "fall":
        add("ribbonA", from_up(UP) @ about("y", 40), view((0, .80, .270)), (1, .6, 1))
        add("ribbonB", ARM, view((0, .93, 0)), (.62, 1.5, .62))
    elif name == "glide":
        add("ribbonA", from_up(BACK + UP * .10), view((0, .74, .285)), (1, 1.25, 1))
        add("ribbonB", ARM, view((0, .585, 0)), (1.08, .85, 1.08))
    elif name == "cast":
        add("ribbonB", ARM @ about("y", 70), view((0, .60, 0)))

    # Unity's view space is left-handed and the file is not: mirror x, so the review's "back" picture (from the
    # player's side, a little toward the screen's middle and above) shows the left hand as the game's camera does.
    posed = Model(model.name)
    for mesh, slot in out:
        posed.add("p%d" % len(posed.parts), mirror(mesh), slot)
    return posed


if __name__ == "__main__":
    m = build()
    which = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--pose=")), None)
    if which:
        m = pose(m, which)          # never ship this file: rebuild without the flag
    shrink(m)
    m.write(PALETTE)
