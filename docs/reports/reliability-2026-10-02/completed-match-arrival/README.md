# Completed-match arrival, October 2

A client joining after the host's match ended could receive the final scores without opening final standings. The receiver only raised completion after it had seen a live match, and its pre-start guard rejected the same finished snapshot when the client had locally started its arena. The host's snapshot recovery also omitted its retained result record.

`MatchDirector` now emits completion once for a validated completed arrival. `MatchRpc` allows that path only for a networked client in an installed four-player arena whose lobby remains in-match, with an inactive terminal snapshot; round-zero/pre-start snapshots retain their existing guard. Nonfinite clocks and invalid round ranges are rejected before either director is updated. `HostSyncPeer` resends a matching finished record to the arriving peer through the existing reliable `MatchRecord` message. The retained record must agree with current scores, winner, mode, map and round count, and is unavailable during loading, live play or a reset. No new wire message or protocol version was introduced.

The client completion event does not author a local result. `MatchStatsCollector.OnMatchEnded` retains its host-authority guard; the existing `RecordReady` route refreshes the results once the authoritative record arrives.

## Native causal evidence

The controlled EditMode fixture calls the public `SyncWorldSnapshotClientRpc` receiver with real dormant Unity components and a fake client authority provider. It creates no transport, scene load, career dispatch or live SDK request; telemetry is cleared and restored for the fixture.

- Original product code: **7 cases, 5 passed and 2 expected failures**. Both cold-arrival variants received zero completion events instead of one. The plain-lobby, pre-start/live-end and three invalid-snapshot controls passed. The original XML is retained as `baseline.xml`.
- Candidate: **12/12 passed**, including the same seven cases and five final-only retained-record cases: matching, wrong winner, live, loading and previous-match/reset. Repeated terminal packets emit no duplicate event, and the client collector emits no local result. `final.xml` is the fresh native result.
- Baseline native duration: 0.1951392 seconds. Final native duration: 0.1287548 seconds. Zero fixture or tool repairs were used. Absence of the new private record selector was never treated as baseline evidence.

Both runs used the guarded serial CPU runner in `tump-feedback-0930`, profile `completed-match-arrival1002`, 1536 MB job budget and 2048 MB reserve, with a 450-second timeout. Baseline session 81337 used guard 16664 / Unity 23676; final session 34079 used guard 23956 / Unity 22832. Both completed preservation and released their leases. The final process check found no remaining Unity process. All 1,462 protected source/settings hashes and the three frozen candidate hashes remained unchanged.

## Source and limits

Baseline source is `39a3bc430e2233ab5d22839980d676505e1b9568`; exact original/candidate hashes and source snapshots are indexed by `inputs.json`. Only `Runtime/MatchDirector.cs`, `Runtime/Net/MatchRpc.cs` and the appended `CompletedMatchArrivalTests` in `Tests/MatchStartRaceTests.cs` changed under `Assets/TumbangPreso`. Existing metadata stayed unchanged. Other ACK, queue, result eligibility, private art/settings and Cinder transport files were protected.

This proves the controlled public snapshot transition and retained-record eligibility. It does not prove an actual late-arriving network peer, WAN delivery, rendered results-board layout, operator rematch, or career persistence. The existing record serialization/delivery route is source-inspected; these tests do not send it over a transport.

`baseline.xml`, `final.xml`, job receipts, guard logs, `acceptance-plan.json`, `inputs.json`, `protected.json` and `result.json` retain the acceptance and preservation evidence. Full Editor logs and frozen source copies remain in the qualification/main `Logs/completed-match-arrival1002*` folders respectively. No private profile contents are included.
