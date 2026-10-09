"""New locomotion clips for the Bayan (display name BERTO) redesign.

Imported by tools/author_character_redesign_bayan.py, which writes them into the redesign .glb
IN PLACE OF the clips of the same name copied from character-male-f.glb. Every other clip in
that file (static, crouch, sit, slide, die, the emotes, the holding and attack clips and the
rest, 28 of them) is copied across untouched. A copy of the cast's clips script rewritten for
him: each character's motion is its OWN (docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9),
so none of the numbers below are anybody else's.

WHAT HIS MOTION SHOULD SAY ABOUT HIM. His roster line (person_bayan.asset): "Built like a
concrete wall. Slow to chase, but once he plants his feet and winds up a throw, the whole
street clears out." docs/GAME_OVERVIEW.md: "The immovable taya. Slow, hard to shift, punishing
at the can" (BILIS 2, the slowest row). And his face is a scowl. He is a Classic character: a
big neighbourhood man with no powers and no gear, only weight and a throwing arm. So:
  * HE IS HEAVY. Every bounce is low and lands with a squash; the hips sway wide; nothing floats.
  * HE STANDS WIDE. His arms never hang at his sides: they are too thick (109 mm through, on a
    body 252 wide), so they are carried out from the body, as the stock rig's idle has them.
  * THE THROW IS HIS ONE FAST THING. Everything else is slow and sure.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a positive angle carries his left leg OUT to his left.

HIS ARMS, AND WHAT THEY CANNOT DO. The elbow is 130 mm from the shoulder, on his bare arm below
the rolled sleeve; the forearm and the fist ride the forearm bone, 99 mm more to the middle of
the fist. The arm is as thick as that forearm is long and his chest is deep, so a forearm folded
ACROSS his front would bury itself in his chest. (These clips were set at v02 to v04, when the
forearm wore a rolled cuff 161 mm across; from v05 the arm is bare and thinner, and the poses
were kept because they still read.) Every pose below keeps the forearm pointing
forward, outward, up or down, never across him. That rules out folded arms, which are Dante's
anyway. A fist can reach the belt at his own hip, with the elbow well out, and no further in.
ARMS UP GO THROUGH THE ELBOW (section 13 rule 8): a straight arm cannot rise above level (his
head and ears are over it), so the upper arm lifts a little and the forearm folds up and out,
and may stretch (scale on the forearm bone) as it is thrown.

Each clip keeps the LENGTH of the one it replaces (walk 0.72 s, sprint 0.48, jump 0.50,
fall 0.33); the idle is ACTED and is 8 s, not the old 1.33 (see `idle`). NOT SEEN IN UNITY.
"""
import math

RATE = 60.0
SPLAY = 30.0      # degrees a folded forearm is turned out from straight ahead in the walk


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


def _rotx(v, deg):
    """A direction turned about +x: positive swings a hanging limb back."""
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return (v[0], v[1] * c - v[2] * s, v[1] * s + v[2] * c)


def _lerp(a, b, t):
    return tuple(p + (q - p) * t for p, q in zip(a, b))


