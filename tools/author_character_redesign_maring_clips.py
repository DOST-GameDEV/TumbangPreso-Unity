"""New locomotion clips for the Maring redesign PROTOTYPE.

Imported by tools/author_character_redesign_maring.py, which writes them into the prototype
.glb IN PLACE OF the clips of the same name copied from character-female-f.glb. Nothing in the
game reads them; character-female-f.glb keeps its own. The thirteen ACTION clips (the carry, the
throw, pick up, tag, shove, the two reaches, slide, crouch, sit, die, yes and no) are hers too,
further down; the fifteen clips left are copied across untouched. A copy of the
heroes' clips script rewritten for her: each character's motion is its OWN
(docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so none of the numbers below are another's.

WHAT HER MOTION SHOULD SAY ABOUT HER. MARING is "the cheerful one from the market: a brisk,
bouncy, hip-swinging walk, and a skipping run" (`GaitStyles.Maring`); "quick hands, quicker
mouth. She has talked her way out of more tags than she has dodged" (the character select);
"pure runner. In and out before the tag lands" (GAME_OVERVIEW.md: speed 5, power 2, grit 2).
A girl of sixteen or so from the street, not a fighter and not a hero. So:
  * SHE IS LIGHT. She is up on her toes and off the ground a lot; nothing of hers lands hard.
  * SHE IS NEVER SQUARE. Her weight is on one hip or the other and keeps changing.
  * HER HANDS ARE QUICK AND BUSY: a wave, a dip into her belt bag, a pat to shut it.
  * SHE IS FRIENDLY. She looks at people, tips her head, greets.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops her left arm from straight out to hanging.

ARMS UP GO THROUGH THE ELBOW (section 13 rule 8, amended by the owner on Dante: "the jump looks
like he's shrugging"). Her elbow is at the mouth of her sleeve, 82 mm from the shoulder; the
bare forearm and the fist ride the forearm bone, 116 mm more to the fist's end. A straight arm
cannot rise above level (her ears and her bunches are over it), so the upper arm lifts a little
and the forearm folds up, and may stretch (scale on the forearm bone) as it is thrown.

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
#   HANGING, HER WAY: loose and close to her sides, the forearms a little forward, the hands
#   never quite still.
ARMS_HANG = ((0.46, -0.88, 0.02), (0.34, -0.90, 0.26))
#   THE WAVE. Her right arm (written as a left arm, mirrored below) thrown up beside her head,
#   forward of the bunch of hair on that side, the forearm wagging from the elbow.
ARM_WAVE = ((0.64, 0.52, 0.44), (0.22, 0.95, 0.16))
WAVE_WAG = 0.42                # how far the forearm swings either side of straight up
WAVE_REACH = 0.85
#   and the other hand meanwhile: a fist on her hip, the elbow out
ARM_HIP = ((0.80, -0.56, -0.20), (-0.62, -0.62, 0.48))
#   THE BELT BAG, on her left hip. Her left hand lifts its flap from outside; her right comes
#   across her front to it and dips in.
ARM_BAG_LEFT = ((0.52, -0.78, 0.34), (-0.34, -0.66, 0.66))
ARM_BAG_RIGHT = ((0.30, -0.80, 0.52), (-0.84, -0.30, 0.44))
BAG_REACH = 0.42
#   THE RUNNER. Elbows folded, fists up in front of her ribs, as they are when she runs.
ARM_RUN = ((0.42, -0.86, -0.10), (0.16, 0.10, 0.98))
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of the girl from the market who cannot stand still, in three things she does:

      THE WAVE (0.5 to 2.9 s). She has seen someone she knows across the street. Up on her
      toes, her right arm thrown up beside her head, the forearm wagging side to side three
      times from the elbow, her left fist on her hip, her head tipped toward the hand.
      THE BELT BAG (3.2 to 5.3 s). She looks down at the bag on her left hip, her left hand
      lifts the flap, her right comes across and dips in twice, quick (counting the change she
      is holding for her mother's stall), and she pats it shut with a nod.
      ON HER MARKS (5.6 to 7.7 s). Elbows fold, fists come up, and she jogs on the spot: four
      quick knees, left, right, left, right, the whole of her bobbing, looking down the street
      to her right at where she means to run. Then she drops her hands and settles.

    WHY THESE. She is "the cheerful one from the market" (GaitStyles.Maring), "quick hands,
    quicker mouth" (the character select) and the roster's pure runner, "in and out before the
    tag lands" (GAME_OVERVIEW.md). So she greets, she handles money with fast hands, and she
    warms up her legs. None of the three is another character's (Dante: fists on hips and
    folded arms; Sean: a flex, a bull lean, a guard; Amihan and Cheska: hops and hands behind
    the back; Bebang: fist into palm, the headband, a shoulder roll; Lola Pacing and Bayan have
    their own). All through it her weight keeps shifting from one hip to the other: she is
    never square on both feet, where Bebang always is.

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
    hip = _arm(1, *ARM_HIP)
    bag = {1: _arm(1, *ARM_BAG_LEFT), -1: _arm(-1, *ARM_BAG_RIGHT)}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 5.0))
        wave = _window(t, 0.5, 2.9, 0.38)
        purse = _window(t, 3.2, 5.3, 0.38)
        jog = _window(t, 5.6, 7.7, 0.34)
        # three wags of the forearm
        wag = math.sin(2.0 * math.pi * (t - 0.95) / 0.52) * _window(t, 0.95, 2.51, 0.20)
        # two dips of the right hand into the bag, then the pat that shuts it
        dip_in = _bump(t, 3.95, 0.09) + _bump(t, 4.33, 0.09)
        pat = _bump(t, 4.80, 0.08)
        # four knees: left, right, left, right
        knees = [_bump(t, at, 0.085) for at in (6.02, 6.36, 6.70, 7.04)]
        knee_left, knee_right = knees[0] + knees[2], knees[1] + knees[3]
        hop = sum(_bump(t, at + 0.02, 0.10) for at in (6.02, 6.36, 6.70, 7.04))
        settle = _bump(t, 7.62, 0.10) - 0.5 * _bump(t, 7.82, 0.10)
        # her weight, on her left hip through the wave, her right through the bag, level for the jog
        lean = _window(t, 0.2, 3.0, 0.5) - _window(t, 3.1, 5.5, 0.5)
        row = {}
        waving = _arm(-1, ARM_WAVE[0], (ARM_WAVE[1][0] + WAVE_WAG * wag, ARM_WAVE[1][1], ARM_WAVE[1][2]))
        for side, name in ((1, "left"), (-1, "right")):
            first = hip if side > 0 else waving
            pump = (knee_right - knee_left) * side          # an arm comes forward with the other knee
            third = _arm(side, (ARM_RUN[0][0], ARM_RUN[0][1], ARM_RUN[0][2] + 0.45 * pump), (ARM_RUN[1][0], ARM_RUN[1][1] + 0.35 * pump, ARM_RUN[1][2]))
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(_mix(_mix(hang[side][k], first[k], wave), bag[side][k], purse), third[k], jog)
                if k == 1 and purse > 0.0:
                    # the right hand dips; both hands pat
                    q = mul(q, qz(side * (-10.0 * dip_in * (1.0 if side < 0 else 0.0) - 8.0 * pat) * purse))
                row[(bone, "rotation")] = q
            reach = (WAVE_REACH * wave if side < 0 else 0.0) + BAG_REACH * purse * (1.0 if side < 0 else 0.55)
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        tiptoe = 0.035 * wave * (0.7 + 0.3 * abs(wag))
        dip = 0.020 * pat * purse + 0.045 * settle - 0.030 * hop * jog
        look = -16.0 * wave + 20.0 * purse - 22.0 * jog * _window(t, 5.8, 7.4, 0.3)
        nod = -6.0 * wave + 9.0 * purse * (1.0 - 0.8 * _window(t, 4.75, 5.3, 0.25)) + 9.0 * pat + 3.0 * hop * jog
        tilt = -9.0 * wave - 3.0 * wag * wave + 5.0 * purse + 3.0 * (knee_left - knee_right) * jog
        row.update({
            ("root", "translation"): (0.012 * lean, -0.10 * dip + 0.020 * tiptoe, 0.0),
            ("root", "scale"): _squash(1.0 + 0.010 * breath - dip + tiptoe),
            ("root", "rotation"): qz(-2.4 * lean),
            ("torso", "rotation"): mul(mul(qx(-3.0 * wave + 4.0 * purse + 4.0 * jog + 0.8 * breath), qy(-8.0 * wave + 10.0 * purse + 7.0 * (knee_left - knee_right) * jog)),
                                       qz(3.6 * lean - 2.0 * wag * wave)),
            ("head", "rotation"): mul(mul(qy(look), qx(nod - 0.6 * breath)), qz(tilt)),
            # the hip she is not standing on lets its leg go a little out; the knees of the jog come up in front
            ("leg-left", "rotation"): mul(qx(-28.0 * knee_left * jog), qz(2.4 * lean + 3.0 * max(0.0, -lean))),
            ("leg-right", "rotation"): mul(qx(-28.0 * knee_right * jog), qz(2.4 * lean - 3.0 * max(0.0, lean))),
        })
        _add(out, t, row)
    return out


def walk():
    """Brisk and bouncy, the hips swinging: a narrow track, the feet almost on one line, a quick
    lift on every step, the shoulders counter-turning against the hips, her arms swinging long
    and loose with the hands flicked out at the end of each swing, her head tipping side to side
    as if she were humming."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.85)
        c = math.cos(phase)
        lift = abs(s) ** 1.1
        sway = math.sin(phase - 0.35)
        _add(out, t, {
            ("root", "translation"): (0.016 * sway, 0.004 + 0.030 * lift, 0.0),
            ("root", "rotation"): mul(mul(qx(2.0), qy(-5.0 * sh)), qz(3.6 * sway)),
            ("root", "scale"): _squash(0.975 + 0.050 * lift),
            ("leg-left", "rotation"): mul(qx(-36.0 * sh), qz(-1.5 - 3.6 * sway)),
            ("leg-right", "rotation"): mul(qx(36.0 * sh), qz(1.5 - 3.6 * sway)),
            ("torso", "rotation"): mul(mul(qx(1.0), qy(12.0 * sh)), qz(-4.5 * sway)),
            ("head", "rotation"): mul(mul(qx(-2.0 + 2.0 * abs(c)), qy(-7.0 * sh)), qz(4.0 * sway)),
            # long loose arms: the swing is from the shoulder, the elbow only gives at the front of it
            ("arm-left", "rotation"): mul(qx(30.0 * sh), qz(-70.0 - 4.0 * sh)),
            ("arm-right", "rotation"): mul(qx(-30.0 * sh), qz(70.0 - 4.0 * sh)),
            ("forearm-left", "rotation"): mul(qx(-8.0), qy(-(10.0 + 26.0 * max(0.0, -sh)))),
            ("forearm-right", "rotation"): mul(qx(-8.0), qy(10.0 + 26.0 * max(0.0, sh))),
        })
    return out


