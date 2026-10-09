"""Zack's (ISAGANI's) bare first-person hands, second idea: MAGNET HANDS. His pocket junk clings to his knuckles.

  py -3 tools/build_hands_zack_magnet.py               the game's model: the ten pieces, laid out apart
  py -3 tools/build_hands_zack_magnet.py --pose=rest   a review model: the pieces on a stand-in left forearm, as
                                                       `ZackMagnetHands` seats them (rest, fall, sprint, crowd, chain)

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/zack_magnet.glb. `ZackMagnetHands.cs` finds every piece by
name and places it every frame, so where a piece stands in the plain file is only a layout for the review sheet.
A review pose is NOT the game's model: rebuild without `--pose` last.

The owner chose this from a list after the lit bands and arcs (`build_hands_zack.py`): "Magnet hands. His hands are
magnetised, from his Magnet Charge skill. Bottle caps, coins, nuts and bolts cling to his knuckles and bands". So what
is here is a street tinkerer's pocket: two crown bottle caps (tansan), an old peso, a scalloped five-sentimo, a hex
nut, a wing nut, a short fat bolt, a washer, a paper clip and one small key. They are objects, not creatures: no faces.

THE WHOLE POCKET IS YELLOW. Of a first palette (steel, copper, a red and a blue cap, a silver coin) the owner said, with
Zack in front of him: "it doesnt fit his design.. he's more yellowish so orient it around that". So it is gold, brass
and yellow enamel, shaded in deep ochre and warm brown, with one cap and the wing nut in neon as the CHARGED ones, and
ONE dark accent, the navy of his jacket's trim, for holes, emblems and the clip. No red, no blue, no grey.

Every piece is modelled LYING FLAT about its own middle: thin along z, its face to +z, and long along y where it has a
long way (the bolt, the clip, the key, the wing nut's wings). The code lays a piece flat on the skin, or tips it up
about x to stand on its edge or its end. `SIZES` below is each piece's half-length along y and half-thickness along z
in the arm's units; `ZackMagnetHands` carries the same numbers.

Modelled at 1/4.4 of the arm's size and a quarter over life size (`BIG`), so they read on a small fist.
"""
import math
import sys

import numpy as np

from hand_companion_kit import Model, ellipsoid, lathe, mirror, moved, rounded, slab, tube

PALETTE = [
    "#e8f53a",  # 0  neon: the two CHARGED pieces (a cap and the wing nut), and his sparks (drawn by the code)
    "#f6ffa0",  # 1  pale neon: a charged piece's highlight
    "#fff8cf",  # 2  white-yellow: highlights
    "#f0c230",  # 3  gold
    "#8a6412",  # 4  deep ochre: his jacket's shade, and every piece's shade tone
    "#d9a531",  # 5  brass
    "#a87a16",  # 6  dark brass
    "#f4d21f",  # 7  yellow enamel: a bottle cap's top
    "#5e3d12",  # 8  warm brown: the deepest shade
    "#c9951c",  # 9  ochre: his jacket
    "#1c2340",  # 10 navy: the ONE dark accent (his jacket's trim): holes, emblems, the clip
    "#f7de6a",  # 11 pale gold
    "#a3b31a",  # 12 neon's shade
    "#f6ecc0",  # 13 cream: a cap's cork liner
    "#c8713c",  # 14 his skin (the review's stand-in arm only)
    "#e2b81c",  # 15 his sleeve (the review's stand-in arm only)
]
NEON, PALE, HILITE, GOLD, OCHRE_DEEP, BRASS, BRASS_DARK, ENAMEL, BROWN, OCHRE, NAVY, GOLD_PALE, NEON_SHADE, CREAM, SKIN, SLEEVE = range(16)

S = 1 / 4.4                  # the arm's own units to model metres
CX, FIST_Y, FACE_Z = .034, .745, .256

NAMES = ["cap-yellow", "cap-neon", "peso", "sentimo", "hex-nut", "wing-nut", "bolt", "washer", "clip", "key"]


# ------------------------------------------------------------------ shapes the kit does not have

