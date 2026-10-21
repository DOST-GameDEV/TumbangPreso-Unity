# Current144 release memory attempts

The owner requested that conservative RAM estimates stop blocking useful tests
and that work be split when necessary. The former6144MiB admission budget plus
1536MiB reserve was an estimate, not a measured minimum Unity requirement. Two
bounded local headless release attempts tested a smaller4096MiB admission estimate
while keeping the1536MiB runtime reserve and600-second deadline unchanged.

Both used Unity6000.5.8f1, full quality, BuildOptions.None, one job worker and one
GC helper. No Development or ConnectWithProfiler flags, parallel heavy job,
Desktop replacement, unrelated process kill or PC native launch.

## Actual results

| Attempt | Starting available | Sampled tree peak | Sampled minimum available | Guard result |
| --- | --- | --- | --- | --- |
| First |6321MiB |4849.06MiB |1256MiB |Interrupted125 at1220MiB below1536 reserve |
| Local importer preference repair |5998MiB |4265.21MiB |1394MiB |Interrupted125 at1394MiB below1536 reserve |

Neither produced an artifact. Both guards are terminal, preservation completed
and leases free. Only their verified owned Editors were stopped. The first has91
one-second samples and the changed repair41. Aggregate working sets can count
shared pages multiple times; these are sampled working sets and available
physical memory, not private-commit peaks or packaged performance acceptance.

The repair backed up this dedicated checkout's UserSettings/EditorUserSettings.asset,
set desired and standby importer counts to0, then restored its original3/2 values
byte exactly after termination. Unity still launched AssetImportWorkerHW0, so
the setting did not eliminate all import-worker memory. No third unchanged
resource retry was launched. Unity documents the desired count as an aim rather
than a guaranteed actual count:
[Unity6.5 API](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/EditorUserSettings-desiredImportWorkerCount.html).

## Source and preservation

Actual shipping Git headc80e4ae32c1a213fdc943ab4568fc158aa23f7c0 has the tracked
build input tree of coherent release73dd6f89c68bd21124d25002247e5429457e362f.
It contains checked focus/input, record normalization and previous144 fixes.
All208 prior importer files remained raw-byte unchanged. Protected artwork
metadata stayed unstaged. The two inspected first-attempt raw input deltas are:

- GraphicsSettings.asset: GameBuilder's required always-included DanteBarrier
  and BlockyCloud shader entries, exactly two additions, retained and preserved.
- ProjectSettings.asset: raw line-ending serialization only; normalized Git
  content is unchanged. This retained serialization is recorded, not discarded.

The first froze19,661 build inputs. The repair reused unchanged hashes and updated
only those two inspected generated settings hashes; all19,661 then stayed
unchanged throughout the repair. Quality settings were restored byte exactly
after both attempts. Raw maps, receipts, samples, summaries and settings snapshots
are preserved with exact hashes in manifest.json.

Current same-artifact two-machine lobby/readiness/match/saved-results and recovery
remain open. This report is evidence of resource limits and preservation, not a
successful build, compilation gate, gameplay qualification or tournament readiness.
The independent PC owns UI/Friends/preload/server source work; local focused
native cohorts can continue within admitted memory. No PC slot release is inferred.
