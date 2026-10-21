# Actual edge recovery, latest owner correction 2026-09-24

Owner: falling from Sa Bubong must show a person trying to get up FROM the railings,
not teleport to the middle. Recover at the actual railing. Lagoon/Bajau: falling
into water means swimming; press jump near a bridge to start button-mash climbing
up from that bridge. This corrects the earlier published fall-recovery interpretation.

Current code confirmed: LagoonWater.ReachedWaterAfterPlatformFall triggers
Respawn()+ApplyFallRecovery on reaching water depth, instead of keeping swimming.
Rooftop recovery source/state/animation/network input must be reviewed next. Existing
press-gated floor get-up cannot be relabelled a ledge-climb animation.

Priority: implement this actual gameplay correction now; preserve bakery research,
generated candidate and every remaining world sign/storefront task for resumption.
No source/asset changes to recovery until state/authority/animation plan is mapped.

Required outcomes:
- Sa Bubong: falling at an actual roof boundary catches the nearby ledge/railing,
  places hands/body convincingly below the lip, visibly struggles as presses advance
  recovery, then pulls onto a safe landing just inside that edge. No centre respawn
  in the normal recovery path. Use actual local geometry and fit the3Dcast.
- Lagoon: a real fall enters the existing swim, no immediate recovery teleport.
  Jump beside a reachable bridge/deck edge initiates climb; mash advances a visibly
  anchored pull-up onto that same surface. Swimming away from an edge stays swimming.
  Existing intentional stairs remain usable; do not force a climb everywhere.
- Host authority, remote input, controller mapping and bots need a coherent path.
  Distinguish edge-hang/climb from generic prone trip. Reset/cancel on round change,
  disconnect/body replacement/actual respawn; don't let stun/tag/pickup bypass it.
- Animation must communicate grip, suspended/swimming body, effort and pull-up,
  with hands at the actual edge, no floor-get-up pose in empty air. Match body/FPP
  presentation and viewer/replay states through existing architecture where possible.
- Preserve slipper-return semantics unless incompatible with swimming, and document
  any necessary correction. Existing water out-of-bounds/under-deck failsafes must
  not fire during valid swimming/climb. Exceptional off-world rescue is separate
  from ordinary recovery; do not mislabel it as successful physical climb.

Plan steps: inspect RooftopRecovery, LagoonWater, CharacterMotor inputs/status and
snapshot/replay ownership, CharacterAnimator recovery/swim, current tests. Define
shared authoritative progress with per-map anchors and distinct animation entry.
Implement rooftop anchor first, then swim-to-bridge initiation/exit. Author suitable
cast-fitted animation using existing voxel rig pipeline. Small focused behavior tests
and actual native animation views, then one two-peer path where supported. No broad
regression/capture-tool refactor or repeated unchanged tests.

Keep previous3e0d0c69a/2passed recovery evidence as historical; it tests the prior
now-superseded respawn/get-up behavior and does not establish this new requirement.
Do not mark either map's recovery complete until edge behavior and animation exist.

## Source findings and chosen integration route

Both map handlers explicitly call Respawn()+ApplyFallRecovery. CharacterMotor is
currently one sealed class, with driver-owned locomotion, host state authority,
episodic reliable mash requests and SyncUnit snapshots. ApplyFallRecovery merely
bypasses hero stun immunity; it has NO distinct replicated ledge state. Do not
reinterpret an existing timer value as a hidden animation mode.

Introduce an explicit edge mode/anchor/phase in a motor partial. Reuse the existing
press accounting/episode acknowledgments, but edge hanging does not passively time
out through the generic floor-trip guard. Host owns the constrained reach/hang/pull
trajectory for every seat, including remote owners; their ordinary movement packets
cannot override it. Owning client sends a jump-to-climb request validated at its
host-side current swim position against real map geometry. Mash requests keep the
existing rate/episode rules. End at a checked landing immediately inside that edge.

SaBubong source rails are atx+/-18.8,z+/-21.8. Actual top rail is localy.72plus
.085thickness above baseRoofY.1; collision top is nearly identical (.46+.30+.1).
Use real nearby rail bounds/placement, not a court-centre spawn. Lagoon public deck
BoxColliders named Continuous deck collision expose actual transformed rectangles;
find an adjacent edge and a supported landing, preserve intentional water stairs.
An entry reach/swim-kick phase prevents snapping from float depth up to a railing.
Hands and body then stay anchored through effort and pull-up. Need deliberate
out-of-world fail-safe separately, never use centre respawn for normal recovery.

SyncUnit has a192byte buffer and protocol50currently. Add explicit validated edge
snapshot fields and a seat-owned climb request, update protocol version consistently
if wire format changes; reject mixed builds. Use movement epoch reset when host takes
and releases trajectory ownership so stale client movement cannot drag the body off
an edge. Keep ordinary movement ownership outside recovery unchanged.

RecordedMatchClip already records root and every bone transform in pose tracks.
A new climb on the retained bone hierarchy should therefore replay via those actual
poses without inventing a separate replay timer/state codec. Verify this at the
focused animation/replay gate rather than changing the archive format blindly.

Animation authoring can reuse the rig discovery/palm-centre measurement in
RecoveryAnimationAuthor, but must produce reach/grip/effort/pull-up poses rather
than reuse prone clips. Tailor palm contact to each retained rig, preserve existing
characters/assets, and fit FPP communication. Bots can reuse existing mash behavior
once in recovery; swimming exit intent still needs a nearby-edge jump path.
