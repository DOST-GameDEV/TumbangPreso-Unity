# Current checkpoint

Updated 2026-10-25. Owner scope: [TODO](TODO.md).

- Zack rework is uncommitted on canonical main at `1395ce099`. Five owner files
  remain excluded. Unity compile, five baked clips, Core 840/840, focused EditMode
  43/43 and focused gameplay 19/19 pass. Six silent clips are ready for owner
  review; retired Zack tests are removed. No Unity job remains active.
  [Verification and broader failure limits](reports/zack-kit-2026-10-25/verification-session3.md).
  Next: owner visual/audio review and actual peer acceptance; publish only after
  owner instruction and branch confirmation.

- Cleanup/integration: `aaf96e52e1` on main, containing original BugFix `47487eda7`.
  Remote BugFix is deleted; NETWORKFIX starts at the merged cleanup commit.
- Preserve the canonical checkout's existing owner asset metadata and
  ProjectSettings changes. The Zack continuation used that guarded checkout.
- Desktop controls and initial pacing/elevation unit complete with native checks;
  [report](reports/spectator-2026-10-25/README.md). Spectator work publishes to main.
  Network reliability fixes stay on NETWORKFIX; the mobile refresh follows network work.
- Cleanup: Core 832/832; 107.2 MiB removed and seven entry docs 97.62% smaller.
  Focused native receipts and their limits live in the linked reports.
- Actual observed player eye/full rotation/FOV/hand state implemented and checked
  locally plus three real Windows processes. The observer received 167 views with
  163 distinct relay serials matched to the host; unsubscribe stopped publication.
  [Evidence and limits](reports/spectator-2026-10-25/view-candidate.md).
- Body/effect framing now measures actual models and lenses, keeps all players
  in quiet frames and validates covered-court fallbacks. All 21 authored bodies
  and six current maps have [native checks](reports/spectator-2026-10-25/framing.md).
- Watched world/HUD filters, audio and ghost cues now follow the actual player
  view, with native perception and lifecycle checks. [Evidence](reports/spectator-2026-10-25/perception.md).
- Next action: hallucination/held-marker parity and regular skill/live events, then
  reliability on NETWORKFIX and mobile last. Current actual-view proof is same-PC
  controlled motion, not natural/WAN/separate-machine/Android acceptance.
  Do not restore the old mobile-every-feature mandate; mobile is a separate assignment.

Keep this file short. Record exact active jobs and next command in the task-local
checkpoint while a run is live. Replace resolved state instead of prepending old
checkpoints. [Previous checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md)
are history, not assignments.
