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
* **Commit it.**
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

### How to verify a clip

⚠️⚠️ **SHOW, DO NOT DESCRIBE** (`CLAUDE.md` § 6.1). A model or animation change with no render
attached cannot be judged.

* Render through the **in-engine probe pipeline**, never an external renderer: the toon shader,
  the ink outline and Unity's linear colour conversion are the look.
* ⚠️⚠️ **VERSION THE FILENAME EVERY TIME** (`zack_ult_v1.png`, `zack_ult_v2.png`). Chat clients
  cache by filename, so overwriting a render conducts the whole review against an image that is
  no longer on disk.
* **Force-reimport sub-assets before rendering**, or you photograph geometry that is no longer
  there.
* `docs/CANONICAL_RENDERING_PIPELINE.md` has the commands and five recorded pitfalls. ⚠️ It is
  written for another tool and its "MANDATE FOR ALL AGENTS" heading is that tool's; where it
  disagrees with `CLAUDE.md`, `CLAUDE.md` wins.

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

⚠️ **ONE HERO IS A REASONABLE SESSION.** Three clips, rendered, committed, ticked. If you take
that route, tick the hero here and leave the box above open:

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

## Adding to this queue

⚠️ **Anything animation-shaped that you find while working goes here as a new numbered task, with
what is wrong, where it lives, and what done looks like.** Same shape as the three above. That is
`CLAUDE.md` § 2.3's rule for `docs/TODO.md` applied to this file: a task noticed and not written
down is a task rediscovered from scratch in three weeks.
