# Recover a stable queue after host startup fails

Matchmaker.Evaluate consumes its cached reevaluation deadline. A failed join
already schedules another pass, but a failed host startup left the deadline at
infinity. Once the rating band reaches its maximum and ServerQuery sees the same
list, neither widening nor ServersChanged produces another evaluation. The queue
remains Searching after a transient failure with no scheduled path to try again.

The current host attempt now schedules cached reevaluation using the existing
MatchmakingCandidateCache.RetrySeconds value of 30. Existing request ownership
guards run before that write. Successful, cancelled and replaced attempts keep
their existing deadline and busy ownership behavior. No new query, rule constant,
protocol change or networking framework was added.

## Native evidence

Unity 6000.5.8f1 ran the focused MatchmakingWireTests cases in the isolated
tump-feedback-0930 checkout. The main run_unity_job.py delegated to that project's
guarded runner. All launches used profile queue-host-retry1002, batchmode,
nographics, EditMode, CPU budget 1536 MiB and reserve 2048 MiB. Wait/run ceilings
were 300/450 seconds, and parallel mode was not enabled.

- baseline/: compilation stopped before tests because the new fixture lacked
  System.Linq for its Contains assertion. No XML or executed cases came from this
  launch. Original logs and pool receipt are retained.
- One bounded fixture repair added that import. Runtime source did not change.
- baseline-retry/tests.xml: 5 cases, 3 passed, 2 failed, 0 skipped. Refusal and
  startup exception both left the actual HostAsync deadline at infinity. Successful,
  cancelled and replaced attempt controls passed.
- final/tests.xml: the same repaired fixture, 5 passed, 0 failed, 0 skipped. Current
  failures schedule the existing retry interval. Success and cancellation add no
  retry; an obsolete callback preserves the replacement ticket's deadline and busy
  ownership.

The fixture invokes the real HostAsync method with controlled startup delegates
and starts at the widest rating band. Dormant account/network components keep
initialization inactive. The accepted control has no beacon, so publication reaches
no SDK call. There were no endpoint calls, live allocations or actual service
recovery attempts.

## Frozen inputs and preservation

Only Matchmaker.cs and MatchmakingWireTests.cs were overlaid. Exact hashes are
retained for the original fixture, repaired causal baseline and final candidate.
Runtime candidate SHA256 is
16516C0B92543B0C77100D673C1139057FDE14E7CA38068B910CB77DDDED7279.
Repaired fixture SHA256 is
4509B3183DBE1AC7FE1268CB97D8843359DEAB18218F8D17B548CE4B74C3C2CB.
Main and qualification copies matched after testing. All 22 unique protected paths
retained their hashes, including the acknowledged action/input fixes, panel and
NetSession sources, private assets and protected metadata.

All three guards completed preservation and retained no pool lease. Each restored
zero existing named-profile files and one shared Editor input preference. Owned
Unity processes exited before the validation slot was released. The launch and
preservation identities are recorded in acceptance-summary.json and raw receipts.
There was one fixture repair and no tooling/product repair or resource intervention.

This future seventeenth fix was not copied into the frozen sixteen-fix player.
Its Runtime SHA256 539deda0cc4e225dcf653c55eacf70e5af4218bca12e9ff34f8d2eebea1ead74
was not modified by this unit. There is no new player build, live Relay recovery,
physical operator input or whole competition qualification claim here.
