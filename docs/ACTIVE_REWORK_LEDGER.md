# Current checkpoint

Updated 2026-10-30. [TODO](TODO.md) is the only active queue.

- UI-REVAMP-1029, `claude/ui-revamp`, uncommitted: Settings, Room and match menu restyled in game; pending owner review. [Method](UI_REVAMP.md).
  next, those and the match menu in game. [Method](UI_REVAMP.md).

- LOBBY-UI-1028: desktop rules/chat/layout fixes7c1a7a0ac integrated here.
  Edit20/20; exact chat/rules Play1/1. Evidence/build/push receipt on PC:
  Logs/lobby-ui-20261028/fixes/. Main separate; phone work excluded.
  Desktop visual review, physical pad and real-peer acceptance remain separate.
- Owner requested wrapping up and handing off unfinished mobile UI work.
  [Resume here](reports/mobile-ui-2026-10-26/HANDOFF.md): ff9 Android candidate is
  built/uploaded, native41/41 and recorded actual touch journeys passed. Newer
  source has6/7 focused checks, picker clipping still fails, and no APK contains
  those patches. Emulator/native jobs are stopped.
- Keep `mobile+network` separate from main. Fetch/inspect main before the next
  final build and publication. Final fetch found newmain43c158b86 (Zack polish),
  not integrated; earlier078f98e87 is included. Preserve
  original local metadata/settings and stage explicit owned files only.
- Desktop spectator is on main: grouped live controls, calmer camera, actual
  player eyes/hands/lens,21 bodies/six maps/private cues.
  [Evidence](reports/spectator-2026-10-25/README.md).
- Network/mobile code0d8 passed recorded Windows/Android view transfer, same-seat
  outage/background reconnect, manual spectator pause/card, host-loss stop,
  six map scenes and Classic HUD. Core841/841 and focused native groups passed.
  Old broad failures and emulator RTT-recovery limits stay explicit.
  [Evidence/build hashes](reports/mobile-2026-10-26/README.md).
- Zack's approved electrical-outline Backtrack308fb345f and hand-zigzag removal
  remain intact. SFX listening review is still open; rejected sounds were not
  installed. [Evidence](reports/zack-kit-2026-10-25/refinement-validation.md).

ARM translation does not qualify physical phone performance/thermals, separate
Wi-Fi machines or WAN/Relay. Lossless Android packing is about71% smaller.
Unrelated future sessions do not automatically owe mobile updates.
[Earlier checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md) are history.
