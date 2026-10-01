# Networking: Where To Work

Current gameplay contract is protocol118, including adopted Hydro Crosscurrent,
Skim, Water wall and charge cancellation while the can is down/protected.
Latest actual Windows direct-peer qualification is protocol114with the CLI
admission handoff fix, through active round2with matching structural state.
It predates Hydro115/116/117/118and the later local menu/bot/notice changes; it cannot qualify
those integrations. [Player evidence](reports/feedback-2026-09-30/cli-admission-handoff.md).
[Hydro evidence](reports/hero-quality-2026-10-01/hydro-current-checks/README.md).
[Skim evidence](reports/hero-quality-2026-10-01/skim-checks/README.md).
[Water wall/tutorial evidence](reports/hero-quality-2026-10-01/wall-tutorial-checks/README.md).

A separate actual116Windows owner-client Haunt fixture passes with host contacts,
replicated clocks, moving familiar and terminal cleanup. Stationary bystanders,
not whole-match/rejoin/loss/other-kit qualification.
[Exact evidence and boundaries](reports/feedback-2026-09-30/haunt-actual-peers.md).

Actual117crash/relaunch restores the same identity/seat and selected character on
the changed pre-round handover candidate. Aggregate gate remains failed at the
host's later round-boundary sample; no118or complete-round proof is claimed.
[Preserved results and limits](reports/feedback-2026-09-30/reconnect-actual-player.md).

Departure notice payloads must end exactly after the declared name, before the
receiver commits the sequence or toast. Six native receiver checks pass, including
the consumed NGO name envelope and malformed suffix recovery.
[Evidence](reports/feedback-2026-09-30/peer-departure-framing.md).

Earlier Windows peer evidence: frozen committed overlaysffe5030c5, protocol103,
same fresh internal binary on actual localhost UDP host/client. Both reach active
HeroStrike/Eskinita round2 with matching structural state and no hard faults.
This is direct local session qualification, not individual skill/online/Relay/
lossy/reconnect/cross-platform delivery.
[Evidence](reports/feedback-2026-09-30/engineering-player-1001.md).

Protocol103 requires current Cheska field/delay/passive/all-player targeting rules.
Cold Feet lasts7.5s, Absolute Zero waits1.5s and includes its caster, and landed
Hero Strike Cheska shoves apply Chilled. Native cases pass; protocol103 actual
peers are not yet qualified. [Evidence](reports/feedback-2026-09-30/cheska-wiki-rules.md).

Protocol101 also requires the corrected Frostbite hit/held-slipper contract.
Both body-hit paths apply Frozen before generic impact consumes the payload.
Native real-flight checks pass; protocol101 actual peers remain unqualified.
[Evidence](reports/feedback-2026-09-30/frostbite-delivery.md).

Queue cancellation only withdraws an active search's advert. A locally refused
attempt cannot clear an existing room's backfill offer; replacing an active search
retires it before a replacement can refuse. Four native offline ownership cases
pass; actual online browse/backfill delivery remains separate.
[Evidence](reports/feedback-2026-09-30/queue-advert-ownership.md).

Protocol100 requires matching builds for the5-second ordinary round boundary and
10-second halftime package. Both freeze simulation and reject gameplay/UI input.
Halftime can play a retained authoritative clip before standings; unavailable or
late footage falls back to the same frozen image without extending the host end.
The latest ordinary/late-deadline and halftime cases pass locally. Earlier eight
focused native cases cover visible retained playback, late clients,
input/image locking and round5 return. [Revision evidence](reports/feedback-2026-09-30/round-timing-replay.md). Earlier [protocol98 evidence](reports/feedback-2026-09-30/round-freeze.md)
covered the former uniform10-second frozen boundary and is not peer qualification
of protocol100.

Latest timing revision: [match UI evidence](reports/feedback-2026-09-30/match-ui-revision.md). Actual protocol100 peers remain unqualified.

