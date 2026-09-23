# UX-1 plan: the owner's front-end flow, built

Written 2026-09-23 before any UX-1 code, from `owner-brief.txt`, `intake.md` (the latest
correction: login and the title screen stay untouched, HOME opens from TAP TO START), the
eleven sketches under `ArtSource/front-end-flow-20260923/`, and a read of the current
front end. `docs/TODO.md` UX-1 is the status queue; this file is the design it points at.

## 0 · Where everything lives, and why

**HOME is built inside the `MatchSetup` scene, over the live court, not as a new scene.**
`ConvertedMatchSetup` already owns every networking path the brief says must keep working:
`NetSession` start and stop, the LAN auto-host, `LobbyJoinPanel.Connect` (codes, addresses,
LAN and online rows), the `Matchmaker` queue and its bot-fill offer, `MatchRpc` picks, ready
and start, reconnect (`RejoinRunningMatch`) and rematch return. A HOME in another scene would
either duplicate those paths (`docs/TODO.md` § 38.5: the maintained protocol becomes the one
nothing calls) or tear the lobby down every time the player looked at HOME, which would make
the top-centre queue plate impossible. So the controller stays and its view changes:

| Before | After |
|---|---|
| Title → `ModeSelect` (LET'S PLAY) → `MatchSetup` (preparation board) | Title → `MatchSetup` showing **HOME** |
| `OwnerPreparationView` is the screen | `TumpHub` is the screen; the preparation view is built and hidden, and its live `MapPreviewSurface` becomes HOME's full-bleed background layer |
| `LeaveMatchToMainMenu` → title | → HOME (the title is the boot screen; returning from a match to TAP TO START was an extra press) |

`ModeSelect`, `CourtPlayView` and `OwnerPreparationView` stay on disk (§ 68.3 keep-the-old-
chrome rule). The title screen (`HomeCourtView`) changes only its destination.

## 1 · The spine every new screen shares

Repeat the chrome, never the composition (`Front_End_Design.md` § 0).

- **BACK** top left, always the same object; Escape, pad B and Android BACK go through
  `MenuNav` via `ConvertedMatchSetup.Cancel` into one hub stack, innermost first.
- **Currency and hamburger** top right on HOME, GAMEMODE SELECT, LOADOUT and HERO.
- **The one primary** bottom right, chartreuse, the largest object (PLAY, SELECT, EQUIP,
  UNLOCK, CREATE LOBBY, START GAME, JOIN).
- **Popups** dim the screen, carry their own `ScreenFocus` so a pad cannot walk out of them,
  and close on BACK. Popups are the owner's: SHOP, HOST/JOIN, CLASSIC-or-HERO-STRIKE, ITEM,
  hamburger, ability details, profile picture.
- **Visual identity (the part the brief leaves to design):** street stickers. Every
  pressable is a flat logo-palette fill inside a heavy warm-black outline with a hard offset
  shadow, a one-degree lean and a chamfered corner: a sticker slapped on the court. Furniture
  (panels, plates) is rounded and has no shadow, so a chamfer still means pressable
  (`CLAUDE.md` § 6.5). Display type Darumadrop, reading type the supporting face. No blue or
  navy (§ 6.4), no white or pale default: grounds are the live court, Army olive, Persimmon,
  Golden and Honey Quartz at most as a tag. Outlines are warm black `#1C0F06` (the in-game
  black-outline rule and the palette's ink agree on it). Chalk scribbles decorate only empty
  ground (§ 1.3).

## 2 · Every screen, its four answers (CLAUDE.md § 6.2)

| Screen | The ONE thing | First press, guessable? | Not needed now | Out in one press |
|---|---|---|---|---|
| **HOME** | PLAY, under the mode card saying what PLAY will do | PLAY, bottom right, biggest | every destination is one sticker, nothing opens by default | Escape asks nothing and leaves to the title (the title's own Escape quits) |
| **GAMEMODE SELECT** | the four cards | a card; hover/focus reveals its one-line description | descriptions until hovered | BACK/Escape to HOME |
| **CLASSIC popup** | CLASSIC or HERO STRIKE (casual) | either card | nothing | BACK closes to GAMEMODE SELECT |
| **HOST/JOIN popup** | two doors | either | nothing | BACK |
| **HERO** | the hero, large, left | ability icon or arrows | ability details until an icon is pressed | BACK |
| **LOADOUT** | the item grid | an item | STATS is deleted; details live in the item popup | BACK |
| **ITEM popup** | the 3D item | EQUIP (or BUY when unowned) | nothing | BACK |
| **SKILL TREE** | the hero's two slots and their branches | a branch | challenge text shows on the focused node only | BACK |
| **SHOP popup** | HERO SHOP or ITEM SHOP | either | prices until a shop is open | BACK |
| **TASKS** | today's three tasks and the week's three | CLAIM on a finished task | how currency is spent (that is the shop) | BACK |
| **Queue plate** | elapsed time | X cancels | band and search detail stay in the log | X or Escape while queued |
| **MATCH FOUND** | the found beat | nothing to press; it advances | everything | not cancellable, like every queue pop |
| **CHARACTER SELECT** | the chosen fighter, big name and model | a portrait | loadout (its own screen) | pick stays; the timer ends it |
| **HOST** | CREATE LOBBY | the name field is pre-filled so CREATE works at once | nothing | BACK |
| **JOIN** | the server list or the code field | a row's JOIN, or the code field | the other source until chosen | BACK |
| **GAME LOBBY** | the players n/4 | START GAME (host) or READY | rules behind the settings icon | BACK leaves the room |
| **LOADING** | the map, its name and the percentage | nothing | tips sit at the bottom | none (loading) |
| **Hamburger** | settings, party, career hub | a row | nothing | BACK |
| **Profile picture** | the big picture | another picture | nothing | BACK |

## 3 · Flows (every press counted, § 6.3)

- **Ranked queue:** mode card → RANKED → PLAY. 3 presses from a fresh HOME, 1 once set.
- **Casual:** mode card → CLASSIC → CLASSIC or HERO STRIKE → PLAY.
- **Practice:** mode card → PRACTICE (starts immediately, offline bots, selected ruleset).
- **Host a custom game:** mode card → CUSTOM → HOST → CREATE LOBBY.
- **Join by code:** mode card → CUSTOM → JOIN → CODE → type → JOIN.
- **Change hero / item:** HERO or LOADOUT on HOME (1 press to the screen).
- **Buy:** SHOP → HERO SHOP or ITEM SHOP → the hero/item → UNLOCK/BUY.
- **Earn:** currency + → TASKS.
- **Settings / party / career:** hamburger → row.
- **Profile settings:** the name plate. **Picture:** the square avatar.

## 4 · The queue, match found and character select, on today's matchmaking

- PLAY calls `Matchmaker.StartQueue(mode, stake, party)`: RANKED is `QueueStake.Ranked` +
  Hero Strike; CLASSIC is `QueueStake.Casual` + the chosen ruleset. Refusals (guest in ranked,
  cooldown, party size) show on the plate's line, never silently.
- The plate is drawn from `Matchmaker.State/Elapsed`; X is `Matchmaker.Cancel` plus stopping
  the room the queue opened (a HOME with no queue has no room). The bot-fill offer
  (`OffersBotFill`) appears under the plate as PLAY WITH BOTS, re-homing Phase 11.
- **MATCH FOUND** fires when the room is full (`OccupiedSeatCount() == Balance.PlayerCount`)
  or the host accepts bots. Every peer sees it from the replicated roster, so it is the same
  moment on every machine without a new message.
- **CHARACTER SELECT** follows for everyone. Picks go through `SelectLobbyPickServerRpc`,
  so today's pick rules and replication are unchanged. SELECT locks in with
  `DeclareReadyServerRpc(true)`; the host starts with `HostStartMatch` when every human is
  locked or the 30 s clock ends. No wire change.

## 5 · Custom games

- HOST writes `NetSession.RoomTitle` and `RoomVisibility` before starting the LAN host or the
  Relay host. The UGS lobby record gains `Map` and `Title` fields (additive data, old clients
  ignore them) and `IsPrivate` for PRIVATE and FRIENDS ONLY; FRIENDS ONLY keeps advertising
  the code through presence, PRIVATE does not. Default map is the last one played.
- JOIN: Dedicated (Internet) lists `ServerQuery.Servers`, Dedicated (LAN) lists
  `LanBeacon.SortedEntries`, one row look, header SERVERS (ONLINE) or SERVERS (LAN). CODE is
  a four-character field. Every JOIN goes through `LobbyJoinPanel.AutomationJoin`, which is
  the same `Connect` the old panel pressed.
- GAME LOBBY reads the replicated roster (`LobbySession`, `_replicatedPicks`); START GAME is
  `OnStartPressed`, READY is `OnPrimaryPressed`, character is CHARACTER SELECT without a clock,
  settings is `CustomGameScreen` (host edits, peers read).
- LOADING: the arena's own art as background, the name, a real percentage from the async
  load, tips, and the BH Studios mark.

## 6 · Currency, shop, tasks, skill tree, unlocks

- **Currency: TANSAN**, bottle caps, the currency every Filipino kid already played for.
- **Authority:** new Cloud Code script `wallet.js` (UGS project dcf0831e..., never relinked)
  owns a protected Cloud Save key `wallet`. A client never writes it. Actions: `load`
  (creates or migrates), `settle` (pays matches the server itself recorded in `matchHistory`
  and advances tasks from the same records), `claim` (a finished task), `buy` (an item id the
  server prices). Every parameter is declared in `module.exports.params`.
- **The rules are specified once in the engine-free core** (`Economy.cs`, `EconomyRules`) and
  asserted in `Core.Tests`; `wallet.js` mirrors them exactly as `match-record.js` mirrors
  `ProfileRules`.
- **Migration, never reset:** a wallet is created on first load with the starter balance, the
  starter set, and every hero and tsinelas the server's own career record shows the player has
  already played. Existing `careerProfile`, `matchHistory`, `accountProfile` and every id are
  untouched.
- **Starter set:** heroes DANTE, CHESKA, SEAN; the first four tsinelas and the first four
  lata. Everything Classic stays free and neutral (VISION § 1). The starter lists are one
  constant each so the owner can widen them in one line.
- **Offline:** the last wallet the server returned is cached read-only. Without any server
  answer the starter set is owned. Practice, LAN and the tournament preset let every hero and
  item be used, because the venue is offline and a skill challenge must be earnable in
  Practice; ownership gates the online routes (queue and online custom rooms).
- **No real money, no paid service.** The + by the currency opens TASKS.
- **Skill tree:** the existing `HeroLoadoutRules` variants and `AbilityChallenges` cast
  counters, per hero, slot 1 and slot 2, default and alternative as branches, plus mastery
  level. The dot on HOME's card means a variant has unlocked since the tree was last opened.
  Skill alternatives leave LOADOUT (its SKILLS tab is deleted, per the brief).

## 7 · Three devices, sizes, accessibility

All screens are built through `OwnerUiLayout.Canvas` (installs `ScreenFocus`,
`InputSurfaceCheck`-approved) or as children of it; popups carry their own `ScreenFocus`.
Every label is 28 canvas units or more. Layout anchors to the canvas corners, and the canvas
is never under 1920x1080 units (`Expand`), so 960x540, 1280x720, 1920x1080, 4:3 and the
owner's 1600x680 window all resolve. Prompts use `Rebinding.DisplayNameFor`. Larger text and
High contrast read `GameSettings.LargerText` / `HighContrastHud`.

## 7b · Where it is built

| Piece | Files |
|---|---|
| Kit (sticker surface, icons, buttons, popups, prompts, motion, chalk) | `Runtime/UI/Hub/HubStyle.cs`, `HubShape.cs`, `HubGlyph.cs`, `HubButton.cs`, `HubKit.cs`, `HubPrompt.cs`, `HubSlap.cs`, `HubPattern.cs`, `HubForms.cs`, `HubChrome.cs` |
| Router and contract | `TumpHub.cs`, `HubScreen.cs`, `IHubHost.cs`, controller side `ConvertedMatchSetup.Hub.cs` |
| HOME, mode select and its popups | `HubHome.cs`, `HubModeSelect.cs` |
| Queue plate, MATCH FOUND, toast | `HubQueue.cs` |
| HERO, LOADOUT, ITEM, CHARACTER SELECT | `HubHero.cs`, `HubLoadout.cs`, `HubCharacterSelect.cs` |
| HOST, JOIN, LOBBY, LOADING | `HubCustom.cs`, `HubLoading.cs` |
| SHOP popup, TASKS, SKILL TREE, hamburger, picture | `HubMenus.cs`, `HubTasks.cs`, `HubSkillTree.cs` |
| Economy rules and server | `Packages/com.tumbangpreso.core/Runtime/Economy.cs`, `ugs/cloud-code/wallet.js`, `Runtime/Net/WalletStore.cs`, tests `Core.Tests/EconomyTests.cs`, `tools/test_wallet_script.js` |
| Routing | `HomeCourtView` (TAP TO START), `SceneFlow.GoHome`, `SceneFlow.LeaveMatchToMainMenu`, `SceneFlow.Go` (loading) |
| Room listing | `NetSession.RoomTitle/RoomMap/RoomVisibility`, `ServerQuery` `Map`/`Vis` lobby data (additive), `SocialStore` presence |

## 8 · Verification plan (one focused pass per batch)

Batch 1 (HOME, mode select, queue): `HubFlowTests` PlayMode fixture: HOME builds from the
title's destination, every door opens its screen and BACK returns, mode card state, practice
start request, queue plate appears on a started queue and disappears on cancel; captures at
the five shapes. Later batches add their screens to the same fixture and capture set.
Economy: `Core.Tests/EconomyTests.cs` (prices, migration, task progress, claims, payouts),
plus `node` over `wallet.js` with a stub DataApi. Live UGS deployment needs the owner's `ugs`
login on a machine that has it: this machine has no `ugs` CLI.
