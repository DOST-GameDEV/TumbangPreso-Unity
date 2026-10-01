# Haunted bot actor perception

Haunted previously left bot observation and selection unchanged. Unknown actors
fell back to their live transforms; remembered actors continued updating while
outside reduced sight. Bots could choose those actors for tags or ability aims.

In Hero Strike, a Haunted observer now samples actors within seven metres of
its body, using the existing sight pass's outer range as the sensing limit.
This is body-distance sensing, not identical to the camera's eye-depth fade.
Its own position remains exact. Previously seen positions and velocities remain
in memory outside the range; they stop updating until sight returns. A new body
in a reused seat has no inherited memory.

Unknown position and prediction are nullable, never invented coordinates.
Actor selection uses a value-type filtered view over the existing player/body
lists. Tags, threats, defender/claimant lookup, sabotage, target counts, aim and
external reach-target results share the sight gate. Facing uses observed position
rather than a separate live-position read. Ordinary ability mechanics, input,
authority, tier tuning and protected hero implementations remain unchanged.

## Evidence

Native D3D11 baseline 4/4 fails at the intended defects: unknown position,
replacement position exposure, distant memory tracking and far tag selection.
Final 13/13 passes: four Haunted cases, seven existing observation/ranking/self/
allocation controls and two real close-tag commitment/input cases. Checks include
unknown prediction, threat/area/sentry/facing refusal, near reacquisition with
reaction lag, status clear, and replacement identity.

Source 8120ab03c plus two owned overlays; 670 frozen inputs have no drift and both
files match the tested candidate. The first final launch failed compilation on a
missed nullable caller. One bounded source integration repair fixed it; no test
expectation or fixture changed. Jobs51299/66972/91287 are terminal; guarded
profiles/input preferences preserved. [Receipts](haunted-actor-checks).

This qualifies actor perception and the local affected input path. Slipper/can
knowledge is separate follow-through, not a claim that every bot sensor is done.
The earlier actual116 Haunt pair predates this host-AI change. No new peer/player,
hardware, audible mix or whole-match difficulty claim. Protocol117 and authored
assets/loading remain unchanged.

October1 follow-through: the separate shared spacing board also gates hidden
actors' fresh lane claims. Three new native cases pass after one reproduced leak;
[exact evidence](haunted-spacing.md). This extends sensing without changing kits.
