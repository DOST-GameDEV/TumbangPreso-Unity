# Interrupted lunge cannot tag after recovery

## Reproduction

Start a legitimate host lunge with the attacker out of reach. Interrupt the
lunge immediately with a one-second stagger, or end the round. After recovery
or the next round begins, move the attacker into the old sweep without starting
another lunge. The baseline awards a tag in both cases.

CombatVerbs returns early when the motor cannot act, before decrementing the
active sweep timer. This pauses an already cancelled contact window and lets it
resume later with its old origin. The visual gesture can have ended already.

## Correction

Retire the lunge contact window on round inactivity, stun/trip or fear. Preserve
its frozen state for presentation holds and offline pauses, so Resume does not
silently cancel a legitimate attack. Cooldowns, impulse distance, host authority,
scoring and protocol remain unchanged. No character-specific ability changes.

## Validation

Native Unity6000.5.8f1 Linux OpenGL, isolated profiles:

- Baseline2/2fail: stagger and round-end cases each award1tag instead of0.
- Final3/3pass: both interrupted attacks stay retired; a genuinely paused lunge
  stays motionless, then resumes and awards exactly1legitimate tag.
- Test-case time4.04seconds. No new OOM or guard stop.

[XML, logs and exact inputs](lunge-interrupt-checks/).
This qualifies actual local host outcomes. New actual-peer, all-roster visual and
refreshed-player checks are separate. Existing uninterrupted travel qualification
is unchanged; the active branch is not edited.
