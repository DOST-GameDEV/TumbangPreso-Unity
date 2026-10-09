"""New locomotion clips for the Totoy redesign PROTOTYPE.

Imported by tools/author_character_redesign_totoy.py, which writes them into the prototype
.glb IN PLACE OF the clips of the same name copied from character-male-a.glb. Nothing in the
game reads them; character-male-a.glb keeps its own. Since 2026-10-07 the thirteen action clips
(the carry and the throw, pick-up, tag, shove, the two gestures, slide, crouch, sit, die, yes
and no) are his own too: see THE ACTION CLIPS below. The rest are copied across untouched. A copy of
Bebang's clips script rewritten for him: each character's motion is its OWN
(docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so none of the numbers below are another's.

WHAT HIS MOTION SHOULD SAY ABOUT HIM. TOTOY is "the little boy of the street: all energy.
Bouncing, head bobbing, arms everywhere; the run flails" (`GaitStyles.Totoy`); speed 5, the
fastest feet in the roster (GAME_OVERVIEW.md); "Raised barefoot on this street. Nobody in this
town has caught him twice" (the character select). A boy of about nine. So:
  * HE IS NEVER STILL. Standing, he is running on the spot; walking, he bounces.
  * HE IS LIGHT. Up on his toes, big squash and stretch, the opposite of Bebang's planted block.
  * HIS ARMS ARE LOOSE. They swing wider than the stride needs and the forearms trail behind.
  * HE IS ALWAYS LOOKING FOR WHO IS CHASING HIM, and always pushing his glasses back up.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops his left arm from straight out to hanging.

ARMS UP GO THROUGH THE ELBOW (section 13 rule 8, amended by the owner on Dante: "the jump looks
like he's shrugging"). His elbow is at the mouth of his sleeve, 82 mm from the shoulder; the
bare forearm and the fist ride the forearm bone, 116 mm more to the fist's end (the heroes'
arm). A straight arm cannot rise above level (his ears and the rim of his hair are over it), so
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



#   where his LEFT arm points in each stance: (upper arm, forearm), in the file's space (+x his
#   left, +y up, +z ahead). The right is the mirror.
#   HANGING, HIS WAY: loose, a little off his sides, the fists just forward of his shorts.
ARMS_HANG = ((0.50, -0.86, 0.04), (0.40, -0.90, 0.16))
#   ON THE SPOT. `swing` is +1 with the fist forward and up, -1 with it back by his hip.
def _pump(side, swing):
    f = max(swing, 0.0)
    b = max(-swing, 0.0)
    upper = (0.42, -0.84 + 0.10 * f, 0.34 * swing)
    fore = (0.18 - 0.10 * f, -0.30 + 0.62 * f - 0.20 * b, 0.90 - 0.30 * f)
    return _arm(side, upper, fore)


#   THE GLASSES. His right arm (written as a left arm, mirrored below): the elbow comes forward
#   and the forearm folds up and in, so the fist's knuckle meets the bridge between his lenses.
#   (v02's elbow stayed low and the fist stopped at his mouth; the bridge is 190 mm above the
#   shoulder and the heroes' arm is short, so the elbow lifts and the forearm stretches.)
ARM_PUSH = ((0.12, 0.42, 0.90), (-0.52, 0.70, 0.49))
PUSH_REACH = 0.74
#   ON YOUR MARKS. Both arms straight down in front of him, the fists toward the ground.
ARM_MARKS = ((0.40, -0.80, 0.44), (0.24, -0.94, 0.24))
MARKS_REACH = 0.30
IDLE_LENGTH = 8.0
STEP_RATE = 3.0        # steps a second when he runs on the spot


def idle():
    """Eight seconds of a boy who cannot stand still, in three things he does:

      RUNNING ON THE SPOT (0.5 to 2.7 s). Six quick knee lifts, foot to foot, the whole of him
      bouncing, his fists pumping at his chest and his head bobbing with every step.
      HIS GLASSES (3.0 to 4.7 s). They have slid down. He ducks his head, brings his right fist
      up and shoves them back up the bridge with one knuckle, and his chin comes up with the
      shove so he ends looking over them at you.
      ON YOUR MARKS (5.0 to 7.5 s). He drops into a runner's crouch, one foot back, fists toward
      the ground, and checks over his left shoulder and then his right for whoever is "it".
      Then he pops back up with a hop.

    WHY THESE. He is "all energy" (GaitStyles.Totoy), the fastest feet in the roster (speed 5)
    and "nobody in this town has caught him twice": so he waits for his turn the way the
    fastest kid waits, already running, and already looking for who is chasing. The glasses
    are the one thing on him that slows him down, and a boy who runs in glasses pushes them up
    all day. None of the three is another character's (Bebang: fist into palm, her headband, a
    shoulder roll; Dante: fists on hips and folded arms; Sean: a flex and a guard; Amihan and
    Cheska: hops and hands behind the back).

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 6.0))
        run = _window(t, 0.5, 2.7, 0.30)
        glasses = _window(t, 3.0, 4.7, 0.35)
        marks = _window(t, 5.0, 7.5, 0.30)
        st = math.sin(2.0 * math.pi * STEP_RATE * (t - 0.5))
        hop = abs(st) ** 0.8
        shove = _bump(t, 3.95, 0.13)                      # the knuckle goes up the bridge
        duck = _window(t, 3.2, 3.9, 0.25)
        look = 40.0 * _window(t, 5.55, 6.30, 0.22) - 40.0 * _window(t, 6.35, 7.10, 0.22)
        settle = _bump(t, 7.52, 0.09) - 0.6 * _bump(t, 7.72, 0.10)
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            hang = _arm(side, *ARMS_HANG)
            pump = _pump(side, st * side)                 # the left fist comes forward as the right knee lifts
            at_marks = _arm(side, *ARM_MARKS)
            push = _arm(side, *ARM_PUSH)
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(hang[k], pump[k], run)
                if side < 0:
                    q = _mix(q, push[k], glasses)
                    if k == 1:
                        q = mul(q, qz(-9.0 * shove * glasses))
                q = _mix(q, at_marks[k], marks)
                row[(bone, "rotation")] = q
            reach = MARKS_REACH * marks + (PUSH_REACH * glasses * (0.85 + 0.15 * shove) if side < 0 else 0.0)
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        lift_left = max(st, 0.0) ** 0.8 * run
        lift_right = max(-st, 0.0) ** 0.8 * run
        nod = 5.0 * hop * run + glasses * (13.0 * duck - 15.0 * shove) - 12.0 * marks
        dip = 0.13 * marks + 0.05 * settle - 0.045 * hop * run
        row.update({
            ("root", "translation"): (0.004 * st * run, 0.030 * hop * run - 0.02 * settle, 0.0),
            ("root", "scale"): _squash(1.0 + 0.010 * breath - dip),
            ("root", "rotation"): qx(2.0 * marks),
            ("torso", "rotation"): mul(mul(qx(4.0 * run + 3.0 * glasses * duck + 15.0 * marks + 0.8 * breath), qy(7.0 * st * run + 0.30 * look)),
                                       qz(2.0 * st * run)),
            ("head", "rotation"): mul(mul(qy(0.80 * look - 5.0 * st * run + 7.0 * glasses), qx(nod - 0.6 * breath)), qz(5.0 * st * run - 6.0 * glasses)),
            # on the spot the knees come up in front; on his marks the left foot goes back
            # (v02 lifted them 40 degrees; the leg has no knee, and the whole sole swung up at the camera)
            ("leg-left", "rotation"): mul(qx(-24.0 * lift_left + 8.0 * lift_right + 24.0 * marks), qz(3.0)),
            ("leg-right", "rotation"): mul(qx(-24.0 * lift_right + 8.0 * lift_left - 18.0 * marks), qz(-3.0)),
        })
        _add(out, t, row)
    return out


