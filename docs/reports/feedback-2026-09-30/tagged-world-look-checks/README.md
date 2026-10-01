# Tagged replay world-look context

Harry reported that tagged replay used different shading from first-person play.
The replay already had ColourGrade and WorldOutline, but lacked the explicit
WorldLookCamera marker used by scoped map shader globals. BuildView now adds that
existing marker. No lighting retune, gameplay or protocol change.

## Evidence

Unity 6000.5.8f1, Linux OpenGL graphics, isolated named profile, protocol 124.
Baseline real accepted replay failed: world-look weight 0 versus gameplay 1,
architecture 0. Final focused native case passed 1/1: weight 1 versus gameplay 1,
architecture 1. It also checks restoration after the off-screen render and
exclusion of an unrelated portrait camera. Frozen final inputs match source.

The first final attempt stopped at the existing memory guard without runtime
results. One bounded tooling repair retires idle import workers before map load
using the documented EditorUserSettings delay, restoring it after reset. Final
run completed in 41.72 seconds; OOM counters remained 11 / 6 kills. Guard unchanged.

## Limits

The actual replay target captures document map shader adoption, not contact
framing: actors are outside these early sampled frames. A bright black/white
patch is visible on distant props and is not qualified as repaired. This does
not resolve the separate red/cyan foliage report. No new player build, actual
peers, all-map visual comparison, cove comparison or human approval is claimed.
