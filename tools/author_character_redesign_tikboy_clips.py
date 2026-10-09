"""New locomotion clips for the Tikboy redesign PROTOTYPE.

Imported by tools/author_character_redesign_tikboy.py, which writes them into the prototype
.glb IN PLACE OF the clips of the same name copied from character-male-c.glb. Nothing in the
game reads them; character-male-c.glb keeps its own. Every other clip in the .glb (slide, sit,
the emotes, the holding and attack clips, 28 in all) is copied across untouched. A copy of the
heroes' clips script rewritten for him: each character's motion is its OWN
(docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so none of the numbers below are another's.

WHAT HIS MOTION SHOULD SAY ABOUT HIM. TIKBOY is "the mischief-maker: hunched, quick small
steps, eyes up; he scampers when he runs" (`GaitStyles.Tikboy`); "always down to one slipper.
Half the footwear, twice the throwing arm" (the character select); "fast and strong, and goes
down easily" (GAME_OVERVIEW.md). A boy of about eleven who should be in school. So:
  * HE IS HUNCHED AND LOW, shoulders up round his ears, chin out, eyes up from under the cap.
  * HE IS NEVER STILL FOR LONG, and what he does is quick: his head snaps, it does not turn.
  * HE IS CHECKING WHO IS WATCHING. Then he does the thing anyway.
  * HIS RIGHT ARM IS THE THROWING ARM, and he cannot leave it alone.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK
and nods the head DOWN; about +y a positive angle turns him to his LEFT; about +z a negative
angle drops his left arm from straight out to hanging.

ARMS UP GO THROUGH THE ELBOW (section 13 rule 8). His elbow is at the mouth of his sleeve,
82 mm from the shoulder; the bare forearm and the fist ride the forearm bone, 116 mm more to
the fist's end. A straight arm cannot rise above level (his ears and his cap are over it), so
the upper arm lifts a little and the forearm folds up, and may stretch (scale on the forearm
bone) as it is thrown.

Each clip keeps the LENGTH of the one it replaces (walk 0.72 s, sprint 0.48, jump 0.50,
fall 0.33); the idle is ACTED and is 8 s, not the old 1.33 (see `idle`). NOT SEEN IN UNITY: the
game layers procedural motion on these bones, and the `root` scale and forearm scale channels
are new.
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



#   where his LEFT arm points in each stance: (upper arm, forearm), in the file's space (+x his
#   left, +y up, +z ahead). The right is the mirror.
#   HANGING, HIS WAY: elbows out, fists carried forward of his hips, ready to bolt.
ARMS_HANG = ((0.62, -0.76, 0.10), (0.36, -0.78, 0.50))
#   THE LOOKOUT. Both fists pulled up in front of his belly, forearms forward, the way a boy
#   holds his hands when he is trying to walk without making a sound.
ARM_SNEAK = ((0.50, -0.80, 0.32), (0.04, 0.30, 0.95))
#   THE CAP. His left hand up to the peak (it points off to his front-left, above his brow);
#   the heroes' arm is short, so the forearm stretches. His right fist goes to his hip.
#   (v02 stopped at his cheek; v03 reached, but straight up across his face, a bar over his
#   eye. The elbow now goes out and forward and the forearm rises OUTSIDE his cheek to the
#   peak's corner.)
ARM_CAP = ((0.70, 0.15, 0.70), (0.00, 0.88, 0.47))
CAP_REACH = 1.25
ARM_HIP = ((0.82, -0.50, -0.22), (-0.50, -0.66, 0.52))
#   THE THROW THAT IS NOT ONE. His right arm (written as a left arm, mirrored below) cocked
#   back behind his ear, then whipped forward and STOPPED. His left arm points at the target.
#   (v02's wind-up kept the fist beside his chest and did not read from the front)
ARM_COCKED = ((0.62, 0.56, -0.55), (0.30, 0.92, -0.26))
COCK_REACH = 0.55
ARM_WHIPPED = ((0.42, 0.20, 0.88), (0.06, 0.34, 0.94))
ARM_POINT = ((0.50, -0.16, 0.84), (0.22, -0.04, 0.97))
THROW_REACH = 0.45
#   THE SNICKER. His left fist over his mouth.
ARM_MOUTH = ((0.36, -0.42, 0.82), (-0.62, 0.74, 0.22))
MOUTH_REACH = 0.62
IDLE_LENGTH = 8.0


def _pose(side, name):
    return _arm(side, *name)


def idle():
    """Eight seconds of a boy who is up to something, in four things he does:

      THE LOOKOUT (0.4 to 2.6 s). He drops into a crouch, fists pulled up in front of his
      belly, and his head SNAPS to his left, holds, snaps to his right, holds: is anybody
      watching. Nobody is.
      THE CAP (2.8 to 4.4 s). He straightens, right fist on his hip, and his left hand goes up
      to the peak of his cap and yanks it down, chin dropping under it, then lets it go.
      THE THROW THAT IS NOT ONE (4.6 to 6.3 s). He winds up with his right arm, the throwing
      arm, his whole body twisting back onto his right foot, his left arm pointing at you, and
      whips it forward and STOPS half way. A feint. He has no slipper to throw.
      THE SNICKER (6.3 to 7.7 s). His left fist goes over his mouth and his shoulders shake,
      five quick bounces, his head tucked. Then he drops his arms and is innocent again.

    WHY THESE. He is the street's mischief-maker ("hunched, quick small steps, eyes up") and
    its best arm ("half the footwear, twice the throwing arm"), so he waits for his turn the
    way that boy does: he checks who can see him, fixes the cap he wears wrong on purpose,
    scares you with a throw he does not make, and laughs at you for flinching. None of the four
    is another character's (Bebang: fist into palm, headband, shoulder roll; Dante: fists on
    hips and folded arms; Sean: a flex, a bull lean, a guard; Amihan and Cheska: hops and hands
    behind the back). All through it he is low and forward on his feet, never square.

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    hang = {1: _pose(1, ARMS_HANG), -1: _pose(-1, ARMS_HANG)}
    sneak = {1: _pose(1, ARM_SNEAK), -1: _pose(-1, ARM_SNEAK)}
    cap_hand = {1: _pose(1, ARM_CAP), -1: _pose(-1, ARM_HIP)}
    cocked, whipped = _pose(-1, ARM_COCKED), _pose(-1, ARM_WHIPPED)
    point = _pose(1, ARM_POINT)
    mouth = _pose(1, ARM_MOUTH)
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 6.0))
        look = _window(t, 0.4, 2.6, 0.30)
        cap = _window(t, 2.8, 4.4, 0.32)
        throw = _window(t, 4.6, 6.3, 0.30)
        snick = _window(t, 6.3, 7.7, 0.30)
        # the head snaps: left, right
        glance = _window(t, 0.75, 1.50, 0.12) - _window(t, 1.60, 2.40, 0.12)
        # the yank on the peak
        yank = _bump(t, 3.60, 0.16)
        # the wind-up eases back, the whip is fast and stops dead with a wobble
        whip = _ease((t - 5.42) / 0.10)
        wobble = math.sin(2.0 * math.pi * (t - 5.52) / 0.22) * math.exp(-max(t - 5.52, 0.0) / 0.16) if t > 5.52 else 0.0
        wind = throw * (1.0 - whip)
        # the snicker: five quick bounces of the shoulders
        shake = snick * max(0.0, math.sin(2.0 * math.pi * (t - 6.55) / 0.20)) if 6.55 < t < 7.55 else 0.0
        settle = _bump(t, 7.78, 0.09)
        row = {}
        fist = tuple(_mix(cocked[k], whipped[k], whip) for k in range(2))
        for side, name in ((1, "left"), (-1, "right")):
            fourth = point if side > 0 else fist
            fifth = mouth if side > 0 else hang[side]
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(_mix(_mix(_mix(hang[side][k], sneak[side][k], look), cap_hand[side][k], cap), fourth[k], throw), fifth[k], snick)
                if side > 0 and k == 0:
                    q = mul(q, qz(9.0 * yank * cap))           # the elbow drops as he yanks
                if side < 0 and k == 1:
                    q = mul(q, qz(-10.0 * wobble * throw))     # the stopped arm rings
                row[(bone, "rotation")] = q
            if side > 0:
                reach = 0.20 * look + CAP_REACH * cap * (1.0 - 0.12 * yank) + 0.35 * throw + MOUTH_REACH * snick
            else:
                reach = 0.20 * look + throw * (COCK_REACH * (1.0 - whip) + THROW_REACH * whip)
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        turn = 46.0 * glance * look
        nod = 6.0 * look + 16.0 * yank * cap - 5.0 * cap - 9.0 * wind + 6.0 * throw * whip + 12.0 * snick + 4.0 * shake
        tilt = -6.0 * cap + 7.0 * snick
        twist = -30.0 * wind + 16.0 * throw * whip + 6.0 * wobble * throw     # onto the back foot, then through
        crouch = 0.055 * look + 0.020 * yank * cap + 0.030 * wind + 0.030 * snick + 0.022 * shake + 0.040 * settle
        row.update({
            ("root", "translation"): (-0.018 * wind + 0.010 * throw * whip, -0.10 * crouch, 0.0),
            ("root", "scale"): _squash(1.0 + 0.008 * breath - crouch),
            ("root", "rotation"): mul(qx(4.0 + 7.0 * look + 3.0 * snick - 5.0 * wind + 6.0 * throw * whip), qz(2.0 * wind)),
            ("torso", "rotation"): mul(mul(qx(5.0 + 4.0 * look - 4.0 * cap + 8.0 * snick + 0.8 * breath), qy(twist + 0.30 * turn)),
                                       qz(3.0 * cap - 3.0 * wind)),
            ("head", "rotation"): mul(mul(qy(0.70 * turn - 0.55 * twist + 10.0 * snick), qx(-8.0 + nod - 0.6 * breath)), qz(tilt)),
            # low and forward on his feet, set apart; the back foot takes the wind-up
            ("leg-left", "rotation"): mul(qx(-4.0 - 4.0 * look + 8.0 * wind), qz(5.0 + 2.0 * look)),
            ("leg-right", "rotation"): mul(qx(-4.0 - 4.0 * look - 6.0 * wind), qz(-5.0 - 2.0 * look - 3.0 * wind)),
        })
        _add(out, t, row)
    return out


