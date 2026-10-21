# Carrier pending input belongs to the current role

An active role snapshot could leave an attacker throw windup alive after becoming
the defender, or leave defender can-reset progress alive after becoming an
attacker. The latter also kept Carrier.IsBusy true by its existing channel gate.
Carrier now retires only the opposite role's pending verb before dispatching the
current role. Held shoes, same-role progress and ordinary throw release remain.

## Checked outcomes

The unchanged five-case fixture first reproduced two causal failures and passed
three controls. The first candidate passed all five. No fixture repair, retry or
additional native run was needed.

| Case | Original | First candidate |
| --- | --- | --- |
| Become defender with an attacker throw charge | FAIL: IsCharging remained true | PASS: charge/tell retired; held shoe preserved; later attacker release does not throw |
| Become attacker with defender reset progress | FAIL: ChannelRatio remained 0.0178542007 | PASS: progress zero, Carrier no longer busy, can remains down |
| Same attacker snapshot while charging | PASS | PASS |
| Same defender snapshot while resetting | PASS | PASS |
| Ordinary attacker release | PASS: owned shoe enters InFlight | PASS |

The two job receipts record normal terminal ownership/preservation and free
leases: original exit 2 ended 2026-10-03T09:57:29Z; candidate exit 0 ended
2026-10-03T10:02:16Z. Main audited all 3346 frozen input hashes unchanged for each
job. The candidate Carrier bytes exactly matched the frozen implementation.

## Change and acceptance boundary

The implementation changes only Carrier.Update's role dispatch. The defender
branch uses the existing CancelCharge and clears its observed throw tell. The
attacker branch reports the existing reset Cancel phase only for positive old
progress, then zeroes channel and ratio. No role-history tracker, held-item
mutation, consumed-pickup latch reset, resource/kit/contact/cooldown change, role
setter change or network protocol change is included.

The fixture supplies a can, owned shoe and registered body in an empty scene,
then applies public MatchDirector and
RoundDirector snapshots while it remains able to act, and creates charge/reset
state through public intent, owned shoe equip/disarm and can knockdown. Motor,
shoe physics and automatic consumer callbacks are disabled; shipping Carrier.Update
is invoked explicitly. No private pending field or role flag is seeded. This
qualifies supplied local active snapshots and consumer state/outcomes. It does
not qualify natural intermission scheduling, physical devices, live snapshot/RPC
delivery, all observers, complete matches or inclusion in a rebuilt artifact.

Practice SetDefender already calls the checked ResetRange retirement path; it was
rejected as a separate cause. Existing producer-rebind tests cover replacement
and deferred-destruction custody, rather than this same-body active taya-role
change. Main's separate Combat press-recovery work is outside this change.

## Source and raw evidence provenance

Local source-only checkout began at 104213d468fa5cbcf21cecdc71327800a782a038.
Both native jobs used Main's qualified overlay at logical
6d5d273334e780016f9d18a6d19490f9f34c5553, not a claim that the older worker Git
checkout was current. Its workerGitBase is
8e7cfc7feb4eee614d456347ccbb26ad962d1714; each full manifest preserves 3346 file
hashes and 30 changed base paths relative to that worker. These incoming paths are
not agent-owned edits. The actual files map records the fixture and tested
Carrier bytes. Candidate sourceControl retains the original baseline text label
mentioning canonical e701; that descriptive label was not updated for the
candidate. The candidate files map correctly records de5a84 below. Raw evidence
is preserved byte-for-byte, including that label.

| Frozen input | SHA256 |
| --- | --- |
| Original Carrier working/native bytes | 5f9969ed79a1f2a3a5e661509016cd962121aefa49edeb2c2697cc6afa9c9048 |
| Original Carrier normalized LF | e701893e0a071821172fd5d35eea18b9acc6d4b87b5700a96bb81fa5e3c14d07 |
| Candidate Carrier working/native bytes | de5a84a4f05625d938ad17ab43f9b177751cce4f0b8f0a4d210c77073f805b59 |
| Candidate Carrier normalized LF | 3b2e1b609a2adad7a947147a9aac5cdf1c65c44a1d73ae30ec995ce22612d458 |
| Unchanged fixture | 9469589fc46f1cb7a7d9ff8b616b1860fea7fb112ae0e25646e307c473ae3b97 |
| Unchanged fixture meta | ea36e4bcfcfd5c1d4461adc50f92c9c2b3557f9ad8c96b57e25fa4244c2640a5 |

Filter: TumbangPreso.PlayTests.CarrierRoleInputLifetimeTests. Meta GUID:
f863e5f72d07432dbd3615446f863432.

Original raw directory: C:/Users/Matthew/dev/tump-workers1003-extra/qa-d/Logs/carrier-role-original5.
Candidate raw directory: C:/Users/Matthew/dev/tump-workers1003-extra/qa-d/Logs/carrier-role-candidate5.
All six curated files were compared byte-for-byte with those raw files.

- Original: [XML](original-tests.xml), [receipt](original-job-receipt.json),
  [full qualified input manifest](original-qualified-source.json).
- First candidate: [XML](candidate-tests.xml), [receipt](candidate-job-receipt.json),
  [full qualified input manifest](candidate-qualified-source.json).
