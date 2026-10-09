"""New locomotion clips for the Ate Girlie redesign PROTOTYPE.

Imported by tools/author_character_redesign_ate_girlie.py, which writes them into the prototype
.glb IN PLACE OF the clips of the same name copied from character-female-b.glb. Nothing in the
game reads them; character-female-b.glb keeps its own. Every other clip in the .glb (slide, sit,
the emotes, the holding and attack clips, 28 in all) is copied across untouched. A copy of
Bebang's clips script rewritten for her: each character's motion is its OWN
(docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so none of the numbers below are another's.

WHAT HER MOTION SHOULD SAY ABOUT HER. ATE GIRLIE is "the composed older sister: upright, chin
up, feet placed neatly, small graceful arm swing" (`GaitStyles.AteGirlie`); "a champion of
playground line games, trying a new court. The footwork came with her" (the character select);
quick and even (GAME_OVERVIEW.md: 4/3/3). About sixteen, the street's big sister. So:
  * SHE IS UPRIGHT AND NEAT. Feet together, chin up, nothing wasted. Where Bebang is planted
    wide and stomps, she is narrow and light.
  * HER FEET ARE THE CLEVER PART. Piko (hopscotch) and Chinese garter are hers, and both show.
  * SHE IS AN ATE. She minds the small ones: a hand on her hip and a wagging hand.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops her left arm from straight out to hanging.

ARMS UP GO THROUGH THE ELBOW (section 13 rule 8, amended by the owner on Dante: "the jump looks
like he's shrugging"). Her elbow is at the mouth of her bell sleeve, 82 mm from the shoulder;
the bare forearm and the fist ride the forearm bone, 116 mm more to the fist's end. A straight
arm cannot rise above level (her ears and buns are over it), so the upper arm lifts a little and
the forearm folds up, and may stretch (scale on the forearm bone) as it is thrown.

Each clip keeps the LENGTH of the one it replaces (walk 0.72 s, sprint 0.48, jump 0.50,
fall 0.33); the idle is ACTED and is 8 s, not the old 1.33 (see `idle`). NOT SEEN IN UNITY: the
game layers procedural motion on these bones, and the `root` scale and forearm scale channels
are new.
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



def _hop(t, at, half):
    """A thrown arc: 1 at `at`, 0 from `half` either side of it."""
    k = (t - at) / half
    return max(0.0, 1.0 - k * k)


#   where her LEFT arm points in each stance: (upper arm, forearm), in the file's space (+x her
#   left, +y up, +z ahead). The right is the mirror.
#   HANGING, HER WAY: arms easy at her sides, elbows soft, hands a little ahead of her hips.
ARMS_HANG = ((0.58, -0.81, 0.02), (0.46, -0.86, 0.22))
#   THE HAND ON THE HIP (her left): the elbow out and a little back, the fist set on her waist.
ARM_HIP = ((0.93, -0.30, -0.20), (-0.46, -0.78, 0.42))
#   THE WAGGING HAND (her right, written as a left arm and mirrored): the elbow by her ribs, the
#   forearm stood up in front of her shoulder. It ticks side to side from there.
ARM_WAG = ((0.50, -0.62, 0.60), (0.10, 0.92, 0.38))
WAG_REACH = 0.20
#   ARMS OUT FOR BALANCE, on one foot
ARM_BALANCE = ((0.95, -0.28, 0.04), (0.90, -0.40, 0.16))
#   THE KICK: the arm on the kicking side thrown back, the other out ahead of her
ARM_KICK_BACK = ((0.70, -0.55, -0.45), (0.55, -0.60, -0.58))
ARM_KICK_FORE = ((0.72, -0.30, 0.62), (0.40, 0.05, 0.92))
#   HANDS TOGETHER, low in front of her: how she stands when she has made her point
ARM_CLASP = ((0.50, -0.82, 0.28), (-0.62, -0.62, 0.48))
CLASP_REACH = 0.12
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of the street's big sister waiting her turn, in three things she does:

      THE ATE (0.5 to 2.6 s). Her left fist goes to her hip, her right hand comes up beside her
      shoulder and WAGS, three ticks, at somebody small off to her left and then somebody off to
      her right, her head tipped and leaning in over them. Not cross: she has seen it all before.
      PIKO (2.9 to 5.4 s). She lifts her left foot behind her, arms out, and hops twice on the
      right, then springs to a landing with both feet apart, then hops them together again: one,
      one, two, one, the squares of a hopscotch court she is drawing in her head. She watches her
      own feet while she does it.
      THE GARTER KICK (5.7 to 6.9 s). One dip, and her right leg swings up high in front of her,
      over a garter nobody is holding, her right arm thrown back and her left out ahead; she
      comes down on both feet together. Then (6.8 to 7.8 s) hands together in front of her, chin
      up, pleased with it, and back to the stand.

    WHY THESE. The character select calls her "a champion of playground line games, trying a
    new court. The footwork came with her", and her gait is "the composed older sister: upright,
    chin up, feet placed neatly". So she waits the way that girl waits: minding the small ones,
    and keeping her feet warm with the two games she is best at. None of the three is another
    character's (Bebang: fist into palm, the headband, a shoulder roll; Dante: fists on hips
    and folded arms; Sean: a flex, a lean, a guard; Amihan and Cheska: bounces on both feet and
    hands behind the back). Her hops are on ONE foot and to a count, with the other leg held up.

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
    hip = _arm(1, *ARM_HIP)
    wag = _arm(-1, *ARM_WAG)
    balance = {1: _arm(1, *ARM_BALANCE), -1: _arm(-1, *ARM_BALANCE)}
    kick_arm = {1: _arm(1, *ARM_KICK_FORE), -1: _arm(-1, *ARM_KICK_BACK)}
    clasp = {1: _arm(1, *ARM_CLASP), -1: _arm(-1, *ARM_CLASP)}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 5.0))
        ate = _window(t, 0.5, 2.6, 0.36)
        piko = _window(t, 2.9, 5.4, 0.30)
        kick = _window(t, 5.7, 6.75, 0.22)
        proud = _window(t, 6.72, 7.80, 0.30)
        # the wag: three ticks, and which way she is looking while she does it
        ticking = _window(t, 0.95, 2.30, 0.18)
        tick = math.sin(2.0 * math.pi * (t - 0.95) / 0.45) * ticking
        look = ate * (20.0 * _window(t, 0.6, 1.65, 0.32) - 20.0 * _window(t, 1.55, 2.55, 0.32))
        # piko: the left foot held up behind her, two hops on the right, a spring to both feet
        # apart, a hop back together
        held = _window(t, 3.02, 4.50, 0.20)
        hops = _hop(t, 3.50, 0.15) + _hop(t, 3.96, 0.15)
        spring = _hop(t, 4.44, 0.17)
        close = _hop(t, 4.98, 0.15)
        apart = _window(t, 4.36, 5.04, 0.14)
        lands = (_bump(t, 3.33, 0.045) + _bump(t, 3.72, 0.05) + _bump(t, 4.17, 0.05) + _bump(t, 4.66, 0.06) + _bump(t, 5.17, 0.06))
        # the garter kick: a dip, the swing, the landing
        swing = _bump(t, 6.22, 0.15)
        dips = _bump(t, 5.95, 0.07) + _bump(t, 6.56, 0.07)
        air = 0.045 * hops + 0.060 * spring + 0.045 * close + 0.018 * swing
        dip = 0.030 * lands + 0.035 * dips
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            first = hip if side > 0 else wag
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(_mix(_mix(_mix(hang[side][k], first[k], ate), balance[side][k], piko), kick_arm[side][k], kick), clasp[side][k], proud)
                if side < 0 and k == 1:
                    q = mul(q, qz(15.0 * tick * ate))
                if k == 0:
                    # the arms give with every landing and float on every hop
                    q = mul(q, qz(side * (9.0 * (hops + spring + close) - 10.0 * lands) * piko))
                row[(bone, "rotation")] = q
            reach = (WAG_REACH * ate if side < 0 else 0.0) + CLASP_REACH * proud
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        nod = 6.0 * ate + 13.0 * piko * (1.0 - 0.6 * apart) - 5.0 * swing * kick - 7.0 * proud + 3.0 * lands
        tilt = -7.0 * ate * (1.0 if look >= 0 else -1.0) * min(1.0, abs(look) / 8.0) + 3.0 * tick * ate
        row.update({
            ("root", "translation"): (-0.020 * held + 0.014 * swing, air - 0.10 * dip, 0.0),
            ("root", "scale"): _squash(1.0 + 0.008 * breath - dip + 0.035 * (hops + spring + close)),
            ("root", "rotation"): qz(-1.6 * held + 2.0 * ate * (1.0 if look >= 0 else -1.0) * min(1.0, abs(look) / 8.0)),
            ("torso", "rotation"): mul(mul(qx(5.0 * ate + 4.0 * piko - 9.0 * swing + 2.5 * dips - 2.0 * proud + 0.8 * breath), qy(0.30 * look + 8.0 * swing)),
                                       qz(2.0 * held)),
            ("head", "rotation"): mul(mul(qy(0.80 * look), qx(nod - 0.6 * breath)), qz(tilt)),
            # feet together and neat; the left held up behind her for the hops; both apart for
            # the two; the right swung up for the kick
            ("leg-left", "rotation"): mul(qx(58.0 * held + 5.0 * swing), qz(1.0 + 15.0 * apart + 3.0 * held)),
            ("leg-right", "rotation"): mul(qx(-86.0 * swing), qz(-1.0 - 15.0 * apart - 4.0 * swing)),
        })
        _add(out, t, row)
    return out


def walk():
    """The composed older sister: upright, chin up, the feet set down one in front of the other
    on a line, a small sway at the hip, the arms swinging small and soft with the elbows barely
    bent. Nothing bounces. (Bebang stomps and pumps her fists; this is the other way to walk.)"""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.85)
        c = math.cos(phase)
        lift = abs(s) ** 1.6
        sway = math.sin(phase - 0.35)
        reach_left = max(0.0, -sh)                # an arm comes forward as its own leg goes back
        reach_right = max(0.0, sh)
        _add(out, t, {
            ("root", "translation"): (0.010 * sway, 0.001 + 0.014 * lift, 0.0),
            ("root", "rotation"): mul(qx(0.5), qz(3.2 * sway)),
            ("root", "scale"): _squash(0.985 + 0.030 * lift),
            ("leg-left", "rotation"): mul(qx(-28.0 * sh), qz(-2.5 - 3.2 * sway)),
            ("leg-right", "rotation"): mul(qx(28.0 * sh), qz(2.5 - 3.2 * sway)),
            ("torso", "rotation"): mul(mul(qx(-1.0), qy(5.0 * sh)), qz(-4.2 * sway)),
            ("head", "rotation"): mul(mul(qx(-5.0 + 1.0 * abs(c)), qy(-4.0 * sh)), qz(2.0 * sway)),
            ("arm-left", "rotation"): mul(qx(2.0 + 15.0 * sh), qz(-61.0)),
            ("arm-right", "rotation"): mul(qx(2.0 - 15.0 * sh), qz(61.0)),
            ("forearm-left", "rotation"): mul(qx(-10.0), qy(-(14.0 + 10.0 * reach_left))),
            ("forearm-right", "rotation"): mul(qx(-10.0), qy(14.0 + 10.0 * reach_right)),
        })
    return out


def sprint():
    """Quick and light, the way she runs to the far square: up on her toes, a small lean, her
    chin still level, the elbows folded and tucked in close and working fast, the feet flicking
    out behind her."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.8)
        lift = abs(s) ** 1.4
        rock = math.sin(phase - 0.3)
        _add(out, t, {
            ("root", "translation"): (0.0, 0.010 + 0.034 * lift, 0.0),
            ("root", "rotation"): mul(qx(9.0), qz(2.0 * rock)),
            ("root", "scale"): _squash(0.965 + 0.075 * lift),
            # more behind than in front: the heels flick
            ("leg-left", "rotation"): mul(qx(6.0 - 48.0 * sh), qz(-1.0 - 2.0 * rock)),
            ("leg-right", "rotation"): mul(qx(6.0 + 48.0 * sh), qz(1.0 - 2.0 * rock)),
            ("torso", "rotation"): mul(mul(qx(3.0), qy(8.0 * sh)), qz(-2.5 * rock)),
            ("head", "rotation"): mul(mul(qx(-11.0 + 1.5 * lift), qy(-6.0 * sh)), qz(1.2 * rock)),
            ("arm-left", "rotation"): mul(qx(34.0 * sh), qz(-72.0)),
            ("arm-right", "rotation"): mul(qx(-34.0 * sh), qz(72.0)),
            ("forearm-left", "rotation"): mul(qx(-8.0), qy(-(64.0 - 12.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-8.0), qy(64.0 + 12.0 * sh)),
        })
    return out


#   arms up: where the left arm points at the top of the jump. Her buns stand high and wide
#   of her head, so the V is thrown up and well out, clear of them.
ARM_UP = ((0.72, 0.62, 0.10), (0.42, 0.90, 0.06))
ARM_UP_REACH = 0.95


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. Hers is the jump of a girl who has cleared a garter at
    shoulder height: one clean spring off both feet, both arms thrown up in a V through the
    elbows, and her heels tucked up neatly behind her, feet still together."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.11)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.12)
        ring = math.cos(2.0 * math.pi * t / 0.44) * math.exp(-t / 0.14)
        row = {
            ("root", "scale"): _squash(1.0 + 0.17 * ring),
            ("root", "rotation"): qx(1.5 * hang),
            # both legs the same: straight under her at take-off, then the heels tucked up behind
            ("leg-left", "rotation"): mul(qx(-6.0 * rise + 26.0 * hang), qz(-1.0)),
            ("leg-right", "rotation"): mul(qx(-6.0 * rise + 26.0 * hang), qz(1.0)),
            ("torso", "rotation"): qx(-5.0 * rise - 3.0 * hang),
            ("head", "rotation"): qx(-10.0 * rise - 3.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 7.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.72 + 0.28 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: she comes down upright and tidy, toes pointed under her and feet together,
    arms up in the V with the hands fluttering a little, looking for her landing square."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.05 + 0.012 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(2.0), qz(1.0 * s)),
            ("leg-left", "rotation"): mul(qx(8.0 + 5.0 * s), qz(0.5)),
            ("leg-right", "rotation"): mul(qx(8.0 - 5.0 * s), qz(-0.5)),
            ("torso", "rotation"): mul(qx(3.0), qz(-1.2 * s)),
            ("head", "rotation"): mul(qx(12.0 + 1.5 * c), qz(-1.0 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(4.0 * s * side))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(7.0 * c))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.64 + 0.05 * s), 1.0, 1.0)
        _add(out, t, row)
    return out


# ======================================================================================
# THE ACTION CLIPS (2026-10-07). Thirteen clips that were copied from the old shared rig,
# which had no elbow. Eight keep the LENGTH of the clip they replace and the TIME of its key
# beat, measured off this .glb before they were replaced. Five are only looked at in the game
# and never timed against, and were a sixth of a second long, so they are longer now.
#
#   clip                 old      the old key beat                           here
#   holding-right-shoot  0.2000   the chest furthest over at 0.067           0.2000, arm furthest out at 0.067
#   pick-up              0.3333   lowest at 0.167                            0.3333, lowest at 0.167
#   attack-melee-right   0.4167   furthest out at 0.250                      0.4167, furthest out at 0.250
#   attack-melee-left    0.4167   furthest out at 0.250                      0.4167, the bump lands at 0.250
#   interact-right       0.6667   out by 0.167, held to 0.40, home by 0.667  0.6667, the same three times
#   interact-left        0.6667   the same                                   0.6667, the same
#   slide                0.9500   down by 0.133, lowest 0.25, reach 0.333    0.9500, the same three times
#   die                  0.3333   in the air at 0.10, down by 0.233          0.3333, the same two times
#   holding-right        0.1667   a held pose                                a loop of 2.0 s
#   crouch               0.1667   a held pose                                a loop of 1.8 s
#   sit                  0.1667   a held pose                                0.8 s of getting down
#   emote-yes            0.6667   a nod                                      a loop of 1.0 s
#   emote-no             0.6667   a shake                                    a loop of 1.2 s
#
# THE THROW AND THE TAG ARE HALF THE GAME'S. While they play, code poses the chest, the head
# and both UPPER arms over the clip. What is left of the clip is the root, the legs and the
# two FOREARMS. So in those two she is in the root and the legs (the knee lift, the step, the
# rise onto her toes) and each forearm is a plain bend of the elbow with no twist, opening to
# straight at the key beat.
#
# HOW SHE DOES THEM. She is sixteen, the street's ate, a champion of piko and Chinese garter:
# upright, chin up, feet neat, and a little bit of a performance in everything. She carries
# the slipper low and away from her jeans and smacks it on her thigh. She picks things up with a curtsy, her back
# never bending. Her slide is a garter girl's front split. She tags like a fencer and shoves
# with her elbow like the canteen line. She sits side saddle and sees to her hair. Knocked
# down, she goes over sideways as stiff as a doll.
#
# WHERE THE FISTS ARE at each key beat (no two the same):
#   carry          right low and wide of her hip, left folded across her waist; a toe tapping
#   throw          right long overhead and ahead, left swept back; up on the front foot
#   pick up        a curtsy: feet scissored, back upright; right straight down beside her, left floated up
#   tag            right dead straight ahead, left dead straight behind; on her toes
#   shove          side on, left ELBOW leading with the fist folded to her chest, right trailing
#   reach right    right low ahead at hip height, patting; left tucked behind her back
#   reach left     left up ahead at face height, scooping "come here"; right hanging
#   slide          a front split; right past her front foot, left thrown up behind
#   out of breath  folded over; right on her knee, left fanning her face
#   sit            legs laid to her left; right in her lap, left up at her hair
#   knocked down   rigid on her left SIDE, facing front; both straight down her sides
#   yes            both out wide at shoulder height, then clapped together ahead of her chin
#   no             left on her hip, right up high and ticking
# ======================================================================================
HIP = (0.0836, 0.17625, -0.02875)     # the leg bones, in the root's space; the sole is LEG below
LEG = 0.17625
LEGS_STAND = (qz(1.0), qz(-1.0))


def _inv(q):
    return (-q[0], -q[1], -q[2], q[3])


def _arm_in(side, upper, fore, parent):
    """`_arm`, with the two directions given in the space ABOVE `parent` (the rotation the
    arm's parent has there). x is OUT from her side for either arm, y up, z ahead."""
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


def _lerp(a, b, t):
    return tuple(tuple(p + (q - p) * t for p, q in zip(u, v)) for u, v in zip(a, b))


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


#   THE CARRY. Somebody's street slipper, so it is held LOW and well out from her jeans, her
#   right elbow in and the fist out wide of her hip where it shows from behind; her left arm
#   is folded across her waist.
ARM_RULER = ((0.66, -0.72, 0.20), (0.86, -0.36, 0.36))
ARM_RULER_DOWN = ((0.62, -0.77, 0.14), (0.22, -0.94, 0.26))
ARM_PALM = ((0.50, -0.80, 0.33), (-0.80, -0.12, 0.58))
RULER_REACH = 0.45


def _carry(tap=0.0, toe=0.0, sway=0.0, breath=0.0, look=0.0):
    """The carry. `tap` 1 is the slipper smacked on her own thigh; `toe` 1 is her front toe up."""
    return _pose(tx=0.008 * sway, root=qz(-1.2 * sway), sy=0.99 + 0.010 * breath - 0.030 * tap,
                 torso=mul(qx(1.0 * breath + 4.0 * tap), qy(7.0 - 5.0 * tap)),
                 head=mul(mul(qy(-6.0 + look - 6.0 * tap), qx(-4.0 + 12.0 * tap)), qz(5.0 - 3.0 * sway)),
                 left=ARM_PALM, right=_lerp(ARM_RULER, ARM_RULER_DOWN, tap),
                 reach=(0.15, RULER_REACH - 0.30 * tap),
                 # her weight on the right foot, the left set a little ahead with its toe tapping
                 legs=(mul(qx(-9.0 - 9.0 * toe), qz(2.0)), mul(qx(2.0), qz(-2.0))))


def holding_right():
    """Carrying the tsinelas, a loop of 2 s. She stands with her weight on one foot and the
    slipper held low and out from her hip, the other arm folded across her waist, and her
    front toe tapping the whole time (four to the loop: her feet never stop). Twice (0.82 s
    and 1.14 s) she smacks the slipper on her own thigh and it springs back out, her chin
    dipping with it: she is waiting for somebody to try it."""
    length = 2.0
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        tap = min(1.0, _bump(t, 0.82, 0.075) + _bump(t, 1.14, 0.075))
        toe = max(0.0, math.sin(4.0 * p)) ** 0.7
        _add(out, t, _carry(tap=tap, toe=toe, sway=math.sin(p), breath=math.sin(2.0 * p), look=14.0 * math.sin(p)))
    return out


def holding_right_shoot():
    """THE THROW, what is hers of it (the game points her chest and upper arms). A garter
    girl's throw, all in the legs: her front knee snaps up high and she sinks on the back foot
    (0.033 s), then she steps down through it and goes up long onto that front foot, the elbow
    snapping straight and the forearm thrown long with her back leg left out straight behind
    her (0.067 s); she tips over the front foot with the back heel flicked up, and drops back
    into the carry."""
    cocked = _pose(tz=-0.012, sy=0.90, root=mul(qx(-5.0), qy(-12.0)), torso=qx(-3.0), head=mul(qy(12.0), qx(4.0)), space="world",
                   left=((0.30, -0.15, 0.94), (0.10, 0.05, 0.99)),
                   right=((0.58, 0.12, -0.80), (0.25, 0.92, -0.30)), reach=(0.20, 0.25),
                   legs=(mul(qx(-62.0), qz(2.0)), mul(qx(5.0), qz(-2.0))))
    out = _pose(tz=0.050, sy=1.11, root=mul(qx(11.0), qy(13.0)), torso=qx(4.0),
                head=mul(qy(-13.0), qx(-13.0)), space="world",
                left=((0.70, -0.40, -0.59), (0.55, -0.30, -0.78)),
                right=((0.16, 0.42, 0.89), (0.02, 0.22, 0.97)), reach=(0.15, 0.90),
                legs=(mul(qx(-23.0), qz(2.0)), mul(qx(27.0), qz(-2.0))))
    through = _pose(tz=0.058, sy=0.93, root=mul(qx(16.0), qy(17.0)), torso=qx(7.0),
                    head=mul(qy(-17.0), qx(-20.0)), space="world",
                    left=((0.72, -0.30, -0.62), (0.60, 0.05, -0.80)),
                    right=((-0.10, -0.50, 0.86), (-0.38, -0.66, 0.65)), reach=(0.10, 0.40),
                    legs=(mul(qx(-16.0), qz(2.0)), mul(qx(46.0), qz(-2.0))))
    _elbows(cocked, (22.0, 0.0), (82.0, 60.0))
    _elbows(out, (30.0, 0.0), (3.0, 0.0))
    _elbows(through, (38.0, 10.0), (24.0, 10.0))
    return _act(0.2, [(0.0, _carry(), "lin"), (1.0 / 30.0, cocked, "out"), (2.0 / 30.0, out, "in"),
                      (0.117, through, "out"), (0.2, _carry(), "smooth")])


def pick_up():
    """She does not bend over for it, she CURTSIES for it. Her feet scissor apart, one ahead
    and one behind, and she drops straight down between them with her back upright and her
    chin up; she leans a little to the right, plucks the slipper off the ground beside her
    with a straight arm, and her left arm floats up and out the way it would for a bow
    (lowest at 0.167 s). The feet snap shut and stand her up on her toes with the slipper
    shown in her hand, and she settles."""
    ready = _pose(sy=1.05, lift=0.006, torso=qx(-3.0), left=((0.80, -0.55, 0.05), (0.80, -0.45, 0.20)),
                  right=((0.70, -0.70, 0.05), (0.60, -0.78, 0.10)))

    def low(sy, part, lean, grab):
        return _pose(sy=sy, torso=mul(qx(2.0), qz(lean)), head=mul(qx(-6.0), qz(-0.9 * lean)), space="root",
                     left=((0.90, 0.12, 0.10), (0.68, 0.68, 0.20)),
                     right=((0.60, -0.80, 0.08), (0.30, -0.95, 0.08)), reach=(0.35, 0.40 + grab),
                     legs=(mul(qx(-part), qz(2.0)), mul(qx(part), qz(-2.0))))

    up = _pose(sy=1.07, lift=0.010, torso=qx(-5.0), head=qx(-8.0),
               left=((0.84, -0.50, 0.0), (0.84, -0.36, 0.20)),
               right=((0.62, -0.50, 0.60), (0.25, 0.80, 0.52)), reach=(0.0, 0.30))
    return _act(1.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 30.0, ready, "out"), (1.0 / 6.0, low(0.90, 52.0, 21.0, 0.20), "in"),
                            (0.2, low(0.95, 48.0, 17.0, 0.0), "out"), (0.283, up, "smooth"), (1.0 / 3.0, _stand(), "smooth")])


