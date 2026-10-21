"""New locomotion and action clips for the Kuya Boy redesign.

Imported by tools/author_character_redesign_kuya_boy.py, which writes them into the redesign
.glb IN PLACE OF the clips of the same name copied from character-male-b.glb: the five
locomotion clips, and since 2026-10-07 the thirteen action clips (see THE ACTION CLIPS, below).
Every other clip in that file (static and the rest, 15 of them) is copied across untouched. A copy of Bayan's clips script rewritten for him:
each character's motion is its OWN (docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so
none of the numbers below are anybody else's.

WHAT HIS MOTION SHOULD SAY ABOUT HIM. GaitStyles.KuyaBoy: "the cool big brother: laid back,
leaning back a little, arms low and lazy, head cocked." The character select: "Eldest of seven.
He has been the defender since before he could count, and both the arm and the footwork know
it." GAME_OVERVIEW.md: "The heaviest hitter in the game" (LAKAS 5, BILIS 3). A Classic
character: a young man of the neighbourhood with no powers and no gear. So:
  * HE IS IN NO HURRY. He leans BACK, not forward; his arms and hips arrive late; his head is
    cocked and held steady while the rest of him rolls.
  * HE IS SOMEBODY'S KUYA. He counts heads, he greets with his chin, he thinks with a hand in
    his beard.
  * WHEN HE RUNS HE RUNS. The sprint is long and committed, the one place he leans in.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a positive angle carries his left leg OUT to his left.

HIS ARMS, AND WHAT THEY CANNOT DO. The heroes' arm: the elbow is 82 mm from the shoulder, just
inside the sleeve's mouth; the forearm and the fist ride the forearm bone, 89 mm more to the
middle of the fist. His chest is 288 mm across and his beard stands 150 mm ahead of his
shoulders, so a fist cannot reach the FRONT of his beard or the top of his head without the
forearm stretching more than reads; it can reach the side of his jaw, his own hip, and anything
out ahead of him. Poses keep the forearm clear of his chest and of the cargo pocket on his leg.
ARMS UP GO THROUGH THE ELBOW (section 13 rule 8): a straight arm cannot rise above level (his
ears and the pencil are over it), so the upper arm lifts a little and the forearm folds up and
out, and stretches (scale on the forearm bone) as it is thrown.

Each clip keeps the LENGTH of the one it replaces (walk 0.72 s, sprint 0.48, jump 0.50,
fall 0.33); the idle is ACTED and is 8 s, not the old 1.33 (see `idle`). NOT SEEN IN UNITY.
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

    Both are directions for his LEFT arm in the file's space (+x his left, +y up, +z ahead);
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
#   HANGING, HIS WAY: low and lazy, the elbows a little out past his cargo pockets, the fists
#   forward of his hips.
ARMS_HANG = ((0.66, -0.75, 0.03), (0.58, -0.78, 0.24))
#   THE BEARD. His left fist up at the side of his jaw, in his beard, the elbow out ahead of him.
#   (The front of the beard is out of reach: his head is big and the heroes' arm is 200 mm.)
ARM_BEARD = ((0.68, -0.30, 0.67), (0.28, 0.88, 0.39))
ARM_BEARD_LOW = ((0.68, -0.34, 0.65), (0.30, 0.66, 0.69))     # the foot of one stroke
BEARD_REACH = 0.30
#   THE COUNT. His right arm (written as a left arm, mirrored below) pointing out ahead of him.
ARM_POINT = ((0.58, -0.04, 0.81), (0.34, 0.14, 0.93))
POINT_REACH = 0.22
#   and his left fist resting on his hip while the right one counts: elbow out, forearm in
ARM_HIP = ((0.90, -0.42, -0.10), (0.10, -0.80, 0.59))
IDLE_LENGTH = 8.0
COUNT_BEATS = (3.95, 4.55, 5.15)        # one, two, three
COUNT_YAW = (30.0, 2.0, -28.0)          # where he points on each: to his left, ahead, to his right


def idle():
    """Eight seconds of the eldest of seven waiting for the game to start, in three things he does:

      THE BEARD (0.6 to 3.0 s). His weight goes onto his right leg and his hip with it. His left
      fist comes up to the side of his jaw and he strokes his beard downward twice, slowly, head
      cocked toward the hand, looking up and away to his right: working something out.
      THE HEAD COUNT (3.3 to 5.7 s). He straightens, plants his left fist on his hip, and counts
      the kids with his right hand: a point to his left, a point straight ahead, a point to his
      right, each one a short jab with a small dip of the head. One, two, three.
      THE NOD (5.9 to 7.6 s). Everybody is here. His arms drop, he leans back on his heels with
      his weight on his left leg, and he gives the street's own greeting twice: the chin flicked
      UP, quick, and let down slow.

    WHY THESE. "Eldest of seven" (his line in the character select): counting heads is what he
    has done all his life, and nobody else in the cast would. The beard is the most particular
    thing about him, and stroking it is the laid-back man's way of thinking. The upward nod is
    how a kuya says hello without taking his hands out of anything. He is "the cool big
    brother: laid back, leaning back a little" (GaitStyles), so nothing here is fast except the
    three jabs and the two flicks; between the stances he stands easy and breathes.
    NOT USED: folded arms (Dante's), a belt hitch, a neck crack or a practice throw (Bayan's),
    a fist in a palm or a shoulder roll (Bebang's).

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 3.0))    # slow: three breaths in the clip
        beard = _window(t, 0.6, 3.0, 0.42)
        stroke = min(1.0, _ease((t - 1.30) / 0.42) * (1.0 - _ease((t - 1.74) / 0.16))
                     + _ease((t - 2.02) / 0.42) * (1.0 - _ease((t - 2.46) / 0.16)))
        count = _window(t, 3.3, 5.7, 0.34)
        jab = sum(_bump(t, at, 0.085) for at in COUNT_BEATS)
        # where he is pointing: it slides from one to the next between the beats
        point = COUNT_YAW[0] + (COUNT_YAW[1] - COUNT_YAW[0]) * _ease((t - 4.12) / 0.30) \
            + (COUNT_YAW[2] - COUNT_YAW[1]) * _ease((t - 4.72) / 0.30)
        nod = _window(t, 5.9, 7.6, 0.36)
        # the chin flick: up in 0.07 s, down over 0.3
        flick = _ease((t - 6.42) / 0.07) * (1.0 - _ease((t - 6.52) / 0.30)) + _ease((t - 6.98) / 0.07) * (1.0 - _ease((t - 7.08) / 0.30))
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            if side > 0:
                first = _arm(side, *_pose_mix(ARM_BEARD, ARM_BEARD_LOW, stroke))
                second = _arm(side, *ARM_HIP)
                reach = BEARD_REACH * beard
            else:
                first = hang[side]
                second = _arm(side, *ARM_POINT)
                reach = POINT_REACH * count * (0.45 + 0.55 * min(1.0, jab))
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(_mix(hang[side][k], first[k], beard), second[k], count)
                if side < 0 and k == 1:
                    q = mul(q, qz(7.0 * min(1.0, jab) * count))      # the jab: the forearm snaps down a little
                row[(bone, "rotation")] = q
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        # his weight: onto his right leg in the beard, level in the count, onto his left in the nod
        hip = -1.0 * beard + 0.9 * nod
        # his head: cocked to the hand and looking up and away; to each child; the flick
        look = -16.0 * beard + count * point * 0.55
        pitch = -7.0 * beard + 5.0 * count * min(1.0, jab) - 15.0 * flick + 3.0 * nod - 0.5 * breath
        tilt = -3.0 - 9.0 * beard + 5.0 * nod
        twist = count * point * 0.45 + 4.0 * beard
        lean = -3.0 - 2.0 * beard + 3.0 * count + 2.0 * count * min(1.0, jab) - 5.0 * nod - 2.0 * flick + 1.0 * breath
        row.update({
            ("root", "translation"): (0.014 * hip, 0.0, 0.0),
            ("root", "scale"): _squash(1.0 + 0.012 * breath - 0.018 * abs(hip) + 0.022 * flick - 0.014 * count * min(1.0, jab)),
            ("root", "rotation"): qz(-3.5 * hip),
            ("torso", "rotation"): mul(mul(qx(lean), qy(twist)), qz(3.0 * hip)),
            ("head", "rotation"): mul(mul(qy(look), qx(pitch - 0.5 * lean)), qz(tilt + 1.5 * hip)),
            # the legs stay under him as the hip goes over: the far foot turns out a little
            ("leg-left", "rotation"): mul(qx(-2.0 * nod), qz(2.0 + 3.5 * hip + 4.0 * max(0.0, -hip))),
            ("leg-right", "rotation"): mul(qx(-2.0 * beard), qz(-2.0 + 3.5 * hip - 4.0 * max(0.0, hip))),
        })
        _add(out, t, row)
    return out