def revolve(strips, seg=16, wave=None):
    """Strips of (radius, z[, waved]) turned about z. Each strip is smooth in itself and hard-edged against the next
    (a coin's face must stay flat to its rim). Run every strip counter-clockwise in the (radius, z) plane: outward along
    an underside, up an outer wall, inward along a top, down the wall of a hole. A point with a third value has its
    radius multiplied by `wave(angle)`: a crown cap's flutes, a scalloped coin's lobes."""
    pos, nrm, tris = [], [], []
    for strip in strips:
        base = len(pos)
        rows = len(strip)
        for point in strip:
            r, z = point[0], point[1]
            for s in range(seg):
                u = 2 * math.pi * s / seg
                rr = r * (wave(u) if (wave and len(point) > 2) else 1.0)
                pos.append((rr * math.cos(u), rr * math.sin(u), z))
        local = np.zeros((rows * seg, 3), np.float64)
        p = np.array(pos[base:], np.float64)
        for i in range(rows - 1):
            for s in range(seg):
                a, c = i * seg + s, i * seg + (s + 1) % seg
                b, d = a + seg, c + seg
                for tri in ((a, c, b), (b, c, d)):
                    n = np.cross(p[tri[1]] - p[tri[0]], p[tri[2]] - p[tri[0]])
                    if np.linalg.norm(n) < 1e-14:
                        continue
                    tris.append(tuple(base + t for t in tri))
                    for t in tri:
                        local[t] += n
        length = np.linalg.norm(local, axis=1, keepdims=True)
        local = np.where(length > 1e-14, local / np.maximum(length, 1e-14), np.array([0, 0, 1.0]))
        nrm.extend(local.tolist())
    return np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32)


def face(radius, z, up=True, waved=False):
    """A flat round face at height z, looking up (+z) or down."""
    edge = (radius, z, 1) if waved else (radius, z)
    return [edge, (0, z)] if up else [(0, z), edge]


def ccw(outline):
    o = list(outline)
    area = sum(o[i][0] * o[(i + 1) % len(o)][1] - o[(i + 1) % len(o)][0] * o[i][1] for i in range(len(o)))
    return o if area > 0 else o[::-1]


def hexagon(radius, flat_up=True):
    return [(radius * math.cos(math.radians(60 * k + (30 if flat_up else 0))), radius * math.sin(math.radians(60 * k + (30 if flat_up else 0)))) for k in range(6)]


def star(outer, inner, depth, points=5):
    ring = []
    for k in range(points * 2):
        a = math.pi * k / points
        r = outer if k % 2 == 0 else inner
        ring.append((r * math.sin(-a), r * math.cos(a)))
    return slab(ccw(ring), depth)


# ------------------------------------------------------------------ the pieces (each about its own middle, lying flat)

def crown_cap(enamel, shade, emblem):
    """A tansan: the fluted crown cap of a soft-drink bottle. Enamel top, a darker fluted skirt, a cream cork liner
    underneath, and a navy emblem on top (a star on the yellow one, a bar on the charged neon one)."""
    r0, r1, h = .0112, .0146, .0033
    flutes = lambda u: 1 + .10 * math.cos(12 * u)
    seg = 24
    top = revolve([[(r0, h * .45), (r0 * .93, h), (0, h)]], seg)
    skirt = revolve([[(r1, -h, 1), (r1 * .93, -h * .25, 1), (r0, h * .45)]], seg, flutes)
    lip = revolve([[(r1 * .78, -h * .55), (r1, -h, 1)]], seg, flutes)
    liner = revolve([face(r1 * .78, -h * .55, up=False)], seg)
    pieces = [(top, enamel), (skirt, shade), (lip, BROWN), (liner, CREAM)]
    if emblem == "star":
        pieces.append((moved(star(.0068, .0030, .0006), at=(0, 0, h + .0002)), NAVY))
    else:
        pieces.append((moved(slab(ccw([(-.0078, -.0022), (.0078, -.0022), (.0078, .0022), (-.0078, .0022)]), .0006), at=(0, 0, h + .0002)), NAVY))
        pieces.append((revolve([face(.0030, h + .0007)], 8), PALE))
    return pieces


def peso():
    """An old peso, in gold: a raised rim, a deep ochre field, a head in relief on one side and a star on the other."""
    r, t = .0138, .0013
    seg = 18
    body = revolve([face(r, -t, up=False), [(r, -t), (r, t)], face(r, t)], seg)
    field_up = revolve([face(r * .80, t + .0002)], seg)
    field_down = revolve([face(r * .80, -t - .0002, up=False)], seg)
    head = ellipsoid((.0046, .0058, .0010), at=(-.0006, -.0004, t + .0003), seg=8, rings=4)
    return [(body, GOLD), (field_up, OCHRE_DEEP), (field_down, OCHRE_DEEP), (head, GOLD_PALE),
            (moved(star(.0064, .0028, .0005), at=(0, 0, -t - .0004), turn=(180, 0, 0)), GOLD_PALE)]


def sentimo():
    """An old brass five-sentimo: small, with a scalloped edge of eight lobes."""
    r, t = .0112, .0012
    lobes = lambda u: 1 + .075 * math.cos(8 * u)
    seg = 32
    body = revolve([face(r, -t, up=False, waved=True), [(r, -t, 1), (r, t, 1)], face(r, t, waved=True)], seg, lobes)
    return [(body, BRASS),
            (revolve([face(r * .66, t + .0002)], 12), BRASS_DARK),
            (revolve([face(r * .66, -t - .0002, up=False)], 12), BRASS_DARK),
            (revolve([face(r * .26, t + .0004)], 8), HILITE),
            (revolve([face(r * .26, -t - .0004, up=False)], 8), HILITE)]


