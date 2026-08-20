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
ROBE_GREEN    = 0   # Primary forest green robe and cape (#3d6335)
ROBE_DARK     = 1   # Deep olive/forest shadow & undershirt (#243e1f)
GOLD_TRIM     = 2   # Radiant gold collar trim, cape rune, wrist cuffs (#dfb248)
LEATHER_BROWN = 3   # Dark brown leather belt, pants, sleeves, hair fade (#482f1d)
BOOT_GREEN    = 4   # Boot upper green (#3d6335)
JADE_GEM      = 5   # Emerald/jade buckle core (#38b848)
HAIR          = 6   # Dark charcoal black spiky hair (#1a181e)
JADE_LIT      = 7   # Jade gemstone highlight (#68e878)
INK           = 8   # Face ink (lash lines, mouth, brows) (#1a1420)
SCAR_RED      = 9   # Deep crimson Karma marks & horn ridge bands (#8e2b1d)
EYE_GOLD      = 10  # Radiant glowing golden amber left eye (#ffd700)
SILVER        = 11  # Earring studs and metal belt rivets (#d4e2ec)
WHITE         = 12  # Crisp boot soles, eye sclera & glints (#f4faff)
SKIN          = 13  # Bronze warrior tan skin & horn stone body (#a8602c)
EYE_EMERALD   = 14  # Rich dark natural forest green right eye iris (#1e5c32)
SKIN_LIT      = 15  # Uniform skin tone for face/body (#a8602c)

PALETTE = {
    ROBE_GREEN:    "3d6335",   # Forest green robe and cape
    ROBE_DARK:     "243e1f",   # Deep olive/forest shadow & undershirt
    GOLD_TRIM:     "dfb248",   # Radiant gold trims, sashes, rune, cuffs
    LEATHER_BROWN: "482f1d",   # Warm dark brown leather & shaved undercut fade
    BOOT_GREEN:    "3d6335",   # Forest green boot upper
    JADE_GEM:      "38b848",   # Radiant emerald / jade belt gem
    HAIR:          "1a181e",   # Dark charcoal spiky hair & undercut
    JADE_LIT:      "68e878",   # Jade highlight sparkle
    INK:           "1a1420",   # Dark ink (lash lines, mouth, brows)
    SCAR_RED:      "8e2b1d",   # Deep crimson Karma marks & horn bands
    EYE_GOLD:      "ffd700",   # Radiant glowing gold left eye
    SILVER:        "d4e2ec",   # Silver earrings & hardware
    WHITE:         "f4faff",   # Boot sole tread, eye white & glints
    SKIN:          "a8602c",   # Warm bronze warrior caramel tan & stone horn
    EYE_EMERALD:   "1e5c32",   # Rich dark natural forest green right eye iris
    SKIN_LIT:      "a8602c",   # Uniform skin midtone for face/body
}

MAX_FACE_LUMINANCE = 0.30


# ---------------------------------------------------------------------------
# LEGS & COMBAT BOOTS
# ---------------------------------------------------------------------------
LEG_LEFT = [
    ("boot-sole-left",      "leg-left", (0.006, 0.000, -0.134), (0.158, 0.024, 0.082), WHITE),
    ("boot-upper-left",     "leg-left", (0.014, 0.024, -0.126), (0.152, 0.068, 0.076), BOOT_GREEN),
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
    ("boot-upper-right",     "leg-right", (-0.152, 0.024, -0.126), (-0.014, 0.068, 0.076), BOOT_GREEN),
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
    ("robe-body",             "torso", (-0.124, 0.232, -0.092), (0.124, 0.445, 0.092), ROBE_GREEN),
    
    # 🥋 LAYERED WARRIOR CHEST, ROBE LAPELS & GOLD FROG FASTENERS
    # Exposed upper chest skin & Karma lightning trail
    ("neck-chest-skin",       "torso", (-0.035, 0.380, -0.095), (0.035, 0.445, -0.080), SKIN_LIT),
    ("karma-neck-slash",      "torso", (0.008, 0.380, -0.098), (0.028, 0.445, -0.082), SCAR_RED),
    ("karma-chest-branch",    "torso", (0.020, 0.395, -0.098), (0.055, 0.425, -0.084), SCAR_RED),
    
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
    
    # Leather Belt
    ("belt-tier-lower",       "torso", (-0.134, 0.272, -0.104), (0.134, 0.304, 0.104), LEATHER_BROWN),
    ("belt-tier-upper",       "torso", (-0.134, 0.306, -0.104), (0.134, 0.338, 0.104), LEATHER_BROWN),
    ("belt-seam-middle",      "torso", (-0.136, 0.302, -0.106), (0.136, 0.308, 0.106), LEATHER_BROWN),
    
    # Jade Medallion Buckle
    ("medallion-plate-gold",  "torso", (-0.048, 0.272, -0.118), (0.048, 0.338, -0.096), GOLD_TRIM),
    ("medallion-rim-top",     "torso", (-0.038, 0.332, -0.122), (0.038, 0.344, -0.096), GOLD_TRIM),
    ("medallion-rim-bot",     "torso", (-0.038, 0.266, -0.122), (0.038, 0.278, -0.096), GOLD_TRIM),
    ("medallion-rim-left",    "torso", (0.042, 0.280, -0.122), (0.054, 0.330, -0.096), GOLD_TRIM),
    ("medallion-rim-right",   "torso", (-0.054, 0.280, -0.122), (-0.042, 0.330, -0.096), GOLD_TRIM),
    ("jade-gem-core",         "torso", (-0.030, 0.282, -0.124), (0.030, 0.328, -0.106), JADE_GEM),
    ("jade-gem-lit",          "torso", (-0.015, 0.294, -0.128), (0.015, 0.316, -0.112), JADE_LIT),
    
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
]


