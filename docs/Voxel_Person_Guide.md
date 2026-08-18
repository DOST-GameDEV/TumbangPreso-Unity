# Authoring the next voxel Person

How to build character number two. Read this before opening
`tools/build_person_voxel.py`, because most of what follows is a list of things that
looked right and were not.

> ⚠️ This is a GUIDE, not a session handoff. It has no "where I left off" in it and
> nothing in it goes stale when the next character lands. Section 1 of `CLAUDE.md`
> bans committed handoff prompts and that ban still stands.

---

## 1 · The five minute version

```bash
python tools/build_person_voxel.py
```

That reads a CC0 rig, keeps its skeleton and all 32 clips, replaces the mesh, and
writes a `.glb` plus a palette `.tres`. Then:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.RosterBookBuilder.Build -logFile Logs/roster.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.PersonSwapProbe.Run -logFile Logs/swap.log
```

⚠️ **The probe has no `-nographics`.** It photographs the result, and with no
rendering device the capture comes back blank and the run still reports success.

It writes three things:

| | |
|---|---|
| `Logs/person-swap-probe.txt` | every assertion, and `RESULT: PASS` or `FAIL` |
| `Logs/person-swap-turnaround.png` | front, three-quarter, side, back |
| `Logs/person-swap-probe.png` | twelve poses, over the rig being replaced |

**Look at the turnaround before you believe anything.** Four of the six mistakes
below passed every assertion and were only visible in a picture.

---

## 2 · Adding a character, concretely

1. **Copy the four box tables and the palette** in `tools/build_person_voxel.py`.
   Everything below the tables is character agnostic.
2. **Set `OUT` and `PALETTE_OUT`.** Name the mesh `team-<id>.glb`. The `team-`
   prefix is what marks a model as ours; `character-*` are the CC0 ones.
3. **Point the roster at it.** `RosterBookBuilder.PersonModels` and
   `PersonPalettes`, both keyed by roster id.
4. **Point the probe at it.** `PersonSwapProbe.NewModel` and `RosterId`, and
   `ModelPreviewTests.ReplacedId` / `ReplacedMesh`.
5. Rebuild the roster book, run the probe, look at the turnaround.

### If the character needs a new name

`Roster.cs`, in the engine-free Core package.

⚠️ **Overwrite a row. Never delete one and never insert one.** `character_index`
crosses the wire as a bare int, so removing an entry shifts everybody after it and
two peers on different builds render different people into the same seat, with
nothing to warn either of them. Overwriting leaves every index where it was.

The tagline lives separately, in `ConvertedCharacterSelect.TaglineFor`. Its one rule
is that **the sentence has to pay out the meters**: if the line says quick, BILIS is
high. A stat nobody can predict from the description is a random modifier.

⚠️ **Change the art or the balance, never both at once.** ZACK kept ATE GIRLIE's
4/3/3 exactly. If the feel changes after a swap you want one candidate, not two.

---

## 3 · What must hold, and what the probe checks

The probe fails the build on all of these. They are here so you know why it is
complaining, not so you can check them by hand.

| Must hold | What breaks if it does not |
|---|---|
| The seven bone NAMES | `arm-right` is hunted by string in two places. A miss is one warning in a match log and a tsinelas hanging in the air |
| Authored height `0.7234` | `PersonScale` is one constant of 2.38 for the whole cast. A taller rig does not get its own scale, it gets that one |
| Hand top at palm + `HandTopLift` (0.0617) | A carried tsinelas is parked there. A chunkier hand buries it |
| Face on the same side as the base rig | `PersonModelYaw` is one constant too. The wrong way round and the character walks, aims and throws backwards |
| UVs in Unity atlas rows 0 to 7 | The shader falls through to the raw atlas and the character wears stock Kenney colours while every name and meter around it stays right |
| Slot 8 stays dark | It draws the eyes and the mouth. A light slot 8 does not give a light-haired character, it gives one with no face |
| Proportions 24 / 23 / 53 | Not probe-checked, and it is the one that got through. Nothing fails; the character simply is not one of the cast. Read the printed line from the build |
| Every emote chain resolves | The wheel opens, the pick registers, the emote replicates to every peer, and the body does not move |

### The bits that are free

- **Bone POSITIONS.** `head`, `arm-left` and `arm-right` are never keyed by any
  clip. `root`, both legs and `torso` are keyed, but only with absolute local
  positions, so shifting the rest and every keyframe by the same vector preserves
  the animation exactly. `tools/glb_anim_channels.py` reports which is which.
  ⚠️ Recompute the inverse bind matrices when you move a bone. Skipping that skins
  the mesh against the old bind pose and the character comes apart the first time
  it moves, with no error.
- **Limb POSITIONS, but not limb LENGTHS.** These limbs have no knee or elbow, so a
  clip's hip rotation sweeps the whole leg and doubling the leg doubles the authored
  stride. Moving a shoulder costs nothing; the arc is identical.

---

## 4 · What he liked

Stated on the build that landed, so start here rather than rediscovering it.

- **THE CAST'S OWN PROPORTIONS: 24 / 23 / 53 legs, torso, head.** ⚠️⚠️ THIS BULLET
  USED TO SAY 32 / 29 / 38 AND IT WAS WRONG, on the strength of *"i liek what u made
  actually, it looks cuter"* said about a character nobody had yet seen standing next
  to the other eleven. The moment one did: *"he doesnt feel like he's part of the
  family"*, *"he looks liek he's from a diff game"*. A 38% head in a line-up of 53%
  heads is not a variation, it is a different toy. **Build to the base rig's numbers.**
- **Stubby legs.** *"its okay to have stubby legs its part of the personality."*
  Do not "fix" them toward realism. This one survived the reproportion — 24% is
  stubbier still.
- **Chamfered, not boxy.** ⚠️ THIS BULLET USED TO SAY *"the boxy and everything looks
  good"* AND THAT ALSO DID NOT SURVIVE CONTACT WITH THE CAST: *"can we make zack a
  little less blocky and more like the original models? he's giving minecraft now"*.
  Every Kenney mini has no 90-degree edge anywhere in its silhouette. See §5.9.
- **Raised detail everywhere except the face.** Pockets, hem, buckle, laces,
  wristbands, chain links. *"i just want u to fix the details bcz it isnt very
  detailed."*
- **Saturated, warm colour.** *"theyre a bit orange and saturated."*

---

## 5 · Nine mistakes, in the order they were made

Every one of these passed the assertions it was checked against at the time.

### 5.1 The palette was dead in the whole port

`Toon.shader` remaps colour by which 32x32 cell of the atlas a UV lands in. The row
test was written against the rows in the `.glb`. glTFast flips V on import, so a
cell authored in file row *r* arrives in Unity row *15 - r*, and every character was
authored in file rows 8 to 15, which is Unity rows 0 to 7. `row >= 8` was never true
for anybody. All twelve people rendered in Kenney's factory colours with sixteen
correct values sitting on the GPU, and nothing logged it, because falling through to
the raw atlas is the deliberate degrade path.

**Fixed on `main` separately.** The lesson that generalises: when a shader comment
describes an atlas layout, ask which side of the importer it was measured on.

### 5.2 The face was on the back of the head

`PersonModelYaw`'s comment says the rig wears its face on -Z. That describes a
different space, and reading it as a claim about the `.glb` put the mop where the
face should be.

⚠️ **Measure it.** `tools/glb_face_side.py` finds the vertices whose UVs land in
slot 8, which on a head is the eyes and the mouth and nothing else, and reports
which side they sit on. The base rigs put them at z **+0.1596**. The toes agree once
they are measured from the leg BONE rather than from the model origin, which is the
error that made -Z look right.

### 5.3 The outline tore itself apart on hard normals

The ink pass is an inverted hull: it pushes every vertex along its normal and draws
the back faces. With one hard normal per face, the eight vertices at a box corner
push six different ways, the hull tears open at every edge, and a border that should
be thick and continuous comes out thin and broken next to eleven rigs that ship
smoothed normals.

**Fix:** average the face normals meeting at each position. The vertices stay split
per face, because a face declares its palette slot through its UV and merging them
would merge their colours. Only the normal is shared. The voxel read is the
silhouette and the flat per-face colour, neither of which normals touch, and the
side effect is that the two lighting bands now fall across a box instead of stopping
at its edges. That is the soft gradient the rest of the cast has.

### 5.4 The face was built out from the head three times

| Attempt | Result |
|---|---|
| Boxes 14 mm proud | Goggles and a beak. *"the eyes and mouth look creepy"* |
| Plates 1.4 mm proud | Fine head on, wrong from the side. *"the side eyes look hella creepy"* |
| A pixel grid replacing the skull's front face | Correct |

The middle one is worth understanding, because it looks like it should work. The
outline hull expands a 3 mm plate into a dark shell 8 mm bigger than the eye in
**every** direction, sideways and forward included. Head on that reads as a border.
From the side it is a black smear hanging off the cheek attached to nothing.

A feature that is part of the head's surface has no hull of its own. The skull is
emitted without its front face and `FACE_PIXELS` is that face. One closed head, one
silhouette, and nothing to z-fight with because the original surface is gone.

⚠️ **This is the one place the raised-detail rule inverts.** Everything else on the
model should stand proud. A face should not.

### 5.5 The hair went to both extremes before it landed

First it wrapped round the sides to a depth in front of the cheeks and swallowed the
face at the three-quarter angle the game actually shows a character from. Correcting
that made it a thin cap, and the head stopped reading at all: *"WTF IS THAT HEAD"*,
with the body called good in the same breath.

The mop is most of this character's silhouette, so the head has to be big enough to
carry it. 38% of height, with the hair going UPWARD into the space between the skull
and the height allowance rather than forward over the face. Sides stop behind the
cheek. The top ends at four different heights, because a mop with one flat top edge
is a helmet.

### 5.6 The dye was buried, then on the wrong side, twice

Authored inside the black mop's own bounds, almost none of it was visible: hair is
opaque, so a coloured box buried in it is a box nobody will ever see. Each streak now
extends a few millimetres past the black box it shares space with, on the face it is
meant to be seen from.

And it was a **bow** for three passes, which is what it looks like in a small render.
In the turnaround it runs through the hair. *"by hair color i want u to put the pink
or other colros in it."*

⚠️ **Never decide a left or right question from a screenshot.** Two transforms sit
between the box table and the pixels, glTFast's X negation and `PersonModelYaw`, and
every attempt to reason through both flipped the answer again. `CheckDyedSide`
compares the mean X of the dyed vertices against the bone NAMED `arm-left`, which is
the character's left arm by definition in whatever space Unity settles on.

### 5.7 The skull kept the wall the face replaces

> ⚠️ **THE MECHANISM THIS DESCRIBES IS GONE.** The skull no longer skips a face at all
> — see §5.10 — so `SKIPPABLE` has no user on this character. It is kept because the
> LESSON is the one that generalises: a fault on a single plane can be invisible from
> the front and shredded from three-quarters, and the assertions cannot see either.


The one that survived longest, because it looked like an art problem and was a table
lookup. `SKIPPABLE` resolves its face names **after** the `FRONT_IS_MINUS_Z` flip, and
that flip negates and swaps each box's z bounds, so the authored front wall comes out as
the box's *upper* z. "front" is `FACES` entry 1 under the flip; the table said 0. The
skull lost its back and kept its front, which put a full skin-coloured quad in exactly
the plane `FACE_PIXELS` draws into.

Two opaque surfaces in one plane is z-fighting, and z-fighting is resolved per fragment
by depth precision rather than by anything stable. **Head on, it landed mostly on the
panel.** That is why the turnaround looked correct, why `PersonSwapProbe` passed, and why
it only showed on the character screen, which views from three-quarters: there, each eye
and the smile tore into triangular shards. *"zach in char select is buggy, his face
specifically is weird af."*

⚠️ Note that `left`/`right` in that table were already swapped and `front`/`back` were
not. It looks like it has been through this correction once before, on the axis somebody
happened to check.

⚠️ **A turnaround is four angles and the game is not.** Two of the six mistakes above and
this one were invisible from the front. If a feature lives on one plane, look at it from
off-axis before believing it.

### 5.9 Every edge was a right angle, and that is the Minecraft read

*"can we make zack a little less blocky and more like the original models? he's giving
minecraft now haha"*.

Correct, and not fixable with colour. This character is built from axis-aligned cuboids,
so every edge in its outline was a hard 90; a Kenney mini has no such edge anywhere.
`bevel_for` now chamfers every box by a fraction of its own smallest half extent, capped,
so a chain link gets a small cut and the torso gets the cap.

Two things make it worth so little geometry:

- **It compounds with `smooth_normals`.** On a plain cuboid three faces meet at 90 and
  the averaged corner normal shades as a hard crease. A chamfer inserts two intermediate
  facets, so the same averaging produces a real gradient round the edge.
- **It closes the outline.** The ink border is an inverted hull pushed along the normal;
  a smoother normal field is a hull that closes instead of tearing at every corner.

⚠️ **A chamfered box is 26 faces, not 6** — 6 octagons, 12 edge quads, 8 corner triangles
— and the winding is SORTED rather than tabulated. A hand-written table for 26 faces in
three different kinds is 26 chances to draw one polygon inside out, which renders as a
hole in the model and not as an error.

### 5.10 The face filled a hole, and the hole is why the head was sharp

*"look his neck and his shharpness of face and features, the other models are rounded"*.

Both halves of that had one cause each and they were adjacent.

**Sharpness.** `bevel_for` refuses to chamfer any box that drops a face, because an
octagonal hole cannot hold a rectangular panel. The skull was the only box on the
character that dropped one — so after §5.9 it was the ONLY box left with hard corners,
and it is the largest and most looked-at mass on the model. The chamfer pass made the
head *relatively* sharper than it had been.

The fix removes the reason for the hole. The skull keeps its front wall, `FACE_PIXELS`
draws **only its ink cells** a fraction of a millimetre proud of that wall
(`PANEL_PROUD`), and there is nothing to z-fight with and nothing to fill. As a bonus the
panel got 10x cheaper: every non-ink cell used to be a skin quad drawn on skin.

⚠️ **THE FEATURES ARE RASTERISED, NOT TYPED.** A hand-written ASCII grid cannot be fine
enough to hide its own stair steps, and every other person in this game wears eyes painted
into a 512x512 atlas — smooth curves, antialiased. A typed 16x12 grid gave six-pixel
blocks and a smile made of two straight runs: *"the face look weird"*, *"the face is not
as smooth and sharp"*. `_face_rows()` draws two ellipses and an ARC of a circle into a
32x24 grid instead. Only ink cells cost geometry, so the resolution is nearly free.

⚠️ **The eyes are taller than wide and set far apart, and the mouth is an arc.** Two
straight runs meeting at a corner is not a smile; the cast's curve opens from a centre
that sits between the eyes.

⚠️ **`BEVEL_MAX` is what the head hits.** Only the largest boxes reach the cap, and those
are the ones whose silhouette IS the character. At 0.030 the skull was cut by 18% of its
smallest half extent — enough to round a chain link, invisible on a head 335 mm across —
and the jaw still read as a corner: *"the face itself as well is too sharp, look chin and
stuff"*. 0.045 leaves it fraction-limited like everything else.

**Neck.** The first family pass held the neck out of the head's XZ growth, reasoning that
the cast has essentially no neck. True, and the conclusion was backwards: holding it at
its authored half-width while the skull grew 1.37x left a 50 mm post carrying a 335 mm
head. The cast does not have a NARROW neck, it has NO neck — the head sits straight on
the shoulders. The neck grows with the head now and the skull reaches down to the collar,
which is what actually hides it.

### 5.11 The proportions were signed off before anyone saw the line-up

The big one, and the reason §4's first bullet is now a correction rather than a
preference. See §4. The mechanism is worth repeating here because it is reusable:

**The tables are remapped, not rewritten.** Every box carries a measurement and a reason.
Re-authoring 86 of them by hand against new joint heights loses all of it and is wrong in
ways only a turnaround catches. `_family` is a three-segment piecewise remap of Y — legs,
torso, head, each onto its band — plus a uniform XZ growth for the head. Each REGION
moves onto its family value and every relationship inside a region survives untouched.

⚠️ **An arm is TRANSLATED, never squashed.** Its Y extent is thickness, not length, so
putting it through the torso's scale gives the character thinner arms than the cast it is
joining. Move it to the new shoulder and leave every dimension alone; the hand's height
against `HandTopLift` then survives by construction, because the box is authored as
shoulder ± that constant and a pure translation preserves the identity.

⚠️ **`verify()` has to check the REMAPPED tables against the REMAPPED skeleton.**
Comparing the authored table to the new joint heights measures a distance that exists in
neither the file nor the model. It fired on `hair-fringe` for a box that had not moved
relative to its own bone at all. Its reach bound is per bone too, because a crown box is
legitimately 0.38 from the head joint once the head is 53% of the figure.

### 5.12 One selection bug took the jaw, and the wrong thing got blamed for it

*"the jaw is gone and the face is so buggy now, i just asked it to change the face to make
it a bit edgy or nonchhalant then it broke"*.

The expression change moved the donor's own mouth triangles in Y, which is right. It picked
them with a **height test and nothing else**, run over every triangle in `head-mesh`, which
is not: everything below y 0.45 is the jaw, the chin, both earlobes and the collar. That is
70 triangles and 129 of the head's 375 vertices where the mouth is 8 and 10. All of it was
crushed to 22% of its distance from the mouth's centre and then tilted by its own x, so at
the ear's 227 mm one side of the head lifted 23 mm and the other dropped 23 mm.

**The selection is by SLOT first and height second.** Slot 8 is the ink, which on a head is
the eyes and the mouth and nothing else. That narrows 221 triangles to 20 before the height
test splits those into the two features, and it is the only test here that knows what a
mouth *is*: a height alone cannot, because the jaw is at the same height.

⚠️ **And it cost a build somewhere else entirely.** The jaw went missing in the same hour
that `character-male-d`'s slot 13 was first dropped, so the drop was blamed, reverted, and
written up as *"dropping one took the jaw and half the ears with it"*. Measured instead of
remembered: slot 15 alone spans y 0.3432 to 0.6613 at |x| 0.2268, so it carries the jaw AND
both ears on its own. The note was wrong, and re-adding slot 13 is what left the hair with
nowhere to sit for another build. **When two changes land in the same hour and one thing
breaks, measure which one did it.**

⚠️ **The build refuses to write now if the selection has widened**, and the guard is worth
copying rather than the fix. `_verify_expression` diffs the vertex array across the edit and
stops on any vertex that moved without being picked, on any moved vertex outside slot 8, and
on any change at all to the skull's bounds. An expression bug does not throw, does not fail
an assert and does not corrupt the file: it produces a valid mesh that is wrong, and the only
thing that catches it otherwise is a human looking at a render.

### 5.13 There is no such thing as a thin line on this character

*"change the expression just a bit to edgy or nonchalant"*, then twice: *"bro look at ur
render u broke the face hahah"* and *"the facial expression doesnt look nonchalant or smug or
edgy anymore too"*.

`ToonSkin.PersonOutlineWidth` is `0.008 * 2.38`, and the 2.38 is `PersonScale`. In the model
space the generator authors in, the inverted hull stands **8 mm off every ink polygon**, 16 mm
across a gap. So flattening the donated smile to a 7 mm stroke drew a 16 mm halo around a 7 mm
shape: the halo closed the curve back up and rendered the original smile with its middle
filled in. Two passes were spent making the line thinner, which is the direction that makes
this worse.

Separating the bow from the stroke by least squares and scaling only the bow was the third
attempt and it did not work either: *"look at the mouth he is smiling ts aint edgy"*. The
donated mouth is a **filled bowl**, an open grin with its interior inked, and no affine bend of
a filled bowl is a smirk. Flattening thins the stroke until the shape stops existing; tilting
swings the bowl without opening it, and it inflates the shape's own height twice over. The last
bend measured **51.9 mm tall against an eye of 27.1 mm**. A mouth twice the size of an eye
reads as a grin whatever its curve is doing.

**The mouth is drawn, and the thing that makes that safe is that the face is FLAT.** Every ink
vertex on this donor sits at z 0.1596 exactly, eyes and mouth alike: it is an inset plate, not
a patch of a curved ovoid. The note that started three passes of bending said otherwise, and it
was true of BOXES, which have depth and corners; it says nothing about a polygon lying in that
plane. So `_mouth_polygon` authors a tapered stroke on that plate, `PANEL_PROUD` keeps it off
the skin, the donor's eight mouth triangles are deleted outright, and `_compact` drops the
vertices nothing references any more so the file can still be measured.

⚠️ **Delete-and-draw, not move**, which is why `_verify_expression` runs before the mouth is
touched: the guard exists to catch a MOVE that reached too far, and the mouth is no longer a
move.

⚠️ **The eyes carry more of the expression than the mouth does.** The donated eyes are tall
rounded pupils set wide apart, which is the open, friendly read the whole CC0 cast shares, and
no mouth under it says nonchalant on its own. Half lidding them does, and it is the cheapest
expression change on a face this size: the outline swallows anything subtle in the mouth, but
an eye at 55% of its height is 55% of its height at 90 px too.

⚠️ **Each eye is squashed toward its OWN centre, and the two are split by sign of x.** The rig
is symmetric, so the two clusters interleave exactly in y; a positional split puts half of each
eye in the other one's group.

⚠️ **`tools/face_mouth_sheet.py --sweep` draws these candidates WITH the halo**, at the
turnaround's size and at the 90 px a head occupies in play. Anything that only reads in the
first of those does not exist in the game, and both earlier attempts were in that category.

### 5.14 The rig was bald because his hair was painted on as skin

*"yea hair doesnt loom good still"*, *"pls imrpove"*, then on the tidy donated cap that
followed: *"that hair looks ugly hjaahah"*, *"not as good as old hair"*, *"maybe redo the
entire hair"*.

Four passes of hand-built mop, and the reason none of them worked was underneath them. Every
rig in this set is built from two shells:

|  |  |
|---|---|
| the skull, on all twelve | y 0.3432 to 0.6613, \|x\| 0.2268 (the ears) |
| the hair, where there is any | y 0.3932 to 0.6713 and up, \|x\| 0.1700 |

`character-male-d` was chosen as the skull donor *because he is bare-headed*, and that reading
was exactly backwards. He is not missing the second shell, he **wears it in skin**: his slot 13
runs y 0.3932 to 0.7218, which is the hair volume, painted as a bald pate. So the mop was being
built on top of a skull that already filled the space the mop needed, with the rig ceiling 8 mm
above it. What came out was a 20 mm cap floating over a forehead, and no amount of moving those
boxes was going to fix it.

Dropping slot 13 is half the fix and it is the half that was blocking everything else.
`_donor_part` takes a rig, a set of slots and an optional repaint, so a skull can come off one
rig and a hair shell off another at 1:1, because they are the same shell to four decimal places.

⚠️⚠️ **The donated hair shell then shipped, and it came back with a name on the fault:**
*"the hairs sucks shiiit why does it have bangs"*. Every hair shell in this set is a **scalloped
fringe across the brow**, because that is the haircut those characters have, and a box laid over
the scallops only replaces them with a straight cut. This character's reference has no fringe at
all. So the mop is boxes again and the donation stops at the skull.

⚠️ **That is not a reason to keep hair out of `_donor_part`.** The next character who wants a
fringe should take one: the mechanism is right, this character's haircut is just not in the set.

⚠️⚠️ **And boxes are only right if they are VOLUME ON TOP OVER TRIMMED SIDES.** Growing the mass
outward all round gives *"pic 2 makes it look like an afro which it isnt"*. In the reference the
sides sit close to the skull and everything the silhouette does happens above the temple: 26 mm
of clearance, not 46, four lumps and all of them on the crown. Lumps scattered down the sides
read as damage rather than as texture, which is what *"make it look cleaner"* was about.

⚠️ **The crimson has to reach the side faces.** With the lock only on the crown and the front,
the side view was the one angle with no crimson in it, and that is the angle a player gets most
of somebody running past them.

### 5.15 `PersonScale` is one constant, and that is not a height constraint

The generator's `verify()` and `PersonSwapProbe.CheckHeight` both refused any authored height
more than a few millimetres off `character-female-b`'s 0.7234, reasoning that `PersonScale` is
a single 2.38 for the whole cast so a taller rig "walks the arena at the wrong size".

The constant is real. The conclusion from it was not. Measured across the twelve CC0 rigs the
port ships:

| | | | |
|---|---|---|---|
| male-b 0.6613 | male-a 0.6713 | male-e 0.6760 | female-e 0.7165 |
| female-f 0.6713 | male-f 0.6713 | male-d 0.7218 | female-b 0.7234 |
| female-a 0.7755 | female-c 0.7755 | female-d 0.7755 | male-c 0.7928 |

They span 132 mm, a fifth of the shortest, and every one of them takes the same 2.38.
`CharacterVisual.AlignToCapsuleFloor` re-measures the *scaled* bounds and drops the feet onto
the capsule floor, so a taller rig stands taller with its feet in the right place. 0.7234 was
one member of that range.

⚠️ **The difference between the two ends of that range is hair.** A bald rig is 0.66 and a rig
with a mop is 0.78. Pinning to the base rig while the donated skull already reached 0.7218 left
under 2 mm for hair, which is §5.14 with a number on it. Both checks bound the cast's range now,
widened by 5 mm at each end, and both carry the table.

⚠️ **A transcribed constant is a measurement of one thing presented as a law.** Two of the three
faults above are the same mistake: 0.7234 was female-b's height, and "slot 13 is his hair" was
one session's guess. Both were written down as facts and cost a build each.

### 5.8 The portrait wore a border 2.38x too thick

`ModelPreview` multiplied `PersonOutlineWidth` by `PreviewScale`. That constant is
already a *world* width and already carries the 2.38 — `CharacterVisual` warns against
exactly this multiply at its own call site — so the character screen drew a 45 mm ink
border against 19 mm everywhere else.

It reads as a face bug rather than an outline one, because the face is where a fat
inverted hull shows first. Worth knowing which of the two you are looking at: **turn the
outline off before blaming the model.** Passing `0.0f` to `ToonSkin.Apply` for one run is
what separated this from 5.7, which was still there underneath it.

---

## 6 · Rules of thumb

- **Depth, not paint.** *"make sure u dnt just paste the texture."* A detail flush
  with what it sits on is a colour change the palette flattens away. Standing 12 mm
  off it, it gets its own ink outline for free. Except the face.
- **Big and few.** A head is roughly 90 px tall in play. A 20 mm feature is two
  pixels and reads as dirt.
- **Nothing pure black.** The shader shades in two flat bands, and a near-black base
  has nowhere to go, so both bands land on the same colour and every face renders
  identically. Cloth sits around 17% luminance. Hair can go to 8%, because at 17% it
  reads as a charcoal wig.
- **Do not author for the raw hex.** `ColourGrade` runs an ACES curve with a warm
  tint over the composited frame, so mid-tones arrive warmer and more saturated than
  the hex suggests. Authoring to the hex bakes the grade in from the wrong end.
- **One silhouette cue per character.** At arena distance neither the face nor the
  jacket detail survives. On ZACK it is the crimson dye.
- **Slots 9, 10 and 11 are spare.** They keep the stock Kenney values, so the shader
  has something sane to read if a future box uses them.

---

## 7 · The other tools

| | |
|---|---|
| `tools/glb_dump.py` | nodes, skins, meshes, animations |
| `tools/glb_mesh_dump.py` | UV atlas cells and per-bone slot use |
| `tools/glb_bone_bounds.py` | bind positions and per-bone vertex bounds |
| `tools/glb_face_side.py` | which way a rig faces, off its own eyes |
| `tools/glb_anim_channels.py` | which bones the clips key, so you know what is free to move |
| `tools/preview_person.py` | a z-buffered turnaround of a built .glb in a second, without Unity |
| `tools/face_mouth_sheet.py` | the face's ink triangles WITH the outline, at play size. See 5.13 |

⚠️ `preview_person.py` is a **drafting** tool and the three things it does not model are the
three that have bitten this file: the ink outline, `ColourGrade`'s ACES curve, and the clips.
Shape it there, then run `PersonSwapProbe`.

---

## 8 · Before you call it done

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode -testResults Logs/tests.xml -logFile Logs/tests.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testResults Logs/play.xml -logFile Logs/play.log
```

⚠️ **PlayMode has no `-nographics`**, and adding it crashes the editor rather than
the tests: the run dies about 360 lines in, writes no `.xml`, and still exits 0.

⚠️ **Assert on the `.xml`, never on the exit code.** Both a crash and a genuine
failure come back as 0.

⚠️ **PlayMode timing tests are sensitive to machine load.** A stamina test failed at
2.96 s against an expected 1.5 s during this work and passed clean once Genshin was
closed. If a timing test fails on its own while everything else passes, check what
else is running before you go looking in the code.
