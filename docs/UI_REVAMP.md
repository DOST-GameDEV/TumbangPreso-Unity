# UI revamp: Photoshop artboards and the unified style

Started 2026-10-28 on branch `claude/ui-revamp` (from `mobile+network` 9bf9c3829).
The game UI code is unchanged. All restyling so far happens in the PSDs only,
as a proposal for the owner to approve before any game change.

## Phase 1: the current UI as artboards (done, with limits below)

`ArtSource/ui/revamp-2026-10-29/` holds 13 PSDs (`TUMP_UI_<area>.psd`, at most 10
artboards each), one 1920x1080 artboard per screen state (95 in total), `png/`
with one export per artboard, and `links/` with the owner's authored PNGs the
PSDs link to. Keep `links/` beside the PSDs when moving them.

Each artboard, bottom to top: the game world render; a `UI` group following the
game's own hierarchy, with code-drawn panels as raster layers, authored sprites as
linked smart objects (tints as clipped Multiply fills) and labels as live text;
and a hidden `REFERENCE: game capture` layer for comparison.

States not captured: the shared ultimate cinematic (its trigger never fired in
batch). The arrival artboard shows the film before its caption fades in. The
spectator replay picture is a substituted still because batch rendering never
lands asynchronous readbacks; its heading and footer are real.

## Boot direction (owner decisions, 2026-10-30)

After a critique of all 95 states the owner chose the boot (login) screen as the best UI
and its language as the target. This supersedes the halftone restyle below, which stays
only as a record; `revamp-2026-10-30-unified` holds its three test PSDs.

