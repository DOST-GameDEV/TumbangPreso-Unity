"""The clips of the Payo audience v2 villager `elder_man`, the village elder (lakay).

Imported by tools/author_payo_audience_v2_elder_man.py, which writes them into his .glb in place
of the clips of the same name copied from character-male-e.glb, and APPENDS the two new ones
(`cheer`, `clap`). docs/reports/character-redesign/payo-audience-brief.md, "Clips".

WHAT IS HIS AND WHAT IS COPIED.
  * HIS OWN, written for him below the line "THE ELDER'S CLIPS": `idle` (8 s, acted: a spectator
    leaning on his spear), `cheer`, `clap` (which is also `emote-yes`, as the brief asks), `sit`
    (seated on a ledge, a loop, the seat on the root's origin), and his `walk`, `sprint`, `jump`
    and `fall`.
  * COPIED from the base Classic (tools/author_character_redesign_mang_kanor_clips.py, which is
    not edited or imported): the math helpers, the pose machinery (`_pose`, `_act`, `_frames`)
    and Mang Kanor's twelve other action clips (the carry, the throw, the pick-up, the tag, the
    shove, the two reaches, the slide, crouch, die, emote-no), running from HIS stand instead of
    Mang Kanor's. The brief: "Copy every clip the base Classic has". They are a crowd figure's
    fallbacks; nothing on the Payo wall plays them.

THE SPEAR. A separate mesh skinned rigidly to `forearm-right`, running through the middle of the
right fist SQUARE TO THE FOREARM, as a fist holds a staff. So the forearm's turn about its own
axis decides where the spear points. Every row this file writes goes through `_row`, which picks
that turn so the shaft points as near as it can to the pose's `spear` direction, and, when the
pose has a `plant` height, swings the whole right arm at the shoulder (Newton's method, a few
steps) until the BUTT is at that height: on the ground for `plant` 0. That is how he leans on it
and how the butt thumps the ground in the clap. GRIP_DROP is the butt's distance below the fist.

THE SPACE. glTF node space: +y up, +z the way he faces, +x his LEFT. Every bone's rest rotation is
identity. The pose numbers are Mang Kanor's (see the copied block's own notes): lean + folds the
chest forward, nod + is chin down, look + is to his left; arms are (upper arm, forearm) directions
written for a LEFT arm, +x out.

NOT SEEN IN UNITY.
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

    Both are directions for his LEFT arm in the file's space (+x her left, +y up, +z ahead);
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


def _rotx(v, deg):
    """A direction turned about +x: positive swings a hanging limb back."""
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return (v[0], v[1] * c - v[2] * s, v[1] * s + v[2] * c)


def _lerp(a, b, t):
    return tuple(p + (q - p) * t for p, q in zip(a, b))



# ---------------------------------------------------------------------------
# HIS STAND (the copied action clips leave from it and come back to it)
# ---------------------------------------------------------------------------
GRIP_DROP = 0.225        # the spear's butt is this far below the middle of his right fist
FIST_X = 0.088           # the fist's middle, out along the forearm from the elbow
STOOP = 12.0             # degrees his chest is carried forward: a little stooped, as Lola Pacing's is
#   his free LEFT arm hangs a little forward of his side, under the edge of the blanket
ARMS_HANG = ((0.66, -0.74, 0.12), (0.42, -0.80, 0.43))
ARMS_STOOD = ARMS_HANG
#   his RIGHT arm on the spear: the elbow down and a little out, the forearm level and ahead.
#   `_row` swings it at the shoulder until the butt is on the ground.
ARM_SPEAR = ((0.80, -0.46, 0.40), (0.25, 0.03, 0.97))
#   the shaft leans a touch out to his right and ahead of him: he leans on it
SPEAR_LEAN = (-0.10, 1.0, 0.12)
SPEAR_UP = (0.0, 1.0, 0.0)

STAND = {
    "tx": 0.0, "ty": 0.0, "tz": 0.0, "pitch": 0.0, "yaw": 0.0, "roll": 1.5, "sq": 1.0,
    "lean": STOOP, "twist": 0.0, "hunch": 2.0, "nod": 4.0, "look": 0.0, "tilt": 0.0,
    "legL": (0.0, 3.0), "legR": (0.0, 3.0),
    "armL": ARMS_STOOD, "armR": ARM_SPEAR, "reachL": 0.0, "reachR": 0.0,
    "spear": SPEAR_UP, "plant": -1.0,
}



# ---------------------------------------------------------------------------
# COPIED FROM tools/author_character_redesign_mang_kanor_clips.py (the base Classic): the pose
# machinery and his action clips, running from the elder's STAND above. `_row` there is
# renamed `_row_base`; the elder's `_row` below wraps it with the spear.
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


def _row_base(p):
    torso = mul(mul(qx(p["lean"]), qy(p["twist"])), qz(p["hunch"]))
    head = mul(_inv(torso), mul(mul(qy(p["look"]), qx(p["nod"])), qz(p["tilt"])))
    row = {
        ("root", "translation"): (p["tx"], p["ty"], p["tz"]),
        ("root", "rotation"): mul(mul(qy(p["yaw"]), qx(p["pitch"])), qz(p["roll"])),
        ("root", "scale"): _squash(p["sq"]),
        ("torso", "rotation"): torso,
        ("head", "rotation"): head,
        ("leg-left", "rotation"): mul(qx(-p["legL"][0]), qz(p["legL"][1])),
        ("leg-right", "rotation"): mul(qx(-p["legR"][0]), qz(-p["legR"][1])),
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


#   THE DANGLE: how he carries the tsinelas. Low, out wide of his right hip, hooked on his
#   fingers the way a man carries his own slippers across a wet street.
ARM_DANGLE = ((0.80, -0.60, 0.02), (0.66, -0.66, 0.36))
#   his left arm while he carries: tucked behind him, the fist at the small of his back
ARM_BEHIND = ((0.74, -0.56, -0.38), (-0.10, -0.50, -0.86))
#   a fist on his hip with the elbow stuck out
ARM_HIP = ((0.90, -0.40, -0.16), (-0.30, -0.85, 0.42))
#   a fist chambered at his ribs: the elbow back, the forearm level and ahead
ARM_RIBS = ((0.76, -0.50, -0.42), (0.26, 0.16, 0.95))

#   HOLDING. Belly out, leaning back on his heels, feet apart.
HOLD = _pose(sq=0.99, lean=-7.0, nod=0.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armR=ARM_DANGLE, armL=ARM_BEHIND)
HOLD_LENGTH = 2.2


def holding_right():
    """A 2.2 s loop. He waits for his turn the way he waits for a fare: belly out, rocked back on
    his heels, one hand behind him. The slipper hangs low at his right hip and SWINGS, out ahead
    and back like something hung from a handlebar, a long swing and a shorter one; at the front
    of each he looks down at it past his belly. His weight rolls from one foot to the other and
    he breathes once. Frame 0 is the carry the throw leaves from."""
    def pose_at(t):
        turn = 2.0 * math.pi * t / HOLD_LENGTH
        breath = math.sin(turn)
        sway = math.sin(turn - 0.9) + math.sin(0.9)                 # nothing at the loop's join
        swing = _bump(t, 0.55, 0.16) - 0.55 * _bump(t, 0.95, 0.15) + 0.65 * _bump(t, 1.35, 0.15) \
            - 0.3 * _bump(t, 1.70, 0.13)
        glance = _window(t, 0.30, 1.55, 0.25)
        arm = (_rotx(ARM_DANGLE[0], -14.0 * swing), _rotx(ARM_DANGLE[1], -62.0 * swing))
        return _pose(HOLD, sq=0.99 + 0.012 * breath, tx=-0.007 * sway, roll=1.6 * sway, hunch=-1.5 * sway,
                     lean=-7.0 - 1.2 * breath, twist=-5.0 * glance,
                     nod=9.0 * glance + 3.0 * abs(swing) * glance, look=-22.0 * glance, tilt=-5.0 * glance,
                     legL=(0.0, 6.0 + 1.6 * sway), legR=(0.0, 6.0 - 1.6 * sway),
                     armR=arm, reachR=0.15 + 0.25 * abs(swing),
                     armL=_blend(ARM_BEHIND, ((0.76, -0.52, -0.40), (-0.16, -0.42, -0.89)), 0.5 - 0.5 * math.cos(2.0 * turn)))
    return _frames(HOLD_LENGTH, pose_at)


def holding_right_shoot():
    """THE THROW, from the instant the slipper leaves (the game winds the arm up itself, plays
    this at the release, and poses his chest, head and upper arms over it). He LOBS it, belly
    first: at the release (0.06 s) he is up off the ground leaning BACK, his left knee kicked up
    ahead of him and the arm following the slipper up high; then he comes down on that foot and
    cannot stop, pitching forward over it with a squash at 0.11 s, the off arm thrown up behind;
    and rocks back onto his heels into the carry. The forearm opens from the carry's bend to
    straight and stretched at 0.06 s."""
    lobbed = _pose(HOLD, sq=1.09, pitch=-9.0, ty=0.022, tz=0.018, twist=14.0, lean=-5.0, nod=-8.0,
                   legL=(40.0, 6.0), legR=(-8.0, 6.0),
                   armR=((0.34, 0.34, 0.88), (0.20, 0.42, 0.885)), reachR=0.75,
                   armL=((0.66, -0.46, -0.60), (0.50, -0.52, -0.70)))
    lurch = _pose(HOLD, sq=0.87, pitch=13.0, ty=-0.008, tz=0.042, twist=18.0, lean=9.0, nod=8.0,
                  legL=(20.0, 9.0), legR=(-24.0, 8.0),
                  armR=((0.34, -0.30, 0.89), (0.24, -0.50, 0.83)), reachR=0.10,
                  armL=((0.80, 0.22, -0.56), (0.66, 0.52, -0.54)), reachL=0.15)
    rock = _pose(HOLD, sq=1.03, pitch=-5.0, tz=0.010, legL=(6.0, 6.0), legR=(-4.0, 6.0))
    return _act(0.20, [(0.0, HOLD), (0.06, lobbed, "out"), (0.11, lurch, "lin"), (0.165, rock), (0.20, HOLD)])


def pick_up():
    """His belly will not let him fold over for it. So he goes down SIDEWAYS: feet apart, he
    tips to his right like a tricycle up on two wheels, his right arm dropping straight down to
    the slipper beside his shoe (lowest at 0.167 s) and his left arm thrown up the other side to
    keep him from going over, his eyes on it. He comes up with an overshoot, belly out."""
    down = _pose(sq=0.84, ty=-0.012, tx=-0.012, roll=13.0, hunch=22.0, lean=14.0, twist=-12.0, nod=18.0, look=-28.0,
                 tilt=6.0, legL=(0.0, 4.0), legR=(0.0, 30.0),
                 armR=((0.42, -0.86, 0.28), (0.26, -0.93, 0.26)), reachR=0.60,
                 armL=((0.84, 0.50, -0.10), (0.62, 0.78, -0.08)), reachL=0.30)
    up = _pose(sq=1.07, lean=-11.0, roll=-3.0, nod=-3.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
               armR=((0.74, -0.50, 0.44), (0.34, 0.20, 0.92)), armL=((0.86, -0.46, 0.0), (0.60, -0.70, 0.38)))
    return _act(1.0 / 3.0, [(0.0, STAND), (1.0 / 6.0, down, "in"), (0.275, up), (1.0 / 3.0, STAND)])


def attack_melee_right():
    """THE TAG, and the brakes. A short sink with the elbow drawn back (0.06 s); then he goes
    after you with the whole of him, the arm out straight and level by 0.13 s and still
    reaching, further than his feet allow, to its far point at 0.254 s: pitched forward past
    his toes, the left arm windmilling up behind him. He totters there, then rocks back onto
    his heels with a squash. The game poses his chest, head and upper arms over this while the
    tag is live, so the legs are mild and the right forearm is straight from 0.13 s; what is
    his own all the way through is the sink, the overbalance and the rock back."""
    wind = _pose(sq=0.91, lean=-9.0, twist=-16.0, nod=6.0, legL=(0.0, 5.0), legR=(0.0, 5.0),
                 armR=((0.66, -0.50, -0.56), (0.48, -0.36, 0.80)), armL=((0.66, -0.56, 0.50), (0.40, -0.30, 0.86)))
    out = _pose(sq=1.05, pitch=6.0, lean=7.0, twist=18.0, nod=0.0, tz=0.008, legL=(6.0, 4.0), legR=(-4.0, 4.0),
                armR=((0.22, 0.04, 0.97), (0.12, 0.04, 0.99)), reachR=0.20,
                armL=((0.78, 0.20, -0.60), (0.62, 0.52, -0.58)))
    far = _pose(out, sq=1.09, pitch=17.0, lean=11.0, twist=26.0, tz=0.026, nod=-6.0, legL=(9.0, 4.0), legR=(-8.0, 4.0),
                armR=((0.15, 0.04, 0.99), (0.08, 0.04, 1.0)), reachR=0.62,
                armL=((0.70, 0.46, -0.55), (0.46, 0.80, -0.40)), reachL=0.20)
    totter = _pose(far, sq=1.01, pitch=20.0, twist=22.0, reachR=0.36,
                   armL=((0.88, 0.10, -0.46), (0.84, 0.30, -0.45)), reachL=0.28)
    heels = _pose(sq=0.92, pitch=-6.0, lean=-9.0, nod=6.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
                  armR=((0.74, -0.50, 0.45), (0.46, -0.40, 0.79)), armL=((0.74, -0.50, 0.45), (0.46, -0.40, 0.79)))
    return _act(0.4167, [(0.0, STAND), (0.06, wind, "out"), (0.13, out, "in"), (0.254, far, "out"),
                         (0.31, totter), (0.375, heels), (0.4167, STAND)])


def attack_melee_left():
    """THE SHOVE, as a tricycle takes a gap: side on and not slowing. He draws back onto his
    right foot, turning a little away, the left elbow tucked and the forearm stood up in front
    of it; then swings his left shoulder round and BARGES, forearm first as a bar with his belly behind it, his head
    still turned to look where he is going and the right arm trailing out behind (far point
    0.254 s). He bounces off it and comes back round."""
    bar = ((0.92, -0.36, 0.14), (0.72, 0.64, 0.26))
    wind = _pose(sq=0.88, yaw=12.0, lean=-12.0, tz=-0.026, nod=6.0, look=-10.0, legL=(0.0, 7.0), legR=(0.0, 7.0),
                 armL=((0.78, -0.56, 0.10), (0.20, 0.80, 0.56)))
    coiled = _pose(wind, sq=0.85, yaw=17.0, tz=-0.032, look=-14.0)
    barge = _pose(sq=1.07, yaw=-50.0, tz=0.055, lean=-5.0, hunch=-19.0, nod=2.0, look=46.0,
                  legL=(0.0, 20.0), legR=(0.0, 6.0), armL=bar, reachL=0.45,
                  armR=((0.86, -0.32, -0.30), (0.92, -0.14, -0.30)), reachR=0.22)
    held = _pose(barge, sq=0.97, tz=0.042, hunch=-13.0, yaw=-45.0, reachL=0.28, reachR=0.10)
    return _act(0.4167, [(0.0, STAND), (0.10, wind, "out"), (0.16, coiled, "lin"), (0.254, barge, "in"),
                         (0.31, held, "out"), (0.4167, STAND)])


def interact_right():
    """"Dito. Upo." His right fist comes up, goes out and down ahead of him (0.18 s) and PATS
    the seat he is offering, twice, his head tipped at it and his left fist on his hip. Slow
    and friendly, nothing like the tag."""
    lift = _pose(sq=1.03, lean=-6.0, nod=0.0, armR=((0.62, -0.20, 0.76), (0.30, 0.50, 0.81)), armL=ARM_HIP)
    here = _pose(sq=0.94, lean=9.0, twist=20.0, nod=17.0, look=-16.0, tilt=-5.0, legL=(0.0, 6.0), legR=(0.0, 6.0),
                 armR=((0.46, -0.44, 0.77), (0.30, -0.62, 0.72)), reachR=0.60, armL=ARM_HIP)
    off = _pose(here, sq=1.0, lean=5.0, nod=10.0, armR=((0.50, -0.26, 0.83), (0.36, -0.16, 0.92)), reachR=0.25)
    pat = _pose(here, sq=0.92, lean=11.0, nod=19.0, reachR=0.56)
    held = _pose(here, sq=0.97, lean=8.0, nod=14.0, reachR=0.42)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.07, lift), (0.18, here, "in"), (0.25, off, "out"), (0.32, pat, "in"),
                            (0.40, held, "out"), (2.0 / 3.0, STAND)])


def interact_left():
    """"Bayad po." The fare hand. His left elbow stays at his side and the forearm comes up level
    with the hand out ahead of his belly (0.18 s), leaning back behind it; he jiggles it twice
    for the coins, head cocked, looking over his glasses at you."""
    gather = _pose(sq=0.97, lean=-6.0, nod=4.0, armL=((0.80, -0.58, -0.12), (0.44, -0.40, 0.80)))
    out = _pose(sq=1.02, lean=-13.0, twist=-14.0, roll=-3.0, nod=9.0, look=14.0, tilt=11.0,
                legL=(0.0, 5.0), legR=(0.0, 5.0),
                armL=((0.56, -0.56, 0.61), (0.28, 0.10, 0.955)), reachL=0.60,
                armR=ARM_BEHIND)
    dip = _pose(out, sq=0.95, armL=((0.56, -0.62, 0.55), (0.28, -0.36, 0.89)), reachL=0.46, tilt=6.0)
    jig = _pose(out, sq=1.04, armL=((0.56, -0.50, 0.66), (0.26, 0.42, 0.87)), reachL=0.62, tilt=13.0)
    held = _pose(out, sq=1.0, reachL=0.52)
    return _act(2.0 / 3.0, [(0.0, STAND), (0.08, gather, "out"), (0.18, out, "in"), (0.235, dip), (0.29, jig),
                            (0.345, dip), (0.40, held), (2.0 / 3.0, STAND)])


#   flat on his belly, both fists out ahead of him and apart (which is +y, lying down): the handlebars
ARM_FLAT = ((0.74, 0.58, 0.34), (0.38, 0.91, 0.16))


def slide():
    """He rides it in on his belly. A sink with both arms back; he dives, and lands on the
    belly, which rocks him like a boat: nose down at 0.25 s, both fists out ahead on the
    handlebars and his shoes up in the air; the right fist goes long for the slipper at
    0.342 s as the nose comes up; he skids, still steering; then shoves himself up off both
    fists, comes up too far, and rocks back onto his heels."""
    back = ((0.72, -0.46, -0.52), (0.52, -0.60, -0.60))
    sink = _pose(sq=0.87, lean=12.0, nod=0.0, legL=(0.0, 6.0), legR=(0.0, 6.0), armL=back, armR=back)
    dive = _pose(sq=1.10, pitch=40.0, ty=0.035, tz=-0.070, nod=-25.0, lean=-4.0, legL=(-12.0, 6.0), legR=(-12.0, 6.0),
                 armL=ARM_FLAT, armR=ARM_FLAT, reachL=0.20, reachR=0.20)
    nose = _pose(sq=0.88, pitch=90.0, ty=0.100, tz=-0.250, lean=-6.0, nod=-48.0, legL=(-44.0, 12.0), legR=(-36.0, 12.0),
                 armL=ARM_FLAT, armR=ARM_FLAT, reachL=0.30, reachR=0.30)
    reach = _pose(nose, sq=1.06, pitch=76.0, ty=0.105, tz=-0.235, lean=-12.0, nod=-58.0, look=-8.0,
                  legL=(-20.0, 10.0), legR=(-26.0, 10.0), reachR=0.75, reachL=0.12,
                  armL=((0.86, 0.40, 0.30), (0.62, 0.74, 0.24)))
    skid = _pose(reach, sq=1.0, pitch=82.0, ty=0.102, roll=5.0, legL=(-34.0, 12.0), legR=(-28.0, 12.0), reachR=0.45)
    push = _pose(sq=0.90, pitch=32.0, ty=0.020, tz=-0.085, lean=14.0, nod=-12.0, legL=(-6.0, 8.0), legR=(-6.0, 8.0),
                 armL=((0.60, -0.60, 0.52), (0.30, -0.90, 0.30)), armR=((0.60, -0.60, 0.52), (0.30, -0.90, 0.30)))
    over = _pose(sq=1.05, pitch=-7.0, lean=-9.0, nod=0.0)
    return _act(0.95, [(0.0, STAND), (0.08, sink, "out"), (0.14, dive, "in"), (0.25, nose, "in"), (0.342, reach, "out"),
                       (0.55, skid), (0.71, push), (0.85, over), (0.95, STAND)])


CROUCH_LENGTH = 1.8
ARM_HUNG = ((0.56, -0.72, 0.42), (0.20, -0.95, 0.24))
CROUCH = _pose(sq=0.86, ty=-0.006, lean=44.0, nod=40.0, legL=(0.0, 22.0), legR=(0.0, 22.0),
               armL=ARM_HUNG, armR=ARM_HUNG, reachL=0.40, reachR=0.40)


def crouch():
    """OUT OF BREATH, a 1.8 s loop. He is too round to prop his hands on his knees, so he hangs:
    feet wide, bent over as far as the belly allows, both arms dropped straight down and
    swinging loose. One heave lifts his back and lets it sag; on the second he throws his head
    right back and gulps at the sky, arms swinging out behind him, and flops down again, the
    arms swinging through. The first and the last frame are the full bent-over pose: the game
    loops this while he is winded and also holds its last frame as an emote."""
    def pose_at(t):
        heave = _bump(t, 0.36, 0.17)
        gulp = _window(t, 0.80, 1.42, 0.24)
        flop = _bump(t, 1.50, 0.11)
        # the arms: a pendulum under his shoulders, pushed by each lift of his back
        hang = 22.0 * gulp - 26.0 * flop + 9.0 * heave - 7.0 * _bump(t, 0.60, 0.12)
        arms = (_rotx(ARM_HUNG[0], 0.5 * hang), _rotx(ARM_HUNG[1], hang))
        return _pose(CROUCH, sq=0.86 + 0.04 * heave + 0.12 * gulp - 0.03 * flop,
                     lean=44.0 - 10.0 * heave - 44.0 * gulp + 4.0 * flop,
                     nod=40.0 - 12.0 * heave - 72.0 * gulp + 6.0 * flop,
                     tilt=7.0 * gulp, armL=arms, armR=arms,
                     reachL=0.40 - 0.20 * gulp + 0.10 * flop, reachR=0.40 - 0.20 * gulp + 0.10 * flop)
    return _frames(CROUCH_LENGTH, pose_at)


def sit():
    """0.8 s of getting down, ending seated. He backs onto it as onto his own saddle: a look
    behind and a sink with both arms reaching back; he drops; lands with a squash at 0.42 s, his
    shoes flying up; rolls back onto his arms and forward again. He ends as a man sits on the
    kerb after lunch: leaning well back on both arms planted behind him, belly up, legs out wide."""
    planted = ((0.74, -0.46, -0.50), (0.34, -0.78, -0.52))
    seat = _pose(ty=-0.095, pitch=-15.0, lean=-7.0, nod=8.0, legL=(73.0, 30.0), legR=(73.0, 30.0),
                 armL=planted, armR=planted, reachL=0.25, reachR=0.25)
    back = ((0.76, -0.54, -0.36), (0.44, -0.80, -0.40))
    sink = _pose(sq=0.90, lean=12.0, nod=10.0, look=-30.0, legL=(0.0, 8.0), legR=(0.0, 8.0), armL=back, armR=back)
    drop = _pose(sq=1.05, ty=-0.048, pitch=-10.0, lean=4.0, nod=0.0, legL=(44.0, 16.0), legR=(44.0, 16.0),
                 armL=back, armR=back, reachL=0.15, reachR=0.15)
    land = _pose(seat, sq=0.83, pitch=-8.0, lean=2.0, nod=14.0, legL=(84.0, 34.0), legR=(84.0, 34.0))
    roll = _pose(seat, sq=1.04, pitch=-26.0, lean=-10.0, nod=4.0, legL=(92.0, 30.0), legR=(86.0, 30.0))
    forward = _pose(seat, sq=0.98, pitch=-11.0, lean=-4.0, nod=10.0, legL=(70.0, 31.0), legR=(70.0, 31.0))
    return _act(0.8, [(0.0, STAND), (0.14, sink), (0.32, drop, "in"), (0.42, land, "lin"), (0.54, roll, "out"),
                      (0.67, forward), (0.8, seat)])


def die():
    """KNOCKED DOWN, wheels up. The hit lifts him off his feet backward with all four limbs
    thrown out ahead of him; he lands on his back (0.22 s), squashes and bounces on it once;
    and ends like a tricycle on its roof: on his back, belly up, both arms and both legs in the
    air, one leg settling lower."""
    air = ((0.50, 0.05, 0.86), (0.30, 0.05, 0.95))         # straight up, once he is on his back
    jolt = _pose(sq=0.90, pitch=-10.0, lean=-12.0, nod=-14.0, legL=(10.0, 5.0), legR=(10.0, 5.0),
                 armL=((0.70, 0.10, 0.70), (0.50, 0.40, 0.76)), armR=((0.70, 0.10, 0.70), (0.50, 0.40, 0.76)))
    flung = _pose(sq=1.08, pitch=-52.0, ty=0.050, lean=6.0, nod=10.0, legL=(40.0, 12.0), legR=(40.0, 12.0),
                  armL=air, armR=air, reachL=0.45, reachR=0.45)
    slam = _pose(sq=0.83, pitch=-90.0, ty=0.100, nod=12.0, legL=(84.0, 18.0), legR=(84.0, 18.0),
                 armL=air, armR=air, reachL=0.50, reachR=0.50)
    bounce = _pose(slam, sq=1.06, ty=0.130, nod=-2.0, legL=(60.0, 22.0), legR=(70.0, 22.0), reachL=0.20, reachR=0.20)
    rest = _pose(sq=1.0, pitch=-90.0, ty=0.100, nod=2.0, look=18.0, legL=(76.0, 20.0), legR=(48.0, 16.0),
                 armL=((0.56, 0.10, 0.82), (0.40, 0.20, 0.90)), armR=((0.44, -0.04, 0.90), (0.24, -0.02, 0.97)),
                 reachL=0.30, reachR=0.38)
    return _act(1.0 / 3.0, [(0.0, STAND), (0.05, jolt, "out"), (0.13, flung, "in"), (0.22, slam, "in"),
                            (0.267, bounce, "out"), (1.0 / 3.0, rest, "in")])


def emote_yes():
    """"AYOS!" A 1.2 s loop that he fills. A sink, both fists gathered; then a hop with his left
    fist punched straight up at the sky, arm stretched, heels kicked up behind, chin up, and
    held; down, the fist pumped to his shoulder with a squash and a nod; a second hop and punch,
    higher; a second pump; and he holds the fist at his shoulder a moment, pleased, before it
    drops. The right fist stays chambered at his ribs throughout."""
    sky = ((0.92, 0.38, 0.10), (0.50, 0.86, 0.05))
    pumped = ((0.86, -0.34, 0.12), (0.36, 0.84, 0.40))
    dip = _pose(sq=0.90, lean=3.0, nod=10.0, legL=(0.0, 7.0), legR=(0.0, 7.0), armL=pumped, armR=ARM_RIBS)
    up = _pose(sq=1.10, ty=0.024, roll=-4.0, lean=-9.0, nod=-12.0, tilt=5.0, legL=(-14.0, 8.0), legR=(-14.0, 8.0),
               armL=sky, reachL=1.35, armR=ARM_RIBS)
    up_held = _pose(up, sq=1.04, ty=0.0, legL=(0.0, 5.0), legR=(0.0, 5.0), reachL=1.20)
    pump = _pose(dip, sq=0.88, lean=6.0, nod=16.0)
    up2 = _pose(up, ty=0.032, sq=1.11, reachL=1.45)
    up2_held = _pose(up_held, reachL=1.25)
    pump2 = _pose(dip, sq=0.86, lean=8.0, nod=20.0, legL=(0.0, 9.0), legR=(0.0, 9.0))
    proud = _pose(dip, sq=1.0, lean=-8.0, nod=-2.0, legL=(0.0, 5.0), legR=(0.0, 5.0))
    return _act(1.2, [(0.0, STAND), (0.11, dip), (0.21, up, "out"), (0.36, up_held), (0.44, pump, "in"),
                      (0.55, up2, "out"), (0.72, up2_held), (0.80, pump2, "in"), (0.90, proud, "out"), (1.02, proud),
                      (1.2, STAND)])


NO_LENGTH = 1.3


def emote_no():
    """"Naku, hindi, hindi." A 1.3 s loop. Both hands come up beside his shoulders, palms out,
    and he backs off behind them, leaning away with his chin tucked: no, not him, not today.
    Three times his chest swings one way and his head the other, the forearms fanning out and
    in with it, and he sinks a little on each. Then the hands drop."""
    def pose_at(t):
        up = _window(t, 0.0, NO_LENGTH, 0.20) if t < 0.5 * NO_LENGTH else 1.0 - _ease((t - 1.02) / 0.28)
        wag = math.sin(2.0 * math.pi * (t - 0.20) / 0.28) * _window(t, 0.16, 1.08, 0.10)
        beat = abs(wag)
        splay = 0.46 + 0.26 * wag
        hands = ((0.92, -0.16, 0.30), (splay, 0.84, 0.30))
        other = ((0.92, -0.16, 0.30), (0.46 - 0.26 * wag, 0.84, 0.30))
        return _pose(sq=1.0 - 0.05 * up * beat + 0.02 * up, tz=-0.016 * up, pitch=-7.0 * up,
                     lean=-4.0 - 8.0 * up, twist=13.0 * wag, nod=1.4 + 7.0 * up, look=-24.0 * wag, tilt=4.0 * wag,
                     legL=(0.0, 3.0 + 4.0 * up), legR=(0.0, 3.0 + 4.0 * up),
                     armL=_blend(ARMS_STOOD, hands, up), armR=_blend(ARMS_STOOD, other, up),
                     reachL=0.50 * up, reachR=0.50 * up)
    return _frames(NO_LENGTH, pose_at)


# ---------------------------------------------------------------------------
# THE ELDER'S CLIPS
# ---------------------------------------------------------------------------
# THE RIG'S JOINTS, from character-male-e.glb (glTF space, each from its parent)
TORSO_OFF = (0.0, 0.17625, -0.02875)
ARM_OFF = {1: (0.09989677, 0.1115, 0.0115), -1: (-0.09989677, 0.1115, 0.0115)}
FORE_OFF = {1: (0.0781, 0.0, 0.0), -1: (-0.0781, 0.0, 0.0)}
UPPER_LEN = 0.0781
NAMES = {1: "left", -1: "right"}


def _add3(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def _sub3(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def _dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _scale3(a, k):
    return (a[0] * k, a[1] * k, a[2] * k)


def _get(r, bone, path):
    default = (0.0, 0.0, 0.0, 1.0) if path == "rotation" else ((1.0, 1.0, 1.0) if path == "scale" else (0.0, 0.0, 0.0))
    return r.get((bone, path), default)


def _fk(r, side, p):
    """Where the point `p`, given in the forearm's own space, is in the world, for the row `r`."""
    name = NAMES[side]
    sf = _get(r, "forearm-" + name, "scale")
    p = (p[0] * sf[0], p[1] * sf[1], p[2] * sf[2])
    p = _add3(FORE_OFF[side], _rot(_get(r, "forearm-" + name, "rotation"), p))
    p = _add3(ARM_OFF[side], _rot(_get(r, "arm-" + name, "rotation"), p))
    p = _add3(TORSO_OFF, _rot(_get(r, "torso", "rotation"), p))
    s = _get(r, "root", "scale")
    p = (p[0] * s[0], p[1] * s[1], p[2] * s[2])
    return _add3(_get(r, "root", "translation"), _rot(_get(r, "root", "rotation"), p))


