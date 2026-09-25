# Rafi islander rework, 2026-09-25

Owner, 2026-09-25: Rafi "looks so bad", research islanders from the two supplied
references (a painted Visayan datu in the Lapu-Lapu tradition, and Maui from Moana),
build him the same way as the other hero builders, "dante has the most advanced one",
and "make sure whatever u make genuinely looks like it still". The owner's two images
are `owner-reference-datu.png` and `owner-reference-maui.png` in this folder.

**Owner follow-up, same day:** a generated concept sheet (front, three-quarter, side,
back) offered as inspiration only, "YOU ARE ALLOED TO CHANGE IT HOWEVER U LIKE". It shows
long straight hair falling down the back, a red headband knotted at the back with two
tails, a tooth necklace on a gold cord, gold ear pieces, and dense banded tattoos on the
chest and both arms. Rejected in it: the face ("it should be fierce", not cute) and the
leaf skirt over a red flap ("looks weeird"). Kept from it: the hair length, the knot at
the back, the tooth necklace, the arm bands. The sheet itself was shared in chat only.

This supersedes the 2026-09-22 brief's exclusion of "tribal patterns" and "invented
ethnic markings": the owner's own references are a tattooed islander, so body
tattoos are now the brief. The builder rules that were owner rulings, not a previous
session's caution, still hold: block-built hair, no eyebrows, native donor skull and
flat graphic face, family proportions (legs 24%, torso 23%, head 53%), the seven bone
names, no weapon or armour, his own builder only.

## 1. What the research says an islander of this tradition looks like

Visayan men of the sixteenth century were called Pintados by the Spanish, "the painted
ones", for their tattoos (batok). What the sources agree on:

| Part | Name | What it looks like |
|---|---|---|
| Chest to throat | dubdub | worn "like breast plates"; paired geometric designs on both pectorals, read by scholars as solar imagery |
| Shoulder | ablay | a cap of pattern on the shoulder |
| Arms | dayadaya (tagur on Panay) | lines running the length of the arm, serpentine or zigzag |
| Legs | labid | an inch-wide line, straight or zigzagging, from the ankle up to the waist; the first tattoo a man received |
| Back | | only after further deeds, so many men had a plainer back |

Dress was minimal because the tattoos were the display: a bahag (loincloth wrapped at
the waist with a hanging flap, colour showing status), a putong (headcloth; a headband
for commoners, a turban for others; red marked a proven warrior), and gold: neck
chains and collars, calombiga armlets with incised patterns, ear ornaments, leglets.
Hair was worn long, from the shoulder to the waist, oiled, often under the headcloth.

Maui is the useful lesson in stylisation rather than in dress: a "man-mountain"
caricature, top-heavy and broad, his tattoos treated as flat graphic design laid over
simple volumes, and his hair a single big mass that is most of his silhouette.

Rafi's lore (docs/BADJAO_EXPANSION.md) makes him a seafarer from Tawi-Tawi. The Sama
sources found no tattoo tradition to cite; they do describe bold rectangular woven
headcloths (destar) in bright dyes. The pintados look is Visayan, so it is presented
here as the islander hero the owner referenced, and the lore line is flagged to the
owner rather than rewritten by this pass.

