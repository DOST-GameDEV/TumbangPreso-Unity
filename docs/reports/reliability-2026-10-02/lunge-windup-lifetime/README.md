# Retired lunge windups do not resume on a reactivated body

CombatVerbs.OnDisable already retired lunge and slide contact windows. It retained
the local charging flag, local charge amount and observed lunge tell. Disabling a
charging body, releasing its input while inactive and reactivating it let StepLunge
release that old charge as a new dash. An observed tell applied through the public
network presentation API also reappeared before its normal stale-packet expiry.

OnDisable now additionally clears the local charging flag/amount and observed tell.
Spent cooldowns, existing contact retirement, sent-state tracking and ordinary
Update/pause behavior are unchanged. No hero, input binding, tuning or protocol
changes.

## Native evidence

Three focused PlayMode cases use actual CharacterMotor and enabled CombatVerbs.
Local charge and release go through public Intent.Set and native Update; observed
charge uses public ApplyObservedLungeCharge. Actual GameObject deactivation and
reactivation exercise component lifetime. The motor is disabled to exclude physical
travel; temporary stats/provider references and the playing clock are restored.
No private timer/charge writes, raw-input framework, range setup or expiry waits.

- Original run 96494: exactly 3 cases, 2 intended causal failures and 1 control
  passed. Reactivating a retired local windup spent the full 1.5-second cooldown;
  the retired observed tell returned 0.400000006 instead of an inactive value.
  An enabled body still charged and released normally.
- Candidate run 64944: exactly 3 cases passed. Local retirement no longer creates
  the spontaneous release/cooldown, observed retirement remains inactive and normal
  enabled-body release still works.
- Identical fixture and valid 32-hex metadata for both runs. Zero fixture, tooling
  or native repairs. Product source change is three reset assignments in the
  existing OnDisable method.
- Each dependent launch followed direct preparation exit 0. Exact 3 owned input
  hashes match MAIN/qualification; all 12,547 protected qualification hashes remain
  unchanged. Existing shipped lunge/slide contact tests were not altered or rerun.
- Unity 6000.5.8f1, batch/nographics PlayMode, profile lunge-windup-lifetime1002,
  exclusive GPU classification with 2048 MB plus 2048 MB reserve, 450-second ceiling.
  Both guards are terminal, preservation completed and no lease remains. No browser
  or preview was opened.

Raw baseline/candidate XML and job receipts, owned-input manifests, case counts and
protected-input result accompany this report. Full logs, original source and
protected snapshots remain in local Logs/lunge-windup-lifetime1002.

This accepts local-input and observed-state windup retirement on actual native
components. It does not qualify range operation, physical travel, actual victim
tags, hardware, peer transport or a complete player build. The frozen 1002j player
predates this change.