def walk():
    """A sneak with a bounce in it: hunched, chin out, eyes up, quick short steps on the balls
    of his feet, elbows bent and both fists carried in front of him like a boy creeping up on
    something."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.65)
        c = math.cos(phase)
        lift = abs(s) ** 1.6                      # up on the toes, quick
        sway = math.sin(phase - 0.3)
        pump_left = max(0.0, -sh)
        pump_right = max(0.0, sh)
        _add(out, t, {
            ("root", "translation"): (0.008 * sway, 0.002 + 0.026 * lift, 0.0),
            ("root", "rotation"): mul(qx(8.0), qz(2.2 * sway)),
            ("root", "scale"): _squash(0.945 + 0.060 * lift),
            ("leg-left", "rotation"): mul(qx(-34.0 * sh), qz(3.0 - 2.2 * sway)),
            ("leg-right", "rotation"): mul(qx(34.0 * sh), qz(-3.0 - 2.2 * sway)),
            ("torso", "rotation"): mul(mul(qx(6.0), qy(6.0 * sh)), qz(-2.5 * sway)),
            ("head", "rotation"): mul(mul(qx(-13.0 + 2.0 * abs(c)), qy(-9.0 * sh)), qz(2.0 * sway)),
            ("arm-left", "rotation"): mul(qx(-10.0 + 14.0 * sh), qz(-60.0)),
            ("arm-right", "rotation"): mul(qx(-10.0 - 14.0 * sh), qz(60.0)),
            ("forearm-left", "rotation"): mul(qx(-20.0), qy(-(58.0 + 16.0 * pump_left))),
            ("forearm-right", "rotation"): mul(qx(-20.0), qy(58.0 + 16.0 * pump_right)),
        })
    return out


#   the sprint: both arms swept back, a little out and down
#   (he leans 26 degrees into it, so "back" has to point well down or the fists ride at his shoulders, v02)
ARM_SWEPT = ((0.40, -0.66, -0.64), (0.24, -0.52, -0.82))
SWEPT = {1: _arm(1, *ARM_SWEPT), -1: _arm(-1, *ARM_SWEPT)}


def sprint():
    """He scampers: bent almost double, head up to see where he is going, legs a blur, and
    both arms thrown straight back behind him, the way a boy runs when he has just done
    something and is leaving."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.6)
        lift = abs(s) ** 1.2
        rock = math.sin(phase - 0.3)
        flutter = math.sin(2.0 * phase)
        _add(out, t, {
            ("root", "translation"): (0.0, 0.004 + 0.040 * lift, 0.0),
            ("root", "rotation"): mul(qx(17.0), qz(2.0 * rock)),
            ("root", "scale"): _squash(0.93 + 0.11 * lift),
            ("leg-left", "rotation"): mul(qx(-50.0 * sh), qz(3.0 - 1.5 * rock)),
            ("leg-right", "rotation"): mul(qx(50.0 * sh), qz(-3.0 - 1.5 * rock)),
            ("torso", "rotation"): mul(mul(qx(9.0), qy(8.0 * sh)), qz(-2.5 * rock)),
            ("head", "rotation"): mul(mul(qx(-24.0 + 2.0 * lift), qy(-6.0 * sh)), qz(1.5 * rock)),
            # both arms swept back and a little out, trailing, fluttering with each step
            ("arm-left", "rotation"): mul(qx(5.0 * flutter), SWEPT[1][0]),
            ("arm-right", "rotation"): mul(qx(-5.0 * flutter), SWEPT[-1][0]),
            ("forearm-left", "rotation"): SWEPT[1][1],
            ("forearm-right", "rotation"): SWEPT[-1][1],
            ("forearm-left", "scale"): (1.10, 1.0, 1.0),
            ("forearm-right", "scale"): (1.10, 1.0, 1.0),
        })
    return out


