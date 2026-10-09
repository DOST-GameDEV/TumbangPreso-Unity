"""New locomotion clips for the Jun-Jun redesign PROTOTYPE.

Imported by tools/author_character_redesign_jun_jun.py, which writes them into the prototype
.glb IN PLACE OF the clips of the same name copied from character-male-d.glb. Nothing in the
game reads them; character-male-d.glb keeps its own. The thirteen ACTION clips (the carry, the
throw, the tag, slide, sit, the emotes ...) were added on 2026-10-07 and are further down, under
"THE ACTION CLIPS"; the other fifteen in the .glb are copied across untouched. The helpers
are the approved Bebang prototype's, copied; each character's motion is its OWN
(docs/CHARACTER_REDESIGN_DANTE.md section 13 rule 9), so none of the numbers below are another's.

WHAT HIS MOTION SHOULD SAY ABOUT HIM. JUN-JUN is "the youngest on the street. Small, slippery,
and impossible to corner. Also impossible to keep upright" (the character select), and "the
sleepy kid dragged out to play: a shuffle, head down, arms hardly bothering; a reluctant jog"
(`GaitStyles.JunJun`). He is about seven and it is past his nap. So:
  * HE IS HALF ASLEEP ON HIS FEET. Chin down, shoulders dropped, arms hanging with nothing in
    them, feet close together and turned in a little. Nothing about him is braced.
  * HE DOES NOT STAY UPRIGHT. His weight wanders: he tips, sways and catches himself, and the
    catch is the liveliest thing he does.
  * WHEN HE DOES RUN HE IS SEVEN. All legs, arms left behind him, nobody can get a hand on him.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK
and tips the head or the body FORWARD; about +z a negative angle drops his left arm from
straight out to hanging and a positive one leans a body to his RIGHT; about +y a positive
angle turns him to his LEFT.

ARMS UP GO THROUGH THE ELBOW (section 13 rule 8, amended by the owner on Dante: "the jump looks
like he's shrugging"). His elbow is 82 mm from the shoulder, where the jacket's sleeve is cut
in two; the lower sleeve, the shirt cuff and the fist ride the forearm bone, 116 mm more to the
fist's end. A straight arm cannot rise above level (his ears are over it), so the upper arm
lifts a little and the forearm folds up, and may stretch (scale on the forearm bone) as it
reaches: the heroes' arm is short and his face is a long way from his shoulder.

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
#   HANGING, HIS WAY: dropped, nothing held, the fists a little in front of the jacket's skirt.
ARMS_HANG = ((0.62, -0.78, 0.04), (0.50, -0.85, 0.16))
#   THE EYE. His right arm (written as a left arm, mirrored below): the elbow lifted out in
#   front of him and the fist brought up and in to his own eye.
ARM_EYE = ((0.55, 0.22, 0.80), (-0.40, 0.77, 0.50))
EYE_REACH = 0.90
#   THE YAWN. His left fist in front of his mouth, and his right arm (written as a left arm)
#   pushed out to the side and up, the way a stretch escapes with a yawn.
ARM_MOUTH = ((0.50, 0.04, 0.86), (-0.66, 0.55, 0.51))
MOUTH_REACH = 0.85
ARM_STRETCH = ((0.82, 0.50, -0.14), (0.74, 0.66, -0.10))
STRETCH_REACH = 0.40
#   THE CATCH. Both arms thrown out for balance when he wakes up falling.
ARM_FLAIL = ((0.94, 0.24, 0.12), (0.86, 0.48, 0.16))
IDLE_LENGTH = 8.0
STAND_NOD = 7.0       # degrees his chin is down when he is just standing
TOE_IN = 6.0          # degrees each foot is turned in


def idle():
    """Eight seconds of a small boy who should be in bed, in three things he does:

      HE RUBS HIS EYE (0.5 to 2.7 s). His right fist comes up to his right eye and grinds at
      it, three small turns, his head tipped over into the fist and his weight gone onto that
      side. The other arm hangs.
      HE YAWNS (2.9 to 5.3 s). His head goes back, his chest fills (the whole of him stretches
      taller), his left fist comes up in front of his mouth and his right arm pushes out to
      the side on its own. Then all of it lets go at once and he sags lower than he started.
      HE NODS OFF, AND CATCHES HIMSELF (5.5 to 7.9 s). His chin sinks, and sinks, and the whole
      of him starts to tip over to his left from the feet. At 7.1 s he wakes up falling: a
      hop, both arms thrown out, head snapped up, one fast look each way to see who saw. Then
      he settles back to where he began.

    WHY THESE. He is "the sleepy kid dragged out to play" and "impossible to keep upright": the
    roster's lightest, tatag 2. So he waits for his turn the way a seven-year-old waits when it
    is past his nap, and the one moment he is quick is the moment he nearly goes over, which is
    also who he is in the game (small, slippery, down a lot). None of the three is another
    character's (Dante: fists on hips and folded arms; Sean: a flex, a bull lean, a guard;
    Amihan and Cheska: hops and hands behind the back; Bebang: fist into palm, the headband, a
    shoulder roll; Bayan and Lola Pacing have their own). All through it his feet are close
    together and turned in, never planted.

    IT IS 8 s, NOT THE OLD 1.33 s (see Dante's). Frame 0 is the neutral stand.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 4.0))
        rub = _window(t, 0.5, 2.7, 0.45)
        grind = math.sin(2.0 * math.pi * (t - 1.0) / 0.42) * _window(t, 1.0, 2.3, 0.2)
        yawn = _window(t, 2.9, 5.3, 0.55)
        peak = _window(t, 3.4, 4.7, 0.5)                 # the top of the yawn
        sag = _bump(t, 5.25, 0.22)                       # letting go of it
        # nodding off: a slow slide that gets faster, cut off by the catch
        doze = _ease((t - 5.5) / 1.6) ** 1.6 * (1.0 - _ease((t - 7.08) / 0.10))
        bob = 0.18 * _bump(t, 6.35, 0.10) * doze         # one half catch on the way down
        catch = _ease((t - 7.08) / 0.10) * (1.0 - _ease((t - 7.34) / 0.42))
        hop = _bump(t, 7.20, 0.085)
        land = _bump(t, 7.42, 0.07)
        glance = _window(t, 7.22, 7.52, 0.12) - _window(t, 7.50, 7.86, 0.14)
        row = {}
        hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
        eye = _arm(-1, *ARM_EYE)
        mouth = _arm(1, *ARM_MOUTH)
        stretch = _arm(-1, *ARM_STRETCH)
        for side, name in ((1, "left"), (-1, "right")):
            flail = _arm(side, *ARM_FLAIL)
            first = hang[side] if side > 0 else eye
            second = mouth if side > 0 else stretch
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(_mix(_mix(hang[side][k], first[k], rub), second[k], yawn), flail[k], catch)
                if k == 1 and side < 0:
                    q = mul(q, qz(7.0 * grind * rub))           # the fist grinding at the eye
                if k == 0:
                    # asleep, the arms hang closer and swing in a little with the tip of his body
                    q = mul(q, qz(side * 5.0 * doze))
                row[(bone, "rotation")] = q
            reach = (EYE_REACH * rub if side < 0 else 0.0) + (MOUTH_REACH if side > 0 else STRETCH_REACH) * yawn * (0.75 + 0.25 * peak)
            row[("forearm-" + name, "scale")] = (1.0 + reach + 0.25 * catch, 1.0, 1.0)
        nod = STAND_NOD + 6.0 * rub - 26.0 * peak * yawn + 9.0 * sag + 27.0 * (doze - bob) - 13.0 * catch + 2.0 * land
        tilt = 12.0 * rub + 2.5 * grind * rub - 9.0 * doze + 3.0 * catch
        look = -6.0 * rub + 34.0 * glance
        lean = 5.0 * rub - 11.5 * (doze - 0.5 * bob) + 3.0 * catch          # degrees to his right, from the feet
        tall = 0.050 * peak * yawn - 0.050 * sag - 0.030 * doze + 0.075 * hop - 0.060 * land
        row.update({
            ("root", "translation"): (0.0, 0.060 * hop, 0.0),
            ("root", "scale"): _squash(1.0 + 0.008 * breath + tall),
            ("root", "rotation"): qz(lean),
            ("torso", "rotation"): mul(mul(qx(4.0 + 3.0 * rub - 9.0 * peak * yawn + 5.0 * sag + 7.0 * doze - 5.0 * catch + 0.8 * breath),
                                           qy(-5.0 * rub + 0.2 * look)), qz(4.0 * rub - 3.5 * doze)),
            ("head", "rotation"): mul(mul(qy(0.8 * look), qx(nod - 0.6 * breath)), qz(tilt)),
            # feet close together and turned in; on the catch they jump apart
            ("leg-left", "rotation"): mul(mul(qz(1.5 + 7.0 * catch - 0.6 * lean), qy(-TOE_IN)), qx(-4.0 * hop)),
            ("leg-right", "rotation"): mul(mul(qz(-1.5 - 7.0 * catch - 0.6 * lean), qy(TOE_IN)), qx(-4.0 * hop)),
        })
        _add(out, t, row)
    return out


def walk():
    """A shuffle. He is being walked somewhere he did not ask to go: short steps that hardly
    leave the ground, chin on his tie, the whole of him rocking from one foot to the other
    like a boat, and his arms hanging off his shoulders and swinging late, with nothing in them."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        late = math.sin(phase - 0.75)             # the arms and the head trail the legs
        lift = abs(s) ** 1.6
        rock = math.sin(phase - 0.35)
        row = {
            ("root", "translation"): (0.016 * rock, 0.012 * lift, 0.0),
            ("root", "rotation"): mul(qx(4.0), qz(4.2 * rock)),
            ("root", "scale"): _squash(0.975 + 0.030 * lift),
            ("leg-left", "rotation"): mul(mul(qx(-21.0 * s), qz(1.5 - 4.2 * rock)), qy(-TOE_IN)),
            ("leg-right", "rotation"): mul(mul(qx(21.0 * s), qz(-1.5 - 4.2 * rock)), qy(TOE_IN)),
            ("torso", "rotation"): mul(mul(qx(6.0), qy(4.0 * s)), qz(-2.0 * rock)),
            ("head", "rotation"): mul(mul(qx(11.0 + 2.5 * abs(c)), qy(-3.0 * late)), qz(-5.0 * late)),
        }
        hang = {1: _arm(1, *ARMS_HANG), -1: _arm(-1, *ARMS_HANG)}
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(qx(side * 9.0 * late), hang[side][0])
            row[("forearm-" + name, "rotation")] = mul(hang[side][1], qy(-side * (6.0 + 5.0 * late * side)))
        _add(out, t, row)
    return out


#   the run: where the left arm points, thrown out behind him
ARM_BEHIND = ((0.62, -0.42, -0.66), (0.50, -0.30, -0.81))


def sprint():
    """Now he is awake. A seven-year-old flat out: head and chest pitched ahead of his feet,
    legs going faster than the rest of him, and both arms left out behind him like a bird's,
    flapping with each step. Nobody can get a hand on that."""
    out = {}
    length = 0.48
    back = {1: _arm(1, *ARM_BEHIND), -1: _arm(-1, *ARM_BEHIND)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.7)
        lift = abs(s) ** 1.2
        rock = math.sin(phase - 0.3)
        flap = math.sin(2.0 * phase - 0.6)
        row = {
            ("root", "translation"): (0.006 * rock, 0.004 + 0.040 * lift, 0.0),
            ("root", "rotation"): mul(qx(17.0), qz(2.4 * rock)),
            ("root", "scale"): _squash(0.95 + 0.09 * lift),
            ("leg-left", "rotation"): mul(qx(-58.0 * sh), qz(2.0 - 2.4 * rock)),
            ("leg-right", "rotation"): mul(qx(58.0 * sh), qz(-2.0 - 2.4 * rock)),
            ("torso", "rotation"): mul(mul(qx(7.0), qy(8.0 * sh)), qz(-2.5 * rock)),
            ("head", "rotation"): mul(mul(qx(-21.0 + 2.0 * lift), qy(-5.0 * sh)), qz(2.0 * rock)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(back[side][0], qz(side * (6.0 * flap + 3.0 * sh * side)))
            row[("forearm-" + name, "rotation")] = mul(back[side][1], qz(side * 7.0 * flap))
            row[("forearm-" + name, "scale")] = (1.12, 1.0, 1.0)
        _add(out, t, row)
    return out


#   arms up: where the left arm points at the top of the jump. His ears stand out past his
#   shoulders, so the V is thrown up and out, clear of them.
#   (v02's forearm leaned out more and reached less, and from the front his fists sat beside his ears)
#   (v03's upper arm was 6 degrees nearer his head and at take-off the fists covered his cheeks)
ARM_UP = ((0.87, 0.40, 0.06), (0.48, 0.87, 0.04))
ARM_UP_REACH = 0.92


def jump():
    """A STANDING jump (owner, 2026-10-05: "i need a jump for standing still"): both feet together
    and the same, never a stride. He weighs nothing, so he goes up like a cork: a long stretch
    off both feet, both arms flung up in a V through the elbows and overshooting, and then his
    knees tucked up under him and his head thrown back while he hangs, enjoying it."""
    out = {}
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(0.50):
        rise = math.exp(-t / 0.11)                 # 1 at take-off, easing away
        hang = 1.0 - math.exp(-t / 0.12)
        ring = math.cos(2.0 * math.pi * t / 0.40) * math.exp(-t / 0.17)
        row = {
            ("root", "scale"): _squash(1.0 + 0.24 * ring),
            ("root", "rotation"): qx(-4.0 * hang),
            # both legs the same: straight under him at take-off, then tucked up in front
            ("leg-left", "rotation"): mul(qx(12.0 * rise - 34.0 * hang), qz(1.5 + 4.0 * hang)),
            ("leg-right", "rotation"): mul(qx(12.0 * rise - 34.0 * hang), qz(-1.5 - 4.0 * hang)),
            ("torso", "rotation"): qx(-8.0 * rise - 3.0 * hang),
            ("head", "rotation"): qx(-16.0 * rise - 9.0 * hang),
        }
        for side, name in ((1, "left"), (-1, "right")):
            row[("arm-" + name, "rotation")] = mul(up[side][0], qz(side * 5.0 * ring))
            # the forearm overshoots open at take-off and settles into the V
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 12.0 * ring))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.72 + 0.28 * rise), 1.0, 1.0)
        _add(out, t, row)
    return out


def fall():
    """A loop: he comes down the way he does everything, not quite in charge of it. Arms up in
    the V and windmilling a little, one after the other; legs pedalling under him; the whole of
    him rocking; looking down at where he is going to land on his seat."""
    out = {}
    length = 1.0 / 3.0
    up = {1: _arm(1, *ARM_UP), -1: _arm(-1, *ARM_UP)}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.05 + 0.016 * math.sin(2.0 * phase)),
            ("root", "rotation"): mul(qx(-5.0), qz(3.0 * s)),
            ("leg-left", "rotation"): mul(qx(-20.0 + 13.0 * s), qz(5.0)),
            ("leg-right", "rotation"): mul(qx(-20.0 - 13.0 * s), qz(-5.0)),
            ("torso", "rotation"): mul(qx(3.0), qz(-2.0 * s)),
            ("head", "rotation"): mul(qx(17.0 + 2.0 * c), qz(-2.5 * s)),
        }
        for side, name in ((1, "left"), (-1, "right")):
            wind = s if side > 0 else -s
            row[("arm-" + name, "rotation")] = mul(up[side][0], mul(qz(side * 9.0 * wind), qx(8.0 * c * side)))
            row[("forearm-" + name, "rotation")] = mul(up[side][1], qz(side * 10.0 * (c if side > 0 else -c)))
            row[("forearm-" + name, "scale")] = (1.0 + ARM_UP_REACH * (0.62 + 0.08 * wind), 1.0, 1.0)
        _add(out, t, row)
    return out


# ---------------------------------------------------------------------------
# THE ACTION CLIPS (2026-10-07): the carry, the throw, the pick-up, the tag, the shove, the two
# gestures, the slide, out of breath, sitting, the knock-down, yes and no. They replace the
# thirteen of the same names copied from the stock rig, which were made for an arm with no elbow.
#
# EIGHT KEEP THE LENGTH AND THE BEAT OF THE CLIP THEY REPLACE, measured off his own .glb before
# they were replaced (the fist is the far end of the forearm; "forward" is +z):
#   holding-right-shoot  0.2000 s  starts WITH the arm forward (the game plays it from the instant
#                                  the slipper leaves); its far point is 0.067 s
#   pick-up              0.3333 s  fist and head lowest at 0.167 s
#   attack-melee-right   0.4167 s  right fist furthest forward at 0.250 s
#   attack-melee-left    0.4167 s  left fist furthest forward at 0.250 s
#   interact-right/left  0.6667 s  fist out by 0.167 s (highest) and held, still, to 0.40 s
#   slide                0.9500 s  head lowest at 0.250 s, right fist furthest forward at 0.342 s
#   die                  0.3333 s  on the ground from 0.267 s
# FIVE ARE ONLY LOOKED AT, NEVER TIMED AGAINST, and are longer than the stock ones (a sixth of a
# second, or two thirds for the emotes) so that there is room to move:
#   holding-right 2.0 s loop, crouch 1.8 s loop, sit 0.8 s, emote-yes 1.2 s loop, emote-no 1.4 s loop
# THIRTEEN SILHOUETTES, by where the fists are at each clip's beat (no two the same):
#   carry: right fist held out LOW at his side, the slipper dangling; he tips toward it
#   throw: spun half round to his left after his own arm, a foot off the ground
#   pick-up: sat down beside it and keeled over to his right, right arm along the ground, left arm up
#   tag: stretched up and forward on his toes, right arm long and HIGH (everyone is taller than he is)
#   shove: head first, like a goat; left fist under his chin, right arm thrown up behind him
#   reach right: right fist up and out to his SIDE, tugging a sleeve, leaning away from it
#   reach left: left fist held out level ahead, leaning back: "akin na"
#   slide: on his back, feet first, right arm reaching up over his knees
#   breath: folded over, both arms hanging straight down like wet washing
#   sit: on the ground, slumped, fists together between his legs, asleep
#   down: on his face, seat in the air, arms left behind him
#   yes: right fist as high beside his head as it goes, the whole of him leaning up after it
#   no: arched over backward looking at the sky, both arms hanging dead, rocking from side to side
# HIS HEAD IS TWICE AS WIDE AS HIS SHOULDERS and reaches 0.2 m out past them, so no fist of his
# ever gets above it: "up" for him is out beside his ear, through the elbow, with the forearm
# stretched, and most of what reads from 10 m is what his whole body is doing under that head.
# THE THROW AND THE TAG ARE PARTLY POSED BY THE GAME over the clip: his chest, his head and both
# UPPER arms are overwritten while they play, so in those two the character is in the root, the
# legs and the squash, and each forearm keeps a moderate bend that opens to straight at the beat.
#
# HOW THEY ARE WRITTEN. A clip is a list of (time, pose, ease): the pose is a table of plain
# numbers in HIS OWN space (the `root` bone's), and `_act` walks from each to the next. Where an
# arm points, and which way his head faces, are given in that space whatever his chest is doing.
#   tx ty tz            root translation, metres
#   pitch yaw roll      root rotation, degrees; pitch + tips him FORWARD, roll + to his RIGHT
#   sq                  root height (the width takes up what the height loses)
#   lean twist hunch    chest: + lean folds forward, + twist turns his chest to his LEFT
#   nod look tilt       head, in his own space: + nod is chin down, + look is to his LEFT
#   legL legR           (forward, out, toes in) degrees
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


def _un(pitch, arm):
    """An arm given in the STREET's space (down is the ground) for a body pitched over by `pitch`."""
    return tuple(_rot(qx(-pitch), _norm(v)) for v in arm)


HIP = (0.17625, -0.02875)    # the hip joint: height, and how far behind the root it sits


def _planted(pitch):
    """(ty, tz) that keep a foot hanging straight down from the hip where it stood, when he pitches."""
    a = math.radians(pitch)
    y = HIP[0] * math.cos(a) - HIP[1] * math.sin(a)
    z = HIP[0] * math.sin(a) + HIP[1] * math.cos(a)
    return HIP[0] - y, HIP[1] - z


#   the stand every one-shot leaves from and comes back to: frame 0 of `idle`. There his arms hang
#   from a chest that is 4 degrees forward, so here they are those directions tipped with it.
STAND = {
    "tx": 0.0, "ty": 0.0, "tz": 0.0, "pitch": 0.0, "yaw": 0.0, "roll": 0.0, "sq": 1.0,
    "lean": 4.0, "twist": 0.0, "hunch": 0.0, "nod": 4.0 + STAND_NOD, "look": 0.0, "tilt": 0.0,
    "legL": (0.0, 1.5, TOE_IN), "legR": (0.0, 1.5, TOE_IN),
    "armL": tuple(_rot(qx(4.0), v) for v in ARMS_HANG), "armR": tuple(_rot(qx(4.0), v) for v in ARMS_HANG),
    "reachL": 0.0, "reachR": 0.0,
}
HANG = STAND["armL"]


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
        ("leg-left", "rotation"): mul(mul(qx(-p["legL"][0]), qz(p["legL"][1])), qy(-p["legL"][2])),
        ("leg-right", "rotation"): mul(mul(qx(-p["legR"][0]), qz(-p["legR"][1])), qy(p["legR"][2])),
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


#   THE DANGLE: how he carries the tsinelas. Held out from his side, low, at the end of a stiff
#   arm, the way you carry something somebody handed you and you did not ask for.
ARM_DANGLE = ((0.80, -0.58, 0.14), (0.88, -0.44, 0.18))
DANGLE_REACH = 0.35
ARM_DROOP = ((0.66, -0.74, 0.10), (0.58, -0.80, 0.14))     # the same arm as he nods off
ARM_HOIST = ((0.86, -0.30, 0.42), (0.78, 0.22, 0.58))      # and jerked back up when he wakes
#   his left arm while he carries: hanging, a little behind him
ARM_SLACK = ((0.60, -0.79, -0.10), (0.50, -0.86, -0.06))

#   HOLDING. Tipped toward the slipper, his head left behind on the other shoulder.
HOLD = _pose(sq=0.985, roll=4.0, lean=5.0, twist=-5.0, nod=13.0, tilt=-9.0,
             armR=ARM_DANGLE, reachR=DANGLE_REACH, armL=ARM_SLACK)
HOLD_LENGTH = 2.0


def holding_right():
    """A 2 s loop. He holds the slipper out at his side at the end of his arm and FORGETS it: his
    chin sinks, the arm sinks with it, and the weight of the thing starts to pull the whole of
    him over to that side (0.35 to 1.2 s). At 1.2 s he catches it: a start, the arm jerked back
    up higher than it was, his head up and round to look at what is in his hand, a small hop.
    Then it settles to where it began. Frame 0 is the carry the throw leaves from."""
    def pose_at(t):
        doze = _ease((t - 0.35) / 0.80) ** 1.4 * (1.0 - _ease((t - 1.20) / 0.09))
        catch = _ease((t - 1.20) / 0.09) * (1.0 - _ease((t - 1.42) / 0.45))
        hop = _bump(t, 1.29, 0.07)
        land = _bump(t, 1.46, 0.07)
        breath = math.sin(2.0 * math.pi * t / HOLD_LENGTH)
        arm = _blend(_blend(ARM_DANGLE, ARM_DROOP, doze), ARM_HOIST, catch)
        return _pose(HOLD, sq=0.985 + 0.008 * breath - 0.035 * doze + 0.060 * hop - 0.045 * land, ty=0.022 * hop,
                     roll=4.0 + 7.0 * doze - 5.0 * catch, tx=-0.010 * doze,
                     lean=5.0 + 6.0 * doze - 7.0 * catch, twist=-5.0 - 9.0 * catch,
                     nod=13.0 + 22.0 * doze - 14.0 * catch, tilt=-9.0 - 5.0 * doze + 12.0 * catch, look=-36.0 * catch,
                     legL=(0.0, 1.5 - 4.0 * doze + 5.0 * catch, TOE_IN), legR=(0.0, 1.5 + 4.0 * doze + 5.0 * catch, TOE_IN),
                     armR=arm, reachR=DANGLE_REACH * (1.0 - 0.6 * doze) + 0.25 * catch,
                     armL=_blend(ARM_SLACK, ARM_FLAIL, 0.45 * catch))
    return _frames(HOLD_LENGTH, pose_at)


def holding_right_shoot():
    """THE THROW, from the instant the slipper leaves (the game winds the arm up itself, plays this
    at the release, and poses his chest, head and upper arms over it). He barely wakes up for it,
    and the throw is bigger than he is: the arm goes and takes him with it. By 0.067 s, the stock
    clip's far point, he is spun a third of the way round to his left after his own fist, up on
    one foot, the other swung out; he keeps going, tipping (0.11 s); and gets the foot down and
    comes back round to the carry. The forearm opens from its bend to straight at the far point."""
    thrown = _pose(HOLD, sq=1.10, yaw=34.0, roll=-7.0, pitch=7.0, ty=0.020, tx=0.012, twist=16.0, lean=8.0,
                   nod=6.0, tilt=4.0, legL=(4.0, 1.0, 0.0), legR=(-16.0, 30.0, 0.0),
                   armR=((0.22, -0.06, 0.97), (-0.06, -0.02, 1.0)), reachR=0.50,
                   armL=((0.90, 0.10, -0.42), (0.82, 0.36, -0.44)), reachL=0.15)
    over = _pose(thrown, sq=0.93, yaw=50.0, roll=-13.0, pitch=10.0, ty=0.006, tx=0.026, twist=22.0, nod=16.0, tilt=10.0,
                 legL=(2.0, -6.0, 0.0), legR=(-6.0, 40.0, 0.0),
                 armR=((0.20, -0.45, 0.87), (-0.20, -0.52, 0.83)), reachR=0.20, armL=ARM_FLAIL, reachL=0.25)
    caught = _pose(HOLD, sq=1.03, yaw=14.0, roll=7.0, tx=-0.006, legL=(0.0, 4.0, TOE_IN), legR=(0.0, 9.0, TOE_IN),
                   armL=_blend(ARM_SLACK, ARM_FLAIL, 0.4))
    return _act(0.20, [(0.0, HOLD), (0.067, thrown, "out"), (0.115, over, "lin"), (0.165, caught), (0.20, HOLD)])


def pick_up():
    """He does not bend for it. He SITS on the street next to it, which is where he wanted to be
    anyway: his feet go out in front of him, he lands on his seat with his legs apart, keeling
    over to his right, and his right arm goes out along the ground to that side and scoops the
    slipper while his left flies up (lowest at 0.167 s). And because he weighs nothing he
    bounces: straight back up off his seat, a little into the air, the slipper against his chest.
    (The first one tipped him over one leg like a drinking bird; three others in the cast do that.)"""
    drop = _pose(sq=1.04, ty=-0.045, pitch=-12.0, lean=2.0, nod=6.0, legL=(46.0, 10.0, 0.0), legR=(46.0, 10.0, 0.0),
                 armL=ARM_FLAIL, armR=ARM_FLAIL, reachL=0.20, reachR=0.20)
    down = _pose(sq=0.85, ty=-0.118, tx=-0.020, pitch=-4.0, roll=19.0, lean=7.0, twist=-20.0, nod=14.0, look=-30.0, tilt=-16.0,
                 legL=(92.0, 26.0, -8.0), legR=(84.0, 8.0, -8.0),
                 armR=((0.84, -0.34, 0.42), (0.80, -0.40, 0.45)), reachR=1.30,
                 armL=((0.78, 0.56, 0.10), (0.52, 0.84, 0.12)), reachL=0.70)
    up = _pose(sq=1.11, ty=0.034, pitch=-5.0, roll=-4.0, lean=-2.0, nod=2.0, legL=(16.0, 8.0, 0.0), legR=(22.0, 8.0, 0.0),
               armR=((0.70, -0.42, 0.58), (0.36, 0.30, 0.88)), reachR=0.25, armL=_blend(HANG, ARM_FLAIL, 0.7), reachL=0.15)
    return _act(1.0 / 3.0, [(0.0, STAND), (0.075, drop, "out"), (1.0 / 6.0, down, "in"), (0.265, up, "out"), (1.0 / 3.0, STAND)])


def attack_melee_right():
    """THE TAG. Everyone he has to tag is taller than he is, so his tag goes UP: a quick sink
    (0.06 s), then he is on his toes and the whole of him is one line from his heels to his fist,
    stretched up and forward as far as a seven-year-old goes, his other arm left behind him; it
    lands at 0.25 s, and he is past his own feet by then, so he wobbles and drops back onto his
    heels. The game poses his chest, head and upper arms over this, so what is his own all the
    way through is the sink, the stretch, the tip forward and the wobble; the legs are mild and
    the right forearm is straight from 0.13 s."""
    behind = ((0.62, -0.50, -0.60), (0.52, -0.40, -0.76))
    wind = _pose(sq=0.87, lean=11.0, twist=-14.0, nod=17.0, legL=(0.0, 3.0, TOE_IN), legR=(0.0, 3.0, TOE_IN),
                 armR=((0.62, -0.62, -0.48), (0.46, -0.50, 0.73)), armL=((0.60, -0.70, 0.38), (0.40, -0.50, 0.77)))
    out = _pose(sq=1.10, pitch=13.0, lean=6.0, twist=12.0, nod=0.0, legL=(9.0, 2.0, 0.0), legR=(4.0, 2.0, 0.0),
                armR=((0.50, 0.26, 0.83), (0.36, 0.40, 0.84)), reachR=0.50, armL=behind)
    far = _pose(out, sq=1.20, pitch=29.0, tz=-0.030, lean=6.0, twist=20.0, nod=-12.0, legL=(27.0, 2.0, 0.0), legR=(12.0, 2.0, 0.0),
                armR=_un(29.0, ((0.46, 0.42, 0.78), (0.34, 0.56, 0.76))), reachR=1.20, reachL=0.45,
                armL=((0.60, -0.30, -0.74), (0.50, -0.12, -0.86)))
    wobble = _pose(far, sq=1.07, pitch=34.0, roll=-6.0, nod=0.0, reachR=0.70, legL=(30.0, 0.0, 0.0), legR=(20.0, 7.0, 0.0))
    settle = _pose(sq=0.93, pitch=-5.0, roll=3.0, lean=2.0, nod=14.0, legL=(-4.0, 4.0, TOE_IN), legR=(-4.0, 4.0, TOE_IN),
                   armR=_blend(HANG, ARM_FLAIL, 0.45), armL=_blend(HANG, ARM_FLAIL, 0.45))
    return _act(0.4167, [(0.0, STAND), (0.06, wind, "out"), (0.13, out, "in"), (0.25, far, "out"),
                         (0.31, wobble), (0.37, settle), (0.4167, STAND)])


def attack_melee_left():
    """THE SHOVE. He has no weight to shove with, so he uses the one heavy thing he owns: his HEAD.
    He rocks back onto his heels with both arms up (0.09 s), hangs there, and goes in like a goat:
    head down and first, his left fist rammed out under his chin, his right arm thrown up behind
    him, a foot left in the air (far point 0.25 s). He bounces off whatever it was and totters
    back upright."""
    ty, tz = _planted(40.0)
    wind = _pose(sq=0.90, pitch=-11.0, lean=-5.0, nod=-4.0, legL=(-8.0, 3.0, TOE_IN), legR=(-8.0, 3.0, TOE_IN),
                 armL=((0.72, 0.10, 0.68), (0.30, 0.76, 0.58)), reachL=0.30, armR=((0.72, 0.10, 0.68), (0.30, 0.76, 0.58)), reachR=0.30)
    coiled = _pose(wind, sq=0.87, pitch=-14.0, nod=6.0)
    butt = _pose(sq=1.12, pitch=40.0, ty=ty, tz=tz + 0.085, lean=8.0, twist=-10.0, nod=30.0,
                 legL=(46.0, 2.0, 0.0), legR=(8.0, 5.0, 0.0),
                 armL=_un(40.0, ((0.30, -0.22, 0.93), (0.12, -0.14, 0.98))), reachL=0.70,
                 armR=_un(40.0, ((0.55, 0.40, -0.73), (0.42, 0.62, -0.66))), reachR=0.45)
    held = _pose(butt, sq=0.97, pitch=35.0, tz=tz + 0.070, nod=24.0, reachL=0.40, reachR=0.25, legR=(16.0, 5.0, 0.0))
    back = _pose(sq=1.03, pitch=-7.0, nod=6.0, roll=-4.0, legL=(-5.0, 5.0, TOE_IN), legR=(-5.0, 5.0, TOE_IN),
                 armL=_blend(HANG, ARM_FLAIL, 0.6), armR=_blend(HANG, ARM_FLAIL, 0.6))
    return _act(0.4167, [(0.0, STAND), (0.09, wind, "out"), (0.16, coiled, "lin"), (0.25, butt, "in"),
                         (0.31, held, "out"), (0.37, back), (0.4167, STAND)])


def interact_right():
    """He tugs your sleeve. His right fist goes up and out to his side, as high as it gets, and
    takes hold of somebody much taller than he is (0.167 s), his face turned up to them; then he
    hangs off it, twice, leaning his whole weight away and down; and lets go."""
    hold = ((0.86, 0.36, 0.36), (0.70, 0.66, 0.26))
    up = _pose(sq=1.06, roll=5.0, lean=1.0, twist=-10.0, nod=-12.0, look=-34.0, tilt=-6.0,
               legL=(0.0, 1.0, TOE_IN), legR=(0.0, 3.0, TOE_IN), armR=hold, reachR=1.00, armL=ARM_SLACK)
    tug = _pose(up, sq=0.92, roll=-10.0, tx=0.012, lean=5.0, nod=-4.0, tilt=8.0,
                legL=(0.0, 6.0, TOE_IN), legR=(0.0, -3.0, TOE_IN),
                armR=((0.93, 0.26, 0.26), (0.84, 0.50, 0.20)), reachR=1.25,
                armL=((0.80, -0.56, -0.20), (0.74, -0.62, -0.24)))
    again = _pose(up, sq=1.03, roll=2.0, reachR=1.05)
    tug2 = _pose(tug, sq=0.90, roll=-12.0, reachR=1.30)
    held = _pose(tug2, sq=0.95, roll=-8.0, reachR=1.15)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.167, up, "out"), (0.235, tug, "in"), (0.30, again, "out"), (0.365, tug2, "in"),
                            (0.42, held, "out"), (2.0 / 3.0, STAND)])


