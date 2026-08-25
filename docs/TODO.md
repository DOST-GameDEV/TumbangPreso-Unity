# TODO: Tumbang Preso Unity

Open work, ordered by what is worth doing next. Each entry says what is wrong, where it lives,
and what "done" looks like, so nobody has to re-derive it.

**Check this before inventing a task, and update it in the same commit as the work.** Finished
items move to **Closed** at the bottom with one line on how they were verified.

Read [`VISION.md`](VISION.md) first if you have not. Several entries here only make sense
against the readability budget in its § 2.

---

## 0 · Hero Strike is being reworked, and the plan is its own file

**Numbered 0 rather than 1 on purpose: every other entry here keeps the number it already had,
because `VISION.md`, `CLAUDE.md` and two handoffs cite them.**

The measurements and the proposal are [`Hero_Strike_Balance.md`](Hero_Strike_Balance.md). Do not
copy them here; that is how § 1 came to be pointed at by `VISION.md` § 2 while holding something
else entirely.

**The three things it found that are facts rather than proposals**, all derived from the code
with no Unity launch:

1. ⚠️⚠️ **Ilalim ng Tulay renders black, and it is one wrong constant.**
   `IlalimNgTulayBuilder.cs:192` sets the map's tonemap exposure to **0.15** where Eskinita uses
   **0.92**. Every linear pixel below **0.59** clips to pure black before it reaches the screen.
   The derivation is § 3.0 of the plan. **This outranks every other item in the file**, because
   nothing else on that map can be judged through it. Reported from the built player, not a test.
2. **Zack's Bolt Sprint paints 27.2 % of the arena off a 6.0 s cooldown**, which is more floor
   than any ultimate. Invisible to every previous pass because the trails were always measured
   one disc at a time and neither trail ability places one disc.
3. **The ultimate meter is a timer.** `UltimatePassiveChargePerSecond` 1.0 against
   `UltimateMax` 100 hands a player who does nothing 90 of the 100 in a 90 s round, which
   `VISION.md` § 4 forbids in as many words.

**Status:** 🧑 has the plan and is arguing with it. **No code has moved.**

**Folds in §§ 2 and 5 below**, both of which the plan answers: the barricade duration becomes a
consequence of the charge economy (§ 3.2) rather than an open measurement, and the overclock rate
loses four fifths of its value the moment cooldowns get long (§ 4.5).

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

⚠️ **§ 0's plan may answer this without a probe run.** Under the charge economy in
`Hero_Strike_Balance.md` § 3.1 the barricade becomes ONE charge per round, and a wall you get
once a round has to be worth the charge. § 3.2 proposes restoring **6.0 s** on that ground rather
than on the A/B. If the charge economy lands, this entry closes as a consequence; if it does not,
the measurement above is still the way to settle it.

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

## 4 · Bayan Plaza's monument stands inside the defender's box

**Found by `MapGeometryCheck`, not by playing, and it is a Hero Strike fairness problem.**

`BayanPlaza/Obstacles/MonumentBody/CollisionShape3D` occupies **0.70 m by 1.90 m of the chalk,
from y = 0.10 to y = 5.10**. The taya is CLAMPED into that box (`Confinement.ClampToBox`) and
cannot step out to walk around it, so one approach to the can is permanently shielded for
whoever is defending.

In Classic that is a quirk you play around. In Hero Strike it is a coin flip: a wall or a zone
placed against the monument closes a lane outright, so the seat that draws the taya round with
the good geometry has a different game from the other three, and `docs/VISION.md` § 4 says the
mode is aimed at a bracket.

**Needs:** the monument moved to the plaza edge outside |x|, |z| = 7.0, or its collider reduced
to something below `CharacterController.stepOffset` (0.30 m) so it is a plinth rather than a
wall. Then add `BayanPlaza.unity` to `MapGeometryCheck.Gated`.

⚠️ **The scene is an IMPORTED `.tscn`, not built from code**, so this is a scene edit rather
than a builder change. That is also why the map is only reported on today and not gated.

**Where.** `Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity`,
`Assets/TumbangPreso/Editor/MapKit/MapGeometryCheck.cs`.

---

## 5 · The overclock window has not been measured against a match

**A new Hero Strike mechanic with a defensible number and no evidence behind it.**

`OverheadPassWindow.OverclockRate` is **2.0** for the 2.70 s the LRT consist is over the street,
every 24 s. The reasoning is in `docs/Ilalim_Ng_Tulay.md` § 3.5 and it is sound: it pays a
player who is already casting rather than one who is waiting, so it cannot violate
`docs/VISION.md` § 4. What nobody has is the number.

