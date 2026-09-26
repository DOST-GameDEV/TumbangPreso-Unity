"""
A rough SKETCH of each hero's introduction stage for the pose sheets.

WHY: the stage is authored in C# (`HeroIntroductionScene.<Hero>.cs`) and this cloud machine
cannot run Unity, so without this the stage pieces were never seen next to the body. The sketch
copies each stage's main shapes, positions and timing (walls, moon, slabs, wave, lanterns...)
closely enough to judge COMPOSITION: is it in frame, is it behind the body, does it cover the
face. It is not the look: flat colour, fake transparency (pre-blended with the sky colour), no
glow. Every sheet says so. When a C# constant changes, change it here too.
"""

import math
import numpy as np

SKY = np.array([236, 226, 206]) / 255.0


def ease(a, b, t):
    u = min(1.0, max(0.0, (t - a) / (b - a))) if b > a else float(t >= b)
    return u * u * (3 - 2 * u)


class Sketch:
    def __init__(self):
        self.tris, self.cols = [], []

    def _add(self, tris, colour, alpha):
        if alpha <= .02 or len(tris) == 0:
            return
        c = np.array(colour[:3]) * alpha + SKY * (1 - alpha)
        self.tris.append(np.asarray(tris, float))
        self.cols.append(np.tile(c, (len(tris), 1)))

    def wall(self, bottom, top, colour, alpha, radius=8.0, sides=48, arc=(0, 360), centre=(0, 0, 0), lean=1.0):
        # Cut into ~1 m rows and narrow columns so the painter's depth sort stays honest.
        tris = []
        a0, a1 = arc
        cx, cy, cz = centre
        rows = max(1, int(math.ceil(top - bottom)))
        for i in range(sides):
            u0 = math.radians(a0 + (a1 - a0) * i / sides)
            u1 = math.radians(a0 + (a1 - a0) * (i + 1) / sides)
            p = [np.array([math.sin(u) * radius, 0, math.cos(u) * radius]) for u in (u0, u1)]
            for r in range(rows):
                f0, f1 = r / rows, (r + 1) / rows
                y0, y1 = bottom + (top - bottom) * f0, bottom + (top - bottom) * f1
                k0, k1 = 1 + (lean - 1) * f0, 1 + (lean - 1) * f1
                b0, b1 = p[0] * k0 + [cx, y0 + cy, cz], p[1] * k0 + [cx, y0 + cy, cz]
                t0, t1 = p[0] * k1 + [cx, y1 + cy, cz], p[1] * k1 + [cx, y1 + cy, cz]
                tris += [[b0, t0, b1], [b1, t0, t1]]
        if top > 10 and arc == (0, 360):
            # the lid, as in C#: closes the stage above every low shot
            for i in range(sides):
                u0 = math.radians(360 * i / sides); u1 = math.radians(360 * (i + 1) / sides)
                c0 = np.array([math.sin(u0) * radius * lean, top, math.cos(u0) * radius * lean])
                c1 = np.array([math.sin(u1) * radius * lean, top, math.cos(u1) * radius * lean])
                tris.append([np.array([0, top, 0]), c0, c1])
        self._add(tris, colour, alpha)

    def disc(self, centre, radius, facing, colour, alpha, sides=20, flat=False):
        centre = np.asarray(centre, float)
        if flat:
            u, v = np.array([1, 0, 0]), np.array([0, 0, 1])
        else:
            n = np.asarray(facing, float) - centre
            n /= np.linalg.norm(n)
            u = np.cross([0, 1, 0], n); u /= np.linalg.norm(u)
            v = np.cross(n, u)
        pts = [centre + (u * math.cos(k * 2 * math.pi / sides) + v * math.sin(k * 2 * math.pi / sides)) * radius for k in range(sides)]
        self._add([[centre, pts[k], pts[(k + 1) % sides]] for k in range(sides)], colour, alpha)

    def box(self, centre, size, colour, alpha, yaw=0.0, tilt=(0, 0, 0)):
        cx, cy, cz = centre
        sx, sy, sz = [s / 2 for s in size]
        c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        tx, tz = math.radians(tilt[0]), math.radians(tilt[2])

        def P(x, y, z):
            # tilt about x then z, then yaw
            y, z = y * math.cos(tx) - z * math.sin(tx), y * math.sin(tx) + z * math.cos(tx)
            x, y = x * math.cos(tz) - y * math.sin(tz), x * math.sin(tz) + y * math.cos(tz)
            return np.array([cx + x * c + z * s, cy + y, cz - x * s + z * c])
        v = [P(x, y, z) for x in (-sx, sx) for y in (-sy, sy) for z in (-sz, sz)]
        faces = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
        tris = []
        for a, b, cc, d in faces:
            tris += [[v[a], v[b], v[cc]], [v[a], v[cc], v[d]]]
        self._add(tris, colour, alpha)

    def line(self, p0, p1, width, colour, alpha):
        p0, p1 = np.asarray(p0, float), np.asarray(p1, float)
        d = p1 - p0
        length = np.linalg.norm(d)
        if length < 1e-4:
            return
        side = np.cross(d, [0, 1, 0])
        if np.linalg.norm(side) < 1e-4:
            side = np.array([1.0, 0, 0])
        side = side / np.linalg.norm(side) * width / 2
        up = np.cross(side, d); up = up / np.linalg.norm(up) * width / 2
        tris = []
        for off in (side, up):
            a, b, c, e = p0 - off, p0 + off, p1 + off, p1 - off
            tris += [[a, b, c], [a, c, e]]
        self._add(tris, colour, alpha)

    def result(self):
        if not self.tris:
            return np.zeros((0, 3, 3)), np.zeros((0, 3))
        return np.concatenate(self.tris), np.concatenate(self.cols)


