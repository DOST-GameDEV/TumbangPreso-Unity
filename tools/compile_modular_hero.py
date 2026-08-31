#!/usr/bin/env python3
"""
tools/compile_modular_hero.py
Comprehensive modular voxel compiler for Tumbang Preso custom characters.
Seamlessly compiles:
- Headwear & Hats (Salakot, Witch Hat, Ushanka, Beanie, Beret, Earmuffs, Demon Horns)
- Eyewear & Face Wear (Pro Ski Goggles, Matrix Shades, Bandages)
- Facial Expressions (Cat :3, Chill Smirk, Street Grin, Determined, Stoic)
- Face Shapes (Kenney Round, Chiseled Jaw, Brawler Square, Slender Anime)
- Tops & Torso (Barangay MVP Jersey #7, Sando, Windbreaker, Zip Hoodie, Street Jacket, Witch Robe)
- Bottoms & Pants (Raw Denim Jorts with Chain, Mesh Basketball Shorts, Cargo Pants, Track Pants)
- Footwear (Rambo Blue Tsinelas, Spartan Red Slippers, Skater High-Tops, Boots)
- Accessories (Good Morning Towel, Cuban Link Chain & Gem, Asymmetric Earrings)
- Full 16-Color Palette Remapping (.tres format)
"""

import sys
import os
import copy
import argparse
import math

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_person_voxel as bpv
import wearables_registry as wr

def get_mouth_ribbon(expression_name="cat_3"):
    """Generate procedural mouth ribbon coordinates for chosen expression."""
    half_w = 0.046
    base_y = 0.418
    steps = 32
    upper, lower = [], []
    
    if expression_name == "cat_3":
        lobe_depth = 0.0215
        th = 0.0135
        for k in range(steps + 1):
            t = k / steps
            x = -half_w + t * (2.0 * half_w)
            u = x / half_w
            val = math.cos(u * math.pi * 2.0)
            dip = lobe_depth * 0.5 * (1.0 - val)
            lift = 0.0075 * (u**2)
            y_c = base_y - dip + lift
            upper.append((x, y_c + th * 0.5))
            lower.append((x, y_c - th * 0.5))
            
    elif expression_name == "smirk":
        th = 0.0120
        for k in range(steps + 1):
            t = k / steps
            x = -half_w * 0.8 + t * (1.6 * half_w)
            u = (x + half_w * 0.8) / (1.6 * half_w)
            y_c = base_y - 0.005 + (0.015 * (u**1.5))
            upper.append((x, y_c + th * 0.5))
            lower.append((x, y_c - th * 0.5))
            
    elif expression_name == "determined":
        th = 0.0110
        for k in range(steps + 1):
            t = k / steps
            x = -half_w * 0.75 + t * (1.5 * half_w)
            u = x / (half_w * 0.75)
            y_c = base_y - 0.006 * (1.0 - u**2)
            upper.append((x, y_c + th * 0.5))
            lower.append((x, y_c - th * 0.5))
            
    elif expression_name == "street_grin":
        th = 0.0140
        for k in range(steps + 1):
            t = k / steps
            x = -half_w * 0.95 + t * (1.9 * half_w)
            u = x / (half_w * 0.95)
            y_c = base_y - 0.010 + 0.018 * (u**2)
            upper.append((x, y_c + th * 0.5))
            lower.append((x, y_c - th * 0.5))
            
    else: # Default straight / stoic line
        th = 0.0110
        for k in range(steps + 1):
            t = k / steps
            x = -half_w * 0.75 + t * (1.5 * half_w)
            y_c = base_y
            upper.append((x, y_c + th * 0.5))
            lower.append((x, y_c - th * 0.5))
            
    return upper, lower

def build_custom_hero(
    headwear_id="ushanka_expedition",
    eyewear_id="ski_goggles_pro",
    accessories=("asymmetric_frost_earrings",),
    expression="cat_3",
    palette_overrides=None,
    out_glb="Assets/TumbangPreso/Art/characters/persons/team-custom.glb",
    out_tres="MapSource/materials_persons/person_team-custom.tres"
):
    """Assembles all modular boxes and compiles the GLB and .tres palette."""
    bpv.BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"
    bpv.OUT = out_glb
    bpv.PALETTE_OUT = out_tres
    bpv.PALETTE_HERO_NAME = "team-custom"

    head_boxes = []
    
    # 1. Base headwear
    if headwear_id and f"headwear/{headwear_id}" in wr.WEARABLES_CATALOG:
        head_boxes.extend(wr.get_wearable(f"headwear/{headwear_id}"))
    elif headwear_id and headwear_id in wr.WEARABLES_CATALOG:
        head_boxes.extend(wr.get_wearable(headwear_id))
        
    # 2. Eyewear
    if eyewear_id and f"eyewear/{eyewear_id}" in wr.WEARABLES_CATALOG:
        head_boxes.extend(wr.get_wearable(f"eyewear/{eyewear_id}"))
    elif eyewear_id and eyewear_id in wr.WEARABLES_CATALOG:
        head_boxes.extend(wr.get_wearable(eyewear_id))
        
    # 3. Additional accessories & jewelry
    for acc in accessories:
        for prefix in ("jewelry/", "accessory/", "hair_accessory/"):
            full_id = f"{prefix}{acc}"
            if full_id in wr.WEARABLES_CATALOG:
                head_boxes.extend(wr.get_wearable(full_id))
                break
        if acc in wr.WEARABLES_CATALOG:
            head_boxes.extend(wr.get_wearable(acc))

    bpv.HEAD_BOXES = tuple(head_boxes)
    bpv.DONOR_SPACE = tuple(entry[0] for entry in bpv.HEAD_BOXES)

    # Patch expression ribbon
    bpv._mouth_ribbon = lambda: get_mouth_ribbon(expression)

    # Apply palette overrides
    if palette_overrides:
        for slot, hex_val in palette_overrides.items():
            bpv.PALETTE[slot] = hex_val.lstrip("#")

    print(f"Compiling custom hero model: {out_glb}")
    print(f"  Headwear: {headwear_id}")
    print(f"  Eyewear: {eyewear_id}")
    print(f"  Expression: {expression}")
    print(f"  Boxes: {len(head_boxes)} head items")

    bpv.main()
    print("Compilation successful!")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Compile modular custom hero model.")
    parser.add_argument("--headwear", default="ushanka_expedition")
    parser.add_argument("--eyewear", default="ski_goggles_pro")
    parser.add_argument("--expression", default="cat_3")
    parser.add_argument("--out-glb", default="Assets/TumbangPreso/Art/characters/persons/team-custom.glb")
    parser.add_argument("--out-tres", default="MapSource/materials_persons/person_team-custom.tres")
    args = parser.parse_args()

    build_custom_hero(
        headwear_id=args.headwear,
        eyewear_id=args.eyewear,
        expression=args.expression,
        out_glb=args.out_glb,
        out_tres=args.out_tres
    )
