# Skill Networking Contract

Presentation is replaceable. Stable ability IDs and shared gameplay state are the
network boundary, not model files,effect class names,clips,palettes or cue names.
Only Paete currently has substantial VFX; other presentation remains provisional.

## Cosmetic Reworks

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
This fixes snapshot recovery; fresh-cast peer target convergence and actual delayed
peer acceptance remain separate. New effect-specific data needs its own semantics.

Protocol79 fixes sentry cleanup ownership. The Paete ultimate tracks exact fresh
instances and adopts recovered instances through the existing owner-specific
IWorldEffectBinding pass. Reset retires only those references,not every matching
effect type in the scene. An unused kit reset must not affect another caster.
Apply that ownership rule to new persistent effects; reconstruction must restore
their cleanup binding as well as their picture. This does not fix fresh-cast target
selection or establish a shared-ultimate lifetime token for every effect kind.

## Victim Feedback

Host-only outcome loops must announce existing contact presentation through the
shared match-event route. UltimateImpact reuses the current HitFeel camera hold,
punch, chromatic pulse and vignette; it changes only the view following the victim.
It does not apply status, modify global time scale or add a second effect on the
caster. Flair messages carry match/round scope in protocol72. Derive the accent from
the actual kit identity, not a cosmetic body's roster index.

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
