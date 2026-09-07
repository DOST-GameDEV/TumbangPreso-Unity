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

# Beat layout, one row per pose, in `FIELDS` order.
#
# ⚠️ THE LEGS TAKE A ROLL AS WELL AS A PITCH, AND ZACK IS WHY. A pitch alone swings a
# leg forward and back, which is a run. A skater's push is LATERAL: the leg goes out to
# the side and the body carves over it, and that is the difference between Bolt Sprint
# and a sprint. Sean's tables carry zeros in both roll columns and are unchanged by
# their arrival, which was checked by re-authoring his three clips and diffing the file.
#
# ⚠️ SIGNS, MEASURED OFF THE SLIDE RATHER THAN GUESSED. Negative pitch swings a limb
# FORWARD. An arm hangs at 0 after the T-pose drop, points straight forward at -90,
# straight overhead at -180, and rakes backward at +80. Torso pitch is a forward fold.
# Head pitch is positive DOWN, so a body folded forward with a negative head is looking
# where it is going.
FIELDS = ("t", "contact", "ry", "rz", "tp", "ty", "tr", "hp", "hy",
          "ll", "llr", "lr", "lrr", "alp", "als", "arp", "ars")

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
                (0.00, 1.0, 0.00, 0.00,   4, 0, 0,   2, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.08, 1.0, 0.00, -0.02, 30, 0, 0, -14, 0,  -24, 0,  16, 0,  -40, 6,   -40, -6),
                (0.25, 1.0, 0.00, 0.13,  48, 0, 0, -36, 0,  -26, 0,  30, 0,   80, 18,   80, -18),
                (0.34, 1.0, 0.00, 0.12,  41, 0, 0, -29, 0,  -19, 0,  24, 0,   66, 15,   66, -15),
                (0.42, 1.0, 0.00, 0.08,  29, 0, 0, -20, 0,  -11, 0,  15, 0,   45, 11,   45, -11),
                (0.55, 1.0, 0.00, 0.00,   4, 0, 0,   2, 0,    0, 0,   0, 0,    0, 0,     0, 0),
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
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,   0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.10, 1.0, 0.00, -0.01, -6, 18, -4, 4, 14,  -6, 0,   6, 0,  -18, -20,  48, 28),
                (0.28, 1.0, 0.00, 0.02,  16, -14, 6, 12, -8,  -9, 0,  10, 0,   40, 22, -92, -14),
                (0.34, 1.0, 0.00, 0.02,  11, -9, 4,   8, -5,  -6, 0,   7, 0,   32, 18, -78, -11),
                (0.45, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
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
                (0.00, 1.0, 0.00, 0.00,    0, 0, 0,    0, 0,   0, 0,   0, 0,    0, 0,    0, 0),
                (0.14, 1.0, 0.00, 0.00,   36, 0, 0,   10, 0,  -8, 0,   6, 0,   72, 14,  72, -14),
                (0.35, 0.0, 0.22, 0.05,  -34, 0, 0,  -46, 0,  28, 0,  24, 0, -158, 10, -158, -10),
                (0.50, 0.0, 0.18, 0.04,  -24, 0, 0,  -38, 0,  22, 0,  18, 0, -150, 9,  -150, -9),
                (0.65, 1.0, 0.00, 0.02,   64, 0, 0,   36, 0, -16, 0,  13, 0,  -30, 5,   -30, -5),
                (0.78, 1.0, 0.00, 0.01,   44, 0, 0,   24, 0, -10, 0,   8, 0,   26, 8,    26, -8),
                (1.00, 1.0, 0.00, 0.00,    0, 0, 0,    0, 0,   0, 0,   0, 0,    0, 0,    0, 0),
            ],
            # ⚠️ THE ONLY TWO BEATS IN SEAN'S KIT THAT LEAVE THE GROUND, and they say
            # so with `contact` 0 rather than by hoping a number is big enough. Between
            # them and their neighbours the contact term blends, which is the takeoff and
            # the landing. Asserting floor clearance through the apex would be asserting
            # that a leap does not leave the road.
            "grounded": (0.00, 0.14, 0.65, 0.78, 1.00),
        },
    },
    # ⚠️⚠️ ZACK IS BUILT AGAINST SEAN, NOT BESIDE HIM. 🧑, 2026-09-02, looking at the
    # two kits: *"the kit of zack and sean are the exact fricking same"*. That was about
    # the ABILITIES and `ZackHeroKit` records the split it caused, but it applies twice
    # over to the animation, because two heroes who move the same way are one hero with
    # two colour ramps however different the payloads are.
    #
    # **Sean thrusts along ONE axis, symmetric, and stops dead. Zack is BLADED and
    # STACCATO.** He turns side-on to travel and leads with a shoulder; his halves
    # oppose each other, torso twisting against the hips and arms counter-swinging; and
    # his poses arrive in a snap followed by an electrical chatter rather than a settle.
    # Where Sean's ultimate is symmetric and vertical, because he IS the meteor, Zack's
    # is asymmetric and DIRECTIONAL, because Thunderstrike is aimed up to 7 m away and
    # he is pointing at it. Every symmetric pose in Sean's tables has an asymmetric
    # counterpart here, on purpose.
    "zack": {
        "hero-zack-sprint": {
            # ⚠️⚠️ NO PUNCH, AND THE ABSENCE IS INHERITED FROM `BuildZackSprint`'S OWN
            # ARGUMENT, WHICH IS RIGHT: *"Bolt Sprint is LOCOMOTION, not a strike: it is
            # a skating cycle held for the whole dash, and there is no instant at which
            # anything lands. Snapping a cycle to a stop would read as the animation
            # breaking."* Two push-glide cycles in 0.60 s against the ability's 2.5 s
            # `Duration`, so the chain loops it rather than playing it once and stopping.
            "punch": None,
            # The carve is the whole silhouette: a deep fold, the torso twisted side-on
            # to the direction of travel, and the body rolled INTO the turn. The push leg
            # goes out to the side (roll) and back (pitch); the glide leg stays under
            # him. Arms counter-swing across the body, which is what a skater does with
            # them and what a runner does not.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,    0, 0),
                (0.15, 1.0, 0.00, 0.05,  34, -20, -15, -22, 16, 22, -18, -30, 6, -62, -8,  52, -6),
                (0.30, 1.0, 0.00, 0.08,  37, 20, 15,  -24, -16, -32, 5, 24, 20,  50, 6, -66, 8),
                (0.45, 1.0, 0.00, 0.05,  33, -16, -12, -20, 13, 19, -15, -27, 5, -55, -7, 46, -5),
                (0.60, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,    0, 0),
            ],
            "grounded": (0.00, 0.15, 0.30, 0.45, 0.60),
        },
        "hero-zack-charge": {
            # ⚠️⚠️ THIS ONE GAINS A PUNCH THAT THE PROCEDURAL CLIP DOES NOT HAVE, AND THE
            # REASON IS THE ABILITY RATHER THAN THE ANIMATION. `BuildZackCharge` is a
            # 0.40 s vibration with no impact, and its header's argument holds for a
            # buzz. But MAGNET's whole effect is that **the tsinelas arrives in his
            # hand**, and that is an event the other three players have to be able to
            # read: it is the difference between "he is doing something" and "he has his
            # shoe back and is about to throw it". So the buzz is the wind-up and the
            # CATCH at 0.34 is the impact.
            "punch": 0.30,
            # The arm snaps out open toward the shoe, the body braces AWAY from the pull,
            # the chatter runs while the tsinelas is dragged in, and the hand closes to
            # the chest. ⚠️ The chatter beats are 0.04 s apart, which is two and a bit
            # frames, so they read as a buzz rather than as a sine. ⚠️ The clip is 0.40 s,
            # which is `BuildZackCharge`'s length: the catch was fitted INSIDE it rather
            # than added to the end, because lengthening a cast to suit an animation is
            # the retune `ASTRA.md` forbids.
            #
            # ⚠️⚠️ THE CATCH HAD TO BE MADE BIGGER THAN THE REACH, AND THE VERIFIER IS
            # WHAT SAID SO. The first table snapped the arm from rest to -96 degrees in
            # 0.05 s and then closed it 44 degrees at the catch, so the fastest frame in
            # the clip was the OPENING and the impact check failed, correctly: whichever
            # moment is fastest is the one a player reads as the event, whatever the
            # table calls the punch. The reach is 0.07 s now and the catch sweeps 75
            # degrees into a dead stop, so the shoe arriving is the loudest thing in it.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,   0, 0,     0, 0),
                (0.07, 1.0, 0.00, -0.02, -12, -22, 6, -6, -18, -8, 0,   9, 0,  34, 26,  -96, -18),
                (0.11, 1.0, 0.00, -0.02, -9, -18, 2,  -3, -15, -8, 0,   9, 0,  30, 22,  -90, -14),
                (0.15, 1.0, 0.00, -0.02, -13, -24, 8, -7, -19, -8, 0,   9, 0,  36, 28,  -99, -20),
                (0.19, 1.0, 0.00, -0.02, -8, -17, 1,  -2, -14, -8, 0,   9, 0,  29, 21,  -88, -13),
                (0.23, 1.0, 0.00, -0.01, -13, -23, 7, -7, -19, -7, 0,   8, 0,  35, 27,  -97, -19),
                (0.25, 1.0, 0.00, -0.01, -14, -25, 8, -8, -20, -7, 0,   8, 0,  36, 28,  -95, -20),
                (0.30, 1.0, 0.00, 0.03,  20, 18, -6,  16, 13,  -12, 0, 13, 0,  48, 31,  -20, -34),
                (0.35, 1.0, 0.00, 0.01,  13, 11, -3,  10, 8,   -7, 0,   8, 0,  31, 20,  -13, -22),
                (0.40, 1.0, 0.00, 0.00,   0, 0, 0,     0, 0,    0, 0,   0, 0,   0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.07, 0.15, 0.23, 0.30, 0.35, 0.40),
        },
        "hero-zack-summon": {
            # ⚠️ THE PUNCH IS 0.45 AND IT IS THE POINT, NOT THE RAISE. `BuildZackSummon`
            # says it in one line and it is worth keeping: *"The bolt comes DOWN. The
            # raise at 0.28 is the call and it stays smooth."*
            "punch": 0.45,
            # ⚠️⚠️ ONE ARM, NOT TWO, WHICH IS THE WHOLE SEPARATION FROM SUPERNOVA. The
            # procedural version throws both arms overhead and slams both down, which is
            # Sean's ultimate with a different particle system. Thunderstrike is AIMED,
            # up to `MaxRange` 7 m away, so the body has to end up pointing at somewhere
            # that is not where it is standing: the right arm calls the sky, the whole
            # torso untwists through the strike, and the arm finishes level and forward
            # at the spot. The left arm is a counterweight thrown back, which is what
            # makes the twist read from any angle.
            #
            # ⚠️⚠️ THE HOLD AT 0.36 IS THE ABILITY'S OWN WORD AND IT IS ALSO WHAT MAKES
            # THE STRIKE THE FASTEST THING IN THE CLIP. The card reads *"Hold to pick a
            # spot, let go and the sky opens on it"*, and the first table had no hold: it
            # raised the arm through 220 degrees and then dropped it through 58, so the
            # verifier reported the fastest frame in the middle of the RAISE and refused
            # the clip. It was right, and the note it was refusing is `BuildZackSummon`'s
            # own: *"The bolt comes DOWN. The raise at 0.28 is the call and it stays
            # smooth."* The raise is longer and shallower now, the hold is nine frames of
            # almost nothing, and the release sweeps 83 degrees in 0.09 s into a stop.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.12, 1.0, 0.00, -0.02, 26, 26, 8,  16, 20,  -7, 0,   8, 0,   44, 10,   20, 10),
                (0.30, 0.55, 0.09, 0.02, -34, 30, 14, -48, 22, -18, 0, 15, 0,   64, 18, -160, 26),
                (0.36, 0.80, 0.04, 0.02, -32, 28, 13, -46, 21, -17, 0, 14, 0,   62, 17, -158, 25),
                (0.45, 1.0, 0.00, 0.05,  40, -26, -12, 26, -20, -24, 0, 20, 0,  74, 20,  -75, -34),
                (0.55, 1.0, 0.00, 0.04,  30, -18, -8,  18, -14, -17, 0, 14, 0,  56, 15,  -88, -26),
                (0.62, 1.0, 0.00, 0.03,  34, -22, -10, 22, -17, -20, 0, 17, 0,  64, 17,  -80, -30),
                (0.75, 1.0, 0.00, 0.00,   0, 0, 0,     0, 0,    0, 0,   0, 0,   0, 0,     0, 0),
            ],
            # ⚠️ THE CALL LIFTS HIM ONTO HIS TOES RATHER THAN OFF THE GROUND, so `contact`
            # is 0.55 at the apex and not 0. He is reaching, not jumping, and a
            # Thunderstrike that left the road would read as the same leap Supernova
            # already owns, which is the whole thing these two kits are being kept apart
            # from.
            "grounded": (0.00, 0.12, 0.45, 0.55, 0.62, 0.75),
        },
    },
}


