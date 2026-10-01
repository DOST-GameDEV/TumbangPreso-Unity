# Current114player build and failed direct-peer check

## Build: succeeds

Committed source239050ac6, including current Nemu/Haunted, Airburst Wiki/court
reach, familiar pose and bot companion observation units. The isolated imported
candidate stays at native Git baseaecc0ee2 with explicit committed overlays;
this is not a pristine-source release certification.

15422Assets/Packages/ProjectSettings inputs were frozen before build. Unity
6000.5.8f1 Windows builder succeeds,2141MB,155s. Executable/data/runtime assembly
exist at internal Builds/feedback-current-player-1001/TumbangPreso.exe. Runtime
SHA256:36cdbf3c41afbb287e4f4890046a26373b5a4e082a478705fdd7a3a61182da21.
After-build comparison finds no changed input, including no C# drift. Existing
native preparation settings were already captured in the freeze. Old Desktop
and frozen103/98outputs are preserved. Build job50183 is terminal and its guard
preserved named-profile files/shared input preferences.

## Real peers: fails

The exact same binary runs two actual D3D11 localhost UDP processes through the
existing guarded run_demo_lan driver, with separate profiles, hidden windows and
batch mode suppressing external UGS sign-in. The client samples150s, host163s.
Both report114and matching runtime SHA. The evaluator correctly fails:

- Host never reaches round1: round0/inactive, no score progression.
- Client loses its connection and reports offline HOST instead of CLIENT.
- Neither crosses a real round boundary. Structural state does not agree.

No connected-match, successful current-peer, release-ready or whole-feature claim.
This failed result replaces any merely plausible expectation of a working link.
The driver stops only its own players, restores profile files and confirms shared
input preferences unchanged. Peer job34576 is terminal. Exact reports/logs and
preservation receipts are retained in current-player-checks.

## Investigation boundary

Logs show host approval for self peer0, then peer1disconnect without a logged
peer1approval. Client reports ClosedByRemote. Arena warmup reports11.31/11.97s.
NetBootstrap's client route treats StartClientAsync("connecting") as enough to
call SceneFlow.Go immediately. Production join callers instead await existing
NetSession.WaitForConnectionAsync, which requires both connection and applied seat.
NGO2.13.1's approval buffer defaults10s; transport silence budget is8s.

These are specific startup sequencing/deadline hypotheses, not a demonstrated
complete root cause. Preserve the established8second outage behavior unless
stronger evidence justifies a change. Correct/check admission handoff using the
existing waiter before retrying this unchanged player. Loading belongs to the
friend: do not modify SceneFlow/preload/assets or author a replacement loader.
