"""Builds phaister-doll.glb: Phaister's voodoo doll, awake (HERO-10 v3, the VOODOO DOLL ultimate). v8, remade from nothing.

    python tools/build_phaister_doll_voxel.py

Owner, 2026-09-27: *"create a new model for the voodoo i guess"*; on v3 *"why does it have her hair"*, *"and her hat"*, *"make it
look like its own vooodoo"*; on v5 *"make it look way more detailed"*, *"add crosshatch"*, and with a reference of a stitched bear
with glowing seams, *"make it look liek its glowing inside and it stitched tgthr ... let it be its own"*; then on v7 (which was
still the cast's body under the cloth) *"dude this shit sucks i wanted a complete overhaul i didnt want it to look like a fucking
character"*, *"I gave u permission to compeltley remake it"*.

So v8 is NOT a cast body. It keeps only the rig's seven bone NAMES (so the gait, animator and bot can still drive it) on a
skeleton of its own, and its body is a thing, not a person: a hulking, hunched stuffed poppet, far taller than a player, with a
small lumpy sack head sunk low between huge shoulders, a barrel of patchwork cloth with a hump, long heavy arms that hang to the
ground ending in stubby stitched fingers, and stumpy legs. It is split open along its seams, the soul light bursting through,
held shut by big X stitches (the glow is its own mesh, `glow-mesh`, painted unlit by `SoulGlow`). The design and its history:
`ArtSource/phaister/doll-20260927/design-brief.md`.

Every part below is typed by hand with its own numbers and the two sides are drawn separately. Tilted and round parts are
oriented boxes and discs. It imports `build_phaister_voxel` for the glb reader and writer, the chamfer and the mesh builder only;
that builder is never run from here (re-running it loses Phaister's baked clips).

Axes as every builder here: +X is the doll's LEFT, its face is on -Z, feet on y = 0, rig units (the game draws people at 2.38).
"""
import json
import math
import os
import struct
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_phaister_voxel as V  # noqa: E402  (geometry helpers only; see the module note)

OUT = "Assets/TumbangPreso/Art/characters/persons/phaister-doll.glb"
PALETTE_OUT = "ArtSource/phaister/doll-20260927/palette.json"

# ---------------------------------------------------------------------------------------------------------------------
# ITS OWN SKELETON. The seven names are the rig's; where they sit is the doll's: short legs set wide, a tall hunched body,
# shoulders out at the sides, the head low and forward between them.
# ---------------------------------------------------------------------------------------------------------------------
SKELETON = {
    "root":      (0.0,    0.0,   0.0),
    "leg-left":  (0.120,  0.215, 0.0),
    "leg-right": (-0.120, 0.215, 0.0),
    "torso":     (0.0,    0.215, 0.0),
    "arm-left":  (0.250,  0.600, 0.0),
    "arm-right": (-0.250, 0.600, 0.0),
    "head":      (0.0,    0.660, -0.020),
}
PARENT = {"leg-left": "root", "leg-right": "root", "torso": "root",
          "arm-left": "torso", "arm-right": "torso", "head": "torso"}
MIN_HEIGHT, MAX_HEIGHT = 1.00, 1.15

# ---------------------------------------------------------------------------------------------------------------------
# PALETTE. Linen and patchwork cloth; her colours only in the pin heads and the purple patch; the soul light. Slot 8 is ink.
# ---------------------------------------------------------------------------------------------------------------------
BURLAP = 0          # the main sackcloth
BURLAP_SHADOW = 1   # the gathered hem, the soles, the stumps of its hands
BURLAP_LIGHT = 2    # the head, a shade lighter so the face reads first
GOLD = 3            # pin heads
TWINE = 4           # the bindings
STUFFING = 5        # stuffing coming out
PIN_MAGENTA = 6     # pin heads
FABRIC_DARK = 7     # the darker patchwork cloth
INK = 8             # every stitch, the X eye
PATCH = 9           # her purple: a patch, the button eye
CRIMSON = 10        # a crimson patch, pin heads
FABRIC_GREY = 11    # a faded grey scrap of the patchwork
PIN_HEAD = 12       # her lilac: pin heads
PIN_SHAFT = 13      # the pins' shafts
GLOW_CORE = 14      # the hot line down the middle of every split (`SoulGlow` `_Core`)
GLOW = 15           # the soul light: every part in this slot or GLOW_CORE goes to the glow mesh

PALETTE = {
    BURLAP:        "b59c74",
    BURLAP_SHADOW: "8c7352",
    BURLAP_LIGHT:  "c6ae86",
    GOLD:          "f8b824",
    TWINE:         "e2d2a8",
    STUFFING:      "f3e9c9",
    PIN_MAGENTA:   "d8186e",
    FABRIC_DARK:   "6f5a43",
    INK:           "14101c",
    PATCH:         "4a1e78",
    CRIMSON:       "8c1424",
    FABRIC_GREY:   "8a8578",
    PIN_HEAD:      "9838d8",
    PIN_SHAFT:     "b87814",
    GLOW_CORE:     "ffd9f5",
    GLOW:          "ff54e6",
}

