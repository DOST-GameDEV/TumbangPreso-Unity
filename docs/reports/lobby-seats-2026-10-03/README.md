# Lobby positions with recipient consent

Owner clarification: a bot slot switches immediately. A human slot asks the
other player with a small Yes/No popup; only Yes exchanges the two seats.

The current HubLobby now exposes SIT HERE on open/bot rows and SWITCH on other
human rows. Own-seat actions stay absent. Controls recheck room state at the
press. Incoming requests use the existing hub popup/focus/back path, with No as
the first focused answer; Back declines. The requester sees a waiting message.

The host derives requester and respondent from transport identity. Pending
requests bind both live PeerRecord identities and their exact seats, with one
request per participant and a20-second expiry. Accept rechecks both identities,
seats, held reservations and match state, then swaps atomically and clears both
ready flags. Character/cosmetic records and leader identity stay with the people.
Decline, expiry, disconnect, seat movement and match start never move anyone.
Malformed, spoofed, stale and duplicate replies cannot consume or apply consent.
Queue-room swaps are unavailable. Protocol140 requires matching rebuilt peers.

## Qualification

Native headless40/40 passes in0.313s: LobbySeatSwapTests11, existing and extended
SeatRequestPacketTests23, HubLobbySeatControlsTests6. This covers immediate bot
movement, recipient-only acceptance/refusal, readiness, duplicate/malformed/wide
sender replies, timeout, disconnect/reset/moved seats, actual lobby buttons and
actual Yes/No/Back popup callbacks. Initial compile completed/reloaded assemblies
then hit the headroom guard. Separate runtime reused those compiled assemblies
after idle cleanup; final exit0/no guard/restored profile and settings.
Runtime peak tree3,401,322,496 bytes, container7,297,089,536 bytes.

Two additional client receiver cases pass2/2 in0.198s for host-only offer/end
packets, stale-offer ordering, invalid fields and exact framing. Their compile
and runtime both exit0/no guard; settings/profile restored, frozen hashes match.
Final runtime peak tree3,282,337,792/container7,116,701,696 bytes. The40-case
production inputs remain identical; only these two test cases were added.
Together42distinct cases passed, not a second42-case integration run. This is an isolated candidate overlay, not whole-branch
qualification. Real network peers, a fresh rendered lobby capture, physical input
and human visual approval remain separate. No original UI baseline result is
claimed: its earlier compile hit the memory guard before a test could run.
