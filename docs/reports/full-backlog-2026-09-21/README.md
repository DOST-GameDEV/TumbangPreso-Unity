# Full unfinished-backlog continuation

The owner's latest correction requires every genuinely unfinished TODO item,
including previously deferred maps, Inday, Rafi/seventh hero, lagoon village,
secondary UI and remaining qualification. The presentation-only stopping rule is
superseded. The source checkpoint10fa4cad and nativev33 remain preserved evidence,
not completion of the whole game.

Ordering and status remain in docs/TODO.md and ACTIVE_REWORK_LEDGER.md. The
[disposition inventory](todo-disposition.json) indexes all366 numbered headings
at intake, with their original labels preserved. They are not366 new tasks and
none was auto-closed. Reconcile actual remaining requirements with source and later
evidence as each area is worked; old OPEN headings often precede documented fixes.
Keep completed replacements, failed attempts and genuine external review distinct.

## First continuation: existing maps

Current source already contains all four playable maps, swimming/roof recovery,
the smaller distinct cats/dogs and their rare interruptible surface pause, birds,
rooftop resident props, connected varied Eskinita laundry and individual map grades.
Relevant retained evidence is in improvement-2026-09-13/ambient-life-and-residents,
owner-map-review, improvement-2026-09-14/graphics-batching and the later presentation
report. MapFinalPassAuthor, AmbientLifeAuthor and current runtime were inspected.

The batching/material-bake experiments were explicitly rejected after native
outline artifacts and no repeatable gain. They remain inert evidence, not missing
production work. The specific historical48-idle match lacks its original seed/
trace; C1's demonstrated current decision/stuck causes and C2/C3 work are retained.
No claim that the historical match was causally reconstructed.

Baseline job: validation workspace, unchanged runtime source db976126; existing
MapExperienceProbe.LegalFirstPersonViewsAtMatchedQualityProfiles. Guarded Editor,
named profile presentation-validation-20260921. Result/log:
Logs/full-backlog-maps-baseline-v1.xml/.log; images and camera metadata under
Logs/full-backlog-maps-baseline-v1. Purpose: inspect actual present four-map
composition at legal owner eyes across modes/quality before deciding which old
visual gaps still exist. This is a baseline capture, not a new blanket pass.

Initial documentation diff check exposed only pre-existing serialized trailing
whitespace in unrelated arm/meta files. Preserve those files; check owned paths
separately. No Unity code/asset change has yet been made by this continuation.

## Rooftop skyline refinement

The baseline passed1/1 in57.862s with200frames. Legal roof views exposed sparse
isolated towers and plain gables; newer laundry, varied trees/animals, civic paving,
individual shop interiors, swimming and resident details are present and retained.
The revised SaBubongBuilder changes only Metro rooftops: occupied facades face the
court,15original stepped residential towers form distant clusters, and supported
ground/streets continue beneath them. No gameplay collider or playable deck change.

Authorv1 passed with0protected-scene changes and0repeat differences across4977rows,
then the all-map geometry gate passed. Guard80c31a9b1dfa. Legal owner-view review
produced50SaBubong frames across both modes and Low/Balanced/High, guard31688626a32c;
sampled court/edge views show layered depth and visible can/players. This is Editor
evidence; the new skyline has not yet been built into a Windows candidate.

MapRouteProbe passed1/1 in533.399s across all four maps/both modes:27232clear shoe
samples,0unreachable samples and62/62actual pickups. Guard7c694ff22f34. Raw XML,
grid/routes CSV and selected matched skyline frames are saved in map-evidence.
Skyline files were selectively recovered into development, with exact hashes in
skyline-assets.json. This closes the sampled route qualification, not all map art.

## First numbered-entry reconciliation

151.19 is satisfied by serialized runtime clips, the editor-only procedural
fallback and actual native ordinary/ultimate routes already recorded in the
presentation report.143.10's corrected VISION rule is present.143.11's18default
source footprint entries were regenerated at41a62d947077;143.12's existing audit
reports0drift across18constructors/16files. These specific leaves are closed with
evidence; their broad parents and the rest of the backlog remain open.

## Latest owner material and sky correction

The owner says the environment is blank/poorly textured and the sky is empty,
then explicitly asks for thorough detail on every environment object and a durable
plan before authoring. The complete plan is the active revision in MAP_FINAL_PASS.
No surface/sky implementation is claimed yet; the skyline batch alone is insufficient.

Read-only inventoryv2 completed on all four registered maps. Enabled renderers:
Eskinita726, Bayan279, Ilalim1482, SaBubong199. A total1964of2686has no referenced
texture; this includes legitimate flat/procedural materials, so the count is coverage
data rather than a quality score. Surface-inventory-summary.json preserves per-map
groups and exact raw inventory paths/hashes. Source commercial GLBs have one mesh
and one colormap material, so distinct parts need a validated semantic mask before
adding material-specific detail. Existing NearFade conversion is part of that route.

Inventoryv1 stopped at compilation: the new recall regression used Unity's removed
GetInstanceID API. Its direct object-identity assertion replaces that call without
weakening the requirement. Log full-backlog-surface-inventory-v1 remains failed;
v2 succeeded with guardb41fd4a21c95. The actual recall test is separately pending.

Primary research used for this design:

- [Unity6 surface shader creation](https://docs.unity3d.com/6000.0/Manual/SL-SurfaceShader-create.html):
  extend the active Built-in surface path rather than introduce another renderer.
- [Unity6 mesh normals](https://docs.unity3d.com/6000.0/Manual/built-in-shader-examples-mesh-normals.html):
  retain correct coordinate spaces when mapping detail to architectural surfaces.
- [National Museum, Cape Bojeador](https://www.nationalmuseum.gov.ph/2023/03/09/cape-bojeador-lighthouse/):
  specific brick/lime/wood/metal, clay-tile and capiz references inform material
  separation; this is a material reference, not a claim that every map is that site.
- [Poly Haven corrugated iron](https://polyhaven.com/a/corrugated_iron) and
  [plaster/brick](https://polyhaven.com/a/plaster_brick_01): material-structure
  references under statedCC0 terms. No downloads, paid APIs or source texture
  imports were made; photoreal detail is not adopted wholesale into TUMP.

The removed any-shoe selector and retired floor-vortex factory have no remaining
code callers. Live pickup and Kuro teardown plus the retained Inday throw review
passed3/3 in25.311s, guard743765d2fb9b. Inday has40quick/89held/83moving body frames
and corresponding owner frames; sampled poses show retained source arms through
carry/hold/release. This does not close remaining complete/native framing review.
The positional-audio audit initially found the existing unclassified lunge rush;
after tracing its live action/quiet restoration route,21calls classify with0findings.

Recall-bindingv1 failed1/1 at a valid captured F10 rebind. TryRebind passed the
device-instance path to a helper that only accepts layout paths, so it refused
the captured key. RebindSession also discarded a previous override before checking
the replacement. The current small fix resolves stable device-family paths,
preserves the other input family and restores the prior override on conflict.
Two focused real-input-operation/marker tests are running as recall-bindingv2;
no pass or native control claim is inferred yet. GenericPadBridge/MenuNav unchanged.
