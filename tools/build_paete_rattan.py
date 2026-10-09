"""THORN HARVEST's armed rattan, remodelled as a little character (`Visual.PaeteThornBody`).

Owner, 2026-10-07: "by 'rework' im thinking of remodelling, textures, and vfx.. not just vfx"; "try the cutesy
character for the plants"; then, of this one, "did you remodel the thorns or no?". The pitcher went first
(`tools/build_paete_bloom.py`); this is the same treatment: the hand companions' kit, round chunky forms, a painted
atlas, an ink line.

It is still a clump of rattan: five ringed canes round a spot, a ruff of bone-tipped spines, on each cane a frond
that rears and hooks over like a talon with a barbed straw whip coiled under its arch. What is new is WHO it is: in
the middle of the canes sits a round spiny bud with a scowl, slanted eyes and red cheeks, and the canes are its arms.
Small, cross, and it wants your slippers.

  py -3 tools/build_paete_rattan.py [--out=folder]
  blender -b --python tools/review_hand_companion.py -- --hero=thorns --folder=PaeteProps --version=v1 --atlas=<folder>/thorns-atlas.png

Writes Assets/TumbangPreso/Resources/Models/PaeteProps/thorns.glb and thorns-atlas.png (the first model is kept at
ArtSource/paete/props-pre-rework/thorns.glb). The node NAMES are the body's contract, unchanged: `clump`, and for
i in 0..4 `sheath-i` (its origin at its foot, turned so +z is outward), `frond-i` (its origin on top of the cane,
turned the same; the body pitches it about x) and `whip-i` under the frond. New: `heart` (the bud; the body pops it
up, squashes it and sinks it) with `eye-l` and `eye-r` under it.

Metres, y up. The sixteen colours are `PaeteThornBody.Palette`, same order; 13 to 15 were spare and are now the
blush, white and the bud's pale belly.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, tube  # noqa: E402
from build_paete_bloom import arg, hexc, inset, leaf_painted, painted, smooth01, turned  # noqa: E402

PALETTE = ["#2F4219", "#1F2D10", "#4A5F26", "#6E6A34", "#34301A", "#221F10", "#D8B86E", "#7E5E2E",
           "#1E140C", "#130E09", "#C9BC98", "#6B4A2E", "#EAD49C", "#F08A8A", "#FFFDF6", "#B9D36A"]
BLADE, BLADE_DK, BLADE_LIT, RACHIS, SHEATH, SHEATH_DK, STRAW, BAND, INK, SPINE, BONE, SOIL, STRAW_LIT, BLUSH, WHITE, BELLY = range(16)

ATLAS = 2048
R_CANE, R_BLADE, R_RACHIS, R_WHIP, R_MOUND = (0.0, 0.0, 0.25, 0.25), (0.25, 0.0, 0.5, 0.25), (0.5, 0.0, 0.625, 0.25), (0.625, 0.0, 0.75, 0.25), (0.75, 0.0, 1.0, 0.25)
R_BUD = (0.0, 0.25, 0.5, 0.5)

# Canes: compass, distance out, height, girth, lean out. (The first model's five, a little stouter.)
# ⚠️ None stands in front of the bud's face (+z, compass 0): the nearest are 52 degrees to either side of it.
CANES = [(52, 0.50, 0.36, 0.100, 14), (122, 0.48, 0.30, 0.092, 10), (180, 0.52, 0.44, 0.108, 15), (238, 0.48, 0.28, 0.090, 11), (308, 0.50, 0.34, 0.098, 14)]
# Spines on each cane, typed: angle round it, how far up (0 to 1), length.
CANE_SPINES = [
    [(0, .30, .13), (75, .26, .11), (150, .34, .14), (225, .28, .11), (300, .32, .12), (40, .68, .11), (170, .72, .12), (290, .66, .10)],
    [(20, .32, .11), (110, .28, .13), (200, .34, .10), (290, .30, .12), (60, .72, .10), (230, .70, .11)],
    [(8, .24, .14), (80, .28, .12), (152, .22, .15), (224, .27, .12), (296, .25, .13), (44, .58, .12), (160, .62, .13), (280, .60, .11), (110, .86, .09)],
    [(34, .32, .11), (124, .28, .12), (214, .34, .10), (304, .30, .11), (80, .74, .10), (250, .72, .10)],
    [(12, .28, .13), (86, .32, .11), (158, .26, .14), (230, .30, .11), (302, .28, .12), (50, .66, .11), (190, .70, .12), (310, .68, .10)],
]
# Fronds: the rachis rearing up, arching over, hooking down like a talon (y up, z out), its girth, and its leaflets:
# (rachis point, length, width, sweep off the rachis, lift).
FRONDS = [
    ([(0, 0, 0), (0, .26, .09), (0, .50, .24), (0, .64, .46), (0, .60, .68), (0, .44, .84), (0, .26, .88), (0, .17, .80)],
     [.050, .046, .040, .033, .026, .019, .013, .008],
     [(1, 0.44, 0.13, 36, 44), (1, 0.42, 0.12, -38, 42), (2, 0.50, 0.14, 33, 30), (2, 0.47, 0.13, -35, 28), (3, 0.44, 0.12, 30, 8), (3, 0.42, 0.12, -31, 6), (4, 0.33, 0.10, 28, -22), (4, 0.32, 0.10, -29, -24)]),
    ([(0, 0, 0), (0, .22, .10), (0, .42, .25), (0, .52, .45), (0, .48, .63), (0, .34, .76), (0, .19, .79), (0, .11, .72)],
     [.046, .042, .036, .030, .023, .017, .012, .008],
     [(1, 0.40, 0.12, 37, 40), (1, 0.39, 0.12, -36, 42), (2, 0.45, 0.13, 34, 26), (2, 0.44, 0.13, -33, 27), (3, 0.40, 0.11, 30, 4), (3, 0.39, 0.11, -30, 5), (4, 0.29, 0.09, 27, -20), (4, 0.28, 0.09, -28, -21)]),
    ([(0, 0, 0), (0, .30, .08), (0, .58, .22), (0, .76, .44), (0, .74, .68), (0, .58, .88), (0, .36, .96), (0, .22, .90)],
     [.054, .050, .043, .036, .029, .021, .014, .009],
     [(1, 0.46, 0.14, 35, 48), (1, 0.45, 0.13, -37, 46), (2, 0.53, 0.15, 32, 34), (2, 0.52, 0.14, -34, 33), (3, 0.49, 0.13, 29, 12), (3, 0.48, 0.13, -30, 11), (4, 0.39, 0.11, 27, -14), (4, 0.38, 0.11, -28, -15)]),
    ([(0, 0, 0), (0, .21, .11), (0, .38, .27), (0, .46, .46), (0, .41, .62), (0, .28, .73), (0, .15, .74), (0, .08, .67)],
     [.044, .040, .034, .028, .022, .016, .011, .007],
     [(1, 0.38, 0.12, 38, 38), (1, 0.37, 0.11, -37, 40), (2, 0.42, 0.12, 34, 24), (2, 0.41, 0.12, -35, 25), (3, 0.37, 0.11, 30, 2), (3, 0.36, 0.10, -31, 3), (4, 0.28, 0.09, 27, -22), (4, 0.27, 0.09, -28, -23)]),
    ([(0, 0, 0), (0, .25, .09), (0, .47, .24), (0, .60, .45), (0, .57, .66), (0, .42, .81), (0, .24, .86), (0, .14, .79)],
     [.050, .045, .039, .032, .025, .018, .012, .008],
     [(1, 0.42, 0.13, 36, 43), (1, 0.41, 0.12, -37, 41), (2, 0.48, 0.14, 33, 29), (2, 0.46, 0.13, -34, 30), (3, 0.42, 0.12, 30, 7), (3, 0.41, 0.12, -30, 8), (4, 0.32, 0.10, 28, -21), (4, 0.31, 0.10, -29, -20)]),
]
# The ruff between the canes: compass, radius, length, lean out.
RUFF = [(0, 0.66, 0.20, 52), (24, .64, .26, 40), (86, .66, .24, 36), (100, .58, .18, 46), (150, .67, .28, 34), (164, .58, .17, 48),
        (208, .66, .26, 36), (222, .58, .18, 46), (272, .66, .24, 36), (286, .58, .17, 48), (336, .64, .26, 40), (350, .60, .16, 54)]
# The bud's own spines, over its crown and its back: angle round (0 is its face), elevation, length.
BUD_SPINES = [(0, 74, .12), (60, 62, .11), (120, 56, .12), (180, 60, .13), (240, 56, .12), (300, 62, .11), (90, 30, .10), (150, 26, .11),
              (210, 26, .11), (270, 30, .10), (180, 4, .10), (128, 2, .09), (232, 2, .09), (38, 44, .08), (322, 44, .08)]
BUD_AT, BUD = (0.0, 0.36, 0.0), (0.315, 0.295, 0.315)


def spine(length, girth=0.020, tip=0.42):
    """A spine along +y from its foot: dark, with a bone point. Returns the two pieces."""
    dark = lathe([(girth, 0.0), (girth * .62, length * (1 - tip)), (girth * .55, length * (1 - tip))], seg=7)
    bone = lathe([(girth * .6, length * (1 - tip)), (0.0, length)], seg=7)
    return dark, bone


def place_spines(pieces, at, lean, compass, length, girth=0.020):
    """Stands a spine at `at`, leant `lean` degrees off upright toward `compass`."""
    dark, bone = spine(length, girth)
    how = dict(turn=(lean, compass, 0), at=at)
    pieces.append((moved(dark, **how), SPINE))
    pieces.append((moved(bone, **how), BONE))


def on_bud(angle, elevation, out=0.0):
    a, e = math.radians(angle), math.radians(elevation)
    d = (math.sin(a) * math.cos(e), math.sin(e), math.cos(a) * math.cos(e))
    return tuple(BUD_AT[k] + d[k] * (BUD[k] + out) for k in range(3))


def paint_atlas(path):
    from PIL import Image, ImageDraw, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc(PALETTE[SHEATH])

    def grid(rect):
        x0, y0, x1, y1 = (int(v * ATLAS) for v in rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.meshgrid(u, t), (x0, y0, x1, y1)

    def mix(base, colour, weight):
        return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]

    # ---- a cane: olive, paler toward its top, ringed at its joints like rattan, each ring with a pale lip under it.
    (u, t), (x0, y0, x1, y1) = grid(R_CANE)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#4B5A22")
    c = mix(c, "#7C8F35", smooth01(0.05, 1.0, t) * 0.8)
    c = mix(c, "#A3B24A", (0.5 + 0.5 * np.cos((u - 0.25) * 2 * np.pi)) ** 4 * 0.35)
    for at in (0.16, 0.37, 0.57, 0.76, 0.93):
        c = mix(c, "#27300F", np.exp(-((t - at) / 0.016) ** 2) * 0.85)
        c = mix(c, "#C2CC62", np.exp(-((t - at + 0.036) / 0.014) ** 2) * 0.5)
    img[y0:y1, x0:x1] = c
    # ---- a leaflet, both faces: deep green, a pale midrib, fine veins swept to the tip, a dark edge.
    (u, t), (x0, y0, x1, y1) = grid(R_BLADE)
    half = np.abs(((u * 2) % 1.0) - 0.5) * 2
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#2C5A1E")
    c = mix(c, "#57952E", smooth01(0.0, 0.9, t) * 0.85)
    c = mix(c, "#1B3A14", smooth01(0.70, 1.0, half) * 0.6)
    side = np.abs(((t * 9.0 - half * 2.2) % 1.0) - 0.5) * 2
    c = mix(c, "#8DC44A", (1 - smooth01(0.0, 0.14, side)) * (1 - smooth01(0.5, 0.9, half)) * 0.4)
    c = mix(c, "#B5DC62", (1 - smooth01(0.0, 0.08, half)) * 0.9)
    img[y0:y1, x0:x1] = c
    # ---- the rachis: cane brown-green going to straw at its tip.
    (u, t), (x0, y0, x1, y1) = grid(R_RACHIS)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#5E6A2A")
    c = mix(c, "#B9A85A", smooth01(0.45, 1.0, t) * 0.85)
    for at in (0.14, 0.28, 0.42, 0.56, 0.70):
        c = mix(c, "#33381A", np.exp(-((t - at) / 0.010) ** 2) * 0.6)
    img[y0:y1, x0:x1] = c
    # ---- the whip: straw, with dark node bands.
    (u, t), (x0, y0, x1, y1) = grid(R_WHIP)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#D8B86E")
    c = mix(c, "#F0DEA8", (0.5 + 0.5 * np.cos((u - 0.25) * 2 * np.pi)) ** 3 * 0.5)
    for k in range(9):
        c = mix(c, "#7E5E2E", np.exp(-((t - (0.08 + k * 0.105)) / 0.012) ** 2) * 0.85)
    img[y0:y1, x0:x1] = c
    # ---- the mound: dark soil, mossy on top.
    (u, t), (x0, y0, x1, y1) = grid(R_MOUND)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#4A3320")
    c = mix(c, "#6B4A2E", smooth01(0.2, 0.7, t) * 0.7)
    c = mix(c, "#4F6B24", smooth01(0.62, 1.0, t) * (0.55 + 0.35 * np.cos(u * 2 * np.pi * 5)) * 0.9)
    img[y0:y1, x0:x1] = c
    # ---- the bud: deep green over its crown and back, a pale belly on its face (u = 0.25).
    (u, t), (x0, y0, x1, y1) = grid(R_BUD)
    front = np.cos((u - 0.25) * 2 * np.pi)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#5D8A2A")
    c = mix(c, "#38601F", smooth01(0.55, 1.0, t) * 0.85)
    c = mix(c, "#2A4A18", np.clip(-front, 0, 1) * 0.5)
    c = mix(c, "#C9E27A", np.exp(-(((u - 0.25) / 0.115) ** 2 + ((t - 0.34) / 0.24) ** 2)) * 0.85)
    c = mix(c, "#3C2A14", (1 - smooth01(0.0, 0.10, t)) * 0.6)
    img[y0:y1, x0:x1] = c

    picture = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(picture, "RGBA")
    x0, y0, x1, y1 = (int(v * ATLAS) for v in R_BUD)
    w, h = x1 - x0, y1 - y0
    # Dark freckles over the bud's crown and back, each typed. None on its face.
    for u_at, t_at, r in [] if PLAIN_BUD else [(0.52, 0.70, 11), (0.60, 0.80, 9), (0.68, 0.66, 12), (0.76, 0.78, 10), (0.84, 0.68, 11), (0.93, 0.80, 9), (0.02, 0.70, 10),
                          (0.46, 0.84, 8), (0.72, 0.52, 8), (0.88, 0.54, 7), (0.58, 0.56, 7), (0.99, 0.56, 7)]:
        cx, cy = x0 + u_at * w, y0 + (1 - t_at) * h
        d.ellipse([cx - r, cy - r * .85, cx + r, cy + r * .85], fill=(30, 52, 18, 230))
    picture = picture.filter(ImageFilter.GaussianBlur(0.6))
    picture.save(path)


# ⚠️ PARED DOWN (owner, 2026-10-07, of the full clump with the bud in it: "i feel like its just too much ngl"). Five leafy
# fronds, a ruff, a ring of soil, spines on everything AND a face was a thicket with a face lost in it. Now it is the
# bud, and five thin thorny vine arms curling up round it from small nubs: no leaflets, no ruff, a low mound, a
# handful of spines. The names and places the body poses are the same.
SIMPLE = arg("pared", "") != ""
# ⚠️ AND THEN THE OTHER WAY (owner, of the pared-down one: "no i the vines were okay, its more the bud itself.."). The
# canes, fronds and whips are back as they were. It is the BUD that was too much: spines, freckles, a leaf tuft, fangs
# and a tongue on one small ball. Now it is a plain smooth bud, a little smaller, with only its cute angry face: two
# shining eyes under slanted brows, red cheeks, a small pout.
PLAIN_BUD = arg("busybud", "") == ""
if PLAIN_BUD:
    BUD_SPINES = []
    BUD_AT, BUD = (0.0, 0.30, 0.0), (0.275, 0.255, 0.275)
if SIMPLE:
    RUFF = []
    CANES = [(c, 0.40, h * 0.42, 0.070, lean) for c, _, h, _, lean in CANES]
    CANE_SPINES = [row[:2] for row in CANE_SPINES]
    FRONDS = [([(x, y * 0.78, z * 0.78) for x, y, z in rach], [r * 0.92 for r in radii], []) for rach, radii, _ in FRONDS]
    BUD_SPINES = [(0, 76, .12), (72, 56, .11), (144, 54, .12), (216, 54, .12), (288, 56, .11), (180, 14, .10), (118, 18, .09), (242, 18, .09)]


def main():
    m = Model("thorns")
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/PaeteProps"))
    m.texture = "thorns-atlas.png"
    m.folder.mkdir(parents=True, exist_ok=True)
    paint_atlas(m.folder / m.texture)

    # The clump: a ring of heaved soil with the ruff standing in it. The middle is the bud's.
    wide = 0.40 if SIMPLE else 0.52
    ring = [(wide * math.cos(2 * math.pi * k / 30), 0.02 + 0.012 * math.sin(k * 1.7), wide * math.sin(2 * math.pi * k / 30)) for k in range(31)]
    mound = tube(ring, [(0.085 if SIMPLE else 0.115) + 0.012 * math.sin(k * 2.3) for k in range(31)], seg=10)
    clump = [painted(mound, R_MOUND, 10)]
    for compass, radius, length, lean in RUFF:
        a = math.radians(compass)
        place_spines(clump, (radius * math.sin(a), 0.06, radius * math.cos(a)), lean, compass, length, girth=0.028)
    m.add("clump", clump)

    for i, (compass, dist, height, girth, lean) in enumerate(CANES):
        a = math.radians(compass)
        foot = (dist * math.sin(a), -0.06, dist * math.cos(a))
        lean_r = math.radians(lean)
        top = (0.0, height * math.cos(lean_r), height * math.sin(lean_r))
        # A stout cane, a little fatter at each joint, with a cap.
        cane = tube([(0, 0, 0), (0, top[1] * .34, top[2] * .34), (0, top[1] * .68, top[2] * .68), top], [girth, girth * 1.04, girth * .98, girth * .86], seg=12)
        pieces = [painted(cane, R_CANE, 12), (ellipsoid((girth * 1.02, 0.030, girth * 1.02), at=(top[0], top[1] + 0.01, top[2])), SHEATH_DK)]
        for angle, frac, length in CANE_SPINES[i]:
            b = math.radians(angle)
            at = (girth * .9 * math.sin(b), top[1] * frac, top[2] * frac + girth * .9 * math.cos(b))
            place_spines(pieces, at, 62, angle, length)
        m.add("sheath-%d" % i, pieces, at=foot, yaw=compass)

        rach, radii, blades = FRONDS[i]
        top_world = (foot[0] + top[2] * math.sin(a), foot[1] + top[1], foot[2] + top[2] * math.cos(a))
        frond = [painted(tube(rach, radii, seg=8), R_RACHIS, 8)]
        for j, length, width, sweep, lift in blades:
            frond.append(turned(leaf_painted(length, width, 0.030, R_BLADE), turn=(-lift, sweep, 0), at=rach[j]))
        # Hooked barbs down the back of the rachis, and the talon its tip hardens into.
        for j, along, length in ([(2, .4, .09), (4, .2, .07)] if SIMPLE else [(1, .5, .09), (2, .4, .10), (3, .3, .09), (4, .4, .07)]):
            p = tuple(rach[j][q] + (rach[j + 1][q] - rach[j][q]) * along for q in range(3))
            place_spines(frond, (p[0], p[1] + 0.02, p[2]), -38, 0, length, girth=0.016)
        tip, before = rach[-1], rach[-2]
        aim = math.degrees(math.atan2(tip[2] - before[2], tip[1] - before[1]))
        dark, bone = spine(0.13, girth=0.014, tip=0.6)
        frond += [(moved(dark, turn=(aim, 0, 0), at=tip), SPINE), (moved(bone, turn=(aim, 0, 0), at=tip), BONE)]
        m.add("frond-%d" % i, frond, at=top_world, yaw=compass)

        # The whip at rest: coiled under the arch like a sprung trap, with two hooks.
        coil = tube([(0, 0, 0), (0.02, -0.08, 0.08), (0.03, -0.20, 0.10), (0.02, -0.28, 0.04), (0.0, -0.25, -0.03), (-0.01, -0.18, -0.01)],
                    [0.020, 0.018, 0.016, 0.014, 0.012, 0.009], seg=8)
        whip = [painted(coil, R_WHIP, 8)]
        for at, compass_h in [((0.03, -0.14, 0.10), 80), ((0.03, -0.14, 0.10), 280), ((0.01, -0.27, 0.02), 40), ((0.01, -0.27, 0.02), 320)]:
            place_spines(whip, at, 70, compass_h, 0.06, girth=0.012)
        m.add("whip-%d" % i, whip, at=rach[3], parent="frond-%d" % i)

    # The bud. Its face is +z.
    body = ellipsoid(BUD, at=BUD_AT, seg=24, rings=16)
    heart = [painted(body, R_BUD, 24, "axis", 1)]
    for angle, elevation, length in BUD_SPINES:
        place_spines(heart, on_bud(angle, elevation, -0.008), 90 - elevation, angle, length, girth=0.024)
    for side in (-1, 1):
        heart.append((moved(ellipsoid((0.058, 0.036, 0.014)), turn=(4, side * 52, 0), at=on_bud(side * 52, -16, 0.002)), BLUSH))
    # ⚠️ CUTE ANGRY (owner, 2026-10-07, of the first face, which had heavy lids and a turned-down line for a mouth and
    # read as sad: "it looks weird.. im thinking of cute angry"). So: big round shining eyes, thick ink brows slanting
    # DOWN TO THE MIDDLE over them, cheeks puffed and red, and a small open shouting mouth with two fangs.
    if PLAIN_BUD:
        # A small pout: one short ink line, its ends turned down.
        heart.append((tube([on_bud(-7, -24, 0.003), on_bud(0, -20, 0.005), on_bud(7, -24, 0.003)], 0.010, seg=6), INK))
    else:
        mouth_at = on_bud(0, -22, 0.003)
        heart.append((moved(ellipsoid((0.040, 0.030, 0.010)), turn=(22, 0, 0), at=mouth_at), INK))
        heart.append((moved(ellipsoid((0.022, 0.012, 0.006)), turn=(22, 0, 0), at=on_bud(0, -27, 0.008)), BLUSH))     # its tongue
        for side in (-1, 1):
            heart.append((moved(lathe([(0.0, -0.026), (0.011, 0.0)], seg=6), turn=(22, 0, 0), at=on_bud(side * 5, -17.5, 0.008)), WHITE))
        # A tuft of two small leaves on its crown, so there is something soft on it.
        for yaw, lift in ((35, 50), (215, 62)):
            heart.append(turned(leaf_painted(0.16, 0.10, 0.03, R_BLADE), turn=(-lift, yaw, 0), at=on_bud(0, 86, -0.01)))
    m.add("heart", heart)
    for name, side in (("eye-l", -1), ("eye-r", 1)):
        at = on_bud(side * 23, 4, 0.004)
        turn = (-4, side * 23, 0)
        eye = moved(ellipsoid((0.056, 0.064, 0.016)), turn=turn)
        glint = moved(ellipsoid((0.020, 0.024, 0.006), at=(side * -0.014, 0.020, 0.014)), turn=turn)
        small = moved(ellipsoid((0.009, 0.010, 0.004), at=(side * 0.018, -0.022, 0.015)), turn=turn)
        # The brow: low at the middle of its face, high at the outside, lying on the top of the eye.
        brow = moved(tube([(side * -0.050, 0.040, 0.016), (side * 0.000, 0.066, 0.020), (side * 0.052, 0.092, 0.012)], [0.016, 0.017, 0.011], seg=8), turn=turn)
        m.add(name, [(eye, INK), (glint, WHITE), (small, WHITE), (brow, INK)], at=at, parent="heart")
    m.write(PALETTE)


if __name__ == "__main__":
    main()
