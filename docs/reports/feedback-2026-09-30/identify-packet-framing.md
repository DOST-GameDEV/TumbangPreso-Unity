# Identify packet framing before string decoding

OnIdentifyMsg read peer-supplied strings and integers directly. Truncated fields,
lengths outside int range and overflowing UTF-16 byte counts threw before identity
or arrival validation. A framing pass now checks the complete payload without
allocating managed strings, restores its starting position, then runs the existing
decoder and approved-identity path only for valid frames.

Each declared UTF-16 character count must fit the actual remaining bytes. The
four initial strings, three picks, mandatory cosmetic frame and existing optional
custom/build tails retain their format. Trailing data must end at the last field.
No guessed text clamp, new field, protocol119, account authority or kit change.

Native baseline6/6fails with actual OverflowException/InvalidCastException on
four truncations and two impossible lengths. Final EditMode8/8passes those plus
explicit valid Unicode/full/legacy-tail acceptance and decode-position restoration.
A separate native listening-host case passes actual OnIdentify delivery, normal
picks and approved-token pinning despite an untrusted message token.
Nine distinct cases across two runs, not one9/9suite.

Source2fdcc6ce9 plus four owned source/test/meta overlays;682frozen input hashes
show no drift, and owned files match the imported candidate. New script metadata
has a valid32hex GUID and native import succeeds. Jobs6231/11606/64329terminal;
no fixture repair or unrelated suite rerun, guarded input/profile bytes preserved.
[Receipts](identify-framing-checks).

This qualifies local malformed framing and the listening-host valid path. It is
not a new player/actual-peer or all-message hardening claim. Existing117reconnect
and116Haunt evidence retain their older source boundaries. No loading, authored
sound/animation/model/map/lighting or extra external service queries changed.