def _leave(t, seconds):
    return 1 - ease(seconds - .45, seconds - .05, t)


def phaister(t, S, hands, seconds, cam):
    L = _leave(t, seconds)
    night = ease(1.0, 1.6, t) * L
    S.wall(0, 1.1, (.06, .02, .09), .88 * night)
    S.wall(1.1, 11, (.16, .06, .25), .84 * night)
    rise = ease(2.2, 2.75, t)
    moon = np.array([-4.2, 5.3 - (1 - rise) * .9, -5.4])
    face = cam
    S.disc(moon, 1.55 * (.7 + .3 * rise), face, (.95, .91, .8), rise * L)
    coil = ease(2.6, 3.35, t)
    if coil > 0:
        n = int(48 * coil)
        for k in range(n):
            a0, a1 = k * 2 * math.pi / 48, (k + 1) * 2 * math.pi / 48
            nrm = face - moon; nrm = nrm / np.linalg.norm(nrm)
            u = np.cross([0, 1, 0], nrm); u /= np.linalg.norm(u); v = np.cross(nrm, u)
            p0 = moon + nrm * .05 + (u * math.sin(a0) + v * math.cos(a0)) * 1.55 * 1.08
            p1 = moon + nrm * .05 + (u * math.sin(a1) + v * math.cos(a1)) * 1.55 * 1.08
            S.line(p0, p1, .12 + .2 * k / 48, (.1, .04, .16), .97 * L)
    swallow = ease(3.3, 3.6, t)
    S.disc(moon + (face - moon) / np.linalg.norm(face - moon) * .1, 1.55 * (.2 + .77 * swallow), face, (.035, .012, .075), swallow * L)
    claim = ease(3.42, 3.62, t)
    S.disc((0, .03, 0), .4 + 1.9 * claim, None, (.63, .23, .86), claim * L * .7, flat=True)


def sean(t, S, hands, seconds, cam):
    L = _leave(t, seconds)
    dusk = ease(.6, 1.3, t) * L
    S.wall(0, 1.0, (.14, .05, .03), .88 * dusk)
    S.wall(1.0, 2.3, (.96, .45, .12), .62 * dusk)
    S.wall(2.3, 11, (.21, .08, .05), .86 * dusk)
    seats = [(-2.6, .4, -4.2), (1.9, .2, -5.3), (-4.6, .7, -2.2), (3.9, .5, -2.9), (-.4, .1, -6.2), (5.2, .3, .6)]
    for i, s in enumerate(seats):
        born = .8 + i * .18
        age = max(0, t - born)
        at = np.array(s) + [0, age * (.55 + (i % 3) * .12), 0]
        S.disc(at, .25, cam, (1, .78, .3), ease(born, born + .4, t) * dusk)
    centre = (hands["left"] + hands["right"]) / 2 + [0, .12, .16]
    fill = ease(1.7, 1.95, t) * (1 - ease(2.95, 3.1, t))
    laid = ease(.85, 1.65, t)
    S.disc(centre, .23, cam, (1, .55, .12), max(laid * .6, fill) * L)


