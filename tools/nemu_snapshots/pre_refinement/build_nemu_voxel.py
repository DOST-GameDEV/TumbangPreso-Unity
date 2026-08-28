"""Builds team-nemu.glb (the Sleepy Ghost / Ofuda Spirit Girl) and person_team-nemu.tres.

    python tools/build_nemu_voxel.py

Master Reference: media_1787270043372.png / media_1787269975463.png
"""
import math
import json
import os
import struct
import sys

BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"
DONOR_SKULL = "Assets/TumbangPreso/Art/characters/persons/character-male-d.glb"

OUT = "Assets/TumbangPreso/Art/characters/persons/team-nemu.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-nemu.tres"
ROSTER_OUT = "Assets/TumbangPreso/Resources/Roster/person_nemu.asset"

BONE = {"root": 0, "leg-left": 1, "leg-right": 2, "torso": 3,
        "arm-left": 4, "arm-right": 5, "head": 6}

PARENT = {"leg-left": "root", "leg-right": "root", "torso": "root",
          "arm-left": "torso", "arm-right": "torso", "head": "torso"}

# ---------------------------------------------------------------------------
# PETITE CHIBI PROPORTIONS SKELETON (~0.6050m AUTHORED HEIGHT)
# ---------------------------------------------------------------------------
WAS_HIPS, WAS_SHOULDER, WAS_NECK, WAS_TOP = 0.150, 0.240, 0.280, 0.6050
NOW_HIPS, NOW_SHOULDER, NOW_NECK, NOW_TOP = 0.150, 0.240, 0.280, 0.6050

HEAD_SHIFT_Y = NOW_NECK - 0.343  # -0.063m vertical shift for donor skull
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
# COMPACT CUTE LEGS & CHUNKY PURPLE SNEAKERS
# ---------------------------------------------------------------------------
LEG_LEFT = [
    # Clean White / Lavender Sneaker Sole Rim Slab
    ("shoe-sole-left",         "leg-left", (0.015, 0.000, -0.115), (0.130, 0.022, 0.070), WHITE),
    ("shoe-tread-left",        "leg-left", (0.018, 0.000, -0.110), (0.126, 0.008, 0.065), LAVENDER_GLOW),

    # Chunky Purple Sneaker Body
    ("shoe-upper-left",        "leg-left", (0.018, 0.022, -0.110), (0.126, 0.065, 0.065), SHOE_PURPLE),
    ("shoe-toe-left",          "leg-left", (0.018, 0.022, -0.114), (0.126, 0.052, -0.060), SHOE_PURPLE),
    ("shoe-heel-left",         "leg-left", (0.018, 0.022, 0.025),  (0.126, 0.068, 0.068), SHOE_PURPLE),

    # Thick Lavender Ankle Strap / Collar
    ("shoe-strap-left",        "leg-left", (0.014, 0.058, -0.095), (0.130, 0.088, 0.060), LAVENDER_GLOW),
    ("shoe-strap-accent-l",    "leg-left", (0.016, 0.064, -0.098), (0.128, 0.082, -0.080), LAVENDER_PALE),

    # Petite Bare Porcelain Skin Calf
    ("leg-skin-left",          "leg-left", (0.025, 0.085, -0.070), (0.120, 0.150, 0.040), SKIN),
    ("leg-shadow-left",        "leg-left", (0.028, 0.135, -0.065), (0.116, 0.150, 0.036), SKIN_DARK),
]

LEG_RIGHT = [
    # Clean White / Lavender Sneaker Sole Rim Slab
    ("shoe-sole-right",        "leg-right", (-0.130, 0.000, -0.115), (-0.015, 0.022, 0.070), WHITE),
    ("shoe-tread-right",       "leg-right", (-0.126, 0.000, -0.110), (-0.018, 0.008, 0.065), LAVENDER_GLOW),

    # Chunky Purple Sneaker Body
    ("shoe-upper-right",       "leg-right", (-0.126, 0.022, -0.110), (-0.018, 0.065, 0.065), SHOE_PURPLE),
    ("shoe-toe-right",         "leg-right", (-0.126, 0.022, -0.114), (-0.018, 0.052, -0.060), SHOE_PURPLE),
    ("shoe-heel-right",        "leg-right", (-0.126, 0.022, 0.025),  (-0.018, 0.068, 0.068), SHOE_PURPLE),

    # Thick Lavender Ankle Strap / Collar
    ("shoe-strap-right",       "leg-right", (-0.130, 0.058, -0.095), (-0.014, 0.088, 0.060), LAVENDER_GLOW),
    ("shoe-strap-accent-r",    "leg-right", (-0.128, 0.064, -0.098), (-0.016, 0.082, -0.080), LAVENDER_PALE),

    # Petite Bare Porcelain Skin Calf
    ("leg-skin-right",         "leg-right", (-0.120, 0.085, -0.070), (-0.025, 0.150, 0.040), SKIN),
    ("leg-shadow-right",       "leg-right", (-0.116, 0.135, -0.065), (-0.028, 0.150, 0.036), SKIN_DARK),
]

