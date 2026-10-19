"""The clips of the Payo audience v2 villager `elder_woman`, an Ifugao grandmother in the crowd.

A COPY OF tools/author_character_redesign_lola_pacing_clips.py (the base Classic's), imported by
tools/author_payo_audience_v2_elder_woman.py. Per docs/reports/character-redesign/payo-audience-brief.md:
  * HER OWN: `idle` (8 s, acted: hands folded, a slow nod, shading her eyes), `cheer` (1 s loop,
    small, both hands up, delighted), `clap` (1 s loop, also written in as `emote-yes`), `sit` (a
    4 s loop on a ledge: hands on her lap, the basket beside her), and `walk`, `sprint`, `jump`,
    `fall` rewritten lightly from Lola Pacing's with HER BASKET CARRIED ON HER LEFT HIP.
  * KEPT FROM THE BASE CLASSIC unchanged: the action clips below (holding, throw, pick-up, the two
    melee, the two interacts, slide, crouch, die, emote-no). They come with the rig.

THE BASKET is a separate mesh skinned to `root`, standing at her left foot in the bind pose. So:
  * idle, cheer, clap: the root stays at its rest place and THE BASKET STANDS BY HER LEFT FOOT.
  * sit: THE ROOT STAYS AT ITS REST PLACE TOO and her body comes down to the ledge by translating
    `torso`, `leg-left` and `leg-right` (`SIT_DROP`), so the basket stands on the ledge top (z 0)
    beside her left hip and THE SEAT (the underside of her hips) IS AT z 0 AT THE ROOT'S ORIGIN.
  * walk, sprint, jump, fall: CARRIED ON HER LEFT HIP. The root is shifted by `CARRY` (up and in) and
    `torso` and both legs are shifted back by exactly the same, through the root's own rotation and
    scale (`_carried`), so her body is where the clip puts it and only the basket rises to her hip,
    under her left forearm.
Every clip keys the translations of `torso` and both legs (their rest values where they do not
move), so a clip that shifts them cannot leak into the next.

(What follows is the base Classic's note, kept for the clips copied from it.)

Imported by tools/author_character_redesign_lola_pacing.py, which writes them into the prototype
.glb IN PLACE OF the clips of the same name copied from character-female-d.glb. Nothing in the
game reads them; the original keeps its own. The five locomotion clips come first; the thirteen
ACTION clips (the hold, the throw, the pick up, the tag, the shove, the two reaches, the slide,
crouch, sit, die, yes and no) follow under their own heading near the end. Every other clip in
the .glb (the left and two-handed holds, the kicks, drive, the wheelchair set) is copied across
untouched. A copy of the
cast's clips script rewritten for her: each character's motion is its OWN
(docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so none of the numbers below are anyone
else's.

WHO SHE IS, AND WHAT HER MOTION SHOULD SAY (`ConvertedCharacterSelect`, docs/GAME_OVERVIEW.md):
"Watches from the window most afternoons. On the good ones she comes down to play, and she does
not miss twice." The slowest in the game (BILIS 1) and "immovable with it" (POWER 4, CONTROL 5).
A grandmother who still plays in the street. So:
  * SHE IS OLD IN HOW SHE STANDS, NOT IN HER FACE. A small stoop in the idle and the walk: the
    chest a little forward, the chin lifted to look out from under it, the elbows carried bent.
  * SHE IS SLOW AND SURE. Short steps, the weight rocking from one foot to the other, the head
    held steady over it. Nothing wobbles. When she plants herself she stays planted.
  * SHE IS NOT FRAIL. The finger she wags is quick and exact, the hop is a real hop, and the
    run is a determined hustle with both elbows working.
  * WHAT SHE DOES WHILE WAITING IS WHAT A LOLA DOES: a hand in the small of her back and a
    stretch, fanning herself with her hand in the heat, wagging a finger at whoever is next.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK
and tips an upright one FORWARD; about +z a negative angle drops her left arm from straight out
to hanging.

ARMS UP GO THROUGH THE ELBOW (section 13 rule 8). Her elbow is where the narrow sleeve meets its
bell mouth, 110 mm from the shoulder; the bell, the cuff and the fist ride the forearm bone,
174 mm more. Her ears stand out beside the shoulders, so the V is thrown up and a little AHEAD
of them.

Each clip keeps the LENGTH of the one it replaces (walk 0.72 s, sprint 0.48, jump 0.50,
fall 0.33); the idle is ACTED and is 8 s, not the old 1.33. NOT SEEN IN UNITY: the game draws
walk and run itself (`CharacterAnimator.LocomotionArms`), and the `root` scale and forearm scale
channels are new to a Classic rig.
"""
import math

RATE = 60.0
STOOP = 4.0       # degrees her chest is carried forward when she stands


def _q(axis, deg):
    h = math.radians(deg) * 0.5
    s = math.sin(h)
    return (axis[0] * s, axis[1] * s, axis[2] * s, math.cos(h))


def qx(deg):
    return _q((1, 0, 0), deg)


def qy(deg):
    return _q((0, 1, 0), deg)


def qz(deg):
    return _q((0, 0, 1), deg)


def mul(a, b):
    """a after b."""
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def _snap(s, power):
    """A sine pushed toward a square wave: the limb hurries through the middle and hangs at the ends."""
    return math.copysign(abs(s) ** power, s)


def _squash(sy):
    """Volume kept: what the height loses the width gains."""
    k = 1.0 / math.sqrt(sy)
    return (k, sy, k)


def _times(length):
    n = int(round(length * RATE))
    return [length * i / n for i in range(n + 1)]


def _add(out, t, row):
    for key, value in row.items():
        out.setdefault(key, []).append((t, value))


def _norm(v):
    n = math.sqrt(sum(k * k for k in v)) or 1.0
    return tuple(k / n for k in v)


def _rot(q, v):
    """The vector `v` turned by the quaternion `q`."""
    x, y, z, w = q
    tx, ty, tz = 2.0 * (y * v[2] - z * v[1]), 2.0 * (z * v[0] - x * v[2]), 2.0 * (x * v[1] - y * v[0])
    return (v[0] + w * tx + (y * tz - z * ty), v[1] + w * ty + (z * tx - x * tz), v[2] + w * tz + (x * ty - y * tx))


