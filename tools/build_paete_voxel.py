# Paete's own builder: a copy of tools/build_amihan_voxel.py at 2026-09-25 (itself a copy of
# Rafi's, itself of build_person_voxel.py). Neither original is touched.
# Brief: ArtSource/paete/concept-20260925/design-brief.md.
RAFI_RECIPE_READY = True
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

OUT = "Assets/TumbangPreso/Art/characters/persons/team-paete.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-paete.tres"

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
# that the numbers in the table above this block were always the target (legs 24%, torso
# 23%, head 53%), and this character had drifted to 32/30/38, which reads as a taller,
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
# ---------------------------------------------------------------------------
# ⚠️⚠️ PAETE, THE NINTH HERO (owner, 2026-09-25): a plant hero from Mount Makiling, named after
# the carving town across the lake. Brief, research and lore:
# `ArtSource/paete/concept-20260925/design-brief.md`. Method: `docs/CHARACTER_MODEL_METHOD.md`.
# This builder is a copy of Amihan's (itself Rafi's, itself `build_person_voxel.py`); nothing here
# touches another hero.
#
# ⚠️⚠️ EVERY PIECE IS TYPED BY HAND, ONE ROW EACH, AND THAT IS THE OWNER'S RULE, NOT A STYLE.
# *"pls draw his 3d model or do it one by one dont try to mass generate it bcz it will loo like
# shit"*, *"thoroughly refine ALL details of EVERYTHINg ur making and do it carefully dont auto
# make it all with a script"*, *"it has to really look like a tree and very detailed but
# blocky"*, *"its fine if its low poly in some parts (vines)"*. So there is no loop anywhere below
# that places a plank, a leaf, a root or a moss patch: each has its own numbers, the two sides of
# the body are typed separately and differ, and no two leaves are the same size or angle.
#
# What he is made of, and why:
#   bark planks      the carved-plank head of the owner's face close-up, and a trunk of planks
#   sunken sockets   the owner: "engraved sunked green eyes". The brow, nose, cheek and temple
#                    planks stand 35 to 60 mm proud of the heartwood, and the eyes sit on the
#                    heartwood between them, so they are recessed by construction.
#   antlers          two branches forking by hand, different on each side
#   moss             his one quiet colour (brief section 6), in the seams: crown, shoulders, knees
#   root feet        splayed root toes, each its own block
#   vines, leaves    low poly by the owner's leave, each one placed
#
# ⚠️ THE FACE IS NOT INK-ONLY, BY THE OWNER'S INSTRUCTION (brief section 5). The eyes are the one
# bright value on him (slot 10), set in a darker socket (slot 9); the only ink is the carved mouth
# cut and the grain lines. The attitude comes from the brow planks' angle.
#
# ⚠️ HIS BARK IS IN THE SKIN SLOTS 13 TO 15 ON PURPOSE. `PaletteRules.IsProtectedSlot` keeps the
# skin slots out of every recolour (nobody's skin is a dial, VISION section 6), and bark is his
# skin; `CharacterVisual.PalmCentre` also looks for the skin cells to find the hand.
#
# ⚠️ THE TABLES ARE AUTHORED DIRECTLY ON THE BASE RIG'S OWN JOINT HEIGHTS. `WAS_*` equals `NOW_*`
# below, so the family remap is the identity and a number here is a number in the file: hips
# 0.176, shoulders 0.288, neck 0.343, and the antler tips under the cast's 0.7928 ceiling.
#
# Slot key:
#   0 moss             1 moss dark        2 leaf             3 leaf dark
#   4 vine             5 heartwood        6 root             7 moss lit
#   8 ink (carved)     9 socket           10 eye light       11 eye glow
#   12 flower heart    13 bark            14 bark dark       15 bark lit
# ---------------------------------------------------------------------------

MOSS, MOSS_DARK, LEAF, LEAF_DARK = 0, 1, 2, 3
VINE, HEARTWOOD, ROOT, MOSS_LIT = 4, 5, 6, 7
INK, SOCKET, EYE, EYE_GLOW = 8, 9, 10, 11
FLOWER_HEART, BARK, BARK_DARK, BARK_LIT = 12, 13, 14, 15

PALETTE = {0: '5E7F24',
 1: '3F5A1A',
 2: '9CC23F',
 3: '6A962E',
 4: '557A26',
 5: 'C29563',
 6: '6B4A2E',
 7: '7FA034',
 8: '1E140C',
 9: '1C1109',
 10: 'D8FF6A',
 11: '86C83A',
 12: 'E8C24A',
 13: '8C6440',
 14: '553A22',
 15: 'B08450'}

MAX_FACE_LUMINANCE = 0.30

# ⚠️⚠️ v7: BUILT FROM THE OWNER'S IDEA BOARD (2026-09-25, shared in chat), read part by part:
#   head     a tall mask of upright planks split down the centre, narrowing to a V jaw, a heavy
#            brow slanting down to the nose over angled green slits, a carved spiral on the
#            forehead plank
#   horns    square-section branches with right-angle elbows, small tines, a few leaves
#   chest    two plank slabs in a V round a diamond boss carrying a spiral, a narrowing plank waist
#   arms     layered slanted shoulder planks, long plank-bundle arms (v15: ending in braided
#            pointed vines, not claws: the cast has no fingers)
#   legs     long plank-bundle legs on spreading root feet
#   green    thick vines wrapping the torso, a forearm and a leg; moss in the crevices; clusters
#            of big leaves sprouting from the seams
#   palette  five browns and four greens (the board's strip)
# The owner on v1 to v4: *"what the fuck is this it DOES NOTTT LOOK LIEK REFERECNES AT ALL"*,
# *"its body is js brown with greenshit coming out (leaves and mosss and engravings)"*, *"it
# doesnt have hair"*, *"i want u to really analyze all details in references"*.
#
# ⚠️ HIS OWN PROPORTIONS, NOT THE CAST'S CHIBI ONES (the board is a long-limbed golem). The bone
# names stay; the four keyed tracks shift with their bones; head and arms are free; the total
# stays inside the cast's height range so he stands at the right size on the street:
#
#     feet 0   hips 0.260   shoulders 0.470   neck 0.495   crown 0.668   horn tips 0.79
SKELETON = {
    "root":      (0.0,    0.0,   0.0),
    "leg-left":  (0.085,  0.260, -0.02875),
    "leg-right": (-0.085, 0.260, -0.02875),
    "torso":     (0.0,    0.260, -0.02875),
    "arm-left":  (0.150,  0.470, -0.01725),
    "arm-right": (-0.150, 0.470, -0.01725),
    "head":      (0.0,    0.495, -0.00236),
}

# ---------------------------------------------------------------------------
# LEGS: a bundle of upright planks round a dark core, a knee plate, a root foot. The roots
# themselves are forms. Hips at 0.260. The two legs are typed separately and differ.
# ---------------------------------------------------------------------------
LEG_LEFT = [
 ('foot-left', 'leg-left', (0.014, 0.000, -0.114), (0.162, 0.044, 0.078), ROOT),
 ('foot-moss-left', 'leg-left', (0.058, 0.034, -0.102), (0.112, 0.050, -0.074), MOSS_DARK),
 ('shin-core-left', 'leg-left', (0.036, 0.036, -0.068), (0.140, 0.150, 0.064), BARK_DARK),
 ('shin-plank-left-a', 'leg-left', (0.040, 0.030, -0.086), (0.084, 0.150, -0.064), BARK),
 ('shin-plank-left-b', 'leg-left', (0.090, 0.040, -0.084), (0.138, 0.138, -0.060), BARK_LIT),
 ('shin-plank-left-c', 'leg-left', (0.132, 0.036, -0.056), (0.156, 0.146, 0.040), BARK),
 ('shin-plank-left-d', 'leg-left', (0.046, 0.040, 0.056), (0.130, 0.144, 0.080), BARK),
 ('knee-left', 'leg-left', (0.036, 0.130, -0.104), (0.140, 0.182, -0.066), BARK_LIT),
 ('knee-moss-left', 'leg-left', (0.048, 0.176, -0.104), (0.128, 0.188, -0.090), MOSS),
 ('thigh-core-left', 'leg-left', (0.034, 0.160, -0.066), (0.146, 0.290, 0.066), BARK_DARK),
 ('thigh-plank-left-a', 'leg-left', (0.040, 0.172, -0.084), (0.096, 0.284, -0.062), BARK),
 ('thigh-plank-left-b', 'leg-left', (0.102, 0.166, -0.082), (0.148, 0.274, -0.058), BARK_LIT),
 ('thigh-plank-left-c', 'leg-left', (0.138, 0.170, -0.050), (0.160, 0.278, 0.044), BARK),
 ('thigh-plank-left-d', 'leg-left', (0.044, 0.176, 0.056), (0.136, 0.286, 0.078), BARK),
]

