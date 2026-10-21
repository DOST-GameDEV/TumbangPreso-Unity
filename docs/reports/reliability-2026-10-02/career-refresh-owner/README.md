# Keep older career refreshes out of a replacement account cache

RefreshAsync awaited the service and then adopted its response into the current
cache, even if OnAccountChanged replaced that cache while the request was in
flight. Actual production-completion baseline: normal refresh passes, but a
replacement cache's XP77 is overwritten by the older profile's XP420.

The refresh captures its cache. Completion rejects a different current cache
before adopting data/status. The abandon-await boundary and failure-status path
also ignore stale/destroyed refresh owners. Normal current response behavior,
profile replacement policy and cache schema remain.

Native final2/2: current refresh appliesXP420; replaced cache remainsXP77 and
completion rejects the response. Two frozen input hashes unchanged, zero fixture
repairs, profile/input restored. Actual cache/profile completion was exercised;
no real login, account switching, endpoint call or deployed service claim.
Automatic newest-account refresh scheduling is not established by these cases.
Current internal Windows player predates this guard; no refreshed binary claimed.
Native log is Logs/career-refresh-owner1002/final.log in the isolated project.
