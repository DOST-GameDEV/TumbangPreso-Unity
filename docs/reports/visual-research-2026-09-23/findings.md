# VISUAL-1 research findings, 2026-09-23

Working record for TODO VISUAL-1 (design in `docs/NATIONALS_POLISH.md`, "Visual communication
and appeal pass"). Written first and appended as research continues, so nothing is lost if a
session is compacted. Owner direction, verbatim where quoted:

- "make it more visually appealing and satisfying to play as a game without making graphics
  more realistic"
- in-game UI "more appealling too, minimalist and professional and easy to look at"
- "explore adding on screen effects/hud that all work together and arent distracting (sepak-u
  does this very well)", "onscreen effects or indicators to better communicate whats happening
  without making game overhwelming"
- "u can edit all UI and hud in the actual game btw including the match end and mid round report
  and icon because astra did a bad job", "thoroughly revamp it"
- "make sure u thoroughly think abt whats visually appealing and communicates the best and works
  well with effects and not overwhelming", "use other games as referecne and research first"
- "i really dont wanna have to communicate by using text", "research analyses of game ui too"

## 1. What TUMP's own screen shows today (audit)

Sources: native captures under `docs/reports/full-backlog-2026-09-21/map-evidence/`,
`accessibility-evidence/`, `black-ui-outline-evidence/`, `close-feedback-2026-09-16/v32/`,
`direct-gameplay-2026-09-16/v28/`, the halftime popup `../TumbangPreso-Unity-validation/Logs/
halftime-popup-native-v21/`, the result board `existing-ui-kits-v41-native/`, and the baseline
taken for this pass: `Logs/look-baseline-v1/shots/` (TumpNativeHudTests at `2e537f1f`).

1. The rule that defines the game (taggable = inside the box + holding own slipper + lata
   upright) is shown only as orange text "You can be tagged" in the bottom prompt
   (`TumpMatchReadout.Prompts`) plus a 0.36 s edge flash at onset (`TumpHudEffects`).
2. The taya has no cue for which attackers are taggable (`CharacterNameplate` colours by seat,
   shapes the taya ring, carries no vulnerability).
3. Timers are sentences: restore progress (taya-only bar), "Protected · 1.1s", "THROW CD ·
   1.0s", "TAG CD · 0.7s", "Fetch your slipper · -5 / second", "Slipper returning · 2.0s",
   "Release to throw", "Pektus left · 34%", "Move the aim sideways for pektus".
4. "Lata down" is said six times (`Lata.BuildDownBeacon` comment lists them); can state is also
   written twice on the HUD ("Can upright" corner text, "CAN PROTECTED" in
   `OffscreenIndicators`). Attackers get no picture of the restore countdown.
5. Four colour systems compete: role (Offense `#f87020`, Defense `#0080e8`), seat identity
   (`PlayerIdentity`: cyan 120,220,222 / salmon 255,158,122 / yellow 244,218,114 / lilac
   204,177,244), hero palettes, UI chrome.
6. White chalk on Bayan's light paving is the least visible line; the lata is a speck at distance;
   a black slipper is the highest-contrast mass in FPP.
7. Flat light: `Toon.shader` shadow = albedo x light x `_ShadowBand` 0.45 plus scene ambient;
   trilight ambient about 0.55 to 0.66 (lagoon 0.80) against sun about 1.1
   (`MapAtmosphereAuthor.Apply`: sun elevation roof 28, alley 34, bridge 38, Bayan 46 degrees),
   so lit to shadow is near 1.5 : 1; zero specular everywhere; rim is an unmasked lerp; fog
   starts at 68 to 85 m on a 40 m arena.
8. The floor is 40 to 50 percent of each FPP frame and says nothing.
9. Arms plus held slipper fill about 12 percent of the frame, ink several times heavier than any
   character; ability tiles and white key boxes sit on top of the arms.
10. HUD before this pass: four 530x320-unit score slabs top left (`OwnerScoreStrip`,
    Color32(35,29,33,210), reads cool grey over sky), a grey clock box, a bottom-left text block
    "Attacker / Slipper in hand / Stamina", a corner "Can upright" for spectators, the permanent
    "Hold TAB for skills", white key boxes. Roughly 13 to 17 percent of the frame in ordinary play.
11. Some effects are raw primitives: confetti = scaled cubes on rigidbodies (`HeroHazards` near
    line 4048); `CanContactAccent` and fire/zap slipper trails use `Sprites/Default`.
