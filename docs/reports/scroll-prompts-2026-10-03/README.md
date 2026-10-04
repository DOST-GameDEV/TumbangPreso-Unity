# Owner scroll-wheel prompts

Up/down now resolve to the cleaned owner-supplied mouse-and-arrow pair instead
of the old 16px pixel-art fallback. Smooth outlined shapes, identical sprite
height and centered group bounds align them with existing mouse prompts. The
existing lookup still follows actual rebinding/device labels. Middle/left/right
mouse, keyboard/controller prompts and gameplay bindings are unchanged.

Built-in image editing used the owner's paired PNG and the current Xelu middle
mouse as references. The runtime resource is transparent vector-style raster,
not an SVG. No new package. Existing bilinear import, caching and fallback
behavior retained. One sheet, two normalized square rectangles.

## Validation

10/10 native EditMode glyph tests pass, including both directions, equal rects,
center pivots, caching and all shipped binding coverage. Native graphic UI-row
case1/1 passes. Inspected prompts.png: rows at32,48,64px on light and dark
grounds, compared with existing left/middle/right mouse. This is the actual
Unity Image/Sprite path, not a generated gameplay mockup; it is a UI stage,
not full-court or physical-device acceptance.

First compile stopped at container-headroom guard. Exact stale compiler
retirement recovered1,619,509,248 RSS bytes; one bounded retry succeeded. Final
graphics tree RSS4,367,958,016 and container7,496,028,160; no guard, exit0.
Original profile/settings restored. Source and image hashes unchanged during
qualification; Unity normalized trailing whitespace in the new PNG meta only,
verified semantically identical and retained. Both failed and final receipts
are preserved. No protocol change.