def _arc(a, b):
    """The shortest turn that takes direction `a` to direction `b`."""
    a, b = _norm(a), _norm(b)
    d = sum(p * q for p, q in zip(a, b))
    if d < -0.9999:
        return (0.0, 0.0, 1.0, 0.0)
    c = (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
    q = (c[0], c[1], c[2], 1.0 + d)
    n = math.sqrt(sum(k * k for k in q))
    return tuple(k / n for k in q)


def _arm(side, upper, fore):
    """An arm posed by where it POINTS: the upper arm along `upper`, the forearm along `fore`.

    Both are directions for her LEFT arm in the file's space (+x her left, +y up, +z ahead);
    `side` -1 mirrors them for the right. Returns (arm rotation, forearm rotation).
    """
    rest = (float(side), 0.0, 0.0)
    upper = (side * upper[0], upper[1], upper[2])
    fore = (side * fore[0], fore[1], fore[2])
    q = _arc(rest, upper)
    inverse = (-q[0], -q[1], -q[2], q[3])
    return q, _arc(rest, _rot(inverse, _norm(fore)))


def _mix(a, b, t):
    """From quaternion `a` to `b`, the short way."""
    if sum(p * q for p, q in zip(a, b)) < 0.0:
        b = tuple(-k for k in b)
    q = tuple(p + (r - p) * t for p, r in zip(a, b))
    n = math.sqrt(sum(k * k for k in q))
    return tuple(k / n for k in q)


def _ease(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def _window(t, start, end, blend):
    """0 outside [start, end], 1 between, eased in and out over `blend` seconds."""
    return _ease((t - start) / blend) * (1.0 - _ease((t - (end - blend)) / blend)) if start <= t <= end else 0.0


def _bump(t, at, width):
    return math.exp(-((t - at) / width) ** 2)


#   STANCES BY WHERE THE ARM POINTS: (upper arm, forearm) for her LEFT arm, in her chest's space
#   (+x her left, +y up, +z ahead); the right is the mirror. Kept from the base Classic for the
#   action clips copied below.
ARMS_HANG = ((0.70, -0.71, 0.04), (0.50, -0.78, 0.38))
ARM_BACK = ((0.78, -0.47, -0.41), (-0.50, -0.30, -0.81))
ARM_HIP = ((0.82, -0.44, -0.36), (0.16, -0.58, 0.80))
IDLE_LENGTH = 8.0

# ---------------------------------------------------------------------------
# HER STANCES BY WHERE THE HAND GOES. `_reach` takes the fist's middle as an offset from the shoulder
# (in her chest's space, x OUT from her side for either arm, y up, z ahead) and the way the upper arm
# points, and returns the arm, the forearm and how far the forearm must reach (the base Classic's
# forearm-scale channel; 1.0 is the fist's middle 0.154 beyond the elbow).
# ---------------------------------------------------------------------------
UPPER_LEN = 0.1098       # shoulder (0.0999) to elbow (0.2097)
FORE_LEN = 0.1540        # elbow to the fist's middle (0.3637)


def _reach(side, hand, elbow):
    e = _norm(elbow)
    f = tuple(h - UPPER_LEN * k for h, k in zip(hand, e))
    length = math.sqrt(sum(k * k for k in f))
    arm, fore = _arm(side, e, f)
    return arm, fore, length / FORE_LEN


#   HANDS FOLDED in front of her belly, the left over the right: where each fist sits from its
#   shoulder, and the elbows carried out a little and back
FOLD_L = ((-0.086, -0.084, 0.190), (0.64, -0.74, 0.20))
FOLD_R = ((-0.090, -0.102, 0.168), (0.64, -0.74, 0.14))
#   SHADING HER EYES: her right hand up in front of her brow on her right side, the elbow up and out
SHADE_R = ((0.004, 0.250, 0.192), (0.42, 0.78, 0.46))
#   while the right hand shades her eyes, the left rests alone across her belly
REST_L = ((-0.060, -0.088, 0.176), (0.64, -0.74, 0.16))


def _hands(row, left, right):
    """Write two (arm, forearm, reach) triples into a key row."""
    for (arm, fore, reach), name in ((left, "left"), (right, "right")):
        row[("arm-" + name, "rotation")] = arm
        row[("forearm-" + name, "rotation")] = fore
        row[("forearm-" + name, "scale")] = (reach, 1.0, 1.0)


def _mix3(a, b, t):
    return (_mix(a[0], b[0], t), _mix(a[1], b[1], t), a[2] + (b[2] - a[2]) * t)


def idle():
    """Eight seconds of a grandmother watching the match from the wall, in three things she does:

      HANDS FOLDED AND A SLOW NOD (0 to 2.4 s). She stands with her hands folded over her belly, the
      left over the right, and gives the play one slow, approving nod, chin down and up.
      SHADING HER EYES (2.6 to 5.6 s). Her right hand comes up flat over her brow against the sun
      and she leans a little in, peering, her head following the play slowly across the court from
      her right to her left; her left hand stays resting on her belly.
      FOLDED AGAIN, CONTENT (5.8 to 8 s). The hand comes down to the other, she settles her weight
      onto her left foot, and gives two small slow nods to herself, smiling.

    WHY THESE. She is the village's grandmother, here for the afternoon with her basket at her feet:
    nothing hurried, nothing loud. The folded hands and the nods are how she approves; the shading
    hand is the terraces' sun. None of it is Lola Pacing's (her back stretch, the fanning, the
    finger). Frame 0 is the folded stand; the loop closes on it. Feet planted throughout.
    """
    out = {}
    fold = {1: _reach(1, *FOLD_L), -1: _reach(-1, *FOLD_R)}
    rest_l = _reach(1, *REST_L)
    shade_r = _reach(-1, *SHADE_R)
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 4.0))
        nod1 = math.sin(math.pi * _ease((t - 0.7) / 1.5)) if 0.7 <= t <= 2.2 else 0.0
        shade = _window(t, 2.6, 5.6, 0.45)
        # her gaze sweeps from her right to her left while she shades her eyes
        sweep = -1.0 + 2.0 * _ease((t - 3.0) / 2.3)
        settle = _window(t, 5.8, 8.0, 0.5) if t < 7.9 else 0.0
        nods = (math.sin(math.pi * _ease((t - 6.2) / 0.7)) if 6.2 <= t <= 6.9 else 0.0) + \
               (math.sin(math.pi * _ease((t - 7.0) / 0.7)) if 7.0 <= t <= 7.7 else 0.0)
        row = {}
        left = _mix3(fold[1], rest_l, shade)
        right = _mix3(fold[-1], shade_r, shade)
        # a little life in the shading hand: it settles once it gets there
        right = (mul(right[0], qz(1.5 * math.sin(2.0 * math.pi * 0.7 * t) * shade)), right[1], right[2])
        _hands(row, left, right)
        chest = STOOP + 3.0 * nod1 + 4.0 * shade + 1.5 * nods + 0.7 * breath
        nod = -STOOP * 0.6 + 11.0 * nod1 + 6.0 * nods - 4.0 * shade
        look = 22.0 * sweep * shade
        tilt = 4.0 * settle + 3.0 * nod1
        lean = -1.0 - 1.4 * settle + 0.6 * shade     # weight on her left foot as she settles
        row.update({
            ("root", "translation"): (0.0, 0.0, 0.0),
            ("root", "scale"): _squash(1.0 + 0.007 * breath - 0.010 * nod1 - 0.006 * nods),
            ("root", "rotation"): qz(lean),
            ("torso", "rotation"): mul(mul(qx(chest), qz(-1.2 * lean)), qy(0.30 * look)),
            ("head", "rotation"): mul(mul(qy(0.70 * look), qx(nod - 0.5 * breath)), qz(tilt - 0.3 * lean)),
            ("leg-left", "rotation"): qz(-2.0 - lean),
            ("leg-right", "rotation"): qz(2.0 - lean),
        })
        _add(out, t, row)
    return out


#   HER BASKET ON HER LEFT HIP in the locomotion: her left hand on its lid, the elbow out and a
#   little back. Its fist offset is from her left shoulder.
CARRY_L = ((0.150, -0.050, 0.040), (0.55, -0.75, -0.37))


