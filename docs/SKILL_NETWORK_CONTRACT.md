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

## Compatibility And Checks

Persistent status pictures belong to a body-owned presenter reading replicated
status,not exclusively inside a host-only victim loop. StatusBodyMarks and
PhaisterStatusPresenter are current examples. Joining peers need no replayed hit.
Make spawn idempotent,follow refreshed live status and clean up on disable/despawn.
When every peer presents a status,play its local cue once; do not relay it again.
Missing presentation assets must not keep allocating or obstruct gameplay outcomes.

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