# ---------------------------------------------------------------------------
# PUFFY OVERSIZED A-LINE BAGGY MIDNIGHT HOODIE TORSO & HIP TALISMAN
# ---------------------------------------------------------------------------
TORSO = [
    # 1. Main Continuous A-Line Torso Body (Clean flat front plane at z = 0.096)
    ("hoodie-core-main",       "torso", (-0.120, 0.105, -0.096), (0.120, 0.285, 0.096), HOODIE_DARK),
    ("hoodie-core-crease",     "torso", (-0.116, 0.100, -0.090), (0.116, 0.120, 0.090), HOODIE_SHADOW),

    # 2. Flared Lower Hem / Sweater-Dress Drape (Wide flared skirt hanging over thighs)
    ("hoodie-skirt-hem",       "torso", (-0.142, 0.075, -0.112), (0.142, 0.120, 0.112), HOODIE_DARK),
    ("hoodie-skirt-shadow",    "torso", (-0.138, 0.068, -0.108), (0.138, 0.082, 0.108), HOODIE_SHADOW),
    ("hoodie-skirt-stripe",    "torso", (-0.145, 0.080, -0.115), (0.145, 0.102, 0.115), LAVENDER_GLOW),

    # 3. Puffy Rounded Cowl Collar Wrap & Front Pearl Drawstring (Above chest decal zone)
    ("hoodie-collar-main",     "torso", (-0.115, 0.260, -0.105), (0.115, 0.298, 0.105), HOODIE_DARK),
    ("hoodie-collar-front",    "torso", (-0.085, 0.265, 0.080),  (0.085, 0.295, 0.105), HOODIE_DARK),
    ("hoodie-collar-shadow",   "torso", (-0.065, 0.268, 0.085),  (0.065, 0.288, 0.102), HOODIE_SHADOW),
    ("hoodie-collar-pearl",    "torso", (-0.015, 0.265, 0.102),  (0.015, 0.295, 0.128), LAVENDER_GLOW),
    ("hoodie-collar-glint",    "torso", (-0.007, 0.275, 0.112),  (0.007, 0.288, 0.132), LAVENDER_PALE),
    ("hoodie-collar-base",     "torso", (-0.018, 0.258, 0.098),  (0.018, 0.268, 0.118), HOODIE_SHADOW),

    # 4. 🏷️ HIP OFUDA PAPER TALISMAN (Front-Left Hip: +X, viewer's right - Prominently hanging)
    ("ofuda-hip-clip",         "torso", (0.092, 0.088, 0.095),   (0.128, 0.120, 0.125), OFUDA_PURPLE),
    ("ofuda-hip-paper",        "torso", (0.095, 0.012, 0.100),   (0.125, 0.095, 0.122), OFUDA_WHITE),
    ("ofuda-hip-backing",      "torso", (0.092, 0.008, 0.096),   (0.128, 0.098, 0.120), HOODIE_SHADOW),
    ("ofuda-hip-kanji-top",    "torso", (0.102, 0.072, 0.123),   (0.118, 0.086, 0.126), OFUDA_PURPLE),
    ("ofuda-hip-kanji-eye",    "torso", (0.102, 0.048, 0.123),   (0.118, 0.068, 0.126), OFUDA_PURPLE),
    ("ofuda-hip-kanji-min",    "torso", (0.102, 0.026, 0.123),   (0.118, 0.044, 0.126), OFUDA_PURPLE),
]