def attack_melee_right():
    """THE TAG, what is hers of it (for 0.34 s the game takes her whole body to its own reach
    and leaves the clip the forearms and the root). A touch like a fencer's: a small sink with
    the elbow folded, then she goes up tall onto her toes, feet together, and the forearm
    opens out long and dead straight ahead with the other arm a straight line behind her
    (0.25 s). It stops at the touch. Then what is all hers: heels down, and the hand that
    tagged flicks up by her shoulder, "taya ka", before it falls."""
    wind = _pose(sy=0.90, root=qy(-8.0), torso=qx(-2.0), head=mul(qy(8.0), qx(2.0)),
                 left=((0.50, -0.60, 0.62), (0.10, 0.20, 0.97)),
                 right=((0.60, -0.56, -0.57), (0.20, 0.30, 0.93)),
                 legs=(mul(qx(-3.0), qz(1.0)), mul(qx(3.0), qz(-1.0))))
    _elbows(wind, (40.0, 20.0), (70.0, 30.0))

    def hit(sy, lean, reach, lift):
        row = _pose(tz=0.040, sy=sy, lift=lift, root=mul(qx(lean), qy(10.0)), torso=qx(3.0),
                    head=mul(qy(-10.0), qx(-lean - 2.0)), space="world",
                    left=((0.34, -0.42, -0.84), (0.30, -0.40, -0.87)),
                    right=((0.06, 0.06, 1.0), (0.0, 0.04, 1.0)), reach=(0.35, reach),
                    legs=(mul(qx(-lean - 3.0), qz(1.0)), mul(qx(-lean + 5.0), qz(-1.0))))
        return _elbows(row, (6.0, 0.0), (2.0, 0.0))

    flick = _pose(sy=1.03, torso=qx(-3.0), head=mul(qx(-6.0), qz(-6.0)),
                  left=ARMS_HANG, right=((0.66, -0.60, 0.45), (0.40, 0.85, 0.34)), reach=(0.0, 0.25))
    return _act(5.0 / 12.0, [(0.0, _stand(), "lin"), (0.12, wind, "out"), (0.25, hit(1.10, 9.0, 0.90, 0.012), "smooth"),
                             (0.29, hit(1.0, 11.0, 0.65, 0.0), "out"), (0.355, flick, "smooth"), (5.0 / 12.0, _stand(), "smooth")])


