"""Builds the Ilalim rebuild's sidewalk beggar: his own voxel person, not a cast rig.

    py -3 tools/build_beggar_voxel.py            (writes the .glb, its atlas and palette)
    py -3 tools/build_beggar_voxel.py --preview  (also Logs/ilalim-unity/beggar_preview.png)

Owner, 2026-09-30, on the beggar who wore Mang Kanor's rig: *"use a different model to make
him look more like a beggar, and better textured with dirt and stuff whil still maintainiing
artstyle"*. He is an NPC of the Ilalim sample scene (`SidewalkLife`), never a playable Person,
so he lives beside the scene's other life art (`Assets/TumbangPreso/Art/IlalimRebuild/Life/`)
and has no roster row, no avatar, no stats.

THE CAST'S PIPELINE, ONE CHARACTER'S COPY (docs/CHARACTER_MODEL_METHOD.md section 2,
docs/Voxel_Person_Guide.md). The machinery is `tools/build_person_voxel.py`'s, imported and
not edited: the chamfered box (`box_polygons`, `bevel_for`, BEVEL_FRACTION 0.45), the averaged
normals the outline needs (`smooth_normals`), the donated native skull (`character-male-d`,
slot 15 skin and slot 8 ink, the pate dropped), the expression guard (`_verify_expression`)
and the glb reader. What is his:

  * THE RIG IS `character-male-e`'s SKELETON, UNMOVED. The seven bones stay where the Kenney
    rig has them (legs 24 %, torso 23 %, head 53 %), so every clip SidewalkLife plays (idle,
    walk, sprint, sit, emote-yes, emote-no, pick-up) moves him exactly as it moves the cast,
    with no retarget and the original inverse bind matrices.
  * THIN AND OLD. Torso +/-0.128 against the cast's 0.151, legs 0.10 wide against 0.145,
    thin forearms, a bare ankle under rolled trousers, bare dusty feet on worn tsinelas (the
    right one's strap broken), messy greying hair receding at the front, a short grey beard,
    no eyebrows and an ink-only face (a small calm mouth, the eyes a little lidded): tired,
    never a caricature.
  * HIS CLOTHES ARE PAINTED, AND THAT IS THE ONE NEW MECHANISM. `Toon.shader` remaps a UV to
    a palette slot only in Unity atlas rows 0 to 7 (file rows 8 to 15); in the other half it
    samples the texture itself (`if (row <= 7) base = _Palette[...]`). So this .glb carries
    its OWN atlas (`npc-beggar-atlas.png`): the bottom half is the stock colormap (the palette
    cells, where his skin, hair, beard, tsinelas and face ink live as flat slots like every
    other Person), and the top half holds hand-drawn swatches that his garment and skin boxes
    are projected onto, one swatch per face (front, back, side, top, bottom). Nothing else in
    the game changes: ToonSkin carries a model's own texture across already.
  * THE TEXTURES ARE THE HOUSE STYLE (docs/KANTO_DESIGN_GUIDE.md section 3,
    docs/LAGOON_REWORK_GUIDE.md section 2): flat fills, a few LARGE patches with feathered
    edges, low contrast, no grain, no noise, no streaks. The dirt is drawn, every mark its own
    hand-set polygon (CHARACTER_MODEL_METHOD section 4, no loop stamps a motif): a sun-faded
    upper back and shoulders, sweat under the arms and down the chest and spine, grime along
    the shirt's hem, a hand-wipe smudge, a hole torn low on the chest, a frayed left sleeve, a
    sewn brown patch on the back and a small one on the right sleeve, a faded denim patch on
    the left knee, worn knees, dust on the seat he sits in, dust at the cuffs, the shins and
    the feet, dirt at the fingertips.
  * NO COLOUR IS NEAR THE ROLE HUES (offence orange #f87020, defence blue #0080e8): every
    garment colour is checked below, and the build refuses one that is. Skin is exempt, as it
    is for the cast.
"""
import json
import math
import os
import struct
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_person_voxel as bpv  # noqa: E402

from PIL import Image, ImageDraw, ImageFilter  # noqa: E402

BASE = "Assets/TumbangPreso/Art/characters/persons/character-male-e.glb"
FOLDER = "Assets/TumbangPreso/Art/IlalimRebuild/Life"
OUT = FOLDER + "/npc-beggar.glb"
ATLAS_NAME = "npc-beggar-atlas.png"
ATLAS_OUT = FOLDER + "/" + ATLAS_NAME
PALETTE_OUT = FOLDER + "/npc-beggar-palette.json"
COLORMAP = "Assets/TumbangPreso/Art/characters/persons/Textures/colormap.png"

# ---------------------------------------------------------------------------
# THE PALETTE SLOTS (flat parts). 8 is the face ink and stays dark (the builder refuses
# otherwise); 13 to 15 are the skin ramp, as on every Person.
# ---------------------------------------------------------------------------
SANDAL, STRAP, HAIR_DARK, HAIR_LIT, BEARD = 3, 4, 5, 6, 7
INK = bpv.INK
SKIN_DARK, SKIN_MID, SKIN = 13, 14, 15

PALETTE = {
    0: "8c826c", 1: "5b4f43", 2: "3e352d",
    SANDAL: "6f7c66",     # worn green-grey rubber, sun-faded
    STRAP: "4c5746",      # the thong strap, darker
    HAIR_DARK: "625e58",  # dark grey, the underside and the back
    HAIR_LIT: "a39e94",   # the lit locks, grey
    BEARD: "858077",      # salt-grey beard, darker than the lit hair
    INK: "1f1c20",
    9: "8c826c", 10: "8c826c", 11: "8c826c", 12: "8c826c",
    SKIN_DARK: "7a4e36",
    SKIN_MID: "8a5a3f",
    SKIN: "936146",       # weathered, sun-darkened
}

