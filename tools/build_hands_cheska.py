"""Yasmin's (id `cheska`) BARE first-person hands: frost that forms on her own wrists, hands and forearms.

  py -3 tools/build_hands_cheska.py                      the model the game loads (every piece at rest, no arm in it)
  py -3 tools/build_hands_cheska.py --pose=rest --arm    POSED on a stand-in arm, for a review picture only (see pose())
  py -3 tools/build_hands_cheska.py --pose=fall --arm    ... then run the first command again before leaving
  (poses: rest, creep, sprint, fall, charge, tagged, cast; add --side=right for the wrist that wears no sweatband)

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/cheska_hands.glb for `CheskaRimeHands`. No creature is in
it (owner, 2026-10-06: "we were trying to reserve the pet idea only for nemu ... i need something for the bare hands").
The cut crystals and shards are round one's (`faceted` is imported from tools/build_hand_cheska.py), without the bun
and without the drift they stood in: every piece here is rooted on her skin or her sweatband and has its origin AT its
root, so the C# grows it by scaling it out from where it sits.

EVERY PLACE BELOW IS TYPED IN THE LEFT ARM'S OWN SPACE (the arm runs along +y from the elbow at 0 to the fist's end at
0.84; +z is the side that faces the player; +x is the arm's outer side), measured off `RosterArms/cheska_left`, and is
written to the file at a quarter of that (`S`), the small scale the kit asks for. `CheskaRimeHands` spawns it at 4, so
a node's place in the file IS its place on the arm, and mirrors it in x for the other arm. The parts, by name:

  roots                 the rime each bracelet crystal stands in: five flat frost rosettes on the band. Never moves.
  gem0..4               the bracelet: a fan of five cut crystals along the band's outer edge (0 the big one in the
                        middle, 1 and 2 beside it, 3 and 4 the small ends). The node is the crystal's foot and carries
                        its lean; `shine<i>` (the bright crystal, which turns) and `dull<i>` (the same crystal cracked
                        and grey, for when she is tagged) are its children.
  stud0..3              the bracelet's beads across the band: cut studs at rest, a crown of spikes on a cast
  plate0..2             the rime on the back of the hand: three flat cut plates, each rooted at its wrist end
  claw0..2              ice claws over the knuckles (a fall), rooted on the fist, hooked over it
  shard0..2             thin shards down the forearm (a fall; the wind-up on the right arm), rooted on the skin
  cuff                  a cuff of frost for the RIGHT wrist, which wears no sweatband, so both bracelets sit alike
  chunk                 one loose piece of ice: the C# copies it for the pieces a landing breaks off
  puff                  her breath on the fist: one small frost cloud
"""
import json
import math
import struct
import sys

import numpy as np

from build_hand_cheska import faceted
from hand_companion_kit import MODELS, Model, ellipsoid, join, moved, slab

NAME = "cheska_hands"
S = .25                                  # the file's metres for one unit of the arm's space (`CheskaRimeHands.Scale` is 4)

# The sixteen colours, the same list as `CheskaRimeHands.Palette`.
PALETTE = [
    "#ffffff",  # 0 snow
    "#cfeaf2",  # 1 snow in shade
    "#5fe8d0",  # 2 her ice accent
    "#b8fff2",  # 3 her ice accent, light
    "#3b9eba",  # 4 the ice teal of `CheskaIceVisuals`
    "#1f6f86",  # 5 deep teal
    "#11485a",  # 6 the darkest teal: cracks
    "#46d7f0",  # 7 the cyan of her sweatband
    "#f2a37e",  # 8 her skin (the review's stand-in arm only)
    "#1f8296",  # 9 her sleeve's cuff (the review's stand-in arm only)
    "#e6fbff",  # 10 frost
    "#86d6e6",  # 11 mid ice
    "#2a86a2",  # 12 a facet in shade
    "#a3ecf7",  # 13 a facet in light
    "#7fa3ae",  # 14 dull ice, in shade
    "#a9c3ca",  # 15 dull ice
]
SNOW, SHADE, ACCENT, MINT, ICE, DEEP, CRACK, BAND, SKIN, SLEEVE, FROST, MID, FACET_DARK, FACET_LIGHT, DULL_DARK, DULL = range(16)