Read [AGENTS](../AGENTS.md),[working rules](WORKING_RULES.md#gameplay-and-authority),
[skill contract](SKILL_NETWORK_CONTRACT.md),then the current NET-SKILLS-1 queue and
[multiplayer evidence](reports/stability-2026-09-27/multiplayer.md).
This is a source map,not another backlog or a declaration of complete replication.

## Runtime Ownership

A frozen c55574cd6 internal Windows player now passes a real two-process direct
LAN check through round2, with matching protocol/seat/character/defender state.
[Exact scope and limitations](reports/feedback-2026-09-30/player-lan.md). This does
not qualify unexercised skills, online/ranked, loss, reconnect or later integrations.

Protocol97 also gates the requested3m ordinary defender lunge. Local prediction
and host resolution share the derived impulse; the movement ceiling remains28m/s.
Bot attempt distances, safe-emote clearance and pressure stats follow actual reach.
Local native travel/sweep checks are in [the lunge report](reports/feedback-2026-09-30/defender-lunge.md);
they do not qualify real peer transport.

Legacy seat-assignment reception now requires exactly4bytes and a valid player
seat0..3 or spectator-1 before notifying local controls. Sender and repeated-seat
semantics stay intact; protocol97 is unchanged. Native19-case receiver proof and
the reproduced malformed baseline are in [seat packet evidence](reports/feedback-2026-09-30/seat-assignment-packets.md).

Runtime files below are in `Assets/TumbangPreso/Runtime/`.

| Concern | Entry points and invariant |
|---|---|
| Session/hello/seat ownership | Net/NetSession.cs,Net/LobbySession.cs,NetAuthority. Authentication or stable ID does not grant another seat. Read the current hello/protocol,not old literals. |
| Presentation teardown/reconnect | NetSession.EndTransportPresentation cancels local match presenters and hitstop before normal speed. Fresh MatchRpc handler binding resets received cohort/pending-request state,not the host lifetime sequence. Ordinary Cancel retains duplicate protection. |
| Ready and countdown | ReadyGate and MatchRpc use protocol83 match identity (zero only for lobby READY). Exact payload bounds and current seated membership precede votes; quorum waits for host loading. Manual votes retry until countdown acknowledgment, and a completed countdown stays consumed until the gate explicitly reopens. |
| Intermission votes | BufferSkipVote and MatchRpc use protocol88 match/round requests and host tally/seat-mask acknowledgement. Requests accept seated clients, not only the host. World state precedes tally; client buffer state mirrors without authoritative round events, and client Update does not erase the received count. |
| Rematch lifetime | Protocol89 rotates the host's PresentationMatchId from the ended match before reloading. The previous/next announcement adopts only a matching forward transition; repeated announcements cannot reload twice. Votes/tallies identify the old result, validate current seated membership and acknowledge the local seat. Existing rotation/result policies stay separate. |
| Discovery and ranked/casual pairing | Net/ServerQuery.cs,Matchmaker.cs,MatchmakingCandidateCache.cs; Core MatchmakingRules owns pool/band rules. Skill contract and reserved-seat capacity filter automatic pairing. |
| Requests and received state | Net/MatchRpc.cs and its partials. Check sender,seat,match/round,epoch,request/event freshness BEFORE gameplay or presentation. Ordinary requests/refusals/actions/charge tells share GameplayActionScope; it scopes context,not per-request receipts. |
| Skill identity/authority | Abilities/HeroAbility.cs,AbilityNetworking,HeroAbilitySystem; Net/SkillCastMessage.cs. Stable IDs and explicit initial/command intent,not temporary art names. |
| Cooldowns and charges | AbilityResourceSnapshot,MatchRpc.AbilityResources and HeroKit.AllAbilities. Scoped,ordered hero/ability identities include both roles; validate the complete set before mutation,and retain owner-live anti-refund behavior. |
| Timed skill recovery | TimedKitState and MatchRpc.TimedKits bind both optional channels to stable ability IDs, world/body scope and per-seat order. Age against the bound ability's duration, keep consumed-state guards and reset receive cursors on new transport. Specialized flight recovery remains separate. |
| Recovery aging | Generic timed and familiar recovery use positive progress of the host's remaining round clock, matching prepared-world aging. Pauses/introduction holds do not age simulation effects; non-live rounds reject nonempty recovery. Do not substitute server wall time for gameplay time. |
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
| Live familiar recovery | FamiliarEffectState and MatchRpc.FamiliarEffects bind the seance to world/body scope, stable hero/ultimate IDs and accepted phase. Active duplicates, older phases and completed lifetimes cannot recreate its field or end a newly active role skill. Retired possession poses are not part of this recovery contract. |
| Effect cleanup ownership | PaeteHeroKit's ultimate tracks its exact fresh sentries and adopts recovered owner instances through IWorldEffectBinding. One kit reset must not destroy other casters' effects. |
| Contact presentation | Visual/MatchFlair announces accepted outcomes with match/round scope. Ordinary player tag posing consumes the existing actor/subject/contact point; its supporting visual step and bounded hand aim cannot award a hit or move a motor. UltimateImpact reuses HitFeel on the victim's own view; no gameplay mutation or caster confirmation. |
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

## Score And Stock Packet Bounds

Score requires exactly12remaining bytes. Last Tsinelas checks its8-byte header,
existing0-4count bound and exact declared table length before any stock update.
Both reject truncated/trailing messages without events or partial state changes.
Published layouts, count semantics and protocol97 remain unchanged.
[Native before/after evidence](reports/feedback-2026-09-30/score-stock-packets.md)
separates packet-handler checks from actual-peer qualification.

## Map Ballot Packet Bounds

SelectMapVote requires exactly one integer and a sender ID representable by the
lobby's peer key. MapVoteTally validates its count, exact payload size and every map
entry before applying any ballot. QueueVoteState requires exactly its published
28 bytes. Existing packet layouts and map-selection rules remain unchanged.
[Native malformed/valid packet evidence](reports/feedback-2026-09-30/map-vote-packets.md)
also records four previously unrun rematch/intermission cases now passing locally.
Actual-peer qualification remains separate.

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
