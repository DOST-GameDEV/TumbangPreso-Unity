# Multiplayer investigation, 2026-09-27

## Scoped world recovery and bound ownership

WorldFieldBegin now has one shared bounded serializer for both directions. It
carries presentation-match identity, round/scene/generation, processed-owner-request
and host-event watermarks, and the round simulation clock. Scene bytes are bounded
before fixed-capacity storage, including unchecked player builds. Malformed,
truncated and trailing data are rejected. Protocol is **62**; older builds must
not mix with this envelope, and all participating platforms need matching builds.

The receiver checks freshness at both beginning and completion, including actual
unsettled owner predictions, observed host casts and the local scene instance.
An obsolete batch cannot erase newer work; the existing scoped/coalesced refresh
requests current state. An ended round still clears persistent fields. Snapshot
aging uses simulation-clock difference so a held cutscene does not consume lifetime
merely because wall time advanced.

World ownership reconnects through `IWorldEffectBinding`, retaining Sean/Zack's
existing behavior and adding Paete's active clock/owner binding. A restored visible
plant no longer leaves the corresponding skill inactive. Replaced plants leave
gameplay/capture immediately while retaining their existing fade; lookup skips
disabled/retiring objects. Retirement releases pullers, and kit reset removes only
that player's plant instead of all players' plants.

Focused evidence: [EditMode 6/6](checks/world-snapshot-codec.xml), 0.1174826 s,
including header round-trip, malformed data, paused clock and old/current protocol
approval/pool checks; [PlayMode 2/2](checks/world-snapshot-lifecycle.xml), 8.731889 s,
including the actual receiver's stale-prediction/event rejection, current empty
replacement, restored plant command state and independent-owner reset. The first
compile needed the existing Unity.Collections assembly explicitly referenced by
Runtime and PlayTests; no package update occurred. Seventeen authored source hashes
matched the frozen native candidate. No physical peers, player build or complete
network-presentation qualification is claimed.

## Independent effect receipts and command lifecycle

Owner effect confirmations no longer depend on the newest request in a slot.
They are tracked per request, slot and ability instance, independently of the
latest resource receipt. An earlier accepted effect can appear after a later
prediction; duplicate, denied and reset confirmations cannot create it again.
The bounded 64-entry window applies prediction backpressure instead of evicting
accepted work. Real message-manager replacement retires the old request scope.
Older denials cannot correct a newer teleport or overwrite newer resources.

An active deferred skill cannot recast until its first object is confirmed. Its
cooldown still drains, but live-state ticking waits until initialization. This
prevents Paete's missing-plant check from ending the skill during response delay.
Recast confirmations are commands, not new activations: they do not rerun the
initial plant spawn. A denied deferred command preserves the earlier accepted
active effect while still giving refusal feedback and authoritative resources.
The wire layout is unchanged; recast intent here is local receipt bookkeeping.

Four distinct local PlayMode cases pass across one run and one fixture-only repair.
The [initial run](checks/effect-receipts-initial.xml) passed both new lifecycle
cases and the existing ice case; the old wall fixture selected attacking FROSTBITE
instead of defending GLACIAL WALL. Selecting the actual defending role retained
all wall/collision assertions and [passed alone](checks/effect-wall-role-repair.xml).
No broad rerun, player build or real-peer claim. Source envelopes remain 93/0.

Host-confirmed windups without a deferred preparation adapter now fail contract
validation rather than silently executing the owner effect before approval.
Explicit observer recast intent, late-join active-state hydration and complete
presentation/state coverage remain open. These tests do not close NET-SKILLS-1.

## Explicit skill delivery contracts

Every concrete `HeroAbility` must now implement `NetworkMode`: Predicted,
HostConfirmed, SharedUltimate or Unavailable. There is no inherited default. The
existing owner-effect deferral flag derives from that declaration, so ordinary
host-confirmed effects keep using the shared request/receipt route. All 34 current
runtime ability classes and the existing test doubles declare their current behavior;
no skill mechanics, animation or effect design changed.

The kit factory validates IDs and routing. A normal Unity pre-build callback also
checks the whole current hero roster, including missing factory registration,
unspecified/invalid modes, ultimate-slot routing and bogus unavailable real skills.
It uses Unity's [pre-build interface](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.IPreprocessBuildWithReport.html).
Five focused EditMode cases passed, including the actual callback and rejected
invalid declarations ([receipt](checks/skill-delivery-contract.xml), 0.0672063 s).
No player build was produced by this check.

