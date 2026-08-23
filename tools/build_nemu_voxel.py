"""Builds team-nemu.glb (the Sleepy Ghost / Ofuda Spirit Girl) and person_team-nemu.tres.

    python tools/build_nemu_voxel.py

Master Reference: media_1787310543564.png / media_1787312793831.png
"""
import math
import json
import os
import struct
import sys

BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"

OUT = "Assets/TumbangPreso/Art/characters/persons/team-nemu.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-nemu.tres"
ROSTER_OUT = "Assets/TumbangPreso/Resources/Roster/person_nemu.asset"

BONE = {"root": 0, "leg-left": 1, "leg-right": 2, "torso": 3,
        "arm-left": 4, "arm-right": 5, "head": 6}

PARENT = {"leg-left": "root", "leg-right": "root", "torso": "root",
          "arm-left": "torso", "arm-right": "torso", "head": "torso"}

# ---------------------------------------------------------------------------
# PETITE MOE / CHIBI PROPORTIONS SKELETON (0.5980m AUTHORED HEIGHT)
# ---------------------------------------------------------------------------
WAS_HIPS, WAS_SHOULDER, WAS_NECK, WAS_TOP = 0.170, 0.250, 0.280, 0.5980
NOW_HIPS, NOW_SHOULDER, NOW_NECK, NOW_TOP = 0.170, 0.250, 0.280, 0.5980

HEAD_GROWTH = 1.0
CAST_MIN_HEIGHT, CAST_MAX_HEIGHT = 0.5900, 0.7928

SKELETON = {
    "root":      (0.0,      0.0,          0.0),
    "leg-left":  (0.070,    NOW_HIPS,     -0.02875),
    "leg-right": (-0.070,   NOW_HIPS,     -0.02875),
    "torso":     (0.0,      NOW_HIPS,     -0.02875),
    "arm-left":  (0.092,    NOW_SHOULDER, -0.01725),
    "arm-right": (-0.092,   NOW_SHOULDER, -0.01725),
    "head":      (0.0,      NOW_NECK,     -0.00236),
}


def cell_uv(slot):
    """Atlas cell for a palette slot."""
    col = 2 * (slot % 8) + 1
    row = 9 if slot < 8 else 13
    return ((col + 0.5) / 16.0, (row + 0.5) / 16.0)


# ---------------------------------------------------------------------------
# 16-COLOR PALETTE DEFINITION
# ---------------------------------------------------------------------------
HOODIE_DARK    = 0   # Deep midnight violet hoodie body & skirt (#231c34)
HOODIE_SHADOW  = 1   # Rich dark purple body creases / seam shadows (#181224)
LAVENDER_GLOW  = 2   # Vibrant lavender crescent moon, back eye, cuff trim (#aa5cf0)
LAVENDER_PALE  = 3   # Soft pastel lilac highlights & graphic inner glints (#d09af8)
SHOE_PURPLE    = 4   # Purple shoe base (#382856)
OFUDA_PURPLE   = 5   # Lavender ofuda tag cap & kanji strokes (#8a3cd0)
HAIR_DARK      = 6   # Deep midnight navy/violet hair (#1d182e)
HAIR_HIGHLIGHT = 7   # Soft purple hair sheen / strand highlights (#32284a)
INK            = 8   # Solid dark purple-black ink (sleepy eyes, mouth) (#120e1c)
OFUDA_WHITE    = 9   # Crisp paper white for head and hip ofuda tags (#f4faff)
GRAPHIC_ACCENT = 10  # Moon eye center dot & stitch crosses (#c87af8)
SILVER         = 11  # Small metal pins & tag rivets (#d0d8e8)
WHITE          = 12  # Clean shoe sole rims (#f4faff)
SKIN           = 13  # Warm golden peach / honey-tan skin (#e0af84)
SKIN_DARK      = 14  # Warm soft honey blush (#d69974)
SKIN_LIT       = 15  # Warm golden peach skin tone (#e0af84)

