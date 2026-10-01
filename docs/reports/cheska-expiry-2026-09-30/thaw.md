# Cold Feet spatial thaw

The existing flat authored field now melts back in broad uneven patches during
its final0.7seconds. Ice and fractures expose the original street between them,
instead of the whole surface merely becoming fainter. The existing full danger
boundary remains until its last0.1second fade and original expiry.

Only FrostSurfacePresentation and its existing shader change at runtime. No new
objects, particles, meshes, audio or gameplay effects are introduced. The separate
Nova path keeps thaw at0 and its previous outward wave. Field lifetime is still
assigned by gameplay: runtime currently5seconds versus the Wiki's7.5seconds.
That discrepancy remains in owner-reserved ability work; this presentation follows
both supplied durations and does not change either rule or status behavior.

## Evidence and critique

Native Unity6000.5.8f1 Linux OpenGL, real Eskinita court, isolated profiles:

- Baseline1/1expected failure: uniform fading has no spatial-thaw phase. Native
  before captures retained; no pre-existing crash is implied by that new feature check.
- Final2/2pass: late visible frost coverage47419→4898pixels, full edge retained,
  unchanged transform, deterministic repeated/restored samples for5and7.5seconds,
  and Nova's separate wave remains untouched.
- A capture-only follow-through passes the same field case while sampling the
 0.7second transition every0.05seconds. This is not another distinct test case.
- No memory guard or new OOM. [Checks and native captures](thaw-checks/).

The [native timeline-sampled film](thaw-checks/thaw-native-v1.mp4) has a0.5second
opening still and0.3second ending still around the0.7second thaw. Its20Hz effect
samples are not a measured player framerate. Intermediate frames were inspected:
the broad gaps are legible, the court returns through them, and the perimeter stays
honest while the field is active. It deliberately avoids a loud burst or glitter
because this is a ground hazard ending, not a second attack.

Tester taste, target-device performance, actual peers and a refreshed player are
separate. The wall-shatter request remains open under the accompanying plan.
