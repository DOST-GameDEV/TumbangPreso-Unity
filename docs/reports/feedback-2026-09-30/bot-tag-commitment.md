# Bot tag commitment

## Confirmed failure

When a defender bot had already started holding a lunge, a target entering punch
range selected the punch branch. That branch returned without touching Lunge.
The Hunt action's untouched-button sweep released it, so the real combat consumer
fired both a punch and the previously charged dash. Both cooldowns were spent on
one target opportunity, including a needless dash after the punch had landed.

The native controlled-input reproduction seeds a0.2s charge through CombatVerbs'
actual input consumer and mirrors that held time in the planner. It then executes
the actual Hunt action, including its release sweep, followed by the real punch /
lunge consumers against a live, taggable nearby seat. The baseline charged case
fails with two cooldowns spent; the fresh nearby-target case already passes.

## Correction

A punch is selected only when no lunge has already been committed. An existing
charge continues through its ordinary aim/hold/release logic; a fresh close target
still gets the immediate punch. AI still writes InputIntent, not gameplay results.
No human controls, cooldowns, ranges, hit rules, kit behavior or protocol changed.

## Evidence

Unity6000.5.8f1 Linux64, named isolated cloud-bot-tag-commitment profile.
The same two native cases pass after the one-condition correction. Runtime and
test inputs remained frozen; no assertion or fixture repair was needed.

- Fresh close target: punch cooldown only, no lunge cooldown.
- Already charging, now close: finish the committed lunge, no extra punch cooldown.

[Before: one pass, one double-action failure](checks/bot-tag-commitment-before.xml).
[After: both pass](checks/bot-tag-commitment-after.xml).

This proves the controlled planner/input transition, not full-match bot win rate,
rendered movement, all difficulty profiles or actual transport peers. The actors'
continuous Update is disabled to isolate the exact input decision; this is not a
claim of an unattended full match. Broader behavioral qualification stays separate.

The Feedback-row addition is pending: the shared-document write was blocked by
an older reservation, and the owner has been asked to confirm current row access.
The tested code work proceeded independently; no document write was bypassed.
