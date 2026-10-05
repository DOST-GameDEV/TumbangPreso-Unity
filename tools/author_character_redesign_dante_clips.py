"""New locomotion clips for the Dante (displayed: Basilio) redesign PROTOTYPE.

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
