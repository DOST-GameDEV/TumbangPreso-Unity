# Correlate late career acknowledgements with the submitted result

FlushAsync captures a record before awaiting upload, but completion previously
removed the current queue head. Record's queue cap can evict that head while
upload awaits; an account change can replace the cache entirely. Two native
production-completion cases show an unrelated pending result being removed.

The upload now captures its cache and record. Completion rejects a replaced
cache before profile/verdict/queue changes, returning false to stop the old flush.
For the same cache it removes only the submitted record, at its actual index,
and its matching witness. If that record was already evicted, no other result
is removed. Queue/witness storage schema, cap and service behavior remain.

Native final 12/12: both late-identity cases preserve the other result/witness;
existing five invalid acknowledgement and five recognized-verdict controls pass.
Actual CareerStore completion/cache mutations exercised, not source-only checks.
Two frozen input hashes unchanged; zero fixture repairs; profile/input restored.
No backend call, deployed-service or actual account-login integration claim.
Current internal Windows player predates this and later source fixes; no refresh
claimed. Native log is Logs/career-submit-identity1002/final.log in the isolated
project. Existing user data and private work were preserved.
