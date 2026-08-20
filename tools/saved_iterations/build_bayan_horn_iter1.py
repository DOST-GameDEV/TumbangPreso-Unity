"""Builds team-bayan.glb (the Earth / Rock Warrior) and person_team-bayan.tres.

    python tools/build_bayan_voxel.py

Reference: Kawaki Karma Form / Concept art media_1787164289261.png & media_1787164220119.png
"""
import math
import json
import os
import struct
import sys

BASE = "Assets/TumbangPreso/Art/characters/persons/character-male-f.glb"

OUT = "Assets/TumbangPreso/Art/characters/persons/team-bayan.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-bayan.tres"
ROSTER_OUT = "Assets/TumbangPreso/Resources/Roster/person_bayan.asset"

BONE = {"root": 0, "leg-left": 1, "leg-right": 2, "torso": 3,
        "arm-left": 4, "arm-right": 5, "head": 6}

PARENT = {"leg-left": "root", "leg-right": "root", "torso": "root",
          "arm-left": "torso", "arm-right": "torso", "head": "torso"}

SKELETON = {
    "root":      (0.0,      0.0,   0.0),
    "leg-left":  (0.08357,  0.176, -0.02875),
    "leg-right": (-0.08357, 0.176, -0.02875),
    "torso":     (0.0,      0.176, -0.02875),
    "arm-left":  (0.0999,   0.288, -0.01725),
    "arm-right": (-0.0999,  0.288, -0.01725),
    "head":      (0.0,      0.343, -0.00236),
}


def cell_uv(slot):
    """Atlas cell for a palette slot."""
    col = 2 * (slot % 8) + 1
    row = 9 if slot < 8 else 13
    return ((col + 0.5) / 16.0, (row + 0.5) / 16.0)


# ---------------------------------------------------------------------------
# 16-COLOR PALETTE DEFINITION
# ---------------------------------------------------------------------------
ROBE_GREEN    = 0   # Primary forest green robe, cape, boot uppers (#3d6335)
ROBE_DARK     = 1   # Deep olive/forest shadow & rune dark outline (#243e1f)
GOLD_TRIM     = 2   # Radiant gold collar trim, cape rune, wrist cuffs, frog knots (#dfb248)
LEATHER_BROWN = 3   # Dark brown leather belt, pants, right sleeve, hair fade (#482f1d)
HORN_LIGHT    = 4   # Slate Charcoal light plane for obsidian horn plates (#4d5464)
JADE_GEM      = 5   # Emerald/jade buckle core, chest frog gem, main Earth runes (#38b848)
HAIR          = 6   # Dark charcoal black spiky hair & deep obsidian shadow (#1a181e)
JADE_LIT      = 7   # Jade gemstone highlight & rune glow sparks (#68e878)
INK           = 8   # Face ink (lash lines, mouth, brows, left demonic sclera) (#1a1420)
HORN_MAIN     = 9   # Dark Slate core obsidian horn body (#272a33)
EYE_GOLD      = 10  # Radiant glowing golden amber left eye (#ffd700)
SILVER        = 11  # Earring studs and metal belt rivets (#d4e2ec)
WHITE         = 12  # Crisp boot soles, right eye sclera & glints (#f4faff)
SKIN          = 13  # Bronze warrior tan skin (#a8602c)
HORN_PURPLE   = 14  # Dark purple sheen & edge accent on obsidian horn (#3e2844)
EYE_EMERALD   = 15  # Rich dark natural forest green right eye iris (#1e5c32)

PALETTE = {
    ROBE_GREEN:    "3d6335",   # Forest green robe, cape, boot uppers
    ROBE_DARK:     "243e1f",   # Deep olive/forest shadow & rune outline
    GOLD_TRIM:     "dfb248",   # Radiant gold trims, sashes, rune, cuffs, frog knots
    LEATHER_BROWN: "482f1d",   # Warm dark brown leather & shaved undercut fade
    HORN_LIGHT:    "4d5464",   # Slate Charcoal light plane on obsidian horn
    JADE_GEM:      "38b848",   # Radiant emerald / jade belt gem & Earth runes
    HAIR:          "1a181e",   # Dark charcoal spiky hair & horn shadow crevices
    JADE_LIT:      "68e878",   # Jade highlight sparkle & rune glow
    INK:           "1a1420",   # Dark ink (lash lines, mouth, brows, left sclera)
    HORN_MAIN:     "272a33",   # Dark Slate obsidian horn main body
    EYE_GOLD:      "ffd700",   # Radiant glowing gold left eye
    SILVER:        "d4e2ec",   # Silver earrings & eyebrow piercing
    WHITE:         "f4faff",   # Boot sole tread, eye white & glints
    SKIN:          "a8602c",   # Warm bronze warrior caramel tan skin
    HORN_PURPLE:   "3e2844",   # Dark purple sheen & edge accent
    EYE_EMERALD:   "1e5c32",   # Rich dark natural forest green right eye iris
}

MAX_FACE_LUMINANCE = 0.30


# ---------------------------------------------------------------------------
# LEGS & COMBAT BOOTS
# ---------------------------------------------------------------------------
LEG_LEFT = [
    ("boot-sole-left",      "leg-left", (0.006, 0.000, -0.134), (0.158, 0.024, 0.082), WHITE),
    ("boot-upper-left",     "leg-left", (0.014, 0.024, -0.126), (0.152, 0.068, 0.076), ROBE_GREEN),
    ("boot-toe-left",       "leg-left", (0.012, 0.024, -0.132), (0.154, 0.042, -0.100), WHITE),
    ("boot-heel-left",      "leg-left", (0.012, 0.024, 0.050), (0.154, 0.056, 0.080), WHITE),
    ("boot-strap-left",     "leg-left", (0.010, 0.068, -0.080), (0.156, 0.086, 0.078), GOLD_TRIM),
    ("boot-strap-accent-l", "leg-left", (0.008, 0.072, -0.084), (0.158, 0.082, -0.070), GOLD_TRIM),
    
    ("pant-left",           "leg-left", (0.018, 0.086, -0.072), (0.150, 0.232, 0.072), LEATHER_BROWN),
    ("pant-cuff-left",      "leg-left", (0.014, 0.086, -0.078), (0.154, 0.104, 0.078), LEATHER_BROWN),
    ("knee-pad-left",       "leg-left", (0.024, 0.130, -0.080), (0.144, 0.168, -0.068), LEATHER_BROWN),
]

LEG_RIGHT = [
    ("boot-sole-right",      "leg-right", (-0.158, 0.000, -0.134), (-0.006, 0.024, 0.082), WHITE),
    ("boot-upper-right",     "leg-right", (-0.152, 0.024, -0.126), (-0.014, 0.068, 0.076), ROBE_GREEN),
    ("boot-toe-right",       "leg-right", (-0.154, 0.024, -0.132), (-0.012, 0.042, -0.100), WHITE),
    ("boot-heel-right",      "leg-right", (-0.154, 0.024, 0.050), (-0.012, 0.056, 0.080), WHITE),
    ("boot-strap-right",     "leg-right", (-0.156, 0.068, -0.080), (-0.010, 0.086, 0.078), GOLD_TRIM),
    ("boot-strap-accent-r",  "leg-right", (-0.158, 0.072, -0.084), (-0.008, 0.082, -0.070), GOLD_TRIM),
    
    ("pant-right",           "leg-right", (-0.150, 0.086, -0.072), (-0.018, 0.232, 0.072), LEATHER_BROWN),
    ("pant-cuff-right",      "leg-right", (-0.154, 0.086, -0.078), (-0.014, 0.104, 0.078), LEATHER_BROWN),
    ("knee-pad-right",       "leg-right", (-0.144, 0.130, -0.080), (-0.024, 0.168, -0.068), LEATHER_BROWN),
]


