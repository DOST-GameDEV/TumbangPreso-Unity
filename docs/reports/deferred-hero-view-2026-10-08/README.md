# Hero view preparation across frames

First Hero opening previously constructed the whole view in one callback.
Development traces measure substantial method compilation, canvas and preview
costs there. The method-entry experiment was rejected after its actual player
frame worsened. This change instead yields between actual view-construction
stages without caching stale account data or changing authored presentation.

Home remains visible and focused while the new view is prepared under an
inactive parent. Its preview camera stays disabled. The completed view is then
shown, Home hidden and focus placed on the current Hero primary action. Back or
another route cancels preparation and destroys its owned preview. Repeated Hero
presses do not start duplicate views. Ordinary pages retain their existing path.

Native62780 passes three current graphical controls covering completion/focus,
Home visibility, settings preservation, Back/cancel cleanup, alternate routing,
repeated press and Shop configuration. All21,423 frozen inputs/preferences were
restored. The only subsequent source difference corrects an opt-in diagnostic
label from CPU duration to coroutine wall duration including yields.

Actual player frame benefit, first canvas/preview render costs, input responsiveness
and ordinary-release acceptance are still pending. Do not infer performance
approval from these behavior controls. No new build/Desktop folder was created
and the qualified Desktop release remains unchanged.

The first actual Development candidate completed14windows but still reached
118.399ms CPU in its worst Hero frame. The final PrepareScreen callback consumed
67.036ms and303 Mono.JIT samples totaled62.188ms; canvas pre-render17.033ms and
ColourGrade11.577ms followed. This is still a smoothness failure. All21,423
player/export/build inputs and preferences restored; exact receipts are retained.

Candidate2 separates description, model binding, kit preparation and each ability
tile into additional yielded stages before revealing the view. Native24100
passes the same three behavior controls on those exact bytes with all21,423
inputs/preferences restored.

Its actual Development player58944 completes14windows and reduces the Hero
maximum to50.435ms wall/50.302ms CPU, with zero frames over100ms in that
transition. The previous staged118.399ms CPU peak is retained. The new worst
frame has74 Mono.JIT samples/18.256ms sum, canvas pre-render16.822ms and
ColourGrade9.847ms. These nested values are not additive to their parents.
All current build/player/export inputs/preferences restored. Ordinary-release
benefit and the remaining noticeable first-render spike are still unqualified.
Desktop remains unchanged; this does not close tournament smoothness.

Candidate3 prepares the actual hidden ModelPreview target and its first camera
render before revealing Hero. Inactive model binding now leaves global ambient
lighting alone; the hidden render temporarily applies its lighting and restores
it in finally. Activation applies the preview lighting and deactivation restores
the previous settings. This keeps Home lighting stable during preparation.

Native56940 passes all four graphical controls, including actual nonempty GPU
pixels, camera disabled while hidden, ambient restoration and the existing
completion/focus/cancel/alternate-route controls. All21,425 frozen inputs and
preferences restore. Exact tested source and receipts are retained under
candidate3-source and candidate3-native. Actual player latency remains pending;
Desktop stays on the qualified ordinary release. Future builds replace G only.

Actual Development candidate3 player43760 passes14 timing windows
and reduces the first-Hero maximum to36.968ms wall/36.883ms CPU.
There are no Hero frames over50ms and1 over33ms. It replaces only
existing G, still403files/3,647,235,596bytes; no new build directory.
Player preferences restore and the completed14-trace exporter restores all
21,425 frozen inputs/preferences. Exact small receipts and nested worst-frame
attribution are retained. Diagnostic overhead and cold/repeated page costs
remain explicit. Ordinary release and tournament smoothness remain open.

The worst current Hero frame is34.735ms inside PrepareScreen, including120
retained Mono.JIT samples totaling24.882ms. These rows come from the exporter
threshold of0.05ms, not an exhaustive count of compiled methods. Nested values
are not additive to the coroutine or frame. The next change should follow this
remaining construction cost; the measured improvement does not prove that all
first-canvas, selection, loading or gameplay stalls are solved.

Candidate4 moves hidden UI preparation under an active transparent CanvasGroup.
It blocks pointer input and interaction while allowing OnEnable/layout work to
run at each existing construction stage. The ModelPreview component and camera
stay disabled until reveal; its explicit hidden render still applies/restores
ambient lighting. Reveal enables the preview and restarts the authored sticker
entrances. Direct synchronous Build also completes that reveal lifecycle.
Bounded profiler markers now separate coroutine work, reveal and focus.

Native49756 passes all four graphical controls on these exact bytes, including
zero inherited visual alpha, exclusion from actual UI pointer raycasts, disabled
preview camera, final primary focus, restarted entrance and cancellation/route
cleanup. All21,425 frozen inputs/preferences restore. Player latency is pending;
the current G and Desktop are unchanged. No player build was made for this unit.

The existing candidate3 raw trace was also imported by native61388 through the
new Editor metadata exporter. Every Mono.JIT row has metadata_count=0. It proves
the recorded trace contains no method names; do not infer a compiled-method list
or a confirmed activation root cause from that trace. The public API is documented
in [Unity6.5](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/profiling/rawframedataview/getsamplemetadataasstring).
This activation change remains a measured-cost hypothesis until a later batched
replacement build is compared. New3 preview posters were visually inspected;
their courts are legible, while continuous motion/loop/overlay quality remains
open. Old-map art and presentation were not modified.
