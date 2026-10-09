"""Rago's hand companion: a pilot flame that lives in a charcoal brazier cup on the gold crest of his left bracer.

  py -3 tools/build_hand_sean.py

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/sean.glb. `SeanEmberHand.cs` finds every part by name and
poses it about its own origin, so a part's origin is where it pivots.

THE THING. A fat teardrop of flame in three stacked tones (deep orange behind, orange-yellow in the middle, a pale
core in front, each a whole flame shape of its own, staggered front to back like cut layers), with a face on the
core: two dark eyes with a light in them, a small mouth, two pink cheeks. Above the body the flame goes on as two
chunky licks (a chain: `lick1` then `lick2`), which is what bends, trails and stretches. Two side tongues flick.
It sits in a brazier cup of charcoal with a gold rim, gold studs, a red gem and a gold saddle plate, on a bed of
coals; three of those coals are loose cubes (`ember-a/b/c`) that hop out of the cup when it flares.
Snuffed, the flame is gone and a sulking lump of coal (`coal`) sits in the cup, its eyes two shut ember slits; one
(`coal-eye`) can open. A thin smoke curl (`curl`) stands on it. `puff` is one smoke puff, copied into the ring.

Parts and pivots (metres, y up, +z is the face):
  cup                      the brazier, origin at its foot
  ember-a, ember-b, ember-c  loose coals in the cup, origin at their own middle
  flame                    the body, origin at its base in the cup's mouth
    tongue-l, tongue-r     side tongues, origin at their roots
    lick1 > lick2          the flame above the body, each with its origin at its own base
    white                  the white heat of a wind-up, origin at its base (scale x and y only: it stays behind the face)
    eye-l, eye-r, mouth    origin at their own middles
  coal                     the snuffed lump, origin at its base
    coal-eye               the one eye that opens, origin at its middle
    curl                   the smoke curl, origin at its foot
  puff                     one smoke puff, origin at its middle
"""
import math
import sys

import numpy as np

from hand_companion_kit import Model, ellipsoid, join, mirror, moved, rounded, tube

# The sixteen colours (the same list, in the same order, is `SeanEmberHand.Palette`).
CHAR, CHAR_LIT, FIRE, FLAME, CORE, WHITE, EYE, GOLD, GOLD_DARK, RED, ACCENT, BLUSH, SMOKE, SMOKE_LIT, GLOW, SMOKE_DARK = range(16)
PALETTE = ["#2a2326", "#4a3b3c", "#ff5c08", "#ffa812", "#ffe98a", "#fffbe6", "#2b1410", "#f2b632",
           "#b87a14", "#b3201f", "#ff3355", "#ff8fa3", "#8d8a92", "#c4c1c8", "#ff7a1a", "#5c5961"]


# ------------------------------------------------------------------ shapes the kit does not have

def drop(radius, height, seg=12, rows=8, squash_z=1.0, belly=.62, curl=0.0, tip=.0):
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


def torus(big, wide, tall, y=0.0, seg=16, around=8):
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


def bowl(profile, seg=14):
    """A cup turned about y from (radius, y) points, up the outside, over the lip and down the inside to the floor."""
    prof = [(0.0, profile[0][1])] + list(profile) + [(0.0, profile[-1][1])]
    pts, nrm, tris = [], [], []
    for r, (rad, y) in enumerate(prof):
        lo, hi = prof[max(0, r - 1)], prof[min(len(prof) - 1, r + 1)]
        dr, dy = hi[0] - lo[0], hi[1] - lo[1]
        length = math.hypot(dr, dy) or 1.0
        out, up = dy / length, -dr / length
        for s in range(seg + 1):
            u = 2 * math.pi * (s % seg) / seg
            pts.append((rad * math.cos(u), y, rad * math.sin(u)))
            nrm.append((out * math.cos(u), up, out * math.sin(u)))
    for r in range(len(prof) - 1):
        for s in range(seg):
            a = r * (seg + 1) + s
            b = a + seg + 1
            if prof[r + 1][0] > 1e-6:
                tris.append((a, b, b + 1))
            if prof[r][0] > 1e-6:
                tris.append((a, b + 1, a + 1))
    nrm = np.array(nrm, np.float32)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-6)
    return np.array(pts, np.float32), nrm, np.array(tris, np.uint32)


def around(mesh, count, radius, y, start=0.0):
    """`mesh` (made facing +z at the origin) set `count` times round a circle, each turned to face out."""
    out = []
    for k in range(count):
        a = start + 360.0 * k / count
        out.append(moved(mesh, (radius * math.sin(math.radians(a)), y, radius * math.cos(math.radians(a))), (0, a, 0)))
    return out


