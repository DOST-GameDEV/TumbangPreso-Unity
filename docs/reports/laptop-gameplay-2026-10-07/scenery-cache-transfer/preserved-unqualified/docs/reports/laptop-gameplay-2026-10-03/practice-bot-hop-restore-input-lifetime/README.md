# Practice bot hop input across removal and restoration

Status: fixture preflight pending; no native original or candidate result yet.
No runtime change has been made for this lead.

The shipping hop producer publishes a held Jump and buffers its press. Public
PracticeRange.SetBot removal disables the producer, whose OnDisable resets its
private action state without clearing that intent. Restoration teleports the bot
and holds its motor for the spawn-settle frames; those callbacks return before
Intent.CommitFrame. The original hypothesis is that an old hop remains available
when the first non-settling motor callback runs.

The five proposed cases generate a real hop through StepHop, then use public
removal/restoration. Two distinguish retained edge and actual upward motor travel.
Three controls require an uninterrupted physical hop, restoration without an old
hop, and a fresh physical hop at the restored position. Grounding comes from forty
manually invoked shipping motor callbacks on a supplied clear floor. Public
snapshots keep the target bot the defender throughout; a feet-based capsule avoids
spawn depenetration being confused with a jump. No private hop clock, held state,
grounded flag, velocity or buffered input is seeded. Unity RNG state is restored.

The prepared practice range supplies readiness and registration through the
existing PracticeProducerResetLifetimeTests setup. Shipping FixedUpdate and full
AI Update callbacks are invoked manually in FixedUpdate-before-Update order during
spawn settling. This qualifies supplied producer/lifecycle/motor behavior, if it
passes; it does not qualify natural scheduling, full planner cadence, actual menu
operation, authored map geometry, physical devices or peer delivery.

AIController, CharacterMotor and InputIntent remain read-only. The hypothetical
suppressed-body extension was rejected: the shipping temporary suppressed AI is
created on the local possessed body, while SetBot excludes the local seat; the
ordinary practice bots already have their own AI. This is not evidence for a
selective buffered-input or global AI change.

Original source is logical ASTRA d34cffb237f9f39182b576137fc543106cb377fa.
PracticeRange working SHA256 c10edc7ad5cd825c5702958cd194b4f05af510e7e309cbe93a540588bc19e8aa
(LF f606dd719a1fe463a00071d621ed47c4cee19c95809982e810f66b40a7d94992).
AIController working SHA256 a159448c8b4beb65cfe819fde0dc560c2f536efb432b593babf4c1e4884b095a.
CharacterMotor working SHA256 c711c7cc676cc1b604784b0023766feb55a82b787277f2f788558e820bddf9ca.
Fixture SHA256 53645c17b04255bbf780e6d73131cedc5d8fa4254ae074335ad913396fa48ec3;
meta f246f3ec7675408b7e9cfa1ed3d33e8ace30e1d656e44a32dfe5511b186e278a.

Preflight qualification: the current shipping SetBot callsites are training-menu
controls under offline pause. While BlocksInput is true, AI.Update ordinarily
calls ReleaseAll and clears this default bot's hop before a later menu choice.
The unpaused public-API fixture therefore does not establish an ordinary paused
menu defect. Scope/value acceptance is pending before any native run or patch.
