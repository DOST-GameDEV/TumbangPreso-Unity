"""The clips of the Payo audience v2 GIRL.

Imported by tools/author_payo_audience_v2_girl.py, which writes them into her .glb IN PLACE OF
the clips of the same name copied from character-female-c.glb, and APPENDS the two audience
clips the base rig has not got (`cheer`, `clap`). A copy of the approved Bebang redesign's clips
script (the audience brief: "the Classic base's versions rewritten lightly for this person are
fine"); `idle`, `sit`, `emote-yes` and the two new ones are hers, written below.

WHAT HER MOTION SAYS ABOUT HER. A village girl of about nine at the edge of the crowd: SHY BUT
EXCITED. She holds her own elbows, keeps her feet together with the toes turned in, rocks on her
heels and peeks round the grown-ups; when something happens her hands fly up under her chin and
she claps, quick and small. Nothing is big: Bebang punches her palm and rolls her throwing arm,
this girl makes herself small and lets the excitement leak out of her in bounces.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle tips a body forward and swings a
hanging limb BACK; about +z a positive angle leans her to her right; about +y a positive angle
turns her face to her left.

ARMS GO THROUGH THE ELBOW. Her elbow is 82 mm from the shoulder; the forearm and fist ride the
forearm bone and may stretch (scale on the forearm bone) to reach her chin or her lap.

walk, sprint, jump, fall and the thirteen action clips are Bebang's, unchanged in their numbers:
the same rig and the same arm, and a crowd villager does not run the match. NOT SEEN IN UNITY.
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
#   left, +y up, +z ahead). The right is the mirror.
#   HANGING (the stand the action clips start from): elbows a little out, fists forward of her hips.
ARMS_HANG = ((0.66, -0.75, 0.02), (0.58, -0.79, 0.20))
#   HOLDING HER ELBOWS: her stance. The forearms cross low in front of her tummy, each hand on the
#   other arm near its elbow, the left over the right. Shoulders a little up.
ARM_HOLD_L = ((0.30, -0.90, 0.32), (-0.88, 0.08, 0.46))
ARM_HOLD_R = ((0.30, -0.92, 0.26), (-0.92, 0.00, 0.40))
HOLD_REACH = 0.40
#   HANDS UNDER HER CHIN: fists together just below her jaw, elbows tucked in.
ARM_CHIN = ((0.24, -0.72, 0.65), (-0.52, 0.78, 0.36))
CHIN_REACH = 0.34
IDLE_LENGTH = 8.0
#   her feet together, the toes turned IN
FEET_IN = (mul(qy(-7.0), qz(1.5)), mul(qy(7.0), qz(-1.5)))


def _hold(rock=0.0, lift=0.0, sy=1.0, torso=None, head=None, root=None, chin=0.0, bounce=0.0):
    """Her stance, holding her elbows, blended toward the hands under her chin by `chin`."""
    left = tuple(tuple(a + (b - a) * chin for a, b in zip(p, q)) for p, q in zip(ARM_HOLD_L, ARM_CHIN))
    right = tuple(tuple(a + (b - a) * chin for a, b in zip(p, q)) for p, q in zip(ARM_HOLD_R, ARM_CHIN))
    reach = HOLD_REACH + (CHIN_REACH - HOLD_REACH) * chin
    root = mul(root or qx(0.0), qx(rock))
    return _pose(lift=lift + bounce, sy=sy, root=root, torso=torso, head=head, left=left, right=right,
                 reach=(reach, reach - 0.02), legs=FEET_IN)


def idle():
    """Eight seconds of a shy girl watching a match, in three things she does. Frame 0 is her
    stance, holding her elbows, so the baked `stand` pose is already her.

      HEELS (0 to 2.9 s). Holding her elbows, she rocks back on her heels and up onto her toes,
      twice, slow, her head tipping the other way each time, watching.
      THE PEEK (3.0 to 4.9 s). Something is happening behind a grown-up: she leans out to her
      right up on her toes, head turned and tipped round the person in front, holds it, and
      ducks back.
      HANDS UNDER HER CHIN (5.0 to 7.4 s). Whatever she saw was good: both hands fly up under her
      chin, fists together, and she does three quick little bounces on her heels, shoulders up,
      then sinks back to holding her elbows, a little embarrassed, and one last rock.

    WHY THESE. A child at the edge of a crowd, too shy to push forward and too excited to keep
    still. None of the three is another's: Bebang punches her palm, does her headband and rolls
    her shoulder; the heroes fold arms, flex, hop. Her feet stay planted, together, toes in.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 4.0))
        rocking = _window(t, 0.0, 2.9, 0.35) + _window(t, 7.2, 8.0, 0.30)
        rock = 5.0 * math.sin(2.0 * math.pi * (t - 0.3) / 1.3) * rocking
        peek = _window(t, 3.0, 4.9, 0.45)
        chin = _window(t, 5.0, 7.4, 0.25)
        hops = sum(_bump(t, at, 0.07) for at in (5.55, 5.95, 6.35))
        sy = 1.0 + 0.008 * breath - 0.035 * hops * chin
        lift = 0.004 * max(0.0, math.sin(2.0 * math.pi * (t - 0.3) / 1.3)) * rocking + 0.012 * peek + 0.020 * hops * chin
        torso = mul(mul(qx(2.0 + 0.6 * breath + 4.0 * peek - 3.0 * chin), qz(6.0 * peek)), qy(-6.0 * peek))
        look = -26.0 * peek + 8.0 * math.sin(2.0 * math.pi * t / 2.6) * rocking
        head = mul(mul(qy(look), qx(4.0 - 0.6 * rock - 6.0 * chin + 5.0 * hops * chin)), qz(10.0 * peek + 3.0 * chin))
        root = qz(5.0 * peek)
        _add(out, t, _hold(rock=rock, lift=lift, sy=sy, torso=torso, head=head, root=root, chin=chin))
    return out


