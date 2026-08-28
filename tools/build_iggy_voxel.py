"""Builds team-iggy.glb (the Heavyweight Fire Brawler) and person_team-iggy.tres.

    python tools/build_iggy_voxel.py

Master Reference: media_1787232940989.png & media_1787232228930.png
"""
import math
import json
import os
import struct
import sys

BASE = "Assets/TumbangPreso/Art/characters/persons/character-male-f.glb"

OUT = "Assets/TumbangPreso/Art/characters/persons/team-iggy.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-iggy.tres"
ROSTER_OUT = "Assets/TumbangPreso/Resources/Roster/person_kuya_boy.asset"

BONE = {"root": 0, "leg-left": 1, "leg-right": 2, "torso": 3,
        "arm-left": 4, "arm-right": 5, "head": 6}

PARENT = {"leg-left": "root", "leg-right": "root", "torso": "root",
          "arm-left": "torso", "arm-right": "torso", "head": "torso"}

# ---------------------------------------------------------------------------
# HEAVYWEIGHT BRAWLER SKELETON: TALL ATHLETIC STATURE + FULL LEG LENGTH (ITER 51)
# ---------------------------------------------------------------------------
WAS_HIPS, WAS_SHOULDER, WAS_NECK, WAS_TOP = 0.245, 0.430, 0.473, 0.8480
NOW_HIPS, NOW_SHOULDER, NOW_NECK, NOW_TOP = 0.245, 0.430, 0.473, 0.8480

HEAD_SHIFT_Y = NOW_NECK - 0.343  # 0.130m vertical lift for donor skull and head features
HEAD_GROWTH = 1.0
CAST_MIN_HEIGHT, CAST_MAX_HEIGHT = 0.6613, 0.8500

