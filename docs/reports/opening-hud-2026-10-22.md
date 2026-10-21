# Opening HUD visibility, October 22, 2026

Owner requested BugFix and supplied a Tondo opening frame with the scoreboard,
clock, identity plate and waiting abilities drawn over the camera introduction.
Gameplay chrome should appear only after the complete 3, 2, 1, GO sequence.
Starting revision: a0acf36a9, synced with origin/BugFix.

The readout previously suppressed its Canvas only for ArenaIntro.HidesUi,
halftime, round swaps and spectator cutscenes. Ordinary map arrival and the
ready countdown were absent from that decision. Arena's own one-time canvas
suppression also finished before the countdown.

ReadyGate now exposes the current opening/countdown presentation hold, including
MatchArrivalPresentation.Active for direct arrival routes. A CanvasGroup hides
the match readout and disables its interaction while that hold remains. The
countdown's own group ignores its parent group, preserving its original layout,
reading scale, sound and timing. Touch controls use the same hold. Cancellation
and late-join adoption use the existing gate lifetime/state transitions.
Independent can/recall markers already hide while PresentationClock is held.

Added ArrivalHandoffTests.MatchHudWaitsForTheCompleteOpeningCountdown: open the
real gate, check hidden score/touch controls, cut the film with the host countdown,
check every cue including GO stays visible independently, then check the HUD
returns and the countdown clears. No new script metadata or authored asset changes.

## Validation

Question: does the scoped candidate compile, and can the countdown remain fully
opaque under a hidden parent CanvasGroup? Stop after both assembly compilations
and one temporary native alpha check. Tooling retries: zero for these checks.

- Direct Roslyn runtime compilation: exit0, existing warnings retained.
  Logs/opening-hud-1022/runtime-compile.log and runtime.rsp.
- Direct PlayTests compilation referencing that candidate runtime: exit0,
  existing warnings retained. Logs/opening-hud-1022/playtests-compile.log and
  playtests.rsp. Compilation is not a test execution or Unity import result.
- Existing Unity 6000.5.8f1 Editor, PID21120, Pipeline eval: temporary offscreen
  overlay Canvas with alpha0 and independent countdown group. Native
  CanvasRenderer.GetInheritedAlpha returned normal0, countdown1. Temporary
  objects were destroyed in finally. This verifies the rendering primitive,
  not the changed game's opening sequence.
- Focused diff whitespace check passed.

Native regression and normal map playback remain unrun: the existing Editor is
actively playing Assets/TumbangPreso/Scenes/Maps/Arena.unity with the owner's
normal profile, and has not loaded the changed assembly. Its session, profile
and preferences were preserved. No player build or peer/device acceptance claimed.
Unrelated HubChrome, HubCustom and ProjectAuditorSettings edits are preserved.

Next native check, when the current play session is available: run the named
regression in a profile-preserved test session, then start Tondo normally. The
opening and peer wait must show no gameplay chrome, all four countdown cues must
remain readable, and the HUD must appear only once GO clears. Repeat as a
spectator and with touch forced visible; retain existing clean-feed behavior.