def _stand():
    """The stand the action clips begin and end on: arms hanging, feet together."""
    return _pose()


def walk():
    """A stomp with a spring in it: feet set wide, short strides, each one landing with a squash
    and bouncing back up off the ball of the foot; the shoulders swing as one block and her
    fists pump low in front of her, elbows bent."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.75)
        c = math.cos(phase)
        lift = abs(s) ** 1.3                      # a quick push up and a hard landing
        sway = math.sin(phase - 0.25)
        pump_left = max(0.0, -sh)                 # an arm comes forward as its own leg goes back
        pump_right = max(0.0, sh)
        _add(out, t, {
            ("root", "translation"): (0.012 * sway, 0.002 + 0.034 * lift, 0.0),
            ("root", "rotation"): mul(qx(3.0), qz(2.6 * sway)),
            ("root", "scale"): _squash(0.955 + 0.075 * lift),
            ("leg-left", "rotation"): mul(qx(-33.0 * sh), qz(5.0 - 2.6 * sway)),
            ("leg-right", "rotation"): mul(qx(33.0 * sh), qz(-5.0 - 2.6 * sway)),
            ("torso", "rotation"): mul(mul(qx(2.0), qy(9.0 * sh)), qz(-3.0 * sway)),
            ("head", "rotation"): mul(mul(qx(-3.0 + 2.5 * abs(c)), qy(-6.0 * sh)), qz(1.5 * sway)),
            ("arm-left", "rotation"): mul(qx(4.0 + 22.0 * sh), qz(-58.0)),
            ("arm-right", "rotation"): mul(qx(4.0 - 22.0 * sh), qz(58.0)),
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(34.0 + 22.0 * pump_left))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(34.0 + 22.0 * pump_right)),
        })
    return out


def sprint():
    """She runs like she means to go through whatever is at the end of it: chest forward, chin
    level, elbows locked square and the fists driving past her hips, the feet hitting hard."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.7)
        lift = abs(s) ** 1.2
        rock = math.sin(phase - 0.3)
        _add(out, t, {
            ("root", "translation"): (0.0, 0.004 + 0.046 * lift, 0.0),
            ("root", "rotation"): mul(qx(13.0), qz(1.5 * rock)),
            ("root", "scale"): _squash(0.94 + 0.11 * lift),
            ("leg-left", "rotation"): mul(qx(-54.0 * sh), qz(3.0 - 1.5 * rock)),
            ("leg-right", "rotation"): mul(qx(54.0 * sh), qz(-3.0 - 1.5 * rock)),
            ("torso", "rotation"): mul(mul(qx(6.0), qy(11.0 * sh)), qz(-2.0 * rock)),
            ("head", "rotation"): mul(mul(qx(-15.0 + 2.0 * lift), qy(-8.0 * sh)), qz(1.0 * rock)),
            # the upper arms swing hard from the shoulder, the elbows stay folded square
            ("arm-left", "rotation"): mul(qx(48.0 * sh), qz(-66.0)),
            ("arm-right", "rotation"): mul(qx(-48.0 * sh), qz(66.0)),
            ("forearm-left", "rotation"): mul(qx(-14.0), qy(-(74.0 - 10.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-14.0), qy(74.0 + 10.0 * sh)),
        })
    return out


#   arms up: where the left arm points at the top of the jump. Her ears and her headband stand
#   out past her shoulders, so the V is thrown up and out, clear of them.
ARM_UP = ((0.80, 0.36, 0.16), (0.34, 0.93, 0.10))
ARM_UP_REACH = 0.72


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. She goes up like a thrown brick: one hard stretch off both
    feet, both arms punched up in a V through the elbows, her knees coming apart under her as
    she hangs, the heavy bun tipping her head back for a moment."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.12)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.13)
        ring = math.cos(2.0 * math.pi * t / 0.46) * math.exp(-t / 0.15)
        row = {
            ("root", "scale"): _squash(1.0 + 0.20 * ring),
            ("root", "rotation"): qx(-2.0 * hang),
            # both legs the same: straight under her at take-off, then drawn up and apart
            ("leg-left", "rotation"): mul(qx(10.0 * rise - 16.0 * hang), qz(2.0 + 9.0 * hang)),
            ("leg-right", "rotation"): mul(qx(10.0 * rise - 16.0 * hang), qz(-2.0 - 9.0 * hang)),
            ("torso", "rotation"): qx(-7.0 * rise - 2.0 * hang),
            ("head", "rotation"): qx(-13.0 * rise + 4.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 8.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.70 + 0.30 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: she comes down square and braced, knees apart, arms up in the V and hardly
    moving, eyes on the ground she is about to hit. She does not flail; the bun and the skirt
    do the fluttering for her."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.04 + 0.014 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(4.0), qz(1.2 * s)),
            ("leg-left", "rotation"): mul(qx(-12.0 + 4.0 * s), qz(10.0)),
            ("leg-right", "rotation"): mul(qx(-12.0 - 4.0 * s), qz(-10.0)),
            ("torso", "rotation"): mul(qx(5.0), qz(-1.0 * s)),
            ("head", "rotation"): mul(qx(14.0 + 2.0 * c), qz(-0.8 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(3.0 * s))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(4.0 * c))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.66 + 0.05 * s * side), 1.0, 1.0)
        _add(out, t, row)
    return out


# ======================================================================================
# THE ACTION CLIPS (2026-10-07). Thirteen clips that were copies of the old shared rig's,
# made for a body with no elbow. Eight keep the LENGTH of the clip they replace and the TIME
# of its key beat, measured off character-female-c.glb before it was replaced. Five are only
# looked at in the game and never timed against, and were a sixth of a second long, so they
# are longer now (the brief's correction of 2026-10-07):
#
#   clip                 old      the old key beat                          here
#   holding-right-shoot  0.2000   the arm's one extreme, at 0.067           0.2000, furthest out at 0.067
#   pick-up              0.3333   lowest at 0.167                           0.3333, lowest at 0.167
#   attack-melee-right   0.4167   hand furthest out at 0.250                0.4167, furthest out at 0.250
#   attack-melee-left    0.4167   hand furthest out at 0.250                0.4167, shut at 0.250
#   interact-right       0.6667   out by 0.167, held to 0.40, home by 0.667 0.6667, the same three times
#   interact-left        0.6667   the same                                  0.6667, the same
#   slide                0.9500   down by 0.133, lowest 0.25, reach 0.333   0.9500, the same three times
#   die                  0.3333   in the air at 0.10, down by 0.233         0.3333, the same two times
#   holding-right        0.1667   a held pose, two equal keys               a loop of 2.0 s
#   crouch               0.1667   a held pose, two equal keys               a loop of 1.8 s
#   sit                  0.1667   a held pose, two equal keys               0.8 s of getting down
#   emote-yes            0.6667   a nod: back at 0.167, down at 0.500       a loop of 1.0 s
#   emote-no             0.6667   a shake: 0.167 one way, 0.500 the other   a loop of 1.2 s
#
# THE THROW AND THE TAG ARE HALF THE GAME'S. While they play, code poses the chest, the head
# and both UPPER arms over the clip (CharacterAnimator.ThrowBody.cs adds its turn to the
# chest and points the arms; TagBody.cs takes the whole body to its own reach for 0.34 s).
# What is left of the clip is the root, the legs and the two FOREARMS. So in those two the
# character is in the root (the squash, the stamp, the hop) and each forearm is a plain bend
# of the elbow with no twist, opening to straight at the key beat, which looks right on
# whatever upper arm the game points. The chest is left nearly alone so the game's turn does
# not stack on one of hers.
#
# HOW SHE DOES THEM. She is the wall and the hardest hitter on the street, a girl of twelve
# with her hands in fists. So nothing here is dainty and nothing is a hero's move: she throws
# the tsinelas the way she would swat with it, she tags with a straight punch of a reach that
# stops at the touch, she shoves by slamming her forearm round like a jeepney door, she goes
# down for the slipper in a wide squat and not a bow, and when she is knocked over she goes
# like a plank and keeps one fist in the air.
# ======================================================================================
HIP = (0.0836, 0.17625, -0.02875)     # the leg bones, in the root's space; the sole is LEG below
LEG = 0.17625
LEGS_STAND = (qz(4.5), qz(-4.5))


def _inv(q):
    return (-q[0], -q[1], -q[2], q[3])


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
        out[key] = _mix(va, vb, t) if key[1] == "rotation" else tuple(p + (q - p) * t for p, q in zip(va, vb))
    return out


EASES = {
    "smooth": _ease,
    "lin": lambda t: t,
    "in": lambda t: t * t,                       # gathers speed and arrives fast: a hit
    "out": lambda t: 1.0 - (1.0 - t) ** 2,        # leaves fast and hangs: a wind up, a settle
    "snap": lambda t: t * t * t,
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




#   THE CARRY. The tsinelas is in her right fist, cocked out beside her jaw where a slipper
#   is held to swat with, clear of her hair so it shows from behind; her left fist rides in
#   front of her hip. Her chest is turned a little off the throwing shoulder.
ARM_CARRY = ((0.86, -0.42, -0.06), (0.64, 0.52, 0.56))
ARM_GUARD = ((0.66, -0.74, 0.10), (0.20, -0.42, 0.88))
CARRY_REACH = 0.28


def _carry(heft=0.0, sy=0.985, look=7.0, lean=0.0, tx=0.0, tip=0.0):
    upper = tuple(a + (b - a) * heft for a, b in zip(ARM_CARRY[0], (0.74, -0.66, 0.12)))
    fore = tuple(a + (b - a) * heft for a, b in zip(ARM_CARRY[1], (0.62, -0.50, 0.60)))
    return _pose(tx=tx, root=qz(tip), sy=sy, torso=mul(qy(-9.0), qx(lean)), head=qy(look), left=ARM_GUARD,
                 right=(upper, fore), reach=(0.0, CARRY_REACH + 0.12 * heft), legs=(qz(7.0), qz(-7.0)))


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


def holding_right():
    """Carrying the tsinelas, a loop of 2 s: square on wide feet with the slipper cocked beside
    her jaw, breathing, her weight going over one foot and back, her eyes going along the
    other side; and twice (0.90 s and 1.32 s) she lets the fist drop to the front of her hip
    and snaps it back up, her whole weight dipping with it: weighing the thing the way you
    weigh a stone you mean to throw hard."""
    length = 2.0
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        breath = math.sin(2.0 * p)
        heft = min(1.0, _bump(t, 0.90, 0.085) + _bump(t, 1.32, 0.085))
        _add(out, t, _carry(heft=heft, sy=0.985 + 0.011 * breath - 0.045 * heft, look=7.0 + 11.0 * math.sin(p + 0.9) - 14.0 * heft,
                            lean=0.8 * breath + 5.0 * heft, tx=0.011 * math.sin(p), tip=-1.5 * math.sin(p)))
    return out


def holding_right_shoot():
    """THE THROW, what is hers of it (the game points her chest and upper arms). She STAMPS
    it: her front foot comes up as she sinks, then she goes forward onto it with the whole
    block of her stretched and turned behind the arm, the elbow snapping straight and the
    forearm thrown long (0.067 s); she lands squashed on that foot with the back one kicked
    up off the ground, and drops back into the carry."""
    # THE TURN IS THE ROOT'S, here and in the tag and the shove: her skirt hangs from her chest
    # over her legs, and a chest turned 30 degrees on still legs drags it through her thighs.
    # It suits her anyway, the whole block of her turns.
    cocked = _pose(sy=0.92, root=qy(-10.0), torso=qx(-4.0), head=mul(qy(12.0), qx(3.0)), space="world",
                   left=((0.20, -0.30, 0.93), (0.0, 0.10, 1.0)),
                   right=((0.62, -0.04, -0.78), (0.22, 0.90, -0.38)), reach=(0.10, 0.25),
                   legs=(mul(qx(-15.0), qz(7.0)), mul(qx(3.0), qz(-7.0))))
    out = _pose(tz=0.045, sy=1.09, root=mul(qx(9.0), qy(12.0)), torso=qx(5.0),
                head=mul(qy(-12.0), qx(-10.0)), space="world",
                left=((0.50, -0.50, -0.70), (0.30, -0.20, -0.93)),
                right=((0.10, 0.10, 0.99), (0.0, -0.05, 1.0)), reach=(0.0, 0.85),
                legs=(mul(qx(-19.0), qz(6.0)), mul(qx(21.0), qz(-6.0))))
    through = _pose(tz=0.055, sy=0.92, root=mul(qx(13.0), qy(16.0)), torso=qx(7.0),
                    head=mul(qy(-16.0), qx(-15.0)), space="world",
                    left=((0.55, -0.45, -0.70), (0.45, 0.10, -0.89)),
                    right=((-0.15, -0.52, 0.84), (-0.42, -0.58, 0.70)), reach=(0.0, 0.40),
                    legs=(mul(qx(-13.0), qz(6.0)), mul(qx(36.0), qz(-6.0))))
    _elbows(cocked, (30.0, 20.0), (78.0, 55.0))
    _elbows(out, (42.0, 0.0), (3.0, 0.0))
    _elbows(through, (50.0, 10.0), (20.0, 10.0))
    return _act(0.2, [(0.0, _carry(), "lin"), (1.0 / 30.0, cocked, "out"), (2.0 / 30.0, out, "in"),
                      (0.117, through, "out"), (0.2, _carry(), "smooth")])


def pick_up():
    """Down for the slipper in a wide squat, not a bow: her feet go apart and the block of her
    drops between them, her left fist lands on her knee and the right goes straight to the
    ground; then she springs up past standing with it and settles. Lowest at 0.167 s."""
    ready = _pose(sy=1.04, left=((0.74, -0.62, 0.05), (0.66, -0.60, 0.30)), right=((0.74, -0.62, 0.05), (0.66, -0.60, 0.30)))
    low_legs = (qz(42.0), qz(-42.0))

    def low(sy, grab):
        return _pose(sy=sy + 0.06, torso=mul(qx(44.0), qy(12.0)), head=mul(qy(-8.0), qx(-28.0)), space="root",
                     left=((0.78, -0.40, 0.42), (0.20, -0.95, 0.05)),
                     right=((0.10, -0.72, 0.66), (-0.30, -0.90, 0.30)), reach=(0.10, 0.36 + grab), legs=low_legs)

    up = _pose(sy=1.07, torso=qx(-4.0), head=qx(-6.0), left=ARMS_HANG,
               right=((0.80, -0.30, 0.30), (0.45, 0.70, 0.52)), reach=(0.0, 0.25))
    return _act(1.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 30.0, ready, "out"), (1.0 / 6.0, low(0.77, 0.12), "in"),
                            (0.2, low(0.81, 0.0), "out"), (0.283, up, "smooth"), (1.0 / 3.0, _stand(), "smooth")])


