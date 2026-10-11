"""Basilio's bare hands: the stone that grows on them (`DanteGauntletHands`). No creature, no face: plates of his own
cut stone, rooted on the back of his fists and forearms, that the C# side grows, slides and sheds.

  py -3 tools/build_hands_dante.py                 the plain model (what the game loads)
  py -3 tools/build_hands_dante.py --pose=rest     review only: the plates as they sit when he stands, on a stand-in arm
  py -3 tools/build_hands_dante.py --pose=fall     review only: the whole gauntlet, fists as boulders
  py -3 tools/build_hands_dante.py --pose=cast     review only: the knuckles stood up stepped like his Bastion, seams lit
  py -3 tools/build_hands_dante.py --pose=sprint   review only: the knuckle plates slid back into a ridge
  (add --right to see a pose on the right arm's stand-in: the rolled gold cuff and the cord)

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/dante_hands.glb. ALWAYS finish with the plain command: a
`--pose` build bakes one pose and a stand-in arm into the file so a review sheet can show it, and the game needs the
plain parts.

Everything below is typed in THE ARM'S OWN SPACE, as `RosterArms/dante_left` is: x across the arm, y along it from the
elbow (0) to the knuckles (0.84), z out of the arm's upper side, which is the side the player sees. It is written to the
file 4.4 times smaller (`DanteGauntletHands.Scale`), the size the kit and the review tool expect.

What is in the plain model (each a named node, its origin at its ROOT: the middle of its underside, on the arm's skin,
so the C# side grows it by scaling it out from there):
  k0 k1 k2 k3   the four knuckle plates
  w             the plate on the heel of the hand, at the wrist
  h             the wide plate over the back of the hand (only in a fall or a cast)
  s0 s1         the two cheek plates on the fist's sides (only in a fall: the fist becomes a boulder)
  f0 f1 f2 f3   the four forearm plates, wrist to elbow
  <plate>-seam  a plate's molten seam (all but s0, s1 and f3), a child of the plate, switched off until a landing or a cast
  pebble        one chunk; the C# side copies it six times for what breaks off

A plate is a rounded block with corners knocked off by flat cuts, darker in its lower half, with a smaller paler block
stepped on top of it (the stepped slabs of `GeoVfx`) and a fat gold vein that runs over its top and down the edge that
faces the player.
"""
import math
import sys

import numpy as np

import hand_companion_kit as kit
from hand_companion_kit import ellipsoid, moved, rounded, tube

# The sixteen colours, the same list as `DanteGauntletHands.Palette`.
PALETTE = [
    "#8f8577",  # 0 stone
    "#b9ad9b",  # 1 stone, light (the step on each plate)
    "#62594f",  # 2 stone, dark (the lower half of each plate)
    "#46403a",  # 3 stone, deepest (the chunk's foot)
    "#dfb248",  # 4 gold vein
    "#f6dc86",  # 5 gold, light (nuggets)
    "#ff7a1c",  # 6 molten seam
    "#ffd257",  # 7 molten seam's core
    "#3d6335",  # 8 moss (his shirt's green)
    "#3fa65c",  # 9 earth accent
    "#c9682d",  # 10 review only: his skin
    "#efe6d2",  # 11 review only: his forearm wrap
    "#d9a93a",  # 12 review only: his rolled cuff
    "#2f8f4a",  # 13 review only: his cord bracelet
    "#3d6335",  # 14 review only: his sleeve
    "#1d1a1c",  # 15 spare
]
STONE, LIGHT, DARK, DEEP, GOLD, GOLD_LIGHT, MOLTEN, CORE, MOSS, EARTH, SKIN, WRAP, CUFF, CORD, SLEEVE, SPARE = range(16)

SCALE = 4.4
S = 1.0 / SCALE
# How far a plate's underside is sunk into the arm, so no light shows under it.
SINK = .012

