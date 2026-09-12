# Substantial map transformation plan

Owner-directed revision,2026-09-12. This is the active map plan. It supersedes the
incremental V9 priority/order in older notes, while preserving their evidence.
The whole game-improvement scope remains open. Read AGENTS.md and the newest
ACTIVE_REWORK_LEDGER.md pointer for actual checkout/process state.

## The result the owner is asking for

Three recognizable, nostalgic Filipino places that feel built and lived in, with
clear space for TUMP. The improvement must be apparent in an ordinary first-person
view and in motion. It must come through architecture, connected streets, useful
spaces, surfaces, light, sound and everyday objects. Adding scattered props or
labels does not achieve that. Keep clean cute blocky forms and purposeful detail.

The owner explicitly says the V9 pictures barely look improved and do not feel
more Filipino. The plaza floor is empty, the houses are plain color slabs, Ilalim
has implausible pisonet placement and badly made text/signs, its broad white road
bands look wrong, and Bayan lacks any broader world beyond the arena. These are
acceptance failures, even though V9's routes, author repeatability and tests pass.

## Current state and constraints

- Only ASTRAReworks in the authorized ok-x20 checkout. Verified spatial source,
  native tricycle and receipts are pushed at97e61f92. Preserve that foundation and
  improve it; no reset, main operation or separate Documents/GitHub checkout.
- No Unity or Blender process is active at this plan's creation.
- No new map changes from this revised plan have been implemented yet.
- A never-compiled graphics-resolution/frame-cap prototype is preserved under
  reports/improvement-2026-09-12/graphics-prototype as text/patch. Its changes were
  removed from compiled source to resume maps on the verified runtime. It is not
  a tested feature. Graphics settings remain required later.
- Four players, rotating defender, can, throw/retrieve loop and both modes stay.
  The18 approved people/outfits at7c7fcb5 remain unchanged. No new controls or
  ornamental systems. Character maker remains inaccessible.
- The owner permits changing/removing any map content. Their explicit criticism
  of Ilalim placement/signage overrides earlier instructions to preserve those
  poor arrangements. Preserve proper names where relevant, not broken typography.
- No subagents, paid work or usage resets. All Unity launches use the profile
  guard. No C#/imported-asset edits during Unity runs. One Editor per checkout.

## Evidence to start from, not rediscover

- Logs/map-owner-composition-v9:144 actual FPP views, eight directions per map,
  both modes, Low/Balanced/High;1.25m eye offset,95-degree FOV. The24 Balanced
  Classic views have three direction contact sheets. These show the criticized
  result and are the BEFORE images for this transformation.
- Logs/map-spatial-v9: actual renderer/collider bounds, assets and materials.
- V9 routes:7047 connected walkable nodes,7420 clear rest samples,18 pickups.
  This isolates geometry; it does not certify AI/opponent avoidance or visual merit.
- V9 semantic repeatability:16746 rows with zero saved/reopened drift.
- Historical Ilalim48-idle attribution is still unknown. The independently fixed
  carrier/flight bugs must not be claimed as its cause without a connecting trace.
- White bands are authored scenery: IlalimNgTulayBuilder.BuildRoadSurfaceDetail,
  Crossing_N_0..5 and Crossing_S_0..5. Each is10.6-12.6m wide and.48m deep, spaced
  alongz10.8..15.7. They span the road in the wrong direction for the implied
  pedestrian crossing and dominate the player view. Required TUMP chalk is separate.
- Existing Ilalim road isx[-7,7]; its broad apron reachesx+/-20. The pavement,
  retail threshold and open road need clearer physical/visual relationships.
  Do not choose new shop locations from object pivots or guessed screenshots.

## First: complete the spatial and visual audit

Before authoring each zone, record its actual plan and relevant heights:

1. Plot road/court, kerbs, pavement, physical play limits, building footprints,
   doors, shop counters, stairs, poles, trees and obstacles from loaded bounds.
   Trace walkable routes between them. Identify disconnected doors, blocked shop
   access, props occupying pedestrian/road space, floating items and overlaps.
2. Inspect owner views from legal axis and corner stations, aimed inward/outward
   and toward important landmarks. Add architecture review views for every
   affected front/side/back, roof and street continuation. Overhead views support
   layout decisions; they do not replace FPP or ordinary-speed movement.
3. Inventory ALL Ilalim visible text/signs: actual content, font/material, physical
   size, facing, mounting, legibility, purpose and association with a real shop.
   Find disconnected signs, backwards faces, stretched letters and black panels.
4. Inspect door/window/floor heights against the existing1.6m player capsule and
   neighboring buildings. Imported models can have misleading pivots and scale.
5. Use a small set of real Philippine visual references for construction and
   street relationships, recording URLs and what was observed. Prefer original
   photographers/local institutions. Do not copy a whole real place, brand art,
   sacred ornament or private identifying details. Nostalgia is not a requirement
   to make every building old, poor, dirty or rural.
6. Give each finding a concrete action and inspectable before/after view. Keep a
   live checklist below. Do not expand to random decoration without a place/use.

## Research-informed implementation sequence (current)

Research receipt: [map-reference-research.md](reports/improvement-2026-09-12/map-reference-research.md).
This is the current next-step order after the owner's research request. The earlier
broad design sections remain requirements; this table makes the next work concrete.

| Order | Work | Review/acceptance before moving on |
|---|---|---|
| M01 | Author Ilalim's actual road/pavement/shop cross-section and plot shop/stall/door/pole footprints from measured bounds. Keep roughly2m of continuous pedestrian route where feasible, distinct from the shop activity zone. | A labeled local/district plan, no blocked entrances or isolated furniture; actual court/physical limits retained. |
| M02 | Keep retained chunky shop/house bodies; build substantial shop fronts and integrated local construction details. The four thin V1-V5 studies are rejected. Compare retained-body work in Unity beside the original and an approved person. | Front/side/back and ordinary-distance views, correct scale, openings and supported roofs; no style verdict from Blender alone. |
| M03 | Integrate the pisonets indoors/recessed with chairs, power source, service space and legible shop sign. Move or retire its old cosmetic collision effects and relocate the single cord risk only if its new physical source makes sense. | No outdoor computer row blocking a pavement; no stranded cord or misleading coin/time popup. Trace and preserve necessary runtime ownership before edits. |
| M04 | Rebuild ALL poor Ilalim signs with proper typography, face separation, supports, size and business relationship. Include natural Bawal umihi dito notices and selective original graffiti. | Readable head-on/oblique, no backwards faces, overlapping letters or giant floating text. Gameplay/UI descriptions remain English; local environmental Filipino text is explicitly authorized. |
| M05 | Author original food-frying/pares, fruit-display and clothing/accessory stalls from the owner's references; distribute them in sheltered customer pockets. Add visible stock, tools and storage at their point of use. | Each stall reads by silhouette/use without a paragraph; clear road and continuous through-pavement; stools/umbrellas/colliders do not block retrieval. No copied photo/brand art. |
| M06 | Remove former-hazard trench/loose-manhole decoration and the oversized crossing ladders. Author modest flush road details and properly oriented crossings at real street connections. | Required TUMP chalk is unchanged and dominant; no floating gray sheets, fake holes or unexplained road bands. |
| M07 | Rework Ilalim's structural material weight and distant blocks/rooflines, then capture the whole corridor in both directions and all storefront approaches. | Plausible guideway/services, continued urban world, safe8m clearance and complete physical route tests. |
| M08 | Rebuild Bayan's paving and civic approaches, then lay out connected surrounding roads/blocks and distinguish church/hall/retail/garden sides. Reposition existing landmarks/terminal if the coherent town plan requires it. | Surface readable near and far; beyond-court world visible; doors connect to paths, streets have meaningful corners and the monument/gardens have usable circulation. |
| M09 | Rebuild Eskinita's housing/store family and actual lots/side passages, roof/awning/utility continuity and domestic activity. | The result no longer reads as colored slabs or repeated generic houses; corner/side/back views and footsteps/retrieval remain coherent. |
| M10 | Revisit all3 together for lighting, material balance, tree density, selective ambience and other problems found. Record additional concrete findings, not just owner-listed defects. | Matched FPP/corner/district images and full-speed sequences show a substantial transformation; both modes and scalability remain sound. |
| M11 | Run repeatability, route, appropriate code/check/source and targeted PlayMode gates, push stable batches, then proceed to the requested movement/Pektus/equipment and remaining TODO work. | Fresh nonzero evidence, preserved profiles/IDs and honest unfinished criteria. Final exact-player release qualification remains mandatory. |

