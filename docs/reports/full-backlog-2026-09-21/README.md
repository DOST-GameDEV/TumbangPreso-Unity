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

Recall-bindingv2 failed both cases during test staging: the empty-scene20frame wait
could expire before Unity's documented50ms candidate settle, and the synthetic
keyboard was not selected/enabled reliably after changing unattended-input settings.
The assertions remain. The fixture now uses a real one-second deadline, explicitly
enables only its own synthetic devices and selects its return keyboard. Input state
and binding preferences restore in finally and through the external guard.

Recall-bindingv3 passed2/2 in3.360s, guard18460a1dee9e. Actual interactive conflict
retainsF10, acceptedF9 persists as a layout path, mouse-to-keyboard binding stays in
the desktop family, the gamepad binding survives, unsupported pad fullscreen refuses
and cancel preserves the choice. The same live recall marker updates after rebind,
tracks the moved shoe, changes to a pad glyph, hides the cap for touch and restores
the rebound keyboard cap. These establish software input routing, not physical devices.

Sky draft now exists in NeighbourhoodSky/MapAtmosphereAuthor: static layered cloud
form, per-map color/coverage/layout, restrained sun and compatibility with existing
weather tint/exposure. It is not yet visually qualified or copied into development
materials. Current guarded author job: map-cloud-materials-v1, validation only.

## Surface/sky prototype evidence and continuation

Source batch79bf5c26published the qualified skyline, captured-control fixes, queued
dead-code removal and their evidence. The shaders/material author below are newer
uncommitted work and have not been qualified in a Windows build.

Sky owner-viewv1 passed1/1 in59.612s,200frames, guardc258918bf0b1, but visual review
rejected its smeared, overly soft clouds. V2adds finer billows, tighter edges and
different density/scale. Its material refresh and new visual review are pending.
Static cloud form is being established first; motion/recording is still in the plan.

MapSurfaceAuthor's representative building study compares source rendering and UV
region colors. V1used Sprites/Default, whose transparent depth behavior drew internal
floors over the facade. Those false-color images are invalid. V2uses opaque NearFade,
but shader import exposed a parser-stack overflow in the long material branch chain;
its exit0is not a shader pass. Flattened disjoint ranges resolve it. V3explicitly
checks ShaderUtil.ShaderHasError before/after drawing and completed with no errors,
guard90cbbb5adc56. The source/UV images establish walls, glazing, plinth and trim;
splitting the first palette column by importedV isolates the roof face. Raw glTFV
is flipped relative to Unity's imported mesh, so the author follows actual imports.

The new opt-in surface include has distinct formulas for16families; role0is unchanged.
Coordinates follow rotated/moving geometry at metre scale. Persistent variants share
the established NearFade albedo/gamma translation. Original textures/mesh GUIDs are
retained. Palette-role meshes copy source geometry and refuse unreviewed UVs/meaningful
vertex colors. Texture images, signs and transparent/procedural surfaces are not
blindly replaced. The author logs unassigned and retained material families.

First surface author job: map-surfaces-eskinita-v1, validation only, session59941.
No source mesh/material assignment is yet visually accepted. Do not promote a
formula, study or coverage count into a claim of finished environment detail.


## Current material, sky motion and Inday checkpoint, 2026-09-22

Source is still an uncommitted extension of79bf5c26; no new native artifact yet.
Surface author v3 assigned689/257/870/179enabled renderers in E/B/I/Sa. Besides
building parts, this now includes concrete bridge supports/soffits, separate track
sleepers/rails, utility timber/footings/hardware, civic stone, benches and hedges.
All actual source colors/geometry remain; debug study colors are never game art.
Fifteen NearFade tests passed0.564s including normal texture/strength/UV preservation.
All-map owner viewsv3 passed1/1 in70.310s,200frames. Actual inspected frames show
construction seams, native palettes and shaded clouds; this is Editor evidence.

Four verified2KCC0PolyHaven pure-sky sources are in ArtSource/environment/skies/polyhaven
with provenance and exact hashes. Unlike the earlier material reference links,
these cloud-shape sources were imported. Cloud brightness is normalized within
neutral cloud pixels to retain body shading; source sun/terrain are not pasted.
The newest source adds slow cloud rotation at distinct per-map speeds, with fixed
sun/horizon. NeighbourhoodSkyMotion publishes scaled Time.time. RecordedWorldView
uses its existing clip time in a scoped override; no replay schema/network bytes change.
First pause/seek/weather check passed2/2,6.079s. Stronger actual rendered-pixel drift,
reverse-frame equality and pause/return checks plus Inday motion passed2/2,20.286s,
guardad0d01523f69. Thus movement is tested in the shader, not just as a changing number.

