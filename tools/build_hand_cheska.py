"""Yasmin's (id `cheska`) first-person hand companion: a frost cluster on her sweatband with a snow-bun nesting in it.

  py -3 tools/build_hand_cheska.py                 the model the game loads (every node at rest, loose pieces at 0,0,0)
  py -3 tools/build_hand_cheska.py --pose=rest     the same model POSED for a review picture only (see POSES)
  py -3 tools/build_hand_cheska.py --pose=fall     ... then run the first command again before leaving

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/cheska.glb for `CheskaFrostHand`. The parts, by name:

  base                 the frost crust on the sweatband, two tones, three snow lumps
  crystal0..3          four chunky six-sided crystals, origin at the foot, each LEANING (the node carries the lean, so
                       the C# can turn a crystal about its own axis and squash it along its own length)
  crack0, crack1       dark cracks on the two big crystals (children of them), shown only when she is tagged
  spike0..5            the crown round the foot: studs at rest, spikes on a cast
  sprite               the snow-bun: body, belly, headband (her sweatband, in small), nose, blush, feet, tail
    earL, earR         crystal ears, origin at the root of each
    eyeL, eyeR         origin at the middle of each
    paw                the polishing paw, origin at its shoulder
    frozen             the block of ice it is stuck in when she is tagged
  claw0..2             the shards that grow down her forearm on a fall, origin at the foot, along +y, bent along +z
  chunk                one loose shard: the C# copies it six times for the pieces that fly off
  puff                 the bun's breath, a small cloud

The kit's nodes carry a position only, and a leaning crystal has to be squashed along its OWN length, so after the kit
has written the file this script opens it again and gives those nodes a rotation (and, for a review pose, a scale).
"""
import json
import math
import struct
import sys

import numpy as np

from hand_companion_kit import MODELS, Model, ellipsoid, join, lathe, moved, slab

# The sixteen colours, the same list as `CheskaFrostHand.Palette`.
PALETTE = [
    "#ffffff",  # 0 snow
    "#cfeaf2",  # 1 snow in shade (feet, tail, belly)
    "#5fe8d0",  # 2 her ice accent
    "#b8fff2",  # 3 her ice accent, light
    "#3b9eba",  # 4 the ice teal of `CheskaIceVisuals` (.23,.62,.73)
    "#1f6f86",  # 5 deep teal
    "#11485a",  # 6 the darkest teal: cracks
    "#46d7f0",  # 7 the cyan of her sweatband
    "#ff9aa6",  # 8 the bun's blush
    "#0d3644",  # 9 its eyes
    "#e6fbff",  # 10 frost
    "#86d6e6",  # 11 mid ice
    "#2a86a2",  # 12 a facet in shade
    "#a3ecf7",  # 13 a facet in light
    "#ffc9b0",  # 14 the inside of an ear
    "#6fb7cf",  # 15 the frost's rim
]
SNOW, SHADE, ACCENT, MINT, ICE, DEEP, CRACK, BAND, BLUSH, EYE, FROST, MID, FACET_DARK, FACET_LIGHT, EAR, RIM = range(16)


# ------------------------------------------------------------------ a faceted shape (the kit's are all smooth)