def unit(v):
    v = np.array(v, np.float64)
    return v / np.linalg.norm(v)


# ------------------------------------------------------------------ where things are on the arm

# The way out from the band's outer upper edge: the side of the wrist that is both towards the player and up the
# screen. The bracelet's fan stands along it and opens along the arm.
OUT = unit((.8, 0, .6))
ALONG = np.array((0, 1, 0), np.float64)
# From the arm to the player's eye (the arm is seen from behind the elbow and a little above), and the screen's up.
EYE = unit((-.065, -.254, .969))
SCREEN_UP = unit((.613, .399, .682))
BAND_Y = .587                            # the middle of the sweatband (it runs from .53 to .643)
BAND_EDGE = np.array((.190, 0, .228))    # its outer upper edge

# name, radius, height, how far along the band from its middle, degrees the fan opens it towards the fist
GEMS = [
    ("gem0", .052, .215, .000, 0),
    ("gem1", .042, .160, .024, 23),
    ("gem2", .042, .160, -.024, -23),
    ("gem3", .033, .112, .044, 46),
    ("gem4", .033, .112, -.044, -46),
]
# place on the band, the way it points
STUDS = [
    ((.060, BAND_Y, .237), (.22, 0, .98)),
    ((-.070, BAND_Y, .237), (-.32, 0, .95)),
    ((-.190, BAND_Y, .228), (-.80, 0, .60)),
    ((-.200, BAND_Y, .090), (-1.0, 0, .12)),
]
STUD_REST = .22                          # a stud is a spike at a fifth of its length
# root (its wrist end), length, width, thickness, degrees tipped down over the knuckles
PLATES = [
    ((0, .684, .259), .074, .215, .024, 0),
    ((0, .733, .267), .066, .160, .024, 4),
    ((0, .781, .258), .066, .125, .022, 42),
]
# root on the fist, size
CLAWS = [((0, .792, .246), 1.0), ((.082, .786, .236), .8), ((-.082, .786, .236), .8)]
CLAW_WAY, CLAW_HOOK = unit((0, .78, .62)), unit((0, .62, -.78))
# root on the forearm, length: a row on the side that faces her, just inside its outer edge, that carries the
# bracelet's fan on down the arm. They stand up the screen, and only lean back, because the sleeve's cuff hides
# whatever lies flat behind it.
SHARDS = [((.100, .506, .219), .215), ((.106, .468, .223), .180), ((.112, .432, .227), .150)]
SHARD_WAY = unit(OUT * .97 + ALONG * .10)      # standing, as the wind-up raises them
SHARD_RAKED = unit(OUT * .86 - ALONG * .50)    # raked back towards the elbow, as a fall lays them
GEM_BACK = unit(-ALONG * .94 + OUT * .34)      # laid back flat along the forearm, as a sprint lays the bracelet


def gem_way(degrees):
    a = math.radians(degrees)
    return unit(OUT * math.cos(a) + ALONG * math.sin(a))


def gem_root(offset):
    return BAND_EDGE + ALONG * (BAND_Y + offset) - OUT * .012


# ------------------------------------------------------------------ rotations (the kit's nodes carry a place only)

def aim(way, face):
    """The rotation that turns a piece's own +y to `way` and its own +z as near to `face` as that allows (x, y, z, w)."""
    y = unit(way)
    z = np.array(face, np.float64)
    z = unit(z - y * float(np.dot(z, y)))
    x = np.cross(y, z)
    m = np.stack([x, y, z], axis=1)
    t = m[0, 0] + m[1, 1] + m[2, 2]
    if t > 0:
        s = math.sqrt(t + 1) * 2
        q = ((m[2, 1] - m[1, 2]) / s, (m[0, 2] - m[2, 0]) / s, (m[1, 0] - m[0, 1]) / s, s / 4)
    elif m[0, 0] > m[1, 1] and m[0, 0] > m[2, 2]:
        s = math.sqrt(1 + m[0, 0] - m[1, 1] - m[2, 2]) * 2
        q = (s / 4, (m[0, 1] + m[1, 0]) / s, (m[0, 2] + m[2, 0]) / s, (m[2, 1] - m[1, 2]) / s)
    elif m[1, 1] > m[2, 2]:
        s = math.sqrt(1 + m[1, 1] - m[0, 0] - m[2, 2]) * 2
        q = ((m[0, 1] + m[1, 0]) / s, s / 4, (m[1, 2] + m[2, 1]) / s, (m[0, 2] - m[2, 0]) / s)
    else:
        s = math.sqrt(1 + m[2, 2] - m[0, 0] - m[1, 1]) * 2
        q = ((m[0, 2] + m[2, 0]) / s, (m[1, 2] + m[2, 1]) / s, s / 4, (m[1, 0] - m[0, 1]) / s)
    return tuple(float(v) for v in q)


