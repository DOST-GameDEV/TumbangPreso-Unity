# Active TUMP rework ledger

## Current execution pointer (live, 2026-09-10)

Continue the same task, do not restart or finish after this network batch. Branch
ASTRAReworks only. The major networking foundation is verified and being committed;
read git log/status for the exact current checkpoint. Detailed results are in
reports/improvement-2026-09-10/network-foundation.md. Earlier entries below are a
chronological record, including failures and disproved hypotheses, not fresh state.

Current verification: Core562/562, full EditMode486/486, current Nemu PlayMode20/20
with native NUnit XML, all8 checks, all14 gating source audits. The native UDP
regression fails on upstream6.5.0 and passes after the embedded receive-pool patch.
Three real processes agree on clean recall/ult and a correctly seated300 ms/2%
recall; direct owner hard-reconnect v6 reclaims the seat, restores the live ultimate
and retains charge counts. Settled remote model/body offsets are zero. These are
local real-process proofs, not a final-release or human playtest approval.

Next: reproduce/fix one-shot impacts on remote human victims (no impact RPC exists),
then same-hero non-default loadout binding and remaining network/mode/round/rematch
coverage. Continue all six hero kits and twelve existing loadout alternatives,
body/FPP animation/SFX/VFX, three maps/trees and final Windows qualification.
Owner explicitly permits replacing boring/redundant abilities; read
HERO_KIT_REWORK_DECISIONS.md and PHILIPPINE_ABILITY_DIRECTION.md. Preserve the
approved people models, flat faces and block hands; no renewed people redesign or
UI art effort. Progress last communicated as approximately45% of the full scope.

Official Unity plugin0.1.3-beta is installed/enabled; CLI1.0.0-beta.5 is available.
com.unity.pipeline0.6.0-exp.1 is installed in this project and a live connection was
verified. It ran20/20 tests. LiveTestResultRecorder now exports native XML for live
runs too. Always target this checkout explicitly. No Editor/player/native-control
session is currently needed or left running after qualification; use the guarded
runner for profile preservation. Runtime Pipeline control defaults off in builds.
No subagents, new tasks, paid work or usage reset. No user configuration is needed.


## Latest owner decision: keep the current models and move on

The owner reviewed the cleaned Classic cast in
`current-playable-18-clean-clothes-v3.png` and said these were already solid,
explicitly asking to move to other work. **Stop character redesigns. Preserve the
current cast models/outfits at `7c7fcb5` and prioritize animation, all eighteen
skills and their SFX/VFX, gameplay and map work, then Windows delivery.**

This supersedes the earlier request to individually rebuild every character.
It is a scope decision and approval of the shown Classic designs, not a claim
that all eighteen individual redesigns were completed. Do not revive the rejected
Berto bodies or integrate the separate forearm study as a new character model.
Animation work still includes convincing weight, footwork, carrying, throws,
retrieval, interrupted actions, all hero casts and imposing ultimates. Preserve
recognizable models, simple faces, block hands, original clothing and lighter ink.

## 1. Owner requirements that must survive compaction

- Make the existing game substantially better to play, look at and experience.
  Complete the authorized work autonomously. Do not stop after housekeeping,
  tests, one character or one attractive screenshot.
- **No subagents or delegation.** Work in this conversation. Parallelize independent
  tools and preparation, not agents.
- Work only on `ASTRAReworks`. Never work on, merge into or push `main`. Fetch and
  inspect actual status before changing branches or assuming a checkpoint. Preserve
  newer work. Do not touch unrelated changes in the Documents/GitHub checkout.
- Preserve four players, rotating defender, the can, throwing and retrieving
  slippers, Classic and Hero Strike, networking authority, profiles and saved IDs.
  No extra modes, skill slots, unrelated features or complicated gameplay systems.
- Windows only for this delivery. Android is deferred. Never perform a usage reset.
  Ask before paid external work; no paid work has been used in this pass.
- The owner may be asleep/AFK. Make routine creative and technical decisions. Do
  independent work instead of waiting for optional questions. Only a genuine
  inability or required external access should need their intervention.
- The owner explicitly allows PC control when needed, and asks that it **end when
  no longer needed**. Stop input and recording after inspection. Reacquire fresh
  window state next time. Do not leave an unrelated app, microphone or desktop
  capture recording.

### Models and style: history, subject to the latest keep-models decision

- **Berto outfit correction:** the owner says they liked his old clothes. Keep his
  original green outfit, straps and belt. These are restored. Improve rig/posing
  and genuine geometry defects without replacing that outfit with the rejected
  plain shirt/slab design. Do not repaint it into a different costume by default.

- The owner flagged Cheska's blue hand squares and the pasted-on clothing
  outlines. These were first-pass cuff fasteners and raised front pockets/buttons,
  not necessary original costume features. They are removed from all eighteen
  playable bodies in the current batch, preserving original wardrobe geometry.
  Roster/FPP regenerated; current cast inspected at
  `Logs/character-design/current-playable-18-clean-clothes-v3.png`; focused
  outline/arm/motion contracts pass 14/14 in `Logs/clean-clothing-contracts-v3.xml`.
- The owner asked to use Blender directly. Blender 5.2.1 is already installed.
  No additional package/download is needed for mesh and rig inspection. The native
  app was opened and the restored Berto inspected front/side. The review-only
  `.blend` is `Logs/character-design/berto-clean-rig-review-v2.blend`; its palette
  lookup accounts for Blender's imported V-coordinate flip. It is not a new model.
- The prior Escape interruption was accidental PC use, according to the owner.
  They explicitly asked to resume PC control and continue. Stop input when not
  needed; the overall task remains authorized and active.

- **Latest correction: stay cute and blocky.** The owner rejected the rounded Berto
  prototype shown in `Logs/character-design/berto-individual-v1.png` as ugly and
  broken. Its oversized blank head, tiny eye marks, lumpy clothing and rigid stance
  are not a direction to carry forward. The previous model and its dependent
  roster, arm and runtime files have been restored to the pushed checkpoint.
- The owner also rejects the added thumb shape. Remove that protruding addition;
  retain the original simple block hands. This applies to body and matching FPP.
  Keep flat graphic faces. Improve individual proportions, clothing construction,
  silhouette, restrained outlines and natural posing inside the existing cute,
  blocky language. Do not replace it with rounded anatomical or realistic models.


- The owner reviewed `all-playable-characters-v4.jpg` and said the model improvement
  was not noticeable and did not look better. Most Classic characters still look
  identical or weird. **The first refinement pass is not accepted or complete.**
- Author and thoroughly refine **every Classic character and every hero one at a
  time**. Think through each character's individual design. Do not mass-produce a
  common face/body recipe with different colors and accessories.
- Shared low-level mesh/export/rigging utilities are engineering support; the
  head, hair, face graphics, proportions, clothing and silhouette need individual
  authored decisions and individual review.
- **Flat, simple graphic faces are intentional and must stay stylistically simple.**
  The owner explicitly rejected the critique that facial flatness or lack of noses,
  eyebrows and other realistic details was a problem. Do not add realistic noses,
  brows, wrinkles or detailed facial anatomy merely to make the models "better."
- Distinguish people through head contour, eye/mouth shapes and placement, hair,
  proportions, costume construction, posture and a few meaningful accents. Simple
  faces can still communicate different personalities. Do not homogenize skin or
  force cultural symbols onto everybody.
- The owner agrees with the other critique: repeated facial grammar, weak body
  variation, awkward waists/crotches/clothing, too many equally strong outlines,
  uneven visual emphasis and rigid posing. Improve these thoroughly.
- Keep recognizable identities and a cohesive quirky Filipino feeling through
  people, everyday clothes, place, materials and sound. No forced decoration.
- **Rework matching first-person arms after the model design settles.** Match the
  actual geometry, costume, palette and grip. Rebuilding generic arms in parallel
  with a changing body is not completion.

### Animation

- Push animation quality as far as practical: believable weight, balance, grounded
  feet, articulation, anticipation, impact, follow-through, interruption and recovery.
  The owner specifically agrees that rigid posing must be fixed.
- Animation realism means convincing motion within TUMP's stylized art, not turning
  the faces photorealistic.
- Review complete sequences at normal speed, including preparation, release,
  contact, retrieval, refusal, interruption and return. Compare owner and observer
  presentation, normal play, and relevant 30/60/144 Hz conditions.
- Preserve authored slide timing/contact and motor/authority contracts while
  improving the rig and motion. Keep Generic rigs and existing bone paths/clip
  names compatible. The first bend-joint prototype was discarded with the rejected Berto model.
  Articulation remains open; choose it to support the preserved blocky style.

### Every existing skill, SFX and VFX

- The owner says most skills feel weak and explicitly authorizes deciding how to
  improve **their looks, functionality, sounds and effects**. This is a full pass on
  all eighteen existing skills, not only retiming body clips.
- Every skill should feel like it is actually being cast. Examine purpose, aiming,
  responsiveness, commitment, payoff, counterplay and recovery.
- **Every ultimate must feel genuinely imposing:** distinct buildup, substantial
  impact and aftermath, with a recognizable character-specific identity.
- Existing skill timing/function can change when an evidence-backed improvement
  warrants it. Record why, keep descriptions truthful and update behavioral
  coverage. Do not add new skill slots or unrelated mechanics.
- Do not equate "imposing" with blinding white frames, blur, generic huge spheres,
  excessive shake or visual clutter. The can, loose slippers, defender and danger
  footprint must remain readable, including overlapping casts and Low graphics.
- Effects must express their actual gameplay footprint and cancel/expire cleanly.
  Follow live runtime paths, not unused showcase builders. Nemu's live ultimate is
  KuroUnbound, not the old SeanceVoid showcase.

### The seven scrutiny priorities the owner explicitly adopted

1. Stronger, individually unique character silhouettes.
2. Better footwork, including carry, retreat and turns, with measured cadence.
3. Convincing skill commitment through body and first-person presentation.
4. Cleaner overlapping effects and clear gameplay priorities.
5. Believable chases; investigate poor bot lunge accuracy before retuning balance.
6. Purposeful map edges, retrieval routes, reachable slippers and sound collision.
7. Consistent first-person throw/retrieval/camera/sound timing across frame rates.

### UI boundary and retained earlier requirements

- **UI is excluded from further design/polish.** The girlfriend will make final UI
  art. Preserve the functional placeholder flows; update existing descriptions when
  necessary for changed skill behavior, without spending the task on UI decoration.
- The original UI checkpoint needed fresh verification in this task, and that has
  been run. Keep the character maker inaccessible to players while retaining its
  implementation, assets and data.
- Retain English player-facing copy and established proper names. Retain the
  requested drawn/moving home/loading art, readiness checks, random 5-15 second
  startup dwell, optional story open/next/close and icon Back/Close controls.
- Font roles remain Darumadrop display, Kawit Extended short accents/actions,
  Lydian reading. The owner confirmed Lydian embedding/distribution permission.

## 2. Current checkpoints and what they actually prove

Repository: `DOST-GameDEV/TumbangPreso-Unity`.

Checkout:
`C:\Users\Matthew\Documents\Codex\2026-09-09\ok-x20\work\TumbangPreso-Unity`

Unity executable:
`C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe`

Blender executable:
`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe` (5.2.1 LTS).

- Starting checkpoint: `4623348069eb0be9d9a00f429fd97c9252cf1fc3`, initially clean and
  matching origin after fetch.
