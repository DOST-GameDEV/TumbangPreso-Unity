"""Generates 4 BIG VOLUMETRIC, Flawlessly Wrapped Headpieces with Radiant Matched Skin Tone."""
import os
import sys
import copy

sys.path.insert(0, "tools")
import build_person_voxel as bpv

# 4 Dramatically Distinct Skin Lightness Tones (Ranging from Warm Gold to Snow White)
# Tone A: Sun-Kissed Warm Tan (Original Baseline)
PALETTE_1 = copy.deepcopy(bpv.PALETTE)
PALETTE_1[bpv.SKIN] = "ecaa6c"
PALETTE_1[bpv.SKIN_DARK] = "d8985e"
PALETTE_1[bpv.SKIN_LIT] = "ecaa6c"

# Tone B: Natural Fair Peach / Rosy Warm
PALETTE_2 = copy.deepcopy(bpv.PALETTE)
PALETTE_2[bpv.SKIN] = "f5b894"
PALETTE_2[bpv.SKIN_DARK] = "db9874"
PALETTE_2[bpv.SKIN_LIT] = "f5b894"

# Tone C: Pale Cream Porcelain (Noticeably Whiter)
PALETTE_3 = copy.deepcopy(bpv.PALETTE)
PALETTE_3[bpv.SKIN] = "fae2ce"
PALETTE_3[bpv.SKIN_DARK] = "e0ba9e"
PALETTE_3[bpv.SKIN_LIT] = "fae2ce"

# Tone D: Snow White Alabaster (Max Lightness)
PALETTE_4 = copy.deepcopy(bpv.PALETTE)
PALETTE_4[bpv.SKIN] = "fff0e2"
PALETTE_4[bpv.SKIN_DARK] = "e8c8b0"
PALETTE_4[bpv.SKIN_LIT] = "fff0e2"

