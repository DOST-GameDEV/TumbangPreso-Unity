# Issued bot hops lost before physics

ENG-0930-HOP, source10192f7e1 plus one producer buffer call and native fixture.

StepHop deliberately releases Jump after one render update. That release can
happen before CharacterMotor's physics step, cancelling an already issued hop.
The producer now buffers the issued Jump press through the existing InputIntent
bridge. Held-state release still occurs on the next render update; the physical
consumer retires the buffered edge. Hop eligibility, chance, intervals and physics
are unchanged. No hero-specific logic, animation or loading changes.

## Native evidence

Unity6000.5.8f1 Windows D3D11 PlayMode, authored Eskinita, isolated profile
feedback-0930-bot-hop. The test exercises the actual eligibility/chance branch,
observes its issued press, then runs the next real StepHop before motor physics.
Its controlled random-state search is bounded and the previous state is restored.

- Baseline1case fails: the render release cancels the issued Jump edge.
- Fixed1case passes: held Jump releases, its press survives, and actual motor
  FixedUpdate retires it.
- 556frozen overlay inputs, unrelated main dirt excluded, no non-metadata drift.

Raw XML/manifests are in checks/bot-hop; full logs remain in the isolated checkout.
This qualifies producer-to-physics input delivery. Whole-match jump rates,
presentation quality and player FPS are not measured by this case.