At 24 s intervals and a 2.70 s window, the mode spends **11.25 per cent of a round** at double
cooldown rate. Against a 9 s skill that is roughly one extra cast every four cycles for a player
who plays around it, and zero for one who does not. Whether that gap is "a skill" or "a tax on
not knowing" is the open question.

**Needs:** a `BotBehaviourProbe` Hero Strike run on this map at `OverclockRate` 1.0 (off), 1.5
and 2.0, comparing skill uses, ultimates and knockdowns per round. The winner goes into the
constant with its number, the way every other measured value in this repo does.

⚠️ **The probe runs on Eskinita today.** Pointing it at a second map is part of the work, and it
is worth doing anyway: § 4 above and `docs/Ilalim_Ng_Tulay.md` § 1 are both arguments that map
geometry changes Hero Strike outcomes, and nothing in the harness has ever measured that.

⚠️⚠️ **DO NOT RUN THIS A/B UNTIL § 0 SETTLES, BECAUSE THE COOLDOWNS MOVE UNDER IT.** Doubling
the rate saves 2.70 s of cooldown whatever the cooldown is, so the RELATIVE value collapses from
41 per cent of a 6.5 s cycle to 7.9 per cent of the 34 s cycle `Hero_Strike_Balance.md` § 3.1
proposes. Measuring 1.0 against 1.5 against 2.0 today measures a mechanic that is about to
change shape. § 4.5 of that file argues the multiplier should become a flat saving or a charge,
which survives any later retune.

**Where.** `Assets/TumbangPreso/Runtime/Map/OverheadPassWindow.cs`,
`Assets/TumbangPreso/Tests/PlayMode/BotBehaviourProbe.cs`.

---

## 6 · `AiDiagnosticProbe`'s Classic round is a real-time test and it flickers red

**Found on 2026-08-25 while verifying an unrelated fix. It is NOT a gameplay regression, and
the evidence for that is written down here so nobody re-derives it.**

`OneClassicRoundAtRealSpeedIsFullyExplained` asserts no tsinelas stays loose longer than
**20.0 s**. It failed twice in a row at **21.6 s** and then **29.9 s**, and passed on the same
machine minutes earlier.

⚠️ **It runs at 1x for 40 real seconds by design** (see the class note: anything measured at a
high time scale is partly a measurement of the harness). That makes it the one test in the
repo whose result depends on how busy the machine is, and two consecutive failures 8.3 s apart
in value is the signature of a frame-rate difference, not of a behaviour change.

**Why it is not the hazard fix that landed the same day:** Eskinita, the map it loads, contains
exactly four MonoBehaviours (`EnvColourPass`, `KillPlane`, `MapGrade`, `MatchInstaller`) and no
`HazardVolume` or `StreetTripHazard` at all, and Classic casts no hero abilities, so `HazardMap`
is empty for the entire run. `OneHeroRoundAtRealSpeedIsFullyExplained`, which is the mode that
does populate it, passed in the same suite.

**Needs:** a decision, not a bug hunt. Either the bound moves with a measured reason, or the
probe stops asserting on wall-clock-sensitive quantities and only prints them, or it is marked
explicit-run-only so it stops costing a full PlayMode suite to learn nothing. The worst outcome
is the current one, where a red result carries no information and the next session spends a run
finding that out again.

⚠️ **The second failure is worth one look before deciding.** It printed
`own=3 plan=Fetch ownerAct=True d3=1.10 grabbable=True`: a bot 1.10 m from a grabbable slipper
it had already decided to fetch, still not holding it. If that is reproducible at a normal frame
rate it is a real retrieval bug and this entry becomes a gameplay one.

**Where.** `Assets/TumbangPreso/Tests/PlayMode/AiDiagnosticProbe.cs:242`.

---

## 7 · The test suite costs more to run than it is currently returning

**Raised by 🧑 on 2026-08-25: *"we have too many tests and we are wasting so many credits to run
them all and fix the code for the test"*. This is a real constraint and it belongs on the list
rather than in a chat log.**

A full verification pass today is Core (1 s) plus EditMode (105 tests) plus PlayMode (55 tests,
several of which run whole matches at 1x) plus four separate editor checks, and **each of the
last five is its own Unity launch**. The launches, not the assertions, are the cost.