def attack_melee_right():
    """THE TAG, what is hers of it (for 0.34 s the game takes her whole body to its own reach
    and leaves the clip the forearms and the root's squash). A touch thrown like a punch: she
    sinks with the elbow cocked, the forearm opens out long and dead straight with the whole
    of her stretched behind it (0.25 s), and it STOPS at the touch. Then the part that is all
    hers: she drops back onto both feet with a bounce, fists up, before they fall."""
    wind = _pose(sy=0.90, root=qy(-10.0), torso=qx(-3.0), head=mul(qy(12.0), qx(3.0)),
                 left=((0.45, -0.55, 0.70), (-0.20, 0.50, 0.84)),
                 right=((0.56, -0.44, -0.70), (0.28, 0.22, 0.93)), reach=(0.0, 0.0),
                 legs=(mul(qx(-5.0), qz(6.0)), mul(qx(5.0), qz(-6.0))))
    _elbows(wind, (55.0, 40.0), (58.0, 20.0))

    def hit(sy, lean, reach):
        row = _pose(tz=0.045, sy=sy, root=mul(qx(0.5 * lean), qy(12.0)), torso=qx(0.4 * lean),
                    head=mul(qy(-12.0), qx(-0.6 * lean)), space="world",
                    left=((0.60, -0.50, -0.62), (0.50, 0.12, -0.86)),
                    right=((0.08, 0.02, 0.99), (0.0, 0.0, 1.0)), reach=(0.0, reach),
                    legs=(mul(qx(-12.0), qz(5.0)), mul(qx(12.0), qz(-5.0))))
        return _elbows(row, (60.0, 20.0), (2.0, 0.0))

    back = _pose(sy=1.04, torso=qx(-3.0), head=qx(-4.0),
                 left=((0.60, -0.66, 0.45), (0.10, 0.60, 0.79)), right=((0.60, -0.66, 0.45), (0.10, 0.60, 0.79)))
    return _act(5.0 / 12.0, [(0.0, _stand(), "lin"), (0.12, wind, "out"), (0.25, hit(1.07, 16.0, 0.85), "smooth"),
                             (0.29, hit(0.96, 20.0, 0.60), "out"), (0.355, back, "smooth"), (5.0 / 12.0, _stand(), "smooth")])


