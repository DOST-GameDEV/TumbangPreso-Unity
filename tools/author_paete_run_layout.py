"""PAETE'S RUN: THE GAMEPLAY LAYOUT, as data (2026-10-10). Plain `py -3 tools/author_paete_run_layout.py`, no Blender.

Reads the course (tools/paete_run_course.py: 36 legs, three shortcuts) and what each leg is (tools/paete_run_stages.py)
and writes Assets/TumbangPreso/Art/PaeteRun/paete_run_layout.json in UNITY axes: Blender (x, y, z) is Unity (-x, z, -y),
moved so the START LINE is the origin at height zero. Everything that is not art is in it: solids (boxes and upright
cylinders, each a SOLID collider), movers (every moving obstacle, its motion a closed-form function of one clock), swim
volumes, geysers, vine anchors, chutes, checkpoints, the start grid, shortcuts, the kill height, bounds, review cameras,
and a route per leg for the play probe. It prints, and writes to Logs/paete-run/layout_report.txt, a check of every gap
and every swing against THE BODY THE GAME HAS.

THE BODY (read from the code on 2026-10-10; change them here if the code changes):
  Balance.JumpVelocity 5.75, Balance.CharacterGravity 20, fixed step 0.02  -> a jump tops out 0.80 m up, 0.58 s in the air.
  Stamina.MovementSpeed: an attacker walks 2.5 and RUNS (sprint held) 5.0 m/s. MovementRework: a body in the air keeps
  the speed it took off with and cannot brake; the cap is 1.2 x run = 6.0 m/s; every jump adds 0.35 to a debt that
  drains with a 0.8 s time constant, and a jump taken with a debt f leaves the ground at (1 - 0.5 f) of the speed. Hops
  one after another settle near f 0.28, so A CHAIN OF HOPS FLIES AT 4.3 m/s, not 5: that is the speed gaps are sized for.
  CharacterController: radius 0.35, height 1.6, step 0.3, slope 45.
  LIANA LEAP (PaeteRules.VineRange 8 m from the chest, 1.3 m up): `PaeteVine.FindCatch` casts a ray at ANY solid collider.
  A catch 2.6 m or more above the feet, on a side or underside, with open air under and past it, is the UNDER swing
  (`PaeteSwing.Plan`): down to the bottom of the swing under the catch, 0.75 rad up the far side, thrown at 12.5 m/s
  (9.15 forward held 0.51 s, 8.52 up). `swing()` below is that plan and the throw, step for step.

ADJUSTMENTS TO THE COURSE'S NUMBERS (made here, never in the course file), each because the body cannot do the original:
  1. Way point 13 (end of the mill wheels) is 12 m up, not 15; way point 17 (end of the seesaws) is 31, not 36; the giant
     steps (leg 18) climb the 5 m the seesaws no longer do. A hop climbs 0.55 m at most, so 7 m needs 13 hops and the
     legs were too short for 13 stones. The geysers after them throw the difference (a geyser can throw any height).
  2. Way points 29 and 30 (the floating rocks' end, the crumbling walk) are 3.2 m up, not 8: rocks that float in a pool
     cannot climb 6 m. The last slide therefore drops 9 m, not 14.
  3. A swim leg ends 4 m further on, on a landing 0.3 m over the water, reached by a ramp out of the pool.
  4. A geyser stands at the END of its leg against the foot of the tier it throws onto (JumpPad throws straight up, and
     a body that left the ground standing drifts only about a metre), with a deck leading to it.
  5. A swing leg is several swings with a small island after each: one swing carries about 10 m past its catch. The
     "one long swing" legs keep ONE spur over one wide gap, with causeway either side.
  6. Stones are closer and more than the blockout drew (a row of 6 boulders 6.5 m apart cannot be hopped).
"""
import json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import paete_run_course as course
import paete_run_stages as stages

ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "PaeteRun", "paete_run_layout.json")
REPORT = os.path.join(ROOT, "Logs", "paete-run", "layout_report.txt")

# ------------------------------------------------------------------ the body
JUMP_V, GRAVITY, DT = 5.75, 20.0, 0.02
RUN, WALK, CAP = 5.0, 2.5, 6.0
HOP_FATIGUE = 0.28
HOP_SPEED = RUN * (1 - 0.5 * HOP_FATIGUE)          # 4.3
MAX_RISE = 0.55                                    # a hop's rise, of the 0.80 a jump reaches
VINE_RANGE, CHEST = 8.0, 1.3
LANE, CHUTE_BED = 8.0, 5.6
DISC = 5.0                                         # every way point's landing is a round platform this wide in radius
WANT = 0.15                                        # the margin every gap must keep at the chained hop's speed


def jump_range(speed, dh):
    """Metres flown before the feet come back down to `dh` over the take-off, by the motor's own steps. 0 if never."""
    y, vy, n, top = 0.0, JUMP_V, 0, 0.0
    while n < 400:
        y += vy * DT; n += 1
        top = max(top, y)
        if vy <= 0 and y <= dh: return speed * n * DT if top >= dh else 0.0
        vy -= GRAVITY * DT
    return 0.0


def swing(ay, land_dy, keys):
    """PaeteSwing.Plan (UNDER) for a catch `ay` over the feet, then the throw. Returns how far PAST THE CATCH the feet
    come down to `land_dy` (over the take-off), the release height, and the seconds from release to landing."""
    over = ay - CHEST
    rope = max(1.5, over * 0.65)
    ey = over - rope
    xr, yr = rope * math.sin(0.75), ey + rope * (1 - math.cos(0.75))
    speed = 12.5
    fy, fx = min(9.0, speed * math.sin(0.75)), speed * math.cos(0.75)
    hold = 1.2 * fy / GRAVITY
    x, y, vy, t, carry, handed = xr, yr, fy, 0.0, hold, False
    if yr + fy * fy / (2 * GRAVITY) < land_dy + 0.8: return None      # the throw does not clear the landing by 0.8 m
    first = True
    for _ in range(600):
        if first: vx = fx; first = False
        elif carry > 0:
            carry -= DT
            vx = fx if carry <= 0 else fx - 0.6 + (1.2 if keys else 0.0)
            if carry <= 0: handed = True
        else: vx = min(fx, CAP)
        x += vx * DT; y += vy * DT; t += DT
        if vy < 0 and y <= land_dy: return x, yr, t
        vy = max(-25.0, vy - GRAVITY * DT)
    return x, yr, t


# ------------------------------------------------------------------ vectors (Unity axes: y up)
def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def mul(a, k): return (a[0] * k, a[1] * k, a[2] * k)
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def length(a): return math.sqrt(dot(a, a))
def norm(a):
    n = length(a)
    return (a[0] / n, a[1] / n, a[2] / n) if n > 1e-9 else (0.0, 0.0, 1.0)
def flat(a): return (a[0], 0.0, a[2])
def flen(a, b): return math.hypot(a[0] - b[0], a[2] - b[2])
def side_of(f): return (f[2], 0.0, -f[0])           # right of the way it is going
def yaw_of(f): return math.degrees(math.atan2(f[0], f[2]))
def lerp(a, b, t): return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t)
def with_y(a, y): return (a[0], y, a[2])
UP = (0.0, 1.0, 0.0)
def V(a): return {"x": round(a[0], 3), "y": round(a[1], 3), "z": round(a[2], 3)}


SX, SY = course.START[0], course.START[1]
def U(p): return (-(p[0] - SX), float(p[2]), -(p[1] - SY))

# ------------------------------------------------------------------ the course, adjusted
KINDS = [leg[0] for leg in course.COURSE]
P = [U(course.START)] + [U(leg[2]) for leg in course.COURSE]
N_LEGS = len(KINDS)
def stage_of(i): return stages.STAGE.get(i, ("plain", ""))[0]
def words_of(i): return stages.STAGE.get(i, ("plain", ""))[1]

ADJUST = []
def set_height(j, y, why):
    ADJUST.append(f"way point {j}: height {P[j][1]:g} -> {y:g} ({why})")
    P[j] = with_y(P[j], y)

