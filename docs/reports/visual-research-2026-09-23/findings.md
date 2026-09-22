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

### 4b. Second 1.4 slice, measured (look-1.4-v3)
- Permanent HUD in ordinary FPP play, measured from canvas rects on a 10-unit grid at 1920x1080:
  Classic 3.72 percent (clock plate 0.81, each chip 0.61, round track 0.32), Hero Strike 5.30
  percent (plus the three power discs, 1.36 together). The pre-pass HUD was estimated at 13 to 17.
- The pictogram feed reads in the 25 percent greyscale thumbnail because each event is a
  different outline (burst with a lying can, burst with a hand, a shield, a turning arrow).

### 4c. Batch A HUD, observed (look-batchA-v4)
- High contrast exposed one fault the default look hid: dark-ink state badges on a chip
  that High contrast turns black. Badges now follow the card.
- The drawn reticle's five states separate in greyscale by shape (ring, arc side, sweep,
  grey, chevron), not only by colour (`reticle-states-grey.png`).
- The halftime popup no longer covers the viewmodel; the court and the held slipper stay
  readable under it at 1600x680.

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

## 7. Further references for the remaining tasks

### Tango Gameworks, "3D Toon Rendering in 'Hi-Fi RUSH'", GDC 2024 (Kosuke Tanaka, Takashi Komada)
GDC Vault and https://www.youtube.com (GDC channel, 57:38); summary at 80.lv, 2024-07-10.
Topics named in the summary (the talk itself was not watched this session): deferred toon
rendering, a comic shader, toon light, shadows, a static shadow map, global illumination and a
dedicated toon FACE shadow. For TUMP (VISUAL-1.8): faces are flat and must never be cut by a
two-band terminator, so the character pass needs a face rule (either bias the face toward the lit
band or exclude the head from the band edge) before the shadow tint lands; the comic-shader idea
maps to optional halftone in shadow only on large environment surfaces, never on characters.

### First-person arms at a fixed FOV (VISUAL-1.7)
Standard practice across engines and shipped games: the viewmodel is animated for one field of
view and rendered at that FOV whatever the player picks, so the arms keep their size and framing.
Unreal Engine 5 documentation, "First Person Rendering" (dev.epicgames.com); Bevy engine example
"first person view model" (bevy.org); Team Fortress 2's separate `viewmodel_fov`; Unity forum
threads on reprojecting the weapon to a fixed FOV. TUMP added a 75 to 110 world FOV slider in
127.3, so the arms currently grow and shrink with it; 1.7 should render them at one authored FOV
(a second camera layer or a projection override on the viewmodel) and keep the world slider.

## 8. Gap-filling research (second pass, before publishing)

### Rocket League (Psyonix), Game UI Database gameData.php?id=134
- In play: ONE compact score bar at top centre, team scores as solid colour blocks either side of
  the clock. Nothing else persistent except a small rank chip top right.
- The ball carries a white ground ring under it, so it is always findable (VISUAL-1.10: the lata
  and an airborne can get the same).
- Goal moment: an orange starburst badge "GOAL +100" plus one big line naming the scorer; replay
  marked by a small red dot and word at bottom left.