The owner explicitly replaced the old Inday-guard preservation choice with plain
brown arms across all models. The author saves raw backups, edits only arm-owned
geometry, retains original simple hands and brown slot14, and verifies unchanged
nodes/skins/material identities, non-arm attributes and all33/32animation samples.
Model hashes are in inday-plain-arms.json. Both FppDetails and RosterArms are rebuilt
from that source. Obsolete guard author/resources and the fallback attachment call
are removed. V1body+FPPquick/held/moving evidence is Logs/inday-plain-arms-motion-v1;
visual inspection exposed a pinched wrist despite passing actions. V2extends the
brown forearm beneath the retained hand centre; that correction is not yet qualified.
The original source-arm dirty whitespace is still backed up in the initial intake.

The surface repeatabilityv1 attempt never executed: a copied shared dirty review
file included its unrelated SeanVisualOnlycall without the companion dirty method.
The failure is retained (guardce21ffa09b17). Validation now uses HEAD plus only the
new map-review hook; original development work remains. Repeatabilityv2is active.
New native map route will inspect both modes/all qualities, real-time cloud/owner
motion and matched process frame-time windows. Never call this all-TODO completion.


## Preview-to-match lifecycle defect found by native map validation

The build itself passed for v34/35/36. V34's review initially set only the mirrored
SelectedMap value, so setup restored its own index; the review now clicks MapNextButton.
V35 then timed out on rooftop startup. V36 inspected the target scene and passed a
rooftop-only route in both modes, yet the complete sequence failed at its eighth map:
setup remained visible, active scene was SaBubong, but HUD/ready gate were absent.
All failed native result JSON, PNGs and partial frame windows remain in their Logs.

A deterministic regression reproduced starting during an actual additive preview load.
Before the fix it failed1/1 in20.429s because MatchInstaller.PreviewOnly stayed true
when the preview's owner/coroutine was cancelled. The real installer then stood down.
This is a runtime lifecycle bug, not a texture failure or a valid reason to retry
until a native run happens to pass.

MapPreviewSurface now drains ongoing preview work before SceneFlow changes scenes,
blocks new swaps during exit, honours the latest requested destination, and owns/
releases the preview guard on normal completion, failure and destruction. The same
regression passed1/1 in6.853s. Adding the adjacent change-destination-to-main-menu
case passed2/2 in7.628s. Scene/rules fixture state is restored. The original failures
remain; environment-v37 is being built for complete native qualification of the fix.


## Full native v37 map route, passed

Environment-v37 built successfully (1180MB,50s; guard9f36f40872e7) with the real
preview-lifecycle correction. The exact player completed all4maps in both modes,
96 unique quality/view rows,8 normal-speed walking captures,12s fixed-view cloud
motion and48 nonempty paired detail-off/detail-on frame windows. Runner exit0,
shared input unchanged,2 existing named-profile files restored. Raw results and
representative frame provenance remain in map-evidence; summary/pairs are in
native-environment-v37.json. The prior native failures remain failures.

On this RX6600/Ryzen2600 host, median additional detail cost across24 paired windows
was0.6585ms; largest detail-enabled p95 was13.5744ms. These windows freeze simulation
and exclude image encoding. They measure the process, not isolated GPU time, and
are not an all-device or live-match performance guarantee.

Inspected native images: civic stone/roof/glass distinctions, underpass concrete/
timber/hardware, roof paving/city/clouds and plain Inday arms are present on Low and
High without pink/missing shaders. Thin distant rooftop window/ledge edges still
show aliasing/speckles in some High views; retain that concrete map-refinement issue
for the existing graphics/outline follow-up rather than silently close all maps.
The mixed-caster Low/comfort native visual check is running; near-fade band captures
and complete material-family/generator-hook review still remain.


Native mixed-caster Low/comfort owner and spectator checks passed on the same v37
artifact,30s each, with four live AI writers after staged full starting meters.
Sampled frames keep the can, chalk, player identity and distinct fields visible
under the changed weather/materials. Audio was captured, not personally listened to.
The existing NearFadeProbe passed1/1 in3.300s, guard949277914ccc; inspected2.50/1.10/
0.20m post views show solid, stippled and cleared geometry. The close view still
has thin outline remnants, retained for the existing outline/graphics follow-up.
Eleven movies were encoded at recorded timing. Native-environment-media.json records
exact executable/runtime-assembly and movie hashes plus their source timing CSVs.
This completes the explicit animated-sky and plain-Inday-arm requests as software;
it does not close the broader map finish or the full project backlog.


## Further graphics and material authoring, after e50f8c37

The published first batch remains e50f8c37. New graphics refinement is in progress.
Matched near-fade mask-on/off/no-outline images identify and remove the dissolved
pole's false outline only within its fade band. Native v38 completed all96 views;
only0..7 additional mask draws were submitted in the sampled native poses. Median
cross-run process-frame difference versus v37 was+0.0677ms, largest p95=14.1115ms.
The phase remains scoped; no isolated GPU-cost guarantee is inferred.

