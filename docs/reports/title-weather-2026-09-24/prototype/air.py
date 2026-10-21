"""Prototype of the title street's weather, new design vs current, in source pixels.

Everything here is a function of time so the Unity port can be a transcription.
"""
import math
import numpy as np
import cv2
from PIL import Image
from scipy import ndimage

import pathlib
ART = str(pathlib.Path(__file__).resolve().parents[4] / "Assets/TumbangPreso/Resources/UI/owner-menu-edits") + "/"

def load(name, mode=None):
    im = Image.open(ART + name)
    if mode: im = im.convert(mode)
    return np.asarray(im).astype(np.float32) / 255.0

PLATE = load("main2-background.png", "RGB")
SKY = load("main2-sky-mask.png", "L")
CLOUD = load("main2-cloud.png", "RGBA")
SHADOW = load("main2-shadow.png", "L")
LEAF = load("main2-leaf.png", "RGBA")
GROUND = load("main-ground-mask.png", "RGBA")[..., 3]
SKY_COLOUR = np.array([136, 200, 119], np.float32) / 255
SHADOW_TINT = np.array([.7079, .665, 1.0149], np.float32)
# a very soft version of her cloud, standing in for tex2Dlod at a high mip
CLOUD_SOFT = cv2.resize(cv2.resize(CLOUD[..., 3], (17, 9), interpolation=cv2.INTER_AREA), (532, 269), interpolation=cv2.INTER_CUBIC)
CLOUD_SOFT = np.clip(CLOUD_SOFT, 0, 1)

def smooth(a, b, x):
    t = min(max((x - a) / (b - a), 0.0), 1.0)
    return t * t * (3 - 2 * t)

def h(*keys):
    """PCG hash of integer keys, 0..1. Same arithmetic in C#."""
    n = 0x9E3779B9
    for k in keys:
        n = (n ^ (k & 0xffffffff)) & 0xffffffff
        n = (n * 747796405 + 2891336453) & 0xffffffff
        w = (((n >> ((n >> 28) + 4)) ^ n) * 277803737) & 0xffffffff
        n = ((w >> 22) ^ w) & 0xffffffff
    return n / 4294967295.0

# ---------------------------------------------------------------- the breeze
GUST_PERIOD = 31.0
def gust(t):
    """0 in the calm, up to 1 at the top of a gust. One every ~31 s, never on a beat."""
    k = math.floor(t / GUST_PERIOD)
    g = 0.0
    for j in (k - 1, k, k + 1):
        start = j * GUST_PERIOD + (h(j, 11) * 0.6 + 0.2) * GUST_PERIOD
        d = t - start
        if d < 0: continue
        rise, hold, fall = 2.6, 1.6, 7.0
        if d < rise: e = smooth(0, 1, d / rise)
        elif d < rise + hold: e = 1.0
        elif d < rise + hold + fall: e = 1 - smooth(0, 1, (d - rise - hold) / fall)
        else: e = 0.0
        g = max(g, e * (0.65 + 0.35 * h(j, 12)))
    return g

# ---------------------------------------------------------------- the ground
def ground_left(y):
    if y < 644: return np.interp(y, [563, 644], [1104, 987])
    if y < 679: return np.interp(y, [644, 679], [987, 817])
    if y < 697: return np.interp(y, [679, 697], [817, 545])
    if y < 735: return np.interp(y, [697, 735], [545, 0])
    return np.interp(y, [735, 810], [0, -160])
def ground_right(y): return np.interp(y, [565, 810], [1730, 2050])

def sun(x, y):
    xi = int(min(max(x, 0), 1919)); yi = int(min(max(y, 0), 1079))
    return 1.0 - float(SHADOW[yi, xi])

# ---------------------------------------------------------------- air shader
CLOUD_W, CLOUD_H = 532.0, 269.0
DIST_REST, DIST_GAP = 1300.0, 150.0

class AirState:
    pass

