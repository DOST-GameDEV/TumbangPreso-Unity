# Consume active touch axes in the spectator camera

The touch producer writes TouchInput while the spectator only read input actions and mouse/pad look. Original Unity24412 reproduces four failures: thumb movement and drag do not release autopilot, drag does not rotate, and thumb movement does not advance free flight. Inactive stale data is correctly ignored.

Candidate5744 passes all five. Active touch movement takes the same priority used by gameplay, drag is consumed once and cleared, and deliberate touch input releases automatic directing. Existing mouse/pad paths and the Update overlay gate remain. No gameplay, Director or hero changes.

This is controlled native producer/consumer evidence, not physical touchscreen or complete mobile spectator acceptance. The current touch layer's spectator visibility and discoverable view commands remain separate unfinished work. This fix does not claim to expose controls that the UI currently hides.

Both terminal runs restore21164 frozen inputs and216 native importer/settings deltas each, input/editor preferences, quality settings and the isolated seed. The fixture restores original touch axes/active state and cursor state. Source e336 with scoped Camera/test changes; private Auditor/voxel work remains.