def _pose_mix(a, b, t):
    """Between two arm poses given as directions."""
    return (_lerp(a[0], b[0], t), _lerp(a[1], b[1], t))


def walk():
    """A stroll. He leans back a little and lets his legs go out ahead of him, his hips swaying
    late, his arms low and swinging lazy from the shoulder with the forearm trailing, his head
    cocked to one side and held steady while the rest of him rolls."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.95)
        late = math.sin(phase - 0.75)           # the arms and the hips arrive after the feet
        lift = abs(s) ** 0.9
        row = {
            ("root", "translation"): (0.012 * late, 0.002 + 0.012 * lift, 0.0),
            ("root", "rotation"): mul(qx(-2.5), qz(4.0 * late)),
            ("root", "scale"): _squash(0.978 + 0.040 * lift),
            ("leg-left", "rotation"): mul(qx(-32.0 * sh - 3.0), qz(3.0 - 4.0 * late)),
            ("leg-right", "rotation"): mul(qx(32.0 * sh - 3.0), qz(-3.0 - 4.0 * late)),
            ("torso", "rotation"): mul(mul(qx(-2.0), qy(6.0 * sh)), qz(-2.5 * late)),
            ("head", "rotation"): mul(mul(qx(3.0 + 1.0 * math.cos(2.0 * phase)), qy(-4.0 * sh)), qz(-4.0 - 1.5 * late)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            swing = 17.0 * late * side          # the left arm goes back as the left leg comes forward
            q = _arm(side, _rotx(ARMS_HANG[0], swing), _rotx(ARMS_HANG[1], 1.7 * swing - 6.0))
            row[("arm-" + name, "rotation")] = q[0]
            row[("forearm-" + name, "rotation")] = q[1]
        _add(out, t, row)
    return out


#   the run: the elbows bent square and close, the fists pumped past his hips
ARM_DRIVE = ((0.52, -0.84, -0.06), (0.26, -0.10, 0.96))


def sprint():
    """When he does run he RUNS: the long stride of the eldest, who was always the one sent to
    fetch. He tips well forward with his chin up and his beard out ahead of him, his elbows
    square and pumping, his feet reaching."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.8)
        lift = abs(s) ** 0.8
        rock = math.sin(phase - 0.30)
        row = {
            ("root", "translation"): (0.006 * rock, 0.004 + 0.030 * lift, 0.0),
            ("root", "rotation"): mul(qx(13.0), qz(2.5 * rock)),
            ("root", "scale"): _squash(0.955 + 0.085 * lift),
            ("leg-left", "rotation"): mul(qx(-52.0 * sh), qz(2.0 - 2.5 * rock)),
            ("leg-right", "rotation"): mul(qx(52.0 * sh), qz(-2.0 - 2.5 * rock)),
            ("torso", "rotation"): mul(mul(qx(6.0), qy(10.0 * sh)), qz(-2.0 * rock)),
            # the chin is lifted back toward level: he looks where he is going
            ("head", "rotation"): mul(mul(qx(-14.0 + 1.5 * math.cos(2.0 * phase)), qy(-6.0 * sh)), qz(-1.0 * rock)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            drive = 40.0 * sh * side
            q = _arm(side, _rotx(ARM_DRIVE[0], drive), _rotx(ARM_DRIVE[1], drive + 12.0 * max(0.0, sh * side)))
            row[("arm-" + name, "rotation")] = q[0]
            row[("forearm-" + name, "rotation")] = q[1]
            row[("forearm-" + name, "scale")] = (1.0 + 0.10 * max(0.0, -sh * side), 1.0, 1.0)
        _add(out, t, row)
    return out


#   arms up: where the left arm points at the top of the jump. His ears and the pencil stand
#   out past his shoulders, so the V is thrown up and out, clear of them.
ARM_UP = ((0.86, 0.30, 0.12), (0.42, 0.90, 0.08))
ARM_UP_REACH = 0.62


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet
    together and the same, never a stride. He goes up easy, as a man does who has been jumping
    for rebounds all his life: one long stretch, both arms thrown up in a V through the elbows,
    his legs hanging loose and a little apart under him, his head back and his beard up."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.13)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.14)
        ring = math.cos(2.0 * math.pi * t / 0.48) * math.exp(-t / 0.16)
        row = {
            ("root", "scale"): _squash(1.0 + 0.16 * ring),
            ("root", "rotation"): qx(-3.0 * hang),
            # both legs the same: trailing at take-off, then hanging loose, a little apart and ahead
            ("leg-left", "rotation"): mul(qx(12.0 * rise - 12.0 * hang), qz(2.0 + 6.0 * hang)),
            ("leg-right", "rotation"): mul(qx(12.0 * rise - 12.0 * hang), qz(-2.0 - 6.0 * hang)),
            ("torso", "rotation"): qx(-8.0 * rise - 3.0 * hang),
            ("head", "rotation"): qx(-15.0 * rise - 3.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 8.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.72 + 0.28 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: he comes down unbothered, leaning back as if into a chair, feet apart and ahead
    of him feeling for the ground, arms up in the V and swaying, looking down past his beard."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.045 + 0.010 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(-4.0), qz(1.0 * s)),
            ("leg-left", "rotation"): mul(qx(-14.0 + 3.0 * s), qz(8.0)),
            ("leg-right", "rotation"): mul(qx(-14.0 - 3.0 * s), qz(-8.0)),
            ("torso", "rotation"): mul(qx(-2.0), qz(-1.0 * s)),
            ("head", "rotation"): mul(qx(16.0 + 1.0 * c), qz(-0.8 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(4.0 * s))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(6.0 * c))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.68 + 0.05 * s * side), 1.0, 1.0)
        _add(out, t, row)
    return out


# ---------------------------------------------------------------------------
# THE ACTION CLIPS (2026-10-07): the carry, the throw, the pick-up, the tag, the shove, the two
# gestures, the slide, out of breath, sitting, the knock-down, yes and no. They replace the
# thirteen of the same names copied from the stock rig, which were made for an arm with no elbow.
#
# EIGHT KEEP THE LENGTH AND THE BEAT OF THE CLIP THEY REPLACE, measured off his own glb while it
# still carried the stock clips (the fist is the far end of the arm; "forward" is +z):
#   holding-right-shoot  0.2000 s  starts WITH the arm already forward: the game plays it from the
#                                  instant the slipper leaves; the far point here is 0.060 s
#   pick-up              0.3333 s  fist and head lowest at 0.167 s
#   attack-melee-right   0.4167 s  right fist furthest forward at 0.258 s
#   attack-melee-left    0.4167 s  left fist furthest forward at 0.258 s
#   interact-right/left  0.6667 s  fist furthest forward at 0.183 s, held to 0.40 s
#   slide                0.9500 s  head lowest at 0.25 to 0.32 s, right fist furthest forward at 0.342 s
#   die                  0.3333 s  on the ground from 0.267 s
# FIVE ARE ONLY LOOKED AT, NEVER TIMED AGAINST, and are longer than the stock ones (a sixth of a
# second, or two thirds for the emotes) so that there is room to move:
#   holding-right 2.2 s loop, crouch 1.8 s loop, sit 0.8 s, emote-yes 1.2 s loop, emote-no 1.4 s loop
# THIRTEEN SILHOUETTES, by where the fists are at each clip's beat (no two the same):
#   carry: right fist level at his hip, the slipper tossed there    throw: right arm long and down across
#   him, his right leg kicked up behind, tipped over the front foot  pick-up: tipped SIDEWAYS, right fist on
#   the ground by his foot, left arm and left leg out as the counterweight
#   tag: side on, one line: right arm level ahead, left arm level behind
#   shove: head down in a long stride, left arm stiff ahead and up, right fist tucked at his ribs
#   reach right: right forearm curled up ahead of him, the underhand "come here"
#   reach left: left arm high over his head, hailing        slide: feet first on his back, right arm reaching
#   breath: bent over, left fist on his thigh, right arm hanging dead
#   sit: leaning back on both fists planted behind him, ankles crossed
#   down: flat on his front, chin on the street, arms out wide, a boot in the air
#   yes: BOTH fists beating his chest, then both arms thrown open wide and low, chin up
#   no: left fist in his beard, right fist on his hip, head turned away
# THE THROW AND THE TAG ARE PARTLY POSED BY THE GAME over the clip: his chest, his head and both
# UPPER arms are overwritten while they play, so in those two the character is in the root, the
# legs and the squash, and each forearm keeps a moderate bend that opens to straight at the beat.
#
# HOW THEY ARE WRITTEN. A clip is a list of (time, pose, ease): the pose is a table of plain
# numbers in HIS OWN space (the `root` bone's), and `_act` walks from each to the next. Where an
# arm points, and which way his head faces, are given in that space whatever his chest is doing.
#   tx ty tz            root translation, metres
#   pitch yaw roll      root rotation, degrees; pitch + tips him FORWARD, yaw + turns him to his
#                       LEFT, roll + tips his head to his RIGHT
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


#   the stand every one-shot leaves from and comes back to: frame 0 of `idle`, which leans back
#   three degrees with his head cocked three, his arms hanging from that leaning chest
STAND_ARMS = tuple(_rot(qx(-3.0), v) for v in ARMS_HANG)
STAND = {
    "tx": 0.0, "ty": 0.0, "tz": 0.0, "pitch": 0.0, "yaw": 0.0, "roll": 0.0, "sq": 1.0,
    "lean": -3.0, "twist": 0.0, "hunch": 0.0, "nod": -1.5, "look": 0.0, "tilt": -3.0,
    "legL": (0.0, 2.0), "legR": (0.0, 2.0),
    "armL": STAND_ARMS, "armR": STAND_ARMS, "reachL": 0.0, "reachR": 0.0,
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


def _key_pose(keys, t):
    k = 0
    while k < len(keys) - 2 and t > keys[k + 1][0]:
        k += 1
    t0, a = keys[k][0], keys[k][1]
    t1, b = keys[k + 1][0], keys[k + 1][1]
    ease = keys[k + 1][2] if len(keys[k + 1]) > 2 else "io"
    f = _EASES[ease](max(0.0, min(1.0, (t - t0) / (t1 - t0))))
    return {name: _blend(a[name], b[name], f) for name in a}


def _act(length, keys):
    """The clip: `keys` is [(time, pose, ease into this pose)], first at 0 and last at `length`."""
    return _frames(length, lambda t: _key_pose(keys, t))


#   AT HIS HIP: how he carries the tsinelas. The elbow hangs by his side and the forearm is held
#   out level, ahead and wide of his pocket, the slipper lying in his fist like a ball he is about
#   to dribble. Low, where Bayan's is cocked at the ear.
ARM_PALM = ((0.70, -0.70, -0.12), (0.85, 0.12, 0.50))
ARM_TOSS = ((0.72, -0.68, -0.06), (0.70, 0.62, 0.36))       # the flick that sends it up
ARM_CATCH = ((0.70, -0.70, -0.14), (0.84, -0.22, 0.48))     # and the give as it lands
#   his weight on his left leg, leaning back from it, his head cocked at the street
HOLD = _pose(tx=0.012, roll=-3.0, hunch=3.0, lean=-6.0, twist=-6.0, nod=-3.0, look=6.0, tilt=-7.0,
             legL=(2.0, 5.5), legR=(-1.0, 3.0), armR=ARM_PALM)
HOLD_LENGTH = 2.2


def holding_right():
    """A 2.2 s loop. He stands on one leg's worth of weight, leaning back, the slipper lying in
    his fist at his hip, and he TOSSES it: a small dip, the forearm flicks up, his eyes follow it
    up and come down with it, the fist gives as it lands. Twice, the second one lazier. Then his
    weight rolls across to the other foot and back, on one slow breath. It is what he does with a
    basketball while he waits for the others. Frame 0 is the carry the throw leaves from."""
    def pose_at(t):
        turn = 2.0 * math.pi * t / HOLD_LENGTH
        breath = math.sin(turn)
        toss = _bump(t, 0.50, 0.075) + 0.7 * _bump(t, 1.02, 0.075)
        dip = _bump(t, 0.40, 0.06) + 0.7 * _bump(t, 0.92, 0.06)
        catch = _bump(t, 0.72, 0.07) + 0.7 * _bump(t, 1.24, 0.07)
        air = _bump(t, 0.60, 0.10) + 0.7 * _bump(t, 1.12, 0.10)      # his eyes on it while it is up
        shift = _window(t, 1.35, 2.15, 0.36)                        # onto his right foot and back
        arm = _blend(_blend(ARM_PALM, ARM_TOSS, min(1.0, toss)), ARM_CATCH, min(1.0, catch))
        return _pose(HOLD, sq=1.0 + 0.010 * breath - 0.030 * dip + 0.020 * toss - 0.018 * catch,
                     tx=0.012 - 0.024 * shift, roll=-3.0 + 6.0 * shift, hunch=3.0 - 6.0 * shift,
                     lean=-6.0 - 1.0 * breath + 2.5 * dip - 1.5 * toss, twist=-6.0 + 5.0 * shift,
                     nod=-3.0 - 9.0 * air + 5.0 * catch, look=6.0 - 14.0 * air - 10.0 * shift,
                     tilt=-7.0 + 3.0 * air + 9.0 * shift,
                     legL=(2.0 - 2.0 * shift, 5.5 - 3.5 * shift), legR=(-1.0 + 2.0 * shift, 3.0 + 3.0 * shift),
                     armR=arm, reachR=0.28 * toss,
                     armL=_blend(STAND_ARMS, ((0.70, -0.70, -0.12), (0.62, -0.76, 0.18)), 0.5 - 0.5 * math.cos(turn)))
    return _frames(HOLD_LENGTH, pose_at)


def holding_right_shoot():
    """THE THROW, from the instant the slipper leaves (the game winds the arm up itself, plays
    this at the release, and poses his chest, head and upper arms over it). The heaviest arm on
    the street throws like a pitcher: everything he has goes over his FRONT foot. At the release
    (0.06 s) he is long and stretched down the line, the forearm dead straight; then the arm
    carries on down and across him and his back leg comes up off the ground behind, high, his
    whole body tipped over the left foot (0.11 s); and it swings down and he rocks back onto his
    heels, into the carry. No hop and no stamp: one long lazy follow through."""
    back = ((0.62, -0.50, -0.60), (0.52, -0.62, -0.58))
    thrown = _pose(HOLD, sq=1.08, tx=0.016, roll=-2.0, hunch=0.0, pitch=9.0, yaw=10.0, twist=20.0, lean=14.0,
                   tz=0.020, nod=-6.0, look=-8.0, tilt=-3.0, legL=(9.0, 4.0), legR=(-26.0, 6.0),
                   armR=((0.26, -0.20, 0.94), (0.14, -0.26, 0.96)), reachR=0.40, armL=back)
    through = _pose(thrown, sq=0.94, pitch=17.0, yaw=16.0, twist=30.0, lean=22.0, tz=0.030, nod=-14.0,
                    legL=(14.0, 4.0), legR=(-52.0, 9.0),
                    armR=((0.10, -0.62, 0.78), (-0.16, -0.74, 0.65)), reachR=0.12)
    rock = _pose(HOLD, sq=1.03, pitch=-3.0, yaw=4.0, lean=-9.0, legL=(3.0, 5.0), legR=(-8.0, 5.0),
                 armR=((0.50, -0.80, 0.20), (0.56, -0.40, 0.72)))
    return _act(0.20, [(0.0, HOLD), (0.06, thrown, "out"), (0.11, through, "lin"), (0.165, rock), (0.20, HOLD)])


def pick_up():
    """He does not bend over for it and he does not look at it. He tips SIDEWAYS over his right
    foot like a man picking a ball up off the court on the run: the right arm drops straight to
    the street beside his boot (lowest at 0.167 s), and his left leg and his left arm swing out
    and up the other side as the counterweight. Then he swings back upright past the middle,
    onto his left foot, the slipper already at his hip."""
    down = _pose(sq=0.86, roll=35.0, tx=-0.030, ty=0.026, lean=8.0, hunch=12.0, twist=-6.0, nod=4.0, tilt=-30.0, look=6.0,
                 legL=(0.0, 18.0), legR=(4.0, 35.0),
                 armR=((0.62, -0.78, 0.10), (0.72, -0.68, 0.14)), reachR=0.0,
                 armL=((0.94, 0.30, -0.10), (0.84, 0.52, -0.10)), reachL=0.20)
    up = _pose(sq=1.05, roll=-7.0, tx=0.014, hunch=5.0, lean=-7.0, nod=-5.0, tilt=-9.0, legL=(0.0, 6.0), legR=(0.0, 8.0),
               armR=ARM_TOSS, armL=((0.74, -0.66, -0.10), (0.66, -0.72, 0.20)))
    return _act(1.0 / 3.0, [(0.0, STAND), (1.0 / 6.0, down, "in"), (0.275, up), (1.0 / 3.0, STAND)])


def attack_melee_right():
    """THE TAG. He has the longest reach on the street and he uses all of it, and nothing else:
    he rocks back a little (0.06 s), then turns side on and his right arm goes out level, one
    line from his left fist behind him to his right fist ahead of him, like a man stealing the
    ball; furthest at 0.258 s; and he rocks back onto his heels, unhurried. The game poses his
    chest, head, upper arms and root over this while the tag is live, so the legs are mild and
    both forearms only open from a moderate bend to straight."""
    wind = _pose(sq=0.93, yaw=-8.0, lean=-8.0, twist=-14.0, nod=-4.0, legL=(0.0, 4.0), legR=(0.0, 4.0),
                 armR=((0.62, -0.58, -0.52), (0.34, -0.30, 0.89)), armL=((0.60, -0.62, 0.50), (0.44, -0.40, 0.80)))
    behind = ((0.46, -0.16, -0.87), (0.38, -0.12, -0.92))
    out = _pose(sq=1.04, yaw=20.0, lean=6.0, twist=24.0, nod=-2.0, look=-22.0, legL=(6.0, 4.0), legR=(-4.0, 4.0),
                armR=((0.22, 0.02, 0.97), (0.12, 0.02, 0.99)), reachR=0.20, armL=behind, reachL=0.10)
    far = _pose(out, sq=1.07, yaw=26.0, lean=11.0, twist=30.0, tz=0.016, look=-28.0, legL=(9.0, 4.0), legR=(-7.0, 4.0),
                armR=((0.14, 0.04, 0.99), (0.07, 0.04, 1.0)), reachR=0.44, reachL=0.24)
    held = _pose(far, sq=1.0, lean=9.0, reachR=0.26, reachL=0.12)
    settle = _pose(sq=0.95, lean=-8.0, nod=-5.0, legL=(0.0, 5.0), legR=(0.0, 5.0),
                   armR=((0.60, -0.72, 0.34), (0.50, -0.60, 0.62)))
    return _act(0.4167, [(0.0, STAND), (0.06, wind, "out"), (0.13, out, "in"), (0.258, far, "out"),
                         (0.31, held), (0.37, settle), (0.4167, STAND)])


#   THE STIFF ARM: his left fist drawn back to his ribs, then driven out level ahead of him,
#   the heel of the hand first. (A forearm barred across his chest was tried first, the way a big
#   man boxes somebody out; it lies in his beard and read as a man scratching his chin.)
ARM_COCKED = ((0.66, -0.50, -0.56), (0.22, -0.14, 0.97))
ARM_STIFF = ((0.54, -0.16, 0.83), (0.46, -0.10, 0.88))   # wide of his beard and under it, so the arm shows
ARM_TUCK = ((0.60, -0.62, -0.50), (0.30, -0.52, 0.80))
ARM_GUARD = ((0.62, -0.50, 0.60), (0.32, -0.08, 0.94))


def attack_melee_left():
    """THE SHOVE. A stiff arm, the heaviest on the street. He sinks and leans away with the
    left fist drawn back to his ribs and holds there a moment; then he puts his head DOWN,
    turns his left shoulder in and drives off his back foot in a long stride, the left arm
    going out dead straight under his beard, into your chest (far point 0.258 s), his right
    fist tucked at his own ribs. He holds you off at arm's length, the way he holds a defender
    off under the hoop, and stands up out of it."""
    wind = _pose(sq=0.90, yaw=10.0, lean=-9.0, twist=8.0, nod=4.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
                 armL=ARM_COCKED, armR=ARM_GUARD)
    coiled = _pose(wind, sq=0.88, lean=-12.0, yaw=13.0)
    shove = _pose(sq=1.07, yaw=-30.0, lean=28.0, twist=-14.0, nod=24.0, look=24.0, tilt=4.0, tz=0.034,
                  legL=(17.0, 5.0), legR=(-17.0, 7.0), armL=ARM_STIFF, reachL=0.44, armR=ARM_TUCK)
    held = _pose(shove, sq=0.97, lean=24.0, reachL=0.24, tz=0.026)
    return _act(0.4167, [(0.0, STAND), (0.09, wind, "out"), (0.16, coiled, "lin"), (0.258, shove, "in"),
                         (0.32, held, "out"), (0.4167, STAND)])


def interact_right():
    """"Halika." Come here. Underhand, the way a kuya calls a small brother over: his right arm
    goes out low ahead of him (0.183 s), and the forearm curls up toward his own shoulder, twice,
    his chin lifting with each one. He leans in for the reach and back for the curl."""
    #   wide of his shoulder and no higher than it, so the curled fist is out in the open and not
    #   at his face (his no has a fist in his beard)
    low = ((0.58, -0.50, 0.64), (0.50, -0.34, 0.80))
    curl = ((0.72, -0.58, 0.38), (0.66, 0.46, 0.60))
    reach = _pose(sq=0.97, lean=8.0, twist=8.0, nod=4.0, legL=(0.0, 4.0), legR=(0.0, 4.0), armR=low, reachR=0.22)
    come = _pose(sq=1.03, lean=-7.0, twist=4.0, nod=-12.0, tilt=-8.0, armR=curl, reachR=0.10)
    again = _pose(reach, lean=5.0, reachR=0.14)
    come2 = _pose(come, sq=1.02, lean=-9.0, nod=-14.0)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.183, reach, "out"), (0.27, come, "in"), (0.34, again), (0.42, come2, "in"),
                            (0.50, come2), (2.0 / 3.0, STAND)])


