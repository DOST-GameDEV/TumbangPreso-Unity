# Throw windup at a possession boundary

Date: 2026-10-03. Original source `0368f56f1`. The five-case original native
run reproduced all three possession failures and passed both controls. The first
narrow charge/tell candidate passes all six focused native cases. Main verified
the worker and primary Carrier hashes match the frozen candidate, the exact
case set and terminal preservation receipts before approving publication.

## Contract and source evidence

A pending throw belongs to the shoe being charged. Public disarm followed by
immediate force equip must not bank that old charge onto the same restored shoe
or its replacement. An observer-only charge tell must also end when that held
relationship ends. Repeated notification of the same held shoe is a keepalive,
not cancellation. A null-to-null hand notification must not cancel a defender's
unrelated can restore channel.

Original `Carrier.NotifyHolding(null)` clears the hand relationship and then
returns without ending `_charging` or the observed throw tell. `HostDisarm`
uses that public notification, and `HostForceEquip` can synchronously disarm a
displaced shoe and equip the replacement before Carrier.Update observes the
empty hand. This is an existing public transition, not a fabricated delayed
thread race. The next release may therefore throw with the old charge. The
source's own CancelCharge contract says hand loss retires the windup.

## Original gate and stopping condition

Run exactly `TumbangPreso.PlayTests.CarrierPossessionLifetimeTests`: five cases,
expected original three causal failures and two passing controls.

- Actual TouchInput charge, public disarm, immediate re-equip of the same shoe.
- Actual charge, public force equip of a replacement through the displaced-shoe
  handover path.
- Public observed-charge state, no local charge, followed by public disarm.
- Same held NotifyHolding, NotifyEquipped and HostForceEquip keepalive control.
- Actual defender Grab/channel, then null-to-null hand notifications preserving
  that unrelated restore work.

The minimal arena fixture begins a real round with an owned shoe. Movement and
shoe flight are disabled to isolate the charge/relationship boundary. The
replacement is supplied owned equipment, exercising the public handover rather
than changing any authored art. The channel control ends knockdown hitstop
before stepping to avoid a zero-time setup failure. All state is restored and
the world resets before/after each case. Main owns snapshots and all native jobs.

Stop at fresh five-case XML and a terminal receipt. Setup/can/readiness errors
are not causal possession failures. Keep callbacks, charges and assertions
unchanged between the original and candidate. After a justified correction,
the candidate gate additionally reuses the existing ordinary release case
`InputProducerCancellationTests.ActualThrowReleaseStillLaunchesTheOwnedShoe`,
because throw release also clears its previous holder. Do not duplicate that
fixture or infer physical/peer results from public callback state.

## Correction scope

Retire only charge and observed throw tell when the held relationship changes.
Preserve same non-null keepalive and null-to-null notifications. Do not broadly
reset channels, committed contact/cooldowns, kit state or ownership rules.
Original/qualified-source evidence remains separate from this next boundary.

## Current evidence and candidate

Original worker `qa-d`, output `Logs/carrier-possession-original5`, produced
fresh five-case XML: disarm/re-equip, replacement and observer-only tell failed;
same-held and null-to-null channel controls passed. The terminal receipt records
exit2, preservation completed and no lease held. No fixture repair was needed.

Candidate `Carrier.cs` SHA256
`b390c3ecf21b0b7d85a6b9f2277ae7c8988df5a8eb21c1e7f3a1c391a6251a01`
calls existing `CancelCharge` and `ApplyObservedCharge(false)` before writing a
different held relationship in both `NotifyHolding` and `NotifyEquipped`.
The same-held and null-to-null paths preserve their original behavior. It does
not call `CancelAll`, reset a can channel or alter pickup press ownership.

The fixture SHA256 remains
`0a98fb3d4a4ff5e0ce7936734ca6ba42fa6c7023ff7d1ec7af98162049686fdf`.
Main ran the six-case candidate gate, adding only the existing ordinary
throw-release control identified above. All six pass on the first candidate,
with no fixture repair or retry. The terminal receipt records exit0,
preservation completed and no lease held.

[Original XML](native-original/tests.xml) and [receipt](native-original/job-receipt.json)
retain the three failures. [Candidate XML](native-candidate/tests.xml) and
[receipt](native-candidate/job-receipt.json) retain the six passing cases. The
same fixture and GUID were used in both runs. Full import logs stay private.

This qualifies the local touch-driven possession and public relationship
boundary plus its ordinary release, same-held and reset-channel controls.
It does not qualify a physical controller, live peer transport, authored hero
mechanics or competition readiness.