def attack_melee_left():
    """THE SHOVE: the wall walks into you. She sinks back onto her heels with both fists
    drawn up to her ribs, then the whole plank of her tips
    forward off her back foot and TRAVELS, both forearms rammed out long and wide of her head
    with her chin tucked between them, the left shoulder a little ahead, the back leg straight
    out behind (0.25 s). She lands on it squashed, arms still out, and rocks back to standing."""
    wind = _pose(tz=-0.035, sy=0.87, root=mul(qx(-9.0), qy(8.0)), torso=qx(-4.0), head=qx(10.0), space="world",
                 # the upper arms stay near where they hang: a wide sleeve swung back opens like a wing
                 left=((0.60, -0.76, -0.24), (0.18, 0.46, 0.87)), right=((0.60, -0.76, -0.24), (0.18, 0.46, 0.87)),
                 reach=(0.0, 0.0), legs=(mul(qx(-10.0), qz(8.0)), mul(qx(-10.0), qz(-8.0))))

    def push(tz, sy, lean, reach, kick):
        return _pose(tz=tz, sy=sy, root=mul(qx(lean), qy(-9.0)), torso=qx(6.0), head=mul(qy(8.0), qx(8.0 - 0.6 * lean)),
                     space="world", left=((0.80, 0.14, 0.58), (0.46, 0.20, 0.86)), right=((0.82, 0.10, 0.56), (0.50, 0.16, 0.85)),
                     reach=(reach, reach - 0.15),
                     legs=(mul(qx(-lean - 12.0), qz(6.0)), mul(qx(kick - lean), qz(-6.0))))

    return _act(5.0 / 12.0, [(0.0, _stand(), "lin"), (0.14, wind, "out"), (0.25, push(0.115, 1.09, 24.0, 0.95, 44.0), "in"),
                             (0.31, push(0.115, 0.93, 20.0, 0.70, 30.0), "out"), (5.0 / 12.0, _stand(), "smooth")])


