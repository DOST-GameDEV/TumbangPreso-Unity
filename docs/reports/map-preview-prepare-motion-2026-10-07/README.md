# Reduced motion while map video preparation is in flight

On source512529d9c plus the focused test overlay, native Unity58748 actually
prepares the Arena movie after ReducedUiMotion is enabled. The late callback
starts playback anyway: the new native case fails with isPlaying=true. The
existing direct-setting case after first frame passes. This distinguishes the
in-flight callback race from the previously fixed live-setting poll.

Original exit2, one pass/one fail. All21315 frozen inputs,13 existing Editor
preferences and four original isolated-profile files restore;295 generated
source deltas are preserved before restoration. Raw XML and source/launch/
restoration receipts are retained; the complete source inventory is gzip-compressed
without changing its underlying bytes. Final candidate57268 exits0 and passes both cases. All21315 frozen inputs,13
preferences and four profile originals restore again;295 generated deltas are
preserved. Actual native preparation can supply its first frame while paused,
and the current poster remains displayed until motion is re-enabled.

The focused correction makes the prepared callback inspect the current motion
setting before Play. A late first frame retains the matching poster and pauses
when motion is disabled. Runtime, native decoder ownership and active-view guards
stay in the existing component. No authored media, art, camera, map, startup,
network, bot or finalized hero changes are part of this unit.


The first candidate60092 is retained as failed. Its new pause/poster assertions
pass, but the resume assertion samples before Update: preparation has already
set HasFirstFrame, so the frame-wait loop does not yield after changing the
setting. Giving the existing setting-change reaction its two frames resolves
that observation failure without another product edit. The final run includes
the existing steady-state control and the new in-flight preparation case.

This is native Windows preview-lifecycle acceptance on source512529d9c plus the
listed runtime/test overlays. It does not qualify the older shared cb5 package
for this fix, Linux codecs, physical controls, visible GPU performance or all
tournament behavior. Existing unrelated passing checks are reused unchanged.
