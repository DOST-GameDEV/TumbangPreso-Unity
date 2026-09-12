# Ground contact revision

The actual skin vertices, renderer bounds and controller bottom all floated by
exactly the controller skin width after movement:8cm at the shipped default,
3.5cm in the independent narrower-controller experiment. This was measured on
Bayan,Dante andNemu;it was not an imported-model bound or shadow-only error.

CharacterVisual now composes a bounded visual offset with its existing model-root
and remote-smoothing owner. A short downward nonallocating ray checks nearby
support while the person is grounded;the offset cannot exceed the capsule skin
allowance. The controller,collision tolerance,mesh geometry and gameplay motion
are unchanged. Jump/no-support returns the correction tozero;teleports clear it.

All18people atbothcontrollerwidths now measure0.0000m planted sole gap across36
samples. The actualmotor climbed a25cmkerb;soles meet its35cm surface while the
controller remains43cm high. Jump,landing,teleport,simulated remote-smoothing seam,
a1.1m raised support and support removal passed. This is not actual multi-process
remote-grounding certification or an arbitrary-slope foot-IK claim.

Evidence:Logs/foot-support-baseline (failed as expected),foot-support-v1/v2 and
foot-support-v4/transitions.csv. V2's kerbfixture initially used world-Z input in
the mouse-relative basis;fixed to explicit movementaiming before its passingV3/V4.
Profiles:baseline07743bd549ec,V1e54a7fd283ed,V2b13873006298,V3ba249aa77653,
V4 0aa5cbcd280e;25files restored/hashverified perrun. Actualordinary carry/throw/
retrieval capture acrossall3maps/bothmodes passed1/1,profile450d960c0ffc;12videos
are being encoded. Idle/carry stills viewed onall3maps. Shadow separation can still
suggest hover despite measured solecontact;shadow/light review stays in graphics.
FullEdit is running and broader visualcritique
still required before a stablecommit. Widerlocomotion/equipment/graphics/newmap/
kit/network/release scope remainsopen.


Verification complete for this bounded revision:fullEdit506/506,
profilefb8e0626d301;14sourceaudits pass;all12 ordinary videos encoded. Stable
commitnext. Actualseparateprocess contact and fullslope/locomotion quality remain
part of the nextmovement qualification;the existing internalplayer predates this.
