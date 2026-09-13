# Play-feel and equipment rework after the maps

Owner-authorized scope,2026-09-12. Begin this phase after the substantial map
transformation. It is not implemented or qualified. Read the actual motor, throw,
Pektus, can, equipment, animation/FPP and network owners before choosing changes.

## Required outcome

The game should feel more convincing to play: weight, momentum, preparation,
contact and recovery agree with what happens. The owner explicitly authorizes
figuring out other applicable cases and implementing the complete pass from their
examples. Do not limit the work to a few named animations or ask them to list every
fault. Keep the approved eighteen blocky people/outfits; realism concerns motion
and gameplay response, not realistic anatomy, rounded bodies or thumb details.

## Movement and complete action sequences

Audit starts/stops, acceleration/deceleration, sprinting, backpedaling, turning,
carrying, planted feet/foot sliding, body/FPP synchronization, impacts, interrupted
blends and recovery. Preserve four players, rotating defender, can and retrieval,
both modes and existing simple controls. Fix sound/contact timing with the action.

Throwing must show preparation and accepted release. The owner's example is a
visible backward lean while winding up/aiming, seen by other players as well as
the owner. Author weight transfer, arm preparation, release and follow-through;
verify interruption/return-to-locomotion and holding a charge while moving.

**Pektus is explicitly required:** it currently feels like the shoe magically
acquires curve. Give it distinct forearm/wrist/arm preparation and a spin-imparting
release, with follow-through matching the accepted curve direction. Verify both
directions, holds/releases, interruption, FPP, observer body and network peers.
A straight-throw gesture followed by curved flight does not meet this requirement.

The owner suggests small noticeable shake and accuracy error for more skillful
aiming. Evaluate readable, controllable instability and timing before arbitrary
random spread. Camera/FPP, aim indication and actual trajectory must agree.
Preserve motion-comfort settings without creating an accuracy advantage. Compare
keyboard, controller and touch control, low/high frame rates and delayed peers.

## Slippers and cans need reasons to be chosen

The owner authorizes substantially differentiating or revamping their existing
attributes so choices go beyond appearance. First inventory the current data,
behaviors, models, collision sizes and network/save contracts. Then create a
small coherent role/tradeoff table for every existing option and implement it.

Slipper candidates to investigate include launch/charge cadence, travel/curve
control, momentum/impact, bounce/settling and retrieval cost. Can candidates must
consider target readability, contact/tipping/stability and recovery/reset behavior.
These are investigation dimensions, not chosen stat changes. Avoid dominant
all-purpose picks, cosmetic-only claims or a new layer of complicated controls.

Keep stable equipment IDs, owned items/profiles and truthful descriptions. Do not
add an unrequested grind, purchase flow or paid system. Shared can choice/behavior
must be consistent for all four players and both modes. Preserve raised-ground,
under-guideway and unreachable-surface slipper recovery.

## Recovery and networking

Thoroughly reverify button mashing across all contexts: stuns, trips and Sa Bubong
fall recovery. Use actual keyboard/controller/touch input paths, held-vs-tapped,
quick taps,30/60/144Hz, minimum recovery and host/remote/delay/rejoin. Respect the
separate GenericPadBridge/MenuNav/controller-mapping owner. Simulated devices are
not proof of physical-device certification.

Validate host-authoritative actions, active effects, equipment/kit swaps, round
and rematch reset, reconnect and host loss. Do not change the protocol for input
presentation. Legitimate new replicated state needs deliberate compatibility work.
Same-hero loadout binding must not reset live state or apply scales twice.

## Evidence and critique

Record current behavior and controlled reproductions before changing physics.
Compare full normal-speed sequences from owner and observer views, include
interruption and overlap, and use real separate processes for network claims.
Bot results have run-to-run noise; one seed does not establish balance.

After each batch criticize style fit, readable intent, weight/contact, timing,
control/fairness, full action continuity and cost. Include inconvenient views and
remaining weaknesses, then revise. A test pass is not a feel verdict.

## Continue afterward

The owner explicitly asks to resume everything else in the actionable TODO list
after the targeted work. Graphics scalability, approved Sa Bubong, whole kits and
alternatives, Phaister/Kuro qualification and remaining animation/network/release
work stay open. No stop or handoff is requested. Preserve ownership and external
approval boundaries, and verify the exact Windows executable when release-ready.

## Source audit and ordinary-speed baseline,2026-09-13

