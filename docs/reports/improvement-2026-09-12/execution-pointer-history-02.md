# Historical execution pointer

Superseded by the short current pointer in ACTIVE_REWORK_LEDGER.md.

## Current execution pointer (continuation, 2026-09-12)

## Current research-to-implementation checkpoint

Research and next-step table M01-M11 are now recorded in MAP_TRANSFORMATION_PLAN.md
and reports/improvement-2026-09-12/map-reference-research.md. The owner explicitly
asked to finish researching, save the detailed steps, then implement. References
include actual local Gilmore/Pila/Project8 photographs, a directly viewed Molo
photo, official Aurora Boulevard photos, Raon retail photos and the owner's4
street-vendor photos. Local originals remain in Logs/reference-review and
Logs/map-owner-references-2026-09-12. The latter retains original watermarks.

The confusing dark road shapes are Dressing_LooseManhole and Dressing_SunkenTrench
under Ilalim's FormerHazardProps, retained cosmetic remnants of removed hazards.
Remove them from central play as part of M06. The owner authorizes natural
'Bawal umihi dito' signs and original graffiti; Filipino environmental wording is
allowed, while gameplay/UI descriptions remain English. Food/fruit/clothes and
accessory stalls are required, with believable vendor/customer/through-space.

All Unity/Blender runs are COMPLETE. Last Unity75613:42 architecture images,1/1;
profile snapshot c616a5c3ea92 restored17 files. Blender75066 finished V3 studies and
copied4 GLBs to Art/models/neighborhood-buildings and4 packed .blends to
MapSource/environment/neighborhood-buildings. They are NOT yet placed in maps,
import-verified, style-accepted or committed. tools/author_neighborhood_buildings.py
owns them. Study images/texture sources are Logs/neighborhood-buildings-v1/v2/v3.

Next concrete work: preserve this research/plan checkpoint, complete the measured
Ilalim placement sheet (M01), then validate native building/stall/sign assets and
implement the coherent street batch. Keep the active scene pipeline repeatable.
Do not resume the paused graphics prototype first or claim the maps are finished.


**Current action plan and exact working-state snapshot:**
[EXECUTION_PLAN.md](EXECUTION_PLAN.md), explicitly requested by the owner for
compaction safety. It includes latest approved Sa Bubong mechanics, graphics/
mashing scope, pushed HEAD, dirty files, evidence, next actions and boundaries.
**OWNER VERDICT,2026-09-12: V9 is insufficient.** The owner says the plaza floor
is empty, the pictures barely look improved, and the maps do not feel more Filipino.
Substantial three-map review, a revised improvement plan and implementation take
priority now. Technical passes/closed152.7 do not constitute visual acceptance.
The uncompiled graphics experiment is preserved as non-compiled text/patch under
reports/improvement-2026-09-12/graphics-prototype; compiled runtime was restored to
97e61f92 before map work resumes. No Unity process is active. Begin with Bayan's
surface/ground composition and audit architecture/materials/spatial relationships
across all3 places against fitting real Philippine references. All broader
requested graphics/mash/Sa Bubong/kit/network/animation scope remains open.


### Further owner feedback,2026-09-12

- Ilalim placement and ALL text/signage need rework. Pisonets standing out in the
  road are not a believable place. Integrate them into actual shop interiors or
  recessed sheltered storefronts, with a continuous pedestrian route.
- The wide white crossing bands were explicitly rejected. Source:
  IlalimNgTulayBuilder.BuildRoadSurfaceDetail creates6 bars per end atz10.8..15.7,
  width12.6 decreasing to10.6m, depth.48m. These are scenery road markings,
  separate from required TUMP chalk. Rebuild a believable crossing away from play.
- Houses are plain color slabs and do not feel Filipino. Improve actual building
  construction, openings, roofs, materials and lived-in use, not only repainting.
- Bayan feels like there is no broader world beyond the court. Build connected
  surrounding streets/blocks and layered architecture, not an isolated ring.
- The owner reiterates freedom to thoroughly improve other problems found and
  wants a nostalgic Filipino feeling. Keep stylized forms and simple gameplay.

These explicit critiques override earlier preservation of poor placement/signage.
Retain proper names where relevant, but correct sign design, geometry and location.
Do not treat the approved tricycle or a technical route pass as completing this.


