"""Rago's bare hands: his bracers are burners. A banked flame grows off a gold crest plate on each bracer.

  py -3 tools/build_hands_sean.py                      the game's model (every part, unposed)
  py -3 tools/build_hands_sean.py --review=rest        only for looking: see `REVIEW`

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/sean_hands.glb. `SeanFlameHands.cs` spawns it twice (one a
bracer), finds every part by name and poses it about its own origin, so a part's origin is where it is ROOTED.

NO CREATURE. Owner, 2026-10-06: "we were trying to reserve the pet idea only for nemu... so i need something for the
bare hands." Nothing here has a face or stands apart from him: a crest plate that beds flat on the bracer's own face,
and fire that grows out of the burner in the middle of it and lies along his forearm toward the fist.

THE MODEL'S SPACE is the bracer's: the origin is ON the bracer's surface, +y is out of that surface, +z runs along
his forearm toward the fist, x is across the arm. One unit of his arm (`AU`) is a third of a metre here, and the
class scales the model by 3, so the numbers below read in the arm's own units (the bracer is 0.66 across).

A TONGUE is a teardrop standing on its round end along its own +y, flattened in z, in three stacked tones. The class
lays it down by turning it about x (`lean`: 0 stands straight out of the bracer, 90 lies flat toward the fist), so
the side of a tongue the player sees is its own -z: the lighter layers are stacked toward -z.

Parts and where each is rooted:
  plate            the crest plate and its burner ring: never moves
  glow             the ember in the burner's well (always there: a banked fire is never quite out)
  hot              the same ember gone pale and big, swapped in when the right crest heats on a wind-up
  main             the big tongue, rooted in the burner
    tip            the lick that goes on above it, rooted on its upper third: the part that arrives late
    white        the pale core a wind-up grows inside it
  side-l, side-r   two smaller tongues at the burner's flanks, which trade heights in the two flicker poses
  comet            the one long tongue a fall draws off the fist, rooted on the knuckles
  coal             the charcoal smoulder that covers the burner when a landing or a tag puts the fire out
  curl             a thin smoke curl standing in the burner
  puff, ember      one smoke puff and one ember cube: the class copies these into its pool of six
"""
import math
import sys

import numpy as np

from hand_companion_kit import Model, ellipsoid, join, mirror, moved, rounded, tube

# The sixteen colours (the same list, in the same order, is `SeanFlameHands.Palette`).
CHAR, CHAR_LIT, FIRE, FLAME, CORE, WHITE, DEEP, GOLD, GOLD_DARK, RED, RED_DARK, SKIN, SMOKE, SMOKE_LIT, GLOW, SMOKE_DARK = range(16)
PALETTE = ["#2a2326", "#4a3b3c", "#ff5c08", "#ffa812", "#ffe98a", "#fffbe6", "#d93a0b", "#f2b632",
           "#b87a14", "#b3201f", "#7d1518", "#c8794a", "#8d8a92", "#c4c1c8", "#ff7a1a", "#5c5961"]

AU = 1.0 / 3.0


def au(*v):
    return tuple(x * AU for x in v) if len(v) > 1 else v[0] * AU


# ------------------------------------------------------------------ shapes the kit does not have

def drop(radius, height, seg=10, rows=6, squash_z=1.0, belly=.62, curl=0.0, tip=.0):
    """A flame tongue: a teardrop standing on its round end, base at y 0, point at y `height`. `belly` below 1 pulls
    the fat part down; `curl` leans the point over in x (a share of the height), `tip` blunts the point."""
    pts, nrm, tris = [], [], []
    prof = []
    for r in range(rows + 1):
        t = r / rows
        rad = radius * math.sin(math.pi * t ** belly) ** .85 if 0 < r < rows else 0.0
        if r == rows - 1:
            rad = max(rad, radius * tip)
        prof.append((rad, height * t))
    for r, (rad, y) in enumerate(prof):
        lo, hi = prof[max(0, r - 1)], prof[min(rows, r + 1)]
        dr, dy = hi[0] - lo[0], hi[1] - lo[1]
        length = math.hypot(dr, dy) or 1.0
        out, up = dy / length, -dr / length
        lean = curl * height * (y / height) ** 2.2
        for s in range(seg + 1):
            u = 2 * math.pi * (s % seg) / seg
            pts.append((rad * math.cos(u) + lean, y, rad * math.sin(u) * squash_z))
            nrm.append((out * math.cos(u), up, out * math.sin(u) / squash_z) if 0 < r < rows else (0, -1 if r == 0 else 1, 0))
    for r in range(rows):
        for s in range(seg):
            a = r * (seg + 1) + s
            b = a + seg + 1
            if r < rows - 1:
                tris.append((a, b, b + 1))
            if r > 0:
                tris.append((a, b + 1, a + 1))
    nrm = np.array(nrm, np.float32)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-6)
    return np.array(pts, np.float32), nrm, np.array(tris, np.uint32)


