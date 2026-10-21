# Hosted room publication order

On source bd6cadfd5, public-room updates could run concurrently. Delaying the
first count update while two later updates completed left the published occupied
count at2 after the room had reached4. This is a reproducible stale-advertisement
defect. It does not establish the cause of the owner's Relay timeout or host kick.

The focused original native run (Unity6000.5.8f1, EditMode, isolated named profile,
PID8652) failed the new publication-order case and passed the seven existing
creation/deletion lifetime cases. The fixture uses the existing service dispatch
seams and makes no UGS requests. Logs and XML are retained under
`Logs/hosted-publication-order1005/original-native`.

The candidate keeps one publication writer per hosted-lobby lifetime. While a
request is outstanding, queued changes replace the pending full advertisement
with the latest counts, match state, map, visibility and matchmaking fields.
Retiring a room clears its pending writer ownership. An already-issued request
may finish against its old ID but cannot publish queued counts into a new room.
A failed update reports its existing diagnostic and permits the newer pending
state to drain. Heartbeat, transport, identity and protocol remain unchanged.

Candidate native PID19604 exited0 with all ten cases passing. The delayed burst
publishes the latest4-seat/in-progress state with one concurrent request maximum
and two total requests instead of three. Replacement publication starts while
the old room's request remains outstanding; completing that request neither
drains its retired queued counts nor alters the new room. A failed older update
still permits the newest counts to publish. All seven prior lifetime controls pass.
Both runs restore shared input/editor preferences, QualitySettings and the named
profile. Each run's202 generated UI metadata changes are retained then restored
to their exact pre-run bytes; no frozen-source differences remain.
Raw result summaries, XML and cleanup receipts accompany this report. These
service-dispatch checks establish ordering/ownership and request coalescing;
they do not establish live service limits, WAN timing or a whole multiplayer pass.
The owner's real QA timeout/host loss remains open regardless of these local checks.