## Owner expansion after maps: movement, throwing and equipment identity

2026-09-12: the owner explicitly requests improving how it feels to play after
the map work. More convincing physics and animation are in scope. Their example
is leaning back during throw windup/aiming, visibly replicated to other players,
with body/FPP preparation and release matching. They suggest small noticeable
shake and accuracy error to make aiming more skillful. They also authorize
substantially differentiating or revamping slipper attributes, and the same for
cans: each needs a reason to be chosen beyond its appearance.

This is required follow-on design/implementation scope, not implemented behavior.
The suggestions authorize investigation, not a mandate for arbitrary random spread.
Examine the actual motor/throw/can/gear/network owners and existing attributes first.
Design and test a small coherent set of useful tradeoffs. Favor readable control
and counterplay, preserve simple controls and both modes. Maintain equipment IDs,
owned items/profiles and truthful descriptions; no new progression grind or
unrequested paid systems. Do not change the approved eighteen people/outfits.

Plan the complete movement/action sequence: starts/stops, acceleration/deceleration,
sprint/backwards/turn cadence, planted feet, carried slipper weight, windup lean,
release/follow-through, impact, interruption and recovery. Owner and observer must
see the same accepted action. Camera/FPP shake, actual trajectory and aim indication
must agree; preserve motion settings without giving an accuracy advantage.

Before changing balance, record current slipper/can behaviors and candidate roles,
tradeoffs, relevant attributes and interactions in a focused play-feel plan. Compare
normal-speed controlled trials and ordinary matches with all input paths and
host/remote authority. Include hold-vs-release timing, movement while aiming,
collision/retrieval, guideway/raised ground, equipment swaps, round/reset/rejoin,
framerate independence and both modes. Do not conclude balance from one bot seed.
Map transformation remains first. The broader kits, loadout binding, mashing,
Sa Bubong, graphics and networking scope remains open alongside this expansion.


### Pektus must visibly impart its curve

Owner clarification,2026-09-12: Pektus currently feels like the slipper magically
acquires curve. Author a distinct arm/forearm/wrist windup and release that visibly
imparts the accepted spin/curve direction, then a matching follow-through. Verify
left/right curve, hold/release, interruption, owner FPP, observer body and remote
peers. Use existing simple hands/approved anatomy. Actual curved flight and
presentation must agree; a generic straight throw with a curved projectile is not
adequate. Trace current Pektus input/physics/animation/network data before choosing
the sequence or modifying the curve.

The owner explicitly asks us to find other mismatches like this proactively.
Audit all movement/throw/retrieval/impact/ability actions for missing preparation,
weight transfer, contact, moving geometry, interruption and recovery; do not wait
for the owner to list each one. Record findings and planned full sequences in the
post-map play-feel plan. Keep map transformation first and all prior scope open.


## Continuation order confirmed by the owner

2026-09-12: after the map and play-feel/equipment work, return to everything else
in the actionable TODO list. The owner has not requested a stop or limited handoff.
Current order: substantial three-map transformation; movement/throw/Pektus and
slipper/can feel (including recovery mashing); remaining requested graphics and
Sa Bubong work; then all remaining actionable TODO items, including whole kits,
loadout binding, Kuro/Phaister qualification, animation and networking. Dependencies
can be handled when needed, without dropping any accepted scope. Preserve the
separate controller owner's work and human-only/external approval boundaries.
The full improvement goal remains open until its required work is actually done.


Owner reiteration,2026-09-12: implement convincing/realistic animation AND gameplay
where appropriate, using the windup/lean, aiming and Pektus examples as guidance.
Proactively identify the other applicable cases and implement the complete pass;
do not limit it to those examples or ask the owner to specify every action.
This changes motion/weight/contact/physics within the approved blocky style, not
the eighteen people's anatomy or a new visual redesign. Map work remains first.

Completed run75613 (1/1,42 images,17 profile files restored; snapshot
 c616a5c3ea92): guarded ArchitectureAndStreetContinuity PlayMode capture,