def torus(big, wide, tall, y=0.0, seg=12, around=5):
    """A ring about y: `big` is its radius, `wide` and `tall` the half-sizes of its section."""
    pts, nrm, tris = [], [], []
    for s in range(seg + 1):
        u = 2 * math.pi * (s % seg) / seg
        for k in range(around + 1):
            v = 2 * math.pi * (k % around) / around
            r = big + wide * math.cos(v)
            pts.append((r * math.cos(u), y + tall * math.sin(v), r * math.sin(u)))
            n = np.array((math.cos(v) / wide * math.cos(u), math.sin(v) / tall, math.cos(v) / wide * math.sin(u)))
            nrm.append(n / np.linalg.norm(n))
    for s in range(seg):
        for k in range(around):
            a = s * (around + 1) + k
            b = a + around + 1
            tris.append((a, a + 1, b)); tris.append((a + 1, b + 1, b))
    return np.array(pts, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32)


def tongue(radius, height, tones, seg=10, rows=6, curl=0.0, squash=.62, step=.26):
    """A tongue in stacked tones: each tone a whole smaller teardrop set a little up its root and toward -z (the side
    the player sees), like cut layers. Sizes in the arm's units."""
    shrink = (1.0, .74, .46)
    out = []
    for k, slot in enumerate(tones):
        f = shrink[k]
        piece = drop(au(radius * f), au(height * (f * .9 + .1 * (k == 0))), max(6, seg - 2 * k), max(4, rows - k - (k > 0)), squash, .58, curl)
        out.append((moved(piece, au(0, height * .045 * k, -radius * step * k)), slot))
    return out


# ------------------------------------------------------------------ the parts

