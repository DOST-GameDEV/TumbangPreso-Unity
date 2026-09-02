"""Canonical In-Engine 3D Modular Voxel Hero Builder.
Uses the official donor mesh architecture (_donor_part) from build_person_voxel.py
so the custom hero shares the exact same skull, eyes, facial expressions, and proportions
as Sean, Dante, Cheska, Zack, Nemu, and Phaister.
"""
import os
import sys
import copy
import struct
import json
import math

sys.path.insert(0, "tools")
import build_person_voxel as bpv
from glb_mesh_dump import read_glb, read_accessor

BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"
DONOR_MALE = "Assets/TumbangPreso/Art/characters/persons/character-male-a.glb"
DONOR_FEMALE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"
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

def build_canonical_custom_hero():
    # Use bpv's own pipeline to generate official custom hero
    bpv.BASE = BASE
    bpv.OUT = OUT
    bpv.PALETTE_OUT = PALETTE_OUT
    bpv.PALETTE_HERO_NAME = "team-custom"

    # Custom palette with Kayumanggi skin, Red Jersey, Denim shorts, Jet black hair
    bpv.PALETTE = {
        0: "FFFFFF", # White / bandage / sole
        1: "C8C8DC", # Silver chain
        2: "1A56DB", # Blue slipper strap
        3: "1A56DB",
        4: "D42828", # Red Jersey #7
        5: "A01E1E", # Red Jersey Dark
        6: "FFBA00", # Gold Trim & #7
        7: "375073", # Denim Jorts
        8: "141416", # Ink eyes / mouth
        9: "22344B", # Denim Dark
        10: "FFBA00",
        11: "141416", # Jet Black Hair
        12: "3C3C41",
        13: "C88A52", # Classic Kayumanggi Skin
        14: "A06E3D", # Skin Dark
        15: "E6A86E", # Skin Lit
    }

    print("Building canonical custom hero GLB with official donor anatomy...")
    bpv.main()
    print("Done!")

if __name__ == "__main__":
    build_canonical_custom_hero()