- `8275111`: first motion/continuity batch and expanded AGENTS.md, pushed.
- `e730878`: model refinement, map geometry/lighting, eighteen cast animations,
  matching FPP meshes, feedback/audio correctness and evidence, pushed.
- `ad91efd`: measured cadence correction,
  foley random-stream isolation, diagnostics and expanded owner brief, pushed.
- `f300e2a`: remove added thumbs from all
  eighteen playable models, regenerate matching arms, lighten character ink, and
  preserve rejection/style decisions. Focused outline/arm/motion checks pass 14/14.
- `7c7fcb5`: remove misplaced cuff and
  clothing additions, regenerate arms, preserve the original wardrobes, current
  eighteen-person sheet and the repeated Berto rejections. Focused checks 14/14.
- **Last pushed gameplay checkpoint: `c09fba2`**, ground placement and Zack function
  correction. Character geometry is still the kept `7c7fcb5` version.

### Verified at e730878

- Core 559/559; EditMode 466/466; all eight `Checks.RunAll` checks.
- `tools/playmode_suite.py --gate --twice`: **197 passed, 9 skipped, zero failed out
  of 206 cases in each pass**. 68 of 78 discovered fixtures ran; ten were explicitly
  excluded by the WallClock/ThumbFloor category policy. Skips are not passes.
- Fourteen gating source audits passed. Informational audio analysis flagged seven
  existing recordings; do not silently normalize preferred/source audio to green it.
- The full Classic and Hero Strike bot matches completed. The default-seed bridge
  match had zero idle penalties. This does not resolve every historical seed.
- Preserved reports: `Logs/qualification-e730878/` includes both PlayMode passes,
  stage JSON and bot reports. Do not overwrite this record with later results.

### Verified at ad91efd before the new Berto model

- A new regression sampled actual imported walk/sprint stance travel on every rig.
  It was seen red: Berto's walk calibration advanced 2.016 m per cycle while the
  feet traveled 0.769 m; sprint was 2.592 m against 1.152 m.
- Cadence now derives from bind-pose leg reach, mesh floor, actual scale and shared
  authored swing angles. Walk is 38 degrees; sprint remains 44. The author reads
  the same constants. Sean's longer legs receive their own cadence.
- Fresh EditMode **467/467** (`Logs/cadence-edit-v1.xml`). Real carry/map/retrieval
  run **18/18** (`Logs/cadence-play-v1.xml`).
- `Logs/gait-cadence.csv` and committed before/after CSVs record forty comparisons.
  These validate the then-current rigid-leg clips. New articulated gait paths will
  need an appropriate actual-stance measurement, not a weakened test.
- Foley accents follow cycle distance; their pitch variation preserves the AI's
  shared random state. The existing network diagnostic report now prints the
  already-collected live frame histogram, actual graphics settings and hardware.

**No Windows build or exact-player verification has been completed in this task.**
Do not report the editor witnesses as remote network-client or human playtest proof.

## 3. Completed implementation retained as the foundation

- Carry/fatigue lower-body animation layer under the stable grip; charge continues
  graph evaluation; gait selection follows observed role speed, not a 7.5 m/s
  threshold unreachable by an ordinary 3.795 m/s attacker sprint.
- Interrupted blends retain their visible mixture. Rebinding retires the old graph.
  FPP recovery uses frame-rate-independent smoothing. Trips/refusals/reset clear
  matching hero presentation. Empty runtime SetCurve hero fallbacks are excluded
  from the player; authored clips own the shipping path.
- All eighteen first-pass hero body casts were authored and exercised through real
  input. FPP tells use both hands. This is not the now-requested complete individual
  functionality/SFX/VFX pass.
- First-pass character meshes received grip/footwear/clothing refinements. Forty
  FPP arm meshes derive from actual body meshes and palettes during roster rebuild.
  The owner rejected the overall visual result as insufficient; do not close the
  character deliverable using those file counts.
- Eskinita gained deeper fronts and authored sari-store geometry. Bayan gained
  articulated church, belfry and civic geometry, paving and shade canopies. Ilalim
  gained viaduct caps/bearings/soffit/drainage structure and clearer fill lighting.
  Preserve sourced jeepney/livery and quiet court space. These maps still need the
  adopted route/edge scrutiny and final ordinary-play review.
- Supernova warning matches existing 5.4 m reach. Kuro warning is pet-centered at
  its existing 4 m reach; bots now recognize that field. No radius was enlarged.
- Zack shock-ring center opened; cast/weather light intensity reduced. New grounded
  rubber contact and slide scrape; existing preferred can/UI recordings retained.
- Private confirmations use owner-only 2D audio. Jump/land relay. Clients can relay
  only five permitted owner cues from a seated sender near that body's position.
  Direct positional producers now have an ownership/reach audit.
- Functional UI fixes: actual raycast/navigation/story checks, custom-game viewport,
  short telemetry label, minimum door caption size and stale English-copy assertions.
- Results show one observed match moment, without new score/reward rules.
- Orphan roster assets removed and future orphan filenames rejected. The naked-base
  probe retains rig checks and tests face/hair features on a suitable Zack reference.
- The bot clock discrepancy was the probe inheriting eight rounds while switching
  only mode to Classic. It now pins/prints the whole ruleset. A live 90-second round
  produced 75 defence events and exactly 75 recorded ticks. Scoring was not retuned.
- Closed bodies 151.18, 151.15, 151.21, 151.6, 149.7 and 147.3 were archived whole with
  index pointers. Section 152.4 remains open.

## 4. Character ledger: redesign queue superseded by owner decision

The owner chose to keep the cleaned models and move on. The earlier individual
redesign queue below is preserved as history; do not execute it as open work.
Animation, rig compatibility and matching existing FPP presentation remain in scope.

| Order | Character / saved ID | Individual pass |
|---|---|---|
| 1 | Berto / `bayan` | Rejected drafts discarded; current original design kept by owner |
| 2 | Maring / `maring` | Keep current model; redesign superseded |
| 3 | Totoy / `totoy` | Keep current model; redesign superseded |
| 4 | Inday / `inday` | Keep current model; redesign superseded |
| 5 | Kuya Boy / `kuya_boy` | Keep current model; redesign superseded |
| 6 | Ate Girlie / `ate_girlie` | Keep current model; redesign superseded |
| 7 | Tikboy / `tikboy` | Keep current model; redesign superseded |
| 8 | Bebang / `bebang` | Keep current model; redesign superseded |
| 9 | Jun-Jun / `jun_jun` | Keep current model; redesign superseded |
| 10 | Lola Pacing / `lola_pacing` | Keep current model; redesign superseded |
| 11 | Mang Kanor / `mang_kanor` | Keep current model; redesign superseded |
| 12 | Aling Nena / `aling_nena` | Keep current model; redesign superseded |
| 13 | Sean / `sean` | Keep current model; prioritize hero motion and skill presentation |
| 14 | Zack / `zack` | Keep current model; prioritize hero motion and skill presentation |
| 15 | Dante / `dante` | Keep current model; prioritize hero motion and skill presentation |
| 16 | Cheska / `cheska` | Keep current model; prioritize hero motion and skill presentation |
| 17 | Nemu / `nemu` | Keep current model; prioritize hero motion and skill presentation |
| 18 | Phaister / `phaister` | Keep current model; prioritize hero motion and skill presentation |

The two custom bases remain retained and inaccessible. Keep their compatibility;
do not present them as additional selectable characters in a cast sheet.

### Rejected Berto prototype and next design constraints

**Second rejection, after the first restoration:** the block-body trials in
`Logs/character-design/berto-block-clothing-v3.png` through `v6.png` are also
rejected. The broad torso enclosed the shoulder pivots; shortening the arms hid
the upper arms and made the fists look attached near the hips. The shirt read as
a green bib/slab, with no clear chest-to-shoulder-to-arm connection. This is a
construction failure, not evidence that the original cute blocky style is wrong.
Do not resume `refine_berto.py` or carry that body into another character.

That trial source and script are quarantined in
`Logs/rejected-berto-block-body-2026-09-10`. The live Berto model, idle, slide,
roster entry and arms are restored to pushed `f300e2a`; GLB blob
`fb6867f9f5bdb42f74e602e2a8472ba30b215c13`. Thumb removal and lighter ink remain.
The trial slide had needed re-solving after geometry changes: the independent
import found a 0.01082-unit floor penetration before the solve and zero at sampled
beats afterward. That solved trial is discarded with its rejected body. It is not
an improvement claimed for the restored model.

Next model work must establish readable shoulder/upper-arm connections in rest,
relaxed pose, raised arms and side/back views before changing dimensions again.
The original shoulder joint is near x=0.0999; the rejected shirt extended to
x=0.159. Rotating shortened arms down around a joint buried that far into the
shirt made them disappear inside it. Do not compensate by endlessly shrinking
hands or flattening the shirt. Review the actual rig and connected volumes.


The draft imported and rendered, but the owner rejected the actual four-angle
image. Successful import was not successful art. The draft was never pushed.
Its model, palette mapping, derived arms and speculative wrist integration were
restored to HEAD `536c1c7` (gameplay/art at `ad91efd`). The restored GLB blob hash
is `ce6e7f1786e11baefdbfe39f9c2db8c7a3714204`. The failed source scripts, palette
and GLB are quarantined under `Logs/rejected-berto-2026-09-10`, outside the live
asset and authoring paths. Do not rerun them as the next design.

The draft had fifteen joints, but no new articulated motion had been authored.
The proposed offline IK, gait metadata and FPP articulation were candidates,
not completed systems. Reassess those needs against the existing blocky rigs.

The rejected first-pass thumb additions have now been removed from all eighteen
playable source models: 384 added vertices / 176 triangles per model, preserving
all animation sampler data. Roster/FPP regeneration succeeded. Berto
four-angle comparison was inspected in Unity; outline/arm/motion checks pass
14/14 in `Logs/block-hands-contracts-v2.xml`. See
`docs/reports/improvement-2026-09-10/block-hands-and-ink.md`. Character hull width is reduced from 0.008 to 0.0045 model units
(44% less expansion), with the original weld retained to avoid torn corners. Then refine Berto's existing angular forms,
not the discarded rounder recipe. Keep recognizable head size and a legible simple
expression, make shirt/shorts read as complete clothing, and use uncluttered shape
changes that remain visible at gameplay distance. Check each change from all four
angles and in motion before treating it as an improvement. No individual character
pass is complete yet. The other seventeen still require their own design work.

## 5. Eighteen-skill ledger

**Latest all-hero cultural request:** incorporate fitting Philippine folklore and
cultural references into all six kits and descriptions, without forced decoration.
English player-facing copy remains the rule; Tagalog only if genuinely needed.
The owner asked for thorough planning first and then full autonomous completion
while AFK. [PHILIPPINE_ABILITY_DIRECTION.md](PHILIPPINE_ABILITY_DIRECTION.md) records
researched sources, limitations, a distinct direction for every kit and English
copy drafts. Complete that plan before further cultural implementation; no optional
approval wait is required. Preserve phased Grand Coven and the raging ghost transformation.