# ---------------------------------------------------------------------------------------------------------------------
# LEGS. Stumps set wide under the body, feet like stuffed pads. The left in linen with a crosshatched patch on the knee; the
# right in the darker patchwork cloth, twine bound at the ankle, split and stitched down its front.
# ---------------------------------------------------------------------------------------------------------------------
LEG_LEFT = [
    ("leg-l",            "leg-left", (0.040, 0.030, -0.105), (0.205, 0.240, 0.095), BURLAP),
    ("foot-l",           "leg-left", (0.030, 0.000, -0.150), (0.215, 0.070, 0.105), BURLAP_SHADOW),
    ("toe-l",            "leg-left", (0.052, 0.010, -0.174), (0.194, 0.056, -0.140), BURLAP_SHADOW),
    ("knee-patch-l",     "leg-left", (0.064, 0.110, -0.113), (0.170, 0.196, -0.101), CRIMSON),
]
LEG_RIGHT = [
    ("leg-r",            "leg-right", (-0.205, 0.030, -0.100), (-0.040, 0.235, 0.098), FABRIC_DARK),
    ("foot-r",           "leg-right", (-0.212, 0.000, -0.146), (-0.032, 0.066, 0.104), BURLAP_SHADOW),
    ("toe-r",            "leg-right", (-0.190, 0.010, -0.168), (-0.054, 0.052, -0.136), BURLAP_SHADOW),
    ("ankle-twine-r",    "leg-right", (-0.210, 0.062, -0.108), (-0.034, 0.082, 0.106), TWINE),
    ("ankle-knot-r",     "leg-right", (-0.226, 0.056, -0.030), (-0.204, 0.088, 0.010), TWINE),
]

# ---------------------------------------------------------------------------------------------------------------------
# BODY. A barrel of stuffed cloth: a big round belly pushed forward, broad shoulders, a hump behind, a gathered hem. Sewn from
# patches: a dark panel over its left chest, a grey scrap low on its right, a purple patch on the belly, a crimson one on the
# hump. Split down the front along the dark panel's seam and down the spine, the light showing (CRACKS), held by big X stitches.
# ---------------------------------------------------------------------------------------------------------------------
TORSO = [
    ("belly",            "torso", (-0.235, 0.205, -0.215), (0.235, 0.470, 0.120), BURLAP),
    ("chest",            "torso", (-0.265, 0.420, -0.178), (0.265, 0.640, 0.150), BURLAP),
    ("hump",             "torso", (-0.200, 0.500, 0.060), (0.200, 0.690, 0.215), BURLAP),
    ("shoulder-l",       "torso", (0.190, 0.515, -0.125), (0.312, 0.665, 0.125), BURLAP),
    ("shoulder-r",       "torso", (-0.312, 0.515, -0.125), (-0.190, 0.665, 0.125), BURLAP),
    ("hem",              "torso", (-0.222, 0.182, -0.150), (0.222, 0.240, 0.112), BURLAP_SHADOW),
    ("chest-panel-l",    "torso", (0.036, 0.440, -0.186), (0.252, 0.622, -0.172), FABRIC_DARK),
    ("scrap-r",          "torso", (-0.214, 0.232, -0.224), (-0.080, 0.330, -0.210), FABRIC_GREY),
    ("neck-twine",       "torso", (-0.132, 0.626, -0.132), (0.132, 0.664, 0.122), TWINE),
    ("neck-knot",        "torso", (-0.096, 0.614, -0.150), (-0.056, 0.668, -0.126), TWINE),
    ("waist-twine",      "torso", (-0.238, 0.300, -0.222), (0.238, 0.322, 0.126), TWINE),
    ("waist-knot",       "torso", (0.120, 0.292, -0.236), (0.160, 0.330, -0.214), TWINE),
    # The gris-gris pouch, hanging from the waist twine at its left hip.
    ("pouch",            "torso", (0.150, 0.214, -0.252), (0.214, 0.282, -0.212), CRIMSON),
    ("pouch-neck",       "torso", (0.164, 0.280, -0.246), (0.200, 0.296, -0.218), CRIMSON),
    ("pouch-tie",        "torso", (0.162, 0.286, -0.248), (0.202, 0.294, -0.216), TWINE),
    # Stuffing out of a torn hem at the back of its right hip.
    ("stuffing-a",       "torso", (-0.206, 0.186, 0.100), (-0.156, 0.226, 0.136), STUFFING),
    ("stuffing-b",       "torso", (-0.186, 0.172, 0.116), (-0.144, 0.204, 0.150), STUFFING),
    ("stuffing-c",       "torso", (-0.222, 0.206, 0.092), (-0.184, 0.238, 0.126), STUFFING),
]

# ---------------------------------------------------------------------------------------------------------------------
# ARMS. Long and heavy, far longer than a player's, so they hang to the ground: a sack upper arm, a thicker forearm, a stump of a
# hand with three stubby stitched fingers and a thumb. Twine at the wrists. The left in linen; the right's upper arm in the dark
# patchwork, a pin through its forearm. Split at both shoulders where the arms were sewn on.
# ---------------------------------------------------------------------------------------------------------------------
ARM_LEFT = [
    ("upper-l",          "arm-left", (0.230, 0.520, -0.085), (0.452, 0.660, 0.085), BURLAP),
    ("fore-l",           "arm-left", (0.432, 0.498, -0.106), (0.642, 0.682, 0.106), BURLAP),
    ("hand-l",           "arm-left", (0.622, 0.506, -0.116), (0.722, 0.676, 0.112), BURLAP_SHADOW),
    ("finger-l1",        "arm-left", (0.712, 0.620, -0.092), (0.772, 0.666, -0.040), BURLAP_SHADOW),
    ("finger-l2",        "arm-left", (0.714, 0.568, -0.030), (0.786, 0.614, 0.020), BURLAP_SHADOW),
    ("finger-l3",        "arm-left", (0.710, 0.518, 0.032), (0.762, 0.562, 0.080), BURLAP_SHADOW),
    ("thumb-l",          "arm-left", (0.650, 0.664, -0.128), (0.702, 0.708, -0.082), BURLAP_SHADOW),
    ("wrist-twine-l",    "arm-left", (0.608, 0.492, -0.120), (0.632, 0.690, 0.120), TWINE),
]
ARM_RIGHT = [
    ("upper-r",          "arm-right", (-0.452, 0.522, -0.083), (-0.230, 0.658, 0.083), FABRIC_DARK),
    ("fore-r",           "arm-right", (-0.646, 0.496, -0.108), (-0.430, 0.684, 0.108), BURLAP),
    ("hand-r",           "arm-right", (-0.726, 0.504, -0.114), (-0.624, 0.678, 0.114), BURLAP_SHADOW),
    ("finger-r1",        "arm-right", (-0.782, 0.616, -0.086), (-0.716, 0.664, -0.036), BURLAP_SHADOW),
    ("finger-r2",        "arm-right", (-0.764, 0.566, -0.024), (-0.716, 0.610, 0.024), BURLAP_SHADOW),
    ("finger-r3",        "arm-right", (-0.776, 0.514, 0.036), (-0.712, 0.560, 0.084), BURLAP_SHADOW),
    ("thumb-r",          "arm-right", (-0.706, 0.664, -0.126), (-0.652, 0.710, -0.078), BURLAP_SHADOW),
    ("wrist-twine-r",    "arm-right", (-0.634, 0.490, -0.122), (-0.610, 0.692, 0.122), TWINE),
]

