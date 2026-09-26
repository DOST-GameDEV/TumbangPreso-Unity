# Current hero skill icon audit, 2026-09-27

## Source and scope

`HeroKit.ScreenSlots` presents signature, attacking, defending and ultimate for all nine current kits (`HeroKit.cs:114-128`): 36 displayed slots, consisting of 33 implemented powers and three `PlaceholderRoleAbility` defending slots for Sean, Zack and Rafi. Before this change, all 33 implemented powers resolved to a PNG at `Resources/UI/ability-icons/<glyph>.png`; distinct PNG filenames had distinct SHA-256 hashes. Four pairs selected the **same glyph and same file**. The three placeholders also borrowed their hero's attacking icon. The rendered screens use each ability's `Glyph`; `HeroGlyphs` is a legacy ID lookup and is not the current screen source.

| Hero | Slot / ability | Before glyph -> drawn PNG | Change |
| --- | --- | --- | --- |
| Amihan | Signature QUICK DASH | AmihanQuickDash | retain |
| Amihan | Attack UPDRAFT | AmihanUpdraft | retain |
| Amihan | Defend WHIRLWIND | AmihanWhirlwind | retain |
| Amihan | Ultimate STORM SURGE | AmihanStormSurge | retain |
| Cheska | Signature COLD FEET | CheskaFrostSheet | retain |
| Cheska | Attack FROSTBITE | CheskaNova | **replace:** iced slipper, charged throw; currently repeats ultimate snowflake |
| Cheska | Defend GLACIAL WALL | CheskaBarricade | retain |
| Cheska | Ultimate ABSOLUTE ZERO | CheskaNova | retain |
| Dante | Signature SHIELD | DanteShield | retain |
| Dante | Attack BOULDER | DanteStomp | **replace:** airborne rock, flight trail and impact; current boot implies a stomp |
| Dante | Defend BARRIER | DanteShield | **replace:** upright force field reflecting a slipper; currently repeats signature crest |
| Dante | Ultimate EARTHQUAKE | DanteFissure | retain |
| Nemu | Signature TERRIFY | NemuSeanceVoid | **replace:** Kuro at a marked haunt spot, startled slipper; currently repeats ultimate vortex |
| Nemu | Attack KURO FETCH | NemuAstralPet | retain |
| Nemu | Defend KURO GUARD | NemuPhase | **replace:** enlarged Kuro intercepting a slipper at the can; current ghost-in-motion reads old evasion |
| Nemu | Ultimate DEVOURING SEANCE | NemuSeanceVoid | retain; current ultimate remains until KURO PLAYS is implemented |
| Paete | Signature LIANA LEAP | PaeteVine | retain |
| Paete | Attack BAKYA BLOOM | PaeteSprout | retain |
| Paete | Defend THORN HARVEST | PaeteThorn | retain |
| Paete | Ultimate MAKILING'S EMBRACE | PaeteSentry | retain |
| Phaister | Signature SHADOW BLINK | PhaisterShadowBlink | retain |
| Phaister | Attack CURSE: DISORIENTED | PhaisterHexSigil | **replace:** thrown cursed doll, disorientation accent; current hex circle reads old ward |
| Phaister | Defend CURSE: VULNERABLE | PhaisterEclipse | **replace:** pin through doll, forward cone; currently repeats ultimate eclipse |
| Phaister | Ultimate HIGOP | PhaisterEclipse | retain for now, though a black-hole redraw may better match the current verb after the new seven are reviewed |
| Rafi | Signature CROSSCURRENT | RafiCrosscurrent | retain |
| Rafi | Attack MIRRORWAKE | RafiMirrorwake | retain |
| Rafi | Defend COMING SOON | RafiMirrorwake | neutral unavailable symbol |
| Rafi | Ultimate BREAKWATER | RafiBreakwater | retain |
| Sean | Signature FLAME RUSH | SeanRush | retain |
| Sean | Attack IGNITION CANNON | SeanIgnite | retain |
| Sean | Defend COMING SOON | SeanIgnite | neutral unavailable symbol |
| Sean | Ultimate SUPERNOVA | SeanSupernova | retain |
| Zack | Signature BOLT SPRINT | ZackSprint | retain |
| Zack | Attack MAGNET | ZackOvercharge | retain; the orb still conveys a magnet, per its source note |
| Zack | Defend COMING SOON | ZackOvercharge | neutral unavailable symbol |
| Zack | Ultimate THUNDERSTRIKE | ZackThunderstrike | retain |

