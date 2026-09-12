# Historical working-plan snapshots

Superseded by the current docs/EXECUTION_PLAN.md. Preserve reasoning; do not treat
old process IDs or priority orders below as live.

# Active execution plan


**ACTIVE MAP PLAN: [MAP_TRANSFORMATION_PLAN.md](MAP_TRANSFORMATION_PLAN.md).**
The owner rejected V9 as insufficient visually. Follow this substantial revision
before the older incremental/graphics-first order; preserve all broader scope.
Updated2026-09-12 at the owner's request so work survives compaction. This is the
current action plan and working state, not a claim of completion. The whole
improvement goal remains open. AGENTS.md is the primary self-contained rulebook;
ACTIVE_REWORK_LEDGER.md preserves detailed decisions and experiments.


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

## Current checkout and durable boundary

- Repository:DOST-GameDEV/TumbangPreso-Unity. Branch:ASTRAReworks ONLY.
- Checkout:`C:\Users\Matthew\Documents\Codex\2026-09-09\ok-x20\work\TumbangPreso-Unity`.
- Started after fetching clean local/remote0f08e992. Current pushed HEAD:
  `97e61f92` (verified spatial batch; inspect git for its full hash).
- No resets, main operations or changes to the separate Documents/GitHub checkout.
- No subagents, paid work or usage resets. Preserve profiles and saved IDs.
- Every Unity launch uses tools/run_unity_guarded.py. One Editor per checkout;
  no C#/imported-asset edits during a run. AssetImportWorker children are not
  second independent Editors. Never terminate an unrelated process.
- All Unity/Blender runs are COMPLETE. No Editor is active. Verification sequence
 85437 passed8 checks,489 EditMode tests and MapRouteProbe1/1 (18 pickups).
  Last profile snapshot: Logs/profile-preservation-368dce845f87;17 files restored.
  V9 semantic comparison and all14 source audits passed too. Known test-generated
  arm tangents and quality-setting changes were verified/restored.


## Current live execution

Guarded Unity run75613 is COMPLETE (1/1,42 images, profile snapshot
 c616a5c3ea92 restored17 files): MapExperienceProbe.ArchitectureAndStreetContinuity,
Logs/map-transformation-architecture-before-v1.xml/log and folder. It captures42
architecture/corner/street-continuation views of the current maps. The1.65m ground
witnesses and elevated district cameras are explicitly architecture views, not the
actual FPP rig or ordinary-speed play. No Unity process is now active. Current HEAD is e7a6f90b, the pushed substantial-map plan;97e61f92 is the last
map implementation. Post-map movement/Pektus/equipment/TODO instructions have been
added durably since that plan commit and still need their next documentation push.

New tool tools/map_layout_review.py reads inventory JSON and uses existing Pillow
and the repository's WorkSans font to plot measured local/district footprints.
Logs/map-transformation-audit-v1 contains6 diagrams and findings.json. Building
AABB overlap counts58/46/65 are inspection candidates, not proven intersections.
Ilalim has1162 sign renderers. PisonetInteractive is cosmetic collision-triggered
popup/audio/light, not a score/economy system; its +5 COIN/10 MINS text is also a
candidate for removal from this scenic shop because it implies nonexistent play.

## Latest owner verdict overrides the previous priority order

2026-09-12: the owner says Bayan's center looks empty, the images barely look
improved, and the maps do not feel more Filipino. **The V9 result is insufficient
for the requested thorough visual/map transformation.** Its technical receipts
remain valid; do not present the closed spatial batch as artistic acceptance.
Substantial three-map review, revised improvement plan and implementation take
priority over graphics, mashing and Sa Bubong for now. All original broader
requirements remain open; the owner has not cancelled them.

The just-written graphics prototype was never compiled or tested. It is preserved
as non-compiled text under reports/improvement-2026-09-12/graphics-prototype,
including an integration patch. Its exact changes were removed from compiled
source so map review stays on the verified runtime. No Unity process is active.
Next: inspect physical architecture, surfaces and sightlines; use fitting real
Philippine place references; record a substantial plan per map and implement it.
First concrete complaint to address: Bayan's large plain center needs intentional
stone/concrete surfaces, joints and wear, while keeping chalk/slippers legible.


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

## Latest owner requirements

1. Thoroughly improve look and feel into a fully realized, surprising game, while
   keeping the cute blocky style and natural Filipino character. Passing tests
   alone is not enough. Useful additions are authorized if they do not complicate
   the player's game.