#   arms up: where the left arm points at the top of the jump. His ears and his cap stand out
#   past his shoulders, so the V is thrown up and out, clear of them.
ARM_UP = ((0.82, 0.34, 0.12), (0.40, 0.90, 0.14))
ARM_UP_REACH = 0.78


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. He goes up like something let off a spring: a long stretch
    off both feet, both arms flung up in a V through the elbows, then his knees snatched up
    under him and his head thrown back to look at how high he got."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.11)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.12)
        ring = math.cos(2.0 * math.pi * t / 0.40) * math.exp(-t / 0.14)
        row = {
            ("root", "scale"): _squash(1.0 + 0.24 * ring),
            ("root", "rotation"): qx(-4.0 * hang),
            # both legs the same: straight under him at take-off, then tucked up in front
            ("leg-left", "rotation"): mul(qx(12.0 * rise - 34.0 * hang), qz(2.0 + 5.0 * hang)),
            ("leg-right", "rotation"): mul(qx(12.0 * rise - 34.0 * hang), qz(-2.0 - 5.0 * hang)),
            ("torso", "rotation"): qx(-8.0 * rise - 3.0 * hang),
            ("head", "rotation"): qx(-10.0 * rise - 12.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 10.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.70 + 0.30 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: he comes down the way he does everything, all at once. Legs pedalling under him
    one after the other, arms up in the V and windmilling a little from the elbows, his head
    down looking for the ground. He "goes down easily" and he knows it."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.05 + 0.018 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(6.0), qz(2.4 * s)),
            ("leg-left", "rotation"): mul(qx(-10.0 + 18.0 * s), qz(7.0)),
            ("leg-right", "rotation"): mul(qx(-10.0 - 18.0 * s), qz(-7.0)),
            ("torso", "rotation"): mul(qx(6.0), qz(-2.0 * s)),
            ("head", "rotation"): mul(qx(15.0 + 3.0 * c), qz(-1.5 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(7.0 * s * side))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qx(12.0 * c * side))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.66 + 0.06 * s * side), 1.0, 1.0)
        _add(out, t, row)
    return out


