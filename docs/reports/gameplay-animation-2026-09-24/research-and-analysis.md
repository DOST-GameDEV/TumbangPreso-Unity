# Gameplay animation: what each action must communicate, and how

🧑 2026-09-24: *"refine other gameplay animations too for both FPP and TPP view for example the raising
of can or throwing of slippers or pektus etc"*, *"genuinely research how it should look like and analyze
how it should look to communicate that that action is happening"*. Earlier the same day:
*"refine actual walking/running animation ... my biggest issue is where the hands are and what they do
when u run"* (done: `CharacterAnimator.LocomotionArms.cs`, `ViewmodelArms.RunSway.cs`).

This file is the research and the analysis. Code and evidence are named per action below.

## 1 · What the research says a gameplay action owes the player

Sources: Jonathan Cooper's *12 principles of animation in video games* (gameanim.com), the throwing
biomechanics literature (Physiopedia, Orthobullets, PMC *Pitching mechanics revisited*), and ultimate
frisbee coaching on the sidearm flick and backhand (Flik, Townsville Ultimate).

1. **Anticipation says "something is coming" and WHICH thing.** A wind-up that reads at a distance is a
   change of SILHOUETTE, not a few degrees of shoulder: the arm leaves the body line, the chest turns
   away from the target, the free arm comes up. In games anticipation must not delay the action; the
   pose is shown while the input is held (our charge), so it can be large without costing response.
2. **Contact is the fastest thing in the action and the pose around it is held.** A thrown object leaves
   in a whip: hips, chest, upper arm, forearm, wrist, in that order, each faster than the last. The
   release frame is the one the eye must catch, so the body is in its most extended, most readable shape
   there, and the follow-through then HOLDS a strong pose briefly (the arm across the body, the chest
   turned past the target) before recovering.
3. **Follow-through tells you where it went.** The arm finishes pointing along the flight. A throw that
   finishes where it started says nothing about direction.
4. **Exaggerate and stay consistent.** "Real life never looks real enough." Every verb in the game is
   pushed by about the same amount, so none looks limp next to another. Our cast is chunky with no
   elbows, so the SHOULDER and CHEST must carry what an elbow and wrist would.
5. **First person has only the hands and the camera.** The action must move THROUGH the screen: the
   wind-up takes the hand back and partly out of frame, the release sweeps it across the screen toward
   the crosshair, the follow-through exits low on the other side. The off hand is the aiming hand. A hand
   that stays in one screen position through a throw (the old viewmodel) reads as "nothing happened".
6. **Progress actions show progress.** A channel (a held interaction) must show that the thing is
   happening AND how far along it is, or a player cannot judge whether to keep holding or run.

## 2 · The three actions

### 2.1 The straight throw (`throw`)

The street throw is an overhand or three-quarter throw of a rubber slipper at a can 5 to 7 m away.

**What it must say:** I am aiming *there* (charge), it left *now* (release), it went *that way*
(follow-through).

| Phase | Third person (the body others see) | First person (your hands) |
|---|---|---|
| Charge | Chest turned away from the target, throwing arm drawn back and OUT to the side at shoulder height (never up behind the head: the shoe cut through the hair, `body-throw.md`), the free arm raised and pointing at the target, weight back. The charge amount scales it. | The slipper hand draws back, up and toward the screen edge; the off hand rises into view pointing at the crosshair. |
| Release (55 ms) | The chest whips round past square, the arm sweeps forward and across at shoulder height. | The hand sweeps across the screen toward the crosshair and down. |
| Follow-through (to 180 ms, held) | Arm across the body, chest turned past the target, head forward. | The hand exits low, left of centre. |
| Recovery (to 540 ms) | Back to the current locomotion pose. | Back to the carry pose. |

### 2.2 Pektus (left and right)

