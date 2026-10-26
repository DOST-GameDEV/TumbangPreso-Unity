# Current checkpoint

Updated 2026-10-26. [TODO](TODO.md) is the only active queue.

- Cleanup aaf96e52e1 integrated BugFix47487eda7; BugFix is deleted. Removed
  107.2 MiB of proven unused assets; seven entry docs are97.62% smaller.
  Preserve owner metadata/settings. [Receipt](reports/repository-cleanup-2026-10-25.md).
- Desktop spectator is on main: grouped live controls, calmer camera, actual
  player eyes/hands/lens,21 bodies/six maps and private cues.
  [Evidence](reports/spectator-2026-10-25/README.md).
- Network/mobile code0d8bee6ba stays on `mobile+network` until integration is
  requested. Stable saved LAN identity survives late online sign-in; Relay keeps
  online identity. Queue512 and12-second silence allowance support mobile loading;
  deliberate host exits still notify immediately. Old simulation is revoked.
- Actual Windows/Android view transfer passed with Android as player, spectator
  and host. Android outage/background rejoin restored the same seat/match. Actual
  touch pause/resume, follow/FPP/TPP, tools, replay/return, peer card and host-loss
  notice were checked. All six Android map scenes and Classic HUD passed smoke.
  Core841/841 and focused native groups passed. Four unrelated broad guard/scanner
  failures and emulator quality-recovery limits remain explicit.
  [Evidence/build hashes](reports/mobile-2026-10-26/README.md).
- Mobile uses ARM64 IL2CPP/API26 minimum/target36; lossless asset payload is about71%
  smaller. Unrelated future sessions do not automatically owe mobile updates.
- Main078f98e87 is included. Zack's owner-approved electrical-outline Backtrack
  look308fb345f has23/23 native and1/1 stroke capture; hand zigzags were removed
  with1/1 capture. Sound review remains open; rejected procedural SFX were not
  installed and five owner files stay excluded.
  [Evidence](reports/zack-kit-2026-10-25/refinement-validation.md).

Android proof uses ARM translation, not physical phone performance or thermals.
Physical Wi-Fi discovery, separate machines and WAN/Relay remain unqualified.
[Earlier checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md) are history.
