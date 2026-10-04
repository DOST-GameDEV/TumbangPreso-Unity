# Failed friend delivery remains retryable

Baseline:3661b19194ad8e4cd797399f3b04d8ede2d664d6, October4.

The request endpoint saved the sender's Outgoing row before the recipient's
Incoming row. If the recipient write failed, the sender remained pending but
the recipient had no request. Core SocialRules.WhyCannotRequest explicitly
refuses resending an already-pending outgoing row, so client refresh could make
this failed delivery unrecoverable through the normal request action.

The endpoint now delivers the recipient row before publishing outgoing pending
state. A recipient failure leaves the sender retryable. A sender failure after
delivery is safe to retry because the existing recipient row is deduplicated.
Blocked-recipient behavior remains opaque: sender sees pending, no recipient row.

Actual exported social script with local Cloud Save one-shot failures:
original1failure/10controls -> corrected11/11. This extends the previous eight
capacity/request/accept/reload/decline/remove cases with both write failures
and blocked-recipient privacy. No credentials, live profile or player modified
by these local cases. No claim of atomic transactions or concurrent-write safety.

The owner separately authorized one bounded live two-account test after the
Unity Cloud Code usage explanation. The game production socialv2 already has
the prior capacity correction, verified code/parameters and rollbackv1 retained.
This delivery correction still needs source publication, verified service
publication and actual two-account execution before full Friends acceptance.