def parts():
    """Every part as (name, pieces, origin, parent), sizes already in the model's metres."""
    out = []

    def add(name, pieces, at=(0, 0, 0), parent=None):
        out.append((name, pieces, at, parent))

    # ---------------- THE CREST PLATE: gold, bedded flat on the bracer's far rim, its burner ring at the origin.
    # Styled as his bracer is: a dark gold bed under a bright gold face, red lugs at the sides, studs at the corners,
    # and behind the burner (toward the elbow) his crest, a raised red diamond in a gold setting.
    bed = rounded(au(.185, .016, .135), au(.016), au(0, .002, -.045), 8, 4)
    face = rounded(au(.160, .014, .112), au(.014), au(0, .016, -.045), 8, 4)
    lugs = [rounded(au(.030, .014, .050), au(.012), au(s * .192, .004, -.045), 4, 4) for s in (-1, 1)]
    studs = [ellipsoid(au(.017, .011, .017), au(sx * .128, .030, -.045 + sz * .082), 4, 2) for sx in (-1, 1) for sz in (-1, 1)]
    ring = torus(au(.078), au(.019), au(.017), au(.036), 10, 4)
    well = ellipsoid(au(.070, .012, .070), au(0, .030, 0), 10, 3)
    setting = moved(rounded(au(.040, .010, .040), au(.008), (0, 0, 0), 4, 4), au(0, .030, -.118), (0, 45, 0))
    gem = moved(rounded(au(.025, .011, .025), au(.008), (0, 0, 0), 4, 4), au(0, .037, -.118), (0, 45, 0))
    add("plate", [(bed, GOLD_DARK), (face, GOLD), (ring, GOLD_DARK), (well, CHAR)] + [(l, RED) for l in lugs] + [(s, GOLD_DARK) for s in studs]
        + [(setting, GOLD_DARK), (gem, RED)])

    # The ember in the well: it is what is left when the right bracer banks down for the slipper.
    add("glow", [(ellipsoid(au(.056, .013, .056), (0, 0, 0), 8, 3), GLOW), (ellipsoid(au(.030, .009, .030), au(0, .008, 0), 6, 3), FLAME)],
        at=au(0, .036, 0))
    add("hot", [(ellipsoid(au(.074, .018, .074), (0, 0, 0), 8, 3), CORE), (ellipsoid(au(.042, .012, .042), au(0, .010, 0), 6, 3), WHITE)],
        at=au(0, .038, 0))

    # ---------------- THE FIRE: one big tongue, a lick above it, two flank tongues. Deep orange under orange under pale.
    add("main", tongue(.120, .340, (FIRE, FLAME, CORE), 8, 6), at=au(0, .030, 0))
    add("tip", tongue(.062, .170, (FIRE, FLAME), 6, 4, curl=.22), at=au(.008, .205, 0), parent="main")
    add("white", [(drop(au(.050), au(.150), 6, 4, .40, .58), WHITE)], at=au(0, .040, -.086), parent="main")
    side = tongue(.074, .205, (DEEP, FLAME), 6, 5, curl=.26)
    add("side-r", side, at=au(.078, .030, -.004))
    add("side-l", [(mirror(m), s) for m, s in side], at=au(-.078, .030, -.004))

    # The fall's comet, rooted on the knuckles: longer and narrower than the bracer's tongue.
    add("comet", tongue(.105, .380, (FIRE, FLAME, CORE), 8, 5), at=au(0, -.045, .300))

    # ---------------- OUT: a charcoal smoulder over the burner, cracked with live ember, a chip of ash on it.
    lump = rounded(au(.086, .040, .080), au(.034), au(0, .034, 0), 8, 4)
    chip = rounded(au(.040, .026, .036), au(.020), au(.048, .058, -.020), 6, 4)
    seams = [moved(ellipsoid(au(.008, .026, .007), (0, 0, 0), 4, 3), au(x, y, z), (tx, 0, tz))
             for x, y, z, tx, tz in ((-.050, .060, -.030, 60, 30), (.010, .074, .010, 80, -50), (-.020, .050, -.074, 20, 70))]
    ash = ellipsoid(au(.036, .010, .030), au(-.026, .072, .020), 6, 3)
    add("coal", [(lump, CHAR), (chip, CHAR_LIT), (ash, SMOKE_DARK)] + [(s, GLOW) for s in seams], at=au(0, .026, 0))

    path = [(0, 0, 0), (.024, .050, 0), (-.022, .115, 0), (-.012, .180, 0), (.034, .245, 0)]
    curl = tube([au(*p) for p in path], [au(r) for r in (.026, .024, .021, .018, .014)], 4)
    curl_end = ellipsoid(au(.032, .028, .027), au(.028, .270, 0), 6, 3)
    add("curl", [(curl, SMOKE_LIT), (curl_end, SMOKE)], at=au(0, .050, 0))

    # ---------------- the pool's two sources: a smoke puff in three greys, an ember cube with a hot face.
    puff = [(ellipsoid(au(.044, .040, .042), (0, 0, 0), 6, 4), SMOKE), (ellipsoid(au(.030, .028, .029), au(.030, .022, -.010), 6, 3), SMOKE_LIT),
            (ellipsoid(au(.027, .024, .026), au(-.030, -.012, -.006), 6, 3), SMOKE_DARK)]
    add("puff", puff, at=au(0, .060, 0))
    ember = [(rounded(au(.024, .024, .024), au(.007), (0, 0, 0), 4, 4), GLOW), (rounded(au(.014, .014, .006), au(.004), au(0, 0, -.021), 4, 4), CORE)]
    add("ember", ember, at=au(0, .060, 0))
    return out


# ------------------------------------------------------------------ only for looking

