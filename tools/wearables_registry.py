"""Voxel Wearables & Accessories Registry for Tumbang Preso Characters.

This registry stores modular voxel definitions for character headwear, eyewear,
and accessories. These presets can be dynamically equipped, swapped, or layered
onto character voxel rigs for customization features.
"""

import copy

# =============================================================================
# 🎨 PALETTE SLOTS (Standard Voxel Index Mappings)
# =============================================================================
WHITE = 0
SILVER = 1
CYAN_TRIM = 2
FROST_ACCENT = 6
INK = 8
OVERALLS = 9
WOOD_GOLD = 10
OVERALLS_DARK = 11
COLLAR_TRIM = 12
SKIN = 13
SKIN_DARK = 14
SKIN_LIT = 15
HAIR = 8

# =============================================================================
# 🥽 EYEWEAR / HEAD ACCESSORIES
# =============================================================================

# Pro High-Tech Ski Goggles (Curved conformal wrap around forehead & crown)
PRO_SKI_GOGGLES = [
    # Full Wraparound Strap around hat (y=0.638 to 0.672)
    ("goggle-strap-b",      "head", (-0.180, 0.638, -0.234), (0.180, 0.672, -0.200), OVERALLS_DARK),
    ("goggle-strap-l",      "head", (0.176, 0.638, -0.214), (0.208, 0.672, 0.140), OVERALLS_DARK),
    ("goggle-strap-r",      "head", (-0.208, 0.638, -0.214), (-0.176, 0.672, 0.140), OVERALLS_DARK),
    ("goggle-strap-trim-l", "head", (0.180, 0.648, -0.120), (0.210, 0.662, 0.120), CYAN_TRIM),
    ("goggle-strap-trim-r", "head", (-0.206, 0.648, -0.120), (-0.180, 0.662, 0.120), CYAN_TRIM),
    
    # Chunky Silver Side Temple Hinge Brackets
    ("goggle-hinge-l",      "head", (0.168, 0.628, 0.140), (0.202, 0.688, 0.195), SILVER),
    ("goggle-hinge-r",      "head", (-0.202, 0.628, 0.140), (-0.168, 0.688, 0.195), SILVER),
    
    # Outer Metallic Silver Frame Chassis (Curved brow)
    ("goggle-frame-l",      "head", (0.015, 0.625, 0.195), (0.165, 0.700, 0.240), SILVER),
    ("goggle-frame-r",      "head", (-0.165, 0.625, 0.195), (-0.015, 0.700, 0.240), SILVER),
    ("goggle-frame-wrap-l", "head", (0.142, 0.628, 0.148), (0.190, 0.694, 0.210), SILVER),
    ("goggle-frame-wrap-r", "head", (-0.190, 0.628, 0.148), (-0.142, 0.694, 0.210), SILVER),
    
    ("goggle-bridge-top",   "head", (-0.020, 0.660, 0.200), (0.020, 0.694, 0.235), SILVER),
    ("goggle-bridge-arch",  "head", (-0.015, 0.638, 0.202), (0.015, 0.662, 0.230), SILVER),
    ("goggle-bridge-rivet", "head", (-0.010, 0.670, 0.232), (0.010, 0.686, 0.240), WHITE),
    
    # Glowing Dual Frost-Cyan Crystal Lenses (Beveled forward profile)
    ("goggle-lens-l",       "head", (0.026, 0.635, 0.210), (0.152, 0.690, 0.246), CYAN_TRIM),
    ("goggle-lens-r",       "head", (-0.152, 0.635, 0.210), (-0.026, 0.690, 0.246), CYAN_TRIM),
    ("goggle-lens-wrap-l",  "head", (0.142, 0.638, 0.158), (0.180, 0.686, 0.215), CYAN_TRIM),
    ("goggle-lens-wrap-r",  "head", (-0.180, 0.638, 0.158), (-0.142, 0.686, 0.215), CYAN_TRIM),
    
    # Crystalline Depth & Specular Glints
    ("goggle-depth-l",      "head", (0.032, 0.660, 0.225), (0.145, 0.686, 0.248), FROST_ACCENT),
    ("goggle-depth-r",      "head", (-0.145, 0.660, 0.225), (-0.032, 0.686, 0.248), FROST_ACCENT),
    ("goggle-glint-l",      "head", (0.045, 0.668, 0.232), (0.090, 0.684, 0.250), WHITE),
    ("goggle-glint-r",      "head", (-0.138, 0.668, 0.232), (-0.092, 0.684, 0.250), WHITE),
]

