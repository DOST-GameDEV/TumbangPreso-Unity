"""The LONGHORN, second design (2026-10-29), for Basilio's CONTINENTAL DRIFT cutscene brought back over ASTRA's armour. The owner:
"we're keeping the shields and body armor but we're applying the old long horn cutscene we had. maybe improve the design on
that tho", then "basically we're merging the armor thing that astra brought + the longhorn cutscene".

  py -3 tools/build_ultimate_props_dante_titan.py [--out=folder]
  py -3 tools/build_ultimate_props_dante_titan.py --out=<folder> --small=0.2        (review only)

WHAT THE FIRST ONE WAS (`tools/build_ultimate_props_dante.py`, `longhorn.glb`, kept: the old storyboard tool still reads it), AND
WHY IT IS REDRAWN. It was built to carry him up out of a molten sea, so it was a 6 m wide drum of a back with a crown he stood on:
 * from the front and the side it read as a turtle's shell or a platform, not a beast; its head sat low on the court (eyes at
   1.3 m) under all that mass, so the face, the thing a player reads first, was the smallest part of it;
 * ten horns and claws stood out of its shoulders, its elbow and its head, so no one of them was the longhorn's horns;
 * it had one arm, the right (the only joint), while he swings two boulder fists;
 * its underside was a flat brown cap (the owner, of a look from below: "underside ... is invisible").
THIS ONE is a bust of a bull made of the mountain, standing up out of the court behind him. Same stone, same painted atlas
(`longhorn-atlas.png`, the first builder's), same drawn face, same ink; by the cast's rule (`docs/Art_Direction.md` section 0) it
is chunky, round-cornered and has fewer, bigger parts:
 * a tall rounded body 4.1 m across with one broad chest slab and a molten V on it; a skirt that runs on under the court, so
   whatever rises out of the court is never open at the bottom;
 * the head is the biggest single shape, high (eyes at 4.1 m) and sat on the chest, 3.2 m across: from the side its muzzle is
   only 0.7 m proud of the chest, so it no longer hangs in front of the body;
 * ONE PAIR OF HORNS, the longhorn's: 9.2 m tip to tip, out and up, tips 5.9 m high. Every other spike is gone but two low fins down its spine;
 * TWO ARMS, each three joints (`arm-l` the shoulder, its child `forearm-l` the elbow, its child `fist-l` the wrist, and the same
   for `-r`): it brings both down with his two, bending at the elbow on the wind-up;
 * PARTS FOR SECONDARY MOTION (film l1, owner: "the longhorn honestly looks so rigid and stiff"): `chest` (breathes), `shoulder-l`
   and `shoulder-r` (the boulders shudder a beat late), `horn-l` and `horn-r` under the head (the tips lag a toss).

THE NUMBERS THE STAGE READS (`HeroIntroductionScene.DanteLonghorn.cs`), typed in the beast's own space (+x its right, +z its
front, y = 0 the court when it has fully risen) and flipped across x for the file, because glTFast negates x on import:
 * `arm-l` and `arm-r` hinge at (-/+2.05, 2.55, 0.55); the elbow 1.5 m below, the wrist 1.35 m below that, the middle of the fist
   0.55 m below the wrist (3.4 m in all, straight down -y); `chest` hinges at (0, 1.2, 1.0); the shoulders at (-/+1.95, 2.55, 0.4);
 * `head` hinges at the neck (0, 3.10, 0.90); its eyes are at (+/-0.72, 4.11, 2.10) in the beast's space; `jaw` (its child)
   at (0, 3.03, 1.28);
 * the top of its horns is y = 5.9, so it is wholly under the court when stood 6.3 m down.
"""
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, rounded  # noqa: E402
from build_paete_bloom import arg  # noqa: E402
import build_ultimate_props_dante as first  # noqa: E402
from build_ultimate_props_dante import (PALETTE, SEAM, SEAM_HOT, CRUST, EYE, EYE_HOT, MOUTH, R_ROCK, R_PLATE, R_HORN,  # noqa: E402
                                        R_BROW, R_MUZZLE, R_ARM, R_KNUCKLE, stone, paint, limb, cord, mir, off, finish)

ARM_AT = (2.05, 2.55, .55)
ARM_LENGTH = 3.4
# The head is typed at its first size and then GROWN about its middle and moved (`head_grow`). Turnaround t1: it sat between the
# shoulders and, from the side, hung in front of the body with air under its chin; t2 (1.15, 0.2 m up, 0.35 m back) still read
# small against the body in the first film and still stood out in front of the torso from the side. t3: 1.5 times, its middle
# brought back 0.85 m so it sits ON the chest and nape rather than in front of them, and raised 0.45 m so it crowns the outline.
# The horns grow with it: 9.2 m tip to tip, as thick at the root as his whole body is wide.
HEAD_MIDDLE, HEAD_SCALE, HEAD_SHIFT = (0.0, 3.40, 1.75), 1.70, (0.0, .60, -1.05)
HEAD_AT = (0.0, 3.10, .90)


