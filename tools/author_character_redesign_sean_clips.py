"""New locomotion clips for the Sean (displayed: Rago) redesign PROTOTYPE.

Imported by tools/author_character_redesign_sean.py, which writes them into the prototype .glb
IN PLACE OF the clips of the same name copied from team-sean.glb. Nothing in the game reads
them; team-sean.glb keeps its own.

WHY. Owner, 2026-10-05, on the game's `walk`, `sprint`, `jump` and `fall`: *"the animations we
have dont fit the poppy/cartoony style aesthetic that the rest of our game has"*; then, for the
whole cast, that each hero's motion is its OWN (docs/CHARACTER_REDESIGN_DANTE.md section 13
rule 9, AGENTS.md: copied cast-wide looks are not allowed): *a heavy hero lands heavy, a light
one floats*.

WHAT HIS MOTION SHOULD SAY ABOUT HIM. He is the heavyweight: the tallest, the widest, built like
a brick, a brawler who smirks. Written down before any number was typed:
  1. WEIGHT GOES DOWN, NOT UP. Dante is thrown up between steps. Rago's body is at its LOWEST on
     every footfall and squashes there; between steps it only climbs back over the planted leg.
     So the bounce is a DROP with a hard bottom, and there is next to no stretch at the top.
  2. HE TAKES UP ROOM. The arms hang wide of the body with the fists forward, a boxer's carry,
     and never tuck in. The feet land wide. The shoulders ROLL (the torso tips side to side over
     the planted foot) on top of the twist, and the whole body sways onto that foot.
  3. THE ARMS ARE LATE. A big arm swings a beat behind the leg that drives it: every arm channel
     is delayed by ARM_LAG of a cycle, and the forearm pumps later still.
  4. CHIN DOWN, EYES UP. The chest leads (the torso leans BACK a little in the walk), the head
     stays dipped. In the sprint he lowers the head and charges like a bull, far further forward
     than anyone light could lean.
  5. IN THE AIR HE IS A FALLING WEIGHT. No flapping. The jump is a shove off the ground with
     both fists punched up, then a strongman's flex held locked; the fall is the same arms up,
     wide and trembling, braced for the landing.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK
(and tips the head, which stands above its pivot, FORWARD); about +z a negative angle drops his
left arm from straight out toward hanging, and tips the torso's top toward his left.

Each clip but `idle` keeps the LENGTH of the one it replaces in team-sean.glb (walk 0.72 s,
sprint 0.48, jump 0.50, fall 0.3333; the idle is 8 s where the old one is 1.3333, see `idle`) so nothing that times itself off a clip changes. NOT SEEN IN UNITY: the
game layers procedural motion on these bones, and the `root` scale channel is new.

THE ELBOWS (rule 8). `forearm-left` and `forearm-right` fold FORWARD and are turned OUT by
SPLAY, never across the chest: his forearm is a 150 mm wide bracer and his chest stands 25 mm
proud, so a forearm folded straight ahead would sit inside the vest. A STRAIGHT arm never rises
above straight out (his head is wider than his shoulders); in the air the arms go up THROUGH
THE ELBOW (the rule as amended, see `jump`).
"""
import math

RATE = 60.0
SPLAY = 38.0      # degrees a folded forearm is turned out from straight ahead: wider than Dante's 30
ARM_LAG = 0.07    # of a cycle: how far the arms trail the legs
ARM_LIFT = 11.0   # degrees the upper arm rises past straight out in the air; more is in the jaw
# ⚠️ THE FOREARM STANDS AT MOST ABOUT 55 DEGREES FROM LEVEL. His ears reach 0.227 from the centre
# and the bracer's rim is 74 mm from the forearm's axis: at 62 degrees the rim was in the ear, at
# 80 in the cheek (seen on the first build's frame 0). So the V is wide, and what makes the
# take-off read as a throw is the lift of the upper arm and the STRETCH of the forearm.
# ⚠️ AND THEN THE HEAD CAME IN TO 0.84 (the model script's HEAD_SCALE): the ears now reach 0.191
# and not 0.227, so the forearm can stand about 73 degrees from level before the rim meets the
# ear. The fists go up BESIDE AND ABOVE the ears now, where before they stopped at the cheek.
FLEX = 62.0       # degrees the forearm stands up from the upper arm in the held pose (was 49)
PUNCH = 62.0      # and at the top of the throw, when the upper arm is lifted the full ARM_LIFT (was 45)
REACH = 0.40      # how much longer the forearm is at the instant of the throw


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


