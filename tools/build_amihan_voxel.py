# Amihan's own builder: a copy of tools/build_rafi_voxel.py at its 2026-09-25 islander rework
# (itself a copy of build_person_voxel.py), for its projected decals and per-mesh slide solve.
# Neither original is touched. Brief: ArtSource/amihan/concept-20260925/design-brief.md.
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

OUT = "Assets/TumbangPreso/Art/characters/persons/team-amihan.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-amihan.tres"

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
# ⚠️⚠️ AMIHAN, THE EIGHTH HERO (owner, 2026-09-25): "she comes from vigan city and is a wind
# character", from a concept sheet: "use this as reference u can change facial expression".
# Brief, research and lore: `ArtSource/amihan/concept-20260925/design-brief.md`. Method:
# `docs/CHARACTER_MODEL_METHOD.md`. This builder is a copy of Rafi's (the projected decals and
# the per-mesh slide solve come with it); nothing here touches another hero.
#
# What she wears, and why:
#   a cream wrap robe       Abel Iloko cotton, Vigan's living craft, undyed
#   a capelet and brooch    over the shoulders, from the concept; the KASIKUS on its back
#   kasikus                 the binakol weave's "whirlwind": graduated diamonds radiating from
#                           a centre, woven to turn spirits away. Her family's loom, her wind.
#   a rust belt and sash    the concept's belt; the sash carries a small binakol zigzag
#   cotton bolls            the concept's white hairpin flowers, read as cotton bolls with woven
#                           tassels: the fibre the Binatbatan festival beats for the loom
#   thick cream sandals     the cast's grounding sole
#
# ⚠️ HER COLOUR IS A SWITCH UNTIL THE OWNER PICKS. The concept's teal sits in the same family
# as Rafi's sea teal, and a hero owns a hue no other hero wears (method, section 3). So
# `AMIHAN_CLOTH=teal` builds the concept's, `AMIHAN_CLOTH=abel` an abel indigo; both are
# rendered beside the cast.
#
# Slot key:
#   0 capelet          1 capelet shadow   2 gold             3 rust (belt, straps, sash)
#   4 weave dark       5 cream robe       6 hair             7 capelet light
#   8 face ink         9 sandal sole      10 robe shadow     11 shorts
#   12 cotton white    13/14/15 skin, shadow, skin (15 is what the donor skull wears)
# ---------------------------------------------------------------------------

CAPE, CAPE_DARK, GOLD, RUST = 0, 1, 2, 3
WEAVE_DARK, CREAM, HAIR, CAPE_LIT = 4, 5, 6, 7
INK, SOLE, CREAM_SHADE, SHORTS = 8, 9, 10, 11
COTTON, SKIN, SKIN_DARK, SKIN_LIT = 12, 13, 14, 15

PALETTE = {0: '2E8C86',
 1: '1F625E',
 2: 'E8B64A',
 3: 'A8502E',
 4: '3A2A24',
 5: 'F1E4C8',
 6: '3A241C',
 7: '5FC2B5',
 8: '181418',
 9: 'F3E6CA',
 10: 'D8C6A2',
 11: '2A7470',
 12: 'FBF8F0',
 13: 'D59A6E',
 14: 'B57A52',
 15: 'D59A6E'}

CLOTH_VARIANTS = {'teal': {0: '2E8C86', 1: '1F625E', 7: '5FC2B5', 11: '2A7470'},
                  'abel': {0: '3C4C7E', 1: '27335A', 7: '8A9CCB', 11: '2F3B66'}}
PALETTE.update(CLOTH_VARIANTS[os.environ.get('AMIHAN_CLOTH', 'teal')])

MAX_FACE_LUMINANCE = 0.30


def mirrored(boxes, bone_from, bone_to):
    out = []
    for name, bone, lo, hi, slot in boxes:
        assert bone == bone_from, f"{name} is on {bone}, not {bone_from}"
        out.append((name.replace("left", "right"), bone_to,
                    (-hi[0], lo[1], lo[2]), (-lo[0], hi[1], hi[2]), slot))
    return out