def attack_melee_left():
    """THE SHOVE, the way an ate gets through the canteen line: with her elbow. She turns side
    on and sinks away from you with the left fist folded to her chest (0.14 s), then steps
    wide onto her left foot and her whole side arrives, left ELBOW first and her shoulder
    behind it, the right arm and the right leg trailing out in one line (0.25 s). She never
    stops looking at you. She lands squashed on the step and draws her feet together again."""
    elbow = ((0.97, 0.16, 0.20), (-0.72, 0.12, 0.68))

    def side(tz, sy, turn, tip, step, trail, reach):
        return _pose(tz=tz, sy=sy, root=mul(qy(-turn), qz(-tip)), torso=qz(-0.35 * tip),
                     head=mul(qy(0.86 * turn), qz(0.9 * tip)),
                     left=elbow, right=((0.80, -0.56, -0.20), (0.90, -0.40, 0.10)), reach=(0.0, reach),
                     legs=(qz(step + tip), qz(-trail + tip)))

    wind = side(-0.030, 0.88, 34.0, -9.0, 4.0, 12.0, 0.0)
    return _act(5.0 / 12.0, [(0.0, _stand(), "lin"), (0.14, wind, "out"), (0.25, side(0.110, 1.07, 66.0, 21.0, 12.0, 34.0, 0.45), "in"),
                             (0.31, side(0.110, 0.92, 62.0, 16.0, 14.0, 22.0, 0.25), "out"), (5.0 / 12.0, _stand(), "smooth")])