def _stomp(length, leg, stance, arm, hang, bend, pump, drop, rise, squash, sway, roll, twist, lean, chest, chin):
    """One cycle of two footfalls. The body is LOWEST at each footfall (the legs at full stride)."""
    out = {}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        swing = _snap(s, 0.8)
        # 1 at a footfall, 0 as the legs pass. Sharpened, so he sits on the ground only briefly
        # and the drop reads as a hit rather than a bob.
        hit = abs(s) ** 2.6
        late = math.sin(phase - 2.0 * math.pi * ARM_LAG)
        arm_swing = _snap(late, 0.9)
        fore = math.sin(phase - 2.0 * math.pi * (ARM_LAG + 0.06))
        # which foot the weight is on: +1 his left (his left leg is forward while s > 0)
        side = _snap(s, 0.6)
        row = {
            ("root", "translation"): (sway * side, rise * (1.0 - hit) - drop * hit, 0.0),
            ("root", "rotation"): qx(lean),
            ("root", "scale"): _squash(1.0 - squash * hit + 0.25 * squash * (1.0 - hit)),
            ("leg-left", "rotation"): mul(qx(-leg * swing), qz(stance)),
            ("leg-right", "rotation"): mul(qx(leg * swing), qz(-stance)),
            # the shoulders roll over the planted foot and turn against the hips
            ("torso", "rotation"): mul(qx(chest), mul(qz(-roll * side), qy(twist * swing))),
            # the head undoes the roll and most of the twist, and keeps the chin down
            ("head", "rotation"): mul(qx(chin + 2.0 * hit), mul(qz(0.8 * roll * side), qy(-0.7 * twist * swing))),
            ("arm-left", "rotation"): mul(qx(arm * arm_swing), qz(-hang)),
            ("arm-right", "rotation"): mul(qx(-arm * arm_swing), qz(hang)),
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(bend - pump * fore))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(bend + pump * fore)),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def walk():
    """A swagger. Short heavy steps on wide feet, the chest out, the fists carried forward and low."""
    return _stomp(0.72, leg=31.0, stance=6.0, arm=22.0, hang=57.0, bend=44.0, pump=9.0, drop=0.020, rise=0.012,
                  squash=0.075, sway=0.014, roll=5.5, twist=11.0, lean=1.0, chest=-4.0, chin=5.0)


def sprint():
    """A charge. The head goes down and forward and everything behind it pounds."""
    return _stomp(0.48, leg=47.0, stance=5.0, arm=40.0, hang=60.0, bend=84.0, pump=16.0, drop=0.026, rise=0.020,
                  squash=0.095, sway=0.010, roll=4.0, twist=9.0, lean=15.0, chest=11.0, chin=-13.0)


