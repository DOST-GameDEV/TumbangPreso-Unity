# Default Hero match, October 2

Two real Windows players completed all eight default Hero rounds on Ilalim Ng Tulay. Both logged the natural match-end event, winner seat 2, and scores `40 / 40 / 3500 / 3035`; both final reports show round 8 inactive. The host logged all eight round starts. Seats 0 and 1 were human-owned with no scripted gameplay input, and seats 2 and 3 used ordinary bots. The host handed seat 1 to a bot after the client's scheduled exit, after the match had ended.

The tested build came from `2c3f39dc97756b23f12239bdd7bba0429ab2e352`, with Runtime SHA-256 `539deda0cc4e225dcf653c55eacf70e5af4218bca12e9ff34f8d2eebea1ead74`. The existing direct LAN harness ran D3D11, Balanced, 1920x1080, a 60 FPS cap, eight 90-second rounds, no tournament preset, and no rule modifiers. The client observation ceiling was 900 seconds, with the existing host settle margin at 913 seconds. No full-match rerun was performed.

## Original verdicts and corrected interpretation

Both original reports remain unchanged and false. `result.json` retains the generic harness's failure because it requires an active round at the tail. `completion-observations.json` additionally required a saved career record: the fresh host profile had empty History/Queue and the client had no career file. Their paths and JSON schema were correct. `completion-observations-addendum.json` records the separate natural-end observation from the actual `[Slice] match over` lines and matching peer reports; it does not replace either original verdict.

The missing career record does not establish loss in the normal menu flow. In this direct route, `NetBootstrap` sets `SceneFlow.Networked` only in its lobby branch. `MatchStatsCollector.BeginMatch` requires that flag as well as network authority when setting `Online`; `CareerStore.Record` declines an offline record. Consequently this route did not qualify career persistence or the operator's results-board/rematch flow. The addendum's preliminary empty-account identity concern was subsequently checked: `PlayerAccount.ReadLocal` supplies a saved account ID or the local identity token. No normal-flow empty-ID defect was established, and no identity fix was made.

## Performance and limits

Host live sampling measured 59.63 FPS average over 43,218 frames / 724.772 seconds, with a 107.07 ms maximum frame. Client sampling measured 59.65 FPS over 43,196 frames / 724.109 seconds, with a 109.43 ms maximum. These observations do not identify the cause of either long frame and do not establish an improvement over another build.

This proves a natural default Hero finish and consistent peer winner/scores in the tested local two-process configuration. It does not qualify physical input, WAN behavior, career persistence, the visible end board, rematch, or menu recovery. No live service actions were called. Both owned players exited; preservation records shared input unchanged. Private profile files are excluded from this report.

## Retained evidence

- `host.log` / `client.log`: actual natural-end events and runtime initialization.
- `host.txt` / `client.txt`: final match state and live frame observations.
- `result.json` / `completion-observations.json`: original false verdicts.
- `completion-observations-addendum.json`: separate source-grounded interpretation and original hashes.
- `launch-contract.json`, `preservation.json`, `processes.json`: artifact, launch scope and cleanup.
- `evidence-inputs.json`: copied evidence provenance and hashes.

Source references: `Runtime/Net/NetBootstrap.cs`, `Runtime/MatchStatsCollector.cs`, `Runtime/Net/CareerStore.cs`, and `Runtime/Net/PlayerAccount.cs`, under `Assets/TumbangPreso` in the tested source. The build receipt remains in the release checkout at `Logs/competition-candidate1002b/build-receipt.json`.