def air_params(t, reduced=False, old=False):
    s = AirState()
    s.t = t
    span = 1920 + CLOUD_W
    if old:
        s.near = (1243 if reduced else ((t * 8.4 + 1243 + CLOUD_W) % span) - CLOUD_W, 28, CLOUD_W, CLOUD_H)
        s.far = (1243 if reduced else ((t * 4.4 + 780 + CLOUD_W) % span) - CLOUD_W, -10, CLOUD_W * .7, CLOUD_H * .7)
        s.mid = (0, 0, 1, 1); s.fade = (1, .8, 0)
        s.sway = 0 if reduced else math.sin(t * 2 * math.pi / 74) * 46
        s.rise = 0 if reduced else math.sin(t * 2 * math.pi / 101) * 13
        s.weight = 1 if reduced else .9 + .1 * math.sin(t * 2 * math.pi / 43)
        s.rustle = 0; s.gust = 0; s.churn = 0; s.shade = None
        return s
    g = 0 if reduced else gust(t)
    s.gust = g
    # clouds cross right to left with the breeze; his speeds, his 2:1 depth ratio
    # each bank wraps just outside her sky opening (x 975 to 1800), behind the roof on the
    # left and the canopy on the right, so the wrap is never seen. Five instances of her one
    # mass: near once, mid and far twice each, half a span apart.
    def bank(rest, speed, span):
        return 1800 - ((1800 - rest + speed * t) % span)
    H = CLOUD_H
    if reduced:
        s.clouds = [((1243, -10, CLOUD_W * .7, H * .7), .8, False, 1.3), ((1243, 28, CLOUD_W, H), 1.0, False, 0.0)]
    else:
        s.clouds = [
            ((bank(593, 4.4, 1300), 300 - H * .7, CLOUD_W * .7, H * .7), .8, False, 1.3),
            ((bank(-57, 4.4, 1300), 300 - H * .7, CLOUD_W * .7, H * .7), .8, True, 5.2),
            ((bank(275.5, 6.3, 1290), 300 - H * .84, CLOUD_W * .84, H * .84), .9, True, 4.1),
            ((bank(-369.5, 6.3, 1290), 300 - H * .84, CLOUD_W * .84, H * .84), .9, False, 2.6),
            ((bank(1243, 8.4, 1370), 28, CLOUD_W, H), 1.0, False, 0.0)]
    s.churn = 0 if reduced else 1
    # shadow: anchored, rustling. A small slow sway is kept so the whole pattern breathes.
    s.sway = 0  # removed: the fold of the mirror showed as a crease at the wall
    s.rise = 0 if reduced else math.sin(t * 2 * math.pi / 101) * 3
    s.weight = 1 if reduced else .93 + .07 * math.sin(t * 2 * math.pi / 43)
    s.rustle = 0 if reduced else 1.5 + 5.5 * g
    # a cloud's shadow crossing the street every so often
    if reduced:
        s.shade = None
    else:
        period, speed, W, H = 150.0, 30.0, 1700.0, 900.0
        k = math.floor(t / period); u = t - k * period
        x = 1920 + 200 - speed * u
        s.shade = (x, 380 + 120 * (h(k, 3) - .5), W, H, 0.34 * (0.8 + 0.4 * h(k, 4)))
    return s

def mirror(t):
    return 1.0 - np.abs(np.mod(t * 0.5, 1.0) * 2.0 - 1.0)

def sample(img, x, y):
    """bilinear, x,y in pixel coords of img, clamped."""
    x = np.clip(x, 0, img.shape[1] - 1); y = np.clip(y, 0, img.shape[0] - 1)
    if img.ndim == 2:
        return ndimage.map_coordinates(img, [y, x], order=1, mode='nearest')
    return np.stack([ndimage.map_coordinates(img[..., c], [y, x], order=1, mode='nearest') for c in range(img.shape[2])], -1)

