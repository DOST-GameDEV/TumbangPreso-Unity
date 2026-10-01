# Quick Circuit and recast impairment fix

The already-started Quick Circuit unit is finished before the October1 owner
scope switch. New Docs-queue work belongs to DOTS; this worker continues
independent network, bot and optimization defects. No cross-chat action.

## Implemented and checked

Zack signature now cuts2metres sideways,30second cooldown,.15second tell and
.2second recovery following the friction-derived impulse. It has no sustained
sprint, stun trail or immunity. Existing authored clip/cue assets remain.
Movement input chooses the side; no lateral input defaults right. The captured
cast aim carries that choice consistently through existing host approval.
Overclock offers one optional second cut within1second. It waits for recovery,
spends no additional cooldown, cannot be refilled into a third cut, expires
cleanly and clears at round reset. Movement recovery ages its optional window
without replaying an impulse or altering cooldown/resources. A pending second
cut keeps its identity when the accepted preparation completes.

The shared recast branch previously bypassed CanAct and Zapped. A real native
baseline returned Cast while Zapped; it now refuses Zapped, stun and inactive
round follow-ups before entering the reactivation path. Existing explicit
impaired-cast exceptions are retained.

## Evidence

Unity6000.5.8f1 Windows/D3D11 isolated candidate, source937ca6483 plus listed
owned overlays. Baseline2/2 failed. Final10/10 passed, including actual motor
movement in both directions and lifecycle/recovery/impairment cases.742 frozen
input hashes unchanged; no tooling repair/retry. Named profiles preserved;
both guards terminal. [Raw receipts](quick-circuit-checks/result.json).

## Network and scope limits

Protocol126 adds one bounded movement flag byte:0ordinary,1optional follow-up,
2second cut. Sean remains0. Movement frames now preflight strings/fixed fields
and reject truncation, extra bytes, conflicting flags and Sean flags before
state application. This framing is statically inspected, not a new actual-peer
claim. Existing match/round/world-generation gates remain.
No current126player/peer/loss/Relay/device approval is claimed. Proposed cut
timing is an implemented initial target, not human balance approval. Closed
Circuit and remaining full-kit/Wiki tasks are left to DOTS. No authored art,
loading, private overlay or protected character kit edit.
