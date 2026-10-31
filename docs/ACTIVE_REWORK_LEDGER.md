# Current checkpoint

Updated 2026-10-31. [TODO](TODO.md) is the only active queue.

- UI-REVAMP-1029, QoLUpdates-v2: Settings, Room, match menu in game; review open. [Method](UI_REVAMP.md).
- FEEL-1031, on main: replay wipes, 3D replay, impact, knockback, SFX. Film: run_ui_player_review.py --feel-only.
- TAG-FEEL-1031 and TAG-PHASE-1031, on main: one tag live and in replay, FPP swing, spacing. Open: arm spin and ult
  visuals in replay. Next: owner review.
- ONLINE-CAST-1028: 73b1f403b pushed/built. Ult victims stand on own floor from the
  host-frozen pose; GroundY ignores awnings. Payo delayed pairs: Cheska gap 0, Nemu 0.015,
  Paete tree grounded. Next: victims in Paete/Amihan/Phaister reach, then multi-PC.
  Logs/ult-terrain-20261029/. Preserve six owner files.
- Owner requested wrapping up and handing off unfinished mobile UI work.
  [Resume here](reports/mobile-ui-2026-10-26/HANDOFF.md): ff9 Android candidate is
  built/uploaded, native41/41 and recorded actual touch journeys passed. Newer
  source has6/7 focused checks, picker clipping still fails, and no APK contains
  those patches. Emulator/native jobs are stopped.
- Keep `mobile+network` separate from main; current main is included.
  Preserve owner metadata/settings and stage only explicit owned paths.
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

Unrelated future sessions do not automatically owe mobile updates.
[Earlier checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md) are history.
