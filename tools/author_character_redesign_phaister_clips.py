"""New locomotion clips for the Phaister (Soraya) redesign PROTOTYPE.

Imported by tools/author_character_redesign_phaister.py, which writes them into the prototype
.glb IN PLACE OF the clips of the same name copied from team-phaister.glb. Nothing in the game
reads them; team-phaister.glb keeps its own, and so do the files under
Art/characters/phaister-motion. Every other clip in the .glb (slide, sit, the emotes, her three
hero clips) is copied across untouched. A copy of the cast's clips script rewritten for her:
each hero's motion is its OWN (docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so none of
the numbers below are another hero's.

WHAT HER MOTION SHOULD SAY ABOUT HER (ArtSource/phaister/kit-20260927/design-brief.md,
`GaitStyles.Phaister`): a mischievous Visayan witch whose magic is moths, moonlight and a rag
doll full of pins. "Unhurried and sure." "Playful and pleased with herself, never menacing."
She "casts with small, precise hands (a prick of a pin, a flick of the wrist)" and "never
strains". She "struts, one foot across the centre line, chin up, a flourish in the arm swing;
the run is an exit with a cape". So:
  * SHE GLIDES. The bounce is low and even, the hat rides level (the head gives back what the
    hips roll), the chin stays up. Dante stomps and Amihan floats; she strolls.
  * THE HIPS LEAD. Weight is always on one hip, in the idle and at each step of the walk.
  * THE HANDS ARE SMALL AND EXACT. Nothing in the idle is fast but two pricks of a pin and one
    flick of the fingers.
  * THE HAT HAS WEIGHT. In the jump it is left behind for a moment and catches up.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops her left arm from straight out to hanging.

ARMS UP GO THROUGH THE ELBOW (section 13 rule 8, amended by the owner on Dante: "the jump looks
like he's shrugging"). Her elbow is where the black sleeve meets the purple band, 76 mm from the
shoulder; the band, the rim, the cuff and the hand ride the forearm bone, 85 mm more to the
palm. A straight arm cannot rise above level (her side hair and her brim are over it), so the
upper arm lifts a little and the forearm folds up, and may stretch (scale on the forearm bone)
as it is thrown.

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


#   where her LEFT arm points in each stance: (upper arm, forearm), in the file's space (+x her
#   left, +y up, +z ahead). The right is the mirror. Her forearm carries a cuff 176 mm across, so
#   every stance keeps the elbow OUT from the body: the cuff is what would cut into the coat.
ARMS_HANG = ((0.74, -0.67, 0.02), (0.60, -0.76, 0.24))
#   THE DOLL. Her left hand comes up in front of her belt, palm up, the way she holds the manika
#   that hangs at her left hip; the elbow stays out to the side.
ARM_DOLL = ((0.62, -0.62, 0.48), (-0.34, -0.10, 0.94))
DOLL_REACH = 0.30
#   THE PIN. Her right hand (written as a left arm, mirrored below) held over the doll, a pin in
#   it, and then pushed down: a short straight prick from the elbow.
ARM_PIN_UP = ((0.66, -0.30, 0.69), (-0.30, 0.52, 0.80))
ARM_PIN_DOWN = ((0.66, -0.42, 0.62), (-0.42, -0.36, 0.83))
PIN_REACH = 0.25
#   A HAND ON HER HIP: the elbow out and back, the forearm forward to the side of the belt
ARM_HIP = ((0.80, -0.42, -0.43), (0.22, -0.52, 0.83))
#   THE FLICK. Her right arm out to the side at shoulder height, the forearm folded up beside
#   her, and then thrown open: a moth sent off her fingers.
#   (v03's flick opened only 30 degrees and from the front it read as a hand held at the shoulder;
#   it now opens flat out to her side and the forearm reaches further as it goes)
ARM_FLICK_HELD = ((0.92, 0.05, 0.38), (0.25, 0.90, 0.35))
ARM_FLICK_OPEN = ((0.97, 0.18, 0.15), (0.95, 0.30, 0.05))
FLICK_REACH = 0.60
#   THE MOON. Both hands in front of her, palms up, forearms opening outward as she lifts her
#   face: the gesture she closes her circle with (ArtSource/phaister/kit-20260927/design-brief.md).
ARM_MOON_LOW = ((0.70, -0.52, 0.49), (0.36, 0.30, 0.88))
ARM_MOON_OPEN = ((0.80, -0.36, 0.48), (0.74, 0.56, 0.37))
MOON_REACH = 0.30
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of a witch who is in no hurry, in three things she does while waiting:

      THE DOLL AND THE PIN (0.7 to 3.3 s). Her left hand comes up in front of her belt as if the
      manika lay in it, her right hand comes over it and pricks down twice, small and exact,
      her head bowed to watch. After the second prick she looks up and to her right, chin
      lifted, to see who jumped.
      A MOTH OFF HER FINGERS (3.6 to 5.1 s). Left hand on her hip, weight on that hip. Her right
      arm comes out to the side with the forearm folded up, holds, and flicks open; her head
      follows what she let go, up and away.
      THE MOON (5.4 to 7.6 s). Both hands come up in front of her, palms up, and open outward as
      her chin lifts and she rises a little through her whole body, then lets it go.

    WHY THESE. Each one is hers and nobody else's in the cast: the manika and the hat pin are her
    kit (MANIKA MISCHIEF, SPOTLIGHT PIN), the moths are her swarm, the moon is her sigil and the
    thing sewn on her back. Her brief says she casts "with small, precise hands (a prick of a
    pin, a flick of the wrist)" and that she is "unhurried and sure", "playful and pleased with
    herself, never menacing", so nothing here is fast except the two pricks and the flick, and
    she never bends to anything: her chin stays up except when she looks at her own hands.
    Between the stances she stands the way she walks, weight on one hip (`GaitStyles.Phaister`:
    "one foot across the centre line").

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 5.0))
        doll = _window(t, 0.7, 3.3, 0.45)
        flick = _window(t, 3.6, 5.1, 0.40)
        moon = _window(t, 5.4, 7.6, 0.55)
        # two pricks, each a quick push and a slower lift
        prick = min(1.0, _bump(t, 1.62, 0.085) + _bump(t, 2.16, 0.085))
        # the look up after the second prick
        glance = _window(t, 2.40, 3.20, 0.25)
        # the flick: held folded, then open in a tenth of a second, then it hangs open
        snap = _ease((t - 4.22) / 0.10) * (1.0 - _ease((t - 4.75) / 0.35))
        # the moon: the hands open over the first second and stay
        open_ = _ease((t - 5.8) / 0.9)
        rise = moon * _ease((t - 5.8) / 0.9) * (1.0 - 0.5 * _ease((t - 6.9) / 0.6))
        row = {}
        hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
        left_doll = _arm(1, *ARM_DOLL)
        up, down = _arm(-1, *ARM_PIN_UP), _arm(-1, *ARM_PIN_DOWN)
        right_pin = tuple(_mix(up[k], down[k], prick) for k in range(2))
        left_hip = _arm(1, *ARM_HIP)
        held, thrown = _arm(-1, *ARM_FLICK_HELD), _arm(-1, *ARM_FLICK_OPEN)
        right_flick = tuple(_mix(held[k], thrown[k], snap) for k in range(2))
        for side, name in ((1, "left"), (-1, "right")):
            low, wide = _arm(side, *ARM_MOON_LOW), _arm(side, *ARM_MOON_OPEN)
            lunar = tuple(_mix(low[k], wide[k], open_) for k in range(2))
            first = left_doll if side > 0 else right_pin
            second = left_hip if side > 0 else right_flick
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(_mix(_mix(hang[side][k], first[k], doll), second[k], flick), lunar[k], moon)
                row[(bone, "rotation")] = q
            reach = (DOLL_REACH if side > 0 else PIN_REACH) * doll + (0.10 if side > 0 else FLICK_REACH * (0.35 + 0.65 * snap)) * flick \
                + MOON_REACH * moon * open_
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        # her head: bowed to the doll, then up and to her right; after the moth, up and away to her
        # right; lifted to the moon
        look = -30.0 * glance - 24.0 * flick * (0.4 + 0.6 * snap) + 10.0 * doll * (1.0 - glance)
        nod = 16.0 * doll * (1.0 - glance) - 9.0 * glance - 8.0 * flick * snap - 15.0 * rise + 3.0 * prick
        tilt = 5.0 * doll * (1.0 - glance) - 6.0 * flick
        # her weight: on her left hip as a rule, further over while her left hand is on it
        hip = 2.2 + 2.6 * flick - 1.6 * moon
        row.update({
            ("root", "translation"): (0.0, 0.012 * rise - 0.004 * prick, 0.0),
            ("root", "scale"): _squash(1.0 + 0.010 * breath + 0.040 * rise - 0.020 * prick),
            ("root", "rotation"): qz(hip),
            ("torso", "rotation"): mul(mul(qx(4.0 * doll * (1.0 - glance) - 3.0 * rise - 2.0 * flick + 0.8 * breath), qz(-1.1 * hip)),
                                       qy(0.30 * look + 6.0 * doll - 8.0 * flick)),
            ("head", "rotation"): mul(mul(qy(0.70 * look), qx(nod - 4.0 - 0.6 * breath)), qz(tilt - 0.4 * hip)),
            # the left leg under her, the right one set across the centre line in front of it
            ("leg-left", "rotation"): qz(1.0 - hip),
            ("leg-right", "rotation"): mul(qx(-5.0 + 3.0 * moon), qz(2.5 - hip)),
        })
        _add(out, t, row)
    return out


def walk():
    """A strut: each foot set across the centre line, the hips rolling over it, the chin up and
    the hat held level, and a flourish in the arm swing: the forearm flicks out at the front of it."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.85)
        c = math.cos(phase)
        lift = abs(s) ** 0.7                    # she glides: a low, even bounce
        roll = math.sin(phase - 0.35)           # the hips arrive a moment after the foot
        # the flourish: as an arm comes forward its forearm opens outward, late and quick
        fl_left = max(0.0, -math.sin(phase - 0.6)) ** 2.0
        fl_right = max(0.0, math.sin(phase - 0.6)) ** 2.0
        _add(out, t, {
            ("root", "translation"): (0.010 * roll, 0.004 + 0.022 * lift, 0.0),
            ("root", "rotation"): mul(qx(-1.5), qz(4.2 * roll)),
            ("root", "scale"): _squash(0.975 + 0.05 * lift),
            # the forward leg swings IN across the line as it reaches
            ("leg-left", "rotation"): mul(qx(-38.0 * sh), qz(-4.2 * roll - 5.0 * max(0.0, sh))),
            ("leg-right", "rotation"): mul(qx(38.0 * sh), qz(-4.2 * roll + 5.0 * max(0.0, -sh))),
            ("torso", "rotation"): mul(mul(qx(-2.0), qy(8.0 * sh)), qz(-6.5 * roll)),
            # the hat rides level: the head gives back what the hips and chest roll
            ("head", "rotation"): mul(mul(qx(-6.0 + 1.5 * c), qy(-5.5 * sh)), qz(2.3 * roll)),
            ("arm-left", "rotation"): mul(qx(8.0 + 24.0 * sh), qz(-50.0 + 5.0 * fl_left)),
            ("arm-right", "rotation"): mul(qx(8.0 - 24.0 * sh), qz(50.0 - 5.0 * fl_right)),
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(30.0 - 22.0 * fl_left))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(30.0 - 22.0 * fl_right)),
        })
    return out


