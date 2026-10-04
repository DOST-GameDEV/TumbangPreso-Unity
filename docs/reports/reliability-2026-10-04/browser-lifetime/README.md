# Room lookups belong to their browser opening

Closing the room browser cleared its visible cache, but an awaited authentication
or lobby query could finish afterward. Its old response repopulated the closed
cache or appeared in a newly reopened browser. Authentication finishing after
closure also dispatched an unnecessary lookup.

Start/stop now advance a browsing generation. The refresh captures that generation
and checks it after authentication and after the query. Old results are discarded;
current browsing and explicit manual refresh without automatic browsing retain
their existing behavior. The public wrapper still uses the same authentication
and spaced-query methods. A private dispatch seam permits local deferred replies;
reversing that seam reproduces the shipping original source exactly.

Direct native EditMode original PID17764/session98876: three causal failures and
two controls passed. The same five cases pass on candidate PID1212/session66148,
exit0. They exercise close-before-reply, close/reopen-before-reply and close-before-
authentication, with current-opening and manual-refresh controls. Dormant objects
and local replies make no Unity Services requests or profile writes.

Both jobs restore quality, editor/player input preferences and isolated settings.
All19252 frozen inputs match after separately retained202 generated GUI importers
are restored exactly. Source, fixture and assembly-reference changes remain the
intentional candidate; no arbitrary admission/timeout guards were used.

This proves response lifetime in Unity with controlled completion order. A live
service and rendered close/reopen journey remain a separate acceptance gate.