- Match end (VISUAL-1.16 model): the winning team's cars pose IN THE ARENA under a huge outlined
  "WINNER ORANGE"; each player has a name banner and TWO earned accolades with numbers
  ("SCORER 2 Goals", "STRIKER 2 Shots on Goal", "TACTICIAN 6 Centered Balls", "ENFORCER 36 Cars
  Bumped"). TUMP can pose its players on the court with chips and two accolades each from match
  stats (knockdowns, tags, blocks, longest throw, escapes, time as taya with the can standing).

### Riot Games, "Art Education: Visual Effects" (riotgames.com/en/artedu/visual-effects)
- VFX artists "carry the heavy burden of restraint so players can actually tell what the hell is
  going on".
- Every effect must accurately represent an action or state, stay thematically cohesive, and work
  across the kit, across the roster and across the whole game; each must clearly belong to its
  character or source.
- Recommended reading: Joseph Gilland, "Elemental Magic" (effects animation: shape, timing,
  anticipation, dissipation). Applied in VISUAL-1.11.

### Riot Games (Brandon Wang), "VALORANT Shaders and Gameplay Clarity", 2020-06-30
(riotgames.com/en/news/valorant-shaders-and-gameplay-clarity, read this session)
- Pillars: performance, competitive integrity, art, balanced equally. No dynamic shadows in the
  playable space, because Low settings that cut them would lose information: readability must
  not depend on a setting.
- One directional light, "style the heck out of it". Gradient Lambert: remap N.L to 0..1 and
  sample an artist gradient that sets highlight, midtone and core shadow brightness AND where
  the transitions sit. VISUAL-1.8 route: a small ramp texture per map (and a warmer one for
  skin) sampled by `Toon.shader` in place of the fixed `_ShadowBand`, keeping a hard two-band
  ramp so the look stays TUMP's.
- Ambient from sampled level lighting, with per-area clamps on maximum darkness and brightness.
- Specular from a painted HDR panorama whose hotspot sits in the sun's direction, so highlight
  and diffuse agree. VISUAL-1.8 route for the lata: a matcap or a hard band keyed to the sun.
- Friend-or-foe FRESNEL: a coloured rim on characters, stronger on UP-facing grazing angles and
  the upper body, modulated so it is not distracting. VISUAL-1.2's taya-only rim on taggable
  attackers is exactly this, in Defense blue, biased upward.
- Depth adjustments: distant characters are brightened and their rim grows, clamped, so they
  stand out as detail breaks down. TUMP's chibi cast at 8 to 12 m needs the same.
- Skin: diffuse falloff shifted toward red in the dark side (a cheap subsurface look). TUMP's
  shadow tint should be warmer on skin than on the environment.
- Cast shadows only on first-person objects (own hands and weapon).
- Environments cheap and quieter; "players are usually mentally blocking out the environment".

### Splatoon 3, press image (dualshockers.com feature image, no HUD visible)
- The world carries the information: team ink on the ground, a lock-on ring on the target, a
  dashed aim arc for a charger. The top bar of player icons is recalled from the game, not
  verified from a screenshot this session.

### Tumbang preso as it is played (search results: Scribd "Bato Lata", Wikipedia "Traditional
games in the Philippines", community posts)
- A chalk circle holds the can; a straight toe line or "home base" is drawn about 2 to 2.5 m
  away; tsinelas, a chalk circle and friends are the whole kit; also called "Manila Lata" in
  Bisaya; some play at dusk or under a full moon. VISUAL-1.9: the circle and the line are the
  game's signature marks and must be the most legible chalk on every map.

### Valorant (Riot), Game UI Database gameData.php?id=1043, in-match frame viewed
- Top centre: the round timer in a small plate, teammates' portraits (small chips with a thin
  state underline) and the team score on the left, opponents' on the right. This is the match
  bar's layout almost exactly.
- Top right: kill feed as pictograms on team-colour plates (portrait, weapon icon, portrait).
- Bottom centre: health number left, abilities as one thin row of icons with tiny keycap letters
  under them and charge pips as dots, ammo right. Minimap top left.
- DAMAGE DIRECTION: an arc of short ticks drawn near the crosshair on the side the hit came
  from. For TUMP (VISUAL-1.1): the taya closing from outside your view can use the same arc near
  the reticle instead of text, bounded to what the audio already tells you.
- Viewmodel pushed to the lower right, leaving the centre and left clear (VISUAL-1.7).

### Overwatch (Blizzard), Game UI Database gameData.php?id=1341, in-match frame viewed
- Top centre: timer, a short objective word and a progress track for the payload.
- Objective in the world: an icon with a small label and a SILHOUETTE outline of the payload
  through walls, so it is always findable (VISUAL-1.10 and the can).
