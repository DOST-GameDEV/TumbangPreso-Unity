# Offline Training Range

Implemented after `6f128705` for PRACTICE-1. Native interaction and visual
qualification remain open; this report does not close the parent requirement.

## Behavior

- The existing Tutorial/Training picker remains. Training cancels room/search
  ownership and requests an explicit offline range without saving AI preferences.
- The range starts with the local attacker only. Three target bodies and their AI
  are prepared during setup, hidden and removed from registered gameplay queries.
  Summoning/removing targets reuses those bodies, with current role spawn positions
  and stable seat-owned slippers. Idle targets are parked; active targets run normal AI.
- The existing pause binding, controller and touch menu open the range controls.
  Character changes use the authored roster model, palette and default kit. Existing
  model-change notification refreshes first-person arms. Nothing changes the saved pick.
- Cooldown, skill-charge, ultimate-bank and stamina switches are independent and
  affect only the local body. Legacy offline sandbox behavior remains outside the
  range. Ultimate introductions, cast timing and authored effects are not redesigned.
- Lock can state prevents knockdown/restoration until disabled. It does not disable
  the can component or freeze cosmetic animation/rolling. Reset explicitly restores
  the can, bodies and kits while retaining the selected training rules.
- Choosing the defender changes the derived round identity, as guided training does;
  it does not create an independent role authority. Reset keeps local menu input and
  idle targets parked. The range clock does not finish the session or award passive
  defense/idle penalties. Existing offline progression exclusion stays in force.
- Training setters refuse network transport, a selected/connecting network session,
  revoked authority, guided tutorial and spectator/all-bot launches. The legacy
  sandbox also now refuses the selected-network connection window. No new RPC or
  protocol change is needed; protocol69 remains current.

## UI And Loading

The range menu is built during match setup through the existing UI row, choice,
toggle, portrait and focus helpers. The root-level canvas stays inactive until the
menu opens; preparing it does not park input or capture the cursor. It is reused,
not reconstructed on every pause. Closing/destroying the owner hides/removes it.
Choice popups register their Escape ownership and use the existing touch-target
sizes so backing out does not also close the parent menu.

The surface has Player, Training Rules and Bots sections in one scroll list, with
Resume, Settings and Leave outside the scroll. Current state refreshes existing
controls. This is not a measured no-hitch claim: model instances and actual GPU/UI
first-use still require native profiling.

## Evidence

- Frozen150 inputs,23 owned source/test/metadata paths; unrelated changes excluded.
- Core, Runtime, Editor, Tests and PlayTests compiled successfully. After the final
  root-canvas lifecycle correction, only Runtime and PlayTests were recompiled.
- Seven new Core gate cases executed and passed; fresh TRX inspected:7total,
  7executed,7passed,0failed. The isolated test project initially had no restored
  dependencies, so the no-restore invocation produced no tests and is not evidence.
  A restore command syntax error was corrected before the single actual test run.
- Added native resource-independence and range lifecycle/control tests covering
  bots-off preference, prepared menu, character change, stable reused bots/roles,
  can lock/reset, network-transition refusal and input release. Updated the existing
  picker journey's Training expectation to the new initially empty range.
- Native tests, screenshots, physical pad/touch and player timings: **NOT RUN** under
  the existing editor disk-reserve limitation. No repeated editor launch, old suite
  rerun or unchanged film regeneration. [Compiler/source receipt](checks/practice-range-compile.json).

## Remaining Acceptance

Run the new range case on the frozen candidate when native headroom is available,
then exercise both rosters, all controls and nested Back over the real background
at the owner's window and touch shape. Record actual action/return paths and any
first-use cost. Keep PRACTICE-1 open until that evidence exists.
