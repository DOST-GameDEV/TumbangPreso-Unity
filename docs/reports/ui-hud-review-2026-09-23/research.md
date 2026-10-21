# UI and HUD research notes (2026-09-23)

What was actually looked at, what it showed, and what transfers to TUMP. Screenshots were
inspected directly (Game UI Database uploads, official itch.io images) rather than described
from memory. Third-party screenshots are cited, not committed.

## Sources inspected

| Source | What was inspected |
|---|---|
| Nintendo designers at UI Crunch #13 (translated notes, https://medium.com/haiiro-io/how-nintendo-designed-switch-and-splatoon-d1a14b9cc2de) | Splatoon typeface brief, colour method, audience rule |
| Inkipedia "Splatoon 3 UX" critique (https://splatoonwiki.org/wiki/User:Slate/Splatoon_3_UX) | Player-reported menu and lobby failures |
| Hi-Fi RUSH on Game UI Database (https://www.gameuidatabase.com/gameData.php?id=1678) | Options (all four tabs), title, main menu, save slots |
| Overwatch on Game UI Database (id 1341) | Hero select (two states), mode cards, role select, results, career |
| Overwatch UI designer portfolio (https://www.baileykalesti.com/overwatch) | Hero select and listbox changes, stated reasons |
| Valorant on Game UI Database (id 1043) | Agent select, agent gallery, settings, HUD, loading |
| Xbox Accessibility Guideline 101 (https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101) | Text size, glyph size, spacing and case numbers |
| League of Legends VFX style guide (Riot, 2017) | Primary/secondary shape, focal point, noise |
| Sepak U (https://teamgoodknight.itch.io/sepak-u) | Already analysed in `docs/reports/visual-research-2026-09-23/findings.md` section 2; reused, not repeated |

## Findings, each with its TUMP consequence

### 1. Focus must be LOUD, and it is the row, not the control (Hi-Fi RUSH options)
Seen: the focused settings row is filled solid chartreuse-yellow with dark lettering and a
white pointer arrow at its left edge. Every other row is a dark slab. The value cell keeps
its own frame with left/right arrows either side of the value.
TUMP: the previous session's settings focus band is accent at 9 per cent alpha, which is
barely visible on the warm grey. Raise it to a readable band plus a solid accent bar, and
turn the focused row's LABEL into the accent colour, so focus survives a glance from a couch.
Splatoon 3's critique names the opposite failure: "the grey contrast of the menu option of
where you currently are should be clearer; it's too similar to the clickable options".

### 2. Group headers are coloured slabs, not more text (Hi-Fi RUSH options)
Seen: each group ("Volume Settings", "Camera Settings") is a thin red slanted strip with small
caps inside it, full width of the list. Rows under it are individual dark plates with a gap.
TUMP: group headers are right, but plain accent text reads as another row label. Give each a
short accent rule underneath so the eye jumps header to header.

### 3. One fixed place explains the focused thing (Hi-Fi RUSH, Valorant)
Seen: Hi-Fi RUSH shows the focused option's name and one sentence in a fixed right column,
with a live preview picture for subtitles. Valorant shows the focused ability's sentence in a
fixed block under the tab row.
TUMP: settings notes are inline paragraphs between rows. Keep the genuinely non-obvious ones
(telemetry, restore toggle, reduced effects scope, unrecognised pads); do not add tooltips.
Character select already has the fixed-block pattern and keeps it.

### 4. The select clock is a NUMBER, prominent and near the decision (Overwatch, Valorant)
Seen: Valorant draws a large light "62" top left above the team column. Overwatch writes
"ASSEMBLE YOUR TEAM: 19" with the number in the accent, centred over the lineup.
Correction to the earlier plan: it proposed a draining ring citing these two games, and
neither uses one. TUMP keeps the number, gives it a plate and a label, and lets it change
colour in the last five seconds (a state change a player can feel without reading).

### 5. Ability icons are ONE flat family of solid silhouettes (Valorant, LoL)
Seen: Valorant's C/Q/E/X icons are single-colour filled glyphs of one visual weight, each a
plain silhouette, key letter above; the focused one's sentence sits in one block. League's
guide: a clear primary shape and focal point, minimal noise, high value contrast.
TUMP: the procedural icons are thin hairline outlines. At the HUD's ~70 px ring the strokes
thin to a pixel or two and several shapes misread (Seismic Stomp reads as a chair, Titan
Fissure as torn paper). Redraw the whole set as filled, chunky silhouettes with one stroke
weight and a baked ink keyline, so they match the black-outlined blocky world and the
Darumadrop lettering. Keep the job vocabulary and one unique picture per ability
(`docs/VISION.md` § 3 rule 1).

### 6. Icons are high chroma; grounds are low chroma; shade in the same hue (Nintendo)
Nintendo: "The icons are important, so make them high chroma. A groundwork such as the modal
window, make it low chroma." Splatoon never used black for shade; shades come from the same hue.
TUMP: confirms the warm settings grey (low chroma ground) and the hero-tinted icons. Hard
offset shadows stay ink per the game's black outline rule; flat fills pick darker SAME-HUE
steps rather than grey.

### 7. One primary per screen, drawn as the only big filled slab (Overwatch, Valorant)
Seen: CONTINUE / SELECT / LOCK IN is the single filled button at bottom centre; everything
else is outline, text or portrait.
TUMP: already true on HOME, character select and the pause card (RESUME). Settings SAVE
becomes filled only when there is something to save, which is the same rule stated in time.

### 8. Same button, same job, everywhere (Splatoon 3 critique)
The critique's biggest complaint is buttons whose meaning changes by context. TUMP keeps
Back as the arrow on every screen, east face button on a pad, and Escape on keyboard.

### 9. Text numbers (XAG 101)
Console 26 px at 1080p minimum, PC 18 px. Glyph lettering must meet the same size. Lines at
most 80 characters; 1.5 line spacing in blocks; sentence case for running text (one or two
word labels exempt). TUMP's 28-unit floor on a 1920x1080 canvas clears the console number.
The keycap glyph drawn beside Back (about 20 units) did not, which is why it goes.

### 10. Characters belong to a cast by construction, palette and silhouette (Overwatch lineups, TUMP's own lineup)
Seen in `rafi-parts-v4-hero-lineup-front.png`: Sean, Cheska, Dante, Zack, Nemu and Phaister
share voxel-stepped hair and clothing detail, one saturated signature hue with gold or cream
trim, a graphic face, and a head silhouette readable at distance. Rafi is built from large
smooth chamfered slabs, is muted sage/navy/tan, has dot eyes, a flat helmet-like hair cap, and
a flat rope ring that reads as a shield. See the Rafi section of `critique-and-plan.md`.

## What does not transfer
- Hi-Fi RUSH's beat-synced motion and manga speed-line wallpaper (TUMP has no music clock).
- Overwatch and Valorant full-screen 3D hero stages and voice barks.
- Splatoon's ink splats as decoration and its pop-up dense lobby.
- Any cold palette: all three shooters' blue-grey chrome breaks `CLAUDE.md` § 6.4.