def cloud_at(px, py, rect, t, seed, churn, flip):
    x, y, w, hh = rect
    u = (px - x) / w; v = (py - y) / hh
    if churn:
        # billow about the base, and slow warps that move each lobe on its own
        b = 1 + 0.018 * math.sin(t * 2 * math.pi / 11.0 + seed)
        u = (u - .5) / b + .5; v = (v - 1) / b + 1
        u = u + 0.011 * np.sin(v * 6.0 + t * 2 * math.pi / 6.7 + seed) + 0.006 * np.sin(v * 13.0 - t * 2 * math.pi / 4.3 + seed * 2)
        v = v + 0.016 * np.sin(u * 7.0 + t * 2 * math.pi / 5.3 + seed * 1.7) + 0.008 * np.sin(u * 15.0 + t * 2 * math.pi / 3.9 + seed)
    inside = (u >= 0) & (u <= 1) & (v >= 0) & (v <= 1)
    if churn:
        # her mass was cut where the roof and the canopy crossed it; dissolve those cuts
        sm = lambda a, b, x: np.clip((x - a) / (b - a), 0, 1) ** 2 * (3 - 2 * np.clip((x - a) / (b - a), 0, 1))
        inside = inside * sm(0, .1, u) * sm(1, .9, u)
    if flip: u = 1 - u
    c = sample(CLOUD, u * (CLOUD.shape[1] - 1), v * (CLOUD.shape[0] - 1))
    c[..., 3] *= inside
    return c

def render_air(s, scale=.5):
    H, W = int(1080 * scale), int(1920 * scale)
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    px = (xs + .5) / scale; py = (ys + .5) / scale
    plate = cv2.resize(PLATE, (W, H), interpolation=cv2.INTER_AREA).copy()
    opening = cv2.resize(SKY, (W, H), interpolation=cv2.INTER_AREA)
    # sky, only in its bounding box for speed
    y1 = int(380 * scale); x0 = int(900 * scale)
    bx, by = px[:y1, x0:], py[:y1, x0:]
    painted = np.broadcast_to(SKY_COLOUR, bx.shape + (3,)).copy()
    clouds = s.clouds if hasattr(s, 'clouds') else [(s.far, s.fade[1], False, 1.3), (s.near, s.fade[0], False, 0.0)]
    for rect, fade, flip, seed in clouds:
        if fade <= 0: continue
        c = cloud_at(bx, by, rect, s.t, seed, s.churn, flip)
        a = np.clip(c[..., 3:4] * fade, 0, 1)
        painted = painted + a * (c[..., :3] - painted)
    plate[:y1, x0:] += opening[:y1, x0:, None] * (painted - SKY_COLOUR)
    # the cast shadow
    qx, qy = px.copy(), py.copy()
    if s.rustle:
        t = s.t; r = s.rustle
        ox = r * (0.6 * np.sin(qy * 0.019 + t * 1.25 + .3) + 0.4 * np.sin(qx * 0.013 - t * 0.9 + 1.7))
        oy = r * 0.55 * (0.6 * np.sin(qx * 0.021 + t * 1.05 + 2.1) + 0.4 * np.sin(qy * 0.015 + t * 1.4 + .6))
        f = 0.9 * (0.35 + s.gust) * np.sin(qx * 0.061 + qy * 0.037 + t * 3.1)
        qx = qx + ox + f; qy = qy + oy + f * .6
    u = mirror(qx / 1920 - s.sway / 1920); v = mirror(qy / 1080 + s.rise / 1080)
    shade = sample(SHADOW, u * 1919, v * 1079) * s.weight
    if s.shade is not None:
        x, y, w, hh, k = s.shade
        cu = (px - x) / w; cv = (py - y) / hh
        inside = (cu >= 0) & (cu <= 1) & (cv >= 0) & (cv <= 1)
        cs = sample(CLOUD_SOFT, (1 - cu) * 531, cv * 268) * inside * k * (1 - opening)
        shade = 1 - (1 - np.clip(shade, 0, 1)) * (1 - cs)
    plate *= 1 + np.clip(shade, 0, 1)[..., None] * (SHADOW_TINT - 1)
    return np.clip(plate, 0, 1)

# ---------------------------------------------------------------- leaves
LEAF_SLOTS, LEAF_PERIOD = 3, 27.0
CANOPY = ((1500, 40), (1880, 280))
# landing zone: x range, y range, drift range, swing range
# landing zone: x range, y range, drift range, swing range, share of falls
LANDINGS = (((1030, 1180), (700, 880), (380, 560), (70, 100), .42),
            ((1150, 1470), (590, 640), (230, 420), (60, 90), .36),
            ((1805, 1885), (770, 960), (25, 70), (26, 40), .22))
