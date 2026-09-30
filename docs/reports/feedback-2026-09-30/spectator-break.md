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
## Refreshed actual player

Source04c675f7 includes the shipped spectator fix4c3f4515 and a read-only witness
change that captures0.5seconds into the actual break duration. The full input
manifest and runtime hash are retained in [player evidence](spectator-player-checks/).
The embedded identity honestly remains detached83136899 dirty, protocol100;
the manifest pins the overlaid source, rather than claiming a clean source build.

Warm headless build succeeded in62.91seconds,2076MB. Actual play used graphics,
a fresh isolated profile and supported custom4round/30second Hero rules. A complete
match ended naturally with scores750/1120/890/705 and seat1winning; the automatic
result-board rematch then began. The outer300second observation deadline stopped
that second match during round3. No new OOM. The frame-cap report was not produced;
exit0 alone is not the completion evidence. Match-over and rematch log events are.

Five ordinary boundaries across the match/rematch begin at4.993-4.999seconds.
All keep simulation and captured frame constant and input blocked; sampled wall
spans are5.08-5.30seconds on the slow software renderer. Three1364x1024 captures
were inspected: centered next-taya, cumulative sorted standings and countdown are
visible with normal map colours. Round-number filenames are reused by rematches,
so captures1/2 are the later match; the raw trace preserves both.

Correction to the proposed run scope: custom4rounds did not exercise halftime.
Do not claim new halftime qualification from this run; prior8round protocol100
player evidence and native checks cover the existing10second path separately.
Unity Services connection-refused errors and319 replay readback failures appear
in the log. Those remain diagnostic limits, not a clean-log or replay qualification.
Actual peers, hardware performance and human approval remain separate.

