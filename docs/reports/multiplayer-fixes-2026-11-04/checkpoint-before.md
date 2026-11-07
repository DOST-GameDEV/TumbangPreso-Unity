# Current checkpoint

Updated 2026-11-04. [TODO](TODO.md) is the only active queue.

- MATCH-DEFAULT-1104, main: four rounds; existing ultimate charge scaling retained.
- CUSTOM-LOGIN-1104, main: no custom bot removal/NONE; restored linked accounts skip
  boot login. Direct compiler checks only; native/peer behavior remains unverified.
- HOME-FILMS-1101, on main: Zack (BGC), Phaister ("Drained", real Ermita map). [Edit guide](../ArtSource/home-scene/README.md). Next: owner.
- FEEL-1031, on main: replay wipes, spectator 3D replay, break card, impact, 0.6 m knockback,
  SFX. Film: run_ui_player_review.py --feel-only. Next: owner review.
- TAG-FEEL-1031, on main: one tag live and in replay (spacing, reach, shared shove), FPP swing, replay impact on hand
  arrival, taya warnings on the red tile, moment families, feed icons. Open: arm spin in tag replay. Next: owner review.
- REPLAY-LIMITS-1101, on main: clip 18/protocol 187 records mountain and headband. Next: owner.
- UI-REVAMP-1029, merged from QoLUpdates-v2: Settings, Room, match menu in game; review open. [Method](UI_REVAMP.md).
- TAG-PHASE-1031, on main: open, ult visuals in replay. Next: owner review.
- ONLINE-CAST-1028: 73b1f403b pushed/built. Ult victims stand on own floor from the
  host-frozen pose; GroundY ignores awnings. Payo delayed pairs: Cheska gap 0, Nemu 0.015,
  Paete tree grounded. Next: victims in Paete/Amihan/Phaister reach, then multi-PC.
  Logs/ult-terrain-20261029/. Preserve six owner files.
- Unfinished mobile UI: [resume here](reports/mobile-ui-2026-10-26/HANDOFF.md). ff9 candidate built; newer
  source has picker clipping failing and no APK. Jobs stopped.
- Keep `mobile+network` separate from main; current main is included.
  Preserve owner metadata/settings and stage only explicit owned paths.
- Desktop spectator is on main. [Evidence](reports/spectator-2026-10-25/README.md).
- Network/mobile code0d8: recorded Windows/Android checks passed within documented
  limits; physical peers remain separate. [Evidence](reports/mobile-2026-10-26/README.md).
- Zack's approved Backtrack308fb345f and hand-zigzag removal
  remain intact. SFX listening review is still open; rejected sounds were not
  installed. [Evidence](reports/zack-kit-2026-10-25/refinement-validation.md).

[Earlier checkpoints](archive/cleanup-2026-10-25/docs/ACTIVE_REWORK_LEDGER.md) are history.