# Legs own everything below the hips at 0.232: thick cream sandals with a rust strap, bare
# calves, shorts under the robe.
LEG_LEFT = [('sandal-sole-left', 'leg-left', (0.008, 0.000, -0.134), (0.158, 0.034, 0.084), SOLE),
 ('foot-left', 'leg-left', (0.024, 0.034, -0.120), (0.142, 0.072, 0.060), SKIN),
 ('sandal-strap-left', 'leg-left', (0.020, 0.048, -0.098), (0.146, 0.068, -0.066), RUST),
 ('calf-left', 'leg-left', (0.036, 0.066, -0.054), (0.130, 0.176, 0.052), SKIN),
 ('shorts-left', 'leg-left', (0.018, 0.152, -0.070), (0.150, 0.262, 0.070), SHORTS)]

LEG_RIGHT = mirrored(LEG_LEFT, "leg-left", "leg-right")

# Torso owns 0.232 to 0.445.
TORSO = [('robe-body', 'torso', (-0.116, 0.232, -0.084), (0.116, 0.440, 0.088), CREAM),
 ('neck', 'torso', (-0.052, 0.400, -0.048), (0.052, 0.452, 0.052), SKIN),
 # The capelet: a mantle over the shoulders, two collar points falling in front, a drop
 # behind that carries the kasikus.
 # ⚠️ v2: A MANTLE, NOT A COLLAR. v1's capelet stopped at the neck line and read as a collar;
 # the concept's covers the shoulders and the top of the chest. It is wider than the robe,
 # drops over the chest in front in two rounded points, and falls to the shoulder blades
 # behind, where the kasikus sits.
 ('capelet', 'torso', (-0.152, 0.378, -0.106), (0.152, 0.454, 0.108), CAPE),
 ('capelet-front-left', 'torso', (0.006, 0.362, -0.114), (0.106, 0.418, -0.094), CAPE),
 ('capelet-front-right', 'torso', (-0.106, 0.362, -0.114), (-0.006, 0.418, -0.094), CAPE),
 ('capelet-back', 'torso', (-0.136, 0.330, 0.092), (0.136, 0.446, 0.114), CAPE),
 ('brooch', 'torso', (-0.023, 0.380, -0.124), (0.023, 0.426, -0.106), GOLD),
 ('brooch-core', 'torso', (-0.011, 0.392, -0.130), (0.011, 0.414, -0.118), CREAM),
 # The belt, its buckle, and the patterned sash hanging at her left hip.
 # ⚠️ v2: THE ROBE IS AN OPEN COAT (owner, of v1: "that dont look like reference at all").
 # The concept's cream robe parts down the front over a teal inner, and hangs to the knee,
 # open at the centre over teal shorts. v1 closed it and stopped it at mid thigh.
 ('robe-inner', 'torso', (-0.052, 0.262, -0.094), (0.052, 0.380, -0.082), CAPE),
 ('belt', 'torso', (-0.122, 0.262, -0.098), (0.122, 0.292, 0.100), RUST),
 ('belt-buckle', 'torso', (-0.015, 0.259, -0.106), (0.015, 0.295, -0.096), GOLD),
 # The patterned sash hangs just off centre on her left, a small knot sits on her right hip.
 ('sash', 'torso', (0.018, 0.132, -0.114), (0.060, 0.268, -0.100), CREAM),
 ('sash-knot', 'torso', (0.010, 0.252, -0.118), (0.066, 0.296, -0.100), CREAM),
 ('hip-knot', 'torso', (-0.108, 0.254, -0.112), (-0.070, 0.294, -0.098), CREAM),
 ('robe-skirt-left', 'torso', (0.040, 0.118, -0.100), (0.134, 0.266, 0.090), CREAM),
 ('robe-skirt-right', 'torso', (-0.134, 0.118, -0.100), (-0.040, 0.266, 0.090), CREAM),
 ('robe-skirt-back', 'torso', (-0.126, 0.118, 0.084), (0.126, 0.266, 0.102), CREAM)]

# Wide cream sleeves to the forearm, bare forearms and hands. The hand keeps the exact
# 0.3383 to 0.4617 span (shoulder +/- `HandTopLift`).
ARM_LEFT = [('sleeve-left', 'arm-left', (0.0999, 0.330, -0.068), (0.244, 0.470, 0.068), CREAM),
 ('forearm-left', 'arm-left', (0.238, 0.352, -0.044), (0.290, 0.448, 0.044), SKIN),
 ('hand-left', 'arm-left', (0.284, 0.3383, -0.050), (0.3836, 0.4617, 0.056), SKIN)]

ARM_RIGHT = mirrored(ARM_LEFT, "arm-left", "arm-right")