12. The halftime popup (red brush "HALFTIME" banner, dark "NEXT TAYA" and "ROUND n Ns" plates,
    four cream player cards) is the best in-match surface so far; it sits bottom-centre over the
    viewmodel. The match-end board is a full-screen cream page with diagonal rows, three tabs,
    a winner banner and a 3D model, three text buttons; it hides the court.
13. Pre-existing defect found by the baseline run: `TumpNativeHudTests.ClassicHudReflects...`
    fails because `CourtBreak-960x540/OwnerRoundSwapCanvas/CourtBreakPopup/NextRole` renders at
    11 units against the 13.95 small-window reading floor (VISUAL-1.17).
14. Test runs dirty two generated assets, `Resources/Models/FppDetails/inday_left_arm.asset` and
    `inday_right_arm.asset` (a tangent channel is added); the validation workspace shows the same.
    Restored to HEAD each time; which version is correct is an open hygiene question.
15. Editor runs write the runtime graphics profile into `ProjectSettings/QualitySettings.asset`
    (Ultra level pixel lights 4 to 2, shadow distance 150 to 40, soft particles off). Churn, not
    a change; restore before committing.

## 2. Reference games, what each actually shows

Observations of shipped games from official or catalogued screenshots, not measured claims.

### Sepak U (Good Knight Collective), official itch.io screenshots
https://teamgoodknight.itch.io/sepak-u
- All persistent HUD in ONE band at the bottom: score "0-0" big in the centre with "RALLY 01"
  and "TIME", portraits in sketch style either side, meter stacks as icons plus numbers, a role
  word ("RECEIVER"), status icons in a row. The top three quarters of the frame are play.
- The ball keeps the brightest, most saturated colour even during a super.
- Impacts are drawn in the characters' ink language: flat two-tone starbursts, a ring plus
  radiating speed strokes at contact, black ink-blob debris, white cartoon dust puffs.
- Ball travel is one bold trail shape (white body with ink puffs).
- At big contact: brief diagonal screen-space speed hatching and a camera push.
- During a super the HUD hides and the summoned spectacle is drawn as a grey ink sketch behind
  the action, so the gameplay objects keep the colour peak.
- Between-round choice screen desaturates the world to monochrome behind gold-on-black UI.

### Knockout City (Velan Studios), Game UI Database gameData.php?id=828
- HUD lives in two bottom corners only. Bottom left: own name and two heart icons (lives) on a
  team-colour diagonal wedge. Bottom right: timer "01:21", "First to 10", team scores as big
  coloured numerals with player-state pips between them.
- No persistent text in the play area. Names above other players with heart icons over them.
- DANGER CUE: a thin red frame round the WHOLE screen edge when you are targeted. Readable at a
  glance, no vignette darkening, no words.
- "ROUND 1" is big italic white type with a dark outline at top centre, no plate, brief.
- Ground paint (arrows, spawn markers) communicates in the world.
- Round result: bold diagonal team-colour slashes, big round-tracker circles, "10 - 9" in team
  colours, one line of instruction. Graphic shapes carry it.