def interact_left():
    """"Oy!" He hails somebody down the street: up on his toes, his left arm thrown up over his
    head through the elbow (0.183 s), and the forearm waved out and in, twice, wide and slow. His
    right fist sits on his hip."""
    hip = ARM_HIP
    high = ((0.84, 0.36, 0.14), (0.36, 0.92, 0.10))
    wide = ((0.88, 0.32, 0.14), (0.80, 0.58, 0.10))
    dip = _pose(sq=0.94, lean=2.0, armL=((0.80, -0.50, 0.30), (0.50, 0.30, 0.80)), armR=hip)
    up = _pose(sq=1.07, roll=3.0, hunch=-4.0, lean=-6.0, nod=-12.0, look=14.0, tilt=4.0, legL=(0.0, 3.0), legR=(0.0, 5.0),
               armL=high, reachL=0.66, armR=hip)
    out = _pose(up, sq=1.04, roll=-1.0, hunch=3.0, armL=wide, reachL=0.56)
    back = _pose(up, sq=1.05, reachL=0.62)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.07, dip), (0.183, up, "out"), (0.27, out), (0.35, back), (0.43, out),
                            (0.50, back), (2.0 / 3.0, STAND)])


def slide():
    """FEET FIRST, the way a kuya slides into home plate, because he leans back even for this.
    A sink; he kicks both boots out ahead of him and drops onto his back and his left fist, down
    at 0.25 s; the right arm reaches out past his boots for the slipper (0.342 s) as he crunches
    up after it; he skids on, lying back; then he rocks forward over his feet and stands up out
    of it, overshooting a little."""
    planted = ((0.62, 0.02, -0.78), (0.44, 0.0, -0.90))        # behind him, which is DOWN once he is on his back
    ahead = ((0.40, -0.70, 0.60), (0.24, -0.72, 0.65))         # down his body, which is AHEAD once he is on his back
    sink = _pose(sq=0.87, lean=10.0, nod=6.0, legL=(0.0, 5.0), legR=(0.0, 5.0),
                 armL=((0.62, -0.60, 0.50), (0.40, -0.30, 0.86)), armR=((0.62, -0.60, 0.50), (0.40, -0.30, 0.86)))
    kick = _pose(sq=1.08, pitch=-26.0, ty=0.030, tz=0.030, lean=6.0, nod=14.0, legL=(34.0, 6.0), legR=(20.0, 5.0),
                 armL=planted, armR=ahead)
    flat = _pose(sq=0.90, pitch=-66.0, ty=0.020, tz=0.130, lean=10.0, nod=40.0, legL=(46.0, 9.0), legR=(30.0, 6.0),
                 armL=planted, reachL=0.20, armR=ahead, reachR=0.20)
    reach = _pose(flat, sq=1.05, pitch=-58.0, lean=24.0, nod=44.0, legL=(34.0, 8.0), legR=(40.0, 6.0), reachR=0.64, reachL=0.30)
    skid = _pose(reach, sq=1.0, pitch=-62.0, lean=16.0, legL=(40.0, 9.0), legR=(32.0, 6.0), reachR=0.36)
    rock = _pose(sq=0.88, pitch=-14.0, tz=0.040, lean=24.0, nod=6.0, legL=(8.0, 7.0), legR=(8.0, 7.0),
                 armL=((0.66, -0.66, 0.36), (0.44, -0.80, 0.40)), armR=((0.66, -0.66, 0.36), (0.44, -0.80, 0.40)))
    over = _pose(sq=1.05, pitch=4.0, lean=-6.0, nod=-4.0)
    return _act(0.95, [(0.0, STAND), (0.08, sink, "out"), (0.14, kick, "in"), (0.25, flat, "in"), (0.342, reach, "out"),
                       (0.55, skid), (0.72, rock), (0.85, over), (0.95, STAND)])