# ---------------------------------------------------------------------------------------------------------------------
# HEAD. Small for the body and sunk low and forward between the shoulders: a lumpy sack, a jaw pushed forward under a wide
# stitched grin of light, a big purple button for its right eye and an X stitched over a glowing eye for its left. Gathered and
# tied at the crown (where its string will hang it from), fraying into a tuft.
# ---------------------------------------------------------------------------------------------------------------------
HEAD = [
    ("head",             "head", (-0.175, 0.662, -0.215), (0.175, 0.930, 0.110), BURLAP_LIGHT),
    ("head-top",         "head", (-0.140, 0.908, -0.180), (0.140, 0.962, 0.076), BURLAP_LIGHT),
    ("cheek-l",          "head", (0.118, 0.690, -0.204), (0.196, 0.802, -0.030), BURLAP_LIGHT),
    ("cheek-r",          "head", (-0.190, 0.700, -0.194), (-0.114, 0.792, -0.040), BURLAP_LIGHT),
    ("jaw",              "head", (-0.152, 0.646, -0.228), (0.152, 0.722, 0.050), BURLAP_LIGHT),
    ("crown-gather",     "head", (-0.066, 0.956, -0.092), (0.066, 0.996, 0.020), BURLAP_LIGHT),
    ("crown-tie",        "head", (-0.074, 0.968, -0.100), (0.074, 0.984, 0.028), TWINE),
    ("crown-loop",       "head", (-0.010, 0.994, -0.044), (0.010, 1.030, -0.024), TWINE),
]

# ---------------------------------------------------------------------------------------------------------------------
# ORIENTED PARTS: (name, bone, centre, size, (rx, ry, rz) degrees, slot). Rotation Z, then Y, then X, in the authored axes.
# ---------------------------------------------------------------------------------------------------------------------
OBOX = [
    # The X over its left eye, and the eye glowing under it.
    ("eye-x-a",          "head", (0.074, 0.820, -0.218), (0.086, 0.020, 0.008), (0, 0, 42), INK),
    ("eye-x-b",          "head", (0.074, 0.820, -0.218), (0.082, 0.020, 0.008), (0, 0, -40), INK),
    ("eye-x-slit",       "head", (0.074, 0.820, -0.2162), (0.064, 0.024, 0.004), (0, 0, 0), GLOW),
    ("eye-x-slit-core",  "head", (0.074, 0.820, -0.2174), (0.042, 0.010, 0.004), (0, 0, 0), GLOW_CORE),
    ("button-thread",    "head", (-0.074, 0.818, -0.2330), (0.030, 0.005, 0.003), (0, 0, -18), INK),
    # The purple patch on its belly, the crimson one on its hump, the grey scrap.
    ("belly-patch",      "torso", (-0.120, 0.400, -0.2175), (0.090, 0.076, 0.006), (0, 0, 8), PATCH),
    ("hump-patch",       "torso", (0.090, 0.600, 0.2185), (0.090, 0.070, 0.008), (0, 0, -9), CRIMSON),
    # The knot ends: neck, waist, ankle.
    ("neck-end-1",       "torso", (-0.084, 0.594, -0.150), (0.012, 0.050, 0.010), (0, 0, -14), TWINE),
    ("neck-end-2",       "torso", (-0.064, 0.598, -0.148), (0.012, 0.040, 0.010), (0, 0, 18), TWINE),
    ("waist-end-1",      "torso", (0.132, 0.272, -0.236), (0.012, 0.050, 0.010), (0, 0, 12), TWINE),
    ("waist-end-2",      "torso", (0.150, 0.276, -0.234), (0.012, 0.042, 0.010), (0, 0, -16), TWINE),
    ("ankle-end-1",      "leg-right", (-0.230, 0.040, -0.016), (0.012, 0.046, 0.012), (0, 0, -14), TWINE),
    ("ankle-end-2",      "leg-right", (-0.224, 0.044, 0.004), (0.012, 0.036, 0.012), (0, 0, 20), TWINE),
    # Two black feathers out of the pouch.
    ("feather-1",        "torso", (0.170, 0.326, -0.240), (0.012, 0.070, 0.004), (0, 0, 14), FABRIC_DARK),
    ("feather-2",        "torso", (0.196, 0.318, -0.242), (0.011, 0.058, 0.004), (0, 0, -20), FABRIC_DARK),
    # Twine wound round the forearms, each its own lean.
    ("wrap-l1",          "arm-left", (0.500, 0.590, 0.000), (0.014, 0.196, 0.222), (0, 0, 20), TWINE),
    ("wrap-l2",          "arm-left", (0.556, 0.590, 0.000), (0.013, 0.194, 0.220), (0, 0, -16), TWINE),
    ("wrap-r1",          "arm-right", (-0.520, 0.590, 0.000), (0.014, 0.196, 0.222), (0, 0, -22), TWINE),
    # The crown's frayed tuft, and the light spilling out of it.
    ("crown-glow",       "head", (0.000, 0.998, -0.036), (0.090, 0.008, 0.090), (0, 0, 0), GLOW),
]