# ---------------------------------------------------------------------------
# TORSO, ROBE, BELT & CAPE
# ---------------------------------------------------------------------------
TORSO = [
    ("robe-body",             "torso", (-0.124, 0.232, -0.092), (0.124, 0.445, 0.092), LEATHER_BROWN),
    
    # 🥋 LAYERED WARRIOR CHEST, ROBE LAPELS & GOLD FROG FASTENERS
    # Exposed upper chest skin & Green Earth Rune trail
    ("neck-chest-skin",       "torso", (-0.035, 0.380, -0.095), (0.035, 0.445, -0.080), SKIN),
    ("rune-neck-slash",       "torso", (0.008, 0.380, -0.098), (0.028, 0.445, -0.082), JADE_GEM),
    ("rune-neck-shadow",      "torso", (0.006, 0.380, -0.097), (0.012, 0.445, -0.083), ROBE_DARK),
    ("rune-chest-branch",     "torso", (0.020, 0.395, -0.098), (0.055, 0.425, -0.084), JADE_GEM),
    
    # Left Lapel (Green fabric with gold border piping)
    ("lapel-body-l",          "torso", (0.020, 0.340, -0.102), (0.085, 0.445, -0.086), ROBE_GREEN),
    ("lapel-trim-outer-l",    "torso", (0.065, 0.340, -0.106), (0.085, 0.448, -0.084), GOLD_TRIM),
    ("lapel-trim-inner-l",    "torso", (0.020, 0.340, -0.106), (0.034, 0.448, -0.084), GOLD_TRIM),
    
    # Right Lapel (Green fabric with gold border piping)
    ("lapel-body-r",          "torso", (-0.085, 0.340, -0.102), (-0.020, 0.445, -0.086), ROBE_GREEN),
    ("lapel-trim-outer-r",    "torso", (-0.085, 0.340, -0.106), (-0.065, 0.448, -0.084), GOLD_TRIM),
    ("lapel-trim-inner-r",    "torso", (-0.034, 0.340, -0.106), (-0.020, 0.448, -0.084), GOLD_TRIM),
    
    # Martial Arts Chest Frog Knot & Medallion Fastener
    ("chest-frog-upper-bar",  "torso", (-0.032, 0.395, -0.108), (0.032, 0.408, -0.092), GOLD_TRIM),
    ("chest-frog-lower-bar",  "torso", (-0.028, 0.360, -0.108), (0.028, 0.373, -0.092), GOLD_TRIM),
    ("chest-frog-center-knot","torso", (-0.012, 0.388, -0.112), (0.012, 0.415, -0.090), GOLD_TRIM),
    ("chest-frog-emerald-gem","torso", (-0.008, 0.394, -0.114), (0.008, 0.410, -0.092), JADE_GEM),
    ("chest-frog-knot-lower", "torso", (-0.010, 0.354, -0.112), (0.010, 0.378, -0.090), GOLD_TRIM),
    ("chest-frog-emerald-low","torso", (-0.006, 0.359, -0.114), (0.006, 0.373, -0.092), JADE_GEM),
    
    # Flared Robe Coat-Tails
    ("coattail-l",            "torso", (0.015, 0.208, -0.102), (0.146, 0.278, 0.102), ROBE_GREEN),
    ("coattail-r",            "torso", (-0.146, 0.208, -0.102), (-0.015, 0.278, 0.102), ROBE_GREEN),
    ("coattail-hem-l",        "torso", (0.012, 0.202, -0.106), (0.150, 0.224, 0.106), GOLD_TRIM),
    ("coattail-hem-r",        "torso", (-0.150, 0.202, -0.106), (-0.012, 0.224, 0.106), GOLD_TRIM),
    ("coattail-edge-l",       "torso", (0.010, 0.218, -0.106), (0.030, 0.278, -0.090), GOLD_TRIM),
    ("coattail-edge-r",       "torso", (-0.030, 0.218, -0.106), (-0.010, 0.278, -0.090), GOLD_TRIM),
    ("coattail-outer-l",      "torso", (0.140, 0.218, -0.104), (0.152, 0.278, 0.104), GOLD_TRIM),
    ("coattail-outer-r",      "torso", (-0.152, 0.218, -0.104), (-0.140, 0.278, 0.104), GOLD_TRIM),
    # 🥋 Warrior Leather Belt (Dark brown waist belt with jade buckle plate matching reference)
    ("belt-tier-lower",       "torso", (-0.134, 0.272, -0.104), (0.134, 0.304, 0.104), LEATHER_BROWN),
    ("belt-tier-upper",       "torso", (-0.134, 0.306, -0.104), (0.134, 0.338, 0.104), LEATHER_BROWN),
    ("belt-seam-middle",      "torso", (-0.136, 0.302, -0.106), (0.136, 0.308, 0.106), ROBE_DARK),
    
    # Jade Medallion Buckle
    ("medallion-plate-gold",  "torso", (-0.048, 0.272, -0.118), (0.048, 0.338, -0.096), GOLD_TRIM),
    ("medallion-rim-top",     "torso", (-0.038, 0.332, -0.122), (0.038, 0.344, -0.096), GOLD_TRIM),
    ("medallion-rim-bot",     "torso", (-0.038, 0.266, -0.122), (0.038, 0.278, -0.096), GOLD_TRIM),
    ("medallion-rim-left",    "torso", (0.042, 0.280, -0.122), (0.054, 0.330, -0.096), GOLD_TRIM),
    ("medallion-rim-right",   "torso", (-0.054, 0.280, -0.122), (-0.042, 0.330, -0.096), GOLD_TRIM),
    ("jade-gem-core",         "torso", (-0.030, 0.282, -0.124), (0.030, 0.328, -0.106), ROBE_GREEN),
    ("jade-gem-lit",          "torso", (-0.015, 0.294, -0.128), (0.015, 0.316, -0.112), JADE_GEM),
    
    # Standing Flared Collar
    ("collar-back",           "torso", (-0.128, 0.435, 0.082), (0.128, 0.500, 0.120), ROBE_GREEN),
    ("collar-back-trim",      "torso", (-0.134, 0.490, 0.078), (0.134, 0.506, 0.124), GOLD_TRIM),
    ("collar-back-inner",     "torso", (-0.118, 0.435, 0.072), (0.118, 0.490, 0.085), ROBE_DARK),
    ("collar-wing-l",         "torso", (0.075, 0.430, -0.095), (0.160, 0.510, 0.105), ROBE_GREEN),
    ("collar-wing-trim-l",    "torso", (0.070, 0.495, -0.102), (0.166, 0.516, 0.112), GOLD_TRIM),
    ("collar-wing-front-l",   "torso", (0.065, 0.410, -0.120), (0.158, 0.500, -0.080), GOLD_TRIM),
    ("collar-wing-inner-l",   "torso", (0.068, 0.430, -0.085), (0.085, 0.490, 0.085), ROBE_DARK),
    ("collar-wing-r",         "torso", (-0.160, 0.430, -0.095), (-0.075, 0.510, 0.105), ROBE_GREEN),
    ("collar-wing-trim-r",    "torso", (-0.166, 0.495, -0.102), (-0.070, 0.516, 0.112), GOLD_TRIM),
    ("collar-wing-front-r",   "torso", (-0.158, 0.410, -0.120), (-0.065, 0.500, -0.080), GOLD_TRIM),
    ("collar-wing-inner-r",   "torso", (-0.085, 0.430, -0.085), (-0.068, 0.490, 0.085), ROBE_DARK),
    
    # Back Cape with Earth Rune
    ("cape-main-body",        "torso", (-0.114, 0.170, 0.098), (0.114, 0.445, 0.118), ROBE_GREEN),
    ("cape-drape-shadow",     "torso", (-0.100, 0.180, 0.092), (0.100, 0.435, 0.102), ROBE_DARK),
    ("cape-trim-left",        "torso", (0.096, 0.165, 0.096), (0.120, 0.448, 0.122), GOLD_TRIM),
    ("cape-trim-right",       "torso", (-0.120, 0.165, 0.096), (-0.096, 0.448, 0.122), GOLD_TRIM),
    ("cape-trim-bottom",      "torso", (-0.120, 0.162, 0.096), (0.120, 0.185, 0.122), GOLD_TRIM),
    
    ("rune-diamond-top",      "torso", (-0.040, 0.355, 0.118), (0.040, 0.388, 0.124), GOLD_TRIM),
    ("rune-diamond-bot",      "torso", (-0.040, 0.285, 0.118), (0.040, 0.318, 0.124), GOLD_TRIM),
    ("rune-diamond-left",     "torso", (0.032, 0.305, 0.118), (0.055, 0.368, 0.124), GOLD_TRIM),
    ("rune-diamond-right",    "torso", (-0.055, 0.305, 0.118), (-0.032, 0.368, 0.124), GOLD_TRIM),
    ("rune-core-dot",         "torso", (-0.016, 0.326, 0.120), (0.016, 0.346, 0.126), GOLD_TRIM),
    ("rune-pedestal",         "torso", (-0.036, 0.235, 0.118), (0.036, 0.265, 0.124), GOLD_TRIM),
    ("rune-pedestal-stem",    "torso", (-0.012, 0.260, 0.118), (0.012, 0.282, 0.124), GOLD_TRIM),
]# ---------------------------------------------------------------------------
# ARMS: ASYMMETRICAL WARRIOR ARMS (SLEEK, FLUSH TEXTURE-EXACT GEOMETRY)
# LEFT = DEMONIC VESSEL BARE ARM + SLEEK GEOMETRIC RUNES (+X)
# RIGHT = DARK BROWN SHORT SLEEVE + GOLD CUFF + BARE FOREARM VINES (-X)
# ---------------------------------------------------------------------------
# ARMS: ASYMMETRICAL WARRIOR ARMS (SLEEK, FLUSH TEXTURE-EXACT GEOMETRY)
# LEFT = DEMONIC VESSEL BARE ARM + SLEEK GEOMETRIC RUNES (+X)
# RIGHT = DARK BROWN SHORT SLEEVE + GOLD CUFF + BARE FOREARM VINES (-X)
# ---------------------------------------------------------------------------
ARM_LEFT = [
    # 💪 BARE MUSCULAR BRONZE SKIN ARM (+X, Completely Sleeveless)
    ("arm-main-l",            "arm-left", (0.0999, 0.340, -0.055), (0.310, 0.460, 0.055), SKIN),
    ("hand-left",             "arm-left", (0.300, 0.340, -0.050), (0.3836, 0.458, 0.050), SKIN),
]