CROUCH_LENGTH = 1.8
ARM_THIGH = ((0.86, -0.34, 0.38), (-0.38, -0.90, 0.22))        # his left fist propped on his thigh, elbow out
ARM_DEAD = ((0.40, -0.86, 0.32), (0.16, -0.97, 0.18))          # his right arm hanging straight off the shoulder
CROUCH = _pose(sq=0.90, ty=-0.008, tx=0.008, roll=-3.0, lean=31.0, twist=11.0, hunch=-5.0, nod=34.0, tilt=-10.0,
               legL=(0.0, 18.0), legR=(0.0, 13.0), armL=ARM_THIGH, armR=ARM_DEAD, reachR=0.12)


def crouch():
    """OUT OF BREATH, a 1.8 s loop. He props ONE fist on his thigh, the left, and hangs off it
    lopsided; the other arm he cannot be bothered to hold up, and it dangles straight off his
    shoulder and swings as he heaves. Two breaths: his back comes up as the air goes in and
    sags as it goes out; on the second his head rolls back and he blows it out at the sky
    before it drops again. The first and the last frame are the full bent-over pose: the game
    loops this while he is winded and also holds its last frame as an emote."""
    def pose_at(t):
        half = 0.5 * CROUCH_LENGTH
        p = (t / half) % 1.0
        if p > 0.9999:
            p = 0.0
        heave = _ease(p / 0.38) if p < 0.38 else 1.0 - _ease((p - 0.38) / 0.62)
        second = 1.0 if t >= half else 0.0
        sky = second * heave
        swing = math.sin(2.0 * math.pi * t / half - 0.9) + math.sin(0.9)       # nothing at the loop's join
        dead = _blend(ARM_DEAD, ((0.40, -0.84, 0.36), (0.20, -0.90, 0.38)), 0.5 * swing)
        return _pose(CROUCH, sq=0.90 + 0.050 * heave * (0.7 + 0.3 * second),
                     lean=31.0 - 6.0 * heave - 3.0 * sky, nod=34.0 - 10.0 * heave - 22.0 * sky,
                     tilt=-10.0 + 6.0 * sky, look=-10.0 * sky, hunch=-5.0 + 2.0 * heave,
                     armL=_blend(ARM_THIGH, ((0.92, -0.22, 0.32), (-0.34, -0.92, 0.20)), heave), armR=dead)
    return _frames(CROUCH_LENGTH, pose_at)