A native roof AA3 comparison confirmed Forward/LDR4xMSAA+FXAA actually reached the
frame, but the small dark normal-prepass dots remained. Source geometry and new
finishes were separately compared at near planes.05/.3/1m and diagnostic22degree
FOV; no material/depth defect was demonstrated. A draft fades distant normal crease
edges by projected size while retaining depth silhouettes. Matched Editor1/1,
5.596s(guardaf87aa18ea4a) visibly reduces the distant tower dots. Native qualification
of this additional refinement is still pending; the user's AA/HDR default stays.

The remaining-parts study produced60source/UV images for15mixed-atlas assets,
guard63c129b06c65. New roles cover car lens/rubber/paint/plate regions, six industrial
models, a city train, streetlight emitters/housings, barriers and metal service parts.
A seventeenth family gives potting earth its own quiet matte grain.

Authorv4 then exposed a persistence defect: Unity renamed saved copies of inline
materials to their hash filenames, so later classification could silently lose
surface assignments. Semantic source names now live in an explicit material tag,
independent of filenames; an untagged saved source fails rather than degrades silently.
Recovery from the exact preserved79bf5c26scenes matched hierarchy/submesh context and
shader/color/vector/scalar/texture values before writing tags.381names recovered,
zero unresolved references, guardccb7a26bfc1c. Disposable baseline copies were cleaned.
New tags and final assignments have not yet been recovered to development; authorv5
and subsequent saved-repeatability/visual checks are required before publication.


## Final environment v39 acceptance

The complete19family pass covers all2686active renderer families with explicit
finish/retention and zero unassigned categories. Surface-only repeated authoring
preserves mapped coverage and has0protected/0repeat differences over27917rows.
Complete neighborhood generators passed on E/I; the initial overall run failed on
8Bayan chalk-scale rows. Canonicalizing the2mm thickness calculation fixed that
floating feedback drift, with the strict comparison unchanged; Bayan then passed
0/3777rows. Full SaBubong generator passed0/5025rows plus geometry validation. Its
only baseline change was sibling order of existing dressing children. Original
assets, color palettes, gameplay colliders and routes are retained.

Final Editor owner views passed1/1,64.804s/200frames. Referenced final dependencies
were recovered selectively:4132files/107,387,663bytes, exact hashes in
surface-assets-final-v7.json. Native environment-v39 built1192MB in85s with guarded
profile restoration. Full native route passed96unique views,8map/mode pairs and
48timing windows;1..8near-band mask draws in the sampled poses. Median process-frame
change against v38 was+0.1572ms, maxp95=14.1064ms on RX6600/Ryzen2600. These are scoped
frozen-scene process measurements, not an all-device/live-match performance claim.

The same v39 passed30s owner and30s spectator mixed-caster Low/comfort captures.
All native runners exited0 with shared input unchanged and2existing named-profile
files restored. Audio was captured, not personally listened to. Human taste,
physical/separate-device checks and broader gameplay/new-character work stay open.
The full backlog goal remains active after this environment checkpoint.

The test partition was also repaired:22new presentation/preview fixtures had not
been registered and prevented --plan from succeeding.19arena/lifecycle fixtures
were placed in match and3continuous-camera capture fixtures in capture. All161
fixtures are now assigned exactly once. This is partition validation, not a full
suite execution; preserved before/after plan logs make that boundary explicit.


## Existing UI and role-aware kit continuation, 2026-09-22

The map batch is published4a8a51ec. Current UI completion plan is existing-ui-plan.md.
Original dirty fixture migrations were reviewed and integrated in isolation; the
legacy338-row capability baseline is preserved. New explicit122identity migration
requires every target, with two documented owner-removal exceptions, and labels
conditional ready/queue/welcome controls separately. The new walk uses active
painted owners rather than counting invisible converted controls.

Settings passes7/7 (37.134s, guard09ddc5539300). Picker baseline18/24 found three
shared-capture missing-directory failures, one stale summary assertion, one login
canvas wrongly disabled by the screenshot fixture and the old inventory walk.
The five non-inventory cases pass after correction; inventory routing errors and
its pre-migration missing-capability failure are retained. Current focused inventory,
lobby and settings capture passes3/3 (30.196s, guard29c70614dd8e). Combined screens
passes113/114,0failures,463.937s, all45fixtures present; the sole skip is UGS identity
sign-in intentionally disabled in batch. Actual Player/skills screenshots inspected.

Current grouped runner now supports named isolated profiles and fresh output folders,
and preserves its launcher stdout/stderr alongside XML. This avoids running screen
checks against the player's default profile. No current full-project gate claim.

