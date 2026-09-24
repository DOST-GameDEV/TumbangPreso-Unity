# Every hub screen is its own place, 2026-09-24

The owner: *"after u finish with this check out and refine the look of all other ui"*, *"try to
refine them and give them all more personality"*, then *"each screen shoudl feel like their own
screen"*. TODO entry: `docs/TODO.md` UX-1.18. Code: `Runtime/UI/Hub/HubScenery.cs` and the
screens that call it.

## 1 · What was wrong

`before/` holds the captures of every hub screen at `6cc04ebb6`, taken by `HubFlowTests` before
any change. The 2026-09-23 pass had already fixed the buttons, the palette and the icons, so the
controls were good. What they sat on was not:

- **One ground for every screen.** HERO, LOADOUT, SKILL TREE, TASKS, GAMEMODE and JOIN each
  began with the same `HubPattern.Ground(Root, HubStyle.Maroon, seed)`: a maroon fill with the
  same scattered chalk marks. Only the seed differed.
- **One header for every screen**: BACK, a Darumadrop title with an ink shadow, the wallet and
  the menu.
- **One container for every group**: a dark rounded plate. TASKS was six of them in a grid and
  read as a spreadsheet; JOIN's empty list was a sentence in a large dark box.
- **The one big moment, MATCH FOUND, was a flat yellow band** with a word on it.

That is `CLAUDE.md` § 6.5's diagnosis from 2026-09-01, repeated: *"everything feels repetitive
bcz i think u use the same code to generate them all"*, and the owner's instruction from the same
day: *"make sure all ui isnt generated in the same way but follows a central theme"*.

## 2 · Research

Pulled from the Interface In Game galleries (Splatoon 2, Hi-Fi Rush, Slay the Spire 2, Sifu,
Brawl Stars, Fall Guys) and the Slay the Spire 2 menu rig decoded for the title pass
(`docs/reports/title-weather-2026-09-24`). What gives those menus personality:

| Game | The device |
|---|---|
| Slay the Spire 2 | Every menu is a physical object: the map is parchment, the modes are carved boards, the character screen is a painting in one mood colour. |
| Splatoon | Menus are signage in the city: level select is a billboard, tabs are tilted stickers, a shopkeeper talks to you. |
| Hi-Fi Rush | Slanted comic slabs, torn edges, art bursting out of its frame, a character presenting each store. |
| Brawl Stars | A slanted name ribbon over a character who overlaps the panel, rays behind the pick. |
| Fall Guys | Radial bursts, a pedestal, bouncy type for the big beats. |

The shared lesson: **no two screens are the same box.** Each is a place or an object from the
game's world, with the same line and colour language holding them together.

## 3 · The design: one street, a different place per screen

Tumbang preso is played on a Filipino street, so the places come from that street:

| Screen | Place | Signature |
|---|---|---|
| SKILL TREE | chalk on the asphalt | warm asphalt with its aggregate and a faded old court; branches chalk themselves in from the hero; chalked title and ring |
| GAMEMODE | posters on a yero wall | corrugated sheet with rust runs; each poster taped on, crooked |
| JOIN | a hollow-block wall | blocks in running bond; panels taped up like notices; an empty list is a lone lata in its chalk circle |
| TASKS | the listahan | a cardboard sign taped to a chipped wall; marker headings and lines; a finished task highlighted, a claimed one ticked in red |
| LOADOUT, SHOP | the sari-sari store | plank front under a striped awning, a shelf under every row; the SHOP popup under the same awning; an item inspected on the counter under the bulb |
| HERO | a collector's poster | rays turning slowly behind the figure, a halftone-printed ground, the role on a slanted red tag |
| HOST | the organiser's clipboard | hardboard with its clip, held up over the court |
| CHARACTER SELECT, MAP VOTE | the court at night | one warm light, a chalk circle under the hero; the two share it because they are one walk onto the street |
| LOBBY, MATCH FOUND | the fiesta | bunting on the title street's breeze (`OwnerMenuWind`); MATCH FOUND bursts, slams too big, jolts the screen once, and the bunting drops in |

Titles follow the place: **sprayed on walls** (ink outline, persimmon overspray, drips, the same
act as the TUMP graffiti on the title screen) and **chalked on roads**.

What stays common is what makes it one game: the ink line, the six logo colours, the pressable
sticker, and the maroon evening family chosen on 2026-09-23 so HOME's sunset and every pushed
screen are the same evening. Every new colour is derived in `HubScenery` with its arithmetic
(asphalt, kraft, plank); none has more blue than red.

## 4 · Rules kept

- **Nothing a player reads is drawn on a texture.** Every texture is low contrast against its
  own ground; lettering stays on stickers or on its own clear ground. The 28-unit type floor is
  untouched and `HubFlowTests.AssertFloor` still runs on every screen.
- **Reduced UI motion holds everything still**: rays, bunting, chalk draw-in, the slam, the jolt.
- **No pale default ground.** The one light surface, the cardboard, is an object on a wall,
  mid-tone kraft rather than white, with ink at about 7 : 1.
- **Untouched on purpose:** HOME (the owner's animated loops), the title and the login (her art),
  LOADING (painted art), the settings workspace (reworked 2026-09-23) and the in-match HUD.

## 5 · Evidence

`before/` holds contact sheets of every hub screen at `6cc04ebb6` (1280x720 captures from
`HubFlowTests`); `after/` holds the same screens after this pass. The final run covered every
fixture that touches these screens and the title:

| Fixture | Result |
|---|---|
| `HubFlowTests` (every door, popups, queue, lobby, loading, high contrast and larger text) | 4 / 4 |
| `MatchArrivalFlowTests` (character select, map vote, arrival) | 4 / 4 |
| `OwnerMenuSkyTests`, `OwnerMenuEditsTests`, `HomeFlowTests` | 8 / 8 |

⚠️ **ONE FAULT FIXED ON THE WAY THAT PREDATES THIS PASS.**
`MatchArrivalFlowTests.CharacterSelectionExplainsItsKitWithoutPausingTheClock` was red in a
result file from 2026-09-23 23:53: with Larger text on, the inspected ability's name wrapped to
two lines (111 units) inside a box stacked for one (52). The stack is measured once per
inspection, so a setting changed while the screen is open outgrew it. `HubCharacterSelect.Tick`
now re-stacks whenever the name no longer fits.

## 6 · Iterations, for the next person

- **v1**: the TASKS cardboard covered the offline note under it; the spray drips on TASKS and
  GAMEMODE ran into their subtitles; JOIN's empty list was still a sentence in a box.
- **v2**: board 720 tall with 196-unit rows; drips capped at 16 units when a subtitle exists;
  the lone lata; HOST, the item popup and MAP VOTE given places.
- **v3**: the counter plank moved under the item's base instead of across its middle.
- Bunting was removed from CHARACTER SELECT before any capture, because its rects put the flags
  straight through the hero's name, and hung 200 units down in LOBBY to clear the title, map
  line and address.