ARM_RIGHT = [
    ("sleeve-right",          "arm-right", (-0.205, 0.338, -0.062), (-0.0999, 0.462, 0.062), LEATHER_BROWN),
    ("sleeve-shadow-r",       "arm-right", (-0.204, 0.336, -0.060), (-0.102, 0.354, 0.060), ROBE_DARK),
    ("sleeve-cuff-r",         "arm-right", (-0.235, 0.336, -0.066), (-0.205, 0.464, 0.066), GOLD_TRIM),
    ("sleeve-cuff-shadow",    "arm-right", (-0.210, 0.334, -0.068), (-0.205, 0.466, 0.068), ROBE_DARK),
    ("arm-main-r",            "arm-right", (-0.310, 0.340, -0.055), (-0.0999, 0.460, 0.055), SKIN),
    ("hand-right",            "arm-right", (-0.3836, 0.340, -0.050), (-0.300, 0.458, 0.050), SKIN),
]

# ---------------------------------------------------------------------------
# ARMS CONTINUOUS QUAD-STRIP DECALS (SLEEK, FLUSH ORGANIC RUNES & MARKINGS)
# ---------------------------------------------------------------------------
ARM_DECALS = [
    # =========================================================================
    # 1. FRONT DEMONIC ARM (+X, Arm-Left, Front Face Z = -0.055 -> Mesh +Z)
    # Authored bounds: X in [0.0999, 0.310], Y in [0.340, 0.460]
    # =========================================================================
    # A. Bicep Crease Shadow Notch & Crevice
    ("f-dem-bicep-shadow", "arm-left", "front", LEATHER_BROWN,
     [(0.100, 0.340), (0.138, 0.340), (0.136, 0.410), (0.100, 0.440)], 1),
    ("f-dem-bicep-crevice", "arm-left", "front", ROBE_DARK,
     [(0.100, 0.340), (0.124, 0.340), (0.120, 0.390), (0.100, 0.420)], 2),

    # B. Lower Carapace Dark Shadow Underline
    ("f-dem-carapace-shadow", "arm-left", "front", ROBE_DARK,
     [(0.135, 0.340), (0.308, 0.340), (0.308, 0.355), (0.135, 0.355)], 1),

    # C. Chevron 1: Upper/Inner 45° Angular Chevron Conduit
    ("f-dem-chev1-diag", "arm-left", "front", ROBE_GREEN,
     [(0.155, 0.460), (0.185, 0.460), (0.245, 0.400), (0.215, 0.400)], 2),
    ("f-dem-chev1-leg", "arm-left", "front", ROBE_GREEN,
     [(0.215, 0.355), (0.245, 0.355), (0.245, 0.400), (0.215, 0.400)], 2),

    # D. Chevron 2: Outer/Forearm 45° Angular Chevron Conduit
    ("f-dem-chev2-diag", "arm-left", "front", ROBE_GREEN,
     [(0.220, 0.460), (0.250, 0.460), (0.308, 0.402), (0.278, 0.402)], 2),
    ("f-dem-chev2-leg", "arm-left", "front", ROBE_GREEN,
     [(0.278, 0.355), (0.308, 0.355), (0.308, 0.402), (0.278, 0.402)], 2),

    # =========================================================================
    # 2. OUTER SIDE DEMONIC ARM (+X, Arm-Left, Outer Side Face X = 0.310)
    # Authored bounds: Z in [-0.055, 0.055], Y in [0.340, 0.460]
    # =========================================================================
    # A. Lower Base Shadow
    ("s-dem-shadow", "arm-left", "left", ROBE_DARK,
     [(-0.050, 0.340), (0.045, 0.340), (0.045, 0.355), (-0.050, 0.355)], 1),

    # B. Lower Horizontal Base Connector
    ("s-dem-bottom-plate", "arm-left", "left", ROBE_GREEN,
     [(-0.050, 0.355), (0.040, 0.355), (0.040, 0.375), (-0.050, 0.375)], 2),

    # C. Front Vertical Conduit
    ("s-dem-front-vert", "arm-left", "left", ROBE_GREEN,
     [(-0.050, 0.375), (-0.025, 0.375), (-0.025, 0.435), (-0.050, 0.435)], 2),

    # D. 45° Diagonal Conduit Bridge (Otsutsuki Karma arch)
    ("s-dem-diag-bridge", "arm-left", "left", ROBE_GREEN,
     [(-0.025, 0.415), (-0.005, 0.415), (0.035, 0.445), (0.015, 0.445)], 2),

    # E. Back Vertical Conduit
    ("s-dem-back-vert", "arm-left", "left", ROBE_GREEN,
     [(0.015, 0.375), (0.040, 0.375), (0.040, 0.445), (0.015, 0.445)], 2),

    # =========================================================================
    # 3. BACK DEMONIC ARM (+X, Arm-Left, Back Face Z = 0.055 -> Mesh -Z)
    # Authored bounds: X in [0.0999, 0.310], Y in [0.340, 0.460]
    # =========================================================================
    # A. Shadow Accents Under Circuit Bars
    ("b-dem-shadow", "arm-left", "back", ROBE_DARK,
     [(0.115, 0.340), (0.308, 0.340), (0.308, 0.355), (0.115, 0.355)], 1),

    # B. Chevron 1: Upper/Inner 45° Angular Chevron Conduit
    ("b-dem-chev1-diag", "arm-left", "back", ROBE_GREEN,
     [(0.155, 0.460), (0.185, 0.460), (0.245, 0.400), (0.215, 0.400)], 2),
    ("b-dem-chev1-leg", "arm-left", "back", ROBE_GREEN,
     [(0.215, 0.355), (0.245, 0.355), (0.245, 0.400), (0.215, 0.400)], 2),

    # C. Chevron 2: Outer/Forearm 45° Angular Chevron Conduit
    ("b-dem-chev2-diag", "arm-left", "back", ROBE_GREEN,
     [(0.220, 0.460), (0.250, 0.460), (0.308, 0.402), (0.278, 0.402)], 2),
    ("b-dem-chev2-leg", "arm-left", "back", ROBE_GREEN,
     [(0.278, 0.355), (0.308, 0.355), (0.308, 0.402), (0.278, 0.402)], 2),

    # =========================================================================
    # 4. TOP DEMONIC ARM (+X, Arm-Left, Top Face Y = 0.460 -> Mesh Y = 0.348)
    # Authored bounds: X in [0.0999, 0.310], Z in [-0.055, 0.055]
    # =========================================================================
    # A. Band 1 Top Crest Wrap
    ("top-dem-band1-wrap", "arm-left", "top", ROBE_GREEN,
     [(0.155, -0.050), (0.185, -0.050), (0.185, 0.050), (0.155, 0.050)], 2),

    # B. Band 2 Top Crest Wrap
    ("top-dem-band2-wrap", "arm-left", "top", ROBE_GREEN,
     [(0.220, -0.050), (0.250, -0.050), (0.250, 0.050), (0.220, 0.050)], 2),

    # C. Outer Crest Wrap
    ("top-dem-side-wrap", "arm-left", "top", ROBE_GREEN,
     [(0.285, -0.050), (0.308, -0.050), (0.308, 0.050), (0.285, 0.050)], 2),

    # =========================================================================
    # 5. BOTTOM DEMONIC ARM (+X, Arm-Left, Bottom Face Y = 0.340 -> Mesh Y = 0.228)
    # Authored bounds: X in [0.0999, 0.310], Z in [-0.055, 0.055]
    # =========================================================================
    ("bot-dem-plate", "arm-left", "bottom", ROBE_GREEN,
     [(0.215, -0.050), (0.308, -0.050), (0.308, 0.050), (0.215, 0.050)], 2),

    # =========================================================================
    # 6. FRONT NATURAL ARM (-X, Arm-Right, Front Face Z = -0.055 -> Mesh +Z)
    # Authored bounds: X in [-0.310, -0.0999], Forearm X in [-0.310, -0.235]
    # NOTE: Dark brown short sleeve [-0.205, -0.0999] & Gold cuff [-0.235, -0.205] PRESERVED!
    # =========================================================================
    # A. Forearm Bottom Shadow Underline
    ("f-nat-bottom-shadow", "arm-right", "front", ROBE_DARK,
     [(-0.308, 0.340), (-0.235, 0.340), (-0.235, 0.355), (-0.308, 0.355)], 1),

    # B. Continuous Flowing Vine
    ("f-nat-top-emerge", "arm-right", "front", ROBE_GREEN,
     [(-0.255, 0.420), (-0.235, 0.420), (-0.235, 0.455), (-0.255, 0.455)], 2),
    ("f-nat-diag-sweep", "arm-right", "front", ROBE_GREEN,
     [(-0.275, 0.380), (-0.250, 0.380), (-0.235, 0.425), (-0.255, 0.425)], 2),
    ("f-nat-bottom-band", "arm-right", "front", ROBE_GREEN,
     [(-0.295, 0.355), (-0.235, 0.355), (-0.235, 0.380), (-0.295, 0.380)], 2),
    ("f-nat-wrist-hook", "arm-right", "front", ROBE_GREEN,
     [(-0.308, 0.370), (-0.280, 0.370), (-0.280, 0.420), (-0.308, 0.420)], 2),

    # =========================================================================
    # 7. OUTER SIDE NATURAL ARM (-X, Arm-Right, Outer Side Face X = -0.310)
    # Authored bounds: Z in [-0.055, 0.055], Y in [0.340, 0.460]
    # =========================================================================
    ("s-nat-wrist-wrap", "arm-right", "right", ROBE_GREEN,
     [(-0.050, 0.370), (0.050, 0.370), (0.050, 0.400), (-0.050, 0.400)], 2),

    # =========================================================================
    # 8. BACK NATURAL ARM (-X, Arm-Right, Back Face Z = 0.055 -> Mesh -Z)
    # Authored bounds: Forearm X in [-0.310, -0.235], Y in [0.340, 0.460]
    # =========================================================================
    # A. Forearm Bottom Shadow Underline
    ("b-nat-bottom-shadow", "arm-right", "back", ROBE_DARK,
     [(-0.308, 0.340), (-0.235, 0.340), (-0.235, 0.355), (-0.308, 0.355)], 1),

    # B. U-Shaped Serpentine Loop
    ("b-nat-top-entry", "arm-right", "back", ROBE_GREEN,
     [(-0.255, 0.420), (-0.235, 0.420), (-0.235, 0.455), (-0.255, 0.455)], 2),
    ("b-nat-desc-runner", "arm-right", "back", ROBE_GREEN,
     [(-0.255, 0.375), (-0.236, 0.375), (-0.236, 0.425), (-0.255, 0.425)], 2),
    ("b-nat-bottom-bar", "arm-right", "back", ROBE_GREEN,
     [(-0.295, 0.355), (-0.236, 0.355), (-0.236, 0.380), (-0.295, 0.380)], 2),
    ("b-nat-wrist-hook", "arm-right", "back", ROBE_GREEN,
     [(-0.308, 0.370), (-0.280, 0.370), (-0.280, 0.420), (-0.308, 0.420)], 2),

    # =========================================================================
    # 9. TOP NATURAL ARM (-X, Arm-Right, Top Face Y = 0.460 -> Mesh Y = 0.348)
    # Authored bounds: Forearm X in [-0.310, -0.235], Z in [-0.055, 0.055]
    # =========================================================================
    ("top-nat-vine", "arm-right", "top", ROBE_GREEN,
     [(-0.255, -0.050), (-0.235, -0.050), (-0.235, 0.050), (-0.255, 0.050)], 2),
]

