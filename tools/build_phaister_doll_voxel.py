"""Builds phaister-doll.glb: Phaister's voodoo doll at player size (HERO-10 v3, the VOODOO DOLL ultimate).

    python tools/build_phaister_doll_voxel.py

Owner, 2026-09-27: *"create a new model for the voodoo i guess"*, for his ultimate *"The voodoo doll becomes a sentient
being that assists you in attacking or defending for the rest of the round"*; then, on v3, *"this shit suckls why does it
have her hair"*, *"and her hat"*, *"make it look like its own vooodoo wtf"*. So it is ITS OWN voodoo doll, not a small
Phaister: a stuffed burlap sack, gathered and tied off at the crown with a frayed tuft, a purple button sewn on for one eye
and an ink X for the other, a stitched grin, pins with coloured heads stuck in it all over (one through its heart patch),
twine bound round its neck, waist, wrists and one ankle, patches and a tuft of stuffing out of a torn hem. Her colours live
only in the pin heads and the patches. The design and its history: `ArtSource/phaister/doll-20260927/design-brief.md`.

Built the cast's way (`docs/CHARACTER_MODEL_METHOD.md`): the base CC0 rig, Phaister's own skeleton (so her doll moves on
her proportions and every clip, gait, animator and bot drives it like a player body), chamfered boxes rigidly skinned to
one bone each. Every part below is typed by hand with its own numbers, and the two sides are drawn separately; nothing is
stamped round a loop. Tilted parts (the X eye, the grin, the pins, the hat worn askew) are oriented boxes, `OBOX` rows.

It imports `build_phaister_voxel` for its GEOMETRY helpers only (the rig, the chamfer, the glb reader and writer). That
builder is never run from here and never edited: re-running it loses Phaister's baked clips (`docs/TODO.md` HERO-10).

Axes, as in every builder here: +X is the doll's LEFT, the face is on -Z, feet on y = 0, metres in the rig's units (the
game draws people at `PersonScale` 2.38).
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
# PALETTE. Her colours on a burlap body. Slot 8 is the face and stays ink (`Toon.shader`).
# ---------------------------------------------------------------------------------------------------------------------
BURLAP = 0          # the sack cloth of the body and limbs
BURLAP_SHADOW = 1   # soles, the gathered hem, the chin gather
BURLAP_LIGHT = 2    # the head, a shade lighter so the face reads first
GOLD = 3            # her one metal: pin heads
TWINE = 4           # the ties at the neck, wrists and ankle
STUFFING = 5        # the tuft out of the shoulder seam
PIN_MAGENTA = 6     # her magenta: pin heads
FABRIC_DARK = 7     # a darker cloth: the patch sewn on its head
INK = 8             # eyes, mouth, every stitch
PATCH = 9           # her royal purple: the heart patch, the button eye
CRIMSON = 10        # her crimson: the knee patch, the back patch, one pin head
FEATHER = 11        # the black feathers in the gris-gris pouch
PIN_HEAD = 12       # her lilac: pin heads
PIN_SHAFT = 13      # the pins' shafts, her gold dulled
GLOW_CORE = 14      # the hot line down the middle of every split (`SoulGlow` paints this cell `_Core`)
GLOW = 15           # the soul light: every part in this slot goes to the glow mesh, painted by `SoulGlow`

# ⚠️ v2: the cloth is a paler linen than v1's a2865f, which sat beside Paete's bark and Sean's skin in the lineup as a third
# brown. Paler and a little greyer, it reads as sackcloth and stays apart from both.
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
    FEATHER:       "241c2c",
    PIN_HEAD:      "9838d8",
    PIN_SHAFT:     "b87814",
    GLOW_CORE:     "ffd9f5",
    GLOW:          "ff54e6",
}

# ---------------------------------------------------------------------------------------------------------------------
# LEGS. Stuffed sacks with rounded feet and no shoes. The left knee carries a crimson patch crossed by one stitch; the
# right ankle is tied with twine, its knot on the outside with two loose ends. Ankle seams differ side to side.
# ---------------------------------------------------------------------------------------------------------------------
LEG_LEFT = [
    ("leg-sack-l",        "leg-left", (0.018, 0.028, -0.078), (0.150, 0.184, 0.072), BURLAP),
    ("foot-l",            "leg-left", (0.012, 0.012, -0.112), (0.156, 0.062, 0.078), BURLAP),
    ("sole-l",            "leg-left", (0.014, 0.000, -0.108), (0.154, 0.014, 0.074), BURLAP_SHADOW),
    ("knee-patch-l",      "leg-left", (0.038, 0.086, -0.086), (0.120, 0.150, -0.074), CRIMSON),
    ("ankle-seam-l",      "leg-left", (0.016, 0.060, -0.082), (0.152, 0.068, 0.076), BURLAP_SHADOW),
]
LEG_RIGHT = [
    ("leg-sack-r",        "leg-right", (-0.150, 0.028, -0.072), (-0.018, 0.184, 0.074), BURLAP),
    ("foot-r",            "leg-right", (-0.156, 0.012, -0.106), (-0.012, 0.060, 0.078), BURLAP),
    ("sole-r",            "leg-right", (-0.154, 0.000, -0.102), (-0.014, 0.014, 0.074), BURLAP_SHADOW),
    ("ankle-twine-r",     "leg-right", (-0.154, 0.064, -0.080), (-0.014, 0.080, 0.078), TWINE),
    ("ankle-knot-r",      "leg-right", (-0.168, 0.060, -0.020), (-0.148, 0.086, 0.014), TWINE),
    ("seam-side-r",       "leg-right", (-0.156, 0.092, -0.004), (-0.149, 0.176, 0.004), INK),
    ("seam-side-r-t1",    "leg-right", (-0.157, 0.103, -0.016), (-0.149, 0.109, 0.016), INK),
    ("seam-side-r-t2",    "leg-right", (-0.157, 0.128, -0.014), (-0.149, 0.134, 0.018), INK),
    ("seam-side-r-t3",    "leg-right", (-0.157, 0.158, -0.017), (-0.149, 0.164, 0.013), INK),
]

# ---------------------------------------------------------------------------------------------------------------------
# TORSO. A stuffed sack with a round belly and a gathered hem, split open down the chest where the soul light shows through
# and held shut by three X straps (`CRACKS`, `STRAPS`); split again down the spine. Twine at the neck and round the waist,
# both knotted on its right. A red gris-gris pouch hangs at its left hip with two black feathers. Seams down both sides,
# drawn separately. Stuffing escapes at the back of the right hip. A crosshatched crimson patch low on the back.
# ---------------------------------------------------------------------------------------------------------------------
TORSO = [
    ("body-sack",         "torso", (-0.148, 0.168, -0.090), (0.148, 0.338, 0.086), BURLAP),
    ("belly",             "torso", (-0.124, 0.182, -0.104), (0.124, 0.250, -0.062), BURLAP),
    ("hem",               "torso", (-0.151, 0.166, -0.093), (0.151, 0.180, 0.089), BURLAP_SHADOW),
    ("neck-twine",        "torso", (-0.094, 0.326, -0.076), (0.094, 0.352, 0.074), TWINE),
    # Bound round the middle, as a manika is: the twine wraps the belly and knots on its right.
    ("waist-twine",       "torso", (-0.152, 0.226, -0.108), (0.152, 0.242, 0.090), TWINE),
    ("waist-knot",        "torso", (-0.100, 0.220, -0.118), (-0.070, 0.250, -0.100), TWINE),
    ("neck-knot",         "torso", (-0.066, 0.316, -0.092), (-0.028, 0.356, -0.072), TWINE),
    # The gris-gris pouch: a little red bag on a string from the waist twine, tied at its neck.
    ("pouch",             "torso", (0.075, 0.166, -0.137), (0.125, 0.218, -0.103), CRIMSON),
    ("pouch-neck",        "torso", (0.086, 0.216, -0.131), (0.114, 0.228, -0.109), CRIMSON),
    ("pouch-tie",         "torso", (0.084, 0.221, -0.133), (0.116, 0.228, -0.107), TWINE),
    ("pouch-string",      "torso", (0.098, 0.227, -0.124), (0.102, 0.244, -0.118), TWINE),
    ("seam-l",            "torso", (0.147, 0.188, -0.004), (0.154, 0.330, 0.004), INK),
    ("seam-l-t1",         "torso", (0.147, 0.198, -0.018), (0.155, 0.204, 0.016), INK),
    ("seam-l-t2",         "torso", (0.147, 0.229, -0.015), (0.155, 0.235, 0.019), INK),
    ("seam-l-t3",         "torso", (0.147, 0.257, -0.019), (0.155, 0.263, 0.015), INK),
    ("seam-l-t4",         "torso", (0.147, 0.296, -0.016), (0.155, 0.302, 0.018), INK),
    ("seam-l-t5",         "torso", (0.147, 0.318, -0.017), (0.155, 0.324, 0.014), INK),
    # The hem is torn at the back of the right hip and the stuffing escapes there.
    ("seam-r",            "torso", (-0.154, 0.228, -0.004), (-0.147, 0.326, 0.004), INK),
    ("seam-r-t2",         "torso", (-0.155, 0.247, -0.014), (-0.147, 0.253, 0.019), INK),
    ("seam-r-t3",         "torso", (-0.155, 0.289, -0.018), (-0.147, 0.295, 0.015), INK),
    ("stuffing-a",        "torso", (-0.150, 0.166, 0.080), (-0.116, 0.196, 0.104), STUFFING),
    ("stuffing-b",        "torso", (-0.136, 0.156, 0.088), (-0.108, 0.180, 0.112), STUFFING),
    ("stuffing-c",        "torso", (-0.160, 0.182, 0.076), (-0.134, 0.206, 0.098), STUFFING),
]

# ---------------------------------------------------------------------------------------------------------------------
# ARMS. Stubby sacks ending in mittens, tied at the wrist. The left arm has a stitched seam along its top; the right arm
# has a purple patch and a pin stuck through it (the pin is an OBOX row below).
# ---------------------------------------------------------------------------------------------------------------------
ARM_LEFT = [
    ("arm-sack-l",        "arm-left", (0.096, 0.222, -0.070), (0.300, 0.354, 0.074), BURLAP),
    ("mitten-l",          "arm-left", (0.290, 0.218, -0.066), (0.356, 0.350, 0.070), BURLAP),
    ("wrist-twine-l",     "arm-left", (0.282, 0.215, -0.075), (0.300, 0.358, 0.079), TWINE),
    ("thumb-l",           "arm-left", (0.310, 0.332, -0.084), (0.342, 0.362, -0.052), BURLAP),
    ("seam-arm-l",        "arm-left", (0.112, 0.353, -0.004), (0.280, 0.360, 0.004), INK),
    ("seam-arm-l-t1",     "arm-left", (0.130, 0.352, -0.017), (0.136, 0.361, 0.017), INK),
    ("seam-arm-l-t2",     "arm-left", (0.168, 0.352, -0.015), (0.174, 0.361, 0.019), INK),
    ("seam-arm-l-t3",     "arm-left", (0.216, 0.352, -0.018), (0.222, 0.361, 0.016), INK),
    ("seam-arm-l-t4",     "arm-left", (0.251, 0.352, -0.016), (0.257, 0.361, 0.017), INK),
]
ARM_RIGHT = [
    ("arm-sack-r",        "arm-right", (-0.300, 0.224, -0.072), (-0.096, 0.352, 0.072), BURLAP),
    ("mitten-r",          "arm-right", (-0.352, 0.220, -0.068), (-0.290, 0.348, 0.068), BURLAP),
    ("wrist-twine-r",     "arm-right", (-0.300, 0.217, -0.077), (-0.283, 0.355, 0.077), TWINE),
    ("thumb-r",           "arm-right", (-0.340, 0.330, -0.082), (-0.308, 0.360, -0.050), BURLAP),
]

# ---------------------------------------------------------------------------------------------------------------------
# HEAD. A big soft sack, a shade lighter than the body, gathered at the neck and gathered again at the crown, where it is tied
# off with twine and the burlap frays up into a tuft (`TUFT`). The face: a round purple button sewn on for its right eye, an
# X of ink for its left, a stitched grin a little higher on its left. Pins stick out of it everywhere (`PINS`).
# ⚠️⚠️ v4, OWNER ON v3: *"this shit suckls why does it have her hair hahahahaa"*, *"and her hat"*, *"make it look like its
# own vooodoo wtf"*. v1 to v3 gave it her magenta hair as yarn and a copy of her hat, which made it a small Phaister rather
# than a doll. It is its own thing now: a voodoo doll, read by the tied sack top, the stitches and the pins.
# ---------------------------------------------------------------------------------------------------------------------
HEAD = [
    ("head-sack",         "head", (-0.198, 0.352, -0.168), (0.198, 0.716, 0.170), BURLAP_LIGHT),
    ("head-crown",        "head", (-0.170, 0.700, -0.140), (0.170, 0.742, 0.142), BURLAP_LIGHT),
    ("chin-gather",       "head", (-0.150, 0.338, -0.140), (0.150, 0.366, 0.142), BURLAP_SHADOW),
    # The crown gathered into a neck and tied off, the knot on its left of the front. It tapers in two steps so the top reads
    # as cloth pulled together, not a lid with a stub on it (v4's first render).
    ("crown-taper",       "head", (-0.128, 0.734, -0.104), (0.128, 0.758, 0.114), BURLAP_LIGHT),
    ("crown-taper-2",     "head", (-0.098, 0.750, -0.082), (0.098, 0.768, 0.092), BURLAP_LIGHT),
    ("crown-gather",      "head", (-0.074, 0.736, -0.062), (0.074, 0.784, 0.070), BURLAP_LIGHT),
    ("crown-tie",         "head", (-0.082, 0.752, -0.070), (0.082, 0.772, 0.078), TWINE),
    ("crown-knot",        "head", (0.058, 0.744, -0.086), (0.088, 0.778, -0.062), TWINE),
    # The sack's back seam, down the back of the head.
    ("seam-head-back",    "head", (-0.016, 0.402, 0.168), (-0.008, 0.690, 0.176), INK),
    ("seam-head-back-t1", "head", (-0.029, 0.426, 0.168), (0.003, 0.432, 0.177), INK),
    ("seam-head-back-t2", "head", (-0.027, 0.470, 0.168), (0.005, 0.476, 0.177), INK),
    ("seam-head-back-t3", "head", (-0.030, 0.527, 0.168), (0.002, 0.533, 0.177), INK),
    ("seam-head-back-t4", "head", (-0.026, 0.588, 0.168), (0.006, 0.594, 0.177), INK),
    ("seam-head-back-t5", "head", (-0.028, 0.645, 0.168), (0.004, 0.651, 0.177), INK),
]

# The frayed tuft above the tie: (name, upper end, lower end, width, depth, slot). Burlap threads and a little stuffing,
# each splayed its own way and its own length, the way a tied-off sack frays.
TUFT = [
    ("tuft-1",    (-0.090, 0.852, -0.050), (-0.050, 0.778, -0.030), 0.022, 0.018, BURLAP_LIGHT),
    ("tuft-2",    (-0.036, 0.874, -0.080), (-0.020, 0.778, -0.040), 0.018, 0.016, STUFFING),
    ("tuft-3",    (0.006, 0.888, 0.010), (0.010, 0.778, 0.000), 0.024, 0.020, BURLAP_LIGHT),
    ("tuft-4",    (0.072, 0.862, -0.050), (0.036, 0.778, -0.020), 0.020, 0.016, STUFFING),
    ("tuft-5",    (0.100, 0.844, 0.070), (0.050, 0.778, 0.040), 0.022, 0.018, BURLAP_LIGHT),
    ("tuft-6",    (-0.072, 0.858, 0.092), (-0.036, 0.778, 0.044), 0.018, 0.016, STUFFING),
    ("tuft-7",    (0.022, 0.838, 0.102), (0.000, 0.778, 0.050), 0.016, 0.014, BURLAP_SHADOW),
]

# Round parts: (name, bone, centre, radius, depth, sides, slot). They face the viewer (-Z). The button eye is sewn on: an
# ink rim, the purple button proud of it, two holes and the thread between them.
DISCS = [
    ("button-glow",       "head", (-0.078, 0.542, -0.1675), 0.058, 0.003, 14, GLOW),
    ("button-rim",        "head", (-0.078, 0.542, -0.169), 0.050, 0.010, 12, INK),
    ("button",            "head", (-0.078, 0.542, -0.1755), 0.041, 0.011, 12, PATCH),
    ("button-hole-a",     "head", (-0.090, 0.546, -0.1815), 0.0085, 0.004, 8, INK),
    ("button-hole-b",     "head", (-0.066, 0.538, -0.1815), 0.0085, 0.004, 8, INK),
]

# ---------------------------------------------------------------------------------------------------------------------
# ORIENTED PARTS: (name, bone, centre, size, (rx, ry, rz) degrees, slot). Rotation is applied Z, then Y, then X, in the
# builder's authored axes (face on -Z). These are the parts a box cannot say: a stroke at an angle.
# ---------------------------------------------------------------------------------------------------------------------
OBOX = [
    # Left knee: one stitch across the patch.
    # Right ankle: the twine's two loose ends.
    ("ankle-end-r1",      "leg-right", (-0.171, 0.046, -0.010), (0.010, 0.040, 0.010), (0, 0, -15), TWINE),
    ("ankle-end-r2",      "leg-right", (-0.166, 0.050, 0.008), (0.010, 0.032, 0.010), (0, 0, 22), TWINE),
    # The waist knot's two ends.
    ("waist-end-1",       "torso", (-0.090, 0.200, -0.116), (0.010, 0.046, 0.008), (0, 0, -16), TWINE),
    ("waist-end-2",       "torso", (-0.076, 0.204, -0.114), (0.010, 0.036, 0.008), (0, 0, 10), TWINE),
    # Two black feathers out of the pouch's neck.
    ("feather-1",         "torso", (0.090, 0.250, -0.126), (0.010, 0.050, 0.004), (0, 0, 16), FEATHER),
    ("feather-2",         "torso", (0.110, 0.246, -0.128), (0.009, 0.042, 0.004), (0, 0, -22), FEATHER),
    # The crown knot's two ends.
    ("crown-end-1",       "head", (0.084, 0.728, -0.082), (0.010, 0.040, 0.008), (0, 0, 20), TWINE),
    ("crown-end-2",       "head", (0.070, 0.726, -0.084), (0.010, 0.034, 0.008), (0, 0, -12), TWINE),
    # The neck knot's two ends, hanging unevenly.
    ("neck-end-1",        "torso", (-0.040, 0.300, -0.090), (0.010, 0.044, 0.008), (0, 0, -12), TWINE),
    ("neck-end-2",        "torso", (-0.058, 0.303, -0.088), (0.010, 0.036, 0.008), (0, 0, 18), TWINE),
    # The crimson patch low on the back, and its two stitches.
    ("back-patch",        "torso", (-0.050, 0.230, 0.090), (0.070, 0.060, 0.008), (0, 0, -7), CRIMSON),
    # The right forearm's purple patch.
    ("arm-patch-r",       "arm-right", (-0.222, 0.300, -0.076), (0.060, 0.050, 0.006), (0, 0, 12), PATCH),
    # The X for her left eye.
    ("eye-x-a",           "head", (0.080, 0.542, -0.171), (0.088, 0.020, 0.008), (0, 0, 42), INK),
    ("eye-x-b",           "head", (0.080, 0.542, -0.171), (0.084, 0.020, 0.008), (0, 0, -40), INK),
    # The eye under the X is not gone: it glows through the stitches.
    ("eye-x-slit",        "head", (0.080, 0.542, -0.1690), (0.066, 0.024, 0.004), (0, 0, 0), GLOW),
    ("eye-x-slit-core",   "head", (0.080, 0.542, -0.1702), (0.044, 0.010, 0.004), (0, 0, 0), GLOW_CORE),
    # Light spills out of the top where the sack is tied, between the frayed threads.
    ("crown-glow",        "head", (0.000, 0.786, 0.004), (0.098, 0.010, 0.104), (0, 0, 0), GLOW),
    ("crown-glow-core",   "head", (0.000, 0.790, 0.004), (0.060, 0.008, 0.066), (0, 0, 0), GLOW_CORE),
    # Twine wound round the forearms, each its own lean, and round the right shin.
    ("wrap-l1",           "arm-left", (0.196, 0.288, 0.002), (0.012, 0.144, 0.154), (0, 0, 22), TWINE),
    ("wrap-l2",           "arm-left", (0.236, 0.288, 0.002), (0.011, 0.142, 0.152), (0, 0, -17), TWINE),
    ("wrap-r1",           "arm-right", (-0.160, 0.288, 0.000), (0.012, 0.140, 0.152), (0, 0, -24), TWINE),
    ("wrap-shin-r",       "leg-right", (-0.084, 0.096, -0.001), (0.140, 0.012, 0.154), (0, 0, 12), TWINE),
    # A patch of darker cloth sewn on its brow.
    ("head-patch",        "head", (0.106, 0.622, -0.1705), (0.068, 0.054, 0.005), (0, 0, -7), FABRIC_DARK),
    # The button and its rim, turned 45 degrees over their squares so both read round.
    # The thread holding the button on, from hole to hole.
    ("button-thread",     "head", (-0.078, 0.542, -0.1830), (0.030, 0.005, 0.003), (0, 0, -18), INK),
]

# The stitched grin: a slit of soul light through six points, higher at its left end (a smirk), sewn shut by four dark cross
# stitches at uneven spacing, each laid across the slit where it crosses it.
GRIN = [(-0.118, 0.452), (-0.070, 0.428), (-0.010, 0.418), (0.050, 0.424), (0.094, 0.440), (0.116, 0.464)]
GRIN_STITCHES = [-0.092, -0.041, 0.018, 0.071]
GRIN_Z = -0.1700

# Pins stuck in: (name, bone, where it enters, the direction it leaves in, length, head slot). Eight, with heads in her
# lilac, crimson, magenta and gold, so its silhouette bristles: out of its right temple and brow, the crown, twice from the
# back of its head, through the split in its chest, through its right arm and its left thigh.
PINS = [
    ("pin-temple-r",      "head", (-0.170, 0.640, -0.040), (-0.82, 0.50, -0.28), 0.150, PIN_HEAD),
    ("pin-brow-r",        "head", (-0.140, 0.664, -0.150), (-0.30, 0.45, -0.84), 0.120, PIN_MAGENTA),
    ("pin-crown-l",       "head", (0.120, 0.716, -0.030), (0.45, 0.85, -0.25), 0.150, CRIMSON),
    ("pin-back-l",        "head", (0.120, 0.600, 0.150), (0.35, 0.30, 0.89), 0.140, GOLD),
    ("pin-back-r",        "head", (-0.110, 0.480, 0.160), (-0.30, -0.10, 0.95), 0.120, PIN_MAGENTA),
    ("pin-heart",         "torso", (0.052, 0.290, -0.092), (0.25, 0.30, -0.92), 0.130, CRIMSON),
    ("pin-arm-r",         "arm-right", (-0.205, 0.350, -0.010), (-0.15, 0.95, -0.25), 0.120, PIN_HEAD),
    ("pin-thigh-l",       "leg-left", (0.100, 0.130, -0.078), (0.40, -0.15, -0.90), 0.110, GOLD),
]
PIN_SHAFT_THICK = 0.010

# ---------------------------------------------------------------------------------------------------------------------
# GLOWING INSIDE, STITCHED TOGETHER (v6; owner: *"make it look liek its glowing inside and it stitched tgthr"*, *"let it be its
# own"*, *"add crosshatch or smth"*, *"make it look way more detailed"*). Where it has split, the soul light shows (`GLOW`, the
# glow mesh), and it is held shut. Each list is typed by hand; a point is (across, up) on the named face at the named depth
# ("front"/"back" faces: x, y).
# ---------------------------------------------------------------------------------------------------------------------

# Splits: (name, bone, face, depth, the jagged line's points, each piece's width).
CRACKS = [
    ("crack-chest",   "torso", "front", -0.0915, [(0.092, 0.338), (0.070, 0.318), (0.078, 0.300), (0.052, 0.281), (0.060, 0.266),
                                                  (0.036, 0.252)], [0.022, 0.017, 0.024, 0.018, 0.021]),
    ("crack-belly",   "torso", "front", -0.1055, [(0.036, 0.248), (0.016, 0.232), (0.024, 0.215), (0.000, 0.199), (-0.016, 0.185)],
     [0.019, 0.022, 0.017, 0.020]),
    ("crack-spine",   "torso", "back", 0.0875, [(0.012, 0.322), (0.004, 0.296), (0.014, 0.268), (0.006, 0.238), (0.016, 0.206),
                                                (0.008, 0.188)], [0.012, 0.014, 0.011, 0.015, 0.012]),
    ("crack-shoulder-l", "arm-left", "front", -0.0715, [(0.118, 0.346), (0.114, 0.300), (0.120, 0.262), (0.116, 0.230)],
     [0.012, 0.014, 0.011]),
    ("crack-shoulder-r", "arm-right", "front", -0.0735, [(-0.118, 0.342), (-0.121, 0.294), (-0.115, 0.236)], [0.013, 0.012]),
    ("crack-thigh-r", "leg-right", "front", -0.0735, [(-0.128, 0.166), (-0.104, 0.146), (-0.086, 0.128), (-0.064, 0.114)],
     [0.012, 0.015, 0.011]),
]

# The big X stitches holding the chest shut: (name, bone, face, depth, centre, length, width, the two angles). Thick dark
# thread over the light; the top one is the cutscene's last stitch. ⚠️ v6 made them linen tape, which vanished into the linen.
STRAPS = [
    ("strap-chest-1", "torso", "front", -0.0955, (0.078, 0.318), 0.052, 0.010, 40, -44),
    ("strap-chest-2", "torso", "front", -0.0955, (0.062, 0.286), 0.048, 0.010, 36, -48),
    ("strap-chest-3", "torso", "front", -0.0955, (0.046, 0.260), 0.042, 0.009, 46, -40),
    ("strap-belly-1", "torso", "front", -0.1095, (0.022, 0.222), 0.048, 0.010, 44, -38),
    ("strap-belly-2", "torso", "front", -0.1095, (0.004, 0.196), 0.044, 0.009, 38, -46),
]

# Dark stitches across the other splits: (name, bone, face, depth, centre, angle, length).
STAPLES = [
    ("staple-sl-1", "arm-left", "front", -0.0755, (0.116, 0.330), 0, 0.026),
    ("staple-sl-2", "arm-left", "front", -0.0755, (0.117, 0.284), 8, 0.024),
    ("staple-sl-3", "arm-left", "front", -0.0755, (0.118, 0.245), -6, 0.026),
    ("staple-sr-1", "arm-right", "front", -0.0775, (-0.119, 0.318), -5, 0.026),
    ("staple-sr-2", "arm-right", "front", -0.0775, (-0.118, 0.262), 7, 0.024),
    ("staple-sp-1", "torso", "back", 0.0905, (0.008, 0.310), 4, 0.028),
    ("staple-sp-2", "torso", "back", 0.0905, (0.010, 0.281), -6, 0.026),
    ("staple-sp-3", "torso", "back", 0.0905, (0.010, 0.252), 3, 0.028),
    ("staple-sp-4", "torso", "back", 0.0905, (0.011, 0.222), -4, 0.026),
    ("staple-sp-5", "torso", "back", 0.0905, (0.012, 0.197), 6, 0.024),
    ("staple-th-1a", "leg-right", "front", -0.0775, (-0.116, 0.156), 45, 0.024),
    ("staple-th-1b", "leg-right", "front", -0.0775, (-0.116, 0.156), -45, 0.024),
    ("staple-th-2a", "leg-right", "front", -0.0775, (-0.078, 0.121), 40, 0.022),
    ("staple-th-2b", "leg-right", "front", -0.0775, (-0.078, 0.121), -48, 0.022),
]

# Crosshatch: (name, bone, face, depth, centre, angle, length, slot). The patches are crosshatched in thread, and the back of
# the head carries a coarse weave, so the big faces read as cloth rather than flat colour.
HATCH = [
    ("hatch-knee-1", "leg-left", "front", -0.0875, (0.066, 0.118), 45, 0.050, INK),
    ("hatch-knee-2", "leg-left", "front", -0.0875, (0.080, 0.118), 45, 0.068, INK),
    ("hatch-knee-3", "leg-left", "front", -0.0875, (0.093, 0.118), 45, 0.048, INK),
    ("hatch-knee-4", "leg-left", "front", -0.0875, (0.065, 0.119), -45, 0.048, INK),
    ("hatch-knee-5", "leg-left", "front", -0.0875, (0.079, 0.117), -45, 0.070, INK),
    ("hatch-knee-6", "leg-left", "front", -0.0875, (0.094, 0.118), -45, 0.050, INK),
    ("hatch-back-1", "torso", "back", 0.0955, (-0.064, 0.230), 45, 0.045, INK),
    ("hatch-back-2", "torso", "back", 0.0955, (-0.050, 0.231), 45, 0.062, INK),
    ("hatch-back-3", "torso", "back", 0.0955, (-0.036, 0.229), 45, 0.044, INK),
    ("hatch-back-4", "torso", "back", 0.0955, (-0.063, 0.229), -45, 0.046, INK),
    ("hatch-back-5", "torso", "back", 0.0955, (-0.049, 0.230), -45, 0.060, INK),
    ("hatch-back-6", "torso", "back", 0.0955, (-0.036, 0.231), -45, 0.045, INK),
    ("hatch-arm-1",  "arm-right", "front", -0.0805, (-0.232, 0.300), 45, 0.042, TWINE),
    ("hatch-arm-2",  "arm-right", "front", -0.0805, (-0.212, 0.301), 45, 0.040, TWINE),
    ("hatch-arm-3",  "arm-right", "front", -0.0805, (-0.231, 0.299), -45, 0.040, TWINE),
    ("hatch-arm-4",  "arm-right", "front", -0.0805, (-0.213, 0.300), -45, 0.042, TWINE),
    ("hatch-brow-1", "head", "front", -0.1745, (0.096, 0.622), 45, 0.042, TWINE),
    ("hatch-brow-2", "head", "front", -0.1745, (0.116, 0.621), 45, 0.040, TWINE),
    ("hatch-brow-3", "head", "front", -0.1745, (0.097, 0.621), -45, 0.040, TWINE),
    ("hatch-brow-4", "head", "front", -0.1745, (0.115, 0.623), -45, 0.042, TWINE),
    ("weave-back-1", "head", "back", 0.1715, (-0.100, 0.534), 45, 0.200, BURLAP_SHADOW),
    ("weave-back-2", "head", "back", 0.1715, (-0.030, 0.528), 45, 0.290, BURLAP_SHADOW),
    ("weave-back-3", "head", "back", 0.1715, (0.052, 0.531), 45, 0.280, BURLAP_SHADOW),
    ("weave-back-4", "head", "back", 0.1715, (0.122, 0.536), 45, 0.190, BURLAP_SHADOW),
    ("weave-back-5", "head", "back", 0.1715, (-0.098, 0.530), -45, 0.210, BURLAP_SHADOW),
    ("weave-back-6", "head", "back", 0.1715, (-0.024, 0.533), -45, 0.280, BURLAP_SHADOW),
    ("weave-back-7", "head", "back", 0.1715, (0.050, 0.527), -45, 0.290, BURLAP_SHADOW),
    ("weave-back-8", "head", "back", 0.1715, (0.120, 0.531), -45, 0.200, BURLAP_SHADOW),
]

FACE_NORMALS = {"front": (0.0, 0.0, -1.0), "back": (0.0, 0.0, 1.0)}
STAPLE_WIDTH = 0.006
HATCH_WIDTH = 0.0035
PIN_HEAD_SIZE = 0.034


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


def _face_row(name, bone, face, depth, centre, angle, length, width, thick, slot):
    """One flat stroke on a front or back face: centred at (x, y) on it, `depth` along Z, turned `angle` degrees in the face."""
    n = FACE_NORMALS[face]
    a = math.radians(angle)
    along = (math.cos(a), math.sin(a), 0.0)
    across = V._cross(n, along)
    m = tuple(tuple((along, across, n)[k][i] for k in range(3)) for i in range(3))
    return (name, bone, (centre[0], centre[1], depth), (length, width, thick), m, slot, "box")


def oriented_rows():
    """Every tilted part as (name, bone, centre, size, matrix, slot)."""
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
        rows.append((f"grin-{k}", "head", ((x0 + x1) / 2, (y0 + y1) / 2, GRIN_Z), (length, 0.018, 0.004),
                     _rot(0, 0, angle), GLOW, "box"))
        rows.append((f"grin-{k}-core", "head", ((x0 + x1) / 2, (y0 + y1) / 2, GRIN_Z - 0.0012), (length - 0.004, 0.007, 0.004),
                     _rot(0, 0, angle), GLOW_CORE, "box"))
    for k, x in enumerate(GRIN_STITCHES):
        y, slope = _grin_at(x)
        rows.append((f"grin-stitch-{k}", "head", (x, y, GRIN_Z - 0.0035), (0.011, 0.044, 0.005),
                     _rot(0, 0, math.degrees(math.atan(slope))), INK, "box"))

    # The splits, piece by piece along each jagged line, in the soul light.
    for name, bone, face, depth, points, widths in CRACKS:
        for k in range(len(points) - 1):
            (x0, y0), (x1, y1) = points[k], points[k + 1]
            length = math.hypot(x1 - x0, y1 - y0) + 0.006
            angle = math.degrees(math.atan2(y1 - y0, x1 - x0))
            rows.append(_face_row(f"{name}-{k}", bone, face, depth, ((x0 + x1) / 2, (y0 + y1) / 2), angle, length, widths[k],
                                  0.004, GLOW))
            proud = depth - 0.0012 if face == "front" else depth + 0.0012
            rows.append(_face_row(f"{name}-{k}-core", bone, face, proud, ((x0 + x1) / 2, (y0 + y1) / 2), angle, length - 0.004,
                                  widths[k] * 0.42, 0.004, GLOW_CORE))
    for name, bone, face, depth, centre, length, width, a1, a2 in STRAPS:
        rows.append(_face_row(name + "-a", bone, face, depth, centre, a1, length, width, 0.006, INK))
        rows.append(_face_row(name + "-b", bone, face, depth - 0.001 if face == "front" else depth + 0.001, centre, a2,
                              length * 0.96, width, 0.006, INK))
    for name, bone, face, depth, centre, angle, length in STAPLES:
        rows.append(_face_row(name, bone, face, depth, centre, angle, length, STAPLE_WIDTH, 0.004, INK))
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


def _grin_at(x):
    """The grin's height and slope at x, on the polyline."""
    for k in range(len(GRIN) - 1):
        (x0, y0), (x1, y1) = GRIN[k], GRIN[k + 1]
        if x0 <= x <= x1:
            t = (x - x0) / (x1 - x0)
            slope = (y1 - y0) / (x1 - x0)
            return y0 + (y1 - y0) * t, slope
    raise SystemExit(f"grin stitch at {x} is off the grin")


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