Logs/map-transformation-architecture-before-v1.xml/log/folder. No Unity process is now active. These42 architecture views supplement144 FPP
views; they are not normal-speed gameplay. The current pushed plan is e7a6f90b;
97e61f92 is last implemented map source. New Pillow footprint tool and6 measured
plans exist under tools/map_layout_review.py and Logs/map-transformation-audit-v1.
Its building AABB overlaps are candidates only. Ilalim signs total1162 renderers.
Recent post-map play-feel/Pektus/equipment/full-TODO requests are saved in docs.


## Mandatory self-critique after every visual/play-feel batch

Owner requirement,2026-09-12: thoroughly evaluate and criticize each completed
batch for whether it looks/feels right. They worry new edits may be ugly or out of
place. This is an explicit acceptance requirement, not an optional final polish.

Before accepting a batch, record actual strengths AND weaknesses against:

- Style: fits the retained cute blocky cast and world at gameplay distance; no
  realistic/noisy reconstructions, excessive detail or unrelated art language.
- Place identity: does construction/use read as the intended Filipino place even
  without a label explaining it? Nostalgia comes from coherent familiar details.
- Spatial logic: doors, steps, roofs, counters, vendors, people and utilities have
  plausible relationships; no arbitrary clutter, overlaps or disconnected access.
- Proportions/materials: human scale, supported geometry, meaningful surfaces,
  restrained wear and useful depth; no broad undifferentiated color slabs.
- Composition: clear near/middle/far layers, distinct directions, connected wider
  world, sufficient quiet space and no awkward empty gaps or repetitive stamping.
- Gameplay: chalk/can/shoes/players/tells remain clear, routes usable, collisions
  honest, both modes sound. Later action review must show preparation/contact/
  momentum/recovery and truthful body/FPP/observer timing at ordinary speed.
- Scalability: actual Low/Balanced/High images and measured costs, not assumptions
  that polygon or renderer counts alone prove a frame-rate improvement.

Include inconvenient views and report what still looks wrong, why, and the next
correction. Passing XML, a nice isolated render or more objects is not acceptance.
Revise within authorized scope without making the owner catch every obvious fault.
Do not claim the owner likes a result unless they actually say so.

Current self-critique: the V3 native building studies have more coherent openings
and roof/drainage construction, but still repeat similar massing/palettes and lack
finished storefront/domestic use. They need reference-specific composition,
materials and in-engine cast comparison. They are not approved final map assets.

Current verified/pushed spatial checkpoint is97e61f92; inspect actual git
HEAD/status before resuming. V9 source, scenes, native model and evidence are committed.
ALL Unity/Blender runs are COMPLETE; no Editor is active. Last sequence85437
passed8/8 checks,489/489 graphical EditMode and MapRouteProbe1/1:7047 connected
walk nodes,7420 clear resting samples,18/18 actual motor-driven pickups. Final
profile snapshot368dce845f87 restored17 files. Known generated tangent-only mesh
and quality-setting changes were verified and restored. Semantic comparison:
16746 rows, zero baseline/run1/run2 changes.14 source audits pass, with7
informational audio flags still present.

V9 clears civic frontage with inward-facing side hoops and adds3 original blocky
passenger tricycles to the roadside bay. All144 camera metadata rows match the
baseline;24 Balanced Classic directions reviewed. Report/images/route CSVs and
native Blender source are durable. Closed152.7 records this measured batch only.
Next: rendering/frame-cost baseline and graphics settings,
all-context recovery mashing, Sa Bubong and the remaining kits/network/animation.
No graphics-runtime, new mash or fourth-map implementation exists yet.


Fetched origin/ASTRAReworks in the specified ok-x20 checkout. Clean local and remote
HEAD both initially equaled 0f08e9921c96148bf0c332d09b8392497f0cc82e. No checkpoint reset, main
operation or separate Documents/GitHub checkout change. Verified foundation is now
pushed at8a22f8b9524ed943105f892fcfc54ef3a9bb43f5. Resume maps, route/idle
investigation and then the remaining kits/network/animation qualification.

The owner explicitly permits worthwhile additions along the way, including more
animation/feedback, as long as they do not overcomplicate the game. Preserve the
core game, both modes, retained eighteen people, existing input/data contracts and
all other boundaries. This is permission for player benefits, not mandatory scope
growth. Keep this clarification through subsequent continuation.

