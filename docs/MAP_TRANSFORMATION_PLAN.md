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

Produce the measured Ilalim shop/road/sign inventory and a top-down placement plan,
then author its first coherent storefront/crossing revision. In parallel read/prep
Bayan's paving and surrounding-block plan. No independent agent work. The graphics
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
