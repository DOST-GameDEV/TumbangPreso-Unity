# Initial bot diagnosis, 2026-09-24

Source 57cf74a9c. Existing ordinary-speed Eskinita diagnostics passed 2/2 in
89.680s with four bots in both modes. These are forty-second samples, not full
matches or all-map coverage. Classic produced14 throws; the longest loose-shoe
spells were1.7/3.3/2.8s. Hero longest spells were0.9/0.9/4.6s. Counts are liveness
observations, not a before/after balance claim.

Repeated stationary spawn samples have CanAct=false. The current tag path applies
Balance.TagStunTime=5seconds, consistent with those windows; do not remove that
rule to make bots look busy. The missing-edge-mash hypothesis was also weakened:
edge recovery uses the trip state handled by the existing bot mash branch.

One concrete timing suspect remains: AIController.React(ed) accumulates its dt
argument, but incoming/lunge checks execute only on think ticks. StepPlan passes
the current frame delta rather than elapsed time between decisions. Normal's
0.30second reaction and0.24second think cadence therefore cannot be interpreted
as their authored durations. Sabotage can also query more than once in one frame.
Neither sample entered Intercept; both had legal sabotage projections without a
plan entry. Those counts alone do not establish cause.

Next isolated reproduction uses the real controller's private gate: first observe
an opportunity, wait its actual reaction delay, observe it again. The old gate is
predicted to still refuse. Also require repeated same-frame queries not to create
elapsed time, false observations to reset, and paused time not to accrue. This
targets the clock mechanism without tuning human rules or changing personalities.
The native reproduction failed exactly at the elapsed-delay assertion; see
reaction-before.xml. The correction now uses scaled first-observation timestamps,
not invocation counts. Querying twice in a frame cannot advance them. False
observations, invalid candidates, new rounds and input-blocked states clear stale
observations. Authored reaction delays and human rules are unchanged.

Full role/map/roster coverage, normal-speed presentation and broader AFK report
remain open. This is not attribution of the historical48-penalty incident.
