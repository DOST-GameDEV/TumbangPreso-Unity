# Multiplayer investigation, 2026-09-27

## Sentry Target Recovery

Base `03f1c741`,protocol74. PaeteSentry's old restored spawn reran InReach against
the observer's current bodies. That could select a bystander or omit an original
target who moved; Rooted status alone did not recover the tree's own target list.

WorldEffectSnapshot now carries an explicit four-seat TargetMask. WorldFieldItem's
base is70bytes,with the new byte before the existing optional water tail. Validation
rejects out-of-range bits,the owner's bit and use on non-Sentry fields. Capture
preserves the host's selected seats; empty means empty. Missing bodies bind once
when installed,independent of their current distance,without duplicating limbs.
Recapture preserves missing seats. A restored sentry cannot execute another catch,
pull or root if authority changes. Fresh cast rules and authored visuals are unchanged.

One guarded native pass completed2/2 new cases,0failed/0skipped,0.2904714s. Tests
exercise the actual70byte receiver,invalid masks,69byte truncation,and restored
target selection after movement/late installation with no gameplay replay.
Frozen206inputs,6changed,no drift; Unity6000.5.8f1/D3D11. Minimum sampled free
storage6,369,206,272bytes; guard restored two named-profile files and three shared
Editor input preferences. [Receipt](checks/sentry-recovery-native.json),
[XML](checks/sentry-recovery-native.xml). No previous tests or films repeated.

This closes the local snapshot target-list gap,not fresh-cast peer convergence,
actual delayed-peer/reconnect/ranked acceptance or the whole networking queue.

## Voodoo Body Snapshot

Base `1848e1dc`,protocol73. The incoming body API now has a typed27-byte snapshot
for Drained/Hexed timers,mark kind/source/age and reach kind/target/elapsed/result.
SyncUnit's minimum is223bytes and its writer capacity304. The prefix sits before
AbilityAimSnapshot because that existing reader requires an exact tail. Incoming
values are finite/bounded with valid kinds,seats and canonical empty state; existing
body epoch/pose serial gates remain in charge of freshness.

Applying a fresh Drained edge can empty a local bar. It now runs before the existing
authoritative resource correction, so a legitimate host pool value remains final.
RecoveryBlocked is updated immediately on local/received application rather than
waiting for the next gameplay step. Reach success is carried explicitly in the
caster's state; it no longer depends on the target's mark snapshot arriving first.
Legacy direct body callers retain optional inference, but the network never uses it.

ONE guarded native pass completed2/2 new checks,0failed/0skipped:
the fixed prefix/aim tail,invalid bounds/truncation, and the actual223-byte receiver
with pool42 surviving a fresh status,explicit success without the target body,
old serial rejection and NaN refusal. Frozen206 inputs,7changed; no input drift.
Unity6000.5.8f1/D3D11 requested; named profile and shared input preferences restored.
[XML](checks/voodoo-network-native.xml),[receipt](checks/voodoo-network-native.json).
No old tests or films were repeated and no separate duplicate compiler pass ran.

This wires transport around the incoming API, not the new creative kit. Body ticking,
reset/HUD/speed integration,mark/doll gameplay lifetimes and actual peers remain
OPEN until the corresponding incoming runtime work is integrated and checked.

Latest evidence: the [input integration pass](input-integration.md) completed7/7
native cases, including the received-root/frozen,hold,plant-lifetime and victim-camera
checks below. It supersedes their earlier NOT RUN notes only for those exact local
cases. Actual peer/ranked/reconnect qualification remains separate and OPEN.

## Received Rooted Presentation

Base `e1f21c5f`,protocol unchanged72. The existing root coil was created only from
PaeteSentry's locally inferred target list. A client with Rooted state but no matching
list entry, or a not-yet-restored sentry, could therefore have the hold without its
body restraint. Rooted itself already travels in the body snapshot.

