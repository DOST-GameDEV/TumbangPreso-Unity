# Scenery rendering cost and build preparation

Final decision: retain the original renderer. Bayan court paint is fixed; the
optimization candidates below were critically evaluated and rejected. Do not
re-enable them after compaction. Reusable measurement tooling/evidence is retained.

The Windows baseline at source6bd3099d plus the WorldGraphicsProbe working copy
renders the actual HDR1920x1080 world with four parked players and ambient life.
Native v2 has120samples per preset/map. Draw Calls Count is unavailable in this
release runtime; SetPass and Triangles are valid. The original runner rejected
that unsupported counter; corrected-counter-evaluation.json accepts the existing
trace using the two valid render counters. The v1 batchmode run did not render
and its sub-millisecond timings are invalid, never a performance claim.

| Map | Balanced mean ms | p95 ms | SetPass | Triangles | Batched / total scenery renderers |
|---|---:|---:|---:|---:|---:|
| Eskinita | 3.858 | 4.771 | 1285.2 | 820165 | 548 / 644 |
| BayanPlaza | 3.580 | 4.366 | 742.1 | 673402 | 167 / 222 |
| IlalimNgTulay | 6.213 | 7.607 | 3435.7 | 1461867 | 396 / 1416 |
| SaBubong | 3.722 | 4.354 | 488.4 | 653557 | 129 / 171 |

This is a fixed-view baseline on the RTX4050 laptop, not low-spec hardware or
worst-case combat FPS. Ilalim contains1020unbatched scenery renderers and380unique
materials; its SetPass cost is several times that of the other maps.

Implementation underway: finish EnvColourPass/NearFade material preparation on
build-scene copies before static batching; skip that material replacement at
player Start; assign BatchingStatic to eligible fixed mesh children. Exclude
animated/physics/script-driven descendants, Sampay wind groups, transparent
surfaces and per-renderer property overrides. Authored scenes, mesh positions,
collisions, native palette/lighting and cloth simulation stay authoritative.

[Unity static batching API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/StaticBatchingUtility.Combine.html)
documents individual-object culling and the immutable child-transform contract.
The implementation uses build preparation, avoiding runtime mesh-combination cost.

Pending: focused preparation tests, native optimized build and same-view counters,
actual world captures and cloth behavior. Reject changes if color/visibility or
motion regress. Do not rerun unrelated full suites.


## First optimized player result

Two focused SceneryBatchPreparationTests passed2/2. Build succeeded; all12native
rows had real SetPass/Triangles samples. Fixed renderers batched619/644Eskinita,
221/222Bayan,1369/1416Ilalim,157/171Sa. No mesh/texture/collision detail was removed.

| Map | Balanced before ms | After ms | Before p95 | After p95 | Before SetPass | After SetPass |
|---|---:|---:|---:|---:|---:|---:|
| Eskinita | 3.858 | 3.711 | 4.771 | 4.687 | 1285.2 | 1280.2 |
| BayanPlaza | 3.580 | 3.203 | 4.366 | 4.669 | 742.1 | 741.4 |
| IlalimNgTulay | 6.213 | 5.063 | 7.607 | 6.144 | 3435.7 | 3397.4 |
| SaBubong | 3.722 | 2.910 | 4.354 | 4.517 | 488.4 | 472.1 |

Ilalim Balanced mean fell6.213to5.063ms (~18.5% in these samples). Material
switches barely changed; the measured improvement is batching/CPU preparation,
not a claim that the shader-pass cost has disappeared. Single short samples on
one laptop do not guarantee the same percentage on other hardware.

The optimized-v1 PNG readback incorrectly wrote linear HDR values to sRGB PNG.
These pictures are too dark and are NOT accepted visual evidence. The game
materials are not to be repainted to compensate. Capture now follows the existing
GameplayShots resolve through an sRGB target. Corrected native captures pending.

## Bayan court visibility correction underway

