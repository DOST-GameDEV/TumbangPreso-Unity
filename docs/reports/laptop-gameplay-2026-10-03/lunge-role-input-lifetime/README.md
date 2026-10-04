# A defender lunge windup ends when the body becomes an attacker

Combat's attacker branch left the previous defender lunge windup alive. Its
local observed-charge getter remained positive; after releasing as an attacker,
returning to defender could launch the old windup without a fresh press.
The attacker branch now calls the existing pending-input cancellation helper.
Same-role charge and already committed contact/cooldown remain intact.

## Checked outcomes

The unchanged five-case fixture first reproduced two causal failures and passed
three controls. The first candidate passed all five. There was no fixture repair,
retry or additional native run.

| Case | Original | First candidate |
| --- | --- | --- |
| Become attacker with defender windup | FAIL: observed tell 0.037948411 instead of -1 | PASS: tell inactive, charge zero, no new cooldown/contact |
| Release as attacker, return to defender without fresh press | FAIL: stale windup committed cooldown 0.5 | PASS: cooldown/contact/charge stay zero |
| Same defender snapshot while holding windup | PASS | PASS |
| Ordinary defender release | PASS: opens cooldown/contact | PASS |
| Change role after public host committed a lunge | PASS: active window and cooldown survive | PASS |

Original exit 2 ended 2026-10-03T10:56:32Z; first candidate exit 0 ended
2026-10-03T10:59:56Z. Both guards were terminal, completed preservation and
released their leases. Main audited all 3360 frozen input hashes unchanged for
each job. Candidate Combat bytes exactly matched the frozen implementation.

## Implementation and measured scope

The runtime diff is two added lines: a comment and CancelPendingInput at attacker
entry. That existing helper only clears local charging, charge time and received
windup presentation. The helper itself is unchanged. Active lunge/slide contact
windows, cooldowns, press ownership, resource rules, owner lunge numbers, free
shoves, slide 25, finalized kits/art and network source/protocol are unchanged.

The fixture uses a supplied can and registered Classic body in an empty scene.
It creates windup through public held intent and shipping Combat.Update, then
applies public MatchDirector/RoundDirector snapshots while the body stays able
to act. It commits the initial held input before switching role, so an old press
edge cannot accidentally become a new attacker shove. Pending state and role
flags are never privately seeded. The contact control observes a private window
read-only, opened by public HostResolveLunge; it also checks spent cooldown.

Motor physics and automatic consumer callbacks are disabled; shipping Update is
invoked explicitly, with positive actual delta below the active-contact window.
These are local consumer windup, release and window/cooldown outcomes. They do
not qualify natural motion/tag delivery, intermission scheduling, a complete
tutorial, physical devices, live snapshot/RPC delivery, complete matches or
inclusion in a rebuilt artifact. GuidedTraining.ApplyRoles uses the same active
public snapshot pair by source trace; no tutorial operator result is implied.

Existing Carrier role retirement and Main's published shove/slide press recovery
remain intact. This unit closes pending defender windup, rather than changing the
shared-button press latch or retiring already committed dash actions.

## Exact source and evidence provenance

Local source-only checkout started at 4078925cc17192df97724cf51ca779611e99b1e7.
Both native jobs used Main's qualified logical overlay
b8e5cec5fbca55cbf8becc399b3fa3937512a815 on older workerGitBase
8e7cfc7feb4eee614d456347ccbb26ad962d1714. Each full manifest preserves 3360 file
hashes and 23 changed base paths relative to that worker. Incoming paths are not
agent-owned edits. The actual files maps identify tested Combat and fixture bytes.
Candidate sourceControl retains the original descriptive canonical 0d939 baseline
label. Its files map correctly records candidate 3f24 below; the stale baseline
label is preserved byte-for-byte, not rewritten as a candidate claim.

| Frozen input | SHA256 |
| --- | --- |
| Original Combat working/native bytes | 4c7f2eae1d3eb305c64da5061548c3139208a6ae2de04cc330a807560614d3b8 |
| Original Combat normalized LF | 0d93959e5510cd7ddb886fd642bb59220b10b57a8d4df6b295c7d654af7fb9b4 |
| Candidate Combat working/native bytes | 3f24f6139906d1f6220215fe2e0413dc061313f91ccc127c97a5af2dc296b65e |
| Candidate Combat normalized LF | cdc611e5125f085b139525c7cc7506bfe34efd81e6aa5e1ba2927ccb342d39dd |
| Unchanged fixture | 72fbd98955cfe8ef0ab0df5b33c8c86d15778b357f3c9f0a8063141c9a88feb9 |
| Unchanged fixture meta | c917864d35eadd61c90ab208b453b9cf8d9c3591ce43fc331cd222d7dc04d07b |

Filter: TumbangPreso.PlayTests.LungeRoleInputLifetimeTests.
Meta GUID: f19e9a61207f42338bcdd09d723bc8d6.
Raw directories: C:/Users/Matthew/dev/tump-workers1003-extra/qa-d/Logs/lunge-role-original5
and C:/Users/Matthew/dev/tump-workers1003-extra/qa-d/Logs/lunge-role-candidate5.
Report-local attributes preserve raw XML/JSON bytes in committed Git blobs.

- Original: [XML](original-tests.xml), [receipt](original-job-receipt.json),
  [full qualified source manifest](original-qualified-source.json).
- First candidate: [XML](candidate-tests.xml), [receipt](candidate-job-receipt.json),
  [full qualified source manifest](candidate-qualified-source.json).