def _to_root(r, p):
    """A world point in the root's own space (before its turn and squash)."""
    p = _sub3(p, _get(r, "root", "translation"))
    p = _rot(_inv(_get(r, "root", "rotation")), p)
    s = _get(r, "root", "scale")
    return (p[0] / s[0], p[1] / s[1], p[2] / s[2])


def _twist(r, target):
    """Turn the right forearm about its own length so the spear points as near `target` as it can."""
    w = mul(mul(mul(_get(r, "root", "rotation"), _get(r, "torso", "rotation")), _get(r, "arm-right", "rotation")),
            _get(r, "forearm-right", "rotation"))
    t = _norm(target)
    yv, zv = _rot(w, (0.0, 1.0, 0.0)), _rot(w, (0.0, 0.0, 1.0))
    tau = math.degrees(math.atan2(_dot(t, zv), _dot(t, yv)))
    r[("forearm-right", "rotation")] = mul(_get(r, "forearm-right", "rotation"), qx(tau))


def butt(r):
    return _fk(r, -1, (-FIST_X, -GRIP_DROP, 0.0))


def fist(r, side):
    return _fk(r, side, (side * FIST_X, 0.0, 0.0))


def _swing(p, d):
    q = dict(p)
    q["armR"] = (_rotx(p["armR"][0], d), _rotx(p["armR"][1], d))
    return q


