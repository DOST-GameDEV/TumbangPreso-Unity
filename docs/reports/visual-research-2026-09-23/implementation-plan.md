# VISUAL-1 implementation plan

The working plan for TODO VISUAL-1. Design and reasoning: `docs/NATIONALS_POLISH.md`, section
"Visual communication and appeal pass (VISUAL-1), 2026-09-23". Research, sources and ideas:
`findings.md` beside this file. Status lives only in `docs/TODO.md`; this file says HOW.

## 0. Standing rules for every task

- No sentences in ordinary play. A state is a shape, a colour and a place (owner: "i really
  dont wanna have to communicate by using text"). Text is allowed for names, numbers, the warm-up
  line, the TAB reference and training.
- World first (diegetic, spatial), then the screen edge (meta), then the HUD (match facts).
  Peacocke et al. 2018: held items belong to the world, vitals to the HUD, locations to spatial
  markers, and the current item to both.
- One family for every in-match surface: `HudCard` cream cards (`CourtPresentationPalette.Paper`)
  with `HudDraw.CardInk` text, `HudDraw.Plate` dark plates, gold for you / ready / leader, role
  colours (Offense `#f87020`, Defense `#0080e8`) only for rules, seat colours
  (`PlayerIdentity.Colour`) only for identity, hero colours only for powers.
- Timers are rings (`HudRing`), never "1.2s" in play.
- The HUD recedes when effects peak; the can and slippers keep the colour peak.
- Every sign survives greyscale, Reduce visual effects, High contrast, HUD 120 percent and
  960x540.
- Supplied owner and girlfriend art is never repainted. Black in-game outlines. No blue in UI
  chrome except `UiTheme.Defense` meaning the taya. No em dashes, sole-author commits.

## 1. Tooling, and the traps this session already hit

- Unity only through `python tools/run_unity_guarded.py <unity args> -tp-profile
  presentation-validation-20260921`. ⚠️ **Never pass `--help` to it: it forwards every argument to
  Unity and opens a graphical editor on the project.** Close such an editor with
  `CloseMainWindow` so the runner's finally block restores the profile; do not kill python.
- Compile plus HUD capture loop (about 4 minutes):
  `python tools/run_unity_guarded.py -batchmode -runTests -testPlatform PlayMode -testFilter
  "TumbangPreso.PlayTests.TumpNativeHudTests;TumbangPreso.PlayTests.ScoreFeedbackTests;
  TumbangPreso.PlayTests.FamiliarReadoutTests" -testResults Logs/look-<task>-vN/hud.xml
  -logFile Logs/look-<task>-vN/hud.log -tp-profile presentation-validation-20260921`
  Captures land in `Logs/shots-native-ui/` (`OwnerHud-Hero-v1`, `CourtHud-Classic-WxH`,
  `CourtHud-held-skills-WxH`, `CourtHud-spectator-WxH`, `CourtBreak-WxH`). ⚠️ A failing case
  stops that case's capture loop, so files after the failure are STALE from an earlier run.
  Copy the frames you judge into `Logs/look-<task>-vN/shots/`.
- ⚠️ **The small-window reading floor is 28 canvas units.** `TumpUiCapture` fails any visible
  Text below 13.95 px at 960x540 (canvas scale 0.5). Every HUD label is 28 or larger.
- After every Unity run, restore generated churn before committing:
  `git checkout -- Assets/TumbangPreso/Resources/Models/FppDetails/inday_left_arm.asset
  Assets/TumbangPreso/Resources/Models/FppDetails/inday_right_arm.asset
  ProjectSettings/QualitySettings.asset ProjectSettings/TimeManager.asset`, and keep excluding
  the two `Resources/UI/composition-redesign/*.png.meta` files. Stage files by path.
- Do not edit `.cs` while a Unity run is in flight. Write docs or research meanwhile.
- `HudContrast` turns every HUD text white and plates named groups; `HudCard` swaps itself to a
  black plate in High contrast, so dark-on-cream text stays legible. New groups that should scale
  with HUD size must be added to `HudReadingLayout.Install`'s name list.
- Tests address HUD objects by name: `OwnerMatchCanvas`, `MatchScores`, `ScoreRow{i}`,
  `PlayerName`, `Score`, `PlayerPortrait`, `RoundClock`, `TimeLeft`, `RoundLabel`, `CanReadout`,
  `LocalState`, `LocalRole`, `SlipperState`, `StaminaLabel`, `ContextualAction`,
  `ActionPrompt`, `ActionDetail`, `MatchToast`, `HitConfirmation`, `PowerSeals`, `Power0..2`,
  `OwnerAbilitySeal` (3 expected). Keep names whose meaning survives; update a test only when the
  behaviour changed by design, and say so in the test comment.
- Pre-existing red, not caused by this pass: `CourtBreak-960x540/.../CourtBreakPopup/NextRole`
  is 11 units; fixing it belongs to 1.17.

## 2. Done so far

- P0 (published `2e537f1f`): reduced-effects link incl. camera shake; native accessibility v57.
- Research record (published `77f3c21f`).
- 1.4 first slice (`4d85395c`): `HudDraw`, `HudCard`, `HudRing`, `HudPips`, `HudBadge`;
  `TumpMatchReadout.MatchBar` (top-centre chips around a clock plate with can glyph and
  protection ring, round pips in taya seat colours on a track, fixed seat order with a leader
  crown, taya border and badge, gold underline for you, state badges, names only for
  spectators, stamina arc beside the reticle, status rows under the reticle, dark pill behind
  the action prompt); `OwnerAbilitySeal` redrawn (dark disc, smooth ring for skills, 10-notch ring
  for the ultimate, gold halo when ready); deck bottom right on keyboard and pad, bottom centre
  on touch, corner keycaps, TAB hint only until first use or after round 1;
  `TumpPowerReadout.DeckRect` and `SlipperRecall` dodging the real deck; `ScoreFeedbackTests`
  asserts the seat's own chip.

## 3. Remaining work, in the TODO priority order

### 1.4 remainder
- Feed (`MatchEventFeed.cs`): at most three pictogram lines on the right edge (portrait, event
  glyph, portrait), fade after about 4 s, no sentences. Event glyphs from `HudBadge` (add
  Knock, Tag, Block, Catch glyphs).
- Toast (`TumpMatchReadout.Toast`, `MatchToast`): the halftime banner language small (brush
  shape `CourtPopupGraphic.Brush` in the moment owner's colour), one at a time, under the bar.
  Score toasts become "+100" drifting from the event into the scorer's chip.
- Hit confirmation: replace the text "×" with a drawn tick shape at the reticle.
- Sandbox and version labels: confirm `GameVersion.ApplyTo` hides the version in a match
  (AGENTS: no version stamps in UI) and that `SandboxState` appears only in practice.
- Measure the permanent HUD area from canvas rects in ordinary FPP play; target under about 8
  percent of 1920x1080. Record the number in the TODO row.
- Capture at 1920x1080, 1280x720, 1280x960 and the owner's short wide window (1680x720), HUD
  100 and 120, High contrast on and off, pad prompts.

### 1.18 icon family
- Replace `TumpAbilitySymbol` stroke glyphs inside the deck with filled silhouettes in the same
  construction as `HudBadge` (solid shape, one detail colour). One per `AbilityGlyph` job (Zone,
  Wall, Dash, Shield, Burst, Projectile, Phase, Slam, Empower); VISION § 3 says the glyph is
  the job, not the hero's element.
- Greyscale test: at 22 units every glyph must be tellable apart from every other.
- Replace `TumpSymbol.Can` in `CanReadout` and any in-match `TumpSymbol` use with `HudBadge`.

### 1.6 reticle hub (`TumpMatchReadout` `_crosshair`, `Carrier.ChargeRatio`, `CurrentPektusSpin`)
- A drawn reticle graphic (dot plus short ticks) replacing the "+" text.
- Charge: a `HudRing` round the reticle filling 0.35 to 1 over the charge; remove the charge bar
  and "Release to throw" from the prompt.
- Pektus: a short curved tick left or right of the ring whose length is the spin; remove
  "Pektus left 34%" and "Move the aim sideways for pektus" (keep the hint in training only).
- Refusal: greyed reticle when a throw would be refused (existing rule), plus the protection
  ring state from the can glyph mirrored as a small lock tick.
- Cooldowns ("THROW CD", "TAG CD"): a thin sweep on the reticle, not status text.

### 1.1 danger made visible
- Screen-edge frame (Knockout City): a thin continuous Defense-blue border round the whole
  screen while the local attacker is taggable; 6 to 8 canvas units, 70 percent alpha, static in
  Reduce UI motion; `TumpHudEffects` owns it (it already knows `threatened`). Keep the 0.36 s
  onset flash. Remove the "You can be tagged" sentence once this reads in the greyscale check.
- Live court boundary: new runtime component (for example `Visual/CourtBoundaryPresentation`)
  that draws the four box edges from `Balance.ConfinementRadius` at the lata's floor height, a
  few millimetres above the authored chalk with Toon depth bias. States: at rest (lata down),
  armed (lata upright, one value step brighter), one outward brightening sweep on restore.
  Chalk colour, never a glow column (the owner rejected a beacon on the can).
- Nearest exit: while taggable, the boundary segment nearest the local player brightens under
  that viewer only.
- Direction option (Valorant's damage arc): when the taya is closing on a taggable attacker from
  outside their view, a short arc of ticks near the reticle on that side. Bounded to what the
  footsteps already tell the ear; owner picks whether it ships on by default.
- Escape beat: crossing out of the box with the slipper while the lata is upright plays a small
  chalk puff at the feet and a soft swish; the frame clears.
- Replay: hide the per-viewer parts during replay; the boundary state follows the recorded can.

### 1.2 the taya's view and the restore clock
- Taya only: taggable attackers get a Defense-blue rim (VALORANT's friend-or-foe fresnel:
  stronger on up-facing grazing angles and the upper body, toned so it never distracts) through
  `CharacterVisual`'s existing `_RimColor` / `_RimStrength` property block, and their `CharacterNameplate` ring switches to a
  "catchable" shape (extends TODO § 127's ring and disc rule).
- Reticle ready tick when a taggable attacker is inside punch range (1.7 m, 75 degrees) or
  lunge reach (1.3 m).
- Restore ring for everyone: a `HudRing`-style world ring on the lata collar filling with the
  taya's `Carrier.ChannelRatio`; after restore the collar drains with `Lata.ProtectionLeft`.
  Record both for replay.

- Distance readability for everyone (VALORANT's depth adjustment): characters and the lata
  brighten slightly and their rim widens with distance, clamped, so an 8 to 12 m chibi still
  reads. Same property-block path; no gameplay information added.

### 1.3 timers on objects
- Own-slipper marker (`SlipperRecall`, `SlipperRecallMark`) gains a ring for the fetch grace
  and the penalty (`TournamentRules.IsSlipperWarning`) and for roof and lagoon returns. Remove
  "Fetch your slipper · ..." and "Slipper returning · ..." from the prompt detail line.
- Edge chevron in the seat colour when the slipper is off-screen (the recall mark already clamps
  to the edge; confirm and restyle rather than add a second system).

### 1.5 one signal language
- Write the meaning, shape, colour and place table from NATIONALS_POLISH V2 as a header comment
  on each owner class and make them obey it.
- Cut "lata down" to the can itself, the bar glyph and one moment beat; remove the rest
  (`Lata.BuildDownBeacon` comment lists all six).
- One full-screen tint at a time: priority across `DamageVignette`, `DownedVignette`,
  `FrostVignette`, caught and moment grades.
- Non-critical HUD recedes to 0 to 30 percent during an accepted ultimate and in replay.
- `OffscreenIndicators` "CAN PROTECTED": glyph only, in the chevron family.
- Experiments to try and keep only if clearer: peripheral speed lines on sprint and dash; a
  brief lower-saturation world grade during a shared ultimate; sound visualisation edge pips
  (owner picks the default).

### 1.16 match end (`MatchResult*.cs`, `ConvertedMatchResult.cs`)
- The court stays visible behind (as halftime): the winner's model or camera beat, then a card
  stack in the HUD family: winner chip grown large with a crown, the other three chips in rank
  order with final scores, one highlight each from existing recognition facts, one obvious next
  action (Rematch), the rest as quiet secondary actions. Knockout City's round result is the
  graphic reference: bold diagonal slashes in seat colours, big numbers, one line.
- Rocket League's podium is the second reference: the players posed ON THE COURT under one big
  winner line, each with a name chip and TWO accolades with numbers taken from match stats
  (`MatchStatsCollector` and the recognition facts: knockdowns, tags, blocks, longest throw,
  escapes, seconds defended with the can up). No invented stats.
- Keep every existing route: rematch, next map, main menu, ranked readouts, pad and touch focus.

### 1.17 halftime and round swap (`TumpRoundSwapView*`, `HalftimePresentation`, `RoleSwapCard`)
- Same card family as the bar and the match end; standings as chips; the next taya as the one
  highlighted fact; the court visible. Fix `NextRole` to 28 units.
- Move the popup off the viewmodel's lower centre (upper third), keep it compact.

### 1.7 viewmodel (`ViewmodelArms.cs`, `CameraRig.ApplyLens`)
- The arms render through the gameplay camera, so the 75 to 110 FOV slider AND the sprint lens
  kick resize them. Render them at one authored FOV (a viewmodel camera layer or a projection
  override) as every major FPS does (UE5 "First Person Rendering", TF2 `viewmodel_fov`).
- Overwatch and Valorant frame the hands low and split or pushed right, leaving the centre
  clear. Lower and slightly smaller rest pose; lit toon bands; rim on the held slipper; ink matched to
  on-screen character weight; throw anticipation inside the existing charge window.

### Batch B: 1.8 lighting, 1.9 court, 1.10 hero objects
VALORANT's shading article is the technique reference: one directional light styled through a
GRADIENT (remap N.L to 0..1 and sample a small ramp texture that sets the lit, mid and shadow
values and hues and where they meet). In `Toon.shader` that replaces the fixed `_ShadowBand`
with a per-map ramp (a hard two-step ramp keeps TUMP's look) plus a warmer ramp for skin.
Nothing that carries gameplay information may depend on cast shadows, because Low settings
may cut them. The lata's metal highlight: a hard band keyed to the sun direction, never across
the owner-drawn labels. Can findability: a ground ring under the can while it is airborne or
far, as Rocket League does for its ball.
See NATIONALS_POLISH V3. Order: ambient ratio and shadow tint in `MapAtmosphereAuthor.Apply`
and `Toon.shader` (with a face rule so no band edge crosses a flat face, per the Hi-Fi RUSH
face-shadow idea), then per-map court medium (chalk on dark asphalt, charcoal or brick on
light paving), then the lata highlight and landing marker and slipper flight streaks.

### Batch C and D: 1.11 to 1.15
See NATIONALS_POLISH V3. 1.11 starts with the confetti cubes in `HeroHazards` near line 4048
and the `Sprites/Default` trails in `Slipper.cs` and `CanContactAccent`.

## 4. Evidence per task

Same-camera before and after, a 25 percent greyscale thumbnail, the smallest test that covers
the changed behaviour, and one line in the TODO row with the capture folder. A fresh internal
Windows build (never the Desktop target) at the end of each batch so the owner can play it.