# Frosted Star Hair Clasp & Fluttering Twin Ribbon Tails
FROSTED_STAR_RIBBONS = [
    ("hair-back-star-clasp",    "head", (-0.030, 0.335, -0.228), (0.030, 0.375, -0.214), FROST_ACCENT),
    ("hair-back-star-gem",      "head", (-0.015, 0.342, -0.232), (0.015, 0.368, -0.216), WHITE),
    ("hair-back-ribbon-tail-l", "head", (0.020, 0.230, -0.225), (0.065, 0.340, -0.215), CYAN_TRIM),
    ("hair-back-ribbon-tail-r", "head", (-0.065, 0.230, -0.225), (-0.020, 0.340, -0.215), CYAN_TRIM),
    ("hair-back-ribbon-tip-l",  "head", (0.035, 0.200, -0.225), (0.075, 0.235, -0.215), FROST_ACCENT),
    ("hair-back-ribbon-tip-r",  "head", (-0.075, 0.200, -0.225), (-0.035, 0.235, -0.215), FROST_ACCENT),
]

# Asymmetric Earrings (Right ear: dangling frost drop, Left ear: silver stud & frost gem)
ASYMMETRIC_EARRINGS = [
    ("earring-stud-r",          "head", (-0.215, 0.485, -0.015), (-0.200, 0.508, 0.005), SILVER),
    ("earring-frost-drop-r",    "head", (-0.218, 0.435, -0.018), (-0.198, 0.485, 0.008), CYAN_TRIM),
    ("earring-frost-sparkle-r", "head", (-0.219, 0.442, -0.012), (-0.204, 0.465, 0.004), FROST_ACCENT),
    ("earring-stud-l",          "head", (0.200, 0.485, -0.015), (0.215, 0.508, 0.005), SILVER),
    ("earring-gem-l",           "head", (0.202, 0.488, -0.010), (0.218, 0.505, 0.002), FROST_ACCENT),
]

# =============================================================================
# 🧢 THE 4 WEARABLE HEADPIECES
# =============================================================================

