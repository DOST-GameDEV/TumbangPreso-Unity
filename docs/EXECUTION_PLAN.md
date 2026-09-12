# Active execution plan

Updated2026-09-12. This is the compact current state. The improvement goal is OPEN.
Read AGENTS.md, VISION, TODO and the newest active-ledger pointer. Compaction is
continuation. Historical snapshots are preserved in the reports linked below.

## Workspace and current checkpoint

- Only DOST-GameDEV/TumbangPreso-Unity, branch ASTRAReworks.
- Checkout: C:\Users\Matthew\Documents\Codex\2026-09-09\ok-x20\work\TumbangPreso-Unity.
- Current pushed HEAD:e7a6f90b (substantial map plan). Last implemented spatial
  batch:97e61f92. Foundation flight/instruction fixes:8a22f8b9.
- Fetch/inspect actual status; preserve newer work. No reset, main operations or
  changes to the separate Documents/GitHub checkout.
- No subagents, paid work or usage resets. Use tools/run_unity_guarded.py for every
  Unity launch; preserve profiles/IDs, one Editor per checkout, no C#/imported-asset
  edits during runs. Controller ownership remains separate.
- ALL Unity/Blender runs are complete. Last Unity75613 captured42 architecture
  views (1/1); snapshot c616a5c3ea92 restored17 profile files. No Editor is active.
- Blender75066 finished4 V3 building studies, copied to Art/models/neighborhood-buildings
  and MapSource/environment/neighborhood-buildings. They are UNVERIFIED in Unity,
  unplaced in scenes and uncommitted. V1/V2/V3 studies remain in Logs.

## Current user verdict and priority order

The owner rejected V9 as barely improved and insufficiently Filipino. Technical
passes do not override this. Required corrections: plain color-slab houses,
empty Bayan floor, no broader town beyond Bayan, implausible outdoor pisonets,
bad Ilalim signs/text, oversized white crossing bars and confusing dark road shapes.
They want nostalgic Filipino places, real stalls and natural wall notices/graffiti,
and authorize thoroughly correcting anything else found.

1. Research actual references, save the detailed plan, then implement the substantial
   three-map transformation. Research is now recorded; M01-M11 are the current steps
   in [MAP_TRANSFORMATION_PLAN.md](MAP_TRANSFORMATION_PLAN.md).
2. Movement/throw/Pektus and slipper/can feel, including all-context recovery mashing.
   Full scope in [PLAY_FEEL_REWORK_PLAN.md](PLAY_FEEL_REWORK_PLAN.md).
3. Remaining requested graphics scalability and approved Sa Bubong work.
4. Resume all remaining actionable TODO work: whole kits/alternatives, same-hero
   binding, Phaister/Kuro qualification, animation/network and exact Windows release.

The owner explicitly requires critical evaluation after EACH batch. Judge style,
place identity, spatial logic, proportions/materials, composition/world depth,
gameplay readability, motion and measured cost. Record weaknesses and revise them.
Do not present isolated renders/imports/tests as visual or feel acceptance.

## Research and next concrete implementation

[Research receipt](reports/improvement-2026-09-12/map-reference-research.md) records
Project8/Pila/Gilmore local photos, Molo, official Aurora Boulevard photos, Raon,
museum/NHCP/NCCA context and the owner's4 stall photos. Original local references:
Logs/reference-review; owner photos:Logs/map-owner-references-2026-09-12/index.json.
The supplied stock-photo watermarks remain intact; their pixels are not game art.

Complete M01: measured Ilalim road/pavement/shop/stall placement. Then M02-M07:
validate/revise native buildings, put pisonets in a real shop, rebuild signs, author
food/fruit/clothes-accessory stalls, remove old hazard-looking road decoration and
crossing ladders, strengthen structural materials and wider street depth. Follow
with Bayan paving/connected town (M08), Eskinita housing/lots (M09), whole-map
critique and verification (M10-M11). All three need substantial implementation.

Filipino environmental text such as Bawal umihi dito is explicitly authorized.
Gameplay/UI descriptions stay English. Keep proper names and the18 approved people
at7c7fcb5, flat graphic faces, simple hands and clean blocky forms.

## Dirty/preparatory work

