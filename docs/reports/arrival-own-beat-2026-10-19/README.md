# Attackers close the match opening on themselves

TODO entry: ARRIVAL-OWN-BEAT-1019. Branch work on ASTRAReworks `1735f1d4b` (protocol 172).

## The report and what it turned out to be

Report, 2026-10-19, from a room-branch online test: the start-of-match cutscene "plays for the host
but joining players doesnt play a cutscene". The joiners saw their own view, on a non-Arena map.

The opening had no peer-visible trace, so one `[Arrival] host|client|offline:` log line now marks
every state change of `ReadyGate`, `MatchArrivalPresentation` and `Map.ArenaIntro` (gate opened,
prepared, rolling or playing, cut, cancelled, ran out, how long it was on screen, and why an Arena
opening refused). With it, real host and joiner players were run on one machine, each in a fresh
named profile, through the real hub lobby START (`peer-traces.txt`):

| Run | Build | Link | Map, arrival | Joiner's opening |
|---|---|---|---|---|
| lan-arena-1 | ASTRAReworks + trace | LAN | Arena, stadium opening | full 24.1 s |
| online-arena-1 | same | Relay | Arena | full 24.1 s |
| online-arena-2 | same | Relay | Arena, window captures | 22.9 s on a 24.1 s film (its shared clock ran 1.2 s fast); on screen in every capture |
| online-Eskinita-1 | same | Relay | Tondo, generic | full 8.6 s |
| online-Kanto-1 | same | Relay | Kanto | no match: the client's Relay join timed out before a seat |
| online-Kanto-2 | same | Relay | Kanto, directed | full 11.0 s |
| online-arena-slow-1 | same | Relay | Arena, joiner on one core at idle priority | full 24.1 s |
| room-nobots-1 | room branch + trace | Relay | Tondo, two empty seats with no body | full 8.6 s |
| room-bots-1 | room branch + trace | Relay | Tondo, bots | full 8.6 s |

In every pair the host's countdown waited for the joiner's own post-film ready vote
(`peer 1 ready (2/2)`, then `countdown sent with 2/2 ready`). `arena-relay-before-change.jpg` is the
host (left) and joiner (right) mid-film in online-arena-2. The room runs needed a local-only
`-tp-room-seats` switch, never committed, because `NetBootstrap.Requested` turns the room's seat
logic off for command-line sessions.

So the film was not failing to play on joiners. Asked again, the reporter put it precisely: "only p1
at the start of the game has a cutscene right before the game starts. incorporate also an
appropriate simple cutscene for other non-taya players right before it starts". On the directed
courts (Kanto, Ilalim, Lagoon) the cast portraits end on round 1's taya and the taya then gets the
spotlight until the camera returns. Round 1's taya is seat 0, usually the host, so on every screen
the last shots before play were P1's.

## The change

After the taya's spotlight, an attacker's own screen cuts (with the same breath of ink as every other
cut) to their own character for 1.6 s: framed from their renderer bounds, a slow push-in, the
featured greeting pose, and the caption "YOU · ATTACKER" under the hero's name. The camera's return
to their own view starts from that shot. The taya's screen and a watcher's keep the spotlight for the
same 1.6 s, so every peer's film has the same length (Kanto 11.0 s became 12.0 s; 1.0 s more on every
directed court) and nobody waits at the countdown for somebody else's extra shot.

- `ReadyGate` hands the arrival the body it was opened for (`MatchArrivalPresentation.Viewer`); the
  arrival keeps that seat only for a seated player who is handed back to their own view.
- Presentation only. Each peer already knows its own seat, so nothing travels: no wire change, and
  protocol 172 is unchanged. The opening already holds the clock and input on every device, so
  keyboard, pad and touch see no new control or exit path.
- Reduced UI motion keeps its single still shot and names this player last; with cinematic camera
  motion off the camera stays at the player's view, as before.
- Tondo's generic arrival and the Arena's stadium opening are unchanged (see the TODO entry).

### Framing: four renders were rejected before this one

