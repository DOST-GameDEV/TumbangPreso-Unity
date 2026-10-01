# Atomic bounded roster synchronization

SyncLobbyPicks previously allocated from an unchecked count and replaced seats
while decoding. A malformed later row left earlier seats changed; invalid footer
counts or trailing bytes could publish an inconsistent roster.

The receiver now checks the complete four-seat layout before decoding: unique
valid seat IDs, Boolean flags, occupied-peer consistency, bounded UTF-16 fields
and an optional spectator count within the existing gallery limit. It decodes
into a four-entry temporary roster and commits all seats together. Host-decided
appearance/build strings remain unchanged; the existing omitted-footer behavior
retains the spectator count. Authority and listen-host loopback guards remain.

Native baseline12/12fails on truncations, counts-1/0/5, duplicate seat, invalid
spectator footer and trailing byte, with exceptions or state/event mutations.
An intentionally enormous allocation was not launched. First final14/16passes;
two valid footer cases expose a fixture event counter carried between tests.
One bounded fixture reset repair reruns only the three valid cases, which pass.
Sixteen distinct successes across two runs, not a claimed16/16single run.

Runtime code is identical across those final runs. Final692input hashes have
no drift and all owned files match the native candidate on source28b22f31f plus
overlays. New32hex script metadata imports natively. The first final manifest was
overwritten during preparation; its reconstructed fixture hash is explicitly
labelled in the receipts, and raw first XML is preserved. No hidden green claim.
Jobs15863/57204/40658terminal; profiles/input preferences preserved.
[Receipts](roster-framing-checks).

This qualifies local real receiver/host-decision/Unicode/legacy/authority behavior,
not a new player or actual-peer roster journey. No field/protocol119, hero-kit,
loading or authored asset change. Earlier117reconnect/116Haunt proof remains
its exact older scope.
