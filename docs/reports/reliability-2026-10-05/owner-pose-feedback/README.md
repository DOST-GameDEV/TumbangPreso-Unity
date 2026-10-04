# Keep accepted owner movement echoes out of reconciliation

The host relays a remote owner's accepted movement position back after a network round trip. The client previously compared that earlier position against its current prediction and treated the round-trip distance as an error. Both a 1.6m echo and a 4.2m echo snapped a fresh local owner back to the older position; the same path could replace current velocity. This explains a concrete feedback mechanism that can rubber-band non-hosts even with smooth rendering.

Protocol150 adds one ownerCorrection flag to SyncUnit. Accepted SubmitMove echoes carry false; authoritative correction and snapshot callers retain true by default. For a local accepted echo in the same movement epoch without host-owned edge recovery, the motor observes grounded/flight evidence but preserves current position and velocity. Explicit corrections, teleports and observed seats retain their previous behavior. Host movement validation and distance budgets are unchanged.

## Evidence

Baseline866ccc80e/protocol149: native Unity9040/session53051 reproduced both owner regressions through the actual SyncUnit receiver, while explicit correction, new-epoch teleport and observer controls passed. Candidate12424/session57811 passed identical five cases plus an existing Voodoo/status replication control:6/6, no skips, exit0. Every packet fixture asserts its exact current schema length and accepted stamina value to prevent a rejected packet from producing a false pass. The older Voodoo fixture lacked the already-shipping Zapped float; that omission and the new flag were corrected without weakening its status assertions.

Both jobs terminal. Isolated settings/shared input/editor preferences/QualitySettings restored;19,275 inputs frozen,202 owned GUI metadata rewrites preserved and restored exactly with no remaining deltas. Pre-existing Auditor dirt preserved. Full local maps/logs are in Logs/owner-pose-feedback1005; compact XML/results and source snapshots are retained here.

This is causal native packet/reconciliation evidence. A matching protocol150 full player build and two-machine controlled latency/movement check remain required. Earlier3947/protocol148 online and visible-UI full matches do not qualify the new wire format. This report does not claim every observed tester delay is resolved or establish human feel.