The retained chalk mesh top is .020m above its root at .082m: exactly .102m,
the new paving height. Its missing segments in FPP were coplanar depth fighting.
Seat the same eight court/boundary marks at .103-.105m with2mm drawn thickness,
preserving their x/z footprint and gameplay bounds. Add the seating step to the
civic ground author so rebuilding does not reintroduce the fault. The targeted
repair checks eight marks and repeat stability, then saves only Bayan and builds.
No unrelated map re-authoring or full test suites.


Native final-v1 produced correct sRGB pictures and confirms the eight court marks
are now continuous. It also shows faint horizontal ghost lines across Bayan and
Eskinita, absent from prior Editor captures. This is UNRESOLVED, so the batching
change is not visually accepted. Extra diagnostic matched frames with WorldOutline
disabled will distinguish outline masking from combined geometry. Do not hide
this concern or claim the performance patch is complete because2tests passed.


Outline isolation result: the ghost lines disappear in both additional captures
with WorldOutline disabled. The underlying scenery positions are intact. The
outline exclusion pass redraws thin surfaces individually using DrawRenderer;
these must not share combined scenery draw ranges. WorldOutline now exposes the
same individual-mask predicate used by its live scan, and build preparation
excludes that set from static batching. The strengthened focused tests passed2/2.
Native build and matched visual/cost review for this correction are pending;
this does not disable the game's outline style. The original broad-batching
candidate remains unaccepted until that check resolves the artefacts.


Rejected hypothesis: excluding thin/toon mask geometry from static batches did NOT
remove the ghost lines in native-graphics-qualified-v1. The extra exclusions and
WorldOutline predicate change have been reverted. Do not revive this as a confirmed
fix after compaction. Underlying geometry remains good with outlines off, but the
cause within the outline path is unresolved. One combined diagnostic build will
capture normal, no outline, no exclusion mask, no MSAA, and actual game-window
output to distinguish a capture/pipeline issue from the batching change.


Combined isolation-v1: ghost lines remain with the exclusion mask disabled and
with MSAA removed; they also appear in the actual game-window screenshot. Only
switching the entire WorldOutline off removes them. This rules out the prior
individual-mask-batching hypothesis, MSAA, and PNG/RenderTexture-only artefacts.
Next compare the ORIGINAL player scenery preparation path using the same current
diagnostic/camera, via SceneryBatchPreparation.BuildEvidenceBaseline. It writes a
separate output and does not alter saved scenes. Original baseline v2 had counters
but no pictures, so no claim that batching introduced these lines is supported.


Original-path comparison DOES NOT have the ghost outlines. Optimization introduced
this regression. Fresh original-path Balanced Ilalim4.970ms, versus prior optimized
~5.06ms, also shows the earlier18.5%comparison was not a repeatable net gain. The
build-time material bake has been removed entirely; EnvColourPass.Start is restored.
One remaining independent variable, eligible child batching flags only, is under
focused/native review. Do not keep the rejected material bake or claim18.5%as an
established performance improvement. Bayan court paint fix290f829e is independent
and remains correct in the original-path player.


## Final decision: keep original rendering path

Flags-only native result preserves the map's appearance, confirming the rejected
material bake introduced the outline issue. However it offers no consistent
frame-time gain: latest Ilalim Balanced5.506ms versus original-path4.970ms; Low
and High vary the other way, and SetPass only falls about3%. This does not justify
a new build-processing policy or extra memory. BOTH optimization changes are
excluded from production. Their final flags-only source is preserved as inert
text in batching-experiment, not a live build hook. Do not revive it after
compaction. EnvColourPass and WorldOutline remain exactly original.

Delivered independently: continuous Bayan court paint (commit290f829e), frame
pacing controls previously validated8/8 and integrated into native settings,
exact input-preference guard, and reusable native graphics measurement/captures.
Original-path map images are the accepted renderer baseline, not the misleading
material-baked images. No Desktop changes, no overall game-completion claim.
Only2focused preparation tests per meaningful candidate revision; no full suites.

Parent next non-UI work: fix the documented same-hero changed-loadout synchronization
bug through its actual legal selection/round boundary; preserve live state and
avoid repeated modifiers. UI agent retains NetSession join cancellation ownership.
