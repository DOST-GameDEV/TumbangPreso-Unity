# Validate the entire edge-climb request before decoding

OnReqEdgeClimbMsg read two integers without checking the unread payload. The
candidate requires exactly eight bytes and TryBeginRead before either read.
Existing sender-seat ownership, movement epoch and host geometry checks remain.
No wire layout, protocol142, recovery timing or actor behavior changes.

Original with the frozen fixture is3ead4fc27; candidatec856dfe25 is on
competition-pc-edge-climb-bounds1003. Six native EditMode cases cover three
truncated boundaries, a trailing byte, complete decode before invalid-seat
refusal and client-authority refusal. The NGO envelope is already consumed.
Original6 reproduced four framing failures and passed two controls. Candidate6
passed6/6 on the identical fixture. Root independently inspected the raw XML,
full3396 maps differing only in EdgeRecovery, fixture/meta bytes and both
terminal/restored/free guard receipts at46782a7c8. No repairs or retries occurred.
[Raw evidence](../../laptop-validation-2026-10-03/edge-climb-packet-bounds/README.md).

The laptop performed original/candidate native validation while Claude owned PC Unity.
PC direct compilation uses the existing isolated worker references and candidate
source overlays; this is a syntax/API check, not current-player qualification.
Native packet framing and decode position are qualified. Real climb execution,
geometry, current-player and peer acceptance remain pending. The inspected guard
is integrated with current ASTRAReworks. No PC Unity job was started.