User-provided original references survive locally in Logs/map-owner-references-2026-09-12.
The supporting Gilmore/Pila/Project8 photos are already in Logs/reference-review.
Do not drop the explicit vendor/signage/nostalgia requests at compaction.

## Place designs and substantial changes

### Ilalim ng Tulay: an established commercial street under a guideway

The deck/columns organize the district. A continuous road and continuous pavement
lead past connected buildings and shops. Electronics/repair/food activity belongs
at real storefront thresholds. This should recall an urban street people know.

- Rebuild the roadside cross-section. Keep a clear road, readable kerb/drain,
  pedestrian through-route, then shops. A broad undifferentiated apron currently
  makes furniture and computer kiosks look dumped in traffic. Correct geometry
  and placement together, including both sides and both ends.
- Retire the current oversized transverse crossing bars. Place a properly oriented
  modest crossing where sidewalks actually connect, toward an appropriate street
  junction outside the active court. Use one coherent width and restrained paint.
  Avoid giant ladders, repeated stripes at arbitrary intervals and extra rule-like
  lines in play. Preserve the actual TUMP chalk and its derived dimensions.
- Put the pisonet computers inside a recessed computer-rental shop with visible
  floor, back/side walls, roof, counter/entry and credible power routing. Remove
  excess duplicated outdoor kiosks. Keep any needed interaction/collision only
  after tracing its runtime owner; no stranded pickups behind unreachable surfaces.
- Recompose PC/electronics and repair fronts with rolling shutters, framed windows,
  shallow awnings attached to walls, visible thresholds and a few purposeful goods.
  Food preparation/waiting belongs in sheltered frontage, not the traffic lane.
  Keep a usable pedestrian passage past the shop activity.
- Redesign every identified bad sign as part of its actual building. Clear primary
  shop name, quiet service information only where useful, deliberate margins and
  letter spacing, legible contrast, correct facing and plausible physical size.
  Use English descriptive copy and preserve relevant proper names. No floating
  text, huge duplicate advertisements or a sign for every square metre.
- Replace generic repetitive frontage with a restrained mix of masonry shop-houses
  and narrow walk-ups. Give ground floors different uses; upper floors need real
  window rhythm, ventilation, balcony/roof construction and believable support.
- Review the whole guideway from below and its visible ends: bearings, drainage,
  service access, columns and structural connections. Keep the8m clearance; do not
  let embellishment hang into throwing/ability space. Preserve existing conductor
  logic while making poles/wires connect to plausible receiving points.
- Extend the commercial street past the map through intersecting roads, staggered
  rooflines and additional building depth. Parked vehicles must have a reason and
  fit a bay/kerb. No traffic mechanic is required.

### Bayan Plaza: a civic square within a connected town

The church/hall, monument, gardens and public gathering ground belong to a wider
poblacion district. The square needs a surface people can recognize and streets
that visibly lead somewhere. It should not read as a beige platform inside a ring.