def interact_right():
    """The right hand is the one that minds the small ones: she bends to somebody about waist
    high with her left hand tucked behind her back, and pats, three times, her head tipped
    over them and her knees giving with each one. Out at 0.167 s, the last pat at 0.40."""
    def pat(down):
        return _pose(sy=0.985 - 0.045 * down, tz=0.012, torso=mul(qx(15.0 + 5.0 * down), qy(13.0)),
                     head=mul(mul(qy(-9.0), qx(4.0 + 5.0 * down)), qz(-11.0)), space="root",
                     left=((0.58, -0.68, -0.45), (-0.52, -0.60, -0.61)),
                     right=((0.26, -0.50 - 0.12 * down, 0.83), (0.04, -0.12 - 0.40 * down, 0.96)),
                     reach=(0.0, 0.45), legs=(mul(qx(-2.0), qz(1.0)), mul(qx(-2.0), qz(-1.0))))

    return _act(2.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 6.0, pat(1.0), "in"), (0.225, pat(0.0), "out"),
                            (0.283, pat(1.0), "in"), (0.342, pat(0.0), "out"), (0.40, pat(1.0), "in"),
                            (0.48, pat(0.3), "out"), (2.0 / 3.0, _stand(), "smooth")])


def interact_left():
    """The left hand calls you over, the Filipino way: the arm goes out wide of her head with
    the hand up, and scoops DOWN and toward her, twice, "halika", her weight rocking back onto
    her heels and her head tipping with each scoop. Up at 0.167 s, the second scoop at 0.40."""
    def call(scoop):
        return _pose(sy=1.0 - 0.02 * scoop, tz=-0.010 * scoop, torso=mul(qx(-4.0 + 3.0 * scoop), qy(-12.0)),
                     head=mul(mul(qy(8.0), qx(-5.0 + 9.0 * scoop)), qz(9.0 - 5.0 * scoop)), space="root",
                     left=((0.70, 0.10 - 0.25 * scoop, 0.70), (0.36 - 0.50 * scoop, 0.88 - 1.40 * scoop, 0.32 + 0.50 * scoop)),
                     right=((0.62, -0.76, -0.16), (0.50, -0.82, 0.26)), reach=(0.85 - 0.25 * scoop, 0.0))

    return _act(2.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 6.0, call(0.0), "out"), (0.235, call(1.0), "in"),
                            (0.315, call(0.0), "out"), (0.40, call(1.0), "in"), (0.48, call(0.75), "out"),
                            (2.0 / 3.0, _stand(), "smooth")])


