"""The ICE LOTUS of Cheska's ABSOLUTE ZERO (2026-10-10): the three petals it blooms in and the splinter it shatters into.

    py -3 tools/build_ultimate_lotus_cheska.py [--out=<folder>] [lotuspetal_a] [lotuspetal_b] [lotuspetal_c] [lotusshard]

Why: the owner pointed at a set of magic effects (a cold orb charges, a lotus of long pointed crystals blooms in layered
rings, pale blue-white with darker blue depths, and shatters) and said "see if you can pull off any of those". The
flower is a THING, so it is modelled and painted the way the rest of her ice is (`tools/build_ultimate_props_cheska.py`,
whose cut-crystal shape and drawn marks this borrows): a narrow root, a fat shoulder, a long point, every face painted.

A petal is not a spike. A spike is as deep as it is wide; a petal is a BLADE, twice as wide as it is deep, so its two
broad faces are what the lens sees and its four narrow ones are the bevels that catch the light. Each is a unit long,
lying along +y with its root on the origin: the stage stands it round her, tips it outward and gives it its length.

One atlas, `lotus-atlas.png`, its upper half paint as the cast's are. The stage draws these with their own shader
(`Shaders/UltimateIceLotus`: the light inside, the rim, the glint that runs up an edge), so the paint carries what a
shader cannot: the tone of each face, the deep blue in the root, the drawn edges, the cracks and the frost.

⚠️ EVERY MARK AND EVERY PART IS TYPED: each ring of each petal, each crack, glint and fleck has its own row of numbers.
⚠️ THE NODE NAMES ARE THE STAGE'S CONTRACT (`HeroIntroductionScene.Cheska.cs`): `petal` on each petal, its pivot at its
root; `shard` on the splinter, about its own middle.

Look at one in Blender before the game is asked:
    blender -b --python tools/review_hand_companion.py -- --hero=lotuspetal_b --version=v1 --file=<out>/lotuspetal_b.glb --atlas=<out>/lotus-atlas.png
"""
import sys

import numpy as np

import build_ultimate_props_cheska as ice
from build_ultimate_props_cheska import Canvas, gem, hexc, marks, mix, model, put, smooth01

OUT = ice.OUT
PALETTE = ice.PALETTE

# lotus-atlas.png, the upper half (u0, v0, u1, v1; v down): the slim inner petal, the middle one, the broad outer one.
L_INNER, L_MID, L_OUTER = (0.0, 0.0, 0.34, 0.5), (0.34, 0.0, 0.67, 0.5), (0.67, 0.0, 1.0, 0.5)


