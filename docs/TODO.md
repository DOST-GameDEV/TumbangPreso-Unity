# TODO: Tumbang Preso Unity

Open work, ordered by what is worth doing next. Each entry says what is wrong, where it lives,
and what "done" looks like, so nobody has to re-derive it.

**Check this before inventing a task, and update it in the same commit as the work.** Finished
items move to **Closed** at the bottom with one line on how they were verified.

Read [`VISION.md`](VISION.md) first if you have not. Several entries here only make sense
against the readability budget in its § 2.

---

## 1 · Peer rematch voting across the wire

**The last genuine PARTIAL row in the ledger, and the only one.**

`match_result.gd`'s rematch is a VOTE in a networked match. Here the button acts locally, so
four peers can each think a rematch is or is not happening. Single-player rematch works.

**Needs:** an RPC pair (a peer votes, the host broadcasts the tally), the tally drawn on the
result card, and the same "counts peers, not characters" rule `ReadyGate` already uses, since
bot-filled seats cannot vote.

**Where.** `Assets/TumbangPreso/Runtime/MatchResult.cs`,
`Assets/TumbangPreso/Runtime/Net/MatchRpc.cs`, and `ReadyGate.cs` for the pattern to copy.

⚠️ **This cannot be finished honestly without two real processes on a LAN**, which has never
been run. Write it, cover it in `RuntimeLayerTests` the way reconnect is covered, and say
plainly in the handoff that the wire half is simulated.

---

## 2 · Cheska's Ice Barricade duration was set by accident

**A one-line balance question left open on purpose, because it wants a measurement.**

`CheskaHeroKit.IceBarricadeAbility.OnActivate` calls
`SpawnIceBarricade(position, forward, duration)`. A calibration pass on 2026-08-23 meant to
set the wall's FOOTPRINT to 3.2 m and passed 3.2 into the third parameter, which is the
DURATION in seconds. The signature has no radius parameter at all, so the footprint stayed at
its `HazardVolume` default of 1.6 m and the wall's life quietly went from 6.0 s to 3.2 s.

The 3.2 was kept rather than reverted, on the balance rather than on the history: the skill
cools in 9 s, so a 6 s wall stands for two thirds of every cycle in front of a lata that only
has to survive 90 s. But nobody has measured either value.

**Needs:** a `BotBehaviourProbe` Hero Strike run at 3.2 s and at 6.0 s, comparing knockdowns
against the round and unretrieved-slipper penalties, and the winner written into the call with
its number. The argument is named now (`duration: 3.2f`) so the next reader cannot repeat the
mistake, and the telegraph radius (1.6 m) is asserted against the `HazardVolume` in
`HeroPresentationTests.TelegraphsMatchWhatTheAbilityPlaces`.

**Where.** `Assets/TumbangPreso/Runtime/Abilities/CheskaHeroKit.cs`,
`Assets/TumbangPreso/Tests/PlayMode/BotBehaviourProbe.cs`.

---

## 3 · The five hero accents have not been seen in a real match

**Not a bug. A judgement that needs a human and a played round.**

`UiTheme`'s five hero accents were re-tuned on 2026-08-23 to answer `Art_Direction.md` § 1:
Dante was four degrees off the Offense orange and Cheska twenty off the Defence blue, so both
could read as a role rather than as a hero. The new set is asserted at 25 degrees clear of both
role hues and 30 degrees clear of each other by `HeroPresentationTests`, and the reasoning is
in `docs/Hero_Strike_UI.md` § 3.

**The one worth arguing with is Dante.** His kit is magma and orange is the colour he cannot
have, so his accent is now jade (`#3fa65c`), the colour of the crust, while his fissure light,
embers and magma core stay hot orange through `UiTheme.HeroMagmaCore`. It is defensible and it
is a real change to a character's identity.

**Needs:** a played Hero Strike round, four seats, and an answer to one question: can you still
tell at a glance which player is the taya. If the answer is no the accents move again; if it is
yes, this closes.

---

## Closed

