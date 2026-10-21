# Role-aware ability recall and remaining Wiki UI parity

The held-description panel cached only its HeroKit reference. A round can switch
the same kit from attacking to defending, so it continued describing Frostbite
instead of Glacial Wall, or Boulder instead of Bastion. The cache now also tracks
the actual role-ability reference. Switching either direction refreshes the name,
full description and metadata. No skill behavior, authored words, input, layout,
art or protocol changes; the shared correction applies to every role kit.

## Native evidence

Baseline reproduces both wrong-name cases,0/2. Corrected role cases pass2/2,
including full description and reverse transition. A separate current-guide case
initially fails because its fixture searches below the owner object while the
UI factory creates a root canvas. One bounded fixture repair uses the panel's
actual owned canvas and cleans it explicitly. Only that failed case is rerun;
it passes1/1. Three distinct cases across2+1, not a single3/3 final run.

The current four-slot guides are exercised through their actual tab callbacks
for Cheska and Dante. Each name and summary matches the live kit, cooldowns match
the current Wiki (Cheska35/35/35; Dante40/35/35), and both ultimates cost12points.
Held attacker/defender descriptions and cooldown/duration metadata match. Guide
frames at960x540 and1600x680, plus both defender recall trays, were inspected.
The displayed text fits; no old Seismic Stomp, Frostbite-in-defence or Boulder-in-
defence substitution remains. Existing supplied portraits and icon bindings stay.

This completes the remaining copy/UI check for F0930-09 and F0930-10 alongside
their retained native mechanics evidence. It is not human verification or approval
of the separate full-roster presentation/SFX pass. No refreshed player, physical
input-device, actual-peer or performance claim is added by these UI checks.
The retained full129Stoke player predates this UI-only correction.

All scoped launches use named isolated profiles, graphics and fresh nonzero XML.
No new OOM or guard stop. Original failed receipts and one fixture repair remain
beside final evidence. Do not repeat unchanged mechanics suites for this cache fix.
