# Skill network contract

## Optional spectator view data

The spectator-view candidate uses a separate capability-negotiated cosmetic
channel. It does not extend movement, cast or score packets. Versioned Hello/Ready
messages bind a transport nonce; a spectator subscribes to one seat and the host
enables only the required owner's camera publication. A legacy peer does not get
a repeated unknown pose stream.

`SpectatorViewState` owns its bounded codec: camera eye/full rotation/FOV, near-fade
eye drop, viewmodel pose/action/aim/time and match/round/body epoch/serial. Only a
current seat owner may submit; only the host relays to subscribed spectators.
Validate complete payload, identity/scope, finite/range values, age and ordering
before updating presentation. Camera data cannot drive a motor, contact or score.
Native codec/local-view tests are scoped evidence; real-peer qualification is
still required before this candidate is integrated.

Live authoring rules, 2026-10-25. Verify details against the owning codec and
runtime path before editing. `NetSession.ProtocolVersion` and
`SkillContractFingerprint` own compatibility. The complete
[protocol history and earlier detailed contracts](archive/cleanup-2026-10-25/docs/SKILL_NETWORK_CONTRACT.md)
retain all previous layouts, measurements and evidence.

## Authority and identity

- Host accepts gameplay outcomes; replicas only present them. `MatchDirector.AddScore`
  owns score. Cosmetic playback must never damage, catch, pick up or reset the can.
- Requests bind sender/seat ownership, match, round and movement epoch through
  `GameplayActionScope`. Validate before consuming any per-body serial cursor.
- Stable hero, ability, roster, field-kind and event IDs are append-only. Retired
  kinds stay reserved; Cinder Gate is retired, not a slot for a new field.
- Casts, commands, recovery and cosmetic receipts have distinct purposes. Reject
  duplicate/stale/out-of-context events without rewinding or extending live effects.

## Existing routes

| State | Contract and consumer |
|---|---|
| Cast intent/acceptance | HeroAbility, AbilityNetworking, HeroAbilitySystem, SkillCastMessage; stable kit and ability identity |
| Predicted cast delivery/refusal | SkillReceipts, SkillDelivery; bounded ordered receipts, no duplicate execution |
| Body and aim tells | SyncUnit, AbilityAimSnapshot, AimReplication; shared presentation, private target guidance stays local |
| Cooldowns/charges | AbilityResourceSnapshot, MatchRpc.AbilityResources; validate full identities before mutation, preserve owner anti-refund behavior |
| Timed buffs/passives | TimedKitState, MatchRpc.TimedKits, ITimedKitReplication; stable ability binding, independent channel clocks |
| Persistent fields | PreparedWorldSnapshot, WorldEffectSnapshot, IPreparedWorldReplication, IWorldEffectBinding |
| Persistent lifetime/targets | AcceptedCastEvent, InstanceId, TargetMask, SentryTargetState; exact host lifetime and target set |
| Familiar/ghost bodies | FamiliarEffectState, NemuGhostFrame, companion snapshots; typed host state, no replica contact decisions |
| Held interactions/can carry | leased input, host reset/reclaim paths, Lata.Carry and tagged SyncLata state |
| Ultimate cohort/cutscene | SharedUltimatePhase, UltimateCommit, MatchRpc.UltimatePhase; sealed duration and current kit/body identity |
| Contact/audio/victim feedback | MatchFlair, HitFeel and shared cue routes; accepted event, correct actor/victim and perspective |
| Recorded playback | RecordedMatchClip/RecordedWorldView codecs; presentation-only objects, supported schema checks |

## Recovery and cleanup

- Restore persistent state at elapsed simulation time, never by recasting. Repeated
  hydration cannot replay a hit or extend a lifetime. Host round-clock progress ages
  effects; paused and opening/ultimate-held simulation does not.
- Validate a complete snapshot before any mutation. Check count/length bounds,
  finite/range-bounded values, IDs, authority, epoch, round and sequence.
- Bind missing bodies once when the matching body/kit arrives; old data cannot
  attach to a replacement body. Reset receive cursors on a new transport.
- Remember retirement before late installation. Cleanup is scoped to the exact
  owner/lifetime, never all casters' fields. Reset/despawn/round/role/kit changes
  cannot revive a consumed field or replay an old possession.
- Cooldown/resource recovery preserves a newer local issued request. A valid no-op
  may still retire its sequence; older snapshots cannot refund a settled action.
- An ultimate's hold, preparation and handback use one shared clock. Missing caster
  art cannot shorten the host-sealed duration or apply gameplay twice.
- Every view needs current held/loose slipper and can state. Carry snapshots draw
  pickup/walk/set-down/drop; observers never independently complete a reset.

## Cosmetic reworks

Use existing body/first-person/cast/state/effect hooks. Preserve the authored kit,
model, clips, VFX, sound and timing unless the current task authorizes a change.
Do not add an ad hoc RPC for an art swap. A genuinely new mechanic needs an explicit
reusable state and recovery contract, with late join and cleanup included.

Spectator first person must observe the target's actual replicated look and
perspective presentation without enabling that player's input or writing its
motor. Cutscenes, stealth/ghost visibility, props and victim-only feedback need
explicit observer rules. Spectators must not inherit private targeting knowledge
unless the product's spectator contract permits it.

## Compatibility and verification

Wire or gameplay-semantic changes require deliberate compatibility review and
the existing protocol/fingerprint updates. Cosmetic/input-only work does not
automatically change the protocol. Saved replay schemas retain old meanings.

Run `tools/audit_wire_payloads.py`, `audit_wire_finite.py`, authority/cue audits
and relevant Core/native tests as applicable. Prove sender rejection, duplication,
late delivery, round/role replacement, pause, cleanup and hydration. Then exercise
actual host, owner and observer/rejoin peers; a local test does not certify a
network path. See [TESTING](TESTING.md) and [NETWORKING](NETWORKING.md).