# ---------------------------------------------------------------------------
# THE ACTION CLIPS (2026-10-07): the carry, the throw, the pick-up, the tag, the shove, the two
# gestures, the slide, out of breath, sitting, the knock-down, yes and no. They replace the
# thirteen of the same names copied from the stock rig, which were made for an arm with no elbow.
#
# EIGHT KEEP THE LENGTH AND THE BEAT OF THE CLIP THEY REPLACE, measured off tikboy-redesign.glb
# before these were written (the fist is the far end of the stock arm; "forward" is +z):
#   holding-right-shoot  0.2000 s  the arm is at its furthest forward from the first frame (the game
#                                  plays it from the instant the slipper leaves); straight by 0.06 s
#   pick-up              0.3333 s  fist and head lowest at 0.167 s
#   attack-melee-right   0.4167 s  right fist furthest forward at 0.254 s
#   attack-melee-left    0.4167 s  left fist furthest forward at 0.254 s
#   interact-right/left  0.6667 s  fist furthest forward at 0.179 s, held to 0.40 s
#   slide                0.9500 s  head lowest at 0.250 s, right fist furthest forward at 0.342 s
#   die                  0.3333 s  on the ground from 0.267 s
# FIVE ARE ONLY LOOKED AT, NEVER TIMED AGAINST, and are longer than the stock ones (a sixth of a
# second, or two thirds for the emotes):
#   holding-right 2.0 s loop, crouch 1.8 s loop, sit 0.8 s, emote-yes 1.0 s loop, emote-no 1.2 s loop
# THIRTEEN SILHOUETTES, by where his fists are at each clip's beat (no two the same):
#   carry: right arm stuck out low from his side, the slipper going round on it; left fist ahead
#   throw: off the ground, legs scissored, right arm whipped long and low ahead, left flung up behind
#   pick-up: a split-legged dive, right fist on the ground ahead, left arm out flat to his side
#   tag: a fencer's lunge, ONE LINE from the left fist behind him to the right fist ahead
#   shove: off the ground, left fist planted UP and out ahead, legs and right arm left behind
#   reach right: leaning out to his right, right arm out wide ahead, left hand holding his cap on
#   reach left: left elbow up in front, the fist jerked back over his shoulder, leaning away
#   slide: on his knees, leaning back, right arm punched ahead, left fist dragging on the ground
#   breath: folded double, left fist on his knee, right arm hanging dead to the ground
#   sit: lying back, both fists behind his cap, elbows wide, ankles crossed
#   down: planted upside down on his cap, legs fallen open in the air
#   yes: in the air, a STAR: both arms out level and wide, both legs out wide
#   no: BOTH arms rammed stiff down and out, a knee up: he is stamping
# THE THROW AND THE TAG ARE PARTLY POSED BY THE GAME over the clip: his chest, his head and both
# UPPER arms are overwritten while they play, so in those two the character is in the root, the
# legs and the squash, and each forearm keeps a moderate bend that opens to straight at the beat.
#
# HOW THEY ARE WRITTEN. A clip is a list of (time, pose, ease): the pose is a table of plain
# numbers in HIS OWN space (the `root` bone's), and `_act` walks from each to the next. Where an
# arm points, and which way his head faces, are given in that space whatever his chest is doing.
#   tx ty tz            root translation, metres
#   pitch yaw roll      root rotation, degrees; pitch + tips him FORWARD, roll + leans him to his RIGHT
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


def _own(pair, lean=5.0):
    """A stance written for the idle (in his chest's space, the chest folded `lean`) in his own space."""
    return tuple(_rot(qx(lean), _norm(v)) for v in pair)


#   the stand every one-shot leaves from and comes back to: frame 0 of `idle`
HANG = _own(ARMS_HANG)
HIP = _own(ARM_HIP)
STAND = {
    "tx": 0.0, "ty": 0.0, "tz": 0.0, "pitch": 4.0, "yaw": 0.0, "roll": 0.0, "sq": 1.0,
    "lean": 5.0, "twist": 0.0, "hunch": 0.0, "nod": -3.0, "look": 0.0, "tilt": 0.0,
    "legL": (4.0, 5.0), "legR": (4.0, 5.0),
    "armL": HANG, "armR": HANG, "reachL": 0.0, "reachR": 0.0,
}


def _p(base=None, **changes):
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


def _between(a, b, t):
    return {name: _blend(a[name], b[name], t) for name in a}


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
        return _between(a, b, _EASES[ease](max(0.0, min(1.0, (t - t0) / (t1 - t0)))))
    return _frames(length, pose_at)


#   THE SLING: how he carries the tsinelas. He cannot leave it alone: his right arm is stuck out
#   from his side, low, and the slipper goes ROUND on the end of it, the forearm drawing a cone
#   from the elbow, the way a boy winds up a sling. His left fist is up in front of him, sneaking.
HOLD_LENGTH = 2.0
SPINS = 4


def _hold_at(t):
    turn = 2.0 * math.pi * t / HOLD_LENGTH
    spin = SPINS * turn
    check = _window(t, 0.90, 1.55, 0.12)
    bob = 0.5 - 0.5 * math.cos(spin)                 # down as the slipper comes round the bottom
    sway = math.sin(turn)
    fore = (0.80, -0.22 + 0.50 * math.sin(spin), 0.18 + 0.50 * math.cos(spin))
    return _p(sq=0.94 + 0.030 * bob - 0.03 * check, pitch=7.0, tx=-0.006 * sway, roll=3.0 * sway + 2.0 * math.sin(spin),
              lean=7.0 + 3.0 * check, twist=-12.0 + 4.0 * math.cos(spin), nod=-9.0 + 5.0 * check, look=56.0 * check,
              tilt=-4.0 * sway, legL=(6.0, 10.0 - 3.0 * sway), legR=(2.0, 10.0 + 3.0 * sway),
              armR=((0.88, -0.38, 0.05), fore), reachR=0.40 + 0.10 * bob,
              armL=_own(ARM_SNEAK, 7.0), reachL=0.15)


HOLD = _hold_at(0.0)


def holding_right():
    """A 2 s loop. He cannot leave it alone. His right arm is stuck out low from his side and the
    slipper goes ROUND on the end of it, four times, winding up like a sling, his whole body
    bobbing under each turn; his left fist is up in front of him and he is low on his feet,
    shifting from one to the other. Half way through his head snaps to his left to see who
    is coming, and back; the slipper never stops. Frame 0 is the carry the throw leaves from."""
    return _frames(HOLD_LENGTH, _hold_at)


