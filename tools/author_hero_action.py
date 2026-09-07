"""Author a hero's cast animations onto the rigs that can play them.

    blender --background --python tools/author_hero_action.py -- --hero sean FILE...

WHY THESE CLIPS ARE BEING AUTHORED AT ALL, WHICH IS NOT "THERE WAS NOTHING THERE":
`Runtime/Visual/HeroAbilityClips.cs` already builds all fifteen procedurally, and its
timing section is good work. ⚠️⚠️ **BUT `docs/TODO_Archive.md` § 80.8 RECORDS, STILL
OPEN, THAT IT USES THE SAME PLAYER-ONLY API THAT MADE THE DANCE A T-POSE IN EVERY
BUILD.** `AnimationClip.SetCurve` fills a non-legacy clip in the EDITOR and returns a
valid, empty clip in a player, and a valid empty clip is the bind pose. § 80.6 fixed
that for the dance by baking it as an asset; the eighteen hero casts were left because
baking them meant 342 curve assets across 18 model-specific hierarchies. **A clip
authored into the `.glb` needs no baking and no lookup**: it ships as a sub-asset, the
roster already serialises it, and `CharacterAnimator.BuildGeneratedClips` only
registers a procedural clip `if (!_clips.ContainsKey(kvp.Key))`, so an authored one
wins on name with no code change at all.

⚠️ THE TIMINGS ARE THE PROCEDURAL ONES AND ARE DELIBERATELY NOT REDESIGNED. They are
derived from the abilities: Flame Rush is 0.55 s against a 0.6 s `Duration`, Ignition
Cannon is 0.45 s because its effect happens later and only the chambering is visible,
and Supernova is 1.00 s against a 0.4 s `UltimateWindup`, a 0.55 s `_airTimer` and a
0.85 s `_impactTimeout`. Changing them would be retuning gameplay to suit an animation,
which `ASTRA.md` forbids in as many words. **What is authored here is the POSE.**

⚠️⚠️ THE IMPACT MODEL IS COPIED FROM `ClipBuilder.PunchAt` BECAUSE IT IS RIGHT, and
because a baked sampler has to reproduce it rather than inherit it. That header's
argument: every one of these is a STRIKE, and a strike is defined by the moment it
stops. Smooth tangents everywhere give sinusoidal drift that never arrives. So the
segment that ENDS on the punch hangs and then snaps (`u ** SNAP`), and every other
segment is smoothstep, which already leaves the punch pose from rest. The overshoot is
authored as a beat rather than as a tangent, because a pose somebody can read in the
table is a pose somebody can fix.

SEAN'S MOTION LANGUAGE, WHICH IS THE THING THE THREE HAVE TO SHARE:
**He thrusts from the hips along ONE axis and stops dead.** A coil, a single explosive
extension, a braced arrest, a settle. ⚠️ **His arms rake BACK, behind the line of
travel, rather than reaching along it**, which is the detail that makes him read as
propelled rather than as swinging, and it is the same shape in all three: Flame Rush
sends it forward, Ignition Cannon spends it through one arm with the body counter
rotating, and Supernova turns the whole thing on its end, up and then inverted. Ask the
question `ASTRA.md` asks: with every effect hidden, the rush is a launch, the cannon is
a round being chambered, and the ultimate is the launch again, bigger, coming down.
"""
import argparse
import json
import math
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from glb_action import Rig, append_action, rotations

# How sharply the segment arriving at an impact accelerates. `ClipBuilder.PunchIn` is
# 2.1 times the linear slope on a Hermite tangent; on a baked ease this is the exponent
# that produces the same read, hanging at the start and fastest in the last few degrees.
SNAP = 2.6

# Beat layout, one row per pose:
#   t, root x/y/z, torso pitch/yaw/roll, head pitch/yaw, leg-left pitch, leg-right
#   pitch, arm-left pitch/spread, arm-right pitch/spread.
#
# ⚠️ SIGNS, MEASURED OFF THE SLIDE RATHER THAN GUESSED. Negative pitch swings a limb
# FORWARD. An arm hangs at 0 after the T-pose drop, points straight forward at -90,
# straight overhead at -180, and rakes backward at +80. Torso pitch is a forward fold.
# Head pitch is positive DOWN, so a body folded forward with a negative head is looking
# where it is going.
FIELDS = ("t", "contact", "ry", "rz", "tp", "ty", "tr", "hp", "hy",
          "ll", "lr", "alp", "als", "arp", "ars")

