"""Builds the BASE rig for the character maker: bald, bare, no clothes, no face.

    python tools/build_base_voxel.py

WHY THIS FILE EXISTS, AND WHY IT IS NOT AN EDIT TO `build_person_voxel.py`.
The team lead, 2026-08-31: *"maybe generate a completely new voxel that will be
customizable... like everything in it is movable and shit, dont toucht heh existing
onnes, i will be very mad if u break or fuck up any of the existing ones"*, and *"using
the simplest character taht we have as base and they will start out as naked"*.

⚠️⚠️ THIS IS THE METHOD EVERY GAME WITH A CHARACTER CREATOR USES, AND THE REPO WAS
DOING THE OTHER ONE. Asked how Monster Hunter does it, the answer is three pieces, and
the first is the one this repo did not have:

  1. a NAKED BASE MESH, with no hair, no clothes and no face painted into it,
  2. equipment authored as SEPARATE geometry, hung on the skeleton one slot at a time,
  3. a colour remap, so one authored garment ships in every colour.

`docs/TODO.md` section 110.3 records what the missing first piece cost. With a DRESSED
base (`team-custom.glb` is a copy of a fully clothed hero) every wearable had to COVER
what was under it rather than BE the thing: a hairstyle became a lid that had to enclose
a baked mop, a sando became a box drawn over another box, and every expression had to lay
a skin-coloured plate over the rig's own painted-on eyes before it could draw its own.
All three of those disappear against a bare rig, and none of them was fixable while the
base wore clothes.

WHAT IT SHARES WITH `build_person_voxel.py`, AND WHY IT IMPORTS RATHER THAN COPIES.
Everything below the box tables in that file is character agnostic and its own header says
so: *"copy the four box tables and the palette, give them a new OUT path"*. The retarget,
the inverse bind matrices, the family remap, the chamfer, the normal smoothing, the glTF
repack and all four build-time constraint checks are reused verbatim by pointing the
module's globals at this character's tables. A copy of that machinery is a copy that
drifts, and the thing it would drift on is the animation retarget, which does not fail
loudly: it tears the mesh apart on the first clip that plays.

⚠️⚠️ AND IT WRITES NEW FILES. NOTHING EXISTING IS TOUCHED. `team-custom.glb`,
`person_team-custom.tres` and `person_custom.asset` are left exactly as they are, and so
is every other `team-*.glb` a roster row points at. This emits `team-custom-base.glb`,
`person_team-custom-base.tres`, and `RosterBookBuilder` gets a new `custom_base` row
beside the `custom` one rather than a change to it.

⚠️ THE HEAD IS THE DONOR'S, MINUS ITS FACE. `character-male-d` is the simplest rig in
the set this pipeline already names: 1239 verts, 711 triangles, five palette slots, and it
is the only one of the twelve that is BALD. `build_person_voxel.py` already lifts its
skull (slot 15) as `DONOR_SKULL` and throws slot 13 away, on the grounds that the pate is
*"the HAIR volume worn in skin"* and keeping it leaves an authored mop nowhere to sit. For
a base rig that sentence is the requirement rather than the problem: slot 13 IS the bald
head. So this build keeps 15 AND 13, and drops slot 8, which is the painted-on eyes and
mouth.

⚠️ DROPPING SLOT 8 IS WHAT RETIRES `VoxelWardrobe`'s FACE PLATE. With no eyes baked
into the head there is nothing to cover, so an expression draws its eyes straight onto the
skin instead of onto a rectangle laid over somebody else's.
"""
import sys

sys.path.insert(0, "tools")
import build_person_voxel as bpv                      # noqa: E402
from build_person_voxel import mirrored               # noqa: E402

# The skeleton, both skins and all 32 clips come from here, exactly as `team-custom.glb`
# takes them. ⚠️ IT IS NOT THE SHAPE DONOR. Every vertex below is either authored here or
# lifted off `DONOR_SKULL`; this file supplies the rig the animation is keyed against.
BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"