set_height(13, 12.0, "mill wheels climb 4 m, the geyser after them throws 13")
set_height(17, 31.0, "seesaws climb 4 m, the giant steps climb the other 5")
set_height(29, 3.2, "floating rocks stay in their pool")
set_height(30, 3.2, "the crumbling walk follows the rocks; the last slide drops 9 m")
WATER_UNDER = 0.4
WATER_NODES = {}                                   # way point -> surface height, for the three that are IN the water
EXIT_NODES = {}
for i, kind in enumerate(KINDS, start=1):
    if kind == "swim":
        surface = P[i][1] - WATER_UNDER
        f = norm(flat(sub(P[i], P[i - 1])))
        WATER_NODES[i - 1] = surface
        P[i] = add(P[i], mul(f, 4.0))
        EXIT_NODES[i] = surface
        ADJUST.append(f"way point {i}: moved 4 m on, to be the landing out of the pool (the water is {WATER_UNDER} m under the way points, as the art has it)")

solids, movers, water, pads, anchors, chutes, checkpoints, routes, GAPS, SWINGS, NOTES = [], [], [], [], [], [], [], {}, [], [], []


class Leg:
    def __init__(self, i, a, c, tag=None):
        self.i, self.A, self.C = i, a, c
        self.f = norm(flat(sub(c, a))); self.s = side_of(self.f)
        self.L = flen(a, c); self.rise = c[1] - a[1]
        self.tag = tag if tag is not None else str(i)
        self.route = []
    def at(self, dist, lat=0.0, y=None):
        p = add(add(self.A, mul(self.f, dist)), mul(self.s, lat))
        return with_y(p, self.A[1] if y is None else y)
    def yaw(self): return yaw_of(self.f)


def box(leg, name, kind, a, b, width, thick, lat=0.0, up=0.0, extra=0.0):
    """A box whose TOP face runs from a to b (they may differ in height), `width` across."""
    f3 = norm(sub(b, a)); fl = norm(flat(sub(b, a))); s = side_of(fl)
    n = (f3[1] * s[2] - f3[2] * s[1], f3[2] * s[0] - f3[0] * s[2], f3[0] * s[1] - f3[1] * s[0])
    if n[1] < 0: n = mul(n, -1)
    mid = add(add(lerp(a, b, 0.5), mul(s, lat)), mul(n, up - thick * 0.5))
    solids.append({"name": name, "kind": kind, "leg": leg.tag, "shape": "box", "c": V(mid),
                   "s": V((width, thick, length(sub(b, a)) + extra)), "yaw": round(yaw_of(fl), 3),
                   "pitch": round(-math.degrees(math.asin(max(-1, min(1, f3[1])))), 3), "r": 0, "h": 0})


def cyl(leg, name, kind, top, radius, height):
    solids.append({"name": name, "kind": kind, "leg": leg.tag, "shape": "cyl", "c": V(add(top, (0, -height * 0.5, 0))),
                   "s": V((radius * 2, height, radius * 2)), "yaw": 0, "pitch": 0, "r": round(radius, 3), "h": round(height, 3)})


def mover(leg, name, kind, shape, rest, size, motion, yaw=0.0, axis=UP, amp=0.0, period=4.0, phase=0.0, pivot=None, arm=0.0, r=0.0, h=0.0, offset=(0.0, 0.0, 0.0)):
    movers.append({"name": name, "kind": kind, "leg": leg.tag, "shape": shape, "c": V(rest), "s": V(size), "yaw": round(yaw, 3), "offset": V(offset),
                   "r": round(r, 3), "h": round(h, 3), "motion": motion, "axis": V(axis), "amp": round(amp, 4),
                   "period": round(period, 3), "phase": round(phase % 1.0, 4), "pivot": V(pivot if pivot else rest), "arm": round(arm, 3)})
    return name


def gap(leg, label, g, dh, speed=HOP_SPEED, want=WANT, timed=""):
    reach = jump_range(speed, dh)
    margin = (reach - g) / reach if reach > 0 else -1.0
    GAPS.append({"leg": leg.tag, "what": label, "gap": g, "dh": dh, "speed": speed, "range": reach, "margin": margin, "want": want, "timed": timed})
    return margin


def step(leg, act, p, **more):
    d = {"act": act, "p": V(p)}
    d.update(more)
    leg.route.append(d)


def disc_top(j): return with_y(P[j], P[j][1] - 0.02)


# ------------------------------------------------------------------ stones: every hop leg is a staircase of tops
def stones_plan(leg, r, lats, s0=DISC, s1=None, max_rise=MAX_RISE, sway=0.0, bob=0.0, min_n=1, want=WANT, speed=HOP_SPEED, r0=DISC, r1=DISC):
    """Centres of N round tops between the two landings, evenly stepped in height, N the fewest that keep every gap
    inside the body's reach with the margin. `lats` gives the sideways place of stone k. Returns [(centre, radius)]."""
    s1 = leg.L - DISC if s1 is None else s1
    radius = (lambda k: r[k % len(r)]) if isinstance(r, (list, tuple)) else (lambda k: r)
    for n in range(min_n, 60):
        dh = leg.rise / (n + 1)
        if dh > max_rise: continue
        span = s1 - s0
        total_r = sum(2 * radius(k) for k in range(n))
        g = (span - total_r) / (n + 1)
        if g < 0.25: return None
        pts, s = [], s0
        for k in range(n):
            s += g + radius(k)
            pts.append((leg.at(s, lats(k), leg.A[1] + dh * (k + 1)), radius(k)))
            s += radius(k)
        ok, prev, prev_r = True, with_y(leg.A, leg.A[1]), r0
        chain = pts + [(with_y(leg.C, leg.C[1]), r1)]
        for k, (c, rr) in enumerate(chain):
            moving = (0 if k == 0 else 1) + (0 if k == len(chain) - 1 else 1)
            d = max(0.0, flen(prev, c) + sway * moving - prev_r - rr)
            worst = (c[1] - prev[1]) + bob * moving
            reach = jump_range(speed, worst)
            if reach <= 0 or (reach - d) / reach < want: ok = False; break
            prev, prev_r = c, rr
        if ok: return pts
    return None


def report_stones(leg, label, pts, sway=0.0, bob=0.0, speed=HOP_SPEED, r0=DISC, r1=DISC, want=WANT):
    prev, prev_r = leg.A, r0
    chain = pts + [(leg.C, r1)]
    for k, (c, rr) in enumerate(chain):
        moving = (0 if k == 0 else 1) + (0 if k == len(chain) - 1 else 1)
        d = max(0.0, flen(prev, c) + sway * moving - prev_r - rr)
        gap(leg, f"{label} {k}->{k + 1}" + (" (worst of the motion)" if sway or bob else ""), d, (c[1] - prev[1]) + bob * moving, speed, want)
        prev, prev_r = c, rr


def need(pts, leg, what):
    if pts is None: raise SystemExit(f"leg {leg.tag}: no row of {what} fits the body. Change the leg's spacing.")
    return pts


# ------------------------------------------------------------------ swings
BEAM_UNDER, BEAM_THICK = 6.0, 1.0


def swing_reaches(land_dy):
    """Where the feet come down past the catch for the lowest and highest hold a beam offers, keys or none."""
    got = [swing(ay, land_dy, keys) for ay in (BEAM_UNDER - 0.25, BEAM_UNDER + 0.35, BEAM_UNDER + 0.9) for keys in (False, True)]
    if any(g is None for g in got): return None, None
    xs = [g[0] for g in got]
    return min(xs), max(xs)


