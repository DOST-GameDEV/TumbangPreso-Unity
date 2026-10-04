# Actual production Friends acceptance

The existing game project/environment matched the configured Unity project.
Published socialv1 had the old capacity behavior. Tested capacity code was
publishedv2 with exact normalized source and unchanged six parameter definitions;
v1 remained available for rollback. Recipient-first delivery code was then
publishedv3, again verified against canonical local source with older versions
retained. No application account or profile was used for deployment.

After the owner explicitly approved the live test, two fresh anonymous test
accounts authenticated through the same HTTPS Authentication endpoint used by
the installed SDK. The existing published player-account endpoint saved each
test name and derived tag. Actual resolve returned the other account's identity.
Social request, recipient load, acceptance and reload from both sides all passed:
both stored the reciprocal friend once and had no pending rows.

Eleven actual script executions included cleanup. Friendship was removed,
account profile/proof/handle-index data cleared and both Authentication accounts
deleted. Empty socialList records remain for subsequent admin cleanup; do not
claim complete Cloud Save erasure. Tokens existed only in helper process memory
and were not printed or retained. Existing user accounts/preferences were untouched.

This is real service-side authenticated HTTPS acceptance, not rendered UI or
UnityWebRequest/SDK execution. Actual invites/join, physical inputs and the full
rendered login-to-gameplay-to-results journey remain open. Billing allowance
remaining was not verified; the owner separately authorized this bounded test.
