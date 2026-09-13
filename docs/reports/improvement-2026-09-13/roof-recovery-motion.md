# Roof fall and get-up motion, active work

Owner requested believable struggle/effort and recovery in the current map pass,
preserving cute blocky people. Existing fall, mash and independent tag mechanics
already passed5/5 in roof-all-fences-v1, covering both modes and all4fences.
Animation acceptance is separate and remains OPEN.

## Observed baseline

Logs/roof-recovery-motion-before-v1.xml passed1/1 and captured176paired body/owner
frames at ordinary recorded times. The body starts die at1.648s, switches to
pick-up at3.946s, and returns to idle at4.246s. Inspected frames45 and75 plus
owner75. The body lies motionless face down, then pops almost upright when
pick-up starts. There is no visible struggle, hand brace or staged weight transfer.

The cause is concrete: the supplied die clip animates the root bone to a prone
rotation and small height offset. The pick-up clip has NO root translation or
rotation tracks. Switching resets the root toward its default pose while merely
playing a picking gesture. Slowing that gesture cannot create a floor-to-standing
sequence. Original GLB channel inspection confirmed those binding differences.

The original camera deliberately shows the fall in TPP, then restores FPP after
recovery. Preserve that accepted view behavior. A new FPP arm struggle is not
visible while that view is active; verify clean owner-camera return instead.

## Planned correction

Author real serialized recovery clips per approved rig. Preserve original die,
pick-up and emotes. Use the existing fall to establish a compatible landing, then
an explicit bracing progression: raise head/shoulder, plant hand, bring the legs
under the body, pause in a low supported position, push back onto the feet.
The final movement must end within the EXISTING MinTripDown=.35s. Longer struggle
is shown during the actual mash phase, not by adding another control lock.

Drive bracing from accepted recovery progress (TripLeft/TripTotal/MashRemoved),
smooth the visible response without changing gameplay time, and allow small
effort movement while waiting. Use the already replicated state for observers.
The final stand samples its normalized time from the remaining real trip time,
so a delayed snapshot cannot leave the body behind restored control. Keep a
fallback for rigs without new clips. Sharing a physical get-up across identical
trip recovery is justified; unrelated casting/throwing must not reuse it.

Bake and inspect actual body contact through the whole sequence, retaining the
approved geometry/bones. A new hand or knee must not pass through the floor; the
character must not float or slide into nearby players. Verify immediate/slow/no
mash, repeat falls, independent tag overlap, interruptions, all18people, both
modes, ordinary-speed owner/body captures, rejoin and camera return.

## Related current work

Swimming is separate: v3functional2/2 passed, v4posture/grip authored but not yet
visually qualified. Water-route audit has current candidate support sampling
correction in runv4. Finish that and recheck current swim motion before expanding
water rendering. This plan survives compaction; no recovery clip implementation
has been applied at this document's creation.

## First recovery implementation, awaiting review

RecoveryAnimationAuthor now creates landing, bracing and stand resources for the
retained rigs. Landing copies the existing fall; original assets remain intact.
Bracing/standing explicitly animate root, torso/head/arms/legs, with rendered-mesh
contact fitted per sample. CharacterAnimator selects resources for physical trip
recovery, samples bracing from accepted trip progress, and standing from actual
remaining MinTripDown. Physics/timers and independent tag clocks are unchanged.
GameBuilder ensures resources are authored. Current author log:
Logs/roof-recovery-author-v1.log; session36291 at launch. NOT visual acceptance.
Next capture and inspect contact, posture, interruptions and owner camera return.

Water-inclusive routev4 passed1/1 for both modes: each5976body nodes,5958connected,
6285clear slipper rest samples,0unreachable. Actual pickups include floating pool
stock and all outer edges/services. Logs/sa-bubong-water-routes-v4.xml and CSVs.
No runtime geometry or pickup-radius changes were used to satisfy the audit.

V2recovery motion1/1 passed. Actual bracing/stand contact measures bottom=.100m
on support=.100m. Palm approaches support while accepted presses progress, and
owner view returns to FPP. BUT the original copied landing clip penetrates up to
.896m during its rotation, revealed by the new trace. V3 now fits landing contact
through the original gesture too, without modifying the original die/emote clip.
Authorv3 is running; collect before further imported/code edits. Afterward recapture
and measure the landing, then verify current swimming posture and full EditMode.

V3landing motion1/1 passed. Actual complete recovery trace has drawn-bottom minus
support between-.0002m and+.011m, replacing v2's-.896m penetration. Grounded recovery
now receives an explicit regression bound and nonzero sample requirement. The
bracing/stand remains within original mash/MinTripDown control timing. Original
people/models and die/pick-up source clips are preserved. All18roster and real
separate-process motion acceptance remain required; this trace is one Classic
body, not full roster proof. Current swim-v4 now records revised torso/legs and
raised right grip in both modes before broad Edit/check/commit qualification.

## Stable source checkpoint scope

Current swimming v5passed2/2 with both modes, actual entry/exit and floating
pickup, and normal-speed paired body/owner captures. Revised forward swimming
extends feet behind the body and derives held-arm placement from the measured
palm. Float depth is.9m; FPP and the held slipper stay above the surface. Water
rendering/contact wakes and full-roster/ability/network review remain open.
Sa author repeatability is0changed/2049rows; all4maps pass the geometry gate.
Core562, fullEdit516(before final v5pose) and14gating source audits passed.
New owner policy now requires focused tests, not routine full suites. Respect it.
Portable selected XML/CSV/images and geometry/repeatability are in
roof-swimming-evidence. Exact runtime DLL/player validation is still outstanding.

Approved18people and purple Kuro retained. Partial Home/Play logo UI and original
PDF/JPG/Figma sources retained. Six cat/aspin models with4exported actions each,
plus bird studies, exist outside Assets and are NOT integrated/approved animals.
The larger map/UI/animation/skill/graphics/network goal is OPEN. No Desktop build
was replaced. Keep implementing after this checkpoint; no handoff is requested.
