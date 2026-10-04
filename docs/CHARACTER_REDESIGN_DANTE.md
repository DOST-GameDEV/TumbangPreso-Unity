# Character redesign prototype: Dante (displayed as Basilio)

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