KEEP_OUT = ((1535, 625, 1750, 950), (1240, 655, 1545, 795), (220, 880, 325, 930), (1020, 925, 1165, 985),
            (590, 955, 1335, 1080), (0, 870, 410, 1080))

class Leaf:
    pass

def canopy_edge(x):
    """the lowest leaves of her tree at this x, read off the sky mask's canopy boundary"""
    return float(np.interp(x, [1450, 1520, 1600, 1680, 1730, 1775, 1900], [70, 70, 100, 130, 170, 260, 300]))

def leaf_plan(slot, cycle):
    L = Leaf()
    L.spawn = slot * LEAF_PERIOD / LEAF_SLOTS + cycle * LEAF_PERIOD + (h(slot, cycle, 1) - .5) * 2.4
    L.T = 10.0 + 3.0 * h(slot, cycle, 2)
    L.rest = 3.5 + 2.0 * h(slot, cycle, 3)
    L.fade = 2.6
    L.omega = 2 * math.pi / (2.2 + .7 * h(slot, cycle, 4))
    L.phi = 2 * math.pi * h(slot, cycle, 5)
    L.tumbler = h(slot, cycle, 7) < .3
    L.turns = 1 + int(h(slot, cycle, 8) * 2)
    L.restRoll = (h(slot, cycle, 9) - .5) * .3
    L.hue = h(slot, cycle, 10)
    L.kick = gust(L.spawn)
    for attempt in range(8):
        pick = h(slot, cycle, 20 + attempt)
        zone = LANDINGS[-1]
        for z in LANDINGS:
            if pick < z[4]: zone = z; break
            pick -= z[4]
        L.xl = zone[0][0] + (zone[0][1] - zone[0][0]) * h(slot, cycle, 30 + attempt)
        L.yl = zone[1][0] + (zone[1][1] - zone[1][0]) * h(slot, cycle, 40 + attempt)
        D = (zone[2][0] + (zone[2][1] - zone[2][0]) * h(slot, cycle, 50 + attempt)) * (.9 + .3 * L.kick)
        L.x0 = min(max(L.xl + D, 1470), 1895)
        L.y0 = canopy_edge(L.x0) - 10 - 40 * h(slot, cycle, 60 + attempt)
        L.A0 = zone[3][0] + (zone[3][1] - zone[3][0]) * h(slot, cycle, 70 + attempt)
        L.size = .36 + .34 * min(max((L.yl - 560) / 380, 0), 1)
        if leaf_path_clear(L): return L
    # the one path known to clear everything: off the left of the canopy to the open road
    L.x0, L.y0, L.xl, L.yl, L.A0 = 1500.0, 40.0, 1060.0, 790.0, 40.0
    L.size = .36 + .34 * min(max((L.yl - 560) / 380, 0), 1)
    return L

def leaf_pose(L, tau):
    """position, roll, width scale, height scale, s (0..1 flight, >1 landed)"""
    T = L.T
    if tau < T:
        s = tau / T
        th = L.omega * tau + L.phi
        A = L.A0 * (1 - s) ** 1.2 * min(1, tau / 1.2)
        D = L.x0 - L.xl
        x = L.x0 - D * (1 - (1 - s) ** 1.5) + A * math.sin(th)
        H = L.yl - L.y0
        # a pendulum: fastest through the bottom of each swing, nearly hanging at the ends
        y = L.y0 + H * (s + .075 * math.sin(2 * th) * s * (1 - s))
        # tilted like the bob of a pendulum: flat through the middle, edge up at each end
        roll = -.55 * math.sin(th) * min(1, tau / 1.2) * (1 - s ** 3) + L.restRoll * s ** 3
        if L.tumbler:
            e = s * (2 - s)
            wsc = math.cos(L.turns * 2 * math.pi * e)
        else:
            wsc = .8 + .2 * math.cos(2 * th)
            wsc = wsc + (1 - wsc) * s ** 4
        hsc = 1.0 + (.4 + .15 * math.cos(2 * th)) * (1 - s ** 2)
        return x, y, roll, wsc, hsc, s
    rest = tau - T
    slide = 5 * smooth(0, 1, rest / 1.6)
    return L.xl - slide, L.yl, L.restRoll, 1.0, 1.0, 1 + rest