#   THE KNOCK. Her fist out in front of her chest, knuckles first.
ARM_KNOCK = ((0.44, -0.38, 0.81), (0.04, 0.22, 0.97))
ARM_KNOCK_BACK = ((0.52, -0.52, 0.68), (0.10, 0.62, 0.78))


def _knock(side, out, lean):
    """One arm out in a knock (`out` 1) or drawn back for the next one (`out` 0)."""
    arm = tuple(tuple(a + (b - a) * out for a, b in zip(p, q)) for p, q in zip(ARM_KNOCK_BACK, ARM_KNOCK))
    turn = 13.0 * side          # the knocking shoulder comes forward
    row = dict(sy=1.0 - 0.02 * out, tz=0.012 * out, torso=mul(qx(lean), qy(turn)), head=mul(qy(-0.6 * turn), qx(2.0)))
    if side > 0:
        return _pose(right=arm, reach=(0.0, 0.10 + 0.55 * out), **row)
    return _pose(left=arm, reach=(0.10 + 0.55 * out, 0.0), **row)


def interact_right():
    """She knocks: the right fist goes out at chest height and raps three times, knuckles
    first, her shoulder behind each one, then drops. Out at 0.167 s, the last rap at 0.40."""
    return _act(2.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 6.0, _knock(1, 1.0, 8.0), "in"), (0.225, _knock(1, 0.0, 3.0), "out"),
                            (0.283, _knock(1, 1.0, 9.0), "in"), (0.342, _knock(1, 0.0, 3.0), "out"), (0.40, _knock(1, 1.0, 10.0), "in"),
                            (0.47, _knock(1, 0.75, 6.0), "out"), (2.0 / 3.0, _stand(), "smooth")])