StatusBodyMarks now owns a received-state fallback using the existing PaeteRootCoil.
The live sentry calls that same owner, so both routes deduplicate. A fallback does
not invent a tree origin; if the sentry becomes available later, its existing outward
facing cue is established once, after which the player remains free to aim. Unroot
keeps the normal release effect; body disable/despawn retires the coil silently and
does not leave an inactive old restraint to return on reuse. Failed construction is
bounded for the continuous status and its partial object is retired.

No mesh paths,branch geometry,material colours,animation curves,root duration,
break-free timing or gameplay victim decision changed. The existing editor review
caller still uses Attach/Step; disabling only the effect component is not treated
as body despawn, and editor cleanup does not recursively self-destroy on disable.
Sentry target-list authority/recovery itself is separate from this body-status fix.

Frozen172 inputs,4 changed sources. Runtime,Editor,Tests and PlayTests compile with
unchanged Core reused. The new native case covers a received root without a sentry,
dedup,late facing once,refresh,unroot/movement and disabled-body cleanup. **NOT RUN**
under the existing native disk boundary; no old Core,broad suite or film rerun.
Actual peer/reconnect qualification and QA-15 remain OPEN.
[Receipt](checks/rooted-presentation-compile.json).

## Existing Ultimate Victim Feedback

Base `ad19d440`,protocol72. Dante and Cheska called HitFeel.Land only inside their
host-authoritative victim loops. Since Land affects only the view following that
victim, a remote victim never received the existing camera hold/punch, chromatic
pulse and vignette. Their authoritative statuses/impulses were a separate path.

Both calls now announce UltimateImpact through MatchFlair. The receive branch
invokes only the existing HitFeel behavior with the original ultimate weight and
accepted origin; no stars, extra animation, new VFX or global hitstop was added.
The accent follows AbilitySystem.HeroId, retaining the roster fallback for bodies
without a kit, so a cosmetic/custom body index cannot misidentify the power.

The shared Flair payload now carries match/round context, with a bounded37-byte
reader, known-kind validation and exact-length check before presentation. Old-match
or old-round messages cannot play against the current cast. Host local playback
still happens once and the relay excludes that host.

Frozen160 inputs,7 changed sources. Runtime,Editor,Tests and PlayTests compile with
unchanged Core reused. The new native receive-handler case covers wrong match/round,
non-victim view, the existing0.11-second victim hold and unchanged global scale/status.
It is **NOT RUN** under the existing native disk boundary; no old Core/broad suite
or film rerun. Actual peer/camera and device acceptance remain open, and this is not
a claimed fix for QA-15's unconfirmed permanent movement freeze.
[Receipt](checks/victim-feedback-compile.json).

## Plant Lifetime Delivery

Base `70ee0152`,protocol71. PlantPulled previously named only the owner's seat and
looked up that seat's newest live plant. A delayed removal could therefore remove
a replacement; a removal arriving while its cast waited for a body could also be
lost and allow the old plant to appear later.

Ordinary accepted initial casts now publish a reusable HeroAbility lifetime hook
from the existing host event ID, reaching host, confirming owner and observers.
Commands do not rename that birth. Paete binds the token to its created object;
the hook also supports a later windup-created object without tying transport to art.
The persistent field carries a separate InstanceId, not an overloaded water-event
or gameplay scalar. Plant snapshot restoration and kit rebinding preserve it.

PlantPulled now names match,round,owner,puller and birth ID. Bounded readers reject
wrong contexts and incomplete/trailing removal payloads. Per-seat retirement floors
reject duplicate/older removals and silently retire a matching late installation;
a newer plant is untouched. Floors reset with match/round identity. Offline direct
spawns retain their existing no-network behavior. No model,VFX,animation,range,
cooldown or duration changes; other persistent effect kinds are not automatically
given a complete lifetime contract by this addition.

Frozen157 inputs,8 changed sources. Runtime,Editor,Tests and PlayTests compile
with unchanged Core reused. A new native case covers command identity,replacement
protection,late retirement and snapshot identity. **NOT RUN** under the existing
native disk boundary. No unchanged Core/broad suite/films repeated. Actual delayed
peers/reconnect/ranked qualification remain open.
[Receipt](checks/plant-lifetime-compile.json).

