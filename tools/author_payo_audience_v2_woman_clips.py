"""The clips of the Payo audience villager `woman` (v2).

Imported by tools/author_payo_audience_v2_woman.py, which writes them into her .glb IN PLACE OF the
clips of the same name copied from character-female-a.glb, and APPENDS `cheer` and `clap`, which
the base rig has no slot for. Every other clip of the base is copied across untouched. A copy of
Inday's clips script (tools/author_character_redesign_inday_clips.py) with her own idle and the
crowd's clips the brief asks for (docs/reports/character-redesign/payo-audience-brief.md):

  idle    8 s, ACTED: the shout with cupped hands on her toes, the nudge into her neighbour,
          fists clasped under her chin and a pump (see `idle`)
  cheer   her signature, a 1 s loop: a big V through the elbows and a hop, head back
  clap    a 1 s loop of applause, two claps, a nod into each; `emote-yes` is the same clip
  sit     SEATED ON A LEDGE, a 2 s loop, the seat (the underside of her hips) at z = 0 at the
          root's origin
  walk, sprint, jump, fall and the action clips: Inday's, kept as the brief allows ("the Classic
          base's versions rewritten lightly for this person are fine"); their docstrings still
          describe Inday's apron and ponytail, which this villager does not have, and nothing in
          the crowd plays them.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK (and
nods the head down); about +y a positive angle turns her to her left.
ARMS UP GO THROUGH THE ELBOW: the upper arm lifts a little and the forearm folds up and may
stretch (scale on the forearm bone). NOT SEEN IN UNITY.
"""
import math

RATE = 60.0


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
#   left, +y up, +z ahead). The right is the mirror.
#   HANGING: close to her sides, the forearms a little forward. Easy, but never for long.
ARMS_HANG = ((0.46, -0.88, 0.04), (0.30, -0.93, 0.20))
#   THE SHOUT. Both hands cupped round her mouth: the upper arms forward and in, the forearms
#   folded up so the fists meet beside her mouth, in front of her face plane.
ARM_CUP = ((0.35, -0.55, 0.76), (-0.42, 0.67, 0.61))
CUP_REACH = 0.62
#   THE NUDGE. Her left elbow out to her side, the forearm folded across her front; on the jab
#   the elbow goes out and back into her neighbour on her left.
ARM_JAB_IN = ((0.78, -0.60, 0.10), (-0.30, -0.20, 0.93))
ARM_JAB_OUT = ((0.96, -0.24, -0.16), (0.10, -0.10, 0.99))
JAB_REACH = 0.05
#   laughing at her own nudge: her right fist comes up to her chest
ARM_LAUGH = ((0.40, -0.80, 0.45), (-0.72, 0.38, 0.58))
LAUGH_REACH = 0.25
#   THE CLASP. Both fists together under her chin, the elbows in: waiting for the next throw.
ARM_CLASP = ((0.40, -0.62, 0.68), (-0.80, 0.40, 0.45))
CLASP_REACH = 0.34
#   THE PUMP. Her right fist punched up in front of her, the forearm standing.
ARM_PUMP = ((0.62, 0.28, 0.73), (0.24, 0.94, 0.24))
PUMP_REACH = 0.62
#   (kept for the carry, which the base's holding clips use)
ARM_HIP = ((0.86, -0.46, -0.22), (-0.66, -0.68, 0.32))
HIP_REACH = 0.10
IDLE_LENGTH = 8.0


def _pair_mix(a, b, t):
    return tuple(_mix(a[k], b[k], t) for k in range(2))


def idle():
    """Eight seconds of the loudest supporter on the wall watching a match, in three things:

      THE SHOUT (0.4 to 3.1 s). Up on her toes, leaning in, both hands cupped round her mouth,
      and she SHOUTS three times (1.05, 1.75, 2.45 s): each one throws her head back, puffs her
      chest and stretches her up, then she drops back to watching.
      THE NUDGE (3.4 to 5.6 s). She turns to the neighbour on her left, head round and on one
      side, and digs her left elbow into them twice (4.05, 4.55 s), laughing: her right fist at
      her chest, her head bobbing with it, her weight on her left foot.
      THE CLASP (5.9 to 7.85 s). Back to the game: both fists clasped under her chin, bouncing on
      her toes twice in suspense, then her right fist PUMPS up (7.15 s) and comes home.

    WHY THESE. She is the crowd's voice, so her idle is the noise and the fidget of someone who
    cannot stay quiet: cupped hands, an elbow in a friend's ribs, fists under the chin. Nobody in
    the cast stands like this (Inday wipes her apron and taps a foot with a fist on her hip,
    Bebang punches her palm, Dante folds his arms). "On her toes" is a stretch of the root and a
    few millimetres of lift: the feet stay planted (the brief), the rig has no ankle.

    Frame 0 is the neutral stand. 8 s, ACTED, as the brief asks.
    """
    out = {}
    hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 5.0))
        shout = _window(t, 0.4, 3.1, 0.40)
        nudge = _window(t, 3.4, 5.6, 0.40)
        clasp = _window(t, 5.9, 7.85, 0.40)
        yell = _bump(t, 1.05, 0.11) + _bump(t, 1.75, 0.11) + _bump(t, 2.45, 0.12)
        jab = _bump(t, 4.05, 0.07) + _bump(t, 4.55, 0.07)
        laugh = nudge * (0.5 + 0.5 * math.sin(2.0 * math.pi * (t - 3.9) / 0.26))
        bounce = clasp * (_bump(t, 6.30, 0.10) + _bump(t, 6.70, 0.10))
        pump = _bump(t, 7.15, 0.16) * clasp
        toes = 0.9 * shout + 0.6 * clasp
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            cup = _arm(side, *ARM_CUP)
            if side > 0:
                second = _pair_mix(_arm(side, *ARM_JAB_IN), _arm(side, *ARM_JAB_OUT), jab)
                third = _arm(side, *ARM_CLASP)
            else:
                second = _arm(side, *ARM_LAUGH)
                third = _pair_mix(_arm(side, *ARM_CLASP), _arm(side, *ARM_PUMP), pump)
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                row[(bone, "rotation")] = _mix(_mix(_mix(hang[side][k], cup[k], shout), second[k], nudge), third[k], clasp)
            reach = CUP_REACH * shout * (1.0 + 0.06 * yell)
            reach += (JAB_REACH if side > 0 else LAUGH_REACH) * nudge
            reach += (CLASP_REACH + (PUMP_REACH - CLASP_REACH) * pump * (side < 0)) * clasp
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        look = 34.0 * nudge
        nod = 6.0 * shout - 15.0 * yell + 4.0 * laugh * nudge - 4.0 * clasp + 5.0 * bounce - 10.0 * pump
        tilt = 10.0 * nudge + 4.0 * clasp - 3.0 * yell
        lift = 0.004 * toes + 0.004 * bounce + 0.006 * pump
        stretch = 0.022 * toes + 0.035 * yell + 0.030 * bounce + 0.030 * pump
        row.update({
            ("root", "translation"): (0.010 * nudge, lift, 0.0),
            ("root", "scale"): _squash(1.0 + 0.008 * breath + stretch),
            ("root", "rotation"): qz(-2.5 * nudge),
            ("torso", "rotation"): mul(mul(qx(5.0 * shout - 6.0 * yell + 3.0 * nudge + 3.0 * clasp - 4.0 * pump + 0.8 * breath),
                                           qy(12.0 * nudge - 5.0 * pump)), qz(-6.0 * jab + 3.0 * nudge)),
            ("head", "rotation"): mul(mul(qy(0.8 * look), qx(nod - 0.6 * breath)), qz(tilt)),
            ("leg-left", "rotation"): qz(2.5 - 2.0 * nudge),
            ("leg-right", "rotation"): qz(-2.5 - 3.5 * nudge),
        })
        _add(out, t, row)
    return out