# ---------------------------------------------------------------------------
# GIANT PUFFY STEPPED FLARED BELL SLEEVES WITH CONCENTRIC FRAMES & HOLLOW CUFFS
# ---------------------------------------------------------------------------
ARM_LEFT = [
    # 1. Slouchy Dropped-Shoulder Capelet (High shoulder zone)
    ("sleeve-shoulder-l",      "arm-left", (0.060, 0.195, -0.080), (0.130, 0.285, 0.080), HOODIE_DARK),

    # 2. Mid-Sleeve Dropped Puffy Box (Stepped downward & outward)
    ("sleeve-bell-mid-l",      "arm-left", (0.110, 0.145, -0.098), (0.180, 0.250, 0.098), HOODIE_DARK),
    ("sleeve-bell-mid-shad-l", "arm-left", (0.115, 0.135, -0.092), (0.175, 0.158, 0.092), HOODIE_SHADOW),

    # 3. Giant Flared Bell Sleeve Outer Body (Puffy flare, calibrated proportions)
    ("sleeve-cuff-outer-l",    "arm-left", (0.160, 0.085, -0.108), (0.240, 0.220, 0.108), HOODIE_DARK),
    ("sleeve-cuff-under-l",    "arm-left", (0.165, 0.075, -0.102), (0.236, 0.098, 0.102), HOODIE_SHADOW),

    # 4. Stepped Concentric Lavender Cuff Border Frame (Outer Ring)
    ("sleeve-cuff-stripe-l",   "arm-left", (0.220, 0.078, -0.112), (0.245, 0.226, 0.112), LAVENDER_GLOW),

    # 5. Concentric Stepped Outer Rim & Deep Hollow Box Tunnel Cavity
    ("sleeve-cuff-rim-l",      "arm-left", (0.235, 0.082, -0.108), (0.252, 0.222, 0.108), HOODIE_DARK),
    ("sleeve-cuff-inner-rim-l","arm-left", (0.238, 0.102, -0.095), (0.254, 0.202, 0.095), LAVENDER_GLOW),
    ("sleeve-cuff-recess-l",   "arm-left", (0.242, 0.118, -0.080), (0.256, 0.186, 0.080), HOODIE_SHADOW),
    ("sleeve-hollow-cavity-l", "arm-left", (0.170, 0.105, -0.075), (0.248, 0.200, 0.075), HOODIE_SHADOW),

    # 6. Cute Tucked Hand (Peeking at the cuff edge - authentic reference look!)
    ("hand-palm-left",         "arm-left", (0.240, 0.195, -0.032), (0.272, 0.255, 0.016), SKIN),
    ("hand-fingers-left",      "arm-left", (0.268, 0.200, -0.028), (0.286, 0.250, 0.010), SKIN),
    ("hand-fingers-tip-l",     "arm-left", (0.282, 0.205, -0.024), (0.292, 0.245, 0.006), SKIN_DARK),
]

ARM_RIGHT = [
    # 1. Slouchy Dropped-Shoulder Capelet (High shoulder zone)
    ("sleeve-shoulder-r",      "arm-right", (-0.130, 0.195, -0.080), (-0.060, 0.285, 0.080), HOODIE_DARK),

    # 2. Mid-Sleeve Dropped Puffy Box (Stepped downward & outward)
    ("sleeve-bell-mid-r",      "arm-right", (-0.180, 0.145, -0.098), (-0.110, 0.250, 0.098), HOODIE_DARK),
    ("sleeve-bell-mid-shad-r", "arm-right", (-0.175, 0.135, -0.092), (-0.115, 0.158, 0.092), HOODIE_SHADOW),

    # 3. Giant Flared Bell Sleeve Outer Body (Puffy flare, calibrated proportions)
    ("sleeve-cuff-outer-r",    "arm-right", (-0.240, 0.085, -0.108), (-0.160, 0.220, 0.108), HOODIE_DARK),
    ("sleeve-cuff-under-r",    "arm-right", (-0.236, 0.075, -0.102), (-0.165, 0.098, 0.102), HOODIE_SHADOW),

    # 4. Stepped Concentric Lavender Cuff Border Frame (Outer Ring)
    ("sleeve-cuff-stripe-r",   "arm-right", (-0.245, 0.078, -0.112), (-0.220, 0.226, 0.112), LAVENDER_GLOW),

    # 5. Concentric Stepped Outer Rim & Deep Hollow Box Tunnel Cavity
    ("sleeve-cuff-rim-r",      "arm-right", (-0.252, 0.082, -0.108), (-0.235, 0.222, 0.108), HOODIE_DARK),
    ("sleeve-cuff-inner-rim-r","arm-right", (-0.254, 0.102, -0.095), (-0.238, 0.202, 0.095), LAVENDER_GLOW),
    ("sleeve-cuff-recess-r",   "arm-right", (-0.256, 0.118, -0.080), (-0.242, 0.186, 0.080), HOODIE_SHADOW),
    ("sleeve-hollow-cavity-r", "arm-right", (-0.248, 0.105, -0.075), (-0.170, 0.200, 0.075), HOODIE_SHADOW),

    # 6. Cute Tucked Hand (Peeking at the cuff edge - authentic reference look!)
    ("hand-palm-right",        "arm-right", (-0.272, 0.195, -0.032), (-0.240, 0.255, 0.016), SKIN),
    ("hand-fingers-right",     "arm-right", (-0.286, 0.200, -0.028), (-0.268, 0.250, 0.010), SKIN),
    ("hand-fingers-tip-r",     "arm-right", (-0.292, 0.205, -0.024), (-0.282, 0.245, 0.006), SKIN_DARK),
]