Current uncommitted ice prototype: `tools/author_permafrost_models.py`, native
`MapSource/abilities/cheska/permafrost.blend`, three OBJ meshes under
`Resources/Models/Permafrost`, `FrostSurface.shader`, `FrostSurfacePresentation.cs`,
`PermafrostModelImport.cs`, GameBuilder shader inclusion and the SpawnIceSheet
replacement. It removes the platform/spikes/cutout in favor of thin fractured film.
The initial kerb failures are diagnosed and fixed: decorative colliders stayed
active until deferred destruction, and the 12.5 cm ground cache merged vertices
across kerbs. Decorative colliders now disable immediately; mesh draping caches
exact repeated XZ coordinates. `Logs/permafrost-play-v3.xml` passes 3/3, including
kerbs, all three maps and all three accepted Cheska casts. The look diagnostic
passes 1/1 (`permafrost-look-diagnostic.xml`). v3 stills show a thin blue cracked
film, no platform/spikes/cutout. Art and full kit completion remain open.

The toon shaders also now apply positional-light attenuation to the final lit
color, preserving the directional toon shadow floor. The red fixture measured
72% remaining light near its range edge; the final solid/transparent fixtures
measure 7.3% / 8.4% and zero outside range (`toon-light-falloff-green-v2.xml`, 2/2).
Actual map captures still show strongly saturated yellow clothing and dark hair.
Isolated no-camera-effect and ambient-only captures preserve the expected orange
skin and yellow Zack clothes; the map key and grade intensify them. Runtime skin
palette13, tint and flash inputs are normal. No palette rewrite is justified by
that capture alone; assess the warm key/grade during the planned map-lighting pass.
`Logs/permafrost-look-isolate.xml` passes 1/1. Temporary diagnostic toggles removed.
Foundation verification: Core 559/559, EditMode 472/472. The shader-stripping audit
caught FrostSurface missing from serialized GraphicsSettings despite the builder
list entry; both now include it. All fourteen gating source audits pass after that correction. This is a stable foundation batch, not completion of the eighteen skills.

Audio source analysis/preparation is possible, but the available model cannot
listen to tool-provided audio. No auditory approval is claimed. Original cues
remain unchanged in this batch. MP3 review copies are under Logs/cheska-audio-review.


**Latest explicit Phaister/Nemu brief:** all Phaister magic must improve. Her
ultimate should read as grand magic, with runes and a complex magic circle that
unfolds in distinct phases. Rework Nemu's ghost and skill presentation. Her ultimate
should turn the cute ghost into a giant raging/scary ghost that visibly sucks
everyone inward. The owner specifically renewed these requirements after earlier
attempts failed. Treat transformation and phased invocation as core deliverables.
Persistent character designs stay kept; this explicit ghost rework and temporary
ultimate forms are authorized. Trace the live companion/hero path so the intended
figure actually transforms; do not substitute an unrelated floor maw/column.

**Execution plan:** [ABILITY_REWORK_PLAN.md](ABILITY_REWORK_PLAN.md) contains the
owner's latest all-ability/model instructions, source inventory, reviewed findings,
per-action design direction, verification criteria and resume order. Read it before
further skill work. Character models stay kept; spawned skill models may be reworked.
Next: Permafrost Sheet art, then complete all three Cheska actions and the other kits.

### Current Nemu gameplay/FX work after pushed Kuro checkpoint a1ac87b

**New source findings that must not be lost:** GhostPetCompanion.UpdatePossession
currently has no map clamp or movement collision, integrates vertical bob directly
per frame, and applies a .35-second stagger every frame inside 1.6 m. Its 3*dt nudge
also loses to friction. The live possessed familiar pose/input has no route in the
network code (search of Runtime/Net and HeroAbilitySystem found none), even though
recast and ultimate placement read that local companion position. Fix the actual
networked possession route using the existing movement/ownership contracts, with
finite/bounds/speed/rate validation and host-only contact resolution. Do not claim
same-process witness footage is remote proof. Audit safe recall placement and
possession camera wall clipping too; the current camera has a fixed 2 m arm and no
collision query in ApplyCompanionPossessionView. These are existing-skill defects,
not a reason to add a generalized network framework or new mechanic.

Nemu kit-function v4 passes all seven tests: measured 2.6501 -> 3.1758 m/s actual
movement, held/newly-acquired slipper behavior, busy familiar charge preservation,
reset/denial without teleport, normal recall with teleport, and immediate reset
removal of the pull field. Ultimate duration now tracks its real seven-second field
so cancellation can own it. Core remains 559/559; fourteen source audits pass.
Full common-cancellation regression checks and the remaining Nemu work are open.

- Kuro's model/personality batch is pushed at a1ac87b. All eight authorable idle
  clips and native source are retained; human character models remain unchanged.
- Uncommitted Veil: actual 20% sustained movement gain through
  Balance.NemuPhaseSpeedScale, initial 5.5 impulse preserved, existing long-fade
  slow retained. A slipper held before casting no longer cancels it; a genuine
  new acquisition still ends the veil. No more 3*dt impulse erased by friction.
- Replaced the phase light/large bloom with spectral body/FPP edges and two thin
  motion trails. Rims restore and the detached trails fade on end. Fixed an initial
  MaterialPropertyBlock field-initializer exception by creating it in Attach.
- Retimed Nemu's authored ghoststep and matching FPP clip to .04 preparation /
  .10 release / .55 recovery; model/skin/material data remain unchanged.
- Possession cannot spend its charge while Kuro is feeding or returning. The first
  busy/return test passes. Reclaim-to-end Veil also passes. The first physical-speed
  route was contaminated by camera-relative steering/collision; the fixture now
  chooses movement aiming and a clear central lane, and measures actual displacement.
  Current run: Logs/nemu-kit-function-v3.xml, session 45976; collect its XML.
- New correctness finding: normal projection completion teleports Nemu, and both
  rollback and round reset previously called that same completion. Added a small
  OnCancelled hook to HeroAbility (default calls OnEnd); rollback/reset use it.
  Nemu projection cancellation ends possession/disposes the fallback WITHOUT
  teleporting, while normal recall/expiry still relocates. Add real-path tests for
  reset, denial and successful recast, then broaden verification across other kits.
- The live giant now has asymmetric grasping wisp motion, not a frozen T pose.
  Remaining Nemu work: full field/return lifecycle and overlap review, actual
  pull/rim escape measurements, possession arrival/model audit, SFX timing and final
  normal-speed owner/observer evidence. The other kits/maps/motion/build remain open.

### Current Nemu work after pushed Cheska checkpoint 1b83710

**Latest owner request:** Kuro must have idle animations and a visible personality,
with future trailers in mind. Make curious looks, sleepy hovering, playful turns,
attention/reactions to Nemu and recovery readable on the real rebuilt face/body/tail.
Keep ordinary play subtle. Preserve authorable/reproducible animation sampling for
trailer capture; do not rely solely on random fidgets that cannot be staged again.
Audit the existing seven fidget states before adding or replacing behavior. The
separate face parts now make eye/mouth personality possible. This is an explicit
part of the current ghost deliverable, not an optional future feature.

- Cheska's full local batch is pushed at 1b83710. It includes actual traction, all
  three visual/action passes, seven timing-aligned sound layers and the shared FPP
  held-preparation support. See reports/improvement-2026-09-10/cheska-kit.md.
- Nemu baseline actual three-cast capture: Logs/nemu-before-ghost-rework, 1/1 test.
  At full size the old ghost is a giant glowing placard with its unchanged cute
  face. The one-node/one-mesh GLB confirms the mouth/eye/tail posing could not bind.
- Current uncommitted native ghost: tools/author_kuro.py,
  MapSource/characters/kuro/kuro.blend, and replacement pet-nemu-ghost.glb at the
  same path/GUID. Nine meshes / ten nodes, separately named eyes/mouth/arm wisps/tail.
  The cute baseline has a chamfered lavender body, simple ink face and connected
  tapered wisps. Its person owner has NOT been redesigned.
- Current GhostPetCompanion changes unify runtime/review devour sampling, broaden
  the form, remove automatic spinning, use the actual court height and animate the
  now-existing face/arms. Remove old horn material logic; use owned body materials.
  CharacterVisual now gives the companion its own materials and thin body ink,
  with no outline around the little face planes, instead of Nemu's person palette.
- The initial missing-field replacement was fixed and the runtime run compiled.
  However, nemu-articulated-ghost-v1.xml passing 1/1 only proves accepted casts:
  visual inspection showed the old petless fallback. The rebuilt GLB changed its
  prefab fileID, so the roster PetModel reference must be refreshed. Existing
  RosterBookBuilder.Build completed and refreshed PetModel to fileID
  6209952159126781060 under the existing GUID. The only actual roster content diff
  is that reference; most other listed roster/arm changes are serialization noise.
- New KuroFormTests checks the REAL roster PetModel and named face/tail/arm targets,
  visible mouth/eye changes, a broad giant scale, return and source material safety.
  It passes 1/1 after the reference refresh (Logs/kuro-form-contract.xml).
  Do not accept a generic fallback as evidence of the ghost rework.
- This prototype is not visually approved or complete. Before further changes,
  verify the roster PetModel reference still resolves and all named targets exist.
  Review the real cute/transform/raging/return states, including FPP and three-quarter.
- Remaining Nemu kit: fix Veil's immediate cancellation while already holding a
  slipper and its erased 3*dt speed impulse; preserve acquisition-driven end and
  existing variant tradeoff. Rework phase/project/arrival cues and sound layers.
  Replace the remaining generic ultimate column/floor maw/light with the actual
  giant ghost plus restrained inward flow to its mouth, retaining truthful radius.
  Measure center/rim pull and escape before deciding whether values need adjustment.
  Preserve possession, return, owner visibility, authority and can immunity.
- Then Phaister's phased grand magic, the other kits and all remaining motion,
  maps/trees/gameplay/Windows delivery. The full plans remain authoritative.

- The v2 runtime cast capture passes 1/1 with the reference fixed. Its witness
  camera follows Nemu and can miss the independently located familiar after Astral
  Hijack. A TUMP_REVIEW_FAMILIAR=1 camera option now frames the actual ghost's front
  and bounds; use it for the next real cast capture. The owner view stays ordinary.
- Removed Nemu's generic light column. SpawnKuroUnbound now keeps its existing
  gameplay component/values but draws the real familiar plus a grounded thin reach
  ring and eight inward wisp packets. It has no old opaque maw/implosion card/light.
  Wisp source ground points are cached once. This needs visual and lifecycle review.
- Added the owner's explicit idle/personality work to the actual ghost. Existing
  seven gestures now share one sampler with live idle faces (blink/sleep/curiosity/
  cheerful mouth/wink), with exponential settling, no pirouette rewind and calmer
  reactions to Nemu's emotes. Fidget randomness is per-ghost System.Random and does
  not consume gameplay UnityEngine.Random. Capture sampling is deterministic and
  refuses to override possession/devour/return.
- KuroIdleClipAuthor.Build completed: eight named Unity .anim clips (hover plus
  seven gestures), baked from the live sampler into Art/animations/kuro-idles for
  future Timeline/trailer authoring. No new player mode or UI. The first bake had
  redundant dense constant curves (about 35 MB); the author now keeps identical
  endpoints for constant channels and every animated sample. Rerun the author
  after the current Unity run, then compare exported clips against sampler poses.
- KuroFormTests passes all three focused checks (kuro-idle-contracts.xml): real
  face/return/source-material safety, repeatable idle sampling/gameplay-random
  isolation, and all eight baked clips versus the sampler. Subsequent review
  found mirrored runtime eye/arm rotations made the rage face sad and wisps hang
  down. Their signs are corrected and a new assertion requires inner eye corners
  below the outer corners. Expired devour timeline sampling is now inert.
  The refreshed full EditMode run Logs/kuro-full-edit.xml passes 481/481.
