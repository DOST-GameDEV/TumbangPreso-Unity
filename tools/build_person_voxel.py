"""Builds a team-authored voxel Person from a CC0 rig: new mesh, retargeted skeleton.

    python tools/build_person_voxel.py

WHY THIS EXISTS, AND WHY IT IS A SCRIPT RATHER THAN A MODELLING SESSION.
`docs/Port_Plan.md` section 8 puts the twelve people third in the replacement
order and calls them "the largest job by far, because it is twelve rigs plus
animation retargeting". That cost is almost entirely the RIG and the CLIPS, not
the shape: the shape of a Kenney mini is a pile of boxes, and so is the art that
replaces it. What is expensive is keeping 32 authored clips working across the
swap, and that is exactly what a script can do exactly and a modelling session
cannot.

  KEPT from the base .glb   both skins, all 32 animation clips, the material and
                            the colormap atlas, and every bone NAME.
  REPLACED                  the vertices, normals, UVs and skin weights, and the
                            bone REST POSITIONS, with the clips corrected to match.

⚠️⚠️ THE BONE NAMES ARE UNTOUCHABLE. `CharacterVisual.BuildHandAnchor` and
`CharacterAnimator.ResolveChargeBone` both hunt `arm-right` then `arm-left` BY
STRING, and a miss is one warning in a match log and a tsinelas hanging in the
air. The seven names are the contract; where the bones SIT is not.

⚠️⚠️ AND THE REST POSITIONS ARE MOVED, WHICH THE FIRST BUILD OF THIS ASSUMED THEY
COULD NOT BE. That build authored the new character around the Kenney skeleton and
came out with a head 53% of its own height against the reference art's 25%, which
is not a stylisation of the reference, it is a different character. The assumption
behind it was that the clips pin the skeleton. `tools/glb_anim_channels.py` was
written to check rather than keep assuming, and the answer is narrower than the
fear:

  * `head`, `arm-left` and `arm-right` translations are NEVER keyed away from rest
    by any of the 32 clips. Those bones are free.
  * `root`, both legs and `torso` are keyed, by 4 clips between them, and every one
    of those tracks is an ABSOLUTE local position. Shifting the rest position and
    every keyframe of those tracks by the SAME vector moves the bone and preserves
    the animation exactly, because only the difference between them is motion.

⚠️ THE INVERSE BIND MATRICES ARE RECOMPUTED, NOT PATCHED. Moving a bone without
them is the failure mode with no error attached: the mesh skins against the OLD
bind pose and the character comes apart limb by limb the first time it moves. The
whole rest pose is pure translation with identity rotation and scale, measured off
the base file, so `IBM = translate(-worldPosition)` outright.

⚠️ LIMB LENGTHS ARE CHANGED SPARINGLY AND THE REASON IS THE CLIPS. These limbs have
no knee or elbow, so a clip's hip rotation sweeps the whole leg: doubling the leg
doubles the stride the animator authored. The legs here grow by 36% and nothing
else grows at all. The arms and head MOVE without changing length, which costs
nothing at all, because a rotation about a relocated shoulder traces exactly the
same arc it always did.

⚠️ AUTHORED HEIGHT IS PINNED TO THE BASE'S 0.7234. `CharacterVisual.PersonScale` is
a single constant of 2.38 applied to every Person, measured off the imported model's
AABB. A replacement authored to a different height does not get its own scale, it
gets that one, so it walks the arena at the wrong size. The check at the end of this
file fails the build rather than letting that ship.

⚠️ EVERY COLOUR COMES FROM THE PALETTE, NOT FROM A NEW TEXTURE. `Toon.shader` picks
a Person's colour from WHICH 32x32 cell of the 512x512 atlas a vertex lands in, so
authoring a UV at the centre of a chosen cell is how a box declares which of the
sixteen palette entries paints it. A bespoke texture would work exactly once and
then opt this character out of the recolour mechanism, the stun frost and the
two-band toon pass that the other eleven get for free.

⚠️ SLOT 8 STAYS DARK AND CARRIES THE FACE, the same hard constraint the Godot
generator aborts on.

ADDING THE NEXT CHARACTER: copy the four box tables and the palette, give them a
new OUT path, and call `build(...)`. Everything below the tables is character
agnostic. `Assets/TumbangPreso/Editor/PersonSwapProbe.cs` is the check that says
whether the result actually works, and it is worth pointing at the new file first.
"""
import math
import json
import os
import struct
import sys

BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"

OUT = "Assets/TumbangPreso/Art/characters/persons/team-inday.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-inday.tres"

# Joint order is identical on both skins of every rig in this set, verified by
# tools/glb_dump.py. Named here so a box declares a BONE rather than an index.
BONE = {"root": 0, "leg-left": 1, "leg-right": 2, "torso": 3,
        "arm-left": 4, "arm-right": 5, "head": 6}

PARENT = {"leg-left": "root", "leg-right": "root", "torso": "root",
          "arm-left": "torso", "arm-right": "torso", "head": "torso"}

# ---------------------------------------------------------------------------
# THE SKELETON, in world space at rest. See the module docstring for what is free
# to move and what it costs.
#
# Base rig, and the proportions it produces:   new:
#   hips     0.176   legs 24% of height        0.232   legs 32%
#   shoulder 0.288                             0.400
#   neck     0.343   torso 23%, head 53%       0.445   torso 30%, head 38%
#
# ⚠️⚠️ 38% OF HEIGHT FOR THE HEAD IS DELIBERATE AND WAS ARRIVED AT BY OVERSHOOTING
# FIRST. A pass at 30% put the head between this cast and a realistic one and landed
# as neither: 🧑 seeing it on the sheet, *"WTF IS THAT HEAD"*. The problem was not the
# number on its own, it was that the HAIR is most of this character's silhouette and a
# small head leaves it nowhere to go, so the mop flattened into a cap and the whole
# read went with it.
#
# The reference art carries a head and hair mass of roughly a third of the figure, and
# the eleven characters standing next to this one carry 53%. 38% sits inside both, and
# the height it buys back over 30% goes almost entirely into hair volume rather than
# into a bigger face.
# ---------------------------------------------------------------------------
# ⚠️⚠️ THESE ARE THE BASE RIG'S OWN JOINT HEIGHTS AGAIN, AND GOING BACK TO THEM IS THE
# FAMILY PASS. See `_family` further down for the whole reasoning; the short version is
# that the numbers in the table above this block were always the target — legs 24%, torso
# 23%, head 53% — and this character had drifted to 32/30/38, which reads as a taller,
# thinner, smaller-headed person standing next to eleven who are not.
#
# ⚠️ THE BOX TABLES ARE AUTHORED AGAINST THE OLD HEIGHTS AND ARE REMAPPED AT BUILD TIME
# rather than rewritten. `WAS_HIPS`/`WAS_SHOULDER`/`WAS_NECK`/`WAS_TOP` below record what
# they are authored against; changing one of these without the other is how the boxes and
# the bones stop agreeing, and the symptom is a limb that animates from the wrong pivot.
SKELETON = {
    "root":      (0.0,      0.0,   0.0),
    "leg-left":  (0.08357,  0.176, -0.02875),
    "leg-right": (-0.08357, 0.176, -0.02875),
    "torso":     (0.0,      0.176, -0.02875),
    "arm-left":  (0.0999,   0.288, -0.01725),
    "arm-right": (-0.0999,  0.288, -0.01725),
    "head":      (0.0,      0.343, -0.00236),
}


def cell_uv(slot):
    """The atlas cell for a palette slot, from the shader's own formula.

    ⚠️ COLUMNS COME IN PAIRS: an even column is a flat swatch and the odd one beside
    it is that swatch's shading ramp. The odd column is used because that is where
    every cell the base rigs sample lands, so the file still reads correctly if the
    palette is ever switched off and the raw atlas shows through.
    """
    col = 2 * (slot % 8) + 1
    row = 9 if slot < 8 else 13
    return ((col + 0.5) / 16.0, (row + 0.5) / 16.0)


# ---------------------------------------------------------------------------
# THE CHARACTER.
#
# Boxes in model space: metres, +Y up, and authored here with the character FACING
# -Z, which is the direction you would call "forward" reading the table.
#
# ⚠️⚠️ THE FILE'S OWN FRONT IS +Z, AND THE FLIP IS APPLIED AT BUILD TIME BY
# `FRONT_IS_MINUS_Z`. This was authored the other way round first, on the strength of
# `CharacterVisual.PersonModelYaw`'s note that "the rig wears its face on -Z", and
# the character came out with the back of its head where the face should be. That
# note describes a different space, and reading it as a claim about the .glb is the
# same mistake its own header says cost ten sessions on the Godot side.
#
# What settled it is a measurement, and it is repeatable: `tools/glb_face_side.py`
# finds the vertices whose UVs land in slot 8, which on a head is the eyes and the
# mouth and nothing else, and reports which side of the mesh they sit on. On the base
# rig they are at z +0.1596.
#
# ⚠️ +X IS THE CHARACTER'S LEFT, because the bone named `leg-left` sits at +0.08357.
# glTFast negates X on import, so left and right swap between this file and the Unity
# scene. Author against the BONE NAMES, never against which side of a screenshot
# something appears on.
#
# Slot key, and what the reference art puts there:
#   0 jacket purple    1 black cloth      2 clip magenta   3 buckle gold
#   4 shoe purple      5 chain silver     6 hair black     7 jacket shadow
#   8 face and ink     12 white           13/14/15 skin ramp
# ---------------------------------------------------------------------------

OVERALLS, OVERALLS_DARK, CYAN_TRIM, BUTTONS = 0, 1, 2, 3
SHOE_CYAN, COLLAR_TRIM, HAIR, SHADOW_CYAN = 4, 5, 6, 7
INK, SILVER, WOOD_GOLD, FROST_ACCENT = 8, 9, 10, 11
WHITE, SKIN, SKIN_DARK, SKIN_LIT = 12, 13, 14, 15

PALETTE = {
    OVERALLS:      "48d4e8",   # Crisp frosty sky cyan overalls, pocket, skirt, straps
    OVERALLS_DARK: "2696a8",   # Deeper glacier cyan for pocket lip trim, skirt hem band, seams
    CYAN_TRIM:     "64e2f6",   # Bright icy frost blue for ribbons, bow knot, barrette clip, dye streak
    BUTTONS:       "ffffff",   # Pure snow white button rivets
    SHOE_CYAN:     "48d4e8",   # Sneaker upper frosty cyan
    COLLAR_TRIM:   "2696a8",   # Frosty collar lapels and sleeve cuffs trim
    HAIR:          "14121a",   # Deep glossy black hair with midnight frost undertone
    SHADOW_CYAN:   "1e7a88",   # Rich shadow band for toon shader
    INK:           "1f1c24",   # Face ink (eyes & smile)
    SILVER:        "d4e2ec",   # Silver buckles, earring hook, spatula neck, carabiner ring
    WOOD_GOLD:     "e4a032",   # Warm bakery spatula wooden handle peeking from pocket
    FROST_ACCENT:  "a8f0fa",   # Shimmering pale frost highlight / crystal sparkle
    WHITE:         "f4faff",   # Crisp snow white with subtle cool undertone (toque, shirt, socks)
    SKIN:          "f5b894",   # Option 2: Natural Fair Peach midtone (face & body)
    SKIN_DARK:     "db9874",   # Luminous warm peach shadow tone
    SKIN_LIT:      "f5b894",   # 100% matched with SKIN midtone for uniform face/body skin
}