SKELETON = {
    "root":      (0.0,      0.0,          0.0),
    "leg-left":  (0.096,    NOW_HIPS,     -0.02875),
    "leg-right": (-0.096,   NOW_HIPS,     -0.02875),
    "torso":     (0.0,      NOW_HIPS,     -0.02875),
    "arm-left":  (0.125,    NOW_SHOULDER, -0.01725),
    "arm-right": (-0.125,   NOW_SHOULDER, -0.01725),
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
SKIN_BASE      = 0   # Main sun-bronze skin (#b87440)
SKIN_SHADOW    = 1   # Shadow creases, sternum, inner ears, muscle separation (#96592e)
SKIN_HIGHLIGHT = 2   # Deltoid, bicep, traps & pectoral highlight (#cb864e)
VEST_RED       = 3   # Main sleeveless chaleco / vest body (#c92a2a)
VEST_SHADOW    = 4   # Vest inner shadow & underside folds (#991b1b)
GOLD_TRIM      = 5   # Radiant golden yellow piping, cuffs, buckle, thigh trim (#f0a500)
GOLD_SHADOW    = 6   # Inner buckle recess, trim underside (#b87a00)
HAIR_BLACK     = 7   # Deep charcoal anime hair base (#1c1a24)
INK            = 8   # Solid black face ink (bold eyes, smile mouth) (#111115) - MAX_FACE_LUMINANCE <= 0.30
FLAME_YELLOW   = 9   # Mohawk front/top peak crest (#ffc107)
FLAME_RED      = 10  # Mohawk base & flame back (#dc2626)
BELT_BROWN     = 11  # Dark brown leather belt strap (#5c3a21)
SHORTS_RED     = 12  # Red athletic trunks (#c92a2a)
WHITE          = 13  # Crisp white sneaker sole (#f0f2f5)
FLAME_ORANGE   = 14  # Mohawk solar core flame (#ff6b1a)
SHOE_ACCENT    = 15  # Sneaker front tongue riser & accents (#ff8800)

PALETTE = {
    SKIN_BASE:      "b87440",   # Warm sun-bronze skin
    SKIN_SHADOW:    "96592e",   # Shadow creases & muscle groove depth
    SKIN_HIGHLIGHT: "cb864e",   # Light-catching bronze highlight (biceps, delts, traps)
    VEST_RED:       "c92a2a",   # Bright chaleco red
    VEST_SHADOW:    "991b1b",   # Deep vest crimson shadow
    GOLD_TRIM:      "f0a500",   # Radiant golden yellow piping & trims
    GOLD_SHADOW:    "b87a00",   # Rich amber gold shadow & buckle recess
    HAIR_BLACK:     "1c1a24",   # Charcoal black hair base & tufts
    INK:            "111115",   # Solid black ink (bold eyes & smile)
    FLAME_YELLOW:   "ffc107",   # Solar yellow flame peak
    FLAME_RED:      "dc2626",   # Fiery red flame base & back face
    BELT_BROWN:     "5c3a21",   # Dark brown leather belt strap
    SHORTS_RED:     "c92a2a",   # Red athletic trunks
    WHITE:          "f0f2f5",   # Crisp white sneaker sole
    FLAME_ORANGE:   "ff6b1a",   # Vibrant solar orange flame core
    SHOE_ACCENT:    "ff8800",   # Bright fiery orange sneaker tongue riser
}

MAX_FACE_LUMINANCE = 0.30


# ---------------------------------------------------------------------------
# MASSIVE HEAVYWEIGHT TREE-TRUNK LEGS & HIGH-TOP SNEAKERS
# ---------------------------------------------------------------------------
LEG_LEFT = [
    # Flat White Sneaker Sole Slab (Heavyweight combat sole)
    ("shoe-sole-left",         "leg-left", (0.006, 0.000, -0.160), (0.185, 0.028, 0.105), WHITE),
    
    # Red Sneaker Lower Body & Heel Counter
    ("shoe-upper-left",        "leg-left", (0.010, 0.028, -0.155), (0.180, 0.085, 0.100), FLAME_RED),
    ("shoe-toe-left",          "leg-left", (0.010, 0.028, -0.160), (0.180, 0.058, -0.080), FLAME_RED),
    ("shoe-heel-left",         "leg-left", (0.010, 0.028, 0.040), (0.180, 0.088, 0.102), FLAME_RED),
    
    # Bright Fiery Orange Sneaker Tongue Riser (Projecting in Front)
    ("shoe-tongue-left",       "leg-left", (0.025, 0.038, -0.165), (0.165, 0.115, -0.075), SHOE_ACCENT),
    ("shoe-collar-trim-l",     "leg-left", (0.008, 0.075, -0.100), (0.182, 0.095, 0.100), FLAME_RED),
    ("shoe-accent-strap-l",    "leg-left", (0.008, 0.038, -0.100), (0.182, 0.062, 0.100), SHOE_ACCENT),
    
    # Muscular Bare Sun-Bronze Calves (Defined muscle contour)
    ("leg-skin-left",          "leg-left", (0.012, 0.070, -0.095), (0.178, 0.140, 0.095), SKIN_BASE),
    ("calf-muscle-high-l",     "leg-left", (0.020, 0.075, 0.010), (0.172, 0.135, 0.098), SKIN_HIGHLIGHT),
    
    # Radiant Golden Thigh Trim Band (Wrapping Lower Hem of Shorts)
    ("thigh-trim-left",        "leg-left", (0.006, 0.108, -0.112), (0.186, 0.155, 0.112), GOLD_TRIM),
    ("thigh-trim-inner-l",     "leg-left", (0.008, 0.112, -0.106), (0.184, 0.132, 0.106), GOLD_SHADOW),
    
    # Red Shorts / Trunks Lower Leg
    ("shorts-leg-left",        "leg-left", (0.008, 0.150, -0.108), (0.184, 0.245, 0.108), SHORTS_RED),
    ("shorts-side-gold-l",     "leg-left", (0.176, 0.150, -0.050), (0.188, 0.245, 0.050), GOLD_TRIM),
]

LEG_RIGHT = [
    # Flat White Sneaker Sole Slab (Heavyweight combat sole)
    ("shoe-sole-right",        "leg-right", (-0.185, 0.000, -0.160), (-0.006, 0.028, 0.105), WHITE),
    
    # Red Sneaker Lower Body & Heel Counter
    ("shoe-upper-right",       "leg-right", (-0.180, 0.028, -0.155), (-0.010, 0.085, 0.100), FLAME_RED),
    ("shoe-toe-right",         "leg-right", (-0.180, 0.028, -0.160), (-0.010, 0.058, -0.080), FLAME_RED),
    ("shoe-heel-right",        "leg-right", (-0.180, 0.028, 0.040), (-0.010, 0.088, 0.102), FLAME_RED),
    
    # Bright Fiery Orange Sneaker Tongue Riser (Projecting in Front)
    ("shoe-tongue-right",      "leg-right", (-0.165, 0.038, -0.165), (-0.025, 0.115, -0.075), SHOE_ACCENT),
    ("shoe-collar-trim-r",     "leg-right", (-0.182, 0.075, -0.100), (-0.008, 0.095, 0.100), FLAME_RED),
    ("shoe-accent-strap-r",    "leg-right", (-0.182, 0.038, -0.100), (-0.008, 0.062, 0.100), SHOE_ACCENT),
    
    # Muscular Bare Sun-Bronze Calves (Defined muscle contour)
    ("leg-skin-right",         "leg-right", (-0.178, 0.070, -0.095), (-0.012, 0.140, 0.095), SKIN_BASE),
    ("calf-muscle-high-r",     "leg-right", (-0.172, 0.075, 0.010), (-0.020, 0.135, 0.098), SKIN_HIGHLIGHT),
    
    # Radiant Golden Thigh Trim Band (Wrapping Lower Hem of Shorts)
    ("thigh-trim-right",       "leg-right", (-0.186, 0.108, -0.112), (-0.006, 0.155, 0.112), GOLD_TRIM),
    ("thigh-trim-inner-r",     "leg-right", (-0.184, 0.112, -0.106), (-0.008, 0.132, 0.106), GOLD_SHADOW),
    
    # Red Shorts / Trunks Lower Leg
    ("shorts-leg-right",       "leg-right", (-0.184, 0.150, -0.108), (-0.008, 0.245, 0.108), SHORTS_RED),
    ("shorts-side-gold-r",     "leg-right", (-0.188, 0.150, -0.050), (-0.176, 0.245, 0.050), GOLD_TRIM),
]


# ---------------------------------------------------------------------------
# ATHLETIC MUSCULAR BRAWLER TORSO, SCULPTED PECS & PROMINENT TRAPS
# ---------------------------------------------------------------------------
TORSO = [
    # Athletic Pelvic Shorts / Saddle with defined crotch inseam cleft
    ("shorts-pelvis-main",     "torso", (-0.180, 0.195, -0.108), (0.180, 0.268, 0.108), SHORTS_RED),
    ("shorts-crotch-seam",     "torso", (-0.014, 0.192, -0.112), (0.014, 0.265, 0.112), VEST_SHADOW),
    ("shorts-side-stripe-l",   "torso", (0.172, 0.195, -0.045), (0.185, 0.268, 0.045), GOLD_TRIM),
    ("shorts-side-stripe-r",   "torso", (-0.185, 0.195, -0.045), (-0.172, 0.268, 0.045), GOLD_TRIM),
    # Pelvis Base & Crotch
    ("pelvis-crotch-red",      "torso", (-0.155, 0.240, -0.090), (0.155, 0.260, 0.050), VEST_RED),
    
    # Heavyweight Champion Gold-Trimmed Belt
    ("belt-strap-dark",        "torso", (-0.165, 0.245, -0.105), (0.165, 0.285, 0.105), BELT_BROWN),
    ("belt-gold-top-rim",      "torso", (-0.166, 0.280, -0.108), (0.166, 0.288, 0.108), GOLD_TRIM),
    ("belt-gold-bot-rim",      "torso", (-0.166, 0.242, -0.108), (0.166, 0.250, 0.108), GOLD_TRIM),
    ("belt-buckle-gold-plate", "torso", (-0.065, 0.225, -0.155), (0.065, 0.315, -0.100), GOLD_TRIM),
    ("belt-buckle-inner-core", "torso", (-0.038, 0.245, -0.158), (0.038, 0.295, -0.102), BELT_BROWN),
    ("buckle-top-bar",         "torso", (-0.060, 0.285, -0.160), (0.060, 0.310, -0.110), GOLD_TRIM),
    ("buckle-bot-bar",         "torso", (-0.060, 0.230, -0.160), (0.060, 0.255, -0.110), GOLD_TRIM),
    ("buckle-left-bar",        "torso", (0.038, 0.230, -0.160),  (0.060, 0.310, -0.110), GOLD_TRIM),
    ("buckle-right-bar",       "torso", (-0.060, 0.230, -0.160), (-0.038, 0.310, -0.110), GOLD_TRIM),
    
    # Muscular Sun-Bronze Chest & Core (Full Stature to 0.473m)
    ("torso-skin-core",        "torso", (-0.160, 0.280, -0.100), (0.160, 0.473, 0.100), SKIN_BASE),
    
    # 🏔️ PROMINENT SCULPTED TRAPEZIUS (TRAPS) MUSCLES (Rising to Elevated Neck)
    ("traps-slope-l",          "torso", (0.035, 0.420, -0.055), (0.145, 0.495, 0.080), SKIN_BASE),
    ("traps-slope-r",          "torso", (-0.145, 0.420, -0.055), (-0.035, 0.495, 0.080), SKIN_BASE),
    ("back-traps-plate-l",     "torso", (0.010, 0.405, 0.085), (0.145, 0.485, 0.125), SKIN_BASE),
    ("back-traps-plate-r",     "torso", (-0.145, 0.405, 0.085), (-0.010, 0.485, 0.125), SKIN_BASE),
    ("back-spine-cleft",       "torso", (-0.008, 0.385, 0.082), (0.008, 0.485, 0.122), SKIN_SHADOW),
    
    # Bulging Pectoral Muscle Slabs Protruding Forward
    ("chest-pec-left",         "torso", (0.008, 0.340, -0.142), (0.098, 0.458, -0.078), SKIN_BASE),
    ("chest-pec-left-upper",   "torso", (0.014, 0.400, -0.146), (0.090, 0.468, -0.082), SKIN_HIGHLIGHT),
    ("chest-pec-left-shadow",  "torso", (0.010, 0.335, -0.140), (0.094, 0.352, -0.080), SKIN_SHADOW),
    ("chest-pec-right",        "torso", (-0.098, 0.340, -0.142), (-0.008, 0.458, -0.078), SKIN_BASE),
    ("chest-pec-right-upper",  "torso", (-0.090, 0.400, -0.146), (-0.014, 0.468, -0.082), SKIN_HIGHLIGHT),
    ("chest-pec-right-shadow", "torso", (-0.094, 0.335, -0.140), (-0.010, 0.352, -0.080), SKIN_SHADOW),
    ("chest-pec-cleft",        "torso", (-0.007, 0.330, -0.134), (0.007, 0.455, -0.082), SKIN_SHADOW),
    ("neck-chest-skin",        "torso", (-0.050, 0.415, -0.110), (0.050, 0.473, -0.045), SKIN_BASE),
    
    # Abdominal Definition (Six-Pack Core)
    ("abs-upper-l",            "torso", (0.008, 0.305, -0.115), (0.065, 0.340, -0.085), SKIN_HIGHLIGHT),
    ("abs-upper-r",            "torso", (-0.065, 0.305, -0.115), (-0.008, 0.340, -0.085), SKIN_HIGHLIGHT),
    ("abs-lower-l",            "torso", (0.008, 0.270, -0.112), (0.062, 0.302, -0.085), SKIN_BASE),
    ("abs-lower-r",            "torso", (-0.062, 0.270, -0.112), (-0.008, 0.302, -0.085), SKIN_BASE),
    ("abs-linea-alba",         "torso", (-0.006, 0.268, -0.110), (0.006, 0.342, -0.085), SKIN_SHADOW),
    ("abs-crease-mid",         "torso", (-0.065, 0.298, -0.110), (0.065, 0.306, -0.086), SKIN_SHADOW),
    
    # Broad Lats & Athletic V-Taper
    ("lat-upper-l",            "torso", (0.135, 0.365, -0.045), (0.198, 0.470, 0.105), VEST_RED),
    ("lat-lower-l",            "torso", (0.115, 0.285, -0.040), (0.165, 0.365, 0.092), VEST_RED),
    ("lat-upper-r",            "torso", (-0.198, 0.365, -0.045), (-0.135, 0.470, 0.105), VEST_RED),
    ("lat-lower-r",            "torso", (-0.165, 0.285, -0.040), (-0.115, 0.365, 0.092), VEST_RED),
    
    # Open Red Chaleco Vest Panels
    ("vest-body-left",         "torso", (0.060, 0.285, -0.135), (0.195, 0.475, 0.110), VEST_RED),
    ("vest-body-right",        "torso", (-0.195, 0.285, -0.135), (-0.060, 0.475, 0.110), VEST_RED),
    
    # Solid Red Vest Back Panel
    ("vest-back-main",         "torso", (-0.190, 0.285, 0.085), (0.190, 0.475, 0.128), VEST_RED),
    
    # 🔥 Radiant Solar Fire Insignia on Back of Chaleco Vest
    ("vest-back-solar-diamond","torso", (-0.044, 0.345, 0.126), (0.044, 0.430, 0.134), GOLD_TRIM),
    ("vest-back-solar-core",   "torso", (-0.026, 0.362, 0.130), (0.026, 0.412, 0.136), FLAME_ORANGE),
    ("vest-back-solar-dot",    "torso", (-0.012, 0.376, 0.132), (0.012, 0.398, 0.138), FLAME_YELLOW),
    
    # Radiant Golden Yellow Piping Tracing the Pectoral Curves
    ("vest-trim-lapel-top-l",  "torso", (0.052, 0.415, -0.145), (0.088, 0.480, -0.102), GOLD_TRIM),
    ("vest-trim-lapel-mid-l",  "torso", (0.060, 0.345, -0.148), (0.096, 0.422, -0.106), GOLD_TRIM),
    ("vest-trim-lapel-bot-l",  "torso", (0.055, 0.285, -0.142), (0.090, 0.352, -0.102), GOLD_TRIM),
    ("vest-trim-lapel-top-r",  "torso", (-0.088, 0.415, -0.145), (-0.052, 0.480, -0.102), GOLD_TRIM),
    ("vest-trim-lapel-mid-r",  "torso", (-0.096, 0.345, -0.148), (-0.060, 0.422, -0.106), GOLD_TRIM),
    ("vest-trim-lapel-bot-r",  "torso", (-0.090, 0.285, -0.142), (-0.055, 0.352, -0.102), GOLD_TRIM),
    
    # Lower Vest Hem Band
    ("vest-hem-front-l",       "torso", (0.052, 0.280, -0.142), (0.196, 0.310, -0.100), GOLD_TRIM),
    ("vest-hem-front-r",       "torso", (-0.196, 0.280, -0.142), (-0.052, 0.310, -0.100), GOLD_TRIM),
    ("vest-hem-back",          "torso", (-0.194, 0.280, 0.090), (0.194, 0.310, 0.132), GOLD_TRIM),
    
    # Shoulder & Armhole Golden Yellow Trims
    ("vest-armhole-trim-l",    "torso", (0.185, 0.370, -0.130), (0.202, 0.480, 0.122), GOLD_TRIM),
    ("vest-armhole-trim-r",    "torso", (-0.202, 0.370, -0.130), (-0.185, 0.480, 0.122), GOLD_TRIM),
    ("vest-shoulder-trim-l",   "torso", (0.100, 0.460, -0.120), (0.198, 0.485, 0.115), GOLD_TRIM),
    ("vest-shoulder-trim-r",   "torso", (-0.198, 0.460, -0.120), (-0.100, 0.485, 0.115), GOLD_TRIM),
]


# ---------------------------------------------------------------------------
# SCULPTED ATHLETIC MUSCULAR ARMS WITH BICEPS, DELTOIDS & BRACERS
# ---------------------------------------------------------------------------
ARM_LEFT = [
    # Deltoid Shoulder Cap (Cannonball Deltoids)
    ("deltoid-core-l",         "arm-left", (0.080, 0.385, -0.085), (0.190, 0.505, 0.085), SKIN_BASE),
    ("deltoid-front-head-l",   "arm-left", (0.090, 0.390, -0.104), (0.185, 0.495, -0.050), SKIN_BASE),
    ("deltoid-rear-head-l",    "arm-left", (0.090, 0.390, 0.050), (0.185, 0.495, 0.104), SKIN_BASE),
    ("deltoid-top-head-l",     "arm-left", (0.095, 0.470, -0.075), (0.195, 0.518, 0.075), SKIN_HIGHLIGHT),
    ("deltoid-crease-l",       "arm-left", (0.095, 0.380, -0.080), (0.185, 0.392, 0.080), SKIN_SHADOW),
    
    # Arm Core / Bone Connector
    ("arm-core-l",             "arm-left", (0.145, 0.370, -0.065), (0.255, 0.485, 0.065), SKIN_BASE),
    
    # Bulging Bicep Peak (Anatomical curve dipping lower than arm core)
    ("bicep-belly-l",          "arm-left", (0.155, 0.345, -0.100), (0.250, 0.465, -0.005), SKIN_BASE),
    ("bicep-peak-high-l",      "arm-left", (0.165, 0.360, -0.108), (0.240, 0.440, -0.040), SKIN_HIGHLIGHT),
    ("bicep-peak-upper-l",     "arm-left", (0.170, 0.445, -0.092), (0.235, 0.475, -0.025), SKIN_HIGHLIGHT),
    ("bicep-lower-curve-l",    "arm-left", (0.165, 0.338, -0.085), (0.235, 0.365, -0.015), SKIN_BASE),
    ("bicep-crease-shadow-l",  "arm-left", (0.160, 0.335, -0.080), (0.240, 0.348, -0.020), SKIN_SHADOW),
    
    # Tricep Horseshoe (Rear Bulge)
    ("tricep-head-l",          "arm-left", (0.155, 0.360, 0.005), (0.250, 0.495, 0.102), SKIN_BASE),
    ("tricep-peak-high-l",     "arm-left", (0.165, 0.385, 0.045), (0.240, 0.480, 0.110), SKIN_HIGHLIGHT),
    ("tricep-peak-upper-l",    "arm-left", (0.170, 0.470, 0.035), (0.235, 0.498, 0.095), SKIN_HIGHLIGHT),
    ("tricep-undercut-l",      "arm-left", (0.160, 0.355, 0.015), (0.240, 0.375, 0.095), SKIN_SHADOW),
    
    # Tapered Forearm Combat Bracer
    ("bracer-inner-gold-l",    "arm-left", (0.242, 0.360, -0.078), (0.260, 0.498, 0.078), GOLD_TRIM),
    ("bracer-body-red-l",      "arm-left", (0.256, 0.364, -0.074), (0.320, 0.495, 0.074), VEST_RED),
    ("bracer-plate-front-l",   "arm-left", (0.270, 0.382, -0.084), (0.305, 0.478, -0.068), GOLD_TRIM),
    ("bracer-plate-shadow-l",  "arm-left", (0.278, 0.398, -0.086), (0.298, 0.462, -0.070), GOLD_SHADOW),
    ("bracer-crest-gold-l",    "arm-left", (0.265, 0.490, -0.050), (0.312, 0.510, 0.050), GOLD_TRIM),
    ("bracer-strap-orange-l",  "arm-left", (0.278, 0.360, -0.078), (0.300, 0.500, -0.065), SHOE_ACCENT),
    ("bracer-outer-gold-l",    "arm-left", (0.316, 0.360, -0.078), (0.332, 0.498, 0.078), GOLD_TRIM),
    
    # Sun-Bronze Wrist Transition & Clenched Fist (Extended Wingspan Reach to 0.415m)
    ("wrist-skin-l",           "arm-left", (0.330, 0.372, -0.060), (0.344, 0.488, 0.060), SKIN_BASE),
    ("fist-main-l",            "arm-left", (0.340, 0.370, -0.058), (0.415, 0.488, 0.058), SKIN_BASE),
    ("fist-knuckles-high-l",   "arm-left", (0.375, 0.400, -0.062), (0.418, 0.480, 0.054), SKIN_HIGHLIGHT),
    ("fist-knuckle-seam-l",    "arm-left", (0.390, 0.405, -0.064), (0.398, 0.475, 0.052), SKIN_SHADOW),
    ("fist-thumb-l",           "arm-left", (0.348, 0.362, -0.065), (0.390, 0.418, -0.020), SKIN_BASE),
]

ARM_RIGHT = [
    # Deltoid Shoulder Cap (Cannonball Deltoids)
    ("deltoid-core-r",         "arm-right", (-0.190, 0.385, -0.085), (-0.080, 0.505, 0.085), SKIN_BASE),
    ("deltoid-front-head-r",   "arm-right", (-0.185, 0.390, -0.104), (-0.090, 0.495, -0.050), SKIN_BASE),
    ("deltoid-rear-head-r",    "arm-right", (-0.185, 0.390, 0.050), (-0.090, 0.495, 0.104), SKIN_BASE),
    ("deltoid-top-head-r",     "arm-right", (-0.195, 0.470, -0.075), (-0.095, 0.518, 0.075), SKIN_HIGHLIGHT),
    ("deltoid-crease-r",       "arm-right", (-0.185, 0.390, -0.080), (-0.095, 0.392, 0.080), SKIN_SHADOW),
    
    # Arm Core / Bone Connector
    ("arm-core-r",             "arm-right", (-0.255, 0.370, -0.065), (-0.145, 0.485, 0.065), SKIN_BASE),
    
    # Bulging Bicep Peak (Anatomical curve dipping lower than arm core)
    ("bicep-belly-r",          "arm-right", (-0.250, 0.345, -0.100), (-0.155, 0.465, -0.005), SKIN_BASE),
    ("bicep-peak-high-r",      "arm-right", (-0.240, 0.360, -0.108), (-0.165, 0.440, -0.040), SKIN_HIGHLIGHT),
    ("bicep-peak-upper-r",     "arm-right", (-0.235, 0.445, -0.092), (-0.170, 0.475, -0.025), SKIN_HIGHLIGHT),
    ("bicep-lower-curve-r",    "arm-right", (-0.235, 0.338, -0.085), (-0.165, 0.365, -0.015), SKIN_BASE),
    ("bicep-crease-shadow-r",  "arm-right", (-0.240, 0.335, -0.080), (-0.160, 0.348, -0.020), SKIN_SHADOW),
    
    # Tricep Horseshoe (Rear Bulge)
    ("tricep-head-r",          "arm-right", (-0.250, 0.360, 0.005), (-0.155, 0.495, 0.102), SKIN_BASE),
    ("tricep-peak-high-r",     "arm-right", (-0.240, 0.385, 0.045), (-0.165, 0.480, 0.110), SKIN_HIGHLIGHT),
    ("tricep-peak-upper-r",    "arm-right", (-0.235, 0.470, 0.035), (-0.170, 0.498, 0.095), SKIN_HIGHLIGHT),
    ("tricep-undercut-r",      "arm-right", (-0.240, 0.355, 0.015), (-0.160, 0.375, 0.095), SKIN_SHADOW),
    
    # Tapered Forearm Combat Bracer
    ("bracer-inner-gold-r",    "arm-right", (-0.260, 0.360, -0.078), (-0.242, 0.498, 0.078), GOLD_TRIM),
    ("bracer-body-red-r",      "arm-right", (-0.320, 0.364, -0.074), (-0.256, 0.495, 0.074), VEST_RED),
    ("bracer-plate-front-r",   "arm-right", (-0.305, 0.382, -0.084), (-0.270, 0.478, -0.068), GOLD_TRIM),
    ("bracer-plate-shadow-r",  "arm-right", (-0.298, 0.398, -0.086), (-0.278, 0.462, -0.070), GOLD_SHADOW),
    ("bracer-crest-gold-r",    "arm-right", (-0.312, 0.490, -0.050), (-0.265, 0.510, 0.050), GOLD_TRIM),
    ("bracer-strap-orange-r",  "arm-right", (-0.300, 0.360, -0.078), (-0.278, 0.500, -0.065), SHOE_ACCENT),
    ("bracer-outer-gold-r",    "arm-right", (-0.332, 0.360, -0.078), (-0.316, 0.498, 0.078), GOLD_TRIM),
    
    # Sun-Bronze Wrist Transition & Clenched Fist (Extended Wingspan Reach to 0.415m)
    ("wrist-skin-r",           "arm-right", (-0.344, 0.372, -0.060), (-0.330, 0.488, 0.060), SKIN_BASE),
    ("fist-main-r",            "arm-right", (-0.415, 0.370, -0.058), (-0.340, 0.488, 0.058), SKIN_BASE),
    ("fist-knuckles-high-r",   "arm-right", (-0.418, 0.400, -0.062), (-0.375, 0.480, 0.054), SKIN_HIGHLIGHT),
    ("fist-knuckle-seam-r",    "arm-right", (-0.398, 0.405, -0.064), (-0.390, 0.475, 0.052), SKIN_SHADOW),
    ("fist-thumb-r",           "arm-right", (-0.390, 0.362, -0.065), (-0.348, 0.418, -0.020), SKIN_BASE),
]

ARM_DECALS = []
BODY_PANELS = []


# ---------------------------------------------------------------------------
# HEAD: FULL SIZE CRANIUM + MOHAWK AT VERY TIP OF HEAD (ITER 51 STATURE + ITER 54 MOHAWK)
# ---------------------------------------------------------------------------
HEAD = [
    # =========================================================================
    # 1. ⚔️ SHAVED TEMPLE UNDERCUT & GOLDEN RAZOR SLITS (// Street Brawler Cut)
    # =========================================================================
    # Subtle Buzz Fade Step on Shaved Temple (Left +X)
    ("fade-temple-l",          "head", (0.150, 0.600, -0.080), (0.165, 0.720, 0.060), SKIN_SHADOW),
    ("razor-slit-fwd-l",       "head", (0.162, 0.620, -0.065), (0.174, 0.645, -0.020), GOLD_TRIM),
    ("razor-slit-mid-l",       "head", (0.162, 0.650, -0.015), (0.174, 0.675, 0.030), GOLD_TRIM),
    ("razor-slit-aft-l",       "head", (0.162, 0.680, 0.035), (0.174, 0.705, 0.080), GOLD_TRIM),
    
    # Subtle Buzz Fade Step on Shaved Temple (Right -X)
    ("fade-temple-r",          "head", (-0.165, 0.600, -0.080), (-0.150, 0.720, 0.060), SKIN_SHADOW),
    ("razor-slit-fwd-r",       "head", (-0.174, 0.620, -0.065), (-0.162, 0.645, -0.020), GOLD_TRIM),
    ("razor-slit-mid-r",       "head", (-0.174, 0.650, -0.015), (-0.162, 0.675, 0.030), GOLD_TRIM),
    ("razor-slit-aft-r",       "head", (-0.174, 0.680, 0.035), (-0.162, 0.705, 0.080), GOLD_TRIM),

    # =========================================================================
    # 2. 💥 TOP-OF-HEAD SPIKY FLAME MOHAWK (Sitting on Very Tip of Cranium -> Rear Nape)
    # =========================================================================
    # --- A. MOHAWK SAGITTAL BASE (Covering Top Cranium Bevel & Rear Ridge) ---
    ("hawk-base-top-front",    "head", (-0.055, 0.745, -0.145), (0.055, 0.818, 0.070), HAIR_BLACK),
    ("hawk-base-rear-occipital","head",(-0.044, 0.620, 0.040), (0.044, 0.798, 0.174), HAIR_BLACK),
    ("hawk-base-nape",         "head", (-0.038, 0.500, 0.100), (0.038, 0.655, 0.176), HAIR_BLACK),
    ("hawk-base-nape-tail",    "head", (-0.024, 0.430, 0.115), (0.024, 0.530, 0.174), HAIR_BLACK),

    # --- B. 3-TONE FLAME FIN RISING AT THE VERY TIP OF HIS HEAD (Z in [-0.140, +0.065]) ---
    # Crimson Red Flame Base
    ("hawk-flame-top-red",     "head", (-0.050, 0.765, -0.140), (0.050, 0.822, 0.050), FLAME_RED),
    # Solar Orange Mid-Flame
    ("hawk-flame-top-orange",  "head", (-0.036, 0.782, -0.125), (0.036, 0.834, 0.030), FLAME_ORANGE),
    # Radiant Solar Yellow Pinnacle Crest (Apex raised up to 0.848m)
    ("hawk-flame-yellow-crest","head", (-0.022, 0.795, -0.110), (0.022, 0.848, 0.015), FLAME_YELLOW),
    ("hawk-flame-needle-fwd",  "head", (-0.014, 0.805, -0.120), (0.014, 0.848, -0.045), FLAME_YELLOW),
    ("hawk-flame-needle-mid",  "head", (-0.014, 0.808, -0.045), (0.014, 0.848, 0.015), FLAME_YELLOW),
    ("hawk-flame-needle-aft",  "head", (-0.014, 0.805, 0.015), (0.014, 0.845, 0.065), FLAME_YELLOW),
    ("hawk-flame-needle-peak", "head", (-0.010, 0.820, -0.080), (0.010, 0.848, -0.010), FLAME_YELLOW),

    # --- C. REAR SPIKY ACCENTS (Visible in Back & 3/4 views) ---
    ("hawk-rear-spike-tooth",  "head", (-0.020, 0.670, 0.070), (0.020, 0.790, 0.176), HAIR_BLACK),
    ("hawk-rear-red",          "head", (-0.014, 0.690, 0.080), (0.014, 0.770, 0.170), FLAME_RED),
]

DONOR_SPACE = tuple(entry[0] for entry in HEAD)


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
# THE DONATED HEAD & DANTE-TIER STYLIZED ANIME BRAWLER FACE
# ---------------------------------------------------------------------------
DONOR_SKULL = "Assets/TumbangPreso/Art/characters/persons/character-male-b.glb"
SKULL_SLOTS = {15: SKIN_BASE, 8: None}

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
                    # Shift position in Y to align with elevated neck
                    p = src_pos[i]
                    pos.append((p[0], p[1] + HEAD_SHIFT_Y, p[2]))
                    nrm.append(src_nrm[i])
                    uv.append(cell_uv(paint) if paint is not None else src_uv[i])

            tris.append(tuple(remap[i] for i in tri))

        if not tris:
            raise SystemExit(f"{path} has no triangles in slots {sorted(slots)}")

        return pos, nrm, uv, tris

    raise SystemExit(f"{path} has no head-mesh")


def _compact(pos, nrm, uv, tris):
    used = sorted({i for t in tris for i in t})
    remap = {i: k for k, i in enumerate(used)}
    return ([pos[i] for i in used], [nrm[i] for i in used], [uv[i] for i in used],
            [tuple(remap[i] for i in t) for t in tris])


def _donor_head():
    """Builds the donor skull with fierce slanted brawler eyes and cocky smirk (pure INK cast standard)."""
    pos, nrm, uv, tris = _donor_part(DONOR_SKULL, SKULL_SLOTS)

    # Strip donor mouth and eyes
    tris = [t for t in tris if not (_slot_at(*uv[t[0]]) == INK)]

    def add_quad(points_bl_tl_tr_br, slot, z_offset):
        bl, tl, tr, br = points_bl_tl_tr_br
        z = MOUTH_Z + z_offset
        first_idx = len(pos)
        pos.append((bl[0], bl[1] + HEAD_SHIFT_Y, z))
        pos.append((tl[0], tl[1] + HEAD_SHIFT_Y, z))
        pos.append((tr[0], tr[1] + HEAD_SHIFT_Y, z))
        pos.append((br[0], br[1] + HEAD_SHIFT_Y, z))
        for _ in range(4):
            nrm.append((0.0, 0.0, 1.0))
            uv.append(cell_uv(slot))
        tris.append((first_idx, first_idx + 3, first_idx + 2))
        tris.append((first_idx, first_idx + 2, first_idx + 1))

    # 1. ⚔️ CONFIDENT COCKY BRAWLER SMIRK (Raised to balance jaw proportion)
    # Main smirk stroke angling up to +X
    add_quad([(-0.042, 0.426), (-0.042, 0.436), (0.040, 0.444), (0.040, 0.434)],
             INK, PANEL_PROUD * 2.2)
    # Cocky Upturned Hook at Corner (+X, Viewer's Right)
    add_quad([(0.036, 0.434), (0.036, 0.458), (0.048, 0.462), (0.048, 0.440)],
             INK, PANEL_PROUD * 2.2)
    # Left Corner Firm Down-tick (-X anchor)
    add_quad([(-0.048, 0.420), (-0.048, 0.430), (-0.040, 0.436), (-0.040, 0.426)],
             INK, PANEL_PROUD * 2.2)

    # 2. 😠 RAISED FIERCE SLANTED-TOP BRAWLER EYES (Eliminating huge forehead gap)
    # --- RIGHT EYE (-X, Viewer's Left: Slanted top, rounded bottom & outer) ---
    # Center Body
    add_quad([(-0.096, 0.478), (-0.104, 0.510), (-0.044, 0.502), (-0.048, 0.478)],
             INK, PANEL_PROUD * 2.6)
    # Upper Slanted Brow Flap (Sharp aggressive inward slant \)
    add_quad([(-0.104, 0.510), (-0.098, 0.532), (-0.040, 0.512), (-0.044, 0.502)],
             INK, PANEL_PROUD * 2.6)
    # Outer Rounded Lateral Curve
    add_quad([(-0.112, 0.492), (-0.112, 0.518), (-0.104, 0.518), (-0.104, 0.492)],
             INK, PANEL_PROUD * 2.6)
    # Bottom Rounded Chin-Facing Curve
    add_quad([(-0.088, 0.472), (-0.088, 0.478), (-0.056, 0.478), (-0.056, 0.472)],
             INK, PANEL_PROUD * 2.6)

    # --- LEFT EYE (+X, Viewer's Right: Slanted top, rounded bottom & outer) ---
    # Center Body
    add_quad([(0.048, 0.478), (0.044, 0.502), (0.104, 0.510), (0.096, 0.478)],
             INK, PANEL_PROUD * 2.6)
    # Upper Slanted Brow Flap (Sharp aggressive inward slant /)
    add_quad([(0.044, 0.502), (0.040, 0.512), (0.098, 0.532), (0.104, 0.510)],
             INK, PANEL_PROUD * 2.6)
    # Outer Rounded Lateral Curve
    add_quad([(0.104, 0.492), (0.104, 0.518), (0.112, 0.518), (0.112, 0.492)],
             INK, PANEL_PROUD * 2.6)
    # Bottom Rounded Chin-Facing Curve
    add_quad([(0.056, 0.472), (0.056, 0.478), (0.088, 0.478), (0.088, 0.472)],
             INK, PANEL_PROUD * 2.6)

    return _compact(pos, nrm, uv, tris)


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

            if channel["target"]["path"] != "translation" or delta is None                     or delta == (0.0, 0.0, 0.0):
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
