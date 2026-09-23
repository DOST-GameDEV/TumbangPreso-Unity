# Individual map refinement plan, 2026-09-23

Status: RESEARCH AND PLANNING. The new map refinement is not implemented or
complete. Older successful passes remain the baseline; their completion does
not satisfy the owner's new visual rejection. Keep REFINE-2 rows open.

## Owner request, restated without losing scope

Improve all five maps individually. Research PEAK, other justified games,
3d-asset.com and Philippine places first. Preserve the blocky game style and
successful assets. Inspect buildings and ordinary props, construction, materials,
textures, animated skies, islands/mountains and background context. Give each map
its own Filipino identity. Fill what the lobby/introduction/player/spectator
cameras actually see with a coherent place, preserving clear gameplay routes.
Use generated reference images critically, not as automatically approved designs.
Lagoon must read as a Sama Bajau community, including varied detached stilt houses.
Natural animals, bot stalls and individual character/action animation remain
separate assigned tasks. Plan first and preserve progress/old tasks in the ledger.

## Baseline and concurrent work

First visual review used existing native High frames for all five maps in the
owned qualification checkout's Logs/p7-native-world-auto, plus the owner's lobby
image. The selected map scenes and Eskinita authoring assets have no content
changes between2d43fc0eb and7a0ae3363. These are useful current geometry references,
not a new performance or whole-gameplay qualification.

The remote lighting/peak-bright-overhaul branch at053e5274e contains LIGHT-1 world
grading, coloured edge, toon and profile changes. It is NOT merged into ASTRAReworks.
Its scope was inspected read-only. Do not independently duplicate its lighting/
outline pass or present its changes as already in the current build. Reassess
shadow/material complaints against the adopted lighting before final art approval.

## Working method for each map

1. Inspect the actual views and identify concrete weaknesses. For every visible
   object/family record source, construction role, keep/refine/replace decision,
   proposed local change and gameplay risk. A list of material categories is not
   evidence that every asset was reviewed.
2. Confirm the place/cultural story using local references. Separate observed
   facts from design interpretation. Construction and everyday use should explain
   the objects and their relationships.
3. Generate a small reference study for a named gap, critique it and retain only
   viable improvements. Reject foreign architecture, visual noise, impossible
   supports, rainbow recolours, blocked lanes and a mismatched level of realism.
4. Author one local asset/context group. Preserve imported masters, source art,
   IDs and collision unless the specific task requires a reviewed correction.
   Scope generated materials/models to this map when shared assets would change
   other maps. Do not run multi-map author defaults.
5. Compare matched cameras, including the actual lobby overview and ordinary
   player height. Inspect25percent greyscale and Low/small-view readability. One
   focused risk check, one bounded tooling repair at most, then continue.
6. Complete the map's asset register and actual pending issues before moving on.
   Full integration/performance/peer regression stays a separate final gate.

## Eskinita: an inhabited neighborhood street

Keep: retained chunky house bodies, clear road/chalk, functioning private lot
boundaries, connected utility poles, selected signs/livery and the afternoon identity.
The native view already has deeper frames and roof detail than the early owner
screenshot; do not assume those additions are absent.

Observed weaknesses: repetitive frontage/boundary rhythm; large similarly treated
wall planes; a severe bright/shadow split that conceals detail on one side; uniform
small tree crowns; background context that is weak in the owner's lobby overview.
The shadow problem is partly a rendering question, not justification for more mesh.

Place basis: the UP Diliman Sitio Olandes storefront study in comparative notes
supports different open, half-open and grilled household/shop thresholds. Obtain
visual construction references before treating those typologies as measured models.

Local order:

1. Map the10frontage house lots,4vehicle lots,26back/corner houses and2shop pockets
   from the existing author to the actual visible scene. Verify active geometry and
   sightlines; do not add a duplicate neighborhood because an old JSON says draft.
2. Refine the weakest near frontage and its attached household/shop threshold.
   Consider a recessed opening, believable awning/frame, sill/counter, wall base,
   roof edge/drainage and one purposeful activity cluster. Keep source proportions.
3. Review every remaining frontage individually. Vary construction and use where
   appropriate; keep good frames and roofs. Decide finish by surface: quiet plaster,
   selective timber courses, directional roof ribs, restrained steel/glass response.
4. Extend visible side/back street relationships where the lobby overview actually
   exposes emptiness. Connect road/footpath/lot logic, building backs and rooflines;
   do not place a decorative wall of front facades facing every camera.
5. Review trees, planters, furniture, utility detail, clotheslines and signs at their
   real screen size. Remove visual repetition through purposeful grouping, not a
   random seed applied to everything.
