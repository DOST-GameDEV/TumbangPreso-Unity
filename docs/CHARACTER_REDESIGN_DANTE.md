# Character redesign prototype: Dante (displayed as Basilio)

> ⚠️ **START AT SECTION 15.9, PART J (the last part, the newest), then part I, then the rest of 15.9, then 15.8.** It is the current state (end of 2026-10-05, second session) and it
> supersedes anything above it that disagrees. Sections 1 to 14 are the record of how it got
> there, in order, and several early statements in them are no longer true (the prototype IS in
> Unity now, the rig is no longer seven bones, the model is no longer fully rigid, the cast is
> no longer one hero).

**Status, 2026-10-05, branch `QoLUpdates`.** A PROTOTYPE, built and rendered in Blender only.
It is NOT in the game: no roster row, no runtime code and no existing character file was
changed, and it has never been opened in a Unity scene. Nothing is committed or pushed; the
coordinator commits after reviewing. The owner has seen renders and is still choosing between
options (see "Not yet decided").

This file is the handoff. It stands alone: a new session should read this, then the three
character docs it points to, then look at the pictures in
[reports/character-redesign-dante-2026-10-05](reports/character-redesign-dante-2026-10-05/README.md).

## 1. The owner's request and every piece of feedback, in order

All on 2026-10-05. Quotes are verbatim.

| | The owner said | What it changed |
|---|---|---|
| a | "can you try redesigning one of the normal blocky character models? add texture, more unique shapes and be less oriented around the whole blocky aesthetic" | Started the work. Context given with it: beside Paete (the golem, carved blocks wrapped in vines) the regular cast read as plain flat boxes, and a flat face has "no shading, texture or any of that stylized character". First pass: a fully rounded head (jaw, cheeks, pointed chin), lofted round limbs, thin swept hair locks, a hand-painted atlas. |
| b | On the first rounded-head close-up: "i dont like the deviation from the boxy head.. i know i said stray further from the original boxy design, but its somewhat part of our game's identity" | The rounded head and round body were dropped. Everything was rebuilt from chamfered blocks on the original's measurements. The painted textures were kept. |
| c | On the note that the head would then have flat faces and square corners: "not necessarily, just not entirely a cube" | The head became a CARVED block: superellipse rings of exponent 4 to 5 (soft corners, a faintly bowed face, the jaw tucked in a little, a small nose wedge). Three depths of carving were built so the owner can point at one. |
| d | On the boxy turnaround, with spots circled (top-front and the whole back of the hair; the ear on the horn side; both hands and the coat hem by the hand): "looks weird in these spots. hair texture overlaps to the ears, weird colors on hands, hair shimmer idk if i like it, it looks weird in some spots" | Painted hair highlights removed (hair is now three flat tones). Hair-black and stubble paint taken off the ears. Bandage, sleeve and cuff paint taken off the hands. A close pass over the whole model found and fixed more of the same class (section 8). |
| e | Pointing back at the early spiky-hair close-up: "can we have one version with this non-blocky hair model?" | A second variant, B: the same head, face, body and textures wearing tapered pointed locks refitted to the block head. Exported as its own `.glb`. |

**The standing direction these add up to.** The head is the game's box head: shaped, but
still a box. Detail comes from painted texture and from pieces ADDED to the blocks, not from
reshaping the blocks into anatomy. The body stays in the blocky family (chamfered, tapered,
layered pieces). Nothing is invented that the original character does not have. A player
should say "that is the same kid, with a lot more detail", not "that is another art style".

## 2. Which character, and where the original lives

The owner described the kid they were looking at: orange-brown skin, a blocky black hair mass
with a fringe, one squinting eye and one yellow eye with a scar down that side, a small frown,
a green jacket with gold trim, brown trousers, white shoes. That is roster id `dante`,
displayed as **Basilio** (AGENTS.md, "Owner-approved character names"). Lowercase ids stay
`dante` everywhere in code and assets.

| What | Where |
|---|---|
| Model in the game | `Assets/TumbangPreso/Art/characters/persons/team-dante.glb` (two meshes, `body-mesh` and `head-mesh`, 7,741 triangles, palette UVs on the shared 512 px `Textures/colormap.png`) |
| Its builder | `tools/build_bayan_voxel.py` (writes `team-bayan.glb`, the same character under his old name; `team-dante.glb` was later touched by `tools/author_cast_finish.py` and `tools/author_hand_volume.py`) |
| Roster entry and 16 colour palette | `Assets/TumbangPreso/Resources/Roster/person_dante.asset`; `RosterBookBuilder.PersonModels` maps `dante` to the `.glb` |
| First-person arms | `Assets/TumbangPreso/Resources/Models/RosterArms/dante_left.asset`, `dante_right.asset` |
| The horn | The original HAS it. `tools/build_bayan_voxel.py`, function `_add_horn_geometry`, and the `head-mesh` of `team-dante.glb` (palette slots 9, 4 and 14). It is kept, same root and tip. |

Paete ("the golem") is `team-paete.glb`, built by `tools/build_paete_voxel.py`. AGENTS.md
protects the finalized Paete and Phaister; this work does not touch them.

## 3. What the character docs require, and how the prototype stands against each