## Host Interaction Holds

Base `c90f7ef8`,protocol70. ReqBreakFree previously ended a live root without
checking a hold duration. HostTryUproot checked plant age/reach but neither the
puller's live CanAct nor accumulated hold. The host relied on the owner's claim
that its local progress was complete.

Interact now travels in bit3 of the existing effort byte. It is adopted only after
the pose sender, epoch, flight episode and movement checks, using the same bounded
input lease as movement/sprint. Epoch changes and stale input clear its authority.
The host times remote root escape and plant pulling from this input. Root progress
persists on release; plant progress resets when the hold, reach or CanAct fails.
Existing durations, range, authored visuals and local input behavior are unchanged.

Completion requests carry the shared match/round/body scope and have bounded
readers. Neither can bypass the host's accumulated hold. The host also completes a
real remote hold itself, so an early/lost client completion notification is no longer
the sole path to escaping/pulling. This contract serves the shared ranked/casual
match code; it changes no rating, results, leave or device-pool rules.

Frozen155 inputs,7 changed sources. Runtime,Editor,Tests and PlayTests compile with
unchanged Core reused. The new native case covers early refusal, earned progress,
release, stale lease, epoch, automatic host completion and plant restart. It is
**NOT RUN** under the existing editor disk boundary; no unchanged Core/broad suite
or film rerun. [Receipt](checks/interaction-holds-compile.json).

Actual delayed/lossy peers, reconnect progress and ranked qualification remain open.
The separate PlantPulled lifetime defect is addressed by the later unit above,
not by hold authority itself. QA-15 remains open:
ordinary stun clocks were confirmed to advance on remote bodies, but that source
fact is not a reproduction or explanation of the tester's Sean freeze.

## Received frozen presentation

Base `a60d3f67`,protocol unchanged69. Existing ice restraints were spawned only in
host-authoritative AbsoluteZero/Frostbite paths. Frozen status reached clients,but
that status picture did not. StatusBodyMarks now owns the existing restraint from
the body's live IsFrozen state on each peer,including received/rejoin state.
Public host spawn calls route through the same idempotent owner. No extra RPC,
victim decision,stun duration,mesh,colour or authored animation change.

The restraint follows refreshed frozen state instead of expiring on its initial
picture timer. Thaw shatters once; body disable/despawn silently cleans owned tells.
Shatter audio is local per peer,not re-relayed. Partial construction cleans up,and
a missing visual fails once per continuous freeze instead of allocating every
frame. Shatter cleanup runs even if the thaw effect fails. The four existing ice
meshes now preload asynchronously during boot and use a retained shared cache.

Four assemblies compile on129frozen inputs (5changed),with unchanged Core reused.
[Receipt](checks/frozen-presentation-compile.json). New native received-state case
covers one restraint,refresh,thaw/movement,nocolliders and disable cleanup; NOT RUN
under the existing native disk boundary. No unchanged film/suite repeated.

QA-15 (Sean movement after Cheska ultimate) remains OPEN. Source inspection found
ordinary stun expiry,no restraint colliders,root release before activation,and a
reentrant hitstop guard; it did not reproduce that report or confirm its cause.
This fixes a separate definite host-only presentation path,not a claimed QA-15 fix.

## Correlated combat refusals

Base `99ce5338`,protocol69. World scope alone did not identify which same-round
prediction was refused. A delayed or duplicate denial could clear a newer action's
cooldown/window. Four combat requests now carry a monotonic request ID; the owner
accepts a denial only for that verb's latest unconsumed prediction in the same scope.
PredictionReceiptWindow is a bounded engine-free channel table; reset retires state
without recycling an old ID. Fixed per-seat host cursors,scoped by owner/world,reject
repeat requests before gameplay. No unbounded pending dictionary or extra approval RPC.