# --- 1. ORGANIC VOLUMETRIC EXPEDITION USHANKA / TRAIPER HAT ---
WEARABLE_USHANKA = [
    # 1. 👑 VOLUMETRIC PUFFY QUILTED CROWN (Chamfered multi-tier dome)
    # Main puffy central core
    ("ush-crown-core",      "head", (-0.180, 0.630, -0.205), (0.180, 0.730, 0.155), OVERALLS),
    # Puffy lateral bulges
    ("ush-crown-side-l",    "head", (0.170, 0.635, -0.180), (0.206, 0.725, 0.130), OVERALLS),
    ("ush-crown-side-r",    "head", (-0.206, 0.635, -0.180), (-0.170, 0.725, 0.130), OVERALLS),
    # Puffy front/back bulges
    ("ush-crown-front",     "head", (-0.165, 0.635, 0.140), (0.165, 0.725, 0.182), OVERALLS),
    ("ush-crown-back",      "head", (-0.165, 0.635, -0.226), (0.165, 0.725, -0.188), OVERALLS),
    # Puffy 45° corner chamfers (gives rounded circular dome from 3/4 view)
    ("ush-crown-fl",        "head", (0.130, 0.635, 0.110), (0.192, 0.725, 0.168), OVERALLS),
    ("ush-crown-fr",        "head", (-0.192, 0.635, 0.110), (-0.130, 0.725, 0.168), OVERALLS),
    ("ush-crown-bl",        "head", (0.130, 0.635, -0.216), (0.192, 0.725, -0.148), OVERALLS),
    ("ush-crown-br",        "head", (-0.192, 0.635, -0.216), (-0.130, 0.725, -0.148), OVERALLS),
    
    # Upper dome taper tier (y=0.725 to 0.768)
    ("ush-dome-top",        "head", (-0.155, 0.725, -0.175), (0.155, 0.768, 0.130), OVERALLS),
    ("ush-dome-top-l",      "head", (0.140, 0.725, -0.145), (0.175, 0.760, 0.105), OVERALLS),
    ("ush-dome-top-r",      "head", (-0.175, 0.725, -0.145), (-0.140, 0.760, 0.105), OVERALLS),
    ("ush-dome-top-f",      "head", (-0.130, 0.725, 0.110), (0.130, 0.760, 0.152), OVERALLS),
    ("ush-dome-top-b",      "head", (-0.130, 0.725, -0.198), (0.130, 0.760, -0.155), OVERALLS),
    
    # Quilted upholstered seam ribs
    ("ush-seam-x",          "head", (-0.158, 0.730, -0.015), (0.158, 0.762, 0.015), OVERALLS_DARK),
    ("ush-seam-z",          "head", (-0.015, 0.730, -0.178), (0.015, 0.762, 0.135), OVERALLS_DARK),
    
    # Top fluffy snowball pom-pom bobble
    ("ush-pompom-base",     "head", (-0.058, 0.758, -0.058), (0.058, 0.785, 0.058), WHITE),
    ("ush-pompom-top",      "head", (-0.036, 0.782, -0.036), (0.036, 0.792, 0.036), WHITE),
    ("ush-pompom-star",     "head", (-0.016, 0.776, -0.016), (0.016, 0.788, 0.016), CYAN_TRIM),
    
    # 2. ❄️ CURVED UPTURNED PLUSH FUR VISOR BRIM (Curving around forehead)
    ("ush-visor-backing",   "head", (-0.170, 0.610, 0.155), (0.170, 0.690, 0.205), OVERALLS_DARK),
    ("ush-visor-center",    "head", (-0.142, 0.610, 0.175), (0.142, 0.702, 0.238), WHITE),
    ("ush-visor-bevel-l",   "head", (0.120, 0.610, 0.150), (0.185, 0.696, 0.222), WHITE),
    ("ush-visor-bevel-r",   "head", (-0.185, 0.610, 0.150), (-0.120, 0.696, 0.222), WHITE),
    ("ush-visor-lip",       "head", (-0.148, 0.665, 0.195), (0.148, 0.708, 0.244), WHITE),
    ("ush-visor-star",      "head", (-0.028, 0.648, 0.230), (0.028, 0.682, 0.246), CYAN_TRIM),
    ("ush-visor-star-core", "head", (-0.014, 0.655, 0.238), (0.014, 0.675, 0.248), FROST_ACCENT),
    
    # 3. 👂 SCULPTED, ORGANIC, 360°-CONNECTED EARFLAPS
    # Upper Temple Root (y=0.54 to 0.65)
    ("ush-flap-root-l",     "head", (0.170, 0.540, -0.150), (0.222, 0.650, 0.110), OVERALLS),
    ("ush-flap-root-r",     "head", (-0.222, 0.540, -0.150), (-0.170, 0.650, 0.110), OVERALLS),
    
    # Mid Ear Bell Swell (y=0.415 to 0.565) - Volumetric cushion
    ("ush-flap-bell-l",     "head", (0.175, 0.415, -0.150), (0.236, 0.565, 0.105), OVERALLS),
    ("ush-flap-cushion-l",  "head", (0.220, 0.430, -0.130), (0.240, 0.550, 0.085), OVERALLS_DARK),
    ("ush-flap-star-l",     "head", (0.234, 0.470, -0.035), (0.242, 0.510, 0.015), CYAN_TRIM),
    ("ush-flap-gem-l",      "head", (0.238, 0.482, -0.020), (0.244, 0.498, -0.000), FROST_ACCENT),
    
    ("ush-flap-bell-r",     "head", (-0.236, 0.415, -0.150), (-0.175, 0.565, 0.105), OVERALLS),
    ("ush-flap-cushion-r",  "head", (-0.240, 0.430, -0.130), (-0.220, 0.550, 0.085), OVERALLS_DARK),
    ("ush-flap-star-r",     "head", (-0.242, 0.470, -0.035), (-0.234, 0.510, 0.015), CYAN_TRIM),
    ("ush-flap-gem-r",      "head", (-0.244, 0.482, -0.020), (-0.238, 0.498, -0.000), FROST_ACCENT),
    
    # Seamless Corner Wedges (Connecting Earflap into Back Mantle at 45°)
    ("ush-flap-wedge-l",    "head", (0.145, 0.415, -0.216), (0.212, 0.640, -0.140), OVERALLS),
    ("ush-flap-wedge-r",    "head", (-0.212, 0.415, -0.216), (-0.145, 0.640, -0.140), OVERALLS),
    
    # Lower Jawline Wrap (y=0.325 to 0.44) - Hugs cheek
    ("ush-flap-jaw-l",      "head", (0.170, 0.325, -0.130), (0.222, 0.440, 0.080), OVERALLS),
    ("ush-flap-jaw-r",      "head", (-0.222, 0.325, -0.130), (-0.170, 0.440, 0.080), OVERALLS),
    
    # Plush White Fur Flap Trim & Inner Face Peeking Out
    ("ush-flap-fur-l",      "head", (0.165, 0.315, -0.140), (0.232, 0.390, 0.090), WHITE),
    ("ush-flap-inner-fur-l","head", (0.162, 0.335, 0.025), (0.202, 0.485, 0.095), WHITE),
    ("ush-flap-fur-r",      "head", (-0.232, 0.315, -0.140), (-0.165, 0.390, 0.090), WHITE),
    ("ush-flap-inner-fur-r","head", (-0.202, 0.335, 0.025), (-0.162, 0.485, 0.095), WHITE),
    
    # Dangling Braided Cords & Fluffy Pom-Poms
    ("ush-cord-l",          "head", (0.192, 0.255, -0.025), (0.212, 0.330, 0.005), CYAN_TRIM),
    ("ush-cord-r",          "head", (-0.212, 0.255, -0.025), (-0.192, 0.330, 0.005), CYAN_TRIM),
    ("ush-cord-pompom-l",   "head", (0.182, 0.215, -0.038), (0.222, 0.260, 0.018), WHITE),
    ("ush-cord-pompom-r",   "head", (-0.222, 0.215, -0.038), (-0.182, 0.260, 0.018), WHITE),
    
    # 4. 🔙 CURVED NAPE MANTLE (Volumetric Multi-Layered Padded Quilt)
    # Upper Mantle (y=0.49 to 0.65)
    ("ush-back-upper-c",    "head", (-0.170, 0.490, -0.234), (0.170, 0.650, -0.155), OVERALLS),
    ("ush-back-upper-pad",  "head", (-0.150, 0.505, -0.238), (0.150, 0.630, -0.170), OVERALLS),
    ("ush-back-upper-l",    "head", (0.128, 0.490, -0.228), (0.192, 0.650, -0.155), OVERALLS),
    ("ush-back-upper-r",    "head", (-0.192, 0.490, -0.228), (-0.128, 0.650, -0.155), OVERALLS),
    
    # Quilted horizontal crease
    ("ush-back-quilt-seam", "head", (-0.165, 0.490, -0.236), (0.165, 0.505, -0.160), OVERALLS_DARK),
    
    # Lower Mantle (y=0.375 to 0.495)
    ("ush-back-lower-c",    "head", (-0.165, 0.375, -0.230), (0.165, 0.495, -0.165), OVERALLS),
    ("ush-back-lower-l",    "head", (0.125, 0.375, -0.224), (0.185, 0.495, -0.165), OVERALLS),
    ("ush-back-lower-r",    "head", (-0.185, 0.375, -0.224), (-0.125, 0.495, -0.165), OVERALLS),
    
    # Unified 360° Puffy Fur Hem (Wrapping smoothly to earflaps)
    ("ush-back-fur-main",   "head", (-0.168, 0.365, -0.238), (0.168, 0.412, -0.170), WHITE),
    ("ush-back-fur-lip",    "head", (-0.155, 0.360, -0.240), (0.155, 0.395, -0.190), WHITE),
    ("ush-back-fur-conn-l", "head", (0.135, 0.350, -0.230), (0.204, 0.405, -0.135), WHITE),
    ("ush-back-fur-conn-r", "head", (-0.204, 0.350, -0.230), (-0.135, 0.405, -0.135), WHITE),
    
    # Tailored Spine Seam
    ("ush-back-strap",      "head", (-0.015, 0.410, -0.234), (0.015, 0.645, -0.224), OVERALLS_DARK),
]