Read [CHARACTER_MODEL_METHOD.md](CHARACTER_MODEL_METHOD.md),
[CAST_CLOTHING_STYLE.md](CAST_CLOTHING_STYLE.md), [Voxel_Person_Guide.md](Voxel_Person_Guide.md)
and [Art_Direction.md section 0](Art_Direction.md#0--new-models-must-belong-to-tump).

| Requirement | Prototype |
|---|---|
| Keep the cute blocky cast, flat faces, simple hands (AGENTS.md) | DEPARTS, on the owner's newer words, and only as a prototype. Head is a carved block, not a cube. Hands are plain blocks with drawn fingers. The face is painted with shading. |
| The seven bone names, rest pose, clips (Voxel guide section 3) | MET. The `.glb` is `team-dante.glb` with only its two meshes replaced. Checked: nodes, inverse bind matrices and all 38 animations are byte-identical. |
| Authored height and feet on zero | MET. Feet at 0, top of hair at 0.785 to 0.789, the original's is 0.785. Arms straight out, as the rig rests. |
| Hand top where a carried tsinelas sits (`CharacterVisual.PalmCentre`, `HandTopLift`) | MET by measurement in the build (hand top 0.056 above the arm bone, the original 0.0555 to 0.060). Not seen in Unity. |
| UVs in atlas rows 0 to 7 take the palette; slot 8 is the face ink | DEPARTS. Every UV sits in the top half of his own atlas, where `Toon.shader` samples the texture and not the palette. This is the beggar NPC's mechanism (`tools/build_beggar_voxel.py`). The face is paint, not slot 8 geometry. |
| Proportions legs 24, torso 23, head 53 | MET. Leg, torso and head heights are the original's. |
| Chamfered boxes rigid to one bone; muscle as form | MET for the body. Every piece is a chamfered block, some tapered. All skinning is rigid, one bone a vertex. |
| No eyebrows, ink-only face, no glints | PARTLY. No eyebrows. The original Dante already carries a gold iris and one glint; those are kept. Added by paint: blush, socket shade, fringe shadow, scar ticks. |
| Hair: a core with separate chunky clumps at graded heights | MET in variant A. Variant B replaces the clumps with pointed locks at the owner's request. |
| Each mark its own hand-set shape, no loop stamping a motif (method section 4) | MET in the textures script. Every patch, fold, stitch row and scuff has its own numbers. |
| One hue per hero, quiet; one metal | MET. The original's forest green, gold and jade, unchanged hex values as the base tones. |
| Clothing language: dark base, signature on layers, gold piping as GEOMETRY, one big fastening, flared lower layer, cuffs as geometry, two-tone hair (clothing doc rules 1 to 7) | PARTLY. Piping (hem, lapel edge, collar lip), the buckle, coat-tails and the rolled cuff are geometry. DEPARTS on rule 1: the jacket reads green overall with brown showing only in the opening, because the owner described it as "a green jacket". Rule 7: hair is a lighter top plane and darker underside, no painted shine. |
| Role hues (#f87020, #0080e8) kept off large areas | MET. The textures script refuses a cloth colour near either hue. Skin is exempt, as for the cast. |
| Review in Unity, in the lineup, a new versioned folder every iteration (method section 5) | NOT DONE. Reviewed in Blender only, by order (the owner's editor was open). See section 9. |
| Design brief in `ArtSource/<hero>/<pass>-<date>/design-brief.md` | NOT WRITTEN. This document stands in for it. |

## 4. What exists now

| File | What it is |
|---|---|
| `tools/author_character_redesign_dante_textures.py` | Paints the atlas (system Python, PIL). Also holds the island LAYOUT that the model script imports. |
| `tools/author_character_redesign_dante.py` | Builds the model in Blender, writes both `.glb` files and the `.blend`. |
| `tools/render_character_redesign_dante.py` | Blender review renders (toon ramp and ink edge approximated in EEVEE). |
| `tools/sheet_character_redesign_dante.py` | Stacks and labels those frames into review sheets (PIL). |
| `Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb` | Variant A, block hair. 3,373 triangles. |
| `Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign-spiky.glb` | Variant B, lock hair. 3,469 triangles. |
| `Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign-atlas.png` | The one atlas both share. 2048 x 2048, paint in the top half only. |
| `ArtSource/dante/redesign-20261005/dante_redesign.blend` | Both variants on the original armature with its clips. B's two meshes are hidden. |
| `docs/reports/character-redesign-dante-2026-10-05/` | The pictures. |

The prototype folder is outside `Resources`, so the game does not load it. The owner's open
Unity editor did import it: `.meta` files for the folder, both `.glb` files and the atlas now
exist. They are harmless and belong with the files if these are ever committed.

**Rebuild** (from the repository root, Git Bash). Paint first, then build:

```bash
py -3 tools/author_character_redesign_dante_textures.py
"C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_dante.py
```

**Switches** of the model script, after `--`:

| Switch | Meaning |
|---|---|
| (none) | Builds both hairs with the `carved` head, writes both `.glb` files and the `.blend`. |
| `--head block` or `carved` or `shaped` | How far the head block is carved. `carved` is the default. |
| `--hair blocks` or `locks` | Which hair, when writing one variant with `--out`. |
| `--out path.glb` | Writes that ONE variant and leaves the `.blend` alone. The file must sit beside a copy of the atlas. |

The textures script takes `--size 1024` (untested beyond painting) and `--sheet file.png`.

**Re-render** (a new version name every time):

```bash
"C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_dante.py -- v12
py -3 tools/sheet_character_redesign_dante.py v12
```

Frames go to `Logs/character-redesign-dante/v12/`, sheets beside that folder. Shot names can
follow the version to render only some: `turn face far trio close golem heads`. The `heads`
shot needs the three head carvings built first; the commands are in that script's header.

**Budgets.**

| | Original | Redesign A | Redesign B |
|---|---|---|---|
| Triangles | 7,741 | 3,373 | 3,469 |
| Texture | shared 512 px colormap plus a 16 colour palette | own 2048 x 2048 atlas, 45 islands, top half used | same atlas |

The atlas is the one real cost: the cast shares one small texture today, and four of these
on screen would be four 2048 atlases. Only half of each is used because of the palette rule
in `Toon.shader`; a material with the palette switched off could use a 2048 x 1024 or a
1024 square atlas instead. That needs a Unity check first.

**Rig and pose.** Bones `root`, `leg-left`, `leg-right`, `torso`, `arm-left`, `arm-right`,
`head`; legs and torso under `root`, arms and head under `torso`. Every vertex is weighted 1.0
to one bone. `body-mesh` uses joints 1 to 5, `head-mesh` joint 6. Faces -y in Blender (+z in
glTF), feet on zero, arms straight out along x. The game's `idle` clip drops the arms 45
degrees; all renders use `idle`.

## 5. The design as it stands

**Head.** A carved block on the original's box (0.340 wide, 0.322 deep, 0.343 to 0.661):
24-sided superellipse rings, exponent 4 to 5, so the corners are soft and the box still wins.
Ear blocks as on the original. A small nose wedge that leaves the face at the bridge. Current
default is `carved`. Alternatives: `block` (one chamfered box, flat face, no nose) and
`shaped` (cheek fullness, a brow ledge, a tapered jaw). Picture 04 shows all three beside
the original.

**Face**, painted on the front of the block: a lit centre, edges that turn away, a stepped
cast shadow under the fringe, socket shade, a low blush, jaw shade in two steps, a nose
shadow. His right eye is the squint (one heavy lid, a slit, a crease under it). His left eye
is the original's: black almond, gold iris, slit pupil, one glint. The scar runs forehead to
jaw through the gold eye, dark with a pale core and three cross ticks. A small frown.
The shaved left temple is drawn as stubble.

**Hair A (block hair).** A slab on the crown that stops short of his left edge (the shaved
temple), a mass down the back ending in three blunt points, a panel over his right ear, a
sideburn, a stepped fringe of four slabs (longest on his right, the gold eye left clear) and
four crest chunks of different heights.

**Hair B (lock hair).** A thin cap that follows the block, then sixteen tapered six-sided
locks set by hand: a three-lock fringe swept to his right, a sideburn, six crown spikes, four
nape locks, one behind and one in front of his right ear.

**Hair shading, both.** No painted highlights. Faces turned up take a lighter tone
(`#2b2933`), sides the base (`#1a181e`), undersides a deeper one (`#0e0d12`). On B one
outward facet per lock is the lighter tone.

**Horn.** The original's, kept: five flat planes round a swept curve, same root on the shaved
temple and the same tip, slate with a violet edge and drawn growth rings.

**Body pieces**, all chamfered blocks: a torso wider at the shoulders than the hips; a belt
7 mm proud; a gold buckle plate with a jade stone; two flared coat-tails, open at the front,
lower behind than in front, each with a gold hem band; gold lapel edges and two frog bars
with jade knots; a standing collar round the foot of the head, taller on the scar's side,
gold along its top; his left arm bare with a torn sleeve stub at the shoulder and a cloth
wrap on the forearm; his right arm sleeved with a fat rolled gold cuff and a cord bracelet;
block hands; trousers that lean in toward the hip, a rolled cuff at the ankle; shoes that
rise from toe to ankle on a wider sole, feet turned out seven degrees.

**Textures and UVs.** Each body part is drawn from up to six sides (front, back, the two x
sides, top, bottom) and each side is one island, a flat orthographic view of the part in
model metres. A face takes the island its normal points at, at the place it sits in space,
so a belt drawn at one height meets itself round the body. Loose pieces (hair, horn, collar,
piping, and block ends that would otherwise land on another part's drawing) take small flat
swatches. Islands are shelf-packed with a 4 px bleed gutter. Painted detail: sun patches and
folds on the jacket, seams and stitch rows, a chest pocket, the old cape emblem brushed on
the back in gold, a sewn patch on the back tail and on the left knee, dust at the hem and
the trouser cuffs, slanted wrap lines on the bandage, a toe line, one scuff per shoe, a green
heel tab.

**Colours.** The original palette's hex values are the base tones: skin `a8602c`, hair
`1a181e`, green `3d6335`, gold `dfb248`, brown `482f1d`, jade `38b848`, white `f4faff`, eye
gold `ffd700`. Painted tones are steps of those. All listed at the top of the textures script.

## 6. Not yet decided by the owner

1. Hair A or hair B.
2. Which head carving: `block`, `carved` (built) or `shaped`.
3. Whether the hair shine fix is right (flat tones only), or whether a few short crisp
   strokes on the clump tops would be wanted. The owner said "hair shimmer idk if i like it".
4. Whether this direction then goes to the rest of the cast, and to which character next.
5. Whether the jacket should read green overall (now) or keep the original's dark brown base
   with green layers (clothing doc rule 1).

## 7. Known weak spots, honestly

- Hair A is plain from behind and above: one large flat black slab with three points. It no
  longer shimmers, but it also carries no drawing at all.
- Variant B shares the atlas, so its forehead shows the STEPPED cast shadow drawn for A's
  four fringe slabs. Under pointed locks it reads as blocky shadow shapes. B needs its own
  forehead shadow if B is chosen (a second `head.front` island or a per-variant atlas).
- The collar now sits as a ring round the foot of the head, resting on the shoulders. It
  clears the head block, but the head still turns inside it during clips, and it is not
  attached to anything at its base. The original's collar blocks have the same problem.
- The upper arms still press into the coat-tails in `idle` (the original does too). The
  tails were narrowed so the wrist and hand clear the hem.
- Limbs have no elbow or knee, as on the cast, so every shaped piece is rigid; the wrap and
  cuff rotate with the arm.
- The face is soft, feathered paint on a model whose neighbours are flat colour. Beside
  Paete it reads as a different surface treatment, though the same block family.
- Texel density is uneven: the face gets about 1,030 px per metre, limbs less. 36 per cent
  of the usable half of the atlas is empty (shelf packing).
- The shoes carry little detail (a toe line, one scuff, a heel tab).
- Colours in the renders come from an EEVEE imitation of the toon shader, not from the game.

**Unverified, because it has never been in Unity:**

- `TumbangPreso/Toon`: the real two-band ramp, tonemap, fog and world look on painted
  texture; whether the bottom-half palette rule behaves as read from the shader source.
- The ink outline: `ToonSkin` welds normals and inflates a hull; thin pieces (lapel edges,
  frog bars, lock tips, the collar lip) may close up or spike at 0.0045 width.
- glTFast import of an external atlas named by relative URI, bilinear with mips.
- The animation rig in play: all 38 clips on the new meshes, interpenetration in `walk`,
  `sprint`, `slide`, `sit` and Dante's five hero clips.
- The carried tsinelas on the hand (`CharacterVisual.BuildHandAnchor`, `PalmCentre`).
- `WindTumble.Resolve` and any other bone lookup by name (names are unchanged).
- First-person arms: `RosterArms/dante_left.asset` and `dante_right.asset` are separate
  assets and still show the OLD arms (runes on the left, brown sleeve on the right).
- Avatar, hero select, showcase, HOME loop and mode card art, all made from the old model.
- Performance of four textured characters on a weak PC.

## 8. What the close pass found and fixed (feedback d)

| Fault | Cause | Fix |
|---|---|---|
| Glossy smears on the hair | Painted highlight strokes on a swatch wrapped round each clump | Hair takes three flat tones by which way a face points; no drawing |
| Black on the horn-side ear | The ear's top faces sampled the head's `top` island, which was filled hair-black; its back faces caught the stubble patch | `head.top` is skin; stubble stops above the ear on the front, side and back islands; a `bottom` island added |
| Pale and green patches on the hands | Shade bands for the bandage, sleeve and cuff were drawn the full width of the arm islands | Every such mark now stops at its piece's edges; the hand is repainted skin last |
| A pale wedge by the hem | The bandage's loose end, a thin wedge hanging beside the hand | Removed |
| A brown and white wedge between wrist and coat | The trouser block was wider than the narrowed coat-tail and came through its wall | Trousers lean in toward the hip; their top cap is brown |
| Skin-coloured polygons on the collar | The head block's lower corners came through a collar that started on the narrower shoulders | The collar now sits wholly outside the head block |
| A ring drawn on each fist | A thumb block whose ink edge read as a ring | Removed; the thumb is one drawn hook |
| A brown dart on the toe box | The trouser-cuff band of the leg islands landing on the shoe's upper edge ahead of the cuff | That region is painted shoe white |
| White rim on the trouser cuffs; skin-coloured cuff ends; green belt ledge; brown buckle sides | Block END faces sampling another part's island | Those faces take a flat tone of their own piece |
| Arms running into the coat-tails | Tails flared to 0.176 half width | Tails kept inside 0.160 |

## 9. What putting it in the game would take (not done)

Governing docs: [CHARACTER_MODEL_METHOD.md](CHARACTER_MODEL_METHOD.md) sections 5 and 6,
[CANONICAL_RENDERING_PIPELINE.md](CANONICAL_RENDERING_PIPELINE.md), [TESTING.md](TESTING.md),
[Voxel_Person_Guide.md](Voxel_Person_Guide.md) sections 2 and 3.

1. The owner picks hair, head carving and whether to proceed.
2. Look at the prototype in Unity WITHOUT wiring it in: a probe scene or an editor probe
   that dresses the prototype `.glb` with `ToonSkin.Apply` beside `team-dante.glb` and the
   cast lineup. Judge the real shader, outline and atlas. Fix what shows.
3. Decide the texture budget (palette off and a smaller atlas, or keep 2048).
4. Archive the current model beside a design brief in `ArtSource/dante/`.
5. Make the builder write `Assets/TumbangPreso/Art/characters/persons/team-dante.glb` and
   put the atlas beside it; decide what `person_dante.asset`'s Palette holds (systems such
   as `PaeteGroundCall` and `VoxelDresser` read palette slots by index).
6. Rebuild the roster book (`RosterBookBuilder`), run `PersonSwapProbe` and
   `HeroTurnaroundProbe`, and look at the turnaround and the lineup.
7. Check motion with `ClipMotionStrip` on the game's action names, Dante's hero clips included.
8. Rebuild what is derived from the model: first-person arms (`RosterArms/dante_*`,
   `ViewmodelArms.SkinColorForCharacter`), avatar (`tools/build_avatars.py`), mode card pose,
   hero select and showcase captures.
9. Focused native checks and an internal build, per TESTING.md. Then the owner plays it.

## 10. Working rules for the next session

- The owner's Unity editor is usually open on this worktree. Batch Unity needs it closed.
  Never kill it. Until told otherwise this work is Blender only.
- Every art step is: render, LOOK at the picture, critique it against the owner's words, fix,
  then show. Close-ups catch what a turnaround hides (section 8 was all found that way).
- Versioned filenames, a new version every iteration.
- No commits or pushes unless the owner asks. Never commit the owner's own uncommitted files
  (the worktree carries many modified `.meta` files that are not this work).
- No em dashes in code, comments, docs or commit messages.
- Do not modify `team-dante.glb`, `build_bayan_voxel.py`, the roster or runtime code while
  this is a prototype. Paete and Phaister are finalized and protected (AGENTS.md).
- One character at a time. This direction is not approved for the rest of the cast.

## 11. Where the pictures are

[reports/character-redesign-dante-2026-10-05/README.md](reports/character-redesign-dante-2026-10-05/README.md)
indexes twelve images: the original beside A and B, the A and B turnarounds and faces, the
three head carvings, the 10 m size check with silhouettes, the redesign beside Paete, the
texture sheet, the close-ups of the fixed spots, and one image of the rejected rounded head.

## 12. Second session, 2026-10-05: the owner's choices and v12 to v14

Worked in worktree `dante-character-redesign-873f79` (branch
`claude/dante-character-redesign-873f79`, fast-forwarded to `a883bbe70`), so the map session
in the other worktree is not disturbed. Still Blender only. Nothing committed.

**Decided by the owner (closes section 6 items 1 and 2):** hair **A** (block hair) on the
**shaped** head. `HEAD_VARIANT` now defaults to `shaped`. B is still built, as a reference.

| | The owner said | What changed |
|---|---|---|
| f | "fix the asymmetrical and detached hood" (both collar ends circled) | The collar is the same on both sides, starts 5 mm inside the torso's top at shoulder width, spreads under the jaw as a shelf and stands as a wall 8 to 12 mm off the shaped head. Its top stays under the ear blocks. `COLLAR_TOP`, `COLLAR_PROFILE`. |
| g | "theres z-fighting on these patches" (knee patch, back tail patch) | Checked the source frames: both patches are paint on one surface, no coplanar geometry, and they are stable frame to frame. The shimmer was the GIF's per-frame palette on a low-contrast soft olive mark. Fixed both ways: patches are now hard-edged scraps of the OTHER garment (brown on the coat, green on the knee) with a dark rim and pale thread, the knee patch sits wholly below the hem, and the turntable GIF uses one palette with no dithering. |

**Weak spots of section 7 fixed for A:** the back of the hair is no longer one slab. Three
hand-set slabs (`BACK_SLABS`) hang from the crown over the back mass, 15 mm proud, and the
mass under them takes the deeper tone. The crown slab is lower (0.738) with taller, more
unequal crest chunks.

**Still open:** the shoes are plain; arms still press the coat-tails in `idle`; B still shows
A's stepped forehead shadow (B was not chosen); everything under "Unverified" in section 7.
Next is section 9 step 2, the Unity side-by-side.

Renders: `Logs/character-redesign-dante/v14*` (turnaround, face, close-ups, 10 m, golem, and
`v14_A_shaped_360.gif` with its eight-angle sheet). The turntable script is not in `tools/`.

**v15 to v16, same session.** Owner: "is the head passing through the hood?" (skin circled
between the nape points) and a request to see `jump`, `fall`, `walk`, `sprint` and the head
looking around.

- It was not passing through. v14 had lowered the collar's back below the hair's foot, so the
  back of the head showed over the rim. v15 raised the collar into the hair; a head-turn test
  then showed the hair (head bone) cutting through the collar (torso bone) at 20 degrees.
- v16: the back of the collar stops at 0.404, under the hair's foot; the three nape points hang
  wholly outside its wall; a thin dark `hair-nape-liner` on the back of the head, inside the
  collar, is what shows over the rim. Clean from behind at 20 and 35 degrees of yaw and 12 of
  pitch. At 55 degrees the jaw corner still dips into the collar's front: a box turning inside
  a fitted square collar cannot avoid that, and the game's procedural head motion is far smaller.
- The game DOES drive the head bone in code (`CharacterAnimator.LocomotionArms`, `ThrowBody`,
  `TagBody`, `DanceClip`, `HeroAbilityClips`), so the collar has to be checked with the head
  turned, not only in `idle`.
- Clips on the new meshes, in Blender: `walk`, `sprint`, `jump`, `fall`, `idle` all play with
  no tearing (every vertex is rigid to one bone). In `jump` and `fall` the legs swing out
  through the coat-tails, which ride the torso; the original's tails do the same.
  `Logs/character-redesign-dante/v16_anim_*.gif`. The look-around is a rig test (head bone
  turned by script), not a game clip; the rig has no such clip.

**v17 to v18, same session.** Owner: "make sure nothing is clipping into clothing", "make it so
the clothes will bend/distort to follow his body", "the back of his cloak is supposed to be
connected, like a coat tail", and "the animations we have dont fit the poppy/cartoony style".

- **The model is no longer fully rigid.** `Part.bend` gives a piece blended weights. Three pieces
  use it: the coat skirt (torso at the top, up to 0.78 of each leg at the hem, the two legs
  mixed across the middle), the collar (0.55 of the head at its rim) and the tips of the three
  nape points (0.6 of the torso). Everything else is still one bone a vertex. The `.glb` now
  writes four joints and weights a vertex. This DEPARTS from the cast's rigid rule (Voxel guide)
  on the owner's words.
- **The coat skirt is one piece**, open only at the front (`coat-skirt`), replacing the two
  split tails.
- **New clips, prototype only:** `tools/author_character_redesign_dante_clips.py` writes
  `walk`, `sprint`, `jump` and `fall` into the two prototype `.glb` files in place of the ones
  copied from `team-dante.glb`, same lengths. Bounce, squash and stretch as scale on `root`
  (a NEW channel), snapped swings, torso twist against the legs. `team-dante.glb` is untouched.
- **A limit found:** the arms cannot rise above straight out. The head is wider than the
  shoulders and starts at the top of the arm, so a raised arm goes into the collar and then
  the head. `jump` and `fall` throw the arms wide instead of up.
- **Checked by eye only**, in Blender, on `idle`, the four new clips and the head-turn test.
  There is no measured penetration test. Not checked: `slide`, `sit`, the five hero clips, the
  emotes, the upper arms against the skirt in `idle`, and all of it in Unity, where the game
  adds procedural motion on top of the clips.

Renders: `Logs/character-redesign-dante/v18_anim_*.gif`, `v18_extremes.png`.

**v19, same session.** Owner: "should try bending the arms for animations, especially for the
sprint and walk" and "the head still clips in the looking around".

- **Two elbow bones, which the cast does not have:** `forearm-left` and `forearm-right`, children
  of the arm bones at x 0.172, appended AFTER the seven so their names and indices do not move.
  Each arm is now two rigid blocks that overlap at the elbow. `walk`, `sprint`, `jump` and
  `fall` key them; every other clip leaves the arm straight, as before. The rig is therefore NO
  LONGER byte-identical to `team-dante.glb` (section 3's "seven bones" row no longer holds).
- **What the elbows cost in the game, not done:** `CharacterVisual.PalmCentre` and the hand
  anchor find the hand from `arm-right` alone, so a carried tsinelas would stay where a straight
  arm's hand would be. First-person arms are separate assets and unaffected. Any code that
  walks "the seven bones" by name still finds all seven.
- **The collar is the head's from its shelf up** (`COLLAR_FOLLOW` 0.92 from 0.358). It turns
  and nods with the head, so the jaw cannot turn through it; the twist is taken by the 20 mm
  between the shoulders and the shelf. Seen clean at 55 degrees of yaw and 22 of pitch, by eye.

Renders: `Logs/character-redesign-dante/v19_anim_*.gif`, `v19_extremes.png`.

**v20 to v22.** Owner: "make it so the arms arent bent too much towards the inside of the
torso". The folded forearm is turned 30 degrees OUT (`SPLAY`), the upper arm hangs wider, and
fold plus swing stays under about 105 degrees from hanging. On the elbows' cost in the game the
owner said: "if that's the case we'll have to rework it later on", so the elbows stay.

## 13. The rest of the cast (owner, 2026-10-05)

"for now we're focusing on fixing the character designs. so following dante's rework, redesign
the rest of the characters. spin up multiple agents to do this for each character, and the
animations i want them all in one gif for each character. remember to be critical of your own
work". This CLOSES section 6 item 4 and supersedes section 10's "one character at a time".

One agent a hero, each in its own files, all PROTOTYPES outside `Resources`:

| What | Where, for hero `<id>` |
|---|---|
| Scripts (copies of Dante's four, rewritten for the hero; never edit Dante's or another hero's) | `tools/author_character_redesign_<id>.py`, `_<id>_textures.py`, `_<id>_clips.py`, `tools/render_character_redesign_<id>.py`, `tools/sheet_character_redesign_<id>.py` |
| Model and atlas | `Assets/TumbangPreso/Art/CharacterRedesign/<id>/<id>-redesign.glb`, `<id>-redesign-atlas.png` |
| Source | `ArtSource/<id>/redesign-20261005/<id>_redesign.blend` |
| Renders | `Logs/character-redesign-<id>/vNN...` |
| The one motion gif | `tools/render_character_redesign_motion.py -- <id> vNN` then `tools/sheet_character_redesign_motion.py <id> vNN` (shared, do not edit) |

**The rules every hero inherits from Dante's review, each one a thing the owner said:**

1. The head is the game's BOX head, `shaped` carving (cheeks, a brow ledge, a tapered jaw),
   never a round skull. Detail is paint and pieces ADDED to blocks. Same kid, more detail.
2. Nothing invented. Every piece and colour is one the hero's current model already has. Keep
   each hero's deliberate signature (CAST_CLOTHING_STYLE.md: Nemu's cowl over her lower face,
   Cheska's pale ice colours).
3. No painted hair shine. Hair is flat tones by which way a face points. A flat back or top
   gets BLOCKS (layered slabs, graded chunks), not drawing.
4. Paint stops at its own piece's edges. No paint from one part on another (ears, hands).
5. A sewn mark is hard edged with a rim, or it reads as a render fault. Never half hide one
   under another piece.
6. Collars, hoods and scarves are symmetric unless the original is not, grow out of the
   shoulders, and from the jaw up they follow the HEAD bone, so the head cannot turn through
   them. Check with the head turned 55 degrees and nodded 22, from the front AND the back.
7. Cloth that lies across two bones BENDS (`Part.bend`): skirts and coat-tails take the legs
   toward the hem, long hair tips take the torso. A coat's back is one piece.
8. Two elbow bones, `forearm-left` and `forearm-right`, appended after the seven. Arms fold
   forward and OUTWARD, never across the torso. ⚠️ AMENDED the same day. The first wording said
   arms never rise above straight out; the owner then said "the arms dont really get much higher
   than the original A posing.. the jump looks like he's shrugging". A STRAIGHT arm still cannot
   rise (the head is wider than the shoulders), so arms go up THROUGH THE ELBOW: the upper arm
   lifts about 12 degrees, the forearm folds up and out past the cheek in a V, and may stretch
   (scale on the forearm bone) as it is thrown. A jump must read as arms UP.
9. New `idle`, `walk`, `sprint`, `jump`, `fall`, the same lengths as the clips they replace.
   `idle` is ACTED, not wobbled: stances a waiting person takes, held long enough to read
   (fists on hips, arms folded, a look about, a tapping foot), with breathing kept small
   underneath. Layered sines on every bone were tried and the owner said "he's just distorting
   around.. needs more character". It may be longer than the clip it replaces (Dante's is 8 s). `jump` is a STANDING jump, both feet together, never a stride
   (owner: "i need a jump for standing still"). All of them: bounce,
   squash and stretch on `root`, snapped swings. Each hero's motion is its OWN (AGENTS.md:
   copied cast-wide looks are not allowed): a heavy hero lands heavy, a light one floats.
10. Feet on zero, the original's height and proportions, the hand top where a tsinelas sits,
    role hues `#f87020` and `#0080e8` off large areas, under 6,000 triangles.
12. ⚠️ A CUTE FACE, NOT A PORTRAIT. Owner, on the whole redesigned cast: "the faces look too
    realistic and look too human, like it lost its charm, the characters have eyebags etc..
    they need to be more cutesy". NO nose (no wedge, no painted bridge or shadow). NO eye
    sockets, under-eye creases, lid lines, lip shadow, jaw or cheekbone contour, wrinkles. Eyes
    are the ORIGINAL's simple solid ink shapes, about a third bigger; the mouth is the
    original's, one stroke. Allowed: flat skin with one very soft lit patch, the fringe's shadow
    as one flat tone, and a round blush under each eye ON THE GIRLS ONLY (Cheska, Nemu, Amihan).
    The owner: "reserve the blush for the female characters". No blush on Dante, Sean, Zack, Rafi. Judge it beside the original's face: as
    cute, or cuter. (This removes the nose that came with the `shaped` head in rule 1.)
    ⚠️ THE EYE'S OUTLINE IS THE EXPRESSION, SO IT IS MEASURED, NOT REDRAWN. Two heroes lost
    theirs in the first cute pass: Cheska "lost the eye quirk" (a notch in the bottom of each
    eye) and Amihan lost her "slight smug look" (the slanted top edge of each eye). Take the
    slot 8 ink polygons off the original `team-<id>.glb`, vertex for vertex, left and right
    separately, and scale THOSE. Same for the mouth.
    ⚠️ NO DARK FACETS ON THE LOWER FACE. The `shaped` jaw turns under and the toon shadow band
    lands on it as hard dark shapes; the owner called them "random dark spots". The front plane
    runs clean to the chin and the head's closing ring is hidden in the neck or collar.
11. Render, LOOK, write down what is wrong, fix, render again under a new version name.
    Close-ups and the turned head catch what a turnaround hides.

Held back: **Phaister (Soraya) and Paete** are finalized and protected by AGENTS.md. No
prototype was started for them; that needs the owner's word. The twelve Classic street
characters were not started either.

**v23 to v25.** Jump and fall throw the arms up in a V through the elbows, with a forearm
stretch at take-off; fists reach eye height beside the head, which is the ceiling for this
body without passing through the head. The motion gif is now a GRID of ten cells that all play
at once (owner: "more like a grid where all animations play at the same time"), 96 frames at
24 a second. `Logs/character-redesign-dante/v25_dante_motion.gif`.

**v26 to v27.** Owner: "fix the idle animation to look less linear and livelier" and "is the jump
supposed to be one foot forward? i need a jump for standing still". The one-foot-forward pose was
the old clip's, which the first new `jump` had kept. `jump` is now symmetric (toes trail, then
both legs swing up in front). `idle` is new: breathing as squash on `root`, a rock from foot to
foot, torso, head and arms each a beat behind. `Logs/character-redesign-dante/v27_dante_motion.gif`.

**v28 to v30.** The sine idle was rejected ("the idle looks like he's just distorting around..
needs more character, like sometimes he puts his hands on the waist, or crosses his arms").
`idle` is now an 8 s acted loop: stand, fists on hips and a look each way, drop, arms folded
with a tapping foot and a look away, let go. Arms are posed by the direction they point
(`_arm` in the clips script). Folding needs the forearms stretched 1.55 times to cross at all;
at their own length they only meet. ⚠️ The idle is no longer 1.33 s; check anything in the game
that assumes its length, or split the stances into clips the game picks between. The grid gif
runs 192 frames when the idle is longer than 96. `Logs/character-redesign-dante/v30_dante_motion.gif`.

### Cast status at the end of 2026-10-05 (all prototypes, Blender only, nothing committed)

| Hero | Version | Triangles | Idle stances | Shown to the owner | Main open faults |
|---|---|---|---|---|---|
| dante | v30 | 4,017 | fists on hips; arms folded with a tapping foot | yes | upper arms press the skirt in the stand; shoes plain; jaw dips into the collar front at 55 degrees |
| cheska | v05 | 5,596 | warms her hands; two hops; tugs her hat strings | yes | jump arms only reach shoulder height (ear flaps); flap fur shears at a 55 degree turn; the hop does not read in a still |
| sean | v05 | 5,360 | bicep flex; bull lean with a pawing foot; guard and two jabs | yes | nape tail stretches at 55 degrees; plain back of the skull; guard reads as arms held out from the front |
| amihan | v06 | 5,900 | hands behind her back on her toes; two hops; feeling the wind | yes | cuffs cut the torso side in the stand; wind stance reads as a wave; forearms thin when stretched |
| nemu | v08 | 4,688 | nods off and jolts awake; yawn and stretch; sleeve swish | yes | sleeves inside the torso with arms down (as the original); looks plainer than the rest; sprint sleeves now held out, not streaming |
| rafi | v07 | 5,964 | hands behind his back; swimmer's shake-out; hand on his shark tooth | yes | front flap against the forward thigh in walk and sprint; forearms stretch about twice their length in the jump |
| zack | v08 | 5,980 | hands in jacket pockets; fixing the quiff; hands behind his back | yes | pockets stance has the fists sitting ON the jacket front; quiff hand reaches the temple, not the lock |

Every hero's jump puts the fists at cheek to ear height in a V, never overhead: the heads are
wider than the shoulders. Every idle is 8 s. Every model has the two elbow bones. None has been
in Unity, none has a measured penetration test, and `slide`, `sit`, the hero clips and the
emotes are unchecked on all of them. Each hero's own faults are in the comments of its scripts
and its renders are under `Logs/character-redesign-<id>/`.

## 14. Playable swap, prepared and NOT yet run (owner, 2026-10-05: "i want to see this in game")

The owner chose a playable swap in this worktree and said NOT YET to launching Unity. Prepared,
uncommitted, no Unity run:

- `Editor/RosterBookBuilder.cs` `PersonModels`: the seven redesigned heroes point at
  `CharacterRedesign/<id>/<id>-redesign.glb`. Phaister and Paete are unchanged. Reverting those
  rows and rebuilding the roster book undoes the swap; no `team-<id>.glb` was touched.
- `Runtime/Visual/CharacterVisual.cs` `HandBones`: `forearm-right` and `forearm-left` are tried
  before the arm bones, so a carried tsinelas follows the bent hand on a rig with elbows. Cast
  rigs have no such bone and behave as before.
- `.meta` files for the six new models and atlases, copied from Dante's prototype with fresh
  GUIDs, so they import with the same glTFast settings.

When the owner says go (editor closed, Hub signed in, nobody else running Unity), in order:
`RosterBookBuilder.Build` through `tools/run_unity_guarded.py`, look at `HeroTurnaroundProbe`
or `PersonSwapProbe` output, then `GameBuilder.BuildWindows -buildOutput Builds/<name>/TumbangPreso.exe`.
This worktree has no `Library`, so the first launch is a full import.

Expected to be wrong on first sight, none of it checked: first-person arms (separate assets,
old look); `CharacterAnimator.EdgeRecovery` and other code that measures the palm on
`arm-left`/`arm-right` by index; the game's procedural squash (`CharacterSquashStretch`)
stacking on the clips' own `root` scale; anything that assumes a 1.33 s idle; palette recolours
(the atlas models ignore the palette); avatars, hero select and showcase art made from the
old models; tests that assert seven bones or the old heights.

## 15. CURRENT STATE, end of 2026-10-05 (read this first)

### 15.1 Where the work is

- Worktree `.claude/worktrees/dante-character-redesign-873f79`, branch
  `claude/dante-character-redesign-873f79`, HEAD `a883bbe70` (the first session's commit).
  **Everything since is UNCOMMITTED and nothing is pushed.** Commit and push only when the owner
  asks. No attribution trailers, no em dashes.
- The owner's Unity editor (6000.5.8f1) is OPEN on this worktree. Never run batch Unity while
  it is; never kill it. Changes reach it by the editor recompiling (the owner must be out of
  Play mode and click into the window). `Logs/Editor.log` in the worktree is the editor's log.
- ⚠️ The working tree has about 400 modified files that are NOT this work and must never be
  committed: Unity re-saved `Resources/UI/input/xelu/**`, `Resources/Models/RosterArms/*`,
  hero `*-motion/*.anim` (Phaister's and Paete's included), `ProjectSettings/*`, and the files
  the owner listed at the start. Whether the RosterArms and `.anim` rewrites are real changes
  or Unity re-serialising has NOT been checked. Stage explicit paths only.

### 15.2 What exists

Seven redesigned heroes, each a prototype with its own scripts (section 13's table):
`dante`, `cheska`, `sean`, `zack`, `nemu`, `rafi`, `amihan`. Phaister and Paete are protected
and untouched. The twelve Classic characters are not started.

| Hero | Latest | Triangles | Notes specific to it |
|---|---|---|---|
| dante | v37 | about 3,960 | Hair A, quiet clothes (`QUIET_CLOTH`), long bangs, eyes measured off the original at 1.28, no blush |
| cheska | v14 | 5,584 | Eyes at 1.15 with the notch, blush, cords hang straight under the flaps |
| sean | v15 | 5,316 | The ORIGINAL's cut-corner head (not Dante's shape), eyes at 1.14, bracer bands are separate flat pieces |
| zack | v18 | 5,968 | Face is `measured` (v13). The owner REJECTED smug variants A, B, C. Do not offer them again |
| nemu | v11 | 4,688 | Quiet clothes; cowl, fringe and face scale together; eyes NOT enlarged |
| rafi | v14 | 5,992 | 8 triangles spare. Neck gap open (15.5) |
| amihan | v16 | 5,888 | Eyes at 1.15 with the slanted top edge, curved mouth, blush |

Shared tools (do not let a hero's agent edit them): `tools/render_character_redesign_motion.py`,
`tools/sheet_character_redesign_motion.py` (the grid gif). Motion gifs on disk are STALE for
every hero; the owner said "gifs when i ask only".

### 15.3 The rules, as they stand now

Section 13's rules 1 to 12, with these later amendments. Each is the owner's own correction.

1. **Head shape:** Dante's rounded `shaped` rows for everyone except Sean (soft superellipse
   corners, fullest at the cheeks, lower corners rounding in). The jaw tapers gently and the
   closing rings hide in the neck or collar.
2. **Flat face plane:** the FRONT depth of the head is one constant value from under the mouth
   to the hairline. No brow ledge, no cheek standing proud. In the game's shader any step there
   draws a straight line across the face. Blender's imitation hides it.
3. **Head size 0.84:** every vertex on the `head` bone is scaled 0.84 about the head joint AFTER
   UVs are resolved (`HEAD_SCALE`, `HEAD_JOINT`). This departs from the cast's 24/23/53.
4. **Cute face:** no nose, no sockets, creases, lids, lip shadow or contour. Eyes and mouth are
   the original's slot 8 outlines MEASURED off `team-<id>.glb`, eyes scaled 1.14 to 1.28, mouth
   drawn as one smooth curved stroke. Blush on Cheska, Nemu and Amihan only.
5. **Quiet clothes** on Dante and Nemu (no stitches, dust or patches; soft marks at 0.40, folds
   at 0.32). NOT yet applied to the other five; the owner has not asked.
6. **Arms:** elbows fold forward and outward; jump and fall throw the arms up in a V through
   the elbow, as high as clears the head; `jump` is a standing two-footed jump.
7. **Idle is acted**, 8 s, stances that belong to that hero (section 13 table has each).
8. **Paint smears on angled faces** (found on Sean's fist in Unity). Fixed on Sean's arms only:
   a face takes a drawing only when it squarely faces a view; chamfers take a flat tone. The
   same projection is still used on every hero's torso, legs, shoes and head. I offered to
   apply the flat-tone rule cast-wide; THE OWNER HAS NOT ANSWERED.
9. **My own pass after every agent** (owner: "i need you to do your own passes when the agent
   is done"). Render close-ups myself and compare across the cast before showing anything.
   Saved to memory as `own-pass-after-agents`.

### 15.4 What is in Unity

- `Editor/RosterBookBuilder.cs` `PersonModels`: the seven heroes point at the redesign `.glb`
  files (a marked block; reverting those rows and rebuilding the roster book undoes it). The
  roster book was rebuilt once in batch mode before the editor was opened.
- `Runtime/Visual/CharacterVisual.cs` `HandBones`: forearm bones tried first, so a carried
  tsinelas should follow the bent hand. NOT verified in play.
- `Shaders/Toon.shader`: a global `_CharacterSmoothShade` (0 is the shipped cel look) and a
  wrapped-Lambert term in `LightingToon`.
- `Runtime/Visual/WorldOutline.cs`: `CharacterAoRadiusTest` beside `CharacterAoTest`.
- `Runtime/UI/TumpMatchReadout.cs`: the F8 character AO switch is REMOVED at the owner's word.
- `Editor/CharacterRedesignLineup.cs` (new, editor only):
  - **Build Lineup On Ilalim (temp scene)**: copies the map to
    `Scenes/Temp/IlalimNgTulay.unity` (same scene NAME so the map's look is found; not in the
    build), puts the seven redesigns in a row 6 m behind the court with each original behind
    it. A file `Temp/character-redesign-lineup.request` makes the open editor rebuild it once.
  - Play in that scene starts the game's **Training Range** (`GameLaunch.TrainingRange`), so the
    owner has a player to move with and no match. Whether it shows an opening camera move or
    unwanted HUD has NOT been seen.
  - **Ctrl+Shift+J** cel or smooth shading (smooth is the default, with character AO on).
  - **Ctrl+Shift+K** cycles the character AO size; the owner chose **0.14 m**.
  - The head-size toggle is gone (heads are built small now).
- `Resources/SwimmingAnimations/*-redesign.asset` and `RecoveryAnimations/*-redesign.asset`
  were generated by `AuthoredAnimationBuildCheck.RepairMissing` so a build passes. Not reviewed.
- `Builds/character-redesign-20261005/TumbangPreso.exe` exists but is STALE (old heads, old
  faces). Rebuild only when the owner asks and the editor is closed.
- All of this is editor-test wiring. In a built game the cel look and zero character AO still
  ship. Making smooth shading and AO 0.14 real is a separate decision (it would reach Phaister
  and Paete too).

### 15.5 Open, in the order I would take it

1. **The elbow pass (the owner's next ask).** Systems that still target `arm-right` /
   `arm-left` and do not know the elbow: `HeroAbilityClips*` (about 170 references),
   `CharacterAnimator.LocomotionArms`, `VoxelDresser`, `PhaisterHandProps`, `PaeteVfx`,
   `AbilityVfx`, `WindTumble`, `SwimmingMotion`, `DanceClip`, `CharacterAnimator.EdgeRecovery`
   (measures the palm by bone index). Decide per system whether a hand attachment moves to the
   forearm or the clip needs an elbow key. First check a carried tsinelas in the Training Range.
2. **Unanswered questions to the owner:** apply the flat-tone-on-angled-faces fix cast-wide?
   Rafi's neck gap (add a neck block and trim a hair chunk, or raise the 6,000 limit)? Quiet
   clothes for the other five? Round Sean's head like the rest? Exaggerate Amihan's eye slant
   (it matches the original but reads faint)? Make smooth shading and AO 0.14 the real look?
3. **Not checked on any hero:** `slide`, `sit`, hero ability clips, emotes, first-person arms,
   walk and sprint frames at the new head size, anything measured for penetration.
4. **Seen in the owner's first Unity screenshot of Dante, not yet addressed:** hard-edged dark
   patches at the shaved temple, colours far more saturated than the Blender renders, the
   collar's inner face showing as a pale strip beside the jaw.
5. Cheska v14's cord fix is the agent's word only; I have not opened its sheet.

### 15.6 How to work (lessons of this session)

- Blender's imitation of the shader hides real faults. Trust the owner's Unity screenshots over
  any Blender render, and say "checked in Blender only" when that is true.
- One agent a hero (they keep their context; continue them by id rather than spawning new
  ones), same instruction to all, then my own pass. Agents drift apart unless measured against
  the original and against Dante.
- Windows sometimes refuses a file write with `OSError: [Errno 22]` while the editor or another
  process holds the file; retry after a second. Bash heredocs with apostrophes break; write
  edit scripts to the scratchpad and run them.
- Show the owner pictures (faces beside the original), short and often. He corrects fast.

### 15.7 Third session, 2026-10-05 (elbow pass begun; all uncommitted, none seen running)

- **The game draws walk and run itself** (`CharacterAnimator.LocomotionArms`, from `GaitStyles`),
  so the redesign `walk` and `sprint` clips only ever reached the forearms. And the redesign
  glbs are named `<id>-redesign`, which `GaitStyles.For` did not know, so all seven walked the
  `Custom` gait. Fixed: `-redesign` maps to the hero's own style.
- Owner chose: the game's gait bends the elbows per hero. `Gait.Elbow` and `Gait.ElbowPump`
  (degrees, per hero, walk and run), `GaitPose.ElbowLeft/Right`, `PoseElbow` in
  `LocomotionArms` (folds ahead and 30 degrees outward; the carrying arm stays straight).
  Rigs without forearm bones ignore all of it. NUMBERS ARE FIRST GUESSES, unseen.
- `CharacterVisual.HandBone(skin, "left"|"right", out bone, out palm)`: the bone the hand is on
  (forearm first). Used by the ledge grab (`EdgeRecovery`, which also holds the forearm straight)
  and `ZackCircuitTell`. Ability clips were left alone: a clip that keys only the upper arm
  plays with a straight forearm.
- Owner's answers: clothes are fine as they are (no quiet pass on the other five); Sean's head
  stays; Amihan's eye slant stays; Rafi's neck is fine; no cast-wide flat-tone pass.
- **Smooth shading and AO 0.14 m are now the GAME's look (owner: yes).** `ToonSkin` sets the
  global `_CharacterSmoothShade` to 1 before the first scene; `WorldLookProfile` defaults are
  `CharacterAmbientOcclusion` 1 and radius 0.14. It reaches everything on the Toon shader (the
  cast, first-person arms, summoned things), not the map. NOT seen in a normal match or a
  build; tests that assert the two-band ramp (`ToonLightFalloffTests`, `WorldCourtCueTests`)
  have not been run and may need updating. This supersedes 15.4's "the cel look still ships".
- Owner: "should probably work on a paete and phaister rework". Prototypes started, one agent
  each, same file layout (`<id>` = `phaister`, `paete`); the real files stay untouched.
- **OWNER DECISION, 2026-10-05: "we'll be using the character redesigns from now on instead of
  the older models".** The redesigns are no longer a side test. All nine heroes' roster rows
  point at `CharacterRedesign/<id>/<id>-redesign.glb`; `Character Redesign/Use Redesigns In
  Roster` (editor menu, also run once by the trigger file when it contains `roster`) rebuilds
  the roster book and authors missing swim and recovery sets. The old `team-<id>.glb` files
  are still on disk and untouched. Still nothing committed.
- Paete is at v07 (owner's exceptions: head at FULL size, real brow blocks, eyes at the
  original's size, the plank between the eyes stays). Phaister is at v06 (face approved at
  v04; hat at the ORIGINAL's size, not scaled with the head; stacked curls). Her display name
  is Soraya. `PhaisterHandProps`, `PhaisterManika`, `VoodooSkyCircle`, `VoodooSoulDraw` use
  `HandBone`; `PaeteVfx` takes the braid tip off the forearm bone (`TipReach`, 0.26).
  `BodyScaleFor` knows `paete-redesign` (1.3). Swim and recovery authoring still measure on
  the upper arm.
- Rafi's neck gap is shown in `Logs/character-redesign-rafi/v14_neck_gap.png`.

### 15.8 Fourth session, 2026-10-06 (first-person arms; work now lives on QoLUpdates)

- **Where:** branch `QoLUpdates`, worktree `.claude/worktrees/kanto-blender-assembly-909442`, shared with
  the arena map session. Touch character files only. That session runs Unity on this folder too: check for a
  running `Unity.exe` before any batch run, and never run one while the owner's editor is open.
- **First-person arms are cut from the live redesign models** (owner: "like how minecraft does it", "take it
  directly from the live model", "the squeeze looks bad"). `ViewmodelArmAuthor.Extract` follows the elbow
  (arm plus forearm) and sizes a redesigned arm BY ITS FIST (`NaturalFist`, Dante's), far end at `ArmLength`.
  `ViewmodelArms.NaturalArmHeroes` (all nine) get the uncut proportions and the owner's chosen rest placement
  (`NaturalRightPosition`..., tile T2 of `Logs/character-redesign-dante/fpvcut04_placement_tilt_variants.png`).
  Paete has no fist: length rule, then `PaeteNaturalBulk` and `PaeteNaturalLength`. The fist-size recut is
  WRITTEN BUT NOT BAKED: run `Tumbang Preso > Build Roster Book` or `ViewmodelArmAuthor.Run`.
- A leftover material slot drew every cut arm white (`ApplyRosterArm` now fills every slot). The cast's AO fades
  out under 0.8 m so the player's own hands do not stripe.
- `Editor/FpvNaturalArmProbe.cs` renders the arms through the game's own camera setup to
  `Logs/shots-fpv-natural/`; `tools/sheet_fpv_natural_arms.py <tag>` joins them. `FppArmsSnapshotTool` uses a
  different lens and cannot judge placement. `WalkArmsProbe` records nothing at present (its player never moves).
- Parked: purpose-built arm model `dante-redesign-fpv-arms.glb` and its scripts (owner chose the live model).
- `Editor/CharacterPrototypeMap.cs`: the test scene as a plain grid map (`Scenes/Temp/Eskinita.unity`, a copy
  of Eskinita with the street removed), heroes in a row, H while looking at one switches to them. NOT yet seen.
- Paete's walk: `Elbow` 24/12 walk, 42/16 run after "looks like he's A-posing". Not seen.
- **Owner's list of reworks still to do:** (1) first-person hand animation (jump, fall, walk reactions; unique
  per-hero touches such as Paete's vines moving), (2) the Classic non-hero cast, (3) skill animation and VFX to
  match the cartoony, lively style. Open on the arms: Nemu shows only sleeves; carry, throw and cast poses are
  unchecked from the new placement.

### 15.9 CURRENT STATE, end of the 2026-10-06 session (read this first)

**Where the work is.** Branch `QoLUpdates`, worktree `.claude/worktrees/kanto-blender-assembly-909442`. The session
must be INSIDE that worktree to edit it (`EnterWorktree` with its path; a write hook blocks edits from any other
worktree, and scripted edits that dodge the hook are not allowed). The worktree is SHARED with the arena map
session: touch character files only, never `ArtSource/arena`, `Editor/MapKit`, `Runtime/Map`. Everything from this
session is UNCOMMITTED. Commit or push only when the owner asks; stage explicit paths.

**Unity.** The owner usually has the editor open on this worktree, and the arena session runs batch Unity on it
too. Check for a running `Unity.exe` (PowerShell `Get-CimInstance Win32_Process`) before any batch run; never
kill one. With the editor open, changes reach it on recompile and editor tools are run by a trigger file or by the
owner clicking a menu. The owner's editor logs to `%LOCALAPPDATA%/Unity/Editor/Editor.log`, not `Logs/Editor.log`.

**Do not touch until the owner says:** `Runtime/Camera/ViewmodelArms.AirMotion.cs` and
`Runtime/Camera/ViewmodelArms.HandStyle.cs` (owner, 2026-10-06: "don't touch ... for now"; the second has changed
on disk since, so it may hold his own edits). The first-person hand EFFECTS are PAUSED ("pause on the hand vfx").

#### A. First-person arms (done, in the game, owner has played it)

- Arms are cut from the live redesign models, following the elbow, sized BY THE FIST to Dante's
  (`ViewmodelArmAuthor.Extract`, `NaturalFist`); `ViewmodelArms.NaturalArmHeroes` (all nine) wear them uncut at
  the owner's chosen rest placement (tile T2). Owner: "looks good". Paete: his old bulk 1.45 across, own length
  (the 2.0 version was reverted at his word).
- ⚠️ The fist-size recut is WRITTEN BUT MAY NOT BE BAKED: `Tumbang Preso > Build Roster Book` or batch
  `ViewmodelArmAuthor.Run`. Check `Logs/shots-fpv-natural/` with `FpvNaturalArmProbe.Run` after a bake.
- Fixed on the way: a leftover material slot drew cut arms white; the cast's AO now fades out under 0.8 m so the
  player's own hands do not stripe.
- OPEN: Nemu shows only sleeves; carry, throw and cast poses are unchecked from the new placement; the owner
  reported walk and sprint hand sway only on PAETE, cause not found (the hands follow
  `CharacterAnimator.LocomotionArmAmount`; the prototype map shows the walk layer's readout at the top of the
  screen, and a screenshot as Dante and as Paete was asked for and not yet given).

#### B. First-person hand animation (base approved, effects rejected then paused)

- Base: `AirMotion` (take-off dip, thrown up on the rise, higher on a fall, landing slam on a spring), and the
  natural arms sway 2.2 times harder on the walk and run. Owner: "this is good as a base".
- Per-hero fall style and effects: the first pass (flat polygons) was rejected ("these are bland vfx ... it doesnt
  even match the illustrated, cartoony, artistic style we have", "most arms falling animation just swings front
  and back", Nemu's sleeves read as arms swinging). The second pass is written and UNSEEN: circular flails, Nemu's
  billow, a drawn sticker sheet in the ability icons' style (`tools/author_hand_stickers.py`,
  `Resources/Vfx/hand_stickers_v1.png`, review picture `Logs/hand-stickers/hand_stickers_review.png`), and a
  signature gag per hero. Then the owner paused it. Do not resume without his word.
- LESSON, his words: "i need you to think and critique your work more and give me something more unique and fun".
  Look at the game's own art (the ability icons under `Resources/UI/ability-icons`) BEFORE drawing anything, and
  look at your own output before showing it.

#### C. The prototype test scene

`Editor/CharacterPrototypeMap.cs` builds `Scenes/Temp/CharacterPrototype.unity` from nothing (owner: "just create a
separate scene thats outside the map pool"): grid floor and walls, blocks, two jump pads, the nine heroes in a row,
the game's launcher. `Runtime/Diagnostics/PrototypeMapPlay.cs` (editor only) asks for the Training Range before
the launcher wakes, hides the can, turns the range's cheats on, and switches the player to the hero he is looking
at on H. `WorldLookProfile.Find` maps the scene name to Ilalim's look. Trigger file
`Temp/character-prototype-map.request`, or the Character Redesign menu. NOT confirmed working by the owner since
the rewrite; his last word on the older version was "gameplay still happening".

#### D. Classic cast rework (IN PROGRESS, the current task)

- Twelve Classic characters (`RosterBookBuilder.PersonModels` rows `bayan` to `aling_nena`), shared rigs recoloured
  by a palette per person (`Resources/Roster/person_<id>.asset`). Three go first as the pattern, one agent each:
  `bayan` (character-male-f), `bebang` (character-female-c), `lola_pacing` (character-female-d). Files per
  character as section 13's table; renders under `Logs/character-redesign-<id>/`.
- First results (bayan v04, bebang v05, lola_pacing v04) were faithful to the originals and the owner REJECTED the
  direction: "head size stays the same as the heroes. my issue is they focus on resembling closer to the original
  design instead of being reworked to be more in the heroes' style. the eyes and face design are different from
  the heros, even the hair on bayan being black doesnt translate to the rework".
- ⚠️ SO THE RULE FOR THE CLASSIC CAST: redraw each one AS IF THEY WERE ONE OF THE NINE HEROES. Head 0.84. Eyes are
  the heroes' simple solid ink blocks (a slant or a half-close for attitude), mouth one thin stroke, hair built
  and coloured the way the heroes' is (black is BLACK), clothes DESIGNED to the heroes' richness while staying
  everyday people. "Nothing invented" and "measure the original's eyes" are LIFTED for this cast. The test is a
  row with three heroes: do they belong in it.
- The three agents were sent that brief (bayan v05, bebang v06, lola_pacing v05) and were RUNNING when this was
  written. Decisions I made in those briefs that the owner has not confirmed: Lola Pacing's hair goes SILVER
  (palette slot 11) instead of peach; Bebang gets real footwear; Bayan's peach limbs become bare forearms and
  proper trousers.
- When they return: my own pass (open the row-with-heroes sheet and the face sheet for each, compare the three
  with each other), show the owner, and only after he approves the pattern start the other nine
  (maring, totoy, inday, kuya_boy, ate_girlie, tikboy, jun_jun, mang_kanor, aling_nena), same brief.
- Not done for any Classic redesign: roster rows, `GaitStyles` names (`<id>-redesign` maps to `team-<id>`, which
  Classic ids do not use), swim and recovery sets, first-person arms.

#### E. The owner's list of reworks still to do

1. First-person hand animation and per-hero touches (B, paused).
2. The Classic cast (D, in progress).
3. Skill animation and VFX for all nine kits: "heavily unrefined", must match the "cartoony and poppy/lively
   stylized artstyle". Not started. Plan: one hero's kit as the style reference, approved, then hero by hero.

#### F. How to work (this session's lessons)

- Sub-agents from before a restart cannot be resumed by id; `ListAgents` shows what exists. If one is gone, start a
  fresh one with a brief that names the files already on disk.
- My own pass after every agent, and after my own work: I shipped effects I had never looked at, and it showed.
- When the owner gives a short look remark, do the smaller change or ask (memory: short-feedback-add-not-replace).
- Windows: bash heredocs with apostrophes or backticks break; write helper scripts with the Write tool into the
  scratchpad and run them. Compound shell commands are refused inside the worktree session: one plain command each.

#### G. Update, later on 2026-10-06: the Classic pattern is APPROVED and the other nine are building

- The three pattern characters came back in the heroes' style and the owner said "everything looks good":
  bayan v07 (`Logs/character-redesign-bayan/v07_row_with_heroes.png`), bebang v08 (`.../bebang/v08_row.png`),
  lola_pacing v07 (`.../lola_pacing/v07_row.png`). They stand as they are: Lola Pacing's silver hair, the invented
  touches (her fan, comb and slippers; Bebang's plaster, sweatband and sneakers; Bayan's towel and wristband) and
  each one's body are approved with that. Bebang is on the heroes' body proportions; Bayan and Lola Pacing kept
  the wider Classic body. My own notes, NOT asked for by the owner: Lola's hair reads lavender and her taupe is
  dull; Bebang's bun reads like a parcel; Bayan's and Bebang's skin sit outside the heroes' range.
- The brief every Classic agent works from is `docs/reports/character-redesign/classic-brief.md`.
- Nine agents, one each, were started with it: maring, totoy, inday, kuya_boy, ate_girlie, tikboy, jun_jun,
  mang_kanor, aling_nena. As each returns: my own pass (its row sheet and face sheet), compare across the cast
  for copies of one another, then show the owner. Twelve characters drawn by twelve agents WILL drift and repeat
  (the same eye cut, the same grin); catching that is my job.
- After all twelve are approved: roster rows in `RosterBookBuilder.PersonModels`, gait names for
  `<id>-redesign` in `GaitStyles.For` (Classic ids are not `team-<id>`), swim and recovery sets, first-person
  arms (`NaturalArmHeroes` is heroes only), then a look in Unity.

### 15.9 Fifth session, 2026-10-06 (hand companions; this session took over the first-person hands)

The owner handed the first-person hand effects to the Arena session ("apply the same mindset you did when you
transformed the plain drone to a cutesy robot") after rejecting two rounds of flat effects from 15.8's session:
polygons, then drawn stickers on quads ("i don't like the sticker effects"). **Stickers are deleted** (the sheet, its
tool, `EmitAirPops`, the gag table). The air spring and the per-hero flail (`AirMotion`, `HandStyle`) are kept.

- **The idea: each hero CARRIES a companion.** A small modelled thing in the toon dress with an ink line, alive in
  every state (standing with idle acts, walking, sprinting, take-off, falling, landing, a slipper, the wind-up, the
  throw, tagged, a cast), everything on springs. `ViewmodelArms.HandLife.cs` holds `HandCompanion`, `HandMood`,
  `Spring` and `CompanionFor`; it is stepped last in `StepVisuals`, lives under the arms' root, and only the player
  whose hands they are sees it.
- **One class and one model a hero** (`Runtime/Camera/<X>Hand.cs`, `Resources/Models/HandCompanions/<hero>.glb` built
  by `tools/build_hand_<hero>.py` with `tools/hand_companion_kit.py`; `HandCompanionProp` loads and dresses them):
  nemu `NemuKuroHand` (Kuro himself in her left sleeve), sean `SeanEmberHand` (a flame creature in a brazier cup),
  dante `DanteStoneHand` (a mother stone and two chicks), zack `ZackSparkHand` (a battery-bean, Kislap), cheska
  `CheskaFrostHand` (a snow bunny in crystals), rafi `RafiTideHand` (a fish in water bangles), amihan
  `AmihanPinwheelHand` (a maya bird on a paper pinwheel), phaister `SorayaMothHand` (a moth in a top hat), paete
  `PaeteSproutHand` (tendril fingers, wrist leaves, a sampaguita bud with a face).
- **Seen where:** ONLY Nemu's has been seen running (the owner: "looks cute"). The other eight were built by one
  agent each while the owner was away with his editor left in Play mode: they compile (checked outside Unity) and
  their MODELS were reviewed in Blender (`Logs/hand-companions/<hero>_vN.png`), but NONE has been seen in the game.
  Every agent guessed its placement on the arm from mesh measurements, and several guessed turn signs: expect things
  buried, floating or facing away on first look. Tune placement first.
- **Kuro was remodelled ("Kubo") for BOTH Kuros** (owner picked blockout B of `tools/review_kuro_cute_options.py`:
  "this. both kuros"): `tools/kubo_kuro.py` replaces only the geometry of the calm form's seventeen parts in
  `pet-nemu-ghost.glb` (rounded block, ear tufts in the old top-rim part, paws in the old bottom-bevel part, big oval
  eyes, cat mouth, a tapering tail, lavender inner ears, hem band and pale pads). No node is added, moved or renamed;
  faces, rage form, rig and clips are untouched. The original is `ArtSource/kuro/pet-nemu-ghost.pre-kubo.glb`;
  `--restore` puts it back. `KuroFormTests` have NOT been run against it.
- **His tail fades out** in halftone dots (`Resources/Shaders/KuroTail.shader`, a NEW shader; vertex alpha baked by
  the tool; `GhostPetCompanion.DressTail`). Not seen running.
- **One Kuro for her own view** (owner: "make it so that the fpv only sees the hand kuro ... when casting her E
  ability to place kuro, it looks like it moves from her hand for fpv only"): `GhostPetCompanion.StepOwnerView`
  hides the world's Kuro from the first-person Nemu player while he is at her side and draws him leaving from, and
  returning to, the sleeve (`NemuKuroHand.For`, `MarkAway`). Only what is drawn moves (the `CalmForm` child); his
  real transform and the network are untouched. Not seen running. The sleeve Kuro copies the live one's materials
  (`MatchTheLiveKuro`) because their eyes differed (purple against black); the cause was not found.
- **Seeing it without closing the editor:** `Editor/FpvHandLifeProbe.cs` plays one script of every state through the
  game's first-person framing in a preview scene, in the OPEN editor: menu `Tumbang Preso > First Person > Render
  Hand Life (all heroes)`, or lines of `hero tag` in `Temp/fpv-hand-life.request` (picked up when not in Play).
  Frames go to `Logs/shots-fpv-hands/<hero>_<tag>/`; `py -3 tools/sheet_fpv_hand_life.py <hero>_<tag>` makes a sheet
  and a gif.
- Another session was writing Classic-cast redesign files (`lola_pacing`, `bayan`, `bebang`) into this worktree the
  same evening. Not this work.
- Nothing of this session is committed.

#### H. Update, 2026-10-06 late: all twelve Classic redesigns are APPROVED and wired, not yet rebuilt

- All twelve are built and the owner said of them together "they all look good"
  (`Logs/character-redesign-classic/v01_lineup.png`, made by `tools/render_character_redesign_classic_lineup.py`
  and `tools/sheet_character_redesign_classic_lineup.py`). Latest versions: bayan v07, bebang v08, lola_pacing v07,
  inday v04, jun_jun v04, aling_nena v05, totoy v04, ate_girlie v04, tikboy v05, maring v05, mang_kanor v06,
  kuya_boy v05. They stand AS THEY ARE: I had proposed a cross-cast fix pass (three green shirts among the men,
  the same three-slab back of the head on seven, similar fringes on four girls, uneven colour strength,
  half-closed eyes on four, Kuya Boy short and plain from behind) and he did not ask for it. Do not run it
  unprompted; raise it again only if he sees the same things in the game.
- WIRED, uncommitted, NOT yet run in Unity: `RosterBookBuilder.PersonModels` points the twelve at
  `CharacterRedesign/<id>/<id>-redesign.glb`; `GaitStyles.For` maps `<id>-redesign` to each person's own gait; every
  Classic gait has first-guess `Elbow` and `ElbowPump` values.
- STILL TO DO for the Classic cast: run `Tumbang Preso > Character Redesign > Use Redesigns In Roster` (rebuilds
  the roster book, bakes arms, authors missing swim and recovery sets), then look at them in the game. Their
  FIRST-PERSON ARMS are still the old block arms: `ViewmodelArms.NaturalArmHeroes` lists the nine heroes only, and
  `ViewmodelArms.cs` sits in the folder another session is working in (the hand effects: `*Hand.cs`,
  `ViewmodelArms.HandLife.cs`), so that one line was left for the owner's say-so.
- The hand-effects work moved on in another session after the owner paused it here. Its rules are in memory
  (`vfx-shape-and-character-not-particles`, `hand-pets-only-nemu`). Leave `Runtime/Camera` to it.

#### I. WHERE THINGS STAND AND WHAT IS NEXT (written 2026-10-07; read this part first)

**One line:** all 21 characters (nine heroes, twelve Classic) are redesigned and approved as art. The heroes are in
the game with live-model first-person arms. The Classic twelve are wired into the roster in code but the roster has
NOT been rebuilt, so the game still shows their old models until the owner runs the menu.

**Uncommitted in the worktree, mine (character work):**
- `Editor/RosterBookBuilder.cs` (Classic rows), `Runtime/Visual/GaitStyles.cs` (Classic names and elbows, Paete's
  elbows), `Editor/ViewmodelArmAuthor.cs`, `Runtime/Camera/ViewmodelArms.cs` and `ViewmodelArms.RunSway.cs` (natural
  arms; ⚠️ another session has since edited files in `Runtime/Camera`, so read before touching and prefer not to),
  `Shaders/WorldOutline.shader` (AO fades out near the lens), `Runtime/Visual/WorldLookProfile.cs` (the prototype
  scene's look), `Editor/CharacterPrototypeMap.cs`, `Editor/FpvNaturalArmProbe.cs`,
  `Runtime/Diagnostics/PrototypeMapPlay.cs`, `Editor/CharacterRedesignLineup.cs`.
- Twelve Classic redesigns: `tools/*character_redesign_<id>*.py`, `Art/CharacterRedesign/<id>/`, `ArtSource/<id>/`,
  plus `tools/render_character_redesign_classic_lineup.py`, `tools/sheet_character_redesign_classic_lineup.py`,
  `tools/author_hand_stickers.py`, `tools/sheet_fpv_natural_arms.py`, `docs/reports/character-redesign/classic-brief.md`.
- Parked and unused: `dante-redesign-fpv-arms.glb` with its scripts; `Resources/Vfx/hand_stickers_v1.png`.
- NOT mine, leave alone: everything arena (`ArtSource/arena`, `Editor/MapKit`, `Runtime/Map`), and the hand-effects
  session's files in `Runtime/Camera` (`*Hand.cs`, `HandCompanionProp.cs`, `ViewmodelArms.HandLife.cs`,
  `ViewmodelArms.AirMotion.cs`, `ViewmodelArms.HandStyle.cs`).

**Next, in order:**
1. THE OWNER RUNS `Tumbang Preso > Character Redesign > Use Redesigns In Roster` (editor, outside Play mode), or, if
   no Unity is running on the worktree, batch `RosterBookBuilder.Build` then `AuthoredAnimationBuildCheck.RepairMissing`
   through `tools/run_unity_guarded.py`. Then the Classic twelve are in the game.
2. Look at the Classic twelve in the game with him (a Classic match). Expect: stronger colour than Blender, 8 s
   idles, forearms stretched to reach faces, old clips sitting differently on the new arms.
3. Classic first-person arms: add the twelve ids to `ViewmodelArms.NaturalArmHeroes`. HELD for the owner's word
   because `ViewmodelArms.cs` is in the hand-effects session's folder.
4. Unanswered questions to him: may natural hair colours (ginger, copper, peach) be exempt from the "keep off the
   offence orange" rule (it dulled Jun-Jun, Ate Girlie and pushed Lola Pacing to silver)? Does Tikboy keep losing
   his moustache? Is Lola Pacing's silver hair right?
5. Still unknown: why only Paete showed walk and sprint hand sway (the prototype scene prints the walk layer's
   readout at the top of the screen; a screenshot as Dante and as Paete was asked for and never given). Whether the
   rebuilt prototype scene finally comes up without a match.
6. The owner's remaining reworks: skill animation and VFX for all nine kits (not started here; the hand effects
   went to another session). Read memory `vfx-shape-and-character-not-particles` before proposing any effect.

**Not checked on any redesign, hero or Classic:** `slide`, `sit`, emotes, the hero ability clips on the new elbows,
swim and recovery on the forearm, tests that assert the old models (`RosterArmAuditTests`, `ToonLightFalloffTests`,
`WorldCourtCueTests` may need updating). No commit has been made of any of it.

### 15.10 Sixth stretch, 2026-10-06 to 10-07 (bare hands, hand rendering, Paete's ability rework begun)

Read 15.9 first. Everything here is UNCOMMITTED on `QoLUpdates`, like 15.8 and 15.9. The owner plays in his open
editor all session: keep C# compiling at every save (compile outside Unity, read the result, only then move on).

**The owner turned the hand companions round.** Of all nine seen in play: *"i like it but we were trying to reserve
the pet idea only for nemu... so i need something for the bare hands"*, *"we might keep this as an unlockable
though"*. So:
- Kuro stays on for Nemu. The other eight pets (15.9) are KEPT behind `ViewmodelArms.UnlockedHandPets` (false; nothing
  sets it yet). Do not delete them.
- Every other hero has a BARE-HANDS class instead (no creature, no face: their own arms, clothes and element), chosen
  in `CompanionFor`: paete `PaeteVineHands`, sean `SeanFlameHands`, dante `DanteGauntletHands`, cheska
  `CheskaRimeHands`, rafi `RafiWaterHands`, amihan `AmihanWindHands`, phaister `SorayaStageHands`, zack
  `ZackMagnetHands` (models `HandCompanions/<hero>_hands.glb`, `zack_magnet.glb`; build scripts
  `tools/build_hands_*.py`). `ZackArcHands` (lit bands and lightning) is the version he refused (*"overall i dont know
  if i like zack's hand design"*); he then chose magnet hands from a list and asked for his yellows.
- All eight were built by one agent each and rendered through `FpvHandLifeProbe`; they sit on the arms and change by
  state. Only Paete and Zack have had the owner's own notes. None has had its two arms de-synced except Paete and Zack.

**What he said about Paete's hands, in order** (each is a rule for the others too):
1. *"its just attached to his arm, its not actually the vine from his arm extending"*: his arm's own mesh must move.
   `PaeteVineHands` deforms a private copy of the arm mesh (its four braid strands are separate connected pieces).
2. *"kinda gross ... flailing around like tentacles.. the vines dont stretch"*: no constant writhing.
3. *"this whole iteration looks bland, they can move separately but make it so they grow in the direction they look
   like theyre growing into"*: each strand on its own clock, GROWING along its own line.
4. *"the vines on the two arms always move in sync, when throwing, both arms extend"*: the arms are not a pair; a
   throw is the right arm's only.
Also: *"you can use 2d textures and effects, i just dont like the stickers"* (Zack's sparks are drawn lines).

**Shared hand fixes (all in `Runtime/Camera/`, all compile, none committed):**
- `ViewmodelArms.HandLife.cs` `CarryWithTheStride`: what rides a hand never sees the walk's swing or the jump/fall
  motion (they are taken off the pivots before the companion steps and put back), and every piece is then moved
  rigidly by what that motion did to its arm. Owner: *"hand additions clip through the hands because of the walking
  animation"*, then *"also fix the clipping when jumping and falling"*. Checked in probe renders.
- `ViewmodelArms.WorldShade.cs`: the hands take no cast shadows (they sat inside the player's own hidden body and
  flickered with its shadow) and are shaded as one by a ray from the eye to the sun past all characters. NOT seen in a
  real map.
- `ViewmodelArms.Framing.cs` `DepthPull` .42: the view model is shrunk toward the eye while drawn so it never sinks
  into a wall (owner: *"youre supposed to render separately and then overlay the hands"*). NOT confirmed by him. If it
  still clips or the bottom of the arms is cut, do a real second camera. `DrawnFromWorld` maps a world point into the
  drawn view model.
- `FpvHandLifeProbe` now plays the walk sway and a named cast (`HandMood.Action`).
- Paete's standing arms: `GaitStyle.IdleArmSpread/IdleArmForward/IdleElbow` (Paete 11, 6, 20), posed in
  `CharacterAnimator.LocomotionArms` (`PoseIdleArms`). Owner: *"paete's idle animation just makes him look like he's
  a-posing all the time.. bring his arms down"*. NOT confirmed by him.

**The ability rework (owner: "do a full rework next on the design and animation for abilities; do paete's abilities
first"; animation and effects "both equally"; "concept should stay the same but ... rework all abilities to look more
poppy, lively and fit our current style", Makiling included; sounds wanted).**
- Groundwork, done: ability clips can key the forearms. `PoseKey(..., leftFore, rightFore)` with `Fore(foldDegrees,
  stretch)` (`HeroAbilityClips.cs`, THE ELBOWS; `.Introductions.cs`). No table but Paete's vine uses it.
- `Editor/MapKit/PaeteAbilityFilm.cs`: body filmstrips and video frames in the OPEN editor from the tables
  (`Temp/paete-ability-film.request`: `vine|sprout|command|thorns|sentry|all <tag>`, or `bake <tag>`). ⚠️ THE GAME PLAYS
  THE BAKED `.anim` FILES: a table change shows in Play only after `bake` (it rewrites the clips in place).
  `tools/video_paete_ability.py` joins frames; Blender muxes sound (`Logs/paete-sfx-drafts/video`, no ffmpeg here).
- LIANA LEAP, in progress, the owner iterating on it live:
  - Body clip v2 (coil with elbows, right-then-left whip, elbows folding in the reel, squash landing): baked. He has
    not commented on the body itself.
  - First person: the strands curl in for the wind-up and do NOT themselves grow (he asked the spikes removed); new
    round limbs are built from the strand tips to the catch (`BuildReach`, the arm's material, the strand's paint
    repeated). The first try stretched the strand's own points and he said *"textures get really distorted and the
    mesh gets really weird"*. Of the limbs: *"it looks and sounds good"*, then *"fix the transition from original arm
    to arm extension"* (a sleeve up the strand's own line was just added, UNSEEN).
  - World: `PaeteVineReach` aims his body's arms at the catch (no bone scaling: that smeared too) and still draws
    its strands from the braid tips for other players; none for the caster's own view. UNSEEN from a second player.
  - The catch: a knot of three inked vine loops that cinch, five whipping ends, a sampaguita that bursts open, twice
    the leaves (*"i wish the vine cluster at the end had a bit more pzazz"*; the extra was just added, UNSEEN).
  - Sound: real CC0 recordings (58 from Freesound, `tools/paete_sfx_sources.json`, `ArtSource/paete/sfx-sources/`),
    drafts by `tools/build_paete_skill_sfx.py` in `Logs/paete-sfx-drafts/`. He heard a to f over the animation and
    chose E. `tools/install_paete_skill_sfx.py` put it in the game: `sfx_cast_paete_vine`, `sfx_paete_vine_catch`,
    `sfx_paete_vine_land` (in `AudioCues.ReworkedSkillSfx`; every other skill sound is still off). *"it looks and
    sounds good"*.
  - THE SWING, BUILT 2026-10-07, COMPILES, NOT PLAYED BY ANYONE. His ask: *"can you make the leap swing better and also
    fix the aiming so i can swing from higher? look at where im aiming vs where i actually leap from"*. His answers:
    catch surfaces with a sky aim SNAPPING UP to the nearest edge (*"if i decide to do surfaces + open air, i'll let
    you know"*), anything in range (`PaeteRules.VineRange` is still 8 m), swing through and fling.
    - `Abilities/PaeteSwing.cs`: the path, pure numbers. Three swings: OVER (the catch is a lip he can stand on: up
      outside it and over), UNDER (open space under and past it: flung out the far side), WALL (high on a bare face
      or a post: up to it and off). Anything low, a floor, or the ground is REEL, the old flat pull, unchanged.
      `tools/plot_paete_swing.py` is the same lines in Python (`Logs/paete-swing/swing_paths.png`); keep them equal.
    - `Abilities/PaeteHazards.cs` `PaeteVine.FindCatch`: the catch and its kind (`SnapUp`, `SwingFor`). `FindAnchor`
      now calls it. The Arena's `MapCatch` still wins first.
    - `CharacterMotor.Swing.cs` `BeginSwing`: the body's OWNER walks the path and is thrown at its end; nothing about
      the motion is sent. Ends like a haul does, and when something holds him 1.6 m behind the path.
    - Network: PROTOCOL 154. `PaeteVinePhase` gained `SwingOver/SwingWall/SwingUnder`; on them a client caster begins
      its own swing in `PaeteHeroKit.ReceiveVine` and the host sends no `Carry`.
    - Not done: the body clip and the first-person arms are still the reel's; the landing sound plays when the vines
      let go, not when he lands; bots never aim up, so they never swing. Untested on a real map and over the network.
- Not started: Bakya Bloom, Thorn Harvest (its body is out of time with the real pull: fix), Makiling's Embrace, and
  the eight other heroes' kits. The plan agreed for Paete is in the session's transcript; in short, each ability gets
  a readable wind-up, a snap, overshoot and settle, modelled pieces in place of cubes, and sounds picked by his ear.

**Rules of this stretch worth keeping:** show him pictures or a video the moment a piece exists, not when the set is
done; for sounds, a video of the move with each option; say plainly what has not been seen; a compile error was live
in his editor for a minute once (a second local named like an outer one): check before every save.

#### J. Update, 2026-10-07: Classic action clips, elbows in the throw and tag, the roster rebuild, a movement prototype

Everything here is UNCOMMITTED and NOT RUN BY ME IN UNITY (the owner's editor was open on the worktree all session;
its live log is `Logs/Editor.log` in the project, not the `%LOCALAPPDATA%` one). A script edit recompiles his editor,
and if he is in Play mode the session breaks with null references across the game: tell him before a C# edit.

**1. Classic action clips (owner said "yes" to the direction on three pattern characters; all twelve are built).**
- Each Classic `tools/author_character_redesign_<id>_clips.py` now writes THIRTEEN more clips into the glb, in that
  person's own way: `holding-right`, `holding-right-shoot`, `pick-up`, `attack-melee-right`, `attack-melee-left`,
  `interact-right`, `interact-left`, `slide`, `crouch`, `sit`, `die`, `emote-yes`, `emote-no`. The brief, with its
  correction and its lessons, is `docs/reports/character-redesign/classic-clips-brief.md`.
- Eight keep the old clip's exact length and key beat (the game times against them). Five are longer because nothing
  times against them: the carry (about 2 s loop), `crouch` (the fatigue pose, 1.5 to 1.8 s, first and last frame the
  full bent pose), `sit` (0.8 s), yes and no (1.0 to 1.4 s loops).
- Tools: `tools/render_character_redesign_classic_clips.py` (`-- <id> [clips] [--at ...] [--back]`),
  `tools/sheet_character_redesign_classic_clips.py`, `tools/sheet_character_redesign_classic_keys.py` (one large still
  per clip; reads `<id>/keys.json` for each clip's peak), `tools/sheet_character_redesign_classic_cast.py` (the whole
  cast, a row each: `Logs/character-redesign-classic/clips/cast_keys_1.png` and `_2.png`). Stills live under
  `Logs/character-redesign-classic/clips/<id>/`.
- My cross-cast pass sent three back (Inday and Jun-Jun for a copied no and a copied pick up, Ate Girlie for pick up,
  knock-down and sit). KNOWN AND LEFT: throw, tag and the two reaches look alike across the cast from the front (an
  arm out ahead); Maring and Totoy both pick up tipping on one leg; Aling Nena's and Ate Girlie's carries are close;
  Tikboy's knock-down ends upside down on his cap (the owner was told and has not answered); Inday's no has no arm
  gesture and relies on motion.
- ⚠️ BIGGEST RISK: while a body walks or throws with the slipper the game re-poses the UPPER arm and keeps the clip's
  forearm. Several carries now bend the elbow hard (Bayan about 100 degrees). Unseen in the game.

**2. Elbows in the code-posed throw and tag (owner: "all characters will have elbows"), all 21 characters.**
`CharacterAnimator.ThrowBody.cs` (`ThrowShape.Fore` and `OffFore`: where each forearm points in the wind-up, at
contact and in the follow-through, overhand and sidearm) and `CharacterAnimator.TagBody.cs` (the reaching forearm
goes to its bind, the off arm's is bent); `_bind` in `LocomotionArms.cs` now keeps the forearms. First-guess numbers.

**3. The roster rebuild HAS RUN** (in the owner's editor, 2026-10-07 02:47, from the prototype map's trigger file with
"roster" in it): the Classic twelve point at their redesigns and every first-person arm was re-cut. The Classic ids
are on `ViewmodelArms.NaturalArmHeroes` (the owner: "u should also fix the fpv hands btw"; that one line only).

**4. The prototype map** has the Classic twelve as a second row 4 m behind the heroes; H on one wears their body,
clips and arms (skills stay the hero's). All cheats on, no cooldowns included. The owner reported the row "slanted":
it was the very wide game view, and he confirmed "slant is gone".

**5. MOVEMENT REWORK PROTOTYPE, a debug switch** (owner: "we'll put this as a debug setting, default to on only when the
characterprototype scene is being played"). `Runtime/MovementRework.cs` (the switch and every number),
`Runtime/CharacterMotor.MovementRework.cs` (the step), hooks in `CharacterMotor.cs` (the velocity write, the jump
test, sprint refused while crouched), `InputIntent.Crouch` (a debug field, not a Verb), `PlayerInputReader` (Left Ctrl
or C), `CameraRig.ApplyFpp` (the eye drop), `CharacterAnimator.Choose` (the bent clip stands in for crouch and slide),
`PrototypeMapPlay` (on in Awake, off in OnDestroy, F9 flips it, a speed readout). Local human only, offline only.
- Ground acceleration and friction; buffered, coyote and held jump with no friction on the hop; crouch (capsule 1.0,
  headroom check to stand); slide on crouch at a sprint.
- ⚠️ THE OWNER REJECTED THE FIRST AIR MODEL (a Quake strafe): "spinning in a circle just ramps up ur speed too easily,
  airstrafing barely moves your character ... i need to actually be able to move directions when airstrafing", and
  "movement should have a cap". Now: the keys steer the velocity at `AirAccel` 18 with speed held to what it took
  off with, a gain of `AirStrafeGain` 1 m/s a second while steering across the travel, and a cap of `MaxSpeedScale`
  1.5 of the body's run speed. He has not yet said how the second version feels.
- If it ever ships: crouch becomes a real Verb on three devices (the pad has no free control), the host cannot see a
  crouch (capsule and stamina are client-local), a protocol bump, the stamina-against-box sizing, and the tests that
  pin standing-start speed. The map of all of it is in this session's transcript; the short form is in
  `MovementRework.cs`.

**Next:** his word on the action clips in the game and on the throw and tag elbows; his feel notes on the movement
numbers; the open questions in part I (hair colours, Tikboy's moustache, Lola Pacing's silver hair) are still open.

### 15.11 Seventh stretch, 2026-10-07 (the leap's swing, and Paete's kit remodelled, painted and given new effects)

Read 15.10 first. Everything here is UNCOMMITTED on `QoLUpdates`, like 15.8 to 15.10.

**What "rework" means (owner, the same day, after a first pass that only added effects):** *"by 'rework' im thinking of
remodelling, textures, and vfx.. not just vfx"*. Asked whether the plants should have character like the Arena's drone:
*"try the cutesy character for the plants"*. So each ability's plant gets a NEW MODEL, a PAINTED ATLAS and new effects.

**How a plant is now built (the pattern for every later one):**
- `tools/hand_companion_kit.py` builds it (round forms, smooth normals). The kit gained: a node `yaw`, `Model.folder`,
  `Model.texture` (an atlas beside the .glb, as the redesigned cast wear theirs) and `grid_uv` (UVs for a lathe, a ball
  or a tube into a rect of the atlas). `TumbangPreso/Toon` takes the PALETTE for a UV in the atlas's lower half and the
  PAINT for its upper half, so one model mixes flat palette cells (eyes, cheeks) and painted pieces.
- The node NAMES and places are each body's contract (`PaetePlantBody`, `PaeteThornBody`, `PaeteSentryBody`): the new
  model is a drop-in. The first models are kept in `ArtSource/paete/props-pre-rework/`.
- Look at it in Blender first: `tools/review_hand_companion.py` (`--file=`, `--atlas=`, `--ink=`, `--size=`, `--centre=`),
  then `py -3 tools/hand_companion_kit.py sheet <name> <tag>`. Build to a temp folder with `--out=` until he says yes.
- ⚠️ A painted piece takes no palette, so a body that dries by re-dressing must also TINT (`_Color` by property block).
  ⚠️ A piece built in code and hung on a prop (`PaeteInk.Part`) must be skipped by `PaeteProp.Redress` (it painted the
  carved clog black): it skips anything carrying `GrowthMeshOwner`.
- Effects are `Visual.PaeteFx` (`Make<T>`, `Step(dt)`, `Finish()`; never `Time.deltaTime`, never `Destroy`), so the film can
  step them outside Play. `Editor/MapKit/PaeteAbilityFilm.Fx.cs` films what an ability puts in the world: request
  `bloomfx|thornfx|sentryfx <tag>` in `Temp/paete-ability-film.request`; `tools/video_paete_ability.py body <name> <tag>`
  joins the frames. Each film's timeline is its own partial file.

**BAKYA BLOOM, done, the owner: "plant looks good".** `tools/build_paete_bloom.py` (variant d is the one; a to c are the
refused ones, kept in the file with why). His notes in order: the first face's pale patch, *"the mouth piece is weird"*;
of two more, *"idk about the design overall.. u can take inspo from bellsprout or peeshooter"*; of a Peashooter with a
snout, *"oh no.. i mean keep it as a pitcher plant design.."*; *"do you plan on texturing this?"*. So: a real pitcher
(tall jug, narrow neck, flared ribbed lip, lid standing up behind like a hood) with a small plain face on its belly.
It blinks, squeezes its eyes shut to spit, is wide-eyed when loaded (`PoseFace`). The clog grows in its belly and is
raised to the mouth; the clog in its mouth is the carved one that flies (`CarveShoe`). Effects: `Visual/PaeteBloomFx.cs`
(by an agent, checked): the seed, the landing, the rise, loaded, the spit, the carved clog and its trail, its landing
and its knock on the can, the uproot. The plant now turns to its shot. The knock shows on every peer (look only).

**THORN HARVEST, installed, the owner has seen the model but not said yes to it in the game.**
`tools/build_paete_rattan.py`. His notes: of a spiny bud with heavy lids, *"it looks weird.. im thinking of cute
angry"*; of that with spines, fangs and a tuft, *"i feel like its just too much ngl"*; of a version with the canes pared
down, *"no i the vines were okay, its more the bud itself.."*. So: the full clump of canes, fronds and whips, and in the
middle a PLAIN smooth bud with a cute angry face (round shining eyes, slanted brows, red cheeks, a pout). `PoseHeart`
moves it. The rattan now comes up FACING THE CASTER (`PaeteThorns.Spawn`). Effects: `Visual/PaeteThornFx.cs` (by an
agent, checked): the stamp, a runner ahead of the trail, a thorn ring to `ThornRange`, grapnel heads and a bite, the
twang, slack running home, streaks and puffs, the clench's snap, leaves after. Not done: a hop of the slipper on
landing (it is host state). Also fixed: a cast made in the air floated the thorns (*"jumping while placing thorn
harvest makes the thorns float"*): both ends are dropped to the ground.

**MAKILING'S EMBRACE, the tree only, INSTALLED BUT NOT SEEN IN THE GAME** (his editor was closed when it went in).
`tools/build_paete_sentry.py`. His notes: *"should be sterner cuz this is an ultimate ability of essentialy a tree
guardian"* (so NOT cute: narrow slanted slit eyes, one heavy brow, a hard down-turned mouth); *"can you fix the shape of
the actual tree"* (a stance: wide foot, waist, heavy head, a lean, boughs like antlers); *"the crown and roots dont
looke natural and organic"* (nothing repeats, limbs wander sideways and are smooth curves, uneven spacing, a broken
bough, a stub root). Kept from the first tree because he asked for each in September: the wrung rope, the pointed crown
with a glow within and no green mass, eyes that are hollows set into the wood, roots that go into the ground. In
`PaeteSentryBody`: the living vines refitted (`Silhouette`, `TrunkBend`, `FaceBurl`), the crown light widened, the boughs
folded 52 degrees under the court. The roots are held inside the first tree's reach (`PaeteRules.SentryCanClearance`).
⚠️ TO CHECK THE MOMENT HIS EDITOR IS OPEN: `sentryfx <tag>`; the crawl-out with the new roots and boughs; the vines;
the glow. NOT STARTED for this ability: the limbs that drag and wrap players, the ground breaking, the break-out, the
cutscene and Makiling's spirit.

**LIANA LEAP, the owner: "the leap is good to go now".** 15.10's open ask is built:
- `Abilities/PaeteSwing.cs` (the path, pure numbers; `tools/plot_paete_swing.py` is the same lines),
  `PaeteVine.FindCatch` (the catch and its kind; a sky aim snaps to the top edge under it), `CharacterMotor.Swing.cs`
  (the owner of the body walks the path and is thrown). OVER a lip he can stand on, UNDER anything with open space
  beneath (checked FIRST: he said the first cut *"doesnt feel spiderman swing-y"*), WALL, else the old flat REEL.
  PROTOCOL 154 (`PaeteVinePhase` gained three values). His answers: surfaces with a sky aim snapping up, anything in
  range (still 8 m), swing through and fling.
- Momentum (*"does the leap let you keep your momentum? i'm trying to leap on the ground then slide"*): where the
  movement rework drives the body, his own leap's carry is held the whole way and its speed handed to the body
  (`KeepCarryMomentumFor`, `StepCarry`), and crouch held through the leap is a slide out of it.
- The prototype map has a hanging beam to swing from (`CharacterPrototypeMap`).

**Also:** the mouse is taken back on a click after the editor or the OS frees it, and that click is swallowed
(`UI.CursorMode.Recapture`, `PlayerInputReader`; seven direct `Cursor.lockState` writes moved onto `CursorMode`).
A script save by an agent while he was in Play (domain reload off) reloaded assemblies mid-match and the log filled
with null references in unrelated files until he left Play: do not let agents save while he plays if it can be helped.

**MAKILING'S EMBRACE, later the same night (2026-10-08): the tree is SEEN in the game and its effects are reworked.**
The owner opened his editor; `sentryfx` films the rise, two stand-in prisoners, a break-out and the sleep. He said of the
tree in motion *"the swelling trunk looks kinda weird but continue"* (the trunk now tapers; `TRUNK`, `Silhouette`).
Then an agent reworked what was still the old look, in `Visual/PaeteEmbraceFx.cs` and inside `PaeteSentryBody`,
`PaeteRootCoil`, `PaeteBarkShatter` (checked in the film; not seen in a match by anyone):
- the limbs (`PaeteEmbraceLimb`: one bough in the trunk's tones, knuckles, a creeper, a sampaguita on every other one);
- the roots on a prisoner (`PaeteRootBands`: two thick roots that cross round the LEGS' box, cinch, squeeze, judder);
- the arrival (`PaeteEmbraceGround` posed from age, and one-shots for the breach, each claw, each haul, the wake);
- the catch, the break-out (snapped bands, splinters, a star, rings), a quiet let-go, and the sleep's leaves and petals.
⚠️ Four things it found that the next session must know:
1. In a match the tree starts at age 3.6 (`PaeteHeroKit` casts with `handBack: true`), so the rise is only ever seen in
   the CUTSCENE, and the new haul and wake one-shots fire nowhere but the film until the cutscene calls them.
2. The ground branches had been wholly under the court since 2026-09-26 (`Mathf.SmoothStep(0.80f, 1.0f, u)` used as the
   shader's smoothstep). That line is fixed in `VinePath`, so root arches now show at its foot: NEW to the owner. The
   same misuse is still in `PoseTrunkVines` (`feel`) and `PaeteGroundCall.cs` (`dive`), untouched.
3. The tree pops out of existence at the end: it sinks only 2.3 m of its 7 before `PaeteSentry` destroys it.
4. The redesigned bodies are BOXES (legs 0.90 x 0.54 m off `sean-redesign.glb`): bands laid as circles ran inside them.
NOT STARTED: the cutscene and Makiling's spirit against the new tree and ground.

**The rise and the exit, fixed 2026-10-08 (owner: "fix the rise and exit, then continue with the rest"):**
- THE RISE IS NOW IN THE GAME. `PaeteFx.BeginStage/EndStage/StepStage`: an effect made on a stage lives under the stage's
  root, takes its layer, is not stepped by `Update` and moves only when the stage steps it. `PaeteSentryBody.StageFx`
  (set by `HeroIntroductionScene.Paete` to its root) makes the staged tree fire the breach, the claws, each haul, the
  crown shaken and the wake itself; the cutscene steps them on its own clock (`_paeteFxClock`, in real seconds). A
  replay's tree (`RecordedFieldView`) sets no stage and makes none, as before.
- THE EXIT: the whole tree goes under (5.7 model metres, eased, in the same 0.9 s) and its boughs fold up against the
  leader on the way down.
- Two new films: `stagefx <tag>` (the tree as the cutscene stages it, then its exit) and `introfx <tag>`
  (`PaeteAbilityFilm.Intro.cs`: the REAL `HeroIntroductionScene` for Paete, sampled and shot through its own cameras,
  outside Play; a plain stage, no other players, no sound, no grade). Both seen by me; the owner has seen neither.
- His editor was closed twice this night; I reopened it (the normal editor, not batch) to render. He had asked for
  that once ("open unity"); the second time I did it unasked and told him.
STILL NOT STARTED: Makiling's spirit and her meadow (`makiling.glb`, `meadow.glb`, the first pipeline) against the new
tree. Ask him before remodelling her: she carries a lot of his September direction.

**MAKILING, remodelled and painted, 2026-10-08 (owner: "remodel makiling and the meadow too"; of the first pass, "fix
those criticisms, change the maikling shader then show it in game").** `tools/build_paete_makiling.py` writes
`makiling.glb` and `makiling-atlas.png`; the first model is in `ArtSource/paete/props-pre-rework/`. Everything he asked
for in September is kept (tall, not chibi, a baro't saya, long parted hair to her knees, the sampaguita wreath, closed
eyes, a small smile, no nose, the light in her cupped hands) and the node contract is unchanged (`body`, `arm-left`,
`arm-right`, `seed`, `head`, `hair`, `flower-crown`).
- The three faults I named in the first pass and he told me to fix: the tapis was a striped barrel (now one length of
  cloth wrapped on a slant, its free edge turned back down her left front, its stripes in the weave); the saya was a
  clean cone (now seven folds that deepen toward a trailing hem, `flow`/`cloth`); her arms, sleeves and pañuelo were
  one white mass (the upper arm hangs out from her side, the open sleeve falls outside her hip, the pañuelo is narrower).
- ⚠️ THE SHADER NOW TAKES PAINT. `Resources/Shaders/SpiritGhost.shader`: a UV in the atlas's upper half is sampled from
  `_MainTex`, the lower half is still a palette slot (`TumbangPreso/Toon`'s rule). `MakilingSpirit` hands the
  importer's texture to the ghost material before replacing it. Her cloth and hair are painted; her skin, her face's
  ink, the flowers and the light stay slots, because the shader treats slots 8 and 10 specially.
- Seen by me in the game's shader through `introfx` (both her ghost and her full form). The owner has been sent the
  film and has not answered yet.
**THE MEADOW** was finished by a background agent after this was written: see the note under the table.

**Where Paete's rework stands at the end of this stretch:**
| Ability | Model | Paint | Effects | Owner |
|---|---|---|---|---|
| Liana Leap | his own arms | none | swing, knot, sounds (option E) | "the leap is good to go now" |
| Bakya Bloom | pitcher, variant d | yes | `PaeteBloomFx` | "plant looks good" |
| Thorn Harvest | rattan with a plain cute-angry bud | yes | `PaeteThornFx` | saw it in the game, no verdict |
| Makiling's Embrace | stern guardian, organic crown and roots | yes | `PaeteEmbraceFx`, the rise staged in the cutscene, a whole exit | "the swelling trunk looks kinda weird but continue" (fixed); no verdict on the rest |
| its cutscene | Makiling (new), the meadow (new) | yes | unchanged apart from the tree's staged effects | sent a film of her and a before-and-after of the meadow, no verdict |
Not started anywhere: body-clip and sound passes for Bakya Bloom, Thorn Harvest and Makiling's Embrace (only Liana Leap
has had them), the other eight heroes' kits. NOTHING IS COMMITTED.

**THE MEADOW, done by an agent and checked by me in the cutscene film (2026-10-08).** `tools/build_paete_meadow.py`
writes `meadow.glb` and `meadow-atlas.png`: the same six species at the same places with the same 196 node names (moss,
grass, pako ferns, sampaguita, gumamela, makahiya), rounder and painted; `PaeteMeadow` is untouched. Comparison:
`Logs/paete-ability-film/meadow_md5_vs_in1.png`. Its weakest part is a fern frond seen end-on (tiers on a stalk).
70,004 triangles against 35,776, not profiled. The owner was sent the comparison and has not answered.
⚠️ TWO THINGS IT FOUND:
1. REIMPORTING A PROP'S ATLAS IN A LIVE EDITOR SESSION TURNS THAT PROP'S FLAT PALETTE CELLS BLACK until scripts reload:
   the cached `ToonSkin` material loses its `_Palette` array when its texture is reimported (measured with two films,
   `Logs/paete-ability-film/meadow_palette_black_mdx1_vs_mdx2.png`). Painted pieces are unaffected. Nothing clears
   `ToonSkin.Cache` on entering Play, so with domain reload off it may follow him into Play (untested). It applies to
   the pitcher, the rattan and the guardian (their eyes and cheeks are cells) whenever their atlas is rebuilt in a
   session: after rebuilding one, make scripts reload (save any .cs) before judging colours, and consider clearing the
   cache when a texture is reimported. The meadow avoids it by using no cells at all. Makiling is not `ToonSkin`.
2. `tools/build_paete_props.py` still holds the FIRST `seedling`, `thorns`, `sentry`, `makiling` and `meadow` builders
   and writes to the same file names: running it would overwrite the new models. Do not run it; if it must be run,
   pass only what is still its own.

**HIS VERDICT ON THE CUTSCENE, 2026-10-08, and what was started.** *"i like the new models but this is essentially just
the same cutscene textured better. it looks just as similar as it did before. it could use some more creativity on it.
makiling's model looks exactly the same. it could use a remodel and i dont like the outline stuff on her."*
- THE INK IS OFF HER: `SpiritGhost.shader`'s inverted-hull pass is deleted (her full form had a black line round it).
  ⚠️ Not yet settled whether he also meant the ghost's bright rim; asked.
- MAKILING, A NEW FIGURE (`tools/build_paete_makiling.py`, rewritten; the morning's script, model and atlas are kept as
  `ArtSource/paete/props-pre-rework/build_paete_makiling_k3.py`, `makiling-k3.glb`, `makiling-k3-atlas.png`). She
  floats (no feet, a wisp), her skirt is a sampaguita hung upside down (two rings of petals), her tapis is its calyx
  (woven green sepals, gold edges, a gold sash), her hair floats out round her in broad locks with flowers in it (a
  first pass of round tubes ending in spirals read as tentacles), lily sleeves, a garland, a fuller wreath, a larger
  head with a blush. Same node names. Palette slots 5 (blush `E7968A`) and 13 (gold `E2B84A`) changed:
  ⚠️ `MakilingSpirit.Palette` MUST BE GIVEN THE SAME TWO when she is installed. NOT INSTALLED: built to the scratch
  folder, shown to him as a Blender sheet (`Logs/hand-companions/makiling_d2_show.png`), waiting for his yes.
- THE CUTSCENE'S REDESIGN is pitched to him, not started. What a redesign may not move without large cost (found by
  reading `docs/reports/paete-kit-2026-09-25/direction.md` and the code): its length (6.5 s is network timing), the
  pose he ends in (the live clip starts from it), the tree's arrival and pace (`PaeteSentry.BodyLead`, the no-repeat
  hand-back), and THE TAKE (3.8 to 5.0). What is cheap: the three shots before it (a table in
  `tools/author_ultimate_intros.py`), her acting (`Look` curves), where and how big she stands, the effects.
  His September asks that stand: he goes to the ground and his roots connect, he channels and glows, his roots travel,
  the tree crawls out; she is see-through and watches over him, briefly whole; her meadow comes and goes with her; it
  opens with leaves; "A LOT MORE VFX AND SHIT LIKE THE GENSHIN REFERENCES"; no deer; no three small plants.
- His answer to that sheet (d2): *"the head shape is still the same, idk it just looks too basic, clothing is good. hair flow i
  like the idea but its poorly executed that it looks like octopus tentacles instead of flowwy and floaty hair"*. So the
  CLOTHING IS APPROVED (flower skirt, calyx tapis, sleeves, garland). Then, in the same script (sheet d4,
  `Logs/hand-companions/makiling_d4_show.png`, shown, no answer yet, still NOT installed):
  - THE HEAD IS SCULPTED (`head_point`, `cap_point`, `sculpted`): a wider skull, a jaw narrowing to a soft chin, cheeks, a
    small nose, eye hollows; hair in two lobes off a parting set further back (a higher brow); a broad sweep of hair
    each side from the parting past her temples; ears with gold drops; her closed eyes are low on her face and drawn as
    swelling crescents with two short lashes sweeping out (three long ones read as tears running).
  - THE HAIR IS ONE STREAM (`STREAM`, `stream_line`): what made tentacles was separate, alike arms radiating from one
    middle. Now ten broad locks and five thin strands share one line, carried off behind her right shoulder the way her
    skirt drifts, lying over each other and only parting toward the end, which lifts. Three more locks hang down her
    back and turn toward the current at their ends. All of it is still the one `hair` node; if it must MOVE like hair
    in water, the stream wants splitting into a few nodes that `MakilingSpirit.Pose` waves in turn.
- Of d4 he said *"weird head shape.."* (a broad brow over a pointed chin, two lobes of hair). The head is now one
  smooth shape with a round chin and only soft relief, and there are TWO to choose from, `--head=a` (oval, hair parted)
  and `--head=b` (rounder, a fringe over her brow): `Logs/hand-companions/makiling_heads_show.png`, shown, waiting for
  his pick. The head also sits 3 cm lower on a thicker neck. Whichever he picks becomes the default of `--head`.
- Of those two heads: *"neither. the it just doesnt fit our current style"*, and of the hair: *"looks good"* (so the STREAM
  IS APPROVED). The current style is the redesigned cast's: a soft box head, a flat face with two plain dark eyes, a
  round blush, a one-stroke smile, no nose, no brows (`tools/author_character_redesign_cheska.py`). Given that box:
  *"maybe slightly more rounded, the boxy look doesnt work when her body is slim and rounded"*; given a rounded block,
  with a round U sketched under her cheeks and a picture of Princess Bubblegum: *"more.. make her look like princess
  bubblegum's head shape"*. So her head is now ONE SMOOTH TALL OVAL with a round chin and a domed crown
  (`_oval_rows`, `soft_box`), the cast's flat FACE laid on its surface (`face_z`, `on_face`, `laid_on_face`), hair that is
  the same oval larger and set back, a parted fringe of thick rounded locks. `--eyes=closed|open` (closed strokes, or
  the cast's dark blocks): `Logs/hand-companions/makiling_i_show.png`, shown, waiting for his pick. STILL NOT INSTALLED.
- THE CUTSCENE'S DIRECTION IS CHOSEN (the same message): *"i like a combination of"* idea 1 (she is vast and bends down
  to lower the light to him, solid only round her face and hands) and idea 2 (the camera drops through the court when
  he slams it, rides his root through the dark soil and bursts up with the tree's first claw), with *"if she's the
  size of the mountain- maybe instead she could fill up the sky or something like a god visible in the sky"*. Idea 3
  (her hand raising the tree) was not chosen. NOT BUILT YET. Notes for building it: the phase camera copies the live
  one (far plane 240 m), `WorldOutline` reads only opaque depth so it draws no line round her, and each authored shot
  is judged for a clear line at its END (`UltimatePhaseView.ChooseShot`), so a shot that goes underground must end
  above ground.

**LIANA LEAP, steered (2026-10-08, owner: "can you also fix the leap swing. its not possible to swing side to side when
the cluster is pulling u directly forward to it").** The swing was on rails in one upright plane straight at the catch.
`CharacterMotor.Swing.cs` (by an agent, read by me, compiles): a sideways speed across that plane, seeded from the
sideways part of the speed he had when the vines took him (to 10 m/s), pushed by his strafe keys toward 7 m/s
(`SwingSideSpeed`, reached in a third of a second, `SwingSideAccel` 21), kept when no key is held, and thrown with him
at the end (the fling is forward plus sideways). Where he is across the plane is MEASURED from the body each step, so
collision always wins; steering into a wall stops the sideways speed instead of tripping the "lost" check. Nothing on
the network, `PaeteSwing.Plan` untouched. NOT PLAYED BY ANYONE: the two numbers he is likeliest to want changed are
`SwingSideSpeed` and `SwingSideAccel`.

**MAKILING IS INSTALLED, AND THE CUTSCENE'S FIRST HALF IS RESTAGED IN THE SKY (2026-10-08, later).**
- Her head, after the oval: *"no. her face is too high making her chin look bloated, and the fringe got ruined"* (her face
  moved 47 mm down, the fringe hangs to her eyes); *"continue with that"* (the go-ahead to install); three wreath flowers
  and a square shoulder of hair each side circled (the wreath rides outside the fringe, the cheek locks leave from
  under her hair, her hair comes down the sides of her head); *"why is the face a model instead of a face texture?"*
  (HER FACE IS PAINTED NOW: `R_FACE` in the atlas, `face_uv`, drawn in `paint_atlas`; `SpiritGhost.shader` `_FaceRect`
  treats its dark pixels as her ink so her eyes show when she is a ghost); *"you might have to merge the neck and head
  mesh into one, look at the neck in her ghost model"* and *"neck needs to be straight and more cut"* (the neck is part of
  the head's mesh, one straight column cut in under a round jaw, `_with_neck`). Eyes: closed is the default
  (`--eyes=open` gives the cast's dark blocks; he never chose between them). `MakilingSpirit.Palette` has the new slots.
- SHE IS IN THE SKY (`HeroIntroductionScene.Paete.cs`, `MakilingSky`, `MakilingSkyScale` 44): on the line straight behind
  him (x = 0, so a MIRRORED shot keeps her in frame), sunk to her breast, with her meadow, mist and fireflies still at
  her old spot beside him. His words on the first cuts: *"she needs to feel more divine with glow and sky effects. right
  now she doesnt look like a god in the sky"* and *"you should also make it so we can see her full dress, even for just a
  short time"*. So: she comes up over the horizon far off and WHOLE in her own colours, then comes to the court by
  GROWING (`MakilingSpirit.Look.Size`; in a sky growing is coming nearer) and sinking until only her breast, hands and
  face are over the horizon, and gives him the light from there (it falls 0.53 to 0.70). `MistFrom`, `MistTo` and
  `FormFloor` on `MakilingSpirit` move with that.
- THE SKY ROUND HER is `HeroIntroductionScene.PaeteSky.cs` (new): a dusk behind her in three rings, a halo (soft glow, hot
  heart, two rings, a toothed crown, a sunburst of thirteen typed rays), ten chunky clouds along the horizon that part
  as she comes close, five shafts of light, ten stars; `MakilingSpirit.Shine` and the shader's `_FormRim` light her.
  The dusk is a flat sheet facing the court, so it is faded out before the side-on shots (1.86 to 1.98).
- The CALL and ROOT shots look up past him (`tools/author_ultimate_intros.py`); the table has a new shot, THE DIVE
  (1.93 to 2.70), whose camera is to be computed by `PuCamera` in `HeroIntroductionScene.PaeteUnder.cs`: an agent was
  building that when this was written. ⚠️ Regenerating the table rewrites every hero's file: `git checkout` the others.
- Seen by me in `introfx` films only (`Logs/paete-ability-film/introfx_sk7.mp4`): a bare stage with no map. NOT SEEN ON A
  REAL MAP BY ANYONE: there she is 72 m behind him and whatever the map has on that side hides her from the ground up,
  and a roof would hide her altogether. The phase camera's far plane is the live one's, 240 m; she reaches about 130.
- THE DIVE IS BUILT (by an agent, its film and files checked by me; `Logs/paete-ability-film/introfx_sk8.mp4` is the whole
  cutscene with the sky and the dive together, sent to him, no answer yet). `HeroIntroductionScene.PaeteUnder.cs`
  (`BuildPaeteUnder`, `SamplePaeteUnder`, `PuCamera`, hooked from `BuildPaete`, `SamplePaete` and the top of `PaeteFrame`)
  and a new unlit shader `Resources/Shaders/PaeteSoil.shader` (the soil is lit only by his root; a lit material under
  a court is black or patchy). On the 5.0 clock: 1.93 the lens plunges past his arm into the court; 2.10 to 2.47 it
  rides his three roots down a burrow of banded soil past a red slipper, a tin can, stones and two worms; 2.50 the
  heads hit the court from below, cracks of light; 2.60 a two-frame lime flash and it is out beside the claws.
  ⚠️ What it could not check, and nobody has: a real map (the set is a closed shell 1.9 m deep and 6.3 m wide under
  the court, so any map geometry inside that volume shows; sloped or kerbed courts), the phase camera MIRRORING or
  pushing in the shot (worked out in numbers only), reduced effects, the game's grade and `WorldOutline` over it. The
  sunk guardian's crown shows in the hollow for three frames, lit by the sun and not by the root. No sounds are made
  for it. The moments that want one (5.0 clock): 1.93 a whoosh down; 2.02 the punch through, then everything muffled;
  2.10 the roots launching; 2.10 to 2.47 earth rushing, rootlets snapping; a tink off the can about 2.28; 2.50 a knock
  under the court; 2.50 to 2.58 cracking and clods; 2.60 the burst, unmuffled; 2.64 the claws.

**THE CUTSCENE IS 9.0 S ON AN UNEVEN CLOCK, AND SHE STANDS IN FRONT OF THE WALLS (2026-10-08, evening).** His verdict on
the whole cut (sky plus dive, still 6.5 s): *"it looks like its sped up. theres not much weight to the timing of
things"*. Asked how to buy the time, he chose **"Lengthen to about 9 s"**, said the rushed part was all of it, and added:
*"its hard to see her especially when its in an enclosed area, for example in the prototype map, the walls cover most of
her body, and this can be the same for other maps like the cities or the arena. so think of a way to counterract that"*.
- THE CLOCK. `PaeteStretch` (an even 1.3) is gone. `HeroIntroductionScene.Paete.cs` `PaeteClockAt`/`PaeteRealAt`,
  `PaeteClock(real)`, `PaeteReal(clock)`: a table mapping the 5.0 s clock every beat is typed on to real seconds, straight
  lines between (0, .55, .85, 1.10, 1.93, 2.70, 3.80, 5.0) and (0, 2.00, 2.70, 3.10, 4.34, 5.94, 7.44, 9.0). Her arrival
  has 2.0 s (was 0.72), the dive 1.6 (1.0), the light 0.7 (0.39); the slam, the hauls and THE TAKE keep close to their
  old pace, because a slow blow has no weight either. ⚠️ THE SAME TABLE IS IN THREE PLACES: there,
  `tools/author_ultimate_intros.py` (`PAETE_CLOCK`, `PAETE_REAL`, `_warp`) and `tools/build_paete_audio.py` (`theme`, `T`).
- ⚠️ PROTOCOL 155, `UltimatePerformance.MaxSeconds` 9.0 (was 6.5): the length is network timing. `PaeteSentry.BodyLead`
  does not change (the staged tree's clock is typed on the 5.0 one).
- SHE IS STOOD IN THE CLEAR AIR BEHIND HIM (`PaeteSkyRoom`): fifteen rays measure the depth behind him over the width
  and height she takes; she, her halo, her clouds, her shafts, her stars and the dusk are all sized by it
  (`_paeteSkyFar` 6 to 72 m, `MakilingSkyScale` 0.61 of it, `_paeteSkyShare`). From the two shots that look at her she
  fills the frame the same at any depth; the dusk stands close behind her hair so it hides the wall she is in front
  of. It cannot cure a ceiling lower than her head or a wall nearer than about 8 m behind him.
  `PaeteAbilityFilm.Intro.cs`: a tag beginning `wall` films it with a wall 16 m behind him (`PaeteSkyClearForFilm`,
  because the film's stage has no physics for the rays).
- ⚠️ THE THEME IS NOT IN THE GAME AT ALL (found by the audio agent): commit 13b338709 (2026-09-29) deleted every skill
  sound on his word and `AudioCues.cs` says not to put them back until he asks, so `sfx_ult_theme_paete` is silent and
  was before today. `tools/build_paete_audio.py` `theme()` is retimed to the table (9.000 s, its loudest moment the
  thud) and builds only to a scratch folder (`PAETE_AUDIO_OUT`); nothing was written under `Resources/Sfx`. Not
  listened to by anyone.
- NOT SEEN: the 9 s cut on film (his editor was in Play when it was asked for; the request `introfx t1` then
  `introfx wall1` waits in `Temp/`), her in an enclosed map, the rays against real colliders.
- ⚠️ THE "STOOD IN THE CLEAR AIR" ANSWER WAS REFUSED AND IS GONE (`PaeteSkyRoom`, the paragraph above): he, having tried
  it: *"we have a distance fade effect for objects near the camera.. maybe you could apply that?"*, *"because its weird
  that shes so close"*. She is 72 m off again, always. Instead THE MAP IS OPENED TO HER: `Shaders/NearFade.shader`
  (the shader the whole dressed map wears) has two GLOBALS, `_SkyReveal` and `_SkyRevealShape`, and dissolves, by its
  own screen-door, every fragment inside a cone from the eye toward a point (not nearer than 8 m, not lower than a
  knee over the court). `NearFade.OpenSky`/`CloseSky` set them; `SamplePaeteSky` opens a window of 48 to 62 degrees
  round her for the two shots that look at her and closes it with the dusk before the dive; `Dispose` closes it.
  `WorldOutline.shader`'s mask pass carries the same rule and `WorldOutline.cs` masks every near-fade renderer while
  the sky is open, or the ink would still trace the walls that are gone. Seen by me in a film inside brick walls and
  a roof (`introfx wall4`, `Logs/paete-ability-film/introfx_wall4.mp4`; a tag beginning `wall` builds them). ⚠️ It
  reaches only what wears `TumbangPreso/NearFade`. The character prototype map's two grid materials were Standard:
  they are on the map's shader now (`fadewalls <tag>` did it in place; `CharacterPrototypeMap.MakeMaterial` does it on
  a rebuild). NOT SEEN ON A REAL MAP OR IN PLAY BY ANYONE; the extra mask draws while it is open are not profiled.
- SOUND: he said *"also needs sound effects"*. An agent is drafting three whole soundtracks for the 9 s cutscene from
  the real CC0 recordings (`ult_a|b|c` in `Logs/paete-sfx-drafts/`, with a video of each) for him to choose by ear.
  Nothing is installed.
- *"dissolve doesnt work if the wall is too close"* (a screenshot from Play of a pillar about 5 m from the lens standing
  solid across her): the window kept everything within 8 m of the lens. It keeps nothing for being near now (0.3 m);
  only the ground stays. Compiles; not seen.
- THE THREE SOUNDTRACKS ARE DRAFTED (by an agent; NOT LISTENED TO BY ANYONE, sent to him to choose by ear):
  `Logs/paete-sfx-drafts/ult_a|b|c.wav` and `video/ult_a|b|c.mp4`. a is earth and wood only; b is the same with a
  voice for her (singing bowl, a choir's held note, chimes, a gong); c is thunder and drums with a more muffled
  underground. `py -3 tools/build_paete_ult_sfx.py` rebuilds them (every time in one table `T`);
  `tools/mux_paete_ult_video.py` is the Blender mux. 43 more CC0 recordings were fetched (`tools/paete_sfx_sources.json`
  59 to 101, `ArtSource/paete/sfx-sources/`); one unused one's page says it is synthesised and three used only in c
  were processed by their authors. Nothing is installed; `sfx_ult_theme_paete` is still silent in the game.
- HIS PICK: *"B but the trunk going up sound feels so light"*. b's three heaves were a creak, a crack and soil, all above
  the low end. Added to b only (`tools/build_paete_ult_sfx.py`): `rise_rumble` (a rockslide kept under 260 Hz from the
  claws to the top), `haul_heavy` (the big log on dirt and the dead pine's crash, low, under each heave), c's bass drum
  under each (`haul_low`), and the heaves a fifth louder. They now sit 6.0, 4.4 and 2.3 dB under the thud, which is
  still the loudest moment. ⚠️ b IS INSTALLED: `Resources/Sfx/sfx_ult_theme_paete.wav` (9.00 s, peak 0.85), registered
  in `AudioCues.cs` (gain row, known names, `ReworkedSkillSfx`), so `HeroIntroductionScene.StartSound` plays it on the
  cutscene's clock. The heavier heaves have NOT been listened to by anyone; the video is
  `Logs/paete-sfx-drafts/video/ult_b.mp4`.
- Then *"coiuld the trunk sounds itself be a bit deeper"*: the weight was under the tree, and the creaks and cracks that
  ARE the tree were unchanged. b now plays each heave's own recordings three semitones down (`DEEP`, the most the
  builder allows) with the top off at 3 kHz, led in by a ship's low timber creak (`haul1_b` to `haul3_b`). Rebuilt
  and installed over the first. Not listened to by anyone.
- ⚠️ THE SKY WINDOW STAYED OPEN OUTSIDE THE CUTSCENE (his screenshots from ordinary play: *"why is the distance fade
  happening"*, *"distance fade isnt just happening in the floor, but also the walls, outside of the cutscene"*). Two
  faults of mine. (1) A shader global outlives whatever set it: a cutscene that ended without `Dispose` (a round reset,
  a script reload while he was in Play, leaving Play mid-cutscene) left the window open over the whole game, in the
  editor across Play sessions. `NearFade.OpenSky` must now be asked for EVERY FRAME: any camera about to draw closes a
  sky whose last `OpenSky` is more than two frames old (`CloseAStaleSky`), and it is closed when the game or the
  editor's scripts start. (2) I had put the prototype map's FLOOR on the map's shader with its walls: the shader
  spares the ground only when it is a standing eye's height (0.8 m) below the lens, so it stippled away round a
  crouched camera. The floor is back on Standard (`fadewalls`, `CharacterPrototypeMap.MakeMaterial(mapShader:)`).
  ⚠️ FOR WHOEVER OWNS THE MOVEMENT REWORK: the same will happen to any shipped ground or block top on
  `TumbangPreso/NearFade` under a crouch or a slide; the guard is `belowFeet` in `NearFade.shader`.
- *"instead of keeping the same length, do the same thing you did with the fpv arms and the leap swing where the arms grow
  out. his live character's vines and twisting branches should be animated in a way thats more organic and fluid"* (of
  his kneel in the cutscene), then *"if the vines are baked into the body mesh, then rework it and add bones if
  needed"*: an agent is on it (growing arms into the court on the leap's pattern, living vines on his body; allowed to
  rework his model and ADD bones through his authoring pipeline). Not reported back yet when this was written.
- Sound: of b's instruments, *"idk about the bells. it should be more chimey and the overall instrument composition
  sounds too rigid and flat"*: the sound agent is making two takes (`ult_b1`, `ult_b2`). b with the deeper trunk is
  what is installed meanwhile.
- ⚠️ CORRECTION TO THE NOTE ABOVE: the window had NOT stayed open. He: *"it didnt stay open. it just fades out when im too
  near"*. What he saw on the walls was the shader's ordinary screen-door at the lens, which the prototype map's walls
  had never had before I put them on the map's shader. Their near band is shut now (`_NearFadeStart` 0.001 on that one
  material), so on that map they only open for the cutscene's sky. The self-closing of the sky window stays: it is
  still right that a global cannot outlive its cutscene.
- AND THE GROUND RULE MEETS THE CROUCH PROPERLY (he, of the note that it was the movement rework's to settle: *"fix
  that"*). `NearFade.shader`'s `belowFeet` spared the ground for being 0.8 to 1.1 m under the lens, a standing eye's
  height; a crouch (0.55 m lower), a slide (0.70), the taya's squat and Paete's kneel all failed it and the floor
  dissolved round the camera. `CameraRig.ApplyFpp` now sets a global, `_NearFadeEyeDrop` (`NearFade.EyeDropId`): how far
  it has lowered the eye from standing; the shader and `WorldOutline.shader`'s mask add it back, so the floor is
  judged as if he stood. `ApplyTpp` sets it to 0. Compiles; NOT SEEN IN PLAY by me.
- THE TWO CHIMIER TAKES OF b ARE DRAFTED (sound agent; NOT LISTENED TO, sent to him to choose): `ult_b1` (wind chimes:
  koshi rods, a bar chime, a mark tree running down, no bowl) and `ult_b2` (a little tune on a toy glockenspiel with a
  music box and a kalimba under it), both in D major, both on b's body with the heavier, deeper trunk. The gong, the
  bowl strikes and the chime tubes are gone from both. Her voice is a phrase now: it climbs through her arrival, peaks
  on his eyes igniting, and echoes on the heartbeats and the tree's eyes. 30 more CC0 recordings (manifest 102 to
  131). Nothing new is installed: the game still plays b.
- Of the arms: *"make it so it really looks like his arms are being planted into the ground"* (passed to the arms agent).
- SOUND, his pick of the two: *"b1, okay nice i like the start sounds with the chimes, but it doesnt carry on well when the
  animation leans to be more dramatic near the end. i was thinking of a whistle before paete slams down, then some more
  dramatic musical sounds after. because the soft chimes dont sell the weight of the tree"*, then *"get it done in 5
  minutes"*. `ult_b3` (one take, built in a hurry, its new recordings' spans and pitches NOT measured, NOT LISTENED TO):
  b1 to his eyes, a slide whistle falling into the slam (497093), then bass drum, timpani, a timpani roll under the
  earth and a low tuba; her chimes once, on the tree's eyes. The burst opens only 5.1 dB (the builder wants 6). Sent
  to him; not installed (the game still plays b). 41 unused CC0 recordings from the abandoned b4 are on disk.
- THE PLANTED ARMS AND THE LIVING VINES ARE BUILT (arms agent; compiles; sheets
  `Logs/paete-ability-film/planted_live_sheet.png`, `planted_cutscene_close_sheet.png`, `planted_cutscene_real_sheet.png`,
  `planted_body_vines_sheet.png`). `Visual/PaeteRootArms.cs` (new): from each of his eight braid strands a sleeve up
  the strand's line, then new round wood in his own material and paint (the leap's `BuildReach` pattern, nothing
  scaled), driven down on the slam at a fixed mouth with a flared base, running under the court, surfacing once and
  forking; they swell per heartbeat and strain on the send and the hauls. `PaeteGroundRoots` gained six tipped slabs
  and a soil mound per mouth and lost its eight hand roots. Five new vines creep down his arms (cutscene and live).
  `Visual/PaeteLivingBody.cs` (new) moves his baked vines, strands, antlers and leaves on a private mesh copy, CUTSCENE
  ONLY. ⚠️ ITS OWN LIST OF WHAT IS NOT DONE OR SEEN: the model was NOT reworked (no vine bones: the pipeline rebuilds
  his glb byte-identical, so adding them is about half a day; needed for his vines to move in live play); in the real
  ROOT shot his hands are at the bottom edge behind the meadow so the planting barely shows (a reframe or an insert
  needs `tools/author_ultimate_intros.py`); FIRST PERSON IS UNSEEN; `PaeteGroundCall` itself was not run; the vines'
  motion was only seen in stills; his other clips were not re-filmed after the change.
- Of b3 and the cut: *"i was thinking of a deeper whistle, without the falling note before the slam. you can see a
  mismatch when it cuts back to both paete and the tree"*; of her light given to him: *"it seems like glowing orbs of
  power from makiling are flowing through his arm, but its not obvious because the particles get hidden/blocked by
  paete himself. i'd also prefer if they came in through the tip of his arm, causing pulsing light waves across his
  body"*. Done (film `introfx v1`, `Logs/paete-ability-film/introfx_v1.mp4`, seen by me):
  - THE MISMATCH, as I read it (he did not say which): the picture went under the court from a dusk with her in it
    and came back up into plain daylight with neither, because the RISE was shot from his side. The two RISE shots
    are from in front now (`tools/author_ultimate_intros.py`), the tree near, him beyond, her in the sky behind; the
    dusk and the opened sky hold until the mist takes her (`dusk = here` in `SamplePaeteSky`).
  - THE LIGHT: its glows are drawn 0.9 m toward the lens so his body no longer hides them, and
    `SpiritVeins.shader` has `_WaveFrom`/`_WaveRadii` (`PaeteChannelGlow.Waves`): three rings of light run across him
    from his raised hand as it lands (0.70, 0.79, 0.88 on the clock).
  - The whistle: the sound agent is replacing b3's falling slide whistle with a deeper held one.
- ⚠️ THE MISMATCH WAS HIS GLOW COPY, AND I GUESSED WRONG TWICE FIRST (the sky, then how bright he was). He: *"the glow is not
  the problem. its the fact that his ghost and his main body arent aligned"*. `PaeteChannelGlow`'s skinned shells share
  his bones but were not told to re-skin per render (`forceMatrixRecalculationPerRender`), as his body's renderers
  are: the cutscene poses him and renders at once by hand, so the shell was drawn a pose behind whenever he moved
  (the wide shots after the dive, where he heaves). Set on every shell; seen aligned in film `introfx v4`
  (`Logs/paete-ability-film/align_before_after.png`). The fault predates today. The glow's strength is as it was.
- b3's flute now comes in at 1.95 s and holds to the slam (*"low bamboo flute needs to go further back for longer"*);
  her light arcs over him and in at his fingertip from outside, as he drew it. The sound videos are muxed on the
  current picture now (`FRAMES` in `tools/build_paete_ult_sfx.py`; they had been on an older film). b3 NOT installed.
- *"install it"*: ⚠️ b3 IS INSTALLED as `Resources/Sfx/sfx_ult_theme_paete.wav` (b1's chimes, the early held bamboo flute,
  then bass drum, timpani and tuba). His other two answers: the close insert on his hands, yes; vine bones, *"do it"*.
  - THE INSERT is in the shot table: 1.17 to 1.52 on the clock, low and close on the two mouths where his arms go
    into the court (the ROOT shot is split round it; eight shots now). Seen in film `introfx v6`
    (`Logs/paete-sfx-drafts/video/ult_b3_v6.mp4` is that film with b3). `PaeteAbilityFilm.Intro.cs` now calls
    `AssetDatabase.Refresh()` first: a film asked for straight after regenerating the table shot the old one.
  - VINE BONES: the arms agent is adding them through his authoring pipeline (ADD only), to be driven in live play
    too. Not back when this was written.
- THE VINE BONES ARE IN HIS MODEL (arms agent; compiles; NOT RUN IN PLAY BY ANYONE). Eleven bones appended, nothing
  renamed: `antler-left/right`, `leaves-crown` under `head`; `branch-back`, `vine-chest-a/b/c`, `leaves-collar`,
  `leaves-hip` under `torso`; `vine-leg-a/b` under `leg-right`. Built by `tools/author_character_redesign_paete.py`
  (`VINE_BONES`, `REBONE`, `soft_vine`): points, normals, texels and triangles byte-identical, only weights changed, all
  33 clips kept. The old glb is `ArtSource/paete/redesign-20261005/paete-redesign.before-vine-bones.glb`.
  `Visual/PaeteVineBones.cs` (new) holds one typed table and one `Pose`; `PaeteVineBonesDriver` creates itself at load
  and poses every body that has the bones in `LateUpdate`; `PaeteGroundCall` calls `PaeteVineBones.Strain` while he
  kneels; the cutscene's `PaeteLivingBody` poses the same table from its clock. Films `all bn1` (his five clips
  intact, bones at rest since the driver does not run outside Play), `introfx bn2`, `armsw5`. ⚠️ UNVERIFIED: the live
  motion and its amplitudes (first guesses, never watched), the kneel's strain, replays, whether a live clip or a
  procedural pass overwrites the new bones, first person (separate cut meshes, untouched), the cost of the driver's
  scan of every skinned renderer each 1.5 s. His shoulder leaves have no bone and move only in the cutscene.
- *"glow isnt applying to his vine extensions"*, *"i was referring to the planted arms in the cutscene"*: his glow
  (`PaeteChannelGlow`) never reached the arms he grows, for two reasons found one after the other: `Attach` skips
  anything carrying `VfxRenderTag` (the grown arms are tagged as effects), and `SpiritVeins.shader` weighs a surface
  by its palette cell, and his wood's weigh nothing. `Attach(..., effects: true)` and `Flat(weight)` (`_FlatWeight`)
  were added; the cutscene has `_paeteGrownGlow` on `PaeteGroundRoots.ArmsRenderer`, and live play has `_grownGlow`
  on that and on the first-person limbs (`PaeteVineHands.ReachRenderers`). Cutscene seen lit in film `introfx v9`;
  LIVE AND FIRST PERSON NOT SEEN.
- THE LIVE TREE FACES ITS CASTER (owner: *"the tree faces backwards from where i cast it"*; asked, "Face toward me").
  `PaeteSentry.Spawn` calls `SetFacing(from - at)` (was `at - from`, the seed's flight). The cutscene's staged tree
  keeps its own yaw (`PaeteTreeYaw`). ⚠️ NOT CHECKED: the last frame of the cutscene against the first of play (the
  tree may turn at the hand-back), and replays.
- *"fpv planted arms dont align with the ground hole stamps"*: the first-person limbs were aimed 45 cm under the court,
  and the viewmodel is drawn over the world, so they ended short of the holes on screen. They aim at the mouths
  themselves now (`PaeteGroundCall`, `ReachTo`). Compiles; NOT SEEN (nothing films first person).
- *"is he supposed to be floating off the ground? if so can you make it more obvious? make him hover up and down or more
  in the air"* (he stood a hair off the court by accident). It is meant now: the `paete` lift keys in
  `tools/author_ultimate_intros.py` raise him 0.2 to 0.5 m from 0.30 on the clock, bobbing, and he DROPS from 0.52 m
  into the slam. Film `introfx h1` (`Logs/paete-sfx-drafts/video/ult_b3_hover.mp4`). ⚠️ The live clip that play
  resumes from is unchanged (he ends on the court as before). Why he was a hair off the court on his map to begin
  with was not found.
- Of the hover: *"he goes up and down too fast, i'd apprecieate if his non-moving arm and legs also swiveled around
  slowly"*; of the column of leaves standing on bare court: *"who are the leaves and vfx supposed to be orienting here?
  if its paete, make it align, if its makiling, make it align"*. The lift is one slow rise, a dip and a last lift;
  the two HOLDS in his body while he floats (`offer`, `ignite`) are drifting keys for his right arm and legs; the
  gust, the wind ribbons and what `PbAnchor(0)` anchors turn round HIM now (they were round `MakilingStand`, her old
  spot beside him, empty since she went to the sky). Her meadow, mist and fireflies are still at that spot. Film
  `introfx h2` (`Logs/paete-sfx-drafts/video/ult_b3_hover2.mp4`).
- *"his arms are bent outwards. they arent bent straight to the front or inwards"* (drawn over him kneeling, from behind,
  in play). Every kneeling pose held his arms 5 degrees out and turned 14. Now 1 and 2, in the cutscene's table
  (`tools/author_ultimate_intros.py`, every `plant`-derived pose) and in the live clip (`BuildPaeteSentry`, `L`/`R`), which
  was BAKED (`bake bk2`); the live mouths moved in with them (`PaeteGroundCall`, 0.27 and 0.25 out, were 0.40 and
  0.38). ⚠️ NOT VERIFIED: no camera I have looks at his arms from behind or in front while he kneels (the meadow hides
  them in the cutscene's own shots); whether his palms still meet the court at the new angles was not re-solved.
- *"the pitcher plant fdoesnt have sfx"*: BAKYA BLOOM's sounds are DRAFTED, three options of eight cues each (sound agent;
  NOT LISTENED TO, sent to him to choose): `Logs/paete-sfx-drafts/bloom_<cue>_<a|b|c>.wav`, `video/bloom_a|b|c.mp4`,
  `py -3 tools/build_paete_bloom_sfx.py`. a wet and botanical, b wood and knock, c a small creature (a squeaky toy).
  Nothing installed. ⚠️ For the install: `sfx_paete_sprout_land` plays where the CLOG lands, not the seed; the seed's
  landing and growing, and the knock on the can, have NO cue name in the game yet (they need call sites). The film has
  no command press and no withering. Thorn Harvest and the live tree are still silent.
- Bakya Bloom's sounds: of a, b, c he said *"i like a and C the most but they're all too harsh on the ears"*; option `d` (a's
  body, c's squeak as a quiet coo, softened, peak 0.55) was sent; NO ANSWER YET, nothing installed.
- THE SESSION MOVED ON (2026-10-08, late): a handoff for the other eight heroes' cutscenes is
  `docs/CUTSCENE_REWORK_HANDOFF.md` (another session will do them in batches of three), and this session picked the
  maps up again with a rebuild of Eskinita: `docs/ESKINITA_REBUILD.md`.
