"""New locomotion clips for the Nemu redesign PROTOTYPE.

Imported by tools/author_character_redesign_nemu.py, which writes them into nemu-redesign.glb IN
PLACE OF the clips of the same name copied from team-nemu.glb. Nothing in the game reads them;
team-nemu.glb keeps its own.

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9: each redesigned hero gets a new `idle`,
`walk`, `sprint`, `jump` and `fall`, and each hero's motion is its OWN (AGENTS.md: copied cast-wide looks
are not allowed). The owner on Nemu, 2026-09-27: "keep nemu js improve her animations".

WHAT HER MOTION HAS TO SAY. She is the sleepy ghost. Three things, and every number below is
tuned to one of them:
  1. SHE DRIFTS, SHE DOES NOT STEP. No hard contact, no snap. Dante is thrown up between steps
     and squashes on landing; she rises and sinks on a smooth curve a third as high, her feet
     shuffle under the hem in small arcs, and she rocks a little from side to side like
     something carried on water. In `sprint` she does not pump: she leans far forward, her legs
     flutter fast and small, and she GLIDES, almost level, sleeves out like wings.
  2. HER WEIGHT IS IN HER SLEEVES, AND THEY ARE LATE. The bells are a third of her width each.
     They never lead a motion. In `walk` they sway a beat behind the legs. In `sprint` both are
     held out and a little back like a glider's wings and only flutter. In `jump` they are
     thrown UP in a V from the first frame and are slow to sag back. In `fall` they are held up
     in that V, a parachute, and she rocks under them like a falling leaf.
  3. SHE IS HALF ASLEEP. Her head hangs forward and lolls a little after each step, then is
     picked back up. Because her cowl is the head's from its underside up (the model script),
     her face stays hidden to the same line however the head hangs.

LIMITS OF HER BODY, found on the model and kept here:
  * A straight arm never rises above straight out; arms go up through the elbow (see `jump`).
    In `walk` they hang at 50 degrees, not the 80 the game's own clip uses: at 80 a bell sleeve
    this size is inside the torso to its middle. They also never swing FORWARD past a few degrees: her two side locks hang in front
    of the shoulders, and a sleeve brought forward goes through them. So her arms sway out and
    back, never across.
  * The elbows fold a little and slowly. A sleeve this big folded sharply is two boxes crossed.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK and
tips a head or torso FORWARD; about +z a negative angle drops her left arm from straight out.

Each clip keeps the LENGTH of the one it replaces (walk 0.72 s, sprint 0.48, jump 0.50,
fall 0.333) but `idle`, which is acted and runs 8 s (see `idle`). NOT YET SEEN IN UNITY: the game layers procedural motion on these bones and the
`root` scale channel is new.
"""
import math

RATE = 60.0
SPLAY = 18.0   # degrees a folded forearm is turned out from straight ahead


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


def fold(side, bend):
    """A forearm folded `bend` degrees forward, turned OUT from the body as it folds.

    `side` is +1 for her left. ⚠️ The turn-out grows WITH the fold and is nothing at no fold.
    A fixed turn-out (v02) twisted both bells 18 degrees about the arm even with the elbow
    straight, so frame 0 of `idle` was not the neutral stand and her outline moved.
    """
    out = SPLAY * min(1.0, abs(bend) / 12.0)
    return mul(qx(-out), qy(-side * bend))


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


# ---------------------------------------------------------------------------
# THE IDLE IS ACTED. Helpers first: an arm is posed by where it POINTS, stances are blended in
# and out through eased windows.
# ---------------------------------------------------------------------------

def _norm(v):
    n = math.sqrt(sum(k * k for k in v)) or 1.0
    return tuple(k / n for k in v)


