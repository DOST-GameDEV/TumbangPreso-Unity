# Practice bot removal: retained lunge contact window

Status: investigation evidence only. No gameplay change or qualified fix.

The actual range menu calls PracticeRange.SetBot to remove and restore its existing
actor. Removal sets RoundActive=false and immediately disables the actor, so
CombatVerbs.Update never observes that interruption. Restoration reuses the same
component, sets RoundActive=true and teleports the actor without retiring its old
contact window. Pause intentionally preserves active contact windows; it does not
resolve this removal boundary. Resuming can therefore run SweepLungeTag from the
pre-removal lunge origin after the actor has been repositioned.

The attempted native fixture uses actual held/released InputIntent to open a lunge,
the actual prepared PausePanel and public SetBot removal/restoration. Real
CharacterMotor, CombatVerbs, Lata and range configuration dependencies live in a
minimal arena-classified scene. Only startup readiness is supplied directly to
avoid a populated-map or match-startup test. There is no physical movement, actual
victim tag, networking or shipping-map acceptance claim.

## Runs and stopping condition

- Original run70621 produced exactly3 cases: the stale-window assertion failed
  with0.446414173 seconds retained, uncharged removal passed, and the ordinary-pause
  control failed during setup because the fixture did not select a playing clock.
- The ONE allowed fixture repair explicitly requested the normal playing clock
  in setup. Gameplay source and all behavioral assertions remained unchanged.
- Corrected original run81031 produced exactly3 cases: the same stale-window
  assertion failed with0.449515074 seconds retained and uncharged removal passed.
  Ordinary pause preserved its contact window correctly, but the control's final
  expiry assertion expected exactlyzero and saw-0.000199135393. The existing timer
  decrements below zero on its last active tick, so this was a second fixture defect.
- The route stopped at that second fixture defect. No second repair, product patch,
  candidate run or forced passing assertion was attempted.

Both original runs and their receipts are retained. This is repeated direct
evidence of the retained-window lifecycle boundary; the attempted three-case gate
did not qualify it or a fix. A proposed minimal correction would retire the contact
and windup state on actual component disable while preserving ordinary pause and
cooldowns. That proposal has not been implemented or validated.

## Isolation and cleanup

Each preparation completed with exit0 before the dependent run. All12529 protected
qualification hashes remained unchanged. Unity6000.5.8f1 used graphics PlayMode,
profilepractice-bot-resume1002, a2048MB GPU job with2048MB reserve and a450-second
ceiling. Both guards are terminal, preservation completed and no lease remains.
Full logs, protected hash snapshots and original source stay in local
Logs/practice-bot-resume1002. No browser or preview was opened.

This report does not claim a completed practice fix, player build, physical device,
performance, victim tag or competition readiness.
