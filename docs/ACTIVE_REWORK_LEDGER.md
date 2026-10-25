# Current checkpoint

Updated 2026-10-25. Owner scope: [TODO](TODO.md).

- Cleanup/integration: `aaf96e52e1` on main, containing original BugFix `47487eda7`.
  Remote BugFix is deleted; NETWORKFIX starts at the merged cleanup commit.
- Task work area is separate from the dirty canonical checkout; preserve its
  existing asset metadata and ProjectSettings changes.
- Current unit: spectator controls, camera pacing, view fidelity and event coverage.
  Spectator changes publish to main; network reliability fixes stay on NETWORKFIX.
- Cleanup evidence: fresh Core 832/832, documentation checks pass, Unity compiles
  without C# errors. Removed 112,453,059 asset bytes; seven entry docs 97.62% smaller.
  Targeted native spectator baseline is still importing in the isolated work area;
  its exact process/log are in the task-local checkpoint. Do not duplicate it.
- Confirmed source defect: current spectator POV uses fixed eye position/body yaw
  and operator pitch. Investigate actual player-view observation and borrowed
  viewmodel behavior before changing it. Director cuts immediately on beat changes.
- Next action: collect native baseline, then implement grouped controls and stable
  camera holds; trace shared eye/look data and perspective effect ownership.

Keep this file short. Record exact active jobs and next command in the task-local
checkpoint while a run is live. Replace resolved state instead of prepending old
checkpoints. [Previous checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md)
preserve the earlier Rago, carry, HUD and replay evidence without assigning work.
