"""New locomotion clips for the Paete redesign PROTOTYPE.

Imported by tools/author_character_redesign_paete.py, which writes them into the prototype .glb
IN PLACE OF the clips of the same name copied from team-paete.glb. Nothing in the game reads
them; team-paete.glb and everything under characters/paete-motion keep their own. His own copy
of the idea in Dante's clips script (docs/CHARACTER_REDESIGN_DANTE.md section 13, rules 8 and
9); none of another hero's numbers are here.

WHAT HIS MOTION SHOULD SAY ABOUT HIM. He is a tree that walks, the guardian of a mountain:
"Never hurries. Always arrives." He stood so still the students used him as the lata twice.
  * HE IS HEAVY AND HE PLANTS. Each step is set down, the whole trunk settles onto it (a squash
    a beat AFTER the foot lands, not with it), and only then does the other root leave the ground.
    Small lift, wide stance, no spring.
  * THE TRUNK SWAYS, THE CROWN FOLLOWS LATE. His torso leans over the planted foot like a tree in
    wind; the head (and its antlers and leaves) arrives a beat after, so the crown always trails.
  * HIS ARMS ARE VINES. They hang long (the points reach his knees) and swing as pendulums, late,
    and the braid from the elbow down is later again: the forearm is still going back when the
    upper arm has started forward.
  * WALK: an unhurried tread. SPRINT: a lumbering charge, trunk pitched forward, both arms dragged
    back and low with the braids streaming behind, long strides, every landing a thud.
  * JUMP: a standing heave. He sinks, then both arms are thrown up in a V through the elbows, the
    braids stretching as they go; the root feet hang together under him.
  * FALL: a tree coming down upright. Arms up and wide, the braids trailing above him and
    fluttering in turn, legs a little apart, a slow rock.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity and his arms rest STRAIGHT OUT. About +x a positive angle
swings a hanging limb BACK and nods the head DOWN; about +z a negative angle drops his left arm
from straight out toward hanging, a positive one lifts it; about +y a positive angle carries his
straight-out left arm BACK and turns the head to his LEFT.

ARMS. His head is NARROW (0.18 wide after the 0.84 scale, against shoulders 0.57 wide), so
unlike the cast his arms have room beside it. What stops a straight arm rising is his own
shoulder: the pauldron's top planks and its leaf cluster ride the arm bone and swing in toward
the head, touching the antler's foot at about 35 degrees of lift. So arms still go up THROUGH THE
ELBOW: the upper arm lifts `ARM_LIFT` and the braid folds up from there into a V.

walk, sprint, jump and fall keep the LENGTH of the clip they replace (0.72 s, 0.48, 0.50, 0.33).
`idle` is 8 s, not the old 1.333: acted stances (see `idle`). NOT SEEN IN UNITY: the game layers
procedural motion on these bones, his hero clips in characters/paete-motion key `arm-left` and
`arm-right` and know nothing of the elbow, and the scale channels are new.
"""
import math

RATE = 60.0
SPLAY = 12.0       # degrees a folded braid is turned out from straight ahead
ARM_LIFT = 27.0    # how far the upper arm rises above straight out when the arms go up
HANG = 46.0        # how far below straight out the upper arm hangs in a stand (the original's 45)


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


def _held(s, power):
    """A sine with its peaks held: a heavy limb lingers at the end of its swing."""
    return math.copysign(abs(s) ** power, s)


def _squash(sy):
    """Volume kept: what the height loses the width gains."""
    k = 1.0 / math.sqrt(sy)
    return (k, sy, k)


def _times(length):
    n = int(round(length * RATE))
    return [length * i / n for i in range(n + 1)]


def _tread(length, leg, lift, settle, settle_lag, sway, crown_lag, arm, arm_lag, back, bend, trail, braid_lag,
           lean, torso_lean, head_up, stance):
    """One stride (two steps) of a heavy walker. Each part reads the same phase, late by its own lag."""
    out = {}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s = math.sin(phase)
        legs = _held(s, 0.7)
        apart = abs(s)                                   # 1 as a foot lands, 0 as the legs pass
        landed = abs(math.sin(phase - settle_lag))       # the same, a beat late: the weight arriving
        arms = _held(math.sin(phase - arm_lag), 0.8)
        braid = math.sin(phase - braid_lag)
        over = math.sin(phase - 0.5)                     # which foot the trunk is over
        crown = math.sin(phase - 0.5 - crown_lag)
        row = {
            # he rises only a little as the legs pass and comes DOWN onto each foot
            ("root", "translation"): (0.006 * sway * over, lift * (1.0 - apart ** 1.6) - 0.004, 0.0),
            ("root", "rotation"): mul(qx(lean), qz(0.5 * sway * over)),
            # the settle: squashed a beat after the landing, tall as the legs pass
            ("root", "scale"): _squash(1.0 + settle * (0.6 - 1.6 * landed ** 2.0)),
            ("leg-left", "rotation"): mul(qx(-leg * legs), qz(stance - 0.5 * sway * over)),
            ("leg-right", "rotation"): mul(qx(leg * legs), qz(-stance - 0.5 * sway * over)),
            # the trunk leans over the planted foot and turns a little against the legs
            ("torso", "rotation"): mul(mul(qx(torso_lean), qy(0.35 * leg * legs * 0.4)), qz(-sway * over)),
            # the crown follows late, and levels itself
            ("head", "rotation"): mul(mul(qx(-head_up + 2.0 * landed), qy(-0.2 * leg * legs * 0.4)), qz(0.9 * sway * crown)),
            ("arm-left", "rotation"): mul(qx(back + arm * arms), qz(-HANG)),
            ("arm-right", "rotation"): mul(qx(back - arm * arms), qz(HANG)),
            # the braid: bent a little forward, trailing the upper arm by its own lag
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(bend - trail * braid))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(bend + trail * braid)),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def walk():
    return _tread(0.72, leg=24.0, lift=0.016, settle=0.045, settle_lag=0.55, sway=4.5, crown_lag=0.7, arm=13.0, arm_lag=0.9,
                  back=2.0, bend=10.0, trail=13.0, braid_lag=1.9, lean=2.0, torso_lean=2.0, head_up=1.0, stance=3.0)


