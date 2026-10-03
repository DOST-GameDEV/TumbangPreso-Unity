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
rejected with exit 2 before a player launch. Native compilation, default no-trace
player behavior and runtime budget interruption remain pending. This change
does not remove earlier files or qualify gameplay, networking or player timings.