# Round parts: (name, bone, centre, radius, depth, sides, slot). The button eye: light leaking round it, an ink rim, the
# button, two holes.
DISCS = [
    ("button-glow",      "head", (-0.074, 0.818, -0.2175), 0.062, 0.003, 16, GLOW),
    ("button-rim",       "head", (-0.074, 0.818, -0.219), 0.054, 0.010, 14, INK),
    ("button",           "head", (-0.074, 0.818, -0.2255), 0.045, 0.011, 14, PATCH),
    ("button-hole-a",    "head", (-0.086, 0.822, -0.2315), 0.009, 0.004, 8, INK),
    ("button-hole-b",    "head", (-0.062, 0.814, -0.2315), 0.009, 0.004, 8, INK),
]

# The frayed tuft at the crown: (name, upper end, lower end, width, depth, slot).
TUFT = [
    ("tuft-1",   (-0.076, 1.066, -0.060), (-0.036, 0.994, -0.040), 0.020, 0.016, BURLAP_LIGHT),
    ("tuft-2",   (-0.022, 1.086, -0.080), (-0.012, 0.994, -0.050), 0.016, 0.014, STUFFING),
    ("tuft-3",   (0.018, 1.094, -0.030), (0.010, 0.994, -0.030), 0.022, 0.018, BURLAP_LIGHT),
    ("tuft-4",   (0.066, 1.070, -0.066), (0.034, 0.994, -0.042), 0.018, 0.014, STUFFING),
    ("tuft-5",   (0.074, 1.058, 0.010), (0.040, 0.994, -0.004), 0.020, 0.016, BURLAP_LIGHT),
    ("tuft-6",   (-0.060, 1.060, 0.018), (-0.030, 0.994, -0.004), 0.016, 0.014, STUFFING),
]

# The grin: a wide slit of light across the jaw, higher at its left end, sewn shut by six dark stitches.
GRIN = [(-0.136, 0.708), (-0.100, 0.686), (-0.050, 0.674), (0.004, 0.672), (0.058, 0.678), (0.106, 0.692), (0.140, 0.716)]
GRIN_STITCHES = [-0.116, -0.074, -0.028, 0.022, 0.066, 0.110]
GRIN_Z = -0.2300

# Pins: (name, bone, where it enters, the direction it leaves in, length, head slot). Long ones, for a big doll.
PINS = [
    ("pin-temple-r",     "head", (-0.160, 0.880, -0.080), (-0.80, 0.52, -0.28), 0.200, PIN_HEAD),
    ("pin-head-back",    "head", (0.080, 0.860, 0.100), (0.30, 0.40, 0.87), 0.190, GOLD),
    ("pin-hump",         "torso", (-0.090, 0.660, 0.180), (-0.25, 0.70, 0.67), 0.220, PIN_MAGENTA),
    ("pin-chest",        "torso", (0.040, 0.560, -0.176), (0.30, 0.25, -0.92), 0.180, CRIMSON),
    ("pin-fore-r",       "arm-right", (-0.540, 0.684, -0.020), (-0.15, 0.95, -0.25), 0.170, PIN_HEAD),
    ("pin-belly",        "torso", (-0.140, 0.300, -0.214), (-0.35, -0.10, -0.93), 0.150, GOLD),
]
PIN_SHAFT_THICK = 0.012
PIN_HEAD_SIZE = 0.042

# ---------------------------------------------------------------------------------------------------------------------
# SPLIT OPEN, STITCHED SHUT. A point is (across, up) on the named face at the named depth: front and back faces take (x, y),
# the left and right sides (z, y).
# ---------------------------------------------------------------------------------------------------------------------
CRACKS = [
    # Down the front along the dark panel's seam, from the collar to the belly.
    ("crack-chest",  "torso", "front", -0.1795, [(0.040, 0.622), (0.022, 0.590), (0.034, 0.556), (0.016, 0.520), (0.030, 0.484),
                                                 (0.018, 0.450)], [0.024, 0.020, 0.026, 0.021, 0.024]),
    ("crack-belly",  "torso", "front", -0.2165, [(0.020, 0.466), (0.004, 0.430), (0.018, 0.394), (-0.002, 0.356), (0.012, 0.318),
                                                 (-0.006, 0.278), (0.006, 0.236)], [0.022, 0.026, 0.021, 0.025, 0.020, 0.022]),
    # Down the spine and over the hump.
    ("crack-spine",  "torso", "back", 0.2165, [(0.006, 0.676), (-0.010, 0.640), (0.004, 0.602), (-0.008, 0.566), (0.006, 0.528)],
     [0.018, 0.021, 0.017, 0.020]),
    ("crack-back",   "torso", "back", 0.1215, [(0.004, 0.468), (-0.012, 0.426), (0.006, 0.388), (-0.006, 0.346), (0.008, 0.300)],
     [0.016, 0.019, 0.015, 0.018]),
    # Where the arms were sewn on.
    ("crack-shoulder-l", "arm-left", "front", -0.0865, [(0.240, 0.654), (0.236, 0.610), (0.244, 0.566), (0.238, 0.526)],
     [0.016, 0.018, 0.015]),
    ("crack-shoulder-r", "arm-right", "front", -0.0845, [(-0.240, 0.650), (-0.244, 0.598), (-0.236, 0.530)], [0.017, 0.016]),
    # Down the right leg's front, and along its left side (the side seam).
    ("crack-leg-r",  "leg-right", "front", -0.1015, [(-0.150, 0.228), (-0.130, 0.190), (-0.140, 0.150), (-0.118, 0.112)],
     [0.016, 0.019, 0.015]),
    ("crack-side-r", "torso", "right", -0.2365, [(0.060, 0.450), (0.040, 0.410), (0.056, 0.370), (0.034, 0.330), (0.050, 0.292)],
     [0.016, 0.019, 0.015, 0.018]),
    # The head: sewn from two halves, split over the top and down the back.
    ("crack-head",   "head", "back", 0.1115, [(0.004, 0.908), (-0.010, 0.860), (0.006, 0.812), (-0.008, 0.762), (0.004, 0.714)],
     [0.016, 0.018, 0.015, 0.017]),
]