def faceted(rings, sides, slot_of, twist=0.0, squash_z=1.0, bend=0.0):
    """A cut gem: `rings` is (radius, y) from bottom to top, each ring a regular polygon of `sides` corners; radius 0
    closes an end to a point and an open end is capped flat. Every face is flat (its own corners, its own normal) and
    takes the palette slot `slot_of(band, side)`, band -1 for a cap. With six sides and no twist one face looks along
    +z. `bend` pushes the higher rings along +z by bend * (y / height) squared: a claw's curve.
    Returns the kit's list of (mesh, slot)."""
    top = max(y for _, y in rings) or 1.0

    def corner(ring, k):
        radius, y = rings[ring]
        u = twist + 2 * math.pi * k / sides
        return np.array((radius * math.cos(u), y, radius * math.sin(u) * squash_z + bend * (max(y, 0.0) / top) ** 2), np.float32)

    middle = np.array((0, (rings[0][1] + rings[-1][1]) * .5, bend * .25), np.float32)
    faces = {}

    def face(points, slot):
        a, b, c = points[0], points[1], points[2]
        n = np.cross(b - a, c - a)
        length = float(np.linalg.norm(n))
        if length < 1e-12:
            return
        n /= length
        if float(np.dot(n, (a + b + c) / 3 - middle)) < 0:
            points = points[::-1]
            n = -n
        pos, nrm, tris = faces.setdefault(slot, ([], [], []))
        base = len(pos)
        pos.extend(points)
        nrm.extend([n] * len(points))
        for i in range(1, len(points) - 1):
            tris.append((base, base + i, base + i + 1))

    for r in range(len(rings) - 1):
        for k in range(sides):
            lo0, lo1, hi0, hi1 = corner(r, k), corner(r, k + 1), corner(r + 1, k), corner(r + 1, k + 1)
            slot = slot_of(r, k)
            if rings[r][0] < 1e-7:
                face([lo0, hi0, hi1], slot)
            elif rings[r + 1][0] < 1e-7:
                face([lo0, lo1, hi0], slot)
            else:
                # A bent or tapered side is not flat: two triangles, each with its own normal.
                face([lo0, lo1, hi1], slot)
                face([lo0, hi1, hi0], slot)
    for end in (0, len(rings) - 1):
        if rings[end][0] > 1e-7:
            face([corner(end, k) for k in range(sides)], slot_of(-1, 0))
    return [((np.array(p, np.float32), np.array(n, np.float32), np.array(t, np.uint32)), slot) for slot, (p, n, t) in faces.items()]


def place(pieces, at=(0, 0, 0), turn=(0, 0, 0), scale=(1, 1, 1)):
    return [(moved(mesh, at, turn, scale), slot) for mesh, slot in pieces]


def quat(axis, degrees):
    half = math.radians(degrees) * .5
    s = math.sin(half)
    return (axis[0] * s, axis[1] * s, axis[2] * s, math.cos(half))


def qmul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz)


def lean(side, front, spin=0.0):
    """A rotation that leans +y by `side` degrees towards +x and `front` degrees towards +z, after a turn about y."""
    return qmul(qmul(quat((0, 0, 1), -side), quat((1, 0, 0), front)), quat((0, 1, 0), spin))


# ------------------------------------------------------------------ the pieces

# The six faces of a crystal as the player meets them: light towards her, dark behind. Side 1 is the face on +z.
GEM = [MID, FACET_LIGHT, ICE, FACET_DARK, DEEP, ICE]
TIP = [ACCENT, SNOW, MINT, MID, ICE, MID]


def crystal(radius, height, band=False):
    """A chunky six-sided crystal standing on its foot: a narrow root, a fat shoulder, a point, with a snow-white tip."""
    def slot(ring, side):
        if ring < 0:
            return DEEP
        return FACET_DARK if ring == 0 and GEM[side] != FACET_LIGHT else TIP[side] if ring == 2 else GEM[side]
    pieces = faceted([(radius * .66, 0), (radius, height * .2), (radius * .9, height * .68), (0, height)], 6, slot)
    if band:
        # Her sweatband again, round the biggest crystal: cyan with a white stripe.
        y0, y1 = height * .27, height * .40
        r0 = radius * 1.12
        pieces += faceted([(r0, y0), (r0, y1)], 6, lambda ring, side: BAND)
        pieces += faceted([(r0 * 1.035, y0 + (y1 - y0) * .34), (r0 * 1.035, y0 + (y1 - y0) * .66)], 6, lambda ring, side: SNOW)
    return pieces