# ---------------------------------------------------------------------------
# ARMS, SLEEVES & FULL 360° KAWAKI KARMA BARE ARM (+X)
# ---------------------------------------------------------------------------
ARM_LEFT = [
    # 💪 BARE MUSCULAR SKIN ARM (+X) - Smooth tapered muscular arm
    ("arm-shoulder-l",        "arm-left", (0.0999, 0.355, -0.070), (0.185, 0.475, 0.080), SKIN_LIT),
    ("arm-upper-l",           "arm-left", (0.170, 0.345, -0.060), (0.240, 0.465, 0.070), SKIN_LIT),
    ("arm-forearm-l",         "arm-left", (0.230, 0.340, -0.050), (0.290, 0.460, 0.058), SKIN_LIT),
    ("arm-wrist-l",           "arm-left", (0.275, 0.3383, -0.032), (0.315, 0.4617, 0.042), SKIN_LIT),
    
    # ⚡ SHOULDER / DELTOID KARMA SUN (Full 360° Wrap Proud of Skin)
    ("karma-deltoid-top",     "arm-left", (0.115, 0.460, -0.045), (0.180, 0.485, 0.045), SCAR_RED),
    ("karma-deltoid-disc",    "arm-left", (0.135, 0.390, -0.045), (0.188, 0.470, 0.045), SCAR_RED),
    # Front Shoulder Sun Arc
    ("karma-sun-front",       "arm-left", (0.125, 0.405, -0.078), (0.175, 0.455, -0.060), SCAR_RED),
    # Back Sun (Proud of skin z: 0.080)
    ("karma-shoulder-sun-b",  "arm-left", (0.120, 0.395, 0.068), (0.178, 0.465, 0.088), SCAR_RED),
    ("karma-shoulder-core-b", "arm-left", (0.138, 0.418, 0.070), (0.160, 0.442, 0.090), GOLD_TRIM),
    
    # ⚡ BICEP / TRICEP / OUTER ARM FLAME TENDRILS
    # Front Bicep Flame Streams
    ("karma-stream-f1",       "arm-left", (0.155, 0.355, -0.068), (0.215, 0.420, -0.052), SCAR_RED),
    ("karma-stream-f2",       "arm-left", (0.180, 0.380, -0.068), (0.230, 0.435, -0.052), SCAR_RED),
    # Outer Arm Connector
    ("karma-stream-outer",    "arm-left", (0.175, 0.410, -0.030), (0.235, 0.460, 0.030), SCAR_RED),
    # Back Tricep Streams (Proud of skin z: 0.070)
    ("karma-flame-b1",        "arm-left", (0.155, 0.345, 0.058), (0.200, 0.410, 0.078), SCAR_RED),
    ("karma-flame-b2",        "arm-left", (0.180, 0.360, 0.058), (0.225, 0.420, 0.078), SCAR_RED),
    ("karma-flame-b3-elbow",  "arm-left", (0.205, 0.370, 0.058), (0.245, 0.435, 0.078), SCAR_RED),
    
    # ⚡ FOREARM CONTINUOUS TAPERED CHEVRON SPEAR WITH EMBEDDED GOLD CORE GEM
    # Front Forearm Tapered Spear Sheath (Connected continuous arrow pointing to elbow)
    ("karma-spear-outer-f",   "arm-left", (0.225, 0.345, -0.058), (0.285, 0.445, -0.042), SCAR_RED),
    # 💎 UNIFIED SINGLE SOLID GOLDEN KARMA CORE GEM (One single solid piece, NOT split!)
    ("karma-spear-gem-f",     "arm-left", (0.242, 0.370, -0.062), (0.268, 0.420, -0.040), GOLD_TRIM),
    
    # Back Forearm Spear (Proud of skin z: 0.058)
    ("karma-forearm-spear-b", "arm-left", (0.225, 0.345, 0.048), (0.285, 0.445, 0.068), SCAR_RED),
    ("karma-forearm-core-b",  "arm-left", (0.240, 0.370, 0.050), (0.270, 0.420, 0.070), GOLD_TRIM),
    
    # Outer Forearm Ridge & Wrist Wrap Bands
    ("karma-forearm-ridge",   "arm-left", (0.250, 0.360, -0.025), (0.294, 0.440, 0.025), SCAR_RED),
    ("karma-wrist-band-top",  "arm-left", (0.282, 0.440, -0.056), (0.302, 0.462, -0.042), SCAR_RED),
    ("karma-wrist-band-bot",  "arm-left", (0.282, 0.338, -0.056), (0.302, 0.360, -0.042), SCAR_RED),
    
    # ⚡ HAND KARMA SEALS (Palm & Backhand Circular Seal with Knuckle Marks)
    ("hand-left",             "arm-left", (0.295, 0.3383, -0.024), (0.3836, 0.4617, 0.036), SKIN_LIT),
    ("karma-palm-seal",       "arm-left", (0.305, 0.370, -0.030), (0.345, 0.420, -0.018), SCAR_RED),
    ("karma-backhand-seal",   "arm-left", (0.300, 0.360, 0.028), (0.350, 0.440, 0.044), SCAR_RED),
    ("karma-backhand-gold",   "arm-left", (0.315, 0.385, 0.030), (0.335, 0.415, 0.046), GOLD_TRIM),
    ("karma-knuckle-mark",    "arm-left", (0.355, 0.375, 0.018), (0.375, 0.425, 0.042), SCAR_RED),
]

