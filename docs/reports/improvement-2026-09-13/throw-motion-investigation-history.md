# Throw and Pektus motion review

This is an active implementation/qualification record,not a full game release.
The approved18 blocky rigs and source geometry are unchanged. Inputs/trajectory
rules are unchanged. Body preparation adds a backward coil,head compensation,
balancing off-hand and signed arm roll. Straight/left/right accepted releases
have separate signed follow-through. The FPP action begins its forward swing
immediately;it no longer re-cocks for0.14s after the actual throw.

## Evidence and corrections

- Body baseline0/1:exactly0degrees torso preparation on the prior runtime.
  Current7motion tests cover18body rigs,3grip framing cases and3signed release/
  cancellation cases across30/60/144Hz. They pass in fullEditMode499/499.
- Relay baseline0/1:accepted remote request did not apply windup on listen host.
  Current handler applies locally before relaying and checks seat ownership and
  finite values. This focused handler regression passes;actual processes pending.
- V1 motion capture sampled before LateUpdate,missing final body/hand pose. It
  also asserted preserved spin after a can hit legitimately consumed spin. Both
  fixture faults were corrected;no slipper-physics change followed that failure.
- V2b late-frame capture1/1,6throws,bothmodes. Rejected body arm lowering and FPP
  downward/eye clipping. V3 corrected body arm direction and solved elbow around
  a hand anchor,but FPP forearm looked disconnected. V4 reduces throw-only FPP
  angle and lowers framing;forearms now extend through the lower screen edge.
  Lunge retains its previous full FPP angle. Body is still a rigid blocky limb;
  this is intentionally not a new anatomical/wrist/thumb model.
