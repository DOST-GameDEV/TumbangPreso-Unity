# Seat Assignment Packet Bounds

The legacy NetSession seat receiver read an int without a complete-payload check.
Four truncated payloads (0..3bytes) threw OverflowException from the actual
FastBufferReader read. Two trailing payloads (5/8bytes) assigned seat2, and four
invalid seat values (-2,4,int extrema) changed LocalSlot. All10 focused baseline
cases failed before the receiver edit.

The receiver now requires exactly4 remaining bytes before reading, then accepts
only player seats0..3 or the existing spectator sentinel-1. The host-sender gate
still runs first. Invalid messages leave seating state and notifications intact.
Valid payloads, duplicate-assignment suppression, wire layout and protocol97 stay
unchanged. LobbySession already emits-1 for spectator/referee arrivals.

The final native EditMode fixture passes19/19: the10 malformed cases, all5 valid
assignments,3 non-host payloads and one repeated-assignment case. It invokes the
real receiver with native FastBufferReaders on an inactive session component;
no NetworkManager, transport or live authentication is started. This proves
receiver behavior, not real-peer transport/reconnect acceptance.

One bounded compile correction qualified Core.Balance from the networking
namespace. That first final launch ran no tests; only fresh retry XML is passing
evidence. Final listed source/fixture/meta hashes stayed unchanged after the run.
The isolated guard restored its named profile and Editor input preferences.

- [Reproduced failures](checks/seat-packet-baseline.xml)
- [Baseline inputs](checks/seat-packet-baseline-inputs.json)
- [Passing receiver cases](checks/seat-packet-final-retry.xml)
- [Final inputs and correction](checks/seat-packet-final-retry-inputs.json)