def interact_left():
    """"Akin na." (Give it.) His left fist comes out level in front of him and stays there
    (0.167 s), and he leans BACK from it with his head on one side, bouncing the hand twice like a
    boy who has asked for the same thing three times already. His right arm does nothing at all."""
    limp = ((0.58, -0.78, -0.22), (0.50, -0.82, -0.28))
    ty, tz = _planted(-15.0)
    out = _pose(sq=0.97, pitch=-15.0, ty=ty, tz=tz, lean=-3.0, twist=-14.0, nod=4.0, tilt=-20.0, look=6.0, roll=-4.0,
                legL=(-15.0, 4.0, TOE_IN), legR=(-15.0, 4.0, TOE_IN),
                armL=_un(-15.0, ((0.46, -0.30, 0.84), (0.20, 0.0, 0.98))), reachL=0.95, armR=limp)
    bounce = _pose(out, sq=1.03, nod=0.0, armL=_un(-15.0, ((0.46, -0.16, 0.87), (0.20, 0.26, 0.94))), reachL=1.05)
    drop = _pose(out, sq=0.95, nod=8.0, armL=_un(-15.0, ((0.46, -0.34, 0.82), (0.20, -0.08, 0.98))), reachL=0.90)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.167, out, "out"), (0.225, bounce, "out"), (0.28, drop, "in"),
                            (0.335, bounce, "out"), (0.39, drop, "in"), (0.43, out), (2.0 / 3.0, STAND)])