def get_iteration_boxes(variant_num):
    leg_l = copy.deepcopy(bpv.LEG_LEFT)
    leg_r = copy.deepcopy(bpv.LEG_RIGHT)
    torso = copy.deepcopy(bpv.TORSO)
    arm_l = copy.deepcopy(bpv.ARM_LEFT)
    arm_r = copy.deepcopy(bpv.ARM_RIGHT)
    
    # 🔒 APPROVED BASELINE: Complete Back Hair & Star Clasp & Ribbons
    core_head_back_and_sides = [
        ("hair-core",           "head", (-0.180, 0.480, -0.215), (0.180, 0.700, -0.040), bpv.HAIR),
        ("hair-crown",          "head", (-0.176, 0.650, -0.170), (0.176, 0.705, 0.140), bpv.HAIR),
        ("hair-back-tier1",     "head", (-0.176, 0.340, -0.218), (0.176, 0.490, -0.135), bpv.HAIR),
        ("hair-back-tier2",     "head", (-0.174, 0.230, -0.216), (0.174, 0.348, -0.138), bpv.HAIR),
        ("hair-back-star-clasp",    "head", (-0.030, 0.335, -0.228), (0.030, 0.375, -0.214), bpv.FROST_ACCENT),
        ("hair-back-star-gem",      "head", (-0.015, 0.342, -0.232), (0.015, 0.368, -0.216), bpv.WHITE),
        ("hair-back-ribbon-tail-l", "head", (0.020, 0.230, -0.225), (0.065, 0.340, -0.215), bpv.CYAN_TRIM),
        ("hair-back-ribbon-tail-r", "head", (-0.065, 0.230, -0.225), (-0.020, 0.340, -0.215), bpv.CYAN_TRIM),
        ("hair-back-ribbon-tip-l",  "head", (0.035, 0.200, -0.225), (0.075, 0.235, -0.215), bpv.FROST_ACCENT),
        ("hair-back-ribbon-tip-r",  "head", (-0.075, 0.200, -0.225), (-0.035, 0.235, -0.215), bpv.FROST_ACCENT),
        ("hair-back-weight-band",   "head", (-0.176, 0.155, -0.224), (0.176, 0.215, -0.145), bpv.HAIR),
        ("hair-back-hem-rim",       "head", (-0.172, 0.148, -0.218), (0.172, 0.165, -0.148), bpv.HAIR),
        ("hair-back-frost-hem",     "head", (-0.172, 0.148, -0.224), (0.172, 0.165, -0.216), bpv.CYAN_TRIM),
        ("hair-back-frost-sparkle", "head", (-0.140, 0.150, -0.226), (0.140, 0.162, -0.220), bpv.FROST_ACCENT),
        ("hair-back-rail-left",     "head", (0.158, 0.170, -0.220), (0.180, 0.600, -0.160), bpv.HAIR),
        ("hair-back-rail-right",    "head", (-0.180, 0.170, -0.220), (-0.158, 0.600, -0.160), bpv.HAIR),
        ("hair-side-cap-left",      "head", (0.165, 0.490, -0.160), (0.190, 0.670, 0.120), bpv.HAIR),
        ("hair-side-cap-right",     "head", (-0.190, 0.490, -0.160), (-0.165, 0.670, 0.120), bpv.HAIR),
        ("hair-side-lock-left",     "head", (0.168, 0.350, 0.000), (0.188, 0.420, 0.080), bpv.HAIR),
        ("hair-side-lock-right",    "head", (-0.188, 0.350, 0.000), (-0.168, 0.420, 0.080), bpv.HAIR),
        ("hair-side-frost-lock-l",  "head", (0.170, 0.355, 0.010), (0.190, 0.410, 0.050), bpv.CYAN_TRIM),
        ("hair-side-frost-lock-r",  "head", (-0.190, 0.355, 0.010), (-0.170, 0.410, 0.050), bpv.CYAN_TRIM),
    ]

    # 💇 MASTER SCULPTED 3D LAYERED HAIR & BANGS
    pure_clean_sculpted_bangs = [
        ("hair-brow-base",          "head", (-0.176, 0.585, 0.130), (0.176, 0.675, 0.172), bpv.HAIR),
        ("hair-fringe-center",      "head", (-0.045, 0.555, 0.158), (0.045, 0.620, 0.188), bpv.HAIR),
        ("hair-fringe-mid-l",       "head", (0.040, 0.540, 0.156), (0.115, 0.625, 0.185), bpv.HAIR),
        ("hair-fringe-mid-r",       "head", (-0.115, 0.540, 0.156), (-0.040, 0.625, 0.185), bpv.HAIR),
        ("hair-fringe-outer-l",     "head", (0.110, 0.505, 0.150), (0.174, 0.620, 0.180), bpv.HAIR),
        ("hair-fringe-outer-r",     "head", (-0.174, 0.505, 0.150), (-0.110, 0.620, 0.180), bpv.HAIR),
        ("hair-vol-lock-c",         "head", (-0.035, 0.565, 0.182), (0.035, 0.635, 0.198), bpv.HAIR),
        ("hair-vol-lock-l",         "head", (0.045, 0.550, 0.180), (0.105, 0.630, 0.195), bpv.HAIR),
        ("hair-vol-lock-r",         "head", (-0.105, 0.550, 0.180), (-0.045, 0.630, 0.195), bpv.HAIR),
        ("hair-vol-lock-r2",        "head", (-0.110, 0.555, 0.182), (-0.040, 0.628, 0.196), bpv.HAIR),
        ("hair-vol-lock-l2",        "head", (0.040, 0.555, 0.182), (0.110, 0.628, 0.196), bpv.HAIR),
    ]

    # 🥽 CONFORMAL PRO SKI GOGGLES
    pro_ski_goggles = [
        ("goggle-strap-b",      "head", (-0.180, 0.638, -0.238), (0.180, 0.672, -0.200), bpv.OVERALLS_DARK),
        ("goggle-strap-l",      "head", (0.176, 0.638, -0.218), (0.212, 0.672, 0.140), bpv.OVERALLS_DARK),
        ("goggle-strap-r",      "head", (-0.212, 0.638, -0.218), (-0.176, 0.672, 0.140), bpv.OVERALLS_DARK),
        ("goggle-strap-trim-l", "head", (0.180, 0.648, -0.120), (0.214, 0.662, 0.120), bpv.CYAN_TRIM),
        ("goggle-strap-trim-r", "head", (-0.214, 0.648, -0.120), (-0.180, 0.662, 0.120), bpv.CYAN_TRIM),
        ("goggle-hinge-l",      "head", (0.170, 0.628, 0.140), (0.206, 0.688, 0.198), bpv.SILVER),
        ("goggle-hinge-r",      "head", (-0.206, 0.628, 0.140), (-0.170, 0.688, 0.198), bpv.SILVER),
        ("goggle-frame-l",      "head", (0.015, 0.625, 0.200), (0.168, 0.702, 0.248), bpv.SILVER),
        ("goggle-frame-r",      "head", (-0.168, 0.625, 0.200), (-0.015, 0.702, 0.248), bpv.SILVER),
        ("goggle-frame-wrap-l", "head", (0.144, 0.628, 0.150), (0.194, 0.696, 0.218), bpv.SILVER),
        ("goggle-frame-wrap-r", "head", (-0.194, 0.628, 0.150), (-0.144, 0.696, 0.218), bpv.SILVER),
        ("goggle-bridge-top",   "head", (-0.020, 0.660, 0.205), (0.020, 0.696, 0.242), bpv.SILVER),
        ("goggle-bridge-arch",  "head", (-0.015, 0.638, 0.208), (0.015, 0.662, 0.236), bpv.SILVER),
        ("goggle-bridge-rivet", "head", (-0.010, 0.670, 0.238), (0.010, 0.686, 0.248), bpv.WHITE),
        ("goggle-lens-l",       "head", (0.026, 0.635, 0.215), (0.155, 0.692, 0.254), bpv.CYAN_TRIM),
        ("goggle-lens-r",       "head", (-0.155, 0.635, 0.215), (-0.026, 0.692, 0.254), bpv.CYAN_TRIM),
        ("goggle-lens-wrap-l",  "head", (0.144, 0.638, 0.162), (0.185, 0.688, 0.222), bpv.CYAN_TRIM),
        ("goggle-lens-wrap-r",  "head", (-0.185, 0.638, 0.162), (-0.144, 0.688, 0.222), bpv.CYAN_TRIM),
        ("goggle-depth-l",      "head", (0.032, 0.660, 0.230), (0.148, 0.688, 0.256), bpv.FROST_ACCENT),
        ("goggle-depth-r",      "head", (-0.148, 0.660, 0.230), (-0.032, 0.688, 0.256), bpv.FROST_ACCENT),
        ("goggle-glint-l",      "head", (0.045, 0.668, 0.238), (0.092, 0.684, 0.258), bpv.WHITE),
        ("goggle-glint-r",      "head", (-0.140, 0.668, 0.238), (-0.094, 0.684, 0.258), bpv.WHITE),
    ]

    # 👑 ULTRA PUFFY VOLUMETRIC EXPEDITION USHANKA (All candidates wear this exact same ultra-puffy hat!)
    ultra_puffy_ushanka = [
        # 1. 👑 Volumetric Quilted Puffy Crown (Extra wide and puffy)
        ("ush-crown-core",      "head", (-0.188, 0.630, -0.210), (0.188, 0.735, 0.160), bpv.OVERALLS),
        ("ush-crown-side-l",    "head", (0.175, 0.635, -0.185), (0.214, 0.730, 0.135), bpv.OVERALLS),
        ("ush-crown-side-r",    "head", (-0.214, 0.635, -0.185), (-0.175, 0.730, 0.135), bpv.OVERALLS),
        ("ush-crown-front",     "head", (-0.170, 0.635, 0.145), (0.170, 0.730, 0.190), bpv.OVERALLS),
        ("ush-crown-back",      "head", (-0.170, 0.635, -0.232), (0.170, 0.730, -0.192), bpv.OVERALLS),
        ("ush-crown-fl",        "head", (0.135, 0.635, 0.115), (0.200, 0.730, 0.175), bpv.OVERALLS),
        ("ush-crown-fr",        "head", (-0.200, 0.635, 0.115), (-0.135, 0.730, 0.175), bpv.OVERALLS),
        ("ush-crown-bl",        "head", (0.135, 0.635, -0.222), (0.200, 0.730, -0.152), bpv.OVERALLS),
        ("ush-crown-br",        "head", (-0.200, 0.635, -0.222), (-0.135, 0.730, -0.152), bpv.OVERALLS),
        
        # Upper Dome Top
        ("ush-dome-top",        "head", (-0.160, 0.730, -0.180), (0.160, 0.772, 0.135), bpv.OVERALLS),
        ("ush-dome-top-l",      "head", (0.145, 0.730, -0.150), (0.182, 0.765, 0.110), bpv.OVERALLS),
        ("ush-dome-top-r",      "head", (-0.182, 0.730, -0.150), (-0.145, 0.765, 0.110), bpv.OVERALLS),
        ("ush-dome-top-f",      "head", (-0.135, 0.730, 0.115), (0.135, 0.765, 0.160), bpv.OVERALLS),
        ("ush-dome-top-b",      "head", (-0.135, 0.730, -0.205), (0.135, 0.765, -0.160), bpv.OVERALLS),
        
        # Quilted Seam Ribs
        ("ush-seam-x",          "head", (-0.165, 0.732, -0.015), (0.165, 0.766, 0.015), bpv.OVERALLS_DARK),
        ("ush-seam-z",          "head", (-0.015, 0.732, -0.185), (0.015, 0.766, 0.140), bpv.OVERALLS_DARK),
        
        # Giant Fluffy Snowball Pom-Pom (Apex)
        ("ush-pompom-base",     "head", (-0.065, 0.760, -0.065), (0.065, 0.788, 0.065), bpv.WHITE),
        ("ush-pompom-top",      "head", (-0.040, 0.784, -0.040), (0.040, 0.792, 0.040), bpv.WHITE),
        ("ush-pompom-star",     "head", (-0.018, 0.778, -0.018), (0.018, 0.790, 0.018), bpv.CYAN_TRIM),
        
        # 2. ❄️ Oversized Fluffy Forward-Flared Fur Visor
        ("ush-visor-backing",   "head", (-0.175, 0.605, 0.160), (0.175, 0.695, 0.210), bpv.OVERALLS_DARK),
        ("ush-visor-center",    "head", (-0.150, 0.605, 0.180), (0.150, 0.710, 0.248), bpv.WHITE),
        ("ush-visor-bevel-l",   "head", (0.125, 0.605, 0.155), (0.192, 0.702, 0.230), bpv.WHITE),
        ("ush-visor-bevel-r",   "head", (-0.192, 0.605, 0.155), (-0.125, 0.702, 0.230), bpv.WHITE),
        ("ush-visor-lip",       "head", (-0.155, 0.665, 0.200), (0.155, 0.715, 0.254), bpv.WHITE),
        ("ush-visor-star",      "head", (-0.030, 0.650, 0.236), (0.030, 0.686, 0.256), bpv.CYAN_TRIM),
        ("ush-visor-star-core", "head", (-0.015, 0.658, 0.246), (0.015, 0.678, 0.258), bpv.FROST_ACCENT),
        
        # 3. 👂 Bulbous Cozy Puffy Earflaps
        ("ush-flap-root-l",     "head", (0.172, 0.540, -0.155), (0.228, 0.655, 0.115), bpv.OVERALLS),
        ("ush-flap-root-r",     "head", (-0.228, 0.540, -0.155), (-0.172, 0.655, 0.115), bpv.OVERALLS),
        ("ush-flap-bell-l",     "head", (0.175, 0.405, -0.155), (0.244, 0.570, 0.110), bpv.OVERALLS),
        ("ush-flap-cushion-l",  "head", (0.224, 0.425, -0.135), (0.248, 0.555, 0.090), bpv.OVERALLS_DARK),
        ("ush-flap-star-l",     "head", (0.240, 0.470, -0.035), (0.250, 0.510, 0.015), bpv.CYAN_TRIM),
        ("ush-flap-gem-l",      "head", (0.245, 0.482, -0.020), (0.252, 0.498, -0.000), bpv.FROST_ACCENT),
        ("ush-flap-bell-r",     "head", (-0.244, 0.405, -0.155), (-0.175, 0.570, 0.110), bpv.OVERALLS),
        ("ush-flap-cushion-r",  "head", (-0.248, 0.425, -0.135), (-0.224, 0.555, 0.090), bpv.OVERALLS_DARK),
        ("ush-flap-star-r",     "head", (-0.250, 0.470, -0.035), (-0.240, 0.510, 0.015), bpv.CYAN_TRIM),
        ("ush-flap-gem-r",      "head", (-0.252, 0.482, -0.020), (-0.245, 0.498, -0.000), bpv.FROST_ACCENT),
        ("ush-flap-wedge-l",    "head", (0.145, 0.405, -0.222), (0.218, 0.645, -0.145), bpv.OVERALLS),
        ("ush-flap-wedge-r",    "head", (-0.218, 0.405, -0.222), (-0.145, 0.645, -0.145), bpv.OVERALLS),
        ("ush-flap-jaw-l",      "head", (0.170, 0.315, -0.135), (0.228, 0.440, 0.085), bpv.OVERALLS),
        ("ush-flap-jaw-r",      "head", (-0.228, 0.315, -0.135), (-0.170, 0.440, 0.085), bpv.OVERALLS),
        ("ush-flap-fur-l",      "head", (0.165, 0.305, -0.145), (0.240, 0.390, 0.095), bpv.WHITE),
        ("ush-flap-inner-fur-l","head", (0.160, 0.325, 0.020), (0.208, 0.490, 0.100), bpv.WHITE),
        ("ush-flap-fur-r",      "head", (-0.240, 0.305, -0.145), (-0.165, 0.390, 0.095), bpv.WHITE),
        ("ush-flap-inner-fur-r","head", (-0.208, 0.325, 0.020), (-0.160, 0.490, 0.100), bpv.WHITE),
        ("ush-cord-l",          "head", (0.194, 0.245, -0.025), (0.216, 0.325, 0.005), bpv.CYAN_TRIM),
        ("ush-cord-r",          "head", (-0.216, 0.245, -0.025), (-0.194, 0.325, 0.005), bpv.CYAN_TRIM),
        ("ush-cord-pompom-l",   "head", (0.182, 0.205, -0.040), (0.228, 0.255, 0.020), bpv.WHITE),
        ("ush-cord-pompom-r",   "head", (-0.228, 0.205, -0.040), (-0.182, 0.255, 0.020), bpv.WHITE),
        
        # 4. 🔙 Deep Quilted Draped Back Mantle
        ("ush-back-upper-c",    "head", (-0.175, 0.490, -0.238), (0.175, 0.655, -0.155), bpv.OVERALLS),
        ("ush-back-upper-pad",  "head", (-0.155, 0.505, -0.242), (0.155, 0.635, -0.170), bpv.OVERALLS),
        ("ush-back-upper-l",    "head", (0.130, 0.490, -0.232), (0.198, 0.655, -0.155), bpv.OVERALLS),
        ("ush-back-upper-r",    "head", (-0.198, 0.490, -0.232), (-0.130, 0.655, -0.155), bpv.OVERALLS),
        ("ush-back-quilt-seam", "head", (-0.170, 0.490, -0.240), (0.170, 0.505, -0.160), bpv.OVERALLS_DARK),
        ("ush-back-lower-c",    "head", (-0.170, 0.370, -0.234), (0.170, 0.495, -0.165), bpv.OVERALLS),
        ("ush-back-lower-l",    "head", (0.128, 0.370, -0.228), (0.190, 0.495, -0.165), bpv.OVERALLS),
        ("ush-back-lower-r",    "head", (-0.190, 0.370, -0.228), (-0.128, 0.495, -0.165), bpv.OVERALLS),
        ("ush-back-fur-main",   "head", (-0.172, 0.360, -0.242), (0.172, 0.412, -0.170), bpv.WHITE),
        ("ush-back-fur-lip",    "head", (-0.160, 0.355, -0.244), (0.160, 0.395, -0.190), bpv.WHITE),
        ("ush-back-fur-conn-l", "head", (0.135, 0.345, -0.234), (0.210, 0.405, -0.135), bpv.WHITE),
        ("ush-back-fur-conn-r", "head", (-0.210, 0.345, -0.234), (-0.135, 0.405, -0.135), bpv.WHITE),
        ("ush-back-strap",      "head", (-0.015, 0.405, -0.238), (0.015, 0.645, -0.228), bpv.OVERALLS_DARK),
    ]

    headpiece = ultra_puffy_ushanka + pro_ski_goggles
    full_head = core_head_back_and_sides + pure_clean_sculpted_bangs + headpiece
    return leg_l, leg_r, torso, arm_l, arm_r, full_head