#   where his LEFT arm points in each stance: (upper arm, forearm), in the file's space (+x his
#   left, +y up, +z ahead). The right is the mirror.
#   CARRIED: out from the body and a little ahead, the forearm lower and further ahead. This is
#   the original idle's own carriage, with the elbow the original bakes into its arm mesh.
ARMS_CARRIED = ((0.84, -0.52, 0.14), (0.60, -0.60, 0.53))
#   THE HITCH. A fist on the belt at each hip, in front of the pouch: the elbow out to the side
#   and a little back, the forearm forward, down and a little in.
#   (v02 had a strongman's flex here, fists up beside his ears. With arms this short it read
#   from the front as a man with his hands up, which is the opposite of him.)
ARM_FLEX = ((0.94, -0.32, -0.10), (-0.22, -0.46, 0.86))
ARM_FLEX_TIGHT = ((0.96, -0.22, -0.10), (-0.22, -0.40, 0.89))    # the hitch itself: elbows and fists lift
FLEX_REACH = 0.0
#   THE WIND-UP, his RIGHT arm (written as a left arm, mirrored below): the upper arm out and
#   back, the forearm cocked up behind his ear with the slipper in it.
ARM_WIND = ((0.80, 0.14, -0.58), (0.30, 0.86, -0.40))
#   THE THROW: the same arm brought over and through, straight out ahead and down
ARM_THROWN = ((0.60, -0.12, 0.79), (0.36, -0.32, 0.88))
THROW_REACH = 0.30
#   his LEFT arm while the right winds up: pointed at what he is aiming for
ARM_AIM = ((0.55, -0.12, 0.83), (0.40, -0.06, 0.91))
AIM_REACH = 0.20
#   and swept back out of the way as the right arm comes through
ARM_SWEPT = ((0.84, -0.42, -0.34), (0.74, -0.60, -0.30))
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of a big man waiting for somebody to try it, in three things he does:

      THE HITCH (0.6 to 3.0 s). He takes hold of his belt at both hips, elbows out, and hitches
      his trousers up twice, coming up on his toes each time and settling back down heavier.
      He looks down the street to his right, then to his left, and gives it a small nod.
      THE NECK (3.2 to 4.4 s). Arms carried again. His head tips hard to his left and stops
      dead, then hard to his right and stops dead: cracking his neck, shoulders riding against it.
      THE PRACTICE THROW (4.6 to 7.6 s). He stamps his feet apart and sinks onto them, points
      his left arm at the can, cocks his right arm up behind his ear with his chest turned
      away, and throws: the arm whips over and through in a tenth of a second and he holds the
      follow-through, leaning over his front foot, before he straightens up.

    WHY THESE. His roster line is "once he plants his feet and winds up a throw, the whole
    street clears out", so the throw is the thing he rehearses while he waits, and it is the
    only fast motion in the clip. The belt and braces are the most particular thing he wears,
    and hitching them up is what a big man in braces does when he means to stay where he is.
    And he scowls: a man who cracks his neck before a game. He is a grown man, so nothing here
    fidgets, taps a foot or bounces; between the stances he stands wide and breathes.
    NOT USED, because his arms are as thick as his forearm is long: folded arms, or
    anything with a hand across his own front (see the top of this file).

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 3.0))    # slow: three breaths in the clip
        flex = _window(t, 0.6, 3.0, 0.45)
        pump = min(1.0, _bump(t, 1.45, 0.11) + _bump(t, 1.95, 0.11))
        look_r = _window(t, 1.15, 1.85, 0.22)
        look_l = _window(t, 2.00, 2.75, 0.22)
        approve = _bump(t, 2.45, 0.10)
        # the neck: each tilt arrives in 0.07 s and stops dead
        crack_l = _ease((t - 3.30) / 0.07) * (1.0 - _ease((t - 3.62) / 0.14))
        crack_r = _ease((t - 3.74) / 0.07) * (1.0 - _ease((t - 4.10) / 0.22))
        neck = _window(t, 3.15, 4.40, 0.20)
        # the throw
        plant = _window(t, 4.6, 7.6, 0.30)
        stamp = _bump(t, 4.78, 0.07) + _bump(t, 5.02, 0.07)
        wind = _ease((t - 5.05) / 0.65) * (1.0 - _ease((t - 5.92) / 0.10))
        thrown = _ease((t - 5.92) / 0.10) * (1.0 - _ease((t - 6.95) / 0.55))
        whip = _bump(t, 5.99, 0.07)
        row = {}
        carried = {1: _arm(1, *ARMS_CARRIED), -1: _arm(-1, *ARMS_CARRIED)}
        for side, name in ((1, "left"), (-1, "right")):
            lazy, tight = _arm(side, *ARM_FLEX), _arm(side, *ARM_FLEX_TIGHT)
            if side < 0:
                first, second = _arm(side, *ARM_WIND), _arm(side, *ARM_THROWN)
                reach = THROW_REACH * thrown * (0.6 + 0.4 * whip)
            else:
                first, second = _arm(side, *ARM_AIM), _arm(side, *ARM_SWEPT)
                reach = AIM_REACH * wind
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(carried[side][k], _mix(lazy[k], tight[k], pump), flex)
                q = _mix(_mix(q, first[k], wind), second[k], thrown)
                row[(bone, "rotation")] = q
            row[("forearm-" + name, "scale")] = (1.0 + FLEX_REACH * flex * pump + reach, 1.0, 1.0)
        # his head: to each arm in the flex; the two cracks; down the line of the throw
        look = -34.0 * look_r + 34.0 * look_l - 16.0 * wind + 6.0 * thrown
        nod = 5.0 + 4.0 * (look_r + look_l) + 7.0 * approve + 3.0 * neck - 6.0 * wind + 9.0 * thrown - 0.5 * breath
        tilt = -24.0 * crack_l + 24.0 * crack_r + 3.0 * look_r - 3.0 * look_l
        # his chest: turned away in the wind-up, through in the throw; lifted in the flex
        twist = -30.0 * wind + 26.0 * thrown
        lean = -5.0 * flex - 3.0 * pump + 3.0 * neck - 7.0 * wind + 15.0 * thrown + 1.0 * breath
        hunch = 7.0 * crack_l - 7.0 * crack_r
        sink = 0.050 * plant + 0.030 * stamp - 0.045 * pump + 0.030 * flex * (1.0 - pump) - 0.030 * whip
        spread = 9.0 * plant
        row.update({
            ("root", "translation"): (0.006 * (crack_l - crack_r) - 0.010 * wind + 0.012 * thrown, 0.0, 0.0),
            ("root", "scale"): _squash(1.0 + 0.012 * breath - sink),
            ("root", "rotation"): qz(-1.5 * wind + 2.0 * thrown),
            ("torso", "rotation"): mul(mul(qx(lean), qy(twist)), qz(hunch)),
            ("head", "rotation"): mul(mul(qy(look - 0.55 * twist), qx(nod - 0.5 * lean)), qz(tilt - hunch)),
            # feet apart for the throw; the left foot steps ahead as the arm comes through
            ("leg-left", "rotation"): mul(qx(4.0 * wind - 13.0 * thrown), qz(2.0 + spread)),
            ("leg-right", "rotation"): mul(qx(-3.0 * wind + 9.0 * thrown), qz(-2.0 - spread)),
        })
        _add(out, t, row)
    return out