- Replace the featureless center with authored stone/concrete paving: believable
  slab scale, expansion joints, restrained aggregate/grain, slight value variation
  and limited repairs/wear. Use original deterministic material work with correct
  world scale, mipmaps and distant sampling. No high-frequency noise or black grid.
- Give peripheral walks/civic approaches material bands and thresholds that connect
  doors, benches, gardens and crossings. The active TUMP ground stays readable,
  with no new movement bumps and no decoration that imitates gameplay boundaries.
  Inspect both near-foot detail and the full court; a faint texture that disappears
  from normal play is not enough to answer the owner's complaint.
- Plan the surrounding road network and blocks. Connect the terminal/waiting strip,
  side streets and civic entrances; provide crossings at real desire lines. Build
  corners and street mouths, then a second layer of roofs/upper floors beyond them.
  Close exposed ground-plane/sky gaps through coherent urban depth, not a random
  skyline wall. The far side of every visible street should have a purpose.
- Author town-house/shop-house frontage with appropriate masonry, shaded windows,
  timber or louver details where fitting, GI/clay roof transitions and doors onto
  actual pavement. Vary massing, setback and roof height deliberately. Keep civic
  buildings recognizable; review their sides/backs and connect their entrances.
- Revisit the monument grounds, tree density, bed edges, seating and waiting areas
  as one civic layout. Trees provide shade and frame views; trunks must not stand
  in vehicles, paths or doors. Do not fill the empty center with obstacle clutter.
- Keep the new blocky tricycle only where its terminal placement supports this
  larger layout. It is one useful prop, not the explanation for Filipino identity.
- Review basketball fixtures and all retained benches/tables against the final
  multipurpose-square use. Remove or relocate anything without coherent use/access.

### Eskinita: a familiar residential street with everyday life at its edges

The current colored house slabs need actual construction and inhabitation. Make
connected homes and small local businesses the main visual change, with detail
concentrated near doors, windows and useful street corners.

- Author a small coherent family of clean blocky buildings, with full visible side
  and back geometry. Candidate types: low-rise masonry home; masonry ground floor
  with timber upper rooms; house with a sari-sari service window; narrow walk-up
  with shaded balcony. Choose placements and proportions from the audit.
- Give homes recessed openings and layered construction: jalousie/louver windows,
  modest grilles, transoms or vent blocks where appropriate, real door frames,
  plinths/steps, overhanging roofs, GI-sheet seams, gutters/downpipes and gates.
  Use broad readable forms, not tiny mechanical detail or realistic surface grime.
- Add purposeful household/shop objects at their points of use: a bench under
  shade, water containers near service access, a few pots with soil/drainage,
  laundry in a private side yard or upper utility area, goods at a service window.
  Vary these per household; do not stamp the same prop kit on every facade.
- Replace mirrored frontage repetition with plausible lots, party walls, small
  setbacks and side passages. Every door/step/gate must meet usable ground.
  Preserve the competitive space and retrieval routes while rebuilding its edges.
- Connect the street ends to more neighborhood: corner buildings, an alley/turn,
  roof silhouettes and utility continuity. Retain the original mountain paintings
  only where their scale/position makes geographical and visual sense.
- Material language: faded but deliberate paint, exposed masonry at repairs,
  timber/metal at actual joints, warm gray concrete and quiet GI roofing. Avoid
  generic saturated toy-building slabs and broad realistic dirt overlays.

## Shared visual and feel work

- Use a consistent material/lighting balance across scene authoring and builders.
  Preserve recognizable chunky silhouettes, flat graphic people and role colors.
  Inspect how outlines interact with roof edges, window depth and textured ground.
- Spend detail on construction and use. Merge static parts/material groups where
  it helps measured performance. Do not scatter hundreds of decorative objects or
  add foliage to hide unfinished architecture.
- Evaluate shadow scale, contact, ambient fill and bright sky exposure through
  actual player views. Make depth readable under Ilalim and outdoor shade without
  washing out paving or turning foliage undersides into featureless black masses.