def _ik(p, side, target, hint, reach):
    """(upper, forearm) directions, written for a LEFT arm in his own space, that put the middle
    of the fist on `target` (a point in his own space), the elbow bowed toward `hint`."""
    torso = mul(mul(qx(p["lean"]), qy(p["twist"])), qz(p["hunch"]))
    shoulder = _add3(TORSO_OFF, _rot(torso, ARM_OFF[side]))
    a, b = UPPER_LEN, FIST_X * (1.0 + reach)
    d = _sub3(target, shoulder)
    length = math.sqrt(_dot(d, d))
    dn = _scale3(d, 1.0 / length)
    length = min(length, 0.999 * (a + b))
    cos_a = max(-1.0, min(1.0, (a * a + length * length - b * b) / (2.0 * a * length)))
    sin_a = math.sqrt(1.0 - cos_a * cos_a)
    h = (side * hint[0], hint[1], hint[2])
    n = _sub3(h, _scale3(dn, _dot(h, dn)))
    n = _norm(n)
    elbow = _add3(shoulder, _scale3(_add3(_scale3(dn, cos_a), _scale3(n, sin_a)), a))
    tip = _add3(shoulder, _scale3(dn, length))
    up = _norm(_sub3(elbow, shoulder))
    fo = _norm(_sub3(tip, elbow))
    return ((side * up[0], up[1], up[2]), (side * fo[0], fo[1], fo[2]))


