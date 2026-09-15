# Rejoining client round authority

A local client clock could call EndRound and BeginIntermission before an accepted
host snapshot. This fired world-mutating intermission handlers on the client and
left IsWarmupBuffer set when the next live snapshot arrived. C4's F4 independently
observed a reclaimed client staying in warm-up; this joining/UI defect is explicitly
outside its reserved request-safety audit.

The focused pre-fix reproduction failed with warmup=True, active=False and one
intermission event on a client. RoundDirector now clamps its presentation clock at
zero and only resolves expiry with authority. Accepted snapshots reconcile the
client's warm-up flag from the host's match/round state, including a late join into
an existing intermission. No round-start/intermission event is replayed, no wire
contract changes, and no reserved MatchRpc/NetSession files are edited.

Three focused PlayMode checks PASS in Logs/client-round-boundary-v2.xml,
guard8a1aed5a1867: client expiry, preserved host expiry, and snapshot transitions
covering late-join buffer, repeat snapshot, next live round, match end and pre-start.
The failed baseline is Logs/client-round-boundary-v1.xml, guard398c896c7b17.

Native qualification PASSED in both modes. The opt-in NetRoundBoundaryProbe samples real
state and restarts the client's transport/reloads its arena during round1; it never
injects a snapshot or writes the clock. The dedicated runner uses two hidden
Windows players, 150ms one-way delay, explicit2-round/30-second review rules,
isolated named profiles and ordinary readiness. It requires a real host buffer,
a sustained live round2, no client intermission events and no warm-up during live
play. Both Classic and Hero Strike passed with zero invalid warm-up samples and
zero client intermission events. Classic host/client buffer samples:49/49;
Hero Strike:50/50. Each client recorded47samples in the second live round after
the actual same-process rejoin. The host fired exactly one intermission event.
Shared input preferences were unchanged and every owned process exited.

Windows build: Builds/round-boundary-v24/TumbangPreso.exe,1135MB/52s,
guard40fe66b8096f. Runtime SHA256:
e08238cfe7b6d8880e4c55c0ab56ba98a66630d36af9f974240f72f3fb79f0fc.
Receipts and sampled traces are in classic/ and hero/ beside this report. Failed
and passing PlayMode XML are retained here too. This is two processes on one PC
with a shaped loopback link, not a physical two-PC venue Wi-Fi certification.
No manual screenshot assertion is made; the HUD directly consumes the measured
IsWarmupBuffer flag. This does not close C4 or the full reconnect/session matrix.