# ---------------------------------------------------------------------------
# THE PAINTED SWATCHES. The top half of a 1024 atlas, an 8 x 4 grid of 128 px cells; UVs are
# inset 3 px so the bilinear filter never reads a neighbour.
# ---------------------------------------------------------------------------
ATLAS = 1024
CELL = 128
INSET = 3

SHIRT = "aca288"; SHIRT_FADE = "b8af96"; SHIRT_SWEAT = "a0967c"; SHIRT_GRIME = "8b806a"
SHIRT_FRAY = "6e6454"; SHIRT_PATCH = "7d6a55"; SHIRT_PATCH_EDGE = "63553f"
PANTS = "5b4f43"; PANTS_DUST = "7a6f60"; PANTS_DARK = "463d34"; PANTS_WORN = "685b4d"
DENIM = "67727b"; DENIM_EDGE = "4d565d"; CUFF = "6c6051"
SKIN_DUST = "8a7865"; SKIN_DIRT = "684b3a"

GARMENT_HEXES = [SHIRT, SHIRT_FADE, SHIRT_SWEAT, SHIRT_GRIME, SHIRT_FRAY, SHIRT_PATCH,
                 SHIRT_PATCH_EDGE, PANTS, PANTS_DUST, PANTS_DARK, PANTS_WORN, DENIM, DENIM_EDGE,
                 CUFF, PALETTE[SANDAL], PALETTE[STRAP], PALETTE[HAIR_DARK], PALETTE[HAIR_LIT],
                 PALETTE[BEARD]]

SWATCHES = [
    "shirt_front", "shirt_back", "shirt_side", "shirt_top", "shirt_bottom",
    "sleeve_left", "sleeve_right", "sleeve_end",
    "leg_left_front", "leg_right_front", "leg_back", "leg_side",
    "hip_front", "hip_back", "hip_side", "cuff",
    "foot", "shin", "forearm", "hand", "hand_palm", "flap",
]


def _rgb(hex_str):
    h = hex_str.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


class Swatch:
    """One 128 px cell: a flat fill and hand-set feathered marks over it."""

    def __init__(self, base):
        self.img = Image.new("RGB", (CELL, CELL), _rgb(base))

    def mark(self, colour, points, feather=6.0, strength=1.0, outline=None, width=0):
        """A polygon in swatch units (0..1, u right, v down), its edge feathered by `feather` px."""
        mask = Image.new("L", (CELL, CELL), 0)
        pts = [(u * CELL, v * CELL) for u, v in points]
        ImageDraw.Draw(mask).polygon(pts, fill=int(255 * strength))
        if feather > 0:
            mask = mask.filter(ImageFilter.GaussianBlur(feather))
        self.img.paste(Image.new("RGB", (CELL, CELL), _rgb(colour)), (0, 0), mask)
        if outline:
            edge = Image.new("L", (CELL, CELL), 0)
            ImageDraw.Draw(edge).line(pts + [pts[0]], fill=230, width=width, joint="curve")
            edge = edge.filter(ImageFilter.GaussianBlur(1.0))
            self.img.paste(Image.new("RGB", (CELL, CELL), _rgb(outline)), (0, 0), edge)
        return self


