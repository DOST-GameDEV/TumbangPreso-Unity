# Room, host flow and match menu: UX research and plan

Owner request, 2026-10-28: refine the custom room (host, joined player, spectator),
its chat, the HOST GAME path into it, and the match menu with its leave
confirmation. Goal in the owner's words: intuitive, simple, not too many options
at once. Gameplay HUD and in-match chat are out of scope.

## What other games do

Sources read for this pass: the Among Us lobby reference (fandom wiki), NN/g
"Confirmation Dialogs Can Prevent User Errors", Indieklem "Creating an intuitive
in-game menu", and the Game Accessibility Guidelines list. Patterns from Fall Guys,
Rocket League, Overwatch, Valorant, Stumble Guys, Brawl Stars, Party Animals,
Pummel Party and Mario Kart are from play knowledge, not fresh captures
(Game UI Database refused the browser with a bot check).

### Room / lobby

| Pattern | Seen in | Why it matters here |
|---|---|---|
| One layout for everyone; the host sees EDIT where others see VIEW | Among Us, Overwatch custom, Valorant custom | Players learn one room. Roles differ by what is editable, not by where things are. |
| Room facts grouped in one card: code, players n/max, map, privacy | Among Us right panel, Fall Guys custom | Today map/mode/network is a 34 pt subtitle and the code, address and map button sit in three corners. |
| The map choice lives with the map facts, inline arrows or list | Pummel Party, Valorant, Rocket League private match | CHANGE MAP sits bottom-left, far from what it changes, and opens a popup. |
| Only the host has START; others wait with a clear sentence | Among Us, Fall Guys, Party Animals | Keep. Players need status, not controls. |
| Seat actions are small and secondary (Add AI in the empty slot, a small remove) | Overwatch, Rocket League, Pummel Party | Three full-size REMOVE BOT buttons are the loudest thing in the current room after START. |
| Spectate is a quiet role switch, not a roster header button | Overwatch spectator column, Rocket League | WATCH INSTEAD competes with the roster title. |
| Code is big and one press copies it, with feedback; raw address is secondary and can be hidden | Among Us (streamer mode), Fall Guys | Address floats over scenery with no plate. |
| Chat is a toggle with an unread marker, opening in place | Among Us, Fall Guys | Current chat is a pale grey-green sheet (off palette), pushes the roster aside and has a second HISTORY view of the same text. |
| Leaving as host warns that it closes the room for everyone | Among Us, Overwatch | Escape closes the host's room today with no warning. |

### Match / pause menu

| Pattern | Seen in | Plan |
|---|---|---|
| 3 to 5 items. Resume first and focused, destructive last and set apart | Rocket League, Overwatch, Valorant, Fall Guys | Keep RESUME, SETTINGS, LEAVE MATCH. Separate LEAVE by space and colour. |
| Online games do not pause, and the menu says so; the match stays visible | Overwatch, Rocket League, Fall Guys | Keep the Live/Paused pill and the left-side card. |
| The menu button toggles: Escape again resumes | Almost every PC game | Keep (Panel close). |
| Leaving an online match asks once, naming the consequence | Rocket League (abandon), Overwatch, Fall Guys | Add. Today one press ends the session, and for a host ends the match for everyone. |
| Confirm buttons say what they do (STAY / LEAVE MATCH), never Yes/No; the safe one is focused; Escape is the safe answer | NN/g guidelines 3, 4, 6 | Applied to every confirmation below. |
| Do not confirm routine, reversible actions | NN/g guideline 2 | A joined player leaving a room is not confirmed; they can rejoin with the code. Spectators and finished/offline-practice exits are not confirmed. |

## The four answers per screen

| Screen | One thing | First press | Not needed now | Out |
|---|---|---|---|---|
| Room, host | Who is in, n/4 | START GAME | Rules, chat, spectate | Escape asks to close the room |
| Room, player | Who is in, and what we are waiting for | READY | Rules (view only), chat, spectate | Escape leaves |
| Room, spectator | Who is playing | none; the status says what happens next | Chat, take a seat | Escape leaves |
| Match menu | Is the match still running | RESUME | Settings | LEAVE MATCH, confirmed when it matters |

## Room layout (all roles share it)

```
[<] ROOM TITLE                                          [ CODE  NN99  (copy) ]
                                                         LAN  192.168.1.7:8910
+--------- PLAYERS 2/4 ----------+  +------------- MATCH --------------+
| (face) P1  YOU  crown   HOST   |  |  MAP     [<]   TONDO      [>]    |  host: arrows
| (face) P2  GUEST        READY  |  |  MODE    HERO STRIKE             |  player: no arrows
| (bot)  P3  BOT              x  |  |  NETWORK LAN                     |
| ( )    P4  OPEN SEAT  [+ BOT]  |  |  [gear  EDIT RULES / VIEW RULES] |
+--------------------------------+  +----------------------------------+
 [CHAT  *]  [WATCH INSTEAD]               status sentence  [ START GAME ]
```

- The right column holds either the MATCH card or the chat. CHAT swaps them in
  place with a short slide; the roster never moves.
- MAP uses `HubArrowChoice`, the HOST GAME selector, wired to the same
  `SelectMap` call. Players see the same field without arrows.
- Seat rows keep the whole-row seat door (`TakeSeat{n}`), the hover hint and the
  authoritative re-check. ADD BOT is a compact sticker in the open row; REMOVE BOT
  becomes a small X icon. Names `AddBot{n}` / `RemoveBot{n}` are kept.
- Row tags: HOST with crown, READY in chartreuse, BOT, OPEN SEAT with a quiet
  "Waiting for a player". The local row stays the persimmon marker.
- One primary per role, bottom right, matching CREATE LOBBY's place on HOST GAME.
- Secondary actions are one quiet row bottom left: CHAT (with an unread dot),
  WATCH INSTEAD / TAKE A SEAT, and CHARACTER only where it exists today (offline).
- The firewall notice keeps its behaviour and moves under the code card.

## Host confirmation and the match menu

- Host Escape/BACK in the room: a hub popup "Close this room?" with "Everyone in
  it goes back to the menu." Buttons KEEP ROOM (focused) and CLOSE ROOM (deep red).
- LEAVE MATCH swaps the match card's content to a confirmation in the same card:
  title "Leave the match?", a consequence line chosen by role (host: ends the
  match for everyone; client: the match goes on without you; offline: this
  match's progress is lost), STAY (focused) and LEAVE MATCH. Escape is STAY.
  Spectator broadcasts and ended matches leave at once. Training keeps its own
  panel.

## Motion and states

- Existing `HubButton` states stay the single source: lift on hover, breathing
  golden ring on pad focus, sit-down press, hatch when disabled.
- New: room panels settle in with `HubSlap` (existing), chat column slides 40
  units and fades over 0.18 s, the match card slides in from the left on open,
  the confirmation cross-fades. Reduced UI motion drops every tween.

## Scope guard

`LobbyChat` keeps its in-match path untouched (`_inMatch` branches). Only the
lobby branch's look, height and history presentation change. `PausePanel` changes
stay in its own partial files and do not touch HUD classes.