def interact_left():
    """The off hand does not knock, it LEANS on the thing: the left fist goes out and she
    puts her weight on it once, slowly, head tipped, and lets go. Out at 0.167 s, held to 0.40."""
    def press(weight):
        return _pose(sy=0.98 - 0.03 * weight, tz=0.012 + 0.020 * weight, torso=mul(qx(7.0 + 9.0 * weight), qy(-13.0)),
                     head=mul(mul(qy(8.0), qx(2.0 - 6.0 * weight)), qz(-7.0 * weight)),
                     left=ARM_KNOCK, reach=(0.45, 0.0),
                     right=((0.62, -0.72, -0.30), (0.50, -0.70, 0.50)))

    return _act(2.0 / 3.0, [(0.0, _stand(), "lin"), (1.0 / 6.0, press(0.0), "in"), (0.30, press(1.0), "smooth"),
                            (0.40, press(0.6), "smooth"), (2.0 / 3.0, _stand(), "smooth")])


def slide():
    """The retrieval slide, feet first like a ballplayer stealing home: a short hop, then she
    is down on her seat leaning back with the right leg shot out ahead and the left kicked
    wide, left fist on the ground behind her and the right reaching past her foot for the
    slipper; she rocks up over her feet into a squat and stands with a bounce.
    Down by 0.133 s, lowest at 0.25, the reach furthest at 0.333, on her feet from 0.62."""
    hop = _pose(sy=1.06, lift=0.020, torso=qx(-5.0), head=qx(-4.0),
                left=((0.80, -0.20, 0.30), (0.50, 0.60, 0.60)), right=((0.80, -0.20, 0.30), (0.50, 0.60, 0.60)),
                legs=(mul(qx(-8.0), qz(3.0)), mul(qx(-8.0), qz(-3.0))))

    def down(back, sy, chest, reach, lift):
        return _pose(tz=0.090, sy=sy, lift=lift, root=qx(-back), torso=mul(qx(chest), qy(14.0)),
                     head=mul(qy(-8.0), qx(back - chest - 2.0)), space="world",
                     left=((0.62, -0.50, -0.60), (0.20, -0.82, -0.54)),
                     right=((0.22, -0.30, 0.93), (0.02, -0.34, 0.94)), reach=(0.45, reach),
                     legs=(mul(qy(34.0), qx(-(70.0 - back))), mul(qy(-4.0), qx(-(88.0 - back)))))

    squat = _pose(sy=0.92, tz=0.030, torso=qx(13.0), head=qx(-8.0), space="root",
                  left=((0.70, -0.45, 0.55), (0.30, -0.30, 0.90)), right=((0.70, -0.45, 0.55), (0.30, -0.30, 0.90)),
                  legs=(qz(27.0), qz(-27.0)))
    up = _pose(sy=1.05, torso=qx(-3.0), head=qx(-4.0))
    settle = _pose(sy=0.975)
    return _act(0.95, [(0.0, _stand(), "lin"), (0.06, hop, "out"), (0.133, down(40.0, 0.96, 20.0, 0.30, 0.0), "in"),
                       (0.25, down(48.0, 0.90, 22.0, 0.50, 0.0), "out"), (1.0 / 3.0, down(43.0, 0.93, 32.0, 0.90, 0.0), "smooth"),
                       (0.38, down(40.0, 0.95, 30.0, 0.70, 0.0), "smooth"), (0.50, squat, "smooth"), (0.64, up, "smooth"),
                       (0.76, settle, "smooth"), (0.86, _stand(), "smooth"), (0.95, _stand(), "lin")])


def crouch():
    """Out of breath, a loop of 1.8 s: folded right over on her feet, her seat pushed back, both
    fists propped on her knees and her head HUNG, the top of it to the street. Her back heaves
    three times, the shoulders coming up between her propped arms and dropping. Once, on the
    third breath, she lifts her face to see where the game has gone, and lets it drop again.
    Folded over on every frame, so it can be held on its last."""
    length = 1.8
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        air = 0.5 - 0.5 * math.cos(3.0 * p)                # 0 emptied, 1 full
        look = _window(t, 1.02, 1.55, 0.22)
        _add(out, t, _pose(tz=-0.030, sy=0.93 + 0.045 * air, torso=qx(50.0 - 11.0 * air),
                           head=mul(qx(15.0 - 5.0 * air - 44.0 * look), qz(4.0 - 10.0 * look)), space="root",
                           left=((0.72, -0.66 + 0.22 * air, 0.20), (-0.10, -0.97, -0.22)),
                           right=((0.72, -0.66 + 0.22 * air, 0.20), (-0.10, -0.97, -0.22)),
                           reach=(0.10 + 0.25 * air, 0.10 + 0.25 * air),
                           legs=(mul(qx(-13.0), qz(15.0)), mul(qx(-13.0), qz(-15.0)))))
    return out


ARM_BEHIND = ((0.58, -0.58, -0.58), (0.30, -0.80, -0.52))      # a fist planted on the ground behind her
ARM_FLUNG = ((0.84, 0.30, 0.25), (0.50, 0.82, 0.20))


def _seat(sy=1.0, back=12.0, kick=74.0, up=0.0, arms=ARM_BEHIND, reach=0.35, chin=14.0):
    """Sat on the ground, legs out in a V: leaning `back`, legs `kick` degrees forward of
    her, `up` above the ground."""
    return _pose(root=qx(-back), sy=sy, lift=0.045 * sy + up, torso=qx(-6.0), head=mul(qx(chin), qz(-5.0)), space="world",
                 left=arms, right=arms, reach=(reach, reach),
                 legs=(mul(qy(26.0), qx(-kick)), mul(qy(-26.0), qx(-kick))))


