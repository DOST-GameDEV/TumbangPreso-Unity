# Preserve the new account's automatic career sync

OnAccountChanged called SyncAsync while another account's flush/refresh was busy.
The existing one-at-a-time gates returned immediately and no later callback
retried the new account's request. Both native scheduling baselines reproduce
the lost sync under a pending flush and pending refresh.

Account changes now retain one waiter per current cache. It waits for existing
operations to release their flags, ignores replaced/destroyed owners, and
coalesces repeated notifications for the same cache. An older waiter's cleanup
cannot clear the newer account's waiter. Existing remote request policy remains.

Native final4/4: busy flush/refresh defer then reach the offline refresh path;
same-owner notifications coalesce; replacement owner outlives old cleanup.
Unity6000.5.8f1 EditMode, isolated tump-feedback-0930, career-account-sync1002
profile. Two frozen hashes unchanged, zero repairs; guarded profile/input
restoration completed. No live service/account switch, endpoint or deployment
claim. Native logs remain in isolated Logs/career-account-sync1002.
