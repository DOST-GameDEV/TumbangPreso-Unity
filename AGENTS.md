# TUMP Working Instructions

## Start Here

1. Read this file, [VISION](docs/VISION.md), the [current TODO queue](docs/TODO.md#current-implementation-queue),
   then the newest [ledger checkpoint](docs/ACTIVE_REWORK_LEDGER.md).
2. Choose the task's route in [docs/README](docs/README.md). Read its live method
   and the relevant [working rules](docs/WORKING_RULES.md), not every historical report.
3. Inspect actual source, branch, dirty state and evidence before changing anything.

New owner instructions and current adopted designs override old schedules,
archived prompts and earlier scope statements. TODO is the ONLY work-status queue.
The ledger is a concise resume checkpoint, not another backlog. Preserve numbered
TODO IDs; open bodies live in TODO_Backlog and finished bodies in TODO_Archive.
An old OPEN heading is not proof a feature is missing. Reconcile implemented,
replaced, retired and genuinely blocked requirements without reviving old ideas.

## Autonomy And Continuity

- Continue assigned independent work through questions, checkpoints, passing
  tests, commits and pushes. A progress report is not a stop. Stop only when the
  assignment is complete, the owner stops it, or no authorized work can progress
  without a genuine external dependency.
- Complete real requirements. Do not mark partial, unverified or failed work done,
  or hand back implementable work as a human-review blocker.
- Finish each coherent unit before switching. Record new requests, decisions,
  ownership, evidence limits, jobs and the exact next action for resumption.
- Do not spawn or delegate workers, contact other conversations, buy services,
  reset usage, mutate live profiles or replace the Desktop build without explicit
  current authorization. Honor contributor reservations and private owner notes.
- Keep updates concise and useful. Prefer actual fixes to planning, audit,
  diagnostic, fixture or capture-framework churn.

## Shared Workspace And Publication

- Work and integrate on ASTRAReworks. Fetch first; inspect status and divergence.
  Never edit/merge/push main, reset, clean, force-push or discard another's changes.
  Integrate relevant contributions with their authored behavior and assets intact.
- Preserve profiles, saves, IDs, source/supplied art, unfinished work and unrelated
  processes. Unexpected edits are not permission to revert them.
- Stage explicit task-owned paths only. Never commit the two protected
  Resources/UI/composition-redesign/*.png.meta files. Keep private notes/overlays private.
- Push coherent checked batches, fetch before each push, then verify remote HEAD.
  Sole author M4tyu633; use git commit -F. No attribution trailers, tooling references
  or em dashes in repository prose and commit messages.
- Handoff prompts go in chat, never a committed file. Include rules, repo/branch/HEAD,
  actual tests/build state, changed behavior and the next TODO pointer.
- Stop only task-owned helpers and close task-owned previews. Never kill unrelated
  apps or delete a managed worktree as ordinary cleanup.

## Engineering And Evidence

- Use existing architecture/helpers and the smallest justified change. Core remains
  engine-free; host owns outcomes; MatchDirector.AddScore owns points. Preserve
  identity/ownership separation, derived defender, square confinement, input
  contexts and the [network contract](docs/SKILL_NETWORK_CONTRACT.md).
- Preserve GenericPadBridge, MenuNav and the input backend. Every feature needs
  mouse/keyboard, controller and touch entry, feedback and exit paths.
- One focused pass per coherent unit, at most ONE bounded tooling repair/retry.
  Reuse unchanged evidence. Broad regression belongs on a coherent integration
  candidate, not after every minor/cosmetic edit.
- Record the question, stopping condition and retry count before a run. Fix actual
  product failures with a relevant check; do not weaken tests or repair unrelated
  fixtures to make a report green.
- Run Unity through tools/run_unity_guarded.py with a named isolated profile.
  Do not invoke the guard with --help. Freeze inputs and isolate writable caches,
  profiles, ports and outputs; one heavy job. Keep working on independent inputs.
- Preserve run diffs before touching generated churn. Never infer runtime success
  from compilation, exit0, stale XML or zero tests. Native visual checks require
  graphics. See [TESTING](docs/TESTING.md) for commands and evidence levels.
- New Unity script metadata needs a valid 32-hex GUID; generate it with
  `Guid.NewGuid().ToString("N")` and verify the file. Direct C# compilation does not
  establish Unity import validity. Preserve existing valid GUIDs.
- Final builds use explicit internal Builds/<candidate>/TumbangPreso.exe paths.
  Verify executable/data and run that exact player. No Desktop replacement.
- Be precise about local tests, actual peers, cross-platform play, hardware and
  human taste. These are separate claims, not interchangeable evidence.

## Product And Authored Work

- Both Classic and Hero Strike ship, defaulting to eight rounds. Classic people
  are cosmetic/neutral. No Street Hype. Preserve the retrieval-centered game.
- Keep the cute blocky cast, flat faces, simple hands, rig paths, action names and
  GUIDs. Character appearance/motion is authored ONE CHARACTER AT A TIME.
  Shared evaluators are fine; copied cast-wide looks are not.
- Models: [character method](docs/CHARACTER_MODEL_METHOD.md),
  [art direction](docs/Art_Direction.md), [clothing constraints](docs/CAST_CLOTHING_STYLE.md).
- Kits/animation/VFX/audio/cutscenes: [hero-kit method](docs/HERO_KIT_METHOD.md)
  and [shared network contract](docs/SKILL_NETWORK_CONTRACT.md).
- HOME/menu/season/showcase animation: [home animation method](docs/HOME_SCREEN_ANIMATION_METHOD.md).
  Preserve real models/faces, individual direction, breathing beats and story.
- Keep supplied menu/login artwork, aspect ratios, controls and current routes.
  Use [UI method](docs/UI_DESIGN_METHOD.md), [authoring](docs/OWNER_UI_AUTHORING.md)
  and [font roles](docs/FONT_USAGE.md). Consent uses the requested check mark.
- Keep the HUD minimal and match text/icons outlined BLACK through
  UiTheme.InGameOutline. No UI version stamps, restored retired layouts or
  explanatory clutter. Full product, input, asset and UI contracts live in
  [WORKING_RULES](docs/WORKING_RULES.md).

## Documentation And Media Maintenance

- Update the owning live document with a behavior/contract change. Add new references
  to the index. Keep status in TODO, methods in method docs, evidence in dated reports.
- Archive genuinely superseded plans whole; retain current methods and decision
  context. Leave a pointer at old paths when referenced; label history as non-authoritative.
- Keep the latest two generated visual iterations per coherent subject/action/view
  when pruning owner-authorized old captures. Preserve shipped assets, source art,
  supplied references and essential method/decision evidence; log exact removals.
- Complete pre-cleanup instructions and incident explanations are preserved in
  [the dated snapshot](docs/archive/snapshots-2026-09-27/README.md). Read history
  to understand a decision, not to reactivate a superseded work order.