# --- 2. VOLUMETRIC SLOUCHY KNIT BEANIE ---
WEARABLE_BEANIE = [
    # Big Puffy Slouchy Beanie Crown (drapes naturally back over skull y=0.615 to 0.745)
    ("bean-body-base",      "head", (-0.195, 0.615, -0.215), (0.195, 0.710, 0.165), OVERALLS),
    ("bean-body-mid",       "head", (-0.198, 0.660, -0.224), (0.198, 0.730, 0.145), OVERALLS),
    ("bean-body-slouch",    "head", (-0.165, 0.685, -0.230), (0.165, 0.745, -0.010), OVERALLS),
    ("bean-rib-dark",       "head", (-0.200, 0.640, -0.035), (0.200, 0.720, 0.035), OVERALLS_DARK),
    
    # Giant Fluffy Snowball Bobble Pom-Pom (y=0.720 to 0.765, z=-0.180 to -0.060)
    ("bean-pompom",         "head", (-0.060, 0.720, -0.180), (0.060, 0.765, -0.060), WHITE),
    ("bean-pompom-star",    "head", (-0.025, 0.728, -0.150), (0.025, 0.758, -0.090), CYAN_TRIM),
    
    # Thick 360° Folded Knit Rim Cuff (Encircling the entire head y=0.590 to 0.660)
    ("bean-cuff-front",     "head", (-0.198, 0.590, 0.140), (0.198, 0.660, 0.198), WHITE),
    ("bean-cuff-back",      "head", (-0.198, 0.590, -0.230), (0.198, 0.660, -0.170), WHITE),
    ("bean-cuff-side-l",    "head", (0.182, 0.590, -0.220), (0.208, 0.660, 0.175), WHITE),
    ("bean-cuff-side-r",    "head", (-0.208, 0.590, -0.220), (-0.182, 0.660, 0.175), WHITE),
    ("bean-cuff-stripe",    "head", (-0.200, 0.620, -0.232), (0.200, 0.638, 0.200), CYAN_TRIM),
    ("bean-cuff-patch",     "head", (-0.038, 0.598, 0.192), (0.038, 0.648, 0.208), OVERALLS_DARK),
    ("bean-cuff-star",      "head", (-0.020, 0.608, 0.200), (0.020, 0.638, 0.212), FROST_ACCENT),
]

