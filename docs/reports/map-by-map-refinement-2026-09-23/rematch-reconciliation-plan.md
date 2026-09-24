# Chosen-map rematch reconciliation, 2026-09-24

Baselinec85614a42. Later UI reports carry RematchActuallyLoadsTheChosenArena as a
possible gameplay defect. The September15 history records a real earlier defect
and its native two-peer fix: BeginRematchNow now advances/syncs the map before
SceneFlow.StartMatch. That implementation remains in current source.

The newer UX-1 loading flow changed offline SceneFlow.Go to HubLoading.Begin,
which starts LoadSceneAsync. The test still requests rematch, yields one frame,
then requires the new active scene. A pending legitimate load can explain the
failure without any broken map selection. Do not remove the loading flow, rewrite
rematch authority or treat old native evidence as current all-path proof.

Run only the existing failing case once at current source and preserve its XML.
If it fails at the immediate scene assertion while SelectedMap is correct,
align this regression with the actual asynchronous entry contract: wait on the
chosen scene/installed round, with a bounded timeout, retaining the exact map
assertion. One bounded fixture repair; no capture work or broader UI rerun.
If selection/loading really fails, diagnose that distinct product cause instead.

On passing, correct the stale TODO/ledger wording with source and exact evidence.
Keep final current-binary network rematch/reconnect qualification in P7. No old
task/history deletion, no implied owner review or all-peer approval.