def walk():
    """Her walk: short sure steps, a small rock over each foot, the chest a little forward and the
    chin up, the basket on her left hip under her hand, her right hand swinging from a bent elbow."""
    out = {}
    length = 0.72
    carry = _reach(1, *CARRY_L)
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.9)
        c = math.cos(phase)
        lift = abs(s) ** 0.8
        rock = math.sin(phase - 0.45)
        _add(out, t, {
            ("root", "translation"): (0.010 * rock, 0.002 + 0.010 * lift, 0.0),
            ("root", "rotation"): mul(qx(1.5), qz(4.0 * rock)),
            ("root", "scale"): _squash(0.988 + 0.024 * lift),
            ("leg-left", "rotation"): mul(qx(-22.0 * sh), qz(-4.0 * rock - 2.0)),
            ("leg-right", "rotation"): mul(qx(22.0 * sh), qz(-4.0 * rock + 2.0)),
            ("torso", "rotation"): mul(mul(qx(STOOP + 2.0), qy(3.0 * sh)), qz(-2.0 * rock)),
            ("head", "rotation"): mul(mul(qx(-STOOP - 2.0 + 1.0 * c), qy(-2.5 * sh)), qz(-1.8 * rock)),
            ("arm-left", "rotation"): mul(carry[0], qx(1.5 * sh)),
            ("forearm-left", "rotation"): carry[1],
            ("forearm-left", "scale"): (carry[2], 1.0, 1.0),
            ("arm-right", "rotation"): mul(qx(4.0 - 14.0 * sh), qz(50.0)),
            ("forearm-right", "rotation"): mul(qx(-24.0), qy(30.0 - 8.0 * sh)),
            ("forearm-right", "scale"): (1.0, 1.0, 1.0),
        })
    return out


def sprint():
    """Her hurry: quicker short steps, pitched forward, the basket clamped to her hip, her right
    elbow pumping."""
    out = {}
    length = 0.48
    carry = _reach(1, *CARRY_L)
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.7)
        lift = abs(s) ** 0.6
        rock = math.sin(phase - 0.3)
        bob = math.sin(2.0 * phase - 0.6)
        _add(out, t, {
            ("root", "translation"): (0.006 * rock, 0.004 + 0.018 * lift, 0.0),
            ("root", "rotation"): mul(qx(9.0), qz(3.0 * rock)),
            ("root", "scale"): _squash(0.975 + 0.05 * lift),
            ("leg-left", "rotation"): mul(qx(-34.0 * sh), qz(-3.0 * rock - 1.5)),
            ("leg-right", "rotation"): mul(qx(34.0 * sh), qz(-3.0 * rock + 1.5)),
            ("torso", "rotation"): mul(mul(qx(7.0), qy(5.0 * sh)), qz(-2.0 * rock)),
            ("head", "rotation"): mul(mul(qx(-15.0 + 3.0 * bob), qy(-3.0 * sh)), qz(-1.0 * rock)),
            ("arm-left", "rotation"): mul(carry[0], qx(2.5 * sh)),
            ("forearm-left", "rotation"): carry[1],
            ("forearm-left", "scale"): (carry[2], 1.0, 1.0),
            ("arm-right", "rotation"): mul(qx(6.0 - 20.0 * sh), qz(58.0)),
            ("forearm-right", "rotation"): mul(qx(-26.0), qy(62.0 + 10.0 * sh)),
        })
    return out


ARM_UP = ((0.80, 0.30, 0.50), (0.40, 0.84, 0.36))
ARM_UP_REACH = 0.24


def jump():
    """A STANDING jump, both feet together: a small hop, her right arm thrown up through the elbow,
    her left hand keeping the basket on her hip, her head back."""
    out = {}
    up = _arm(-1, *ARM_UP)
    carry = _reach(1, *CARRY_L)
    for t in _times(0.50):
        rise = math.exp(-t / 0.15)
        hang = 1.0 - math.exp(-t / 0.13)
        ring = math.cos(2.0 * math.pi * t / 0.50) * math.exp(-t / 0.20)
        _add(out, t, {
            ("root", "scale"): _squash(1.0 + 0.09 * ring),
            ("root", "rotation"): qx(-2.0 * hang),
            ("leg-left", "rotation"): mul(qx(10.0 * rise - 14.0 * hang), qz(-2.0 + 3.0 * hang)),
            ("leg-right", "rotation"): mul(qx(10.0 * rise - 14.0 * hang), qz(2.0 - 3.0 * hang)),
            ("torso", "rotation"): qx(-6.0 * rise - 2.0 * hang),
            ("head", "rotation"): qx(-14.0 * rise - 3.0 * hang),
            ("arm-left", "rotation"): carry[0],
            ("forearm-left", "rotation"): carry[1],
            ("forearm-left", "scale"): (carry[2], 1.0, 1.0),
            ("arm-right", "rotation"): up[0],
            ("forearm-right", "rotation"): mul(up[1], qz(-8.0 * ring)),
            ("forearm-right", "scale"): (1.0 + ARM_UP_REACH * (0.7 + 0.3 * rise), 1.0, 1.0),
        })
    return out


def fall():
    """A loop: coming down upright, her right arm up and steadying her, the basket held to her hip,
    her feet reaching for the ground one and then the other."""
    out = {}
    length = 1.0 / 3.0
    up = _arm(-1, *ARM_UP)
    carry = _reach(1, *CARRY_L)
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        _add(out, t, {
            ("root", "scale"): _squash(1.03 + 0.010 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(2.0), qz(1.5 * s)),
            ("leg-left", "rotation"): mul(qx(-8.0 + 6.0 * s), qz(-3.0)),
            ("leg-right", "rotation"): mul(qx(-8.0 - 6.0 * s), qz(3.0)),
            ("torso", "rotation"): mul(qx(5.0), qz(-1.2 * s)),
            ("head", "rotation"): mul(qx(11.0 + 1.0 * c), qz(-0.8 * s)),
            ("arm-left", "rotation"): carry[0],
            ("forearm-left", "rotation"): carry[1],
            ("forearm-left", "scale"): (carry[2], 1.0, 1.0),
            ("arm-right", "rotation"): mul(up[0], qz(5.0 * s)),
            ("forearm-right", "rotation"): mul(up[1], qz(4.0 * c)),
            ("forearm-right", "scale"): (1.0 + ARM_UP_REACH * 0.65, 1.0, 1.0),
        })
    return out