def small(pieces):
    """Pieces typed in the arm's units, at the file's scale."""
    return [(moved(mesh, scale=(S, S, S)), slot) for mesh, slot in pieces]


# ------------------------------------------------------------------ the pieces (all typed in the arm's units)

# The six faces of a crystal as the player meets them: light towards her, dark behind. Side 1 is the face on +z.
GEM = [MID, FACET_LIGHT, ICE, FACET_DARK, DEEP, ICE]
TIP = [ACCENT, SNOW, MINT, MID, ICE, MID]
GREY = [DULL, SHADE, DULL, DULL_DARK, DULL_DARK, DULL]


def gem_foot(radius, height):
    """The crystal's narrow root, which stays whatever the rest of it does."""
    return faceted([(radius * .66, 0), (radius, height * .2)], 6, lambda ring, side: DEEP if ring < 0 else FACET_DARK if GEM[side] != FACET_LIGHT else ICE)


def gem_shine(radius, height, belt):
    """The crystal above its root: a fat shoulder, a snow-white point, and on the big one her sweatband again."""
    pieces = faceted([(radius, height * .2), (radius * .9, height * .68), (0, height)], 6,
                     lambda ring, side: DEEP if ring < 0 else TIP[side] if ring == 1 else GEM[side])
    if belt:
        y0, y1 = height * .30, height * .42
        r0 = radius * 1.10
        pieces += faceted([(r0, y0), (r0, y1)], 6, lambda ring, side: BAND)
        pieces += faceted([(r0 * 1.04, y0 + (y1 - y0) * .34), (r0 * 1.04, y0 + (y1 - y0) * .66)], 6, lambda ring, side: SNOW)
    return pieces


def gem_dull(radius, height):
    """The same crystal grey, with one crack down the face that looks at her (the C# stops it turning, face on)."""
    pieces = faceted([(radius, height * .2), (radius * .9, height * .68), (0, height)], 6,
                     lambda ring, side: DULL_DARK if ring < 0 else GREY[(side + 1) % 6] if ring == 1 else GREY[side])
    spine = [(.05, .22), (-.36, .36), (.30, .50), (-.16, .66)]
    width = max(.0055, radius * .15)
    bars = []
    for (x0, t0), (x1, t1) in zip(spine, spine[1:]):
        x0, x1, y0, y1 = x0 * radius, x1 * radius, t0 * height, t1 * height
        bars.append(slab([(x0 - width, y0), (x0 + width, y0), (x1 + width, y1), (x1 - width, y1)], .008))
    pieces.append((moved(join(*bars), at=(0, 0, radius * .845)), CRACK))
    return pieces


def rosette(radius):
    """The rime a crystal stands in: a low six-sided plate of frost, snow on top."""
    return faceted([(radius * 1.55, -.004), (radius * 1.2, .012)], 6, lambda ring, side: SNOW if ring < 0 else (FROST, SHADE)[side % 2], twist=math.pi / 6)


def spike():
    """A bead of the bracelet at full length: four-sided, mint, with a deep foot."""
    return faceted([(.026, 0), (.031, .030), (0, .150)], 4, lambda ring, side: DEEP if ring < 0 else (ACCENT, MINT, ACCENT, ICE)[side] if ring > 0 else (ICE, FACET_LIGHT, ICE, FACET_DARK)[side],
                   twist=math.pi / 4)