def slide():
    """The retrieval slide is a garter girl's front SPLIT. A small hop with her arms thrown
    out, and she drops straight down into it, right leg ahead and left behind, her back still
    upright and her chin up (down by 0.133 s, lowest at 0.25); then she folds forward over the
    front leg and her right hand goes out past her foot for the slipper while the left arm
    floats up behind her (0.333 s). Her legs scissor shut underneath her and stand her up, and
    she finishes with her feet together and her arms out: a landing."""
    hop = _pose(sy=1.07, lift=0.022, torso=qx(-4.0), head=qx(-5.0),
                left=((0.90, -0.30, 0.20), (0.80, 0.30, 0.50)), right=((0.90, -0.30, 0.20), (0.80, 0.30, 0.50)),
                legs=(mul(qx(14.0), qz(1.0)), mul(qx(-18.0), qz(-1.0))))

    def split(sy, chest, reach, open_=86.0, lift=0.040):
        return _pose(tz=0.090, sy=sy, lift=lift, torso=mul(qx(chest), qy(8.0)), head=mul(qy(-6.0), qx(-0.8 * chest - 3.0)),
                     space="root",
                     left=((0.62, 0.42, -0.66), (0.42, 0.84, -0.34)),
                     right=((0.18, -0.34 + 0.004 * chest, 0.92), (0.0, -0.30 + 0.004 * chest, 0.95)), reach=(0.45, reach),
                     legs=(mul(qx(open_ - 2.0), qz(2.0)), mul(qx(-open_), qz(-2.0))))

    done = ((0.80, -0.56, 0.20), (0.90, -0.30, 0.30))
    scissor = _pose(tz=0.050, sy=0.98, torso=qx(6.0), head=qx(-6.0), left=done, right=done,
                    legs=(mul(qx(38.0), qz(2.0)), mul(qx(-38.0), qz(-2.0))))
    up = _pose(sy=1.06, lift=0.012, torso=qx(-4.0), head=qx(-7.0), left=done, right=done, reach=(0.2, 0.2))
    settle = _pose(sy=0.975, left=_lerp(done, ARMS_HANG, 0.6), right=_lerp(done, ARMS_HANG, 0.6))
    return _act(0.95, [(0.0, _stand(), "lin"), (0.06, hop, "out"), (0.133, split(1.03, 2.0, 0.30, lift=0.045), "in"),
                       (0.25, split(0.88, 8.0, 0.50, lift=0.034), "out"), (1.0 / 3.0, split(0.95, 34.0, 0.95), "smooth"),
                       (0.39, split(0.97, 28.0, 0.70), "smooth"), (0.52, scissor, "smooth"), (0.64, up, "smooth"),
                       (0.76, settle, "smooth"), (0.86, _stand(), "smooth"), (0.95, _stand(), "lin")])


