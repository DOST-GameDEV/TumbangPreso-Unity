# Multiplayer investigation, 2026-09-27

## Phase-aware world and OMEN recovery

Integration follow-up: incoming `baec93c9` adds Phaister's refined aim hooks and
5-second intro. Both are retained; protocol65 wins the two version/comment conflicts.
The screen effect introduced by the refinement is logically owned by PhaisterOmen
for enable/disable/destruction, while retaining its independent screen-space
transform. This prevents invisible warmup/recovery cleanup leaving its screen layer
active. Runtime,Editor,Tests and PlayTests compile across73 merged source/asset
inputs ([receipt](checks/phaister-integration-compile.json)). No film/old suite was
rerun. The incoming author explicitly notes that remote held-aim presentation is
not yet on the wire; generic aiming delivery and ranked admission are next checks.

Base `bf0b80db`. WorldSnapshotHeader now carries ultimate cohort ID/stage and
processed-owner ultimate request alongside its existing cast freshness. A snapshot
captured before a cohort handback cannot erase the effects created at handback,
even though its cohort ID has not changed. New pending ultimate requests also keep
older world data from replacing their state. Protocol is **65**; all peers must match.

The PreparedWorld channel replaces CovenEffect with a single bounded serializer:
stable ability ID,match,round,accepted world generation,3D centre,preparation/live
clocks and captured round time. It is sent AFTER its world batch, inherits that batch's scene/phase/
prediction freshness, suppresses duplicate generations and ages by simulation time,
not wall time through another cutscene's pause. Empty state is now sent too, so it
can release an obsolete root/effect rather than waiting for a stale local timer.

VoodooBlackHole's optional elapsed start preserves its normal zero-start behavior.
Recovery keeps the original windup/live lengths and seeks the authored PhaisterOmen
timeline; it no longer starts a shortened fresh presentation. A restored preparation
seeks the existing body/FPP action. Empty recovery does not cancel a reserved new
introduction. Geometry,palette,animation assets and effect direction are unchanged.

Review also found that receiving a newer EXPIRED introduction retained an older
active phase and could reset deduplication repeatedly on a new match. That path now
cancels the older phase and retains the received match/round/phase as terminal,
without replaying its already-finished introduction or activating its old effects.

The owner clarified that other heroes' current VFX are provisional and that later
reworks must not rebuild networking. IPreparedWorldReplication now belongs to an
ability, not a hero/effect type switch. The sender discovers it across the entire
kit; the receiver matches its stable ability ID and deduplicates per actor/ability.
Shared restoration handles pose resume and guarded empty-state cleanup. The ability
owns its current effect factory and state. Multiple prepared abilities can use the
same channel. The compatibility fingerprint includes this recovery capability.
[Authoring contract](../../SKILL_NETWORK_CONTRACT.md) states scope and remaining limits.
Ranked is explicitly included; no queue-type branch or rating/result-rule change
was added here. Ranked callers/admission still need their own verification.

Validation: initial13-file candidate compiled Runtime,Tests and PlayTests. Two
compiled NUnit methods were directly invoked as managed code and passed: phase/
handback freshness and preparation-then-live simulation-clock aging, including
paused,expired,invalid and empty states. [Managed receipt](checks/omen-recovery-managed.json).
A follow-up compiler preflight initially refused below5GiB. After the owner's new
scope clarification, the ability-owned interface replaced the specific receiver;
the resulting20-file candidate compiles all three assemblies, including the
terminal-phase correction and its test. One NEW managed capability/fingerprint case
passes ([receipt](checks/prepared-world-capability-managed.json)). Earlier clock
cases were not repeated. The final source differs only by a comment after that
compile. The added codec,OMEN visual/cleanup and expired-cohort native cases have
NOT run; no real-peer or ranked acceptance claim. No character film was repeated.

## Skill data compatibility without cosmetic coupling

Base `5d18011e`. A manual wire version alone could admit peers with different
reworked cooldowns,aim rules or shared intro duration. `SkillContractFingerprint`
hashes canonical roster order and ability-role identity, delivery mode, resource/
duration/preparation/aim metadata and normal/held shared intro lengths. Binary
encoding avoids locale-sensitive float formatting; role order is canonical and
live timers never enter the hash. Names/descriptions,cues,clips,glyphs,palettes and
model files are deliberately excluded. Boot fills this local cache after the
existing kit/intro preload. There is no service call or LAN authentication wait.

ConnectionHello carries the fingerprint; host approval refuses a mismatch with a
specific message before admission. Existing protocol,capacity,block and identity
checks remain. Protocol is **64** and every participating build must match it.
This is compatibility, not anti-cheat or proof that arbitrary future code is safe.
Effect-internal rules and new wire/lifecycle semantics still require explicit
protocol changes and focused coverage; the hash does not inspect arbitrary code.

Runtime,Tests and PlayTests compile. Two compiled NUnit contract methods were
invoked directly as managed code: cosmetic/live-state/role/locale invariance, and
identity/mode/cooldown/intro-duration mismatch detection. Both pass. No Unity
process or native API was used by these methods. [Managed receipt](checks/skill-compatibility-managed.json).
The existing real approval fixture now includes the new hello field and negative
control, but it has NOT run on this candidate. Native approval,actual transport
and Windows/Android compatibility remain pending under the recorded disk limit.

