# ASTRA.md

**The animation and Blender queue. This file is Astra's; nobody else works from it.**

Created 2026-09-05, at 🧑's instruction, to split animation authoring away from gameplay
engineering. The engineering queue is [`docs/TODO.md`](docs/TODO.md) and it does **not** contain
the work below, on purpose: two people writing the same layer is worse than nobody writing it,
which is the rule `docs/TODO.md`'s queue already states about controller support.

---

## Astra Rules

* **Read [`CLAUDE.md`](CLAUDE.md), [`docs/VISION.md`](docs/VISION.md),
  [`docs/TODO.md`](docs/TODO.md), and this file first.** The summaries in this file are not the
  rules; they are pointers to them.
* **Pull latest `main` before working.**
* **Animation and Blender work only.** Rigs, clips, `.glb`/`.fbx` authoring, import settings for
  the clips you land. Gameplay code, networking, UI, VFX systems and balance numbers belong to
  somebody else.
* ⚠️⚠️ **EXECUTE ONE UNCHECKED TASK PER SESSION. DO NOT AUTOMATICALLY CONTINUE TO THE NEXT ONE.**
  Usage allowance here is limited and a session that runs the whole queue spends it on the tasks
  nobody has reviewed yet. One task, finished properly, beats three started.
* **Test it.** § "How to verify a clip" below is the whole procedure; it is a render, not a
  description.
* ⚠️⚠️ **COMMIT EACH FINISHED PIECE AS IT LANDS. DO NOT SAVE IT ALL FOR THE END OF THE SESSION.**
  🧑 2026-09-06: *"ask astra to commit each important shit one at a time bcz i dont have much
  usage on it (only on plus plan)"*. **The usage runs out mid-task, and when it does, whatever is
  not committed is gone.** One task per session is still the rule; several commits inside that
  task is the point. § "When to commit" below is the list.
* **Mark completion here**: tick the box, and add what you actually shipped under the task.
* ⚠️ **STOP AFTER THAT TASK.** Even if the next one looks like ten minutes.

⚠️ **ADD NEW ANIMATION AND BLENDER TASKS TO THIS FILE INSTEAD OF DOING THEM.** That is what keeps
the one-task rule honest.

---

## What the code already does for you, so you do not have to touch it

⚠️⚠️ **THE CLIP NAMES ARE ALREADY WIRED AND WAITING. A CLIP LANDS BY NAME WITH NO CODE CHANGE.**

[`Assets/TumbangPreso/Runtime/Visual/CharacterAnimator.cs`](Assets/TumbangPreso/Runtime/Visual/CharacterAnimator.cs)
holds a table of **chains**: each gameplay action names a list of clips and `Play` walks it until
it finds one the rig actually carries. Its own note, at the head of the hero block:

> ⚠️⚠️ **EVERY `hero-*` CLIP IN THE FIRST SLOT IS ASPIRATIONAL AND NONE OF THEM EXISTS TODAY,
> WHICH MAKES THE SECOND SLOT THE ONE THAT SHIPS.** The CC0 rig carries exactly 43 named clips
> and not one is a hero cast. [...] **THE FIRST SLOT STAYS.** When the team's own cast animations
> land they drop in by name with no code change, which is the entire reason these are chains.

**So your deliverable is a clip with the right name, on the existing rig.** No table edit, no call
site, no gameplay code. If you believe a task needs a code change, say so in the task instead of
making it.

⚠️ **THE RIGS IMPORT AS GENERIC, NOT HUMANOID, AND THAT IS DELIBERATE** (`CLAUDE.md` § 6): the
current clips ship with their own rig and humanoid retargeting would re-solve poses that are
already correct. ⚠️ **If your clips start coming from a library (Mixamo or similar), Humanoid
becomes the right answer and `ModelImportSetup` has to be revisited.** Raise it here; do not flip
it quietly.

⚠️⚠️ **AND DO NOT GO LOOKING FOR THAT SETTING IN THE INSPECTOR, BECAUSE THERE IS NO RIG TAB ON
THESE FILES.** Unity has no native glTF support, so **glTFast's ScriptedImporter owns every
`.glb` here and `ModelImporter` does not** — the paragraph above states an INTENT that is true of
how the clips were authored, not a checkbox anybody set. Step 3 below has the whole of it,
including the silent no-op `ModelImportSetup`'s own header records from the last time somebody
assumed otherwise.