BODY_PANELS = []


# ---------------------------------------------------------------------------
# HEAD, SPIKY HAIR, UNDERCUT FADE & OBSIDIAN SWEPT-BACK HORN (+X)
# ---------------------------------------------------------------------------
HEAD = [
    # 💇 FULL SPIKY BLACK ANIME HAIR CROWN & FULL BACK SKULL (Full 360° skull dome & solid nape drape!)
    ("hair-crown-main",       "head", (-0.165, 0.620, -0.215), (0.150, 0.745, 0.130), HAIR),
    ("hair-crown-top",        "head", (-0.130, 0.720, -0.190), (0.110, 0.785, 0.100), HAIR),
    ("hair-back-skull-upper", "head", (-0.175, 0.520, -0.215), (0.170, 0.640, -0.080), HAIR),
    ("hair-back-skull-mid",   "head", (-0.170, 0.420, -0.210), (0.165, 0.530, -0.090), HAIR),
    ("hair-back-nape-full",   "head", (-0.165, 0.320, -0.205), (0.160, 0.430, -0.100), HAIR),
    
    # Right side full hair (-X has long thick hair)
    ("hair-skull-right",      "head", (-0.180, 0.460, -0.180), (-0.145, 0.640, 0.080), HAIR),
    ("hair-temple-right",     "head", (-0.195, 0.480, -0.120), (-0.155, 0.660, 0.090), HAIR),
    ("hair-sideburn-right",   "head", (-0.190, 0.420, -0.020), (-0.155, 0.530, 0.070), HAIR),
    
    # 💈 SHAVED BUZZCUT UNDERCUT SIDE PANEL (+X side around ear and temple ONLY!)
    ("undercut-side-main",    "head", (0.145, 0.410, -0.080), (0.184, 0.630, 0.090), LEATHER_BROWN),
    ("undercut-side-front",   "head", (0.145, 0.430, 0.080), (0.182, 0.620, 0.145), LEATHER_BROWN),
    ("undercut-sideburn",     "head", (0.160, 0.400, -0.010), (0.188, 0.500, 0.080), LEATHER_BROWN),

    
    # 💇 DYNAMIC ANIME SPIKES (Front, top, back)
    ("spike-front-c1",        "head", (-0.050, 0.670, 0.115), (0.040, 0.765, 0.192), HAIR),
    ("spike-front-c2",        "head", (-0.035, 0.700, 0.130), (0.030, 0.778, 0.198), HAIR),
    ("spike-front-l1",        "head", (0.010, 0.650, 0.100), (0.095, 0.740, 0.175), HAIR),
    ("spike-front-l2",        "head", (0.040, 0.640, 0.080), (0.125, 0.755, 0.160), HAIR),
    ("spike-front-r1",        "head", (-0.120, 0.655, 0.100), (-0.040, 0.750, 0.185), HAIR),
    ("spike-front-r2",        "head", (-0.155, 0.630, 0.080), (-0.080, 0.725, 0.165), HAIR),
    
    # Top Ridge Spikes
    ("spike-top-crest1",      "head", (-0.045, 0.740, -0.060), (0.035, 0.785, 0.050), HAIR),
    ("spike-top-crest2",      "head", (-0.040, 0.730, -0.130), (0.030, 0.775, -0.040), HAIR),
    ("spike-top-l1",          "head", (0.035, 0.730, -0.060), (0.095, 0.778, 0.050), HAIR),
    ("spike-top-l2-back",     "head", (0.030, 0.710, -0.210), (0.110, 0.765, -0.120), HAIR),
    ("spike-top-r1",          "head", (-0.105, 0.720, -0.050), (-0.035, 0.768, 0.060), HAIR),
    ("spike-top-r2",          "head", (-0.115, 0.700, -0.120), (-0.040, 0.758, -0.020), HAIR),
    
    # Back Crown Spikes & Tufts
    ("spike-back-crest",      "head", (-0.055, 0.660, -0.220), (0.045, 0.740, -0.140), HAIR),
    ("spike-back-r1",         "head", (-0.135, 0.610, -0.215), (-0.045, 0.710, -0.135), HAIR),
    ("spike-back-l1",         "head", (0.010, 0.610, -0.218), (0.105, 0.710, -0.135), HAIR),
    ("spike-back-mid",        "head", (-0.070, 0.520, -0.212), (0.050, 0.630, -0.145), HAIR),
    
    # Front Forehead Bang Tufts
    ("fringe-center",         "head", (-0.035, 0.585, 0.145), (0.040, 0.665, 0.176), HAIR),
    ("fringe-l1",             "head", (0.015, 0.580, 0.135), (0.075, 0.655, 0.175), HAIR),
    ("fringe-l2-circle",      "head", (0.045, 0.580, 0.125), (0.125, 0.675, 0.175), HAIR),
    ("fringe-r1",             "head", (-0.095, 0.575, 0.140), (-0.030, 0.655, 0.172), HAIR),
    ("fringe-r2",             "head", (-0.150, 0.560, 0.130), (-0.090, 0.645, 0.165), HAIR),
    
    # 💎 SILVER STUD EARRINGS ON BOTH EARLOBES
    ("earring-stud-left",     "head", (0.174, 0.405, -0.015), (0.194, 0.428, 0.005), SILVER),
    ("earring-stud-right",    "head", (-0.194, 0.405, -0.015), (-0.174, 0.428, 0.005), SILVER),
]

