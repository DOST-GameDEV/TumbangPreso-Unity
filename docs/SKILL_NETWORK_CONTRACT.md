# Skill Networking Contract

Presentation is replaceable. Stable ability IDs and shared gameplay state are the
network boundary, not model files,effect class names,clips,palettes or cue names.
Only Paete currently has substantial VFX; other presentation remains provisional.

World identity must change on every network match, including rematches. Protocol89
announces the ended/current identity pair before arena reload and accepts only a
matching forward transition. Reusing a round-one/body-zero scope from the previous
game defeats otherwise correct packet guards. Cosmetic changes do not mint world
identities; the host's match/rematch transition owns them.

## Kuro Sit Compatibility

Protocol94 changes nemu_skill1 from Terrify to the Wiki Kuro: Sit recall anchor.
Its stable ID remains; peers with the former behavior cannot join this version.
The existing prepared-world route sends its anchor and simulation-clock lifetime,
including authoritative empty state. Restore does not cast, spend resources or
replay a cue. Recall uses CharacterMotor.Teleport for confinement and authority;
observer playback cannot move another player's motor. The existing request/event,
match/round/scene and snapshot-generation gates protect recovery ordering.
Native lifecycle, real-companion placement and resource checks pass; actual-peer
qualification remains open. [Evidence](reports/feedback-2026-09-30/nemu-kuro-sit.md).

## Kuro Fetch Delivery

Protocol95 changes nemu_skill2 delivery from forced equipment to a loose slipper
beside its owner. Only the host moves or lands the slipper; ordinary pickup retains
its ownership, reach and status gates. A changed owner or non-loose state ends the
old fetch before any transform write. Delivery, interception and cancellation reuse
Slipper.HostScatter for the existing terrain, playable bounds and landing state.
The existing slipper snapshot route publishes the result; no packet layout changes.
Native delivery/authority checks are recorded in [the Fetch report](reports/feedback-2026-09-30/nemu-kuro-fetch.md).
Actual-peer transport and reconnect qualification remain open.

## Airburst Compatibility

Protocol102 changes Amihan's existing ultimate outcome to include Whirled and
an airborne launch of caught loose/in-flight slippers. ApplyWhirled disarms caught
holders before the slipper pass. Existing immunity and status snapshots remain.
HostThrow with a null thrower reuses environmental flight and carries no shot
credit; existing slipper state/pose snapshots distribute flight and landing.
Body carry still uses the existing owner-delivered Carry message, capped lift7m/s,
15m/s horizontal speed and the unchanged16m travel budget. No packet layout changes.
The2.5s accepted windup and stable ability ID remain. Actual peers require matching
protocol102 builds; local authority checks are not actual-peer qualification.

## Second Wind Compatibility

Protocol104 appends a bounded Second Wind remaining float to Amihan's existing
scoped flight recovery tail (61bytes). The same match, round, epoch, generation,
request/event watermark and adopted-clock gates protect it. A non-live round
cannot restore positive time; simulation-clock age preserves ordinary pauses.
Drift uses the current35second cooldown with its stable ability ID unchanged.

Amihan opts into owner accepted-cast events through HeroKit.RequiresOwnerCastEvents.
The existing matching owner request receives its event without activating or
spending the predicted payload again. Rejected prediction alone grants no speed;
repeated/older accepted events cannot refresh the timer. Shared ultimate reserved
activation starts the same clock. Offline successful kit casts use that clock too.
Round/transport reset clears it. This changes no other kit's event subscription.

## Whirled Reset Compatibility

Protocol105 makes Whirled refuse and cancel can-reset channels on both the local
Carrier target gate and HostMayChannelReset. The existing status timer and unit
snapshot carry the state; no new packet field. Other CanAct permissions remain.
Matching builds are required. Local channel and host-gate tests are not actual peers.

## Earthbound Compatibility

Protocol106 halves Dante incoming horizontal knockback/carry distance in Hero
Strike. Existing Impact/Carry packets still deliver unmodified host intent to
the simulating owner; its motor applies the kit factor once. Replica pose adoption
does not reapply it. Held speed/time use sqrt(distance scale), lift stays unchanged.
No new packet fields. Classic and default-one kits keep their existing behavior.

## Unstoppable Compatibility