def slide():
    """He does not mean to slide. His feet go out from under him (he is "impossible to keep
    upright"), both arms fly up, and he lands flat on his back and keeps going, feet first, one
    leg in the air (down at 0.25 s). And because he is also "slippery", he grabs the slipper on
    the way past: his right arm comes up over his knees and stretches for it (0.342 s). He skids
    on a little, rocks up onto his seat, and is on his feet again as if it had been the plan."""
    over = (ARM_UP[0], ARM_UP[1])
    slip = _pose(sq=1.06, pitch=-20.0, ty=0.030, tz=0.020, lean=-4.0, nod=-8.0, legL=(34.0, 4.0, 0.0), legR=(50.0, 4.0, 0.0),
                 armL=ARM_FLAIL, armR=ARM_FLAIL, reachL=0.30, reachR=0.30)
    air = _pose(sq=1.08, pitch=-52.0, ty=0.085, tz=0.090, lean=-2.0, nod=6.0, legL=(38.0, 6.0, 0.0), legR=(70.0, 6.0, 0.0),
                armL=over, armR=over, reachL=0.70, reachR=0.70)
    flat = _pose(sq=0.86, pitch=-76.0, ty=0.060, tz=0.190, lean=0.0, nod=14.0, legL=(4.0, 8.0, 0.0), legR=(46.0, 8.0, 0.0),
                 armL=over, armR=over, reachL=0.50, reachR=0.50)
    grab = _pose(sq=1.04, pitch=-66.0, ty=0.050, tz=0.230, lean=24.0, twist=14.0, nod=22.0, legL=(10.0, 10.0, 0.0), legR=(30.0, 10.0, 0.0),
                 armR=_un(-66.0, ((0.25, 0.42, 0.87), (0.10, 0.34, 0.93))), reachR=1.10,
                 armL=_un(-66.0, ((0.80, -0.50, -0.33), (0.62, -0.75, -0.22))), reachL=0.20)
    skid = _pose(grab, sq=0.98, pitch=-70.0, tz=0.255, lean=16.0, nod=18.0, legL=(6.0, 9.0, 0.0), legR=(52.0, 9.0, 0.0), reachR=0.60)
    seat = _pose(sq=0.94, pitch=-22.0, ty=-0.085, tz=0.180, lean=22.0, nod=14.0, legL=(66.0, 10.0, 0.0), legR=(70.0, 10.0, 0.0),
                 armR=((0.70, -0.30, 0.65), (0.45, 0.25, 0.86)), reachR=0.25, armL=((0.80, -0.58, -0.15), (0.50, -0.85, 0.15)))
    up = _pose(sq=1.07, pitch=7.0, tz=0.050, lean=0.0, nod=4.0, armL=_blend(HANG, ARM_FLAIL, 0.5), armR=_blend(HANG, ARM_FLAIL, 0.5))
    return _act(0.95, [(0.0, STAND), (0.07, slip, "out"), (0.15, air, "lin"), (0.25, flat, "in"), (0.342, grab, "out"),
                       (0.54, skid), (0.70, seat), (0.84, up), (0.95, STAND)])