DONOR_SPACE = tuple(entry[0] for entry in HEAD)

# ---------------------------------------------------------------------------
# § THE FAMILY PASS.
# ---------------------------------------------------------------------------
WAS_HIPS, WAS_SHOULDER, WAS_NECK, WAS_TOP = 0.232, 0.400, 0.445, 0.722
NOW_HIPS, NOW_SHOULDER, NOW_NECK, NOW_TOP = 0.176, 0.288, 0.343, 0.7234

HEAD_GROWTH = (NOW_TOP - NOW_NECK) / (WAS_TOP - WAS_NECK)
CAST_MIN_HEIGHT, CAST_MAX_HEIGHT = 0.6613, 0.7928


def _remap_y(y):
    if y <= WAS_HIPS:
        return y / WAS_HIPS * NOW_HIPS
    if y <= WAS_NECK:
        t = (y - WAS_HIPS) / (WAS_NECK - WAS_HIPS)
        return NOW_HIPS + t * (NOW_NECK - NOW_HIPS)

    t = (y - WAS_NECK) / (WAS_TOP - WAS_NECK)
    return NOW_NECK + t * (NOW_TOP - NOW_NECK)


def _family(boxes, head, as_authored=()):
    out = []
    for entry in boxes:
        name, bone, lo, hi, slot = entry[:5]
        rest = entry[5:]

        if name in as_authored:
            out.append((name, bone,
                        (lo[0], lo[1], -hi[2]), (hi[0], hi[1], -lo[2]), slot) + rest)
            continue

        if bone in ("arm-left", "arm-right"):
            shift = NOW_SHOULDER - WAS_SHOULDER
            lo = (lo[0], lo[1] + shift, lo[2])
            hi = (hi[0], hi[1] + shift, hi[2])
            out.append((name, bone, lo, hi, slot) + rest)
            continue

        grow = HEAD_GROWTH if head else 1.0

        lo = (lo[0] * grow, _remap_y(lo[1]), lo[2] * grow)
        hi = (hi[0] * grow, _remap_y(hi[1]), hi[2] * grow)

        out.append((name, bone, lo, hi, slot) + rest)

    return out


def signed_area_2d(pts):
    return 0.5 * sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1]
                     for i in range(len(pts)))


def _family_panels(panels):
    out = []
    for name, bone, low, high, z_plane, face, rows in panels:
        if bone in ("arm-left", "arm-right"):
            shift = NOW_SHOULDER - WAS_SHOULDER
            low = (low[0], low[1] + shift)
            high = (high[0], high[1] + shift)
        else:
            low = (low[0], _remap_y(low[1]))
            high = (high[0], _remap_y(high[1]))
        out.append((name, bone, low, high, z_plane, face, rows))
    return out