LEG_RIGHT = [
 ('foot-right', 'leg-right', (-0.164, 0.000, -0.116), (-0.012, 0.046, 0.076), ROOT),
 ('shin-core-right', 'leg-right', (-0.142, 0.036, -0.066), (-0.034, 0.152, 0.066), BARK_DARK),
 ('shin-plank-right-a', 'leg-right', (-0.080, 0.034, -0.088), (-0.036, 0.152, -0.066), BARK_LIT),
 ('shin-plank-right-b', 'leg-right', (-0.140, 0.036, -0.082), (-0.088, 0.142, -0.058), BARK),
 ('shin-plank-right-c', 'leg-right', (-0.160, 0.040, -0.044), (-0.136, 0.140, 0.050), BARK_LIT),
 ('shin-plank-right-d', 'leg-right', (-0.128, 0.036, 0.058), (-0.044, 0.148, 0.082), BARK),
 ('knee-right', 'leg-right', (-0.144, 0.134, -0.102), (-0.036, 0.184, -0.066), BARK),
 ('knee-knot-right', 'leg-right', (-0.100, 0.150, -0.110), (-0.070, 0.172, -0.098), BARK_DARK),
 ('shin-moss-right', 'leg-right', (-0.092, 0.050, -0.094), (-0.082, 0.126, -0.084), MOSS_LIT),
 ('thigh-core-right', 'leg-right', (-0.148, 0.162, -0.064), (-0.034, 0.290, 0.068), BARK_DARK),
 ('thigh-plank-right-a', 'leg-right', (-0.094, 0.168, -0.086), (-0.038, 0.282, -0.064), BARK),
 ('thigh-plank-right-b', 'leg-right', (-0.150, 0.174, -0.080), (-0.100, 0.280, -0.056), BARK),
 ('thigh-plank-right-c', 'leg-right', (-0.162, 0.168, -0.046), (-0.140, 0.272, 0.048), BARK_LIT),
 ('thigh-plank-right-d', 'leg-right', (-0.138, 0.170, 0.056), (-0.046, 0.284, 0.080), BARK),
]

# ---------------------------------------------------------------------------
# TORSO: a narrow plank waist, an abdomen of two slanted planks, a broad chest of two slabs
# meeting in a V round a diamond boss, plank sides and back, moss in the crevices.
# Owns 0.22 to 0.51.
# ---------------------------------------------------------------------------
TORSO = [
 ('waist-core', 'torso', (-0.110, 0.228, -0.078), (0.110, 0.302, 0.074), BARK_DARK),
 ('hip-plank-left', 'torso', (0.018, 0.228, -0.094), (0.118, 0.292, -0.070), BARK),
 ('hip-plank-right', 'torso', (-0.120, 0.232, -0.092), (-0.016, 0.290, -0.070), BARK_LIT),
 # ⚠️ v13: bark plates hanging from the waist over the thighs, so the trunk runs into the legs
 # instead of stopping at a line; each its own length and lean.
 ('tasset-left', 'torso', (0.040, 0.196, -0.104), (0.126, 0.258, -0.086), BARK),
 ('tasset-left-outer', 'torso', (0.112, 0.202, -0.080), (0.150, 0.256, 0.020), BARK_LIT),
 ('tasset-right', 'torso', (-0.124, 0.190, -0.102), (-0.044, 0.254, -0.084), BARK_LIT),
 ('tasset-right-outer', 'torso', (-0.152, 0.206, -0.070), (-0.114, 0.254, 0.030), BARK),
 ('ab-plank-left', 'torso', (0.008, 0.290, -0.106), (0.106, 0.352, -0.082), BARK),
 ('ab-plank-right', 'torso', (-0.108, 0.286, -0.104), (-0.006, 0.348, -0.080), BARK),
 ('chest-core', 'torso', (-0.160, 0.340, -0.100), (0.160, 0.480, 0.096), BARK_DARK),
 ('pec-left', 'torso', (0.006, 0.382, -0.132), (0.166, 0.478, -0.098), BARK_LIT),
 ('pec-right', 'torso', (-0.168, 0.378, -0.130), (-0.004, 0.474, -0.096), BARK),
 ('sternum-boss', 'torso', (-0.028, 0.352, -0.140), (0.028, 0.408, -0.108), BARK),
 # ⚠️ v12: NO SIDE PLANKS. v7 to v11 hung one down each flank and below the chest they read as
 # two thin posts beside the waist; the owner circled both.
 ('back-plank-left', 'torso', (0.004, 0.350, 0.092), (0.158, 0.478, 0.118), BARK),
 ('back-plank-right', 'torso', (-0.156, 0.346, 0.094), (-0.006, 0.474, 0.120), BARK_LIT),
 ('back-lower', 'torso', (-0.100, 0.262, 0.074), (0.100, 0.346, 0.098), BARK),
 ('spine-plank', 'torso', (-0.020, 0.300, 0.110), (0.022, 0.470, 0.130), BARK_LIT),
 # ⚠️ v14: the back gets its own planks (v13's was two flat slabs): shoulder blades that slant
 # out and down, a plate over the small of the back, moss where they meet.
 ('blade-left', 'torso', (0.040, 0.390, 0.116), (0.140, 0.454, 0.136), BARK),
 ('blade-right', 'torso', (-0.142, 0.384, 0.118), (-0.042, 0.448, 0.138), BARK),
 ('small-of-back', 'torso', (-0.070, 0.270, 0.096), (0.064, 0.316, 0.114), BARK_LIT),
 ('back-moss-seam', 'torso', (0.024, 0.360, 0.130), (0.090, 0.372, 0.142), MOSS),
 ('back-moss-low', 'torso', (-0.090, 0.318, 0.106), (-0.034, 0.330, 0.120), MOSS_DARK),
 ('chest-strand-a', 'torso', (0.130, 0.410, -0.132), (0.144, 0.446, -0.122), MOSS),
 ('chest-strand-b', 'torso', (-0.146, 0.360, -0.132), (-0.134, 0.392, -0.122), MOSS_LIT),
 ('neck', 'torso', (-0.060, 0.466, -0.050), (0.060, 0.512, 0.050), BARK_DARK),
 # Moss in the crevices, raised clumps of two greens.
 ('collar-moss-left', 'torso', (0.040, 0.470, -0.108), (0.142, 0.494, -0.030), MOSS),
 ('collar-moss-right', 'torso', (-0.132, 0.466, -0.100), (-0.050, 0.488, -0.020), MOSS_LIT),
 ('collar-moss-back', 'torso', (-0.080, 0.454, 0.110), (0.030, 0.480, 0.128), MOSS),
 ('pec-moss-left', 'torso', (0.122, 0.440, -0.138), (0.162, 0.468, -0.122), MOSS),
 ('pec-moss-right', 'torso', (-0.156, 0.380, -0.134), (-0.090, 0.392, -0.120), MOSS_DARK),
 ('ab-moss', 'torso', (-0.010, 0.292, -0.110), (0.004, 0.344, -0.098), MOSS),
 ('hip-moss', 'torso', (0.030, 0.286, -0.108), (0.104, 0.296, -0.094), MOSS_LIT),
 ('pec-edge-moss-left', 'torso', (0.020, 0.470, -0.130), (0.060, 0.484, -0.112), MOSS_LIT),
 ('pec-edge-moss-right', 'torso', (-0.074, 0.462, -0.126), (-0.044, 0.476, -0.110), MOSS),
]

# ---------------------------------------------------------------------------
# ARMS: slanted shoulder planks over a dark block, an upper-arm bundle, an elbow, a long
# forearm bundle ending in pointed vines twisted round each other. Shoulder at 0.470, so the palm is 0.4083 to
# 0.5317 (`HandTopLift` either side) and the vine points reach x 0.62: in the idle they hang to the knee.
# ---------------------------------------------------------------------------
ARM_LEFT = [
 ('pauldron-left', 'arm-left', (0.118, 0.418, -0.094), (0.250, 0.522, 0.094), BARK_DARK),
 ('pauldron-top-left', 'arm-left', (0.112, 0.512, -0.092), (0.262, 0.540, 0.088), BARK),
 ('pauldron-front-left', 'arm-left', (0.124, 0.436, -0.112), (0.252, 0.520, -0.090), BARK_LIT),
 ('pauldron-back-left', 'arm-left', (0.130, 0.436, 0.088), (0.246, 0.516, 0.108), BARK),
 ('pauldron-moss-left-a', 'arm-left', (0.150, 0.534, -0.050), (0.196, 0.552, 0.004), MOSS),
 ('pauldron-moss-left-b', 'arm-left', (0.190, 0.532, 0.020), (0.232, 0.548, 0.060), MOSS_LIT),
 ('pauldron-drip-left', 'arm-left', (0.198, 0.488, -0.118), (0.240, 0.520, -0.102), MOSS_LIT),
 # ⚠️ v11: a second slanted plank under the first (the board's shoulders are layered), and moss
 # hanging off the edge in strands, each its own length.
 ('pauldron-lower-left', 'arm-left', (0.200, 0.470, -0.100), (0.284, 0.494, 0.096), BARK),
 ('moss-strand-left-a', 'arm-left', (0.262, 0.444, -0.104), (0.278, 0.488, -0.094), MOSS),
 ('moss-strand-left-b', 'arm-left', (0.270, 0.430, -0.060), (0.284, 0.486, -0.050), MOSS_DARK),
 ('moss-strand-left-c', 'arm-left', (0.266, 0.452, 0.040), (0.280, 0.488, 0.050), MOSS_LIT),
 ('upper-arm-left', 'arm-left', (0.240, 0.430, -0.052), (0.340, 0.508, 0.052), BARK_DARK),
 ('upper-arm-top-left', 'arm-left', (0.244, 0.500, -0.040), (0.336, 0.520, 0.036), BARK),
 ('upper-arm-front-left', 'arm-left', (0.246, 0.440, -0.066), (0.334, 0.496, -0.048), BARK_LIT),
 ('elbow-left', 'arm-left', (0.326, 0.422, -0.068), (0.376, 0.518, 0.068), BARK),
 ('elbow-moss-left', 'arm-left', (0.360, 0.508, -0.050), (0.384, 0.524, 0.030), MOSS),
]

