# Enabled practice producer state at public world reset

Date: 2026-10-03. Baseline `65acbd3d3`. Original five-case native proof
completed after the sole fixture repair: three causal
failures and two passing controls. The additional original custody control
passes, and the first composite candidate passes all six. Main audited the
exact source pair/fixture and terminal preservation before publication. Main
owns the separate PracticeRange call-site and combined ASTRA integration; this
helper-only branch does not provide standalone reset-suite acceptance.

## Distinct producer boundary

Public ResetRange permits an active requested game clock. It cancels Carrier
and Combat commitments, resets kits and teleports the world, while an enabled
AIController can keep private hold clocks/remembered input from before reset.
Teleport does not disable that producer, and ResetRange does not raise the
producer's ordinary retirement callback. Normal paused-menu updates can already
clear those clocks through ReleaseAll, so this is a separate legal public
active-reset boundary, not a newly demonstrated normal menu failure.

The existing ReleaseAll(Intent) primitive resets action state and owned input,
while AbilitiesEnabled false preserves shared human hero keys. Reuse that
behavior only after causal proof. Do not reset Plan, reaction gates, difficulty,
personality, resources or lifecycle subscriptions/disposals. Full world reset
already refills stamina and resets kits by design; that does not mean the new
producer helper itself may introduce resource refunds or retuning.

## Frozen original gate

The original gate uses the first five cases in
`TumbangPreso.PlayTests.PracticeProducerResetLifetimeTests`: three causal failures
and two passing controls. One subsequently approved disabled-custody control is
run separately against the original, then the composite candidate uses all six.

- Active public reset retires an enabled bot's old lunge clock and owned press.
- A post-reset lunge starts with a fresh 0.1 s producer clock.
- Shared human Skill1/Skill2/Ultimate remain held while the body's old lunge
  clock/press retires.
- An uninterrupted producer continues its existing hold.
- A refused reset leaves live producer state intact.

The bot's shipping StepLungeIntent creates the real hold, with a supplied 0.2 s
step. A public missed punch spends its ordinary cooldown, making lunge eligible
without fabricating a private cooldown. The producer remains enabled. Readiness
and seat registration use the same supplied seam as PracticeResetCombatLifetimeTests;
public ResetRange, shipping world teleport and held state run unchanged. A flat
primitive floor keeps geometry stable. This qualifies producer/reset behavior,
not full bot-planner cadence, actual menu navigation or map loading.

Both hooks reset the world and restore provider, launch, network, sandbox, stats
and can state. No user profiles, source assets or authored heroes are changed.

Fixture SHA256:
`dc1566e357dfd3e7bf2fab246ccf30a4dac660356bb08abf894811ddab957510`.
Metadata SHA256:
`34263380cbd073174e31c4d564bdf1df1db46e5b5a29327ebc3039f17b71cac9`.
GUID: `ffe8a2fe394a4f47b1a7f8eb5608bca5`.
Original canonical Git65ac AI:
`b2e76a38d485057c2105a571029fa8583e69ee7ee112713d44e9cc4e79294213`.
Original canonical PracticeRange:
`98eaf33b164965639d472d8f24d5d88d9b2d2364cafe45a70a143aa3cb761f53`.
Working AI `a093874fe998e180a7ea178eea66b571cfb8708fb1199333f5a49535edf50871`,
PracticeRange `63558d8da72addb35cb5c36fcf078972b2d9aae6692270f3b60c7bf1a95a44ca`.
Working CRLF normalization equals the exact Git originals.

Stop at fresh exact five-case XML and terminal receipt. Producer eligibility,
hold creation or readiness failures are not public-reset cause proof. Keep
fixture/metadata/assertions unchanged between original and candidate. Main
implements only the proven ResetRange call-site after the producer helper is
reviewed; the implementation agent does not edit Main's reserved call-site.
No physical input, peer, performance, all-map, planner or competition claim.

## Retained setup failure and sole fixture repair

The initial original run produced five setup failures, with no reset cause
proof: every hold was -1 instead of the expected 0.2 before ResetRange, including
both controls. [Setup XML](native-setup-failed/tests.xml) and
[receipt](native-setup-failed/job-receipt.json) retain exit2/terminal/preservation.
The initial fixture hash was
`eebf61987253728cef0e4314b76db0307f299d427b5df2182bffaacedda72be5`.

The enabled bot had observed the target at its original position during the
setup frame. Later direct placement moved the actors but did not update that
perceived location. AheadOf uses that cached belief, so predicted reach could
exceed the lunge range. Punch cooldown 0.9 versus hold 0.5 correctly bypassed
punch preference, and the supplied 0.2 step could not reach the release threshold.

