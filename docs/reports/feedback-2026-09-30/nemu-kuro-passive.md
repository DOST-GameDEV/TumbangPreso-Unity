# Nemu Kuro Passive: Cooldown Movement Bonus

## Corrected behavior

The current Wiki Kuro passive specifies 10 percent faster movement while Kuro is
on cooldown. NemuHeroKit previously returned a constant movement scale of 1.
It now returns 1.1 while any signature, attacking or defending basic-ability
cooldown is positive, otherwise 1. Multiple cooling abilities do not stack.

The existing CharacterMotor movement calculation consumes this scale. The getter
reads the existing ability clocks, so expiry, round resets and authoritative
cooldown corrections remove the bonus immediately without another state flag.
No wire format, protected hero behavior, authored asset or tutorial file changed.

Source: [current Wiki abilities](https://docs.google.com/document/d/1jvr7NLzhHrbw-wrG676AeOkoTxJf4GokkfmxpO0ddLg/edit?tab=t.0),
Nemu's Kuro passive, read 2026-09-30. This is one coherent part of F0930-12 and the
Feedback row about other complete character specifications. The full row remains
open. The first movement-only change did not complete shared basic cooldowns,
Sit/Fetch/Catch/Haunt behavior or ultimate cost. The next section records the
subsequent shared-cooldown unit; the other kit behavior remains open.

## Reproduction and validation

Base: 83136899, with only the owned Nemu passive fixture added for reproduction.
Unity 6000.5.8f1, Linux64, graphics enabled with Mesa llvmpipe. The guarded launcher
ran in a separate complete candidate with a named cloud-nemu-passive profile.

- Before the correction: 8 native EditMode cases, 1 passed and 7 failed. Every
  failing case expected 1.1 and received 1.0.
- After the correction: 8 native EditMode cases, 8 passed, 0 failed or skipped.
- Cases cover each of the three basic clocks, non-stacking, ready-state control,
  expiry, round reset and authoritative cooldown correction.
- Frozen input hashes were unchanged during each run. The test fixture and
  CharacterMotor were identical between the before and after candidates.
- Initial workstation qualification also passed 3 existing AiLungeRulesTests in
  native Unity. The separate .NET Core suite passed 681/681 before this unit.

These are actual native contract checks, not merely compilation. They do not
qualify real-player movement feel, physical input, actual peers, a new player
build or the broader Nemu kit. No extra gameplay wire semantics were introduced.

Generated changes to the two protected composition-redesign PNG metadata files
remain only in the isolated validation candidate and are excluded from commits.
The source checkout preserves the local contributor's tutorial and Doc reservation.

## Evidence identifiers

- baseline.xml: SHA-256 617eaef2c3704f98f0baab24335024402c0a862d6260bc7eba9a9c4e31dee78d
- fixed.xml: SHA-256 1032f53a33ca60f37bb2e7c4ad9e5c51fe4044d852b274756d466779b165d037
- baseline-inputs.json: SHA-256 6fbd16e217abd331e9af2afed78ad015243f46aa5421880281693f953f4ff46c
- fixed-inputs.json: SHA-256 223511a60260b3897ccf66ce9711a2f85865aaf459300175f2239602e7c99dd4

Native fixed run: 2026-09-30 03:42:08Z to 2026-09-30 03:42:08Z; 0.1090302 seconds for the tests.

## Shared Basic Cooldowns

The same current Wiki defines a 25-second cooldown for each basic ability and
requires a basic cast to put all other non-ultimate abilities on that cooldown.
The former kit instead had separate 40/30/30-second clocks.

The basic durations now use NecroRules.BasicCooldown. Each accepted basic
activation shares its actual cooldown with the three existing basic abilities.
This hook also runs when observing clients replay an accepted activation directly,
rather than depending only on the local input path. Ultimate charge and cooldown
remain separate. All three existing clocks continue to tick and reset normally.

A shared resource group also needs coherent host receipts. HeroKit now provides a
small authoritative-resource hook; independent kits retain their previous behavior.
Nemu corrects all three clocks together on the newest response, including a refusal.
A response from an older other-slot request cannot overwrite a newer pending OR
already accepted cast. Duplicate receipts retain the existing rejection guard.
No packet schema or protocol field changed, and the existing host authority,
request identity, scope and sender checks remain in place.

Native evidence on Unity 6000.5.8f1 Linux64:
- Before shared-clock correction: 13 cases, 9 passed and 4 failed, reproducing
  the incorrect initial values and independent ticking.
- Final fixture: 18 cases, 18 passed, 0 failed or skipped.
- Added receiver checks use the real ResolveSkillReceipt method on isolated
  components with a predicting-owner provider. They cover latest acceptance,
  refusal, duplicate answers, reordered other-slot answers before and after
  later acceptance, plus an independent Zack-kit control.
- All frozen candidate input hashes remained unchanged during the final run.

This qualifies native activation/resource/receipt logic. Actual transport, actual
peers and player-feel checks are not claimed. Sit, Fetch delivery changes, Catch
can protection, Haunt and the ultimate price still require their own coherent
implementation and acceptance. The parent Feedback row remains unfinished.

- shared-baseline.xml: SHA-256 030cd4598fc76b209efdc54e42f08ac7317230c8bed7a9c09a0ff78b46279355
- shared-fixed.xml: SHA-256 98f1b2641260a2114e6043353d95a21b904d61bdec7fbeb87431ca92ac54a644
- shared-baseline-inputs.json: SHA-256 a2940e2a0667bdcac57eefc1eb7ead996cd841fb7e5c5cacc539eb573724f70e
- shared-fixed-inputs.json: SHA-256 2ed14bc1006b3094150bbc27393d768f0a1324ebc99f6902185c7d50426993db

Shared fixed native run: 2026-09-30 03:57:19Z to 2026-09-30 03:57:19Z; 0.1115132 seconds for the tests.