def grown(point):
    return tuple(c + (v - c) * HEAD_SCALE + d for v, c, d in zip(point, HEAD_MIDDLE, HEAD_SHIFT))


JAW_AT = grown((0.0, 2.85, 2.0))


def head_grow(piece):
    """A head piece typed at the first size, grown and moved as the head is (see `HEAD_MIDDLE`)."""
    (pos, nrm, tris), paint_or_slot = piece
    c, d = np.array(HEAD_MIDDLE, np.float32), np.array(HEAD_SHIFT, np.float32)
    return ((((pos - c) * HEAD_SCALE + c + d).astype(np.float32), nrm, tris), paint_or_slot)


ELBOW_DROP, WRIST_DROP, FIST_DROP = 1.50, 1.35, .55   # shoulder to elbow, elbow to wrist, wrist to the middle of the fist (3.4 m)


def arm(side):
    """One arm in the beast's space as THREE parts, typed for the LEFT (x < 0) and mirrored for the right. Owner on film l1: "the
    longhorn honestly looks so rigid and stiff". One arm was one mesh, so it could only swing like a gate; now the upper arm, the
    forearm (hinged at the elbow) and the fist (hinged at the wrist) are each a node, so the stage can bend the elbow on the
    wind-up, let the forearm lag the shoulder and the fist lag the forearm, and squeeze the fist. Each hangs straight down."""
    x = -ARM_AT[0]
    elbow_y = ARM_AT[1] - ELBOW_DROP
    wrist_y = elbow_y - WRIST_DROP
    fist_y = wrist_y - FIST_DROP
    upper = [stone((.60, .56, .60), .34, [((-1, 1, .4), .88), ((-1, -.3, -1), .9)], R_PLATE, seg=16, rings=10, at=(x, ARM_AT[1], ARM_AT[2])),
             limb([(x, ARM_AT[1] - .1, .55), (x, elbow_y + .2, .57), (x, elbow_y - .05, .58)], [.56, .50, .47], R_ARM, seg=14)]
    fore = [stone((.50, .42, .50), .3, [((-1, 1, .3), .9)], R_PLATE, seg=14, rings=8, at=(x, elbow_y, .57)),
            limb([(x, elbow_y, .58), (x, wrist_y + .4, .56), (x, wrist_y - .05, .55)], [.47, .46, .54], R_ARM, seg=14),
            stone((.62, .36, .62), .26, [((-1, 1, .3), .9)], R_PLATE, seg=16, rings=10, at=(x, wrist_y + .55, .57))]
    # THE FIST, CLENCHED (film maplh6: one rounded block with three pale knuckle balls in a row read as "a box on wheels"). The back
    # of the hand, then four fingers rolled under it, each two blocks (the knuckle row across the front, the middle joints under
    # it), and the thumb laid across the fingers from the inside: a fist from any side, by its finger masses. Same size as before.
    inner = 1.0                                  # +x is the inside (towards the body) of the left fist; mirrored for the right
    fist = [stone((.70, .50, .58), .32, [((-1, 1, -.4), .9), ((0, 1, -1), .9)], R_ROCK, seg=18, rings=10, at=(x, fist_y + .12, .42))]
    for i, dx in enumerate((-.47, -.16, .16, .47)):
        tall = .27 if i in (1, 2) else .24
        fist.append(stone((.17, tall, .22), .12, [((0, 1, 1), .92)], R_PLATE, seg=12, rings=8, at=(x + dx, fist_y + .02, 1.0)))
        fist.append(stone((.16, .17, .22), .11, [((0, -1, 1), .92)], R_ROCK, seg=12, rings=8, at=(x + dx, fist_y - .38, .82)))
    fist.append(stone((.32, .16, .19), .12, [((0, 1, 1), .9)], R_PLATE, seg=12, rings=8, at=(x + inner * .22, fist_y - .22, 1.18), turn=(0, 0, 8 * inner)))
    fist.append(stone((.2, .26, .24), .14, [], R_ROCK, seg=12, rings=8, at=(x + inner * .58, fist_y + .08, .78)))
    # The molten seam where the fingers fold under the back of the hand, and one crack down the back.
    fist += [cord([(x - .66, fist_y + .3, .78), (x, fist_y + .36, .92), (x + .66, fist_y + .3, .78)], .06, SEAM),
             cord([(x - .1, fist_y + .55, .62), (x + .08, fist_y + .3, .82), (x - .05, fist_y + .1, .86)], .045, SEAM_HOT, seg=6)]
    if side > 0:
        upper, fore, fist = [mir(p) for p in upper], [mir(p) for p in fore], [mir(p) for p in fist]
    return upper, fore, fist