OUT = "Assets/TumbangPreso/Art/characters/persons/team-custom-base.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-custom-base.tres"

# ---------------------------------------------------------------------------
# § PALETTE
#
# ⚠️⚠️ FIFTEEN OF THESE SIXTEEN ARE OVERWRITTEN AT RUNTIME AND THE LIST IS STILL
# AUTHORED HONESTLY. `CustomCharacterScreen.PaletteFor` writes the skin ramp (13, 14, 15),
# the hair ramp (10, 11, 12), the top (4, 5, 6), the bottom (0, 1, 2) and the three gear
# tones (3, 7, 9) from the player's own choices every time the model is shown. The only
# entry that survives from this table is slot 8, the ink.
#
# The rest are what the rig wears when nothing has chosen for it: a turnaround,
# `PersonSwapProbe`, the roster sheet, and a fresh clone before the creator has ever been
# opened. A file whose unchosen colours are magenta reads as broken in all four.
#
# ⚠️ THE SLOT MEANINGS ARE `VoxelWardrobe`'s, NOT `build_person_voxel.py`'s. That file
# names slot 0 OVERALLS because that is what its one character wears there. Here the groups
# are the character maker's: bottom, gear, top, gear, ink, gear, hair, skin.
# ---------------------------------------------------------------------------
PALETTE = {
    0:  "2E3C52",   # bottom, shade
    1:  "3F5270",   # bottom, base
    2:  "56698C",   # bottom, lit
    3:  "8B5227",   # gear A: carved wood edge. `docs/VISION.md` section 6.
    4:  "8E3A2C",   # top, shade
    5:  "B04A38",   # top, base
    6:  "C96A52",   # top, lit
    7:  "FFBA00",   # gear B: amber gold
    8:  "141416",   # ⚠️ INK. The eyes, brows and mouth the wardrobe draws. Never light.
    9:  "F5E6C8",   # gear C: cream paper and chalk
    10: "241C18",   # hair, shade
    11: "3A2C22",   # hair, base
    12: "5A4634",   # hair, lit
    13: "C88A52",   # ⚠️ SKIN, MID. 13 is the base tone and 14 is the shadow: see below.
    14: "A06E3D",   # skin, shade
    15: "E6A86E",   # skin, lit
}

# ⚠️⚠️ 13 IS THE MID TONE AND 14 IS THE SHADOW, WHICH IS THE OPPOSITE OF WHAT THE NAMES
# IN `VoxelWardrobe` SUGGEST. `PaletteRules.SkinSlots` is `{13, 14, 15}` and
# `CustomCharacterScreen.Ramp` writes them as (x1.14, x0.78, x1.14), so the MIDDLE entry is
# the dark one. Every shipped `.tres` agrees: `person_team-zack.tres` carries 13 and 15 at
# the same lit value with 14 a clear step under. Authored to match, so a slot painted here
# and the same slot painted at runtime are the same kind of colour.
SKIN, SKIN_DARK, SKIN_LIT = 13, 14, 15

# ---------------------------------------------------------------------------
# § THE BODY, AND EVERY BOX IS SKIN
#
# ⚠️⚠️ AUTHORED IN THE SAME SPACE `build_person_voxel.py`'s TABLES ARE, so `_family`
# remaps it onto the cast's own joint heights. `WAS_HIPS` 0.232, `WAS_SHOULDER` 0.400 and
# `WAS_NECK` 0.445 are the heights these numbers mean; the remap lands them on the shipped
# 0.176, 0.288 and 0.343. Authoring against the final numbers instead would look right and
# then move the moment anybody runs the family pass again.
#
# ⚠️ THE Z BOUNDS ARE AUTHORED FACING -Z. `FRONT_IS_MINUS_Z` negates and swaps them at
# build time, so a toe is at NEGATIVE z here and positive z in the file. Getting that
# backwards puts the feet on backwards and nothing in the build complains.
#
# ⚠️⚠️ THE MEASURED EXTENTS ARE THE CONTRACT, NOT THE BOXES. `VoxelDresser.MeasureAnchor`
# derives every wearable frame from this rig at runtime: the torso from the torso and head
# BONES at 1.28 times the shoulder joint's x, an arm as the outer fifth of a horizontal
# bar, a leg from the floor to the hip, and the head from `head-mesh`'s own bounds.
# `docs/TODO.md` section 110.9 is the entry about guessing those instead of measuring them,
# twice, and shipping both guesses. So this body is authored to land on the same measured
# frame `team-custom.glb` produces, to within a few millimetres:
#
#     body-mesh   x +/-0.3836   y 0 to 0.366   z -0.104 to 0.134
#     torso       |x| 0.128     y 0.1745 to 0.3499
#     arm-left    x 0.0999 to 0.3836, at a near-constant height: a HORIZONTAL bar
#
# A wardrobe authored against one rig and worn by another is the whole fault that section
# is about, and the cheapest way not to have it is for the two rigs to measure the same.
# ---------------------------------------------------------------------------