def walk():
    """He does not walk, he bounces: quick springy steps up on his toes, his arms swinging
    loose and far too wide for the size of the stride, his head bobbing along and rocking
    from side to side."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.8)
        c = math.cos(phase)
        lift = abs(s) ** 1.1
        sway = math.sin(phase - 0.25)
        late = math.sin(phase - 0.6)                 # the forearms trail the upper arms
        _add(out, t, {
            ("root", "translation"): (0.010 * sway, 0.004 + 0.044 * lift, 0.0),
            ("root", "rotation"): mul(qx(3.0), qz(3.0 * sway)),
            ("root", "scale"): _squash(0.95 + 0.085 * lift),
            ("leg-left", "rotation"): mul(qx(-32.0 * sh), qz(2.5 - 2.0 * sway)),
            ("leg-right", "rotation"): mul(qx(32.0 * sh), qz(-2.5 - 2.0 * sway)),
            ("torso", "rotation"): mul(mul(qx(2.0), qy(10.0 * sh)), qz(-3.5 * sway)),
            ("head", "rotation"): mul(mul(qx(-2.0 + 5.0 * abs(c)), qy(-7.0 * sh)), qz(5.0 * sway)),
            ("arm-left", "rotation"): mul(qx(40.0 * sh), qz(-64.0 + 6.0 * abs(s))),
            ("arm-right", "rotation"): mul(qx(-40.0 * sh), qz(64.0 - 6.0 * abs(s))),
            ("forearm-left", "rotation"): mul(qx(-18.0), qy(-(26.0 - 20.0 * late))),
            ("forearm-right", "rotation"): mul(qx(-18.0), qy(26.0 + 20.0 * late)),
        })
    return out


def sprint():
    """THE RUN FLAILS (GaitStyles.Totoy). Head down and forward, the legs a blur under him, and
    his arms not pumping at all but thrown: flung wide and far back and far forward, the
    forearms flapping a beat behind them. It is the fastest run on the street and it looks
    like he is falling the whole way."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.7)
        lift = abs(s) ** 1.2
        rock = math.sin(phase - 0.3)
        late = math.sin(phase - 0.9)
        _add(out, t, {
            ("root", "translation"): (0.0, 0.004 + 0.050 * lift, 0.0),
            ("root", "rotation"): mul(qx(16.0), qz(2.5 * rock)),
            ("root", "scale"): _squash(0.94 + 0.12 * lift),
            ("leg-left", "rotation"): mul(qx(-60.0 * sh), qz(3.0 - 1.5 * rock)),
            ("leg-right", "rotation"): mul(qx(60.0 * sh), qz(-3.0 - 1.5 * rock)),
            ("torso", "rotation"): mul(mul(qx(7.0), qy(14.0 * sh)), qz(-3.0 * rock)),
            ("head", "rotation"): mul(mul(qx(-17.0 + 3.0 * lift), qy(-9.0 * sh)), qz(4.0 * rock)),
            # thrown from the shoulder, held wide of his sides; each rises as it goes back
            ("arm-left", "rotation"): mul(qx(66.0 * sh), qz(-52.0 + 14.0 * max(sh, 0.0))),
            ("arm-right", "rotation"): mul(qx(-66.0 * sh), qz(52.0 - 14.0 * max(-sh, 0.0))),
            ("forearm-left", "rotation"): mul(qx(-10.0), qy(-(38.0 - 34.0 * late))),
            ("forearm-right", "rotation"): mul(qx(-10.0), qy(38.0 + 34.0 * late)),
        })
    return out