# The big X stitches over the front split: (name, bone, face, depth, centre, length, width, the two angles). The top one is
# the cutscene's last stitch.
STRAPS = [
    ("strap-1", "torso", "front", -0.1835, (0.030, 0.600), 0.076, 0.014, 40, -44),
    ("strap-2", "torso", "front", -0.1835, (0.026, 0.540), 0.070, 0.013, 36, -48),
    ("strap-3", "torso", "front", -0.1835, (0.024, 0.476), 0.066, 0.013, 46, -40),
    ("strap-4", "torso", "front", -0.2205, (0.010, 0.414), 0.074, 0.014, 44, -38),
    ("strap-5", "torso", "front", -0.2205, (0.006, 0.340), 0.068, 0.013, 38, -46),
    ("strap-6", "torso", "front", -0.2205, (0.002, 0.270), 0.062, 0.012, 42, -42),
]

# Dark stitches across the other splits: (name, bone, face, depth, centre, angle, length).
STAPLES = [
    ("st-spine-1", "torso", "back", 0.2205, (0.000, 0.656), 4, 0.040),
    ("st-spine-2", "torso", "back", 0.2205, (-0.004, 0.604), -6, 0.036),
    ("st-spine-3", "torso", "back", 0.2205, (0.000, 0.552), 3, 0.040),
    ("st-back-1",  "torso", "back", 0.1255, (-0.004, 0.446), -4, 0.036),
    ("st-back-2",  "torso", "back", 0.1255, (0.000, 0.390), 6, 0.038),
    ("st-back-3",  "torso", "back", 0.1255, (0.002, 0.330), -3, 0.034),
    ("st-sh-l-1",  "arm-left", "front", -0.0905, (0.238, 0.630), 0, 0.034),
    ("st-sh-l-2",  "arm-left", "front", -0.0905, (0.240, 0.584), 8, 0.032),
    ("st-sh-l-3",  "arm-left", "front", -0.0905, (0.241, 0.544), -6, 0.034),
    ("st-sh-r-1",  "arm-right", "front", -0.0885, (-0.242, 0.622), -5, 0.034),
    ("st-sh-r-2",  "arm-right", "front", -0.0885, (-0.240, 0.560), 7, 0.032),
    ("st-leg-r-1a", "leg-right", "front", -0.1055, (-0.140, 0.206), 45, 0.034),
    ("st-leg-r-1b", "leg-right", "front", -0.1055, (-0.140, 0.206), -45, 0.034),
    ("st-leg-r-2a", "leg-right", "front", -0.1055, (-0.128, 0.132), 40, 0.030),
    ("st-leg-r-2b", "leg-right", "front", -0.1055, (-0.128, 0.132), -48, 0.030),
    ("st-side-r-1", "torso", "right", -0.2405, (0.050, 0.430), 2, 0.034),
    ("st-side-r-2", "torso", "right", -0.2405, (0.046, 0.352), -4, 0.032),
    ("st-head-1",  "head", "back", 0.1155, (-0.002, 0.884), 4, 0.034),
    ("st-head-2",  "head", "back", 0.1155, (-0.002, 0.812), -6, 0.032),
    ("st-head-3",  "head", "back", 0.1155, (0.000, 0.740), 3, 0.034),
]

# Crosshatch on the patches, and a coarse weave over the big belly, so the cloth reads as cloth.
HATCH = [
    ("h-knee-1", "leg-left", "front", -0.1145, (0.098, 0.153), 45, 0.070, INK),
    ("h-knee-2", "leg-left", "front", -0.1145, (0.118, 0.153), 45, 0.096, INK),
    ("h-knee-3", "leg-left", "front", -0.1145, (0.138, 0.153), 45, 0.070, INK),
    ("h-knee-4", "leg-left", "front", -0.1145, (0.098, 0.153), -45, 0.070, INK),
    ("h-knee-5", "leg-left", "front", -0.1145, (0.118, 0.153), -45, 0.096, INK),
    ("h-knee-6", "leg-left", "front", -0.1145, (0.138, 0.153), -45, 0.070, INK),
    ("h-belly-1", "torso", "front", -0.2215, (-0.140, 0.400), 45, 0.070, TWINE),
    ("h-belly-2", "torso", "front", -0.2215, (-0.110, 0.402), 45, 0.066, TWINE),
    ("h-belly-3", "torso", "front", -0.2215, (-0.138, 0.398), -45, 0.066, TWINE),
    ("h-belly-4", "torso", "front", -0.2215, (-0.108, 0.401), -45, 0.070, TWINE),
    ("h-hump-1", "torso", "back", 0.2235, (0.070, 0.600), 45, 0.070, INK),
    ("h-hump-2", "torso", "back", 0.2235, (0.100, 0.598), 45, 0.066, INK),
    ("h-hump-3", "torso", "back", 0.2235, (0.072, 0.602), -45, 0.066, INK),
    ("h-hump-4", "torso", "back", 0.2235, (0.102, 0.600), -45, 0.070, INK),
    ("h-panel-1", "torso", "front", -0.1875, (0.110, 0.560), 45, 0.150, BURLAP_SHADOW),
    ("h-panel-2", "torso", "front", -0.1875, (0.180, 0.540), 45, 0.150, BURLAP_SHADOW),
    ("h-panel-3", "torso", "front", -0.1875, (0.120, 0.520), -45, 0.150, BURLAP_SHADOW),
    ("h-panel-4", "torso", "front", -0.1875, (0.190, 0.548), -45, 0.140, BURLAP_SHADOW),
    ("h-scrap-1", "torso", "front", -0.2255, (-0.150, 0.282), 45, 0.090, BURLAP_SHADOW),
    ("h-scrap-2", "torso", "front", -0.2255, (-0.130, 0.280), -45, 0.090, BURLAP_SHADOW),
]

