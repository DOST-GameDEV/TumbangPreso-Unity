# REFINE-2.8 bot refinement: evidence and order, 2026-09-24

Starting revision 57cf74a9c, Unity 6000.5.8f1, Windows native graphics. The owner
reports bots often stand around instead of participating. Diagnose the blocked
exchange and fix one cause at a time. Do not manufacture busyness or replace the
existing planner wholesale. New hero/action animation remains the cloud lane.

## Research and application

[Valve, Michael Booth, The AI Systems of Left 4 Dead](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf),
slides 37-42 and 47: separate locomotion, intention and body; record reasons for
action transitions; react to move success/failure and object events. Reliable,
understandable decisions matter alongside imperfect senses/reaction time.
Application here: compare chosen plan, input, resolved movement and accepted verb.
An idle picture cannot distinguish a deliberate charge from a failed action.
Retain human InputIntent rules; no teleports or direct scores to conceal a stall.

[Epic, Behavior Tree Overview](https://dev.epicgames.com/documentation/en-us/unreal-engine/behavior-tree-overview?application_version=4.27)
explains conditional aborts driven by events and clearer execution history.
Application here is conceptual, not an Unreal/behavior-tree migration: inspect
whether role, can, slipper ownership or recovery transitions invalidate the plan
and whether its next action still has a legal target. Reuse current diagnostics.

## Current source and hypotheses

- AIController already has commitments, retrieval safety/patience overrides,
  InputIntent actions, resolved-motion stuck detection and map water-exit handling.
  Preserve C1/C2 fixes. They do not prove the current AFK report resolved.
- The initial missing-edge-mash suspicion is weakened by source:
  BeginEdgeRecovery applies a trip; CanAct=false with IsTripped enters the existing
  mash branch. Do not change recovery speculatively from that suspicion.
- Distinguish deliberate Windup/Stalk/Guard waits, unresolved movement, unreachable
  pickups, stale targets, lost input edges and inability to act. AiDiagnosticProbe
  already emits plan/role/action/charge/throw permission/movement/positions and
  loose-slipper eligibility. Use it before adding instrumentation.

## Order and bounded checks

1. Run existing forty-second Eskinita diagnostics in both modes at native 1x.
   Locate concrete failures from traces; counts are not a quality score. Record
   seated bot count. An unmanned human seat is not a broken bot.
2. Fix the first supported cause and reproduce its state with an existing relevant
   test or one small regression. Preserve before/after evidence and limits.
3. Cover all five maps, both roles/modes, round/seat transitions and roster/tier
   choices using existing match routes and scoped diagnostics. No large seed sweep
   for reassurance; use one only for a concrete balance question.
4. Inspect actual exchange/motion evidence for changed behavior. Legitimate waits
   remain when they lead to useful accepted actions.
5. Keep 2.8 open until coverage is accounted for. Transport/replay/final performance
   remain in 2.10/P7. Do not claim the old 48-penalty incident explained without its trace.

Next check: what plan/input/target explains sustained lack of progress in the two
ordinary-speed samples? Stop after reading XML and both traces and selecting the
next cause/action. Fixture repairs 0/1. This is initial diagnosis, not completion.
