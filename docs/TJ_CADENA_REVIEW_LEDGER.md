# TJ Cadena review ledger

November 9, 2026. Work is local on `tj-cadena-fixes`, based on main
`988ec7d53`. The previous Windows player was built from `efc0a5ff5`
and was closed for this follow-up.
Keep this branch for review until the owner directs publication. Use the
canonical checkout and `python tools/build_windows.py --launch` for future
Windows players.

## Earlier pickup and skip work on main

Main already has the contextual pickup and opening skip fixes
(`6b6fbc747` and `60ddc80c9`, recorded at `988ec7d53`). Hands-free pickup
spends the existing 0.5-second dwell on the crouch and reach. The shoe
starts at the ground and joins the hand on contact. Standing, moving,
run-stop, crouched, sliding and quick-snatch have their own poses. Right
click remains shove. Opening skip now frees the pointer for the vote and
restores gameplay capture after the film. EditMode 4/4 and exact PlayMode
9/9 were recorded in `Logs/feel-pickup-revision-20261107/`. See the
[retrieval design](Design.md#retrieval-and-can-hit-feel-november-7).

## Owner decisions

- Keep the gameplay loop simple. Ask before changing balance or mechanics.
  The approved timing values are seven seconds of can-down penalty grace and
  four seconds of can protection after reset. The final second of gold pulses
  faster. Extra throw inaccuracy is a proposal only.
- The Home screenshot shows HERO, ITEM, SHOP and TASK without the requested
  icon and lettering shadows. Check the real Home entry as well as the Shop and
  Hero surfaces. Verify all changed features in the running game paths.
- A slipper that hits the can can hang in the air until protection expires.
  Fix the contact bug while retaining the first rebound and protection rules.
- Replace the results tabs for Standings, Your Match and Players with a simple
  summary that matches current in-game UI. Show the winner, scores and next
  actions, with earned XP compact when applicable. Keep rematch, next map and
  menu wired. Keep the active board clear of old match HUD clutter.
- Reset feedback is too crowded. One prompt and progress bar should explain
  lift, carry and placement. Gold and its countdown explain the protected state.
- Keep a permanent white chalk circle at the real can reset spot. It should
  stay during active play and suit each map's theme. Remove the brief white
  impact or settling circle. Keep the visible tin movement and contact sound.

## Evidence and working state

The previous pass implemented the flight path and landing marker, defender
task prompts, four-second gold protection, raised-can motion, pickup and throw
animation, and shop/Hero focus and layout changes. Its rules test, five EditMode
cases and nine exact PlayMode cases passed. Controller coverage used a virtual
pad. Two-PC play and physical controller verification remain open.

`HubHome` routes the four sticker doors through
the existing printed-shadow effect with a more visible offset. `HubKnit` shares
that effect, and the Shop focus test captures Home before opening the popup.
That exact test passed. Reviewed the actual Home capture at
`Logs/shots-native-ui/TJ-home-sticker-shadows-1280x720.png`.

`EquipmentIntegrationProbe.ASlowSlipperLandsBeforeCanProtectionEnds` reproduces
the freeze on the live contact path. The first bounce was accepted, but after
1.2 seconds of physics steps the shoe was still `InFlight` while the can stayed
upright and protected. See `Logs/tj-cadena-fixes/review-repro.xml` and `.log`.
`Slipper.FixedUpdate` was revisiting the same can footprint and resetting its
upward velocity repeatedly. It now ignores repeat contact until the shoe has
separated horizontally. A new throw or the end of flight clears that contact.
The same exact regression passed in `review-fixed.xml`: the slipper became
loose while the can remained protected. The actual rematch to Ermita and
the actual match-end result buttons also passed in that run.

## Follow-up and validation

`MatchResult.FinishSheet` now builds one results page with the winner, four
scores, optional earned XP and the three existing actions. It uses current
`LiveHud`, `HudCard` and `HubKnit` components. Tab removal takes the detail
and player-action panels off the results surface. Rematch, map voting and reward
data are preserved. The live HUD is hidden while the result is visible.

`TumpMatchReadout.DefenderTask` uses the actual carry phase for a single
prompt and progress bar. The carry and ordinary protection circles and extra
arrow are suppressed. The can's gold highlight, protection countdown, clatter
and existing ability effects remain.

`WorldContactPresentation` draws a permanent ground-supported mark at
`Lata.Mark`. The temporary settling footprint and live contact strokes are
gone. The six styles are an uneven loop in Tondo, twin rails in Ermita, a
wave in Sitangkai, rounded grid corners in BGC, a broken court ring in Arena
and terrace strokes in Banaue. Recorded replays use the new mark style.

The first follow-up run, `Logs/tj-cadena-fixes/review-fixed.xml`, found a
D3D11 chalk shader syntax error, a capture helper aimed at a nested result
canvas, an early return that blocked result reward updates and test timing
issues. Those were addressed. The second focused run uses the same named
profile, `tj-cadena-verification-20261109`, and passed 9/9
(`Logs/tj-cadena-fixes/review-recheck.xml`). That includes all six actual
courts, all three defender prompts, the gold protection edge, reward
acknowledgement and the match-end input route. The corrected restore and
protection captures were reviewed. The rendered results board exposed an
overstated card slant, now fixed. The final render run passed 5/5 exact
cases in `review-render-final.xml`. The real Classic match-end board and the
Hero Strike XP fixture were reviewed, along with detail views from all six
actual court scenes. The game-side evidence is
`Logs/shots-native-ui/TJ-real-match-end-1280x720.png` and
`Logs/tj-cadena-fixes/marks-final/`.

The Windows 64-bit player built from gameplay commit `9e1aac96a` and opened
to a responsive window. See `Logs/windows-build-20261109-190215/job.json`.
The helper recorded a clean starting tree and restored source settings.
The in-build identity still marks the tree dirty. The visual evidence above is
from the actual PlayMode courts and UI routes. It does not stand in for a
two-PC playthrough or owner feel approval.

Presentation reach, audio reach, cue relay and positional audio audits each
found zero new host-only call signatures against fetched main `988ec7d53`.
Their prior failures remain. The docs audit only flags the existing TODO
size limit. No paid generation, resets, force pushes, broad PlayMode runs
or other chats. The main audit scratch copy was removed. Automatic approval
review blocked removal of the first render's superseded shader-error
screenshots, so they remain in `Logs/tj-cadena-fixes/marks/`.

## Owner review still open

Judge the pickup animation and audio in play, including the heartbeat,
`ui_valid` and `court_escape` levels and whether the escape text is too
much. Check the changes on two actual peers and with a physical controller.
Do not retune motion inaccuracy or add pickup, chase or defender mechanics
without approval. The November 5 Windows backup remains preserved.
