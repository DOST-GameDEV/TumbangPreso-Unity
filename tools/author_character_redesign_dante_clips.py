"""New locomotion clips for the Dante (displayed: Dante) redesign PROTOTYPE.

Imported by tools/author_character_redesign_dante.py, which writes them into the two prototype
.glb files IN PLACE OF the clips of the same name copied from team-dante.glb. Nothing in the
game reads them; team-dante.glb keeps its own.

WHY. Owner, 2026-10-05, after seeing the game's `walk`, `sprint`, `jump` and `fall` on the new
meshes: *"the animations we have dont fit the poppy/cartoony style aesthetic that the rest of
our game has"*. The cast's rig has seven bones and no knees or elbows (the prototype adds two elbows), so "poppy" has to come from
what a block toy can do: a real bounce, squash on the contact and stretch at the top (scale on
`root`, whose origin is at the feet), a snapped rather than sinusoidal swing, the torso twisting
against the legs and the head staying level over it.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops his left arm from straight out to hanging.

Each clip keeps the LENGTH of the one it replaces (walk 0.72 s, sprint 0.48, jump 0.50,
fall 0.33) so nothing that times itself off a clip changes. NOT YET SEEN IN UNITY: the game
layers procedural motion on these bones (`CharacterAnimator.LocomotionArms`, `LocomotionWeight`)
and the `root` scale channel is new; both need a look there before any of this is believed.
"""
import math

RATE = 60.0
SPLAY = 30.0   # degrees a folded forearm is turned out from straight ahead
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


def _stride(length, leg, arm, hang, bend, pump, bounce, low, stretch, lean, torso_lean, twist, head_up):
    out = {}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.7)
        air = abs(s) ** 0.85          # 0 as the legs pass, 1 at full stride: he is thrown up between steps
        row = {
            ("root", "translation"): (0.0, low + bounce * air, 0.0),
            ("root", "rotation"): qx(lean),
            ("root", "scale"): _squash((1.0 - stretch) + 2.0 * stretch * air),
            ("leg-left", "rotation"): mul(qx(-leg * sh), qz(3.0)),
            ("leg-right", "rotation"): mul(qx(leg * sh), qz(-3.0)),
            # the shoulders turn against the hips, and the head turns back so the face stays ahead
            ("torso", "rotation"): mul(qx(torso_lean), qy(twist * sh)),
            ("head", "rotation"): mul(qx(-head_up - 3.0 * math.sin(2.0 * phase - 0.8)), qy(-0.6 * twist * sh)),
            ("arm-left", "rotation"): mul(qx(arm * sh), qz(-hang)),
            ("arm-right", "rotation"): mul(qx(-arm * sh), qz(hang)),
            # THE ELBOWS (the prototype's two extra bones). The forearm folds FORWARD, and folds
            # and the fold plus the swing never passes about 105 degrees from hanging: past that the
            # fist comes back at his own chest. In the arm's own space the fold is a turn about y.
            # ⚠️ AND IT FOLDS OUTWARD, NOT ACROSS HIM. Folded straight ahead, a forearm as wide as his
            # is sat inside the coat's front. Owner, 2026-10-05: *"make it so the arms arent bent too
            # much towards the inside of the torso"*. `SPLAY` turns the folded forearm away from the
            # body, and the upper arm hangs further out (`hang`) so the elbow clears the skirt.
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(bend - pump * sh))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(bend + pump * sh)),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def walk():
    return _stride(0.72, leg=40.0, arm=42.0, hang=66.0, bend=38.0, pump=10.0, bounce=0.048, low=0.004, stretch=0.065,
                   lean=3.0, torso_lean=4.0, twist=9.0, head_up=6.0)


def sprint():
    return _stride(0.48, leg=52.0, arm=48.0, hang=62.0, bend=72.0, pump=0.0, bounce=0.062, low=0.010, stretch=0.085,
                   lean=9.0, torso_lean=9.0, twist=13.0, head_up=16.0)


