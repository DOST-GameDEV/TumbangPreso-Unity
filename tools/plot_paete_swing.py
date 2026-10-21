"""The Liana Leap swing, in the plane through him and the catch: x along the ground toward it, y up, from his feet.
A line for line port of Core.PaeteSwing.Plan, so the paths can be seen before they are played."""
import math, sys
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

G = 20.0
STEP = 0.02
MAX_STEPS = 110
CHEST = 1.3
STAND_OFF = 0.55
OVER_CLEAR = 0.35
START_SPEED, TOP_SPEED, SPEED_GAIN = 5.0, 13.0, 55.0
OVER_FLING_X, OVER_FLING_Y, OVER_HOLD = 5.5, 4.0, 0.28
WALL_FLING_Y = 2.0
UNDER_ROPE, UNDER_MIN_ROPE, UNDER_LET_GO = 0.65, 1.5, 0.75
UNDER_SPEED = 12.5
UNDER_HOLD = 1.2
MAX_FLING_Y = 9.0
OVER, WALL, UNDER = 1, 2, 3


def plan(kind, ax, ay):
    """A curve from his feet round a corner to (ex, ey). UNDER carries on from there in an arc on the vines."""
    rope = 0.0
    if kind == UNDER:
        over = ay - CHEST
        rope = max(UNDER_MIN_ROPE, over * UNDER_ROPE)
        ex, ey = ax, over - rope
        cx, cy = ax * 0.5, ey
        top = UNDER_SPEED
    else:
        ex = max(0.0, ax - STAND_OFF)
        ey = ay + OVER_CLEAR if kind == OVER else max(0.3, ay - 1.1)
        cx, cy = ex * 0.85, ey * 0.10
        top = TOP_SPEED
    xs, ys = [0.0], [0.0]
    t = u = 0.0
    while u < 1.0 and len(xs) < MAX_STEPS:
        t += STEP
        speed = min(top, START_SPEED + SPEED_GAIN * t)
        dx = 2 * (1 - u) * cx + 2 * u * (ex - cx)
        dy = 2 * (1 - u) * cy + 2 * u * (ey - cy)
        u = min(1.0, u + speed * STEP / max(0.5, math.hypot(dx, dy)))
        xs.append(2 * (1 - u) * u * cx + u * u * ex)
        ys.append(2 * (1 - u) * u * cy + u * u * ey)
    swung = 0.0
    while kind == UNDER and swung < UNDER_LET_GO and len(xs) < MAX_STEPS:
        t += STEP
        swung = min(UNDER_LET_GO, swung + min(top, START_SPEED + SPEED_GAIN * t) * STEP / rope)
        xs.append(ex + rope * math.sin(swung))
        ys.append(ey + rope * (1 - math.cos(swung)))
    if kind == OVER: return xs, ys, OVER_FLING_X, OVER_FLING_Y, OVER_HOLD
    if kind == WALL: return xs, ys, 0.0, WALL_FLING_Y, 0.0
    speed = min(top, START_SPEED + SPEED_GAIN * t)
    lift = min(MAX_FLING_Y, speed * math.sin(swung))
    return xs, ys, speed * math.cos(swung), lift, UNDER_HOLD * lift / G


def after(xs, ys, fx, fy, hold, floor_at):
    """What the motor does with him once the vine lets go: the carry held, then friction, gravity throughout."""
    x, y, vx, vy, t = xs[-1], ys[-1], fx, fy, 0.0
    ox, oy = [x], [y]
    while t < 3.0:
        t += STEP
        if t > hold: vx = max(0.0, vx - 30.0 * STEP)
        vy -= G * STEP
        x += vx * STEP; y += vy * STEP
        ground = floor_at(x)
        if y <= ground and vy < 0: ox.append(x); oy.append(ground); break
        ox.append(x); oy.append(y)
    return ox, oy


cases = [
    ("OVER: roof edge, 45 degrees", OVER, 5.6, 6.9, True),
    ("OVER: roof edge, steep", OVER, 2.5, 8.6, True),
    ("WALL: high on a bare wall", WALL, 5.0, 6.0, None),
    ("UNDER: the prototype map beam, from 6 m back", UNDER, 6.0, 5.8, False),
    ("UNDER: lamp post, far", UNDER, 7.0, 4.5, False),
    ("UNDER: branch, nearly overhead", UNDER, 1.0, 5.0, False),
]
fig, axes = plt.subplots(2, 3, figsize=(18, 8.2))
for (name, kind, ax_, ay_, lip), a in zip(cases, axes.flat):
    xs, ys, fx, fy, hold = plan(kind, ax_, ay_)
    if lip is True:
        a.fill([ax_, ax_ + 6, ax_ + 6, ax_], [0, 0, ay_, ay_], color="#c9b79c")
        floor = lambda x, ax_=ax_, ay_=ay_: ay_ if x > ax_ else 0.0
    elif lip is None:
        a.fill([ax_, ax_ + 6, ax_ + 6, ax_], [0, 0, 10.5, 10.5], color="#c9b79c")
        floor = lambda x: 0.0
    else:
        floor = lambda x: 0.0
    ox, oy = after(xs, ys, fx, fy, hold, floor)
    secs = (len(xs) - 1) * STEP
    a.plot(xs, ys, color="#2e8b3d", lw=3, label="on the vine, %.2f s" % secs)
    a.plot(ox, oy, color="#e08a1e", lw=3, ls="--", label="let go (he can steer in the air)")
    for i in range(0, len(xs), 5):
        a.plot([xs[i], ax_], [ys[i] + CHEST, ay_], color="#2e8b3d", lw=.6, alpha=.5)
    a.plot([ax_], [ay_], "o", color="#b0281f", ms=9)
    a.plot([ox[-1]], [oy[-1]], "v", color="#333", ms=9)
    a.axhline(0, color="#555", lw=1)
    a.set_title("%s\ncatch %.1f m out, %.1f m up. peak %.1f m, lands %.1f m out" % (name, ax_, ay_, max(ys + oy), ox[-1]), fontsize=10)
    a.set_aspect("equal"); a.set_xlim(-1, 21); a.set_ylim(-.5, 10.5); a.legend(loc="lower right", fontsize=8)
    print(name, "vine %.2fs" % secs, "peak %.2f" % max(ys + oy), "lands x=%.2f" % ox[-1], "fling %.1f %.1f" % (fx, fy), "n", len(xs))
fig.suptitle("Liana Leap swing: his feet, seen from the side. Red dot = where the vine catches. Today every one of these is a flat 8 m pull with a 1 m hop.", fontsize=12)
fig.tight_layout()
fig.savefig(sys.argv[1], dpi=110)
