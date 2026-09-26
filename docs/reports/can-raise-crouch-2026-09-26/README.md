# The can raise crouches first (GAMEANIM-1, 2026-09-26)

Owner: *"i want u to improve animation of raising can too when its down"*, *"they should crouch
first and put it up"*.

Captured by `GameplayActionShots.RaisingTheCanInBothViews` (Bayan Plaza, native Windows, graphics
on), body witness on top and the taya's own first-person view below.

- `raise_before_bob_v1.png`: the first pass of the new shape. The metrics (`torso_x`) showed the
  real fault underneath it: every 0.4 s relayed `grab` restarted the 0.33 s `pick-up` one-shot, so
  the torso went 87, 42, 83, 42 degrees, a bob on every relay.
- `raise_crouch_v2.png` and `frames_v2.csv`: with the one-shot suppressed during a raise. The torso
  runs 0, 20, 57 (the squat, both hands on the can), 35, 30, 22 (the lift with the can tipping
  upright), then idle; no reversal. First person: the eye drops and tips down onto the can with both
  hands on it, then rises with it.

The shape is `Visual.CanRaiseShape` (crouch 0 to 0.18, grip to 0.30, lift to 0.85, set to 1), shared
by `CharacterAnimator.ResetRaise` and `CameraRig.ApplyFpp` / `ViewmodelArms.RaiseCan`. The capture
window was lengthened from 2.4 s to 4.0 s so it reaches the lift. The owner has not yet seen it in play.