def petal_paint(c, rect, tone, frost_at, pink_at):
    """The painted skin of one petal, face by face. Six faces: the second and the fifth are the broad ones, the rest are
    bevels. `tone` is how light each face is (-1 deep to 1 light). Up it: a deep blue root, the pale body, a frosted
    white tip from `frost_at`. `pink_at` is how far up the one warm glance sits on the broad front face."""
    u, t = c.grid(rect)
    face = np.floor(u * 6).astype(int).clip(0, 5)
    x = u * 6 - face
    col = np.zeros(u.shape + (3,), np.float32) + hexc("#a6e2f8")          # pale blue-white, the body of the flower
    # The depths: deep blue in the root, thinning out by a third of the way up, and a cooler mid tone through the body.
    col = mix(col, "#7fd3ea", (1 - smooth01(0.25, 0.75, t)) * 0.7)
    col = mix(col, "#2f7fc0", (1 - smooth01(0.04, 0.46, t)) * 0.9)
    col = mix(col, "#173f86", (1 - smooth01(0.0, 0.14, t)) * 0.8)
    lightness = np.array(tone, np.float32)[face]
    col = mix(col, "#eefcff", np.clip(lightness, 0, 1) * 0.38)
    col = mix(col, "#2466b0", np.clip(-lightness, 0, 1) * 0.72)
    # Across a face: lit side to shaded side, so no plane is one flat tone.
    col = mix(col, "#f2fdff", (1 - smooth01(0.0, 0.5, x)) * 0.2)
    col = mix(col, "#2f7fc0", smooth01(0.62, 1.0, x) * 0.24)
    # The light inside: a pale vein up the middle of each broad face, fattest at the shoulder, and a darker blue either
    # side of it low down, so the vein reads as something behind the surface and not a stripe on it.
    broad = np.isin(face, (1, 4)).astype(np.float32)
    vein = np.exp(-((x - 0.5) / (0.10 + 0.16 * np.exp(-((t - 0.36) / 0.2) ** 2))) ** 2) * smooth01(0.06, 0.3, t) * (1 - smooth01(0.7, 0.96, t))
    col = mix(col, "#1f5aa6", np.exp(-((np.abs(x - 0.5) - 0.3) / 0.12) ** 2) * (1 - smooth01(0.1, 0.5, t)) * broad * 0.4)
    col = mix(col, "#f6feff", vein * broad * 0.6)
    col = mix(col, "#ffc4dc", np.exp(-((x - 0.42) / 0.16) ** 2) * np.exp(-((t - pink_at) / 0.07) ** 2) * (face == 1) * 0.34)
    # The cut edges, drawn: a bright line down the lit side of each face and a deep one down the other.
    col = mix(col, "#ffffff", (1 - smooth01(0.0, 0.05, x)) * 0.9)
    col = mix(col, "#14407a", smooth01(0.95, 1.0, x) * 0.7)
    # The frosted tip, with a waved lower edge.
    edge = frost_at + 0.03 * np.cos((u * 6 + 0.2) * 2 * np.pi) + 0.015 * np.cos((u * 15 + 0.6) * 2 * np.pi)
    col = mix(col, "#dff6fd", smooth01(edge - 0.07, edge, t) * 0.7)
    col = mix(col, "#ffffff", smooth01(edge - 0.008, edge + 0.006, t))
    c.put(rect, col)


def paint_lotus_atlas(path):
    from PIL import ImageFilter
    c = Canvas(1024, "#bfeefb")
    # ---- the inner petal: slim, the palest, nearly upright round her.
    petal_paint(c, L_INNER, (0.30, 0.95, 0.15, -0.55, 0.55, -0.35), 0.86, 0.22)
    marks(c, L_INNER, 6,
          cracks=[(1, [(0.56, 0.58), (0.44, 0.48), (0.56, 0.40)], 3), (4, [(0.44, 0.52), (0.58, 0.42), (0.46, 0.33)], 3)],
          glints=[(1, 0.22, 0.46, 0.16, 4), (4, 0.64, 0.50, 0.12, 3), (0, 0.50, 0.40, 0.14, 3), (3, 0.46, 0.54, 0.10, 2)],
          flecks=[(1, 0.72, 0.66, 3), (4, 0.28, 0.64, 2), (2, 0.50, 0.30, 2), (5, 0.48, 0.58, 2)])
    # ---- the middle petal: the one the eye rests on.
    petal_paint(c, L_MID, (0.20, 0.85, 0.05, -0.65, 0.45, -0.45), 0.84, 0.20)
    marks(c, L_MID, 6,
          cracks=[(1, [(0.36, 0.62), (0.50, 0.52), (0.40, 0.42), (0.54, 0.33)], 3), (1, [(0.50, 0.52), (0.68, 0.50)], 2),
                  (4, [(0.58, 0.56), (0.44, 0.46), (0.56, 0.36)], 3)],
          glints=[(1, 0.68, 0.48, 0.16, 4), (1, 0.24, 0.30, 0.10, 3), (4, 0.26, 0.44, 0.14, 4), (2, 0.48, 0.50, 0.12, 2), (5, 0.52, 0.36, 0.12, 2)],
          flecks=[(1, 0.26, 0.70, 3), (4, 0.70, 0.68, 3), (0, 0.50, 0.62, 2), (3, 0.50, 0.28, 2), (1, 0.74, 0.24, 2)])
    # ---- the outer petal: the broad one that lies nearly flat, deepest in tone.
    petal_paint(c, L_OUTER, (0.10, 0.75, -0.05, -0.75, 0.35, -0.55), 0.82, 0.18)
    marks(c, L_OUTER, 6,
          cracks=[(1, [(0.60, 0.60), (0.46, 0.50), (0.58, 0.40), (0.42, 0.30)], 3), (4, [(0.40, 0.58), (0.54, 0.47), (0.42, 0.37)], 3),
                  (4, [(0.54, 0.47), (0.70, 0.44)], 2)],
          glints=[(1, 0.24, 0.50, 0.18, 5), (1, 0.34, 0.54, 0.08, 3), (4, 0.66, 0.42, 0.14, 4), (0, 0.50, 0.46, 0.12, 2), (2, 0.50, 0.30, 0.10, 2)],
          flecks=[(1, 0.74, 0.68, 3), (4, 0.26, 0.66, 3), (1, 0.70, 0.22, 2), (3, 0.50, 0.60, 2), (5, 0.50, 0.44, 2)])
    picture = c.picture(False).filter(ImageFilter.GaussianBlur(0.5))
    picture.save(path)
    return path


