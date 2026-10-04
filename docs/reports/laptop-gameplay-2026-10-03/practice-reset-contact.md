# Practice reset retires pre-teleport combat state

Source review begins from `88dba66a1`, the qualified input producer retirement
unit. This is a separate reset API boundary from PRACTICE-BOT-RESUME-1002's
previously fixed body disable path. Full practice menu/operator acceptance stays
open. Root ledger, TODO, Runtime/Net and hero/art assets are unchanged by this report.

## Original source mechanism

PracticeRange.ResetRange calls SliceRunner.ResetWorld while its active bodies
remain enabled. ResetWorld changes roles and teleports them; CharacterMotor's
Teleport/BeginSpawnSettle clear motion and statuses but do not retire CombatVerbs
contact windows. Ordinary offline pause intentionally preserves committed lunge
and slide contact. On resume, a still-live lunge can therefore evaluate its old
origin against the new spawn, rather than a dash the actor actually travelled.
SetDefender calls this same ResetRange route. The pending lunge windup also lacks
an explicit reset boundary in the original path.

This source finding does not claim a native victim tag or physical operator
reproduction. Retiring pre-reset contact is distinct from cancelling an input
producer: focus loss keeps an already committed dash, while resetting the
practice world moves the actor and invalidates that dash's segment.

## Focused native fixture

`PracticeResetCombatLifetimeTests.cs` contains five authored cases using actual
public HostResolveLunge, ResetRange, SetDefender and SliceRunner.ResetWorld:

- ResetRange retires existing contact before teleport, with positive measured
  relocation and the exact spent cooldown preserved.
- SetDefender retires existing contact when the role changes.
- ResetRange cancels a windup started through actual InputIntent.
- Ordinary pause preserves existing contact and cooldown.
- A refused, out-of-range defender choice preserves existing contact and cooldown.

The fixture supplies only range readiness and scene references directly. The
motor's movement simulation is disabled; it does not load a populated map,
operate PausePanel, run AI or measure a victim tag. Native execution belongs to
the parent, one guarded job at a time. Test metadata GUID:
`23b8ca8a88494502a5b613a10747164c`.

## Current validation

Original native `laptop-reset-original1003` ran exactly five cases: three causal
failures and two passing controls. ResetRange and SetDefender each retained
0.449999988 seconds of contact; ResetRange retained a 0.00255700108 lunge charge
ratio. Ordinary pause and invalid defender choice controls passed.
Candidate `laptop-reset-candidate1003` ran the identical five named cases once:
5 passed, zero failed. No fixture repair or assertion change was needed.

The parent uses the isolated native checkout at
`C:/Users/Matthew/dev/tump-laptop-native1003`, Unity 6000.5.8f1 and named guarded
profiles, one heavy job at a time. Both runs use GPU classification with 2048 MB
job memory and 1024 MB reserve. Fresh XML and terminal guard receipts are retained
in [practice-reset-native](practice-reset-native/). Original exit is 2; candidate
exit is 0. Both guards completed preservation and released their leases.
Candidate XML records the actual test window 2026-10-03 03:37:58Z; its guard ended
at 03:38:02Z. Nonzero exact case counts and the controls establish this focused
native result. No unchanged input-retirement or tournament gate was repeated.

The fix factors existing CombatVerbs.OnDisable retirement into RetireActions,
then calls it and Carrier.CancelPendingInput for each current range seat before
the actual ResetWorld teleport. It preserves spent cooldowns and ordinary pause
semantics. No broad CharacterMotor teleport, network or hero mechanism changes.

Physical devices, actual menu operation, victim tags, the slide-specific reset
route and a new player build remain separate acceptance limits. The pending
windup case observes synchronous reset cancellation, not a physical release gate
or a bot planner cadence measurement.