CROUCH_LENGTH = 1.8
ARM_WASHING = ((0.34, -0.93, 0.14), (0.22, -0.96, 0.16))    # straight down from the shoulder, wherever that is
CROUCH = _pose(sq=0.93, pitch=6.0, lean=44.0, nod=64.0, legL=(6.0, 7.0, 16.0), legR=(6.0, 7.0, 16.0),
               armL=ARM_WASHING, armR=ARM_WASHING, reachL=0.30, reachR=0.30)


def crouch():
    """OUT OF BREATH, a 1.8 s loop. He does not prop himself on his knees, he just HANGS: folded
    over, toes turned in, his head upside down nearly, both arms straight down like wet washing
    and swinging. Two heaves: his back humps up as the air goes in and his arms swing out ahead,
    and it all sags again; on the second, bigger one his head is too much for him and he starts
    to go over forward, and shuffles to stay under it. The first and the last frame are the full
    folded pose: the game loops this while he is winded and also holds its last frame as an emote."""
    def pose_at(t):
        p = (2.0 * t / CROUCH_LENGTH) % 1.0
        if t > CROUCH_LENGTH - 1e-6:
            p = 0.0
        heave = _ease(p / 0.38) if p < 0.38 else 1.0 - _ease((p - 0.38) / 0.62)
        heave *= 0.7 if t < 0.5 * CROUCH_LENGTH else 1.0
        swing = math.sin(2.0 * math.pi * (2.0 * t / CROUCH_LENGTH) - 1.1) + math.sin(1.1)
        swing *= _window(t, 0.0, CROUCH_LENGTH, 0.25)
        tip = _bump(t, 1.42, 0.13)
        sway = math.sin(2.0 * math.pi * t / CROUCH_LENGTH)
        arms = _blend(ARM_WASHING, ((0.34, -0.90, 0.30), (0.22, -0.86, 0.46)), 0.5 * heave + 0.25 * swing + 0.5 * tip)
        return _pose(CROUCH, sq=0.93 + 0.060 * heave - 0.020 * tip, pitch=6.0 + 9.0 * tip, roll=2.5 * sway, tz=0.012 * tip,
                     lean=44.0 - 11.0 * heave + 5.0 * tip, nod=64.0 - 16.0 * heave + 8.0 * tip, tilt=5.0 * sway,
                     legL=(6.0 + 11.0 * tip, 7.0 - 2.5 * sway, 16.0), legR=(6.0 + 5.0 * tip, 7.0 + 2.5 * sway, 16.0),
                     armL=arms, armR=arms, reachL=0.30 + 0.10 * heave, reachR=0.30 + 0.10 * heave)
    return _frames(CROUCH_LENGTH, pose_at)


