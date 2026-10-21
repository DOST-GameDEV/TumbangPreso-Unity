# Bot reaction timing correction, 2026-09-24

Baseline 57cf74a9c. A visible opportunity could last longer than the authored
reaction delay without the bot reacting. The planner runs intermittently, but
Reacted added only the current render delta per query. Sabotage also queries
multiple times per decision, making that same clock depend on invocation count.

The isolated native reproduction failed on the expected assertion before the
change ([failure](../bot-baseline/reaction-before.xml)). The controller now stores
the scaled game time of the first observation. Repeated same-frame queries add
no time; pause freezes time. False conditions, invalid/switched sabotage targets,
new rounds and input-blocked states clear old observations. Delay/tier/personality
values, human rules, authority, movement and animation are unchanged.

The same regression plus both existing forty-second native 1x diagnostics passed
3/3 in88.089s. The regression itself took3.089s and covers held duration, first
sighting, false/reset, repeated queries and pause. No fixture repair or retry.
The test's reflection call only changed arity with the private method; assertions
were not weakened. Original failed evidence is retained.

All four bots participated in both new Eskinita samples. Intercept appears for
1.1s Classic/2.1s Hero, and each sample has one accepted sabotage shove. Neither
plan appeared in the earlier samples. This is compatible with the fixed mechanism,
not a controlled balance estimate: ordinary real-time samples have different
trajectories. Loose-shoe maxima remain below5s in these samples. Existing deliberate
five-second tag penalties remain. No new visual/art or animation approval claimed.

The timer regression is resolved; the broader AFK report remains open across
maps, roles, transitions, tiers and roster. Next inspect Lagoon's actual bot
decisions, then remaining maps with the existing diagnostic. Do not rerun the
successful Eskinita pair unchanged or build a new capture framework.

Native inputs: AIController.cs and AiReactionTimingTests.cs on57cf74a9c. Logs and
full pre-restore patches remain in the owned qualification worktree. Generated
Inday/meta/ProjectAuditor changes were backed up/restored. Original DEV PNG metas
were never staged. No native player build or transport claim.
