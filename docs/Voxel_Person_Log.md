# ZACK: what was built, and what it cost

The record of the first person replacement under `Port_Plan.md` §8.3 item 3. The
how-to lives in [`Voxel_Person_Guide.md`](Voxel_Person_Guide.md); this is what
happened and why the code looks the way it does.

Branch `models/phase1-voxel-cast`. One commit went to `main` on its own, for the
reason in §2.

---

## 1 · What shipped

| | |
|---|---|
| `tools/build_person_voxel.py` | builds the model and its palette from a CC0 rig |
| `tools/glb_dump.py`, `glb_mesh_dump.py`, `glb_bone_bounds.py`, `glb_face_side.py`, `glb_anim_channels.py` | five read-only probes over the `.glb` format |
| `Assets/.../Art/characters/persons/team-zack.glb` | the model. 61 boxes plus a 12x8 face grid, 1,536 verts, 768 tris |
| `MapSource/materials_persons/person_team-zack.tres` | its sixteen colours |
| `Assets/.../Editor/PersonSwapProbe.cs` | the check that says whether a replacement actually works |
| `Assets/.../Tests/PlayMode/ModelPreviewTests.cs` | a select-screen test for the replaced character |
| `Roster.cs`, `RosterBookBuilder.cs`, `ConvertedCharacterSelect.cs` | ZACK, in ATE GIRLIE's seat |
| `Assets/.../Shaders/Toon.shader` | the palette fix, on `main` |
| `Assets/.../Editor/ModelSheet.cs` | the cast sheet renders palettes now |

`character-female-b.glb` is still in the repo and still imports. Nothing points at
it, which is what §8.3 asks for while a replacement is being measured.

## 2 · The bug that was found on the way

**The person palette was dead in this port.** Every character rendered in Kenney's
factory colours with `_UsePalette` set to 1 and sixteen correct values uploaded to
the GPU.

`Toon.shader` picks a colour by which 32x32 cell of the 512x512 atlas a UV lands in,
and remaps rows 8 and up. glTFast flips V on import, so a cell authored in `.glb`
row *r* arrives in Unity row *15 - r*. Both person meshes are authored in file rows
8 to 15, which is where the shader's slot table was measured from and what Godot
reads directly, and every one of them lands in Unity rows 0 to 7. The branch never
ran for any character.

Nothing logged it. Falling through to the raw atlas is the deliberate degrade path
for a model that samples the wrong rows, so the symptom is only visible if you
already know what the cast is supposed to look like, and the cast sheet was rendering
without palettes at the time, so it could not have shown it either.

Committed to `main` on its own, because it fixes eleven characters that have nothing
to do with this branch.

## 3 · How the model is built

The expensive half of replacing a Person is not the shape. A Kenney mini is a pile of
boxes and so is the art replacing it. The expensive half is keeping 32 authored clips
working across the swap, and that is exactly what a script can do exactly and a
modelling session cannot.

**Kept:** both skins, all 32 clips, the material, the colormap atlas, every bone name.
**Replaced:** vertices, normals, UVs, skin weights, and the bone rest positions.

### The skeleton turned out to be movable

§8.2 implied it was not. The real constraint is narrower: the clips must not be
contradicted. `tools/glb_anim_channels.py` was written to check rather than keep
assuming, and the answer is that `head`, `arm-left` and `arm-right` translations are
never keyed by any clip, and `root`, both legs and `torso` are keyed by four clips
between them with absolute local positions. Shifting a rest position and every
keyframe of its tracks by the same vector moves the bone and preserves the animation
exactly, because only the difference between the two is motion.

That is what got this character to **32 / 29 / 38** legs, torso, head against the
Kenney rig's 24 / 23 / 53. Without it the replacement is a recolour of somebody
else's proportions.

⚠️ The inverse bind matrices are recomputed, not patched. The whole rest pose is pure
translation with identity rotation and scale, measured off the base file, so
`IBM = translate(-worldPosition)` outright. Skipping this skins the mesh against the
old bind pose and the character comes apart limb by limb the first time it moves,
with no error attached.

