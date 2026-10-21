"""Locomotion clips for the Cheska (displayed: Yasmin) redesign PROTOTYPE.

Imported by tools/author_character_redesign_cheska.py, which writes them into the prototype .glb
IN PLACE OF the clips of the same name copied from team-cheska.glb. Nothing in the game reads
them; team-cheska.glb keeps its own.

WHAT HER MOTION SHOULD SAY ABOUT HER (written down first, then tuned to it; none of these
numbers are Dante's, docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9):
  She is the smallest and lightest of the cast, bundled into a hat bigger than her body, and her
  kit is ice. So she moves like a small kid on a frozen pond.
    * LIGHT. She spends longer at the top of a bounce than at the bottom of it (`FLOAT` pushes
      the bounce curve toward its top), and she barely squashes: a heavy hero lands heavy, she
      touches down.
    * QUICK, SMALL STEPS. Her legs swing through a small arc; the lift comes from the bounce.
    * HAPPY. Her head rocks from side to side with each step, which no other hero's does, and
      her whole body sways with it.
    * HANDS UP LIKE MITTENS in the walk: the upper arms held a little out from the body for
      balance, the forearms folded up and turned OUTWARD.
    * SHE SKATES when she sprints: tipped well forward, her weight thrown from one side to the
      other, the arms swept back and out behind her like wings and only fluttering.
    * SHE FLOATS when she falls: one slow sway, both arms out wide and beating together, the
      legs hanging close.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops her left arm from straight out to hanging, and a positive angle
on a forearm folds it UP.

⚠️ HOW HIGH HER ARMS CAN GO, AND WHY IT IS LESS THAN DANTE'S. Rule 8 (amended by the owner on
Dante: *"the arms dont really get much higher than the original A posing.. the jump looks like
he's shrugging"*) sends the arms up THROUGH THE ELBOW. Dante's forearm folds up past his cheek.
Hers cannot: her hat's ear flaps hang beside the cheeks from x 0.17 to 0.25, down to z 0.35,
right over the shoulders, and they are rigid to the head. Everything above the shoulder line is
head, flap or hair, except the space OUTSIDE the flaps. So her arms go up and OUT: the upper arm
stays `ARM_LIFT` under straight out (it passes under the flap's fur), the forearm folds up from
the elbow by `FORE_UP` at most (steeper and its top edge cuts the flap's outer corner, measured
in the model script's numbers), and it STRETCHES at take-off, so the fists are thrown wide and
high in an open Y. It reads as arms flung out, not a shrug; it is not arms overhead.

Each clip keeps the LENGTH of the one it replaces (walk 0.72 s, sprint 0.48, jump 0.50,
fall 0.333), except `idle`, which is ACTED and runs 8 s where the old one ran 1.333. NOT YET SEEN IN UNITY.
"""
import math

RATE = 60.0
SPLAY = 26.0      # degrees a folded forearm is turned OUT from straight ahead (never across her)
ARM_LIFT = -10.0  # the highest her upper arm goes: 10 degrees UNDER straight out, clear of the flap's fur
FORE_UP = 40.0    # the most a forearm folds up from that upper arm (30 degrees over the level).
#                   It was 28 before her head was drawn in to 0.84: the flap's outer corner came in
#                   from 0.237 to about 0.206, and the forearm's top edge now clears it steeper.
FLOAT = 0.55      # the bounce curve's exponent: under 1 she hangs at the top


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


def _rows(out, t, row):
    for key, value in row.items():
        out.setdefault(key, []).append((t, value))


def _norm(v):
    n = math.sqrt(sum(k * k for k in v))
    return tuple(k / n for k in v)


def _rot(q, v):
    """Turn vector `v` by quaternion `q`."""
    x, y, z, w = q
    tx, ty, tz = 2.0 * (y * v[2] - z * v[1]), 2.0 * (z * v[0] - x * v[2]), 2.0 * (x * v[1] - y * v[0])
    return (v[0] + w * tx + y * tz - z * ty, v[1] + w * ty + z * tx - x * tz, v[2] + w * tz + x * ty - y * tx)


