# Fetch query allocations

Nemu's Fetch availability check ran FindObjectsByType on every eligible query.
It now reuses the existing BotSlipperInventory, which retains the native slipper
snapshot between births/destruction and filters current object activity. Owner
and loose state are read live. The disabled-component, empty-hand and attacker
eligibility behavior stays intact; Fetch delivery, cooldowns, art and other kit
rules are unchanged. This is an optimization, not completion of the Nemu kit.

## Native evidence

Unity6000.5.8f1 Windows D3D11; guarded isolated feedback-fetch-query-1001 profile.
Source5dcb6a54c plus the exact owned candidate;589 input hashes unchanged after run.

- Baseline3cases:2passed/1failed.100warmed actual CanActivate calls generated
  200GC.Alloc events; the same-thread Unity ProfilerRecorder was calibrated by a
  deliberate4096-byte allocation before measuring the query.
- Fixed3/3passed. The same100warmed actual calls generated zero allocation events.
- Live ownership, inactive/reactivated objects, disabled components and thrown
  state retain the correct eligibility. Destroying the old shoe and creating a
  new loose owned shoe refreshes availability correctly.
- No fixture repair or repeated unchanged suite. No loading path changed.

Exact XML/input/CSV receipts are retained in fetch-query-checks. Full logs remain
in the isolated checkout Logs/feedback-0930/fetch-query-*.log. This is a measured
allocation reduction in repeated native eligibility calls, not a player FPS,
whole-match performance, hardware or live-peer result. Broader Nemu Wiki work
remains open.
