# Seat requests and retained readiness

F0930-36, candidate9127a54b9 plus the seat-request guards and duplicate handling.

## Fix

OnReqSeatMsg now requires exactly four readable remaining bytes and a transport
ID representable by the lobby's integer peer IDs. Previously short requests threw,
trailing requests moved seats, and IDs4294967296/4294967297 wrapped into the host
or guest ID and moved that other peer's seat.

HostAssignSeat now recognizes an unchanged seat/spectator assignment. It retains
Ready and uses the existing seating acknowledgement route. Previously requesting
the current seat removed the guest's Ready declaration and broadcast a changed
tally. Actual seat changes still clear only that peer's Ready declaration.
Existing occupied-seat, invalid-seat, live-match and host authority gates remain.
Packet format and protocol are unchanged. No hero, loading or presentation edits.

## Evidence

Unity6000.5.8f1 Windows native EditMode, isolated named profile
feedback-0930-seat-request. Actual MatchRpc receiver, FastBufferReader and
LobbySession with admitted host/guest and a declared-ready fixture state:

- Baseline17cases:9failed,8passed. Four short payloads throw, two trailing
  payloads change seats, two wide IDs move another peer, and a duplicate drops Ready.
- Fixed17cases:17passed. Six malformed lengths, three wide IDs, duplicate
  requests, two actual changes, two invalid seats, occupied seat, live match and
  non-host handling.
- 546 frozen overlay inputs, unrelated main dirt excluded, no non-metadata drift.

Raw XML/manifests are in checks/seat-request; full logs remain in the isolated
checkout's Logs/feedback-0930. The fixture checks receiver/lobby state and tally
events. It does not send the seating acknowledgement over a real transport or
qualify live matchmaking. That outgoing route is retained in source.