# --- 3. VOLUMETRIC PASTRY BAKER BERET ---
WEARABLE_BERET = [
    # Fitted 360° Beret Base Band (y=0.605 to 0.655)
    ("beret-band-f",        "head", (-0.192, 0.605, 0.135), (0.192, 0.655, 0.185), OVERALLS_DARK),
    ("beret-band-b",        "head", (-0.192, 0.605, -0.225), (0.192, 0.655, -0.165), OVERALLS_DARK),
    ("beret-band-l",        "head", (0.178, 0.605, -0.215), (0.202, 0.655, 0.170), OVERALLS_DARK),
    ("beret-band-r",        "head", (-0.202, 0.605, -0.215), (-0.178, 0.655, 0.170), OVERALLS_DARK),
    ("beret-band-trim",     "head", (-0.195, 0.625, -0.228), (0.195, 0.642, 0.188), CYAN_TRIM),
    
    # Giant Flared Puffy Snow-White Baker Cloud Crown (y=0.655 to 0.770)
    ("beret-puff-base",     "head", (-0.230, 0.655, -0.230), (0.230, 0.715, 0.210), WHITE),
    ("beret-puff-top",      "head", (-0.210, 0.715, -0.210), (0.210, 0.755, 0.190), WHITE),
    ("beret-puff-dome",     "head", (-0.145, 0.755, -0.145), (0.145, 0.772, 0.125), WHITE),
    ("beret-stalk",         "head", (-0.018, 0.770, -0.035), (0.018, 0.790, -0.005), CYAN_TRIM),
    
    # Silver Snowflake Brooch & Frost Cyan Ribbon on Side
    ("beret-brooch",        "head", (-0.210, 0.635, 0.050), (-0.185, 0.678, 0.115), SILVER),
    ("beret-gem",           "head", (-0.214, 0.648, 0.070), (-0.188, 0.665, 0.095), FROST_ACCENT),
    ("beret-ribbon",        "head", (-0.200, 0.565, 0.065), (-0.190, 0.638, 0.098), CYAN_TRIM),
]