def walk():
    """Neat little skipping steps: a long light lift off each foot and a soft landing, her head
    tipping from side to side with the skip, her arms loose and swinging a little out from her
    sides, hands turned out, the way a girl walks when she is pleased with herself."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.85)
        lift = abs(s) ** 0.8                      # up early and held: a skip, not a stomp
        sway = math.sin(phase - 0.25)
        _add(out, t, {
            ("root", "translation"): (0.008 * sway, 0.004 + 0.046 * lift, 0.0),
            ("root", "rotation"): mul(qx(1.5), qz(2.0 * sway)),
            ("root", "scale"): _squash(0.965 + 0.065 * lift),
            ("leg-left", "rotation"): mul(qx(-30.0 * sh), qz(1.5 - 2.0 * sway)),
            ("leg-right", "rotation"): mul(qx(30.0 * sh), qz(-1.5 - 2.0 * sway)),
            ("torso", "rotation"): mul(mul(qx(1.0), qy(7.0 * sh)), qz(-3.5 * sway)),
            ("head", "rotation"): mul(mul(qx(-2.0 + 2.0 * lift), qy(-5.0 * sh)), qz(7.0 * sway)),
            ("arm-left", "rotation"): mul(qx(26.0 * sh), qz(-64.0)),
            ("arm-right", "rotation"): mul(qx(-26.0 * sh), qz(64.0)),
            # the forearms hang almost straight and kick out as the arm swings back
            ("forearm-left", "rotation"): mul(qx(-10.0), qy(-(12.0 + 10.0 * max(0.0, -sh)))),
            ("forearm-right", "rotation"): mul(qx(-10.0), qy(12.0 + 10.0 * max(0.0, sh))),
        })
    return out


def sprint():
    """A light bouncing run: she stays upright, her chin up, her arms held out from her sides
    with the elbows bent and the forearms swinging across her, her feet kicking up behind."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.8)
        lift = abs(s) ** 1.0
        rock = math.sin(phase - 0.3)
        _add(out, t, {
            ("root", "translation"): (0.0, 0.006 + 0.054 * lift, 0.0),
            ("root", "rotation"): mul(qx(8.0), qz(2.0 * rock)),
            ("root", "scale"): _squash(0.95 + 0.10 * lift),
            # the back swing is longer than the forward one: her heels kick up
            ("leg-left", "rotation"): mul(qx(8.0 - 50.0 * sh), qz(1.5 - 2.0 * rock)),
            ("leg-right", "rotation"): mul(qx(8.0 + 50.0 * sh), qz(-1.5 - 2.0 * rock)),
            ("torso", "rotation"): mul(mul(qx(2.0), qy(12.0 * sh)), qz(-2.5 * rock)),
            ("head", "rotation"): mul(mul(qx(-9.0 + 2.0 * lift), qy(-8.0 * sh)), qz(4.0 * rock)),
            ("arm-left", "rotation"): mul(qx(30.0 * sh), qz(-52.0)),
            ("arm-right", "rotation"): mul(qx(-30.0 * sh), qz(52.0)),
            ("forearm-left", "rotation"): mul(qx(-24.0), qy(-(62.0 - 16.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-24.0), qy(62.0 + 16.0 * sh)),
        })
    return out


#   arms up: where the left arm points at the top of the jump. The V is thrown up, out and a
#   little FORWARD: her ponytail hangs past her right ear behind the line of her shoulders, and
#   the right arm must go up in front of it.
ARM_UP = ((0.78, 0.36, 0.30), (0.36, 0.90, 0.24))
ARM_UP_REACH = 0.72


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. She goes up like a skip that kept going: a quick stretch off
    both feet, both arms flung up in a V through the elbows, her heels kicked up behind her
    together and her head tipped to one side at the top."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.12)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.13)
        ring = math.cos(2.0 * math.pi * t / 0.46) * math.exp(-t / 0.15)
        row = {
            ("root", "scale"): _squash(1.0 + 0.19 * ring),
            ("root", "rotation"): qx(3.0 * hang),
            # both legs the same: straight under her at take-off, then her heels kick up behind
            ("leg-left", "rotation"): mul(qx(8.0 * rise + 26.0 * hang), qz(1.0 + 3.0 * hang)),
            ("leg-right", "rotation"): mul(qx(8.0 * rise + 26.0 * hang), qz(-1.0 - 3.0 * hang)),
            ("torso", "rotation"): qx(-6.0 * rise - 3.0 * hang),
            ("head", "rotation"): mul(qx(-12.0 * rise - 2.0 * hang), qz(8.0 * hang)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 8.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.70 + 0.30 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: she comes down with her arms up in the V and her legs pedalling under her, one
    and then the other, small and quick, her head down to see where she will land. The apron
    and the ponytail do the fluttering."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.04 + 0.014 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(3.0), qz(1.5 * s)),
            ("leg-left", "rotation"): mul(qx(-6.0 + 13.0 * s), qz(4.0)),
            ("leg-right", "rotation"): mul(qx(-6.0 - 13.0 * s), qz(-4.0)),
            ("torso", "rotation"): mul(qx(4.0), qz(-1.5 * s)),
            ("head", "rotation"): mul(qx(13.0 + 2.0 * c), qz(4.0 - 1.0 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(4.0 * s * side))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(5.0 * c))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.66 + 0.06 * s * side), 1.0, 1.0)
        _add(out, t, row)
    return out


# ======================================================================================
# THE ACTION CLIPS. Thirteen that the game plays on her, replacing the ones copied from
# character-female-a.glb, which were made for a body with no elbow. Eight keep the LENGTH of
# the clip they replace and the TIME of its key beat, measured off the copied clips in her
# .glb before they were replaced. Five are only looked at in the game and never timed
# against, and were a sixth of a second long, so they are longer now:
#
#   clip                 old      the old key beat                          here
#   holding-right-shoot  0.2000   the arm's one extreme, at 0.067           0.2000, furthest out at 0.067
#   pick-up              0.3333   lowest at 0.167                           0.3333, lowest at 0.167
#   attack-melee-right   0.4167   hand furthest out at 0.250                0.4167, furthest out at 0.250
#   attack-melee-left    0.4167   hand furthest out at 0.250                0.4167, the bump at 0.250
#   interact-right       0.6667   out by 0.167, held to 0.40, home by 0.667 0.6667, the same three times
#   interact-left        0.6667   the same                                  0.6667, the same
#   slide                0.9500   down by 0.167, lowest at 0.267            0.9500, down 0.150, lowest 0.267
#   die                  0.3333   in the air at 0.10, down by 0.233         0.3333, the same two times
#   holding-right        0.1667   a held pose, two equal keys               a loop of 2.0 s
#   crouch               0.1667   a held pose, two equal keys               a loop of 1.8 s
#   sit                  0.1667   a held pose, two equal keys               0.8 s of getting down
#   emote-yes            0.6667   a nod                                     a loop of 1.2 s
#   emote-no             0.6667   a shake                                   a loop of 1.4 s
#
# THE THROW AND THE TAG ARE HALF THE GAME'S. While they play, code poses the chest, the head
# and both UPPER arms over the clip. What is left of the clip is the root, the legs and the
# two FOREARMS. So in those two she is in the root and the legs (the skip, the kicked heel),
# and each forearm is a plain bend of the elbow with no twist, opening to straight at the
# key beat.
#
# HOW SHE DOES THEM. She is the girl from the bakery stall: light on her feet, her head on
# one side, never hurried and never a threat. So she flips the slipper like a thing on a
# griddle while she carries it, throws it backhand off a skip the way she would deal a tray
# across a counter, picks it up in a curtsey with a corner of her apron held out,
# tags with one long poke, shoves by shutting the oven door with her hip, and when she is
# knocked down she lands flat on her front with her heels in the air.
#
# WHERE HER FISTS ARE AT EACH KEY BEAT (no two the same):
#   carry          right low and out at her right side, forearm level; left on her hip
#   throw          right long, out ahead and to her right, off the ground; left tucked
#   pick up        sunk in a curtsey; right on the ground by her foot, left holding her apron out
#   tag            right dead straight ahead at her shoulder; left straight down behind her
#   shove          side on; left elbow leading, fist up by her jaw; right thrown out behind
#   reach right    leaning in; right long and sloping down ahead to waist height; left on her apron
#   reach left     folded down to her left; left at her shin; right in the small of her back
#   slide          on the ground on her left side; left planted, right past her feet
#   out of breath  folded over; left on her knee; right hanging dead and swinging
#   sit            sat; both under her chin
#   knocked down   on her front; both flat on the ground ahead, heels up behind
#   yes            right up beside her head and flicked out high; left behind her back
#   no             both in the small of her back; she leans in and swings
# ======================================================================================
HIP = (0.0836, 0.17625, -0.02875)     # the leg bones, in the root's space; the sole is LEG below
LEG = 0.17625
LEGS_STAND = (qz(2.5), qz(-2.5))


def _inv(q):
    return (-q[0], -q[1], -q[2], q[3])


def _arm_in(side, upper, fore, parent):
    """`_arm`, with the two directions given in the space ABOVE `parent` (the rotation the
    arm's parent has there), so a fist can be sent to a knee or the ground whatever the chest
    is doing. x is OUT from her side for either arm, y up, z ahead."""
    rest = (float(side), 0.0, 0.0)
    back = _inv(parent)
    u = _rot(back, (side * upper[0], upper[1], upper[2]))
    f = _rot(back, (side * fore[0], fore[1], fore[2]))
    q = _arc(rest, u)
    return q, _arc(rest, _rot(_inv(q), _norm(f)))


def _pose(tx=0.0, tz=0.0, lift=0.0, root=None, sy=1.0, scale=None, torso=None, head=None,
          left=ARMS_HANG, right=ARMS_HANG, reach=(0.0, 0.0), legs=LEGS_STAND, space="torso", ground=True):
    """One whole-body pose, as the row of channels a clip keys.

    `left` and `right` are (upper arm, forearm) directions with x OUT from her side; `space`
    says what they are measured in: "torso" (they ride the chest), "root" (the chest's lean is
    taken out) or "world" (the root's turn is taken out too). `reach` is how far each forearm
    is stretched past its own length, left then right. `sy` squashes the root keeping volume
    (`scale` overrides it). With `ground` the root is set down so the lower sole is on the
    floor, then raised by `lift`.
    """
    root = root or qx(0.0)
    torso = torso or qx(0.0)
    head = head or qx(0.0)
    scale = scale or _squash(sy)
    parent = {"torso": qx(0.0), "root": torso, "world": mul(root, torso)}[space]
    row = {}
    for side, name, arm, k in ((1, "left", left, 0), (-1, "right", right, 1)):
        a, f = _arm_in(side, arm[0], arm[1], parent)
        row[("arm-" + name, "rotation")] = a
        row[("forearm-" + name, "rotation")] = f
        row[("forearm-" + name, "scale")] = (1.0 + reach[k], 1.0, 1.0)
    y = lift
    if ground:
        low = 1e9
        for side, q in ((1, legs[0]), (-1, legs[1])):
            d = _rot(q, (0.0, -LEG, 0.0))
            foot = (side * HIP[0] + d[0], HIP[1] + d[1], HIP[2] + d[2])
            foot = _rot(root, tuple(a * b for a, b in zip(foot, scale)))
            low = min(low, foot[1])
        y -= low
    row.update({
        ("root", "translation"): (tx, y, tz),
        ("root", "rotation"): root,
        ("root", "scale"): scale,
        ("torso", "rotation"): torso,
        ("head", "rotation"): head,
        ("leg-left", "rotation"): legs[0],
        ("leg-right", "rotation"): legs[1],
    })
    return row


def _blend(a, b, t):
    """From row `a` to row `b`: turns the short way, everything else straight."""
    out = {}
    for key, va in a.items():
        vb = b[key]
        out[key] = _mix(va, vb, t) if key[1] == "rotation" else tuple(p + (q - p) * t for p, q in zip(va, vb))
    return out


EASES = {
    "smooth": _ease,
    "lin": lambda t: t,
    "in": lambda t: t * t,                       # gathers speed and arrives fast: a hit
    "out": lambda t: 1.0 - (1.0 - t) ** 2,        # leaves fast and hangs: a wind up, a settle
}


def _act(length, keys):
    """Keyed poses played at the clip's rate. `keys` are (time, row, how it ARRIVES)."""
    out = {}
    for t in _times(length):
        k = 1
        while k < len(keys) - 1 and t > keys[k][0]:
            k += 1
        t0, a, _ = keys[k - 1]
        t1, b, how = keys[k]
        u = 0.0 if t1 <= t0 else max(0.0, min(1.0, (t - t0) / (t1 - t0)))
        _add(out, t, _blend(a, b, EASES[how](u)))
    return out


def _stand():
    """The neutral stand, which is the idle's first frame."""
    return _pose()


def _lerp3(a, b, t):
    return tuple(p + (q - p) * t for p, q in zip(a, b))


def _elbow(side, bend, up=0.0):
    """A forearm folded `bend` degrees at the elbow and nothing else: forward of the arm at
    `up` 0, above it at 90. No twist, so it sits right on an upper arm the game has pointed."""
    b, u = math.radians(bend), math.radians(up)
    return _arc((float(side), 0.0, 0.0), (side * math.cos(b), math.sin(b) * math.sin(u), math.sin(b) * math.cos(u)))


def _elbows(row, left, right):
    """`row` with both forearms replaced by plain bends, each (bend, up)."""
    row[("forearm-left", "rotation")] = _elbow(1, *left)
    row[("forearm-right", "rotation")] = _elbow(-1, *right)
    return row


#   THE CARRY. The tsinelas rides flat in her right fist, low and out at her side with the
#   forearm level, the way she holds a turner over a griddle; her left fist is on her hip.
ARM_PAN = ((0.72, -0.66, 0.20), (0.42, -0.06, 0.90))
ARM_PAN_UP = ((0.74, -0.56, 0.38), (0.40, 0.58, 0.71))
PAN_REACH = 0.34


def _carry(flip=0.0, sy=1.0, sway=0.0, look=-8.0, nod=0.0, tilt=8.0):
    upper = _lerp3(ARM_PAN[0], ARM_PAN_UP[0], flip)
    fore = _lerp3(ARM_PAN[1], ARM_PAN_UP[1], flip)
    return _pose(tx=0.012 + 0.010 * sway, root=qz(2.6 + 1.6 * sway), sy=sy,
                 torso=mul(qy(-7.0), qz(-4.5 - 1.5 * sway)), head=mul(mul(qy(look), qx(nod)), qz(tilt)),
                 left=ARM_HIP, right=(upper, fore), reach=(HIP_REACH, PAN_REACH + 0.30 * flip),
                 legs=(qz(0.5), mul(qx(-7.0), qz(-8.0))))


def holding_right():
    """Carrying the tsinelas, a loop of 2 s: stood on her left hip with her left fist on it and
    the slipper held low and level out at her right side. She breathes, her weight rocks, and
    twice (0.62 s and 1.06 s) she FLIPS it: the forearm snaps up from the elbow, her chin
    goes up after the thing in the air, and she dips to catch it. A pandesal on a griddle."""
    length = 2.0
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        breath = math.sin(2.0 * p)
        flip = min(1.0, _bump(t, 0.62, 0.075) + _bump(t, 1.06, 0.075))
        catch = _bump(t, 0.76, 0.06) + _bump(t, 1.20, 0.06)
        watch = _window(t, 0.45, 1.40, 0.25)
        _add(out, t, _carry(flip=flip, sy=1.0 + 0.010 * breath + 0.030 * flip - 0.040 * catch, sway=math.sin(p),
                            look=-8.0 - 14.0 * watch, nod=-13.0 * flip + 7.0 * catch - 0.6 * breath,
                            tilt=8.0 - 5.0 * watch + 3.0 * math.sin(p)))
    return out


def holding_right_shoot():
    """THE THROW, what is hers of it (the game points her chest and upper arms). She deals it
    BACKHAND off a skip: she coils down over her left leg with the slipper drawn across to her
    left shoulder, then goes up off the ground uncoiling, the elbow opening straight and the
    forearm thrown long out to her right (0.067 s), her right heel kicked up high behind her;
    she comes down on her left toe with the heel still up, and drops back into the carry."""
    cocked = _pose(sy=0.90, root=qy(24.0), torso=qx(6.0), head=mul(qy(-20.0), qz(10.0)), space="world",
                   left=((0.62, -0.60, 0.50), (0.10, 0.20, 0.97)),
                   right=((0.10, -0.25, 0.96), (-0.92, 0.30, 0.25)), reach=(0.0, 0.20),
                   legs=(mul(qx(-4.0), qz(4.0)), mul(qx(14.0), qz(-5.0))))
    out = _pose(tz=0.040, lift=0.034, sy=1.10, root=mul(qx(8.0), qy(-14.0)), torso=qx(3.0),
                head=mul(qy(12.0), qz(-9.0)), space="world",
                left=((0.60, -0.60, -0.52), (0.10, 0.55, 0.83)),
                right=((0.62, 0.22, 0.75), (0.50, 0.20, 0.84)), reach=(0.0, 1.10),
                legs=(mul(qx(-16.0), qz(3.0)), mul(qx(46.0), qz(-4.0))))
    through = _pose(tz=0.050, sy=0.93, root=mul(qx(11.0), qy(-24.0)), torso=qx(5.0),
                    head=mul(qy(20.0), qz(-11.0)), space="world",
                    left=((0.55, -0.70, -0.45), (0.20, 0.30, 0.93)),
                    right=((0.96, 0.02, 0.26), (0.92, -0.20, -0.33)), reach=(0.0, 0.45),
                    legs=(mul(qx(-11.0), qz(3.0)), mul(qx(40.0), qz(-4.0))))
    _elbows(cocked, (55.0, 30.0), (82.0, 25.0))
    _elbows(out, (60.0, 40.0), (4.0, 0.0))
    _elbows(through, (50.0, 30.0), (22.0, 0.0))
    return _act(0.2, [(0.0, _carry(), "lin"), (1.0 / 30.0, cocked, "out"), (2.0 / 30.0, out, "in"),
                      (0.117, through, "out"), (0.2, _carry(), "smooth")])


def pick_up():
    """A curtsey. She stays upright and DIPS: her feet together with the left tucked behind
    the right, the whole of her sinking straight down and tipping a little to her right, her
    left fist holding the corner of her apron out at her side and her right fist sweeping
    along the ground beside her foot, her head over on the other side (lowest at 0.167 s).
    Then she bobs up past standing with the slipper held up beside her, and settles.
    (a first try tipped over on one leg like a seesaw; four others in the cast do that)"""
    ready = _pose(sy=1.06, torso=qx(-3.0), head=mul(qx(-4.0), qz(6.0)), left=((0.84, -0.50, 0.14), (0.74, -0.50, 0.44)),
                  right=((0.80, -0.56, 0.20), (0.70, -0.66, 0.26)))

    def low(sy, sweep, grab):
        return _pose(tx=-0.020, sy=sy, root=qz(13.0), torso=mul(qx(10.0), qz(27.0)),
                     head=mul(mul(qy(-12.0), qx(2.0)), qz(-34.0)), space="world",
                     left=((0.90, -0.34, 0.26), (0.84, -0.20, 0.50)),
                     right=((0.46, -0.87, 0.18), (0.24, -0.93, 0.28 - 0.36 * sweep)), reach=(0.30, 0.55 + grab),
                     legs=(mul(mul(qy(-16.0), qx(13.0)), qz(-13.0)), mul(qx(-5.0), qz(-13.0))))

    up = _pose(sy=1.07, torso=qx(-4.0), head=mul(qx(-6.0), qz(8.0)), left=((0.80, -0.56, 0.16), (0.66, -0.66, 0.36)),
               right=((0.80, -0.20, 0.50), (0.40, 0.78, 0.48)), reach=(0.0, 0.30))
    return _act(1.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 30.0, ready, "out"), (1.0 / 6.0, low(0.80, 0.0, 0.25), "in"),
                            (0.21, low(0.83, 1.0, 0.15), "out"), (0.283, up, "smooth"), (1.0 / 3.0, _stand(), "smooth")])