def walk():
    """A lumber. Short steps on a wide base, the whole body rolling over each foot as it lands,
    a low bounce with a squash at the bottom of it, the arms carried out and swung stiff from
    the shoulder, the head held down and forward."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.8)
        lift = abs(s) ** 0.9
        roll = math.sin(phase - 0.45)           # the weight arrives well after the foot
        thud = math.cos(2.0 * phase - 0.9)      # twice a stride, just after each foot lands
        row = {
            ("root", "translation"): (0.016 * roll, 0.002 + 0.013 * lift, 0.0),
            ("root", "rotation"): mul(qx(3.0), qz(5.5 * roll)),
            ("root", "scale"): _squash(0.972 + 0.045 * lift),
            ("leg-left", "rotation"): mul(qx(-27.0 * sh), qz(4.0 - 5.5 * roll)),
            ("leg-right", "rotation"): mul(qx(27.0 * sh), qz(-4.0 - 5.5 * roll)),
            ("torso", "rotation"): mul(mul(qx(4.0 + 1.2 * thud), qy(5.0 * sh)), qz(-3.5 * roll)),
            ("head", "rotation"): mul(mul(qx(5.0 + 2.2 * thud), qy(-3.5 * sh)), qz(-1.6 * roll)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            swing = 13.0 * sh * side            # the left arm goes back as the left leg comes forward
            upper = _rotx(ARMS_CARRIED[0], swing)
            fore = _rotx(ARMS_CARRIED[1], 1.5 * swing)
            q = _arm(side, upper, fore)
            row[("arm-" + name, "rotation")] = q[0]
            row[("forearm-" + name, "rotation")] = q[1]
        _add(out, t, row)
    return out


#   the run: elbows wide, forearms ahead, driven like pistons
ARM_DRIVE = ((0.76, -0.62, 0.10), (0.24, 0.06, 0.97))


def sprint():
    """A charge. He tips forward and puts his head down like a bull, drives his forearms ahead
    one after the other with the elbows wide, and comes down hard on each foot."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.7)
        lift = abs(s) ** 0.75
        rock = math.sin(phase - 0.35)
        row = {
            ("root", "translation"): (0.008 * rock, 0.004 + 0.028 * lift, 0.0),
            ("root", "rotation"): mul(qx(18.0), qz(3.5 * rock)),
            ("root", "scale"): _squash(0.95 + 0.09 * lift),
            ("leg-left", "rotation"): mul(qx(-44.0 * sh), qz(3.0 - 3.5 * rock)),
            ("leg-right", "rotation"): mul(qx(44.0 * sh), qz(-3.0 - 3.5 * rock)),
            ("torso", "rotation"): mul(mul(qx(8.0), qy(9.0 * sh)), qz(-3.0 * rock)),
            # the head is NOT lifted back to level: he runs looking out from under his fringe
            ("head", "rotation"): mul(mul(qx(-9.0 + 2.0 * math.cos(2.0 * phase)), qy(-5.0 * sh)), qz(-1.0 * rock)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            drive = 24.0 * sh * side
            q = _arm(side, _rotx(ARM_DRIVE[0], drive), _rotx(ARM_DRIVE[1], 1.2 * drive))
            row[("arm-" + name, "rotation")] = q[0]
            row[("forearm-" + name, "rotation")] = q[1]
            row[("forearm-" + name, "scale")] = (1.0 + 0.12 * max(0.0, -sh * side), 1.0, 1.0)
        _add(out, t, row)
    return out


#   arms up: where the left arm points at the top of the jump. His ears stand out past his
#   shoulders, so the V is thrown wide of them.
ARM_UP = ((0.93, 0.30, 0.20), (0.64, 0.73, 0.24))
ARM_UP_REACH = 0.55


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet
    together and the same, never a stride. He heaves himself up: a short stretch, both arms
    thrown up in a V through the elbows, his legs kicked apart under him like a man jumping
    for a rebound, his chin up."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.13)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.14)
        ring = math.cos(2.0 * math.pi * t / 0.50) * math.exp(-t / 0.15)
        row = {
            ("root", "scale"): _squash(1.0 + 0.12 * ring),
            ("root", "rotation"): qx(-2.0 * hang),
            # both legs the same: trailing at take-off, then kicked apart and a little ahead
            ("leg-left", "rotation"): mul(qx(11.0 * rise - 10.0 * hang), qz(2.0 + 9.0 * hang)),
            ("leg-right", "rotation"): mul(qx(11.0 * rise - 10.0 * hang), qz(-2.0 - 9.0 * hang)),
            ("torso", "rotation"): qx(-7.0 * rise - 2.0 * hang),
            ("head", "rotation"): qx(-11.0 * rise + 2.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 7.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.75 + 0.25 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: he comes down like a dropped sack, feet apart and braced for the ground, arms up
    in the V and shaking a little, looking down at where he will land."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.04 + 0.010 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(4.0), qz(1.2 * s)),
            ("leg-left", "rotation"): mul(qx(-7.0 + 4.0 * s), qz(11.0)),
            ("leg-right", "rotation"): mul(qx(-7.0 - 4.0 * s), qz(-11.0)),
            ("torso", "rotation"): mul(qx(5.0), qz(-1.0 * s)),
            ("head", "rotation"): mul(qx(14.0 + 1.2 * c), qz(-0.8 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(3.0 * s))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(5.0 * c))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.70 + 0.05 * s * side), 1.0, 1.0)
        _add(out, t, row)
    return out


# ---------------------------------------------------------------------------
# THE ACTION CLIPS (2026-10-07): the throw, the pick-up, the tag, the shove, the two gestures,
# the slide, out of breath, sitting, the knock-down, yes and no. They replace the thirteen of
# the same names copied from the stock rig, which were made for an arm with no elbow.
#
# EIGHT KEEP THE LENGTH AND THE BEAT OF THE CLIP THEY REPLACE, measured off character-male-f.glb
# (the fist is the far end of the stock arm; "forward" is +z):
#   holding-right-shoot  0.2000 s  starts WITH the arm at its furthest forward, so the game plays
#                                  it from the instant the slipper leaves; its far point is 0.060 s
#   pick-up              0.3333 s  fist and head lowest at 0.167 s
#   attack-melee-right   0.4167 s  right fist furthest forward at 0.255 s
#   attack-melee-left    0.4167 s  left fist furthest forward at 0.256 s
#   interact-right/left  0.6667 s  fist furthest forward at 0.180 s, held to 0.40 s
#   slide                0.9500 s  head lowest at 0.250 s, right fist furthest forward at 0.342 s
#   die                  0.3333 s  on the ground from 0.267 s
# FIVE ARE ONLY LOOKED AT, NEVER TIMED AGAINST, and are longer than the stock ones (which were a
# sixth of a second, or two thirds for the emotes) so that there is room to move:
#   holding-right 2.0 s loop, crouch 1.6 s loop, sit 0.8 s, emote-yes 1.2 s loop, emote-no 1.3 s loop
# THIRTEEN SILHOUETTES, by where the fists are at each clip's beat (no two the same):
#   carry: right fist cocked beside his ear       throw: right arm long and level, feet split, mid-hop
#   pick-up: folded double, right fist on ground  tag: right arm straight ahead at chest height, standing
#   shove: BOTH arms straight ahead, head down    reach right: right arm planted DOWN at the ground ahead
#   reach left: left arm level, wide to his left  slide: flat on his front, right arm along the ground
#   breath: bent, a fist on each thigh            sit: on the ground, a fist planted each side
#   down: on his back, arms flung, a boot up      yes: BOTH fists over his head
#   no: turned half away, right arm thrown out wide behind him, left fist on his belt
# THE THROW AND THE TAG ARE PARTLY POSED BY THE GAME over the clip: his chest, his head and both
# UPPER arms are overwritten while they play, so in those two the character is in the root, the
# legs and the squash, and each forearm keeps a moderate bend that opens to straight at the beat.
#
# HOW THEY ARE WRITTEN. A clip is a list of (time, pose, ease): the pose is a table of plain
# numbers in HIS OWN space (the `root` bone's), and `_act` walks from each to the next. Where an
# arm points, and which way his head faces, are given in that space whatever his chest is doing,
# so "the fist on the ground" stays on the ground when the chest folds over it.
#   tx ty tz            root translation, metres
#   pitch yaw roll      root rotation, degrees; pitch + tips him FORWARD
#   sq                  root height (the width takes up what the height loses)
#   lean twist hunch    chest: + lean folds forward, + twist turns his chest to his LEFT
#   nod look tilt       head, in his own space: + nod is chin down, + look is to his LEFT
#   legL legR           (forward, out) degrees
#   armL armR           (upper arm, forearm) directions, written for a LEFT arm (+x is OUT)
#   reachL reachR       forearm stretch, as a share of its length
# ---------------------------------------------------------------------------

def _inv(q):
    return (-q[0], -q[1], -q[2], q[3])


def _arm_in(side, upper, fore, frame):
    """As `_arm`, for directions given in his own space when his chest is turned by `frame`."""
    inverse = _inv(frame)
    rest = (float(side), 0.0, 0.0)
    u = _rot(inverse, _norm((side * upper[0], upper[1], upper[2])))
    f = _rot(inverse, _norm((side * fore[0], fore[1], fore[2])))
    q = _arc(rest, u)
    return q, _arc(rest, _rot(_inv(q), f))


#   the stand every one-shot leaves from and comes back to: frame 0 of `idle`
STAND = {
    "tx": 0.0, "ty": 0.0, "tz": 0.0, "pitch": 0.0, "yaw": 0.0, "roll": 0.0, "sq": 1.0,
    "lean": 0.0, "twist": 0.0, "hunch": 0.0, "nod": 5.0, "look": 0.0, "tilt": 0.0,
    "legL": (0.0, 2.0), "legR": (0.0, 2.0),
    "armL": ARMS_CARRIED, "armR": ARMS_CARRIED, "reachL": 0.0, "reachR": 0.0,
}


def _pose(base=None, **changes):
    pose = dict(base or STAND)
    pose.update(changes)
    return pose


_EASES = {
    "io": _ease,
    "lin": lambda t: t,
    "in": lambda t: t * t,                       # gathers speed and arrives at full speed: a hit
    "out": lambda t: 1.0 - (1.0 - t) ** 2,       # leaves at full speed and settles
    "whip": lambda t: t ** 3,
}


def _blend(a, b, t):
    if isinstance(a, tuple):
        return tuple(_blend(p, q, t) for p, q in zip(a, b))
    return a + (b - a) * t


def _row(p):
    torso = mul(mul(qx(p["lean"]), qy(p["twist"])), qz(p["hunch"]))
    head = mul(_inv(torso), mul(mul(qy(p["look"]), qx(p["nod"])), qz(p["tilt"])))
    row = {
        ("root", "translation"): (p["tx"], p["ty"], p["tz"]),
        ("root", "rotation"): mul(mul(qy(p["yaw"]), qx(p["pitch"])), qz(p["roll"])),
        ("root", "scale"): _squash(p["sq"]),
        ("torso", "rotation"): torso,
        ("head", "rotation"): head,
        ("leg-left", "rotation"): mul(qx(-p["legL"][0]), qz(p["legL"][1])),
        ("leg-right", "rotation"): mul(qx(-p["legR"][0]), qz(-p["legR"][1])),
    }
    for side, name, key, reach in ((1, "left", "armL", "reachL"), (-1, "right", "armR", "reachR")):
        q = _arm_in(side, p[key][0], p[key][1], torso)
        row[("arm-" + name, "rotation")] = q[0]
        row[("forearm-" + name, "rotation")] = q[1]
        row[("forearm-" + name, "scale")] = (1.0 + p[reach], 1.0, 1.0)
    return row


def _frames(length, pose_at):
    """The clip: `pose_at(t)` is his pose at each sixtieth of a second."""
    out = {}
    for t in _times(length):
        _add(out, t, _row(pose_at(t)))
    # one rotation has two quaternions: keep each key on the side of the one before it
    for (bone, path), rows in out.items():
        if path != "rotation":
            continue
        for i in range(1, len(rows)):
            if sum(p * q for p, q in zip(rows[i - 1][1], rows[i][1])) < 0.0:
                rows[i] = (rows[i][0], tuple(-k for k in rows[i][1]))
    return out


def _act(length, keys):
    """The clip: `keys` is [(time, pose, ease into this pose)], first at 0 and last at `length`."""
    def pose_at(t):
        k = 0
        while k < len(keys) - 2 and t > keys[k + 1][0]:
            k += 1
        t0, a = keys[k][0], keys[k][1]
        t1, b = keys[k + 1][0], keys[k + 1][1]
        ease = keys[k + 1][2] if len(keys[k + 1]) > 2 else "io"
        f = _EASES[ease](max(0.0, min(1.0, (t - t0) / (t1 - t0))))
        return {name: _blend(a[name], b[name], f) for name in a}
    return _frames(length, pose_at)


#   THE CHAMBER: an elbow drawn back and a fist beside his ribs, the forearm level and ahead
ARM_CHAMBER = ((0.70, -0.40, -0.60), (0.15, -0.10, 0.98))
#   THE COCK: how he carries the tsinelas. Up beside his ear, the elbow out to the side and the
#   forearm standing up from it, the way a man holds the thing he is about to throw at you. The
#   fist is out past his head, where the slipper shows from behind. (The first carry had the fist
#   held out ahead of his chest, and read as the tag.)
ARM_HEFT = ((0.88, -0.28, -0.12), (0.18, 0.92, 0.32))
ARM_HEFT_UP = ((0.90, -0.16, -0.10), (0.26, 0.95, 0.12))    # the slipper bounced in his fist
#   his left arm while he carries: low and ahead, half pointing at what he means to hit
ARM_READY = ((0.70, -0.55, 0.45), (0.42, -0.50, 0.76))

#   HOLDING. Feet apart, sunk onto them, his chest turned from the hand that holds it.
HOLD = _pose(sq=0.985, twist=-12.0, lean=2.0, nod=7.0, legL=(0.0, 7.0), legR=(0.0, 7.0),
             armR=ARM_HEFT, armL=ARM_READY)
HOLD_LENGTH = 2.0


def holding_right():
    """A 2 s loop. The slipper is COCKED beside his ear and he weighs it there: twice he bounces
    it in his fist, the second time smaller, the way a man tries a stone before he throws it,
    his eyes going to it and back up the street. Under that one slow breath, and his weight
    rolling from one foot to the other and back. Frame 0 is the carry the throw leaves from."""
    def pose_at(t):
        turn = 2.0 * math.pi * t / HOLD_LENGTH
        breath = math.sin(turn)
        sway = math.sin(turn - 0.9) + math.sin(0.9)                 # nothing at the loop's join
        heft = _bump(t, 0.62, 0.085) + 0.6 * _bump(t, 0.98, 0.085)
        dip = _bump(t, 0.50, 0.07) + 0.6 * _bump(t, 0.87, 0.07)     # the sink before each bounce
        glance = _window(t, 0.40, 1.25, 0.25)
        arm = _blend(ARM_HEFT, ARM_HEFT_UP, heft)
        return _pose(HOLD, sq=0.985 + 0.010 * breath - 0.030 * dip + 0.022 * heft,
                     tx=0.005 * sway, roll=1.3 * sway, twist=-12.0 - 1.5 * sway, lean=2.0 - 1.0 * breath + 2.0 * dip,
                     nod=7.0 - 3.0 * glance - 4.0 * heft, look=-20.0 * glance, tilt=-1.0 * sway - 4.0 * glance,
                     legL=(0.0, 7.0 - 1.3 * sway), legR=(0.0, 7.0 + 1.3 * sway),
                     armR=arm, reachR=0.22 * heft,
                     armL=_blend(ARM_READY, ((0.72, -0.52, 0.40), (0.44, -0.56, 0.70)), 0.5 - 0.5 * math.cos(turn)))
    return _frames(HOLD_LENGTH, pose_at)


def holding_right_shoot():
    """THE THROW, from the instant the slipper leaves (the game winds the arm up itself, plays
    this at the release, and poses his chest, head and upper arms over it). So the throw is in
    his FEET: he comes off the back foot in a short hop, his whole body turning in behind the
    arm and stretching, and STAMPS the front foot down, squashing on it, 0.1 s in; then hauls
    himself back to the carry. The forearm opens from its carry bend to dead straight and
    stretched at 0.06 s, the stock clip's far point, and the same pose as written here (chest
    round, arm straight down the line, the off arm swept back) is what plays where the game
    does not pose him."""
    thrown = _pose(HOLD, sq=1.07, yaw=12.0, pitch=7.0, twist=14.0, lean=14.0, tz=0.025, ty=0.014, nod=3.0,
                   legL=(22.0, 6.0), legR=(-18.0, 7.0),
                   armR=((0.30, 0.02, 0.95), (0.15, -0.08, 0.99)), reachR=0.38, armL=ARM_SWEPT)
    stamp = _pose(thrown, sq=0.89, yaw=16.0, pitch=10.0, twist=18.0, lean=17.0, ty=-0.012, tz=0.032, nod=6.0,
                  legL=(24.0, 9.0), legR=(-20.0, 9.0),
                  armR=((0.22, -0.42, 0.88), (0.10, -0.55, 0.83)), reachR=0.10)
    rise = _pose(HOLD, sq=1.02, yaw=5.0, pitch=2.0, legL=(6.0, 7.0), legR=(-5.0, 7.0))
    return _act(0.20, [(0.0, HOLD), (0.06, thrown, "out"), (0.105, stamp, "lin"), (0.165, rise), (0.20, HOLD)])


def pick_up():
    """He does not bend his knees for a slipper. He spreads his feet, props his left fist on
    his thigh with the elbow stuck out, folds over from the hips like a crane and drops his
    right fist on it (lowest at 0.167 s). Coming up he overshoots, chest out, the slipper
    already in the carry."""
    down = _pose(sq=0.82, ty=-0.007, lean=62.0, twist=14.0, nod=26.0, legL=(0.0, 16.0), legR=(0.0, 16.0),
                 armR=((0.25, -0.62, 0.74), (0.10, -0.62, 0.78)), reachR=0.24,
                 armL=((0.95, -0.10, 0.05), (-0.70, -0.68, 0.20)))
    up = _pose(sq=1.06, lean=-7.0, nod=-2.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armR=((0.70, -0.45, 0.55), (0.25, 0.35, 0.90)),
               armL=((0.90, -0.40, 0.0), (0.45, -0.75, 0.48)))
    return _act(1.0 / 3.0, [(0.0, STAND), (1.0 / 6.0, down, "in"), (0.275, up), (1.0 / 3.0, STAND)])


def attack_melee_right():
    """THE TAG. He does not jab: he puts a hand ON you. A short sink with the elbow drawn back
    (0.05 s), then the arm goes out like a ram, level and straight, his chest turning in behind
    it; out by 0.12 s and still pushing to its far point at 0.255 s; and he comes back onto
    his heels with a squash, heavy. The game poses his chest, head, upper arms, root and legs
    over this while the tag is live, so the legs here are mild, the elbows bend no more than
    64 degrees and the right one is straight from 0.12 s; what is his own all the way through
    is the sink, the stretch into the reach and the settle."""
    wind = _pose(sq=0.90, lean=-6.0, twist=-20.0, nod=8.0, legL=(0.0, 5.0), legR=(0.0, 5.0),
                 armR=((0.62, -0.50, -0.60), (0.50, -0.45, 0.35)), armL=((0.60, -0.50, 0.60), (0.40, -0.20, 0.89)))
    back = ((0.70, -0.50, -0.50), (0.45, -0.60, 0.30))       # the off arm, drawn back to his hip
    out = _pose(sq=1.05, lean=14.0, twist=22.0, nod=4.0, legL=(7.0, 4.0), legR=(-5.0, 4.0),
                armR=((0.22, 0.0, 0.97), (0.12, 0.0, 0.99)), reachR=0.22, armL=back)
    far = _pose(out, sq=1.08, lean=20.0, twist=30.0, tz=0.015, legL=(9.0, 4.0), legR=(-7.0, 4.0),
                armR=((0.15, 0.02, 0.99), (0.08, 0.02, 1.0)), reachR=0.40)
    held = _pose(far, sq=1.0, lean=17.0, twist=26.0, reachR=0.24)
    settle = _pose(sq=0.94, lean=-3.0, nod=7.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
                   armR=((0.78, -0.50, 0.37), (0.50, -0.50, 0.70)))
    return _act(0.4167, [(0.0, STAND), (0.05, wind, "out"), (0.12, out, "in"), (0.255, far, "out"),
                         (0.30, held), (0.37, settle), (0.4167, STAND)])


def attack_melee_left():
    """THE SHOVE, both hands, the left leading. Both elbows come back to his ribs, he sinks and
    leans away from it and holds there a moment; then the wall falls on you: he drives off his
    back foot, his left shoulder goes through and the left arm rams out straight, the right
    half a length behind it (far point 0.256 s)."""
    wind = _pose(sq=0.89, lean=-10.0, twist=10.0, nod=10.0, legL=(0.0, 7.0), legR=(0.0, 7.0),
                 armL=ARM_CHAMBER, armR=ARM_CHAMBER)
    coiled = _pose(wind, sq=0.87, lean=-13.0, twist=13.0)
    shove = _pose(sq=1.08, lean=30.0, twist=-9.0, nod=12.0, tz=0.030, legL=(12.0, 5.0), legR=(-14.0, 6.0),
                  # wide apart, so both fists show past his body from behind
                  armL=((0.52, 0.04, 0.85), (0.44, 0.08, 0.89)), reachL=0.44,
                  armR=((0.56, -0.02, 0.83), (0.46, 0.04, 0.89)), reachR=0.30)
    held = _pose(shove, sq=0.98, lean=25.0, reachL=0.26, reachR=0.14, tz=0.022)
    return _act(0.4167, [(0.0, STAND), (0.09, wind, "out"), (0.16, coiled, "lin"), (0.25, shove, "in"),
                         (0.31, held, "out"), (0.4167, STAND)])


def interact_right():
    """"Here. Put it HERE." Low and slow, nothing like the tag: he sinks, leans over and plants
    his right arm down at the ground ahead of his boots, a long line from the shoulder to the
    street (0.18 s), and presses it down twice more while he looks at the spot. His left fist
    sits on his belt with the elbow out."""
    lift = _pose(sq=1.02, lean=-3.0, nod=2.0, armR=((0.80, -0.45, 0.30), (0.40, -0.05, 0.92)), armL=ARM_FLEX)
    here = _pose(sq=0.91, lean=20.0, twist=10.0, nod=22.0, legL=(0.0, 9.0), legR=(0.0, 9.0),
                 armR=((0.58, -0.60, 0.55), (0.42, -0.70, 0.58)), reachR=0.34, armL=ARM_FLEX)
    off = _pose(here, sq=0.95, lean=16.0, nod=17.0, armR=((0.62, -0.56, 0.50), (0.48, -0.66, 0.52)), reachR=0.10)
    press = _pose(here, sq=0.89, lean=22.0, nod=25.0, armR=((0.58, -0.64, 0.50), (0.42, -0.76, 0.50)), reachR=0.30)
    held = _pose(press, sq=0.93, lean=19.0, nod=20.0, reachR=0.18)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.07, lift), (0.18, here), (0.25, off), (0.32, press, "in"),
                            (0.40, held, "out"), (2.0 / 3.0, STAND)])