- Bottom left: portrait, health number and segmented bar. Bottom centre: the ultimate as a
  large ring with a percentage (the notched F ring). Bottom right: abilities with keycap labels
  and ammo. Crosshair: a small circle.
- Viewmodel hands low and split left and right, leaving the centre clear (VISUAL-1.7).

### Not covered, stated plainly
- Omega Strikers (Game UI Database search found nothing) and Fall Guys (the catalogued entry
  holds menus and the level editor, no match HUD) were not viewed; points attributed to them are
  general knowledge. Splatoon's top bar is recalled, not viewed. Audio references were not researched (VISUAL-1.15 audio should start with the
  existing Asset_Sourcing and AudioDirector notes). No performance profiling references beyond
  the earlier draft's Riot profiling article.

## 9. World continuation from090d8c4a,2026-09-23

Owner scope excludes the completed in-game UI/HUD.1.1 begins with the world half.
Source finding: Confinement is a strict square about world origin; a body exactly
on a line is outside. Ilalim's side chalk is raised onto the kerb, so drawing every
new boundary vertex at the lata's flat floor height would recreate the old buried-
chalk defect. Sample the actual ground near each edge and preserve the exact X/Z
rule. Disable only recognised Chalk boundary renderers while the new treatment is
on, restoring them for baseline/off and teardown; never remove their colliders or
the home/throw marks. Detect escape from real crossings and unchanged movement epoch,
not merely taggable becomingfalse (which also happens on a tag, blink or role change).
Personal exit/air cues belong only to the active local camera. Recorded playback
uses recorded can flags for the shared boundary and no personal cue. No rule,
network message, HUD or timing changes. Latest owner validation discipline defers
native builds/full regression to P7, superseding per-batch builds in the old plan.

1.1 first rendered look (look-1.1-v3): all five surfaces/rule bounds passed, but
rest versus armed is too subtle in the inspected25percent greyscale sheet. The
second look increases the armed ink/chalk width while keeping rest thin/dimmer;
no new filled area inside the court and no glow. Source check also corrected a
false assumption: MovementEpoch alone does not identify offline teleports. A
presentation-only discontinuity serial is required; no gameplay wire changes.

Correction from actual runtime discovery (look-1.1-v7): Mat_chalk is a material
name; the node names are default and their imported meshes are read-only. The
four edges are separate, exact thin renderer bounds at+/-7. Earlier speculation
about combined chalk was wrong. The final route recognises material plus whole-
edge bounds and toggles those renderers; it does not split or alter any mesh.
Throw marks at+/-8, outer lines at+/-12.5 and the home ring remain separate/intact.

1.1 local close-up/escape study (v9): the central puff was fully obscured by the
feet despite a passing trigger test. Move it to two small lateral wisps. The near
line looked like heavy rails, so width is reduced at close range and grows smoothly
to the already-reviewed far width by8m. This is the third/final boundary look;
retain the far-map evidence and capture only the changed close-up/escape views.

Final1.1 evidence: look-1.1-world/report.md. v8 five-map geometry/state passes;
v11 nested-camera ownership, actual settled crossing, teleport rejection and the
appearance study pass. Dynamic batching had pretransformed billboard vertices,
misplacing its object origin: DisableBatching fixes it. Personally inspected full
local/escape frames and all25percent-grey samples; retain the third subjective
look and stop iterating. The wisps sit outside the feet and fade over0.42s. Far
branch is unchanged from inspected v8 map frames. Replay shared state/audio mute
proof from v3 is retained. No native or human-listening/stranger-review claim.

1.2 source discovery: Carrier.ChannelRatio only advances on the controlling peer;
the host separately times validated remote reset requests. Repeated grab animation
is not a safe clock (cancellation and late joins lose timing). Existing SyncLata
therefore gains an optional version1 suffix with two finite presentation floats,
while retaining the entire original prefix and host-only handler guard. New peers
read legacy messages; old readers ignore the suffix. During an active clock it
updates at10Hz plus edges. This does not change gameplay protection, authority,
reset admission or network protocol50. Real multi-peer transport stays P7.