MAX_FACE_LUMINANCE = 0.30


def mirrored(boxes, bone_from, bone_to):
    out = []
    for name, bone, lo, hi, slot in boxes:
        assert bone == bone_from, f"{name} is on {bone}, not {bone_from}"
        out.append((name.replace("left", "right"), bone_to,
                    (-hi[0], lo[1], lo[2]), (-lo[0], hi[1], hi[2]), slot))
    return out


# Hips at 0.240, so the leg owns everything below it.
# 🌟 ASYMMETRIC SOCKS: Left leg wears a high-knee athletic sock with dual frost stripes!
LEG_LEFT = [
    # White sneaker sole
    ("shoe-sole-left",   "leg-left", (0.006, 0.000, -0.134), (0.158, 0.024, 0.082), WHITE),
    # Frosty cyan sneaker upper
    ("shoe-upper-left",  "leg-left", (0.014, 0.024, -0.126), (0.152, 0.056, 0.076), SHOE_CYAN),
    # White toe cap (front profile)
    ("shoe-toe-left",    "leg-left", (0.012, 0.024, -0.132), (0.154, 0.044, -0.100), WHITE),
    # White heel counter (side & back profile)
    ("shoe-heel-left",   "leg-left", (0.012, 0.024, 0.052), (0.154, 0.056, 0.080), WHITE),
    # Icy heel pull tab & frost gem
    ("shoe-pulltab-left",    "leg-left", (0.070, 0.054, 0.076), (0.096, 0.082, 0.088), CYAN_TRIM),
    ("shoe-pulltab-gem-left","leg-left", (0.076, 0.062, 0.084), (0.090, 0.076, 0.092), FROST_ACCENT),
    # White laces
    ("shoe-lace-left",   "leg-left", (0.030, 0.038, -0.108), (0.136, 0.050, -0.092), WHITE),
    # White sneaker collar
    ("shoe-collar-left", "leg-left", (0.012, 0.056, -0.096), (0.154, 0.070, 0.078), WHITE),
    
    # Sporty high-knee sock (white)
    ("sock-high-left",   "leg-left", (0.016, 0.070, -0.076), (0.150, 0.165, 0.074), WHITE),
    # Dual icy-cyan frost athletic stripes around calf
    ("sock-stripe1-left","leg-left", (0.014, 0.128, -0.078), (0.152, 0.140, 0.076), CYAN_TRIM),
    ("sock-stripe2-left","leg-left", (0.014, 0.146, -0.078), (0.152, 0.158, 0.076), CYAN_TRIM),
    
    # Fair human skin leg above high sock
    ("leg-skin-left",    "leg-left", (0.018, 0.165, -0.070), (0.148, 0.232, 0.070), SKIN_LIT),
]

# 🌟 Right leg wears a folded ankle sock showing fair skin!
LEG_RIGHT = [
    # White sneaker sole
    ("shoe-sole-right",   "leg-right", (-0.158, 0.000, -0.134), (-0.006, 0.024, 0.082), WHITE),
    # Frosty cyan sneaker upper
    ("shoe-upper-right",  "leg-right", (-0.152, 0.024, -0.126), (-0.014, 0.056, 0.076), SHOE_CYAN),
    # White toe cap (front profile)
    ("shoe-toe-right",    "leg-right", (-0.154, 0.024, -0.132), (-0.012, 0.044, -0.100), WHITE),
    # White heel counter (side & back profile)
    ("shoe-heel-right",   "leg-right", (-0.154, 0.024, 0.052), (-0.012, 0.056, 0.080), WHITE),
    # Icy heel pull tab & frost gem
    ("shoe-pulltab-right",    "leg-right", (-0.096, 0.054, 0.076), (-0.070, 0.082, 0.088), CYAN_TRIM),
    ("shoe-pulltab-gem-right","leg-right", (-0.090, 0.062, 0.084), (-0.076, 0.076, 0.092), FROST_ACCENT),
    # White laces
    ("shoe-lace-right",   "leg-right", (-0.136, 0.038, -0.108), (-0.030, 0.050, -0.092), WHITE),
    # White sneaker collar
    ("shoe-collar-right", "leg-right", (-0.154, 0.056, -0.096), (-0.012, 0.070, 0.078), WHITE),
    
    # Folded short ankle sock (white)
    ("sock-ankle-right",  "leg-right", (-0.150, 0.070, -0.076), (-0.016, 0.098, 0.074), WHITE),
    ("sock-cuff-right",   "leg-right", (-0.152, 0.092, -0.078), (-0.014, 0.108, 0.076), CYAN_TRIM),
    
    # Fair human skin leg
    ("leg-skin-right",    "leg-right", (-0.148, 0.108, -0.070), (-0.018, 0.232, 0.070), SKIN_LIT),
]

# Torso owns everything between 0.240 and 0.440
TORSO = [
    # White rolled undershirt base (visible at neck and lower waist/sides)
    ("shirt-body",          "torso", (-0.118, 0.235, -0.092), (0.118, 0.445, 0.092), WHITE),
    
    # 👗 OVERALLS SHORTS / HIPS (Clean high-waisted shorts with frosty cyan trim)
    # Shorts lower waist / groin
    ("shorts-base",         "torso", (-0.125, 0.232, -0.096), (0.125, 0.320, 0.096), OVERALLS),
    ("shorts-hem-cuff",     "torso", (-0.128, 0.230, -0.098), (0.128, 0.248, 0.098), OVERALLS_DARK),
    # Left shorts leg cuff
    ("shorts-cuff-left",    "torso", (0.010, 0.230, -0.098), (0.128, 0.248, 0.098), CYAN_TRIM),
    # Right shorts leg cuff
    ("shorts-cuff-right",   "torso", (-0.128, 0.230, -0.098), (-0.010, 0.248, 0.098), CYAN_TRIM),
    
    # 🎽 OVERALLS BIB & POCKET (Center chest bib in Team Cyan)
    ("bib-front",           "torso", (-0.105, 0.315, -0.104), (0.105, 0.415, -0.078), OVERALLS),
    # Stepped upper bib hem
    ("bib-top-hem",         "torso", (-0.095, 0.410, -0.106), (0.095, 0.420, -0.082), OVERALLS_DARK),
    
    # 🦘 ICONIC KANGAROO BIB POCKET (Front & center with spatula peeking!)
    ("bib-pocket-main",     "torso", (-0.075, 0.325, -0.112), (0.075, 0.380, -0.098), OVERALLS_DARK),
    ("bib-pocket-rim",      "torso", (-0.078, 0.375, -0.114), (0.078, 0.385, -0.096), CYAN_TRIM),
    # ❄️ Embroidered Team Snowflake Icon on Bib Pocket Center
    ("bib-pocket-frost-icon","torso",(-0.020, 0.342, -0.116), (0.020, 0.362, -0.110), FROST_ACCENT),
    ("bib-pocket-icon-dot",  "torso",(-0.008, 0.348, -0.118), (0.008, 0.356, -0.112), WHITE),
    
    # 🍳 TUCKED BAKERY SPATULA HANDLE peeking from right side of bib pocket (-X)
    ("spatula-handle-wood", "torso", (-0.065, 0.365, -0.114), (-0.045, 0.428, -0.100), WOOD_GOLD),
    ("spatula-metal-neck",  "torso", (-0.062, 0.385, -0.116), (-0.048, 0.395, -0.098), SILVER),
    ("spatula-hole",        "torso", (-0.058, 0.418, -0.116), (-0.052, 0.424, -0.098), WHITE),
    
    # 📏 SUSPENDER STRAPS (Front straps running over shoulders to back)
    # Left suspender front
    ("strap-front-left",    "torso", (0.062, 0.380, -0.106), (0.095, 0.448, -0.086), OVERALLS),
    ("strap-buckle-left",   "torso", (0.058, 0.395, -0.112), (0.098, 0.415, -0.094), SILVER),
    ("strap-buckle-pin-l",  "torso", (0.072, 0.400, -0.114), (0.084, 0.410, -0.100), FROST_ACCENT),
    # Right suspender front
    ("strap-front-right",   "torso", (-0.095, 0.380, -0.106), (-0.062, 0.448, -0.086), OVERALLS),
    ("strap-buckle-right",  "torso", (-0.098, 0.395, -0.112), (-0.058, 0.415, -0.094), SILVER),
    ("strap-buckle-pin-r",  "torso", (-0.084, 0.400, -0.114), (-0.072, 0.410, -0.100), FROST_ACCENT),
    
    # Suspender shoulder arches
    ("strap-shoulder-left", "torso", (0.062, 0.440, -0.088), (0.095, 0.450, 0.088), OVERALLS),
    ("strap-shoulder-right","torso", (-0.095, 0.440, -0.088), (-0.062, 0.450, 0.088), OVERALLS),
    
    # 🔙 BACK SUSPENDERS: Classic Crossed X-Back Straps
    ("strap-back-left",     "torso", (0.062, 0.315, 0.080), (0.095, 0.445, 0.098), OVERALLS),
    ("strap-back-right",    "torso", (-0.095, 0.315, 0.080), (-0.062, 0.445, 0.098), OVERALLS),
    ("strap-cross-brace",   "torso", (-0.085, 0.360, 0.086), (0.085, 0.385, 0.100), OVERALLS_DARK),
    ("strap-cross-gem",     "torso", (-0.015, 0.366, 0.092), (0.015, 0.378, 0.104), FROST_ACCENT),
    
    # Neck skin (V-neck opening)
    ("neck-skin",           "torso", (-0.030, 0.380, -0.088), (0.030, 0.445, -0.076), SKIN_LIT),
    # Crisp triangular frosty cyan collar wings/lapels
    ("collar-left",         "torso", (0.018, 0.390, -0.108), (0.065, 0.445, -0.082), COLLAR_TRIM),
    ("collar-right",        "torso", (-0.065, 0.390, -0.108), (-0.018, 0.445, -0.082), COLLAR_TRIM),
    # Center neckline cute cyan bowtie knot & loops
    ("collar-ribbon-knot",  "torso", (-0.015, 0.395, -0.114), (0.015, 0.415, -0.096), CYAN_TRIM),
    ("collar-ribbon-loop-l","torso", (0.012, 0.398, -0.112), (0.038, 0.418, -0.098), CYAN_TRIM),
    ("collar-ribbon-loop-r","torso", (-0.038, 0.398, -0.112), (-0.012, 0.418, -0.098), CYAN_TRIM),
    ("collar-ribbon-gem",   "torso", (-0.008, 0.400, -0.116), (0.008, 0.412, -0.098), FROST_ACCENT),
    # White shirt back collar
    ("collar-back",         "torso", (-0.095, 0.425, 0.080), (0.095, 0.445, 0.094), WHITE),
]

