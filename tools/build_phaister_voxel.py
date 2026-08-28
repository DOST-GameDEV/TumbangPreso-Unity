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
CAST_MIN_HEIGHT, CAST_MAX_HEIGHT = 0.6613, 1.1500

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
    TEAL_KNOT:      "f4c098",   # Remapped to uniform skin tone (no green/teal!)
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
# Full 1:1 scale (X in [0.006, 0.158])
# ---------------------------------------------------------------------------
# ---------------------------------------------------------------------------
# LEGS & SHOES (Black trousers, crimson ankle band, purple shoes, white sole)
# Full 1:1 chunky cast scale (X in [0.005, 0.160])
# ---------------------------------------------------------------------------
LEG_LEFT = [
    # 1. Crisp White Sneaker Sole Slab (Y in [0.000, 0.024])
    ("shoe-sole-left",         "leg-left", (0.005, 0.000, -0.136), (0.160, 0.024, 0.084), WHITE),

    # 2. Royal Purple Shoe Upper (Y in [0.024, 0.060])
    ("shoe-upper-left",        "leg-left", (0.012, 0.024, -0.128), (0.154, 0.060, 0.078), CLOTH_PURPLE),
    ("shoe-toe-left",          "leg-left", (0.010, 0.024, -0.134), (0.156, 0.048, -0.090), CLOTH_PURPLE),
    ("shoe-heel-left",         "leg-left", (0.010, 0.024, 0.045),  (0.156, 0.060, 0.082), CLOTH_PURPLE),

    # 3. Crimson Red Ankle Stripe (Y in [0.060, 0.074])
    ("shoe-ankle-crimson-l",   "leg-left", (0.012, 0.060, -0.088), (0.154, 0.074, 0.074), CRIMSON),

    # 4. Full-Length Black Trousers (Y in [0.074, 0.176])
    ("pants-cuff-left",        "leg-left", (0.012, 0.074, -0.084), (0.156, 0.104, 0.078), COAT_DARK),
    ("pants-leg-left",         "leg-left", (0.016, 0.104, -0.078), (0.152, 0.176, 0.075), COAT_DARK),
]

LEG_RIGHT = mirrored(LEG_LEFT, "leg-left", "leg-right")


