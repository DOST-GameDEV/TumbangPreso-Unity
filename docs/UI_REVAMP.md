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
    are berry inside a tan rim, lime when chosen, in the same chamfered outline as the
    empty-slot wells, so a filled slot and an open one match (owner, 2026-10-30).
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
