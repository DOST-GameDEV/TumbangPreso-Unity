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

## Bayan, source043cf804c

Both ordinary samples passed. Loose-shoe maxima were1.5/6.3/2.3s Classic and
2.8/4.8/1.8s Hero, with continued fetch/withdraw and defense. No production map
change is justified by these samples alone.

The same launch's added bot recovery case failed in setup, before AI control:
the swimmer was not beside a reachable edge. It omitted the existing ExerciseEdge
case's .35second buoyancy-settling wait. One bounded correction restores that
wait and logs the handoff position. The original failed XML is preserved; do not
describe it as a bot failure. The correction runs alongside the first SaBubong
samples, not another Bayan rerun. Fixture allowance1/1 across compaction.

## SaBubong and recovery fixture disposition, source043cf804c

Both SaBubong samples passed. Loose-shoe maxima were4.8/4.1/5.5s Classic and
2.5/1.5/1.8s Hero. All five maps now have these initial two-mode samples; this does
not cover every role rotation, tier, roster, edge fall or network transition.

Recovery setup failed again before the bot took control, at(16.92,-2.01,16.38).
That is not the intended outer east lip nearx22.65. The existing ExerciseEdge also
sets the followed camera and movement aim source; the draft omitted that setup,
so its digital movement direction needs inspection against the input basis.
This is the next diagnostic, not a confirmed production failure. Fixture repair
allowance is exhausted. Preserve the draft in bot-edge-recovery-draft.cs.txt and
both failed XML files; the original compiled LagoonRecoveryProbe is unchanged.
Explicit bot water/roof recovery remains unverified in the final integration queue.

## Next production correction: per-seat difficulty

AIController.Me already uses SeatDifficulty before ActiveDifficulty. However,
ChoosePektusSpin uses the global setting for both the beginner early return and
expert spin candidates, and lunge release uses the global cone. A custom room can
therefore give one bot incompatible settings. Consolidate the effective difficulty
lookup and use it in those three existing decisions. Preserve every tuning value.
Run existing AiLungeRulesTests for the unchanged lunge-rule contract and compilation;
inspect all remaining ActiveDifficulty references. Do not write a tautological
getter test or rerun unchanged map samples. Live mixed-tier play remains unverified.

Implemented the shared effective lookup in Me, both pektus tier decisions and
the lunge cone. Remaining ActiveDifficulty references are the default declaration,
settings initialization and fallback only. Compilation and existing lunge-rule
checks passed3/3 in0.121s. This checks the rule contract and source wiring, not a
live mixed-tier room. No extra tests or unchanged map reruns were added.

The local diagnosis produced three targeted fixes: reaction duration, chase
patience and per-seat difficulty consistency. No other behavior was changed just
to create activity. REFINE-2.8 remains open for complete-role/roster/tier play and
the explicitly unverified bot recovery case. Keep those requirements in final
integrated qualification; continue the outstanding visible map/light work.
