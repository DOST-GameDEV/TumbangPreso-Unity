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

⚠️⚠️ EACH HERO IS ONE DIRECTION AND NO TWO MAY SHARE IT, WHICH IS THE ONE RULE THIS
TABLE IS ORGANISED AROUND. `ASTRA.md`: *"Each hero should have a distinct motion
language"*, and *"if all VFX disappeared, would this still unmistakably be this hero's
ultimate?"* The per-hero comment below each key says what that hero's direction IS,
and the numbers it quotes are the verifier's rather than adjectives:

| | The direction | Feet | Impact stops within |
|---|---|---|---|
| **Sean** | FORWARD, one axis, symmetric, arms raked back behind the line of travel | Supernova LEAPS, 0.160 clear | 2.3 to 5.9 per cent of peak |
| **Zack** | SIDEWAYS, bladed, halves opposing, snap then electrical chatter | the call goes onto the TOES, 0.074 | 2.8 to 8.6 per cent |
| **Dante** | DOWN, wide, planted, heavy, and he never travels | 0.000 in all three clips | 1.4 to 5.7 per cent |
| **Cheska** | NOWHERE. She stays upright and spends a hand, then HOLDS | a controlled rise, 0.022 | **0.0 to 0.7 per cent** |

⚠️ THE LAST COLUMN IS THE CLEAREST OF THE FOUR AND IT WAS NOT DESIGNED, IT WAS
MEASURED. Cheska's casts stop harder than anybody's because ice is the element that
stops, which `BuildCheskaRaise` said first: *"The pillars lock. Ice is the one element
that STOPS, so it should stop."*
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
    # ⚠️⚠️ DANTE IS THE THIRD DIRECTION, AND THAT IS THE WHOLE BRIEF. Sean's power goes
    # FORWARD along one axis. Zack's goes SIDEWAYS, bladed and counter-rotating.
    # **Dante's goes DOWN, into the floor, and he never travels at all.** He plants,
    # widens, and drives mass through his own centre. `contact` is 1 in all three of his
    # clips and there is not one airborne beat in the kit: Supernova leaps and
    # Thunderstrike goes up on its toes, and a Dante who left the road would be
    # borrowing from both.
    #
    # ⚠️ THE STANCE IS THE SIGNATURE AND IT IS THE LEG ROLL THAT DRAWS IT. Every cast
    # splays both legs outward (positive roll on the left, negative on the right, which
    # is outward on this rig) into a braced base no other hero uses. His arms move
    # TOGETHER, low and wide, and never take the graceful overhead arc Sean and Zack
    # both own: they come up short of vertical and hammer down. His head stays low and
    # forward like a bull rather than being thrown back.
    #
    # ⚠️ AND HIS RECOVERIES ARE LONG BECAUSE HE IS HEAVY. Sean settles in 0.13 s after
    # his ultimate lands and Dante takes 0.45, which is most of the reason his clips are
    # the longest in the game at 0.55, 0.65 and 0.85.
    "dante": {
        "hero-dante-stomp": {
            "punch": 0.30,
            # SEISMIC STOMP. The knee comes up, the body gathers UP, and then everything
            # goes down at once. ⚠️ There are no knees, so the raise is the whole left
            # leg pitched forward and the solve keeps the RIGHT foot on the road: with
            # `contact` at 1 the lowest vertex is the planted foot, which is exactly what
            # a one-legged stance should key.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.18, 1.0, 0.00, -0.02, -14, 0, 0, -10, 0,  -58, 6,   4, -6,   20, 26,   20, -26),
                (0.30, 1.0, 0.00, 0.03,  34, 0, 0,  22, 0,     8, 20, -6, -20, -12, 34,  -12, -34),
                (0.38, 1.0, 0.00, 0.02,  26, 0, 0,  17, 0,     6, 15, -4, -15,  -6, 26,   -6, -26),
                (0.55, 1.0, 0.00, 0.00,   0, 0, 0,   0, 0,     0, 0,   0, 0,     0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.18, 0.30, 0.38, 0.55),
        },
        "hero-dante-roar": {
            "punch": 0.32,
            # DEMONIC CARAPACE. ⚠️⚠️ THE STOMP GOES DOWN AND THIS GOES OUT, which is the
            # only thing keeping his two skills apart: they are both a plant and a drop,
            # and one of them has to be a WIDENING or Dante has one skill twice. He draws
            # in small, then the legs splay, the shoulders come up, and both arms drive
            # down and OUT into the flex. The card is *"Nothing stuns, shoves or slips
            # you"*, so the pose that holds afterwards is the read: he is bigger now.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.15, 1.0, 0.00, -0.02, 22, 0, 0,   16, 0,   -6, -4,  5, 4,  -40, -22, -40, 22),
                (0.32, 1.0, 0.00, 0.02, -16, 0, 0,  -20, 0,  -10, 26, 12, -26,  14, 40,   14, -40),
                (0.44, 1.0, 0.00, 0.01, -11, 0, 0,  -14, 0,   -8, 22, 10, -22,  11, 34,   11, -34),
                (0.65, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.15, 0.32, 0.44, 0.65),
        },
        "hero-dante-fissure": {
            "punch": 0.40,
            # TITAN FISSURE. ⚠️ IT SPLITS THE COURT AHEAD OF HIM, not under him:
            # `telegraphRange` is 2.2 and the radius is 4.5, so the slam has to land
            # FORWARD of his feet or the animation is telling the other three the wrong
            # place to not be standing. Both arms come up short of vertical, hold, and
            # then go down and forward into the road.
            #
            # ⚠️⚠️ THE HOLD AT 0.30 IS WHAT MAKES THIS THE BIGGEST THING IN HIS KIT
            # RATHER THAN THE SECOND BIGGEST. Without it the slam had 0.18 s to travel
            # and read slower than the stomp's knee drop, which would put his ultimate
            # below his first skill on the one measurement this project has for force. It
            # is also just correct for a titan: the weight hangs before it falls.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.22, 1.0, 0.00, -0.03, -36, 0, 0,  -30, 0,  -8, 22, 10, -22, -128, 16, -128, -16),
                (0.30, 1.0, 0.00, -0.03, -34, 0, 0,  -28, 0,  -8, 22, 10, -22, -126, 16, -126, -16),
                (0.40, 1.0, 0.00, 0.06,  56, 0, 0,   34, 0, -20, 28, 16, -28,  -34, 6,   -34, -6),
                (0.52, 1.0, 0.00, 0.05,  44, 0, 0,   27, 0, -16, 24, 13, -24,  -26, 5,   -26, -5),
                (0.62, 1.0, 0.00, 0.03,  30, 0, 0,   18, 0, -11, 17,  9, -17,  -18, 4,   -18, -4),
                (0.85, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,   0, 0,   0, 0,     0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.22, 0.30, 0.40, 0.52, 0.62, 0.85),
        },
    },
    # ⚠️⚠️ CHESKA IS THE ONE WHO COMMITS NOTHING, WHICH IS THE FOURTH THING A BODY CAN
    # DO WITH A CAST. The other three all spend their whole mass: Sean forward, Zack
    # sideways, Dante down. **She stays upright and spends a hand.** Her spine barely
    # moves, her feet stay under her and turned out, and the power is one forearm
    # describing an exact shape in a single plane. Torso pitch never passes 22 degrees in
    # her kit against Dante's 56, and that number is the difference, not a coincidence.
    #
    # ⚠️ AND SHE IS THE ONLY ONE WHO HOLDS. Every clip here ends its impact on a HOLD
    # beat that barely moves before it lowers, because ice is the element that STOPS and
    # `BuildCheskaRaise` already says so in one line: *"The pillars lock. Ice is the one
    # element that STOPS, so it should stop."* Sean settles, Zack chatters, Dante takes
    # his weight back slowly. Cheska arrives, holds, and then puts her arm down.
    "cheska": {
        "hero-cheska-frostwave": {
            "punch": 0.28,
            # PERMAFROST SHEET. ⚠️ It is aimed and thrown to `MaxRange`, so the sweep has
            # to finish POINTING somewhere: the hand draws across the chest and then goes
            # out and down in a flat plane, palm toward the ground it is about to freeze.
            #
            # ⚠️⚠️ THE DRAW IS 0.16 AND THE SWEEP IS 0.12, AND THE FIRST TABLE HAD IT THE
            # OTHER WAY ROUND. With a 0.12 s draw the hand crossed the chest faster than
            # it left it, so the fastest frame in the clip was the WIND-UP and the
            # verifier refused it. Same fault as Zack's Magnet, one hero later, and the
            # same answer: whichever moment is quickest is the one a player reads as the
            # cast, so the anticipation has to be the slow half by construction.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.16, 1.0, 0.00, -0.01,  4, 16, 3,   4, 12,  -5, 3,   4, -3,   20, 14,  -70, -40),
                (0.28, 1.0, 0.00, 0.02,   6, -14, -4, 2, -12, -9, 5,   7, -5,   28, 20,  -40, 44),
                (0.38, 1.0, 0.00, 0.02,   5, -13, -3, 2, -11, -8, 4,   6, -4,   26, 18,  -38, 41),
                (0.50, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.16, 0.28, 0.38, 0.50),
        },
        "hero-cheska-raise": {
            "punch": 0.30,
            # ICE BARRICADE. Both palms come up together and lock. ⚠️ The wall stops
            # bodies AND tsinelas, so the pose that holds is a barrier made with her own
            # forearms: it is the one gesture in the game that says "nothing comes past
            # this" without touching anybody.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.14, 1.0, 0.00, -0.01, 10, 0, 0,    8, 0,   -4, 2,   3, -2,   30, -6,   30, 6),
                (0.30, 1.0, 0.00, 0.02,  -8, 0, 0,  -14, 0,   -4, 5,   3, -5,  -95, 8,   -95, -8),
                (0.40, 1.0, 0.00, 0.02,  -7, 0, 0,  -12, 0,   -4, 5,   3, -5,  -93, 7,   -93, -7),
                (0.55, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.14, 0.30, 0.40, 0.55),
        },
        "hero-cheska-nova": {
            "punch": 0.32,
            # GLACIAL NOVA. Compression, then one flat radial opening. ⚠️⚠️ THE DRAW-IN
            # IS LONG AND THE OPENING IS SHORT, which is what makes this the biggest
            # thing in her kit rather than the second biggest: Ice Barricade's forearms
            # travel 125 degrees in 0.16 s and would otherwise be the fastest moment she
            # has. The nova opens 0.20 to 0.32, which is twelve hundredths for a wider
            # sweep, and then holds longer than either skill.
            #
            # ⚠️ SHE RISES, SHE DOES NOT LEAP. `contact` 0.70 at the burst lifts her onto
            # the balls of her feet and no further. Supernova leaves the road by 0.160 and
            # Dante never leaves it at all; a controlled hero needs a third answer.
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.20, 1.0, 0.00, -0.02, 14, 0, 0,   12, 0,   -4, -3,  3, 3,   -96, -52, -96, 52),
                (0.32, 0.70, 0.03, 0.02, -22, 0, 0, -38, 0,   -6, 12,  5, -12,  10, 74,   10, -74),
                (0.44, 0.85, 0.01, 0.02, -20, 0, 0, -35, 0,   -6, 11,  5, -11,   9, 70,    9, -70),
                (0.56, 1.0, 0.00, 0.01, -10, 0, 0, -18, 0,    -3, 6,   2, -6,    5, 36,    5, -36),
                (0.70, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.20, 0.56, 0.70),
        },
    },
    # ⚠️⚠️ NEMU IS THE ONE WHO DOES NOT PLANT, AND IT IS THE ONLY THING SHE HAS THAT
    # NOBODY ELSE CAN BORROW. Four heroes stand on the road to cast; Dante's whole
    # identity is that he never leaves it. Hers is that she never touches it. `contact`
    # runs to 0.05 in the middle of her ultimate and no beat in her kit is a push-off:
    # she rises without pressing on anything, which is the one thing a body cannot do.
    #
    # ⚠️ AND HER LIMBS ARRIVE BEFORE HER TORSO, WHICH IS EVERYBODY ELSE BACKWARDS. Sean,
    # Zack, Dante and Cheska all lead with the trunk and let the extremities follow; her
    # arms hit their extreme a beat EARLY and the torso catches up, which is what makes
    # a body read as being carried rather than as moving itself. The asymmetry is part of
    # it: her two legs drift the same way rather than opposing, which no living stance
    # does.
    #
    # ⚠️ `BuildNemuGhoststep` ALREADY MADE HALF OF THIS ARGUMENT AND IT IS KEPT WHOLE:
    # *"Nemu going part-ghost is the single power in the game that should have NO weight:
    # she is untaggable while it runs, and the whole read is that the body stops being a
    # body. Every other hero gets a frame where the world stops. Hers does not, and that
    # is what makes it hers."*
    "nemu": {
        "hero-nemu-ghoststep": {
            "punch": None,
            "beats": [
                (0.00, 1.00, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,   0, 0,    0, 0),
                (0.15, 0.30, 0.05, 0.03, -10, 14, -12, -16, 20, -14, 8,  6, 4,  36, 22,  24, -10),
                (0.30, 0.15, 0.07, 0.05,  -6, -12, 10, -12, -18, -6, -6, 16, -10, 20, 30, 40, -18),
                (0.42, 0.70, 0.02, 0.02,  -3, -5, 4,   -5, -8,  -2, -2,  6, -4,   8, 14, 16, -8),
                (0.50, 1.00, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,   0, 0,    0, 0),
            ],
            "grounded": (0.00, 0.50),
        },
        "hero-nemu-project": {
            # ASTRAL HIJACK. ⚠️ The punch is the moment the spirit LEAVES, so the body has
            # to look emptied by it: the chest opens, the head goes back, and one arm is
            # flung out after the thing that left. The card is *"Possess your familiar"*,
            # and a cast that reached out and grabbed would be describing the opposite.
            "punch": 0.26,
            "beats": [
                (0.00, 1.00, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.14, 0.60, 0.03, -0.02, 16, -10, -8, 12, -14, -8, 4,   4, 2,   30, 16,  -56, -30),
                (0.26, 0.25, 0.06, 0.04, -34, 10, 6,  -42, 12, -20, -6, 14, -8,  58, -20, -104, 26),
                (0.34, 0.45, 0.04, 0.03, -24, 7, 4,   -30, 8,  -14, -4, 10, -6,  42, -14, -88, 20),
                (0.50, 1.00, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.50),
        },
        "hero-nemu-seance": {
            # DEVOURING SEANCE. ⚠️⚠️ IT COLLAPSES INWARD AND EVERY OTHER ULTIMATE IN THE
            # GAME STRIKES OUTWARD, which is deliberate and is the clearest single frame
            # of separation in the six kits. Supernova comes down, Thunderstrike points,
            # Titan Fissure splits, Glacial Nova opens. Hers PULLS: she rises with her
            # arms wide, and then the whole body is dragged in and folded toward the
            # thing she opened. The word in the card is *"consuming"*.
            "punch": 0.38,
            "beats": [
                (0.00, 1.00, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,     0, 0,     0, 0),
                (0.20, 0.10, 0.10, -0.02, -22, -14, -10, -34, 16, -10, 6,  8, -6, -130, 40, -124, -46),
                (0.30, 0.05, 0.11, -0.01, -20, -12, -9,  -32, 14,  -9, 5,  7, -5, -128, 38, -122, -44),
                (0.38, 0.15, 0.05, 0.03,  44, -14, 8,   30, -12, -30, -8, -24, 8,  -26, -44, -30, 40),
                (0.50, 0.30, 0.04, 0.02,  38, -11, 6,   25, -9,  -25, -6, -20, 6,  -22, -36, -26, 33),
                (0.62, 0.60, 0.02, 0.01,  18, -5, 3,    12, -4,  -12, -3, -10, 3,  -10, -17, -12, 15),
                (0.80, 1.00, 0.00, 0.00,   0, 0, 0,     0, 0,     0, 0,   0, 0,    0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.80),
        },
    },
    # ⚠️⚠️ PHAISTER PERFORMS, AND THAT IS THE SIXTH AND LAST DIRECTION. The other five
    # are all doing something TO the court: Sean drives through it, Zack points at it,
    # Dante breaks it, Cheska freezes a piece of it, Nemu is taken out of it. **She is
    # doing something IN FRONT of it.** Every cast here passes through a FLOURISH beat,
    # an off-axis pose neither the gather nor the strike would reach on its own, and
    # every one of them finishes front-on with the chest open and the pose held for the
    # room. It is the difference between casting a spell and presenting one.
    #
    # ⚠️ IT IS THE ORNAMENT THAT SEPARATES HER FROM CHESKA, WHO IS THE OTHER ONE WHO
    # HOLDS. Cheska is minimal and exact: the shortest line between rest and the shape,
    # then stop. Phaister takes the long way round on purpose. Same stillness at the end,
    # opposite route to it, and the flourish beat is where that lives in the table.
    #
    # ⚠️ SHADOW BLINK IS HER ONE EXCEPTION AND THE CONTRAST IS THE POINT. It has no
    # flourish, because a blink has no time to have one: she collapses inward and is
    # thrown open somewhere else. `BuildPhaisterBlink` already made the neighbouring
    # argument about her and Nemu: *"Ghost Step is a state you enter and drift in, so it
    # has no frame where the world stops. A blink is INSTANTANEOUS."*
    "phaister": {
        "hero-phaister-hex": {
            # Draw the sigil, then stamp it into the ground. ⚠️ It is aimed to `MaxRange`,
            # so the stamp finishes reaching FORWARD at the chalk rather than at her own
            # feet, the same requirement Titan Fissure has and for the same reason.
            "punch": 0.34,
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.12, 1.0, 0.00, -0.01, 10, 18, 4,   8, 14,  -4, 0,   3, 0,   24, 20,  -30, 34),
                (0.24, 1.0, 0.00, 0.00, -6, -6, 8,  -20, -10, -6, 3,   5, -3,  34, 26, -118, -26),
                (0.34, 1.0, 0.00, 0.05,  30, 0, 0,   24, 0,  -14, 8,  11, -8,  46, 30,  -52, 40),
                (0.42, 1.0, 0.00, 0.04,  24, 0, 0,   19, 0,  -11, 6,   9, -6,  37, 24,  -46, 34),
                (0.55, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.12, 0.24, 0.34, 0.42, 0.55),
        },
        "hero-phaister-blink": {
            # Collapse inward, then snap out of the far side, thrown open and front-on.
            "punch": 0.24,
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
                (0.12, 0.90, 0.00, -0.03, 26, 0, 0,  20, 0,   -3, 0,   2, 0,   -70, -50, -70, 50),
                (0.24, 0.60, 0.03, 0.06, -20, 0, 0, -26, 0,  -10, 12,  8, -12,  30, 52,   30, -52),
                (0.32, 0.85, 0.01, 0.04, -14, 0, 0, -18, 0,   -7, 8,   6, -8,   22, 44,   22, -44),
                (0.42, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,    0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.12, 0.32, 0.42),
        },
        "hero-phaister-eclipse": {
            # GRAND COVEN. ⚠️⚠️ THE HOLD IS THE POINT AND IT IS THE LONGEST ANTICIPATION
            # IN THE SIX KITS, which `BuildPhaisterEclipse` states as a requirement rather
            # than a flourish: *"`Hero_Strike_Balance.md` § 4.3 asks for a wind-up so the
            # payoff has a moment; this is the longest anticipation of the six kits, which
            # is what an arena-wide power should cost to cast."* Sixteen frames pass
            # between the arms reaching the sky and the night coming down.
            "punch": 0.62,
            "beats": [
                (0.00, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,     0, 0,     0, 0),
                (0.20, 1.0, 0.00, -0.02, 20, 0, 0,   16, 0,   -5, 0,   4, 0,    40, -14,  40, 14),
                (0.36, 1.0, 0.00, -0.01, -10, 0, 0, -20, 0,   -6, 4,   5, -4,  -96, 62,  -96, -62),
                (0.44, 0.80, 0.03, 0.01, -34, 0, 0, -46, 0,   -8, 6,   6, -6, -166, 18, -166, -18),
                (0.52, 0.80, 0.03, 0.01, -33, 0, 0, -45, 0,   -8, 6,   6, -6, -164, 17, -164, -17),
                (0.62, 1.0, 0.00, 0.06,  34, 0, 0,   28, 0,  -16, 16, 13, -16,  -10, 66,  -10, -66),
                (0.74, 1.0, 0.00, 0.05,  26, 0, 0,   21, 0,  -12, 12, 10, -12,   -6, 58,   -6, -58),
                (0.95, 1.0, 0.00, 0.00,   0, 0, 0,    0, 0,    0, 0,   0, 0,     0, 0,     0, 0),
            ],
            "grounded": (0.00, 0.20, 0.36, 0.62, 0.74, 0.95),
        },
    },
}


