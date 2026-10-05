"""New locomotion clips for the Zack (displayed: Isagani) redesign PROTOTYPE.

Imported by tools/author_character_redesign_zack.py, which writes them into the prototype .glb IN
PLACE OF the clips of the same name copied from team-zack.glb. Nothing in the game reads them;
team-zack.glb keeps its own.

WHAT HIS MOTION SHOULD SAY ABOUT HIM (rule 9: each hero's motion is its own, so this is written
down first and the numbers below are tuned to it, not copied from Dante's):

    docs/CHARACTER_ORIGINS.md: "he makes difficult plays look casual and casual plays
    unnecessarily difficult." A condo roofdeck kid with a smirk and half-shut eyes whose talent
    is electricity. He is LIGHT and QUICK and he is showing off without appearing to try.

  walk    a stroll, not a march. He leans BACK a little with his chin up, the shoulders roll and
          the hips dip side to side, the bounce is small and late. The two arms do not match: his
          left swings loose and long, his right barely moves and stays bent by the hip where the
          wallet chain hangs. An even, eager arm swing would be somebody else.
  sprint  a spark along the ground. Far forward, almost no bounce (he skims), very fast short
          snapped steps, and the arms thrown BACK and out behind him, nearly straight, hardly
          pumping. The effort is all in the legs; the top half looks like it is being pulled.
  idle    ACTED, three stances a kid like him takes while he waits (see `idle`): hands in his
          jacket pockets, slouched, a foot tapping, a hand up to fix the quiff, hands behind his back rocking
          on his heels with his chin up. Breathing stays small underneath.
  jump    a pop from STANDING: both feet together, both knees snapped up in front of him by the
          same amount like a skater's tuck, the body tipped back, and the arms UP through the
          elbow in a V, the left a little higher than the right, the forearms stretching as they
          are thrown. (Owner, 2026-10-05: "is the jump supposed to be one foot forward? i need a
          jump for standing still". So the legs are a mirror pair; only the arms are uneven.)
  fall    unbothered. Reclined, legs out in front paddling a little, arms up in a loose V with
          the forearms waving slowly out of step, eyes on the ground.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT. Every
bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK; about +z a
negative angle drops his left arm from straight out to hanging, a positive one lifts it.

RULE 8, AS AMENDED BY THE OWNER ("the arms dont really get much higher than the original A
posing.. the jump looks like he's shrugging"): a STRAIGHT arm cannot rise above straight out, his
head is wider than his shoulders and his ears wider still. Arms go up THROUGH THE ELBOW: the upper
arm lifts `ARM_LIFT`, the forearm folds up and out past the ear, and it may stretch (a scale
channel on the forearm bone). In walk and sprint a folded forearm turns OUTWARD (`SPLAY`), never
across the torso.

Each clip keeps the LENGTH of the one it replaces (walk 0.72, sprint 0.48,
jump 0.50, fall 0.333). `idle` is 8 s where the old one was 1.333: stances need time. NOT SEEN IN UNITY: the game layers procedural motion on these bones and the scale
channels are new.
"""
import math

RATE = 60.0
SPLAY = 34.0      # degrees a folded forearm is turned out from straight ahead
ARM_LIFT = 12.0   # degrees an upper arm may rise above straight out before it meets the collar


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


def mul(a, b, *more):
    """a after b (after any more, right to left)."""
    if more:
        b = mul(b, *more)
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


def _collect(out, t, row):
    for key, value in row.items():
        out.setdefault(key, []).append((t, value))


def _norm(v):
    n = math.sqrt(sum(c * c for c in v)) or 1.0
    return tuple(c / n for c in v)


def _rot(q, v):
    """`v` turned by `q`."""
    x, y, z, w = q
    vx, vy, vz = v
    tx, ty, tz = 2.0 * (y * vz - z * vy), 2.0 * (z * vx - x * vz), 2.0 * (x * vy - y * vx)
    return (vx + w * tx + (y * tz - z * ty), vy + w * ty + (z * tx - x * tz), vz + w * tz + (x * ty - y * tx))


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
    """0 outside [start, end], 1 in the middle, eased in and out over `blend` seconds."""
    return _ease((t - start) / blend) * (1.0 - _ease((t - (end - blend)) / blend)) if start <= t <= end else 0.0