2. Anything in the maps may be added, removed or changed. Preserve the intended
   places: Eskinita's neighborhood street, Bayan's civic plaza and Ilalim's urban
   guideway/shop district. Every placement must make spatial sense. Do not protect
   bad legacy layout simply because it exists; do not add forced flags/slogans.
3. Thorough graphics AND graphics-settings work is required: playable lower-spec
   settings and visible benefits on stronger hardware, with measured frame/image
   results and readable gameplay at every tier. Functional graphics UI is in scope;
   a general UI-art remake is not the priority.
4. **Sa Bubong is APPROVED.** The owner first chose 'Revise', then clarified the
   changes and said they already liked the condo roofdeck concept, requesting a
   Filipino name. Sa Bubong is the selected name. No further concept approval is
   pending. Keep clear central play, fenced pool, shaded residents' area and a
   separate stairwell/utility/laundry corner above a Metro Manila neighborhood.
5. Sa Bubong has actual falls only at the very edge. Players can button-mash to
   get back up. Slippers that fall off remain unavailable for about10 seconds,
   then teleport to a safe rooftop location. The wait is explicitly a penalty.
   Preserve both modes, four players, rotating defender, can and retrieval.
6. Verify button mashing in ALL recovery contexts and keyboard/controller/touch/
   network paths. Earlier claims are not a substitute for actual input evidence.
7. Keep the approved18 people/outfits at7c7fcb5. No new person redesigns, thumbs,
   realistic anatomy or noisy reconstructed models. Animation includes body/FPP,
   casting, moving skill geometry/VFX, impact, interruption and recovery. Distinct
   actions need appropriate distinct sequences. Keep English descriptive copy.

## What is already pushed in this session

8a22f8b9 contains the independent slipper corrections and durable instructions:

- Direct HostThrow retained Carrier.Held because it passed the same owner to a
  helper that preserves unchanged holders. It now detaches the prior carrier.
- Normal Carrier.HostThrowAt cleared Held, but the flight query saw the guideway
 9.04m overhead and immediately landed/recovered a throw released at3.6m. Flight
 now samples support below its swept step and passes that same support into Land.
- Unchanged-runtime baseline0/2; after4/4 PlayMode, including fast descent onto
  a1m slab and unreachable3m roof recovery. Historical48-idle attribution is still
  OPEN; no causal link to that run is claimed.
- Core562/562, graphical EditMode489/489,8 checks and14 gating audits passed.
- MapRepeatabilityCheck compares saved/reopened author runs by loaded component,
  reference, mesh/material and lighting semantics, not regenerated IDs.
- AGENTS.md became the self-contained primary instructions. CLAUDE.md retains
  historical reasoning; current read-order links point to AGENTS first.
- Five inherited fixtures omitted from the isolated PlayMode partition were added.

Reports:reports/improvement-2026-09-12/map-retrieval.md and map-repeatability.md.
The original unsuccessful flight experiment remains archived unchanged as text.

## V9 map batch, verified and pushed at97e61f92

**Map authoring:** MapFinalPassAuthor.cs and MapFinalInventory.cs, all3 map scenes,
four new terminal materials/meta files under Art/MapFinalPass. Scene changes are
INTENTIONAL. Do not restore them as if they were the earlier ID-only experiment.

- Grouped non-solid plants, tyres, drums, loose panels and furniture at sensible
  edges. Retained excess inactive; preserved actual interactive/colliding objects.
- Moved/scaled Eskinita's existing mountain paintings as distant landscape, with
  original artwork/material/aspect and corrected facing. Removed2 accidental
  colliders on distant picture quads. The alpha image has22% transparent top space.
- Fixed original overlapping crate stacks, wrong bench orientation and imported
  pivot assumptions. Placement now uses drawn bounds from a fixed origin.
- Corrected repeatability: duplicate Ilalim sibling names needed sibling identity
  in the sort; relative world-coordinate subtraction caused tiny vertical drift.
- Latest V8 Bayan: open monument approach (redundant ghost railings retired),
  perimeter lanterns, aligned basketball hoops, fewer garden borders, a southern
 3-tricycle bay with parking marks and shaded waiting. Moved trunks behind that
 bay; retired unrelated boulders/extra vehicles and a placeholder flagpole.
- V9 clears the civic frontage with inward-facing east/west hoops and brackets.
  A new original motorcycle/sidecar model replaces3 ambiguous cargo-like terminal
  props. Blender source: MapSource/environment/terminal; author:
  tools/author_terminal_tricycle.py. Owner views show clear parked silhouettes,
  shaded waiting and separate trunks. All24 Balanced Classic directions reviewed
  as3 contact sheets in Logs/map-owner-composition-v9. Final route/checks and
  broader side/back architecture/ordinary-play qualification remain open.

