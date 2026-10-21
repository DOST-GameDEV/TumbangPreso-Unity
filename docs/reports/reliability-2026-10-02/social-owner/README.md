# Social replies remain owned by the requesting account

Refresh/Post awaited an answer, then adopted it into whichever account was current;
Save stamped that older friends/block data with the new player's ID. Handle lookup
also proceeded into status/new Request dispatch without checking its original owner.
Native baseline4cases: two normal controls pass, stale list and stale lookup fail.

Refresh/Post now capture owner before await, Adopt rejects older/destroyed owners,
and handle resolution rejects an obsolete owner before status or Request dispatch.
Final4/4 preserves current friends/blocks/search status and normal responses.
No social schema, friendship rules, permissions, presence cadence or wire change.

Unity6000.5.8f1 EditMode, isolated tump-feedback-0930, social-response-owner1002
profile. Two frozen hashes unchanged, zero fixture repairs; guard restored profile/
input. Synthetic list and EMPTY handle resolution only, no friendship/message/
endpoint call. The entry guard covers the valid-result branch in source, but
actual signed dispatch, runtime account cache clearing and newest-owner refresh
scheduling are not established by these four cases. Native logs remain isolated.
