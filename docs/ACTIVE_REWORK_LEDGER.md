# Active TUMP rework ledger

Updated 2026-09-10. **The task is ongoing. Do not treat a compaction or a green test
run as completion.** This file preserves the owner's current instructions, the
actual state and the unfinished work. Update it when a decision or result changes.

Read CLAUDE.md first, docs/VISION.md second and docs/TODO.md third. Then read this
ledger and docs/IMPROVEMENT_PLAN.md. Current owner instructions supersede older
restrictions in repository documents. The plan's outcome table remains useful;
this ledger resolves the later owner feedback that changed the scope.


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