def holding_right_shoot():
    """THE THROW, from the instant the slipper leaves (the game winds the arm up itself, plays
    this at the release, and poses his chest, head and upper arms over it). Twice the throwing
    arm on half the boy: he throws so hard he LEAVES THE GROUND. Both feet come off it, legs
    scissored, his whole body turned in behind the arm and stretched out flat along the throw
    (0.06 s, the forearm dead straight and long); he comes down on the front foot in a heap
    (0.11 s) and bounces back up into the carry, the slipper already going round again."""
    thrown = _p(HOLD, sq=1.12, ty=0.050, tz=0.030, pitch=24.0, yaw=24.0, twist=16.0, lean=8.0, nod=-26.0,
                legL=(36.0, 8.0), legR=(-44.0, 10.0),
                armR=((0.52, 0.20, 0.83), (0.38, 0.24, 0.89)), reachR=0.50,
                armL=((0.55, 0.05, -0.83), (0.40, 0.25, -0.88)), reachL=0.20)
    heap = _p(thrown, sq=0.82, ty=-0.014, tz=0.042, pitch=15.0, yaw=31.0, twist=20.0, lean=16.0, nod=-8.0,
              legL=(26.0, 12.0), legR=(-22.0, 13.0),
              armR=((0.20, -0.42, 0.88), (-0.06, -0.50, 0.86)), reachR=0.12,
              armL=((0.70, -0.20, -0.68), (0.62, -0.30, -0.72)), reachL=0.0)
    spring = _p(HOLD, sq=1.04, ty=0.008, yaw=9.0, legL=(10.0, 9.0), legR=(-6.0, 9.0))
    return _act(0.20, [(0.0, HOLD), (0.06, thrown, "out"), (0.11, heap, "in"), (0.165, spring, "out"), (0.20, HOLD)])


def pick_up():
    """He does not stop for it. He DIVES: legs split fore and aft, down flat over the front one,
    his right fist swiping along the ground for the slipper (lowest at 0.167 s), his left arm
    stuck out to his side like a wing, and his eyes never leave the street ahead. Then he pops
    back up, taller than he is, the slipper held up where you can see he has it."""
    down = _p(sq=0.74, ty=-0.020, tz=0.020, pitch=24.0, yaw=12.0, lean=22.0, twist=14.0, nod=-30.0,
              legL=(46.0, 10.0), legR=(-50.0, 10.0),
              armR=((0.40, -0.52, 0.76), (0.22, -0.56, 0.80)), reachR=0.55,
              armL=((0.96, 0.25, -0.15), (0.95, 0.30, -0.05)), reachL=0.15)
    up = _p(sq=1.10, ty=0.006, pitch=0.0, lean=-5.0, nod=-12.0, legL=(2.0, 7.0), legR=(2.0, 7.0),
            armR=((0.88, 0.10, 0.40), (0.60, 0.70, 0.40)), reachR=0.40)
    return _act(1.0 / 3.0, [(0.0, STAND), (1.0 / 6.0, down, "in"), (0.275, up, "out"), (1.0 / 3.0, STAND)])


def attack_melee_right():
    """THE TAG. He is the smallest one out there and he tags like a fencer: a quick sink with the
    elbow drawn back (0.06 s), then he goes out FLAT, the right arm long and level ahead and the
    left thrown straight out behind it, one line from fist to fist, chin up to watch it land
    (far point 0.254 s). He snatches it back as if you were hot. The game poses his chest, head,
    upper arms, root and legs over this while the tag is live, so the legs here are mild and the
    right elbow is straight from 0.13 s; what is his own all through is the sink, the stretch
    and the snatch back."""
    wind = _p(sq=0.88, lean=10.0, twist=-22.0, nod=-2.0, legL=(4.0, 7.0), legR=(4.0, 7.0),
              armR=((0.60, -0.40, -0.69), (0.66, -0.66, -0.20)), armL=((0.55, -0.50, 0.66), (0.30, -0.20, 0.93)))
    out = _p(sq=1.06, pitch=14.0, lean=8.0, twist=24.0, nod=-16.0, tz=0.015, legL=(9.0, 5.0), legR=(-7.0, 5.0),
             armR=((0.44, 0.30, 0.84), (0.36, 0.30, 0.88)), reachR=0.30,
             armL=((0.62, -0.40, -0.68), (0.56, -0.32, -0.76)), reachL=0.10)
    far = _p(out, sq=1.11, pitch=20.0, lean=10.0, twist=32.0, nod=-24.0, tz=0.030, legL=(11.0, 5.0), legR=(-10.0, 5.0),
             armR=((0.42, 0.40, 0.81), (0.34, 0.38, 0.86)), reachR=0.55,
             armL=((0.60, -0.38, -0.70), (0.54, -0.36, -0.76)), reachL=0.40)
    held = _p(far, sq=1.03, pitch=17.0, reachR=0.36, reachL=0.16)
    snatch = _p(sq=0.92, pitch=0.0, lean=0.0, nod=-8.0, legL=(2.0, 7.0), legR=(2.0, 7.0),
                armR=((0.72, -0.50, 0.48), (0.10, 0.55, 0.83)))
    return _act(25.0 / 60.0, [(0.0, STAND), (0.06, wind, "out"), (0.13, out, "in"), (0.254, far, "out"),
                         (0.30, held), (0.365, snatch, "in"), (25.0 / 60.0, STAND)])