⚠️ **THE ANSWER IS NOT TO DELETE TESTS, AND SPECIFICALLY NOT THE MEASURED ONES.** `CLAUDE.md`
§ 7.1 lists three faults that no amount of playing would have found, and the crash closed on
2026-08-25 was caught by nothing at all and cost a whole session. Coverage is not the problem.
**Cadence and batching are.**

**Needs, in order of payoff:**

1. **One launch, many checks.** `ArenaCheck`, `MapGeometryCheck`, `AudioCueCheck` and
   `SceneScriptCheck` are four `-executeMethod` launches that could be one entry point running
   all four and exiting non-zero if any fails. That is the single biggest saving and it changes
   no test logic.
2. **Name a fast gate and a full gate.** Fast: Core plus EditMode plus the combined checks, for
   every change. Full: adds PlayMode, for anything touching gameplay, and before a build.
   Right now every change pays for everything.
3. **Take the wall-clock probes out of the default PlayMode run** (§ 6). `AiDiagnosticProbe`
   alone is 80 real seconds of the suite and produces a report to read, not a pass to rely on.

**Done looks like:** a documented two-tier command list in `docs/TESTING.md`, and a full pass
that is fewer than four Unity launches.

---

## Closed

- **The shipped build hard crashed the moment a player selected Ilalim ng Tulay.** ✅ 2026-08-25.
  Reported from the actual .exe, not from a test.

  **The symptom lied.** `Player.log` read
  `The file '.../TumbangPreso_Data/level8' is corrupted! Remove it and launch unity again!`
  followed by `[Position out of bounds!]` and a native `Crash!!!`. Nothing was corrupt. Every
  serialized file in the build parses clean: headers self-consistent, all 12,045 objects in
  `level8` inside the data section with zero overlaps and zero slack, all 8 external references
  present, and all 78 mesh and texture streaming records inside their `.resS`.

  **The cause.** Eight `HazardVolume` components in the scene had an `m_Script` pointing at an
  inline `!u!115 MonoScript` document written into the scene file itself rather than at a script
  asset. Unity emits that stub when it cannot resolve a `MonoScript` for a type, which happens
  whenever **the class name does not match the file name**: `HazardVolume` was declared at line
  182 of `HazardMap.cs`. The player has no layout to deserialize the component against, reads
  past the end of the object, and dies.

  **Why nothing caught it.** Core 60/60, EditMode 105/105, PlayMode 55/55, HeadlessCheck,
  ArenaCheck, AudioCueCheck and MapGeometryCheck were all green on the commit that shipped it.
  ⚠️⚠️ **Every one of them runs in the editor, and the editor resolves the stub by class name.**
  This failure is invisible to any in-editor check by construction. Every other `HazardVolume`
  in the game is attached at RUNTIME by `HeroHazards` and `StreetTripHazard`, where nothing is
  serialized and the defect cannot occur; Ilalim ng Tulay is the first map to bake one into a
  scene at author time (`IlalimNgTulayBuilder` attaches one per LRT pillar), which is why one
  map and only one map crashed.

  **The fix, in three parts.** `HazardVolume` moved to its own `HazardVolume.cs` with a note
  saying why nothing may be merged back into it. All three affected scenes repointed at real
  script assets: `IlalimNgTulay.unity` (8 x `HazardVolume`), and two that never shipped and had
  never been mentioned anywhere, `CharacterSelect.unity` (1 x `ConvertedCharacterSelect`) and
  `VerticalSlice.unity` (4 x `EmotePlayer`) whose stubs were stale rather than structural, since
  both of those classes already live in correctly named files. Then `SceneScriptCheck`, which
  fails any build scene carrying a stub, a guid-less `m_Script` or a guid that resolves to
  nothing, reading the scene as TEXT because opening it is what hides the fault.

  **Verified** by reintroducing the exact defect into `IlalimNgTulay.unity` and confirming the
  new check exits 1 and names all nine findings, then restoring and confirming it exits 0 across
  all 9 build scenes and 8 non-shipping ones. `GameBuilder` now runs it before every build, and
  the rebuilt player reaches the map with no `MonoScript` object in `level8` at all.


