# One pickup press across synchronous hand loss

Date: 2026-10-03. Original pickup condition from `0368f56f1`. The original
three-case native gate reproduced one successful-press reuse failure and passed
both controls. The separate narrow pickup candidate on the published
possession fix `ca004fe5b` passes all three cases on its first native run.
Main verified matching worker/primary source and terminal preservation before
approving publication.

## Contract and distinct source defect

A successful pickup owns its Grab press until release. InputIntent.JustPressed
persists until the next physics commit, so several render Update passes may
observe that same press. If a public HostDisarm empties the hand between those
passes, the same successful press must not pick the shoe up again. A refused
pickup must not consume the press, and a real release followed by a fresh press
must still retrieve the shoe.

Carrier already stores `_grabPressConsumed` and preserves it until release,
but StepAttacker's pickup condition does not consult it. The second pass can
therefore regrab after hand loss. This is distinct from the existing
PickupPressOwnershipProbe, which keeps the hand occupied between Update passes
and checks that the press does not also shove. The ordinary client request
path can likewise repeat a successful request while awaiting the host, since
TryPickup returns true before Held changes; that is source-supported reachability,
not an actual-peer result from this planned local fixture.

## Gate and stopping condition

Run exactly `TumbangPreso.PlayTests.CarrierPickupPressLifetimeTests`, three
cases expected original one causal failure and two passing controls. The
fixture begins a real round and uses a stationary owned shoe, direct InputIntent
and the actual Carrier.Update, HostGrab and HostDisarm methods. No physics
commit occurs between the successful pickup, synchronous hand loss and second
Update. It verifies the disarmed shoe remains eligible and in range so refusal
cannot be attributed to geometry or ownership.

The release/commit/new press control verifies a fresh retrieval, while the
out-of-reach refusal followed by an eligible target before commit verifies that
an unconnected pickup request is not spent. Movement is disabled to keep the
reach condition deterministic. Both hooks reset the world and restore source
provider, launch, network and stats state.

Stop at fresh three-case XML and a terminal guard receipt. Main owns all worker
snapshots and execution. The proposed minimal correction consults the existing
successful-press flag in the pickup condition, preserving its current release
reset and failed-request behavior. Do not mix this into the separate charged
possession handover correction or change input-edge, ownership or grab rules.

This tests native local consumer/relationship behavior, not physical hardware,
authored hero snatch behavior, transport messages, screenshots or competition
readiness. No protected hero or asset is altered.

## Original evidence and correction

Main ran the frozen fixture in `qa-c/Logs/pickup-press-original3`. The same-frame
successful pickup followed by hand loss fails; the release/fresh-press and
refused-then-eligible controls pass. [Fresh XML](native-original/tests.xml) and
[terminal receipt](native-original/job-receipt.json) retain that exact result,
exit2, preservation completed and no lease held. No fixture repair was needed.

The candidate consults the existing `_grabPressConsumed` flag before attempting
another pickup. It keeps the current release reset and marks the press spent
only after a successful pickup. It does not alter ownership, grab eligibility,
input edges or the charged-possession correction. Candidate Carrier SHA256:
`a7e78b1ea7e8855bc67b5dfd2dd5de48cd04412ec7e53ef0733400727ffa3bbc`.
The three-case fixture SHA256 remains
`0e7d25f6d691ee9934036601f2c6fc0667e8c39759ae0ab5a6e3e9ce100439fc`.

Main's `qa-c/Logs/pickup-press-candidate3` passes the same three unchanged
cases on the first candidate, with no fixture repair or retry.
[Candidate XML](native-candidate/tests.xml) and [receipt](native-candidate/job-receipt.json)
record exit0, terminal state, preservation completed and no lease held.
