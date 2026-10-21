# Retained abandonment context in failure-bundle diagnostics

FailureBundle's NETWORK section omitted MatchAbandon's retained diagnostic. After normal simulation retirement resets live Match/Round state, the MATCH section reports that reset state. The abandonment console message is a Warning, which the bundle's recent-error queue excludes. A bundle prepared afterward therefore lost the classified session-end cause and original round context even though MatchAbandon intentionally retained them.

The NETWORK formatter now includes MatchAbandon.Diagnostic when Cause is not None. This uses only the classified retained cause and captured round/total context; it does not include RawReason, player identity or a private profile. The no-cause path remains unchanged. File destinations, storage and all gameplay/transport cleanup behavior are unchanged.

Two focused guarded native EditMode cases exercise the exact formatter used by Write's NETWORK section. A controlled director receives an actual ApplySnapshot for round 3, MatchAbandon.Note captures its context, then ResetForNewMatch and Clear retire live state and release authority. The formatter must retain the previously captured diagnostic and exclude a private raw-reason marker. The other case checks no abandonment line is produced when Cause is None. Global service and abandonment properties are restored. No normal-profile bundle file is written.

Baseline 2: one no-cause control passed and one retained-diagnostic case failed. The actual summary omitted `RemovedByHost: ABANDONED at round 3 of 8; this peer may no longer resolve anything`. Final 2: both cases passed against the unchanged fixture. No fixture or tooling repair was needed. This accepts diagnostic text output; it is not a gameplay or real-peer behavior fix.

Baseline session 15524 and final 51416 used Unity 6000.5.8f1 EditMode, serialized CPU pool 1536MB/2048MB reserve, 450-second limit and named `failure-bundle-abandon1002` profile. Both preparations finished exit0 before launch and checked exactly three frozen source/fixture/meta inputs. Fresh XML, guard receipts and logs are retained. Qualification's existing protocol 129 source remained protected; these cases do not claim protocol 130 peer validation or whole-branch compilation.

Both guards completed restoration without a held lease. All 1290 protected source/private hashes and the three own inputs matched after final. Shared Editor input preferences and named-profile files were restored according to the raw guard receipts. No SDK, input-event framework, normal persistent bundle write or additional broad test ran. No task-owned Unity/player process remained when the slot was released.

Original FailureBundle SHA256: `DEDBD39420E518ACCFF05F5A3CC33387C0EFDAB1915F24CD4F95E30E1C038266`.

Final source SHA256: `1C9B956235CD316EDC8A63326C5103013C7CD19FF91D3B36AA94DD97CB4DB2F6`.

Unchanged fixture SHA256: `BD3F1BDCC0659B9CF0A3058BF1A77C66FE9917E37E18498A75786DD29ECE539A`; meta `1D5529BFC8F8209E0E6DE3F6B46670D3FC9F192D6E5BEA3910FC07C2E4670A07`.

The raw XML/logs/guard receipts, separate counts and frozen/preparation/protected hash manifests accompany this report. Actual exported-file behavior, standalone inclusion and full competition readiness require their own evidence; no such acceptance is implied here.