PALETTE = {
    HOODIE_DARK:    "231c34",   # Deep midnight violet hoodie body & skirt
    HOODIE_SHADOW:  "181224",   # Rich dark purple body creases / seam shadows
    LAVENDER_GLOW:  "aa5cf0",   # Vibrant lavender crescent moon, back eye, cuff trim
    LAVENDER_PALE:  "d09af8",   # Soft pastel lilac highlights & graphic inner glints
    SHOE_PURPLE:    "382856",   # Purple shoe base
    OFUDA_PURPLE:   "8a3cd0",   # Lavender ofuda tag cap & kanji strokes
    HAIR_DARK:      "1d182e",   # Deep midnight navy/violet hair
    HAIR_HIGHLIGHT: "32284a",   # Soft purple hair sheen / strand highlights
    INK:            "120e1c",   # Solid dark purple-black ink (sleepy eyes, mouth)
    OFUDA_WHITE:    "f4faff",   # Crisp paper white for head and hip ofuda tags
    GRAPHIC_ACCENT: "c87af8",   # Moon eye center dot & stitch crosses
    SILVER:         "d0d8e8",   # Small metal pins & tag rivets
    WHITE:          "f4faff",   # Clean shoe sole rims
    SKIN:           "e0af84",   # Warm golden peach / honey-tan skin
    SKIN_DARK:      "d69974",   # Warm soft honey blush
    SKIN_LIT:       "e0af84",   # Warm golden peach skin tone
}

MAX_FACE_LUMINANCE = 0.30

# ---------------------------------------------------------------------------
# SLENDER PETITE MOE LEGS & CHUNKY STREETWEAR SNEAKERS
# (Calibrated to fold cleanly in crouch without punching into chest)
# ---------------------------------------------------------------------------
LEG_LEFT = [
    # Clean White Sneaker Sole Rim Slab (Single solid box, NO coplanar overlapping tread!)
    ("shoe-sole-left",         "leg-left", (0.020, 0.000, -0.100), (0.120, 0.022, 0.060), WHITE),

    # Chunky Purple Sneaker Body
    ("shoe-upper-left",        "leg-left", (0.024, 0.022, -0.095), (0.116, 0.058, 0.055), SHOE_PURPLE),
    ("shoe-toe-left",          "leg-left", (0.024, 0.022, -0.102), (0.116, 0.048, -0.055), SHOE_PURPLE),
    ("shoe-heel-left",         "leg-left", (0.024, 0.022, 0.020),  (0.116, 0.062, 0.058), SHOE_PURPLE),

    # Thick Lavender Ankle Strap / Collar
    ("shoe-strap-left",        "leg-left", (0.022, 0.052, -0.080), (0.118, 0.074, 0.048), LAVENDER_GLOW),
    ("shoe-strap-accent-l",    "leg-left", (0.024, 0.058, -0.084), (0.116, 0.070, -0.065), LAVENDER_PALE),

    # Slender Porcelain Skin Calf (Tucks cleanly under hoodie skirt hem)
    ("leg-skin-left",          "leg-left", (0.042, 0.072, -0.055), (0.098, 0.136, 0.005), SKIN),
    ("leg-shadow-left",        "leg-left", (0.045, 0.115, -0.050), (0.095, 0.136, 0.000), SKIN_DARK),
]

LEG_RIGHT = [
    # Clean White Sneaker Sole Rim Slab (Single solid box, NO coplanar overlapping tread!)
    ("shoe-sole-right",        "leg-right", (-0.120, 0.000, -0.100), (-0.020, 0.022, 0.060), WHITE),

    # Chunky Purple Sneaker Body
    ("shoe-upper-right",       "leg-right", (-0.116, 0.022, -0.095), (-0.024, 0.058, 0.055), SHOE_PURPLE),
    ("shoe-toe-right",         "leg-right", (-0.116, 0.022, -0.102), (-0.024, 0.048, -0.055), SHOE_PURPLE),
    ("shoe-heel-right",        "leg-right", (-0.116, 0.022, 0.020),  (-0.024, 0.062, 0.058), SHOE_PURPLE),

    # Thick Lavender Ankle Strap / Collar
    ("shoe-strap-right",       "leg-right", (-0.118, 0.052, -0.080), (-0.022, 0.074, 0.048), LAVENDER_GLOW),
    ("shoe-strap-accent-r",    "leg-right", (-0.116, 0.058, -0.084), (-0.024, 0.070, -0.065), LAVENDER_PALE),

    # Slender Porcelain Skin Calf (Tucks cleanly under hoodie skirt hem)
    ("leg-skin-right",         "leg-right", (-0.098, 0.072, -0.055), (-0.042, 0.136, 0.005), SKIN),
    ("leg-shadow-right",       "leg-right", (-0.095, 0.115, -0.050), (-0.045, 0.136, 0.000), SKIN_DARK),
]

