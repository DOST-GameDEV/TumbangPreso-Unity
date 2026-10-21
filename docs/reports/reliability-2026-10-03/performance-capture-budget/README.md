# Keep routine performance evidence compact

The prior development-player run retained 127 timing windows but also wrote
14,434,505,500 bytes of binary profiler data before its outer timeout. Native
inspection attributed the large Nemu peaks to profiler collection. Routine
timings must not automatically incur that collection cost or disk growth.

Performance review now disables binary logging and the Profiler by default,
while retaining existing frame timing CSVs, contexts and action receipts.
Explicit `--performance-binary` requires `--performance-only --review-hero ID`.
Binary runs check aggregate raw output once per second and stop at 512 MiB,
retaining partial evidence with a failed completion receipt. Sampling and final
buffer flush can exceed that threshold; it is not a strict byte ceiling.

Python compilation passed. Both missing-route and missing-hero requests were
rejected with exit 2 before a player launch. Full runtime Roslyn compilation
against installed Unity references passed with existing warnings. This does not
establish a native or player pass.

The new focused native case checks one retained CSV sample, context and result
files with no binary logging or raw output. First admission was refused by an
exclusive GPU lease. After it cleared, the one bounded retry launched but
stopped before fresh XML: another Main Editor started and memory fell to1002MiB
below1024 reserve. Only owned Editors19552/1960 were stopped; the guard completed
preservation and released its lease. No native case passed. Both receipts and
changed-input hashes are retained here. The current player build was refused by
a shared lease and never launched; no new artifact exists.

Default no-trace player behavior and runtime budget interruption remain pending.
This change does not remove earlier files or qualify gameplay, networking or
player timings. Older binaries retain their original capture behavior; this fix
requires a matching rebuilt player.