def leaf_opacity(L, tau):
    if tau < 0: return 0
    a = smooth(0, 1, tau / .7)
    end = L.T + L.rest
    if tau > end: a *= 1 - smooth(0, 1, (tau - end) / L.fade)
    return .95 * a

def leaf_path_clear(L):
    w = LEAF.shape[1] * L.size * .5 + 6; hh = LEAF.shape[0] * L.size * 1.45 * .5 + 6
    r = max(w, hh)
    for i in range(61):
        x, y, *_ = leaf_pose(L, L.T * i / 60)
        if x - r < 0 or x + r > 1920: return False
        for (a, b, c, d) in KEEP_OUT:
            if x + r > a and x - r < c and y + r > b and y - r < d: return False
    x, y, *_ = leaf_pose(L, L.T + 1.6)
    for (a, b, c, d) in KEEP_OUT:
        if x + r > a and x - r < c and y + r > b and y - r < d: return False
    return True

def leaves_at(t):
    out = []
    for slot in range(LEAF_SLOTS):
        base = slot * LEAF_PERIOD / LEAF_SLOTS
        c = math.floor((t - base) / LEAF_PERIOD)
        for cycle in (c - 1, c):
            L = leaf_plan(slot, cycle)
            tau = t - L.spawn
            if tau < 0 or tau > L.T + L.rest + L.fade: continue
            out.append((L, tau))
    return out

def old_leaves_at(t):
    out = []
    for i in range(5):
        phase = (t / 11.4 + i * .61803399) % 1
        seed = i * 2.39996
        startX = 1425 + 455 * ((seed * .37) % 1)
        x = startX - phase * (360 + 260 * ((seed * .53) % 1)) + math.sin(phase * 7.1 + seed) * 34
        y = -40 + 800 * (phase * phase * .55 + phase * .45)
        turn = math.sin(t * 1.7 + seed * 3.1)
        roll = math.sin(phase * 5.2 + seed) * .55
        fade = smooth(0, 1, phase / .12) * smooth(0, 1, (1 - phase) / .3)
        size = .42 + .2 * ((seed * .71) % 1)
        out.append((x, y, roll, turn * size, size, fade * .85))
    return out

# ---------------------------------------------------------------- dust
def motes_at(t):
    out = []
    for i in range(26):
        d = (i * .754877666 + .13) % 1
        y0 = 585 + (1040 - 585) * d
        left, right = ground_left(y0) - 40, ground_right(y0) + 40
        vx = 4 + 9 * d
        span = right - left
        ph = ((h(i, 1) * span - vx * t) % span) / span
        x = left + ph * span
        y = y0 + math.sin(t * (.21 + .1 * h(i, 2)) + 6.28 * h(i, 3)) * (6 + 10 * d) + math.sin(t * (.53 + .2 * h(i, 4)) + 6.28 * h(i, 5)) * (2 + 5 * d)
        edge = math.sin(math.pi * ph) ** .5
        pg = 4.5 + 5 * h(i, 6)
        glint = max(0.0, math.sin(2 * math.pi * (t / pg + h(i, 7)))) ** 40
        a = (.14 + .55 * glint) * (.7 + .3 * d) * sun(x, y) * edge
        r = (1.3 + 2.3 * d) * (1 + .6 * glint)
        out.append((x, y, r, r, a, (1.0, .95, .82)))
    return out

