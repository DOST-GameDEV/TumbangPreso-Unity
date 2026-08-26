"""Builds team-phaister.glb (the Street Witch Hero) and person_team-phaister.tres.

    python tools/build_phaister_voxel.py

Master Reference: media_1787715124521.png
"""
import math
import json
import os
import struct
import sys

BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"

OUT = "Assets/TumbangPreso/Art/characters/persons/team-phaister.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-phaister.tres"
ROSTER_OUT = "Assets/TumbangPreso/Resources/Roster/person_phaister.asset"

BONE = {"root": 0, "leg-left": 1, "leg-right": 2, "torso": 3,
        "arm-left": 4, "arm-right": 5, "head": 6}

PARENT = {"leg-left": "root", "leg-right": "root", "torso": "root",
          "arm-left": "torso", "arm-right": "torso", "head": "torso"}

# ---------------------------------------------------------------------------
# CANONICAL KENNEY CAST SKELETON (0.7850m AUTHORED HEIGHT)
# Standard Kenney cast proportions (Head 56%, Torso 21%, Legs 22%)
# ---------------------------------------------------------------------------
WAS_HIPS, WAS_SHOULDER, WAS_NECK, WAS_TOP = 0.176, 0.288, 0.343, 0.7850
NOW_HIPS, NOW_SHOULDER, NOW_NECK, NOW_TOP = 0.176, 0.288, 0.343, 0.7850

HEAD_GROWTH = 1.0
CAST_MIN_HEIGHT, CAST_MAX_HEIGHT = 0.6613, 0.7928

SKELETON = {
    "root":      (0.0,      0.0,          0.0),
    "leg-left":  (0.08357,  NOW_HIPS,     -0.02875),
    "leg-right": (-0.08357, NOW_HIPS,     -0.02875),
    "torso":     (0.0,      NOW_HIPS,     -0.02875),
    "arm-left":  (0.0999,   NOW_SHOULDER, -0.01725),
    "arm-right": (-0.0999,  NOW_SHOULDER, -0.01725),
    "head":      (0.0,      NOW_NECK,     -0.00236),
}


def cell_uv(slot):
    """Atlas cell for a palette slot."""
    col = 2 * (slot % 8) + 1
    row = 9 if slot < 8 else 13
    return ((col + 0.5) / 16.0, (row + 0.5) / 16.0)


# ---------------------------------------------------------------------------
# 16-COLOR PALETTE DEFINITION (Transcribed from media_1787716960756.png)
# ---------------------------------------------------------------------------
COAT_DARK      = 0   # Deep black/charcoal coat, hat, trousers (#181622)
CLOTH_PURPLE   = 1   # Royal purple hat band, belt, sleeve band, coat trim, shoes (#4a1e78)
LILAC_GEM      = 2   # Medallion crystal, wand crystal tips (#9838d8)
GOLD           = 3   # Hat buckle, necklace chain, waist buckle, sleeve cross, back moon & stars (#f8b824)
WAND_WOOD      = 4   # Warm wood wand shafts (#7c3c20)
WAND_BAND      = 5   # Crimson wand wrap bands (#b83424)
HAIR_MAGENTA   = 6   # Vibrant rich hot pink/magenta hair body (#d8186e)
HAIR_HIGHLIGHT = 7   # Magenta hair highlights and stepped locks (#e82882)
INK            = 8   # Solid dark ink for eyes (#14101c)
TEAL_KNOT      = 9   # Teal/cyan collar knot above medallion (#20b2aa)
CRIMSON        = 10  # Crimson cape, high collar, sleeve stripe, ankle stripe (#8c1424)
GOLD_SHADOW    = 11  # Deep gold buckle shadow (#b87814)
WHITE          = 12  # Crisp white shoe sole slabs, shirt cuffs (#ffffff)
SKIN           = 13  # Warm porcelain peach skin midtone (#f4c098)
SKIN_DARK      = 14  # Warm peach skin shadow (#e0a078)
SKIN_LIT       = 15  # Uniform skin tone (#f4c098)

PALETTE = {
    COAT_DARK:      "181622",   # Deep black/charcoal coat, hat, trousers
    CLOTH_PURPLE:   "4a1e78",   # Royal purple hat band, belt, sleeve band, coat trim, shoes
    LILAC_GEM:      "9838d8",   # Medallion crystal, wand crystal tips
    GOLD:           "f8b824",   # Hat buckle, necklace chain, waist buckle, sleeve cross, back moon & stars
    WAND_WOOD:      "7c3c20",   # Warm wood wand shafts
    WAND_BAND:      "b83424",   # Crimson wand wrap bands
    HAIR_MAGENTA:   "d8186e",   # Vibrant rich hot pink/magenta hair body
    HAIR_HIGHLIGHT: "e82882",   # Magenta hair highlights and stepped locks
    INK:            "14101c",   # Solid dark ink for eyes
    TEAL_KNOT:      "20b2aa",   # Teal/cyan collar knot above medallion
    CRIMSON:        "8c1424",   # Crimson cape, high collar, sleeve stripe, ankle stripe
    GOLD_SHADOW:    "b87814",   # Deep gold buckle shadow
    WHITE:          "ffffff",   # Crisp white shoe sole slabs, shirt cuffs
    SKIN:           "f4c098",   # Warm porcelain peach skin midtone
    SKIN_DARK:      "e0a078",   # Warm peach skin shadow
    SKIN_LIT:       "f4c098",   # Uniform skin tone
}

MAX_FACE_LUMINANCE = 0.30


def mirrored(boxes, bone_from, bone_to):
    out = []
    for name, bone, lo, hi, slot in boxes:
        assert bone == bone_from, f"{name} is on {bone}, not {bone_from}"
        out.append((name.replace("left", "right").replace("-l-", "-r-").replace("-l", "-r"), bone_to,
                    (-hi[0], lo[1], lo[2]), (-lo[0], hi[1], hi[2]), slot))
    return out


