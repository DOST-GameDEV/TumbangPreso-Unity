# First-use CPU attribution and gated UI code preparation

An actual hidden Development player completed 14 menu windows and wrote
376,116,812 bytes of binary traces under the 512 MiB limit. The internal player
replaced the existing G folder. Desktop remains the qualified ordinary release.
All 21,417 build/export inputs and preferences were restored. Actual import
produced frame/thread/nested-sample CSVs for all 14 traces; the earlier exporter
compile check alone was not treated as functional export proof.

The first Hero frame is 123.737 ms with profiler overhead. Its callback takes
77.950 ms and 343 Mono.JIT samples total 68.614 ms across the main-thread frame.
Canvas pre-render is another 17.614 ms and first ColourGrade render is 10.012 ms.
These nested samples must not be added to their parents. Repeat-Hero worst frame
is predominantly a presentation wait. Ordinary-release frame times remain the
performance comparison baseline, not these Development timings.

The largest title/preparation frame is 909.092 ms, including 865.432 ms in
Gfx.WaitForPresentOnGfxThread. That does not prove resource decoding or integration
caused the stall. Render-thread/wall-clock/focus behavior still needs attribution;
the hidden capture is not focused-device performance approval.

The gated candidate prepares a finite set of Hero/UI method entry points during
loading in small coroutine batches. No method behavior is invoked and no view
or model graph is constructed. Mono's PrepareMethod is a no-op; this experiment
uses GetFunctionPointer instead. Actual player benefit is still unmeasured.
The flag `-tp-ui-code-warmup` is required; normal behavior is unchanged.

Native control 63968 passes: no GameObject/previews created, settings and selected
map/mode/rules unchanged and repeated preparation is idempotent. All 21,421
inputs/preferences were restored. The initial malformed GUID and obsolete
GetInstanceID fixture compiler failures remain retained. They are not game
performance evidence. Exact tested source and hashes are in this report.

Next build by replacing existing G and compare fresh first-Hero CPU samples
with the candidate enabled. Enable normal preparation only after actual benefit
and startup/input/art/data behavior are qualified. Performance, all-map AI,
default full matches, replay/camera/slipper edge cases and tournament readiness
remain open. No new build or Desktop directory was created.