Protocol107 aligns Dante ward/Bastion timing and permits only the cleanse
signature to cross ordinary impairment in an active unpaused round. Tagged and
physical trips still refuse it. Host validation uses the same ability eligibility
as offline input; accepted playback cannot clear a newer tag. No packet layout
changes. Existing bound timed-kit recovery reads the corrected15second duration.

## Cosmetic Reworks

Haunted status delivery appends a bounded remaining-time float to SyncUnit before
Voodoo/Aim. The existing host/loopback/world/body/serial gates remain. Invalid
remaining values reject before serial/state mutation. Existing status IDs and
Voodoo semantics remain. Native receiver/actor/HUD cases pass; actual peers and
full Haunt perception/chase remain separate.
[Evidence](reports/feedback-2026-09-30/haunted-runtime.md).

Kuro Catch protocol109: nemu_skill2d becomes a host-confirmed five-second upright-can
protection. Lata retains the ability-owned clock independently of restoration;
the existing clock snapshot publishes the current maximum. Shared prepared-world
recovery restores active/empty remaining state without recasting. Approved replicas
project that clock for local state/presentation; unapproved calls cannot grant it,
and actual knockdown always resolves on the host. Eight distinct native cases pass;
actual peer/companion rendering qualification remains separate.
[Evidence](reports/feedback-2026-09-30/nemu-kuro-catch.md).

Cheska protocol103: the current Wiki7.5s field,1.5s ultimate delay/every-player
targeting and Chilled shove passive require matching builds. Shared ultimate
delivery and existing status/slipper snapshot formats stay intact. Five native
cases and one Core numeric case pass; actual peer delivery remains separate.
[Evidence](reports/feedback-2026-09-30/cheska-wiki-rules.md).

Frostbite protocol101: a held slipper is required for activation; on body impact
the host applies Frozen before generic affinity cleanup. The normal slipper and
motor snapshot routes are unchanged. Native defender/attacker flight, eligibility
and neutral-control cases pass; actual peer transport remains unqualified.
[Evidence](reports/feedback-2026-09-30/frostbite-delivery.md).

- Keep an ability's ID when its gameplay identity is unchanged. Display names,
  meshes,clips,effect implementation and cues can change without adding RPCs.
- Continue using the ability's CastAction/ViewmodelAction and shared cast path.
  Do not send a second animation/effect RPC from the renderer.
- Keep gameplay outcomes host-owned. Rendering an accepted cast must not spend
  resources again,award points or choose new victims on an observing client.

## Existing Routes

- HeroAbility.NetworkMode is mandatory. Ordinary prediction,host-confirmed instant
  effects and shared ultimates enter the existing delivery paths. HostConfirmed
  windups still require a prediction adapter; a recovery interface alone is not one.
- ITimedKitReplication binds clocks to their owning abilities,not a mutable slot.
- IWorldEffectBinding adopts restored world objects without casting again.
- IPreparedWorldReplication is implemented on an ability that owns one spatial
  effect through its preparation/live clocks. MatchRpc discovers these abilities
  automatically,including both role abilities; no hero/effect type switch is needed.

## Timed Recovery

Generic ITimedKitReplication uses TimedKitState (introduced in protocol84). The
current protocol86 bounded230-byte envelope carries GameplayActionScope (match, round, body epoch),
seat, per-seat sequence, stable hero ID and the IDs of both optional owning ability
channels, remaining clocks, pending flag and the host's remaining round clock. A channel
swap cannot silently restore a different ability just because the hero name matches.
CaptureTimedKit supplies these bindings; use the same stable IDs through cosmetic
reworks. Validate every channel and age its clock against that ability's duration
before invoking RestoreTimedKit. Empty channels must have no time or pending state.
Age uses positive round-clock progress, not wall time, so paused introductions and
slow motion preserve simulation lifetime. Non-live rounds accept only empty state.

The receiver rejects stale scope, wrong bindings and duplicate/older snapshots before
restoration. A valid ignored restore (already active or consumed) still advances its
cursor. New transport binding resets receive cursors, not the host's sequence. Keep
the kit's existing consumed/active guards; this is recovery, not permission to cast
or spend resources. The specialized Amihan flight TimedKit envelope and its existing
generation/request/event/pose/episode checks remain separate and unchanged. The old
unscoped generic fallback is no longer accepted.

