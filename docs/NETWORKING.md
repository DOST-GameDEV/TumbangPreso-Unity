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
| Presentation teardown/reconnect | NetSession.EndTransportPresentation cancels local match presenters and hitstop before normal speed. Fresh MatchRpc handler binding resets received cohort/pending-request state,not the host lifetime sequence. Ordinary Cancel retains duplicate protection. |
| Ready and countdown | ReadyGate and MatchRpc use protocol83 match identity (zero only for lobby READY). Exact payload bounds and current seated membership precede votes; quorum waits for host loading. Manual votes retry until countdown acknowledgment, and a completed countdown stays consumed until the gate explicitly reopens. |
| Discovery and ranked/casual pairing | Net/ServerQuery.cs,Matchmaker.cs,MatchmakingCandidateCache.cs; Core MatchmakingRules owns pool/band rules. Skill contract and reserved-seat capacity filter automatic pairing. |
| Requests and received state | Net/MatchRpc.cs and its partials. Check sender,seat,match/round,epoch,request/event freshness BEFORE gameplay or presentation. Ordinary requests/refusals/actions/charge tells share GameplayActionScope; it scopes context,not per-request receipts. |
| Skill identity/authority | Abilities/HeroAbility.cs,AbilityNetworking,HeroAbilitySystem; Net/SkillCastMessage.cs. Stable IDs and explicit initial/command intent,not temporary art names. |
| Cooldowns and charges | AbilityResourceSnapshot,MatchRpc.AbilityResources and HeroKit.AllAbilities. Scoped,ordered hero/ability identities include both roles; validate the complete set before mutation,and retain owner-live anti-refund behavior. |
| Prediction and delayed bodies | HeroAbilitySystem.SkillReceipts,MatchRpc.SkillReceipts,MatchRpc.SkillDelivery. Independent effect receipts,ordered bounded delivery and recovery are not interchangeable. |
| Body-held aiming | HeroAbilitySystem.AimReplication,Net/AbilityAimSnapshot,CharacterAnimator.AimPose. Private target guidance stays local; body tells are shared presentation only. |
| Remote resources | CharacterMotor.NetworkStamina advances host resource clocks from accepted pose intent without a second movement simulation. Input leases/epochs bound it; Stamina resource corrections preserve held-sprint continuation. |
| Voodoo body state | Net/VoodooBodySnapshot prefixes SyncUnit's aim tail; bounded status/mark/reach state and explicit reach result. Apply status before the authoritative pool; gameplay wiring is separate. |
| Body snapshot scope | SyncUnit uses GameplayActionScope before its ordered pose/status payload. Validate match/round before a fresh body's serial cursor; movement epoch and cross-delivery serial rules still apply. |
| Combat refusals | MatchRpc.VerbReceipts and Core PredictionReceiptWindow bind denial to the newest prediction per verb; host fixed-seat cursors reject repeats. Network rollback does not add stamina; a targeted reliable SyncUnit corrects the authoritative pool. |
| Held interactions | CharacterMotor.InteractionHeldForSimulation reads local or accepted leased host input. Root/plant hold clocks run on the host; scoped client completion notifications cannot skip time. Visual progress is not authoritative. |
| Ultimate cutscene/preparation | SharedUltimatePhase,HeroAbilitySystem.SharedUltimate,MatchRpc.UltimatePhase. Host-sealed duration travels with the cohort so missing caster bodies cannot choose a short local fallback; one shared hold/handback,no duplicate gameplay. |
| Ultimate identity | UltimateCommit carries hero/ability IDs; ReqUltimate uses GameplayActionScope including body epoch. Host checks current kit and ownership,preparation waits for matching kit,and execution refuses a changed one. |
| Requested match clock | MatchClockMessage and MatchRpc.Clock scope pause/speed to match/round and sequence. SyncWorld precedes recovery rate on the reliable stream. PresentationClock excludes temporary Hitstop; spectators retain existing request permissions. |
| Persistent recovery | WorldSnapshotHeader,PreparedWorldSnapshot,HeroAbilitySystem.WorldRecovery,IPreparedWorldReplication,IWorldEffectBinding,ITimedKitReplication. Restore state at elapsed simulation time; do not recast. |
| Persistent lifetime | HeroAbility.AcceptedCastEvent and WorldEffectSnapshot.Field.InstanceId; ordinary initial cast identity survives commands/recovery. Plant removal matches exact lifetime plus match/round and remembers retirement before late installation. |
| Recovered target sets | WorldEffectSnapshot.Field.TargetMask and PaeteSentry; preserve host-selected seats rather than rerunning distance checks. Missing bodies bind once; restoration never recatches. |
| Live sentry targets | SentryTargetState and MatchRpc.SentryTargets deliver the host mask by match/round/owner/ultimate cohort,with bounded pre-birth delivery. PaeteSentry live registry binds it once; replicas never infer or catch. |
| Effect cleanup ownership | PaeteHeroKit's ultimate tracks its exact fresh sentries and adopts recovered owner instances through IWorldEffectBinding. One kit reset must not destroy other casters' effects. |
| Contact presentation | Visual/MatchFlair announces accepted outcomes with match/round scope. UltimateImpact reuses HitFeel on the victim's own view; no gameplay mutation or caster confirmation. |
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
adapter. Correlated ordinary-verb refusals are implemented; native and real-peer
qualification of the new receipt/resource path remains open.
Do not mark NET-SKILLS-1 complete from declarations or compilation alone.
Old C4/engineering assignments and port mappings live in [archive](archive/README.md);
their exclusive reservations and historical counts are not current instructions.