def crack(radius, height):
    """A zigzag crack on the two opposite faces of a crystal that look along z (the C# stops it turning, face on)."""
    # The kit's slab fans from its middle, so a zigzag is a chain of small quads. It lies on the long middle face
    # (from 0.2 to 0.68 of the height), a hair proud of it: any further out and it reads as a stick, not a crack.
    pieces = []
    spine = [(.0006, .0), (-.38, .26), (.34, .42), (-.30, .68), (.10, 1.0)]
    for (x0, t0), (x1, t1) in zip(spine, spine[1:]):
        w = .0017
        x0, x1 = x0 * radius if abs(x0) > .01 else x0, x1 * radius if abs(x1) > .01 else x1
        y0, y1 = t0 * height * .44, t1 * height * .44
        pieces.append(slab([(x0 - w, y0), (x0 + w, y0), (x1 + w, y1), (x1 - w, y1)], .0010))
    one = join(*pieces)
    out = []
    for k in range(2):
        on_face = moved(one, at=(0, height * .22, radius * .866 * .965), turn=(-1.5, 0, 0))
        out.append((moved(on_face, turn=(0, 180 * k, 0)), CRACK))
    return out


def build():
    m = Model("cheska")

    # ---------------- the frost on her sweatband
    base = [
        # A drift, not a tray: a low mound with a cooler skirt showing all round it and snow heaped at its edge.
        (ellipsoid((.0400, .0050, .0300), at=(0, .0012, 0), seg=12, rings=5), RIM),
        (ellipsoid((.0365, .0078, .0268), at=(0, .0030, 0), seg=12, rings=6), FROST),
        (ellipsoid((.0090, .0062, .0080), at=(-.0200, .0066, .0200), seg=8, rings=5), SNOW),
        (ellipsoid((.0075, .0052, .0068), at=(.0225, .0060, .0215), seg=8, rings=5), SNOW),
        (ellipsoid((.0062, .0046, .0058), at=(-.0345, .0042, -.0080), seg=8, rings=5), SNOW),
    ]
    m.add("base", base)

    # ---------------- four crystals: (name, radius, height, foot, lean to +x, lean to +z)
    for name, radius, height, at, side, front, band in CRYSTALS:
        m.add(name, crystal(radius, height, band), at=at)
    m.add("crack0", crack(CRYSTALS[0][1], CRYSTALS[0][2]), parent="crystal0")
    m.add("crack1", crack(CRYSTALS[1][1], CRYSTALS[1][2]), parent="crystal1")

    # ---------------- the crown: six studs round the foot that a cast throws out as spikes
    for k, (at, side, front) in enumerate(SPIKES):
        m.add("spike%d" % k, faceted([(.0056, 0), (.0066, .0050), (0, .023)], 4,
                                     lambda ring, s: (ACCENT, MINT, ACCENT, ICE)[s] if ring > 0 else DEEP, twist=math.pi / 4), at=at)

    # ---------------- the snow-bun
    r, h = .0190, .0172                                   # its body: round as a ball seen from above, a little squat

    def girth(y):
        return r * math.sqrt(max(0.0, 1 - ((y - h) / h) ** 2))

    body = [
        (ellipsoid((r, h, r), at=(0, h, 0), seg=16, rings=9), SNOW),
        # A cool belly patch, proud of the body by a hair.
        (ellipsoid((.0112, .0086, .0040), at=(0, .0098, girth(.0098) - .0026), seg=10, rings=5), SHADE),
        # Her sweatband in small, worn as a headband: cyan with a white stripe.
        (lathe([(girth(y) + .0012, y) for y in (.0238, .0256, .0274, .0290)], seg=14), BAND),
        (lathe([(girth(y) + .0019, y) for y in (.0256, .0274)], seg=14), SNOW),
        (ellipsoid((.0017, .0012, .0010), at=(0, .0140, girth(.0140) + .0002), seg=6, rings=3), DEEP),
        (ellipsoid((.0054, .0032, .0066), at=(.0082, .0026, .0125), seg=8, rings=5), SHADE),
        (ellipsoid((.0054, .0032, .0066), at=(-.0082, .0026, .0125), seg=8, rings=5), SHADE),
        (ellipsoid((.0060, .0055, .0055), at=(0, .0080, -.0185), seg=8, rings=5), SHADE),
        # The arm that is not polishing, tucked against its side.
        (ellipsoid((.0036, .0052, .0040), at=(-.0172, .0105, .0060), seg=8, rings=5), SNOW),
    ]
    for s in (-1, 1):
        y = .0120
        z = math.sqrt(max(1e-9, girth(y) ** 2 - .0124 ** 2))
        body.append((moved(ellipsoid((.0034, .0021, .0009), seg=8, rings=4), at=(s * .0124, y, z + .0001), turn=(0, s * 42, 0)), BLUSH))
    m.add("sprite", body, at=SPRITE)

    ear = faceted([(.0040, 0), (.0058, .0055), (.0046, .0165), (0, .0235)], 4,
                  lambda ring, s: FROST if ring < 0 else (EAR if s == 0 and ring < 2 else (MINT, ACCENT, MID, ACCENT)[s] if ring < 2 else (SNOW, MINT, ACCENT, MINT)[s]),
                  twist=math.pi / 4, squash_z=.78)
    # Side 0 of a four-sided shape with this twist looks along +z: the ear's warm inside faces her.
    m.add("earL", ear, at=(-.0078, .0300, -.0010), parent="sprite")
    m.add("earR", ear, at=(.0078, .0300, -.0010), parent="sprite")

    for name, s in (("eyeL", -1), ("eyeR", 1)):
        x, y = s * .0074, .0172
        z = math.sqrt(girth(y) ** 2 - x * x)
        eye = [(ellipsoid((.0036, .0048, .0016), seg=8, rings=5), EYE),
               (ellipsoid((.0013, .0016, .0007), at=(.0010, .0018, .0012), seg=6, rings=3), SNOW)]
        m.add(name, eye, at=(x, y, z - .0002), parent="sprite")

    m.add("paw", [(ellipsoid((.0036, .0054, .0038), at=(0, -.0038, .0006), seg=8, rings=5), SNOW)], at=(.0170, .0146, .0066), parent="sprite")

    # The block it is frozen into when she is tagged: up to its nose, feet and all.
    block = faceted([(.0262, -.0006), (.0290, .0030), (.0290, .0082), (.0250, .0108)], 4,
                    lambda ring, s: FROST if ring < 0 else (FACET_LIGHT, MID, ICE, MID)[s] if ring == 1 else (MINT, FACET_LIGHT, MID, FACET_LIGHT)[s],
                    twist=math.pi / 4)
    m.add("frozen", block, parent="sprite")

    # ---------------- the shards of a fall, the loose chunk, the breath
    claw = faceted([(.0075, 0), (.0110, .011), (.0080, .032), (0, .054)], 4,
                   lambda ring, s: DEEP if ring < 0 else (FACET_LIGHT, ICE, FACET_DARK, ICE)[s] if ring < 2 else (SNOW, MINT, MID, MINT)[s],
                   twist=math.pi / 4, squash_z=.8, bend=.012)
    for k in range(3):
        m.add("claw%d" % k, claw)
    m.add("chunk", faceted([(0, -.0070), (.0056, -.0012), (.0038, .0034), (0, .0086)], 4,
                           lambda ring, s: (FACET_LIGHT, ICE, FACET_DARK, MID)[s] if ring < 2 else (SNOW, MINT, MID, MINT)[s], twist=.5))
    m.add("puff", [(ellipsoid((.0072, .0062, .0060), seg=8, rings=5), FROST),
                   (ellipsoid((.0052, .0046, .0046), at=(.0064, -.0010, .0004), seg=8, rings=5), SNOW),
                   (ellipsoid((.0046, .0042, .0042), at=(-.0062, -.0014, .0006), seg=8, rings=5), SNOW),
                   (ellipsoid((.0040, .0036, .0036), at=(.0010, .0054, .0000), seg=8, rings=5), SHADE)])
    return m


