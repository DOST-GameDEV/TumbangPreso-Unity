# REFINE-2 research and execution plan

Status: saved plan for the next refinement phase. Full comparative research and
new implementation are NOT complete. Finish the older actionable queue first;
retain every older task, unresolved test and specific external dependency.
Owner's exact words are in [owner-request.md](owner-request.md); scope intake is
[intake.md](intake.md). New playtest feedback reopens aesthetic/motion acceptance:
previous software checks do not prove that walking, throwing or pektus feel good.

## Direction

Aesthetically pleasing stylised3D, not a realism target. Preserve TUMP's cute blocky
cast, readable sporting action, Philippine places and original supplied art.
Appeal comes from considered proportions, silhouette, construction depth, colour/
value grouping, material identity, lighting and motion. Texture is one tool, not a
reason to cover everything in noise, dirt, cracks or glossy PBR. A flat clean region
can be intentional; a whole place lacking hierarchy, material cues or authored
points of interest is the reported problem.

The owner rejects broad multi-aspect fixes. Keep the new work isolated: one map,
one asset family/object or one movement/character at a time. Reuse infrastructure
only when its output is deliberately fitted to that subject. A global multiplier,
shared walk replacement or one shader across every building is not completion.
Do not change maps2-5 silently while refining map1. Record the local selection and
verify the intended scope, without rerunning the whole game for every small edit.

## Research-first foundation

Latest owner additions are captured in [the cultural and camera brief](cultural-and-camera-brief.md):
study PEAK and other games before implementation; give each map its own researched
Filipino place/cultural identity; fill sparse context coherently at the lobby's real
overview as well as gameplay/introduction/spectator views. This is queued after UI
and current tests. The brief extends the existing per-map order and research gate.

Before the asset/motion prescriptions, research what makes this kind of game good:
player goals/agency, meaningful choices and counterplay, responsiveness, challenge,
pacing, readable causality and satisfying feedback. Connect that to visual appeal:
shape/value/colour hierarchy, construction/material identity, coherent motion and
purposeful sensory emphasis. Realism and sheer detail/effect count are not proxies.
[Initial primary-source notes](foundation-research.md) have begun; the broader
comparative research remains open. No new refinement edit bypasses this gate.

## Research deliverables before implementation

1. A reference sheet with actual images/motion, source links, access dates and a
   short explanation of what transfers. 3d-asset.com is ONE source. Compare other
   independent stylised game environments, developer art breakdowns, asset artists
   and real construction/place references. Prefer primary artist/developer material
   for technical choices. Candidate styles to investigate include graphic material
   separation, hand-painted/low-poly forms, simplified PBR and illustrated worlds;
   do not copy one game's whole identity or assume realistic detail is better.
2. An appeal breakdown: large/mid/small shape balance, bevels and edge rhythm,
   recesses and overlaps, material-specific marks, texture scale at player distance,
   colour families, quiet/detail distribution, focal landmarks and court contrast.
   Note failures too: uniform grime, noisy repetition, floating decals, black empty
   windows, over-gloss, oversaturated props and detail that disappears in play.
3. A TUMP comparison using the actual current maps and normal gameplay cameras,
   including Low and the small viewport. Name each current weakness and show its
   corresponding reference principle. Distinguish an asset problem from lighting,
   camera/compression or material conversion; do not repaint art to fix a pipeline bug.
4. An asset decision register per map: location/object, material/construction,
   existing source and ownership, keep/refine/replace decision, exact intended
   change, gameplay/readability risk and completion evidence. Include ordinary
   background objects, not only a showcase building. Preserve imported masters;
   make explicit derived variants where needed and keep repeatable authoring.
5. Animation references separate from environment references: stylised3D locomotion,
   rigid/simple-limb characters, throwing and wrist/hand release, anticipations,
   follow-through and transitions. Use actual motion/animator breakdowns, not just
   still poses. Research real tsinelas throwing/pektus where useful, then adapt the
   intent to TUMP's simple hands/rig rather than forcing realistic anatomy onto it.