For a new skill, choose its delivery mode explicitly and use the existing cast
pipeline. A new timed grant must bind its owning ability through timed replication;
world, movement, companion and flight state still require their appropriate restore
path. The declarations do not prove those paths exist or behave correctly. Owner
scope now explicitly includes body animations, VFX, cutscenes, cancellation and
cleanup for all peers/spectators, including late join/reconnect. Remaining state
coverage and real-peer presentation qualification stay OPEN under NET-SKILLS-1.

## Timed-state ownership correction

The TimedKit receiver used a ten-second fixed bound and then clamped most heroes
against the live second slot. Dante's current SHIELD is a twenty-second signature;
its snapshots were either rejected above ten seconds or clamped to BOULDER's zero
duration. Sean and Zack also need their attacking ability, not a defending placeholder.

`ITimedKitReplication` now binds capture/restore and duration validation to each
kit's actual timed abilities. The existing seven-field wire envelope and protocol
61 remain unchanged. Dante, Sean and Zack implement the binding; Nemu's retired
Phantom Veil no longer emits a meaningless timed message. Amihan keeps its separate
episode-aware flight state. Common validation rejects nonfinite/negative timers,
out-of-range values and unsupported ultimate-pending state before restoring anything.

The focused graphics-enabled EditMode case passed 1/1, 0.1096448 s, compiling the
current source. It checks the eighteen-second shield sample, role-independent
attacking bindings, pending support and malformed timers. [Receipt](checks/timed-kit-contract.xml).
The source wire audit remains 93 messages with zero count/type mismatches. This
does not prove real-peer rejoin behavior or complete NET-SKILLS-1; mandatory skill
declarations, remaining state channels and extension enforcement are still open.

## Published lifecycle fixes

Initial source review at `04886cc4` found that host starts could resume after STOP
or a newer operation, and matchmaking continuations could publish stale success
after cancellation. Published fixes use operation ownership through shutdown,
host/Relay awaits and matchmaking completion. Pending queue joins wait for both
connection and an assigned seat; cancelling an old attempt cannot stop its successor.

This batch is no longer native-unrun: the first focused lifecycle group passed
11/11, pending join 1/1, and the post-merge EditMode group 37/37. Exact revisions,
checks and limits are in [validation.md](validation.md). These controlled local
tests are not live Relay, two-peer reconnect or service-timing qualification.

Room-title fixes are published in `367a78ae`: known LAN directory titles are
retained by the current session, room code and attempt, including controller
recreation and transport restart. See [joiner title](qa04-joiner-title.md) and
[QA2](qa2-validation.md). Physical typing and actual peer coverage remain separate.

## Client-only skill report

The owner reports that many skills may fail in multiplayer or affect only the
casting client. No specific affected set or reliable reproduction was supplied.
Keep three questions separate: did the host accept the input, did authoritative
gameplay occur, and did the owner/observer receive its presentation and state?
Do not weaken host authority or infer success from local effects.

A source audit of the current integration candidate reports 93 message envelopes
with zero count/type mismatches. The authority scan's RafiWaterField alert is
caller-gated; the Paete pull-sound alert has an existing PlantPulled receive route.
Those alerts do not establish product defects.

Two specific findings remain OPEN:
- Dante's ultimate calls HitFeel.Land inside its host-only impact loop, so that
  local feedback does not execute on remote peers. Authoritative impact resolution
  is separate and remains host-owned; this is not evidence that damage/status fails.
- PlayAbility advances its event watermark before null-conditionally applying to
  the installed body. An event received before that body exists can be consumed
  without playing. The conditional source loss is real, but actual occurrence and
  its proper recovery require a focused reproduction before changing routing.

The current flight unit includes protocol-61 episode/receipt/snapshot ordering.
Its exact implementation and qualification status are in
[Featherfall](../amihan-kit-2026-09-27/featherfall.md). It does not establish that
every kit works across real peers.

## Sean after Cheska's ultimate

QA-15 remains unresolved. The expected Ice freeze is 2.5 seconds after impact,
followed by reduced speed; the ultimate introduction is a separate interval.
Source inspection has not proved a permanent Sean-specific movement lock.
[Sean investigation](sean-freeze.md) records competing explanations and the opt-in
two-process diagnostic. Controlled movement bypasses physical input and bots, so
a future pass would not cover every interpretation of the tester report.

Required peer qualification includes host, owning client and observer outcomes,
join/reconnect state, stale-packet rejection and interrupted ability recovery on
matching builds. No real-player acceptance result is claimed here.
