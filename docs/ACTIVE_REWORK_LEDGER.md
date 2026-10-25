# Current checkpoint

Updated 2026-10-25. Owner scope: [TODO](TODO.md).

- Source baseline: `BugFix` at `47487eda7`; `main` at `2239d8d1b` is an ancestor.
  The apparent divergence was a shallow-history boundary, recovered before merging.
- Task work area is separate from the dirty canonical checkout; preserve its
  existing asset metadata and ProjectSettings changes.
- Current unit: documentation/token cleanup and unused-asset dependency audit.
  No gameplay or network behavior is changed by the documentation cleanup.
- Publication order: cleaned BugFix into main, verify ancestry, delete BugFix,
  create NETWORKFIX; spectator refinements publish to main; network changes stay
  on NETWORKFIX. Check live refs before each mutation.
- Evidence baseline: prior Rago/local carry tests are dated, not new qualification.
  Fresh documentation/Core checks and targeted runtime checks must be recorded.
- Next action: finish asset/credit cleanup, run docs/source/Core checks, inspect
  the diff and integrate the verified documentation unit.

Keep this file short. Record exact active jobs and next command in the task-local
checkpoint while a run is live. Replace resolved state instead of prepending old
checkpoints. [Previous checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md)
preserve the earlier Rago, carry, HUD and replay evidence without assigning work.