def sit():
    """0.8 s of getting down, ending seated. His legs simply stop: he drops straight onto his seat
    (0.34 s) with a squash, his feet flying out and his head bobbing; sits up for one moment,
    awake and surprised to be down there; and then all of it goes out of him. He ends slumped
    over his own lap with his fists together on the ground between his legs and his head on one
    shoulder: asleep, sitting up."""
    #   (the first seat was folded 27 degrees over his lap with his chin on his chest, and from the side it was `crouch`)
    lap = ((0.42, -0.72, 0.55), (-0.30, -0.80, 0.52))
    seat = _pose(sq=0.97, ty=-0.110, pitch=-4.0, roll=5.0, lean=9.0, hunch=7.0, nod=20.0, tilt=27.0, look=8.0,
                 legL=(80.0, 17.0, -8.0), legR=(86.0, 13.0, -8.0), armL=lap, armR=lap, reachL=0.45, reachR=0.45)
    sink = _pose(sq=0.93, lean=8.0, nod=16.0, legL=(4.0, 5.0, TOE_IN), legR=(4.0, 5.0, TOE_IN))
    drop = _pose(sq=1.07, ty=-0.050, pitch=-9.0, lean=4.0, nod=2.0, legL=(48.0, 9.0, 0.0), legR=(48.0, 9.0, 0.0),
                 armL=ARM_FLAIL, armR=ARM_FLAIL, reachL=0.30, reachR=0.30)
    land = _pose(seat, sq=0.80, ty=-0.125, pitch=-2.0, lean=14.0, hunch=0.0, nod=40.0, tilt=0.0, look=0.0,
                 legL=(96.0, 20.0, -8.0), legR=(96.0, 20.0, -8.0),
                 armL=((0.86, -0.46, 0.20), (0.70, -0.70, 0.15)), armR=((0.86, -0.46, 0.20), (0.70, -0.70, 0.15)), reachL=0.2, reachR=0.2)
    awake = _pose(seat, sq=1.05, ty=-0.105, pitch=-10.0, lean=-2.0, hunch=0.0, nod=-4.0, tilt=0.0, look=0.0,
                  legL=(72.0, 15.0, -8.0), legR=(76.0, 15.0, -8.0),
                  armL=((0.80, -0.56, 0.20), (0.50, -0.80, 0.34)), armR=((0.80, -0.56, 0.20), (0.50, -0.80, 0.34)), reachL=0.3, reachR=0.3)
    return _act(0.8, [(0.0, STAND), (0.10, sink), (0.25, drop, "in"), (0.34, land, "lin"), (0.44, awake, "out"),
                      (0.54, awake), (0.8, seat)])


