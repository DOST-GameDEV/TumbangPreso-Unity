# Block lesson: attacker behind the middle spawn

Latest Wiki Feedback asks the Block AI attacker to stand behind the middle
attacker spawn, looking as if aiming at the can while its practice throw misses.
Current source stands at x2 on the spawn line and throws straight down x2.

Move only this lesson's actor to x0, one metre behind the normal middle spawn.
Aim its real throw along a slightly diagonal near-can lane, with a clearance
beyond the can's authored horizontal hit window. Face that lane. Preserve the
student's defender role, real body-block interaction,2.5second throw cadence,
three-block completion, other lessons and all ordinary AI/match behavior.
Do not make the can artificially invulnerable or teleport an incoming slipper.

Own only the Block preparation and target expressions in Runtime/GuidedTraining.cs,
a focused addition to Tests/PlayMode/TutorialLessonHonestyProbe.cs, and owning
documents. Record actual current spawn/capture and expected baseline failure.
Then verify behind-centre spawn, actual unblocked throw passages with an upright
unprotected can, and successful student interception. Use the real tutorial
route. Separate script import from runtime up front to respect limited RAM;
one Low graphics case per process, same guard, no user PC.
