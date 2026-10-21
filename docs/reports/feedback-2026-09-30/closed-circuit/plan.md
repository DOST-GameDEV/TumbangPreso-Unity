# Closed Circuit implementation plan

Current source still installs PlaceholderRoleAbility for Zack's defending slot.
The live Abilities tab and researched Electro design specify6m acquisition,
.4s maintained visible aim,2s Zapped,35s cooldown on successful commitment.
Overclock offers one optional different-target acquisition within1s, requiring
another full .4s tell. This plan implements that scoped requirement; it does not
reopen the other completed Electro units or their authored assets.

## Decisions before code

- Do not use HeroAbility.Windup: it roots the caster and is deliberately
  uninterruptible. Acquisition must remain cancellable and ordinary movement
  must continue. A dedicated ability phase owns its short acquisition clock.
- Lock one eligible target at the press; never silently transfer to another.
  Require a living, untagged rival in front, within6m and inside the actual aim
  ray's capsule-sized tolerance. Recheck range, aim and non-trigger world LOS
  throughout the hold. Full nonalloc ray buffers fail closed.
- Cooldown remains unspent until successful contact. A failed first acquisition
  clears without Zapped. A failed second acquisition ends the optional window
  while retaining the first cooldown. Amped-Up cannot refresh the circuit while
  its acquisition/follow-up sequence is active.
- Only the host applies Zapped through the existing status API. Existing status
  snapshots remain the authority for victims; no new scoring or movement state.
- Live camera aim is not currently sent in the normal remote motor input. The
  existing AbilityAimSnapshot intentionally excludes destinations. Add a bounded
  owner-to-host circuit-aim message only during acquisition, with scope, sequence,
  finite/range validation and a stale-input cancellation deadline. Do not pretend
  the initial cast snapshot proves .4s of maintained aim.
- Use a dedicated bounded host circuit-state message, not overloaded signature
  movement fields. Include scope, sequence, phase, target, first victim, remaining
  phase time and cooldown. Age late snapshots without restarting the lock, beam,
  hit or cooldown. Reject invalid flags/targets, stale sequence, wrong round,
  epoch, hero and non-owner/non-host senders. Increment protocol compatibility.
- Flush newly started state after the accepted cast message; avoid a snapshot
  arriving first and then being restarted by its cast playback. Pending/idle
  corrections close failed local presentation rather than applying status twice.
- A narrow target-facing electrical tell must exist throughout the lock, disappear
  on cancellation and not advertise a successful hit before host commitment.
  Reuse the authored Electro palette. Keep SFX suppressed until actual listening.
- Bots get the same range/aim/LOS requirements and ordinary reaction cadence,
  not perfect tracking or a bypass. Their old throw-buff branch does not cover
  the defending slot and must be split narrowly.

## Owned work to reserve before editing

ZackHeroKit.cs and a new ClosedCircuit partial; a small circuit tell component;
new bounded circuit wire state/MatchRpc partial; named-message registration and
snapshot call sites only in MatchRpc.cs; NetSession protocol; narrow AIController
Zack defending branch; focused native circuit tests and serializer checks.
Do not touch the private HeroHazards overlay or the reliability worker's build.

## Smallest acceptance

1. Real host target is not Zapped before .4s, then gets2s and35s cooldown;
   ordinary movement remains possible, no score is minted.
2. Aim loss, wall, range, tagged target, impairment and round change cancel.
3. Overclock second target needs another full tell; same target/third cast and
   objective cooldown refill during the sequence cannot bypass the limits.
4. Remote owner aim is actually consumed; spoofed/stale/wrong-scope input fails.
   Replica/recovery cannot apply gameplay or restart expired state.
5. Native tell visibility/cleanup; actual matching-peer qualification when a
   fresh bounded player candidate can be built. Compilation is not peer proof.

Retain failure evidence, run one focused pass with at most one bounded tooling
repair, and do not mark the whole Zack feedback row complete from this unit.