- docs/AGENTS/TODO/plan/ledger/research updates carry all new requests; preserve them.
- MapExperienceProbe.ArchitectureAndStreetContinuity added and verified1/1,42 frames.
- tools/map_layout_review.py uses existing Pillow/WorkSans to plot measured plans.
- tools/author_neighborhood_buildings.py owns4 original V3 building studies and
  packed native sources. No map scene uses them yet; critique/in-engine check next.
- Potential test-generated dirt: bayan left/right RosterArms tangents and
  ProjectAuditorSettings. Restore only after byte/diff verification, never map work.
- Never-compiled graphics-resolution/frame-cap experiment is archived as TEXT in
  reports/improvement-2026-09-12/graphics-prototype. Its runtime/UI changes were
  restored. No graphics setting or performance gain from it is implemented/proven.

## Audit findings and evidence

- Current layouts: Logs/map-transformation-audit-v1 (6 diagrams/findings.json),
  portably copied under reports/improvement-2026-09-12/map-transformation-audit.
  AABB overlap counts58/46/65 are candidates, not automatic proof of intersection.
- Logs/map-transformation-architecture-before-v1:42 wider/corner/frontage views
  and metadata;1/1. These include1.65m witnesses and elevated views, not FPP or play.
  Review found corner overlap, detached Bayan building ring, poorly joined storefront
  activity and overlapping pisonet sign geometry. Some near-camera witness views
  show dithering and must not be confused with a full owner-camera verdict.
- Ilalim:1162 sign renderers. Three pisonets occupyx8.625..10.125 between roadx7
  and shop facadesnear11.3. PisonetInteractive is cosmetic popup/audio/light code;
  its coin/time claims are not an economy. The nearby cord is the single remaining
  trip hazard; trace/relocate it coherently instead of leaving an orphaned hazard.
- White bars:Crossing_N/S_0..5,10.6-12.6m wide,.48m deep,z10.8..15.7, generated
  by IlalimNgTulayBuilder.BuildRoadSurfaceDetail. Actual TUMP chalk is separate.
- Dark shapes:Dressing_LooseManhole/Dressing_SunkenTrench under FormerHazardProps,
  decorative remnants of removed hazards. Remove/rebuild as sensible road detail.
- V9 technical evidence retained:144 matched FPP views (1.25m/95degrees),7420 clear
  rest samples,7047 connected walk nodes,18 real pickups,16746 semantic rows with
  zero two-run drift,8 checks,489 EditMode,14 source audits.7 informational audio
  flags remain. V9 is NOT accepted as the requested visual transformation.
- Earlier flight fix: baseline0/2, after4/4, including direct carrier detach,
  actual under-guideway flight, raised slab and unreachable roof recovery. Historical
  Ilalim48-idle outlier remains unattributed. Core562/562 at that foundation.

## Remaining broader contracts

Sa Bubong is APPROVED: Metro Manila condo roofdeck, open court/fenced scenic pool/
residents' shade/utility corner; real falls only at the exposed edge; button-mash
get-up; fallen slippers unavailable for about10 seconds then safe rooftop return.
Both modes, no new controls/health system. No fourth-map code/assets yet.

All six heroes/defaults/alternatives need purposes/tradeoffs/counterplay and complete
body/FPP/geometry/VFX/SFX sequences. Same-hero build rebind returns early today;
repair without live-state reset/double modifiers. Phaister still has10.5m reach,
no constructor windup, late inscription and11m moon above8m guideway. Kuro33/33
and6 clearance positions are prior local evidence; protocol28 separate-process
staged/yaw/rejoin and wider owner/overlap/roster capture remain open. Keep both
accepted purple forms/expressions and do not revive rejected models.

Run appropriate fresh nonzero Core/EditMode/check/source and isolated PlayMode
gates; final release gate twice. Build Windows only when ready and verify THAT
executable at ordinary speed in both modes and required network paths. Current
Desktop/internal players are older. Sole-author commit messages from files; push
ASTRAReworks only. Actual handoffs belong in chat, no handoff-prompt files.

History: [earlier working plan](reports/improvement-2026-09-12/execution-plan-history-02.md)
and [earlier pointer](reports/improvement-2026-09-12/execution-pointer-history-02.md).