#   WHERE AN ARM POINTS IN EACH STANCE: (upper arm, forearm, forearm stretch), written for a LEFT
#   arm and mirrored for the right. His arm is short (upper 90 mm, forearm and fist 95 mm) and the
#   fist is a 100 by 120 mm block, so a hand only gets somewhere by the elbow going out first.
ARM_HANG = ((0.64, -0.77, 0.00), (0.50, -0.80, 0.33), 1.0)
#   both hands shoved in the jacket's welt pockets: elbows forward and out, the forearms coming in
#   across the front of the jacket to the pocket fronts (x 0.094, 0.223 up, 0.11 ahead), stretched
#   so the fists sit ON the welts and not inside the jacket.
#   (v06 and v07 had one fist on his hip here. On a 90 mm upper arm it could not be told from a
#   hanging arm from the front, and the coordinator's review said so. Hands in front of the body
#   change the silhouette; a hand at the side does not.)
ARM_POCKET = ((0.70, -0.50, 0.50), (-0.25, -0.25, 0.94), 1.7)
#   a hand up to the quiff. The upper arm goes forward (his ears are behind the middle of the
#   head and stand out to 0.224; a forearm raised beside them is IN them, see `jump`), the forearm
#   rises ahead of the ear beside the cheek and stretches to get there.
#   (Re-aimed for the 0.84 head: the lock's tip is now at x 0.082, 0.48 up, 0.18 ahead, so the
#   forearm leans IN and forward toward it and needs less stretch than the 2.35 the big head took.)
ARM_QUIFF = ((0.86, 0.20, 0.47), (-0.22, 0.86, 0.46), 2.1)
#   hands behind his back: elbows out and back, the forearms reaching in behind the jacket, a
#   little stretched so the fists sit BEHIND the back panel and not in it
ARM_BEHIND = ((0.55, -0.58, -0.60), (-0.46, -0.40, -0.80), 1.35)
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of THIS kid waiting. Three things he does, each held long enough to read:

      1.0 to 3.3   both hands go into his jacket pockets, he slouches back onto his heels with
                   his hips forward, and looks one way and then the other while one foot taps.
      3.7 to 5.3   he fixes his quiff. Left hand up to the dyed lock, head tipped into it, two
                   small flicks. He knows what the lock looks like and checks it anyway.
      5.8 to 7.7   hands behind his back, rocking on his heels, chin up, looking at the sky as
                   if the match were somebody else's problem.

    THREE TRIES (the first two were Dante's lesson and mine). The clip this replaces moved three
    bones on straight ramps. My first new one layered sines on every bone; the owner said of the
    same idea on Dante: "the idle looks like he's just distorting around.. needs more character,
    like sometimes he puts his hands on the waist, or crosses his arms, other things idle people
    do". So the idle is ACTED, and the breathing underneath is kept small.

    IT IS 8 s, NOT THE OLD 1.33 s. Anything in the game that assumes the idle's length needs a
    look, and the game may prefer these as separate clips it picks between. Frame 0 is the plain
    stand with the arms hanging.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 5.0))
        pocket = _window(t, 1.0, 3.3, 0.40)
        quiff = _window(t, 3.7, 5.3, 0.32)
        behind = _window(t, 5.8, 7.7, 0.45)
        # a small dip as each stance is taken, so the change has a beat
        pop = sum(math.exp(-((t - at) / 0.09) ** 2) for at in (1.25, 3.92, 6.10))
        # the two flicks at the quiff, the rock on his heels, the tapping foot
        flick = quiff * (math.exp(-((t - 4.30) / 0.07) ** 2) + math.exp(-((t - 4.62) / 0.07) ** 2))
        rock = behind * math.sin(2.0 * math.pi * (t - 5.8) / 0.95)
        tap = pocket * max(0.0, math.sin(2.0 * math.pi * 2.2 * t)) ** 2

        up = _arm(1, ARM_QUIFF[0], (ARM_QUIFF[1][0] - 0.10 * flick, ARM_QUIFF[1][1], ARM_QUIFF[1][2] + 0.16 * flick))
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            hang = _arm(side, *ARM_HANG[:2])
            in_pocket = _arm(side, *ARM_POCKET[:2])
            back = _arm(side, *ARM_BEHIND[:2])
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(hang[k], in_pocket[k], pocket)
                if side > 0:
                    q = _mix(q, up[k], quiff)
                row[(bone, "rotation")] = _mix(q, back[k], behind)
            reach = 1.0 + (ARM_POCKET[2] - 1.0) * pocket + (ARM_BEHIND[2] - 1.0) * behind
            if side > 0:
                reach += (ARM_QUIFF[2] - 1.0) * quiff
            row[("forearm-" + name, "scale")] = (reach, 1.0, 1.0)

        look = 30.0 * (_window(t, 1.4, 2.2, 0.28) - _window(t, 2.3, 3.2, 0.28)) + 10.0 * quiff - 8.0 * behind
        row.update({
            ("root", "scale"): _squash(1.0 + 0.012 * breath - 0.03 * pop),
            # the slouch: the whole of him tips back and the legs come forward under him
            ("root", "rotation"): mul(qz(1.5 * quiff), qx(-5.0 * pocket - 2.5 * behind - 2.5 * rock)),
            ("leg-left", "rotation"): mul(qz(1.5 - 1.5 * quiff + 2.0 * pocket), qx(5.0 * pocket - 9.0 * tap + 2.5 * behind + 2.5 * rock)),
            ("leg-right", "rotation"): mul(qz(-1.5 - 1.5 * quiff - 2.0 * pocket), qx(5.0 * pocket + 2.5 * behind + 2.5 * rock)),
            ("torso", "rotation"): mul(qx(0.8 * breath - 3.0 * pocket - 4.0 * behind), qz(-2.0 * quiff), qy(6.0 * quiff)),
            ("head", "rotation"): mul(qy(look), qx(9.0 * pocket - 0.8 * breath + 4.0 * quiff - 12.0 * behind - 3.0 * flick),
                                      qz(4.0 * pocket + 9.0 * quiff + 4.0 * behind)),
        })
        _collect(out, t, row)
    return out


