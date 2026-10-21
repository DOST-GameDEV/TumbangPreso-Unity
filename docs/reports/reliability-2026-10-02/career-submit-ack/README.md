# Require acknowledgement before removing a saved result

CareerStore.FlushAsync removed its queued record and matching witness after any
parseable response, even an empty object, missing verdict or unknown verdict.
The current match-record.js submit contract always supplies an acknowledgement.

The production completion block is extracted into one private method used by
FlushAsync. It rejects absent/null output and requires a recognized submit
verdict before profile, record or witness changes. pending, witnessed, disputed,
impossible and offline remain accepted. A pending rating is an acknowledged
submission; it does not mean the rating is final. Duplicate applied=false and
permanent refusal responses still remove the acknowledged pair normally.
Existing outer deferred-upload handling retains invalid responses for retry;
parallel queue/witness storage and service/request behavior are unchanged.

Native baseline: four missing/unknown responses discarded the first pair.
JSON null already retained data by throwing ArgumentException; its baseline
failure concerned the expected error type, not data loss. Final 10/10 passes:
five invalid shapes keep both records and witnesses; five legitimate verdicts
remove only the first pair. The tests exercise actual production completion and
cache mutation in an isolated career profile, not a source-only parser check.
Two frozen input hashes unchanged; zero fixture repairs. Named profile files
and shared input preferences restored by the guarded runner.

No live/payed service call, deployment, actual backend response or refreshed
Windows player claim. Repository server response contract was inspected; no
claim that a deployed service was queried. Native final log remains at
Logs/career-submit-ack1002/final.log in the isolated project.