BODY_BOXES = LEG_LEFT + LEG_RIGHT + TORSO + ARM_LEFT + ARM_RIGHT
HEAD_BOXES = HEAD


def verify(body, head, rows):
    lo = [min(v[a] for v in body[0] + head[0]) for a in range(3)]
    hi = [max(v[a] for v in body[0] + head[0]) for a in range(3)]
    height = hi[1] - lo[1]
    print(f"boxes: body={len(BODY_BOXES)} head={len(HEAD_BOXES)} oriented={len(rows)}")
    print(f"tris: body={len(body[5]) // 3} head={len(head[5]) // 3}")
    print(f"bounds min={[round(v, 4) for v in lo]} max={[round(v, 4) for v in hi]}  height={height:.4f}")
    if not (V.CAST_MIN_HEIGHT - 0.005 <= height <= V.CAST_MAX_HEIGHT + 0.005):
        raise SystemExit(f"HEIGHT {height:.4f} outside the cast's {V.CAST_MIN_HEIGHT}..{V.CAST_MAX_HEIGHT}; nothing written.")
    if abs(lo[1]) > 0.001:
        raise SystemExit(f"feet are at y={lo[1]:.4f}, not 0.")
    head_reach = (V.CAST_MAX_HEIGHT - V.SKELETON["head"][1]) * 1.15
    for name, bone, box_lo, box_hi, slot in BODY_BOXES + HEAD_BOXES:
        origin = V.SKELETON[bone][1]
        reach = max(abs(box_lo[1] - origin), abs(box_hi[1] - origin))
        if reach > (head_reach if bone == "head" else 0.35):
            raise SystemExit(f"box '{name}' is {reach:.3f} from the {bone} bone: almost certainly on the wrong bone.")
    for name, bone, centre, size, m, slot, kind in rows:
        origin = V.SKELETON[bone][1]
        if abs(centre[1] - origin) > (head_reach if bone == "head" else 0.35):
            raise SystemExit(f"part '{name}' is far from the {bone} bone: almost certainly on the wrong bone.")
    r, g, b = (int(PALETTE[INK][i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    if 0.2126 * r + 0.7152 * g + 0.0722 * b > V.MAX_FACE_LUMINANCE:
        raise SystemExit("slot 8 (the face) must stay ink.")


def main():
    if not os.path.exists(V.BASE):
        raise SystemExit(f"base rig not found: {V.BASE}")
    gltf, buffer = V.read_glb(V.BASE)
    deltas = V.retarget(gltf, buffer)
    V.bind_matrices(gltf)

    rows = oriented_rows()
    lit = (GLOW, GLOW_CORE)
    glow_rows = [r for r in rows if r[5] in lit]
    body_rows = [r for r in rows if r[1] != "head" and r[5] not in lit]
    head_rows = [r for r in rows if r[1] == "head" and r[5] not in lit]
    body = assemble(BODY_BOXES, body_rows)
    head = assemble(HEAD_BOXES, head_rows)
    glow = assemble([], glow_rows)
    verify(body, head, rows)
    print(f"glow parts: {len(glow_rows)}, tris {len(glow[5]) // 3}")

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

    # ⚠️ THE SOUL LIGHT IS A THIRD SKINNED MESH ON THE BODY'S SKIN, NAMED `glow-mesh`, so the game can paint it unlit
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
