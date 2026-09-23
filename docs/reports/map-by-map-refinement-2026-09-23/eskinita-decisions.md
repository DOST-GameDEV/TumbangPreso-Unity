# Eskinita decisions before implementation, 2026-09-23

Status: first outer-context group implemented and inspected; see
[the bounded report](eskinita-context/report.md). No new textures or lighting
changes. Individual near-asset work and whole-map completion remain open.

**Latest owner correction:** the region beyond that row is still an empty plane
and is visible in map-select renders. The initial extension does not complete the
background requirement. The exact crop is saved as owner-references/eskinita-empty-distance.png.
Continue the background now before moving on to near-house polish.

Follow-up implemented and inspected: the deeper district now fills that exposed
distance in the actual preview comparison. See [distance evidence](eskinita-distance/report.md).
This closes that specific empty-band finding for the inspected views; it does not
close the whole map or remove the remaining individual near-asset work.

The next layer extends connected residential blocks across the preview's exposed
distance, with medium-distance retained geometry and simplified distant rooflines.
Use grouped meshes and a small palette atlas derived from the existing roof/facade
colours to avoid hundreds of separate detailed renderers/materials. Distant plots
must include ground/use relationships and road connections, not rows floating on
the same exposed grid. Keep real roofs/window proportions near the camera and
reduce detail with distance. Existing court, first rows, models and collision stay.
Judge the full map-select/lobby view, including the top band the owner circled.

## First asset and context inventory

Verified in the current scene/source:10primary retained house lots,4vehicle lots,
12back-row houses and14corner houses. The26context prefab name overrides exist
in Eskinita.unity; NeighborhoodRework is present. The older district diagram is
historical evidence and includes arrangements that must not be assumed current.

The primary lot registry is
MapSource/environment/layouts/eskinita-neighborhood-plan-v1.json. Its old draft
status is historical; the current scene and author determine what is implemented.
Native player view was inspected alongside the owner's sparse-lobby reference.

Primary house instances to review individually, retaining their existing bodies:

- Bahay_0_W: family a, proposed street coordinate22.011.
- Bahay_1_W: family c,14.695.
- Bahay_3_W: family a,0.667.
- Bahay_4_W: family b,-8.178.
- Bahay_5_W: family o,-16.773.
- Bahay_1_E: family c,16.116.
- Bahay_2_E: family e,8.8.
- Bahay_3_E: family c,1.275.
- Bahay_5_E: family o,-12.188.
- Bahay_6_E: family e,-20.603.

These are source-plan coordinates, not a new exported bounds measurement. Individual
front/side/roof/material acceptance is still pending; listing a house is not inspecting
every face. Parking lots Sasakyan_0_E,6_W,2_W,4_E stay grounded and retain their role.

Other groups that must be accounted for: each plot's gates/walls/grilles; both
SariSari_W/E shop pockets and their seating;12utility poles and connected cables;
laundry supports; trees and container plants; road/chalk/curb/paving; cross streets;
background building backs/rooflines; supplied distant art and animated sky.

## Generated references and critique

Both images live under ArtSource/map-refinement-20260923/concepts. They are concept
references only, never game textures, approved assets or completion evidence.

### eskinita-v1.png

Useful: coherent side/back streets around a quiet court; differentiated domestic
and shop thresholds; roof/eave/window depth; objects grouped around practical uses.

Reject: the enclosed square court/fence arrangement changes the actual open street;
fine vegetation/packaging/realistic finishes exceed the native blocky style; large
numbers of background buildings are not an automatic density/performance target.
Do not copy its invented signs, decorative skyline or new house designs wholesale.

### eskinita-v2.png

This second study used the actual retained family-b comparison as its style anchor.
It better preserves the stepped roof, chunky frames, simple wall planes and scale.
The small braced entry shade and separately constructed counter annex are useful
construction ideas. The reduced product shapes are more appropriate than v1's
microdetail.

Keep the existing house mesh/GUID. Several features in this image already exist
in the fitted detail set, including shutters/downpipe, so do not rebuild them.
The green colour is not a new recolouring instruction. Fit any chosen addition to
the actual model coordinates, roof, door access and street camera before adoption.
The free-standing shop study is not permission to replace both current sari-sari
assets without inspecting them. No further concept generation is needed for these
same questions; the next useful evidence is the actual scene.

## First implementation decision to resolve

The highest-priority observed issue is the sparse neighborhood context visible
behind LOBBY. Existing foreground bodies already have substantial fitted detail.
Before adding another layer of detail to them, use the actual current overview to
locate exposed gaps and trace how the existing street blocks end.

The proposed local context group is an outward continuation of the existing street
network, with retained building families, believable side/rear relationships and
varied roof heights. It must sit outside the existing playable/collision envelope.
Do not simply ring the court with more facades or scatter houses across blank ground.
Use explicit street/lot placements and affordable geometry at that camera distance.
Object counts/footprints and which views need them must be recorded before generation.

Then proceed through individual near houses and props, changing only demonstrated
weaknesses. Surface treatments depend on actual material/construction. Preserve good
frames, roofs, shop/livery art, poles and collision. LIGHT-1 separately targets dark
shading/world edges; do not solve concealed detail by adding unbounded geometry.

## Scope and acceptance

First context draft:26outer house lots using five already-finished retained body
families. West/east facade edges atx=-37/+37, six lots per side with8.8m pitch;
north/south edges atz=+55.2/-55.2, seven lots per side with8.8m pitch. Two side
streets atx=+/-32.2connect to the existingz=+/-29.5cross streets; new cross streets
at z=+/-50complete the next neighborhood block. Footpaths leave junctions open.
Different explicit family sequences avoid a repeated four-house cycle.

The dedicated EskinitaContextAuthor adds only its owned group to the current scene,
reuses finished mesh/material references without repainting original assets, keeps
all new bodies outside the retained neighborhood, and asserts no building overlap,
floating support or change to existing gameplay collider bounds. It also integrates
after Eskinita's existing finish author for repeatable regeneration. The matched
visual check uses the real MapPreviewSurface camera, with only the new group
toggled for before/after. This draft is not considered successful until inspected.

Only Eskinita authors/scene and explicitly new map-specific derived assets may
change in its batch. Shared original GLBs, other maps, approved characters and
gameplay state stay preserved. The existing author accepts TUMP_MAP_AUTHOR=Eskinita.

Acceptance views: actual lobby overview, normal player eye height, a useful spectator
angle and one near-frontage angle. Keep one matched before/after set; inspect small
and greyscale readings. Check added solid/support bounds against existing routes and
verify scene scope in the diff. Do not turn this into another general capture-tool
project, or declare the map done after one context group.
