# Peer departures: completed task140.5

Remaining players and spectators now see a compact host-confirmed notice with the
stable seat number, display name, LEFT or DISCONNECTED, and bot takeover/reserved
seat status. Only an explicit authenticated self-leave intent can produce LEFT;
a silent link loss is not labelled a forfeit, timeout cause or mandatory replay.
Seat reservation, transport timeout, bot tier, scoring and adjudication are unchanged.
Protocol50 carries the two messages. Session/match identity, one-use intent,
12-second expiry, bounded names and host-origin/sequence checks prevent stale or
invented notices. A successful transport start clears the old shutdown latch.

Evidence:
- Core intent lifecycle5/5,20ms.
- Unity receiver/protocol/envelope4/4,.131s. Native work then added the visible
  stable seat prefix; the actual same-packet peer runs below prove that text.
- Native v54 orderly Classic: four players plus an actual fifth-join spectator.
  The leaver called real NetSession.Stop; all four survivors displayed one matching
  LEFT notice, and the host's existing bot takeover was observed.
- Native v54 abrupt Hero: only the test-owned leaver process was terminated.
  All four survivors displayed one matching DISCONNECTED notice with bot takeover.
- Actual host/observer/spectator HUD PNGs are retained; host and spectator frames
  were inspected. Profile files and shared input preferences were preserved.

Failure history is retained. v53-batchmode had no player backbuffer. Removing that
flag while retaining hidden owned windows fixed capture. v53b then exposed the
real reader-envelope defect: NGO consumes its8-byte name hash in the same reader,
so Length includes bytes before Position. The intent guard now checks the unread
payload; the new regression reproduces that envelope. No assertion was weakened.

Build v54 is an internal1211MB Windows player (62s,guard8c5a0b8b5aa8), based on
7c028dda plus the recorded framing/text/capture changes. Its stamp honestly says
dirty because authoring/import output differed; it is not a clean release claim.
The runtime hash and per-peer result/viewport data live in peer-departure-evidence.
This is local process evidence, not WAN/human/device certification. Full candidate
qualification and the rest of the TODO remain active.
