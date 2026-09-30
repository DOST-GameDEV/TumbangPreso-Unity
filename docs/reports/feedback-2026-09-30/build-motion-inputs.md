# Retained Build Inputs, 2026-09-30

GameBuilder invoked SwimmingAnimationAuthor and RecoveryAnimationAuthor for every
build. Both author tools CopySerialized newly generated data over saved clips,
discarding hand-adjusted curves. The normal build now validates existing sets
without authoring them. Explicit authoring remains available; EnsureMissing and
a separate repair entry point skip every existing set.

The native prerequisite check found missing team-amihan input. Runtime lookup has
no alternate set for that key. The missing-only repair found four absent sets:
swim and recovery for team-amihan and team-paete. It added only those assets/meta,
using existing motion definitions. All84previous asset/meta hashes are unchanged.
This is a missing-animation bug repair, not a redesign or ability change.

Two final native EditMode cases pass in1.8480611s:23rig prerequisites preserve
retained file bytes; missing/empty/duplicate inputs refuse while an authored curve
is retained. One additional native case passes in0.353258s: all fourteen repaired
clips bind valid paths and move the actual model bones when sampled. The initial
two failures are preserved: missing input and a synthetic test that assumed Unity
created only one position binding. Selecting its x binding fixes that fixture.

- [Initial prerequisite result](checks/build-inputs.xml)
- [Final prerequisite cases](checks/build-inputs-final.xml)
- [Actual repaired-rig bone motion](checks/missing-rig-motion.xml)
- [Retained input hashes](checks/retained-motion-before.json)
- [Missing-only repair receipt](checks/motion-repair-receipt.json)
- [Source inputs](checks/build-motion-inputs.json)

Native curve/binding validation is not a new player build, animation-feel review,
all-map playback or real-peer qualification. An internal Windows build and direct
LAN session are the next qualification steps. Other reported walking/running
defects still need the named character/state; this repair does not close F0930-16.
