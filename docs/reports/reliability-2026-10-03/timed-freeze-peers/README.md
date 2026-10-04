# Timed Frozen recovery peer gate: setup refusal retained

First existing Cheska/Sean45141 actual two-player scenario on1003e failed with
zero trace rows on both peers. Both were admitted/entered arena/ready, but the
probe never confirmed its required Cheska/Sean actors. No timed-recovery defect
is inferred from absent effect data. Input restored, Runtime unchanged/leasefree,
all owned processes retired, unique profiles had no prior files.

Source: direct -tp-host commits Lobby.StartMatch before peer roster confirmation;
SelectLobbyPickServerRpc refuses changes after MatchInProgress. Exact initial
roster-mismatch cause is unproven. One diagnostic setup repair uses normal lobby
admission before start and recognizes lobbyjoin when selecting Sean's initial
profile. All existing trace-density/actor/2.5second Frozen/movement/phase assertions
remain unchanged. No game/hero tuning or roster regeneration. Candidate56995 on1003f also FAILED: zero actor/effect rows on both roles,
normal admission/ready/round1 observed, no prepared marker. Source05b2ee156,
protocol134. Input restored, Runtime unchanged and lease free; processes terminal.
The single setup repair did not resolve the gate. Do not rebuild or retry unchanged;
resume only from actual roster/mode/kit evidence that justifies a correction.
Neither run is an effect pass or a demonstrated game recovery bug.