# The 2026-09-10 runtime capture showed several instant effects arriving a quarter
# second before the body's peak, and Supernova recovering while still descending.
# These are PRESENTATION clocks. Ability windup, contact, range and cooldown remain
# owned by the game. Distinct source poses above are retained and timed to their jobs.
CAST_PRESENTATION_TIMES = {
    "hero-sean-dash": (0,.045,.11,.19,.36,.58),
    "hero-sean-ignite": (0,.05,.13,.28,.52),
    "hero-sean-supernova": (0,.20,.52,.78,1.12,1.30,1.55),
    "hero-zack-sprint": (0,.16,.32,.48,.64),
    "hero-zack-summon": (0,.11,.27,.34,.40,.53,.65,.82),
    "hero-dante-stomp": (0,.045,.10,.24,.58),
    "hero-dante-roar": (0,.055,.14,.36,.70),
    "hero-dante-fissure": (0,.18,.31,.40,.54,.70,1.0),
    "hero-cheska-frostwave": (0,.045,.11,.30,.56),
    "hero-cheska-raise": (0,.05,.12,.34,.62),
    "hero-cheska-nova": (0,.23,.40,.55,.68,.85),
    "hero-nemu-ghoststep": (0,.18,.36,.50,.62),
    "hero-nemu-project": (0,.05,.14,.32,.60),
    "hero-nemu-seance": (0,.18,.31,.40,.57,.73,.95),
    "hero-phaister-hex": (0,.045,.085,.14,.30,.64),
    "hero-phaister-blink": (0,.04,.09,.22,.46),
    "hero-phaister-eclipse": (0,.04,.08,.115,.14,.18,.38,.85),
}
for actions in HEROES.values():
    for name,spec in actions.items():
        if name not in CAST_PRESENTATION_TIMES: continue
        times=CAST_PRESENTATION_TIMES[name]
        previous=[b[0] for b in spec["beats"]]
        assert len(times)==len(previous),name
        remap=dict(zip(previous,times))
        spec["beats"]=[(t,*b[1:]) for t,b in zip(times,spec["beats"])]
        spec["grounded"]=tuple(remap[t] for t in spec["grounded"])
        if spec["punch"] is not None:spec["punch"]=remap[spec["punch"]]