def interact_left():
    """"Clear off." His left fist gathers ahead of his chest, goes out at you (0.18 s), then
    sweeps the street wide to his left and stays out there, his chest and head following it."""
    gather = _pose(sq=0.96, twist=-12.0, lean=3.0, nod=8.0,
                   armL=((0.45, -0.45, 0.77), (-0.25, 0.10, 0.96)))
    out = _pose(sq=1.02, twist=-6.0, lean=10.0, nod=6.0, legL=(0.0, 4.0), legR=(0.0, 4.0),
                armL=((0.30, -0.05, 0.95), (0.02, 0.05, 1.0)), reachL=0.20)
    swept = _pose(sq=1.03, twist=17.0, lean=3.0, nod=4.0, look=22.0, legL=(0.0, 5.0), legR=(0.0, 5.0),
                  armL=((0.90, 0.0, 0.42), (0.97, 0.10, 0.18)), reachL=0.28,
                  armR=((0.84, -0.50, -0.05), (0.62, -0.66, 0.30)))
    held = _pose(swept, sq=1.0, twist=19.0, look=16.0, armL=((0.97, -0.05, 0.20), (0.99, 0.05, 0.05)), reachL=0.12)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.08, gather, "out"), (0.18, out, "in"), (0.30, swept, "out"),
                            (0.40, held), (2.0 / 3.0, STAND)])