# --- 4. VOLUMETRIC FROST PLUSH EARMUFFS & HEADSET ---
WEARABLE_EARMUFFS = [
    # Thick Padded Overhead Headband (y=0.680 to 0.730, z=-0.040 to 0.050)
    ("muff-arch-top",       "head", (-0.180, 0.695, -0.025), (0.180, 0.730, 0.045), OVERALLS_DARK),
    ("muff-arch-trim",      "head", (-0.184, 0.708, -0.018), (0.184, 0.722, 0.038), CYAN_TRIM),
    ("muff-arch-pad",       "head", (-0.170, 0.680, -0.020), (0.170, 0.700, 0.040), WHITE),
    ("muff-arch-l",         "head", (0.175, 0.520, -0.020), (0.205, 0.700, 0.040), OVERALLS_DARK),
    ("muff-arch-r",         "head", (-0.205, 0.520, -0.020), (-0.175, 0.700, 0.040), OVERALLS_DARK),
    
    # Giant Plush Snow-White Fur Ear Muffs (Left Ear +X)
    ("muff-fur-main-l",     "head", (0.185, 0.380, -0.070), (0.235, 0.540, 0.090), WHITE),
    ("muff-fur-lip-l",      "head", (0.195, 0.400, -0.055), (0.238, 0.520, 0.075), WHITE),
    ("muff-core-l",         "head", (0.228, 0.430, -0.030), (0.242, 0.490, 0.050), CYAN_TRIM),
    ("muff-star-l",         "head", (0.235, 0.445, -0.015), (0.244, 0.475, 0.035), FROST_ACCENT),
    
    # Giant Plush Snow-White Fur Ear Muffs (Right Ear -X)
    ("muff-fur-main-r",     "head", (-0.235, 0.380, -0.070), (-0.185, 0.540, 0.090), WHITE),
    ("muff-fur-lip-r",      "head", (-0.238, 0.400, -0.055), (-0.195, 0.520, 0.075), WHITE),
    ("muff-core-r",         "head", (-0.242, 0.430, -0.030), (-0.228, 0.490, 0.050), CYAN_TRIM),
    ("muff-star-r",         "head", (-0.244, 0.445, -0.015), (-0.235, 0.475, 0.035), FROST_ACCENT),
]

# --- 5. TRADITIONAL FILIPINO CONICAL SALAKOT ---
WEARABLE_SALAKOT = [
    ("salakot-rim-base",   "head", (-0.260, 0.580, -0.260), (0.260, 0.605, 0.260), WOOD_GOLD),
    ("salakot-rim-inner",  "head", (-0.245, 0.595, -0.245), (0.245, 0.620, 0.245), OVERALLS),
    ("salakot-mid-cone",   "head", (-0.200, 0.610, -0.200), (0.200, 0.655, 0.200), WOOD_GOLD),
    ("salakot-mid-weave",  "head", (-0.175, 0.645, -0.175), (0.175, 0.690, 0.175), OVERALLS),
    ("salakot-apex-cone",  "head", (-0.120, 0.680, -0.120), (0.120, 0.730, 0.120), WOOD_GOLD),
    ("salakot-apex-cap",   "head", (-0.070, 0.720, -0.070), (0.070, 0.755, 0.070), OVERALLS),
    ("salakot-finial-pin", "head", (-0.020, 0.750, -0.020), (0.020, 0.778, 0.020), SILVER),
    # Woven Chin Ties
    ("salakot-tie-l",      "head", (0.160, 0.350, -0.010), (0.175, 0.585, 0.010), WOOD_GOLD),
    ("salakot-tie-r",      "head", (-0.175, 0.350, -0.010), (-0.160, 0.585, 0.010), WOOD_GOLD),
    ("salakot-tie-chin",   "head", (-0.050, 0.340, 0.030), (0.050, 0.355, 0.050), WOOD_GOLD),
]

# --- 6. ARCANE WITCH HAT WITH POTION VIALS ---
WEARABLE_WITCH_HAT = [
    # Wide Rim Brim (y=0.580 to 0.605)
    ("witch-brim-base",    "head", (-0.265, 0.580, -0.265), (0.265, 0.605, 0.265), INK),
    ("witch-brim-edge",    "head", (-0.275, 0.585, -0.275), (0.275, 0.600, 0.275), INK),
    # Purple Ribbon Band & Gold Buckle
    ("witch-band",         "head", (-0.205, 0.605, -0.205), (0.205, 0.640, 0.205), OVERALLS),
    ("witch-buckle",       "head", (-0.045, 0.610, 0.208), (0.045, 0.638, 0.218), WOOD_GOLD),
    # Stepped Pointy Cone
    ("witch-cone-1",       "head", (-0.175, 0.635, -0.175), (0.175, 0.680, 0.175), INK),
    ("witch-cone-2",       "head", (-0.135, 0.675, -0.135), (0.135, 0.720, 0.135), INK),
    ("witch-cone-3",       "head", (-0.090, 0.715, -0.090), (0.090, 0.755, 0.090), INK),
    ("witch-cone-tip",     "head", (-0.045, 0.750, -0.060), (0.045, 0.778, 0.030), INK),
    # Side Potion Test Tubes (+X side)
    ("witch-vial-1-glass", "head", (0.210, 0.615, 0.050), (0.235, 0.720, 0.080), SILVER),
    ("witch-vial-1-cork",  "head", (0.212, 0.718, 0.052), (0.233, 0.740, 0.078), WOOD_GOLD),
    ("witch-vial-2-glass", "head", (0.210, 0.615, -0.030), (0.235, 0.700, -0.000), SILVER),
    ("witch-vial-2-cork",  "head", (0.212, 0.698, -0.028), (0.233, 0.720, -0.002), WOOD_GOLD),
]

