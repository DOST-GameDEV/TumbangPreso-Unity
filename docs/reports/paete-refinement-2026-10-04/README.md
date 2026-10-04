# Paete animation and mutual vine pull

Owner-requested scope on October4: refine the existing attacking/defending
animations and let Liana Leap attach to a player, pulling both toward a meeting.
The [research and preservation plan](plan.md) records the source/reference review.
This work preserves the existing plant, authored braided vines, character models,
palette, attack cooldown and13m/s wooden-slipper launch.

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
No new OOM occurred. These focused passes are not a full-game regression pass.

Matching protocol149 live peers,
Windows player, latency/loss/reconnect scenarios and human visual acceptance remain
unqualified. The earlier protocol148 two-machine result does not qualify this change.
