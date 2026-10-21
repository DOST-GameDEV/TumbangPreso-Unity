# Consume the settings rebind cancellation press

The active native settings view checks Cancel after input update. A rebind operation
can cancel during that update and its callback immediately clears Listening. The
view then sees no active rebind when its legacy Back reader runs and can leave the
settings view on the same press. This project enables both input backends, and
MenuNav intentionally retains the legacy Escape path. The installed Input System
already handles its own cancellation event; that does not supply the shared menu's
frame stamp.

The TumpSettingsSession completion callback now consumes Back only for
RebindOutcome.Cancelled. Completed, conflicted and disposed operations keep their
existing behavior. There are no binding, input backend, menu layout or game rule
changes.

## Native acceptance

Unity 6000.5.8f1 ran the four focused ControllerSupportTests cases in isolated
tump-feedback-0930 through the main run_unity_job.py and the target project's
guarded runner. The named profile is rebind-cancel1002. Both phases use batchmode,
nographics, EditMode, CPU budget 1536 MiB, reserve 2048 MiB, wait ceiling 300 seconds
and run ceiling 450 seconds. Parallel mode was not enabled.

- baseline/tests.xml: 4 cases, 2 passed, 2 failed, 0 skipped. Both real operation.Cancel
  callback cases failed because the same-frame Back stamp was absent. Successful
  rebinding and disposal controls passed.
- final/tests.xml: the same frozen fixture, 4 passed, 0 failed, 0 skipped. Cancellation
  consumes the current-frame Back press, clears Listening and re-enables the target
  without changing its binding. Successful binding and disposal do not consume Back.
- The successful binding control uses a synthetic keyboard and selects an unbound
  function key, then restores the original asset overrides, target enable state and
  saved preference. The keyboard/gamepad cancellation cases start real operations
  for those binding contexts and call operation.Cancel directly.

The original failed result and logs are retained. No physical legacy Escape key or
controller press was driven, and the view's rendered exit behavior was not measured.
The legacy-reader reuse is established by the current source; native acceptance
proves the actual cancellation callback's frame ownership and binding state.

## Preparation, inputs and preservation

The first command addressed a pool runner absent from the qualification checkout
and exited before any lease, guard or Unity launch. One bounded tooling repair used
the main runner's absolute path with the existing --project delegation. No other
source was copied to repair it. tooling-repair.json retains that failure. No fixture
repair, native retry or resource intervention was used.

Only TumpSettingsSession.cs and ControllerSupportTests.cs were overlaid. Exact
baseline/final hashes are retained in the input manifests. The runtime candidate is
DB81342F4787F3B41DD83AC3396D0DF7310C39E52553AB677F862906202DF29A.
The unchanged fixture is
E34251CBECF139336A62BFA166B2F84C9B8C973A7E5EEB62968CB49A004CECCF.
owned-input-check.json confirms main and qualification copies matched after testing.
All 24 protection checks over 20 unique paths passed, including the acknowledged
action APIs/UI/tests, panel and hub fixes, private roster assets, QualitySettings,
HeroHazards and the two protected UI metadata files.

Both pool receipts report terminal guards, completed preservation and no retained
lease. Baseline restored zero existing named-profile files and final restored one;
each restored one shared Editor input preference. Native sessions and their Unity
processes exited before the shared slot was released. acceptance-summary.json records
identities, preservation snapshots and the exact repair count.

There is no new player build, actual parallel Editor proof or whole competition
qualification claim from this unit.