def build_swings(leg, kind, s_start, s_end, r_end=DISC, island_extra=1.6, island_width=5.0, moving=False):
    """Swings from a take-off edge at `s_start` to a landing whose near edge is `s_end`. One beam and one island a
    swing; the last swing lands on the far landing. The spacing is solved so every landing is inside its island."""
    best = None
    for n in range(1, 6):
        dh = leg.rise / n
        lo, hi = swing_reaches(dh)
        if lo is None: continue
        mid = (lo + hi) * 0.5
        # take-off edge -> ax -> beam -> mid -> island centre; next take-off edge is the island's far edge.
        # n * (ax + mid) + (n - 1) * half = (s_end + 2.2) - s_start      (the last lands 2.2 m inside the far landing)
        # An island may be longer than it has to be (up to 5 m more) where the leg's length does not divide evenly.
        # And the first take-off may stand on a short causeway out from the landing (`lead`), for the same reason.
        for lead in [0.5 * q for q in range(0, 25)]:
            for more in [0.25 * q for q in range(0, 9)]:
                half = (hi - lo) * 0.5 + island_extra + more
                ax = ((s_end + 2.2 - s_start - lead) - (n - 1) * half) / n - mid
                # ⚠️ NO FURTHER THAN 4.2 m TO THE CATCH (play probe 1, 2026-10-10: on the two rows that had settled at 5.1 m the
                # vine did not take a body standing a step back from the edge; the cast was 8.2 m against a reach of 8).
                if 2.6 <= ax <= 4.2:
                    score = abs(ax - 3.4) + more * 0.6 + lead * 0.5 + n * 0.01
                    if best is None or score < best[0]: best = (score, n, ax, dh, lo, hi, half, mid, lead)
    if best is None: raise SystemExit(f"leg {leg.tag}: no row of swings fits {s_end - s_start:.1f} m with a rise of {leg.rise:.1f}")
    _, n, ax, dh, lo, hi, half, mid, lead = best
    if lead > 0:
        box(leg, f"lead_{leg.tag}", "sand", leg.at(s_start - 0.4, 0, leg.A[1]), leg.at(s_start + lead, 0, leg.A[1]), 5.0, 1.0)
        s_start += lead
    edge, y = s_start, leg.A[1]
    for k in range(n):
        beam_s = edge + ax
        under = y + BEAM_UNDER
        centre = leg.at(beam_s + 0.5, 0.0, under + BEAM_THICK * 0.5)
        aim = leg.at(beam_s + 0.05, 0.0, under + 0.3)
        name = f"anchor_{leg.tag}_{k}"
        reach_m = math.hypot(ax - 0.6, BEAM_UNDER + 0.3 - CHEST)
        m_name = ""
        if moving:
            m_name = mover(leg, name, "chain_anchor", "box", centre, (3.2, BEAM_THICK, 1.0), "sway", yaw=leg.yaw(), axis=leg.s,
                           amp=1.2, period=5.0 + 0.6 * k, phase=0.31 * k)
        else:
            solids.append({"name": name, "kind": "vine", "leg": leg.tag, "shape": "box", "c": V(centre), "s": V((7.0, BEAM_THICK, 1.0)),
                           "yaw": round(leg.yaw(), 3), "pitch": 0, "r": 0, "h": 0})
            for sd in (-1, 1):                         # the arch's legs, outside the lane
                foot = leg.at(beam_s + 0.5, sd * 4.0, under + BEAM_THICK)
                cyl(leg, f"arch_{leg.tag}_{k}_{'l' if sd < 0 else 'r'}", "post", foot, 0.45, BEAM_UNDER + BEAM_THICK + 6.0)
        anchors.append({"name": name, "leg": leg.tag, "kind": kind, "aim": V(aim), "c": V(centre), "mover": m_name,
                        "under": round(under, 3), "from": V(leg.at(edge - 0.6, 0.0, y))})
        land_s, land_y = beam_s + mid, y + dh
        last = k == n - 1
        SWINGS.append({"leg": leg.tag, "swing": k + 1, "of": n, "take_off_to_catch": ax, "cast_distance": reach_m, "rise": dh,
                       "lands_past_catch_min": lo, "lands_past_catch_max": hi,
                       "island": "the far landing" if last else f"{2 * half:.1f} m long",
                       "margin_near": (beam_s + lo) - (s_end if last else land_s - half),
                       "margin_far": ((s_end + 2 * r_end) if last else land_s + half) - (beam_s + hi)})
        step(leg, "run", leg.at(edge - 0.7, 0.0, y))
        step(leg, "vine", leg.at(land_s, 0.0, land_y), aim=V(aim), mover=m_name)
        if not last:
            a, b = leg.at(land_s - half, 0.0, land_y), leg.at(land_s + half, 0.0, land_y)
            box(leg, f"island_{leg.tag}_{k}", "rock", a, b, island_width, 1.4)
            cyl(leg, f"island_post_{leg.tag}_{k}", "post", leg.at(land_s, 0.0, land_y - 1.4), 0.9, 9.0)
            edge, y = land_s + half, land_y
    return n


# ------------------------------------------------------------------ the legs
def deck(leg, a_s, b_s, y=None, width=LANE, kind="sand", name=None, thick=1.0):
    y = leg.A[1] if y is None else y
    box(leg, name or f"deck_{leg.tag}_{len(solids)}", kind, leg.at(a_s, 0, y), leg.at(b_s, 0, y), width, thick)


def leg_plain(leg):
    deck(leg, 0.0, leg.L)
    step(leg, "run", leg.at(leg.L, 0, leg.C[1]))


def leg_boulders(leg):
    lats = lambda k: 0.5 if k % 2 else -0.5
    sway = 0.3
    pts = need(stones_plan(leg, 1.0, lats, sway=sway), leg, "boulders")
    for k, (c, r) in enumerate(pts):
        name = mover(leg, f"boulder_{leg.tag}_{k}", "boulder_chain", "cyl", add(c, (0, -0.8, 0)), (2 * r, 1.6, 2 * r), "sway",
                     axis=leg.s, amp=sway, period=3.4 + 0.25 * (k % 3), phase=0.37 * k, r=r, h=1.6)
        step(leg, "hop", c, r=r, mover=name)
    report_stones(leg, "boulder", pts, sway=sway)
    step(leg, "hop", leg.C, r=DISC, mover="")


def leg_beams(leg):
    s0, s1 = DISC - 0.3, leg.L - DISC + 0.3
    span = s1 - s0
    cut = s0 + span * 0.55
    y = leg.A[1]
    box(leg, f"beam_{leg.tag}_left", "wood", leg.at(s0, -2.8, y), leg.at(s1, -2.8, y), 0.9, 0.6)
    box(leg, f"beam_{leg.tag}_mid_a", "wood", leg.at(s0, 0, y), leg.at(cut, 0, y), 0.9, 0.6)
    box(leg, f"beam_{leg.tag}_mid_b", "wood", leg.at(cut + 2.0, 0, y), leg.at(s1, 0, y), 0.9, 0.6)
    box(leg, f"beam_{leg.tag}_right", "wood", leg.at(s0 + span * 0.25, 2.8, y), leg.at(s1, 2.8, y), 0.9, 0.6)
    gap(leg, "the broken middle beam", 2.0, 0.0, RUN * 0.93)
    gap(leg, "middle beam across to the right beam", 2.8 - 0.9, 0.0, RUN * 0.93)
    step(leg, "run", leg.at(cut - 0.35, 0, y))
    step(leg, "hop", leg.at(cut + 3.0, 0, y), r=0.9, mover="")
    step(leg, "run", leg.at(leg.L, 0, y))


def leg_pendulums(leg):
    y = leg.A[1]
    deck(leg, DISC - 0.3, leg.L - DISC + 0.3, y, width=3.0, name=f"ledge_{leg.tag}")
    n = 3
    for k in range(n):
        at = leg.at(DISC + (leg.L - 2 * DISC) * (k + 0.5) / n, 0, y)
        pivot = add(at, (0, 10.0, 0))
        mover(leg, f"pendulum_{leg.tag}_{k}", "pendulum", "sphere", add(at, (0, 1.8, 0)), (3.0, 3.0, 3.0), "pendulum", yaw=leg.yaw(),
              axis=leg.s, amp=0.75, period=3.6, phase=0.33 * k, pivot=pivot, arm=8.2, r=1.5)
    NOTES.append(f"leg {leg.tag}: three pendulums, 3.6 s a swing, a third of a swing apart; the ledge is 3 m wide and a boulder is over it for about 0.35 s of each pass")
    step(leg, "run", leg.at(leg.L, 0, y))


def leg_bridge(leg):
    y = leg.A[1]
    s0, s1 = DISC - 0.3, leg.L - DISC + 0.3
    span = s1 - s0
    cuts = [(0.24, 1.9), (0.52, 1.7), (0.80, 2.0)]
    at = s0
    for k, (u, g) in enumerate(cuts):
        end = s0 + span * u - g * 0.5
        deck(leg, at, end, y, width=5.0, name=f"bridge_{leg.tag}_{k}", thick=1.2)
        gap(leg, f"missing span {k + 1}", g, 0.0)
        step(leg, "run", leg.at(end - 0.35, 0, y))
        step(leg, "hop", leg.at(end + g + 1.3, 0, y), r=1.3, mover="")
        at = end + g
    deck(leg, at, s1, y, width=5.0, name=f"bridge_{leg.tag}_{len(cuts)}", thick=1.2)
    step(leg, "run", leg.at(leg.L, 0, y))


