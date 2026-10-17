"""The modelled props of Rago's SUPERNOVA cutscene, built and PAINTED the way the hand companions and Paete's reworked
plants are: the PAROL he builds and lets go, the small parol that drifts up behind him with the dusk, and a quarter of
the far TOWN of home with its strings of fiesta flags.

  py -3 tools/build_ultimate_props_sean.py [--out=<folder>] [parol] [parol_small] [duskline]
  blender -b --python tools/review_hand_companion.py -- --hero=parol --file=<folder>/parol.glb --atlas=<folder>/parol-atlas.png --version=v1
  py -3 tools/hand_companion_kit.py sheet parol v1

Why: the cutscene was staged with stock prisms and flat stars, and the owner, once the staging stood: "continue polishing
all the ultimates. i dont want to see anything bare in textures, anything looking unpolished, no vfx etc." A first set of
these models was flat palette prisms, as Rafi's first shark was; of that shark he said it "doesn't fit the hand painted
illustrated stylistic art design of our characters and overall game style". So: `tools/hand_companion_kit.py` (round
forms, smooth normals), and a painted atlas beside the models (`parol-atlas.png`) whose upper half carries the drawing:
the paper of each point with its chevrons and its capiz diamond, the heart's rosette, the lattice of the star behind,
the shell fans of the crown, bamboo with its knuckles, the cut paper of the tails, and the lit windows of the town.

A PAROL HAS FIVE OF EVERYTHING, so a point is modelled and painted ONCE and stood five times round the middle; that is what
a parol is, the same as a left hand and its mirror. Each of the five is still its own node, because the runtime moves
them one by one.

The parol lies in the XY plane, one unit from its middle to a point, its FACE ON +Z: the runtime grows that one object
from a hand's width to 24 m and lays it over with +z down, so +z is both what the lens sees in his hands and the
underside the court sees. Nodes (each one's origin is where it pivots):
  stick-0..4         the bamboo frame, each stick from a point to the point two along; origin at its middle
  point-0..4         the paper of each point, a soft pillow; origin at its middle
  heart              the middle, with its capiz boss
  ring               the bamboo hoop through the five points and its lashings
  back               a second star behind, its points in the gaps of the first (the runtime turns it against the first)
  crown > petal-0..9 ten shell scallops outside the hoop; each petal's origin at its own middle
  strings            the five cords its lights hang on
  bulb               one bulb (the runtime copies it on to the cords); origin at its middle, its glass toward +z
  tail-0..4 > tail-N-end   a tail at every point: cord, paper bell, and under it the three strips of cut paper

Metres, y up. The sixteen flat colours are `UltimateProp.ParolPalette` (`Runtime/Visual/UltimateProp.Sean.cs`), same order.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, rounded, tube  # noqa: E402

PALETTE = ["#E9BF6A", "#B26F2C", "#FFCF3F", "#FF8F1F", "#E8402A", "#FF6A1A", "#FFF3D6", "#F6DCA6",
           "#B8242C", "#8C1C22", "#4A2414", "#2A120C", "#3D1A10", "#FFC24A", "#F2705A", "#FFF0B8"]
BAMBOO, BAMBOO_DK, GOLD, ORANGE, RED, FLAME, CAPIZ, CAPIZ_SH, CRIMSON, DEEP, STRING, TOWN, TOWN_LT, WINDOW, CORAL, BULB = range(16)

ATLAS = 2048
TEXTURE = "parol-atlas.png"
# The atlas's upper half is paint (u0, v0, u1, v1; v down as glTF has it). Each rect is one drawing.
R_POINT, R_HEART, R_BACK = (0.0, 0.0, 0.25, 0.25), (0.25, 0.0, 0.5, 0.25), (0.5, 0.0, 0.625, 0.25)
R_SCALLOP, R_SCALLOP_S = (0.625, 0.0, 0.75, 0.125), (0.625, 0.125, 0.75, 0.25)
R_BAMBOO, R_BELL = (0.75, 0.0, 0.875, 0.25), (0.875, 0.0, 1.0, 0.125)
R_STRIP_A, R_STRIP_B = (0.875, 0.125, 0.9375, 0.25), (0.9375, 0.125, 1.0, 0.25)
R_HOUSE_A, R_HOUSE_B, R_HOUSE_C = (0.0, 0.25, 0.25, 0.5), (0.25, 0.25, 0.5, 0.5), (0.5, 0.25, 0.75, 0.5)
R_ROOF, R_TOWER = (0.75, 0.25, 0.875, 0.5), (0.875, 0.25, 1.0, 0.5)
# What each drawing covers, in the piece's own upright space (x0, x1, y0, y1).
B_POINT, B_HEART, B_BACK = (-0.26, 0.26, 0.27, 1.03), (-0.42, 0.42, -0.42, 0.42), (-0.24, 0.24, -0.02, 0.94)
B_SCALLOP, B_SCALLOP_S = (-0.18, 0.18, -0.17, 0.25), (-0.18, 0.18, -0.17, 0.16)


def arg(name, default):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def smooth01(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def inset(rect, px=3):
    d = px / ATLAS
    return (rect[0] + d, rect[1] + d, rect[2] - d, rect[3] - d)


# ------------------------------------------------------------------ the paint

def paint_atlas(path):
    from PIL import Image, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc(PALETTE[GOLD])

    def field(rect, bounds):
        """The piece's own x and y at every pixel of its rect (the top row is the top of the piece)."""
        x0, y0, x1, y1 = (int(round(v * ATLAS)) for v in rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        uu, tt = np.meshgrid(u, t)
        return bounds[0] + (bounds[1] - bounds[0]) * uu, bounds[2] + (bounds[3] - bounds[2]) * tt, (x0, y0, x1, y1)

    def flat(shape, colour):
        return np.zeros(shape + (3,), np.float32) + hexc(colour)

    def mix(base, colour, weight):
        w = np.clip(weight, 0.0, 1.0)[..., None]
        return base * (1 - w) + hexc(colour) * w

    def line(value, width):
        """1 on a repeating line, 0 between: `value` counts lines."""
        f = value % 1.0
        return np.exp(-(np.minimum(f, 1 - f) / width) ** 2)

    def put(box, c):
        x0, y0, x1, y1 = box
        img[y0:y1, x0:x1] = c

    # ---- one point of the star: lit paper in two warm halves that meet softly on the ridge, a trim along its two long
    # edges with a cream pinstripe inside it, chevrons climbing to the tip, and a capiz diamond set in the middle.
    x, y, box = field(R_POINT, B_POINT)
    c = flat(x.shape, "#FFD24A")
    c = mix(c, "#FF9E2C", smooth01(-0.05, 0.05, x) * 0.9)
    c = mix(c, "#F2701C", smooth01(0.62, 0.30, y) * 0.45)                       # deeper toward the heart
    c = mix(c, "#FFF0B4", smooth01(0.74, 1.0, y) * 0.5)                         # paler toward the tip
    c = mix(c, "#FFF6D0", np.exp(-(x / 0.014) ** 2) * 0.55)                     # the ridge catches the light
    half = 0.2245 * (1 - (y - 0.309) / 0.691)
    edge = half - np.abs(x)
    c = mix(c, "#FFF3D0", line((y + np.abs(x) * 1.5) / 0.15, 0.085) * smooth01(0.03, 0.06, edge) * 0.5)
    c = mix(c, "#E9571C", line((y + np.abs(x) * 1.5) / 0.15 + 0.5, 0.05) * smooth01(0.03, 0.06, edge) * 0.28)
    c = mix(c, "#D94A1A", smooth01(0.036, 0.016, edge) * 0.85)                  # the trim
    c = mix(c, "#FFF3D0", np.exp(-((edge - 0.05) / 0.007) ** 2) * 0.7)          # its pinstripe
    c = mix(c, "#E9571C", smooth01(0.345, 0.325, y) * 0.8)                      # the hem along its base
    diamond = np.abs(x) / 0.062 + np.abs(y - 0.60) / 0.095
    c = mix(c, "#C8651E", smooth01(1.22, 1.08, diamond))
    c = mix(c, "#FFF8E2", smooth01(1.04, 0.94, diamond))
    c = mix(c, "#F2D9A0", smooth01(1.04, 0.94, diamond) * smooth01(-0.004, 0.012, x) * 0.8)
    c = mix(c, "#FFFFFF", np.exp(-(((x + 0.018) / 0.012) ** 2 + ((y - 0.63) / 0.02) ** 2)) * 0.8)
    for dy, r in ((0.80, 0.017), (0.875, 0.012)):                               # two shell beads above it
        bead = np.hypot(x, y - dy) / r
        c = mix(c, "#C8651E", smooth01(1.5, 1.2, bead)); c = mix(c, "#FFF8E2", smooth01(1.1, 0.9, bead))
    put(box, c)

    # ---- the heart: hot at its middle and red at its rim, a ten-petal rosette, two capiz rings and a ring of beads.
    x, y, box = field(R_HEART, B_HEART)
    r, a = np.hypot(x, y), np.arctan2(y, x)
    c = flat(x.shape, "#FFF2B0")
    c = mix(c, "#FFB42E", smooth01(0.03, 0.15, r)); c = mix(c, "#FF6E1C", smooth01(0.13, 0.27, r)); c = mix(c, "#D6342A", smooth01(0.25, 0.40, r))
    petal = 0.5 + 0.5 * np.cos(10 * (a - math.pi / 2))
    inside = smooth01(0.02, -0.02, r - (0.10 + 0.13 * petal ** 1.5))
    c = mix(c, "#FFD766", inside * 0.6)
    c = mix(c, "#E9571C", np.exp(-(((r - (0.10 + 0.13 * petal ** 1.5)) / 0.008) ** 2)) * 0.7)
    c = mix(c, "#FFF3D0", np.exp(-((r - 0.275) / 0.007) ** 2) * 0.85)
    c = mix(c, "#FFF3D0", np.exp(-((r - 0.335) / 0.005) ** 2) * 0.6)
    beads = (0.5 + 0.5 * np.cos(20 * a)) ** 6 * np.exp(-((r - 0.305) / 0.013) ** 2)
    c = mix(c, "#FFF8E2", beads)
    put(box, c)

    # ---- a point of the star behind: deep paper, warmer toward the middle of the lantern, a capiz lattice like the panes
    # of a shell window, a gold edge and a small shell star near its tip.
    x, y, box = field(R_BACK, B_BACK)
    c = flat(x.shape, "#B8242C")
    c = mix(c, "#E4502E", smooth01(0.55, 0.05, y) * 0.7)
    c = mix(c, "#7E1620", smooth01(0.5, 0.92, y) * 0.6)
    c = mix(c, "#8E1A24", smooth01(-0.03, 0.03, x) * 0.35)
    half = np.where(y > 0.275, 0.2 * (0.9 - y) / 0.625, 0.2 * (y - 0.02) / 0.255)
    edge = half - np.abs(x)
    panes = np.maximum(line((x + y) / 0.085, 0.07), line((x - y) / 0.085, 0.07))
    c = mix(c, "#F4A070", panes * smooth01(0.02, 0.05, edge) * 0.42)
    c = mix(c, "#FFC84A", smooth01(0.034, 0.018, edge) * 0.9)
    c = mix(c, "#FFF0C0", np.exp(-((edge - 0.045) / 0.006) ** 2) * 0.5)
    spark = np.minimum(np.abs(x) / 0.05 + np.abs(y - 0.64) / 0.014, np.abs(x) / 0.014 + np.abs(y - 0.64) / 0.05)
    c = mix(c, "#FFF8E2", smooth01(1.1, 0.8, spark))
    put(box, c)

    # ---- a scallop of the crown: a capiz shell fan, ribs spreading from its foot, a gold lip; the short one is the
    # same shell in gold with cream ribs.
    for rect, bounds, body, rib, lip, tall in ((R_SCALLOP, B_SCALLOP, "#FFF6DE", "#EBC98C", "#FFB830", 0.22), (R_SCALLOP_S, B_SCALLOP_S, "#FFC93C", "#FFF1C4", "#F0781E", 0.13)):
        x, y, box = field(rect, bounds)
        c = flat(x.shape, body)
        fan = np.arctan2(x, y + 0.34)
        c = mix(c, rib, line(fan / 0.105 + 0.5, 0.16) * 0.75)
        c = mix(c, "#FF9A30", smooth01(0.0, -0.13, y) * 0.55)
        reach = np.hypot(x * 1.1, y + 0.11) / (tall + 0.11)
        c = mix(c, rib, line(reach * 3.0, 0.08) * 0.3)
        c = mix(c, lip, smooth01(0.80, 0.93, reach + np.abs(x) * 1.6))
        put(box, c)

    # ---- bamboo: round its girth left to right, along its length bottom to top. A lit lane and a shaded lane, fine
    # grain, and a dark knuckle with a pale lip eight times along it.
    x, y, box = field(R_BAMBOO, (0.0, 1.0, 0.0, 1.0))
    c = flat(x.shape, "#E6B866")
    c = mix(c, "#F8DE9C", np.exp(-(((x - 0.25) % 1.0 - 0.0) / 0.10) ** 2) * 0.7 + np.exp(-((x - 0.25) / 0.10) ** 2) * 0.7)
    c = mix(c, "#B77E36", np.exp(-((x - 0.75) / 0.16) ** 2) * 0.6)
    c = mix(c, "#C99444", line(x * 14 + np.sin(y * 40) * 0.2, 0.12) * 0.22)
    c = mix(c, "#8A5622", line(y * 8, 0.035) * 0.9)
    c = mix(c, "#FBE6AE", line(y * 8 + 0.07, 0.03) * 0.6)
    put(box, c)

    # ---- the paper bell of a tail: red, folded, a gold zigzag round its waist and a cream hem.
    x, y, box = field(R_BELL, (0.0, 1.0, 0.0, 1.0))
    c = flat(x.shape, "#E23C2A")
    c = mix(c, "#F2643A", smooth01(0.5, 1.0, y) * 0.6)
    c = mix(c, "#B8242C", line(x * 8, 0.16) * 0.35)
    zig = np.abs(((x * 8) % 1.0) - 0.5) * 0.16
    c = mix(c, "#FFCF3F", smooth01(0.05, 0.03, np.abs(y - 0.42 - zig)))
    c = mix(c, "#FFF3D6", smooth01(0.14, 0.11, y))
    c = mix(c, "#FFCF3F", smooth01(0.03, 0.02, np.abs(y - 0.16)))
    put(box, c)

    # ---- the strips of cut paper under it: across left to right, along bottom (its pointed end) to top.
    for rect, body, band_a, band_b, cut in ((R_STRIP_A, "#FFF3D6", "#E23C2A", "#FF8F1F", "#FFCF3F"), (R_STRIP_B, "#F2705A", "#FFF3D6", "#FFCF3F", "#B8242C")):
        x, y, box = field(rect, (-1.0, 1.0, 0.0, 1.0))
        c = flat(x.shape, body)
        c = mix(c, "#E8C890" if body == "#FFF3D6" else "#C9483C", smooth01(0.55, 1.0, np.abs(x)) * 0.5)
        c = mix(c, band_a, smooth01(0.045, 0.035, np.abs(y - 0.74)))
        c = mix(c, band_b, smooth01(0.035, 0.025, np.abs(y - 0.44)))
        c = mix(c, band_a, smooth01(0.02, 0.012, np.abs(y - 0.88)))
        for at in (0.59, 0.30):
            c = mix(c, cut, smooth01(1.1, 0.9, np.abs(x) / 0.55 + np.abs(y - at) / 0.06))
        c = mix(c, cut, smooth01(0.16, 0.12, y))
        put(box, c)

    # ---- the town, seen against the last of the sun: dark walls that catch a rim of ember light at the top, planks,
    # and windows lit from inside, each with its frame, its bars and the light it throws on the wall round it.
    def wall(rect, windows, door=None, awning=False):
        x, y, box = field(rect, (0.0, 1.0, 0.0, 1.0))
        c = flat(x.shape, "#2A120C")
        c = mix(c, "#4C2210", smooth01(0.55, 1.0, y) * 0.8)
        c = mix(c, "#1C0A06", line(y * 9, 0.06) * 0.45)
        c = mix(c, "#1C0A06", smooth01(0.12, 0.0, y) * 0.6)
        for wx, wy, ww, wh in windows:
            reach = np.maximum(np.abs(x - wx) / ww, np.abs(y - wy) / wh)
            c = mix(c, "#B4501A", np.exp(-(np.hypot((x - wx) / (ww * 2.6), (y - wy) / (wh * 2.6))) ** 2 * 1.4) * 0.5)
            c = mix(c, "#160804", smooth01(1.28, 1.14, reach))
            c = mix(c, "#FFC24A", smooth01(1.04, 0.96, reach))
            c = mix(c, "#FFE9A6", np.exp(-(np.hypot((x - wx) / ww, (y - wy + wh * 0.3) / wh)) ** 2 * 2.2) * smooth01(1.04, 0.96, reach) * 0.8)
            bars = np.maximum(smooth01(0.10, 0.05, np.abs(x - wx) / ww), smooth01(0.10, 0.05, np.abs(y - wy) / wh))
            c = mix(c, "#3D1A10", bars * smooth01(1.04, 0.96, reach) * 0.9)
        if door:
            dx, dw, dh = door
            inside = smooth01(1.05, 0.95, np.maximum(np.abs(x - dx) / dw, y / dh))
            c = mix(c, "#160804", inside); c = mix(c, "#5A2812", inside * line(x * 22, 0.1) * 0.5)
        if awning:
            band = smooth01(0.05, 0.035, np.abs(y - 0.70))
            c = mix(c, "#E23C2A", band * (line(x * 6, 0.25) > 0.4)); c = mix(c, "#FFF3D6", band * (line(x * 6, 0.25) <= 0.4))
        put(box, c)

    wall(R_HOUSE_A, [(0.30, 0.56, 0.11, 0.15)], door=(0.72, 0.09, 0.52))
    wall(R_HOUSE_B, [(0.28, 0.74, 0.09, 0.10), (0.72, 0.74, 0.09, 0.10), (0.30, 0.30, 0.09, 0.12)], door=(0.72, 0.08, 0.40))
    wall(R_HOUSE_C, [(0.50, 0.38, 0.30, 0.17)], awning=True)
    wall(R_TOWER, [(0.50, 0.80, 0.20, 0.07), (0.50, 0.46, 0.16, 0.05)])
    # ---- a roof: rows of dark tiles, each row's lower lip catching the light, brightest along the ridge.
    x, y, box = field(R_ROOF, (0.0, 1.0, 0.0, 1.0))
    c = flat(x.shape, "#3D1A10")
    c = mix(c, "#1E0C06", line(y * 5, 0.10) * 0.8)
    c = mix(c, "#6E3016", line(y * 5 + 0.16, 0.07) * 0.5)
    c = mix(c, "#2A120C", line(x * 12 + np.floor(y * 5) * 0.5, 0.07) * 0.5)
    c = mix(c, "#A04A1C", smooth01(0.86, 1.0, y) * 0.6)
    put(box, c)

    picture = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").filter(ImageFilter.GaussianBlur(0.8))
    # The lower half belongs to the palette (`TumbangPreso/Toon` reads a slot there, not a pixel): leave it one flat tone.
    flatten = np.array(picture); flatten[ATLAS // 2:] = hexc(PALETTE[GOLD]).astype(np.uint8)
    Image.fromarray(flatten, "RGB").save(path)


# ------------------------------------------------------------------ shapes the kit does not have

def turn_xy(points, degrees):
    c, s = math.cos(math.radians(degrees)), math.sin(math.radians(degrees))
    return [(x * c - y * s, x * s + y * c) for x, y in points]


def pillow(outline, puff, back=None, z=0.0, per=7, rings=5, power=2.2):
    """A soft cushion over a flat outline (xy, star-shaped about its middle): domed `puff` on its face (+z) and `back`
    behind, the two skins meeting in one rounded rim. Smooth normals. Paper stretched over a frame, a shell, a flag."""
    back = puff if back is None else back
    o = np.array(outline, np.float32)
    rim = []
    for i in range(len(o)):
        j = (i + 1) % len(o)
        for k in range(per):
            rim.append(o[i] + (o[j] - o[i]) * (k / per))
    rim = np.array(rim, np.float32)
    mid = o.mean(axis=0)
    n = len(rim)
    pos = [(mid[0], mid[1], z + puff)]
    for k in range(1, rings + 1):
        s = k / rings
        for p in rim:
            q = mid + (p - mid) * s
            pos.append((q[0], q[1], z + puff * (1 - s ** power)))
    back_mid = len(pos)
    pos.append((mid[0], mid[1], z - back))
    for k in range(1, rings):
        s = k / rings
        for p in rim:
            q = mid + (p - mid) * s
            pos.append((q[0], q[1], z - back * (1 - s ** power)))
    pos = np.array(pos, np.float32)
    tris = []

    def front(k, i):
        return 1 + (k - 1) * n + i % n

    def behind(k, i):
        return front(k, i) if k == rings else back_mid + 1 + (k - 1) * n + i % n

    for i in range(n):
        tris.append((0, front(1, i), front(1, i + 1)))
        tris.append((back_mid, behind(1, i + 1), behind(1, i)))
        for k in range(1, rings):
            tris.append((front(k, i), front(k + 1, i), front(k + 1, i + 1))); tris.append((front(k, i), front(k + 1, i + 1), front(k, i + 1)))
            tris.append((behind(k, i), behind(k + 1, i + 1), behind(k + 1, i))); tris.append((behind(k, i), behind(k, i + 1), behind(k + 1, i + 1)))
    tris = np.array(tris, np.uint32)
    # Counter-clockwise from outside: the face skin looks along +z, the back skin along -z.
    a, b, c = pos[tris[:, 0]], pos[tris[:, 1]], pos[tris[:, 2]]
    fn = np.cross(b - a, c - a)
    want = np.where(((a + b + c) / 3)[:, 2] >= z, 1.0, -1.0)
    flip = fn[:, 2] * want < 0
    tris[flip] = tris[flip][:, ::-1]
    fn[flip] *= -1
    nrm = np.zeros_like(pos)
    for k in range(3):
        np.add.at(nrm, tris[:, k], fn)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-9)
    return pos, nrm.astype(np.float32), tris


def uv_flat(mesh, rect, bounds, turned=0.0, about=(0.0, 0.0)):
    """Lays a flat-ish piece into its drawing: its xy, turned back `turned` degrees about `about` to stand upright, read
    against `bounds` (x0, x1, y0, y1) and put into `rect`. Call it on the piece where it is BUILT."""
    pos = mesh[0]
    c, s = math.cos(math.radians(-turned)), math.sin(math.radians(-turned))
    x = (pos[:, 0] - about[0]) * c - (pos[:, 1] - about[1]) * s
    y = (pos[:, 0] - about[0]) * s + (pos[:, 1] - about[1]) * c
    u0, v0, u1, v1 = inset(rect)
    u = np.clip((x - bounds[0]) / (bounds[1] - bounds[0]), 0, 1)
    t = np.clip((y - bounds[2]) / (bounds[3] - bounds[2]), 0, 1)
    return np.stack([u0 + (u1 - u0) * u, v1 - (v1 - v0) * t], axis=1).astype(np.float32)


def painted(mesh, rect, cols, along="rows", axis=1):
    return (mesh, kit.grid_uv(mesh, inset(rect), cols, along, axis))


def shifted(piece, by):
    """A (mesh, colour) piece moved into its node's own space."""
    mesh, colour = piece
    return ((mesh[0] - np.array(by, np.float32), mesh[1], mesh[2]), colour)


def circle(radius, z, count=40):
    return [(radius * math.cos(2 * math.pi * k / count), radius * math.sin(2 * math.pi * k / count), z) for k in range(count + 1)]


POINT = [(0.0, 1.0), (-0.2245, 0.309), (0.2245, 0.309)]            # one point of the star, upright
HEART = [(0.0, 0.382), (-0.3633, 0.1180), (-0.2245, -0.3090), (0.2245, -0.3090), (0.3633, 0.1180)]
BACK = [(0.0, 0.90), (-0.2, 0.275), (0.0, 0.02), (0.2, 0.275)]     # one point of the star behind, upright


def shrunk(outline, by):
    o = np.array(outline, np.float32)
    mid = o.mean(axis=0)
    return [tuple(mid + (p - mid) * by) for p in o]


def star_point(turn, puff, back):
    """The paper of one point, stood `turn` degrees round from the top. A little inside its frame, so the bamboo shows
    between it and its neighbours."""
    mesh = pillow(turn_xy(shrunk(POINT, 0.93), turn), puff, back)
    return (mesh, uv_flat(mesh, R_POINT, B_POINT, turn))


def star_heart(puff, back):
    mesh = pillow(shrunk(HEART, 0.93), puff, back, per=6)
    return (mesh, uv_flat(mesh, R_HEART, B_HEART))


def bamboo(points, girth, seg=10):
    return painted(tube(points, girth, seg=seg), R_BAMBOO, seg)


def tail_strip(x, top, length, width, lean, rect, z):
    """One strip of cut paper hanging from `top`, pointed at its end, leaning `lean` degrees."""
    outline = [(-width, 0.0), (-width, -length), (0.0, -length - width * 1.5), (width, -length), (width, 0.0)]
    mesh = pillow(outline, 0.007, per=4, rings=3)
    uv = uv_flat(mesh, rect, (-width, width, -length - width * 1.5, 0.0))
    return (moved(mesh, turn=(0, 0, lean), at=(x, top, z)), uv)


def tail(m, name, at, parent):
    """A tail hanging from `at`: its cord, a knot, a paper bell with a gold collar; and, as a second joint under the bell
    so it swings late, three strips of cut paper, the middle one longest."""
    bell = lathe([(0.046, -0.215), (0.064, -0.195), (0.058, -0.150), (0.036, -0.118), (0.014, -0.098)], seg=14)
    m.add(name, [(tube([(0, 0, 0), (0, -0.05, 0), (0, -0.10, 0)], 0.007, seg=6), STRING),
                 (ellipsoid((0.022, 0.022, 0.022), at=(0, -0.03, 0)), BAMBOO_DK),
                 painted(bell, R_BELL, 14, "axis", 1),
                 (tube([(0.066 * math.cos(2 * math.pi * k / 16), -0.205, 0.066 * math.sin(2 * math.pi * k / 16)) for k in range(17)], 0.009, seg=6), GOLD)],
          at=at, parent=parent)
    m.add(name + "-end", [tail_strip(0.0, 0.0, 0.40, 0.024, 0.0, R_STRIP_A, 0.0),
                          tail_strip(-0.03, 0.0, 0.30, 0.020, -7.0, R_STRIP_B, 0.012),
                          tail_strip(0.03, 0.0, 0.33, 0.020, 7.0, R_STRIP_B, -0.012),
                          (ellipsoid((0.016, 0.016, 0.016), at=(0.0, -0.455, 0.0)), GOLD)],
          at=(0.0, -0.21, 0.0), parent=name)


def finish(m):
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/UltimateProps"))
    m.texture = TEXTURE
    m.folder.mkdir(parents=True, exist_ok=True)
    paint_atlas(m.folder / m.texture)
    m.write(PALETTE)


# ------------------------------------------------------------------ the models

def parol():
    m = Model("parol")
    tips = turn_xy([(0.0, 1.0)], 0) + turn_xy([(0.0, 1.0)], 72) + turn_xy([(0.0, 1.0)], 144) + turn_xy([(0.0, 1.0)], 216) + turn_xy([(0.0, 1.0)], 288)

    # The frame: five canes, each from a point to the point two along.
    for i in range(5):
        a, b = tips[i], tips[(i + 2) % 5]
        mid = ((a[0] + b[0]) * .5, (a[1] + b[1]) * .5, 0.0)
        cane = bamboo([(a[0] + (b[0] - a[0]) * k / 6, a[1] + (b[1] - a[1]) * k / 6, 0.0) for k in range(7)], 0.021)
        m.add("stick-%d" % i, [shifted(cane, mid)], at=mid)

    # The paper: one point, five times round, and the heart with its capiz boss.
    for i in range(5):
        mid = turn_xy([(0.0, 0.56)], 72 * i)[0] + (0.0,)
        m.add("point-%d" % i, [shifted(star_point(72 * i, 0.085, 0.06), mid)], at=mid)
    m.add("heart", [star_heart(0.15, 0.10),
                    (ellipsoid((0.068, 0.068, 0.034), at=(0, 0, 0.150)), CAPIZ),
                    (ellipsoid((0.030, 0.030, 0.022), at=(0, 0, 0.176)), GOLD)])

    # The hoop through the five points, lashed to each with a bound knot.
    hoop = [bamboo(circle(1.03, 0.0), 0.024)]
    for i in range(5):
        x, y = turn_xy([(0.0, 1.02)], 72 * i)[0]
        hoop.append((ellipsoid((0.05, 0.05, 0.045), at=(x, y, 0.0)), BAMBOO_DK))
        hoop.append((ellipsoid((0.026, 0.026, 0.05), at=(x, y, 0.0)), CAPIZ_SH))
    m.add("ring", hoop)

    # The star behind: one point, five times round, in the gaps of the first.
    behind = []
    for i in range(5):
        mesh = pillow(turn_xy(BACK, 36 + 72 * i), 0.03, 0.03, z=-0.055)
        behind.append((mesh, uv_flat(mesh, R_BACK, B_BACK, 36 + 72 * i)))
    m.add("back", behind)

    # The crown: ten shell scallops outside the hoop, a long one over every point and a short one over every gap.
    m.add("crown", [(ellipsoid((0.01, 0.01, 0.01)), BAMBOO_DK)])
    for i in range(10):
        long_ = i % 2 == 0
        tall = 0.22 if long_ else 0.13
        mid = turn_xy([(0.0, 1.16)], 36 * i)[0] + (0.0,)
        mesh = pillow(turn_xy([(0.0, tall), (-0.15, -0.09), (0.0, -0.13), (0.15, -0.09)], 36 * i), 0.04, 0.03, per=6, rings=4)
        uv = uv_flat(mesh, R_SCALLOP if long_ else R_SCALLOP_S, B_SCALLOP if long_ else B_SCALLOP_S, 36 * i)
        bead = turn_xy([(0.0, tall)], 36 * i)[0]
        m.add("petal-%d" % i, [(mesh, uv), (ellipsoid((0.026, 0.026, 0.026), at=(bead[0], bead[1], 0.0)), CAPIZ if long_ else GOLD)], at=mid, parent="crown")

    # The cords its five rings of lights hang on, in front of the face, and one bulb for the runtime to copy on to them.
    m.add("strings", [(tube(circle(r, 0.125), 0.0045, seg=5), STRING) for r in (0.22, 0.40, 0.58, 0.76, 0.94)])
    # It is made at its true size and kept inside the heart, where nothing shows it; only its copies are seen.
    m.add("bulb", [(ellipsoid((0.016, 0.016, 0.021), at=(0, 0, 0.008), seg=10, rings=7), BULB),
                   (rounded((0.009, 0.009, 0.008), 0.004, at=(0, 0, -0.014), seg=10, rings=6), BAMBOO_DK)])

    # A tail at every point.
    for i in range(5):
        x, y = tips[i]
        tail(m, "tail-%d" % i, (x, y, 0.0), None)
    finish(m)


def parol_small():
    """The paper lantern of home: the same star as one piece with its hoop, and its two tails as one joint so they sway."""
    m = Model("parol_small")
    star = [star_point(72 * i, 0.11, 0.08) for i in range(5)] + [star_heart(0.19, 0.12), bamboo(circle(1.03, 0.0, 28), 0.04, seg=8)]
    m.add("star", star)
    tails = []
    for side in (-1, 1):
        x = side * 0.5878
        bell = moved(lathe([(0.075, -0.33), (0.10, -0.30), (0.09, -0.22), (0.05, -0.17), (0.02, -0.14)], seg=12), at=(x, 0, 0))
        tails.append((tube([(x, 0, 0), (x, -0.08, 0), (x, -0.15, 0)], 0.012, seg=6), STRING))
        tails.append((bell, kit.grid_uv(bell, inset(R_BELL), 12, "axis", 1)))
        tails.append(tail_strip(x - 0.035, -0.31, 0.42, 0.036, -5.0 * side, R_STRIP_A, 0.01))
        tails.append(tail_strip(x + 0.035, -0.31, 0.32, 0.032, 6.0 * side, R_STRIP_B, -0.01))
    m.add("tails", tails, at=(0.0, -0.809, 0.0))
    finish(m)


def stand(mesh, radius, compass, y, face=None):
    """A piece built upright at the origin, facing -z, stood on the far ring of town `compass` degrees round from +z and
    turned to face the court. `face` is its drawing: its own x and y are read against its own box into that rect."""
    lo, hi = mesh[0].min(axis=0), mesh[0].max(axis=0)
    uv = uv_flat(mesh, face, (lo[0], hi[0], lo[1], hi[1])) if face is not None else None
    a = math.radians(compass)
    put = moved(mesh, turn=(0, compass, 0), at=(radius * math.sin(a), y, radius * math.cos(a)))
    return (put, uv)


def house(radius, compass, wide, tall, deep, face, roofs):
    """A house: one soft block with its front drawn on it, and its roof in steps, each a softer, wider block. `roofs` is a
    list of (how wide, how tall, how deep) from the eaves up."""
    out = [stand(rounded((wide * .5, tall * .5, deep * .5), 0.09), radius, compass, tall * .5, face)]
    top = tall
    for w, h, d in roofs:
        out.append(stand(rounded((w * .5, h * .5, d * .5), min(0.11, h * .45)), radius, compass, top + h * .5 - 0.02, R_ROOF))
        top += h - 0.03
    return out


def flag(radius, compass, y, colour, swing):
    """One flag on its string: a small soft diamond hanging by its top corner."""
    mesh = pillow([(0.0, 0.0), (-0.17, -0.20), (0.0, -0.42), (0.17, -0.20)], 0.022, per=4, rings=3)
    a = math.radians(compass)
    return (moved(mesh, turn=(0, compass, swing), at=(radius * math.sin(a), y, radius * math.cos(a))), colour)


def on_ring(radius, compass, y):
    a = math.radians(compass)
    return (radius * math.sin(a), y, radius * math.cos(a))


def duskline():
    """A quarter of the far town, against the low sun: seven houses and a church on an arc 13.6 m out, from 42 degrees one
    side of +z to 42 the other, two power posts, and two strings of fiesta flags between the high places. The runtime
    stands four of these round him. Each house is typed: where it stands, how big, which front it wears, how its roof steps."""
    m = Model("duskline")
    town = []
    town += house(13.6, -38.0, 3.0, 1.6, 1.4, R_HOUSE_A, [(3.4, 0.30, 1.8), (2.3, 0.30, 1.3), (1.1, 0.26, 0.9)])
    town += house(13.7, -27.0, 2.2, 2.7, 1.5, R_HOUSE_B, [(2.6, 0.22, 1.9)])
    # A water tank on two legs on that roof.
    town.append(stand(lathe([(0.34, 0.0), (0.40, 0.08), (0.40, 0.52), (0.30, 0.66)], seg=12), 13.7, -25.6, 3.22, R_ROOF))
    town.append((tube([on_ring(13.7, -26.5, 2.86), on_ring(13.7, -26.5, 3.26)], 0.045, seg=6), TOWN))
    town.append((tube([on_ring(13.7, -24.7, 2.86), on_ring(13.7, -24.7, 3.26)], 0.045, seg=6), TOWN))
    town += house(13.6, -16.5, 3.2, 1.3, 1.4, R_HOUSE_C, [(3.6, 0.22, 1.9)])
    # The church: a wide body, a bell tower with a lit belfry, a cap in three steps.
    town += house(13.8, -7.0, 3.0, 2.4, 1.6, R_HOUSE_B, [(3.4, 0.30, 2.0), (2.2, 0.38, 1.4)])
    town += house(13.7, -1.2, 1.3, 4.2, 1.3, R_TOWER, [(1.6, 0.22, 1.6), (1.0, 0.50, 1.0), (0.5, 0.40, 0.5)])
    town += house(13.6, 7.5, 2.6, 1.7, 1.4, R_HOUSE_A, [(3.0, 0.30, 1.8), (1.8, 0.30, 1.2)])
    town += house(13.7, 24.0, 3.0, 2.1, 1.5, R_HOUSE_B, [(3.4, 0.26, 1.9), (2.1, 0.32, 1.3)])
    town += house(13.7, 39.0, 2.4, 1.4, 1.4, R_HOUSE_C, [(2.8, 0.24, 1.8)])
    # Two power posts, each a little off plumb, with their crossbars, and the lines sagging between them and away.
    town.append((tube([on_ring(13.2, 15.5, 0.0), on_ring(13.2, 15.6, 2.3), on_ring(13.2, 15.4, 4.5)], [0.09, 0.08, 0.06], seg=8), TOWN))
    town.append((tube([on_ring(13.2, 12.9, 4.18), on_ring(13.2, 15.5, 4.22), on_ring(13.2, 18.1, 4.18)], 0.045, seg=6), TOWN))
    town.append((tube([on_ring(13.2, 13.7, 3.84), on_ring(13.2, 15.5, 3.86), on_ring(13.2, 17.3, 3.84)], 0.04, seg=6), TOWN))
    town.append((tube([on_ring(13.2, 33.0, 0.0), on_ring(13.2, 32.9, 2.1), on_ring(13.2, 33.1, 4.1)], [0.09, 0.08, 0.06], seg=8), TOWN))
    town.append((tube([on_ring(13.2, 30.6, 3.78), on_ring(13.2, 33.0, 3.82), on_ring(13.2, 35.4, 3.78)], 0.045, seg=6), TOWN))
    town.append((tube([on_ring(13.2, 15.5, 4.22), on_ring(13.2, 20.0, 3.72), on_ring(13.2, 24.5, 3.55), on_ring(13.2, 29.0, 3.62), on_ring(13.2, 33.0, 3.82)], 0.018, seg=5), TOWN))
    town.append((tube([on_ring(13.2, 33.0, 3.82), on_ring(13.3, 37.0, 3.42), on_ring(13.3, 41.0, 3.34), on_ring(13.4, 45.0, 3.60)], 0.018, seg=5), TOWN))
    m.add("town", town)

    # The fiesta flags: a string from the tall house to the bell tower, and one from the tower to the first post, each
    # sagging, each flag typed where it hangs and how far it has swung. Their own node.
    flags = [(tube([on_ring(13.3, -25.5, 3.75), on_ring(13.25, -21.0, 3.24), on_ring(13.2, -15.5, 2.98), on_ring(13.2, -10.0, 3.02), on_ring(13.2, -5.5, 3.62), on_ring(13.2, -2.5, 4.40)], 0.016, seg=5), STRING),
             flag(13.26, -22.5, 3.38, GOLD, 8.0), flag(13.22, -19.0, 3.12, CORAL, -6.0), flag(13.20, -15.5, 2.98, CAPIZ, 10.0),
             flag(13.20, -12.0, 2.96, RED, -9.0), flag(13.20, -8.5, 3.20, GOLD, 5.0), flag(13.20, -5.2, 3.68, ORANGE, -7.0),
             (tube([on_ring(13.2, 0.2, 4.40), on_ring(13.15, 3.5, 3.66), on_ring(13.1, 8.0, 3.26), on_ring(13.15, 12.0, 3.50), on_ring(13.2, 15.5, 4.22)], 0.016, seg=5), STRING),
             flag(13.16, 2.6, 3.86, CORAL, 7.0), flag(13.12, 5.2, 3.44, CAPIZ, -8.0), flag(13.10, 8.0, 3.26, GOLD, 6.0),
             flag(13.12, 10.8, 3.38, RED, -5.0), flag(13.16, 13.4, 3.76, ORANGE, 9.0)]
    m.add("flags", flags)
    finish(m)


if __name__ == "__main__":
    which = {a for a in sys.argv[1:] if not a.startswith("--")} or {"parol", "parol_small", "duskline"}
    for name, build in (("parol", parol), ("parol_small", parol_small), ("duskline", duskline)):
        if name in which:
            build()
