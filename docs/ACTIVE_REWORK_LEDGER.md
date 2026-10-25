# Current checkpoint

Updated 2026-10-25. Owner scope: [TODO](TODO.md).

- Zack baseline: `e01da9d59` on main; old magnets removed in `5105a70b4`, sound
  auditions in `08f602c12`. Owner authorizes ongoing main commits. Pullback,
  bare hands and low jumpable wire pass Core 840/840, EditMode 31/31 and PlayMode
  19/19. Updated clips contain offline audition SFX, not enabled game sounds.
  Five owner files remain excluded. No Unity job is active; no push requested.
  Next: owner visual/listening review and actual peers.
  [Evidence](reports/zack-kit-2026-10-25/refinement-validation.md).
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
  Next: hallucination/held-marker parity and regular skill/live events. Current
  peer proof is controlled on one PC, not natural/WAN/separate-machine/Android.
- General reliability stays on NETWORKFIX; the mobile refresh follows it.
  Do not restore a mobile-every-feature mandate.

Keep jobs and exact commands in task-local receipts while running. Older
[checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md) are history.
