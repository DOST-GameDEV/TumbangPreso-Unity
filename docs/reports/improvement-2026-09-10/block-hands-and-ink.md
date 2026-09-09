# Block hands and lighter character ink

The owner rejected the rounded Berto draft and asked to retain the cute, angular
cast with flat graphic faces. That draft and its speculative wrist integration
were never pushed and have been removed from the live source. The previous model
was restored and rendered in Unity before this correction.

The first refinement had added two protruding thumb pieces to every hand. Those
additions are now removed from all eighteen playable bodies (384 vertices and
176 triangles per actor). Original block hands, head meshes, authored animation
samplers, skin bindings and gameplay identifiers remain. The authoring migration
checks the affected arm joints and complete triangles before removing them and
asserts unchanged animation data afterward. It cannot add the rejected thumbs again.

Character outline expansion is 0.0045 rather than 0.008 model units, 44% thinner.
The welded hull remains continuous; props keep their separate width. This reduces
uniform ink heaviness. It does not claim every crowded costume is individually
resolved. The comparison is the same imported Berto, four angles and camera setup:

- [Before](style-correction/berto-before-hands-ink.png)
- [After](style-correction/berto-after-hands-ink.png)

Roster regeneration succeeded (20 retained people, 6 cans, 10 slippers), including
matching body-derived first-person arm meshes. Focused EditMode XML reports 14/14
passing across OutlineWeldTests, RosterArmGeometryTests and MotionContinuityTests.
Nine existing profile files were restored and hash-checked after each Unity run.
These results certify this correction's checks, not final cast approval or the
unfinished all-character/all-animation/all-skill scope. Final player build pending.