# --- 7. OBSIDIAN DEMON HORNS ---
WEARABLE_DEMON_HORNS = [
    # Left Horn Root & Curve (+X)
    ("horn-root-l",        "head", (0.135, 0.570, 0.020), (0.190, 0.640, 0.090), INK),
    ("horn-mid-l",         "head", (0.165, 0.630, 0.000), (0.220, 0.705, 0.070), INK),
    ("horn-tip-l",         "head", (0.190, 0.695, -0.030), (0.235, 0.760, 0.040), INK),
    ("horn-vein-l",        "head", (0.170, 0.645, 0.015), (0.215, 0.690, 0.065), FROST_ACCENT),
    # Right Horn Root & Curve (-X)
    ("horn-root-r",        "head", (-0.190, 0.570, 0.020), (-0.135, 0.640, 0.090), INK),
    ("horn-mid-r",         "head", (-0.220, 0.630, 0.000), (-0.165, 0.705, 0.070), INK),
    ("horn-tip-r",         "head", (-0.235, 0.695, -0.030), (-0.190, 0.760, 0.040), INK),
    ("horn-vein-r",        "head", (-0.215, 0.645, 0.015), (-0.170, 0.690, 0.065), FROST_ACCENT),
]

# --- 8. RETRO 90S MATRIX BLACK SHADES ---
WEARABLE_RETRO_SHADES = [
    ("shades-lens-l",      "head", (0.025, 0.490, 0.170), (0.145, 0.535, 0.195), INK),
    ("shades-lens-r",      "head", (-0.145, 0.490, 0.170), (-0.025, 0.535, 0.195), INK),
    ("shades-bridge",      "head", (-0.030, 0.505, 0.172), (0.030, 0.525, 0.192), SILVER),
    ("shades-temple-l",    "head", (0.140, 0.495, 0.020), (0.195, 0.525, 0.180), SILVER),
    ("shades-temple-r",    "head", (-0.195, 0.495, 0.020), (-0.140, 0.525, 0.180), SILVER),
]

# --- 9. SILVER CUBAN LINK CHAIN & PENDANT ---
WEARABLE_CUBAN_CHAIN = [
    ("chain-neck-l",       "torso", (0.035, 0.280, -0.090), (0.080, 0.335, -0.078), SILVER),
    ("chain-neck-r",       "torso", (-0.080, 0.280, -0.090), (-0.035, 0.335, -0.078), SILVER),
    ("chain-pendant-gem",  "torso", (-0.020, 0.250, -0.094), (0.020, 0.282, -0.080), CYAN_TRIM),
]

# --- 10. GOOD MORNING TOWEL ---
WEARABLE_GOOD_MORNING_TOWEL = [
    ("towel-collar-back",  "torso", (-0.130, 0.290, 0.070), (0.130, 0.345, 0.095), WHITE),
    ("towel-drape-l",      "torso", (0.060, 0.160, -0.092), (0.115, 0.330, -0.072), WHITE),
    ("towel-drape-r",      "torso", (-0.115, 0.160, -0.092), (-0.060, 0.330, -0.072), WHITE),
    ("towel-stripe-l",     "torso", (0.062, 0.170, -0.094), (0.113, 0.190, -0.070), CYAN_TRIM),
    ("towel-stripe-r",     "torso", (-0.113, 0.170, -0.094), (-0.062, 0.190, -0.070), CYAN_TRIM),
]