# 🌟 ASYMMETRIC WRISTWEAR: Left wrist has sporty cyan sweatband with white stripe!
ARM_LEFT = [
    # White short sleeve
    ("sleeve-left",         "arm-left", (0.0999, 0.330, -0.066), (0.208, 0.470, 0.084), WHITE),
    # Stepped frosty cyan sleeve cuff
    ("sleeve-cuff-left",    "arm-left", (0.208, 0.322, -0.074), (0.234, 0.478, 0.092), COLLAR_TRIM),
    # Icy-Cyan Sports Wristband with White Stripe on Left Wrist
    ("wristband-left",      "arm-left", (0.250, 0.334, -0.024), (0.285, 0.466, 0.042), CYAN_TRIM),
    ("wristband-stripe",    "arm-left", (0.262, 0.332, -0.026), (0.274, 0.468, 0.044), WHITE),
    # Fair human skin forearm & hand
    ("hand-left",           "arm-left", (0.234, 0.3383, -0.020), (0.3836, 0.4617, 0.038), SKIN_LIT),
]

# Right arm has clean fair human skin
ARM_RIGHT = [
    # White short sleeve
    ("sleeve-right",        "arm-right", (-0.208, 0.330, -0.066), (-0.0999, 0.470, 0.084), WHITE),
    # Stepped frosty cyan sleeve cuff
    ("sleeve-cuff-right",   "arm-right", (-0.234, 0.322, -0.074), (-0.208, 0.478, 0.092), COLLAR_TRIM),
    # Fair human skin forearm & hand
    ("hand-right",          "arm-right", (-0.3836, 0.3383, -0.020), (-0.234, 0.4617, 0.038), SKIN_LIT),
]

HEAD = [
    # Hair core covering back and sides of skull
    ("hair-core",           "head", (-0.180, 0.480, -0.215), (0.180, 0.700, -0.040), HAIR),
    ("hair-crown",          "head", (-0.176, 0.650, -0.170), (0.176, 0.705, 0.140), HAIR),
    
    # 💇 BACK HAIR DRAPES & STRUCTURED TIERS (Smooth, elegant layered cascade)
    ("hair-back-tier1",     "head", (-0.176, 0.340, -0.218), (0.176, 0.490, -0.135), HAIR),
    ("hair-back-tier2",     "head", (-0.174, 0.230, -0.216), (0.174, 0.348, -0.138), HAIR),
    
    # 🎀 Frosted Star Clasp & Twin Fluttering Ribbon Tails (Positioned below Ushanka fur hem)
    ("hair-back-star-clasp",    "head", (-0.030, 0.335, -0.228), (0.030, 0.375, -0.214), FROST_ACCENT),
    ("hair-back-star-gem",      "head", (-0.015, 0.342, -0.232), (0.015, 0.368, -0.216), WHITE),
    ("hair-back-ribbon-tail-l", "head", (0.020, 0.230, -0.225), (0.065, 0.340, -0.215), CYAN_TRIM),
    ("hair-back-ribbon-tail-r", "head", (-0.065, 0.230, -0.225), (-0.020, 0.340, -0.215), CYAN_TRIM),
    ("hair-back-ribbon-tip-l",  "head", (0.035, 0.200, -0.225), (0.075, 0.235, -0.215), FROST_ACCENT),
    ("hair-back-ribbon-tip-r",  "head", (-0.075, 0.200, -0.225), (-0.035, 0.235, -0.215), FROST_ACCENT),
    
    # 📐 Raised horizontal weight band & clean frosted bottom hem rim
    ("hair-back-weight-band",   "head", (-0.176, 0.155, -0.224), (0.176, 0.215, -0.145), HAIR),
    ("hair-back-hem-rim",       "head", (-0.172, 0.148, -0.218), (0.172, 0.165, -0.148), HAIR),
    ("hair-back-frost-hem",     "head", (-0.172, 0.148, -0.224), (0.172, 0.165, -0.216), CYAN_TRIM),
    ("hair-back-frost-sparkle", "head", (-0.140, 0.150, -0.226), (0.140, 0.162, -0.220), FROST_ACCENT),
    
    # 📐 Side framing rails
    ("hair-back-rail-left",     "head", (0.158, 0.170, -0.220), (0.180, 0.600, -0.160), HAIR),
    ("hair-back-rail-right",    "head", (-0.180, 0.170, -0.220), (-0.158, 0.600, -0.160), HAIR),
    
    # 💇 SEAMLESS SIDE HAIR & LOCKS
    ("hair-side-cap-left",      "head", (0.165, 0.490, -0.160), (0.190, 0.670, 0.120), HAIR),
    ("hair-side-cap-right",     "head", (-0.190, 0.490, -0.160), (-0.165, 0.670, 0.120), HAIR),
    ("hair-side-lock-left",     "head", (0.168, 0.350, 0.000), (0.188, 0.420, 0.080), HAIR),
    ("hair-side-lock-right",    "head", (-0.188, 0.350, 0.000), (-0.168, 0.420, 0.080), HAIR),
    ("hair-side-frost-lock-l",  "head", (0.170, 0.355, 0.010), (0.190, 0.410, 0.050), CYAN_TRIM),
    ("hair-side-frost-lock-r",  "head", (-0.190, 0.355, 0.010), (-0.170, 0.410, 0.050), CYAN_TRIM),
    
    # 💇 100% PURE CLEAN BLACK SCULPTED 3D BANGS
    ("hair-brow-base",          "head", (-0.176, 0.585, 0.130), (0.176, 0.675, 0.172), HAIR),
    ("hair-fringe-center",      "head", (-0.045, 0.555, 0.158), (0.045, 0.620, 0.188), HAIR),
    ("hair-fringe-mid-l",       "head", (0.040, 0.540, 0.156), (0.115, 0.625, 0.185), HAIR),
    ("hair-fringe-mid-r",       "head", (-0.115, 0.540, 0.156), (-0.040, 0.625, 0.185), HAIR),
    ("hair-fringe-outer-l",     "head", (0.110, 0.505, 0.150), (0.174, 0.620, 0.180), HAIR),
    ("hair-fringe-outer-r",     "head", (-0.174, 0.505, 0.150), (-0.110, 0.620, 0.180), HAIR),
    ("hair-vol-lock-c",         "head", (-0.035, 0.565, 0.182), (0.035, 0.635, 0.198), HAIR),
    ("hair-vol-lock-l",         "head", (0.045, 0.550, 0.180), (0.105, 0.630, 0.195), HAIR),
    ("hair-vol-lock-r",         "head", (-0.105, 0.550, 0.180), (-0.045, 0.630, 0.195), HAIR),
    ("hair-vol-lock-r2",        "head", (-0.110, 0.555, 0.182), (-0.040, 0.628, 0.196), HAIR),
    ("hair-vol-lock-l2",        "head", (0.040, 0.555, 0.182), (0.110, 0.628, 0.196), HAIR),

    # 🥽 PRO HIGH-TECH SKI GOGGLES (Curved conformal wrap around forehead & crown)
    ("goggle-strap-b",      "head", (-0.180, 0.638, -0.234), (0.180, 0.672, -0.200), OVERALLS_DARK),
    ("goggle-strap-l",      "head", (0.176, 0.638, -0.214), (0.208, 0.672, 0.140), OVERALLS_DARK),
    ("goggle-strap-r",      "head", (-0.208, 0.638, -0.214), (-0.176, 0.672, 0.140), OVERALLS_DARK),
    ("goggle-strap-trim-l", "head", (0.180, 0.648, -0.120), (0.210, 0.662, 0.120), CYAN_TRIM),
    ("goggle-strap-trim-r", "head", (-0.210, 0.648, -0.120), (-0.180, 0.662, 0.120), CYAN_TRIM),
    ("goggle-hinge-l",      "head", (0.168, 0.628, 0.140), (0.202, 0.688, 0.195), SILVER),
    ("goggle-hinge-r",      "head", (-0.202, 0.628, 0.140), (-0.168, 0.688, 0.195), SILVER),
    ("goggle-frame-l",      "head", (0.015, 0.625, 0.195), (0.165, 0.700, 0.240), SILVER),
    ("goggle-frame-r",      "head", (-0.165, 0.625, 0.195), (-0.015, 0.700, 0.240), SILVER),
    ("goggle-frame-wrap-l", "head", (0.142, 0.628, 0.148), (0.190, 0.694, 0.210), SILVER),
    ("goggle-frame-wrap-r", "head", (-0.190, 0.628, 0.148), (-0.142, 0.694, 0.210), SILVER),
    ("goggle-bridge-top",   "head", (-0.020, 0.660, 0.200), (0.020, 0.694, 0.235), SILVER),
    ("goggle-bridge-arch",  "head", (-0.015, 0.638, 0.202), (0.015, 0.662, 0.230), SILVER),
    ("goggle-bridge-rivet", "head", (-0.010, 0.670, 0.232), (0.010, 0.686, 0.240), WHITE),
    ("goggle-lens-l",       "head", (0.026, 0.635, 0.210), (0.152, 0.690, 0.246), CYAN_TRIM),
    ("goggle-lens-r",       "head", (-0.152, 0.635, 0.210), (-0.026, 0.690, 0.246), CYAN_TRIM),
    ("goggle-lens-wrap-l",  "head", (0.142, 0.638, 0.158), (0.180, 0.686, 0.215), CYAN_TRIM),
    ("goggle-lens-wrap-r",  "head", (-0.180, 0.638, 0.158), (-0.142, 0.686, 0.215), CYAN_TRIM),
    ("goggle-depth-l",      "head", (0.032, 0.660, 0.225), (0.145, 0.686, 0.248), FROST_ACCENT),
    ("goggle-depth-r",      "head", (-0.145, 0.660, 0.225), (-0.032, 0.686, 0.248), FROST_ACCENT),
    ("goggle-glint-l",      "head", (0.045, 0.668, 0.232), (0.090, 0.684, 0.250), WHITE),
    ("goggle-glint-r",      "head", (-0.138, 0.668, 0.232), (-0.092, 0.684, 0.250), WHITE),

    # 🧢 ORGANIC VOLUMETRIC EXPEDITION USHANKA
    # Crown & Quilted Puffy Dome
    ("ush-crown-core",      "head", (-0.180, 0.630, -0.205), (0.180, 0.730, 0.155), OVERALLS),
    ("ush-crown-side-l",    "head", (0.170, 0.635, -0.180), (0.206, 0.725, 0.130), OVERALLS),
    ("ush-crown-side-r",    "head", (-0.206, 0.635, -0.180), (-0.170, 0.725, 0.130), OVERALLS),
    ("ush-crown-front",     "head", (-0.165, 0.635, 0.140), (0.165, 0.725, 0.182), OVERALLS),
    ("ush-crown-back",      "head", (-0.165, 0.635, -0.226), (0.165, 0.725, -0.188), OVERALLS),
    ("ush-crown-fl",        "head", (0.130, 0.635, 0.110), (0.192, 0.725, 0.168), OVERALLS),
    ("ush-crown-fr",        "head", (-0.192, 0.635, 0.110), (-0.130, 0.725, 0.168), OVERALLS),
    ("ush-crown-bl",        "head", (0.130, 0.635, -0.216), (0.192, 0.725, -0.148), OVERALLS),
    ("ush-crown-br",        "head", (-0.192, 0.635, -0.216), (-0.130, 0.725, -0.148), OVERALLS),
    ("ush-dome-top",        "head", (-0.155, 0.725, -0.175), (0.155, 0.768, 0.130), OVERALLS),
    ("ush-dome-top-l",      "head", (0.140, 0.725, -0.145), (0.175, 0.760, 0.105), OVERALLS),
    ("ush-dome-top-r",      "head", (-0.175, 0.725, -0.145), (-0.140, 0.760, 0.105), OVERALLS),
    ("ush-dome-top-f",      "head", (-0.130, 0.725, 0.110), (0.130, 0.760, 0.152), OVERALLS),
    ("ush-dome-top-b",      "head", (-0.130, 0.725, -0.198), (0.130, 0.760, -0.155), OVERALLS),
    ("ush-seam-x",          "head", (-0.158, 0.730, -0.015), (0.158, 0.762, 0.015), OVERALLS_DARK),
    ("ush-seam-z",          "head", (-0.015, 0.730, -0.178), (0.015, 0.762, 0.135), OVERALLS_DARK),
    ("ush-pompom-base",     "head", (-0.058, 0.758, -0.058), (0.058, 0.785, 0.058), WHITE),
    ("ush-pompom-top",      "head", (-0.036, 0.782, -0.036), (0.036, 0.792, 0.036), WHITE),
    ("ush-pompom-star",     "head", (-0.016, 0.776, -0.016), (0.016, 0.788, 0.016), CYAN_TRIM),

    # Curved Plush Fur Visor Brim
    ("ush-visor-backing",   "head", (-0.170, 0.610, 0.155), (0.170, 0.690, 0.205), OVERALLS_DARK),
    ("ush-visor-center",    "head", (-0.142, 0.610, 0.175), (0.142, 0.702, 0.238), WHITE),
    ("ush-visor-bevel-l",   "head", (0.120, 0.610, 0.150), (0.185, 0.696, 0.222), WHITE),
    ("ush-visor-bevel-r",   "head", (-0.185, 0.610, 0.150), (-0.120, 0.696, 0.222), WHITE),
    ("ush-visor-lip",       "head", (-0.148, 0.665, 0.195), (0.148, 0.708, 0.244), WHITE),
    ("ush-visor-star",      "head", (-0.028, 0.648, 0.230), (0.028, 0.682, 0.246), CYAN_TRIM),
    ("ush-visor-star-core", "head", (-0.014, 0.655, 0.238), (0.014, 0.675, 0.248), FROST_ACCENT),

    # Sculpted 360-Connected Earflaps
    ("ush-flap-root-l",     "head", (0.170, 0.540, -0.150), (0.222, 0.650, 0.110), OVERALLS),
    ("ush-flap-root-r",     "head", (-0.222, 0.540, -0.150), (-0.170, 0.650, 0.110), OVERALLS),
    ("ush-flap-bell-l",     "head", (0.175, 0.415, -0.150), (0.236, 0.565, 0.105), OVERALLS),
    ("ush-flap-cushion-l",  "head", (0.220, 0.430, -0.130), (0.240, 0.550, 0.085), OVERALLS_DARK),
    ("ush-flap-star-l",     "head", (0.234, 0.470, -0.035), (0.242, 0.510, 0.015), CYAN_TRIM),
    ("ush-flap-gem-l",      "head", (0.238, 0.482, -0.020), (0.244, 0.498, -0.000), FROST_ACCENT),
    ("ush-flap-bell-r",     "head", (-0.236, 0.415, -0.150), (-0.175, 0.565, 0.105), OVERALLS),
    ("ush-flap-cushion-r",  "head", (-0.240, 0.430, -0.130), (-0.220, 0.550, 0.085), OVERALLS_DARK),
    ("ush-flap-star-r",     "head", (-0.242, 0.470, -0.035), (-0.234, 0.510, 0.015), CYAN_TRIM),
    ("ush-flap-gem-r",      "head", (-0.244, 0.482, -0.020), (-0.238, 0.498, -0.000), FROST_ACCENT),
    ("ush-flap-wedge-l",    "head", (0.145, 0.415, -0.216), (0.212, 0.640, -0.140), OVERALLS),
    ("ush-flap-wedge-r",    "head", (-0.212, 0.415, -0.216), (-0.145, 0.640, -0.140), OVERALLS),
    ("ush-flap-jaw-l",      "head", (0.170, 0.325, -0.130), (0.222, 0.440, 0.080), OVERALLS),
    ("ush-flap-jaw-r",      "head", (-0.222, 0.325, -0.130), (-0.170, 0.440, 0.080), OVERALLS),
    ("ush-flap-fur-l",      "head", (0.165, 0.315, -0.140), (0.232, 0.390, 0.090), WHITE),
    ("ush-flap-inner-fur-l","head", (0.162, 0.335, 0.025), (0.202, 0.485, 0.095), WHITE),
    ("ush-flap-fur-r",      "head", (-0.232, 0.315, -0.140), (-0.165, 0.390, 0.090), WHITE),
    ("ush-flap-inner-fur-r","head", (-0.202, 0.335, 0.025), (-0.162, 0.485, 0.095), WHITE),
    ("ush-cord-l",          "head", (0.192, 0.255, -0.025), (0.212, 0.330, 0.005), CYAN_TRIM),
    ("ush-cord-r",          "head", (-0.212, 0.255, -0.025), (-0.192, 0.330, 0.005), CYAN_TRIM),
    ("ush-cord-pompom-l",   "head", (0.182, 0.215, -0.038), (0.222, 0.260, 0.018), WHITE),
    ("ush-cord-pompom-r",   "head", (-0.222, 0.215, -0.038), (-0.182, 0.260, 0.018), WHITE),

    # Multi-Layered Padded Nape Mantle
    ("ush-back-upper-c",    "head", (-0.170, 0.490, -0.234), (0.170, 0.650, -0.155), OVERALLS),
    ("ush-back-upper-pad",  "head", (-0.150, 0.505, -0.238), (0.150, 0.630, -0.170), OVERALLS),
    ("ush-back-upper-l",    "head", (0.128, 0.490, -0.228), (0.192, 0.650, -0.155), OVERALLS),
    ("ush-back-upper-r",    "head", (-0.192, 0.490, -0.228), (-0.128, 0.650, -0.155), OVERALLS),
    ("ush-back-quilt-seam", "head", (-0.165, 0.490, -0.236), (0.165, 0.505, -0.160), OVERALLS_DARK),
    ("ush-back-lower-c",    "head", (-0.165, 0.375, -0.230), (0.165, 0.495, -0.165), OVERALLS),
    ("ush-back-lower-l",    "head", (0.125, 0.375, -0.224), (0.185, 0.495, -0.165), OVERALLS),
    ("ush-back-lower-r",    "head", (-0.185, 0.375, -0.224), (-0.125, 0.495, -0.165), OVERALLS),
    ("ush-back-fur-main",   "head", (-0.168, 0.365, -0.238), (0.168, 0.412, -0.170), WHITE),
    ("ush-back-fur-lip",    "head", (-0.155, 0.360, -0.240), (0.155, 0.395, -0.190), WHITE),
    ("ush-back-fur-conn-l", "head", (0.135, 0.350, -0.230), (0.204, 0.405, -0.135), WHITE),
    ("ush-back-fur-conn-r", "head", (-0.204, 0.350, -0.230), (-0.135, 0.405, -0.135), WHITE),
    ("ush-back-strap",      "head", (-0.015, 0.410, -0.234), (0.015, 0.645, -0.224), OVERALLS_DARK),
]