# A pose is part -> (lean, yaw, (sx, sy, sz)); a part left out is not drawn. These are the class's own targets for a
# state, typed again here, so a sheet shows what the game is asked to show.
REVIEW = {
    "rest": {"plate": 0, "glow": 0, "main": (66, 0, (.86, .86, .86)), "tip": (10, 0, (1, 1, 1)),
             "side-l": (58, -30, (.80, .92, .80)), "side-r": (58, 30, (.80, .70, .80))},
    "fall": {"plate": 0, "glow": 0, "main": (80, 0, (.92, 1.42, .92)), "tip": (4, 0, (.85, 1.5, .85)),
             "side-l": (76, -14, (.8, 1.25, .8)), "side-r": (76, 14, (.8, 1.25, .8)), "comet": (38, 0, (1, 1.25, 1))},
    "sprint": {"plate": 0, "glow": 0, "main": (86, 0, (.74, 1.5, .6)), "tip": (2, 0, (.8, 1.5, .8)),
               "side-l": (84, -10, (.7, 1.35, .6)), "side-r": (84, 10, (.7, 1.35, .6))},
    "walk": {"plate": 0, "glow": 0, "main": (40, 0, (.86, .9, .86)), "tip": (-14, 0, (1, 1, 1)),
             "side-l": (36, -30, (.8, .9, .8)), "side-r": (36, 30, (.8, .72, .8))},
    "charge": {"plate": 0, "glow": 0, "main": (58, 0, (1.0, 1.45, 1.0)), "tip": (6, 0, (1, 1.2, 1)), "white": (0, 0, (1.2, 1.2, 1)),
               "side-l": (54, -24, (.9, 1.1, .9)), "side-r": (54, 24, (.9, 1.1, .9))},
    "ember": {"plate": 0, "hot": (0, 0, (1.1, 1.1, 1.1))},
    "out": {"plate": 0, "coal": 0, "curl": (38, 0, (1, 1, 1)), "puff": 0},
}


def arm_for_review():
    """His first-person forearm as `RosterArms/sean_left` measures (a box 0.66 by 0.59, the bracer from 0.15 to 0.54
    along it, the fist from 0.61 to 0.84), under the seat at 0.47 along. Only ever in a review sheet."""
    def block(lo, hi, hx, hy, slot, r=.02):
        return (rounded(au(hx, hy, (hi - lo) * .5), au(r), au(0, -.310, (lo + hi) * .5 - .47), 8, 4), slot)
    return [block(.15, .54, .332, .293, RED), block(.15, .22, .349, .310, GOLD), block(.46, .54, .346, .310, GOLD),
            block(.29, .37, .337, .302, GLOW), block(.50, .63, .205, .200, SKIN), block(.61, .84, .258, .252, SKIN, .06)]


def build(review=None, eye=None):
    m = Model("sean_hands")
    made = parts()
    if not review:
        for name, pieces, at, parent in made:
            m.add(name, pieces, at=at, parent=parent)
        return m
    pose = REVIEW[review]
    by_name = {name: (pieces, at, parent) for name, pieces, at, parent in made}

    def placed(name, mesh):
        pieces, at, parent = by_name[name]
        p = pose.get(name) or (0, 0, (1, 1, 1))
        mesh = moved(mesh, at, (p[0], p[1], 0), p[2])
        return placed(parent, mesh) if parent else mesh

    flat = [(m_, s) for m_, s in arm_for_review()]
    for name, (pieces, at, parent) in by_name.items():
        if name in pose:
            flat += [(placed(name, mesh), slot) for mesh, slot in pieces]
    if eye is not None:
        # Turned so the sheet's FRONT view is the player's: from behind the elbow and `eye` degrees above the arm.
        flat = [(moved(moved(mesh, turn=(0, 180, 0)), turn=(eye, 0, 0)), slot) for mesh, slot in flat]
    m.add("review", flat)
    return m


if __name__ == "__main__":
    look = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--review=")), None)
    eye = next((float(a.split("=", 1)[1]) for a in sys.argv if a.startswith("--eye=")), None)
    build(look, eye).write(PALETTE)
