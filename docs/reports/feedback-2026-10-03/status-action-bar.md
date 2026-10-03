# Timed status indicators without duplicate action bars

The latest human note removes the Frozen/Stunned action bar because existing
status indicators already communicate those effects. TumpMatchReadout now hides
that contextual surface for elemental stun, and the legacy Hud stun card stays
hidden. Timers, gameplay restrictions, status chips and actual Rooted interaction
are unchanged. Trip/edge recovery is outside this requested removal.

## Evidence

The original native stage fails specifically because the Frozen action root is
visible. Its existing Frozen status-chip control passes. The same case passes
with the correction: 1/1 in 0.764s, including Shock hiding and the genuine Rooted
Interact action returning afterward. Original and candidate 960x540 native UI
captures are retained; the final image was inspected. This is the actual HUD
staged over a plain background, not a full authored court or player-build film.

Initial original compilation was stopped by the whole-container headroom guard.
A verified orphaned task compiler was retired: 1,089,884,160 bytes RSS, about
1.03 GB measured container recovery. The single tooling retry compiles cleanly
and reaches the expected product failure. Candidate compile and runtime pass.
Final peak tree RSS 4,540,039,168 bytes, container 7,408,873,472 bytes, no guard
stop. Original validation EditorSettings and named profile were restored.

The existing full-map regression expectation is updated for the new requirement
but was not rerun. Refreshed player, full-court visual review, physical inputs and
human approval remain separate. No network format or gameplay timing change.