def plate(length, width, thick):
    """A flat cut plate of rime lying on the hand: its root edge on the origin, its length along +y, its top on +z.
    A second small plate in her accent is cut into its top."""
    def tablet(l, w, t, top, sides):
        # A corner of the six points down the hand, towards the knuckles: a scale of ice, not a nut.
        raw = faceted([(1.0, 0), (.74, 1.0)], 6, lambda ring, side: top if ring < 0 else sides[side], twist=math.pi / 6)
        return [(moved(mesh, at=(0, l * .5, 0), turn=(90, 0, 0), scale=(w * .5 / .866, t, l * .5)), slot) for mesh, slot in raw]
    pieces = tablet(length, width, thick, FROST, [MID, FACET_DARK, MID, FACET_LIGHT, SNOW, FACET_LIGHT])
    ridge = tablet(length * .46, width * .40, thick * .45, MINT, [ACCENT] * 6)
    pieces += [(moved(mesh, at=(0, length * .27, thick * .98)), slot) for mesh, slot in ridge]
    return pieces


def claw(size):
    """An ice claw: four-sided, fat at the root, hooked along its own +z."""
    return faceted([(.034 * size, 0), (.046 * size, .034 * size), (.034 * size, .095 * size), (0, .175 * size)], 4,
                   lambda ring, s: DEEP if ring < 0 else (FACET_LIGHT, ICE, FACET_DARK, ICE)[s] if ring < 2 else (SNOW, MINT, MID, MINT)[s],
                   twist=math.pi / 4, squash_z=.8, bend=.05 * size)


def shard(length):
    """A thin shard: round one's, slimmer. Bent along its own +z."""
    k = length / .216
    return faceted([(.026 * k, 0), (.040 * k, .044 * k), (.030 * k, .128 * k), (0, length)], 4,
                   lambda ring, s: DEEP if ring < 0 else (FACET_LIGHT, ICE, FACET_DARK, ICE)[s] if ring < 2 else (SNOW, MINT, MID, MINT)[s],
                   twist=math.pi / 4, squash_z=.8, bend=.045 * k)


def box_ring(hx, hz, cut):
    """The arm's cross-section: a box of half-size hx, hz with its corners cut by `cut`, counter-clockwise seen from +y."""
    return [(hx, hz - cut), (hx - cut, hz), (-hx + cut, hz), (-hx, hz - cut), (-hx, -hz + cut), (-hx + cut, -hz), (hx - cut, -hz), (hx, -hz + cut)]


def prism(ring0, y0, ring1, y1, slot):
    """A solid between two cross-sections (lists of (x, z)), flat faces, capped."""
    def corner(ring, y, k):
        x, z = ring[k % len(ring)]
        return np.array((x, y, z), np.float32)
    middle = np.array((0, (y0 + y1) * .5, 0), np.float32)
    pos, nrm, tris = [], [], []

    def face(points):
        n = np.cross(points[1] - points[0], points[2] - points[0])
        n /= max(float(np.linalg.norm(n)), 1e-9)
        if float(np.dot(n, sum(points) / len(points) - middle)) < 0:
            points = points[::-1]
            n = -n
        base = len(pos)
        pos.extend(points)
        nrm.extend([n] * len(points))
        for i in range(1, len(points) - 1):
            tris.append((base, base + i, base + i + 1))

    count = len(ring0)
    for k in range(count):
        face([corner(ring0, y0, k), corner(ring0, y0, k + 1), corner(ring1, y1, k + 1), corner(ring1, y1, k)])
    face([corner(ring0, y0, k) for k in range(count)])
    face([corner(ring1, y1, k) for k in range(count)])
    return [((np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32)), slot)]


def cuff():
    """The right wrist's cuff of frost, as big round as the left wrist's sweatband: frost, with a stripe cut in mint."""
    pieces = prism(box_ring(.203, .240, .022), .548, box_ring(.203, .240, .022), .626, FROST)
    pieces += prism(box_ring(.209, .246, .022), .560, box_ring(.209, .246, .022), .572, MINT)
    pieces += prism(box_ring(.209, .246, .022), .602, box_ring(.209, .246, .022), .614, MINT)
    return pieces