## Cooldowns And Charges

Protocol78 replaces mutable-slot SyncAbility values with AbilityResourceSnapshot:
GameplayActionScope,seat,sequence,stable hero ID,ultimate meter and the complete
HeroKit.AllAbilities ID/cooldown/charge set. Both role abilities travel even when
one is inactive. Reworks keep their stable IDs; role order does not identify state.
The receiver checks current world/epoch and the complete matching kit before any
resource mutation,then rejects duplicate/older per-seat sequences. Unknown,missing
or duplicate IDs reject the whole snapshot,not just one entry. The bounded format
supports up to8abilities and existing FixedString64Bytes IDs; expanding those limits
requires an explicit contract/version change,not a silent truncation.

The locally owned live-round kit still cannot have predicted cooldowns lowered or
charges refunded by a lagging host snapshot. Observers and intermission accept
authoritative correction. Ultimate meter remains host-owned. Resource application
does not change role,cast skills,restore active-effect durations or replay visuals.
HeroKit's legacy slot-based ApplyNetworkSnapshot remains a direct local helper;
the wire uses the identity-based route in MatchRpc.AbilityResources.

## Held-Aim Presentation

Protocol66 appends the dominant held slot,stable ability ID,elapsed hold and hold
token to the existing accepted pose stream. No target position travels in that
state. Ranked,casual,custom and spectators use the same route and authority gates.

- AimPoseAction names an existing body clip. CharacterAnimator consumes IsAiming
  on local and remote bodies; neither clips nor bone transforms need a new RPC.
- PresentAimBody/EndAimBody own shared body props or tells. These hooks are
  presentation only: never cast,spend resources or resolve hits inside them.
- PresentAim/EndAim remain private aiming guidance. Do not move destination
  markers into the body hooks; peers should not receive hidden targeting intent.
- The system ends presentation on cancellation,disable,reset or input-blocking
  phases. An unrefreshed remote hold expires after0.75s; fresh state can renew it.
  Cast/ultimate messages close only their corresponding slot/hold token, so an
  older pose cannot restore a consumed hold or erase a newer one.

Tokens are scoped to the movement epoch and advance on each hold. Pose ownership,
epoch and ordering checks run before presentation is accepted. Replicas tick kit
clocks but never turn the received hold/release into input or another cast.
Keep shared body hooks independent of private target guides during later reworks.

## Held Interactions

Protocol70 carries Interact in bit3 of the existing accepted pose intent. Use
CharacterMotor.InteractionHeldForSimulation for gameplay hold clocks: local input
for simulated bodies, accepted host input with a0.5-second lease for remote bodies.
Movement-epoch changes clear that lease. Do not authorize an outcome from the
client's visual progress byte or a claim that its hold finished.

Root escape preserves earned progress on release; plant pulling resets on release
or loss of reach/CanAct, using the existing durations. The host advances and completes
both clocks. Scoped completion notifications are hints, not permission to skip time.
New held interactions should reuse this input ownership boundary while specifying
their own target lifetime and reset rules. Client presentation remains separate.

## Prepared World Recovery

Protocol85 scopes the current familiar seance's recovery to GameplayActionScope,
accepted ultimate phase and stable hero/ultimate IDs. Protocol86 replaces wall-clock
expiry with the host round clock and remaining simulation life, retaining the same
178-byte maximum and refusing restoration outside an active round.
Its178-byte bounded envelope rejects malformed payloads; application waits for the
matching body/kit/companion without consuming missing state. It does not replace an
active same-phase effect, revive a completed phase or interrupt a new introduction.
The accepted-phase callback covers immediate activation as well as existing windup.
No clip, pose, field art or authored duration is encoded as identity. Retired
possession movement is separate; a genuinely new familiar mechanic still needs an
explicit state contract, not reuse of the seance restorer by accident.

Protocol80's UltimatePhase header carries the host-sealed cohort duration. The host
still derives the longest authored introduction; receiving peers must not derive
it from whichever kits have loaded locally. Missing actors wait inside that same
host boundary instead of classifying a longer introduction as already expired.
The44byte header validates duration as finite,(0,30]seconds; protocol81 extends
the original73byte commit as described below. The wire enters SharedUltimatePhase.ReceiveTimed. Legacy internal
Receive remains a local-probe wrapper,not the authoritative network path. No authored
duration or presentation was retuned. A new range beyond30seconds requires a contract
decision rather than bypassing the bound.

