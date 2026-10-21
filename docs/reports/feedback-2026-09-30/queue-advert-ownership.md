# Queue advert ownership

## Fix

Cancelling a locally refused new queue cleared the NetSession advert left by an
existing room, including its backfill offer. Conversely, replacing an active queue
with a refused request left the abandoned queue advertised and subscribed to browser
changes. The refusal path returns before binding a new session or beginning browsing.

StartQueue now retires an active previous search's advert/subscription before
testing its replacement. Cancel withdraws an advert only when it is cancelling an
active search. A refused new request still closes normally but preserves the existing
room's advert. Attempt invalidation, cancellation tokens and active cancellation
remain intact. No wire layout, protocol or service-query frequency changed.

## Evidence

Unity6000.5.8f1 Windows D3D11; guarded isolated feedback-queue-advert-1001 profile.
Source4c58ac265 plus the owned candidate;587 frozen input hashes unchanged after run.
Real Matchmaker StartQueue/Cancel and NetSession advert/subscription are exercised.
The fixture arranges existing room/search state, then invokes a deterministic ranked
four-stack refusal before any browser, host, join or paid service session starts.

- Baseline4cases:2passed/2failed, reproducing both advert ownership mistakes.
- Fixed4/4passed: refused new queue preserves the existing advert; refused replacement
  withdraws the old search and subscription; active cancellation still withdraws;
  repeated cancellation cannot clear a later room advert.
- No fixture repair, new compiler failure or repeated unchanged validation.

Exact XML and input receipts are in queue-advert-checks. Full logs remain in the
isolated checkout Logs/feedback-0930/queue-advert-*.log. Actual online discoverability,
Relay teardown, live backfill admission and physical-device UI are not qualified here.