#   flat on his front: the right arm thrown up past his ear (which is AHEAD of him, lying down),
#   the left along his side
ARM_AHEAD = ((0.80, 0.50, 0.25), (0.45, 0.88, 0.15))
ARM_ALONG = ((0.80, -0.55, -0.20), (0.60, -0.75, -0.25))


def slide():
    """He does not slide, he FALLS ON IT. A sink with both arms back; he throws himself flat on
    his front like a slab coming off a truck, boots kicked up behind him, flat at 0.25 s; the
    right arm stretches out along the ground for the slipper (0.342 s); he skids; then he
    pushes himself up off both fists and settles back onto his feet, heavy."""
    back = ((0.70, -0.45, -0.55), (0.50, -0.60, -0.62))
    sink = _pose(sq=0.86, lean=14.0, nod=0.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armL=back, armR=back)
    launch = _pose(sq=1.10, pitch=38.0, ty=0.030, tz=-0.070, nod=-25.0, legL=(-10.0, 5.0), legR=(-10.0, 5.0),
                   armR=ARM_AHEAD, reachR=0.20, armL=ARM_ALONG)
    flat = _pose(sq=0.88, pitch=82.0, ty=0.090, tz=-0.260, nod=-56.0, legL=(-34.0, 12.0), legR=(-24.0, 12.0),
                 armR=ARM_AHEAD, reachR=0.30, armL=ARM_ALONG)
    reach = _pose(flat, sq=1.06, pitch=77.0, ty=0.110, tz=-0.240, nod=-66.0, legL=(-12.0, 9.0), legR=(-16.0, 9.0), reachR=0.65)
    skid = _pose(reach, sq=1.0, pitch=78.0, ty=0.105, legL=(-24.0, 11.0), legR=(-18.0, 11.0), reachR=0.40)
    push = _pose(sq=0.90, pitch=32.0, ty=0.020, tz=-0.085, lean=18.0, nod=-10.0, legL=(-6.0, 8.0), legR=(-6.0, 8.0),
                 armL=((0.60, -0.60, 0.50), (0.30, -0.90, 0.30)), armR=((0.60, -0.60, 0.50), (0.30, -0.90, 0.30)))
    over = _pose(sq=1.05, pitch=-4.0, lean=-3.0, nod=2.0)
    return _act(0.95, [(0.0, STAND), (0.08, sink, "out"), (0.14, launch, "in"), (0.25, flat, "in"), (0.342, reach, "out"),
                       (0.55, skid), (0.71, push), (0.85, over), (0.95, STAND)])


