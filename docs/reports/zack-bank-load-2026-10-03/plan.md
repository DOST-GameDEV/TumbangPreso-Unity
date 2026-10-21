# Bank Shot load gesture

Status: implementation and scoped evidence now available in [implementation.md](implementation.md). This original plan is retained; remaining visual acceptance is explicit there.

## Current source and reference

BANK SHOT loads the already-held slipper for8seconds, with35second cooldown and
85percent first-bank speed retention. Its cast still requests hero-zack-charge
and the overcharge owner gesture. The latter is explicitly directed as the old
magnet recall: an off hand reaches out and the right snaps the arriving shoe to
the chest. That action no longer describes loading an object already in hand.

Reinspected the retained Keqing35/44/50-second stills linked in the existing
[Zack reference](../hero-reference-footage-2026-10-02/zack.md). The useful features
are a small opposing preparation, directed free hand and clear single-body
recovery. Reject the sword, distributed bodies, broad bright crossing lines and
covered floor. This pass is still-frame inspection, not new continuous viewing
or audio listening. The source report's provenance and limits remain applicable.

## Direction

Zack briefly looks toward the real slipper, brushes a compact angular charge
across it with his free hand, then settles back into his normal grip. The held
shoe stays visible and attached throughout. Keep the body's lateral confidence;
do not copy Sean's shoulder-loading ritual or make the slipper arrive from air.

Six authored beats, cosmetic only:

| Time | Read |
|---|---|
|0.00|Existing grip and neutral base|
|0.08|Small opposing shoulder preparation; eyes toward the shoe|
|0.18|Free hand approaches the held slipper; right hand offers its surface|
|0.28|Short brush/trace and contained settle, no fake impact|
|0.42|Free hand withdraws; shoe returns to its normal read|
|0.64|Neutral recovery|

No new gameplay windup: the existing charge is active immediately. Throwing at
once must interrupt the cosmetic gesture and consume the real loaded throw.
Walking remains legal and its legs must continue. Existing loaded-shoe poles
remain attached to the actual world/owner shoe; no new unrelated particle cloud.
The separate true-wall-contact fork from the broader plan remains a later unit.

## Scoped implementation

- Append hero-zack-bankshot to Zack's existing GLB; preserve geometry, all37
  existing animations, original binary data and model metadata/GUID.
- Wire the one appended clip into person_zack through a scoped Editor command.
- BankShotAbility changes only its body/owner action identifiers. Names, copy,
  cooldown, duration, bank retention, Overclock and protocol remain unchanged.
- Add the dedicated bank-load owner clip/path and body action chain. Preserve
  all existing paths, especially unrelated Amihan/Phaister/Paete authored work.
- Include this mobile upper-body action in the existing gait exception.
- No sound changes in this unit; no listening or sound-quality claim.

Reserved scope: author_zack_bank_shot.py; team-zack.glb; person_zack.asset;
ZackBankShotMotionAuthor; ZackHeroKit.BankShotAbility action identifiers;
CharacterAnimator action-chain/gait entries; ViewmodelArms bank-load dispatcher,
new ZackBankShot partial and bank-only CastGesture entry; focused motion tests
and ZackBankShotTests. No shared Slipper collision or network-packet edits.

## Acceptance

Serialized own-clip resolution, neutral recovery and original asset preservation;
actual accepted cast/held-shoe identity; immediate throw, expiry and role/round
cleanup; actual walking legs; standing owner/body controls. Inspect native front
and side motion before effects, then held-shoe/owner views as resources permit.
A native pose strip is not full-court, actual-peer, packaged-player, audio or
human acceptance. Preserve failures and scope any RAM-blocked stage honestly.