def stand_in_arm(right):
    """Her first-person arm, roughly, for a review picture ONLY (never in the game's model): white sleeve, teal cuff,
    bare forearm, the cyan sweatband (left arm only), the block hand."""
    pieces = prism(box_ring(.29, .30, .06), .06, box_ring(.30, .31, .06), .31, SNOW)
    pieces += prism(box_ring(.328, .338, .06), .305, box_ring(.328, .338, .06), .403, SLEEVE)
    pieces += prism(box_ring(.197, .235, .042), .394, box_ring(.169, .211, .042), .629, SKIN)
    if not right:
        pieces += prism(box_ring(.200, .237, .019), .530, box_ring(.200, .237, .019), .643, BAND)
        pieces += prism(box_ring(.204, .241, .019), .566, box_ring(.204, .241, .019), .584, SNOW)
        pieces += prism(box_ring(.204, .241, .019), .598, box_ring(.204, .241, .019), .612, SNOW)
    pieces += prism(box_ring(.127, .169, .017), .629, box_ring(.127, .169, .017), .690, SKIN)
    pieces += prism(box_ring(.179, .263, .057), .690, box_ring(.169, .254, .057), .784, SKIN)
    pieces += prism(box_ring(.169, .254, .057), .784, box_ring(.113, .197, .022), .840, SKIN)
    return pieces


def build(with_arm=False, right=False):
    """Returns the model and the rotation of every node that has one."""
    m = Model(NAME)
    turns = {}

    def put(name, pieces, at=(0, 0, 0), turn=None, parent=None):
        m.add(name, small(pieces), at=tuple(float(v) * S for v in at), parent=parent)
        if turn is not None:
            turns[name] = turn

    # ---------------- the bracelet: a fan of five crystals on the band's outer edge, and the rime they stand in
    roots = []
    for name, radius, height, offset, degrees in GEMS:
        q = aim(gem_way(degrees), EYE)
        at = gem_root(offset)
        roots += [(moved(mesh, at=tuple(at)), slot) for mesh, slot in turned(rosette(radius), q)]
        put(name, gem_foot(radius, height), at=at, turn=q)
        put("shine" + name[3:], gem_shine(radius, height, belt=name == "gem0"), parent=name)
        put("dull" + name[3:], gem_dull(radius, height), parent=name)
    put("roots", roots)

    # ---------------- its beads across the band
    for k, (at, way) in enumerate(STUDS):
        put("stud%d" % k, spike(), at=at, turn=aim(way, ALONG))

    # ---------------- the rime on the back of the hand
    for k, (at, length, width, thick, tip) in enumerate(PLATES):
        a = math.radians(tip)
        put("plate%d" % k, plate(length, width, thick), at=at, turn=aim((0, math.cos(a), -math.sin(a)), (0, math.sin(a), math.cos(a))))

    # ---------------- what a fall grows: claws over the knuckles, shards down the forearm
    for k, (at, size) in enumerate(CLAWS):
        put("claw%d" % k, claw(size), at=at, turn=aim(CLAW_WAY, CLAW_HOOK))
    for k, (at, length) in enumerate(SHARDS):
        put("shard%d" % k, shard(length), at=at, turn=aim(SHARD_WAY, -ALONG))

    # ---------------- the right wrist's cuff, the loose piece, her breath
    put("cuff", cuff())
    put("chunk", faceted([(0, -.028), (.0224, -.0048), (.0152, .0136), (0, .0344)], 4,
                         lambda ring, s: (FACET_LIGHT, ICE, FACET_DARK, MID)[s] if ring < 2 else (SNOW, MINT, MID, MINT)[s], twist=.5))
    put("puff", [(ellipsoid((.029, .025, .024), seg=8, rings=4), FROST),
                 (ellipsoid((.021, .018, .018), at=(.026, -.004, .002), seg=8, rings=4), SNOW),
                 (ellipsoid((.018, .017, .017), at=(-.025, -.006, .002), seg=8, rings=4), SNOW),
                 (ellipsoid((.016, .014, .014), at=(.004, .022, .000), seg=8, rings=4), SHADE)])
    if with_arm:
        put("arm", stand_in_arm(right))
    return m, turns