# ---------------------------------------------------------------------------
# THE HEAD, in the donor's own space (+Z is the face). Skull shell y 0.343 to 0.661, |x| 0.227.
# Shoulder-length dark brown hair with a swept fringe that falls low on her right, soft waves
# on top, the cotton-boll pin above her left ear with two gold tassels.
# ---------------------------------------------------------------------------
HEAD = [
 # ⚠️ v2: THE HAIR IS LOCKS, NOT SLABS. v1's fringe was one tilted block and read as a visor
 # brim; its back hung to the shoulders and hid the kasikus. The fringe is now four stepped
 # locks sweeping across the brow from her left, longest on her right, and the back stops at
 # the nape in three uneven tips (the concept's length), so the capelet shows.
 ('hair-crown', 'head', (-.212, .604, -.212), (.212, .712, .146), HAIR),
 ('hair-top', 'head', (-.176, .706, -.186), (.170, .742, .110), HAIR),
 ('hair-wave-1', 'head', (-.168, .736, -.100), (-.078, .772, .060), HAIR),
 ('hair-wave-2', 'head', (-.070, .742, -.150), (.050, .786, .044), HAIR),
 ('hair-wave-3', 'head', (.044, .736, -.130), (.150, .770, .050), HAIR),
 ('hair-wave-4', 'head', (-.120, .716, -.206), (.110, .752, -.120), HAIR),
 ('fringe-lock-1', 'head', (.040, .604, .124), (.132, .666, .178), HAIR),
 ('fringe-lock-2', 'head', (-.040, .586, .128), (.060, .660, .184), HAIR),
 ('fringe-lock-3', 'head', (-.116, .556, .126), (-.030, .650, .186), HAIR),
 ('fringe-lock-4', 'head', (-.178, .486, .098), (-.112, .640, .168), HAIR),
 ('hair-side-left', 'head', (.200, .470, -.190), (.236, .650, .040), HAIR),
 ('hair-side-right', 'head', (-.238, .430, -.190), (-.200, .650, .096), HAIR),
 ('hair-back', 'head', (-.222, .440, -.230), (.222, .660, -.100), HAIR),
 ('hair-nape-a', 'head', (-.200, .404, -.224), (-.070, .446, -.120), HAIR),
 ('hair-nape-b', 'head', (-.064, .394, -.228), (.070, .446, -.124), HAIR),
 ('hair-nape-c', 'head', (.076, .410, -.222), (.200, .446, -.120), HAIR),
 # The cotton-boll pin: three bolls, each its own size, and two woven gold tassels.
 # ⚠️ v2: a cluster at the temple, forward of the ear, each boll 40 to 48 mm; v1's were 30 mm
 # and stacked up the side of the head like an antenna.
 # ⚠️ v3/v4: EACH FLOWER IS FOUR PETALS ROUND A GOLD HEART, the concept's pixel flower, each
 # about 65 to 78 mm across (v3's 18 mm petals read as white dots). v2's
 # three near-cubes merged into one white streak down the hair. Cotton bolls open in four
 # locks, so the reading holds: the flower in the concept, the boll in the lore.
 ('flower-1-n', 'head', (0.137, 0.703, 0.148), (0.163, 0.729, 0.174), COTTON),
 ('flower-1-s', 'head', (0.137, 0.651, 0.148), (0.163, 0.677, 0.174), COTTON),
 ('flower-1-e', 'head', (0.163, 0.677, 0.148), (0.189, 0.703, 0.174), COTTON),
 ('flower-1-w', 'head', (0.111, 0.677, 0.148), (0.137, 0.703, 0.174), COTTON),
 ('flower-1-c', 'head', (0.141, 0.681, 0.158), (0.159, 0.699, 0.179), GOLD),
 ('flower-2-n', 'head', (0.186, 0.645, 0.140), (0.210, 0.668, 0.163), COTTON),
 ('flower-2-s', 'head', (0.186, 0.600, 0.140), (0.210, 0.623, 0.163), COTTON),
 ('flower-2-e', 'head', (0.210, 0.623, 0.140), (0.233, 0.645, 0.163), COTTON),
 ('flower-2-w', 'head', (0.164, 0.623, 0.140), (0.186, 0.645, 0.163), COTTON),
 ('flower-2-c', 'head', (0.190, 0.626, 0.149), (0.206, 0.642, 0.168), GOLD),
 ('flower-3-n', 'head', (0.219, 0.593, 0.128), (0.239, 0.613, 0.148), COTTON),
 ('flower-3-s', 'head', (0.219, 0.553, 0.128), (0.239, 0.573, 0.148), COTTON),
 ('flower-3-e', 'head', (0.239, 0.573, 0.128), (0.259, 0.593, 0.148), COTTON),
 ('flower-3-w', 'head', (0.199, 0.573, 0.128), (0.219, 0.593, 0.148), COTTON),
 ('flower-3-c', 'head', (0.222, 0.576, 0.136), (0.236, 0.590, 0.153), GOLD),
 ('tassel-1', 'head', (.226, .500, .128), (.235, .566, .137), GOLD),
 ('tassel-2', 'head', (.238, .512, .132), (.245, .566, .139), GOLD),
 ('tassel-1-end', 'head', (.222, .474, .124), (.239, .502, .141), GOLD),
 ('tassel-2-end', 'head', (.235, .488, .129), (.248, .514, .142), GOLD)]