def sit():
    """0.8 s of getting down, ending seated. He looks back for the kerb, sinks and lets himself
    go backward onto it: lands with a squash at 0.44 s, his boots coming up; rocks back onto his
    fists; and settles the way he sits on the court between games, LEANING BACK on both arms
    planted behind him, legs out and crossed at the ankle, chin up and head cocked."""
    behind = ((0.52, -0.42, -0.74), (0.34, -0.56, -0.76))
    seat = _pose(ty=-0.100, tz=0.030, pitch=-29.0, lean=3.0, nod=22.0, tilt=-9.0, look=8.0,
                 legL=(60.0, 4.0), legR=(68.0, -7.0), armL=behind, armR=behind, reachL=0.30, reachR=0.30)
    back = ((0.66, -0.56, -0.50), (0.50, -0.70, -0.52))
    sink = _pose(sq=0.91, lean=12.0, nod=8.0, look=-30.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armL=back, armR=back)
    drop = _pose(sq=1.05, ty=-0.050, pitch=-12.0, lean=8.0, nod=6.0, legL=(40.0, 10.0), legR=(40.0, 8.0),
                 armL=back, armR=back, reachL=0.14, reachR=0.14)
    land = _pose(seat, sq=0.85, pitch=-14.0, lean=10.0, nod=22.0, legL=(82.0, 12.0), legR=(86.0, 8.0), reachL=0.10, reachR=0.10)
    rock = _pose(seat, sq=1.04, pitch=-38.0, lean=-2.0, nod=22.0, legL=(84.0, 8.0), legR=(92.0, 0.0), reachL=0.36, reachR=0.36)
    forward = _pose(seat, sq=0.98, pitch=-25.0, lean=5.0, nod=22.0, legL=(60.0, 4.0), legR=(66.0, -6.0))
    return _act(0.8, [(0.0, STAND), (0.14, sink), (0.34, drop, "in"), (0.44, land, "lin"), (0.55, rock, "out"),
                      (0.68, forward), (0.8, seat)])