DONOR_SPACE = tuple(entry[0] for entry in HEAD)

# ---------------------------------------------------------------------------
# § THE FAMILY PASS.
#
# ⚠️⚠️ 🧑 2026-08-18, with the cast sheet in front of him: *"he doesnt feel like he's part
# of the family"*, *"he looks liek he's from a diff game"*. The numbers were already written
# at the top of this file:
#
#     the eleven he stands next to   legs 24%   torso 23%   head 53%
#     this character, before         legs 32%   torso 30%   head 38%
#
# A 38% head in a line-up of 53% heads is not a variation, it is a different toy.
#
# ⚠️ THE TABLES ARE REMAPPED, NOT REWRITTEN. Every box carries a measurement and a reason —
# the sole's thickness, the chain's link gap, the hand's height against `HandTopLift`.
# Re-authoring them by hand against new joint heights loses all of it and is wrong in ways
# only a turnaround catches. This moves each REGION onto its family value and leaves every
# relationship inside a region exactly as measured.
# ---------------------------------------------------------------------------

# Source joint heights, as the tables above are authored.
WAS_HIPS, WAS_SHOULDER, WAS_NECK, WAS_TOP = 0.232, 0.400, 0.445, 0.722

# The base rig's own, from the header's table. These ARE the family proportions.
NOW_HIPS, NOW_SHOULDER, NOW_NECK, NOW_TOP = 0.176, 0.288, 0.343, 0.7234

HEAD_GROWTH = (NOW_TOP - NOW_NECK) / (WAS_TOP - WAS_NECK)

# ⚠️ MEASURED OFF THE TWELVE SHIPPED RIGS, not chosen. See the height check in `verify()`
# for the full table and for why a single number was the wrong bound.
CAST_MIN_HEIGHT, CAST_MAX_HEIGHT = 0.6613, 0.7928


def _remap_y(y):
    """Legs, torso and head each onto their family band."""
    if y <= WAS_HIPS:
        return y / WAS_HIPS * NOW_HIPS
    if y <= WAS_NECK:
        t = (y - WAS_HIPS) / (WAS_NECK - WAS_HIPS)
        return NOW_HIPS + t * (NOW_NECK - NOW_HIPS)

    t = (y - WAS_NECK) / (WAS_TOP - WAS_NECK)
    return NOW_NECK + t * (NOW_TOP - NOW_NECK)


def _family(boxes, head, as_authored=()):
    """The remap, applied to one table.

    ⚠️ AN ARM IS TRANSLATED, NEVER SQUASHED. Its Y extent is thickness, not length, so
    putting it through the torso's 0.78 would give this character thinner arms than the cast
    he is joining. Moving it to the new shoulder also preserves the hand's height against
    `HandTopLift` by construction: the box is authored as shoulder +/- that constant, and a
    pure translation leaves the identity alone.
    """
    out = []

    for entry in boxes:
        name, bone, lo, hi, slot = entry[:5]
        rest = entry[5:]

        # ⚠️⚠️ SOME HEAD BOXES ARE AUTHORED IN THE DONOR'S OWN SPACE. The skull is lifted off
        # a CC0 rig at 1:1 (`_donor_head`), so anything measured AGAINST it is already in
        # final coordinates and must be neither remapped nor grown.
        #
        # ⚠️ ITS Z IS PRE-FLIPPED SO `build_mesh`'s FLIP RESTORES IT. Everything in the tables
        # is authored facing -Z and negated at build time; a box measured off the donor is
        # already in the file's own space, so without undoing that flip here it lands on the
        # opposite side of the head. The earring looked right from the front and was on the
        # wrong face of the ear.
        if name in as_authored:
            out.append((name, bone,
                        (lo[0], lo[1], -hi[2]), (hi[0], hi[1], -lo[2]), slot) + rest)
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


BODY_BOXES = (LEG_LEFT + LEG_RIGHT
              + TORSO
              + ARM_LEFT + ARM_RIGHT)
HEAD_BOXES = HEAD

