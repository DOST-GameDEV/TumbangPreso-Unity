# Validate the entire reconnect seat packet before changing ownership

OnRebindSeatMsg previously decoded unchecked integers/boolean/string and called
ApplyAssignedSeat. Original41278: four native causal failures, incomplete frame
throws, invalid seat becomes local-2, invalid defender and nonboolean data change
local1 to2. Valid player/spectator/free-roam sentinel/duplicate and sender/loopback
controls both pass.

Candidate validates the complete9-byte fixed header, canonical byteboolean,
seat and defender ranges(-1..3), and complete bounded UTF-16 name before decoding
or applying any ownership change. Keep-1 spectator/pre-round defender, valid
rebind roles/camera/input/name behavior and idempotency. No protocol/schema change.

Candidate50085 FIRST6/6, same assertions/fixture, no repairs/retries. Post9971:
all18447 protected qualification assets unchanged, exact3 MAIN/q matches.
Named profiles/shared input preserved, leases released. Dormant actual NetSession
and actual callback, Round=null; this proves ownership/event boundaries rather
than whole live-body/control restoration. Current1003d build/fullHero run predates
this patch. No crafted live-peer fault injection or whole readiness claim.