def hex_nut():
    """A hex nut: a dark brass hexagon, a pale gold washer face and a navy threaded hole on both sides."""
    d = .0070
    pieces = [(slab(hexagon(.0128), d), BRASS_DARK)]
    for up in (True, False):
        z = d * .5
        pieces.append((revolve([face(.0092, (z + .0002) if up else -(z + .0002), up=up)], 12), GOLD_PALE))
        pieces.append((revolve([face(.0058, (z + .0004) if up else -(z + .0004), up=up)], 12), NAVY))
    return pieces


def wing_nut():
    """A wing nut on its side: a fat boss with a collar, and two ears that rise and spread from it."""
    ear = ccw([(.0030, -.0030), (.0122, .0030), (.0150, .0100), (.0112, .0138), (.0062, .0098), (.0022, .0026)])
    wing = slab(ear, .0038)
    rim = slab(ccw([(.0118, .0086), (.0138, .0100), (.0112, .0124), (.0092, .0102)]), .0046)
    return [(rounded((.0068, .0052, .0056), .0024, at=(0, -.0052, 0), seg=10, rings=6), NEON),
            (rounded((.0080, .0019, .0064), .0016, at=(0, -.0096, 0), seg=10, rings=4), NEON_SHADE),
            (wing, NEON), (mirror(wing), NEON),
            (rim, NEON_SHADE), (mirror(rim), NEON_SHADE),
            (revolve([face(.0034, .0058)], 8), NAVY), (revolve([face(.0034, -.0058, up=False)], 8), NAVY)]


def bolt():
    """A short fat bolt on its side: a gold hex head, a deep ochre shank and three raised turns of thread in pale gold
    (dark with bright turns, so it still reads when a sprint drags it onto his yellow sleeve)."""
    head = lathe([(.0078, .0052), (.0092, .0064), (.0092, .0108), (.0078, .0120)], seg=6)
    shank = lathe([(.0048, -.0120), (.0056, -.0108), (.0056, .0054)], seg=10)
    pieces = [(head, GOLD), (shank, OCHRE_DEEP)]
    for y in (-.0082, -.0040, .0002):
        pieces.append((lathe([(.0056, y - .0013), (.0068, y - .0005), (.0068, y + .0005), (.0056, y + .0013)], seg=10), GOLD_PALE))
    pieces.append((rounded((.0050, .0008, .0050), .0008, at=(0, .0121, 0), seg=8, rings=4), BROWN))
    return pieces


def washer():
    """A flat washer: a pale gold ring with a real hole, its inner wall brown, and a navy pressed groove."""
    ro, ri, t = .0126, .0054, .0011
    seg = 16
    ring = revolve([[(ri, -t), (ro, -t)], [(ro, -t), (ro, t)], [(ro, t), (ri, t)]], seg)
    hole = revolve([[(ri, t), (ri, -t)]], seg)
    groove_up = revolve([[(ro * .80, t + .0002), (ro * .66, t + .0002)]], seg)
    groove_down = revolve([[(ro * .66, -t - .0002), (ro * .80, -t - .0002)]], seg)
    return [(ring, GOLD_PALE), (hole, BROWN), (groove_up, NAVY), (groove_down, NAVY)]


def arc(cx, cy, r, a0, a1, steps=4):
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * k / steps)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * k / steps)), 0.0) for k in range(steps + 1)]


def clip():
    """A paper clip: one fat wire bent into the two loops, in navy (the dark piece that reads on any yellow)."""
    path = [(-.0016, .0076, 0)]
    path += arc(.0007, -.0086, .0023, 180, 360)
    path += arc(-.0005, .0108, .0035, 0, 180)
    path += arc(.0003, -.0110, .0043, 180, 360)
    path += [(.0046, .0064, 0)]
    return [(tube(path, .00155, seg=5), NAVY)]


def key():
    """One small gold key: a round bow with a real hole, a brown collar, and a blade with two teeth."""
    ro, ri, t = .0074, .0030, .0014
    bow = moved(revolve([[(ri, -t), (ro, -t)], [(ro, -t), (ro, t)], [(ro, t), (ri, t)], [(ri, t), (ri, -t)]], 12), at=(0, .0078, 0))
    blade = slab(ccw([(-.0021, .0016), (-.0021, -.0128), (0, -.0150), (.0021, -.0128), (.0040, -.0108), (.0021, -.0088),
                      (.0040, -.0066), (.0021, -.0044), (.0021, .0016)]), .0020)
    return [(bow, GOLD), (blade, GOLD), (rounded((.0036, .0014, .0018), .0010, at=(0, .0010, 0), seg=8, rings=4), BROWN)]


