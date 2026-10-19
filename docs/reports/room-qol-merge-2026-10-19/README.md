# The player's room merged into QoLUpdates

TODO entry: ROOM-QOL-MERGE-1019.

## What was merged

`player-room-depth-and-bots` at `380430039` (19 commits not on QoLUpdates: a room that opens with a
bot in every empty seat, ADD BOT and REMOVE BOT seat by seat carried as `CustomRules.BotSeats`, no
body for an empty seat with no bot, benched seats for a mid-match arrival, the room's depth layers,
the character select rework, HOME keeping a live room's rules) onto QoLUpdates `c3cd5fba8`.

The only conflict was `MatchArrivalPresentation.cs`: two copies of the same `[Arrival]` trace lines,
an older one on the room branch and QoLUpdates' newer one with the attackers' closing beat. QoLUpdates'
copy stands; the merged file is byte-identical to it. Every other room file merged on its own, and the
merge carries exactly the room branch's own 58 files.

## Protocol 174

The room branch appended `BotSeats` as a twelfth field of the rules wire and kept the number, because
an older parser ignores the suffix. That keeps the rules readable; it does not keep the match the same.
A merged peer builds a body only for a seat that a person or a chosen bot fills
(`MatchInstaller.SeatHasABody`, read from `CustomGameRules.SeatHasBot`); a 173 peer builds all four. In
a room with bots off, or with an OPEN seat, host and client would disagree about which bodies exist.
174 makes them refuse each other instead. `NetSession.ProtocolVersion` and the pin in
`ChatAndLobbyChromeTests` moved together. Windows and Android must be rebuilt from the same commit.

## Verification on the merged tree

- Core, `dotnet test`: 756 of 756.
- `editmode-merged.xml`: 44 of 44 (`BenchedSeatTests` 3, `ChatAndLobbyChromeTests` 10 including the
  174 pin, `HubLobbySeatControlsTests` 13, `HubPinnedRulesTests` 4, `LobbyRulesPacketTests` 6,
  `MapLaunchAndRegistryTests` 8).
- `playmode-merged.xml`: 75 cases, 66 pass. `CustomBotPolicyStartTests` 7/7, `MapVotePacketTests`
  18/18, `LateJoinReadyGateTests` 9/9, the two named `HubFlowTests` cases 2/2 (character select sizes,
  the door walk), `DirectedMapArrivalTests` 18/20, `MatchArrivalFlowTests` 8/10 (including
  `CustomRoomWithEveryBotRemovedStartsWithOnlyTheHostsSeatInTheMatch`), `ModelPreviewTests` 9/14.
- All nine failures are QoLUpdates' own, not the merge's:
  - `playmode-qol-baseline.xml`, the same seven non-sole cases on pure QoLUpdates `c3cd5fba8`: six
    fail with identical messages. Two `MatchArrivalFlowTests` cases still expect the vote to land on
    `BayanPlaza`, which left the pool on 2026-10-18; three `ModelPreviewTests` cases still expect the
    retired Classic and Dante rigs (`character-male-f`, `team-dante`) where QoLUpdates ships the
    redesigns; two find `forearm-left` motionless.
  - The seventh, `HeroCharacterSelectShowsAbilitiesInsteadOfClassicAttributes`, writes into
    `Logs/shots-preview`, which only `TheCharacterPreviewIsFramedPosedAndMovable` creates, later in the
    alphabetical run. In a fresh checkout it fails first time and passes after:
    `playmode-merged-hero-rerun.xml`, 1 of 1 on the merged tree.
  - The two `DirectedMapArrivalTests` sole-height cases are DIRECTED-ARRIVAL-SOLE-1019, identical on
    every baseline.

## Found, not changed here

`MatchRpc.RequestSnapshotWhenArenaReady` asks the host for the world only once
`round.Players.Count >= Balance.PlayerCount`. With the room's empty seats that count is never reached,
so a client in such a room never sends its snapshot request. A normal start lives on the host's
broadcasts; a cold join or a rejoin into such a room loses the rehydration that request exists for.

## Limits

No two-peer native run of the merged build yet (host and client building the same seats is the room's
own open item), no Android player, no human pass.