BOX_TILTS = {'fringe-lock-1': 16, 'fringe-lock-2': 12, 'fringe-lock-3': 8, 'fringe-lock-4': -4,
             'capelet-front-left': -8, 'capelet-front-right': 8}

BOX_TAPERS = {'robe-body': (1, 0, .92, 1),
              'capelet-front-left': (1, 0, .55, 1), 'capelet-front-right': (1, 0, .55, 1),
              'capelet-back': (1, 0, .86, 1),
              'sash': (1, 0, .86, 1),
              # ⚠️ v3: the coat flares at the hem (1.18 wide at the bottom); v2's panels hugged each
              # leg and read as cream trousers.
              'robe-skirt-left': (1, 0, 1.18, 1), 'robe-skirt-right': (1, 0, 1.18, 1),
              'robe-skirt-back': (1, 0, 1.10, 1),
              'fringe-lock-2': (1, 0, .70, 1), 'fringe-lock-3': (1, 0, .62, 1), 'fringe-lock-4': (1, 0, .52, 1),
              'hair-nape-a': (1, 0, .72, 1), 'hair-nape-b': (1, 0, .70, 1), 'hair-nape-c': (1, 0, .74, 1),
              'tassel-1-end': (1, 0, 1, .55), 'tassel-2-end': (1, 0, 1, .55),
              'hand-left': (0, 2, .72, 1), 'hand-right': (0, 2, 1, .72)}