- Personality/form v4 review passes 2/2: all eight idle sequences and the actual
  three-cast Nemu sequence. Logs/kuro-personality-and-form-v4 has timestamped stills
  and encoded silent MP4s. The familiar close-up optionally hides surrounding
  bodies only for its witness camera; the owner's view remains ordinary. These are
  art close-ups, not ordinary-play overlap evidence. The real companion is asserted.
  v4 shows a clear cute/sleepy face and broad toothed form. Corrected-form v5 passes
  1/1 and visually confirms inward-slanting angry eyes and outward-reaching wisps.
  Model/idle source audits pass all fourteen gates; audio retains seven informational
  flags. Checks.RunAll passes all eight (kuro-model-idle-checks.log).
  Nemu's remaining function/SFX/possession/pull work is not yet complete.
- Model/personality sampling, exported clips, actual cute/giant forms and source
  material safety are locally verified. Push this stable model/idle batch, then
  complete Veil, possession, pull/counterplay, cast motion and SFX. Do not treat the
  familiar work as completion of the full Nemu kit or the other five hero queues.

### Current uncommitted Cheska kit batch after a275138

- Native Blender wall source and four small mesh exports under Models/CheskaIce.
  Three separately drawn crowns replace rotated cubes and detached diamond toppers.
  Convex collision uses each exact rendered slab mesh; bases anchor to the lowest
  supporting corner. No persistent cyan light or physical decorative debris.
- Freeze restraint now uses five low shards and the existing full-body frost coat,
  leaving faces/FPP eye lines open. It follows the victim and ends when the actual
  Ice status ends, including mash-out, rather than always waiting the original timer.
- Cheska's ultimate no longer uses the shared nine-metre glowing column. Six tiny
  cold fragments gather during windup, then a short radial wave reaches the actual
  4.6 m blast radius and thaws. Existing blast/stun/shove rules are preserved.
- FPP held-aim preview added for existing placement actions. No extra cast/resource
  event; throw/lunge charge and active action priority remain. Instant releases can
  continue from the held preparation key. Thunderstrike retains its full windup clock.
- Cheska squash/stretch amplitudes reduced to 3.5/4.5/6.5 percent; authored weight
  carries her casts rather than a 20-35 percent body deformation.
- First actual-kit review accepted all three casts. Tests then exposed two fixture
  issues: test floor transforms were not synced before ground rays, and twelve
  same-frame mash calls were correctly throttled by the existing anti-spam interval.
  The corrected fixture syncs its floor and spaces real mash calls by .12 seconds;
  it still requires visible collider bounds and early restraint removal.
- Local visual/animation/audio verification now passes: EditMode 478/478 including
  held preparation/cancellation at 30/60/144 FPS and throw-charge priority; PlayMode
  cheska-ice-kit-v4.xml 6/6 including actual three casts, wall collision, escaped
  restraint and grounding. Checks.RunAll passes all eight. Fourteen source audits
  pass; the existing informational audio audit still flags seven of 119 cues.
- Isolated owner/local-witness captures are in Logs/cheska-ice-kit-v4-isolated,
  encoded at recorded wall-clock intervals with no interpolation (silent videos).
  The wall is distinct and transparent; the nova no longer bleaches its caster.
  Reduced the excessive combined torso/head backbend in wall/nova authored clips.
  Their GLB author reports unchanged model/skin/material data and preserved source
  binary, with grounded contact checks passing. This did not redesign Cheska.
- Seven sound layers shortened and shaped with preserved/lower peak and RMS budgets.
  tools/refine_cheska_audio.py rebuilds from the baked cues at a275138, not its own
  outputs. Mixed recorded/synthesised source provenance stays explicit. Timing and
  signal measurements are in the committed cheska-audio-timing.json; no listening
  approval is claimed. The old cast wall peak was .866 s, now .156 s; the nova
  preparation ends at its .4 s windup and the payload peaks at .011 s.
- Subsequent live-source review found IceSheetComponent still rotated its grounded
  root by 20 degrees/sec, which invalidated kerb conformance after the initial frame.
  Removed it and strengthened the kerb test to recheck vertices after one second.
- The old 5.5*dt ice impulse was erased by Friction=30. Current uncommitted correction
  registers per-sheet traction on the motor, keeps the existing slow/owner immunity,
  and approaches movement intent with 3.8 m/s2 acceleration while grounded on ice.
  Combat external-impulse friction and authored retrieval slides are untouched.
  Sources release independently on exit/destroy, including overlapping sheets.
  IceTractionProbe drives actual dry/ice stops and overlap/owner-immunity cleanup.
  Logs/ice-traction-play.xml passes 4/4. Actual dry stopping distance was 0.0000 m;
  on ice it was 0.2163 m, with input released, and overlap/owner-immunity cleanup
  passed. The dynamic kerb check also passes after one second. The authority audit
  now includes SetIceSurface and reports 49 sites, zero ungated other-body writes.
- The final refreshed EditMode run Logs/cheska-final-edit.xml passes 478/478.
  This locally verified Cheska batch is ready for a stable push; the all-hero and
  Windows release task continues. Remote and final ordinary-play
  qualification stay open for the complete task.
  Existing audio peaks occur at .866 s for a wall cast and 1.26 s for nova preparation,
  after their meaningful gestures. Plan to align preparation/contact/recovery using
  current licensed baked recordings, preserving peak/RMS headroom. Source downloads
  are absent in this checkout; do not claim unavailable stems were inspected.
  Audio listening is unsupported by the current model; never claim auditory approval.

### Current implementation batch: Zack function and grounded skill placement

**Latest art feedback:** the owner called the grounded ice skill ugly and renewed
the request to thoroughly improve all eighteen abilities. Keep ground placement
fixed but rework the raised platform, five decorative spikes and central upright
cutout into a convincing frozen street surface. Broader animation/SFX/VFX work
remains mandatory. Additional tools may be downloaded if needed; existing Blender,
Python and media tools are currently sufficient. No paid work has been used.

This correction batch passed broad verification and is being committed. Models stay at the kept
`7c7fcb5` checkpoint; no body or forearm study was integrated. Blender is closed
and the native input session was reset.

- Zack Bolt Sprint's per-tick 4 m/s² impulse was erased by the motor's 30 m/s²
  friction. A kit-owned wish-speed multiplier now grants 25% for its existing
  2.5-second active period. Initial dash, cooldown, trail radius/cap and slows
  remain. Real input measured 2.6565 -> 3.3206 -> 2.6565 m/s before/during/reset.
- Thunderstrike's old per-tick self-impulse was seen failing at 30/60/144 update
  rates: 5.5 m/s of added impulse over one simulated second, altering an incoming
  knockback. Removed that active-tail impulse. Existing seven-second charged
  throws remain. Do not claim it caused visible standing drift: ordinary friction
  can erase small per-frame impulses.
- Magnet now draws a narrow 0.22-second source-to-hand trace, not arcs toward
  nearby objects. Host equip remains immediate; the unused 0.45-second-flight
  claim was removed. Its owned trace/hand charge clean up on end/reset/consumption.
- Hand auras now attach to the measured HandAnchor when available. The old guessed
  arm-local offset was not the grip position. The anchor/lifecycle checks pass.
- `MagnetRecallTrace.cs` is new, including its Unity-generated meta. The first
  compile lacked explicit IVfxTimeline.LifeSeconds; corrected. A strict endpoint
  check found a floating-point endpoint offset; endpoints are now set exactly.
- The owner reported floating ground skills, especially Cheska and Sean. The
  marker snapped to the ground but the actual hazard retained caster Y. Red tests
  found ice/fire/wall at y=3 above a floor at 0.6, and ice at y=8 above a bridge.
- Existing VfxShapes ground sampling is now shared by actual placement and the
  reticle. GroundPoint makes full downward placement; small-piece GroundAt keeps
  its existing local clamp. Sampling excludes actors, slippers, can, rigid bodies,
  VFX and generated barriers, and prefers the actual map floor groups over the
  overhead guideway. Ceiling/wall normals are excluded.
- Eleven floor spawners now project to ground, including ice sheet/wall, fire and
  shock trails, magma/pillars, crater, hex, Kuro floor zone, coven and thunder.
  Explosion floor rings project separately; airborne blast cores retain their
  impact origin. Ice slab/rim/cracks, fire char, shock scorch/ring and crater bed/
  rim use existing mesh draping at 3 mm clearance across kerbs.
- Focused checks: `zack-motion-green-v2.xml` 10/10; `ground-skill-green.xml` 4/4;
  `ground-map-play-v1.xml` 3/3 including kerb mesh vertices, all maps and the actual
  sprint speed/reset. Ground rows: Eskinita .1000/.1000, Bayan .1000/.1000,
  Ilalim .0000/.0000 for both ice and fire. Three in-game images inspected under
  `Logs/ground-skill-review-v1`.
- `Logs/zack-skill-review-v1` contains accepted real-input captures of all three
  Zack actions and encoded full-speed owner/local-witness MP4s. Earlier PlayMode
  result 3/4 failed only on the exact trace endpoint; the trace now passes in the
  ground-focused rerun. This is not real network-observer evidence or finished
  Zack animation/SFX/ultimate quality approval.
- Capture finding: faces look strongly yellow in skill and warm-up images.
  Trace material/lighting and capture color handling before assuming all of it is
  electric light spill. UltimateColumn's filled flare and strong glow still merit
  individual readability/impact review. Do not tune blind from one still.
- Broad correction verification: Core 559/559; full EditMode 470/470; all fourteen
  gating audits pass (informational cue audio still seven flags / 119 files). See
  `docs/reports/improvement-2026-09-10/ground-and-zack-correction.md`. The complete final
  twice-through PlayMode gate, build and exact-player checks remain required.


Names below are read from current constructors. All eighteen have first-pass body
and FPP animation work; **all still need the owner's requested deep individual
functionality, SFX and VFX review**. Keep per-row receipts, not one blanket "polished."

| Hero | Existing skill | Saved ability ID | Deep pass |
|---|---|---|---|
| Sean | Flame Rush | `sean_skill1` | Pending |
| Sean | Ignition Cannon | `sean_skill2` | Pending |
| Sean | Supernova | `sean_ultimate` | Pending imposing-ultimate pass |
| Zack | Bolt Sprint | `zack_skill1` | Real sustained-speed correction verified; animation/SFX/VFX refinement remains |
| Zack | Magnet | `zack_skill2` | Recall trace and hand anchor/cleanup corrected; deeper presentation remains |
| Zack | Thunderstrike | `zack_ultimate` | Stale self-impulse removed; imposing animation/SFX/VFX pass remains |
| Dante | Seismic Stomp | `dante_skill1` | Pending |
| Dante | Demonic Carapace | `dante_skill2` | Pending |
| Dante | Titan Fissure | `dante_ultimate` | Pending imposing-ultimate pass |
| Cheska | Permafrost Sheet | `cheska_skill1` | Pending |
| Cheska | Ice Barricade | `cheska_skill2` | Pending |
| Cheska | Glacial Nova | `cheska_ultimate` | Pending imposing-ultimate pass |
| Nemu | Phantom Veil | `nemu_skill1` | Pending |
| Nemu | Astral Hijack | `nemu_skill2` | Pending |
| Nemu | Devouring Seance | `nemu_ultimate` | Pending imposing-ultimate pass |
| Phaister | Hex | `phaister_skill1` | Pending |
| Phaister | Shadow Blink | `phaister_skill2` | Pending |
| Phaister | Grand Coven | `phaister_ultimate` | Pending imposing-ultimate pass |

