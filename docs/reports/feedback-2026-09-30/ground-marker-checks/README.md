# Flat hollow player ground markers

Harry requested flat shader glow instead of raised circular planes, then added
"Preferably make it a hollow circle." Both roles now use hollow shader coverage:
attackers a circle, taya an open octagon, catchable targets four scoped brackets.
Seat colours, water/flight placement, role pulse and local first-person hiding
remain. A four-vertex XZ mesh has no side walls, collider or cast shadows.
Floor clearance is 6 mm instead of 20 mm; blending provides a soft luminous rim.

## Native evidence

Unity 6000.5.8f1, Linux OpenGL, isolated profile. Final hollow-shape case passes
1/1 on protocol125: actual visible rim pixels, empty centre for both roles,
zero-height vertices, no collider, correct shader/role property and inspected
256px captures. Uncommitted aiming changes were excluded from this candidate.
Prior focused protocol124 cases passed water entry/exit and nested-camera target
scoping. Their geometry/placement logic is unchanged by the new hollow preference.
The updated small EditMode mesh test was not separately run. No new player build,
actual peers, all-map view or human approval is claimed.

## Retained failures and limits

The first run stopped at the memory guard. One bounded setup repair allows idle
import workers a second to retire. Two later filled-disc pixel checks failed
because they selected the local FPP-hidden seat; CameraRig sets that entire
renderer hierarchy to ShadowsOnly. Replacing negative-base pow with multiplication
did not cure that capture mismatch. These failures remain archived.

The later human hollow-circle requirement was checked on observer-visible seat2;
it requires both empty centre and visible rim, so a blank capture cannot pass.
Final XML completed and passed before the memory guard acted during exit. The
runner records stoppedForMemory=true and exit0; do not describe this as a clean
full-editor shutdown. OOM counters stayed11 / 6. The final shape captures are
isolated top-down views; prior camera/water evidence is not a fresh all-map film.