# The arm's upper side by height along it, measured off `RosterArms/dante_left` and `dante_right` and smoothed over
# the narrow necks (a plate is longer than a neck). The same two tables as `DanteGauntletHands.Surface`.
LEFT_SURFACE = [(0, .294), (.26, .294), (.30, .245), (.34, .258), (.37, .215), (.50, .235), (.56, .24), (.60, .263), (.78, .256), (.84, .20)]
RIGHT_SURFACE = [(0, .29), (.24, .285), (.29, .321), (.42, .321), (.47, .26), (.52, .225), (.57, .245), (.60, .263), (.78, .256), (.84, .20)]
FIST_SIDE = .222

# name: (across, along, half width, half length, thickness). The same roots as `DanteGauntletHands.RestX` and `RestY`.
PLATES = {
    "k0": (-.168, .740, .052, .058, .100),
    "k1": (-.057, .750, .056, .064, .125),
    "k2": (.056, .748, .055, .062, .115),
    "k3": (.166, .738, .050, .055, .095),
    "w": (0, .600, .150, .048, .085),
    "h": (0, .675, .190, .085, .060),
    "s0": (-FIST_SIDE, .690, .200, .090, .075),
    "s1": (FIST_SIDE, .690, .200, .090, .075),
    "f0": (0, .440, .170, .050, .080),
    "f1": (0, .330, .190, .050, .085),
    "f2": (0, .220, .210, .050, .090),
    "f3": (0, .110, .210, .050, .080),
}


def surface(y, right=False):
    table = RIGHT_SURFACE if right else LEFT_SURFACE
    for (y0, z0), (y1, z1) in zip(table[:-1], table[1:]):
        if y <= y1:
            return z0 + (z1 - z0) * max(0.0, min(1.0, (y - y0) / (y1 - y0)))
    return table[-1][1]


def cut_block(half, radius, chips=(), seg=8, rings=6, waist=-.15):
    """A rounded block (thickness on y, as the kit's shapes stand) with `chips`: (normal, share) flat cuts, each taking
    the block back to `share` of its own reach along that normal. Returns the mesh and its triangles' own ring
    directions, so a caller can colour the lower half apart. The middle ring of the ball it is made from is set at
    `waist` of the half thickness: that is where the two tones meet on the block's sides."""
    dirs, tris = kit._sphere(seg, rings)
    dirs = dirs.astype(np.float64)
    half = np.array(half, np.float64)
    core = np.maximum(half - radius, 0)
    scale = np.minimum(half, radius)
    pos = np.sign(dirs) * core + dirs * scale
    middle = np.abs(dirs[:, 1]) < 1e-6
    pos[middle, 1] = half[1] * waist
    nrm = dirs / scale
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True)
    for normal, share in chips:
        n = np.array(normal, np.float64)
        n /= np.linalg.norm(n)
        reach = (float(np.dot(np.abs(n), core)) + radius) * share
        over = pos @ n - reach
        hit = over > 0
        pos[hit] -= np.outer(over[hit], n)
        nrm[hit] = nrm[hit] * .35 + n * .65
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True)
    return (pos.astype(np.float32), nrm.astype(np.float32), tris), dirs


def two_tone(block, dirs, body, foot):
    pos, nrm, tris = block
    low = dirs[tris][:, :, 1].max(axis=1) < .1
    return [((pos, nrm, tris[~low]), body), ((pos, nrm, tris[low]), foot)]


def stand(pieces, thickness):
    """Pieces typed with the thickness on y and the knuckle end on -z, stood into the arm's space (thickness on z, the
    knuckle end on +y) with the underside sunk `SINK` into the skin."""
    return [(moved(mesh, at=(0, 0, thickness * .5 - SINK), turn=(90, 0, 0)), slot) for mesh, slot in pieces]


