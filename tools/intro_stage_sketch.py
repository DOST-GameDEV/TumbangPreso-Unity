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


STAGES = dict(phaister=phaister, sean=sean, zack=zack, nemu=nemu, dante=dante, cheska=cheska, rafi=rafi)