Protocol81 adds bounded hero and ability IDs to UltimateCommit and uses the existing
GameplayActionScope for requests,including the body movement epoch. Host acceptance
checks both IDs and current scope before reservation,and stamps the accepted kit's
identity. Familiar anchoring preserves it. Preparation waits for the correct kit;
execution refuses a kit that changed after acceptance. Empty identity is supported
only by legacy direct local probes; the wire rejects it. Cosmetic model/clip/name
changes do not rename these IDs. Hero registration validates hero-ID capacity too.

Each commit has77fixed bytes plus two UTF8 ID contents(up to61bytes each),max199;
request scope adds16(max215),and the44byte cohort header with4commits is at most840.
Readers reject oversized/truncated IDs and trailing payload. No phase timing,
casting resource rule or authored presentation was redesigned. Actual peer/ranked
qualification is separate from the bounded codec and matching-kit native cases.

Introduction view construction also waits for the matching visual and cached
preparation result. Normal match loading runs existing grounded-clip preparation
behind its curtain; late views may prepare one missing entry per frame. This never
extends the host cohort deadline or makes gameplay depend on a rendered view.
Known unsupported rigs keep the existing fallback and warn once instead of cloning
every idle frame; see [loading ownership](LOADING_AND_PERFORMANCE.md).

Protocol76 scopes requested pause/speed to match,round and sequence. The host sends
the requested rate after SyncWorld on the same reliable stream,including ordinary
recovery outside a cinematic phase. Receivers reject stale/malformed envelopes;
spectator requests retain their existing role check and independent peer ordering.
Read PresentationClock.RequestedScale for resume state,not Time.timeScale: local
Hitstop is temporary presentation,not the authoritative speed. Unchanged refreshes
must not cancel hitstop; a changed rate during a hold applies when that hold ends.

The prepared-effect adapter supplies centre,preparation and remaining life through
CapturePreparedWorld. RestorePreparedWorld restores only that state and returns
true when a new preparation needs its body/FPP pose resumed. Zero clocks mean
authoritative empty state: release the old root/effect. Keep authored full timeline
lengths and seek elapsed time; do not rebuild a shortened "fresh" performance.
The shared layer owns scoped delivery,ability-ID matching,generation deduplication,
clock aging,prediction/cohort freshness and preparation-pose playback.

This compact route is for a single centre plus clocks. A new mechanic with other
shared data still needs an explicit versioned state contract. Do not overload
unrelated fields or pretend arbitrary mechanics fit this shape. Existing persistent
world collections use WorldEffectSnapshot; their broader extensibility is still open.

## Persistent Effect Identity

Protocol71 adds InstanceId to persistent fields. Ordinary accepted initial casts
call HeroAbility.AdoptAcceptedCastEvent on the host, confirming owner and observers;
commands keep the original birth identity. Activate/reset clear the previous token.
An effect-owning ability can override OnAcceptedCastEvent for an immediate object,
or read AcceptedCastEvent when a committed windup later creates it. Keep this hook
free of casting, resource spending and presentation replay.

Paete's plant uses that token, carries it through Capture/Restore, and requires it
on the match/round-scoped PlantPulled message. Bounded per-seat retirement floors
prevent a removed lifetime returning through delayed installation or recovery.
Cosmetic swaps do not affect the token. Other effect kinds, multiple objects from
one cast and shared-ultimate cohorts still need their explicit lifetime contract;
adding this field does not silently give every existing effect an identity.

Protocol74 adds a bounded TargetMask to the world-field payload. Currently only
Sentry uses it,with its owner's bit forbidden. Capture keeps the host's selected
seats; recovery does not choose new targets from current distance. An empty mask
means no targets,not permission to infer them. Missing bodies bind once when their
seats install,and the original mask survives recapture in the meantime. Restoring
presentation cannot perform another catch,pull or root,even if authority later changes.
This fixes snapshot recovery; protocol82 below adds live delivery. Actual delayed
peer/staged-scene acceptance remains separate. New effect data needs its own semantics.