For every row review: purpose and utility; actual activation/aim/release path;
resource/cooldown/refusal; body and FPP preparation; active effect and collision;
sound layers/timing/distance; overlap; cancellation/interruption; recovery; owner,
observer and player-build behavior. Keep alternate loadout modifiers working.

### Source-grounded leads already found

- Zack's aimed Thunderstrike retains an `OnTick` that pushes its caster forward for
  its seven-second active period, apparently left over from an older overdrive.
  Trace dependencies and reproduce before removing the unintended drift.
- Magnet's actual equip is immediate, while its visual `SpawnCircuitArcs` radius
  is scaled by the entire recall distance. A connection from slipper to hand would
  communicate recall more truthfully than a huge radial field. Its unused
  `FlightSeconds` constant is not evidence that a real flight occurs.
- Sean Flame Rush uses a 2.2 m body-contact distance but a 1 m fire trail. Review the
  visible contact promise and actual swept travel, without blindly increasing reach.
- Flame Rush/Bolt Sprint currently have 50/46 second cooldowns. The owner permits
  functional improvements, but inspect actual usefulness, AI gating and map
  overclock modifiers before assuming shorter cooldowns solve weak skills.
- Held aiming can leave arms visually idle before the accepted cast. Improve
  preparation where useful, distinguishing private aim preview from an accepted
  warning. Do not accidentally cast, spend resources or confirm a refused action.
- Sean's first-pass ignition/ultimate captures include very pink-looking effects.
  Inspect the actual sheet/tint/shader path before calling it an intentional fire
  palette or a missing-shader bug. The diagnosis is not established yet.
- Look at source-specific cast, impact, loop and end cues. The existing eighteen
  cast cues and hero/ultimate themes are not all individually improved yet. Preserve
  licences and preferred can/UI audio; do not replace everything with generic noise.

## 6. Maps, chase and other open work

- **Latest tree feedback:** the owner calls the current trees ugly and expects
  better alternatives may already be installed. At the map stage, inventory the
  existing tree/nature assets before downloading or making replacements. Plan all
  three maps thoroughly, with appropriate tree silhouettes/materials/placement,
  architecture, lighting, retrieval routes and sightlines considered together.
  Replace or substantially improve the current weak trees; do not treat the first
  map pass as final. Keep the ability pass first and map rework afterward.

- Review all three maps from first throw, retrieval, defender reverse view and
  spectator view. Examine repeated architecture, prop grouping, physical/visual
  edges, ground geometry and every legal slipper resting place.
- Use real Philippine references. `world-direction.md` records the sari-store,
  Pila plaza/church and Gilmore/LRT references already inspected. Reference photos
  are not shipped textures. Keep sourced livery intact and the court readable.
- Bot lunge accuracy is low in current reports. Separate aiming/decision problems
  from player counterplay before changing balance. Source `DoHunt` and actual
  release/facing must be examined at normal speed. No lunge balance was changed.
- Historical bridge idle lead: one of six seeds had 48 penalties. The committed
  report does not identify that arm's individual seed and its claimed raw JSON is
  not tracked. Do not pretend it was reproduced. Seeds are 20260823, 1, 7, 4242,
  20260904, 99991. `BotBehaviourProbe` accepts `-tp-bot-seed`; the bridge method is
  `HeroStrikeBotsPlayAWholeMatchUnderTheBridge`. New reports include resting-shoe
  positions/owner state every five seconds continuously loose.
- A real 2,032-component arena query sample measured 0.0568 ms per slipper query
  over 10,000 calls. Allocation-counter support was unverified; zero returned is
  not a zero-allocation claim. Network seat/slipper resolution is already cached.
  Do not add a speculative registry without need.
- Continue the bounded request/duplicate/lifecycle review. The cue relay bug is
  fixed. No new request epoch or nonce framework has been implemented; do not
  invent a generalized network subsystem for an unproven hypothesis.
- Check ready, role swaps, interrupted final actions, results, rematch, spectator
  clarity and stale effects/input/animation ownership. Keep existing good contracts.

## 7. Verification, evidence and delivery still required

- Use one Unity editor process per checkout. **Never edit C# or imported assets
  while that checkout's Unity run is active.** Check processes before the next run.
  Blender asset writes happen with Unity closed. Read-only analysis/preparation can
  run independently. Unity asset-worker children are not a second editor session.
- Always launch Windows Unity verification through `tools/run_unity_guarded.py`.
  It snapshots/restores existing files under
  `C:\Users\Matthew\AppData\LocalLow\BH Studios\Tumbang Preso` and verifies hashes.
  Recent runs preserved nine files. Early pre-guard runs did not have a full initial
  snapshot; no loss was observed, but do not claim unmeasured original-byte proof.
- `Logs/player_profile.py` is prepared for built-player backup/restore but has NOT
  been run. Snapshot before actual player work; restore after all player processes
  exit. Avoid sending fabricated career results to a real account.
- Fresh nonempty XML, zero failures and expected fixture coverage establish a test
  pass. Do not use `-nographics` for PlayMode or `-batchmode -quit` as compile proof.
- Final candidate needs Core, EditMode, eight checks, source audits and the isolated
  PlayMode gate twice from the settled state. Prior e730878 results do not certify
  the newer cadence or subsequent individual model changes.
- Run appropriate WallClock evidence separately: actual action sequences, overlap,
  frame conditions, updated rig contact and performance. Inspect normal-speed
  motion; tests alone cannot establish feel.
- Build with `GameBuilder.BuildWindows` once the work is ready. Check for running
  players and the validated output before purge. Verify exact executable/data
  timestamps, build identity and hash, then launch that exact executable.
- Exercise both modes, keyboard/mouse navigation, relevant controller-focus coverage,
  localhost host/joiner/observer and appropriate shaped-link/lifecycle paths.
  `tools/net_matrix.py` / `net_link.py` are the existing real two-process harness;
  validate output paths before its recursive cleanup. Do not use the obsolete
  UnityTransportDebugSimulator approach.
- Measure available Windows hardware/settings, live frame distribution and ordinary
  play at Low/Balanced/High where relevant. The machine has an i5-13420H and RTX 4050
  Laptop GPU; do not infer VRAM from WMI's inaccurate AdapterRAM field.
- Keep versioned before/after images and full-speed ordinary-play footage. The v5/v6
  observer is a second camera in the same process, not remote-client evidence.
- Push substantial stable batches to ASTRAReworks. Update TODO in the same commits,
  archive completed numbered sections whole and retain index pointers. Use sole-
  author commit messages from files, no coauthor/tool attribution or new em dashes.
- Final chat must report pushed checkpoint, material improvements, actual build/test/
  play results, limitations and any genuinely remaining decisions. No claim of human
  taste approval, physical-pad testing or Android testing that did not happen.
  Any further handoff belongs directly in chat; this file is the requested active
  plan, not a reason to stop work.

## 8. Useful evidence and tool state

- `docs/reports/improvement-2026-09-10/`: first-pass cast/model/map evidence, v5 body
  and FPP reels, coverage CSV, cadence comparison and reports. The owner rejected
  the overall model refinement shown in `all-playable-characters-v4.jpg`.
- `Logs/improvement-baseline-v1`: original carry/ordinary-map baseline.
- `Logs/improvement-after-v5`: all eighteen accepted casts, owner and local witness,
  four eye views per map and ordinary play. Earlier v4 witness frames included
  private FPP meshes; do not use those to claim attachment quality.
- `Logs/improvement-cadence-v6`: corrected-cadence carry and ordinary-map captures.
- `Logs/cast-finish-v2`: old four-angle sheets for twenty retained rigs. They are
  pre-individual-rebuild evidence, not the requested final cast.
- `Logs/shots-fpp/*v22.png`: first-pass matching arms before individual redesign.
- `tools/encode_motion_evidence.py` preserves recorded wall-clock intervals without
  interpolating motion. Those automated videos are silent.
- Read the computer-use skill before native control. It was read in this task.
  Use `@oai/sky` through the node_repl tool and fresh returned windows. No separate
  control-release API was found; stop inputs/recording and reset the JS session
  when done. The node_repl kernel has currently been reset; OBS is closed.
- OBS recording preparation: separate profile and collection
  `TUMP Review 2026-09-10`, 1280x720 at 60 FPS, output `Logs/recorded-player`.
  Its only source is specific-window Game Capture with game audio; the game window
  is not selected yet. No recording has started. Original `Untitled` collection/
  profile contents remain; restore that selection after finishing OBS work.
- `HeroTurnaroundProbe.RunOne` provides versioned individual four-angle review.
  Cell spacing now matches the 1.16-unit vertical view; the previous 1.0 spacing
  offset subjects in nominal 600px cells. This is evidence tooling, not product UI.

## 9. Immediate continuation

1. Stop the model redesign work. The owner likes the cleaned cast and explicitly
   asked to move on. Current live models are `7c7fcb5`, not the rejected bodies.
2. Complete animation and footwork refinement, then all eighteen deep skill/SFX/VFX
   rows, including genuinely imposing but readable ultimates. Use the existing
   authored rigs and verified contact/authority paths. Do not spend another batch
   changing Berto's outfit or body silhouette.
3. Complete remaining map/chase/gameplay/feedback scrutiny and Windows verification,
   build, exact executable checks, comparable full-speed evidence and push.
4. Preserve the pending character-selectable evidence probe change; exercise it in
   the animation work before committing. The native Blender review was read-only
   with respect to the live assets. The forearm study exists only under Logs.

The owner has explicitly asked not to lose important planning across compactions.
Before a context boundary, update the current in-flight state and next concrete
steps here. Resume the same task; do not restart or silently narrow its scope.


## Latest explicit networking request, 2026-09-10

The owner explicitly says networking is broken and asks for a thorough fix. Treat
this as required release work, not an optional polish item. Verify separate
host/joiner/observer processes, actual skill/familiar positions and contacts,
seating/ownership, denied or repeated requests, reconnects, round/rematch transitions,
stale effects and authority. Include the proven missing controlled-familiar motion
route and the projection cancellation fixes already in progress. Keep both modes,
four players, saved IDs and profiles. Use existing transport/movement contracts,
finite/bounds/elapsed-distance validation and a protocol bump if the wire changes.
Do not claim same-process witness evidence verifies LAN. Network work remains in
this same task; no new task, subagent, paid service or usage reset is authorized.

### Nemu function checkpoint before familiar networking

- Fresh `nemu-kit-complete-cycle-v1.xml`: 11/11, including ten real-input
  contracts and all three complete cast/recovery captures. Intake measurement:
  player 1.4960 -> 0.4786 m, loose slipper 0.8242 -> 0.1200 m; can unchanged.
- Full EditMode 480/481: remaining failure was description length, not behavior.
  Shortened both inspected descriptions and the ultimate summary; focused copy
  verification is running. Do not mark full suite passed before reading it.
- The model/cycle witness is an isolated familiar camera, not ordinary overlap or
  network proof. Current witness frame35 has readable teeth/angry eyes and reach;
  map lighting/sky saturation still needs the planned broader pass.
- Networking source confirms controlled Kuro never transmits position. Also,
  BeginPossession adds temporary AI even to remote human replicas, potentially
  changing body authority. Use CharacterMotor.IsLocallySimulated for ownership.
