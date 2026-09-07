# ASTRA.md

**The animation and Blender queue. This file is Astra's; nobody else works from it.**

Created 2026-09-05 at 🧑's instruction to split animation authoring away from gameplay
engineering. The engineering queue is [`docs/TODO.md`](docs/TODO.md) and it intentionally does
not contain the work below.

---

## Astra Rules

* Read [`CLAUDE.md`](CLAUDE.md), [`docs/VISION.md`](docs/VISION.md),
  [`docs/TODO.md`](docs/TODO.md), and this file first.
* Pull latest `main` before working.
* Animation and Blender only. Rigs, clips, `.glb`/`.fbx` authoring, animation import work, posing,
  and character motion design are yours.
* Gameplay code, networking, UI, balance numbers, and `.cs` engineering belong elsewhere.
* ⚠️⚠️ **EXECUTE ONE UNCHECKED TASK PER SESSION.**
* ⚠️⚠️ **FOR HERO WORK, ONE CHARACTER IS ONE TASK AND ONE SESSION.**
  Finish only that hero's assigned work, verify it, commit and push it, update this file, then STOP.
  Do not start another hero in the same session even if usage remains.
* ⚠️⚠️ **COMMIT EACH FINISHED PIECE AS IT LANDS. DO NOT SAVE EVERYTHING FOR THE END.**
* Mark completion here only after the work is actually shipped.
* Add newly discovered animation/Blender work to this file instead of silently doing extra tasks.

---

## Creative ownership

You are not just a clip executor.

For every hero animation, inspect:

1. the actual model and rig
2. the ability implementation and description
3. the hero's element/theme
4. the hero's silhouette and personality
5. the gameplay timing the animation must fit

Then design the animation yourself.

Use polished hero/action games such as Genshin Impact, Overwatch, Valorant, fighting games, and
stylized action games as a quality bar only. Do not copy any animation frame-for-frame.

Each hero should have a distinct motion language.

Skill 1, Skill 2, and Ultimate should feel related within that hero, with the Ultimate being the
strongest expression of the character.

Avoid six versions of:

* generic hand raise
* arms spread outward
* crouch then explosion
* same pose with different VFX
* identical timing across the roster
* dance-like movement

The animation itself should communicate who the hero is, even with VFX hidden.

---

## What the code already does

[`Assets/TumbangPreso/Runtime/Visual/CharacterAnimator.cs`](Assets/TumbangPreso/Runtime/Visual/CharacterAnimator.cs)
already contains action chains for all eighteen `hero-*` slots and for `slide`.

Your normal deliverable is therefore:

**a correctly named animation action on the existing rig**

Do not edit the chain table unless engineering explicitly asks you to.

The Blender action name becomes the Unity clip name.

A misspelling silently falls back to the stock animation, so always copy clip names exactly.

Do not use `__preview` prefixes. Those are filtered.

The rigs remain on their existing setup. Do not silently retarget or replace rig architecture.

---

## Session workflow

### Step 0: Pick exactly one task

For hero work, choose exactly one named hero task from the queue below.

One hero means that character only.

Do not interpret the whole Hero Cast section as one task.

### Step 1: Use the existing rig

Hero files:

* `Assets/TumbangPreso/Art/characters/persons/team-sean.glb`
* `Assets/TumbangPreso/Art/characters/persons/team-zack.glb`
* `Assets/TumbangPreso/Art/characters/persons/team-dante.glb`
* `Assets/TumbangPreso/Art/characters/persons/team-cheska.glb`
* `Assets/TumbangPreso/Art/characters/persons/team-nemu.glb`
* `Assets/TumbangPreso/Art/characters/persons/team-phaister.glb`

Do not build a replacement rig.

Blender is installed at:

`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`

### Step 2: Name the action exactly

For hero tasks, use only the three exact names listed under that hero.

For retrieval slide:

`slide`

A typo is not a visible error. It simply causes the old fallback to keep playing.

### Step 3: Export over the same `.glb`

