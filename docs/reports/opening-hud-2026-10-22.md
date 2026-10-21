# Opening HUD visibility, October 22, 2026

Owner requested BugFix and supplied a Tondo introduction frame with gameplay
chrome drawn over it. The initial hide-through-GO change shipped as 1bd559dd1.
The owner revised this to a 0.5 second fade immediately before the first 3 cue.

## Current behavior

ReadyGate hides gameplay chrome during the introduction and loaded-peer wait.
Once the countdown is authorized, the HUD and touch controls fade together on
one unscaled clock for 0.5 seconds. The first 3 cue follows at full opacity.
The simulation remains held during the fade and original countdown. The
one-second 3/2/1 cues and half-second GO retain their durations. Touch raycasts
stay disabled until fully opaque. No camera, scoring or authored asset changes.

The readout uses its root CanvasGroup. The original countdown-only group
exception is removed because the entire HUD is opaque when counting begins.
Cancellation and late-join adoption retain existing state/lifetime transitions.
Can/recall markers retain their existing PresentationClock hold rule.

## Coverage and validation

ArrivalHandoffTests.MatchHudFadesInBeforeTheOpeningCountdown checks hidden
intro chrome, an interrupted film, intermediate HUD/touch alpha, full opacity
before 3, every countdown cue and the final clear. ReadyCountdownAbandonTests
checks the half-second pre-cue yield with simulation held, plus abandonment
after GO. Existing test/probe setup waits immediately after StartLocalCountdown
include HudRevealSeconds; their gameplay assertions are unchanged.

Question: do runtime, PlayTests and Tests compile with the new lead and fade?
Stopping condition: all three sequential direct checks. Tooling retries: zero.
All three exited 0. Logs and response files: Logs/opening-hud-fade-1022.
Compilation does not establish Unity import or native test execution. The
original native alpha primitive check remains recorded in Logs/opening-hud-1022;
it does not establish playback of the revised fade. Focused whitespace check passed.

The existing Editor session was preserved. A current Pipeline query timed out
on its main thread, so no current runtime state or changed-source native
acceptance is inferred. No build, profile mutation or actual-peer/device
verification is claimed. Concurrent map/visual, Hub and Auditor edits are preserved.

Next: run the named fade regression and ReadyCountdownAbandonTests with a
named preserved profile when the Editor session is available. Start Tondo:
intro and peer wait hide chrome, the HUD fades in over 0.5 seconds, then 3
appears at full opacity. Repeat as a spectator and with touch forced visible;
retain clean-feed behavior and verify abandonment during the fade.