All six map/mode carry-throw-retrieval sequences completed atTime.timeScale1 in
Logs/maps-ordinary-baseline-v1. Owner frames are usable; the observer offset put
the Eskinita camera behind a fence and was rejected for body-motion assessment.
V2 uses an explicit court-side witness offset, preserving other capture callers.
Motion videos use actual recorded frame intervals (tools/encode_motion_review.py),
not a fixed input rate that would speed up dropped-frame captures.

Current code:CharacterAnimator's charge override affects one arm after graph
evaluation; torso lean/weight transfer and Pektus-specific preparation are absent
from that path. ViewmodelArms also uses a single-axis charge pose. Carrier stores
CurrentPektusSpin locally, while ThrowCharge replicates only active/inactive;
trace other pose/intent payloads before adding network data. ObservedChargePower
and actual ChargeRatio intentionally differ, so distinguish timing from power.

The baseline action logs show attack-melee-left during some Hero Strike pickup
sequences versus pick-up elsewhere. Trace the selected rig and cue/fallback before
calling it a bug; it is a concrete review lead, not a confirmed cause. Leg-angle
CSV values wrap0..360 and must be normalized before reporting motion amplitudes.

Relevant owners:Runtime/Carrier.cs,Runtime/Visual/CharacterAnimator.cs,
Runtime/Camera/ViewmodelArms.cs,Runtime/Net/MatchRpc.cs,Core ThrowRules/Roster/Balance.
No play-feel runtime changes have been made in this audit.

Equipment audit detail: Roster currently maps flight to launch speed,impact to
body-block push,and recovery to post-pickup ThrowLockTime. Can scales affect
reset time,rebound and hit margin. Do not assume the slipper IMPACT label already
means easier can knockdown. AIController currently calls ThrowRules.LaunchSpeedFor
in its aiming/charge solve,so the old comment about AI assuming one launch speed
needs checking against current code before using it as a tuning restriction.
Can entry0 has authored5/1/1 points while the comments describe neutral fallbacks;
trace selection/default consumers before deciding the neutral-contract correction.
No balance change is implemented from these observations yet.

Network presentation lead:OnReqThrowChargeMsg validates the remote seat then
BroadcastThrowCharge sends to peers excluding local host and source,without an
obvious host-local ApplyObservedCharge call in that handler. Trace all remaining
input/snapshot writers and reproduce on the host before claiming this is the
observed missing tell. Windup spin is absent from these payloads; existing spin
serialization appears only in throw requests/loose-slipper state.


## Active throw implementation,2026-09-13

Baseline fresh regression0/1 fails with exactly0degrees torso preparation on Bayan,
Logs/throw-motion-baseline.xml,profile19a202c86804 restored17. Current dirty runtime
adds shared ThrowGesture timing/pose,torso/head/off-hand preparation,signed Pektus
arm roll and forward release/recovery in body/FPP. Legs continue during throws.
No extra controls/random trajectory changes. Original rigs/assets unchanged.
This is unqualified. Windup spin currently local only;need complete host/peer
accepted charge state/clock/spin relay and snapshot compatibility before shipping.
Ordinary-speed visual review must assess arm/head/torso intersections,foot motion,
release truthfulness,cancel return and18rig coverage. No artistic success yet.


Protocol29 dirty:phase+signedspin in both charge payloads,host-local application,
owned-seat validation,finite checks,rate-limited changing pose/heartbeat and world
snapshot replay to observers. Owner retains local input authority;round inability,
release and departure clear preparation. Old protocol28 players must be rebuilt;
Windows is this qualification target,Android remains unqualified.

Relay baseline0/1 reproduced hostObservedCharge=-1 despite an accepted peer request,
Logs/throw-relay-baseline.xml/profile819b7b9568b1. V2 fullEditMode492/493:
all new motion/relay regressions pass;DeadFeatureAudit's literal throw-call lookup
misses directional dispatch. Updated it to follow the actual selector/call;initial
follow-up failed compilation because its LINQ import was missing,now corrected.
Wire audit initially counted compressed same-line writes once;expanded writes,
added explicit finite guards in client receiver (Carrier also guards application).

V1 ordinary capture rejected as evidence for final body pose:coroutine sampled
before LateUpdate. Record now captures from a late callback after body/Carrier/FPP.
V1 negative spin assertion also kept checking after a can hit consumed spin;now
checks the first InFlight observation. No slipper-physics patch from that fixture.
Reduced FPP spin roll after the negative preparation dropped too far below frame.
V2b capture/critique and fresh gates are next. All failure logs preserved.