# ---------------------------------------------------------------------------
# TORSO: BROAD FROCK COAT, EXPANDED CAPE, V-CHAIN, MEDALLION, BELT, MOON & STARS
# (Master Reference: media_1787737584126.png - Witch Qualities & Arcane Regalia)
# ---------------------------------------------------------------------------
TORSO = [
    # 1. Black Trousers Pelvis / Hips / Crotch
    ("pants-pelvis",           "torso", (-0.148, 0.160, -0.085), (0.148, 0.185, 0.082), COAT_DARK),
    ("pants-crotch-center",    "torso", (-0.055, 0.085, -0.078), (0.055, 0.160, 0.072), COAT_DARK),

    # 2. Dramatic High Upturned Crimson Witch Collar (Flanking neck & jaw)
    ("collar-crimson-left",    "torso", (0.055,  0.280, -0.098), (0.132, 0.365, -0.070), CRIMSON),
    ("collar-crimson-right",   "torso", (-0.132, 0.280, -0.098), (-0.055, 0.365, -0.070), CRIMSON),
    ("collar-crimson-flare-l", "torso", (0.115,  0.315, -0.095), (0.142, 0.365, -0.065), CRIMSON),
    ("collar-crimson-flare-r", "torso", (-0.142, 0.315, -0.095), (-0.115, 0.365, -0.065), CRIMSON),
    ("collar-crimson-back",    "torso", (-0.125, 0.300, 0.070),  (0.125, 0.365, 0.098),  CRIMSON),

    # 3. Crimson Red Cape INNER LINING (Visible in front & 3/4 views flanking legs)
    ("cape-lining-upper",      "torso", (-0.195, 0.148, 0.068),  (0.195, 0.300, 0.092),  CRIMSON),
    # Left Wing Lining (+X)
    ("cape-lining-wing-l1",    "torso", (0.070,  0.110, 0.072),  (0.140, 0.160, 0.096),  CRIMSON),
    ("cape-lining-wing-l2",    "torso", (0.120,  0.078, 0.076),  (0.185, 0.125, 0.100),  CRIMSON),
    ("cape-lining-wing-l3",    "torso", (0.165,  0.045, 0.080),  (0.235, 0.090, 0.104),  CRIMSON),
    # Right Wing Lining (-X)
    ("cape-lining-wing-r1",    "torso", (-0.140, 0.110, 0.072),  (-0.070, 0.160, 0.096), CRIMSON),
    ("cape-lining-wing-r2",    "torso", (-0.185, 0.078, 0.076),  (-0.120, 0.125, 0.096), CRIMSON),
    ("cape-lining-wing-r3",    "torso", (-0.235, 0.045, 0.080),  (-0.165, 0.090, 0.104), CRIMSON),
    # Side Cape Flaps (visible in side view connecting cape to upper torso)
    ("cape-side-l",            "torso", (0.175,  0.135, 0.035),  (0.220, 0.285, 0.088),  CRIMSON),
    ("cape-side-r",            "torso", (-0.220, 0.135, 0.035),  (-0.175, 0.285, 0.088), CRIMSON),

    # 4. Main Coat Body Core & Under-tunic
    ("coat-chest-core",        "torso", (-0.148, 0.200, -0.088), (0.148, 0.343, 0.082), COAT_DARK),
    ("coat-side-l",            "torso", (0.100,  0.180, -0.088), (0.152, 0.343, 0.084), COAT_DARK),
    ("coat-side-r",            "torso", (-0.152, 0.180, -0.088), (-0.100, 0.343, 0.084), COAT_DARK),

    # Deep V-Neck Peach Skin Chest Opening (Clean skin, no teal knot!)
    ("chest-skin-v",           "torso", (-0.048, 0.280, -0.092), (0.048, 0.343, -0.080), SKIN),

    # 5. Front Coat Peplum & Diagonal Split with Royal Purple Trim (Hang BELOW Belt, Y <= 0.160)
    # Front Left Skirt Body (+X)
    ("skirt-front-l",          "torso", (0.020,  0.085, -0.096), (0.175, 0.160, -0.078), COAT_DARK),
    ("skirt-front-trim-bot-l", "torso", (0.020,  0.070, -0.098), (0.180, 0.088, -0.076), CLOTH_PURPLE),
    ("skirt-front-trim-diag-l","torso", (0.012,  0.085, -0.098), (0.032, 0.160, -0.076), CLOTH_PURPLE),

    # Front Right Skirt Body (-X)
    ("skirt-front-r",          "torso", (-0.175, 0.085, -0.096), (-0.020, 0.160, -0.078), COAT_DARK),
    ("skirt-front-trim-bot-r", "torso", (-0.180, 0.070, -0.098), (-0.020, 0.088, -0.076), CLOTH_PURPLE),
    ("skirt-front-trim-diag-r","torso", (-0.032, 0.085, -0.098), (-0.012, 0.160, -0.076), CLOTH_PURPLE),

    # Side Hip Flared Skirt Panels
    ("skirt-side-l",           "torso", (0.138,  0.085, -0.088), (0.188, 0.160, 0.050),  COAT_DARK),
    ("skirt-side-r",           "torso", (-0.188, 0.085, -0.088), (-0.138, 0.160, 0.050), COAT_DARK),
    ("skirt-side-trim-l",      "torso", (0.138,  0.070, -0.090), (0.192, 0.088, 0.052),  CLOTH_PURPLE),
    ("skirt-side-trim-r",      "torso", (-0.192, 0.070, -0.090), (-0.138, 0.088, 0.052), CLOTH_PURPLE),

    # Front-Side Diagonal Corners
    ("skirt-corner-fl",        "torso", (0.118,  0.085, -0.098), (0.182, 0.160, -0.058), COAT_DARK),
    ("skirt-corner-fr",        "torso", (-0.182, 0.085, -0.098), (-0.118, 0.160, -0.058), COAT_DARK),
    ("skirt-corner-trim-fl",   "torso", (0.118,  0.070, -0.100), (0.186, 0.088, -0.058), CLOTH_PURPLE),
    ("skirt-corner-trim-fr",   "torso", (-0.186, 0.070, -0.100), (-0.118, 0.088, -0.058), CLOTH_PURPLE),

    # 6. Outermost Black Cape / Swallowtail Back with Stepped Inverted-V Chevron (PERFECT - PRESERVED)
    ("coat-back-main-upper",   "torso", (-0.195, 0.220, 0.086),  (0.195, 0.340, 0.115),  COAT_DARK),
    ("coat-back-main-mid",     "torso", (-0.205, 0.148, 0.094),  (0.205, 0.220, 0.122),  COAT_DARK),
    # Left Swallowtail Stepped Body (+X)
    ("coat-back-tail-l-seg1",  "torso", (0.020,  0.138, 0.096),  (0.065, 0.220, 0.125),  COAT_DARK),
    ("coat-back-tail-l-seg2",  "torso", (0.062,  0.118, 0.098),  (0.112, 0.220, 0.128),  COAT_DARK),
    ("coat-back-tail-l-seg3",  "torso", (0.108,  0.094, 0.100),  (0.160, 0.220, 0.132),  COAT_DARK),
    ("coat-back-tail-l-seg4",  "torso", (0.155,  0.070, 0.102),  (0.205, 0.220, 0.136),  COAT_DARK),
    ("coat-back-tail-l-seg5",  "torso", (0.185,  0.045, 0.105),  (0.235, 0.220, 0.140),  COAT_DARK),
    # Right Swallowtail Stepped Body (-X)
    ("coat-back-tail-r-seg1",  "torso", (-0.065, 0.138, 0.096),  (-0.020, 0.220, 0.125), COAT_DARK),
    ("coat-back-tail-r-seg2",  "torso", (-0.112, 0.118, 0.098),  (-0.062, 0.220, 0.128), COAT_DARK),
    ("coat-back-tail-r-seg3",  "torso", (-0.160, 0.094, 0.100),  (-0.108, 0.220, 0.132), COAT_DARK),
    ("coat-back-tail-r-seg4",  "torso", (-0.205, 0.070, 0.102),  (-0.155, 0.220, 0.136), COAT_DARK),
    ("coat-back-tail-r-seg5",  "torso", (-0.235, 0.045, 0.105),  (-0.185, 0.220, 0.140), COAT_DARK),

    # Continuous Royal Purple Chevron Trim Band (/ \)
    ("coat-back-trim-apex",    "torso", (-0.025, 0.138, 0.108),  (0.025, 0.148, 0.128),  CLOTH_PURPLE),
    ("coat-back-trim-l-s1",    "torso", (0.020,  0.122, 0.110),  (0.065, 0.140, 0.130),  CLOTH_PURPLE),
    ("coat-back-trim-l-s2",    "torso", (0.062,  0.102, 0.112),  (0.112, 0.124, 0.132),  CLOTH_PURPLE),
    ("coat-back-trim-l-s3",    "torso", (0.108,  0.078, 0.114),  (0.160, 0.104, 0.136),  CLOTH_PURPLE),
    ("coat-back-trim-l-s4",    "torso", (0.155,  0.054, 0.116),  (0.205, 0.080, 0.140),  CLOTH_PURPLE),
    ("coat-back-trim-l-tip",   "torso", (0.185,  0.035, 0.118),  (0.235, 0.056, 0.144),  CLOTH_PURPLE),
    ("coat-back-trim-r-s1",    "torso", (-0.065, 0.122, 0.110),  (-0.020, 0.140, 0.130), CLOTH_PURPLE),
    ("coat-back-trim-r-s2",    "torso", (-0.112, 0.102, 0.112),  (-0.062, 0.124, 0.132), CLOTH_PURPLE),
    ("coat-back-trim-r-s3",    "torso", (-0.160, 0.078, 0.114),  (-0.108, 0.104, 0.136), CLOTH_PURPLE),
    ("coat-back-trim-r-s4",    "torso", (-0.205, 0.054, 0.116),  (-0.155, 0.080, 0.140), CLOTH_PURPLE),
    ("coat-back-trim-r-tip",   "torso", (-0.235, 0.035, 0.118),  (-0.185, 0.056, 0.144), CLOTH_PURPLE),

    # 7. Royal Purple Waist Belt & Large Square Gold Buckle (Worn OVER the coat & robe!)
    ("waist-belt-purple",      "torso", (-0.155, 0.160, -0.108), (0.155, 0.208, 0.092), CLOTH_PURPLE),
    ("waist-buckle-gold",      "torso", (-0.052, 0.154, -0.120), (0.052, 0.214, -0.096), GOLD),
    ("waist-buckle-slot",      "torso", (-0.026, 0.166, -0.122), (0.026, 0.202, -0.098), COAT_DARK),
    ("waist-buckle-prong",     "torso", (-0.026, 0.180, -0.125), (0.018, 0.190, -0.100), GOLD),

    # 8. Scalloped Gold Arcane V-Chain Necklace & Faceted Amethyst Talisman (Elevated with Clear Gap)
    # Scalloped Gold V-Chain Links (Double-tiered drape)
    ("chain-v-link-l1",        "torso", (0.075,  0.305, -0.100), (0.125, 0.335, -0.082), GOLD),
    ("chain-v-link-l2",        "torso", (0.042,  0.280, -0.104), (0.085, 0.310, -0.085), GOLD),
    ("chain-v-link-l3",        "torso", (0.018,  0.258, -0.108), (0.048, 0.285, -0.088), GOLD),
    ("chain-v-link-r1",        "torso", (-0.125, 0.305, -0.100), (-0.075, 0.335, -0.082), GOLD),
    ("chain-v-link-r2",        "torso", (-0.085, 0.280, -0.104), (-0.042, 0.310, -0.085), GOLD),
    ("chain-v-link-r3",        "torso", (-0.048, 0.258, -0.108), (-0.018, 0.285, -0.088), GOLD),

    # Faceted Amethyst Witch Medallion with Gold Setting & Drop Charm (100% NON-OVERLAPPING with belt!)
    ("pendant-gold-frame",     "torso", (-0.028, 0.238, -0.112), (0.028, 0.278, -0.090), GOLD),
    ("pendant-gold-corner-tl", "torso", (-0.032, 0.264, -0.114), (-0.024, 0.282, -0.092), GOLD),
    ("pendant-gold-corner-tr", "torso", (0.024,  0.264, -0.114), (0.032, 0.282, -0.092), GOLD),
    ("pendant-gem-core",       "torso", (-0.022, 0.242, -0.116), (0.022, 0.274, -0.090), LILAC_GEM),
    ("pendant-gem-top",        "torso", (-0.015, 0.270, -0.114), (0.015, 0.280, -0.090), LILAC_GEM),
    ("pendant-gold-prong-bot", "torso", (-0.012, 0.232, -0.113), (0.012, 0.240, -0.092), GOLD),
    ("pendant-gold-charm-tip", "torso", (-0.006, 0.226, -0.114), (0.006, 0.234, -0.094), GOLD),
    ("pendant-gem-highlight",  "torso", (-0.015, 0.258, -0.118), (-0.005, 0.270, -0.092), WHITE),

    # 9. Back Coat Graphic: Large Gold Crescent Moon + 2 Stars (PERFECT - PRESERVED)
    ("back-moon-spine",        "torso", (-0.024, 0.190, 0.120),  (0.014, 0.262, 0.130),  GOLD),
    ("back-moon-top",          "torso", (0.012,  0.238, 0.120),  (0.038, 0.262, 0.130),  GOLD),
    ("back-moon-bot",          "torso", (0.012,  0.190, 0.120),  (0.038, 0.214, 0.130),  GOLD),
    ("back-moon-cut",          "torso", (0.010,  0.206, 0.122),  (0.040, 0.246, 0.132),  COAT_DARK),
    ("back-star-l-vert",       "torso", (-0.120, 0.195, 0.120),  (-0.098, 0.228, 0.130), GOLD),
    ("back-star-l-horiz",      "torso", (-0.136, 0.204, 0.120),  (-0.082, 0.219, 0.130), GOLD),
    ("back-star-r-vert",       "torso", (0.098,  0.195, 0.120),  (0.120, 0.228, 0.130),  GOLD),
    ("back-star-r-horiz",      "torso", (0.082,  0.204, 0.120),  (0.136, 0.219, 0.130),  GOLD),
]