# ---------------------------------------------------------------------------
# OVERSIZED A-LINE STREETWEAR HOODIE TORSO & HIP TALISMAN
# ---------------------------------------------------------------------------
TORSO = [
    # 1. Main Continuous A-Line Torso Body (Clean flat front plane at z = -0.096)
    ("hoodie-core-main",       "torso", (-0.112, 0.135, -0.096), (0.112, 0.285, 0.096), HOODIE_DARK),
    ("hoodie-core-crease",     "torso", (-0.108, 0.130, -0.090), (0.108, 0.155, 0.090), HOODIE_SHADOW),

    # 2. Flared Lower Hem / Sweater-Dress Drape (Overlaps leg top at y=0.125-0.160)
    ("hoodie-skirt-hem",       "torso", (-0.135, 0.122, -0.102), (0.135, 0.165, 0.102), HOODIE_DARK),
    ("hoodie-skirt-shadow",    "torso", (-0.130, 0.118, -0.098), (0.130, 0.130, 0.098), HOODIE_SHADOW),
    ("hoodie-skirt-stripe",    "torso", (-0.138, 0.126, -0.105), (0.138, 0.146, 0.105), LAVENDER_GLOW),

    # 3. Puffy Rounded Cowl Collar Wrap (Wrapped in front of face/mouth in Z)
    ("hoodie-collar-main",     "torso", (-0.118, 0.260, -0.128), (0.118, 0.298, 0.105), HOODIE_DARK),
    ("hoodie-collar-front",    "torso", (-0.095, 0.265, -0.134), (0.095, 0.298, -0.080), HOODIE_DARK),
    ("hoodie-collar-rim",      "torso", (-0.085, 0.280, -0.136), (0.085, 0.298, -0.110), HOODIE_SHADOW),
    ("hoodie-collar-shadow",   "torso", (-0.065, 0.268, -0.132), (0.065, 0.288, -0.095), HOODIE_SHADOW),
    ("hoodie-collar-pearl",    "torso", (-0.015, 0.265, -0.148), (0.015, 0.295, -0.126), LAVENDER_GLOW),
    ("hoodie-collar-glint",    "torso", (-0.007, 0.275, -0.152), (0.007, 0.288, -0.136), LAVENDER_PALE),
]

# ---------------------------------------------------------------------------
# OVERSIZED STEPPED FLARED BELL SLEEVES WITH HOLLOW CUFFS & TUCKED HANDS
# ---------------------------------------------------------------------------
ARM_LEFT = [
    # 1. Slouchy Dropped-Shoulder Capelet (High shoulder zone)
    ("sleeve-shoulder-l",      "arm-left", (0.060, 0.200, -0.078), (0.130, 0.285, 0.078), HOODIE_DARK),

    # 2. Mid-Sleeve Dropped Puffy Box (Stepped downward & outward)
    ("sleeve-bell-mid-l",      "arm-left", (0.110, 0.160, -0.092), (0.180, 0.270, 0.092), HOODIE_DARK),
    ("sleeve-bell-mid-shad-l", "arm-left", (0.115, 0.150, -0.088), (0.175, 0.175, 0.088), HOODIE_SHADOW),

    # 3. Flared Bell Sleeve Outer Body (Puffy flare, calibrated proportions)
    ("sleeve-cuff-outer-l",    "arm-left", (0.160, 0.110, -0.102), (0.240, 0.260, 0.102), HOODIE_DARK),
    ("sleeve-cuff-under-l",    "arm-left", (0.165, 0.100, -0.095), (0.238, 0.125, 0.095), HOODIE_SHADOW),

    # 4. Lavender Cuff Border Band (Clean solid frame matching reference)
    ("sleeve-cuff-stripe-l",   "arm-left", (0.220, 0.105, -0.106), (0.245, 0.265, 0.106), LAVENDER_GLOW),

    # 5. Dark Hollow Cuff Interior (Single clean recess plate, zero z-fighting)
    ("sleeve-cuff-interior-l", "arm-left", (0.238, 0.118, -0.092), (0.246, 0.252, 0.092), HOODIE_SHADOW),

    # 6. Cute Tucked Hand (Peeking horizontally at cuff side - inside vertical sleeve span)
    ("hand-palm-left",         "arm-left", (0.236, 0.160, -0.015), (0.268, 0.210, 0.025), SKIN),
    ("hand-fingers-left",      "arm-left", (0.264, 0.165, -0.010), (0.282, 0.205, 0.020), SKIN),
    ("hand-fingers-tip-l",     "arm-left", (0.278, 0.170, -0.006), (0.288, 0.200, 0.016), SKIN_DARK),
]