## Room listing state and admission

Base `ac5524d9`. The room adapter discarded LAN `IsJoinable` (which includes reserved
seats and connection capacity), and the screen inferred JOIN eligibility from
visible player count. Online occupied/reserved seats were likewise ignored by the
row. Both adapters now carry the source admission decision. Displayed player counts
remain seated players, not a misleading total including reservations/spectators.

The redraw key concatenated every room's key/player-count/in-progress fields each
second, but omitted name,capacity and admission. It now compares the actual visible
row values directly, including all displayed metadata. Renaming or closing admission
refreshes the row even when the visible player count is unchanged. Unchanged rows
and changes outside the existing five-row window do not rebuild the displayed list.
No layout redesign or change to the guarded connection path.

Runtime/Tests compiler checks pass. Two direct managed checks invoke the compiled
HubRoom/LanEntry helpers without Unity: reserved/socket capacity admission versus
visible player count, and every public listing field invalidating equality (7fields).
[Managed receipt](checks/room-listing-managed.json). Added equivalent focused
EditMode cases for the next native integration run; no Unity runner case ran here.
Native row interaction and live discovery remain pending.

Compiler preflight initially refused before launching because freeC was below5GiB;
a read showed3,779,158,016bytes with no Unity/dotnet process. After free space
recovered above7GB, one bounded compiler retry completed. A proposed cleanup of the
earlier task-owned aborted player was blocked by the tool before execution; no
files were deleted and no alternate deletion method was used. No Unity relaunch.

## Explicit cast identity, intent and delayed delivery

Base `0e00de51`. ReqAbility/PlayAbility now share `SkillCastMessage`'s single bounded
serializer. It carries the stable ability ID, explicit new-cast/command intent,
full3D aim and the existing match/round/request/event/familiar/flight facts. IDs are
UTF8-capacity-checked at kit/build validation. Model, clip, effect and display names
are not identity. The host rejects wrong role/ability/intent before mutation and
still owns resources/outcomes. Accepted replicas preserve command intent even if
their active clock expired; they cannot silently turn a command into a new spawn.

A64-entry,2-second delivery queue retains accepted casts while the matching body,
kit or familiar is installing. It preserves each actor's order, deduplicates before
queueing, and lets other ready actors proceed. Match/round/active-scene/transport
changes retire the queue. Expiry/overflow uses existing scoped snapshot recovery,
not indefinite replay. World snapshots defer replacement while casts are queued.
Protocol is **63**; every participating platform needs matching builds.

Added focused codec round-trip/malformed cases and an actual receiver case for
body arrival, duplicates, command intent, wrong ability identity and round reset.
Updated the existing flight message fixture and protocol pin for the changed wire
format. These are pending tests, not claimed passes.

Native attempt stopped BEFORE compilation/tests at11:44:35 when freeC reached
4,668,747,776bytes, below the4.5GiB precaution threshold. No resultXML exists.
The PlayMode pass was not launched. The initial editor log had no compiler verdict.
Free space rebounded after exit; the cause of these transient startup drops is
unresolved. No repeated editor launch was used for this batch.

Bounded fallback: Unity's installed Roslyn compiler and existing Bee response files,
with outputs redirected outside the project cache and the two new sources included.
Runtime, EditMode tests and PlayMode tests each exit0. This checks compilation only:
no IL postprocessing, player build, tests or real peers.14/14 candidate input hashes
match. [Compiler receipt](checks/cast-delivery-compile.json). Shared presentation,
new runtime cases, real peers and full NET-SKILLS-1 acceptance remain OPEN.

## Approved replay and joining status presentation

Base `0706a9ef` includes the incoming Phaister rework, preserved unchanged in look
and mechanics. Her status presenter previously installed only on MANIKA/PIN casts.
A joining peer receives status timers, not historical casts, so it could omit the
moonlight and hex mark. Presentation now follows each CharacterMotor's lifecycle,
like existing body status marks. Disable/despawn destroys only that body's tells;
received expiry still plays the authored ending. No effect geometry or timing edits.

Accepted replica casts carry an internal execution-context flag, preserved through
windup capture. This bypasses predicted-effect deferral for unpredicted accepted
owner casts and permits Paete's accepted shot despite a replica reload clock lag.
Ordinary commands and host eligibility still enforce readiness; retiring plants
still refuse shots. Existing transport event deduplication is unchanged. Owner
request0 uses full playback because there is no predicted cast to confirm. The
approved flag also avoids waiting for a second approval for Phaister's sky.

Focused native PlayMode: **2/2**, 0 failures/skips, 0.3334298 s, graphics/D3D11 in
the existing isolated checkout. Receipt: [replica-lifecycle.xml](checks/replica-lifecycle.xml).
Cases cover the actual replica/authority entry points, normal shot gating, active
plant retention, received timers with no original cast, expiry and per-body cleanup.
Only these two new cases ran. No prior passing suite or character film was repeated.
The private manifest captures100 synchronized dependency/source files (including
the incoming rework); it is not100 test cases. The runner was initially invoked
before copying finished and exited before Unity; it was launched once after copy.
The sky flag follow-through was source-reviewed after the run, not runtime-qualified.
These are local provider/object checks, not real transport or all-peer acceptance.

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