# ---------------------------------------------------------------------------
# THE ACTION CLIPS (2026-10-07). Thirteen clips that were copies from the old shared rig, which
# has no elbow, redone as HER doing them. Each keeps the LENGTH of the clip it replaces and the
# TIME of its key beat, both read out of character-female-d.glb before anything was replaced:
#
#   clip                  length   the old key beat                          hers
#   holding-right         0.1667   a still pose                              2.0 s loop (see below)
#   holding-right-shoot   0.2000   arm at its extreme at 0.067               slipper leaves at 0.067
#   pick-up               0.3333   hand lowest at 0.167                      hand lowest at 0.167
#   attack-melee-right    0.4167   hand furthest out at 0.250                hand furthest out at 0.250
#   attack-melee-left     0.4167   hand furthest out at 0.250                forearm furthest out at 0.250
#   interact-right/left   0.6667   hand out by 0.183, held to 0.45           the same
#   slide                 0.9500   lowest from 0.25 to 0.37                  down at 0.25, deepest 0.37
#   crouch                0.1667   a still pose                              1.8 s, two breaths
#   sit                   0.1667   a still pose                              0.8 s of getting down
#   die                   0.3333   top of the arc 0.083, down by 0.283       the same
#   emote-yes / emote-no  0.6667   loops                                     loops of 1.0 and 1.2 s
#
# FIVE ARE LONGER THAN THE CLIP THEY REPLACE (`holding-right`, `crouch`, `sit`, `emote-yes`,
# `emote-no`): the game only looks at those and times nothing against them, and a sixth of a
# second leaves no room to move. The other eight keep their length and their beat exactly.
#
# THE THROW AND THE TAG ARE POSED OVER BY THE GAME (`CharacterAnimator.ThrowBody`, `.TagBody`):
# her chest, her head and both UPPER arms are overwritten while they play, and what is kept of
# the clip is the root, the legs and the two FOREARMS. So in those two, and in the carry the
# throw starts from, who she is goes into the root and the legs (the squash, the turn of her
# feet, the step in), and each forearm is a plain HINGE at the elbow (`"hinge"` below): a
# moderate bend that opens to straight at the beat, with no twist in it, so it still looks
# right on an upper arm the game has pointed somewhere else.
#
# HOW THEY ARE WRITTEN. A clip is a handful of whole-body POSES at times (`_body`), and `_play`
# samples between them at 60 a second. An arm in a pose is given by where it POINTS IN THE WORLD
# (+x OUT from her side, whichever arm it is; +y up; +z ahead), whatever her chest and her root
# are doing under it, so "the hand goes to the ground" stays true when she bends. A stance from
# the table above (`ARM_BACK`, `ARM_HIP`) is given as it is, in her chest's own space, by
# passing a third item.
# ---------------------------------------------------------------------------
HIP = (0.0, 0.17625, -0.02875)      # where her legs and chest join the root, from the file


def _ypr(pitch, yaw, roll):
    """Turned `yaw` to her left, tipped `pitch` forward, leaned `roll` to her right; degrees."""
    return mul(mul(qy(yaw), qx(pitch)), qz(roll))


def _conj(q):
    return (-q[0], -q[1], -q[2], q[3])


def _limb(side, spec, frame):
    """(arm rotation, forearm rotation) for one arm of a pose. See the note above."""
    if len(spec) == 3:
        return _arm(side, spec[0], spec[1])
    inverse = _conj(frame)
    if spec[0] == "hinge":
        # ("hinge", where the upper arm points in the world, bend, lift): the forearm is only a
        # hinge, `bend` degrees forward at the elbow and `lift` degrees up
        upper = _rot(inverse, (side * spec[1][0], spec[1][1], spec[1][2]))
        return _arc((float(side), 0.0, 0.0), upper), mul(qy(-side * spec[2]), qz(side * spec[3]))
    upper = _rot(inverse, (side * spec[0][0], spec[0][1], spec[0][2]))
    fore = _rot(inverse, (side * spec[1][0], spec[1][1], spec[1][2]))
    rest = (float(side), 0.0, 0.0)
    q = _arc(rest, upper)
    return q, _arc(rest, _rot(_conj(q), _norm(fore)))


def _body(at=(0.0, 0.0, 0.0), hip=None, root=(0.0, 0.0, 0.0), sy=1.0, torso=(STOOP, 0.0, 0.0),
          head=(-4.0, 0.0, 0.0), legl=(0.0, -2.0), legr=(0.0, 2.0), left=None, right=None, reach=(1.0, 1.0)):
    """One whole-body pose. `root`, `torso` and `head` are (pitch, yaw, roll); a leg is (pitch,
    roll), pitch negative swinging it forward. `hip`, when given, is where her hip joint is to
    be in the world, and the root is put wherever that needs (she sits and lies by it)."""
    rq, tq = _ypr(*root), _ypr(*torso)
    scale = _squash(sy)
    if hip is not None:
        offset = _rot(rq, (0.0, HIP[1] * scale[1], HIP[2] * scale[2]))
        at = tuple(h - o for h, o in zip(hip, offset))
    frame = mul(rq, tq)
    la = _limb(1, left or (ARMS_HANG[0], ARMS_HANG[1], "chest"), frame)
    ra = _limb(-1, right or (ARMS_HANG[0], ARMS_HANG[1], "chest"), frame)
    return {
        ("root", "translation"): tuple(at), ("root", "rotation"): rq, ("root", "scale"): scale,
        ("torso", "rotation"): tq, ("head", "rotation"): _ypr(*head),
        ("leg-left", "rotation"): mul(qx(legl[0]), qz(legl[1])),
        ("leg-right", "rotation"): mul(qx(legr[0]), qz(legr[1])),
        ("arm-left", "rotation"): la[0], ("forearm-left", "rotation"): la[1],
        ("arm-right", "rotation"): ra[0], ("forearm-right", "rotation"): ra[1],
        ("forearm-left", "scale"): (reach[0], 1.0, 1.0), ("forearm-right", "scale"): (reach[1], 1.0, 1.0),
    }


_EASE = {
    "s": _ease,                                   # leaves gently, arrives gently
    "in": lambda u: u * u,                        # gathers speed: arrives at full pace (a strike)
    "out": lambda u: 1.0 - (1.0 - u) * (1.0 - u),  # leaves at full pace and settles
    "lin": lambda u: u,
}


def _blend(a, b, u):
    row = {}
    for key, value in a.items():
        other = b[key]
        row[key] = _mix(value, other, u) if key[1] == "rotation" else tuple(p + (q - p) * u for p, q in zip(value, other))
    return row


def _play(length, keys):
    """Sixty poses a second between the `keys`: (time, pose, how she ARRIVES at it)."""
    out = {}
    for t in _times(length):
        row = keys[-1][1]
        for (t0, a, _), (t1, b, how) in zip(keys, keys[1:]):
            if t <= t1 + 1e-9:
                row = _blend(a, b, _EASE[how](max(0.0, min(1.0, (t - t0) / (t1 - t0)))))
                break
        _add(out, t, row)
    return out


_STAND = None


def _stand():
    """The neutral stand: frame 0 of her idle, exactly, so a one-shot blends in and out of it."""
    global _STAND
    if _STAND is None:
        _STAND = {key: keys[0][1] for key, keys in idle().items()}
    return dict(_STAND)


ON_BACK = (ARM_BACK[0], ARM_BACK[1], "chest")
ON_HIP = (ARM_HIP[0], ARM_HIP[1], "chest")
CARRY_LENGTH = 2.0