def leg_colonnade(leg):
    lats = lambda k: 0.0 if k % 4 == 2 else (0.6 if k % 2 else -0.6)
    pts = need(stones_plan(leg, [1.0, 1.0, 0.8, 1.0], lats), leg, "columns")
    for k, (c, r) in enumerate(pts):
        if k % 4 == 2:
            box(leg, f"fallen_{leg.tag}_{k}", "rock", add(c, mul(leg.f, -r)), add(c, mul(leg.f, r)), 7.0, 1.2)
            cyl(leg, f"fallen_post_{leg.tag}_{k}", "post", add(c, (0, -1.2, 0)), 0.5, 7.0)
        else:
            cyl(leg, f"column_{leg.tag}_{k}", "rock", c, r, 9.0)
        step(leg, "hop", c, r=r, mover="")
    report_stones(leg, "column", pts)
    step(leg, "hop", leg.C, r=DISC, mover="")


def leg_dive(leg):
    y = leg.A[1]
    surface = WATER_NODES[leg.i]
    box(leg, f"board_{leg.tag}", "wood", leg.at(DISC - 1.0, 0, y), leg.at(DISC + 6.5, 0, y), 3.0, 0.5)
    NOTES.append(f"leg {leg.tag}: a dive of {y - surface:.0f} m off a board 6.5 m long; the pool is under the whole of the fall (the swim caps the entry speed at 1.8 m/s, so the height costs nothing)")
    step(leg, "run", leg.at(DISC + 9.0, 0, y))
    step(leg, "swim", with_y(leg.C, surface - 0.9))


def pool(name, leg, back, ahead, width, surface, rims=True, rim_from=None):
    depth = 3.0
    a, b = leg.at(-back, 0, surface), leg.at(leg.L + ahead, 0, surface)
    mid = lerp(a, b, 0.5)
    ln = flen(a, b)
    water.append({"name": name, "leg": leg.tag, "c": V(with_y(mid, surface - depth * 0.5)), "s": V((width, depth, ln)),
                  "yaw": round(leg.yaw(), 3), "surface": round(surface, 3), "floor": round(surface - depth, 3)})
    box(leg, f"{name}_floor", "basin", with_y(a, surface - depth), with_y(b, surface - depth), width + 1.0, 1.0)
    if rims:
        ra = a if rim_from is None else leg.at(rim_from, 0, surface)
        for sd in (-1, 1):
            box(leg, f"{name}_rim_{'l' if sd < 0 else 'r'}", "basin", with_y(ra, surface + 0.6), with_y(b, surface + 0.6), 0.8, depth + 1.6,
                lat=sd * (width * 0.5 + 0.4))


def leg_swim(leg):
    surface = EXIT_NODES[leg.i]
    # The landing is the way point itself (already moved 4 m on); the ramp climbs out of the water to its edge.
    top_s = leg.L - DISC + 0.4
    box(leg, f"ramp_{leg.tag}", "sand", leg.at(top_s - 7.0, 0, surface - 1.9), leg.at(top_s, 0, leg.C[1] - 0.02), LANE, 0.6)
    step(leg, "swim", leg.at(top_s - 6.0, 0, surface - 0.9))
    step(leg, "run", leg.C)


def leg_wheels(leg):
    lats = lambda k: 1.0 if k % 2 else -1.0
    pts = need(stones_plan(leg, 1.0, lats), leg, "mill wheels")
    for k, (c, r) in enumerate(pts):
        name = mover(leg, f"wheel_{leg.tag}_{k}", "mill_wheel", "cyl", add(c, (0, -0.3, 0)), (2 * r, 0.6, 2 * r), "spin",
                     amp=(1 if k % 2 else -1), period=7.0, phase=0.13 * k, r=r, h=0.6)
        cyl(leg, f"wheel_post_{leg.tag}_{k}", "post", add(c, (0, -0.62, 0)), 0.4, 6.0)
        step(leg, "hop", c, r=r, mover=name)
    report_stones(leg, "wheel", pts)
    NOTES.append(f"leg {leg.tag}: {len(pts)} wheels of 1.0 m radius turning once in 7 s, alternate ways: a body on the rim is carried at 0.9 m/s")
    step(leg, "hop", leg.C, r=DISC, mover="")


def leg_geyser(leg):
    y, top = leg.A[1], leg.C[1]
    wall = leg.L - DISC
    deck(leg, 0.0, wall + 0.4, y, name=f"deck_{leg.tag}")
    cyl(leg, f"tier_{leg.tag}", "rock", disc_top(leg.i), DISC, top - y + 2.0)
    centre = leg.at(wall - 1.35, 0, y + 0.02)
    apex = (top - y) + 2.4
    speed = math.sqrt(2 * GRAVITY * apex) + 0.25
    above = 2 * math.sqrt(2 * 2.4 / GRAVITY)
    pads.append({"name": f"geyser_{leg.tag}", "leg": leg.tag, "position": V(centre), "radius": 1.2, "speed": round(speed, 2),
                 "rise": round(top - y, 2), "apex": round(apex, 2)})
    NOTES.append(f"leg {leg.tag}: geyser throws {speed:.1f} m/s for a tier {top - y:.1f} m up: the top of the arc is 2.4 m over the tier, "
                 f"{above:.2f} s above its edge; a body that left standing drifts 1.2 m/s, {1.2 * above:.2f} m, and needs 0.6 m (the pad's middle is 1.35 m from the wall)")
    step(leg, "pad", centre, to=V(leg.C))


def leg_swings(leg, kind):
    n = build_swings(leg, kind, DISC, leg.L - DISC, moving=(kind == "chain_swing"))
    step(leg, "run", leg.C)


def leg_long_swing(leg):
    y = leg.A[1]
    lo, hi = swing_reaches(leg.rise)
    ax = 3.6
    mid = (lo + hi) * 0.5
    take = max(DISC + 2.0, (leg.L - (ax + mid)) * 0.5)
    far = take + ax + lo - 1.6                      # the far causeway starts 1.6 m before the shortest landing
    deck(leg, 0.0, take, y, name=f"causeway_{leg.tag}_a", width=6.0)
    deck(leg, far, leg.L, leg.C[1], name=f"causeway_{leg.tag}_b", width=6.0)
    under = y + BEAM_UNDER
    centre = leg.at(take + ax + 0.5, 1.0, under + BEAM_THICK * 0.5)
    aim = leg.at(take + ax + 0.05, 0.0, under + 0.3)
    name = f"anchor_{leg.tag}_0"
    solids.append({"name": name, "kind": "vine", "leg": leg.tag, "shape": "box", "c": V(centre), "s": V((8.0, BEAM_THICK, 1.0)),
                   "yaw": round(leg.yaw(), 3), "pitch": 0, "r": 0, "h": 0})
    cyl(leg, f"spur_{leg.tag}", "post", leg.at(take + ax + 0.5, 5.4, under + BEAM_THICK + 1.0), 0.9, 22.0)
    anchors.append({"name": name, "leg": leg.tag, "kind": "long_swing", "aim": V(aim), "c": V(centre), "mover": "", "under": round(under, 3),
                    "from": V(leg.at(take - 0.6, 0, y))})
    SWINGS.append({"leg": leg.tag, "swing": 1, "of": 1, "take_off_to_catch": ax, "cast_distance": math.hypot(ax - 0.6, BEAM_UNDER + 0.3 - CHEST),
                   "rise": leg.rise, "lands_past_catch_min": lo, "lands_past_catch_max": hi, "island": f"the far causeway ({leg.L - far:.1f} m long)",
                   "margin_near": 1.6, "margin_far": leg.L + DISC - (take + ax + hi)})
    NOTES.append(f"leg {leg.tag}: ONE spur over a gap of {far - take:.1f} m (the leg is {leg.L:.1f} m; one swing carries {ax + lo:.1f} to {ax + hi:.1f} m)")
    step(leg, "run", leg.at(take - 0.7, 0, y))
    step(leg, "vine", leg.at(take + ax + mid, 0, leg.C[1]), aim=V(aim), mover="")
    step(leg, "run", leg.C)