- Add/adjust restrained local ambience only after the visual place/use is settled:
  distant street/household activity, shop hum, appropriate space under the deck.
  Keep movement, throws, impacts and ability counters audible. No forced music
  or constant stereotyped cues. Follow current audio ownership/audit contracts.
- Restore worthwhile graphics scalability after the maps have a convincing visual
  direction. Measure images and frame cost on this exact scene state; stronger
  hardware should gain clean detail, lower settings must keep gameplay tells.

## Implementation order and durable checkpoints

1. **Audit and plan the real spaces**, beginning with Ilalim's crossings, pisonets,
   signs and frontage, plus Bayan's paving/world edges. Record measured plans and
   before views. Use the owner complaints as mandatory checks, not optional taste.
2. **Ilalim layout/signage batch:** correct crossings and shop placement; author
   coherent shop spaces and signs, then inspect all player approaches and updated
   routes. Include enough building work for the changed street to read as a place.
3. **Bayan ground/world batch:** author paving and connected civic walks, streets,
   corners and layered town blocks; review monument/garden/seating as part of it.
4. **Eskinita architecture batch:** build/apply a small native house family, connect
   lots/entries/rooflines and household details; improve street-end continuity.
5. **Whole-map coherence pass:** revisit all three from every direction, including
   side/back/roof visibility, materials/light, broader world and simple ambience.
   Correct concrete problems discovered along the way without waiting to be asked.
6. **Verify and push stable batches:** semantic two-run authoring; updated physical
   grids and real retrieval routes around changed collisions; appropriate
   Core/EditMode/check/source gates and targeted PlayMode. Preserve profiles.
   Images/ordinary-speed recordings must show the change; totals cannot approve it.
7. **Resume remaining larger scope:** graphics settings/performance; all-context
   recovery mashing; approved Sa Bubong with actual falls and10s shoe penalty;
   six whole kits/alternatives, same-hero build binding, Phaister/Kuro qualification,
   movement/FPP/action synchronization and networking. No silent scope deletion.
8. **Final qualification:** both modes, active effects/quality profiles, ordinary
   speed, isolated gate twice when release-ready, then Windows build and that exact
   executable. Desktop/internal executables are older and are not this checkpoint.

Each batch records: files/author entry point, current branch/HEAD, before/after
views, measured layout, collision/route implications, evidence, remaining issues,
active run IDs and next concrete action. Update EXECUTION_PLAN and the newest
ledger pointer before long runs and after results. Commit sole-author messages
from files; push only ASTRAReworks. Any actual handoff belongs directly in chat.

## Visible acceptance checklist

- [ ] Ilalim has no pisonet furniture pretending to belong in the road.
- [ ] Ilalim road/pavement/store thresholds are physically and visually distinct.
- [ ] Ilalim crossing geometry connects real walking routes and no giant ladder
      competes with the chalk or fills the pictured street.
- [ ] Every visible Ilalim sign has correct text, facing, scale, mounting and purpose.
- [ ] Bayan's central paving has visible intentional material/construction at both
      foot and normal court distances; chalk/can/loose shoes remain clear.
- [ ] Bayan has connected streets, town blocks and convincing depth beyond the arena.
- [ ] Houses have real openings, roof/edge construction, entries and distinct uses;
      the main impression is no longer flat slabs of color.
- [ ] Filipino nostalgia reads through coherent familiar places and everyday use,
      without forced decoration, noisy props or altered people style.
- [ ] All visible front/side/back/roof relationships make sense, without overlaps,
      unsupported canopies, blocked doors, floating objects or exposed backdrop gaps.
- [ ] Actual retrieval routes and raised/unreachable-surface recovery work after
      layout changes; both modes and ordinary-speed play remain sound.
- [ ] Final paired images and motion show a substantial improvement. The owner has
      not approved V9 as meeting this requirement; never infer that from test passes.

## Current next action