WISP_BANDS = ((600, 680), (600, 680), (690, 860), (690, 860), (860, 1060), (860, 1060), (940, 1070))
def wisps_at(t):
    out = []
    g = gust(t)
    for i, (ya, yb) in enumerate(WISP_BANDS):
        P = 11 + 7 * h(i, 1); L = 7.5 + 2.5 * h(i, 2)
        off = P * i / len(WISP_BANDS)
        c = math.floor((t + off) / P); local = t + off - c * P
        if local > L: continue
        u = local / L
        y = ya + (yb - ya) * h(i, c, 3)
        d = (y - 590) / 480
        x0 = ground_left(y) + 120 + (ground_right(y) - ground_left(y) - 120) * h(i, c, 4)
        travel = (90 + 200 * d) * (.75 + .7 * gust(t - local))
        x = x0 - travel * u
        y = y - (2 + 8 * d) * u
        rx = (60 + 130 * d) * (.8 + .5 * u); ry = (4 + 10 * d) * (.9 + .4 * u)
        a = math.sin(math.pi * u) ** 2 * (.11 + .07 * d) * (.75 + .6 * g)
        out.append((x, y, rx, ry, a, (.965, .81, .59)))
    return out

def grains_at(t):
    """sand lying loose on the road, creeping with the breeze and lifting when it gusts"""
    out = []
    g = gust(t)
    for i in range(GRAINS):
        d = (i * .754877666 + .03) % 1
        y0 = 590 + (1078 - 590) * d
        left, right = ground_left(y0) - 60, ground_right(y0) + 60
        span = right - left
        v = 5 + 13 * d
        ph = (((i * .569840291 + .41) % 1) * span - v * t) % span / span
        x = left + ph * span
        edge = min(1.0, ph * span / 50, (1 - ph) * span / 50)
        Ph = 3.2 + 3.0 * h(i, 2)
        lift = max(0.0, math.sin(2 * math.pi * (t / Ph + h(i, 3)))) ** 6 * (.3 + g)
        y = y0 - (2 + 7 * d) * lift + math.sin(t * .4 + i * 2.31) * (1 + 3 * d)
        tw = .78 + .22 * math.sin(t * (1.1 + .9 * h(i, 4)) + 6.28 * h(i, 5))
        a = (.26 + .2 * d) * (.6 + .4 * sun(x, y)) * edge * tw
        tint = (.63, .46, .31) if i % 3 == 0 else (1.0, .89, .66)
        out.append((x, y, (1.7 + 2.6 * d) * (1 + .5 * lift), .9 + 1.3 * d, a, tint))
    return out

GRAINS = 84

def old_dust_at(t):
    out = []
    for i in range(11):
        depth = (i * .75487766 + .11) % 1
        phase = (t / (100 + (38 - 100) * depth) + i * .618034) % 1
        life = math.sin(phase * math.pi)
        y = 594 + (1080 - 594) * depth + math.sin(t * .25 + i) * (2 + 6 * depth)
        x = ground_left(y) - 70 + (ground_right(y) + 70 - ground_left(y) + 70) * phase
        out.append((x, y, 65 + 115 * depth, 4 + 9 * depth, life * life * (.11 + .05 * depth), (244 / 255, 195 / 255, 133 / 255)))
    for i in range(78):
        depth = (i * .75487766 + .03) % 1
        phase = (t / (130 + (45 - 130) * depth) + i * .618034) % 1
        life = math.sin(phase * math.pi)
        y = 586 + (1085 - 586) * depth + math.sin(t * .4 + i * 2.31) * (2 + 5 * depth)
        x = ground_left(y) - 50 + (ground_right(y) + 50 - ground_left(y) + 50) * phase
        tint = (172 / 255, 126 / 255, 85 / 255) if i % 3 == 0 else (1.0, 224 / 255, 166 / 255)
        out.append((x, y, 2 + 3.4 * depth, .9 + 1.4 * depth, life * (.24 + .19 * depth), tint))
    return out