Owner additionally requests compaction-safe working state and a self-contained
AGENTS.md for ChatGPT/Codex. AGENTS is now primary, carrying the actual scope,
gameplay/art contracts, tooling/verification and continuity rules. CLAUDE.md is
retained as historical reasoning. Active read-order links use AGENTS first.
Latest emphasis: thoroughly improve look and feel into a fully realized game that
surprises people while retaining the existing style, with fitting Filipino aspects.
Judge complete play sequences and coherent places, not only technical test totals.
Latest owner expansion: remove/add anything in the maps as useful. Correct spatial
relationships thoroughly and increase natural Filipino place identity without
forced decoration. Legacy placement is not protected just because it exists.
Also thoroughly improve graphics AND graphics settings for lower-spec play and
visible higher-end benefits. Measure frame cost and actual image differences;
preserve gameplay readability at every preset. Functional graphics UI is explicitly
in scope. Windows remains the established delivery target; do not imply testing
on absent lower-end/Android hardware. Keep these requests through continuation.
The owner reiterates that any map element may change, provided the original
Filipino feel and purpose remain: neighborhood street, civic plaza and guideway/
shop district. Preserve those identities through substantial spatial improvements.

**Fourth map: APPROVED WITH CHANGES, Sa Bubong.** The owner permits/requested a Filipino
rooftop/building/condo map with falling off at the very edge, then explicitly asked
to hear the chosen concept and approve it while other work continues. Proposed
in chat and via asynchronous approval question: **Skyline Roofdeck**, a Metro
Manila condo roof with open central court, fenced pool, shaded residents' area,
separate stairwell/laundry/water-tank corner, mostly railed edges and one clearly
marked unfinished ledge. Falls cost time/position, recover safely and keep slippers
retrievable. Both modes; no new controls/health system. This is a concept, not
implemented mechanics. The owner first answered 'Revise the concept', then explained
the requested revisions and explicitly said they already like the concept, asking
for a Filipino name. The selected name is **Sa Bubong**. This later acceptance
approves implementation; do not remain blocked on the old question.

Required changes: players can button-mash to get back up after falling. A slipper
that falls off remains unavailable for about10 seconds, then teleports back to a
safe rooftop location. The owner explicitly calls that delay a penalty. Do not
silently replace it with immediate recovery or permanent lost ammunition. Also
thoroughly verify mashing in ALL contexts (stun, trip and rooftop recovery), through
actual keyboard/controller/touch input paths and host/remote authority. Preserve
the existing controller-ownership boundary. This is authorized new map gameplay,
not just a decorative roof. No new map implementation exists yet; plan the real
edge, return anchors, readable recovery, AI boundaries and network/round cleanup.

Current verified runtime correction: MapRetrievalProbe reproduced two separate
failures on unchanged Slipper source (0/2, Logs/map-retrieval-before-v2.xml).
Direct HostThrow retained Carrier.Held through the helper's same-holder exemption;
the actual Carrier.HostThrowAt path cleared Held but landed prematurely on the
overhead guideway query. Slipper now detaches its carrier and uses swept-height
support for flight, passing the same support into Land. After:4/4 PlayMode,
Logs/map-retrieval-after-v1.xml. Raised-slab descent and unreachable-roof recovery
also pass. Five physics steps cover1.273m below the deck. The historical48-idle
cause remains OPEN and is not attributed to either defect. Full report:
[map-retrieval.md](reports/improvement-2026-09-12/map-retrieval.md).

Core562/562 and all14 gating audits pass. The informational audio audit flags7
files, not a clean listening verdict. The discovered PlayMode partition originally
refused five inherited unassigned fixtures; all are now assigned to existing match
or capture groups, with MapRetrievalProbe also included. Full gate is not run yet.
EditMode v1 with -nographics:487/489, two near-light fixtures unable to illuminate
their surfaces. Rerun with graphics, do not weaken those tests. V2 failed to compile
the new semantic checker due to the removed InstanceIDToObject API; checker now
uses SerializedProperty references. Graphic EditMode V3 passes489/489 in
Logs/continuation-editmode-v3.xml/log. Guard completed and restored17 files.

