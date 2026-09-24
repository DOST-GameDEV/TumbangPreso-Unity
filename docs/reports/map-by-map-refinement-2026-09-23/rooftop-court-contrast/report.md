# Sa Bubong court paint, 2026-09-24

Baseline5e9b711c7. The pale warm court rectangle became difficult to distinguish
from the bright green coating in the live high preview. A native fixed-camera
study compared existing paint, ivory and a dark warm neutral. Actual images and
25percent greyscale were inspected. Dark paint makes the rectangle clearest;
ivory barely improves the baseline. No wider colour or lighting change was needed.

Selected colour(.23,.19,.15) is authored in Readablecourtmarking.mat and assigned
only to the six existing court/throw lines. SaBubongBuilder uses it on rebuilds;
RefineCourtMarkings updates only those assignments. The scene diff is exactly six
material references. Line geometry/width, physical rules, court coating, roof,
sun/fog, other maps and the old Courtchalk material used by notice paper are unchanged.

Native study1/1 passed in1.740s. The three study images are the same-camera A/B.
After authoring, the same small route passed1/1 in1.287s; actual serialized-result
frame and grey were inspected. The final scene matches the selected dark paint.
The final frame is an authored-result check, not a claim that a separate launch
reproduced the previous preview orbit exactly. No fixture repair or failed run.

Raw frames/XML/full pre-restore patches remain in QUAL Logs/roof-court-contrast-*
and roof-court-*-before-restore.patch. Known generated churn restored; DEV protected
PNG metas untouched. No UI/HUD layout, model, character, collision or player build.
Parent map/high-preview haze, final card refresh and integrated camera/platform
qualification remain open. This closes only the local court-paint weakness.