# ------------------------------------------------------------------ the models

# The first corner of a six-sided crystal sits on +x, so its second face looks along +z and its fifth along -z: with the
# depth squashed those two are the petal's broad faces.

def lotuspetal_a():
    """The inner petal: slim and tall, a short root, its shoulder a third of the way up."""
    m = model("lotuspetal_a", "lotus-atlas.png")
    m.add("petal", [gem([(0.045, 0.0), (0.115, 0.10), (0.150, 0.33), (0.095, 0.70), (0.0, 1.0)], 6, L_INNER, squash_z=0.52)])
    return m.write(PALETTE)


def lotuspetal_b():
    """The middle petal: fuller at the shoulder, with a short crystal grown out of its side low down."""
    m = model("lotuspetal_b", "lotus-atlas.png")
    m.add("petal", [
        gem([(0.050, 0.0), (0.140, 0.09), (0.185, 0.30), (0.120, 0.68), (0.0, 1.0)], 6, L_MID, squash_z=0.50),
        put(gem([(0.030, 0.0), (0.062, 0.05), (0.050, 0.17), (0.0, 0.30)], 6, L_INNER, squash_z=0.6, lean=(0.5, 0.0)), at=(0.10, 0.10, 0.02)),
    ])
    return m.write(PALETTE)


def lotuspetal_c():
    """The outer petal: the broadest and the flattest, its point drawn out long, a bud either side of its root (the left
    one typed, the right its smaller mirror)."""
    m = model("lotuspetal_c", "lotus-atlas.png")
    m.add("petal", [
        gem([(0.055, 0.0), (0.160, 0.10), (0.205, 0.32), (0.125, 0.66), (0.0, 1.0)], 6, L_OUTER, squash_z=0.44),
        put(gem([(0.032, 0.0), (0.066, 0.05), (0.052, 0.16), (0.0, 0.28)], 6, L_MID, squash_z=0.6, lean=(-0.55, 0.0)), at=(-0.12, 0.08, 0.02)),
        put(gem([(0.026, 0.0), (0.054, 0.04), (0.042, 0.13), (0.0, 0.22)], 6, L_MID, squash_z=0.6, lean=(0.55, 0.0)), at=(0.12, 0.08, 0.02)),
    ])
    return m.write(PALETTE)


def lotusshard():
    """What a petal breaks into: a splinter, pointed at both ends and fat off its middle, about its own middle so the
    stage tumbles it. A unit long."""
    m = model("lotusshard", "lotus-atlas.png")
    m.add("shard", [gem([(0.0, -0.5), (0.13, -0.24), (0.17, 0.02), (0.09, 0.28), (0.0, 0.5)], 6, L_MID, squash_z=0.55, twist=0.3)])
    return m.write(PALETTE)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    builds = (("lotuspetal_a", lotuspetal_a), ("lotuspetal_b", lotuspetal_b), ("lotuspetal_c", lotuspetal_c), ("lotusshard", lotusshard))
    which = {a for a in sys.argv[1:] if not a.startswith("--")} or {name for name, _ in builds}
    paint_lotus_atlas(OUT / "lotus-atlas.png")
    for name, build in builds:
        if name in which:
            build()
