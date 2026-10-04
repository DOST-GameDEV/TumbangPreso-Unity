# Cold completed arrival freezes all four actors on actual Windows peers

One first-run short custom Hero1-round30-second match completed naturally with two
human-origin seats and two bots. Frozen Windows1002g/source248836d1e/protocol131,
Runtime421af2276ca14c6a304890062fefacfe22974ef83bc3d4b397b8c0e32d444755.
Root session48099 passed exit0; no scenario, build or fixture repair/retry.

The same client invoked the current result MAIN MENU action, reached current HOME,
then used public StartClientAsync/WaitForConnectionAsync and trusted seating to
enter a new arena scene. Host remained ended. Fresh MatchEnded and RecordReady
counts increased; record identity/scores and visible result board matched. Normal
MAIN MENU gives up the former seat, so this is not retained-seat recovery.

The NEW runtime observer records every installed actor after terminal cold arrival:
RoundActive=[false,false,false,false], Parked=[true,true,true,true],
Sprint=[false,false,false,false], MoveAxis.sqrMagnitude=[0,0,0,0]. The actual client
was slot1/non-spectator. This supplies the actor-state transport acceptance missing
from the earlier1002e49462 board/record proof; old receipts remain unchanged.

Both owned players18264/24636 retired. Input and two fresh profile seeds restored,
exclusive GPU lease released, runtime hash unchanged. Raw receipts, logs, seeds
and before/after input evidence accompany this report. Port9080/9081;120s ceiling;
existing serialized2048MB/2048MB-reserve pool. No AllBots, autorematch, force score,
force finish, SDK control, physical clicking, WAN, live career settlement or full
shipping-default tournament acceptance is claimed.