def die():
    """KNOCKED DOWN, and it is the big man's own weight that does it. The hit stands him up
    straight, arms flung up; then he goes over FORWARD like a felled tree, arms out to break it
    and too late; he lands flat on his front with a squash (0.22 s), his boots flying up behind;
    bounces once; and ends with his chin on the street, staring up it, his arms out wide and
    one boot still in the air."""
    flung = ((0.80, 0.30, 0.52), (0.52, 0.76, 0.40))
    #   lying on his front: "up" his body is AHEAD along the street, "ahead" of his chest is DOWN
    spread = ((0.92, 0.36, 0.10), (0.80, 0.58, 0.16))
    jolt = _pose(sq=1.09, pitch=-9.0, lean=-12.0, nod=-20.0, legL=(4.0, 4.0), legR=(4.0, 4.0),
                 armL=flung, armR=flung, reachL=0.30, reachR=0.30)
    topple = _pose(sq=1.05, pitch=44.0, ty=0.020, tz=-0.080, lean=-4.0, nod=-26.0, legL=(-8.0, 6.0), legR=(-8.0, 6.0),
                   armL=((0.70, 0.10, 0.70), (0.50, 0.30, 0.80)), armR=((0.70, 0.10, 0.70), (0.50, 0.30, 0.80)),
                   reachL=0.40, reachR=0.40)
    slam = _pose(sq=0.84, pitch=86.0, ty=0.090, tz=-0.260, nod=-52.0, legL=(-50.0, 14.0), legR=(-62.0, 12.0),
                 armL=spread, armR=spread, reachL=0.30, reachR=0.30)
    bounce = _pose(slam, sq=1.05, ty=0.120, pitch=82.0, nod=-70.0, legL=(-30.0, 16.0), legR=(-72.0, 12.0), reachL=0.14, reachR=0.14)
    rest = _pose(sq=1.0, pitch=86.0, ty=0.095, tz=-0.260, nod=-64.0, tilt=-10.0, legL=(-6.0, 16.0), legR=(-56.0, 10.0),
                 armL=((0.97, 0.16, 0.12), (0.90, 0.36, 0.20)), armR=((0.96, -0.20, 0.12), (0.70, -0.66, 0.24)))
    return _act(1.0 / 3.0, [(0.0, STAND), (0.05, jolt, "out"), (0.13, topple, "in"), (0.22, slam, "in"),
                            (0.27, bounce, "out"), (1.0 / 3.0, rest, "in")])


