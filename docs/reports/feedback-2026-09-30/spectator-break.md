# Shared round standings for spectators

The actual protocol100 Linux bot run exposed a bare frozen view at ordinary
round boundaries. Timers/input/frozen-frame behavior worked, but spectators lost
the next-taya and cumulative standings context.

Two old personal-card rules caused it: initial spectator installation skipped
RoleSwapCard entirely, and entering spectator mode disabled an existing card.
The current passive round card describes the whole match, not the viewer's role.

The shared card is now installed for watchers and remains active across watch
entry. YouCard stays personal and tutorial still omits both. Explicit clean feed
hides the root canvas drawing without disabling its event owner or losing the
ongoing break state. Leaving clean feed restores the still-active card. Its
independent scaler and authored layout remain unchanged.

## Verification

Native Unity6000.5.8f1 Linux OpenGL, isolated profiles, exact source/candidate inputs:

- Baseline1/1fails: entering spectator mode disables the round-card owner.
- Final3/3pass in12.84s: initial all-bots spectator gets the shared card but no
  personal card; player-to-watch transition shows it; clean-feed hide/restore
  retains the break; existing player frozen frame/input/deadline checks pass.
- Native960x540 card capture inspected. The transition test retains its original
  camera; initial spectator construction is independently checked.
- No new OOM or guard stop. No timer, protocol, scoring, ability or asset changes.

[Raw checks and capture](spectator-break-checks/).
The earlier player exposed the defect, but has not been rebuilt with this fix yet.
Actual spectator peer/player and physical-device qualification remain separate.