def attack_melee_right():
    """THE TAG, what is hers of it (for 0.34 s the game takes her whole body to its own reach
    and leaves the clip the forearms and the root). One long poke: she sinks a little with the
    elbow folded, then goes up tall on her toes with the forearm opened dead straight and
    long ahead of her (0.25 s), her other arm straight down behind her like a fencer's. It
    stops at the touch. Then, hers alone: she rocks back onto her heels with the fist flicked
    up beside her shoulder, pleased, before it falls."""
    wind = _pose(sy=0.93, tz=-0.012, root=qy(-8.0), torso=qx(-2.0), head=mul(qy(8.0), qz(8.0)),
                 left=((0.60, -0.76, 0.20), (0.30, -0.50, 0.80)),
                 right=((0.62, -0.60, -0.50), (0.22, 0.10, 0.97)),
                 legs=(mul(qx(-3.0), qz(3.0)), mul(qx(3.0), qz(-3.0))))
    _elbows(wind, (40.0, 0.0), (70.0, 15.0))

    def hit(sy, lean, reach):
        row = _pose(tz=0.040, sy=sy, root=mul(qx(lean), qy(9.0)), torso=qx(0.3 * lean),
                    head=mul(mul(qy(-9.0), qx(-0.8 * lean)), qz(10.0)), space="world",
                    left=((0.34, -0.62, -0.70), (0.26, -0.64, -0.72)),
                    right=((0.06, 0.12, 0.99), (0.0, 0.10, 1.0)), reach=(0.45, reach),
                    legs=(mul(qx(-lean - 5.0), qz(3.0)), mul(qx(9.0 - lean), qz(-3.0))))
        return _elbows(row, (3.0, 0.0), (2.0, 0.0))

    back = _pose(sy=1.03, tz=-0.010, root=qx(-3.0), torso=qx(-3.0), head=mul(qx(-5.0), qz(-9.0)),
                 right=((0.70, -0.55, 0.45), (0.30, 0.86, 0.40)), reach=(0.0, 0.25))
    return _act(5.0 / 12.0, [(0.0, _stand(), "lin"), (0.12, wind, "out"), (0.25, hit(1.10, 17.0, 1.45), "smooth"),
                             (0.29, hit(1.01, 20.0, 1.05), "out"), (0.355, back, "smooth"), (5.0 / 12.0, _stand(), "smooth")])