def crouch():
    """Out of breath, a loop of 1.8 s, and she will not be seen with her head hung: she is
    folded over with her right hand propped on her knee, but her face is UP, and her left hand
    is out in front of it FANNING, quick little flaps, the whole time. Her back heaves three
    times under it. Once, on the third breath, her chin drops and the fan stops for a moment,
    and then she remembers herself. Folded over on every frame, so it can be held on its last."""
    length = 1.8
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        air = 0.5 - 0.5 * math.cos(3.0 * p)                # 0 emptied, 1 full
        droop = _window(t, 1.08, 1.56, 0.20)
        fan = math.sin(9.0 * p) * (1.0 - droop)
        _add(out, t, _pose(tz=-0.030, sy=0.93 + 0.040 * air, torso=mul(qx(50.0 - 10.0 * air), qy(-5.0)),
                           head=mul(qx(-30.0 + 6.0 * air + 34.0 * droop), qz(-5.0 + 3.0 * fan)), space="root",
                           left=((0.34, -0.20 - 0.30 * droop, 0.92), (0.26 + 0.34 * fan, 0.42 - 0.50 * droop, 0.87)),
                           right=((0.60, -0.70 + 0.16 * air, -0.30), (-0.53, -0.78, -0.34)),
                           reach=(0.40, 0.05 + 0.22 * air),
                           legs=(mul(qx(-10.0), qz(5.0)), mul(qx(-10.0), qz(-5.0)))))
    return out