def leg_logs(leg):
    y = leg.A[1]
    r, stand = 0.75, 0.75 * math.sin(math.radians(45))
    span = leg.L - 2 * DISC
    n = int(span / 3.0) - 1
    g = (span - n * 2 * stand) / (n + 1)
    s = DISC
    prev_edge = DISC
    for k in range(n):
        s += g + stand
        c = leg.at(s, 0, y)
        name = mover(leg, f"log_{leg.tag}_{k}", "log", "capsule", add(c, (0, -r, 0)), (8.5, 2 * r, 2 * r), "roll", yaw=leg.yaw(), axis=leg.s,
                     amp=(1 if k % 2 else -1), period=2 * math.pi * r / 1.0, phase=0.21 * k, r=r, h=8.5)
        gap(leg, f"log {k}", s - stand - prev_edge, 0.0)
        step(leg, "hop", c, r=stand, mover=name)
        s += stand
        prev_edge = s
    gap(leg, "last log to the landing", (leg.L - DISC) - prev_edge, 0.0)
    NOTES.append(f"leg {leg.tag}: {n} logs of 0.75 m radius, their tops turning at 1 m/s, alternate ways; a body stands on the middle 1.06 m of each")
    step(leg, "hop", leg.C, r=DISC, mover="")


def leg_seesaws(leg):
    half, tilt = 1.2, 6.0
    bob = half * math.sin(math.radians(tilt))
    lats = lambda k: 1.0 if k % 2 else -1.0
    pts = need(stones_plan(leg, half, lats, bob=bob, want=0.05), leg, "seesaws")
    for k, (c, r) in enumerate(pts):
        name = mover(leg, f"seesaw_{leg.tag}_{k}", "seesaw", "box", add(c, (0, -0.7, 0)), (3.6, 0.4, 2 * half), "tilt", yaw=leg.yaw(), axis=leg.s,
                     amp=tilt, period=3.6 + 0.3 * (k % 2), phase=0.29 * k, offset=(0.0, 0.5, 0.0))
        cyl(leg, f"seesaw_post_{leg.tag}_{k}", "post", add(c, (0, -0.45, 0)), 0.35, 6.0)
        step(leg, "hop", c, r=half, mover=name)
    report_stones(leg, "seesaw", pts, bob=bob, want=0.05)
    NOTES.append(f"leg {leg.tag}: the seesaws tip on a SLOW CLOCK (6 degrees each way, about 3.7 s), not under a body's weight: a slab's ends rise and fall {bob:.2f} m")
    step(leg, "hop", leg.C, r=DISC, mover="")


def leg_steps(leg):
    y0, y1 = leg.A[1], leg.C[1]
    s0, s1 = DISC - 0.3, leg.L - DISC + 0.3
    n = max(1, math.ceil((y1 - y0) / 0.5))
    d = (s1 - s0) / n
    riser = (y1 - y0) / n
    for k in range(n):
        top = y0 + riser * (k + 1) - (0.02 if k == n - 1 else 0.0)
        width = 7.2 if k % 2 == 0 else 5.2
        box(leg, f"giant_step_{leg.tag}_{k}", "rock", leg.at(s0 + d * k, 0, top), leg.at(s0 + d * (k + 1) + (0.3 if k == n - 1 else 0), 0, top), width, 4.0 + riser * k)
        step(leg, "hop", leg.at(s0 + d * (k + 0.55), 0, top), r=d * 0.45, mover="")
    gap(leg, f"each of {n} risers (treads {d:.2f} m deep)", 0.45, riser, speed=WALK)
    step(leg, "run", leg.C)


def leg_chute(leg, to_water):
    y0 = leg.A[1]
    s0 = DISC - 0.4
    s1 = leg.L - 1.0 if to_water else leg.L - DISC + 0.4
    y1 = (WATER_NODES[leg.i] + 0.8) if to_water else leg.C[1] - 0.02
    span = s1 - s0
    segs, amp = 12, 5.5
    # ⚠️ NO HOLE AND NO STEP IN A CHUTE (play probes 1 and 2, 2026-10-10; the owner, watching: "stuck again at leg 22"). Each
    # chute had a 3.2 m hole with a 1 m step down two thirds of the way along. The mode CARRIES a body down a chute only
    # while it is over a bed (`RunMode.OnChute`), so the carry ended at the hole and the body stopped there, both tries of
    # 40 s each. The bed is unbroken now. A jump in a slide wants the carry to span it first.
    drop_at, drop, hole = 7, 0.0, 0.0
    def centre(u):
        lat = amp * math.sin(2 * math.pi * u) * math.sin(math.pi * u)
        fall = (y0 - y1 - drop) * u + (drop if u > (drop_at / segs) - 1e-6 else 0.0)
        return leg.at(s0 + span * u, lat, y0 - fall)
    pts = []
    for k in range(segs):
        a, b = centre(k / segs), centre((k + 1) / segs)
        if k == drop_at:                                # the lip: this segment starts a body's length further on, a step down
            fl = norm(flat(sub(b, a)))
            a = add(a, mul(fl, hole))
            a = with_y(a, centre((k + 0.001) / segs)[1] - (hole / (span / segs)) * (centre(k / segs + 1e-3)[1] - b[1]) * 0.0)
        box(leg, f"chute_{leg.tag}_bed_{k}", "teal", a, b, CHUTE_BED, 0.6, extra=0.3)
        for sd in (-1, 1):
            box(leg, f"chute_{leg.tag}_wall_{k}_{'l' if sd < 0 else 'r'}", "teal", add(a, (0, 1.2, 0)), add(b, (0, 1.2, 0)), 0.5, 1.8,
                lat=sd * (CHUTE_BED * 0.5 + 0.25), extra=0.3)
        pts.append((a, b))
    line = []
    for a, b in pts:
        line.append(V(a)); line.append(V(b))
    slope = math.degrees(math.atan2(y0 - y1, span))
    chutes.append({"name": f"chute_{leg.tag}", "leg": leg.tag, "points": line, "width": CHUTE_BED, "speed": 13.0, "start_speed": 7.0,
                   "gain": 0.5, "above": 4.0, "below": 2.5})
    fall_t = math.sqrt(2 * drop / GRAVITY)
    NOTES.append(f"leg {leg.tag}: chute {span:.0f} m long, {y0 - y1:.1f} m down ({slope:.1f} degrees), bed {CHUTE_BED} m, in {segs} straight pieces that swing "
                 f"{amp * 0.77:.1f} m either side; one lip with a {hole} m hole and a {drop} m step down, crossed at the chute's 13 m/s without a jump "
                 f"({13.0 * fall_t:.1f} m flown while falling the step)")
    step(leg, "ride", centre(1.0))
    if to_water: step(leg, "swim", with_y(leg.C, WATER_NODES[leg.i] - 0.9))
    else: step(leg, "run", leg.C)


def leg_ledges(leg):
    wall = 4.1
    box(leg, f"cliff_{leg.tag}", "post", leg.at(2.0, 0, leg.A[1] + 6.0), leg.at(leg.L - 2.0, 0, leg.A[1] + 6.0), 1.0, 6.0 - leg.rise + 6.0, lat=wall + 0.5)
    s0, s1 = DISC, leg.L - DISC
    half = 0.8
    for n in range(4, 40):
        dh = leg.rise / (n + 1)
        g = ((s1 - s0) - 2 * half * n) / (n + 1)
        reach = jump_range(HOP_SPEED, dh)
        if g > 0.3 and reach > 0 and (reach - g) / reach >= WANT + 0.25: break
    s = s0
    for k in range(n):
        s += g + half
        width = 1.3 + (k % 3) * 0.9
        y = leg.A[1] + dh * (k + 1)
        c = leg.at(s, wall - width * 0.5, y)
        box(leg, f"ledge_{leg.tag}_{k}", "rock", add(c, mul(leg.f, -half)), add(c, mul(leg.f, half)), width, 0.5)
        gap(leg, f"ledge {k}", g, dh)
        step(leg, "hop", leg.at(s, wall - 0.65, y), r=min(half, 0.6), mover="")
        s += half
    gap(leg, "last ledge to the landing", g, dh)
    step(leg, "hop", leg.C, r=DISC, mover="")


