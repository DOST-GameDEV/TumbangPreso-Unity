"""New locomotion clips for the Aling Nena redesign PROTOTYPE.

Imported by tools/author_character_redesign_aling_nena.py, which writes them into the prototype
.glb IN PLACE OF the clips of the same name copied from character-female-e.glb. Nothing in the
game reads them; character-female-e.glb keeps its own. Every other clip in the .glb (slide, sit,
the emotes, the holding and attack clips, 28 in all) is copied across untouched. The helpers are
the heroes' clips script's; each character's motion is its OWN (docs/CHARACTER_REDESIGN_DANTE.md
section 13 rule 9), so none of the numbers in the clips below are another's.

WHAT HER MOTION SHOULD SAY ABOUT HER. ALING NENA "owns the corner store, so she owns the rules.
Nobody has ever argued a call twice" (the character select); she is "the auntie on an errand:
brisk and purposeful, leaning in, arms pumping short and business-like" (`GaitStyles`); "slow
and very hard to knock down" (GAME_OVERVIEW.md: speed 2, grit 5). A woman of about fifty who
has minded a counter and a street full of children for thirty years. So:
  * SHE IS THE ONE IN CHARGE. Her gestures are aimed AT somebody: a finger wagged, a call up
    the street. One fist lives on her hip.
  * SHE IS BUSY. Between calls her hands go to her apron.
  * SHE IS BRISK, NOT FAST. Short quick steps, the hips swinging, the elbows tucked and pumping
    small. Nothing of her flails.
  * SHE IS SOLID. Little bounce, the weight low; squash and stretch stay small on her.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way she faces, +x her LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops her left arm from straight out to hanging.

ARMS UP GO THROUGH THE ELBOW (section 13 rule 8, amended by the owner on Dante: "the jump looks
like he's shrugging"). Her elbow is at the mouth of her short sleeve, 82 mm from the shoulder;
the bare forearm and the fist ride the forearm bone, 116 mm more to the fist's end (the heroes'
arm). A straight arm cannot rise above level (her ears and her pencil are over it), so the
upper arm lifts a little and the forearm folds up, and may stretch (scale on the forearm bone)
as it is thrown or as it reaches.

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
#   left, +y up, +z ahead). The right is the mirror.
#   HANGING, HER WAY: elbows out past the apron, forearms turned in and forward, fists held in
#   front of her hips the way a woman stands who is about to pick something up.
ARMS_HANG = ((0.70, -0.71, 0.02), (0.40, -0.84, 0.36))
#   FIST ON HIP (the pamewang): the elbow out to the side and a little back, the fist set on her
#   waistband.
ARM_HIP = ((0.92, -0.36, -0.16), (0.45, -0.86, 0.24))
#   THE WAG. Her right arm (written as a left arm, mirrored below): the elbow forward of her
#   shoulder, the forearm straight up beside her face. The wag swings the forearm side to side.
ARM_WAG = ((0.86, -0.28, 0.42), (0.24, 0.95, 0.18))
WAG_SWING = 0.34
WAG_REACH = 0.55
#   THE APRON. Both hands brought down and in onto the front of the apron.
ARM_APRON = ((0.52, -0.79, 0.33), (-0.38, -0.50, 0.78))
APRON_REACH = 0.60
#   THE CALL. Her right hand (written as a left arm) cupped beside her mouth.
ARM_CALL = ((0.70, -0.12, 0.70), (-0.06, 0.86, 0.42))
CALL_REACH = 0.60
IDLE_LENGTH = 8.0


def _lerp_arm(a, b, t):
    return tuple(_mix(a[k], b[k], t) for k in range(2))


def idle():
    """Eight seconds of the woman whose store this corner belongs to, in three things she does:

      THE WAG (0.6 to 3.0 s). Her left fist goes to her hip, her right hand comes up beside her
      face and she wags it at somebody across the street, three times, her head shaking the
      other way with each one and her whole body leaning into it. Then one short nod: that is
      settled.
      THE APRON (3.3 to 5.2 s). Both hands come down onto the front of her apron and she wipes
      them on it, one going down as the other comes up, twice, looking down at them. Back to work.
      THE CALL (5.5 to 7.7 s). Her left fist goes back to her hip, her right hand cups beside
      her mouth, and she leans out and calls up the street, twice (a child is being sent home,
      or sent to buy vinegar), rising onto her toes with each call. Then she drops her arms and
      settles back onto her heels.

    WHY THESE. She "owns the rules" and "nobody has ever argued a call twice": she is the
    street's referee and its shopkeeper, so what she does while she waits is rule on things and
    mind the store. None of the three is another character's (Dante: fists on both hips and
    folded arms; Bebang: a fist into her palm, her headband, a shoulder roll; Lola Pacing: her
    fan; Amihan and Cheska: hops and hands behind the back). One fist on one hip under a raised
    hand is hers.

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
    hip = _arm(1, *ARM_HIP)
    call = _arm(-1, *ARM_CALL)
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 4.0))
        scold = _window(t, 0.6, 3.0, 0.40)
        wipe = _window(t, 3.3, 5.2, 0.40)
        shout = _window(t, 5.5, 7.7, 0.40)
        # three wags, eased in and out, then the nod that closes the matter
        wagging = _window(t, 1.05, 2.25, 0.20)
        wag = math.sin(2.0 * math.pi * (t - 1.05) / 0.40) * wagging
        nod = _bump(t, 2.48, 0.10)
        # the wipe: two strokes, the hands opposed
        rub = math.sin(2.0 * math.pi * (t - 3.70) / 0.55) * _window(t, 3.70, 4.80, 0.20)
        # two calls: a lean out and up onto the toes, held, and back
        cry = _window(t, 6.00, 6.55, 0.16) + _window(t, 6.70, 7.25, 0.16)
        settle = _bump(t, 7.62, 0.10) - 0.5 * _bump(t, 7.82, 0.10)
        row = {}
        wag_arm = _arm(-1, ARM_WAG[0], (ARM_WAG[1][0] - WAG_SWING * wag, ARM_WAG[1][1], ARM_WAG[1][2]))
        for side, name in ((1, "left"), (-1, "right")):
            apron = _arm(side, ARM_APRON[0], (ARM_APRON[1][0], ARM_APRON[1][1] - 0.16 * side * rub, ARM_APRON[1][2]))
            first = hip if side > 0 else wag_arm
            third = hip if side > 0 else call
            pose = _lerp_arm(_lerp_arm(_lerp_arm(hang[side], first, scold), apron, wipe), third, shout)
            row[("arm-" + name, "rotation")] = pose[0]
            row[("forearm-" + name, "rotation")] = pose[1]
            reach = APRON_REACH * wipe * (1.0 + 0.12 * side * rub)
            if side < 0:
                reach += WAG_REACH * scold + CALL_REACH * shout
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        # her head: turned toward whoever she is wagging at (her right) and shaking against the
        # wag; down at her hands; up and out with the call
        look = -14.0 * scold + 9.0 * wag - 16.0 * shout
        chin = 4.0 * scold + 12.0 * nod * scold + 12.0 * wipe - 9.0 * cry * shout + 3.0 * shout
        tilt = -5.0 * scold + 2.0 * wag + 3.0 * shout
        lean = 6.0 * scold + 2.0 * nod + 5.0 * wipe + 2.0 * abs(rub) + 5.0 * shout + 7.0 * cry * shout
        rise = 0.030 * cry * shout
        dip = 0.016 * nod * scold + 0.012 * abs(rub) + 0.040 * settle - rise
        row.update({
            ("root", "translation"): (0.0, -0.10 * dip, 0.0),
            ("root", "scale"): _squash(1.0 + 0.008 * breath - dip),
            ("root", "rotation"): qz(1.2 * scold + 1.0 * shout),
            ("torso", "rotation"): mul(mul(qx(lean + 0.6 * breath), qy(-8.0 * scold + 3.0 * wag - 10.0 * shout)), qz(-2.0 * scold - 2.0 * shout)),
            ("head", "rotation"): mul(mul(qy(look), qx(chin - 0.5 * breath)), qz(tilt)),
            # her weight on her left leg while she wags and calls, the right foot turned out
            ("leg-left", "rotation"): qz(3.0 - 1.2 * scold - 1.0 * shout),
            ("leg-right", "rotation"): mul(qz(-3.0 - 2.5 * scold - 2.5 * shout), qx(-3.0 * scold - 4.0 * cry * shout)),
        })
        _add(out, t, row)
    return out


