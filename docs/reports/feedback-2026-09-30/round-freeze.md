# Frozen Round Break

Latest Feedback replaces the per-round warmup/skip with a frozen final view and
no clicking or movement until the next round. Every break now uses the existing
10-second halftime length. The host owns its deadline; clients join the remaining
time and never advance the round. The last round still ends the match directly.
Protocol98 requires matching builds for this changed phase behavior.

The frame cache copies the final gameplay-camera output and holds those pixels
through the break. An already-displayed fullscreen render view is composited
before its presenter closes. Existing scores/next-taya cards stay above that image;
the ordinary HUD does not duplicate behind them. Gameplay time, camera input and
UI input modules are held. Cancellation restores input and requested match speed.
The buffer does not eagerly reset the world or permit skip votes. Replay archives
and authored models/animations/VFX/audio assets are unchanged.

Four native Windows D3D11 cases pass for frozen pixels/real input, late/cold client
timing, cancellation and normal/middle/final boundaries. Their first run had no
camera image in the batch Editor. One fixture repair rendered the actual gameplay
camera offscreen, as the existing native capture helper does. The fresh retry
passes4/4. The current project reports Built-in rendering; URP is not qualified.
The960x540 capture was inspected: centered cards over the frozen view, no doubled
HUD or warmup-skip line.

An additional native case first reproduced old introduction cleanup releasing
the new break hold. Cancelling that ended introduction before taking the new hold
fixes the handoff; the same case passes. A separate native GPU case passes for
preserving a visible full-screen view, including tint, when its presenter closes.
Actual player/peer qualification remains pending; F0930-32 is not marked done.

- [Initial native run](checks/round-freeze.xml)
- [Native retry](checks/round-freeze-retry.xml)
- [Frozen retry inputs](checks/round-freeze-retry-inputs.json)
- [Handoff baseline](checks/round-freeze-handoff-baseline.xml)
- [Handoff fix](checks/round-freeze-handoff-final.xml)
- [Displayed-view snapshot](checks/round-freeze-overlay.xml)
- [Frozen view](Round-frozen-final-view-960x540.png)
