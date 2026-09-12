# Active execution plan

Updated2026-09-12. The improvement goal is OPEN. Compaction is continuation.
Read AGENTS.md, VISION, TODO152/152.4 and the newest active-ledger pointer.

## Current branch and run

- ONLY ASTRAReworks in C:\Users\Matthew\Documents\Codex\2026-09-09\ok-x20\work\TumbangPreso-Unity.
- Fetched this continuation, no divergence. Latest pushed HEAD0611c6d4 fixes
  NearFade alpha. Earlier38908619 preserves research/plans;97e61f92 is V9 spatial
  work, rejected by the owner as insufficient visually.8a22f8b9 fixes isolated
  flight/carrier defects. Never reset, touch main or the Documents/GitHub checkout.
- No active Unity/Blender run. V7 semantic79898 completed19118 rows with zero drift.
  No C#/imported-asset edits until collected. All other Unity/Blender runs completed. Source audits95609 completed14/14 gating,7 informational audio flags.
  Logs/map-place-draft-v5-audits.log. Fresh Core passed562/562.
- Use tools/run_unity_guarded.py for EVERY Unity launch. Preserve profiles/IDs,
  one Editor, no subagents/paid work/resets. Controller ownership stays separate.
- No new Windows build; Desktop/internal players are older than current source.

## Owner priority and preserved direction

1. Substantial Filipino map transformation: finish Ilalim, then Bayan's paving/
   connected town, Eskinita's houses/lots, then all-map critique. Research is done;
   MAP_TRANSFORMATION_PLAN.md M01-M11 is the implementation contract.
2. Movement, throw lean/weight visible to others, distinct Pektus arm/wrist motion,
   evaluated controllable aiming shake/error, meaningful slipper AND can choices,
   all-context recovery mashing. See PLAY_FEEL_REWORK_PLAN.md.
3. Graphics scalability/settings and approved Sa Bubong roofdeck: real ledge fall,
   mash to get up, about10s fallen-shoe penalty before safe rooftop return.
4. Remaining actionable TODO: six whole kits/alternatives, same-hero build binding,
   Phaister/Kuro qualification, full animation/network and exact Windows release.

Owner rejects thin/flimsy V1-V5 house replacements. Retain OLD solid chunky bodies,
deep frames and roof silhouettes; add fitted local construction/use. Keep18 approved
people/outfits at7c7fcb5, flat faces and simple hands. Four players, rotating defender,
can/slipper retrieval and BOTH modes remain. Environmental Filipino text is allowed
(including Bawal umihi dito), gameplay/UI descriptions English, proper names retained.

Criticize every batch: place identity, style, geometry/scale/support, clear routes,
composition/world depth, readable play and measured cost. Tests/imports/isolated
renders cannot approve art. The owner's bakery/pares screenshots exposed real
pole/sign/roof intersections: check all related objects, not only that screenshot.

## Current Ilalim V7 draft, uncommitted and UNACCEPTED

MapPlaceAuthor.cs is called by MapFinalPassAuthor through NeighborhoodFinishAuthor.
Plan data:MapSource/environment/layouts/ilalim-place-plan-v1.json. It retains11
original chunky commercial bodies, fits substantial ground-floor shop rooms,
adds type-specific stock and2 indoor pisonets, and replaces old floating signs
with12 original raster sign faces authored by tools/author_street_signs.py.

Four original vendor V3 models are placed with static mesh collision: frying cart,
fruit cart, pares cart, clothes/accessory stall. Native sources:
MapSource/environment/street-stalls; imports:Art/models/street-stalls;
author:tools/author_street_stalls.py. V2 duplicated-index backfaces were invalid,
rejected; V3 uses separate reverse-face vertices. No photo pixels/brands copied.