def jump():
    """Thrown up long and thin with the arms flung up in a V, then both legs gathered up in front and held."""
    out = {}
    for t in _times(0.50):
        tuck = 1.0 - math.exp(-t / 0.09)
        ring = math.cos(2.0 * math.pi * t / 0.44) * math.exp(-t / 0.12)
        # ARMS UP, AS HIGH AS THIS BODY ALLOWS. Owner, 2026-10-05: *"the arms dont really get much
        # higher than the original A posing.. the jump looks like he's shrugging"*. His head is wider
        # than his shoulders and sits on top of the arm, so a straight arm lifted 20 degrees is in
        # the collar and at 50 in the head. The ELBOW is the way round: the upper arm lifts only
        # `ARM_LIFT` (clear of the collar's shelf) and the forearm folds UP and out past the cheek,
        # a V thrown up at take-off that eases a little as he hangs.
        lift = ARM_LIFT * (0.55 + 0.45 * math.exp(-t / 0.20))
        fore = 34.0 + 12.0 * math.exp(-t / 0.20) + 5.0 * ring
        reach = 1.22 + 0.33 * math.exp(-t / 0.18)
        row = {
            ("root", "scale"): _squash(1.0 + 0.19 * ring),
            ("root", "rotation"): qx(0.0),
            # ⚠️ A STANDING JUMP: BOTH FEET TOGETHER. The clip this replaces held a stride, one foot
            # forward and one back, and the first version here kept that. Owner, 2026-10-05: *"is the
            # jump supposed to be one foot forward? i need a jump for standing still"*. The game has
            # one `jump` clip, played from a standstill as well as from a run, so it is symmetric:
            # the toes trail as he leaves the ground, then both legs swing up in front, a little apart.
            ("leg-left", "rotation"): mul(qx(10.0 - 34.0 * tuck), qz(2.0 + 7.0 * tuck)),
            ("leg-right", "rotation"): mul(qx(10.0 - 34.0 * tuck), qz(-2.0 - 7.0 * tuck)),
            ("torso", "rotation"): qx(-9.0 * math.exp(-t / 0.10) + 6.0 * tuck),
            ("head", "rotation"): qx(-9.0 * tuck),
            ("arm-left", "rotation"): mul(qy(8.0 * tuck), qz(lift)),
            ("arm-right", "rotation"): mul(qy(-8.0 * tuck), qz(-lift)),
            ("forearm-left", "rotation"): qz(fore),
            ("forearm-right", "rotation"): qz(-fore),
            # and the forearm STRETCHES as it is thrown, a cartoon reach: with a head this size it is
            # the only way a fist gets past the cheek toward the top of the head
            ("forearm-left", "scale"): (reach, 1.0, 1.0),
            ("forearm-right", "scale"): (reach, 1.0, 1.0),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def fall():
    """A loop: stretched, arms up and waving, legs dangling apart and kicking in turn, eyes on the ground."""
    out = {}
    length = 0.33
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        row = {
            ("root", "scale"): _squash(1.07 + 0.02 * math.sin(2.0 * phase)),
            ("leg-left", "rotation"): mul(qx(-12.0 + 13.0 * s), qz(9.0)),
            ("leg-right", "rotation"): mul(qx(-12.0 - 13.0 * s), qz(-9.0)),
            ("torso", "rotation"): qx(7.0),
            ("head", "rotation"): qx(9.0),
            # arms up in the same V as `jump`, each forearm waving in turn
            ("arm-left", "rotation"): mul(qy(10.0 * math.cos(phase)), qz(0.5 * ARM_LIFT + 5.0 * s)),
            ("arm-right", "rotation"): mul(qy(10.0 * math.cos(phase)), qz(-0.5 * ARM_LIFT + 5.0 * s)),
            ("forearm-left", "rotation"): qz(40.0 + 14.0 * s),
            ("forearm-right", "rotation"): qz(-(40.0 - 14.0 * s)),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


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


def _window(t, start, hold, end, blend):
    """0 outside [start, end], 1 from `hold` to `end - blend`, eased in and out over `blend`."""
    return _ease((t - start) / blend) * (1.0 - _ease((t - (end - blend)) / blend)) if start <= t <= end else 0.0


#   where his LEFT arm points in each stance: (upper arm, forearm). The right is the mirror
#   unless given its own.
ARMS_HANG = ((0.67, -0.74, 0.02), (0.56, -0.74, 0.37))
#   fists on the hips: the elbow is pushed out and BACK so a forearm this long can come forward
#   to the side of the waist without ending inside the coat
ARMS_HIPS = ((0.58, -0.42, -0.70), (0.30, -0.40, 0.87))
#   arms folded: both elbows forward, the forearms across the front of the chest, his left over
#   his right. The chest is deep and the upper arm is 72 mm, so the forearms lie ahead of the
#   frog bars and not against the cloth.
ARMS_FOLD_LEFT = ((0.46, -0.20, 0.86), (-0.92, 0.06, 0.38))
ARMS_FOLD_RIGHT = ((0.46, -0.42, 0.78), (-0.90, -0.14, 0.42))
#   and the forearms STRETCH as they fold (scale on the forearm bone). At their own length two
#   120 mm forearms only meet in the middle, which reads as fists pressed together; to CROSS,
#   each has to reach past the other.
FOLD_REACH = 0.55
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of a kid waiting: he stands, plants his fists on his hips and looks about,
    drops them, folds his arms and taps a foot, lets go.

    ⚠️ THREE TRIES. The clip this replaces moved three bones on straight ramps. The first new one
    layered sines on every bone; owner, 2026-10-05: *"the idle looks like he's just distorting
    around.. needs more character, like sometimes he puts his hands on the waist, or crosses his
    arms, other things idle people do"*. Motion with no intent reads as a mesh wobbling. So the
    idle is now ACTED: stances a person takes, held long enough to read, with the breathing kept
    small underneath. Arms are posed by where they point (`_arm`), not by joint angles.

    ⚠️ IT IS 8 s, NOT THE OLD 1.33 s. Stances need time. Anything in the game that assumes the
    idle's length needs a look, and the game may prefer these as separate clips it picks between.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        p = 2.0 * math.pi * t / (IDLE_LENGTH / 6.0)        # one breath every 1.33 s, six a loop
        breath = math.sin(p)
        hips = _window(t, 1.2, 1.7, 3.9, 0.45)             # fists on the hips
        fold = _window(t, 4.7, 5.2, 7.6, 0.45)             # arms folded
        # a small hop of the shoulders as each stance is taken, so the change has a beat
        pop = math.exp(-((t - 1.45) / 0.10) ** 2) + math.exp(-((t - 4.95) / 0.10) ** 2)
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            hang = _arm(side, *ARMS_HANG)
            onhip = _arm(side, *ARMS_HIPS)
            folded = _arm(side, *(ARMS_FOLD_LEFT if side > 0 else ARMS_FOLD_RIGHT))
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                q = _mix(_mix(hang[k], onhip[k], hips), folded[k], fold)
                row[(bone, "rotation")] = q
            row[("forearm-" + name, "scale")] = (1.0 + FOLD_REACH * fold, 1.0, 1.0)
        # where he looks: about, with his fists on his hips; down and away, with his arms folded
        look = 26.0 * (_window(t, 1.9, 2.2, 2.9, 0.3) - _window(t, 2.8, 3.1, 3.7, 0.3)) - 14.0 * _window(t, 5.3, 5.6, 7.2, 0.3)
        nod = 5.0 * _window(t, 5.3, 5.6, 7.2, 0.3) - 4.0 * hips
        lean = 3.0 * hips - 5.0 * fold                      # chest out on the hips, rocked back when folded
        shift = 3.0 * hips - 2.5 * fold                     # his weight goes to one foot, then the other
        # the impatient foot: toes of the free leg tapping while the arms are folded
        tap = fold * max(0.0, math.sin(2.0 * math.pi * 2.5 * t)) ** 2
        row.update({
            ("root", "scale"): _squash(1.0 + 0.016 * breath - 0.035 * pop),
            ("root", "rotation"): qz(shift),
            ("torso", "rotation"): mul(qx(-lean + 1.2 * breath), qz(-0.8 * shift)),
            ("head", "rotation"): mul(mul(qy(look), qx(nod - 1.0 * breath)), qz(-0.6 * shift + 4.0 * fold)),
            ("leg-left", "rotation"): mul(qz(1.0 - 1.0 * shift), qx(-9.0 * tap)),
            ("leg-right", "rotation"): qz(-1.0 - 1.0 * shift),
        })
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall}


# ------------------------------------------------------------------ MOUNTAIN FORM
#
# HIS OWN BODY IN THE ARMOUR (owner, 2026-10-18: *"baguhin mo na ren animation niya"*, change his animations too, rather
# than fitting the suit to the kid's clips). After CONTINENTAL DRIFT he wears `DanteMountainArmour` for 30 s, a bulk suit
# of basalt with a boulder past each hand. A kid's bounce and folded arms inside that read as the suit floating on him.
# So these are a HEAVY body's: the boulder arms held wide off the suit (they cannot hang inside it), a wide planted
# stance, steps that STOMP (no bounce up, a drop and a squash on the contact), the weight rolling over the planted foot,
# and the head low and forward. `CharacterAnimator.ClipVariant` plays `mountain-<name>` in place of `<name>` while the
# suit is on; walk and sprint keep the kid's lengths so the gait's cadence calibration is unchanged.
#
# ⚠️ NO KNEES. The rig bends at the hip only, so "low" cannot be a crouch: it is the wide splay of the legs (which drops
# the hips a little; the root is lowered to keep the feet on the court) and the chest leaned over them.

MOUNTAIN_SPLAY = 7.0        # degrees each leg is turned out from straight down: the wide stance
MOUNTAIN_SINK = -0.0016     # the root lowered by what that splay lifts the feet (a 0.18 m leg at 7 degrees)
#   the boulder arms, held wide: the upper arm out and down, the forearm forward and out, so the fist boulders sit
#   beside the hips and clear of the suit's skirt (his LEFT arm; the right is the mirror)
MOUNTAIN_ARMS = ((0.80, -0.56, 0.10), (0.55, -0.62, 0.56))
MOUNTAIN_ARMS_UP = ((0.93, 0.26, 0.20), (0.35, 0.90, 0.25))      # flung up and out: a gorilla's leap, not a V
MOUNTAIN_ARMS_BRACE = ((0.90, -0.10, 0.40), (0.30, -0.55, 0.78))  # out in front of him to meet the court


def _mountain_arms(row, pose, swing=0.0):
    """Both arms posed by where they point, each swung `swing` degrees about his left-right axis (+ back)."""
    for side, name in ((1, "left"), (-1, "right")):
        upper, fore = _arm(side, *pose)
        sw = swing * side
        row[("arm-" + name, "rotation")] = mul(qx(sw), upper)
        row[("forearm-" + name, "rotation")] = fore


def _mountain_stride(length, leg, arm, bounce, squash, roll, lean, torso_lean, twist, head_down):
    """THE STOMP: a short heavy stride. The body VAULTS over the planted leg (highest as the legs pass) and DROPS onto the
    next foot with a squash; the whole body rolls onto the planted side; the arms swing a little, held wide."""
    out = {}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        sh = _snap(s, 0.6)
        reach = abs(s)
        contact = reach ** 10                    # sharp: the foot lands and the body takes it
        vault = 1.0 - reach ** 1.6               # highest as the legs pass under him
        row = {
            ("root", "translation"): (0.0, MOUNTAIN_SINK + bounce * vault, 0.0),
            ("root", "rotation"): mul(qx(lean), qz(roll * sh)),
            ("root", "scale"): _squash(1.0 - squash * contact),
            ("leg-left", "rotation"): mul(qx(-leg * sh), qz(MOUNTAIN_SPLAY)),
            ("leg-right", "rotation"): mul(qx(leg * sh), qz(-MOUNTAIN_SPLAY)),
            ("torso", "rotation"): mul(qx(torso_lean + 2.0 * contact), qy(twist * sh)),
            ("head", "rotation"): mul(qx(head_down - 2.5 * contact), qy(-0.7 * twist * sh)),
        }
        _mountain_arms(row, MOUNTAIN_ARMS, swing=arm * sh)
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def mountain_walk():
    return _mountain_stride(0.72, leg=30.0, arm=12.0, bounce=0.005, squash=0.07, roll=4.5, lean=2.0, torso_lean=7.0,
                            twist=5.0, head_down=4.0)


def mountain_sprint():
    return _mountain_stride(0.48, leg=42.0, arm=22.0, bounce=0.009, squash=0.09, roll=3.5, lean=10.0, torso_lean=13.0,
                            twist=8.0, head_down=-2.0)


MOUNTAIN_IDLE_LENGTH = 3.2


def mountain_idle():
    """A mountain at rest: wide and planted, the shoulders heaving on a slow heavy breath, the weight shifting from foot to
    foot once a loop, the fists flexing out and the head turning over the court."""
    out = {}
    for t in _times(MOUNTAIN_IDLE_LENGTH):
        u = t / MOUNTAIN_IDLE_LENGTH
        breath = math.sin(2.0 * math.pi * u * 2.0)          # two deep breaths a loop
        shift = math.sin(2.0 * math.pi * u)                  # the weight over one foot, then the other
        flex = _window(t, 1.1, 1.3, 2.0, 0.2)                # the fists pushed out once, a show of the boulders
        look = 14.0 * math.sin(2.0 * math.pi * u + 0.6)
        row = {
            ("root", "translation"): (0.0, MOUNTAIN_SINK, 0.0),
            ("root", "scale"): _squash(1.0 + 0.022 * breath),
            ("root", "rotation"): qz(2.2 * shift),
            ("leg-left", "rotation"): qz(MOUNTAIN_SPLAY - 2.2 * shift),
            ("leg-right", "rotation"): qz(-MOUNTAIN_SPLAY - 2.2 * shift),
            ("torso", "rotation"): mul(qx(6.0 - 2.5 * breath), qz(-1.4 * shift)),
            ("head", "rotation"): mul(mul(qy(look), qx(5.0 + 1.5 * breath)), qz(-0.8 * shift)),
        }
        upper, fore = MOUNTAIN_ARMS
        out_upper = (upper[0] + 0.12 * flex, upper[1] + 0.10 * flex + 0.03 * breath, upper[2])
        _mountain_arms(row, (out_upper, fore), swing=0.0)
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def mountain_jump():
    """The heavy leap: he does not spring, he HEAVES. Squashed hard at take-off, then the boulder arms flung up and out
    and the legs drawn up apart under the suit."""
    out = {}
    for t in _times(0.50):
        heave = 1.0 - math.exp(-t / 0.08)
        ring = math.cos(2.0 * math.pi * t / 0.40) * math.exp(-t / 0.10)
        row = {
            ("root", "translation"): (0.0, MOUNTAIN_SINK, 0.0),
            ("root", "scale"): _squash(1.0 + 0.14 * ring),
            ("root", "rotation"): qx(4.0 * heave),
            ("leg-left", "rotation"): mul(qx(8.0 - 26.0 * heave), qz(MOUNTAIN_SPLAY + 6.0 * heave)),
            ("leg-right", "rotation"): mul(qx(8.0 - 26.0 * heave), qz(-MOUNTAIN_SPLAY - 6.0 * heave)),
            ("torso", "rotation"): qx(-10.0 * math.exp(-t / 0.10) + 8.0 * heave),
            ("head", "rotation"): qx(-6.0 * heave),
        }
        q = []
        for k in range(2):
            a, b = MOUNTAIN_ARMS[k], MOUNTAIN_ARMS_UP[k]
            w = _ease(t / 0.16)
            q.append(tuple(a[i] + (b[i] - a[i]) * w for i in range(3)))
        _mountain_arms(row, (q[0], q[1]))
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def mountain_fall():
    """Coming down to break the court: arms out in front and wide to meet it, legs apart and reaching, eyes down."""
    out = {}
    length = 0.40
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        row = {
            ("root", "translation"): (0.0, MOUNTAIN_SINK, 0.0),
            ("root", "scale"): _squash(1.04 + 0.012 * math.sin(2.0 * phase)),
            ("leg-left", "rotation"): mul(qx(-10.0 + 6.0 * s), qz(MOUNTAIN_SPLAY + 5.0)),
            ("leg-right", "rotation"): mul(qx(-10.0 - 6.0 * s), qz(-MOUNTAIN_SPLAY - 5.0)),
            ("torso", "rotation"): qx(10.0),
            ("head", "rotation"): qx(12.0),
        }
        _mountain_arms(row, MOUNTAIN_ARMS_BRACE, swing=3.0 * s)
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def mountain_throw():
    """THE THROW'S LEGS (0.54 s, `ThrowGesture.ReleaseSeconds`). The arms, chest and head of a throw are posed by
    `CharacterAnimator.ThrowBody` over whatever plays; this is the body under it: the front (left) foot STAMPS forward at
    the release, the root drops and squashes onto it and the back leg drives, then he settles back into the stance."""
    out = {}
    length = 0.54
    for t in _times(length):
        stamp = _ease(t / 0.07) * (1.0 - _ease((t - 0.30) / 0.24))
        hit = math.exp(-((t - 0.08) / 0.035) ** 2)
        row = {
            ("root", "translation"): (0.0, MOUNTAIN_SINK, 0.0),
            ("root", "scale"): _squash(1.0 - 0.10 * hit),
            ("root", "rotation"): qx(7.0 * stamp + 3.0 * hit),
            ("leg-left", "rotation"): mul(qx(-26.0 * stamp), qz(MOUNTAIN_SPLAY + 3.0 * stamp)),
            ("leg-right", "rotation"): mul(qx(18.0 * stamp), qz(-MOUNTAIN_SPLAY)),
            ("torso", "rotation"): qx(6.0 + 6.0 * stamp),
            ("head", "rotation"): qx(3.0 - 4.0 * hit),
        }
        _mountain_arms(row, MOUNTAIN_ARMS, swing=-10.0 * stamp)
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


#   CARRYING (the game plays `holding-right` over the gait whenever a slipper is in his hand, which in MOUNTAIN FORM is most
#   of the time): the kid's carry folds both arms in against a chest that is now a suit of stone, so the boulder fists sank
#   into the plates. Here the left arm is held wide as in the stance and the right is brought forward and out, the fist (and
#   the slipper in it) ahead of the breastplate where the throw takes it from.
MOUNTAIN_ARMS_CARRY_RIGHT = ((0.62, -0.50, 0.60), (0.30, -0.22, 0.93))


def mountain_holding_right():
    """The carry: arms only (the gait layer owns the legs and the root), a held pose with a slow heavy breath in the arms."""
    out = {}
    length = 1.6
    for t in _times(length):
        breath = math.sin(2.0 * math.pi * t / length)
        left_upper, left_fore = MOUNTAIN_ARMS
        l_up, l_fo = _arm(1, (left_upper[0], left_upper[1] + 0.03 * breath, left_upper[2]), left_fore)
        r_upper, r_fore = MOUNTAIN_ARMS_CARRY_RIGHT
        r_up, r_fo = _arm(-1, (r_upper[0], r_upper[1] + 0.025 * breath, r_upper[2]), r_fore)
        row = {("arm-left", "rotation"): l_up, ("forearm-left", "rotation"): l_fo,
               ("arm-right", "rotation"): r_up, ("forearm-right", "rotation"): r_fo}
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def mountain_crouch():
    """SPENT (the fatigue pose, `crouch`): a mountain out of breath does not fold up small like the kid; it sags. The legs
    splay wider, the chest drops forward over them, the boulder fists hang low and forward almost to the court, the head
    hangs, and the whole suit heaves on a slow labouring breath."""
    out = {}
    length = 1.4
    for t in _times(length):
        breath = math.sin(2.0 * math.pi * t / length)
        row = {
            ("root", "translation"): (0.0, MOUNTAIN_SINK - 0.006, 0.0),
            ("root", "scale"): _squash(0.97 + 0.025 * breath),
            ("leg-left", "rotation"): mul(qx(-6.0), qz(MOUNTAIN_SPLAY + 5.0)),
            ("leg-right", "rotation"): mul(qx(-6.0), qz(-MOUNTAIN_SPLAY - 5.0)),
            ("torso", "rotation"): qx(26.0 - 3.5 * breath),
            ("head", "rotation"): qx(14.0 + 2.0 * breath),
        }
        _mountain_arms(row, ((0.55, -0.70, 0.45), (0.25, -0.88, 0.40)), swing=-4.0 * breath)
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def mountain_slide():
    """THE SLIDE (0.95 s, the kid's length): too heavy to skim, he ploughs. Dropped low on the planted back leg with the
    front leg driven out ahead, the chest leaned back against the speed, the boulder arms thrown out wide for balance; a
    jolt as he lands into it and a heave as he comes up out of it."""
    out = {}
    length = 0.95
    for t in _times(length):
        into = _ease(t / 0.12)
        up = _ease((t - 0.72) / 0.23)
        low = into * (1.0 - up)
        jolt = math.exp(-((t - 0.12) / 0.04) ** 2)
        row = {
            ("root", "translation"): (0.0, MOUNTAIN_SINK - 0.018 * low, 0.0),
            ("root", "scale"): _squash(1.0 - 0.08 * jolt),
            ("root", "rotation"): qx(-6.0 * low),
            ("leg-left", "rotation"): mul(qx(-48.0 * low), qz(MOUNTAIN_SPLAY)),
            ("leg-right", "rotation"): mul(qx(22.0 * low), qz(-MOUNTAIN_SPLAY - 4.0 * low)),
            ("torso", "rotation"): qx(6.0 - 16.0 * low + 3.0 * jolt),
            ("head", "rotation"): qx(4.0 + 8.0 * low),
        }
        wide = ((0.95, -0.15, 0.05), (0.75, -0.05, 0.45))
        q = []
        for k in range(2):
            a, b = MOUNTAIN_ARMS[k], wide[k]
            q.append(tuple(a[i] + (b[i] - a[i]) * low for i in range(3)))
        _mountain_arms(row, (q[0], q[1]))
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


MOUNTAIN_CLIPS = {"mountain-idle": mountain_idle, "mountain-walk": mountain_walk, "mountain-sprint": mountain_sprint,
                  "mountain-jump": mountain_jump, "mountain-fall": mountain_fall, "mountain-throw": mountain_throw,
                  "mountain-holding-right": mountain_holding_right, "mountain-crouch": mountain_crouch, "mountain-slide": mountain_slide}
