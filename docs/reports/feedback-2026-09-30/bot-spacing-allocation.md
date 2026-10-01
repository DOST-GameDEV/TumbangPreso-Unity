# Bot spacing query allocation

Each ThrowSpot reconsideration previously built a new list and backing array
for current rival bearings. Retain a per-brain four-entry scratch list, clear it
on each query and preserve the existing shared-board/self/expiry rules. This
does not change spacing scores, claim lifetime or bot difficulty.

Windows Unity6000.5.8f1 native PlayMode baseline measures200GC.Alloc events
over100 warmed queries with three rival claims. The same final query records0.
All four final cases pass: calibrated allocation, own/expired exclusion without
board mutation, refreshed/empty-board reads and independent simultaneous readers.
All697frozen inputs remain unchanged. New script metadata imports natively.
Jobs81100/67847 are terminal; guarded profiles/shared input preferences preserved.
No native fixture repair was needed. An initial scratch-script shell quoting error
occurred before either run and was corrected by saving the script directly.

[Raw XML and input manifests](bot-spacing-checks). This measures a bounded
native query cost, not an FPS improvement or whole-match/player/peer performance.
No loading, authored asset, input, gameplay or protocol change.