def _carry(t):
    """Carrying the slipper, at `t` seconds into the loop. See `holding_right`."""
    breath = math.sin(2.0 * math.pi * t / 1.0)
    shift = math.sin(2.0 * math.pi * t / CARRY_LENGTH)            # her weight, to one foot and back
    swing = math.sin(2.0 * math.pi * 2.0 * t / CARRY_LENGTH + 0.6)  # the slipper, a small pendulum
    tap = _bump(t, 1.18, 0.050) + _bump(t, 1.40, 0.050)           # two flicks of it, late in the loop
    look = _window(t, 0.95, 1.70, 0.25)                           # she looks down at it as she does
    roll = 2.4 * shift
    return _body(at=(0.010 * shift, 0.0, 0.0), root=(0.0, 0.0, roll), sy=1.0 + 0.010 * breath - 0.020 * tap,
                 torso=(STOOP + 0.8 * breath, -5.0 + 2.0 * shift, -0.6 * roll),
                 head=(-5.0 - 0.5 * breath + 7.0 * look, 4.0 - 3.0 * shift - 14.0 * look, -0.4 * roll),
                 legl=(0.0, -2.0 - roll), legr=(0.0, 2.0 - roll),
                 left=("hinge", (0.56, -0.82, 0.06), 40.0 - 3.0 * breath, 0.0),
                 right=("hinge", (0.86, -0.50 + 0.03 * breath, 0.08 + 0.05 * swing), 22.0 + 8.0 * swing + 16.0 * tap, 32.0 - 22.0 * tap),
                 reach=(1.0, 1.10 + 0.12 * tap))


def _holding():
    return _carry(0.0)


def holding_right():
    """Carrying the slipper, a 2 s loop. It is HELD OUT WIDE: her right elbow out from her side
    and the forearm level beyond it, the slipper out at the height of her chest and well clear
    of her skirt, where it can be seen from behind and where the child can see it, which is
    most of what a slipper is for. Her left arm hangs close with its elbow bent. She breathes, her weight goes
    over to one foot and comes back, the slipper swings a little from the elbow, and late in
    the loop she looks down at it and flicks it twice, testing it."""
    out = {}
    for t in _times(CARRY_LENGTH):
        _add(out, t, _carry(t if t < CARRY_LENGTH - 1e-9 else 0.0))
    return out


def holding_right_shoot():
    """THE THROW. Sixty years of it: no run-up and no overarm, a SIDEARM FLICK from where she
    carries it. Her feet turn away and she sinks (0.033), then her whole planted body turns in
    and steps a foot through with the root stretching, the forearm opening dead straight as the
    slipper leaves at 0.067; it carries across her (0.10) and she is back in the carry before
    the first one has landed. What the game keeps of this is the root, the legs and the
    forearms, so that is where the throw is."""
    hold = _holding()
    spare = ("hinge", (0.60, -0.50, 0.62), 40.0, 0.0)
    thrown = ("hinge", (0.74, -0.46, -0.50), 30.0, 0.0)
    back = _body(at=(0.0, 0.0, -0.018), root=(-2.0, -13.0, 0.0), sy=0.91, torso=(0.0, -14.0, 0.0), head=(-3.0, 20.0, 0.0),
                 legl=(-4.0, -2.0), legr=(5.0, 2.0), left=spare,
                 right=("hinge", (0.62, -0.50, -0.60), 66.0, 10.0))
    out = _body(at=(0.0, 0.014, 0.036), root=(6.0, 12.0, 0.0), sy=1.08, torso=(12.0, 16.0, 0.0), head=(-14.0, -22.0, 0.0),
                legl=(8.0, -2.0), legr=(-9.0, 2.0), left=thrown,
                right=("hinge", (0.22, -0.06, 0.97), 4.0, 0.0), reach=(1.0, 1.42))
    through = _body(at=(0.0, 0.012, 0.028), root=(5.0, 16.0, 0.0), sy=0.96, torso=(18.0, 20.0, 0.0), head=(-16.0, -28.0, 0.0),
                    legl=(6.0, -2.0), legr=(-7.0, 2.0), left=thrown,
                    right=("hinge", (0.0, -0.55, 0.83), 18.0, 0.0), reach=(1.0, 1.15))
    return _play(0.2, [(0.0, hold, "lin"), (2 / 60.0, back, "out"), (4 / 60.0, out, "in"),
                       (6 / 60.0, through, "out"), (0.2, hold, "s")])


def pick_up():
    """She does not bend her back for anybody. Her left hand goes to the small of it, she tips
    over from the hips all in one piece with her seat going back, and the right hand goes
    straight down to the ground and takes the slipper (0.167). Then up, past upright for a
    moment with the hand carried in front of her, and into her stand."""
    low = _body(at=(0.0, 0.0, -0.035), sy=0.84, torso=(64.0, 12.0, 0.0), head=(-44.0, -8.0, 0.0),
                legl=(-9.0, -5.0), legr=(-9.0, 5.0), left=ON_BACK,
                right=((0.16, -0.52, 0.84), (0.02, -0.80, 0.60)), reach=(1.10, 1.0))
    up = _body(sy=1.04, torso=(-3.0, 0.0, 0.0), head=(-8.0, 0.0, 0.0), left=ON_BACK,
               right=((0.55, -0.55, 0.62), (0.12, 0.30, 0.95)), reach=(1.08, 1.0))
    return _play(1.0 / 3.0, [(0.0, _stand(), "lin"), (10 / 60.0, low, "s"), (12 / 60.0, low, "lin"),
                             (16 / 60.0, up, "out"), (1.0 / 3.0, _stand(), "s")])


def attack_melee_right():
    """THE TAG. She does not chase: she plants herself, sinks and turns her feet away with the
    elbow drawn back to her ribs and her other hand up in front as a marker (0.13), and then
    all of her goes in behind one straight exact hand at chest height (0.25), the way it takes
    an ear. Held a moment so it can be seen, then home. The legs do little and the forearms are
    plain hinges: the game lays its own chest, head and upper arms over this."""
    wind = _body(at=(0.0, 0.0, -0.022), root=(-2.0, -9.0, 0.0), sy=0.90, torso=(1.0, -13.0, 0.0), head=(-5.0, 18.0, 0.0),
                 legl=(-3.0, -2.0), legr=(4.0, 2.0),
                 left=("hinge", (0.55, -0.40, 0.73), 46.0, 18.0),
                 right=("hinge", (0.62, -0.42, -0.66), 62.0, 6.0))
    out = _body(at=(0.0, 0.016, 0.046), root=(7.0, 10.0, 0.0), sy=1.08, torso=(11.0, 18.0, 0.0), head=(-15.0, -24.0, 0.0),
                legl=(8.0, -2.0), legr=(-9.0, 2.0), left=("hinge", (0.72, -0.40, -0.57), 34.0, 0.0),
                right=("hinge", (0.16, 0.02, 0.99), 3.0, 0.0), reach=(1.0, 1.42))
    held = _body(at=(0.0, 0.012, 0.038), root=(5.0, 8.0, 0.0), sy=1.0, torso=(10.0, 16.0, 0.0), head=(-13.0, -20.0, 0.0),
                 legl=(7.0, -2.0), legr=(-8.0, 2.0), left=("hinge", (0.72, -0.40, -0.57), 34.0, 0.0),
                 right=("hinge", (0.18, 0.0, 0.98), 10.0, 4.0), reach=(1.0, 1.22))
    return _play(5.0 / 12.0, [(0.0, _stand(), "lin"), (8 / 60.0, wind, "s"), (15 / 60.0, out, "in"),
                              (18 / 60.0, held, "out"), (5.0 / 12.0, _stand(), "s")])