The kit audit reproduced Sean E arming an unusable defender throw. Baselinev2 failed
that case and passed strengthened inventory (13.650s, guard91d8013a8580). The small
CanActivate role gate now preserves the finite charge, and four Sean/Zack E choice
descriptions make the attacker-only role explicit. Fix-v3 passes4/4 (48.734s,
guard67a1ffe14157), including all4role refusals, Afterburn, receipts and picker.
An unmatched Flare filter in that run is not counted; its correctly named actual
case subsequently passes in beam-and-flare-v1. The initial native diagnostic's
missing Visual namespace compile failure is separately retained; no tests ran there.

Beam-and-flare-v1 passes2/2 (16.761s, guardaf0ce7281e6d). Seven same-camera A/B samples
include the actual beam column/pool/lamp/bloom at range and within approach fade,
1280x720 and1600x720. Maximum changed fraction0.002650 (0.265percent), no newly white
pixels, versus the existing12percent bound. This is sampled Editor image coverage,
not whole-map/device/GPU certification. CSV and representative frames are in ui-evidence.

Internal existing-ui-kits-v40 builds1193MB/66s (guard60a83de04fbc). New diagnostics
follow actual title/play/lobby/settings routes and current OwnerMenuAir; original
Sean visual route and new48choice/role native comparison are included. Native UI,
variants, observer view and final peer qualification remain in progress. All earlier
failed receipts and the original dirty-source intake remain. No Desktop replacement.


### Controller parity correction after the first green inventory

Manual tracing caught a false migration: the legacy Button_ON was the generic
controller switch, not Touch selection. The old green inventory is preserved as
insufficient evidence for that capability. The corrected mapping targets
GenericControllerValue and exposes it through a synthetic joystick on the actual
InputSystem device-change path. This is software evidence, not a real-pad claim.

That path first exposed a reentrant GenericPadBridge.Sync loop. Creating its virtual
Gamepad synchronously called Sync again before the bridge was recorded. The first
baseline emitted166809551bytes of errors; only the verified owned Editor27340 was
terminated, and guardffd36e74c8f2 restored files/input. A pending-pass guard preserves
the existing mapping and completes its bookkeeping before nested notifications.
The unchanged test with just that fix reached the missing UI assertion and failed
in1.482s (guard813adbf22cf3), distinguishing both actual defects.

The conditional switch/hotplug refresh now uses the current settings session's
save/discard transaction. Corrected inventory and the new save/discard regression
passed, but the older controller-layout case exposed a long status clipped at960x540:
fix-v3 was6/7 in32.412s, guard2e89da456385. Contextual concise footer copy fixes that
without moving/replacing the supplied controller diagram or its18callouts/lines.
Fix-v4 passes2/2 in17.337s, guard3d8659b265bf. Existing ControllerSupportTests pass11/11
in0.498s, guardfb9a5fe41c5f. Named profile preservation now includes the genericpad
preference; four portable restoration/allowlist tests pass. Nativev41 qualification
is still next. Nativev40 UI passed, but its first variant route stopped at case6 on
a diagnostic assumption about silent CannotAct buffering; the partial/failure is
retained and the corrected route must complete all48cases before it is called passed.


### V41 native acceptance and media checkpoint

V41built1193MB/54s, guarde8d2d8b42207. Full native UI passed again, including actual
synthetic-controller switch clicks, discard, save, reopen and restoration;4existing
named-profile files restored and all3shared input preference families unchanged.
The actual controller-switch screenshot was inspected. Supplied diagram unchanged.
The original SeanVisualOnly route passed through actual selection/cast/ember geometry.

Both variant routes passed all48unique choice/role cases each:44casts and4silent
role refusals per view. Exact equipped identity is checked during capture. Caster
profiles restored2existing files; observer/Sean profiles were fresh. Input unchanged.
The v40partial failure remains preserved as a diagnostic failure, not a game refusal
regression. Early rush travel distinguishes4.033/4.065m normal vs2.261m Afterburn
in these staged situations; the common later walking made maximum travel saturate.
This is observed scenario behavior, not a universal tuning number or balance proof.

98movies encoded from actual recorded timing:96variant movies include real engine
audio aligned with captured callback/first-frame clocks;2title-motion movies have
no audio track. Every container's audio presence and duration were verified, with
zero mismatches (duration tolerance0.22s). Binary/movie/timing/audio hashes are in
native-ui-kit-media-v41.json. No audio listening or human approval is claimed.

A final semantic trace corrected two additional generic-name mappings without
runtime changes: Button_RESET was touch-layout reset; ResetAllButton reset bindings.
Their actual current targets already occur in the captured walk and the existing
reset/cancel tests. Do not treat a generic label as a capability without tracing its
callback. All old baseline rows and earlier imperfect mappings/evidence are retained.

Current existing UI/kit software batch is ready for scoped publication. Next is the
saved existing-network-plan.md, then selected Rafi/lagoon and the final coherent
candidate. A published checkpoint does not finish the full TODO or active goal.
