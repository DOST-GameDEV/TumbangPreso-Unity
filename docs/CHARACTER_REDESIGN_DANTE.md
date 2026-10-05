# Character redesign prototype: Dante (displayed as Basilio)

> ⚠️ **START AT SECTION 15.** It is the current state (end of 2026-10-05, second session) and it
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
