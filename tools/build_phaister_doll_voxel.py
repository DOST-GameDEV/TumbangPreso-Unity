"""Builds phaister-doll.glb: Phaister's voodoo doll at player size (HERO-10 v3, the VOODOO DOLL ultimate).

    python tools/build_phaister_doll_voxel.py

Owner, 2026-09-27: *"create a new model for the voodoo i guess"*, for his ultimate *"The voodoo doll becomes a sentient
being that assists you in attacking or defending for the rest of the round"*. The design is
`docs/reports/phaister-kit-2026-09-27/plan.md` section 9.10: a rag manika in HER colours, a burlap sack body with stitched
seams, magenta yarn hair like hers, a tiny copy of her hat worn askew, one button eye and one X-stitched eye, a mouth sewn
into a grin, pins in its head, twine at its neck, wrists and one ankle, a purple patch over its heart crossed by the last
stitch of the cutscene, and a tuft of stuffing out of one shoulder seam.

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
GOLD = 3            # her one metal: the hat buckle
TWINE = 4           # the ties at the neck, wrists and ankle
STUFFING = 5        # the tuft out of the shoulder seam
YARN = 6            # her magenta, as yarn
YARN_DARK = 7       # the darker yarn strands, so the hair is not one flat colour
INK = 8             # eyes, mouth, every stitch
PATCH = 9           # her royal purple: the heart patch, the hat band, the button eye
CRIMSON = 10        # her crimson: the knee patch, the back patch, one pin head
HAT = 11            # her hat's near black
PIN_HEAD = 12       # her lilac: pin heads
PIN_SHAFT = 13      # the pins' shafts, her gold dulled
SEAM_DARK = 14      # the darkest burlap, inside the stitched seams
SPARE = 15

# ⚠️ v2: the cloth is a paler linen than v1's a2865f, which sat beside Paete's bark and Sean's skin in the lineup as a third
# brown. Paler and a little greyer, it reads as sackcloth and stays apart from both.
PALETTE = {
    BURLAP:        "b59c74",
    BURLAP_SHADOW: "8c7352",
    BURLAP_LIGHT:  "c6ae86",
    GOLD:          "f8b824",
    TWINE:         "e2d2a8",
    STUFFING:      "f3e9c9",
    YARN:          "d8186e",
    YARN_DARK:     "a4105a",
    INK:           "14101c",
    PATCH:         "4a1e78",
    CRIMSON:       "8c1424",
    HAT:           "181622",
    PIN_HEAD:      "9838d8",
    PIN_SHAFT:     "b87814",
    SEAM_DARK:     "6a5539",
    SPARE:         "a2865f",
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
    ("ankle-seam-l",      "leg-left", (0.016, 0.060, -0.082), (0.152, 0.068, 0.076), SEAM_DARK),
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
# TORSO. A stuffed sack with a round belly and a gathered hem. Twine at the neck, knotted on her left of the chest. The
# heart patch sits high on her left, tilted as if sewn in a hurry. Seams down both sides, drawn separately (the right side
# has fewer stitches). Stuffing escapes at the back of the right shoulder. A crimson patch low on the back.
# ---------------------------------------------------------------------------------------------------------------------
TORSO = [
    ("body-sack",         "torso", (-0.148, 0.168, -0.090), (0.148, 0.338, 0.086), BURLAP),
    ("belly",             "torso", (-0.124, 0.182, -0.104), (0.124, 0.262, -0.062), BURLAP),
    ("hem",               "torso", (-0.151, 0.166, -0.093), (0.151, 0.180, 0.089), BURLAP_SHADOW),
    ("neck-twine",        "torso", (-0.094, 0.326, -0.076), (0.094, 0.352, 0.074), TWINE),
    ("neck-knot",         "torso", (0.028, 0.316, -0.092), (0.066, 0.356, -0.072), TWINE),
    ("seam-l",            "torso", (0.147, 0.188, -0.004), (0.154, 0.330, 0.004), INK),
    ("seam-l-t1",         "torso", (0.147, 0.198, -0.018), (0.155, 0.204, 0.016), INK),
    ("seam-l-t2",         "torso", (0.147, 0.229, -0.015), (0.155, 0.235, 0.019), INK),
    ("seam-l-t3",         "torso", (0.147, 0.257, -0.019), (0.155, 0.263, 0.015), INK),
    ("seam-l-t4",         "torso", (0.147, 0.296, -0.016), (0.155, 0.302, 0.018), INK),
    ("seam-l-t5",         "torso", (0.147, 0.318, -0.017), (0.155, 0.324, 0.014), INK),
    # The hem is torn at the front of the right hip and the stuffing escapes there (v2 hid it behind the hanging arm).
    ("seam-r",            "torso", (-0.154, 0.228, -0.004), (-0.147, 0.326, 0.004), INK),
    ("seam-r-t2",         "torso", (-0.155, 0.247, -0.014), (-0.147, 0.253, 0.019), INK),
    ("seam-r-t3",         "torso", (-0.155, 0.289, -0.018), (-0.147, 0.295, 0.015), INK),
    ("stuffing-a",        "torso", (-0.150, 0.166, -0.104), (-0.116, 0.196, -0.080), STUFFING),
    ("stuffing-b",        "torso", (-0.136, 0.156, -0.112), (-0.108, 0.180, -0.088), STUFFING),
    ("stuffing-c",        "torso", (-0.160, 0.182, -0.098), (-0.134, 0.206, -0.076), STUFFING),
]

# ---------------------------------------------------------------------------------------------------------------------
# ARMS. Stubby sacks ending in mittens, tied at the wrist. The left arm has a stitched seam along its top; the right arm
# has a purple patch and a pin stuck through it (the pin is an OBOX row below).
# ---------------------------------------------------------------------------------------------------------------------
ARM_LEFT = [
    ("arm-sack-l",        "arm-left", (0.096, 0.222, -0.070), (0.300, 0.354, 0.074), BURLAP),
    ("mitten-l",          "arm-left", (0.290, 0.218, -0.066), (0.356, 0.350, 0.070), BURLAP),
    ("wrist-twine-l",     "arm-left", (0.282, 0.215, -0.075), (0.300, 0.358, 0.079), TWINE),
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
]

# ---------------------------------------------------------------------------------------------------------------------
# HEAD. A big soft sack, a shade lighter than the body, gathered at the neck. The face: a round purple button sewn on for
# her right eye, an X of ink for her left, a stitched grin a little higher on her left (a smirk). Magenta yarn hangs from a
# domed yarn cap under the hat: a short uneven fringe across the brow, five strands down each side and six down the back,
# every one its own width, length and lean (`STRANDS`), with gaps where the sack shows through.
# ⚠️ v1 hung twenty-one strands of one width straight down, a curtain from every side (a barcode from behind), with a flat
# slab of yarn under the hat and no fringe, so from the front the hair was two pink side-curtains beside a bare forehead.
# ---------------------------------------------------------------------------------------------------------------------
HEAD = [
    ("head-sack",         "head", (-0.198, 0.352, -0.168), (0.198, 0.716, 0.170), BURLAP_LIGHT),
    ("head-crown",        "head", (-0.170, 0.700, -0.140), (0.170, 0.742, 0.142), BURLAP_LIGHT),
    ("chin-gather",       "head", (-0.150, 0.338, -0.140), (0.150, 0.366, 0.142), BURLAP_SHADOW),
    # The yarn cap, domed in two steps so the hat sits INTO hair, not on a lid.
    ("yarn-cap",          "head", (-0.188, 0.690, -0.156), (0.188, 0.742, 0.170), YARN),
    ("yarn-cap-top",      "head", (-0.150, 0.730, -0.124), (0.152, 0.762, 0.142), YARN),
    # The sack's back seam, seen through the gap in the back strands.
    ("seam-head-back",    "head", (-0.016, 0.402, 0.168), (-0.008, 0.640, 0.176), INK),
    ("seam-head-back-t1", "head", (-0.029, 0.426, 0.168), (0.003, 0.432, 0.177), INK),
    ("seam-head-back-t2", "head", (-0.027, 0.470, 0.168), (0.005, 0.476, 0.177), INK),
    ("seam-head-back-t3", "head", (-0.030, 0.527, 0.168), (0.002, 0.533, 0.177), INK),
    ("seam-head-back-t4", "head", (-0.026, 0.588, 0.168), (0.006, 0.594, 0.177), INK),
]

# Yarn: (name, top, bottom, width across the face it hangs on, depth off it, slot). Each strand is laid from its top to its
# bottom, so a bottom set off the top is the strand's lean. Front strands stop above the eyes (their tops are y 0.584).
STRANDS = [
    ("fringe-1",  (-0.172, 0.742, -0.170), (-0.177, 0.600, -0.176), 0.030, 0.014, YARN),
    ("fringe-2",  (-0.140, 0.742, -0.172), (-0.136, 0.648, -0.180), 0.026, 0.012, YARN_DARK),
    ("fringe-3",  (-0.074, 0.742, -0.172), (-0.079, 0.668, -0.179), 0.034, 0.012, YARN),
    ("fringe-4",  (-0.022, 0.742, -0.172), (-0.016, 0.640, -0.180), 0.028, 0.012, YARN),
    ("fringe-5",  (0.020, 0.742, -0.172), (0.014, 0.660, -0.179), 0.024, 0.012, YARN_DARK),
    ("fringe-6",  (0.098, 0.742, -0.172), (0.105, 0.632, -0.180), 0.032, 0.012, YARN),
    ("fringe-7",  (0.160, 0.742, -0.170), (0.169, 0.560, -0.178), 0.030, 0.014, YARN),
    ("side-l1",   (0.204, 0.742, -0.128), (0.214, 0.462, -0.134), 0.030, 0.014, YARN),
    ("side-l2",   (0.204, 0.742, -0.076), (0.210, 0.520, -0.072), 0.024, 0.014, YARN_DARK),
    ("side-l3",   (0.204, 0.742, -0.020), (0.219, 0.418, -0.028), 0.036, 0.014, YARN),
    ("side-l4",   (0.204, 0.742, 0.040), (0.212, 0.486, 0.046), 0.026, 0.014, YARN),
    ("side-l5",   (0.204, 0.742, 0.100), (0.216, 0.440, 0.110), 0.032, 0.014, YARN_DARK),
    ("side-r1",   (-0.204, 0.742, -0.122), (-0.212, 0.494, -0.118), 0.032, 0.014, YARN),
    ("side-r2",   (-0.204, 0.742, -0.060), (-0.219, 0.430, -0.066), 0.028, 0.014, YARN),
    ("side-r3",   (-0.204, 0.742, 0.004), (-0.210, 0.508, 0.012), 0.024, 0.014, YARN_DARK),
    ("side-r4",   (-0.204, 0.742, 0.066), (-0.216, 0.452, 0.060), 0.034, 0.014, YARN),
    ("side-r5",   (-0.204, 0.742, 0.124), (-0.210, 0.470, 0.130), 0.026, 0.014, YARN),
    ("back-1",    (-0.170, 0.742, 0.176), (-0.179, 0.450, 0.182), 0.030, 0.014, YARN),
    ("back-2",    (-0.112, 0.742, 0.176), (-0.106, 0.398, 0.184), 0.036, 0.014, YARN_DARK),
    ("back-3",    (-0.052, 0.742, 0.176), (-0.058, 0.470, 0.182), 0.026, 0.014, YARN),
    ("back-4",    (0.034, 0.742, 0.176), (0.042, 0.408, 0.184), 0.034, 0.014, YARN),
    ("back-5",    (0.096, 0.742, 0.176), (0.090, 0.462, 0.182), 0.028, 0.014, YARN_DARK),
    ("back-6",    (0.156, 0.742, 0.176), (0.166, 0.420, 0.184), 0.032, 0.014, YARN),
]

# Round parts: (name, bone, centre, radius, depth, sides, slot). They face the viewer (-Z). The button eye is sewn on: an
# ink rim, the purple button proud of it, two holes and the thread between them.
DISCS = [
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
    ("knee-stitch-l",     "leg-left", (0.079, 0.118, -0.088), (0.074, 0.010, 0.004), (0, 0, 36), INK),
    # Right ankle: the twine's two loose ends.
    ("ankle-end-r1",      "leg-right", (-0.171, 0.046, -0.010), (0.010, 0.040, 0.010), (0, 0, -15), TWINE),
    ("ankle-end-r2",      "leg-right", (-0.166, 0.050, 0.008), (0.010, 0.032, 0.010), (0, 0, 22), TWINE),
    # The heart patch, tilted, and the last stitch across it (the cutscene's): a big X, plus three border stitches.
    ("heart-patch",       "torso", (0.058, 0.298, -0.094), (0.078, 0.072, 0.008), (0, 0, 9), PATCH),
    ("heart-stitch-a",    "torso", (0.058, 0.298, -0.0995), (0.086, 0.012, 0.004), (0, 0, 49), INK),
    ("heart-stitch-b",    "torso", (0.058, 0.298, -0.0995), (0.080, 0.012, 0.004), (0, 0, -31), INK),
    ("heart-border-1",    "torso", (0.024, 0.326, -0.0995), (0.005, 0.018, 0.004), (0, 0, 9), INK),
    ("heart-border-2",    "torso", (0.095, 0.270, -0.0995), (0.005, 0.016, 0.004), (0, 0, 9), INK),
    ("heart-border-3",    "torso", (0.028, 0.265, -0.0995), (0.016, 0.005, 0.004), (0, 0, 9), INK),
    # The neck knot's two ends, hanging unevenly.
    ("neck-end-1",        "torso", (0.040, 0.300, -0.090), (0.010, 0.044, 0.008), (0, 0, 12), TWINE),
    ("neck-end-2",        "torso", (0.058, 0.303, -0.088), (0.010, 0.036, 0.008), (0, 0, -18), TWINE),
    # The crimson patch low on the back, and its two stitches.
    ("back-patch",        "torso", (-0.050, 0.230, 0.090), (0.070, 0.060, 0.008), (0, 0, -7), CRIMSON),
    ("back-stitch-1",     "torso", (-0.070, 0.230, 0.0945), (0.006, 0.040, 0.004), (0, 0, -7), INK),
    ("back-stitch-2",     "torso", (-0.030, 0.232, 0.0945), (0.006, 0.034, 0.004), (0, 0, -3), INK),
    # The right forearm's purple patch.
    ("arm-patch-r",       "arm-right", (-0.222, 0.300, -0.076), (0.060, 0.050, 0.006), (0, 0, 12), PATCH),
    # The X for her left eye.
    ("eye-x-a",           "head", (0.080, 0.542, -0.171), (0.088, 0.020, 0.008), (0, 0, 42), INK),
    ("eye-x-b",           "head", (0.080, 0.542, -0.171), (0.084, 0.020, 0.008), (0, 0, -40), INK),
    # The button and its rim, turned 45 degrees over their squares so both read round.
    # The thread holding the button on, from hole to hole.
    ("button-thread",     "head", (-0.078, 0.542, -0.1830), (0.030, 0.005, 0.003), (0, 0, -18), INK),
]

# The stitched grin: a line through six points, higher at her left end (the smirk), then four cross stitches at uneven
# spacing, each laid across the line where it crosses it.
GRIN = [(-0.118, 0.452), (-0.070, 0.428), (-0.010, 0.418), (0.050, 0.424), (0.094, 0.440), (0.116, 0.464)]
GRIN_STITCHES = [-0.092, -0.041, 0.018, 0.071]
GRIN_Z = -0.171

# The hat, a tiny copy of hers worn askew: typed in its own frame round a pivot on her crown, then tilted as one piece
# toward her left and back. The upper cone leans further and the tip droops (a hat that has been sat on).
HAT_PIVOT = (0.030, 0.750, 0.000)
HAT_TILT = (6.0, 0.0, -11.0)   # back 6 degrees, toward her left 11 (v1: 14, off a flat lid)
HAT_PARTS = [
    ("hat-brim",          (-0.180, 0.000, -0.180), (0.180, 0.022, 0.180), HAT),
    ("hat-band",          (-0.118, 0.022, -0.118), (0.118, 0.056, 0.118), PATCH),
    ("hat-buckle",        (-0.030, 0.026, -0.126), (0.030, 0.052, -0.116), GOLD),
    ("hat-cone-1",        (-0.112, 0.056, -0.112), (0.112, 0.100, 0.112), HAT),
    ("hat-cone-2",        (-0.084, 0.100, -0.090), (0.096, 0.148, 0.090), HAT),
    ("hat-cone-3",        (-0.050, 0.148, -0.066), (0.082, 0.194, 0.066), HAT),
    ("hat-cone-4",        (-0.014, 0.194, -0.044), (0.074, 0.236, 0.044), HAT),
    ("hat-tip-1",         (0.026, 0.232, -0.024), (0.074, 0.262, 0.024), HAT),
    ("hat-tip-2",         (0.060, 0.244, -0.014), (0.090, 0.268, 0.014), HAT),
]

# Pins stuck in: (name, bone, where it enters, the direction it leaves in, length, head slot). Out of her right temple,
# the back of her head twice, and one through her right arm.
PINS = [
    ("pin-temple-r",      "head", (-0.170, 0.640, -0.040), (-0.82, 0.50, -0.28), 0.150, PIN_HEAD),
    ("pin-back-l",        "head", (0.120, 0.600, 0.150), (0.35, 0.30, 0.89), 0.140, PIN_HEAD),
    ("pin-back-r",        "head", (-0.110, 0.480, 0.160), (-0.30, -0.10, 0.95), 0.120, CRIMSON),
    ("pin-arm-r",         "arm-right", (-0.205, 0.350, -0.010), (-0.15, 0.95, -0.25), 0.120, PIN_HEAD),
]
PIN_SHAFT_THICK = 0.010
PIN_HEAD_SIZE = 0.030


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


def oriented_rows():
    """Every tilted part as (name, bone, centre, size, matrix, slot)."""
    rows = [(n, b, c, s, _rot(*r), slot, "box") for n, b, c, s, r, slot in OBOX]

    for name, top, bottom, width, depth, slot in STRANDS:
        up = tuple(top[i] - bottom[i] for i in range(3))
        length = math.sqrt(V._dot(up, up)) + 0.006
        centre = tuple((top[i] + bottom[i]) * 0.5 for i in range(3))
        # A side strand's width runs along the side of the head (Z); a front or back strand's across it (X).
        size = (depth, length, width) if name.startswith("side") else (width, length, depth)
        rows.append(("yarn-" + name, "head", centre, size, _towards(up), slot, "box"))

    for name, bone, centre, radius, depth, sides, slot in DISCS:
        rows.append((name, bone, centre, (radius, depth, sides), _rot(0, 0, 0), slot, "disc"))

    for k in range(len(GRIN) - 1):
        (x0, y0), (x1, y1) = GRIN[k], GRIN[k + 1]
        length = math.hypot(x1 - x0, y1 - y0) + 0.008
        angle = math.degrees(math.atan2(y1 - y0, x1 - x0))
        rows.append((f"grin-{k}", "head", ((x0 + x1) / 2, (y0 + y1) / 2, GRIN_Z), (length, 0.013, 0.007),
                     _rot(0, 0, angle), INK, "box"))
    for k, x in enumerate(GRIN_STITCHES):
        y, slope = _grin_at(x)
        rows.append((f"grin-stitch-{k}", "head", (x, y, GRIN_Z - 0.001), (0.011, 0.042, 0.007),
                     _rot(0, 0, math.degrees(math.atan(slope))), INK, "box"))

    tilt = _rot(*HAT_TILT)
    for name, lo, hi, slot in HAT_PARTS:
        local = tuple((lo[i] + hi[i]) * 0.5 for i in range(3))
        size = tuple(hi[i] - lo[i] for i in range(3))
        r = _apply(tilt, local)
        centre = tuple(HAT_PIVOT[i] + r[i] for i in range(3))
        rows.append((name, "head", centre, size, tilt, slot, "box"))

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
    body_rows = [r for r in rows if r[1] != "head"]
    head_rows = [r for r in rows if r[1] == "head"]
    body = assemble(BODY_BOXES, body_rows)
    head = assemble(HEAD_BOXES, head_rows)
    verify(body, head, rows)

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
