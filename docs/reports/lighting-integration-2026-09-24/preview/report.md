# Preview lighting integration, 2026-09-24

Map selection and lobby now install the adopted bright lighting from the selected
map's own profile and directional sun. They reuse the published world look, grade,
outline and ground brightening. No separate palette or shader was introduced.
Only explicitly tagged world cameras receive the preview grade. Repeated setup
refreshes reuse the same ramp, sky and owner; switching maps releases the previous
sun/ground state before parking its scene. Returning to a cached map reinstalls it.
All three authored ambient colors are cached by map alongside existing settings.

The actual Eskinita preview is visibly improved: court shadows are readable,
windows separate from walls and distant streets recede into the warm atmosphere.
The same preview route's before/after, small view and 25 percent grey comparison
were inspected. These are across-run views with the preview's small natural sway,
not a claim of pixel-identical camera matrices or a native FPS result.

Initial preview plus transition set4/4passed11.934s. Reading its output exposed an
actual cached-map bug: an empty per-material property block still marked ground
as owned, preventing its brightening on the return visit. Cleanup now passes null
to remove ownership. The focused revisit test checks the actual ground color on
both visits; preview/revisit2/2passed3.475s, with2lifted ground slots on both visits.
First-view overview has1ground slot at that point in startup; both counts are real
receipts, not a fixed expected mesh count. Test settings are explicitly restored.
No further unchanged runs are needed. Shader source, native performance and the
remaining lighting taste/performance rows are otherwise unchanged/open.
