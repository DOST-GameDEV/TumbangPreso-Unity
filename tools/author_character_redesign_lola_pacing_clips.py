"""New locomotion clips for the Lola Pacing redesign PROTOTYPE.

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
STOOP = 5.0       # degrees her chest is carried forward when she stands


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


#   where her LEFT arm points in each stance: (upper arm, forearm), in the file's space (+x her
#   left, +y up, +z ahead). The right is the mirror. Her sleeve ends in a bell 161 mm across, so
#   every stance keeps the elbow OUT from the body: the bell is what would cut into the jacket.
#   AS SHE STANDS: the arms hang out from her sides with the elbows a little bent and the hands
#   carried ahead of her hips, the way the original's bent forearms hold them.
ARMS_HANG = ((0.70, -0.71, 0.04), (0.50, -0.78, 0.38))
#   A HAND IN THE SMALL OF HER BACK: the elbow out and behind, the forearm turned in across the
#   back of her hip, the fist on the back of the jacket
ARM_BACK = ((0.78, -0.47, -0.41), (-0.50, -0.30, -0.81))
#   FANNING HERSELF: the upper arm ahead and out, the forearm standing up beside her face with
#   the hand level with her cheek; the hand flaps from the elbow (`fan` below)
ARM_FAN = ((0.66, -0.36, 0.66), (0.10, 0.93, 0.36))
#   a hand resting on her hip while the other one is busy
ARM_HIP = ((0.82, -0.44, -0.36), (0.16, -0.58, 0.80))
#   THE FINGER: the arm out in front of her at chest height, the forearm standing up, and the
#   whole forearm ticked from side to side at whoever she is telling off
ARM_WAG = ((0.74, -0.34, 0.58), (0.30, 0.82, 0.49))     # v03's stood in front of her mouth; this one is beside her face
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of a grandmother waiting her turn in the street, in three things she does:

      HER BACK (0.6 to 2.7 s). Her left hand goes to the small of her back, she presses into it
      and straightens up out of her stoop, chin lifting, holds it, and lets it go with a breath.
      THE HEAT (3.0 to 5.3 s). Left hand on her hip. Her right hand comes up beside her face and
      fans it, quick small flaps from the elbow, her head tipped toward the breeze and her face
      turned a little away from it.
      THE FINGER (5.6 to 7.6 s). Her right arm comes out in front of her, forearm up, and ticks
      side to side three times at somebody across the court, her head following with a small
      shake and her chest leaning into it. Then it drops.

    WHY THESE. Her line is "Watches from the window most afternoons. On the good ones she comes
    down to play, and she does not miss twice": an old woman who is here because her back let
    her today (the hand on it), in the afternoon heat of a Manila street (the fanning), and who
    is nobody's pushover (the finger: she is the slowest and the hardest to move in the game).
    None of them is anyone else's in the cast. Between them she stands in her stoop, weight a
    little on her right foot, breathing small.

    IT IS 8 s, NOT THE OLD 1.33 s. Frame 0 is the neutral stand.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 4.0))
        back = _window(t, 0.6, 2.7, 0.50)
        # the stretch: up out of the stoop over half a second, held, let go
        arch = back * _ease((t - 1.0) / 0.55) * (1.0 - _ease((t - 2.05) / 0.45))
        heat = _window(t, 3.0, 5.3, 0.45)
        # the fanning hand: five flaps a second, only while the hand is up
        fan = math.sin(2.0 * math.pi * 5.0 * (t - 3.45)) * _window(t, 3.45, 5.0, 0.25)
        tell = _window(t, 5.6, 7.6, 0.40)
        # the finger: three ticks, each a quick throw and a short hang
        wag = _snap(math.sin(2.0 * math.pi * 2.2 * (t - 6.05)), 0.6) * _window(t, 6.0, 7.36, 0.20)
        row = {}
        hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
        left_back = _arm(1, *ARM_BACK)
        left_hip = _arm(1, *ARM_HIP)
        right_fan = _arm(-1, *ARM_FAN)
        right_wag = _arm(-1, *ARM_WAG)
        # her left arm: hang, to her back, hang, to her hip (through the heat and the finger)
        hip = max(heat, tell)
        for k, bone in enumerate(("arm-left", "forearm-left")):
            row[(bone, "rotation")] = _mix(_mix(hang[1][k], left_back[k], back), left_hip[k], hip)
        # her right arm: hang, up to fan, across to wag
        upper = _mix(_mix(hang[-1][0], right_fan[0], heat), right_wag[0], tell)
        fore = _mix(_mix(hang[-1][1], right_fan[1], heat), right_wag[1], tell)
        #   the flap and the tick are both turns of the forearm about the upper arm's own axis
        fore = mul(fore, qy(16.0 * fan * heat + 20.0 * wag * tell))
        row[("arm-right", "rotation")] = mul(upper, qz(2.0 * wag * tell))
        row[("forearm-right", "rotation")] = fore
        row[("forearm-left", "scale")] = (1.0 + 0.10 * back, 1.0, 1.0)
        row[("forearm-right", "scale")] = (1.0 + 0.16 * heat + 0.08 * tell, 1.0, 1.0)
        # her chest: the stoop, taken out by the stretch, leaned into the telling off
        chest = STOOP - 9.0 * arch + 4.0 * tell + 0.8 * breath
        # her head: chin up out of the stoop always; back with the stretch; tipped to the fan and
        # turned a little from it; turned to the one she is telling off, shaking with the finger
        nod = -STOOP * 0.8 - 7.0 * arch + 3.0 * heat + 2.0 * tell
        look = 14.0 * heat - 16.0 * tell + 5.0 * wag * tell - 8.0 * back * (1.0 - arch)
        tilt = -7.0 * heat + 3.0 * tell
        lean = -1.6 + 2.6 * back - 1.2 * heat      # weight on her right foot as a rule
        row.update({
            ("root", "translation"): (0.0, 0.006 * arch, 0.0),
            ("root", "scale"): _squash(1.0 + 0.008 * breath + 0.022 * arch - 0.010 * tell),
            ("root", "rotation"): qz(lean),
            ("torso", "rotation"): mul(mul(qx(chest), qz(-1.2 * lean)), qy(0.35 * look - 5.0 * back)),
            ("head", "rotation"): mul(mul(qy(0.65 * look), qx(nod - 0.5 * breath)), qz(tilt - 0.3 * lean)),
            ("leg-left", "rotation"): qz(-2.0 - lean),
            ("leg-right", "rotation"): qz(2.0 - lean),
        })
        _add(out, t, row)
    return out


def walk():
    """A grandmother's walk: short sure steps, the weight rocked right over each foot before the
    other one leaves the ground, the chest forward, the chin up, the head held level over the
    rocking. Her left hand is carried at the small of her back; her right swings a little from
    a bent elbow."""
    out = {}
    length = 0.72
    back = _arm(1, *ARM_BACK)
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.9)
        c = math.cos(phase)
        lift = abs(s) ** 0.8
        rock = math.sin(phase - 0.45)           # the weight arrives after the foot does
        _add(out, t, {
            ("root", "translation"): (0.014 * rock, 0.002 + 0.012 * lift, 0.0),
            ("root", "rotation"): mul(qx(2.0), qz(5.5 * rock)),
            ("root", "scale"): _squash(0.985 + 0.03 * lift),
            ("leg-left", "rotation"): mul(qx(-22.0 * sh), qz(-5.5 * rock - 2.0)),
            ("leg-right", "rotation"): mul(qx(22.0 * sh), qz(-5.5 * rock + 2.0)),
            ("torso", "rotation"): mul(mul(qx(STOOP + 3.0), qy(4.0 * sh)), qz(-3.0 * rock)),
            # the head stays level: it gives back what the body rocks
            ("head", "rotation"): mul(mul(qx(-STOOP - 3.0 + 1.0 * c), qy(-3.0 * sh)), qz(-2.2 * rock)),
            ("arm-left", "rotation"): mul(back[0], qx(3.0 * sh)),
            ("forearm-left", "rotation"): back[1],
            ("forearm-left", "scale"): (1.10, 1.0, 1.0),
            ("arm-right", "rotation"): mul(qx(4.0 - 13.0 * sh), qz(48.0)),
            ("forearm-right", "rotation"): mul(qx(-24.0), qy(34.0 - 8.0 * sh)),
        })
    return out


def sprint():
    """Her run is a hustle: she cannot stride, so she takes more steps. Short quick ones, the body
    pitched forward over them, both elbows tucked and pumping small and fast, the chin out. Her
    bun bobs once a step."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.7)
        lift = abs(s) ** 0.6
        rock = math.sin(phase - 0.3)
        bob = math.sin(2.0 * phase - 0.6)
        _add(out, t, {
            ("root", "translation"): (0.008 * rock, 0.004 + 0.020 * lift, 0.0),
            ("root", "rotation"): mul(qx(11.0), qz(3.5 * rock)),
            ("root", "scale"): _squash(0.97 + 0.06 * lift),
            ("leg-left", "rotation"): mul(qx(-36.0 * sh), qz(-3.5 * rock - 1.5)),
            ("leg-right", "rotation"): mul(qx(36.0 * sh), qz(-3.5 * rock + 1.5)),
            ("torso", "rotation"): mul(mul(qx(8.0), qy(7.0 * sh)), qz(-2.5 * rock)),
            ("head", "rotation"): mul(mul(qx(-17.0 + 3.0 * bob), qy(-4.0 * sh)), qz(-1.0 * rock)),
            # elbows in and bent to a right angle, pumping from the shoulder
            ("arm-left", "rotation"): mul(qx(6.0 + 20.0 * sh), qz(-58.0)),
            ("arm-right", "rotation"): mul(qx(6.0 - 20.0 * sh), qz(58.0)),
            ("forearm-left", "rotation"): mul(qx(-26.0), qy(-(62.0 - 10.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-26.0), qy(62.0 + 10.0 * sh)),
        })
    return out


#   arms up: where the left arm points at the top of the jump. Her ears and earrings stand out
#   beside her shoulders, so the V goes up AHEAD of them and wide.
ARM_UP = ((0.84, 0.22, 0.50), (0.46, 0.82, 0.34))
ARM_UP_REACH = 0.30


def jump():
    """A STANDING jump, both feet together and the same, never a stride. Hers is a hop with her
    whole heart in it: she straightens right out of her stoop as she leaves, both arms thrown up
    in a V through the elbows like a cheer, her head back, her feet together under her. She does
    not stretch far (she is the heaviest thing on the court); she goes up all of a piece."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.15)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.13)
        ring = math.cos(2.0 * math.pi * t / 0.50) * math.exp(-t / 0.20)
        row = {
            ("root", "scale"): _squash(1.0 + 0.11 * ring),
            ("root", "rotation"): qx(-2.0 * hang),
            # both legs the same: trailing a little at take-off, then tucked forward together
            ("leg-left", "rotation"): mul(qx(10.0 * rise - 14.0 * hang), qz(-2.0 + 3.0 * hang)),
            ("leg-right", "rotation"): mul(qx(10.0 * rise - 14.0 * hang), qz(2.0 - 3.0 * hang)),
            ("torso", "rotation"): qx(-7.0 * rise - 2.0 * hang),
            ("head", "rotation"): qx(-14.0 * rise - 3.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearms overshoot open at take-off and settle into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 8.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.7 + 0.3 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: she comes down upright and all of a piece, arms still up in the V and steadying
    her (a small see-saw, one up as the other dips), her feet reaching down for the ground one
    and then the other, looking at where she will land."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.035 + 0.010 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(2.0), qz(1.5 * s)),
            ("leg-left", "rotation"): mul(qx(-8.0 + 6.0 * s), qz(-3.0)),
            ("leg-right", "rotation"): mul(qx(-8.0 - 6.0 * s), qz(3.0)),
            ("torso", "rotation"): mul(qx(5.0), qz(-1.2 * s)),
            ("head", "rotation"): mul(qx(11.0 + 1.0 * c), qz(-0.8 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(5.0 * s))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(4.0 * c))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.65 + 0.08 * s * side), 1.0, 1.0)
        _add(out, t, row)
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


def _seated(height, sy):
    return _body(hip=(0.0, height, -0.03), root=(-8.0, 0.0, 0.0), sy=sy, torso=(STOOP + 9.0, 0.0, 0.0), head=(-5.0, 0.0, 0.0),
                 legl=(-82.0, -11.0), legr=(-82.0, 11.0),
                 left=((0.62, -0.50, 0.60), (-0.34, -0.06, 0.94)), right=((0.62, -0.50, 0.60), (-0.34, -0.06, 0.94)))


def sit():
    """Getting down onto the street, 0.8 s, ending seated the way she sits anywhere: her legs
    straight out in front of her, her back kept up, her hands together on her lap. She braces
    on her knees (0.18), lowers herself with her left hand going back for the ground (0.40),
    lets go and lands (0.55), squashes, and is sat."""
    brace = _winded(0.89, 32.0, -12.0)
    half = _body(hip=(0.0, 0.152, -0.045), root=(-12.0, 0.0, 0.0), sy=0.97, torso=(STOOP + 17.0, 0.0, 0.0), head=(-8.0, 0.0, 0.0),
                 legl=(-33.0, -10.0), legr=(-33.0, 10.0),
                 left=((0.68, -0.44, -0.59), (0.34, -0.48, -0.81)), reach=(1.0, 1.0),
                 right=((0.64, -0.46, 0.61), (-0.20, -0.36, 0.91)))
    return _play(0.8, [(0.0, _stand(), "lin"), (11 / 60.0, brace, "s"), (24 / 60.0, half, "s"),
                       (33 / 60.0, _seated(0.090, 0.86), "in"), (39 / 60.0, _seated(0.103, 1.04), "out"),
                       (0.8, _seated(0.095, 1.0), "s")])


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


def emote_yes():
    """YES is applause, a 1 s loop. Her hands come up in front of her and she claps twice,
    small and quick and pleased, dipping into each clap with a nod and swaying from one foot to
    the other between them."""
    def pose(clap, sway):
        apart = ((0.80, -0.30, 0.52), (0.52, 0.50, 0.69))
        closed = ((0.62, -0.50, 0.60), (-0.62, 0.06, 0.78))
        arm = closed if clap else apart
        return _body(at=(0.005 * sway, 0.0, 0.0), root=(0.0, 0.0, 3.0 * sway), sy=0.93 if clap else 1.04,
                     torso=(STOOP + (8.0 if clap else -4.0), 0.0, -2.2 * sway),
                     head=(9.0 if clap else -10.0, 0.0, 3.5 * sway),
                     legl=(0.0, -2.0 - 3.0 * sway), legr=(0.0, 2.0 - 3.0 * sway),
                     left=arm, right=arm, reach=(1.14, 1.14) if not clap else (1.0, 1.0))
    return _play(1.0, [(0.0, pose(False, 1.0), "lin"), (8 / 60.0, pose(True, 1.0), "in"), (11 / 60.0, pose(True, 0.6), "lin"),
                       (30 / 60.0, pose(False, -1.0), "out"), (38 / 60.0, pose(True, -1.0), "in"),
                       (41 / 60.0, pose(True, -0.6), "lin"), (1.0, pose(False, 1.0), "out")])


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


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
         "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
         "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
         "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
         "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no}