def _rot(q, v):
    """`v` turned by quaternion `q`."""
    x, y, z, w = q
    vx, vy, vz = v
    tx, ty, tz = 2.0 * (y * vz - z * vy), 2.0 * (z * vx - x * vz), 2.0 * (x * vy - y * vx)
    return (vx + w * tx + (y * tz - z * ty), vy + w * ty + (z * tx - x * tz), vz + w * tz + (x * ty - y * tx))


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
    """An arm posed by where it POINTS: the upper sleeve along `upper`, the bell along `fore`.

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


def _window(t, start, end, blend_in, blend_out=None):
    """0 outside [start, end], eased up over `blend_in` and down over `blend_out`."""
    blend_out = blend_in if blend_out is None else blend_out
    if not start <= t <= end:
        return 0.0
    return _ease((t - start) / blend_in) * (1.0 - _ease((t - (end - blend_out)) / blend_out))


def _dir(down, back=0.0):
    """Her left arm pointing `down` degrees below straight out, swung `back` degrees behind her."""
    d, b = math.radians(down), math.radians(back)
    return (math.cos(d) * math.cos(b), -math.sin(d), -math.cos(d) * math.sin(b))


#   where her LEFT sleeve points in each stance: (upper sleeve, bell). The right is the mirror.
#   ⚠️ EVERY STANCE KEEPS THE BELLS OUT AT HER SIDES. Fists on the hips and folded arms, the
#   stances Dante takes, are not hers: a bell 200 mm deep brought to her waist, across her
#   chest or behind her back is inside the hoodie (and, in front, through both side locks).
#   So what she does while she waits is what a shy, sleepy kid whose sleeves hide her hands
#   CAN do, and all of it is done WITH the sleeves.
ARMS_HANG = (_dir(45.0), _dir(45.0))                 # the neutral stand: the old clip's 45 degrees
ARMS_SLACK = (_dir(55.0), _dir(61.0))                # asleep on her feet: everything let go
ARMS_START = (_dir(30.0), _dir(22.0))                # the jolt awake: both bells flick out
ARMS_YAWN = (_dir(-27.0), _dir(-70.0))               # the stretch: `jump`'s V, checked there
ARMS_SWISH = (_dir(33.0), _dir(25.0))                # the bells swung out as she twists
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of the sleepy ghost waiting. Three things she does, in order:

      1. SHE NODS OFF. Her head sinks, catching itself and sinking further, her shoulders and
         sleeves go slack and she sags to one side. Then she JOLTS awake: head up, both bells
         flick out, a small hop of the whole body. She looks left and right to see if anyone saw.
      2. SHE YAWNS AND STRETCHES. Both sleeves go up in a V beside her head (the same V as
         `jump`, the only way her arms go up), she tips back and grows a little taller, holds
         it, and lets it all drop.
      3. SHE SWISHES HER SLEEVES. A kid in sleeves too long for her twists at the waist, one
         way and the other, to make them swing; the bells lift out as she turns and her head
         follows a beat late.

    ⚠️ THREE TRIES AT THIS CLIP. The one it replaces holds four bones still. The first new one
    layered sines on every bone; the owner, 2026-10-05, on Dante's version of that: "the idle
    looks like he's just distorting around.. needs more character, like sometimes he puts his
    hands on the waist, or crosses his arms, other things idle people do". So it is ACTED:
    stances held long enough to read, breathing kept small underneath.
    ⚠️ HER FACE STAYS COVERED THROUGH ALL OF IT. The cowl is the head's (the model script), so
    when her head drops 26 degrees the cowl drops with it and the same strip of face shows.
    ⚠️ FRAME 0 IS EXACTLY THE NEUTRAL STAND, so stills and silhouettes taken there are the old
    clip's. IT IS 8 s, NOT THE OLD 1.33 s: anything in the game that assumes the idle's length
    needs a look, and the game may prefer these as separate clips it picks between.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / 2.0)          # slow: a breath every two seconds
        # 1. nodding off: a slow sink from 0.5 s, gone in a fifth of a second at 2.75
        doze = _window(t, 0.5, 2.95, 1.9, 0.22)
        bob = doze * 0.5 * (1.0 - math.cos(2.0 * math.pi * (t - 0.5) / 0.75))   # the head catching itself
        jolt = math.exp(-((t - 2.90) / 0.11) ** 2)
        peek = 20.0 * (_window(t, 3.05, 3.50, 0.18) - _window(t, 3.45, 3.95, 0.18))
        # 2. the yawn
        yawn = _window(t, 4.05, 5.75, 0.50, 0.40)
        # 3. the sleeve swish: two twists each way
        swish = _window(t, 6.0, 7.85, 0.35, 0.40)
        twist = 17.0 * swish * math.sin(2.0 * math.pi * (t - 6.0) / 0.925)
        late = 17.0 * swish * math.sin(2.0 * math.pi * (t - 6.16) / 0.925)
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            poses = [_arm(side, *pose) for pose in (ARMS_HANG, ARMS_SLACK, ARMS_START, ARMS_YAWN, ARMS_SWISH)]
            # the bell on the side she is turning AWAY from swings out further
            out_swing = swish * (0.55 + 0.45 * side * twist / 17.0)
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(poses[0][k], poses[1][k], doze)
                q = _mix(q, poses[2][k], jolt)
                q = _mix(q, poses[3][k], yawn)
                q = _mix(q, poses[4][k], out_swing)
                row[(bone, "rotation")] = q
            row[("forearm-" + name, "scale")] = (1.0 + 0.10 * yawn, 1.0, 1.0)
        row.update({
            ("root", "scale"): _squash(1.0 + 0.012 * breath - 0.035 * doze + 0.045 * jolt + 0.050 * yawn),
            ("root", "rotation"): mul(qx(-3.0 * yawn), qz(2.0 * doze)),
            ("torso", "rotation"): mul(mul(qy(twist), qx(5.0 * doze - 6.0 * yawn + 0.8 * breath)), qz(-1.5 * doze)),
            ("head", "rotation"): mul(mul(qy(peek + 0.55 * late - 0.75 * twist),
                                          qx(19.0 * doze + 7.0 * bob - 9.0 * jolt - 15.0 * yawn + 5.0 * swish - 0.8 * breath)),
                                      qz(6.0 * doze)),
            # the feet stay planted while she sags to one side
            ("leg-left", "rotation"): mul(qz(-2.0 * doze), qx(3.0 * yawn)),
            ("leg-right", "rotation"): mul(qz(-2.0 * doze), qx(3.0 * yawn)),
        })
        _add(out, t, row)
    return out


SLEEVE_LAG = 1.15   # radians her sleeves run behind her legs: about a fifth of a stride


def walk():
    """A drowsy shuffle. Small steps, a slow float, a side to side rock, the head lolling."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        late = math.sin(phase - SLEEVE_LAG)
        rise = 0.5 - 0.5 * math.cos(2.0 * phase)        # 0 as the feet pass, 1 at full stride, no corner in it
        _add(out, t, {
            ("root", "translation"): (0.0, 0.002 + 0.015 * rise, 0.0),
            ("root", "rotation"): mul(qx(2.0), qz(2.6 * s)),
            ("root", "scale"): _squash(0.985 + 0.035 * rise),
            ("leg-left", "rotation"): mul(qx(-25.0 * s), qz(2.0)),
            ("leg-right", "rotation"): mul(qx(25.0 * s), qz(-2.0)),
            ("torso", "rotation"): mul(qx(3.0), mul(qy(4.0 * s), qz(-1.6 * s))),
            # the head hangs, drops a little further just after each foot lands, and rolls with the rock
            ("head", "rotation"): mul(qx(7.0 + 4.0 * math.sin(2.0 * phase - 1.4)), mul(qy(-2.4 * s), qz(-3.2 * late))),
            # the sleeves: out and back, late, never forward of the body
            ("arm-left", "rotation"): mul(qx(4.0 + 8.0 * late), qz(-50.0 - 4.0 * late)),
            ("arm-right", "rotation"): mul(qx(4.0 - 8.0 * late), qz(50.0 - 4.0 * late)),
            ("forearm-left", "rotation"): fold(1, 10.0 + 7.0 * math.sin(phase - 2.0 * SLEEVE_LAG)),
            ("forearm-right", "rotation"): fold(-1, 10.0 - 7.0 * math.sin(phase - 2.0 * SLEEVE_LAG)),
        })
    return out


