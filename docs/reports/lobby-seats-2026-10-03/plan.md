# Lobby seat selection and consensual swaps

Owner clarification: choosing a bot seat switches immediately. Choosing an
occupied human seat sends that player a small Yes/No request. Exchange both
seats only if the recipient accepts. No unilateral host movement of people.

Restore existing vacant-seat ReqSeat authority, then extend occupied-seat
requests with host-issued IDs, participant/seat snapshots and expiry. Only the
transport-authenticated recipient can accept. Refuse stale, duplicate, spoofed,
disconnected, in-progress or queued requests; revalidate and exchange atomically.
Clear both ready states and broadcast resulting seating and roster. Preserve
characters and all unrelated room/rules flows. One pending request per person.

Qualify host rules and actual UI request/accept/decline controls separately in
bounded native runs. Actual multiplayer peers and physical-device acceptance
remain separate. This is an implementation plan, not shipped functionality.
