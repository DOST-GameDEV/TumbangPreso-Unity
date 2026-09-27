# Active Rework Checkpoint

Updated 2026-09-28. Branch: ASTRAReworks. Latest source checkpoint: `71396c97`.
Current protocol:87 in [NetSession](../Assets/TumbangPreso/Runtime/Net/NetSession.cs).
Read [AGENTS](../AGENTS.md), [task routes](README.md) and the [queue](TODO.md).
TODO is the work-status queue; this file records resumption and evidence boundaries.

## Current Unit

The async roster-catalogue implementation is pushed. Boot awaits one shared
Resources.LoadAsync request before visiting referenced art/clips. Direct fallback,
once-only missing warning and cancelled-request handoff are retained. Its focused
test is authored but NOT RUN. Source-only compilation stopped at storage preflight
before copying or compiling. Do not report a compile or runtime pass for this unit.
[Exact state](reports/stability-2026-09-27/loading-audit.md#asynchronous-roster-catalogue).

The preceding first-person preload unit passed one native case on its frozen
candidate:50 source meshes retained through the consumer cache, no actors created.
The teammate integration then landed cleanly as `68bc1008`, preserving its authored
assets, shared fixes and protocol87 compatibility. Only the eleven reviewed preload
paths differed from the incoming tree before the checkpoint update. The whole
merged candidate has NOT received a new native pass.

## Implementation And Qualification

- Loading: work-driven boot/menu/match curtains, async arena entry on every peer,
  retained menu/HOME/effect/mesh data and preparation ownership are implemented.
  The first HOME decoder's reproduced30-second timeout was fixed and its focused
  retry passed. [Current loading route](LOADING_AND_PERFORMANCE.md) and
  [per-unit evidence](reports/stability-2026-09-27/loading-audit.md).
- Networking: shared identity/authority, ordered receipts, scoped ready/countdown,
  ultimate identity/duration, sentry targets, timed/familiar recovery and
  simulation-clock aging have focused evidence. [Contract](SKILL_NETWORK_CONTRACT.md),
  [source route](NETWORKING.md), [exact coverage](reports/stability-2026-09-27/multiplayer.md).
  A cosmetic rework reuses stable IDs/hooks; a new gameplay mechanic needs a contract.
- QA: [19 deduplicated comments](reports/stability-2026-09-27/qa-comments.md).
  QA-01 title blur and QA-15 Sean/Cheska freeze remain unresolved. Login feedback now
  has native field-state evidence, not merely compilation; live service, sound,
  visual judgment and physical input are separate.
- No current successful player build, whole-player hitch table or blanket
  ranked/reconnect/cross-platform qualification. Do not infer these from local tests,
  source inspection or the incoming contributor's different candidate.

## Validation Environment

No task-owned job is active at this checkpoint. The latest storage preflight was
blocked; do not relaunch an unchanged blocked workload. The source-only candidate
needs about20.24MB of source plus32MB output budget above its5GiB reserve. A full
asset import/build needs additional headroom. Check actual space once when deciding
a new run, not in a polling loop. [Validation rules](TESTING.md).

Freeze a complete current candidate, isolate writable caches/profiles and use the
guarded runner. Do not reuse old partial overlays as if they were the merged source.
Run only changed cases; preserve fresh XML/hashes and the distinction between
source compilation, native state, rendered output and actual peers.

## Next Action

Continue actual loading/network/flow fixes within the latest owner scope. When
storage permits, qualify the roster handoff and the merged candidate. Keep pushing
coherent owned units; do not stop at a passing case or create a validation loop.
Keep temporary ownership notes and private overlays out of commits.

## History

The complete341-line checkpoint and every older receipt it referenced remain in
[ledger history through71396c97](archive/ledger-through-71396c97-2026-09-28.md).
Its stale counts/protocol literals are historical. Earlier instruction/method context
remains in [the2026-09-27 snapshot](archive/snapshots-2026-09-27/README.md).