def sample(beats, punch, t):
    """The pose at t, with the hang-and-snap ease on the run into the impact.

    ⚠️⚠️ `punch` MAY BE `None`, AND THE ABSENCE IS A DESIGN DECISION RATHER THAN AN
    OMISSION. `HeroAbilityClips` makes the argument in its own words about two of Zack's
    three: Bolt Sprint is LOCOMOTION, *"a skating cycle held for the whole dash, and
    there is no instant at which anything lands. Snapping a cycle to a stop would read
    as the animation breaking."* A clip with no punch is smoothstep throughout, and the
    verifier drops its impact assertions for exactly that clip rather than being talked
    out of them globally.
    """
    for a, b in zip(beats, beats[1:]):
        if t <= b[0] + 1e-8:
            u = max(0.0, min(1.0, (t - a[0]) / (b[0] - a[0])))
            snapping = punch is not None and abs(b[0] - punch) < 1e-6
            u = u ** SNAP if snapping else u * u * (3 - 2 * u)
            return [x + (y - x) * u for x, y in zip(a[1:], b[1:])]
    raise ValueError(t)


def angles_from(values):
    (contact, ry, rz, tp, ty, tr, hp, hy,
     ll, llr, lr, lrr, alp, als, arp, ars) = values
    return (contact, ry, rz,
            {"root": (0, 0, 0), "torso": (tp, ty, tr), "head": (hp, hy, 0),
             "leg-left": (ll, 0, llr), "leg-right": (lr, 0, lrr),
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