def sprint():
    """An exit with a cape: the hat leads, both arms are flung out and back like the edges of the
    cape she is wearing, held high and wide, and she rocks from foot to foot under them."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.65)
        lift = abs(s) ** 0.6
        rock = math.sin(phase - 0.3)
        beat = math.sin(2.0 * phase + 0.4)      # the arms beat once a step, like wings
        _add(out, t, {
            ("root", "translation"): (0.0, 0.008 + 0.036 * lift, 0.0),
            ("root", "rotation"): mul(qx(15.0), qz(3.0 * rock)),
            ("root", "scale"): _squash(0.96 + 0.09 * lift),
            ("leg-left", "rotation"): mul(qx(-52.0 * sh), qz(1.5 - 3.0 * rock)),
            ("leg-right", "rotation"): mul(qx(52.0 * sh), qz(-1.5 - 3.0 * rock)),
            ("torso", "rotation"): mul(mul(qx(9.0), qy(7.0 * sh)), qz(-4.0 * rock)),
            # the hat leads: the head is carried forward of the chest and held there
            ("head", "rotation"): mul(mul(qx(-16.0 + 2.5 * beat), qy(-4.5 * sh)), qz(1.5 * rock)),
            # out wide and back, only a little below level: the cape's edges
            ("arm-left", "rotation"): mul(qy(34.0 + 6.0 * sh), qz(-16.0 + 7.0 * beat)),
            ("arm-right", "rotation"): mul(qy(-34.0 + 6.0 * sh), qz(16.0 - 7.0 * beat)),
            ("forearm-left", "rotation"): mul(qy(16.0 - 6.0 * beat), qz(-10.0 - 8.0 * beat)),
            ("forearm-right", "rotation"): mul(qy(-16.0 + 6.0 * beat), qz(10.0 + 8.0 * beat)),
        })
    return out


#   arms up: where the left arm points at the top of the jump. Her side hair hangs beside her
#   shoulders and her brim is over them, so the V is thrown up and a little AHEAD of her, in
#   front of the hair, not straight out to the sides.
ARM_UP = ((0.86, 0.24, 0.45), (0.50, 0.80, 0.33))
ARM_UP_REACH = 0.75


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. She goes up like something she conjured: a long stretch with
    both arms thrown up in a V through the elbows, the hat left behind for a moment (the head
    tips back and then catches up), her feet together and pointed under her."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.14)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.12)
        ring = math.cos(2.0 * math.pi * t / 0.50) * math.exp(-t / 0.17)
        row = {
            ("root", "scale"): _squash(1.0 + 0.17 * ring),
            ("root", "rotation"): qx(-3.0 * hang),
            # both legs the same: trailing at take-off, then drawn together under her
            ("leg-left", "rotation"): mul(qx(14.0 * rise - 9.0 * hang), qz(3.0 - 4.0 * hang)),
            ("leg-right", "rotation"): mul(qx(14.0 * rise - 9.0 * hang), qz(-3.0 + 4.0 * hang)),
            ("torso", "rotation"): qx(-9.0 * rise - 3.0 * hang),
            # the hat lags: back as she leaves, forward again as she hangs
            ("head", "rotation"): qx(-17.0 * rise + 5.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 9.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.75 + 0.25 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: she comes down upright under her hat, arms up in the V and wavering, her feet
    paddling a little, one and then the other, looking down at where she will land."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.05 + 0.012 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(3.0), qz(2.0 * s)),
            ("leg-left", "rotation"): mul(qx(-6.0 + 9.0 * s), qz(4.0)),
            ("leg-right", "rotation"): mul(qx(-6.0 - 9.0 * s), qz(-4.0)),
            ("torso", "rotation"): mul(qx(4.0), qz(-1.5 * s)),
            ("head", "rotation"): mul(qx(13.0 + 1.5 * c), qz(-1.0 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(side * 4.0 * s * side))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * (6.0 * c * side)))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.72 + 0.06 * s * side), 1.0, 1.0)
        _add(out, t, row)
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall}