- **First-Person Character-Specific Viewmodel Arms.** ✅ 2026-08-23.
  Customized first-person viewmodel arms (`ViewmodelArms.cs`) with bespoke skin tones, sleeves, wristbands/bracers, and elemental signatures for each hero (Sean, Zack, Dante, Cheska, Nemu, and Classic street mode):
  - Sean: Warm golden brown tan skin, red athletic rolled sleeves with gold trim, fiery orange wristbands with ember warmth, and crimson wraps.
  - Zack: Athletic warm tan skin, high-tech carbon compression sleeves with electric yellow/teal speed stripes, angular tech bracers with glowing lightning conductor plates.
  - Dante: Dark volcanic bronze skin, heavy faceted basalt rock arm guards with jade crust studs, molten glowing magma fissure veins (`UiTheme.HeroMagmaCore`), and volcanic rock knuckles.
  - Cheska: Fair porcelain skin, frost-blue winter coat sleeves, insulated fluffy white cuff trim, crystalline ice bracers with snowflake facets (`UiTheme.HeroIceBright`), and fingerless winter gloves.
  - Nemu: Pale lavender ghostly skin, dark shadow-purple spectral wraps, flowing spirit ribbon wisps (`UiTheme.HeroSpiritBright`), void energy wristbands with glowing runes.
  - Classic: Canonical street tan skin (`ArmColour`), rolled streetwear shirt sleeves, and neutral athletic sweatbands.
  - Preserved camera mounting, all 15 bespoke hero action clips (`PlayAction`), wind-up charge (`WindupRad`), carry poses, and held slipper attachment under `RightPivot/Arm`.
  - Shaded with canonical `ToonSkin` ink outlines and `VfxMaterial` emission.
  - Verified with 56/56 Core tests, 100 EditMode tests (`HeroPresentationTests`), 55 PlayMode tests, and clean Windows standalone build.