Keep the same path and filename so the existing asset GUID remains intact.

Do not create a replacement filename just to hold the new clip.

### Step 4: Run Build Roster Book

After every export, run:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.RosterBookBuilder.Build -logFile Logs/roster.log
```

Menu equivalent:

`Tumbang Preso > Build Roster Book`

This is mandatory because animation clips are `.glb` sub-assets and must be serialized into the
roster so they survive a player build.

### Step 5: Verify resolution

Do not stop at "the clip exists."

Verify that Unity actually resolves the intended action instead of its fallback.

### Step 6: Render motion honestly

Read the current verification guidance in the repo before claiming the animation was visually
verified.

Existing frame-zero probes are not enough to prove motion.

If a motion-strip/frame-strip probe has landed, use it.

If it has not landed:

* do not build the `.cs` probe yourself
* do not claim a bind-pose image verifies motion
* state clearly that integration was checked but the motion itself was not photographed

### Step 7: Commit and push

Commits are sole-authored.

Do not add:

* `Co-Authored-By`
* mentions of AI tooling

Do not add em dashes to repository text.

### Step 8: STOP

After the selected task is finished, pushed, and marked here, stop the session.

---

## Commit policy for limited Plus usage

The usage allowance may end mid-task.

Commit and push work as soon as it becomes valuable.

For a hero session:

* commit/push after Clip 1
* commit/push after Clip 2
* commit/push after Clip 3
* commit/push roster changes after `Build Roster Book`
* commit/push before expensive Unity launches or renders
* commit/push render/review artifacts
* commit/push the final `ASTRA.md` checkbox update

If usage is nearly exhausted, commit and push the current work even if unfinished, explain what
is wrong or what remains, leave the task box unticked, and stop.

A partially finished pushed clip is better than losing the entire session.

---

## What is not yours

Anything requiring a `.cs` edit belongs to engineering.

If you discover one:

1. do not implement it
2. add a numbered handoff item to this file with what is wrong, where it lives, and what done means
3. mention it in your session handoff

Examples:

| Need | Owner |
|---|---|
| New `CharacterAnimator` chain entry | Engineering |
| New first-person arm action lookup | Engineering |
| Network synchronization for a cinematic | Engineering |
| Balance changes to make an animation fit | 🧑 / Attention.md |
| New VFX system | Engineering |

Do not alter balance numbers to make a clip look better.

---

# Queue

## 1. Hero cast animations: ONE CHARACTER AT A TIME

Each hero below is a separate task and separate Astra session.

Choose ONE unchecked hero, finish that hero's three clips, verify them, commit and push them, tick
that hero, then STOP.

Before animating that hero:

* read their abilities
* inspect their model
* define their motion language
* make Skill 1, Skill 2, and Ultimate feel like one character
* make the Ultimate the strongest expression of that identity

### 1A. Sean

- [ ] **SEAN: all three hero casts, then STOP**
  - `hero-sean-dash`
  - `hero-sean-ignite`
  - `hero-sean-supernova`

Suggested motion identity: fiery, forceful, energetic, confident.

### 1B. Zack

- [ ] **ZACK: all three hero casts, then STOP**
  - `hero-zack-sprint`
  - `hero-zack-charge`
  - `hero-zack-summon`

Suggested motion identity: explosive, sharp, electrical, aggressive.

### 1C. Dante

- [ ] **DANTE: all three hero casts, then STOP**
  - `hero-dante-stomp`
  - `hero-dante-roar`
  - `hero-dante-fissure`

Suggested motion identity: heavy, grounded, violent, powerful.

### 1D. Cheska

- [ ] **CHESKA: all three hero casts, then STOP**
  - `hero-cheska-frostwave`
  - `hero-cheska-raise`
  - `hero-cheska-nova`

Suggested motion identity: controlled, elegant, sharp, cold, deliberate.

### 1E. Nemu

- [ ] **NEMU: all three hero casts, then STOP**
  - `hero-nemu-ghoststep`
  - `hero-nemu-project`
  - `hero-nemu-seance`

Suggested motion identity: ghostly, floating, unnatural, playful, unsettling.

### 1F. Phaister

- [ ] **PHAISTER: all three hero casts, then STOP**
  - `hero-phaister-hex`
  - `hero-phaister-blink`
  - `hero-phaister-eclipse`

Suggested motion identity: theatrical, magical, deliberate, witch-like.

---

## 2. Hero ultimate comic-book cinematics: ONE CHARACTER AT A TIME

Each hero ultimate cinematic is also a separate task/session.

Do NOT design all six in one session.

Shared presentation goals:

* short controlled global cinematic freeze
* dramatic hero presentation
* comic-book framing / speed lines / graphic treatment
* hero-specific pose and motion
* clean return to gameplay
* shared-match feel

### Creative ownership

For the selected hero, you own the visual concept and motion design.

Do not wait for 🧑 to storyboard it.

Decide:

* anticipation
* signature silhouette
* body orientation
* weight shift
* head movement
* arm movement
* release pose
* recoil/recovery
* dramatic timing

Ask:

> If all VFX disappeared, would this still unmistakably be this hero's ultimate?

If not, redesign it.

Tasks:

- [ ] **2A. SEAN ultimate cinematic animation/pose, then STOP**
- [ ] **2B. ZACK ultimate cinematic animation/pose, then STOP**
- [ ] **2C. DANTE ultimate cinematic animation/pose, then STOP**
- [ ] **2D. CHESKA ultimate cinematic animation/pose, then STOP**
- [ ] **2E. NEMU ultimate cinematic animation/pose, then STOP**
- [ ] **2F. PHAISTER ultimate cinematic animation/pose, then STOP**

Important existing hooks:

* `TumbangPreso.Hitstop` already owns the bounded global freeze
* `Visual.HitFeel` is victim-specific feedback, not the global freeze
* existing ultimate introductions already exist in the engineering history
* `docs/VISION.md` contains the readability budget

Do not implement network synchronization yourself.

If the visual design requires an engineering hook, document it and hand it back.

---

## 3. Retrieval slide animation

- [ ] **A real retrieval-slide clip replacing the reused lunge**

Verified checkpoint 2026-09-07, second pass: **every rig the game actually ships now
carries `slide`**, and the reach is solved per rig rather than posed once.

**Twenty characters, not twenty-two files.** `Assets/TumbangPreso/Resources/Roster/`
holds 22 `person_*.asset` files and `Roster.People` owns 20 ids, so
`RosterBookBuilder.Fill` never touches `person_berto.asset` or `person_iggy.asset`
and the build logs `20 people`. ⚠️ **Those two orphans are what makes the model list
look longer than it is**: they point at `team-bayan.glb` and `team-iggy.glb`, which no
shipped character uses. This file's list of eight remaining rigs was correct.
`team-bayan.glb` and `team-iggy.glb` were authored anyway and are kept that way, at no
cost: `team-iggy` is the same skeleton as `team-sean` and `team-bayan` the same as
`team-dante`, so leaving them behind would have left a trap for whoever wires one up.
`team-inday.glb` is deliberately NOT authored: it is `build_person_voxel.py`'s output
target and a probe subject, and nothing in the roster loads it.

### What the inspection found, because it changed the clip

`tools/inspect_slide_rig.py` is new and exists for this file's own instruction to
inspect proportions rather than assume the Classic poses fit. Two rigs do not fit.

⚠️⚠️ **THE JOINT ANGLES ARE NOT THE ANIMATION. THE REACH IS.** Where a hand ends
up is the angle times the limb length, and this cast does not share limb lengths.
Right arm length against shoulder height is **1.008 to 1.023 on twenty of the
twenty-two rigs**, so a hanging arm grazes the ground. It is **0.712 on `team-sean`
and `team-iggy`**: a 0.306 arm on a 0.430 shoulder, which is 0.124 short of the floor
standing still. Applied blind, the shared pose left their hand **0.101 above the
ground across the whole contact window**, and this clip is a reach toward a slipper.

So `author_retrieval_slide.py` now bisects one extra torso roll per rig, on the
torso's own pitch envelope so it is full at contact and exactly zero at 0.95 s, until
the hand clears the ground by `REACH_FRACTION` of that rig's own height. **The target
is 6.17 per cent and it is not zero on purpose**: pelvis height is solved from the
lowest skinned vertex, so a hand on the floor becomes the contact point and lifts the
hip off it, which deletes the skid.

| Decision | Why, measured |
|---|---|
| **Roll, not pitch** | Extra torso pitch also brings the hand down and folds the whole silhouette with it: the 20 degrees that reach the ground drop `team-sean`'s head from 74 to **61 per cent** of standing height, so the tall character face-plants while the cast skids. Roll drops the reaching shoulder alone, 74.0 to **73.2** for the same result |
| **Not the arm at all** | The authored -125 degrees is already past the bottom of the arm's arc, so more of it swings the hand back UP. Twenty degrees more made the gap **worse**, 0.104 to 0.181 |
| **Solved over the contact WINDOW, not its deepest beat** | Solving at 0.25 s alone rolled `character-male-b` 23.4 degrees, and its 0.14 s beat was already at target, so the same roll drove THAT beat to zero: the hand became the lowest vertex and the hip stopped skidding a tenth of a second before the pose the solver was watching. `CONTACT` is all three beats now, and male-b needs **0.317** degrees |

**Only four rigs needed a correction**: `team-sean` and `team-iggy` at 17.832 degrees,
`character-female-d` at 2.445, `character-female-c` at 2.195, `character-male-b` at
0.317. **The other eighteen re-exported byte-identical to what shipped on 2026-09-07**,
which is the check that the solver is a correction and not a rewrite.

### Verification that was actually done

* `tools/verify_retrieval_slide.py`: **22 of 22 pass**, each a separate Blender launch
  that re-imports the exported GLB through the glTF importer and measures **deformed
  meshes** at nine times. Floor clearance, body lowering, duration, and now the reach.
  Body drop **13.28 to 29.19 per cent** of each rig's own standing height, reach
  **0.00 to 6.17 per cent**. ⚠️ **This is not a Unity render.**
* ⚠️⚠️ **AND ONE OF ITS BOUNDS WAS WRONG BEFORE IT COULD SAY SO.** The body
  lowering check read `> 0.1` and refused `team-nemu` at **0.0983** for a perfectly
  good slide: that rig stands **0.598** against the cast's 1.000, so an absolute bound
  written from one rig's height refuses a shorter one for being short. Both bounds are
  fractions of the rig's own standing height now. Same family as `docs/VISION.md` § 2's
  footprint rule stated in two units, and `CLAUDE.md` § 6.2c's *"what is this size
  measured AGAINST"*.
* Unity 6000.5.8f1 `RosterBookBuilder.Build`: `[RosterBook] OK. 20 people, 6 cans,
  10 slippers.` **All twenty live roster entries carry the slide reference.**
* `tools/audit_slide_import.py`: **20 of 20**, following each serialized reference into
  Unity's own imported artifact. Every one names `slide`, carries seven rotation
  curves, and `CharacterAnimator`'s body-action chain still picks that name first.
  Evidence: `docs/reports/retrieval-slide-roster-import-v2.json`. No network claim.
* ⚠️⚠️ **THAT AUDIT ALSO RAN ON EXACTLY ONE MACHINE UNTIL TODAY.** It shelled out
  to `rg`, which is not a dependency of this repository: the only copy on this profile
  is inside a VS Code extension folder, so it died with a `WinError 2` before reading
  an artifact, which reads like a broken import rather than a missing binary. The scan
  is python and single-pass now.
* ⚠️⚠️ **AND WHAT IT KEYS ON CHANGED, WHICH IS A FACT ABOUT THE ART.** Two kinds
  of artifact hold the clip: one carries the source asset path, and one carries **only
  the curve bindings**, with no path, no guid in any byte order and not even the file
  name. So the artifact is identified by the hierarchy its curves address, which is the
  better key anyway, because that hierarchy is the exact thing that has to match the rig
  at runtime. ⚠️ **The root name has to be READ from each `.glb` rather than assumed
  from the file name: three team rigs carry another character's name at their root**
  (`team-dante`'s is `team-bayan`, `team-cheska`'s is `team-inday`, `team-sean`'s is
  `team-iggy`), left over from the rig each was branched off. Harmless in the game,
  where every clip in a file addresses that file's own hierarchy. Not harmless to
  anything matching by name: it attributed `team-dante`'s artifact to `team-bayan` and
  then reported `team-dante` as having no imported slide at all.

### Remaining, before checking task 3

* ⚠️⚠️ **NOTHING HAS PHOTOGRAPHED THE MOTION.** `docs/TODO.md` § 151.16 is still
  open and still engineering's: every character probe in the repository samples
  `clip.SampleAnimation(model, 0.0f)`. Blender deformation sampling is evidence that
  the clip moves the mesh; it is not a picture of the clip in this game's camera,
  shader and outline. Do not let the numbers above stand in for one.
* **Nobody has felt it in a match.** Transitions into and out of `slide`, whether the
  0.95 s recovery reads as vulnerable, and whether the reach lands near the tsinelas at
  the moment the pickup fires. No player build was made in this session.
* **The first-person arm is still the lunge**, which is task 4 below and engineering's.
  The body clip cannot reach it.
* ⚠️ `character-female-a` is the one rig whose reach reads **0.000** and whose solve
  is a no-op, and it is not a good number. It carries **28 vertices weighted to the
  right arm reaching 0.451 from the shoulder** against the cast's 0.290, so what the
  measurement calls a hand is an accessory or a sleeve, and that geometry rather than
  the hip is what touches the floor through its skid. Worth an eye before this rig's
  slide is called finished; it is not a reason to hold the other twenty-one.

Task 3 stays unchecked for the first three of those. No hero skill or ultimate task
has been started.

Unity clip name:

`slide`

The gameplay chain and call sites are already wired.

The animation should communicate:

* deliberate forward commitment
* low body position
* reach toward the slipper
* momentum into the pickup
* readable recovery
* urgency without reading as a generic combat dash

This is not simply a lunge.

It is a desperate committed retrieval.

Do not change:

* `SlideActiveTime`
* `SlideRecoveryTime`
* movement values
* slide distance
* pickup range
* gameplay balance

If the animation makes the existing timing feel wrong, document that for human review rather than
retuning the gameplay.

---

## Current engineering context

### 4. First-person retrieval arm still explicitly selects the lunge (engineering)

- [ ] `Assets/TumbangPreso/Runtime/Camera/ViewmodelArms.cs`, `PlayAction`, maps
  `slide` directly to `LungeClip`. Adding the body GLB action cannot replace this
  generated first-person arm motion. The existing camera kick also remains.
  Done means a dedicated low reaching/recovering arm action on the existing
  first-person hook, verified alongside the body's 0.95-second slide. No movement,
  pickup or cooldown retuning. This requires engineering; no `.cs` was edited.


The recent engineering pass preserved the hooks Astra depends on:

* `hero-*` action chains
* `slide` chain
* `HeroHazards.Spawn*`
* `CreateExplosionVisual`
* `MatchFlair`
* `HitFeel`
* camera impact hooks
* audio hooks

Recent engineering also improved:

* spatial audio positioning from the player's listening position
* replicated lata knockdown impact timing
* ability-effects stress measurement

Use those systems rather than designing around imaginary missing infrastructure.

---

## Adding to this queue

Anything animation-shaped discovered while working goes here as a new numbered task with:

* what is wrong
* where it lives
* what done looks like

Do not silently expand the current session.