# ---------------------------------------------------------------------------
# LEGS & SHOES (Black trousers, crimson ankle band, purple shoes, white sole)
# ---------------------------------------------------------------------------
LEG_LEFT = [
    # 1. Crisp White Sneaker Sole Slab (Y in [0.000, 0.022])
    ("shoe-sole-left",         "leg-left", (0.015, 0.000, -0.125), (0.155, 0.022, 0.075), WHITE),

    # 2. Royal Purple Shoe Upper (Y in [0.022, 0.058])
    ("shoe-upper-left",        "leg-left", (0.022, 0.022, -0.120), (0.148, 0.058, 0.070), CLOTH_PURPLE),
    ("shoe-toe-left",          "leg-left", (0.022, 0.022, -0.125), (0.148, 0.045, -0.080), CLOTH_PURPLE),
    ("shoe-heel-left",         "leg-left", (0.022, 0.022, 0.040),  (0.148, 0.058, 0.075), CLOTH_PURPLE),

    # 3. Crimson Red Ankle Stripe (Y in [0.056, 0.068])
    ("shoe-ankle-crimson-l",   "leg-left", (0.020, 0.056, -0.085), (0.150, 0.068, 0.065), CRIMSON),

    # 4. Full-Length Black Trousers (Y in [0.068, 0.176])
    ("pants-cuff-left",        "leg-left", (0.022, 0.068, -0.080), (0.148, 0.100, 0.060), COAT_DARK),
    ("pants-leg-left",         "leg-left", (0.025, 0.100, -0.075), (0.145, 0.176, 0.065), COAT_DARK),
]

LEG_RIGHT = mirrored(LEG_LEFT, "leg-left", "leg-right")


# ---------------------------------------------------------------------------
# TORSO: HIGH CRIMSON COLLAR, FROCK COAT, CAPE, V-CHAIN, MEDALLION, BELT, MOON
# ---------------------------------------------------------------------------
TORSO = [
    # 1. Black Trousers Pelvis / Hips
    ("pants-pelvis",           "torso", (-0.110, 0.176, -0.075), (0.110, 0.200, 0.070), COAT_DARK),

    # 2. High Upturned Crimson Red Collar (Flanking neck behind chin)
    ("collar-crimson-left",    "torso", (0.040,  0.280, -0.080), (0.090, 0.355, -0.062), CRIMSON),
    ("collar-crimson-right",   "torso", (-0.090, 0.280, -0.080), (-0.040, 0.355, -0.062), CRIMSON),
    ("collar-crimson-back",    "torso", (-0.085, 0.300, 0.055),  (0.085, 0.355, 0.075),  CRIMSON),

    # 3. Crimson Red Cape (Wide drape behind body, visible from front beside arms and below coat)
    ("cape-crimson-back",      "torso", (-0.132, 0.075, 0.076),  (0.132, 0.320, 0.096),  CRIMSON),
    ("cape-crimson-wing-l",    "torso", (0.085,  0.075, 0.045),  (0.138, 0.280, 0.094),  CRIMSON),
    ("cape-crimson-wing-r",    "torso", (-0.138, 0.075, 0.045),  (-0.085, 0.280, 0.094), CRIMSON),

    # 4. Main Coat Body Core & Under-tunic
    ("coat-chest-core",        "torso", (-0.108, 0.230, -0.072), (0.108, 0.343, 0.070), COAT_DARK),
    ("coat-side-l",            "torso", (0.075,  0.200, -0.074), (0.112, 0.343, 0.072), COAT_DARK),
    ("coat-side-r",            "torso", (-0.112, 0.200, -0.074), (-0.075, 0.343, 0.072), COAT_DARK),
    ("shirt-neck-v",           "torso", (-0.040, 0.290, -0.076), (0.040, 0.343, -0.068), COAT_DARK),

    # 5. Front Coat Flaps / Skirt (Flared peplum over trousers) with Royal Purple Trim
    ("coat-skirt-l",           "torso", (0.015,  0.135, -0.084), (0.118, 0.200, 0.075), COAT_DARK),
    ("coat-skirt-r",           "torso", (-0.118, 0.135, -0.084), (-0.015, 0.200, 0.075), COAT_DARK),
    ("coat-skirt-trim-l",      "torso", (0.012,  0.122, -0.087), (0.120, 0.138, 0.078), CLOTH_PURPLE),
    ("coat-skirt-trim-r",      "torso", (-0.120, 0.122, -0.087), (-0.012, 0.138, 0.078), CLOTH_PURPLE),

    # 6. Back Coat Panel (Black over cape, with Purple V-Hem)
    ("coat-back-main",         "torso", (-0.115, 0.135, 0.084),  (0.115, 0.343, 0.098),  COAT_DARK),
    ("coat-back-trim-v",       "torso", (-0.118, 0.122, 0.085),  (0.118, 0.142, 0.100),  CLOTH_PURPLE),

    # 7. Royal Purple Waist Belt & Large Square Gold Buckle
    ("waist-belt-purple",      "torso", (-0.112, 0.198, -0.078), (0.112, 0.230, 0.074), CLOTH_PURPLE),
    ("waist-buckle-gold",      "torso", (-0.038, 0.190, -0.088), (0.038, 0.238, -0.072), GOLD),
    ("waist-buckle-slot",      "torso", (-0.020, 0.198, -0.090), (0.020, 0.230, -0.076), COAT_DARK),
    ("waist-buckle-pin",       "torso", (-0.006, 0.198, -0.092), (0.006, 0.230, -0.078), GOLD),

    # 8. Scalloped Gold V-Chain Necklace, Teal Collar Knot & Amethyst Medallion
    ("collar-teal-knot",       "torso", (-0.014, 0.272, -0.088), (0.014, 0.292, -0.074), TEAL_KNOT),
    ("chain-v-link-l1",        "torso", (0.055,  0.310, -0.082), (0.092, 0.345, -0.068), GOLD),
    ("chain-v-link-l2",        "torso", (0.025,  0.280, -0.086), (0.062, 0.315, -0.072), GOLD),
    ("chain-v-link-r1",        "torso", (-0.092, 0.310, -0.082), (-0.055, 0.345, -0.068), GOLD),
    ("chain-v-link-r2",        "torso", (-0.062, 0.280, -0.086), (-0.025, 0.315, -0.072), GOLD),
    ("chain-v-knot",           "torso", (-0.025, 0.262, -0.088), (0.025, 0.278, -0.074), GOLD),
    ("pendant-gem",            "torso", (-0.022, 0.228, -0.095), (0.022, 0.262, -0.078), LILAC_GEM),
    ("pendant-prong-bot",      "torso", (-0.010, 0.215, -0.092), (0.010, 0.228, -0.078), GOLD),
    ("pendant-gem-hl",         "torso", (-0.008, 0.245, -0.097), (0.008, 0.256, -0.080), WHITE),

    # 9. Back Coat Graphic: Gold Crescent Moon + 2 Stars
    ("back-moon-spine",        "torso", (-0.016, 0.185, 0.099),  (0.016, 0.245, 0.106),  GOLD),
    ("back-moon-top",          "torso", (0.006,  0.230, 0.099),  (0.028, 0.252, 0.106),  GOLD),
    ("back-moon-bot",          "torso", (0.006,  0.178, 0.099),  (0.028, 0.200, 0.106),  GOLD),
    ("back-moon-cut",          "torso", (0.006,  0.195, 0.100),  (0.030, 0.235, 0.107),  COAT_DARK),
    ("back-star-l-vert",       "torso", (-0.070, 0.165, 0.099),  (-0.055, 0.205, 0.106), GOLD),
    ("back-star-l-horiz",      "torso", (-0.085, 0.180, 0.099),  (-0.040, 0.190, 0.106), GOLD),
    ("back-star-r-vert",       "torso", (0.055,  0.160, 0.099),  (0.070, 0.200, 0.106),  GOLD),
    ("back-star-r-horiz",      "torso", (0.040,  0.175, 0.099),  (0.085, 0.185, 0.106),  GOLD),
]