def attack_melee_left():
    """THE SHOVE. Everybody he shoves is bigger than he is, so he has to JUMP at them. He coils
    right down with his left fist drawn back and holds it a moment; then he goes off both feet,
    stretched out, and plants his left fist up and out ahead of him at the height of YOUR chest,
    his legs left behind in the air and his right arm thrown back (far point 0.254 s); hangs
    on it; and drops back in a squash."""
    coil = _p(sq=0.80, lean=16.0, twist=22.0, nod=-12.0, legL=(4.0, 10.0), legR=(4.0, 10.0),
              armL=((0.66, -0.50, -0.56), (0.60, -0.20, 0.77)), armR=((0.60, -0.60, 0.53), (0.30, -0.10, 0.95)))
    leap = _p(sq=1.15, ty=0.055, tz=0.050, pitch=22.0, twist=-26.0, lean=2.0, nod=-22.0,
              legL=(-34.0, 8.0), legR=(-50.0, 12.0),
              armL=((0.66, 0.34, 0.67), (0.42, 0.62, 0.66)), reachL=0.65,
              armR=((0.62, -0.50, -0.60), (0.52, -0.46, -0.72)), reachR=0.20)
    hang = _p(leap, sq=1.04, ty=0.030, pitch=18.0, legL=(-22.0, 8.0), legR=(-34.0, 12.0), reachL=0.42)
    drop = _p(sq=0.86, tz=0.012, lean=10.0, nod=2.0, legL=(4.0, 10.0), legR=(4.0, 10.0))
    return _act(25.0 / 60.0, [(0.0, STAND), (0.09, coil, "out"), (0.16, _p(coil, sq=0.78), "lin"), (0.254, leap, "in"),
                         (0.31, hang, "out"), (0.37, drop, "in"), (25.0 / 60.0, STAND)])


def interact_right():
    """"Mine." A snatch. He sinks, then his right arm darts out wide ahead of him and his whole
    body leans out after it, his left hand clapped to the peak of his cap to keep it on
    (0.18 s); a second grab at it (0.32 s); and at 0.40 s he has it and it is hugged to his
    chest under both arms, his back hunched over it and his head snapped round over his left
    shoulder to see who saw."""
    sink = _p(sq=0.92, lean=10.0, twist=10.0, nod=0.0, armR=((0.70, -0.60, 0.40), (0.20, -0.20, 0.96)))
    grab = _p(sq=1.05, roll=13.0, tx=-0.022, tz=0.010, lean=12.0, twist=-20.0, nod=-10.0, look=-24.0,
              legL=(2.0, 0.0), legR=(4.0, 17.0),
              armR=((0.70, 0.12, 0.70), (0.58, 0.16, 0.80)), reachR=0.55,
              armL=_own(ARM_CAP, 12.0), reachL=CAP_REACH)
    half = _p(grab, sq=0.99, roll=9.0, tx=-0.015, armR=((0.74, 0.0, 0.67), (0.40, 0.20, 0.90)), reachR=0.15)
    again = _p(grab, sq=1.06, roll=15.0, tx=-0.026, reachR=0.62)
    got = _p(sq=0.88, roll=-5.0, tx=0.006, lean=18.0, twist=12.0, nod=4.0, look=50.0, legL=(4.0, 8.0), legR=(4.0, 8.0),
             armR=((0.62, -0.62, 0.48), (-0.55, 0.20, 0.81)), armL=((0.62, -0.66, 0.42), (-0.50, 0.05, 0.86)))
    return _act(2.0 / 3.0, [(0.0, STAND), (0.07, sink, "out"), (0.18, grab, "in"), (0.25, half, "out"),
                            (0.32, again, "in"), (0.40, again, "lin"), (0.49, got), (0.56, got, "lin"), (2.0 / 3.0, STAND)])


def interact_left():
    """"Not me. HIM." His left fist jabs out at you (0.18 s), then hooks up and back over his own
    shoulder, elbow in the air, jerking a thumb at whoever is behind him: once, and again, his
    body leaning away from it and his head tipped, looking at you sideways from under the cap.
    His right fist never leaves his hip."""
    jab = _p(sq=1.03, lean=8.0, twist=-16.0, nod=-4.0, armR=HIP,
             armL=((0.36, 0.02, 0.93), (0.12, 0.08, 0.99)), reachL=0.32)
    over = ((0.74, 0.42, 0.52), (0.30, 0.60, -0.74))         # the elbow up ahead, the fist back past his ear
    back = _p(sq=0.97, pitch=-5.0, lean=-10.0, twist=14.0, nod=-2.0, look=-22.0, tilt=10.0, roll=5.0,
              legL=(4.0, 5.0), legR=(4.0, 9.0), armR=HIP, armL=over, reachL=0.45)
    cocked = _p(back, sq=1.0, lean=-6.0, tilt=6.0, armL=((0.72, 0.36, 0.59), (0.36, 0.84, -0.40)), reachL=0.25)
    again = _p(back, sq=0.95, lean=-12.0, tilt=12.0, reachL=0.52)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.08, _p(sq=0.95, twist=-8.0, armR=HIP), "out"), (0.18, jab, "in"),
                            (0.29, back, "in"), (0.35, cocked, "out"), (0.41, again, "in"), (0.50, again, "lin"),
                            (2.0 / 3.0, STAND)])


