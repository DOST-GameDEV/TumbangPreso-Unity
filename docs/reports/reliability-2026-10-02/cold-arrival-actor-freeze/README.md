# Cold completed arrival: actor freeze, October 2

A client entering an already completed arena can receive the final world snapshot before a ready countdown ever calls `SliceRunner.Begin`. The completed-arrival receiver correctly raised `MatchEnded`, but the runner subscribed only in `Begin`. All four actors could therefore remain active and unparked beneath the result board.

`SliceRunner.OnEnable` now subscribes to **only** the existing match-ended event. Round-start, intermission and skip listeners retain their existing `Begin` timing. The early listener is null-safe and removes before adding; `Begin` remains idempotent through its existing unsubscribe/subscribe path, and `OnDestroy` removes the listener. No network message, protocol, scene routing, camera, hero or gameplay timing changed.

## Focused native evidence

Three PlayMode cases use real component activation/destruction, four active `CharacterMotor` objects and the public `MatchRpc.SyncWorldSnapshotClientRpc` receiver. The runner has never begun. The test confirms the actual completed event and checks each actor's `RoundActive`, parked input, movement axis and held sprint.

- First baseline session **86027** stopped at compilation: this project's NUnit version lacks `Assert.Multiple`. No test cases ran. Its original fixture, compiler evidence and receipts are retained.
- One fixture-only correction collects the same per-seat violations into a list and asserts it empty. Repaired original session **46557** produced **1 causal failure / 2 passing controls**. All four actors still had active round state, unparked input, nonzero movement and held sprint after the completed event.
- Candidate session **76318** passed **3/3**. The completed arrival stops all four actors. Round-zero prestart snapshots retain free movement; destroying the runner prevents a later completion from reaching it. The completed case also checks that a repeated final packet does not replay the completion event.

This proves controlled native actor-state cleanup, not actual transport, rendered scorecard presentation, physical body travel or hardware input. The earlier real-peer run `49462` observed client **slot 1 / spectator false** and did not assert actor freeze; these tests do not retrospectively expand that claim.

## Source and preservation

Baseline source is `cce112e9b19069094710116dfcea13bf5c571c92`. The publication unit is exactly `Runtime/SliceRunner.cs`, new `Tests/PlayMode/ColdArrivalActorFreezeTests.cs`, and its metadata. Frozen original/candidate inputs are indexed in `inputs.json` and retained under main `Logs/cold-arrival-freeze1002-inputs`. Candidate source and fixture were unchanged during their native runs.

The serial GPU jobs used `tump-feedback-0930`, profile `cold-arrival-freeze1002`, DX11 at 960×540, a 1536 MB declared job budget, 2048 MB reserve and a 450-second ceiling. The first compile run used guard 26368; repaired baseline used 22020; final used 25224. All three guards completed profile/input preservation and released their leases. There were no remaining owned Unity processes when the slot was handed to the next task. Each run preserved zero existing profile files and one shared Editor input preference.

All **2,656** protected source, metadata and settings hashes remained unchanged. The three final inputs matched both main and qualification. Raw XML, job/guard receipts, input manifests, protected hashes and case outcomes are retained here; full Editor logs remain at the paths and hashes in `results.json`. No private profile contents or unrelated private art/source changes are included. There was exactly one fixture compatibility repair and no further repair loop.
