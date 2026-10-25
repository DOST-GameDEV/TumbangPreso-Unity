# Network reliability candidate

Work stays on `NETWORKFIX`. Spectator integration is on main; mobile follows
network acceptance. Current changes preserve gameplay protocol 183 and existing
movement, cast and score packet layouts.

## Behavior

- Losing the host retires the old match and opens an acknowledged Home notice.
  An unexpected seatless-server stop now uses the same authority latch and exit
  route. It cannot inherit offline authority over its abandoned match.
- A small connection badge follows the scoreboard: yellow for sustained mild
  trouble, red for severe trouble, hidden when healthy. Existing 200/450 ms RTT
  thresholds remain. Escalation/recovery settle times prevent flicker.
- A bounded optional 17-byte request/echo measures fresh round trips at 1 Hz per
  admitted peer. Transport RTT could stay stale while an idle client received
  unreliable state. Replies bind local outstanding serials and a session nonce;
  invalid, replayed and expired replies do not update the measurement. The
  unreliable probe cannot drive gameplay. Legacy peers ignore this channel and
  retain transport-RTT fallback. Host quality means its worst peer, not a diagnosis
  of the host's physical Wi-Fi.
- Actual client receive silence and missed known probe replies also warn before
  the unchanged eight-second disconnect timeout. A short outage can recover
  without abandoning a healthy session.
- Player departure uses the existing authoritative notification. Spectators get
  a compact right-rail card with a live pause binding and twelve seconds of
  visible reaction time. Hidden HUDs and covering menus retain it. It clears
  after an operator pause; a departure does not pause the game automatically.
- Recent disconnected spectators retain their audience role instead of taking
  a free player chair on reconnect. The per-match history is bounded to 32 recent
  identities and clears at room/match boundaries. Player reservations remain
  separate. Replacement connections can reuse their own identity at capacity;
  a reserved player chair cannot admit a fifth spectator.
- Valid authentication profile names keep their exact identity. Invalid SDK
  names receive a stable bounded name; local save paths and LAN tokens retain
  their original profile. Permanent invalid-profile failures do not retry as
  transient network outages.
- The unit-pose writer now allocates the four bytes it already writes for SEARED.
  A maximum valid aim identity previously overflowed that buffer. The complete
  317-byte packet now reaches the actual named-message callback; its wire layout
  is unchanged.

## Evidence so far

| Check | Result and limit |
|---|---|
| Core after shutdown classification change | 840/840, `Logs/networkfix-1025/core/network-session-1025.trx` |
| Native seating/rejoin/admission/notice/quality rules | 135/135, `role-edit.xml` |
| Native connection UI, reaction window and Home action | 6/6, `layout-play-b.xml`; 960x540 and 1600x680 renders inspected |
| Real host leave and process loss | Player plus actual spectator retired the old round, fabricated no result, returned Home and retained the notice; earlier runtime `313f7ad70c0ec3fabcf447122e512bfec246b4fa83b5b0784ab492bd380d1086` |
| Shaped-link delivery and cold restart | Full `networkfix-link-rejoin-1025-04` passed on source `ceb7936060`: yellow/red, stable recovery, five-second outage, actual arena/body restoration in original seat and match |
| Same-process rejoin | `networkfix-outage-rejoin-1025-02` passed on current source: eleven-second real outage, retired old games, ordinary Hub rejoin in the same processes, original player seat and spectator role, actual arena and same match. The failed promotion baseline is retained |
| Unexpected seatless-server stop | `networkfix-dedicated-shutdown-1025-03` passed on current source: referee and both clients retired, Home notices, no manufactured results. The baseline that kept awarding points is retained |
| Hosted lag, departure and operator action | `networkfix-link-rejoin-1025-05` passed: actual card captured, no automatic pause, spectator pause/resume reached every survivor, cold restart restored the arena/body/seat |
| Seatless referee lag and controls | `networkfix-dedicated-link-1026-02` passed after correcting the launch-flag guard: actual referee pause/resume reached all survivors, lag tiers/recovery and cold rejoin passed |
| Slipper windup, spin and rejoin | Hero three-peer trace passed with 75 ms each-way delay, straight/negative/positive spin, visible matching hold states, release and 120 rejoined active samples |
| Recall visibility | Five actual Hero peers, including a real spectator, passed the recall-view ownership check |
| Native current gameplay/recovery | Initial broad run: 75/86. Focused fixture/protocol follow-ups passed 8/11, then the remaining 3/3. Receipts overlap; this is not a fresh all-green 86-case run |

Focused suites overlap. Logs above live in the isolated worktree or canonical
checkout's scoped Logs directories; they are not source assets. UI review images
use actual captured court imagery with staged HUD state, separate from peer proof.
Connection/lifecycle/UI source `3a99849f1` uses runtime SHA-256
`5764006e7d728dd9581c554607afcc7603da9c816942274f200c64d06299b86a`.
The referee-control follow-up `3d9a713c0` uses runtime SHA-256
`0eae6435017c65b86ec3be45101eaae0a0c9bd702237e33f377f479a65d43886`;
its three native control/permission checks also pass. The actual peer pictures
show the right-side notice and its pause prompt, and the small red badge beside
the scoreboard. Native source/capture hashes are [recorded here](native-validation.json).

All peer evidence so far is controlled Windows processes on one PC. It is not
natural human play, separate-machine LAN, Relay/WAN or Android acceptance.
The requested Windows connection/recovery candidate is qualified within these
controlled checks. Mobile is the next assignment. Keep failed evidence and
qualify every result by its actual source/runtime; these checks do not guarantee
every external network or device condition.

The broader run exposed stale fixture assumptions: a disabled map, retired KURO
Haunt/free-recall/Slide behavior, an immediate countdown tick before the existing
HUD reveal, a lost deferred-effect receipt, and an absolute score expectation
that omitted current bonus awards. Follow-ups retain the relevant invariants:
current arena/body wiring, inert retired endpoints, full scoped countdown,
non-mutating eligibility/resource checks, every outstanding receipt retired on
reset, and complete companion award attribution. Paete recovery now compares
the captured clock with measured frame age at a tighter tolerance. Authored kits
and balance were preserved.

The updated current-ultimate check passed on player source `45a4f00e1`, runtime
`fecf6717e8232692c979fbc219504cb2b12b65be2e289dd4f45f140bc012a362`:
shared cohort of two, 5.9669-second hold, identical frozen clock and no duplicate
effects. Both dolls and their own slippers arrived on every peer. The late joiner
recorded 32 complete body/shoe samples without replaying the introduction; all
time-matched companion position errors were zero. The fixture waits for the
actual owner seat before launching a competing peer.

The round-boundary rejoin also passed under 150 ms each-way delay, with trusted
admission distinguished from a listening socket. Host/client recorded 33/34
intermission samples and 74/44 sustained second-round samples, no live warmup,
one host-owned intermission event and the original client seat in the same
process. Source `90acb83ea` adds only the fixture admission wait to that player.
Failed legacy fixture receipts remain preserved.

This branch was tested against main's spectator/Zack integration through
`f97da3be6`. Main's later Backtrack visual refinement is separate and has not been
overwritten. Network changes remain unmerged until the owner requests integration.