def attack_melee_left():
    """THE SHOVE: she shuts the oven door with her hip. She turns her left side to you and
    sinks, then the hip goes into you with the whole of her behind it (0.25 s): side on, her
    left elbow leading with the fist up by her jaw, her right arm thrown out behind and her
    right foot kicked off the ground, her face looking back at you over the shoulder. She
    lands, squashed, and swings round to the front again."""
    wind = _pose(tz=-0.030, sy=0.88, root=qy(-22.0), torso=qx(4.0), head=mul(qy(20.0), qz(-6.0)), space="world",
                 left=((0.70, -0.66, 0.26), (0.10, 0.40, 0.91)), right=((0.66, -0.70, 0.26), (0.10, 0.30, 0.95)),
                 legs=(mul(qx(-4.0), qz(5.0)), mul(qx(-4.0), qz(-5.0))))

    def bump(tz, sy, lean, kick, reach):
        return _pose(tz=tz, sy=sy, root=mul(qx(-lean), qy(-66.0)), torso=qz(-9.0),
                     head=mul(qy(46.0), qz(-10.0)), space="world",
                     left=((0.30, -0.10, 0.95), (0.06, 0.90, 0.42)),
                     right=((0.50, -0.20, -0.84), (0.34, 0.30, -0.89)), reach=(reach, 0.40),
                     legs=(qz(lean + 2.0), mul(qx(6.0), qz(-kick))))

    return _act(5.0 / 12.0, [(0.0, _stand(), "lin"), (0.14, wind, "out"), (0.25, bump(0.120, 1.07, 19.0, 44.0, 0.60), "in"),
                             (0.31, bump(0.120, 0.93, 12.0, 26.0, 0.40), "out"), (5.0 / 12.0, _stand(), "smooth")])


