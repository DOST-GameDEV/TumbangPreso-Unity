"""New locomotion clips for the Rafi (displayed: Ilyas) redesign PROTOTYPE.

Imported by tools/author_character_redesign_rafi.py, which writes them into the prototype .glb
IN PLACE OF the clips of the same name copied from team-rafi.glb. Nothing in the game reads
them; team-rafi.glb keeps its own. Rafi's own copy of the idea in Dante's clips script
(docs/CHARACTER_REDESIGN_DANTE.md section 13, rules 8 and 9); none of Dante's numbers are here.

WHAT HIS MOTION SHOULD SAY ABOUT HIM. He is the islander, the water hero: "Draws you into the
wrong current. Leaves with his slipper." His face is the calm one of the cast (a level mouth, a
level eye), he is "a bit muscular", and he wears almost nothing that can flap. So:

  * HE MOVES LIKE A SWELL, NOT LIKE A SPRING. One wave runs UP him: the legs lead, the hips roll
    onto the planted foot, the chest answers a beat later, the arms trail a beat after that.
    Every part uses the same stride with its own LAG. Dante snaps all his parts on one beat;
    Rafi's parts arrive one after another, which is what reads as water.
  * HE RIDES LOW AND LONG. The bounce is small and the stride is wide; the pop is in the
    stretch as the legs pass and in the late flick of the forearm, not in height.
  * THE HEAD DOES NOT CARE. It stays level and nearly still over all of it, chin a little up.
    The big hair and the tied tail then swing against a quiet face.
  * WALK: an unhurried roll, arms loose and low, elbows barely bent, hands flicking late.
  * SPRINT: he cuts through like a swimmer off the wall: far forward, arms swept BACK and out
    like a wake and pumping only a little, the legs doing the work.
  * JUMP: a breach, from standing. Both arms thrown up in a V past the cheeks, the body long,
    both legs TOGETHER and trailing behind like a tail, then the arms ease and he hangs.
  * FALL: sinking through water. Arms up and loose, sculling in turn, legs drifting apart, the
    whole body rocking slowly from side to side, eyes on the ground.

THE SPACE. glTF node space, as the file stores it: +y up, +z the way he faces, +x his LEFT.
Every bone's rest rotation is identity. About +x a positive angle swings a hanging limb BACK;
about +z a negative angle drops his left arm from straight out to hanging, a positive one
lifts it; about +y a positive angle carries his straight-out left arm BACK.

ARMS (rule 8, as amended). Folded forearms turn OUTWARD, never across the torso. A straight arm
cannot rise above straight out, because his head and his hair are far wider than his shoulders:
the side hair hangs to the shoulder at x 0.19 to 0.24 and the ear and its silver drop stand at
0.16 to 0.22. So arms go up THROUGH THE ELBOW, and for him they also go up IN FRONT of the ear:
the upper arm lifts `ARM_LIFT` and swings `ARM_FORE` forward, and the forearm folds up past the
cheek and stretches as it is thrown.

walk, sprint, jump and fall keep the LENGTH of the clip they replace (0.72 s, 0.48, 0.50,
0.33). `idle` is 8 s, not the old 1.333: it is acted stances now (see `idle`). NOT SEEN IN UNITY: the game layers procedural motion on these bones and the scale
channels are new; both need a look there before any of this is believed.
"""
import math

RATE = 60.0
SPLAY = 34.0       # degrees a folded forearm is turned out from straight ahead
ARM_LIFT = 16.0    # how far the upper arm rises above straight out when the arms go up
ARM_FORE = 8.0    # how far it swings forward as it does, so the forearm passes in FRONT of the ear


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


def _soft(s, power):
    """A sine with its peaks held a little: the limb lingers at the end of its swing and
    eases through the middle. Gentler than a snap; a swell has no corners."""
    return math.copysign(abs(s) ** power, s)


def _squash(sy):
    """Volume kept: what the height loses the width gains."""
    k = 1.0 / math.sqrt(sy)
    return (k, sy, k)


def _times(length):
    n = int(round(length * RATE))
    return [length * i / n for i in range(n + 1)]