#   arms up: where the left arm points at the top of the jump. His ears and the rim of his bowl
#   cut stand out past his shoulders, so the V is thrown up and out, clear of them.
ARM_UP = ((0.76, 0.50, 0.14), (0.42, 0.90, 0.08))
ARM_UP_REACH = 0.86


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. He goes up like a frog: one long stretch off both feet, both
    arms flung up in a V through the elbows, and both heels kicked up BEHIND him and apart as
    he hangs, his chin up, delighted with himself."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.12)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.13)
        ring = math.cos(2.0 * math.pi * t / 0.46) * math.exp(-t / 0.15)
        row = {
            ("root", "scale"): _squash(1.0 + 0.24 * ring),
            ("root", "rotation"): qx(3.0 * hang),
            # both legs the same: straight under him at take-off, then the heels kicked up behind and apart
            ("leg-left", "rotation"): mul(qx(-8.0 * rise + 34.0 * hang), qz(2.0 + 10.0 * hang)),
            ("leg-right", "rotation"): mul(qx(-8.0 * rise + 34.0 * hang), qz(-2.0 - 10.0 * hang)),
            ("torso", "rotation"): qx(-8.0 * rise - 6.0 * hang),
            ("head", "rotation"): qx(-12.0 * rise - 9.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 10.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.70 + 0.30 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: he is still running. His legs pedal under him as if the ground were there, his
    arms are up in the V and flapping, and he looks down at where he is going to land."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.05 + 0.016 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(5.0), qz(2.0 * s)),
            ("leg-left", "rotation"): mul(qx(-6.0 - 26.0 * s), qz(6.0)),
            ("leg-right", "rotation"): mul(qx(-6.0 + 26.0 * s), qz(-6.0)),
            ("torso", "rotation"): mul(qx(4.0), qz(-2.0 * s)),
            ("head", "rotation"): mul(qx(13.0 + 2.0 * c), qz(-2.0 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(8.0 * s * side))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(10.0 * c * side))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.66 + 0.06 * s * side), 1.0, 1.0)
        _add(out, t, row)
    return out


# ---------------------------------------------------------------------------
# THE ACTION CLIPS (2026-10-07): the carry, the throw, the pick-up, the tag, the shove, the two
# gestures, the slide, out of breath, sitting, the knock-down, yes and no. They replace the
# thirteen of the same names copied from the stock rig, which were made for an arm with no elbow.
#
# EIGHT KEEP THE LENGTH AND THE BEAT OF THE CLIP THEY REPLACE, measured off totoy-redesign.glb
# before these were written (the fist is the far end of the stock arm; "forward" is +z):
#   holding-right-shoot  0.2000 s  starts WITH the arm at its furthest forward (0.000 s), so the
#                                  game plays it from the instant the slipper leaves
#   pick-up              0.3333 s  fist and head lowest at 0.167 s
#   attack-melee-right   0.4167 s  right fist furthest forward at 0.254 s
#   attack-melee-left    0.4167 s  left fist furthest forward at 0.254 s
#   interact-right/left  0.6667 s  fist furthest forward at 0.179 s, held to about 0.40 s
#   slide                0.9500 s  head lowest at 0.250 s, right fist furthest forward at 0.342 s
#   die                  0.3333 s  down from about 0.27 s, lowest on its last frame
# FIVE ARE ONLY LOOKED AT, NEVER TIMED AGAINST, and are longer than the stock ones (0.1667 s, or
# 0.6667 s for the two emotes) so that there is room to move:
#   holding-right 1.6 s loop, crouch 1.5 s loop, sit 0.8 s, emote-yes 1.0 s loop, emote-no 1.2 s loop
# He is nine and never still, so his loops are the short end of what is allowed and full of hops.
# THIRTEEN SILHOUETTES, by where the fists are at each clip's beat (no two the same):
#   carry: right arm stuck straight out to his side, the slipper flapped on the end of it
#   throw: in the air, flat out forward, heels kicked up, right arm long ahead, left flung back
#   pick-up: tipped over on ONE leg, the other kicked up behind, right fist on the ground
#   tag: up on his toes, right arm one long line up at a bigger kid's shoulder, left straight back
#   shove: turned side on, left shoulder and elbow first, forearm up like a shield, feet off the ground
#   reach right: squatted low and wide, right fist snatching at knee height, left arm out for balance
#   reach left: leaning out over his toes, left fist at his mouth, shouting; right arm thrown back
#   slide: on his belly, BOTH arms out ahead, chin up, both heels in the air
#   breath: folded in half, both arms hanging to the ground like washing
#   sit: on the ground, legs out in a V, leaning forward with a fist on each foot
#   down: face in the dirt, legs stuck up in a V, arms flat back along his sides
#   yes: at the top of a jump, right fist punched straight up, left elbow yanked down
#   no: both fists clamped over his ears, elbows out wide, the whole of him twisting
# THE THROW AND THE TAG ARE PARTLY POSED BY THE GAME over the clip: his chest, his head and both
# UPPER arms are overwritten while they play, so in those two the character is in the root, the
# legs and the squash, and each forearm keeps a moderate bend that opens to straight at the beat.
#
# HOW THEY ARE WRITTEN. A clip is a list of (time, pose, ease): the pose is a table of plain
# numbers in HIS OWN space (the `root` bone's), and `_act` walks from each to the next. Where an
# arm points, and which way his head faces, are given in that space whatever his chest is doing.
#   tx ty tz            root translation, metres
#   pitch yaw roll      root rotation, degrees; pitch + tips him FORWARD, yaw + turns him to his LEFT
#   sq                  root height (the width takes up what the height loses)
#   lean twist hunch    chest: + lean folds forward, + twist turns his chest to his LEFT
#   nod look tilt       head, in his own space: + nod is chin down, + look is to his LEFT
#   legL legR           (forward, out) degrees
#   armL armR           (upper arm, forearm) directions, written for a LEFT arm (+x is OUT)
#   reachL reachR       forearm stretch, as a share of its length
# ---------------------------------------------------------------------------