6. Assess the sky's coarse/fine cloud balance and distant context with the adopted
   lighting. Retain slow drift and stable horizon; no extra noise layer by default.

Source owners: EskinitaNeighborhoodAuthor, EskinitaUtilityAuthor,
MapSource/environment/layouts/eskinita-neighborhood-plan-v1.json,
tools/author_retained_house_details.py, MapSurfaceAuthor. Existing author supports
TUMP_MAP_AUTHOR=Eskinita. Detailed per-object decisions follow in its own register.

## Bayan Plaza: a civic gathering place

Keep: recognisable church/civic landmark, arch/window rhythm, open playing surface,
plaza routes and useful benches/planting. The landmark is already more developed
than the surrounding houses and should remain a focal point.

Observed weaknesses: surrounding facade repetition, dark side elevations, uniformly
bar-like hedges and some flattened canopy shapes. The large open floor is partly
required gameplay space, so filling the court with stalls is not an improvement.

Research basis: distinguish an actual chosen civic/heritage context. UNESCO Vigan
and National Museum Casa Rocha provide building-use/material principles, not a
license to combine every region's architectural motifs.

Local order: landmark details/material transitions; adjacent civic/domestic fronts;
perimeter paving and shaded seating/activity pockets; varied native-style planting;
connected streets and distant roofline; map-specific sky/motion. Review church
stone/plaster, timber shutters, shell/glass windows and metal fittings separately.
Keep activity at plausible public edges and preserve all court sightlines.

## Ilalim ng Tulay: infrastructure and daily commerce

Keep: the legible viaduct corridor, existing locally specific food/repair/internet
shop signs, supported stalls and connected utilities. Its spatial identity is
already different from Eskinita.

Observed weaknesses: the broad underside becomes nearly black, side structures
merge into shadow, and long repeating structural spans dominate smaller activity.
Separate lighting visibility from construction/detail defects before authoring.

Research next: Philippine elevated-road/rail structural photographs and local
street-edge uses. Confirm which retained infrastructure this map depicts. Use
functional drainage, bearing/joint/support and shop access references; do not make
deprivation or arbitrary litter the identity.

Local order: overhead beam/pier construction and joins; each shop frontage; paved
edges/drainage; vendor and household work pockets; background street continuity;
shade-aware vegetation and sky glimpses. Preserve collision clearance, actual
vehicle routes and retained train/ambient behavior. Avoid uniform concrete noise.

## Sa Bubong: a usable household roof above a neighborhood

Keep: open court, pool/water/recovery systems, safety boundaries and distinct height.

Observed weaknesses: sparse surrounding roof activity, isolated utility structures,
few layered neighboring roofs, and pale distant buildings that read as flat blocks.
This is a context/composition problem as well as a surface problem.

Research next: contemporary Philippine residential roof photographs and household
roof uses. Establish credible access, storage/water supports, washing/drying areas,
drainage and plant placement. Roof fixtures need attachment and service logic.

Local order: access/utility structure; parapets/railings and roof-edge construction;
two or three coherent household activity pockets; each nearby neighboring roof;
varied background building silhouettes/window grouping; sky and existing laundry
motion. Keep the active court and pool routes clear. Do not scatter containers or
put every possible rooftop use on the same building.

## Lagoon: a Sama Bajau maritime community

Keep: current water/recovery rules, playable deck, improved pile-supported houses,
boats and the requested surrounding islands/mountains. The current native view
already contains more construction than the early sparse reference.

Observed weaknesses: house supports/rails become a fine dark tangle at player
distance; distant island shapes have limited internal hierarchy; very similar
large deck treatment dominates the foreground. Overview village structure and
detached-house relationships still need explicit inspection.

Research next: community/place photographs and architectural sources for house
assemblies, boat access, working space, roof patches and timber connections.
National Museum distinguishes fixed stilt housing, dugout transport and lepa
houseboats. Do not animate a fixed house as a floating boat.

Local order: overview settlement/boat-lane plan; one near house construction study;
each other house's structure and household/work role; detached homes with plausible
access; piles and deck/rail legibility; boat/landing details; water/shore relationship;
layered, varied island/mountain silhouettes and sky. Preserve the specific community
direction instead of producing a generic holiday resort.

## Completion and remaining research

Each map remains open until its actual asset register, authored changes and bounded
visual/behavior checks are complete. A generated image, research note, new shader,
object-count increase or old green test does not close it. This plan does not claim
that all source objects have been inspected yet. Motion/animal/bot research remains
separate and must lead to individually fitted changes, including Sean's walking,
the rest of the cast, ordinary throws and both pektus directions.