## Additional presentation failures

- `HeroPresentationTests.EveryAbilityAcrossAllHeroesHasAUniqueBespokeGlyph` is right to require unique **implemented powers** but expected 29 from the older cast. The initial correction miscounted `ScreenSlots` as 33; source review found all nine kits have four slots. The corrected test examines all 36, requires 33 distinct real glyphs, and separately requires precisely Sean/Zack/Rafi's defending placeholders to say COMING SOON and use `AbilityGlyph.ComingSoon`. That appended enum value draws a neutral ring-minus; it does not imply an implemented effect. Native `TumpAbilitySymbol` integration is part of the reviewed UI batch.
- The 125-character inspect-card bound was exceeded by Phaister CURSE: DISORIENTED (134), CURSE: VULNERABLE (156), HIGOP (152), and Paete MAKILING'S EMBRACE (160). Their descriptions are now shorter while retaining gameplay facts; Paete's 71-character summary is also shortened to fit its 62-character bound.
- `HeroPresentationTests.TelegraphsMatchWhatTheAbilityPlaces` expected Cheska's former ring at slot 2. Current FROSTBITE has no aim/ground telegraph: it grants `IsFrostbiteLoaded` to the next slipper for 10 s (`CheskaHeroKit.cs:74-105`), and the freeze resolves on a slipper hit (`Slipper.cs:1756`). The current check asserts no Frostbite ring, preserves COLD FEET's 2.3 m field and GLACIAL WALL's 4.2 m arc, and updates the other reworked kits' literal geometry checks to their present abilities. The test has not been run in Unity in this lane.
- The legacy `HeroGlyphs` ID table still maps old roles and lacks current role IDs. Align it with kit assignments for any indirect caller, while screen drawing continues to read the actual ability object.

## Verification and status

Seven new drawings were authored individually in `tools/build_ability_icons.py`, exported as 256 px RGBA sprites with matching uncompressed sprite import metadata, and inspected on the builder's dark HUD, honey tile and maroon screen grounds at 128/72/44 px. [Contact sheet](skill-icons-contact.png) shows all seven; the original correctly assigned PNGs were preserved. The original static asset checks found 33 matching real-power ID mappings, 46 unique PNG hashes and metadata GUIDs, bounded summary/description lengths, and valid builder syntax. Its displayed-slot count omitted the placeholders and was corrected during integration. `TouchControls.RefreshIcon` and `Hud.PaintGlyph` have their own change checks; those checks do not prove that the native `TumpAbilitySymbol` material refreshes. That separate native cache path is being corrected and tested in the UI batch. No native result is claimed here before the coordinated run.

## Implementation state

Saved edits: `DanteHeroKit.cs`, `CheskaHeroKit.cs`, `NemuHeroKit.cs`, `PhaisterHeroKit.cs`, `PaeteHeroKit.cs` (glyphs and short copy only); `AbilityIcons.cs` (seven real-power enum values plus ComingSoon, job labels and fallback shapes); `PlaceholderRoleAbility.cs` (neutral unavailable glyph); `HeroGlyphs.cs` (33 current ID mappings); `HeroPresentationTests.cs` (33 implemented-power uniqueness, three explicit placeholders, current rework telegraphs); `tools/build_ability_icons.py` (seven typed icon drawings). Generated assets: seven named PNGs and seven matching `.png.meta` files in `Resources/UI/ability-icons/`, plus `skill-icons-contact.png` here.

Remaining verification: the contact sheet was reviewed during integration; run the affected EditMode tests in Unity and inspect character select, hold-to-inspect, live HUD and touch hero/role switches in a native player. Stale comments claiming Phaister's second slot is Blink and his ultimate is Eclipse were removed. The current `PhaisterEclipse` drawing still depicts an eclipse although HIGOP now creates a black hole; it no longer duplicates another current slot after the new Vulnerable icon, but a dedicated HIGOP drawing is a separate visual refinement. No native visual or runtime pass is claimed.