def walk():
    """The stroll: leaning back, shoulders rolling, a loose left arm and a lazy right one."""
    out = {}
    length = 0.72
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.85)                          # barely snapped: he is not hurrying
        late = math.sin(phase - 0.55)                # the arms and the shoulders trail the legs
        air = abs(math.sin(phase - 0.25)) ** 1.1     # a small bounce that arrives late
        row = {
            ("root", "translation"): (0.0, 0.003 + 0.026 * air, 0.0),
            # the hips dip toward the planted foot, and the whole of him leans back
            ("root", "rotation"): mul(qx(-3.5), qz(3.0 * s)),
            ("root", "scale"): _squash(0.975 + 0.05 * air),
            ("leg-left", "rotation"): mul(qx(-34.0 * sh), qz(3.0 - 3.0 * s)),
            ("leg-right", "rotation"): mul(qx(34.0 * sh), qz(-3.0 - 3.0 * s)),
            # the shoulders roll against the hips and swing wide
            ("torso", "rotation"): mul(qx(-2.0), qy(13.0 * late), qz(-4.5 * late)),
            # chin up, head cocked to his left, and it stays level while the shoulders turn
            ("head", "rotation"): mul(qx(-5.0 - 2.0 * math.sin(2.0 * phase - 1.0)), qy(-8.0 * late), qz(4.0 + 2.0 * late)),
            # HIS LEFT ARM: long and loose, nearly straight, the forearm lagging behind the swing
            ("arm-left", "rotation"): mul(qx(34.0 * late), qz(-70.0)),
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(16.0 + 12.0 * math.sin(phase - 1.25)))),
            # HIS RIGHT ARM: held bent at the hip, hardly swinging
            ("arm-right", "rotation"): mul(qx(-10.0 * late + 6.0), qz(66.0)),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(46.0 + 5.0 * late)),
        }
        _collect(out, t, row)
    return out


def sprint():
    """The spark: far forward, skimming, the legs a blur and the arms thrown back behind him."""
    out = {}
    length = 0.48
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.5)                           # hard snap: the legs hang at full stride
        air = abs(s) ** 0.8
        row = {
            ("root", "translation"): (0.0, 0.006 + 0.024 * air, 0.0),
            ("root", "rotation"): qx(15.0),
            # he stretches ALONG the run, not up: long and low at full stride
            ("root", "scale"): (1.0 - 0.03 * air, 1.0 - 0.04 * air, 1.0 + 0.08 * air),
            ("leg-left", "rotation"): mul(qx(-58.0 * sh - 6.0), qz(3.0)),
            ("leg-right", "rotation"): mul(qx(58.0 * sh - 6.0), qz(-3.0)),
            ("torso", "rotation"): mul(qx(11.0), qy(9.0 * sh)),
            # the head stays up and ahead over a body that is nearly lying on the air
            ("head", "rotation"): mul(qx(-22.0 - 2.0 * math.sin(2.0 * phase)), qy(-5.0 * sh)),
            # arms back and out, trailing: the upper arm swept 58 degrees behind hanging, the
            # forearm almost straight and turned out, a small shiver instead of a pump
            # (hung at 58 the sleeve pressed into the jacket's side; 46 holds the elbow clear)
            ("arm-left", "rotation"): mul(qx(58.0 + 6.0 * sh), qz(-46.0)),
            ("arm-right", "rotation"): mul(qx(58.0 - 6.0 * sh), qz(46.0)),
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(14.0 - 7.0 * sh))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(14.0 + 7.0 * sh)),
        }
        _collect(out, t, row)
    return out


