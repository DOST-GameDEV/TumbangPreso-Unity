# Practice idle does not restore an old producer command

SetBot parked a bot's InputIntent and disabled its AI, but the raw move remained.
Resuming unparked that old move before the next producer update. The first legal
motor fixed callback could steer the bot from a command made before idle.
PracticeRange now retires an enabled producer's input in the existing active-to-idle
transition before disabling and parking it.

## Measured original and first candidate

| Case | Original | First candidate |
| --- | --- | --- |
| Public idle/resume leaves no old move | FAIL: MoveAxis returned (0,1) | PASS: zero until fresh command |
| First motor callback after settled idle | FAIL: XZ travel 0.0750000477 versus <0.0001 | PASS |
| Repeating ACTIVE keeps current command | PASS | PASS |
| Fresh producer command after resume | PASS: actual XZ travel and speed | PASS |
| Producer with AbilitiesEnabled=false keeps shared hero keys | PASS | PASS |
| Already-disabled attached brain does not own held human keys | PASS | PASS |

Original six reproduced two causes and passed four controls. The first candidate
passed all six on the unchanged fixture/meta. There was no native fixture repair,
retry or repeat of either cohort. Preflight added clear floor placement, movement
gates and fresh-control travel assertions before the first native run.

Original exit 2 ended 2026-10-03T12:34:11Z; candidate exit 0 ended 2026-10-03T12:39:38Z.
Both receipts are terminal with completed preservation and released leases. Main
verified all 3384 frozen input hashes unchanged for each job; candidate PracticeRange
bytes matched the approved implementation exactly.

## Caller-only source change

Only PracticeRange.SetBot changes: its existing brain lookup moves before the
active-present-to-idle branch. After the existing consumer windup cancellation,
a brain that isActiveAndEnabled retires its producer input before being disabled.
This reuses AIController.RetirePendingInput and its existing selective ReleaseAll.
The planner source is unchanged. Repeating ACTIVE does not enter this branch;
a disabled attached brain is skipped; AbilitiesEnabled=false retains shared human
Skill1/Skill2/Ultimate controls. Existing held-input resources, kits, owner numbers,
consumer/contact behavior and absent/restore policy are unchanged.

## Native acceptance boundary

The fixture composes the existing public PracticeProducerResetLifetimeTests
world setup and cleanup. That supplies a prepared public range, registered
Classic actors and a floor. Supplied bot placement is clear of the can collider.
A real AIController.Drive invocation creates movement; no raw Move or private
velocity is seeded. Public SetBot performs idle and resume.

For the physical cause, 40 shipping CharacterMotor.FixedUpdate callbacks are
manually invoked while idle to settle momentum. After public resume, one shipping
fixed callback runs before another AI.Update. Actual CharacterController XZ
travel is measured with CanMove and positive fixedDeltaTime preconditions. The
fresh-producer control checks XZ travel as well as positive planar speed, so the
physical check is not an actor blocked against geometry. These are manually
stepped callbacks in the engine, not 40 naturally scheduled physics ticks or a
complete planner/menu/operator/device run. No Physics.Simulate is used.

The shared-key preservation controls publish held human hero keys through public
intent; they do not qualify a full debug/human takeover route. Remove/restore is
not included: restore has teleport/spawn-settle protection, and was not proven to
consume old move on a normal first step. Held-action-before-producer ordering is
also not claimed. No AI/Net/kit/art change, actual gameplay build inclusion,
physical device or live-peer acceptance is inferred from these focused outcomes.

## Exact provenance and byte-preserved evidence

Both native jobs use qualified logical source 31b9fdc98c5c459643cf16db94edd5ffa96a2555,
also the agent's local source-only starting base. The underlying workerGitBase is
older 8e7cfc7feb4eee614d456347ccbb26ad962d1714. Both full manifests preserve 3384
file hashes and 22 changed base paths relative to that worker. Those incoming
paths are not agent-owned edits; the actual files map identifies tested inputs.

| Frozen input | SHA256 |
| --- | --- |
| Original Practice working/native | 6f3e2bbe5931e8bf963ea36a3305463cd08eee2f725eb07e2dc7d5476fb9935a |
| Original Practice normalized LF | 101992b5d28721d4a457a708ba04a2397c153621ec2314af9217e7e68bfb625d |
| Candidate Practice working/native | c10edc7ad5cd825c5702958cd194b4f05af510e7e309cbe93a540588bc19e8aa |
| Candidate Practice normalized LF | f606dd719a1fe463a00071d621ed47c4cee19c95809982e810f66b40a7d94992 |
| Unchanged AI working/native | a159448c8b4beb65cfe819fde0dc560c2f536efb432b593babf4c1e4884b095a |
| Unchanged AI normalized LF | a8e8ad1fbaf67c59357c51d8eedcf53e20c4af52270a193f0381791e3a382575 |
| Unchanged fixture | c7bc8913efa043803c4d1ad2c7dde32f7bf6887c42ec8a12892c4060777b122d |
| Unchanged meta | ced5a7cd7c8a334979a130acd995bf1a55b48d4af9f13eb598854915e042d9c6 |

Filter: TumbangPreso.PlayTests.PracticeBotResumeInputLifetimeTests.
Meta GUID: 65fb7f6f042d4073b81884ab4fedf628.
Raw directories: C:/Users/Matthew/dev/tump-workers1003/qa-a/Logs/practice-resume-original6
and C:/Users/Matthew/dev/tump-workers1003/qa-a/Logs/practice-resume-candidate6.
All six copied artifacts and their committed Git blobs match raw bytes exactly;
report-local attributes preserve their XML/JSON line endings.

- Original: [XML](original-tests.xml), [receipt](original-job-receipt.json), [full source manifest](original-qualified-source.json).
- First candidate: [XML](candidate-tests.xml), [receipt](candidate-job-receipt.json), [full source manifest](candidate-qualified-source.json).
