# Spectator input behind results

F0930-33, source candidate91558c5a6 plus the two camera guards and native fixture.

## Fix

SpectatorCamera previously ignored registered full-screen takeovers other than
Panel. Its unscaled flight continued behind the final standings, even though the
board released the cursor and offline simulation paused. SpectatorDirector could
also keep writing the camera pose. Both now stand down for takeovers outside the
spectator camera hierarchy. The spectator's own replay is excluded from that gate.
No gameplay authority, presentation assets, loading or ability mechanics changed.

## Native evidence

Unity6000.5.8f1, Windows D3D11, Eskinita, isolated validation checkout and named
profile feedback-0930-victory-spectator. The test uses the actual input action and
actual final-round result entry. Before results, held W moves the spectator.

- Baseline:1case fails. Camera moves from(0,8.99,-13.97) to(0,8.16,-12.28)
  behind the board.
- Fixed:1case passes in7.991215s. Position and rotation remain unchanged with W
  held and with automatic directing engaged. The cursor stays unlocked and visible.
  Closing results releases flight.
- Final frozen manifest contains537 overlay inputs. Non-metadata inputs retain
  their hashes after the run. Main-checkout unrelated dirt is excluded.

Raw XML and input manifests are in checks/victory-spectator. Full logs remain in
the isolated checkout's Logs/feedback-0930 directory. Initial overlay preparation
overlapped Editor initialization; the Editor recompiled after that preparation,
before the baseline case ran. The final run began after preparation completed.

This is a spectator bug found independently. It does not establish the original
player mouse-look complaint's root cause; that report is human-retired in the Doc.
An earlier player fixture failed only because synthetic MouseState.delta does not
drive legacy Mouse X/Y. That failed baseline is preserved locally. The corrected
player fixture is not claimed as tested by this focused spectator run.
No new player build, live peer, physical device or human verification is claimed.