⚠️ Limb LENGTHS are treated separately. These limbs have no knee, so a hip rotation
sweeps the whole leg and doubling the leg doubles the authored stride. The legs grew
32% and nothing else grew at all.

### Colour comes from the palette, not a texture

Every box declares a palette slot by where its UV lands. A bespoke texture would work
exactly once and then opt this character out of the recolour mechanism, the stun
frost and the two-band toon pass that the other eleven get for free.

The palette is emitted by the same script and from the same table that lays out the
UVs, because a slot number in the mesh and a colour in the palette are two halves of
one decision. Split across two files, a renumbered slot silently repaints a limb with
nothing failing.

## 4 · The name

ZACK takes ATE GIRLIE's index rather than being appended.

⚠️ Deleting the row would shift TIKBOY and everyone after him up by one, and
`character_index` crosses the wire as a bare int, so two peers on different builds
would render different people into the same seat with nothing to warn either of them.
Overwriting a row leaves every index exactly where it was.

The cost is a saved pick: anything on disk holding the id `ate_girlie` no longer
resolves. Acceptable here and only here, because the Godot build is still the
shipping one and this port has no players with saved rosters yet.

This is also the first entry where the two builds disagree by design.
`character_roster.gd` still carries ATE GIRLIE at this index and should, because the
replacement cast is being authored on this side.

Traits are unchanged from the row he replaces. The art moved; the balance did not.

## 5 · What the probe covers

`PersonSwapProbe` exists because every way a swapped rig can fail is silent. A
character that imports cleanly, spawns cleanly and is simply wrong in play is the
normal outcome, not the unusual one.

It asserts the clip set against the base rig, every clip name `CharacterAnimator` can
ask for, the bone names, the vertices weighted to each arm, the authored height
against `PersonScale`, the palette rows, where a carried tsinelas lands, which way
the face points, which side the dye is on, that the roster resolves to the new mesh
with 32 clip references and 16 palette entries, that slot 8 is dark enough, that
every clip moves at least one bone, and that all seven emotes resolve to a clip that
moves.

Then it photographs the result, because the failure no assert catches is that it
imports, animates, and looks wrong.

⚠️ Two of the checks measure against the BASE RIG rather than against a constant: the
atlas row band and the facing. Both are conventions of the importer rather than of
the project, and the working rig is the only honest reference for either.

## 6 · Verified

- `dotnet test` 51/51
- EditMode 24/24
- PlayMode 33/33, including the new select-screen test
- `PersonSwapProbe` PASS
- Windows player built to `C:\Users\matth\Desktop\TumbangPreso-Unity\TumbangPreso.exe`

⚠️ One PlayMode timing test failed once at 2.96 s against an expected 1.5 s and
passed clean on a quiet machine. It was load, not the model. If a timing test fails
alone, check what else is running before going looking in the code.

## 7 · Where it does not match the reference

Honest list, so nobody re-derives it.

- **Proportions are chibi-er.** The reference is roughly Minecraft proportioned at
  37 / 37 / 25. This is 32 / 29 / 38. The legs are the one measurement that costs
  animation quality to change, so they grew 32% and no further, and the head kept the
  height the mop needs. Signed off: *"its okay to have stubby legs its part of the
  personality"*.
- **The hair is blockier.** The reference mop is a dense curl cloud; this is seven
  boxes ending at four heights.
- **The face is simpler.** Two eyes and a mouth on a 12x8 grid, no brows. Brows sat
  close enough to the eyes to merge into one dark bar at play distance.

## 8 · Left for the next pass

- Eleven CC0 rigs still to replace. `Port_Plan.md` §8.3 has the order.
- `ModelImportSetup` still imports Generic on purpose. That comment says to revisit
  when animations start coming from a library; this replacement reuses the shipped
  clips, so it does not trigger that yet.
- The old `person_ate-girlie.tres` is still in `MapSource/`. Nothing reads it. It is
  the Godot build's data and it is left alone deliberately.