Protocol79 fixes sentry cleanup ownership. The Paete ultimate tracks exact fresh
instances and adopts recovered instances through the existing owner-specific
IWorldEffectBinding pass. Reset retires only those references,not every matching
effect type in the scene. An unused kit reset must not affect another caster.
Apply that ownership rule to new persistent effects; reconstruction must restore
their cleanup binding as well as their picture. This does not fix fresh-cast target
selection or establish a shared-ultimate lifetime token for every effect kind.

Protocol82 supplies live sentry target masks. HeroAbility.AcceptedUltimatePhase is
a distinct identity space from ordinary AcceptedCastEvent. Shared execution passes
the accepted cohort ID,adopts it after activation,and keeps it through windup.
Immediate effects use OnAcceptedUltimatePhase; deferred spawns read the retained
property. Activation/reset clear it. Paete captures it into the tree's InstanceId,
which also survives world recovery. Its identity is match/round/owner/cohort; this
assumes one sentry per caster/cohort. Multiple objects need an additional explicit
identity contract rather than blindly reusing that key.

The25byte SentryTargets message follows a reliable world header,comes only from
the host,and validates scope/owner/mask. A bounded pending set waits for local tree
birth; overflow requests normal recovery. Live lookup uses the field registry.
Replicas do not choose nearby victims or execute catches. Late bodies bind once;
snapshot state wins over a delayed birth message. Missing selection requests recovery
after1s; fresh late catch feedback occurs once,while restoration does not replay it.
Attack timing,placement and authored visuals remain unchanged. This is live target
delivery,not a change to the staged introduction's local target presentation.

Transport lifecycle is separate from ordinary phase cancellation. A fresh messaging
binding clears received cohort cursors and pending ultimate requests so the same
still-active host cohort can be received after reconnect. Preserve the host sequence
because existing effects may own earlier lifetime IDs. Ordinary Cancel still rejects
same-transport duplicates. Local session stop/disconnect cancels ultimate/halftime/
arrival owners and hitstop before restoring normal speed; another peer leaving the
host does not run that local teardown. No wire change is needed for this cleanup.

## Victim Feedback

Host-only outcome loops must announce existing contact presentation through the
shared match-event route. UltimateImpact reuses the current HitFeel camera hold,
punch, chromatic pulse and vignette; it changes only the view following the victim.
It does not apply status, modify global time scale or add a second effect on the
caster. Flair messages carry match/round scope in protocol72. Derive the accent from
the actual kit identity, not a cosmetic body's roster index.

## Companion Bodies

Protocol90 (HERO-10 v3, Phaister's VOODOO DOLL; Nemu's KURO PLAYS is the same kind of body).
A companion is a body that is not a player, in a companion seat: `Core.CompanionSeats`,
`PlayerCount + owner` (4 to 7), at most one per player. The HOST owns it: it spawns it
(`Abilities.VoodooDollBody.HostSpawn`), runs its brain (an Astig `AIController`) and
resolves everything it does. It is never in `RoundDirector.Players`; `Companions`,
`Bodies` and `BodyAt` are how the few sweeps that must see it (tags, shoves, a slipper's
body blocks, bot tag targets, flair seat lookups) find it.