ARM_RIGHT = [
    # Clothed warrior arm (-X) with sleeve and gold wrist cuff
    ("sleeve-right",          "arm-right", (-0.245, 0.330, -0.068), (-0.0999, 0.470, 0.084), LEATHER_BROWN),
    ("sleeve-shoulder-r",     "arm-right", (-0.190, 0.360, -0.075), (-0.0999, 0.482, 0.090), LEATHER_BROWN),
    ("wrist-cuff-right",      "arm-right", (-0.278, 0.322, -0.074), (-0.240, 0.478, 0.090), GOLD_TRIM),
    ("hand-right",            "arm-right", (-0.3836, 0.3383, -0.020), (-0.275, 0.4617, 0.038), SKIN_LIT),
]


# ---------------------------------------------------------------------------
# HEAD, FAUXHAWK QUIFF, SHAVED FADE & KAWAKI OTSUTSUKI HORN (+X)
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
    
    # 🪨 SLEEK ORGANIC OTSUTSUKI HORN (+X temple, dynamic lateral-flare parabolic crescent arc)
    # 1. Brow / Supraorbital Root (Emerges directly above left eyebrow & temple plate)
    ("horn-root-brow",        "head", (0.125, 0.515, 0.080), (0.172, 0.575, 0.155), SKIN),
    ("horn-root-temple",      "head", (0.138, 0.540, 0.050), (0.185, 0.605, 0.135), SKIN),
    
    # 2. Dynamic Lateral Flare & Parabolic Crescent Tiers (Curving outward +X then hooking inward -X)
    ("horn-tier-1",           "head", (0.148, 0.575, 0.025), (0.196, 0.640, 0.115), SKIN),
    ("horn-tier-2",           "head", (0.158, 0.625, -0.010), (0.206, 0.690, 0.080), SKIN),
    ("horn-tier-3",           "head", (0.152, 0.675, -0.050), (0.200, 0.742, 0.035), SKIN),
    ("horn-tier-4",           "head", (0.142, 0.705, -0.095), (0.188, 0.760, -0.010), SKIN),
    ("horn-tier-5",           "head", (0.134, 0.695, -0.140), (0.174, 0.745, -0.055), SKIN),
    ("horn-tier-6",           "head", (0.128, 0.665, -0.180), (0.162, 0.715, -0.095), SKIN),
    
    # 3. Sharp Chisel Tip (Hooking downward/backward along skull contour)
    ("horn-tip",              "head", (0.122, 0.630, -0.215), (0.152, 0.675, -0.140), SKIN),
    
    # ⚡ 3 RAZOR-THIN ETCHED CRIMSON KARMA CARAPACE RINGS (Clean engraved grooves, NO fat blotches!)
    ("karma-ring-rise",       "head", (0.165, 0.645, 0.010), (0.208, 0.660, 0.045), SCAR_RED),
    ("karma-ring-crown",      "head", (0.148, 0.725, -0.065), (0.192, 0.740, -0.030), SCAR_RED),
    ("karma-ring-hook",       "head", (0.136, 0.685, -0.145), (0.176, 0.700, -0.110), SCAR_RED),
    
    # ⚡ DUAL SLEEK TEMPLE KARMA STREAM LINES (Connecting facial scar to horn root)
    ("karma-stream-line-top", "head", (0.135, 0.605, 0.075), (0.185, 0.620, 0.160), SCAR_RED),
    ("karma-stream-line-bot", "head", (0.135, 0.538, 0.085), (0.182, 0.552, 0.160), SCAR_RED),
    
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