The older additive stamina refund also assumed the owner never received host pool
state,which is no longer true. Network rollback now clears only the correlated
action's timer/window. The host sends reliable current SyncUnit state only to that
requester; the existing pose serial rejects older corrections across delivery lanes.
Direct rollback's default resource-refund behavior remains for its existing callers.
No impulse yank or stamina-price change. Request buffers remain64bytes; denial29bytes
fits32. Matching clients are required.

Two NEW filtered Core cases ran:2executed,2passed,0failed (TRX inspected),covering
older/duplicate/cross-channel refusals and reset identity. Core plus all four Unity
assemblies compile on122frozen inputs (10changed).
[Receipt](checks/verb-receipts-compile.json). New native no-double-refund/commitment
case is NOT RUN under the recorded native disk limitation. No previous Core cases,
native suite or film rerun. Real host/owner/observer/ranked/reconnect qualification
remains OPEN; these checks do not claim the whole network assignment complete.

## Remote stamina clock

Base `8e20a495`,protocol68. CharacterMotor's remote-body FixedUpdate branch returned
before Stamina.StepFatigue/Step. Only edge recovery had its own remote resource tick.
The host spends remote shove/slide stamina and sends its resource values back with
accepted poses,but did not regenerate it or expire fatigue on ordinary remote bodies.

The host now advances those same pure resource clocks without moving the replica.
Spare effort bits carry the owner's movement/sprint intent only after its pose,
ownership,epoch and flight gates accept. A0.5s lease stops abandoned held input;
epoch changes clear it. Current host movement/fear/concussion states apply the same
resource eligibility as local simulation. Observing clients do not simulate this
authority path; existing edge recovery does not tick twice. No extra message/byte,
no map/animation change and no tuning to stamina prices,rates or fatigue duration.

Separately,ApplyNetworkSnapshot always cleared IsSprinting,turning every correction
below the sprint-start floor into a forced release. It now preserves an already
running sprint only while stamina is positive and not fatigued. A real input release
still clears it and a later restart still requires the existing floor.

Two NEW filtered Core tests ran:2executed,2passed,0failed (TRX inspected),covering
continuation/release/restart and empty/fatigue corrections. Core,Runtime,Editor,
Tests and PlayTests compile on116frozen inputs (10changed).
[Receipt](checks/network-stamina-compile.json). A new native case exercises the real
remote FixedUpdate branch,fatigue recovery,drain,unchanged pose,epoch/lease cleanup
and observer non-simulation; it is NOT RUN under the existing native disk boundary.
No old suite/film repeated. Real ranked/casual resource behavior remains unqualified;
this does not close QA-15 or the separate per-request verb-refund work.

## Ordinary action scope

Base `ce0edc7a`,protocol67. Ordinary requests only named the current seat/intent.
An old round's queued input could satisfy a later round's current ownership and
pose gates; unscoped refusals and body/charge playback had the corresponding risk.
GameplayActionScope adds16bytes: match identity,round and body movement epoch.
Requests for punch,lunge,slide,shove,grab,throw and can-reset require an exact valid
scope before the existing eligibility/outcome logic. Combat action/refusal replies
retain the original request's context even if resolution advances the world.

Body actions and charge tells also require current context; snapshot charge refresh
uses the same updated writer. Charge kind is explicit rather than an optional tail.
All changed handlers check minimum lengths and exact scope tails. Action names use
the NGO string layout with a64-character bound and exact remaining payload size,
and the writer sizes from the name. No hero-specific logic,score/rating/leave change,
input redesign or extra service query. Matching clients are required.

Frozen110source/dependency inputs,9changed: Runtime,Editor,Tests and PlayTests compile
with exit0. One new pure match/round/epoch case passes by direct managed invocation.
[Receipt](checks/action-scope-managed.json). New native codec test and amended charge
fixture (current,foreign match/round/epoch,malformed/unseated/cancel cases) are NOT RUN.
No unchanged suite/film repeat or new Unity launch under unchanged disk headroom.
Actual ranked/casual/reconnect peers remain OPEN. Scope does not provide same-round
per-request verb receipts; old same-round denials are a separate remaining issue.