# ---------------------------------------------------------------------------
# ARMS: BLACK UPPER, ROYAL PURPLE FOREARM, SIDE-FACING GOLD CROSS, WHITE CUFF
# ---------------------------------------------------------------------------
ARM_LEFT = [
    # 1. Black Coat Upper Sleeve (X in [0.0999, 0.170])
    ("sleeve-upper-l",         "arm-left", (0.0999, 0.230, -0.055), (0.170, 0.343, 0.055), COAT_DARK),

    # 2. Royal Purple Forearm Band (X in [0.170, 0.235]) - Solid purple on front & back
    ("sleeve-purple-l",        "arm-left", (0.170, 0.224, -0.058),  (0.235, 0.345, 0.058), CLOTH_PURPLE),

    # 3. Small Gold Cross Emblem placed on the TOP (+Y in T-pose -> OUTSIDE in resting pose / side view)
    ("sleeve-cross-v-side-l",  "arm-left", (0.195, 0.344, -0.025),  (0.215, 0.352, 0.025), GOLD),
    ("sleeve-cross-h-side-l",  "arm-left", (0.182, 0.344, -0.012),  (0.228, 0.352, 0.012), GOLD),

    # 4. Crimson Red Sleeve Stripe (X in [0.235, 0.252])
    ("sleeve-crimson-l",       "arm-left", (0.235, 0.222, -0.060),  (0.252, 0.347, 0.060), CRIMSON),

    # 5. Crisp White Shirt Cuff (X in [0.252, 0.280])
    ("sleeve-white-cuff-l",    "arm-left", (0.252, 0.220, -0.062),  (0.280, 0.349, 0.062), WHITE),

    # 6. Warm Peach Skin Hand (X in [0.280, 0.335])
    ("hand-left",              "arm-left", (0.280, 0.238, -0.038),  (0.335, 0.333, 0.038), SKIN),
]

ARM_RIGHT = mirrored(ARM_LEFT, "arm-left", "arm-right")