BODY_BOXES = (LEG_LEFT + LEG_RIGHT
              + TORSO
              + ARM_LEFT + ARM_RIGHT)
HEAD_BOXES = HEAD

# ---------------------------------------------------------------------------
# § THE DONATED HEAD, SCAR & HETEROCHROMIA EYES
# ---------------------------------------------------------------------------
DONOR_SKULL = "Assets/TumbangPreso/Art/characters/persons/character-male-d.glb"
SKULL_SLOTS = {15: SKIN_LIT, 8: None}

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
    """Builds the donor skull with Kawaki Karma slash pattern, sleek anime heterochromia eyes, and smirk mouth."""
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

    # 3. ⚔️ KAWAKI KARMA FACIAL SLASH & UNDERCUT TEMPLE (media_1787164289261.png)
    # Natural Stepped Buzzcut Undercut Hairline on Temple (+X)
    add_quad([(0.065, 0.605), (0.065, 0.665), (0.170, 0.665), (0.170, 0.605)],
             LEATHER_BROWN, PANEL_PROUD * 1.5)
    add_quad([(0.095, 0.535), (0.095, 0.605), (0.170, 0.605), (0.170, 0.535)],
             LEATHER_BROWN, PANEL_PROUD * 1.5)
    add_quad([(0.130, 0.450), (0.130, 0.535), (0.170, 0.535), (0.170, 0.450)],
             LEATHER_BROWN, PANEL_PROUD * 1.5)
    add_quad([(0.150, 0.405), (0.150, 0.450), (0.170, 0.450), (0.170, 0.405)],
             LEATHER_BROWN, PANEL_PROUD * 1.5)

    # Main Flowing Karma Facial Ribbon (Continuous S-curve from high forehead to chin)
    add_quad([(0.032, 0.585), (0.032, 0.655), (0.056, 0.655), (0.056, 0.585)],
             SCAR_RED, PANEL_PROUD * 2.2)
    add_quad([(0.036, 0.525), (0.036, 0.585), (0.062, 0.585), (0.062, 0.525)],
             SCAR_RED, PANEL_PROUD * 2.2)
    add_quad([(0.048, 0.485), (0.048, 0.525), (0.074, 0.525), (0.074, 0.485)],
             SCAR_RED, PANEL_PROUD * 2.2)
    add_quad([(0.078, 0.445), (0.078, 0.488), (0.106, 0.488), (0.100, 0.445)],
             SCAR_RED, PANEL_PROUD * 2.2)
    add_quad([(0.066, 0.385), (0.078, 0.448), (0.102, 0.448), (0.088, 0.385)],
             SCAR_RED, PANEL_PROUD * 2.2)
    add_quad([(0.048, 0.335), (0.066, 0.390), (0.088, 0.390), (0.068, 0.335)],
             SCAR_RED, PANEL_PROUD * 2.2)

    # ⚡ DUAL SLEEK TEMPLE SURGE LINES (Connecting facial ribbon directly into temple stream lines)
    # Upper Stream Line (Connecting high forehead ribbon at y=0.605 to side stream at y=0.615)
    add_quad([(0.040, 0.600), (0.040, 0.615), (0.180, 0.625), (0.180, 0.610)],
             SCAR_RED, PANEL_PROUD * 2.2)
    # Lower Stream Line (Connecting mid-face ribbon at y=0.540 to side stream at y=0.546)
    add_quad([(0.048, 0.535), (0.048, 0.549), (0.180, 0.558), (0.180, 0.544)],
             SCAR_RED, PANEL_PROUD * 2.2)

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
    # Crimson Karma Eye Frame Bed (Surrounding the black eye)
    add_quad([(0.030, 0.456), (0.030, 0.498), (0.118, 0.504), (0.118, 0.456)],
             SCAR_RED, PANEL_PROUD * 2.3)
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

    return _compact(pos, nrm, uv, tris)


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


def build_mesh(boxes, panels=(), donor=None):
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

    body = build_mesh(_family(BODY_BOXES, head=False))
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