def _inv(q):
    return (-q[0], -q[1], -q[2], q[3])


def _rotx(v, deg):
    """A direction turned about +x: positive tips an upright thing forward."""
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return (v[0], v[1] * c - v[2] * s, v[1] * s + v[2] * c)


def _street(pitch, upper, fore):
    """An arm given by where it points on the STREET (down is down) when he is tipped by `pitch`."""
    return (_rotx(upper, -pitch), _rotx(fore, -pitch))


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
    "lean": 0.0, "twist": 0.0, "hunch": 0.0, "nod": 0.0, "look": 0.0, "tilt": 0.0,
    "legL": (0.0, 3.0), "legR": (0.0, 3.0),
    "armL": ARMS_HANG, "armR": ARMS_HANG, "reachL": 0.0, "reachR": 0.0,
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


#   THE CARRY. He holds the slipper the way a boy holds a thing he has just won: stuck out at
#   arm's length to his side, where everybody behind him can see it, and flapped.
ARM_SHOW = ((0.88, -0.42, 0.10), (0.90, -0.30, 0.28))
ARM_SHOW_FLAP = ((0.94, -0.16, 0.10), (0.62, 0.72, 0.30))     # the forearm flipped up from the elbow
#   his left fist meanwhile, up by his chest and ready to run
ARM_TUCK = ((0.46, -0.84, 0.14), (0.14, 0.34, 0.92))
HOLD = _pose(sq=0.98, lean=3.0, twist=-6.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armR=ARM_SHOW, armL=ARM_TUCK,
             reachR=0.10)
HOLD_LENGTH = 1.6


def holding_right():
    """A 1.6 s loop. He has it, and he cannot keep still about it: four little hops from foot
    to foot, the slipper held out at arm's length to his right and flapped up and down on the
    end of it (two quick flaps, then a bigger one), his left fist pumping at his chest, and
    his head going over his right shoulder and then his left for whoever is coming to take it
    back. Frame 0 is the carry the throw leaves from."""
    def pose_at(t):
        turn = 2.0 * math.pi * t / HOLD_LENGTH
        step = math.sin(2.0 * turn)                       # two full steps a loop: four hops
        hop = abs(step) ** 0.8
        flap = min(1.0, _bump(t, 0.30, 0.07) + _bump(t, 0.52, 0.07) + _bump(t, 1.12, 0.11))
        look = -42.0 * _window(t, 0.20, 0.78, 0.18) + 46.0 * _window(t, 0.88, 1.46, 0.18)
        arm = _blend(ARM_SHOW, ARM_SHOW_FLAP, flap)
        pump = 0.5 + 0.5 * math.sin(2.0 * turn - 1.2) - (0.5 + 0.5 * math.sin(-1.2))
        tuck = _blend(ARM_TUCK, ((0.50, -0.78, 0.30), (0.10, 0.62, 0.78)), 0.5 * pump)
        return _pose(HOLD, sq=0.98 + 0.07 * hop - 0.02 * flap, ty=0.034 * hop, tx=0.010 * step, roll=4.0 * step,
                     lean=3.0 + 2.0 * hop, twist=-6.0 + 0.25 * look, nod=4.0 * hop - 3.0 * flap, look=look,
                     tilt=-4.0 * step,
                     legL=(16.0 * max(step, 0.0), 6.0 + 9.0 * max(step, 0.0)),
                     legR=(16.0 * max(-step, 0.0), 6.0 + 9.0 * max(-step, 0.0)),
                     armR=arm, reachR=0.10 + 0.40 * flap, armL=tuck)
    return _frames(HOLD_LENGTH, pose_at)


def holding_right_shoot():
    """THE THROW, from the instant the slipper leaves (the game winds the arm up itself, plays
    this at the release, and poses his chest, head and upper arms over it). He is too small to
    throw with his arm, so he throws with ALL of him: both feet leave the ground and he goes
    out flat behind the slipper like a frog off a wall, heels kicked up and apart, stretched
    as long as he gets (0.05 s, the forearm dead straight and stretched from the first frames
    on); comes down on his front foot in a squash with the back leg still in the air (0.11 s);
    and bounces back to the carry. Where the game does not pose him the same thing plays with
    his own arms: the right one long down the line of the throw, the left flung up behind."""
    flung = ((0.62, 0.34, -0.71), (0.50, 0.52, -0.69))
    flown = _pose(HOLD, sq=1.16, ty=0.060, tz=0.030, pitch=30.0, lean=8.0, twist=12.0, nod=-22.0,
                  legL=(-34.0, 16.0), legR=(-40.0, 16.0),
                  armR=_street(30.0, (0.22, -0.02, 0.97), (0.10, -0.06, 0.99)), reachR=0.90,
                  armL=flung, reachL=0.25)
    landed = _pose(HOLD, sq=0.84, ty=0.0, tz=0.040, pitch=14.0, lean=10.0, twist=16.0, nod=-8.0,
                   legL=(26.0, 6.0), legR=(-38.0, 12.0),
                   armR=_street(14.0, (0.30, -0.40, 0.87), (0.16, -0.50, 0.85)), reachR=0.15,
                   armL=((0.80, -0.20, -0.56), (0.70, -0.30, -0.65)))
    rebound = _pose(HOLD, sq=1.06, ty=0.016, tz=0.012, pitch=3.0, legL=(8.0, 6.0), legR=(-8.0, 8.0))
    return _act(0.20, [(0.0, HOLD), (0.05, flown, "out"), (0.11, landed, "in"), (0.165, rebound, "out"),
                       (0.20, HOLD)])


