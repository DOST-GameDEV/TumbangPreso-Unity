# Haunted through match menus

Opening Pause previously cleared near sight and world audio filtering while the
body remained Haunted. The actual Pause backdrop is translucent, so a player
could inspect the visible match without its active impairment.

The shared perception gate now remains active through match menus. Pooled UI
voices bypass listener effects, keeping navigation feedback clear while world
voices retain the listener filter. Existing victim, round, spectator, replay and
camera ownership gates remain. No clips, authored visuals, loading or packet
layout changed; compatibility remains114.

## Evidence

On source ed86e3b2c plus the three owned files, the baseline actual PausePanel
case fails because near sight becomes false. Final native D3D11 PlayMode3/3
passes: the same real menu case, existing victim/view/lifecycle controls and
existing near/far pixel plus cleared-frame checks. The menu case verifies UI
source bypass, world source filtering, close and explicit status cleanup.

All659 frozen inputs remain unchanged after the final run. The three published
source files match that native candidate byte for byte. Baseline and final XML
and manifests are retained in [haunted-menu-checks](haunted-menu-checks).
Both jobs are terminal; no fixture repair or unchanged suite retry occurred.

This proves local callbacks, routing and native pixels. It does not qualify an
audible mix, physical controls, rejoin or this change in a new player. The prior
passing protocol114 direct pair predates this local-only change and retains its
own source boundary. The broad character-reconciliation Feedback row stays open.