Verified editor authoring verifier: MapRepeatabilityCheck.Run saves/reopens
each map after two NeighborhoodFinishAuthor runs and compares normalized loaded
components, mesh/material contents and lighting, keeping inactive objects. It
backs up scene bytes and retains baseline/run1/run2/differences in a versioned
Logs folder. First semantic run refused an embedded scene material before making
map edits. V2 exposed raw Unity reference-ID children in its serializer; V3 visits
structural containers only, hashes embedded assets and retains semantic references.
V3 PASSES two saved/reopened runs on all maps:2389 Eskinita,3055 Bayan,11223 Ilalim
rows, zero changed rows on each. Baseline-to-run1 also zero. Art asset byte content
is unchanged; scene diffs are generated object IDs. Restore the three scenes to
their original clean bytes after the current run. Report is
Logs/map-repeatability-v3/report.txt; snapshots/diffs are beside it. Do not quote
V2's ID-only failures as map drift. Checks.RunAll passes8/8 in
Logs/continuation-checks-v1.log. All Unity runs are complete; no active process.
All proven test-only arm tangents, quality settings, line endings and generated
scene IDs are restored to the initial clean bytes. Next: commit/push this stable
throw/instruction/repeatability batch, then capture real FPP at legal map positions
and test routes. No new Windows build yet.

All completed guards restored17 existing profile files. Latest completed backup
after checks:profile-preservation-95a84664e407. The36 named-person arm tangent
changes and QualitySettings/ProjectAuditorSettings have been restored. Current
work also includes AGENTS/read-order docs, Slipper, MapRetrievalProbe plus meta,
MapRepeatabilityCheck plus meta, playmode_suite.py and this report/TODO/plan.
No map composition has intentionally changed yet. Preserve all other state.
The older side-eye captures on Eskinita use x=+/-10 outside its8.1m wall; they are
not legal player views. The real FPP rig uses1.25m eye offset and95-degree FOV.
Use it with actual follow/self-hide and legal body locations for the next review.

**Active follow-up after8a22f8b9:** MapExperienceProbe is new/uncommitted, and
MapRetrievalProbe.Load is now internal with an optional mode parameter. The
capture fixture is registered in playmode_suite.py. Completed guarded session12915 ran
Logs/map-owner-baseline-v1.xml/log with TUMP_EVIDENCE=Logs/map-owner-baseline-v1.
It captures8 legal directions at Low/Balanced/High with the actual FPP rig in both
modes/all maps, plus separate16-second controlled input-driven carry/throw/retrieve
owner/body sequences at1x. Result1/2:144 static views recorded with correct1.25m
offset/95-degree FOV, but visual inspection finds owner frames mostly black, so
the static test's structural pass is NOT visual acceptance. The first controlled
Eskinita retrieval failed near the parked defender and stopped that test before
the other five sequences; investigate the actual route, not a completion claim.
Static quality comparisons pause time and explicitly do not claim ordinary play.

Completed guarded session51244 ran MapExperienceProbe.DiagnoseOwnerOcclusion,
Logs/map-owner-occlusion-v1.xml/log and outputLogs/map-owner-occlusion-v1. It logs
near renderers and compares ordinary owner/no-outline/no-viewmodel/no-body frames
one variable at a time. Its renderer trace identifies the problem: the north
camera station was teleported INSIDE seat2's visible head (center0,1.53,8.53),
while its own body correctly remained ShadowsOnly. Pixel inspection finds24/144
static views over80% black, concentrated at that north station; other directions
are readable. This is fixture staging, not a demonstrated self-hide/runtime bug.
StageOtherSeats now keeps all four people visible but moves the parked others off
the measured paths. It asserts1.5m separation at each camera station. The diagnostic
also captures a separated-seats control, and retrieval now logs actual positions.

V2 completed: static144 views and separated-seat diagnostic pass; the controlled
Ilalim retrieval failed with the body actually within0.1m of its loose shoe.
The test wrote input in a coroutine after Update, and CharacterMotor.FixedUpdate
committed away the edge before Carrier.Update consumed it. A test-only ReviewInput
component now writes at Update order-300, matching normal producers. No runtime
input change. V3 (completed session96432) PASSES1/1 across all6 map/mode sequences,
Logs/map-owner-motion-v3.xml/log and matching folder. All six real throw releases
and pickups occur at1x. Timestamped owner/body videos are encoded with
tools/encode_motion_evidence.py; no speedup/interpolation. This is controlled
sequence evidence, not free-play or a historical idle fix.