---

## How a session runs, end to end

⚠️⚠️ **READ THIS WHOLE SECTION BEFORE OPENING BLENDER.** Five of the eight steps below are the
ones that make a clip actually reach a player, and **four of the five fail silently**: the
character simply stands still, or plays the old fallback, with no error anywhere. Every warning
here is a fault that has already happened in this repository and is recorded in the file it
happened in.

### Step 0 · Pick ONE task and say which

`ASTRA.md`'s rules: one unchecked task per session, finished, rendered, committed, ticked. For
task 1 that means **one hero, three clips** rather than eighteen clips badly.

### Step 1 · Find the rig you are animating, and do not build a new one

| | |
|---|---|
| The twelve street characters | `Assets/TumbangPreso/Art/characters/persons/character-{male,female}-{a..f}.glb` |
| The six heroes | `Assets/TumbangPreso/Art/characters/persons/team-{sean,zack,dante,cheska,nemu,phaister}.glb` |
| The pets | `Assets/TumbangPreso/Art/characters/pets/` |

Each `.glb` carries **its own skeleton and about 32 clips already**. ⚠️⚠️ **YOU ARE ADDING AN
ACTION TO AN EXISTING RIG, NOT RETARGETING ONTO IT.** `CLAUDE.md` § 6: the clips ship with their
own rig and are authored against it, which is why the import is Generic rather than Humanoid.
**If your clips ever start coming from a library (Mixamo or similar), stop and say so here** —
Humanoid becomes the right answer and `ModelImportSetup` has to be revisited. Do not flip it
quietly.

⚠️ **BLENDER IS `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`** and it is not on
PATH. `tools/build_slipper_models.py` is the shape the repo already uses for a headless run:
`blender -b --factory-startup --python tools/<script>.py -- <args>`.

### Step 2 · Name the ACTION exactly the clip name. The name is the entire contract

`CharacterAnimator.ResolveChain` walks a chain of names and plays **the first one the rig actually
carries**. glTF exports each action as a clip named after the action, so **the Blender action name
IS the Unity clip name.**

* Task 1: `hero-sean-dash`, `hero-zack-summon`, and the other sixteen, spelled exactly as the
  table in the queue below spells them.
* Task 3: `slide`.

⚠️⚠️ **A MISSPELLED NAME IS NOT AN ERROR, IT IS A SILENT FALLBACK.** `ResolveChain` *"returns
null rather than a guess when nothing matches"*, and every one of these chains has a stock clip
in its second slot, so `hero-sean-dashh` produces exactly what shipping nothing produces: the old
animation, no warning, no log line. **Check the name against the table by copy and paste, not by
eye.**

⚠️ **`__preview`-prefixed clips are filtered out** by `CacheClips` and by `RosterBookBuilder`.
Do not name anything that way.

### Step 3 · Export the `.glb` back over the same path

Same filename, same folder. The `.glb.meta` beside it holds the asset GUID that every roster
entry, prefab and scene points at; **a new filename is a new GUID and breaks all of them.**

⚠️ **NOTHING IN THE UNITY IMPORT INSPECTOR NEEDS SETTING, AND THERE IS NO RIG TAB TO SET.**
Unity has no native glTF support, so **glTFast's ScriptedImporter owns these files, not
`ModelImporter`** — `ModelImportSetup`'s own header records an earlier version that cast to
`ModelImporter`, got null for every character, skipped them all and logged *"fixed 0 character
models"* as though they were already correct. glTFast already emits the Animator the animation
layer needs (with a **null controller**, which is correct for Playables). ⚠️ **Do not "fix" the
null controller by authoring an AnimatorController.** That is exactly what the Playables graph
exists to avoid.

### Step 4 · ⚠️⚠️ RUN `Build Roster Book`, OR THE CLIP DOES NOT SHIP