- Kept as they are: boot (sign up, sign in, welcome, password, terms, title) and HOME.
- Everything else is rebuilt with the boot kit, `tools/ui_artboards/bootkit.py`:
  - Ground: the boot weave repainted berry (#6A1B2C to #9A3445). The owner asked for a
    new colour, not a darkened salmon.
  - Panels: a cream card in the boot field's colours, chamfered corners, a lightly wobbled
    rim, a faint cream weave on its face, and the slab 14 units straight below. Cream is
    for the large panels only. Small cards (portraits, room seats, choices, the room code)
    are berry inside a lighter berry rim, lime when chosen, in the same chamfered outline as the
    empty-slot wells, so a filled slot and an open one match (owner, 2026-10-30).
    A panel carrying its own red back or close button is a dark panel with a visible weave and cream words (owner, 2026-11-01).
    No yellow outlines: a viewed or hovered card is slightly darker instead.
    An element may not share its background's colour: on the berry ground, cards are dark.
    Patterns on coloured cards use an overlay blend, never just a lower lightness.
    GAMEMODE's coloured card is approved (2026-11-01): checkerboard overlay, a sharp inner shadow top left, a soft one
    bottom right, and a fill hue-shifted at saturation x1.14 and value x0.94 of the rim's face colour.
  - Buttons: one shape, the boot CREATE button, repainted per role by mapping its slab,
    face and highlight. Lime for the main action, yellow for secondary actions and arrow
    keys, red for leaving and back, cream for unselected tabs. The striped guest button and
    the old title-menu buttons are retired. Labels are solid Darumadrop in a dark shade of
    their own button (dark green, dark orange, dark red, maroon) with no outline.
  - Label placement: centred on the painted face by ink, below the highlight lip (45% of
    the button's height, 47% on pills), and on the paint's own horizontal centre, since the
    PNG margins are uneven.
  - Tabs and sliders: the SIGN UP / SIGN IN track with its lime pill, the pill raised so its
    slab sits inside the track.
  - Titles: white Darumadrop with a 7 unit berry outline, caps centred on the back button.
  - Type: Darumadrop for titles, buttons and labels; Grandstander for sentences. Boot keeps
    its Paalalabas field labels.
  - Spacing: even gaps between title block, tabs, panel and the bottom button row.
- Approved style frames: custom rules, settings (audio) and the match menu, in
  `ArtSource/ui/revamp-2026-10-30-boot/style-frames/` (`bootkit.py --out` redraws them).

Rollout: `bootkit.py` draws onto a `Board` that keeps a flat preview and a layer list.
`tools/ui_artboards/bootscreens.py` lays each screen out from its capture (texts, values,
order) and writes `job.json` for the same `build_psd.jsx`: every kit piece once as a PNG in
`<out>/kit/` placed as a linked smart object, every label as live text placed by its planned
ink, and the old capture hidden on top as REFERENCE. `<out>/preview/` keeps the flat drawing
for checking against the PSD export. Kit files are per size (`button-yellow-400x112.png`),
so a repaint means rerunning the script. Built so far: Settings (9 screens) and Room (15). Then the other
areas, in batches the owner reviews, except boot and HOME.

    py -3 tools/ui_artboards/bootscreens.py --out ArtSource/ui/revamp-2026-10-30-boot

Fixes made in the rework rather than copied: the touch helper text sits inside its panel,
the controller callouts are wide enough for "Retrieve Slipper / Reset Can", the swipe
read-out says 1.0x (the game font has no multiplication sign), off toggles show a cream
pill so lime always means on, and the touch layout toolbar no longer covers the emote wheel.

## Halftone restyle (superseded 2026-10-30)

- Two faces. Darumadrop One for display (titles, mode names, the match clock,
  big numbers already in it). Grandstander for everything else: SemiBold for
  labels, buttons, tabs, stamps and numbers; Regular for sentences and
  descriptions. It replaces Nunito, DIN Next, Paalalabas and Lydian. Grandstander
  is SIL OFL (Google Fonts `ofl/grandstander`), variable weight 100 to 900.
  The owner compared Nunito, DIN Next, Fredoka, Comic Neue, Baloo 2, Mali and
  Grandstander on one panel and chose Grandstander.
- One texture for dark surfaces: the subtle halftone print of the Replays board
  (`HubScenery.Wash`: deep maroon #2A0611, a #7A1830 bloom, 34-unit dots at 30
  degrees swelling toward the lower right). The dark knit plates (maroon name
  plate and wine mode card pieces tiled over large areas) and the rose
  `knit_grain` overlay read as noise and are replaced by a flat fill in the
  plate's own colour plus that print. A full-screen dark knit ground (custom
  rules) becomes the whole Replays ground.
- The bright knit pieces (HERO, TASK, SHOP, PLAY, menu button) are the owner's
  HOME art, which the owner says executes the fabric well. They stay.
- Every slab drops 12 units straight below its face, in the face's own outline,
  instead of spreading out to the lower right. The owner chose this over a
  slight diagonal slab, a soft drop shadow and a thin rim.

The unified PSDs go to a separate folder so the faithful copy stays intact.

## Game rollout (owner, 2026-10-30)

The owner cleared Settings, the Room area and the match menu and asked for them in the
game. Boot and HOME stay as they are. This lifts the earlier "no game UI code" rule for
these screens only; the PSDs in `ArtSource/ui/revamp-2026-10-30-boot/` and the previews
in its `preview/` folder are the target.

Kit textures: `py -3 tools/ui_artboards/bootkit.py --game` writes
`Resources/UI/boot-kit/` (button-yellow/red/plain, pill-yellow/red/plain, ground-berry,
panel, card, card-on, well, field, knob; the lime button and pill are the boot's own
`owner-menu-edits/login3-primary` and `login3-tabs-pill`). They import as plain textures
like the login3 art; slice borders are set in code with `Sprite.Create(..., border)` and
`Image.Type.Sliced`, the way `HubKnit.Knitted` already does. The boot screen itself never
slices (`OwnerMenuArt.Image` is Simple with `preserveAspect`).

Where each family is drawn (paths under `Assets/TumbangPreso/Runtime/UI/`):

- Room area: `Hub/HubCustom.cs` (`HubHost.Build` :38, `HubJoin.Build` :211, `HubLobby.Build`
  :521 with `DrawSeats` :1037, the match settings and close popups :1228/:1293),
  `Hub/HubSeatSwapPopup.cs`, `LobbyChat.Lobby.cs` :32, `CustomGameScreen.MatchSlate.cs` :12
  (custom rules), `Hub/HubMapVote.cs` :25, `Hub/HubQueue.cs` :196 (match found),
  `Hub/HubCharacterSelect.cs` :112. Shared levers: `Hub/HubKnit.cs` (`CutOf` :48 picks the
  art per colour; `Plate`, `Button`, `Field`, `Stamp`, `TitlePlate`; child names Slab, Face,
  Grain, RimGlow, RimLine, Label, Icon are a contract), `Hub/HubKit.cs`, `Hub/HubChrome.cs`,
  `Hub/HubBoardSkin.cs`, `Hub/HubStyle.cs` (colours, fonts, `Floor` 28). Map vote, match
  found and character select still use the older sticker kit through `HubBoardSkin.Dress`.
- Settings: `TumpSettingsView.Workspace.cs` :29 (shell), `TumpSettingsView.OwnerPainted.cs`
  (pages :95 to :188, keep-changes popup `Decision` :238), `SettingsWorkspaceRows.cs` (row,
  toggle, slider, entry, action, chip), `SettingsOptionMenu.cs`, `SettingsPalette.cs`,
  `../Input/ControllerMapScreen.cs` :513, `TumpTouchLayoutView.OwnerPainted.cs` :18.
  The `*Previous` methods are legacy and not called.
- Match menu: `PausePanel.LiveMenu.cs` :11/:32 and `MatchMenuButton.cs` (`KnitOf` :28).

Tests that pin the current look and must be updated with it, not around it:
`Tests/HubLobbySeatControlsTests.cs` :193 (room buttons must be knit with
`HubStickerFeel`), `Tests/PlayMode/TumpNativeSettingsTests.cs` :196-228 (tab font,
controller map callouts and colours, touch editor), `LobbyTypingProbe.cs` :135 (field
graphic named Face), `HubFlowTests.cs` :136/:286 (no hub text under 28),
`HubJoinLanVersionTests.cs` :123, and the name lookups in BrandSettingsTests,
SettingsDecisionFocusTests, OfflineMenuPauseTests, MapVote tests and UiArtboardShots.

Order: shared levers first (HubKnit art per role from boot-kit, HubStyle and
SettingsPalette colours to the kit's), then the per-screen layout changes the PSDs show
(floating map/mode/rules row, stage pick screen, berry small cards, wrapped hints). Check
each area with its exact UiArtboardShots capture test (Pipeline step 1), never a broad run.

### Rollout state (2026-10-31)

Settings, the Room area and the match menu are in the game, uncommitted. The kit lives in
`Hub/HubKnit.Boot.cs`: new `Knit` values (Go, Yellow, Danger, Plain, Pill*, Track, BootField,
Panel, Card, CardOn, Well), the `At`/`In` placers, `Title`, `Back`, `Tabs`, `Tag`, `Modal`,
`Ground`, `KitEdge` for title outlines, and Grandstander in `Resources/UI/fonts`. The old
`Knit` values stay for screens not rolled out yet (GAMEMODE and its popups). `SettingsPalette`
plain names are for controls on the berry; its Panel names are for words on cream.

Running the capture tests overwrites `Logs/ui-artboards`, which `bootroom.py` reads for hero
renders, so re-capture from a build with the old pick screen before redrawing those PSDs.

### Next rollout (2026-11-01)

Boards in `tools/ui_artboards/boot_menus.py`, `boot_heroes.py` and `boot_record.py` are reviewed and go into the game
next: menus, modes, heroes, items, shop, tasks, record, replays, results, loading, break and halftime. Settings and
Room need updating to the newer kit: dark panels where a panel has its own back or close button (Room Chat), dark
cards on the berry ground, rose rims, thin-slab controller callouts. Locked boards are in the boot folder's `approved/`.
Character displays use Ilalim ng Tulay lighting and the Chromatic render style through `ModelPreview`.

### Menus in the game (2026-11-02, uncommitted)

Shared pieces live in `Hub/HubKnit.Menus.cs`: `Frame` (BACK, white title, wallet and menu), `TopBar`, `DarkModal`
(a dark popup with its own red BACK; centred or left-aligned), `PictureCard` and `ShowCard` (picture cards),
`RimOver` (a card's rim closing over a picture), `KitCutPicture.Fit` (cover crop), `Piece` and `At`/`In` for board
coordinates (cards and panels are 14 units taller than the board draws them, `SlabHeight`). Extra kit textures:
`bootkit.py --game` now also writes `card-rim*`, `dark-card-rim*` and the sunbursts; `boot_menus.py --game` writes the warning
tape, warning mark and queue ticket into `Resources/UI/boot-kit`.

In the game: GAMEMODE and its three popups, the hub menu (PC and touch), credits, connection lost, toast, queue ticket,
shop, picture picker, tasks, items (grid and popup), ability popup, local replays, loading and its failure, the player
record shell (tabs, header, panel and SAVE). Room Chat is a dark panel, seats and the code chip on the berry ground are
dark cards, controller callouts are thin-slab buttons (yellow while focused).

Not yet: the hero screen itself, story, skill tree, the rows inside the record tabs, the result sheet (four views),
next court, round break and halftime. See the handoff in the session report.

### Live HUD decisions (2026-11-01)

The HUD is simpler than the menus and uses button colours only. Drawn in `tools/ui_artboards/boot_match.py`.

- Timer: the kit slider grown into a 380 wide rectangle, the kit lime pill draining inside it with a thin slab, the time
  in Darumadrop with no outline (dark over the fill, cream over the track), and the round count below the bar.
- Scores: your score beside your hero, bottom left. Other scores pop up top left for a moment; Tab shows the table.
- Tooltips such as Retrieve Slipper have no background.
- Warning: painted lettering with a flat fill, outline and slab in one darker red, one tilted mark each side, a soft coral
  band behind the words at 55 per cent, and the reason on a cream tag below. Backup in the boot folder's `approved/`.
- Points tags such as +50 are green. Other notices are not settled yet.
- Notices are painted lettering on a soft band: red moments, yellow for the top moment and CAN DOWN (a can each side), lime TAYA.
- Skills and ultimate: rings that fill, yellow for skills, lime for a charging ultimate and yellow when ready. A round glow outside the
  ring marks a ready ability, with none inside it. Bare icon, cooldown number on top, keys below. Drawn in the scratch trial, not yet in `boot_match.py`.
- Score corner: bare hero, painted lime score, yellow place, soft lime band, flat tilted slipper from `UI/slippers-flat` behind them (HUD only).
- Hero portraits are baked with the display's look, crease AO and a dark-tone lift. `tools/build_avatars.py` must be rerun after any portrait bake.

Later board decisions, now in the game (`tools/ui_artboards/boot_match.py`; code in `TumpMatchReadout.Board.cs`, `MatchEventFeed.cs`, `TumpMatchReadout.Statuses.cs`; textures from `export_live_hud.py` into `Resources/UI/live-hud`):
- Others' scores and Tab: grey woven plates flush with the left edge, a nearly solid hero section with an inner shadow. Tab has no panel.
- Kill feed: the same plates on the right edge, seat-colour gradients clipped to the section, points on the green tag.
- Status effects: small coral draining rings right of the score. When one lands, its name and description show over it for about 3 s; Tab lists them.
- Status text queue: soonest to expire goes first; text never outlasts its effect; an effect that ends while waiting is dropped; under 1.5 s left gets the ring pop only; a refreshed effect shows no text again.
- Grey woven plates are for small HUD pieces only (owner, 2026-11-02). The emote wheel is the one large exception. Large surfaces such as held skills, guided training and the training menu keep the dark and cream kit panels.
- Hero and held slipper: a crisp dark stroke at 35 per cent.
- Tab is its own rebindable `Scoreboard` action; ability details moved to F1 (owner, 2026-11-02). A pad and touch still open the table on the info hold, as they have no spare control. In debug builds the first seat's handover is Shift+F1. Holding it shows the plates for every seat and lists every active effect. The old four-chip score row is off in play (`ShowScoreRowInPlay`) and spectators keep it. The connection icon moved to the timer so it stays visible.
- The stroked hero and held slipper are baked copies in `live-hud/hero-corner` and `live-hud/held`; the approved portraits and avatars are never rewritten. Plate heroes are trimmed copies in `live-hud/hero-plate`, so every hero centres by its own ink at one height.

Motion for the live HUD, built with it (spring or eased, never linear; UI Motion Reduced keeps the fades and drops the movement):
- Ability ready: the ring overshoots to 1.12, settles in .25 s, the glow flashes up then breathes slowly at 80 to 100 per cent. Used: the icon squashes, the glow cuts.
- Ring fill: follows the value smoothly; the ultimate's lime arc gains a brief lit tip on each charge gain.
- Timer: fill drains continuously; under 10 s the bar pulses once a second with a small scale.
- Score: the number rolls up and pops 1.15, the +points tag rises and fades. A lead change pops the place.
- Notices: words stamp in from 1.3 with a spring and a slight per-letter stagger, the band wipes out from the centre, the `!` marks or cans pop last with a tilt wobble; out is a quick fade and rise.
- Warning: the band pulses slowly while it stands. Others' scores slide in from the left and out after 3 s. Tab: scrim fades, the panel rises, rows stagger .04 s.

### State at the 2026-11-07 handoff

Main is at `cb8f313b6` with all the UI work below. `QoLUpdates-v2` is behind at `f189981bd`; the owner asked for main only.

Done and on main since 2026-11-03:
- Panels: cream and dark panels use the boards' large weave (`bootkit.panel_art` at 1536 x 864, stretched, not tiled).
- Custom popup (Host, Join) matches its board: the berry card, bold icons, the board's line spacing, the yellow action at 104.
- Mode and choice cards: on hover the card's own border turns white (`paint-*-rim`), a clear-to-dark gradient on the picture, the description slides up clipped to the picture, and a black inner shadow (hard top-left at 35 per cent, soft bottom-right at 40).
- The log-in fit (`login3-tabs`: pill 13 down and 61 tall of 89) on the timer, loading bar, tabs, switches, sliders, XP and task bars. Inactive tabs turn white on hover.
- Short green and yellow tags use `tag-go` and `tag-yellow` (the thin button's two ends) so a short tag does not smear.
- Bots on seats: the grin bot in cream (`boot-kit/bot-face`, from `tools/ui_artboards/export_bot_face.py`).
- Character picker and select seats: figures with no background (`hero-cuts`, rebuilt from our portraits with the approved framing). HOME and the profile keep the approved avatars with their sunburst.
- PICTURE screen: square grid tiles (`frame-rim-116`), pictures clipped to their frames; the big preview keeps the board's chamfer.
- Centre messages: CAN DOWN and the TAYA reveal join the message lanes, so nothing overlaps (`LiveHudCaptureShots.CentreMessagesDoNotOverlap`).
- Tagged's description waits for the catch replay to end, then starts fresh.

Comparison of every board with a real capture (2026-11-05). Still on an old design or missing: Let's Play (approved, but the game shows the old tan page), hero and ability popup, story, skill tree, Next Round, Halftime, results (players, your match), Next Court, powers (F1 and phone), training menu, guided training (PC and phone), spectator controls and phone tools, spectator chips, loading title. The record tabs have their panel but plain-text rows. Smaller: body text runs smaller than the boards (the game has Grandstander 400 and 600 only; the boards use 450 to 800), menu and Host or Join icons, slipper size in item tiles and inspect, the wallet chip, the task reward icon, Keep Your Changes' buttons, the phone settings tabs, the phone HUD's pale buttons, the countdown digit, the ultimate-ready notice, the emote wheel's transparency, the catch replay's REC tag. The capture tests `MatchHud` and `PracticeAndSpectator` still miss some states, and `match-result-standings` captures with the camera inside a character.

Uncommitted in this worktree: `Assets/TumbangPreso/Editor/TumpPosedPortrait.cs` (Dante posed on the avatars' stage for the owner) and the weave exports in `ArtSource/ui/revamp-2026-10-30-boot/weave/`.

Teammate note: Matyu removed the owner's STOP notes from `AGENTS.md` and `CLAUDE.md` twice (`96d0b2427` on `QoLUpdates-v2`, `a6ded64cb` on main). Both times the merge kept the owner's notes. The owner keeps them.

### State at the 2026-11-08 handoff

Main was merged in at `a5e9c0fbe` (it had dropped the STOP notes again in `3dede18c3`; ours were kept). On `claude/ui-revamp`, not pushed: the Let's Play screen and the player record rows from their boards (`a26b38af9`), then uncommitted, in game:
- Round abilities (owner, 2026-11-07): each power is a disc in the kill feed's plate material (`TumpPowerReadout.Discs.cs`, textures `live-hud/disc-*` from `tools/ui_artboards/export_live_hud.py abilities()`). The stroke is the state: gold all round when ready, lime filling clockwise while the ultimate charges, your colour while a power runs. A skill cooling down keeps a grey stroke and a light grey fill (30 per cent) over its inside draining from the top; its icon sits at 60 per cent under the seconds. The game still halves a not-ready icon's brightness (`TumpAbilitySymbol.Muted`); the owner keeps that.
- F1 reference (`TumpPowerReadout.Reference.cs`, board `match-powers`): each power on a soft-cornered kill-feed plate over its disc, a dark header box holding the name with a clean weave circle cut round a small copy of the disc, its key under the mini disc, the ability's short `Summary` on the weave in regular Grandstander 28 (the match's 960 x 540 reading floor). Skills stack right with Q nearest, the ultimate centres over its disc, a header-only page plate on top: ATTACKING, the page key (Interact, F by default), DEFENDING dimmed. Mini disc icons are always full brightness.
- Queue ticket: the cancel button centred on the stub, across and up and down (`HubQueue.cs`).
- Record header avatar fills its frame, clipped to the card (`PlayerHub.RecordBook.cs`).
- Tests updated for these: `OwnerSkillHudReferenceTests`, `TumpNativeHudTests.HeroPowerDetails...` (four-round default, cooldown refresh), `TumpUiCapture` (a painted seal draws no mesh), `AccessibilityControlTests.LargeAbilityReferences...`.
- Key prompts are only the Xelu pack, through `InputGlyphs.For` (dark keys on dark plates, `onDark: false`).

Boards added: `match-hud-discs` (the round abilities, approved), `match-hud-plates` (a rejected flat-plate trial, kept for reference). `boot_match.py` draws authored icons (`icon_art`), not dimmed captures.

Still on old designs in game, next in this order (boards in the preview folder, all approved): hero screen and ability popup (`hub-hero`, `hub-ability`), story (`hub-story`, still the old cream page), skill tree (`hub-skilltree`), Next Round (`match-break`), Halftime (`match-halftime`; the capture currently lands on the results), results views (`match-result-standings`, `-yourmatch`, `-players`; the game draws a cream sheet), Next Court (`match-result-nextcourt`). Then training menu, guided training, spectator controls and chips, phone powers. Smaller gaps are in the 11-07 list above.

## Pipeline

Run from this worktree; the owner's editor and other worktrees are never touched.

1. Capture (Unity, exact tests only, named profile):
   `py -3 tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile ui-artboards-20261029 -runTests -testPlatform PlayMode -testFilter "TumbangPreso.PlayTests.UiArtboardShots.<Test>" -testResults Logs/<name>.xml -logFile Logs/<name>.log`
   Tests: `Boot`, `Hub`, `Room`, `SettingsScreens`, `MatchHud`,
   `PracticeAndSpectator`, `Mobile` (semicolon separated). `Retakes` holds one-off
   second takes. Output: `Logs/ui-artboards/<id>/` (manifest, background,
   reference, straight-alpha rasters) and `index.txt` for order.
2. Job: `py -3 tools/ui_artboards/prepare.py --out <folder> [--style unified]`.
   It crops sprite rectangles into `<folder>/links/` and writes `job.json`.
   `unified.py` holds the restyle rules.
3. Build: run `tools/ui_artboards/build_psd.jsx` in Photoshop 2025 with `JOB`
   (and optionally `ONLY`, a document name) defined first, for example through
   COM `Photoshop.Application.DoJavaScriptFile` on a two-line launcher that sets
   `var JOB = "<folder>/job.json";` then `$.evalFile` on the builder. A finished
   PSD is kept, so a run resumes; delete a PSD to rebuild it. Log:
   `<folder>/build_log.txt`.
4. Sheet: `py -3 tools/ui_artboards/contact_sheet.py --out <folder>`.

Photoshop needs the faces installed for the Windows user (done on the owner's
PC: Darumadrop, DIN Next, Nunito, Paalalabas, Lydian, Grandstander).

## Known limits and open fixes

- Text order: making a layer into text lifts it out of its group, so live text
  landed at the top of its artboard and labels from under a popup showed above
  it. The builder now files each text layer into its group after converting it
  (2026-10-30). All thirteen faithful PSDs in `revamp-2026-10-29` predate that
  and need a rebuild.
- Dimming scrims come out lighter in Photoshop than in the game: Unity blends in
  linear light and Photoshop in gamma, so a straight alpha recovered for one
  cannot match both over every background. Affects dimmed popups (room settings,
  close room, switch seats, connection lost, menu).
- 178 text layers sit inside rasters (masked or carrying mesh effects); the
  unified restyle cannot change their face.
- A PSD is at most 30,000 pixels wide and one 40-artboard document exhausted the
  scratch disk, hence ten artboards per document.

### Live HUD in the game (2026-11-01)

Timer, score corner, rings, notices (warning, moments, CAN DOWN, TAYA, toast) and prompts follow `boot_match.py`. Pieces live in
`Runtime/UI/LiveHud.cs` (`PaintedLetters`, `LiveNotice`); textures come from `tools/ui_artboards/export_live_hud.py` into
`Resources/UI/live-hud`. The old four-player row stays behind `ShowScoreRowInPlay` in `TumpMatchReadout.MatchBar.cs` until the
top-left pop-up and the Tab table exist. The can in CAN DOWN is a stand-in sprite.

Hamburger MENU (2026-11-02): four rows only, SETTINGS, FRIENDS, CREDITS and a red EXIT GAME (`HubMenus.cs`, `boot_menus.py` boards `hub-menu`, `hub-menu-touch`). CAREER stays and REPLAYS joins as the sixth tab of the player profile (`PlayerHub.RecordBook.cs`; REPLAYS opens `HubReplays`, BACK returns to the profile). LEARN TO PLAY and BACK TO TITLE are gone; the tutorial lives inside PRACTICE only, so the GAMEMODE TUTORIAL shortcut is removed. PRACTICE gets a RULES button (`PracticeRules`) for its rules and bot difficulty. Hub navigation only: no rules, networking or save changes.