ARM_SMOOTH = ((0.56, -0.74, -0.38), (0.34, -0.84, -0.42))      # both hands behind her, on the way down
ARM_LAP = ((0.48, -0.80, 0.36), (-0.60, -0.12, 0.79))          # both hands folded in her lap


#   a hand up to her hair, under a bun. Her head is wider than her shoulders, so the elbow
#   goes well out and the forearm is long, or the hand is lost inside her hair.
ARM_BUN = ((0.95, 0.25, 0.10), (0.25, 0.96, 0.10))
BUN_REACH = 1.10


def _seat(sy=1.0, back=2.0, kick=86.0, side=50.0, up=0.0, left=ARM_BUN, right=ARM_LAP, reach=(BUN_REACH, 0.12), chin=-3.0, tilt=-8.0):
    """Sat on the ground with both legs laid out together to her LEFT (`side` degrees round
    from straight ahead): leaning `back`, the legs `kick` degrees forward of her, `up` above
    the ground. Her chest leans a little the other way, as it does sat like that."""
    lean = 0.12 * side
    return _pose(root=qx(-back), sy=sy, lift=0.040 * sy + up, torso=mul(qx(back - 1.0), qz(lean)),
                 head=mul(mul(qy(0.2 * side), qx(chin)), qz(tilt - 0.6 * lean)), space="root",
                 left=left, right=right, reach=reach,
                 legs=(mul(qy(side + 6.0), qx(-kick + back)), mul(qy(side - 8.0), qx(-kick + back))))


def sit():
    """She sits the way she was told once a girl sits, legs laid to one side, and then she
    sees to her hair, 0.8 s. A small dip with both hands swept behind her; her feet slide out
    and round to her left as she lowers and she touches down with the smallest bounce
    (0.42 s); both hands go up to her hair under the buns and press it once (0.56 s); then
    the right comes down into her lap and the left stays up at her hair, her head tipped into
    it. That is how she is held: sat side saddle, still fixing her hair."""
    dip = _pose(sy=0.92, tz=-0.010, torso=qx(12.0), head=qx(-8.0), left=ARM_SMOOTH, right=ARM_SMOOTH,
                legs=(mul(qx(-6.0), qz(1.0)), mul(qx(-6.0), qz(-1.0))))
    return _act(0.8, [(0.0, _stand(), "lin"), (0.12, dip, "out"),
                      (0.29, _seat(sy=1.02, back=10.0, kick=58.0, side=20.0, up=0.045, left=ARM_SMOOTH, right=ARM_SMOOTH,
                                   reach=(0.2, 0.2), chin=4.0, tilt=0.0), "smooth"),
                      (0.42, _seat(sy=0.84, back=5.0, kick=88.0, side=46.0, left=ARM_SMOOTH, right=ARM_SMOOTH,
                                   reach=(0.1, 0.1), chin=12.0, tilt=0.0), "in"),
                      (0.56, _seat(sy=1.05, back=0.0, kick=84.0, up=0.008, right=ARM_BUN, reach=(BUN_REACH, BUN_REACH),
                                   chin=-8.0, tilt=0.0), "out"),
                      (0.65, _seat(sy=0.96, right=ARM_BUN, reach=(BUN_REACH - 0.30, BUN_REACH - 0.30), chin=4.0, tilt=0.0), "in"),
                      (0.80, _seat(), "smooth")])