def interact_right():
    """The counter. She leans in over it with her left fist held to the front of her apron
    and presses her right fist down ahead of her at the height of her waist, the arm long and
    sloping down, three times, the way she tests a loaf: down by 0.167 s, again at 0.30 and
    at 0.40, her head on one side to listen to it. Then she straightens.
    (a first try reached for a high shelf; with a head wider than her shoulders an arm sent
    up beside it is a wave, and it was her yes over again)"""
    def press(deep, tilt):
        return _pose(sy=0.99 - 0.035 * deep, tz=-0.012, root=qx(9.0), torso=mul(qx(15.0 + 9.0 * deep), qy(12.0)),
                     head=mul(mul(qy(-10.0), qx(-10.0 - 5.0 * deep)), qz(tilt)), space="world",
                     left=((0.50, -0.78, 0.38), (-0.52, -0.40, 0.75)),
                     right=((0.24, -0.34 - 0.20 * deep, 0.91), (0.0, -0.30 - 0.34 * deep, 0.95)), reach=(0.22, 0.75 + 0.30 * deep),
                     legs=(mul(qx(-9.0), qz(3.0)), mul(qx(-9.0), qz(-3.0))))

    return _act(2.0 / 3.0, [(0.0, _stand(), "lin"), (0.09, press(-0.4, 3.0), "out"), (1.0 / 6.0, press(1.0, 9.0), "in"),
                            (0.24, press(0.0, 6.0), "out"), (0.30, press(1.0, 11.0), "in"),
                            (0.35, press(0.1, 8.0), "out"), (0.40, press(1.0, 13.0), "in"),
                            (0.50, press(0.2, 8.0), "out"), (2.0 / 3.0, _stand(), "smooth")])


