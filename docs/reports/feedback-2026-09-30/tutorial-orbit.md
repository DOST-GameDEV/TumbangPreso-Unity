# Smooth tutorial orbit and uncluttered ultimate

Continue QA_TUMP_0044 for the new Human notes/comments.

The circular attacker chased a slowly moving point at full walking speed, caught
it, overshot and reversed repeatedly. The native baseline measured144 reversals
in201 fixed-step samples, with a179.97degree maximum heading change. It is a
reproduced movement defect, not an authored animation redesign.

The target now follows the circle tangent with gentle radial correction. An owned
speed-zone multiplier preserves the original3m/.45rad-per-second pace. It is
paired with cleanup on lesson change and destruction, so normal bots and the
completed practice ground do not inherit it. No motor, animation or AIController
implementation changed. Final201samples have zero reversals, maximum0.52degree
turn and1.355m/s mean speed. Radius and cleanup assertions pass.

The shared ultimate identity header is hidden only during guided training. The
actual introduction scene, texture, performance and ability timing remain intact.
The same header stays visible in ordinary play. Its rendered native training frame
was inspected. No character-specific kit or cutscene was changed.

Unity6000.5.8f1 LinuxOpenGL, guarded isolated profile/mip2,2/2 final native cases
pass in10.23seconds; exit0, no extra OOM or memory stop. Baseline includes the real
orbit failure and one banner-fixture lookup error: its canvas is root-level,
not under its lifetime owner. Correcting that lookup retained all assertions;
no product failure is inferred from the fixture error.

[Raw XML, receipts, hashes, before/after motion and frame](tutorial-orbit-checks/).
This is local native behavior/presentation qualification, not a new standalone
build, physical-device or multiplayer claim.
