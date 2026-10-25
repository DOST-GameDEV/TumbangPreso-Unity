# Networking

Read the [authoring contract](SKILL_NETWORK_CONTRACT.md) before changing match
delivery. Live compatibility is `NetSession.ProtocolVersion` and
`SkillContractFingerprint`; branch names and old report protocol numbers are
not compatibility identities. Current work belongs in [TODO](TODO.md).

## Ownership and source map

| Responsibility | Owner |
|---|---|
| Seat reservation, reconnect, stable identity | `Net/LobbySession.cs`; identity never grants control by itself |
| Transport start/stop, approval, callback lifetime | `Net/NetSession.cs`, `JoinAttemptGate`, `NetBootstrapRunner` |
| LAN discovery / online listing | `LanBeacon`, `ServerQuery`, `Matchmaker`; UGS Lobby/Relay peer hosting |
| Gameplay requests and snapshots | `MatchRpc.cs` and partials, `GameplayActionScope` |
| Movement/prediction | `MatchRpc.Movement`, CharacterMotor; accepted owner simulation and host validation |
| Match clock / pause | `MatchClockMessage`, `MatchRpc.Clock`; shared host clock, ordered recovery |
| Abilities and remote presentation | HeroAbilitySystem, SkillCastMessage, typed world/timed/familiar/ghost state |
| Peer departure and returning bodies | `MatchRpc.PeerDeparture`, LobbySession, reconnect hydration |
| Results/rematch/replays | Existing result identity, seat membership, replay and queue-arrival routes |

Host owns outcomes and score. Validate sender, current seat control, match/round,
movement epoch, serial order, finite values and exact payload bounds before
mutation. Preserve ranked device pools and existing casual/custom rules.

## LAN and VPN

Game UDP defaults to 8910; discovery uses 8911. Beacon v3 carries protocol/map,
retains v1/v2 reading and emits the older compatible beacon as required. A
version mismatch must remain a clear refusal. Prefer the existing address ranking
when the same room is heard on LAN and VPN adapters.

A visible beacon does not prove the game port is reachable. The host's firewall,
VPN adapter and program path matter. Use the existing LanFirewall UI/installer
route and readable failure; do not silently alter OS security during a test.
Never diagnose a game-code defect solely from a third-party firewall timeout.

## Reconnect and replicated presentation

- Retire stale callbacks/cursors when a transport ends. A late completion cannot
  resurrect an old join attempt or shut down a new session.
- Reclaim only the accepted identity/reservation and current seat ownership.
  Restore the current match/round/body state, not old local selections.
- Hydrate durable state without replaying casts, hits, score or expired effects.
  Age simulation clocks from host round progress; holds/pauses do not age them.
- Observers/spectators receive accepted bodies, aim, held/loose slippers, can
  carry, fields, ghost/companion states, ultimate phases and cleanup.
- Do not replace durable state with a one-shot cosmetic event or introduce
  character-specific RPCs where a shared typed contract already exists.

## Validation

Use scoped native tests plus actual host/owner/observer processes for behavior.
Exercise join, departure, reconnect, late arrival, round transition, repeated
shutdown and relevant latency/loss. Label same-PC peers accurately; Relay,
separate machines and Android require their own evidence.
Existing `tools/net_*.py`, wire audits and test fixtures are starting points.
[Historical guide](archive/cleanup-2026-10-25/docs/NETWORKING.md) retains packet
reasoning and exact dated peer receipts; it does not qualify current source.
