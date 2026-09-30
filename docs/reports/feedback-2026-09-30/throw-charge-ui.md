# Throw Charge Clarity, 2026-09-30

The charging reticle now names its actual power percentage and FULL RELEASE at
full power. WAIT covers protection, the round's throw gate, local throw lock and
inability to act. Text is input-independent, outlined black, near the existing
charge ring and hidden with the reticle or when charge ends. Stable values reuse
the label. No throw tuning, kit, animation, sound, effects or map were changed.

The initial diagnosis wrongly assumed the down can refused throwing. Current
ThrowRules explicitly permits it, so that behavior is preserved: a held charge
still shows FULL RELEASE while the can is down. Restoration protection remains
shown, with the round/actor gates also consulted.

One native D3D11 case passes in12.0026813s through actual carrier charge:50percent,
full power, can down, protection, resumed availability, disabled reticle and real
release.960x540 and1600x680 captures were inspected. The initial two runs exposed
the fixture's incorrect can-down expectation and a dropped protection predicate;
the final predicate retains protection, and the test asserts the adopted down-can
rule. No unrelated checks were repeated. Hardware, actual peers and every-map
acceptance are separate qualifications.

- [Initial result](checks/charge-ui.xml)
- [Intermediate result](checks/charge-ui-final.xml)
- [Accepted result](checks/charge-ui-accepted.xml)
- [Frozen inputs](checks/charge-ui-inputs.json)
- [960x540](Throw-charge-full-960x540.png)
- [1600x680](Throw-charge-full-1600x680.png)