def paint_swatches():
    """Every swatch, drawn by hand. u runs across a face, v DOWN it: for a torso or a leg v=0
    is the top of the box, for an arm v=0 is the shoulder end."""
    s = {}

    # THE SHIRT. An old tee gone grey-khaki: the sun has bleached the shoulders and the back,
    # the hem is grimy, sweat has darkened the chest, the armpits and the spine.
    s["shirt_front"] = (Swatch(SHIRT)
        .mark(SHIRT_FADE, [(0, 0), (1, 0), (1, .25), (.81, .30), (.56, .26), (.31, .33), (0, .27)], 7)
        .mark(SHIRT_SWEAT, [(.38, .37), (.53, .33), (.64, .40), (.63, .53), (.54, .61), (.42, .58), (.35, .49)], 8, .7)
        .mark(SHIRT_GRIME, [(0, .80), (.21, .75), (.46, .82), (.71, .76), (1, .81), (1, 1), (0, 1)], 6, .85)
        .mark(SHIRT_GRIME, [(.69, .58), (.84, .55), (.91, .65), (.81, .72), (.70, .69)], 5, .55)
        # the hole: a jagged tear, crisp enough to read as a hole and not a stain
        .mark(SHIRT_FRAY, [(.14, .63), (.21, .60), (.25, .66), (.30, .63), (.28, .72), (.21, .75), (.16, .70)], 1.2))
    s["shirt_back"] = (Swatch(SHIRT)
        .mark(SHIRT_FADE, [(0, 0), (1, 0), (1, .40), (.74, .47), (.50, .40), (.23, .48), (0, .42)], 8)
        .mark(SHIRT_SWEAT, [(.44, .19), (.57, .21), (.61, .49), (.55, .67), (.45, .66), (.40, .47)], 8, .65)
        .mark(SHIRT_PATCH, [(.61, .52), (.86, .49), (.89, .72), (.64, .75)], 1.0, 1.0, SHIRT_PATCH_EDGE, 3)
        .mark(SHIRT_GRIME, [(0, .85), (.30, .79), (.61, .86), (1, .80), (1, 1), (0, 1)], 6, .8))
    s["shirt_side"] = (Swatch(SHIRT)
        .mark(SHIRT_SWEAT, [(.18, 0), (.82, 0), (.86, .19), (.61, .31), (.29, .28), (.14, .14)], 8, .75)
        .mark(SHIRT_GRIME, [(0, .83), (.52, .77), (1, .85), (1, 1), (0, 1)], 6, .8))
    s["shirt_top"] = (Swatch(SHIRT_FADE)
        .mark(SHIRT, [(.1, .7), (.4, .62), (.52, .9), (.2, .95)], 9, .5))
    s["shirt_bottom"] = Swatch(SHIRT_GRIME)
    s["flap"] = (Swatch(SHIRT)
        .mark(SHIRT_GRIME, [(0, .45), (.35, .38), (.7, .5), (1, .42), (1, 1), (0, 1)], 6, .9))

    # THE SLEEVES. Left torn at the elbow into a frayed edge, right with a small sewn patch.
    s["sleeve_left"] = (Swatch(SHIRT)
        .mark(SHIRT_FADE, [(0, 0), (1, 0), (1, .38), (.61, .45), (.29, .39), (0, .46)], 7)
        .mark(SHIRT_GRIME, [(.1, .55), (.5, .5), (.92, .58), (.9, .75), (.1, .76)], 6, .6)
        .mark(SHIRT_FRAY, [(0, .83), (.12, .77), (.21, .88), (.33, .79), (.43, .90), (.55, .78),
                           (.66, .87), (.79, .79), (.9, .89), (1, .81), (1, 1), (0, 1)], 1.2))
    s["sleeve_right"] = (Swatch(SHIRT)
        .mark(SHIRT_FADE, [(0, 0), (1, 0), (1, .33), (.55, .40), (.25, .34), (0, .41)], 7)
        .mark(SHIRT_PATCH, [(.30, .36), (.63, .33), (.66, .60), (.33, .63)], 1.0, 1.0, SHIRT_PATCH_EDGE, 3)
        .mark(SHIRT_GRIME, [(0, .80), (.45, .74), (1, .82), (1, 1), (0, 1)], 6, .85))
    s["sleeve_end"] = (Swatch(SHIRT_FRAY)
        .mark(SHIRT_GRIME, [(.15, .15), (.85, .15), (.85, .85), (.15, .85)], 10, .6))

    # THE TROUSERS. Dark brown gone dusty: a faded denim patch on the left knee, both knees
    # worn lighter, a dark stain on the right thigh, dust at the bottoms and over the seat.
    s["leg_left_front"] = (Swatch(PANTS)
        .mark(PANTS_WORN, [(.14, .33), (.86, .31), (.90, .72), (.12, .74)], 8, .8)
        .mark(DENIM, [(.23, .39), (.75, .36), (.79, .67), (.21, .69)], 1.0, 1.0, DENIM_EDGE, 3)
        .mark(PANTS_DUST, [(0, .80), (.38, .73), (.71, .81), (1, .75), (1, 1), (0, 1)], 7, .85))
    s["leg_right_front"] = (Swatch(PANTS)
        .mark(PANTS_WORN, [(.19, .37), (.83, .35), (.81, .67), (.17, .65)], 9, .9)
        .mark(PANTS_DARK, [(.54, .12), (.76, .10), (.81, .26), (.61, .31)], 5, .6)
        .mark(PANTS_DUST, [(0, .76), (.33, .82), (.66, .74), (1, .80), (1, 1), (0, 1)], 7, .85))
    s["leg_back"] = (Swatch(PANTS)
        .mark(PANTS_WORN, [(.2, .42), (.8, .44), (.78, .62), (.22, .6)], 8, .6)
        .mark(PANTS_DUST, [(0, .74), (.5, .70), (1, .77), (1, 1), (0, 1)], 7, .9))
    s["leg_side"] = (Swatch(PANTS)
        .mark(PANTS_DUST, [(0, .78), (.45, .73), (1, .79), (1, 1), (0, 1)], 7, .85))
    s["hip_front"] = (Swatch(PANTS)
        .mark(PANTS_WORN, [(.3, .2), (.7, .18), (.72, .7), (.28, .72)], 10, .5))
    s["hip_back"] = (Swatch(PANTS)
        .mark(PANTS_DUST, [(.10, .28), (.9, .26), (.93, .92), (.08, .93)], 10, .75))
    s["hip_side"] = Swatch(PANTS)
    s["cuff"] = (Swatch(CUFF)
        .mark(PANTS_DARK, [(0, .44), (1, .41), (1, .58), (0, .61)], 4, .5)
        .mark(PANTS_DUST, [(0, .70), (.52, .62), (1, .72), (1, 1), (0, 1)], 6, .8))

    # THE SKIN that shows: dusty feet and shins, a smudged forearm, dirty fingertips.
    skin = PALETTE[SKIN]
    s["foot"] = (Swatch(skin)
        .mark(SKIN_DUST, [(0, .52), (.5, .46), (1, .54), (1, 1), (0, 1)], 8, .85)
        .mark(SKIN_DIRT, [(0, .86), (1, .82), (1, 1), (0, 1)], 3, .55))
    s["shin"] = (Swatch(skin)
        .mark(SKIN_DUST, [(0, .46), (.5, .40), (1, .49), (1, 1), (0, 1)], 9, .8)
        .mark(SKIN_DIRT, [(.58, .14), (.79, .11), (.83, .30), (.62, .31)], 4, .35))
    s["forearm"] = (Swatch(skin)
        .mark(SKIN_DIRT, [(.28, .38), (.60, .33), (.69, .60), (.34, .66)], 7, .3))
    s["hand"] = (Swatch(skin)
        .mark(SKIN_DIRT, [(0, .70), (1, .65), (1, 1), (0, 1)], 6, .5))
    s["hand_palm"] = (Swatch(skin)
        .mark(SKIN_DIRT, [(.1, .1), (.9, .1), (.9, .9), (.1, .9)], 9, .45))

    missing = set(SWATCHES) - set(s)
    if missing:
        raise SystemExit(f"swatches not painted: {sorted(missing)}")
    return s


def swatch_rect(name):
    i = SWATCHES.index(name)
    col, row = i % 8, i // 8
    if row >= 4:
        raise SystemExit("too many swatches for the top half of the atlas")
    return col * CELL, row * CELL