- **Ilalim ng Tulay looked assembled rather than lived in.** ✅ 2026-08-25. Four faults, all
  found in renders and all fixed against renders rather than against prose.

  **The PC Express sign was not the PC Express mark.**
  `tools/build_pc_express_logo_mesh.py` kept only the WHITE pixels of the supplied artwork, so
  the blue "P" of EXPRESS vanished (v14 reads "PC EX RESS"), the italic X collapsed into a
  starburst and the slanted red-over-blue field was replaced by a blue rectangle with a red bar.
  It now segments all three brand colours (`#FFFFFF`, `#D22630`, `#003DA5`) off one quantised
  image and extrudes **five stacked plates**: the parallelogram panel, the red field band, the
  white keyline around the monogram, the red PC outline with its counters, and the white letter
  faces. 5,272 vertices, 18 contours, 48 mm of relief, registered mark omitted. Three faults
  were found and fixed by capture on the way: the ® sat INSIDE the panel so forcing its corner
  to red before the panel was solved grew a square red horn (v15); the dark metal return was a
  SOLID box pushed 0.19 m proud, which buried the whole mark behind a grey slab (v16), and is
  four perimeter rails now; and the plate emission at 0.30 to 0.52 Ke under a 1.5-intensity sign
  light clipped the whites to paper and washed the field pink, so both came down by more than
  half. Verified in `ilalim_pcexpress_close_v22.png`, `ilalim_pcexpress_v22.png` and
  `ilalim_thrower_view_v22.png`, where it reads from the throwing line.

  **Every sign on the strip was the same sign.** § 9.2 was ticked off with six different
  STRINGS on six near-identical wall-flush rectangles. `StreetSignKit` now holds **eleven sign
  systems** (lightbox, framed fascia, projecting blade, ground A-board, enamel placard, lashed
  tarpaulin, pole pylon, hung panel, painted wall, vertical banner, corrugated tin sheet) and
  one `LetterStyle` (aspect, weight, tracking, slant, relief) applied to the ONE 5-by-7 glyph
  table this repo is allowed to have. Thirteen businesses, and no two neighbours share a
  silhouette. Two real bugs were found by capture: glyph aspect was computed in the plate's
  normalised space, so on the 1.80 by 0.92 m tin sheet a 0.78 ratio became 1.53 in world and the
  strokes merged into slabs; and the tin sheet's corrugation ribs were 13 mm PROUD of the board,
  interleaving through every letter. Verified in `ilalim_pavement_west_v22.png` and
  `ilalim_pavement_east_v22.png`, where GOMA and XEROX PRINT read cleanly.

  **The background was underfilled and visibly repeated.** Renderers went from **1,242 to
  2,314**. The near rows carry per-instance scale (4.40 to 6.20 instead of a flat 5.0) and
  setbacks of 0.00 to 1.10 m, and the two sides run different sequences so the street is not
  mirrored. Added: roof tanks, chimneys, aircon plant and aerials on every near shophouse, five
  swaying `Sampay` washing lines, a twelve-building second shop row, yard fencing, back-lot
  stacks, a pipe run on trestles, a crane, a hopper, six hoardings on masts, district lamp rows,
  cross-street traffic and a four-piece stabled consist. Verified in
  `ilalim_depth_overview_v22.png` and `ilalim_background_north_v22.png`.

  **And the far ground plate was being painted as a BUILDING.** It was named `MalayoX_Ground`
  under `Malayo`, and `EnvColourPass.IsBuilding` matches any `MalayoX_` instance, so the pass
  gave the 240 m ground a facade tint and mapped a corrugated ROOF atlas across it. That is why
  every gap in the district read warm pink. It sits in its own `Lupa` group now.

  **Two grounding bugs the gate caught that no render would have.** `Renderer.bounds` is a
  cached world AABB and had not taken the position written one line earlier, so solving a ground
  offset from it read the model's LOCAL underside as a world height: `TryVisibleBounds` pushes
  the mesh bounds through `localToWorldMatrix` instead, which cannot be stale. And
  `AirborneByDesign` on a whole vehicle was hiding the resulting float behind an excuse whose own
  text named the number; `ExcuseSuperstructure` excuses the body and leaves the WHEELS gated, so
  the solve stays verifiable. The boundary cars now touch the road.

  Verified: `MapGeometryCheck` **0 findings** on the gated map with 12 joined bays, 8
  pillar-to-soffit joins, 2 track-to-deck joins, train on rail, 26 wire spans and 28 grounded
  poles; box clear; floor solid across x +/-11.2, z +/-16.7; Core 60/60, EditMode 105/105,
  PlayMode 55/55, `HeadlessCheck` OK, `ArenaCheck` OK, `AudioCueCheck` OK. Renders v15 to v22 in
  `Logs/shots-ilalim/`. The plan they were built against is `docs/Ilalim_Ng_Tulay.md` § 10.