# ---------------------------------------------------------------------------
# ARMS: FULL CHUNKY CAST SCALE WITH GOLD CROSS & STAR SLEEVE EMBLEMS
# (Master Reference: media_1787737584126.png)
# ---------------------------------------------------------------------------
ARM_LEFT = [
    # 1. Black Coat Upper Sleeve (X in [0.0999, 0.220])
    ("sleeve-upper-l",         "arm-left", (0.0999, 0.208, -0.074), (0.220, 0.364, 0.090), COAT_DARK),

    # 2. Royal Purple Forearm Band (X in [0.220, 0.265])
    ("sleeve-band-l",          "arm-left", (0.220,  0.204, -0.076), (0.265, 0.366, 0.092), CLOTH_PURPLE),

    # 3. Gold Emblems on Forearm Band - Signature Cross on Outer Lateral Side
    # Front-facing cross representation
    ("sleeve-gold-cross-v-l",  "arm-left", (0.236, 0.245, -0.082),  (0.250, 0.325, -0.065), GOLD),
    ("sleeve-gold-cross-h-l",  "arm-left", (0.228, 0.272, -0.082),  (0.258, 0.298, -0.065), GOLD),
    # Lateral side-facing cross representation (3/4 and side views)
    ("sleeve-gold-cross-side-v-l", "arm-left", (0.264, 0.245, -0.025), (0.272, 0.325, 0.025), GOLD),
    ("sleeve-gold-cross-side-h-l", "arm-left", (0.264, 0.272, -0.045), (0.272, 0.298, 0.045), GOLD),

    # 4. Gold Lower Rim Band beneath Purple Band (X in [0.265, 0.276])
    ("sleeve-gold-rim-l",      "arm-left", (0.265, 0.204, -0.078),  (0.276, 0.366, 0.094), GOLD),

    # 5. Crisp White Shirt Cuff (X in [0.276, 0.312])
    ("sleeve-white-cuff-l",    "arm-left", (0.276, 0.202, -0.080),  (0.312, 0.368, 0.096), WHITE),

    # 6. Crimson Under-Cuff Peeking along inner hem (X in [0.276, 0.312])
    ("sleeve-crimson-under-l", "arm-left", (0.276, 0.198, -0.076),  (0.308, 0.208, 0.092), CRIMSON),

    # 7. Warm Peach Skin Hand (X in [0.312, 0.3836]) - EXACT Kenney palm/anchor span
    ("hand-left",              "arm-left", (0.312, 0.222, -0.042),  (0.3836, 0.354, 0.042), SKIN),
]