def slide():
    """He has seen it done on television. A sink; he throws himself onto his KNEES, leaning right
    back, both arms flung up (down at 0.25 s) and skids along on them; punches his right arm out
    ahead for the slipper as he goes past it, the left fist dragging on the ground behind him
    (0.342 s); holds the slipper up over his head, still sliding; and springs back onto his
    feet in one hop."""
    up = ((0.80, 0.50, -0.10), (0.50, 0.85, -0.15))
    drag = ((0.55, -0.30, -0.78), (0.30, -0.62, -0.72))
    sink = _p(sq=0.84, lean=16.0, nod=-8.0, legL=(4.0, 8.0), legR=(4.0, 8.0),
              armL=((0.70, -0.45, -0.55), (0.50, -0.60, -0.62)), armR=((0.70, -0.45, -0.55), (0.50, -0.60, -0.62)))
    leap = _p(sq=1.10, ty=0.010, tz=0.040, pitch=-10.0, lean=-6.0, nod=4.0, legL=(-50.0, 6.0), legR=(-36.0, 6.0),
              armL=up, armR=up, reachL=0.40, reachR=0.40)
    knees = _p(sq=0.84, ty=-0.085, tz=0.110, pitch=-22.0, lean=-8.0, nod=14.0, legL=(-110.0, 9.0), legR=(-110.0, 9.0),
               armL=up, armR=up, reachL=0.50, reachR=0.50)
    reach = _p(knees, sq=1.02, ty=-0.082, tz=0.170, pitch=-26.0, lean=4.0, twist=22.0, nod=10.0,
               legL=(-114.0, 9.0), legR=(-114.0, 9.0),
               armR=((0.22, -0.30, 0.93), (0.08, -0.36, 0.93)), reachR=0.70, armL=drag, reachL=0.45)
    show = _p(knees, sq=1.0, ty=-0.082, tz=0.200, pitch=-30.0, lean=-6.0, nod=16.0, legL=(-118.0, 9.0), legR=(-118.0, 9.0),
              armR=up, reachR=0.75, armL=drag, reachL=0.40)
    spring = _p(sq=1.12, ty=0.030, tz=0.090, pitch=6.0, lean=4.0, nod=-8.0, legL=(-20.0, 6.0), legR=(-8.0, 6.0),
                armR=((0.80, 0.20, 0.45), (0.30, 0.85, 0.40)), reachR=0.25)
    land = _p(sq=0.88, tz=0.020, lean=10.0, nod=2.0, legL=(4.0, 9.0), legR=(4.0, 9.0))
    return _act(0.95, [(0.0, STAND), (0.08, sink, "out"), (0.16, leap, "in"), (0.25, knees, "in"), (0.342, reach, "out"),
                       (0.52, show), (0.66, show, "lin"), (0.76, spring, "out"), (0.87, land, "in"), (0.95, STAND)])


CROUCH_LENGTH = 1.8
CROUCH = _p(sq=0.86, ty=-0.010, pitch=8.0, lean=44.0, nod=26.0, legL=(4.0, 15.0), legR=(4.0, 15.0),
            armL=((0.84, -0.42, 0.34), (-0.36, -0.88, 0.30)),
            armR=((0.50, -0.78, 0.38), (0.16, -0.94, 0.30)), reachR=0.30)


def crouch():
    """OUT OF BREATH, a 1.8 s loop. He ran all of it flat out and has nothing left: folded double,
    his left fist propped on his knee and his right arm hanging dead to the ground, swinging.
    Three quick heaves, a boy's, his back jumping up and falling. On the second his head snaps
    UP, eyes on the street: is the taya coming. Not yet. It drops again. The first and the last
    frame are the full bent-over pose: the game loops this while he is winded and also holds
    its last frame as an emote."""
    def pose_at(t):
        p = (3.0 * t / CROUCH_LENGTH) % 1.0
        if t > CROUCH_LENGTH - 1e-6:
            p = 0.0
        heave = _ease(p / 0.30) if p < 0.30 else 1.0 - _ease((p - 0.30) / 0.70)
        peek = _window(t, 0.62, 1.20, 0.13)
        swing = math.sin(2.0 * math.pi * 2.0 * t / CROUCH_LENGTH)
        dead = ((0.50, -0.78, 0.38), (0.16 + 0.20 * swing, -0.94, 0.30 + 0.10 * swing))
        return _p(CROUCH, sq=0.86 + 0.060 * heave, lean=44.0 - 10.0 * heave - 6.0 * peek, nod=26.0 - 10.0 * heave - 50.0 * peek,
                  look=-8.0 * peek, hunch=2.0 * swing, armR=dead, reachR=0.30 - 0.10 * heave)
    return _frames(CROUCH_LENGTH, pose_at)


def sit():
    """0.8 s of getting down, ending seated. He jumps and lands on his bottom (0.34 s), his feet
    flying up; rolls back, and while he is back there his fists go up behind his cap; and he
    settles LYING BACK on nothing, hands behind his head, elbows wide, one ankle crossed over
    the other. Nobody told him to get comfortable."""
    behind = ((0.85, 0.30, -0.40), (-0.25, 0.85, -0.45))      # the elbow out and up, the fist behind his cap
    flung = ((0.80, 0.40, 0.30), (0.50, 0.80, 0.30))
    seat = _p(ty=-0.105, pitch=-26.0, lean=-6.0, nod=6.0, legL=(60.0, -5.0), legR=(70.0, -5.0),
              armL=behind, armR=behind, reachL=1.0, reachR=1.0)
    hop = _p(sq=1.10, ty=0.020, pitch=-6.0, lean=-4.0, nod=-8.0, legL=(30.0, 8.0), legR=(30.0, 8.0),
             armL=flung, armR=flung, reachL=0.30, reachR=0.30)
    land = _p(seat, sq=0.80, pitch=-10.0, lean=8.0, nod=10.0, legL=(96.0, 14.0), legR=(96.0, 14.0),
              armL=flung, armR=flung, reachL=0.10, reachR=0.10)
    roll = _p(seat, sq=1.05, pitch=-40.0, lean=-10.0, nod=14.0, legL=(84.0, 6.0), legR=(96.0, 6.0))
    return _act(0.8, [(0.0, STAND), (0.08, _p(sq=0.86, lean=12.0), "out"), (0.20, hop, "out"), (0.34, land, "in"),
                      (0.50, roll, "out"), (0.66, _p(seat, sq=0.97, pitch=-22.0, legR=(64.0, -5.0))), (0.8, seat)])


def _on_cap(sq, pitch):
    """How high his root has to be for the top of his cap to rest on the ground, upside down."""
    turn = math.radians(pitch)
    return -(0.778 * sq * math.cos(turn)) + 0.16 * math.sin(turn)