ARM_RIGHT = [
 ('pauldron-right', 'arm-right', (-0.252, 0.416, -0.092), (-0.116, 0.520, 0.096), BARK_DARK),
 ('pauldron-top-right', 'arm-right', (-0.264, 0.510, -0.086), (-0.110, 0.538, 0.092), BARK_LIT),
 ('pauldron-front-right', 'arm-right', (-0.254, 0.434, -0.110), (-0.122, 0.518, -0.088), BARK),
 ('pauldron-back-right', 'arm-right', (-0.248, 0.438, 0.090), (-0.128, 0.512, 0.110), BARK),
 # ⚠️ v8: tufts, not one slab (v7's 80 mm square read as a sticker on the shoulder).
 ('pauldron-moss-right-a', 'arm-right', (-0.214, 0.532, 0.010), (-0.168, 0.552, 0.062), MOSS_LIT),
 ('pauldron-moss-right-b', 'arm-right', (-0.176, 0.530, -0.040), (-0.140, 0.546, -0.004), MOSS),
 ('pauldron-moss-right-c', 'arm-right', (-0.252, 0.528, -0.060), (-0.226, 0.544, -0.030), MOSS_DARK),
 ('pauldron-drip-right', 'arm-right', (-0.254, 0.430, -0.116), (-0.218, 0.464, -0.100), MOSS),
 ('pauldron-lower-right', 'arm-right', (-0.288, 0.466, -0.094), (-0.204, 0.492, 0.100), BARK_LIT),
 ('moss-strand-right-a', 'arm-right', (-0.282, 0.426, -0.090), (-0.268, 0.484, -0.080), MOSS),
 ('moss-strand-right-b', 'arm-right', (-0.286, 0.446, 0.010), (-0.272, 0.484, 0.020), MOSS_LIT),
 ('upper-arm-right', 'arm-right', (-0.342, 0.428, -0.050), (-0.242, 0.506, 0.054), BARK_DARK),
 ('upper-arm-top-right', 'arm-right', (-0.338, 0.498, -0.034), (-0.246, 0.518, 0.042), BARK_LIT),
 ('upper-arm-back-right', 'arm-right', (-0.334, 0.444, 0.050), (-0.248, 0.500, 0.068), BARK),
 ('elbow-right', 'arm-right', (-0.378, 0.420, -0.066), (-0.328, 0.516, 0.070), BARK),
 ('elbow-moss-right', 'arm-right', (-0.386, 0.418, -0.040), (-0.362, 0.434, 0.050), MOSS_LIT),
]

# ---------------------------------------------------------------------------
# THE HEAD: a tall mask of planks on a dark core that narrows to the jaw. Neck at 0.495, crown
# 0.668. Face at -z; the core's front is at z -0.100 and the face planks stand 26 to 46 mm proud,
# so the eyes sit sunk between the slanted brow and the cheek planks.
# ---------------------------------------------------------------------------
HEAD = [
 ('head-core', 'head', (-0.100, 0.490, -0.100), (0.100, 0.660, 0.094), BARK_DARK),
 ('face-left', 'head', (0.004, 0.490, -0.132), (0.104, 0.552, -0.098), BARK),
 ('face-right', 'head', (-0.104, 0.490, -0.130), (-0.004, 0.550, -0.098), BARK_LIT),
 ('cheek-left', 'head', (0.040, 0.548, -0.126), (0.106, 0.566, -0.098), BARK),
 ('cheek-right', 'head', (-0.106, 0.548, -0.124), (-0.040, 0.566, -0.098), BARK),
 # ⚠️ v10: the centre ridge is a seam plank, not a nose: v9's stood 46 mm proud and ran to the
 # chin, and read as a long nose.
 ('nose-ridge', 'head', (-0.012, 0.548, -0.130), (0.012, 0.604, -0.104), BARK_LIT),
 ('brow-left', 'head', (0.004, 0.584, -0.142), (0.114, 0.608, -0.100), BARK),
 ('brow-right', 'head', (-0.114, 0.584, -0.140), (-0.004, 0.608, -0.100), BARK),
 ('forehead', 'head', (-0.058, 0.604, -0.126), (0.058, 0.672, -0.098), BARK_LIT),
 ('forehead-side-left', 'head', (0.060, 0.606, -0.118), (0.102, 0.662, -0.096), BARK),
 ('forehead-side-right', 'head', (-0.102, 0.606, -0.116), (-0.060, 0.658, -0.096), BARK),
 # ⚠️ v9: the sides are two planks each, at different depths and heights, not one flat wall.
 ('head-side-left', 'head', (0.094, 0.500, -0.090), (0.116, 0.676, -0.004), BARK),
 ('head-side-left-rear', 'head', (0.090, 0.508, 0.004), (0.110, 0.652, 0.088), BARK_LIT),
 ('head-side-right', 'head', (-0.116, 0.498, -0.086), (-0.094, 0.678, 0.010), BARK_LIT),
 ('head-side-right-rear', 'head', (-0.112, 0.504, 0.018), (-0.090, 0.644, 0.090), BARK),
 ('head-back-left', 'head', (0.002, 0.500, 0.090), (0.100, 0.652, 0.112), BARK),
 ('head-back-right', 'head', (-0.100, 0.496, 0.092), (-0.002, 0.658, 0.114), BARK_LIT),
 ('crown', 'head', (-0.092, 0.652, -0.090), (0.092, 0.670, 0.088), BARK),
 ('crown-moss', 'head', (-0.054, 0.666, -0.030), (0.046, 0.680, 0.070), MOSS),
 ('head-moss-back', 'head', (-0.086, 0.612, 0.108), (-0.060, 0.634, 0.120), MOSS_LIT),
 ('nape-moss', 'head', (-0.050, 0.496, 0.104), (0.060, 0.510, 0.120), MOSS),
 ('head-back-knot', 'head', (0.030, 0.560, 0.110), (0.060, 0.586, 0.122), BARK_DARK),
 ('head-moss-side', 'head', (0.110, 0.640, 0.020), (0.124, 0.664, 0.052), MOSS),
]

BOX_TILTS = {
 # The brow slants down to the nose, gently (the board's NEUTRAL, held calm: the owner asked for
 # a nonchalant face; v9's 7 degrees read stern). A positive
 # tilt lifts the +x end, so the left plank's outer end rises and the right plank's mirrors it.
 'brow-left': 4.0, 'brow-right': -4.0,
 'forehead-side-left': 4.0, 'forehead-side-right': -4.0,
 # The chest slabs meet in a V; the boss is a diamond.
 'pec-left': -12.0, 'pec-right': 11.0, 'sternum-boss': 45.0,
 'hip-plank-left': 6.0, 'hip-plank-right': -5.0,
 'tasset-left': 7.0, 'tasset-right': -6.0, 'tasset-left-outer': 4.0, 'tasset-right-outer': -5.0,
 'ab-plank-left': -3.0, 'ab-plank-right': 3.0,
 'back-plank-left': 8.0, 'back-plank-right': -7.0,
 'blade-left': -9.0, 'blade-right': 10.0,
 'pauldron-top-left': -8.0, 'pauldron-front-left': -6.0, 'pauldron-back-left': -5.0,
 'pauldron-top-right': 7.0, 'pauldron-front-right': 5.0, 'pauldron-back-right': 6.0,
 'pauldron-lower-left': -14.0, 'pauldron-lower-right': 13.0,
 'knee-left': -4.0, 'knee-right': 3.0,



 'shin-plank-left-a': 2.0, 'shin-plank-left-b': -2.0, 'shin-plank-right-a': -1.5, 'shin-plank-right-b': 2.5,
 'thigh-plank-left-b': -2.0, 'thigh-plank-right-a': 2.0,
}

BOX_TURNS = {}

BOX_TAPERS = {
 'thigh-core-left': (1, 0, .86, 1.0), 'thigh-core-right': (1, 0, .88, 1.0),
 'thigh-plank-left-b': (1, 0, .82, 1.0), 'thigh-plank-right-b': (1, 0, .84, 1.0),
 # ⚠️ v12: the shins widen toward the ground like a trunk into its roots (v11's even boxes read
 # as trousers).
 'shin-core-left': (1, 0, 1.22, 1.0), 'shin-core-right': (1, 0, 1.24, 1.0),
 'shin-plank-left-a': (1, 0, 1.30, 1.0), 'shin-plank-left-b': (1, 0, 1.26, 1.0),
 'shin-plank-right-a': (1, 0, 1.28, 1.0), 'shin-plank-right-b': (1, 0, 1.32, 1.0),
 # The forearms narrow toward the wrist.


 # The mask narrows to the jaw: the core, and each lower face plank toward its inner edge.
 'head-core': (1, 0, .74, 1.0),
  'nose-ridge': (1, 0, .70, 1.0),
 'waist-core': (1, 0, .86, 1.0),
 # Claws thin toward their tips.


}