def leg_rocks(leg):
    bob = 0.18
    lats = lambda k: 1.1 if k % 2 else -1.1
    pts = need(stones_plan(leg, 1.3, lats, bob=bob, min_n=5), leg, "floating rocks")
    for k, (c, r) in enumerate(pts):
        name = mover(leg, f"floating_rock_{leg.tag}_{k}", "floating_rock", "cyl", add(c, (0, -1.1, 0)), (2 * r, 2.2, 2 * r), "sway",
                     axis=UP, amp=bob, period=2.8 + 0.2 * (k % 3), phase=0.27 * k, r=r, h=2.2)
        step(leg, "hop", c, r=r, mover=name)
    report_stones(leg, "rock", pts, bob=bob)
    step(leg, "hop", leg.C, r=DISC, mover="")


def leg_crumble(leg):
    y = leg.A[1]
    s0, s1 = DISC - 0.3, leg.L - DISC + 0.3
    n = 5
    d = (s1 - s0) / n
    for k in range(n):
        c = leg.at(s0 + d * (k + 0.5), 0, y)
        mover(leg, f"crumble_{leg.tag}_{k}", "crumble_slab", "box", add(c, (0, -0.25, 0)), (5.0, 0.5, d - 0.06), "crumble", yaw=leg.yaw(),
              amp=0.7, period=4.5)
    NOTES.append(f"leg {leg.tag}: {n} slabs {d:.2f} m long, each falls 0.7 s after it is first stood on and is back 4.5 s later (PER PEER: each peer drops a slab when it sees a body on it)")
    step(leg, "run", leg.C)


def leg_drums(leg):
    radii = [1.5, 1.2, 1.6, 1.1]
    lats = lambda k: (1.2, -0.5, 0.6, -1.3)[k % 4]
    pts = need(stones_plan(leg, radii, lats), leg, "drums")
    wild = (0.0, 0.12, -0.10, 0.08)
    out = []
    for k, (c, r) in enumerate(pts):
        c = add(c, (0, wild[k % 4] if 0 < k < len(pts) - 1 else 0.0, 0))
        cyl(leg, f"drum_{leg.tag}_{k}", "rock", c, r, c[1] - (min(leg.A[1], leg.C[1]) - 5.0))
        step(leg, "hop", c, r=r, mover="")
        out.append((c, r))
    report_stones(leg, "drum", out)
    step(leg, "hop", leg.C, r=DISC, mover="")


def goat(leg, r_start, r_end):
    """The goat track: thin pillars, as FEW as the body can still hop with a small margin. It is the hard way."""
    lats = lambda k: 0.5 if k % 2 else -0.5
    pts = stones_plan(leg, 0.75, lats, s0=r_start, s1=leg.L - r_end, want=0.10, r0=r_start, r1=r_end, min_n=4)
    pts = need(pts, leg, "goat track pillars")
    for k, (c, r) in enumerate(pts):
        cyl(leg, f"goat_{leg.tag}_{k}", "rock", c, r, 10.0)
        step(leg, "hop", c, r=r, mover="")
    report_stones(leg, "pillar", pts, r0=r_start, r1=r_end, want=0.10)
    step(leg, "hop", leg.C, r=r_end, mover="")


def chimney(leg, r_start, r_end, buttress):
    """THE CHIMNEY: two tall steps a leg, each taken by the vine's OVER swing (a catch within 1.6 m under a lip he can
    stand on: up outside the face, over the lip and onto it, about 1.5 m in). An UNDER swing cannot climb this fast:
    its throw tops out about 4.4 m over the take-off. Every face here is a SOLID wall under its lip, or the game would
    read the lip as a beam and swing him under it."""
    y0, y1 = leg.A[1], leg.C[1]
    rise = (y1 - y0) * 0.5
    gap_m = 3.2
    a_s, b_s = r_start + gap_m, leg.L - r_end - gap_m - (0.3 if buttress else 0.0)
    top = y0 + rise
    box(leg, f"shelf_{leg.tag}", "rock", leg.at(a_s, 0, top), leg.at(b_s, 0, top), 5.0, rise + 6.0)
    far_face = leg.L - r_end - (0.3 if buttress else 0.0)
    if buttress:
        box(leg, f"buttress_{leg.tag}", "rock", leg.at(far_face, 0, y1 - 0.02), leg.at(far_face + 2.6, 0, y1 - 0.02), 6.0, rise + 6.0)
    for k, (edge, face, y_from, y_to) in enumerate(((r_start, a_s, y0, top), (b_s, far_face, top, y1))):
        aim = leg.at(face - 0.02, 0, y_to - 0.7)
        name = f"lip_{leg.tag}_{k}"
        cast = math.hypot(face - (edge - 0.7), (y_to - 0.7) - (y_from + CHEST))
        anchors.append({"name": name, "leg": leg.tag, "kind": "over", "aim": V(aim), "c": V(aim), "mover": "", "under": round(y_to, 3),
                        "from": V(leg.at(edge - 0.7, 0, y_from))})
        SWINGS.append({"leg": leg.tag, "swing": k + 1, "of": 2, "take_off_to_catch": face - (edge - 0.7), "cast_distance": cast, "rise": y_to - y_from,
                       "lands_past_catch_min": 1.2, "lands_past_catch_max": 1.8, "island": "a lip (the OVER swing puts him about 1.5 m in from it)",
                       "margin_near": 1.2, "margin_far": (b_s - a_s if k == 0 else 2 * r_end) - 1.8})
        step(leg, "run", leg.at(edge - 0.7, 0, y_from))
        step(leg, "vine", leg.at(face + 1.5, 0, y_to), aim=V(aim), mover="")
    step(leg, "run", leg.C)


BUILDERS = {
    "chained_boulders": leg_boulders, "three_beams": leg_beams, "pendulums": leg_pendulums, "gapped_bridge": leg_bridge,
    "colonnade": leg_colonnade, "dive": leg_dive, "swim": leg_swim, "mill_wheels": leg_wheels, "geyser": leg_geyser,
    "rolling_logs": leg_logs, "tilting_slabs": leg_seesaws, "stair_blocks": leg_steps, "wall_ledges": leg_ledges,
    "floating_rocks": leg_rocks, "crumble": leg_crumble, "drum_stones": leg_drums, "long_swing": leg_long_swing,
}

legs = []
for i in range(1, N_LEGS + 1):
    leg = Leg(i, P[i - 1], P[i])
    name, kind = stage_of(i), KINDS[i - 1]
    if name in ("arches", "chain_swing"): leg_swings(leg, name)
    elif name == "chute": leg_chute(leg, to_water=(i in WATER_NODES))
    elif name in BUILDERS: BUILDERS[name](leg)
    elif kind == "run" or name == "plain": leg_plain(leg)
    else: raise SystemExit(f"leg {i}: no builder for stage '{name}' ({kind})")
    legs.append(leg)
    routes[str(i)] = leg.route

# ---- the landings: a round platform at every way point that is not in the water
for j in range(0, N_LEGS + 1):
    if j in WATER_NODES: continue
    home = legs[min(j, N_LEGS - 1)]
    is_tier = j >= 1 and stage_of(j) == "geyser"
    is_tower = j < N_LEGS and KINDS[j] == "dive"
    if is_tier: continue                               # the geyser's tier IS the landing
    if j in EXIT_NODES: height = 3.6
    elif is_tower: height = P[j][1] - (P[j + 1][1] - 3.0) + 1.0
    else: height = 1.2
    cyl(Leg(j, P[j], add(P[j], (0, 0, 1)), tag=f"wp{j}"), f"landing_{j}", "sand", disc_top(j), DISC, height)

# ---- the start plaza (the game puts its bodies round the origin before the mode takes them) and the finish platform
first, last = legs[0], legs[-1]
solids.append({"name": "start_plaza", "kind": "sand", "leg": "start", "shape": "box", "c": V((2.0, -0.52, 0.0)), "s": V((26.0, 1.0, 26.0)),
               "yaw": 0, "pitch": 0, "r": 0, "h": 0})
box(last, "finish_platform", "sand", last.at(last.L - 1.0, 0, P[-1][1] - 0.02), last.at(last.L + 9.0, 0, P[-1][1] - 0.02), 10.0, 1.2)