def _family_decals(decals):
    out = []
    for name, bone, face, slot, poly, layer in decals:
        shift = NOW_SHOULDER - WAS_SHOULDER if bone in ("arm-left", "arm-right") else 0.0
        if isinstance(poly, tuple) and len(poly) == 2 and isinstance(poly[0], tuple):
            if face in ("front", "back", "left", "right"):
                poly_shifted = ((poly[0][0], poly[0][1] + shift), (poly[1][0], poly[1][1] + shift))
            else:
                poly_shifted = poly
        elif isinstance(poly, (list, tuple)):
            if face in ("front", "back", "left", "right"):
                poly_shifted = [(p[0], p[1] + shift) for p in poly]
            else:
                poly_shifted = list(poly)
        else:
            poly_shifted = poly
        out.append((name, bone, face, slot, poly_shifted, layer))
    return out


BODY_BOXES = (LEG_LEFT + LEG_RIGHT
              + TORSO
              + ARM_LEFT + ARM_RIGHT)
HEAD_BOXES = HEAD

# ---------------------------------------------------------------------------
# § THE DONATED HEAD, SCAR & HETEROCHROMIA EYES
# ---------------------------------------------------------------------------
DONOR_SKULL = "Assets/TumbangPreso/Art/characters/persons/character-male-d.glb"
SKULL_SLOTS = {15: SKIN, 8: None}

DONOR_MOUTH_Y = 0.45
DONOR_MOUTH_TRIS = 8
DONOR_MOUTH_VERTS = 10
DONOR_EYE_TRIS = 12
DONOR_EYE_VERTS = 16

MOUTH_Z = 0.1596
MOUTH_HALF = 0.038
MOUTH_BASE = 0.4150
MOUTH_RISE = 0.006
MOUTH_THIN = 0.0035
MOUTH_THICK = 0.0065
MOUTH_HOOK_FROM = 0.60
MOUTH_HOOK = 0.004
MOUTH_STEPS = 12


def _mouth_polygon():
    """Confident slight warrior smirk on face plate."""
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
    """Builds the donor skull with Jade Earth Rune facial incision, heterochromia eyes, and smirk mouth."""
    pos, nrm, uv, tris = _donor_part(DONOR_SKULL, SKULL_SLOTS)

    # 1. Strip donor mouth and eyes (all slot 8 triangles) so our authored features take full crisp ownership!
    tris = [t for t in tris if not (_slot_at(*uv[t[0]]) == INK)]

    # Helper function to add a CCW quad facing +Z
    def add_quad(points_bl_tl_tr_br, slot, z_offset):
        bl, tl, tr, br = points_bl_tl_tr_br
        z = MOUTH_Z + z_offset
        first_idx = len(pos)
        pos.append((bl[0], bl[1], z))
        pos.append((tl[0], tl[1], z))
        pos.append((tr[0], tr[1], z))
        pos.append((br[0], br[1], z))
        for _ in range(4):
            nrm.append((0.0, 0.0, 1.0))
            uv.append(cell_uv(slot))
        # CCW triangles: (BL, BR, TR) and (BL, TR, TL)
        tris.append((first_idx, first_idx + 3, first_idx + 2))
        tris.append((first_idx, first_idx + 2, first_idx + 1))

    # 2. ⚔️ STOIC BADASS WARRIOR MOUTH & CHIN CONTOUR
    # Tight, firm, stoic warrior mouth line (Kawaki brooding expression)
    add_quad([(-0.028, 0.407), (-0.028, 0.413), (0.028, 0.413), (0.028, 0.407)],
             INK, PANEL_PROUD * 2.0)
    # Subtle downward corner accents (stoic intensity)
    add_quad([(-0.034, 0.403), (-0.028, 0.413), (-0.028, 0.407), (-0.034, 0.400)],
             INK, PANEL_PROUD * 2.0)
    add_quad([(0.028, 0.407), (0.028, 0.413), (0.034, 0.403), (0.034, 0.400)],
             INK, PANEL_PROUD * 2.0)
    # Underlip shadow notch / Chin contour
    add_quad([(-0.009, 0.390), (-0.009, 0.397), (0.009, 0.397), (0.009, 0.390)],
             INK, PANEL_PROUD * 1.6)

    # 3. ⚔️ WARRIOR FACIAL SCAR & UNDERCUT TEMPLE (media_1787202252967.png)
    # Natural Stepped Buzzcut Undercut Hairline on Temple (+X)
    add_quad([(0.065, 0.605), (0.065, 0.665), (0.170, 0.665), (0.170, 0.605)],
             LEATHER_BROWN, PANEL_PROUD * 1.5)
    add_quad([(0.095, 0.535), (0.095, 0.605), (0.170, 0.605), (0.170, 0.535)],
             LEATHER_BROWN, PANEL_PROUD * 1.5)
    add_quad([(0.130, 0.450), (0.130, 0.535), (0.170, 0.535), (0.170, 0.450)],
             LEATHER_BROWN, PANEL_PROUD * 1.5)
    add_quad([(0.150, 0.405), (0.150, 0.450), (0.170, 0.450), (0.170, 0.405)],
             LEATHER_BROWN, PANEL_PROUD * 1.5)

    # Subtle Flowing Warrior Face Scar (Continuous diagonal incision slicing through left eye)
    add_quad([(0.038, 0.585), (0.038, 0.655), (0.050, 0.655), (0.050, 0.585)],
             LEATHER_BROWN, PANEL_PROUD * 2.2)
    add_quad([(0.042, 0.525), (0.042, 0.585), (0.054, 0.585), (0.054, 0.525)],
             LEATHER_BROWN, PANEL_PROUD * 2.2)
    add_quad([(0.052, 0.485), (0.052, 0.525), (0.066, 0.525), (0.066, 0.485)],
             LEATHER_BROWN, PANEL_PROUD * 2.2)
    add_quad([(0.078, 0.445), (0.078, 0.488), (0.096, 0.488), (0.092, 0.445)],
             LEATHER_BROWN, PANEL_PROUD * 2.2)
    add_quad([(0.068, 0.385), (0.078, 0.448), (0.092, 0.448), (0.082, 0.385)],
             LEATHER_BROWN, PANEL_PROUD * 2.2)
    add_quad([(0.052, 0.335), (0.068, 0.390), (0.082, 0.390), (0.064, 0.335)],
             LEATHER_BROWN, PANEL_PROUD * 2.2)

    # Scar Depth Crevice / Shadow
    add_quad([(0.036, 0.585), (0.036, 0.655), (0.038, 0.655), (0.038, 0.585)],
             INK, PANEL_PROUD * 2.1)
    add_quad([(0.040, 0.525), (0.040, 0.585), (0.042, 0.585), (0.042, 0.525)],
             INK, PANEL_PROUD * 2.1)
    add_quad([(0.050, 0.485), (0.050, 0.525), (0.052, 0.525), (0.052, 0.485)],
             INK, PANEL_PROUD * 2.1)

    # 4. 👁️ EXACT MATCHING ANIME EYE SHAPE & DESIGN (EMERALD / GOLD HETEROCHROMIA)
    # --- RIGHT EYE (-X, Viewer's Left): NORMAL SLEEK EMERALD GREEN ANIME EYE (WHITE SCLERA) ---
    # White Sclera Aperture (Exact mirrored shape of left eye aperture)
    add_quad([(-0.114, 0.460), (-0.114, 0.496), (-0.034, 0.502), (-0.034, 0.460)],
             WHITE, PANEL_PROUD * 2.8)
    # Radiant Emerald Green Iris (Fills the iris aperture)
    add_quad([(-0.092, 0.464), (-0.092, 0.492), (-0.054, 0.488), (-0.054, 0.464)],
             EYE_EMERALD, PANEL_PROUD * 3.2)
    # Centered Deep Charcoal Pupil Core (Normal round/square anime pupil, NOT a slit!)
    add_quad([(-0.078, 0.468), (-0.078, 0.486), (-0.064, 0.484), (-0.064, 0.468)],
             INK, PANEL_PROUD * 3.6)
    # Crisp Bright Anime Glint Sparkle (Top-left glint highlight)
    add_quad([(-0.082, 0.476), (-0.082, 0.484), (-0.072, 0.483), (-0.072, 0.476)],
             WHITE, PANEL_PROUD * 4.2)
    # Thick Sharp Slanted Upper Lash Wing (INK)
    add_quad([(-0.116, 0.488), (-0.116, 0.504), (-0.032, 0.500), (-0.032, 0.484)],
             INK, PANEL_PROUD * 4.0)
    # Lower Lash Continuous Underline (INK)
    add_quad([(-0.114, 0.460), (-0.114, 0.466), (-0.034, 0.466), (-0.034, 0.460)],
             INK, PANEL_PROUD * 4.0)

    # --- LEFT EYE (+X, Viewer's Right): DEMONIC BLACK SCLERA WITH GLOWING GOLDEN IRIS ---
    # Jade Earth Rune Eye Frame Bed
    add_quad([(0.030, 0.456), (0.030, 0.498), (0.118, 0.504), (0.118, 0.456)],
             ROBE_DARK, PANEL_PROUD * 2.3)
    # Pitch-Black Sclera & Eye Body (COMPLETELY BLACK, ZERO WHITE!)
    add_quad([(0.034, 0.460), (0.034, 0.496), (0.114, 0.502), (0.114, 0.460)],
             INK, PANEL_PROUD * 2.8)
    # Radiant Glowing Golden Iris in Center (EYE_GOLD)
    add_quad([(0.054, 0.464), (0.054, 0.488), (0.092, 0.492), (0.092, 0.464)],
             EYE_GOLD, PANEL_PROUD * 3.4)
    # Sharp Otsutsuki Vertical Pupil Slit (INK)
    add_quad([(0.068, 0.466), (0.068, 0.488), (0.076, 0.488), (0.076, 0.466)],
             INK, PANEL_PROUD * 3.8)
    # Radiant Glowing Core Flare (WHITE)
    add_quad([(0.070, 0.473), (0.070, 0.483), (0.074, 0.483), (0.074, 0.473)],
             WHITE, PANEL_PROUD * 4.2)
    # Thick Sharp Slanted Upper Lash Wing (INK)
    add_quad([(0.032, 0.484), (0.032, 0.500), (0.116, 0.504), (0.116, 0.488)],
             INK, PANEL_PROUD * 4.0)
    # Lower Lash Continuous Underline (INK)
    add_quad([(0.034, 0.460), (0.034, 0.466), (0.114, 0.468), (0.114, 0.460)],
             INK, PANEL_PROUD * 4.0)

    # 5. 😠 EXACT MATCHING ARCHED ANIME EYEBROWS & PIERCING
    # Right Eyebrow (Solid continuous arched anime eyebrow, thickness 0.018)
    add_quad([(-0.114, 0.514), (-0.114, 0.532), (-0.036, 0.518), (-0.036, 0.498)],
             INK, PANEL_PROUD * 3.4)
    # Silver Eyebrow Piercing Bar on Right Brow Outer Edge
    add_quad([(-0.116, 0.520), (-0.116, 0.532), (-0.104, 0.532), (-0.104, 0.520)],
             SILVER, PANEL_PROUD * 3.8)

    # Left Eyebrow (Exact matching arched anime eyebrow split by Karma slash)
    # Inner piece
    add_quad([(0.036, 0.498), (0.036, 0.516), (0.052, 0.518), (0.052, 0.500)],
             INK, PANEL_PROUD * 3.4)
    # Outer piece
    add_quad([(0.072, 0.508), (0.072, 0.526), (0.114, 0.532), (0.114, 0.514)],
             INK, PANEL_PROUD * 3.4)

    # 6. 🪨 CHUNKY LOW-POLY SWEPT-BACK OTSUTSUKI HORN (media_1787216732286.jpg Iteration B)
    _add_horn_geometry(pos, nrm, uv, tris)

    return _compact(pos, nrm, uv, tris)