def attack_melee_left():
    """THE SHOVE. Not a push with the hand: she is the hardest thing on the court to move and
    she shoves with all of it. She sinks and turns her left side away (0.13), then walks her
    whole weight in behind her left forearm, carried across her like a bar with the elbow
    leading (0.25). The right arm goes back to keep her on her feet."""
    wind = _body(at=(0.0, 0.0, -0.016), sy=0.89, torso=(9.0, 18.0, 0.0), head=(-8.0, -12.0, 0.0),
                 left=((0.80, -0.30, -0.52), (-0.40, 0.28, 0.87)), right=ON_HIP)
    out = _body(at=(0.0, 0.0, 0.050), root=(9.0, 0.0, 0.0), sy=1.05, torso=(11.0, -26.0, 0.0), head=(-16.0, 18.0, 0.0),
                legl=(-9.0, -2.0), legr=(11.0, 2.0),
                left=((0.52, -0.16, 0.84), (-0.84, -0.06, 0.54)), reach=(1.22, 1.0),
                right=((0.70, -0.48, -0.53), (0.52, -0.66, -0.54)))
    held = _body(at=(0.0, 0.0, 0.042), root=(7.0, 0.0, 0.0), sy=0.99, torso=(10.0, -22.0, 0.0), head=(-14.0, 15.0, 0.0),
                 legl=(-8.0, -2.0), legr=(9.0, 2.0),
                 left=((0.54, -0.18, 0.82), (-0.80, -0.08, 0.60)), reach=(1.10, 1.0),
                 right=((0.70, -0.48, -0.53), (0.52, -0.66, -0.54)))
    return _play(5.0 / 12.0, [(0.0, _stand(), "lin"), (8 / 60.0, wind, "s"), (15 / 60.0, out, "in"),
                              (18 / 60.0, held, "out"), (5.0 / 12.0, _stand(), "s")])


def _interact(side):
    """One hand out to something in front of her. RIGHT: the hand goes out at chest height from
    a bent elbow, her other hand at her back, and presses once, the way she tries a door.
    LEFT: the hand goes up in front of her with the other on her hip, and comes down twice in
    two big pats, the way she pats a head."""
    def pose(upper, fore, reach, lean):
        turn = 12.0 * -side      # the reaching shoulder comes forward
        arm = (upper, fore)
        rest = ON_BACK if side < 0 else ON_HIP
        return _body(sy=1.0 - 0.004 * lean, torso=(STOOP + lean, turn, 0.0), head=(-5.0 - 0.6 * lean, -0.6 * turn, 0.0),
                     left=rest if side < 0 else arm, right=arm if side < 0 else rest,
                     reach=(1.0, reach) if side < 0 else (reach, 1.0))
    if side < 0:
        over = pose((0.46, -0.30, 0.83), (0.06, 0.34, 0.94), 1.28, 9.0)
        rest = pose((0.48, -0.36, 0.80), (0.10, 0.26, 0.96), 1.10, 7.0)
        press = pose((0.44, -0.30, 0.85), (0.04, 0.02, 1.0), 1.30, 11.0)
        keys = [(0.0, _stand(), "lin"), (10 / 60.0, over, "out"), (14 / 60.0, rest, "s"), (19 / 60.0, press, "in"),
                (23 / 60.0, rest, "out"), (27 / 60.0, rest, "lin"), (2.0 / 3.0, _stand(), "s")]
    else:
        #   the pat is a whole forearm, from standing up in front of her to below level, and
        #   her chest dips with it: it has to read from the side and from behind
        over = pose((0.36, -0.04, 0.93), (0.08, 0.82, 0.57), 1.28, 5.0)
        rest = pose((0.36, -0.08, 0.93), (0.08, 0.74, 0.67), 1.16, 4.0)
        pat = pose((0.36, -0.20, 0.91), (0.05, -0.34, 0.94), 1.32, 14.0)
        keys = [(0.0, _stand(), "lin"), (10 / 60.0, over, "out"), (15 / 60.0, pat, "in"),
                (20 / 60.0, rest, "out"), (25 / 60.0, pat, "in"), (29 / 60.0, rest, "out"),
                (2.0 / 3.0, _stand(), "s")]
    return _play(2.0 / 3.0, keys)


def interact_right():
    return _interact(-1)


def interact_left():
    return _interact(1)


def slide():
    """She does not dive. She gathers her skirt (0.10), sits down onto the street and goes in
    FEET FIRST on her seat, leaning back on her left hand with the right one out low for the
    slipper (down by 0.25, deepest at 0.37). Then she rocks forward onto her feet, pushes up
    off her knees, and has to straighten her back out with a hand on it before she is standing
    again."""
    gather = _body(sy=0.87, torso=(16.0, 0.0, 0.0), head=(-12.0, 0.0, 0.0), legl=(0.0, -5.0), legr=(0.0, 5.0),
                   left=((0.72, -0.56, -0.40), (0.30, -0.60, 0.74)), right=((0.72, -0.56, -0.40), (0.30, -0.60, 0.74)))
    down = _body(hip=(0.0, 0.112, -0.03), root=(-28.0, 0.0, 0.0), sy=1.04, torso=(14.0, 0.0, 0.0), head=(10.0, 0.0, 0.0),
                 legl=(-58.0, -7.0), legr=(-70.0, 7.0),
                 left=((0.70, -0.44, -0.56), (0.34, -0.62, -0.71)), reach=(1.0, 1.30),
                 right=((0.34, -0.22, 0.91), (0.08, -0.10, 0.99)))
    deep = _body(hip=(0.0, 0.104, -0.03), root=(-34.0, 0.0, 0.0), sy=1.0, torso=(18.0, 8.0, 0.0), head=(12.0, -6.0, 0.0),
                 legl=(-54.0, -8.0), legr=(-66.0, 8.0),
                 left=((0.70, -0.44, -0.56), (0.34, -0.62, -0.71)), reach=(1.0, 1.42),
                 right=((0.26, -0.30, 0.92), (0.02, -0.22, 0.98)))
    rock = _body(hip=(0.0, 0.150, 0.0), root=(4.0, 0.0, 0.0), sy=0.90, torso=(34.0, 0.0, 0.0), head=(-20.0, 0.0, 0.0),
                 legl=(-46.0, -9.0), legr=(-46.0, 9.0),
                 left=((0.72, -0.22, 0.66), (0.10, -0.40, 0.91)), right=((0.72, -0.22, 0.66), (0.10, -0.40, 0.91)))
    push = _body(at=(0.0, 0.008, 0.0), sy=0.86, torso=(40.0, 0.0, 0.0), head=(-22.0, 0.0, 0.0), legl=(0.0, -12.0), legr=(0.0, 12.0),
                 left=((0.90, 0.05, 0.10), (-0.20, -0.96, -0.10)), right=((0.90, 0.05, 0.10), (-0.20, -0.96, -0.10)))
    back = _body(at=(0.0, 0.004, 0.0), sy=1.04, torso=(-6.0, 0.0, 0.0), head=(-12.0, 0.0, 0.0), left=ON_BACK, right=ON_BACK,
                 reach=(1.10, 1.10))
    return _play(0.95, [(0.0, _stand(), "lin"), (6 / 60.0, gather, "s"), (15 / 60.0, down, "in"), (22 / 60.0, deep, "out"),
                        (27 / 60.0, deep, "lin"), (35 / 60.0, rock, "s"), (41 / 60.0, push, "s"), (49 / 60.0, back, "s"),
                        (0.95, _stand(), "s")])