Sources: Boxer Codex summaries (VERA Files, Prehispanic Cebu, detailedpedia Batok),
Wikipedia "Fashion and clothing in the Philippines", Moana production interviews
(Hollywood Reporter, That's It LA).

## 2. What makes the current model fail

v8/v9 (`docs/reports/ui-hud-review-2026-09-23/evidence/rafi-v8-*`): a kid in a green
work shirt, navy belt and a thin headband. Nothing in the silhouette says island,
sea or hero; the shirt is the largest shape and it is the plainest one; a small
orange float is his only accent. Beside Dante's horn and robe or Sean's flame and vest
he reads as a Classic extra.

## 3. The design

Read at game distance, in order: a mass of long black hair with a knotted headcloth
and trailing tails; a bare bronze torso with a dark tattooed breastplate band; a gold
neck collar; a loincloth in his signature colour; tattooed arms and legs with gold at
the arm and wrist.

- **Silhouette.** A V torso inside the family proportions: chest wider than the waist
  (taper), thicker bare arms (Dante's 0.12 section), a shallow pectoral plane for the
  chest tattoo to sit on. Bigger hair volume than anyone except Phaister's hat.
- **Hair.** Long, stepped, block built: a full crown with Maui-style volume above the
  band, sides falling past the ears to the jaw, the back falling in three staggered
  tiers to the top of the shoulders. Two locks spill over the band at the front so the
  hair frames the face (the v7 correction). Tips taper; no loft, no curve.
- **Putong.** A thick red band worn round the outside of the hair, knotted at the back
  with two tails hanging down over the hair (the concept sheet's back view). Red because a
  red putong marked a proven warrior; it has its own palette slot.
- **Face: fierce.** Native skull, no brows, so the eye's own top edge does a scowling
  brow's job: it drops 26 mm toward the nose over a 42 mm eye, with a flat hard bottom and
  one glint. The mouth is a one-sided snarl over clenched teeth, one of them gold (the
  sources describe gold pegs set in high-status men's teeth). No facial tattoo.
- **Tattoos (projected decals on the chamfered boxes, dark indigo ink).** Each part is
  designed for its place, not stamped (Art_Direction owner rule of 2026-09-14):
  - chest: a collarbone band with a row of hanging triangles (the painting's
    breastplate), a pair of lines down the sternum to the navel, ribs plain;
  - shoulders: a small ablay cap, different on each side;
  - right arm: a zigzag line down the outer arm; left arm: a straight line with a
    triangle border; forearms plainer;
  - legs: one labid band up the outside of each shin and thigh, zigzag;
  - back: a single pair of lines down the spine, otherwise plain.
- **Gold.** A chunky neck collar, a calombiga armlet on each upper arm, cuffs at both
  wrists, small ear ornaments.
- **Bahag.** A wrapped waist band with a woven border stripe, a front flap to mid thigh
  and a shorter back flap, a knot at the hip.
- **Feet.** Bare feet on a chunky sole with a single strap: a tsinelas, which in this
  game is exactly right, and keeps the cast's chunky-sole grounding.
- **Colour.** The putong is red in every variant. The bahag is the open choice: Sean
  already owns red with gold on bronze skin (red vest, red trunks, gold trim, bare arms),
  so a red bahag as well risks making Rafi his sibling in the lineup, while Rafi's kit is
  water and his UI accent a sea teal. `RAFI_CLOTH=teal` (default) or `red` builds either;
  both are rendered beside the cast for the owner to pick. Tattoo ink is a dark indigo.

## 4. Construction (how, in tools/build_rafi_voxel.py)

Everything Dante's builder does, plus what it lacks:

1. Chamfered boxes, rigidly skinned per bone, family remap, donor skull (unchanged).
2. **Projected decals**, new: a decal is a convex polygon drawn in a view plane
   (front, back, left, right, top) and projected onto one named box. Each chamfer
   facet the projection hits is clipped against it (Sutherland-Hodgman) and the piece
   lifted onto that facet's plane. Dante's decals are pinned to hard-coded arm planes
   (0.055, 0.310) and stop at the chamfer; these wrap over it, and work on any box.
3. Tube and loft forms (existing) for the collar, armlets and headband.
4. Tapers and tilts (existing) for the V torso, hair tips, cloth flaps and putong tails.

## 5. Delivery and checks

Archive the v9 builder and glb in `ArtSource/rafi/rejected-green-shirt-v9-20260925/`.
Build, then render through the canonical pipeline (`RafiNativeModelReview.Run`: hero
lineup front and quarter, turnaround, head study) with versioned folders
`Logs/rafi-v10`, `v11`... and iterate until it holds up beside the cast. Then refresh
his roster entry and FPP arms (`RosterBookBuilder.RefreshPerson`), rebake his three
authored clips if the feet moved (`RafiMotionAuthor`), and rebuild the portrait,
avatar and HERO STRIKE poster. Checks: height inside the cast range, feet on zero,
every box within its bone's reach, `PersonSwapProbe` hand anchor, the Rafi tests, a
motion strip of a throw so the flaps and hair are seen moving.

## 6. What shipped (v31) and how it got there

The plan above was the starting point; the owner's notes during the session moved it.
The model at v31 (superseded in its tattoo, face and back hair by v35, section 7):

- **Head:** a curly mane (a core ringed by curls graded into a dome), long hair down the
  back in three staggered tiers, a sea-teal putong tipped high at the brow and knotted at
  the back with two tails, silver ear drops. Ink-only fierce face: slanted eyes, a heavy
  scowl hooked at one corner.
- **Body:** bare, "a bit" muscular (pectoral slabs 10 mm proud, deltoids just fuller than
  the arm), a muted sea-teal bahag with a sand and cream woven stripe, cream-soled tsinelas.
- **Tattoo (black, every shape hand-set):** shoulder caps with three big teeth on each
  deltoid, a doubled V down the chest that frames the necklace, a band on each upper arm
  (skin diamonds on his left, a skin zigzag on his right). Legs and back bare.
- **Metal (silver):** a chain laid on the collarbones and chest carrying a shark tooth and two
  smaller teeth, and a cuff on each wrist engraved differently.

| Versions | Change | Owner note that drove it |
|---|---|---|
| v10 to v11 | islander base: long hair, putong, tattoos, gold, bahag | the brief |
| v12 to v13 | ink-only face; bolder ink | "everyone else has just black eyes and just black mouth" |
| v14 to v17 | band sits in the hair, curls, all-red cloth | "keep improving it" |
| v18 to v21 | ocean blue, blue ink | "too much like sean give it its own colors" |
| v22 | muted sea teal, black ink, no arm gold | "ocean blue ugly", "should be subtle", Harbor |
| v23 to v24 | sun emblem, half sleeve | "i hate the markings" |
| v25 to v26 | muscle as blocks; silver cuffs | "a bit muscular", "arm bands should be silver" |
| v27 | shark tooth necklace | "give him a shark tooth necklace" |
| v28 to v29 | tattoo remade from the canvas up, by hand; muscle toned | "think abt how to actually draw it", "too muscular" |
| v30 to v31 | the chain laid on the body | "its js floating" |
| v32 to v33 | tattoo researched and planned as routes: chaklag, labid, dakag, gulot, bangut; foot markings | "do proper research", "thoroughly plan", "cohesive and not repetitive", "foot markings" |
| v34 | long hair gathered into a tied tail so the back tattoo shows | "does his back have markings too" |
| v34 to v35 | six mouths compared side by side; the single hard bar kept; war slashes off the eyes | "especially the facial expression", "extend to his face" |
| v36 | baseline islander rework committed at 8efb0522 | "rework Rafi as a tattooed islander hero" |
| v37 | bold batok upgrade: collar solar burst, interlocking rung ladders, lower pectoral gin-ginnam sawteeth, deltoid Ablay caps, forearm serpent ladders, leg Labid | "reallly try to improve tattoos" |
| v38 | 5-tier winged back armor (Dakag) flanking spine ladder, connected thigh-to-shin routes, pre-cuff wrist chevrons, bold pectoral sweep | "improve construction, silhouette, proportion, detail quality and appeal" |
| v39 to v40 | continuous abdominal batok tiers connecting chest to bahag waistband; facial markings removed; nonchalant calm eyes and relaxed mouth | "those markings suck its also weird theres none that continues to here", "make him look nonchalant" |
| v41 | necklace chain clearance & pendant stand-off (>18 mm proud, 0 body clipping); 360-degree lateral rib & flank batok connecting front abs to back wings | "REFINE MARKINGS EVEN MORE AND THE NECKLACE AS WELL COZ IT ENTER HIS BODY" |
| v42 | subtle forehead Init solar crest diamond & warrior accents; 6-tier bold trapezius/scapular back armor (Dakag) framing hair tail | "refine tattoos more try to give him subtle forehead markings", "does he have back markings" |
| v43 | elevated front Dubdub collar sawteeth, Chaklag pectoral gin-ginnam sawteeth, 4-point navel star, and 3 dense interlocking Inagdan abdominal tiers | "make the markings even better as well as the markings on the front" |
| v44 | cleared forehead marks, smoothed chunky lock masses for hair crown, bold high-contrast graphic warrior plates with generous negative space across chest, back, arms, shins | "hmm thoroughly think of diff way to do his marks, pls refine wtv the fuck is on his head, it looks bad.." |

The method this produced is written up for every future character in
[docs/CHARACTER_MODEL_METHOD.md](../../../docs/CHARACTER_MODEL_METHOD.md).

## 7. The tattoo plan (v32), researched and planned before drawing

Owner, of v31: "still a bit ugly in some parts especially the facial expression and
markings", "give him foot markings too", "do proper research on tribal markings",
"does his back have markings too", "make some of that tattoos extend to his face",
"thoroughly plan how to make tattoos", "it has to be cohesive throughout the body and
not repetitive".

**What the research changed.** Philippine men's batok is not a set of motifs placed
around a body; it is a few ROUTES that follow the body, each with its own fill, all in one
line language (Wikipedia "Batok"; Lars Krutak on Kalinga batok; Aswang Project; Prehispanic
Cebu on the Boxer Codex):

| Route | Tradition | Path on the body | Fill motif |
|---|---|---|---|
| chaklag | Bontoc, Ifugao chest piece of a proven warrior | starts at the nipple, runs up over the pectoral, curves round the shoulder, ends on the upper arm in "two or three horizontal line sets" | centipede body (gayaman) / ladder (inar-archan): two rails with rungs |
| labid | Visayan, the first tattoo a man received | from the ankle up the leg to the waist, an inch wide | python or crocodile scales: triangles alternating between two rails |
| dakag | Kalinga back | a vertical pattern down the spine, flanked by bars following the ribs | a ladder spine, rib bars |
| gulot | Kalinga hands and wrists, "cutter of the head" | stripes across the back of each hand | plain stripes |
| bangut | Visayan face, elite warriors | marks on the chin and cheeks (a full "crocodile jaw" mask would bury the face) | short heavy lines |

**Cohesion.** One ink (black), one line family: rails 7 to 8 mm, rungs and stripes 4 to
6 mm, solid anchors (the nipple diamond) larger; every field bounded by rails, so the
body reads as one hand's work. **Not repetitive:** each route has its own fill, so no
motif appears in two places, and the two sides of the body are drawn separately with
their own small differences. The necklace owns the sternum; the ink frames it.

**Face.** The expression is judged separately: three mouth options are built behind a
switch and rendered side by side, and the fiercest one that still reads as this cast's
ink face is kept. The bangut marks are thinner than the eyes and mouth, so the eyes and
mouth still read first.
