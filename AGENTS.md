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

## Finalized Paete And Phaister

- The owner-approved finalized Paete and Phaister implementations are authoritative
  over stale Wiki text, old TODO entries and earlier proposals.
- Do not change either hero's mechanics, model, animation, VFX, SFX, cutscene,
  camera direction, UI presentation or authored assets except for a demonstrated
  bug fix. Do not redesign or polish them as part of the broader presentation pass.
- Do not change their in-game skill names or descriptions. Passive descriptions
  are the only description exception; do not infer permission to change passives'
  behavior. The owner separately authorized updating stale Wiki descriptions to
  accurately document the finalized implementation; that is documentation work.
- This protection is character-specific. Authorized global/shared changes apply
  normally to Paete and Phaister too, including changes to everyone's HUD buttons.
  Do not ask for a separate exception merely because a shared change includes them.
  Do not use a global change as a pretext to redesign their finalized kits or art.
- Any permitted bug fix must identify the reproducible defect, preserve the
  finalized direction and include focused behavioral evidence.

### October1 owner exception: Phaister hallucination refinement

The owner explicitly requested refining Phaister's hallucination skill. This
permits a focused Curse: Hex hallucination refinement after inspecting its actual
play, with a recorded plan and evidence. It is not permission to redesign the
rest of Phaister or Paete. Preserve current cast/mark/recast rules and in-game
copy unless the scoped refinement demonstrates a reason to change them; do not
silently retune timings or replace the finalized doll/ultimate direction.

### October 1 owner exception: replace all authored SFX

The owner explicitly reopened all SFX made in the earlier presentation work,
including the finalized Paete/Phaister cues. This permits researched, critically
listened-to sound replacement and mixing, with timed in-game validation. It does
not reopen their mechanics, models, animation, cameras or descriptions. Preserve
human-voice policy; permission for SFX is not permission to imitate voices.
Do not present amplitude measurements or silent film as listening approval.

## Autonomy And Continuity

- Continue assigned independent work through questions, checkpoints, passing
  tests, commits and pushes. A progress report is not a stop. Stop only when the
  assignment is complete, the owner stops it, or no authorized work can progress
  without a genuine external dependency.
- Complete real requirements. Do not mark partial, unverified or failed work done,
  or hand back implementable work as a human-review blocker.
- Finish each coherent unit before switching. Record new requests, decisions,
  ownership, evidence limits, jobs and the exact next action for resumption.
- While a test or build runs, advance useful independent work: inspect new
  feedback, critique retained results or plan the next coherent fix. Do not
  mutate frozen test inputs or overlap heavy Unity jobs. Recover ordinary
  tooling failures autonomously within the bounded retry and safety rules.
- Before compaction or resumption, preserve current source/publication identity,
  active jobs, failures, ownership, the next action and recent owner corrections.
  Track which owner messages already received answers; do not replay an old
  answered question as a new request or resend an unchanged status update.
- Maximize useful progress rather than activity for its own sake. If execution
  must wait, critique the actual result and prepare a concrete next step; never
  repeat unchanged tests, invent completion or burn resources as busywork.
- Do not spawn or delegate workers, contact other conversations, buy services,
  reset usage, mutate live profiles or replace the Desktop build without explicit
  current authorization. Honor contributor reservations and private owner notes.
- Keep updates concise and useful. Prefer actual fixes to planning, audit,
  diagnostic, fixture or capture-framework churn.

## Approved Cloud Setup Recovery

- The owner explicitly authorizes restoring the existing TUMP cloud setup when
  tools or files become unavailable. Recover the approved repository/branch,
  matching Unity editor, .NET test tools and required official dependencies
  autonomously. Reuse available installations and preserve existing logins,
  profiles, source assets and unfinished work. Follow docs/WORKSTATION_SETUP.md.
- Missing files alone do not prove a reset or replacement. Verify the actual
  workspace, native execution environment and tool state before explaining a cause.
- Record successful recovery steps and exact blockers, then verify fresh managed
  and native results before resuming implementation. Installation is not activation
  and a website login is not an editor licence. Do not redo already-shipped work.
- The owner accepted the Unity Hub Terms of Service and Editor Software Terms
  presented on October 1, 2026. Do not ask again for those same accepted terms.
  This recovery authorization does not cover different new agreements, purchases,
  expanded account access or entering passwords on the owner's behalf.

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
