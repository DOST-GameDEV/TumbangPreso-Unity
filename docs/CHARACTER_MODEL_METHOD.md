# Character model method

How a hero model is made or reworked in this repository, written down after the Rafi
islander rework of 2026-09-25 (v10 to v29, `tools/build_rafi_voxel.py`). CLAUDE.md and
AGENTS.md point here. Read [Art_Direction.md section 0](Art_Direction.md#0--new-models-must-belong-to-tump)
and [Voxel_Person_Guide.md](Voxel_Person_Guide.md) alongside it; this file is the working
method, those are the laws and the builder's internals.

## 0 · The standing questions (owner, 2026-09-25)

Every render is judged against these, in this order, and the answers are written down
before the next change:

1. **Does it feel like it belongs in the cast?** Put it in the hero lineup, never alone.
2. **Does it look visually pleasing?**
3. **Is it great?**

Add your own critiques on top every time. The ones that caught real faults on Rafi:
silhouette at lineup distance, colour hierarchy (what does the eye hit first), whether any
motif repeats or collides with another, whether a shape reads as something it is not (a
row of teeth under a chin reads as a second mouth; a flat plate on a chest reads as a
badge), the back and side views, and how it moves.

## 1 · Research first, then a written plan

- Read what the owner supplied as references and research the real subject before
  building (sources written into the brief). For Rafi: the Boxer Codex accounts of Visayan
  pintados (putong, bahag, batok by body part), Maui's stylisation, and later Harbor
  (Valorant) for how a water character stays subtle.
- Inspect how the existing heroes are built before touching anything: which builder makes
  which `.glb` (`team-dante.glb` comes from `build_bayan_voxel.py`, `team-sean.glb` from
  `build_iggy_voxel.py`, `team-cheska.glb` from `build_person_voxel.py`'s Inday recipe).
- Write the brief in `ArtSource/<hero>/<pass>-<date>/design-brief.md` with the owner's words,
  the research and the plan; archive the model being replaced beside it
  (`ArtSource/rafi/rejected-green-shirt-v9-20260925/`).

## 2 · Build it the cast's way

The builder is a per-hero copy of the voxel person builder. Never edit another hero's builder.

- **Rig and proportions:** the base CC0 rig and the family pass (legs 24 %, torso 23 %,
  head 53 %); height inside the cast's 0.6613 to 0.7928 range; feet on zero; the seven bone
  names untouchable. The builder refuses to write otherwise.
- **Head:** the donated native skull (`character-male-d`), block-built hair, **no eyebrows**,
  and an **ink-only face**: the eyes and mouth are solid slot-8 shapes, nothing else. No
  white glints, no teeth, no second colour (owner: "everyone else has just black eyes and
  just black mouth"). Attitude comes from shape: the eye's top edge does a brow's job.
- **Forms:** chamfered boxes (`BEVEL_FRACTION` 0.45) rigidly skinned to one bone each; tapers
  and tilts for cloth ends and hair tips; tubes and lofts (`_rafi_tube`, `_rafi_loft`) for
  rings, cords and headbands. Muscle is **form, not shading**: a pectoral slab, a deltoid
  block, and only "a bit" (Rafi v26 at 18 mm proud read "too muscular"; 10 mm is right).
- **Hair silhouette:** a core with a ring of separate chunky curls at graded heights reads
  as hair; stacked full-width layers read as a cake, lumps of one height as a crate.

## 3 · Colour: its own, and quiet

- Each hero owns one hue no other hero wears. Check the lineup: Sean red and orange, Cheska
  light cyan and white, Dante forest green and gold, Zack yellow, Nemu violet, Phaister
  magenta, Rafi a muted sea teal with silver.
- A signature colour is **subtle**: low saturation, carried by one or two cloth pieces, with
  the character's element living in the ability VFX, not the costume (the owner's Harbor
  reference: "a water based character but it doesnt feel forced"). Rafi's saturated ocean
  blue read "forced" and "ugly"; blue tattoo ink read wrong. **Tattoo ink is black.**
- Metal is one metal per character. Rafi's is silver (gold rings read as Sean's and as
  "yellow rings").
- Keep rejected palettes buildable for comparison (`RAFI_CLOTH=red|teal|ocean`) and render
  them beside the cast rather than arguing about them.
- The CLAUDE.md 6.4 no-blue rule is for UI chrome, not character models.

## 4 · Markings and tattoos: drawn by hand

Owner: "do the markings manually dont generate it for the whole body with repetitive code",
"dont just copy paste the same white line code to his arm bands", "think abt how to actually
draw it".

- **Every mark is its own hand-set polygon with its own numbers.** No loop, no helper that
  stamps one motif round a limb or down a body. Two cuffs get two different engravings; two
  teeth are never the same size.
- **Research the tradition's STRUCTURE, not just its motifs.** Philippine men's batok is a few
  routes that follow the body, each with its own fill, in one line language: the chaklag
  (nipple, over the pectoral, round the shoulder, ending on the upper arm in line sets), the
  labid (from the foot and ankle up the leg), the dakag (spine and ribs), the gulot (backs of
  the hands), the bangut (face). Plan every route on the body first, give each its own fill
  so nothing repeats, then draw. Rafi v10 to v31 placed motifs and never got there; v32 to
  v35 planned routes and did. The plan belongs in the brief before any coordinates.
- **Cohesion is one line family; variety is one fill per route.** Rails, rungs, stripes and
  scales at fixed weights make the body one hand's work; the two sides are drawn separately.
- **Curves may be laid out on a drafting script** (offset rails along a hand-drawn centreline)
  and pasted as literal polygons; the builder itself runs no generator.
- **Check the canvas is visible from EVERY side.** Rafi's long hair hid his whole back until
  it was gathered into a tied tail; a back tattoo nobody can see is not a back tattoo.
- **Faces:** judge mouths by rendering the options side by side (Rafi went through six). One
  heavy stroke reads as the cast; a stroke with several bends reads as a squiggle; a frown
  reads sad; a small open triangle reads cute. Face marks stay lighter than the eyes and
  mouth, and must not sit where they read as something else (two lines beside the mouth are
  whiskers, a bar under the eye is a tired lid, a vertical line off the eye is a tear).
- **Design against the canvas, not the mesh.** Work out which skin actually shows at game
  distance in the idle pose (for Rafi: shoulders and upper arms, a narrow chest strip; the
  sternum belongs to the necklace). Put the ink there.
- **Big shapes only:** nothing under about 15 mm reads through the outline; use negative
  space (skin laid back over ink, decal layer 3) instead of fine line.
- **Follow the muscle:** caps wrap the deltoids, strokes run along the pectorals.
- **One motif per zone, and never the same motif as an accessory beside it.** Rafi v25 to v27
  put tattoo teeth right under the shark-tooth pendant and the chest read as one motif twice.
- **Double a line to make it read as ink;** a single tapering line next to a cord reads as a
  strap.
- What failed, so it is not tried again: a dozen scattered small motifs (noise), a sun emblem
  (a sticker, then a gear), a flat chest plate (a badge), a teeth row under the chin (a jaw).

The mechanism is `_project_decal` in `tools/build_rafi_voxel.py`: a convex polygon drawn in a
view (front, back, left, right, top) and projected onto one named box, clipped against each
chamfer facet so it wraps the edge with the skin. Polygons are authored in the builder's
table space against the same numbers as the box they sit on.

## 5 · The review loop

- Build: `python tools/build_rafi_voxel.py` (per-hero builder).
- Render through the canonical in-engine pipeline, a **new versioned folder every
  iteration**:

  ```bash
  python tools/run_unity_guarded.py -batchmode -quit -executeMethod TumbangPreso.EditorTools.RafiNativeModelReview.Run -tp-head-study -rig rafi -out Logs/rafi-vNN/rafi-turnaround.png -logFile Logs/rafi-vNN.log
  ```

  It writes the hero lineup (front and three-quarter), the head study, the turnaround, and
  refreshes the roster entry, the first-person arms, the authored clips and the portrait.
  For the whole cast, `HeroTurnaroundProbe.Run` (sized to the roster since 2026-09-25).
- ⚠️ Chain the build and the render so a failed build stops the render (`&&`, not `;`): a
  failed build once let Unity render the previous `.glb` into the new folder.
- Crop close-ups with PIL when a detail is in question; judge at lineup distance too.
- Check motion with `ClipMotionStrip` using the game's action names (`throw`, `slide`), not
  raw clip names.
- Share each meaningful iteration with the owner and take the note; do not wait for approval
  to keep iterating.

## 6 · Delivery

After the model is right: avatar (`tools/build_avatars.py`), the HERO STRIKE poster pose
(`ModeCardPoseAuthor`, `tools/build_mode_cards.py`), `ViewmodelArms.SkinColorForCharacter` if
the skin changed, the design brief updated with the final state, `docs/TODO.md`, then the
usual verification and build (CLAUDE.md section 7).