def pick_up():
    """He does not stop for it. He swoops: tips right over on his LEFT leg with the right one
    kicked up high behind him and his left arm thrown up for balance, like a boy playing
    aeroplanes, and his right fist skims the slipper off the ground on the way past (lowest at
    0.167 s). He comes up with a hop, both feet off the ground."""
    tip = 62.0
    swoop = _pose(sq=0.94, pitch=tip, ty=0.030, tz=-0.085, lean=10.0, twist=14.0, nod=-40.0, look=-6.0,
                  legL=(tip - 4.0, 2.0), legR=(-30.0, 10.0),
                  armR=_street(tip, (0.20, -0.90, 0.38), (0.10, -0.97, 0.22)), reachR=0.95,
                  armL=_street(tip, (0.60, 0.60, -0.52), (0.50, 0.76, -0.42)), reachL=0.30)
    dip = _pose(sq=0.90, pitch=14.0, lean=8.0, nod=6.0, legL=(8.0, 4.0), legR=(-8.0, 5.0),
                armR=((0.45, -0.75, 0.48), (0.25, -0.70, 0.67)), armL=((0.70, -0.50, -0.50), (0.60, -0.50, -0.62)))
    up = _pose(sq=1.12, ty=0.040, pitch=-4.0, lean=-6.0, nod=-8.0, legL=(-10.0, 9.0), legR=(14.0, 9.0),
               armR=((0.60, -0.50, 0.62), (0.30, 0.30, 0.90)), armL=((0.80, -0.40, -0.45), (0.70, -0.60, -0.40)))
    return _act(1.0 / 3.0, [(0.0, STAND), (0.06, dip, "out"), (1.0 / 6.0, swoop, "in"), (0.27, up, "out"),
                            (1.0 / 3.0, STAND, "in")])


def attack_melee_right():
    """THE TAG. Everybody he tags is bigger than he is, so he tags UP: a quick sink with the
    elbow back (0.05 s), then he goes up on his toes and his right arm shoots out and up at
    shoulder height on somebody twice his size, one long line from his back heel to his fist,
    the left arm thrown straight back the other way; out by 0.12 s and still stretching to its
    far point at 0.254 s. He drops back onto his heels with a bounce. The game poses his chest,
    head, upper arms, root and legs over this while the tag is live, so the legs are mild and
    the forearms never fold past a right angle; the sink, the stretch and the bounce are his."""
    wind = _pose(sq=0.86, lean=4.0, twist=-18.0, nod=6.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
                 armR=((0.60, -0.55, -0.58), (0.40, -0.10, 0.60)), armL=((0.50, -0.60, 0.62), (0.30, -0.10, 0.95)))
    back = ((0.42, -0.40, -0.81), (0.34, -0.46, -0.82))
    out = _pose(sq=1.08, ty=0.008, lean=10.0, twist=20.0, nod=-10.0, legL=(6.0, 4.0), legR=(-6.0, 4.0),
                armR=((0.20, 0.32, 0.93), (0.10, 0.36, 0.93)), reachR=0.35, armL=back, reachL=0.15)
    far = _pose(out, sq=1.15, ty=0.016, tz=0.014, lean=15.0, twist=28.0, nod=-16.0, legL=(9.0, 4.0), legR=(-9.0, 5.0),
                armR=((0.14, 0.38, 0.92), (0.06, 0.42, 0.91)), reachR=0.95, reachL=0.45)
    held = _pose(far, sq=1.06, ty=0.008, lean=12.0, twist=24.0, reachR=0.65, reachL=0.25)
    settle = _pose(sq=0.90, lean=2.0, nod=5.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
                   armR=((0.55, -0.70, 0.45), (0.30, -0.30, 0.90)))
    return _act(0.4167, [(0.0, STAND), (0.05, wind, "out"), (0.12, out, "in"), (0.254, far, "out"),
                         (0.30, held), (0.37, settle, "in"), (0.4167, STAND, "out")])


def attack_melee_left():
    """THE SHOVE. He weighs nothing, so he does not push: he THROWS himself at you. He turns
    side on and coils, his left elbow up in front of his face like a shield, the right fist
    tucked; then he leaves the ground and goes in shoulder and elbow first, the whole of him
    one tilted lump with his heels trailing (0.254 s); bounces off; and lands back where he
    started with a squash."""
    shield = ((0.90, -0.10, 0.42), (0.34, 0.92, 0.20))
    tucked = ((0.60, -0.70, -0.38), (0.10, 0.10, 0.99))
    coil = _pose(sq=0.82, yaw=-38.0, roll=8.0, lean=10.0, nod=10.0, look=34.0, legL=(0.0, 8.0), legR=(0.0, 8.0),
                 armL=((0.70, -0.55, 0.45), (0.30, 0.80, 0.52)), armR=tucked)
    barge = _pose(sq=1.12, yaw=-62.0, roll=-26.0, ty=0.050, tz=0.060, lean=4.0, nod=14.0, look=40.0, tilt=10.0,
                  legL=(0.0, -4.0), legR=(0.0, 30.0),
                  armL=shield, reachL=0.30, armR=((0.80, -0.50, -0.34), (0.72, -0.58, -0.38)), reachR=0.15)
    off = _pose(barge, sq=0.96, yaw=-52.0, roll=-10.0, ty=0.026, tz=0.034, legL=(0.0, 6.0), legR=(0.0, 18.0), reachL=0.10)
    land = _pose(sq=0.88, yaw=-14.0, lean=4.0, nod=6.0, legL=(0.0, 8.0), legR=(0.0, 8.0))
    return _act(0.4167, [(0.0, STAND), (0.09, coil, "out"), (0.16, coil, "lin"), (0.254, barge, "in"),
                         (0.31, off, "out"), (0.37, land, "in"), (0.4167, STAND, "out")])