# Magnet is a retrieval now, not the old held-shoe overcharge. Aim the off hand
# toward the street, draw the receiving arm inward, then release the shoulder.
HEROES["zack"]["hero-zack-charge"] = {
    "punch": .12,
    "beats": [
        (0.,1.,0.,0., 0,0,0, 0,0, 0,0,0,0, 0,0,0,0),
        (.045,1.,0.,0., 12,18,7, -4,14, -6,-4,8,3, -52,22,-66,-8),
        (.12,1.,0.,-.015, -6,-22,-5, 4,-12, 4,3,-6,-2, -68,26,18,-30),
        (.29,1.,0.,-.008, -3,-12,-2, 2,-6, 2,1,-3,-1, -35,16,10,-19),
        (.52,1.,0.,0., 0,0,0, 0,0, 0,0,0,0, 0,0,0,0),
    ],
    "grounded": (0.,.045,.12,.29,.52),
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


def author(path, name, spec, replace=False):
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

    report = append_action(rig, name, times, tracks, root, order=order, replace=replace)
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
    ap.add_argument("--replace", action="store_true", help="replace only these named actions, retaining current geometry")
    ap.add_argument("files", type=Path, nargs="+")
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:]
                         if "--" in sys.argv else sys.argv[1:])

    actions = HEROES[args.hero]
    if args.action:
        actions = {args.action: actions[args.action]}

    for path in args.files:
        for name, spec in actions.items():
            print("HERO_ACTION " + json.dumps(author(path, name, spec, replace=args.replace)))