# ---------------------------------------------------------------------------
# HEAD: WIDE CHIBI FACE, BOB HAIR, SLEEPY EYES (-   -), NO MOUTH (CUTER!)
# ---------------------------------------------------------------------------
HEAD = [
    # =========================================================================
    # 0. 🌸 CUTE WIDE CHIBI PALE PORCELAIN FACE BASE
    # =========================================================================
    ("face-core",              "head", (-0.125, 0.285, -0.090), (0.125, 0.460, 0.114), SKIN),
    ("face-chin-taper",        "head", (-0.100, 0.275, -0.075), (0.100, 0.295, 0.100), SKIN),

    # =========================================================================
    # 0.1 😴 ICONIC BOLD SLEEPY SPIRIT EYES (-   -) (CLEAN PORCELAIN, MOUTHLESS CUTE!)
    # =========================================================================
    # Left Eye (+X, viewer's right in front view) - Chunky bold sleepy horizontal bar
    ("eye-lash-main-l",        "head", (0.028, 0.328, 0.1130), (0.092, 0.358, 0.1145), INK),
    ("eye-lash-lid-l",         "head", (0.034, 0.332, 0.1130), (0.088, 0.354, 0.1145), INK),

    # Right Eye (-X, viewer's left in front view) - Chunky bold sleepy horizontal bar
    ("eye-lash-main-r",        "head", (-0.092, 0.328, 0.1130), (-0.028, 0.358, 0.1145), INK),
    ("eye-lash-lid-r",         "head", (-0.088, 0.332, 0.1130), (-0.034, 0.354, 0.1145), INK),

    # =========================================================================
    # 1. 💇 COMPACT HAIR CROWN DOME & SMOOTH SILHOUETTE
    # =========================================================================
    ("hair-crown-core",        "head", (-0.142, 0.400, -0.128), (0.142, 0.585, 0.116), HAIR_DARK),
    ("hair-crown-top-apex",    "head", (-0.120, 0.570, -0.105), (0.120, 0.598, 0.095), HAIR_DARK),
    ("hair-crown-sheen",       "head", (-0.115, 0.560, -0.040), (0.115, 0.595, 0.040), HAIR_HIGHLIGHT),
    ("hair-crown-side-l",      "head", (0.124, 0.380, -0.112), (0.145, 0.575, 0.092), HAIR_DARK),
    ("hair-crown-side-r",      "head", (-0.145, 0.380, -0.112), (-0.124, 0.575, 0.092), HAIR_DARK),

    # =========================================================================
    # 2. ✂️ STRAIGHT HIME-CUT BLUNT BANGS (LOW DOWN - ZERO FOREHEAD!)
    # =========================================================================
    # Brow Base Block: Covers from dome (0.575) down to 0.365
    ("hair-bangs-brow-base",   "head", (-0.136, 0.365, 0.095), (0.136, 0.575, 0.128), HAIR_DARK),
    ("hair-bangs-brow-bevel",  "head", (-0.130, 0.390, 0.122), (0.130, 0.555, 0.136), HAIR_HIGHLIGHT),

    # 4 Distinct Chunky Vertical Bangs Strands hanging directly over top of eyes:
    ("hair-strand-outer-l",    "head", (0.066, 0.348, 0.118),  (0.130, 0.450, 0.136), HAIR_DARK),
    ("hair-strand-outer-l-top","head", (0.070, 0.354, 0.128),  (0.124, 0.445, 0.140), HAIR_HIGHLIGHT),
    ("hair-strand-mid-l",      "head", (0.004, 0.358, 0.120),  (0.062, 0.450, 0.138), HAIR_DARK),
    ("hair-strand-mid-l-top",  "head", (0.008, 0.364, 0.130),  (0.058, 0.445, 0.142), HAIR_HIGHLIGHT),
    ("hair-strand-mid-r",      "head", (-0.062, 0.358, 0.120), (-0.004, 0.450, 0.138), HAIR_DARK),
    ("hair-strand-mid-r-top",  "head", (-0.058, 0.364, 0.130), (-0.008, 0.445, 0.142), HAIR_HIGHLIGHT),
    ("hair-strand-outer-r",    "head", (-0.130, 0.348, 0.118), (-0.066, 0.450, 0.136), HAIR_DARK),
    ("hair-strand-outer-r-top","head", (-0.124, 0.354, 0.128), (-0.070, 0.445, 0.140), HAIR_HIGHLIGHT),

    # 3 Vertical Notch Gap Dividers separating the 4 chunky strands:
    ("hair-bangs-gap-l",       "head", (0.062, 0.350, 0.116),  (0.066, 0.445, 0.136), HOODIE_SHADOW),
    ("hair-bangs-gap-mid",     "head", (-0.004, 0.360, 0.116), (0.004, 0.445, 0.136), HOODIE_SHADOW),
    ("hair-bangs-gap-r",       "head", (-0.066, 0.350, 0.116), (-0.062, 0.445, 0.136), HOODIE_SHADOW),

    # =========================================================================
    # 3. 🎀 LONG STRAIGHT SIDE LOCKS (Framing Cheeks & Shoulders down to y = 0.200)
    # =========================================================================
    # Left Side Lock (+X, viewer's right)
    ("hair-sidelock-main-l",   "head", (0.118, 0.200, -0.010), (0.146, 0.455, 0.122), HAIR_DARK),
    ("hair-sidelock-front-l",  "head", (0.114, 0.200, 0.076),  (0.142, 0.440, 0.132), HAIR_DARK),
    ("hair-sidelock-highlight-l","head",(0.120, 0.220, 0.090), (0.144, 0.425, 0.136), HAIR_HIGHLIGHT),

    # Right Side Lock (-X, viewer's left)
    ("hair-sidelock-main-r",   "head", (-0.146, 0.200, -0.010), (-0.118, 0.455, 0.122), HAIR_DARK),
    ("hair-sidelock-front-r",  "head", (-0.142, 0.200, 0.076),  (-0.114, 0.440, 0.132), HAIR_DARK),
    ("hair-sidelock-highlight-r","head",(-0.144, 0.220, 0.090), (-0.120, 0.425, 0.136), HAIR_HIGHLIGHT),

    # =========================================================================
    # 4. 📜 SHORT NAPE BACK HAIR DRAPE (Stopping above Back All-Seeing Eye!)
    # =========================================================================
    # Tier 1 (Upper Nape)
    ("hair-back-tier1-c",      "head", (-0.138, 0.380, -0.138), (0.138, 0.490, -0.085), HAIR_DARK),
    ("hair-back-tier1-l",      "head", (0.092, 0.380, -0.134), (0.144, 0.490, -0.075), HAIR_DARK),
    ("hair-back-tier1-r",      "head", (-0.144, 0.380, -0.134), (-0.092, 0.490, -0.075), HAIR_DARK),

    # Tier 2 (Lower Nape - Stops at y = 0.315, well above eye emblem!)
    ("hair-back-tier2-c",      "head", (-0.132, 0.315, -0.138), (0.132, 0.395, -0.090), HAIR_DARK),
    ("hair-back-tier2-l",      "head", (0.088, 0.315, -0.134), (0.138, 0.395, -0.080), HAIR_DARK),
    ("hair-back-tier2-r",      "head", (-0.138, 0.315, -0.134), (-0.088, 0.395, -0.080), HAIR_DARK),

    # Vertical Strand Notches / Seams across Back Hair
    ("hair-back-seam-1",       "head", (-0.048, 0.315, -0.140), (-0.038, 0.475, -0.092), HAIR_HIGHLIGHT),
    ("hair-back-seam-2",       "head", (0.038, 0.315, -0.140),  (0.048, 0.475, -0.092), HAIR_HIGHLIGHT),
    ("hair-back-seam-mid",     "head", (-0.005, 0.315, -0.140), (0.005, 0.475, -0.092), HOODIE_SHADOW),

    # =========================================================================
    # 5. 🏷️ HEAD OFUDA PAPER TALISMAN CLIP (Front Right Hair: -X, Opposite of Ghost Pet)
    # =========================================================================
    # Purple Top Clip on Front Hair
    ("ofuda-clip-main",        "head", (-0.124, 0.490, 0.130), (-0.072, 0.535, 0.144), OFUDA_PURPLE),
    ("ofuda-clip-top-bevel",   "head", (-0.120, 0.530, 0.132), (-0.076, 0.542, 0.142), OFUDA_PURPLE),
    ("ofuda-clip-pin",         "head", (-0.108, 0.518, 0.136), (-0.088, 0.528, 0.146), SILVER),
    ("ofuda-clip-eye-pupil",   "head", (-0.106, 0.498, 0.140), (-0.090, 0.510, 0.146), INK),

    # Crisp Paper White Ofuda Tag (Hanging on front bangs/face)
    ("ofuda-paper-body",       "head", (-0.126, 0.350, 0.132), (-0.070, 0.495, 0.142), OFUDA_WHITE),
    ("ofuda-paper-backing",    "head", (-0.130, 0.345, 0.128), (-0.066, 0.498, 0.136), HOODIE_SHADOW),

    # Kanji 「眠」 and Spirit Eye Glyph on Ofuda Front Face
    ("ofuda-kanji-eye-dot",    "head", (-0.104, 0.468, 0.140), (-0.092, 0.482, 0.144), OFUDA_PURPLE),
    ("ofuda-kanji-stroke-top", "head", (-0.118, 0.438, 0.140), (-0.078, 0.452, 0.144), OFUDA_PURPLE),
    ("ofuda-kanji-stroke-eye", "head", (-0.118, 0.395, 0.140), (-0.102, 0.432, 0.144), OFUDA_PURPLE),
    ("ofuda-kanji-stroke-min", "head", (-0.098, 0.390, 0.140), (-0.078, 0.432, 0.144), OFUDA_PURPLE),
    ("ofuda-kanji-stroke-bot", "head", (-0.118, 0.365, 0.140), (-0.078, 0.388, 0.144), OFUDA_PURPLE),

    # (Companion pet is now an autonomous dynamic companion entity)

    # =========================================================================
    # 7. 👂 SQUARE TOON EARS TUCKED UNDER SIDE HAIR
    # =========================================================================
    ("ear-left",               "head", (0.118, 0.315, -0.010), (0.140, 0.365, 0.025), SKIN),
    ("ear-shadow-l",           "head", (0.120, 0.325, -0.002), (0.138, 0.355, 0.018), SKIN_DARK),
    ("ear-right",              "head", (-0.140, 0.315, -0.010), (-0.118, 0.365, 0.025), SKIN),
    ("ear-shadow-r",           "head", (-0.138, 0.325, -0.002), (-0.120, 0.355, 0.018), SKIN_DARK),
]

