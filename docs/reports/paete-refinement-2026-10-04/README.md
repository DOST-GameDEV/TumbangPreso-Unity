# Paete animation and mutual vine pull

Owner-requested scope on October4: refine the existing attacking/defending
animations and let Liana Leap attach to a player, pulling both toward a meeting.
The [research and preservation plan](plan.md) records the source/reference review.
This work preserves the existing plant, authored braided vines, character models,
palette, attack cooldown and13m/s wooden-slipper launch.

## Current integrated candidate

Protocol151 integrates the shared explicit owner-acknowledgement fix and mature
five-second automatic lobs. The earlier experimental recent-pose trail is removed;
there is only one acknowledgement/correction path. Remote replica grace is bounded
and separate from movement speed. Plant charging retains a0.6s window independent
of the shorter reload.

Final source has14/14 headless packet/input checks,5/5 graphics gameplay/animation
checks and20/20 Core cases. The combined attempt first hit OOM with no result XML;
that failed run is retained, its orphan compiler was retired, and the split runs
passed without another OOM. The internal Linux build passes130.6s and packages
254files/2601722190bytes. Its QA checkout has no Git stamp, so the external manifest
and source delta identify it; it is not a certified release package.

Three matching151 processes pass the150ms one-way delayed case:4.176m caster,
1.044m target,0.78m final separation on all peers, zero backwards steps on the
controlling peer, and observer refresh. [Final delayed evidence](peer151-delay150/evaluation.json).
The [final direct control](peer151-direct/evaluation.json) also passes with the
same4.176m/1.044m travel and0.78m stop gap. All final traces reach their bounded
observation window; cleanup/exit receipts are retained. Windows, WAN/loss/disconnect, replay, sound
and human acceptance remain separate.

## Behavior

- Bakya Bloom stores its windup before the wooden slipper actually launches,
  releases forward on that frame and settles afterward. It retains the same meshes.
- Thorn Harvest reaches for0.45s before snatching, starts pulling at0.65s and
  takes1.1s to bring the slipper home. The existing3s construct remains.
- Liana Leap selects the nearest unobstructed eligible player within its existing
 8m reach. The target moves20percent of the available gap, capped at1.25m and
  reduced by existing displacement resistance; Paete covers the remainder.
- Both move through their normal CharacterController collision path. Capsule
  clearance ends the pull, with a small cosmetic contact response. No damage,
  tag, stun status or score is added. Terrain fallback retains its established reel.
- Contact, collision stall, new impact/carry, disabling, status, teleport epoch,
  role, kit or round change release the constraint. New impacts retain their own
  velocity. Vines follow the player's body and retract at termination.
- Protocol149 carries the host's scoped target/endpoints/deadline. Only the owner
  simulates each body. Foreign, stale, duplicate and wrong-incarnation states are
  rejected. Predicted presentation retains the existing tell; it does not select
  or move a victim locally before the host decision.

## Evidence and honest limits

[evidence.json](evidence.json) retains named native results and original failures.
The first integrated run exposed an unsupported host-confirmed windup mode. The
fix preserves the existing predicted tell and places movement behind the scoped
host result; it does not weaken the networking validator. Initial timing-fixture
failures were deferred collider destruction and held-prop LateUpdate order;
only fixture yield order changed before the same checks passed.

The direct prototype measured4.176m caster travel,1.044m target travel and0.78m
minimum separation. Actual input integration subsequently passes mutual movement,
contact stop, terrain fallback, obstacle stop and interruption with new-impact
preservation. A packet roundtrip measures exactly81bytes. Native receiver checks
cover foreign sender, target epoch, round, duplicate start, old start after end,
and owner-only movement. These are local receiver/physics checks, not a second
live network machine.

Actual1280x720 plant/defense and player-latch frames were inspected. The public
comparison is silent and does not qualify sound. The prototype shows connected
arm strands and bounded mutual movement at contact; human taste remains separate.

The coherent local sweep passed10/11, exposing an actual offline-teleport
cancellation defect. Offline Teleport does not change movement epoch, so epoch-only
cleanup was insufficient. Teleport now cancels immediately, and epoch adoption
also cancels before replacement. The original failed test plus actual input and
receiver controls then passed3/3 on final source. Core Paete rules passed20/20.
No new OOM occurred in those initial runs. These focused passes are not a full-game regression pass.

At this initial checkpoint, matching protocol149 live peers, Windows player,
latency/loss/reconnect scenarios and human visual acceptance were unqualified. The earlier protocol148 two-machine result does not qualify this change.


## Actual peer follow-up

A Linux player build passed with the shipped gameplay source and an opt-in
extension to the existing personal-state probe. Three separate processes on
loopback (host target, remote controlling caster, observer) passed:
[direct result](peer-direct/evaluation.json). All three agree on4.176m caster
travel, approximately0.98m target travel and0.844m final separation. Observer
kit replacement was followed by a live restored constraint. All processes exited0.
This is actual transport delivery, not separate hardware or WAN certification.

Adding150ms one-way delay on the controlling client's link reproduced a real
failure: the host mistook delayed replica updates for a collision stall. The
caster stopped at2.8m, leaving2.36m between the bodies on all peers. The original
[failed evaluation](peer-delay150-before/evaluation.json) is retained.

The correction gives remote replicas bounded measured-RTT grace while retaining
immediate local obstacle checks and the same velocity/distance rules. Grace
extends the deadline, not travel speed. The short ability presentation timer no
longer owns cancellation of the independently bounded movement constraint.
Four native input, obstacle, teleport/kit and receiver checks pass. The subsequent packaged results and final integration are recorded above.


The first grace-only protocol150 retest revealed a second defect rather than
passing: ordinary delayed own-position echoes rewound the controlling caster.
Its final travel was0.28m while the target reached1.044m; the
[failed three-process traces](peer-delay150-echo-before/evaluation.json) retain
the repeated reversals. A scoped recent-pose trail now distinguishes those
known echoes during an approved tether from genuine corrections. A new native
case proves delayed matching positions do not rewind the pull while off-trail
and forced corrections still win. That case plus input/receiver controls pass3/3.
That experimental workaround passed matching delayed peers, but was superseded
by the shared explicit acknowledgement fix before this follow-up was published.

The broad gameplay-clock audit reports13 findings in six other source files.
Those findings were in unchanged baseline files at the time of that audit.
This is not a full-project audit pass; no audit exemption was added for this change.