V2b late-frame capture1/1,all6 throws/bothmodes (profile8f9dfdab22eb) succeeded.
Critique: body+X charge offset applied on top of holding-right lowers the hand,
instead of lifting it behind the shoulder. Old comment was not a measurement of
this actual layered pose. V3 uses the negative offset and16degree back lean.
FPP fixed-elbow carry lets charged rotations bring the shoe into the eye or below
frame even at smaller spin angles. V3 solves elbow from the raised hand anchor
and actual arm rotation,keeping forearm/slipper attached at arm's length. Added
hand-space measurements and framing regression. No changes to people geometry.
All style/timing/collision/play and separate-process criteria still need review.


V4 capture1/1,both modes3styles,profileb24103d8836a. Reduced FPP throw-only cock
angle and lower hand target keep the forearm reaching the frame edge;the legacy
lunge FPP angle remains unchanged. Body timing/cock direction and signed roll
retained. Seven motion/framing/recovery tests are now authored (all18body rigs plus
3grips plus3signed release/cancel across30/60/144fps),fullEdit running. Current
source audits14/14 pass. A new opt-in NetThrowProbe with a separate late recorder
and tools/net_throw_matrix.py will drive only the owning client's actual input
and inspect3real processes,delay/rejoin. Must build a new internal executable first.

Movement lead: actual capture body root settles .08m above the road after motion;
MatchInstaller leaves CharacterController.skinWidth at Unity's default. Rendered
foot support still needs measuring before choosing a physics/alignment correction.
Do not equate the projected shadow gap alone with a proven foot-position bug.


Warmup handover investigation:real Hero150ms/rejoin trace isolated seat1 grabbing
shoe0 beforewhistle,then being handed shoe1 without disarming parkedshoe0. Repeated
inactive Held/holder1 packets re-equipped the ghost on clients after the throw.
Baseline focused0/2 reproduces both forced replacement and parked holder leak.
Dirty fix lives inSlipper.HostForceEquip,SliceRunner.EquipOwnedSlippers,and inactive
MatchRpc snapshot normalization. It does not alter flight/GroundY queries;dropped
replacement is seated with existing silentLand using holder's foot-level support.
Three focused regressions are running;actual3process reruns still required.


## Ground contact baseline, after pushed4981c986

FootSupportProbe baked actual skin vertices after movement/settle forBayan,Dante,
andNemu. Atcontroller skin.08,root/sole/controllerbottom are.18 above road.10;
at.035,allare.135. Importedbounds and solesagree. Baselinefailed0/1 withthree
8cmplanted gaps;profile07743bd549ec preserved25files. Cause is capsule skinspace,
not modelshape/bindbounds. No physics skinwidth/sourcegeometry change selected.

Dirty CharacterVisual correction composes a bounded rendered contact offset with
its existing model-root/smoothing owner. Only groundedpeople over nearby support,
short downward nonallocquery,limitactualskinwidth;airborne/no-support returns0.
Next:verifybothskin experiments,all18people,jump/kerb/slope/no-support/snap/rebind/
remote smoothing,ordinaryplay and meaningful cadence/backwards/turn improvements.
Do not call contact solved from idle samples alone.


Foot contactV1 passes all6measurements:solegap0.0000m forbothskinwidths;controller
bottom staysat.18/.135 above road.10. All18rigs andactualkerb/jump/teleport sequence
nowunder verification. No successclaim forslopes/no-support orremoteuntilchecked.
Further movement sourcelead:CharacterAnimator.FlatSpeed reads Motor.Velocity,which
is steering velocity and excludes external impulse and actual collision displacement.
StepGait also advances phaseforward regardless of backward/strafe movement. Measure
those sequences before changing stride direction/cadence;keep gameplay velocity
and network budget semantics separate from observed animation speed.


FootV2:all18bodies/bothwidths,36measurements,all0.0000msolegap. The separatekerb
fixture failedbefore contact sampling because its world+Z intent was interpreted
in the defaultmouse/body-relative basis and moved toward-Z. It now explicitly
selects movement aiming for its world-space route;no runtimephysics change from
that fixture failure. Profileb13873006298 restored25files.

Additional movementsourcefinding:Steer normalizes any nonzero wish in BOTH aim
modes,discarding analog magnitude. Even reviewinput .2 therefore moved atfullspeed.
After grounding,assess preserving analog magnitude in the sharedmotor (including
AI partial-intent consumers),alongside observed ratherthancommanded gait speed.
This concerns motor/control feel,not separate-owned controller device mappings.

