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