def interact_left():
    """The low shelf. She folds down to her left, her seat pushed back, the left fist down by
    her shin and the right tucked into the small of her back, head on one side to see in;
    rummages twice; and stands. Down by 0.167 s, held to 0.40."""
    def down(fold, sy, reach):
        return _pose(sy=sy, tz=-0.045, tx=0.012, root=qx(10.0), torso=mul(qx(fold), qz(-12.0)),
                     head=mul(mul(qy(14.0), qx(-0.85 * fold)), qz(-12.0)), space="world",
                     left=((0.34, -0.80, 0.50), (0.04, -0.84, 0.54)),
                     right=((0.55, -0.50, -0.67), (-0.62, -0.42, -0.66)), reach=(reach, 0.0),
                     legs=(mul(qx(-10.0), qz(7.0)), mul(qx(-10.0), qz(-5.0))))

    return _act(2.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 6.0, down(48.0, 0.93, 0.70), "in"), (0.23, down(42.0, 0.96, 0.40), "smooth"),
                            (0.30, down(50.0, 0.92, 0.80), "smooth"), (0.35, down(44.0, 0.95, 0.45), "smooth"),
                            (0.40, down(50.0, 0.92, 0.80), "smooth"), (0.56, _pose(sy=1.03, torso=qx(-3.0)), "smooth"),
                            (2.0 / 3.0, _stand(), "smooth")])


def slide():
    """The retrieval slide, on her side like a girl sliding across a waxed floor: a skip, and
    she is down on her left hip, leaning back and rolled over onto her left fist, her legs
    together out ahead and her right arm thrown up over her head; at the bottom of it the arm
    comes down and reaches long past her feet for the slipper; then she rocks up over her
    feet and stands with a bounce. Down by 0.150 s, lowest at 0.267, on her feet from 0.62."""
    hop = _pose(sy=1.06, lift=0.022, torso=qx(-5.0), head=qx(-4.0),
                left=((0.84, -0.30, 0.30), (0.60, 0.50, 0.60)), right=((0.80, 0.10, 0.40), (0.40, 0.80, 0.40)),
                reach=(0.0, 0.3), legs=(mul(qx(-10.0), qz(2.0)), mul(qx(-10.0), qz(-2.0))))

    def down(back, roll, sy, arm, reach):
        high = ((0.62, 0.66, 0.30), (0.30, 0.92, 0.20))
        far = ((0.20, -0.36, 0.91), (0.04, -0.30, 0.95))
        right = (_lerp3(high[0], far[0], arm), _lerp3(high[1], far[1], arm))
        return _pose(tz=0.080, tx=0.030, lift=0.045, sy=sy, root=mul(qz(-roll), qx(-back)), torso=mul(qx(14.0 + 10.0 * arm), qz(0.4 * roll)),
                     head=mul(qx(back - 22.0), qz(0.5 * roll)), space="world",
                     left=((0.70, -0.62, -0.36), (0.52, -0.84, -0.14)),
                     right=right, reach=(0.50, reach),
                     legs=(mul(qy(6.0), qx(-(84.0 - back))), mul(qy(10.0), qx(-(98.0 - back)))))

    squat = _pose(sy=0.90, tz=0.030, torso=qx(14.0), head=mul(qx(-8.0), qz(8.0)), space="root",
                  left=((0.76, -0.50, 0.40), (0.40, -0.40, 0.82)), right=((0.70, -0.40, 0.60), (0.30, 0.50, 0.81)),
                  reach=(0.0, 0.2), legs=(qz(16.0), qz(-16.0)))
    up = _pose(sy=1.06, torso=qx(-3.0), head=mul(qx(-4.0), qz(6.0)))
    settle = _pose(sy=0.975)
    return _act(0.95, [(0.0, _stand(), "lin"), (0.06, hop, "out"), (0.15, down(38.0, 24.0, 0.96, 0.0, 0.60), "in"),
                       (0.267, down(46.0, 30.0, 0.91, 0.25, 0.60), "out"), (0.34, down(40.0, 22.0, 0.94, 1.0, 0.95), "smooth"),
                       (0.40, down(36.0, 18.0, 0.96, 1.0, 0.70), "smooth"), (0.52, squat, "smooth"), (0.64, up, "smooth"),
                       (0.76, settle, "smooth"), (0.86, _stand(), "smooth"), (0.95, _stand(), "lin")])


