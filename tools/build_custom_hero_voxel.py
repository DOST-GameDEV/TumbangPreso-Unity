"""Modular Voxel Person Builder for Tumbang Preso Custom Characters.
Builds genuine, game-ready team-custom.glb models with all 32 animations,
16-color palette mapping, and canonical game proportions (24% legs, 23% torso, 53% head).
"""
import os
import sys
import copy
import struct
import json
import math

sys.path.insert(0, "tools")
import build_person_voxel as bpv

BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"
OUT = "Assets/TumbangPreso/Art/characters/persons/team-custom.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-custom.tres"

# Palette Slots
WHITE = 0
SILVER = 1
CYAN_TRIM = 2
ACCENT = 6
INK = 8
TOP = 4
TOP_DARK = 5
TOP_LIT = 6
BOTTOM = 7
BOTTOM_DARK = 9
BOTTOM_LIT = 10
HAIR = 11
HAIR_DARK = 8
HAIR_LIT = 12
SKIN = 13
SKIN_DARK = 14
SKIN_LIT = 15

# --- 1. HEAD & FACIAL EXPRESSIONS ---
def get_head_boxes(expression=0, marking=1):
    boxes = [
        ("head-core", "head", (-0.165, 0.445, -0.165), (0.165, 0.720, 0.145), SKIN),
        ("ear-l", "head", (0.165, 0.520, -0.035), (0.195, 0.590, 0.035), SKIN),
        ("ear-r", "head", (-0.195, 0.520, -0.035), (-0.165, 0.590, 0.035), SKIN),
    ]

    if expression == 1: # Determined / Brawler
        boxes += [
            ("eye-brow-l", "head", (0.035, 0.595, 0.148), (0.115, 0.615, 0.158), INK),
            ("eye-brow-r", "head", (-0.115, 0.595, 0.148), (-0.035, 0.615, 0.158), INK),
            ("eye-l", "head", (0.045, 0.540, 0.146), (0.105, 0.590, 0.155), INK),
            ("eye-r", "head", (-0.105, 0.540, 0.146), (-0.045, 0.590, 0.155), INK),
            ("mouth-smirk", "head", (-0.040, 0.470, 0.146), (0.060, 0.490, 0.155), INK),
        ]
    elif expression == 2: # Cat :3 Smile
        boxes += [
            ("eye-l", "head", (0.045, 0.550, 0.146), (0.095, 0.600, 0.155), INK),
            ("eye-r", "head", (-0.095, 0.550, 0.146), (-0.045, 0.600, 0.155), INK),
            ("mouth-w", "head", (-0.050, 0.465, 0.146), (0.050, 0.495, 0.155), INK),
        ]
    else: # Street Chill Grin
        boxes += [
            ("eye-l", "head", (0.045, 0.540, 0.146), (0.105, 0.600, 0.155), INK),
            ("eye-r", "head", (-0.105, 0.540, 0.146), (-0.045, 0.600, 0.155), INK),
            ("eye-glint-l", "head", (0.055, 0.570, 0.156), (0.075, 0.595, 0.160), WHITE),
            ("eye-glint-r", "head", (-0.095, 0.570, 0.156), (-0.075, 0.595, 0.160), WHITE),
            ("mouth-grin", "head", (-0.055, 0.470, 0.146), (0.055, 0.495, 0.155), INK),
        ]

    if marking == 1:
        boxes.append(("bandage", "head", (0.060, 0.490, 0.148), (0.125, 0.520, 0.158), WHITE))
    elif marking == 2:
        boxes.append(("nose-strip", "head", (-0.035, 0.520, 0.148), (0.035, 0.545, 0.158), WHITE))
    elif marking == 3:
        boxes.append(("scar", "head", (0.060, 0.470, 0.148), (0.080, 0.580, 0.158), ACCENT))

    return boxes