The ONE bounded fixture-only repair invokes the shipping Observe pass with a
supplied 2 s sensing integration after direct placement, then asserts the shipping
AheadOf point is in the actual Normal range. It does not seed private hold or
sensing state, alter gameplay, or weaken/change the hold/reset/control assertions.
Corrected fixture/metadata are frozen for a new original gate. No source patch
is justified by the failed setup; no further fixture repair/retry is planned.

## Corrected original proof

The corrected `qa-d/Logs/practice-producer-original5-observation-repair` produces
the exact three reset failures: enabled and shared-key producer clocks retain
0.2 s after reset, and the next hold accumulates 0.3 s rather than starting 0.1 s.
Uninterrupted hold and refused reset controls pass.
[Corrected original XML](native-original/tests.xml) and
[receipt](native-original/job-receipt.json) retain exit2, terminal state,
preservation completed and no lease held. The repaired five test bodies, setup
and assertions remain unchanged in the six-case candidate. The earlier five setup failures remain separate evidence;
they are not counted as causes or overwritten.

## Pending composite correction

The agent's AI-only helper working SHA256 is
`4b002e6109169a489cab155e4d5357725d98a9da946aff1a81c627b4d766be33`.
It exposes only the existing ReleaseAll(Intent) retirement primitive. Main owns
the separate PracticeRange call-site before the world reset. Candidate proof
must identify both source hashes; the helper-only branch does not by itself
invoke the new reset behavior and must not be described as standalone native
acceptance.

The call-site must retire only active enabled producers. A disabled controller
can remain attached to a human-controlled debug seat, with default hero-key
ownership; invoking ReleaseAll on it would clear the human's intent. OnDisable
intentionally only resets private state. The current five cases qualify enabled
producer behavior; a separately approved custody control is needed before
claiming native disabled-controller preservation. No production call-site is
edited by the implementation agent.

Main approved one additional native preservation control,
PublicResetPreservesHumanKeysOnADisabledAttachedBrain. It disables a default
hero-key-owning brain retained on a human seat, publishes shared hero held/press
edges and calls public ResetRange. The original must preserve those fields and
the disabled brain; the composite candidate must do the same. All original five
methods/setup/assertions remain byte-identical: removing only this added method
reproduces corrected five-case SHA
`a0ffe7ecb86f4044056fa41b33b8693ed440e420c882b5791eeb9c10892eb094`.
The metadata/GUID are unchanged. This coverage addition is not another fixture
repair and does not repeat the five-case original proof.

Main's guarded PracticeRange working candidate is
`98a3f2ce2ceb6bf66e581fcb896c01cd464a1ef8e5d35a21b52792fcfb1723b9`.
It checks a non-null active enabled producer before calling the helper in the
existing pre-teleport retirement loop. The helper-only branch still retains its
old call-site until Main's integration; native claims identify the composite.

## Composite native qualification

The separately added disabled-brain custody case passes against the complete
original AI/PracticeRange pair. [Custody original XML](native-custody-original/tests.xml)
and [receipt](native-custody-original/job-receipt.json) preserve that one passing
case. The existing corrected five-case original proof is reused unchanged.

Main's `qa-d/Logs/practice-producer-candidate6` passes all six on the first
composite candidate: AI working SHA
`4b002e6109169a489cab155e4d5357725d98a9da946aff1a81c627b4d766be33` and Main's
guarded PracticeRange SHA
`98a3f2ce2ceb6bf66e581fcb896c01cd464a1ef8e5d35a21b52792fcfb1723b9`.
The same six-case fixture/metadata are used. [Candidate XML](native-candidate/tests.xml)
and [receipt](native-candidate/job-receipt.json) retain exit0, terminal state,
preservation completed and no lease held; guard ended08:06:33Z.

One observation setup repair was used before causal proof, with the initial
five setup failures retained. No additional fixture repair, assertion weakening
or native candidate retry occurred. The custody coverage addition did not
change existing five tests or repeat their original proof. Main owns the final
call-site integration; the helper branch alone retains the old call-site and
its reset regression remains dependent on that integration.

This qualifies the enabled producer/public-reset boundary and disabled/shared
human input preservation through supplied readiness/sensing/hold-step seams.
Full reset's existing stamina/kit refills are not new helper behavior. No natural
planner cadence, actual menu/debug navigation, resource progression, physical
devices, peer, authored map, performance or readiness claim is made.