def crouch():
    """Out of breath, a loop of 1.8 s, and not making a fuss of it: folded over with her left
    fist propped on her knee and her right arm hanging dead, swinging under her like a rope.
    Three breaths lift her back. On the third she puffs her fringe out of her eyes, her face
    coming up and on one side, and lets it drop. Folded over on every frame."""
    length = 1.8
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        air = 0.5 - 0.5 * math.cos(3.0 * p)                # 0 emptied, 1 full
        swing = math.sin(p)
        puff = _window(t, 1.05, 1.55, 0.20)
        _add(out, t, _pose(tz=-0.030, tx=0.008, sy=0.94 + 0.040 * air, root=qz(2.0), torso=mul(qx(40.0 - 9.0 * air), qz(-5.0)),
                           head=mul(qx(2.0 - 5.0 * air - 30.0 * puff), qz(-6.0 + 16.0 * puff)), space="root",
                           left=((0.72, -0.64 + 0.20 * air, 0.22), (-0.10, -0.97, -0.20)),
                           right=((0.52 + 0.10 * swing, -0.80, 0.30), (0.30 + 0.30 * swing, -0.93, 0.16)),
                           reach=(0.10 + 0.22 * air, 0.45),
                           legs=(mul(qx(-12.0), qz(11.0)), mul(qx(-12.0), qz(-9.0)))))
    return out


ARM_CHIN = ((0.24, -0.76, 0.60), (-0.28, 0.62, 0.73))       # a fist under her chin, the elbow dropped
ARM_OUT = ((0.86, 0.10, 0.30), (0.70, 0.56, 0.30))
CHIN_REACH = 0.30


def _seat(sy=1.0, back=-4.0, kick=86.0, up=0.0, chin=1.0, nod=4.0, tilt=10.0, cross=5.0):
    """Sat on the ground with her legs together out ahead: leaning `back` (forward if under
    0), `chin` 1 with both fists under her chin and 0 with her arms out for balance."""
    arm = (_lerp3(ARM_OUT[0], ARM_CHIN[0], chin), _lerp3(ARM_OUT[1], ARM_CHIN[1], chin))
    return _pose(root=qx(-back), sy=sy, lift=0.040 * sy + up, torso=qx(4.0), head=mul(qx(nod), qz(tilt)), space="world",
                 left=arm, right=arm, reach=(CHIN_REACH * chin + 0.2 * (1.0 - chin),) * 2,
                 legs=(mul(qy(-cross), qx(-(kick - back))), mul(qy(cross), qx(-(kick - back)))))


def die():
    """Knocked down, and she goes over FORWARD: a jolt off her feet with her arms flung out,
    over onto her front in the air (0.10 s), flat on the street with a smack (0.233 s) that
    throws her heels up behind her, and there she stays: on her front, chin on the ground,
    arms out ahead, both feet stuck up in the air. The last frame is held."""
    wide = ((0.84, 0.30, 0.25), (0.60, 0.74, 0.20))
    ahead = ((0.70, 0.0, 0.70), (0.50, -0.06, 0.86))
    jolt = _pose(lift=0.060, ground=False, sy=1.10, root=qx(10.0), torso=qx(4.0), head=qx(-18.0),
                 left=wide, right=wide, reach=(0.3, 0.3), legs=(mul(qx(18.0), qz(5.0)), mul(qx(18.0), qz(-5.0))))
    over = _pose(lift=0.130, tz=-0.060, ground=False, sy=1.04, root=qx(50.0), torso=qx(3.0), head=qx(-30.0),
                 left=wide, right=wide, reach=(0.4, 0.4), legs=(mul(qx(34.0), qz(8.0)), mul(qx(34.0), qz(-8.0))))

    def flat(lift, scale, legs, chin, tilt, reach):
        return _pose(lift=lift, tz=-0.200, ground=False, scale=scale, root=qx(84.0), head=mul(qx(-chin), qz(tilt)), space="world",
                     left=ahead, right=ahead, reach=(reach, reach),
                     legs=(mul(qx(legs[0]), qz(7.0)), mul(qx(legs[1]), qz(-7.0))))

    return _act(1.0 / 3.0, [(0.0, _stand(), "lin"), (0.05, jolt, "out"), (0.10, over, "lin"),
                            (0.233, flat(0.085, (1.10, 1.03, 0.82), (20.0, 20.0), 62.0, 0.0, 0.5), "in"),
                            (0.283, flat(0.125, (0.97, 1.0, 1.06), (74.0, 60.0), 50.0, 6.0, 0.3), "out"),
                            (1.0 / 3.0, flat(0.100, (1.0, 1.0, 1.0), (50.0, 66.0), 66.0, 12.0, 0.4), "in")])


def emote_no():
    """NO, a loop of 1.4 s, sung more than said: nuh-uh. Both fists go behind her back and
    stay there, and she leans in toward you from the waist and SWINGS: her shoulders turn one
    way and her head goes the other with her chin up, the whole of her rocking over onto one
    foot and then the other with the free foot lifting off the ground, twice over. Nothing
    of her is in a hurry. Her fists show in the small of her back, which is the side the
    player sees.
    (a first try wagged both fists beside her ears like wipers; another in the cast does)"""
    length = 1.4
    behind = ((0.52, -0.60, -0.61), (-0.60, -0.46, -0.65))
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        s = _snap(math.sin(2.0 * p), 0.6)
        beat = abs(math.sin(2.0 * p))
        # the foot she is not on comes up and out as she rocks over the other
        legs = (mul(qx(-13.0), qz(7.0 * s + 24.0 * max(0.0, -s))), mul(qx(-13.0), qz(7.0 * s - 24.0 * max(0.0, s))))
        _add(out, t, _pose(sy=0.97 + 0.05 * beat, tx=0.026 * s, tz=-0.024, root=mul(qx(13.0), qz(-7.0 * s)),
                           torso=mul(mul(qx(11.0), qy(30.0 * s)), qz(-6.0 * s)),
                           head=mul(mul(qy(-56.0 * s), qx(-20.0)), qz(13.0 * s)),
                           left=behind, right=behind, reach=(0.10, 0.10), legs=legs))
    return out