# ⚠️ THE FOOT IS ITS OWN BOX AND IT IS THE FOOTPRINT OF THE SHOE THAT IS NOT THERE. Every
# entry in `VoxelWardrobe.Footwear` runs from V -1.02 to about -0.56, which is the bottom
# quarter of the floor-to-hip frame. A bare leg running straight to the ground would leave a
# tsinelas strapped to a shin. The toe is also what holds the body mesh's z extent at 0.134,
# which is what the torso frame's depth is measured from.
#
# ⚠️⚠️ AND IT IS 30 MM TALL RATHER THAN 46, BECAUSE A SLIPPER HAS TO FIT ON IT. The leg frame
# runs from the floor to the hip, so `V` -1 is the ground by construction and a sole cannot be
# authored UNDER a foot without going through the street. At 46 mm the foot filled `V` -1.00 to
# -0.48 and every entry in `VoxelWardrobe.Footwear` was buried inside it. At 30 mm the foot is
# -1.00 to -0.66, a sole covers its lower two thirds and a strap crosses the instep above it,
# which is what a tsinelas is.
#
# ⚠️ THE FOOT IS WIDER THAN THE LEG ABOVE IT, WHICH IS BOTH TRUE OF FEET AND REQUIRED BY THE
# TWO FRAMES THAT OVERLAP HERE. A shoe is authored against the LEG frame and a pair of shorts
# against the TORSO frame, and the thigh is the only place both reach: at 0.150 the leg was
# wider than a garment at `U` 1.06 of a torso half width of 0.1279, so every pair of shorts
# left a bare notch down the outside of the thigh. The leg is 0.142 now and the foot is not.
LEG_LEFT = [
    ("foot-left",  "leg-left", (0.012, 0.000, -0.134), (0.156, 0.030, 0.078), SKIN),
    ("ankle-left", "leg-left", (0.026, 0.030, -0.076), (0.142, 0.082, 0.070), SKIN_DARK),
    ("shin-left",  "leg-left", (0.024, 0.082, -0.070), (0.144, 0.232, 0.068), SKIN),
]

LEG_RIGHT = mirrored(LEG_LEFT, "leg-left", "leg-right")