V1 failed: roof lips hid shop names, opaque NearFade hid interiors, moved bodies
orphaned roof attachments. V2 fixed those. Owner screenshots then exposed pole
intersections. V3b moves28 retained poles toabsX9.80, adds mesh collision, fits signs
to shaft-clear spans and rebuilds78 conductors AFTER placement. Roof laundry is
fitted to its own roof and supported. V3's first author run failed on a Unity
fake-null MeshCollider check, corrected with explicit ==null; failure preserved.

V4 retires duplicate old sari-sari booth, parked/cargo tricycles, old pares cart,
hedge/dumpster/delivery boxes/hoop/street laundry in the new shop pockets; original
assets and inactive placements retained. Tree+planter move together to(-9.8,20.5)
before tree regeneration. Former-hazard trench/manhole and crossing ladders inactive;
properly oriented crossings now at street connectionsz+/-24.8.

Still criticize/correct: uniform plaque-like signs and generic identical bays,
missing subtle original graffiti, structural materials/world continuity, every
side/back/roof/stall overlap and all quality/normal-speed views. Do not equate this
with finished maps. New mesh poles/vendors need the broader scene checks below.

## Current house construction studies, NOT placed in maps

Retained old city houses a/c plus fitted new detail sets. Author:
tools/author_retained_house_details.py; native:MapSource/environment/retained-house-details;
imports:Art/models/retained-house-details. New jalousies fit actual window panes;
a low house has a service window; only the taller house gets a supported entrance
shade. V1 low-house canopy covered its transom and was rejected. V2 omits it.
Reserve private setbacks for the projecting steps/porch before any scene placement.

NeighborhoodBuildingReview.cs compares unchanged originals against detailed bodies
and approved Tikboy. Run95905 completed8 construction/paired views in
Logs/retained-house-unity-review-v1; profile6d41addf8ece restored17. Mass and original
silhouette are preserved; this is an Editor construction bench, not real FPP/play
or complete Filipino architecture acceptance. No new house detail set is in maps.

Rejected thin replacement models, their native sources and old author/review code
are preserved under Logs/rejected-thin-house-studies. Removed from imported Assets
only after scanning all serialized scenes/prefabs/assets/materials/controllers and
finding zero GUID references. Never revive as approved direction.

## Verification receipts and remaining gates

- NearFade fix0611c6d4: before0/2, after13/13 focused tests. Fresh glazing diagnostic
  v2 passed1/1,3 controls: live Standard/Transparent/queue3000/alpha.12; interiors
  visible. report:reports/improvement-2026-09-12/near-fade-alpha.md.
- V3 frontages: Logs/map-place-draft-v3-frontages,1/1,22 actual owner images;
 10 broadphase roof/sign-pole pairs had no mesh penetration. Profile9e139852b17e.
  Direct bakery/pares images clear poles. Other duplicate clutter was then found.
- V4 author67472 completed,profile84bc312818c8. Inventory:
  Logs/map-place-draft-v4-inventory. V5 checks37998 passed8/8 after the support corrections.
- V4 routes: Logs/map-place-draft-v4-routes.xml and folder,1/1;
 6994/6994 connected nodes,7363 clear shoe samples,0 unreachable,20/20 real
  motor-driven pickups, including all4 vendors and a utility pole. Other seats
  isolated. Profile11ecb57d91c7 restored17. This is geometry, not live AI balance.
- Need V4 renewed FPP/architecture after clutter removal, whole overlap/footpath
  review, semantic two-run MapRepeatabilityCheck, appropriate EditMode/source gates.
- Historical Ilalim48-idle cause remains unestablished. Flight foundation tests
  passed4/4 including raised slab/unreachable roof; do not attribute idle to them.
- Last broad gates before this draft:Core562,EditMode489,8checks,14source audits;
  V9 semantic16746 rows without drift,144 matched FPP views. Those do not qualify V4.
- Final release: appropriate Core/EditMode/check/source gates, isolated PlayMode
  gate twice, Windows build and THAT executable in both modes at ordinary speed,
  required real separate-process network cases. Zero-test XML is not a pass.