# ======================================================================================
# THE CROWD'S CLIPS (docs/reports/character-redesign/payo-audience-brief.md): `cheer`, `clap`
# and the ledge `sit`. `emote-yes` is the clap.
# ======================================================================================
#   THE V: thrown up and out through the elbows, a little forward of her coil, wide open at
#   the top of the hop and pulled in a little as she lands
ARM_V_HIGH = ((0.74, 0.44, 0.26), (0.46, 0.86, 0.20))
ARM_V_LOW = ((0.80, 0.24, 0.40), (0.40, 0.88, 0.26))
V_REACH = 0.72


def cheer():
    """HER SIGNATURE, a 1 s loop: the big V and a hop. Squashed on landing at 0 s with the arms
    pulled in, she springs (0.18 s), is in the air with her heels kicked up behind, the V thrown
    wide and her head back, shouting (0.42 s), and lands again, the arms dropping in with the
    squash (0.80 s), so the loop starts on the landing. Feet together, never a stride."""
    def at(lift, sy, v, head, heels, chest):
        arm = (_lerp3(ARM_V_LOW[0], ARM_V_HIGH[0], v), _lerp3(ARM_V_LOW[1], ARM_V_HIGH[1], v))
        return _pose(lift=lift, sy=sy, torso=qx(chest), head=mul(qx(head), qz(3.0 * v)), left=arm, right=arm,
                     reach=(V_REACH * (0.85 + 0.15 * v),) * 2,
                     legs=(mul(qx(heels), qz(2.5 + 2.0 * v)), mul(qx(heels), qz(-2.5 - 2.0 * v))))

    land = at(0.0, 0.88, 0.0, 6.0, 0.0, 6.0)
    return _act(1.0, [(0.0, land, "lin"), (0.18, at(0.010, 1.10, 0.75, -10.0, 6.0, -4.0), "out"),
                      (0.42, at(0.042, 1.04, 1.0, -20.0, 22.0, -6.0), "out"), (0.62, at(0.026, 1.02, 0.9, -14.0, 14.0, -4.0), "smooth"),
                      (0.80, at(0.0, 0.86, 0.15, 4.0, 2.0, 5.0), "in"), (1.0, land, "out")])


#   THE CLAP: open in front of her chest, the forearms standing; shut, the fists meeting
ARM_CLAP_OPEN = ((0.62, -0.48, 0.62), (0.18, 0.58, 0.80))
ARM_CLAP_SHUT = ((0.44, -0.58, 0.68), (-0.80, 0.30, 0.52))
CLAP_REACH = 0.42


def clap():
    """Applause, a 1 s loop: two claps (0.13 s and 0.63 s), quick and big, her hands meeting in
    front of her chest, a nod and a dip into each clap and a little stretch up between them, her
    weight swaying from one foot to the other. (Lola Pacing's emote-yes is the pattern; hers is
    small and pleased, this one is loud.)"""
    def at(shut, sway, sy):
        arm = (_lerp3(ARM_CLAP_OPEN[0], ARM_CLAP_SHUT[0], shut), _lerp3(ARM_CLAP_OPEN[1], ARM_CLAP_SHUT[1], shut))
        return _pose(tx=0.006 * sway, root=qz(2.5 * sway), sy=sy, torso=mul(qx(6.0 * shut - 3.0), qz(-2.0 * sway)),
                     head=mul(qx(10.0 * shut - 8.0), qz(4.0 * sway)), left=arm, right=arm,
                     reach=(CLAP_REACH * (0.75 + 0.25 * shut),) * 2,
                     legs=(qz(2.5 - 2.5 * sway), qz(-2.5 - 2.5 * sway)))

    return _act(1.0, [(0.0, at(0.0, 1.0, 1.03), "lin"), (0.13, at(1.0, 0.8, 0.94), "in"), (0.18, at(0.9, 0.6, 0.95), "lin"),
                      (0.50, at(0.0, -1.0, 1.04), "out"), (0.63, at(1.0, -0.8, 0.94), "in"), (0.68, at(0.9, -0.6, 0.95), "lin"),
                      (1.0, at(0.0, 1.0, 1.03), "out")])


emote_yes = clap


#   SEATED: her hands on her knees, the forearms reaching forward along her thighs
ARM_KNEE = ((0.24, -0.82, 0.52), (-0.18, -0.20, 0.96))
SIT_LENGTH = 2.0
SIT_THIGH = 64.0       # degrees the legs are swung forward from hanging


def sit():
    """SEATED ON A LEDGE as a spectator, a 2 s loop. THE ROOT AT ITS REST PLACE: the seat, the
    underside of her hips, is at z = 0 at the root's origin, centred on it across and front to
    back (the hips block runs y -0.088 to 0.084 in Blender space, x -0.140 to 0.140). The root is
    lowered by the leg length (0.17625) and nothing else: no root turn and no root scale, so the
    seat stays on the ledge whatever the rest does; the lean and the breath are in the chest.

    Her legs are swung forward off the ledge (the rig's leg is one bone, so the thigh and the
    shin are one block: it points forward and a little down, out over the edge) and she swings
    her feet, one forward as the other goes back, the way a person on a wall does when the game
    is good; her hands are on her knees, her chest leaning in, her head up and following play."""
    out = {}
    for t in _times(SIT_LENGTH):
        p = 2.0 * math.pi * t / SIT_LENGTH
        breath = math.sin(p)
        swing = math.sin(p)
        look = math.sin(p * 0.5 + 0.6)
        row = _pose(lift=-LEG, ground=False, torso=qx(7.0 + 1.5 * breath), head=mul(mul(qy(14.0 * look), qx(-6.0 - 1.0 * breath)), qz(-4.0 * look)),
                    left=ARM_KNEE, right=ARM_KNEE, reach=(0.05, 0.05), space="root",
                    legs=(mul(qx(-SIT_THIGH + 7.0 * swing), qz(3.0)), mul(qx(-SIT_THIGH - 7.0 * swing), qz(-3.0))))
        _add(out, t, row)
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
         "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
         "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
         "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
         "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no,
         "cheer": cheer, "clap": clap}
