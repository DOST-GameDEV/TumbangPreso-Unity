# Owner playtest corrections

Base: d9c0314b on ASTRAReworks. The existing Desktop review executable still
represents that base until a newer build is explicitly delivered. This is a
verified source batch, not a claim that the entire map/game improvement is done.

## What changed

- Removed Street Hype: the meter,tiers,celebrations,rewards and PlayStyle network
  producers/handler. Actual scoring,impact feedback and match highlight records
  remain. The main RULES stepper is gone in both lobby layouts. The existing
  advanced Custom Game editor is reachable again from its settings drawer.
- Ilalim's asphalt previously stopped atZ +/-18.5 while the street continued to
  +/-120. The road now keeps one texture scale/height through that carriageway
  and all four cross-street arms. It does not cover the sidewalks or lower lots.
- Rooftop units use real building mesh support,including lower roof wings.
  Industrial shop chimneys and the unconvincing optional background crane were
  retired; original model assets remain. Billboard masts now meet actual foot
  vertices,correcting a mistaken assumption about the model's rotated axes.
- Each shop has its own printed/painted sign layout and appropriate cloth,metal
  or timber backing. Bawal is original brush-stroke lettering on the actual
  column face with a3mm offset,no plaque or opaque background.
- MapAtmosphereAuthor gives Eskinita warm afternoon light,Bayan warm civic
  masonry/stone,and Ilalim cooler shelter with warm daylight. An original simple
  sky shader replaces the uncontrolled bright sky. Ambient/fog/sun/grading are
  authored consistently. The warm asphalt tint follows all Ilalim continuations.
- Bayan's original paving has more visible aggregate/slab variation. Its normal
  map is now actually imported/bound,and the ground mesh has tangents. No gameplay
  collision was added to the thin asphalt skin.

## Evidence

- Core:562/562.
- Fresh EditMode:510/510,Logs/owner-review-edit-v4.xml.
- Lobby:3/3,Logs/owner-lobby-removal-v2.xml,including an actual raycast/pointer click
  opening advanced rules after the duplicate main selector was removed.
- Actual FPP:1/1,Logs/owner-palette-fpp-v1.xml;144 matched views across3maps,2modes
  and3quality profiles,plus6HUD frames. This is visual coverage,not a performance
  or whole-play qualification.
- Ilalim signs/paint/architecture:1/1,24views,
  Logs/owner-ilalim-signs-review-v2.xml and folder. Final billboard/culling review
  is in Logs/owner-ilalim-final-batch-v1 (1/1,24views,inspected).
- All8 editor checks pass,Logs/owner-map-checks-v6.log and Logs/checks.txt.
- Semantic saved authoring run1/run2:0 changed rows in every map,
  Logs/owner-maps-repeatability-v6/report.txt.4148 Eskinita,3724 Bayan,13613 Ilalim
  rows compared. This preserves the inspected current layout.
- Four Python file-isolation regressions pass for run_unity_guarded. Explicit
  -tp-profile owner-review-editor scopes backup/restore to that named profile.
  The owner's running Desktop player and main-profile saves were not overwritten.
- All14 source audits pass,recorded in Logs/owner-review-source-audits-v2.

## Failed hypotheses and corrections

The roof gate originally substituted a parent's AABB top for its actual support.
It both approved floating units at that height and rejected units correctly
seated on a lower wing. It now samples parent mesh triangles. A regression
checks a2m roof beside a6m tower,including the incorrect floating6m placement.
Float/sink/coverage tolerances are unchanged.

An optional rooftop tank with no usable footprint is omitted instead of forced
into unsupported space. An attempted crane bearing query did not verify a
trustworthy join; the model is not granted an unchecked airborne exemption.
That optional background assembly is retired. Failed author/check logs and the
noncompiled crane-bearing investigation are retained. A compiler namespace typo
was corrected before the final successful author/check run.

The first full Edit run rejected the old37m road-size expectation. The replacement
checks actual continuation coverage and texture scale/height at every cross arm.
The second found a stale Ilalim builder grade literal; that source is now aligned
with the final atmosphere author. These failures are retained with fresh XML.

## Critique and work still open

The palette and surfaces are more deliberate,but the maps are not fully finished.
Trees still repeat their crown silhouettes. Eskinita needs the redesigned laundry
and tighter residential enclosure. A new original4.4m/18m laundry source draft
exists in tools/author_resident_laundry.py and Logs/resident-laundry-v1,not in the
playable scene yet. Ilalim's interiors repeat generic stock and distant buildings
remain too plain. Bayan's oversized wooden ground strips need a clear physical
purpose or revision. Broad ordinary-speed play,route coverage and measured graphics
performance remain open. No human visual approval is inferred from the tests.

Sa Bubong has an approved concept and measured source layout; its playable scene,
edge fall,mash recovery and10s slipper penalty are still to be implemented. Next
map work must begin it. After maps,the owner prioritizes full ability mechanics,
implementation and visual revamps,with distinct signature ultimate moments. The
remaining movement/equipment/graphics/network/TODO and exact final Windows build
qualification are preserved in the live ledger.

Portable original captures and check/audit/repeatability receipts are in the
[owner-map-review](owner-map-review/) folder beside this report.

Final side-placement review found that thinner sign boards inherited the old
22cm frame centre,leaving a7cm fascia gap. Their back face now sits3mm from the
actual fascia regardless of board thickness. MapSurfaceTests checks all11
backings against their own roof-front plane;2/2 surface tests pass after authoring.
The final repeatability receipt is Logs/owner-maps-repeatability-v7/report.txt.