- V4 capture1/1,6sequences,bothmodes,TimeScale1. Real timestamped videos preserve
  ordinary speed despite capture frame gaps. All12 MP4s are in
  Logs/throw-motion-v4-ordinary/*/{owner,observer}-ordinary-speed.mp4.
- Core562/562;fullEditMode499/499,0skips,profile125799d07998. V4 captureprofile
  b24103d8836a restored17 files. Earlier failures/logs are retained in Logs/throw-*.

## Networking and remaining critique

Protocol29 adds elapsed phase and signed spin to both charge messages. Changing
spin is sent at most10Hz with a0.5s steady heartbeat. Snapshot replay includes
ongoing preparation;owners retain their own live input state. Departure,release
and inability to act clear observed preparation. Old protocol28 players must be
rebuilt;Android qualification remains deferred. New NetThrowProbe and
net_throw_matrix.py must verify real host/owner/observer,directional changes,
release,shaped delay and observer rejoin in the exact new internal Windows build.
An internal protocol29 executable has now been built;separate-process tests are underway,with no pass claimed yet.

Self-critique: V4 corrects clear pose/framing failures and communicates direction,
but technical motion is not human feel approval. Broader owner/observer angles,
all18 skins in real play,foot support/cadence/backpedal/turns,aim-control/equipment
and remaining action sequences are still required. Current actual motor settles
0.08m above road support;measure drawn feet before choosing a grounding correction.
Source audits and carried-anchor regression are being rerun before the internal
build. Final game qualification and Windows release remain open.

![Straight body preparation](images/throw-straight-body.jpg)

![Negative Pektus grip](images/throw-left-owner.jpg)

![Positive Pektus grip](images/throw-right-owner.jpg)


Carried-anchor5/5,profile34e6a2b20095. All8 editor checks,profile627a278efdd5.
All14source audits pass. Diagnostic Active is a declared tournament modifier with
real guard read/clear routes;mode is only its guarded case parameter. Targeted
TournamentGuardTests first10/12 because test setup did not enable the new flag;
its setup/restore/by-name setter were extended,V2 passed12/12,profile12bcd7bc4a12. No guard weakened.


Internal Windows build succeeded through guarded GameBuilder (1019MB,58s,profile12282eaa369e) at
Builds/ThrowReview/TumbangPreso.exe,Logs/throw-review-build-v1.log. This is for
new-source process verification;the Desktop player and final release are untouched.


## First actual process run: FAILED, investigation open

ClassicV1 used internal executableSHA6f44fe53090dad3edd9f86f5b5691b2cc8cba07deb4efd4791d7e32135385cb8,
RuntimeDLL38f96cddf1d45016e0af3e8306ea5051cbd589569dce805f0370d2f754bd18fa.
Negative/positive windup phases match on host/owner/observer. The straight window
was trimmed to zero by the evaluator;proportional trimming fixes that fixture.
More serious:both clients report Held again after shoe1 lands (Loose),while host
stays empty. The earlier evaluator accepted a few initial released frames and
missed this later inconsistency. Strengthened it to reject post-release holding.
RawCSV/failedresult are preserved in throw-network-classic-v1 beside this report.
Cause is NOT isolated;instrumentation now names heldSeat/heldState/heldHolder and
traces actual Carrier holding/equip writers. Do not claim flight/carry fixed or
attribute the historical48idle penalties to this without connecting evidence.
Runner restored18 existing profile files. No player process remains active.


Instrumented buildV2 (RuntimeDLL0b1ac8640ad546ae1ce4d572752a7c45319e06aaf7bad752a8e491992715203a)
ClassicV2 passes all3 sustained phases and release across3processes,with0 unexpected
post-release held samples. Carrier traces show normal initial equip and release.
It did not reproduce V1;no Slipper runtime fix was made. V1's cause remains open;
repeatV3 is running before accepting this networking batch. Runner preserved25files.


## Orphaned warmup handover isolated by the delayed rejoin trace

ClassicV3 repeat passed. Hero Strike150ms one-way/rejoined observer reproduced the
post-release held state,despite158 correct active rejoin samples. The held item
is shoe0,the parked defender shoe,not the thrown shoe1. Host stack traces show:
Carrier.TryPickup -> Slipper.HostGrab(shoe0,seat1) during warmup,then
SliceRunner.EquipOwnedSlippers -> HostForceEquip(shoe1,seat1) at the whistle.
The old shoe0 still says Held/holder1 after it is parked. Periodic snapshots call
Carrier.NotifyEquipped(shoe0) on clients;world snapshot replay also alternates
that stale equip on the host. This is a one-hand/two-shoe relationship failure,
not a trajectory/ground query failure. RawCSV/results are beside this report;
full setter stacks are in Logs/throw-network-hero-delay-rejoin-v1/*.log.

Next: reproduce forced replacement and parking in focused tests,clear displaced
and parked holders through the state owner,then repeat the exact3process cases.
Do not claim corrected until the failing case passes. Historical48idle attribution
is still unresolved;there is no evidence connecting its run to this warmup issue.


Focused handover baseline0/2 failed on both still-present holder references.
Correction3/3 passes (profile85416977932f,25files restored):forced replacement
releases/seats the displaced shoe through existing silentLand;defender parking
disarms first;inactive snapshots cannot replace a real grip. Added fourth case:
a delayed normal grab must not overwrite occupied possession. FullEditMode running,
then renewed physical retrieval and corrected actual-process build/cases are required.


Handover V1 fullEdit503/503,profiledefa03145ccd;physical under-guideway/raised-slab/
unreachable-roof/direct-release regressions4/4,profilee818d65aa488. A final new-drop
refinement uses the existing narrow support query at the holder's feet,not broad
GroundY(feet) which could choose an overhead roof during a high/raised position.
The fifth handover test explicitly builds a3.5m support below a9mroof and requires
the displaced shoe on the support. PublicGroundY and actual flight are unchanged.
FullEditV2 is running before rebuilding the corrected process-test player.


Latest:504/504fullEdit (profile15a25637349e),4/4flight/retrieval and8checks
(profile75151794b649) pass for the handover correction. Corrected buildV3 is still
running. An orchestration retry launched too early after a wait still reported
running;the builder had purged the previousexe,so no player launched. Its
throw-network-hero-delay-rejoin-v2 folder is not gameplay evidence. Wait for
actual buildcompletion and verify RuntimeDLL before a fresh network test folder.


Corrected buildV4 Hero150ms/rejoin:allstages/release matched,0ghostsamples,and158
rejoined active samples. Strictcase failed its setup requirement because warmupAI
pickedownshoe1. This cannot prove the causalcase. Fixture now isolates warmup
inputs and walks/grabs shoe0 through ordinaryInputIntent before the owning client
arrives;no forced equip/reset in setup. BuildV5 is running. Require actual foreign
pickup AND disarm beforewhistle in the following corrected3process regression.


## Corrected causal process case passes

BuildV5 RuntimeDLL307167a3a54ebb1f9ec44db7799bd14cad46321cf98b906c76b2fbf60bf2189c.
Hero Strike150msone-way/observerrejoinV4 PASSED. Actualordinary-input warmup
picked foreignshoe0;hosttrace shows disarm beforewhistle,thenownshoe1 equip.
All3 processes match straight/negative/positive preparation;rejoinobserves159active
samples;0unexpected post-release held samples on host,owner andobserver.26profile
files restored. RawCSV/result preserved in throw-network-hero-delay-rejoin-v4.
Classic strictcase is running on that same executable before committing the batch.