CROUCH_LENGTH = 1.6
CROUCH = _pose(sq=0.90, ty=-0.010, lean=34.0, nod=30.0, legL=(0.0, 20.0), legR=(0.0, 20.0),
               armL=((0.92, -0.28, 0.28), (-0.45, -0.88, 0.10)), armR=((0.92, -0.28, 0.28), (-0.45, -0.88, 0.10)))


def crouch():
    """OUT OF BREATH, a 1.6 s loop. Feet wide, folded over, a fist propped on each thigh with the
    elbows stuck out, his head hanging. Two heaves: his back comes up fast as he drags the air
    in, elbows flaring, and sags back down slowly as it goes out; the second is the bigger and
    his head rolls to one side under it. His fists do not leave his thighs. The first and the
    last frame are the full bent-over pose: the game loops this while he is winded and also
    holds its last frame as an emote."""
    def pose_at(t):
        p = (2.0 * t / CROUCH_LENGTH) % 1.0
        if p > 0.9999:
            p = 0.0
        heave = _ease(p / 0.35) if p < 0.35 else 1.0 - _ease((p - 0.35) / 0.65)
        heave *= 0.75 if t < 0.5 * CROUCH_LENGTH else 1.0
        loll = _bump(t, 1.15, 0.15)
        elbows = _blend(CROUCH["armL"], ((0.97, -0.16, 0.20), (-0.40, -0.90, 0.14)), heave)
        return _pose(CROUCH, sq=0.90 + 0.055 * heave, lean=34.0 - 9.0 * heave, nod=30.0 - 13.0 * heave + 4.0 * loll,
                     tilt=9.0 * loll, hunch=2.0 * loll, armL=elbows, armR=elbows)
    return _frames(CROUCH_LENGTH, pose_at)