# ⚠️⚠️ THIS RIG CANNOT CROUCH, AND EVERY CLIP THAT TRIED TO PUT ITS FEET THROUGH
# THE ROAD. There are no knees: a leg is one rigid segment from the hip to the sole, so
# lowering the hips lowers the feet with them, and there is nothing to fold. Worse, the
# shoe extends FORWARD of the ankle, so swinging a leg BACKWARD rotates the toe DOWN:
# measured on `team-sean`, the sprinter's set at 0.08 s put the trailing toe **0.100
# below the road** and Supernova's landing put it **0.142** under.
#
# ⚠️ THE PROCEDURAL CLIPS THIS REPLACES CARRY THE SAME FAULT AND IT IS SMALLER RATHER
# THAN ABSENT: `BuildSeanDash` keys `localPosition.y` to -0.04 at 0.10 s with the legs
# still at rest, so the feet sink about 4 per cent of body height for a tenth of a
# second. It is not a criticism of that file; it is what the API and the rig make easy.
#
# SO THE CROUCH IS SPENT ON THE SPINE AND THE ROOT HEIGHT IS SOLVED. `contact` says how
# much of this beat is standing on the ground: at 1 the root is placed so the lowest
# skinned vertex sits exactly on the road, whatever the legs are doing, and at 0 the
# authored `ry` lift is used raw because the body is genuinely in the air. In between it
# blends, which is what a takeoff and a landing are. A knee-less character reading as
# LOW is a character folded at the waist, and all three clips below say it that way.

HEROES = {
    "sean": {
        "hero-sean-dash": {
            "punch": 0.25,
            # FLAME RUSH. A sprinter's load, then everything goes forward at once and
            # the arms rake back into wings. The head stays UP through the whole thing:
            # he is looking down the lane he is about to set on fire, and a tucked head
            # would read as a stumble rather than as a launch.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   4, 0, 0,   2, 0,    0,   0,    0, 0,     0, 0),
                (0.08, 1.0, 0.00, -0.02, 30, 0, 0, -14, 0,  -24,  16,  -40, 6,   -40, -6),
                (0.25, 1.0, 0.00, 0.13,  48, 0, 0, -36, 0,  -26,  30,   80, 18,   80, -18),
                (0.34, 1.0, 0.00, 0.12,  41, 0, 0, -29, 0,  -19,  24,   66, 15,   66, -15),
                (0.42, 1.0, 0.00, 0.08,  29, 0, 0, -20, 0,  -11,  15,   45, 11,   45, -11),
                (0.55, 1.0, 0.00, 0.00,   4, 0, 0,   2, 0,    0,   0,    0, 0,     0, 0),
            ],
            # ⚠️ CONTACT 1 THROUGHOUT, EVEN THOUGH THE IMPULSE CARRIES 1.5 UP. The
            # motor owns his real height; this clip owns his shape, and a rush along a
            # road is the one power where a foot through the tarmac would be read as a
            # bug rather than as speed.
            "grounded": (0.00, 0.08, 0.25, 0.34, 0.42, 0.55),
        },
        "hero-sean-ignite": {
            "punch": 0.28,
            # IGNITION CANNON. One arm, compact, and the body counter rotates behind it.
            # ⚠️ THE FIST STOPS AND HOLDS, which is the whole read: this is the one
            # power whose effect happens later, so the cast has to LOOK finished or the
            # three people deciding whether to cross his lane have nothing to go on.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,   0, 0,    0,   0,    0, 0,     0, 0),
                (0.10, 1.0, 0.00, -0.01, -6, 18, -4, 4, 14,  -6,   6,  -18, -20,  48, 28),
                (0.28, 1.0, 0.00, 0.02,  16, -14, 6, 12, -8,  -9,  10,   40, 22, -92, -14),
                (0.34, 1.0, 0.00, 0.02,  11, -9, 4,   8, -5,  -6,   7,   32, 18, -78, -11),
                (0.45, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0,   0,    0, 0,    0, 0),
            ],
            "grounded": (0.00, 0.10, 0.28, 0.34, 0.45),
        },
        "hero-sean-supernova": {
            "punch": 0.65,
            # SUPERNOVA. The rush turned on its end. Same coil, same single axis, same
            # dead stop, and it is the biggest of the three because the extension runs
            # the full height of the body and then reverses through it.
            #
            # ⚠️ THE HANG AT 0.50 IS ALMOST THE APEX POSE ON PURPOSE. `ClipBuilder`'s
            # anticipation is what makes the drop read, and a beat that barely moves is
            # the readable way to spend a tenth of a second not moving.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,    0, 0, 0,    0, 0,   0,   0,    0, 0,    0, 0),
                (0.14, 1.0, 0.00, 0.00,   36, 0, 0,   10, 0,  -8,   6,   72, 14,  72, -14),
                (0.35, 0.0, 0.22, 0.05,  -34, 0, 0,  -46, 0,  28,  24, -158, 10, -158, -10),
                (0.50, 0.0, 0.18, 0.04,  -24, 0, 0,  -38, 0,  22,  18, -150, 9,  -150, -9),
                (0.65, 1.0, 0.00, 0.02,   64, 0, 0,   36, 0, -16,  13,  -30, 5,   -30, -5),
                (0.78, 1.0, 0.00, 0.01,   44, 0, 0,   24, 0, -10,   8,   26, 8,    26, -8),
                (1.00, 1.0, 0.00, 0.00,    0, 0, 0,    0, 0,   0,   0,    0, 0,    0, 0),
            ],
            # ⚠️ THE ONLY TWO BEATS IN SEAN'S KIT THAT LEAVE THE GROUND, and they say
            # so with `contact` 0 rather than by hoping a number is big enough. Between
            # them and their neighbours the contact term blends, which is the takeoff and
            # the landing. Asserting floor clearance through the apex would be asserting
            # that a leap does not leave the road.
            "grounded": (0.00, 0.14, 0.65, 0.78, 1.00),
        },
    },
}


