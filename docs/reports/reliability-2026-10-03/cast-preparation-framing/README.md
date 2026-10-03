# Bound cast preparation decoding

Truncated host cast-preparation messages threw OverflowException from the
unchecked header/string/tail reads. A trailing byte also advanced decoding
instead of rejecting the frame. The handler now reuses SkipWireString to
preflight its fixed header, UTF16 hero name and exact52-byte tail, restoring the
decode position before reading. Wire layout, protocol, kits and timings remain.

Original8f0b34054 reproduced seven causal failures and passed three controls.
Candidate088ae2aee passed10/10 on the identical fixture. Root independently read
the raw XML, receipts and full3388-input maps published at a80acb40b. The only
declared input change was MatchRpc.Preparations.cs. Committed candidate SHA256
3be659f077b32a8e3ca02ff57de0766c575f66594bff79900671ca8628255a54 matches the
tested code. Fixture/meta remain unchanged; native line-ending conversions were
checked against the original and candidate Git blobs. Both guards completed
profile preservation and released their leases. There was no fixture repair.

[Raw evidence](../../laptop-validation-2026-10-03/cast-preparation-packet-bounds/README.md).
This qualifies native packet framing and decode position, not actual routing,
ability execution, whole-player crash, peer/Relay or current player acceptance.
The laptop ran the tests; Claude's PC Unity slot remained untouched.
