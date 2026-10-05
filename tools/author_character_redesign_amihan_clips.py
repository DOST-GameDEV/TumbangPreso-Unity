"""New locomotion clips for the Amihan redesign PROTOTYPE.

Imported by tools/author_character_redesign_amihan.py, which writes them into the prototype .glb
IN PLACE OF the clips of the same name copied from team-amihan.glb. Nothing in the game reads
them; team-amihan.glb keeps its own. A copy of Dante's clips script rewritten for her: rule 9 of
docs/CHARACTER_REDESIGN_DANTE.md section 13 says each hero's motion is its OWN, so none of the
numbers below are his.

WHAT HER MOTION SHOULD SAY ABOUT HER (ArtSource/amihan/concept-20260925/design-brief.md):
she is the wind hero, the fastest of the cast (bilis 5, lakas 2), light in the body, bright, and
she "hates a stalled game: when a match slows she is the one moving". So:
  * LIGHT, NOT HEAVY. Dante lands and squashes. She barely lands: little squash on the contact,
    more stretch at the top, and a bounce curve that HANGS at its height (the time in the air is
    long, the time on the ground is short). A heavy hero's curve is the other way round.
  * SHE NEVER QUITE SETTLES. Even at the low point of a step the root is a few millimetres off
    its rest, a hover; the wind is under her.
  * SHE LEADS WITH THE CHEST AND THE ARMS TRAIL. The body leans into where she is going and the
    wide sleeves are left behind and out to the sides, like cloth in a draught: in the walk the
    arms swing a little, held away from the skirt; in the sprint they are swept BACK and held
    there, fluttering, while only the legs work (the floating run).
  * SHE SWAYS. A small roll of the shoulders over the hips each step, the head tipping the other
    way, so the walk has a skip in it rather than a march.
  * IN THE AIR SHE IS A LEAF. The jump (a STANDING jump, both feet together) throws the arms up
    in a V and she hangs there, back a little arched; the fall rocks from side to side with the arms up and soft, the legs
    together and trailing, not kicking.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops her left arm from straight out to hanging.

ARMS UP GO THROUGH THE ELBOW (rule 8, amended by the owner on Dante: "the arms dont really get
much higher than the original A posing.. the jump looks like he's shrugging"). A straight arm
cannot rise above straight out: her head is wider than her shoulders and sits on top of the arm.
So the upper arm lifts only `ARM_LIFT`, the forearm folds UP and out past the cheek, and it
stretches (scale on the forearm bone) as it is thrown. In walk and sprint the folded forearm is
turned OUTWARD by `SPLAY`, never across the torso.

Each clip keeps the LENGTH of the one it replaces (walk 0.72 s, sprint 0.48, jump 0.50,
fall 0.33); the idle is ACTED and is 8 s, not the old 1.33 (see `idle`). NOT SEEN IN UNITY: the game layers procedural motion on these bones and the `root`
scale and forearm scale channels are new.
"""
import math

RATE = 60.0
SPLAY = 36.0      # degrees a folded forearm is turned out from straight ahead
ARM_LIFT = 14.0   # degrees the upper arm may rise above straight out. It was 9 under the full-size head; at 0.84 the ear is 17 mm above the cuff at 14


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


def _float(s):
    """Her bounce: 0 as the legs pass, 1 at full stride, and it HANGS near 1. A power under one
    half keeps her up for most of the step and drops her through the contact quickly."""
    return abs(s) ** 0.42


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