def sample(beats, punch, t):
    """The pose at t, with the hang-and-snap ease on the run into the impact."""
    for a, b in zip(beats, beats[1:]):
        if t <= b[0] + 1e-8:
            u = max(0.0, min(1.0, (t - a[0]) / (b[0] - a[0])))
            u = u ** SNAP if abs(b[0] - punch) < 1e-6 else u * u * (3 - 2 * u)
            return [x + (y - x) * u for x, y in zip(a[1:], b[1:])]
    raise ValueError(t)


def angles_from(values):
    contact, ry, rz, tp, ty, tr, hp, hy, ll, lr, alp, als, arp, ars = values
    return (contact, ry, rz,
            {"root": (0, 0, 0), "torso": (tp, ty, tr), "head": (hp, hy, 0),
             "leg-left": (ll, 0, 0), "leg-right": (lr, 0, 0),
             "arm-left": (alp, 0, als), "arm-right": (arp, 0, ars)})


def author(path, name, spec):
    rig = Rig(path)
    beats, punch = spec["beats"], spec["punch"]
    duration = beats[-1][0]

    frames = int(round(duration * 60))
    times = sorted(set([round(i / 60, 8) for i in range(frames)]
                       + [b[0] for b in beats]))

    order = ["root", "torso", "head", "leg-left", "leg-right", "arm-left", "arm-right"]
    tracks = {n: [] for n in order}
    root = []
    dips = []

    lifts = []

    for t in times:
        contact, ry, rz, angles = angles_from(sample(beats, punch, t))
        rots = rotations(angles)
        # ⚠️ THE SOLVED TERM IS WHAT PUTS A FOOT ON THE ROAD, and it is weighted by
        # `contact` rather than applied flat: at 1 the lowest skinned vertex lands
        # exactly on the rest floor whatever the legs are doing, and at 0 none of it is
        # used, because a body in the air is not standing on anything.
        planted = rig.floor - rig.lowest(rig.posed(rots))
        y = ry + contact * planted
        root.append((0.0, float(y), float(rz)))
        lifts.append((t, planted, y))
        for n in order:
            q = rots[n]
            tracks[n].append((q.x, q.y, q.z, q.w))
        dips.append((t, rig.lowest(rig.posed(rots)) + y - rig.floor))

    # ⚠️ ONLY THE GROUNDED BEATS ARE ASSERTED, AND THE WORST DIP IS REPORTED WHATEVER
    # IT IS. A number in the report is what lets the next pass tune a pose; an assertion
    # over the whole clip would refuse a leap for leaving the road.
    for t in spec["grounded"]:
        low = next(v for u, v in dips if abs(u - t) < 1e-6)
        assert low > -0.005, (path.name, name, t, round(low, 5),
                              "a grounded beat puts the body through the road")

    worst = min(dips, key=lambda d: d[1])

    report = append_action(rig, name, times, tracks, root, order=order)
    report.update(punch=punch,
                  deepest_dip=round(worst[1], 5), deepest_dip_at=worst[0],
                  grounded_beats=list(spec["grounded"]),
                  root_y_range=[round(min(v for _, _, v in lifts), 5),
                                round(max(v for _, _, v in lifts), 5)],
                  largest_plant=round(max(abs(p) for _, p, _ in lifts), 5))
    return report


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--hero", required=True, choices=sorted(HEROES))
    ap.add_argument("--action", help="one action name; default is all three")
    ap.add_argument("files", type=Path, nargs="+")
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:]
                         if "--" in sys.argv else sys.argv[1:])

    actions = HEROES[args.hero]
    if args.action:
        actions = {args.action: actions[args.action]}

    for path in args.files:
        for name, spec in actions.items():
            print("HERO_ACTION " + json.dumps(author(path, name, spec)))
