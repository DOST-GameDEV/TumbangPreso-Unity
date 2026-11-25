# MATCH-END-1126: the final whistle, rejoin and grounded openings

Owner handoff, 2026-11-25 and 2026-11-26. The match ending felt abrupt, bodies could be left in the
air under the result board, things in the opening films floated, and rejoining had to be safe.
Windows only: no macOS player this time.

## The final whistle

1. `MatchDirector.ReportMatchEnded` locks the result first, with the existing scoring and tie rules
   (a tie at the top is a draw, -1). Then it starts `MatchEndBeat` and raises `MatchEnded`.
2. `MatchEndBeat` cancels any ultimate hold, halftime break or hitstop, and parks every seat
   (`CharacterMotor.FreezeForMatchEnd`). It shows MATCH OVER in the TUMP readout over the live court,
   with the existing `round_end` cue.
3. Bodies in the air come down under their own physics. Vertical speed may only fall, and flight,
   carries, vine pulls and swings end. At 1.45 s the silent leaving band (`ReplayStinger.CoverLeaving`)
   crosses. Under it, the host moves anyone still unsettled to a safe map spot through the
   authoritative teleport (`CharacterMotor.CorrectUnrestedForMatchEnd`).
4. At 2.0 s the existing board is revealed once (`MatchEndBeat.Revealed`, then `MatchResult.RevealBoard`).
   Standings, result music, progression, rematch, next map and return all work as before. Single
   player now really stops its clock under the board. Before, the runner's own scale request at the
   whistle overrode the board's pause.

## The same ending for everyone

- The host stamps the beat's start on the shared clock (`SharedUltimatePhase.Now`) and sends `MatchEnd`
  (match id, final round, start) right after the terminal snapshot, on the same reliable stream.
  Protocol 194.
- A client times its beat from the host's stamp. A late or rejoining peer sees only what is left of
  it, or the board at once. A client whose terminal snapshot is late keeps MATCH OVER up until the
  snapshot lands.
- The message is refused for another match, a round out of range, or a start that is not finite or
  is in the future. Duplicates and resends change nothing. A rematch, a new match, a scene change or a
  transport change clears the beat.

## Rejoin

When this machine's link to the room drops (`SessionEndCause.HostLost`), CONNECTION LOST offers
REJOIN to the same room, by room code or LAN address, and BACK TO HOME. REJOIN is the ordinary join
with the same durable token, so the host gives back the held chair, with its score, picks and
spectator flag.

- A refusal is shown on the notice, and REJOIN stays available.
- BACK, or BACK TO HOME, during the join abandons it.
- A spectator comes back as a spectator.
- REJOIN is not offered for a removal, a full room, another version or a replaced session.
- The target is forgotten when any new session starts, so an old room is never offered for a later
  drop.

## Openings

`OpeningGroundingTests` plays each court's real opening and measures every drawn frame. The worst
case was Tondo. Two of Eskinita's walls stand behind a strip of ground 0.65 m below the court.
`MapFilm.Floor` found no floor within half a metre there and fell back to the mark's height, so
the door set, its step and the kid stepping out were built 0.7 to 0.9 m in the air.
`TondoFilm.FindDoor` now only accepts a door whose sill (`MapFilm.TryFloor`) and whole way to the
ring are on real floor, level with the ring. Other doors, or the court's own edge, are used instead.

Banaue had the same fault on its raised layouts. On `hagdan` (the can 1.2 m up), the file walked in at
the plaza's height, 0.5 to 1 m over the lower terraces. `BanaueFilm.TraceGround` now traces the real
floor back from each spot, following the stair's slope. Where a block's edge breaks the way, the walk
starts after it. The test plays Banaue's opening in each of its five layouts.

The other films' raised things are meant to be up: washing lines, lamps, the train, the billboard,
the Arena's drones, the banca and its riders on the water, the kids on the stilt-house decks, the
can held in a hand, the jeepney's back step, the bicycle, and the jumping cheers (about 0.36 m for
about 0.3 s at a time).

## Evidence

Evidence for this change lives under `Logs/match-end-20261126/`, from exact tests and built players
only.

| Run | Cases | Result |
|---|---|---|
| `play1` | The 9 affected PlayMode cases | 2 passed, 7 failed: a counter shared across cases, a first-knockdown bonus, a raycast on the board's first frame, and the Tondo doors |
| `play2` | The 7 that failed | 5 passed; the opening and the native result case failed |
| `play4` | The opening on all five Banaue layouts | Failed: the `hagdan` walk-in, 0.5 to 1.0 m up |
| `play5` | The opening, after the Banaue fix | 1/1 passed: six courts, five Banaue layouts |
| `edit1` | CompletedMatchArrivalTests, protocol pins, three CONNECTION LOST cases | 20/20 passed |

Victory input (2/2, `play1`) and all five `MatchEndBeatTests` (`play2`) pass.
`TumpNativeResultTests.RealMatchEndParksTouchAndLeavesOverlayResultActionsHittable` now waits out
the beat, and its board actions are hittable. It still fails its last check, which looks for live
HUD layers under the board's own canvas. The board has been a root canvas since 9e1aac96a
(Nov 9), so that check cannot pass; this change does not touch it.

Source audits: the findings that name files this change touched are rows unchanged since HEAD.
These are allowlist rows, cue rows and anonymous subscriptions. The new `MatchEnd` message reads
"no writer in this file", as `MatchBreak` and `MatchMoment` do, because its writer is in a partial.

Built-player runs (`tools/net_match_end.py`, `tools/net_resilience.py`) are recorded in the PR.