def _winded(sy, pitch, nod, left=None):
    return _body(at=(0.0, 0.009, 0.0), sy=sy, torso=(pitch, 0.0, 0.0), head=(nod, 0.0, 0.0),
                 legl=(0.0, -13.0), legr=(0.0, 13.0),
                 left=left or ((0.92, 0.10, 0.04), (-0.22, -0.96, -0.10)), right=((0.92, 0.10, 0.04), (-0.22, -0.96, -0.10)))


CROUCH_LENGTH = 1.8


def crouch():
    """Out of breath, 1.8 s: her feet apart, bent over with a hand braced on each knee and her
    elbows out, head hanging. Two breaths, each a quick heave up and a long sag back down. On
    the second, bigger one her left hand leaves her knee and goes to the small of her back as
    she comes up, and is back on the knee as she sags. The first and last frames are the full
    bent pose: the game loops this while she is tired and holds its last frame as an emote."""
    out = {}
    period = CROUCH_LENGTH / 2.0
    for t in _times(CROUCH_LENGTH):
        p = (t % period) / period if t < CROUCH_LENGTH - 1e-9 else 0.0
        lift = _ease(p / 0.34) * (1.0 - _ease((p - 0.34) / 0.66))
        lift *= 1.0 if t < period else 1.45
        back = _window(t, 0.98, 1.66, 0.24)
        args = (0.88 + 0.065 * lift, 42.0 - 10.0 * lift, -17.0 + 10.0 * lift)
        row = _blend(_winded(*args), _winded(*args, left=ON_BACK), back)
        _add(out, t, row)
    return out


def die():
    """Knocked down, and it is a comedy: both feet leave the street with her arms thrown up
    (the top is at 0.083), she comes down flat on her back (0.25) and bounces once, and she
    stays there with her legs stuck up in the air, one arm flung out and the other forearm
    still standing straight up."""
    flat_l = ((0.92, 0.04, -0.38), (0.78, 0.06, -0.62))
    up = _body(at=(0.0, 0.11, -0.04), root=(-24.0, 0.0, 0.0), sy=1.12, torso=(-6.0, 0.0, 0.0), head=(-16.0, 0.0, 0.0),
               legl=(-30.0, -6.0), legr=(-42.0, 6.0),
               left=((0.84, 0.30, 0.45), (0.46, 0.84, 0.28)), right=((0.84, 0.30, 0.45), (0.46, 0.84, 0.28)), reach=(1.3, 1.3))
    hit = _body(at=(0.0, 0.150, 0.0), root=(-90.0, 0.0, 0.0), sy=0.92, torso=(4.0, 0.0, 0.0), head=(20.0, 0.0, 0.0),
                legl=(-84.0, -8.0), legr=(-72.0, 8.0),
                left=flat_l, right=((0.92, 0.10, -0.36), (0.50, 0.60, -0.62)), reach=(1.15, 1.15))
    bounce = _body(at=(0.0, 0.185, 0.0), root=(-88.0, 0.0, 0.0), sy=1.03, torso=(2.0, 0.0, 0.0), head=(14.0, 0.0, 0.0),
                   legl=(-70.0, -8.0), legr=(-88.0, 8.0),
                   left=flat_l, right=((0.92, 0.14, -0.34), (0.20, 0.95, -0.24)), reach=(1.0, 1.25))
    rest = _body(at=(0.0, 0.156, 0.0), root=(-90.0, 0.0, 0.0), sy=1.0, torso=(3.0, 0.0, 0.0), head=(17.0, 0.0, 5.0),
                 legl=(-48.0, -9.0), legr=(-66.0, 9.0),
                 left=flat_l, right=((0.92, 0.10, -0.36), (0.06, 0.99, -0.10)), reach=(1.0, 1.10))
    return _play(1.0 / 3.0, [(0.0, _stand(), "lin"), (5 / 60.0, up, "out"), (15 / 60.0, hit, "in"),
                             (17 / 60.0, bounce, "out"), (1.0 / 3.0, rest, "s")])


def emote_no():
    """NO is the finger, a 1.2 s loop. Her left hand on her hip, her right arm up and out
    beside her with the forearm standing, and the whole forearm throws from side to side three
    times, from upright beside her ear to nearly flat out, quick and exact with a short hang at
    each end. Her chest leans in and rocks with it and her head shakes after it."""
    out = {}
    length = 1.2
    for t in _times(length):
        phase = 2.0 * math.pi * 3.0 * t / length
        wag = _snap(math.sin(phase), 0.5)
        late = _snap(math.sin(phase - 0.8), 0.7)       # her head follows the finger
        fore = (0.60 - 0.56 * wag, 0.80 + 0.22 * wag, 0.24)
        row = _body(at=(-0.006 * wag, 0.0, 0.0), root=(0.0, 0.0, -1.5 + 3.0 * wag), sy=1.0 - 0.02 * abs(wag),
                    torso=(STOOP + 8.0, 8.0 - 5.0 * wag, -2.0 * wag), head=(-9.0, -8.0 + 17.0 * late, 0.0),
                    legl=(0.0, -0.5 - 3.0 * wag), legr=(0.0, 3.5 - 3.0 * wag),
                    left=ON_HIP, right=((0.84, 0.02 + 0.10 * wag, 0.54), fore), reach=(1.0, 1.32))
        _add(out, t, row)
    return out



# ---------------------------------------------------------------------------
# HER SIT, HER CLAP, HER CHEER, AND THE BASKET'S TWO PLACES
# ---------------------------------------------------------------------------
REST_T = {"torso": (0.0, 0.17625, -0.02875), "leg-left": (0.0835709646, 0.17625, -0.02875),
          "leg-right": (-0.08229596, 0.17625, -0.02875)}       # the rig's own, read from character-female-d.glb
#   THE SIT. Her body comes down by SIT_DROP (and back by SIT_BACK) with the root left at its rest
#   place, so the underside of her hips lands on z 0 at the root's origin (measured on the posed
#   mesh by the render script's `seat` shot). Her legs are one bone each (the rig has no knee), so
#   they hang forward and down over the ledge's edge at SIT_LEGS degrees.
SIT_DROP = 0.101
SIT_BACK = 0.0
SIT_LEGS = -56.0
SIT_LENGTH = 4.0
#   her fists resting on her thighs, a little in from her knees, the elbows out at her sides
LAP_L = ((-0.024, -0.060, 0.104), (0.70, -0.68, -0.10))
LAP_R = ((-0.030, -0.062, 0.100), (0.70, -0.68, -0.10))