def sprint():
    """A glide. Leaning far forward, legs fluttering small and fast, both sleeves streaming behind."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        rise = 0.5 - 0.5 * math.cos(2.0 * phase)
        flutter_l = math.sin(2.0 * phase - 0.6)
        flutter_r = math.sin(2.0 * phase - 2.1)         # the two sleeves do not flap together
        _add(out, t, {
            ("root", "translation"): (0.0, 0.010 + 0.007 * rise, 0.0),
            ("root", "rotation"): qx(13.0),
            ("root", "scale"): _squash(1.035 + 0.012 * rise),
            ("leg-left", "rotation"): mul(qx(-6.0 - 32.0 * s), qz(2.0)),
            ("leg-right", "rotation"): mul(qx(-6.0 + 32.0 * s), qz(-2.0)),
            ("torso", "rotation"): mul(qx(10.0), qy(5.0 * s)),
            ("head", "rotation"): mul(qx(-17.0 + 1.5 * math.sin(2.0 * phase)), qy(-3.0 * s)),
            # ⚠️ HELD OUT LIKE WINGS AND ONLY A LITTLE BACK. Swung back about x (v03) the bell turned
            # its open mouth to the side and she ran carrying two boxes. Swept 46 degrees back
            # about the upright axis (v05) the bells trailed well, but a bell is 200 mm deep and
            # its inner half went into her back. At 16 degrees of sweep the sleeve's inner edge
            # stays where it is at rest; the glide is carried by the lean and the flutter.
            ("arm-left", "rotation"): mul(qy(16.0 + 5.0 * flutter_l), qz(-25.0 + 4.0 * flutter_l)),
            ("arm-right", "rotation"): mul(qy(-16.0 - 5.0 * flutter_r), qz(25.0 - 4.0 * flutter_r)),
            ("forearm-left", "rotation"): fold(1, 4.0 + 4.0 * math.sin(2.0 * phase - 1.5)),
            ("forearm-right", "rotation"): fold(-1, 4.0 + 4.0 * math.sin(2.0 * phase - 3.0)),
        })
    return out


# ⚠️ HER UPPER ARM LIFTS 27 DEGREES, NOT DANTE'S 12. At 11 (v03) only the short bell turned up at
# the end of a level sleeve: from the front it was a tray held out to each side with a hand
# standing in it, which is the shrug the owner turned down on Dante. Dante cannot lift further
# because his collar is in the way. She can: her sleeve's shoulder goes up BEHIND her side lock
# and into the side of the cowl, violet behind violet, and the second and third steps of the
# bell come out past the hair (it is 0.146 wide) and stand beside her cheeks.
ARM_LIFT = 27.0


def jump():
    """Lifted, not launched, with both sleeves thrown UP. She goes up a little long, her feet
    trail back together like a ghost's tail, and the bells stand up beside her head in a V.

    ARMS UP GO THROUGH THE ELBOW (docs/CHARACTER_REDESIGN_DANTE.md rule 8 as amended; owner on
    Dante: "the arms dont really get much higher than the original A posing.. the jump looks
    like he's shrugging"). Her head and hair are far wider than her shoulders, so a straight arm
    cannot rise. The upper arm lifts `ARM_LIFT`, the forearm (the bell and the hand in it) folds
    UP and out past her side locks, and it stretches as it is thrown. The V is there from the
    FIRST frame. What is late, as everything about her sleeves is late, is the settle: the bells
    overshoot, then sag back a little and hang open while she floats.
    """
    out = {}
    for t in _times(0.50):
        gone = 1.0 - math.exp(-t / 0.10)                # the body: quick
        sag = math.exp(-t / 0.24)                       # the sleeves: slow to give the height back
        ring = math.cos(2.0 * math.pi * t / 0.50) * math.exp(-t / 0.16)
        lift = ARM_LIFT * (0.80 + 0.20 * sag)
        # ⚠️ lift and fold together stay under about 72 degrees: past that the bell's inner face
        # is in the side lock, and it is 200 mm deep, so it would cut the whole lock
        # (36, not 30, since her head came in to 0.84: the lock the bell must stay outside of
        # is 23 mm nearer the middle, so the bell stands a little higher)
        fore = 36.0 + 9.0 * sag + 4.0 * math.sin(2.0 * math.pi * t / 0.34) * (1.0 - sag)
        # a small stretch only: the hand is in the bell and stretches with it, and at 1.3 it
        # stood out of the cuff as one long finger
        reach = 1.0 + 0.13 * math.exp(-t / 0.15)
        _add(out, t, {
            ("root", "scale"): _squash(1.0 + 0.10 * ring),
            ("root", "rotation"): qx(0.0),
            # ⚠️ A STANDING JUMP: both feet together, the same angle (owner, 2026-10-05: "is the
            # jump supposed to be one foot forward? i need a jump for standing still")
            ("leg-left", "rotation"): mul(qx(24.0 * gone), qz(1.5)),
            ("leg-right", "rotation"): mul(qx(24.0 * gone), qz(-1.5)),
            ("torso", "rotation"): qx(-5.0 * math.exp(-t / 0.12) + 4.0 * gone),
            ("head", "rotation"): qx(-8.0 * math.exp(-t / 0.14) + 5.0 * gone),
            ("arm-left", "rotation"): qz(lift),
            ("arm-right", "rotation"): qz(-lift),
            ("forearm-left", "rotation"): qz(fore),
            ("forearm-right", "rotation"): qz(-fore),
            ("forearm-left", "scale"): (reach, 1.0, 1.0),
            ("forearm-right", "scale"): (reach, 1.0, 1.0),
        })
    return out


def fall():
    """A loop. Both sleeves held up in the jump's V, a parachute, and the whole of her rocking
    under them like a falling leaf: the bell on the side she rocks away from rides higher. Feet
    hanging, eyes on the ground."""
    out = {}
    length = 1.0 / 3.0
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        _add(out, t, {
            ("root", "scale"): _squash(1.04 + 0.008 * math.sin(2.0 * phase)),
            ("root", "rotation"): qz(3.5 * s),
            ("leg-left", "rotation"): mul(qx(9.0 + 5.0 * c), qz(4.0 - 3.0 * s)),
            ("leg-right", "rotation"): mul(qx(13.0 - 5.0 * c), qz(-4.0 - 3.0 * s)),
            ("torso", "rotation"): mul(qx(6.0), qz(-2.0 * s)),
            ("head", "rotation"): mul(qx(12.0), qz(-3.0 * c)),
            ("arm-left", "rotation"): qz(0.8 * ARM_LIFT + 4.0 * s),
            ("arm-right", "rotation"): qz(-0.8 * ARM_LIFT + 4.0 * s),
            ("forearm-left", "rotation"): qz(39.0 + 7.0 * s),
            ("forearm-right", "rotation"): qz(-(39.0 - 7.0 * s)),
        })
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall}