def zack(t, S, hands, seconds, cam):
    L = _leave(t, seconds)
    storm = ease(.45, .95, t) * L
    S.wall(0, 1.3, (.09, .09, .1), .86 * storm)
    S.wall(1.3, 11, (.24, .25, .26), .84 * storm)
    for i in range(16):
        a = math.radians(-160 + i * 21.3)
        h = 1.5 + (i * 53 % 7) * .28 + (.9 if i % 5 == 2 else 0)
        S.box((math.sin(a) * 7.5, h / 2, math.cos(a) * 7.5), (2.75, h, 1.2), (.07, .07, .08), .92 * storm, yaw=math.degrees(a))
    up = ease(.62, .72, t) * (1 - ease(1.12, 1.3, t))
    if up > .05:
        S.line(hands["right"], hands["right"] + [0, .9, 0], .03, (1, .82, .2), up * L)
    answered = ease(.78, .82, t) * (1 - ease(.9, 1.2, t))
    S.line((-3.6, 6.4, -5.8), (-3.6, 1.2, -5.8), .12, (1, .82, .2), answered * storm)
    sighting = ease(1.3, 1.45, t) * (1 - ease(1.8, 1.9, t))
    if sighting > .05:
        S.line(hands["right"], hands["right"] + [-.05, -.12, 5.5 * ease(1.3, 1.6, t)], .035, (1, .82, .2), sighting * L)
    drop = ease(1.86, 1.9, t) * (1 - ease(2.05, 2.5, t))
    S.line((.8, 13, -2.8), (.8, 0, -2.8), .16, (1, .82, .2), drop * storm)


def nemu(t, S, hands, seconds, cam):
    L = _leave(t, seconds)
    bob = math.sin(t * 2.4) * .06
    at = np.array([-.95, .65 + bob, .15])
    hand = hands["left"] + [-.2, .12, .12]
    at = at + (np.array([-.55, .95, .25]) - at) * ease(.52, .7, t) * (1 - ease(.78, .98, t))
    at = at + (hand - at) * ease(1.2, 1.45, t) * (1 - ease(1.72, 1.95, t))
    at = at + (np.array([-.75, .8, -.35]) - at) * ease(1.8, 2.1, t)
    amount = ease(2.2, 3.2, t)
    at = at + (np.array([-1.55, 0, -.2]) - at) * amount
    size = .35 * (1 + 6.4 * amount)
    S.box(at + [0, size / 2, 0], (size, size, size * .8), (.12, .1, .16), .95)
    climb = ease(2.05, 2.9, t)
    if climb > 0:
        S.wall(0, max(.01, climb * 11), (.05, .02, .08), .9 * ease(2.05, 2.2, t) * L)
    S.disc((-1.2, .02, -.2), .3 + 6.2 * ease(1.88, 2.7, t), None, (.04, .01, .06), ease(1.88, 2.4, t) * L * .9, flat=True)


def dante(t, S, hands, seconds, cam):
    L = _leave(t, seconds)
    haze = ease(.3, 1.1, t) * L
    S.wall(0, 1.2, (.2, .14, .09), .8 * haze)
    S.wall(1.2, 11, (.42, .32, .22), .72 * haze)
    for i in range(9):
        a = math.radians(180 - 80 + i * 20)
        h = (1.6 + (i * 37 % 5) * .45) * ease(.9 + i * .04, 1.6 + i * .04, t)
        S.box((math.sin(a) * 7.1, h / 2, math.cos(a) * 7.1), (2.2, max(.01, h), 1.8), (.24, .19, .15), haze)
    surface, part = ease(1.0, 1.7, t), ease(1.8, 2.4, t)
    for side in (-1, 1):
        x = side * (1.15 + 1.35 * part)
        y = -3.2 + 3.2 * surface
        S.box((x, y + 1.55, -3.1), (2.2, 3.1, 1.5), (.36, .3, .25), 1 if haze > .01 else 0, yaw=side * 12, tilt=(0, 0, -side * (2 + 11 * part)))
    crack = ease(3.16, 3.4, t)
    S.box((0, .03, 1.9), (1.2, .02, 5.2 * max(.08, crack)), (1, .5, .12), crack * L)


