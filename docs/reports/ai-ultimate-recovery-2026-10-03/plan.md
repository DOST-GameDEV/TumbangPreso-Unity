# Defender recovery after an ultimate

Owner clarification: the defender stops after some ultimates, rather than every
status timer being broken. The earlier normal-match status probe is superseded
and retained as text; it hit the memory guard before trace/XML.

Causal baseline: SharedUltimatePhase.ClearActions calls RequireFreshActions,
which latches the defender's held Grab. AI.ReleaseAll clears held input without
Set(false), leaving the human release gate in place. DoReset holds Grab until
the can is upright, so the blocked reset and stationary defender can persist.
Native original: one causal rearm failure, two controls pass. An earlier fixture
failed to invoke AI.Awake in EditMode; its unrelated null errors are retained.

Candidate explicitly releases only bot-owned input. Suppressed-body AI still
leaves the human hero keys alone; human hardware still requires a real release.
Qualify the three input/handoff cases and an actual accepted Zack ultimate while
a real defender is channeling a real downed can, then require reset completion
and physical patrol movement. Small stage, no authored cinematic film claim.