# ---------------------------------------------------------------------------
# § THE DONATED HEAD. See the note above `HEAD`.
#
# ⚠️ IT DROPS IN AT 1:1 WITH NO TRANSFORM, and that is not luck. The donor's head spans
# y 0.343 to 0.722, which is exactly `NOW_NECK` to `NOW_TOP`: the family pass put this
# character on the base rig's own joint heights, so the donor's head already fits the
# skeleton it is being attached to. Move the proportions off the base rig again and this
# stops being free.
#
# ⚠️⚠️ AND BOTH DONORS SHARE THAT SPACE EXACTLY, which is what lets a skull come off one rig
# and a mop off another. Measured, not assumed: `character-male-d` slot 15 and
# `character-male-a` slot 14 are the same shell to four decimal places, y 0.3432 to 0.6613
# at |x| 0.2268 and z -0.1624 to 0.1576. Every rig in the set is that shell plus its own
# second one. Check it with `tools/glb_mesh_dump.py` before adding a third donor.
DONOR_SKULL = "Assets/TumbangPreso/Art/characters/persons/character-male-d.glb"

# ⚠️⚠️ SLOT 13 OF THE SKULL DONOR IS DROPPED ON PURPOSE. It is his bald pate: y 0.3932 to
# 0.7218, which is the HAIR volume worn in skin. Keeping it is what left the mop nowhere to
# go. See the block above `HEAD`, including why the note claiming this took his jaw was
# wrong.
SKULL_SLOTS = {15: None, 8: None}

# ⚠️⚠️ THERE IS NO DONATED MOP, AND THE ONE THAT WAS HERE IS WHY. `character-male-a`'s
# slot 8 dropped in at 1:1 and gave a hairline that follows the skull, which no box can do.
# It also brought his HAIRCUT, and his haircut is a scalloped fringe across the brow:
# 🧑 *"the hairs sucks shiiit why does it have bangs"*. Every hair shell in this set has one,
# because they all belong to characters who wear one, and a box laid over the scallops only
# replaces them with a straight cut. This character's reference has no fringe at all.
#
# So `_donor_part` is still the mechanism and still takes a repaint, and the next character
# who DOES want a fringe should use it. This one draws its hair in `HEAD`.

# ⚠️⚠️ THE MOUTH IS BENT, NOT REPLACED, AND REPLACING IT BROKE THE FACE. 🧑: *"change the
# expression just a bit to edgy or nonchalant? the :)"*, then *"bro look at ur render u
# broke the face hahah"*. A donated head is a rounded ovoid: z 0.1596 is its frontmost point
# at the CENTRE only, and the surface curves away toward the cheeks. Two axis-aligned boxes
# laid across that plane punched out through both cheeks and showed their own side faces as
# dark tabs at three-quarters, the exact angle the character screen uses.
#
# The donor's own mouth triangles already lie ON that curved surface. Moving their vertices
# in Y alone keeps them there whatever the surface does in Z, which no box can do.
#
# ⚠️⚠️ AND THEN THE BEND TOOK THE WHOLE LOWER HEAD WITH IT. 🧑 2026-08-18, on the turnaround
# that followed: *"the jaw is gone and the face is so buggy now"*. The selection below was
# a HEIGHT TEST AND NOTHING ELSE, run over every triangle in `head-mesh`, so it caught 70
# triangles and 129 of the head's 375 vertices when the mouth is 8 and 10. Everything below
# y 0.45 is the jaw, the chin, the lobes of both ears and the collar of the neck, and all of
# it got crushed to 0.22 of its distance from the mouth's centre and then tilted by its own
# x. At the ear that is 227 mm of x against a tilt of 0.10, so one side of the head lifted
# 23 mm and the other dropped 23 mm: the jaw collapsed into a flat band and the ears sheared
# into wedges.
#
# ⚠️ IT ALSO COST A BUILD IN THE WRONG PLACE. The jaw went missing in the same hour slot 13
# was first dropped, so the drop was blamed and reverted, and the pate came back with it.
# The mop had nowhere to sit for one more build because of a bad attribution.
#
# THE SELECTION IS BY SLOT FIRST AND HEIGHT SECOND. Slot 8 is the ink, which on a head is
# the eyes and the mouth and nothing else, so `_slot_at` narrows 221 triangles to 20 before
# the height test splits those 20 into the two features. That is the same measurement
# `tools/glb_face_side.py` makes to answer which way a rig faces, and it is the only test
# here that knows what a mouth IS. A height alone cannot: the jaw is at the same height.
#
# ⚠️ THE COUNT IS ASSERTED BELOW so this cannot quietly widen again. A selection that grows
# does not fail, it deforms, and the failure is a screenshot away rather than a stack trace.
#
# The split is measured: on this rig the twelve eye triangles sit at centroid y 0.4714 to
# 0.5066 and the eight mouth triangles at 0.4058 to 0.4295, with nothing in between.
DONOR_MOUTH_Y = 0.45
DONOR_MOUTH_TRIS = 8
DONOR_MOUTH_VERTS = 10
DONOR_EYE_TRIS = 12
DONOR_EYE_VERTS = 16


# ⚠️⚠️ THE MOUTH IS DRAWN, NOT BENT, AND THREE PASSES OF BENDING IT IS WHY. 🧑, each time:
# *"bro look at ur render u broke the face hahah"*, *"the facial expression doesnt look
# nonchalant or smug or edgy anymore too"*, *"look at the mouth he is smiling ts aint edgy"*.
#
# The donated mouth is a FILLED BOWL, an open grin with its interior inked, and no affine
# bend of a filled bowl is a smirk. Flattening it thins the stroke until the shape vanishes;
# tilting it swings the bowl without opening it. The last attempt measured 51.9 mm tall
# against an eye of 27.1 mm, because tilt over a 90 mm mouth adds its own lift to the height
# twice over. A mouth twice the size of an eye reads as a grin whatever its curve is doing.
#
# ⚠️⚠️ AND THE FACE IS FLAT, WHICH THE NOTE THAT STARTED ALL THIS SAID IT WAS NOT. Every ink
# vertex on this donor sits at z 0.1596 exactly, eyes and mouth alike: it is an inset PLATE,
# not a patch of a curved ovoid. The claim that "two axis-aligned boxes punched out through
# both cheeks" was true of BOXES, which have depth and corners; it says nothing about a
# polygon lying in that plane. So the mouth can be authored outright, the same way
# `FACE_PIXELS` was drawn before the head was donated, and `PANEL_PROUD` keeps it off the
# skin by the same fraction of a millimetre.
#
# The shape is a tapered stroke: thin at the character's RIGHT, thickening as it rises to the
# left, with a short flick up at the end. That is a smirk, and it is 24 mm tall, which is
# just under an eye.
MOUTH_Z = 0.1596
MOUTH_HALF = 0.042
MOUTH_BASE = 0.4135

# The centreline's rise across the whole mouth, and the stroke's weight at each end. The
# taper is what makes it read as one-sided: an even stroke at an angle is a straight line
# drawn crooked, and a stroke that grows into the lift is a lip curling.
MOUTH_RISE = 0.013
MOUTH_THIN = 0.0032
MOUTH_THICK = 0.0092

# The flick. It applies over the last `MOUTH_HOOK_FROM` of the +x end and is what stops the
# stroke reading as a frown drawn upward.
MOUTH_HOOK_FROM = 0.55
MOUTH_HOOK = 0.006

# How many samples along it. The chamfer does not touch this (it is not a box) and the
# outline traces the polygon, so the only thing resolution buys is a smooth taper.
MOUTH_STEPS = 14


def _mouth_polygon():
    """This character's own mouth, as a closed polygon on the face plate.

    ⚠️ RETURNED IN FILE SPACE, NOT TABLE SPACE. The donated head is not put through
    `build_mesh`'s `FRONT_IS_MINUS_Z` flip, so +z is the face and these are used as written.
    """
    upper, lower = [], []

    for k in range(MOUTH_STEPS + 1):
        t = k / MOUTH_STEPS
        x = -MOUTH_HALF + t * (2.0 * MOUTH_HALF)

        centre = MOUTH_BASE + (t - 0.5) * MOUTH_RISE
        half = 0.5 * (MOUTH_THIN + t * (MOUTH_THICK - MOUTH_THIN))

        if t > MOUTH_HOOK_FROM:
            u = (t - MOUTH_HOOK_FROM) / (1.0 - MOUTH_HOOK_FROM)
            centre += MOUTH_HOOK * u * u

        upper.append((x, centre + half))
        lower.append((x, centre - half))

    return upper + list(reversed(lower))


# ⚠️⚠️ AND THE EYES CARRY MORE OF THE EXPRESSION THAN THE MOUTH DOES. 🧑 after two passes
# that only touched the mouth: *"the facial expression doesnt look nonchalant or smug or
# edgy anymore too"*. The donated eyes are tall rounded pupils set wide apart, which is the
# open, friendly read the whole CC0 cast shares, and no mouth under it is going to say
# nonchalant on its own. Half lidding them does, and it is the single cheapest expression
# change on a face this size: the outline swallows anything subtle in the mouth, but an eye
# that is 55% of its height is 55% of its height at 90 px as well.
#
# `EYE_SQUASH` scales each eye toward its OWN centre rather than toward a shared line,
# because the two are not at the same height on this rig and squashing both toward one
# would leave the character walleyed. `EYE_DROP` then lowers both, which is the difference
# between narrowed (annoyed) and lidded (bored): the lid comes down from the brow, so the
# ink has to move down with it or it reads as a squint.
EYE_SQUASH = 0.55
EYE_DROP = 0.008