def die():
    """Knocked down, and the wall goes over like a plank: a jolt off her feet with her arms
    thrown up, straight over backward with no fold in her, flat on her back with a bounce and
    her legs kicked up, and then she lies starfished with her right fist still stuck up in the
    air. In the air at 0.10 s, down at 0.233, and the last frame is held."""
    wide = ((0.80, 0.34, 0.30), (0.50, 0.80, 0.24))
    jolt = _pose(lift=0.060, ground=False, sy=1.10, root=qx(-12.0), torso=qx(-6.0), head=qx(-16.0),
                 left=wide, right=wide, reach=(0.3, 0.3), legs=(mul(qx(-22.0), qz(8.0)), mul(qx(-22.0), qz(-8.0))))
    over = _pose(lift=0.100, ground=False, sy=1.04, root=qx(-52.0), torso=qx(-4.0), head=qx(-10.0),
                 left=((0.92, 0.25, -0.20), (0.62, 0.62, 0.48)), right=((0.92, 0.25, -0.20), (0.62, 0.62, 0.48)),
                 reach=(0.3, 0.3), legs=(mul(qx(-34.0), qz(12.0)), mul(qx(-34.0), qz(-12.0))))
    flat = ((0.95, 0.10, -0.26), (0.95, 0.20, -0.20))
    land = _pose(lift=0.100, ground=False, scale=(1.12, 1.04, 0.80), root=qx(-90.0), head=qx(20.0),
                 left=flat, right=flat, reach=(0.2, 0.2), legs=(mul(qx(-52.0), qz(16.0)), mul(qx(-52.0), qz(-16.0))))
    bounce = _pose(lift=0.150, ground=False, scale=(0.96, 1.0, 1.08), root=qx(-93.0), head=qx(24.0),
                   left=flat, right=((0.90, -0.10, -0.10), (0.30, 0.10, 0.95)), reach=(0.1, 0.2),
                   legs=(mul(qx(-26.0), qz(18.0)), mul(qx(-30.0), qz(-18.0))))
    rest = _pose(lift=0.120, ground=False, root=qx(-90.0), head=mul(qx(20.0), qz(12.0)),
                 left=((0.92, -0.34, -0.20), (0.80, -0.55, -0.20)), right=((0.62, -0.10, 0.78), (0.06, 0.04, 1.0)),
                 reach=(0.0, 1.30), legs=(mul(qx(-3.0), qz(15.0)), mul(qx(-3.0), qz(-15.0))))
    return _act(1.0 / 3.0, [(0.0, _stand(), "lin"), (0.05, jolt, "out"), (0.133, over, "lin"), (0.233, land, "in"),
                            (0.283, bounce, "out"), (1.0 / 3.0, rest, "in")])


def emote_no():
    """NO, a loop of 1.2 s: a barred gate. Her elbows go out wide of her shoulders and her
    forearms cross in a big X held well out in front of her chest, long enough that an elbow
    is a bar across the whole front of her, and she HOLDS it, leaning back from the idea, the
    whole block of her turning one way and the other behind it while her head shakes against
    the turn. Twice (0.34 s and 0.94 s) she slashes both fists down and out past her hips with
    a stamp (never level: it is a cut, not a T) and the X snaps back up. The slashes are what
    shows from behind; her upper arm is too short for a crossed forearm to clear her back."""
    length = 1.2
    gate = (((0.90, -0.10, 0.42), (-0.76, 0.26, 0.60)), ((0.90, -0.10, 0.42), (-0.70, 0.16, 0.70)))
    slash = ((0.72, -0.66, 0.20), (0.66, -0.72, 0.22))
    held = _pose(sy=1.0, torso=qx(-7.0), left=gate[1], right=gate[0], reach=(1.05, 1.15), legs=(qz(8.0), qz(-8.0)))
    down = _pose(sy=0.90, torso=qx(4.0), left=slash, right=slash, reach=(0.45, 0.45), legs=(qz(11.0), qz(-11.0)))
    out = {}
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        s = _snap(math.sin(2.0 * p), 0.6)
        cut = min(1.0, _bump(t, 0.34, 0.075) + _bump(t, 0.94, 0.075))
        row = _blend(held, down, cut)
        row[("root", "rotation")] = qy(20.0 * s)
        row[("root", "scale")] = _squash(row[("root", "scale")][1] - 0.025 * abs(s))
        row[("head", "rotation")] = mul(qy(-44.0 * s), qx(-3.0 + 12.0 * cut))
        _add(out, t, row)
    return out


#   CHEER: both arms up in a V through the elbows, thrown a little wider than her shoulders
ARM_UP_CHEER = ((0.62, 0.72, 0.14), (0.22, 0.97, 0.08))
CHEER_REACH = 0.62


def cheer():
    """A 1 s loop. Two small hops, both feet together, arms up in a V through the elbows and the
    hands shaking a little at the top, head back: a kid cheering, all bounce and no swagger."""
    out = {}
    length = 1.0
    for t in _times(length):
        p = 2.0 * math.pi * 2.0 * t / length         # two hops a second
        air = max(0.0, math.sin(p)) ** 0.8
        land = max(0.0, -math.sin(p)) ** 1.5
        shake = math.sin(2.0 * math.pi * 4.0 * t / length)
        up = {1: _arm(1, *ARM_UP_CHEER), -1: _arm(-1, *ARM_UP_CHEER)}
        row = _pose(lift=0.030 * air, sy=1.0 + 0.05 * air - 0.07 * land, torso=qx(-4.0 - 2.0 * air),
                    head=mul(qx(-14.0 - 4.0 * air), qz(3.0 * math.sin(p * 0.5))),
                    legs=(mul(qx(-6.0 * air), qz(2.0)), mul(qx(-6.0 * air), qz(-2.0))))
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(side * 3.0 * land))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 7.0 * shake))
            row[("forearm-" + name, "scale")] = (1.0 + CHEER_REACH, 1.0, 1.0)
        _add(out, t, row)
    return out