# --- 2. HAIRSTYLES ---
def get_hair_boxes(style=0):
    if style == 1: # Kalye Wolf Cut
        return [
            ("hair-crown", "head", (-0.180, 0.690, -0.180), (0.180, 0.745, 0.160), HAIR),
            ("hair-shag-l", "head", (0.165, 0.430, -0.140), (0.195, 0.690, 0.110), HAIR),
            ("hair-shag-r", "head", (-0.195, 0.430, -0.140), (-0.165, 0.690, 0.110), HAIR),
            ("hair-nape", "head", (-0.160, 0.380, -0.190), (0.160, 0.490, -0.150), HAIR),
            ("hair-bangs-messy", "head", (-0.150, 0.630, 0.146), (0.150, 0.710, 0.175), HAIR),
        ]
    elif style == 2: # Sean Spiky Quiff
        return [
            ("hair-quiff-base", "head", (-0.175, 0.680, -0.175), (0.175, 0.740, 0.155), HAIR),
            ("hair-quiff-crest", "head", (-0.060, 0.720, -0.050), (0.060, 0.775, 0.165), ACCENT),
            ("hair-side-l", "head", (0.165, 0.500, -0.120), (0.190, 0.690, 0.120), HAIR),
            ("hair-side-r", "head", (-0.190, 0.500, -0.120), (-0.165, 0.690, 0.120), HAIR),
        ]
    elif style == 3: # Slouchy Beanie
        return [
            ("beanie-brim", "head", (-0.180, 0.620, -0.180), (0.180, 0.670, 0.160), BOTTOM_DARK),
            ("beanie-dome", "head", (-0.175, 0.665, -0.190), (0.175, 0.755, 0.145), BOTTOM_DARK),
            ("beanie-slouch", "head", (-0.140, 0.660, -0.240), (0.140, 0.730, -0.175), BOTTOM_DARK),
            ("bangs-under", "head", (-0.150, 0.590, 0.146), (0.150, 0.630, 0.165), HAIR),
        ]
    else: # 90s Curtains
        return [
            ("hair-crown", "head", (-0.175, 0.700, -0.175), (0.175, 0.745, 0.155), HAIR),
            ("hair-back", "head", (-0.175, 0.460, -0.185), (0.175, 0.720, -0.155), HAIR),
            ("hair-curtain-l", "head", (0.020, 0.610, 0.146), (0.175, 0.720, 0.175), HAIR),
            ("hair-curtain-drop-l", "head", (0.120, 0.530, 0.135), (0.175, 0.620, 0.170), HAIR),
            ("hair-curtain-r", "head", (-0.175, 0.610, 0.146), (-0.020, 0.720, 0.175), HAIR),
            ("hair-curtain-drop-r", "head", (-0.175, 0.530, 0.135), (-0.120, 0.620, 0.170), HAIR),
        ]

# --- 3. TOPS & STREETWEAR ---
def get_top_boxes(style=0):
    if style == 1: # Classic White Sando
        return [
            ("torso-tank", "torso", (-0.145, 0.230, -0.095), (0.145, 0.400, 0.095), TOP),
            ("strap-l", "torso", (0.080, 0.400, -0.090), (0.140, 0.445, 0.090), TOP),
            ("strap-r", "torso", (-0.140, 0.400, -0.090), (-0.080, 0.445, 0.090), TOP),
            ("chest-bare", "torso", (-0.080, 0.380, 0.070), (0.080, 0.445, 0.096), SKIN),
            ("neck", "torso", (-0.060, 0.400, -0.060), (0.060, 0.445, 0.060), SKIN),
            ("arm-l-bare", "arm-left", (-0.045, 0.230, -0.065), (0.045, 0.430, 0.065), SKIN),
            ("arm-r-bare", "arm-right", (-0.045, 0.230, -0.065), (0.045, 0.430, 0.065), SKIN),
        ]
    elif style == 2: # Nemu Bell-Sleeve Hoodie
        return [
            ("torso-hoodie", "torso", (-0.160, 0.210, -0.110), (0.160, 0.410, 0.110), TOP),
            ("hoodie-cowl", "torso", (-0.140, 0.380, -0.150), (0.140, 0.445, -0.080), TOP_DARK),
            ("bell-sleeve-l", "arm-left", (-0.065, 0.180, -0.085), (0.065, 0.430, 0.085), TOP),
            ("bell-sleeve-r", "arm-right", (-0.065, 0.180, -0.085), (0.065, 0.430, 0.085), TOP),
            ("sleeve-trim-l", "arm-left", (-0.070, 0.175, -0.090), (0.070, 0.205, 0.090), ACCENT),
            ("sleeve-trim-r", "arm-right", (-0.070, 0.175, -0.090), (0.070, 0.205, 0.090), ACCENT),
        ]
    else: # Barangay MVP Jersey #7
        return [
            ("torso-jersey", "torso", (-0.150, 0.230, -0.100), (0.150, 0.410, 0.100), TOP),
            ("jersey-trim-neck", "torso", (-0.080, 0.410, -0.080), (0.080, 0.445, 0.080), ACCENT),
            ("jersey-num-7", "torso", (-0.040, 0.280, 0.098), (0.040, 0.380, 0.105), ACCENT),
            ("neck", "torso", (-0.060, 0.410, -0.060), (0.060, 0.445, 0.060), SKIN),
            ("arm-l-bare", "arm-left", (-0.045, 0.230, -0.065), (0.045, 0.430, 0.065), SKIN),
            ("arm-r-bare", "arm-right", (-0.045, 0.230, -0.065), (0.045, 0.430, 0.065), SKIN),
            ("sweatband-r", "arm-right", (-0.050, 0.260, -0.070), (0.050, 0.300, 0.070), TOP),
        ]

