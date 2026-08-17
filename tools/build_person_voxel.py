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
import json
import os
import struct
import sys

BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-b.glb"

# ⚠️ IT SITS BESIDE THE CC0 RIGS RATHER THAN IN A `team/` SUBFOLDER, and the reason
# is one line of the file: the material points at `Textures/colormap.png` as a
# RELATIVE uri, which glTFast resolves against the .glb's own directory. A subfolder
# breaks that silently, and the symptom is a character rendered in flat white with no
# error, because a missing texture is not an import failure. The `team-` prefix is
# what marks it as ours; `character-*` are the CC0 ones.
OUT = "Assets/TumbangPreso/Art/characters/persons/team-ate-girlie.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-ate-girlie.tres"

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
#   hips     0.176   legs 24% of height        0.240   legs 33%
#   shoulder 0.288                             0.452
#   neck     0.343   torso 23%, head 53%       0.505   torso 37%, head 30%
#
# ⚠️ THE TARGET IS THE REFERENCE ART, WHICH IS ROUGHLY MINECRAFT PROPORTIONED at
# 37 / 37 / 25. This lands at 33 / 37 / 30 rather than matching it outright, and the
# remaining 5% of head is deliberate: the legs are the one measurement that costs
# animation quality to change, so they are grown by 36% and no further.
# ---------------------------------------------------------------------------
SKELETON = {
    "root":      (0.0,      0.0,   0.0),
    "leg-left":  (0.08357,  0.240, -0.02875),
    "leg-right": (-0.08357, 0.240, -0.02875),
    "torso":     (0.0,      0.240, -0.02875),
    "arm-left":  (0.0999,   0.452, -0.01725),
    "arm-right": (-0.0999,  0.452, -0.01725),
    "head":      (0.0,      0.505, -0.00236),
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

JACKET, CLOTH, CLIP, GOLD = 0, 1, 2, 3
SHOE, CHAIN, HAIR, JACKET_DARK = 4, 5, 6, 7
INK, WHITE, SKIN, SKIN_DARK, SKIN_LIT = 8, 12, 13, 14, 15

# ⚠️⚠️ THE PALETTE IS AUTHORED IN THIS FILE, BESIDE THE UVs, AND SPLITTING THE TWO IS
# THE ONE CHANGE THAT WOULD BREAK THIS CHARACTER SILENTLY. A box says "I am slot 0" by
# where its UV lands; the palette says what slot 0 IS. Keep them in two files and a
# slot renumbered on one side repaints a limb on the other, with nothing to fail: the
# model still imports, still animates, and simply wears the wrong colours.
#
# Slots 9, 10 and 11 keep the stock Kenney values. Nothing here samples them, and
# leaving them stocked means the shader has something sane to read if a future box does.
PALETTE = {
    JACKET:      "7a34c4",   # the open jacket, and the pendant that matches it
    CLOTH:       "1a1a20",   # shirt, pants, belt
    CLIP:        "e8306b",   # the bow
    GOLD:        "f2c230",   # belt buckle
    SHOE:        "a63fd9",   # sneaker uppers
    CHAIN:       "b8bcc4",   # necklace and hip chain
    HAIR:        "141018",
    JACKET_DARK: "4e1e80",   # collar and cuffs, the jacket's own shadow
    INK:         "1b1b20",   # ⚠️ THE FACE. Must stay dark, see the module docstring.
    9:           "868ba1",
    10:          "4f5260",
    11:          "a0a8c9",
    WHITE:       "ffffff",   # sneaker soles
    SKIN:        "e08a3c",
    SKIN_DARK:   "a85a22",
    SKIN_LIT:    "f0a85a",   # forearms and hands
}

# The Godot generator aborts its build on this and so does this one. Slot 8 draws the
# eyes, brows and mouth, and a light slot 8 does not give a light-haired character, it
# gives one with no face.
MAX_FACE_LUMINANCE = 0.30


def mirrored(boxes, bone_from, bone_to):
    """The same parts on the other side, with X negated and the bone renamed.

    ⚠️ A MIRROR RATHER THAN A SECOND TABLE. Two hand-authored halves drift, and a limb
    2 mm wider on one side reads as a bug in the rig rather than as a typo in a list.
    """
    out = []
    for name, bone, lo, hi, slot in boxes:
        assert bone == bone_from, f"{name} is on {bone}, not {bone_from}"
        out.append((name.replace("left", "right"), bone_to,
                    (-hi[0], lo[1], lo[2]), (-lo[0], hi[1], hi[2]), slot))
    return out


# Hips at 0.240, so the leg owns everything below it.
LEG_LEFT = [
    # ⚠️ THE SOLE IS THE THICKEST BAND ON THE SHOE, and it is white against a black
    # upper because that contrast is the only part of the footwear that survives being
    # seen from across the box.
    ("shoe-sole-left",   "leg-left", (0.006, 0.000, -0.134), (0.158, 0.024, 0.082), WHITE),
    ("shoe-upper-left",  "leg-left", (0.014, 0.024, -0.126), (0.152, 0.056, 0.076), CLOTH),
    ("shoe-collar-left", "leg-left", (0.010, 0.056, -0.100), (0.156, 0.076, 0.080), SHOE),
    ("shoe-toe-left",    "leg-left", (0.010, 0.024, -0.132), (0.156, 0.042, -0.100), WHITE),
    ("pant-left",        "leg-left", (0.018, 0.068, -0.072), (0.150, 0.240, 0.072), CLOTH),

    # The hip chain hangs off the belt and down the outside of the thigh, so it rides
    # the LEG bone rather than the torso: on the torso it would swing with the body
    # while the leg it lies against walked out from under it.
    ("chain-hip-left",   "leg-left", (0.148, 0.130, -0.052), (0.168, 0.240, 0.034), CHAIN),
    ("chain-drop-left",  "leg-left", (0.144, 0.086, -0.058), (0.164, 0.136, -0.012), CHAIN),
    ("chain-front-left", "leg-left", (0.040, 0.146, -0.084), (0.150, 0.174, -0.070), CHAIN),
]

# Waist at 0.240, shoulders at 0.452, neck at 0.505.
TORSO = [
    # ⚠️⚠️ EVERY DETAIL STANDS PROUD OF WHAT IT SITS ON, BY AT LEAST 12 mm BEFORE THE
    # 2.38 SCALE, and that is the difference between this reading as a voxel character
    # and reading as a printed texture. 🧑 on the first pass: *"make sure u dnt just
    # paste the texture"*. Depth is what a flat map cannot fake, and the inverted-hull
    # ink outline in `ToonSkin` draws a border around each step, so a detail that pops
    # gets its own outline for free. A buckle flush with the belt gets neither, and at
    # arena distance it stops existing.
    #
    # ⚠️ THE JACKET IS WIDE AND THE SHIRT IS A NARROW STRIP DOWN THE MIDDLE, which is
    # the read in the reference and the opposite of the first pass. With narrow lapels
    # the character was a black torso with purple piping, which at arena distance is a
    # black torso.
    ("shirt",         "torso", (-0.100, 0.240, -0.082), (0.100, 0.500, 0.078), CLOTH),
    ("jacket-left",   "torso", (0.048, 0.240, -0.100), (0.138, 0.500, 0.092), JACKET),
    ("jacket-right",  "torso", (-0.138, 0.240, -0.100), (-0.048, 0.500, 0.092), JACKET),
    ("jacket-back",   "torso", (-0.138, 0.240, 0.076), (0.138, 0.500, 0.092), JACKET),
    ("jacket-collar", "torso", (-0.142, 0.474, -0.106), (0.142, 0.500, 0.098), JACKET_DARK),
    ("lapel-left",    "torso", (0.032, 0.420, -0.108), (0.056, 0.500, -0.088), JACKET_DARK),
    ("lapel-right",   "torso", (-0.056, 0.420, -0.108), (-0.032, 0.500, -0.088), JACKET_DARK),

    ("belt",          "torso", (-0.106, 0.240, -0.092), (0.106, 0.268, 0.086), CLOTH),
    ("buckle",        "torso", (-0.036, 0.236, -0.108), (0.036, 0.272, -0.090), GOLD),

    # The necklace: two strands to the collarbone and a cross hanging off them.
    ("chain-left",    "torso", (0.024, 0.436, -0.098), (0.040, 0.478, -0.080), CHAIN),
    ("chain-right",   "torso", (-0.040, 0.436, -0.098), (-0.024, 0.478, -0.080), CHAIN),
    ("cross-stem",    "torso", (-0.012, 0.392, -0.102), (0.012, 0.440, -0.080), JACKET),
    ("cross-arm",     "torso", (-0.030, 0.416, -0.102), (0.030, 0.430, -0.080), JACKET),
]

# ⚠️ THE ARM BOXES ARE CENTRED ON THE SHOULDER'S OWN Y (0.452) BECAUSE THE BIND POSE
# IS A T-POSE: the limb runs along X, not down. Its vertical extent is thickness.
ARM_LEFT = [
    # ⚠️⚠️ THE SLEEVE IS SHORT AND THE BARE ARM IS THE LONGER HALF. 🧑 comparing the
    # first build to the reference: *"purple dude doesnt have sleeves"* and *"whyd u
    # use osmoene with fat ass sleeves"*. That version ran the jacket almost to the
    # wrist with a small hand on the end, which is the BASE rig's silhouette and not
    # this character's: the reference stops the sleeve near the elbow and the forearm
    # and hand are one long bare block, roughly 45 percent sleeve to 55 percent skin.
    ("sleeve-left", "arm-left", (0.0999, 0.390, -0.064), (0.226, 0.514, 0.082), JACKET),
    # A raised band, not a colour change on the sleeve. Same rule as the buckle: it
    # needs an edge or the outline pass has nothing to draw.
    ("cuff-left",   "arm-left", (0.226, 0.384, -0.074), (0.248, 0.520, 0.092), JACKET_DARK),

    # ⚠️⚠️ THE HAND'S HEIGHT IS NOT A STYLING CHOICE, IT IS WHERE THE TSINELAS SITS.
    # `CharacterVisual.BuildHandAnchor` puts a carried shoe at the palm centre plus
    # `HandTopLift`, which is 0.0617 measured against the Kenney hand, and that one
    # constant serves all twelve people. The palm centre lands on this box's centre, so
    # the box's TOP has to sit 0.0617 above that centre or the shoe is buried in the
    # hand. An earlier build made a chunkier mitt and buried it by 11 mm, which is the
    # failure the Godot side reported as *"its almost on the arm, js phasing a bit thru
    # it"*.
    #
    #   shoulder Y 0.452, so 0.452 - 0.0617 = 0.3903 to 0.452 + 0.0617 = 0.5137.
    #
    # `PersonSwapProbe` re-derives this from the built mesh and fails on it, so the
    # arithmetic is checked rather than trusted.
    ("hand-left",   "arm-left", (0.248, 0.3903, -0.062), (0.3836, 0.5137, 0.080), SKIN_LIT),
]

# Neck at 0.505, and the rig may not exceed 0.7234.
HEAD = [
    # ⚠️⚠️ THE FACE IS THE BUDGET, AND AN EARLIER BUILD SPENT IT ON HAIR. That version
    # wrapped the mop round the sides to a depth in FRONT of the cheeks, and rendered at
    # the three-quarter angle the game shows a character from, the read was "a black
    # shape with an orange corner". The skull is the larger half and the hair is a cap
    # over the top of it.
    #
    # This matters more here than on the base rig, because a Kenney head is a smooth
    # ovoid and this one is a box: a box's front face is the only surface the eyes can
    # live on, so anything overhanging it costs the whole expression.
    ("neck",      "head", (-0.046, 0.505, -0.040), (0.046, 0.536, 0.040), SKIN),
    ("skull",     "head", (-0.106, 0.528, -0.106), (0.106, 0.674, 0.092), SKIN),
    ("ear-left",  "head", (0.106, 0.578, -0.036), (0.124, 0.618, 0.018), SKIN),
    ("ear-right", "head", (-0.124, 0.578, -0.036), (-0.106, 0.618, 0.018), SKIN),

    # ⚠️ BIG AND FEW. Rendered in play a head is roughly 90 px tall, so an eye 20 mm
    # wide on a 210 mm head is two pixels and reads as dirt. These are each about a
    # fifth of the face and there are only three of them.
    ("eye-left",     "head", (0.026, 0.596, -0.118), (0.062, 0.626, -0.100), INK),
    ("eye-right",    "head", (-0.062, 0.596, -0.118), (-0.026, 0.626, -0.100), INK),
    ("brow-left",    "head", (0.022, 0.634, -0.118), (0.066, 0.648, -0.100), INK),
    ("brow-right",   "head", (-0.066, 0.634, -0.118), (-0.022, 0.648, -0.100), INK),
    ("mouth",        "head", (-0.024, 0.552, -0.118), (0.024, 0.564, -0.100), INK),
    ("smile-left",   "head", (0.024, 0.564, -0.118), (0.038, 0.578, -0.100), INK),
    ("smile-right",  "head", (-0.038, 0.564, -0.118), (-0.024, 0.578, -0.100), INK),

    # The hair is a CAP. It sits on the skull and comes down the back, and the only
    # thing it puts in front of the face is a fringe that stops above the brows. The
    # volume that makes it read as a mop goes UPWARD, into the space between the top of
    # the skull and the 0.7234 the rig is allowed to occupy, which is height this
    # character has going spare and the face does not.
    ("hair-fringe",  "head", (-0.108, 0.652, -0.116), (0.108, 0.680, -0.096), HAIR),
    ("hair-top",     "head", (-0.110, 0.670, -0.114), (0.110, 0.702, 0.098), HAIR),
    ("hair-crown",   "head", (-0.094, 0.700, -0.098), (0.094, 0.7184, 0.086), HAIR),
    ("hair-peak",    "head", (-0.062, 0.714, -0.070), (0.062, 0.7234, 0.060), HAIR),
    ("hair-curl-a",  "head", (-0.112, 0.696, -0.064), (-0.068, 0.7234, 0.010), HAIR),
    ("hair-curl-b",  "head", (0.072, 0.694, 0.000), (0.116, 0.7184, 0.074), HAIR),
    ("hair-curl-c",  "head", (-0.038, 0.708, 0.056), (0.022, 0.7234, 0.100), HAIR),

    # ⚠️ THE SIDES STOP BEHIND THE CHEEK. Sideburns reaching the front plane are what
    # swallowed the face in the earlier build. They are also thin: the ears sit at 0.106
    # to 0.124 and the hair has to clear them.
    ("hair-side-left",  "head", (0.106, 0.618, -0.084), (0.126, 0.680, 0.098), HAIR),
    ("hair-side-right", "head", (-0.126, 0.618, -0.084), (-0.106, 0.680, 0.098), HAIR),
    ("hair-back",    "head", (-0.110, 0.556, 0.092), (0.110, 0.702, 0.124), HAIR),
    ("hair-nape",    "head", (-0.082, 0.528, 0.070), (0.082, 0.562, 0.116), HAIR),

    # The clip, on the character's RIGHT, which is -X here. It is deliberately the
    # biggest single non-hair shape on the head: it is the one silhouette cue that tells
    # this character apart from the rest of the cast at arena distance, where neither
    # the face nor the jacket detail survives.
    ("clip-band",       "head", (-0.130, 0.662, -0.074), (-0.106, 0.706, 0.022), CLIP),
    ("clip-lobe-upper", "head", (-0.172, 0.694, -0.054), (-0.108, 0.7234, 0.016), CLIP),
    ("clip-lobe-lower", "head", (-0.162, 0.640, -0.062), (-0.108, 0.684, 0.008), CLIP),
    ("clip-knot",       "head", (-0.140, 0.680, -0.034), (-0.104, 0.708, 0.002), CLIP),
]

BODY_BOXES = (LEG_LEFT + mirrored(LEG_LEFT, "leg-left", "leg-right")
              + TORSO
              + ARM_LEFT + mirrored(ARM_LEFT, "arm-left", "arm-right"))
HEAD_BOXES = HEAD

# ---------------------------------------------------------------------------
# Geometry.
# ---------------------------------------------------------------------------

# ⚠️ THE TABLES ARE AUTHORED FACING -Z AND THE FILE'S FRONT IS +Z. The conversion is a
# NEGATION WITH THE TWO Z BOUNDS SWAPPED, so every box stays well formed and the face
# winding below stays outward. Mirroring without the swap turns every normal inside out
# and the character renders as a hole.
FRONT_IS_MINUS_Z = True

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


def build_mesh(boxes):
    """Boxes to flat glTF attribute arrays."""
    pos, nrm, uv, joints, weights, idx = [], [], [], [], [], []

    for name, bone, lo, hi, slot in boxes:
        if FRONT_IS_MINUS_Z:
            lo, hi = (lo[0], lo[1], -hi[2]), (hi[0], hi[1], -lo[2])

        for axis in range(3):
            if hi[axis] <= lo[axis]:
                raise SystemExit(f"box '{name}' is inside out on axis {axis}")

        j = BONE[bone]
        u, v = cell_uv(slot)

        for normal, corners in FACES:
            first = len(pos)

            for cx, cy, cz in corners:
                pos.append((lo[0] + (hi[0] - lo[0]) * cx,
                            lo[1] + (hi[1] - lo[1]) * cy,
                            lo[2] + (hi[2] - lo[2]) * cz))
                nrm.append(tuple(float(c) for c in normal))
                uv.append((u, v))
                joints.append((j, 0, 0, 0))
                weights.append((1.0, 0.0, 0.0, 0.0))

            idx += [first, first + 1, first + 2, first, first + 2, first + 3]

    return pos, nrm, uv, joints, weights, idx


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

    body = build_mesh(BODY_BOXES)
    head = build_mesh(HEAD_BOXES)

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

    # See the module docstring: PersonScale is one constant for the whole cast.
    if abs(height - 0.7234) > 0.002:
        raise SystemExit(
            f"\nHEIGHT CONSTRAINT VIOLATION - nothing written.\n"
            f"  authored height {height:.4f}, base rig is 0.7234.\n"
            f"  CharacterVisual.PersonScale multiplies every Person by 2.38, so a rig\n"
            f"  authored to a different height walks the arena at the wrong size.")

    if abs(lo[1]) > 0.001:
        raise SystemExit(f"feet are at y={lo[1]:.4f}, not 0. The floor align measures "
                         "bounds, but the bind pose should still stand on zero.")

    # ⚠️ EVERY BOX MUST BE INSIDE ITS OWN BONE'S REACH, or the limb tears when the clip
    # rotates it. A box hung off the wrong bone is the single easiest mistake to make in
    # a table this long and it is invisible until something moves.
    for name, bone, box_lo, box_hi, slot in BODY_BOXES + HEAD_BOXES:
        origin = SKELETON[bone][1]
        reach = max(abs(box_lo[1] - origin), abs(box_hi[1] - origin))

        if reach > 0.30:
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