Research is recorded. Complete M01 using the measured Ilalim plan, then validate
and integrate M02-M07 as coherent shop/street batches. Continue M08-M11 for Bayan,
Eskinita and whole-map review. Do not stop after an isolated prop or texture. No independent agent work. The graphics
prototype stays preserved as text until this map direction is materially improved.

### Audit finding added after initial plan

Ilalim's signs are assembled from many tiny mesh pieces, rather than a coherent
sign face: Billiards162 renderers, Pisonet107, Xerox95, Pares82, Panaderia82,
PC Repair78 and more. The under-bridge sign group adds321. They need a visual
redesign and materially simpler authored faces; preserve legibility without the
fragmented geometry. Measure rendering cost after replacement.

The three pisonet units currently occupyx8.625..10.125 atz1.16..5.74, between the
road edge atx7 and the shop row nearx11.3. They have PisonetInteractive components,
and TripHazard_PisonetCord is a separate nearby gameplay object. Repositioning
must trace/relocate the associated controls/cord/hazards together, or deliberately
retire a hazard that no longer has a sensible physical source. No secret removal
of interaction and no cable left in the lane after the machines move.

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

### M01 measured placement proposal

See MapSource/environment/layouts/ilalim-place-plan-v1.json and the
[proposed plan](reports/improvement-2026-09-12/map-transformation-audit/Ilalim-proposed-layout-v1.png).
11 shop lots are non-overlapping;4 vendor ground footprints avoid the proposed
pedestrian band. Ground/awning/door/pole clearance still needs loaded-scene review.
The existing invisible bounds are6m tall at innerx+/-11,z+/-16.5. Align private
storefronts with real closed/service/glazed edges; do not make an apparently
walk-through door blocked by an unrelated invisible wall. Revisit end-boundary
presentation/physical correspondence as part of the whole-map critique.

Pole count alone is misleading:28 pole renderers span about159m of extended
street. Do not simplify this to28 poles within the48m central map. Verify actual
positions/connections and change only what improves the real composition.

## Owner rejects flimsy house construction

2026-09-12: the owner says the new houses look flimsy, too thin, as if wind could
knock them over, and lack the old houses' feel. V1-V5 thin-panel/wafer-roof studies
are NOT accepted. Their added detail does not compensate for lost solid blocky
mass. Do not revive them as the approved architecture after compaction.

Use retained old house silhouettes/proportions as the baseline. Walls, roof edges,
window/door frames, plinths and supports must have substantial chunky weight.
Integrate Filipino construction/use into that mass; do not make spindly realistic
frames. The next study must be compared directly beside a retained old house AND
an approved person in Unity before broad placement. Existing map houses have not
yet been replaced, so their source/direction remains intact. The broader map
transformation and all later play-feel/TODO scope remain open.


### Owner intersection receipts,2026-09-12

Bakery and pares screenshots expose signs/roof slabs passing through the old
utility poles. This is a placement failure, not acceptable detail. Check every
sign and canopy against shaft-height geometry, roof surfaces, wires and neighbors.
V3 moves the utility row to its own pavement strip and rebuilds its conductors;
signs occupy measured clear spans. Required follow-up: actual FPP/oblique review,
mesh overlap checks and updated retrieval/footpath clearance. Do not claim success
from a picture showing only the sign face.

### M08 actual review findings,2026-09-13

V1 player views show the new slab texture and town depth, but a tree pair
intersected basketball backboards. V2 removed that pair and strengthened the
terminal shelter. V2 district views found4 old fence objects in new roads; the
43 active building footprints themselves had0 AABB overlap candidates and0 road
corridor overlaps. Do not confuse a house-only audit with a complete spatial pass.
V3 retires those fences, gives roads real kerb depth/collision and adds closed
side/rear nave and municipal windows/service access. Court boundary clarity at
open-looking path mouths remains part of M10, alongside every side/back, route,
lighting and ordinary-play review. No full artistic acceptance is implied.