# --- 4. SHORTS & BOTTOMS ---
def get_bottom_boxes(style=0):
    if style == 1: # Mesh Basketball Shorts
        return [
            ("hips-mesh", "root", (-0.150, 0.180, -0.100), (0.150, 0.235, 0.100), BOTTOM),
            ("leg-l-mesh", "leg-left", (-0.070, 0.080, -0.080), (0.070, 0.190, 0.080), BOTTOM),
            ("stripe-l", "leg-left", (0.065, 0.080, -0.030), (0.075, 0.190, 0.030), ACCENT),
            ("leg-l-bare", "leg-left", (-0.050, 0.025, -0.060), (0.050, 0.080, 0.060), SKIN),
            ("leg-r-mesh", "leg-right", (-0.070, 0.080, -0.080), (0.070, 0.190, 0.080), BOTTOM),
            ("stripe-r", "leg-right", (-0.075, 0.080, -0.030), (-0.065, 0.190, 0.030), ACCENT),
            ("leg-r-bare", "leg-right", (-0.050, 0.025, -0.060), (0.050, 0.080, 0.060), SKIN),
        ]
    else: # Denim Jorts
        return [
            ("hips-denim", "root", (-0.145, 0.180, -0.095), (0.145, 0.235, 0.095), BOTTOM),
            ("leg-l-denim", "leg-left", (-0.065, 0.090, -0.075), (0.065, 0.190, 0.075), BOTTOM),
            ("leg-l-bare", "leg-left", (-0.050, 0.025, -0.060), (0.050, 0.090, 0.060), SKIN),
            ("leg-r-denim", "leg-right", (-0.065, 0.090, -0.075), (0.065, 0.190, 0.075), BOTTOM),
            ("leg-r-bare", "leg-right", (-0.050, 0.025, -0.060), (0.050, 0.090, 0.060), SKIN),
        ]

# --- 5. FOOTWEAR ---
def get_footwear_boxes(style=0):
    return [
        ("footbed-l", "leg-left", (-0.060, 0.000, -0.090), (0.060, 0.025, 0.090), WHITE),
        ("strap-l", "leg-left", (-0.055, 0.020, -0.020), (0.055, 0.045, 0.050), CYAN_TRIM),
        ("footbed-r", "leg-right", (-0.060, 0.000, -0.090), (0.060, 0.025, 0.090), WHITE),
        ("strap-r", "leg-right", (-0.055, 0.020, -0.020), (0.055, 0.045, 0.050), CYAN_TRIM),
    ]

# --- 6. ACCESSORIES ---
def get_accessories_boxes(has_chain=True, has_salakot=False):
    acc = []
    if has_chain:
        acc.append(("chain-collar", "torso", (-0.075, 0.385, 0.080), (0.075, 0.405, 0.102), SILVER))
    if has_salakot:
        acc += [
            ("salakot-brim", "head", (-0.230, 0.685, -0.230), (0.230, 0.705, 0.230), ACCENT),
            ("salakot-cone", "head", (-0.140, 0.705, -0.140), (0.140, 0.745, 0.140), ACCENT),
            ("salakot-knob", "head", (-0.025, 0.745, -0.025), (0.025, 0.760, 0.025), TOP_DARK),
        ]
    return acc

def assemble_model(config):
    head_boxes = get_head_boxes(config.get("exp", 0), config.get("mark", 1))
    if not config.get("salakot", False):
        head_boxes += get_hair_boxes(config.get("hair", 0))

    body_boxes = get_top_boxes(config.get("top", 0)) + get_bottom_boxes(config.get("bot", 0)) + get_footwear_boxes(config.get("shoes", 0)) + get_accessories_boxes(config.get("chain", True), config.get("salakot", False))

    bpv.HEAD_BOXES = head_boxes
    bpv.BODY_BOXES = body_boxes
    bpv.BASE = BASE
    bpv.OUT = OUT
    bpv.PALETTE_OUT = PALETTE_OUT

    bpv.PALETTE = {
        0: "FFFFFF",
        1: "C8C8DC",
        2: "1A56DB",
        3: "1A56DB",
        4: config.get("top_col", "D42828"),
        5: "A01E1E",
        6: "FFBA00",
        7: config.get("bot_col", "375073"),
        8: "141416",
        9: "22344B",
        10: "FFBA00",
        11: config.get("hair_col", "141416"),
        12: "3C3C41",
        13: config.get("skin_col", "C88A52"),
        14: "A06E3D",
        15: "E6A86E",
    }

    bpv.main()

if __name__ == "__main__":
    assemble_model({
        "hair": 1,
        "top": 1,
        "bot": 1,
        "exp": 1,
        "mark": 2,
        "chain": True,
        "salakot": True
    })
