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
open. Shared basic cooldowns, Sit/Fetch/Catch/Haunt behavior and ultimate cost are
not completed by this change.

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
