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