#   where her LEFT arm points in each stance: (upper arm, forearm). The right is the mirror.
#   Her sleeve ends in a cuff 150 mm across, so every stance keeps the upper arm well OUT from
#   the body: the cuff is what would cut into the coat, long before the hand does.
ARMS_HANG = ((0.69, -0.72, -0.02), (0.62, -0.74, 0.26))
#   hands behind her back: the elbows out and back, the forearms turned in behind the skirt.
#   The forearm stretches to get there (`BACK_REACH`): hers is 90 mm and the skirt is 230 wide.
ARMS_BACK = ((0.66, -0.62, -0.42), (-0.52, -0.46, -0.72))
BACK_REACH = 0.55
#   a fist on the hip: the elbow out and back, the forearm forward to the side of the belt
ARMS_HIP = ((0.78, -0.45, -0.44), (0.25, -0.50, 0.83))
HIP_REACH = 0.15
#   a hand held up to feel the wind: the upper arm just above level (`ARM_LIFT`), the forearm up
#   and out past the cheek, a little ahead of her, the same wide V as her jump and for the same
#   reason
ARMS_WIND = ((0.970, 0.242, 0.07), (0.50, 0.82, 0.26))
WIND_REACH = 0.75   # it was 1.25 beside the full-size head
#   loose in the air on a hop: floated out and up a little, the sleeves catching it
ARMS_FLOAT = ((0.86, -0.50, -0.06), (0.80, -0.56, 0.20))
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of a kid who cannot stand still, in three things she does while waiting:

      HANDS BEHIND HER BACK, UP ON HER TOES (0.9 to 3.3 s). She clasps her hands behind the
      skirt and rocks up onto her toes twice, looking one way down the street and then the other.
      TWO SMALL HOPS ON THE SPOT (3.7 to 4.6 s). The arms come loose and float out as she goes up.
      FEELING THE WIND (5.0 to 7.6 s). Her right hand goes up beside her head, her left fist to
      her hip, her chin lifts toward the raised hand and she leans into where the air comes from.

    THREE TRIES, AS ON DANTE. The clip this replaces moved four bones on straight ramps. A first
    new one layered sines on every bone; the owner, 2026-10-05, of Dante's: "the idle looks like
    he's just distorting around.. needs more character, like sometimes he puts his hands on the
    waist, or crosses his arms, other things idle people do". So the idle is ACTED: stances held
    long enough to read, the breathing kept small underneath, arms posed by where they point
    (`_arm`). Her stances are her own. Dante folds his arms and taps a foot, a kid who waits by
    digging in. She waits by nearly leaving the ground, and by reading the wind off the cloth
    hung out to air (the brief, section 3).

    IT IS 8 s, NOT THE OLD 1.33 s. Stances need time. Anything in the game that assumes the
    idle's length needs a look, and the game may prefer these as separate clips it picks between.
    Frame 0 is the neutral stand.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 6.0))
        back = _window(t, 0.9, 3.3, 0.40)
        wind = _window(t, 5.0, 7.6, 0.45)
        # up on her toes twice while her hands are behind her, each rise held a moment
        toes = back * (_window(t, 1.35, 2.15, 0.28) + _window(t, 2.35, 3.10, 0.28))
        # two hops: a dip, a rise, a soft landing, and again a little lower
        hop = _bump(t, 3.92, 0.10) + 0.7 * _bump(t, 4.34, 0.09)
        dip = _bump(t, 3.76, 0.07) + 0.6 * _bump(t, 4.20, 0.06) + 0.5 * _bump(t, 4.52, 0.07)
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            hang = _arm(side, *ARMS_HANG)
            behind = _arm(side, *ARMS_BACK)
            third = _arm(side, *(ARMS_HIP if side > 0 else ARMS_WIND))
            floats = _arm(side, *ARMS_FLOAT)
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(_mix(_mix(hang[k], floats[k], min(1.0, hop)), behind[k], back), third[k], wind)
                row[(bone, "rotation")] = q
            reach = BACK_REACH * back + (HIP_REACH if side > 0 else WIND_REACH) * wind
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        # where she looks: down the street one way, then the other; then up at her raised hand
        look = 30.0 * (_window(t, 1.3, 2.2, 0.3) - _window(t, 2.3, 3.2, 0.3)) - 16.0 * _window(t, 5.3, 7.3, 0.35)
        nod = -5.0 * toes - 12.0 * _window(t, 5.3, 7.3, 0.35) + 5.0 * dip
        lean = 4.0 * back + 3.0 * wind                    # chest out, both times
        shift = -3.0 * wind + 1.5 * math.sin(2.0 * math.pi * t / 4.0) * back
        sway = wind * 2.0 * math.sin(2.0 * math.pi * (t - 5.0) / 1.3)   # she gives a little to the air
        row.update({
            ("root", "translation"): (0.0, 0.010 * toes + 0.034 * hop, 0.0),
            ("root", "scale"): _squash(1.0 + 0.012 * breath + 0.035 * toes + 0.05 * hop - 0.06 * dip),
            ("root", "rotation"): qz(shift + sway),
            ("torso", "rotation"): mul(mul(qx(-lean + 1.0 * breath), qz(-0.8 * shift - sway)), qy(0.25 * look)),
            ("head", "rotation"): mul(mul(qy(0.75 * look), qx(nod - 0.8 * breath)), qz(-0.5 * shift + 5.0 * wind)),
            ("leg-left", "rotation"): qz(1.0 - shift - sway),
            ("leg-right", "rotation"): qz(-1.0 - shift - sway),
        })
        _add(out, t, row)
    return out