ARM_RIGHT = [
    # 1. Slouchy Dropped-Shoulder Capelet (High shoulder zone)
    ("sleeve-shoulder-r",      "arm-right", (-0.130, 0.200, -0.078), (-0.060, 0.285, 0.078), HOODIE_DARK),

    # 2. Mid-Sleeve Dropped Puffy Box (Stepped downward & outward)
    ("sleeve-bell-mid-r",      "arm-right", (-0.180, 0.160, -0.092), (-0.110, 0.270, 0.092), HOODIE_DARK),
    ("sleeve-bell-mid-shad-r", "arm-right", (-0.175, 0.150, -0.088), (-0.115, 0.175, 0.088), HOODIE_SHADOW),

    # 3. Flared Bell Sleeve Outer Body (Puffy flare, calibrated proportions)
    ("sleeve-cuff-outer-r",    "arm-right", (-0.240, 0.110, -0.102), (-0.160, 0.260, 0.102), HOODIE_DARK),
    ("sleeve-cuff-under-r",    "arm-right", (-0.238, 0.100, -0.095), (-0.165, 0.125, 0.095), HOODIE_SHADOW),

    # 4. Lavender Cuff Border Band (Clean solid frame matching reference)
    ("sleeve-cuff-stripe-r",   "arm-right", (-0.245, 0.105, -0.106), (-0.220, 0.265, 0.106), LAVENDER_GLOW),

    # 5. Dark Hollow Cuff Interior (Single clean recess plate, zero z-fighting)
    ("sleeve-cuff-interior-r", "arm-right", (-0.246, 0.118, -0.092), (-0.238, 0.252, 0.092), HOODIE_SHADOW),

    # 6. Cute Tucked Hand (Peeking horizontally at cuff side - inside vertical sleeve span)
    ("hand-palm-right",        "arm-right", (-0.268, 0.160, -0.015), (-0.236, 0.210, 0.025), SKIN),
    ("hand-fingers-right",     "arm-right", (-0.282, 0.165, -0.010), (-0.264, 0.205, 0.020), SKIN),
    ("hand-fingers-tip-r",     "arm-right", (-0.288, 0.170, -0.006), (-0.278, 0.200, 0.016), SKIN_DARK),
]

