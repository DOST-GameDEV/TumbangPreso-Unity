# New tutorial report, October1

Same Feedback row QA_TUMP_0044: Harry reports the slipper still flies, with a
Retrieve07/20 screenshot showing it above the court. Build and preceding throw
are not identified. The image is evidence of that player's view, not proof that
current source reproduces it. Keep the earlier landing fix and its limits.
The second screenshot clearly boxes the corner COMPLETE label, redundant beside
TRAINING COMPLETE. This is a complete actionable request, despite an earlier
partial text read.

Claim GuidedTraining.cs completion label only and OwnerTrainingUiTests.cs targeted
new cases first. Reproduce actual automatic Throw/Retrieve progression at high and
normal trajectories, plus skipped-throw staging. Do not modify shared Slipper
physics without a demonstrated failing path and expanded claim. Baseline checks
must identify loose/inflight state, height and elapsed simulation time. Completion
check requires counter hidden at completion and restored on ordinary lessons.
One guarded native run, explicit profile/results, one bounded tooling repair.
Retain tester images in their original Doc row; do not publish them elsewhere.

## Reproduction and narrowed cause

Automatic high throw toward(0,14,0) advances itself into Retrieve but remains
InFlight after12simulation seconds, at(0,11.77,0.44), velocity(0,1.54,0).
The shoe is enabled and timeScale1. This reproduces the skyward symptom in current
source, not merely an old build. Skipped-throw staging passes at road y0.20.
The corner COMPLETE test also fails as reported.

The hidden can remains upright/protected because inactive Update cannot expire
its restore shield. Slipper's flat can-contact test ignores object activity;
repeated contact returns before the six-second airborne ceiling. Expand claim to
Slipper.cs active-object contact gates and enforce its existing flight ceiling
before contact returns, plus NetSession.cs compatibility. Hidden bodies likewise
must not become invisible blockers. Preserve normal active-can/body geometry,
score ownership, real trajectory and existing duration constants. Add a focused
protected-can lifetime control, then rerun the changed tutorial cases once.