def _row(p):
    """Mang Kanor's row (`_row_base`), then the spear: turned toward `spear`, and when `plant` is
    0 or more the right arm swung at the shoulder until the butt is at that height. A pose may
    also carry `left_on` = (offset, reach): the left fist is put at the right fist plus that
    offset (in his own space), which is how the clap meets the hand holding the spear, and
    `left_at` / `right_at` = (point in his own space, reach, elbow hint) to reach a place."""
    target = p.get("spear", SPEAR_UP)
    plant = p.get("plant", -1.0)
    if "right_at" in p:
        at, reach, hint = p["right_at"]
        p = dict(p, armR=_ik(p, -1, at, hint, reach), reachR=reach)
    if "left_at" in p:
        at, reach, hint = p["left_at"]
        p = dict(p, armL=_ik(p, 1, at, hint, reach), reachL=reach)
    d = 0.0
    if plant > -0.5:
        for _ in range(8):
            r0 = _row_base(_swing(p, d)); _twist(r0, target)
            f0 = butt(r0)[1] - plant
            if abs(f0) < 1e-4:
                break
            r1 = _row_base(_swing(p, d + 1.0)); _twist(r1, target)
            slope = (butt(r1)[1] - plant) - f0
            if abs(slope) < 1e-7:
                break
            d = max(-50.0, min(50.0, d - f0 / slope))
    q = _swing(p, d)
    r = _row_base(q)
    _twist(r, target)
    if "left_on" in p:
        offset, reach = p["left_on"]
        at = _add3(_to_root(r, fist(r, -1)), offset)
        upper, fore = _ik(q, 1, at, (1.0, -0.8, -0.2), reach)
        torso = _get(r, "torso", "rotation")
        ql = _arm_in(1, upper, fore, torso)
        r[("arm-left", "rotation")] = ql[0]
        r[("forearm-left", "rotation")] = ql[1]
        r[("forearm-left", "scale")] = (1.0 + reach, 1.0, 1.0)
    return r