- **A trip put you on the floor and gave you nothing to do about it.** ✅ 2026-08-25.
  🧑: *"like maybe places u can trip on? then fall down animation plays and u have to spam a
  button to get back up"*. The knockdown already shipped (`CharacterAnimator` plays `die` while
  `TripLeft > 0.70` and `pick-up` under it, both non-looping) but nothing could shorten it.
  `Combat.MashRecover` takes `Balance.MashRecoverPerPress` (0.13 s) off per press, rate-capped by
  `Balance.MashCooldown` (0.10 s, so 10 Hz) and floored at `Balance.MinTripDown` (0.90 s), which
  leaves 0.20 s of knockdown before the get-up begins. `CharacterMotor.MashRecover` takes the
  STUN down with the trip, without which the player mashes free and then stands frozen for the
  rest of the original 2.5 s. Bound to Jump contextually, so no binding was added; the AI toggles
  the same verb so a bot is held to the same 10 Hz ceiling by the same function; the HUD prompt
  reads the live binding. Two new trip sites inside the chalk, both on road detail that explains
  them (the loose lid at -4.60, 2.40 and the sunken trench at 4.60, -2.60), both clearing the can
  by over 5 m against a 1.40 m minimum. Four Core tests assert the bound rather than the feel:
  a mash cannot cancel a trip, presses inside the cap do nothing, the saving fits inside the fall
  (12.3 presses over 1.23 s of a 2.50 s trip), and the floor leaves the knockdown clip time to
  play.

- **The three measured art faults under the LRT guideway.** ✅ 2026-08-24. The 6.88 m custom
  deck, its unsupported third rails and the wrong-gauge custom train were replaced together by
  a 10.5 m `roads/road-bridge` guideway, two `train/track-detailed` lines and a three-piece
  `train-electric-city` consist. The cargo tricycle now has a stem from its y = 0.93 frame to
  its y = 1.025 handlebar and a second join across the 0.15 m handlebar-to-grip gap. The
  basketball rim's separate 0.23 m gap to its backboard was found in the same island audit and
  received a bracket too. Verified from the v9 guideway and hoop captures, the committed island
  checker, and gated `MapGeometryCheck` including the elevated-assembly joins.

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

- **Character-Specific Viewmodel Arms for All Heroes and Classic Characters.** ✅ 2026-08-23.
  First-person viewmodel arms dynamically match each character's TPP model, palette skin tone,
  sleeve cuts, arm markings/tattoos, watches, wristbands, and elemental accessories across all 5
  Heroes (Sean, Zack, Dante, Cheska, Nemu) and all 12 Classic characters (Berto/bayan, Maring,
  Totoy, Inday, Kuya Boy, Ate Girlie, Tikboy, Bebang, Jun-Jun, Lola Pacing, Mang Kanor, Aling
  Nena). Held slipper parenting and all 15 bespoke hero ability animations preserved. Verified
  by 100 EditMode tests, 55 PlayMode tests, and 56 Core tests.