- Existence: `CompanionSet` (host to all; to one peer inside `HostSyncPeer`) lists every
  live companion (seat, kind, position, yaw) scoped to the presentation match and round.
  A receiver keeps or builds a brainless replica (`VoodooDollBody.Spawn(..., brain:false)`)
  for each listed seat and removes every other companion with its slipper. A list from
  another match, from an earlier round than the receiver's, or from a peer that is not the
  host is ignored. The host sends it on every change (`RoundDirector.CompanionsChanged`:
  spawn, `EndRound`, `ResetForNewMatch`, `Clear`, its owner's `Unregister`).
- Body state rides the existing seat-keyed routes, which admit companion seats
  (`MatchRpc.ValidBody`): SyncUnit (pose, statuses, the voodoo snapshot), Teleport,
  PlayAction, ThrowCharge, SyncSlipper and SlipperPose. `_movementEpochs`,
  `_unitPoseSerial` and `_slippersBySeat` are sized `CompanionSeats.BodyCount`; the host
  broadcasts only LIVE companions' slippers, so an empty seat never forces a rescan.
- Stays four seats wide (`ValidSlot`): abilities, cooldown receipts, timed kits, emotes,
  scores, seat ownership and departures. A companion has no kit, no peer can claim its
  seat, and `MatchDirector.AddScore` pays its points to its owner before any Score
  message exists. Impact and Carry target a peer-simulated body; a companion is always
  host-simulated, so the host applies them directly.
- Its slipper is a fifth `Slipper`: seat of origin and owner = the companion's seat,
  armed into its hand by the host when it attacks, parked (inactive, owner -1) when it
  defends, destroyed with it.
- A tag on a companion (`RoundDirector.ResolveTag`) stuns it five seconds where it stands,
  pays nobody, credits no sabotage and raises `CompanionTagged`, never `Tagged`. Every peer
  shows the grey stitched X from the replicated stun (`CharacterMotor.IsTagged`), with no
  message of its own (`Visual.VoodooDollPresence`).
- Protocol91: the `Score` message carries the scoring BODY after the event (its own seat, or
  a companion's seat whose owner is the paid `slot`). The host raises
  `MatchDirector.CompanionScored` in `AddScore`; a client raises it in
  `ApplyNetworkScoreEvent` only when that body is a companion owned by `slot`. It is
  presentation (the +100 over the doll in its owner's colour), never a second payment.
- Protocol92: Phaister's introduction is 6.35 s (the shared phase every peer derives) and
  the doll's `BodySpeedScale` is 0.5 (`VoodooRules.DollSpeedScale`). Protocol93: the
  introduction is 6.0 s (it ends on the doll's stare).
- Presentation owned by the body on every peer: its nameplate (`CharacterNameplate` reads a
  companion seat: the owner's colour, the body's own name), THE CIRCLE as a portal that shuts
  3 s after the hand-back, and the marionette control over its head with its wires
  (`VoodooSkyCircle`, `MarionetteControl`). A rejoiner builds it with the portal already shut.

## Compatibility And Checks

Protocol73's VoodooBodySnapshot carries DRAINED/HEXED timers and mark/reach state
inside SyncUnit, before the existing exact aim tail. It validates kinds,seats and
finite bounded clocks. A reach's completion result comes from that caster's own
snapshot, never another body's possibly later packet. Status application precedes
the authoritative resource correction, so a fresh depletion cannot erase that pool.
RecoveryBlocked updates immediately on received/host Drained application.

The body's gameplay tick, reset, cleanse, passive speed and HUD are wired (HERO-10,
`VoodooBodyWiringTests`); it still needs
the kit's final integration. Preserve the owner's no-added-fatigue depletion rule;
Hexed's screen effect stays victim-local. Mark/doll gameplay entity lifetimes need
their explicit contracts. Two native codec/receiver checks do not qualify the whole
unfinished kit or actual peer behavior.

Phaister's curses (HERO-10 v3) cast through the existing routes with no new message.
CURSE: DRAIN (`phaister_skill2`) and CURSE: HEX (`phaister_skill2d`) are HostConfirmed.
The owner's press and the host's acceptance both require `PhaisterHeroKit.ReachTargetFor`
to find somebody; the host picks the target again from its own bodies and starts the
body's reach (`HostBeginVoodooReach`). From there the body owns the reach and the mark
(protocol73 state). Every peer settles the cast when the caster's reach ends: through
`VoodooReachEnded`, or, for a reach it never saw start, from the caster's replicated
`VoodooReachSucceeded` 0.75 s after the reach's own 2 s. A broken reach takes half the
cooldown off what is LEFT on every peer, so the owner (whose cooldown a host snapshot
never lowers) and the host agree. HEX's recast is a reactivation on the existing cast
route: valid while the ability is active (reach plus mark life) and `ReactivateReady`
(an armed Hex mark of this seat); the host sets it off with `HostDetonateHex(source)`
and no resource changes. The changed cooldown, duration and reactivation metadata are
in the skill fingerprint, so mismatched builds are refused at connect. Presentation
reads replicated state or accepted casts: the reach pose, the belt, the wring, the stab;
HEXED's phantom slippers are victim-local. OPEN: a rejoining Phaister's live HEX (a
duration is not restored, so she cannot recast a hex she placed before the drop) and
actual peers.

Protocol75 gates the newly active status/movement semantics so an older admitted
body cannot leave received curses inert or permanent. Replicas advance received
clocks and clear expired recovery suppression,but only the host resolves a waiting
mark/reach into gameplay. One new native received-state/authority check passes;
the actual peer/doll-entity contract is still separate from body-state transport.

Protocol77 adds match/round to SyncUnit by replacing its bare movement epoch with
GameplayActionScope. Reject another world's packet before advancing the body pose
serial or applying status/resources. A fresh body's empty cursor is not permission
to accept an old round. Base payload is235bytes with Voodoo and the empty aim tail;
longer aim IDs retain their existing bounded encoding. Reliable handovers and normal
poses still share the serial,and current-world newer movement epochs still install.

Persistent status pictures belong to a body-owned presenter reading replicated
status,not exclusively inside a host-only victim loop. StatusBodyMarks and
PhaisterStatusPresenter are current examples. Joining peers need no replayed hit.
Make spawn idempotent,follow refreshed live status and clean up on disable/despawn.
When every peer presents a status,play its local cue once; do not relay it again.
Missing presentation assets must not keep allocating or obstruct gameplay outcomes.
Rooted also uses StatusBodyMarks to reconstruct its existing PaeteRootCoil without
a sentry's inferred target list. The sentry shares that owner and may supply its
facing origin later, once; it must not turn the player every frame. Body disable
retires the owned restraint silently, while normal unroot keeps its authored release.

The connection fingerprint includes stable identity,recovery capability,shared
skill metadata and intro durations. It excludes cosmetic data,live timers and
current role. Effect-internal rules and arbitrary code are not automatically hashed;
version genuine wire/semantic changes and ship matching clients.

These routes serve ranked/casual and LAN/online matches. Do not fork authority by
queue type or change rating,result,leave or device-pool rules as part of art work.
Check the behavior actually changed: admission,prediction/confirmation,owner and
observer presentation,spectators,late join,interruption and cleanup as applicable.
Reuse unaffected evidence. Cosmetic edits do not justify repeating every network
suite or character film. Native/real-peer gaps remain explicit until exercised.

## Boulder Slipper Compatibility

Protocol108 appends Concussed affinity4 while preserving Normal0, Fire1, Electric2
and Frost3. The host imbues a real held slipper and publishes the existing reliable
slipper state. Held/drop recovery retains this affinity; ordinary throw carries
it and impact consumes it. Round reset clears unused charges. Accepted observer
casts cannot mutate it. Existing status snapshots carry2.5second Concussed with
0.25movement scale. No new packet layout, timer or separate rock is introduced.
Native delivery/recovery checks do not qualify actual peers or reconnect.


## Slipper Contact Lifetime Compatibility

Protocol109 excludes inactive can/bodies from host slipper contact and enforces
existing flight/airborne ceilings before contact early returns. No duration,
active collision geometry, scoring or packet layout changes. This resolves the
hidden tutorial can's indefinite rebound. Native proof is separate from actual
matching-player/peer qualification.


## Continental Drift Compatibility

Protocol111 appends world-field kind15 using existing origin, unit forward,
radius-as-half-width, first-scale-as-reach, duration and remaining fields. Five
host-owned bands run0.30s apart; restore skips past outcomes and observers cannot
apply status. The shared atomic world snapshot generation/match/scene/event gates
and round retirement apply. Replay is render-only. No packet layout changes.
The accepted shared ultimate still owns windup/release; cost12, stable ID retained.
Focused native recovery is not actual matching-peer qualification.

Protocol112 combines the checked Continental Drift111 contract with Haunted's appended SyncUnit timer. Native Haunted evidence is the pre-merge owned candidate; current receiver logic is unchanged by the independent cascade. Actual protocol112 peers/player remain unqualified.

## Kuro Haunt Compatibility

Protocol113 replaces Nemu's old seance with15point host-owned sequential Haunt.
The existing FamiliarEffect packet carries current ground position/remaining
round-bounded clock at10Hz and terminal zero. Scope/identity/phase checks remain;
within-phase round-clock ordering rejects old/duplicate movement. Completed
lifetimes cannot revive, including completion during windup. Recovery never
replays a hit/resource spend and live movement does not cancel new basic skills.
No wire layout changes. Native companion/receiver evidence is separate from
actual-peer qualification and remaining Haunted nearsight/audio.
[Evidence](reports/feedback-2026-09-30/nemu-kuro-haunt.md).