# ---------------------------------------------------------------------------
# IDLE. 8 s, acted. Frame 0 is the stand.
# ---------------------------------------------------------------------------
IDLE_LENGTH = 8.0
#   his left fist up at his brow, shading his eyes (a point in his own space; the forearm stretches
#   to get there, as the cast's do)
SHADE_AT = (0.060, 0.585, 0.250)
#   pointing out at the play with his left fist, the forearm level
ARM_POINT = ((0.62, -0.18, 0.76), (0.34, 0.02, 0.94))


def idle():
    """Eight seconds of the lakay on the basin wall, leaning on his spear, watching the match.
    His right hand never leaves the spear; the butt stays on the ground (the solver in `_row`).

      WATCHING (0.0 to 2.5 s). Leaning his weight onto the spear, he follows the play from his
      left across to his right with his head, slowly, the way an old man watches: the eyes go
      first, the chest turns after.
      SHADING HIS EYES (2.6 to 4.7 s). Something at the far end. His left hand comes up flat over
      his brow, he leans further onto the spear and forward, chin out, peering; holds; lets it
      down.
      APPROVAL (4.9 to 6.3 s). A good play. He lifts the spear a hand's width and THUMPS the butt
      down twice, nodding into each thump: the elders' applause.
      POINTING IT OUT (6.4 to 7.7 s). He points at the field with his left fist, his head turned
      that way, as if telling the young man beside him what he just saw; then the hand drops.

    WHY THESE. The brief asks for a spectator's own stances; his are an elder's: still, slow,
    weighted onto the spear, and the spear itself is his voice (the thump). None of them is in
    the copied set or any Classic's idle (Lola Pacing fans herself and wags a finger, Mang Kanor
    nudges his glasses and hails a fare).
    """
    out = {}
    for t in _times(IDLE_LENGTH):
        breath = math.sin(2.0 * math.pi * t / (IDLE_LENGTH / 3.0))
        # the eyes follow the play from his left across to his right and back to the middle
        follow = 18.0 * math.sin(2.0 * math.pi * (t - 0.2) / 2.4) * _window(t, 0.2, 2.6, 0.3)
        shade = _window(t, 2.6, 4.7, 0.42)
        peer = _window(t, 3.0, 4.4, 0.35)
        approve = _window(t, 4.9, 6.3, 0.30)
        lift = 0.032 * approve * (_bump(t, 5.28, 0.09) + _bump(t, 5.78, 0.09))
        thump = _bump(t, 5.42, 0.07) + _bump(t, 5.92, 0.07)
        point = _window(t, 6.4, 7.7, 0.35)
        look = follow - 6.0 * shade + 24.0 * point
        p = _pose(
            sq=1.0 + 0.010 * breath - 0.012 * thump,
            roll=1.5 + 1.5 * shade, tx=-0.004 * shade,
            lean=STOOP + 5.0 * peer + 2.0 * approve + 3.0 * thump + 0.8 * breath,
            twist=0.35 * look, hunch=2.0 + 2.0 * shade,
            nod=4.0 - 7.0 * peer + 12.0 * thump + 3.0 * point - 0.6 * breath,
            look=look,
            tilt=-4.0 * peer + 3.0 * point,
            armL=_blend(ARMS_HANG, ARM_POINT, point), reachL=0.25 * point,
            spear=SPEAR_LEAN, plant=lift,
        )
        if shade > 0.0:
            # the shading hand reaches for the brow; blended in from the hanging arm
            shaded = _ik(p, 1, SHADE_AT, (1.0, -0.2, -0.6), 0.85)
            p["armL"] = _blend(p["armL"], shaded, shade)
            p["reachL"] = p["reachL"] + 0.85 * shade
        _add(out, t, _row(p))
    _fix_signs(out)
    return out


