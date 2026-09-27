# Networking: Where To Work

Read [AGENTS](../AGENTS.md),[working rules](WORKING_RULES.md#gameplay-and-authority),
[skill contract](SKILL_NETWORK_CONTRACT.md),then the current NET-SKILLS-1 queue and
[multiplayer evidence](reports/stability-2026-09-27/multiplayer.md).
This is a source map,not another backlog or a declaration of complete replication.

## Runtime Ownership

Runtime files below are in `Assets/TumbangPreso/Runtime/`.

| Concern | Entry points and invariant |
|---|---|
| Session/hello/seat ownership | Net/NetSession.cs,Net/LobbySession.cs,NetAuthority. Authentication or stable ID does not grant another seat. Read the current hello/protocol,not old literals. |
| Discovery and ranked/casual pairing | Net/ServerQuery.cs,Matchmaker.cs,MatchmakingCandidateCache.cs; Core MatchmakingRules owns pool/band rules. Skill contract and reserved-seat capacity filter automatic pairing. |
| Requests and received state | Net/MatchRpc.cs and its partials. Check sender,seat,match/round,epoch,request/event freshness BEFORE gameplay or presentation. Ordinary requests/refusals/actions/charge tells share GameplayActionScope; it scopes context,not per-request receipts. |
| Skill identity/authority | Abilities/HeroAbility.cs,AbilityNetworking,HeroAbilitySystem; Net/SkillCastMessage.cs. Stable IDs and explicit initial/command intent,not temporary art names. |
| Prediction and delayed bodies | HeroAbilitySystem.SkillReceipts,MatchRpc.SkillReceipts,MatchRpc.SkillDelivery. Independent effect receipts,ordered bounded delivery and recovery are not interchangeable. |
| Body-held aiming | HeroAbilitySystem.AimReplication,Net/AbilityAimSnapshot,CharacterAnimator.AimPose. Private target guidance stays local; body tells are shared presentation only. |
| Ultimate cutscene/preparation | SharedUltimatePhase,HeroAbilitySystem.SharedUltimate,MatchRpc.UltimatePhase. One accepted cohort controls hold/handback; do not duplicate gameplay on observers. |
| Persistent recovery | WorldSnapshotHeader,PreparedWorldSnapshot,HeroAbilitySystem.WorldRecovery,IPreparedWorldReplication,IWorldEffectBinding,ITimedKitReplication. Restore state at elapsed simulation time; do not recast. |
| Compatibility | Net/SkillContractFingerprint and NetSession.ProtocolVersion. Fingerprints exclude cosmetic files,but semantic/wire changes still need explicit versioning. |

Ranked,casual,custom,LAN/online and spectators share match delivery. Read the queue,
admission,reconnect and results callers as well as MatchRpc. Preserve rating,result,
leave/backfill rules and ranked device pools when fixing transport or presentation.
Online currently uses UGS Lobby/Relay peer hosting; the old Godot VPS plan is history.

## When A Character Is Reworked

Keep authored models,clips,effects,timing and intended mechanics. Cosmetic swaps use
existing cast/state/body hooks. Only Paete's VFX were considered substantial at the
owner's 2026-09-27 checkpoint; temporary effects are not permanent transport APIs.
Read the [authoring contract](SKILL_NETWORK_CONTRACT.md) before adding a new RPC.
New gameplay data needs an explicit reusable state/recovery contract; no promise
that every future mechanic fits the current centre-plus-clocks adapter.

## Evidence And Remaining Limits

Use [TESTING](TESTING.md) for guarded runs. Existing focused fixtures include
SkillReceiptTests,IceSnapshotBatchTests,MatchmakingWireTests and protocol/approval
checks in ChatAndLobbyChromeTests. tools/net_link.py and tools/net_matrix.py drive
actual peers; a source helper test is not a ranked/reconnect/cross-platform match.

Current reports separate compiler checks,pure managed checks,native local cases
and actual peers. Real ranked/party/reconnect/results qualification is still open.
Persistent world collections retain explicit kinds beyond the generic prepared
adapter. Same-round ordinary-verb refusals still need per-request receipt coverage.
Do not mark NET-SKILLS-1 complete from declarations or compilation alone.
Old C4/engineering assignments and port mappings live in [archive](archive/README.md);
their exclusive reservations and historical counts are not current instructions.
