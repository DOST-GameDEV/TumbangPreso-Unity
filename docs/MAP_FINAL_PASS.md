# Final map improvement pass

Owner scope change,2026-09-10: finish ALL map improvements on Eskinita, Bayan
Plaza and Ilalim ng Tulay, then hand off everything else directly in chat for the
next session. Status: IN PROGRESS. This document is the active implementation
plan, not the final handoff. Preserve the retained cute blocky cast and core rules.

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
