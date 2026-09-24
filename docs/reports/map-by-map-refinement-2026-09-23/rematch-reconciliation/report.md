# Chosen-map rematch: asynchronous fixture reconciliation

Sourcec85614a42. The historical September15 runtime bug is already corrected:
MatchResult advances/synchronizes map selection before SceneFlow.StartMatch.
Current offline entry uses UX-1 HubLoading.LoadSceneAsync. The old test still
checked the actual scene after one frame, a stale synchronous assumption.

Fresh baseline failed at that actual-scene assertion after the selected-map
assertion passed. One bounded correction waited for loading. The next run reached
BayanPlaza and passed both original map assertions, then failed an extra assertion
I added incorrectly: RoundDirector belongs to DontDestroyOnLoad, not the arena.
This was an agent test-design mistake, not a product rematch failure. Both XML
results are retained. No production rematch/authority/loading code changed.

Final test source waits for the desired active scene and a ready gate with a
30second/6000frame ceiling, retaining the original selected-map, actual-map and
ready-gate assertions. The invalid round-scene ownership requirement is removed.
There is NO fresh green result for that final source. Fixture allowance exhausted;
do not loop. Run it in the final regression. The existing run already establishes
that actual BayanPlaza was entered, so later ledgers must not call the one-frame
failure proof of an ongoing runtime map-selection defect.

This does not qualify the latest binary's network rematch/reconnect. That remains
P7. Old native two-peer evidence and all historical failure records are retained.
Known generated churn backed up/restored; no intermediate player build.
