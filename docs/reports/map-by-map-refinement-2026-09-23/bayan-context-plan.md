# Bayan town context, measured plan before implementation, 2026-09-24

The garden overview shows a real unresolved defect: the town stops shortly beyond
the first houses, and the edge of its ground plane is visible against empty sky.
Do not copy Eskinita's96dense residential blocks. Bayan is a planted civic square
within a lower, more open town, informed by the Cabatuan image already inspected.
The generated study's connected roof/green layers are useful; its invented extra
towers, generic bus/tuk-tuk and universal orange roofs are rejected.

Measured current scene:40retained houses under Dressing/Bahay span x-46.75..46.72,
z-31.24..40.54. CivicTown reaches approximatelyx+/-52.6. TownGround is110m square
atx/z+/-55, road top0m and walks/yards.102m; older floor is200m square, top-.165m.
The road plan axis names mean constant-coordinate axes: north/south roads at
x-18.5/+18.5, east/west roads atz-20.5/+29.8. All currently end at+/-55.
Continue those actual four roads at their original height and width6m, with
connected walking edges; do not use the axis label as the running direction.

Use a separate map-local outer-context group beyond the retained55m boundary.
Proposed structure: extensions of the four streets into low residential/service
clusters and several planted shared yards, plus lower distant houses/groves that
fade into the existing atmosphere. Keep routes meaningful, houses facing streets,
back/side roofs visible from overview, and purposeful gaps filled by gardens rather
than empty ground. Near context should reuse the retained chunky building families;
far houses can use simple matching silhouettes/window groupings with lower cost.

Before generating, save explicit new block/road/tree placements and check them
against the measured110m retained town. Preserve all originals, church/hall/monument,
terminal, materials, collision and current garden unit. New scenery adds no gameplay
colliders or distant shadow casters. Use grouped meshes/shared palette material;
record actual vertices/draw groups. Extend only visual ground so its hard edge is
outside the selected-map/introduction camera, without enlarging gameplay collision.
No church rebuild is justified by the current overview: existing arch/roof/stone
construction already reads. Later near material/window review remains separate.

Acceptance: actual map preview plus overview and player-height north/south context,
matched before/after with the context toggled; small/grey25 inspected. Keep only a
bounded focused geometry/support check. Native performance belongs to final gate.
This file is a plan; no Bayan context has been authored yet.


Explicit proposal saved in bayan-town-context-20260924.json:12near/20far blocks,
four potential house plots each, selected garden slots replacing buildings. Near
side blocks atx+/-76 leave room for links at+/-58.2 without entering existing houses.
North/south blocks keep gaps for the retained x+/-18.5roads. Connect these to existing
z-20.5/+29.8cross-streets and outer lanes.128potential plots before garden omissions,
far fewer than Eskinita's dense district. Check every generated body against roads
and retained envelope before acceptance. Large visual-only ground avoids the hard
horizon edge without extra subdivisions or a gameplay-boundary change.

The placement guard caught thez-84outer side plots entering thez-78cross-street.
Both side blocks/local streets moved toz-100; the guard remains strict.
