# Movement revision result

The owner-requested numbers are implemented. [Exact scope](plan.md).

## Managed rules

708/708 pass. The first run's eight failures remain available. Superseded
literal expectations were updated to the explicit owner contract; the expanded
lunge also required its active sweep to cover the full impulse decay and the
movement budget to preserve its existing greater-than-two-times impulse margin.

## Native Unity physics

Four distinct cases are qualified with Unity 6000.5.8f1, without graphics:

- Eight actual locomotion samples cover both roles, walk/run and both modes.
  Measured attacker speeds: 3.7500 and 5.6250 m/s. Defender: 5.0000 and 7.5000 m/s.
  Hero samples include Sean and Amihan, with no innate speed difference.
- Actual flat-ground jump: 1.0539 m height and 0.7600 s airborne. The continuous
  model is 1 m and 0.75s; fixed-step integration accounts for the small difference.
- Fatigue retains 3.75 m/s attacker walking while sprint and regeneration remain
  blocked, after genuinely spending the full stamina pool.
- Real lunge input verifies tap recovery near 0.5s and full-hold recovery near 2.5s.

The first native run failed all four during fixture grounding, before testing
movement. The fixture now derives capsule clearance, synchronizes transforms
and waits for actual ground contact. The next run passed jump and fatigue but
revealed a separate fixture error: it always published defender slot 0 while
trying to test the actor in slot 1 as defender. Correcting that snapshot argument
preserved all behavioral assertions. Only those two failed cases were rerun;
both pass. This is four qualified distinct cases across the retained runs, not
four passes in the last two-case run. Production movement was unchanged during
these fixture repairs.

Final targeted process: tree RSS 3,333,279,744 bytes, container 6,584,127,488 bytes,
no guard stop, exit 0. Original validation EditorSettings and named profiles were
restored. Frozen input hashes are retained. No copied player cache, changed
production project settings, altered projectile gravity or weakened guard.

## Handoff

Protocol 137 requires matching rebuilt clients. Pull ASTRAReworks and let Unity
compile before playing. This evidence does not claim a refreshed player build,
actual peer match, controller/touch hardware, visual review or human acceptance.
The attacker slide remains conditional on a retrievable loose slipper ahead; it
is not a free general-purpose dash and its values are preserved.