- Revised implementation direction: unreliable sequenced familiar pose stream,
  just like existing body poses, plus a reliable final anchor in the ability
  transaction. Do not put the continuous stream behind reliable retransmission;
  MatchRpc documents a previous actual head-of-line blocking regression.
- Validate seat, current round, active possession, finite pose, court, collision,
  floor height and time-accrued travel. A reliable final anchor must be captured
  BEFORE local reactivation destroys possession. Broadcast the accepted anchor
  with the cast so observer R/E uses the same position. Budget starts at cast,
  not the first received movement. No per-packet distance gifts.
- Keep cancellation/reset fixes separate and commit the locally verified Nemu
  function/audio batch before changing the protocol. Separate-process networking,
  reconnect transient restoration and all remaining original scope stay open.

Copy follow-up `nemu-copy-edit-v2.xml`: 24/24 passed after shortening text.

### Network implementation in progress, after 74664f4

The local Nemu function/audio batch is pushed at 74664f4. Protocol 25 changes are
uncommitted and must not be called release-ready. Implemented locally: a 20 Hz
unreliable-sequenced familiar pose pair; final familiar anchor in reliable
ReqAbility/PlayAbility; floor/court/collision and elapsed-distance validation;
local-only temporary body AI; non-simulating host/observer familiar replicas.
Cast source position is now captured BEFORE local teleport, which matters for
Phaister blink too: the old request reported its destination as its source and
could fail the host's 2.25 m intent plausibility check.

Also found and fixed delayed OnActivate dropping the received cast context and
using the replica's live aim after windup. The accepted release position/forward/
aim now survives the delay. Kuro holds still during ultimate invocation. Countdown
announcements now explicitly require the host, matching other play messages.

Core 562/562 and unchanged local Nemu 10/10 pass. Wire shape: 64 messages, zero
mismatches; request paths: 61, zero unreachable; numeric wire fields: 68, zero
findings. Added actual remote-body ownership, rejected-flight and delayed-aim
contracts; `familiar-network-contract-v1` is running. Check fresh XML.

Remaining before calling network repair done: owner confirmation/correction of
accepted ultimate anchor at high latency; actual host/client/observer flight,
recall, denial and cast traces; latejoin/reconnect active effects; remote body
teleport/interpolation state; disconnect/round/rematch. A temporary verification
player under Logs may be necessary for real processes; it is not final delivery
or a replacement of the Desktop build. Final Windows delivery still waits for
remaining hero/motion/map work.

Map preparation while tests run found reusable tree models already installed in
Art/models/kits/city (small/large), forest (tree/tree-high), town
(tree/tree-high/tree-high-round/tree-crooked). Review those assets in engine before
choosing replacements; do not download another pack without inspecting them.

### Further network findings and verification preparation

- `familiar-network-contract-v2.xml` is 15/15. It adds genuine host-replica
  ownership, zero-input, invalid-flight, preserved delayed aim and teleport-target
  checks. Core remains 562/562; fourteen source gates pass before snapshot changes.
- Found four more host announcement handlers lacking sender guards: Seating,
  RematchTally, BeginRematch and RebindSeat. Added the same host-only sender and
  host-loopback checks used by adjacent messages; host call sites already run the
  local action directly. This is explicit authorization, not merely finite data.
- Added a reliable FamiliarEffect message for the actual ultimate anchor/lifetime
  and explicit latejoin snapshots. Flight remains unreliable. NGO's ServerTime
  removes travel time from the effect's remaining lifetime. Owner and observers
  can rehydrate Kuro and the actual pulling field without spending a charge or
  replaying a full fresh cast. Full EditMode `network-foundation-edit` is running.
- Need verify this snapshot code after compilation, including duplicate state,
  late join, no teleport when replacing possession, exact floor and expiration.
  Known review point: an existing predicted field has its own scheduled Destroy;
  a restored longer clock must also correct that field's lifetime rather than
  only the ghost/ability timer. Do not leave that mismatch for the final player.
- A scripted three-process familiar trace is being prepared, using real input on
  the owning client and independent host/observer CSVs. Use isolated profile
  backup/restore around all processes. Avoid interpreting NetworkMultiProcessProbes
  as actual processes; despite its name those tests simulate state in one editor.

### Exact verification state for the current protocol work

Full EditMode `network-foundation-edit.xml`: 481/481. Familiar contract v1 after
snapshot compilation: 15/15. New snapshot tests are running in
`familiar-effect-contract-v2.xml` (17 expected). The predicted field now rebuilds
on authoritative confirmation, inactive old root first, so its scheduled Destroy
agrees with the restored ghost clock. Repeated-state and no-teleport/resource
contracts cover this. NGO ServerTime supplies the expiry clock.

Prepared `NetFamiliarProbe` and `tools/net_familiar_matrix.py` for actual three
process traces. The fixture is explicit, guarded as tournament modifier
NetFamiliarProbe.Active, default off; its command line selects Nemu, parks
bystanders and drives only the owning client's real input. No ordinary game/UI
flow reaches it. Its two cases are recall and ultimate during possession; no
mid-run cooldown reset is needed. The runner launches hidden/batch players,
optionally shapes only the owner's link, compares independent trace endpoints,
actual field count and expiry, and backs up/restores existing profile files.

Next: read fresh 17-case XML, fix any actual failures, run Checks.RunAll, build a
verification-only Windows player under Logs with GameBuilder -buildOutput and run
both cases clean and delayed. Do not replace the Desktop build or call this final
shipping; the broader ability/map/motion scope is still open. While Unity builds,
prepare/review Phaister geometry and installed map assets, without imported edits.

### First actual protocol-25 verification player

Snapshot contracts pass 17/17; Checks.RunAll passes all eight; all fourteen source
checks pass. GameBuilder succeeded in 77 seconds, 995 MB, nine scenes, at
Logs/network-verification-v1/TumbangPreso.exe. This is an internal verification
player, not final delivery; Desktop output was untouched. Build log reports
protocol25 and dirty tree, but unexpectedly no SHA. Investigate that identity
lookup before final release; use the executable hash in the trace report meanwhile.

Three actual processes are now running recall case under
Logs/familiar-recall-clean-v1, orchestrated by tools/net_familiar_matrix.py.
Fresh result.json and independent CSVs, not process survival, determine success.
No native control or recording is active. All player processes and profile restore
are owned by the script; collect its result before edits or another live run.

Installed-tree review used Blender without changing source assets. All six
reviewed candidates are conical evergreen forms or single teardrop canopies,
including town/tree-crooked. They do not solve the requested Philippine urban
shade-tree silhouette. Prefer an authored branching broad canopy for plaza shade
and smaller street forms rather than swapping one cone for another. Reference:
https://ncr.denr.gov.ph/news-events/denr-ncr-declares-new-heritage-tree-in-metro-manila/
identifies established Metro Manila heritage species including Narra, Acacia,
Mango and Rain Trees. The design inference is to use fitting shade-tree forms,
not claim all these species are native. Native trees and introduced rain trees
must not be conflated in copy.

## Latest ability and loadout scope expansion, 2026-09-10

The owner explicitly authorizes changing or replacing boring abilities, as well
as improving their presentation. Every hero's loadout alternatives must differ
enough to have a real use case and deserve their slot. This supersedes the earlier
blanket restriction against changing existing ability mechanics where it would
prevent the requested kit rework. Preserve four players, rotating defender,
can/slipper throw/retrieval, both modes, six hero identities and the existing
loadout structure. No extra modes or feature systems are requested.

Before implementing each kit, record the base action and each existing alternative:
its distinct tactical job, attacker and defender use case, readable tell, response
available to opponents, cost/opportunity cost and cancellation/network contract.
Compare alternatives side by side. A tiny statistical difference or cosmetic
recolour alone does not satisfy the latest request. Replace a redundant option
within its existing slot rather than accumulating more abilities or complexity.
Review actual play and loadout descriptions together, in English with restrained
Philippine cultural grounding. Keep all existing IDs/save migration obligations
explicit if an ability's function changes. Animation/FPP/VFX/SFX must communicate
that function, especially imposing but readable ultimates.

Finish the currently exposed network failures first, then execute this expanded
six-kit review and remaining animation/map work. Do not quietly return to the
old presentation-only interpretation after compaction. No subagents or new tasks.

### Actual three-process failure and fixes in progress

The first recall trace FAILED, honestly: no process reached sustained possession.
Do not count it as network acceptance. It exposed a real OverflowException in
BroadcastLobbyPicks, where four variable-length player records were written into
512 bytes. The fix sizes that buffer from actual UTF-16 encoded strings and uses
reliable fragmented delivery. Identify and SelectLobbyPick had the same fixed-size
string-payload pattern; they now size their own data and fragment reliably too.

The first run also showed simultaneous settings.json.tmp writes despite distinct
-tp-profile values: only network authentication/token salt was isolated. New
ProfilePaths routes settings, career and social files into a stable hashed
subdirectory for explicit launch profiles. With no profile argument, the exact
existing default paths remain. Save/profile preservation remains required.

Fixture correction: pin Hero Strike rules and have the host stage the intended
Nemu fixture and broadcast its pick when the installed seat differs. The initial
fixture waited for Nemu without ensuring its late-installed pick, producing empty
CSV evidence instead of a false pass. All three first-run processes were stopped
and all ten pre-existing persistent files restored by the runner.

Build identity no-sha cause confirmed: this checkout is a linked Git worktree.
HEAD is in its worktree gitdir, while refs live in commondir. HeadSha now resolves
that file for loose/packed refs. The separate checkout's content was read only;
none of its unrelated user changes were touched.

Full EditMode network-roster-edit-v1 is running after those fixes. Add direct
regressions for long/unicode roster payload sizing, isolated/default profile paths
and linked-worktree SHA; then rebuild the verification player and rerun traces.

### Remaining flight-stream latency correction

Before the next verification build, distinguish accepted flight echoes from real
host corrections. The first stream patch applies a received owner pose whenever
it differs by >1.5 m. At Kuro speed a healthy delayed echo can exceed that distance;
rewinding to every old accepted echo would turn ping into a movement penalty.
Transmit a correction flag on SyncFamiliar. Owner ignores accepted echoes and
applies only explicit rejections/corrections; observers interpolate both. Keep
server time-accrued distance and collision validation. This is still protocol25,
which has not been pushed/released. The actual delayed-link trace must prove it.

New full EditMode network-roster-edit-v2 is running. The first pass found the
new diagnostic modifier was absent from TournamentGuardTests' explicit setter/
reset fixture, and the payload regression needed NGO/Collections test assembly
references. These were corrected without weakening the nine-modifier assertions.

### Verification player v2 in flight

Full EditMode network-roster-edit-v2 passes 485/485, including long Unicode roster
allocation, explicit/default profile isolation, linked worktree loose/packed refs
and the complete nine-modifier guard coverage. SyncFamiliar now carries an explicit
correction bit: the owner ignores accepted delayed echoes, while observers still
interpolate them. All 65 wire messages have matching field counts.

The second verification-only build is running under
Logs/network-verification-v2/TumbangPreso.exe, log network-verification-build-v2.log.
Read the fresh success/identity, then run net_familiar_matrix recall and ultimate
again. v1 traces remain failed evidence, not a baseline pass. No native PC control
or recording active. Continue expanded all-six kits/loadouts and remaining maps/
motion after the active network failure is resolved.

### Verification v2 built and running