The existing bounded recorded can State has room for an8bit clock plus a presence
bit; upright/protected flags choose restore versus protection. Values stay below
2048, so clip wire11 and its decoder bound remain unchanged. Legacy clips do not
invent missing timing. Camera-scoped taya material/mesh overrides restore their
originals even for nested cameras, preserving identity and authored power state.

1.2 inspected evidence: v3 passed3/3behavior checks; v4 passed target/clock2/2.
The first clock witness was blocked by the taya, so the corrected angle is from
the front/side. The absent blue arc was initially attributed to can occlusion; v5 source
inspection disproved that diagnosis: freezing Time.timeScale makes CanAct false,
so Carrier cancelled the photographed hold. The corrected fixture preserves an
actual accepted channel and asserts progress/visibility at capture. Clock look2
billboards its close ring around the real body centre with normal depth testing;
its final appearance remains pending this corrected capture. It is a
shared world timer, not an immunity zone, and cannot show through a wall.
Catchable brackets gained a6.5cm lip for the low10m eye angle; target look2 retained.

Carry into1.11/1.12 review, not another1.2 task: the actual grab/restore gesture
produces a large red crescent in these frames. Assess its meaning and dominance
against the shared small blue clock during the effects/choreography batch.

Final1.2: v7 actual-held clock/cancel/protection and unchanged quantization bound
passed11.383s,guard0f87a3f9bb6b. Inspected blue and gold arcs in full frames and
all25percent-grey comparisons. The fixture now asserts active progress/renderer
at capture and waits the release's Update/LateUpdate handoff. Retain target look2
from v4 and close viewer-facing clock look2 from v7. STOP tuning/retesting; move
to1.5. Source review handles destroyed registry keys and changed can skins. Report
look-1.2-world records evidence limits and earlier invalid capture diagnoses.

1.5 implementation research: Unity's [DrawRenderer API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.CommandBuffer.DrawRenderer.html)
does not initialize lighting data for a custom draw, so the protected-object mask
uses an unlit shader. [Matrix API documentation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.CommandBuffer.SetViewProjectionMatrices.html)
confirms the Built-in route and camera-space convention. This implementation uses
explicit view/VP uniforms to avoid changing camera globals, checks actual scene
depth to reject hidden objects, and allocates nothing at experiment-off. This is
an implementation choice inferred from the documented constraints; rendered colour
retention and occlusion are still being tested, not claimed from API support.

1.5 source reconciliation: the supposed world LATA DOWN popup is already absent
from both Lata.AnnounceUprightChange and MatchFlair.Kind.LataDown at88f9e6e68.
Comments/TODO were stale. Preserve the current contact/voice/moment and completed
HUD. The new experiments are deliberately separate0default authoring knobs until
the owner chooses defaults after reviewing the comparisons. Sound pips consume
actual world-step events in the existing2..32m audio envelope, with pre-slider
gain for muted/deaf play. They add no enemy identity or unplayed proximity cue.

Final1.5 comparison: v2 passed3/3,19.393s; accepted ultimate keeps actual body colour
while surrounding world and a wall hiding that body desaturate. v3 passed2/2,
13.211s with corrected FPP captures. Normal and25percent-grey images inspected.
Speed lines are quiet at the periphery and absent in comfort mode. The hollow
sound diamond is intentionally subtle; a detail crop confirms its actual edge
placement against the house. It survives reduced-effects mode because it carries
heard information and has no moving stroke. World desaturation restores full
colour in comfort mode. Retain first usable looks, with all3defaults OFF pending
owner selection. The broken v1 shader and v2 own-head captures are preserved and
not claimed as visual evidence. No more tuning before other unfinished features.
