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
The actual Windows player/peer qualification below also passes.

- [Initial native run](checks/round-freeze.xml)
- [Native retry](checks/round-freeze-retry.xml)
- [Frozen retry inputs](checks/round-freeze-retry-inputs.json)
- [Handoff baseline](checks/round-freeze-handoff-baseline.xml)
- [Handoff fix](checks/round-freeze-handoff-final.xml)
- [Displayed-view snapshot](checks/round-freeze-overlay.xml)
- [Frozen view](Round-frozen-final-view-960x540.png)

## Actual Windows Peers

Clean detached7cb964d0c builds successfully:231.847seconds of build steps. Before
preparation,15348 tracked inputs were hashed. Preparation changes203 inputs
(texture metadata and3settings) inside the isolated checkout. No runtime/package
source, model or animation input changed. Runtime DLL SHA256 is
4d29b3e2235ed71aa2c7992c56ff5e560ca82c5ce6d960ae2318e65440f000aa.

The first real two-process batch run passes ordinary LAN progression and the
shared freeze clock, but has no rendered image. Its image gate correctly fails.
One graphics-enabled rerun removes batch mode and keeps the existing local-review
sign-in bypass. The standalone review flag is last, so it disables UGS without
installing the unrelated menu-walk diagnostic. No Relay/online session is allocated.

That same binary passes the graphics-enabled Hero Strike/Eskinita pair through
round2, protocol98 and matching structural state F8D5C0E1. The observed frozen spans
are9.8732s host and9.9767s client,95/97samples at approximately100ms intervals. Both
receive identical began109.467993964413, retain1920x1080images with unchanged
capture count, keep simulation time and input blocked, then resume round2. Player
screenshots were inspected. All-bot review suppresses its ordinary HUD/card, so
these screenshots qualify the rendered frozen world; the native human-view capture
above separately qualifies the centered card. This is direct local-peer evidence,
not online/ranked/lossy/all-map or all-effect qualification.

Both native players exited. Named profile/shared input preservation passes. The
old Desktop binary and unrelated work were not replaced.

- [Successful peer gate](checks/round-freeze-peer-result.json)
- [Actual LAN report](checks/round-freeze-lan-result.json)
- [Initial batch image failure](checks/round-freeze-batch-result.json)
- [Player identity](checks/round-freeze-player-identity.json)
- [Preparation drift](checks/round-freeze-preparation-drift.json)
- [Host frozen frame](Round-frozen-peer-host.png)
- [Client frozen frame](Round-frozen-peer-client.png)