ARM_RIGHT = mirrored(ARM_LEFT, "arm-left", "arm-right")


# ---------------------------------------------------------------------------
# HEAD: HAND-SCULPTED VOXEL HAIR & WITCH HAT (Attached over Donor Head Mesh)
# (Master Reference: media_1787735121595.png)
# ---------------------------------------------------------------------------
HEAD = [
    # -----------------------------------------------------------------------
    # 1. Hair Crown & Internal Cap (Full rear cranium encapsulation)
    # -----------------------------------------------------------------------
    ("hair-skull-crown",       "head", (-0.225, 0.540, -0.155), (0.225, 0.665, 0.165), HAIR_MAGENTA),

    # -----------------------------------------------------------------------
    # 2. Front Bangs & Dipping Bang (Frames the canonical donor face plate)
    # -----------------------------------------------------------------------
    # Brow base beneath hat brim
    ("hair-bang-brow-base",    "head", (-0.150, 0.535, -0.174), (0.150, 0.645, -0.135), HAIR_MAGENTA),

    # Far Left Cheek Lock (-X, X <= -0.135)
    ("hair-bang-far-l-upper",  "head", (-0.175, 0.485, -0.178), (-0.135, 0.635, -0.135), HAIR_MAGENTA),
    ("hair-bang-far-l-mid",    "head", (-0.170, 0.425, -0.176), (-0.140, 0.495, -0.140), HAIR_MAGENTA),
    ("hair-bang-far-l-tip",    "head", (-0.165, 0.385, -0.174), (-0.145, 0.435, -0.145), HAIR_MAGENTA),

    # Left Brow Step (-X) - Stepping down to Y=0.515
    ("hair-bang-mid-l-step",   "head", (-0.135, 0.515, -0.176), (-0.070, 0.575, -0.135), HAIR_MAGENTA),

    # Center-Right Dipping Notch Lock (Between eyes, terminates cleanly at Y=0.475)
    ("hair-bang-notch-t1",     "head", (-0.010, 0.515, -0.186), (0.045,  0.590, -0.135), HAIR_MAGENTA),
    ("hair-bang-notch-tip",    "head", (0.005,  0.475, -0.180), (0.038,  0.520, -0.145), HAIR_MAGENTA),

    # Right Brow Step (+X) - Stepping down to Y=0.515
    ("hair-bang-mid-r-step",   "head", (0.070,  0.515, -0.176), (0.135,  0.575, -0.135), HAIR_MAGENTA),

    # Far Right Cheek Lock (+X, X >= 0.135)
    ("hair-bang-far-r-upper",  "head", (0.135,  0.485, -0.178), (0.175,  0.635, -0.135), HAIR_MAGENTA),
    ("hair-bang-far-r-mid",    "head", (0.140,  0.425, -0.176), (0.170,  0.495, -0.140), HAIR_MAGENTA),
    ("hair-bang-far-r-tip",    "head", (0.145,  0.385, -0.174), (0.165,  0.435, -0.145), HAIR_MAGENTA),

    # -----------------------------------------------------------------------
    # 3. Articulated Side Hair Lock Columns (Left Side +X, Chunky Layered Wave)
    # -----------------------------------------------------------------------
    # Strand L1 (Cheek Flange, Z in [-0.160, -0.065])
    ("hair-side-l1-upper",     "head", (0.155,  0.465, -0.160), (0.230,  0.635, -0.065), HAIR_MAGENTA),
    ("hair-side-l1-mid",       "head", (0.160,  0.405, -0.155), (0.225,  0.475, -0.070), HAIR_MAGENTA),
    ("hair-side-l1-tip",       "head", (0.165,  0.360, -0.150), (0.218,  0.415, -0.075), HAIR_MAGENTA),

    # Strand L2 (Mid-Ear Outward Wave, Z in [-0.065, 0.040] - Proud Protrusion)
    ("hair-side-l2-upper",     "head", (0.165,  0.465, -0.065), (0.240,  0.640, 0.040),  HAIR_MAGENTA),
    ("hair-side-l2-mid",       "head", (0.170,  0.385, -0.060), (0.235,  0.475, 0.035),  HAIR_MAGENTA),
    ("hair-side-l2-low",       "head", (0.175,  0.330, -0.055), (0.230,  0.395, 0.030),  HAIR_MAGENTA),
    ("hair-side-l2-tip",       "head", (0.180,  0.295, -0.050), (0.225,  0.340, 0.025),  HAIR_MAGENTA),

    # Strand L3 (Rear Corner Flange, Z in [0.040, 0.145])
    ("hair-side-l3-upper",     "head", (0.155,  0.465, 0.040),   (0.232,  0.635, 0.145),  HAIR_MAGENTA),
    ("hair-side-l3-mid",       "head", (0.160,  0.395, 0.045),   (0.226,  0.475, 0.140),  HAIR_MAGENTA),
    ("hair-side-l3-tip",       "head", (0.165,  0.345, 0.050),   (0.220,  0.405, 0.135),  HAIR_MAGENTA),

    # -----------------------------------------------------------------------
    # 4. Articulated Side Hair Lock Columns (Right Side -X, Chunky Layered Wave)
    # -----------------------------------------------------------------------
    # Strand R1 (Cheek Flange, Z in [-0.160, -0.065])
    ("hair-side-r1-upper",     "head", (-0.230, 0.465, -0.160), (-0.155, 0.635, -0.065), HAIR_MAGENTA),
    ("hair-side-r1-mid",       "head", (-0.225, 0.405, -0.155), (-0.160, 0.475, -0.070), HAIR_MAGENTA),
    ("hair-side-r1-tip",       "head", (-0.218, 0.360, -0.150), (-0.165, 0.415, -0.075), HAIR_MAGENTA),

    # Strand R2 (Mid-Ear Outward Wave, Z in [-0.065, 0.040] - Proud Protrusion)
    ("hair-side-r2-upper",     "head", (-0.240, 0.465, -0.065), (-0.165, 0.640, 0.040),  HAIR_MAGENTA),
    ("hair-side-r2-mid",       "head", (-0.235, 0.385, -0.060), (-0.170, 0.475, 0.035),  HAIR_MAGENTA),
    ("hair-side-r2-low",       "head", (-0.230, 0.330, -0.055), (-0.175, 0.395, 0.030),  HAIR_MAGENTA),
    ("hair-side-r2-tip",       "head", (-0.225, 0.295, -0.050), (-0.180, 0.340, 0.025),  HAIR_MAGENTA),

    # Strand R3 (Rear Corner Flange, Z in [0.040, 0.145])
    ("hair-side-r3-upper",     "head", (-0.232, 0.465, 0.040),  (-0.155, 0.635, 0.145),  HAIR_MAGENTA),
    ("hair-side-r3-mid",       "head", (-0.226, 0.395, 0.045),  (-0.160, 0.475, 0.140),  HAIR_MAGENTA),
    ("hair-side-r3-tip",       "head", (-0.220, 0.345, 0.050),  (-0.165, 0.405, 0.135),  HAIR_MAGENTA),

    # -----------------------------------------------------------------------
    # 5. Wide Volumetric Back Bob with 5 V-Tiered Locks & Full Gap Fill (No Bald Spots!)
    # -----------------------------------------------------------------------
    # Foundation Back Dome Cap & Continuous Under-Plate (100% encapsulates skull skin!)
    ("hair-back-foundation",   "head", (-0.205, 0.485, 0.115),  (0.205, 0.665, 0.165),  HAIR_MAGENTA),
    ("hair-back-upper-terrace","head", (-0.195, 0.435, 0.125),  (0.195, 0.540, 0.172),  HAIR_MAGENTA),
    ("hair-back-under-plate",  "head", (-0.190, 0.335, 0.110),  (0.190, 0.450, 0.172),  HAIR_MAGENTA),

    # Inter-lock Webbing Plates (Fills empty notches between stepped locks without making them longer)
    ("hair-web-1-2",           "head", (-0.165, 0.355, 0.120),  (-0.110, 0.440, 0.174), HAIR_MAGENTA),
    ("hair-web-2-3",           "head", (-0.090, 0.315, 0.128),  (-0.035, 0.430, 0.176), HAIR_MAGENTA),
    ("hair-web-3-4",           "head", (0.035,  0.315, 0.128),  (0.090,  0.430, 0.176), HAIR_MAGENTA),
    ("hair-web-4-5",           "head", (0.110,  0.355, 0.120),  (0.165,  0.440, 0.174), HAIR_MAGENTA),

    # Lock 1 (Far Left Lock -X, chunky block ending high at Y=0.385)
    ("hair-lock1-upper",       "head", (-0.210, 0.435, 0.125),  (-0.135, 0.530, 0.175), HAIR_MAGENTA),
    ("hair-lock1-tip",         "head", (-0.200, 0.385, 0.130),  (-0.145, 0.445, 0.170), HAIR_MAGENTA),

    # Lock 2 (Mid Left Lock -X, chunky stepped block ending at Y=0.325)
    ("hair-lock2-upper",       "head", (-0.135, 0.415, 0.130),  (-0.060, 0.510, 0.182), HAIR_MAGENTA),
    ("hair-lock2-mid",         "head", (-0.128, 0.355, 0.135),  (-0.068, 0.425, 0.178), HAIR_MAGENTA),
    ("hair-lock2-tip",         "head", (-0.120, 0.325, 0.138),  (-0.075, 0.365, 0.174), HAIR_MAGENTA),

    # Lock 3 (Central Spine V-Tail Lock - Thick Center Lock ending at Y=0.285 above moon)
    ("hair-lock3-upper",       "head", (-0.060, 0.425, 0.135),  (0.060,  0.525, 0.188), HAIR_MAGENTA),
    ("hair-lock3-mid",         "head", (-0.052, 0.355, 0.138),  (0.052,  0.435, 0.192), HAIR_MAGENTA),
    ("hair-lock3-low",         "head", (-0.042, 0.310, 0.142),  (0.042,  0.365, 0.188), HAIR_MAGENTA),
    ("hair-lock3-tip",         "head", (-0.028, 0.285, 0.145),  (0.028,  0.320, 0.184), HAIR_MAGENTA),

    # Lock 4 (Mid Right Lock +X, chunky stepped block ending at Y=0.325)
    ("hair-lock4-upper",       "head", (0.060,  0.415, 0.130),  (0.135,  0.510, 0.182), HAIR_MAGENTA),
    ("hair-lock4-mid",         "head", (0.068,  0.355, 0.135),  (0.128,  0.425, 0.178), HAIR_MAGENTA),
    ("hair-lock4-tip",         "head", (0.075,  0.325, 0.138),  (0.120,  0.365, 0.174), HAIR_MAGENTA),

    # Lock 5 (Far Right Lock +X, chunky block ending high at Y=0.385)
    ("hair-lock5-upper",       "head", (0.135,  0.435, 0.125),  (0.210,  0.530, 0.175), HAIR_MAGENTA),
    ("hair-lock5-tip",         "head", (0.145,  0.385, 0.130),  (0.200,  0.445, 0.170), HAIR_MAGENTA),

    # -----------------------------------------------------------------------
    # 6. Snug Pointed Witch Hat with Large Gold Buckle & 2 Wands (Y: 0.640 -> 1.000)
    # -----------------------------------------------------------------------
    # Wide Octagonal Hat Brim (Y in [0.640, 0.680])
    ("hat-brim-core",          "head", (-0.330, 0.640, -0.250), (0.330, 0.680, 0.250),  COAT_DARK),
    ("hat-brim-fb",            "head", (-0.250, 0.640, -0.330), (0.250, 0.680, 0.330),  COAT_DARK),
    ("hat-brim-c-fl",          "head", (0.190,  0.640, -0.300), (0.300, 0.680, -0.190), COAT_DARK),
    ("hat-brim-c-fr",          "head", (-0.300, 0.640, -0.300), (-0.190, 0.680, -0.190), COAT_DARK),
    ("hat-brim-c-bl",          "head", (0.190,  0.640, 0.190),  (0.300, 0.680, 0.300),  COAT_DARK),
    ("hat-brim-c-br",          "head", (-0.300, 0.640, 0.190),  (-0.190, 0.680, 0.300), COAT_DARK),

    # Royal Purple Hat Ribbon Band (Y in [0.680, 0.755])
    ("hat-band-core",          "head", (-0.225, 0.680, -0.225), (0.225, 0.755, 0.225),  CLOTH_PURPLE),
    ("hat-band-front",         "head", (-0.215, 0.680, -0.242), (0.215, 0.755, -0.215), CLOTH_PURPLE),
    ("hat-band-back",          "head", (-0.215, 0.680, 0.215),  (0.215, 0.755, 0.242),  CLOTH_PURPLE),
    ("hat-band-left",          "head", (0.215,  0.680, -0.215), (0.242, 0.755, 0.215),  CLOTH_PURPLE),
    ("hat-band-right",         "head", (-0.242, 0.680, -0.215), (-0.215, 0.755, 0.215), CLOTH_PURPLE),

    # Large Square Gold Hat Buckle with Hollow Slot & Horizontal Prong (Center Front)
    ("hat-buckle-frame",       "head", (-0.062, 0.685, -0.254), (0.062, 0.750, -0.234), GOLD),
    ("hat-buckle-slot",        "head", (-0.035, 0.698, -0.257), (0.035, 0.738, -0.236), COAT_DARK),
    ("hat-buckle-prong",       "head", (-0.035, 0.712, -0.260), (0.022, 0.726, -0.238), GOLD),

    # 2 Tucked Wands on Left Side (+X, character's left)
    # Forward Wand
    ("wand1-wood-lo",          "head", (0.215,  0.690, -0.100), (0.248, 0.770, -0.068), WAND_WOOD),
    ("wand1-wood-hi",          "head", (0.222,  0.770, -0.114), (0.255, 0.880, -0.078), WAND_WOOD),
    ("wand1-wrap",             "head", (0.218,  0.720, -0.105), (0.250, 0.745, -0.072), WAND_BAND),
    ("wand1-gem-base",         "head", (0.228,  0.880, -0.124), (0.268, 0.935, -0.082), LILAC_GEM),
    ("wand1-gem-tip",          "head", (0.232,  0.935, -0.120), (0.262, 0.955, -0.086), LILAC_GEM),

    # Rear Wand
    ("wand2-wood-lo",          "head", (0.215,  0.690, 0.022),  (0.248, 0.770, 0.054),  WAND_WOOD),
    ("wand2-wood-hi",          "head", (0.225,  0.770, 0.035),  (0.258, 0.855, 0.066),  WAND_WOOD),
    ("wand2-wrap",             "head", (0.220,  0.725, 0.028),  (0.252, 0.750, 0.058),  WAND_BAND),
    ("wand2-gem-base",         "head", (0.232,  0.855, 0.040),  (0.270, 0.910, 0.080),  LILAC_GEM),
    ("wand2-gem-tip",          "head", (0.236,  0.910, 0.045),  (0.266, 0.930, 0.074),  LILAC_GEM),

    # Stepped Cone Tiers (Swept back towards apex)
    ("hat-cone-t1",            "head", (-0.185, 0.755, -0.185), (0.185, 0.820, 0.185),  COAT_DARK),
    ("hat-cone-t2",            "head", (-0.150, 0.820, -0.120), (0.150, 0.880, 0.180),  COAT_DARK),
    ("hat-cone-t3",            "head", (-0.110, 0.880, -0.060), (0.110, 0.935, 0.165),  COAT_DARK),
    ("hat-cone-t4",            "head", (-0.065, 0.935, 0.010),  (0.065, 0.975, 0.130),  COAT_DARK),
    ("hat-cone-tip",           "head", (-0.028, 0.975, 0.040),  (0.028, 1.000, 0.105),  COAT_DARK),
]