# ---------------------------------------------------------------------------
# HEAD: WIDE FACE, SEAMLESS FULL MAGENTA HAIR (ZERO BALD SPOTS), WITCH HAT & WANDS
# ---------------------------------------------------------------------------
HEAD = [
    # -----------------------------------------------------------------------
    # 1. 100% Solid Magenta Skull Enclosure (No skin on skull, top, back, or sides!)
    # -----------------------------------------------------------------------
    ("hair-skull-main",        "head", (-0.170, 0.330, -0.140), (0.170, 0.535, 0.175), HAIR_MAGENTA),
    ("hair-forehead-brow",     "head", (-0.170, 0.485, -0.168), (0.170, 0.535, -0.120), HAIR_MAGENTA),

    # -----------------------------------------------------------------------
    # 2. Warm Peach Skin Face Opening (Strictly front plane Y: 0.343-0.485, X: [-0.095, 0.095], Z: [-0.162, -0.135])
    # -----------------------------------------------------------------------
    ("face-opening",           "head", (-0.095, 0.343, -0.162), (0.095, 0.485, -0.135), SKIN),
    ("face-neck-connector",    "head", (-0.050, 0.330, -0.060), (0.050, 0.343, 0.020),  SKIN),

    # Eyes in solid INK (#14101c) (2 blocks tall, crisp and cleanly spaced)
    ("eye-left",               "head", (0.040,  0.415, -0.165), (0.075, 0.485, -0.155), INK),
    ("eye-right",              "head", (-0.075, 0.415, -0.165), (-0.040, 0.485, -0.155), INK),

    # -----------------------------------------------------------------------
    # 3. Front Bangs & Fringe (Framing forehead and dipping into face opening)
    # -----------------------------------------------------------------------
    # Left fringe (viewer's left / -X): Flat stepped horizontal fringe
    ("hair-bang-l-step",       "head", (-0.095, 0.460, -0.170), (-0.030, 0.495, -0.145), HAIR_MAGENTA),

    # Right fringe (viewer's right / +X): Distinct diagonal notched fringe!
    ("hair-bang-r-notch",      "head", (0.015,  0.435, -0.172), (0.055,  0.495, -0.145), HAIR_MAGENTA),
    ("hair-bang-r-step",       "head", (0.055,  0.455, -0.170), (0.095,  0.495, -0.145), HAIR_MAGENTA),

    # -----------------------------------------------------------------------
    # 4. Cheek Framing Side Locks & Side Volume (Draping down to Y=0.315)
    # -----------------------------------------------------------------------
    # Left side (+X, viewer's right)
    ("hair-cheek-l",           "head", (0.095,  0.320, -0.168), (0.155, 0.520, -0.050), HAIR_MAGENTA),
    ("hair-side-l-outer",      "head", (0.135,  0.315, -0.155), (0.180, 0.520, 0.080),  HAIR_MAGENTA),
    ("hair-side-l-rear",       "head", (0.125,  0.315, 0.040),  (0.180, 0.520, 0.160),  HAIR_MAGENTA),
    ("hair-side-l-highlight",  "head", (0.145,  0.355, -0.100), (0.182, 0.475, 0.020),  HAIR_HIGHLIGHT),

    # Right side (-X, viewer's left)
    ("hair-cheek-r",           "head", (-0.155, 0.320, -0.168), (-0.095, 0.520, -0.050), HAIR_MAGENTA),
    ("hair-side-r-outer",      "head", (-0.180, 0.315, -0.155), (-0.135, 0.520, 0.080),  HAIR_MAGENTA),
    ("hair-side-r-rear",       "head", (-0.180, 0.315, 0.040),  (-0.125, 0.520, 0.160),  HAIR_MAGENTA),
    ("hair-side-r-highlight",  "head", (-0.182, 0.355, -0.100), (-0.145, 0.475, 0.020),  HAIR_HIGHLIGHT),

    # -----------------------------------------------------------------------
    # 5. Back Hair Mane (Multi-tiered stepped 3D cascade down to Y=0.280)
    # -----------------------------------------------------------------------
    ("hair-back-tier1",        "head", (-0.170, 0.440, 0.125),  (0.170, 0.535, 0.182),  HAIR_MAGENTA),
    ("hair-back-col-l",        "head", (0.055,  0.330, 0.135),  (0.165, 0.450, 0.185),  HAIR_MAGENTA),
    ("hair-back-col-c",        "head", (-0.055, 0.315, 0.140),  (0.055, 0.450, 0.188),  HAIR_MAGENTA),
    ("hair-back-col-r",        "head", (-0.165, 0.330, 0.135),  (-0.055, 0.450, 0.185), HAIR_MAGENTA),
    ("hair-back-tail",         "head", (-0.028, 0.280, 0.145),  (0.028, 0.335, 0.192),  HAIR_HIGHLIGHT),

    # -----------------------------------------------------------------------
    # 6. Pointed Witch Hat with Large Gold Buckle & 2 Wands
    # -----------------------------------------------------------------------
    # Wide Octagonal Hat Brim (Y in [0.535, 0.565])
    ("hat-brim-core",          "head", (-0.230, 0.535, -0.210), (0.230, 0.565, 0.210),  COAT_DARK),
    ("hat-brim-fb",            "head", (-0.210, 0.535, -0.230), (0.210, 0.565, 0.230),  COAT_DARK),
    ("hat-brim-c-fl",          "head", (0.150,  0.535, -0.220), (0.220, 0.565, -0.150), COAT_DARK),
    ("hat-brim-c-fr",          "head", (-0.220, 0.535, -0.220), (-0.150, 0.565, -0.150), COAT_DARK),
    ("hat-brim-c-bl",          "head", (0.150,  0.535, 0.150),  (0.220, 0.565, 0.220),  COAT_DARK),
    ("hat-brim-c-br",          "head", (-0.220, 0.535, 0.150),  (-0.150, 0.565, 0.220), COAT_DARK),

    # Royal Purple Hat Ribbon Band (Y in [0.565, 0.625])
    ("hat-band-core",          "head", (-0.165, 0.565, -0.165), (0.165, 0.625, 0.165),  CLOTH_PURPLE),
    ("hat-band-front",         "head", (-0.155, 0.565, -0.174), (0.155, 0.625, -0.155), CLOTH_PURPLE),
    ("hat-band-back",          "head", (-0.155, 0.565, 0.155),  (0.155, 0.625, 0.174),  CLOTH_PURPLE),
    ("hat-band-left",          "head", (0.155,  0.565, -0.155), (0.174, 0.625, 0.155),  CLOTH_PURPLE),
    ("hat-band-right",         "head", (-0.174, 0.565, -0.155), (-0.155, 0.625, 0.155), CLOTH_PURPLE),

    # Large Square Gold Hat Buckle (Center Front)
    ("hat-buckle-frame",       "head", (-0.045, 0.570, -0.182), (0.045, 0.620, -0.168), GOLD),
    ("hat-buckle-slot",        "head", (-0.024, 0.578, -0.185), (0.024, 0.612, -0.170), COAT_DARK),
    ("hat-buckle-pin",         "head", (-0.008, 0.578, -0.187), (0.008, 0.612, -0.172), GOLD),

    # 2 Tucked Wands on Left Side (+X, character's left)
    # Wand 1 (Forward wand)
    ("wand1-wood",             "head", (0.155,  0.585, -0.060), (0.178, 0.700, -0.038), WAND_WOOD),
    ("wand1-wrap",             "head", (0.153,  0.600, -0.062), (0.180, 0.620, -0.036), WAND_BAND),
    ("wand1-gem",              "head", (0.150,  0.700, -0.065), (0.182, 0.745, -0.033), LILAC_GEM),
    # Wand 2 (Rear wand)
    ("wand2-wood",             "head", (0.160,  0.585, 0.005),  (0.182, 0.715, 0.028),  WAND_WOOD),
    ("wand2-wrap",             "head", (0.158,  0.605, 0.003),  (0.184, 0.625, 0.030),  WAND_BAND),
    ("wand2-gem",              "head", (0.155,  0.715, 0.000),  (0.186, 0.760, 0.033),  LILAC_GEM),

    # Stepped Cone Tiers
    ("hat-cone-t1",            "head", (-0.145, 0.625, -0.145), (0.145, 0.680, 0.145),  COAT_DARK),
    ("hat-cone-t2",            "head", (-0.120, 0.680, -0.120), (0.120, 0.725, 0.120),  COAT_DARK),
    ("hat-cone-t3",            "head", (-0.090, 0.725, -0.090), (0.085, 0.760, 0.085),  COAT_DARK),
    ("hat-cone-apex",          "head", (-0.055, 0.760, -0.055), (0.040, 0.785, 0.040),  COAT_DARK),
]


