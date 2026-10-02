# Steady Ember implementation plan

Status: planned, no implementation or qualification claim.

The live Pyro proposal rewards manually retrieving a genuinely thrown own slipper:
the next throw charges25percent faster, once within4seconds, without increasing
maximum power. Dropping and picking up cannot refresh it. This supports the
retrieval-centred vision through a small tempo reward, not damage or another
floor field. It uses the published all-hero research and Sean's action plan;
no new footage or listening claim is made by this implementation pass.

## Operational reading

Use1.25x charge accumulation while the four-second window is live, capped by the
existing full-charge value. This is an80percent time-to-full at the same maximum.
One accepted throw consumes the window. An aborted charge does not create a fresh
window; the original deadline continues. Drop/regrab can retain only the original
remaining time, never grant or extend it. Round/role reset and expiry retire it.
Classic and every other kit retain multiplier1. No power, curve, flight, cooldown,
scoring or existing ultimate-retrieval economy changes.

## Identity and authority

Do not award from Carrier.NotifyHolding or OnOwnSlipperRetrieved alone: those
are presentation/economy funnels and lack a genuine throw episode. The inspected
host-only Slipper.HostGrab is reached by ordinary and slide retrieval, while
HostForceEquip is a distinct automatic route.

Track a private host-owned eligible retrieval episode on the slipper when its
owner performs a real throw. Bind it to the current actor instance and match/round.
Capture and consume eligibility before the actual HostGrab switches to Held.
Any other Held transition, forced equip or round mismatch invalidates eligibility.
Landing does not destroy it. An environmental displacement must not mint a new
owner throw episode. The episode is consumed even if its current hero has no
retrieval passive, so drop/regrab cannot grant after a later hero swap.

Use default-neutral HeroKit hooks for a qualified manual own-throw retrieval and
an authoritative released throw. Sean owns the four-second state. No other kit
is hard-coded into Carrier or transport. Shared throw accumulation reads a
bounded multiplier from the current kit; the existing accepted host throw still
owns trajectory and outcomes.

## Shared-state delivery

Extend the existing generic TimedKitSnapshot/TimedKitState with an optional,
ability-bound passive remaining value. Existing kits bind a zero-capacity passive
channel. Sean binds four seconds alongside its unchanged eight-second loaded
slipper state. Validate finite duration, scope, hero, round and sequence; age the
new value with the same adopted round clock. Positive passive state is rejected
outside a live round. A newer empty state clears it; stale/repeated packets do
not extend it. Recovery does not award a retrieval or replay a cue.

Broadcast grants/consumption through the existing TimedKitState route and scoped
sequence, without a Sean-only RPC or a second private timer message. Update the
wire maximum and compatibility version before publication. Protocol131 is the
planned next value only if no other contributor has already adopted it; inspect
current protocol and ownership before changing the constant.

## Exact reservation

- Runtime/Slipper.cs: private eligible episode and qualified host grab/release hooks
- Runtime/Carrier.cs: existing charge accumulation multiplier only
- Runtime/Abilities/HeroKit.cs: default-neutral hooks/rate/passive capacity only
- Runtime/Abilities/SeanHeroKit.cs: passive grant, consumption, tick/reset and recovery
- Runtime/Abilities/ITimedKitReplication.cs: optional bounded passive channel
- Runtime/Net/TimedKitState.cs and MatchRpc.TimedKits.cs: shared channel/sequence
- Runtime/Net/NetSession.cs: protocol constant only
- Tests/SeanSteadyEmberTests.cs and metadata; existing PlayMode/SkillReceiptTests.cs permanent-flag byte offset only,
  because the new passive float follows that byte; focused PlayMode/SeanSteadyEmberProbe and metadata
- Runtime/Diagnostics/NetSeanProbe.cs and tools/net_sean_review.py: new passive case
- tools/playmode_suite.py: the new fixture's single partition entry
- This report, live network contract, TODO and ledger

Paths above are relative to Assets/TumbangPreso except tools/docs. Do not touch
Amihan, the private Supernova opacity work, account/queue/completed-arrival fixes,
other heroes' art, or the already-qualified Cinder mechanic. Expand ownership
explicitly before another path is edited.

## Acceptance before shipment

1. Real own throw and manual grounded retrieval grant once. Initial equip,
   automatic catch, denied grab, other ownership, duplicate notification and
   drop/regrab do not mint or refresh a window.
2. Actual held input charges at1.25x while live, reaches the unchanged power cap,
   consumes on one accepted release and returns to normal on expiry/reset.
3. Pause, round/role exit, lost equipment and an abandoned charge retain honest
   timing. Existing retrieval points and non-Sean charge behavior stay unchanged.
4. Generic state rejects malformed, stale, wrong-scope and over-capacity values;
   aged positive and newer zero recover without a second reward.
5. Matching actual host/owner/observer players establish grant, faster charge,
   consumption and no regrant from drop/regrab. Native-only checks are not peers.

Use small focused runs and retained failures, not unchanged broad matrices.
After the cloud compiler/disk recovery, keep compiler concurrency bounded and use
an explicitly labelled compressed internal Linux candidate when needed. Preserve
all source, existing profiles and other contributors. No SFX or untested taste claim.
