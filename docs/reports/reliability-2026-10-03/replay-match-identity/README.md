# Replay history belongs to a match

A reusable same-scene match start can return to round1 while creating a new
PresentationMatchId. Body sampling and live prop/field history previously reset
only when RoundNumber changed. The archive cleared its shortlist for the new
match but retained previous-match live samples, which could supply the new
highlight's lead-in.

Body history now resets on match or round boundaries. The archive resets its
live sample, pending, field and sound state on either boundary, while a normal
round keeps the same match's completed highlights. Detached retained bytes are
untouched. Cadence and prop scan deadlines restart with their respective rings.

The native dormant fixture calls the actual boundary methods and seeds samples
directly. Original same-round new-match cases failed2; four same-match/round
controls passed. The first candidate passed all6 with unchanged assertions.
Independent static review passed. Both guards terminated, restored profiles and
preferences and released their leases. This proves source/lifecycle behavior,
not rendered highlights, live match operator flow or performance improvement.