def interact_right():
    """"Akin na!" Gimme. He drops into a squat, feet wide, and his right fist darts out low at
    knee height (0.18 s) while his left arm sticks out to the side to keep him up; he snatches
    at it twice more, quick as a chicken pecking, and pops back up."""
    wide = ((0.96, 0.10, -0.10), (0.94, 0.26, -0.05))
    snatch = _pose(sq=0.76, lean=24.0, twist=14.0, nod=-12.0, legL=(0.0, 22.0), legR=(0.0, 22.0),
                   armR=((0.22, -0.52, 0.82), (0.10, -0.44, 0.89)), reachR=0.60, armL=wide, reachL=0.20)
    drawn = _pose(snatch, sq=0.80, lean=18.0, twist=6.0, armR=((0.50, -0.60, 0.50), (0.20, -0.20, 0.96)), reachR=0.0)
    again = _pose(snatch, sq=0.74, lean=22.0, twist=12.0, reachR=0.50)
    up = _pose(sq=1.08, ty=0.020, lean=-4.0, nod=-4.0, legL=(0.0, 8.0), legR=(0.0, 8.0),
               armR=((0.50, -0.80, 0.30), (0.30, -0.30, 0.90)))
    gather = _pose(sq=1.04, lean=-4.0, nod=2.0, armR=((0.55, -0.70, -0.20), (0.20, -0.10, 0.97)))
    return _act(2.0 / 3.0, [(0.0, STAND), (0.07, gather, "out"), (0.18, snatch, "in"), (0.25, drawn, "out"),
                            (0.32, again, "in"), (0.40, again, "out"), (0.54, up), (2.0 / 3.0, STAND)])


def interact_left():
    """"HOY!" He fills up (a breath, leaning back, chest out), then throws himself forward over
    his toes with his left fist at his mouth for a trumpet and his right arm flung out behind
    him, and lets the whole street have it (0.18 s); the shout shakes him twice; and he rocks
    back upright."""
    trumpet = ((0.42, -0.10, 0.90), (-0.70, 0.58, 0.40))
    behind = ((0.60, -0.22, -0.77), (0.52, -0.16, -0.84))
    fill = _pose(sq=1.07, lean=-12.0, nod=-10.0, legL=(0.0, 4.0), legR=(0.0, 4.0),
                 armL=((0.60, -0.70, 0.38), (-0.10, 0.50, 0.86)), armR=((0.60, -0.76, -0.25), (0.50, -0.80, -0.30)))
    shout = _pose(sq=0.94, pitch=24.0, tz=-0.020, lean=22.0, nod=-44.0, legL=(24.0, 5.0), legR=(24.0, 5.0),
                  armL=_street(24.0, *trumpet), reachL=0.50, armR=_street(24.0, *behind), reachR=0.60)
    shake = _pose(shout, sq=1.0, pitch=20.0, lean=18.0, nod=-50.0, reachR=0.40)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.09, fill, "out"), (0.18, shout, "in"), (0.25, shake, "out"),
                            (0.32, shout, "in"), (0.40, shake, "out"), (0.52, _pose(sq=1.03, lean=-5.0, nod=-4.0)),
                            (2.0 / 3.0, STAND)])


#   on his belly: both arms thrown up past his ears (which is AHEAD of him, lying down)
ARM_SUPER = ((0.62, 0.74, 0.25), (0.34, 0.92, 0.18))


def slide():
    """A belly flop, and he loves it. A quick sink with both arms back; he DIVES, both arms
    out ahead of him, and lands flat on his front with his chin up and his heels in the air
    (down at 0.25 s); his right arm stretches out for the slipper (0.342 s) while his legs
    kick like a swimmer's through the skid; then he bounces up off his fists straight into a
    hop and lands standing."""
    back = ((0.62, -0.50, -0.60), (0.50, -0.40, -0.77))
    sink = _pose(sq=0.82, lean=16.0, nod=-6.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armL=back, armR=back)
    dive = _pose(sq=1.16, pitch=42.0, ty=0.050, tz=-0.130, nod=-30.0, legL=(-14.0, 6.0), legR=(-14.0, 6.0),
                 armL=ARM_SUPER, armR=ARM_SUPER, reachL=0.40, reachR=0.40)
    flat = _pose(sq=0.86, pitch=84.0, ty=0.095, tz=-0.250, nod=-58.0, legL=(-58.0, 14.0), legR=(-30.0, 14.0),
                 armL=ARM_SUPER, armR=ARM_SUPER, reachL=0.30, reachR=0.40)
    reach = _pose(flat, sq=1.08, pitch=80.0, ty=0.105, tz=-0.185, nod=-66.0, legL=(-24.0, 12.0), legR=(-62.0, 12.0),
                  reachL=0.30, reachR=0.90)
    skid = _pose(reach, sq=1.0, pitch=82.0, ty=0.100, tz=-0.200, legL=(-60.0, 14.0), legR=(-26.0, 14.0), reachR=0.50)
    push = _pose(sq=0.86, pitch=30.0, ty=0.015, tz=-0.080, lean=16.0, nod=-12.0, legL=(-6.0, 9.0), legR=(-6.0, 9.0),
                 armL=((0.55, -0.62, 0.55), (0.30, -0.90, 0.30)), armR=((0.55, -0.62, 0.55), (0.30, -0.90, 0.30)))
    hop = _pose(sq=1.14, ty=0.050, pitch=-5.0, lean=-5.0, nod=-8.0, legL=(-12.0, 12.0), legR=(-12.0, 12.0),
                armL=((0.80, -0.30, 0.30), (0.70, 0.30, 0.50)), armR=((0.80, -0.30, 0.30), (0.70, 0.30, 0.50)))
    land = _pose(sq=0.90, lean=3.0, nod=4.0, legL=(0.0, 7.0), legR=(0.0, 7.0))
    return _act(0.95, [(0.0, STAND), (0.07, sink, "out"), (0.14, dive, "in"), (0.25, flat, "in"), (0.342, reach, "out"),
                       (0.52, skid), (0.66, push), (0.78, hop, "out"), (0.88, land, "in"), (0.95, STAND, "out")])