Pektus is the curve throw: a signed spin (`Balance.MaxPektusSpin`, -1 to +1) that bends the slipper's
flight (`PektusCurveStrength`). It is a DIFFERENT throw, not a rotated one. The frisbee literature is the
closest documented analogue: spin comes from a wrist cocked back and snapped through a SIDEARM path, and
the direction of the snap decides the direction of the spin.

**What it must say:** this is a curve, and it will bend THIS way. Two cues, both readable from across the
court and from your own hands:

| | Right pektus (spin +1) | Left pektus (spin -1) |
|---|---|---|
| Plane | Sidearm: the arm travels low and flat, at hip-to-waist height, not over the shoulder. The body leans to the throwing side. | The same sidearm plane. |
| Wrist / slipper during charge | Cocked and rolled so the slipper's face turns out (palm up), visibly twisted. | Rolled the other way (palm down, back of the hand up). |
| Follow-through | Finishes wide and OPEN, the arm ending out to the right, the chest turned right. | Finishes CLOSED across the body to the left. |
| First person | The hand drops low and rolls palm-up; the sweep ends low right. | Rolls palm-down; the sweep crosses to the low left. |

### 2.3 Raising the can (the taya's reset channel, `ResetChannelTime` 1.5 s)

The taya must stand the downed can back in its circle before they may tag (`Design.md`). In the street
this is a quick crouch, a hand on the can, standing it up and pressing it down so it stays.

**What it must say:** the taya is busy at the can (so attackers can run), and how close they are to done.

Before this pass: third person looped a generic standing `interact-right` reach, never bending to the
can; first person re-fired a 0.4 s grab dip three or four times; the can lay flat for the whole 1.5 s and
popped upright at the end. Nothing showed progress.

| | Third person | First person | The can |
|---|---|---|---|
| 0 to 15 % | Drop into a crouch at the can, both hands down to it. | Both hands come into view low, closing on the can. | Down. |
| 15 to 85 % | Stay low; the hands lift and roll the can upright as the ratio climbs. | The hands tilt up with the ratio. | Its drawn tilt eases from down to upright WITH the ratio (presentation only; uprightness and every rule stay host-decided). |
| 85 to 100 % | A small press down: the plant. | A short press. | Upright. |
| Interrupted | Hands leave; stand. | Hands drop out. | Back to its real (down) tilt. |

## 3 · Rules this pass keeps

- Timing never changes: release time, projectile origin and trajectory, the 1.5 s channel, and every
  cooldown are the game's, and animation fits around them (`AGENTS.md`, REFINE-2 item 5).
- The carried shoe never passes through the head volume (`body-throw.md`), and the FPP carry hand is
  never rotated by a sway (`ViewmodelArms` carry rule).
- No new elbows, fingers or rig parts; the seven-bone rig is kept (`AGENTS.md`).
- Both views are designed together and must agree on what happened.

## 4 · What was built, and what the film showed

