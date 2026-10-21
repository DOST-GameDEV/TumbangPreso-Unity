# Block lesson: centred attacker, genuine near-can misses

The Block attacker now stands one metre behind the normal middle attacker
spawn: x0,z10 with the current court. It faces a slightly diagonal lane toward
the can and throws to a point outside the can's actual horizontal hit window.
The can is not made invulnerable and incoming slippers are never teleported.

The baseline actual guided route places the actor at x2 and fails the requested
centre-line assertion. Its capture is retained. Candidate1/1 passes in11.627s,
40s outer, guard null. Three real unblocked practice throws leave the upright
can alone after protection has expired. Measured nearest horizontal passage is
1.081m versus the current can hit window0.579m. Moving the student into that same
lane produces one real block; the lesson remains Block, so the three-block
completion rule is not shortened. The2.5s cadence and other lesson behavior stay.

[Before](evidence/before.png) and [after](evidence/after.png) are inspected native
960x540 world/tutorial captures from the student's camera. The attacker reads
behind the central spawn stripe, lined up toward the can rather than starting
on a separate parallel x2 lane.

## Execution and scope

Script compilation/import and graphics runtime ran in separate processes under
the unchanged cloud memory guard. The first import was refused before Unity
started because an outside Unity process was reported; no profile was altered.
A later native inventory found only the two already-recognized CLI/Hub helpers,
not a live Editor/player. One bounded admission retry was then admitted and
completed. Read-only blocker logging was added without changing the admission
condition. Original refusal and all later receipts are retained.

Only Block preparation/aim target in GuidedTraining changes. Existing body
contact, can contact, scoring, player roles, standard AI and network rules are
unchanged. This is offline native tutorial evidence, not a refreshed player,
actual-peer, hardware-performance or human-approval claim.
