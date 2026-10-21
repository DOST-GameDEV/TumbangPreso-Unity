# Ordinary pickup reach through solid street geometry

Date: 2026-10-03. Baseline `73dab428a` on competition-laptop-gameplay.
Status: original native six-case gate reproduced two tall-wall failures and
passed four controls. The first candidate passes all ten cases, including four
existing slide controls. Main audited exact frozen worker/primary source hashes
and terminal preservation before approving publication.

## Proposed contract and source distinction

Ordinary pickup currently checks ownership, loose state, an empty active hand
and `PickupRadius`. It does not check solid street geometry, so a public
`CanBeGrabbedBy`/`HostGrab` call can retrieve within that radius through a tall
wall. The existing slide rule explicitly refuses a shoe through a solid wall;
`RetrievalSlideTests.ASlideCannotCollectThroughAWall` explains that standing
against a wall must not fish a shoe from the next street. Ordinary pickup's
Design table only states radius, so this investigation must establish that
sharing the solid-wall reach constraint is justified before changing gameplay.

Preserve unobstructed short reach, a low kerb, trigger-only surfaces and other
players. A player before a real wall must not hide that wall from the reach
query. The current slide helper accepts a first CharacterMotor/Slipper hit
without inspecting geometry behind it; blindly reusing that first-hit helper
would leave that case incorrect. No broad collision/raycast framework or hero
mechanic changes are proposed.

## Frozen original gate

Run exactly `TumbangPreso.PlayTests.PickupStreetReachTests`, six cases.
Expected original: two causal failures and four passing controls.

- Tall solid primitive wall, with and without another player before it:
  eligibility and authoritative HostGrab must both refuse, leaving the shoe
  loose and the hand empty.
- Clear street, a 0.1 m kerb, a tall trigger curtain and another player alone:
  public eligibility and HostGrab still connect.

The fixture begins a real local round, supplies an owned loose shoe 1.2 m away,
and invokes the shipping public eligibility/relationship methods. Motor and
shoe updates are disabled to hold geometry constant. `Physics.SyncTransforms`
and real ray/line queries verify the wall is present, the intended collider
ordering is correct, and the wall remains behind any player occluder. The
occluder's real CharacterMotor/CharacterController is narrowed to 0.12 m radius
for deterministic ordering; no source actor/art is changed. Both hooks reset
the world and restore provider, launch and stats state.

Fixture SHA256:
`04dd6388666595ae4a285c52a0055095ff1a265a167423ceecaabc8ef3ebc369`.
Metadata GUID: `75d51c95a23e4aa5a0750ebfb335be97`.
Unchanged working Slipper SHA256:
`adb50ce5e405621e69275db25bfc8a453582db7981a9c3e2ef124be781620497`.
Unchanged working CombatVerbs SHA256:
`de992f761fb539a265f2d0103922658e09ea44bc93c622b67926683c304e66ad`.
Exact Git original blobs in `73dab428a` use canonical LF bytes: Slipper
`cbe4c0eeb9facc813a49021be8bf0b9d74aa88ca5745345d0f0aa1c0e52eb42e`,
CombatVerbs `8ae8ccbeceae34d018f188043c32641f39bf445360cbff47dc96637d6c2aead9`.
Normalizing each unchanged working copy's CRLF to LF gives its exact Git hash.
Use these canonical byte hashes if Main extracts an original from Git, rather
than asserting the mixed-line-ending working-copy hashes against Git output.

Main owns original snapshots, jobs and preservation. Stop at fresh exact
six-case XML and terminal receipt. Setup or geometry-probe failures do not
establish the pickup defect. Do not change assertions or introduce a source
patch before the original gate and review. Any shared reach correction needs
the existing ordinary-slide wall and resource-refusal controls alongside this
fixture; the direct pickup cases do not qualify slide prediction/sweeps.

This is a proposed short-reach geometry contract, not a new authored hero
design or demonstrated all-map hardware/operator result. Raised ledges,
nonstandard colliders, actual map retrieval and low-platform tuning remain
outside this primitive fixture and must not be silently reinterpreted.

## Correction and native evidence

`CanBeGrabbedBy` retains its existing eligibility/radius gates and asks the
shared street query. CombatVerbs delegates its existing slide predicate/sweep
query to that helper, preserving the passed origin, probe heights, all-layer
mask and ignored triggers. The query examines all hits past ignored players
and shoes. A 32-entry NonAlloc buffer uses a complete RaycastAll fallback if
full, following the existing shove-route pattern; scratch references clear in
finally. There is no new collision layer, sort, force-equip gate, kit or asset
change. The live Design pickup rule now states the solid street-reach gate.

Frozen candidate working hashes: Slipper
`6b49fb0a352297f84e0e36542fdd89df1c7eb70c8fe536ffd8ae1b54c4c28d33`,
CombatVerbs `88b221c96d4701d0db07128114b997162bedbf80a95dd27af951092d1795e531`.
The six-case fixture and metadata remain unchanged.

Main's `qa-a/Logs/pickup-street-original6` gives both expected wall failures,
with/without a player before it; clear, low kerb, trigger and player-only
controls pass. `qa-a/Logs/pickup-street-candidate10` passes all six unchanged
cases plus existing ASlideCannotCollectThroughAWall,
NeitherPathStartsASlideAtATsinelasBehindAWall,
TheHostGrantsASlideAtALegalLooseTsinelasAhead, and
ARefusedSlideHandsBackTheCooldownTheStaminaAndTheCommitment. No fixture repair
or native retry occurred.

[Original XML](native-original/tests.xml) and [receipt](native-original/job-receipt.json)
retain exit2 and both failures. [Candidate XML](native-candidate/tests.xml) and
[receipt](native-candidate/job-receipt.json) retain exit0 and ten passes. Both
guards are terminal, preservation completed and no lease held. Raw Git versus
working-copy newline provenance was clarified before native execution; no
fixture/source semantics were changed for that preflight.

This qualifies the primitive public pickup boundary and the four named actual
slide controls. Buffer saturation and arbitrary authored geometry remain
source-reviewed limits, without native/all-map/performance/hardware/transport
claims. It does not establish competition readiness.