# ---- the pools
def leg_n(i): return legs[i - 1]
pool("pool_1", leg_n(12), back=22.0, ahead=-4.0, width=26.0, surface=EXIT_NODES[12])
pool("pool_2", leg_n(28), back=30.0, ahead=-4.0, width=26.0, surface=EXIT_NODES[28])
rocks = leg_n(29)
pool("pool_2_rocks", rocks, back=3.0, ahead=-2.0, width=14.0, surface=EXIT_NODES[28], rim_from=12.0)
pool("pool_3", leg_n(32), back=9.0, ahead=-4.0, width=18.0, surface=EXIT_NODES[32])

# ------------------------------------------------------------------ shortcuts
shortcuts = []
SHORT_R = 3.0
for index, (name, words, after, before, sc_legs) in enumerate(course.SHORTCUTS):
    a_node, b_node = after, before - 1
    mids = [U(leg[2]) for leg in sc_legs[:-1]]
    chain_pts = [P[a_node]] + mids + [P[b_node]]
    ids = []
    for k in range(len(chain_pts) - 1):
        tag = f"s{index}.{k}"
        end_r = DISC if k == len(chain_pts) - 2 else SHORT_R
        sleg = Leg(100 + index * 10 + k, chain_pts[k], chain_pts[k + 1], tag=tag)
        kind = sc_legs[k][0]
        start_r = DISC if k == 0 else SHORT_R
        if name == "THE CHIMNEY":
            chimney(sleg, start_r, end_r, buttress=(k == len(chain_pts) - 2))
        elif kind == "swing":
            build_swings(sleg, "shortcut", start_r, sleg.L - end_r, r_end=end_r, island_extra=1.0, island_width=3.2)
            step(sleg, "run", sleg.C)
        else:
            goat(sleg, start_r, end_r)
        routes[tag] = sleg.route
        if k < len(chain_pts) - 2:
            cyl(sleg, f"shortcut_node_{index}_{k}", "sand", with_y(chain_pts[k + 1], chain_pts[k + 1][1] - 0.02), SHORT_R,
                (chain_pts[k + 1][1] - chain_pts[k][1]) * 0.5 + 6.0 if name == "THE CHIMNEY" else 1.2)
            ids.append(f"s{index}.{k}")
    shortcuts.append({"name": name, "words": words, "from": a_node, "to": b_node, "nodes": ids, "mid": [V(m) for m in mids]})

# ------------------------------------------------------------------ checkpoints: a graph, taken in order
progress = [0.0]
for leg in legs: progress.append(progress[-1] + leg.L)
node_ids = {}


def fall_under(a, b): return min(a[1], b[1]) - 7.0


for j in range(0, N_LEGS + 1):
    nxt = legs[j] if j < N_LEGS else legs[-1]
    in_water = j in WATER_NODES
    p = with_y(P[j], WATER_NODES[j] - 0.9) if in_water else add(P[j], (0, 0.08, 0))
    if j == 0: p = (0.0, 0.08, 0.0)
    nexts = [j + 1] if j < N_LEGS else []
    fall = fall_under(P[j], P[j + 1]) if j < N_LEGS else P[j][1] - 7.0
    checkpoints.append({"id": j, "name": ("START" if j == 0 else "FINISH" if j == N_LEGS else f"{j}"), "leg": j + 1 if j < N_LEGS else N_LEGS,
                        "stage": (words_of(j + 1) if j < N_LEGS else "the finish"), "p": V(p), "yaw": round(nxt.yaw(), 2),
                        "radius": 15.0 if in_water else DISC + 1.2,   # ⚠️ a pool is 26 m wide: at 8.5 a swimmer could pass a checkpoint
                        # without touching it (play probes 3 and 4: the second dive ended 14 m from its own and was never counted)
                        "half_height": 4.5, "fall_y": round(fall, 2), "next": nexts,
                        "progress": round(progress[j], 2), "main": j, "shortcut": -1, "water": in_water})
for index, sc in enumerate(shortcuts):
    a_node, b_node = sc["from"], sc["to"]
    chain_pts = [P[a_node]] + [(m["x"], m["y"], m["z"]) for m in sc["mid"]] + [P[b_node]]
    prev_id = a_node
    for k, m in enumerate(chain_pts[1:-1]):
        new_id = len(checkpoints)
        f = norm(flat(sub(chain_pts[k + 2], m)))
        u = (k + 1) / (len(chain_pts) - 1)
        checkpoints.append({"id": new_id, "name": sc["name"], "leg": a_node + 1, "stage": sc["words"], "p": V(add(m, (0, 0.08, 0))),
                            "yaw": round(yaw_of(f), 2), "radius": SHORT_R + 1.2, "half_height": 4.5,
                            "fall_y": round(fall_under(m, chain_pts[k + 2]), 2), "next": [],
                            "progress": round(progress[a_node] + (progress[b_node] - progress[a_node]) * u, 2), "main": a_node, "shortcut": index, "water": False})
        checkpoints[prev_id]["next"].append(new_id)
        checkpoints[prev_id]["fall_y"] = round(min(checkpoints[prev_id]["fall_y"], fall_under(chain_pts[k], m)), 2)
        prev_id = new_id
    checkpoints[prev_id]["next"].append(b_node)
    sc["checkpoints"] = [c["id"] for c in checkpoints if c["shortcut"] == index]

# ---- the start grid: 8 poses behind the line, two rows of four
grid = []
for row in range(2):
    for col in range(4):
        grid.append({"p": V(first.at(-(2.0 + 2.4 * row), (col - 1.5) * 2.0, 0.08)), "yaw": round(first.yaw(), 2)})

# ------------------------------------------------------------------ extents, bounds, kill height, review cameras
def extent(sd):
    c, s = sd["c"], sd["s"]
    half = math.hypot(s["x"], s["z"]) * 0.5
    return (c["x"] - half, c["x"] + half, c["y"] - s["y"] * 0.5 - 1, c["y"] + s["y"] * 0.5 + 1, c["z"] - half, c["z"] + half)

ex = [extent(sd) for sd in solids + movers]
min_x, max_x = min(e[0] for e in ex), max(e[1] for e in ex)
min_y, max_y = min(e[2] for e in ex), max(e[3] for e in ex)
min_z, max_z = min(e[4] for e in ex), max(e[5] for e in ex)
MARGIN = 30.0
bounds = {"min_x": round(min_x - MARGIN, 1), "max_x": round(max_x + MARGIN, 1), "min_z": round(min_z - MARGIN, 1), "max_z": round(max_z + MARGIN, 1),
          "floor": round(min_y - 4.0, 1), "ceiling": round(max_y + 40.0, 1)}
kill_y = round(min(c["fall_y"] for c in checkpoints) - 14.0, 1)
mid = ((min_x + max_x) * 0.5, (min_y + max_y) * 0.5, (min_z + max_z) * 0.5)
span_x, span_z = max_x - min_x, max_z - min_z
review = [
    {"name": "air_south", "at": V((mid[0] + span_x * 0.15, max_y + 120, max_z + span_z * 0.75)), "look": V(mid), "fov": 60},
    {"name": "air_north", "at": V((mid[0] - span_x * 0.15, max_y + 120, min_z - span_z * 0.75)), "look": V(mid), "fov": 60},
    {"name": "plan", "at": V((mid[0], max_y + 260, mid[2] - 0.01)), "look": V(mid), "fov": 62},
    {"name": "start", "at": V((6.0, 2.2, 3.0)), "look": V((-40.0, 2.0, -1.0)), "fov": 95},
]
for j, label in ((2, "boulders"), (5, "gauntlet"), (9, "colonnade"), (13, "wheels"), (17, "seesaws"), (22, "summit_slide"), (29, "floating"), (33, "drums")):
    leg = leg_n(j)
    review.append({"name": f"leg{j:02d}_{label}", "at": V(add(leg.at(-7.0, 5.0, leg.A[1] + 4.5), (0, 0, 0))),
                   "look": V(leg.at(leg.L * 0.55, 0, (leg.A[1] + leg.C[1]) * 0.5)), "fov": 80})

