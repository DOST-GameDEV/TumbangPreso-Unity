# UI and HUD review: research, screen critique and ordered plan (2026-09-23)

Owner request: research first, then refine the whole game's UI and HUD, with Settings as a
dedicated priority. Mid-review the owner added: "improve ui LOOK of all current screens please
like all buttons etc and typography etc", "u figure out what to improve from ur research and
analysis", and, about GAMEMODE SELECT, that the wireframe drawings were placeholders: each mode
card should get its own personalised picture, with a short mode description on hover. The
wireframes in `ArtSource/front-end-flow-20260923/` are the UX draft (structure, placement,
flow), not the visual design. Later the same day: "i want to improve all skill icons too",
"improve the model of rafi bcz it DOESNT LOOK GOOD at all compared to reference its supposed to
have", and "it doesnt look like it belongs in hero cast too". These newer owner instructions
override the original brief's "do not alter character models" for Rafi only.

The research that this plan cites is in `research.md` beside this file (sources actually
inspected, findings, and what does not transfer).

Baseline: HEAD `a7c4d1b3`, one PlayMode capture pass (HubFlowTests, TumpNativeSettingsTests,
TumpNativeHudTests, TumpNativeFrontEndTests, TumpNativeResultTests): 21 cases, 18 passed,
527 frames at 960x540, 1280x720, 1920x1080, 1280x960 (4:3), 1600x680 and wider shapes.
Three failures predate this review and are recorded, not repaired here:
`OwnerAccountUsesExactArtworkTypeColoursAndWorkingTerms` expects the retired phrase
"PLAY FAIR" in the rewritten Terms; `TitlePlayCreditsAndSettingsReturnThroughNativeViews`
walks the retired title route (TAP TO START now opens HOME); `RematchActuallyLoadsTheChosenArena`
reports the rematch staying on Eskinita after choosing Bayan Plaza (a possible real defect,
logged in TODO for the gameplay lane; the native two-peer rematch receipt is a different route).

## 1. References, and what transfers

Chosen because each solves a TUMP problem, not for popularity.

| Reference | Source viewed | Problem it solves for TUMP | Transfers | Does not transfer |
|---|---|---|---|---|
| Nintendo, Splatoon 3 menus and lobby | Game UI Database id 1512; Inkipedia "Splatoon 3 UX" critique; UI Crunch #13 talk notes (Nintendo designers on Switch and Splatoon) | Playful identity that stays readable; one obvious focus | One loud focus state (thick ring plus lift), big display type only for names and verbs, body copy in a plain face, every screen answers "what do I press" | Splatoon's ink splats as decoration; its dense lobby of pop-ups (Inkipedia's own critique) |
| Hi-Fi RUSH menus | Game UI Database id 1678; Interface In Game entry | Graphic hierarchy with personality; comic halftone and bold slabs | A shape-first system: chunky outlined slabs, one accent per screen, hard offset shadows, type that is either huge or small and never medium-everything; motion snaps on a beat instead of floating | Beat-synced UI (TUMP has no music clock); its manga speed-line wallpaper everywhere |
| Overwatch 2 hero select, Valorant agent select | Game UI Database ids 1341, 1043; prior in-match research in `visual-research-2026-09-23/findings.md` | Learning a kit while a clock runs | Ability icons as one flat family of solid silhouettes; the focused one's name large, kind and cost as small meta, one sentence; the clock as a prominent NUMBER (both games, inspected) | Their full-screen 3D stage and voice lines |
| Overwatch 2 / Valorant / Apex settings | Game UI Database settings screens; OW2 UX case study | A long form that is still scannable | Group headers inside a tab; one fixed description strip for the focused row instead of inline paragraphs; a real SAVE/APPLY button with an unsaved marker; values as chips (keycaps as keycaps) | Hundreds of options; tiny 14 px type |
| Sepak U | official itch.io screenshots (findings.md section 2) | Coordinated spectator and in-match feedback | Already applied by VISUAL-1 (one HUD band, HUD recedes at peaks, gold on dark interstitials). Pause and result cards follow the same warm ink card family | 2D staging |

Principles taken (TUMP wording):
1. **Shape says pressable.** Every pressable thing is an outlined sticker with a hard shadow
   (`HubShape`); plain floating words are never buttons. Rows and panels are flat.
2. **Two faces, three sizes.** Darumadrop One for names, titles and verbs; Nunito Bold for
   anything read as a sentence or a value. Per screen: one display size, one label size,
   one body size.
3. **One focus language.** Golden ring plus lift, the same on every screen, including Settings
   and the pause menu.
4. **Say it once.** No navigation tutorials; keep rules, constraints, errors and bindings.
5. **Warm darks only.** CLAUDE.md 6.4 bans cold greys; the owner asked for DARK GREY Settings
   (2026-09-15). Both hold with a neutral-warm grey (red channel at or above blue).

## 2. Screen-by-screen critique and plan

Each row: observed problem, what stays, principle, change, owner file, acceptance check.
"Keep" means inspected and judged working; no change is manufactured.

### Settings (front end and in-match pause share `TumpSettingsView`), priority