def cheska(t, S, hands, seconds, cam):
    L = _leave(t, seconds)
    mist = ease(.7, 1.4, t) * L
    S.wall(0, 1.4, (.6, .66, .64), .55 * mist)
    S.wall(1.4, 11, (.8, .86, .88), .5 * mist)
    for i in range(10):
        a = math.radians(180 - 95 + i * 21)
        h = (2.1 + (i * 41 % 5) * .3) * ease(.8 + i * .03, 1.3 + i * .03, t)
        S.box((math.sin(a) * 7.2, h / 2, math.cos(a) * 7.2), (.9, max(.01, h), .9), (.26, .34, .31), 1 if mist > .01 else 0)
    held = False
    gather = (hands["left"] + hands["right"]) / 2 + [0, .1, .12]
    form, snap = ease(1.45, 1.95, t), ease(2.88, 2.92, t)
    S.box(gather, (.18 * form, .4 * form, .18 * form), (.7, .94, 1), form * (1 - snap) * L)
    spread = ease(2.1, 2.9, t)
    S.disc((0, .02, 0), .3 + 2.0 * spread, None, (.78, .95, 1), spread * L * .8, flat=True)
    for i in range(9):
        a = math.radians(180 - 90 + i * 22.5)
        r = ease(2.15 + i * .05, 2.5 + i * .05, t)
        h = (1.3 + (i % 3) * .35) * r
        S.box((math.sin(a) * 5.6, h / 2, math.cos(a) * 5.6), (.5, max(.01, h), .5), (.55, .85, 1), r * L * .85)


def rafi(t, S, hands, seconds, cam):
    L = _leave(t, seconds)
    sea = ease(.85, 1.5, t) * L
    S.wall(0, 1.0, (.04, .22, .25), .88 * sea)
    S.wall(1.0, 1.07, (.9, .97, .95), .85 * sea)
    S.wall(1.07, 11, (.55, .78, .76), .6 * sea)
    for i in range(4):
        a = math.radians(180 - 55 + i * 34)
        base = np.array([math.sin(a) * 7.3, 0, math.cos(a) * 7.3])
        across = np.array([math.cos(a), 0, -math.sin(a)]) * .45
        for off in (-across, across):
            S.box(base + off + [0, .65, 0], (.12, 1.3, .12), (.08, .09, .09), 1 if sea > .01 else 0)
        S.box(base + [0, 1.6, 0], (1.3, .6, 1.0), (.1, .12, .12), 1 if sea > .01 else 0, yaw=math.degrees(a))
        S.box(base + [0, 2.05, 0], (1.1, .3, .9), (.08, .09, .09), 1 if sea > .01 else 0, yaw=math.degrees(a))
    climb, pitch = ease(2.0, 2.75, t), ease(2.88, 3.2, t)
    height = .05 + 2.65 * climb
    S.wall(0, height, (.16, .55, .62), .78 * climb * L * (1 - pitch * .35), radius=2.4, sides=12, arc=(120, 240),
           centre=(0, 0, -.2 + pitch * .8), lean=.82)


# PAETE v4 (direction.md section 5.13): no stage walls at all; the court is the stage. Constants mirror
# `HeroIntroductionScene.Paete.cs` (her stand behind his RIGHT shoulder, the landing 5.5 m ahead). v5, direction.md 5.14.
PAETE_HER = np.array([1.0, 0.0, -1.2])
PAETE_HER_SCALE = 1.1
PAETE_LANDING = np.array([0.0, 0.0, 5.5])
PAETE_TREE_YAW = 105.0
# Her meadow's plants (the glb's ground points, `tools/build_paete_props.py` meadow()), for the sketch: where and how big.
PAETE_MEADOW = [((0.78, -1.30), 1.3), ((0.10, 0.10), .8), ((1.62, 0.92), .8), ((-0.90, -0.84), .7), ((1.92, -2.20), .9),
                ((0.34, 1.62), .7), ((1.30, 0.40), .35), ((1.92, 1.62), .35), ((2.62, 0.18), .35), ((-1.28, -0.18), .35),
                ((0.10, -2.30), .4), ((1.72, -1.88), .45), ((1.48, 1.30), .3), ((2.52, -1.92), .3), ((2.10, 1.00), .25)]