def sprint():
    return _tread(0.48, leg=40.0, lift=0.030, settle=0.075, settle_lag=0.5, sway=3.0, crown_lag=0.6, arm=9.0, arm_lag=0.8,
                  back=38.0, bend=-16.0, trail=11.0, braid_lag=1.6, lean=13.0, torso_lean=9.0, head_up=17.0, stance=4.0)


def jump():
    """A standing heave: both arms thrown up in a V through the elbows, the braids stretching as
    they go, both root feet hanging together under him."""
    out = {}
    for t in _times(0.50):
        up = math.exp(-t / 0.16)                 # 1 at take-off, gone by the top
        hang = 1.0 - math.exp(-t / 0.12)
        ring = math.cos(2.0 * math.pi * t / 0.50) * math.exp(-t / 0.15)
        lift = ARM_LIFT * (0.80 + 0.20 * up)
        fold = 44.0 + 8.0 * up + 4.0 * ring      # the braid's fold up from the lifted upper arm
        reach = 1.0 + 0.20 * up                  # and its stretch as it is thrown
        row = {
            ("root", "scale"): _squash(1.0 + 0.15 * ring),
            ("root", "rotation"): qx(3.0 * hang),
            # A STANDING JUMP: both feet together and symmetric, never a stride. His hang straight
            # down and a little apart, like roots pulled out of the ground.
            ("leg-left", "rotation"): mul(qx(9.0 * hang), qz(5.0 * hang)),
            ("leg-right", "rotation"): mul(qx(9.0 * hang), qz(-5.0 * hang)),
            ("torso", "rotation"): qx(-8.0 * up - 3.0 * hang),
            ("head", "rotation"): qx(-12.0 * up - 4.0 * hang),
            ("arm-left", "rotation"): mul(qy(-6.0), qz(lift)),
            ("arm-right", "rotation"): mul(qy(6.0), qz(-lift)),
            ("forearm-left", "rotation"): qz(fold),
            ("forearm-right", "rotation"): qz(-fold),
            ("forearm-left", "scale"): (reach, 1.0, 1.0),
            ("forearm-right", "scale"): (reach, 1.0, 1.0),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def fall():
    """A loop: a tree coming down upright. Arms up and wide, the braids fluttering in turn."""
    out = {}
    length = 0.33
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s, c = math.sin(phase), math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.04 + 0.012 * math.sin(2.0 * phase)),
            ("root", "rotation"): qz(2.0 * s),
            ("leg-left", "rotation"): mul(qx(6.0 + 4.0 * c), qz(8.0 - 2.0 * s)),
            ("leg-right", "rotation"): mul(qx(6.0 - 4.0 * c), qz(-8.0 - 2.0 * s)),
            ("torso", "rotation"): mul(qx(4.0), qz(-1.5 * s)),
            ("head", "rotation"): mul(qx(9.0), qz(-1.0 * s)),
            ("arm-left", "rotation"): mul(qy(-4.0 + 3.0 * c), qz(0.75 * ARM_LIFT + 2.0 * s)),
            ("arm-right", "rotation"): mul(qy(4.0 + 3.0 * c), qz(-0.75 * ARM_LIFT + 2.0 * s)),
            ("forearm-left", "rotation"): mul(qz(50.0 + 7.0 * s), qy(6.0 * c)),
            ("forearm-right", "rotation"): mul(qz(-(50.0 - 7.0 * s)), qy(6.0 * c)),
            ("forearm-left", "scale"): (1.12, 1.0, 1.0),
            ("forearm-right", "scale"): (1.12, 1.0, 1.0),
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
    """An arm posed by where it POINTS: the upper arm along `upper`, the braid along `fore`.

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


#   where his LEFT arm points in each stance: (upper arm, braid). +x his left, +y up, +z ahead.
#   the stand: the original idle's hang, the braid a little forward of the upper arm
ARMS_HANG = ((0.70, -0.71, 0.00), (0.56, -0.80, 0.20))
#   THE SEED. His left braid brought up in front of him, its point at chin height, where he can
#   look at it: the elbow forward and out, the braid up and a little in
ARMS_SEED = ((0.45, -0.55, 0.70), (-0.25, 0.55, 0.80))
#   ROOTING. Both arms let go, wider and lower than the stand, the points turned to the ground
ARMS_ROOT = ((0.80, -0.60, 0.02), (0.60, -0.80, 0.06))
#   THE REACH. His right braid sent out ahead of him and to his side at shoulder height (typed for
#   the left, mirrored). v02 sent it straight ahead and from the front or behind, where the game's
#   camera sits, it was an arm seen end on; 40 degrees out to the side it reads from both.
ARMS_REACH = ((0.76, 0.04, 0.65), (0.62, 0.10, 0.78))
REACH_STRETCH = 0.38
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of THIS character waiting. Acted, not wobbled.

    WHAT PAETE DOES WHILE HE WAITS. He is the one who "stood so still watching that they used him
    as the lata twice", and whose kit is growth: a seed put in his open hand, roots called out of
    the ground, a vine sent across the court from his forearm. So he stands like a tree, and three
    times in the loop the tree does something a tree should not:
      0.0 to 1.0 s  THE TREE. Still. Only the breath and a slow lean of the trunk.
      1.0 to 3.3    THE SEED. He brings his left braid up in front of him and turns it over,
                    slowly, one way and back, looking down at its point (his ultimate: "he opens
                    his hand, and she puts a seed in it"). Calm, curious, unhurried.
      3.7 to 4.9    ROOTING. He lets both arms go, sinks his weight and spreads his root feet (his
                    ground call), then one short shudder runs up him and shakes the crown, leaves
                    and antlers, the way a tree shakes off rain. The one quick beat in the loop.
      5.2 to 7.6    THE REACH. His right braid goes out ahead of him at shoulder height and GROWS
                    (the forearm stretches by a third, Liana Leap rehearsed), hangs there while
                    he looks along it, and draws back in.
    Frame 0 and the last frame are the stand.

    IT IS 8 s, NOT THE OLD 1.33 s. Anything in the game that assumes the idle's length needs a
    look, and the game may prefer these as separate clips it picks between.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / 2.0)                       # four slow breaths a loop
        lean = math.sin(2.0 * math.pi * t / IDLE_LENGTH)                 # one slow lean of the trunk
        seed = _window(t, 1.0, 3.3, 0.55)
        root = _window(t, 3.7, 4.9, 0.30)
        reach = _window(t, 5.2, 7.6, 0.60)
        grown = _window(t, 5.6, 7.3, 0.50)
        turn = seed * math.sin(2.0 * math.pi * (t - 1.3) / 1.7)          # the braid turned over and back
        shudder = _window(t, 4.25, 4.75, 0.12) * math.sin(2.0 * math.pi * 7.0 * t)
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            hang = _arm(side, *ARMS_HANG)
            rooted = _arm(side, *ARMS_ROOT)
            other = _arm(side, *(ARMS_SEED if side > 0 else ARMS_REACH))
            amount = seed if side > 0 else reach
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                row[(bone, "rotation")] = _mix(_mix(hang[k], rooted[k], root), other[k], amount)
            if side > 0:
                # turning the braid over: about its own length (the forearm bone's x)
                row[("forearm-left", "rotation")] = mul(row[("forearm-left", "rotation")], qx(34.0 * turn))
            row[("forearm-" + name, "scale")] = (1.0 + (REACH_STRETCH * grown if side < 0 else 0.0), 1.0, 1.0)
        # where he looks: down and to his left at the seed; along his right braid as it grows
        look = 13.0 * seed - 30.0 * reach + 6.0 * shudder
        nod = 12.0 * seed + 3.0 * root - 2.0 * reach
        sink = root
        row.update({
            ("root", "translation"): (0.0, -0.003 * sink, 0.0),
            ("root", "scale"): _squash(1.0 + 0.010 * breath - 0.060 * sink + 0.012 * shudder),
            ("root", "rotation"): qz(1.2 * lean),
            ("torso", "rotation"): mul(mul(qx(0.8 * breath + 3.0 * seed + 2.0 * reach), qy(5.0 * seed - 9.0 * reach + 2.0 * shudder)),
                                       qz(-0.8 * lean)),
            ("head", "rotation"): mul(mul(qy(look), qx(nod - 0.6 * breath)), qz(-0.9 * lean + 4.0 * shudder)),
            ("leg-left", "rotation"): qz(2.0 + 6.0 * sink - 1.2 * lean),
            ("leg-right", "rotation"): qz(-2.0 - 6.0 * sink - 1.2 * lean),
        })
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall}