# ------------------------------------------------------------------ the model

def build(review=None):
    """`review` is only for looking: "lit" leaves out what the lit flame never shows (the coal, the white heat, the
    puff), "snuffed" leaves out the flame. The game's model is built with neither and holds every part."""
    m = Model("sean")
    if review:
        skip = ("coal", "coal-eye", "curl", "white", "puff") if review == "lit" else             ("flame", "tongue-l", "tongue-r", "lick1", "lick2", "white", "eye-l", "eye-r", "mouth")
        real = m.add
        m.add = lambda name, *a, **k: None if name in skip else real(name, *a, **k)

    # ---------------- THE BRAZIER CUP: charcoal, gold lipped, studded, on a gold saddle that sits on the bracer.
    cup = bowl([(.019, .004), (.027, .008), (.0315, .016), (.032, .0245), (.0265, .0245), (.0245, .017)], 12)
    lip = torus(.0305, .0042, .0036, .0250, 12, 5)
    saddle = rounded((.030, .0032, .024), .0030, (0, .0020, 0), 8, 4)
    saddle_ends = [rounded((.0075, .0026, .0100), .0025, (s * .0335, .0016, 0), 4, 4) for s in (-1, 1)]
    studs = around(ellipsoid((.0040, .0040, .0024), (0, 0, 0), 6, 3), 6, .0312, .0150, 30)
    gem = ellipsoid((.0058, .0062, .0030), (0, .0150, .0318), 8, 4)
    gem_set = ellipsoid((.0076, .0080, .0020), (0, .0150, .0306), 8, 3)
    # The cup's own glow: ember cracks up its sides, in the orange of `SeanCinderVisual`'s live seam.
    crack = moved(ellipsoid((.0018, .0062, .0014), (0, 0, 0), 4, 3), (0, 0, 0), (0, 0, 22))
    cracks = around(crack, 2, .0300, .0125, 62) + around(moved(crack, turn=(0, 0, -50), scale=(1, .7, 1)), 2, .0293, .0105, 118)
    bed = ellipsoid((.0250, .0060, .0250), (0, .0185, 0), 8, 3)
    m.add("cup", [(cup, CHAR), (lip, GOLD), (saddle, GOLD), (bed, CHAR_LIT)]
          + [(e, RED) for e in saddle_ends] + [(s, GOLD) for s in studs[1:]] + [(gem_set, GOLD_DARK), (gem, ACCENT)]
          + [(c, GLOW) for c in cracks])

    # ---------------- three loose coals that hop: small solid cubes, as `EmberDrift`'s are.
    for name, at, slot in (("ember-a", (.015, .0240, .012), GLOW), ("ember-b", (-.016, .0240, -.004), FLAME), ("ember-c", (.003, .0245, -.016), GLOW)):
        m.add(name, rounded((.0042, .0042, .0042), .0012, (0, 0, 0), 4, 4), slot, at=at)

    # ---------------- THE FLAME'S BODY: three whole flames, back to front.
    outer = moved(drop(.0345, .0660, 12, 7, .52, .58), (0, 0, -.0060))
    back_tongue = moved(drop(.0150, .0400, 6, 4, .7, .6, -.25), (.006, .018, -.0200), (-24, 0, 0))
    mid = moved(drop(.0282, .0580, 12, 6, .52, .58), (0, .0035, .0040))
    core = moved(drop(.0192, .0430, 10, 6, .58, .62), (0, .0090, .0112))
    cheeks = [ellipsoid((.0042, .0027, .0016), (s * .0140, .0218, .0200), 6, 3) for s in (-1, 1)]
    m.add("flame", [(outer, FIRE), (back_tongue, FIRE), (mid, FLAME), (core, CORE)] + [(c, BLUSH) for c in cheeks], at=(0, .0215, 0))

    # Side tongues: each a fat lick with a lighter inside, rooted low on the body's flank.
    tongue = join(drop(.0125, .0380, 6, 4, .55, .55, .30))
    tongue_in = moved(drop(.0074, .0240, 4, 3, .5, .55, .30), (.0006, .0050, .0052))
    m.add("tongue-r", [(tongue, FIRE), (tongue_in, FLAME)], at=(.0265, .0130, -.0030), parent="flame")
    m.add("tongue-l", [(mirror(tongue), FIRE), (mirror(tongue_in), FLAME)], at=(-.0265, .0130, -.0030), parent="flame")

    # The licks above the body: the chain that bends, trails and stretches.
    lick1 = moved(drop(.0215, .0420, 8, 5, .55, .55, .12), (0, -.0090, 0))
    lick1_in = moved(drop(.0130, .0300, 6, 4, .5, .55, .12), (0, -.0070, .0092))
    m.add("lick1", [(lick1, FIRE), (lick1_in, FLAME)], at=(0, .0500, -.0050), parent="flame")
    lick2 = moved(drop(.0130, .0320, 6, 4, .6, .55, -.38), (0, -.0070, 0))
    lick2_in = moved(drop(.0066, .0170, 4, 3, .55, .55, -.38), (0, -.0030, .0062))
    m.add("lick2", [(lick2, FIRE), (lick2_in, FLAME)], at=(.0030, .0250, 0), parent="lick1")

    # The white heat: flat, just proud of the core and behind the face, so it can swell without eating the eyes.
    m.add("white", drop(.0150, .0330, 8, 4, .30, .60), WHITE, at=(0, .0100, .0190), parent="flame")

    # The face.
    eye = ellipsoid((.0052, .0070, .0034), (0, 0, 0), 8, 4)
    for name, s in (("eye-l", -1), ("eye-r", 1)):
        glint = ellipsoid((.0019, .0024, .0014), (-.0014, .0026, .0026), 6, 3)
        spark = ellipsoid((.0010, .0011, .0010), (.0018, -.0026, .0028), 4, 3)
        m.add(name, [(eye, EYE), (glint, WHITE), (spark, WHITE)], at=(s * .0088, .0290, .0222), parent="flame")
    m.add("mouth", [(ellipsoid((.0038, .0025, .0022), (0, 0, 0), 8, 4), EYE), (ellipsoid((.0021, .0011, .0012), (0, -.0010, .0014), 6, 3), ACCENT)],
          at=(0, .0192, .0236), parent="flame")

    # ---------------- SNUFFED: a sulking coal with two shut ember slits, a frown, glowing cracks and a chipped corner.
    lump = rounded((.0245, .0165, .0215), .0125, (0, .0165, 0), 8, 6)
    chip = rounded((.0100, .0078, .0090), .0060, (.0150, .0290, -.0060), 8, 4)
    slit = lambda s: moved(ellipsoid((.0056, .0013, .0016), (0, 0, 0), 6, 3), (s * .0100, .0215, .0208), (0, 0, -s * 16))
    frown = tube([(-.0050, .0128, .0212), (-.0020, .0150, .0218), (.0020, .0150, .0218), (.0050, .0128, .0212)], .0012, 4)
    seams = [moved(ellipsoid((.0017, .0062, .0014), (0, 0, 0), 4, 3), (x, y, z), (0, 0, t))
             for x, y, z, t in ((-.0190, .0120, .0150, 30), (.0215, .0140, .0100, -38), (.0040, .0320, .0040, 80), (-.0100, .0300, -.0080, 62))]
    ash = ellipsoid((.0090, .0030, .0080), (-.0080, .0322, .0030), 6, 3)
    m.add("coal", [(lump, CHAR), (chip, CHAR_LIT), (slit(-1), GLOW), (slit(1), GLOW), (frown, GLOW), (ash, SMOKE_DARK)] + [(s, GLOW) for s in seams],
          at=(0, .0215, 0))
    m.add("coal-eye", [(ellipsoid((.0058, .0064, .0024), (0, 0, 0), 8, 4), CORE), (ellipsoid((.0026, .0034, .0016), (.0008, -.0006, .0016), 6, 3), EYE)],
          at=(-.0100, .0220, .0212), parent="coal")
    curl_path = [(0, 0, 0), (.0040, .0080, 0), (.0010, .0160, 0), (-.0050, .0230, 0), (-.0030, .0310, 0), (.0040, .0370, 0), (.0070, .0440, 0)]
    curl = tube(curl_path, [.0046, .0043, .0040, .0036, .0032, .0027, .0022], 4)
    curl_end = ellipsoid((.0056, .0050, .0046), (.0050, .0480, 0), 6, 3)
    m.add("curl", [(curl, SMOKE_LIT), (curl_end, SMOKE)], at=(.0050, .0330, 0), parent="coal")

    # ---------------- one smoke puff: three lumps in three greys. Six copies make the ring a landing knocks out.
    puff = [(ellipsoid((.0090, .0080, .0085), (0, 0, 0), 8, 4), SMOKE), (ellipsoid((.0062, .0058, .0060), (.0060, .0046, .0020), 6, 3), SMOKE_LIT),
            (ellipsoid((.0056, .0050, .0054), (-.0062, -.0026, .0010), 6, 3), SMOKE_DARK)]
    m.add("puff", puff, at=(.0450, .0600, 0) if review else (0, .0300, 0))
    return m


if __name__ == "__main__":
    look = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--review=")), None)
    build(look).write(PALETTE)