**Current author batch, unreviewed:** MapFinalPassAuthor now groups selected
non-solid pots, tyres, drums, bollards, corrugated panels, hedge/fence pieces and
Ilalim loose chairs/crates beyond the physical play edges. Candidates with any
collider retain their gameplay role; monument-related MonHedge stays. Excess art
is retained inactive. Placement uses actual drawn bounds and includes inactive
candidates for stable repeat runs. Eskinita's three retained Mountain paintings
move farther away at smaller apparent scale, preserving texture/material/aspect;
two accidental distant quad colliders are removed. No people or source art edited.
Author session19958 completed successfully, Logs/map-composition-v3-author.log.
It grouped24/20/18 non-solid pieces across Eskinita/Bayan/Ilalim and retained17/6/4
excess pieces inactive. Three paintings retain their artwork. Scene changes are
intentional and not visually accepted yet. Latest restored profile backup:
profile-preservation-a801ef3f11be (17 existing files).

Capture session3591 completed1/1,144 matched FPP views under
Logs/map-owner-composition-v3. Visual review catches two author errors: the
painted quads face away and disappear; Bayan's source hedge is long along localZ,
so the chosen90-degree yaw made perpendicular fingers through the benches.
Corrected both (quad local-Z normal faces court; side hedges use0/180-degree yaw).
Sequence14109 completed; V4 capture passes1/1 with144 matched images. Correct
hedge orientation now reads as a border outside the play wall. The paintings
returned but their peaks barely cleared the roofs: the source alpha's top22% is
transparent. V5 raises only their world placement (side centers34m, north28m),
retaining distance, aspect, texture/material and the smaller angular size.
Completed guarded sequence8769 ran author then Eskinita-only matched
capture, Logs/map-composition-v5-author.log and Logs/map-owner-composition-v5
xml/log/folder, TUMP_MAP_REVIEW=Eskinita. V5 passes1/1 with48 Eskinita images;
the other maps retain V4. The painting now peeks clearly above rooftops at smaller
apparent scale. MapRouteProbe completed1/1 in Logs/map-routes-composition-v5.xml/log:
7420 clear resting samples have connected pickup approaches; all7047 walkable nodes
connect. All18 actual motor routes/pickups pass. CSVs are beside map-composition.md.
This probe (registered
in the match partition) samples a0.5m resting/walkable grid and then drives the real
motor to representative wall/trunk/pillar/monument/kiosk/cart pickup approaches.
It ignores other actors for map-only path planning and disables their collision
during these controlled route cases; this is not opponent/AI avoidance evidence.
The next semantic run FAILED and stopped chained checks/EditMode before launch.
Cause1: relative bounds placement accumulates tiny y-coordinate rounding.
Cause2: Ilalim repeats names like env_bollard; name-only sort ties reordered them
by enumeration order and swapped locations after reopening. Do not loosen the
checker. The author now derives bounds from a fixed origin and sorts by sibling
identity. Benches/chairs also use drawn center/base rather than imported pivots,
correcting possible hedge overlaps. Fresh bounds export through MapFinalInventory.

Sequence60471 completed; V6 semantic comparison passes all16666 rows with zero
changes. Fresh spatial inventories exposed overlapping original crate stacks,
chairs clipping shops, bench orientation and ghost roadside landmarks. V7 fixed
the seating/store aisles and duplicate stacks. V8 adds a coherent tricycle bay and
shaded waiting on Bayan's south apron, clears the monument approach and moves
lanterns/planters/hoops beyond the play wall. Both captures pass1/1 with144 images;
visual review remains separate. Latest outputs:Logs/map-spatial-v8 and
Logs/map-owner-composition-v8, plus author/capture logs/XML. Sequence71051 finished,
profile-preservation-ffac60607bc8 restored17 files; no Unity process remains.
Current visual follow-up: review/reposition/fix facing of Bayan's hoops, which
currently interfere with the civic facade view. Finish final checks/repeatability,
update report/images and push the stable spatial batch. Graphics settings, Sa Bubong
and expanded all-context mash work are approved but not implemented yet. The
remaining full sequence is in EXECUTION_PLAN.md, not forgotten or complete.
The newest source batch is uncommitted after pushed8a22f8b9; do not restore its
intentional map scene changes as if they were the earlier ID-only experiment.