**This is the step that is not obvious, and skipping it produces a clip that works perfectly in
the editor and does not exist in the built game.**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.RosterBookBuilder.Build -logFile Logs/roster.log
```

(The menu item is **Tumbang Preso > Build Roster Book**.)

⚠️⚠️ **WHY, IN `RosterEntryAsset.Clips`' OWN WORDS:** the clips are sub-assets of the `.glb`, and
*"an asset nothing points at is stripped from the player. Nothing pointed at them: the animator
looked them up through the AssetDatabase, which does not exist in a build, so **every character in
every build stood still**."* `RosterBookBuilder` sweeps every clip out of each `.glb` into
`RosterEntryAsset.Clips`, which is a real serialised reference, and that is what both makes them
ship and makes them findable.

⚠️ **IT ALSO FAILS LOUDLY IF THE EXPORT WENT WRONG**, which is the cheapest check you have:
*"'<id>' has a model with no clips. That character will not animate."*

### Step 5 · Verify it resolves, before you look at it

⚠️ **The question is not "does the clip exist", it is "does `PlayAction` reach it".** The chain,
the roster book and the rig all have to agree. `RosterBookBuilder`'s log line is the first half;
a render of the pose is the second.

### Step 6 · Render it. Show, do not describe

See the section below. ⚠️ **A clip change with no picture attached cannot be reviewed and is not
finished.**

### Step 7 · Commit, tick the box here, and say what shipped

`CLAUDE.md` § 3: sole-authored, **no `Co-Authored-By` trailer of any kind**, no mention of any AI
tooling anywhere in the repository, and **no em dashes**. Comment the WHY at length. Push.

⚠️ **THIS IS THE LAST COMMIT OF THE SESSION, NOT THE ONLY ONE.** See § "When to commit" below;
by the time you reach this step the clip should already be in git.

---

### Step 8 · Stop

Even if the next task looks like ten minutes. The rule is at the top of this file and it exists
because usage here is limited.

---

## When to commit, and why it is not once at the end

⚠️⚠️ **THE USAGE ALLOWANCE RUNS OUT MID-TASK AND EVERYTHING UNCOMMITTED GOES WITH IT.** 🧑
2026-09-06: *"ask astra to commit each important shit one at a time bcz i dont have much usage on
it (only on plus plan)"*. This is not a git-hygiene preference. **A session that dies with an
exported `.glb` sitting uncommitted in the working tree has produced nothing**, and the next
session starts from the same blank Blender file.

**Commit at every one of these, separately:**

| Commit when | Because |
|---|---|
| **one clip is exported and reimported**, before you render it | A clip on disk and in git is work that survives. A clip on disk only is not. ⚠️ **If the task is a hero, that is three commits, one per clip** — not one commit for the hero |
| **`Build Roster Book` has run and reported clips** | It rewrites `Resources/Roster/*.asset`, which are real files. Committing the `.glb` and not the roster entry ships the § Step 4 fault into the repository rather than merely into your session |
| **before any Unity launch or render** | A batchmode launch is minutes and is where the allowance goes. ⚠️ **Nothing you have already made should be at risk while you wait for a picture** |
| **the render is taken** | The PNG is the review. Commit it with the clip it is of, versioned filename and all (`CLAUDE.md` § 6.1) |
| **the task is ticked here** | The `ASTRA.md` edit is part of the work, not paperwork after it |

⚠️ **PUSH EACH ONE.** `CLAUDE.md` § 2.2: *"FINISHED MEANS PUSHED. Committed and waiting is not
done."* On a laptop that may not come back to this task for a week, a local commit is only half
of the protection.

⚠️⚠️ **AND COMMIT WORK IN PROGRESS RATHER THAN LOSING IT, IF YOU ARE ABOUT TO RUN OUT.** A clip
that is half right, committed, with a commit message saying exactly what is wrong with it and what
you were going to do next, is worth far more than a clean tree. **Say so in the message and leave
the box here unticked**; the next session reads the message and carries on. The one thing that
must never happen is a session ending with something good in the working tree and nothing in git.

⚠️ **A COMMIT IS CHEAP AND A LOST SESSION IS NOT.** Do not batch them to keep the log tidy. The
log being tidy has never once been worth an afternoon of animation.

## How to verify a clip

⚠️⚠️ **SHOW, DO NOT DESCRIBE** (`CLAUDE.md` § 6.1). A model or animation change with no render
attached cannot be judged.

* Render through the **in-engine probe pipeline**, never an external renderer: the toon shader,
  the ink outline and Unity's linear colour conversion are the look. **A Blender viewport render
  is not a picture of this game.**
* ⚠️⚠️ **VERSION THE FILENAME EVERY TIME** (`zack_ult_v1.png`, `zack_ult_v2.png`). Chat clients
  cache by filename, so overwriting a render conducts the whole review against an image that is
  no longer on disk.
* **Force-reimport sub-assets before rendering**, or you photograph geometry that is no longer
  there.
* `docs/CANONICAL_RENDERING_PIPELINE.md` has the commands and five recorded pitfalls. ⚠️ It is
  written for another tool and its "MANDATE FOR ALL AGENTS" heading is that tool's; where it
  disagrees with `CLAUDE.md`, `CLAUDE.md` wins.

⚠️⚠️ **AND HERE IS THE HONEST STATE OF THE TOOLING, BECAUSE THE SENTENCE ABOVE OVERSOLD IT UNTIL
2026-09-06: EVERY EXISTING CHARACTER PROBE PHOTOGRAPHS FRAME ZERO.** `HeroTurnaroundProbe` and
`PersonSwapProbe` both call `clip.SampleAnimation(model, 0.0f)`, so what they render is **the
first pose of a clip and nothing else**. That is the right tool for "is this the right character,
wearing the right thing, at the right scale" and it is **useless for judging an animation**, which
is a thing that happens over time.

| What you need to show | What to use |
|---|---|
| the character is intact, scaled and skinned | `Tumbang Preso > Probe All Heroes Turnaround`, `Probe Person Swap`, `Shoot Model Sheet` |
| **the motion** | ⚠️ **A strip of frames across the clip.** `docs/TODO.md` § 151.16 is the engineering item for a probe that takes one; **until it lands, say in your handoff that the motion was not photographed** rather than attaching a frame-0 render and calling it verified |
| the effect a cast draws | `Tumbang Preso > Capture Ability Showcase` (`AbilityShowcaseProbe`), and ⚠️ it **fails a run where one effect blows more than 12 per cent of the frame to white**. `docs/VISION.md` § 2 rule 5 |

⚠️ **DO NOT BUILD THE FRAME-STRIP PROBE YOURSELF.** It is a `.cs` file, which is the line drawn
below. Ask for it; it is written up and small.

---

---

## What is NOT yours, and what to hand back instead

⚠️⚠️ **THE LINE IS: A CLIP AND ITS RIG ARE YOURS. ANYTHING THAT NEEDS A `.cs` EDIT IS NOT.**
When you hit one, write it into this file as a new numbered task **with what is wrong, where it
lives and what done looks like**, and say so in your handoff. Do not make the edit.

| If you find you need | It belongs to |
|---|---|
| a new entry in `CharacterAnimator`'s chain table | engineering. The eighteen `hero-*` names and `slide` are **already** in it; a NINETEENTH name is a code change |
| a first-person ARM clip | ⚠️⚠️ engineering, and this is the sharp one. `ViewmodelArms.PlayAction` is a **flat lookup with no chain**, unlike the body's, so a name it does not know resolves to `null` and the hand simply does not move. `docs/TODO.md` § 151.13a is that exact fault costing the retrieval slide its arm for a day. **A viewmodel clip needs a new field in that file. Ask for it.** |
| a balance number changed to fit a clip | 🧑. `Attention.md`, never `Balance.cs` |
| "every player sees the same cinematic" | engineering. `NetSession.ProtocolVersion` may not be moved by animation work |
| a new VFX system | engineering, and read `docs/VISION.md` § 2 first |

---

## Queue

### 1. Hero cast animations: FIRST PRIORITY

- [ ] **Real hero-specific combat casts for the eighteen existing `hero-*` slots.**

Create real hero-specific combat cast animations for the existing `hero-*` animation slots instead
of the generic stock fallbacks they resolve to today.

Each hero should have a **distinct motion language**, with **Skill 1 → Skill 2 → Ultimate
increasing in intensity**.

⚠️⚠️ **THIS IS NOT DANCING.** These are combat casts.

**The eighteen slot names, exactly as `CharacterAnimator` will look them up**, with the stock clip
each one falls back to today so you can see what is being replaced:

| Hero | Skill 1 | Skill 2 | Ultimate |
|---|---|---|---|
| **SEAN** | `hero-sean-dash` *(→ attack-kick-right)* | `hero-sean-ignite` *(→ attack-melee-right)* | `hero-sean-supernova` *(→ jump)* |
| **ZACK** | `hero-zack-sprint` *(→ sprint)* | `hero-zack-charge` *(→ emote-no)* | `hero-zack-summon` *(→ holding-both-shoot)* |
| **DANTE** | `hero-dante-stomp` *(→ pick-up)* | `hero-dante-roar` *(→ attack-melee-left)* | `hero-dante-fissure` *(→ attack-kick-left)* |
| **CHESKA** | `hero-cheska-frostwave` *(→ interact-right)* | `hero-cheska-raise` *(→ pick-up)* | `hero-cheska-nova` *(→ holding-left-shoot)* |
| **NEMU** | `hero-nemu-ghoststep` *(→ sprint)* | `hero-nemu-project` *(→ interact-left)* | `hero-nemu-seance` *(→ emote-yes)* |
| **PHAISTER** | `hero-phaister-hex` *(→ interact-right)* | `hero-phaister-blink` *(→ attack-kick-right)* | `hero-phaister-eclipse` *(→ crouch)* |

⚠️ **The fallbacks were chosen for the MOTION and not to fill a row** (`CharacterAnimator`'s note):
all six ultimates used to be one nod of the head, `emote-yes`, and 🧑 named it: *"make the
animations appropriate for skills and what theyre doing btw dont js spam the same animation"*.
**That is the bar your replacements have to clear, per hero rather than per table.**

⚠️⚠️ **READ THE ABILITY BEFORE ANIMATING IT.** `docs/Hero_Strike_Balance.md` § 1 has what each one
does and its cast time; a cast whose animation is longer than its cast time reads as lag. The
ability kits are in `Assets/TumbangPreso/Runtime/Abilities/`.

⚠️ **ONE HERO IS A REASONABLE SESSION.** Three clips, rendered, committed, ticked.
⚠️⚠️ **AND THAT IS THREE COMMITS, ONE PER CLIP, NOT ONE COMMIT FOR THE HERO.** See § "When to
commit": if the allowance runs out after the second clip, two clips should be in git and the
third should be a message saying what was left. If you take that route, tick the hero here and
leave the box above open:

- [ ] SEAN &nbsp;&nbsp; - [ ] ZACK &nbsp;&nbsp; - [ ] DANTE &nbsp;&nbsp; - [ ] CHESKA &nbsp;&nbsp; - [ ] NEMU &nbsp;&nbsp; - [ ] PHAISTER

---

### 2. Hero ultimate comic-book cinematics

- [ ] **Design the shared-match ultimate presentation, visually.**

Design the shared-match ultimate presentation system visually:

* short controlled global cinematic freeze
* dramatic hero presentation
* comic-book framing / speed lines / graphic treatment
* hero-specific pose and effects
* return cleanly to gameplay
* **every player experiences the same event**

⚠️⚠️ **COORDINATE WITH THE EXISTING GAMEPLAY AND NETWORK HOOKS RATHER THAN REDESIGNING
AUTHORITY.** The hooks you need already exist and each one already carries the reasoning for why
it is shaped the way it is. Read all four before designing anything:

| Hook | What it already is |
|---|---|
| `TumbangPreso.Hitstop` | ⚠️ **The global freeze, and it already exists.** It scales `Time.timeScale` for one bounded beat on the frame an ultimate detonates, felt by the whole match. Its header records a probe that ran a 120x slowdown for a whole match because an orphaned timer never restored the scale. **This is the "controlled global cinematic freeze"; do not write a second one.** |
| `Visual.HitFeel` | The per-VICTIM confirm, and explicitly **not** the global one. Its header explains why the two are both correct and not duplicates. |
| § 134.7, "THE ULTIMATE INTRODUCTIONS" | Six ultimate introductions are **already built and captured**. Read `docs/TODO.md` § 134.7 before designing a seventh thing that introduces an ultimate. |
| `docs/VISION.md` § 2 | The readability budget, and it is a hard number. `AbilityShowcaseProbe` **fails a run where an effect blows more than 12 per cent of the frame to white**. Zack's Thunderstrike once read 62.8 per cent. A comic-book treatment is exactly the kind of thing that trips this. |

⚠️ **"EVERY PLAYER EXPERIENCES THE SAME EVENT" IS A NETWORK CLAIM AND IT IS NOT YOURS TO
IMPLEMENT.** Design it, say what it needs, and hand the wire half back into `docs/TODO.md`.
`NetSession.ProtocolVersion` may not be moved by animation work.

⚠️ **DELIVERABLE IS A DESIGN PLUS THE HERO POSES**, not a rewrite of the ability system.

---

### 3. Retrieval slide animation

- [ ] **A real retrieval-slide clip, replacing the reused lunge.**

Replace the reused lunge clip with a proper retrieval-slide animation:

* low body position
* forward commitment
* hand/reach toward slipper
* readable recovery
* ⚠️ **preserve current gameplay balance and timings unless integration genuinely requires
  otherwise**

**Where it comes from:** `docs/TODO.md` § 146 built Classic's committed retrieval slide, and
§ 146.6 is this row: *"The retrieval slide plays the LUNGE clip, because both are a body-led dash
and the rig has one. A slide of its own, which is art work rather than code work."*

**The name to author against is `slide`.** ⚠️ The chain and the call site are **already wired**
by the engineering side, so a clip named `slide` on the rig replaces the fallback with no code
change, exactly like the `hero-*` slots above.

**The timings the clip has to fit, from `Core.Balance` (do not change them):**

* `SlideActiveTime`, the dash itself
* `SlideRecoveryTime`, the commitment, and **the recovery IS the punishment**
  (§ 146.3: it is commitment, not a status effect)

⚠️⚠️ **THE FEEL OF THOSE NUMBERS IS NOT YOURS AND IS NOT SETTLED.** `Attention.md` § 17.2 holds
🧑's own test: *"I can safely approach and pick this up normally, OR I can commit"*. **Nobody has
felt it yet.** If your clip makes the recovery read as longer or shorter than it is, say so here
rather than retuning the constant.

---

## What the 2026-09-06 engineering pass changed underneath you, and what it did NOT

⚠️⚠️ **NOTHING IN THAT PASS REPLACED A HOOK WITH A HARDCODED EFFECT, AND THAT WAS AN EXPLICIT
CONSTRAINT ON IT RATHER THAN LUCK.** `docs/TODO.md` § 151 is the record. **Every API this file
depends on is untouched**: the `hero-*` and `viewmodel` action chains in `CharacterAnimator`, the
`HeroHazards.Spawn*` and `CreateExplosionVisual` surfaces, `MatchFlair`, `HitFeel`, the camera
hooks (`ImpactPunch`, `Shake`, `HoldFrame`, `ViewmodelKick`) and the audio hooks. Task 3's
`"slide"` action name is still a name with no clip behind it, falling through to the lunge exactly
as § 150.8 left it.

**Three things did change and each makes a task here easier rather than harder:**

- ⚠️ **The audio is spatial from the player's head now** (§ 151.1). A cast, an impact or a
  footstep you time an animation against is heard from where the camera is, so **an animation's
  timing against its sound is now judgeable in a build.** Before, both were computed from the
  middle of the map and a cue landing "on the left" told you nothing about the clip.
- ⚠️ **The lata knockdown's impact frame reaches every peer** (§ 151.3). If you animate anything
  around the tumba, all four screens now hold the same 0.045 s beat, so a clip cut to it lands the
  same everywhere instead of only on the host's.
- ⚠️⚠️ **`AbilityStressProbe` is a frame-time and object-count measurement of the whole effects
  pile** (§ 151.8, `Logs/ability-stress.txt`). **Use it when you make an effect prettier.** It
  reports a quiet arm, a first cast and a WARM cast, and the warm one is the number that says
  whether what you added costs anything in play. ⚠️ It also asserts that nothing is left behind
  after two piles' lifetimes, so an effect that forgets to clean itself up fails there rather than
  becoming *"the game gets slower the longer you play"* at the venue.

---

## Adding to this queue

⚠️ **Anything animation-shaped that you find while working goes here as a new numbered task, with
what is wrong, where it lives, and what done looks like.** Same shape as the three above. That is
`CLAUDE.md` § 2.3's rule for `docs/TODO.md` applied to this file: a task noticed and not written
down is a task rediscovered from scratch in three weeks.