def build_all():
    target_dir = "Assets/TumbangPreso/Art/characters/persons"
    os.makedirs(target_dir, exist_ok=True)
    
    variants = [
        (1, "iteration-1.glb", PALETTE_1),  # 1: Fair Peach Porcelain
        (2, "iteration-2.glb", PALETTE_2),  # 2: Bright Warm Ivory
        (3, "iteration-3.glb", PALETTE_3),  # 3: Pale Porcelain Rose
        (4, "iteration-4.glb", PALETTE_4),  # 4: Anime Fair Snow White
    ]
    
    for v_num, fname, pal in variants:
        glb_path = f"{target_dir}/{fname}"
        leg_l, leg_r, torso, arm_l, arm_r, head = get_iteration_boxes(v_num)
        bpv.HEAD = head
        bpv.HEAD_BOXES = head
        bpv.LEG_LEFT = leg_l
        bpv.LEG_RIGHT = leg_r
        bpv.TORSO = torso
        bpv.ARM_LEFT = arm_l
        bpv.ARM_RIGHT = arm_r
        bpv.BODY_BOXES = leg_l + leg_r + torso + arm_l + arm_r
        bpv.DONOR_SPACE = tuple(entry[0] for entry in head)
        bpv.OUT = glb_path
        bpv.PALETTE = pal
        
        bpv.main()
        print(f"Generated {glb_path}")

if __name__ == "__main__":
    build_all()
