# TUMP repository instructions

## Read first and resolve current scope

Read this file, [VISION](docs/VISION.md), the [current TODO queue](docs/TODO.md#current-implementation-queue),
then [ACTIVE_REWORK_LEDGER](docs/ACTIVE_REWORK_LEDGER.md). The ledger is the resume point;
TODO is the only status queue. New owner instructions and adopted designs override
historical schedules, archived instructions and superseded proposals.

- TODO preserves every numbered ID. Open bodies live in [TODO_Backlog](docs/TODO_Backlog.md),
  finished bodies in [TODO_Archive](docs/TODO_Archive.md), and retired plans in
  [docs/archive](docs/archive/README.md). [docs/README](docs/README.md) indexes the documents.
- Complete current requirements, not every old idea. Reconcile old OPEN headings,
  missing classes and retired FUTURE plans against current source/design/evidence.
  Record implemented, replaced or retired dispositions without deleting IDs/history.
  Uncertainty calls for reconciliation, not speculative features or invented blockers.
- Inspect before improving. Name the actual visual, usability or functional weakness;
  preserve successful work. An old test pass is not final art/motion approval, and
  a completed HUD must not regain retired layouts, extra timers or explanatory clutter.
- Never stamp one change across the whole cast (owner 2026-09-27): walk, run, idle, poses,
  casts, faces and proportions are authored and filmed one character at a time from that
  character's personality. Shared plumbing is fine; a shared look is not. CLAUDE.md section 0.
- The full remaining actionable TODO is assigned, including formerly deferred maps,
  Rafi, Lagoon, Inday, UI and qualification. "Later" means order, not exclusion.
  Current ownership and sequencing belong in TODO/ledger, not a duplicated plan here.
- Owner update 2026-09-24: a separate owner-run cloud lane is assigned ALL animation
  research/direction/implementation, including individual ability casts and ultimates.
  It may start alongside local map/environment/bot work. Preserve current merged
  animation progress; a handoff is not completion. Do not contact another conversation.

## Autonomy, continuity and honest completion

The owner explicitly wants autonomous work while AFK, without routine questions or
another "continue" at each checkpoint. On 2026-09-21 the owner was deeply disappointed
when this agent stopped prematurely twice in one day. These were agent mistakes,
not owner-requested pauses. The owner also repeatedly rejected excessive verification/
fixture work. Keep these corrections active across compaction.

- Plan and reconcile documentation before implementation; save decisions and the
  next steps so the work survives compaction. Finish the active coherent unit before
  switching; log new requests without losing existing tasks.
- Continue authorized independent work after status questions, corrections, plans,
  passing checks, commits, builds and phase boundaries. Keep progress in commentary;
  do not end with a final delivery while independent in-scope work remains.
- Complete each actual requirement. A subset, existing class or saved capture does
  not close its parent task. Never present unfinished implementable work as a human-
  review blocker or mark failed/unverified requirements complete.
- Human taste, credentials and unavailable hardware limit their specific claims.
  Record real dependencies and continue other work. Stop only when the assignment
  is complete or no remaining authorized work can progress without a genuine external
  dependency. Save non-routine decisions/questions for the owner's return.
- Autonomy does not authorize paid services, usage resets, subagents/delegation,
  other conversations, crossing current contributor reservations or Desktop replacement.
  Built-in image generation is authorized for requested ideation; no paid API/CLI fallback.

## Workspace, collaborators and publication

- Work/integrate on ASTRAReworks. Fetch first; inspect branch, dirty state and divergence.
  A cloud-assigned working branch may deliver a pull request targeting ASTRAReworks.
  Never edit/merge/push main, reset, clean, force-push or discard another's work.
  Relevant friend branches may be inspected, tested and scoped-integrated with authorship intact.
- Concurrent contributors exist. Preserve unexpected edits, inspect overlap, and
  confirm provenance before restoring or staging supposed generated churn. The old
  C4 reservation no longer blocks assigned MatchRpc/NetSession/authority integration;
  newer explicit ownership still applies. No delegation or unrelated audits are implied.
- Preserve profiles, saves, IDs, supplied/source art, unfinished work and unrelated apps.
  Keep GenericPadBridge, MenuNav, controller mappings, artwork, callouts and connections;
  do not replace the input backend.
- Push stable verified batches to origin ASTRAReworks; fetch before every push and
  verify remote HEAD afterward. Stage explicit paths. Never commit the two protected
  `Resources/UI/composition-redesign/*.png.meta` files. Sole author M4tyu633; use
  `git commit -F`, no attribution trailers, AI mentions or em dashes in commit messages.
- Handoffs go in chat. Repository source, plans, instructions, logs and scaffolding
  stay local/in the repo, never Google Drive. Drive is only for explicitly requested deliverables.
- Close task-owned tabs/previews and stop task-owned helpers when finished. Preserve
  pre-existing tabs, unrelated processes and other contributors' work; never kill an
  entire browser to clean up one tab. Record a genuine tool-blocked cleanup limitation.

## Engineering and validation

**Quality over verification loops.** Use existing tools and the smallest meaningful
check for the changed behavior. Do not build a new capture framework for each edit,
repair unrelated stale fixtures or regenerate successful unchanged evidence.

- Before a run, record its question, stopping condition and tooling retry count.
  Each coherent batch gets one focused pass and at most ONE bounded tooling repair/
  retry, including across compaction. If that still fails, preserve the failure,
  exact unverified claim and next diagnostic, then continue independent feature work.
- Fix real product defects exposed by a check and run their smallest relevant test.
  This is not permission to expand fixture work or weaken assertions to go green.
  After a pass, return to implementation; repeat/broaden only for a new change,
  actual failure or concrete unresolved concern. Full regression/integration follows
  the assigned features on one frozen candidate. No intermediate player builds.
- Unity launches use `python tools/run_unity_guarded.py` and a named isolated profile
  (current validation profile: `presentation-validation-20260921`). Never pass the
  guard `--help`. Read ProjectVersion/installed tools instead of copying paths/PIDs.
  Another OS needs an equivalent profile-preserving guard, not an unguarded Windows command.
- Run Unity in the background; no `.cs` edits while a run is in flight. Keep tested
  inputs frozen. Independent work may continue in isolated workspaces subject to
  that freeze; never share writable Assets/Library/Temp/obj, profile guards, ports
  or output paths. Prefer one heavy workload and warm caches. No real user profiles/services in tests.
- Back up the run's diff before restoring confirmed generated churn: Inday FppDetails,
  ProjectAuditorSettings, and QualitySettings/TimeManager when the run changed them.
  Preserve unrelated dirt and contributor changes. Require fresh nonzero XML and
  expected cases; exit codes or zero-test passes are insufficient.
- Inspect real native before/after views and normal-speed owner/observer motion,
  including the required greyscale thumbnails. Compilation, object spawn, staged
  captures, host-only checks and actual target builds prove different things.
  Never claim unperformed playback, human approval, freeform play or multiplayer validation.
  Native visual checks require graphics, not `-nographics`.
- When the final build is justified, use guarded GameBuilder.BuildWindows with
  `-buildOutput` into `Builds/<name>/TumbangPreso.exe`. Verify executable/data and
  run that exact build. Never replace the Desktop build.
- Keep the ledger concise: feature/decision, source/tested revision, dirty work,
  owned jobs/results, blockers and next action. Put detailed receipts in reports;
  update TODO, ledger and findings in the same commit as the corresponding work.

Engineering invariants:

- Core is engine-free; the Core project compiles the same embedded package sources.
  Host owns outcomes; MatchDirector.AddScore owns points. Preserve square confinement
  and deduplication. Bots submit InputIntent; overlapping stuns use Max.
  Stable identity is not ownership.
- Current source defines the network protocol. Wire changes require compatibility
  validation; never renumber existing values or trust historical version literals.
- Preserve the established sight-line launch and the guide's bounded movement/
  early-hold inaccuracy. Animation must cover preparation, release, contact,
  interruption and recovery without inventing hits or adding rooting. Support
  keyboard/mouse, controller and touch; body and first-person presentation must agree.

## Product, art and UI contracts

- Both modes ship and default to eight rounds, with configurable custom lengths.
  Classic people are cosmetic with neutral stats. No Street Hype mechanic or meter.
  Preserve the complete throw/can/retrieval/restore/chase/escape/tag exchange,
  ownership/identity/locators/feed, victim reconstruction/capped chains, distinctive
  hero performances and spectator/all-player highlights already implemented.
- Keep the approved cute blocky cast, flat faces, simple no-thumb hands, rig/bone paths,
  action names and GUIDs. No roster redesign. TUMP stays playful sport, without
  killing, guns, lasting destruction or realistic horror.
- Each hero/action needs its own form, silhouette, casting, movement and sound.
  Repeating noisy patterns/cracks/symbols is not distinct design. Retain Dante's
  orbiting protectors unless a demonstrated improvement warrants change; follow
  the fitted-armor-marking work in TODO. No noisy basalt overlay or rejected Inday studies.
- Keep the can, slippers, players, chalk and routes readable on Low and during
  overlaps. Layered spectacle is welcome when its parts complement each other;
  realism is not the goal. Label diagnostic/false-colour images before showing them.
- Preserve owner/girlfriend supplied menu/login artwork, pixels and aspect ratios.
  Home opens from the existing TAP TO START. No UI version stamps; retain internal
  version/protocol identity. Use readable Darumadrop with supporting type for dense
  text; in-game HUD labels are at least 28 canvas units. Generated placeholder icons
  are replaceable, not automatically approved art. Do not launch gratuitous icon or UI passes.
- VISUAL-1 authorized all in-game UI, results, halftime and icons; it superseded the
  older HUD prohibition, not rules, timing, authority or network contracts. Keep the
  HUD minimalist, professional and easy to read. Respect current TODO completion.
  Halftime is a compact popup over the visible game, not a green full-screen standings board.
- The Terms popup/consent control is the authorized exception within protected login:
  consent is empty or solid-filled, never a tick. Loading tips stay inline with
  rotating replacement artwork. Track new arrival/UI surfaces in the UX-1 inventory.
- Do not explain obvious controls. Back is an arrow without redundant BACK text;
  visible scrollbars do not need SCROLL TO READ. Keep useful bindings, rules, errors,
  constraints and state; preserve control identities and operation.
- All in-game UI text/icon strokes are BLACK via `UiTheme.InGameOutline`, not red,
  DeepInk or brand red. This includes HUD, spectator, notifications, prompts,
  countdown, ability, training/replay and match-chat surfaces. Preserve meaningful
  fills and supplied art; this does not recolour character/environment outlines.

## Map and character-specific requirements

- REFINE-2 starts from research into pleasing/satisfying games and stylised visuals,
  then a saved local plan. Use multiple references; 3d-asset.com is one source.
  Refine each of the five maps, animal behavior, bot inactivity and actual gameplay
  individually. Sean's stuck walking arms, copied-looking movement, throws and both
  pektus directions were explicitly reopened; reconcile newer work before redoing them.
- Buildings need construction-specific material/detail, not one texture/noise spammed
  everywhere. Shaders may help. Keep each map's Filipino place/cultural identity,
  native style, supplied imagery and readable action. Include coherent distant
  context visible in map select/lobby, skies, islands and mountains.
- Ambient sky needs slow per-map cloud drift with a steady horizon/sun and correct
  pause/replay sampling/return. Static sky is not completion.
- Lagoon must read as a documented Sama Dilaut/Sama-Bajau water village: refine house
  construction/layout/life, include detached houses on piles and retain islands/
  mountains. Fixed houses do not float/bob; houseboats may. Reference photos are
  reference, not shipped textures. Preserve court/recovery behavior.
- Roof recovery hangs/mashes/climbs from the actual rail. Lagoon uses swimming,
  Jump near a bridge, then button-mash climbing; no ordinary centre teleport.
- **Any character model, new or reworked, starts with
  [CHARACTER_MODEL_METHOD](docs/CHARACTER_MODEL_METHOD.md)** (owner, 2026-09-25): research
  first, the three standing questions (belongs in the cast, visually pleasing, great) on
  every render, an ink-only face, one quiet colour of its own, markings drawn by hand for
  the canvas that shows (never generated by repetitive code), and the versioned review loop.
- Rafi (islander rework 2026-09-25, v31; brief in
  `ArtSource/rafi/islander-rework-20260925/`): muted sea teal and silver, black ink, the
  method above. No gills in active model, concept, hooks or lore. Use the HERO cast and
  [canonical pipeline](docs/CANONICAL_RENDERING_PIPELINE.md) as the family reference.
  Keep blocky hair and no eyebrows, matching that cast's style.
  Retrofit `tools/build_rafi_voxel.py`, the dedicated copy of
  `tools/build_person_voxel.py`/[Voxel_Person_Guide](docs/Voxel_Person_Guide.md).
  Preserve the original builder and other characters. Use the native donor face/skull,
  proportions, chamfer and outline normals, not only a donor rig. Keep selected B
  identity/kit/map progress and Rafi's own character/lore. Use requested image ideation,
  then the real lineup. Never revive or spend more capture work on
  `ArtSource/rafi/rejected-box-recipe-20260922`.
- Inday: plain brown arms in every model and first person; remove pogo/coral arm
  attachments. Preserve the rest of her body, simple hands, rig, animation and palette.
- **Any hero kit, new or reworked (abilities, their animation, effects and sound, the ultimate's
  cutscene), starts with [HERO_KIT_METHOD](docs/HERO_KIT_METHOD.md)** (owner, 2026-09-27: Paete is
  *"THE BASELINE QUALITY OF EVERYTHING ELSE MOVING FORWARD"*, and the record of how he was built is
  for every other hero). Research from footage first, the six-beat table per ability, props typed
  by hand, one sound recipe per verb, a directed cutscene of at most 5.0 s, films in a match sent
  to the owner. Take the method, never Paete's look.
- Home/menu/season/showcase animation starts with [HOME_SCREEN_ANIMATION_METHOD](docs/HOME_SCREEN_ANIMATION_METHOD.md):
  research, real posed models, unaltered faces, retimed beats and frame-by-frame review.
  Give beats breathing room, expressive personality and a story about TUMP. Reuse
  the method, not Zack's loop content. Sources: `ArtSource/home-scene/`; reports:
  `docs/reports/home-scene/` (README for Zack, phaister.md for Phaister).

## Design references and preserved history

[NATIONALS_POLISH](docs/NATIONALS_POLISH.md) owns feature design, not status.
Its [delivery design](docs/NATIONALS_POLISH.md#current-delivery-design-2026-09-21)
and [VISUAL-1 design](docs/NATIONALS_POLISH.md#visual-communication-and-appeal-pass-visual-1-2026-09-23)
preserve the detailed rationale.
Consult relevant [play-feel](docs/PLAY_FEEL_REWORK_PLAN.md),
[abilities](docs/ABILITY_REWORK_PLAN.md), [kit decisions](docs/HERO_KIT_REWORK_DECISIONS.md),
[ability direction](docs/PHILIPPINE_ABILITY_DIRECTION.md), [art](docs/Art_Direction.md),
[rigs](ASTRA.md), [implementation brief](docs/reports/presentation-pass-2026-09-21/implementation-brief.md)
and [REFINE-2 research/execution plan](docs/reports/map-by-map-refinement-2026-09-23/research-and-execution-plan.md).

Exact prior instructions, quotations and dated sequencing are preserved in the
[2026-09-24 cleanup snapshot and coverage record](docs/reports/instructions-cleanup-2026-09-24/README.md)
and the [earlier intake archive](docs/reports/presentation-pass-2026-09-21/intake/AGENTS.md).
Use history to understand decisions, not to override current owner direction.
