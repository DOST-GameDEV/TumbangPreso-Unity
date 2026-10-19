# In-game HUD in Overwatch's simple shape, 2026-10-20

Request: "overhaul the ingame ui. make it similar to overwatch's simple ingame hud."
TODO entry: HUD-OVERWATCH-1020. Method: [UI design method, glass HUD](../../UI_DESIGN_METHOD.md#match-hud-the-glass-hud-overwatchs-simple-shape-2026-10-20).

## What Overwatch's match HUD is (research)

Sourced facts, with links below; positions are estimates checked against the in-repo
first-hand note in [visual-research-2026-09-23](../visual-research-2026-09-23/findings.md)
(lines 354 to 361), because the screenshot databases refused automated access.

- The screen is split by whose information it is: you (portrait, health) bottom left, your
  kit (abilities with a key under each) bottom right, your ultimate (a ring with the
  percentage inside) bottom centre, the match (objective, timer, both teams' hero icons) top
  centre, the kill feed top right. The centre holds the crosshair and sub-second confirmations.
- Health is drawn in segments of 25. One lean runs through the whole family: the HUD typeface
  (Big Noodle Too Oblique) is a 15 degree oblique and frames and segments lean with it.
- White on the world, translucent dark plates only behind dense rows. Blizzard's stated
  direction for the OW2 scoreboard was more transparency, to see more of the game.
- Colour is spent on meaning: one friendly/enemy pair (remappable for colour blindness), red for
  danger, and a ready ultimate is the one glowing element.
- The February 2026 rebrand changed menus; no in-match HUD change was found.

Sources: [OW2 beta patch notes](https://overwatch.blizzard.com/en-us/news/patch-notes/beta/2022/06),
[hit points](https://overwatch.fandom.com/wiki/Hit_points),
[ultimate meter](https://overwatch.fandom.com/wiki/Ultimate_ability#Ultimate_meter),
[layout overview](https://bushy2.itch.io/a1-skill-development/devlog/812196/other-games-uis),
[Big Noodle Too](https://myfonts.com/fonts/sentinel/big-noodle-too),
[colour-blind remap](https://www.slashgear.com/overwatch-gets-updated-colorblind-feature-with-nine-color-options-22546973),
[2026 menu overhaul](https://press-start.com.au/news/2026/02/05/overwatch-spotlight-2026/).

## What changed, element by element

| Element | Before (Toy Block, 2026-10-06) | Now |
|---|---|---|
| Surface | Cream or brown tile, bevelled corners, extruded side, sheen | `HudCard.Glass`: flat warm smoked glass at 55 per cent, cream hairline, short shadow, leaning by `HudDraw.Lean` |
| Team bar | Four 250 x 68 cream cards, 1306 wide, "You" tile under one | Four 200 x 58 glass chips, 1060 wide; portrait on a leaning seat tile, italic ivory score, can or slipper glyph; your chip has a honey foot bar and honey score; names only for spectators |
| Clock | Brown tile, octagon beads in a tray | Denser glass, ivory Darumadrop (kept by `FONT_USAGE.md`), leaning round ticks in a glass tray |
| Vitals (new) | Two mostly hidden sentences; stamina only as the reticle arc | Bottom-left plate at the deck's 125 per cent: portrait on a honey tile with a seat strip, role glyph, ten leaning stamina segments of 25 (`Balance.StaminaMax` is 250). Keyboard and pad only |
| Stamina arc | Shown whenever stamina was below full | Shown only for fatigue, DRAINED and the wring while the vitals plate is drawn; unchanged on touch |
| Statuses | Cream tabs, octagon medallion, from the screen's bottom edge | Glass tabs and round glass medallion, stacked above the vitals plate |
| Skills | Two dark discs bottom right with a keycap badge on the corner | Two leaning glass tiles, a 32-unit key cap centred under each, cooldown shade draining from the top, large italic seconds, honey edge and foot level while active |
| Ultimate | A third, notched disc in the same row | Its own group, `UltimateSeal`, at the bottom centre: ten notches lighting ivory, percentage inside, solid honey ring with a halo when ready. Touch: one centred row, skill, ultimate, skill |
| Action prompt | Cream tile, brown words, orange groove | Dense glass strip, ivory words, honey progress inside it |
| Announcement | Cream plaque, brown words | Glass strip, large italic ivory words |
| Refusal | Red toy tile | The same red, fully opaque, as a flat leaning panel |
| Score feed | Cream rows under the old bar | Glass rows, italic ivory points; top right corner (30 units down) on keyboard and pad, unchanged height on touch |
| Earned moment | Red and gold toy tiles with wings, sunburst, confetti | Same tiers and ceremony on flat leaning panels, italic title |

Kept on purpose: every group's object name (the reading layout, the contrast pass and the probes
address groups by name); `HudReadingLayout`, `HudContrast` and the ultimate recede (the new
`UltimateSeal` is registered in all three); the black in-match outline; the reticle and hit mark;
the held power reference sheet; Phaister's DRAINED pins on the stamina arc.

## Evidence

Local native PlayMode, Unity 6000.5.8f1, Windows, this worktree. Not a phone, not a pad, not peers.

| Run | Source | Result |
|---|---|---|
| [native-full-set.xml](native-full-set.xml) | this change before the final trim (210 chips, 96 ring) | 33 cases: 25 passed, 8 failed |
| [baseline-qolupdates.xml](baseline-qolupdates.xml) | pure QoLUpdates `8c4164b2a`, the same failing cases | 10 cases: 2 passed, 8 failed |
| [native-final.xml](native-final.xml) | the committed source | 28 cases (TumpNativeHudTests, OwnerSkillHudReferenceTests, TimedPowerUiTests): 23 passed, 5 failed |

Every failure in the final and full-set runs also fails on the baseline, with the same message:
stale expectations the QoLUpdates content has outgrown (QOL-STALE-PLAYMODE-1019 family: a
"CANNOT THROW" sentence the warning code no longer produces, a 64-unit ready glyph the code places
at 58, a stamina fill computed for an older `StaminaMax`, a Darumadrop moment title after the
October 6 DIN override, a familiar projection and a scoreboard name lookup). One case changes
shape: `HeroPowerDetailsAndSpectatorCleanFeedKeepTheirLiveContracts` failed on the baseline at its
HUD-area budget (10.8 per cent against 8) and now passes it (7.77) and stops later, on the held
power reference's description text, a sheet this change does not touch.

Two failures were this change's and are fixed: the skill tile's lean reached 0.8 units outside its
slot on a pixel-perfect canvas (now 1.5 units of clearance), and the first trim of the bar still
measured 8.0006 per cent (chips are 200, the ring 92). `RetrieveAndResetUseTheirLiveGlyphsAndKeepRealActions`
failed once on its synthetic Grab timing and passed on the baseline and on the next run; it does not
touch HUD drawing.

HUD area, ordinary play at 1920 x 1080 (`TumpUiCapture.HudShare`): Classic 5.24 per cent, Hero Strike
7.77, both under the VISUAL-1.4 budget of 8.

Renders (the touch frames come from the full-set run, before the last 10-unit trim of the chips):

- [keyboard-hero-strike-1600x680.jpg](keyboard-hero-strike-1600x680.jpg) and
  [keyboard-hero-strike-960x540.jpg](keyboard-hero-strike-960x540.jpg): the whole layout on
  keyboard and mouse, vitals bottom left, skills bottom right, ultimate bottom centre.
- [classic-1920x1080.jpg](classic-1920x1080.jpg): Classic, a `-100000` score in its chip.
- [statuses-dense-1600x680.jpg](statuses-dense-1600x680.jpg): ten live statuses, the refusal and
  the action.
- [contrast-hud120-1920x1080.jpg](contrast-hud120-1920x1080.jpg): High contrast at HUD 120 per cent.
- [touch-street-crowded-1920x1080.jpg](touch-street-crowded-1920x1080.jpg) and
  [touch-arena-quiet-1920x1080.jpg](touch-arena-quiet-1920x1080.jpg): the touch layout (this
  machine reports a touchscreen, so unforced fixtures run as touch) with the touch controls drawn.

Not established here: the owner's verdict in play, a physical phone or pad, and whether the
bottom-centre ultimate stays clear of every hero's held-slipper pose on every map.
