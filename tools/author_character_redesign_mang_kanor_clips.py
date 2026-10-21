"""New locomotion and action clips for the Mang Kanor redesign.

Imported by tools/author_character_redesign_mang_kanor.py, which writes them into the redesign
.glb IN PLACE OF the clips of the same name copied from character-male-e.glb. Five locomotion
clips and, below them, thirteen action clips (see THE ACTION CLIPS); every other clip in that
file is copied across untouched. A copy of Bayan's clips script rewritten for him:
each character's motion is its OWN (docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so none
of the stances below are anybody else's.

WHAT HIS MOTION SHOULD SAY ABOUT HIM. "Tricycle driver. He knows every corner of this town by
its potholes and he takes them at speed. Braking was never the strong suit."
(ConvertedCharacterSelect). "The neighbourhood tito: belly first, leaning back, wide and
rolling, arms out; a huffing jog" (GaitStyles.MangKanor). BILIS 5: he is FAST, for all of it. So:
  * HE LEADS WITH HIS BELLY. Standing and walking he leans BACK a few degrees, feet wide.
  * HE ROLLS. The weight goes from side to side over each foot; the arms hang out and swing late.
  * HE IS CHEERFUL AND UNHURRIED until he moves, and then he moves quicker than he looks.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a positive angle carries his left leg OUT to his left.

HIS ARMS, AND WHAT THEY CANNOT DO. They are the heroes' length: the elbow is 78 mm from the
shoulder and the middle of the fist 88 mm past it. His belly and his head are both wider than
that, so a fist cannot cross his front or reach his face. Every pose keeps the forearm pointing
forward, outward, up or down. ARMS UP GO THROUGH THE ELBOW (section 13 rule 8): the upper arm
lifts a little and the forearm folds up and out, and may stretch (scale on the forearm bone).

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

    Both are directions for his LEFT arm in the file's space (+x her left, +y up, +z ahead);
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
#   CARRIED: out from the body, hanging, the forearm a little ahead ("arms out", his gait's words)
ARMS_CARRIED = ((0.72, -0.68, 0.10), (0.50, -0.79, 0.36))
#   THE GLASSES, his RIGHT arm (written as a left arm, mirrored below): the elbow out and up, the
#   fist brought up beside his temple where the arm of his glasses is
ARM_GLASSES = ((0.80, 0.46, 0.38), (0.20, 0.86, 0.47))
GLASSES_REACH = 0.55
#   THE FARE CALL, his LEFT arm: thrown up and out through the elbow, the fist high over his head
ARM_HAIL = ((0.90, 0.36, 0.20), (0.66, 0.72, 0.18))
ARM_HAIL_IN = ((0.90, 0.36, 0.20), (0.28, 0.93, 0.22))     # the beckon: the forearm tipped in over his head
HAIL_REACH = 0.95
#   THE HANDLEBARS, both arms: out ahead of him, fists apart at the height of his belly
ARM_BARS = ((0.52, -0.30, 0.80), (0.16, -0.06, 0.985))
BARS_REACH = 0.18
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of a tricycle driver waiting at the corner for his next fare, in three things:

      THE GLASSES (0.6 to 2.3 s). His right fist comes up beside his temple and nudges his
      glasses straight, twice, his head tipping into it; then he lifts his chin and looks down
      the road through them.
      THE FARE CALL (2.6 to 4.9 s). He has seen somebody. He goes up on his toes, throws his
      left arm high and beckons with it twice, leaning that way, head turned to them: "sakay na!"
      THE HANDLEBARS (5.2 to 7.6 s). Nobody came, so he rides in his head. He sits down into
      his knees with both fists out on the handlebars and the engine shaking him, leans into a
      corner to his left, then into one to his right, at speed, and then BRAKES: the whole of
      him pitches forward and rocks back. Then he straightens up, belly first.

    WHY THESE. His roster line: "Tricycle driver. He knows every corner of this town by its
    potholes and he takes them at speed. Braking was never the strong suit." The ride and the
    late brake are that line acted out; the fare call is the job; the glasses are the most
    particular thing on his face. He is an older man and a cheerful one, so nothing here is
    quick except the brake, and between the stances he stands leaning back on his heels.
    NOT USED: anything with a hand across his own front (his arms are short and his belly is
    in the way), folded arms (Dante's), a hitch of the belt or a practice throw (Bayan's), a fan
    (Lola Pacing's).

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 3.0))    # slow: three breaths in the clip
        # the glasses
        specs = _window(t, 0.6, 2.3, 0.35)
        nudge = min(1.0, _bump(t, 1.18, 0.09) + _bump(t, 1.50, 0.09))
        gaze = _window(t, 1.65, 2.35, 0.25)
        # the fare call
        hail = _window(t, 2.6, 4.9, 0.32)
        beckon = min(1.0, _bump(t, 3.35, 0.13) + _bump(t, 3.95, 0.13))
        # the ride
        ride = _window(t, 5.2, 7.6, 0.32)
        lean_l = _window(t, 5.75, 6.40, 0.26)
        lean_r = _window(t, 6.35, 7.00, 0.26)
        brake = _ease((t - 7.02) / 0.07) * (1.0 - _ease((t - 7.12) / 0.34))
        engine = ride * math.sin(2.0 * math.pi * t * 9.0)             # the idle of a two-stroke
        row = {}
        carried = {1: _arm(1, *ARMS_CARRIED), -1: _arm(-1, *ARMS_CARRIED)}
        for side, name in ((1, "left"), (-1, "right")):
            bars = _arm(side, *ARM_BARS)
            if side < 0:
                first = _arm(side, *ARM_GLASSES)
                amount, reach = specs, GLASSES_REACH * specs * (0.85 + 0.15 * nudge)
            else:
                first = tuple(_mix(a, b, beckon) for a, b in zip(_arm(side, *ARM_HAIL), _arm(side, *ARM_HAIL_IN)))
                amount, reach = hail, HAIL_REACH * hail
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(_mix(carried[side][k], first[k], amount), bars[k], ride)
                if k == 1 and side < 0:
                    q = mul(q, qz(side * 5.0 * nudge * specs))
                if k == 0:
                    # on the handlebars the arms steer: the outside arm of each corner pushes ahead
                    q = mul(qy(side * 9.0 * (lean_l - lean_r) * ride), q)
                row[(bone, "rotation")] = q
            row[("forearm-" + name, "scale")] = (1.0 + reach + BARS_REACH * ride * (1.0 + 0.5 * brake), 1.0, 1.0)
        # his head: into the fist, then down the road; to the fare; into each corner; thrown forward by the brake
        look = 6.0 * specs - 16.0 * gaze + 30.0 * hail + 12.0 * lean_l - 12.0 * lean_r
        nod = 3.0 + 7.0 * specs * (1.0 - gaze) - 8.0 * gaze - 6.0 * hail + 4.0 * ride + 11.0 * brake - 0.5 * breath
        tilt = -9.0 * specs * (1.0 - gaze) - 3.0 * nudge * specs + 5.0 * hail + 8.0 * lean_l - 8.0 * lean_r
        # his chest: leaning back on his heels when he stands (belly first); over the bars when he rides
        lean = -4.0 + 2.0 * specs - 3.0 * hail + 13.0 * ride + 11.0 * brake + 1.0 * breath
        twist = 5.0 * specs + 12.0 * hail + 6.0 * (lean_l - lean_r)
        side_lean = 7.0 * hail + 13.0 * lean_l - 13.0 * lean_r
        sink = 0.075 * ride - 0.035 * hail * (0.6 + 0.4 * beckon) + 0.006 * engine + 0.030 * brake
        row.update({
            ("root", "translation"): (0.012 * hail + 0.016 * (lean_l - lean_r), 0.0, 0.004 * engine + 0.016 * brake),
            ("root", "scale"): _squash(1.0 + 0.012 * breath - sink),
            ("root", "rotation"): mul(qz(2.0 * hail + 9.0 * (lean_l - lean_r)), qx(5.0 * brake)),
            ("torso", "rotation"): mul(mul(qx(lean), qy(twist)), qz(side_lean)),
            ("head", "rotation"): mul(mul(qy(look - 0.5 * twist), qx(nod - 0.6 * lean)), qz(tilt - 0.6 * side_lean)),
            # feet apart and ahead of him when he rides, as a man sits a saddle
            ("leg-left", "rotation"): mul(qx(-16.0 * ride + 8.0 * brake), qz(3.0 + 9.0 * ride + 3.0 * hail)),
            ("leg-right", "rotation"): mul(qx(-16.0 * ride + 8.0 * brake), qz(-3.0 - 9.0 * ride + 2.0 * hail)),
        })
        _add(out, t, row)
    return out


def walk():
    """A tito's stroll: belly first and leaning back on his heels, feet wide, the whole of him
    rolling from side to side over each foot, arms out and swinging loose, head level."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.85)
        lift = abs(s) ** 0.9
        roll = math.sin(phase - 0.35)           # the weight arrives after the foot
        thud = math.cos(2.0 * phase - 0.8)      # twice a stride, just after each foot lands
        row = {
            ("root", "translation"): (0.020 * roll, 0.002 + 0.016 * lift, 0.0),
            ("root", "rotation"): mul(qx(-3.0), qz(5.0 * roll)),
            ("root", "scale"): _squash(0.975 + 0.045 * lift),
            ("leg-left", "rotation"): mul(qx(3.0 - 31.0 * sh), qz(5.0 - 5.0 * roll)),
            ("leg-right", "rotation"): mul(qx(3.0 + 31.0 * sh), qz(-5.0 - 5.0 * roll)),
            ("torso", "rotation"): mul(mul(qx(-3.0 + 1.5 * thud), qy(4.0 * sh)), qz(-3.0 * roll)),
            ("head", "rotation"): mul(mul(qx(4.0 + 1.2 * thud), qy(-3.0 * sh)), qz(-2.5 * roll)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            swing = 19.0 * math.sin(phase - 0.3) * side     # late and loose
            upper = _rotx(ARMS_CARRIED[0], swing)
            fore = _rotx(ARMS_CARRIED[1], 1.6 * swing - 6.0)
            q = _arm(side, upper, fore)
            row[("arm-" + name, "rotation")] = q[0]
            row[("forearm-" + name, "rotation")] = q[1]
        _add(out, t, row)
    return out


#   the jog: elbows out and bent, fists ahead of his belly
ARM_JOG = ((0.74, -0.64, 0.02), (0.28, -0.10, 0.955))


def sprint():
    """A huffing jog, and a fast one. He leans into it only a little, his belly leading, fists
    pumping short in front of him with the elbows out, bouncing high off each foot and puffing:
    his chest heaves and his head bobs twice a stride."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.75)
        lift = abs(s) ** 0.8
        rock = math.sin(phase - 0.3)
        huff = math.cos(2.0 * phase - 0.6)
        row = {
            ("root", "translation"): (0.012 * rock, 0.004 + 0.032 * lift, 0.0),
            ("root", "rotation"): mul(qx(7.0), qz(4.0 * rock)),
            ("root", "scale"): _squash(0.95 + 0.085 * lift),
            ("leg-left", "rotation"): mul(qx(-42.0 * sh), qz(5.0 - 3.5 * rock)),
            ("leg-right", "rotation"): mul(qx(42.0 * sh), qz(-5.0 - 3.5 * rock)),
            ("torso", "rotation"): mul(mul(qx(1.0 + 3.0 * huff), qy(7.0 * sh)), qz(-3.0 * rock)),
            ("head", "rotation"): mul(mul(qx(-6.0 + 3.0 * huff), qy(-4.0 * sh)), qz(-2.0 * rock)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            drive = 26.0 * sh * side
            q = _arm(side, _rotx(ARM_JOG[0], 0.8 * drive), _rotx(ARM_JOG[1], 1.3 * drive))
            row[("arm-" + name, "rotation")] = q[0]
            row[("forearm-" + name, "rotation")] = q[1]
        _add(out, t, row)
    return out


#   arms up: where the left arm points at the top of the jump. His ears stand out past his
#   shoulders, so the V is thrown wide of them.
ARM_UP = ((0.88, 0.44, 0.14), (0.56, 0.82, 0.12))
ARM_UP_REACH = 1.05


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet
    together and the same, never a stride. He hops like a man half his age: a stretch, both
    arms thrown up in a V through the elbows, his heels kicked up behind him and his chin up."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.13)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.14)
        ring = math.cos(2.0 * math.pi * t / 0.50) * math.exp(-t / 0.15)
        row = {
            ("root", "scale"): _squash(1.0 + 0.13 * ring),
            ("root", "rotation"): qx(-3.0 * hang),
            # both legs the same: trailing at take-off, then the heels kicked up behind, knees apart
            ("leg-left", "rotation"): mul(qx(8.0 * rise + 16.0 * hang), qz(2.0 + 7.0 * hang)),
            ("leg-right", "rotation"): mul(qx(8.0 * rise + 16.0 * hang), qz(-2.0 - 7.0 * hang)),
            ("torso", "rotation"): qx(-8.0 * rise - 4.0 * hang),
            ("head", "rotation"): qx(-12.0 * rise - 2.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 7.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.75 + 0.25 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: he comes down belly first with his feet pedalling under him, one then the other,
    arms up in the V and flapping, looking down through his glasses at where he will land."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.04 + 0.010 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(6.0), qz(1.5 * s)),
            ("leg-left", "rotation"): mul(qx(-4.0 + 13.0 * s), qz(8.0)),
            ("leg-right", "rotation"): mul(qx(-4.0 - 13.0 * s), qz(-8.0)),
            ("torso", "rotation"): mul(qx(-2.0), qz(-1.2 * s)),
            ("head", "rotation"): mul(qx(13.0 + 1.5 * c), qz(-1.0 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(side * 6.0 * c))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 8.0 * s))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.70 + 0.05 * s * side), 1.0, 1.0)
        _add(out, t, row)
    return out


# ---------------------------------------------------------------------------
# THE ACTION CLIPS (2026-10-07): the carry, the throw, the pick-up, the tag, the shove, the two
# gestures, the slide, out of breath, sitting, the knock-down, yes and no. They replace the
# thirteen of the same names copied from the stock rig, which were made for an arm with no elbow.
#
# EIGHT KEEP THE LENGTH AND THE BEAT OF THE CLIP THEY REPLACE, measured off his own glb before
# these were written (the fist is the far end of the stock arm; "forward" is +z):
#   holding-right-shoot  0.2000 s  starts WITH the arm at its furthest forward, so the game plays
#                                  it from the instant the slipper leaves; the new far point is 0.06 s
#   pick-up              0.3333 s  fist and head lowest at 0.167 s
#   attack-melee-right   0.4167 s  right fist furthest forward at 0.254 s
#   attack-melee-left    0.4167 s  left fist furthest forward at 0.254 s
#   interact-right/left  0.6667 s  fist furthest forward at 0.179 s, held to 0.40 s
#   slide                0.9500 s  head lowest at 0.250 s, right fist furthest forward at 0.342 s
#   die                  0.3333 s  on the ground from 0.267 s
# FIVE ARE ONLY LOOKED AT, NEVER TIMED AGAINST, and are longer than the stock ones (a sixth of a
# second, or two thirds for the emotes):
#   holding-right 2.2 s loop, crouch 1.8 s loop, sit 0.8 s, emote-yes 1.2 s loop, emote-no 1.3 s loop
# THIRTEEN SILHOUETTES, by where the fists are at each clip's beat (no two the same):
#   carry: right fist LOW and wide of his hip, the slipper swinging; left arm tucked behind him
#   throw: leaning BACK on one foot, a knee kicked up ahead, right arm long and HIGH (a lob)
#   pick-up: tipped over sideways to his right, right fist on the ground, left arm flung up as a counterweight
#   tag: pitched forward past his toes, right arm straight ahead, left arm windmilling up behind
#   shove: SIDE ON, left shoulder first, the left forearm stood up as a bar, right arm trailing
#   reach right: right arm out and DOWN ahead, patting the seat ("dito")
#   reach left: left forearm level at his belly from an elbow at his side, the hand out for the fare
#   slide: on his belly, both fists ahead on the handlebars, shoes in the air
#   breath: bent over with both arms hanging straight down, swinging
#   sit: on the ground leaning well back on both arms, legs out wide
#   down: on his back, belly up, both arms and both legs in the air
#   yes: left fist punched straight up off a hop, right fist chambered at his ribs
#   no: both forearms stood up at his shoulders, palms out, leaning away
# THE THROW AND THE TAG ARE PARTLY POSED BY THE GAME over the clip: his chest, his head and both
# UPPER arms are overwritten while they play, so in those two he is in the root, the legs and
# the squash, and each forearm keeps a moderate bend that opens to straight at the beat.
#
# HOW THEY ARE WRITTEN. A clip is a list of (time, pose, ease): the pose is a table of plain
# numbers in HIS OWN space (the `root` bone's), and `_act` walks from each to the next. Where an
# arm points, and which way his head faces, are given in that space whatever his chest is doing.
#   tx ty tz            root translation, metres
#   pitch yaw roll      root rotation, degrees; pitch + tips him FORWARD, yaw + turns him to his
#                       LEFT, roll + tips him to his RIGHT
#   sq                  root height (the width takes up what the height loses)
#   lean twist hunch    chest: + lean folds forward, + twist turns his chest to his LEFT, + hunch
#                       tips it to his RIGHT
#   nod look tilt       head, in his own space: + nod is chin down, + look is to his LEFT
#   legL legR           (forward, out) degrees
#   armL armR           (upper arm, forearm) directions, written for a LEFT arm (+x is OUT)
#   reachL reachR       forearm stretch, as a share of its length
# NOT SEEN IN UNITY.
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


#   his carried arms as they sit in his own space when his chest leans back 4 degrees (the idle's stand)
ARMS_STOOD = tuple(_rot(qx(-4.0), v) for v in ARMS_CARRIED)

#   the stand every one-shot leaves from and comes back to: frame 0 of `idle`
STAND = {
    "tx": 0.0, "ty": 0.0, "tz": 0.0, "pitch": 0.0, "yaw": 0.0, "roll": 0.0, "sq": 1.0,
    "lean": -4.0, "twist": 0.0, "hunch": 0.0, "nod": 1.4, "look": 0.0, "tilt": 0.0,
    "legL": (0.0, 3.0), "legR": (0.0, 3.0),
    "armL": ARMS_STOOD, "armR": ARMS_STOOD, "reachL": 0.0, "reachR": 0.0,
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


#   THE DANGLE: how he carries the tsinelas. Low, out wide of his right hip, hooked on his
#   fingers the way a man carries his own slippers across a wet street.
ARM_DANGLE = ((0.80, -0.60, 0.02), (0.66, -0.66, 0.36))
#   his left arm while he carries: tucked behind him, the fist at the small of his back
ARM_BEHIND = ((0.74, -0.56, -0.38), (-0.10, -0.50, -0.86))
#   a fist on his hip with the elbow stuck out
ARM_HIP = ((0.90, -0.40, -0.16), (-0.30, -0.85, 0.42))
#   a fist chambered at his ribs: the elbow back, the forearm level and ahead
ARM_RIBS = ((0.76, -0.50, -0.42), (0.26, 0.16, 0.95))

#   HOLDING. Belly out, leaning back on his heels, feet apart.
HOLD = _pose(sq=0.99, lean=-7.0, nod=0.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armR=ARM_DANGLE, armL=ARM_BEHIND)
HOLD_LENGTH = 2.2


def holding_right():
    """A 2.2 s loop. He waits for his turn the way he waits for a fare: belly out, rocked back on
    his heels, one hand behind him. The slipper hangs low at his right hip and SWINGS, out ahead
    and back like something hung from a handlebar, a long swing and a shorter one; at the front
    of each he looks down at it past his belly. His weight rolls from one foot to the other and
    he breathes once. Frame 0 is the carry the throw leaves from."""
    def pose_at(t):
        turn = 2.0 * math.pi * t / HOLD_LENGTH
        breath = math.sin(turn)
        sway = math.sin(turn - 0.9) + math.sin(0.9)                 # nothing at the loop's join
        swing = _bump(t, 0.55, 0.16) - 0.55 * _bump(t, 0.95, 0.15) + 0.65 * _bump(t, 1.35, 0.15) \
            - 0.3 * _bump(t, 1.70, 0.13)
        glance = _window(t, 0.30, 1.55, 0.25)
        arm = (_rotx(ARM_DANGLE[0], -14.0 * swing), _rotx(ARM_DANGLE[1], -62.0 * swing))
        return _pose(HOLD, sq=0.99 + 0.012 * breath, tx=-0.007 * sway, roll=1.6 * sway, hunch=-1.5 * sway,
                     lean=-7.0 - 1.2 * breath, twist=-5.0 * glance,
                     nod=9.0 * glance + 3.0 * abs(swing) * glance, look=-22.0 * glance, tilt=-5.0 * glance,
                     legL=(0.0, 6.0 + 1.6 * sway), legR=(0.0, 6.0 - 1.6 * sway),
                     armR=arm, reachR=0.15 + 0.25 * abs(swing),
                     armL=_blend(ARM_BEHIND, ((0.76, -0.52, -0.40), (-0.16, -0.42, -0.89)), 0.5 - 0.5 * math.cos(2.0 * turn)))
    return _frames(HOLD_LENGTH, pose_at)


def holding_right_shoot():
    """THE THROW, from the instant the slipper leaves (the game winds the arm up itself, plays
    this at the release, and poses his chest, head and upper arms over it). He LOBS it, belly
    first: at the release (0.06 s) he is up off the ground leaning BACK, his left knee kicked up
    ahead of him and the arm following the slipper up high; then he comes down on that foot and
    cannot stop, pitching forward over it with a squash at 0.11 s, the off arm thrown up behind;
    and rocks back onto his heels into the carry. The forearm opens from the carry's bend to
    straight and stretched at 0.06 s."""
    lobbed = _pose(HOLD, sq=1.09, pitch=-9.0, ty=0.022, tz=0.018, twist=14.0, lean=-5.0, nod=-8.0,
                   legL=(40.0, 6.0), legR=(-8.0, 6.0),
                   armR=((0.34, 0.34, 0.88), (0.20, 0.42, 0.885)), reachR=0.75,
                   armL=((0.66, -0.46, -0.60), (0.50, -0.52, -0.70)))
    lurch = _pose(HOLD, sq=0.87, pitch=13.0, ty=-0.008, tz=0.042, twist=18.0, lean=9.0, nod=8.0,
                  legL=(20.0, 9.0), legR=(-24.0, 8.0),
                  armR=((0.34, -0.30, 0.89), (0.24, -0.50, 0.83)), reachR=0.10,
                  armL=((0.80, 0.22, -0.56), (0.66, 0.52, -0.54)), reachL=0.15)
    rock = _pose(HOLD, sq=1.03, pitch=-5.0, tz=0.010, legL=(6.0, 6.0), legR=(-4.0, 6.0))
    return _act(0.20, [(0.0, HOLD), (0.06, lobbed, "out"), (0.11, lurch, "lin"), (0.165, rock), (0.20, HOLD)])


def pick_up():
    """His belly will not let him fold over for it. So he goes down SIDEWAYS: feet apart, he
    tips to his right like a tricycle up on two wheels, his right arm dropping straight down to
    the slipper beside his shoe (lowest at 0.167 s) and his left arm thrown up the other side to
    keep him from going over, his eyes on it. He comes up with an overshoot, belly out."""
    down = _pose(sq=0.84, ty=-0.012, tx=-0.012, roll=13.0, hunch=22.0, lean=14.0, twist=-12.0, nod=18.0, look=-28.0,
                 tilt=6.0, legL=(0.0, 4.0), legR=(0.0, 30.0),
                 armR=((0.42, -0.86, 0.28), (0.26, -0.93, 0.26)), reachR=0.60,
                 armL=((0.84, 0.50, -0.10), (0.62, 0.78, -0.08)), reachL=0.30)
    up = _pose(sq=1.07, lean=-11.0, roll=-3.0, nod=-3.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
               armR=((0.74, -0.50, 0.44), (0.34, 0.20, 0.92)), armL=((0.86, -0.46, 0.0), (0.60, -0.70, 0.38)))
    return _act(1.0 / 3.0, [(0.0, STAND), (1.0 / 6.0, down, "in"), (0.275, up), (1.0 / 3.0, STAND)])


def attack_melee_right():
    """THE TAG, and the brakes. A short sink with the elbow drawn back (0.06 s); then he goes
    after you with the whole of him, the arm out straight and level by 0.13 s and still
    reaching, further than his feet allow, to its far point at 0.254 s: pitched forward past
    his toes, the left arm windmilling up behind him. He totters there, then rocks back onto
    his heels with a squash. The game poses his chest, head and upper arms over this while the
    tag is live, so the legs are mild and the right forearm is straight from 0.13 s; what is
    his own all the way through is the sink, the overbalance and the rock back."""
    wind = _pose(sq=0.91, lean=-9.0, twist=-16.0, nod=6.0, legL=(0.0, 5.0), legR=(0.0, 5.0),
                 armR=((0.66, -0.50, -0.56), (0.48, -0.36, 0.80)), armL=((0.66, -0.56, 0.50), (0.40, -0.30, 0.86)))
    out = _pose(sq=1.05, pitch=6.0, lean=7.0, twist=18.0, nod=0.0, tz=0.008, legL=(6.0, 4.0), legR=(-4.0, 4.0),
                armR=((0.22, 0.04, 0.97), (0.12, 0.04, 0.99)), reachR=0.20,
                armL=((0.78, 0.20, -0.60), (0.62, 0.52, -0.58)))
    far = _pose(out, sq=1.09, pitch=17.0, lean=11.0, twist=26.0, tz=0.026, nod=-6.0, legL=(9.0, 4.0), legR=(-8.0, 4.0),
                armR=((0.15, 0.04, 0.99), (0.08, 0.04, 1.0)), reachR=0.62,
                armL=((0.70, 0.46, -0.55), (0.46, 0.80, -0.40)), reachL=0.20)
    totter = _pose(far, sq=1.01, pitch=20.0, twist=22.0, reachR=0.36,
                   armL=((0.88, 0.10, -0.46), (0.84, 0.30, -0.45)), reachL=0.28)
    heels = _pose(sq=0.92, pitch=-6.0, lean=-9.0, nod=6.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
                  armR=((0.74, -0.50, 0.45), (0.46, -0.40, 0.79)), armL=((0.74, -0.50, 0.45), (0.46, -0.40, 0.79)))
    return _act(0.4167, [(0.0, STAND), (0.06, wind, "out"), (0.13, out, "in"), (0.254, far, "out"),
                         (0.31, totter), (0.375, heels), (0.4167, STAND)])


def attack_melee_left():
    """THE SHOVE, as a tricycle takes a gap: side on and not slowing. He draws back onto his
    right foot, turning a little away, the left elbow tucked and the forearm stood up in front
    of it; then swings his left shoulder round and BARGES, forearm first as a bar with his belly behind it, his head
    still turned to look where he is going and the right arm trailing out behind (far point
    0.254 s). He bounces off it and comes back round."""
    bar = ((0.92, -0.36, 0.14), (0.72, 0.64, 0.26))
    wind = _pose(sq=0.88, yaw=12.0, lean=-12.0, tz=-0.026, nod=6.0, look=-10.0, legL=(0.0, 7.0), legR=(0.0, 7.0),
                 armL=((0.78, -0.56, 0.10), (0.20, 0.80, 0.56)))
    coiled = _pose(wind, sq=0.85, yaw=17.0, tz=-0.032, look=-14.0)
    barge = _pose(sq=1.07, yaw=-50.0, tz=0.055, lean=-5.0, hunch=-19.0, nod=2.0, look=46.0,
                  legL=(0.0, 20.0), legR=(0.0, 6.0), armL=bar, reachL=0.45,
                  armR=((0.86, -0.32, -0.30), (0.92, -0.14, -0.30)), reachR=0.22)
    held = _pose(barge, sq=0.97, tz=0.042, hunch=-13.0, yaw=-45.0, reachL=0.28, reachR=0.10)
    return _act(0.4167, [(0.0, STAND), (0.10, wind, "out"), (0.16, coiled, "lin"), (0.254, barge, "in"),
                         (0.31, held, "out"), (0.4167, STAND)])


def interact_right():
    """"Dito. Upo." His right fist comes up, goes out and down ahead of him (0.18 s) and PATS
    the seat he is offering, twice, his head tipped at it and his left fist on his hip. Slow
    and friendly, nothing like the tag."""
    lift = _pose(sq=1.03, lean=-6.0, nod=0.0, armR=((0.62, -0.20, 0.76), (0.30, 0.50, 0.81)), armL=ARM_HIP)
    here = _pose(sq=0.94, lean=9.0, twist=20.0, nod=17.0, look=-16.0, tilt=-5.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
                 armR=((0.46, -0.44, 0.77), (0.30, -0.62, 0.72)), reachR=0.60, armL=ARM_HIP)
    off = _pose(here, sq=1.0, lean=5.0, nod=10.0, armR=((0.50, -0.26, 0.83), (0.36, -0.16, 0.92)), reachR=0.25)
    pat = _pose(here, sq=0.92, lean=11.0, nod=19.0, reachR=0.56)
    held = _pose(here, sq=0.97, lean=8.0, nod=14.0, reachR=0.42)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.07, lift), (0.18, here, "in"), (0.25, off, "out"), (0.32, pat, "in"),
                            (0.40, held, "out"), (2.0 / 3.0, STAND)])


def interact_left():
    """"Bayad po." The fare hand. His left elbow stays at his side and the forearm comes up level
    with the hand out ahead of his belly (0.18 s), leaning back behind it; he jiggles it twice
    for the coins, head cocked, looking over his glasses at you."""
    gather = _pose(sq=0.97, lean=-6.0, nod=4.0, armL=((0.80, -0.58, -0.12), (0.44, -0.40, 0.80)))
    out = _pose(sq=1.02, lean=-13.0, twist=-14.0, roll=-3.0, nod=9.0, look=14.0, tilt=11.0,
                legL=(0.0, 5.0), legR=(0.0, 5.0),
                armL=((0.56, -0.56, 0.61), (0.28, 0.10, 0.955)), reachL=0.60,
                armR=ARM_BEHIND)
    dip = _pose(out, sq=0.95, armL=((0.56, -0.62, 0.55), (0.28, -0.36, 0.89)), reachL=0.46, tilt=6.0)
    jig = _pose(out, sq=1.04, armL=((0.56, -0.50, 0.66), (0.26, 0.42, 0.87)), reachL=0.62, tilt=13.0)
    held = _pose(out, sq=1.0, reachL=0.52)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.08, gather, "out"), (0.18, out, "in"), (0.235, dip), (0.29, jig),
                            (0.345, dip), (0.40, held), (2.0 / 3.0, STAND)])


#   flat on his belly, both fists out ahead of him and apart (which is +y, lying down): the handlebars
ARM_FLAT = ((0.74, 0.58, 0.34), (0.38, 0.91, 0.16))


def slide():
    """He rides it in on his belly. A sink with both arms back; he dives, and lands on the
    belly, which rocks him like a boat: nose down at 0.25 s, both fists out ahead on the
    handlebars and his shoes up in the air; the right fist goes long for the slipper at
    0.342 s as the nose comes up; he skids, still steering; then shoves himself up off both
    fists, comes up too far, and rocks back onto his heels."""
    back = ((0.72, -0.46, -0.52), (0.52, -0.60, -0.60))
    sink = _pose(sq=0.87, lean=12.0, nod=0.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armL=back, armR=back)
    dive = _pose(sq=1.10, pitch=40.0, ty=0.035, tz=-0.070, nod=-25.0, lean=-4.0, legL=(-12.0, 6.0), legR=(-12.0, 6.0),
                 armL=ARM_FLAT, armR=ARM_FLAT, reachL=0.20, reachR=0.20)
    nose = _pose(sq=0.88, pitch=90.0, ty=0.100, tz=-0.250, lean=-6.0, nod=-48.0, legL=(-44.0, 12.0), legR=(-36.0, 12.0),
                 armL=ARM_FLAT, armR=ARM_FLAT, reachL=0.30, reachR=0.30)
    reach = _pose(nose, sq=1.06, pitch=76.0, ty=0.105, tz=-0.235, lean=-12.0, nod=-58.0, look=-8.0,
                  legL=(-20.0, 10.0), legR=(-26.0, 10.0), reachR=0.75, reachL=0.12,
                  armL=((0.86, 0.40, 0.30), (0.62, 0.74, 0.24)))
    skid = _pose(reach, sq=1.0, pitch=82.0, ty=0.102, roll=5.0, legL=(-34.0, 12.0), legR=(-28.0, 12.0), reachR=0.45)
    push = _pose(sq=0.90, pitch=32.0, ty=0.020, tz=-0.085, lean=14.0, nod=-12.0, legL=(-6.0, 8.0), legR=(-6.0, 8.0),
                 armL=((0.60, -0.60, 0.52), (0.30, -0.90, 0.30)), armR=((0.60, -0.60, 0.52), (0.30, -0.90, 0.30)))
    over = _pose(sq=1.05, pitch=-7.0, lean=-9.0, nod=0.0)
    return _act(0.95, [(0.0, STAND), (0.08, sink, "out"), (0.14, dive, "in"), (0.25, nose, "in"), (0.342, reach, "out"),
                       (0.55, skid), (0.71, push), (0.85, over), (0.95, STAND)])


CROUCH_LENGTH = 1.8
ARM_HUNG = ((0.56, -0.72, 0.42), (0.20, -0.95, 0.24))
CROUCH = _pose(sq=0.86, ty=-0.006, lean=44.0, nod=40.0, legL=(0.0, 22.0), legR=(0.0, 22.0),
               armL=ARM_HUNG, armR=ARM_HUNG, reachL=0.40, reachR=0.40)


def crouch():
    """OUT OF BREATH, a 1.8 s loop. He is too round to prop his hands on his knees, so he hangs:
    feet wide, bent over as far as the belly allows, both arms dropped straight down and
    swinging loose. One heave lifts his back and lets it sag; on the second he throws his head
    right back and gulps at the sky, arms swinging out behind him, and flops down again, the
    arms swinging through. The first and the last frame are the full bent-over pose: the game
    loops this while he is winded and also holds its last frame as an emote."""
    def pose_at(t):
        heave = _bump(t, 0.36, 0.17)
        gulp = _window(t, 0.80, 1.42, 0.24)
        flop = _bump(t, 1.50, 0.11)
        # the arms: a pendulum under his shoulders, pushed by each lift of his back
        hang = 22.0 * gulp - 26.0 * flop + 9.0 * heave - 7.0 * _bump(t, 0.60, 0.12)
        arms = (_rotx(ARM_HUNG[0], 0.5 * hang), _rotx(ARM_HUNG[1], hang))
        return _pose(CROUCH, sq=0.86 + 0.04 * heave + 0.12 * gulp - 0.03 * flop,
                     lean=44.0 - 10.0 * heave - 44.0 * gulp + 4.0 * flop,
                     nod=40.0 - 12.0 * heave - 72.0 * gulp + 6.0 * flop,
                     tilt=7.0 * gulp, armL=arms, armR=arms,
                     reachL=0.40 - 0.20 * gulp + 0.10 * flop, reachR=0.40 - 0.20 * gulp + 0.10 * flop)
    return _frames(CROUCH_LENGTH, pose_at)


def sit():
    """0.8 s of getting down, ending seated. He backs onto it as onto his own saddle: a look
    behind and a sink with both arms reaching back; he drops; lands with a squash at 0.42 s, his
    shoes flying up; rolls back onto his arms and forward again. He ends as a man sits on the
    kerb after lunch: leaning well back on both arms planted behind him, belly up, legs out wide."""
    planted = ((0.74, -0.46, -0.50), (0.34, -0.78, -0.52))
    seat = _pose(ty=-0.095, pitch=-15.0, lean=-7.0, nod=8.0, legL=(73.0, 30.0), legR=(73.0, 30.0),
                 armL=planted, armR=planted, reachL=0.25, reachR=0.25)
    back = ((0.76, -0.54, -0.36), (0.44, -0.80, -0.40))
    sink = _pose(sq=0.90, lean=12.0, nod=10.0, look=-30.0, legL=(0.0, 8.0), legR=(0.0, 8.0), armL=back, armR=back)
    drop = _pose(sq=1.05, ty=-0.048, pitch=-10.0, lean=4.0, nod=0.0, legL=(44.0, 16.0), legR=(44.0, 16.0),
                 armL=back, armR=back, reachL=0.15, reachR=0.15)
    land = _pose(seat, sq=0.83, pitch=-8.0, lean=2.0, nod=14.0, legL=(84.0, 34.0), legR=(84.0, 34.0))
    roll = _pose(seat, sq=1.04, pitch=-26.0, lean=-10.0, nod=4.0, legL=(92.0, 30.0), legR=(86.0, 30.0))
    forward = _pose(seat, sq=0.98, pitch=-11.0, lean=-4.0, nod=10.0, legL=(70.0, 31.0), legR=(70.0, 31.0))
    return _act(0.8, [(0.0, STAND), (0.14, sink), (0.32, drop, "in"), (0.42, land, "lin"), (0.54, roll, "out"),
                      (0.67, forward), (0.8, seat)])


def die():
    """KNOCKED DOWN, wheels up. The hit lifts him off his feet backward with all four limbs
    thrown out ahead of him; he lands on his back (0.22 s), squashes and bounces on it once;
    and ends like a tricycle on its roof: on his back, belly up, both arms and both legs in the
    air, one leg settling lower."""
    air = ((0.50, 0.05, 0.86), (0.30, 0.05, 0.95))         # straight up, once he is on his back
    jolt = _pose(sq=0.90, pitch=-10.0, lean=-12.0, nod=-14.0, legL=(10.0, 5.0), legR=(10.0, 5.0),
                 armL=((0.70, 0.10, 0.70), (0.50, 0.40, 0.76)), armR=((0.70, 0.10, 0.70), (0.50, 0.40, 0.76)))
    flung = _pose(sq=1.08, pitch=-52.0, ty=0.050, lean=6.0, nod=10.0, legL=(40.0, 12.0), legR=(40.0, 12.0),
                  armL=air, armR=air, reachL=0.45, reachR=0.45)
    slam = _pose(sq=0.83, pitch=-90.0, ty=0.100, nod=12.0, legL=(84.0, 18.0), legR=(84.0, 18.0),
                 armL=air, armR=air, reachL=0.50, reachR=0.50)
    bounce = _pose(slam, sq=1.06, ty=0.130, nod=-2.0, legL=(60.0, 22.0), legR=(70.0, 22.0), reachL=0.20, reachR=0.20)
    rest = _pose(sq=1.0, pitch=-90.0, ty=0.100, nod=2.0, look=18.0, legL=(76.0, 20.0), legR=(48.0, 16.0),
                 armL=((0.56, 0.10, 0.82), (0.40, 0.20, 0.90)), armR=((0.44, -0.04, 0.90), (0.24, -0.02, 0.97)),
                 reachL=0.30, reachR=0.38)
    return _act(1.0 / 3.0, [(0.0, STAND), (0.05, jolt, "out"), (0.13, flung, "in"), (0.22, slam, "in"),
                            (0.267, bounce, "out"), (1.0 / 3.0, rest, "in")])


def emote_yes():
    """"AYOS!" A 1.2 s loop that he fills. A sink, both fists gathered; then a hop with his left
    fist punched straight up at the sky, arm stretched, heels kicked up behind, chin up, and
    held; down, the fist pumped to his shoulder with a squash and a nod; a second hop and punch,
    higher; a second pump; and he holds the fist at his shoulder a moment, pleased, before it
    drops. The right fist stays chambered at his ribs throughout."""
    sky = ((0.92, 0.38, 0.10), (0.50, 0.86, 0.05))
    pumped = ((0.86, -0.34, 0.12), (0.36, 0.84, 0.40))
    dip = _pose(sq=0.90, lean=3.0, nod=10.0, legL=(0.0, 7.0), legR=(0.0, 7.0), armL=pumped, armR=ARM_RIBS)
    up = _pose(sq=1.10, ty=0.024, roll=-4.0, lean=-9.0, nod=-12.0, tilt=5.0, legL=(-14.0, 8.0), legR=(-14.0, 8.0),
               armL=sky, reachL=1.35, armR=ARM_RIBS)
    up_held = _pose(up, sq=1.04, ty=0.0, legL=(0.0, 5.0), legR=(0.0, 5.0), reachL=1.20)
    pump = _pose(dip, sq=0.88, lean=6.0, nod=16.0)
    up2 = _pose(up, ty=0.032, sq=1.11, reachL=1.45)
    up2_held = _pose(up_held, reachL=1.25)
    pump2 = _pose(dip, sq=0.86, lean=8.0, nod=20.0, legL=(0.0, 9.0), legR=(0.0, 9.0))
    proud = _pose(dip, sq=1.0, lean=-8.0, nod=-2.0, legL=(0.0, 5.0), legR=(0.0, 5.0))
    return _act(1.2, [(0.0, STAND), (0.11, dip), (0.21, up, "out"), (0.36, up_held), (0.44, pump, "in"),
                      (0.55, up2, "out"), (0.72, up2_held), (0.80, pump2, "in"), (0.90, proud, "out"), (1.02, proud),
                      (1.2, STAND)])


NO_LENGTH = 1.3


def emote_no():
    """"Naku, hindi, hindi." A 1.3 s loop. Both hands come up beside his shoulders, palms out,
    and he backs off behind them, leaning away with his chin tucked: no, not him, not today.
    Three times his chest swings one way and his head the other, the forearms fanning out and
    in with it, and he sinks a little on each. Then the hands drop."""
    def pose_at(t):
        up = _window(t, 0.0, NO_LENGTH, 0.20) if t < 0.5 * NO_LENGTH else 1.0 - _ease((t - 1.02) / 0.28)
        wag = math.sin(2.0 * math.pi * (t - 0.20) / 0.28) * _window(t, 0.16, 1.08, 0.10)
        beat = abs(wag)
        splay = 0.46 + 0.26 * wag
        hands = ((0.92, -0.16, 0.30), (splay, 0.84, 0.30))
        other = ((0.92, -0.16, 0.30), (0.46 - 0.26 * wag, 0.84, 0.30))
        return _pose(sq=1.0 - 0.05 * up * beat + 0.02 * up, tz=-0.016 * up, pitch=-7.0 * up,
                     lean=-4.0 - 8.0 * up, twist=13.0 * wag, nod=1.4 + 7.0 * up, look=-24.0 * wag, tilt=4.0 * wag,
                     legL=(0.0, 3.0 + 4.0 * up), legR=(0.0, 3.0 + 4.0 * up),
                     armL=_blend(ARMS_STOOD, hands, up), armR=_blend(ARMS_STOOD, other, up),
                     reachL=0.50 * up, reachR=0.50 * up)
    return _frames(NO_LENGTH, pose_at)


CLIPS = {
    "idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
    "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
    "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
    "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
    "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no,
}