# ⚠️ FOUR BOXES RATHER THAN ONE, AND THE REASON IS THE SILHOUETTE. `docs/VISION.md`
# section 2: a screenshot taken mid-fight must still show every player, and what separates
# one character from another at arena distance is the outline. A single cuboid torso reads
# as a crate; a hip wider than the waist and a chest wider than both is a person at the
# same triangle count, because the chamfer and the smoothed normals do the rest.
#
# ⚠️⚠️ NOTHING HERE REACHES 0.128, AND THE 10 MM IT GIVES BACK IS THE INK OUTLINE'S.
# `VoxelDresser.MeasureAnchor` sets the torso frame's half width from the SHOULDER JOINT, at
# 1.28 times 0.0999, which is 0.1279 whatever this mesh does. Every top in `VoxelWardrobe` is
# authored at about `U` 1.02, which is 0.1305. **`ToonSkin.Apply` extrudes the base rig's
# inverted-hull outline by `PersonOutlineWidth`, 0.008 of model space**, so a torso authored out
# to 0.128 left a garment 2.5 mm proud of a surface wearing an 8 mm black border: the first
# dressed render came back with the sando speckled black where the body's own outline punched
# through it. `docs/CANONICAL_RENDERING_PIPELINE.md` pitfall 5, from the other side.
TORSO = [
    ("hips",  "torso", (-0.118, 0.230, -0.104), (0.118, 0.300, 0.098), SKIN),
    ("waist", "torso", (-0.104, 0.290, -0.096), (0.104, 0.372, 0.090), SKIN),
    ("chest", "torso", (-0.116, 0.360, -0.100), (0.116, 0.440, 0.096), SKIN),
    ("neck",  "torso", (-0.038, 0.428, -0.064), (0.038, 0.452, 0.052), SKIN_DARK),
]

# ⚠️⚠️ THE ARM IS A HORIZONTAL BAR AND THAT IS NOT A MISTAKE. `docs/TODO.md` section
# 110.9: `arm-left` runs x 0.0999 to 0.3836 at a near-constant height on every rig in this
# cast, and a wristband authored as though the arm hung downward wraps thin air beside the
# elbow. The hand is the outer third, which is the span `MeasureAnchor` hands the wrist
# slot.
ARM_LEFT = [
    ("upperarm-left", "arm-left", (0.0999, 0.330, -0.062), (0.262, 0.470, 0.060), SKIN),
    ("hand-left",     "arm-left", (0.262, 0.3383, -0.052), (0.3836, 0.4617, 0.050), SKIN),
]

ARM_RIGHT = mirrored(ARM_LEFT, "arm-left", "arm-right")

# ⚠️⚠️ THE HEAD TABLE IS EMPTY ON PURPOSE. Every other character in this pipeline authors
# hair, a fringe, ears and an expression as boxes on top of the donated skull. This one is
# the skull and nothing else, because everything that would go here is a wardrobe slot the
# player chooses. An empty table is not a missing step; it is the feature.
HEAD = []

# ⚠️ 13 IS THE BALD PATE AND 15 IS THE SKULL, AND `build_person_voxel.py` DROPS 13.
# Its own note says why: on a character with a mop, the pate *"is the HAIR volume worn in
# skin"* and keeping it leaves the hair nowhere to go. Here it is the whole point. Slot 8,
# the donor's painted eyes and mouth, is not listed and is therefore not lifted.
SKULL_SLOTS = {15: SKIN, 13: SKIN}


def _bald_head():
    """The donor's skull and pate, repainted to this rig's skin, with no face on it."""
    return bpv._compact(*bpv._donor_part(bpv.DONOR_SKULL, SKULL_SLOTS))


def main():
    bpv.BASE = BASE
    bpv.OUT = OUT
    bpv.PALETTE_OUT = PALETTE_OUT
    bpv.PALETTE = PALETTE

    bpv.LEG_LEFT = LEG_LEFT
    bpv.LEG_RIGHT = LEG_RIGHT
    bpv.TORSO = TORSO
    bpv.ARM_LEFT = ARM_LEFT
    bpv.ARM_RIGHT = ARM_RIGHT
    bpv.HEAD = HEAD

    bpv.BODY_BOXES = LEG_LEFT + LEG_RIGHT + TORSO + ARM_LEFT + ARM_RIGHT
    bpv.HEAD_BOXES = HEAD

    # ⚠️ `DONOR_SPACE` IS COMPUTED FROM `HEAD` AT IMPORT TIME and names the boxes already
    # in the donor's own coordinates. With no head boxes there are none, and leaving the
    # imported value in place would name boxes from a character this build does not have.
    bpv.DONOR_SPACE = ()

    bpv._donor_head = _bald_head

    print("building the naked base rig: bald skull, no face, skin only")
    bpv.main()


if __name__ == "__main__":
    sys.exit(main())