def sit():
    """0.8 s of getting down, ending seated. He does not lower himself: he sinks a little, reaching
    back for the ground, and DROPS; lands on it with a squash at 0.4 s, his boots flying up; rocks
    back onto his fists and forward again; and is still. He sits like something set down: legs
    straight out in a V, a fist planted on the ground each side of him, elbows locked wide, chin up."""
    planted = ((0.88, -0.30, -0.25), (0.30, -0.93, -0.10))
    seat = _pose(ty=-0.100, lean=-7.0, nod=2.0, legL=(84.0, 20.0), legR=(84.0, 20.0), armL=planted, armR=planted)
    back = ((0.75, -0.55, -0.35), (0.40, -0.85, -0.35))
    sink = _pose(sq=0.90, lean=16.0, nod=12.0, legL=(0.0, 8.0), legR=(0.0, 8.0), armL=back, armR=back)
    drop = _pose(sq=1.05, ty=-0.050, pitch=-8.0, lean=8.0, nod=0.0, legL=(46.0, 14.0), legR=(46.0, 14.0),
                 armL=back, armR=back, reachL=0.12, reachR=0.12)
    land = _pose(seat, sq=0.84, lean=3.0, nod=13.0, legL=(90.0, 24.0), legR=(90.0, 24.0))
    rock = _pose(seat, sq=1.04, pitch=-5.0, lean=-13.0, nod=-4.0, legL=(72.0, 20.0), legR=(76.0, 20.0))
    forward = _pose(seat, sq=0.98, lean=-3.0, nod=6.0, legL=(86.0, 21.0), legR=(86.0, 21.0))
    return _act(0.8, [(0.0, STAND), (0.13, sink), (0.30, drop, "in"), (0.40, land, "lin"), (0.50, rock, "out"),
                      (0.63, forward), (0.8, seat)])


