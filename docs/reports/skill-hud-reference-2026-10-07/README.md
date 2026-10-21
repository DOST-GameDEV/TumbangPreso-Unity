# Skill-HUD-only reference

Apply the supplied skill-ui-only reference: dark circular faces, gold progress
rims, notched ultimate and compact saved-binding prompts at the lower-right of
each icon. Existing role/charge information uses small round chips. Retain the
current deck placement/accessibility scale, skill illustrations and runtime
readiness/recast/duration/refusal/charge semantics. Tile/jar overlays are removed
from this deck. No other HUD, font, ability, control or network code is replaced.

Hidden D3D11 native21092 on baseff663f5d8 plus the recorded source passes3/3.
OwnerSkillHudReferenceTests checks the real Arena deck, three seals, current
bindings, compact26-unit prompts, unchanged DIN labels and Darumadrop TimeLeft.
TimedPowerUiTests checks active basic/recast/ultimate timers and expiry, permanent
Overclock and existing zapped logo crosses. Actual frames at
[1080](arena-1080.png) and [720](arena-720.png) show the current surrounding HUD.
All21234 frozen inputs and shared preferences restore after parent termination.

This is staged native UI/selected-state qualification, not full gameplay, physical
device, owner visual approval or peer acceptance. The source reference is in
../ui-owner-reference-2026-10-06/skill-ui-only.png. XML and a compact source receipt
are here; full native logs/frozen hashes remain in local Logs.
