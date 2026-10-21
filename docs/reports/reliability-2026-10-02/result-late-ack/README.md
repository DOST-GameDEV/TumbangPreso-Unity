# Open result details update when the current acknowledgement arrives

The result board listened to RecordReady but not Career.Changed. Its initial XP
breakdown rendered correctly, but a later current-match dispute acknowledgement
never updated those already-open details. The native baseline reproduces the
missing warning using Stats.Adopt and actual production submission completion.

An enabled board now observes its captured career store. Only a visible board
whose displayed record still matches the latest collector record repaints its
progression details. Disabling it releases the captured subscription. Native
final1/1 verifies the actual late dispute text plus one live callback and zero
callbacks after retirement. Existing match-ID dispute filter remains in use.
No layout, input, rank-rule, server or protocol changes.

Unity6000.5.8f1 PlayMode/D3D11, isolated tump-feedback-0930, result-late-ack1002
profile. Two frozen hashes unchanged, zero fixture repairs; guard restored
profile/input. Synthetic payable record and local acknowledgement only: no real
match/server/deployment claim. Logs remain in isolated Logs/result-late-ack1002.