- **Map "Ilalim ng Tulay" (LRT Gilmore Strip).** ✅ 2026-08-24, rebuilt from the cross section
  out. It shipped on 2026-08-24 and every one of the faults below was in that build; the map
  now has a design document, `docs/Ilalim_Ng_Tulay.md`, and a check that refuses it,
  `MapGeometryCheck`.

  **The geometry was wrong in ways four signed-off renders did not show.** `MapGeometryCheck`
  measured **62 findings** on the shipped scene and **0** on this one:
  - Both pavements floated **0.15 m over open air**: 40 plaza tiles with nothing built under
    them. Five buildings, a pole, the pares cart and the PC Express storefront stood on nothing
    at all, 1.5 m past where the ground stopped.
  - Every prop on either pavement was **sunk 0.062 m into it**, because the placement height was
    the plaza tile's ORIGIN and not its TOP. Everything is placed through `SurfaceTop(x)` now and
    the builder holds no typed-in heights.
  - All 50 kerb tiles were laid **across** the carriageway instead of along it, because
    `env_kerb_tile` is 2.0 m on local X and 0.35 m on local Z and the street runs along Z. Those
    are the loose pale slabs strewn over the road in `ilalim_thrower_view.png`.
  - The map **ended in white sky** in every direction a metre past the pavement. There is a
    240 m ground plate now. The road and supported pavements continue to the fog limit, with
    car-kit traffic, background intersections, corner shops and a lower mid-rise belt instead
    of the later cross rows that made the carriageway look built against a wall.
  - All four **utility poles were yawed the wrong way**, hanging their 6.6 m wire spans out over
    the back lots instead of over the street.
  - The collision floor was one flat plane at y = 0 while the pavement was drawn 0.212 m up, so
    every body walked through both pavements to the shin.

  **The rules geometry was wrong, and that mattered more.**
  - There was **no chalk box at all**. It drew a "throwing line" at z = 3.0 and a "base circle"
    at z = 13.5, neither derived from anything, while the can spawns at the world origin: the
    circle was **13.5 m from the can it was drawn for**. All of it comes from
    `Balance.ConfinementRadius` and `Confinement` now.
  - Two 3.4 m **viaduct columns stood inside the box** at z = -5.0. The taya is clamped in there
    and cannot step out to walk around one. Both live rows are outside |z| = 7 now; 1.4 m kit
    pillars at x = +/-4.45 leave a measured 7.5 m centre gap and 1.85 m kerb gaps.
  - A **trip hazard was centred on the world origin**, which is where the can spawns and where
    every retrieval in the match converges.
  - The **overclock pad was inside the PC Express collider** and could not be reached.
  - The PC Express collider itself reached **2.1 m into the carriageway**.

  **It did not look like the same game, and the cause was one word.**
  `EnvColourPass.DressingRoot()` looks for a child named exactly `Dressing`; the map put
  everything under `Geometry`, so the pass walked nothing and repainted nothing while both other
  maps were getting the seeded Manila palette, the roof atlases, the road correction and the
  belt fade. The map was not using a different palette, it was using no palette. Groups are
  named `Kalsada`, `Slab` and `Malayo` now. The hand-built near blocks are gone; commercial,
  industrial, roads, train, factory and car kits use complete generated warm atlas replacements
  so their orange and blue source swatches never become role-colour decoration. The showcase
  probe also runs `EnvColourPass.Apply()` before rendering, without which an edit-mode capture
  shows raw materials.

  **PC Express is the shop it is named after.** It shipped as a green lightbox with two blank
  white slabs and a green-white-red awning. `PcExpressSignAuthor` now builds the deep red-blue
  lightbox and metal return from the supplied real exterior. The official horizontal artwork
  is traced by `build_pc_express_logo_mesh.py` into smooth raised white letters with the real PC
  monogram, italic X and one clean blue channel return. The registered-mark badge is omitted
  because it is not mounted on the real storefront. Glass mullions, centre doors, kick plate and
  a slim overhang replace the market awning. Both authoring tools are idempotent.

  **New for Hero Strike**, all of it argued in `docs/Ilalim_Ng_Tulay.md` § 3:
  - The chalk box IS the carriageway, so a player reads the danger zone off the kerb line.
  - **4.2 m of legal standing room** outside the box on each long side against Eskinita's 1.6 m,
    which is the measured reason 🧑 reported both existing maps as *"weird to play abilities
    gamemode on"*. `ArenaCheck` bound 3 now clears its wall by 2.6 m instead of by 0.0.
  - The **train pass is a mechanic**: `OverheadPassWindow` doubles ability COOLDOWN rate (only
    the cooldown, never the ultimate charge) for the measured 2.70 s the consist is overhead,
    every 24 s.
    Classic gets Street Hype and the spectacle instead, per `VISION.md` § 1.1.
  - The **bridge hoop**: a tsinelas through the ring fires "TRES!" and Street Hype, and awards
    no score, because `MatchDirector.AddScore` stays the only place a point is made.
  - "BAWAL UMIHI DITO" on the column faces, two potholes off the spawn-to-can line, and clutter
    that is all on the pavements.

  **The final composition pass removed the set edge.** The road and pavements now reach the
  120 m plate and disappear into fog; 26 joined wire spans sit on 28 single shopfront-edge
  posts; the side gaps contain dense second and outer building rows, industrial tanks and far
  intersection blocks; the pisonet, PC-repair, pares, regulatory and civic signs each use a
  different silhouette and mounting. Boundary cars remain wholly outside |z| = 16.5.

  Verified by 56 core tests, 105 EditMode, 55 PlayMode, `HeadlessCheck`, `ArenaCheck`,
  `AudioCueCheck`, `MapGeometryCheck` at 0 findings including every elevated join, eight v14
  in-engine renders in `Logs/shots-ilalim/`, idempotent palette/logo/sign generators, the model
  island checker, and a clean Windows player build smoke-launched from the Desktop.