FACE_NORMALS = {"front": (0.0, 0.0, -1.0), "back": (0.0, 0.0, 1.0), "left": (1.0, 0.0, 0.0), "right": (-1.0, 0.0, 0.0)}
STAPLE_WIDTH = 0.009
HATCH_WIDTH = 0.0045


# ---------------------------------------------------------------------------------------------------------------------
# GEOMETRY
# ---------------------------------------------------------------------------------------------------------------------
def _rot(rx, ry, rz):
    """A 3x3 rotation, Z then Y then X, degrees."""
    ax, ay, az = (math.radians(a) for a in (rx, ry, rz))
    cx, sx, cy, sy, cz, sz = math.cos(ax), math.sin(ax), math.cos(ay), math.sin(ay), math.cos(az), math.sin(az)
    rxm = ((1, 0, 0), (0, cx, -sx), (0, sx, cx))
    rym = ((cy, 0, sy), (0, 1, 0), (-sy, 0, cy))
    rzm = ((cz, -sz, 0), (sz, cz, 0), (0, 0, 1))
    return _mul(rxm, _mul(rym, rzm))


def _mul(a, b):
    return tuple(tuple(sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)) for i in range(3))


def _apply(m, v):
    return tuple(sum(m[i][k] * v[k] for k in range(3)) for i in range(3))


def _towards(direction):
    """The rotation carrying +Y onto `direction` (Rodrigues)."""
    d = V._unit(direction)
    y = (0.0, 1.0, 0.0)
    axis = V._cross(y, d)
    s = math.sqrt(V._dot(axis, axis))
    c = V._dot(y, d)
    if s < 1e-6:
        return ((1, 0, 0), (0, 1, 0), (0, 0, 1)) if c > 0 else ((1, 0, 0), (0, -1, 0), (0, 0, -1))
    k = tuple(a / s for a in axis)
    kx = ((0, -k[2], k[1]), (k[2], 0, -k[0]), (-k[1], k[0], 0))
    kx2 = _mul(kx, kx)
    return tuple(tuple((1.0 if i == j else 0.0) + s * kx[i][j] + (1 - c) * kx2[i][j] for j in range(3)) for i in range(3))


def _outward(pts, normal):
    """The polygon wound so its right-hand normal is `normal` (the shared builder's convention)."""
    a = tuple(pts[1][i] - pts[0][i] for i in range(3))
    b = tuple(pts[2][i] - pts[1][i] for i in range(3))
    return pts if V._dot(V._cross(a, b), normal) > 0 else list(reversed(pts))


def _place(centre, m, local):
    r = _apply(m, local)
    return (centre[0] + r[0], centre[1] + r[1], centre[2] + r[2])


def _obox_polygons(centre, size, m):
    """Six outward quads of an oriented box, in the authored axes."""
    h = [size[i] * 0.5 for i in range(3)]
    for normal, corners in V.FACES:
        pts = [_place(centre, m, ((cx * 2 - 1) * h[0], (cy * 2 - 1) * h[1], (cz * 2 - 1) * h[2])) for cx, cy, cz in corners]
        n = _apply(m, normal)
        yield n, _outward(pts, n)


def _disc_polygons(centre, radius, depth, sides, m):
    """A round slab facing -Z: two caps and its rim, in the authored axes."""
    ring = [(radius * math.cos(2 * math.pi * k / sides), radius * math.sin(2 * math.pi * k / sides)) for k in range(sides)]
    for z, nz in ((-depth * 0.5, -1.0), (depth * 0.5, 1.0)):
        n = _apply(m, (0.0, 0.0, nz))
        yield n, _outward([_place(centre, m, (x, y, z)) for x, y in ring], n)
    for k in range(sides):
        (x0, y0), (x1, y1) = ring[k], ring[(k + 1) % sides]
        mid = math.atan2((y0 + y1) * 0.5, (x0 + x1) * 0.5)
        n = _apply(m, (math.cos(mid), math.sin(mid), 0.0))
        pts = [_place(centre, m, p) for p in ((x0, y0, -depth * 0.5), (x1, y1, -depth * 0.5),
                                              (x1, y1, depth * 0.5), (x0, y0, depth * 0.5))]
        yield n, _outward(pts, n)


def assemble(boxes, rows):
    """The chamfered boxes through the shared builder, then the oriented parts, flat-shaded, in the output axes."""
    pos, nrm, uv, joints, weights, idx = V.build_mesh(boxes)
    for name, bone, centre, size, m, slot, kind in rows:
        j = V.BONE[bone]
        u, v = V.cell_uv(slot)
        polygons = _obox_polygons(centre, size, m) if kind == "box" else _disc_polygons(centre, size[0], size[1], size[2], m)
        for normal, pts in polygons:
            # The builder authors the face on -Z and writes it on +Z: mirror Z, which also reverses the winding.
            pts = [(p[0], p[1], -p[2]) for p in reversed(pts)]
            normal = (normal[0], normal[1], -normal[2])
            first = len(pos)
            for p in pts:
                pos.append(p); nrm.append(normal); uv.append((u, v))
                joints.append((j, 0, 0, 0)); weights.append((1.0, 0.0, 0.0, 0.0))
            for k in range(1, len(pts) - 1):
                idx += [first, first + k, first + k + 1]
    return pos, nrm, uv, joints, weights, idx