DONOR_SKULL = "Assets/TumbangPreso/Art/characters/persons/character-female-b.glb"
SKULL_SLOTS = {15: SKIN, 8: INK}

EYE_SQUASH = 0.65
EYE_DROP = 0.005

MOUTH_Z = 0.1596
MOUTH_HALF = 0.030
MOUTH_BASE = 0.4120
MOUTH_RISE = 0.004
MOUTH_THIN = 0.0030
MOUTH_THICK = 0.0055
MOUTH_HOOK_FROM = 0.60
MOUTH_HOOK = 0.003
MOUTH_STEPS = 12


def _mouth_polygon():
    """Sleek, nonchalant, mysterious mouth on the face plate."""
    upper, lower = [], []
    for k in range(MOUTH_STEPS + 1):
        t = k / MOUTH_STEPS
        x = -MOUTH_HALF + t * (2.0 * MOUTH_HALF)

        centre = MOUTH_BASE + (t - 0.5) * MOUTH_RISE
        half = 0.5 * (MOUTH_THIN + t * (MOUTH_THICK - MOUTH_THIN))

        if t > MOUTH_HOOK_FROM:
            u = (t - MOUTH_HOOK_FROM) / (1.0 - MOUTH_HOOK_FROM)
            centre += MOUTH_HOOK * u * u

        upper.append((x, centre + half))
        lower.append((x, centre - half))

    return upper + list(reversed(lower))


