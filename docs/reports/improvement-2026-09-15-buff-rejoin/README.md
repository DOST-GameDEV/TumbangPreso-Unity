# Joining active buffs

Status: Sean's held-charge restoration passes local, actual observer reconnect
and controlling-owner reconnect checks. This does not cover every timed power.

The held-charge baseline kept Sean's fire charge active without throwing. Host
and owner recorded77/77 and76/76live-window charged/ember samples. A restarted
observer recorded0/92despite retaining the correct spent skill resource. Raw
traces and result are in baseline/.

The new restore-only path restores the remaining clock, flag and ember. It does
not activate the skill again or spend resources. Initial hydration settles once.
A real activation or accepted empowered release also settles it, preventing a
late joining record from overriding a newer cast or rearming a consumed charge.

Three focused local tests pass: remaining lifetime/resource preservation and
duplicate expiry protection; consumed-shot/newer-cast protection; the existing
repeated fire-snapshot consumption/cleanup contract. Protocol34 assertion passes.
The HeldCharge message is targeted during initial world synchronization and uses
the accepted hero, round and remaining clock. Actual34host/33clientrefusal passes.

The corrected observer recorded94/94live-window charge/ember samples, compared
with0/92in the failure. It kept one spent skill charge and expired normally.
Runtime49aa89a46a2f78e3ee2957e4470b4a135f48829d2ac98cd17a9cb0209d5d60ff.
The controlling-owner reconnect uses an observe-existing diagnostic path, so it
does not reseed its meter, position, character pick or cast input. It restores the
charge331ms after the arena becomes available, then preserves all subsequent
live-window charge/ember samples and the correct spent resource/expiry.
Runtime09c8aa4aef075b7679e5beeaab4f49b652bda000b4af5afe2a160ff67d1d8b60.

The initial evaluator counted the joining reply interval as missing state and
failed at84/88samples. Raw failure is preserved. The corrected check explicitly
allows the configured snapshot round trip plus250ms only for the returning seat,
then requires continuous state. With150ms one-way delay, the bound is550ms;
the same trace has79/79samples after that bound. No production change was made
to accommodate the measurement correction. The original missing-state baseline
still fails because it never restores the charge during its entire window.

Protocol34host/33client refusal passes. All named test profiles and shared Editor
input settings were preserved by the guards. No Desktop update or UI rework.

## Next timed states to inspect independently

- Zack Magnet: restore its charge without recalling/equipping another slipper.
  Preserve the independent Thunderstrike charged-throw window and signed throws.
- Dante Carapace: restore immunity, remaining armor age and optional Heavy Plating
  slowdown exactly once. Do not clear a new stun or replay the roar on hydration.
- Nemu Veil: restore its remaining clock and optional slow grant, remembering the
  current possession state so a pre-existing shoe does not cancel it. A later
  pickup must still end it. Never replay the dash impulse.
- Short movement powers and other world fields need their own state contract;
  calling OnActivate on arbitrary reconstructed skills is unsafe.

These later bullets are analysis, not implementation or qualification. Continue
the existing non-UI queue after the current concrete fix is saved.

The hydration guard is per newly constructed kit. Normal casts and accepted
empowered releases settle it too; it is not a general periodic buff correction
that can replace ongoing local actions. The packet handler validates the hero,
round and remaining time before applying it.