CROUCH_LENGTH = 1.5
ARM_DANGLE = ((0.36, -0.90, 0.22), (0.22, -0.96, 0.16))
CROUCH = _pose(sq=0.90, lean=40.0, pitch=16.0, tz=-0.020, nod=20.0, legL=(16.0, 14.0), legR=(16.0, 14.0),
               armL=_street(16.0, *ARM_DANGLE), armR=_street(16.0, *ARM_DANGLE), reachL=0.45, reachR=0.45)


def crouch():
    """OUT OF BREATH, a 1.5 s loop, and for once he has stopped. He is folded in half with his
    feet apart, his head hanging and both arms dangling to the ground like washing on a line,
    swinging. Three pants, quick, the way a small boy gets his breath back: each one jerks his
    back up and drops it, and sets his arms swinging the other way; on the third he throws his
    head up for a gulp of air and lets it fall again. The first and the last frame are the
    full folded pose: the game loops this while he is winded and also holds its last frame."""
    def pose_at(t):
        p = (3.0 * t / CROUCH_LENGTH) % 1.0
        if t >= CROUCH_LENGTH - 1e-6:
            p = 0.0
        heave = _ease(p / 0.30) if p < 0.30 else 1.0 - _ease((p - 0.30) / 0.70)
        gulp = _bump(t, 1.12, 0.10)
        swing = math.sin(2.0 * math.pi * t / CROUCH_LENGTH * 3.0) * (1.0 - _bump(t, 0.0, 0.08) - _bump(t, CROUCH_LENGTH, 0.08))
        left = _street(16.0, (0.36 + 0.26 * swing, -0.90, 0.22), (0.22 + 0.40 * swing, -0.96, 0.16))
        right = _street(16.0, (0.36 - 0.26 * swing, -0.90, 0.22), (0.22 - 0.40 * swing, -0.96, 0.16))
        return _pose(CROUCH, sq=0.90 + 0.06 * heave + 0.03 * gulp, lean=40.0 - 9.0 * heave - 3.0 * gulp,
                     nod=20.0 - 8.0 * heave - 34.0 * gulp, hunch=2.5 * swing, tilt=5.0 * swing,
                     armL=left, armR=right, reachL=0.45 - 0.12 * heave, reachR=0.45 - 0.12 * heave)
    return _frames(CROUCH_LENGTH, pose_at)


def sit():
    """0.8 s of getting down, ending seated. He does not sit down, he JUMPS down: a hop into the
    air with his legs shot out in front of him, and he lands on his bottom with a squash
    (0.36 s), bounces once, and grabs his own feet. He ends with his legs out in a wide V,
    leaning forward between them with a fist on each foot, chin up, looking at you."""
    toes = ((0.52, -0.46, 0.72), (0.50, -0.30, 0.81))
    seat = _pose(ty=-0.100, lean=20.0, nod=-16.0, legL=(84.0, 26.0), legR=(84.0, 26.0),
                 armL=toes, armR=toes, reachL=0.60, reachR=0.60)
    dip = _pose(sq=0.84, lean=8.0, nod=4.0, legL=(0.0, 7.0), legR=(0.0, 7.0))
    air = _pose(sq=1.14, ty=0.060, pitch=-10.0, lean=4.0, nod=-6.0, legL=(60.0, 16.0), legR=(60.0, 16.0),
                armL=ARM_UP, armR=ARM_UP, reachL=0.60, reachR=0.60)
    land = _pose(seat, sq=0.78, lean=10.0, nod=10.0, legL=(92.0, 30.0), legR=(92.0, 30.0),
                 armL=((0.90, -0.10, 0.20), (0.80, 0.40, 0.40)), armR=((0.90, -0.10, 0.20), (0.80, 0.40, 0.40)),
                 reachL=0.2, reachR=0.2)
    bounce = _pose(seat, sq=1.08, ty=-0.080, lean=4.0, nod=-8.0, legL=(74.0, 24.0), legR=(78.0, 24.0),
                   armL=((0.70, -0.30, 0.60), (0.60, -0.10, 0.80)), armR=((0.70, -0.30, 0.60), (0.60, -0.10, 0.80)),
                   reachL=0.3, reachR=0.3)
    grab = _pose(seat, sq=0.96, lean=26.0, nod=-20.0)
    return _act(0.8, [(0.0, STAND), (0.10, dip, "out"), (0.24, air, "out"), (0.36, land, "in"), (0.47, bounce, "out"),
                      (0.62, grab, "in"), (0.8, seat)])