def _swell(length, leg, roll, sway, twist, chest_lag, arm, arm_lag, hang, back, bend, flick, flick_lag,
           bounce, low, stretch, lean, torso_lean, head_up):
    """One stride as a wave that climbs him. Every part reads the same phase, late by its own lag."""
    out = {}
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        legs = _soft(math.sin(phase), 0.8)
        chest = _soft(math.sin(phase - chest_lag), 0.8)
        arms = _soft(math.sin(phase - arm_lag), 0.85)
        hands = math.sin(phase - flick_lag)
        # he is lowest as the feet land apart and longest as the legs pass: a glide, then a dip
        passing = math.cos(phase) ** 2
        row = {
            ("root", "translation"): (sway * 0.004 * math.sin(phase), low + bounce * passing, 0.0),
            # the hips roll onto the planted foot
            ("root", "rotation"): mul(qx(lean), qz(roll * math.sin(phase))),
            ("root", "scale"): _squash((1.0 - stretch) + 2.0 * stretch * passing),
            ("leg-left", "rotation"): mul(qx(-leg * legs), qz(3.0 - roll * math.sin(phase))),
            ("leg-right", "rotation"): mul(qx(leg * legs), qz(-3.0 - roll * math.sin(phase))),
            # the chest answers late, turning against the hips and tipping back over them
            ("torso", "rotation"): mul(mul(qx(torso_lean), qy(twist * chest)), qz(-1.4 * roll * math.sin(phase - chest_lag))),
            # the head undoes all of it and stays level
            ("head", "rotation"): mul(mul(qx(-head_up), qy(-0.8 * twist * chest)), qz(0.4 * roll * math.sin(phase - chest_lag))),
            ("arm-left", "rotation"): mul(qx(back + arm * arms), qz(-hang)),
            ("arm-right", "rotation"): mul(qx(back - arm * arms), qz(hang)),
            # the forearm turns OUT (`SPLAY`) and flicks last, opening as the arm swings back
            ("forearm-left", "rotation"): mul(qx(-SPLAY), qy(-(bend - flick * hands))),
            ("forearm-right", "rotation"): mul(qx(-SPLAY), qy(bend + flick * hands)),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def walk():
    return _swell(0.72, leg=29.0, roll=5.0, sway=1.0, twist=13.0, chest_lag=0.75, arm=30.0, arm_lag=1.25, hang=68.0,
                  back=4.0, bend=20.0, flick=15.0, flick_lag=2.1, bounce=0.026, low=0.002, stretch=0.045,
                  lean=1.0, torso_lean=-2.0, head_up=1.0)


def sprint():
    return _swell(0.48, leg=46.0, roll=3.5, sway=0.6, twist=17.0, chest_lag=0.6, arm=13.0, arm_lag=1.0, hang=58.0,
                  back=44.0, bend=30.0, flick=12.0, flick_lag=1.7, bounce=0.034, low=0.004, stretch=0.075,
                  lean=13.0, torso_lean=11.0, head_up=22.0)


def jump():
    """A breach: arms thrown up past the cheeks in a V, the body long, the legs together and
    trailing like a tail; then the arms open and he hangs."""
    out = {}
    for t in _times(0.50):
        up = math.exp(-t / 0.17)                 # 1 at take-off, gone by the top
        hang = 1.0 - math.exp(-t / 0.11)
        ring = math.cos(2.0 * math.pi * t / 0.46) * math.exp(-t / 0.13)
        # THE V OPENS OUTWARD. Through v06 the forearm folded 30 to 42 degrees at a stretch of
        # 1.2 to 1.6 and the fists sat against his cheeks under the putong: a kid holding his
        # face, not arms up. His head, ears and side hair reach x 0.24, so the way up is WIDER,
        # not higher: a smaller fold (the forearm leans away from the head) and a long stretch
        # that carries the fist out beside the hair at about ear height.
        # v14: THE HEAD IS 0.84 OF WHAT IT WAS, so there is room beside it. The forearm folds
        # HIGHER (38 to 44 degrees, was 27 to 32) and stretches LESS (1.5 to 1.75, was 1.75 to
        # 2.1): the fists finish higher and closer in, still clear of the ear and its drop.
        lift = ARM_LIFT * (0.75 + 0.25 * up)
        fore = 38.0 + 6.0 * up + 3.0 * ring      # the forearm's fold up, in the lifted arm's plane
        reach = 1.50 + 0.25 * up                 # and its stretch as it is thrown
        fwd = ARM_FORE * (0.55 + 0.45 * up)
        # A STANDING JUMP (owner, 2026-10-05: "i need a jump for standing still"): both feet
        # together and symmetric, never a stride. For him they trail back as one, like a tail.
        row = {
            ("root", "scale"): _squash(1.0 + 0.17 * ring),
            ("root", "rotation"): qx(6.0 * hang),
            ("leg-left", "rotation"): mul(qx(21.0 * hang), qz(2.0)),
            ("leg-right", "rotation"): mul(qx(21.0 * hang), qz(-2.0)),
            ("torso", "rotation"): qx(-12.0 * up - 5.0 * hang),
            ("head", "rotation"): qx(-10.0 * up - 4.0 * hang),
            ("arm-left", "rotation"): mul(qy(-fwd), qz(lift)),
            ("arm-right", "rotation"): mul(qy(fwd), qz(-lift)),
            ("forearm-left", "rotation"): qz(fore),
            ("forearm-right", "rotation"): qz(-fore),
            ("forearm-left", "scale"): (reach, 1.0, 1.0),
            ("forearm-right", "scale"): (reach, 1.0, 1.0),
        }
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


def fall():
    """A loop: sinking. Arms up and loose, sculling in turn; legs drifting; the body rocking."""
    out = {}
    length = 0.33
    for t in _times(length):
        phase = 2.0 * math.pi * t / length
        s, c = math.sin(phase), math.cos(phase)
        row = {
            ("root", "scale"): _squash(1.05 + 0.015 * math.sin(2.0 * phase)),
            ("root", "rotation"): qz(3.0 * s),
            ("leg-left", "rotation"): mul(qx(8.0 + 9.0 * c), qz(7.0 - 3.0 * s)),
            ("leg-right", "rotation"): mul(qx(8.0 - 9.0 * c), qz(-7.0 - 3.0 * s)),
            ("torso", "rotation"): mul(qx(5.0), qz(-2.0 * s)),
            ("head", "rotation"): mul(qx(8.0), qz(-1.0 * s)),
            ("arm-left", "rotation"): mul(qy(-ARM_FORE + 4.0 * c), qz(0.8 * ARM_LIFT + 3.0 * s)),
            ("arm-right", "rotation"): mul(qy(ARM_FORE + 4.0 * c), qz(-0.8 * ARM_LIFT + 3.0 * s)),
            ("forearm-left", "rotation"): qz(36.0 + 8.0 * s),
            ("forearm-right", "rotation"): qz(-(36.0 - 8.0 * s)),
            ("forearm-left", "scale"): (1.5, 1.0, 1.0),
            ("forearm-right", "scale"): (1.5, 1.0, 1.0),
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


def _window(t, start, end, blend):
    """0 outside [start, end], 1 between, eased in and out over `blend` seconds."""
    return _ease((t - start) / blend) * (1.0 - _ease((t - (end - blend)) / blend)) if start <= t <= end else 0.0


#   where his LEFT arm points in each stance: (upper arm, forearm). +x his left, +y up, +z ahead.
#   the stand: the original idle's 45 degree hang, the elbow barely soft
ARMS_HANG = ((0.70, -0.71, 0.00), (0.62, -0.74, 0.26))
#   hands behind the back: elbows out and well BACK, forearms in toward the spine and down, so
#   the fists sit over the top of the back flap, under the tip of the tied tail (0.208)
ARMS_BEHIND = ((0.40, -0.55, -0.73), (-0.50, -0.45, -0.74))
BEHIND_REACH = 0.70
#   the swimmer's shake-out: arms loose and low at his sides, the forearms flicked (see `idle`)
ARMS_LOOSE = ((0.80, -0.60, 0.05), (0.75, -0.60, 0.25))
#   his RIGHT hand on the shark tooth: elbow forward and in, forearm across to the sternum,
#   stretched so the fist sits in front of the pendant instead of inside the pectoral
ARMS_TOOTH = ((0.25, -0.45, 0.86), (-0.55, 0.09, 0.83))
TOOTH_REACH = 0.62
#   his LEFT arm in that stance: hanging, a little behind him. (v05 put that fist on the belt,
#   forearm forward; with the right fist at the chest it read as a boxer's guard, two fists up.
#   One hand up and one hanging reads as a hand on a pendant.)
ARMS_REST = ((0.62, -0.76, -0.18), (0.58, -0.80, 0.14))
IDLE_LENGTH = 8.0


def idle():
    """Eight seconds of THIS kid waiting. Acted, not wobbled (rule 9, as rewritten).

    The owner turned down a layered-sine idle on Dante: "looks like he's just distorting
    around.. needs more character, like sometimes he puts his hands on the waist, or crosses
    his arms, other things idle people do". So this is stances, held long enough to read, with
    the breathing kept small underneath. Arms are posed by where they point (`_arm`).

    WHAT RAFI DOES WHILE HE WAITS. Not Dante's fists on hips and folded arms: Dante is a brawler
    who is impatient. Rafi is the calm one, the islander who watches the water.
      1.0 to 3.6 s  HANDS BEHIND HIS BACK. Chest out, chin up, rocking slowly on his heels,
                    looking along the horizon one way and then the other. Nothing to prove.
      3.75 to 4.75  THE SHAKE-OUT. A swimmer before a dive: arms dropped loose, both forearms
                    flicked fast, a little bounce on his toes. The one quick beat in the loop.
      5.0 to 7.5    THE SHARK TOOTH. Weight on his left leg, left arm hanging, right hand up
                    to the tooth on his chest; he looks down at it, then up and away.
    Frame 0 is the neutral stand (arms hanging at the original's 45 degrees).

    IT IS 8 s, NOT THE OLD 1.33 s. Anything in the game that assumes the idle's length needs a
    look, and the game may prefer these as separate clips it picks between.
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 5.0))      # five slow breaths a loop
        behind = _window(t, 1.0, 3.6, 0.45)
        shake = _window(t, 3.75, 4.75, 0.25)
        tooth = _window(t, 5.0, 7.5, 0.45)
        flick = math.sin(2.0 * math.pi * 6.0 * t)
        loose = (ARMS_LOOSE[0], (0.75, -0.60 + 0.26 * flick, 0.25 + 0.22 * math.cos(2.0 * math.pi * 6.0 * t)))
        row = {}
        for side, name in ((1, "left"), (-1, "right")):
            hang = _arm(side, *ARMS_HANG)
            back = _arm(side, *ARMS_BEHIND)
            shaken = _arm(side, *loose)
            held = _arm(side, *(ARMS_REST if side > 0 else ARMS_TOOTH))
            for k, bone in enumerate(("arm-" + name, "forearm-" + name)):
                row[(bone, "rotation")] = _mix(_mix(_mix(hang[k], back[k], behind), shaken[k], shake), held[k], tooth)
            reach = BEHIND_REACH * behind + (TOOTH_REACH * tooth if side < 0 else 0.0)
            row[("forearm-" + name, "scale")] = (1.0 + reach, 1.0, 1.0)
        # where he looks: along the horizon with his hands behind him; down at the tooth, then away
        look = (24.0 * (_window(t, 1.5, 2.4, 0.35) - _window(t, 2.5, 3.4, 0.35))
                + 9.0 * _window(t, 5.3, 6.3, 0.3) - 24.0 * _window(t, 6.4, 7.3, 0.3))
        nod = -6.0 * behind + 8.0 * _window(t, 5.3, 6.3, 0.3) - 3.0 * _window(t, 6.4, 7.3, 0.3)
        rock = 2.4 * behind * math.sin(2.0 * math.pi * (t - 1.0) / 1.3)   # heels to toes
        shift = 3.2 * tooth                                                # onto his left leg
        hop = shake * abs(math.sin(2.0 * math.pi * 3.0 * t))
        row.update({
            ("root", "translation"): (0.0, 0.012 * hop, 0.0),
            ("root", "scale"): _squash(1.0 + 0.014 * breath + 0.03 * hop),
            ("root", "rotation"): mul(qx(rock), qz(shift)),
            ("torso", "rotation"): mul(qx(-4.0 * behind + 3.0 * tooth + 1.0 * breath), qz(-0.9 * shift)),
            ("head", "rotation"): mul(mul(qy(look), qx(nod - 0.8 * breath)), qz(-0.5 * shift + 3.0 * shake * flick)),
            ("leg-left", "rotation"): mul(qx(-rock), qz(1.0 - shift)),
            ("leg-right", "rotation"): mul(qx(-rock), qz(-1.0 - shift)),
        })
        for key, value in row.items():
            out.setdefault(key, []).append((t, value))
    return out


CLIPS = {"idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall}