def plate(name, variant):
    """One plate about its root, in the arm's space, and its seam. `variant` picks its cuts, its step and its vein."""
    _, _, hx, hy, t = PLATES[name]
    top = t * .5
    r = min(.024, t * .3)
    v = variant % 4
    # Corners off the outline (the block seen from above), and one bevel off the top.
    cuts = [
        [((1, 0, -1), .88), ((-1, 0, .9), .9), ((-1, .9, -.5), .93)],
        [((-1, 0, -1), .87), ((1, 0, 1), .9), ((.8, 1, .3), .93)],
        [((1, 0, 1), .88), ((-1, 0, -.8), .9), ((-.6, 1, .6), .94)],
        [((-1, 0, 1), .87), ((1, 0, -.9), .9), ((.5, 1, -.7), .93)],
    ][v]
    block, dirs = cut_block((hx, top, hy), r, cuts)
    pieces = two_tone(block, dirs, STONE, DARK)

    # The step: a paler slab set back towards the knuckle end and off to one side.
    sx, sz = hx * .56, hy * .52
    ox, oz = hx * (.16 if v % 2 == 0 else -.18), -hy * .22
    st = max(.014, t * .2)
    step_cut = [((1, 0, 1), .86)] if v % 2 == 0 else [((-1, 0, 1), .86)]
    if name not in ("s0", "s1"):
        step, _ = cut_block((sx, st, sz), min(.012, st * .8), step_cut, seg=8, rings=3)
        pieces.append((moved(step, at=(ox, top + st * .35, oz)), LIGHT))
    else:
        sx = sz = 0
    step_top = top + st * 1.35

    # The vein: over the top beside the step, to the edge that faces the player, and down it.
    vr = max(.011, min(.015, hx * .12))
    side = -1 if v % 2 == 0 else 1
    vx = side * hx * .52
    vein = [(vx - side * hx * .1, top - vr * .2, -hy * .55), (vx + side * hx * .12, top - vr * .15, hy * .45),
            (vx - side * hx * .06, -top * .5, hy + vr * .15)]
    pieces.append((tube(vein, vr, seg=3), GOLD))

    # The molten seam: a jagged crack across the plate from side to side, over the step, proud of the stone.
    def height(x, z):
        return step_top if abs(x - ox) < sx and abs(z - oz) < sz else top

    sr = max(.008, min(.012, hy * .16))
    zig = [.3, -.25, .25]
    path = [(-hx - sr * .1, top * .1, hy * .3)]
    for k, zz in enumerate(zig):
        x = hx * (-.8 + 1.6 * k / (len(zig) - 1))
        path.append((x, height(x, hy * zz) + sr * .35, hy * zz))
    path.append((hx + sr * .1, top * .1, hy * .15))
    seam = [(tube(path, sr, seg=3), MOLTEN)] if name not in ("s0", "s1", "f3") else []
    return stand(pieces, t), stand(seam, t), (hx, hy, t, top, step_top, sr, path)


def extras(name, shape):
    """What only some plates carry: a seam's bright core, a nugget, a fleck of moss."""
    hx, hy, t, top, step_top, sr, path = shape
    pieces, seam = [], []
    if name in ("h", "w"):
        core = [(x, y + sr * .75, z) for x, y, z in path[1:-1]]
        seam.append((tube(core, sr * .45, seg=3), CORE))
    if name in ("k1", "h", "w"):
        pieces.append((ellipsoid((.016, .009, .014), at=(-hx * .45, top + .002, hy * .55), seg=5, rings=3), GOLD_LIGHT))
    if name in ("w", "f1", "f3"):
        pieces.append((ellipsoid((.03, .012, .024), at=(hx * .62, top + .001, hy * .3), seg=5, rings=3), MOSS))
        if name == "w":
            pieces.append((ellipsoid((.014, .008, .012), at=(hx * .62 + .012, top + .011, hy * .3 - .006), seg=5, rings=3), EARTH))
    return stand(pieces, t), stand(seam, t)


def small(pieces, scale=(1, 1, 1), turn=(0, 0, 0)):
    """Posed (review only), then brought down to the file's size."""
    return [(moved(moved(mesh, scale=scale, turn=turn), scale=(S, S, S)), slot) for mesh, slot in pieces]