def footprints(sd):
    c, sz = sd["c"], sd["s"]
    if sd["shape"] != "box": return [(c["x"], c["z"], sz["x"] * 0.5, c["y"] - sz["y"] * 0.5, c["y"] + sz["y"] * 0.5)]
    yaw, pitch = math.radians(sd.get("yaw", 0)), math.radians(sd.get("pitch", 0))
    fx, fz, fy = math.sin(yaw) * math.cos(pitch), math.cos(yaw) * math.cos(pitch), -math.sin(pitch)
    n = max(1, int(math.ceil(sz["z"] / 2.0)))
    d = sz["z"] / n
    out = []
    for k in range(n):
        along = -sz["z"] * 0.5 + d * (k + 0.5)
        y = c["y"] + fy * along
        out.append((c["x"] + fx * along, c["z"] + fz * along, math.hypot(sz["x"] * 0.5, d * 0.5), y - sz["y"] * 0.5 - abs(fy) * d, y + sz["y"] * 0.5 + abs(fy) * d))
    return out


def leg_index(tag):
    if tag.startswith("wp"): return float(tag[2:]) + 0.5
    if tag == "start": return 0.5
    if tag.startswith("s"): return None
    return float(tag)


CLASH = {}
everything = [(sd, footprints(sd)) for sd in solids + movers if sd["kind"] not in ("post", "basin")]
for i in range(len(everything)):
    for j in range(i + 1, len(everything)):
        a_sd, a_fp = everything[i]; b_sd, b_fp = everything[j]
        if a_sd["leg"] == b_sd["leg"]: continue
        ia, ib = leg_index(a_sd["leg"]), leg_index(b_sd["leg"])
        if ia is not None and ib is not None and abs(ia - ib) <= 1.6: continue
        worst = None
        for (ax_, az_, ar, alo, ahi) in a_fp:
            for (bx_, bz_, br, blo, bhi) in b_fp:
                if math.hypot(ax_ - bx_, az_ - bz_) >= ar + br: continue
                clear = max(blo - ahi, alo - bhi)                  # air between them; negative: they overlap in height
                if clear < 2.4 and (worst is None or clear < worst): worst = clear
        if worst is not None:
            key = tuple(sorted((a_sd["leg"], b_sd["leg"])))
            if key not in CLASH or worst < CLASH[key][0]: CLASH[key] = (worst, a_sd["name"], b_sd["name"])

layout = {
    "offset_blender": [SX, SY, 0.0],
    "meta": {"made_by": "tools/author_paete_run_layout.py", "axes": "Unity: x and z on the flat, y up, metres; Blender (x, y, z) is (-x, z, -y) from the start line",
             "legs": N_LEGS, "length": round(progress[-1], 1), "adjustments": ADJUST},
    "body": {"jump_velocity": JUMP_V, "gravity": GRAVITY, "run": RUN, "walk": WALK, "hop_speed": round(HOP_SPEED, 3), "air_cap": CAP,
             "vine_range": VINE_RANGE, "chest": CHEST, "lane": LANE, "chute_bed": CHUTE_BED},
    "solids": solids, "movers": movers, "water": water, "pads": pads, "vine_anchors": anchors, "chutes": chutes,
    "checkpoints": checkpoints, "finish": N_LEGS, "start_grid": grid,
    "shortcuts": [{"name": s["name"], "words": s["words"], "from": s["from"], "to": s["to"], "checkpoints": s["checkpoints"], "legs": s["nodes"] + [f"s{i}.{len(s['nodes'])}"]}
                  for i, s in enumerate(shortcuts)],
    "kill_y": kill_y, "bounds": bounds, "review": review,
    "legs": [{"n": leg.i, "kind": KINDS[leg.i - 1], "stage": stage_of(leg.i), "words": words_of(leg.i), "a": V(leg.A), "c": V(leg.C),
              "length": round(leg.L, 2), "rise": round(leg.rise, 2)} for leg in legs],
    "routes": [{"leg": k, "steps": v} for k, v in routes.items()],
}

# ------------------------------------------------------------------ the report
lines = []
def say(text=""): lines.append(text)

say("PAETE'S RUN: GAMEPLAY LAYOUT REPORT")
say(f"{N_LEGS} legs, {progress[-1]:.0f} m; {len(solids)} solids, {len(movers)} movers, {len(water)} swim volumes, {len(pads)} geysers, "
    f"{len(anchors)} vine anchors, {len(chutes)} chutes, {len(checkpoints)} checkpoints ({N_LEGS + 1} on the course, {len(checkpoints) - N_LEGS - 1} on shortcuts)")
say(f"bounds x {bounds['min_x']} to {bounds['max_x']}, z {bounds['min_z']} to {bounds['max_z']}; lowest solid y {min_y:.1f}, highest {max_y:.1f}; kill height y {kill_y}")
say()
say("THE BODY (from the code): jump 5.75 m/s against gravity 20 (0.80 m up, 0.58 s); run 5.0 m/s with sprint held, walk 2.5; a chain of hops flies at "
    f"{HOP_SPEED:.2f} m/s (jump fatigue); a level jump covers {jump_range(RUN, 0):.2f} m at a run, {jump_range(HOP_SPEED, 0):.2f} m in a chain, "
    f"{jump_range(HOP_SPEED, 0.5):.2f} m rising 0.5 m, {jump_range(HOP_SPEED, -0.6):.2f} m dropping 0.6 m.")
lo0, hi0 = swing_reaches(0.0)
say(f"The vine: 8 m from the chest; a level UNDER swing on a beam {BEAM_UNDER:.0f} m overhead lands {lo0:.1f} to {hi0:.1f} m past the catch.")
say()
say("ADJUSTMENTS TO THE COURSE'S NUMBERS:")
for a in ADJUST: say("  - " + a)
say()
say("EVERY GAP (edge to edge, the worst the motion makes it; margin = share of the reach left over):")
bad = 0
for g in GAPS:
    flag = "" if g["margin"] >= g["want"] - 1e-6 else "   <-- UNDER THE MARGIN WANTED"
    if flag: bad += 1
    say(f"  leg {g['leg']:>5}  {g['what']:<44} gap {g['gap']:.2f} m  rise {g['dh']:+.2f} m  reach {g['range']:.2f} m at {g['speed']:.2f} m/s  margin {g['margin'] * 100:5.1f}%{flag}")
worst = min(GAPS, key=lambda g: g["margin"])
say(f"  {len(GAPS)} gaps; the tightest is leg {worst['leg']} {worst['what']}: {worst['margin'] * 100:.1f}%; {bad} under the margin wanted")
say()
say("EVERY SWING (metres; 'cast' is chest to the beam's near lower edge, the vine reaches 8):")
bad_swing = 0
for s in SWINGS:
    ok = s["cast_distance"] <= VINE_RANGE - 0.6 and s["margin_near"] >= 0.8 and s["margin_far"] >= 0.8
    if not ok: bad_swing += 1
    say(f"  leg {s['leg']:>5}  swing {s['swing']} of {s['of']}: take-off {s['take_off_to_catch']:.2f} m before the catch, cast {s['cast_distance']:.2f} m, rise {s['rise']:+.2f} m, "
        f"lands {s['lands_past_catch_min']:.1f} to {s['lands_past_catch_max']:.1f} m past it on {s['island']}; clear of its near edge by {s['margin_near']:.1f} m and its far edge by {s['margin_far']:.1f} m"
        + ("" if ok else "   <-- NOT CROSSABLE WITH MARGIN"))
say(f"  {len(SWINGS)} swings; {bad_swing} without margin")
say()
say("NOTES:")
for n in NOTES: say("  - " + n)
say()
say("LEGS THAT COME WITHIN 2.4 m OF ANOTHER LEG (a body is 1.6 m tall; negative is an overlap):")
if not CLASH: say("  none")
for key, (clear, na, nb) in sorted(CLASH.items(), key=lambda kv: kv[1][0]):
    say(f"  legs {key[0]} and {key[1]}: {clear:+.2f} m ({na} and {nb})")
say()
say("SHORTCUTS:")
for s in layout["shortcuts"]:
    say(f"  {s['name']}: leaves way point {s['from']}, rejoins at {s['to']}; checkpoints {s['checkpoints']}; legs {s['legs']}")

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as fh: json.dump(layout, fh, indent=1)
os.makedirs(os.path.dirname(REPORT), exist_ok=True)
with open(REPORT, "w", encoding="utf-8") as fh: fh.write("\n".join(lines) + "\n")
print("\n".join(lines))
print(f"\nwrote {OUT} ({os.path.getsize(OUT) // 1024} KB) and {REPORT}")
if bad or bad_swing: sys.exit(2)