def die():
    """KNOCKED DOWN, and it is not much of a change for him: he was never very upright. A start,
    arms out; he goes over FORWARD after his own head, arms left behind him like his run; lands
    on his face with his seat in the air, on the ground from 0.267 s; his seat bounces once; and
    he stays there, face down, knees under him, arms lying where they fell. A comic face-plant."""
    trail = ((0.62, -0.74, -0.26), (0.54, -0.80, -0.26))
    start = _pose(sq=0.90, pitch=-9.0, lean=-8.0, nod=-12.0, legL=(-6.0, 5.0, 0.0), legR=(-6.0, 5.0, 0.0),
                  armL=ARM_FLAIL, armR=ARM_FLAIL, reachL=0.30, reachR=0.30)
    going = _pose(sq=1.10, pitch=46.0, ty=0.060, tz=-0.030, lean=4.0, nod=-14.0, legL=(-14.0, 5.0, 0.0), legR=(10.0, 5.0, 0.0),
                  armL=ARM_BEHIND, armR=ARM_BEHIND, reachL=0.30, reachR=0.30)
    plant = _pose(sq=0.84, pitch=98.0, ty=0.170, tz=-0.110, lean=0.0, nod=-12.0, legL=(84.0, 9.0, 0.0), legR=(60.0, 9.0, 0.0),
                  armL=trail, armR=trail, reachL=0.25, reachR=0.25)
    bounce = _pose(plant, sq=1.04, pitch=122.0, ty=0.300, nod=-40.0, legL=(86.0, 12.0, 0.0), legR=(108.0, 12.0, 0.0))
    rest = _pose(plant, sq=0.97, pitch=114.0, ty=0.262, nod=-34.0, tilt=14.0, look=10.0, legL=(112.0, 10.0, 0.0), legR=(106.0, 12.0, 0.0),
                 armL=((0.70, -0.70, -0.14), (0.66, -0.74, -0.14)), armR=((0.56, -0.80, -0.20), (0.40, -0.90, -0.16)),
                 reachL=0.20, reachR=0.20)
    return _act(1.0 / 3.0, [(0.0, STAND), (0.05, start, "out"), (0.14, going, "in"), (0.235, plant, "in"),
                            (0.285, bounce, "out"), (1.0 / 3.0, rest, "in")])