# =============================================================================
# 📦 REGISTRY CATALOG DICTIONARY
# =============================================================================
WEARABLES_CATALOG = {
    "headwear/ushanka_expedition": {
        "name": "Volumetric Expedition Ushanka",
        "slot": "headwear",
        "description": "Cyan fabric trapper shell with turned-up snow-white fur visor, silver snowflake crest, side earflaps, and braided cord pom-pom ties.",
        "boxes": WEARABLE_USHANKA,
    },
    "headwear/beanie_slouchy": {
        "name": "Volumetric Slouchy Knit Beanie",
        "slot": "headwear",
        "description": "Chunky 360° folded white knit cuff with cyan athletic stripe and giant fluffy snowball bobble pom-pom.",
        "boxes": WEARABLE_BEANIE,
    },
    "headwear/beret_baker": {
        "name": "Volumetric Pastry Meister Baker Beret",
        "slot": "headwear",
        "description": "Chic flared snow-white puffy pastry cloud beret with fitted cyan base band, silver snowflake brooch, and fluttering side ribbon tails.",
        "boxes": WEARABLE_BERET,
    },
    "headwear/earmuffs_frost": {
        "name": "Volumetric Frost Plush Earmuffs & Headset",
        "slot": "headwear",
        "description": "Giant plush snow-white fur ear warmers with cyan crystal snowflake cores and a padded overhead arch band.",
        "boxes": WEARABLE_EARMUFFS,
    },
    "headwear/salakot_woven": {
        "name": "Traditional Woven Salakot",
        "slot": "headwear",
        "description": "Filipino conical sun hat with double-layered rattan weave, wooden apex finial, and chin cords.",
        "boxes": WEARABLE_SALAKOT,
    },
    "headwear/witch_hat_arcane": {
        "name": "Arcane Witch Hat with Potion Vials",
        "slot": "headwear",
        "description": "Pointed stepped witch hat with gold buckle, purple hatband, and twin side potion test tubes.",
        "boxes": WEARABLE_WITCH_HAT,
    },
    "headwear/demon_horns_obsidian": {
        "name": "Obsidian Demon Horns",
        "slot": "headwear",
        "description": "Curving volcanic demon horns with glowing magma vein accents.",
        "boxes": WEARABLE_DEMON_HORNS,
    },
    "eyewear/ski_goggles_pro": {
        "name": "Pro High-Tech Ski Goggles",
        "slot": "eyewear",
        "description": "Perched metallic silver chassis with dual cyan crystal lenses, specular glints, and wraparound strap.",
        "boxes": PRO_SKI_GOGGLES,
    },
    "eyewear/retro_shades_matrix": {
        "name": "Retro 90s Street Shades",
        "slot": "eyewear",
        "description": "Slim rectangular black sunglasses with silver temple frames.",
        "boxes": WEARABLE_RETRO_SHADES,
    },
    "hair_accessory/star_ribbons": {
        "name": "Frosted Star Clasp & Fluttering Twin Ribbons",
        "slot": "hair_accessory",
        "description": "Silver-rimmed frosted star clasp with white diamond gem and twin cyan fluttering ribbon tails.",
        "boxes": FROSTED_STAR_RIBBONS,
    },
    "jewelry/asymmetric_frost_earrings": {
        "name": "Asymmetric Frost Drop Earrings",
        "slot": "jewelry",
        "description": "Silver stud with dangling frost cyan droplet on right ear, silver stud with frost gem on left ear.",
        "boxes": ASYMMETRIC_EARRINGS,
    },
    "jewelry/cuban_chain_silver": {
        "name": "Silver Cuban Link Chain & Pendant",
        "slot": "jewelry",
        "description": "Chunky silver link street necklace with a radiant crystal gemstone pendant.",
        "boxes": WEARABLE_CUBAN_CHAIN,
    },
    "accessory/good_morning_towel": {
        "name": "Classic Good Morning Towel",
        "slot": "accessory",
        "description": "White cotton shoulder towel with traditional red and blue border stripes.",
        "boxes": WEARABLE_GOOD_MORNING_TOWEL,
    },
}

def get_wearable(id_str):
    """Retrieve boxes for a specific wearable by identifier."""
    if id_str in WEARABLES_CATALOG:
        return copy.deepcopy(WEARABLES_CATALOG[id_str]["boxes"])
    raise KeyError(f"Wearable '{id_str}' not found in registry.")

def list_wearables():
    """Return all available wearable IDs."""
    return list(WEARABLES_CATALOG.keys())

