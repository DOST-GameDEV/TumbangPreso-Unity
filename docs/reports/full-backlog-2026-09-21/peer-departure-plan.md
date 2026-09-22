# Remaining task140.5: distinguish an intentional leave from a disconnection

Source finding: NetSession.Stop notifies remote peers when the HOST closes the
room, but a departing CLIENT sends no intent. HostPeerLeft takes the same branch
for every departing client and no player-facing cause is relayed.143.9 fixes host
loss classification; it does not close this peer-departure requirement.

Chosen scope under the owner's full-backlog mandate: informational notices for
remaining players and spectators. Preserve existing transport/reconnect timing,
seat reservation, bot tier/takeover, scoring and adjudication. Never infer a forfeit,
network cause or mandatory replay from a missing packet. No new settings or modal.

Implementation order:
1. Verify the installed NGO shutdown queue behavior. Send an authenticated
   self-only leave-intent message before an orderly client shutdown; no arbitrary
   target peer, player name or outcome is accepted from the requester.
2. Store bounded session-local intent, expire/consume it, and clear it on new
   transport registration. Only the actual disconnect callback produces a notice.
3. Host derives the departed seat/name and bot replacement from its own record,
   then relays a typed, bounded notice with match/session identity. LEFT means an
   explicit intent was received; otherwise say DISCONNECTED, not TIMED OUT.
4. Use the existing compact HUD toast for all current peers/spectators, suppress
   room-wide shutdown chatter and reject duplicate/stale/foreign-origin notices.
5. Bump the protocol if new named-message contracts are added, update the exact
   protocol fixture, add focused authority/identity/cleanup coverage, and verify
   real orderly-leave versus killed-link behavior in a scoped three-peer run.
   Keep failed evidence. Stop this check after the changed behavior is proven.

Implementation now authored locally, not yet compiled/native-qualified: bounded
PeerLeaveIntents core ledger; two named protocol50 messages; self-only intent before
Shutdown(false); actual HostPeerLeft-only publication with trusted name/bot state;
match/sequence/source filtering and compact HUD toast. NGO installed source explicitly
states Shutdown(false) drains outgoing messages before teardown. All four successful
start paths now clear the old local-shutdown latch through RegisterSeatHandler.
Core intent tests passed5/5 in20ms. Need Unity compile/authority checks and the real
orderly/drop peer run. Do not mark140.5 complete before those observations.
All unrelated numbered tasks remain in todo-disposition.json.