**Verification tools:** MapExperienceProbe.cs/meta, MapRouteProbe.cs/meta,
MapRetrievalProbe.Load made internal with a mode argument, playmode_suite.py
registers the new fixtures. Both new capture/route fixtures are WallClock and must
be run explicitly; registration does not mean the ordinary gate runs them.

**Documentation/evidence:** TODO/closed152.7, active ledger, execution/map plans,
reports/improvement-2026-09-12/map-composition.md, current V9 images/metadata/route
CSVs and semantic receipt. Older V5 images/route CSVs remain historical evidence.

**Test-generated dirt:**24 named-person arm tangent streams were byte-verified
(position/normal/UV and other mesh properties unchanged), then restored. The
Balanced profile written into QualitySettings by tests was restored too. Future
runs may repeat this; restore only confirmed generated changes, never map scenes.

## Exact evidence and limitations

| Evidence | Result | What it proves |
|---|---|---|
| Logs/map-owner-baseline-v2 | 144 static views, correct rig metadata | Legal owner cameras in both modes at Low/Balanced/High; not ordinary play |
| Logs/map-owner-motion-v3.xml and folder | 1/1, all6 map/mode sequences | 16s each at1x, actual input-driven carry/charge/release/retrieval; owner/body recordings encoded by real timestamps |
| Logs/map-routes-composition-v9.xml/folder | 1/1,18 actual pickups | 7420 clear resting samples have approaches; all7047 walkable grid nodes connect; representative wall/trunk/monument/pillar/kiosk/cart routes work |
| Logs/map-repeatability-composition-v9/report.txt | PASS | Two saved/reopened runs:2388/3135/11223 rows, zero differences, including baseline to first run |
| Logs/map-owner-composition-v9.xml/folder | 1/1,144 static views | All144 camera metadata rows match baseline exactly;24 Balanced Classic directions reviewed |
| Logs/map-spatial-v9 | Fresh bounds inventories | Actual active surfaces/colliders for spatial review |

All144 baseline/final V5 camera rows matched exactly (position, eye offset1.25m,
yaw,95-degree FOV, quality). V9 also matches all144 metadata rows exactly. Earlier witness
cameras used1.65m/52degrees and Eskinita side stations outside its8.1m wall.

Do not rediscover these fixture errors as game bugs:

- Original north station was inside another parked player's head.24/144 frames
  were over80% black; self-hide was correct. StageOtherSeats plus separation checks
  repairs the camera staging. Diagnostic images are in map-owner-occlusion-v1 and
  map-owner-baseline-v2 (separated-seats control).
- Coroutine input could lose Grab before Carrier.Update because motor FixedUpdate
  committed the edge. MapExperienceProbe's test-only ReviewInput writes at order
 -300, matching real producers. V3 succeeds without changing runtime input.
- The first EditMode run used -nographics and failed2 illumination fixtures.
  Graphical rerun passed489/489. Do not weaken those tests.
- Semantic V5 failed on real enumeration swapping plus tiny relative-placement
  drift. The author was repaired; the semantic checker was not loosened.

## Next actions, in order

1. DONE: verified V9 spatial batch pushed at97e61f92 with report and closed152.7.
   Broader architecture/side-back/ordinary-player review remains open after this
   batch; do not call all maps complete. The source/scenes/model and evidence are
   committed, and all current Unity runs have finished. No new graphics runtime,
   mashing changes or fourth-map code has been implemented yet.
2. Establish rendering/frame-cost baseline. Active pipeline is Built-in, despite
   URP being installed: ColourGrade/PostAntiAlias/WorldOutline use OnRenderImage.
   Current3 quality profiles only vary shadows/lights/soft particles. VIDEO uses
   uGUI ConvertedSettingsPanel. AA is separate; MSAA currently disables HDR as a
   historical white-fringe workaround. Read those mechanisms before changing them.
