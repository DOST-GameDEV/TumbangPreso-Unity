# Current checkpoint

Updated 2026-10-28. Owner scope: [TODO](TODO.md).

- LOBBY-UI-1028: knit rebuild on lobby-ui-revamp. Next: owner review,
  room PlayMode tests, real joined peer, then main and mobile+network.
- Zack refinement follows main `6ab16b867`: short Momentum streamers, tapered
  ribbons, correct gold/ink layering and two empty zap hands with prop return.
  Final native film passed 1/1 across 12 takes; final focused zap capture passed
  1/1.
  Evidence: `Logs/zack-refine-20261026/`. Protocol 184, clip schema 17 unchanged.
  [Refinement](reports/zack-kit-2026-10-25/refinement-2026-10-26.md).
  Next: owner picture review, Wiki/voice text, rejected SFX and real peers.
  Five owner files stay excluded; no Unity job remains after validation.
- Cleanup `aaf96e52e1` integrated BugFix `47487eda7`; old BugFix deleted and
  NETWORKFIX starts there. Preserve owner metadata and ProjectSettings dirt.
  [Receipt](reports/repository-cleanup-2026-10-25.md).
- Spectator desktop controls and pacing are implemented. Actual player eye,
  rotation, FOV and hand state passed local and three Windows-process checks;
  167 received views, 163 distinct host-matched relay serials and unsubscribe
  stopped publication. [Evidence](reports/spectator-2026-10-25/view-candidate.md).
- Body/effect framing measures actual models and lenses, keeps quiet-frame
  players and checks covered-court fallbacks. All 21 bodies and six maps have
  [native checks](reports/spectator-2026-10-25/framing.md).
- World/HUD filters, audio and ghost cues follow actual watched views, with
  [native lifecycle evidence](reports/spectator-2026-10-25/perception.md).
  Private hallucinations/held markers now passed native and real three-process
  checks: 159 owner-to-host and 156 host-to-observer packet matches, nine render
  roots and unsubscribe cleanup. [Receipt](reports/spectator-2026-10-25/private-candidate.md).
  Next: regular skill/live events alongside NETWORKFIX. Current peer proof is
  controlled on one PC, not natural/WAN/separate-machine/Android.
- General reliability stays on NETWORKFIX; the mobile refresh follows it.

Keep jobs and exact commands in task-local receipts while running. Older
[checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md) are history.