## Held-aim body presentation

Base `9a44be97`, protocol66. Held aiming had no remote state even though incoming
reworks authored AimPoseAction and prop hooks. AbilityAimSnapshot now travels at
the existing pose cadence: slot,stable ID,held time and movement-epoch/hold token.
SubmitMove/SyncUnit retain their owner,epoch and pose-order gates. Private target
positions are absent. The maximum added tail is76bytes (empty15); writer budgets
are128/272bytes. Skill and shared-ultimate commits carry the consumed hold token.

Replicas tick their kits and run shared PresentAimBody/EndAimBody, never local
input/cast buffers. CharacterAnimator reads replicated IsAiming through its existing
hook. Phaister cuffs/doll move to shared body hooks; her sigils stay in private
PresentAim/EndAim. No clip,geometry,palette or timing changes. Per-slot closed/seen
tokens reject stale holds without cancelling a newer hold or another slot. A0.75s
lease clears disconnected presentation; reset,disable,phase and epoch changes
clean up. The expired-newer ultimate phase also closes its consumed hold.

One frozen86-file source/dependency candidate (19changed inputs) compiled Runtime,
Editor,Tests and PlayTests with exit0. One new pure token/scope NUnit method passed
by direct managed invocation. [Receipt](checks/aim-replication-managed.json).
The new native codec roundtrip and replica body/private/cleanup case are NOT RUN;
the prior disk-reserve limitation still applies. No old suites or films repeated.
Actual ranked/casual/observer/spectator/reconnect play remains OPEN; compilation
and a pure check do not establish multi-peer correctness.

## Ranked and casual discovery admission

Base `b7d26bcf`. Connection approval checked skill compatibility, but automatic
pairing could still choose the same incompatible host before being refused. Online
room creation/update now publishes the cached contract as an additional public,
nonindexed data value; existing queries read it. No new service request or index.
Automatic pairing requires the matching contract and a usable allocation endpoint.
Missing metadata is not auto-paired; manual joins still use normal approval.

Candidate eligibility also accounts for occupied/reserved chairs and the full party
size. It does not use the browser's no-in-progress rule, which would incorrectly
disable permitted backfill. Existing Core band,spread,block,backfill and device-pool
rules still decide among eligible candidates. No rating,result or penalty changes.

A failed connection records the attempted lobby/allocation pair for30s (maximum64
entries), not a mutable advertisement's later endpoint. A replacement allocation
can be tried immediately without reviving a stale failed advertisement. New tickets
and cancellation clear this queue-local cache. After failure, the cached list is
reconsidered after0.25s without issuing another query; a stable list at maximum band
width no longer leaves the search inert. Allocation,pool,band,backfill and contract
changes now participate in the existing discovery-change notification.

Runtime,Editor,Tests and PlayTests compile. Three new compiled NUnit cases invoked
as managed code pass: casual and ranked compatibility/party/backfill eligibility
with unchanged pool separation, plus failed-endpoint expiry/replacement/ticket reset.
[Receipt](checks/ranked-discovery-managed.json). No Unity process,live service,
profile mutation or existing suite rerun. Actual queue/admission/reconnect/party/
spectator/result integration remains OPEN; these are not real ranked matches.

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

Two findings from the initial audit, reconciled against the current source:
- The host-only Dante victim-feedback path, also present in Cheska, is routed through
  the shared event channel by the later victim-feedback unit above. Native/peer
  qualification remains separate from its successful compiler check.
- The original PlayAbility body-not-ready loss is addressed by MatchRpc.SkillDelivery:
  accepted events enter a bounded ordered queue, wait for the matching body/kit,
  and request state recovery on expiry/overflow. Its current compiler/native/peer
  limits are recorded in the cast-delivery unit above; do not revive the retired
  null-conditional implementation as a new finding.

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