#   THE THUMP: a fist on his chest over the number, under his beard, the elbow out wide and level
ARM_THUMP = ((0.97, -0.16, 0.10), (-0.62, -0.14, 0.77))
#   and the same arm swung off it, the elbow higher and the fist out ahead of his shoulder
ARM_THUMP_OFF = ((0.96, 0.04, 0.16), (0.10, 0.10, 0.99))
#   THROWN OPEN: both arms out wide and a little low, palms to the street
ARM_OPEN = ((0.84, -0.50, 0.20), (0.84, -0.40, 0.36))     # well below level: an A, never a T


def emote_yes():
    """YES, a 1.2 s loop that he fills: "Ako bahala." Leave it to me. He leans back, sticks his
    chest and his beard out, and BEATS HIS CHEST over the number with both fists, one after the
    other, four times, elbows out wide and level, the whole of him rocking onto the side of the
    fist that lands and his chin jumping with each one. Then he throws both arms OPEN, wide and
    low, with the street's own nod, the chin flicked up, holds it, bounces it once, and lets go.
    (His first yes ended with the right arm thrown up high, which was his own "Oy!" in a mirror;
    the second kept both fists on his chest to the end, and from behind he had no arms.)"""
    def beat(side, big=1.0):
        on, off = (ARM_THUMP, ARM_THUMP_OFF)
        left, right = (on, off) if side > 0 else (off, on)
        return _pose(sq=1.0 + 0.035 * big, tx=0.010 * side, roll=-3.5 * side, hunch=3.0 * side, twist=-9.0 * side,
                     lean=-10.0, nod=-9.0 * big, tilt=-3.0 - 5.0 * side, legL=(0.0, 5.0), legR=(0.0, 5.0),
                     armL=left, armR=right, reachL=0.0 if side > 0 else 0.12, reachR=0.12 if side > 0 else 0.0)
    ready = _pose(sq=0.93, lean=-3.0, nod=4.0, legL=(0.0, 5.0), legR=(0.0, 5.0), armL=ARM_THUMP_OFF, armR=ARM_THUMP_OFF)
    wide = _pose(sq=1.07, lean=-13.0, nod=-22.0, tilt=-8.0, legL=(0.0, 7.0), legR=(0.0, 7.0),
                 armL=ARM_OPEN, armR=ARM_OPEN, reachL=0.42, reachR=0.42)
    proud = _pose(wide, sq=1.02, nod=-8.0, reachL=0.32, reachR=0.32)
    again = _pose(wide, sq=1.05, nod=-19.0, reachL=0.40, reachR=0.40)
    return _act(1.2, [(0.0, STAND), (0.12, ready), (0.20, beat(-1), "in"), (0.30, beat(1), "in"), (0.40, beat(-1, 1.2), "in"),
                      (0.50, beat(1, 1.2), "in"), (0.62, wide, "out"), (0.74, proud), (0.81, again, "out"), (0.96, proud),
                      (1.2, STAND)])


