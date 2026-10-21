# Join success waits for room admission

The native join panel closed and raised Joined after StartClientAsync returned.
An unanswered local address therefore looked successful before a host connected
or assigned a seat. Direct address, resolved LAN code and Relay paths now await
the existing connection/seat admission gate before publishing success. Existing
cancellation/attempt ownership and failure reason reporting remain in place.

Native PlayMode baseline reproduces both failures (0/2): unanswered real local
UDP transport reports success; started transport completes before connection/seat.
Final admission and existing cancellation cases pass2/2. The unavailable-host
case initially failed on Unity Transport's expected error log; the ONE fixture
repair explicitly expects that log, then only this case reruns and passes1/1.
All three distinct acceptance cases now pass; first failed XML is retained.
No runtime change was made in the fixture repair, and other cases were not rerun.

Actual local client transport is used in both new cases. The successful control
supplies independent connection and seat notifications after startup; it proves
the UI admission boundary, not a real peer connection or Relay/WAN service.
The unavailable-host case waits for actual transport failure and verifies open
retry controls, no Joined event and retired listening transport. Profile files
and shared input preferences were restored by the guarded runner each time.

TumpHub's automatic lobby transition needs its own client admission gate. Astra's
separate friend-join unit covers that integration; this narrow panel acceptance
does not establish the whole operator flow or competition qualification.
Original Hero reconnect failure is preserved separately because its probe
bypasses the panel and entered the arena before admission.
