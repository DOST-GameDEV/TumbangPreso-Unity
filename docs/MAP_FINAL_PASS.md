> ACTIVE again after the owner's2026-09-21 "finish everything note yet done"
> correction. Resume actual unfinished work from current source/evidence; preserve
> completed map foundations. Old checkpoint/scheduling statements below are history.
> Follow [the current queue](TODO.md#current-implementation-queue).

**Newest owner playtest corrections:** [OWNER_PLAYTEST_REVISION.md](OWNER_PLAYTEST_REVISION.md).
Current implementation/run state is the newest active ledger pointer.

# Final map improvement pass

## Active material and sky revision,2026-09-21

Owner feedback after current captures: everything feels blank or poorly textured;
give all buildings considered materials, avoid repeating one treatment, and improve
the empty sky. This adds explicit surface/sky acceptance to the full map task.

The current four-map baseline is Logs/full-backlog-maps-baseline-v1 in validation:
200legal owner frames, both modes and three quality levels. A skyline-only roof
refinement already has0protected-scene changes/0repeat differences and50new frames;
it improves depth but does not close this newly specified surface/sky work.

### Explicit sky-motion addition, 2026-09-22

The owner asks to animate the sky, not leave it static. Use restrained panorama
cloud drift at 0.035 degrees/second in Eskinita, 0.025 in Bayan, 0.045 in Ilalim
and 0.022 on Sa Bubong. The sun/horizon remain fixed. A single scaled shader clock
serves live maps and previews; recorded views temporarily sample their existing
clip timestamps and restore the live clock even on rendering failure. No new
network/replay bytes are needed for this deterministic presentation. Verify actual
pixel movement, reverse seeking, pause/resume and normal-speed native footage.

### Surface design and implementation order

1. Inventory actual visible building meshes, atlas UVs/materials and the runtime
   EnvColourPass/NearFade conversion before authoring. Preserve original imported
   meshes/GUIDs, owner imagery, shop lettering, vehicle livery and approved colors.
   Avoid a universal noise overlay or an expensive material split on every triangle.
2. Assign a material language by construction and place. Eskinita: warm plaster
   with broad paint variation, restrained lower-wall exposure, corrugated or tiled
   roofing, timber shutters/doors with directional grain and quieter window panes.
   Bayan: finer civic masonry/plaster, legible stone joints at appropriate scale,
   terracotta roofs, timber trim and distinct storefront materials. Ilalim: cast
   concrete with broad formwork/joint/weather directions, metal shop shutters and
   roofs, recessed glass, small repair/retail interiors and retained independent
   signs. Sa Bubong: painted/stucco residential facades, balcony concrete/steel,
   quiet glass and waterproof roof/coating, retaining readable ceramic pool detail.
   The lagoon expansion will use actual board construction, weathered timber,
   different painted house finishes and restrained water reflection instead of
   inheriting an urban plaster treatment.
3. Use separate detail scales/forms for each material: plaster grain and patching,
   concrete seams, roof ribs or tile courses, wood grain along boards, metal edge
   wear and controlled glass response. Broad variation should read at normal
   distance; fine detail should recede. Distribute by authored surface/instance
   purpose rather than changing every building's random seed over one recipe.
4. Replace the empty gradient with layered stylized clouds, lit/shaded cloud form,
   horizon haze and a restrained sun. Give the alley/civic/underpass/roof/lagoon
   distinct atmosphere through coverage, height, light and horizon composition.
   Keep sky detail behind the action, preserve weather/ultimate/replay transitions
   and avoid screen wash or rapid distracting motion.
5. Integrate with the existing lighting, outline and near-camera fade. A texture
   that disappears when NearFade swaps the shader is not implemented. Keep source
   shader stripping/build registration and bounded rendering cost explicit. Low
   must retain material identity and important silhouettes even if fine detail falls.
6. Inspect close, oblique and normal owner/observer distances, both sides of each
   map, sunny/shaded surfaces, Low/High and busy ability overlaps. Compare matched
   baseline images and normal-speed native motion. Reject tiling, texture stretch,
   noisy surfaces, floating decals and fog that erases the objective. Preserve
   clear routes/colliders; use existing all-map route and geometry evidence when
   unchanged rather than rerunning it for every material adjustment.

Only actual implemented and inspected results close these criteria. A palette
change, generated texture sheet or shader existing on disk is insufficient.

### Full environment detail mandate and coverage

The owner then clarified: "i want u to thoroughly add detail to everysingle thinge
ven if it takes time Do it thoroughly pls plan first so that this shit survives
comapction". This means a considered finish for the entire environment, not only
one demonstration facade. Detail includes construction, material response, placement,
light and motion. It does not mean identical dirt, cracks or symbols on every object.
The full TODO remains assigned while this map work is prioritized.

Use the existing MapFinalInventory to account for every active scene renderer and
its mesh, material and texture references. The expanded inventory includes all
registered maps and the sky. Output lives in validation Logs/full-backlog-surface-
inventory-v1; summarize families and unresolved surfaces in the full-backlog report.
Inactive retained/rejected scenery is preserved, not reactivated to increase detail.
Every active family needs an explicit treatment, inspected retained finish, or a
specific remaining gap before the map is closed.

#### Walls, structure and small architectural surfaces

- Plaster and stucco: broad, low-contrast paint variation plus a finer mineral
  grain. Weather collects selectively near ground, runoff and roof edges. Keep
  sheltered upper walls cleaner. A patched alley house, maintained civic wall
  and newer condo must not share the same wear distribution.
- Exposed concrete: cast panel/joint logic, restrained porous roughness, modest
  weather streaks below ledges and darker sheltered contact. Underpass piers and
  soffits read as heavy structure; do not cover them in arbitrary repeated cracks.
- Masonry: use larger limestone/rendered civic courses where appropriate, smaller
  brick only where the building is actually designed as brick. Mortar follows the
  wall plane; courses turn corners coherently. Keep church/hall bases different
  from painted residential walls. Do not relabel every cream wall as heritage stone.
- Timber doors, shutters, benches and structural posts: grain follows the wood,
  joints follow assembled boards, edges carry selective wear. Shutters need slats
  and shadows; a door needs a coherent frame/threshold. No bark texture on furniture
  or the same decorative slash across every wooden part.
- Windows: separate glazing from painted frames, timber louvers and capiz-style
  panels where already authored. Maintain visible recesses and some interior depth.
  Glass gets controlled reflection/smoothness rather than wall noise or full mirror
  brightness. Keep genuinely transparent panes transparent and retain near-fade.
- Roofs: distinguish corrugated sheets, clay courses and flat sealed concrete.
  Ribs run down the slope; seams meet ridges and eaves. Metal has occasional sheet
  variation and restrained edge oxidation, not uniformly rusty brown everywhere.
  Clay has staggered courses and broad tonal variation, not a flat red fill. Flat
  roofs get coating seams/drain context without inventing hazards or leaking geometry.
- Gutters, pipes, railings, vents and utility hardware: metal paint, joints and
  fastening/support cues at close range. Keep thin wires clean enough to remain
  continuous under the existing outline/AA rules. Do not add tiny geometry that
  turns into flickering black pixels at gameplay distance.

#### Ground, streets and boundaries

- Asphalt: readable aggregate at close range, broad wear/color variation and
  believable patch direction. Preserve continuous street elevation and UV scale;
  no reinstatement of the old isolated dark road slabs or discontinuous extensions.
- Civic paving: appropriately scaled stone grain, restrained joints and a few
  larger tonal groupings. Chalk remains visibly above the paving and distinguishable
  from joints. Avoid bright mortar grids competing with the actual court.
- Pavements/kerbs: distinguish concrete slabs from road and private thresholds;
  show edges/drainage and plausible contact. Keep all existing collision, step,
  broad access and slipper pickup routes unchanged unless a real
  defect requires repair. Detail cannot introduce fake walkable doors or passages.
- Roof deck: separate recreation coating, perimeter concrete and pool ceramic.
  Retain tile joints through the water, grounded furniture and open pool access.
  Court paint may wear lightly, but boundaries and the can remain legible on Low.
- Hazards and gameplay markers: visible physical identity first. Do not make
  ordinary texture cracks look like active hero fields or trip hazards. Existing
  actual hazards retain clear form, placement and behavior.

#### Props, greenery and neighborhood life

- Shop signs and livery: preserve the supplied lettering/images. Add mounting,
  material response and context to the backing where useful; never multiply a
  dirt shader over readable text. Fascia, fabric banner, metal board and small
  hand-painted notice should still look independently made.
- Shop interiors/stalls: keep the existing business-specific contents (laundry,
  repair, print, barber, bakery, pares, clothing, hardware and pisonet). Refine
  counters, cloth, trays, wood and metal separately. Preserve customer space and
  believable support rather than adding loose road clutter.
- Vehicles: retain source livery, identity, wheels and geometry. Rubber, painted
  body, glass and metal get separate responses only where a current flat material
  warrants it. Keep the already-qualified jeepney reflection/finish; do not repaint
  the entire vehicle or duplicate its previously completed repair.
- Planters, pots, pails and household objects: glazed ceramic, terracotta, plastic
  and metal have different roughness/color variation. Place wear at contact/rims,
  not a universal procedural scratch layer. Keep table plants attached to support.
- Vegetation: inspect varied crown silhouettes, leaf masses, underside light,
  trunks and ground contact. Preserve native broadleaf species variation. Add
  branch/bark/leaf treatment at a scale that reads; no photoreal leaf wallpaper,
  uniformly black undersides, neon green masses or synchronized identical sway.
- Laundry, awnings and fabric: subtle weave/color falloff, hems/folds and existing
  grounded attachments. Wind should leave pegs/supports fixed. Different garments
  keep their silhouettes and colors; no reused all-over pattern.
- Animals and birds: preserve the smaller differentiated cats, aspin builds,
  private randomness and existing reactions. Review their contact/light against
  the revised ground; do not rebuild accepted animal identities to match a shader.

#### Water, skyline, light and sky

- Pool water: preserve actual depth, visible ceramic and readable swimmer/shoe
  position. Use restrained moving reflection/ripple cues and shore contact rather
  than opaque cyan. Keep gameplay swimming/entry/exit and wakes truthful.
- Lagoon water: use its own wider-scale ripples, reflection/haze and timber-pile
  contact. Houses/decks stay fixed. Boats alone may bob. Edge/recovery rules and
  the new hero's water effects must remain visually distinct from scenery water.
- Near skyline: substantial facades, occupied windows/balconies, service heads and
  rooted structures. Preserve the new layered Sa Bubong city; give its residential
  surfaces a finish without obscuring the roof's play space.
- Far skyline: reduce texture frequency and contrast with distance. Cluster masses
  around sight lines, leave intentional sky gaps and retain depth through haze.
  No repeated identical towers or billboard-like blank backs dominating a view.
- Clouds: broad authored cumulus forms with warmer lit edges and cooler body
  shadows, plus a quieter higher layer. Avoid uniform procedural static/noise,
  an evenly dotted sky, sharp horizon seams and a giant white sun patch.
- Per-map atmosphere: Eskinita has warm afternoon light and a broken cloud bank
  beyond close roofs; Bayan has clearer civic daylight and broad soft clouds;
  Ilalim has cooler overhead shelter with warmer open-street sky; Sa Bubong has
  layered dusk clouds and a longer city horizon; the lagoon has open coastal light
  and reflected cloud structure. Shared shader code does not require identical skies.
- Motion/ultimate/replay: keep any cloud/water motion slow and bounded. Weather
  still responds to accepted ultimates and restores correctly after interruption.
  If new time-varying shader values affect retained footage, connect them to the
  existing recorded environment/time contract rather than silently replaying the
  current live sky behind an old clip. Record any required format change explicitly.

### Concrete technical route and decisions to verify

The inspected project uses the Built-in Render Pipeline. EnvColourPass applies
retained tint/roof atlases, then NearFade converts opaque scenery into its Standard
surface shader. Current commercial buildings are one mesh with one `colormap`
material. Painting detail into that shared palette would stamp it on unrelated
parts and cannot establish consistent physical texture scale.

Preferred route: opt-in material detail within the existing NearFade/Standard
surface path, with untouched behavior when detail is disabled. Preserve source
albedo/atlas sampling. Distinct analytic or authored patterns handle plaster,
concrete, masonry, timber, metal roofing and glazing at world-meter scale; the
same noise function is not the finished visual design of every material.

For multi-surface atlas buildings, validate a per-face material-role mask against
actual geometry/UVs before widespread authoring. A cloned authored mesh attribute
can carry role data without splitting every building into many draw calls; preserve
original mesh/import GUIDs, vertices, indices, normals and collision. Do not infer
that every green surface is a roof or every pale surface is plaster. Review a false-
color role preview on representative house, shop, civic and tower meshes first.
Explicit named material roles cover native separately authored architectural pieces.

Use shared material variants by source/role/profile; no per-frame material creation
or uncontrolled per-object duplication. Preserve transparent/cutout source behavior,
normal/roughness data and pool shader exemptions. The earlier build-time material-
bake optimization is rejected and is not being revived under a texture task.

Extend NeighbourhoodSky's existing per-map materials with controlled clouds/sun/haze.
Support the tint/exposure properties already understood by SkyEvent/RecordedEnvironment
where suitable. Prove interruption/replay restoration after integration. No new
render-pipeline package, volumetric renderer or large external texture dependency is
assumed necessary. Primary references are linked in the full-backlog report.

**Prototype decisions after actual rendering:** the two fully procedural cloud
drafts were rejected as smeared/pattern-like. Four verified CC0 Poly Haven pure-sky
HDR sources now supply cloud form only, recolored through the authored map palette;
source ground/exposure/sun are not pasted into the scene. Raw files and provenance
are under ArtSource/environment/skies/polyhaven. No paid API or reset was used.
This source-based draft still needs actual game/native review before acceptance.

Material coordinates must survive Unity static batching. The current author bakes
metre-scaled part coordinates into unusedUV3 on derived meshes, preserving UV0/UV1,
positions/normals/indices and original assets. Shared source/scale variants avoid
per-instance mesh copies. Do not disable global batching or rely on object matrices
that the native batch process rewrites. The shader packs role/coordinates into one
full-precision varying. Standard still needs11interpolators, exceeding target3.0's
10, so target3.5provides15. This matches Unity6's supported D3D11/Metal/Vulkan/GLES3/
WebGL2 platforms; no global batching disable or camera-fade rewrite is needed.

Diagnostic color studies are temporary inspection views, not replacement building
art. The owner asked about their purple/blue/yellow thumbnails; explain them before
showing any further study. All saved game surface materials use_SurfaceDebug0.

### Execution batches and exact completion rules

1. Finish the actual surface inventory and save the family coverage/role decisions.
2. Make and inspect one close facade/roof/window sample per required material family
   in the real lighting; resolve atlas-role mapping before propagating it.
3. Apply Eskinita's residential finish, then Bayan's civic/stone/timber finish, then
   Ilalim's concrete/retail finish, then Sa Bubong's roof/residential finish. Each
   batch includes its existing props, ground and vegetation, not just walls.
4. Integrate the distinct sky/cloud treatments and verify their full horizon/zenith
   in each map, including reflection/weather/return behavior.
5. Inspect all inventoried active families; fix omissions and visibly repeated,
   stretched or noisy treatments. Keep quiet surfaces deliberate and documented,
   not silently skipped. Check complete normal-speed action on the revised maps.
6. Build a coherent internal Windows candidate, inspect that exact player's Low/
   Balanced/High views and measured rendering costs. Use comparable captures/counters,
   not an FPS claim inferred from Editor timings. Run only affected checks per batch.
7. Update the existing152.4/MAP_FINAL_PASS rows with exact implementation/evidence,
   retain genuine human/device limits, and continue the rest of the full TODO.

Resume after compaction by reading AGENTS, ACTIVE_REWORK_LEDGER, current TODO and
this active revision before the historical sections below. The ledger owns current
jobs/file state; this section owns the complete material/sky/detail design. Never
replace either with a vague "polish maps" bookmark or close the whole goal here.

**2026-09-12 continuation:** the owner's current request resumes this open plan.
Use AGENTS.md and the newest active-ledger pointer. The saved/reopened two-run
semantic comparison now passes on all three maps (16667 normalized rows, zero
changes). The isolated flight/carrier defects have4/4 focused regressions, with
the historical48-idle outlier still unattributed. See
[map-retrieval.md](reports/improvement-2026-09-12/map-retrieval.md) and
[map-repeatability.md](reports/improvement-2026-09-12/map-repeatability.md).
Composition, route/idle and real FPP/exact-player review remain open. The older
Eskinita side-eye captures at x=+/-10 are outside its8.1m wall; do not call them
legal first-person views. Current real FPP uses the live1.25m offset and95-degree
lens, not the old1.65m/52-degree witness.

Current composition follow-up is being authored, not yet reviewed: clear selected
non-solid plants/tyres/fences/hedges from playable lanes into purposeful perimeter
groups, preserve real obstacles/interactions and retained inactive sources, and
stage Eskinita's supplied mountain painting farther away at its original aspect.
Actual owner baseline is Logs/map-owner-baseline-v2 (144 matched static views),
with six completed1x controlled throw/retrieval sequences in
Logs/map-owner-motion-v3. Fixture head-overlap and late-input staging errors are
documented in the active ledger; neither was a runtime camera/input repair.

Owner scope change,2026-09-10: finish ALL map improvements on Eskinita, Bayan
Plaza and Ilalim ng Tulay, then hand off everything else directly in chat for the
next session. Status: PARTIAL CHECKPOINT, NOT FINISHED. This document is the active implementation
plan, not the final handoff. Preserve the retained cute blocky cast and core rules.

**Superseding wrap-up instruction:** the owner explicitly permits unfinished map
work to go into the handoff. Finish verifying the current batch, commit/push and
provide the thorough continuation handoff in chat. Do not expand this session or
mark the whole map plan complete. The rows below remain the continuation goal.

Final checkpoint verification:489/489 EditMode,8/8 editor checks,14/14 source
audits and1/1 ordinary Classic capture covering all three maps. Unity is closed;
17 existing profile files were restored. No new final Windows build was made.
See [map-checkpoint.md](reports/improvement-2026-09-10/map-checkpoint.md) for
before/after images, precise evidence and unresolved work. The slipper runtime
experiment was reverted; its source/patch survive as non-compiled investigation
text. Finish broader composition, route/edge/idle and exact-player review later.

## Baseline and observed problems

Fresh ordinary Classic play and four matched eye-height views per map are in
Logs/maps-before-final-pass. The probe passes1/1 and captures real movement.
These views avoid mistaking Nemu's temporary nightmare sky for the base map.
The earlier civic/frontage work remains present and is a starting point.

| Map | What currently reads poorly | Intended complete result |
|---|---|---|
| Eskinita | Bright repeated cone trees and dark flattened shade canopies; repeated frontage shapes; scattered furniture/planters; little distinction between neighborhood edges | More convincing broadleaf vegetation, two purposeful inhabited roadside pockets, varied roof/frontage depth, quiet asphalt and coherent warm daylight |
| Bayan Plaza | Cone-tree skyline; broad canopies with black undersides; civic facades need convincing side/back mass; many stools/benches/pallets scattered across the view | An open civic court framed by a complete church/belfry/hall silhouette, meaningful shaded seating/stall groups, restrained stone/terracotta/wood, clear retrieval lanes |
| Ilalim ng Tulay | Thick cable bundles read like tubes; repeated plain towers/shop edges; road texture and uniform greenish materials compete with depth | Convincing guideway/utility scale, selective storefront depth and sheltered light, intact jeepney/shop livery, readable shaded carriageway and plausible distant city mass |

Do not solve the maps by filling their centers, adding slogans/flags everywhere,
or repainting all imported art. Keep the existing livery and proper names. Reuse
suitable installed assets where they fit, or author clean native geometry when
the inventory is unsuitable. Trees are an explicit owner deliverable.

## Reference and composition

The earlier [world direction](reports/improvement-2026-09-10/world-direction.md)
records observed Philippine reference photographs and the main composition.
Additional primary references inform this pass:

- [National Museum: Sta. Ana church heritage](https://www.nationalmuseum.gov.ph/2021/10/28/built-heritage-tradition-of-the-sta-ana-church/): articulated church/convent forms and capis-window context.
- [Quezon City: Aurora Boulevard lighting and sidewalk work](https://quezoncity.gov.ph/installation-of-lighting-and-electrical-fixtures-sidewalk-maintenance-aurora-boulevard/): pedestrian edges and fixtures below the LRT alignment.
- [Gilmore station project, Facilities Design Group](https://fdgarch.com/lrt_gilmore.html): structural/infrastructure reference for the specific urban setting.

These are observed references for original game geometry, not assets to copy.
Eskinita keeps close homes and a shaded sari-sari frontage; Bayan keeps a civic
landmark and open center; Ilalim keeps the strong long guideway and electronics/
food/shop edges. Each gets an explicit vegetation, ground, material and lighting
decision before implementation.

## Execution and acceptance

Inventory completed: Unity Search found eight installed source tree models in
the city/forest/town kits. Their shape comparison is
Logs/map-tree-inventory/installed-trees.png (material-atlas-free silhouette study).
They are pointed/simple cone crowns or conifers, not suitable broadleaf shade
trees for these lowland urban settings. Additional asset inventory found only a
small forest plant and city planter. Preserve the packs; author native broadleaf
trees with purposeful trunks/branching and clean canopy masses. Replace both the
old cone skyline and the rejected flattened shade-canopy pass, while preserving
the open court and actual collision boundaries.

1. Inventory/render installed tree assets and inspect all four baseline views.
   Select suitable replacements; record why. Preserve sourced assets and author
   any new tree/prop meshes within TUMP's visual language.
2. Finish all three compositions through the existing repeatable map-authoring
   path. Repair architecture/ground geometry and prop placement together with
   their collision. Keep generation idempotent and preserve unrelated scene work.
3. Check all legal slipper resting/retrieval routes and map edges. Investigate
   the recorded Ilalim idle outlier, collision seams, stuck slippers, ramps/kerbs
   and reachable prop surfaces. Fix causes rather than tuning around a bad map.
4. Verify actual ordinary movement/retrieval from both sides on all maps, at
   matched eye height and normal speed. Record before/after frames and physical
   checks. The Kuro map-clearance pass is useful but does not complete these maps.
5. Run appropriate Core/EditMode/PlayMode, map/check/source validation, build
   Windows when map work is ready, and exercise that exact executable. Report any
   non-map failures separately without hiding a map defect behind them.
6. Commit/push stable map batches on ASTRAReworks, update TODO and the active
   ledger, then deliver the map results and full remaining-work handoff in chat.

All architecture, vegetation, material/light, collision/retrieval and ordinary-
play rows above remain open until supported by fresh evidence. Human taste or
playtest approval must not be claimed. UI art and further people/ability redesign
are deferred under the owner's latest scope.

## First implementation batch, not yet reviewed

Kuro's prior work and the scope transition are pushed at864cead. The current map
batch introduces MapFinalPassAuthor, called after the existing neighborhood finish
from both retained scene editing and map rebuilds. It owns only its named group,
new materials/meshes, and explicit dressing changes.

- Three original Blender trees: street-broadleaf, plaza-shade, courtyard-tree.
  Native source is MapSource/environment/urban-trees; authoring is
  tools/author_urban_trees.py (--publish after reviewing a versioned study).
  The first plaza study exposed a cut trunk stub; extended it into the crown
  before publication. Source studies are Logs/urban-trees-v2.
- Replace original tree/conifer meshes throughout the three maps, including the
  old flattened-canopy override. Preserve source assets. Reachable trunks gain
  narrow collision outside the chalk; distant scenery remains non-colliding.
- Eskinita: move non-solid furniture and stores into two street-end pockets,
  add supported shade roofs and preserve the already improved house fronts.
- Bayan: group seating/stalls beyond the playable walls, retain excess assets
  inactive, add a complete church nave/roof/buttresses and hall roof end walls,
  and give the quiet central plaza broad stone paving with restrained variation.
- Ilalim: replace the giant wire tubes with44mm conductors connected to the
  existing pole crossarms, and add mounted shelter to PC Express/pisonet fronts.
  Existing physical pillars, kiosks, carts and their gameplay contracts stay.
- All maps: neutralize the yellow/green lighting cast while keeping warm sun,
  readable hemispherical shade and existing imported livery.

Historical v1 author run (finished): session59344, Logs/map-final-author-v1.log.
Do not treat an intended change as
verified. Inspect fresh four-direction/ordinary-play captures, physical route
checks and author idempotence next; fix ugly placements or collisions before
marking any of these rows complete. The old Ilalim idle outlier still needs its
source-grounded investigation. No map completion or final build is claimed yet.

### V1 review and next corrections

First authored maps compile and all8 editor checks pass, including zero gated
geometry findings. Ordinary Classic capture passes1/1 at Logs/maps-final-v1.
The visual review is not complete: Bayan has too many broad shade crowns and
bright paving gaps. Thin its background planting, keep broad shade at selected
near positions, and make paving continuous with quiet tonal variation. Ilalim's
generic added shop canopies duplicate existing work and read as floating blocks;
remove them. Preserve the already modeled shop fronts and livery. Eskinita's
cleared play lane reads better, with activity moved to street-end pockets.

In the actual Ilalim scene, Slipper.GroundY at (2,.5,-2) returns0, but at
(2,3.6,-2) returns9.040001, the TOP of the guideway while the throw is underneath.
FixedUpdate uses that query to decide whether flight has landed. A HostThrow
regression is being run before the fix (Logs/viaduct-flight-before-v2.xml/log).
The first fixture started too soon after round initialization, so its observed
low-height flight was not accepted as conclusive; v2 waits for initialization and
logs the release/after state. Distinguish this from the historical48-idle-penalty
seed. Preserve raised-ground and unreachable-roof recovery when fixing flight.

### Wrap-up disposition of the flight investigation

The below-flight query attempt did NOT resolve the real HostThrow probe: it still
reported InFlight near the owner after0.1s. Therefore the query observation alone
does not establish the cause of that test's spatial reset. The runtime attempt
will be reverted before this checkpoint is pushed, and the failing exploratory
fixture kept outside the test gate for continuation. Next session should trace
the actual position writer and ensure all intent/AI drivers are frozen before
deciding whether the fixture, carrier release, grounding, or another path owns
the reset. Do not claim this bug or the historical idle outlier fixed.

### 2026-09-12 V9 spatial continuation

Owner views now use the real1.25m/95-degree first-person rig. The144 V9 camera
metadata rows exactly match the baseline across both modes and all3 qualities.
Loose dressing is grouped at scene edges using actual drawn bounds; imported
benches/chairs/crates leave customer space. Bayan has a shaded southern tricycle
bay with an original simple motorcycle/sidecar model, trunks behind it, open
monument access and inward-facing east/west hoops clear of civic frontage.
The author retains excess legacy props inactive. Eskinita's original mountain
paintings retain their artwork and aspect at a quieter apparent distance.

Saved/reopened two-run semantic comparison V9 passes16746 rows with zero changes.
V9 route evidence has7420 resting samples and18 real pickups; all8 editor checks
and489 graphical EditMode tests pass. See reports/improvement-2026-09-12/map-composition.md. This is
one stable spatial batch; graphics, broader architecture/ordinary-speed player
review and the approved Sa Bubong map remain part of the open improvement goal.

## Rooftop owner revision,2026-09-13

Sa Bubong needs a larger believable pool, full perimeter fencing and falls over
any outer railing. Retire the isolated paint stripe/open gap. Add restrained
resident detail and several cosmetic bird types. Keep existing controls, mash
get-up and10s lost-slipper penalty. Current exact state is EXECUTION_PLAN.md.