BODY_BOXES = (LEG_LEFT + LEG_RIGHT
              + TORSO
              + ARM_LEFT + ARM_RIGHT)
HEAD_BOXES = HEAD

# ---------------------------------------------------------------------------
# GEOMETRY GENERATION & CHAMFER
# ---------------------------------------------------------------------------
FRONT_IS_MINUS_Z = True

FACES = [
    ((0, 0, -1), [(0, 0, 0), (0, 1, 0), (1, 1, 0), (1, 0, 0)]),
    ((0, 0, 1), [(1, 0, 1), (1, 1, 1), (0, 1, 1), (0, 0, 1)]),
    ((-1, 0, 0), [(0, 0, 1), (0, 1, 1), (0, 1, 0), (0, 0, 0)]),
    ((1, 0, 0), [(1, 0, 0), (1, 1, 0), (1, 1, 1), (1, 0, 1)]),
    ((0, 1, 0), [(0, 1, 0), (0, 1, 1), (1, 1, 1), (1, 1, 0)]),
    ((0, -1, 0), [(0, 0, 1), (0, 0, 0), (1, 0, 0), (1, 0, 1)]),
]

_FACE_NAMES = {"front": 0, "back": 1, "left": 3, "right": 2, "top": 4, "bottom": 5}
SKIPPABLE = dict(_FACE_NAMES)
if FRONT_IS_MINUS_Z:
    SKIPPABLE["front"], SKIPPABLE["back"] = _FACE_NAMES["back"], _FACE_NAMES["front"]

BEVEL_FRACTION = 0.45
BEVEL_MAX = 0.055


def bevel_for(lo, hi):
    smallest = min((hi[i] - lo[i]) * 0.5 for i in range(3))
    if smallest < 0.004:
        return 0.0
    return min(BEVEL_MAX, smallest * BEVEL_FRACTION)


def _ring(points, normal):
    cx = sum(p[0] for p in points) / len(points)
    cy = sum(p[1] for p in points) / len(points)
    cz = sum(p[2] for p in points) / len(points)

    least = min(range(3), key=lambda i: abs(normal[i]))
    helper = [0.0, 0.0, 0.0]
    helper[least] = 1.0

    u = _cross(helper, normal)
    u = _unit(u)
    v = _unit(_cross(normal, u))

    def angle(p):
        d = (p[0] - cx, p[1] - cy, p[2] - cz)
        return math.atan2(_dot(d, v), _dot(d, u))

    return sorted(points, key=angle)


def _cross(a, b):
    return (a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0])


def _dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _unit(a):
    length = math.sqrt(_dot(a, a)) or 1.0
    return (a[0] / length, a[1] / length, a[2] / length)


def signed_area_2d(pts):
    area = 0.0
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        area += pts[i][0] * pts[j][1] - pts[j][0] * pts[i][1]
    return area * 0.5