### Team Fortress 2, Valve, "Illustrative Rendering in Team Fortress 2" (NPAR 2007)
https://steamcdn-a.akamaihd.net/apps/valve/2007/NPAR07_IllustrativeRenderingInTeamFortress2.pdf
(link verified; content cited from the paper's well-known argument, not re-read this session)
- Value and saturation hierarchy: characters highest contrast, environments lower contrast and
  less saturated, detail simplified away from where players look.
- Warped diffuse with a ramp, rim light from above, a gradient pulling the eye up the body.

### Talks verified by title
- Martin Jonasson and Petri Purho, "Juice it or lose it": https://www.youtube.com/watch?v=Fy0aCDmgnxg
- Jan Willem Nijman (Vlambeer), "The Art of Screenshake", INDIGO 2013:
  https://www.youtube.com/watch?v=AJdEqssNZ-U
- Dominic Kao, "The Effects of Juiciness in an Action RPG", Entertainment Computing 34 (2020):
  intensity must be tested; more is not always better.

### Games cited from general knowledge (to be checked against screenshots where possible)
- Splatoon: the top bar of player icons is the scoreboard; a splatted player's icon changes.
- Valorant, Overwatch 2: minimal top bar, ability bar with integrated keycaps.
- Rocket League: score and time top centre only, boost meter bottom right, the ball always
  findable.
- Fall Guys: almost no persistent HUD, big brief celebrations.
- Fortnite: optional "visualize sound effects" edge indicators for accessibility.

## 3. Design decisions taken from the above

- Minimal and image-first: no sentences in ordinary play (VISION § 3). State is carried by
  shapes, colours and positions: chips with badges, rings for timers, pips for rounds, a thin
  screen-edge frame for danger (Knockout City), glyphs instead of "Can upright".
- One visual family for every in-match surface: warm cream cards (`CourtPresentationPalette.
  Paper` 255,244,222) with warm ink (43,22,11), dark plates (`HudDraw.Plate` 35,29,33 at 235)
  for the clock and powers, gold (255,197,76) for "you", "ready" and "leader", the role colours
  only for rules, seat colours only for identity. The halftime popup already uses this family.
- The HUD must work WITH effects: it recedes during ultimates and replay, uses no full-screen
  tints of its own except the danger frame, and never competes with the can and slippers for
  the colour peak (Sepak U).
- Round pips wear each round's taya seat colour, so rotation is readable without words.

## 4. Implementation state (for resume after compaction)

Baseline captures: `Logs/look-baseline-v1/` (not committed; Logs is local). Code written, NOT
yet compiled or captured, in the ASTRAReworks worktree after `2e537f1f`:
- New `Runtime/UI/HudDraw.cs` (rounded rect, frame, disc, arc, fan, bar), `HudCard.cs` (card
  with shadow, border, accent strip, moment glow, follows High Contrast), `HudRing.cs` (arc
  meter, smooth or notched), `HudPips.cs` (round track in taya seat colours), `HudBadge.cs`
  (filled glyphs: Can, CanDown, Slipper, Crown, Star, Wave, Dot, with optional backing disc).
- New `Runtime/UI/TumpMatchReadout.MatchBar.cs`: top-centre bar of four fixed-seat chips (portrait
  on seat swatch, seat tag, score, state badge, taya border and can badge, gold underline for the
  local player, crown for a unique leader) around a dark clock plate carrying the can glyph with a
  protection ring, the time and round pips; stamina arc right of the reticle, shown only while
  not full.
- Edited: `TumpMatchReadout.CourtHud.cs` (Build calls BuildMatchBar and BuildStaminaArc; status
  rows moved under the reticle), `TumpMatchReadout.cs` (fixed seat order and leader, can glyph,
  corner can readout never shown, personal texts shown only when piloting Kuro or with a live
  slipper stock), `TumpMatchReadout.ScoreFeedback.cs` (moment glow on chips), `HudReadingLayout.cs`
  ("StaminaArc" scales with HUD size), `OwnerAbilitySeal.cs` (dark disc plate, smooth ring for
  skills, 10-notch ring for the ultimate, gold halo when ready).
- Still to do in this step: `TumpPowerReadout.OwnerDeck.cs` (deck bottom right on keyboard and
  pad, bottom centre on touch; corner keycaps as cards; TAB hint only until first use),
  `TumpPowerReadout.DeckRect` plus `SlipperRecall.ClearOfTheDeck` using the real rect, test
  updates (`ScoreFeedbackTests` rank assertion becomes seat assertion), add `VisualHudShots`
  or reuse `TumpNativeHudTests` captures, compile, capture, compare against baseline.

## 5. Published analyses of what works (research added after the owner asked for it)

### Peacocke, Teather, Carette, MacKenzie, McArthur (2018), empirical
"An empirical comparison of first-person shooter information displays: HUDs, diegetic displays,
and spatial representations", Entertainment Computing 26, 41-58.
https://doi.org/10.1016/j.entcom.2018.01.003 (abstract read this session)
- Four experiments, one information type each. No display class wins everywhere.
- Ammunition (what you are holding): diegetic or spatial displays performed best.
- Health (a continuous vital): a HUD performed significantly better.
- Current weapon: best with a redundant HUD icon PLUS the diegetic display (the actual weapon).
- Navigation: a spatial navigation line was best; a HUD minimap was competitive.
TUMP mapping: "slipper in hand" belongs to the viewmodel (diegetic) with at most a redundant
chip badge, never a sentence; stamina belongs on a HUD meter (the arc); "where is my slipper"
belongs to a spatial marker (the recall mark and beam already exist) plus an edge chevron; the
can's state belongs on the can plus one HUD glyph.

### Fagerholt and Lorentzon (2009), "Beyond the HUD: User Interfaces for Increased Player
Immersion in FPS Games", Chalmers University of Technology master's thesis
https://odr.chalmers.se (catalogue entry found; the taxonomy is the widely cited part)
- Four kinds of UI element: diegetic (in the world and in the fiction: the held slipper, the
  chalk), non-diegetic (the HUD), spatial (in the world but not in the fiction: nameplates,
  rings, the recall beam), meta (in the fiction but on the screen plane: a screen-edge danger
  frame, a desaturation when caught).
- TUMP should prefer diegetic and spatial for world facts, non-diegetic only for match facts
  (score, clock, powers) and meta for personal state (danger, caught).

### Celia Hodent, game usability heuristics (The Gamer's Brain; "Understanding the Success of
Fortnite: A UX & Psychology Perspective", celiahodent.com, 2019)
Seven heuristics: signs and feedback; clarity (are the signs perceived); form follows function;
consistency; minimum workload (physical and cognitive); error prevention and recovery;
flexibility. Applied here:
- Signs and feedback: every rule state gets a sign (taggable, restoring, protected, can down).
- Clarity: signs must survive greyscale, 960x540 and the smallest HUD scale.
- Form follows function: shape carries meaning (ring = timer or zone, starburst = contact).
- Consistency: one card family, one badge family, one colour hierarchy for every surface.
- Minimum workload: no reading in play; icons and positions instead of sentences.
- Flexibility: reduced effects, high contrast, HUD scale all keep the same information.

### Apex Legends ping system, UX write-ups
"Apex Legend's 'ping' system: gaming UX done right", UX Collective, 2019
(https://uxdesign.cc, found by search; the system itself is common knowledge)
- Context-sensitive world markers with an icon and a distance replaced most voice and all text
  callouts. Principle for TUMP: a marker's icon says WHAT, its place says WHERE, its colour says
  WHOSE; words are unnecessary.

### Game UI Database (https://www.gameuidatabase.com)
Catalogue of screenshots by screen type. Used for Knockout City above; recommended for any
further comparisons (Results Screen, Player Vitals, Waypoints and Markers categories).

### Splatoon 3, interface notes
Jonathan Copeland, "More on Splatoon 3's interface design" (jcopeland.co, 2023) is a link list
to longer analyses ("The Genius of Splatoon 3's UI" and others) and not itself an analysis.
Transferable points from the game itself: the top bar of player icons IS the scoreboard, an
out-of-play player's icon changes shape, special-ready glows on the icon, everything is drawn
in one sticker-and-ink style so HUD, menus and world signage agree.

## 6. The plan and the ideas, in one place

Rules for every in-match surface, drawn from sections 2 and 5:
1. No sentences in ordinary play. A state is a shape, a colour and a place. Text is allowed
   for names, numbers, the warm-up line and the TAB reference, and for training.
2. World first (diegetic, spatial), then meta (screen edge), then HUD (match facts only).
3. One family: cream cards, dark plates, gold for you, ready and leader; role colours only for
   rules; seat colours only for identity; hero colours only for powers.
4. Timers on the thing they time: rings, never "1.2s" text in play.
5. The HUD recedes when effects peak (ultimates, replay), the can and slippers keep the colour
   peak, and the only full-screen overlay the HUD itself adds is the thin danger frame.
6. Every sign survives greyscale, reduced effects, high contrast and 960x540.

Ideas queued by task (TODO VISUAL-1):
- 1.1 Danger: thin Defense-blue frame round the whole screen edge while taggable (Knockout City's
  targeted frame, in the taya's colour), the live chalk box, the nearest exit segment, an escape
  puff and swish.
- 1.2 Taya view: taggable attackers rimmed in Defense blue for the taya only; a reticle tick in
  reach; the restore ring on the can for everyone.
- 1.3 Timers on objects: own-slipper marker rings; edge chevron when off-screen.
- 1.4 Match bar (built): chips, clock plate, can glyph, pips in taya colours, stamina arc.
- 1.5 One signal language: cut duplicate "lata down" copies; one tint at a time; HUD recedes.
- 1.6 Reticle hub: charge ring, pektus tick, cooldown sweep, refusal state instead of sentences.
- 1.16 Match end: court stays visible behind; winner moment; chips grown into standings; one
  next action; bold graphic shapes in the seat colours (Knockout City's round result, the
  halftime banner).
- 1.17 Halftime and round swap: same card family; fix the 11-unit "NextRole" label.
- 1.18 Icons: filled silhouettes (HudBadge) replacing stroke icons in the match.
- Prompts: keycap badge plus a verb only ("[E] Pick up"), no second line in ordinary play.
