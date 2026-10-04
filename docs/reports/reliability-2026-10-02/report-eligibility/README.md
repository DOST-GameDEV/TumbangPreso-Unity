# Report availability is independent of friend offers

The active result row formerly created REPORT only after SocialRules.RecentPlayers
offered ADD FRIEND. Already-friends, blocked players and pending outgoing requests
were therefore unreportable. A missing SocialStore also removed the report action.

The result row now selects reportable opponents from the match record separately:
another addressable human may be reported. Self, bots and empty player IDs remain
excluded. ADD FRIEND still follows the existing social offer and requires SocialStore.
No report callback, backend, gameplay rule or account behavior was changed by the
availability fix.

## Causal native acceptance

Unity 6000.5.8f1 ran the eight RecentPlayerReportEligibilityTests cases in isolated
tump-feedback-0930 through the main pool runner and target guarded runner. Both
phases used profile report-eligibility1002, batchmode, nographics and EditMode with
CPU budget 1536 MiB and reserve 2048 MiB. The old coupled UI was captured from
committed cd4a41069, with the same frozen new fixture and metadata in both phases.

The fixture reuses the qualified recent-action setup: dormant account, social,
career and statistics components; an active small native row parent; no live round.
Each case calls the real NativeRecentPlayers once from an empty parent and inspects
actual created Button objects and their active/interactable state. It never presses
an action or invokes a request/report callback.

- baseline/tests.xml: 8 cases, 4 passed, 4 failed, 0 skipped. REPORT was absent for
  friend, blocked, pending and no-social cases. Ordinary opponents, self, bots and
  empty-ID controls passed.
- final/tests.xml: same fixture, 8 passed, 0 failed, 0 skipped. Friend/blocked/pending
  opponents retain REPORT without ADD FRIEND; ordinary opponents retain both; the
  exclusions retain neither; a missing SocialStore still permits REPORT.
- There was no fixture repair, native retry, service call or world load.

## Preparation timing limitation

Before the final launch, the old UI and unchanged fixture were checked and the root
candidate was copied from its validated frozen snapshot. A PowerShell metadata-path
expression then evaluated as .meta and stopped the final input-manifest write. The
dependent native launch nevertheless proceeded. This was an orchestration error,
not a fixture or production change.

All three running files were immediately checked against their frozen hashes and
the manifest was completed after launch. The same hashes matched main/qualification
after testing, and every protected path remained unchanged. The observed original
failure output and manifest-repair.json are retained. Full preparation exit zero
before the final launch is not claimed. Future launches must stop on any nonzero or
incomplete preparation step.

## Frozen inputs and preservation

Root candidate UI SHA256:
FD102A14E8F327D60E70C719A4D0376B7CBC36E153DFFCC124130F0776F94EE3.
Fixture SHA256:
2472EC9D56FBC24F05B34EE3D96E5BF655178F174C0C935A08FE9E658E9A6925.
Metadata SHA256:
265E3ACB9433D13759536C2E309655826CF12BCB29BF2B3FF0416BE8B6A573F5.
The script GUID is valid and retained. Input manifests and owned-input-check.json
record exact copies. All 23 protected ACK/input/queue/panel/NetSession/private paths
retained their hashes.

Both guards completed preservation with no held lease. Each restored zero existing
named-profile files and one shared Editor input preference. Owned Unity processes
exited before slot release. Sessions and snapshots are recorded in acceptance-summary.json.

These tests establish native row availability, not clicked delivery, live report
service success, rendered pixels, physical mouse/controller input or a new player
build. The preceding Hero player was not modified by this unit.