def die():
    """KNOCKED DOWN. "Goes down easily", and he is mostly head, so that is the end that lands.
    A jolt, arms out; he is flipped clean over forwards; comes down ON HIS CAP and squashes onto
    it (down at 0.25 s); one bounce; and stays there, planted upside down on his head, legs
    fallen open in the air and his fists on the ground either side of it."""
    out = ((0.95, 0.25, 0.15), (0.80, 0.55, 0.20))
    props = ((0.80, 0.50, 0.33), (0.45, 0.86, 0.22))          # upside down, "up" is the ground
    jolt = _p(sq=0.88, pitch=-10.0, lean=-12.0, nod=-18.0, legL=(8.0, 6.0), legR=(8.0, 6.0), armL=out, armR=out)
    over = _p(sq=1.10, ty=0.34, tz=-0.06, pitch=80.0, lean=10.0, nod=10.0, legL=(-30.0, 14.0), legR=(20.0, 14.0),
              armL=out, armR=out, reachL=0.35, reachR=0.35)
    plant = _p(sq=0.80, ty=_on_cap(0.80, 164.0), tz=-0.20, pitch=164.0, nod=4.0, legL=(40.0, 20.0), legR=(40.0, 20.0),
               armL=props, armR=props, reachL=0.50, reachR=0.50)
    bounce = _p(plant, sq=1.08, ty=_on_cap(1.08, 170.0) + 0.02, pitch=170.0, legL=(-20.0, 34.0), legR=(60.0, 30.0),
                reachL=0.30, reachR=0.30)
    rest = _p(plant, sq=1.0, ty=_on_cap(1.0, 166.0), pitch=166.0, roll=5.0, legL=(-34.0, 30.0), legR=(52.0, 24.0),
              reachL=0.62, reachR=0.62)
    return _act(1.0 / 3.0, [(0.0, STAND), (0.05, jolt, "out"), (0.15, over, "lin"), (0.25, plant, "in"),
                            (0.29, bounce, "out"), (1.0 / 3.0, rest, "in")])


YES_LENGTH = 1.0


def emote_yes():
    """YES! A 1 s loop, and all of him says it: he tucks down small, fists pulled in under his
    chin, and goes off the ground in a STAR, arms and legs flung out as wide as they go, head
    back; hangs there; drops into the tuck; and does it again, higher, tipped over to one side
    this time. He lands wide and loose and is slouching again by the end. From the first tuck
    to the last landing he is never still. (A salute to the peak of his cap was tried first:
    the fist was lost against his cheek and nothing of it showed from behind.)"""
    pulled = ((0.70, -0.62, 0.35), (0.04, 0.45, 0.89))
    wide = ((0.97, 0.22, 0.05), (0.90, 0.42, 0.08))
    tuck = _p(sq=0.80, lean=13.0, nod=6.0, legL=(6.0, 5.0), legR=(6.0, 5.0), armL=pulled, armR=pulled)
    star = _p(sq=1.13, ty=0.060, pitch=-2.0, lean=-9.0, nod=-18.0, legL=(0.0, 40.0), legR=(0.0, 40.0),
              armL=wide, armR=wide, reachL=0.65, reachR=0.65)
    hang = _p(star, sq=1.06, ty=0.066, legL=(0.0, 44.0), legR=(0.0, 44.0), reachL=0.55, reachR=0.55)
    star2 = _p(star, sq=1.15, ty=0.075, roll=-12.0, tilt=8.0, legL=(0.0, 46.0), legR=(0.0, 34.0), reachL=0.70, reachR=0.70)
    hang2 = _p(star2, sq=1.07, ty=0.080, roll=-14.0, reachL=0.58, reachR=0.58)
    land = _p(sq=0.86, lean=6.0, nod=4.0, legL=(4.0, 16.0), legR=(4.0, 16.0),
              armL=((0.90, -0.30, 0.20), (0.80, -0.40, 0.40)), armR=((0.90, -0.30, 0.20), (0.80, -0.40, 0.40)))
    return _act(YES_LENGTH, [(0.0, STAND), (0.09, tuck, "out"), (0.19, star, "out"), (0.31, hang, "lin"),
                             (0.42, tuck, "in"), (0.50, tuck, "lin"), (0.60, star2, "out"), (0.72, hang2, "lin"),
                             (0.84, land, "in"), (1.0, STAND)])


NO_LENGTH = 1.2


def emote_no():
    """NO. AYOKO. A 1.2 s loop, and it is a tantrum: both arms rammed stiff down and out from his
    sides, fists clenched, chin on his chest, and he STAMPS, left, right, left, right, six of
    them, his whole body rocking off each foot and his fists punching down with every one,
    while his head shakes hard the other way. It is on for five sixths of the loop."""
    def pose_at(t):
        on = _window(t, 0.0, NO_LENGTH, 0.12)
        u = (t - 0.12) / 0.96
        live = _window(t, 0.10, 1.10, 0.06)
        stamp = math.sin(2.0 * math.pi * 3.0 * u) * live
        left, right = max(0.0, stamp), max(0.0, -stamp)
        shake = math.sin(2.0 * math.pi * 4.0 * u) * live
        punch = abs(stamp)
        rigid = ((0.62, -0.76 + 0.20 * punch, -0.20), (0.58, -0.78 + 0.26 * punch, -0.24))
        fit = _p(sq=0.90 + 0.09 * punch, ty=0.006 * punch, pitch=7.0, lean=5.0, nod=8.0 - 6.0 * punch, look=38.0 * shake,
                 roll=7.0 * stamp, tx=-0.010 * stamp, hunch=-3.0 * stamp,
                 legL=(4.0 + 56.0 * left, 12.0), legR=(4.0 + 56.0 * right, 12.0),
                 armL=rigid, armR=rigid, reachL=0.42 - 0.22 * punch, reachR=0.42 - 0.22 * punch)
        return _between(STAND, fit, on)
    return _frames(NO_LENGTH, pose_at)


CLIPS = {
    "idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
    "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
    "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
    "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
    "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no,
}