def write_atlas(swatches):
    """The stock colormap scaled x2 (nearest) with the swatches over its top half."""
    atlas = Image.open(COLORMAP).convert("RGB").resize((ATLAS, ATLAS), Image.NEAREST)
    top = Image.new("RGB", (ATLAS, ATLAS // 2), _rgb("8c826c"))
    atlas.paste(top, (0, 0))
    for name, sw in swatches.items():
        x, y = swatch_rect(name)
        atlas.paste(sw.img, (x, y))
    atlas.save(ATLAS_OUT)
    print(f"wrote {ATLAS_OUT}")
    return atlas


# ---------------------------------------------------------------------------
# THE CHARACTER, in the .glb's own space: metres, +Y up, +Z the FRONT, +X the character's
# LEFT (the bone named `leg-left` sits at +0.0836). No flip, no family remap: the skeleton is
# the base rig's own, so these numbers are the final ones.
#
#   bones (world, at rest): legs (+/-0.0836, 0.176, -0.029), torso (0, 0.176, -0.029),
#   arms (+/-0.0999, 0.288, -0.017), head (0, 0.343, -0.002).
#   The arms are authored straight out along X (the Kenney T pose); the clips swing them down.
#   The hand is centred on the shoulder height with its top at shoulder + 0.0617
#   (`HandTopLift`), as on every Person.
#
# A box is (name, bone, lo, hi, paint[, long axis[, tilt degrees]]). `paint` is a palette
# slot, or a dict of swatches by face kind ("front", "back", "side", "top", "bottom", "end",
# "*" the fallback). The long axis is 1 (Y) for the body and 0 (X) for an arm: it is what a
# swatch's v runs along.
# ---------------------------------------------------------------------------

def shirt(front="shirt_front"):
    return {"front": front, "back": "shirt_back", "side": "shirt_side", "top": "shirt_top",
            "bottom": "shirt_bottom", "*": "shirt_side"}


def leg(front):
    return {"front": front, "back": "leg_back", "side": "leg_side", "*": "leg_side"}


HIP = {"front": "hip_front", "back": "hip_back", "side": "hip_side", "*": "hip_side"}

LEG_LEFT = [
    # A worn tsinelas: the sole, then a bare dusty foot on it, the thong strap over the toes.
    ("sandal-left", "leg-left", (0.030, 0.000, -0.086), (0.140, 0.016, 0.106), SANDAL),
    ("foot-left", "leg-left", (0.038, 0.014, -0.070), (0.132, 0.050, 0.096), {"*": "foot"}),
    ("strap-left", "leg-left", (0.035, 0.040, 0.028), (0.135, 0.057, 0.050), STRAP),
    # The bare ankle and shin under a rolled trouser leg.
    ("shin-left", "leg-left", (0.050, 0.046, -0.044), (0.120, 0.100, 0.034), {"*": "shin"}),
    ("cuff-left", "leg-left", (0.031, 0.090, -0.062), (0.139, 0.119, 0.052), {"*": "cuff"}),
    ("trouser-left", "leg-left", (0.035, 0.112, -0.057), (0.135, 0.182, 0.047), leg("leg_left_front")),
]

LEG_RIGHT = [
    ("sandal-right", "leg-right", (-0.140, 0.000, -0.086), (-0.030, 0.016, 0.106), SANDAL),
    ("foot-right", "leg-right", (-0.132, 0.014, -0.070), (-0.038, 0.050, 0.096), {"*": "foot"}),
    # The strap has snapped: a stub on the outside of the foot, its loose end bent up.
    ("strap-stub-right", "leg-right", (-0.137, 0.040, 0.026), (-0.098, 0.057, 0.050), STRAP),
    ("strap-end-right", "leg-right", (-0.100, 0.046, 0.030), (-0.074, 0.058, 0.046), STRAP, 1, (0, 0, 28)),
    ("shin-right", "leg-right", (-0.120, 0.046, -0.044), (-0.050, 0.100, 0.034), {"*": "shin"}),
    ("cuff-right", "leg-right", (-0.139, 0.094, -0.062), (-0.031, 0.121, 0.052), {"*": "cuff"}),
    ("trouser-right", "leg-right", (-0.135, 0.114, -0.057), (-0.035, 0.182, 0.047), leg("leg_right_front")),
]

TORSO = [
    # The trousers' seat and hips, under the shirt's hem.
    ("hips", "torso", (-0.132, 0.168, -0.090), (0.132, 0.232, 0.062), HIP),
    # The shirt, untucked and thin on him, with a torn corner of the hem hanging on his left.
    ("shirt", "torso", (-0.128, 0.205, -0.086), (0.128, 0.350, 0.058), shirt()),
    ("hem-flap", "torso", (0.052, 0.186, 0.030), (0.126, 0.214, 0.061), {"*": "flap"}, 1, (0, 0, -9)),
]

ARM_LEFT = [
    ("sleeve-left", "arm-left", (0.092, 0.228, -0.076), (0.206, 0.350, 0.042),
     {"end": "sleeve_end", "*": "sleeve_left"}, 0),
    ("forearm-left", "arm-left", (0.200, 0.246, -0.058), (0.300, 0.330, 0.024), {"*": "forearm"}, 0),
    ("hand-left", "arm-left", (0.290, 0.2263, -0.050), (0.3836, 0.3497, 0.052),
     {"bottom": "hand_palm", "*": "hand"}, 0),
]

ARM_RIGHT = [
    ("sleeve-right", "arm-right", (-0.206, 0.228, -0.076), (-0.092, 0.350, 0.042),
     {"end": "sleeve_end", "*": "sleeve_right"}, 0),
    ("forearm-right", "arm-right", (-0.300, 0.246, -0.058), (-0.200, 0.330, 0.024), {"*": "forearm"}, 0),
    ("hand-right", "arm-right", (-0.3836, 0.2263, -0.050), (-0.290, 0.3497, 0.052),
     {"bottom": "hand_palm", "*": "hand"}, 0),
]

# The skull is the donor's: cranium +/-0.16 wide from y 0.393 to 0.614 (its top +/-0.12 at
# 0.661), z -0.162 to 0.158, ears to +/-0.227 at y 0.41 to 0.51 and z -0.09 to 0.01, the face
# plane at z 0.1596 with the eyes at y 0.46 to 0.51 and the mouth at 0.40 to 0.44.
HEAD = [
    # Greying hair, thinning and receding, built the cast's way (CHARACTER_MODEL_METHOD 2): a
    # dark core hugging the back and the sides, then separate chunky locks at graded heights
    # and angles, lit grey on top. One slab over the back read as a helmet (2026-09-30 v1).
    ("hair-core-back", "head", (-0.158, 0.455, -0.176), (0.158, 0.640, -0.096), HAIR_DARK),
    ("hair-core-left", "head", (0.146, 0.515, -0.140), (0.176, 0.628, 0.012), HAIR_DARK),
    ("hair-core-right", "head", (-0.176, 0.515, -0.140), (-0.146, 0.628, 0.012), HAIR_DARK),
    ("hair-core-top", "head", (-0.132, 0.622, -0.168), (0.132, 0.678, 0.058), HAIR_DARK),
    # The nape: three locks of different lengths, so the bottom edge is ragged.
    ("lock-nape-left", "head", (0.040, 0.402, -0.186), (0.150, 0.478, -0.124), HAIR_DARK, 1, (0, 0, 7)),
    ("lock-nape-mid", "head", (-0.058, 0.386, -0.190), (0.050, 0.468, -0.124), HAIR_DARK),
    ("lock-nape-right", "head", (-0.154, 0.414, -0.184), (-0.052, 0.482, -0.124), HAIR_DARK, 1, (0, 0, -9)),
    # Locks over the ears, sticking out behind them.
    ("lock-ear-left", "head", (0.150, 0.470, -0.130), (0.196, 0.540, -0.050), HAIR_DARK, 1, (0, 0, -14)),
    ("lock-ear-right", "head", (-0.194, 0.478, -0.120), (-0.150, 0.548, -0.044), HAIR_DARK, 1, (0, 0, 12)),
    # The lit locks: a ring over the crown at graded heights, a wisp over the bare brow, two
    # sticking out at the sides, three down the back.
    ("lock-crown-a", "head", (-0.122, 0.666, -0.052), (-0.036, 0.716, 0.040), HAIR_LIT, 1, (0, 0, 12)),
    ("lock-crown-b", "head", (-0.022, 0.672, -0.022), (0.072, 0.728, 0.070), HAIR_LIT, 1, (8, 0, -8)),
    ("lock-crown-c", "head", (0.070, 0.662, -0.086), (0.142, 0.708, 0.010), HAIR_LIT, 1, (0, 0, -18)),
    ("lock-crown-d", "head", (-0.094, 0.660, -0.142), (0.008, 0.702, -0.060), HAIR_LIT, 1, (-12, 0, 6)),
    ("lock-crown-e", "head", (0.020, 0.654, -0.174), (0.112, 0.696, -0.096), HAIR_LIT, 1, (-18, 0, 0)),
    ("wisp-brow", "head", (-0.070, 0.656, 0.058), (0.018, 0.686, 0.118), HAIR_LIT, 1, (18, 0, 10)),
    ("lock-out-left", "head", (0.140, 0.600, -0.074), (0.192, 0.650, 0.018), HAIR_LIT, 1, (0, 0, -28)),
    ("lock-out-right", "head", (-0.196, 0.586, -0.112), (-0.142, 0.636, -0.030), HAIR_LIT, 1, (0, 0, 26)),
    ("lock-back-a", "head", (-0.142, 0.560, -0.204), (-0.050, 0.630, -0.150), HAIR_LIT, 1, (0, 0, 8)),
    ("lock-back-b", "head", (0.030, 0.530, -0.202), (0.132, 0.600, -0.150), HAIR_LIT, 1, (0, 0, -10)),
    ("lock-back-c", "head", (-0.050, 0.602, -0.198), (0.052, 0.662, -0.144), HAIR_LIT),
    # A short salt-grey beard, all under the mouth and a shade darker than the hair: a chin
    # block and a lock along each jaw. No moustache: the mouth stays one clean stroke.
    # (v1's wide pale block from ear to ear read as a mask.)
    ("beard-chin", "head", (-0.086, 0.336, 0.112), (0.086, 0.394, 0.170), BEARD),
    ("beard-jaw-left", "head", (0.080, 0.346, 0.056), (0.150, 0.406, 0.152), BEARD, 1, (0, 0, 6)),
    ("beard-jaw-right", "head", (-0.150, 0.346, 0.056), (-0.080, 0.406, 0.152), BEARD, 1, (0, 0, -6)),
]

BODY = LEG_LEFT + LEG_RIGHT + TORSO + ARM_LEFT + ARM_RIGHT

SKELETON = {
    "root": (0.0, 0.0, 0.0), "leg-left": (0.0836, 0.176, -0.029), "leg-right": (-0.0836, 0.176, -0.029),
    "torso": (0.0, 0.176, -0.029), "arm-left": (0.0999, 0.288, -0.017), "arm-right": (-0.0999, 0.288, -0.017),
    "head": (0.0, 0.343, -0.002),
}

# ---------------------------------------------------------------------------
# THE FACE: the donor's eyes, lidded a little (older, tired, not sad), and his own mouth, a
# short calm stroke with the faintest lift at the ends. Drawn on the face plate like Inday's,
# never bent from the donor's open grin (Voxel_Person_Guide 5.4, build_person_voxel's notes).
# ---------------------------------------------------------------------------
EYE_SQUASH = 0.84
EYE_DROP = 0.004
MOUTH_HALF = 0.034
MOUTH_Y = 0.419
MOUTH_THICK = 0.0088
MOUTH_THIN = 0.0062
MOUTH_LIFT = 0.0028
MOUTH_STEPS = 12


def donor_head():
    pos, nrm, uv, tris = bpv._donor_part(bpv.DONOR_SKULL, {15: SKIN, 8: INK})
    mouth_tris, eyes = set(), set()
    for tri in tris:
        a, b, c = tri
        if bpv._slot_at(*uv[a]) != INK:
            continue
        if (pos[a][1] + pos[b][1] + pos[c][1]) / 3.0 < bpv.DONOR_MOUTH_Y:
            mouth_tris.add(tri)
        else:
            eyes.add(tri)
    if len(mouth_tris) != bpv.DONOR_MOUTH_TRIS or len(eyes) != bpv.DONOR_EYE_TRIS:
        raise SystemExit(f"donor face moved: {len(mouth_tris)} mouth and {len(eyes)} eye triangles")

    before = list(pos)
    eye_verts = {i for tri in eyes for i in tri}
    for side in (1.0, -1.0):
        lid = {i for i in eye_verts if pos[i][0] * side > 0.0}
        centre = sum(pos[i][1] for i in lid) / len(lid)
        for i in lid:
            x, y, z = pos[i]
            pos[i] = (x, centre + (y - centre) * EYE_SQUASH - EYE_DROP, z)
    bpv._verify_expression(before, pos, uv, eye_verts)

    tris = [t for t in tris if t not in mouth_tris]
    plate = bpv.MOUTH_Z + bpv.PANEL_PROUD
    upper, lower = [], []
    for k in range(MOUTH_STEPS + 1):
        t = k / MOUTH_STEPS
        x = -MOUTH_HALF + t * 2.0 * MOUTH_HALF
        e = abs(2.0 * t - 1.0)
        y = MOUTH_Y + MOUTH_LIFT * e * e
        half = 0.5 * (MOUTH_THICK + (MOUTH_THIN - MOUTH_THICK) * e)
        upper.append((x, y + half))
        lower.append((x, y - half))
    ub = len(pos)
    for x, y in upper:
        pos.append((x, y, plate)); nrm.append((0.0, 0.0, 1.0)); uv.append(bpv.cell_uv(INK))
    lb = len(pos)
    for x, y in lower:
        pos.append((x, y, plate)); nrm.append((0.0, 0.0, 1.0)); uv.append(bpv.cell_uv(INK))
    for k in range(MOUTH_STEPS):
        tris.append((ub + k, lb + k, ub + k + 1))
        tris.append((ub + k + 1, lb + k, lb + k + 1))
    return bpv._compact(pos, nrm, uv, tris)


# ---------------------------------------------------------------------------
# GEOMETRY: the cast's chamfered boxes, with a projected UV for a painted box.
# ---------------------------------------------------------------------------

def _rotate(p, centre, tilt):
    """Rotates p about centre by tilt (degrees about X, then Y, then Z)."""
    x, y, z = (p[0] - centre[0], p[1] - centre[1], p[2] - centre[2])
    ax, ay, az = (math.radians(a) for a in tilt)
    y, z = y * math.cos(ax) - z * math.sin(ax), y * math.sin(ax) + z * math.cos(ax)
    x, z = x * math.cos(ay) + z * math.sin(ay), -x * math.sin(ay) + z * math.cos(ay)
    x, y = x * math.cos(az) - y * math.sin(az), x * math.sin(az) + y * math.cos(az)
    return (x + centre[0], y + centre[1], z + centre[2])


def _kind(normal, axis):
    d = max(range(3), key=lambda a: abs(normal[a]))
    s = normal[d] > 0
    if axis == 0 and d == 0:
        return "end", d, s
    if d == 2:
        return ("front" if s else "back"), d, s
    if d == 0:
        return "side", d, s
    return ("top" if s else "bottom"), d, s


def _face_uv(p, lo, hi, kind, d, s, axis):
    n = [(p[a] - lo[a]) / (hi[a] - lo[a]) for a in range(3)]
    if axis == 1:
        if kind == "front":
            return n[0], 1 - n[1]
        if kind == "back":
            return 1 - n[0], 1 - n[1]
        if kind == "side":
            return (1 - n[2] if s else n[2]), 1 - n[1]
        if kind == "top":
            return n[0], 1 - n[2]
        return n[0], n[2]
    # An arm, along X: v runs from the shoulder (v 0) to the hand.
    t = n[0] if lo[0] >= 0 else 1 - n[0]
    if kind == "end":
        return n[2], 1 - n[1]
    if kind == "front":
        return n[1], t
    if kind == "back":
        return 1 - n[1], t
    if kind == "top":
        return n[2], t
    return 1 - n[2], t


def _atlas_uv(name, u, v):
    x, y = swatch_rect(name)
    span = CELL - 2 * INSET
    u = min(max(u, 0.0), 1.0)
    v = min(max(v, 0.0), 1.0)
    return ((x + INSET + u * span) / ATLAS, (y + INSET + v * span) / ATLAS)


def build_mesh(boxes, donor=None):
    pos, nrm, uv, joints, weights, idx = [], [], [], [], [], []
    held = []
    for entry in boxes:
        name, bone, lo, hi, paint = entry[:5]
        axis = entry[5] if len(entry) > 5 else 1
        tilt = entry[6] if len(entry) > 6 else None
        for a in range(3):
            if hi[a] <= lo[a]:
                raise SystemExit(f"box '{name}' is inside out on axis {a}")
        j = bpv.BONE[bone]
        centre = tuple((lo[a] + hi[a]) * 0.5 for a in range(3))
        for normal, points in bpv.box_polygons(lo, hi, -1, bpv.bevel_for(lo, hi)):
            first = len(pos)
            kind, d, s = _kind(normal, axis)
            for p in points:
                if isinstance(paint, int):
                    uv.append(bpv.cell_uv(paint))
                else:
                    sw = paint.get(kind, paint.get("*"))
                    uv.append(_atlas_uv(sw, *_face_uv(p, lo, hi, kind, d, s, axis)))
                q, nn = p, normal
                if tilt:
                    q = _rotate(p, centre, tilt)
                    nn = _rotate(normal, (0.0, 0.0, 0.0), tilt)
                pos.append(q); nrm.append(nn)
                joints.append((j, 0, 0, 0)); weights.append((1.0, 0.0, 0.0, 0.0))
            for k in range(1, len(points) - 1):
                idx += [first, first + k, first + k + 1]
    if donor is not None:
        dpos, dnrm, duv, dtris = donor
        base = len(pos)
        for i in range(len(dpos)):
            held.append(len(pos))
            pos.append(tuple(dpos[i])); nrm.append(tuple(dnrm[i])); uv.append(tuple(duv[i]))
            joints.append((bpv.BONE["head"], 0, 0, 0)); weights.append((1.0, 0.0, 0.0, 0.0))
        for a, b, c in dtris:
            idx += [base + a, base + b, base + c]
    return pos, bpv.smooth_normals(pos, nrm, held), uv, joints, weights, idx


# ---------------------------------------------------------------------------
# CHECKS
# ---------------------------------------------------------------------------

def _near_role_hue(hex_str):
    import colorsys
    r, g, b = (c / 255.0 for c in _rgb(hex_str))
    h, s, v = colorsys.rgb_to_hsv(r, g, b)
    for role in ("f87020", "0080e8"):
        rr, rg, rb = (c / 255.0 for c in _rgb(role))
        hr, _, _ = colorsys.rgb_to_hsv(rr, rg, rb)
        dh = abs(h - hr)
        dh = min(dh, 1 - dh)
        if dh < 0.05 and s > 0.45 and v > 0.45:
            return role
    return None


def verify(body, head):
    everything = body[0] + head[0]
    lo = [min(v[a] for v in everything) for a in range(3)]
    hi = [max(v[a] for v in everything) for a in range(3)]
    height = hi[1] - lo[1]
    print(f"boxes: body={len(BODY)} head={len(HEAD)}; verts body={len(body[0])} head={len(head[0])}")
    print(f"bounds min={[round(v, 4) for v in lo]} max={[round(v, 4) for v in hi]} height={height:.4f}")
    if not (bpv.CAST_MIN_HEIGHT - 0.005 <= height <= bpv.CAST_MAX_HEIGHT + 0.005):
        raise SystemExit(f"height {height:.4f} is outside the cast's range")
    if abs(lo[1]) > 0.001:
        raise SystemExit(f"feet at y={lo[1]:.4f}, not 0")
    reach_head = (bpv.CAST_MAX_HEIGHT - 0.343) * 1.05
    for entry in BODY + HEAD:
        name, bone, blo, bhi = entry[:4]
        origin = SKELETON[bone]
        # An arm reaches along X; everything else is measured in Y, as the cast's builder does.
        a = 0 if bone.startswith("arm") else 1
        reach = max(abs(blo[a] - origin[a]), abs(bhi[a] - origin[a]))
        if reach > (reach_head if bone == "head" else 0.30):
            raise SystemExit(f"box '{name}' is {reach:.3f} from the {bone} bone")
    for hex_str in GARMENT_HEXES:
        if _near_role_hue(hex_str):
            raise SystemExit(f"#{hex_str} is near a role hue")
    r, g, b = (c / 255.0 for c in _rgb(PALETTE[INK]))
    if 0.2126 * r + 0.7152 * g + 0.0722 * b > bpv.MAX_FACE_LUMINANCE:
        raise SystemExit("slot 8 must stay dark: it draws the face")
    hand = [e for e in BODY if e[0] == "hand-left"][0]
    if abs(hand[3][1] - (SKELETON["arm-left"][1] + 0.0617)) > 0.002:
        raise SystemExit("the hand's top must sit at the shoulder + HandTopLift (0.0617)")


# ---------------------------------------------------------------------------
# THE .glb
# ---------------------------------------------------------------------------

def main():
    gltf, buffer = bpv.read_glb(BASE)
    by_name = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    for bone, world in SKELETON.items():
        # The base rig's own skeleton, measured, not moved. Checked so a different base is caught.
        n = gltf["nodes"][by_name[bone]]
        parent = {"leg-left": "root", "leg-right": "root", "torso": "root", "arm-left": "torso",
                  "arm-right": "torso", "head": "torso"}.get(bone)
        local = n.get("translation", [0.0, 0.0, 0.0])
        at = [local[a] + (SKELETON[parent][a] if parent else 0.0) for a in range(3)]
        if any(abs(at[a] - world[a]) > 0.002 for a in range(3)):
            raise SystemExit(f"{bone} of {BASE} is at {at}, not {world}")

    swatches = paint_swatches()
    body = build_mesh(BODY)
    head = build_mesh(HEAD, donor=donor_head())
    verify(body, head)

    blob = bytearray()
    views, accessors, remap = [], [], {}

    def align():
        while len(blob) % 4:
            blob.append(0)

    def keep(old):
        if old in remap:
            return remap[old]
        acc = dict(gltf["accessors"][old])
        data = bpv.accessor_bytes(gltf, buffer, old)
        align()
        acc["bufferView"] = len(views)
        acc.pop("byteOffset", None)
        views.append({"buffer": 0, "byteOffset": len(blob), "byteLength": len(data)})
        blob.extend(data)
        remap[old] = len(accessors)
        accessors.append(acc)
        return remap[old]

    def add(values, fmt, kind, component, minmax=False):
        align()
        start = len(blob)
        for v in values:
            blob.extend(struct.pack("<" + fmt * len(v), *v))
        acc = {"bufferView": len(views), "componentType": component, "count": len(values), "type": kind}
        if minmax:
            acc["min"] = [min(v[a] for v in values) for a in range(len(values[0]))]
            acc["max"] = [max(v[a] for v in values) for a in range(len(values[0]))]
        views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        accessors.append(acc)
        return len(accessors) - 1

    for skin in gltf["skins"]:
        skin["inverseBindMatrices"] = keep(skin["inverseBindMatrices"])
    for anim in gltf["animations"]:
        for sampler in anim["samplers"]:
            sampler["input"] = keep(sampler["input"])
            sampler["output"] = keep(sampler["output"])

    for mesh, built in ((gltf["meshes"][0], body), (gltf["meshes"][1], head)):
        pos, nrm, uv, joints, weights, idx = built
        mesh["primitives"] = [{
            "attributes": {
                "POSITION": add(pos, "f", "VEC3", 5126, minmax=True),
                "NORMAL": add(nrm, "f", "VEC3", 5126),
                "TEXCOORD_0": add(uv, "f", "VEC2", 5126),
                "JOINTS_0": add(joints, "H", "VEC4", 5123),
                "WEIGHTS_0": add(weights, "f", "VEC4", 5126),
            },
            "indices": add([(i,) for i in idx], "I", "SCALAR", 5125),
            "material": 0,
            "mode": 4,
        }]

    gltf["accessors"] = accessors
    gltf["bufferViews"] = views
    gltf["buffers"] = [{"byteLength": len(blob)}]
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso voxel person builder (beggar)"}
    gltf["images"] = [{"uri": ATLAS_NAME, "name": "npc-beggar-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": "npc-beggar-atlas"}]
    # Bilinear, not the stock nearest: the painted swatches are drawn at 128 px a face.
    gltf["samplers"] = [{"magFilter": 9729, "minFilter": 9987}]
    gltf["materials"][0]["name"] = "npc-beggar"
    gltf["nodes"][0]["name"] = "npc-beggar"
    gltf["scenes"][0]["name"] = "npc-beggar"

    os.makedirs(FOLDER, exist_ok=True)
    write_atlas(swatches)
    bpv.write_glb(OUT, gltf, blob)
    with open(PALETTE_OUT, "w", encoding="utf-8", newline="\n") as handle:
        json.dump({"palette": [PALETTE[i] for i in range(16)]}, handle, indent=1)
    print(f"wrote {PALETTE_OUT}")
    if "--preview" in sys.argv:
        preview(body, head)


# ---------------------------------------------------------------------------
# A quick look without Unity: bind pose, front, three-quarter and back, flat-lit, textured.
# ---------------------------------------------------------------------------

def preview(body, head, path="Logs/ilalim-unity/beggar_preview.png"):
    import numpy as np
    atlas = np.asarray(Image.open(ATLAS_OUT).convert("RGB"), dtype=np.float32)
    pal = {i: np.array(_rgb(PALETTE[i]), dtype=np.float32) for i in range(16)}
    W, H, S = 420, 460, 520.0
    views = []
    for yaw in (0.0, 35.0, 90.0, 180.0):
        img = np.full((H, W, 3), 60.0, dtype=np.float32)
        zb = np.full((H, W), -1e9, dtype=np.float32)
        cy, sy = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        for pos, nrm, uv, _, _, idx in (body, head):
            P = np.array(pos); N = np.array(nrm); U = np.array(uv)
            X = P[:, 0] * cy + P[:, 2] * sy
            Z = -P[:, 0] * sy + P[:, 2] * cy
            NX = N[:, 0] * cy + N[:, 2] * sy
            NZ = -N[:, 0] * sy + N[:, 2] * cy
            sx = W / 2 + X * S
            sy_ = H - 20 - P[:, 1] * S
            light = np.clip(0.55 + 0.45 * (NZ * 0.6 + N[:, 1] * 0.6 - NX * 0.3), 0.3, 1.0)
            for t in range(0, len(idx), 3):
                a, b, c = idx[t], idx[t + 1], idx[t + 2]
                xs = np.array([sx[a], sx[b], sx[c]]); ys = np.array([sy_[a], sy_[b], sy_[c]])
                x0, x1 = int(max(0, xs.min())), int(min(W - 1, xs.max() + 1))
                y0, y1 = int(max(0, ys.min())), int(min(H - 1, ys.max() + 1))
                if x1 < x0 or y1 < y0:
                    continue
                den = (ys[1] - ys[2]) * (xs[0] - xs[2]) + (xs[2] - xs[1]) * (ys[0] - ys[2])
                if abs(den) < 1e-9:
                    continue
                gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
                w0 = ((ys[1] - ys[2]) * (gx - xs[2]) + (xs[2] - xs[1]) * (gy - ys[2])) / den
                w1 = ((ys[2] - ys[0]) * (gx - xs[2]) + (xs[0] - xs[2]) * (gy - ys[2])) / den
                w2 = 1 - w0 - w1
                inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
                if not inside.any():
                    continue
                z = w0 * Z[a] + w1 * Z[b] + w2 * Z[c]
                sub = zb[y0:y1 + 1, x0:x1 + 1]
                win = inside & (z > sub)
                if not win.any():
                    continue
                uu = w0 * U[a, 0] + w1 * U[b, 0] + w2 * U[c, 0]
                vv = w0 * U[a, 1] + w1 * U[b, 1] + w2 * U[c, 1]
                row = np.clip((vv * 16).astype(int), 0, 15)
                col = np.clip((uu * 16).astype(int), 0, 15)
                tex = atlas[np.clip((vv * ATLAS).astype(int), 0, ATLAS - 1), np.clip((uu * ATLAS).astype(int), 0, ATLAS - 1)]
                slot = (col // 2) + np.where(row >= 12, 8, 0)
                palc = np.stack([pal[int(k)] for k in slot.ravel()]).reshape(slot.shape + (3,))
                colour = np.where((row >= 8)[..., None], palc, tex)
                shade = w0 * light[a] + w1 * light[b] + w2 * light[c]
                region = img[y0:y1 + 1, x0:x1 + 1]
                region[win] = colour[win] * shade[win][..., None]
                sub[win] = z[win]
        views.append(Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)))
    sheet = Image.new("RGB", (W * len(views), H))
    for i, v in enumerate(views):
        sheet.paste(v, (i * W, 0))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sheet.save(path)
    print(f"wrote {path}")


if __name__ == "__main__":
    sys.exit(main())
