# Bound service requests that accept a connection but stop responding

CloudCode awaited request completion without an explicit timeout. Callers only
release Busy/writing/flushing state after the request settles. The actual native
production-built POST reached a task-owned loopback server that accepted but
never responded: timeout 0, still InProgress at 2.000443 seconds. The baseline
request and socket were explicitly aborted/disposed afterward.

The existing production request construction is extracted and now sets one
20-second UnityWebRequest timeout. URL, method, upload body, headers, bearer
handling and caller error paths remain. No retry configuration or UI setting.

Native final 2/2: actual stalled request settles ConnectionError at 20.00403s;
normal local JSON response completes Success at 0.007270098s. The same production
request builder is used. This proves native request liveness and local success,
not deployed service speed or a full shop/account UI journey. Three frozen inputs
including valid script metadata unchanged; zero fixture repairs. Both local
listeners/requests and the native process retired; profile/input preserved.

No live endpoint, real token, paid service or external server was contacted.
Current internal Windows player predates this and later source fixes. Native
final log remains Logs/http-timeout1002/final.log in the isolated project.