# ---------------------------------------------------------------------------
# § DECALS: the kasikus and the trims, every one its own hand-set polygon (method, section 4).
# Views: front/back (x, y), left/right (z, y), top (x, z); table space; layer 2 on the surface.
# ---------------------------------------------------------------------------
def _quad(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def _stroke(points, width):
    """A polyline as convex quads, with a square at each inner joint (used by the face only)."""
    out, half = [], width * 0.5
    for (ax, ay), (bx, by) in zip(points, points[1:]):
        length = math.hypot(bx - ax, by - ay)
        nx, ny = -(by - ay) / length * half, (bx - ax) / length * half
        out.append([(ax + nx, ay + ny), (bx + nx, by + ny), (bx - nx, by - ny), (ax - nx, ay - ny)])
    for x, y in points[1:-1]:
        out.append(_quad(x - half, y - half, x + half, y + half))
    return out


# The kasikus on the capelet's back: two diamond outlines, one inside the other, around a solid
# diamond, all radiating from one centre (0, 0.398): the whirlwind as the binakol draws it.
# Each bar is typed on its own; the stroke is 7 mm (0.0099 across a 45 degree edge).
KASIKUS = [
    ('capelet-back', 'back', CREAM, [(0.0, 0.4260), (0.0420, 0.3840), (0.0321, 0.3840), (0.0, 0.4161)], 2),
    ('capelet-back', 'back', CREAM, [(0.0420, 0.3840), (0.0, 0.3420), (0.0, 0.3519), (0.0321, 0.3840)], 2),
    ('capelet-back', 'back', CREAM, [(0.0, 0.3420), (-0.0420, 0.3840), (-0.0321, 0.3840), (0.0, 0.3519)], 2),
    ('capelet-back', 'back', CREAM, [(-0.0420, 0.3840), (0.0, 0.4260), (0.0, 0.4161), (-0.0321, 0.3840)], 2),
    ('capelet-back', 'back', CREAM, [(0.0, 0.4090), (0.0250, 0.3840), (0.0165, 0.3840), (0.0, 0.4005)], 2),
    ('capelet-back', 'back', CREAM, [(0.0250, 0.3840), (0.0, 0.3590), (0.0, 0.3675), (0.0165, 0.3840)], 2),
    ('capelet-back', 'back', CREAM, [(0.0, 0.3590), (-0.0250, 0.3840), (-0.0165, 0.3840), (0.0, 0.3675)], 2),
    ('capelet-back', 'back', CREAM, [(-0.0250, 0.3840), (0.0, 0.4090), (0.0, 0.4005), (-0.0165, 0.3840)], 2),
    ('capelet-back', 'back', CREAM, [(0.0, 0.3930), (0.0090, 0.3840), (0.0, 0.3750), (-0.0090, 0.3840)], 2),
]

# The sleeve cuffs: two capelet-coloured stripes near the end of each sleeve (the concept's).
CUFFS = [
    # The concept's cuff: a teal band, a cream gap, a teal band, near the end of each sleeve.
    ('sleeve-left', 'front', CAPE, [(0.212, 0.330), (0.222, 0.330), (0.222, 0.470), (0.212, 0.470)], 2),
    ('sleeve-left', 'front', CAPE, [(0.229, 0.330), (0.239, 0.330), (0.239, 0.470), (0.229, 0.470)], 2),
    ('sleeve-left', 'top', CAPE, [(0.212, -0.068), (0.222, -0.068), (0.222, 0.068), (0.212, 0.068)], 2),
    ('sleeve-left', 'top', CAPE, [(0.229, -0.068), (0.239, -0.068), (0.239, 0.068), (0.229, 0.068)], 2),
    ('sleeve-left', 'back', CAPE, [(0.212, 0.330), (0.222, 0.330), (0.222, 0.470), (0.212, 0.470)], 2),
    ('sleeve-left', 'back', CAPE, [(0.229, 0.330), (0.239, 0.330), (0.239, 0.470), (0.229, 0.470)], 2),
    ('sleeve-right', 'front', CAPE, [(-0.222, 0.330), (-0.212, 0.330), (-0.212, 0.470), (-0.222, 0.470)], 2),
    ('sleeve-right', 'front', CAPE, [(-0.239, 0.330), (-0.229, 0.330), (-0.229, 0.470), (-0.239, 0.470)], 2),
    ('sleeve-right', 'top', CAPE, [(-0.222, -0.068), (-0.212, -0.068), (-0.212, 0.068), (-0.222, 0.068)], 2),
    ('sleeve-right', 'top', CAPE, [(-0.239, -0.068), (-0.229, -0.068), (-0.229, 0.068), (-0.239, 0.068)], 2),
    ('sleeve-right', 'back', CAPE, [(-0.222, 0.330), (-0.212, 0.330), (-0.212, 0.470), (-0.222, 0.470)], 2),
    ('sleeve-right', 'back', CAPE, [(-0.239, 0.330), (-0.229, 0.330), (-0.229, 0.470), (-0.239, 0.470)], 2),
]

# The robe's hem: one capelet stripe round the skirt panels.
HEM = [
    ('robe-skirt-left', 'front', CAPE, [(0.030, 0.128), (0.146, 0.128), (0.146, 0.137), (0.030, 0.137)], 2),
    ('robe-skirt-right', 'front', CAPE, [(-0.146, 0.128), (-0.030, 0.128), (-0.030, 0.137), (-0.146, 0.137)], 2),
    ('robe-skirt-left', 'left', CAPE, [(-0.098, 0.128), (0.090, 0.128), (0.090, 0.137), (-0.098, 0.137)], 2),
    ('robe-skirt-right', 'right', CAPE, [(-0.098, 0.128), (0.090, 0.128), (0.090, 0.137), (-0.098, 0.137)], 2),
    ('robe-skirt-back', 'back', CAPE, [(-0.126, 0.128), (0.126, 0.128), (0.126, 0.137), (-0.126, 0.137)], 2),
]

# The sash's binakol: three small chevrons pointing down, cream on rust, each typed on its own.
SASH = [
    # The sash's binakol: a column of small diamonds in the dark weave, each typed on its own,
    # and a rust border at its foot (the concept's patterned sash).
    ('sash', 'front', WEAVE_DARK, [(0.039, 0.252), (0.050, 0.241), (0.039, 0.230), (0.028, 0.241)], 2),
    ('sash', 'front', WEAVE_DARK, [(0.039, 0.224), (0.050, 0.213), (0.039, 0.202), (0.028, 0.213)], 2),
    ('sash', 'front', WEAVE_DARK, [(0.039, 0.196), (0.050, 0.185), (0.039, 0.174), (0.028, 0.185)], 2),
    ('sash', 'front', RUST, [(0.018, 0.140), (0.060, 0.140), (0.060, 0.150), (0.018, 0.150)], 2),
    ('sash', 'front', RUST, [(0.018, 0.156), (0.060, 0.156), (0.060, 0.160), (0.018, 0.160)], 2),
]

BODY_DECALS = KASIKUS + CUFFS + HEM + SASH

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
# ⚠️ THE TABLES ARE REMAPPED, NOT REWRITTEN. Every box carries a measurement and a reason:
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
# AMIHAN'S FACE: bright and sure of herself (owner: "u can change facial expression"; the
# concept's ":3" is replaced). Ink only, like the whole cast (method, section 2): two eyes and
# a mouth, nothing else. Mouths are compared by rendering them side by side (`AMIHAN_MOUTH`).
# ---------------------------------------------------------------------------
MOUTH_Z = 0.1596

EYE_SCALE = 0.82

# Eyes: tall rounded blocks, their tops lifting slightly outward, which reads as lively.
EYE_X, EYE_HALF, EYE_TOP_OUT, EYE_TOP_IN, EYE_BOTTOM, EYE_CUT = 0.076, 0.023, 0.510, 0.506, 0.452, 0.006

MOUTHS = {
    'smile': ([(-0.026, 0.428), (-0.010, 0.416), (0.010, 0.416), (0.026, 0.428)], 0.010),
    'grin': ([(-0.028, 0.430), (0.028, 0.430), (0.020, 0.414), (0.000, 0.408), (-0.020, 0.414)], None),
    'smirk': ([(-0.024, 0.419), (0.012, 0.419), (0.030, 0.431)], 0.010),
    'cat': ([(-0.024, 0.428), (-0.012, 0.415), (0.000, 0.425), (0.012, 0.415), (0.024, 0.428)], 0.008),
}
SCOWL, SCOWL_WEIGHT = MOUTHS[os.environ.get('AMIHAN_MOUTH', 'smile')]

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

    # Replace only the donor's graphic ink, keeping all native skull/ear skin.
    tris = [t for t in tris if t not in mouth_tris and t not in eyes]

    def polygon(points, slot, layer):
        """A convex polygon flat on the face plate, `layer` steps of PANEL_PROUD proud."""
        points = list(points)
        area = sum(a[0] * b[1] - b[0] * a[1] for a, b in zip(points, points[1:] + points[:1]))
        if area < 0:
            points.reverse()
        z = MOUTH_Z + PANEL_PROUD * layer
        first = len(pos)
        for x, y in points:
            pos.append((x, y, z)); nrm.append((0, 0, 1)); uv.append(cell_uv(slot))
        tris.extend((first, first + i, first + i + 1) for i in range(1, len(points) - 1))

    for centre in (-EYE_X, EYE_X):
        out = 1.0 if centre > 0 else -1.0          # away from the nose
        xo, xi = centre + out * EYE_HALF, centre - out * EYE_HALF
        polygon([(xo, EYE_TOP_OUT - EYE_CUT), (xo - out * EYE_CUT, EYE_TOP_OUT), (xi + out * EYE_CUT, EYE_TOP_IN),
                 (xi, EYE_TOP_IN - EYE_CUT), (xi, EYE_BOTTOM + EYE_CUT), (xi + out * EYE_CUT, EYE_BOTTOM),
                 (xo - out * EYE_CUT, EYE_BOTTOM), (xo, EYE_BOTTOM + EYE_CUT)], INK, 1)

    if SCOWL_WEIGHT is None:
        polygon(SCOWL, INK, 1)
    else:
        for piece in _stroke(SCOWL, SCOWL_WEIGHT):
            polygon(piece, INK, 1)

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


def _rafi_forms(head):
    """Tubes and lofts. Amihan wears none: every piece of her is a box or a decal."""
    return iter(())


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


def build_mesh(boxes, panels=(), donor=None, decals=()):
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

    for bone,slot,faces in _rafi_forms(donor is not None):
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
    body = build_mesh(_family(BODY_BOXES, head=False), decals=BODY_DECALS)
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso Amihan native voxel builder"}

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