def emote_no():
    """NO, a 1.4 s loop, and he gives it some thought first, which is worse. His left fist goes
    up into his beard and his right onto his hip; he leans back from you with his weight on his
    right leg and turns his face away; and he shakes his head, slow, twice each way, the fist
    stroking down his beard on each one, his shoulders turning against it. Then he lets go.
    (Folded arms are not his: they are Dante's, and his forearms cannot cross his chest.)"""
    beard = ((0.68, -0.30, 0.67), (0.28, 0.88, 0.39))
    beard_low = ((0.68, -0.34, 0.65), (0.30, 0.66, 0.69))
    base = _pose(sq=0.985, tx=-0.014, roll=3.5, hunch=-3.0, lean=-9.0, nod=-5.0, tilt=-11.0,
                 legL=(0.0, 6.0), legR=(-2.0, 2.0), armL=beard, reachL=BEARD_REACH, armR=ARM_HIP)
    left = _pose(base, look=30.0, twist=-11.0, armL=beard_low)
    right = _pose(base, look=-38.0, twist=11.0, sq=1.0)
    left2 = _pose(left, look=26.0, lean=-11.0)
    right2 = _pose(right, look=-42.0, lean=-11.0, nod=-8.0)
    return _act(1.4, [(0.0, STAND), (0.22, right), (0.42, left), (0.60, right), (0.78, left2), (0.96, right2),
                      (1.10, right2), (1.4, STAND)])


CLIPS = {
    "idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
    "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
    "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
    "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
    "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no,
}
