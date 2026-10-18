"""The modelled prop of Basilio's ultimate cutscene (CONTINENTAL DRIFT): the LONGHORN, the horned basalt beast that
shoulders up under his plate. Built the way his hand companion and Paete's reworked plants are: the hand companions'
kit (round chunky forms, smooth normals), a PAINTED atlas beside the model, an ink line.

  py -3 tools/build_ultimate_props_dante.py [--out=folder]
  py -3 tools/build_ultimate_props_dante.py --out=<folder> --small=0.2        (review only: see `small` below)
  blender -b --python tools/review_hand_companion.py -- --hero=longhorn --file=<folder>/longhorn.glb --atlas=<folder>/longhorn-atlas.png --version=v1 --size=1.9 --centre=0,0.4,0.12 --ink=0.007
  py -3 tools/hand_companion_kit.py sheet longhorn v1

Why: the cutscene was staged with about fifteen stock prisms for it, and the owner, once the staging stood: "continue
polishing all the ultimates. i dont want to see anything bare in textures, anything looking unpolished, no vfx etc."
A first model made of flat single-colour boxes was dropped the same day, when he refused Rafi's shark, made the same
way, for not fitting "the hand painted illustrated stylistic art design of our characters and overall game style".

What is kept of his earlier asks (`HeroIntroductionScene.Dante.cs`, its header): basalt and molten seams with horn and
claw silhouettes ("more demonic skills"), and stone that is SOLID and plain, with no grain. So the paint is broad soft
tone (dark at the foot, paler where the light falls) and a few drawn cracks with heat in them; nothing is speckled.
Its stone is his hand companion's stone (`tools/build_hand_dante.py`, `Rock`): a rounded block with its corners knocked
off by flat cuts, and the molten seams are fat cords pressed half into it.

⚠️ IT IS TYPED IN THE STAGE'S OWN SPACE (+x the stage's right, +z ahead, metres, y = 0 the court once it has fully risen)
AND FLIPPED ACROSS x FOR THE FILE, BECAUSE glTFast NEGATES X ON IMPORT. The beast is not symmetric: only its RIGHT arm
is a joint, and the stage swings that one.

⚠️ THE NUMBERS THE STAGE READS, which must not move without it: the top of the back is y = 3.4 under his plate
(`DnTitanTop`); the right arm's joint `arm-r` is at (2.75, 2.5, 0.6) and the arm hangs straight down -y from it, 3.3 m to
the middle of the fist; the eyes are on the face at (+/-0.50, 1.30, 3.36); `stand`, where his feet go, is on the crown of its back at (0, 3.40, 0.10)
(the crown's top is 3.40); `head` hinges at the neck (0, 1.0, 1.9) and
`jaw`, its child, at the back of the mouth (0, 0.55, 2.75).

Writes Assets/TumbangPreso/Resources/Models/UltimateProps/longhorn.glb and longhorn-atlas.png.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, rounded, tube  # noqa: E402
from build_hand_dante import Rock  # noqa: E402
from build_paete_bloom import arg, hexc, inset, smooth01  # noqa: E402

# The sixteen flat colours (`UltimateProp.EarthPalette`, same order). The rock itself is painted, not a slot.
PALETTE = ["#4B4441", "#322D2C", "#6B6058", "#FF7A18", "#FFC548", "#E6D6B4", "#A98F6C", "#FFA21F",
           "#FFF1B8", "#C23A16", "#F2E8D0", "#1E1A1A", "#8C8178", "#B8420E", "#7D726B", "#151212"]
BASALT, BASALT_DK, BASALT_LT, SEAM, SEAM_HOT, HORN, HORN_DK, EYE, EYE_HOT, MOUTH, TOOTH, CRUST, COURT, EMBER_DK, ASH, INK = range(16)

ATLAS = 2048
R_BODY, R_ROCK, R_PLATE = (0.0, 0.0, 0.5, 0.25), (0.5, 0.0, 0.75, 0.25), (0.75, 0.0, 1.0, 0.25)
R_HORN, R_BROW, R_MUZZLE = (0.0, 0.25, 0.125, 0.5), (0.125, 0.25, 0.25, 0.5), (0.25, 0.25, 0.5, 0.5)
R_ARM, R_COURT, R_KNUCKLE = (0.5, 0.25, 0.75, 0.5), (0.75, 0.25, 0.875, 0.5), (0.875, 0.25, 1.0, 0.5)


def paint_atlas(path):
    """Every painted surface: u is round the piece (its front is u = 0.25), t is 0 at its foot and 1 at its top.

    ⚠️ PAINTED STRUCTURE, NOT SPECKLE (owner, of the first atlas, which was three soft tones and a few cracks: "the
    longhorn lacks texture"; and his older rule, that the stone stays SOLID and plain of noisy grain). So nothing here
    is noise. The rock is laid down in broad STRATA, each a typed bed with its own tone, a drawn shadow under its ledge
    and a lit line on top of it; pits, chips and scars are typed marks, one at a time; ash lies on the upper faces and
    hangs down in typed runs; the stone is scorched brown toward every seam, with plates of cooled crust beside it.
    """
    from PIL import Image, ImageDraw, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc(PALETTE[BASALT])

    def grid(rect):
        x0, y0, x1, y1 = (int(v * ATLAS) for v in rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.meshgrid(u, t), (x0, y0, x1, y1)

    def mix(base, colour, weight):
        return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]

    def strata(u, t, foot, beds):
        """Basalt in beds. `beds`: (height it starts at, its tone, how far its ledge wanders, how many times round, where
        the wander starts). Above its ledge a bed is its tone; just under the ledge a drawn shadow; on it a lit line."""
        c = np.zeros(u.shape + (3,), np.float32) + hexc(foot)
        for at, tone, wander, times, phase in beds:
            edge = at + wander * np.sin(u * 2 * np.pi * times + phase) + wander * .45 * np.sin(u * 2 * np.pi * (times * 2 + 1) + phase * 2.3)
            c = mix(c, tone, smooth01(-0.006, 0.006, t - edge))
            c = mix(c, "#191514", np.exp(-((t - edge + 0.013) / 0.008) ** 2) * 0.55)
            c = mix(c, "#9A8E82", np.exp(-((t - edge - 0.011) / 0.005) ** 2) * 0.35)
        return c

    def ash(c, u, t, frm, runs):
        """Ash and soot settled on the upper faces, hanging down in typed runs (where round, how far down, how wide)."""
        c = mix(c, "#8F857B", smooth01(frm, 1.0, t) * 0.55)
        for at, down, wide in runs:
            c = mix(c, "#8F857B", np.exp(-(((u - at + .5) % 1.0 - .5) / wide) ** 2) * smooth01(frm - down, frm + 0.02, t) * (1 - smooth01(frm, frm + .12, t)) * 0.5)
        return c

    # ---- the back: scorched and lit from below where it comes out of the sea, four beds, ash on its shelf.
    (u, t), (x0, y0, x1, y1) = grid(R_BODY)
    c = strata(u, t, "#2A2422", [(0.20, "#3B3431", 0.020, 3, 0.4), (0.42, "#4B4441", 0.026, 2, 1.9), (0.63, "#57504B", 0.018, 4, 0.9), (0.82, "#6A6058", 0.014, 3, 2.6)])
    c = mix(c, "#7A2E12", (1 - smooth01(0.0, 0.17, t)) * 0.6)
    c = ash(c, u, t, 0.88, [(0.08, 0.10, 0.012), (0.19, 0.16, 0.016), (0.33, 0.08, 0.010), (0.46, 0.13, 0.014), (0.61, 0.11, 0.012), (0.77, 0.17, 0.016), (0.90, 0.09, 0.011)])
    img[y0:y1, x0:x1] = c
    # ---- a boulder (the shoulders, the skull, the fists): three beds, ash on top.
    (u, t), (x0, y0, x1, y1) = grid(R_ROCK)
    c = strata(u, t, "#2E2826", [(0.24, "#463F3C", 0.024, 2, 0.7), (0.52, "#544C47", 0.020, 3, 2.2), (0.76, "#6E645B", 0.016, 2, 1.3)])
    c = ash(c, u, t, 0.86, [(0.12, 0.12, 0.016), (0.30, 0.08, 0.012), (0.55, 0.14, 0.018), (0.72, 0.09, 0.012), (0.91, 0.12, 0.014)])
    img[y0:y1, x0:x1] = c
    # ---- an armour plate: darker, two beds, a worn pale edge along its top.
    (u, t), (x0, y0, x1, y1) = grid(R_PLATE)
    c = strata(u, t, "#252020", [(0.34, "#342E2D", 0.022, 2, 1.1), (0.66, "#423B38", 0.018, 3, 0.3)])
    c = mix(c, "#857A6E", smooth01(0.87, 0.95, t) * 0.75)
    img[y0:y1, x0:x1] = c
    # ---- a horn. t runs root to tip: a dark root, growth rings that crowd toward it, a lit side, a dark tip.
    (u, t), (x0, y0, x1, y1) = grid(R_HORN)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#E9DCBC")
    c = mix(c, "#B49A76", (1 - smooth01(0.0, 0.50, t)) * 0.8)
    c = mix(c, "#5C4936", (1 - smooth01(0.0, 0.10, t)) * 0.85)
    for at, wide, dark in [(0.075, 0.010, 0.8), (0.125, 0.010, 0.75), (0.17, 0.009, 0.7), (0.225, 0.009, 0.62), (0.285, 0.008, 0.55), (0.355, 0.008, 0.46),
                           (0.44, 0.007, 0.36), (0.54, 0.006, 0.26), (0.65, 0.006, 0.18)]:
        ring_at = at + 0.006 * np.sin(u * 2 * np.pi * 2 + at * 40)
        c = mix(c, "#6E5A44", np.exp(-((t - ring_at) / wide) ** 2) * dark)
        c = mix(c, "#F8F0DC", np.exp(-((t - ring_at - wide * 1.9) / (wide * .8)) ** 2) * dark * 0.5)
    c = mix(c, "#FBF4E2", np.exp(-((u - 0.25) / 0.09) ** 2) * smooth01(0.2, 0.5, t) * 0.55)
    c = mix(c, "#9C8666", np.exp(-((u - 0.75) / 0.14) ** 2) * 0.45)
    c = mix(c, "#2A211C", smooth01(0.80, 0.92, t))
    img[y0:y1, x0:x1] = c
    # ---- the brow: a dark ledge. t runs from the middle of the face out to its end.
    (u, t), (x0, y0, x1, y1) = grid(R_BROW)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#2F2928")
    c = mix(c, "#4A423E", np.exp(-((u - 0.25) / 0.16) ** 2) * 0.8)
    c = mix(c, "#7C7166", np.exp(-((u - 0.25) / 0.045) ** 2) * 0.55)
    for at in (0.30, 0.52, 0.72):
        c = mix(c, "#161212", np.exp(-((t - at - 0.03 * np.sin(u * 2 * np.pi)) / 0.010) ** 2) * 0.8)
    img[y0:y1, x0:x1] = c
    # ---- the muzzle: ash grey, darker toward the mouth, a paler pad round the nostrils.
    (u, t), (x0, y0, x1, y1) = grid(R_MUZZLE)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#7D726B")
    c = mix(c, "#5A504A", (1 - smooth01(0.0, 0.34, t)) * 0.85)
    c = mix(c, "#9C9086", smooth01(0.62, 1.0, t) * 0.7)
    c = mix(c, "#A69A90", np.exp(-(((u - 0.25) / 0.13) ** 2 + ((t - 0.60) / 0.16) ** 2)) * 0.6)
    img[y0:y1, x0:x1] = c
    # ---- a forearm. t runs wrist to shoulder: scorched at the wrist, beds round it like the bands of a pillar.
    (u, t), (x0, y0, x1, y1) = grid(R_ARM)
    c = strata(u, t, "#2E2826", [(0.22, "#453E3B", 0.018, 2, 0.5), (0.50, "#4F4743", 0.016, 3, 1.7), (0.78, "#5E554F", 0.014, 2, 2.9)])
    c = mix(c, "#8A3412", (1 - smooth01(0.0, 0.2, t)) * 0.65)
    img[y0:y1, x0:x1] = c
    # ---- the court: the grey of the plates he broke.
    (u, t), (x0, y0, x1, y1) = grid(R_COURT)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#8C8178")
    c = mix(c, "#6E655E", (1 - smooth01(0.0, 0.3, t)) * 0.8)
    c = mix(c, "#A0958B", smooth01(0.6, 1.0, t) * 0.6)
    img[y0:y1, x0:x1] = c
    # ---- a knuckle: pale worn stone, darker round its foot.
    (u, t), (x0, y0, x1, y1) = grid(R_KNUCKLE)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#8A7F76")
    c = mix(c, "#544B46", (1 - smooth01(0.0, 0.36, t)) * 0.9)
    c = mix(c, "#A69B90", smooth01(0.6, 1.0, t) * 0.7)
    img[y0:y1, x0:x1] = c

    picture = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
    scorch = Image.new("RGBA", picture.size, (0, 0, 0, 0))
    glow = Image.new("RGBA", picture.size, (0, 0, 0, 0))
    burnt, hot = ImageDraw.Draw(scorch), ImageDraw.Draw(glow)

    def px(rect, a, b):
        x0, y0, x1, y1 = (int(v * ATLAS) for v in rect)
        return (x0 + a * (x1 - x0), y0 + (1 - b) * (y1 - y0))

    def crack(rect, points, width):
        """One drawn crack, typed points (u, t): the stone scorched brown well out from it, heat soaked in nearer, then
        (drawn last, below) a dark lip, the molten line, and its bright middle."""
        pts = [px(rect, a, b) for a, b in points]
        burnt.line(pts, fill=(96, 40, 20, 170), width=width * 13, joint="curve")
        hot.line(pts, fill=(255, 110, 20, 160), width=width * 6, joint="curve")
        return pts

    drawn = []
    # The back's cracks, each typed: up from the sea on either flank and across the rump. None where the head hides it.
    for pts, w in [([(0.02, 0.02), (0.05, 0.22), (0.03, 0.40), (0.07, 0.56)], 7), ([(0.05, 0.22), (0.10, 0.30), (0.11, 0.44)], 5),
                   ([(0.47, 0.02), (0.44, 0.24), (0.47, 0.42), (0.43, 0.58)], 7), ([(0.44, 0.24), (0.39, 0.33)], 5),
                   ([(0.62, 0.02), (0.66, 0.20), (0.63, 0.38), (0.68, 0.52), (0.66, 0.70)], 7), ([(0.66, 0.20), (0.72, 0.28), (0.74, 0.42)], 5),
                   ([(0.84, 0.02), (0.81, 0.18), (0.85, 0.36), (0.82, 0.50)], 7), ([(0.85, 0.36), (0.90, 0.46)], 5),
                   ([(0.18, 0.58), (0.22, 0.72), (0.20, 0.86)], 5), ([(0.33, 0.60), (0.30, 0.74), (0.32, 0.88)], 5),
                   ([(0.14, 0.02), (0.16, 0.14), (0.13, 0.26)], 5), ([(0.36, 0.02), (0.34, 0.16), (0.37, 0.28)], 5),
                   ([(0.54, 0.02), (0.55, 0.16)], 5), ([(0.74, 0.02), (0.76, 0.12), (0.73, 0.20)], 5), ([(0.94, 0.02), (0.96, 0.18), (0.93, 0.30)], 5)]:
        drawn.append((crack(R_BODY, pts, w), w))
    # A boulder's: one long crack off to each side of its front, so a face or a set of knuckles is left clean.
    for pts, w in [([(0.04, 0.10), (0.07, 0.34), (0.04, 0.55), (0.08, 0.74)], 6), ([(0.07, 0.34), (0.12, 0.42)], 4),
                   ([(0.47, 0.12), (0.44, 0.38), (0.47, 0.60)], 6), ([(0.72, 0.20), (0.76, 0.44), (0.73, 0.66)], 6), ([(0.76, 0.44), (0.82, 0.50)], 4),
                   ([(0.58, 0.08), (0.60, 0.26)], 4), ([(0.90, 0.30), (0.93, 0.52), (0.90, 0.70)], 5)]:
        drawn.append((crack(R_ROCK, pts, w), w))
    for pts, w in [([(0.60, 0.30), (0.64, 0.50), (0.61, 0.74)], 5), ([(0.10, 0.36), (0.07, 0.58), (0.11, 0.80)], 5), ([(0.36, 0.20), (0.40, 0.42)], 4),
                   ([(0.84, 0.24), (0.86, 0.46), (0.83, 0.62)], 4)]:
        drawn.append((crack(R_ARM, pts, w), w))
    for pts, w in [([(0.20, 0.16), (0.24, 0.40), (0.20, 0.60)], 4), ([(0.70, 0.22), (0.66, 0.46), (0.70, 0.70)], 4)]:
        drawn.append((crack(R_PLATE, pts, w), w))
    scorch = scorch.filter(ImageFilter.GaussianBlur(22))
    glow = glow.filter(ImageFilter.GaussianBlur(12))
    picture.paste(scorch, (0, 0), scorch)
    picture.paste(glow, (0, 0), glow)
    line = ImageDraw.Draw(picture, "RGBA")

    def crust(rect, a, b, wide, high, turn):
        """A plate of cooled crust: a dark flake with a lit upper edge. Typed: where, how big, how it is turned."""
        cx, cy = px(rect, a, b)
        w, h = wide * ATLAS * .25, high * ATLAS * .25
        co, si = math.cos(math.radians(turn)), math.sin(math.radians(turn))
        shape = [(-1, -.2), (-.5, -1), (.4, -.8), (1, -.1), (.6, .9), (-.5, 1)]
        pts = [(cx + (x * w) * co - (y * h) * si, cy + (x * w) * si + (y * h) * co) for x, y in shape]
        line.polygon(pts, fill=(26, 20, 19, 240))
        line.line([pts[0], pts[1], pts[2], pts[3]], fill=(120, 108, 98, 220), width=3)

    def pit(rect, a, b, r):
        """A pit: a dark hollow, lit along its lower lip."""
        cx, cy = px(rect, a, b)
        line.ellipse([cx - r, cy - r * .8, cx + r, cy + r * .8], fill=(24, 19, 18, 235))
        line.arc([cx - r, cy - r * .8, cx + r, cy + r * .9], 20, 160, fill=(150, 138, 126, 230), width=max(2, int(r * .3)))

    def chip(rect, a, b, wide, deep):
        """A chip out of an edge: a pale fresh break with a dark line under it."""
        cx, cy = px(rect, a, b)
        w, d = wide * ATLAS * .25, deep * ATLAS * .25
        pts = [(cx - w, cy), (cx - w * .3, cy + d), (cx + w * .5, cy + d * .7), (cx + w, cy)]
        line.polygon(pts, fill=(158, 146, 132, 245))
        line.line(pts, fill=(28, 22, 20, 235), width=3)

    def scar(rect, a0, b0, a1, b1, width):
        """A scar: one dark cut with a pale edge."""
        p, q = px(rect, a0, b0), px(rect, a1, b1)
        line.line([p, q], fill=(170, 158, 144, 230), width=width + 4)
        line.line([p, q], fill=(34, 26, 24, 245), width=width)

    for pts, w in drawn:
        line.line(pts, fill=(30, 14, 10, 255), width=w + 8, joint="curve")
        line.line(pts, fill=(255, 122, 24, 255), width=w, joint="curve")
        line.line(pts, fill=(255, 220, 120, 255), width=max(2, w // 3), joint="curve")
    # Cooled crust beside the back's seams, each flake typed.
    for a, b, w, h, turn in [(0.075, 0.30, .060, .020, 20), (0.015, 0.48, .050, .018, -30), (0.495, 0.33, .056, .020, 160), (0.415, 0.50, .050, .016, 10),
                             (0.69, 0.30, .060, .020, -20), (0.60, 0.46, .050, .018, 30), (0.87, 0.26, .056, .020, 200), (0.795, 0.43, .050, .016, -15),
                             (0.24, 0.66, .046, .016, 40), (0.285, 0.80, .044, .016, -40), (0.185, 0.10, .040, .016, 10), (0.31, 0.12, .040, .016, -10)]:
        crust(R_BODY, a, b, w, h, turn)
    for a, b, w, h, turn in [(0.10, 0.26, .050, .022, 25), (0.02, 0.62, .044, .020, -30), (0.50, 0.30, .046, .020, 150), (0.79, 0.34, .050, .022, -20), (0.70, 0.56, .044, .020, 35)]:
        crust(R_ROCK, a, b, w, h, turn)
    for a, b, w, h, turn in [(0.66, 0.42, .040, .020, 20), (0.05, 0.50, .040, .020, -25)]:
        crust(R_ARM, a, b, w, h, turn)
    # Pits, each typed. None on a face's front (u near 0.25 on a boulder).
    for a, b, r in [(0.13, 0.50, 11), (0.26, 0.38, 14), (0.24, 0.47, 8), (0.38, 0.70, 10), (0.52, 0.52, 13), (0.57, 0.74, 9), (0.71, 0.60, 12),
                    (0.78, 0.76, 9), (0.92, 0.58, 13), (0.97, 0.72, 8), (0.09, 0.74, 10), (0.44, 0.78, 8)]:
        pit(R_BODY, a, b, r)
    for a, b, r in [(0.56, 0.40, 10), (0.62, 0.66, 12), (0.86, 0.24, 9), (0.94, 0.80, 8), (0.02, 0.30, 9), (0.42, 0.74, 9)]:
        pit(R_ROCK, a, b, r)
    for a, b, r in [(0.12, 0.30, 9), (0.40, 0.52, 11), (0.58, 0.24, 8), (0.84, 0.48, 10)]:
        pit(R_PLATE, a, b, r)
    for a, b, r in [(0.20, 0.62, 9), (0.48, 0.36, 10), (0.76, 0.70, 8), (0.92, 0.42, 9)]:
        pit(R_ARM, a, b, r)
    # Chips out of the plates' worn top edge, each typed.
    for a, wide, deep in [(0.07, .030, .024), (0.21, .042, .034), (0.34, .026, .020), (0.47, .046, .030), (0.62, .030, .026), (0.75, .040, .034), (0.90, .028, .022)]:
        chip(R_PLATE, a, 0.955, wide, deep)
    # The horn's wear: long scratches down its lit side, a nick out of its edge.
    for a0, b0, a1, b1 in [(0.20, 0.36, 0.22, 0.52), (0.30, 0.44, 0.31, 0.64), (0.26, 0.58, 0.24, 0.72), (0.62, 0.30, 0.64, 0.48), (0.80, 0.40, 0.78, 0.58)]:
        scar(R_HORN, a0, b0, a1, b1, 3)
    # The muzzle's drawn structure: three creases over the bridge of the nose, the line of its lip, whisker pits.
    for b, sag in [(0.84, 18), (0.74, 14), (0.65, 10)]:
        cx, cy = px(R_MUZZLE, 0.25, b)
        line.arc([cx - 110, cy - sag * 2, cx + 110, cy + sag * 2], 20, 160, fill=(70, 62, 58, 235), width=5)
        line.arc([cx - 110, cy - sag * 2 + 7, cx + 110, cy + sag * 2 + 7], 30, 150, fill=(170, 160, 150, 200), width=3)
    p, q = px(R_MUZZLE, 0.04, 0.22), px(R_MUZZLE, 0.46, 0.22)
    line.line([p, px(R_MUZZLE, 0.25, 0.18), q], fill=(44, 36, 34, 245), width=7, joint="curve")
    line.line([px(R_MUZZLE, 0.25, 0.18), px(R_MUZZLE, 0.25, 0.42)], fill=(60, 52, 48, 235), width=5)
    for a, b in [(0.10, 0.44), (0.13, 0.36), (0.085, 0.33), (0.40, 0.44), (0.37, 0.36), (0.415, 0.33)]:
        pit(R_MUZZLE, a, b, 6)
    # Scarred knuckles: two cuts and a chip on each.
    scar(R_KNUCKLE, 0.16, 0.72, 0.30, 0.52, 4); scar(R_KNUCKLE, 0.22, 0.44, 0.34, 0.60, 3)
    scar(R_KNUCKLE, 0.62, 0.70, 0.74, 0.50, 4); scar(R_KNUCKLE, 0.86, 0.60, 0.94, 0.44, 3)
    pit(R_KNUCKLE, 0.46, 0.66, 9); pit(R_KNUCKLE, 0.06, 0.56, 7)
    # The chalk line on the court's pieces, and a crack through it.
    x0, y0, x1, y1 = (int(v * ATLAS) for v in R_COURT)
    line.line([(x0, y0 + 0.42 * (y1 - y0)), (x1, y0 + 0.42 * (y1 - y0))], fill=(226, 220, 206, 235), width=22)
    scar(R_COURT, 0.20, 0.80, 0.34, 0.20, 3)
    picture = picture.filter(ImageFilter.GaussianBlur(0.7))
    picture.save(path)


# ------------------------------------------------------------------ pieces

def stone(half, radius, chips, rect, seg=22, rings=12, at=(0, 0, 0), turn=(0, 0, 0)):
    """His hand companion's stone at the size of a hill: a rounded block of half-size `half` (metres) with flat cuts,
    painted from `rect`, turned and placed. `Rock` is typed for a toy, so it is cut at a hundredth and scaled up."""
    rock = Rock(tuple(v * .01 for v in half), radius * .01, chips)
    parts = rock.mesh(seg, rings, 0, 0, foot_below=-2.0)
    pos, nrm = parts[0][0][0] * 100.0, parts[0][0][1]
    tris = np.concatenate([parts[0][0][2], parts[1][0][2]]) if len(parts[1][0][2]) else parts[0][0][2]
    mesh = (pos.astype(np.float32), nrm, tris)
    return (moved(mesh, at=at, turn=turn), kit.grid_uv(mesh, inset(rect), seg, "axis", 1))


def paint(mesh, rect, cols, along="rows", at=(0, 0, 0), turn=(0, 0, 0)):
    return (moved(mesh, at=at, turn=turn), kit.grid_uv(mesh, inset(rect), cols, along, 1))


def limb(points, radii, rect, seg=12, per=5):
    """A smooth tapering limb through typed points (a Catmull-Rom curve), painted along its length."""
    pts, rad = [], []
    P = [np.array(q, np.float32) for q in points]
    n = len(P)
    for i in range(n - 1):
        p0, p1, p2, p3 = P[max(i - 1, 0)], P[i], P[i + 1], P[min(i + 2, n - 1)]
        for k in range(per):
            t = k / per
            pts.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            rad.append(radii[i] + (radii[i + 1] - radii[i]) * (t * t * (3 - 2 * t)))
    pts.append(P[-1]); rad.append(radii[-1])
    mesh = tube([tuple(float(v) for v in q) for q in pts], rad, seg=seg)
    return (mesh, kit.grid_uv(mesh, inset(rect), seg))


def cord(points, radius, slot, seg=8):
    """A molten seam: a fat cord of the flat seam colour lying half in the stone, through typed points."""
    return limb_flat(points, radius, slot, seg)


def limb_flat(points, radius, slot, seg=8):
    mesh = limb(points, [radius] * len(points) if np.isscalar(radius) else radius, R_ROCK, seg=seg)[0]
    return (mesh, slot)


def mir(piece):
    """The same piece on the other side of the beast."""
    return (kit.mirror(piece[0]), piece[1])


def off(piece, origin):
    """A piece typed in the beast's space, taken into the space of a node whose origin is `origin`."""
    mesh = piece[0]
    return ((mesh[0] - np.array(origin, np.float32), mesh[1], mesh[2]), piece[1])


def main():
    m = Model("longhorn")
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/UltimateProps"))
    m.texture = "longhorn-atlas.png"
    m.folder.mkdir(parents=True, exist_ok=True)
    paint_atlas(m.folder / m.texture)

    # ---------------------------------------------------------------- the body
    body = []
    # The back: one great mound, widest at the court and closing to a flat shelf his plate rides on.
    body.append(paint(lathe([(2.95, -.30), (2.93, .60), (2.78, 1.50), (2.42, 2.40), (2.02, 2.96), (1.74, 3.22), (1.60, 3.32)], seg=28, squash_z=.87),
                      R_BODY, 28, "axis", at=(0, 0, -.25)))
    # The neck, and the nape over it: one dark plate between his plate and the head.
    body.append(stone((1.00, .95, .80), .5, [((0, 1, 1), .9)], R_ROCK, at=(0, .85, 1.85)))
    body.append(stone((.92, .44, .56), .3, [((-1, 1, .4), .88), ((1, 1, .3), .9)], R_PLATE, seg=16, rings=10, at=(0, 2.42, 1.66), turn=(28, 0, 0)))
    # The armour over the left flank: three plates back from the shoulder and one on the slope above them.
    flank = [stone((.30, .98, .74), .24, [((-1, 1, .5), .88), ((-1, -.6, -1), .9)], R_PLATE, seg=14, rings=10, at=(-2.66, 1.45, -1.00), turn=(0, -10, -13)),
             stone((.30, .93, .69), .24, [((-1, 1, -.5), .88), ((-1, -.5, 1), .9)], R_PLATE, seg=14, rings=10, at=(-2.00, 1.40, -2.30), turn=(0, -42, -13)),
             stone((.30, .88, .66), .24, [((-1, 1, .4), .88)], R_PLATE, seg=14, rings=10, at=(-.74, 1.35, -2.90), turn=(0, -74, -13)),
             stone((.28, .50, .76), .22, [((-1, 1, -.6), .88)], R_PLATE, seg=14, rings=10, at=(-1.90, 2.82, -.95), turn=(0, -10, -40))]
    # The seams between them: molten cords lying in the gaps, each with a hot middle.
    seams = [cord([(-2.72, .35, -.20), (-2.70, 1.20, -.16), (-2.52, 2.10, -.22), (-2.28, 2.50, -.30)], .075, SEAM),
             cord([(-2.48, .35, -1.80), (-2.40, 1.20, -1.74), (-2.20, 2.05, -1.62)], .075, SEAM),
             cord([(-1.44, .35, -2.72), (-1.36, 1.20, -2.62), (-1.22, 2.00, -2.40)], .075, SEAM),
             cord([(-2.36, 2.44, -.30), (-2.30, 2.40, -.95), (-2.16, 2.36, -1.60)], .06, SEAM),
             cord([(-2.70, 1.20, -.16), (-2.52, 2.10, -.22)], .05, SEAM_HOT, seg=6)]
    body += flank + [mir(p) for p in flank] + seams + [mir(p) for p in seams]
    # The chest's two seams, a V from the shoulders down to the neck: the widest on it.
    chest = [cord([(-1.72, 2.86, 1.20), (-1.50, 2.30, 1.56), (-1.22, 1.72, 1.80)], [.07, .10, .06], SEAM),
             cord([(-1.62, 2.58, 1.44), (-1.50, 2.30, 1.63), (-1.36, 2.00, 1.76)], .045, SEAM_HOT, seg=6)]
    body += chest + [mir(p) for p in chest]
    # Two blunt fins down the spine behind his plate, for the outline from the side.
    body.append(stone((.24, .52, .50), .2, [((0, 1, -1), .86)], R_PLATE, seg=12, rings=8, at=(0, 2.80, -2.25), turn=(-32, 0, 0)))
    body.append(stone((.24, .52, .50), .2, [((0, 1, -1), .86)], R_PLATE, seg=12, rings=8, at=(0, 1.90, -2.86), turn=(-50, 0, 0)))

    # The left shoulder: a boulder leaning out, a paler cap of stone on it, and two claws of rock standing off the cap.
    shoulder = [stone((.92, 1.50, 1.00), .55, [((-1, 1, .3), .86), ((-1, .2, 1), .9), ((-.4, 1, -1), .88), ((1, 1, 1), .9), ((-1, -.5, -.6), .92)], R_ROCK,
                      at=(-2.58, 1.42, .55), turn=(0, -18, 5)),
                stone((.84, .30, .90), .26, [((-1, 1, .5), .86), ((1, 1, -.4), .9)], R_PLATE, seg=16, rings=10, at=(-2.70, 3.00, .52), turn=(0, -18, 9)),
                limb([(-2.85, 3.02, .82), (-3.30, 3.48, .86), (-3.52, 3.98, .76), (-3.50, 4.22, .70)], [.32, .24, .12, .05], R_HORN, seg=10),
                limb([(-2.72, 3.02, -.02), (-3.06, 3.36, -.12), (-3.20, 3.72, -.20), (-3.18, 3.90, -.22)], [.26, .19, .10, .045], R_HORN, seg=10),
                cord([(-2.30, 2.70, 1.46), (-2.22, 2.05, 1.60), (-2.36, 1.40, 1.58), (-2.28, .70, 1.56)], [.04, .07, .07, .04], SEAM),
                cord([(-2.36, 1.40, 1.58), (-2.70, 1.10, 1.44)], [.06, .03], SEAM),
                cord([(-2.24, 2.30, 1.60), (-2.22, 2.05, 1.66), (-2.33, 1.62, 1.65)], .032, SEAM_HOT, seg=6)]
    body += shoulder + [mir(p) for p in shoulder]
    # What it came up through: three pieces of the court still lying on it, each its own size and its own way round.
    body.append(paint(rounded((.60, .09, .50), .07, seg=12, rings=8), R_COURT, 12, at=(-2.36, 3.36, .14), turn=(0, 26, -10)))
    body.append(paint(rounded((.44, .09, .62), .07, seg=12, rings=8), R_COURT, 12, at=(2.10, 3.34, -.20), turn=(0, -38, 10)))
    body.append(paint(rounded((.40, .08, .34), .06, seg=12, rings=8), R_COURT, 12, at=(1.06, 2.96, 1.50), turn=(30, 14, 0)))

    # The left arm, planted: shoulder to wrist in one taper that thickens toward the fist, a dark bracer round it, then
    # the fist, a stone with three knuckles along its front edge. No fingers, no thumb.
    body.append(limb([(-3.05, .95, 2.60), (-3.00, 1.50, 1.95), (-2.82, 2.25, 1.15)], [.64, .52, .56], R_ARM, seg=14))
    body.append(stone((.64, .38, .64), .26, [((-1, 1, .3), .9)], R_PLATE, seg=16, rings=10, at=(-2.98, 1.52, 1.93), turn=(-48, 0, 0)))
    body.append(stone((.76, .56, .74), .34, [((-1, 1, .6), .86), ((1, 1, .3), .9), ((0, .3, 1), .93), ((-1, -.2, -1), .9)], R_ROCK, seg=18, rings=10, at=(-3.08, .52, 3.00), turn=(0, 10, 0)))
    for x, y, z in ((-3.56, .98, 3.50), (-3.12, 1.02, 3.60), (-2.68, .98, 3.64)):
        body.append(paint(ellipsoid((.21, .17, .23), seg=10, rings=6), R_KNUCKLE, 10, at=(x, y, z)))
    body.append(cord([(-3.60, 1.02, 2.50), (-3.05, 1.12, 2.26), (-2.50, 1.02, 2.50), (-2.52, .98, 2.96)], .08, SEAM))
    body.append(limb([(-3.20, 2.20, .70), (-3.60, 2.42, .38), (-3.84, 2.80, .16), (-3.86, 2.98, .10)], [.26, .19, .10, .045], R_HORN, seg=10))   # the elbow's claw
    # ⚠️ WHERE HE STANDS (owner, 2026-10-10, of the court plate he used to ride up on: "i feel like this should dissolve
    # into rocks and have dante actually stand on the longhorn"). A flat crown of rock on the top of its back, between
    # the shoulders, with two low stones behind it like the cantle of a saddle. Its top is y = 3.40 exactly, and the
    # empty node `stand` sits on it: the cutscene and live play both put his feet there.
    body.append(stone((1.30, .15, 1.20), .13, [((-1, 1, 1), .9), ((1, 1, -1), .9), ((1, .6, 1), .93)], R_PLATE, seg=18, rings=8, at=(0, 3.25, .10)))
    body.append(stone((.42, .24, .34), .18, [((-1, 1, -.5), .88)], R_ROCK, seg=12, rings=8, at=(-.78, 3.42, -1.12), turn=(0, 20, 0)))
    body.append(stone((.36, .20, .30), .16, [((1, 1, -.5), .88)], R_ROCK, seg=12, rings=8, at=(.72, 3.38, -1.18), turn=(0, -25, 0)))
    m.add("body", body)
    m.add("stand", [(ellipsoid((.02, .02, .02), at=(0, -.06, 0), seg=4, rings=3), CRUST)], at=(0, 3.40, .10))

    # ---------------------------------------------------------------- the right arm, the one that comes down
    arm_at = (2.75, 2.50, .60)
    arm = [stone((.62, .58, .62), .36, [((1, 1, .4), .88), ((1, -.3, -1), .9)], R_PLATE, seg=16, rings=10, at=(2.75, 2.50, .60)),      # the joint's own boulder
           limb([(2.75, -.20, .60), (2.75, 1.05, .64), (2.75, 2.35, .60)], [.64, .50, .54], R_ARM, seg=14),
           stone((.62, .38, .62), .26, [((1, 1, .3), .9)], R_PLATE, seg=16, rings=10, at=(2.75, 1.00, .60)),                           # the bracer
           stone((.76, .56, .72), .34, [((1, -1, .6), .86), ((-1, -1, .3), .9), ((0, -.3, 1), .93), ((1, .2, -1), .9)], R_ROCK, seg=18, rings=10, at=(2.75, -.80, .60)),
           paint(ellipsoid((.21, .17, .24), seg=10, rings=6), R_KNUCKLE, 10, at=(2.30, -1.30, 1.00)),                                    # its three knuckles
           paint(ellipsoid((.21, .17, .24), seg=10, rings=6), R_KNUCKLE, 10, at=(2.75, -1.34, 1.06)),
           paint(ellipsoid((.21, .17, .24), seg=10, rings=6), R_KNUCKLE, 10, at=(3.20, -1.30, 1.00)),
           cord([(2.18, -.28, .86), (2.75, -.22, 1.16), (3.32, -.28, .86), (3.30, -.26, .30)], .08, SEAM),                             # the wrist
           cord([(2.60, -.50, 1.30), (2.82, -.84, 1.34), (2.66, -1.12, 1.26)], .06, SEAM),                                             # one crack down the face of the fist
           cord([(2.66, -.60, 1.36), (2.82, -.84, 1.40), (2.72, -1.02, 1.34)], .028, SEAM_HOT, seg=6),
           cord([(2.30, -.26, 1.00), (2.75, -.22, 1.24), (3.20, -.26, 1.00)], .035, SEAM_HOT, seg=6),
           limb([(2.95, 2.30, .10), (3.30, 2.52, -.26), (3.48, 2.90, -.50), (3.48, 3.08, -.56)], [.26, .19, .10, .045], R_HORN, seg=10)]
    m.add("arm-r", [off(p, arm_at) for p in arm], at=arm_at)

    # ---------------------------------------------------------------- the head
    # A wide low stone. A drawn face, as the cast's are, but a stern one: two ember eyes slanted down to the middle,
    # each in a dark hollow under one heavy brow; a pale muzzle with two nostrils; the long horns.
    head_at = (0.0, 1.00, 1.90)
    head = [stone((1.14, .80, .84), .5, [((-1, 1, .2), .88), ((1, 1, .2), .88), ((0, 1, -1), .9), ((-1, -.6, .4), .93), ((1, -.6, .4), .93), ((0, .2, 1), .97)], R_ROCK,
                  seg=24, rings=12, at=(0, 1.16, 2.50)),
            stone((.80, .16, .56), .14, [((0, 1, 1), .9)], R_PLATE, seg=14, rings=8, at=(0, 1.96, 2.40)),                              # its crown
            paint(rounded((.70, .36, .30), .24, seg=16, rings=10), R_MUZZLE, 16, "axis", at=(0, .86, 3.42))]                            # the muzzle
    face = [(moved(ellipsoid((.34, .19, .07), seg=12, rings=6), at=(-.50, 1.31, 3.30), turn=(0, 0, -10)), CRUST),                      # the hollows
            (moved(ellipsoid((.25, .105, .06), seg=12, rings=6), at=(-.50, 1.30, 3.35), turn=(0, 0, -10)), EYE),                        # the eyes
            (moved(ellipsoid((.10, .06, .05), seg=8, rings=5), at=(-.44, 1.29, 3.39), turn=(0, 0, -10)), EYE_HOT),
            limb([(-.06, 1.50, 3.42), (-.50, 1.66, 3.44), (-.98, 1.78, 3.26), (-1.16, 1.74, 3.00)], [.16, .21, .17, .08], R_BROW, seg=10),   # the brow
            (ellipsoid((.10, .07, .05), at=(-.30, .98, 3.71), seg=8, rings=5), CRUST),                                                 # a nostril
            cord([(-1.20, 1.34, 2.86), (-1.26, 1.04, 2.80), (-1.18, .78, 2.90)], .05, SEAM, seg=6),                                     # a crack down the cheek
            limb([(-.92, 1.60, 2.38), (-1.50, 1.74, 2.44), (-1.98, 2.20, 2.56), (-2.10, 2.82, 2.68), (-2.04, 3.24, 2.76)], [.42, .37, .27, .15, .05], R_HORN, seg=14)]
    head += face + [mir(p) for p in face]
    head.append((moved(ellipsoid((.60, .05, .40), seg=10, rings=5), at=(0, .49, 3.28)), MOUTH))                                        # the roof of the mouth
    m.add("head", [off(p, head_at) for p in head], at=head_at)

    # The lower jaw, hinged at the back of the mouth: an underbite with two blunt tusks.
    jaw_at = (0.0, .55, 2.75)
    jaw = [stone((.78, .22, .58), .2, [((0, -1, 1), .9), ((-1, -1, 0), .9), ((1, -1, 0), .9)], R_PLATE, seg=16, rings=8, at=(0, .32, 3.26)),
           (moved(ellipsoid((.58, .04, .40), seg=10, rings=5), at=(0, .52, 3.26)), MOUTH)]
    tusk = lathe([(.16, 0), (.15, .22), (.09, .44), (0, .56)], seg=10)
    jaw.append(paint(tusk, R_HORN, 10, "axis", at=(-.54, .44, 3.68), turn=(8, 0, 6)))
    jaw.append(paint(tusk, R_HORN, 10, "axis", at=(.54, .44, 3.68), turn=(8, 0, -6)))
    m.add("jaw", [off(p, jaw_at) for p in jaw], at=tuple(a - b for a, b in zip(jaw_at, head_at)), parent="head")

    finish(m)

    # ---------------------------------------------------------------- the fist alone, for live play
    # CONTINENTAL DRIFT in play is five blasts up the court, and each is this fist coming down (`Visual.DanteDriftFist`).
    # The same fist, wrist, forearm and bracer as the arm above, and the same paint. One node, `fist`; its origin is under
    # the knuckles, in the middle of the fist, so the thing is set down ON a point and stands straight up from it.
    f = Model("longhorn-fist")
    f.folder, f.texture = m.folder, m.texture
    f.add("fist", [off(p, (2.75, -1.36, .60)) for p in arm[1:-1]])
    finish(f)


def finish(m):
    # Typed in the stage's space; flipped for the file (see the top).
    m.parts = [(name, [(kit.mirror(mesh), paint_or_slot) for mesh, paint_or_slot in pieces], (-at[0], at[1], at[2]), parent) for name, pieces, at, parent in m.parts]
    # Review only (`--small=0.2`): the review's camera stands two metres off, inside a thing this size, so the picture
    # is taken of it shrunk. The game's model is never built with this.
    small = float(arg("small", 1))
    if small != 1:
        m.parts = [(name, [((mesh[0] * small, mesh[1], mesh[2]), paint_or_slot) for mesh, paint_or_slot in pieces], tuple(v * small for v in at), parent)
                   for name, pieces, at, parent in m.parts]
    m.write(PALETTE)


if __name__ == "__main__":
    main()