Observed at every shape, all five tabs:
- Palette is COLD: background 29,31,35, surface 42,45,50, control 58,62,68, rule 94,101,110
  (blue above red). Violates 6.4; reads as a generic dashboard next to the warm game.
- Triple labelling: SETTINGS, the lit tab and a 62-unit section heading all say where you are.
- Patronising copy: "Save your changes when you're ready." (sidebar), "Listen as you adjust.
  Save to keep your changes.", "Preview a change, then save it or return to your previous
  settings.", and "Interface motion can be reduced while gameplay movement stays visible."
  duplicating its own row.
- Accessibility is one flat list of 15 controls and four paragraphs; nothing groups text,
  motion/effects and controls/camera.
- SAVE CHANGES is floating words in the corner, grey when clean, indistinguishable from a label;
  there is no unsaved marker except transient status text.
- Binding values ("W", "S") and "OPEN"/"RESET CONTROLS" are plain words: nothing says they
  are pressable, and keys do not look like keys.
- Focus is a 3-unit underline under the control only; at 960x540 it is about 1.5 px.
- Keep: the two-column layout, section order, the row/control widths sized for 4:3, the
  Larger-text reflow, the save/discard transaction, option popups, binding capture, controller
  map and touch layout routes.

Plan (source `TumpSettingsView.*.cs`, `SettingsWorkspaceRows.cs`, `SettingsPalette.cs`,
`SettingsControlFocus.cs`, `SettingsOptionMenu.cs`):
1. Neutral-warm dark grey palette (same values, red channel lifted over blue).
2. Section heading kept (it anchors the scroll column) but the sidebar sentence and the two
   navigation notes go. Genuine explanations stay (telemetry disclosure, frame-rate reason,
   toggle-restore rule, reduced-effects scope, unrecognised controller, captions).
3. Group headers: Accessibility becomes READING, MOTION AND EFFECTS, PLAY CONTROLS; Graphics
   DISPLAY and PERFORMANCE; Controls DEVICE and BINDINGS. Small Nunito caps in the accent.
4. SAVE CHANGES becomes a filled accent button when there is something to save, a quiet
   outlined one when not, with an "Unsaved" chip beside it while dirty.
5. Binding values draw as keycaps (outlined, rounded chip); action rows (OPEN, ARRANGE,
   RESET CONTROLS) as outlined pills. Listening state fills the keycap.
6. Focus and hover light the WHOLE row, loudly (research finding 1, Hi-Fi RUSH): a visible
   accent band, a solid 8-unit accent bar at the left and the row's label turned accent. The
   first draft's 9 per cent band was too faint to find from a couch.
7. Group headers carry a short accent rule underneath (finding 2), so a long tab is scanned
   header to header.
Acceptance: five tabs at 960x540, 1280x720, 1920x1080, 1280x960, 1600x680, normal and Larger
text; existing TumpNativeSettingsTests 5/5 (save/discard, rebinding, controller/touch return,
pause child discard); every label at or above 28 units.

### Pause menu (`PausePanel.LiveMenu.cs`)
Problem: cold dark green column (29,46,35); RESUME is a flat lime rectangle with no focus or
press state; SETTINGS and LEAVE MATCH are floating words, so the three actions read as three
different kinds of object. Keep: the left column over the live match, the live-match notice
(a real rule), the names ResumeMatch/PauseSettings/LeaveMatch and their callbacks.
Change: warm ink card (the in-match card family), the three actions as hub stickers: RESUME
primary chartreuse, SETTINGS honey, LEAVE MATCH deep red (the one destructive control).
Acceptance: LivePause captures, `PauseEscapeRespectsChildSettingsDiscardAndReturn` passes.

### HOME (`HubHome.cs`)
Problem: the HERO door's label is drawn over the portrait's torso, so "HERO" sits on dark
clothing; the XP groove at level 1 reads as an empty black bar. Keep: the wireframe layout,
sticker doors, colours, the HOME scene layer and its HOME-only behaviour, PLAY and the mode card.
Change: a label band on picture doors (HERO, LOADOUT) so the word sits on its own plate;
an XP track with a visible honey rim. Acceptance: Hub-Home captures at five shapes.

### GAMEMODE SELECT (`HubModeSelect.cs`)
Problem (owner): the four cards reuse cropped roster portraits (heads cut at odd places), not
pictures made for each mode. Keep: layout from the UX draft (small stacked PRACTICE/CUSTOM,
tall CLASSIC/RANKED), hover and focus reveal the short description, all routes.
Change: one purpose-made picture per mode, posed with the real models in the real court
(the HOME method: real models, faces never altered): PRACTICE one kid alone throwing at the
can; CUSTOM friends together; CLASSIC street kids round the can in the chalk circle; RANKED
heroes mid-action. Rendered in-engine by an editor tool, committed as sprites with provenance,
fitted to each card's aspect. Acceptance: all four cards at five shapes, default and focused.