Filmed with `GameplayActionShots` (both views at once: a witness camera on the body, including one
straight in front of the thrower where the taya stands, and the owner's own first-person camera) and
`LocomotionArmsProbe`. Each change was judged on its pictures, then tuned.

| Action | Third person | First person | Tuning the film forced |
|---|---|---|---|
| Walk and run | `CharacterAnimator.LocomotionArms.cs`: free arms clear of the body, swung against the opposite leg (sprint -28 to +65 degrees, walk -21 to +30); the slipper carried low at the side | `ViewmodelArms.RunSway.cs`: a footfall bob; the empty hand pumps | The first three probe runs measured the clip under the layer (a coroutine resumes before `LateUpdate`); glTF import mirrors X; hero bodies carry a hidden second rig, so bones come from the visible skin and axes from the bind pose |
| Straight throw | `CharacterAnimator.ThrowBody.cs`: chest turned away, arm back and out, free arm pointing at the target; release across the body; held follow-through | `ViewmodelArms.ThrowReach.cs`: the slipper hand draws back and close, the off hand rises into view; the release sweep finishes low left | The shoe first sat beside the head in the taya's view (lowered); the old charge cock turned the first-person slipper end-on at full charge (countered on the raw charge) |
| Pektus | Sidearm at the waist, wrist rolled while charging, finishing open to the right or closed across to the left | The hand drops low and wide and rolls; the finish follows the spin | The left roll first turned the hand out of the bottom of the frame (milder roll, higher left sidearm) |
| Can raise | `CharacterAnimator.ResetRaise.cs`: a bow to the can, hands rising with progress, a press; `Lata` tilts only its mesh up with it | `ViewmodelArms.RaiseCan.cs`: both hands on the can at the centre, rising; the repeated grab dip is suppressed | The first bow put the head at the can (eased to 50 degrees) |

Still owed before calling this finished: the shoe's head-volume clearance re-measured on the new
overhand pose (`ThrowMotionReviewProbe`), every other body's walk inspected individually (REFINE-2.9c),
and a look at a live match at normal speed, including an observer's view of the raise estimate.

## 5 · The tag, the other verbs and every hero cast (second pass)

🧑, after the first pass: *"can u finish all other characters and all otehr animations"*, *"make tagging
better too"*, *"and the animation of all skill casting"*. `CastAndMotionReel` films every roster body
moving (side and front), the base verbs, and all 21 hero casts in both views.

**The tag.** A tag is a touch, and what it must say to the chased attacker is *that hand is coming for
you, and now it has reached you*. Measured on the shared rig, the dash tag (`lunge`) played
`attack-kick-right`, a 104-degree KICK, and the quick tag (`punch`) played `attack-melee-right`, an
overhead CHOP. `CharacterAnimator.TagBody.cs` poses both as reaches: the jab leans in with the arm straight
out at chest height and snaps home; the dive leans deep with the arm long and low and the legs split, and
HOLDS the reach while the sweep can still land. `ViewmodelArms.TagReach.cs` makes the first-person hand
travel toward the crosshair (it only pitched in place before).

**The casts.** Filmed, six already read (Supernova, Summon, Stomp, Fissure, Seance, Eclipse). Eleven did
not: each was a few degrees and back at rest within about half a second, so the pose that says what the
skill did never registered.

⚠️⚠️ **WHERE THE SHIPPING CASTS LIVE.** The first attempt re-keyed `HeroAbilityClips.cs` and the film did
not change: that file is only the EDITOR's fallback for a clip a rig lacks. The shipping casts are baked
into each hero's glb from the beat tables in `tools/author_hero_action.py` (Blender, `polish_hero_actions.py`,
checked by `verify_hero_action.py`); Rafi's come from `HeroAbilityClips.Rafi.cs` through
`RosterBookBuilder` into `rafi-motion/*.anim`. So the fix went there:

1. **Bigger, same shape.** A readability pass in `author_hero_action.py` scales the weak casts' angles
   about rest (1.3 to 1.5), so the rhythm, the fastest-at-contact moment and the stop after it are the
   verifier's same clip, only larger. All 18 re-baked glb casts pass `verify_hero_action.py`.
2. **Held.** The contact pose is copied 0.12 to 0.16 s later, so the pose that says the skill registers.
   Rafi's use `ClipBuilder.HoldAt` for the same thing.
3. **First person.** The casts barely moved the hands at all; `ViewmodelArms.CastGesture.cs` gives each a
   gesture through the screen, keyed by the first-person action the ability sends.

Each re-keyed cast is shaped to say its skill: Permafrost Sheet a low sweep over the road; Ice Barricade a
scoop and an overhead heave; Glacial Nova a curl then a burst; Flame Rush a rocket launch; Ignition Cannon
a fist punched up; Magnet a reach and a yank; Demonic Carapace a double-arm flex and roar; Phantom Veil a
rise and a trailing glide; Crosscurrent a wide held sweep; Mirrorwake a big sell and a cut back;
Breakwater the wave sent with the whole body. Phaister's Hex no longer pitches her head at the viewer,
which turned her brim into a black slab.