# ---------------------------------------------------------------------------
# § DECALS: the eyes and the carved cuts, every one its own hand-set polygon (method, section 4).
# Views: front/back (x, y), left/right (z, y), top (x, z); table space; layer on the surface.
# ---------------------------------------------------------------------------
def _quad(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def _stroke(points, width):
    """A polyline as convex quads, with a square at each inner joint."""
    out, half = [], width * 0.5
    for (ax, ay), (bx, by) in zip(points, points[1:]):
        length = math.hypot(bx - ax, by - ay)
        nx, ny = -(by - ay) / length * half, (bx - ax) / length * half
        out.append([(ax + nx, ay + ny), (bx + nx, by + ny), (bx - nx, by - ny), (ax - nx, ay - ny)])
    for x, y in points[1:-1]:
        out.append(_quad(x - half, y - half, x + half, y + half))
    return out


# The eyes: a dark socket in the heartwood between the slanted brow and the cheek plank, and an
# angled slit of light inside it, lower at the nose (the board's NEUTRAL). Each eye typed on its
# own.
FACE = [
 ('head-core', 'front', SOCKET, [(0.016, 0.565), (0.100, 0.565), (0.100, 0.590), (0.016, 0.578)], 1),
 ('head-core', 'front', SOCKET, [(-0.100, 0.565), (-0.016, 0.565), (-0.016, 0.578), (-0.100, 0.590)], 1),
 # ⚠️ v9: EACH EYE IS A BRIGHT CORE INSIDE A GREEN RIM, the board's glow spilling onto the socket;
 # a single flat lime slit read as a sticker at the three-quarter angle.
 ('head-core', 'front', EYE_GLOW, [(0.026, 0.568), (0.084, 0.571), (0.083, 0.584), (0.030, 0.577)], 2),
 ('head-core', 'front', EYE_GLOW, [(-0.084, 0.571), (-0.026, 0.568), (-0.030, 0.577), (-0.083, 0.584)], 2),
 ('head-core', 'front', EYE, [(0.034, 0.571), (0.078, 0.573), (0.077, 0.580), (0.037, 0.576)], 3),
 ('head-core', 'front', EYE, [(-0.078, 0.573), (-0.034, 0.571), (-0.037, 0.576), (-0.077, 0.580)], 3),
]

# The engravings, cut into the bark in the dark bark colour, each drawn on its own. The spirals
# are the board's: squared, turning inward, each laid out as one hand-drawn centreline and cut
# at a 4 to 5 mm stroke (`_stroke` only thickens the line that was drawn).
SPIRAL_FOREHEAD = [(-0.030, 0.612), (-0.030, 0.662), (0.030, 0.662), (0.030, 0.624), (-0.016, 0.624),
                   (-0.016, 0.650), (0.016, 0.650), (0.016, 0.636), (-0.002, 0.636)]
SPIRAL_BOSS = [(-0.014, 0.372), (-0.014, 0.390), (0.012, 0.390), (0.012, 0.376), (-0.006, 0.376),
               (-0.006, 0.384), (0.004, 0.384)]
SPIRAL_SHOULDER = [(0.166, 0.460), (0.166, 0.496), (0.210, 0.496), (0.210, 0.468), (0.178, 0.468),
                   (0.178, 0.486), (0.198, 0.486)]

ENGRAVE_HEAD = (
    [('forehead', 'front', BARK_DARK, piece, 1) for piece in _stroke(SPIRAL_FOREHEAD, 0.005)]
    + [
 # The face's grain: one upright line down each face plank, not level with each other.
 ('face-left', 'front', BARK_DARK, [(0.068, 0.502), (0.073, 0.502), (0.075, 0.542), (0.070, 0.542)], 1),
 ('face-right', 'front', BARK_DARK, [(-0.066, 0.506), (-0.061, 0.506), (-0.062, 0.540), (-0.067, 0.540)], 1),
 # The mouth: a short carved line across the split, low on the mask. Calm.
 ('face-left', 'front', BARK_DARK, [(0.008, 0.510), (0.048, 0.512), (0.048, 0.516), (0.008, 0.514)], 1),
 ('face-right', 'front', BARK_DARK, [(-0.048, 0.511), (-0.008, 0.509), (-0.008, 0.513), (-0.048, 0.515)], 1),
 # Up the sides of the head.
 ('head-side-left', 'left', BARK_DARK, [(-0.050, 0.520), (-0.045, 0.520), (-0.040, 0.640), (-0.045, 0.640)], 1),
 ('head-back-left', 'back', BARK_DARK, [(0.066, 0.512), (0.071, 0.512), (0.070, 0.640), (0.065, 0.640)], 1),
 ('head-back-right', 'back', BARK_DARK, [(-0.040, 0.520), (-0.035, 0.520), (-0.036, 0.600), (-0.041, 0.600)], 1),
 ('head-side-right', 'right', BARK_DARK, [(-0.040, 0.530), (-0.035, 0.530), (-0.037, 0.630), (-0.042, 0.630)], 1),
])

ENGRAVE_BODY = (
    [('sternum-boss', 'front', BARK_DARK, piece, 1) for piece in _stroke(SPIRAL_BOSS, 0.004)]
    + [('pauldron-front-left', 'front', BARK_DARK, piece, 1) for piece in _stroke(SPIRAL_SHOULDER, 0.005)]
    + [
 # Chest: a long cut along each slab, following its slope, and a notch on the right.
 ('pec-left', 'front', BARK_DARK, [(0.030, 0.452), (0.150, 0.430), (0.151, 0.436), (0.031, 0.458)], 1),
 ('pec-right', 'front', BARK_DARK, [(-0.152, 0.428), (-0.032, 0.450), (-0.033, 0.456), (-0.153, 0.434)], 1),
 ('pec-right', 'front', BARK_DARK, [(-0.130, 0.396), (-0.100, 0.388), (-0.098, 0.394), (-0.128, 0.402)], 1),
 # Abdomen and hips: upright grain.
 ('ab-plank-left', 'front', BARK_DARK, [(0.050, 0.296), (0.056, 0.296), (0.058, 0.344), (0.052, 0.344)], 1),
 ('ab-plank-right', 'front', BARK_DARK, [(-0.064, 0.292), (-0.058, 0.292), (-0.060, 0.340), (-0.066, 0.340)], 1),
 ('hip-plank-right', 'front', BARK_DARK, [(-0.090, 0.240), (-0.084, 0.240), (-0.082, 0.282), (-0.088, 0.282)], 1),
 # Legs: grain down the planks, each its own length.
 ('thigh-plank-left-a', 'front', BARK_DARK, [(0.064, 0.184), (0.069, 0.184), (0.071, 0.270), (0.066, 0.270)], 1),
 ('thigh-plank-right-a', 'front', BARK_DARK, [(-0.070, 0.190), (-0.065, 0.190), (-0.066, 0.262), (-0.071, 0.262)], 1),
 ('shin-plank-left-b', 'front', BARK_DARK, [(0.110, 0.052), (0.115, 0.052), (0.117, 0.124), (0.112, 0.124)], 1),
 ('shin-plank-right-b', 'front', BARK_DARK, [(-0.118, 0.060), (-0.113, 0.060), (-0.112, 0.100), (-0.117, 0.100)], 1),
 # Back: a cut across each slab.
 ('back-plank-left', 'back', BARK_DARK, [(0.024, 0.420), (0.140, 0.428), (0.140, 0.434), (0.024, 0.426)], 1),
 ('back-plank-right', 'back', BARK_DARK, [(-0.136, 0.392), (-0.030, 0.386), (-0.030, 0.392), (-0.136, 0.398)], 1),
 ('small-of-back', 'back', BARK_DARK, [(-0.050, 0.290), (0.040, 0.294), (0.040, 0.299), (-0.050, 0.295)], 1),
 ('thigh-plank-left-d', 'back', BARK_DARK, [(0.086, 0.186), (0.091, 0.186), (0.092, 0.270), (0.087, 0.270)], 1),
 ('shin-plank-right-d', 'back', BARK_DARK, [(-0.084, 0.050), (-0.079, 0.050), (-0.080, 0.130), (-0.085, 0.130)], 1),
])

BODY_DECALS = ENGRAVE_BODY
HEAD_DECALS = FACE + ENGRAVE_HEAD

DONOR_SPACE = ()


# ---------------------------------------------------------------------------
# § FORMS: the low-poly parts (branches, vines, leaves), each typed by hand. Table space.
# ---------------------------------------------------------------------------
def _rotate(p, yaw, pitch, roll):
    """Local point rotated by roll (about x), then pitch (about z), then yaw (about y). Degrees."""
    x, y, z = p
    r = math.radians(roll)
    y, z = y * math.cos(r) - z * math.sin(r), y * math.sin(r) + z * math.cos(r)
    q = math.radians(pitch)
    x, y = x * math.cos(q) - y * math.sin(q), x * math.sin(q) + y * math.cos(q)
    w = math.radians(yaw)
    x, z = x * math.cos(w) + z * math.sin(w), -x * math.sin(w) + z * math.cos(w)
    return (x, y, z)


def _leaf(centre, length, width, thickness, yaw, pitch, roll=0.0):
    """One leaf: a flat six-sided prism, blunt at the stem and pointed at the tip.

    ⚠️ A PRISM, NOT A CARD. A single quad has no back faces for the inverted-hull outline to draw,
    so it would be the one un-inked thing on the model. 5 to 7 mm thick reads as a leaf at
    lineup distance and still takes the ink.
    """
    outline = [(-0.50 * length, 0.0), (-0.22 * length, 0.42 * width), (0.18 * length, 0.50 * width),
               (0.50 * length, 0.0), (0.18 * length, -0.50 * width), (-0.22 * length, -0.42 * width)]
    top = [_rotate((a, 0.5 * thickness, b), yaw, pitch, roll) for a, b in outline]
    bottom = [_rotate((a, -0.5 * thickness, b), yaw, pitch, roll) for a, b in outline]
    top = [tuple(centre[i] + p[i] for i in range(3)) for p in top]
    bottom = [tuple(centre[i] + p[i] for i in range(3)) for p in bottom]
    middle = _rafi_mean(top + bottom)
    faces = [_rafi_orient(top, _rafi_sub(_rafi_mean(top), middle)),
             _rafi_orient(bottom, _rafi_sub(_rafi_mean(bottom), middle))]
    for i in range(6):
        k = (i + 1) % 6
        side = [top[i], top[k], bottom[k], bottom[i]]
        faces.append(_rafi_orient(side, _rafi_sub(_rafi_mean(side), middle)))
    return faces


def _branch(path, radii, sides=6):
    """A branch as a tapered faceted tube: one hand-set radius per point, capped at both ends."""
    rings, tangents = [], []
    for i, centre in enumerate(path):
        previous = path[max(0, i - 1)]
        following = path[min(len(path) - 1, i + 1)]
        tangent = _unit(_rafi_sub(following, previous))
        helper = (0.0, 0.0, 1.0) if abs(tangent[2]) < 0.9 else (1.0, 0.0, 0.0)
        normal = _unit(_cross(tangent, helper))
        side = _unit(_cross(tangent, normal))
        tangents.append(tangent)
        rings.append([tuple(centre[k] + radii[i] * (normal[k] * math.cos(j * math.tau / sides)
                                                    + side[k] * math.sin(j * math.tau / sides))
                            for k in range(3)) for j in range(sides)])
    faces = []
    for i in range(len(path) - 1):
        middle = _rafi_mean([path[i], path[i + 1]])
        for j in range(sides):
            k = (j + 1) % sides
            quad = [rings[i][j], rings[i][k], rings[i + 1][k], rings[i + 1][j]]
            faces.append(_rafi_orient(quad, _rafi_sub(_rafi_mean(quad), middle)))
    faces.append(_rafi_orient(rings[0], tuple(-v for v in tangents[0])))
    faces.append(_rafi_orient(rings[-1], tangents[-1]))
    return faces


def _forms(part):
    """Every horn, root, vine and leaf, typed one by one. `part` is 'head' or 'body'.

    ⚠️ HORNS AND ROOTS ARE SQUARE IN SECTION WITH RIGHT-ANGLE ELBOWS (the board's BRANCH HORNS),
    four sides each, so they read as carved blocks rather than smooth sticks. A root ends with its
    centre one radius off the floor so it never goes below zero.
    """
    if part == 'head':
        # Left horn: up out of the head's side plank, an elbow outward, up again; a tine off the
        # elbow and a spur near the top.
        yield 'head', BARK, _branch([(0.086, 0.664, 0.000), (0.094, 0.700, 0.000), (0.150, 0.708, 0.000),
                                     (0.160, 0.746, 0.000), (0.156, 0.784, 0.000)],
                                    [0.030, 0.026, 0.023, 0.019, 0.014], sides=4)
        yield 'head', BARK_LIT, _branch([(0.120, 0.705, 0.000), (0.116, 0.752, 0.006), (0.100, 0.778, 0.006)],
                                        [0.017, 0.014, 0.010], sides=4)
        yield 'head', BARK, _branch([(0.158, 0.744, 0.000), (0.198, 0.752, 0.000), (0.206, 0.772, 0.000)],
                                    [0.014, 0.012, 0.009], sides=4)
        # Right horn: its elbow is higher and it reaches further out.
        yield 'head', BARK, _branch([(-0.086, 0.662, 0.004), (-0.098, 0.706, 0.004), (-0.160, 0.716, 0.000),
                                     (-0.170, 0.752, 0.004), (-0.162, 0.786, 0.006)],
                                    [0.031, 0.026, 0.022, 0.018, 0.013], sides=4)
        yield 'head', BARK_LIT, _branch([(-0.128, 0.712, 0.002), (-0.126, 0.756, -0.004), (-0.110, 0.780, -0.004)],
                                        [0.016, 0.013, 0.009], sides=4)
        yield 'head', BARK, _branch([(-0.170, 0.750, 0.004), (-0.210, 0.744, 0.004), (-0.222, 0.764, 0.004)],
                                    [0.014, 0.011, 0.008], sides=4)
        # Leaves on the horns and out of the crown moss, each its own size and angle.
        yield 'head', LEAF, _leaf((0.212, 0.774, 0.004), 0.037, 0.023, 0.006, 30, 34, 35)
        yield 'head', LEAF_DARK, _leaf((0.096, 0.782, 0.008), 0.029, 0.018, 0.005, 150, 24, -40)
        yield 'head', LEAF, _leaf((-0.228, 0.766, 0.004), 0.037, 0.023, 0.006, 160, 30, 50)
        yield 'head', LEAF_DARK, _leaf((-0.104, 0.782, -0.004), 0.029, 0.018, 0.005, 60, 20, -30)
        yield 'head', LEAF, _leaf((0.028, 0.692, 0.030), 0.064, 0.038, 0.007, -50, 42, 55)
        yield 'head', LEAF_DARK, _leaf((-0.034, 0.690, 0.050), 0.058, 0.035, 0.007, 130, 38, -45)
        yield 'head', LEAF, _leaf((0.004, 0.694, -0.020), 0.052, 0.032, 0.006, -100, 50, 30)
        return
    # Roots: four per foot, square, each its own path off the foot into the ground.
    yield 'leg-left', ROOT, _branch([(0.120, 0.030, -0.080), (0.150, 0.023, -0.130), (0.164, 0.015, -0.164)],
                                    [0.030, 0.022, 0.014], sides=4)
    yield 'leg-left', ROOT, _branch([(0.050, 0.030, -0.090), (0.040, 0.023, -0.140), (0.030, 0.015, -0.170)],
                                    [0.030, 0.021, 0.014], sides=4)
    yield 'leg-left', ROOT, _branch([(0.150, 0.030, 0.000), (0.192, 0.017, 0.010), (0.214, 0.010, 0.018)],
                                    [0.018, 0.013, 0.009], sides=4)
    yield 'leg-left', ROOT, _branch([(0.090, 0.030, 0.070), (0.100, 0.016, 0.110), (0.104, 0.010, 0.130)],
                                    [0.016, 0.012, 0.009], sides=4)
    yield 'leg-right', ROOT, _branch([(-0.110, 0.030, -0.086), (-0.126, 0.023, -0.136), (-0.140, 0.015, -0.172)],
                                     [0.031, 0.022, 0.014], sides=4)
    yield 'leg-right', ROOT, _branch([(-0.040, 0.030, -0.080), (-0.020, 0.017, -0.124), (-0.012, 0.010, -0.150)],
                                     [0.018, 0.013, 0.009], sides=4)
    yield 'leg-right', ROOT, _branch([(-0.150, 0.030, -0.010), (-0.194, 0.018, -0.020), (-0.220, 0.010, -0.024)],
                                     [0.020, 0.015, 0.009], sides=4)
    yield 'leg-right', ROOT, _branch([(-0.070, 0.030, 0.068), (-0.058, 0.016, 0.108), (-0.052, 0.010, 0.132)],
                                     [0.016, 0.012, 0.009], sides=4)
    # ⚠️ v16: FROM THE ELBOW DOWN EACH ARM IS VINES, TANGLED. The owner: *"none of our characters have
    # fingers so try to make his arms js look like pointy vines tangling on each other"*. v15 kept
    # the square forearm and hung thin strands off it, which read as an afterthought. Now four
    # strands leave the elbow block thick (28 to 34 mm), twist round each other along the arm and
    # taper to separate points past the wrist; one thin tendril curls off. Two strands are bark,
    # one light bark, one green; every point and radius is typed, and the two arms twist differently.
    #
    # ⚠️ THE BARK STRANDS ARE IN THE SKIN SLOTS ON PURPOSE: `CharacterVisual.PalmCentre` finds the
    # hand as the far end of the arm's skin-slot vertices, so the slipper anchor lands in the braid.
    yield 'arm-left', BARK, _branch([(0.360, 0.500, -0.036), (0.404, 0.506, 0.010), (0.448, 0.476, 0.044),
                                     (0.492, 0.440, 0.022), (0.534, 0.438, -0.020), (0.572, 0.458, -0.030),
                                     (0.604, 0.474, -0.012), (0.630, 0.478, 0.004)],
                                    [0.034, 0.031, 0.028, 0.025, 0.020, 0.015, 0.009, 0.004])
    yield 'arm-left', BARK_LIT, _branch([(0.360, 0.440, 0.030), (0.404, 0.434, -0.012), (0.448, 0.458, -0.046),
                                         (0.492, 0.494, -0.030), (0.534, 0.502, 0.012), (0.570, 0.486, 0.034),
                                         (0.600, 0.466, 0.022), (0.624, 0.456, 0.008)],
                                        [0.032, 0.030, 0.027, 0.024, 0.019, 0.014, 0.009, 0.004])
    yield 'arm-left', BARK, _branch([(0.362, 0.470, 0.050), (0.404, 0.486, 0.052), (0.446, 0.508, 0.014),
                                     (0.488, 0.494, -0.030), (0.528, 0.462, -0.040), (0.562, 0.440, -0.014),
                                     (0.590, 0.440, 0.010), (0.612, 0.448, 0.020)],
                                    [0.028, 0.026, 0.024, 0.021, 0.017, 0.012, 0.008, 0.004])
    yield 'arm-left', VINE, _branch([(0.362, 0.476, -0.054), (0.406, 0.450, -0.050), (0.448, 0.434, -0.010),
                                     (0.490, 0.450, 0.036), (0.530, 0.484, 0.040), (0.566, 0.504, 0.008),
                                     (0.596, 0.496, -0.016), (0.616, 0.484, -0.020)],
                                    [0.024, 0.023, 0.021, 0.019, 0.016, 0.012, 0.008, 0.004])
    yield 'arm-left', VINE, _branch([(0.520, 0.500, 0.036), (0.548, 0.524, 0.056), (0.566, 0.542, 0.044),
                                     (0.574, 0.550, 0.028)], [0.009, 0.007, 0.005, 0.003])
    yield 'arm-right', BARK, _branch([(-0.362, 0.444, -0.034), (-0.406, 0.438, 0.012), (-0.450, 0.462, 0.046),
                                      (-0.494, 0.498, 0.030), (-0.536, 0.504, -0.010), (-0.574, 0.486, -0.034),
                                      (-0.606, 0.466, -0.024), (-0.632, 0.458, -0.008)],
                                     [0.034, 0.031, 0.028, 0.025, 0.020, 0.015, 0.009, 0.004])
    yield 'arm-right', BARK_LIT, _branch([(-0.360, 0.500, 0.032), (-0.402, 0.508, -0.010), (-0.446, 0.484, -0.046),
                                          (-0.490, 0.448, -0.034), (-0.532, 0.436, 0.006), (-0.568, 0.448, 0.032),
                                          (-0.600, 0.468, 0.026), (-0.626, 0.480, 0.010)],
                                         [0.032, 0.030, 0.027, 0.023, 0.019, 0.014, 0.009, 0.004])
    yield 'arm-right', BARK, _branch([(-0.364, 0.470, -0.052), (-0.406, 0.456, -0.054), (-0.448, 0.436, -0.018),
                                      (-0.490, 0.444, 0.028), (-0.528, 0.474, 0.042), (-0.560, 0.500, 0.020),
                                      (-0.588, 0.502, -0.006), (-0.610, 0.494, -0.018)],
                                     [0.028, 0.026, 0.024, 0.021, 0.017, 0.012, 0.008, 0.004])
    yield 'arm-right', VINE, _branch([(-0.362, 0.470, 0.056), (-0.404, 0.496, 0.048), (-0.446, 0.510, 0.006),
                                      (-0.488, 0.494, -0.036), (-0.528, 0.462, -0.042), (-0.564, 0.440, -0.010),
                                      (-0.594, 0.444, 0.016), (-0.614, 0.456, 0.022)],
                                     [0.024, 0.023, 0.021, 0.019, 0.016, 0.012, 0.008, 0.004])
    yield 'arm-right', VINE, _branch([(-0.530, 0.440, -0.040), (-0.556, 0.420, -0.058), (-0.572, 0.404, -0.046),
                                      (-0.580, 0.398, -0.030)], [0.009, 0.007, 0.005, 0.003])
    # The torso vine: over his right shoulder, down across the chest under the boss, round his
    # left side and onto the back. Thick, the board's.
    yield 'torso', VINE, _branch([(-0.152, 0.476, 0.060), (-0.156, 0.470, -0.080), (-0.090, 0.428, -0.148),
                                  (-0.010, 0.344, -0.146), (0.070, 0.296, -0.126), (0.146, 0.262, -0.088),
                                  (0.176, 0.252, 0.000), (0.150, 0.256, 0.090), (0.060, 0.272, 0.112)],
                                 [0.014, 0.015, 0.016, 0.016, 0.015, 0.015, 0.014, 0.013, 0.012])
    # A small branch sprouting from his back, off the spine plank, with its leaves.
    yield 'torso', BARK, _branch([(-0.010, 0.420, 0.128), (-0.030, 0.450, 0.168), (-0.070, 0.470, 0.186),
                                  (-0.084, 0.500, 0.190)], [0.016, 0.013, 0.010, 0.007], sides=4)
    yield 'torso', LEAF, _leaf((-0.090, 0.512, 0.192), 0.050, 0.030, 0.007, 120, 40, 30)
    yield 'torso', LEAF_DARK, _leaf((-0.050, 0.466, 0.196), 0.044, 0.026, 0.007, 60, 20, -40)
    # A vine coiling up his right leg.
    yield 'leg-right', VINE, _branch([(-0.030, 0.040, -0.080), (-0.080, 0.070, -0.100), (-0.150, 0.100, -0.084),
                                      (-0.168, 0.140, 0.000), (-0.130, 0.180, 0.084), (-0.050, 0.210, 0.086),
                                      (-0.030, 0.240, 0.000)],
                                     [0.011, 0.012, 0.012, 0.012, 0.011, 0.011, 0.010])
    # ⚠️ v12: LEAVES GROW IN CLUSTERS, three or four together, never scattered singly (v11's
    # singles read as confetti). Four clusters on the body: each shoulder, the collar, the left hip.
    # Each leaf typed with its own size, angle and roll.
    yield 'arm-left', LEAF, _leaf((0.186, 0.566, -0.030), 0.074, 0.046, 0.008, 10, 42, -40)
    yield 'arm-left', LEAF_DARK, _leaf((0.214, 0.562, 0.024), 0.066, 0.040, 0.008, -40, 34, 36)
    yield 'arm-left', LEAF, _leaf((0.160, 0.560, 0.030), 0.060, 0.036, 0.007, 120, 30, -30)
    yield 'arm-left', MOSS_LIT, _leaf((0.236, 0.556, -0.040), 0.052, 0.032, 0.007, -10, 26, 44)
    yield 'arm-right', LEAF, _leaf((-0.186, 0.564, 0.030), 0.074, 0.046, 0.008, 170, 40, -44)
    yield 'arm-right', LEAF_DARK, _leaf((-0.214, 0.558, -0.030), 0.062, 0.038, 0.007, -150, 30, 34)
    yield 'arm-right', LEAF, _leaf((-0.156, 0.558, -0.040), 0.056, 0.034, 0.007, 60, 28, 40)
    yield 'torso', LEAF, _leaf((0.110, 0.504, -0.084), 0.070, 0.044, 0.008, -60, 34, 40)
    yield 'torso', LEAF_DARK, _leaf((0.136, 0.500, -0.054), 0.062, 0.038, 0.007, -10, 38, -36)
    yield 'torso', LEAF, _leaf((0.082, 0.498, -0.104), 0.056, 0.034, 0.007, -110, 26, 46)
    yield 'torso', LEAF, _leaf((0.112, 0.262, -0.104), 0.060, 0.036, 0.007, -80, -16, -40)
    yield 'torso', LEAF_DARK, _leaf((0.132, 0.252, -0.084), 0.052, 0.032, 0.006, -20, -8, 46)
    yield 'torso', MOSS_LIT, _leaf((0.094, 0.248, -0.110), 0.046, 0.028, 0.006, -130, -20, 30)

BODY_BOXES = LEG_LEFT + LEG_RIGHT + TORSO + ARM_LEFT + ARM_RIGHT
HEAD_BOXES = HEAD

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
# ⚠️ THE TABLES ARE REMAPPED, NOT REWRITTEN. Every box carries a measurement and a reason:
# the sole's thickness, the chain's link gap, the hand's height against `HandTopLift`.
# Re-authoring them by hand against new joint heights loses all of it and is wrong in ways
# only a turnaround catches. This moves each REGION onto its family value and leaves every
# relationship inside a region exactly as measured.
# ---------------------------------------------------------------------------

# Source joint heights, as the tables above are authored.
# ⚠️ PAETE IS AUTHORED ON THE BASE RIG ITSELF, so the source heights ARE the family heights and
# the remap below is the identity. See the note at the top of his tables.
WAS_HIPS, WAS_SHOULDER, WAS_NECK, WAS_TOP = 0.176, 0.288, 0.343, 0.7234

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


# ⚠️ THERE IS NO DONATED HEAD. The native skull is a person's; his head is a block of carved
# planks, typed in `HEAD` above (brief section 5). The donor machinery Amihan and Rafi carry
# (`_donor_part`, the expression guard) is left out rather than kept unused.

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
# lower-z quad and `FACES[1]` the upper one, so under the flip "front" is entry 1, while
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
# voxel language (flat facets, one palette slot per box, the same table above unchanged)
# while removing exactly the thing that reads as Minecraft, which is the hard 90.
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
# corners of the face panel hanging in space outside it: the same class of fault as the
# z-fighting `SKIPPABLE` was written to fix, arrived at from the other side. Four boxes
# carry a skip; they keep their corners and nobody can see them, because the only skipped
# face on the model is the one the face is drawn on.
#
# ⚠️ THE SIZE IS PROPORTIONAL, WITH A CEILING. A flat 20 mm cut is most of a chain link
# and nothing at all on the torso, so it is a fraction of the box's own smallest half
# extent, capped so the large masses do not turn into gems. The cap is what keeps the
# skull a head.
# ⚠️⚠️ 0.45 IS NEARLY A CAPSULE AND THAT IS THE POINT. 🧑 2026-08-18: *"still to blocky
# btw"*. At 0.34 a limb 124 mm thick got a 21 mm cut: enough to kill the hard 90 and not
# enough to read as ROUND, and round is what the cast is.
#
# ⚠️ IT MUST STAY BELOW 0.5. The bevel is measured from each corner inward, so at half the
# extent opposing cuts meet and the box turns inside out. The fraction IS the clamp.
BEVEL_FRACTION = 0.45

# ⚠️⚠️ THE CAP IS WHAT THE BIG MASSES HIT, AND 0.030 LEFT THE JAW A CORNER. 🧑 2026-08-18,
# after the first chamfer pass: *"the face itself as well is too sharp, look chin and
# stuff"*. Only the largest boxes reach the cap at all (the skull, the hair crown, the
# jacket), and those are exactly the ones whose silhouette is the character. At 0.030 the
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
    polygon inside out, which renders as a hole in the model, not as an error. Sorting
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


def _rafi_sub(a, b):
    return tuple(a[i]-b[i] for i in range(3))


def _rafi_mean(points):
    return tuple(sum(p[i] for p in points)/len(points) for i in range(3))


def _rafi_orient(points, outward):
    points=list(points)
    normal=_cross(_rafi_sub(points[1],points[0]),_rafi_sub(points[2],points[0]))
    return list(reversed(points)) if _dot(normal,outward)<0 else points


def _rafi_loft(sections):
    """Connected chamfered sections, ordered top to bottom, in native head space.

    Each section is (y, centre_x, centre_z, width, depth). Broad roots and narrower
    bent tips make a connected cloth or fastening volume. Hair uses native boxes.
    """
    outline=[(-.38,-.5),(.38,-.5),(.5,-.38),(.5,.38),
             (.38,.5),(-.38,.5),(-.5,.38),(-.5,-.38)]
    rings=[[(x+u*w,y,z+v*d) for u,v in outline] for y,x,z,w,d in sections]
    faces=[_rafi_orient(rings[0],(0,1,0)),_rafi_orient(rings[-1],(0,-1,0))]
    for i in range(len(rings)-1):
        centre=_rafi_mean([_rafi_mean(rings[i]),_rafi_mean(rings[i+1])])
        for j in range(8):
            k=(j+1)%8
            poly=[rings[i][j],rings[i][k],rings[i+1][k],rings[i+1][j]]
            faces.append(_rafi_orient(poly,_rafi_sub(_rafi_mean(poly),centre)))
    return faces


def _rafi_tube(path, radius, plane_normal, closed=True, sides=6):
    """Continuous faceted cord with real inner clearance and a clean silhouette."""
    rings=[];tangents=[]
    for i,centre in enumerate(path):
        previous=path[(i-1)%len(path)] if closed or i>0 else path[i]
        following=path[(i+1)%len(path)] if closed or i+1<len(path) else path[i]
        tangent=_unit(_rafi_sub(following,previous));tangents.append(tangent)
        normal=_unit(_cross(tangent,plane_normal));side=_unit(_cross(tangent,normal))
        rings.append([tuple(centre[k]+radius*(normal[k]*math.cos(j*math.tau/sides)
                         +side[k]*math.sin(j*math.tau/sides)) for k in range(3)) for j in range(sides)])
    faces=[]
    for i in range(len(path) if closed else len(path)-1):
        following=(i+1)%len(path);centre=_rafi_mean([path[i],path[following]])
        for j in range(sides):
            k=(j+1)%sides
            poly=[rings[i][j],rings[i][k],rings[following][k],rings[following][j]]
            faces.append(_rafi_orient(poly,_rafi_sub(_rafi_mean(poly),centre)))
    if not closed:
        faces += [_rafi_orient(rings[0],tuple(-v for v in tangents[0])),
                  _rafi_orient(rings[-1],tangents[-1])]
    return faces


def _box_facets(name, lo, hi, skip):
    """Every facet of one box in FILE space: chamfered, tapered and tilted.

    `lo`/`hi` are already flipped to the file's +Z front. Split out of `build_mesh` so the
    decals project onto exactly the surface the mesh draws, taper and tilt included; a
    decal measured against the untapered box would float off the V of the chest.
    """
    for axis in range(3):
        if hi[axis] <= lo[axis]:
            raise SystemExit(f"box '{name}' is inside out on axis {axis}")

    # ⚠️ A BOX THAT DROPS A FACE STAYS SQUARE. See the chamfer block: the face panel
    # is a full rectangle drawn into the plane of the wall this removes, and an
    # octagonal hole leaves its corners outside the model.
    bevel = 0.0 if skip >= 0 else bevel_for(lo, hi)

    angle = math.radians(BOX_TILTS.get(name, 0))
    cosine, sine = math.cos(angle), math.sin(angle)
    centre = tuple((lo[i] + hi[i]) * .5 for i in range(3))
    taper = BOX_TAPERS.get(name)
    out = []

    for normal, points in box_polygons(lo, hi, skip, bevel):
        if taper:
            driving, narrowed, low_width, high_width = taper
            shaped = []
            for point in points:
                p = list(point)
                t = (p[driving] - lo[driving]) / (hi[driving] - lo[driving])
                p[narrowed] = centre[narrowed] + (p[narrowed] - centre[narrowed]) * (low_width + (high_width - low_width) * t)
                shaped.append(tuple(p))
            points = shaped
            # Recompute the changed polygon normal before the existing
            # chamfer/outline smoothing stages consume it.
            n = [0., 0., 0.]
            for a, b in zip(points, points[1:] + points[:1]):
                n[0] += (a[1] - b[1]) * (a[2] + b[2])
                n[1] += (a[2] - b[2]) * (a[0] + b[0])
                n[2] += (a[0] - b[0]) * (a[1] + b[1])
            normal = _unit(tuple(n))
        if angle:
            normal = (normal[0] * cosine - normal[1] * sine,
                      normal[0] * sine + normal[1] * cosine, normal[2])
            turned = []
            for p in points:
                x, y = p[0] - centre[0], p[1] - centre[1]
                turned.append((centre[0] + x * cosine - y * sine, centre[1] + x * sine + y * cosine, p[2]))
            points = turned
        turn = BOX_TURNS.get(name)
        if turn:
            # Yaw about Y, then roll about X, both about the box centre (file space).
            yaw, roll = (math.radians(a) for a in turn)
            def spin(v, about):
                x, y, z = v[0] - about[0], v[1] - about[1], v[2] - about[2]
                x, z = x * math.cos(yaw) + z * math.sin(yaw), -x * math.sin(yaw) + z * math.cos(yaw)
                y, z = y * math.cos(roll) - z * math.sin(roll), y * math.sin(roll) + z * math.cos(roll)
                return (x + about[0], y + about[1], z + about[2])
            points = [spin(p, centre) for p in points]
            normal = spin(normal, (0.0, 0.0, 0.0))
        out.append((normal, points))
    return out


# Where each decal view's projector stands, as the direction it shines in TABLE space, and
# how a view's 2D polygon becomes a 3D point on its plane.
_DECAL_VIEWS = {
    'front': ((0, 0, 1), lambda a, b: (a, b, 0.0)),
    'back': ((0, 0, -1), lambda a, b: (a, b, 0.0)),
    'left': ((-1, 0, 0), lambda a, b: (0.0, b, a)),
    'right': ((1, 0, 0), lambda a, b: (0.0, b, a)),
    'top': ((0, -1, 0), lambda a, b: (a, 0.0, b)),
}


def _clip_convex(subject, clipper):
    """Sutherland-Hodgman: `subject` clipped to the convex, counter-clockwise `clipper`."""
    out = list(subject)
    for (ax, ay), (bx, by) in zip(clipper, clipper[1:] + clipper[:1]):
        if not out:
            break
        source, out = out, []

        def side(p):
            return (bx - ax) * (p[1] - ay) - (by - ay) * (p[0] - ax)

        for i, current in enumerate(source):
            previous = source[i - 1]
            sc, sp = side(current), side(previous)
            if sc >= -1e-12:
                if sp < -1e-12:
                    t = sp / (sp - sc)
                    out.append((previous[0] + (current[0] - previous[0]) * t,
                                previous[1] + (current[1] - previous[1]) * t))
                out.append(current)
            elif sp >= -1e-12:
                t = sp / (sp - sc)
                out.append((previous[0] + (current[0] - previous[0]) * t,
                            previous[1] + (current[1] - previous[1]) * t))
    return out


def _area2(points):
    return sum(a[0] * b[1] - b[0] * a[1] for a, b in zip(points, points[1:] + points[:1]))


def _project_decal(facets, view, polygon, lift, layer):
    """One decal polygon, projected onto a box's facets. Yields (normal, points) pieces.

    `lift` turns an authored (table space) point into a file space one: the family remap
    and the Z flip. The projector direction goes through the same flip.
    """
    direction, to_3d = _DECAL_VIEWS[view]
    d = lift(direction, vector=True)
    k = max(range(3), key=lambda i: abs(d[i]))
    u, v = [i for i in range(3) if i != k]
    shape = [lift(to_3d(a, b)) for a, b in polygon]
    shape = [(p[u], p[v]) for p in shape]
    if _area2(shape) < 0:
        shape.reverse()

    for normal, points in facets:
        if _dot(normal, d) > -0.02:
            continue
        clipper = [(p[u], p[v]) for p in points]
        if abs(_area2(clipper)) < 1e-12:
            continue
        if _area2(clipper) < 0:
            clipper.reverse()
        piece = _clip_convex(shape, clipper)
        if len(piece) < 3 or abs(_area2(piece)) < 1e-10:
            continue
        # Lift each 2D point onto this facet's plane along the projector axis, then stand it
        # `layer` steps of PANEL_PROUD off the skin along the facet normal.
        plane = _dot(normal, points[0])
        lifted = []
        for a, b in piece:
            p = [0.0, 0.0, 0.0]
            p[u], p[v] = a, b
            p[k] = (plane - normal[u] * a - normal[v] * b) / normal[k]
            lifted.append(tuple(p[i] + normal[i] * PANEL_PROUD * layer for i in range(3)))
        yield normal, lifted


def build_mesh(boxes, panels=(), donor=None, decals=(), part="body"):
    """Boxes, decals, pixel panels and an optional donated mesh, to flat glTF arrays."""
    pos, nrm, uv, joints, weights, idx = [], [], [], [], [], []

    # Which vertices came from a pixel panel rather than from a box. See smooth_normals:
    # they are held out of the averaging in both directions.
    panel_indices = []
    facets_by_box = {}

    for entry in boxes:
        name, bone, lo, hi, slot = entry[:5]
        skip = SKIPPABLE[entry[5]] if len(entry) > 5 else -1

        if FRONT_IS_MINUS_Z:
            lo, hi = (lo[0], lo[1], -hi[2]), (hi[0], hi[1], -lo[2])

        j = BONE[bone]
        u, v = cell_uv(slot)
        facets = _box_facets(name, lo, hi, skip)
        facets_by_box[name] = (bone, facets)

        for normal, points in facets:
            first = len(pos)
            for p in points:
                pos.append(p)
                nrm.append(normal)
                uv.append((u, v))
                joints.append((j, 0, 0, 0))
                weights.append((1.0, 0.0, 0.0, 0.0))

            # ⚠️ A FAN, BECAUSE THE FACES ARE NO LONGER ALL QUADS. Every polygon here is
            # planar and convex (an octagon, a rectangle or a triangle) so a fan from
            # its first vertex is exact rather than an approximation.
            for k in range(1, len(points) - 1):
                idx += [first, first + k, first + k + 1]

    # § THE DECALS. See the note above `_quad`. Body only: every decal is authored in table
    # space, so it goes through the same remap as the box it lands on.
    for box, view, slot, polygon, layer in decals:
        if donor is not None:
            raise SystemExit(f"decal on '{box}': decals are body-only")
        if box not in facets_by_box:
            raise SystemExit(f"decal targets '{box}', which is not a box in this mesh")
        bone, facets = facets_by_box[box]

        def lift(p, vector=False, bone=bone):
            x, y, z = p
            if not vector:
                y = y + NOW_SHOULDER - WAS_SHOULDER if bone.startswith('arm-') else _remap_y(y)
            return (x, y, -z) if FRONT_IS_MINUS_Z else (x, y, z)

        drawn = 0
        for normal, points in _project_decal(facets, view, polygon, lift, layer):
            first = len(pos)
            for p in points:
                panel_indices.append(len(pos))
                pos.append(p)
                nrm.append(normal)
                uv.append(cell_uv(slot))
                joints.append((BONE[bone], 0, 0, 0))
                weights.append((1.0, 0.0, 0.0, 0.0))
            # Wind it to face out along the facet it sits on, whatever the flip did. Newell's
            # sum rather than the first corner: clipping can leave the first three collinear.
            newell = [0.0, 0.0, 0.0]
            for a, b in zip(points, points[1:] + points[:1]):
                newell[0] += (a[1] - b[1]) * (a[2] + b[2])
                newell[1] += (a[2] - b[2]) * (a[0] + b[0])
                newell[2] += (a[0] - b[0]) * (a[1] + b[1])
            outward = _dot(newell, normal) >= 0
            for k in range(1, len(points) - 1):
                idx += ([first, first + k, first + k + 1] if outward
                        else [first, first + k + 1, first + k])
            drawn += 1
        if not drawn:
            raise SystemExit(f"decal on '{box}' ({view}) {polygon[:2]}... missed the box entirely")

    for bone,slot,faces in _forms(part):
        def remap(point):
            x,y,z=point
            if donor is None:
                y=y+NOW_SHOULDER-WAS_SHOULDER if bone.startswith('arm-') else _remap_y(y)
                if FRONT_IS_MINUS_Z:z=-z
            return x,y,z
        for face in faces:
            points=[remap(point) for point in face]
            if donor is None and FRONT_IS_MINUS_Z:points.reverse()
            normal=_unit(_cross(_rafi_sub(points[1],points[0]),_rafi_sub(points[2],points[0])))
            first=len(pos)
            for point in points:
                pos.append(point);nrm.append(normal);uv.append(cell_uv(slot))
                joints.append((BONE[bone],0,0,0));weights.append((1.,0.,0.,0.))
            for i in range(1,len(points)-1):idx.extend((first,first+i,first+i+1))

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
                # skull: the hole had to be covered edge to edge or the head had a window
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
    # The donor arrives with its own authored normals: it is a smooth low-poly head and
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
    the outline pass (an inverted hull that pushes along exactly these normals) grows a
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
    if not RAFI_RECIPE_READY:
        raise SystemExit("Rafi voxel recipe is still being retrofitted; do not run the rejected builder.")
    if not os.path.exists(BASE):
        raise SystemExit(f"base rig not found: {BASE}")

    gltf, buffer = read_glb(BASE)

    deltas = retarget(gltf, buffer)
    bind_matrices(gltf)

    # § THE FAMILY PASS, applied to the authored tables on the way into the mesh. See
    # `_family`: the tables stay as measured and the REGIONS move onto the base rig's own
    # proportions, so this character stands in the line-up as one of the cast.
    body = build_mesh(_family(BODY_BOXES, head=False), decals=BODY_DECALS, part="body")
    head = build_mesh(_family(HEAD_BOXES, head=True, as_authored=DONOR_SPACE),
                      decals=HEAD_DECALS, part="head")

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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso Paete native voxel builder"}

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
    resolve_slide(OUT)


# ⚠️⚠️ THE SLIDE IS RE-SOLVED FOR THIS MESH ON EVERY BUILD (2026-09-25). This builder starts from
# the base rig's `.glb`, so the `slide` clip it carries was solved by
# `tools/author_retrieval_slide.py` for the BASE mesh: pelvis height set from the base's own
# lowest vertex. Rafi's mane and tail stand far behind his head, and leaning back through that
# base solve put his hair 0.112 m through the street (`ClipMotionStrip`, v29 and v35), where
# Sean and Dante, whose slides were solved on their own rigs, read -0.006 and 0.000. Re-solved
# on his mesh: floor 0.000 at every sample, body drop 12.8 per cent of height (floor 12),
# reach 4.75 per cent (ceiling 10), by the independent `verify_retrieval_slide.py`.
BLENDER_CANDIDATES = (
    r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe",
    "/Applications/Blender.app/Contents/MacOS/Blender",
)


def resolve_slide(path):
    import subprocess
    blender = next((b for b in BLENDER_CANDIDATES if os.path.exists(b)), None)
    if blender is None:
        print("\n⚠️⚠️ BLENDER 5.2 NOT FOUND: the slide was NOT re-solved for this mesh. Run\n"
              "  blender --background --python tools/author_retrieval_slide.py -- "
              f"{path} --replace\nbefore shipping, or his hair goes through the street.\n")
        return
    result = subprocess.run([blender, "--background", "--python", "tools/author_retrieval_slide.py",
                             "--", path, "--replace"], capture_output=True, text=True)
    line = next((l for l in result.stdout.splitlines() if l.startswith("SLIDE_REPORT")), None)
    if result.returncode != 0 or line is None:
        raise SystemExit("slide re-solve failed:\n" + result.stdout[-2000:] + result.stderr[-2000:])
    print("slide re-solved: " + line[len("SLIDE_REPORT "):][:160])


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
    # the file nor the model: it fired on `hair-fringe` the first time this ran, for a
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
            f"  Slot 8 draws the eyes and mouth. A light slot 8 does not give a\n"
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
