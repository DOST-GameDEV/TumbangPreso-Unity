# Recent-player actions acknowledge actual completion

The result board immediately wrote REQUEST SENT or REPORTED and disabled the
button after calling an asynchronous operation. Both actual offline native button
callbacks reproduced the false claim: two baseline cases failed on those labels.

The board now waits for RequestAsync or ReportAsync. Pending actions disable
duplicate presses; confirmed actions show success. A refusal or failure restores
the original action and retry with guidance. An account change cannot accept a
late positive result, and a destroyed button is not repainted.

Career ReportAsync returns true only for the endpoint's boolean applied:true
acknowledgement while the requesting account object and player ID remain current,
signed in and nonguest. Guests cannot use the primary account's retained service
credential. Missing, null, false or wrong-typed acknowledgements, exceptions and
obsolete owners return false. The old void Report wrapper remains compatible.
Social RequestAsync confirms its target only in a successfully adopted response
owned by the requester; a cached outgoing row alone cannot acknowledge a new
request. Existing offline, guest, self, blocked and busy refusals remain local.
There is no protocol, service schema, reporting rule or friendship-policy change.

Native Unity6000.5.8f1 EditMode final:41/41 passes, comprising6 UI cases,22 career
report cases and13 social request cases. The same two actual button callbacks now
retain retry and sign-in guidance. Four controlled UI cases cover deferred true/
false completion, duplicate suppression, changed owner and destroyed button.
The original UI lacks the new completion helper, so only the two real-button
cases were selected for its baseline. API candidates stayed identical between
runs; only MatchResult.OwnerPainted changed. Zero fixture repairs were used.

The UI fixture constructs one lightweight active native row with dormant offline
services and an unsaved two-human record. It asserts that no live Round exists,
builds its initially empty row once, and restores service/instance pointers. Career
completion tests inject local tasks at the dispatch boundary. Social response
tests use controlled adoption; public request cases refuse before dispatch.
No SDK, live endpoint, actual report, friend request or message was sent. This
qualifies callback/text/control state, not rendered layout, physical input or
service/remote-peer delivery. No scene, map, player, screenshot or broad suite ran.

Runs used isolated tump-feedback-0930 and named recent-player-ack1002, batchmode
and nographics. The default serial job runner waited for the foreign Amihan
Editor to exit before admission; it did not stop that job. Baseline admitted at
6833MiB available and final at7034MiB. Both guarded runs are terminal, restored
their one shared Editor input preference, and released their leases. No existing
named-profile file needed restoration. Eleven frozen inputs and thirteen protected
source/private-asset hashes remained unchanged. Baseline native0.2787s and final
0.4500s cover different case counts and are not a performance comparison.

Raw original baseline2/final41 XML, job/guard receipts and input/protection hashes
are retained here. Full logs remain in qualification
Logs/recent-player-ack1002/{baseline,final}. No additional run is needed for this
unit; shipping-player and operator-flow qualification remain separate.
