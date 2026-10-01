# Reject missing service output at the shared response boundary

CloudCode.CallAsync documented failure as an exception but returned successfully
when its HTTP-success envelope lacked output or supplied null. Four native
production-parser cases reproduce that acceptance. TelemetrySink would then
increment delivery/funnel progress, despite no output confirming the request.
Other account/social/wallet callers also share this response boundary.

The unchanged extraction is moved to a private production method used by the
HTTP path. Absent, null or undefined output now throws InvalidOperationException
and reaches callers' existing failure handling. Valid payloads retain their shape;
the helper does not interpret profiles, wallet data or operation-specific verdicts.
No URL, token/header, request rate, service or account configuration change.

Native final 8/8: missing/null cases rejected; object, array, false and zero
payloads preserved. Two frozen input hashes unchanged; zero fixture repairs.
Guarded named profile/shared input preferences restored; all jobs terminal.
No live HTTP, paid service, deployed endpoint or caller end-to-end delivery claim.
The exact shared parser was exercised. Current internal Windows player predates
this fix and other later source changes; no refreshed binary claimed.
Native log remains Logs/cloud-output1002/final.log in the isolated project.
