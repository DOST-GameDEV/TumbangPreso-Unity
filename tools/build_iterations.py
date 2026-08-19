"""Generates 4 BIG VOLUMETRIC Headpiece & Sculpted 3D Hair iterations on the Brown Skin + Twin-Ribbon baseline."""
import os
import sys
import copy

sys.path.insert(0, "tools")
import build_person_voxel as bpv

# Rich Brown Skin Palette (Approved Baseline)
PALETTE_BROWN = copy.deepcopy(bpv.PALETTE)
PALETTE_BROWN[bpv.SKIN] = "d88c48"
PALETTE_BROWN[bpv.SKIN_DARK] = "a45c26"
PALETTE_BROWN[bpv.SKIN_LIT] = "f4a868"

def get_iteration_boxes(variant_num):
    leg_l = copy.deepcopy(bpv.LEG_LEFT)
    leg_r = copy.deepcopy(bpv.LEG_RIGHT)
    torso = copy.deepcopy(bpv.TORSO)
    arm_l = copy.deepcopy(bpv.ARM_LEFT)
    arm_r = copy.deepcopy(bpv.ARM_RIGHT)
    
    # =========================================================================
    # 🔒 APPROVED BASELINE: Back (Twin-Ribbon Style) & Sides (Ear Contouring)
    # =========================================================================
    core_head_back_and_sides = [
        ("hair-core",           "head", (-0.180, 0.480, -0.215), (0.180, 0.700, -0.040), bpv.HAIR),
        ("hair-crown",          "head", (-0.176, 0.650, -0.170), (0.176, 0.705, 0.140), bpv.HAIR),
        ("hair-back-tier1",     "head", (-0.176, 0.340, -0.218), (0.176, 0.490, -0.135), bpv.HAIR),
        ("hair-back-tier2",     "head", (-0.174, 0.230, -0.216), (0.174, 0.348, -0.138), bpv.HAIR),
        # Iteration 2 Back: Frosted Star Clasp & Twin Fluttering Ribbon Tails
        ("hair-back-star-clasp",    "head", (-0.030, 0.455, -0.228), (0.030, 0.495, -0.214), bpv.FROST_ACCENT),
        ("hair-back-star-gem",      "head", (-0.015, 0.462, -0.232), (0.015, 0.488, -0.216), bpv.WHITE),
        ("hair-back-ribbon-tail-l", "head", (0.020, 0.360, -0.225), (0.065, 0.460, -0.215), bpv.CYAN_TRIM),
        ("hair-back-ribbon-tail-r", "head", (-0.065, 0.360, -0.225), (-0.020, 0.460, -0.215), bpv.CYAN_TRIM),
        ("hair-back-ribbon-tip-l",  "head", (0.035, 0.340, -0.225), (0.075, 0.365, -0.215), bpv.FROST_ACCENT),
        ("hair-back-ribbon-tip-r",  "head", (-0.075, 0.340, -0.225), (-0.035, 0.365, -0.215), bpv.FROST_ACCENT),
        ("hair-back-weight-band",   "head", (-0.176, 0.155, -0.224), (0.176, 0.215, -0.145), bpv.HAIR),
        ("hair-back-hem-rim",       "head", (-0.172, 0.148, -0.218), (0.172, 0.165, -0.148), bpv.HAIR),
        ("hair-back-frost-hem",     "head", (-0.172, 0.148, -0.224), (0.172, 0.165, -0.216), bpv.CYAN_TRIM),
        ("hair-back-frost-sparkle", "head", (-0.140, 0.150, -0.226), (0.140, 0.162, -0.220), bpv.FROST_ACCENT),
        ("hair-back-rail-left",     "head", (0.158, 0.170, -0.220), (0.180, 0.600, -0.160), bpv.HAIR),
        ("hair-back-rail-right",    "head", (-0.180, 0.170, -0.220), (-0.158, 0.600, -0.160), bpv.HAIR),
        # Side ear coverage
        ("hair-side-cap-left",      "head", (0.165, 0.490, -0.160), (0.190, 0.670, 0.120), bpv.HAIR),
        ("hair-side-cap-right",     "head", (-0.190, 0.490, -0.160), (-0.165, 0.670, 0.120), bpv.HAIR),
        ("hair-side-post-ear-l",    "head", (0.165, 0.410, -0.155), (0.188, 0.500, -0.010), bpv.HAIR),
        ("hair-side-post-ear-r",    "head", (-0.188, 0.410, -0.155), (-0.165, 0.500, -0.010), bpv.HAIR),
        ("hair-side-ante-ear-l",    "head", (0.165, 0.410, 0.015), (0.188, 0.500, 0.115), bpv.HAIR),
        ("hair-side-ante-ear-r",    "head", (-0.188, 0.410, 0.015), (-0.165, 0.500, 0.115), bpv.HAIR),
        ("hair-side-lock-left",     "head", (0.168, 0.350, 0.000), (0.188, 0.420, 0.080), bpv.HAIR),
        ("hair-side-lock-right",    "head", (-0.188, 0.350, 0.000), (-0.168, 0.420, 0.080), bpv.HAIR),
        ("hair-side-frost-lock-l",  "head", (0.170, 0.355, 0.010), (0.190, 0.410, 0.050), bpv.CYAN_TRIM),
        ("hair-side-frost-lock-r",  "head", (-0.190, 0.355, 0.010), (-0.170, 0.410, 0.050), bpv.CYAN_TRIM),
        ("earring-stud",            "head", (-0.215, 0.485, -0.015), (-0.200, 0.508, 0.005), bpv.SILVER),
        ("earring-frost-drop",      "head", (-0.218, 0.435, -0.018), (-0.198, 0.485, 0.008), bpv.CYAN_TRIM),
        ("earring-frost-sparkle",   "head", (-0.219, 0.442, -0.012), (-0.204, 0.465, 0.004), bpv.FROST_ACCENT),
        ("earring-stud-left",       "head", (0.200, 0.485, -0.015), (0.215, 0.508, 0.005), bpv.SILVER),
        ("earring-gem-left",        "head", (0.202, 0.488, -0.010), (0.218, 0.505, 0.002), bpv.FROST_ACCENT),
    ]

    # =========================================================================
    # 💇 MASTER SCULPTED 3D LAYERED HAIR & BANGS
    # =========================================================================
    # Beautiful multi-tier bangs with arched center brow, stepped 3D depth, and clean flowing streaks
    sculpted_3d_bangs = [
        # Base brow hair mantle connecting skull to hairline
        ("hair-brow-base",          "head", (-0.176, 0.585, 0.130), (0.176, 0.675, 0.172), bpv.HAIR),
        
        # Tier 1: Main contoured fringe layer (framing eyes with soft arch)
        ("hair-fringe-center",      "head", (-0.045, 0.555, 0.158), (0.045, 0.620, 0.188), bpv.HAIR),
        ("hair-fringe-mid-l",       "head", (0.040, 0.540, 0.156), (0.115, 0.625, 0.185), bpv.HAIR),
        ("hair-fringe-mid-r",       "head", (-0.115, 0.540, 0.156), (-0.040, 0.625, 0.185), bpv.HAIR),
        ("hair-fringe-outer-l",     "head", (0.110, 0.505, 0.150), (0.174, 0.620, 0.180), bpv.HAIR),
        ("hair-fringe-outer-r",     "head", (-0.174, 0.505, 0.150), (-0.110, 0.620, 0.180), bpv.HAIR),

        # Tier 2: Forward 3D Volumetric Overhang Locks (z=0.180 to 0.198, rich cast shadow)
        ("hair-vol-lock-c",         "head", (-0.035, 0.565, 0.182), (0.035, 0.635, 0.198), bpv.HAIR),
        ("hair-vol-lock-l",         "head", (0.045, 0.550, 0.180), (0.105, 0.630, 0.195), bpv.HAIR),
        ("hair-vol-lock-r",         "head", (-0.105, 0.550, 0.180), (-0.045, 0.630, 0.195), bpv.HAIR),

        # Tier 3: Stylized Cyan Frost Dyed Highlights (Right Bangs Sweep)
        ("hair-streak-base",        "head", (-0.110, 0.555, 0.190), (-0.040, 0.628, 0.204), bpv.CYAN_TRIM),
        ("hair-streak-tip",         "head", (-0.045, 0.565, 0.192), (-0.015, 0.605, 0.202), bpv.CYAN_TRIM),
        ("hair-streak-glint",       "head", (-0.095, 0.575, 0.198), (-0.055, 0.620, 0.206), bpv.FROST_ACCENT),
        
        # Frosted Star Hair Barrette on Left Temple (+X)
        ("hair-clip-silver",        "head", (0.105, 0.600, 0.180), (0.155, 0.615, 0.198), bpv.SILVER),
        ("hair-clip-gem",           "head", (0.120, 0.595, 0.192), (0.142, 0.620, 0.204), bpv.FROST_ACCENT),
    ]

    # =========================================================================
    # 👨‍🍳 EXTRA VOLUMINOUS 4-TIER BAKER TOQUE BASE (Stepped Dome & Puffy Cloud Silhouette)
    # =========================================================================
    # Brim at y=0.660-0.695, lower flare at y=0.695-0.735, grand puff at y=0.735-0.768, stepped dome peak at y=0.768-0.788
    grand_toque_base = [
        # Tier 1: Fitted Brim Band (y=0.660 to 0.695)
        ("cap-brim-base",           "head", (-0.175, 0.660, -0.175), (0.175, 0.695, 0.175), bpv.WHITE),
        ("cap-brim-ribbon",         "head", (-0.180, 0.668, -0.180), (0.180, 0.688, 0.180), bpv.CYAN_TRIM),
        ("cap-brim-piping",         "head", (-0.182, 0.674, -0.182), (0.182, 0.682, 0.182), bpv.FROST_ACCENT),
        
        # Tier 2: Bulging Lower Mushroom Flare (y=0.695 to 0.735, expands to |x|,|z| <= 0.198)
        ("cap-puff-mid",            "head", (-0.195, 0.695, -0.195), (0.195, 0.735, 0.195), bpv.WHITE),
        ("cap-puff-lobe-f1",        "head", (-0.160, 0.695, 0.180), (0.160, 0.735, 0.208), bpv.WHITE),
        ("cap-puff-lobe-b1",        "head", (-0.160, 0.695, -0.208), (0.160, 0.735, -0.180), bpv.WHITE),
        ("cap-puff-lobe-l1",        "head", (0.180, 0.695, -0.160), (0.208, 0.735, 0.160), bpv.WHITE),
        ("cap-puff-lobe-r1",        "head", (-0.208, 0.695, -0.160), (-0.180, 0.735, 0.160), bpv.WHITE),
        
        # Tier 3: Grand Billowing Mushroom Cloud Crown (y=0.735 to 0.765, expands to |x|,|z| <= 0.208)
        ("cap-puff-crown",          "head", (-0.205, 0.735, -0.205), (0.205, 0.765, 0.205), bpv.WHITE),
        ("cap-puff-crown-f",        "head", (-0.170, 0.738, 0.190), (0.170, 0.762, 0.218), bpv.WHITE),
        ("cap-puff-crown-b",        "head", (-0.170, 0.738, -0.218), (0.170, 0.762, -0.190), bpv.WHITE),
        ("cap-puff-crown-l",        "head", (0.190, 0.738, -0.170), (0.218, 0.762, 0.170), bpv.WHITE),
        ("cap-puff-crown-r",        "head", (-0.218, 0.738, -0.170), (-0.190, 0.762, 0.170), bpv.WHITE),
        # Corner puff billows
        ("cap-puff-c-fl",           "head", (0.160, 0.735, 0.160), (0.208, 0.762, 0.208), bpv.WHITE),
        ("cap-puff-c-fr",           "head", (-0.208, 0.735, 0.160), (-0.160, 0.762, 0.208), bpv.WHITE),
        ("cap-puff-c-bl",           "head", (0.160, 0.735, -0.208), (0.208, 0.762, -0.160), bpv.WHITE),
        ("cap-puff-c-br",           "head", (-0.208, 0.735, -0.208), (-0.160, 0.762, -0.160), bpv.WHITE),
        
        # Tier 4: Stepped Dome Crown (Eliminates flat pancake top! y=0.765 to 0.788)
        # Lower Dome Tier (y=0.765 to 0.778, |x|,|z| <= 0.175)
        ("cap-dome-tier1",          "head", (-0.175, 0.765, -0.175), (0.175, 0.778, 0.175), bpv.WHITE),
        ("cap-dome-tier1-f",        "head", (-0.140, 0.765, 0.160), (0.140, 0.776, 0.188), bpv.WHITE),
        ("cap-dome-tier1-b",        "head", (-0.140, 0.765, -0.188), (0.140, 0.776, -0.160), bpv.WHITE),
        ("cap-dome-tier1-l",        "head", (0.160, 0.765, -0.140), (0.188, 0.776, 0.140), bpv.WHITE),
        ("cap-dome-tier1-r",        "head", (-0.188, 0.765, -0.140), (-0.160, 0.776, 0.140), bpv.WHITE),
        
        # Middle Dome Tier (y=0.778 to 0.784, |x|,|z| <= 0.135)
        ("cap-dome-tier2",          "head", (-0.135, 0.778, -0.135), (0.135, 0.784, 0.135), bpv.WHITE),
        ("cap-dome-stripe-x",       "head", (-0.138, 0.780, -0.035), (0.138, 0.784, 0.035), bpv.CYAN_TRIM),
        ("cap-dome-stripe-z",       "head", (-0.035, 0.780, -0.138), (0.035, 0.784, 0.138), bpv.CYAN_TRIM),
        
        # Peak Crown Cap (y=0.784 to 0.788, |x|,|z| <= 0.075)
        ("cap-dome-peak",           "head", (-0.075, 0.784, -0.075), (0.075, 0.788, 0.075), bpv.WHITE),
        ("cap-dome-star-gem",       "head", (-0.025, 0.784, -0.025), (0.025, 0.789, 0.025), bpv.FROST_ACCENT),
    ]

    # =========================================================================
    # 🎨 4 DISTINCT THEMED ACCESSORY ITERATIONS
    # =========================================================================
    if variant_num == 1:
        # 🥽 ITERATION 1: Frost Baker Ski Goggles Perched on Hat Brim
        # Perched stylishly right above bangs on hat band, showcasing full 3D face & bangs!
        headpiece = grand_toque_base + [
            # Goggle Strap wrapping continuously around toque brim band
            ("goggle-strap-b",      "head", (-0.182, 0.662, -0.185), (0.182, 0.686, -0.175), bpv.OVERALLS_DARK),
            ("goggle-strap-l",      "head", (0.175, 0.662, -0.180), (0.186, 0.686, 0.165), bpv.OVERALLS_DARK),
            ("goggle-strap-r",      "head", (-0.186, 0.662, -0.180), (-0.175, 0.686, 0.165), bpv.OVERALLS_DARK),
            ("goggle-buckle-l",     "head", (0.180, 0.665, 0.010), (0.190, 0.683, 0.045), bpv.SILVER),
            ("goggle-buckle-r",     "head", (-0.190, 0.665, 0.010), (-0.180, 0.683, 0.045), bpv.SILVER),
            
            # Chunky Silver Metallic Frames (Perched at y=0.640 to 0.710, z=0.178 to 0.222)
            ("goggle-frame-l",      "head", (0.018, 0.640, 0.178), (0.155, 0.710, 0.220), bpv.SILVER),
            ("goggle-frame-r",      "head", (-0.155, 0.640, 0.178), (-0.018, 0.710, 0.220), bpv.SILVER),
            ("goggle-bridge",       "head", (-0.024, 0.665, 0.182), (0.024, 0.695, 0.215), bpv.SILVER),
            
            # Inner Dark Rubber Gasket
            ("goggle-seal-l",       "head", (0.024, 0.646, 0.184), (0.149, 0.704, 0.222), bpv.OVERALLS_DARK),
            ("goggle-seal-r",       "head", (-0.149, 0.646, 0.184), (-0.024, 0.704, 0.222), bpv.OVERALLS_DARK),
            
            # Glowing Frost Cyan Lenses
            ("goggle-lens-l",       "head", (0.028, 0.650, 0.190), (0.145, 0.700, 0.225), bpv.CYAN_TRIM),
            ("goggle-lens-r",       "head", (-0.145, 0.650, 0.190), (-0.028, 0.700, 0.225), bpv.CYAN_TRIM),
            
            # Shimmering 3D Glints & Star Highlights
            ("goggle-glint-l",      "head", (0.040, 0.672, 0.208), (0.095, 0.695, 0.228), bpv.FROST_ACCENT),
            ("goggle-glint-r",      "head", (-0.135, 0.672, 0.208), (-0.080, 0.695, 0.228), bpv.FROST_ACCENT),
            ("goggle-star-l",       "head", (0.048, 0.678, 0.215), (0.072, 0.690, 0.230), bpv.WHITE),
            ("goggle-star-r",       "head", (-0.127, 0.678, 0.215), (-0.103, 0.690, 0.230), bpv.WHITE),
        ]

    elif variant_num == 2:
        # 👑 ITERATION 2: Grand Ice Tiara / Royal Baker Crown + Side Flutter Bow
        headpiece = grand_toque_base + [
            # Side Ribbon Bow with Fluttering Tails on Left (+X)
            ("cap-bow-knot",        "head", (0.125, 0.665, 0.135), (0.160, 0.695, 0.182), bpv.CYAN_TRIM),
            ("cap-bow-loop-u",      "head", (0.110, 0.685, 0.130), (0.145, 0.718, 0.175), bpv.CYAN_TRIM),
            ("cap-bow-loop-d",      "head", (0.140, 0.685, 0.130), (0.175, 0.718, 0.175), bpv.CYAN_TRIM),
            ("cap-bow-tail-f",      "head", (0.130, 0.625, 0.145), (0.155, 0.670, 0.172), bpv.CYAN_TRIM),
            ("cap-bow-tail-b",      "head", (0.145, 0.630, 0.140), (0.170, 0.670, 0.168), bpv.CYAN_TRIM),
            ("cap-bow-frost-star",  "head", (0.135, 0.672, 0.170), (0.152, 0.688, 0.185), bpv.WHITE),

            # Center 3D Sculpted Ice Crown / Frost Tiara on Brim
            ("cap-tiara-base",      "head", (-0.115, 0.660, 0.175), (0.115, 0.682, 0.195), bpv.SILVER),
            # Center Majestic Spire
            ("cap-tiara-center",    "head", (-0.030, 0.678, 0.178), (0.030, 0.748, 0.205), bpv.CYAN_TRIM),
            ("cap-tiara-center-gem","head", (-0.018, 0.692, 0.185), (0.018, 0.732, 0.210), bpv.FROST_ACCENT),
            ("cap-tiara-center-star","head", (-0.010, 0.705, 0.192), (0.010, 0.722, 0.215), bpv.WHITE),
            # Flanking Tiered Spires
            ("cap-tiara-spire-l1",  "head", (0.038, 0.672, 0.174), (0.075, 0.725, 0.195), bpv.CYAN_TRIM),
            ("cap-tiara-gem-l1",    "head", (0.046, 0.685, 0.180), (0.068, 0.712, 0.200), bpv.FROST_ACCENT),
            ("cap-tiara-spire-r1",  "head", (-0.075, 0.672, 0.174), (-0.038, 0.725, 0.195), bpv.CYAN_TRIM),
            ("cap-tiara-gem-r1",    "head", (-0.068, 0.685, 0.180), (-0.046, 0.712, 0.200), bpv.FROST_ACCENT),
            ("cap-tiara-spire-l2",  "head", (0.080, 0.668, 0.172), (0.108, 0.702, 0.190), bpv.CYAN_TRIM),
            ("cap-tiara-spire-r2",  "head", (-0.108, 0.668, 0.172), (-0.080, 0.702, 0.190), bpv.CYAN_TRIM),
        ]

    elif variant_num == 3:
        # ❄️ ITERATION 3: Winter Trapper Chef Toque (Plush Fur Visor + Warm Earflaps + Top Pom-Pom)
        headpiece = [
            # Huge Chef Crown Body
            ("cap-trapper-body",    "head", (-0.195, 0.660, -0.195), (0.195, 0.750, 0.195), bpv.WHITE),
            ("cap-trapper-top",     "head", (-0.160, 0.750, -0.160), (0.160, 0.782, 0.160), bpv.WHITE),
            # Fluffy Snow Pom-Pom on peak
            ("cap-pompom-main",     "head", (-0.048, 0.755, -0.048), (0.048, 0.788, 0.048), bpv.WHITE),
            ("cap-pompom-frost",    "head", (-0.028, 0.762, -0.028), (0.028, 0.785, 0.028), bpv.FROST_ACCENT),
            # Plush Turn-up Fur Visor Brim
            ("cap-visor-base",      "head", (-0.180, 0.640, 0.165), (0.180, 0.695, 0.210), bpv.CYAN_TRIM),
            ("cap-visor-fur",       "head", (-0.170, 0.650, 0.175), (0.170, 0.702, 0.218), bpv.WHITE),
            ("cap-visor-frost-star","head", (-0.030, 0.662, 0.212), (0.030, 0.690, 0.224), bpv.FROST_ACCENT),
            ("cap-visor-star-core", "head", (-0.015, 0.668, 0.218), (0.015, 0.684, 0.226), bpv.WHITE),
            # Cozy Side Earflaps
            ("cap-earflap-l",       "head", (0.175, 0.460, -0.040), (0.212, 0.670, 0.080), bpv.WHITE),
            ("cap-earflap-trim-l",  "head", (0.180, 0.450, -0.035), (0.216, 0.515, 0.075), bpv.CYAN_TRIM),
            ("cap-earflap-r",       "head", (-0.212, 0.460, -0.040), (-0.175, 0.670, 0.080), bpv.WHITE),
            ("cap-earflap-trim-r",  "head", (-0.216, 0.450, -0.035), (-0.180, 0.515, 0.075), bpv.CYAN_TRIM),
        ]

    else:
        # ⚡ ITERATION 4: Pastry Meister Visor Toque + Holographic HUD Scanner & Wing Barrettes
        headpiece = grand_toque_base + [
            # Forward-projecting Curved Visor Peak on Hat Brim (y=0.655 to 0.678, z=0.175 to 0.230)
            ("visor-peak-center",   "head", (-0.120, 0.655, 0.175), (0.120, 0.675, 0.228), bpv.CYAN_TRIM),
            ("visor-peak-rim",      "head", (-0.115, 0.650, 0.215), (0.115, 0.668, 0.232), bpv.WHITE),
            ("visor-peak-stripe",   "head", (-0.040, 0.662, 0.185), (0.040, 0.678, 0.225), bpv.FROST_ACCENT),

            # Holographic Sci-Fi / Baker HUD Scanner over Right Eye (-X)
            ("hud-headband",        "head", (-0.185, 0.585, 0.145), (0.185, 0.608, 0.178), bpv.SILVER),
            ("hud-frame-r",         "head", (-0.155, 0.540, 0.170), (-0.015, 0.625, 0.208), bpv.SILVER),
            ("hud-lens-r",          "head", (-0.145, 0.550, 0.178), (-0.025, 0.615, 0.215), bpv.CYAN_TRIM),
            ("hud-reticle",         "head", (-0.115, 0.570, 0.190), (-0.055, 0.600, 0.218), bpv.FROST_ACCENT),
            ("hud-sensor-gem",      "head", (-0.162, 0.570, 0.155), (-0.145, 0.605, 0.198), bpv.FROST_ACCENT),
            
            # Twin Frost Wing Clips on Left Temple (+X)
            ("hair-wing-base",      "head", (0.140, 0.590, 0.150), (0.175, 0.620, 0.188), bpv.SILVER),
            ("hair-wing-feather-1", "head", (0.155, 0.605, 0.130), (0.190, 0.640, 0.175), bpv.CYAN_TRIM),
            ("hair-wing-feather-2", "head", (0.155, 0.580, 0.130), (0.188, 0.610, 0.175), bpv.CYAN_TRIM),
            ("hair-wing-gem",       "head", (0.165, 0.592, 0.160), (0.182, 0.612, 0.182), bpv.FROST_ACCENT),
        ]

    full_head = core_head_back_and_sides + sculpted_3d_bangs + headpiece
    return leg_l, leg_r, torso, arm_l, arm_r, full_head

def build_all():
    target_dir = "Assets/TumbangPreso/Art/characters/persons"
    os.makedirs(target_dir, exist_ok=True)
    
    variants = [
        (1, "iteration-1.glb", PALETTE_BROWN),  # 1: Frost Baker Ski Goggles on Grand Volumetric Toque
        (2, "iteration-2.glb", PALETTE_BROWN),  # 2: Grand Frost Tiara & Bow on Grand Volumetric Toque
        (3, "iteration-3.glb", PALETTE_BROWN),  # 3: Winter Trapper Toque with Earflaps & Plush Visor
        (4, "iteration-4.glb", PALETTE_BROWN),  # 4: Pastry Meister Visor Toque + Holographic HUD Scanner
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
