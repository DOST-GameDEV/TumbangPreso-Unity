# Continuous UI glyph arcs

Current native Home and Join captures at1600x900 and2560x1440 preserve readable
body text and authored poster proportions. The pale empty-state can showed
vertical opacity ribs around its elliptical rim. HubGlyph.Arc constructed every
arc sample through Line, which feathers all four edges of each short segment.
Those segment caps overlap on semitransparent icons.

The correction uses a continuous opaque strip with transparent side bands.
Only open-arc endpoints have caps. Existing icon silhouettes, stroke weights,
colors, adaptive detail limits, fonts, supplied PNGs and navigation remain.
At n segments, the arc uses4(n+1) vertices plus8 open-end vertices instead of20n.
This reduces mesh work; no measured frame-rate improvement is claimed.

The same native current-screen capture passes before and after. Original four
PNGs and candidate first-pass PNGs are retained locally. The chalk circle has an
independent .2s delay/.55s draw-in; one candidate frame caught it mid-animation.
The fixture now waits.8s before capturing that view. Final settled review passes; all four native views were inspected individually.
All three parents exit0;21126 input files, shared preferences, Quality settings
and isolated profile seeds restore byte exactly.216 incidental native metadata
deltas per run are preserved outside source. Packaged visual acceptance remains
separate from this native UI review.