# ---------------------------------------------------------------------------
# HEAD: WIDE CHIBI FACE, BOB HAIR, SINGLE SOLID SLEEPY EYE BARS (-   -)
# ---------------------------------------------------------------------------
HEAD = [
    # 0. CUTE WIDE CHIBI PALE PORCELAIN FACE BASE
    ("face-core",              "head", (-0.125, 0.285, -0.1140), (0.125, 0.460, 0.0900), SKIN),
    ("face-chin-taper",        "head", (-0.100, 0.275, -0.1000), (0.100, 0.295, 0.0750), SKIN),

    # 0.1 ICONIC CLEAN SLEEPY SPIRIT EYES (-   -) (Single crisp flat solid bar, NO double lines)
    ("eye-sleepy-left",        "head", (0.024, 0.320, -0.1142), (0.092, 0.355, -0.1130), INK),
    ("eye-sleepy-right",       "head", (-0.092, 0.320, -0.1142), (-0.024, 0.355, -0.1130), INK),

    # 1. COMPACT HAIR CROWN DOME & SMOOTH SILHOUETTE
    ("hair-crown-core",        "head", (-0.142, 0.400, -0.1160), (0.142, 0.585, 0.1280), HAIR_DARK),
    ("hair-crown-top-apex",    "head", (-0.120, 0.570, -0.0950), (0.120, 0.598, 0.1050), HAIR_DARK),
    ("hair-crown-sheen",       "head", (-0.115, 0.560, -0.0400), (0.115, 0.595, 0.0400), HAIR_HIGHLIGHT),
    ("hair-crown-side-l",      "head", (0.124, 0.380, -0.0920), (0.145, 0.575, 0.1120), HAIR_DARK),
    ("hair-crown-side-r",      "head", (-0.145, 0.380, -0.0920), (-0.124, 0.575, 0.1120), HAIR_DARK),

    # 2. STRAIGHT HIME-CUT BLUNT BANGS (4 Chunky Strands, Bevel Sheen & Gap Dividers)
    ("hair-bangs-brow-base",   "head", (-0.136, 0.365, -0.1280), (0.136, 0.575, -0.0950), HAIR_DARK),
    ("hair-bangs-brow-bevel",  "head", (-0.130, 0.390, -0.1360), (0.130, 0.555, -0.1220), HAIR_HIGHLIGHT),
    ("hair-strand-outer-l",    "head", (0.066, 0.348, -0.1360), (0.130, 0.450, -0.1180), HAIR_DARK),
    ("hair-strand-outer-l-top","head", (0.070, 0.354, -0.1400), (0.124, 0.445, -0.1280), HAIR_HIGHLIGHT),
    ("hair-strand-mid-l",      "head", (0.004, 0.358, -0.1380), (0.062, 0.450, -0.1200), HAIR_DARK),
    ("hair-strand-mid-l-top",  "head", (0.008, 0.364, -0.1420), (0.058, 0.445, -0.1300), HAIR_HIGHLIGHT),
    ("hair-strand-mid-r",      "head", (-0.062, 0.358, -0.1380), (-0.004, 0.450, -0.1200), HAIR_DARK),
    ("hair-strand-mid-r-top",  "head", (-0.058, 0.364, -0.1420), (-0.008, 0.445, -0.1300), HAIR_HIGHLIGHT),
    ("hair-strand-outer-r",    "head", (-0.130, 0.348, -0.1360), (-0.066, 0.450, -0.1180), HAIR_DARK),
    ("hair-strand-outer-r-top","head", (-0.124, 0.354, -0.1400), (-0.070, 0.445, -0.1280), HAIR_HIGHLIGHT),
    ("hair-bangs-gap-l",       "head", (0.062, 0.350, -0.1360), (0.066, 0.445, -0.1160), HOODIE_SHADOW),
    ("hair-bangs-gap-mid",     "head", (-0.004, 0.360, -0.1360), (0.004, 0.445, -0.1160), HOODIE_SHADOW),
    ("hair-bangs-gap-r",       "head", (-0.066, 0.350, -0.1360), (-0.062, 0.445, -0.1160), HOODIE_SHADOW),

    # 3. LONG STRAIGHT SIDE LOCKS (Framing Cheeks & Shoulders)
    ("hair-sidelock-main-l",   "head", (0.118, 0.200, -0.1220), (0.146, 0.455, 0.0100), HAIR_DARK),
    ("hair-sidelock-front-l",  "head", (0.114, 0.200, -0.1320), (0.142, 0.440, -0.0760), HAIR_DARK),
    ("hair-sidelock-highlight-l","head",(0.120, 0.220, -0.1360), (0.144, 0.425, -0.0900), HAIR_HIGHLIGHT),
    ("hair-sidelock-main-r",   "head", (-0.146, 0.200, -0.1220), (-0.118, 0.455, 0.0100), HAIR_DARK),
    ("hair-sidelock-front-r",  "head", (-0.142, 0.200, -0.1320), (-0.114, 0.440, -0.0760), HAIR_DARK),
    ("hair-sidelock-highlight-r","head",(-0.144, 0.220, -0.1360), (-0.120, 0.425, -0.0900), HAIR_HIGHLIGHT),

    # 4. SHORT NAPE BACK HAIR DRAPE
    ("hair-back-tier1-c",      "head", (-0.138, 0.380, 0.0850), (0.138, 0.490, 0.1380), HAIR_DARK),
    ("hair-back-tier1-l",      "head", (0.092, 0.380, 0.0750), (0.144, 0.490, 0.1340), HAIR_DARK),
    ("hair-back-tier1-r",      "head", (-0.144, 0.380, 0.0750), (-0.092, 0.490, 0.1340), HAIR_DARK),
    ("hair-back-tier2-c",      "head", (-0.132, 0.315, 0.0900), (0.132, 0.395, 0.1380), HAIR_DARK),
    ("hair-back-tier2-l",      "head", (0.088, 0.315, 0.0800), (0.138, 0.395, 0.1340), HAIR_DARK),
    ("hair-back-tier2-r",      "head", (-0.138, 0.315, 0.0800), (-0.088, 0.395, 0.1340), HAIR_DARK),
    ("hair-back-seam-1",       "head", (-0.048, 0.315, 0.0920), (-0.038, 0.475, 0.1400), HAIR_HIGHLIGHT),
    ("hair-back-seam-2",       "head", (0.038, 0.315, 0.0920), (0.048, 0.475, 0.1400), HAIR_HIGHLIGHT),
    ("hair-back-seam-mid",     "head", (-0.005, 0.315, 0.0920), (0.005, 0.475, 0.1400), HOODIE_SHADOW),

    # 5. HEAD OFUDA PAPER TALISMAN CLIP
    ("ofuda-clip-main",        "head", (-0.124, 0.490, -0.1440), (-0.072, 0.535, -0.1300), OFUDA_PURPLE),
    ("ofuda-clip-top-bevel",   "head", (-0.120, 0.530, -0.1420), (-0.076, 0.542, -0.1320), OFUDA_PURPLE),
    ("ofuda-clip-pin",         "head", (-0.108, 0.518, -0.1460), (-0.088, 0.528, -0.1360), SILVER),
    ("ofuda-clip-eye-pupil",   "head", (-0.106, 0.498, -0.1460), (-0.090, 0.510, -0.1400), INK),
    ("ofuda-paper-body",       "head", (-0.126, 0.350, -0.1420), (-0.070, 0.495, -0.1320), OFUDA_WHITE),
    ("ofuda-paper-backing",    "head", (-0.130, 0.345, -0.1360), (-0.066, 0.498, -0.1280), HOODIE_SHADOW),
    ("ofuda-kanji-eye-dot",    "head", (-0.104, 0.468, -0.1440), (-0.092, 0.482, -0.1400), OFUDA_PURPLE),
    ("ofuda-kanji-stroke-top", "head", (-0.118, 0.438, -0.1440), (-0.078, 0.452, -0.1400), OFUDA_PURPLE),
    ("ofuda-kanji-stroke-eye", "head", (-0.118, 0.395, -0.1440), (-0.102, 0.432, -0.1400), OFUDA_PURPLE),
    ("ofuda-kanji-stroke-min", "head", (-0.098, 0.390, -0.1440), (-0.078, 0.432, -0.1400), OFUDA_PURPLE),
    ("ofuda-kanji-stroke-bot", "head", (-0.118, 0.365, -0.1440), (-0.078, 0.388, -0.1400), OFUDA_PURPLE),

    # 6. SQUARE TOON EARS
    ("ear-left",               "head", (0.118, 0.315, -0.0250), (0.140, 0.365, 0.0100), SKIN),
    ("ear-shadow-l",           "head", (0.120, 0.325, -0.0180), (0.138, 0.355, 0.0020), SKIN_DARK),
    ("ear-right",              "head", (-0.140, 0.315, -0.0250), (-0.118, 0.365, 0.0100), SKIN),
    ("ear-shadow-r",           "head", (-0.138, 0.325, -0.0180), (-0.120, 0.355, 0.0020), SKIN_DARK),
]

