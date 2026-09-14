# Backpedaling follows actual travel

The previous gait advanced forward for every movement direction. A focused
regression sampled an actual retained foot through the animation graph at the
same phase: forward and backward travel both swept -0.02793851m. Both carrying
and empty-hand cases failed, independently of camera framing.

CharacterAnimator now derives forward/reverse gait direction from observed
velocity relative to body facing and eases through a reversal over0.12seconds.
The explicit phase wraps in either direction. The base walk/sprint and masked
carrying legs share that phase, so possession changes do not introduce a second
unrelated walking clock. The carrying arm remains outside the gait mask. Gait
speed calibration, role-speed thresholds, controls and physical movement remain.

Six affected gait contracts passed: two real-foot reversal cases, fixed carrying
grip with moving legs, two observed role-speed cases and full-roster stride
calibration. The ordinary PlayMode directional review then passed with216carrying
and219empty-hand samples over roughly12seconds each, including stops, forward,
backward, strafe, sprint and turning. Source recordings retain actual timestamps.
These checks do not claim complete strafe/turning animation polish or device QA.

Evidence is in backpedal-evidence and Logs/directional-locomotion-backpedal-v2.
The previously built DestinationEquipmentReview player predates this gait change;
its network proof applies to the throwing/equipment/restoration checkpointbc8a5f00.
A future combined player build must include the newer gait before player-level
claims about this change. Inday FPP remains explicitly deferred by the owner.