def emote_yes():
    """YES: "ako! ako!" (me! me!), the hand up in class. A 1.2 s loop that he fills. A sink; his
    right fist goes up beside his head as high as he can get it, the forearm stretched, and the
    whole of him leans up after it, up on his toes and tipped to the other side to make it
    higher; he holds it; drops a little and stretches again, higher; and a third time. His left
    arm is rigid at his side, as told. It is the one time all day he is properly awake."""
    high = ((0.80, 0.56, 0.10), (0.36, 0.93, 0.02))
    half = ((0.86, 0.26, 0.30), (0.46, 0.78, 0.42))
    rigid = ((0.46, -0.88, -0.06), (0.42, -0.90, -0.06))
    dip = _pose(sq=0.91, lean=7.0, nod=14.0, roll=3.0, legL=(0.0, 4.0, TOE_IN), legR=(0.0, 4.0, TOE_IN),
                armR=((0.78, -0.30, 0.54), (0.30, 0.60, 0.74)), armL=rigid)
    up = _pose(sq=1.12, roll=-9.0, tx=0.012, ty=0.020, lean=-3.0, twist=-6.0, nod=-8.0, tilt=5.0, look=-8.0,
               legL=(0.0, -3.0, 0.0), legR=(0.0, 9.0, 0.0), armR=high, reachR=1.45, armL=rigid, reachL=0.15)
    up_held = _pose(up, sq=1.08, ty=0.012, reachR=1.35)
    down = _pose(up, sq=0.95, roll=-4.0, ty=0.0, lean=3.0, nod=4.0, legL=(0.0, 1.0, TOE_IN), legR=(0.0, 5.0, TOE_IN),
                 armR=half, reachR=0.80, reachL=0.0)
    up2 = _pose(up, sq=1.15, roll=-11.0, ty=0.028, reachR=1.60)
    up2_held = _pose(up2, sq=1.10, ty=0.016, reachR=1.48)
    up3 = _pose(up, sq=1.13, roll=-10.0, ty=0.022, reachR=1.55)
    up3_held = _pose(up3, sq=1.07, ty=0.008, reachR=1.40)
    return _act(1.2, [(0.0, STAND), (0.12, dip), (0.22, up, "out"), (0.36, up_held), (0.44, down, "in"),
                      (0.54, up2, "out"), (0.68, up2_held), (0.76, down, "in"), (0.86, up3, "out"), (1.00, up3_held),
                      (1.2, STAND)])


