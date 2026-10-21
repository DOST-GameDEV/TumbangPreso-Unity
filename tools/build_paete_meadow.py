"""MARIANG MAKILING'S MEADOW, remodelled and painted (`Visual.PaeteMeadow`, the ultimate's cutscene).

Owner, 2026-10-08, after the tree, the pitcher and the rattan were remodelled: "remodel makiling and the meadow too".
The first meadow (`tools/build_paete_props.py` `meadow`, kept at ArtSource/paete/props-pre-rework/meadow.glb) was
faceted cards in flat fills: leaf cards for grass, boxes for flower hearts and puffs, low green lumps for moss. This one
is built with the hand companions' kit like the other three (docs/CHARACTER_REDESIGN_DANTE.md 15.11): round plump
forms, smooth normals, and a painted atlas for every piece. ⚠️ Unlike those three it uses NO flat palette cell, not
even for the flowers' gold hearts: see the note in `sampaguita` (flat cells drew black in the second film).

The SAME plants in the SAME places, because each was chosen on purpose and the cutscene is directed round them:
moss in mounds, grass, pako ferns that unroll, sampaguita (her wreath's own flower), gumamela, and makahiya that folds
shut when his palms hit the court. Gentle and alive, no faces: this is the mountain's meadow, not a cast of characters.

  py -3 tools/build_paete_meadow.py [--out=folder]
  blender -b --python tools/review_hand_companion.py -- --hero=meadow --file=<folder>/meadow.glb
      --atlas=<folder>/meadow-atlas.png --version=m1 --ink=0.008 --size=6.6 --centre=-0.55,0.2,-0.1
  py -3 tools/hand_companion_kit.py sheet meadow m1

Writes Assets/TumbangPreso/Resources/Models/PaeteProps/meadow.glb and meadow-atlas.png.

THE CONTRACT with `PaeteMeadow` (unchanged; it finds everything by NAME and poses each node about its own origin):
  moss-N            a cushion, origin on the court: spread in x and z, raised in y.
  grass-N           a tuft, origin on the court: sprung up, bowed about its foot.
  fern-N            a fern's crown, origin on the court; under it, chained, `fern-N-fK-a` / `-b` / `-c`: one frond in
                    three segments, each along its own +z and rising, each origin the end of the one before. The class
                    pitches each about x to curl the frond into a fiddlehead, so only `-a` carries the frond's yaw.
  samp-N, gum-N     a bush, origin on the court; `samp-N-bK` / `gum-N-bK` its blossoms (scaled from a quarter to open).
  maka-N            a makahiya, origin on the court; `maka-N-lK` a leaf along its +z from (0, 0.03, 0), which the class
                    pitches up about x and narrows in x to fold shut; `maka-N-pK` a puff (scaled about its middle).
⚠️ No other node may begin `moss-`, `grass-`, `fern-`, `samp-`, `gum-` or `maka-`: the class would pose it too.
⚠️ THE LAYOUT IS TYPED IN HIS SPACE (+x his right, +z ahead) AND `mx` MIRRORS EACH PLANT'S GROUND POINT FOR THE FILE,
because glTFast negates x on import (the first meadow's lesson, `paete_meadow_v4.png`).

Metres, y up. PALETTE is `PaeteMeadow.Palette`, same order, same values: the paint is mixed round those colours and
the review needs the list, but no piece is a palette cell. The class never re-dresses the meadow (it wilts by
folding, curling and sinking, not by drying), so the paint needs no tint.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, tube  # noqa: E402
from build_paete_bloom import arg, hexc, inset, painted, smooth01, turned  # noqa: E402

PALETTE = ["#5E8A2E", "#44701F", "#78B040", "#4E8A2C", "#4E8E34", "#72AA40", "#F8F5EA", "#EAD27E",
           "#1E140C", "#5E7A2C", "#E88AB8", "#FAD8EA", "#6E9E36", "#D2344A", "#E8E2C6", "#2F5E22"]
MOSS, MOSS_DK, GRASS, GRASS_DK, FERN, FERN_LT, PETAL, HEART, INK, STEM, PINK, PINK_TIP, MIMOSA, GUMAMELA, BUD, LEAF_DK = range(16)

ATLAS = 2048
# The atlas's upper half, in eighths across and two rows down. (u0, v0, u1, v1), v down.
R_GRASS_A, R_GRASS_B = (0.000, 0.0, 0.125, 0.25), (0.125, 0.0, 0.250, 0.25)
R_PINNA_A, R_PINNA_B = (0.250, 0.0, 0.375, 0.25), (0.375, 0.0, 0.500, 0.25)
R_SAMP_LEAF, R_GUM_LEAF = (0.500, 0.0, 0.625, 0.25), (0.625, 0.0, 0.750, 0.25)
R_PETAL, R_GUM_PETAL = (0.750, 0.0, 0.875, 0.25), (0.875, 0.0, 1.000, 0.25)
R_MOSS_A, R_MOSS_B = (0.000, 0.25, 0.125, 0.5), (0.125, 0.25, 0.250, 0.5)
R_RACHIS, R_CROZIER = (0.2500, 0.25, 0.3125, 0.5), (0.3125, 0.25, 0.3750, 0.5)
R_TWIG, R_BUD = (0.3750, 0.25, 0.4375, 0.5), (0.4375, 0.25, 0.5000, 0.5)
R_LEAFLET, R_PUFF = (0.5000, 0.25, 0.5625, 0.5), (0.5625, 0.25, 0.6500, 0.5)
R_GOLD, R_RED = (0.6500, 0.25, 0.66875, 0.5), (0.66875, 0.25, 0.6875, 0.5)
R_STAMEN, R_COLUMN = (0.6875, 0.25, 0.7500, 0.5), (0.7500, 0.25, 0.8125, 0.5)
R_GUM_BUD, R_SEED = (0.8125, 0.25, 0.8750, 0.5), (0.8750, 0.25, 0.9375, 0.5)
R_MAKA_STEM = (0.9375, 0.25, 1.0000, 0.5)


# ---------------------------------------------------------------------------------------------- the paint

def paint_atlas(path):
    from PIL import Image, ImageDraw, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc(PALETTE[GRASS_DK])

    def grid(rect):
        x0, y0, x1, y1 = (int(v * ATLAS) for v in rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.meshgrid(u, t), (x0, y0, x1, y1)

    def mix(base, colour, weight):
        return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]

    def flat(u, colour):
        return np.zeros(u.shape + (3,), np.float32) + hexc(colour)

    # ---- a grass blade, both faces (a face's midrib is u = 0.25 and 0.75): deep at its foot, sunlit at its tip,
    # a pale keel, the fine parallel veins a grass has. B is the older, bluer blade with a dry tip.
    for rect, foot, body, tip, keel, dry in ((R_GRASS_A, "#3F7A26", "#78B040", "#B4DC5C", "#D2EE86", 0.0),
                                             (R_GRASS_B, "#2F6424", "#4E8A2C", "#86BC46", "#A8D464", 0.75)):
        (u, t), (x0, y0, x1, y1) = grid(rect)
        half = np.abs(((u * 2) % 1.0) - 0.5) * 2
        c = flat(u, foot)
        c = mix(c, body, smooth01(0.02, 0.45, t))
        c = mix(c, tip, smooth01(0.50, 1.0, t) * 0.85)
        c = mix(c, foot, smooth01(0.74, 1.0, half) * 0.45)
        c = mix(c, keel, np.exp(-((half - 0.42) / 0.05) ** 2) * 0.28)
        c = mix(c, keel, np.exp(-((half - 0.70) / 0.04) ** 2) * 0.18)
        c = mix(c, keel, (1 - smooth01(0.0, 0.09, half)) * 0.85)
        c = mix(c, "#D8C27A", smooth01(0.90, 1.0, t) * dry)
        img[y0:y1, x0:x1] = c

    # ---- a leaf with a midrib and side veins swept to its tip, both faces. One painter, four leaves.
    def leafpaint(rect, lo, hi, vein, edge, count, sweep, gloss=0.0, teeth=0.0):
        (u, t), (x0, y0, x1, y1) = grid(rect)
        half = np.abs(((u * 2) % 1.0) - 0.5) * 2
        c = flat(u, lo)
        c = mix(c, hi, smooth01(0.0, 0.9, t) * 0.85)
        rim = smooth01(0.70, 1.0, half)
        c = mix(c, edge, rim * (0.55 + teeth * 0.4 * np.cos(t * 2 * np.pi * 11)))
        side = np.abs(((t * count - half * sweep) % 1.0) - 0.5) * 2
        c = mix(c, vein, (1 - smooth01(0.0, 0.15, side)) * (1 - smooth01(0.55, 0.92, half)) * 0.45)
        # The shine a waxy leaf carries beside its midrib, on one side only.
        c = mix(c, "#C8E89A", np.exp(-(((u % 0.5) - 0.34) / 0.035) ** 2 - ((t - 0.56) / 0.22) ** 2) * gloss)
        c = mix(c, vein, (1 - smooth01(0.0, 0.07, half)) * 0.9)
        img[y0:y1, x0:x1] = c

    leafpaint(R_PINNA_A, "#356F2A", "#5E9E38", "#A2D060", "#2A5A22", 9.0, 1.2)             # a grown pinna
    leafpaint(R_PINNA_B, "#4E8E34", "#8CC24A", "#D0EC8C", "#3F7A2C", 9.0, 1.2)             # a young, paler one
    leafpaint(R_SAMP_LEAF, "#25521F", "#3F7E2C", "#8CC456", "#1B3E1A", 6.0, 1.9, gloss=0.55)   # dark and waxy
    leafpaint(R_GUM_LEAF, "#3A7628", "#69A83A", "#B8DE6A", "#2A5E22", 8.0, 1.5, gloss=0.25, teeth=1.0)

    # ---- a sampaguita's petal: u is 0 down a petal's middle and 1 in the notch between two, t is 0 at the heart and
    # 1 at the tip. White, warm cream toward the heart, a cool shade in the notch so the petals part, a faint line.
    (u, t), (x0, y0, x1, y1) = grid(R_PETAL)
    c = flat(u, "#FFFDF6")
    c = mix(c, "#F3E2A2", (1 - smooth01(0.05, 0.42, t)) * 0.85)
    c = mix(c, "#D9D6BC", smooth01(0.62, 1.0, u) * (1 - smooth01(0.80, 1.0, t)) * 0.65)
    c = mix(c, "#E9E1C2", (1 - smooth01(0.0, 0.08, u)) * smooth01(0.25, 0.5, t) * 0.5)
    img[y0:y1, x0:x1] = c
    # ---- a gumamela's petal, the same way round: a deep wine throat, red, a lit pink edge, veins fanning out.
    (u, t), (x0, y0, x1, y1) = grid(R_GUM_PETAL)
    c = flat(u, "#D2344A")
    c = mix(c, "#F0687A", smooth01(0.62, 1.0, t) * 0.7)
    c = mix(c, "#6E0F26", (1 - smooth01(0.08, 0.40, t)) * 0.92)
    fan = np.abs(((u * 3.0) % 1.0) - 0.5) * 2
    c = mix(c, "#A01C38", (1 - smooth01(0.0, 0.22, fan)) * (1 - smooth01(0.55, 0.95, t)) * 0.5)
    c = mix(c, "#98182F", smooth01(0.78, 1.0, u) * 0.6)
    c = mix(c, "#FF9AA6", smooth01(0.92, 1.0, t) * (1 - smooth01(0.5, 0.9, u)) * 0.5)
    img[y0:y1, x0:x1] = c

    # ---- moss: a mound seen from above is its top (t = 1) and its skirt (t = 0). Soft tufts, deep at the skirt.
    for rect, deep, body, lit in ((R_MOSS_A, "#3A6420", "#5E8A2E", "#8CB844"), (R_MOSS_B, "#2C5219", "#44701F", "#6C9A34")):
        (u, t), (x0, y0, x1, y1) = grid(rect)
        c = flat(u, body)
        # Blotches, not ribs: m1's tufts ran straight up a mound and it read as a melon. Each wave is bent by the other
        # axis, and all of it fades out toward the top, where the round of the mound pinches to a point.
        even = 1 - smooth01(0.78, 0.98, t)
        tuft = (0.5 + 0.5 * np.sin(u * 2 * np.pi * 4 + np.sin(t * 11.0) * 2.6)) * (0.5 + 0.5 * np.sin(t * 2 * np.pi * 3.5 + np.sin(u * 2 * np.pi * 3) * 2.2))
        c = mix(c, lit, tuft * 0.50 * even)
        c = mix(c, deep, (0.5 + 0.5 * np.sin(u * 2 * np.pi * 5 + 1.3 + np.sin(t * 17.0) * 2.0)) ** 3 * (0.5 + 0.5 * np.sin(t * 2 * np.pi * 5.5 + 0.7)) * 0.40 * even)
        c = mix(c, lit, smooth01(0.62, 1.0, t) * 0.35)
        c = mix(c, deep, (1 - smooth01(0.0, 0.34, t)) * 0.85)
        img[y0:y1, x0:x1] = c

    # ---- a fern's stalk: brown and scaly where it leaves the crown, green above. Three segments share it, a third each.
    (u, t), (x0, y0, x1, y1) = grid(R_RACHIS)
    c = flat(u, "#6C7C30")
    c = mix(c, "#86B446", smooth01(0.30, 1.0, t) * 0.9)
    c = mix(c, "#7A5630", (1 - smooth01(0.0, 0.22, t)) * 0.9)
    c = mix(c, "#B4DA6A", (0.5 + 0.5 * np.cos((u - 0.75) * 2 * np.pi)) ** 4 * 0.4)
    for at in (0.04, 0.09, 0.15):
        c = mix(c, "#4A3018", np.exp(-((t - at) / 0.010) ** 2) * 0.6)
    img[y0:y1, x0:x1] = c
    # ---- a fiddlehead still rolled: furred brown, greening at its coil.
    (u, t), (x0, y0, x1, y1) = grid(R_CROZIER)
    c = flat(u, "#7A5630")
    c = mix(c, "#A47A44", (0.5 + 0.5 * np.sin(t * 2 * np.pi * 14 + u * 2 * np.pi * 3)) * 0.45)
    c = mix(c, "#6E9A3A", smooth01(0.16, 0.42, t) * 0.92)
    c = mix(c, "#A6D25C", smooth01(0.55, 1.0, t) * 0.7)
    img[y0:y1, x0:x1] = c
    # ---- a bush's twig: wood at its foot, green where it is new, a ring at each joint.
    (u, t), (x0, y0, x1, y1) = grid(R_TWIG)
    c = flat(u, "#6A5230")
    c = mix(c, "#5E7A2C", smooth01(0.35, 0.90, t) * 0.9)
    c = mix(c, "#8E7444", (0.5 + 0.5 * np.cos((u - 0.25) * 2 * np.pi)) ** 3 * 0.35)
    for at in (0.22, 0.47, 0.70):
        c = mix(c, "#3E2E18", np.exp(-((t - at) / 0.012) ** 2) * 0.55)
    img[y0:y1, x0:x1] = c
    # ---- a sampaguita bud: a green cup, cream, a white point; the faint seams of its furled petals.
    (u, t), (x0, y0, x1, y1) = grid(R_BUD)
    c = flat(u, "#E8E2C6")
    c = mix(c, "#FFFDF6", smooth01(0.45, 1.0, t) * 0.9)
    seam = np.abs(((u * 4 + t * 1.2) % 1.0) - 0.5) * 2
    c = mix(c, "#CFC8A6", (1 - smooth01(0.0, 0.16, seam)) * smooth01(0.30, 0.5, t) * 0.5)
    c = mix(c, "#5E8A2E", (1 - smooth01(0.16 + 0.05 * np.cos(u * 2 * np.pi * 5), 0.26 + 0.05 * np.cos(u * 2 * np.pi * 5), t)))
    img[y0:y1, x0:x1] = c
    # ---- a makahiya leaf, the whole feather as one blade (`feather`): across is the midrib (u = 0.25 and 0.75) out to
    # the leaflets' tips, up is the gap between two leaflets (t = 0) to a leaflet's middle (t = 1). Soft green, paler
    # and yellower at the tips, a deep gap so the leaflets part, a plum midrib.
    (u, t), (x0, y0, x1, y1) = grid(R_LEAFLET)
    half = np.abs(((u * 2) % 1.0) - 0.5) * 2
    c = flat(u, "#4F8A30")
    c = mix(c, "#7FB23E", smooth01(0.10, 0.60, half))
    c = mix(c, "#B4DA62", smooth01(0.60, 1.0, half) * 0.75)
    c = mix(c, "#2C5A22", (1 - smooth01(0.0, 0.34, t)) * smooth01(0.10, 0.30, half) * 0.85)
    c = mix(c, "#C6E67C", smooth01(0.70, 1.0, t) * (1 - smooth01(0.2, 0.7, half)) * 0.25)
    c = mix(c, "#9A4A62", (1 - smooth01(0.0, 0.10, half)) * 0.85)
    img[y0:y1, x0:x1] = c
    # ---- a makahiya puff: deeper underneath, lit on top. Its pale flecks are typed below.
    (u, t), (x0, y0, x1, y1) = grid(R_PUFF)
    c = flat(u, "#E88AB8")
    c = mix(c, "#C2569A", (1 - smooth01(0.0, 0.42, t)) * 0.85)
    c = mix(c, "#F6B4D6", smooth01(0.58, 1.0, t) * 0.7)
    img[y0:y1, x0:x1] = c
    # ---- the flowers' gold (a sampaguita's heart, a gumamela's anthers: pollen, paler on top) and the red knob at
    # the end of the gumamela's column. See the note on `R_GOLD` in `sampaguita` for why these are paint.
    (u, t), (x0, y0, x1, y1) = grid(R_GOLD)
    c = flat(u, "#D9B45A")
    c = mix(c, "#EAD27E", smooth01(0.15, 0.55, t))
    c = mix(c, "#F8ECB0", smooth01(0.70, 1.0, t) * 0.7)
    img[y0:y1, x0:x1] = c
    (u, t), (x0, y0, x1, y1) = grid(R_RED)
    c = flat(u, "#B0243C")
    c = mix(c, "#D2344A", smooth01(0.10, 0.55, t))
    c = mix(c, "#F0687A", smooth01(0.70, 1.0, t) * 0.6)
    img[y0:y1, x0:x1] = c
    # ---- one tuft of the puff's fuzz: pink where it leaves the ball, pale at its end.
    (u, t), (x0, y0, x1, y1) = grid(R_STAMEN)
    c = flat(u, "#E88AB8")
    c = mix(c, "#F6B4D6", smooth01(0.25, 0.65, t))
    c = mix(c, "#FFE6F3", smooth01(0.62, 0.95, t))
    img[y0:y1, x0:x1] = c
    # ---- the gumamela's column: wine in the throat, red, pink where the anthers are.
    (u, t), (x0, y0, x1, y1) = grid(R_COLUMN)
    c = flat(u, "#8E1830")
    c = mix(c, "#D2344A", smooth01(0.10, 0.45, t))
    c = mix(c, "#F58C98", smooth01(0.55, 1.0, t) * 0.9)
    img[y0:y1, x0:x1] = c
    # ---- a gumamela bud: a green calyx in five points, and the red furled tight above it.
    (u, t), (x0, y0, x1, y1) = grid(R_GUM_BUD)
    c = flat(u, "#D2344A")
    furl = np.abs(((u * 5 + t * 2.2) % 1.0) - 0.5) * 2
    c = mix(c, "#9A1C34", (1 - smooth01(0.0, 0.22, furl)) * 0.6)
    c = mix(c, "#F0687A", smooth01(0.80, 1.0, t) * 0.5)
    c = mix(c, "#4E8A2C", (1 - smooth01(0.30 + 0.10 * np.cos(u * 2 * np.pi * 5), 0.36 + 0.10 * np.cos(u * 2 * np.pi * 5), t)))
    img[y0:y1, x0:x1] = c
    # ---- a grass seed: green going to straw, a husk line.
    (u, t), (x0, y0, x1, y1) = grid(R_SEED)
    c = flat(u, "#9CBC52")
    c = mix(c, "#E6CE82", smooth01(0.25, 0.9, t) * 0.9)
    c = mix(c, "#B49650", (0.5 + 0.5 * np.cos(u * 2 * np.pi * 3)) ** 4 * 0.4)
    img[y0:y1, x0:x1] = c
    # ---- the makahiya's stem: the plum red of the real plant at its foot, green toward its leaves.
    (u, t), (x0, y0, x1, y1) = grid(R_MAKA_STEM)
    c = flat(u, "#9A4A62")
    c = mix(c, "#6E9E36", smooth01(0.40, 1.0, t) * 0.9)
    c = mix(c, "#C27890", (0.5 + 0.5 * np.cos((u - 0.25) * 2 * np.pi)) ** 3 * 0.35 * (1 - smooth01(0.4, 0.8, t)))
    img[y0:y1, x0:x1] = c

    picture = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(picture, "RGBA")

    def dots(rect, marks, colour):
        x0, y0, x1, y1 = (int(v * ATLAS) for v in rect)
        for u_at, t_at, r in marks:
            cx, cy = x0 + u_at * (x1 - x0), y0 + (1 - t_at) * (y1 - y0)
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=colour)

    # Pale new tips on the moss, each typed: where round, how high, how big. Thicker toward the top of a mound.
    dots(R_MOSS_A, [(0.06, 0.82, 5), (0.15, 0.66, 4), (0.21, 0.90, 6), (0.30, 0.74, 4), (0.37, 0.58, 3), (0.44, 0.86, 5), (0.52, 0.70, 4),
                    (0.58, 0.93, 5), (0.66, 0.62, 3), (0.71, 0.80, 6), (0.79, 0.69, 4), (0.86, 0.88, 5), (0.93, 0.60, 3), (0.97, 0.77, 4),
                    (0.11, 0.48, 3), (0.48, 0.50, 3), (0.83, 0.47, 3), (0.26, 0.42, 2), (0.62, 0.40, 2)], (200, 226, 122, 215))
    dots(R_MOSS_B, [(0.08, 0.72, 4), (0.19, 0.86, 5), (0.27, 0.62, 3), (0.36, 0.91, 5), (0.46, 0.68, 4), (0.55, 0.82, 5), (0.63, 0.57, 3),
                    (0.72, 0.90, 4), (0.80, 0.73, 5), (0.89, 0.60, 3), (0.95, 0.85, 4), (0.41, 0.46, 2), (0.76, 0.44, 2)], (150, 190, 92, 210))
    # Pale flecks all over a puff (the tips of its filaments), and a few deep ones low down.
    dots(R_PUFF, [(0.04, 0.70, 5), (0.11, 0.52, 4), (0.17, 0.84, 5), (0.24, 0.62, 5), (0.30, 0.40, 4), (0.36, 0.78, 5), (0.43, 0.55, 4),
                  (0.49, 0.90, 5), (0.56, 0.68, 5), (0.62, 0.44, 4), (0.68, 0.82, 5), (0.75, 0.58, 4), (0.81, 0.36, 4), (0.87, 0.74, 5),
                  (0.93, 0.50, 4), (0.98, 0.88, 5), (0.20, 0.30, 3), (0.52, 0.28, 3), (0.72, 0.24, 3)], (255, 232, 246, 225))
    dots(R_PUFF, [(0.08, 0.18, 4), (0.33, 0.14, 4), (0.60, 0.16, 3), (0.90, 0.20, 4)], (160, 60, 130, 200))
    # Brown scales on the fiddlehead.
    dots(R_CROZIER, [(0.12, 0.10, 5), (0.40, 0.18, 6), (0.70, 0.08, 5), (0.88, 0.24, 5), (0.25, 0.32, 4), (0.58, 0.36, 5), (0.80, 0.44, 4),
                     (0.10, 0.50, 4), (0.45, 0.56, 3), (0.66, 0.62, 3), (0.30, 0.70, 3), (0.85, 0.76, 3)], (122, 86, 44, 200))
    picture = picture.filter(ImageFilter.GaussianBlur(0.6))
    picture.save(path)
    return path


# ---------------------------------------------------------------------------------------------- shapes

def mx(x, z):
    """A ground point typed in his space, mirrored for the file (glTFast negates x on import)."""
    return (-x, 0.0, z)


def on(compass, r, y):
    """A point `r` out toward `compass` (0 = +z, 90 = +x) at height y."""
    a = math.radians(compass)
    return (r * math.sin(a), y, r * math.cos(a))


def bend(mesh, degrees, length):
    """A piece built along +z from the origin, bowed so its tip has turned `degrees` DOWN (negative: up) by `length`.
    A true arc: nothing stretches. What is behind the origin stays straight."""
    if abs(degrees) < 0.01:
        return mesh
    p, n, t = mesh
    k = math.radians(degrees) / length
    th = np.clip(p[:, 2], 0.0, None) * k
    back = np.minimum(p[:, 2], 0.0)
    c, s = np.cos(th), np.sin(th)
    pos = np.stack([p[:, 0], -(1 - c) / k + p[:, 1] * c, s / k + p[:, 1] * s + back], axis=1)
    nrm = np.stack([n[:, 0], n[:, 1] * c - n[:, 2] * s, n[:, 1] * s + n[:, 2] * c], axis=1)
    return pos.astype(np.float32), nrm.astype(np.float32), t


OVAL = [(0.0, 0.0), (0.46, 0.06), (0.92, 0.28), (1.0, 0.50), (0.84, 0.74), (0.42, 0.92), (0.0, 1.0)]      # a bush's plump leaf
POINTED = [(0.0, 0.0), (0.56, 0.06), (1.0, 0.26), (0.96, 0.46), (0.68, 0.72), (0.28, 0.91), (0.0, 1.0)]   # a gumamela's
BLADE = [(0.0, 0.0), (0.74, 0.04), (1.0, 0.20), (0.90, 0.44), (0.66, 0.68), (0.34, 0.88), (0.0, 1.0)]     # grass
PINNA = [(0.0, 0.0), (0.96, 0.22), (0.84, 0.62), (0.0, 1.0)]                                              # a fern's: blunt, four rings


def leaf(length, width, thick, rect, prof=OVAL, seg=8, droop=0.0, twist=0.0):
    """A plump leaf along +z from its stem at the origin, painted (stem at the bottom of `rect`, both faces), bowed
    down by `droop` degrees. `twist` rolls it about its length first, so it does not bow square to its face."""
    blade = lathe([(r * width * .5, y * length) for r, y in prof], seg=seg, squash_z=thick / width)
    uv = kit.grid_uv(blade, inset(rect), seg, "axis", 1)
    mesh = moved(blade, turn=(90, 0, 0))
    if twist:
        mesh = moved(mesh, turn=(0, 0, twist))
    return (bend(mesh, droop, length), uv)


def stand(piece, compass, pitch, at=(0, 0, 0)):
    """A piece built along +z, raised `pitch` degrees off the court and turned to `compass`, its foot at `at`."""
    return turned(piece, turn=(-pitch, compass, 0), at=at)


def part(rect, lo, hi):
    """The slice of `rect` from `lo` to `hi` of its height (0 its bottom, 1 its top)."""
    u0, v0, u1, v1 = rect
    return (u0, v1 - (v1 - v0) * hi, u1, v1 - (v1 - v0) * lo)


def cup(profile, lobes, radius, rect, notch, twirl=0.0, dip=0.0, per=4, sharp=1.0):
    """A flower as ONE surface: a dish turned from `profile` (radius, height; the rim is radius 1) whose rim is drawn
    in `lobes` times, so each petal is a lobe and no petal crosses another (the tree's sampaguita taught this: five
    leaf shapes crossing at the heart ink over each other and the flower is a black speck from behind). `twirl` leans
    the lobes into a pinwheel, `dip` drops the notches, `sharp` above 1 narrows the notch so the petal is rounder. Painted from `rect`: across is a petal's middle to the notch,
    up is the heart to the tip. Faces +y."""
    seg = lobes * per
    dish = lathe(profile, seg=seg)
    pos = dish[0].copy()
    reach = np.hypot(pos[:, 0], pos[:, 2])
    f = reach / float(reach.max())
    column = np.arange(len(pos)) % (seg + 1)
    # Half a step off a petal's middle, so its tip is an edge between two points (blunt and round) and not one point.
    phase = (column + 0.5) * (2 * math.pi / per) + twirl * f
    w = ((1 - np.cos(phase)) * 0.5) ** sharp            # 0 down a petal's middle, 1 in the notch
    lobe = 1 - notch * w * f ** 1.5
    pos[:, 0] *= lobe; pos[:, 2] *= lobe
    pos[:, 1] -= dip * w * f
    pos *= radius
    u0, v0, u1, v1 = inset(rect)
    across = np.arccos(np.clip(np.cos(phase), -1, 1)) / math.pi
    uv = np.stack([u0 + (u1 - u0) * across, v1 - (v1 - v0) * f], axis=1).astype(np.float32)
    return ((pos.astype(np.float32), dish[1], dish[2]), uv)


SAMPAGUITA = [(0.06, 0.0), (0.56, 0.06), (1.0, 0.20), (0.82, 0.29), (0.0, 0.18)]
GUMAMELA_CUP = [(0.07, 0.0), (0.15, 0.17), (0.40, 0.36), (0.78, 0.50), (1.0, 0.52), (0.86, 0.60), (0.46, 0.50), (0.18, 0.36), (0.0, 0.26)]


def sampaguita(radius, petals, turn, tilt, compass, twirl):
    """A sampaguita on its node: a white pinwheel of `petals` round a gold heart, tipped `tilt` toward `compass`."""
    how = dict(turn=(tilt, compass, 0))
    # ⚠️ ROUND PETALS (m1 and m2: a point at each petal's tip and an even notch drew a star, a daisy; the sampaguita's
    # are oblong with round ends). Six points to a petal, none on its middle, and a narrow notch.
    flower = cup(SAMPAGUITA, petals, radius, R_PETAL, notch=0.50, twirl=twirl, dip=0.05, per=6, sharp=1.7)
    flower = (moved(flower[0], turn=(0, turn, 0)), flower[1])
    # ⚠️ THE HEART IS PAINT, NOT PALETTE CELL 7 (the second film, `introfx md2`: every flat-cell piece of the meadow,
    # the hearts, the anthers and the column's knob, drew BLACK, while every painted piece was right; the first film
    # of the same pieces had been right). Measured with two more films: the same model with only its material RENAMED
    # drew gold again (`mdx1`), and then one pixel of the atlas changed, nothing else, drew black again (`mdx2`). So:
    # `ToonSkin` keeps one dressed material per source material for the editor's whole session; when the atlas is
    # reimported that dressed material loses its sixteen palette colours (an array, which Unity does not keep) and
    # nothing sets them again until scripts reload. Paint is a texture and survives. So this model asks the palette
    # for nothing; `PaeteMeadow.Palette` is still what the class passes and is simply unused.
    heart = painted(ellipsoid((radius * .20, radius * .13, radius * .20), seg=8, rings=4), R_GOLD, 8, "axis", 1)
    return [turned(flower, **how), turned(turned(heart, at=(0, radius * .21, 0)), **how)]


def gumamela(radius, turn, tilt, compass, column, anthers):
    """A gumamela on its node: five broad red petals in one trumpet, and the long column the flower is known by
    (`column`: how far it stands out and how it leans, typed), gold anthers round its end (`anthers`: how far along,
    which way round, each typed)."""
    how = dict(turn=(tilt, compass, 0))
    flower = cup(GUMAMELA_CUP, 5, radius, R_GUM_PETAL, notch=0.30, twirl=0.5, dip=0.07, per=5)
    flower = (moved(flower[0], turn=(0, turn, 0)), flower[1])
    reach, lean_x, lean_z = column
    spine = [(0.0, radius * .26, 0.0), (lean_x * .3, radius * .26 + reach * .4, lean_z * .3), (lean_x * .75, radius * .26 + reach * .78, lean_z * .75),
             (lean_x, radius * .26 + reach, lean_z)]
    pieces = [turned(flower, **how), turned(painted(tube(spine, [0.017, 0.015, 0.013, 0.012], seg=7), R_COLUMN, 7), **how)]
    for along, round_, size in anthers:
        a = math.radians(round_)
        at = tuple(spine[2][k] + (spine[3][k] - spine[2][k]) * along for k in range(3))
        at = (at[0] + 0.016 * math.sin(a), at[1], at[2] + 0.016 * math.cos(a))
        anther = painted(ellipsoid((size, size * 1.15, size), seg=7, rings=5), R_GOLD, 7, "axis", 1)
        pieces.append(turned(turned(anther, at=at), **how))
    tip = spine[3]
    knob = painted(ellipsoid((0.017, 0.015, 0.017), seg=7, rings=5), R_RED, 7, "axis", 1)
    pieces.append(turned(turned(knob, at=(tip[0], tip[1] + 0.012, tip[2])), **how))
    return pieces


def bud(length, girth, rect, lean, compass, at):
    """A closed bud: a drop standing on its stem, leant `lean` degrees toward `compass`."""
    drop = lathe([(girth * .55, 0.0), (girth, length * .22), (girth * .92, length * .48), (girth * .52, length * .80), (0.0, length)], seg=8)
    return turned(painted(drop, rect, 8, "axis", 1), turn=(lean, compass, 0), at=at)


def twig(points, radii):
    return painted(tube(points, radii, seg=7), R_TWIG, 7)


# ---------------------------------------------------------------------------------------------- the plants, typed

# MOSS: eighteen cushions where the first meadow had them, each a group of mounds that overlap and step in height
# (moss grows in mounds; a single low lump read as a lily pad on the plaza). Rounder and taller than the first.
# Each: its ground point, then its mounds as (dx, dz), (rx, ry, rz), turn, tilt, which paint.
CUSHIONS = [
    # round her hem
    ((0.78, -1.02), [((0.00, 0.00), (0.215, 0.135, 0.180), 12, 5, R_MOSS_A), ((0.185, 0.085), (0.140, 0.100, 0.118), 64, -7, R_MOSS_B),
                     ((-0.130, 0.140), (0.118, 0.082, 0.104), 133, 6, R_MOSS_A)]),
    ((1.28, -1.42), [((0.00, 0.00), (0.178, 0.118, 0.150), 52, -4, R_MOSS_B), ((-0.160, -0.055), (0.126, 0.094, 0.112), 17, 8, R_MOSS_A)]),
    ((0.52, -1.58), [((0.00, 0.00), (0.190, 0.142, 0.162), 118, 3, R_MOSS_A), ((0.140, -0.125), (0.122, 0.092, 0.130), 204, -6, R_MOSS_B),
                     ((-0.150, -0.075), (0.104, 0.078, 0.096), 296, 9, R_MOSS_A), ((0.028, 0.170), (0.094, 0.070, 0.084), 37, -5, R_MOSS_B)]),
    ((1.10, -0.70), [((0.00, 0.00), (0.146, 0.102, 0.124), 197, 6, R_MOSS_B), ((0.128, 0.066), (0.106, 0.080, 0.094), 274, -8, R_MOSS_A)]),
    ((1.52, -0.98), [((0.00, 0.00), (0.166, 0.116, 0.140), 247, -3, R_MOSS_A), ((-0.138, 0.104), (0.118, 0.090, 0.106), 333, 7, R_MOSS_B),
                     ((0.124, 0.082), (0.096, 0.072, 0.086), 8, -9, R_MOSS_A)]),
    ((0.94, -1.86), [((0.00, 0.00), (0.200, 0.128, 0.156), 304, 4, R_MOSS_B), ((0.172, 0.046), (0.132, 0.096, 0.118), 23, -6, R_MOSS_A),
                     ((-0.086, -0.150), (0.108, 0.080, 0.098), 158, 8, R_MOSS_A)]),
    # between her and him
    ((0.46, -0.44), [((0.00, 0.00), (0.134, 0.098, 0.146), 28, -5, R_MOSS_A), ((-0.118, 0.092), (0.098, 0.076, 0.100), 112, 7, R_MOSS_B)]),
    ((0.18, -0.86), [((0.00, 0.00), (0.156, 0.112, 0.126), 147, 4, R_MOSS_B), ((0.136, -0.072), (0.110, 0.084, 0.098), 228, -7, R_MOSS_A),
                     ((-0.104, -0.106), (0.088, 0.068, 0.086), 63, 6, R_MOSS_A)]),
    ((0.70, 0.12), [((0.00, 0.00), (0.124, 0.094, 0.112), 83, 6, R_MOSS_A), ((0.104, 0.084), (0.088, 0.070, 0.080), 168, -8, R_MOSS_B)]),
    ((-0.30, -0.52), [((0.00, 0.00), (0.144, 0.100, 0.132), 222, -4, R_MOSS_A), ((0.124, 0.078), (0.098, 0.078, 0.102), 297, 9, R_MOSS_B)]),
    # behind him to his left
    ((-0.82, -0.92), [((0.00, 0.00), (0.186, 0.130, 0.146), 68, 3, R_MOSS_B), ((0.156, -0.084), (0.122, 0.092, 0.108), 152, -7, R_MOSS_A),
                      ((-0.134, 0.106), (0.106, 0.080, 0.098), 243, 8, R_MOSS_A)]),
    ((-1.24, -0.38), [((0.00, 0.00), (0.142, 0.102, 0.166), 172, 5, R_MOSS_A), ((-0.116, -0.104), (0.108, 0.082, 0.110), 258, -6, R_MOSS_B)]),
    ((-0.62, -1.46), [((0.00, 0.00), (0.156, 0.116, 0.134), 283, -5, R_MOSS_A), ((0.126, 0.104), (0.112, 0.088, 0.100), 13, 7, R_MOSS_B),
                      ((-0.104, 0.124), (0.090, 0.068, 0.088), 98, -8, R_MOSS_A)]),
    ((-1.40, -1.20), [((0.00, 0.00), (0.136, 0.098, 0.122), 22, 6, R_MOSS_B), ((0.112, -0.074), (0.098, 0.076, 0.090), 93, -7, R_MOSS_A)]),
    # out to her right
    ((1.98, -0.42), [((0.00, 0.00), (0.176, 0.120, 0.144), 108, -3, R_MOSS_B), ((-0.146, 0.072), (0.122, 0.092, 0.108), 192, 8, R_MOSS_A),
                     ((0.106, 0.124), (0.098, 0.072, 0.088), 277, -6, R_MOSS_A)]),
    ((2.26, -1.30), [((0.00, 0.00), (0.144, 0.104, 0.166), 187, 5, R_MOSS_A), ((0.124, -0.106), (0.110, 0.084, 0.100), 268, -8, R_MOSS_B)]),
    ((1.70, 0.48), [((0.00, 0.00), (0.134, 0.098, 0.122), 262, -5, R_MOSS_A), ((-0.106, 0.092), (0.098, 0.076, 0.090), 338, 7, R_MOSS_B)]),
    ((2.10, 1.10), [((0.00, 0.00), (0.166, 0.118, 0.134), 327, 4, R_MOSS_B), ((0.136, 0.084), (0.120, 0.090, 0.108), 52, -7, R_MOSS_A),
                    ((-0.124, 0.104), (0.098, 0.074, 0.090), 143, 8, R_MOSS_A), ((0.022, -0.146), (0.086, 0.066, 0.078), 218, -6, R_MOSS_B)]),
]

# GRASS: fourteen tufts where the first meadow had them. Each blade: compass, pitch off the court, length, width, how
# far its tip bows over, the roll it bows on, which paint. A tuft has a habit: a fountain, a lean, a young upright
# one, a lopsided one with a single long blade. Then its seed stalks, where it has gone to seed: compass, pitch,
# length, bow, and the seeds up its end as (how far along, which side, size).
A, B = R_GRASS_A, R_GRASS_B
TUFTS = [
    ((1.30, 0.40), [(6, 84, 0.52, 0.056, 34, 8, A), (58, 66, 0.38, 0.050, 58, -20, B), (104, 78, 0.47, 0.054, 46, 14, A), (143, 52, 0.27, 0.046, 40, 30, B),
                    (196, 72, 0.44, 0.052, 62, -12, A), (251, 60, 0.33, 0.048, 52, 22, B), (309, 80, 0.56, 0.058, 40, -6, A)], []),
    ((1.92, 1.62), [(24, 70, 0.42, 0.052, 50, -16, B), (61, 82, 0.60, 0.058, 38, 10, A), (118, 58, 0.31, 0.046, 44, 26, A), (172, 76, 0.50, 0.054, 56, -8, B),
                    (229, 86, 0.63, 0.060, 30, 4, A), (284, 62, 0.36, 0.050, 60, 18, B), (338, 74, 0.48, 0.054, 48, -22, A)],
     [(205, 84, 0.72, 26, [(0.72, 1, 0.026), (0.80, -1, 0.028), (0.87, 1, 0.026), (0.93, -1, 0.023), (0.99, 1, 0.020)])]),
    # a young one: few, short, upright
    ((0.96, 2.10), [(31, 80, 0.33, 0.050, 22, 10, A), (112, 74, 0.38, 0.052, 30, -14, A), (190, 68, 0.29, 0.046, 36, 20, B), (268, 82, 0.41, 0.054, 26, -6, A)], []),
    # leaning away from him, toward her right
    ((2.62, 0.18), [(52, 70, 0.56, 0.058, 50, 12, A), (78, 58, 0.43, 0.052, 62, -18, B), (104, 76, 0.60, 0.060, 44, 6, A), (131, 50, 0.34, 0.048, 48, 24, A),
                    (22, 64, 0.40, 0.050, 56, -10, B), (164, 72, 0.47, 0.054, 40, 16, B), (318, 82, 0.36, 0.050, 26, -24, A), (238, 78, 0.31, 0.048, 30, 8, B)],
     [(88, 80, 0.76, 34, [(0.70, -1, 0.027), (0.78, 1, 0.028), (0.85, -1, 0.026), (0.92, 1, 0.024), (0.98, -1, 0.020)])]),
    ((-0.62, -1.62), [(27, 72, 0.46, 0.054, 44, -14, B), (88, 84, 0.57, 0.058, 32, 8, A), (151, 60, 0.35, 0.048, 56, 22, A), (214, 76, 0.49, 0.054, 48, -8, B),
                      (283, 56, 0.30, 0.046, 42, 28, A)], []),
    # one long blade and a huddle of short ones
    ((1.60, -2.62), [(342, 62, 0.72, 0.060, 78, 10, A), (48, 78, 0.36, 0.050, 30, -18, B), (97, 70, 0.41, 0.052, 44, 14, A), (158, 82, 0.44, 0.054, 28, -6, A),
                     (221, 66, 0.33, 0.048, 50, 24, B), (289, 74, 0.38, 0.050, 38, -12, B)], []),
    ((2.42, -1.20), [(19, 76, 0.49, 0.054, 46, 12, A), (74, 64, 0.40, 0.050, 60, -22, B), (133, 84, 0.58, 0.058, 34, 6, A), (187, 54, 0.32, 0.046, 44, 26, B),
                     (248, 72, 0.46, 0.052, 52, -10, A), (301, 80, 0.55, 0.056, 40, 18, B), (352, 60, 0.35, 0.048, 58, -28, A)],
     [(140, 86, 0.70, 22, [(0.74, 1, 0.026), (0.82, -1, 0.027), (0.89, 1, 0.025), (0.96, -1, 0.021)])]),
    ((-1.28, -0.18), [(41, 70, 0.44, 0.052, 50, -12, B), (103, 80, 0.54, 0.056, 36, 10, A), (176, 58, 0.33, 0.048, 46, 24, A), (244, 76, 0.48, 0.054, 54, -18, B),
                      (317, 66, 0.39, 0.050, 42, 6, A), (279, 84, 0.30, 0.046, 20, -4, B)], []),
    # low and spread, near his knee
    ((0.62, 0.62), [(8, 56, 0.35, 0.050, 48, 14, A), (73, 68, 0.43, 0.052, 40, -10, B), (128, 48, 0.29, 0.046, 36, 22, A), (201, 62, 0.40, 0.050, 52, -16, A),
                    (262, 74, 0.47, 0.054, 44, 8, B), (323, 52, 0.31, 0.048, 40, -24, B)], []),
    ((2.24, 2.62), [(33, 74, 0.46, 0.052, 48, -14, B), (96, 62, 0.37, 0.050, 58, 20, A), (148, 84, 0.59, 0.058, 30, 6, A), (203, 70, 0.42, 0.052, 50, -22, B),
                    (259, 78, 0.50, 0.054, 42, 12, A), (318, 58, 0.33, 0.048, 46, -8, A)],
     [(160, 82, 0.74, 30, [(0.71, -1, 0.027), (0.79, 1, 0.028), (0.86, -1, 0.026), (0.93, 1, 0.023), (0.99, -1, 0.020)])]),
    ((0.30, -2.10), [(12, 78, 0.53, 0.056, 42, 10, A), (69, 64, 0.39, 0.050, 56, -18, B), (124, 86, 0.48, 0.054, 24, 4, A), (181, 58, 0.34, 0.048, 48, 26, A),
                     (236, 80, 0.61, 0.060, 46, -8, B), (291, 68, 0.43, 0.052, 54, 16, A), (341, 52, 0.28, 0.046, 38, -26, B)], []),
    ((-1.60, -1.62), [(18, 72, 0.42, 0.052, 46, 12, A), (84, 82, 0.52, 0.056, 34, -8, B), (157, 60, 0.34, 0.048, 54, 22, A), (226, 76, 0.47, 0.054, 44, -16, A),
                      (298, 64, 0.38, 0.050, 50, 8, B)], []),
    # a fountain: a tall middle, arching outers
    ((1.38, -1.96), [(350, 88, 0.62, 0.060, 22, 0, A), (40, 70, 0.50, 0.056, 60, -12, B), (92, 62, 0.44, 0.052, 68, 16, A), (148, 74, 0.52, 0.056, 56, -6, B),
                     (203, 60, 0.41, 0.052, 70, 20, A), (262, 72, 0.49, 0.054, 58, -18, B), (311, 64, 0.43, 0.052, 66, 10, A), (118, 84, 0.34, 0.048, 24, -4, A)],
     [(20, 88, 0.80, 28, [(0.72, 1, 0.027), (0.79, -1, 0.029), (0.86, 1, 0.027), (0.92, -1, 0.024), (0.98, 1, 0.021)])]),
    ((2.80, -0.30), [(38, 74, 0.48, 0.054, 46, -14, A), (106, 62, 0.39, 0.050, 58, 18, B), (181, 82, 0.56, 0.058, 34, 6, A), (254, 56, 0.33, 0.048, 44, -22, A),
                     (322, 70, 0.45, 0.052, 52, 10, B)], []),
]

# The young shoots in the middle of each tuft, the same order as TUFTS (m1's tufts were all long blades fanned from a
# point, and from above each was a star): short, upright, pale. Typed like a blade.
SHOOTS = [
    [(33, 86, 0.24, 0.046, 12, 20, A), (228, 82, 0.19, 0.042, 16, -30, A)],
    [(141, 84, 0.27, 0.048, 14, -16, A), (312, 88, 0.21, 0.044, 10, 24, A), (86, 80, 0.17, 0.040, 18, 6, B)],
    [(158, 86, 0.18, 0.042, 12, 28, A), (334, 84, 0.22, 0.044, 14, -12, A)],
    [(201, 86, 0.25, 0.046, 12, 18, A), (348, 80, 0.20, 0.042, 18, -26, B)],
    [(63, 84, 0.23, 0.046, 14, -22, A), (247, 88, 0.18, 0.042, 10, 14, A)],
    [(128, 86, 0.21, 0.044, 12, 26, A), (262, 82, 0.25, 0.046, 16, -8, A), (16, 84, 0.16, 0.040, 14, 12, B)],
    [(102, 88, 0.24, 0.046, 10, -18, A), (276, 84, 0.20, 0.042, 16, 22, A)],
    [(74, 84, 0.22, 0.044, 14, 16, A), (212, 86, 0.18, 0.042, 12, -28, B)],
    [(44, 80, 0.19, 0.044, 16, -14, A), (172, 84, 0.23, 0.046, 12, 24, A), (296, 78, 0.16, 0.040, 20, 4, A)],
    [(67, 86, 0.24, 0.046, 12, 20, A), (233, 82, 0.19, 0.042, 16, -22, A)],
    [(96, 84, 0.22, 0.044, 14, -10, A), (318, 88, 0.26, 0.048, 10, 18, A)],
    [(51, 86, 0.20, 0.044, 12, 26, A), (192, 82, 0.24, 0.046, 16, -16, B)],
    [(176, 86, 0.26, 0.048, 12, 14, A), (286, 84, 0.21, 0.044, 14, -24, A), (64, 82, 0.18, 0.042, 16, 8, A)],
    [(72, 84, 0.23, 0.046, 14, -20, A), (218, 88, 0.19, 0.042, 10, 22, A)],
]

# FERNS (pako): a crown, a fiddlehead or two still rolled, and fronds, each in three segments so the class can unroll
# it. A frond: compass, girth at its foot, which paint (a young frond is paler), then its segments as
# (length, rise in degrees, the lengths of its pinnae pairs). No two fronds of a fern are the same age.
YOUNG, GROWN = R_PINNA_B, R_PINNA_A
FERNS = [
    ("fern-0", (0.10, -2.30),
     [(14, 0.018, GROWN, [(0.40, 62, [0.13, 0.17, 0.19]), (0.34, 38, [0.18, 0.17, 0.15]), (0.27, 6, [0.12, 0.09])]),
      (78, 0.015, YOUNG, [(0.33, 55, [0.11, 0.14, 0.16]), (0.29, 30, [0.15, 0.13, 0.11]), (0.22, -4, [0.09, 0.07])]),
      (151, 0.019, GROWN, [(0.44, 60, [0.14, 0.18, 0.20]), (0.37, 37, [0.19, 0.18, 0.15]), (0.29, 8, [0.13, 0.10, 0.07])]),
      (236, 0.014, YOUNG, [(0.27, 50, [0.10, 0.13]), (0.24, 27, [0.13, 0.12, 0.10]), (0.19, -8, [0.08, 0.06])]),
      (301, 0.017, GROWN, [(0.37, 59, [0.12, 0.16, 0.17]), (0.31, 33, [0.16, 0.15, 0.12]), (0.25, 2, [0.10, 0.08])])],
     [(196, 0.90), (48, 0.62)]),
    # the tallest, by her hem
    ("fern-1", (1.72, -1.88),
     [(352, 0.020, GROWN, [(0.46, 64, [0.15, 0.19, 0.21]), (0.39, 40, [0.20, 0.19, 0.16]), (0.31, 8, [0.13, 0.10, 0.07])]),
      (53, 0.017, GROWN, [(0.40, 57, [0.13, 0.17, 0.18]), (0.34, 31, [0.17, 0.16, 0.13]), (0.26, 0, [0.11, 0.08])]),
      (117, 0.014, YOUNG, [(0.30, 60, [0.10, 0.13, 0.14]), (0.25, 39, [0.13, 0.12, 0.10]), (0.19, 11, [0.08, 0.06])]),
      (171, 0.020, GROWN, [(0.48, 60, [0.15, 0.19, 0.21]), (0.41, 35, [0.20, 0.19, 0.17]), (0.33, 4, [0.14, 0.11, 0.07])]),
      (243, 0.016, GROWN, [(0.37, 53, [0.12, 0.16, 0.17]), (0.32, 28, [0.16, 0.15, 0.12]), (0.25, -6, [0.10, 0.08])]),
      (296, 0.018, YOUNG, [(0.42, 62, [0.13, 0.17, 0.19]), (0.35, 37, [0.18, 0.16, 0.14]), (0.27, 5, [0.11, 0.09])])],
     [(214, 1.00)]),
    # a small one behind his left shoulder
    ("fern-2", (-0.72, -1.12),
     [(36, 0.015, GROWN, [(0.32, 61, [0.11, 0.14, 0.15]), (0.27, 36, [0.14, 0.13, 0.11]), (0.21, 4, [0.09, 0.07])]),
      (124, 0.016, GROWN, [(0.36, 55, [0.12, 0.15, 0.17]), (0.30, 30, [0.15, 0.14, 0.12]), (0.23, -2, [0.10, 0.07])]),
      (207, 0.013, YOUNG, [(0.26, 58, [0.09, 0.12]), (0.22, 37, [0.12, 0.11, 0.09]), (0.17, 9, [0.07, 0.05])]),
      (288, 0.015, GROWN, [(0.34, 58, [0.11, 0.15, 0.16]), (0.28, 33, [0.15, 0.13, 0.11]), (0.21, 2, [0.09, 0.07])])],
     [(338, 0.70), (171, 0.52)]),
    ("fern-3", (2.30, -0.62),
     [(9, 0.017, GROWN, [(0.38, 60, [0.13, 0.16, 0.18]), (0.32, 35, [0.17, 0.16, 0.13]), (0.24, 3, [0.11, 0.08])]),
      (96, 0.016, YOUNG, [(0.34, 64, [0.12, 0.15, 0.16]), (0.29, 40, [0.15, 0.14, 0.11]), (0.22, 9, [0.10, 0.07])]),
      (178, 0.018, GROWN, [(0.41, 54, [0.13, 0.17, 0.19]), (0.34, 28, [0.18, 0.17, 0.14]), (0.27, -4, [0.12, 0.09, 0.06])]),
      (263, 0.015, GROWN, [(0.33, 58, [0.11, 0.15, 0.16]), (0.28, 34, [0.15, 0.13, 0.11]), (0.21, 5, [0.09, 0.07])])],
     [(140, 0.80)]),
    ("fern-4", (-1.20, -2.02),
     [(22, 0.018, GROWN, [(0.41, 58, [0.13, 0.17, 0.18]), (0.34, 33, [0.17, 0.16, 0.13]), (0.27, 1, [0.11, 0.09])]),
      (93, 0.014, YOUNG, [(0.29, 59, [0.10, 0.13, 0.14]), (0.25, 38, [0.13, 0.12, 0.10]), (0.19, 10, [0.08, 0.06])]),
      (148, 0.017, GROWN, [(0.38, 62, [0.12, 0.16, 0.18]), (0.32, 38, [0.17, 0.15, 0.13]), (0.24, 7, [0.10, 0.08])]),
      (219, 0.019, GROWN, [(0.43, 55, [0.14, 0.18, 0.19]), (0.36, 30, [0.18, 0.17, 0.14]), (0.28, -2, [0.12, 0.09, 0.06])]),
      (304, 0.016, GROWN, [(0.36, 60, [0.12, 0.15, 0.17]), (0.30, 35, [0.16, 0.14, 0.12]), (0.23, 4, [0.10, 0.07])])],
     [(262, 0.85), (58, 0.58)]),
    # the youngest: three fronds and two fiddleheads still to come
    ("fern-5", (2.00, 0.70),
     [(18, 0.016, YOUNG, [(0.34, 63, [0.12, 0.15, 0.16]), (0.29, 39, [0.15, 0.14, 0.11]), (0.22, 8, [0.09, 0.07])]),
      (134, 0.017, GROWN, [(0.38, 56, [0.13, 0.16, 0.18]), (0.32, 31, [0.17, 0.15, 0.13]), (0.24, 0, [0.10, 0.08])]),
      (251, 0.015, YOUNG, [(0.31, 60, [0.11, 0.14, 0.15]), (0.26, 36, [0.14, 0.13, 0.10]), (0.20, 6, [0.08, 0.06])])],
     [(72, 0.95), (198, 0.72)]),
]

# SAMPAGUITA (her wreath's own flower): a low mound of dark waxy leaves on woody twigs, white pinwheels opening on top.
# A bush: its twigs as (points, radii); its leaves as (how far out its stem starts, how high, compass, pitch, length,
# width, bow); its blossoms as (where, radius, petals, turn, tilt, toward which compass, pinwheel); its buds as
# (where, length, lean, toward).
SAMPAGUITAS = [
    ("samp-0", (1.48, 1.30),
     [([(0, 0, 0), (0.012, 0.12, -0.010), (-0.010, 0.235, 0.015)], [0.026, 0.021, 0.016]),
      ([(0.005, 0.08, 0), (0.072, 0.150, 0.040), (0.118, 0.215, 0.058)], [0.017, 0.015, 0.012]),
      ([(0, 0.06, 0), (-0.062, 0.130, -0.050), (-0.108, 0.190, -0.084)], [0.016, 0.014, 0.012])],
     [(0.03, 0.06, 12, 18, 0.235, 0.128, 30), (0.03, 0.07, 74, 27, 0.205, 0.116, 24), (0.04, 0.05, 139, 12, 0.245, 0.132, 36), (0.03, 0.08, 201, 23, 0.212, 0.120, 28),
      (0.03, 0.06, 262, 15, 0.230, 0.126, 34), (0.04, 0.07, 318, 29, 0.192, 0.110, 22), (0.05, 0.15, 40, 44, 0.172, 0.100, 18), (0.06, 0.17, 110, 52, 0.150, 0.090, 14),
      (0.05, 0.14, 172, 40, 0.180, 0.102, 20), (0.06, 0.18, 232, 56, 0.142, 0.086, 12), (0.05, 0.16, 292, 46, 0.162, 0.096, 16), (0.04, 0.19, 350, 60, 0.140, 0.084, 10)],
     [((0.05, 0.305, 0.02), 0.094, 8, 5, 12, 60, 0.9), ((-0.11, 0.262, 0.07), 0.082, 7, 22, 28, 300, 0.7), ((0.10, 0.252, -0.10), 0.086, 6, 13, 30, 135, 1.0),
      ((-0.04, 0.236, -0.13), 0.076, 8, 31, 36, 200, 0.6)],
     [((-0.02, 0.290, -0.06), 0.070, 18, 190), ((0.13, 0.226, 0.07), 0.062, 34, 62)]),
    ("samp-1", (0.72, 1.94),
     [([(0, 0, 0), (-0.010, 0.11, 0.012), (0.012, 0.215, -0.008)], [0.024, 0.020, 0.015]),
      ([(0, 0.07, 0), (0.058, 0.135, -0.046), (0.100, 0.200, -0.070)], [0.016, 0.014, 0.012])],
     [(0.03, 0.06, 24, 16, 0.222, 0.122, 32), (0.03, 0.07, 91, 26, 0.198, 0.112, 24), (0.04, 0.05, 153, 13, 0.232, 0.128, 34), (0.03, 0.07, 219, 22, 0.206, 0.116, 28),
      (0.03, 0.06, 287, 18, 0.218, 0.120, 30), (0.05, 0.14, 58, 46, 0.164, 0.096, 16), (0.05, 0.16, 128, 54, 0.146, 0.088, 12), (0.06, 0.15, 196, 42, 0.170, 0.098, 18),
      (0.05, 0.17, 262, 58, 0.140, 0.084, 10), (0.04, 0.15, 334, 48, 0.156, 0.092, 14)],
     [((0.03, 0.282, 0.04), 0.090, 7, 9, 10, 20, 0.8), ((-0.09, 0.240, -0.06), 0.080, 8, 30, 30, 236, 1.0), ((0.10, 0.232, -0.05), 0.084, 6, 18, 32, 116, 0.6)],
     [((0.07, 0.262, 0.09), 0.066, 26, 38)]),
    ("samp-2", (2.52, -1.92),
     [([(0, 0, 0), (0.014, 0.13, 0.008), (-0.006, 0.250, -0.012)], [0.027, 0.022, 0.016]),
      ([(0.006, 0.09, 0), (-0.070, 0.160, 0.044), (-0.122, 0.226, 0.060)], [0.017, 0.015, 0.012]),
      ([(0, 0.07, 0), (0.064, 0.140, -0.058), (0.104, 0.206, -0.092)], [0.016, 0.014, 0.012])],
     [(0.03, 0.06, 8, 20, 0.242, 0.130, 30), (0.04, 0.07, 63, 14, 0.226, 0.124, 36), (0.03, 0.05, 121, 26, 0.252, 0.136, 26), (0.03, 0.08, 183, 12, 0.216, 0.120, 38),
      (0.04, 0.06, 241, 24, 0.236, 0.128, 28), (0.03, 0.07, 303, 17, 0.224, 0.122, 32), (0.05, 0.16, 28, 48, 0.176, 0.102, 16), (0.06, 0.18, 96, 56, 0.152, 0.090, 12),
      (0.05, 0.15, 156, 42, 0.184, 0.104, 20), (0.06, 0.19, 214, 60, 0.146, 0.088, 10), (0.05, 0.17, 276, 50, 0.166, 0.098, 14), (0.05, 0.16, 338, 44, 0.172, 0.100, 18)],
     [((0.04, 0.318, 0.01), 0.098, 8, 17, 10, 350, 0.9), ((-0.11, 0.274, 0.06), 0.084, 7, 2, 28, 296, 0.7), ((0.10, 0.262, -0.09), 0.088, 6, 26, 30, 130, 1.1),
      ((-0.02, 0.246, 0.13), 0.078, 7, 40, 38, 352, 0.6)],
     [((-0.05, 0.300, -0.07), 0.072, 20, 214)]),
    ("samp-3", (-0.92, -2.42),
     [([(0, 0, 0), (-0.012, 0.12, -0.008), (0.008, 0.225, 0.012)], [0.025, 0.020, 0.015]),
      ([(0, 0.08, 0), (0.066, 0.145, 0.040), (0.108, 0.205, 0.066)], [0.016, 0.014, 0.012])],
     [(0.03, 0.06, 33, 18, 0.226, 0.124, 32), (0.03, 0.07, 104, 28, 0.202, 0.114, 24), (0.04, 0.05, 168, 13, 0.238, 0.130, 36), (0.03, 0.07, 237, 23, 0.210, 0.118, 28),
      (0.03, 0.06, 308, 16, 0.222, 0.122, 32), (0.05, 0.15, 66, 46, 0.168, 0.098, 16), (0.05, 0.17, 141, 55, 0.148, 0.088, 12), (0.06, 0.15, 204, 43, 0.174, 0.100, 18),
      (0.05, 0.17, 274, 57, 0.144, 0.086, 10), (0.04, 0.16, 346, 49, 0.158, 0.094, 14)],
     [((0.02, 0.292, 0.03), 0.092, 8, 11, 10, 40, 0.8), ((0.10, 0.250, -0.06), 0.082, 6, 24, 30, 122, 1.0), ((-0.09, 0.240, 0.07), 0.084, 7, 3, 32, 306, 0.7)],
     [((-0.07, 0.272, 0.05), 0.068, 24, 300)]),
    ("samp-4", (1.20, -0.32),
     [([(0, 0, 0), (0.010, 0.11, 0.010), (-0.008, 0.205, -0.010)], [0.024, 0.019, 0.015]),
      ([(0, 0.07, 0), (-0.056, 0.130, -0.044), (-0.094, 0.186, -0.072)], [0.016, 0.014, 0.012])],
     [(0.03, 0.06, 18, 17, 0.212, 0.118, 30), (0.03, 0.06, 83, 26, 0.192, 0.110, 24), (0.04, 0.05, 147, 12, 0.224, 0.124, 34), (0.03, 0.07, 213, 22, 0.200, 0.114, 28),
      (0.03, 0.06, 282, 15, 0.210, 0.118, 32), (0.05, 0.14, 49, 46, 0.158, 0.094, 16), (0.05, 0.15, 119, 53, 0.142, 0.086, 12), (0.05, 0.14, 188, 41, 0.164, 0.096, 18),
      (0.05, 0.16, 251, 56, 0.138, 0.084, 10), (0.04, 0.15, 322, 47, 0.152, 0.090, 14)],
     [((0.03, 0.266, 0.02), 0.088, 7, 7, 10, 70, 0.9), ((-0.08, 0.232, 0.07), 0.078, 8, 19, 30, 316, 0.6), ((0.08, 0.222, -0.07), 0.082, 6, 34, 32, 136, 1.0)],
     [((0.02, 0.246, -0.10), 0.064, 28, 176)]),
    # the smallest, out on his left: two blossoms
    ("samp-5", (-1.46, -0.86),
     [([(0, 0, 0), (-0.008, 0.11, 0.010), (0.010, 0.215, -0.006)], [0.024, 0.019, 0.015])],
     [(0.03, 0.06, 27, 19, 0.218, 0.120, 30), (0.03, 0.07, 98, 14, 0.204, 0.114, 36), (0.04, 0.05, 163, 27, 0.228, 0.126, 26), (0.03, 0.07, 238, 12, 0.196, 0.110, 34),
      (0.03, 0.06, 304, 22, 0.214, 0.118, 28), (0.05, 0.15, 61, 48, 0.162, 0.096, 16), (0.05, 0.16, 177, 54, 0.148, 0.088, 12), (0.05, 0.15, 289, 44, 0.166, 0.098, 18)],
     [((0.04, 0.278, 0.02), 0.090, 8, 13, 12, 50, 0.8), ((-0.09, 0.246, -0.05), 0.080, 7, 27, 30, 240, 1.0)],
     [((0.08, 0.250, 0.07), 0.066, 26, 48), ((-0.03, 0.262, 0.09), 0.058, 22, 340)]),
]

# GUMAMELA (the red a Filipino garden has; every child in the islands has sucked the nectar out of one): a taller
# bush of broad toothed leaves, two flowers each and a bud. A bush: twigs; leaves as the sampaguita's; blossoms as
# (where, radius, turn, tilt, toward, its column (reach, lean x, lean z), its anthers (along, round, size)); the bud.
GUMAMELAS = [
    ("gum-0", (2.10, -2.62),
     [([(0, 0, 0), (0.020, 0.18, 0.000), (0.000, 0.345, 0.020)], [0.030, 0.024, 0.018]),
      ([(0.010, 0.14, 0), (0.092, 0.262, 0.030), (0.132, 0.366, 0.044)], [0.019, 0.016, 0.013]),
      ([(0, 0.10, 0), (-0.084, 0.224, -0.052), (-0.142, 0.314, -0.072)], [0.019, 0.016, 0.013])],
     [(0.04, 0.09, 4, 22, 0.272, 0.172, 30), (0.04, 0.11, 72, 30, 0.252, 0.160, 24), (0.05, 0.08, 138, 16, 0.284, 0.178, 36), (0.04, 0.12, 208, 26, 0.246, 0.156, 28),
      (0.04, 0.10, 281, 34, 0.262, 0.166, 22), (0.06, 0.22, 36, 46, 0.214, 0.138, 18), (0.07, 0.25, 164, 52, 0.196, 0.126, 14), (0.06, 0.23, 247, 42, 0.220, 0.140, 20),
      (0.05, 0.27, 322, 56, 0.186, 0.120, 12)],
     [((0.06, 0.415, 0.03), 0.165, 12, 40, 38, (0.26, 0.030, 0.050), [(0.10, 20, 0.014), (0.34, 150, 0.015), (0.52, 270, 0.013), (0.74, 60, 0.014), (0.86, 200, 0.012)]),
      ((-0.13, 0.350, -0.06), 0.146, 40, 54, 246, (0.23, -0.040, 0.030), [(0.14, 300, 0.013), (0.36, 80, 0.014), (0.58, 190, 0.013), (0.80, 330, 0.012)])],
     ((0.02, 0.440, -0.09), 0.105, 0.030, 16, 172)),
    ("gum-1", (-1.72, -1.66),
     [([(0, 0, 0), (-0.016, 0.17, 0.010), (0.008, 0.330, -0.012)], [0.029, 0.023, 0.018]),
      ([(0, 0.12, 0), (0.086, 0.240, -0.040), (0.126, 0.340, -0.066)], [0.019, 0.016, 0.013])],
     [(0.04, 0.09, 22, 28, 0.262, 0.166, 26), (0.04, 0.10, 96, 18, 0.248, 0.158, 34), (0.05, 0.08, 171, 32, 0.276, 0.174, 24), (0.04, 0.11, 252, 22, 0.256, 0.162, 30),
      (0.04, 0.09, 323, 15, 0.244, 0.156, 36), (0.06, 0.21, 58, 48, 0.206, 0.134, 16), (0.06, 0.24, 137, 54, 0.190, 0.124, 14), (0.06, 0.22, 216, 44, 0.212, 0.136, 18),
      (0.05, 0.26, 298, 58, 0.180, 0.118, 12)],
     [((0.04, 0.395, 0.05), 0.156, 25, 42, 12, (0.25, 0.020, 0.060), [(0.12, 40, 0.014), (0.32, 170, 0.014), (0.54, 290, 0.013), (0.72, 100, 0.014), (0.88, 230, 0.012)]),
      ((0.11, 0.330, -0.08), 0.138, 5, 56, 128, (0.22, 0.045, -0.020), [(0.16, 10, 0.013), (0.40, 140, 0.013), (0.62, 250, 0.013), (0.82, 20, 0.012)])],
     ((-0.07, 0.420, 0.02), 0.098, 0.028, 20, 286)),
    ("gum-2", (2.84, 1.30),
     [([(0, 0, 0), (0.014, 0.18, -0.012), (-0.010, 0.340, 0.010)], [0.030, 0.024, 0.018]),
      ([(0.006, 0.13, 0), (-0.078, 0.250, 0.040), (-0.120, 0.356, 0.052)], [0.019, 0.016, 0.013]),
      ([(0, 0.11, 0), (0.080, 0.226, 0.058), (0.128, 0.312, 0.090)], [0.018, 0.016, 0.013])],
     [(0.04, 0.09, 13, 24, 0.266, 0.168, 28), (0.04, 0.11, 88, 32, 0.250, 0.160, 22), (0.05, 0.08, 157, 17, 0.280, 0.176, 36), (0.04, 0.10, 231, 27, 0.254, 0.162, 26),
      (0.04, 0.12, 307, 20, 0.242, 0.154, 32), (0.06, 0.22, 46, 50, 0.208, 0.134, 16), (0.07, 0.24, 121, 44, 0.218, 0.140, 20), (0.06, 0.23, 196, 56, 0.188, 0.122, 12),
      (0.06, 0.25, 268, 47, 0.202, 0.130, 16), (0.05, 0.28, 340, 60, 0.178, 0.116, 10)],
     [((-0.05, 0.405, 0.02), 0.160, 33, 40, 318, (0.25, -0.030, 0.050), [(0.10, 330, 0.014), (0.30, 90, 0.014), (0.50, 210, 0.014), (0.70, 350, 0.013), (0.86, 120, 0.012)]),
      ((0.10, 0.345, 0.08), 0.142, 18, 52, 58, (0.23, 0.040, 0.030), [(0.14, 60, 0.013), (0.38, 180, 0.014), (0.60, 300, 0.013), (0.82, 70, 0.012)])],
     ((0.03, 0.435, -0.08), 0.100, 0.029, 18, 160)),
]

# MAKAHIYA (the one every Filipino child has touched): low plum-red runners along the court, feathery leaves that fold
# shut at a touch, pink puffs on short stalks. A plant: its runners as (compass, length); its leaves as (compass,
# length, rise, the lengths of its leaflet pairs from stalk to tip); its puffs as (where, radius, its filaments as
# (compass, elevation, length)).
MAKAHIYAS = [
    ("maka-0", (2.10, 1.00),
     [(52, 0.20), (223, 0.16)],
     [(8, 0.29, 20, [0.050, 0.058, 0.060, 0.054, 0.044, 0.032]), (97, 0.25, 26, [0.046, 0.054, 0.052, 0.044, 0.032]),
      (176, 0.31, 16, [0.052, 0.060, 0.062, 0.058, 0.048, 0.038]), (262, 0.26, 22, [0.048, 0.056, 0.054, 0.046, 0.034]), (318, 0.21, 30, [0.042, 0.050, 0.044, 0.032])],
     [((0.06, 0.185, 0.05), 0.052, [(0, 18, 0.036), (63, 48, 0.034), (118, 12, 0.038), (184, 42, 0.034), (238, 22, 0.036), (303, 56, 0.032), (28, 80, 0.034), (150, -14, 0.030), (270, -10, 0.030)]),
      ((-0.09, 0.150, -0.05), 0.044, [(22, 28, 0.032), (94, 54, 0.030), (163, 18, 0.032), (228, 48, 0.030), (297, 30, 0.032), (120, 82, 0.030), (350, -12, 0.028)])]),
    ("maka-1", (1.02, 2.70),
     [(140, 0.18), (300, 0.21)],
     [(38, 0.27, 22, [0.048, 0.056, 0.056, 0.048, 0.036]), (127, 0.30, 16, [0.050, 0.060, 0.060, 0.054, 0.044, 0.032]),
      (221, 0.24, 26, [0.046, 0.052, 0.048, 0.036]), (307, 0.28, 19, [0.050, 0.058, 0.056, 0.048, 0.038])],
     [((0.04, 0.175, 0.06), 0.050, [(12, 24, 0.036), (77, 52, 0.032), (141, 16, 0.036), (203, 46, 0.034), (268, 28, 0.036), (333, 58, 0.032), (100, 82, 0.032), (40, -12, 0.030)]),
      ((-0.08, 0.145, -0.06), 0.042, [(31, 24, 0.030), (108, 50, 0.030), (192, 28, 0.032), (271, 54, 0.028), (348, 18, 0.030), (220, 84, 0.028)])]),
    ("maka-2", (2.88, -0.42),
     [(18, 0.19), (168, 0.17), (275, 0.14)],
     [(2, 0.30, 17, [0.052, 0.060, 0.062, 0.056, 0.046, 0.034]), (88, 0.26, 22, [0.048, 0.056, 0.052, 0.042, 0.032]),
      (187, 0.28, 19, [0.050, 0.058, 0.058, 0.050, 0.038]), (271, 0.24, 25, [0.046, 0.054, 0.050, 0.038]), (136, 0.20, 32, [0.042, 0.048, 0.042, 0.030])],
     [((-0.05, 0.180, 0.04), 0.054, [(3, 28, 0.038), (71, 50, 0.034), (139, 22, 0.038), (212, 54, 0.034), (279, 18, 0.036), (341, 44, 0.034), (180, 82, 0.034), (40, 76, 0.030), (100, -12, 0.030)]),
      ((0.08, 0.148, -0.07), 0.044, [(28, 26, 0.032), (112, 52, 0.030), (188, 30, 0.032), (268, 56, 0.028), (352, 20, 0.030), (60, 84, 0.028)])]),
    # one puff, out on his left
    ("maka-3", (-1.58, -1.12),
     [(98, 0.18), (250, 0.20)],
     [(22, 0.28, 19, [0.050, 0.058, 0.056, 0.048, 0.036]), (108, 0.25, 24, [0.046, 0.054, 0.050, 0.040, 0.030]),
      (203, 0.30, 16, [0.052, 0.060, 0.062, 0.056, 0.046, 0.034]), (291, 0.26, 21, [0.048, 0.056, 0.052, 0.042])],
     [((0.05, 0.178, 0.03), 0.050, [(17, 28, 0.036), (84, 56, 0.032), (153, 22, 0.036), (227, 50, 0.034), (296, 32, 0.036), (0, 82, 0.032), (190, -12, 0.030), (320, -8, 0.030)])]),
    ("maka-4", (0.42, -2.92),
     [(64, 0.17), (198, 0.20)],
     [(33, 0.27, 21, [0.048, 0.056, 0.054, 0.046, 0.034]), (122, 0.30, 16, [0.052, 0.060, 0.060, 0.052, 0.042, 0.032]),
      (217, 0.25, 24, [0.046, 0.054, 0.050, 0.040]), (303, 0.28, 18, [0.050, 0.058, 0.058, 0.050, 0.038])],
     [((0.03, 0.178, -0.05), 0.052, [(6, 24, 0.036), (72, 50, 0.034), (133, 18, 0.038), (201, 56, 0.032), (263, 28, 0.036), (328, 48, 0.034), (100, 82, 0.032), (230, -12, 0.030)]),
      ((-0.08, 0.146, 0.05), 0.042, [(42, 28, 0.030), (121, 56, 0.028), (203, 24, 0.032), (282, 50, 0.030), (350, 82, 0.028)])]),
    # the nearest to his palms: the one that shuts first
    ("maka-5", (-0.56, -0.20),
     [(160, 0.16), (318, 0.18)],
     [(13, 0.26, 19, [0.048, 0.056, 0.054, 0.044, 0.034]), (104, 0.28, 23, [0.050, 0.058, 0.056, 0.048, 0.036]),
      (197, 0.25, 17, [0.048, 0.054, 0.052, 0.042]), (287, 0.23, 22, [0.044, 0.052, 0.048, 0.036]), (241, 0.19, 30, [0.040, 0.046, 0.038])],
     [((0.04, 0.168, 0.04), 0.048, [(11, 28, 0.034), (82, 50, 0.032), (148, 24, 0.036), (222, 56, 0.032), (291, 30, 0.034), (60, 82, 0.030), (330, -12, 0.030)])]),
]


# ---------------------------------------------------------------------------------------------- the build

# ⚠️ PROPORTIONS FOUND IN THE REVIEW (m1, Blender): the fronds were typed at the first meadow's lengths with its small
# pinnae, and in round forms that read as long bare ladders; the grass read as a few thin spikes. The fronds are
# shorter and their pinnae longer (a frond is about half as wide as it is long), the blades are wider.
FROND_LONG, PINNA_LONG, BLADE_WIDE = 0.80, 1.30, 1.30
# And m2: the makahiya's feathers were lost beside the moss and the grass. Longer, and half as wide again.
FEATHER_LONG, FEATHER_WIDE = 1.12, 1.40
# ⚠️ AND THE FIRST FILM (`introfx md1`): a frond standing at the first meadow's 55 to 68 degrees, with full pinnae in
# tiers up it, was a little Christmas tree from every one of the cutscene's cameras. A fern is a fountain: each
# segment lies this much lower than it is typed, so the fronds arch out and their tips hang.
FROND_LOWER = (16.0, 22.0, 34.0)


def feather(length, rise, pairs):
    """A makahiya leaf along +z from its node, rising by `rise`: a short bare stalk, then the feather of leaflets.

    ⚠️ ONE SURFACE, NOT TWELVE LEAFLETS (m1: every leaflet was its own small bean, the ink went round each, and the
    leaf was a string of dark beads, a caterpillar). The feather is one flat blade whose edge is drawn in between each
    pair of leaflets (`pairs`: how long each pair is, from the stalk to the tip) and swept forward, so the ink runs
    round the whole comb and the paint parts the leaflets. The class folds it by narrowing it in x: the comb closes.
    """
    length *= FEATHER_LONG
    pairs = [long * FEATHER_WIDE for long in pairs]
    bare = length * 0.20
    step = (length - bare) / (len(pairs) + 0.45)
    prof, phase = [(0.0, bare - 0.004)], [0.0]
    for j, long in enumerate(pairs):
        y = bare + j * step
        # Four rings to a pair, so a leaflet's end is round (three drew a saw's tooth).
        prof += [(long * 0.24, y), (long * 0.90, y + step * 0.26), (long, y + step * 0.52), (long * 0.86, y + step * 0.80)]
        phase += [0.0, 0.8, 1.0, 0.7]
    y = bare + len(pairs) * step
    prof += [(pairs[-1] * 0.28, y), (pairs[-1] * 0.46, y + step * 0.22), (0.0, length)]
    phase += [0.0, 1.0, 1.0]
    wide = max(pairs)
    blade = lathe(prof, seg=6, squash_z=0.026 / (2 * wide))
    u0, v0, u1, v1 = inset(R_LEAFLET)
    uv = np.zeros((len(blade[0]), 2), np.float32)
    for i in range(len(blade[0])):
        row, col = divmod(i, 7)
        uv[i] = (u0 + (u1 - u0) * col / 6.0, v1 - (v1 - v0) * phase[row])
    pos = moved(blade, turn=(90, 0, 0))[0].copy()
    reach = np.abs(pos[:, 0])
    pos[:, 2] += reach * 0.30                 # the leaflets sweep forward
    pos[:, 1] += reach * 0.22                 # and lift a little off the stalk, a shallow V
    r = math.radians(rise)
    end = (0.0, length * math.sin(r), length * math.cos(r))
    stalk = tube([(0.0, 0.0, 0.0), (0.0, end[1] * .18, end[2] * .18), (0.0, end[1] * .36, end[2] * .36)], [0.013, 0.012, 0.010], seg=5)
    return [((moved((pos, blade[1], blade[2]), turn=(-rise, 0, 0))), uv), painted(stalk, part(R_MAKA_STEM, 0.35, 1.0), 5)]


def frond(m, fern, name, compass, girth, paint, segments):
    """One frond in three chained segments. Each segment is a node whose origin is the end of the one before (only the
    first is turned to `compass`); its stalk rises by its own angle and carries its pinnae left and right."""
    parent, origin = fern, (0.0, 0.035, 0.0)
    for s, (length, rise, pinnae) in enumerate(segments):
        length *= FROND_LONG
        rise -= FROND_LOWER[s]
        r = math.radians(rise)
        end = (0.0, length * math.sin(r), length * math.cos(r))
        arch = (0.0, math.cos(r) * length * 0.06, -math.sin(r) * length * 0.06)      # the stalk bows up between its ends
        thick = [girth * (1.0 - 0.24 * s), girth * (0.90 - 0.24 * s), girth * (0.78 - 0.24 * s)]
        spine = [(0.0, 0.0, 0.0), (0.0, end[1] * .5 + arch[1], end[2] * .5 + arch[2]), end]
        pieces = [painted(tube(spine, thick, seg=5), part(R_RACHIS, s / 3.0, (s + 1) / 3.0), 5)]
        for k, long in enumerate(pinnae):
            long *= PINNA_LONG
            u = (k + 0.6) / (len(pinnae) + 0.4)
            bow = 4.0 * u * (1.0 - u)
            at = (0.0, end[1] * u + arch[1] * bow, end[2] * u + arch[2] * bow)
            for side in (1.0, -1.0):
                # Each pinna sweeps forward along the frond and hangs a little; the far side a shade shorter.
                pinna = leaf(long * (1.0 if side > 0 else 0.93), long * 0.46, 0.026, paint, prof=PINNA, seg=6)
                pinna = turned(pinna, turn=(13.0 + 3.0 * s, side * (68.0 - 6.0 * s), 0))
                pieces.append(turned(pinna, turn=(-rise, 0, 0), at=at))
        if s == 2:
            last = pinnae[-1] * PINNA_LONG
            pieces.append(turned(leaf(last * 1.15, last * 0.50, 0.026, paint, prof=PINNA, seg=6), turn=(-rise + 10.0, 0, 0), at=end))
        seg_name = "%s-%s" % (name, "abc"[s])
        m.add(seg_name, pieces, at=origin, parent=parent, yaw=compass if s == 0 else 0.0)
        parent, origin = seg_name, end


def main():
    m = Model("meadow")
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/PaeteProps"))
    m.texture = "meadow-atlas.png"
    m.folder.mkdir(parents=True, exist_ok=True)
    paint_atlas(m.folder / m.texture)

    for i, ((x, z), mounds) in enumerate(CUSHIONS):
        pieces = []
        for (dx, dz), size, turn, tilt, paint in mounds:
            # A third sunk in the stone, so it sits IN the court like moss, not on it like a cap.
            mound = painted(ellipsoid(size, seg=11, rings=6), paint, 11, "axis", 1)
            pieces.append(turned(mound, turn=(tilt, turn, 0), at=(dx, size[1] * 0.30, dz)))
        m.add("moss-%d" % i, pieces, at=mx(x, z))

    for i, ((x, z), blades, stalks) in enumerate(TUFTS):
        pieces = []
        for compass, pitch, length, width, droop, roll, paint in blades:
            wide = width * BLADE_WIDE
            blade = leaf(length, wide, wide * 0.40, paint, prof=BLADE, seg=6, droop=droop, twist=roll)
            pieces.append(stand(blade, compass, pitch, at=on(compass, 0.034, -0.02)))
        for compass, pitch, length, width, droop, roll, paint in SHOOTS[i]:
            wide = width * BLADE_WIDE
            shoot = leaf(length, wide, wide * 0.44, paint, prof=BLADE, seg=6, droop=droop, twist=roll)
            pieces.append(stand(shoot, compass, pitch, at=on(compass, 0.012, -0.01)))
        for compass, pitch, length, droop, seeds in stalks:
            straw = lathe([(0.012, 0.0), (0.011, length * .5), (0.009, length * .9), (0.0, length)], seg=6)
            uv = kit.grid_uv(straw, inset(R_SEED), 6, "axis", 1)
            pieces.append(stand((bend(moved(straw, turn=(90, 0, 0)), droop, length), uv), compass, pitch))
            for along, side, size in seeds:
                seed = painted(ellipsoid((size * .62, size, size * .62), seg=7, rings=5), R_SEED, 7, "axis", 1)
                seed = turned(seed, turn=(62, 0, side * 24), at=(side * 0.016, 0.0, length * along))
                pieces.append(stand((bend(seed[0], droop, length), seed[1]), compass, pitch))
        m.add("grass-%d" % i, pieces, at=mx(x, z))

    for name, (x, z), fronds, croziers in FERNS:
        # The crown: a low dark knuckle the fronds leave from (m1's was a brown ball the size of a pot, and read as one).
        crown = painted(ellipsoid((0.066, 0.040, 0.062), seg=9, rings=5), part(R_CROZIER, 0.0, 0.30), 9, "axis", 1)
        pieces = [turned(crown, at=(0.0, 0.016, 0.0))]
        for compass, size in croziers:
            # A fiddlehead still rolled: a stalk leaning out, then over the top and round into itself, a turn and a half,
            # the coil tightening as it goes. (m2's was eight typed points and read as a bent brown stick: a spiral
            # wants more points than that, so this one is walked round its own middle.)
            high, out, wide = 0.215 * size, 0.105 * size, 0.062 * size
            coil = [(0.0, 0.02, 0.03), (0.0, high * .50, (out - wide) * .72), (0.0, high * .86, out - wide * 1.04)]
            for k in range(15):
                turn_, shrink = math.radians(180.0 - 540.0 * k / 14.0), 1.0 - 0.60 * k / 14.0
                coil.append((0.0, high + wide * shrink * math.sin(turn_), out + wide * shrink * math.cos(turn_)))
            girth = [0.024 * (0.6 + 0.4 * size) * (1.0 - 0.46 * k / (len(coil) - 1)) for k in range(len(coil))]
            pieces.append(turned(painted(tube(coil, girth, seg=7), R_CROZIER, 7), turn=(0, compass, 0)))
        m.add(name, pieces, at=mx(x, z))
        for k, (compass, girth, paint, segments) in enumerate(fronds):
            frond(m, name, "%s-f%d" % (name, k), float(compass), girth, paint, segments)

    for name, (x, z), twigs, leaves, blooms, buds in SAMPAGUITAS:
        pieces = [twig(points, radii) for points, radii in twigs]
        for out, high, compass, pitch, length, width, droop in leaves:
            pieces.append(stand(leaf(length, width, 0.042, R_SAMP_LEAF, droop=droop), compass, pitch, at=on(compass, out, high)))
        for at, length, lean, toward in buds:
            pieces.append(bud(length, 0.021, R_BUD, lean, toward, at))
        m.add(name, pieces, at=mx(x, z))
        for k, (at, radius, petals, turn, tilt, toward, twirl) in enumerate(blooms):
            m.add("%s-b%d" % (name, k), sampaguita(radius, petals, turn, tilt, toward, twirl), at=at, parent=name)

    for name, (x, z), twigs, leaves, blooms, (bud_at, bud_long, bud_girth, bud_lean, bud_toward) in GUMAMELAS:
        pieces = [twig(points, radii) for points, radii in twigs]
        for out, high, compass, pitch, length, width, droop in leaves:
            pieces.append(stand(leaf(length, width, 0.046, R_GUM_LEAF, prof=POINTED, droop=droop), compass, pitch, at=on(compass, out, high)))
        pieces.append(bud(bud_long, bud_girth, R_GUM_BUD, bud_lean, bud_toward, bud_at))
        m.add(name, pieces, at=mx(x, z))
        for k, (at, radius, turn, tilt, toward, column, anthers) in enumerate(blooms):
            m.add("%s-b%d" % (name, k), gumamela(radius, turn, tilt, toward, column, anthers), at=at, parent=name)

    for name, (x, z), runners, leaves, puffs in MAKAHIYAS:
        knot = painted(ellipsoid((0.052, 0.036, 0.050), seg=8, rings=5), part(R_MAKA_STEM, 0.0, 0.5), 8, "axis", 1)
        pieces = [turned(knot, at=(0.0, 0.022, 0.0))]
        for compass, length in runners:
            # A runner creeping along the court, lifting its tip.
            creep = tube([(0, 0.022, 0.02), (0.012, 0.016, length * .45), (-0.008, 0.018, length * .8), (0, 0.044, length)], [0.017, 0.015, 0.013, 0.010], seg=7)
            pieces.append(turned(painted(creep, R_MAKA_STEM, 7), turn=(0, compass, 0)))
        for at, radius, _ in puffs:
            stalk = tube([(0.0, 0.03, 0.0), (at[0] * .45, at[1] * .62, at[2] * .45), (at[0], at[1] - radius * .6, at[2])], [0.014, 0.012, 0.011], seg=6)
            pieces.append(painted(stalk, part(R_MAKA_STEM, 0.3, 1.0), 6))
        m.add(name, pieces, at=mx(x, z))
        for k, (compass, length, rise, pairs) in enumerate(leaves):
            m.add("%s-l%d" % (name, k), feather(length, rise, pairs), at=(0.0, 0.03, 0.0), parent=name, yaw=float(compass))
        for k, (at, radius, filaments) in enumerate(puffs):
            # ⚠️ A POMPOM, NOT A MINE (m1: each filament was a spike with a white bead and its own ink, and the puff was
            # a pink sea mine). The fuzz is soft round tufts half sunk in the ball, pink going pale, so its edge is
            # bumpy and nothing sticks out of it. `length` is how big a tuft is.
            ball = painted(ellipsoid((radius, radius * .96, radius), seg=11, rings=7), R_PUFF, 11, "axis", 1)
            fuzz = [ball]
            for compass, elevation, length in filaments:
                tuft = painted(ellipsoid((length * .50, length * .56, length * .50), seg=6, rings=4), R_STAMEN, 6, "axis", 1)
                e = math.radians(elevation)
                foot = on(compass, radius * .86 * math.cos(e), radius * .86 * math.sin(e))
                fuzz.append(turned(tuft, turn=(90.0 - elevation, compass, 0), at=foot))
            m.add("%s-p%d" % (name, k), fuzz, at=at, parent=name)

    m.write(PALETTE)


if __name__ == "__main__":
    main()
