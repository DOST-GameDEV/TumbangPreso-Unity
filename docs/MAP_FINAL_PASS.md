# Final map improvement pass

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
