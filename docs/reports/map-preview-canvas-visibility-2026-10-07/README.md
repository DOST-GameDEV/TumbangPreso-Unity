# Pause recorded map decoding behind a hidden Canvas

TumpHub hides its Canvas for older overlays without disabling child components.
MapPreviewVideo originally follows its own requested visibility and active
component state, so a hidden Canvas keeps playing its map movie. Sourcebe8281c54
with the new native controls reproduces the resource-lifetime defect.

Original60552 fails both cases: an actual playing movie remains playing after
its Canvas is disabled; the initial hidden-view case first hits a fixture
assumption because Graphic.canvas skips disabled canvases. Correcting that
lookup to the actual owning parent changes only the test. Baseline59580 then
fails both intended predicates: hidden Canvas allocates a decoder and an overlay
hide leaves the movie playing. Both runs exit2 and restore all21317 inputs,13
Editor preferences and four profile originals, preserving295 generated deltas.

The focused candidate caches the real parent Canvas, includes its enabled state
in the existing requested visibility, defers native preparation while hidden
and pauses/resumes the same decoder when the Canvas changes. Its preparation,
first-frame callbacks and timeout follow that effective visibility. Resuming a
hidden preparation gets its own first-frame observation window. No authored
media, map, art, hero, startup, networking or bot code is changed.

Candidate59936 exits0 with six actual native passes and zero failures. All21317
frozen inputs,13 Editor preferences and four original profile files restore;295
generated deltas are preserved. Its directly affected controls include
the two Canvas cases and existing reduced-motion/inactive-view lifetime cases.
The isolated worker reuses verified unchanged inputs from the prior motion
candidate; both changed source paths since512 exactly match the currentbe828
Git blobs. Full qualified source receipts retain that identity and test overlay.
Newer PC AI source is not claimed by this older-source focused check.


This qualifies actual decoder deferral and pause/resume under a disabled owning
Canvas, including the exact hide mechanism used by TumpHub. It does not measure
a numerical CPU/GPU saving or approve every overlay visually. No additional
decoder is created on resume. The older shared cb5 package does not contain this
fix; native component evidence must not be described as its packaged acceptance.
