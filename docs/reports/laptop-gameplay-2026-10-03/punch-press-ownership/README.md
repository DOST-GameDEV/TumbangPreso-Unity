# Predicted punch press across refusal recovery

Date: 2026-10-03. Current baseline `659e9b28b` in the separate source-only
competition-laptop-gameplay-next1003 worktree. Four-case fixture authored;
reviewed. Original native proof shows one refusal-reuse cause and three passing
controls. Both added original recovery controls pass, and the first combined
candidate passes all six. Main audited exact source/fixture manifests and
terminal preservation. Main owns guarded execution and worker inputs.

## Contract and source mechanism

InputIntent edges remain readable across rendered Updates until the motor's
next physics CommitFrame. StepPunch gates a prediction with cooldown and
JustPressed, then stamps the punch cooldown. Public RollBackRefusedVerb(Punch)
returns that cooldown after a refused prediction. Before physics commit, the
same already-predicted edge can therefore stamp another punch cooldown.

One local prediction owns its press through refusal recovery. Cooldown refund
must remain effective; release followed by a fresh edge can retry. A press
blocked before any prediction must remain available if eligibility changes
before commit. This matches existing shove/slide and hero consumer press
ownership without modifying the producer's physics snapshot. A held edge does
not remain fresh after CommitFrame; that is a separate passing control.

Incoming free-shove/fatigue behavior, power-dependent lunge cooldown, movement,
bindings, Net handlers, owner rules and finalized hero/art direction are outside
this correction. No cooldown or stamina retune is proposed.

## Frozen original gate

Run exactly `TumbangPreso.PlayTests.PunchPressOwnershipTests`, four cases.
Expected original: one recovery causal failure and three passing controls.

- Shipping CombatVerbs.Update predicts a punch; public Punch rollback clears
  cooldown; a second Update before commit must not re-predict the same edge.
- Commit a retained held press, then update: no repeat prediction.
- Release, commit and publish a fresh press: another prediction works.
- A public authoritative missed punch establishes normal cooldown before the
  first client edge; the blocked edge remains eligible after public rollback.

The minimal arena actor uses a client INetProvider and explicitly null RPC.
Actual consumer and public refusal paths manipulate predictive cooldown; no
wire request/refusal or peer delivery is fabricated. Motor and live consumer
updates are disabled to isolate the readable edge interval, with manual
shipping Update invocations. Both hooks reset the world and restore provider,
RPC, launch, network, sandbox and stats state. No actor art/profile is modified.

Fixture SHA256:
`b3a67f270f60abefb4d582496735317ae6270f93620630b37db3a668bda6955e`.
Metadata SHA256:
`f297743189d28ce2dcf61bd3a91164201088d61f31c2cfb75522b150c2550262`.
GUID: `435be427215e4fe3b08734cc5a94cb96`.
Exact original Git CombatVerbs SHA256:
`a3be75759c81c7b387ea956329f8eaf2cfa666ac59e10da7a1cfb078533b0bce`.
Working original:
`0c39268b659fc86d8d61367044e414ae48f49b293a6fde8e8d7c2fc9c8e845ad`.
Working CRLF normalization equals the exact Git byte identity in `659e9b28b`.

Stop at fresh exact four-case XML and a terminal guard receipt. Readiness or
pre-prediction errors are not recovery cause proof. Keep fixture/metadata and
assertions frozen between original and candidate. Consume only a committed
local prediction, never a cooldown-blocked initial edge; do not CommitFrame
inside the consumer or change host RPC/resource/movement policy.

This qualifies a local predictive/public-refusal consumer boundary, not actual
transport timing, physical devices, UI navigation, authored maps, performance
or competition readiness.

## Original result and candidate

Main's `qa-d/Logs/punch-press-original4` gives the expected recovery failure:
the same predicted edge re-stamps cooldown to 0.899999976 after refusal, before
commit. All three controls pass. [Original XML](native-original/tests.xml) and
[receipt](native-original/job-receipt.json) retain exit2, terminal state,
preservation completed and no lease held. Main verified 3328 frozen hashes
unchanged. The four-case fixture/metadata stay byte-identical.

Candidate working Combat SHA256:
`1999fcf5bd59c4029c344288987dbd8e05ff86030a16611d4e4ed7cd9f5be7cd`.
LF-normalized candidate:
`f0ffb39e58e15ba1627d31a4ff65227958ef717fd8189c774cf3a5b7d0205a9d`.
The consumer owns a punch press only after cooldown/edge gates stamp a local
prediction. Refusal refunds cooldown but keeps press ownership. An observed
release clears ownership before CanAct/role early returns, so recovery does not
block a later fresh punch. It does not clear ownership just because an actor is
interrupted; a still-held spent edge remains spent. Producer snapshots, host
validation/refund and incoming free-shove/lunge values are unchanged.

Main approved two separate preservation cases, run only against the original
before combining with the unchanged four for candidate6. The added file
PunchPressReleaseRecoveryTests verifies release observed during public stagger
and release observed in the attacker role, then fresh defender punch. It uses
the same disclosed client/null-RPC consumer seam.
Additional fixture SHA256:
`694d1d786b71f4cf019f2338bea315ce86c6d0c8af3dbc0cce6cfee4a463789c`.
Metadata SHA256:
`0be6e0aff7225bba384cfb0452e3e8fea2eabed4afb70893c1fd019eb148e952`.
GUID: `cb85b7ca66694464855b543bc09f8756`.
Both interruption/role preservation controls pass against the original and
candidate. No wire, physical focus/input or full actor simulation claim is implied.

## Candidate acceptance and exact manifests

Main ran only the two added release controls against the complete original
source, reusing the unchanged original four-case cause/control result.
[Added original XML](native-original-release/tests.xml) and
[receipt](native-original-release/job-receipt.json) retain two passes.
The first combined `qa-d/Logs/punch-press-candidate6` passes all six unchanged
cases on candidate `1999fcf5...`. [Candidate XML](native-candidate/tests.xml) and
[receipt](native-candidate/job-receipt.json) retain exit0, terminal state,
preservation completed and no lease held. No fixture repair or native retry
was needed. All original/candidate guards retired normally.

Exact source manifests are preserved for the [original four](native-original/qualified-source.json),
[added original two](native-original-release/qualified-source.json) and
[candidate six](native-candidate/qualified-source.json). They identify the
immutable `659e9b28b` overlay, worker base, isolated identity and exact runtime/
fixture hashes. The changed-path list describes updates relative to the older
worker base, not new gameplay-agent edits. Main verified original3328 and
added/candidate3330 frozen inputs unchanged after their runs. Source and both
fixture byte hashes match the frozen primary inputs.

This accepts local predicted cooldown/refusal ownership and observed-release
recovery under supplied client context/manual consumer updates. It does not
prove actual network request/refusal delivery, physical input/recovery, natural
FixedUpdate cadence or any full player/competition gate. Incoming free-shove,
movement and lunge balance values are preserved. The owning input rule now
states successful-prediction press ownership and blocked-edge availability.