def _fix_signs(out):
    for (bone, path), rows in out.items():
        if path != "rotation":
            continue
        for i in range(1, len(rows)):
            if sum(a * b for a, b in zip(rows[i - 1][1], rows[i][1])) < 0.0:
                rows[i] = (rows[i][0], tuple(-k for k in rows[i][1]))


# ---------------------------------------------------------------------------
# CHEER, CLAP, SIT
# ---------------------------------------------------------------------------
#   THE SPEAR RAISED. A fist cannot hold a shaft upright above its own shoulder (the shaft is
#   square to the forearm), and v03 kept it upright at shoulder height, which read as no raise at
#   all; v04 threw the forearm straight up and the shaft lay flat across his face. So: the upper
#   arm up and out, the forearm up and AHEAD, the fist high in front of his right shoulder, and the
#   spear rising out of it tipped back over that shoulder, clear of his head, like a flag held up.
ARM_SPEAR_UP = ((0.52, 0.80, 0.30), (0.10, 0.62, 0.78))
SPEAR_BRANDISH = (-0.15, 0.85, -0.50)
ARM_V = ((0.86, 0.46, 0.20), (0.52, 0.84, 0.16))


def cheer():
    """A 1 s loop. One hop: he dips (frame 0, squashed), springs up out of his stoop with his
    head thrown back, the left arm up in the V through the elbow and the spear thrust high in
    the right, and comes down into the dip again. The spear stays upright, held aloft."""
    def pose_at(t):
        up = 0.5 - 0.5 * math.cos(2.0 * math.pi * t)
        dip = (0.5 + 0.5 * math.cos(2.0 * math.pi * t)) ** 3
        return _pose(sq=1.0 - 0.07 * dip + 0.05 * up, ty=0.026 * up ** 1.5,
                     lean=STOOP - 9.0 * up + 4.0 * dip, hunch=0.0, roll=0.0, nod=-6.0 - 12.0 * up + 6.0 * dip,
                     tilt=3.0 * math.sin(2.0 * math.pi * t),
                     legL=(-6.0 * up, 4.0 + 3.0 * up), legR=(-6.0 * up, 4.0 + 3.0 * up),
                     armL=(_rotx(ARM_V[0], -6.0 * up), ARM_V[1]), reachL=0.55 + 0.30 * up,
                     armR=(_rotx(ARM_SPEAR_UP[0], -8.0 * up), ARM_SPEAR_UP[1]), reachR=0.30 + 0.20 * up,
                     spear=SPEAR_BRANDISH, plant=-1.0)
    out = _frames(1.0, pose_at)
    return out