def _add_horn_geometry(pos, nrm, uv, tris):
    """Generates continuous faceted swept-back Otsutsuki horn matching 4-tone reference sheet."""
    # Stations along the swept spine curve from side temple base to sharp swept tip
    # Coordinate frame: +X=Left, +Y=Up, +Z=Front, -Z=Back
    stations = [
        # (t, cx, cy, cz, w, h)
        (0.00, 0.155, 0.540,  0.050, 0.040, 0.042),
        (0.20, 0.175, 0.575,  0.005, 0.036, 0.038),
        (0.40, 0.196, 0.615, -0.045, 0.031, 0.033),
        (0.60, 0.218, 0.655, -0.098, 0.025, 0.026),
        (0.80, 0.238, 0.698, -0.152, 0.018, 0.019),
        (0.92, 0.248, 0.728, -0.188, 0.011, 0.012),
        (0.98, 0.254, 0.744, -0.210, 0.006, 0.006),
    ]
    apex_tip = (0.258, 0.755, -0.225)

    def cross(a, b):
        return (a[1] * b[2] - a[2] * b[1],
                a[2] * b[0] - a[0] * b[2],
                a[0] * b[1] - a[1] * b[0])

    def unit(a):
        length = math.sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]) or 1.0
        return (a[0] / length, a[1] / length, a[2] / length)

    def add_flat_quad(p0, p1, p2, p3, slot):
        """Adds a flat quad (p0, p1, p2, p3) in CCW winding with computed normal."""
        v1 = (p1[0] - p0[0], p1[1] - p0[1], p1[2] - p0[2])
        v2 = (p2[0] - p0[0], p2[1] - p0[1], p2[2] - p0[2])
        normal = unit(cross(v1, v2))
        first = len(pos)
        for p in (p0, p1, p2, p3):
            pos.append(p)
            nrm.append(normal)
            uv.append(cell_uv(slot))
        tris.append((first, first + 1, first + 2))
        tris.append((first, first + 2, first + 3))

    def add_flat_tri(p0, p1, p2, slot):
        """Adds a flat triangle (p0, p1, p2) in CCW winding with computed normal."""
        v1 = (p1[0] - p0[0], p1[1] - p0[1], p1[2] - p0[2])
        v2 = (p2[0] - p0[0], p2[1] - p0[1], p2[2] - p0[2])
        normal = unit(cross(v1, v2))
        first = len(pos)
        for p in (p0, p1, p2):
            pos.append(p)
            nrm.append(normal)
            uv.append(cell_uv(slot))
        tris.append((first, first + 1, first + 2))

    # Build 5-vertex rings for each station
    rings = []
    for t, cx, cy, cz, w, h in stations:
        # 1. Top crest (Light plane)
        v_top = (cx - 0.003, cy + h * 1.05, cz - 0.006)
        # 2. Outer-upper accent crease (Violet accent)
        v_accent = (cx + w * 0.82, cy + h * 0.35, cz - 0.002)
        # 3. Outer lateral flank (Main charcoal plane)
        v_outer = (cx + w * 0.95, cy - h * 0.38, cz + 0.002)
        # 4. Bottom keel (Underside shadow)
        v_bottom = (cx + 0.002, cy - h * 0.92, cz + 0.005)
        # 5. Inner cranium-facing edge (Medial shadow)
        v_inner = (cx - w * 0.80, cy - h * 0.10, cz)
        rings.append((v_top, v_accent, v_outer, v_bottom, v_inner))

    # Connect adjacent rings with 5 faceted quad strips
    for i in range(len(rings) - 1):
        r0 = rings[i]
        r1 = rings[i + 1]
        top0, acc0, out0, bot0, inn0 = r0
        top1, acc1, out1, bot1, inn1 = r1

        # 1. Top Light Plane (HORN_LIGHT)
        add_flat_quad(inn0, inn1, top1, top0, HORN_LIGHT)
        # 2. Violet Accent Bevel Crease (HORN_PURPLE)
        add_flat_quad(top0, top1, acc1, acc0, HORN_PURPLE)
        # 3. Main Charcoal Lateral Flank (HORN_MAIN)
        add_flat_quad(acc0, acc1, out1, out0, HORN_MAIN)
        # 4. Bottom Keel Under-Shadow (HAIR)
        add_flat_quad(out0, out1, bot1, bot0, HAIR)
        # 5. Inner Cranium Medial-Shadow (HAIR)
        add_flat_quad(bot0, bot1, inn1, inn0, HAIR)

    # Apex tip triangles
    top_last, acc_last, out_last, bot_last, inn_last = rings[-1]
    add_flat_tri(inn_last, apex_tip, top_last, HORN_LIGHT)
    add_flat_tri(top_last, apex_tip, acc_last, HORN_PURPLE)
    add_flat_tri(acc_last, apex_tip, out_last, HORN_MAIN)
    add_flat_tri(out_last, apex_tip, bot_last, HAIR)
    add_flat_tri(bot_last, apex_tip, inn_last, HAIR)

    # Base socket collar cap embedding into skull at station 0
    top0, acc0, out0, bot0, inn0 = rings[0]
    add_flat_tri(top0, inn0, bot0, HAIR)
    add_flat_tri(top0, bot0, out0, HAIR)
    add_flat_tri(top0, out0, acc0, HAIR)