`closing-beat-renders.jpg` rows are Kanto seat 3, Ilalim seat 1 and Lagoon seat 2 as attackers, then
Kanto seat 0 as the taya; columns are the taya's spotlight just before the cut, the middle of the own
beat, and the hold after it.

1. Re-using the cast portrait and pushing in along it read as three attackers standing shoulder to
   shoulder, not as "you".
2. Pushing further centred the frame on the road behind them. `PrepareShots` puts each portrait's focus
   1.86 to 2.04 m above its seat's mark on Ilalim, above these characters' heads.
3. A baked-mesh measurement then cut the player off at the neck, while the crop check passed.
4. Facing the player straight on still left a neighbour looking bigger at the frame's edge.

The cause of 2 and 3 is measured (`KantoAttackerClosesOnThemselves`, `measures.txt` in the local run):
the hero model is scaled 2.38. `Renderer.bounds` gives the drawn body 1.76 m tall beside a 1.6 m
capsule; `BakeMesh(useScale: true)` placed without scale gives 0.88 m (the tests' `RenderedPoints`
helper); `BakeMesh` without scale then `TransformPoint` gives 3.9 m (`DrawnBounds`, the scale applied
twice). The closing beat fills 82 per cent of the frame height with the renderer bounds through a
40 degree lens, choosing among yaws within 60 degrees of the player's facing the one that walls and
other bodies (weighted by how big they would be drawn) intrude on least. Its test checks all eight
corners of the renderer bounds are in frame and that the player spans at least half its height.

## Verification

- `directed-final.xml`: `DirectedMapArrivalTests`, 20 cases, 18 pass. New: `Bridge`, `Cove` and
  `KantoAttackerClosesOnThemselves`, `KantoTayaKeepsTheSpotlight`, `KantoWatcherKeepsTheSpotlight`. The
  three natural Hero countdowns play as seat 1 through the real `ReadyGate` and now also assert the
  attacker's own beat appeared. The two failures are `BridgeLowArrival` and `KantoCrossingArrival`,
  seat 0's rendered sole 0.04295 m and 0.04259 m above the floor against a 0.04 m bound at 75 per cent
  of the walk-in, before the spotlight.
- `baseline-sole.xml`: those two cases fail with identical values on the unmodified `aad16105c`
  (ASTRAReworks plus the trace), so they predate this change: DIRECTED-ARRIVAL-SOLE-1019.
- `ready-and-handoff-final.xml`: `ArrivalHandoffTests` 8, `LateJoinReadyGateTests` 9 and
  `MatchStartContractTests` 6, all 23 pass on the final source.
- `merged-qolupdates.xml`: after merging into QoLUpdates `e33323e13` (protocol 173, Payo), the same
  four fixtures give 41/43 with the same two sole failures and identical values.
- Real players, Relay, Kanto, the built player of this source (`kanto-relay-host-vs-joiner.jpg`, host
  left, joiner right, 6, 9, 10 and 11 s into the film): the host as taya keeps "DANTE · PLAYER 1 ·
  TAYA"; the joiner (seat 1) cuts to "YASMIN · YOU · ATTACKER"; both return to their own views and the
  traces read `closes on the taya` and `closes on this player, seat 1`, 12.0 s each.

## Found on the way, not changed here

- Every Arena round, on host and client, throws
  `Scenery texture identity was not prepared: arena_stage_props` once per frame from the local replay
  scenery sampler: ARENA-SCENERY-IDENTITY-1019.
- online-Kanto-1: the joiner's Relay join timed out ("Connection timed out before a seat was
  assigned") on a fresh allocation; the next attempt joined. Recorded for the open Relay timeout report
  (OWNER-MULTIPLAYER-FEEDBACK-1004).
- After its countdown begins, a client often logs `adopting the host's running match; the opening is
  retired`: the host's first in-progress snapshot can arrive within the client's own 3.5 s count, and
  the late-join adoption then ends the client's count a moment early. The round state is the host's
  either way; the visible cost is at most the end of "GO!".

## Limits

Two players on one machine, LAN loopback and Relay; no WAN pair between two machines, no Android
player, no human playtest. The framing was judged from renders and two window captures, not in play.
