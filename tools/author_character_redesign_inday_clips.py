"""New locomotion clips for the Inday redesign PROTOTYPE.

Imported by tools/author_character_redesign_inday.py, which writes them into the prototype
.glb IN PLACE OF the clips of the same name copied from character-female-a.glb. Nothing in the
game reads them; character-female-a.glb keeps its own. Every other clip in the .glb (slide, sit,
the emotes, the holding and attack clips, 28 in all) is copied across untouched. A copy of
Bebang's clips script rewritten for her: each character's motion is its OWN
(docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so none of the numbers below are another's.

WHAT HER MOTION SHOULD SAY ABOUT HER. INDAY is "the playful girl: neat little skipping steps, a
tilted head, a light bouncing run" (`GaitStyles.Inday`); she "minds the corner stall and is
afraid of absolutely nothing that walks past it" (the character select); "the all-rounder with
no weak column" (GAME_OVERVIEW.md); an "ice-cold bakery prodigy ... a cheeky cat smirk" (her
tagline). A girl of about twelve who works a bakery stall, not a fighter. So:
  * SHE IS LIGHT. She stands on one hip, her weight is never square, and when she moves she
    skips: the lift is long and the landing is soft.
  * HER HEAD IS TILTED, to one side or the other, almost all the time.
  * HER HANDS ARE A BAKER'S: they wipe on her apron and knock the flour off each other.
  * SHE IS UNBOTHERED. Nothing she does is hurried and nothing is a threat.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops her left arm from straight out to hanging.

ARMS UP GO THROUGH THE ELBOW (section 13 rule 8, amended by the owner on Dante: "the jump looks
like he's shrugging"). Her elbow is 82 mm from the shoulder; the bare forearm and the fist ride
the forearm bone, 116 mm more to the fist's end. A straight arm cannot rise above level (her
head is over it), so the upper arm lifts a little and the forearm folds up, and may stretch
(scale on the forearm bone) as it is thrown. HER PONYTAIL hangs out past her right ear, behind
the line of her shoulders: her right arm goes up IN FRONT of it.

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



#   where her LEFT arm points in each stance: (upper arm, forearm), in the file's space (+x her
#   left, +y up, +z ahead). The right is the mirror.
#   HANGING, HER WAY: close to her sides, the forearms a little forward and in. Easy.
ARMS_HANG = ((0.46, -0.88, 0.04), (0.30, -0.93, 0.20))
#   THE APRON. Both hands flat on the front of her apron; she wipes them down it. HIGH is at
#   the tie, LOW is at the hem. The forearms reach in across her front.
ARM_WIPE_HIGH = ((0.50, -0.80, 0.33), (-0.50, -0.50, 0.70))
ARM_WIPE_LOW = ((0.46, -0.85, 0.26), (-0.36, -0.86, 0.36))
WIPE_REACH = 0.22
#   THE CLAP. Her hands in front of her chest, apart, and then together: the flour knocked off.
ARM_CLAP_OPEN = ((0.52, -0.62, 0.59), (-0.20, 0.30, 0.93))
ARM_CLAP_SHUT = ((0.42, -0.64, 0.64), (-0.78, 0.26, 0.57))
CLAP_REACH = 0.42
#   THE PONYTAIL. Her right hand (written as a left arm, mirrored below) goes up beside her
#   head to the tail where it falls past her ear, lifts it and flicks it back over her shoulder.
#   (v02 held the fist beside her cheek and it read as a wave: it goes up OUT at her side and
#   back, under the fall of the tail)
ARM_TAIL = ((0.92, 0.30, 0.02), (0.38, 0.90, -0.12))
ARM_TAIL_FLICK = ((0.90, 0.36, -0.20), (0.74, 0.56, -0.38))
TAIL_REACH = 0.70
#   THE HIP. Her left fist on her left hip, the elbow out to the side and a little back.
ARM_HIP = ((0.86, -0.46, -0.22), (-0.66, -0.68, 0.32))
HIP_REACH = 0.10
IDLE_LENGTH = 8.0


def _pair_mix(a, b, t):
    return tuple(_mix(a[k], b[k], t) for k in range(2))


def idle():
    """Eight seconds of a girl minding a stall with nobody at it, in three things she does:

      THE APRON (0.5 to 3.0 s). Both hands go to the front of her apron and wipe down it, twice,
      her chin tucked to watch them; then they come up in front of her chest and knock the
      flour off each other, clap, clap, her head coming up and round to her left on the second.
      THE PONYTAIL (3.3 to 5.3 s). Her head tips to her left, her right hand goes up to the tail
      where it hangs past her ear, and she flicks it back over her shoulder with a toss of her
      head. The hand comes down slowly.
      THE HIP (5.6 to 7.8 s). Her left fist goes onto her hip, her weight drops onto her left
      leg, and her right foot taps, three times, toe up and down, while she looks down the
      street to her right with her head on one side. Then the weight comes back to the middle
      with one small bounce.

    WHY THESE. She is the girl from the corner bakery ("minds the corner stall and is afraid of
    absolutely nothing that walks past it"), so she waits the way a girl waits behind a counter:
    wiping her hands, fixing her hair, and standing with a fist on her hip tapping her foot at
    a street with no customers in it. None of the three is another character's (Dante: both
    fists on his hips and folded arms; Sean: a flex, a bull lean, a guard; Amihan and Cheska:
    hops and hands behind the back; Bebang: fist into palm, her headband, a shoulder roll;
    Bayan and Lola Pacing have their own). ONE fist on ONE hip with the foot tapping is hers.

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 5.0))
        apron = _window(t, 0.5, 3.0, 0.35)
        tail = _window(t, 3.3, 5.3, 0.45)
        hip = _window(t, 5.6, 7.8, 0.40)
        # the apron: two wipes down it (0.85 to 1.75 s), then the hands come up for two claps
        wipe = 0.5 - 0.5 * math.cos(2.0 * math.pi * _ease((t - 0.85) / 0.90) * 2.0)
        clapping = _ease((t - 1.80) / 0.28)
        shut = max(_ease((t - 2.14) / 0.07) * (1.0 - _ease((t - 2.24) / 0.14)), _ease((t - 2.48) / 0.07) * (1.0 - _ease((t - 2.58) / 0.20)))
        jolt = _bump(t, 2.21, 0.05) + _bump(t, 2.55, 0.05)
        # the ponytail: the hand is at the tail by 3.9 s, the flick is at 4.3 s
        flick = _ease((t - 4.22) / 0.12) * (1.0 - _ease((t - 4.42) / 0.40))
        toss = _bump(t, 4.34, 0.11)
        # the hip: three taps of the right foot
        tapping = _window(t, 6.05, 7.45, 0.20)
        tap = tapping * (0.5 - 0.5 * math.cos(2.0 * math.pi * (t - 6.05) / 0.46))
        settle = _bump(t, 7.78, 0.09) - 0.5 * _bump(t, 7.93, 0.08)
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            # her hands wipe one after the other, not together
            w = wipe if side > 0 else 0.5 - 0.5 * math.cos(2.0 * math.pi * _ease((t - 0.97) / 0.90) * 2.0)
            first = _pair_mix(_pair_mix(_arm(side, *ARM_WIPE_HIGH), _arm(side, *ARM_WIPE_LOW), w),
                              _pair_mix(_arm(side, *ARM_CLAP_OPEN), _arm(side, *ARM_CLAP_SHUT), shut), clapping)
            second = _pair_mix(_arm(side, *ARM_TAIL), _arm(side, *ARM_TAIL_FLICK), flick) if side < 0 else hang[side]
            third = _arm(side, *ARM_HIP) if side > 0 else hang[side]
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                row[(bone, "rotation")] = _mix(_mix(_mix(hang[side][k], first[k], apron), second[k], tail), third[k], hip)
            reach = apron * (WIPE_REACH * (1.0 - clapping) + CLAP_REACH * clapping * (0.55 + 0.45 * shut))
            if side < 0:
                reach += TAIL_REACH * tail * (1.0 - 0.35 * flick)
            else:
                reach += HIP_REACH * hip
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        # her head: down at her hands, up and left on the second clap; tipped left for the tail
        # and tossed; on one side, looking right down the street, at the hip
        look = 22.0 * apron * _ease((t - 2.40) / 0.30) + 10.0 * tail - 26.0 * hip
        nod = 15.0 * apron * (1.0 - clapping) + 3.0 * jolt - 3.0 * tail - 9.0 * toss
        tilt = -5.0 * apron * clapping + 11.0 * tail + 6.0 * toss + 9.0 * hip + 1.5 * tap
        dip = 0.016 * jolt + 0.012 * toss + 0.040 * settle + 0.006 * tap
        lean = hip * (1.0 - 0.0 * tap)
        row.update({
            ("root", "translation"): (0.016 * lean, -0.10 * dip - 0.004 * lean, 0.0),
            ("root", "scale"): _squash(1.0 + 0.010 * breath - dip),
            ("root", "rotation"): qz(3.2 * lean),
            ("torso", "rotation"): mul(mul(qx(5.0 * apron * (1.0 - clapping) + 2.0 * jolt + 0.8 * breath), qy(0.3 * look + 5.0 * flick)),
                                       qz(-5.5 * lean + 2.5 * tail)),
            ("head", "rotation"): mul(mul(qy(0.7 * look), qx(nod - 0.6 * breath)), qz(tilt)),
            # one hip dropped: the left leg under her, the right one out a little and tapping
            ("leg-left", "rotation"): qz(2.5 - 3.2 * lean),
            ("leg-right", "rotation"): mul(qx(-9.0 * lean - 7.0 * tap), qz(-2.5 - 7.0 * lean)),
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


def sit():
    """Getting down, 0.8 s: a small skip with her arms out, her feet go out ahead of her
    together and she lands on her seat, lightly, with one bounce (0.34 s); then her elbows
    come in and both fists go up under her chin, her head goes over on one side, and she is
    a girl waiting behind a counter. The last frame is held."""
    hop = _pose(sy=1.06, lift=0.030, torso=qx(-3.0), head=qx(-5.0), left=ARM_OUT, right=ARM_OUT, reach=(0.2, 0.2),
                legs=(mul(qx(-16.0), qz(2.0)), mul(qx(-16.0), qz(-2.0))))
    return _act(0.8, [(0.0, _stand(), "lin"), (0.10, hop, "out"),
                      (0.23, _seat(sy=1.04, back=6.0, kick=56.0, up=0.060, chin=0.0, nod=0.0, tilt=0.0), "smooth"),
                      (0.34, _seat(sy=0.82, back=2.0, kick=88.0, chin=0.0, nod=16.0, tilt=3.0), "in"),
                      (0.45, _seat(sy=1.05, back=3.0, kick=78.0, up=0.016, chin=0.35, nod=0.0, tilt=5.0), "out"),
                      (0.60, _seat(sy=0.96, chin=1.0, nod=9.0, tilt=13.0), "smooth"),
                      (0.80, _seat(), "smooth")])


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


#   YES. Her right fist up beside her head, clear of her hair, and flicked out high from it.
ARM_SALUTE = ((0.86, 0.28, 0.42), (0.34, 0.90, 0.28))
ARM_FLICK = ((0.80, 0.46, 0.38), (0.72, 0.62, 0.30))
ARM_TUCK = ((0.50, -0.62, -0.60), (-0.56, -0.50, -0.66))     # the other fist in the small of her back


def emote_yes():
    """YES, and READY, a loop of 1.2 s: aye aye. Her right fist is up beside her head the
    whole time, her left tucked behind her back, her weight on one hip. She dips with a nod,
    then FLICKS the fist out high and wide with a bounce off her toes and her head thrown
    over the other way (0.30 s), holds it there, brings it back to her head with a second
    nod (0.66 s), and gives it one more, smaller (0.88 s)."""
    def at(flick, sy, lift, nod, tilt, hip):
        arm = (_lerp3(ARM_SALUTE[0], ARM_FLICK[0], flick), _lerp3(ARM_SALUTE[1], ARM_FLICK[1], flick))
        return _pose(sy=sy, lift=lift, tx=0.012 * hip, root=qz(3.0 * hip), torso=mul(mul(qx(0.4 * nod), qy(6.0 - 12.0 * flick)), qz(-5.0 * hip)),
                     head=mul(qx(nod), qz(tilt)), left=ARM_TUCK, right=arm, reach=(0.0, 0.62 + 0.55 * flick),
                     legs=(qz(2.5 - 3.0 * hip), mul(qx(-6.0 * hip), qz(-2.5 - 5.0 * hip))))

    home = at(0.0, 1.0, 0.0, 2.0, 8.0, 1.0)
    return _act(1.2, [(0.0, home, "lin"), (0.16, at(0.0, 0.90, 0.0, 13.0, 4.0, 0.6), "out"),
                      (0.30, at(1.0, 1.09, 0.030, -12.0, -12.0, -0.6), "out"), (0.50, at(0.95, 1.02, 0.0, -8.0, -13.0, -1.0), "smooth"),
                      (0.66, at(0.0, 0.92, 0.0, 12.0, 6.0, 0.4), "in"), (0.76, at(0.1, 0.97, 0.0, 4.0, 8.0, 0.8), "out"),
                      (0.88, at(0.75, 1.06, 0.012, -9.0, -8.0, 0.0), "out"), (1.02, at(0.6, 1.01, 0.0, -5.0, -6.0, 0.4), "smooth"),
                      (1.2, home, "smooth")])


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


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
         "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
         "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
         "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
         "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no}
