# Hand-made HOME (owner's Canva board, 2026-10-21)

The HOME screen is the owner's hand-made board: Canva design `DAHUJISshgw` ("TUMP"), page 68
(labelled) and the no-text page beside it. Everything here rebuilds that board for the game at
2x the 1920x1080 layout, so it is pixel-exact on a 4K screen and only ever downscaled below that.

Owner rules this pipeline enforces (2026-10-21):

- **Her originals, not the page export.** Every piece comes from her uncropped source images
  (Canva media exported at native size from TUMP pages 70 to 78 into `canva/src/`). The cut lines,
  specks and rim in the page export came from Canva cropping them on the board.
  `canva/copy_elements.json` holds each board element's frame and imageBox (same ids as TUMP
  page 69), which places every native pixel on the board exactly.
- **Finished pieces are not edited.** HERO, ITEM, SHOP, TASK, PLAY (and their icons, and the queue
  plate) are her pixels. The only thing allowed is smoothing an edge the export spoiled (their
  near-white anti-aliasing hairline); the icons' cream outline is her sticker border and stays.
- **Unfinished pieces are refined to match the finished ones**: the avatar and name plate, the
  currency bar with its + and menu, and the mode card (whose bottom was a crop line). They get her
  own knit (lifted from her mode card and PLAY) and a darker floor like PLAY's, drawn below her
  silhouette so nothing on them moves. Peach stays flat.
- **No white or cream on any edge, nothing pixelated.** Edge fix: specks under 400 px go, cream
  rim pixels take the colour just inside, the silhouette is smoothed (4x up, sigma 3, threshold,
  area down), the outer 3 px take their colour from inside, colour is bled under alpha 0.
  `tools/audit_edges.py` fails the build on any white or cream edge pixel outside her own art.
  Several of her originals are smaller than the 2x board (tile faces 1.5x, queue 1.4x, mode card
  and PLAY about 1.1x per unit); they are resampled once with Lanczos, never twice.
- **Exact fonts, colours, positions and sizes** from her Canva text layers (`canva/page68_text.json`).
- **People are the game's model renders**, never drawn (`docs/HOME_SCREEN_ANIMATION_METHOD.md` 2).

## Faces (all in `Assets/TumbangPreso/Resources/UI/fonts`)

| Board role | Face | Used for |
|---|---|---|
| display (Canva `YAFyMTTArpM`) | Darumadrop One, set in capitals | PLAY, HERO, ITEM, SHOP, TASK, mode name, queue clock, player name |
| digits (`YAEY67jaCX4`) | Paalalabas Display Wide | currency, level number, XP |
| tag (`YAFcf5egi5g`) | Lydian | the #1234 tag |
| heading (`YAFcfwDhZC4`, bold) | DIN Next LT Arabic Bold | GAMEMODE |

The owner confirmed Paalalabas Wide and Lydian by name; Darumadrop and DIN Next were matched
glyph for glyph against her export.

## Run order

```
python -I tools/extract_originals.py   # her originals -> out_src/, finished untouched, 1:1 before/after
python -I tools/refine_unfinished.py   # the unfinished plates refined -> out_src/*_r.png, board preview
python -I tools/assemble_board.py      # every game slot -> out/ + manifest.json, audited
# Unity, graphics on: -executeMethod TumbangPreso.EditorTools.HandmadeLobbyAuthor.BakeModeFigures
python -I tools/build_mode_art.py      # the three GAMEMODE window pictures from renders/
python -I tools/export_board.py        # sprites + board.json -> Resources/UI/handmade-lobby
```

`canva/slide68@2x.png` (no text) and `canva/labelled68@2x.png` are her page exports, used for her
vector glyphs and shape layers and for checking positions; `canva/src/` holds her originals.
Re-export both from Canva if she changes the board, then re-run.

## In the game

`Runtime/UI/Hub/HubBoard.cs` reads `board.json` and places sprites and labels (labels sit on the
board's measured baseline via `BoardBaseline`). `HubStickerFeel.cs` is the press/hover/idle motion.
`HubHome`, `HubChrome.TopRight` (currency, menu: every hub screen) and `HubQueuePlate` use it.