def _face_row(name, bone, face, depth, centre, angle, length, width, thick, slot):
    """One flat stroke on a face. Front and back: centred at (x, y), `depth` along Z. Left and right: centred at (z, y), `depth`
    along X. `angle` turns it in the face (0 runs across: along X on the front and back, along Z on the sides)."""
    n = FACE_NORMALS[face]
    a = math.radians(angle)
    if face in ("front", "back"):
        along = (math.cos(a), math.sin(a), 0.0)
        at = (centre[0], centre[1], depth)
    else:
        along = (0.0, math.sin(a), math.cos(a))
        at = (depth, centre[1], centre[0])
    across = V._cross(n, along)
    m = tuple(tuple((along, across, n)[k][i] for k in range(3)) for i in range(3))
    return (name, bone, at, (length, width, thick), m, slot, "box")


def _grin_at(x):
    """The grin's height and slope at x, on the polyline."""
    for k in range(len(GRIN) - 1):
        (x0, y0), (x1, y1) = GRIN[k], GRIN[k + 1]
        if x0 <= x <= x1:
            t = (x - x0) / (x1 - x0)
            slope = (y1 - y0) / (x1 - x0)
            return y0 + (y1 - y0) * t, slope
    raise SystemExit(f"grin stitch at {x} is off the grin")


def oriented_rows():
    """Every tilted, round and stroked part as (name, bone, centre, size, matrix, slot, kind)."""
    rows = [(n, b, c, s, _rot(*r), slot, "box") for n, b, c, s, r, slot in OBOX]

    for name, top, bottom, width, depth, slot in TUFT:
        up = tuple(top[i] - bottom[i] for i in range(3))
        length = math.sqrt(V._dot(up, up)) + 0.006
        centre = tuple((top[i] + bottom[i]) * 0.5 for i in range(3))
        rows.append((name, "head", centre, (width, length, depth), _towards(up), slot, "box"))

    for name, bone, centre, radius, depth, sides, slot in DISCS:
        rows.append((name, bone, centre, (radius, depth, sides), _rot(0, 0, 0), slot, "disc"))

    for k in range(len(GRIN) - 1):
        (x0, y0), (x1, y1) = GRIN[k], GRIN[k + 1]
        length = math.hypot(x1 - x0, y1 - y0) + 0.008
        angle = math.degrees(math.atan2(y1 - y0, x1 - x0))
        centre = ((x0 + x1) / 2, (y0 + y1) / 2)
        rows.append((f"grin-{k}", "head", (centre[0], centre[1], GRIN_Z), (length, 0.024, 0.004), _rot(0, 0, angle), GLOW, "box"))
        rows.append((f"grin-{k}-core", "head", (centre[0], centre[1], GRIN_Z - 0.0012), (length - 0.004, 0.009, 0.004),
                     _rot(0, 0, angle), GLOW_CORE, "box"))
    for k, x in enumerate(GRIN_STITCHES):
        y, slope = _grin_at(x)
        rows.append((f"grin-stitch-{k}", "head", (x, y, GRIN_Z - 0.0040), (0.013, 0.058, 0.005),
                     _rot(0, 0, math.degrees(math.atan(slope))), INK, "box"))

    for name, bone, face, depth, points, widths in CRACKS:
        outward = -0.0012 if face in ("front", "right") else 0.0012
        for k in range(len(points) - 1):
            (a0, b0), (a1, b1) = points[k], points[k + 1]
            length = math.hypot(a1 - a0, b1 - b0) + 0.006
            angle = math.degrees(math.atan2(b1 - b0, a1 - a0))
            mid = ((a0 + a1) / 2, (b0 + b1) / 2)
            rows.append(_face_row(f"{name}-{k}", bone, face, depth, mid, angle, length, widths[k], 0.004, GLOW))
            rows.append(_face_row(f"{name}-{k}-core", bone, face, depth + outward, mid, angle, length - 0.004,
                                  widths[k] * 0.42, 0.004, GLOW_CORE))
    for name, bone, face, depth, centre, length, width, a1, a2 in STRAPS:
        rows.append(_face_row(name + "-a", bone, face, depth, centre, a1, length, width, 0.006, INK))
        rows.append(_face_row(name + "-b", bone, face, depth - 0.001 if face == "front" else depth + 0.001, centre, a2,
                              length * 0.96, width, 0.006, INK))
    for name, bone, face, depth, centre, angle, length in STAPLES:
        rows.append(_face_row(name, bone, face, depth, centre, angle, length, STAPLE_WIDTH, 0.005, INK))
    for name, bone, face, depth, centre, angle, length, slot in HATCH:
        rows.append(_face_row(name, bone, face, depth, centre, angle, length, HATCH_WIDTH, 0.003, slot))

    for name, bone, base, direction, length, head_slot in PINS:
        d = V._unit(direction)
        m = _towards(d)
        centre = tuple(base[i] + d[i] * length * 0.5 for i in range(3))
        rows.append((name, bone, centre, (PIN_SHAFT_THICK, length, PIN_SHAFT_THICK), m, PIN_SHAFT, "box"))
        tip = tuple(base[i] + d[i] * length for i in range(3))
        rows.append((name + "-head", bone, tip, (PIN_HEAD_SIZE,) * 3, m, head_slot, "box"))
        rows.append((name + "-head-45", bone, tip, (PIN_HEAD_SIZE * 0.86,) * 3, _mul(m, _rot(0, 45, 45)), head_slot, "box"))
    return rows


BODY_BOXES = LEG_LEFT + LEG_RIGHT + TORSO + ARM_LEFT + ARM_RIGHT
HEAD_BOXES = HEAD