ARM_NEAT = ((0.24, -0.97, 0.02), (0.12, -0.99, 0.06))          # arms straight down her sides, a doll's


def die():
    """Knocked down, and she goes over like a dropped doll. The hit snaps her to attention, up
    off the ground stretched thin with her arms clapped to her sides and her feet together;
    then she tips over SIDEWAYS in one rigid piece, not a joint moving, and lands flat on her
    left side with a bounce that jolts the top leg up and clacks it shut again. She lies there
    on her side exactly as she stood, arms neat, facing front. In the air at 0.10 s, down at
    0.233, and the last frame is held."""
    def doll(tip, lift, scale, leg=0.0, head=0.0):
        # she is carried back the way she came as she goes over, so she lies where she stood
        return _pose(tx=-0.20 * math.sin(math.radians(max(0.0, tip))), lift=lift, ground=False, scale=scale, root=qz(-tip), head=qz(head),
                     left=ARM_NEAT, right=ARM_NEAT, legs=(qz(0.0), qz(-leg)))

    jolt = doll(-6.0, 0.045, _squash(1.12))
    over = doll(40.0, 0.075, _squash(1.06), head=-6.0)
    land = doll(90.0, 0.120, (0.80, 1.05, 1.10), leg=4.0, head=8.0)
    bounce = doll(95.0, 0.165, (1.06, 1.0, 0.97), leg=24.0, head=-5.0)
    rest = doll(90.0, 0.140, (1.0, 1.0, 1.0))
    return _act(1.0 / 3.0, [(0.0, _stand(), "lin"), (0.05, jolt, "out"), (0.133, over, "in"), (0.233, land, "in"),
                            (0.283, bounce, "out"), (1.0 / 3.0, rest, "in")])


ARM_CLAP = ((0.45, -0.42, 0.79), (-0.64, 0.26, 0.72))          # the fists together ahead of her chin
ARM_OPEN = ((0.70, -0.52, 0.49), (0.80, 0.50, 0.33))           # elbows in, fists out wide at shoulder height


def emote_yes():
    """YES, and READY: she claps and skips, a loop of 1 s. Her fists fly out wide at shoulder
    height as she hops onto one foot with the other heel flicked up behind her and her head
    tipped over that way, and come together in a clap ahead of her chin as she lands, held for
    a beat with a little squeeze; then the same on the other foot. Two hops, two claps."""
    def wide(lean):
        heel = (mul(qx(4.0), qz(2.0)), mul(qx(48.0), qz(-3.0))) if lean > 0 else (mul(qx(48.0), qz(3.0)), mul(qx(4.0), qz(-2.0)))
        return _pose(sy=1.07, lift=0.030, tx=0.012 * lean, root=qz(-5.0 * lean), torso=mul(qx(-5.0), qz(-4.0 * lean)),
                     head=mul(qx(-9.0), qz(-10.0 * lean)), left=ARM_OPEN, right=ARM_OPEN, reach=(0.75, 0.75), legs=heel)

    def clap(lean, squeeze):
        return _pose(sy=0.91 + 0.04 * squeeze, tx=0.006 * lean, torso=mul(qx(5.0 - 3.0 * squeeze), qz(-2.0 * lean)),
                     head=mul(qx(4.0 - 8.0 * squeeze), qz(-7.0 * lean)), left=ARM_CLAP, right=ARM_CLAP,
                     reach=(0.80, 0.80), legs=(qz(2.0), qz(-2.0)))

    return _act(1.0, [(0.0, wide(1.0), "lin"), (0.17, clap(1.0, 0.0), "in"), (0.33, clap(1.0, 1.0), "out"),
                      (0.50, wide(-1.0), "out"), (0.67, clap(-1.0, 0.0), "in"), (0.83, clap(-1.0, 1.0), "out"),
                      (1.0, wide(1.0), "out")])


ARM_TICK = ((0.62, -0.12, 0.78), (0.32, 0.90, 0.30))           # her right hand stood up high ahead of her shoulder
TICK_REACH = 0.75


def emote_no():
    """NO, a loop of 1.2 s, and it is the ate's own: her left fist is planted on her hip with
    the elbow out, and her right hand is stood up HIGH beside her head and ticks side to side
    like a metronome, four ticks, as wide as the forearm will go. Her head shakes against the
    hand, she leans back from the idea, her hips go over to the fist side and back, and her
    right foot stamps on the first tick of each pair."""
    length = 1.2
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        tick = _snap(math.sin(2.0 * p), 0.6)
        sway = math.sin(p)
        stamp = max(0.0, math.cos(2.0 * p)) ** 3
        row = _pose(tx=0.010 * sway, root=mul(qy(7.0 * tick), qz(-2.0 * sway)), sy=0.985 - 0.03 * (1.0 - stamp) * abs(tick),
                    torso=mul(qx(-7.0), qz(-3.0 * sway)), head=mul(mul(qy(-30.0 * tick), qx(-4.0)), qz(4.0 * tick)),
                    left=ARM_HIP, right=ARM_TICK, reach=(0.0, TICK_REACH),
                    legs=(qz(3.0), mul(qx(-14.0 * stamp), qz(-3.0))))
        # it ticks from upright to well out, never in: inward it goes behind her own head
        row[("forearm-right", "rotation")] = mul(row[("forearm-right", "rotation")], qz(16.0 + 24.0 * tick))
        _add(out, t, row)
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
         "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
         "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
         "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
         "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no}