def walk():
    """Brisk and on an errand: short quick steps with her hips swinging, leaning in from the
    ankles, her elbows tucked in and her fists pumping small beside her apron, her chin up."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.85)
        c = math.cos(phase)
        lift = abs(s) ** 1.6                      # low: her feet hardly leave the ground
        sway = math.sin(phase - 0.20)
        _add(out, t, {
            ("root", "translation"): (0.016 * sway, 0.002 + 0.018 * lift, 0.0),
            ("root", "rotation"): mul(qx(6.0), qz(3.6 * sway)),
            ("root", "scale"): _squash(0.975 + 0.040 * lift),
            ("leg-left", "rotation"): mul(qx(-27.0 * sh), qz(3.0 - 3.6 * sway)),
            ("leg-right", "rotation"): mul(qx(27.0 * sh), qz(-3.0 - 3.6 * sway)),
            # the shoulders turn against the hips and stay level while the hips swing
            ("torso", "rotation"): mul(mul(qx(1.0), qy(7.0 * sh)), qz(-4.6 * sway)),
            ("head", "rotation"): mul(mul(qx(-7.0 + 1.5 * abs(c)), qy(-5.0 * sh)), qz(1.0 * sway)),
            ("arm-left", "rotation"): mul(qx(6.0 + 16.0 * sh), qz(-64.0)),
            ("arm-right", "rotation"): mul(qx(6.0 - 16.0 * sh), qz(64.0)),
            ("forearm-left", "rotation"): mul(qx(-18.0), qy(-(62.0 - 14.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-18.0), qy(62.0 + 14.0 * sh)),
        })
    return out


def sprint():
    """She does not run, she HURRIES: a fast shuffle on her slippers, bent forward from the
    waist, her head pushed out ahead of her, her elbows high and her fists going like pistons."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.8)
        lift = abs(s) ** 1.4
        rock = math.sin(phase - 0.3)
        _add(out, t, {
            ("root", "translation"): (0.010 * rock, 0.003 + 0.026 * lift, 0.0),
            ("root", "rotation"): mul(qx(10.0), qz(3.0 * rock)),
            ("root", "scale"): _squash(0.965 + 0.060 * lift),
            ("leg-left", "rotation"): mul(qx(-44.0 * sh), qz(2.0 - 3.0 * rock)),
            ("leg-right", "rotation"): mul(qx(44.0 * sh), qz(-2.0 - 3.0 * rock)),
            ("torso", "rotation"): mul(mul(qx(11.0), qy(8.0 * sh)), qz(-3.0 * rock)),
            ("head", "rotation"): mul(mul(qx(-17.0 + 2.0 * lift), qy(-6.0 * sh)), qz(1.0 * rock)),
            ("arm-left", "rotation"): mul(qx(10.0 + 34.0 * sh), qz(-58.0)),
            ("arm-right", "rotation"): mul(qx(10.0 - 34.0 * sh), qz(58.0)),
            ("forearm-left", "rotation"): mul(qx(-10.0), qy(-(86.0 - 12.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-10.0), qy(86.0 + 12.0 * sh)),
        })
    return out


#   arms up: where the left arm points at the top of the jump. Her ears, her earrings and her
#   pencil stand out past her shoulders, so the V is thrown up and out, clear of them.
ARM_UP = ((0.78, 0.40, 0.12), (0.30, 0.94, 0.14))
ARM_UP_REACH = 0.70


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. A heavy woman's hop: a short hard push off both feet, both
    arms thrown up in a V through the elbows, her feet tucked back under her and held together
    (she is keeping her slippers on), her chin up."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.11)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.14)
        ring = math.cos(2.0 * math.pi * t / 0.44) * math.exp(-t / 0.13)
        row = {
            ("root", "scale"): _squash(1.0 + 0.15 * ring),
            ("root", "rotation"): qx(3.0 * hang),
            # both legs the same: straight under her at take-off, then tucked back, hardly apart
            ("leg-left", "rotation"): mul(qx(8.0 * rise + 20.0 * hang), qz(1.5 + 3.0 * hang)),
            ("leg-right", "rotation"): mul(qx(8.0 * rise + 20.0 * hang), qz(-1.5 - 3.0 * hang)),
            ("torso", "rotation"): qx(-5.0 * rise + 2.0 * hang),
            ("head", "rotation"): qx(-10.0 * rise - 6.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = up[side][0]
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 7.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.72 + 0.28 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: she comes down upright and cross about it, arms up in the V and shaking a little,
    her feet paddling small and fast under her, looking for the ground."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.03 + 0.012 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(-3.0), qz(1.5 * s)),
            ("leg-left", "rotation"): mul(qx(6.0 + 13.0 * s), qz(4.0)),
            ("leg-right", "rotation"): mul(qx(6.0 - 13.0 * s), qz(-4.0)),
            ("torso", "rotation"): mul(qx(-3.0), qz(-1.5 * s)),
            ("head", "rotation"): mul(qx(12.0 + 2.0 * c), qz(-1.0 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(4.0 * s * side))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(6.0 * c))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.64 + 0.06 * c), 1.0, 1.0)
        _add(out, t, row)
    return out


# ---------------------------------------------------------------------------
# THE THIRTEEN ACTION CLIPS (2026-10-07). Each replaces the clip of its name copied from the old
# shared rig, which was made for a body with no elbow and which all twelve of the cast played
# alike. These are hers: the woman who owns the corner store and the rules. One fist lives on
# her hip. She does nothing with a run-up. What she does is aimed AT somebody.
#
# MEASURED from the copied clips in the glb before they were replaced:
#
#   clip                  length   the old key beat                          hers
#   holding-right         0.1667   a still pose                              2.0 s loop
#   holding-right-shoot   0.2000   arm at its extreme at 0.067               slipper leaves at 0.067
#   pick-up               0.3333   lowest at 0.167                           hand on the ground at 0.167
#   attack-melee-right    0.4167   furthest out at 0.250 (root at 0.256)     hand furthest out at 0.250
#   attack-melee-left     0.4167   furthest out at 0.250 (root at 0.256)     hand furthest out at 0.250
#   interact-right/left   0.6667   hand out by 0.167, held to 0.45           the same
#   slide                 0.9500   down by 0.14, lowest 0.25 to 0.35         down at 0.25, deepest 0.37
#   crouch                0.1667   a still pose                              1.8 s, two breaths
#   sit                   0.1667   a still pose                              0.8 s of getting down
#   die                   0.3333   top of the arc 0.100, down by 0.267       the same
#   emote-yes / emote-no  0.6667   loops                                     loops of 1.2 and 1.0 s
#
# FIVE ARE LONGER THAN THE CLIP THEY REPLACE (`holding-right`, `crouch`, `sit`, `emote-yes`,
# `emote-no`): the game only looks at those and times nothing against them. The other eight keep
# their length and their beat exactly.
#
# THE THROW AND THE TAG ARE POSED OVER BY THE GAME (`CharacterAnimator.ThrowBody`, `.TagBody`):
# her chest, her head and both UPPER arms are overwritten while they play, and what is kept of
# the clip is the root, the legs and the two FOREARMS. So in those two, and in the carry the
# throw starts from, who she is goes into the root and the legs, and each forearm is a plain
# HINGE at the elbow (`"hinge"` below): a moderate bend that opens to straight at the beat.
#
# WHERE HER FISTS ARE AT EACH KEY BEAT (no two the same):
#   carry         left on her hip; right low and wide, the slipper beside her right thigh
#   throw         right straight, up and ahead, her right foot kicked up behind her; left still on her hip
#   pick up       right on the ground beside her right foot; left on her hip; tipped over, left leg in the air
#   tag           right long, straight and level ahead; left straight back behind her
#   shove         left swept low and wide to her front left; right on her hip; chest turned
#   reach right   right forearm level at her waist, elbow tucked; left in her apron pocket
#   reach left    left long and low out in front, palm up; right on her hip; chin up
#   slide         on her left hip, legs ahead; left propping on the street, right out ahead
#   out of breath left on her knee, bent over; right flapping at her face
#   sit           both fists on her hips, elbows out; legs out in a V
#   knocked down  face down; right flat out ahead on the street, left forearm standing straight up
#   yes           left forearm level in front as her notebook; right ticking it and flicking up
#   no            both arms thrown out wide and low from crossed in front of her
#
# HOW THEY ARE WRITTEN. A clip is a handful of whole-body POSES at times (`_body`), and `_play`
# samples between them at 60 a second. An arm in a pose is given by where it POINTS IN THE WORLD
# (+x OUT from her side, whichever arm it is; +y up; +z ahead), whatever her chest and her root
# are doing under it. A stance from the table above (`ARM_HIP`, `ARM_APRON`) is given as it is,
# in her chest's own space, by passing a third item.
# ---------------------------------------------------------------------------
HIP = (0.0, 0.17625, -0.02875)      # where her legs and chest join the root, from the file


def _ypr(pitch, yaw, roll):
    """Turned `yaw` to her left, tipped `pitch` forward, leaned `roll` to her right; degrees."""
    return mul(mul(qy(yaw), qx(pitch)), qz(roll))


def _conj(q):
    return (-q[0], -q[1], -q[2], q[3])


def _limb(side, spec, frame):
    """(arm rotation, forearm rotation) for one arm of a pose. See the note above."""
    if len(spec) == 3:
        return _arm(side, spec[0], spec[1])
    inverse = _conj(frame)
    if spec[0] == "hinge":
        # ("hinge", where the upper arm points in the world, bend, lift): the forearm is only a
        # hinge, `bend` degrees forward at the elbow and `lift` degrees up
        upper = _rot(inverse, (side * spec[1][0], spec[1][1], spec[1][2]))
        return _arc((float(side), 0.0, 0.0), upper), mul(qy(-side * spec[2]), qz(side * spec[3]))
    upper = _rot(inverse, (side * spec[0][0], spec[0][1], spec[0][2]))
    fore = _rot(inverse, (side * spec[1][0], spec[1][1], spec[1][2]))
    rest = (float(side), 0.0, 0.0)
    q = _arc(rest, upper)
    return q, _arc(rest, _rot(_conj(q), _norm(fore)))


def _body(at=(0.0, 0.0, 0.0), hip=None, root=(0.0, 0.0, 0.0), sy=1.0, torso=(0.0, 0.0, 0.0),
          head=(0.0, 0.0, 0.0), legl=(0.0, 3.0), legr=(0.0, -3.0), left=None, right=None, reach=(1.0, 1.0)):
    """One whole-body pose. `root`, `torso` and `head` are (pitch, yaw, roll); a leg is (pitch,
    roll), pitch negative swinging it forward, roll positive carrying it to her left. `hip`,
    when given, is where her hip joint is to be in the world, and the root is put wherever that
    needs (she sits and lies by it)."""
    rq, tq = _ypr(*root), _ypr(*torso)
    scale = _squash(sy)
    if hip is not None:
        offset = _rot(rq, (0.0, HIP[1] * scale[1], HIP[2] * scale[2]))
        at = tuple(h - o for h, o in zip(hip, offset))
    frame = mul(rq, tq)
    la = _limb(1, left or (ARMS_HANG[0], ARMS_HANG[1], "chest"), frame)
    ra = _limb(-1, right or (ARMS_HANG[0], ARMS_HANG[1], "chest"), frame)
    return {
        ("root", "translation"): tuple(at), ("root", "rotation"): rq, ("root", "scale"): scale,
        ("torso", "rotation"): tq, ("head", "rotation"): _ypr(*head),
        ("leg-left", "rotation"): mul(qx(legl[0]), qz(legl[1])),
        ("leg-right", "rotation"): mul(qx(legr[0]), qz(legr[1])),
        ("arm-left", "rotation"): la[0], ("forearm-left", "rotation"): la[1],
        ("arm-right", "rotation"): ra[0], ("forearm-right", "rotation"): ra[1],
        ("forearm-left", "scale"): (reach[0], 1.0, 1.0), ("forearm-right", "scale"): (reach[1], 1.0, 1.0),
    }


_EASE = {
    "s": _ease,                                   # leaves gently, arrives gently
    "in": lambda u: u * u,                        # gathers speed: arrives at full pace (a strike)
    "out": lambda u: 1.0 - (1.0 - u) * (1.0 - u),  # leaves at full pace and settles
    "lin": lambda u: u,
}


def _blend(a, b, u):
    row = {}
    for key, value in a.items():
        other = b[key]
        row[key] = _mix(value, other, u) if key[1] == "rotation" else tuple(p + (q - p) * u for p, q in zip(value, other))
    return row


def _play(length, keys):
    """Sixty poses a second between the `keys`: (time, pose, how she ARRIVES at it)."""
    out = {}
    for t in _times(length):
        row = keys[-1][1]
        for (t0, a, _), (t1, b, how) in zip(keys, keys[1:]):
            if t <= t1 + 1e-9:
                row = _blend(a, b, _EASE[how](max(0.0, min(1.0, (t - t0) / (t1 - t0)))))
                break
        _add(out, t, row)
    return out


_STAND = None


def _stand():
    """The neutral stand: frame 0 of her idle, exactly, so a one-shot blends in and out of it."""
    global _STAND
    if _STAND is None:
        _STAND = {key: keys[0][1] for key, keys in idle().items()}
    return dict(_STAND)


ON_HIP = (ARM_HIP[0], ARM_HIP[1], "chest")
ON_APRON = (ARM_APRON[0], ARM_APRON[1], "chest")      # wants a forearm reach of about 1.5
CARRY_LENGTH = 2.0


def _carry(t):
    """Carrying the slipper, at `t` seconds into the loop. See `holding_right`."""
    breath = math.sin(2.0 * math.pi * t / 1.0)
    shift = math.sin(2.0 * math.pi * t / CARRY_LENGTH)                 # her weight, onto her left foot and back
    slap = _bump(t, 0.52, 0.055) + _bump(t, 0.78, 0.055) + _bump(t, 1.04, 0.055)   # the slipper against her thigh
    ready = _bump(t, 0.40, 0.07) + _bump(t, 0.66, 0.06) + _bump(t, 0.92, 0.06)     # drawn out before each slap
    look = _window(t, 1.20, 1.90, 0.25)                                # then she looks up the street the other way
    roll = -2.6 - 1.6 * shift
    out = 0.92 + 0.20 * ready - 0.40 * slap
    return _body(at=(0.012 + 0.006 * shift, 0.0, 0.0), root=(0.0, 0.0, roll), sy=1.0 + 0.008 * breath - 0.018 * slap,
                 torso=(1.0 + 0.6 * breath, -4.0, -0.7 * roll),
                 head=(-4.0 - 0.5 * breath, -12.0 + 30.0 * look, -0.5 * roll),
                 legl=(0.0, 3.0 - roll), legr=(-3.0 - 5.0 * ready + 4.0 * slap, -5.0 - roll),
                 left=ON_HIP,
                 right=("hinge", (out, -0.50, 0.08 + 0.10 * ready), 18.0 + 12.0 * ready - 10.0 * slap, -4.0 + 14.0 * ready - 30.0 * slap),
                 reach=(1.0, 1.30))


def _holding():
    return _carry(0.0)


def holding_right():
    """Carrying the slipper, a 2 s loop. Her left fist is on her hip and her weight on her left
    leg; the slipper hangs LOW AND WIDE in her right hand, out from her thigh where the child
    it is for can see it from anywhere on the street. She is waiting, and not patiently: three
    times she draws it out and slaps it back against her thigh, her right foot lifting and
    coming down with each one, and then she turns her head and looks up the street the other
    way to see who else needs it."""
    out = {}
    for t in _times(CARRY_LENGTH):
        _add(out, t, _carry(t if t < CARRY_LENGTH - 1e-9 else 0.0))
    return out


def holding_right_shoot():
    """THE THROW. A mother's throw: OVERARM, and her left fist never leaves her hip for it. The
    slipper goes straight up beside her head as she sinks onto her heels (0.033), then all of
    her tips forward over her left foot with her right foot kicked up behind her, the forearm
    snapping straight as it leaves (0.067); she follows it down with the foot still rising
    (0.10) and is back in the carry. What the game keeps of this is the root, the legs and the
    forearms, so the sink, the tip and the kicked-up foot are where the throw is."""
    hold = _holding()
    back = _body(at=(0.006, 0.0, -0.020), root=(-5.0, -6.0, -2.0), sy=0.92, torso=(-6.0, -12.0, 0.0), head=(-6.0, 10.0, 0.0),
                 legl=(-5.0, 4.0), legr=(4.0, -4.0), left=ON_HIP,
                 right=("hinge", (0.84, 0.22, -0.30), 10.0, 78.0), reach=(1.0, 1.15))
    out = _body(at=(0.0, 0.012, 0.020), root=(20.0, 8.0, 0.0), sy=1.09, torso=(10.0, 14.0, 0.0), head=(-24.0, -12.0, 0.0),
                legl=(-20.0, 3.0), legr=(34.0, -5.0), left=ON_HIP,
                right=("hinge", (0.42, 0.46, 0.78), 3.0, 0.0), reach=(1.0, 1.60))
    through = _body(at=(0.0, 0.004, 0.022), root=(22.0, 10.0, 0.0), sy=0.95, torso=(15.0, 18.0, 0.0), head=(-26.0, -14.0, 0.0),
                    legl=(-22.0, 3.0), legr=(44.0, -5.0), left=ON_HIP,
                    right=("hinge", (0.16, -0.50, 0.85), 14.0, 0.0), reach=(1.0, 1.18))
    return _play(0.2, [(0.0, hold, "lin"), (2 / 60.0, back, "out"), (4 / 60.0, out, "in"),
                       (6 / 60.0, through, "out"), (0.2, hold, "s")])


def pick_up():
    """She does not bend over in front of the whole street, and she does not take her fist off
    her hip. She TIPS, all in one piece, over onto her right foot like a teapot being poured:
    her left leg comes up off the street out to the side as the counterweight, her right arm
    goes straight down and takes the slipper off the ground beside her foot (0.167), and her
    head stays level with her eyes on the game the whole time. Then she rocks back upright,
    past it with the slipper brought up in front of her, and into her stand."""
    low = _body(at=(0.062, 0.040, 0.0), root=(4.0, 0.0, 26.0), sy=0.93, torso=(8.0, -6.0, 24.0), head=(-6.0, 6.0, -44.0),
                legl=(0.0, 40.0), legr=(0.0, -22.0), left=ON_HIP,
                right=((0.56, -0.82, 0.10), (0.44, -0.90, 0.06)), reach=(1.0, 1.60))
    up = _body(sy=1.05, torso=(-4.0, 6.0, 0.0), head=(-8.0, -6.0, 0.0), left=ON_HIP,
               right=((0.50, -0.56, 0.66), (0.10, 0.42, 0.90)), reach=(1.0, 1.15))
    return _play(1.0 / 3.0, [(0.0, _stand(), "lin"), (10 / 60.0, low, "s"), (12 / 60.0, low, "lin"),
                             (16 / 60.0, up, "out"), (1.0 / 3.0, _stand(), "s")])


def attack_melee_right():
    """THE TAG. It is a ruling: "IKAW. Out." She gathers with her elbow drawn back and her
    weight on her heels (0.13), then goes up onto her toes and her whole solid self tips in
    behind one long level arm pointed straight at the child (0.25), her other arm thrown
    straight back behind her to keep her on her feet. Held so it can be seen, then home. The
    legs do little and the forearms are plain hinges: the game lays its own chest, head and
    upper arms over this."""
    wind = _body(at=(0.0, 0.0, -0.020), root=(-4.0, -8.0, 0.0), sy=0.92, torso=(-2.0, -14.0, 0.0), head=(-4.0, 16.0, 0.0),
                 legl=(-4.0, 3.0), legr=(4.0, -3.0),
                 left=("hinge", (0.60, -0.62, 0.50), 50.0, 0.0),
                 right=("hinge", (0.60, -0.36, -0.72), 70.0, 8.0))
    out = _body(at=(0.0, 0.016, 0.044), root=(9.0, 9.0, 0.0), sy=1.07, torso=(8.0, 16.0, 0.0), head=(-15.0, -22.0, 0.0),
                legl=(9.0, 3.0), legr=(-8.0, -3.0), left=("hinge", (0.50, -0.42, -0.76), 8.0, 0.0),
                right=("hinge", (0.14, 0.03, 0.99), 3.0, 0.0), reach=(1.20, 1.45))
    held = _body(at=(0.0, 0.010, 0.038), root=(7.0, 8.0, 0.0), sy=1.0, torso=(8.0, 14.0, 0.0), head=(-13.0, -20.0, 0.0),
                 legl=(8.0, 3.0), legr=(-7.0, -3.0), left=("hinge", (0.52, -0.46, -0.72), 14.0, 0.0),
                 right=("hinge", (0.16, 0.0, 0.99), 9.0, 5.0), reach=(1.10, 1.25))
    return _play(5.0 / 12.0, [(0.0, _stand(), "lin"), (8 / 60.0, wind, "s"), (15 / 60.0, out, "in"),
                              (18 / 60.0, held, "out"), (5.0 / 12.0, _stand(), "s")])


def attack_melee_left():
    """THE SHOVE is how she clears children off her step: "TABI!" Her right fist stays on her
    hip. Her left hand is drawn up across her to her right shoulder as she sinks and turns away
    (0.13), and then the whole arm is swept out and down across the front of her, a backhand
    that ends low and wide to her front left (0.25), with a step in behind it and her chest
    turned after it."""
    wind = _body(at=(0.0, 0.0, -0.014), sy=0.91, torso=(4.0, -24.0, 0.0), head=(-4.0, 20.0, 0.0),
                 legl=(3.0, 3.0), legr=(-2.0, -3.0),
                 left=((0.34, -0.36, 0.87), (-0.84, 0.50, 0.20)), right=ON_HIP)
    out = _body(at=(0.050, 0.0, 0.050), root=(8.0, 0.0, -18.0), sy=1.06, torso=(10.0, 38.0, -10.0), head=(-10.0, -6.0, 16.0),
                legl=(-14.0, 16.0), legr=(10.0, 6.0),
                left=((0.84, -0.06, 0.54), (0.88, -0.10, 0.46)), reach=(1.60, 1.0), right=ON_HIP)
    held = _body(at=(0.044, 0.0, 0.044), root=(7.0, 0.0, -15.0), sy=0.99, torso=(9.0, 34.0, -9.0), head=(-9.0, -4.0, 14.0),
                 legl=(-12.0, 14.0), legr=(9.0, 5.0),
                 left=((0.88, -0.12, 0.46), (0.94, -0.16, 0.30)), reach=(1.35, 1.0), right=ON_HIP)
    return _play(5.0 / 12.0, [(0.0, _stand(), "lin"), (8 / 60.0, wind, "s"), (15 / 60.0, out, "in"),
                              (18 / 60.0, held, "out"), (5.0 / 12.0, _stand(), "s")])


def interact_right():
    """Over the counter. Her left hand goes into her apron pocket, her right elbow stays tucked
    at her side and the forearm comes out level at her waist (0.167), and she raps down twice
    on whatever is in front of her, the way she counts change out onto the counter, looking
    down at it. Then it is put away."""
    def pose(fore, reach, lean, nod):
        return _body(sy=1.0 - 0.004 * lean, torso=(lean, 9.0, 0.0), head=(nod, -5.0, 0.0),
                     left=ON_APRON, right=((0.40, -0.88, 0.24), fore), reach=(1.45, reach))
    over = pose((0.10, 0.24, 0.96), 1.30, 3.0, 2.0)
    rest = pose((0.08, 0.10, 0.99), 1.20, 4.0, 6.0)
    rap = pose((0.06, -0.44, 0.90), 1.30, 9.0, 12.0)
    return _play(2.0 / 3.0, [(0.0, _stand(), "lin"), (10 / 60.0, over, "out"), (14 / 60.0, rap, "in"),
                             (19 / 60.0, rest, "out"), (23 / 60.0, rap, "in"), (27 / 60.0, rest, "out"),
                             (2.0 / 3.0, _stand(), "s")])


def interact_left():
    """"Akin na." Give it here. Her right fist goes to her hip and her left hand goes out in
    front of her, low and long with the palm up, her chest leaning in behind it and her chin
    up (0.167). Then the forearm curls up toward her twice, quick: she is not going to ask a
    third time."""
    def pose(fore, reach, lean, chin):
        return _body(sy=1.0 - 0.004 * lean, torso=(lean, -14.0, 3.0), head=(chin, 10.0, -5.0),
                     left=((0.36, -0.52, 0.78), fore), right=ON_HIP, reach=(reach, 1.0))
    out = pose((0.10, -0.12, 0.99), 1.60, 10.0, -12.0)
    rest = pose((0.10, -0.06, 0.99), 1.45, 8.0, -10.0)
    curl = pose((0.04, 0.80, 0.60), 1.25, 4.0, -15.0)
    return _play(2.0 / 3.0, [(0.0, _stand(), "lin"), (10 / 60.0, out, "out"), (14 / 60.0, rest, "s"),
                             (18 / 60.0, curl, "in"), (22 / 60.0, rest, "out"), (25 / 60.0, curl, "in"),
                             (29 / 60.0, rest, "out"), (2.0 / 3.0, _stand(), "s")])


def slide():
    """She does not dive and she does not sit: she goes in ON HER HIP, the way she gets under
    the shutter of her own store. She gathers with her arms back (0.08), drops onto her left
    hip with both legs out ahead of her and her left forearm on the street (down by 0.25), and
    goes through propped on that elbow with her right hand out ahead for the slipper (deepest
    at 0.37).
    Then she rocks up onto her feet, slaps the dust off her apron with both hands, and is
    standing again."""
    gather = _body(sy=0.90, torso=(14.0, 0.0, 0.0), head=(-10.0, 0.0, 0.0), legl=(0.0, 5.0), legr=(0.0, -5.0),
                   left=((0.70, -0.50, -0.50), (0.40, -0.60, 0.70)), right=((0.70, -0.50, -0.50), (0.40, -0.60, 0.70)))
    down = _body(hip=(0.03, 0.142, -0.02), root=(-10.0, 0.0, -62.0), sy=1.04, torso=(4.0, 0.0, 12.0), head=(0.0, 0.0, 30.0),
                 legl=(-80.0, 0.0), legr=(-58.0, -6.0),
                 left=((0.92, -0.38, 0.0), (0.40, -0.20, 0.90)), reach=(1.0, 1.40),
                 right=((0.30, 0.10, 0.95), (0.10, 0.04, 0.99)))
    deep = _body(hip=(0.03, 0.138, -0.02), root=(-12.0, 0.0, -74.0), sy=1.0, torso=(6.0, 0.0, 14.0), head=(0.0, 0.0, 38.0),
                 legl=(-82.0, 0.0), legr=(-50.0, -8.0),
                 left=((0.95, -0.28, 0.0), (0.34, -0.10, 0.93)), reach=(1.0, 1.60),
                 right=((0.24, 0.06, 0.97), (0.06, 0.0, 1.0)))
    rock = _body(hip=(0.0, 0.150, 0.0), root=(6.0, 0.0, -8.0), sy=0.90, torso=(30.0, 0.0, 4.0), head=(-18.0, 0.0, 0.0),
                 legl=(-40.0, 12.0), legr=(-40.0, -12.0),
                 left=((0.78, -0.40, 0.48), (0.20, -0.80, 0.56)), right=((0.78, -0.40, 0.48), (0.20, -0.80, 0.56)))
    dust = _body(sy=1.04, torso=(8.0, 0.0, 0.0), head=(10.0, 0.0, 0.0), left=ON_APRON, right=ON_APRON, reach=(1.6, 1.4))
    dust2 = _body(sy=0.98, torso=(6.0, 0.0, 0.0), head=(8.0, 0.0, 0.0), left=ON_APRON, right=ON_APRON, reach=(1.4, 1.6))
    return _play(0.95, [(0.0, _stand(), "lin"), (5 / 60.0, gather, "s"), (15 / 60.0, down, "in"), (22 / 60.0, deep, "out"),
                        (27 / 60.0, deep, "lin"), (36 / 60.0, rock, "s"), (44 / 60.0, dust, "s"), (49 / 60.0, dust2, "s"),
                        (0.95, _stand(), "s")])


CROUCH_LENGTH = 1.8


def _winded(lift, flap):
    """Bent over with her left hand braced on her knee, `lift` of the way up out of it (a
    breath), her right hand at her face and `flap` (-1 to 1) of the way through a fan of it."""
    return _body(at=(0.0, -0.004, -0.012), sy=0.90 + 0.06 * lift, torso=(40.0 - 13.0 * lift, 6.0, 0.0),
                 head=(-20.0 + 10.0 * lift, -6.0, 0.0), legl=(-3.0, 13.0), legr=(-3.0, -13.0),
                 left=((0.94, -0.06, 0.10), (-0.22, -0.96, -0.12)),
                 right=((0.62, -0.50, 0.60), (-0.30 + 0.20 * flap, 0.66 + 0.10 * flap, 0.68 - 0.22 * flap)),
                 reach=(1.10, 1.25))


def crouch():
    """Out of breath, 1.8 s: her feet apart, bent over with her left hand braced on her knee and
    her elbow out, and her right hand up FANNING HER FACE, quick and cross, because the heat is
    somebody's fault. Two breaths, each a quick heave up and a long sag back down, the second
    one bigger. The first and last frames are the full bent pose: the game loops this while she
    is tired and holds its last frame as an emote."""
    out = {}
    period = CROUCH_LENGTH / 2.0
    for t in _times(CROUCH_LENGTH):
        p = (t % period) / period if t < CROUCH_LENGTH - 1e-9 else 0.0
        lift = _ease(p / 0.34) * (1.0 - _ease((p - 0.34) / 0.66))
        lift *= 0.75 if t < period else 1.15
        fan = math.sin(2.0 * math.pi * 5.0 * t / CROUCH_LENGTH)
        _add(out, t, _winded(lift, fan))
    return out


def _seated(height, sy, fold=1.0, chin=-8.0):
    """Sat on the street with her legs out in a V and her fists `fold` of the way onto her hips."""
    rest = ((0.66, -0.60, 0.46), (0.30, -0.70, 0.64))
    akimbo = ((0.96, -0.22, -0.16), (0.30, -0.92, 0.24))
    arm = tuple(tuple(p + (q - p) * fold for p, q in zip(u, v)) for u, v in zip(rest, akimbo))
    return _body(hip=(0.0, height, -0.03), root=(-7.0, 0.0, 0.0), sy=sy, torso=(3.0, 0.0, 0.0), head=(chin, 0.0, 0.0),
                 legl=(-83.0, 24.0), legr=(-83.0, -24.0), left=(arm[0], arm[1], "chest"), right=(arm[0], arm[1], "chest"),
                 reach=(1.0 + 0.10 * fold, 1.0 + 0.10 * fold))


def sit():
    """Getting down onto the street, 0.8 s, and ending the way she sits on her own doorstep:
    legs out in a V, back straight, BOTH FISTS ON HER HIPS with the elbows out, chin up, still
    in charge. She bends with both hands going back for the ground (0.17), drops (0.40), lands
    and squashes (0.55), and as she bounces up out of it her fists go to her hips and her chin
    comes up."""
    brace = _body(at=(0.0, 0.0, -0.016), sy=0.92, torso=(26.0, 0.0, 0.0), head=(-14.0, 0.0, 0.0), legl=(-6.0, 9.0), legr=(-6.0, -9.0),
                  left=((0.70, -0.46, -0.55), (0.30, -0.80, -0.52)), right=((0.70, -0.46, -0.55), (0.30, -0.80, -0.52)))
    half = _body(hip=(0.0, 0.150, -0.045), root=(-12.0, 0.0, 0.0), sy=0.98, torso=(16.0, 0.0, 0.0), head=(-8.0, 0.0, 0.0),
                 legl=(-36.0, 16.0), legr=(-36.0, -16.0),
                 left=((0.66, -0.50, -0.56), (0.30, -0.84, -0.46)), right=((0.66, -0.50, -0.56), (0.30, -0.84, -0.46)),
                 reach=(1.2, 1.2))
    return _play(0.8, [(0.0, _stand(), "lin"), (10 / 60.0, brace, "s"), (24 / 60.0, half, "s"),
                       (33 / 60.0, _seated(0.088, 0.87, 0.0, 4.0), "in"), (39 / 60.0, _seated(0.104, 1.04, 0.7, -2.0), "out"),
                       (0.8, _seated(0.095, 1.0), "s")])


def die():
    """Knocked down, and she is "very hard to knock down", so it is a comedy when it happens:
    her feet leave the street with her arms windmilling (the top is at 0.100), she goes over
    FORWARD and lands flat on her front (0.267) with a bounce, and stays there with her seat
    up, her feet stuck in the air behind her, her right arm flat out on the street, and her
    left forearm still standing straight up beside her with a finger in it. She has not
    finished."""
    flat = ((0.62, -0.10, 0.78), (0.50, -0.06, 0.86))
    up = _body(at=(0.0, 0.105, -0.03), root=(22.0, 0.0, 0.0), sy=1.10, torso=(8.0, 0.0, 0.0), head=(-24.0, 0.0, 0.0),
               legl=(34.0, 6.0), legr=(20.0, -6.0),
               left=((0.84, 0.30, -0.45), (0.50, 0.84, -0.20)), right=((0.84, 0.30, 0.45), (0.46, 0.84, 0.28)), reach=(1.3, 1.3))
    hit = _body(at=(0.0, 0.110, -0.12), root=(92.0, 0.0, 0.0), sy=0.92, torso=(-4.0, 0.0, 0.0), head=(-34.0, 0.0, 0.0),
                legl=(26.0, 8.0), legr=(40.0, -8.0),
                right=flat, left=((0.90, -0.04, 0.40), (0.80, 0.10, 0.56)), reach=(1.2, 1.2))
    bounce = _body(at=(0.0, 0.150, -0.12), root=(98.0, 0.0, 0.0), sy=1.03, torso=(-8.0, 0.0, 0.0), head=(-38.0, 10.0, 0.0),
                   legl=(62.0, 8.0), legr=(40.0, -8.0),
                   right=flat, left=((0.96, 0.04, 0.24), (0.50, 0.80, 0.30)), reach=(1.4, 1.1))
    rest = _body(at=(0.0, 0.118, -0.12), root=(97.0, 0.0, 0.0), sy=1.0, torso=(-6.0, 0.0, 0.0), head=(-40.0, 22.0, 0.0),
                 legl=(48.0, 8.0), legr=(74.0, -8.0),
                 right=flat, left=((0.98, 0.02, 0.16), (0.10, 0.99, 0.04)), reach=(1.85, 1.1))
    return _play(1.0 / 3.0, [(0.0, _stand(), "lin"), (6 / 60.0, up, "out"), (16 / 60.0, hit, "in"),
                             (18 / 60.0, bounce, "out"), (1.0 / 3.0, rest, "s")])


def emote_yes():
    """YES goes in the book, a 1.2 s loop. Her left forearm is held out level in front of her
    (the notebook from her apron) and her right hand is up at her ear, where the pencil lives.
    Down it comes and TICKS the book, with a nod and a dip of her whole body; then it flicks up
    and out wide beside her with her chin coming up, pleased; and again. "Lista." It is on
    your tab."""
    book = ((0.50, -0.74, 0.45), (-0.30, 0.06, 0.95))
    def pose(right, reach, sy, lean, chin, sway):
        return _body(at=(0.004 * sway, 0.0, 0.0), root=(0.0, 0.0, 2.0 * sway), sy=sy, torso=(lean, 5.0 + 4.0 * sway, -1.5 * sway),
                     head=(chin, -4.0 - 6.0 * sway, 2.0 * sway), legl=(0.0, 3.0 - 2.0 * sway), legr=(0.0, -3.0 - 2.0 * sway),
                     left=book, right=right, reach=(1.30, reach))
    ear = pose(((0.90, 0.02, 0.42), (-0.20, 0.96, 0.16)), 1.20, 1.02, -2.0, -6.0, 0.0)
    tick = pose(((0.50, -0.62, 0.60), (-0.62, -0.30, 0.72)), 1.30, 0.93, 9.0, 16.0, 1.0)
    tick2 = pose(((0.50, -0.62, 0.60), (-0.50, -0.34, 0.80)), 1.36, 0.94, 8.0, 14.0, 1.0)
    flick = pose(((0.88, -0.04, 0.46), (0.62, 0.74, 0.26)), 1.50, 1.05, -5.0, -12.0, -1.0)
    held = pose(((0.88, -0.08, 0.46), (0.54, 0.80, 0.26)), 1.38, 1.0, -3.0, -8.0, -0.6)
    return _play(1.2, [(0.0, ear, "lin"), (9 / 60.0, tick, "in"), (13 / 60.0, tick2, "lin"), (22 / 60.0, flick, "out"),
                       (30 / 60.0, held, "s"), (38 / 60.0, tick, "in"), (42 / 60.0, tick2, "lin"), (51 / 60.0, flick, "out"),
                       (60 / 60.0, held, "s"), (1.2, ear, "s")])


def emote_no():
    """NO is a ruling, a 1.0 s loop: the wave-off. Her forearms cross in front of her chest as
    she sinks, and then both arms are THROWN OUT wide and low to either side, flat, with her
    chest going back and her head turning hard away; crossed again, thrown out again, her head
    going the other way. "Wala. Hindi puwede." Nobody argues it twice."""
    def pose(open_, turn, sway):
        if open_:
            left = right = ((0.90, -0.26, 0.34), (0.94, -0.26, 0.20))
            return _body(at=(0.004 * sway, 0.0, 0.0), root=(-2.0, 0.0, 1.5 * sway), sy=1.04, torso=(-6.0, 0.3 * turn, 0.0),
                         head=(-6.0, turn, 0.0), legl=(0.0, 4.0), legr=(0.0, -4.0), left=left, right=right, reach=(1.45, 1.45))
        return _body(at=(0.004 * sway, 0.0, 0.0), root=(0.0, 0.0, 1.5 * sway), sy=0.93, torso=(8.0, 0.3 * turn, 0.0),
                     head=(8.0, turn, 0.0), legl=(0.0, 3.0), legr=(0.0, -3.0),
                     left=((0.56, -0.66, 0.50), (-0.80, 0.42, 0.42)), right=((0.56, -0.60, 0.56), (-0.76, 0.30, 0.58)),
                     reach=(1.25, 1.30))
    return _play(1.0, [(0.0, pose(False, 0.0, 0.0), "lin"), (8 / 60.0, pose(True, -22.0, -1.0), "in"),
                       (20 / 60.0, pose(True, -16.0, -0.6), "s"), (30 / 60.0, pose(False, 0.0, 0.0), "s"),
                       (38 / 60.0, pose(True, 22.0, 1.0), "in"), (50 / 60.0, pose(True, 16.0, 0.6), "s"),
                       (1.0, pose(False, 0.0, 0.0), "s")])


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
         "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
         "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
         "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
         "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no}