DONOR_SPACE = tuple(entry[0] for entry in HEAD)

# ---------------------------------------------------------------------------
# HEAD DECALS (Soft Light Sakura Cheek Blush)
# ---------------------------------------------------------------------------
HEAD_DECALS = [
    # Soft, light, sweet sakura pink cheek blush (2D planar decal - flush on face, 0 thickness!)
    ("cheek-blush-l",          "head", "front", SKIN_DARK,
     ((0.034, 0.298), (0.088, 0.320)), 1),
    ("cheek-blush-r",          "head", "front", SKIN_DARK,
     ((-0.088, 0.298), (-0.034, 0.320)), 1),
]

# ---------------------------------------------------------------------------
# TORSO & SLEEVE DECALS (Front Moon & Eye, Stitches, Back All-Seeing Eye)
# ---------------------------------------------------------------------------
BODY_PANELS = []

# Front & Back Decals:
BODY_DECALS = [
    # =========================================================================
    # 1. 🌙 FRONT CRESCENT MOON & EYE GLYPH (Centered on Upper Chest)
    # =========================================================================
    # Smooth C-shaped Crescent Arc (Opening to the right (+X))
    ("front-moon-arc",         "torso", "front", LAVENDER_GLOW,
     [(-0.046, 0.205), (-0.038, 0.238), (-0.010, 0.252), (0.028, 0.250),
      (0.038, 0.238), (0.010, 0.232), (-0.018, 0.205), (0.010, 0.178),
      (0.038, 0.172), (0.028, 0.160), (-0.010, 0.158), (-0.038, 0.172)], 1),

    # Moon Center Eye Dot Glyph (Inside the open hollow of the crescent)
    ("front-moon-eye-dot",     "torso", "front", GRAPHIC_ACCENT,
     [(-0.002, 0.205), (0.010, 0.217), (0.022, 0.205), (0.010, 0.193)], 2),
    ("front-moon-eye-core",    "torso", "front", LAVENDER_PALE,
     ((0.005, 0.200), (0.015, 0.210)), 3),

    # Top Crown Dot on Crescent (Floating above top horn)
    ("front-moon-top-dot",     "torso", "front", LAVENDER_GLOW,
     ((0.028, 0.248), (0.040, 0.260)), 2),

    # =========================================================================
    # 2. ✖️ FRONT STITCH CROSSES (Flanking lower chest below moon)
    # =========================================================================
    # Left Stitch Cross (+X side, viewer's right)
    ("front-stitch-l-bar1",    "torso", "front", LAVENDER_GLOW,
     [(0.052, 0.142), (0.076, 0.166), (0.082, 0.160), (0.058, 0.136)], 1),
    ("front-stitch-l-bar2",    "torso", "front", LAVENDER_GLOW,
     [(0.052, 0.160), (0.076, 0.136), (0.082, 0.142), (0.058, 0.166)], 1),
    ("front-stitch-l-center",  "torso", "front", GRAPHIC_ACCENT,
     ((0.063, 0.147), (0.071, 0.155)), 2),

    # Right Stitch Cross (-X side, viewer's left)
    ("front-stitch-r-bar1",    "torso", "front", LAVENDER_GLOW,
     [(-0.082, 0.142), (-0.058, 0.166), (-0.052, 0.160), (-0.076, 0.136)], 1),
    ("front-stitch-r-bar2",    "torso", "front", LAVENDER_GLOW,
     [(-0.082, 0.160), (-0.058, 0.136), (-0.052, 0.142), (-0.076, 0.166)], 1),
    ("front-stitch-r-center",  "torso", "front", GRAPHIC_ACCENT,
     ((-0.071, 0.147), (-0.063, 0.155)), 2),

    # =========================================================================
    # 3. 👁️ BACK EMBLEM: ALL-SEEING EYE (Prominent on Back of Hoodie)
    # =========================================================================
    # Outer Almond Eye Border (Top/Bottom Arcs & Lateral Corners)
    ("back-eye-almond-main",   "torso", "back", LAVENDER_GLOW,
     [(-0.085, 0.205), (-0.045, 0.240), (0.045, 0.240), (0.085, 0.205),
      (0.045, 0.170), (-0.045, 0.170)], 1),
    ("back-eye-inner-socket",  "torso", "back", HOODIE_DARK,
     [(-0.068, 0.205), (-0.036, 0.230), (0.036, 0.230), (0.068, 0.205),
      (0.036, 0.180), (-0.036, 0.180)], 2),

    # Radiant Lavender Iris Circle
    ("back-eye-iris-ring",     "torso", "back", LAVENDER_GLOW,
     [(-0.028, 0.205), (-0.018, 0.226), (0.018, 0.226), (0.028, 0.205),
      (0.018, 0.184), (-0.018, 0.184)], 3),

    # Inner Pupil & Core Glint
    ("back-eye-pupil",         "torso", "back", HOODIE_DARK,
     ((-0.014, 0.196), (0.014, 0.214)), 4),
    ("back-eye-glint",         "torso", "back", LAVENDER_PALE,
     ((-0.007, 0.205), (0.007, 0.214)), 5),
]

