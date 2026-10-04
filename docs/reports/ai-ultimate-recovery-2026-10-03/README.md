# Defender reset recovery after an ultimate

The owner isolated the stall to a downed can plus an ultimate. The shared
introduction requires a fresh button release before gameplay actions resume.
AI.ReleaseAll previously called InputIntent.Clear, which removed held state but
intentionally left that human release requirement intact. The defender's DoReset
then held Grab continuously until the can stood up. Grab remained blocked, the
can never reset, and the defender stayed next to it for the rest of the round.

The bot now explicitly releases its owned buttons while stopped. A fresh reset
decision can therefore start its channel after the introduction. Human hardware
still needs a real release. A body controlled by AI during familiar possession
continues to leave the human's hero keys alone. No status duration, ability,
ultimate timing, networking protocol or can-reset balance change.

## Evidence

Original native handoff checks: one causal defender-Grab rearm failure and two
passing human/suppressed-body controls. Candidate3/3 passes in0.063s. This calls
the actual SharedUltimatePhase.ClearActions and AI.Update interruption path.
The first EditMode fixture omitted AI.Awake and produced two unrelated null
errors; those failures are retained separately, not used as causal evidence.

The physical PlayMode check starts a real AI defender's reset on a downed can,
then accepts Zack's ultimate through actual input and the shared phase. It must
leave the simulation hold, stand the can up and physically patrol again. Its
first fixture omitted the role assignment normally done by SliceRunner, failing
before the scenario; the corrected fixture derives roles from Match.DefenderSlot.
Final physical result passes1/1 in5.147s: the real can becomes upright and the
defender travels over0.65m after the accepted shared ultimate. Final exit0/no
guard; tree RSS3,293,880,320/container7,493,455,872bytes. Settings/profile restored
and frozen input hashes match. The three candidate input/control cases also
finish without a guard stop; only the physical fixture role setup changed afterward. Small stage without authored character models,
so this does not claim rendered cinematic, whole-map, actual-peer or human approval.

Compilation/import headroom stops are retained. Completed assemblies are reused
for separate bounded runtime checks after idle cleanup. Guards remain unchanged.
The earlier broad normal-match status probe is preserved as text: it stopped at
the memory guard before trace/XML and was superseded by the owner's precise repro.
