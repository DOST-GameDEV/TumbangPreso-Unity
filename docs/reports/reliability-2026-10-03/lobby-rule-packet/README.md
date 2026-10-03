# Preserve lobby rules on malformed updates and refuse leader ID aliases

Original30474 demonstrates three causal failures: incomplete leader SelectRules
and host SyncRules frames throw OverflowException, and sender1UL<<32 narrows to
leader0 and publishes rules/format events. The three controls pass: valid leader
and host1-round/30-second custom wire, ordinary role/leader/loopback refusal.

Candidate checks complete bounded UTF-16 frame through existing ValidStringFrame
before decoding in both callbacks. Shared leader authorization rejects sender
IDs above int.MaxValue before narrowing. Valid rule parsing/clamping, events and
leader authority remain. No protocol/schema or gameplay parameter change.

Candidate15396 FIRST6/6, same fixture and assertions, no repairs/retries. Dormant
actual NetSession/Lobby leader and actual receive callbacks, no transport/SDK.
Both guards preserve named profiles/shared input and release leases. Post58447:
all18445 protected qualification assets unchanged, exact3 MAIN/q matches.
Current1003c artifact predates this unit and replay binding optimization; current
packaged acceptance is a separate gate. No live-peer fault injection or whole
competition-readiness claim.