def _slot_at(u, v):
    col = min(int(u * 16.0), 15)
    row = min(int(v * 16.0), 15)
    if row < 8:
        return None
    return (col // 2) + (8 if row >= 12 else 0)


def _donor_part(path, slots):
    gltf, buffer = read_glb(path)
    for node in gltf["nodes"]:
        if node.get("name") != "head-mesh":
            continue

        prim = gltf["meshes"][node["mesh"]]["primitives"][0]
        src_pos = [tuple(p) for p in read_accessor(gltf, buffer, prim["attributes"]["POSITION"])]
        src_nrm = [tuple(n) for n in read_accessor(gltf, buffer, prim["attributes"]["NORMAL"])]
        src_uv = [tuple(t) for t in read_accessor(gltf, buffer, prim["attributes"]["TEXCOORD_0"])]

        raw = read_accessor(gltf, buffer, prim["indices"])
        idx = [v[0] for v in raw] if isinstance(raw[0], tuple) else list(raw)

        pos, nrm, uv, tris = [], [], [], []
        remap = {}

        for t in range(0, len(idx), 3):
            tri = (idx[t], idx[t + 1], idx[t + 2])
            slot = _slot_at(*src_uv[tri[0]])
            if slot not in slots:
                continue

            paint = slots[slot]
            for i in tri:
                if i not in remap:
                    remap[i] = len(pos)
                    pos.append(src_pos[i])
                    nrm.append(src_nrm[i])
                    uv.append(cell_uv(paint) if paint is not None else src_uv[i])

            tris.append(tuple(remap[i] for i in tri))

        if not tris:
            raise SystemExit(f"{path} has no triangles in slots {sorted(slots)}")

        return pos, nrm, uv, tris

    raise SystemExit(f"{path} has no head-mesh")


def _donor_head():
    """Builds the canonical donor skull with nonchalant/mysterious half-lidded eyes and sleek mouth."""
    pos, nrm, uv, tris = _donor_part(DONOR_SKULL, SKULL_SLOTS)

    mouth, eyes, mouth_tris = set(), set(), set()
    for a, b, c in tris:
        if _slot_at(*uv[a]) != INK:
            continue
        if (pos[a][1] + pos[b][1] + pos[c][1]) / 3.0 < 0.45:
            mouth.update((a, b, c))
            mouth_tris.add((a, b, c))
        else:
            eyes.add((a, b, c))

    # Half-lid both eyes for the nonchalant / mysterious anime gaze
    for side in (1.0, -1.0):
        lid = {i for tri in eyes for i in tri if pos[i][0] * side > 0.0}
        if not lid:
            continue
        centre = sum(pos[i][1] for i in lid) / len(lid)
        for i in lid:
            x, y, z = pos[i]
            pos[i] = (x, centre + (y - centre) * EYE_SQUASH - EYE_DROP, z)

    # Replace donor mouth with sleek mysterious mouth line
    tris = [t for t in tris if t not in mouth_tris]
    first = len(pos)
    plate = MOUTH_Z + PANEL_PROUD

    poly = _mouth_polygon()
    for x, y in poly:
        pos.append((x, y, plate))
        nrm.append((0.0, 0.0, 1.0))
        uv.append(cell_uv(INK))

    n = len(poly)
    for k in range(1, n - 1):
        tris.append((first, first + k, first + k + 1))

    return pos, nrm, uv, tris


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
    head = build_mesh(HEAD_BOXES, donor=_donor_head())

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