def emote_no():
    """NO: "ayoko naaa", the whine. A 1.4 s loop. He has no fight in him, so his no has no arms in
    it at all: his head drops BACK until he is looking at the sky, his knees go, the whole of him
    arches backward with both arms hanging dead, and he rocks from one side to the other from
    the feet like a boy being made to go home, four times, each arm swinging out on the low
    side and the far foot dragging off the ground. Then it all falls forward, chin on his tie,
    and he stands there. (The first one put a fist on each ear and twisted, which is Totoy's.)"""
    dead_out = ((0.84, -0.54, -0.06), (0.90, -0.42, -0.04))      # the arm on the low side, swung out from him
    dead_in = ((0.40, -0.91, -0.10), (0.22, -0.97, -0.06))       # and the one on the high side, against him
    ty, tz = _planted(-13.0)

    def whine(roll, sq):
        low_right = roll > 0.0
        amount = abs(roll) / 15.0
        return _pose(sq=sq, pitch=-13.0, ty=ty, tz=tz, roll=roll, tx=-0.0012 * roll, lean=-13.0, nod=-40.0, tilt=1.1 * roll,
                     legL=(-13.0, 3.0 + (9.0 * amount if low_right else -2.0), TOE_IN),
                     legR=(-13.0, 3.0 + (-2.0 if low_right else 9.0 * amount), TOE_IN),
                     armR=_blend(HANG, dead_out if low_right else dead_in, amount), reachR=0.45 * amount if low_right else 0.0,
                     armL=_blend(HANG, dead_in if low_right else dead_out, amount), reachL=0.0 if low_right else 0.45 * amount)
    back = _pose(sq=0.93, pitch=-9.0, lean=-9.0, nod=-30.0, legL=(-9.0, 3.0, TOE_IN), legR=(-9.0, 3.0, TOE_IN))
    flop = _pose(sq=0.90, lean=17.0, nod=36.0, legL=(0.0, 3.0, TOE_IN), legR=(0.0, 3.0, TOE_IN),
                 armL=ARM_WASHING, armR=ARM_WASHING, reachL=0.15, reachR=0.15)
    return _act(1.4, [(0.0, STAND), (0.13, back, "out"), (0.30, whine(15.0, 0.96)), (0.52, whine(-15.0, 0.99)),
                      (0.74, whine(15.0, 0.96)), (0.96, whine(-13.0, 0.99)), (1.10, flop, "in"), (1.20, flop),
                      (1.4, STAND)])


CLIPS = {
    "idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
    "holding-right": holding_right, "holding-right-shoot": holding_right_shoot, "pick-up": pick_up,
    "attack-melee-right": attack_melee_right, "attack-melee-left": attack_melee_left,
    "interact-right": interact_right, "interact-left": interact_left, "slide": slide,
    "crouch": crouch, "sit": sit, "die": die, "emote-yes": emote_yes, "emote-no": emote_no,
}
