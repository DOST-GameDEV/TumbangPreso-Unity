# Social cache account ownership

The previous SocialStore checked its disk cache owner only at startup. Switching
accounts could leave the previous account's friends and blocks visible, and a
new account's Refresh call was dropped while the old account's load was pending.
Tournament guests also retain the primary account's underlying authentication
session. Loading or writing social data with that retained session could apply
primary-account data or actions while the local account is a guest.

The fix retires another owner's in-memory cache and search status on account
notification and before cache reads, refreshes and updates. It notifies listeners
once when ownership changes and preserves same-owner cached data and list identity.
It retains one pending refresh behind the previous account's load or write and
while offline. A signed-in account notification schedules that refresh. Discarding
the in-memory cache does not overwrite the previous disk cache; only an accepted
service response writes social.json. Guest and offline accounts cannot dispatch
social refreshes, handle lookups, writes or presence, and friend actions show sign-in
guidance. The existing requested-owner response guards remain intact.

## Native acceptance

Unity 6000.5.8f1 ran the focused EditMode cases in the isolated tump-feedback-0930
checkout through tools/run_unity_guarded.py, using profile social-cache-owner1002,
batchmode and nographics. No quit flag, PlayMode, player build or endpoint calls
were requested. Preparation succeeded and recorded hashes before every launch.

- baseline.xml: 11 cases, 5 passed, 6 failed, 0 skipped. The four existing response
  ownership cases and the same-owner cache case passed. The changed-owner cache
  still exposed a previous friend, the two pending-operation cases found no deferred
  refresh mechanism, and three offline/guest cases attempted lookup through the
  service helper rather than returning local sign-in guidance.
- final.xml: 11 cases, 8 passed, 3 failed, 0 skipped. Cache ownership, both pending
  operation cases and the existing ownership cases passed. All three offline/guest
  cases passed their service guard assertions, then failed the event notification
  check because ordinary EditMode AddComponent did not invoke SocialStore.OnEnable.
- One bounded fixture repair explicitly invokes OnEnable and OnDisable while
  keeping PlayerAccount.Awake dormant. Production source did not change.
- final-retry.xml: only the three failed offline/guest cases ran, 3 passed, 0 failed,
  0 skipped. Their account event notification retired the cached rail before any
  List getter ran. The eight unchanged passing cases were not repeated.

There are 11 distinct passing acceptance cases across final.xml and final-retry.xml,
listed in distinct-acceptance-cases.json. This is not a clean combined-suite result.
The original failed final XML and log are retained. The enable and disable methods
were explicitly driven in EditMode; normal runtime callback delivery was not
exercised in a player.

## Frozen inputs and preservation

baseline-inputs.json, final-inputs.json and final-retry-inputs.json retain exact
source and fixture SHA256 values. The runtime candidate was unchanged between the
first final and retry: B32FF538FA76DCEA7CF86E0227B560954696F04F398CAC051E8ED3922320A266.
The repaired fixture is A6ED86344778F21433D1D65D7E0CBDCE07E566130A1C2184CBD696DB939EC1D6.
owned-input-check.json confirms main and qualification copies matched after testing.

All three sessions completed and their owned Unity processes exited. The guarded
runner restored 0, 1 and 2 pre-existing named-profile files respectively, and one
shared Editor input preference each time. guard-receipt.json records the snapshots
and session identities. No resource stop, reset, broad copy, unrelated source edit
or additional native run occurred. The shared validation slot was released after
the retry completed.

## Limits

Signed-in deferred-load dispatch is inspected in source, not exercised against the
service. Offline fixtures verify that the pending refresh survives both old
operations and remains queued until the new owner can use its own session. Existing
successful-response cases verify valid current-owner adoption and rejection of an
old owner's response. No live SDK authentication, friendship request, remote
service success, player build or whole competition qualification is established.