# name, radius, height, foot, degrees leaning to +x, degrees leaning to +z, banded
CRYSTALS = [
    ("crystal0", .0135, .060, (-.0120, .0045, -.0125), -7, -5, True),
    ("crystal1", .0108, .043, (.0185, .0045, -.0130), 15, -4, False),
    ("crystal2", .0082, .028, (-.0300, .0040, .0050), -27, 9, False),
    ("crystal3", .0078, .023, (.0315, .0040, .0070), 29, 10, False),
]
SPRITE = (.0030, .0085, .0075)


def _spikes():
    out = []
    for k in range(6):
        u = math.radians(18 + 60 * k)
        x, z = math.cos(u), math.sin(u)
        out.append(((x * .0360, .0030, z * .0262), 66 * x, 66 * z))
    return out


SPIKES = _spikes()

# Review poses: node -> (position or None, rotation or None, scale or None). "rest" is how she stands; the others are
# what the C# does to the same nodes, typed by hand so a picture can be taken without the game.
HIDE = (None, None, (.0001, .0001, .0001))
POSES = {
    "rest": dict({"claw%d" % k: HIDE for k in range(3)}, chunk=HIDE, puff=HIDE, frozen=HIDE, crack0=HIDE, crack1=HIDE,
                 **{"spike%d" % k: (None, None, (.85, .36, .85)) for k in range(6)}),
    "breath": dict({"claw%d" % k: HIDE for k in range(3)}, chunk=HIDE, frozen=HIDE, crack0=HIDE, crack1=HIDE,
                   puff=((.012, .026, .036), None, None),
                   **{"spike%d" % k: (None, None, (.85, .36, .85)) for k in range(6)}),
    "fall": dict(chunk=HIDE, puff=HIDE, frozen=HIDE, crack0=HIDE, crack1=HIDE,
                 sprite=((-.0170, .0520, -.0170), None, (1.12, .86, 1.12)),
                 claw0=((-.004, -.004, -.040), lean(0, -58), None), claw1=((.000, -.006, -.066), lean(0, -58), (.85, .85, .85)),
                 claw2=((.004, -.008, -.090), lean(0, -58), (.7, .7, .7)),
                 **{"spike%d" % k: (None, None, (.85, .36, .85)) for k in range(6)}),
    "tagged": dict({"claw%d" % k: HIDE for k in range(3)}, chunk=HIDE, puff=HIDE,
                   eyeL=(None, None, (1.4, 1.4, 1)), eyeR=(None, None, (1.4, 1.4, 1)),
                   **{"spike%d" % k: (None, None, (.85, .36, .85)) for k in range(6)}),
    "cast": dict({"claw%d" % k: HIDE for k in range(3)}, chunk=HIDE, puff=HIDE, frozen=HIDE, crack0=HIDE, crack1=HIDE,
                 **{"spike%d" % k: (None, None, (1.15, 1.75, 1.15)) for k in range(6)}),
}


