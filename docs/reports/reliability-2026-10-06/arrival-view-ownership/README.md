# Preserve current view ownership after arrival

The opening saved an active gameplay rig then unconditionally reactivated it during return or cancellation, even after the public seat handoff switched the user to spectator. Arrival now checks current HUD view ownership before returning gameplay control, with spectator/launch fallback when the HUD is absent. Normal gameplay and reseating still restore their active view. Camera poses and authored hero motion are unchanged.

Native original Unity24808 reproduced two watch-return failures with two gameplay controls passing. Candidate Unity22772 passes all four same cases, including public cancellation and the final normal arrival sample without a launch spectator flag. Each run restored 21172 frozen inputs, 216 native importer/settings deltas, input/editor preferences, quality settings and the isolated profile. Both jobs are terminal and no source deltas remain beyond the intended patch/test and pre-existing private work.

This establishes the lifecycle defect and correction. Natural installed-model selected-wide visibility, the Arena opening variant and current packaged/live-peer acceptance remain open; these four controlled cases do not establish complete spectator or network readiness.