### CHARACTER SELECT (`HubCharacterSelect.cs`)
Problem: the model stands on an empty olive rounded rectangle that reads as a blank text field;
the ability column leaves a 70-unit dead gap between the ability name and its kind/cost; the
ability name is set smaller than its own metadata's visual weight; the clock is a bare "29".
Keep: model height, portrait grid, seats rail, SELECT, the running deadline, Classic neutrality.
Change: an ink contact shadow instead of the plinth; name at Title size with meta and sentence
directly under it; the clock stays a NUMBER (research finding 4 corrected the first draft's
ring) on a small plate with a "PICK" label, turning persimmon for the last five seconds.
Acceptance: Hub-CharacterSelect normal and A11y at five shapes; selection timer test still passes.

### SKILL ICONS (`AbilityIcons.cs`, every surface that draws a kit), owner addition
Observed (Hub-CharacterSelect, Hub-Hero, CourtHud-held-skills baseline captures): every glyph
is a thin hairline outline. In the HUD's ~70 px ability ring the stroke is one or two pixels;
Seismic Stomp reads as a chair, Titan Fissure as torn paper, several shapes are empty frames.
Keep: the job vocabulary (`LabelFor`), one unique picture per ability, white-on-transparent
tinted at the use site, the cooldown disc, every call site.
Principle: research findings 5 and 6 (Valorant/LoL filled single-weight family; Nintendo
high-chroma icons on low-chroma grounds) and TUMP's black-outline rule.
Change: redraw all 27 glyphs as filled, chunky silhouettes of one visual weight with a baked
ink keyline (black survives `Image.color` tinting, so the outline stays black on every hero).
Authored by `tools/build_ability_icons.py` into committed sprites under
`Resources/UI/ability-icons/`, loaded by `AbilityIcons.For`, with the procedural `Bake` kept as
the fallback, which is exactly the swap its own header anticipates.
Acceptance: an icon sheet at 256, 96 and 48 px inspected; HUD ring, HERO tiles, character
select and hold-to-inspect captures; `HeroPresentationTests` uniqueness still holds.

### RAFI MODEL (`tools/build_rafi_voxel.py` only), owner addition
Observed against `ArtSource/badjao/rafi-refinement-20260922/rafi-distinctive-hero-v3.png`
and the six-hero lineup: (1) CONSTRUCTION: the cast's hair and clothing detail are small
voxel-stepped cubes; Rafi's hair, hip panel and rope are large smooth chamfered slabs and a
flat ring that reads as a shield. (2) PALETTE: each hero owns one saturated signature hue with
gold or cream trim; Rafi is muted sage, navy and tan and reads as an NPC. (3) FACE AND
SILHOUETTE: the reference's tall messy crest is gone (the hair is a flat helmet cap) and the
face is two dots, the blandest in the cast.
Keep: owner rulings (no gills, no eyebrows, no swept/curved hair, block-built hair, native
skull/proportions/rig, never edit another hero or the original builder), kit, lore, the
headwrap and float clip, the one left wrist cord.
Change: voxel-stepped hair crest with real volume above the headwrap (the reference's
silhouette, rebuilt from cubes rather than curves); headwrap tails at the back; a saturated
sea-green signature hue with cream trim and one orange accent; sash and hip cloth as stepped
voxel layers instead of one diagonal slab; the rope coil as a small stepped coil at the hip,
not a ring; a graphic eye shape with character (no brows). Rendered through the canonical
in-engine pipeline, versioned filenames, turnaround plus full lineup.
Acceptance: turnaround and lineup inspected beside the cast; protected-hero hashes unchanged.

### Remaining surfaces
| Surface | Verdict | Change |
|---|---|---|
| Back button ESC badge (every hub screen) | The keycap sprite is drawn about 20 units on the Back sticker's corner, illegible at 960x540 | Drop it for keyboard/mouse (Escape is universal); keep the pad's east glyph, drawn at a legible 40 units beside the arrow |
| HERO | Works: model card, kit tiles, biography, story door | Keep |
| LOADOUT | The selected item's NAME is nowhere on screen until the popup opens | Selected item name above EQUIP in the empty right column |
| ITEM POPUP, SHOP, TASKS, SKILL TREE, AVATAR, MENU | Inspected; consistent sticker family | Keep unless the shared changes touch them |
| HOST | Clear form, good grouping | Keep |
| JOIN | Empty list says "Looking for public rooms online..." and "Nobody yet" at once, in the display face at body size | One state line at a time, in the reading face |
| LOBBY | Clear; code plate strong | Keep |
| MATCH FOUND, MAP VOTE, LOADING, Terms, login/title | Recent work, protected art; inspected | Keep |
| In-match HUD, halftime popup, match end | VISUAL-1.4/1.16/1.17/1.18 done and captured today; no new defect seen | Keep (popup stays a popup) |

## 3. Order

1. Settings (all seven changes) and the pause menu, plus the shared hub fixes (back prompt,
   HOME label band and XP track, character select, loadout name, join empty state): one C#
   batch, one focused Unity run (settings, hub flow, pause captures).
2. While that run is in flight (no C# edits): author the new skill icon set in Python and
   inspect it; rebuild Rafi in his own builder.
3. Icon loader swap (C#), Rafi model review render, one Unity run.
4. Personalised mode-card art: editor render tool, four sprites, card fitting, one capture run.
5. Evidence, TODO, ledger and inventory, push.