def dress_nodes(path, pose):
    """Open the kit's .glb again and give the leaning nodes their rotation (and a review pose its changes)."""
    data = path.read_bytes()
    length = struct.unpack_from("<I", data, 12)[0]
    doc = json.loads(data[20:20 + length])
    rest = data[20 + length:]
    turns = {name: lean(side, front) for name, _, _, _, side, front, _ in CRYSTALS}
    turns.update({"spike%d" % k: lean(side, front) for k, (_, side, front) in enumerate(SPIKES)})
    turns["earL"] = lean(-16, -4)
    turns["earR"] = lean(16, -4)
    for node in doc["nodes"]:
        name = node.get("name")
        if name in turns:
            node["rotation"] = [float(v) for v in turns[name]]
        if pose and name in POSES[pose]:
            at, turn, scale = POSES[pose][name]
            if at is not None:
                node["translation"] = [float(v) for v in at]
            if turn is not None:
                node["rotation"] = [float(v) for v in turn]
            if scale is not None:
                node["scale"] = [float(v) for v in scale]
    text = json.dumps(doc, separators=(",", ":")).encode()
    text += b" " * (-len(text) % 4)
    path.write_bytes(struct.pack("<4sII", b"glTF", 2, 20 + len(text) + len(rest)) + struct.pack("<I4s", len(text), b"JSON") + text + rest)


if __name__ == "__main__":
    pose = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--pose=")), None)
    model = build()
    written = model.write(PALETTE)
    dress_nodes(written, pose)
    print("posed for a picture: %s. Build again without --pose before leaving." % pose if pose else "the game's model (nothing posed)")
    assert written == MODELS / "cheska.glb"