def sprint():
    """A skipping run, light as she is: she is in the air more than she is on the ground. Each
    stride is thrown up as well as forward, her chest only a little ahead of her feet, chin up
    and looking where she is going, the elbows folded tight and pumping high."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.8)
        lift = abs(math.sin(phase - 0.35)) ** 0.9        # the bounce comes late, after the push
        rock = math.sin(phase - 0.3)
        _add(out, t, {
            ("root", "translation"): (0.0, 0.006 + 0.062 * lift, 0.0),
            ("root", "rotation"): mul(qx(9.0), qz(2.2 * rock)),
            ("root", "scale"): _squash(0.95 + 0.11 * lift),
            ("leg-left", "rotation"): mul(qx(-50.0 * sh + 4.0), qz(-1.0 - 2.2 * rock)),
            ("leg-right", "rotation"): mul(qx(50.0 * sh + 4.0), qz(1.0 - 2.2 * rock)),
            ("torso", "rotation"): mul(mul(qx(4.0), qy(13.0 * sh)), qz(-2.5 * rock)),
            ("head", "rotation"): mul(mul(qx(-11.0 + 3.0 * lift), qy(-9.0 * sh)), qz(2.0 * rock)),
            ("arm-left", "rotation"): mul(qx(40.0 * sh - 6.0), qz(-72.0)),
            ("arm-right", "rotation"): mul(qx(-40.0 * sh - 6.0), qz(72.0)),
            ("forearm-left", "rotation"): mul(qx(-10.0), qy(-(88.0 - 16.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-10.0), qy(88.0 + 16.0 * sh)),
        })
    return out


#   arms up: where the left arm points at the top of the jump. Her bunches stand out past her
#   shoulders and behind her ears, so the V is thrown up, out and FORWARD of them.
ARM_UP = ((0.56, 0.68, 0.38), (0.32, 0.94, 0.10))
ARM_UP_REACH = 0.95


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. She goes up like a skipping rope: a quick light stretch off
    both feet, both arms flung up in a V through the elbows, and her heels kicked up behind her
    as she hangs, chin lifted."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.12)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.13)
        ring = math.cos(2.0 * math.pi * t / 0.42) * math.exp(-t / 0.15)
        row = {
            ("root", "scale"): _squash(1.0 + 0.22 * ring),
            ("root", "rotation"): qx(3.0 * hang),
            # both legs the same: straight under her at take-off, then the heels kicked up behind
            ("leg-left", "rotation"): mul(qx(-6.0 * rise + 26.0 * hang), qz(1.0 + 2.0 * hang)),
            ("leg-right", "rotation"): mul(qx(-6.0 * rise + 26.0 * hang), qz(-1.0 - 2.0 * hang)),
            ("torso", "rotation"): qx(-6.0 * rise - 5.0 * hang),
            ("head", "rotation"): qx(-10.0 * rise - 7.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 9.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.86 + 0.14 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: she comes down light and a little surprised, arms up in the V and fluttering from
    the elbows, her feet pedalling under her in small quick steps looking for the ground, her
    eyes on where she will land."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.05 + 0.016 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(3.0), qz(1.6 * s)),
            ("leg-left", "rotation"): mul(qx(-6.0 + 13.0 * s), qz(2.0)),
            ("leg-right", "rotation"): mul(qx(-6.0 - 13.0 * s), qz(-2.0)),
            ("torso", "rotation"): mul(qx(3.0), qz(-1.4 * s)),
            ("head", "rotation"): mul(qx(12.0 + 2.0 * c), qz(-1.5 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(4.0 * s * side))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(11.0 * c * side))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.80 + 0.06 * s * side), 1.0, 1.0)
        _add(out, t, row)
    return out


# ======================================================================================
# THE ACTION CLIPS (2026-10-07). Thirteen of the 28 copied clips, done again for her: the
# copies were made for a body with no elbow. Eight keep the LENGTH of the clip they replace
# and the TIME of its key beat, measured off maring-redesign.glb (the copies of
# character-female-f.glb's) before they were replaced. Five are only looked at in the game
# and never timed against, and were a sixth of a second long, so they are longer now:
#
#   clip                 old      the old key beat                          here
#   holding-right-shoot  0.2000   the arm's one extreme, at 0.067           0.2000, furthest out at 0.067
#   pick-up              0.3333   lowest at 0.167                           0.3333, lowest at 0.167
#   attack-melee-right   0.4167   wound up 0.160, furthest out at 0.256     0.4167, furthest out at 0.250
#   attack-melee-left    0.4167   wound up 0.160, furthest out at 0.256     0.4167, the bump at 0.250
#   interact-right       0.6667   out by 0.167, held to 0.40, home by 0.667 0.6667, the same three times
#   interact-left        0.6667   the same                                  0.6667, the same
#   slide                0.9500   down by 0.133, lowest 0.25, up from 0.60  0.9500, the same; reach 0.333
#   die                  0.3333   in the air at 0.10, down by 0.233         0.3333, the same two times
#   holding-right        0.1667   a held pose, two equal keys               a loop of 2.2 s
#   crouch               0.1667   a held pose, two equal keys               a loop of 1.6 s
#   sit                  0.1667   a held pose, two equal keys               0.8 s of getting down
#   emote-yes            0.6667   a nod: back at 0.167, down at 0.500       a loop of 1.0 s
#   emote-no             0.6667   a shake: 0.167 one way, 0.500 the other   a loop of 1.2 s
#
# THE THROW AND THE TAG ARE HALF THE GAME'S. While they play, code poses the chest, the head
# and both UPPER arms over the clip (CharacterAnimator.ThrowBody.cs, TagBody.cs). What is
# left of the clip is the root, the legs and the two FOREARMS. So in those two she is in the
# root and the legs (the skip, the dart on her toes, the hop away) and each forearm is a plain
# bend of the elbow with no twist, opening to straight at the key beat.
#
# HOW SHE DOES THEM. The runner, light and quick-handed, speed 5 and power 2: nothing of hers
# is heavy. She throws on a SKIP, in the air when the slipper goes. She tags with a dart on
# her toes and is already hopping away backward when it lands. Her shove is her whole side
# thrown in, hip first, and it is she who bounces off. She scoops the slipper up on one leg
# without stopping, slides on her hip, sits side-saddle, and falls flat on her front with her
# heels in the air. WHERE HER FISTS ARE at each clip's key beat, no two the same:
#   carry        right fist out at her waist, flicking the slipper; left fist on her hip
#   throw        (the game's arms) both feet off the ground, legs scissored
#   pick up      right fist on the ground ahead, left flung up behind, one leg out behind her
#   tag          right arm long and straight ahead, left thrown back, up on her toes
#   shove        turned side on: left forearm barred across in front of her, right fist tucked
#   reach right  right fist HIGH ahead of her head, plucking; left on the belt bag
#   reach left   left fist LOW at her knee, tipped over on one leg; right out wide for balance
#   slide        down on her left hip: left fist on the ground behind, right long past her feet
#   breathless   folded over: left fist on her knee, right pressed into the stitch in her side
#   sit          side-saddle: left fist on the ground beside her, right on her lap
#   knocked down flat on her front, both arms flopped out past her head, heels up
#   yes          both fists up and wide in a W, off the ground
#   no           both forearms up in front of her, wiping side to side, leaning away
# ======================================================================================
HIP = (0.0836, 0.17625, -0.02875)     # the leg bones, in the root's space; the sole is LEG below
LEG = 0.17625
LEGS_STAND = (qx(0.0), qx(0.0))       # the idle's first frame: straight under her


def _inv(q):
    return (-q[0], -q[1], -q[2], q[3])


def _lerp(a, b, t):
    return tuple(p + (q - p) * t for p, q in zip(a, b))


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
        out[key] = _mix(va, vb, t) if key[1] == "rotation" else _lerp(va, vb, t)
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


#   THE CARRY. The tsinelas rides in her right fist out beside her waist, the forearm level,
#   where she can flick it; her left fist is on her hip and her weight on that hip.
ARM_TRAY = ((0.62, -0.76, 0.12), (0.62, 0.10, 0.78))
ARM_FLICK = ((0.66, -0.66, 0.26), (0.50, 0.74, 0.45))
TRAY_REACH = 0.30


def _carry(flick=0.0, w=1.0, sy=1.0, look=0.0):
    """`w` is which hip her weight is on (1 her left, -1 her right); `flick` tosses the slipper."""
    arm = (_lerp(ARM_TRAY[0], ARM_FLICK[0], flick), _lerp(ARM_TRAY[1], ARM_FLICK[1], flick))
    return _pose(tx=0.014 * w, root=qz(-3.5 * w), sy=sy, torso=mul(qy(-6.0), qz(5.0 * w)),
                 head=mul(mul(qy(look), qx(-5.0 * flick)), qz(-5.0 * w)),
                 left=ARM_HIP, right=arm, reach=(0.0, TRAY_REACH + 0.25 * flick),
                 legs=(qz(3.5 * w + 2.5), qz(3.5 * w - 2.5)))


def holding_right():
    """Carrying the tsinelas, a loop of 2.2 s: one fist on her hip and the slipper out at her
    waist in the other. Twice (0.50 s and 0.84 s) she flicks it up off her forearm with a
    little bob, the way she bounces a coin; then her weight goes over to the other hip and
    comes back, her head tipping with it. She is never square and never still."""
    length = 2.2
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        flick = min(1.0, _bump(t, 0.50, 0.075) + _bump(t, 0.84, 0.075))
        dip = _bump(t, 0.40, 0.07) + _bump(t, 0.74, 0.07)
        w = 1.0 - 2.0 * _window(t, 1.10, 2.02, 0.42)
        _add(out, t, _carry(flick=flick, w=w, sy=1.0 + 0.010 * math.sin(2.0 * p) - 0.05 * dip + 0.04 * flick,
                            look=-9.0 * math.sin(p)))
    return out


def holding_right_shoot():
    """THE THROW, what is hers of it (the game points her chest and upper arms). She throws
    on a SKIP: a quick sink with the front knee coming up, then she is OFF the ground,
    stretched, legs scissored, the elbow snapping straight (0.067 s); she comes down on her
    front toe with the back heel kicked up behind her, and drops into the carry."""
    cocked = _pose(sy=0.92, root=qy(-9.0), torso=qx(-3.0), head=qy(9.0), space="world",
                   left=((0.30, -0.35, 0.89), (0.05, 0.25, 0.97)),
                   right=((0.55, -0.50, -0.67), (0.45, -0.10, -0.89)), reach=(0.0, 0.2),
                   legs=(mul(qx(-24.0), qz(3.0)), mul(qx(4.0), qz(-3.0))))
    out = _pose(tz=0.040, lift=0.040, sy=1.11, root=mul(qx(7.0), qy(10.0)), torso=qx(3.0),
                head=mul(qy(-10.0), qx(-8.0)), space="world",
                left=((0.55, -0.40, -0.73), (0.40, 0.30, -0.87)),
                right=((0.12, -0.22, 0.97), (0.0, -0.10, 1.0)), reach=(0.0, 0.80),
                legs=(mul(qx(-30.0), qz(3.0)), mul(qx(27.0), qz(-3.0))))
    through = _pose(tz=0.050, sy=0.93, root=mul(qx(11.0), qy(14.0)), torso=qx(5.0),
                    head=mul(qy(-14.0), qx(-12.0)), space="world",
                    left=((0.60, -0.35, -0.72), (0.50, 0.40, -0.77)),
                    right=((-0.20, -0.30, 0.93), (-0.50, 0.10, 0.86)), reach=(0.0, 0.35),
                    legs=(mul(qx(-11.0), qz(3.0)), mul(qx(46.0), qz(-3.0))))
    _elbows(cocked, (35.0, 30.0), (70.0, 40.0))
    _elbows(out, (48.0, 10.0), (3.0, 0.0))
    _elbows(through, (55.0, 20.0), (24.0, 20.0))
    return _act(0.2, [(0.0, _carry(), "lin"), (1.0 / 30.0, cocked, "out"), (2.0 / 30.0, out, "in"),
                      (0.117, through, "out"), (0.2, _carry(), "smooth")])


def pick_up():
    """She does not stop for it: she tips over on one leg like a bird drinking, the other leg
    going out straight behind her as her head goes down, her right fist sweeping the ground
    and her left flung up behind for balance (lowest at 0.167 s); then she swings back up
    onto her toes with the slipper held up to look at, and settles."""
    ready = _pose(sy=1.05, lift=0.008, torso=qx(-3.0), left=((0.70, -0.60, -0.20), (0.60, -0.50, 0.30)),
                  right=((0.60, -0.50, 0.40), (0.30, -0.30, 0.80)))

    def low(tip, sy, grab):
        return _pose(tz=0.020, sy=sy, root=qx(tip), torso=mul(qx(26.0), qy(10.0)), head=mul(qy(-6.0), qx(-34.0)), space="world",
                     left=((0.62, 0.38, -0.69), (0.42, 0.72, -0.55)),
                     right=((0.12, -0.80, 0.58), (-0.06, -0.97, 0.22)), reach=(0.35, 0.45 + grab),
                     legs=(qx(-tip), mul(qx(48.0), qz(-4.0))))

    up = _pose(sy=1.07, lift=0.010, torso=qx(-5.0), head=mul(qy(-12.0), qx(-8.0)),
               left=((0.72, -0.62, -0.10), (0.62, -0.50, 0.30)),
               right=((0.52, -0.30, 0.80), (0.20, 0.80, 0.56)), reach=(0.0, 0.45))
    return _act(1.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 30.0, ready, "out"), (1.0 / 6.0, low(44.0, 0.94, 0.15), "in"),
                            (0.2, low(40.0, 0.97, 0.0), "out"), (0.283, up, "smooth"), (1.0 / 3.0, _stand(), "smooth")])


def attack_melee_right():
    """THE TAG, what is hers of it (the game takes her chest, head and upper arms to its own
    reach). A touch and gone: she sinks a hair, darts up onto her toes with the forearm
    opening long and straight (0.25 s), and before it has finished landing she is hopping
    AWAY backward off the ground with both hands thrown up, not it; she lands light."""
    wind = _pose(tz=-0.012, sy=0.92, root=qy(-8.0), torso=qx(-2.0), head=qy(8.0),
                 left=((0.50, -0.60, 0.62), (0.0, 0.40, 0.92)),
                 right=((0.60, -0.50, -0.62), (0.30, 0.30, 0.90)),
                 legs=(mul(qx(-6.0), qz(3.0)), mul(qx(6.0), qz(-3.0))))
    _elbows(wind, (50.0, 30.0), (62.0, 25.0))

    def hit(sy, lean, reach):
        row = _pose(tz=0.040, lift=0.012, sy=sy, root=mul(qx(lean), qy(10.0)), torso=qx(0.4 * lean),
                    head=mul(qy(-10.0), qx(-lean)), space="world",
                    left=((0.66, 0.10, -0.74), (0.50, 0.56, -0.66)),
                    right=((0.06, 0.04, 1.0), (0.0, 0.02, 1.0)), reach=(0.3, reach),
                    legs=(mul(qx(-lean - 4.0), qz(2.0)), mul(qx(16.0 - 0.5 * lean), qz(-2.0))))
        return _elbows(row, (55.0, 30.0), (2.0, 0.0))

    away = _pose(tz=-0.030, lift=0.035, sy=1.06, root=qx(-6.0), torso=qx(-3.0), head=qx(4.0),
                 left=((0.80, -0.30, 0.50), (0.35, 0.86, 0.36)), right=((0.80, -0.30, 0.50), (0.35, 0.86, 0.36)),
                 reach=(0.3, 0.3), legs=(mul(qx(-14.0), qz(4.0)), mul(qx(-8.0), qz(-4.0))))
    _elbows(away, (80.0, 70.0), (80.0, 70.0))
    return _act(5.0 / 12.0, [(0.0, _stand(), "lin"), (0.12, wind, "out"), (0.25, hit(1.09, 18.0, 1.00), "in"),
                             (0.283, hit(1.02, 20.0, 0.75), "out"), (0.35, away, "out"), (5.0 / 12.0, _stand(), "in")])


def attack_melee_left():
    """THE SHOVE, and she has no weight to shove with, so she throws ALL of it: she turns
    side on, left shoulder first, her left forearm barred across in front of her and her
    right fist tucked, and bumps you hip and shoulder with both feet off the ground
    (0.25 s). It is she who bounces off: back and up, arms flying, and down onto her feet."""
    wind = _pose(tz=-0.030, sy=0.88, root=mul(qz(4.0), qy(-24.0)), torso=qx(-2.0), head=qy(22.0),
                 left=((0.55, -0.60, 0.58), (-0.50, 0.30, 0.81)), right=((0.60, -0.70, -0.38), (0.30, 0.20, 0.93)),
                 legs=(mul(qx(-8.0), qz(4.0)), mul(qx(-8.0), qz(-4.0))))

    def bump(tz, lift, sy, tip):
        return _pose(tz=tz, lift=lift, sy=sy, root=mul(qx(tip), qy(-58.0)), torso=qz(-6.0), head=mul(qy(46.0), qx(-4.0)),
                     space="world", left=((0.16, 0.04, 0.99), (0.0, 0.90, 0.44)), right=((0.30, -0.25, -0.92), (0.16, 0.40, -0.90)),
                     reach=(0.55, 0.35), legs=(mul(qx(-24.0), qz(10.0)), mul(qx(26.0), qz(-16.0))))

    off = _pose(tz=-0.020, lift=0.030, sy=1.05, root=mul(qx(-9.0), qy(-20.0)), torso=qx(-4.0), head=mul(qy(16.0), qx(6.0)),
                left=((0.86, 0.10, 0.50), (0.50, 0.80, 0.33)), right=((0.86, 0.10, 0.50), (0.50, 0.80, 0.33)),
                reach=(0.3, 0.3), legs=(mul(qx(-16.0), qz(6.0)), mul(qx(-10.0), qz(-6.0))))
    return _act(5.0 / 12.0, [(0.0, _stand(), "lin"), (0.14, wind, "out"), (0.25, bump(0.095, 0.026, 1.07, 20.0), "in"),
                             (0.283, bump(0.090, 0.008, 0.94, 14.0), "out"), (0.355, off, "out"), (5.0 / 12.0, _stand(), "in")])


#   THE PLUCK. Her right fist up ahead of her head, as if at fruit hung over a stall.
ARM_PLUCK = ((0.42, 0.30, 0.86), (0.10, 0.80, 0.59))
ARM_PLUCKED = ((0.46, 0.06, 0.89), (0.12, 0.42, 0.90))


def _pluck(take):
    """Up on her toes with the right fist high ahead (`take` 0), or snatched down a hand (1)."""
    arm = (_lerp(ARM_PLUCK[0], ARM_PLUCKED[0], take), _lerp(ARM_PLUCK[1], ARM_PLUCKED[1], take))
    return _pose(sy=1.07 - 0.07 * take, lift=0.014 * (1.0 - take), tz=0.012, torso=mul(qx(-4.0 + 5.0 * take), qy(12.0)),
                 head=mul(qy(-12.0), qx(-16.0 + 12.0 * take)), left=ARM_BAG_LEFT, right=arm,
                 reach=(0.25, 0.75 - 0.30 * take), legs=(qz(2.0), qz(-2.0)))


def interact_right():
    """Quick hands: she goes up on her toes and PLUCKS, her right fist high ahead of her head,
    snatching down twice like taking two mangoes off a string, her left hand already holding
    her belt bag open for them. Out at 0.167 s, the second snatch at 0.40, home by 0.667."""
    return _act(2.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 6.0, _pluck(0.0), "out"), (0.235, _pluck(1.0), "in"),
                            (0.32, _pluck(0.0), "out"), (0.40, _pluck(1.0), "in"), (0.47, _pluck(0.7), "out"),
                            (2.0 / 3.0, _stand(), "smooth")])


def interact_left():
    """The other hand goes LOW: she tips over sideways onto her left leg, the right leg
    swinging out off the ground and her right arm out wide to balance it, and pats the thing
    twice down by her knee, her head on one side to look. Out at 0.167 s, held to 0.40."""
    def pat(down):
        tip = 15.0 + 3.0 * down
        return _pose(tx=0.020, sy=0.97 - 0.03 * down, root=qz(-tip), torso=mul(qx(20.0 + 6.0 * down), qy(16.0)),
                     head=mul(mul(qy(10.0), qx(-10.0)), qz(10.0)), space="world",
                     left=((0.50, -0.72, 0.48), (0.22, -0.80 - 0.15 * down, 0.56)),
                     right=((0.92, 0.36, -0.10), (0.70, 0.70, 0.10)), reach=(0.45 + 0.20 * down, 0.35),
                     legs=(qz(tip), qz(-8.0)))

    return _act(2.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 6.0, pat(1.0), "in"), (0.235, pat(0.0), "out"),
                            (0.31, pat(1.0), "in"), (0.40, pat(0.3), "out"), (2.0 / 3.0, _stand(), "smooth")])


def slide():
    """The retrieval slide, on her HIP like a girl stealing second: a skip, then she is down
    on her left side leaning back on her left fist, the low leg shot out straight and the top
    one hooked up over it, her right arm going out long past her feet for the slipper; she
    rolls up over her feet and is standing with a bounce, already going.
    Down by 0.133 s, lowest at 0.25, the reach furthest at 0.333, on her feet from 0.60."""
    skip = _pose(sy=1.06, lift=0.022, root=qx(-6.0), torso=qx(-3.0), head=qx(-3.0),
                 left=((0.80, -0.30, -0.30), (0.60, 0.30, 0.50)), right=((0.70, -0.30, 0.50), (0.40, 0.50, 0.70)),
                 legs=(mul(qx(-20.0), qz(2.0)), mul(qx(8.0), qz(-2.0))))

    def down(back, roll, sy, chest, reach):
        return _pose(tz=0.090, lift=0.040, sy=sy, root=mul(qz(-roll), qx(-back)), torso=mul(qx(chest), qy(10.0)),
                     head=mul(mul(qy(-6.0), qx(back - chest - 6.0)), qz(0.6 * roll)), space="world",
                     left=((0.70, -0.62, -0.36), (0.30, -0.92, -0.26)),
                     right=((0.30, 0.10, 0.95), (0.06, -0.05, 1.0)), reach=(0.50, reach),
                     legs=(mul(qy(8.0), qx(-(88.0 - back))), mul(qy(-12.0), qx(-(56.0 - back)))))

    squat = _pose(sy=0.90, tz=0.040, torso=qx(16.0), head=qx(-10.0), space="root",
                  left=((0.80, -0.50, 0.30), (0.50, -0.10, 0.86)), right=((0.80, -0.50, 0.30), (0.50, -0.10, 0.86)),
                  legs=(mul(qx(-10.0), qz(20.0)), mul(qx(-10.0), qz(-20.0))))
    up = _pose(sy=1.07, lift=0.020, torso=qx(-4.0), head=qx(-5.0), left=((0.80, -0.50, 0.20), (0.60, 0.10, 0.60)),
               right=((0.80, -0.50, 0.20), (0.60, 0.10, 0.60)))
    settle = _pose(sy=0.97)
    return _act(0.95, [(0.0, _stand(), "lin"), (0.06, skip, "out"), (0.133, down(36.0, 28.0, 0.97, 12.0, 0.30), "in"),
                       (0.25, down(54.0, 46.0, 0.91, 14.0, 0.55), "out"), (1.0 / 3.0, down(50.0, 42.0, 0.94, 22.0, 1.00), "smooth"),
                       (0.39, down(40.0, 30.0, 0.96, 24.0, 0.70), "smooth"), (0.50, squat, "smooth"), (0.62, up, "smooth"),
                       (0.74, settle, "smooth"), (0.84, _stand(), "smooth"), (0.95, _stand(), "lin")])


def crouch():
    """Out of breath, a loop of 1.6 s, and she has a STITCH: folded over with her left fist on
    her knee and her right pressed into her side, her weight rocking from one foot to the
    other because even now she cannot keep still. Three quick breaths, her chin coming up for
    each one; on the third she takes the hand off her side and fans her face with it.
    Folded over on every frame, so it can be held on its last."""
    length = 1.6
    out = {}
    side = ((0.80, -0.50, -0.33), (-0.52, -0.40, 0.75))
    fan = ((0.60, -0.36, 0.71), (-0.25, 0.84, 0.48))
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        air = 0.5 - 0.5 * math.cos(3.0 * p)                # 0 emptied, 1 full
        rock = math.sin(p)
        fanning = _window(t, 0.92, 1.50, 0.16)
        wag = math.sin(2.0 * math.pi * (t - 0.92) / 0.16)
        right = (_lerp(side[0], fan[0], fanning), _lerp(side[1], (fan[1][0] + 0.45 * wag, fan[1][1], fan[1][2]), fanning))
        _add(out, t, _pose(tx=0.012 * rock, tz=-0.030, sy=0.94 + 0.04 * air, root=qz(-2.5 * rock),
                           torso=mul(qx(44.0 - 10.0 * air), qz(3.0 * rock)),
                           head=mul(qx(4.0 - 26.0 * air - 8.0 * fanning), qz(-6.0 * rock + 8.0 * fanning)), space="root",
                           left=((0.60, -0.74 + 0.16 * air, 0.30), (-0.06, -0.98, -0.16)), right=right,
                           reach=(0.25 + 0.20 * air, 0.10 + 0.45 * fanning),
                           legs=(mul(qx(-12.0), qz(9.0 + 2.5 * rock)), mul(qx(-12.0), qz(-9.0 + 2.5 * rock)))))
    return out


ARM_PROP = ((0.74, -0.62, -0.26), (0.30, -0.94, 0.10))      # her left fist on the ground beside her
ARM_LAP = ((0.40, -0.72, 0.57), (-0.42, -0.50, 0.76))       # her right fist on her lap
ARM_OUT = ((0.90, 0.20, 0.38), (0.72, 0.62, 0.30))          # thrown out wide, for the way down


def _saddle(sy=1.0, back=8.0, roll=11.0, swing=1.0, kick=80.0, up=0.0, arms=None, reach=(0.40, 0.25), chin=6.0):
    """Sat side-saddle: leaning on her left fist, both legs laid over to her right (`swing` 0
    has them straight ahead), `kick` degrees forward of her."""
    left, right = arms or (ARM_PROP, ARM_LAP)
    return _pose(root=mul(qz(-roll), qx(-back)), sy=sy, lift=0.042 * sy + up, torso=mul(qx(-3.0), qy(-10.0 * swing)),
                 head=mul(mul(qy(-12.0 * swing), qx(chin)), qz(roll)), space="world", left=left, right=right, reach=reach,
                 legs=(mul(qy(-34.0 * swing), qx(-kick)), mul(qy(-52.0 * swing), qx(-kick + 4.0))))


def sit():
    """She sits the way she would on the kerb with her friends, 0.8 s: up on her toes with her
    arms out, her feet swept out from under her to one side, down onto her seat lightly with
    one small bounce (lands at 0.36 s), and she ends side-saddle, leaning on her left fist
    with both legs laid over to her right, the other hand on her lap, head tipped. The last
    frame is held."""
    toes = _pose(sy=1.07, lift=0.020, torso=qx(-3.0), head=qx(-5.0), left=ARM_OUT, right=ARM_OUT, reach=(0.2, 0.2),
                 legs=(mul(qx(-8.0), qz(3.0)), mul(qx(-8.0), qz(-3.0))))
    wide = (ARM_OUT, ARM_OUT)
    return _act(0.8, [(0.0, _stand(), "lin"), (0.10, toes, "out"),
                      (0.24, _saddle(sy=1.04, back=2.0, roll=3.0, swing=0.3, kick=48.0, up=0.060, arms=wide, reach=(0.2, 0.2), chin=0.0), "smooth"),
                      (0.36, _saddle(sy=0.84, back=4.0, roll=7.0, swing=0.8, kick=84.0, arms=wide, reach=(0.1, 0.1), chin=14.0), "in"),
                      (0.47, _saddle(sy=1.05, back=6.0, roll=9.0, swing=1.0, kick=72.0, up=0.016, chin=0.0), "out"),
                      (0.61, _saddle(sy=0.96, back=9.0, roll=12.0, chin=9.0), "smooth"),
                      (0.80, _saddle(), "smooth")])


def die():
    """Knocked down, and she goes over FORWARD like someone tripped mid-run: her feet go out
    from under her behind, arms thrown ahead (in the air at 0.10 s), she lands flat on her
    front (0.233 s) with a bounce, and lies there with her arms flopped out past her head,
    her chin on the street and both heels up in the air behind her, one higher than the
    other. The last frame is held."""
    ahead = ((0.70, 0.45, 0.55), (0.40, 0.80, 0.45))
    trip = _pose(lift=0.050, ground=False, sy=1.09, root=qx(14.0), torso=qx(-6.0), head=qx(-18.0),
                 left=ahead, right=ahead, reach=(0.3, 0.3), legs=(mul(qx(26.0), qz(5.0)), mul(qx(12.0), qz(-5.0))))
    over = _pose(lift=0.105, ground=False, sy=1.04, root=qx(52.0), torso=qx(-6.0), head=qx(-30.0),
                 left=((0.70, 0.66, 0.26), (0.40, 0.90, 0.16)), right=((0.70, 0.66, 0.26), (0.40, 0.90, 0.16)),
                 reach=(0.4, 0.4), legs=(mul(qx(38.0), qz(8.0)), mul(qx(20.0), qz(-8.0))))
    flop = ((0.92, 0.38, 0.04), (0.74, 0.67, 0.04))

    def flat(lift, scale, chin, kick_left, kick_right, tilt=0.0):
        return _pose(lift=lift, ground=False, scale=scale, root=qx(90.0), head=mul(qx(-chin), qz(tilt)),
                     left=flop, right=flop, reach=(0.35, 0.35),
                     legs=(mul(qx(kick_left), qz(7.0)), mul(qx(kick_right), qz(-7.0))))

    return _act(1.0 / 3.0, [(0.0, _stand(), "lin"), (0.05, trip, "out"), (0.117, over, "lin"),
                            (0.233, flat(0.085, (1.10, 1.05, 0.80), 30.0, 64.0, 50.0), "in"),
                            (0.283, flat(0.125, (0.97, 1.0, 1.06), 44.0, 22.0, 36.0), "out"),
                            (1.0 / 3.0, flat(0.100, (1.0, 1.0, 1.0), 36.0, 40.0, 68.0, 10.0), "in")])


ARM_W = ((0.86, 0.36, 0.36), (0.22, 0.96, 0.18))            # a fist up and wide, over her bunches
ARM_YANK = ((0.74, -0.50, 0.45), (0.42, 0.70, 0.58))        # and pulled down to her ribs


def emote_yes():
    """YES, and READY: she cannot say it standing still. A loop of 1 s, two bounces: she sinks
    with both fists pulled in at her ribs, and springs off the ground with them thrown up and
    wide in a W, one heel kicked up behind her, chin up (0.26 s, hung to 0.38); lands, and
    goes again with the other heel (0.76 s). The fists are never lower than her ribs."""
    def up(heel):
        kick = (qx(34.0), qx(-6.0)) if heel > 0 else (qx(-6.0), qx(34.0))
        return _pose(sy=1.09, lift=0.045, root=qz(-4.0 * heel), torso=mul(qx(-6.0), qz(4.0 * heel)),
                     head=mul(qx(-14.0), qz(-7.0 * heel)), left=ARM_W, right=ARM_W, reach=(1.30, 1.30), legs=kick)

    def low(sy, nod, heel):
        return _pose(sy=sy, root=qz(2.0 * heel), torso=qx(0.5 * nod), head=mul(qx(nod), qz(4.0 * heel)),
                     left=ARM_YANK, right=ARM_YANK, reach=(0.20, 0.20), legs=(qz(4.0), qz(-4.0)))

    hang = lambda heel: _blend(up(heel), low(1.0, 0.0, heel), 0.12)
    return _act(1.0, [(0.0, low(0.97, 4.0, 1.0), "lin"), (0.12, low(0.86, 12.0, 1.0), "out"), (0.26, up(1.0), "out"),
                      (0.38, hang(1.0), "smooth"), (0.50, low(0.97, 4.0, -1.0), "in"), (0.62, low(0.86, 12.0, -1.0), "out"),
                      (0.76, up(-1.0), "out"), (0.88, hang(-1.0), "smooth"), (1.0, low(0.97, 4.0, 1.0), "in")])


def emote_no():
    """NO, a loop of 1.2 s, and it is the one she talks her way out of tags with: not me, not
    me. She leans away from you with both forearms up in front of her, and wipes them side to
    side together, twice over and back, wide enough that a hand shows past her on each side
    from behind; her hips go the other way under them and her head shakes against the hands."""
    length = 1.2
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        s = _snap(math.sin(2.0 * p), 0.7)
        bob = abs(math.cos(2.0 * p))
        arms = []
        for side in (1.0, -1.0):
            x = 0.30 + 0.80 * s * side          # out from her own side, so the two go the same way
            arms.append(((x, -0.26, 0.86), (x + 0.25 * side * s, 0.50, 0.66)))
        _add(out, t, _pose(tx=-0.028 * s, tz=-0.018, sy=0.97 + 0.03 * bob, root=mul(qx(-5.0), qz(3.0 * s)),
                           torso=mul(mul(qx(-8.0), qy(11.0 * s)), qz(-5.0 * s)),
                           head=mul(qy(-34.0 * s), qx(4.0)), left=arms[0], right=arms[1], reach=(0.60 + 0.35 * max(0.0, s), 0.60 + 0.35 * max(0.0, -s)),
                           legs=(mul(qx(-5.0), qz(3.0 - 3.0 * s)), mul(qx(-5.0), qz(-3.0 - 3.0 * s)))))
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
         "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
         "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
         "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
         "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no}