def die():
    """KNOCKED DOWN, like a wall: he does not crumple, he TOPPLES. A jolt, arms thrown up; he
    goes over backward dead straight; he hits flat and squashes, his boots fly up; one bounce;
    and he ends on his back, arms flung wide, one boot still in the air."""
    wide = ((0.95, 0.25, 0.15), (0.80, 0.55, 0.20))
    jolt = _pose(sq=0.90, pitch=-8.0, lean=-14.0, nod=-15.0, legL=(6.0, 4.0), legR=(6.0, 4.0),
                 armL=((0.70, 0.20, 0.70), (0.50, 0.60, 0.60)), armR=((0.70, 0.20, 0.70), (0.50, 0.60, 0.60)))
    topple = _pose(sq=1.08, pitch=-48.0, ty=0.030, lean=-6.0, nod=-10.0, legL=(10.0, 10.0), legR=(10.0, 10.0),
                   armL=ARM_UP, armR=ARM_UP, reachL=0.40, reachR=0.40)
    slam = _pose(sq=0.84, pitch=-90.0, ty=0.105, nod=14.0, legL=(52.0, 16.0), legR=(60.0, 16.0),
                 armL=wide, armR=wide, reachL=0.30, reachR=0.30)
    bounce = _pose(slam, sq=1.05, ty=0.135, nod=-4.0, legL=(40.0, 18.0), legR=(74.0, 18.0), reachL=0.10, reachR=0.10)
    rest = _pose(sq=1.0, pitch=-90.0, ty=0.105, nod=0.0, look=24.0, legL=(4.0, 18.0), legR=(46.0, 14.0),
                 armL=((0.97, 0.20, 0.05), (0.92, 0.38, 0.05)), armR=((0.97, -0.10, 0.05), (0.80, -0.55, 0.10)))
    return _act(1.0 / 3.0, [(0.0, STAND), (0.05, jolt, "out"), (0.13, topple, "in"), (0.22, slam, "in"),
                            (0.27, bounce, "out"), (1.0 / 3.0, rest, "in")])


#   both fists pulled down to his shoulders, elbows driven at the ground
ARM_PULLED = ((0.80, -0.56, 0.12), (0.42, 0.74, 0.52))


def emote_yes():
    """YES, with both fists. A 1.2 s loop that he fills: a sink; both fists punched up over his
    head through the elbows, forearms stretched, chin up, and held there; yanked down to his
    shoulders as he squashes and nods (the first hit); up again; down again, harder (the
    second); he holds that, fists at his shoulders, and lets it go. No one else his size puts
    both arms over his head for anything but a jump."""
    high = ((0.90, 0.34, 0.16), (0.52, 0.84, 0.14))
    # the sink gathers both fists to his shoulders first, so the arms go UP from there and never out sideways
    dip = _pose(sq=0.92, lean=5.0, nod=11.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armL=ARM_PULLED, armR=ARM_PULLED)
    up = _pose(sq=1.09, lean=-6.0, nod=-12.0, armL=high, armR=high, reachL=0.60, reachR=0.60)
    up_held = _pose(up, sq=1.05, nod=-9.0, reachL=0.52, reachR=0.52)
    hit = _pose(sq=0.87, lean=9.0, nod=20.0, legL=(0.0, 8.0), legR=(0.0, 8.0), armL=ARM_PULLED, armR=ARM_PULLED)
    up2 = _pose(up, sq=1.07, reachL=0.55, reachR=0.55)
    up2_held = _pose(up2, sq=1.04, nod=-8.0, reachL=0.50, reachR=0.50)
    hit2 = _pose(hit, sq=0.84, lean=12.0, nod=24.0, legL=(0.0, 10.0), legR=(0.0, 10.0))
    proud = _pose(hit, sq=1.0, lean=-2.0, nod=4.0, legL=(0.0, 5.0), legR=(0.0, 5.0))
    return _act(1.2, [(0.0, STAND), (0.12, dip), (0.22, up, "out"), (0.34, up_held), (0.42, hit, "in"),
                      (0.54, up2, "out"), (0.66, up2_held), (0.74, hit2, "in"), (0.86, proud, "out"), (0.98, proud),
                      (1.2, STAND)])


def emote_no():
    """NO, and he is done with you. A 1.3 s loop: he turns half away to his left, his left fist
    going to his belt, and SWATS the air with his right arm, out wide and back, the way
    you put a fly off the table; holds it; winds it up and swats again, heavier; and his head,
    left behind looking at you over his shoulder, shakes against each one. Then he comes back
    round. (Folded arms are not his: his forearms cannot cross his chest. The first version cut
    both arms out wide, and passed through a T.)"""
    wound = ((0.62, -0.25, 0.74), (0.20, 0.62, 0.76))        # the fist brought up ahead of his shoulder
    swat = ((0.70, -0.32, -0.64), (0.64, -0.30, -0.71))      # thrown out and back, wide of him and a little down
    turn = _pose(sq=0.96, yaw=30.0, lean=3.0, twist=6.0, nod=8.0, look=-34.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
                 armR=wound, armL=ARM_FLEX)
    away = _pose(turn, sq=1.04, yaw=50.0, lean=-5.0, twist=14.0, nod=2.0, look=-70.0, tilt=-4.0,
                 armR=swat, reachR=0.36)
    hold = _pose(away, sq=1.0, yaw=48.0, look=-52.0, reachR=0.22)
    wind = _pose(turn, sq=0.94, yaw=42.0, lean=4.0, twist=4.0, look=-36.0, nod=9.0)
    away2 = _pose(away, sq=1.06, yaw=56.0, lean=-7.0, twist=16.0, look=-76.0, reachR=0.44)
    hold2 = _pose(away2, sq=1.0, yaw=54.0, look=-54.0, reachR=0.24)
    last = _pose(away2, sq=0.99, yaw=50.0, lean=-3.0, look=-70.0, reachR=0.14)
    return _act(1.3, [(0.0, STAND), (0.16, turn), (0.28, away, "in"), (0.42, hold, "out"), (0.56, wind),
                      (0.68, away2, "in"), (0.82, hold2, "out"), (0.96, last), (1.3, STAND)])


CLIPS = {
    "idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
    "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
    "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
    "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
    "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no,
}