def sit():
    """SEATED ON A LEDGE as a spectator, a 4 s loop: her hips on the ledge top, her legs hanging
    forward over its edge, her hands resting on her lap, her basket on the ledge at her left side.
    She breathes, her head tips to the play and back, and once in the loop she gives it a slow
    nod. Frame 0 and the last frame are the same pose."""
    out = {}
    lap = {1: _reach(1, *LAP_L), -1: _reach(-1, *LAP_R)}
    drop = (0.0, -SIT_DROP, -SIT_BACK)
    for t in _times(SIT_LENGTH):
        breath = math.sin(2.0 * math.pi * t / 2.0)
        look = math.sin(2.0 * math.pi * t / SIT_LENGTH)
        nod = math.sin(math.pi * _ease((t - 1.6) / 1.2)) if 1.6 <= t <= 2.8 else 0.0
        row = {}
        _hands(row, (lap[1][0], lap[1][1], lap[1][2]), (lap[-1][0], lap[-1][1], lap[-1][2]))
        row.update({
            ("root", "translation"): (0.0, 0.0, 0.0),
            ("root", "rotation"): (0.0, 0.0, 0.0, 1.0),
            ("root", "scale"): (1.0, 1.0, 1.0),
            ("torso", "translation"): tuple(a + b for a, b in zip(REST_T["torso"], drop)),
            ("leg-left", "translation"): tuple(a + b for a, b in zip(REST_T["leg-left"], drop)),
            ("leg-right", "translation"): tuple(a + b for a, b in zip(REST_T["leg-right"], drop)),
            ("torso", "rotation"): mul(qx(STOOP + 1.0 + 0.8 * breath + 3.0 * nod), qy(5.0 * look)),
            ("head", "rotation"): mul(mul(qy(12.0 * look), qx(-3.0 + 10.0 * nod - 0.6 * breath)), qz(-2.0 * look)),
            ("leg-left", "rotation"): mul(qx(SIT_LEGS + 1.5 * look), qz(-4.0)),
            ("leg-right", "rotation"): mul(qx(SIT_LEGS - 1.5 * look), qz(4.0)),
        })
        _add(out, t, row)
    return out


#   THE CLAP: where each fist goes from its shoulder, apart and met in front of her chest
CLAP_OPEN = ((0.028, 0.040, 0.205), (0.70, -0.42, 0.58))
CLAP_SHUT = ((-0.050, 0.022, 0.226), (0.56, -0.55, 0.62))


def clap():
    """Applause, a 1 s loop: her hands meet in front of her chest twice, firm and small, and she
    nods into each clap with a little dip of the knees (a squash on the root). She stays planted and
    square to the play, smiling into it; Lola Pacing's sways from foot to foot instead."""
    def pose(shut, dip):
        hand = CLAP_SHUT if shut else CLAP_OPEN
        a = _reach(1, *hand)
        b = _reach(-1, *hand)
        row = {}
        _hands(row, a, b)
        row.update({
            ("root", "translation"): (0.0, 0.0, 0.0), ("root", "rotation"): (0.0, 0.0, 0.0, 1.0),
            ("root", "scale"): _squash(1.0 - 0.05 * dip),
            ("torso", "rotation"): qx(STOOP + 6.0 * dip - 2.0 * (1.0 - dip)),
            ("head", "rotation"): qx(-6.0 + 14.0 * dip),
            ("leg-left", "rotation"): qz(-2.0), ("leg-right", "rotation"): qz(2.0),
        })
        return row
    return _play(1.0, [(0.0, pose(False, 0.0), "lin"), (9 / 60.0, pose(True, 1.0), "in"), (13 / 60.0, pose(True, 0.7), "lin"),
                       (30 / 60.0, pose(False, 0.0), "out"), (39 / 60.0, pose(True, 1.0), "in"), (43 / 60.0, pose(True, 0.7), "lin"),
                       (1.0, pose(False, 0.0), "out")])


#   THE CHEER: both hands up in a small V through the elbows, not flung (she is a grandmother)
CHEER_UP = ((0.74, 0.40, 0.46), (0.28, 0.92, 0.28))


def cheer():
    """Delighted, a 1 s loop: both hands up in a small V through the elbows, the fists shaking a
    little with joy, two small bounces on her toes (the root lifts 10 mm with a squash and stretch),
    her head back and her chest lifted."""
    out = {}
    up = {1: _arm(1, *CHEER_UP), -1: _arm(-1, *CHEER_UP)}
    for t in _times(1.0):
        bounce = abs(math.sin(2.0 * math.pi * t))         # two a second
        shake = math.sin(2.0 * math.pi * 4.0 * t)
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(side * 3.0 * bounce))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 7.0 * shake))
            row[("forearm-" + name, "scale")] = (1.12 + 0.04 * bounce, 1.0, 1.0)
        row.update({
            ("root", "translation"): (0.0, 0.010 * bounce, 0.0),
            ("root", "rotation"): qz(1.5 * math.sin(2.0 * math.pi * t)),
            ("root", "scale"): _squash(0.96 + 0.07 * bounce),
            ("torso", "rotation"): qx(-3.0 - 2.0 * bounce),
            ("head", "rotation"): mul(qx(-13.0 - 4.0 * bounce), qz(3.0 * math.sin(2.0 * math.pi * t))),
            ("leg-left", "rotation"): qz(-2.5), ("leg-right", "rotation"): qz(2.5),
        })
        _add(out, t, row)
    return out


#   THE CARRY: the root goes up and in by this (glTF space: +x her left, +y up), so the basket, which
#   stands at her left foot in the bind pose, rises to her left hip
CARRY = (-0.070, 0.072, 0.0)


def _carried(clip):
    """Lift the basket to her hip without moving her: shift the root by CARRY, and shift `torso` and
    both legs back by the same through the root's own rotation and scale at that frame, so every
    joint of her body stays exactly where the clip put it."""
    def run():
        out = clip()
        rot = dict(out[("root", "rotation")]) if ("root", "rotation") in out else {}
        scl = dict(out[("root", "scale")]) if ("root", "scale") in out else {}
        loc = dict(out[("root", "translation")]) if ("root", "translation") in out else {}
        times = [t for t, _ in out[("torso", "rotation")]]
        res = {k: v for k, v in out.items() if k != ("root", "translation")}
        res[("root", "translation")] = []
        for bone in REST_T:
            res[(bone, "translation")] = []
        for t in times:
            q = rot.get(t, (0.0, 0.0, 0.0, 1.0))
            s = scl.get(t, (1.0, 1.0, 1.0))
            base = loc.get(t, (0.0, 0.0, 0.0))
            res[("root", "translation")].append((t, tuple(b + d for b, d in zip(base, CARRY))))
            back = _rot(_conj(q), CARRY)
            back = (back[0] / s[0], back[1] / s[1], back[2] / s[2])
            for bone, rest in REST_T.items():
                res[(bone, "translation")].append((t, tuple(r - k for r, k in zip(rest, back))))
        return res
    return run


def _rested(clip):
    """Key the rest translations of `torso` and both legs wherever a clip does not move them."""
    def run():
        out = clip()
        times = sorted({t for keys in out.values() for t, _ in keys})
        for bone, rest in REST_T.items():
            if (bone, "translation") not in out:
                out[(bone, "translation")] = [(t, rest) for t in (times[0], times[-1])]
        return out
    return run


CLIPS = {name: _rested(fn) for name, fn in {
    "idle": idle, "walk": _carried(walk), "sprint": _carried(sprint), "jump": _carried(jump), "fall": _carried(fall),
    "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
    "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
    "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
    "crouch": crouch, "sit": sit, "die": die, "emote-yes": clap, "emote-no": emote_no,
    "cheer": cheer, "clap": clap}.items()}
