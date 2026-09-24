# Map-specific bot diagnosis, 2026-09-24

The existing AiDiagnosticProbe now accepts TUMP_AI_MAP; default Eskinita behavior
and filenames stay unchanged. Additional maps get distinct filenames. This is a
small extension of the existing trace, not a replacement diagnostic framework.
Each result below is two forty-second ordinary-speed samples with four bots,
not a complete match, every defender rotation or every roster/tier combination.

## Lagoon, source 5de3a78f3 plus map selector

Native2/2 passed in87.850s. The longest loose-shoe spells were1.4/4.1/5.8s Classic
and4.8/5.1/1.5s Hero. The traces show throws, fetch/withdraw, can resets and active
defense. No sustained inaccessible loose shoe appeared. Retain current deck
decisions on this evidence; no speculative map AI patch.

No sampled player position was underwater, so these traces do not qualify bot
water escape or bridge climbing. That case still needs a deliberate local
reproduction and remains in coverage, alongside full role/round transitions.

## Next focused cause and map

Source inspection found the same invocation-count problem in
ChaseIsGoingSomewhere: it added render dt only at planner decisions, stretching
the authored2second no-progress patience. Correct its elapsed-time measurement
and reset across round/input interruptions. Preserve the threshold and progress
distance. The regression should stop a non-closing chase after its real window,
then prove closing distance refreshes it. No claim that the preceding Lagoon
sample itself contained a stalled chase.

The chase clock now measures scaled time since the last sufficient closing
movement. A new round or input block discards the old chase observation. The
isolated regression checks expiry, a fresh chase window and progress resetting
the window. No authored threshold, human verb or character animation changed.

## Ilalim and chase fix, source5de3a78f3 plus focused changes

Native3/3 passed in96.089s: the new chase regression and both first Ilalim samples.
The Classic trace's loose-shoe maxima were3.5/1.4/3.7s. Both traces show ongoing
play rather than a permanently abandoned shoe. This remains sampled liveness,
not attribution of the historical48-penalty match or complete map qualification.
The chase defect was established from its source clock and corrected by the
new focused regression; no separate native pre-fix chase failure is claimed.

No fixture repairs. No unchanged reaction/Eskinita/Lagoon reruns. Next: remaining
Bayan/SaBubong samples and explicit bot edge/water recovery, which these ordinary
samples have not exercised. Round/role/roster/tier coverage remains tracked.
