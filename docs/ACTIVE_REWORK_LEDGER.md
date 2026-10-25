# Current checkpoint

Updated 2026-10-25. Owner scope: [TODO](TODO.md).

- Cleanup/integration: `aaf96e52e1` on main, containing original BugFix `47487eda7`.
  Remote BugFix is deleted; NETWORKFIX starts at the merged cleanup commit.
- Task work area is separate from the dirty canonical checkout; preserve its
  existing asset metadata and ProjectSettings changes.
- Desktop controls and initial pacing/elevation unit complete with native checks;
  [report](reports/spectator-2026-10-25/README.md). Spectator work publishes to main.
  Network reliability fixes stay on NETWORKFIX; the mobile refresh follows network work.
- Cleanup evidence: fresh Core 832/832, documentation checks pass, Unity compiles
  without C# errors. Removed 112,453,059 asset bytes; seven entry docs 97.62% smaller.
  Native baseline19, geometry21, binding/tuning12, desktop15 and final gamepad11
  cases passed in separate focused receipts. No Unity job is active at publication.
- Confirmed source defect: current spectator POV uses fixed eye position/body yaw
  and operator pitch. Investigate actual player-view observation and borrowed
  viewmodel behavior before changing it. Director cuts immediately on beat changes.
- Next action: implement actual observed player eye/look/FOV and perspective
  presentation without controlling the target, then body/effect/map coverage.
  Do not restore the old mobile-every-feature mandate; mobile is a separate assignment.

Keep this file short. Record exact active jobs and next command in the task-local
checkpoint while a run is live. Replace resolved state instead of prepending old
checkpoints. [Previous checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md)
preserve the earlier Rago, carry, HUD and replay evidence without assigning work.