# ---------------------------------------------------------------- compositing meshes
def splat_puffs(img, puffs, scale, clip_ground=True):
    H, W = img.shape[:2]
    gm = cv2.resize(GROUND, (W, H), interpolation=cv2.INTER_AREA) if clip_ground else None
    for (x, y, rx, ry, a, tint) in puffs:
        if a <= .003: continue
        cx, cy = x * scale, y * scale; sx, sy = max(rx * scale, .5), max(ry * scale, .5)
        x0, x1 = int(cx - sx - 2), int(cx + sx + 3); y0, y1 = int(cy - sy - 2), int(cy + sy + 3)
        x0c, x1c, y0c, y1c = max(x0, 0), min(x1, W), max(y0, 0), min(y1, H)
        if x0c >= x1c or y0c >= y1c: continue
        yy, xx = np.mgrid[y0c:y1c, x0c:x1c].astype(np.float32)
        rr = np.sqrt(((xx + .5 - cx) / sx) ** 2 + ((yy + .5 - cy) / sy) ** 2)
        al = np.clip(1 - rr, 0, 1) * a   # the fan: centre alpha a, rim 0, linear
        if gm is not None: al *= gm[y0c:y1c, x0c:x1c]
        patch = img[y0c:y1c, x0c:x1c]
        patch += al[..., None] * (np.array(tint, np.float32) - patch)

def draw_quad(img, tex, cx, cy, w, hh, roll, tint, alpha, scale):
    """draw tex (RGBA) centred at cx,cy with size w x hh (w may be negative: mirrored), rotated by roll."""
    H, W = img.shape[:2]
    if abs(w) < .5 or alpha <= .004: return
    th, tw = tex.shape[:2]
    c, s = math.cos(roll), math.sin(roll)
    # map texture pixel (u,v) -> screen: centre + R * ((u/tw - .5)*w, (v/th - .5)*hh)
    # y down in screen; roll positive rotates clockwise visually with y down, matches Unity's counterclockwise in y-up after flip
    ax = w / tw * scale; ay = hh / th * scale
    M = np.array([[c * ax, -s * ay, 0], [s * ax, c * ay, 0]], np.float32)
    M[0, 2] = cx * scale - (M[0, 0] * tw / 2 + M[0, 1] * th / 2)
    M[1, 2] = cy * scale - (M[1, 0] * tw / 2 + M[1, 1] * th / 2)
    warped = cv2.warpAffine(tex, M, (W, H), flags=cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
    a = warped[..., 3:4] * alpha
    img += a * (warped[..., :3] * np.array(tint, np.float32) - img)

def render_leaves(img, t, scale, old=False):
    if old:
        for (x, y, roll, wsc, size, a) in old_leaves_at(t):
            draw_quad(img, LEAF, x, y, LEAF.shape[1] * wsc, LEAF.shape[0] * size, -roll, (1, 1, 1), a, scale)
        return
    items = leaves_at(t)
    for (L, tau) in items:   # shadows first
        x, y, roll, wsc, hsc, s = leaf_pose(L, tau)
        near = smooth(.45, 1, min(s, 1)) ** 2
        if near <= 0: continue
        hgt = max(L.yl - y, 0)
        sx = x + hgt * .12; sy = L.yl + 3
        a = .30 * sun(L.xl, L.yl) * near * leaf_opacity(L, tau) / .95
        grow = 1 + .7 * min(hgt / 300, 1)
        draw_quad(img, LEAF, sx, sy, LEAF.shape[1] * L.size * abs(wsc) * grow, LEAF.shape[0] * L.size * .9 * grow, roll * .5, (.16, .09, .04), a / grow, scale)
    for (L, tau) in items:
        x, y, roll, wsc, hsc, s = leaf_pose(L, tau)
        under = wsc < 0
        tint = (.80, .76, .66) if under else (1.0, 1.0, 1.0)
        warm = .94 + .12 * L.hue
        tint = (tint[0] * warm, tint[1], tint[2] * (2 - warm))
        draw_quad(img, LEAF, x, y, LEAF.shape[1] * L.size * wsc, LEAF.shape[0] * L.size * hsc, roll, tint, leaf_opacity(L, tau), scale)

def frame(t, scale=.5, old=False, reduced=False):
    s = air_params(t, reduced, old)
    img = render_air(s, scale)
    if not reduced:
        splat_puffs(img, old_dust_at(t) if old else (wisps_at(t) + grains_at(t) + motes_at(t)), scale)
        render_leaves(img, t, scale, old)
    return img