BIG = 1.25                   # a quarter over life size: at life size they were specks on a fist the size of a thumbnail


def pieces():
    parts = [crown_cap(ENAMEL, OCHRE_DEEP, "star"), crown_cap(NEON, NEON_SHADE, "bar"), peso(), sentimo(), hex_nut(), wing_nut(),
             bolt(), washer(), clip(), key()]
    return [[(moved(mesh, scale=(BIG, BIG, BIG)), slot) for mesh, slot in part] for part in parts]


def extents(parts):
    """Each piece's half-length along y and half-thickness along z, in the ARM's units (`ZackMagnetHands.Rad`, `.Thin`)."""
    out = []
    for part in parts:
        pos = np.concatenate([m[0] for m, _ in part])
        out.append((float(np.abs(pos[:, 1]).max()) / S, float(np.abs(pos[:, 2]).max()) / S))
    return out


def build_plain():
    m = Model("zack_magnet")
    for k, (name, part) in enumerate(zip(NAMES, pieces())):
        m.add(name, part, at=(-.072 + .036 * (k % 5), .020 - .040 * (k // 5), 0))
    return m


# ------------------------------------------------------------------ review poses on a stand-in left forearm

# The seats on a hand, as `ZackMagnetHands` has them: x from the arm's middle line, y up the arm, the turn in the
# face's plane, whether the piece stands on its edge, and how far it is raised (a piece stacked on another).
SLOTS = [(-.125, .770, 20, 0, 0), (.030, .800, -15, 1, 0), (.145, .725, 40, 0, 0), (-.050, .640, -30, 0, 0), (.100, .585, 75, 0, 0),
         (-.150, .600, 0, 1, 0), (.000, .730, 10, 0, .050), (-.110, .690, 50, 0, .050), (.095, .680, -40, 1, .045), (.000, .520, 90, 0, 0)]


def face_at(y):
    """The arm's +z face by height: the fist and the bare forearm, then the band, then the thicker sleeve."""
    u = min(1.0, max(0.0, (y - .44) / .14))
    return .325 + (FACE_Z - .325) * u


def seated(part, size, x, y, yaw, tilt, raise_by=0.0):
    rad, thin = size
    h = thin * abs(math.cos(math.radians(tilt))) + rad * abs(math.sin(math.radians(tilt)))
    at = ((CX + x) * S, y * S, (face_at(y) + h + raise_by) * S)
    return [(moved(mesh, at=at, turn=(tilt, 0, yaw)), slot) for mesh, slot in part]


def build_pose(pose):
    m = Model("zack_magnet")
    skin, jacket, cuff = SKIN, SLEEVE, OCHRE_DEEP
    arm = [(rounded((.30 * S, .15 * S, .29 * S), .03, at=(CX * S, .26 * S, 0), seg=12, rings=6), jacket),
           (rounded((.33 * S, .04 * S, .31 * S), .008, at=(CX * S, .445 * S, 0), seg=12, rings=4), cuff),
           (rounded((.215 * S, .04 * S, .27 * S), .008, at=(CX * S, .52 * S, 0), seg=12, rings=4), NEON_SHADE),
           (rounded((.16 * S, .06 * S, .20 * S), .012, at=(CX * S, .60 * S, 0), seg=12, rings=4), skin),
           (rounded((.2125 * S, .095 * S, FACE_Z * S), .014, at=(CX * S, FIST_Y * S, 0), seg=12, rings=6), skin)]
    m.add("stand-in-arm", arm)
    parts, sizes = pieces(), extents(pieces())
    left = [0, 2, 4, 8, 6] if pose != "crowd" else [0, 2, 4, 8, 6, 1, 3, 5, 7, 9]
    for slot, i in enumerate(left):
        x, y, yaw, stand, up = SLOTS[slot]
        tilt = 90.0 * stand
        if pose == "fall":
            x, y, tilt, up = x * .7, FIST_Y + (y - FIST_Y) * .7, 90.0, up + .09
            yaw += 47 * slot
        if pose == "sprint":
            x, y, tilt = .07 * math.sin(slot * 2.3), .70 - .10 * slot, 63.0
        if pose == "chain" and slot == 3:
            tilt = 90.0
        if pose == "chain" and slot == 4:
            x, y, yaw, tilt, up = SLOTS[3][0], SLOTS[3][1], SLOTS[3][2] + 90, 90.0, 2 * sizes[left[3]][0]
        m.add(NAMES[i], seated(parts[i], sizes[i], x, y, yaw, tilt, up))
    return m


if __name__ == "__main__":
    posed = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--pose=")), None)
    (build_pose(posed) if posed else build_plain()).write(PALETTE)
    for name, (rad, thin) in zip(NAMES, extents(pieces())):
        print("  %-9s rad %.3f thin %.3f" % (name, rad, thin))