def turned(pieces, q):
    """`pieces` turned by the rotation `q` (x, y, z, w) about the origin."""
    x, y, z, w = q
    r = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                  [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                  [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]], np.float32)
    return [((p @ r.T, n @ r.T, t), slot) for (p, n, t), slot in pieces]


# ------------------------------------------------------------------ review poses

HIDE = (.0001, .0001, .0001)


def pose(name, right):
    """What the C# does to the nodes in one state, typed by hand so a picture can be taken without the game:
    node -> (rotation or None, scale or None)."""
    p = {"chunk": (None, HIDE), "puff": (None, HIDE)}
    if not right:
        p["cuff"] = (None, HIDE)
    for k in range(5):
        p["dull%d" % k] = (None, HIDE)
    for k in range(4):
        p["stud%d" % k] = (None, (.85, STUD_REST, .85))
    for k in range(3):
        p["claw%d" % k] = (None, HIDE)
        p["shard%d" % k] = (None, HIDE)
    p["plate2"] = (None, HIDE)
    if name == "creep":
        del p["plate2"]
        p["gem1"] = (None, (1.3, 1.3, 1.3))
    elif name == "sprint":
        for gem, _, _, _, degrees in GEMS:
            way = unit(gem_way(degrees) * .12 + GEM_BACK * .88)
            p[gem] = (aim(way, EYE), None)
    elif name == "fall":
        del p["plate2"]
        for k in range(3):
            p["plate%d" % k] = (None, (1, 1, 2.1))
            p["claw%d" % k] = (None, None)
            p["shard%d" % k] = (aim(SHARD_RAKED, -ALONG), None)
    elif name == "charge":
        for k in range(3):
            p["plate%d" % k] = (None, HIDE)
            p["shard%d" % k] = (None, (.8, .8, .8))
    elif name == "tagged":
        for k in range(5):
            p["dull%d" % k] = (None, None)
            p["shine%d" % k] = (None, HIDE)
    elif name == "cast":
        for k in range(4):
            p["stud%d" % k] = (None, None)
        for gem, _, _, _, degrees in GEMS:
            p[gem] = (aim(gem_way(degrees * 1.55), EYE), (1.2, 1.2, 1.2))
    elif name != "rest":
        raise SystemExit("no such pose: " + name)
    return p


def dress_nodes(path, turns, posed):
    """Open the kit's .glb again and give the nodes their rotations (and a review pose its changes)."""
    data = path.read_bytes()
    length = struct.unpack_from("<I", data, 12)[0]
    doc = json.loads(data[20:20 + length])
    rest = data[20 + length:]
    for node in doc["nodes"]:
        name = node.get("name")
        if name in turns:
            node["rotation"] = [float(v) for v in turns[name]]
        if posed and name in posed:
            turn, scale = posed[name]
            if turn is not None:
                node["rotation"] = [float(v) for v in turn]
            if scale is not None:
                node["scale"] = [float(v) for v in scale]
    text = json.dumps(doc, separators=(",", ":")).encode()
    text += b" " * (-len(text) % 4)
    path.write_bytes(struct.pack("<4sII", b"glTF", 2, 20 + len(text) + len(rest)) + struct.pack("<I4s", len(text), b"JSON") + text + rest)


if __name__ == "__main__":
    asked = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--pose=")), None)
    is_right = "--side=right" in sys.argv[1:]
    with_arm = "--arm" in sys.argv[1:]
    if (with_arm or is_right) and not asked:
        raise SystemExit("--arm and --side are for a posed review picture: give --pose too")
    model, node_turns = build(with_arm, is_right)
    written = model.write(PALETTE)
    dress_nodes(written, node_turns, pose(asked, is_right) if asked else None)
    print("posed for a picture: %s. Build again with no arguments before leaving." % asked if asked else "the game's model (nothing posed, no arm)")
    assert written == MODELS / (NAME + ".glb")