3. Implement/test graphics scaling and meaningful quality gains. Candidate design
   is3D resolution scaling/supersampling with native-resolution UI, a useful frame
   cap, clearer preset tradeoffs, and restrained contact/material/light quality.
   One simpler prototype candidate is a scaled gameplay-camera target presented
   by a lowest-order non-raycasting overlay RawImage, with normal UI drawn above.
   This avoids a second presentation camera, but is NOT yet tested/settled.
   Linear/HDR transfer, outline thickness, viewport-based offscreen indicators,
   external capture targets, replay and camera lifecycle all need explicit checks.
   **No graphics-settings/runtime implementation has been made yet.** Prototype
   actual image delivery before committing to a render approach. Do not let scaling
   change aiming, HUD placement, capture/replay or render color. Retain save IDs and
   preview/Back/discard behavior. Check keyboard/controller/touch navigation.
4. Reverify and repair all recovery-mash paths using real input events, not only
   direct method calls. Keyboard, simulated pad/touch,30/60/144Hz, held-vs-tapped,
   quick taps, stun elements, trip, minimum recovery, host/remote/delay/rejoin.
   Preserve the controller owner's GenericPadBridge/MenuNav area. Physical device
   certification is not implied by simulated input.
5. Build Sa Bubong and its approved edge/10s slipper/mash mechanic. Plan physical
   floor/edge/railings, return anchors, fall presentation, cleanup, AI goal limits,
   both modes and networking together. Avoid fake floor support outside the roof.
   Reuse existing get-up/input/teleport authority where sound; no new controls or
   health system. Test fall/bounce/retrieve/10s timing, duplicate messages, role/round/
   rematch changes and reconnect. Integrate map selection, previews, stories, votes,
   build scenes and all map checks. A wire/schema change needs deliberate protocol
   compatibility work, not an input-driven bump. No fourth-map assets/code yet.
6. Continue the remaining broader abilities/network/animation request. Whole kits
   and all existing alternatives need distinct jobs, tradeoffs and counterplay.
   Same-hero build binding in MatchRpc still returns early; fix without resetting
   live kit state or multiplying loadout scales twice. Phaister still has10.5m reach,
   no constructor windup, inscription after control and11m moon above8m guideway.
   Design invocation/inscription/alignment/payoff/pulses/unbinding coherently.
   Finish the other5 kits, normal carry/backwards/turn/sprint cadence, body/FPP and
   effect/SFX synchronization, interruption and full-speed overlap review.
7. Broaden real network/lifecycle coverage. Kuro protocol28 staged/yaw/rejoin,
   active effects, alternatives, both modes, rounds/rematches, reconnect and host
   loss remain open. Kuro33/33 and6 clearance positions were earlier local evidence;
   retain both purple forms, expressions and accepted native style. Do not revive
   rejected realistic/crab/noisy studies or redesign the18 people.
8. Investigate the historical Ilalim48-idle outlier without attributing it to the
   now-fixed flight fixture. Original aggregate report exists, but seed-specific
   resting-position evidence has not been found here. Current bot traces log loose
   shoe positions; targeted/several fixed-seed runs can identify current outliers.
   Do not change seeds or balance merely to obtain a pass.
9. Final qualification only when implementation is ready: Core, graphical EditMode,
   checks/source audits, isolated PlayMode gate twice, targeted visual/mash/edge/
   network evidence, then clean Windows build and that exact executable at ordinary
   speed in both modes. Desktop/internal executables currently predate this work.

## Verification entry points

- `python tools/run_unity_guarded.py -batchmode -runTests -buildTarget Win64
  -testPlatform PlayMode -testFilter <single fixture or method>
  -testResults Logs/<unique>.xml -logFile Logs/<unique>.log`
- EditMode uses the same guard with `-testPlatform EditMode`, graphics enabled.
- `MapFinalPassAuthor.Run`, `MapRepeatabilityCheck.Run` and `Checks.RunAll` are
  under `TumbangPreso.EditorTools` (first2 also `.MapKit`). Use the guard.
- Author inventory env:TUMP_MAP_INVENTORY. Semantic output:TUMP_MAP_REPEATABILITY.
  Camera output:TUMP_EVIDENCE; optional validated TUMP_MAP_REVIEW selects1 map.
  Route output:TUMP_MAP_ROUTES. Output folders always versioned.
- `python tools/qualify.py --stage audits` runs14 gating audits; informational
  audio audit currently flags7 files. `playmode_suite.py --plan` checks coverage;
  `--gate --twice` is the release gate. Nonzero fresh XML and expected coverage are
  mandatory. Pipeline zero-test results are not passes.

Keep this plan's current-state sections and the newest active-ledger pointer up to
date. Retire finished process/session IDs. Commit messages use files and sole
authorship; push only ASTRAReworks. No handoff-prompt file. Further handoff goes in
chat, with unresolved criteria explicit. Compaction is continuation, not a restart.

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
