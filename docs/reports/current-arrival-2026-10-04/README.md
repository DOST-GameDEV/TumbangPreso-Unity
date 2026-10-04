# Current custom-match arrival qualification

Source: `c9d876da26df851f7dcebc5c05a210dfa991ce0d`, Unity6000.5.8f1,
Linux Editor PlayMode, complete current Assets/Packages/ProjectSettings in an
isolated validation project. llvmpipe/OpenGL,640×360,2 worker threads. Fresh named
profile, separate validation company/product; preferences and settings restored.

## Player-facing behavior checked

Existing `MatchArrivalFlowTests.CustomHostSelectedCourtSkipsVotingAndBeginsWithoutSecondReady`
passed1/1 in24.5395321s at06:39UTC. It exercises custom LAN-host/bots character
selection, host-chosen Eskinita without a vote, loading, intro and automatic
round start with3,2,1,GO! No second in-match Ready action is supplied. It also
checks presentation-clock release and loading dismissal.

No gameplay implementation changed for this qualification. All frozen input
bytes matched after the passing run. Process exit0; no new kernel OOM event.
This is Editor integration evidence, not a packaged remote-peer, FPS, human or
full visual acceptance claim.

## Failed attempts retained

- Initial complete-project import reached scene setup then exited247 near the
  8GiB cgroup limit, with an OOM-kill recorded. No XML result.202 isolated importer
  metadata files changed; no source/model/scene payload was copied back to main.
- Fresh cached run with optional camera captures failed the existing20s start
  assertion.14 camera-only frames were recorded. No new OOM event; input bytes
  matched. Early frames were behind loading, later frames show overview/lineup.
  Screen-space UI/title cards were not captured. This is not accepted intro film.
- Same cached source/backend without optional captures passed. No test deadline
  was relaxed. Capture overhead and game-flow acceptance remain separate.

Raw result XML, resource observations and input audits accompany this report.
No full-suite or multiplayer readiness is inferred from this single route.

## Defender recovery after an ultimate

Existing `DefenderUltimateResetRecoveryTests.DefenderFinishesItsResetAndMovesAfterAnAcceptedUltimate`
also passed1/1,5.0872247s,06:59:52–57UTC on the same current complete-project
source. The real AI defender starts a held reset on a knocked-down can, an
accepted Zack ultimate freezes simulation, then the phase releases input/clock,
the defender restores the can and physically resumes patrol by more than0.65m.
All19212 frozen inputs and both settings remained unchanged; exit0 and no new
OOM event. This is an isolated two-actor native reproduction, not proof of all
hero/status combinations, actual peers or the original player's exact session.
No defect reproduced in this case, so no speculative gameplay fix was made.