The second verification player built successfully in 37 seconds, 995 MB, and now
correctly stamps 74664f4ea446175e29c38b31ec58acb947d6780d + dirty, protocol25.
Linked-worktree identity is fixed. Recall clean v2 is running actual three-process
trace. Core 562, EditMode485, Nemu PlayMode17 and all14 audits are the preceding
verified counts. Do not call the actual trace passed until result.json says so.

The whole-kit/default-alternative review is now in [HERO_KIT_REWORK_DECISIONS.md](HERO_KIT_REWORK_DECISIONS.md). It records all twelve choice pairs, source contradictions, candidate replacements, lore boundaries and actual-play acceptance. Candidates are not marked implemented or balanced.

### Actual clean familiar network results

Recall clean v2 PASS: host/owner flight span6.0401 m; observer6.0491 m. All three
final bodies (1.1757,-1.6029), zero recorded endpoint error.
Ultimate clean v2 PASS: all three actual field centers (0.6378,-0.8670), zero
endpoint error; expiry times spread46 ms; no duplicate/live-leftover field.
Owner remains at its original body after R, as required (ultimate is not recall).
Executable launcher hashes are shared by Unity builds and cannot identify code;
the runner now also records TumbangPreso.Runtime.dll SHA256. V2 runtime hash:
dffbf5fd3753a06ec0d31e34a1f3acf3534a2537b50bbd15861e2343c8fa7473.

Now running familiar-ultimate-300ms-v2: three real processes, owner link150 ms each
way with2% packet loss. Do not claim it passed until reading result.json. Reconnect,
all-other-kit effects, ordinary play/mode matrices and final release still open.

### Actual delayed ultimate result

familiar-ultimate-300ms-v2 PASS:150 ms each way,2% configured packet loss, three
actual processes. All three field centers (-0.6184,15.4216), zero recorded endpoint
error, expiry spread14.5 ms. No duplicate field. This tests a delayed ultimate,
not all networking. Note the fixture staging body spawn was corrected by incoming
initial poses on this delayed run; it still exercised a real six-metre controlled
flight and common field anchor, but not a matched art-camera position.
Now running the same link with recall in familiar-recall-300ms-v2.

Further source audit: SubmitMove/SyncUnit carry no round number, so a stale pose
can cross a round reset. Also review all client announcement sender guards and
reliable ability requests being overtaken by predicted body poses, especially
Phaister blink. Do not mark broad networking done after familiar-only success.

### Delayed recall failure and movement epochs

Delayed recall v2 FAILED by6.53 m; keep the red trace. Old accepted body echoes
rewound the owner after its predicted recall; old owner movement then overwrote
the host's recall. Its old spawn-settle target also resurfaced when the owner left.
Clean recall and both ultimate traces do not cover this ordering failure.

Implemented next: reliable Teleport message plus a per-seat movement epoch in
SubmitMove and SyncUnit. Host teleports start a new epoch; old-epoch movement is
rejected. A locally predicted ability teleport holds its outgoing body stream and
ignores old echoes until the host confirms a newer epoch. CastDenied carries the
host pose/epoch so a denied teleport resumes at the real source without a free
warp. This preserves responsive local prediction while making the discontinuity
an explicit accepted event. Normal Nemu timed recall marks the same pending path.

Added host-sender validation to the remaining fifteen state announcements without
changing their existing loopback behavior. Current wire audit:66 messages, zero
mismatches;74 numeric fields, zero unchecked. Nemu epoch contracts are running in
network-teleport-epoch-contract.xml,18 cases expected. No epoch player built yet.

Before rebuild: preserve owner look direction when applying a position-only
teleport correction; ensure only the registered live unit announces teleports;
strengthen recall trace acceptance to compare the destination against last flight
and sample before the owner quits (host bot takeover is not the recall endpoint).
Then rebuild verification v3 and repeat delayed recall and ult. Do not declare
network done or leave this failed case unresolved after compaction.

### Epoch player v3 and reconnect runner ready

Epoch contract passes18/18. The v3 verification player built successfully in35 s,
995 MB, identity60dea16+dirty, protocol25. Owner view yaw is preserved across
position corrections, and only a registered live unit announces a teleport.
Recall acceptance now checks proximity to the last controlled familiar position
and samples before owner shutdown, so mere agreement back at the source cannot
pass. Delayed recall v3 is running in familiar-recall-300ms-v3.

The runner now supports --case ultimate --reconnect: after the owner casts a real
field, kill and reopen that same profile, use the observer-only fixture path, and
verify the reclaimed seat, recreated field, expiry and retained resource counts.
The observe path neither grants charge nor teleports or presses skills. v3 includes
that path and CSV charge fields. Run it after delayed recall finishes. No native
control active; existing profile files are restored around every row.

### Correct the shaped-peer fixture before accepting v3

The first v3 recall endpoints agree, but inspection caught the delayed connection
joining second: owner.csv LocalSlot2, observer.csv LocalSlot1. Thus that run shaped
an observer, not the actual driver, and is NOT delayed-owner acceptance despite
its old result.json being green. The runner now waits for the delayed owner to
actually occupy seat1 before launching the observer, and fails incorrect seats.
Repeat as familiar-recall-300ms-v3b with the same built runtime. Do not quote the
old v3 row as passing the intended latency case.

Additional source findings for continuing network work: host-only impulses applied
to remote human replicas never integrate, because FixedUpdate exits into replica
smoothing, and no impulse RPC exists. Add an explicit authoritative one-shot impact
route at resolved impact sites; do NOT blindly relay all ApplyImpulse calls because
caster prediction and continuous locally applied Seance pull would double. Verify
an actual remote victim. Also inspect the observer's post-recall pet/model anchor:
physical body and familiar follow target separated in v3 CSV; CharacterVisual binds
Kuro to the rendered person instance, not directly to the motor.

### Intended delayed recall now passes

familiar-recall-300ms-v3b PASS with verified owner in seat1 and observer in seat2,
150 ms each way and2% configured loss. All three observed6.0908 m controlled
flight and agreed on recall body(1.1791,-1.5714), zero recorded endpoint error.
Runtime hash2efffcdb25d625b05fb324c1dafd82ce4e7d33812e13cde36e698c47c85c0625.
Now running actual owning-profile reconnect during ultimate in
familiar-reconnect-clean-v3. Collect that before edits/another live run.

Observer model/pet anchor suspicion is source-grounded: CharacterVisual's
AlignToCapsuleFloor stores its current modelRoot.localPosition as _alignedLocal,
which may include a temporary remote-smoothing XZ offset during model rebind.
That makes the transient offset permanent. Remove smoothing before measuring and
retain the authored alignment baseline; add a regression and capture ModelRoot
position beside the actual motor in the multi-process trace. Snap the rendered
root on a confirmed teleport, while preserving ordinary smoothing. This is an
alignment repair, not restarting the stopped character redesign queue.

### Actual reconnect v3 is red

familiar-reconnect-clean-v3 FAILED. Rejoined owner wrote no gameplay rows and hit
MaxConnectionAttempts. Observer initially held seat2, then reset itself to slot0
when another peer left; its scene teardown also exposed a fixture null check using
?. on a destroyed Unity object. No reconnect success is claimed.

NetSession.OnClientDisconnected only special-cases a host observing another peer.
A CLIENT observing another peer falls into its own disconnect cleanup, clears its
seat/lobby and announces MatchAbandon. Check NGO callback identity semantics and
filter unrelated peer notifications without suppressing the actual host-loss path.
Read the real logs and re-run the exact reconnect case after fixing it. Also add
non-secret approval-reason logging if the replacement client's initial transport
failure remains unexplained. Preserve identity security checks.

### Reconnect diagnosis correction and model regression in flight

The foreign-disconnect-callback explanation was NOT confirmed. Installed NGO
ClientDisconnectedMessage invokes OnPeerDisconnectedCallback for other peers;
OnClientDisconnectCallback is local connection loss. Observer's log says actual
ClosedByRemote while host was still alive. Do not add a guessed filter and claim
this is fixed. Added non-secret approval/arrival/replacement/disconnect-ID logs to
NetSession for the next reproduction. The reconnect failure remains open.

Host/owner/observer first CSV times16.0417/16.1441/16.1448; host last40.0004,
observer last31.2735. No host rendering stall (largest gap77 ms). This rules out
both the guessed early host timeout and an eight-second render freeze. New arrival
has no gameplay rows and reaches transport MaxConnectionAttempts. Trace its real
approval/transport path on the next build.

Implemented the independently evidenced visual alignment fix: SetModelRoot/Awake
remember the base offset; AlignToCapsuleFloor removes temporary remote smoothing
before measuring, and accepted teleports snap the rendered root. Forced replica
poses also update/clear stale spawn-settle targets, preventing a later ownership
handover from reviving an old teleport. Probe now captures modelX/modelZ beside
bodyX/bodyZ and safely checks destroyed Unity objects. Nemu/model contract run
network-model-alignment-contract is active,19 cases expected.

### v4 alignment/connection diagnostic build

Nemu/model alignment contracts pass19/19. Verification build v4 is running with
non-secret connection approval/arrival/replacement/disconnect logs and model-root
CSV fields. No speculative peer-callback filter was applied. Reconnect v3 remains
red and source still needs a real cause; preserve the record of the disproven
hypothesis above. Next rerun actual reconnect with v4, inspect exact IDs and
transport outcomes, then verify model-root/body agreement and delayed recall.

The source, protocol and model-alignment changes are still uncommitted after the
pushed plan checkpoint60dea16. Preserve them and do not restore broad paths. Known
Unity-generated noise is only RosterArms tangent serialization, QualitySettings,
ProjectAuditorSettings and line-only ProjectSettings, which must be checked before
restoring. After stable verification, commit/push this significant networking batch
with TODO in the same commit, while keeping unfinished wider network and all
hero/loadout/map/animation scope open.

### v4 reconnect still fails before approval

v4 direct reconnect is also red. Instrumented host sees peer0/1/2 approval and
arrival, then disconnects1 and2; there is NO approval/arrival for the replacement
client. Observer's callback is peer2/local2, so it really lost its own connection.
The earlier foreign-callback theory is disproved and no such filter was added.
Host stays alive and rendering throughout. The replacement reaches transport
MaxConnectionAttempts. No identity security check is being relaxed.

Isolating transport behavior next: familiar-reconnect-proxy-v4 runs the same case
with a1 ms persistent UDP proxy on the owner link, so killing the owner does not
close the server's immediate UDP endpoint. This can distinguish direct endpoint/
ICMP or transport reconnection behavior from application identity/field recovery.
It is an isolation experiment, not a workaround offered to players. No result yet.

### Receive-pool defect found in installed Unity Transport6.5.0

The repaired proxy now keeps receiving after the old client closes; proxy-v4b
reclaims seat1 as peer3 replacing peer1. The remaining proxy case failure is active
field reconstruction, not admission. Physical/model offsets are now zero in the
settled host/observer trace. The proxy's own Windows UDP reset handling is fixed
in tools/net_link.py; do not mistake the earlier dead proxy thread for game proof.

Installed package source UDPNetworkInterface.ReceiveJob acquires buffers through
PacketsQueue.TryAcquireBuffer, but on Failed completion or invalid byte count it
continues without ReleaseBuffer and without enqueueing the buffer for later
release. A dead Windows UDP endpoint can generate failed receives; empty datagrams
are a deterministic way to exercise the same retirement. After enough discarded
completions the driver has no buffers left to schedule, matching a host that keeps
rendering/sending yet cannot receive a reconnect or other client traffic.