#   the hand on the spear, in front of his chest a little right of his middle
ARM_CLAP_SPEAR = ((0.38, -0.62, 0.69), (-0.62, 0.08, 0.78))
CLAP_TOUCH = (0.106, 0.004, 0.0)        # the left fist against the right one, from its left
CLAP_APART = (0.190, 0.040, -0.030)


def clap():
    """A 1 s loop of applause, and it is also `emote-yes`. He holds the spear upright in his
    right fist in front of his chest and CLAPS HIS LEFT HAND AGAINST THAT FIST, twice a second,
    and on each clap the spear's BUTT THUMPS THE GROUND (lifted a hand's width between): the
    clap and the thump are one beat. He nods and dips into each, swaying a little from one foot
    to the other between them, as Lola Pacing's applause does."""
    def closed(t):
        def one(at):
            if t < at - 0.13:
                return _ease((t - (at - 0.36)) / 0.23) if t > at - 0.36 else 0.0
            if t <= at + 0.05:
                return min(1.0, (t - (at - 0.13)) / 0.13) ** 2 if t < at else 1.0
            return 1.0 - _ease((t - (at + 0.05)) / 0.28)
        return max(one(8 / 60.0), one(38 / 60.0), one(68 / 60.0), one(-22 / 60.0))

    def pose_at(t):
        c = closed(t)
        sway = math.cos(2.0 * math.pi * t)
        return _pose(sq=1.0 - 0.05 * c + 0.02 * (1.0 - c), roll=2.0 * sway, tx=0.004 * sway,
                     lean=STOOP + 5.0 * c - 2.0 * (1.0 - c), nod=2.0 + 10.0 * c - 4.0 * (1.0 - c), hunch=-1.5 * sway,
                     legL=(0.0, 3.0 - 2.0 * sway), legR=(0.0, 3.0 + 2.0 * sway),
                     armR=ARM_CLAP_SPEAR, reachR=0.0,
                     spear=(0.0, 1.0, 0.06), plant=0.034 * (1.0 - c) ** 1.5,
                     left_on=(_lerp(CLAP_APART, CLAP_TOUCH, c), 0.15))
    out = _frames(1.0, pose_at)
    return out


#   SEATED. Where the root goes so the underside of his hips is on the origin (measured off the
#   posed mesh by the render script's `seat` shot), the legs swung forward over the edge.
SEAT_TY = -0.0834
SEAT_TZ = 0.020
SIT_LEGS = 55.0
#   the spear hand: the upper arm hanging close to his side, the forearm LEVEL and ahead, turned in
#   over his lap, so the spear in it stands straight up in front of his knees (v01 reached the
#   fist to a point and the forearm sloped, which tipped the spear 40 degrees)
ARM_SIT_SPEAR = ((0.20, -0.94, 0.28), (-0.42, 0.0, 0.91))
SIT_KNEE_AT = (0.090, 0.064 - SEAT_TY, 0.118 - SEAT_TZ)      # the left fist resting on his left knee
SIT_LENGTH = 3.0