def _paete_her(t):
    """Her offset, lean, presence and how SOLID she is at t (the same curves as the C# driver). ⚠️ For the ROOT shot she
    comes in close BEHIND him (the owner's note on v4: she filled the left of that frame), her hands over his shoulders."""
    rise = ease(.03, .45, t)
    solid = ease(.30, .50, t) * (1 - ease(.82, 1.08, t))
    close = ease(1.0, 1.3, t) * (1 - ease(2.3, 2.7, t))
    fade = ease(3.6, 4.4, t)
    off = np.array([-.95 * close, -1.2 * PAETE_HER_SCALE * (1 - rise), .05 * close])
    lean = 10 + 8 * ease(.25, .55, t) + 6 * close
    return off, lean, rise * (1 - fade), solid


def _tree_up(t):
    """The guardian's CRAWL (direction.md 5.14), as the fraction of its height out of the court: three hauls, pauses between."""
    s = t - 2.75
    return .35 * ease(.40, .70, s) + .35 * ease(.80, 1.10, s) + .30 * ease(1.20, 1.50, s)


def paete(t, S, hands, seconds, cam):
    off, lean, alpha, solid = _paete_her(t)
    base = PAETE_HER + off
    k = PAETE_HER_SCALE
    green = (.55, .95, .65)
    def lean_pt(p):
        y, z = p[1] - base[1], p[2] - base[2]
        a = math.radians(lean)
        return np.array([p[0], base[1] + y * math.cos(a), base[2] + z * math.cos(a) + y * math.sin(a)])
    # HER: a jade ghost, or in her own colours (cream camisa, dark hair) while she is FORMED (0.30 to 1.08).
    def tint(c):
        return tuple(np.array(green) * (1 - solid) + np.array(c) * solid)
    if alpha > .02:
        a_ = alpha * (.45 + .55 * solid)
        for y0, y1, w in ((0, .6, 1.3), (.6, 1.2, .9), (1.2, 1.56, .52)):
            c = lean_pt(base + np.array([0, (y0 + y1) / 2 * k, 0]))
            S.box(c, (w * k, (y1 - y0) * k, w * .8 * k), tint((.92, .9, .84)), a_, tilt=(lean, 0, 0))
        S.box(lean_pt(base + np.array([0, 1.86 * k, 0])), (.42 * k, .6 * k, .3 * k), tint((.9, .88, .8)), a_, tilt=(lean, 0, 0))
        S.box(lean_pt(base + np.array([0, 2.5 * k, .02])), (.37 * k, .44 * k, .38 * k), tint((.78, .56, .41)), a_, tilt=(lean, 0, 0))
        S.box(lean_pt(base + np.array([0, 1.6 * k, -.25 * k])), (.4 * k, 1.9 * k, .1 * k), tint((.14, .1, .09)), a_, tilt=(lean, 0, 0))
    palms = (hands["left"] + hands["right"]) / 2
    her_hands = lean_pt(base + np.array([0, 2.0 * k, .36 * k]))
    # THE LIGHT: in her hands, dropped into his left hand (0.58 to 0.70), into him at 0.78.
    if .1 < t < .58:
        S.box(her_hands, (.12, .12, .12), (1, 1, .7), 1)
    elif .58 <= t < .8:
        u = ease(.58, .70, t)
        at = her_hands + (hands["left"] - her_hands) * u + np.array([0, .4 * math.sin(u * math.pi), 0])
        S.box(at, (.12, .12, .12), (1, 1, .7), 1)
    # HER MEADOW: out from her as a wave (3 m/s from 0.1), wilting from the outer edge in (3.6 to 4.5).
    for (x, z), r in PAETE_MEADOW:
        d = math.hypot(x - PAETE_HER[0], z - PAETE_HER[2])
        grow = ease(.1 + d / 3.0, .1 + d / 3.0 + .35, t)
        wilt = ease(3.6 + (3.4 - d) * .15, 3.9 + (3.4 - d) * .15, t)
        if grow * (1 - wilt) > .02:
            S.disc((x, .01, z), r * grow * (1 - .6 * wilt), (0, 1, 0), (.36, .6, .22), 1, flat=True)
    # HIS ROOTS INTO THE COURT (1.25 to 1.6), held through the channel and the rise.
    dig = ease(1.25, 1.6, t)
    if dig > 0:
        for hand in (hands["left"], hands["right"]):
            for j, (dx, dz) in enumerate(((.25, .2), (-.2, .25), (.05, -.25), (.3, -.05))):
                tip = hand + np.array([dx, -hand[1] - .05, dz]) * dig
                S.line(hand, tip, .05, (.36, .25, .15), 1)
    # THE CHANNEL GLOW (1.6 to 2.4): lime on his arms and a ring pulsing out round his hands.
    glow = ease(1.55, 1.75, t) * (1 - ease(2.6, 3.0, t))
    if glow > .05:
        for hand in (hands["left"], hands["right"]):
            S.box(hand, (.16, .16, .16), (.85, 1, .4), glow)
        for at in (1.72, 1.96, 2.14):
            u = (t - at) / .35
            if 0 < u < 1:
                S.disc((palms[0], .02, palms[2]), .3 + 1.6 * u, (0, 1, 0), (.85, 1, .4), 1 - u, flat=True)
    # THE TRAVEL (2.3 to 2.75): three ridges from his hands to the spot: heaved court, a lit crack, no light at the front.
    reach = ease(2.3, 2.75, t) * (1 - ease(3.2, 3.6, t))
    start = np.array([palms[0], .02, max(.5, palms[2])])
    for v, amp in ((0, .45), (1, -.35), (2, .1)):
        prev = start
        for i in range(1, int(24 * reach) + 1):
            u = i / 24
            side = amp * math.sin(u * math.pi) + .12 * math.sin(u * 9 + v * 2) * math.sin(u * math.pi)
            p = start + (PAETE_LANDING - start) * u + np.array([side, .05 * abs(math.sin(u * 18 + v)), 0])
            S.line(prev, p, .07, (.85, 1, .4), 1)
            if i % 4 == 0:
                S.box(p + np.array([0, .06, 0]), (.22, .1, .18), (.45, .36, .26), 1, yaw=30 * i)
            prev = p
    # THE GUARDIAN CRAWLS OUT (from 2.75): the court bulges, six claws break out and grab, three hauls, crown, eyes at 4.5.
    s = t - 2.75
    if s > 0:
        bulge = ease(0, .15, s)
        S.box(PAETE_LANDING + np.array([0, .12 * bulge, 0]), (3.4, .3 * bulge + .01, 3.4), (.3, .22, .14), 1)
        for j in range(6):
            a = math.radians(j * 60 + 20)
            claw = ease(.05 + .06 * j, .25 + .06 * j, s)
            if claw > 0:
                d = np.array([math.sin(a), 0, math.cos(a)])
                S.line(PAETE_LANDING + d * .9 + np.array([0, .9 * claw, 0]), PAETE_LANDING + d * (.9 + 1.4 * claw), .28, (.3, .21, .13), 1)
        up = _tree_up(t)
        if up > 0:
            sink = (1 - up) * 9.0
            a = math.radians(PAETE_TREE_YAW)
            fwd = np.array([math.sin(a), 0, math.cos(a)])
            S.box(PAETE_LANDING + np.array([0, 2.4 - sink, 0]), (1.6, 4.8, 1.6), (.36, .25, .15), 1, yaw=PAETE_TREE_YAW)
            crown = ease(1.35, 1.7, s)
            S.box(PAETE_LANDING + np.array([0, 6.6 - sink, 0]), (1.6 + 2.6 * crown, 3.6, 1.6 + 2.6 * crown), (.3, .5, .2), 1, yaw=PAETE_TREE_YAW)
            if s > 1.75:
                side = np.array([fwd[2], 0, -fwd[0]])
                for s_ in (-1, 1):
                    S.box(PAETE_LANDING + fwd * .82 + side * s_ * .3 + np.array([0, 4.3 - sink, 0]), (.22, .12, .1), (.9, 1, .45), 1, yaw=PAETE_TREE_YAW)


STAGES = dict(phaister=phaister, sean=sean, zack=zack, nemu=nemu, dante=dante, cheska=cheska, rafi=rafi, paete=paete)
