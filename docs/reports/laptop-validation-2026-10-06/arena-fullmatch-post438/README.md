# Eight-round Arena Hero Strike after supported-shoe fix

## Scope and source

Exact80440c18e80db1a5b85a99151e88914d53674d70 on local Windows11 gamergmae/Unity6000.5.8f1. This source includes438fcac65's supported-shoe correction and the later between-ramp coverage. All21194 inputs exactly match the preceding qualified worker. Claude UI1856ea1a0 arrived after this run began and is NOT qualified by these results. Older27 UI draft paths remain preserved in the integration checkout; no art or UI work is discarded.

The existing ArenaMatchProbe runs four real bots for eight60s HeroStrike rounds with actual directors, scoring, breaks, falls and recovery. No director or actor is manually driven. The probe fixes simulation frames at1/60 via Time.captureDeltaTime, with timeScale1 and frame cap lifted; live play runs about4.1x real time. It is native graphics/physics evidence with real AI decisions, not normal-clock operator, packaged-player or actual-peer acceptance. The reported total69721 frames includes presentation frames while the game clock is held.

Stopping conditions are the existing probe's MatchEnded, game-time rounds*seconds*1.5+60, or2100 real seconds, with a40-minute fixture timeout. Actual MatchEnded/winner/eight round starts were observed; neither budget ended the run. Error logs are captured and asserted by the probe rather than silently ignored. Fresh unique screenshot names preserve earlier captures.

## Actual result

Unity40648: one native UnityTest passes, zero fail, exit0. Eight rounds cover all five layouts and seven changes. All round-start bodies stand on supported marks; can and drawn/collider surfaces agree. All seven breaks pass, including the18s halftime package/show.

The match records33 can knockdowns,41 restores and65 tags, and finishes with scores3395/3885/3920/4325, winner seat3. Four falls are all caught, returned to supported floor, frozen for the intended tag duration, then moving again. Aggregate fall rate0.13 per bot-minute and45 bot-seconds stalled stay within the existing probe's per-layout criteria. No unsupported standing body, exception or error is recorded. Jump pads fire8 times and speed pads5 times. Stamina pickups are NOT SEEN and remain unqualified.

The criteria are a probe threshold, not proof every decision is good: at most1 fall per bot-minute and15% stalled share per layout. Raw report retains the per-layout measurements and exact fall traces. This run does not establish all nine kits, bonus-pad planning, optimal routing, physical controls, spectator camera quality or competitive fairness. No further AI/runtime change was justified by the passing result.

## Preservation and evidence

All21194 inputs,13 existing editor preferences and four original isolated-profile files restore;279 native import deltas are retained before restoration. The source-equivalence receipt links the existing full manifest without rerunning or duplicating unchanged input preparation. Raw XML, classified launch/restoration receipts and full match report are covered by SHA256.json. Native captures remain under the worker's internal Logs folder with an exact local capture manifest. No remote process or UI source was edited.