def box_polygons(lo, hi, skip, bevel):
    if bevel <= 0.0:
        for face, (normal, corners) in enumerate(FACES):
            if face == skip:
                continue
            yield (tuple(float(c) for c in normal),
                   [(lo[0] + (hi[0] - lo[0]) * cx,
                     lo[1] + (hi[1] - lo[1]) * cy,
                     lo[2] + (hi[2] - lo[2]) * cz) for cx, cy, cz in corners])
        return

    centre = [(lo[i] + hi[i]) * 0.5 for i in range(3)]
    half = [(hi[i] - lo[i]) * 0.5 for i in range(3)]
    signs = [(sx, sy, sz) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]

    vertex = {}
    for s in signs:
        for axis in range(3):
            p = [centre[i] + s[i] * half[i] for i in range(3)]
            p[axis] = centre[axis] + s[axis] * (half[axis] - bevel)
            vertex[(s, axis)] = tuple(p)

    for axis in range(3):
        for sgn in (-1, 1):
            normal = [0.0, 0.0, 0.0]
            normal[axis] = float(sgn)
            points = [vertex[(s, other)]
                      for s in signs if s[axis] == sgn
                      for other in range(3) if other != axis]
            yield (tuple(normal), _ring(points, tuple(normal)))

    for a in range(3):
        for b in range(a + 1, 3):
            third = 3 - a - b
            for sa in (-1, 1):
                for sb in (-1, 1):
                    normal = [0.0, 0.0, 0.0]
                    normal[a] = float(sa)
                    normal[b] = float(sb)
                    normal = _unit(tuple(normal))
                    points = []
                    for sc in (-1, 1):
                        s = [0, 0, 0]
                        s[a], s[b], s[third] = sa, sb, sc
                        s = tuple(s)
                        points.append(vertex[(s, a)])
                        points.append(vertex[(s, b)])
                    yield (normal, _ring(points, normal))

    for s in signs:
        normal = _unit((float(s[0]), float(s[1]), float(s[2])))
        yield (normal, _ring([vertex[(s, 0)], vertex[(s, 1)], vertex[(s, 2)]], normal))


PANEL_PROUD = 0.0006


def build_mesh(boxes, panels=(), decals=(), donor=None):
    pos, nrm, uv, joints, weights, idx = [], [], [], [], [], []
    panel_indices = []

    for entry in boxes:
        name, bone, lo, hi, slot = entry[:5]
        skip = SKIPPABLE[entry[5]] if len(entry) > 5 else -1

        if FRONT_IS_MINUS_Z:
            lo, hi = (lo[0], lo[1], -hi[2]), (hi[0], hi[1], -lo[2])

        for axis in range(3):
            if hi[axis] <= lo[axis]:
                raise SystemExit(f"box '{name}' is inside out on axis {axis}")

        j = BONE[bone]
        u, v = cell_uv(slot)
        bevel = 0.0 if skip >= 0 else bevel_for(lo, hi)

        for normal, points in box_polygons(lo, hi, skip, bevel):
            first = len(pos)
            for p in points:
                pos.append(p)
                nrm.append(normal)
                uv.append((u, v))
                joints.append((j, 0, 0, 0))
                weights.append((1.0, 0.0, 0.0, 0.0))

            for k in range(1, len(points) - 1):
                idx += [first, first + k, first + k + 1]

    for name, bone, face, slot, poly, layer in decals:
        j = BONE[bone]
        u, v = cell_uv(slot)
        offset = layer * PANEL_PROUD

        if isinstance(poly, tuple) and len(poly) == 2 and isinstance(poly[0], tuple):
            (x0, y0), (x1, y1) = poly
            pts_2d = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
        else:
            pts_2d = list(poly)

        if face == "front":
            if signed_area_2d(pts_2d) < 0:
                pts_2d.reverse()
            normal = (0.0, 0.0, 1.0)
            z_plane = 0.1141 if bone == "head" else 0.096
            pts_3d = [(p[0], p[1], z_plane + offset) for p in pts_2d]
        elif face == "back":
            if signed_area_2d(pts_2d) > 0:
                pts_2d.reverse()
            normal = (0.0, 0.0, -1.0)
            pts_3d = [(p[0], p[1], -0.096 - offset) for p in pts_2d]
        elif face == "left":
            pts_3d = [(0.156 + offset, p[1], -p[0]) for p in pts_2d]
            if signed_area_2d(pts_2d) < 0:
                pts_3d.reverse()
            normal = (1.0, 0.0, 0.0)
        elif face == "right":
            pts_3d = [(-0.156 - offset, p[1], -p[0]) for p in pts_2d]
            if signed_area_2d(pts_2d) > 0:
                pts_3d.reverse()
            normal = (-1.0, 0.0, 0.0)
        else:
            continue

        base_idx = len(pos)
        for p in pts_3d:
            panel_indices.append(len(pos))
            pos.append(p)
            nrm.append(normal)
            uv.append((u, v))
            joints.append((j, 0, 0, 0))
            weights.append((1.0, 0.0, 0.0, 0.0))

        for k in range(1, len(pts_3d) - 1):
            idx += [base_idx, base_idx + k, base_idx + k + 1]

    if donor is not None:
        dpos, dnrm, duv, dtris = donor
        j = BONE["head"]
        base = len(pos)

        for i in range(len(dpos)):
            panel_indices.append(len(pos))
            pos.append(tuple(dpos[i]))
            nrm.append(tuple(dnrm[i]))
            uv.append(tuple(duv[i]))
            joints.append((j, 0, 0, 0))
            weights.append((1.0, 0.0, 0.0, 0.0))

        for a, b, c in dtris:
            idx += [base + a, base + b, base + c]

    return pos, smooth_normals(pos, nrm, panel_indices), uv, joints, weights, idx


def smooth_normals(pos, nrm, panel_indices=()):
    skip = set(panel_indices or ())
    buckets = {}

    for i, p in enumerate(pos):
        if i in skip:
            continue
        key = (round(p[0], 5), round(p[1], 5), round(p[2], 5))
        acc = buckets.setdefault(key, [0.0, 0.0, 0.0])
        for a in range(3):
            acc[a] += nrm[i][a]

    out = []
    for i, p in enumerate(pos):
        if i in skip:
            out.append(nrm[i])
            continue
        key = (round(p[0], 5), round(p[1], 5), round(p[2], 5))
        n = buckets[key]
        length = math.sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]) or 1.0
        out.append((n[0] / length, n[1] / length, n[2] / length))

    return out


