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

- **Chibi proportions.** 32 / 29 / 38 legs, torso, head. *"i liek what u made
  actually, it looks cuter"*.
- **Stubby legs.** *"its okay to have stubby legs its part of the personality."*
  Do not "fix" them toward realism.
- **The boxy read.** *"the boxy and everything looks good."*
- **Raised detail everywhere except the face.** Pockets, hem, buckle, laces,
  wristbands, chain links. *"i just want u to fix the details bcz it isnt very
  detailed."*
- **Saturated, warm colour.** *"theyre a bit orange and saturated."*

---

## 5 · Six mistakes, in the order they were made

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