def walk():
    """A skipping walk: a long float between steps, a sway of the shoulders, the sleeves held out."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.8)
        air = _float(s)
        sway = math.sin(phase - 0.5)          # the shoulders arrive a moment after the foot
        _add(out, t, {
            ("root", "translation"): (0.0, 0.007 + 0.050 * air, 0.0),
            ("root", "rotation"): mul(qx(4.0), qz(1.6 * sway)),
            # light: she loses 3 per cent on the contact and gains 6 at the top
            ("root", "scale"): _squash(0.97 + 0.09 * air),
            ("leg-left", "rotation"): mul(qx(-34.0 * sh), qz(2.0)),
            ("leg-right", "rotation"): mul(qx(34.0 * sh), qz(-2.0)),
            ("torso", "rotation"): mul(mul(qx(3.0), qy(7.0 * sh)), qz(-3.4 * sway)),
            ("head", "rotation"): mul(mul(qx(-6.0 - 2.5 * math.sin(2.0 * phase - 1.0)), qy(-4.5 * sh)), qz(4.2 * sway)),
            # the arms hang well away from the skirt and are carried a little BEHIND her; they swing
            # less than her legs do, the sleeves are being towed
            ("arm-left", "rotation"): mul(qx(10.0 + 20.0 * sh), qz(-56.0 - 4.0 * air)),
            ("arm-right", "rotation"): mul(qx(10.0 - 20.0 * sh), qz(56.0 + 4.0 * air)),
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(24.0 - 9.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(24.0 + 9.0 * sh)),
        })
    return out


def sprint():
    """The floating run: the chest leads, the arms are swept back and held, only the legs work."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.6)
        air = _float(s)
        flutter = math.sin(2.0 * phase + 0.7)   # the sleeves shake twice a stride, in the draught
        _add(out, t, {
            ("root", "translation"): (0.0, 0.016 + 0.040 * air, 0.0),
            ("root", "rotation"): qx(13.0),
            ("root", "scale"): _squash(0.985 + 0.075 * air),
            ("leg-left", "rotation"): mul(qx(-50.0 * sh), qz(2.0)),
            ("leg-right", "rotation"): mul(qx(50.0 * sh), qz(-2.0)),
            ("torso", "rotation"): mul(qx(10.0), qy(6.0 * sh)),
            ("head", "rotation"): mul(qx(-20.0 - 2.0 * flutter), qy(-3.6 * sh)),
            # swept back about 38 degrees and held: each arm gives a little as its own leg drives
            ("arm-left", "rotation"): mul(qx(38.0 + 7.0 * sh + 4.0 * flutter), qz(-42.0 - 3.0 * flutter)),
            ("arm-right", "rotation"): mul(qx(38.0 - 7.0 * sh + 4.0 * flutter), qz(42.0 + 3.0 * flutter)),
            # nearly straight, turned out: the hands trail behind and wide, not tucked to the chest
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(12.0 + 5.0 * flutter))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(12.0 + 5.0 * flutter)),
        })
    return out


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. Picked up like a leaf: a long thin stretch with the arms thrown
    up in a V, then she hangs, back a little arched, both legs trailing behind her as one."""
    out = {}
    for t in _times(0.50):
        rise = math.exp(-t / 0.15)                 # 1 at take-off, easing away: she settles slowly
        hang = 1.0 - math.exp(-t / 0.13)
        ring = math.cos(2.0 * math.pi * t / 0.50) * math.exp(-t / 0.16)
        # ARMS UP, A V BESIDE HER HEAD. Under the full-size head (hair 0.238 wide) the fold had to stay
        # near 50 degrees from level and the forearm stretched to 2.35 times to get the fists past
        # her ears. The head is 0.84 now (hair 0.200 wide), so the upper arm lifts 14 degrees, the
        # forearm stands at about 58 from level and the stretch is 1.9 at take-off, easing to 1.5:
        # the fists finish at eye height beside the hair. Steeper than this the fist is in the side
        # locks; she has a 90 mm forearm and that is the ceiling.
        lift = ARM_LIFT * (0.60 + 0.40 * rise)
        fore = 44.0 - 2.0 * rise + 2.0 * ring      # the forearm's fold UP from the upper arm
        reach = 1.50 + 0.40 * rise                 # and its cartoon stretch as it is thrown
        _add(out, t, {
            ("root", "scale"): _squash(1.0 + 0.15 * ring),
            ("root", "rotation"): qx(-4.0 * hang),
            ("leg-left", "rotation"): mul(qx(17.0 * hang), qz(2.5)),
            ("leg-right", "rotation"): mul(qx(17.0 * hang), qz(-2.5)),
            ("torso", "rotation"): qx(-12.0 * rise - 5.0 * hang),
            ("head", "rotation"): qx(-12.0 * hang - 6.0 * rise),
            ("arm-left", "rotation"): mul(qy(-10.0 * hang), qz(lift)),
            ("arm-right", "rotation"): mul(qy(10.0 * hang), qz(-lift)),
            ("forearm-left", "rotation"): qz(fore),
            ("forearm-right", "rotation"): qz(-fore),
            ("forearm-left", "scale"): (reach, 1.0, 1.0),
            ("forearm-right", "scale"): (reach, 1.0, 1.0),
        })
    return out


def fall():
    """A loop: she drifts down rocking from side to side, arms up and soft, legs together."""
    out = {}
    length = 1.0 / 3.0
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        _add(out, t, {
            ("root", "scale"): _squash(1.045 + 0.012 * math.sin(2.0 * phase)),
            # the rock of a falling leaf: the body tips one way and the legs are left the other
            ("root", "rotation"): qz(4.5 * s),
            ("leg-left", "rotation"): mul(qx(14.0 + 4.0 * c), qz(3.0 - 5.0 * s)),
            ("leg-right", "rotation"): mul(qx(8.0 - 4.0 * c), qz(-3.0 - 5.0 * s)),
            ("torso", "rotation"): mul(qx(5.0), qz(-2.5 * s)),
            ("head", "rotation"): mul(qx(11.0), qz(-3.0 * s)),
            # arms up in the V of her jump, the high side's forearm opening as she tips
            ("arm-left", "rotation"): mul(qy(-6.0), qz(0.4 * ARM_LIFT + 3.0 * s)),
            ("arm-right", "rotation"): mul(qy(6.0), qz(-0.4 * ARM_LIFT + 3.0 * s)),
            ("forearm-left", "rotation"): qz(44.0 + 6.0 * s),
            ("forearm-right", "rotation"): qz(-(44.0 - 6.0 * s)),
            ("forearm-left", "scale"): (1.50 + 0.10 * s, 1.0, 1.0),
            ("forearm-right", "scale"): (1.50 - 0.10 * s, 1.0, 1.0),
        })
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall}
