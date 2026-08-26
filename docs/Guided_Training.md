# Guided training

## Purpose

The existing **How to Play** panel stays as the reference manual. Guided training is a
separate button inside that panel for players who learn by doing. It follows the useful part
of Valorant's onboarding structure: one objective at a time, a clear action prompt, a marked
world target, immediate completion feedback, and the real controls in the real game.

It is not a scripted video and it does not perform verbs for the player.

## Route

The session launches locally on Eskinita in Hero Strike so every shipped control is available.
Bots become stationary training dummies and the round clock holds at 90 seconds. The player
completes these lessons in order:

1. Look and aim
2. Move
3. Sprint and read stamina as one danger-box crossing
4. Jump
5. Charge and release a normal throw
6. Retrieve the player's own tsinelas
7. Add spin and release a Pektus throw
8. Shove an attacker dummy
9. Hold the ability information panel
10. Cast Skill 1
11. Cast Skill 2
12. Cast a one-time training-funded ultimate
13. Swap to defender and reset a down lata
14. Punch a vulnerable attacker
15. Charge and release a lunge
16. Mash the live jump binding to recover from a trip
17. Choose and play an emote

The objective card always uses live binding labels. `N` skips a lesson for accessibility and
debugging. `Backspace` exits training. Completion returns to the main menu with `Enter`.

## Rules

- `GuidedTraining` observes `InputIntent`, cooldowns, projectile state, cast answers, lata
  state, trip recovery and emote state. It never calls throw, shove, punch, lunge or ability
  activation methods for the player.
- Setup actions are allowed between lessons: freeze bots, position a dummy, equip the player's
  own tsinelas, restore or knock down the lata, switch the derived defender round, refill the
  training ultimate once, and apply the trip that teaches recovery.
- The mode is local-only. It creates no network session and carries no replay or score state to
  another peer.
- Training holds the round clock but leaves movement, physics, real cooldowns, protection,
  stamina and gameplay rules active.
- Leaving the arena clears `GameLaunch.GuidedTutorial`, including exits through the pause menu.

## Acceptance

- The original tutorial pages still open and turn exactly as before.
- A `START TRAINING` button appears inside the tutorial panel.
- All 17 objectives can be completed with player input or skipped with `N`.
- The player experiences both attacker and defender verbs in one session.
- Core tests, runtime compilation, EditMode compilation, PlayMode compilation and a standalone
  player build pass.