# ---------------------------------------------------------------------------
# GLB EXPORT, RETARGETING & PALETTE WRITER
# ---------------------------------------------------------------------------
def read_glb(path):
    with open(path, "rb") as handle:
        data = handle.read()

    magic, version, _total = struct.unpack_from("<III", data, 0)
    if magic != 0x46546C67:
        raise SystemExit(f"{path} is not a .glb")

    offset = 12
    gltf = None
    buffer = None

    while offset < len(data):
        length, kind = struct.unpack_from("<II", data, offset)
        offset += 8
        chunk = data[offset:offset + length]
        offset += length

        if kind == 0x4E4F534A:
            gltf = json.loads(chunk.decode("utf-8"))
        elif kind == 0x004E4942:
            buffer = chunk

    return gltf, buffer


def read_accessor(gltf, buffer, index):
    acc = gltf["accessors"][index]
    counts = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}
    fmts = {5120: "b", 5121: "B", 5122: "h", 5123: "H", 5125: "I", 5126: "f"}

    n = counts[acc["type"]]
    fmt = fmts[acc["componentType"]]
    size = struct.calcsize("<" + fmt)
    element = size * n

    view = gltf["bufferViews"][acc["bufferView"]]
    start = view.get("byteOffset", 0) + acc.get("byteOffset", 0)
    stride = view.get("byteStride") or element

    out = []
    for i in range(acc["count"]):
        offset = start + i * stride
        vals = struct.unpack_from("<" + fmt * n, buffer, offset)
        out.append(vals[0] if n == 1 else vals)
    return out


def accessor_bytes(gltf, buffer, index):
    acc = gltf["accessors"][index]
    counts = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}
    sizes = {5120: 1, 5121: 1, 5122: 2, 5123: 2, 5125: 4, 5126: 4}

    n = counts[acc["type"]]
    size = sizes[acc["componentType"]]
    element = size * n

    view = gltf["bufferViews"][acc["bufferView"]]
    start = view.get("byteOffset", 0) + acc.get("byteOffset", 0)
    stride = view.get("byteStride") or element

    if stride == element:
        return buffer[start:start + element * acc["count"]]

    out = bytearray()
    for i in range(acc["count"]):
        out += buffer[start + i * stride: start + i * stride + element]
    return bytes(out)


def retarget(gltf, buffer):
    by_name = {node.get("name"): i for i, node in enumerate(gltf["nodes"])}
    deltas = {}

    for bone, world in SKELETON.items():
        index = by_name[bone]
        parent = SKELETON[PARENT[bone]] if bone in PARENT else (0.0, 0.0, 0.0)

        local = tuple(world[a] - parent[a] for a in range(3))
        old = tuple(gltf["nodes"][index].get("translation", [0.0, 0.0, 0.0]))

        gltf["nodes"][index]["translation"] = list(local)
        deltas[index] = tuple(local[a] - old[a] for a in range(3))

    return deltas


def bind_matrices(gltf):
    by_name = {node.get("name"): i for i, node in enumerate(gltf["nodes"])}
    out = {}

    for skin in gltf["skins"]:
        rows = []
        for joint in skin["joints"]:
            name = gltf["nodes"][joint].get("name")
            world = SKELETON[name]
            rows.append((1.0, 0.0, 0.0, 0.0,
                         0.0, 1.0, 0.0, 0.0,
                         0.0, 0.0, 1.0, 0.0,
                         -world[0], -world[1], -world[2], 1.0))
        out[id(skin)] = rows
        skin["_rows"] = rows

    return out


def main():
    if not os.path.exists(BASE):
        raise SystemExit(f"base rig not found: {BASE}")

    gltf, buffer = read_glb(BASE)

    deltas = retarget(gltf, buffer)
    bind_matrices(gltf)

    body = build_mesh(BODY_BOXES)
    head = build_mesh(HEAD_BOXES)

    blob = bytearray()
    new_views = []
    new_accessors = []
    remap = {}

    def align():
        while len(blob) % 4:
            blob.append(0)

    def keep(old_index):
        if old_index in remap:
            return remap[old_index]

        acc = dict(gltf["accessors"][old_index])
        data = accessor_bytes(gltf, buffer, old_index)

        align()
        acc["bufferView"] = len(new_views)
        acc.pop("byteOffset", None)
        new_views.append({"buffer": 0, "byteOffset": len(blob), "byteLength": len(data)})
        blob.extend(data)

        remap[old_index] = len(new_accessors)
        new_accessors.append(acc)
        return remap[old_index]

    def add(values, fmt, kind, component, minmax=False):
        align()
        start = len(blob)

        for v in values:
            blob.extend(struct.pack("<" + fmt * len(v), *v))

        acc = {"bufferView": len(new_views), "componentType": component,
               "count": len(values), "type": kind}

        if minmax:
            n = len(values[0])
            acc["min"] = [min(v[a] for v in values) for a in range(n)]
            acc["max"] = [max(v[a] for v in values) for a in range(n)]

        new_views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        new_accessors.append(acc)
        return len(new_accessors) - 1

    for skin in gltf["skins"]:
        rows = skin.pop("_rows")
        skin["inverseBindMatrices"] = add(rows, "f", "MAT4", 5126)

    moved = 0

    for anim in gltf["animations"]:
        for channel in anim["channels"]:
            sampler = anim["samplers"][channel["sampler"]]
            sampler["input"] = keep(sampler["input"])

            node = channel["target"]["node"]
            delta = deltas.get(node)

            if channel["target"]["path"] != "translation" or delta is None \
                    or delta == (0.0, 0.0, 0.0):
                sampler["output"] = keep(sampler["output"])
                continue

            values = read_accessor(gltf, buffer, sampler["output"])
            shifted = [tuple(v[a] + delta[a] for a in range(3)) for v in values]
            sampler["output"] = add(shifted, "f", "VEC3", 5126)
            moved += 1

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

    gltf["accessors"] = new_accessors
    gltf["bufferViews"] = new_views
    gltf["buffers"] = [{"byteLength": len(blob)}]
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso voxel person builder"}

    stem = os.path.splitext(os.path.basename(OUT))[0]
    gltf["nodes"][0]["name"] = stem
    gltf["scenes"][0]["name"] = stem

    print(f"retargeted {len(deltas)} bones, shifted {moved} translation tracks")

    verify(body, head)
    write_glb(OUT, gltf, blob)
    write_palette(PALETTE_OUT)
    write_unity_asset(ROSTER_OUT)