def _compact(pos, nrm, uv, tris):
    used = sorted({i for t in tris for i in t})
    remap = {i: k for k, i in enumerate(used)}
    return ([pos[i] for i in used], [nrm[i] for i in used], [uv[i] for i in used],
            [tuple(remap[i] for i in t) for t in tris])


# ---------------------------------------------------------------------------
# GEOMETRY GENERATION & CHAMFER
# ---------------------------------------------------------------------------
FRONT_IS_MINUS_Z = True
PANEL_PROUD = 0.0006

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
BEVEL_MAX = 0.060


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

    for name, bone, low, high, z_plane, face, rows in panels:
        j = BONE[bone]
        cols = len(rows[0])
        num_rows = len(rows)
        cell_x = (high[0] - low[0]) / cols
        cell_y = (high[1] - low[1]) / num_rows

        for r, row in enumerate(rows):
            for c, mark in enumerate(row):
                if mark not in PANEL_PALETTE:
                    continue

                slot = PANEL_PALETTE[mark]
                u, v = cell_uv(slot)
                layer = 2 if mark == "L" else 1
                offset = layer * PANEL_PROUD
                # Rows read top down, so row 0 is the TOP of the rectangle
                y0 = high[1] - cell_y * (r + 1)
                y1 = y0 + cell_y

                if face == "front":
                    # Screen Left (c=0) is low[0], Screen Right (c=cols-1) is high[0]
                    x0 = low[0] + cell_x * c
                    x1 = x0 + cell_x
                    z = -z_plane + offset
                    normal = (0.0, 0.0, 1.0)
                    p_bl = (x0, y0, z)
                    p_br = (x1, y0, z)
                    p_tr = (x1, y1, z)
                    p_tl = (x0, y1, z)
                else:  # face == "back"
                    # Screen Left (c=0) is high[0], Screen Right (c=cols-1) is low[0]
                    x0 = high[0] - cell_x * (c + 1)
                    x1 = high[0] - cell_x * c
                    z = -z_plane - offset
                    normal = (0.0, 0.0, -1.0)
                    p_bl = (x1, y0, z)
                    p_br = (x0, y0, z)
                    p_tr = (x0, y1, z)
                    p_tl = (x1, y1, z)

                first = len(pos)
                for p in (p_bl, p_br, p_tr, p_tl):
                    panel_indices.append(len(pos))
                    pos.append(p)
                    nrm.append(normal)
                    uv.append((u, v))
                    joints.append((j, 0, 0, 0))
                    weights.append((1.0, 0.0, 0.0, 0.0))
                idx += [first, first + 1, first + 2, first, first + 2, first + 3]

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
            pts_3d = [(p[0], p[1], 0.055 + offset) for p in pts_2d]
        elif face == "back":
            if signed_area_2d(pts_2d) > 0:
                pts_2d.reverse()
            normal = (0.0, 0.0, -1.0)
            pts_3d = [(p[0], p[1], -0.055 - offset) for p in pts_2d]
        elif face == "top":
            pts_3d = [(p[0], 0.348 + offset, -p[1]) for p in pts_2d]
            if signed_area_2d([(p[0], -p[1]) for p in pts_2d]) < 0:
                pts_3d.reverse()
            normal = (0.0, 1.0, 0.0)
        elif face == "bottom":
            pts_3d = [(p[0], 0.228 - offset, -p[1]) for p in pts_2d]
            if signed_area_2d([(p[0], -p[1]) for p in pts_2d]) > 0:
                pts_3d.reverse()
            normal = (0.0, -1.0, 0.0)
        elif face == "left":
            pts_3d = [(0.310 + offset, p[1], -p[0]) for p in pts_2d]
            if signed_area_2d(pts_2d) < 0:
                pts_3d.reverse()
            normal = (1.0, 0.0, 0.0)
        elif face == "right":
            pts_3d = [(-0.310 - offset, p[1], -p[0]) for p in pts_2d]
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
        length = (n[0] * n[0] + n[1] * n[1] + n[2] * n[2]) ** 0.5
        out.append(tuple(n[a] / length for a in range(3)) if length > 1e-6 else nrm[i])

    return out


# ---------------------------------------------------------------------------
# GLB READ / WRITE
# ---------------------------------------------------------------------------
COMPONENT = {5120: ("b", 1), 5121: ("B", 1), 5122: ("h", 2),
             5123: ("H", 2), 5125: ("I", 4), 5126: ("f", 4)}
COUNT = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}


def read_glb(path):
    with open(path, "rb") as handle:
        data = handle.read()

    offset, gltf, buffer = 12, None, None
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
    fmt, size = COMPONENT[acc["componentType"]]
    n = COUNT[acc["type"]]
    view = gltf["bufferViews"][acc["bufferView"]]
    start = view.get("byteOffset", 0) + acc.get("byteOffset", 0)
    stride = view.get("byteStride") or (size * n)

    return [struct.unpack("<" + fmt * n,
                          buffer[start + i * stride: start + i * stride + size * n])
            for i in range(acc["count"])]


def accessor_bytes(gltf, buffer, index):
    acc = gltf["accessors"][index]
    fmt, size = COMPONENT[acc["componentType"]]
    n = COUNT[acc["type"]]
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

    body = build_mesh(_family(BODY_BOXES, head=False), panels=_family_panels(BODY_PANELS),
                      decals=_family_decals(ARM_DECALS))
    head = build_mesh(_family(HEAD_BOXES, head=True, as_authored=DONOR_SPACE),
                      donor=_donor_head())

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

    head_reach = (CAST_MAX_HEIGHT - NOW_NECK) * 1.05

    for entry in (_family(BODY_BOXES, head=False)
                  + _family(HEAD_BOXES, head=True, as_authored=DONOR_SPACE)):
        name, bone, box_lo, box_hi, slot = entry[:5]
        origin = SKELETON[bone][1]
        reach = max(abs(box_lo[1] - origin), abs(box_hi[1] - origin))

        if reach > (head_reach if bone == "head" else 0.30):
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

    print(f"wrote {path}  ({total} bytes)")


if __name__ == "__main__":
    sys.exit(main())