# ---------------------------------------------------------------------------
# HEAD DECALS (Pure clean porcelain skin, no blush)
# ---------------------------------------------------------------------------
HEAD_DECALS = []

# ---------------------------------------------------------------------------
# TORSO & SLEEVE DECALS (Front Moon & Eye, Stitches, Back All-Seeing Eye)
# ---------------------------------------------------------------------------
BODY_PANELS = []

BODY_DECALS = [
    # =========================================================================
    # 1. FRONT PIXEL CRESCENT C-GLYPH & EYE (Centered on Upper Chest)
    # =========================================================================
    # Sharp square-styled C-shaped Crescent Bracket (Decomposed into convex quads)
    ("front-c-spine",          "torso", "front", LAVENDER_GLOW,
     ((-0.024, 0.170), (-0.012, 0.236)), 1),
    ("front-c-top-arm",        "torso", "front", LAVENDER_GLOW,
     ((-0.012, 0.222), (0.024, 0.236)), 1),
    ("front-c-bot-arm",        "torso", "front", LAVENDER_GLOW,
     ((-0.012, 0.170), (0.024, 0.184)), 1),
    ("front-c-top-serif",      "torso", "front", LAVENDER_GLOW,
     ((0.014, 0.210), (0.024, 0.222)), 1),
    ("front-c-bot-serif",      "torso", "front", LAVENDER_GLOW,
     ((0.014, 0.184), (0.024, 0.196)), 1),

    # Center Eye Dot Inside the C
    ("front-moon-center-dot",  "torso", "front", GRAPHIC_ACCENT,
     ((-0.003, 0.198), (0.009, 0.208)), 2),
    ("front-moon-center-glint","torso", "front", LAVENDER_PALE,
     ((0.000, 0.201), (0.006, 0.205)), 3),

    # =========================================================================
    # 2. FRONT FLANKING SQUARE DOTS (Left & Right of C-Glyph at Bottom)
    # =========================================================================
    # Left Square Dot (-X side, viewer's left)
    ("front-square-dot-l",     "torso", "front", LAVENDER_GLOW,
     ((-0.065, 0.155), (-0.045, 0.175)), 1),
    ("front-square-core-l",    "torso", "front", GRAPHIC_ACCENT,
     ((-0.060, 0.160), (-0.050, 0.170)), 2),

    # Right Square Dot (+X side, viewer's right)
    ("front-square-dot-r",     "torso", "front", LAVENDER_GLOW,
     ((0.045, 0.155), (0.065, 0.175)), 1),
    ("front-square-core-r",    "torso", "front", GRAPHIC_ACCENT,
     ((0.050, 0.160), (0.060, 0.170)), 2),

    # =========================================================================
    # 3. BACK EMBLEM: ALL-SEEING EYE (Prominent on Back of Hoodie)
    # =========================================================================
    ("back-eye-almond-main",   "torso", "back", LAVENDER_GLOW,
     [(-0.085, 0.205), (-0.045, 0.240), (0.045, 0.240), (0.085, 0.205),
      (0.045, 0.170), (-0.045, 0.170)], 1),
    ("back-eye-inner-socket",  "torso", "back", HOODIE_DARK,
     [(-0.068, 0.205), (-0.036, 0.230), (0.036, 0.230), (0.068, 0.205),
      (0.036, 0.180), (-0.036, 0.180)], 2),

    ("back-eye-iris-ring",     "torso", "back", LAVENDER_GLOW,
     [(-0.028, 0.205), (-0.018, 0.226), (0.018, 0.226), (0.028, 0.205),
      (0.018, 0.184), (-0.018, 0.184)], 3),

    ("back-eye-pupil",         "torso", "back", HOODIE_DARK,
     ((-0.014, 0.196), (0.014, 0.214)), 4),
    ("back-eye-glint",         "torso", "back", LAVENDER_PALE,
     ((-0.007, 0.205), (0.007, 0.214)), 5),
]


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
            if FRONT_IS_MINUS_Z:
                out.append((name, bone,
                            (lo[0], lo[1], -hi[2]), (hi[0], hi[1], -lo[2]), slot) + rest)
            else:
                out.append((name, bone, lo, hi, slot) + rest)
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

    body = build_mesh(_family(BODY_BOXES, head=False), panels=_family(BODY_PANELS, head=False),
                      decals=_family_decals(BODY_DECALS))
    head = build_mesh(_family(HEAD_BOXES, head=True, as_authored=()),
                      decals=_family_decals(HEAD_DECALS),
                      donor=None)

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

    for entry in (_family(BODY_BOXES, head=False)
                  + _family(HEAD_BOXES, head=True, as_authored=())):
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