def jump():
    """A standing jump: a shove off both feet with BOTH FISTS THROWN UP, then a flexed, braced hold."""
    out = {}
    for t in _times(0.50):
        hold = 1.0 - math.exp(-t / 0.075)                  # the pose he gathers into
        # ONE heavy overshoot and it is done: a big body does not ring like a light one
        ring = math.exp(-t / 0.085) * math.cos(2.0 * math.pi * t / 0.60)
        # ARMS UP (rule 8 as amended: owner, on Dante, *"the jump looks like he's shrugging"*). A
        # straight arm cannot rise past straight out, so the upper arm lifts ARM_LIFT and the
        # forearm goes up from the elbow. At take-off it is a punch at the sky, the forearm
        # stretched; then it settles into the pose a strongman holds, upper arms level, fists up
        # beside the head, and he hangs there flexing. Dante throws a loose V and lets it ease;
        # Rago's arms LOCK.
        throw = math.exp(-t / 0.11)
        lift = ARM_LIFT * (0.45 + 0.55 * throw)
        fore = FLEX + (PUNCH - FLEX) * throw + 4.0 * ring
        reach = 1.0 + REACH * throw
        row = {
            ("root", "scale"): _squash(1.0 + 0.15 * ring - 0.02 * hold),
            ("root", "rotation"): qx(0.0),
            # A STANDING JUMP (rule 9; owner: *"is the jump supposed to be one foot forward? i need
            # a jump for standing still"*). Both legs do the same thing: they leave the ground
            # together, come a little forward and spread, a wide braced stance carried in the air.
            ("leg-left", "rotation"): mul(qx(-13.0 * hold), qz(12.0 * hold)),
            ("leg-right", "rotation"): mul(qx(-13.0 * hold), qz(-12.0 * hold)),
            ("torso", "rotation"): qx(-3.0 * math.exp(-t / 0.09) + 4.0 * hold),
            ("head", "rotation"): qx(-5.0 * math.exp(-t / 0.12) + 6.0 * hold),
            ("arm-left", "rotation"): mul(qy(6.0 * hold), qz(lift)),
            ("arm-right", "rotation"): mul(qy(-6.0 * hold), qz(-lift)),
            ("forearm-left", "rotation"): qz(fore),
            ("forearm-right", "rotation"): qz(-fore),
            ("forearm-left", "scale"): (reach, 1.0, 1.0),
            ("forearm-right", "scale"): (reach, 1.0, 1.0),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def fall():
    """A loop: fists up, braced wide and locked, shuddering, the eyes on the ground he is about to hit."""
    out = {}
    length = 1.0 / 3.0
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        shudder = math.sin(2.0 * phase)                    # twice a loop, small: effort, not flapping
        row = {
            ("root", "scale"): _squash(1.045 + 0.012 * shudder),
            ("root", "rotation"): qx(4.0),
            ("leg-left", "rotation"): mul(qx(-17.0 + 4.0 * s), qz(14.0)),
            ("leg-right", "rotation"): mul(qx(-9.0 - 4.0 * s), qz(-14.0)),
            ("torso", "rotation"): qx(9.0 + 1.5 * shudder),
            ("head", "rotation"): qx(11.0),
            # the arms stay UP, a little lower and wider than the jump's flex, as if holding the
            # air apart. They do not wave: both shake together, and only a little.
            ("arm-left", "rotation"): mul(qy(8.0), qz(0.3 * ARM_LIFT + 2.5 * shudder)),
            ("arm-right", "rotation"): mul(qy(-8.0), qz(-0.3 * ARM_LIFT - 2.5 * shudder)),
            ("forearm-left", "rotation"): qz(FLEX - 6.0 + 4.0 * s),
            ("forearm-right", "rotation"): qz(-(FLEX - 6.0 - 4.0 * s)),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


# ---------------------------------------------------------------------------
# THE IDLE. Arms are posed by where they POINT, stances are blended with eased windows.
# ---------------------------------------------------------------------------

def _norm(v):
    n = math.sqrt(sum(c * c for c in v)) or 1.0
    return tuple(c / n for c in v)


def _turn(q, v):
    """`v` turned by quaternion `q`."""
    x, y, z, w = q
    vx, vy, vz = v
    tx, ty, tz = 2.0 * (y * vz - z * vy), 2.0 * (z * vx - x * vz), 2.0 * (x * vy - y * vx)
    return (vx + w * tx + (y * tz - z * ty), vy + w * ty + (z * tx - x * tz), vz + w * tz + (x * ty - y * tx))


def _arc(a, b):
    """The shortest turn that takes direction `a` onto direction `b`."""
    a, b = _norm(a), _norm(b)
    d = sum(p * q for p, q in zip(a, b))
    if d < -0.9999:
        return (0.0, 0.0, 1.0, 0.0)
    c = (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
    q = (c[0], c[1], c[2], 1.0 + d)
    n = math.sqrt(sum(k * k for k in q))
    return tuple(k / n for k in q)


def _arm(side, upper, fore):
    """An arm posed by direction: the upper arm along `upper`, the forearm along `fore`, both
    given for his LEFT arm in the file's space (+x his left, +y up, +z ahead). `side` -1 mirrors
    them. Returns (arm rotation, forearm rotation in the arm's own space)."""
    rest = (float(side), 0.0, 0.0)
    upper = (side * upper[0], upper[1], upper[2])
    fore = (side * fore[0], fore[1], fore[2])
    q = _arc(rest, upper)
    return q, _arc(rest, _turn((-q[0], -q[1], -q[2], q[3]), _norm(fore)))


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
    if not start <= t <= end:
        return 0.0
    return _ease((t - start) / blend) * (1.0 - _ease((t - (end - blend)) / blend))


def _beat(t, at, width):
    """A single hit centred on `at`: up fast, down fast."""
    return math.exp(-((t - at) / width) ** 2)


#   where his LEFT arm points: (upper arm, forearm)
#   the stand: arms carried WIDE of the body, the fists a little forward. The first build hung
#   them at the original idle's 45 degrees, where the bracer's inner rim grazed the vest's side
#   and left a jagged sliver; five degrees wider and it stands clear.
ARMS_STAND = ((0.77, -0.64, 0.02), (0.64, -0.70, 0.32))
#   THE FLEX: the upper arm level and a touch high, the forearm stood up. No steeper than 52
#   degrees from level, or the bracer's rim is in his ear (see FLEX above).
ARMS_FLEX = ((0.985, 0.14, 0.10), (0.44, 0.88, 0.16))
ARMS_FLEX_PUMP = ((0.985, 0.17, 0.10), (0.36, 0.91, 0.20))
#   THE GUARD: elbows down and out, fists up in front of the shoulders, wide enough that the
#   bracers stand off the vest and the fists off the cheeks
ARMS_GUARD = ((0.70, -0.52, 0.49), (0.20, 0.85, 0.49))   # nearer the face than before the head came in (was 0.40 out)
#   THE JAB: the whole arm thrown ahead, a little out and down so it passes UNDER the jaw and
#   outside the chest, the forearm stretched as it lands
ARMS_JAB = ((0.34, -0.24, 0.91), (0.22, -0.16, 0.96))
#   THE BULL: arms back and wide, fists low, as he leans over his front foot
ARMS_BULL = ((0.74, -0.62, -0.26), (0.66, -0.62, 0.42))
JAB_REACH = 0.38
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of a brawler with nobody to fight. He stands; he flexes his left arm and
    looks at it, pleased with himself; he drops it, leans in and paws the ground twice like a
    bull; he brings his guard up, throws a left and a right at the air, and lets his hands fall.

    ⚠️ THE SECOND IDLE. The first layered lagging sines on every bone (the first wording of rule
    9). Owner, 2026-10-05, on Dante's: *"the idle looks like he's just distorting around.. needs
    more character, like sometimes he puts his hands on the waist, or crosses his arms, other
    things idle people do"*. So it is ACTED: three stances a waiting person takes, each held
    long enough to read, with the breathing kept small underneath.

    WHY THESE THREE AND NOT DANTE'S. Dante plants his fists on his hips and folds his arms: a
    sullen kid. Rago is vain and restless. His arms are also the wrong shape for either of
    Dante's: the upper arm is 68 mm and the forearm is a 150 mm wide bracer, so a fist cannot
    reach his own waist or cross his chest without passing through the vest. What his arms CAN
    do is what a strongman and a boxer do, which is who he is.

    ⚠️ IT IS 8 s, NOT THE OLD 1.33 s. Anything in the game that assumes the idle's length needs
    a look, and the game may prefer these as separate clips it picks between.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / 2.0)             # a slow breath every two seconds
        flex = _window(t, 0.7, 3.0, 0.40)
        bull = _window(t, 3.3, 5.0, 0.35)
        guard = _window(t, 5.3, 7.6, 0.35)
        pump = flex * (_beat(t, 1.75, 0.13) + _beat(t, 2.25, 0.13))
        scrape = bull * max(0.0, math.sin(2.0 * math.pi * 1.7 * (t - 3.72))) ** 2
        jab_left, jab_right = guard * _beat(t, 6.05, 0.085), guard * _beat(t, 6.55, 0.085)
        # the beat a stance is taken on: a small drop of the whole body
        pop = _beat(t, 1.0, 0.10) + _beat(t, 5.6, 0.10) + 0.7 * (jab_left + jab_right)
        row = {}
        for side, name, flexes, jab in ((1, "left", flex, jab_left), (-1, "right", 0.0, jab_right)):
            stand = _arm(side, *ARMS_STAND)
            poses = ((_arm(side, *ARMS_FLEX), flexes), (_arm(side, *ARMS_FLEX_PUMP), flexes * pump),
                     (_arm(side, *ARMS_BULL), bull), (_arm(side, *ARMS_GUARD), guard), (_arm(side, *ARMS_JAB), jab))
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = stand[k]
                for pose, weight in poses:
                    q = _mix(q, pose[k], weight)
                row[(bone, "rotation")] = q
            row[("forearm-" + name, "scale")] = (1.0 + JAB_REACH * jab, 1.0, 1.0)
        # he looks at his own arm while he flexes it, glares ahead and down as the bull, and
        # each jab turns the shoulders behind it
        look = 30.0 * _window(t, 1.2, 2.8, 0.35)
        nod = 4.0 * flex + 12.0 * bull + 3.0 * guard
        twist = -5.0 * flex + 13.0 * (jab_right - jab_left)
        lean = -3.0 * flex + 13.0 * bull + 3.0 * guard
        shift = 3.0 * flex - 2.5 * bull
        row.update({
            ("root", "scale"): _squash(1.0 + 0.014 * breath - 0.035 * pop),
            ("root", "rotation"): mul(qx(0.6 * lean), qz(shift)),
            ("torso", "rotation"): mul(mul(qx(0.5 * lean - 1.0 * breath), qy(twist)), qz(-0.8 * shift)),
            ("head", "rotation"): mul(mul(qy(look - 0.8 * twist), qx(nod)), qz(-0.5 * shift)),
            ("leg-left", "rotation"): mul(qz(2.0 - shift), qx(-5.0 * bull)),
            # the pawing foot: swung back and dragged home, twice
            ("leg-right", "rotation"): mul(qz(-2.0 - shift), qx(4.0 * bull + 22.0 * scrape)),
        })
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall}