## Next concrete steps

V4 checks93470 failed on23 support findings: shallow ground slabs were actually
6-7cm above the scenery ground, and small joined fixtures were split into separate
renderer pieces. V5 extends slabs to the real ground and authors monitors/chairs
as complete material-grouped meshes. V5 author21241 completed,profile1746dd8b9048.
V5 checks37998 passed8/8; profile4d2829840d8d restored17. Collect semantic comparison.
 Capture/check V4 all storefronts/corridor and
newly cleared pockets; examine signs, roof laundry supports and all close crossings.
Run semantic two-run authoring and focused gates, update TODO/ledger, commit/push
stable map batches using sole-author message files. Continue the remaining visual
Ilalim work, Bayan/M08 and Eskinita/M09; do not stop with a handoff.

Potential test-only dirt: RosterArms tangent serialization, ProjectAuditorSettings
and newline-only materials. Verify against exact pre-run/source bytes before
restoring; never discard map edits. Skills/AGENTS preserve all broader contracts.

Research/provenance: reports/improvement-2026-09-12/map-reference-research.md,
Logs/reference-review and Logs/map-owner-references-2026-09-12/index.json.
Past execution snapshots are archived whole in execution-plan-history-02/03.md
and execution-pointer-history-02/03.md under the same report directory.

## Latest verification and material review

V5 semantic4716 passed19111 rows (2388/3135/13588),zero baseline/run1/run2
drift. Profileeb196f2256f9 restored17. V5 owner57373 passed1/1,48 matched real FPP
views across both modes/3profiles,profile3571edeaddc4. V6 author42684 is active.
V6 retains pole geometry but restores the imported source timber/metal palette;
the previous commercial palette colored shafts pink/yellow. Original source pole
review is Logs/utility-pole-original.png. It varies sign layouts and adds one
original Tara Laro wall painting. Renew these actual views and final batch gates.

M08 prep, not imported/applied: tools/author_civic_paving.py generated original
6m-repeat concrete with1.5m slabs/12mm joints under Logs/civic-paving-v1. Proposed
connected town roads/landmark clearances are in
MapSource/environment/layouts/bayan-town-plan-v1.json. Continue after stable Ilalim
batch; broader streets/corners and existing tree/kiosk conflicts need authored
placement and actual views. This is not a completed Bayan change.

V6 author42684 and frontage41884 completed:22 owner views plus clearance1/1,
profileb293100184c8 restored17. The clean source pole palette read too orange
under actual sun, so V7 applies a muted neutral material tint to source textures
without changing imported geometry or texture pixels. V7 author27586 active.
No other runs active. Review the new material before accepting the batch.

Latest:V7 author27586 completed,17 profiles restoredfd3224687022. Full graphical
EditMode53852 is now ACTIVE; no C#/asset edits. V7 final tinted-pole view still
needed. Draft report:reports/improvement-2026-09-12/ilalim-street-draft.md.
Before stable commit, restore only proven generated dirt: Eskinita/Bayan current
semantic rows exactly match pushed V9, so their serialized ID churn is removable.
Material changes at m_LockedProperties are whitespace only; verify others.
Arm tangent-only data needs byte/channel verification before restoration.

Full graphical EditMode53852 completed491/491,zero skipped/failures; profile
c1b827143f2b restored17. Final V7 frontage/material review is active. No C#/asset
edits during it. Afterwards, inspect results, run V7 semantic comparison for its
new material/paint, then remove only verified generated serialization/tangent dirt
and commit/push the stable intermediate map/source batch. Larger art work remains.

Latest completed state:V7 final frontage12263 passed1/1,22 owner images and
clearance; V7 semantic79898 passed19118 rows,zero drift,profilec65efd3d553b.
All runs complete. Generated scene/arm/material dirt restored only after explicit
semantic or byte/channel/whitespace verification. Save stable batch now; then
implement M08 Bayan paving/connected town from its saved plan. Broader work remains.