def _slot_at(u, v):
    """Which palette slot an atlas UV samples, by the shader's own formula.

    ⚠️ IT IS THE INVERSE OF `cell_uv` AND IT IS NEEDED FOR THE DONORS ONLY. Our own boxes
    declare a slot and get a UV; a donated mesh arrives with UVs already baked and has to
    be asked. Rows under 8 are the atlas's non-palette half and belong to no slot.
    """
    col = min(int(u * 16.0), 15)
    row = min(int(v * 16.0), 15)

    if row < 8:
        return None

    return (col // 2) + (8 if row >= 12 else 0)


def _donor_part(path, slots):
    """One rig's `head-mesh`, filtered to `slots` and repainted where a slot maps to one.

    `slots` is {source slot: destination slot or None}. A destination rewrites the UV to
    that palette cell, which is how a donated hair shell becomes THIS character's hair
    colour without touching the atlas.

    ⚠️ A TRIANGLE IS KEPT ON ITS FIRST VERTEX'S SLOT, AND THE VERTICES ARE REINDEXED. The
    atlas cell is per vertex, so a mesh split by slot would otherwise carry indices into an
    array it no longer has, which glTF validates as an out-of-range accessor and Unity
    imports as an empty mesh with no error text worth reading.
    """
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
                    pos.append(src_pos[i])
                    nrm.append(src_nrm[i])
                    uv.append(cell_uv(paint) if paint is not None else src_uv[i])

            tris.append(tuple(remap[i] for i in tri))

        if not tris:
            raise SystemExit(f"{path} has no triangles in slots {sorted(slots)}")

        return pos, nrm, uv, tris

    raise SystemExit(f"{path} has no head-mesh")


# ⚠️⚠️ THE FACE HAS BROKEN TWICE, BOTH TIMES ON AN EXPRESSION CHANGE, AND THIS IS THE GUARD
# THAT MAKES IT AN ERROR INSTEAD OF A SCREENSHOT. 🧑 2026-08-18: *"last time the face broke
# when we changde expression pls try to make srue it doesnt happen again"*.
#
# The shape of both failures was the same. An expression edit selects some vertices and
# moves them, the selection is wider than intended, and NOTHING FAILS: the mesh is still
# valid, the build still writes, the probe still passes its four asserts, and the damage
# only exists in a render nobody has looked at yet. The first one took the jaw and both
# ears; the second reached the cheeks.
#
# So the selection is checked against the geometry rather than trusted:
#
#   * the counts are exact. Eight mouth triangles over ten vertices, twelve eye triangles
#     over sixteen, measured off this donor. A selection that has widened by even one
#     triangle stops the build.
#   * NOTHING outside slot 8 may move, at all. That is the whole class the jaw fell into:
#     slot 15 is the skull, the jaw, the chin and both ears, and no expression has any
#     business touching a vertex of it.
#   * the skull's bounds must be identical before and after, which catches a move of zero
#     length in the count but non-zero in the mesh.
#
# ⚠️ IT COMPARES A COPY TAKEN BEFORE THE EDIT, not the file on disk. Re-reading the donor
# would pass trivially if the edit were applied twice.
def _verify_expression(before, after, uv, moved):
    """Refuses the build unless the expression moved exactly what it was allowed to."""
    changed = {i for i in range(len(before)) if before[i] != after[i]}

    if changed != moved:
        raise SystemExit(
            f"\nEXPRESSION SELECTION VIOLATION - nothing written.\n"
            f"  {len(changed)} vertices moved, {len(moved)} were selected to move.\n"
            f"  Strays: {sorted(changed - moved)[:12]}\n"
            f"  An expression edit that moves anything it did not pick is how the jaw and\n"
            f"  both ears were lost, and it fails silently every time.")

    stray = {i for i in changed if _slot_at(*uv[i]) != INK}

    if stray:
        raise SystemExit(
            f"\nEXPRESSION SELECTION VIOLATION - nothing written.\n"
            f"  {len(stray)} moved vertices are not in slot {INK} (the ink).\n"
            f"  Slot 15 is the skull, the jaw, the chin and both ears. No expression\n"
            f"  touches it. See the note above `_donor_head`.")

    keep = [i for i in range(len(before)) if _slot_at(*uv[i]) != INK]

    for axis in range(3):
        was = (min(before[i][axis] for i in keep), max(before[i][axis] for i in keep))
        now = (min(after[i][axis] for i in keep), max(after[i][axis] for i in keep))

        if was != now:
            raise SystemExit(
                f"\nEXPRESSION SELECTION VIOLATION - nothing written.\n"
                f"  the skull's axis {axis} bounds moved from {was} to {now}.\n"
                f"  Nothing but the ink may change shape when the expression does.")


# ---------------------------------------------------------------------------
# § EXPRESSION: Wide Happy Cat (:3)
# ---------------------------------------------------------------------------
MOUTH_Z = 0.1596


def _mouth_ribbon():
    half_w = 0.046
    base_y = 0.418
    lobe_depth = 0.0215
    th = 0.0135
    steps = 32
    upper, lower = [], []
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
    return upper, lower


EYE_SCALE = 1.08


SKULL_SLOTS = {15: SKIN, 8: INK}


def _donor_head():
    pos, nrm, uv, tris = _donor_part(DONOR_SKULL, SKULL_SLOTS)
    mouth, eyes, mouth_tris = set(), set(), set()
    for a, b, c in tris:
        if _slot_at(*uv[a]) != INK:
            continue
        if (pos[a][1] + pos[b][1] + pos[c][1]) / 3.0 < 0.45:
            mouth.update((a, b, c))
            mouth_tris.add((a, b, c))
        else:
            eyes.add((a, b, c))

    eye_verts = {i for tri in eyes for i in tri}
    before = list(pos)

    for side in (1.0, -1.0):
        lid = {i for tri in eyes for i in tri if pos[i][0] * side > 0.0}
        if not lid:
            continue
        centre_y = sum(pos[i][1] for i in lid) / len(lid)
        for i in lid:
            x, y, z = pos[i]
            pos[i] = (x, centre_y + (y - centre_y) * EYE_SCALE, z)

    _verify_expression(before, pos, uv, eye_verts)

    tris = [t for t in tris if t not in mouth_tris]
    plate = MOUTH_Z + PANEL_PROUD

    upper, lower = _mouth_ribbon()
    steps = len(upper) - 1

    # Add vertices for ribbon: upper[0..steps] then lower[0..steps]
    u_base = len(pos)
    for x, y in upper:
        pos.append((x, y, plate))
        nrm.append((0.0, 0.0, 1.0))
        uv.append(cell_uv(INK))

    l_base = len(pos)
    for x, y in lower:
        pos.append((x, y, plate))
        nrm.append((0.0, 0.0, 1.0))
        uv.append(cell_uv(INK))

    # Clean quad-strip triangulation (Counter-Clockwise facing +Z)
    for k in range(steps):
        u0 = u_base + k
        u1 = u_base + k + 1
        l0 = l_base + k
        l1 = l_base + k + 1
        tris.append((u0, l0, u1))
        tris.append((u1, l0, l1))

    return _compact(pos, nrm, uv, tris)


def _compact(pos, nrm, uv, tris):
    """Drops vertices nothing references any more, and reindexes what is left.

    ⚠️ IT IS NOT AN OPTIMISATION, IT IS SO THE FILE CAN BE MEASURED. An orphaned vertex
    still sits in the POSITION accessor, so `glb_mesh_dump.py`, `glb_face_side.py` and
    every bounds check in `verify()` keep reading geometry that is not drawn. The deleted
    mouth is exactly the shape whose absence is being checked.
    """
    used = sorted({i for t in tris for i in t})
    remap = {i: k for k, i in enumerate(used)}

    return ([pos[i] for i in used], [nrm[i] for i in used], [uv[i] for i in used],
            [tuple(remap[i] for i in t) for t in tris])

    return pos, nrm, uv, tris

# ---------------------------------------------------------------------------
# Geometry.
# ---------------------------------------------------------------------------

# ⚠️ THE TABLES ARE AUTHORED FACING -Z AND THE FILE'S FRONT IS +Z. The conversion is a
# NEGATION WITH THE TWO Z BOUNDS SWAPPED, so every box stays well formed and the face
# winding below stays outward. Mirroring without the swap turns every normal inside out
# and the character renders as a hole.
FRONT_IS_MINUS_Z = True

# How far a pixel panel stands off the surface it is drawn on, in model space. Under a
# millimetre once `CharacterVisual.PersonScale` has multiplied it by 2.38. See the panel
# loop in `build_mesh`.
PANEL_PROUD = 0.0006

# Unit cube corners per face, and the face normal. Every box face is flat and gets its
# own four vertices, which is what makes the shading read as voxel facets rather than
# as a smoothed blob.
FACES = [
    ((0, 0, -1), [(0, 0, 0), (0, 1, 0), (1, 1, 0), (1, 0, 0)]),
    ((0, 0, 1), [(1, 0, 1), (1, 1, 1), (0, 1, 1), (0, 0, 1)]),
    ((-1, 0, 0), [(0, 0, 1), (0, 1, 1), (0, 1, 0), (0, 0, 0)]),
    ((1, 0, 0), [(1, 0, 0), (1, 1, 0), (1, 1, 1), (1, 0, 1)]),
    ((0, 1, 0), [(0, 1, 0), (0, 1, 1), (1, 1, 1), (1, 1, 0)]),
    ((0, -1, 0), [(0, 0, 1), (0, 0, 0), (1, 0, 0), (1, 0, 1)]),
]


# Which entry of FACES a name refers to, for boxes that leave one out.
#
# ⚠️⚠️ THE NAMES ARE RESOLVED AFTER THE `FRONT_IS_MINUS_Z` FLIP, NOT BEFORE IT, AND GETTING
# THAT BACKWARDS IS WHY THE FACE WAS SHREDDED. 🧑 *"zach in char select is buggy, his face
# specifically is weird af"*.
#
# The tables are authored facing -Z and the flip NEGATES AND SWAPS each box's z bounds, so
# the authored front wall (the lower z) comes out as the box's UPPER z. `FACES[0]` is the
# lower-z quad and `FACES[1]` the upper one, so under the flip "front" is entry 1 — while
# this table said 0. The skull therefore kept the wall it was supposed to lose and lost the
# one behind it, which put a full skin-coloured quad in EXACTLY the plane `FACE_PIXELS`
# draws into.
#
# Two opaque surfaces in one plane is z-fighting, and z-fighting is resolved per fragment
# by depth precision rather than by anything stable: head on it happened to land mostly on
# the panel, which is why the front-only turnaround looked correct and passed every
# assertion, and at the three-quarter angle the character screen actually uses it tore each
# eye and the smile into triangular shards. The whole point of the panel is that the wall
# it replaces is GONE ("no z-fighting, because there is nothing to fight with").
#
# ⚠️ NOTE THAT left/right WERE ALREADY SWAPPED HERE and front/back were not, which is what
# hid this: the table looks like it has been through exactly this correction once.
_FACE_NAMES = {"front": 0, "back": 1, "left": 3, "right": 2, "top": 4, "bottom": 5}

SKIPPABLE = dict(_FACE_NAMES)

if FRONT_IS_MINUS_Z:
    SKIPPABLE["front"], SKIPPABLE["back"] = _FACE_NAMES["back"], _FACE_NAMES["front"]


# ---------------------------------------------------------------------------
# § THE CHAMFER.
#
# ⚠️⚠️ 🧑 2026-08-18, looking at the finished model: *"can we make zack a little less
# blocky and more like the original models? he's giving minecraft now haha"*. He is
# describing a real difference and not a tuning one. Every other person in this cast is
# a Kenney mini, and a Kenney mini has NO 90-degree silhouette edge on it: the head is
# an ovoid, the limbs are capsules with rounded ends, and the shading rolls round every
# corner instead of stopping dead at it. This character was built from axis-aligned
# cuboids, so every edge in its outline is a right angle, and a stack of right-angled
# cuboids in a palette is the Minecraft read whatever colours you put on it.
#
# The fix is to CUT THE EDGES, not to round the forms. A chamfered cuboid keeps the
# voxel language — flat facets, one palette slot per box, the same table above unchanged
# — while removing exactly the thing that reads as Minecraft, which is the hard 90.
#
# ⚠️ AND IT COMPOUNDS WITH `smooth_normals`, WHICH IS WHY IT IS WORTH SO LITTLE
# GEOMETRY. That function averages the facet normals meeting at a position. On a plain
# cuboid three faces meet at 90 degrees and the average is a corner normal that shades as
# a hard crease; with a chamfer there are now two intermediate facets between them, so
# the same averaging produces a genuine gradient round the edge. The character shades
# like the rest of the cast because it now has somewhere to shade.
#
# ⚠️ IT ALSO FIXES THE OUTLINE FOR FREE, for the reason `smooth_normals` documents at
# length: the inverted hull pushes along the normal, and a smoother normal field round
# an edge is a hull that closes rather than tearing.
#
# ⚠️⚠️ A BOX THAT SKIPS A FACE IS LEFT SQUARE, AND THAT IS DELIBERATE. The skull drops
# its front wall so `FACE_PIXELS` can draw into that exact plane, and the panel is a full
# rectangle. Chamfering the skull would shrink the hole to an octagon and leave the
# corners of the face panel hanging in space outside it — the same class of fault as the
# z-fighting `SKIPPABLE` was written to fix, arrived at from the other side. Four boxes
# carry a skip; they keep their corners and nobody can see them, because the only skipped
# face on the model is the one the face is drawn on.
#
# ⚠️ THE SIZE IS PROPORTIONAL, WITH A CEILING. A flat 20 mm cut is most of a chain link
# and nothing at all on the torso, so it is a fraction of the box's own smallest half
# extent, capped so the large masses do not turn into gems. The cap is what keeps the
# skull a head.
# ⚠️⚠️ 0.45 IS NEARLY A CAPSULE AND THAT IS THE POINT. 🧑 2026-08-18: *"still to blocky
# btw"*. At 0.34 a limb 124 mm thick got a 21 mm cut — enough to kill the hard 90 and not
# enough to read as ROUND, and round is what the cast is.
#
# ⚠️ IT MUST STAY BELOW 0.5. The bevel is measured from each corner inward, so at half the
# extent opposing cuts meet and the box turns inside out. The fraction IS the clamp.
BEVEL_FRACTION = 0.45

# ⚠️⚠️ THE CAP IS WHAT THE BIG MASSES HIT, AND 0.030 LEFT THE JAW A CORNER. 🧑 2026-08-18,
# after the first chamfer pass: *"the face itself as well is too sharp, look chin and
# stuff"*. Only the largest boxes reach the cap at all — the skull, the hair crown, the
# jacket — and those are exactly the ones whose silhouette is the character. At 0.030 the
# skull was cut by 18% of its smallest half extent, which rounds a chain link nicely and
# barely touches a head 335 mm across. At 0.045 it is fraction-limited like everything
# else and the chin actually turns.
BEVEL_MAX = 0.060


def bevel_for(lo, hi):
    """How far to cut this box's edges, or 0 to leave it square."""
    smallest = min((hi[i] - lo[i]) * 0.5 for i in range(3))

    # Below this a box is a detail plate a couple of millimetres thick, and a chamfer on
    # it is smaller than the ink outline that will be drawn round it: pure cost.
    if smallest < 0.004:
        return 0.0

    return min(BEVEL_MAX, smallest * BEVEL_FRACTION)


def _ring(points, normal):
    """The points of one planar face, ordered around `normal` so the winding is outward.

    ⚠️ SORTED RATHER THAN TABULATED. A chamfered cuboid has 26 faces and three different
    face kinds, and a hand-written winding table for that is 26 chances to draw one
    polygon inside out — which renders as a hole in the model, not as an error. Sorting
    by angle in the face's own plane and then orienting against the outward normal is
    correct by construction for all three kinds at once.
    """
    cx = sum(p[0] for p in points) / len(points)
    cy = sum(p[1] for p in points) / len(points)
    cz = sum(p[2] for p in points) / len(points)

    # Any two axes perpendicular to the normal will do; pick the world axis least
    # aligned with it so the cross product is well conditioned.
    least = min(range(3), key=lambda i: abs(normal[i]))
    helper = [0.0, 0.0, 0.0]
    helper[least] = 1.0

    u = _cross(helper, normal)
    u = _unit(u)
    v = _unit(_cross(normal, u))

    def angle(p):
        d = (p[0] - cx, p[1] - cy, p[2] - cz)
        return math.atan2(_dot(d, v), _dot(d, u))

    ordered = sorted(points, key=angle)

    # Counter-clockwise about `normal` is what Unity and glTF call front-facing, and the
    # sort above produces exactly that for a right-handed (u, v, normal) frame.
    return ordered


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
    """The faces of one box: six quads square, or twenty-six chamfered.

    Yields (normal, [points]) with the points already wound outward.
    """
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

    # Three vertices per original corner, each pulled in along one axis. This is the
    # standard chamfered cuboid: 24 vertices, 6 octagons, 12 edge quads, 8 corner tris.
    vertex = {}
    for s in signs:
        for axis in range(3):
            p = [centre[i] + s[i] * half[i] for i in range(3)]
            p[axis] = centre[axis] + s[axis] * (half[axis] - bevel)
            vertex[(s, axis)] = tuple(p)

    # The six original faces, now octagons.
    for axis in range(3):
        for sgn in (-1, 1):
            normal = [0.0, 0.0, 0.0]
            normal[axis] = float(sgn)

            points = [vertex[(s, other)]
                      for s in signs if s[axis] == sgn
                      for other in range(3) if other != axis]

            yield (tuple(normal), _ring(points, tuple(normal)))

    # The twelve edge chamfers, one per pair of faces.
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

    # The eight corner triangles.
    for s in signs:
        normal = _unit((float(s[0]), float(s[1]), float(s[2])))
        yield (normal, _ring([vertex[(s, 0)], vertex[(s, 1)], vertex[(s, 2)]], normal))


def build_mesh(boxes, panels=(), donor=None):
    """Boxes, pixel panels and an optional donated mesh, to flat glTF arrays."""
    pos, nrm, uv, joints, weights, idx = [], [], [], [], [], []

    # Which vertices came from a pixel panel rather than from a box. See smooth_normals:
    # they are held out of the averaging in both directions.
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

        # ⚠️ A BOX THAT DROPS A FACE STAYS SQUARE. See the chamfer block: the face panel
        # is a full rectangle drawn into the plane of the wall this removes, and an
        # octagonal hole leaves its corners outside the model.
        bevel = 0.0 if skip >= 0 else bevel_for(lo, hi)

        for normal, points in box_polygons(lo, hi, skip, bevel):
            first = len(pos)

            for p in points:
                pos.append(p)
                nrm.append(normal)
                uv.append((u, v))
                joints.append((j, 0, 0, 0))
                weights.append((1.0, 0.0, 0.0, 0.0))

            # ⚠️ A FAN, BECAUSE THE FACES ARE NO LONGER ALL QUADS. Every polygon here is
            # planar and convex — an octagon, a rectangle or a triangle — so a fan from
            # its first vertex is exact rather than an approximation.
            for k in range(1, len(points) - 1):
                idx += [first, first + k, first + k + 1]

    for name, bone, low, high, plane, rows in panels:
        j = BONE[bone]

        cols = len(rows[0])
        cell_x = (high[0] - low[0]) / cols
        cell_y = (high[1] - low[1]) / len(rows)

        # ⚠️ A HAIR IN FRONT OF THE SKULL, NOT IN ITS PLANE. The skull keeps its front wall
        # now (see its own note), so the features sit ON the face instead of filling a hole
        # in it. PANEL_PROUD is under a millimetre before the 2.38 person scale, which is
        # far too little to read as a raised object and far more than the depth buffer needs
        # to keep the two apart.
        z = (-plane + PANEL_PROUD) if FRONT_IS_MINUS_Z else (plane - PANEL_PROUD)
        normal = (0.0, 0.0, 1.0) if FRONT_IS_MINUS_Z else (0.0, 0.0, -1.0)

        for r, row in enumerate(rows):
            if len(row) != cols:
                raise SystemExit(f"panel '{name}' row {r} is {len(row)} wide, not {cols}")

            for c, mark in enumerate(row):
                # ⚠️⚠️ ONLY THE INK CELLS ARE DRAWN NOW. Every other cell used to emit a
                # SKIN quad, which was necessary while this grid was FILLING a hole in the
                # skull — the hole had to be covered edge to edge or the head had a window
                # in it. The skull keeps its front wall since the chamfer pass, so a skin
                # cell here is skin drawn on skin: 90% of this panel was overdraw, and on a
                # chamfered skull its rectangular corners would have hung off the octagon.
                if mark != "X":
                    continue

                u, v = cell_uv(INK)

                # Rows read top down, so row 0 is the TOP of the rectangle.
                x0 = low[0] + cell_x * c
                y0 = high[1] - cell_y * (r + 1)

                quad = [(x0, y0), (x0, y0 + cell_y),
                        (x0 + cell_x, y0 + cell_y), (x0 + cell_x, y0)]

                # ⚠️ WOUND TO FACE THE SAME WAY THE BOX FACE IT REPLACES DID. Reversed, it
                # is back-face culled and the head has a hole where the face should be,
                # which reads as the model failing to import rather than as a winding bug.
                if FRONT_IS_MINUS_Z:
                    quad = list(reversed(quad))

                first = len(pos)

                for x, y in quad:
                    panel_indices.append(len(pos))
                    pos.append((x, y, z))
                    nrm.append(normal)
                    uv.append((u, v))
                    joints.append((j, 0, 0, 0))
                    weights.append((1.0, 0.0, 0.0, 0.0))

                idx += [first, first + 1, first + 2, first, first + 2, first + 3]

    # § THE DONATED HEAD. See `_donor_head`.
    #
    # ⚠️⚠️ ITS VERTICES ARE HELD OUT OF `smooth_normals`, and that is not an optimisation.
    # The donor arrives with its own authored normals — it is a smooth low-poly head and
    # those normals are what make it read as one. Averaging them against the hair boxes now
    # sitting on top would drag the crown's shading toward the mop and crease a surface that
    # has none.
    if donor is not None:
        dpos, dnrm, duv, dtris = donor

        j = BONE["head"]
        base = len(pos)

        for i in range(len(dpos)):
            panel_indices.append(len(pos))
            pos.append(tuple(dpos[i]))
            nrm.append(tuple(dnrm[i]))
            uv.append(tuple(duv[i]))

            # ⚠️ BOUND RIGIDLY TO THE HEAD JOINT rather than carried across from the donor's
            # own skin. A Kenney head is rigid on that one bone, and a joint INDEX is per
            # file: copying the donor's would bind this head to whatever bone sits at that
            # index in the rig being written.
            joints.append((j, 0, 0, 0))
            weights.append((1.0, 0.0, 0.0, 0.0))

        for a, b, c in dtris:
            idx += [base + a, base + b, base + c]

    return pos, smooth_normals(pos, nrm, panel_indices), uv, joints, weights, idx


def smooth_normals(pos, nrm, panel_indices=()):
    """Averages the face normals meeting at each position, in place of the flat ones.

    ⚠️⚠️ THIS IS WHAT MAKES THE MODEL LOOK LIKE THE REST OF THE CAST, AND THE REASON IS
    THE OUTLINE RATHER THAN THE SHADING. `Toon.shader`'s outline is an INVERTED HULL: it
    pushes every vertex along its normal and draws the back faces. With one hard normal
    per face, the eight vertices at a box corner push in six different directions, the
    hull tears open at every edge, and what should be a thick continuous border comes out
    as a thin broken one. Kenney's rigs ship smoothed normals, which is why theirs closes
    and the first build of this one did not.
    🧑: *"GIVE it the same toon vibe as well as my other shi"*.

    ⚠️ AND IT COSTS NOTHING THAT MATTERS. The voxel read comes from the SILHOUETTE and
    from each face being one flat palette colour, neither of which normals touch. What
    changes is that the two lighting bands now fall across a box instead of stopping at
    its edges, which is the soft gradient every other character already has, and which
    the reference art has too.

    ⚠️ THE VERTICES ARE STILL SPLIT PER FACE. They have to be: a face declares its palette
    slot through its UV, so merging them would merge their colours as well as their
    normals. Only the normal is shared.

    ⚠️⚠️ THE FACE PANEL IS HELD OUT OF THIS ENTIRELY, IN BOTH DIRECTIONS, AND INCLUDING IT
    IS WHAT ATE THE FACE. 🧑 *"zach in char select is buggy, his face specifically is weird
    af"*, with the eyes reduced to diagonal slashes and the smile growing a tooth.

    The skull is emitted WITHOUT its front face so the panel can BE that face, which means
    its hull is open there and the only vertices on the front plane belong to the sides,
    the top and the bottom. Averaging those with the panel's own +Z pulls them FORWARD, so
    the outline pass — an inverted hull that pushes along exactly these normals — grows a
    black frame that leans in OVER the front plane instead of standing off the sides of the
    head. Its inner edge follows whatever the averaging produced, which is where the
    diagonal slashes come from, and it scales with the outline width: at the doubled width
    `ModelPreview` was passing it covered most of the face, and at the correct width it
    still cuts across the eyes.

    Held out, the skull's border vertices average only among the skull's own side, top and
    bottom faces, all of which point AWAY from the front plane, so the frame pushes
    outward as a silhouette border should. The panel keeps its flat +Z, which is what it
    wants anyway: it is one plane, there is nothing for it to be smoothed against, and its
    triangles face the viewer so the outline pass culls them outright.

    ⚠️ IT IS EXCLUDED AS A CONTRIBUTOR *AND* AS A CONSUMER. Doing only the second leaves
    the panel's +Z in the buckets still tilting every skull vertex it shares a corner with,
    which is the half that actually draws the frame.
    """
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

        # A vertex whose neighbours cancel out exactly, which happens where two boxes meet
        # face to face, keeps its own normal rather than becoming a zero vector.
        out.append(tuple(n[a] / length for a in range(3)) if length > 1e-6 else nrm[i])

    return out


# ---------------------------------------------------------------------------
# glb read / write.
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
    """An accessor's data as tightly packed bytes, de-interleaving if it was strided."""
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


# ---------------------------------------------------------------------------

def retarget(gltf, buffer):
    """Moves the bones to SKELETON and corrects the clips that key them.

    Returns {node index: local translation delta}, which the animation rewrite below
    applies to every translation track those nodes own.

    ⚠️ THE DELTA IS ON THE LOCAL TRANSLATION, NOT THE WORLD ONE, because that is what a
    track holds. A bone whose parent also moved has already inherited the parent's
    shift, so subtracting the new parent world position is what stops the two being
    counted twice: an early version added the world delta and put the head 10 cm above
    the shoulders it was attached to.
    """
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
    """Fresh inverse bind matrices for the retargeted skeleton.

    ⚠️ RECOMPUTED RATHER THAN PATCHED, and it is only this simple because the rest pose
    carries no rotation and no scale on any node, which was measured off the base file
    rather than assumed. For a pure translation, the inverse bind matrix is the inverse
    translation, column major with the position in elements 12 to 14.
    """
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

    # § THE FAMILY PASS, applied to the authored tables on the way into the mesh. See
    # `_family`: the tables stay as measured and the REGIONS move onto the base rig's own
    # proportions, so this character stands in the line-up as one of the cast.
    body = build_mesh(_family(BODY_BOXES, head=False))
    head = build_mesh(_family(HEAD_BOXES, head=True, as_authored=DONOR_SPACE),
                      donor=_donor_head())

    # ⚠️ EVERY RETAINED ACCESSOR IS REPACKED INTO A FRESH BUFFER rather than the old one
    # being patched. The base carries one bufferView per accessor, so a rebuild is a
    # straight copy with new offsets, and it drops the old mesh data instead of leaving
    # 90 KB of orphaned vertices in the file.
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

            # ⚠️ ONLY TRANSLATION TRACKS SHIFT. A rotation is about the bone's own
            # origin and moving that origin does not change the rotation; rewriting one
            # would be corrupting the animation to fix a problem it does not have.
            if channel["target"]["path"] != "translation" or delta is None \
                    or delta == (0.0, 0.0, 0.0):
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

    # ⚠️ THE ROOT NODE AND THE SCENE CARRY THE BASE RIG'S NAME AND MUST NOT. `ModelPreview`
    # and the roster sheet both title a preview off the instanced object's name, so
    # leaving it puts "character-female-b" under the portrait of a model that no longer
    # is one. The SEVEN BONE NAMES below are NOT renamed: the clips address them by name
    # and the hand anchor and the wind-up pose both hunt `arm-right` by string.
    stem = os.path.splitext(os.path.basename(OUT))[0]
    gltf["nodes"][0]["name"] = stem
    gltf["scenes"][0]["name"] = stem

    print(f"retargeted {len(deltas)} bones, shifted {moved} translation tracks")

    verify(body, head)
    write_glb(OUT, gltf, blob)
    write_palette(PALETTE_OUT)


# ---------------------------------------------------------------------------

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

    # ⚠️⚠️ THE CEILING IS THE CAST'S RANGE, NOT THE BASE RIG'S ONE NUMBER, AND THAT
    # CORRECTION IS WHAT LET THIS CHARACTER HAVE HAIR. This check read
    # `abs(height - 0.7234) > 0.002` and refused anything else, on the reasoning that
    # `CharacterVisual.PersonScale` is a single constant of 2.38 for all twelve so a
    # replacement authored taller "walks the arena at the wrong size".
    #
    # The constant is real and the conclusion from it was not. Measured across the twelve
    # CC0 rigs the port actually ships, model AABB height:
    #
    #     male-b   0.6613     male-a  0.6713     male-e   0.6760     female-e 0.7165
    #     female-f 0.6713     male-f  0.6713     male-d   0.7218     female-b 0.7234
    #     female-a 0.7755     female-c 0.7755    female-d 0.7755     male-c   0.7928
    #
    # They span 132 mm, a fifth of the shortest, and they all take the same 2.38.
    # `CharacterVisual.AlignToCapsuleFloor` re-measures the SCALED bounds and drops the feet
    # onto the capsule floor, so a taller rig stands taller with its feet in the right place
    # rather than sinking or floating. 0.7234 was one member of that range and pinning to it
    # was a transcription of the base rig rather than a family constraint.
    #
    # ⚠️ WHY IT MATTERED. The whole difference between those two ends is HAIR. A bald rig is
    # 0.66 and a rig with a mop is 0.78. Holding this character at the base's 0.7234 while
    # its donated skull already reached 0.7218 left 1.6 mm for hair, which is how four
    # sessions of hand-built cap ended up as a slab floating over a forehead. See the note
    # above `HEAD`.
    #
    # The bound is still a bound: outside the cast's own range this is a mistake, not a
    # style. It is widened by 5 mm at each end so a rig at either extreme can be matched.
    if not (CAST_MIN_HEIGHT - 0.005 <= height <= CAST_MAX_HEIGHT + 0.005):
        raise SystemExit(
            f"\nHEIGHT CONSTRAINT VIOLATION - nothing written.\n"
            f"  authored height {height:.4f}, the twelve CC0 rigs span "
            f"{CAST_MIN_HEIGHT:.4f} to {CAST_MAX_HEIGHT:.4f}.\n"
            f"  CharacterVisual.PersonScale multiplies every Person by 2.38, so a rig\n"
            f"  authored outside that range walks the arena at the wrong size.")

    if abs(lo[1]) > 0.001:
        raise SystemExit(f"feet are at y={lo[1]:.4f}, not 0. The floor align measures "
                         "bounds, but the bind pose should still stand on zero.")

    # ⚠️ EVERY BOX MUST BE INSIDE ITS OWN BONE'S REACH, or the limb tears when the clip
    # rotates it. A box hung off the wrong bone is the single easiest mistake to make in
    # a table this long and it is invisible until something moves.
    #
    # ⚠️⚠️ IT CHECKS THE REMAPPED TABLES AGAINST THE REMAPPED SKELETON, NOT THE AUTHORED
    # ONES. `_family` moves the boxes and `SKELETON` moves the bones, so comparing the
    # authored table to the new joint heights measures a distance that exists in neither
    # the file nor the model — it fired on `hair-fringe` the first time this ran, for a
    # box that had not moved relative to its own bone at all.
    #
    # ⚠️ AND THE BOUND IS PER BONE, because the head is now 53% of the figure. A crown box
    # is legitimately 0.38 from the head joint on a rig built to these proportions, which
    # a flat 0.30 calls a mistake. The head's bound is its own height plus a margin; every
    # other bone keeps the original number.
    # ⚠️ THE HEAD'S BOUND COMES OFF THE CAST'S TALLEST RIG, NOT OFF `NOW_TOP`. Same
    # correction as the height check above and for the same reason: `NOW_TOP` is the base
    # rig's own total, and a mop is legitimately taller than the base rig is. `character-
    # male-c` reaches 0.7928 with the head joint at 0.343, so 0.450 is a real box on a real
    # character, and this fired on `hair-curl-b` at 0.439 for a crown lump that is exactly
    # where the reference art puts it. 5% of margin over the tallest thing the cast ships.
    head_reach = (CAST_MAX_HEIGHT - NOW_NECK) * 1.05

    for entry in (_family(BODY_BOXES, head=False)
                  + _family(HEAD_BOXES, head=True, as_authored=DONOR_SPACE)):
        name, bone, box_lo, box_hi, slot = entry[:5]
        origin = SKELETON[bone][1]
        reach = max(abs(box_lo[1] - origin), abs(box_hi[1] - origin))

        if reach > (head_reach if bone == "head" else 0.30):
            raise SystemExit(f"box '{name}' is {reach:.3f} from the {bone} bone. "
                             "It is almost certainly on the wrong bone.")

        if slot not in PALETTE:
            raise SystemExit(f"box '{name}' uses palette slot {slot}, which is not set.")


def rgb(hex_str):
    h = hex_str.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def write_palette(path):
    """Emits the Godot `.tres` that `RosterBookBuilder` reads the sixteen colours from.

    ⚠️ IT IS A `.tres` BECAUSE THAT IS THE FORMAT THE UNITY SIDE ALREADY PARSES, not
    because Godot will ever load this one. `RosterBookBuilder.ReadPalette` reads
    `MapSource/materials_persons/` with a regex over `PackedColorArray`, which is how the
    other eleven characters get their colours across from the original build. A second
    format for the one character authored here would be a second parser.

    ⚠️ AND THIS ONE IS NOT A COPY OF A GODOT FILE. The others in that folder are carried
    over and are generated there by `tools/models/generate_person_palettes.py`; do not
    hand-edit those. This model does not exist in the Godot build, so its palette is
    generated HERE, by the same script that lays out the UVs it belongs to.
    """
    r, g, b = rgb(PALETTE[8])
    lum = 0.2126 * r + 0.7152 * g + 0.0722 * b

    if lum > MAX_FACE_LUMINANCE:
        raise SystemExit(
            f"\nFACE CONSTRAINT VIOLATION - nothing written.\n"
            f"  slot 8 is #{PALETTE[8]} (luminance {lum:.2f} > {MAX_FACE_LUMINANCE:.2f}).\n"
            f"  Slot 8 draws the eyes, brows and mouth. A light slot 8 does not give a\n"
            f"  light-haired character, it gives one with no face.")

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
