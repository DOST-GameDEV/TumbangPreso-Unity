# Recorded clip validation must refuse, not throw

A complete native recording fixture reproduced InvalidDataException escaping
RecordedMatchClip.TryDecode when it encountered an incompatible field/schema.
The method deliberately raises that exception for malformed headers, bounds and
schema errors, but its catch filter included IOException, ArgumentException and
OverflowException only. In this runtime InvalidDataException was not captured.

Add that exact exception to the existing filter. Keep all format validation,
size limits and error reasons. Invalid clips return false with a null clip and
reason; no corrupt data is accepted. This narrow fix does not change the published
protocol131 or recording schema12. Unfinished Rafi schema13/gameplay is excluded.

The retained reproduced failure comes from the separate complete Baha fixture;
it is not a shipped Baha acceptance claim. Exact shipping schema12 decoder plus
only this catch fix passes2/2 independent native header checks at07:05:35UTC:
wrong signature and future schema99. Both exercise the decompressed parser beyond
the short-input gate. Exit0,45seconds outer, guardnull, named profile restored.
The standalone tests do not depend on unpublished Rafi fields. No new player,
transport, rendering, physical-device or whole-replay-flow qualification claim.