def _arc(a, b):
    """The shortest turn that takes direction `a` onto direction `b`."""
    a, b = _norm(a), _norm(b)
    d = sum(p * q for p, q in zip(a, b))
    c = (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
    if d < -0.9999:
        return (0.0, 0.0, 1.0, 0.0)
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


# WHAT THIS KID DOES WHILE SHE WAITS. Not Dante's fists on hips and folded arms: those are a
# bigger, surlier kid's. She is the small one in the winter hat whose kit is ice, so:
#   1 SHE WARMS HER HANDS. Both fists brought together in front of her chest, her head dipped
#     to blow on them, a shiver through her shoulders. The one thing an ice hero in a fur hat
#     would do standing still.
#   2 SHE HOPS. Two little bounces on the spot with a look to each side: she cannot keep still,
#     and it is the same floaty bounce her walk has.
#   3 SHE TUGS HER HAT STRINGS. A hand up to each bobble that hangs from her ear flaps, pulling
#     them in turn so her head tips one way, then the other. Only she has them.
# Where her LEFT arm points in each: (upper arm, forearm), +x her left, +y up, +z ahead.
ARMS_HANG = ((0.68, -0.73, 0.02), (0.60, -0.72, 0.34))
#   the elbows come forward and a little out, the forearms run in and forward to meet ahead of
#   the bib (its pocket stands 116 mm ahead of her middle), stretched so the fists touch
ARMS_WARM = ((0.34, -0.66, 0.67), (-0.50, 0.42, 0.76))
WARM_REACH = 0.30
#   the elbow stays low and out, the forearm goes up and forward to the bobble. With her head at
#   0.84 the bobble hangs 178 mm out, 96 mm ahead, its middle at 0.293 (it was 198 and 107).
ARMS_TUG = ((0.58, -0.80, 0.16), (0.28, 0.53, 0.80))
TUG_REACH = 0.25
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of her waiting: she stands, warms her hands and blows on them, hops twice
    looking about, tugs her hat strings one after the other, lets go.

    The layered-sine idle this replaces was rejected on Dante (owner: "he's just distorting
    around.. needs more character, like sometimes he puts his hands on the waist, or crosses his
    arms, other things idle people do"), so this one is ACTED: stances held long enough to read,
    the breathing kept small underneath, arms posed by where they point (`_arm`).
    IT IS 8 s, NOT THE OLD 1.33 s: anything in the game that assumes the idle's length needs a look.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 6.0))
        warm = _window(t, 0.8, 3.3, 0.40)
        hop = _window(t, 3.7, 4.9, 0.15)
        tug = _window(t, 5.2, 7.5, 0.40)
        # the shiver: quick and small, only while her hands are together, in two bursts
        shiver = warm * math.sin(2.0 * math.pi * 9.0 * t) * (_window(t, 1.3, 1.9, 0.15) + _window(t, 2.5, 3.0, 0.15))
        blow = _window(t, 1.2, 2.1, 0.25) + _window(t, 2.4, 3.1, 0.25)       # the head dips to her hands
        # two hops, each hanging at its top
        up = hop * abs(math.sin(2.0 * math.pi * (t - 3.7) / 1.2)) ** FLOAT
        look = 24.0 * (_window(t, 3.7, 4.3, 0.2) - _window(t, 4.3, 4.9, 0.2))
        # the tugs: her left string, then her right; the head tips toward the hand that pulls
        pull_left = _window(t, 5.7, 6.4, 0.2)
        pull_right = _window(t, 6.6, 7.3, 0.2)
        tip = 9.0 * (pull_left - pull_right)
        row = {}
        for side, name, pull in ((1, "left", pull_left), (-1, "right", pull_right)):
            hang = _arm(side, *ARMS_HANG)
            warming = _arm(side, *ARMS_WARM)
            # a pull draws that hand down and in, off the bobble's rest
            tugging = _arm(side, ARMS_TUG[0], (ARMS_TUG[1][0] - 0.10 * pull, ARMS_TUG[1][1] - 0.34 * pull, ARMS_TUG[1][2]))
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                row[(bone, "rotation")] = _mix(_mix(hang[k], warming[k], warm), tugging[k], tug)
            row[("forearm-" + name, "scale")] = (1.0 + WARM_REACH * warm + TUG_REACH * tug, 1.0, 1.0)
        row.update({
            ("root", "translation"): (0.0, 0.034 * up, 0.0),
            ("root", "scale"): _squash(1.0 + 0.014 * breath + 0.05 * up - 0.03 * hop * (1.0 - up)),
            ("root", "rotation"): qz(1.5 * tip / 9.0),
            ("torso", "rotation"): mul(qx(3.0 * warm + 1.0 * breath), qy(1.6 * shiver)),
            ("head", "rotation"): mul(mul(qy(look), qx(9.0 * warm * blow - 5.0 * up - 1.0 * breath)), qz(tip + 1.2 * shiver)),
            ("leg-left", "rotation"): mul(qz(1.0 + 4.0 * up), qx(8.0 * up)),
            ("leg-right", "rotation"): mul(qz(-1.0 - 4.0 * up), qx(8.0 * up)),
        })
        _rows(out, t, row)
    return out


def walk():
    """A skipping walk: small quick steps, a long hang between them, mitten hands up, the head rocking."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.6)
        air = abs(s) ** FLOAT
        _rows(out, t, {
            ("root", "translation"): (0.006 * s, 0.058 * air, 0.0),
            ("root", "rotation"): mul(qx(1.0), qz(2.4 * s)),
            ("root", "scale"): _squash(0.965 + 0.075 * air),
            ("leg-left", "rotation"): mul(qx(-29.0 * sh), qz(2.0)),
            ("leg-right", "rotation"): mul(qx(29.0 * sh), qz(-2.0)),
            ("torso", "rotation"): mul(qx(2.0), qy(6.0 * sh)),
            # the head stays facing ahead and ROCKS: over to the side of the foot that is down
            ("head", "rotation"): mul(mul(qz(7.5 * math.sin(phase - 0.5)), qy(-3.6 * sh)), qx(-3.0 - 2.0 * math.sin(2.0 * phase - 0.6))),
            # arms a little out from the body, swinging a short way, opposite the legs
            ("arm-left", "rotation"): mul(qx(22.0 * sh), qz(-(57.0 - 5.0 * air))),
            ("arm-right", "rotation"): mul(qx(-22.0 * sh), qz(57.0 - 5.0 * air)),
            # the forearm folded up in front and turned OUT (never across her), pumping a little
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(54.0 - 13.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(54.0 + 13.0 * sh)),
        })
    return out


def sprint():
    """She skates: tipped forward, thrown from side to side, arms swept back and out like wings."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.55)
        air = abs(s) ** 0.8
        _rows(out, t, {
            # low and gliding, the weight carried across to the pushing side
            ("root", "translation"): (0.014 * sh, 0.006 + 0.036 * air, 0.0),
            ("root", "rotation"): mul(qx(12.0), qz(4.5 * sh)),
            ("root", "scale"): _squash(0.97 + 0.085 * air),
            ("leg-left", "rotation"): mul(qx(-45.0 * sh), qz(4.0)),
            ("leg-right", "rotation"): mul(qx(45.0 * sh), qz(-4.0)),
            ("torso", "rotation"): mul(qx(12.0), qy(8.0 * sh)),
            ("head", "rotation"): mul(mul(qz(-4.0 * sh), qy(-5.0 * sh)), qx(-21.0 - 2.0 * math.sin(2.0 * phase))),
            # swept back and held out from the body; only a flutter, in time with the push
            ("arm-left", "rotation"): mul(qx(50.0 + 8.0 * sh), qz(-(38.0 + 5.0 * math.sin(2.0 * phase)))),
            ("arm-right", "rotation"): mul(qx(50.0 - 8.0 * sh), qz(38.0 + 5.0 * math.sin(2.0 * phase))),
            # nearly straight, the hand trailing a little OUT, away from her
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(14.0 + 7.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(14.0 - 7.0 * sh)),
        })
    return out


def jump():
    """A standing hop: popped up thin, arms flung up and out in an open Y, both heels flicked back."""
    out = {}
    for t in _times(0.50):
        tuck = 1.0 - math.exp(-t / 0.08)
        ring = math.cos(2.0 * math.pi * t / 0.40) * math.exp(-t / 0.15)
        throw = math.exp(-t / 0.45)   # slow: the Y is HELD through the hang, it does not sag into a shrug
        # ARMS UP AND OUT (see the docstring for why not overhead). The game has one `jump`, played
        # from a standstill as well as from a run, so it is the same on both sides (the owner asked
        # Dante's for that: "i need a jump for standing still").
        lift = ARM_LIFT - 5.0 * (1.0 - throw)
        fore = FORE_UP + 5.0 * throw - 6.0 * (1.0 - throw) + 4.0 * ring
        reach = 1.15 + 0.45 * throw
        _rows(out, t, {
            ("root", "scale"): _squash(1.0 + 0.13 * ring),
            ("root", "rotation"): qx(0.0),
            # the toes trail at take-off, then both heels flick up behind her, a little apart
            ("leg-left", "rotation"): mul(qx(-8.0 + 23.0 * tuck), qz(2.0 + 6.0 * tuck)),
            ("leg-right", "rotation"): mul(qx(-8.0 + 23.0 * tuck), qz(-2.0 - 6.0 * tuck)),
            ("torso", "rotation"): qx(-7.0 * tuck),
            ("head", "rotation"): qx(-10.0 * tuck + 4.0 * ring),
            ("arm-left", "rotation"): mul(qy(-6.0 * tuck), qz(lift)),
            ("arm-right", "rotation"): mul(qy(6.0 * tuck), qz(-lift)),
            ("forearm-left", "rotation"): qz(fore),
            ("forearm-right", "rotation"): qz(-fore),
            ("forearm-left", "scale"): (reach, 1.0, 1.0),
            ("forearm-right", "scale"): (reach, 1.0, 1.0),
        })
    return out


def fall():
    """A loop: she floats down like a snowflake, one slow sway, arms wide and beating together."""
    out = {}
    length = 1.0 / 3.0
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        beat = math.sin(phase - 0.9)
        _rows(out, t, {
            ("root", "scale"): _squash(1.045 + 0.015 * math.sin(2.0 * phase)),
            ("root", "rotation"): qz(4.0 * s),
            # legs hanging close together, paddling a little, a touch forward
            ("leg-left", "rotation"): mul(qx(-9.0 + 6.0 * s), qz(4.0)),
            ("leg-right", "rotation"): mul(qx(-9.0 - 6.0 * s), qz(-4.0)),
            ("torso", "rotation"): mul(qx(4.0), qz(-3.0 * s)),
            ("head", "rotation"): mul(qx(12.0), qz(-4.0 * s)),
            # both wings together: the upper arms beat under the flaps, the forearms lag behind them
            ("arm-left", "rotation"): qz(ARM_LIFT - 9.0 + 7.0 * s),
            ("arm-right", "rotation"): qz(-(ARM_LIFT - 9.0 + 7.0 * s)),
            ("forearm-left", "rotation"): qz(FORE_UP - 8.0 + 8.0 * beat),
            ("forearm-right", "rotation"): qz(-(FORE_UP - 8.0 + 8.0 * beat)),
            ("forearm-left", "scale"): (1.12, 1.0, 1.0),
            ("forearm-right", "scale"): (1.12, 1.0, 1.0),
        })
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall}
