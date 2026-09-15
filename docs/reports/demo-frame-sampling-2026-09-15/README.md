# Separate the native review's polling from gameplay sampling

The native UI review previously searched all scene Selectables every frame while
waiting for match results. That is unnecessary measurement overhead. It now checks
at10Hz while continuing to sample every gameplay frame. An explicit --frame-poll
control retains the old search frequency for comparison. Per-frame structs record
timestamps, remaining round time and GC counters; CSV strings are written only
after the sample closes. Normal gameplay does not install this diagnostic.

Two sequential runs used the same v7 native binary and the same UI/match route.
Both passed actual loadout/mouse/save, both-mode short matches, results/rematch and
profile preservation. At1366x768 on Ryzen5 2600/RX6600, old result polling performed
4942/5366 searches in the Classic/Hero windows;10Hz polling performed274/274.
GC counters changed198/83 versus196/81. Recorded maxima were40.0/43.3ms versus
36.7/33.3ms. None coincided with a collection-counter change within one sample.

This removes a clear harness confound. It does not explain the earlier123ms outlier
or prove a gameplay performance improvement: the bot workload is not a controlled
seeded comparison and collection completion is not the whole incremental-GC cost.
The broader performance task remains open. Do not tune gameplay or assets from
these small samples. The owner subsequently prioritized compact picker typography.
