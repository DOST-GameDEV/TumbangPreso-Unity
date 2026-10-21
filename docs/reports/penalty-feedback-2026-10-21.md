# Penalty feedback ownership, October 21

Owner report on BugFix: a penalty appears without a warning, including an orange
attacker penalty marker while playing defender. Baseline is ce529dd3b. Existing
ProjectAuditorSettings and Assets/_Recovery work are preserved.

## Cause and correction

RoundDirector.StepTournamentPenalties calls the one local Hud.PopHitmarker for
every penalized seat, independently of the slot passed to MatchDirector.AddScore.
Hud.OnScored also sounds every tournament penalty before checking ownership.
The warning correctly reads the local role and local violation timer, so another
seat's marker has no matching local warning. This is evidence of incorrect
feedback attribution, not evidence of a wrong-slot score deduction.

Remove the two simulation-loop marker calls. Route the existing marker and
penalty sound through Hud.OnScored after checking local slot and spectator state.
The same path receives MatchDirector.ApplyNetworkScoreEvent, so clients retain
their own tick feedback. Scoring, timer thresholds, assets and warning copy are
unchanged. Keep the tick wordless; the persistent warning already names its cause.

## Native baseline

Unity 6000.5.8f1, Windows Direct3D11, original production source from ce529dd3b:
four fresh PlayMode cases, two passed and two failed. Another defender's camping
flashed the attacker's crosshair; a received own penalty failed to flash its
owner's crosshair. The local slipper warning before/during a penalty and clearing
after retrieval passed. The baseline's other-attacker case sampled after the HUD
tick, which can consume a marker's lifetime on a long frame; subsequent marker
assertions sample immediately after scoring, before ticking presentation.

Baseline XML: Logs/penalty-feedback-1021/causal-baseline-ready.xml.

## Fixed-source validation

Five fresh PlayMode cases passed, zero failures, on the corrected source using
Unity 6000.5.8f1 and Direct3D11. XML: Logs/penalty-feedback-1021/fixed.xml.
The other-seat cases also verify that the actual five-point deduction belongs
to the offending seat and leaves the local player's score unchanged. Both own
penalties show the advance warning, the immediate reason during the penalty,
and clear when their cause ends. Received score events show the own-seat marker,
suppress another seat's marker and suppress personal feedback for spectators.
Compilation completed without errors. The guard restored 27 existing named-profile
files and one shared Editor input preference after the Editor exited.

Added focused native cases to TournamentPenaltyTimingTests for other-seat
penalties in both roles, local camping/slipper warnings before and during a
penalty, clearing after leaving the can or retrieving the slipper, and received
own/other penalty events including spectator suppression. The new cases use real
directors, characters, slippers and native HUD on an empty controlled court.

The initial full-map run exited during Eskinita/arrival loading without XML.
The next attempt could not acquire the checkout after the owner opened Unity.
Neither attempt establishes a test result. Logs are retained in
Logs/penalty-feedback-1021. The owner then approved stopping the match for isolated
validation. A clean SplashScreen was verified before closing the Editor; a launch
that raced Editor shutdown was also rejected without running tests. Named
profiles and shared Editor input preferences use the existing preservation guard.
The completed baseline restored 23 existing test-profile files and one shared
Editor input preference. No player build, listening judgement, physical-device
or real-peer claim. The controlled network check calls the same event delivery
entry used by the score-message receiver; it is not a two-peer session.