def retarget(gltf):
    """The rig's bones moved onto the doll's own skeleton; returns each moved node's translation delta (for the clips)."""
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
    for skin in gltf["skins"]:
        rows = []
        for joint in skin["joints"]:
            world = SKELETON[gltf["nodes"][joint].get("name")]
            rows.append((1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, -world[0], -world[1], -world[2], 1.0))
        skin["_rows"] = rows


def verify(body, head, glow, rows):
    every = body[0] + head[0] + glow[0]
    lo = [min(v[a] for v in every) for a in range(3)]
    hi = [max(v[a] for v in every) for a in range(3)]
    height = hi[1] - lo[1]
    print(f"boxes: body={len(BODY_BOXES)} head={len(HEAD_BOXES)} oriented={len(rows)}")
    print(f"tris: body={len(body[5]) // 3} head={len(head[5]) // 3} glow={len(glow[5]) // 3}")
    print(f"bounds min={[round(v, 4) for v in lo]} max={[round(v, 4) for v in hi]}  height={height:.4f}")
    if not (MIN_HEIGHT <= height <= MAX_HEIGHT):
        raise SystemExit(f"HEIGHT {height:.4f} outside {MIN_HEIGHT}..{MAX_HEIGHT}; nothing written.")
    if abs(lo[1]) > 0.001:
        raise SystemExit(f"feet are at y={lo[1]:.4f}, not 0.")
    for name, bone, box_lo, box_hi, slot in BODY_BOXES + HEAD_BOXES:
        if slot not in PALETTE:
            raise SystemExit(f"box '{name}' uses palette slot {slot}, which is not set.")
        origin = SKELETON[bone]
        centre = [(box_lo[i] + box_hi[i]) * 0.5 for i in range(3)]
        if math.dist(centre, origin) > 0.75:
            raise SystemExit(f"box '{name}' is far from the {bone} bone: almost certainly on the wrong bone.")
    for name, bone, centre, size, m, slot, kind in rows:
        if math.dist(centre, SKELETON[bone]) > 0.75:
            raise SystemExit(f"part '{name}' is far from the {bone} bone: almost certainly on the wrong bone.")
    r, g, b = (int(PALETTE[INK][i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    if 0.2126 * r + 0.7152 * g + 0.0722 * b > V.MAX_FACE_LUMINANCE:
        raise SystemExit("slot 8 must stay ink.")


def main():
    if not os.path.exists(V.BASE):
        raise SystemExit(f"base rig not found: {V.BASE}")
    gltf, buffer = V.read_glb(V.BASE)
    deltas = retarget(gltf)
    bind_matrices(gltf)

    rows = oriented_rows()
    lit = (GLOW, GLOW_CORE)
    glow_rows = [r for r in rows if r[5] in lit]
    body = assemble(BODY_BOXES, [r for r in rows if r[1] != "head" and r[5] not in lit])
    head = assemble(HEAD_BOXES, [r for r in rows if r[1] == "head" and r[5] not in lit])
    glow = assemble([], glow_rows)
    verify(body, head, glow, rows)

    blob, new_views, new_accessors, remap = bytearray(), [], [], {}

    def align():
        while len(blob) % 4:
            blob.append(0)

    def keep(old_index):
        if old_index in remap:
            return remap[old_index]
        acc = dict(gltf["accessors"][old_index])
        data = V.accessor_bytes(gltf, buffer, old_index)
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
        acc = {"bufferView": len(new_views), "componentType": component, "count": len(values), "type": kind}
        if minmax:
            n = len(values[0])
            acc["min"] = [min(v[a] for v in values) for a in range(n)]
            acc["max"] = [max(v[a] for v in values) for a in range(n)]
        new_views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        new_accessors.append(acc)
        return len(new_accessors) - 1

    for skin in gltf["skins"]:
        skin["inverseBindMatrices"] = add(skin.pop("_rows"), "f", "MAT4", 5126)

    for anim in gltf["animations"]:
        for channel in anim["channels"]:
            sampler = anim["samplers"][channel["sampler"]]
            sampler["input"] = keep(sampler["input"])
            delta = deltas.get(channel["target"]["node"])
            if channel["target"]["path"] != "translation" or delta is None or delta == (0.0, 0.0, 0.0):
                sampler["output"] = keep(sampler["output"])
                continue
            values = V.read_accessor(gltf, buffer, sampler["output"])
            sampler["output"] = add([tuple(v[a] + delta[a] for a in range(3)) for v in values], "f", "VEC3", 5126)

    # ⚠️ THE SOUL LIGHT IS A THIRD SKINNED MESH ON THE BODY'S SKIN, `glow-mesh`, so the game paints it unlit
    # (`PhaisterDollArt.ApplyGlow`) while the toon paint keeps the cloth. Its joints index the same seven bones.
    gltf["meshes"].append({"name": "glow-mesh", "primitives": []})
    gltf["nodes"].append({"name": "glow-mesh", "mesh": len(gltf["meshes"]) - 1, "skin": 0})
    gltf["nodes"][0].setdefault("children", []).append(len(gltf["nodes"]) - 1)

    for mesh, built in ((gltf["meshes"][0], body), (gltf["meshes"][1], head), (gltf["meshes"][-1], glow)):
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso voodoo doll builder"}
    stem = os.path.splitext(os.path.basename(OUT))[0]
    gltf["nodes"][0]["name"] = stem
    gltf["scenes"][0]["name"] = stem

    V.write_glb(OUT, gltf, bytes(blob))
    os.makedirs(os.path.dirname(PALETTE_OUT), exist_ok=True)
    with open(PALETTE_OUT, "w", encoding="utf-8", newline="\n") as handle:
        json.dump({"slots": [PALETTE[s] for s in range(16)]}, handle, indent=2)
        handle.write("\n")
    print(f"wrote {PALETTE_OUT}")


if __name__ == "__main__":
    sys.exit(main())