- **Hero Ability Animations & VFX Overhaul.** ✅ 2026-08-23.
  Overhauled cast animations and visual feedback across all 5 hero kits (15 abilities total).
  - Built procedural 3D AnimationClips on the 7-bone rig (`HeroAbilityClips.cs`) replacing borrowed generic fallback clips (`dash`, `shove`, `jump`).
  - Added bespoke 1st-person viewmodel animation keyframe clips (`ViewmodelArms.cs`) for all 15 hero cast verbs.
  - Implemented elemental hand empower VFX (`AbilityVfx.AttachHandVfx`) for Sean's Ignition Cannon and Zack's Static Charge.
  - Added responsive cast flash VFX bursts (`AbilityVfx.SpawnCastFlash`) and tightened integration with character squash/stretch.
  - Preserved color laws, readability budget, and Quiesce rules (no auras on Cheska's body, no auras on trail discs).
  - Verified with 56/56 core tests, 98 EditMode tests (`HeroPresentationTests.EveryHeroAbilityHasBespokeCastAndViewModelActions`), 55 PlayMode tests, and standalone Windows build.

- **Hero abilities felt clunky, and the hero UI was cramped and off-brand.** ✅ 2026-08-23.
  One request, seven separate faults, every one verified rather than assumed.

  **It did not compile.** `Hud.BuildAbilityCard` was missing its closing brace at
  `feat/hero-modes-and-abilities-ui-antigravity` HEAD, and `InputBinding.ToHumanReadableString`
  was being reached as an extension method in a file with no `using UnityEngine.InputSystem`.
  Two compile errors, so nothing on the branch had run.

  **Presses were being eaten.** `HeroAbilitySystem.Update` returned before it read the intent
  table whenever `CanAct()` was false, and `JustPressed` is a one-frame edge, so a skill
  pressed during a stun, a stagger or a shove recovery vanished with nothing recording it. A tag
  is a five second stun. There is now a 0.30 s buffer, and only "cannot act" is held: a cooldown
  or charge refusal is answered and cleared. `TheInputBufferIsShortEnoughToBeAnAid`.

  **A refused press looked identical to one that worked.** `TryActivate*` returned a bare
  `bool`, so nothing could tell "on cooldown" from "stunned" from "meter empty", and all three
  drew nothing at all. `HeroKit.CastOutcome` reports which; the tile now flashes hero-accent on
  a cast and `Danger` with `ui_error` on a refusal, inside one frame.
  `ACoolingAbilityAnswersDifferentlyFromAnEmptyMeter`, `ASkillOnCooldownSaysSo`.

  **The ground telegraph lied.** The HUD invented 7.5 m for any ultimate, 5.0 m for any first
  skill and 3.5 m for any second, and offset the ring forward only for Cheska. Nine of twelve
  numbers disagreed with the ability they were drawn for: Dante's 2.4 m stomp drew 5.0 m, Nemu's
  3.2 m void drew 7.5 m centred on Nemu when it lands 3.5 m ahead. `TelegraphRadius` and
  `TelegraphRange` live on `HeroAbility` now and every pair is asserted against the spawn call
  in `TelegraphsMatchWhatTheAbilityPlaces`. The ring also survives 0.35 s past the cast, because
  every one of these fires on the press edge and the held ring was unreachable on a tap.

  **Every see-through effect in Hero Strike was opaque.** `CreatePrimitive` returns the
  built-in `Default-Material`, which is Standard in OPAQUE mode, and writing an alpha into
  `material.color` there does nothing. Thirty-odd effects authored at 0.25 to 0.92 alpha all
  rendered solid. Worst two: Sean's Supernova grew a solid 10.6 m sphere over the camera and
  popped out at full brightness, and Dante's Carapace put a solid sphere around his own head for
  four seconds of a nine second cooldown. `Visual.VfxMaterial` configures Standard for Fade
  properly (the five flags the material inspector normally writes) and 25 call sites go through
  it. It also strips the collider every decorative primitive arrives with: ice shards and
  volcanic debris shipped with rigidbodies AND colliders and were physically shoving players.

  **The hero UI was a different game's palette.** Seventeen `new Color(...)` slate-blue
  literals across `Hud.cs`, `AbilityInspectPanel.cs` and `ConvertedCharacterSelect.cs`, none of
  them in `UiTheme`, against `Art_Direction.md` § 1's "ui_theme.gd is the only place a colour is
  named". 🧑: *"i lowk dont get why we use light blue and shit in some parts of ui, it doesnt
  really look good with brown"*. All seventeen are gone; the chrome is six named constants
  derived from the wood set. Two hero accents were also sitting on the role hues (Dante four
  degrees off Offense, Cheska twenty off Defence) and the whole set was re-spaced.
  `TheHeroChromeIsTheWoodSet`, `NoHeroAccentSitsOnARoleColour`,
  `TheFiveHeroAccentsAreTellableApart`.

  **Four of fifteen ability descriptions were cut off mid-word.** Character select drew
  `Description` into a 46 px box with `VerticalWrapMode.Truncate`, silently, on the one screen a
  player uses to CHOOSE a hero. `Summary` is a short line for that box and `Description` is the
  full sentence for the tray, which no longer truncates. All fifteen were rewritten out of
  shouty marketing copy into plain sentences on request. `EverySummaryFitsTheCardItIsDrawnIn`,
  `EveryAbilityNameFitsItsHeaderRow`.

  Design and reasoning: `docs/Hero_Strike_UI.md`. 95 EditMode tests green, 56 core tests green.

- **The comic callouts were unreadable, and there really were ten of them.** ✅ 2026-08-23.
  🧑: *"they feel diff earlier and weird and overwhelming bcz like 10 show up at once and u
  cant read and they were a weird font"*. Three faults, none of them tunable:

  **The font really was wrong.** They were `TextMesh`, which draws off a font's atlas material
  and does not rebuild when that atlas does. Darumadrop is a dynamic font, so any other text
  requesting a new glyph at a new size re-packs the atlas and every live callout's UVs then
  point at other letters' pixels. Rewritten onto a world-space `Canvas` with a `Text`, which
  re-runs its own layout on `Font.textureRebuilt`.

  **They were blurry.** Rasterised at 64 px and drawn at about 112 screen px, so every glyph was
  being blown up nearly two to one. Now 110 px into 0.48 m plus a 2x dynamic scaler.

  **Ten really did show up.** The cap was 4 and evicted the OLDEST, so a hero exchange threw
  away the score callout and kept four flavour hits. There is a `Weight` now (Flavour, Cast,
  Score) and the LEAST important is evicted; duplicates within 0.35 s and 3 m kick the live one
  instead of stacking; flavour hits past 15 m are not drawn at all. Eight call sites that fired
  one callout per victim were cut: Dante's ultimate alone used to put five on screen in one
  frame.

- **The ability glyphs were a smudge at the size they are actually drawn.** ✅ 2026-08-23.
  A deck tile shows a 128 px sprite at about 40 px, and the old set was line art at 0.06 to
  0.09 stroke, which is one and a half screen pixels. All nine redrawn to one fat stroke
  (`AbilityIcons.Stroke`, 0.16), at most three elements each, solid mass over outline. The
  `Ring` and `Diamond` primitives went with them. `Logs/shots-hero/hero_glyphs_v1.png` draws
  every glyph at 128, 64, 40 and 24 px on the real plate colour; the 24 px column is the test.

- **The intermission banner was the loudest thing on screen and unreadable.** ✅ 2026-08-23.
  `ReadyObjective` was 32 pt of ALL-CAPS `UiTheme.Offense` across 900 px, over a sunlit
  asphalt court, which is both illegible and a role colour used decoratively. It is 20 pt cream
  on a dark plate now, with the role colour on the plate's rim, in sentence case. The ready
  prompt lost three of its four clauses.

- **Dante 3D Model Stray Geometry Fix.** ✅ 2026-08-23. Removed 1,340 stray vertex and triangle
  elements (islands 959-992) from `team-dante.glb` head mesh that formed an asymmetrical floating horn/spike
  protruding through the temple and rear of the character's head. Cleaned binary buffers and re-indexed
  mesh to restore symmetrical head bounds `[-0.195, 0.188]`.

- **In-Game Ability HUD Slimdown & First-Person Hand Clearance.** ✅ 2026-08-23. Redesigned `HeroDeck`
  in `Hud.cs` into a slim, minimalist 240x68 dark glass panel anchored at `y = 10` (down from 592x122 at `y = 24`).
  Replaced cramped 3-line text wrapping with centered high-DPI vector SDF glyphs, corner key chips (`[Q]`, `[F]`, `[X]`),
  and bold centered cooldown countdowns (`4.2s`). Moved `ReadyObjective` to top-center (`y = -210`) and
  `ReadyPrompt` to `y = 96` so game text never obstructs first-person hands.

- **Character Select Ability Ribbon & Tile Polish.** ✅ 2026-08-23. Polished `ConvertedCharacterSelect.cs`
  to style selected ability tiles with a crisp gold/accent glowing border over dark slate glass rather than
  a solid orange background fill. Expanded ability details card to an uncluttered 2-line tactical readout.

- **Ability VFX footprints, procedural particles & UI overhaul.** ✅ 2026-08-23. Calibrated all
  hazard footprints (Cheska Permafrost 2.3m / Barricade 3.2m / Nova 4.6m, Dante Stomp 2.4m / Fissure
  5.5m, Nemu Void 3.2m, Sean Supernova 4.8m, Zack Thunderstrike 4.5m). Introduced `AbilityVfx.cs`
  procedural ParticleSystem generators for ice bursts, magma eruptions, void wisps, and electric arcs.
  Overhauled `ComicPopup.cs` with comic font (Darumadrop One), ink outline layers, and punchy bounce.
  Redesigned `AbilityIcons.cs` with 128px high-DPI procedural vector glyphs and modern tactical shapes.
  Overhauled UI theming across Character Select, HUD Deck, and Inspect Panel, replacing solid bright
  orange tiles with sleek dark glass plates (`rgba(16, 22, 34, 0.90)`) with glowing white/accent glyphs.
  Redesigned Character Select Hero Loadout into a Valorant-style horizontal ability ribbon with an
  interactive details card, eliminating button collisions. Rewrote all ability descriptions across all 5
  hero kits into intuitive, action-driven tactical instructions (`ACTIVATE`, `DEPLOY`, `SLAM`, `PHASE`, `SURGE`).
  Fixed font blurriness by increasing dynamic TTF raster size to 32 and removing unnecessary outline overhead.

- **The 8 PARTIAL rows in `docs/Port_Ledger.md`.** ✅ 2026-08-23, audited against the code
  rather than against the rows. **Seven of the eight were stale bookkeeping**: the work had
  landed and the row was never updated. The audit table is in the ledger's status summary.
  Two pieces were genuinely missing and were written: the music **intensity lift** in the last
  15 s of a round, and the **duck-trigger table** that drops the bed under the countdown, the
  round end, the win and the score award. One row remains genuinely partial and is § 1 above.

- **Load every resource on the BH Studios loading screen.** ✅ 2026-08-23. The preload covered
  the roster, audio and the MAIN MENU scene, and then the arena, its materials, the baked UI
  sprites and the hero kits were all still cold when Play was pressed. It now also warms both
  maps through their assets, every `GodotTheme` box, every ability glyph, the input asset with
  the player's rebinds, and all five kits. `SplashScreen.PreloadGameAssets`.

- **Plan the whole keymap and put throw on left click.** ✅ 2026-08-23. Throw always WAS on
  left click; Grab was on it too, which is why it did not behave like it. There were four live
  collisions in total (left click carried Throw and Grab, E carried Grab, Lunge and Skill 1,
  Q carried Throw and Skill 2). Every action now owns exactly one control:
  left click throws or punches, E is the contextual pick up / shove / reset, right click
  lunges, Q and F are the skills, X is the ultimate, Tab holds the ability panel open.
  `InputMapAndAbilityTests` asserts no control is shared and that throw is on left click.

- **Redesign the skill UI.** ✅ 2026-08-23. Cards are an icon tile with the bound key on a chip
  in the corner; cooldowns drain a smooth bar and the ultimate fills a notched one, so the two
  quantities can no longer be confused; the deck moved out from under the practice prompt; key
  labels come from the live bindings. Descriptions moved off the HUD entirely into a
  hold-to-read panel, and character select now shows every power with its icon, its kind and
  its sentence.

- **Ultimate charging during the ready screen.** ✅ 2026-08-23, and the requirement changed
  mid-flight. Charge now PERSISTS across rounds and is frozen whenever the round clock is not
  running; during the warm-up and the between-round buffer the ultimate is free to cast off a
  practice counter, so it can be rehearsed without spending the meter or earning one by
  waiting. Cooldowns still run in practice, deliberately.

- **Organise the settings controls into groups.** ✅ 2026-08-23. Four headed sections
  (Movement, Playing the game, Hero powers, Round and screen) instead of fourteen unlabelled
  rows. `SettingsGroupsCoverEveryActionExactlyOnce` asserts nothing can fall out of the panel.

- **Hero Strike unretrieved-slipper penalty variance.** ✅ 2026-08-23. Two causes, both
  measured. The probe was unseeded, so the same build measured 110 and then 467 penalties on
  consecutive runs either side of its own 200 ceiling; it is seeded now. And bots walked
  straight through hero hazards on the way to a tsinelas, so `HazardMap` and
  `AIController.AvoidHazards` steer around them. Hero Strike now measures 77 throws, 77
  retrievals, 182 skill uses, 21 ultimates and **1** unretrieved-slipper penalty in a match.
  ⚠️ The avoidance is capped at 3 m until § 1 lands; see § 1.2.

- **The stun frost is very strong.** ✅ 2026-08-23, reduced. Reach 0.36 to 0.24 screen heights,
  body alpha 0.36 to 0.30. Both opposite edges spend the reach, so at 0.36 the clear strip left
  in the middle was 0.28 of the screen height for a five second stun. Still worth a look in a
  real match: § 1.5.

- **The preview idle pose vs the Godot reference.** No need. The character preview was reworked
  in a separate pass; the arms-crossed mismatch in `ModelPreview.PlayIdle` is not being chased.