def arm(right):
    """A stand-in for his first-person arm, for a review sheet only: the fist, the wrist, and the wrap or the cuff."""
    def box(half, at, slot, radius=.035):
        return (rounded(half, radius, at=at, seg=8, rings=6), slot)
    parts = [box((.227, .105, .263), (0, .685, 0), SKIN, .045), box((.16, .03, .195), (0, .81, 0), SKIN, .03),
             box((.165, .05, .185), (0, .53, 0), SKIN)]
    if right:
        parts += [box((.262, .13, .29), (0, .14, 0), SLEEVE), box((.299, .075, .321), (0, .36, 0), CUFF, .06),
                  box((.2, .03, .215), (0, .47, 0), SKIN), box((.185, .02, .205), (0, .522, 0), CORD, .02)]
    else:
        parts += [box((.267, .13, .294), (0, .13, 0), WRAP), box((.225, .04, .245), (0, .31, 0), WRAP),
                  box((.195, .075, .21), (0, .42, 0), SKIN)]
    return parts


# Review poses: name -> (grown, along, across, thick, long, wide). A plate left out is not grown.
def pose(which, right):
    p = {}
    rest = {n: (1, PLATES[n][1], PLATES[n][0], 1, 1, 1) for n in ("k0", "k1", "k2", "k3", "w")}
    if which == "rest":
        p = rest
    elif which == "fall":
        for n, (x, y, _, _, _) in PLATES.items():
            p[n] = (1, y, x, 1, 1, 1)
        for n in ("k0", "k1", "k2", "k3"):
            p[n] = (1, PLATES[n][1] + .02, PLATES[n][0], 1.45, 1.25, 1.06)
        p["h"] = (1, PLATES["h"][1], 0, 1.3, 1, 1)
    elif which == "cast":
        p = dict(rest)
        for k, (n, lng, tall) in enumerate((("k0", 1.5, 1.3), ("k1", 2.3, 1.9), ("k2", 1.9, 1.6), ("k3", 1.35, 1.2))):
            p[n] = (1, PLATES[n][1] + (lng - 1) * .05, PLATES[n][0], tall, lng, 1)
        p["w"] = (1, PLATES["w"][1], 0, 1.4, 1, 1)
        p["h"] = (1, PLATES["h"][1], 0, 1.3, 1, 1)
    elif which == "sprint":
        p = {"w": rest["w"]}
        for k, n in enumerate(("k0", "k1", "k2", "k3")):
            p[n] = (1, .50 - .09 * k, 0, 1.15, 1.5, 1.7)
    return p


def build(which=None, right=False):
    m = kit.Model("dante_hands")
    posed = pose(which, right) if which else None
    for index, name in enumerate(PLATES):
        body, seam, shape = plate(name, index)
        more_body, more_seam = extras(name, shape)
        body, seam = body + more_body, seam + more_seam
        x, y, _, _, _ = PLATES[name]
        cheek = name in ("s0", "s1")
        if posed is None:
            # The plain model: every plate whole, at its own root on the left arm. The C# side places them all.
            at = (x, y, 0) if cheek else (x, y, surface(y))
            m.add(name, small(body), at=tuple(v * S for v in at))
            if seam:
                m.add(name + "-seam", small(seam), parent=name)
            continue
        if name not in posed:
            continue
        grown, py, px, thick, lng, wide = posed[name]
        scale = (wide * grown, lng * grown, thick * grown)
        turn = (0, 90 if x > 0 else -90, 0) if cheek else (0, 0, 0)
        at = (px, py, 0) if cheek else (px, py, surface(py, right))
        pieces = body + (seam if which in ("cast", "land") else [])
        m.add(name, small(pieces, scale, turn), at=tuple(v * S for v in at))

    # One chunk of the same stone, for what cracks off.
    block, dirs = cut_block((.034, .026, .03), .013, [((1, 1, .3), .84), ((-1, .6, -.5), .86), ((-.6, -1, .6), .88), ((.5, -.3, -1), .87)])
    chunk = two_tone(block, dirs, STONE, DEEP)
    chunk.append((ellipsoid((.011, .008, .009), at=(-.012, .012, .022), seg=5, rings=3), GOLD))
    m.add("pebble", small(chunk), at=((.34 * S, .5 * S, .1 * S) if posed else (0, 0, 0)))

    if posed is not None:
        m.add("review-arm", small(arm(right)))
    m.write(PALETTE)


if __name__ == "__main__":
    build(next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--pose=")), None), "--right" in sys.argv)