Latest ultimate direction: the owner wants each ultimate to have its own special
moment, citing Tekken and Genshin Impact as impact/timing references. Give each a
distinct preparation, camera/body sequence, sound, imposing payoff and recovery;
keep opponent tells/counterplay visible and the native blocky style. No copied
characters/effects, forced long input locks or shared generic recolored cutscene.

## Newest rooftop and throwing requirements, 2026-09-13

The owner explicitly rejects the small closed/scenic pool. Enlarge it, REMOVE
the pool fence, make it accessible, and implement actual swimming with a swimming
animation. Swimming/body/FPP/observer support belongs to this MAP pass, not the
later whole-roster animation pass. Pool geometry must have a real basin/opening,
entry/exit and water interaction; standing on a solid blue block is not swimming.
Pool slipper handling must be reconsidered because it is now accessible; the
10second penalty is required for slippers falling OFF the roof, not automatically
for all water contacts. Preserve retrievability and both modes.

Fence ALL outer roof edges; retire the isolated east opening/paint stripe. Actual
falls must be possible over any outer railing with existing controls. Keep mash
get-up and the10second off-roof slipper penalty. Add reasonable pots/resident
objects and several types of cosmetic birds that arrive/perch/peck/fly away.
Avoid new controls/objectives and keep all shared gameplay random streams untouched.

Throwing after maps+UI: movement worsens accuracy; standing still improves it;
immediate releases are shakier/less accurate; holding longer settles and improves
aim, with a small residual shake. The owner compares aiming behavior to PUBG sniper
settling. Author visible body/FPP aiming/release to match actual authoritative
trajectory, preserve Pektus signed arm action, and avoid invisible arbitrary misses.
The order remains MAPS -> FULL UI -> ANIMATIONS/SKILLS/PLAY FEEL.
# Renewed owner throwing-animation feedback, 2026-09-13

The visible character/model throwing animation still looks weird and weak to the
owner. Do not treat existing throw engineering as accepted animation. Rework the
full observer/body gesture and FPP together: pullback/lean, weight transfer, arm
path, release and follow-through. Pektus has its own signed arm/wrist spin action.
Retain moving/still and immediate/held aiming-accuracy requirements, simple
controls and the18approved models. Maps and full UI remain earlier priorities;
roof get-up and swimming animation are explicitly current-map work.


## Parent source audit during graphics build,2026-09-14

This is preparation, NOT implemented animation/skill work. Current maps/graphics
remain the parent priority; UI agent independently finishes native views.

The same-hero changed-loadout defect is visible at MatchRpc.RebindKitIfHeroChanged:
it returns when current/wanted Kit types match, before decoding the newly received
HeroBuild. Consequently a changed sidegrade on the same hero can retain the old
realized ability while the selection/profile UI shows the new choice. Do not fix
this in UI. HeroAbilitySystem.BindHero creates a new Kit and applies multiplicative
ScaleLoadout changes, so blindly calling it on every network update would reset
live state; applying its modifiers repeatedly to an existing kit would stack them.
Next focused design must compare normalized equipped build identity at the actual
legal pick/round transition, preserve unchanged active kits, and prove defaults ->
alternate -> defaults plus repeated sync, both local and remote seat ownership.
Do not claim a fix until actual synchronization boundaries/state have been traced.


## Current solo throwing pass,Sep14 after UI deferral

Owner moved unfinished UI to LAST and forbids all further agents. Current
implementation is the shared throw-aim drift, before body-pose refinement.
ThrowAimRules defines smooth bounded angular movement: quick releases have more
motion, holding settles toward small residual movement, and movement adds motion
with a smooth rise/settle envelope. No extra randomized error is added at release.
Carrier's effective aim feeds preview, muzzle direction and the actual existing
throw request. Capture that aim BEFORE CancelCharge; otherwise releasing would
silently remove the visible drift. No new network message fields or gameplay RNG.
The main reticle follows effective aim in LateUpdate; right-hand preparation
uses the same angles. This is functional aiming integration, not resuming the
full deferred UI overhaul. Standalone/the host and client requests share the
accepted endpoint; remote pose exact wobble phase/replay remains a later check.

Core ThrowAimRulesTests6/6passed. Current Unity ThrowAimIntegrationProbe is being
compiled/run; it compares actual released velocity to preview in Classic and
boosted Hero Strike. A related existing preview discrepancy was corrected:
Zack Snap Discharge/Sean Flare Shot preview now includes the same variant gain
as HostThrowAt. Do not call this phase visually accepted yet. Next: check aim
motion at normal speed, then refine full-body preparation/release/follow-through
and signed Pektus against actual retained rig poses. Do not change the18people.