def main():
    # The atlas is the first builder's, written beside this model byte for byte (`first.paint_atlas` is deterministic).
    m = Model("longhorn-titan")
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/UltimateProps"))
    m.texture = "longhorn-atlas.png"
    m.folder.mkdir(parents=True, exist_ok=True)
    first.paint_atlas(m.folder / m.texture)

    # ---------------------------------------------------------------- the body
    body = []
    # One rounded mound, taller than it is wide, its skirt carried 1.6 m under the court.
    body.append(paint(lathe([(2.05, -1.6), (2.05, -.2), (1.98, .7), (1.80, 1.6), (1.45, 2.4), (1.05, 2.85), (.80, 3.0)], seg=28, squash_z=.78),
                      first.R_BODY, 28, "axis", at=(0, 0, -.2)))
    # The chest: one broad flat slab, and the nape plate over it that the head rises from.
    # (a part of its own, `chest`, so it can breathe: see below)
    plate = [stone((1.15, .95, .45), .4, [((-1, 1, .3), .9), ((1, 1, .3), .9), ((0, -1, .4), .93)], R_PLATE, seg=20, rings=10,
                   at=(0, 1.95, 1.05), turn=(-12, 0, 0)),
             stone((.90, .50, .60), .3, [((0, 1, 1), .9)], R_PLATE, seg=16, rings=10, at=(0, 2.75, .80), turn=(20, 0, 0))]
    # Its molten V, from each shoulder down to the middle of the chest: the widest seam on it, so it reads from across the court.
    chest = [cord([(-1.05, 2.62, 1.30), (-.66, 2.05, 1.52), (-.20, 1.50, 1.55)], [.08, .11, .07], SEAM),
             cord([(-.98, 2.50, 1.38), (-.66, 2.05, 1.60), (-.30, 1.62, 1.62)], .05, SEAM_HOT, seg=6)]
    # One plate down each flank, a seam beside it.
    flank = [stone((.28, .85, .65), .24, [((-1, 1, .5), .88)], R_PLATE, seg=14, rings=10, at=(-1.86, 1.15, -.55), turn=(0, -14, -12)),
             cord([(-1.98, .25, .32), (-1.94, 1.05, .30), (-1.70, 1.85, .22)], .075, SEAM)]
    # The shoulders: a boulder each and a paler cap. No claws (the first one had four here; they took the horns' place).
    shoulder = [stone((.85, .80, .85), .5, [((-1, 1, .3), .86), ((-1, .2, 1), .9), ((-.4, 1, -1), .88), ((-1, -.5, -.6), .92)], R_ROCK,
                      at=(-1.95, 2.55, .40), turn=(0, -14, 5)),
                stone((.75, .26, .80), .24, [((-1, 1, .5), .86), ((1, 1, -.4), .9)], R_PLATE, seg=16, rings=10, at=(-2.0, 3.32, .42), turn=(0, -14, 8))]
    body += flank + [mir(p) for p in flank]
    # Two low fins down the spine, for the outline from the side.
    body.append(stone((.24, .48, .46), .2, [((0, 1, -1), .86)], R_PLATE, seg=12, rings=8, at=(0, 2.95, -1.15), turn=(-32, 0, 0)))
    body.append(stone((.24, .44, .44), .2, [((0, 1, -1), .86)], R_PLATE, seg=12, rings=8, at=(0, 2.15, -1.70), turn=(-50, 0, 0)))
    m.add("body", body)
    # THE PARTS THAT MOVE ON THEIR OWN (film l1, owner: "rigid and stiff"): the chest plate and its molten V breathe (`chest`,
    # hinged at the bottom of the plate); each shoulder boulder is a node (`shoulder-l`, `shoulder-r`) so it can shudder after a
    # heave and a blow, a beat behind the body.
    chest_at = (0.0, 1.20, 1.0)
    m.add("chest", [off(p, chest_at) for p in plate + chest + [mir(p) for p in chest]], at=chest_at)
    for name, side in (("shoulder-l", -1), ("shoulder-r", 1)):
        at = (side * 1.95, 2.55, .40)
        pieces = shoulder if side < 0 else [mir(p) for p in shoulder]
        m.add(name, [off(p, at) for p in pieces], at=at)

    # ---------------------------------------------------------------- both arms: shoulder, elbow, wrist
    for side, tag in ((-1, "l"), (1, "r")):
        at = (side * ARM_AT[0], ARM_AT[1], ARM_AT[2])
        elbow = (at[0], at[1] - ELBOW_DROP, at[2])
        wrist = (at[0], elbow[1] - WRIST_DROP, at[2])
        upper, fore, fist = arm(side)
        m.add("arm-" + tag, [off(p, at) for p in upper], at=at)
        m.add("forearm-" + tag, [off(p, elbow) for p in fore], at=(0.0, -ELBOW_DROP, 0.0), parent="arm-" + tag)
        m.add("fist-" + tag, [off(p, wrist) for p in fist], at=(0.0, -WRIST_DROP, 0.0), parent="forearm-" + tag)

    # ---------------------------------------------------------------- the head: the biggest single shape, high and forward
    head = [stone((1.05, .82, .88), .5, [((-1, 1, .2), .88), ((1, 1, .2), .88), ((0, 1, -1), .9), ((-1, -.6, .4), .93), ((1, -.6, .4), .93), ((0, .2, 1), .97)],
                  R_ROCK, seg=24, rings=12, at=(0, 3.40, 1.75)),
            stone((.78, .17, .55), .14, [((0, 1, 1), .9)], R_PLATE, seg=14, rings=8, at=(0, 4.20, 1.62)),
            paint(rounded((.72, .38, .34), .24, seg=16, rings=10), R_MUZZLE, 16, "axis", at=(0, 3.07, 2.62))]
    face = [(moved(ellipsoid((.36, .21, .07), seg=12, rings=6), at=(-.48, 3.57, 2.55), turn=(0, 0, -10)), CRUST),
            (moved(ellipsoid((.27, .12, .06), seg=12, rings=6), at=(-.48, 3.56, 2.60), turn=(0, 0, -10)), EYE),
            (moved(ellipsoid((.11, .065, .05), seg=8, rings=5), at=(-.42, 3.55, 2.64), turn=(0, 0, -10)), EYE_HOT),
            limb([(-.06, 3.77, 2.66), (-.50, 3.93, 2.68), (-.95, 4.03, 2.48), (-1.12, 3.99, 2.20)], [.17, .22, .18, .09], R_BROW, seg=10),
            (ellipsoid((.11, .075, .05), at=(-.30, 3.21, 2.95), seg=8, rings=5), CRUST),
            cord([(-1.08, 3.58, 2.10), (-1.14, 3.28, 2.06), (-1.06, 3.02, 2.14)], .05, SEAM, seg=6)]
    # THE HORNS: out level from the temples, then up. One pair and nothing competing with it: 10.4 m tip to tip once grown. Each
    # is a node of its own under the head (`horn-l`, `horn-r`), rooted at the temple, so the stage can let the tips lag a toss.
    horn = limb([(-.88, 3.78, 1.62), (-1.55, 3.86, 1.70), (-2.25, 3.98, 1.78), (-2.85, 4.30, 1.72), (-3.05, 4.75, 1.62)], [.38, .32, .24, .14, .05],
                R_HORN, seg=14)
    head += face + [mir(p) for p in face]
    head.append((moved(ellipsoid((.60, .05, .40), seg=10, rings=5), at=(0, 2.82, 2.48)), MOUTH))
    head = [head_grow(p) for p in head]
    # Its neck, so the head grows out of the body rather than hanging in front of it.
    head.append(stone((.86, .72, .78), .4, [((0, 1, 1), .9), ((-1, 1, 0), .92), ((1, 1, 0), .92)], R_ROCK, seg=18, rings=10, at=(0, 3.10, .95)))
    m.add("head", [off(p, HEAD_AT) for p in head], at=HEAD_AT)

    # The lower jaw: an underbite, two blunt tusks.
    jaw = [stone((.78, .22, .58), .2, [((0, -1, 1), .9), ((-1, -1, 0), .9), ((1, -1, 0), .9)], R_PLATE, seg=16, rings=8, at=(0, 2.62, 2.50)),
           (moved(ellipsoid((.58, .04, .40), seg=10, rings=5), at=(0, 2.82, 2.50)), MOUTH)]
    tusk = lathe([(.16, 0), (.15, .22), (.09, .44), (0, .56)], seg=10)
    jaw.append(paint(tusk, R_HORN, 10, "axis", at=(-.54, 2.74, 2.92), turn=(8, 0, 6)))
    jaw.append(paint(tusk, R_HORN, 10, "axis", at=(.54, 2.74, 2.92), turn=(8, 0, -6)))
    jaw = [head_grow(p) for p in jaw]
    m.add("jaw", [off(p, JAW_AT) for p in jaw], at=tuple(a - b for a, b in zip(JAW_AT, HEAD_AT)), parent="head")
    root = grown((-.88, 3.78, 1.62))
    for name, piece, at in (("horn-l", head_grow(horn), root), ("horn-r", head_grow(mir(horn)), (-root[0], root[1], root[2]))):
        m.add(name, [off(piece, at)], at=tuple(a - b for a, b in zip(at, HEAD_AT)), parent="head")

    finish(m)


if __name__ == "__main__":
    main()