def sit():
    """SEATED ON A LEDGE, a 3 s loop (the brief). His hips on the ledge top at the root's origin,
    his legs swung forward over the edge (the rig has no knee: each leg is one bone, so the shin
    cannot hang straight down; the whole leg slopes forward and down at 72 degrees), his back in
    its small stoop. He holds the spear UPRIGHT IN FRONT OF HIM BETWEEN HIS KNEES in his right
    fist over the middle of his lap, the butt hanging toward the foot of the wall; his left fist
    rests on his left knee. He breathes, and his head drifts slowly across the play and back.
    Every frame is a seated frame, so frame 0 is the pose the static crowd bakes."""
    def pose_at(t):
        breath = math.sin(2.0 * math.pi * t / SIT_LENGTH)
        drift = math.sin(2.0 * math.pi * t / SIT_LENGTH)
        return _pose(tx=0.0, ty=SEAT_TY, tz=SEAT_TZ, sq=1.0 + 0.008 * breath, roll=0.0,
                     lean=STOOP - 2.0 + 1.0 * breath, hunch=0.0, nod=7.0 - 0.6 * breath, look=14.0 * drift, tilt=-2.0 * drift,
                     twist=3.0 * drift,
                     legL=(SIT_LEGS, 7.0), legR=(SIT_LEGS, 7.0),
                     spear=(0.0, 1.0, 0.02), plant=-1.0,
                     armR=ARM_SIT_SPEAR, reachR=0.30,
                     left_at=(SIT_KNEE_AT, 0.05, (1.0, 0.2, -0.4)))
    return _frames(SIT_LENGTH, pose_at)


# ---------------------------------------------------------------------------
# LOCOMOTION, lightly his: the stoop, the spear carried upright in the right fist
# ---------------------------------------------------------------------------
ARM_CARRY = ((0.30, -0.58, 0.76), (-0.04, 0.10, 0.99))


def walk():
    """An old man's walk, 0.72 s: stooped and steady, short steps, the head level, the spear
    carried upright ahead of him in his right fist with only a little swing, the left arm
    swinging loose."""
    def pose_at(t):
        phase = 2.0 * math.pi * t / 0.72
        s = math.sin(phase)
        lift = abs(s) ** 0.9
        roll = math.sin(phase - 0.35)
        swing = math.sin(phase - 0.3)
        return _pose(tx=0.010 * roll, ty=0.002 + 0.010 * lift, sq=0.985 + 0.025 * lift, roll=3.0 * roll,
                     lean=STOOP + 2.0, twist=3.0 * s, hunch=-2.0 * roll, nod=3.0, look=-2.5 * s,
                     legL=(24.0 * s, 3.0), legR=(-24.0 * s, 3.0),
                     armL=(_rotx(ARMS_HANG[0], 16.0 * swing), _rotx(ARMS_HANG[1], 22.0 * swing)),
                     armR=(_rotx(ARM_CARRY[0], -5.0 * swing), ARM_CARRY[1]),
                     spear=(0.0, 1.0, 0.08 * swing), plant=-1.0)
    return _frames(0.72, pose_at)


ARM_JOG = ((0.70, -0.64, 0.30), (0.20, 0.10, 0.97))


def sprint():
    """A determined old man's hustle, 0.48 s: leaning in, elbows working, the spear upright in
    his fist pumping with the right arm."""
    def pose_at(t):
        phase = 2.0 * math.pi * t / 0.48
        s = math.sin(phase)
        sh = _snap(s, 0.75)
        lift = abs(s) ** 0.8
        return _pose(ty=0.004 + 0.022 * lift, sq=0.96 + 0.06 * lift, roll=3.0 * math.sin(phase - 0.3),
                     pitch=6.0, lean=STOOP + 6.0, twist=6.0 * sh, nod=-4.0,
                     legL=(36.0 * sh, 4.0), legR=(-36.0 * sh, 4.0),
                     armL=(_rotx(ARM_JOG[0], 24.0 * sh), _rotx(ARM_JOG[1], 30.0 * sh)),
                     armR=(_rotx(ARM_CARRY[0], -16.0 * sh), _rotx(ARM_CARRY[1], -10.0 * sh)),
                     spear=SPEAR_UP, plant=-1.0)
    return _frames(0.48, pose_at)


def jump():
    """A standing two-footed hop, 0.5 s: a stretch out of the stoop, the left arm up in the V
    through the elbow, the spear thrust up in the right, the heels kicked up, the chin up."""
    def pose_at(t):
        rise = math.exp(-t / 0.13)
        hang = 1.0 - math.exp(-t / 0.14)
        ring = math.cos(2.0 * math.pi * t / 0.50) * math.exp(-t / 0.15)
        return _pose(sq=1.0 + 0.12 * ring, pitch=-3.0 * hang, lean=STOOP - 10.0 * rise - 4.0 * hang,
                     nod=-10.0 * rise - 2.0 * hang, roll=0.0, hunch=0.0,
                     legL=(-(8.0 * rise + 14.0 * hang), 2.0 + 6.0 * hang), legR=(-(8.0 * rise + 14.0 * hang), 2.0 + 6.0 * hang),
                     armL=ARM_V, reachL=0.65 + 0.2 * rise, armR=ARM_SPEAR_UP, reachR=0.15,
                     spear=SPEAR_BRANDISH, plant=-1.0)
    return _frames(0.50, pose_at)


def fall():
    """A loop: coming down with the feet paddling under him, the arms up and the spear held high."""
    def pose_at(t):
        phase = 2.0 * math.pi * t / (1.0 / 3.0)
        s, c = math.sin(phase), math.cos(phase)
        return _pose(sq=1.03 + 0.01 * math.sin(2.0 * phase), pitch=5.0, roll=1.5 * s, lean=STOOP - 6.0, hunch=0.0,
                     nod=10.0 + 1.5 * c, legL=(-(-4.0 + 12.0 * s), 6.0), legR=(-(-4.0 - 12.0 * s), 6.0),
                     armL=(_rotx(ARM_V[0], 6.0 * c), ARM_V[1]), reachL=0.6,
                     armR=(_rotx(ARM_SPEAR_UP[0], -6.0 * c), ARM_SPEAR_UP[1]), reachR=0.15,
                     spear=SPEAR_BRANDISH, plant=-1.0)
    return _frames(1.0 / 3.0, pose_at)


CLIPS = {
    "idle": idle, "walk": walk, "sprint": sprint, "jump": jump, "fall": fall,
    "cheer": cheer, "clap": clap, "emote-yes": clap, "sit": sit,
    # the copied ones: they go through `_row` too, so the spear stands upright in his fist in each
    "holding-right": holding_right, "holding-right-shoot": holding_right_shoot,
    "pick-up": pick_up, "attack-melee-right": attack_melee_right,
    "attack-melee-left": attack_melee_left, "interact-right": interact_right,
    "interact-left": interact_left, "slide": slide, "crouch": crouch,
    "die": die, "emote-no": emote_no,
}