ARM_DECALS = []


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
# DONATED SKULL & SLEEPY SPIRIT GIRL EXPRESSION (- _ -)
# ---------------------------------------------------------------------------
SKULL_SLOTS = {15: SKIN}
MOUTH_Z = 0.1596
PANEL_PROUD = 0.0006


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
                    # Apply vertical shift so donor skull matches NOW_NECK
                    p = src_pos[i]
                    p_shifted = (p[0] * HEAD_GROWTH,
                                 (p[1] - 0.343) * HEAD_GROWTH + NOW_NECK,
                                 p[2] * HEAD_GROWTH)
                    pos.append(p_shifted)
                    nrm.append(src_nrm[i])
                    uv.append(cell_uv(paint) if paint is not None else src_uv[i])

            tris.append(tuple(remap[i] for i in tri))

        if not tris:
            raise SystemExit(f"{path} has no triangles in slots {sorted(slots)}")

        return pos, nrm, uv, tris

    raise SystemExit(f"{path} has no head-mesh")


def _donor_head():
    """Builds donor head mesh with cute sleepy horizontal eyes (- _ -) and neutral mouth."""
    pos, nrm, uv, tris = _donor_part(DONOR_SKULL, SKULL_SLOTS)

    # Face plate Z plane
    plate = (MOUTH_Z - 0.002) + PANEL_PROUD

    def add_quad_2d(pts, slot, layer=1):
        """Adds a 2D quad in (X, Y) onto the face plate at +Z."""
        offset = layer * PANEL_PROUD
        z = plate + offset
        normal = (0.0, 0.0, 1.0)
        u, v = cell_uv(slot)
        first = len(pos)
        for x, y in pts:
            pos.append((x, y, z))
            nrm.append(normal)
            uv.append((u, v))
        tris.append((first, first + 1, first + 2))
        tris.append((first, first + 2, first + 3))

    # =========================================================================
    # 😴 SLEEPY SPIRIT EYES (- _ -)
    # =========================================================================
    # Eye Level: y in [0.415, 0.445]
    # LEFT EYE (+X side, viewer's right):
    eye_lx = 0.072
    eye_y = 0.428
    # Main Horizontal Sleepy Slit Lash Bar
    add_quad_2d([(eye_lx - 0.038, eye_y - 0.005), (eye_lx + 0.038, eye_y - 0.005),
                 (eye_lx + 0.038, eye_y + 0.007), (eye_lx - 0.038, eye_y + 0.007)],
                INK, layer=1)
    # Downward Outer Eyelash Flik
    add_quad_2d([(eye_lx + 0.032, eye_y - 0.012), (eye_lx + 0.040, eye_y - 0.012),
                 (eye_lx + 0.040, eye_y + 0.007), (eye_lx + 0.032, eye_y + 0.007)],
                INK, layer=1)
    # Sleepy Pupil Bar Under Lash
    add_quad_2d([(eye_lx - 0.024, eye_y - 0.012), (eye_lx + 0.024, eye_y - 0.012),
                 (eye_lx + 0.024, eye_y - 0.004), (eye_lx - 0.024, eye_y - 0.004)],
                INK, layer=2)
    # Soft Peach Blushing Cheek Highlight
    add_quad_2d([(eye_lx - 0.030, eye_y - 0.028), (eye_lx + 0.030, eye_y - 0.028),
                 (eye_lx + 0.030, eye_y - 0.016), (eye_lx - 0.030, eye_y - 0.016)],
                SKIN_DARK, layer=1)

    # RIGHT EYE (-X side, viewer's left):
    eye_rx = -0.072
    # Main Horizontal Sleepy Slit Lash Bar
    add_quad_2d([(eye_rx - 0.038, eye_y - 0.005), (eye_rx + 0.038, eye_y - 0.005),
                 (eye_rx + 0.038, eye_y + 0.007), (eye_rx - 0.038, eye_y + 0.007)],
                INK, layer=1)
    # Downward Outer Eyelash Flik
    add_quad_2d([(eye_rx - 0.040, eye_y - 0.012), (eye_rx - 0.032, eye_y - 0.012),
                 (eye_rx - 0.032, eye_y + 0.007), (eye_rx - 0.040, eye_y + 0.007)],
                INK, layer=1)
    # Sleepy Pupil Bar Under Lash
    add_quad_2d([(eye_rx - 0.024, eye_y - 0.012), (eye_rx + 0.024, eye_y - 0.012),
                 (eye_rx + 0.024, eye_y - 0.004), (eye_rx - 0.024, eye_y - 0.004)],
                INK, layer=2)
    # Soft Peach Blushing Cheek Highlight
    add_quad_2d([(eye_rx - 0.030, eye_y - 0.028), (eye_rx + 0.030, eye_y - 0.028),
                 (eye_rx + 0.030, eye_y - 0.016), (eye_rx - 0.030, eye_y - 0.016)],
                SKIN_DARK, layer=1)

    # =========================================================================
    # 👄 SWEET NEUTRAL / SLEEPY MOUTH LINE
    # =========================================================================
    mouth_y = 0.365
    add_quad_2d([(-0.018, mouth_y - 0.003), (0.018, mouth_y - 0.003),
                 (0.018, mouth_y + 0.003), (-0.018, mouth_y + 0.003)],
                INK, layer=1)

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
            z_plane = 0.1141 if bone == "head" else 0.098
            pts_3d = [(p[0], p[1], z_plane + offset) for p in pts_2d]
        elif face == "back":
            if signed_area_2d(pts_2d) > 0:
                pts_2d.reverse()
            normal = (0.0, 0.0, -1.0)
            z_plane = -0.1141 if bone == "head" else -0.098
            pts_3d = [(p[0], p[1], z_plane - offset) for p in pts_2d]
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
    head = build_mesh(_family(HEAD_BOXES, head=True, as_authored=DONOR_SPACE),
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
                  + _family(HEAD_BOXES, head=True, as_authored=DONOR_SPACE)):
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

    print(f"wrote {path}  ({total} bytes)")


if __name__ == "__main__":
    sys.exit(main())