def jump():
    """The pop, from standing: both knees up together in front, tipped back, arms up in a V."""
    out = {}
    for t in _times(0.50):
        tuck = 1.0 - math.exp(-t / 0.07)             # quick: he is light
        ring = math.cos(2.0 * math.pi * t / 0.40) * math.exp(-t / 0.10)
        thrown = math.exp(-t / 0.22)
        # ARMS UP THROUGH THE ELBOW (rule 8), AND WIDE. The upper arm rises ARM_LIFT; the forearm
        # leans AWAY from the head, about 45 degrees off level in all, and stretches, so from the
        # front there is clear air between each fist and his cheek and the fists finish beside
        # the ears. Two things were tried and are not to be tried again:
        #   v02  forearm 36 to 44 with almost no stretch: fists at jaw height, a flex
        #   v03 to v07  the upper arm swung 26 degrees FORWARD so the forearm could stand steep
        #        ahead of the ear: the fists landed in front of his cheeks, a kid holding his
        #        face, and at 2.1x stretch two brown columns beside the head (coordinator review)
        lift = ARM_LIFT * (0.7 + 0.3 * thrown)
        ahead = 7.0                                    # just enough to pass in front of the ear
        # HIGHER SINCE THE HEAD CAME IN TO 0.84: his ears now end at x 0.188 where they ended at
        # 0.224, so the forearm can stand steeper beside them (about 60 degrees off level in all)
        # and the fists finish above the ears' tops, level with his brow.
        fore_l = 47.0 + 6.0 * thrown + 3.0 * ring
        fore_r = 43.0 + 6.0 * thrown + 3.0 * ring     # his right a little lower
        reach = 1.65 + 0.40 * thrown
        row = {
            ("root", "scale"): _squash(1.0 + 0.16 * ring),
            ("root", "rotation"): qx(-7.0 * tuck),
            # both knees up in FRONT of him by the same amount, a mirror pair, feet a little apart
            ("leg-left", "rotation"): mul(qx(-40.0 * tuck), qz(6.0 * tuck)),
            ("leg-right", "rotation"): mul(qx(-40.0 * tuck), qz(-6.0 * tuck)),
            ("torso", "rotation"): qx(-6.0 * math.exp(-t / 0.10) - 3.0 * tuck),
            ("head", "rotation"): mul(qx(4.0 * tuck), qz(3.0)),
            ("arm-left", "rotation"): mul(qy(-ahead), qz(lift)),
            ("arm-right", "rotation"): mul(qy(ahead), qz(-lift)),
            ("forearm-left", "rotation"): qz(fore_l),
            ("forearm-right", "rotation"): qz(-fore_r),
            ("forearm-left", "scale"): (reach, 1.0, 1.0),
            ("forearm-right", "scale"): (0.92 * reach, 1.0, 1.0),
        }
        _collect(out, t, row)
    return out


def fall():
    """A loop: reclined and unbothered, legs out in front paddling, arms up and waving out of step."""
    out = {}
    length = 1.0 / 3.0
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        c = math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.05 + 0.012 * math.sin(2.0 * phase)),
            ("root", "rotation"): qx(-6.0),
            ("leg-left", "rotation"): mul(qx(-24.0 + 8.0 * s), qz(6.0)),
            ("leg-right", "rotation"): mul(qx(-14.0 - 8.0 * s), qz(-6.0)),
            ("torso", "rotation"): qx(-3.0),
            ("head", "rotation"): mul(qx(13.0), qz(3.0)),
            # the same V as `jump`, held lower and looser; each forearm waves in its own time
            ("arm-left", "rotation"): mul(qy(-7.0 + 4.0 * c), qz(0.8 * ARM_LIFT + 3.0 * s)),
            ("arm-right", "rotation"): mul(qy(7.0 + 4.0 * c), qz(-0.8 * ARM_LIFT + 3.0 * s)),
            ("forearm-left", "rotation"): qz(44.0 + 7.0 * s),
            ("forearm-right", "rotation"): qz(-(40.0 + 7.0 * c)),
            ("forearm-left", "scale"): (1.70, 1.0, 1.0),
            ("forearm-right", "scale"): (1.65, 1.0, 1.0),
        }
        _collect(out, t, row)
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall}