Added actual local-UDP EditMode regression TransportReceiveRecoveryTests: send2048
empty packets in bounded batches, then require a legitimate transport connection.
Unpatched run transport-empty-before is in flight. Read its fresh XML before
embedding/fixing the package. Do not edit ignored Library as the durable fix.

Proposed minimal durable fix: embed the full installed com.unity.transport6.5.0
package (291 files,1511KiB; Runtime/Editor/docs/samples/license retained) under
Packages/com.unity.transport, return buffers on failed/invalid/failed-enqueue
completions, document the exact patch and upstream version. Baselib's completion
result carries the original requestUserdata, identifying the acquired buffer.
Unity Companion License notice is preserved; official current license reviewed
https://unity.com/legal/licenses/unity-companion-license . This is a Unity-dependent
game, no paid service, upgrade or engine-version change. Re-run empty-packet test
and the exact direct reconnect case; do not claim causal repair from static review.

### UDP regression reproduced and durable package fix under test

transport-empty-before.xml FAILED1/1 exactly at legitimate connection after2048
empty UDP datagrams. The installed package's receive-pool retirement is reproduced,
not just suspected from source. Embedded the complete6.5.0 package under
Packages/com.unity.transport and pointed manifest there. Only its runtime
UDPNetworkInterface.cs has behavior changes: ReleaseBuffer on failed completion,
invalid size and failed enqueue. Upstream file SHA is
9ccdca3a402e3c56e39eb8a3431e700afc38544bb4b4cf16cb3b876b82358e01.
TUMP_PATCH.md records source/version/license and the regression. The fixed run
transport-empty-after is in flight. No engine upgrade or paid dependency involved.

Proxy-v4b demonstrated seat reclamation works once the proxy's ICMP-reset handling
is fixed: peer3 reclaimed seat1, replacing peer1. It still missed the active field.
Therefore direct transport receive recovery and active-effect restoration are two
separate defects to verify. Next read fixed UDP XML, then build v5 and repeat the
DIRECT reconnect case before changing its target/timing to hide the failure.

### Receive-pool regression passes after patch

transport-empty-after.xml PASS1/1 with exactly the same2048-empty-datagram then
legitimate-connect test that failed before. The embedded receive-buffer repair is
therefore measured against actual UDP, not merely source structure.

Found active snapshot ordering: HostLateJoin calls HostSyncPeer before the later
HandleIdentify BroadcastPicks. A cold client therefore receives ability/effect
state while still holding its temporary default kit, then rebinds the intended
hero and loses that state. HostSyncPeer now sends the reliable SyncPicks first,
then rebind/world/resource/familiar snapshots. The larger lobby roster is on a
separate fragmented stream, so relying on it arriving first was not safe.

Next verification build v5 includes the transport fix and explicit kit-before-state
ordering. Repeat DIRECT owning-profile reconnect, then delayed recall/ultimate.
Core/whole EditMode/Checks must be rerun on the eventual stable source, not claimed
from the earlier v2 tree. The broader one-shot impact replication gap remains open.

### Wider networking work that must survive this focused repair

- One-shot knockbacks on remote human victims are still unreplicated: the host
  writes ApplyImpulse to a non-simulated replica; no impulse RPC exists. Add a
  dedicated host-resolved impact event at one-shot victim sites, never a blanket
  relay in ApplyImpulse (would double caster prediction and continuous local pull).
  Reproduce with host Dante Q against an owning-client victim, measuring actual
  displacement, then keep that separate-process case. Core shove needs the same
  examination. Keep score/contact resolution host-only.
- RebindKitIfHeroChanged returns on matching kit TYPE alone. Audit whether an
  arriving player's non-default loadout is consequently ignored on the host's
  pre-existing same-hero bot. Fix without resetting live cooldowns for cosmetic
  updates. This directly matters to the newly expanded useful-loadout request.
- All other kits need active-effect latejoin/cancel cleanup as they are reworked;
  the explicit familiar snapshot is not a universal hazard reconstruction system.
- Final qualification remains full Core/EditMode/Checks/audits, twice-isolated
  PlayMode gate, both modes, round/rematch/disconnect matrices, actual Windows
  executable and ordinary/full-speed visual evidence after all kit/map work.

### Direct reconnect v5 now running

Verification v5 build succeeded in74 s,995 MB, protocol25,60dea16+dirty. It includes
both the measured receive-buffer repair and reliable picks-before-live-state
ordering. Direct owning-profile reconnect during ultimate is running at
Logs/familiar-reconnect-clean-v5. Read its independent result/CSVs before declaring
transport or snapshot acceptance. No proxy in this case. No native control active.

### Direct transport recovery proven; actual effect loss isolated

Direct reconnect v5 now admits peer3 into the same seat1 and observer remains
connected through its run. The previous direct transport failure is repaired.
The only remaining row failure is live field reconstruction.

CSV proves why: host's pet IsDevouring changes1->0 when the roster is resent on
rejoin, while the actual pulling field and ultimate clock remain active. Every
SyncPicks called CharacterVisual.ApplyModel unconditionally; ApplyModel destroyed
both person and pet even when all assets were identical. BroadcastFamiliarEffect
then saw an ordinary pet and sent no live state. This also interrupted animations
and explains why snapshot ordering alone did not repair the effect.

ApplyModel now compares actual model/pet assets, clip contents, palette contents,
tint and person/prop mode and preserves an identical existing binding. It still
rebuilds changed models or appearances; mode-relative index equality is not used.
Added real active-ghost/duplicate-roster regression. network-model-snapshot-contract
is next (20 cases). Do not call complete until direct reconnect reruns on v6.

## Latest tooling request

The owner asks whether any Unity configuration is needed and explicitly requests
checking/using the new Unity plugin wherever it helps this task. Existing local
Unity/tests/builds work; no configuration is currently needed from the owner.
Investigating the local plugin catalog through the installed CLI. No Unity tool is
currently exposed in this conversation, and the connector's search/suggest plugin
tools are absent; do not claim it installed/connected without verification.
Plugin-management and OpenAI-docs skills were read. Official plugin docs were
fetched; inspect the concrete Unity entry rather than infer capabilities.
Continue game work while plugin discovery proceeds; no new task or subagent.

### Exact current run correction

Duplicate-model snapshot PlayMode run was19/20, not20/20: no-teleport assertion
sampled after a rendered frame and saw0.106 m ordinary body motion against0.1 m.
Inspect temporary AI/input cleanup and assert the actual synchronous snapshot
operation before ordinary motion resumes; do not simply widen the threshold.
Verification build v6 was launched before that output was fully inspected. It is
not a passing-test checkpoint. Read build outcome, fix/verify that contract before
committing, and do not cite20/20 from this run. The duplicate-binding case itself
passed. No native control active.

### Official Unity plugin is available

CLI catalog confirms unity@openai-curated-remote0.1.3-beta installed/enabled, authored
by Unity Technologies. It is a skills package; its live-editor route is Unity CLI
plus com.unity.pipeline, not an already-exposed Unity MCP tool in this session.
Read unity-cli/SKILL.md and integration-advanced.md from its installed cache.
The CLI already exists at C:/Users/Matthew/AppData/Local/Unity/bin/unity.exe.
Checking status/package setup; always target this checkout explicitly and keep one
Editor. User authorizes use wherever helpful. Existing tests/builds need no new
user config/credential. Monetization/cloud changes are not requested.

The plugin's audio-mixer workflow was only inspected for capabilities, not applied;
its manual-mixer-creation/approval steps are not a reason to block current gameplay
work. Use relevant skills deliberately, preserving this task's standing autonomy
and limited UI scope. Plan/code sources retain the existing sole-author rules.

### Unity plugin live connection verified

Unity CLI1.0.0-beta.5 and official plugin0.1.3-beta were already installed. Added
com.unity.pipeline0.6.0-exp.1 through `unity pipeline install` for this checkout.
Launched one resident batch Editor via the profile-preserving runner, log
Logs/unity-pipeline-editor.log. It is ready on local port7800; CLI editor_status
confirms correct project and6000.5.8f1, not compiling, Play Mode initially stopped.
Use explicit --project-path on all commands. Asset import worker is not a second
project Editor. No native desktop control is active.

Plugin command catalog saved as Logs/unity-command-catalog.json. It exposes eval,
scene operations, capture_game_view, console, run_tests and test_status. Started
NemuKitContractProbe asynchronously through the plugin after fixing the0.106m
post-frame assertion: snapshot no-teleport is now checked synchronously at.001m;
temporary body AI is disabled and its movement intent cleared before destruction.
This is a20-case live run. Poll test_status, inspect actual counts/failures and
retain the result. JSON is not automatically an NUnit XML gate; investigate export
or rerun the repository batch gate later. Do not claim the prior19/20 run passed.

The resident Editor is held by exec session36397 and must be closed through a
verified Editor command/eval before source/imported edits or another Unity batch.
The runner restores persistent files after that Editor exits. Do not terminate
unrelated Editors or leave this one running when unnecessary.

### Plugin used successfully; resumed real-player reconnect

Live plugin test_status reports20/20 passed in34.06 s after temporary AI cleanup
and synchronous no-teleport assertion. Preserved raw report at
Logs/unity-live-nemu-completed.json and wrapped response at
Logs/unity-live-nemu-test-status.json. This is verified plugin JSON, not an NUnit
XML substitute for the final gate. CLI eval then scheduled EditorApplication.Exit;
collecting runner36397 restores the profile snapshot. No resident Editor is needed
for the next standalone-player row and it was closed normally.

The CLI's old1.0.0-beta.5 skill-install dry-run would write the entire skill into
AGENTS.md, so that migration was not applied. Existing curated skills plus explicit
CLI commands work; do not bloat or overwrite the carefully maintained AGENTS.md.

Now running direct familiar-reconnect-clean-v6. V6 includes duplicate model binding
preservation, but predates the tiny temporary-AI cleanup and test timing change.
Keep its exact runtime hash/report scope honest. After verification, add the live
NUnit report exporter or use the existing batch XML gate before stable delivery.

### Progress estimate communicated

The owner asked percent complete. Answered approximately45% of the FULL expanded
scope, explicitly a rough scope estimate rather than test percentage. Listed the
cleaned cast/core movement, Cheska first deep pass, Kuro model/idles, Nemu gameplay
and implemented networking repairs; stated that reconnect verification, other
kits/distinct alternatives, deeper animation, all three maps/trees and final
Windows qualification remain. Do not silently treat this as45% of networking only
or claim the broader work is done when the next network test passes.

### Direct reconnect v6 passes

familiar-reconnect-clean-v6 PASS. The restarted owning profile reclaims seat1,
reconstructs the actual live field at(1.1712,-1.5592), matches its lifetime and
retains s2charges0/ultcharge0. Settled model/body error0 on host and observer.
Runtime hashf9a972cfd93e926f6f5f048b43efbe3637c7d736e6ea69447d477bd76d7bc460.
This closes the reproduced familiar/receive-pool/repeated-model failure chain;
it does not close all networking or the broader game-polish task.

Adding a small Editor-only native NUnit result recorder so plugin-driven tests
retain the repository's XML evidence contract. Full EditMode, current20-case Nemu
XML and checks/audits will gate the stable batch before commit/push. Keep remaining
remote impacts/loadout binding/all-kit states and final matrices open.
