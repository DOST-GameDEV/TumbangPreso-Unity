# Current checkpoint

MATCH-END-1126 is on branch `match-end-and-rejoin-20261126`, with a PR against main.
[Report, evidence and limits](reports/match-end-2026-11-26/README.md).

- Final whistle: `MatchEndBeat` shows MATCH OVER over the live court for 2 s from the host's start
  (`MatchEnd` message, protocol 194). It parks and settles the cast, then reveals the existing board
  once. Single player now really pauses under the board.
- Rejoin: after a dropped link, the CONNECTION LOST notice offers REJOIN to the same room (code or
  address) or BACK TO HOME. Refusal, cancel and spectator rejoins are handled.
- Openings: Tondo's doors, and the kids stepping out of them, are only built on floor level with the
  ring (`TondoFilm.FindDoor`, `MapFilm.TryFloor`). Banaue's file follows the real ground on every
  layout (`BanaueFilm.TraceGround`). `OpeningGroundingTests` measures all six courts.
- Built-player harness: `tools/net_match_end.py`. It covers six courts offline, then host, a delayed
  client and a spectator, with a link cut and REJOIN.
- Open: owner play of the beat's feel, two-PC peers and Android.

Standing rules:
- Keep the canonical checkout on main. No reset, clean or force push. Exact tests only.
- Development players go outside `Builds/Windows` only temporarily. Restore the four build-touched
  settings files after a direct build.

Still open from before: CONTROLLER-NATIVE-1107 hardware acceptance, and BANAUE-PERF-1105's next
action (prewarm each picked hero's `HeroIntroductionScene`).
[Previous checkpoint](reports/match-end-2026-11-26/checkpoint-before.md).
