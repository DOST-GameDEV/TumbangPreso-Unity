# Visible throw-aim settling

Carrier now uses one effective aim point for the muzzle direction, trajectory
preview and released throw request. It captures that point before clearing the
charge state. Quick throws have more continuous angular drift, holding settles
toward a small residual, and actual movement adds drift with a smooth recovery.
The first-person hand uses the same angles; the native reticle projects effective
aim after the camera updates. There is no extra randomized error on release and
no new network message fields. Existing charge power, Pektus and rules remain.

A related preview mismatch is corrected: Zack Snap Discharge and Sean Flare Shot
now preview the same variant speed multiplier that HostThrowAt applies.

Validation:6focused Core ThrowAimRulesTests passed. Unity integration v4 passed
1case across Classic and boosted HeroStrike. Nonzero target shifts were.07633m
and.11788m; released velocity matched preview exactly (error0.000000). The reticle
projection also matched effective aim. Fixtures1/2had compile/parked-input issues;
v3proved velocity consistency but faced the review camera away from the target.
V4corrected only test staging, and those earlier pictures were not accepted as
reticle visual evidence. No full suites were run. Profiles/input prefs restored.

This is controlled intent/pose validation, not yet a complete ordinary-speed
movement/hold/throw or separate-process network qualification. The next body/FPP
motion pass will review that experience together with the whole release action.
Older BayanClassic controlled-sequence footage (animation baseline only, not
current map art) shows a very slow initial gather and short follow-through. Improve
those within the retained18blocky models; do not add fingers or remodel the cast.

UI overhaul remains LAST under the latest owner instruction. These reticle
changes only connect aiming feedback. No agents and no reset authority.