def write_unity_asset(path):
    if not os.path.exists(path):
        return
    with open(path, "r", encoding="utf-8") as handle:
        content = handle.read()

    pal_lines = ["  Palette:"]
    for slot in range(16):
        r, g, b = rgb(PALETTE[slot])
        pal_lines.append(f"  - {{r: {r:.6f}, g: {g:.6f}, b: {b:.6f}, a: 1}}")

    import re
    new_pal_block = "\n".join(pal_lines)
    content = re.sub(r"  Palette:\n(  - \{r: [^\n]+\n)+", new_pal_block + "\n", content)

    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(content)

    print(f"wrote {path}")


def verify(body, head):
    lo = [min(v[a] for v in body[0] + head[0]) for a in range(3)]
    hi = [max(v[a] for v in body[0] + head[0]) for a in range(3)]
    height = hi[1] - lo[1]

    print(f"boxes: body={len(BODY_BOXES)} head={len(HEAD_BOXES)}")
    print(f"verts: body={len(body[0])} head={len(head[0])}  "
          f"tris: body={len(body[5]) // 3} head={len(head[5]) // 3}")
    print(f"bounds min={[round(v, 4) for v in lo]} max={[round(v, 4) for v in hi]}")

    legs = SKELETON["leg-left"][1]
    neck = SKELETON["head"][1]
    print(f"height={height:.4f}  legs {legs / height:.0%}  "
          f"torso {(neck - legs) / height:.0%}  head {(height - neck) / height:.0%}")

    if not (CAST_MIN_HEIGHT - 0.005 <= height <= CAST_MAX_HEIGHT + 0.005):
        raise SystemExit(
            f"\nHEIGHT CONSTRAINT VIOLATION - nothing written.\n"
            f"  authored height {height:.4f}, the twelve CC0 rigs span "
            f"{CAST_MIN_HEIGHT:.4f} to {CAST_MAX_HEIGHT:.4f}.")

    if abs(lo[1]) > 0.001:
        raise SystemExit(f"feet are at y={lo[1]:.4f}, not 0.")

    head_reach = (CAST_MAX_HEIGHT - NOW_NECK) * 1.15

    for entry in (BODY_BOXES + HEAD_BOXES):
        name, bone, box_lo, box_hi, slot = entry[:5]
        origin = SKELETON[bone][1]
        reach = max(abs(box_lo[1] - origin), abs(box_hi[1] - origin))

        if reach > (head_reach if bone == "head" else 0.35):
            raise SystemExit(f"box '{name}' is {reach:.3f} from the {bone} bone. "
                             "It is almost certainly on the wrong bone.")

        if slot not in PALETTE:
            raise SystemExit(f"box '{name}' uses palette slot {slot}, which is not set.")


def rgb(hex_str):
    h = hex_str.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def write_palette(path):
    r, g, b = rgb(PALETTE[8])
    lum = 0.2126 * r + 0.7152 * g + 0.0722 * b

    if lum > MAX_FACE_LUMINANCE:
        raise SystemExit(
            f"\nFACE CONSTRAINT VIOLATION - nothing written.\n"
            f"  slot 8 is #{PALETTE[8]} (luminance {lum:.2f} > {MAX_FACE_LUMINANCE:.2f}).")

    values = []
    for slot in range(16):
        values += [f"{c:.6f}" for c in rgb(PALETTE[slot])] + ["1"]

    name = os.path.splitext(os.path.basename(path))[0]

    text = (
        '[gd_resource type="ShaderMaterial" load_steps=4 format=3]\n\n'
        '[ext_resource type="Shader" '
        'path="res://assets/characters/persons/materials/person_palette.gdshader" id="1"]\n'
        '[ext_resource type="Texture2D" '
        'path="res://assets/characters/persons/Textures/colormap.png" id="2"]\n'
        '[ext_resource type="Material" '
        'path="res://assets/characters/persons/materials/person_outline.tres" id="3"]\n\n'
        "[resource]\n"
        f'resource_name = "{name}"\n'
        'shader = ExtResource("1")\n'
        'shader_parameter/source_map = ExtResource("2")\n'
        "shader_parameter/albedo_color = Color(1, 1, 1, 0)\n"
        f"shader_parameter/palette = PackedColorArray({', '.join(values)})\n"
        'next_pass = ExtResource("3")\n')

    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)

    print(f"wrote {path}")


def write_glb(path, gltf, blob):
    os.makedirs(os.path.dirname(path), exist_ok=True)

    js = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    js += b" " * ((4 - len(js) % 4) % 4)

    bin_chunk = bytes(blob)
    bin_chunk += b"\0" * ((4 - len(bin_chunk) % 4) % 4)

    total = 12 + 8 + len(js) + 8 + len(bin_chunk)

    with open(path, "wb") as handle:
        handle.write(struct.pack("<III", 0x46546C67, 2, total))
        handle.write(struct.pack("<II", len(js), 0x4E4F534A))
        handle.write(js)
        handle.write(struct.pack("<II", len(bin_chunk), 0x004E4942))
        handle.write(bin_chunk)

    print(f"wrote {path}")


if __name__ == "__main__":
    sys.exit(main())