def die():
    """KNOCKED DOWN, and nothing about it hurts. His feet go out from under him and he is in
    the air, arms and legs everywhere; he comes down FACE FIRST with a splat (0.22 s); his
    legs fly up behind him; one bounce; and he ends with his face in the dirt, turned to one
    side, his arms flat back along his sides and both legs stuck up in the air in a V."""
    flail = ((0.80, 0.50, 0.30), (0.50, 0.80, 0.30))
    along = ((0.70, -0.70, -0.10), (0.62, -0.76, -0.18))
    hit = _pose(sq=1.14, ty=0.070, pitch=-14.0, lean=-10.0, nod=-18.0, legL=(34.0, 14.0), legR=(20.0, 14.0),
                armL=flail, armR=flail, reachL=0.50, reachR=0.50)
    over = _pose(sq=1.06, ty=0.130, tz=-0.090, pitch=52.0, nod=-20.0, legL=(-20.0, 16.0), legR=(10.0, 16.0),
                 armL=ARM_UP, armR=ARM_UP, reachL=0.50, reachR=0.50)
    splat = _pose(sq=0.80, ty=0.085, tz=-0.250, pitch=96.0, nod=-24.0, look=20.0, legL=(-20.0, 20.0), legR=(-30.0, 20.0),
                  armL=((0.96, 0.10, 0.20), (0.90, 0.30, 0.20)), armR=((0.96, 0.10, 0.20), (0.90, 0.30, 0.20)),
                  reachL=0.30, reachR=0.30)
    bounce = _pose(splat, sq=1.07, ty=0.115, pitch=100.0, nod=-30.0, look=30.0, legL=(-110.0, 26.0), legR=(-80.0, 26.0),
                   armL=along, armR=along, reachL=0.10, reachR=0.10)
    rest = _pose(sq=0.97, ty=0.090, tz=-0.250, pitch=97.0, nod=-26.0, look=38.0, tilt=0.0,
                 legL=(-78.0, 24.0), legR=(-98.0, 20.0), armL=along, armR=along)
    return _act(1.0 / 3.0, [(0.0, STAND), (0.05, hit, "out"), (0.14, over, "lin"), (0.22, splat, "in"),
                            (0.27, bounce, "out"), (1.0 / 3.0, rest, "in")])


#   the fist punched at the sky, and the same fist yanked down beside his ear
ARM_SKY = ((0.80, 0.52, 0.14), (0.44, 0.89, 0.08))
ARM_YANK = ((0.74, -0.60, 0.24), (0.34, 0.84, 0.42))
ARM_ELBOW = ((0.62, -0.50, -0.60), (0.26, -0.05, 0.96))       # the other arm: elbow driven back, fist at his ribs


def emote_yes():
    """YES! A 1.0 s loop of a boy who has just been picked first. He drops, and JUMPS: right
    fist punched straight up at the sky, left elbow yanked down and back, knees up, hanging
    there with his chin up; lands and yanks the fist down beside his ear ("yes!"); and goes
    straight up again, higher, with a twist; lands, yanks, and lets go. He is in the air or
    pulling the fist for most of the second."""
    dip = _pose(sq=0.80, lean=8.0, nod=8.0, legL=(0.0, 8.0), legR=(0.0, 8.0), armR=ARM_YANK, armL=ARM_ELBOW)
    up = _pose(sq=1.16, ty=0.075, lean=-8.0, twist=-8.0, nod=-18.0, tilt=-14.0, roll=7.0, legL=(34.0, 12.0), legR=(-22.0, 12.0),
               armR=ARM_SKY, reachR=1.10, armL=ARM_ELBOW)
    hang = _pose(up, sq=1.06, ty=0.085, legL=(40.0, 14.0), legR=(-26.0, 14.0), reachR=1.0)
    land = _pose(sq=0.80, lean=10.0, nod=14.0, legL=(0.0, 10.0), legR=(0.0, 10.0), armR=ARM_YANK, armL=ARM_ELBOW)
    up2 = _pose(up, ty=0.090, sq=1.18, yaw=-16.0, twist=-14.0, legL=(-22.0, 14.0), legR=(36.0, 14.0), reachR=1.15)
    hang2 = _pose(up2, sq=1.06, ty=0.100, yaw=-20.0, reachR=1.0)
    land2 = _pose(land, sq=0.78, lean=12.0, nod=16.0)
    proud = _pose(land, sq=1.02, lean=-3.0, nod=-4.0, legL=(0.0, 5.0), legR=(0.0, 5.0))
    return _act(1.0, [(0.0, STAND), (0.09, dip, "out"), (0.19, up, "out"), (0.30, hang), (0.39, land, "in"),
                      (0.50, up2, "out"), (0.62, hang2), (0.71, land2, "in"), (0.82, proud, "out"), (1.0, STAND)])


#   a fist clamped over each ear, the elbows stuck out wide
ARM_EARS = ((0.98, 0.14, 0.12), (0.04, 0.98, 0.16))
NO_LENGTH = 1.2


def emote_no():
    """"AYAW!" A 1.2 s loop: he clamps a fist over each ear, elbows stuck out wide, screws
    himself down, and shakes his whole body no, not just his head: chest one way and head the
    other, four times, stamping a foot with each, sinking lower as he goes. Then he lets go
    and pops back up. He is not listening and wants the whole street to see him not listening."""
    def pose_at(t):
        on = _window(t, 0.0, NO_LENGTH, 0.16)
        shake = math.sin(2.0 * math.pi * (t - 0.16) / 0.44) * _window(t, 0.14, 1.06, 0.12)
        stamp = abs(shake) ** 0.7
        sink = _window(t, 0.10, 1.10, 0.40)
        ears = _blend(ARMS_HANG, ARM_EARS, on)
        return _pose(sq=1.0 - 0.07 * on - 0.05 * sink + 0.04 * stamp * on, ty=0.010 * stamp * on,
                     lean=7.0 * on, twist=28.0 * shake, yaw=22.0 * shake, roll=3.0 * shake,
                     nod=10.0 * on, look=-30.0 * shake, tilt=-6.0 * shake,
                     legL=(20.0 * max(shake, 0.0), 3.0 + 7.0 * on + 8.0 * max(shake, 0.0)),
                     legR=(20.0 * max(-shake, 0.0), 3.0 + 7.0 * on + 8.0 * max(-shake, 0.0)),
                     armL=ears, armR=ears, reachL=0.75 * on, reachR=0.75 * on)
    return _frames(NO_LENGTH, pose_at)


CLIPS = {
    "idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
    "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
    "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
    "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
    "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no,
}