#   CLAP: her hands up under her chin, elbows tucked in to her sides
ARM_CLAP_OPEN = ((0.30, -0.70, 0.65), (0.02, 0.86, 0.50))
ARM_CLAP_SHUT = ((0.24, -0.72, 0.65), (-0.52, 0.76, 0.38))
CLAP_REACH = (0.34, 0.30)


def _clap_row(shut, sway=0.0, sy=1.0, lift=0.0, legs=None, extra_head=None, ground=True, root=None, torso=None, space="torso"):
    left = tuple(tuple(a + (b - a) * shut for a, b in zip(p, q)) for p, q in zip(ARM_CLAP_OPEN, ARM_CLAP_SHUT))
    reach = CLAP_REACH[0] + (CLAP_REACH[1] - CLAP_REACH[0]) * shut
    head = mul(qx(-6.0 + 9.0 * shut), qz(3.0 * sway))
    if extra_head is not None:
        head = mul(extra_head, head)
    return _pose(lift=lift, sy=sy, root=root, torso=torso if torso is not None else mul(qx(3.0 * shut), qz(-2.0 * sway)),
                 head=head, left=left, right=left, reach=(reach, reach), legs=legs or FEET_IN, ground=ground, space=space)


CLAPS = (0.0, 1.0 / 3.0, 2.0 / 3.0)


def _clapping(t, length=1.0, claps=CLAPS):
    """0 apart, 1 the hands meeting: each clap is a quick snap shut and a slower open."""
    best = 0.0
    for at in claps:
        d = (t - at) % length                   # time since this clap, 0..length
        if d > 0.5 * length:
            d -= length                         # negative: the clap is coming
        width = 0.030 if d < 0.0 else 0.060     # in fast, out slower
        best = max(best, math.exp(-(d / width) ** 2))
    return best


def clap():
    """HER SIGNATURE, a 1 s loop: quick little claps, three a second, hands up under her chin
    and elbows tucked in, a small nod into each clap and a tiny bounce on her heels, swaying a
    touch from foot to foot. Small and fast; the lola claps twice a second with her whole
    stoop, this girl claps like she cannot help it."""
    out = {}
    length = 1.0
    for t in _times(length):
        shut = _clapping(t, length)
        sway = math.sin(2.0 * math.pi * t / length)
        _add(out, t, _clap_row(shut, sway=sway, sy=1.0 - 0.025 * shut, lift=0.006 * shut))
    return out


def emote_yes():
    """YES is her clap (the audience brief: emote-yes is replaced with the clap)."""
    return clap()


#   SIT: where the seat is. The underside of her hips (the pelvis block under the tapis, at
#   0.118 in the model) is brought to the root's origin, so a ledge top at the root's height
#   carries her. Her thighs (the leg bones; the rig has no knee) go forward and down over the
#   ledge's front edge.
SEAT = 0.118
SIT_LEGS = 56.0
ARM_LAP = ((0.42, -0.80, 0.42), (-0.10, -0.42, 0.90))
LAP_REACH = 0.30


def sit():
    """SEATED ON A LEDGE as a spectator, a loop of 2.4 s: hips on the ledge top, knees
    together, her legs swinging over the edge one against the other the way a kid's do,
    hands on her lap; then (1.05 to 2.05 s) her hands come up under her chin for three quick
    claps and go back down. A small breath all through."""
    out = {}
    length = 2.4
    claps = (1.30, 1.55, 1.80)
    for t in _times(length):
        p = 2.0 * math.pi * t / length
        breath = math.sin(2.0 * p)
        swing = math.sin(2.0 * p)
        up = _window(t, 1.05, 2.05, 0.18)
        shut = sum(math.exp(-((t - at) / 0.045) ** 2) for at in claps) * up
        sy = 1.0 + 0.008 * breath
        legs = (mul(qx(-SIT_LEGS + 9.0 * swing), qz(-2.5)), mul(qx(-SIT_LEGS - 9.0 * swing), qz(2.5)))
        lap = ARM_LAP
        chin = tuple(tuple(a + (b - a) * shut for a, b in zip(p_, q)) for p_, q in zip(ARM_CLAP_OPEN, ARM_CLAP_SHUT))
        arm = tuple(tuple(a + (b - a) * up for a, b in zip(p_, q)) for p_, q in zip(lap, chin))
        reach = LAP_REACH + (CLAP_REACH[0] - LAP_REACH) * up
        head = mul(mul(qx(5.0 - 8.0 * up + 7.0 * shut), qy(6.0 * math.sin(p))), qz(2.0 * swing))
        row = _pose(lift=-SEAT * sy, ground=False, sy=sy, root=qx(-2.0), torso=qx(3.0 + 0.8 * breath + 3.0 * shut), head=head,
                    left=arm, right=arm, reach=(reach, reach), legs=legs)
        _add(out, t, row)
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
         "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
         "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
         "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
         "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no,
         "cheer": cheer, "clap": clap}
