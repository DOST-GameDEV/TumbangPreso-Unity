# Refused results keep the remaining verification witnesses aligned

DropUnsubmittable removed queued records without removing their same-index
QueueWitness entries. A mixed queue's remaining valid record therefore inherited
a rejected record's digest when FlushAsync later read witness index zero.
Legacy cache files without the list also remained unnormalized in this path.

Native baseline fails both cases. The existing PadWitnesses helper now runs
before filtering, and each refusal removes the record and witness at the same
index. Final2/2: refused head/tail leave the valid middle record and its own
digest; legacy missing witnesses leave that record paired with an empty digest.
No career-file schema, queue limit, verdict, retry or backend changes.

Unity6000.5.8f1 EditMode, isolated tump-feedback-0930, profile career-witness1002.
Two source hashes unchanged, zero fixture repairs; profile and shared-input
restoration completed. Native logs in isolated Logs/career-witness1002.
No live submission, authentication, deployment or refreshed-player claim.
