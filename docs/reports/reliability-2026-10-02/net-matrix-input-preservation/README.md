# Matrix cleanup restores shared player input

The existing matrix runner restored its named profile files, but merely detected
shared input drift and raised an error. A successful or failed scenario that
changed bindings or created a touch layout could therefore leave those changes
behind. Cleanup now uses the existing narrow Windows player-input restorer, even
when profile restoration throws. It preserves unrelated registry values and
records whether input changed before restoration and whether it was restored.

Two tests use temporary actual profile files and an in-memory registry with the
real playerprefs_guard.restore_values algorithm. No real player, Editor, registry,
network or service is touched. They cover normal completion and an exception:
existing binding bytes restored, newly created layout removed, unrelated volume
retained and existing profile bytes restored.

Original committed runner:2 cases,1 cleanup error plus1 failure because cleanup
masked the scenario exception. Candidate:2/2 pass. A baseline helper first used
Windows' default text encoding and failed before running tests; UTF8 correction
allowed the actual original run. Its result expectation assumed2 errors, whereas
the failed-scenario assertion correctly reports a failure. Raw actual counts are
retained; no original test rerun or fixture weakening. Candidate ran after the
cleanup ordering refinement,2/2 again. This is runner safety, not a game-runtime
fix or transport qualification.
