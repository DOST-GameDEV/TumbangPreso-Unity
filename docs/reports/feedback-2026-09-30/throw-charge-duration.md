# Faster ordinary throw charge

## Behavior

Feedback asks for faster tsinelas charging. Full ordinary throw power now takes
1.25 seconds instead of2.5 seconds in both Classic and Hero Strike. This halves
the wind-up without changing minimum/full launch power, slipper skin modifiers,
throw legality, spin, pickup range or recovery rules. Existing aim settling remains
separate from power charging. No hero kit, binding, art or sound was changed.

Local charge ratio and observed charge presentation both use Balance.ChargeFullTime.
Protocol96 excludes older peers that interpret the same charge-seconds messages
against the old2.5-second duration. Every participant needs a matching updated build.
No packet layout is added or changed.

## Native validation

Unity6000.5.8f1 Linux64, guarded cloud-throw-charge profile and isolated frozen inputs.
Eight real Carrier cases cover both modes:

- Full local and observed power after1.25 seconds, while the slipper remains held.
- Half power after0.625 seconds and clamped full power on a long hold.
- Actual release transitions the slipper to InFlight, uses the same displayed
  launch velocity, clears held state and stops observed wind-up.
- Received charge seconds produce the same half/full ratios and clear correctly.

Baseline:8/8 fail the new timing acceptance, showing0.5 power after1.25 seconds and
0.25 after0.625 seconds. Corrected candidate:8/8 pass, none skipped. Frozen inputs
remain unchanged, with no fixture repair or retry. These tests call the real carrier
intent/release and observed-state paths; they do not simulate physical devices,
actual peer transport, full-match bot decisions or human balance preference.
The separate landing-circle and clearer charge-feedback requests remain open.

## Evidence hashes

- baseline.xml: SHA-256 a22a5824dcbf1cca980be3deb7977a0a71c1ee2ed314739ae1105ee85a81f1a4
- fixed.xml: SHA-256 240e3ed8eb37aaecea47ae2cade8019d8ae0a8adba4d59d466092d69829bfea0
- baseline-inputs.json: SHA-256 bc42cbe754e332e838c3757032bbdba462b8ff5e3d5234a4e0f8a8abbf7e549e
- fixed-inputs.json: SHA-256 c75bf34a2159af539adb88d5a31752ea2953bcbe5e8fad8dd49195a74fd8fb0e