Initial reference metadata only: [3D Asset homepage](https://3d-asset.com/),
[catalog](https://3d-asset.com/catalog/), [sample previews](https://3d-asset.com/samples/)
and [licence](https://3d-asset.com/license/) were opened. Rendered comparisons are
still pending. Public previews are references, not automatically approved imports.
No purchases/downloads were made. Retain licence/provenance for anything later used.

## Map-by-map work

Provisional order: Eskinita, Bayan Plaza, Ilalim ng Tulay, Sa Bubong, Lagoon.
Research can justify a different order, but all five remain assigned. Complete one
map's local inventory, design, implementation and bounded inspection before moving
on. Do not mix its facade work with unrelated character/gameplay changes in a patch.

For each map:

- Establish a small set of representative real viewpoints and the map's specific
  visual story. Inventory foreground, playable perimeter, skyline and distant
  background at their actual screen sizes. Keep lanes and objective readable.
- Refine construction before decorating: thickness, joins, recesses, roof structure,
  supporting elements, glazing/shutters, ground contact and believable attachment.
- Author material detail according to its job and exposure. Plaster, timber, stone,
  sheet metal, clay tile, concrete, cloth and water must not share the same treatment.
  Give separate houses/structures considered variations, not random rainbow colours.
- Add restrained place-specific lived detail and useful landmarks. Respect local
  cultural context. Keep supplied signs/livery and existing collision/routes intact.
- Judge the result in motion and in ordinary player/spectator views, not only a close
  model turntable. Keep a normal/greyscale comparison and the smallest changed-risk
  check. Record unresolved scope accurately and stop capture-tool repair at its cap.

Place-specific questions are in intake.md. Lagoon houses remain fixed on piles;
boat motion is separate. New sky/water refinements must support each place rather
than apply another universal colour/noise pass. The previous VISUAL1world finish
is the starting point, not proof that this new asset-level request is finished.

## Animation plan: character and movement separately

The owner reports Sean's walking arms sticking to his body, similar problems on
other characters, and unsatisfactory throwing/pektus. Their guess that motion was
copied is a hypothesis to investigate, not a measured cause. Inspect actual clip
bindings, rig axes/proportions, blend/overlay ownership, carrying pose, speed and
network sampling before choosing a fix.

1. Build a concise animation coverage list for every shipped classic and hero body
   and each FPP arm pair. Track empty/held states and the movement/action being
   assessed. Preserve existing meshes, faces, rigs, bone paths and saved IDs. This
   request authorises animation refinement, not another character-model redesign.
2. Start with Sean walking. Observe front/side/rear/three-quarter at normal speed.
   Diagnose shoulder/arm clearance, arm swing arc and timing, torso counter-rotation,
   weight transfer, foot plant/slide, stride relative to travel and transitions.
   Check empty and carrying separately: a stable throwing hand must not freeze the
   off-hand or entire upper body. Respect the simple-limb rig rather than adding
   realistic elbows/fingers as a shortcut. Fit the solution to Sean first.
3. Review every other character's walk individually. Keep identity/proportions and
   silhouette clear, with sensible motion differences where they help. Shared clips
   are acceptable only when they actually fit and have been inspected; never call
   a single global walk edit an all-character pass. Maintain a per-character status.
4. Then separate run/sprint, strafe/backpedal, start/stop/turn and held-object blends.
   Inspect speed changes, fatigue, slopes/kerbs, jumps/landings, slide/recovery,
   swimming and carrying where supported. Fix one movement issue at a time; do not
   hide foot sliding with extra VFX or camera shake. Avoid excessive body scaling.
5. Throwing gets its own work item. Inspect short tap, partial/full charge, stationary
   and moving release; anticipation, actual release frame, hand/slipper separation,
   torso/arm follow-through and recovery. Compare real third-person body, FPP and
   observer rather than making one camera look good at the other's expense.
   Preserve input responsiveness, authoritative aim/trajectory and actual contact.
   Improve animation around real timing instead of delaying gameplay to suit it.
6. Left and right pektus are separate inspections. The preparation, wrist/hand/slipper
   orientation, release and follow-through must communicate each curve direction.
   It must look like a purposeful variation of the actual throw, not a rotated
   generic clip or body twist pasted over it. Match the real spin/flight direction,
   test low/high input and a moving throw, and retain aim/authority contracts.
7. Cancellation, denied/empty attempts, interruption, pickup into carry and recovery
   require continuity. Do not show release/contact/success when none happened. Verify
   that layering does not erase the authored motion, snap back or accumulate offsets.
8. Review remaining verbs and each hero's casts/alternates/ultimate phases as their
   own performances. Preserve approved distinct shapes and warnings. Refinement is
   warranted by an observed problem, not by an old OPEN label or desire to redo art.

Acceptance per animation change: the specific motion visibly improves at normal
speed, works in its relevant views/states, preserves gameplay and its interruption/
recovery contracts, and has clear scope. Freeze/capture staging is not a substitute
for watching the real movement. Record why any sharing remains appropriate.

## Animals and bots

Animals: observe their actual loop and classify each unnatural moment. Retain safe
habitats and contact; add meaningful activity/goal selection, variable dwell and
turning, believable acceleration and reactions suited to each species/place.
Avoid merely randomising waypoint order, jittering motion or adding more path nodes.
Use existing safe routes as constraints where appropriate, not an endlessly visible
patrol script. Keep collision/readability and replay/pause behaviour explicit.

Bots: gather decision/reason/action/movement traces during ordinary complete play,
across both modes, both roles, all maps, roster/choices and relevant round/ownership
transitions. Separate deliberate waiting from stuck navigation, unreachable targets,
stale state, refusal loops or actions that never fire. Fix the cause one at a time.
A random nudge or a timeout reset that conceals a dead decision is not a solution.
Retain existing C1/C2 fixes but do not cite them as proof the new AFK report is gone.

Whole-gameplay issue log: observation, reproduction, intended player benefit,
smallest responsible subsystem, implementation, focused evidence and remaining
limits. Include physical interactions, movement, targeting, networking, spectator
understanding, audiovisual timing and transitions. Do not combine unrelated aspects
under one broad polish patch. Final integration follows completed implementation.

## Continuity and budget

Keep TODO status, active ledger, decisions and concrete next step current after
meaningful work. No silent task deletion, old-task deferral or checkpoint completion.
Follow the one focused check/one bounded tooling-repair rule. Real product defects
get fixed; unrelated capture machinery does not consume the implementation phase.
Record unfinished or external acceptance honestly. New research/implementation
begins after the older actionable-work gate, not instead of finishing it.

## Owner clarification: map references after UI,2026-09-23

Finish all currently assigned UI work before starting map refinement. The older map
feedback remains active: all five maps individually, construction-specific detail
and material identity, pleasing stylised art rather than realism, more convincing
Sama Bajau lagoon architecture including detached over-water homes, animated sky
and island/mountain context, natural ambient movement. Keep good existing work.

Use built-in image generation as an additional reference source during that later
pass. Critique every result: record useful silhouette/material/composition ideas,
errors, over-detail, cultural/place mismatches and parts that would not work in the
actual3D camera or navigable map. Copy only the good ideas into authored game assets.
Generate further targeted references when the comparison reveals a concrete gap;
do not turn reference generation into a substitute for implementing and viewing a
better map. Continue using3d-asset.com and independent real-world/game references.
Compare actual in-engine before/after at player/spectator distances for each map.
No global texture/noise/recolour sweep, no deleting older tasks, no premature closure.
Current generated images are loading-screen illustrations, not a started map rebuild.

Owner expands the later review to ALL animated skies, islands, mountains and other
background scenery. This means assess and improve weak existing work, not merely
retain it because animation exists. Added REFINE-2.6a, carried inside each map pass.
UI remains the immediate priority.
