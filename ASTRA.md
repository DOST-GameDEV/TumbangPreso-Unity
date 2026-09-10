# ASTRA.md

**Current continuation state:** the owner requested a wrap-up and thorough chat
handoff, including unfinished maps. Kuro's matching blocky purple forms,18-bone
giant loop and eleven idle clips (including :3/XoX/shy-pout gestures) are pushed
at864cead. The approved people are retained. Broader animation/FPP/skill acceptance
remains open; read docs/ACTIVE_REWORK_LEDGER.md and docs/MAP_FINAL_PASS.md before
resuming, rather than restarting the rejected model studies recorded below.

**Model style is fixed by the game:** use
[Art_Direction.md section0](docs/Art_Direction.md#0--new-models-must-belong-to-tump)
for every Blender/model deliverable. New assets must look native to TUMP's cute
blocky cast, without excessive detail or realistic anatomy. Requests for maximum
animation realism concern weight, timing, contact and recovery inside that style.
External reference games and generated concepts do not override it.

**Animation scope includes casting and VFX:** author distinct whole-action
sequences for different skills/actions, including body, FPP, moving effect geometry,
release, impact and recovery. Do not repeat generic animations unless the same
action or a shared contract makes reuse necessary. Record that reason. See
Art_Direction.md section0.1; body-clip-only polish does not satisfy this brief.

**Animation and Blender history plus the current improvement pass.**

2026-09-10 authority: the owner explicitly expanded this work to the whole game on
`ASTRAReworks` only, without agents or one-character stopping limits. Never use
`main`. [IMPROVEMENT_PLAN.md](docs/IMPROVEMENT_PLAN.md) owns the active outcome table.
All eighteen named character models and their first-person arm geometry are now
reworked; forty gait clips and all eighteen hero body/FPP actions have current
in-engine evidence in [the presentation report](docs/reports/improvement-2026-09-10/presentation-batch.md).
The full-speed player and multiplayer acceptance checks remain open. These captures
are automated observations and do not claim the owner's taste approval.

Created 2026-09-05 at 🧑's instruction to split animation authoring away from gameplay
engineering. The engineering queue is [`docs/TODO.md`](docs/TODO.md) and it intentionally does
not contain the work below.

---

## Historical Astra Rules (superseded where they conflict with the authority above)

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

⚠️⚠️ **THE MOTION STRIP HAS LANDED, 2026-09-09. USE IT.** `docs/TODO.md` § 151.16 is closed and
`docs/CANONICAL_RENDERING_PIPELINE.md` step 3b has the full command and every argument:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.ClipMotionStrip.Run -rig sean -clip slide -logFile Logs/motion.log
```

It writes two auto-versioned strips to `Logs/motion/` (side and three-quarter) plus a text
report. **Read the report as well as the picture**, because three of the things a review has to
catch are not visible in either strip:

* **which clip the GAME resolved.** The report says the slot. `CharacterAnimator`'s chains are
  fallback chains, so a rig missing `slide` plays the lunge, looks like a dash, and logs
  nothing. **Slot 0 or you are reviewing the wrong animation.**
* **the peak bone speed and when.** The trace is sampled far finer than the strip, so a snap
  between two photographed poses still shows up with a time attached.
* **the distance between the last frame and `idle` frame 0**, which is whether it comes back.

The strip draws the floor at world y = 0 and darkens the band beneath it, and every pose
carries its lowest deformed vertex and its reaching-hand height in metres. **Negative is
through the street.**

Existing frame-zero probes are still not enough to prove motion: `HeroTurnaroundProbe`,
`PersonSwapProbe` and the lineup all sample `0.0f`. Do not claim a bind-pose image, a cast
lineup or a Blender deformation sample verifies a clip.

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

**Authored 2026-09-07. Left unchecked for the same single reason as task 3: nothing in
the repository can photograph an animation** (`docs/TODO.md` § 151.16). Everything else
below is done, verified and pushed.

⚠️⚠️ **THEY WERE NOT MISSING, AND THAT IS THE REASON FOR AUTHORING THEM.**
`Runtime/Visual/HeroAbilityClips.cs` builds all fifteen procedurally and its timing
section is good work. **But `docs/TODO_Archive.md` § 80.8 records, still open, that it
fills non-legacy clips with `AnimationClip.SetCurve` at runtime**, which is the same
editor-versus-player API that made the dance a T-pose in every build: in a player it
returns a valid clip with ZERO curves, and a valid empty clip is the bind pose. § 80.6
fixed that for the dance by baking it; the casts were left because baking meant 342
curve assets across 18 hierarchies. **An authored action needs neither.** It ships as a
`.glb` sub-asset the roster already serialises, and `CharacterAnimator
.BuildGeneratedClips` registers a procedural clip only when the name is still free, so
an authored one wins with no `.cs` change. `docs/TODO.md` § 151.19 is the entry.

**Rigs: `team-sean.glb`, `team-custom.glb` and `team-custom-base.glb`.** A custom
character borrows a WHOLE hero kit (`docs/VISION.md` § 6, one `HeroKitId` field), so a
saved character running Sean's kit casts `hero-sean-dash` on a custom rig and would
otherwise have fallen through the chain to a kick.

**The timings are the procedural ones and were deliberately not redesigned**, because
they are derived from the abilities: 0.55 s against Flame Rush's 0.6 s `Duration`,
0.45 s for Ignition Cannon because its effect happens LATER and only the chambering is
visible, and 1.00 s against Supernova's 0.4 s `UltimateWindup`, 0.55 s `_airTimer` and
0.85 s `_impactTimeout`. Retuning them would be changing gameplay to suit an animation.
**What is authored is the pose.**

**The motion language, which is the thing the three share:** he thrusts from the hips
along ONE axis and stops dead. A coil, one explosive extension, a braced arrest, a
settle. ⚠️ **His arms rake BACK behind the line of travel rather than reaching along
it**, which is what makes him read as propelled rather than as swinging. Flame Rush
sends that forward, Ignition Cannon spends it through one arm with the body counter
rotating, and Supernova turns it on its end, up and then inverted. With every effect
hidden: a launch, a round being chambered, and the launch again, bigger, coming down.

⚠️⚠️ **THIS RIG CANNOT CROUCH, AND THAT CHANGED HOW ALL THREE ARE BUILT.** There are
no knees, so lowering the hips lowers the feet, and the shoe extends FORWARD of the
ankle, so swinging a leg backward rotates the toe DOWN. The first pass put the trailing
toe **0.100 below the road** at the sprinter's set and **0.142 under** at the ultimate's
landing. ⚠️ **The procedural clips carry a smaller version of the same thing**:
`BuildSeanDash` keys `localPosition.y` to -0.04 with the legs still at rest. So the
crouch is spent on the SPINE and the root height is SOLVED: a per-beat `contact` term
places the lowest skinned vertex exactly on the road wherever the body is standing on
it, and hands back the authored lift only where it is genuinely airborne, which is the
apex and the hang.

**Verified, nine clips, one Blender re-import each** (`tools/verify_hero_action.py`):
name, duration, impact, return to rest and floor clearance.

| Clip | Peak speed | After one frame | Silhouette |
|---|---|---|---|
| `hero-sean-dash` | **1956 deg/s**, exactly on the 0.25 impact | 5.9 per cent | forward extent 0.20 to 0.50 |
| `hero-sean-ignite` | **1847 deg/s** on 0.28 | 2.3 per cent | he RISES onto the strike rather than sinking |
| `hero-sean-supernova` | **3293 deg/s** on 0.65 | 3.0 per cent | height 0.53 to 1.02 against a 0.85 standing height, feet 0.16 clear of the road |

**The ultimate carries the largest peak speed, the largest height range and the largest
forward extent of the three**, which is this file's own test for it.

⚠️⚠️ **AND THE IMPACT CHECK HAD TO BE REWRITTEN, BECAUSE THE FIRST VERSION ASSERTED
THE WRONG THING.** It demanded the punch frame be the FURTHEST pose from rest, and
Supernova failed it correctly: that clip's furthest pose is the apex, both arms 158
degrees overhead, while the impact is a 30 degree arm and a 64 degree fold. **A strike
is not the biggest pose. It is the fastest arrival followed by a stop.** The check is
frame-to-frame speed now.

⚠️ **The diagnosis of that failure found a second measurement bug worth the same
note.** `Quaternion.rotation_difference().angle` does NOT take the shortest path: it
returns `2 * acos(w)` over the full circle, so a three degree change across the hang
read as **357**, and the verifier announced a 21,429 deg/s frame in the quietest part of
the clip. The exported samples were checked pair by pair and carry no sign flip at all.
The angle is folded now.

**Still open for Sean:** the motion photograph (§ 151.16), a person feeling the
transitions in a match, and the first-person `viewmodelAction`s (`thrust-fire`,
`ignite`, `supernova-slam`), which are engineering's hook and not this rig's.

### 1B. Zack

- [ ] **ZACK: all three hero casts, then STOP**
  - `hero-zack-sprint`
  - `hero-zack-charge`
  - `hero-zack-summon`

Suggested motion identity: explosive, sharp, electrical, aggressive.

**Authored 2026-09-07, on `team-zack.glb`, `team-custom.glb` and
`team-custom-base.glb`. Unchecked for the one reason every task here is unchecked:
nothing can photograph an animation** (`docs/TODO.md` § 151.16). Same argument as
Sean for why they were authored at all: `docs/TODO.md` § 151.19.

⚠️⚠️ **ZACK IS BUILT AGAINST SEAN RATHER THAN BESIDE HIM.** 🧑, 2026-09-02, looking
at the two kits: *"the kit of zack and sean are the exact fricking same"*. That was
about the ABILITIES and `ZackHeroKit` records the split it caused. **It applies twice
over to the animation**, because two heroes who move the same way are one hero with two
colour ramps however different the payloads are.

| | Sean | Zack |
|---|---|---|
| **Shape** | one axis, symmetric, thrust from the hips | bladed and side-on, halves opposing: torso twists against the hips, arms counter-swing |
| **Rhythm** | coil, extension, dead stop, settle | snap, then an electrical chatter instead of a settle |
| **The ultimate** | symmetric and VERTICAL, because he IS the meteor | asymmetric and DIRECTIONAL, because Thunderstrike is aimed up to 7 m away and he is pointing at it |
| **Feet** | Supernova leaps, 0.160 clear of the road | the call lifts him onto his toes, 0.074 |
| **Peak speed** | 3293 deg/s | **4677 deg/s** |

⚠️ **TWO OF THE THREE CARRY NO IMPACT, AND THE ABSENCE IS INHERITED FROM
`BuildZackSprint`'S OWN ARGUMENT, WHICH IS RIGHT:** *"Bolt Sprint is LOCOMOTION, not a
strike: it is a skating cycle held for the whole dash, and there is no instant at which
anything lands. Snapping a cycle to a stop would read as the animation breaking."* The
tool takes `punch: None` now, and `tools/verify_hero_action.py` drops its impact
assertions for exactly the clips that declare no impact, keeping the two that apply to
any cast at all: it has to MOVE, and it has to END WHERE IT STARTED.

⚠️ **THE LEGS TAKE A ROLL AS WELL AS A PITCH NOW, AND ZACK IS WHY.** A pitch alone
swings a leg forward and back, which is a run. **A skater's push is LATERAL**: the leg
goes out to the side and the body carves over it, and that is the whole difference
between Bolt Sprint and a sprint. Sean's tables carry zeros in both new columns and his
three clips re-export byte for byte identical, which is how that was checked.

### ⚠️⚠️ THE VERIFIER REFUSED TWO OF THE THREE TABLES, AND BOTH REFUSALS WERE RIGHT

**Magnet.** The first table snapped the arm from rest to -96 degrees in 0.05 s and then
closed it 44 degrees at the catch, so **the fastest frame in the clip was the OPENING**.
Whichever moment is fastest is the one a player reads as the event, whatever the table
calls the punch, and this ability's readable moment is the tsinelas ARRIVING in his
hand: that is the difference between *"he is doing something"* and *"he has his shoe
back and is about to throw it"*. The reach is 0.07 s now and the catch sweeps 75 degrees
into a dead stop. ⚠️ **This clip gained a punch the procedural one does not have**,
and that is the one place Zack's tables argue with `HeroAbilityClips`: its header is
right that a buzz has no impact, and MAGNET's buzz is the WIND-UP rather than the whole
gesture.

**Thunderstrike.** The first table had no hold, so it raised the arm through 220 degrees
and dropped it through 58, and the fastest frame landed in the middle of the RAISE. The
note it was refusing is `BuildZackSummon`'s own: *"The bolt comes DOWN. The raise at 0.28
is the call and it stays smooth."* The card says **"Hold to pick a spot, let go"**, so
the hold is nine frames of almost nothing now and the release sweeps 83 degrees in
0.09 s.

**Verified: fifteen clip checks across the three rigs, one Blender re-import each.**

| Clip | Impact | Peak speed | Silhouette |
|---|---|---|---|
| `hero-zack-sprint` | **none, by design** | 1264 deg/s | widest pose 87 degrees; a carve rather than a fold |
| `hero-zack-charge` | 0.30, the catch | **4564 deg/s**, 8.6 per cent one frame later | reach to -96 degrees, caught to -20 |
| `hero-zack-summon` | 0.45, the release | **4677 deg/s**, 2.8 per cent after | widest pose 177 degrees, height 0.63 to 0.86 against a 0.79 standing height |

**The timings are the procedural ones**: 0.60, 0.40 and 0.75 seconds. ⚠️ **The catch
was fitted INSIDE Magnet's existing 0.40 rather than added to the end**, because
lengthening a cast to suit an animation is the retune this file forbids.

**Still open for Zack:** the motion photograph, a person feeling it in a match, and the
first-person `viewmodelAction`s (`sprint-electric`, `overcharge`, `summon-lightning`),
which are engineering's hook. Task 6 covers that class for Sean and the same is true
here.

### 1C. Dante

- [ ] **DANTE: all three hero casts, then STOP**
  - `hero-dante-stomp`
  - `hero-dante-roar`
  - `hero-dante-fissure`

Suggested motion identity: heavy, grounded, violent, powerful.

**Authored 2026-09-07 on `team-dante.glb` and both custom rigs. Unchecked for the one
reason every task here is unchecked** (`docs/TODO.md` § 151.16).

⚠️⚠️ **DANTE IS THE THIRD DIRECTION AND THAT IS THE WHOLE BRIEF.** Sean's power goes
FORWARD. Zack's goes SIDEWAYS. **Dante's goes DOWN, into the floor, and he never travels
at all.** He plants, widens, and drives mass through his own centre. **That claim is
measured**: `contact` is 1 in all three of his clips and the verifier reports his feet
**0.000** clear of the road in every one, against Supernova's 0.160 leap and
Thunderstrike's 0.074 rise onto the toes. He is the only hero in the cast with no lift
anywhere.

⚠️ **THE STANCE IS THE SIGNATURE AND THE LEG ROLL IS WHAT DRAWS IT.** Every cast
splays both legs outward into a braced base no other hero uses. His arms move TOGETHER,
low and wide, and never take the graceful overhead arc Sean and Zack both own: they come
up short of vertical and hammer down. His head stays low and forward like a bull rather
than being thrown back. His recoveries are long because he is heavy, which is most of
why his are the longest clips in the game.

⚠️ **HIS TWO SKILLS ARE BOTH A PLANT AND A DROP, SO ONE OF THEM HAD TO BE A WIDENING**
or he has one skill twice. The stomp goes down and the carapace goes OUT.

| Clip | Impact | Peak speed | Note |
|---|---|---|---|
| `hero-dante-stomp` | 0.30 | 1546 deg/s, 5.7 per cent after | the knee comes up and everything goes down at once |
| `hero-dante-roar` | 0.32 | 1114 deg/s, 2.5 per cent after | **the smallest pose in the game at 40 degrees**, which is right for a buff whose read is that he is bigger afterwards rather than that he hit something |
| `hero-dante-fissure` | 0.40 | **4128 deg/s**, 1.4 per cent after | widest pose 164 degrees, height 0.55 to 0.86 against a 0.79 standing height |

⚠️ **THE HOLD AT 0.30 IS WHY THE ULTIMATE IS THE BIGGEST THING IN HIS KIT RATHER THAN
THE SECOND BIGGEST.** Without it the slam had 0.18 s to travel and read slower than the
stomp's knee drop, which would have put his ultimate below his first skill on the one
measurement this project has for force. It is also correct for a titan: the weight hangs
before it falls. ⚠️ **The slam lands FORWARD of his feet**, because `telegraphRange`
is 2.2 and the radius 4.5; a slam straight down would tell the other three the wrong
place to not be standing.

All three tables passed the verifier on the first attempt. Timings are the procedural
ones: 0.55, 0.65, 0.85.

### 1D. Cheska

- [ ] **CHESKA: all three hero casts, then STOP**
  - `hero-cheska-frostwave`
  - `hero-cheska-raise`
  - `hero-cheska-nova`

Suggested motion identity: controlled, elegant, sharp, cold, deliberate.

**Authored 2026-09-07 on `team-cheska.glb` and both custom rigs. Unchecked for the same
one reason** (`docs/TODO.md` § 151.16).

⚠️⚠️ **SHE IS THE ONE WHO COMMITS NOTHING, WHICH IS THE FOURTH THING A BODY CAN DO
WITH A CAST.** The other three all spend their whole mass. **She stays upright and spends
a hand.** Her torso pitch never passes 22 degrees against Dante's 56, her feet stay under
her and turned out rather than splayed, and the power is one forearm describing an exact
shape in a single plane.

⚠️⚠️ **AND SHE IS THE ONLY ONE WHO HOLDS.** Every clip ends its impact on a hold beat
that barely moves before it lowers, because ice is the element that STOPS and
`BuildCheskaRaise` said it first: *"The pillars lock. Ice is the one element that STOPS,
so it should stop."* **That turned out to be the clearest of the six separations and it
was measured rather than designed.** Speed one frame after the impact, as a fraction of
the peak: **Cheska 0.0 to 0.7 per cent**, Dante 1.4 to 5.7, Sean 2.3 to 5.9, Zack 2.8 to
8.6.

| Clip | Impact | Peak speed | Note |
|---|---|---|---|
| `hero-cheska-frostwave` | 0.28 | 1385 deg/s, **0.0 per cent after** | a dead stop, aimed and finishing pointed at the sheet |
| `hero-cheska-raise` | 0.30 | 2144 deg/s, 0.7 per cent after | the widest pose in her kit at 103 degrees |
| `hero-cheska-nova` | 0.32 | **3053 deg/s**, 0.4 per cent after | a 0.022 rise onto the balls of her feet |

⚠️ **HER ULTIMATE IS THE FASTEST AND LONGEST-HELD THING IN HER KIT BUT NOT THE WIDEST
POSE**, and that is left as it is rather than tuned away: a wall is made with the
forearms in front of the body and a nova opens to the sides. **Forcing the pose wider to
win a number would be fitting the art to the metric.**

⚠️ Permafrost Sheet's first table was refused and the refusal was right, for the third
time in three heroes and the same reason each time: with a 0.12 s draw the hand crossed
the chest faster than it left it, so the fastest frame in the clip was the WIND-UP.
Whichever moment is quickest is the one a player reads as the cast, so the anticipation
has to be the slow half by construction. Timings: 0.50, 0.55, 0.70.

### 1E. Nemu

- [ ] **NEMU: all three hero casts, then STOP**
  - `hero-nemu-ghoststep`
  - `hero-nemu-project`
  - `hero-nemu-seance`

Suggested motion identity: ghostly, floating, unnatural, playful, unsettling.

**Authored 2026-09-07 on `team-nemu.glb` and both custom rigs. Unchecked for the same
one reason** (`docs/TODO.md` § 151.16).

⚠️⚠️ **SHE IS THE ONE WHO DOES NOT PLANT, AND IT IS THE ONLY THING SHE HAS THAT
NOBODY ELSE CAN BORROW.** Four heroes stand on the road to cast and Dante's whole
identity is that he never leaves it. **Hers is that she never touches it**: her feet are
clear of the road in all three clips, **0.042, 0.037 and 0.094**, and `contact` runs to
0.05 in the middle of her ultimate. No beat in her kit is a push-off. She rises without
pressing on anything, which is the one thing a body cannot do.

⚠️ **HER LIMBS ARRIVE BEFORE HER TORSO, WHICH IS EVERYBODY ELSE BACKWARDS.** The other
five lead with the trunk and let the extremities follow; her arms hit their extreme a
beat EARLY and the torso catches up, which is what makes a body read as being carried
rather than as moving itself. Her two legs drift the same way rather than opposing,
which no living stance does.

⚠️ `BuildNemuGhoststep` already made half of this argument and it is kept whole:
*"Nemu going part-ghost is the single power in the game that should have NO weight ...
Every other hero gets a frame where the world stops. Hers does not, and that is what
makes it hers."* **Her ghost step measures 693 deg/s of peak, the slowest cast in the
game by a factor of two.**

⚠️⚠️ **HER ULTIMATE COLLAPSES INWARD AND EVERY OTHER ULTIMATE STRIKES OUTWARD**,
which is the clearest single frame of separation in the six kits. Supernova comes down,
Thunderstrike points, Titan Fissure splits, Glacial Nova opens, Grand Coven falls.
**Devouring Seance PULLS**: she rises with her arms wide and is then dragged in and
folded toward the thing she opened. The word in the card is *"consuming"*.

| Clip | Impact | Peak speed | Note |
|---|---|---|---|
| `hero-nemu-ghoststep` | **none, by design** | 693 deg/s | the slowest cast in the game |
| `hero-nemu-project` | 0.26 | 2437 deg/s, 3.3 per cent after | the body looks EMPTIED by it: chest open, head back, one arm flung after the thing that left |
| `hero-nemu-seance` | 0.38 | **5293 deg/s**, 0.0 per cent after | widest pose 157 degrees, feet 0.094 clear |

All three tables passed the verifier on the first attempt. Timings: 0.50, 0.50, 0.80.

### 1F. Phaister

- [ ] **PHAISTER: all three hero casts, then STOP**
  - `hero-phaister-hex`
  - `hero-phaister-blink`
  - `hero-phaister-eclipse`

Suggested motion identity: theatrical, magical, deliberate, witch-like.

**Authored 2026-09-07 on `team-phaister.glb` and both custom rigs. Unchecked for the same
one reason** (`docs/TODO.md` § 151.16).

⚠️⚠️ **SHE PERFORMS, AND THAT IS THE SIXTH AND LAST DIRECTION.** The other five are
all doing something TO the court: Sean drives through it, Zack points at it, Dante breaks
it, Cheska freezes a piece of it, Nemu is taken out of it. **She is doing something IN
FRONT of it.** Every cast passes through a FLOURISH beat, an off-axis pose neither the
gather nor the strike would reach on its own, and finishes front-on with the chest open
and held for the room. It is the difference between casting a spell and presenting one.

⚠️ **THE ORNAMENT IS WHAT SEPARATES HER FROM CHESKA, WHO IS THE OTHER ONE WHO HOLDS.**
Cheska takes the shortest line between rest and the shape, then stops. Phaister takes the
long way round on purpose. Same stillness at the end, opposite route to it.

⚠️ **SHADOW BLINK IS HER ONE EXCEPTION AND THE CONTRAST IS THE POINT**: no flourish,
because a blink has no time to have one. `BuildPhaisterBlink` already drew the
neighbouring line between her and Nemu, that a ghost step is a state you drift in and a
blink is instantaneous.

| Clip | Impact | Peak speed | Note |
|---|---|---|---|
| `hero-phaister-hex` | 0.34 | 2946 deg/s, 2.4 per cent after | draws the sigil through the flourish, then stamps it forward at the chalk |
| `hero-phaister-blink` | 0.24 | 2389 deg/s, 2.5 per cent after | collapse inward, thrown open front-on on the far side |
| `hero-phaister-eclipse` | 0.62 | **5728 deg/s**, 0.7 per cent after | **the widest pose in the game at 179 degrees** |

⚠️ **GRAND COVEN CARRIES THE LONGEST ANTICIPATION IN THE SIX KITS**, which
`BuildPhaisterEclipse` states as a requirement rather than a flourish: *"§ 4.3 asks for a
wind-up so the payoff has a moment; this is the longest anticipation of the six kits,
which is what an arena-wide power should cost to cast."* **Sixteen frames pass between
the arms reaching the sky and the night coming down.**

All three tables passed the verifier on the first attempt. Timings: 0.55, 0.42, 0.95.

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
* `tools/audit_clip_import.py`: **20 of 20**, following each serialized reference into
  Unity's own imported artifact. Every one names `slide`, carries seven rotation
  curves, and `CharacterAnimator`'s body-action chain still picks that name first.
  Evidence: `docs/reports/retrieval-slide-roster-import-v2.json`. No network claim.
  ⚠️ **That tool was `audit_slide_import.py` and asked about ONE clip by its
  hard-coded `fileID`**, so Sean's three casts landed and it had nothing to say about
  them: `CLAUDE.md` § 4a's *"a checker that carries a list cannot see the thing added
  after the list"*. It walks the roster's whole `Clips` array now and compares the names
  it resolves against the animations actually in the `.glb`.
  `docs/reports/roster-clip-import-v3.json` is the current evidence.
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

### Sean reference review, 2026-09-09

- [x] **First polish batch increment 3: keep Sean's `slide`; it passes the
  isolated strip review.** Reviewed the committed side and quarter v1 strips and
  `docs/reports/motion/sean_slide_v1.txt` on `ef6cd703`. No export or solver change.
  This is Sean's reference approval, not completion of the broader task 3.

1. **Entry:** 0.000 to 0.140 reads as deliberate commitment into a feet-forward
   skid, with a lowered reaching shoulder and a lifted face rather than a trip;
   the 8.71 m/s head peak at 0.115 s supports a forceful dive, but sampled poses
   cannot rule out an inter-frame snap in full-speed playback.
2. **Low movement and reach:** the quarter view separates the forward hand from
   the torso while the side view preserves a long low silhouette, so the pose
   reads as road-level retrieval at the strip's small display size, with hand
   clearance 0.125 m at 0.140/0.250 and 0.193 m at 0.342 as withdrawal begins.
3. **Follow-through and recovery:** 0.407 to 0.814 moves through a tilted low
   support and bent-knee rise before straightening, which reads as an exposed
   recovery rather than an immediate stand-up, within the existing 0.950 s clip.
4. **Return to rest:** the almost-upright 0.814 pose leads into the planted 0.950
   pose, so matching idle frame 0 is an appropriate destination rather than
   evidence of a cut; the reported zero bone difference establishes endpoint
   continuity, not the behavior of the runtime blend.
5. **Verdict:** keep the clip, it passes this review; there is no visible defect
   warranting an export, and the nine floor readings are 0.000 m with only
   -0.002 m at 0.095 in the finer trace.

**Measurement correction:** the earlier claim below that 0.125 m held through
the whole contact window was wrong. The report and both image captions say
**0.193 m at 0.342 s**, or **9.56%** of the 2.018 m rig, versus **6.19%** at the
two earlier contacts. The calibrated renderer box and chosen baked span both
read 2.018 m; the rejected double-scaled span is 4.803 m. The rising hand is
visible in both views and does not justify changing the clip to fit stale prose.

No fresh Unity/Blender run, player build, full-speed match capture, or human feel
approval is claimed. The unchanged v1 evidence remains the review source.
`docs/TODO_Archive.md` section 151.22 records this completed bounded review.

### Remaining, before checking the whole task 3

* ✅ **THE MOTION IS PHOTOGRAPHED AND SEAN'S ISOLATED STRIP IS APPROVED ABOVE.**
  `docs/TODO.md` § 151.16 is closed: `ClipMotionStrip` shoots nine poses across the
  0.95 s clip through the game's camera, toon shader and ink outline, with the floor
  drawn at y = 0. **Sean's `slide` resolves at SLOT 0**, its own authored clip rather
  than the `attack-kick-right` lunge fallback. Evidence:
  `docs/reports/motion/sean_slide_side_v1.png` and `..._quarter_v1.png`, with
  `sean_slide_v1.txt` beside them.
  ⚠️⚠️ **AND THE PROBE INDEPENDENTLY CONFIRMED THIS FILE'S OWN BLENDER SOLVE.** The
  reach was solved to `REACH_FRACTION` **6.17 per cent** in Blender against deformed
  meshes. Unity measures the lowest `arm-right` vertex at **0.125 m** at 0.140 and
  0.250 s against a **2.018 m** rig: **6.19 per cent**; at 0.342 s it is **0.193 m**.
  The early contacts agree with the target; the entire window does not hold it.
  Floor penetration is **-0.002 m at its deepest**, the peak bone speed is
  **8.71 m/s on `head` at 0.115 s**, and the last frame is **identical to `idle` frame
  0**, so there is nothing to snap back from.
  ⚠️ **What is still not evidence: the strip shows the clip in ISOLATION.** Transitions
  into and out of it, the blend weight, and whether the reach lands near the tsinelas at
  the moment the pickup fires are things only a match shows.
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

Task 3 stays unchecked for the live-match review and first-person dependency.
Sean's bounded strip review is complete; the separate accessory question remains
outside its scope. No hero skill or ultimate task was started in this review.

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

- [x] **Closed 2026-09-09.** `ViewmodelArms.PlayAction("slide")` now selects a
  dedicated `SlideClip`, not `LungeClip`. Its reach holds at Sean's approved body
  contacts 0.140/0.250 s, withdraws from 0.342 s and returns over the body's full
  0.950 s. A real pickup alone enters the existing carry pose through a 0.18 s
  wrist release; a miss completes the recovery with the viewmodel shoe hidden.
  `LungeClip` is unchanged and the existing camera kick remains.

  Focused EditMode evidence drives both outcomes at 60 Hz and compares the slide
  against the lunge. The action capture records failed contact/recovery/rest,
  successful pickup/carry and the lunge under one camera in
  `docs/reports/motion/fpp_*_v1.png`. `docs/TODO_Archive.md` section 151.23 carries
  the full verification boundary. No movement, pickup, cooldown, authority or
  input value changed.


### 5. `PersonSwapProbe` asserts every rig carries the same clip count (engineering) ✅ DONE 2026-09-09

- [x] **Fixed.** `PersonSwapProbe.MissingBaseClips` is a superset check now and the report line
  says the count both ways (`51 clips (base rig has 33, 0 missing)`), so a genuine loss still
  reads at a glance. `AnimationReviewTests` owns the rule from both sides: an extra authored
  action is not a loss, and a base clip that disappeared fails even when the total went UP.
  `docs/TODO.md` § 151.20.

  The original entry, kept for its reasoning:

- [ ] `Assets/TumbangPreso/Editor/PersonSwapProbe.cs` compares
  `team-custom-base.glb`'s clip count against `character-female-a.glb`'s and reports
  *"FAIL: the clip set did not survive the rebuild"* when they differ. **They now
  differ for a correct reason**: clips are authored per character, so that rig holds 51
  (32 source, `slide`, and all eighteen hero casts, because a custom character borrows a
  whole hero kit) against 33.
  Done means a SUPERSET check, which is what the assertion always meant: the rebuilt
  rig must not have LOST any of the base rig's clips, and `oldClips.Except(clips)` is
  the finding. `docs/TODO.md` § 151.20. Not a build gate, and no `.cs` was edited here.

### 6. Eighteen first-person viewmodel actions (engineering)

- [ ] Every hero ability names a `viewmodelAction`, and
  `Assets/TumbangPreso/Runtime/Camera/ViewmodelArms.cs` is where those resolve. **The
  eighteen body clips authored on 2026-09-07 cannot reach any of them**, exactly as
  task 4 records for `slide`: `thrust-fire`, `ignite`, `supernova-slam`,
  `sprint-electric`, `overcharge`, `summon-lightning`, `stomp-heavy`, `carapace-guard`,
  `fissure-slam` and the rest. Done means the first-person arms match the body's casts.
  Same rule as task 4: no movement, cooldown or balance retuning.

### 7. "32 clips" is a stale count in three `.cs` comments (engineering) ✅ DONE 2026-09-09

- [x] **Fixed, and it was more than three places.** `ModelImportSetup`, `RosterEntryAsset`,
  `DanceClip` and `PersonSwapProbe` in code, plus `docs/CANONICAL_RENDERING_PIPELINE.md`,
  `docs/Port_Ledger.md`, `docs/Port_Plan.md` and `docs/Voxel_Person_Guide.md`. Each one now
  says what it is describing without naming a total, and each carries a ⚠️ line saying the
  count was there and had gone stale, so nobody puts one back.
  ⚠️ **`docs/Voxel_Person_Log.md` is deliberately UNTOUCHED**: it is a record of one day and
  32 was true on that day. `docs/Port_Plan.md` § 8's retargeting permission was the one worth
  reading twice, because it was not a comment but a MEASUREMENT (*"`head`, `arm-left` and
  `arm-right` translations are never keyed"*) and an authored clip is free to key a translation
  the CC0 pack never touched. It says so now rather than being deleted.

  The original entry, kept for its reasoning:

- [ ] `ModelImportSetup`, `RosterEntryAsset` and `DanceClip` each state that a rig
  carries 32 clips. It is 33 on most, 36 on the six hero rigs and 51 on the two custom
  ones, and it moves every time this queue is worked. The fix is to stop naming a number
  rather than to update it. `docs/TODO.md` § 151.20.

### 8. ⚠️⚠️ SECTION 2 HAS NO ACTION NAME TO ATTACH TO, WHICH BLOCKS ALL SIX OF IT (engineering)

- [ ] **The ultimate comic-book cinematics (§ 2A to § 2F) cannot be delivered under this
  file's own rules as it stands.** `CharacterAnimator`'s chain table holds the stock set,
  `slide`, and the eighteen `hero-*` casts authored on 2026-09-07. **There is no
  cinematic name in it.** ⚠️ **No count is given here on purpose**, which is `CLAUDE.md`
  § 4a's rule about a number outliving the list it describes; the first draft of this
  entry said 39 and the table holds 32, because a `grep` for `{ "` catches bone names as
  well as action names. **Read `CharacterAnimator`'s table, do not trust a total.** So a
  clip authored for a cinematic would be a `.glb` sub-asset
  nothing ever asks for, and the only way to reach it is to add a row to the chain table,
  which this file forbids in as many words: *"Do not edit the chain table unless
  engineering explicitly asks you to."*

  ⚠️ **THE REST OF THE PRESENTATION IS ALREADY ENGINEERING'S BY THIS FILE'S OWN TABLE.**
  § 2 asks for a *"short controlled global cinematic freeze"*, comic-book framing and
  speed lines. `TumbangPreso.Hitstop` owns the bounded global freeze and § 2 says so;
  framing is `CameraRig`, whose header records that it is deliberately NOT hitstop; and a
  graphic treatment is a new VFX system, which the *"What is not yours"* table assigns to
  engineering by name. **What is left over for Astra is the POSE**, and the pose is the
  half with no hook.

  **Done means one of two decisions, and it is a decision rather than work:**
  1. **A named action per hero** (`hero-<name>-intro`, or one shared `ultimate-intro`)
     added to the chain table, plus whatever plays it during the freeze. Then § 2 becomes
     six ordinary Astra sessions of exactly the shape § 1 just was.
  2. **Or the cinematic is a CAMERA and VFX feature over the existing cast clip**, in
     which case § 2 is not animation work at all and should move to `docs/TODO.md`.

  ⚠️ **Do not answer this by quietly widening § 1's clips.** Each hero's ultimate cast is
  already the strongest thing in its own kit, measured, and stretching one to double as a
  cinematic pose would spend the separation the six motion languages were built on.

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
