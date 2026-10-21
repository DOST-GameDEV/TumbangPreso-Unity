Implement the gameplay-feel, presentation, and watchability rework for **Tumbang Preso (TUMP)**. Improve the existing game, integrate and thoroughly refine my friend's placeholder work, and reorder the active backlog so visible gameplay improvements come first. **Build, run, inspect, and iterate; do not stop at another plan.**

Act as my lead developer, gameplay-presentation designer, and animation/VFX/audio technical director. Use your current project knowledge and creativity to improve the suggestions below. Treat them as a guide to the experience I want, not rigid recipes.

Repository and default integration/delivery branch:

```text
https://github.com/DOST-GameDEV/TumbangPreso-Unity/tree/ASTRAReworks
```

### Implementation mandate

**Implement and integrate the relevant friend-work for delivery on `ASTRAReworks` using the non-destructive workflow below. No separate “go ahead” is needed for the in-scope work.** Inspect the latest checkout, resolve the important unknowns, then edit the game, author/refine assets, integrate the changes, run it, inspect the result, and iterate in this assignment. This includes relevant code, animations, viewmodels, VFX, SFX, cameras, gameplay HUD, ownership/chain logic, spectator/replay integration, settings, tests, and necessary documentation.

Research and planning support the build; they are not the final product. Briefly explain your approach and then act. Do not end at a diagnosis, a proposed architecture, empty scaffolding, one isolated effect, or another implementation prompt. Carry the coherent pass forward through the actual play routes in both modes.

### Creative authority: follow the intent, improve the execution

**Treat every proposed timing, pose, shot, label, effect recipe, score number, class name, replay architecture, and development order below as a starting point. You have permission to change them.** The document explains the experience I want; it does not claim to know the best implementation for the current game.

You may combine or replace weaker ideas, reorder work, refactor systems that need it, author stronger choreography, tune the presentation, and add high-value opportunities we missed. Do not retain an inferior solution just because it appears here. Do not blindly restore old behavior when the current game already has something better.

Keep the intended outcomes recognizable: a simple Tumbang Preso loop, satisfying physical actions, intuitive danger, tailored viewer experiences, real ultimate character moments, clear ownership, distinct side-feed and major-moment channels, rewarding capped chains, and worthwhile halftime highlights. These are the direction to carry through, not dozens of disconnected boxes to tick. An improved equivalent is welcome; quietly dropping difficult parts or replacing the ambition with generic minimal effects is not.

**Make ordinary design and engineering decisions yourself, implement them, and judge them in motion.** A meaningful departure needs a brief rationale in your progress notes, not a new permission request for every experiment. Ask only when the choice would fundamentally change the core rules or identity, conflict with another active work lane, or require access, spending, destructive changes, or other permission you do not have. Own-only slipper retrieval, modest capped chain bonuses, and the necessary shared cinematic/replay behavior are already within this assignment.

If a detailed example below conflicts with a better in-game solution, this creative-authority section takes precedence over the example. Current explicit owner decisions and genuine project safety/authority boundaries still apply. Preserve truthful outcomes, control, multiplayer consistency, and TUMP's identity while improving how we achieve them.

### Work autonomously, with continuity

Maintain the existing active ledger as a compact, reliable resume point, following the documentation workflow below. Keep the current direction, decisions, actual implementation/verification state, running jobs, and next action current. Update changed decisions instead of repeatedly appending contradictory “LATEST” paragraphs. Record reasoning briefly; do not turn every aesthetic choice into another permanent rule.

Do not wait for approval between ordinary stages or stop automatically after one hero or one successful prototype. Continue through the scoped pass as the environment allows. Make coherent commits and follow the active repository workflow for pushing checkpoints to `ASTRAReworks`. Preserve unrelated work, active reservations, and genuinely approved assets; no work on `main`, destructive history rewriting, or unrelated deployment. Preserve and improve the identified friend-branch placeholders under the friend-work instructions rather than treating them as finalized assets.

If a real blocker prevents one part, isolate it, continue independent work, and report the exact remaining dependency. Never substitute a placeholder or an untested claim for completion. If a session ends before everything is finished, leave a usable checkpoint and precise continuation state rather than claiming the full pass is done.

We have time to do this properly. Do not reduce the goal to a shallow one-day polish pass, and do not spend the assignment endlessly replanning. Build, observe, improve, and keep the game simple.

---

## Work faster without lowering the quality bar

**Cut redundant testing, repeated setup, idle waiting, and repeated context loading—not creative iteration or meaningful verification.** The earlier runs felt too slow, but the cause is not established. Use existing timestamps/logs to distinguish implementation, asset import/compilation, test execution, visual review, polling, and documentation overhead. A short elapsed-time note per meaningful checkpoint is enough; do not build a time-tracking system or investigate old five-hour tasks before making progress.

These workflow instructions govern how to satisfy the later test and evidence requirements. Those requirements describe behaviors to establish across a feature, not a command to rerun the whole matrix after every edit. Preserve the ambitious game-feel scope, creative freedom, friend-work refinement, and maps/new-characters-later priorities.

### Choose tests by what changed and what could actually break

Before launching an expensive check, be able to name **the changed behavior, the realistic failure it addresses, and the decision its result will change**. Do this briefly in the existing job/checkpoint record, not a separate report for each command. Prefer the cheapest check that answers that question.

| Change | Useful first validation | Broader validation only when justified |
|---|---|---|
| Documentation or comments only | Relevant diff, links, references, and tooling assumptions. | No Unity build or gameplay suite unless the edit affects generated behavior, executable configuration, or a tool contract. |
| Local visual/audio tuning with unchanged gameplay and timing contracts | Targeted in-engine preview/listen/capture in the affected real situation; inspect the result. Compile/import only what is needed. | Nearby clipping, representative distance/aspect, overlap, or low-settings cases when the changed asset makes them relevant. Not the entire LAN/UI suite for every color or ember adjustment. Shared shader/material changes have a wider affected surface. |
| Animation, viewmodel, camera, input, or interruption changes | Focused action, transition, failure/return-to-control checks, plus real motion review. | Native-player, owner/observer, aim, held-item, and timing cases actually touched by the change. |
| Ownership, score, chain logic, abilities, or authoritative messages | Focused success/refusal/duplicate/boundary regressions and relevant host/client integration. | Cover the changed authority and state-lifecycle contracts before marking them verified. Cosmetic screenshots cannot replace this evidence. |
| Shared cinematic time, replay, or cross-system integration | Focused pause/resume, cleanup, faithful playback, resource/score invariants, and participant delivery checks. | A coherent native and multiplayer checkpoint, relevant concurrent/late/interrupted cases, and representative busy play. |
| Engine/package/build-pipeline changes or a release candidate | The broader affected suite and the actual intended build/runtime. | Expand for cross-cutting risk or unresolved failures. A clean rebuild is a specific diagnostic/release choice, not the default after ordinary edits. |

Reuse relevant existing regressions, fixtures, capture tools, and passing evidence. Select tests by names/categories/assemblies through the installed framework or the project's existing runner. Verify the selection actually executed the intended cases: an empty result, zero selected tests, or a process exit alone is not a pass. Check the installed tool version before copying command-line flags.

Batch related refinements into coherent playable changes. During a fast art/animation iteration, preview the affected moment instead of rebuilding and qualifying the whole game after each parameter tweak. At a meaningful integration checkpoint, run the needed native/multiplayer checks and inspect the exact output. Do not accumulate a huge unverified rewrite while claiming to save time.

Keep import/build caches warm within each workspace; avoid routine deletion of `Library`, repeated full exports, repeated engine launches for adjacent checks, and rebuilding the same executable for unchanged scenarios. Reuse existing focused capture routes and vary their inputs when possible. Do not disable reload/reset behavior just to make tests faster without preserving the state-isolation contract.

**A passing test on unchanged behavior is not work to repeat for reassurance.** Reuse its recorded scope when relevant dependencies, configuration, and assets have not changed. Do not fabricate a fresh run or transfer its pass status to materially different source. Shared dependencies count, not just the edited filename.

Do not weaken assertions, delete important tests, retry failures until they happen to pass, or suppress errors to shorten a run. Separate a production defect from a fixture/environment failure using a focused discriminating check. Retain the failing evidence and unresolved risk. If a check is genuinely redundant or obsolete, remove it from automatic execution with a short reason; preserve useful history and coverage. Unavailable hardware or human feedback is pending review, not a reason for endless reruns.

### Use a development workspace and a stable validation workspace

**Implement the two-folder approach when it helps on the actual machine.** Prefer two long-lived isolated workspaces rather than copying the whole project for every run:

- **Development workspace:** the authoritative editable source for this assignment, normally on `ASTRAReworks`. Continue coding, authoring, and integrating here.
- **Validation workspace:** a pinned snapshot of one coherent development checkpoint. Run its selected tests, builds, and captures without changing the project inputs during the run.

A detached Git worktree is a useful starting choice; a separate clone or safely prepared snapshot copy is also acceptable if current tooling, LFS/submodules, or isolation requirements make it better. Reuse an existing safe setup before creating another. Use local workspace paths discovered on this machine, not hard-coded paths from earlier reports. Keep the validation directory outside the development project's imported `Assets` tree.

Git worktrees have separate working directories/indexes but share repository refs and configuration. Keep the validation tree detached or on a dedicated test ref; do not force the same editable branch into two trees or make conflicting shared-Git configuration/history changes. This is tool/process parallelism for the same agent, not permission to start another agent or ignore contributor reservations.

Use this loop:

1. **Snapshot deliberately.** Prefer a coherent commit containing the intended code, assets, `.meta` files, scenes/prefabs, and package/project configuration. Do not sweep a friend's unrelated work or secrets into a commit. If a dirty checkpoint must be tested, capture an explicit base revision plus complete input/patch manifest, including required new/untracked files and relevant generated inputs. A `HEAD` hash alone does not identify dirty source.
2. **Freeze the validation inputs.** Synchronize only when that workspace's owned jobs have stopped. Resolve its exact dependencies and verify necessary assets are present before running. Never live-sync development edits into the copy under test. Detect unexpected source/importer rewrites; review them rather than silently calling a changed input tree the original snapshot.
3. **Launch with durable evidence.** Give the job an ID, exact snapshot identity, selected cases, engine/build configuration, workspace/profile, command, log/result paths, and a discoverable owned process/tool handle. Use the existing guarded runner where possible. Write results outside imported assets and avoid relying on a chat message as the only record that a run exists.
4. **Keep working while it runs.** When the execution tools permit nonblocking jobs, launch and return to development work that is safe to advance. Review an existing capture, author the next related asset, implement an independent part, or update the short ledger. Check completion at natural work boundaries or useful intervals, not with constant empty polling. Do not end the assignment simply because a test started, or claim background progress if the available tools cannot actually keep it running.
5. **Review and incorporate.** Inspect failures and useful captures promptly. Fix in development, then validate a new pinned checkpoint. Do not automatically copy the validation folder back over newer development. Any intentional source fix made while diagnosing in validation must be explicitly recovered and integrated with its provenance; generated logs/caches are not source patches.
6. **Deliver the version you actually checked.** Record development HEAD separately from the tested snapshot/build. Before calling the delivered result verified, validate that exact relevant source state and intended artifact; documentation-only differences can be identified without an unnecessary rebuild. Keep a usable tested fallback while further development continues.

The tested snapshot stays fixed even when development advances. Tests on checkpoint A do not prove new changes B and C work. Keep the amount of unverified follow-on work modest, especially when the pending result decides an architecture or gameplay contract. Do not build a large dependent stack on a foundation known to be failing. Independent work may continue.

### Make the isolation real; two folder names are not enough

Give each Unity workspace its own writable `Library`, `Temp`, `obj`, logs, build outputs, and intermediate files. Do not symlink or hard-link writable source/assets or Unity working caches between them. Do not clone a live mutable cache and assume it is consistent. Reuse each workspace's own cache; use already-supported shared immutable caches only through their intended tooling. Do not erase caches to “make sure” without evidence of a cache problem.

**Separate test profiles and machine-global state too.** Two copies of the same game can still address the same saves/preferences/accounts, ports, lock files, and external services. Use the project's isolated test profiles and explicit output/port paths; never run competing backup-and-restore guards against the same real player profile. If isolation cannot be established, serialize that resource while continuing safe development work. No tests against real user data or production services merely because a copied project was used.

Keep one Unity Editor per project directory. A validation run in its own directory does not prohibit code/asset edits in the separate development directory. The frozen-input rule applies to the workspace being tested or built. Where older instructions imply all coding must stop during any Unity run, update that scheduling assumption while preserving its safety purpose and active ownership boundaries.

Prefer one heavy Unity/render/build workload at a time unless measured CPU/RAM/GPU/disk headroom, licensing, display/capture access, and profile isolation justify more. Coding alongside one test process is often the useful overlap; running two GPU-heavy scenes is not automatically faster. Do not measure target frame performance while competing jobs distort the result. Respect the engine/project restrictions and do not bypass locks, licensing, guarded launches, or another contributor's job.

Use timeouts and progress/heartbeat information appropriate to the actual operation. A long asset import is not a hung gameplay test. Before restarting, inspect the owned process and recent output; retry only with a reason. Do not kill unrelated processes. Reuse one validation workspace, coalesce queued revisions, and cancel only owned obsolete work when it no longer answers a useful question. Avoid an ever-growing queue of outdated builds.

Do not turn this setup into a new CI or tooling project. Make the smallest safe runner/profile adjustment needed, or use a simpler isolated-copy/sequential-heavy-work fallback and keep implementing. The two-folder layout, concurrency level, and batching are adaptable means to better throughput, not sacred architecture.

## Keep the ledger and documentation small, durable, and current

**Clean up the active documentation now, then maintain it incrementally.** This explicitly includes the docs that currently direct the agent—`AGENTS.md` routing, `docs/TODO.md`, `docs/ACTIVE_REWORK_LEDGER.md`, `EXECUTION_PLAN.md`, `GAMEPLAY_RESUME_AFTER_UI.md`, and equivalent current entry points. Inspect the actual files first. Preserve standing rules, genuine permissions/reservations, approved art decisions, deferred features, and historical evidence. Do not use “cleanup” to remove a safety rule, rewrite history, or hide an unfinished feature.

Keep one authoritative place for each kind of information:

| Information | Where it belongs |
|---|---|
| Standing owner constraints, branch/integration rules, tool/profile safety, and where to start | A short repository entry point such as `AGENTS.md`, with direct links to details. |
| Active, deferred, retired, and externally blocked work | One current task queue, normally `docs/TODO.md`; preserve stable task identifiers. |
| Exactly where to resume this work | One active ledger, not several competing session journals. |
| Detailed designs, durable decisions, rejected approaches and their reasons | Topic documents/reference sections, read only when relevant. Keep useful recipes and alternatives, not just a terse summary. |
| Run outputs, detailed investigations, completed batches, old instructions | Reports and clearly labeled archives with stable links; not the default startup context. |

Use existing equivalents instead of creating every named file anew. Briefly inventory the current doc entry points, consolidate duplicate active instructions, fix contradictory scheduling, and make old execution/resume documents point directly to the real queue and ledger. Extract still-binding constraints before archiving old paragraphs. Preserve task IDs, cited evidence, licenses, ownership decisions, and important rejected approaches. Fix affected links/anchors or leave an appropriate redirect when relocating material. Do not mass-delete or mechanically summarize away unique content.

This includes authorization to maintain project instruction documents for this workflow, but not to override higher-priority instructions or actual contributor boundaries. Broad historical rewriting, report beautification, and exhaustive archive reading remain lower priority. Make the high-value active-doc cleanup once, then clean a topic when work touches it. Do not spend another day “future-proofing” docs before improving the game.

### The active ledger must survive compaction

Aim for a readable current ledger of roughly **100–150 lines**, or an equivalent small resume packet. This is a soft target: preserve necessary handoff facts, and move long history behind links instead of deleting it. Keep it self-sufficient for the next concrete action.

Record:

- Current outcome, next concrete action, and the important creative decisions already made—especially what not to restart.
- Development branch/HEAD, meaningful dirty/untracked work, friend-branch integration state, and which files are owned/reserved.
- Last integrated/visually inspected checkpoint versus exact last tested snapshot/build; pending changes are explicitly not yet verified.
- Running or queued jobs: job ID, machine/workspace, snapshot, command/handle or owned PID plus start information, isolated profile, log/results, and when to inspect it next. Historical PIDs must never be reused blindly.
- Current failure or blocker, the latest relevant finding, the next discriminating experiment if one is needed, and links to the minimum supporting evidence.
- Any safe pending integration/cleanup and a precise resume command or file/function pointer when useful.

Update this record at meaningful boundaries: a coherent implementation checkpoint, changed design decision, job launch/completion/failure, important integration, or before a likely compaction/handoff. Do not rewrite the whole ledger after every tool call. Keep machine-specific live process paths in that machine's current run note, not as permanent cross-machine instructions. Never store credentials in documentation.

On resume, check the short entry instructions, active ledger, current queue, and actual branch/worktree/job state. Reconcile only the changes since the recorded checkpoint. Read the relevant design/code/receipt on demand. Do not reload the entire prompt, all historical ledgers, or every completed report at each compaction. Do not relaunch a test simply because the conversation lost its status; inspect the recorded job/result first.

### Keep context costs low without losing the design

Replace ambiguous “current/latest/tomorrow” paragraphs with one genuinely current status and dated/revision-scoped historical notes. Mark decisions as accepted, prototype, rejected, superseded, or pending where that distinction matters. Use repository-relative paths and stable headings/symbols, and link to code/config for volatile values rather than copying them into five documents. Historical measurements keep their original environment and revision.

Keep this detailed creative guide once as reference material. Extract a short list of durable outcomes and the active feature's relevant details into the working docs; link back to the complete guidance. Do not paste this entire document into every ledger or let compaction replace the ambitious scope with a handful of generic bullets.

Prefer targeted file ranges, symbols, diffs, and log excerpts. Exclude archives/generated outputs from routine broad searches unless investigating history or artifacts. Keep full test output in files; surface a compact result, important warnings, and the first relevant error/stack trace. Reuse one evidence folder per coherent batch rather than creating a new report for every tiny edit.

At checkpoints, briefly report what visibly improved, which exact state was tested, what meaningful test work was avoided/reused, and the next action. Diagnose disproportionate test/setup/context overhead if it recurs. No fixed percentage of time, test-count quota, or five-hour task limit replaces judgment: optimize for **finished improvements per work cycle with trustworthy evidence**, not maximum commands, green checks, or lines of documentation.

---

## Improve my friend's placeholder work

### Improve the starter work without discarding useful progress

My friend has started some of this work on a different branch. **That is okay and authorized. I want us to use and substantially improve what they made; the relevant work they started is placeholder work, not a finished quality bar.**

Do not reject it just because it did not originate on `ASTRAReworks`. Do not ignore it and build the same feature independently. Equally, do not assume that “it exists,” “it compiles,” or “my friend made it” means it already meets the presentation standard of this guide.

**Preserve progress, not its roughness.** Keep useful concepts, behavior, assets, and integration, while giving the placeholders the same deep creative review as everything else. You can refactor or replace weak code, staging, cameras, animations, effects, sound, and gameplay HUD presentation when that produces a stronger, coherent result within this assignment.

This permission is about the friend's identified placeholder work. It does not erase separate explicit owner approvals for finished character designs, menu artwork, rigs, saved content, or unrelated systems.

### Identify and compare the actual work before duplicating it

The friend's branch name is **not specified in this prompt**. Identify it from the real repository, branch history, relevant pull requests/commits, current project notes, or an accompanying owner message. Do not assume a historical UI/controller branch, whichever branch was modified most recently, or a similarly named branch is the intended one.

If the branch cannot be identified confidently, ask for its name or link once while continuing independent work that will not conflict. Do not invent its contents or claim it has been integrated. Do not repeatedly request confirmation when the repository already resolves the question.

Compare the identified work against the actual latest integration state, including local uncommitted work where available. Check which changes are already merged or equivalently implemented before applying anything again. A different commit hash alone is not proof that the feature is absent.

Keep a short intake note in the existing ledger: source branch/commit, feature purpose, what is already integrated, what is functional, what remains placeholder-quality, and the next useful refinement. This is a focused intake, not permission to spend the assignment cataloguing every branch or reviewing unrelated history.

### Integration permission and destination

**`ASTRAReworks` remains the default integration and delivery destination unless I explicitly provide a newer destination.** The old branch-only wording means no accidental work on or delivery to `main`; it is not a prohibition on reading, comparing, or safely testing my friend's approved branch.

You are authorized to bring the relevant identified work into `ASTRAReworks` through the project's normal non-destructive integration workflow. Choose a scoped merge, selected commits, or an adapted implementation according to the actual differences and dependencies. A temporary integration branch or isolated worktree is fine when it helps keep this safe. This does not authorize another agent, another paid service, or unrelated deployment.

Do not blindly merge an entire branch containing unrelated or deferred changes just because one feature is useful. Preserve commit provenance and authorship. Do not force-push, reset, delete the friend's branch, overwrite uncommitted work, or publish changes to their source branch without the relevant permission.

Where both branches changed a shared system, reconcile their intended functionality rather than resolving the entire file with a blanket “ours” or “theirs.” Integrate the required assets, `.meta`/GUID relationships, prefab and scene wiring, dependencies, and relevant tests together; a copied script alone may not be an integrated feature. Retain stable save/network identities and preserve valid behavior from both sides.

If another contributor is actively editing the same files or using the same checkout, respect the current ownership and arrange a bounded handover. The fact that the work is on another branch is allowed; that does not itself release an active C4 or other file reservation. Conversely, do not keep honoring a reservation that has an actual documented handback.

Discover the current engine, tools, working directories, and process ownership rather than copying old machine paths or PIDs. Preserve the project's guarded Unity/profile workflow. Keep the workspace under test/build stable; continue safe coding or asset work in the isolated development workspace under the workflow above. Once a coherent checkpoint is integrated, follow the normal workflow for committing/pushing it to `ASTRAReworks` and verify the actual destination. Do not claim publication if access or tools prevented it.

### Thorough placeholder refinement is part of implementation

For each relevant starter feature, inspect what a player actually sees and feels through the normal game route, then improve its complete interaction. Do not merely recolor the placeholders or add a small extra effect and call the work done.

Examples of how to apply the main brief to existing starters:

| What the friend may already have started | How to develop it rather than duplicate or freeze it |
|---|---|
| An event feed or central banner | Preserve its useful event wiring, then refine identity, typography, placement, motion, prioritization, deduplication, and expiration. Keep the side feed distinct from major moments. |
| A tag camera or a basic ultimate cutscene | Preserve useful triggers and timing contracts, then author the actual character performance, shot composition, sound, contact, transitions, and return to control. Validate victim/taya/other-player differences and actual pause behavior. |
| A can/slipper marker or ownership prototype | Verify the real ownership/state behavior, then improve the low locator, silhouette, distance handling, colors, and truthful objective states. Do not build a competing marker or pickup rule beside it. |
| A replay or highlight prototype | Preserve useful recording/event work, then develop faithful contact, candidate retention, quality, selection, participant delivery, and playback safety. A demo overlay is not automatically final replay. |
| Starter body/FPP animation, VFX, or audio | Improve anticipation, contact, direction, weight, follow-through, materials, mixing, and busy-match readability together. Do not preserve generic effects just because they passed a spawn test. |
| An underlying helper or event architecture | Reuse it if sound, refactor it if awkward, or replace the weak part if necessary. Preserve the useful behavior and avoid two authorities or two active presentation paths. |

These are conditional examples, **not claims that those features exist on the friend's branch**.

Evaluate both technical completeness and perceptual quality. A placeholder may be correctly wired but visually weak, visually appealing but not authoritative, or neither. Test the real contract and inspect the actual result before deciding to keep, refine, or replace it.

Regression tests that protect real gameplay or integration behavior should survive. Tests that merely freeze a superseded placeholder layout, effect recipe, or timing can be updated to the intended result once that result is established. Do not weaken meaningful tests just to produce a green report.

Your creativity should improve the starter work, not be limited by it. A brief rationale for major departures is enough; you do not need to ask me about every pose, shot, sound, shader, or ordinary refactor. Show the difference in normal gameplay when the tools support it, not only in an isolated showcase.

### Friend-branch work does not reverse the backlog priorities

If the friend's work concerns the current game-feel, readability, tags, ultimates, feeds, chains, audio/camera, or spectator/replay pass, incorporate and refine it in the relevant active feature.

If it concerns **new maps, new characters, broad existing-map beautification, or another deferred area**, preserve its source, useful concepts, and continuation notes for later. Do not expand the current assignment merely because a prototype already exists.

If deferred work has already been integrated into the game, do not automatically delete, roll back, or disable it. Preserve the functioning baseline, fix a genuine immediate regression if necessary, and defer further expansion/polish. “Later” means scheduling, not destruction.

Continue to improve the existing characters' actions and their in-scope presentation now. Deferring a new character is not permission to postpone the six existing heroes' signature performances.

---

## Prioritize visible gameplay progress

This is my latest scheduling instruction. **Implement the priority cleanup, then continue implementing the gameplay-feel/watchability pass in the same assignment. Do not stop after giving me another audit or plan.**

I have had an agent working for days, but the game does not feel improved enough yet. That does not mean the engineering fixes were worthless. Preserve them. What I want to change is what the agent chooses next and what counts as meaningful progress.

> **Do not try to empty the historical TODO list before making the game noticeably better. Build the excellent existing-game experience described below, with engineering and testing supporting it.**

Your current project knowledge and creative judgment still matter. Timings, choreography, implementation details, and the order of related features remain flexible. Choose a better route when the actual game supports it. These priorities do not limit your ability to improve the ideas.

### What takes precedence now

The gameplay-presentation pass is the main deliverable: a satisfying throw → can hit → retrieval → chase → escape/tag exchange; readable ownership and objective state; viewer-specific tag presentation; substantial bespoke ultimate moments; distinct side-feed and major-announcement channels; capped chain bonuses; and worthwhile spectator/halftime presentation.

**Maps and new characters go later.** This includes both new-map/new-hero expansion and broad visual reconstruction of existing maps. Preserve their assets, concepts, unfinished work, and evidence. Do not delete them or pretend they are complete.

This overrides historical scheduling instructions such as “complete the ENTIRE TODO list,” “finish all UI first,” “start with the map review,” and old demo-day ordering where they compete with this assignment. Those requests remain history or deferred work, not a reason to exhaust every old item now.

This does **not** override actual safety boundaries, existing contributor ownership, preservation of local work, or the game's authority/control rules. It is not permission to hide regressions or ship broken multiplayer.

### Reconcile the real checkout once, then build

A prior external review inspected the **pushed `ASTRAReworks` branch at `a6f8b275d0083ca2066793e447dca3a92b07c29d`, recorded as September 16, 2026**. Treat this as historical evidence, not the current state. The latest pushed change there was the Sean ember-debris refinement. It did not inspect your PC's uncommitted work, unpublished commits, running tools, or current executable. [R1]

**Use your actual latest state. Do not reset to that hash or assume no further work exists.**

Briefly inspect branch, upstream divergence, working changes, the identified friend-branch work, current ledger, build evidence, and any active owned processes. Preserve a usable checkpoint and unfinished real work. If another worker is still editing or running Unity against the same checkout, arrange a safe handover rather than starting competing writes or killing someone else's process.

Read the current top-level queue and the newest relevant completion reports. Open older sections only when needed to decide a live task. Do not reread the entire historical archive as a mandatory startup ritual or reproduce every old result before editing anything.

Make one compact reconciliation of the active work. Use the existing TODO and ledger rather than creating another parallel master backlog. Each actionable entry should say what changes for the player, what actually remains, and what evidence or dependency matters next.

### Put these outcomes at the front

This is a dependency-aware direction, **not a demand for six rigid waterfall stages**. Merge or reorder neighboring work when that produces a better playable result. A relevant architecture or networking dependency can be tackled early; an unrelated open umbrella task cannot block everything.

| Priority | Working outcome | How to keep it focused |
|---|---|---|
| **Urgent exceptions** | Fix a demonstrated or concretely established blocker: broken startup/match entry, lost input, invalid possession, wrong scores/roles, stuck recovery/cinematic, a serious authority flaw, or an action unavailable on the real route. | Repair the affected cause and legitimate path. Do not use “reliability first” to reopen the entire network/device matrix. If the current build already passes the relevant route and it is unchanged, preserve that evidence. |
| **Main focus: repeated gameplay exchange** | Improve movement/turns/feet and body/FPP continuity where they affect actual actions; release and Pektus; can impact; slipper landing and owned retrieval; restore; lunge/block/whiff; escape and tag. Integrate player identity, low slipper locator, state-aware lata marker, and the small side feed. | Use one existing arena, likely Eskinita, as the reference environment. Author complete interactions at normal speed with other players active. Do not require a new map, rebuilt roster, or every animation variant to be finished first. |
| **Signature moments, still current work** | Deliver the victim-specific catch scene, taya confirmation without new lockout, distinct central milestones and capped chain rewards, plus real hero-specific ultimate performances and their actual consequences. | Plan shared contact/history needs early enough to avoid disposable duplicate recorders. Prove an integrated representative tag and ultimate without waiting for every historical coverage task. Then extend the quality across the existing cast. These are not “post-polish someday” features. |
| **Existing heroes as a coherent game** | Improve and check the six current heroes and their eighteen main ability slots, including first-person identity, tells, actual effects, refusal/interruption, and attacker/defender use. | Preserve already-good kits/assets. Check each as it is changed and in a representative busy match. Do not repeat wholesale redesigns or complete every alternate × map × device × reconnect permutation before visible progress. |
| **Watchability and pacing** | Refine the existing spectator director, preserve meaningful highlights, deliver participant-visible halftime playback, shorten ordinary transitions, and clarify score/role changes. | Existing spectator controls and manual replay are a foundation, not a blank slate. Build the missing story selection, reconstruction/delivery, and halftime integration. Keep the full presentation ambition. |
| **Integrated qualification** | Validate changed behavior in real normal-speed play, a current Windows build, relevant owner/observer views and LAN paths, then full-match performance and repeated-use comfort. | Testing happens throughout the work, not only here. This is the integrated pass after substantial changes, not permission to rerun all historical suites after every particle edit. |

Maintain a modest, usable baseline in both Classic and Hero Strike. Do not make the whole game wait for one perfect character. Equally, do not create fifteen half-integrated effects and call that breadth progress.

### Retire these from active execution, within their actual evidence limits

“Retire” here means remove from the automatic pick-next queue. It does **not** mean delete production features, tests, historical evidence, saved assets, or unresolved caveats.

The following decisions are grounded in the reviewed branch. Reconcile them against fresher local work if necessary.

| Old task or instruction | Disposition now | When to revisit |
|---|---|---|
| **TODO151.9 / C2: investigate the old 5-hits-in-125 bot-lunge ratio again** | The newer engineering report traced wasted lunges to bots punching/tagging during the charge and then releasing at an already-gone target. A focused AI decision fix and regressions are recorded, without retuning human combat numbers. Retire the old unanswered-investigation wording; preserve the fix. [R4] | A current representative match shows a new decision defect, the relevant AI logic changes, or a focused regression fails. Ordinary lunge animation/feel remains current presentation work. |
| **TODO149.5 / C3: introduce a slipper registry/cache because lookups sound expensive** | The measurement task is complete with a justified **no production rewrite** result. The assigned sites measured about 0.052–0.096 ms per frame in the tested macOS Editor workload. This is not Windows certification, but it is not a reason for an unmeasured registry rewrite either. [R4] | Target-hardware profiling identifies this cost, object counts/call frequency change materially, or the new implementation introduces a measurable regression. |
| **TODO151.9 / C1: keep recovering the exact historical “48 idle penalties” run** | Preserve the demonstrated retrieval-yield and stuck-detection fixes. Mark the exact historical attribution as **unresolved historical / not scheduled**, not “fixed.” The per-seed original data was unavailable and that exact run was not reproduced. It must not remain a standing prerequisite for new gameplay work. [R4] | The missing original evidence becomes available or a current equivalent produces a usable causal trace. A bot currently unable to retrieve its slipper remains an active defect. |
| **Redo basic spectator entry, POV item hiding/restoration, manual/free/autopilot handoff, replay Escape/footer/aspect fixes** | The Windows v37 report records a bounded native pass for those named cases. Preserve it. This is not permission to declare all cinematic framing, network spectators, or halftime playback complete. [R2] [R8] | The new camera/replay work touches that behavior or a current regression appears. Re-run the affected tests, not an unrelated exhaustive spectator rebuild. |
| **Redo finished main-menu/login artwork, dust/cloud revisions, or completed native route/control fixes** | The ledger records implemented menu work, direct-control fixes, and a v29 combined native route/control checkpoint. Keep those working foundations. Retire old instructions to perform the same finished work again. Owner taste review remains distinct. The friend's newly identified in-scope placeholders are not covered by a blanket 'finished' label; improve them under the friend-work instructions without reopening unrelated finalized menus. [R2] | A relevant new change breaks the route, a current visible defect appears, or the owner gives new feedback. Necessary gameplay HUD work is not deferred menu polish. |
| **Re-run named completed round-boundary, pending-cast, charge-tail, or movement-joining cases as fresh investigations** | The resume list already identifies specific qualified cases and explicitly says not to redo them by default. Preserve those receipts and their scope. Broad lifecycle coverage remains separate. [R3] | Your cinematic/ownership/event changes invalidate a covered assumption, a dependency changes, or current evidence gives a reason. |
| **“Publish Sean ember batch” as the current next task** | At the reviewed remote HEAD, that batch was already the latest pushed commit. Reconcile the ledger's stale publication instruction. Do not manufacture a duplicate change just to satisfy it. A current build may still need to include that source change. [R1] [R2] | Actual unpublished local changes or a real build/integration dependency remain. |
| **Old “START HERE” execution tables, one-task/animation-only limits, superseded model studies, obsolete demo deadlines** | Keep useful reasoning and stable task references, but remove their authority as current scheduling instructions. The older index still advertises historical lunge/lookup investigations and obsolete work splits beneath the newer queue. [R6] | A specific current dependency requires the underlying evidence, not because an old heading says “current.” |

An open parent such as TODO152.4 or TODO151.9 is not proof every child still needs implementation. Split the remaining acceptance gap from the code already delivered. Do not check an entire parent complete merely because you retired a stale child instruction.

### Move these valid but lower-priority tasks to later

#### Maps and expansion: explicitly deferred

Move **broad existing-map composition, tree/vegetation replacement, facade/landmark reconstruction, environmental decoration, cultural-reference expansion, lighting beautification, and map-generation showcase work** out of the active presentation queue. Preserve `MAP_FINAL_PASS.md` as the deferred plan. [R7]

Likewise defer the **seventh hero/new character, including Rafi and the associated selected stilt-lagoon village expansion**, its new kit, models, concept iteration, lore expansion, and new-map implementation. Keep approved concepts intact. The existing queue already says expansion is last; this message makes the same deferral explicit for broad existing-map beautification too. [R3]

Also defer the separate **Inday reconstruction/special FPP-framing project** unless the current playable character has a serious defect blocking the selected exchange. Do not revive rejected body/arm studies or redesign the existing roster.

This is a scheduling boundary, not a ban on touching a scene. Fix a **specific existing-map collision/retrieval problem, severe occlusion, or measured render bottleneck** when it directly prevents current play or the new presentation from working. Keep the repair bounded. A small needed visibility correction is not permission to reopen the whole map pass.

#### Broad coverage and secondary systems

Defer the remaining broad **U8 secondary-dialog, native preview-pixel/high-DPI, touch-editor, and device-specific coverage** unless a changed primary PC path is actually broken. The reviewed TODO puts U8 at post-candidate item 7, before the larger gameplay resume item 8. Reverse that priority for this assignment. [R5]

Defer exhaustive **all-hero/all-alternative/all-map/all-device/all-frame-rate/all-reconnect** combinations. Keep the default existing kits and touched alternatives correct; do not confuse preserving them with demanding the full cross-product before implementing the first finished interaction.

Defer broad host-switch/outage/process-rejoin and rare phase-permutation matrices that do not establish a needed current feature. Also defer water/swimming/roof-fall polish and cross-device recovery permutations outside the selected normal-play work. Preserve their current functioning behavior and revisit affected cases whenever shared code changes.

Defer speculative lookup optimization, renderer/graphics-tier rewrites, generic abstraction cleanups, and new diagnostic infrastructure without a concrete current decision they enable. Use measured frame-time problems on intended hardware to prioritize performance.

Do not use these deferrals to drop essential request authority, correct scores, normal LAN/rematch behavior, camera restoration, relevant accessibility controls, or regression tests for changed code. Those belong with the feature that needs them.

Human-only listening, physical-device checks, taste approval, and unavailable-machine validation should be labeled **awaiting external review** with a ready build/clip when feasible. They are not automatically failed implementation tasks and should not trigger an endless rebuild loop while waiting.

### Keep request safety useful, targeted, and with the correct owner

The reviewed **C1–C3 lane explicitly ends with a handback releasing its files to Codex**, although older resume/execution text still says reserved. Reconcile that stale ownership text against any newer instructions. Do not continue blocking normal `Slipper`/`Carrier` work on a reservation that was actually handed back. [R3] [R4]

**C4 / TODO149.4 is different.** Its request-safety lane remains open and reserved in the reviewed branch. Its report records successful tested host guards and no demonstrated host-side defect in those cases, but leaves client-prediction findings and further qualification open. That is neither “all networking is broken” nor “all request safety is finished.” [R9]

Do not duplicate the full C4 inventory. Preserve the actual remaining findings. If they affect the new accepted-cast, refusal, ownership, or cinematic path, arrange the narrow necessary integration/reproduction with the file owner rather than waiting for the entire old audit to close.

If a currently reserved file becomes a real dependency, identify the exact file/function and request a bounded handback or ownership decision. Continue independent in-scope work meanwhile. Do not silently cross an active reservation, and do not make that one blocked part an excuse to stop every visual improvement.

Treat current source as the protocol truth. Old notes mention several different protocol versions; do not use a historical literal from this review as an instruction to downgrade or bump it. Validate compatibility when your actual changes require it.

### Change how the queue works, not just its wording

Update the existing **CURRENT IMPLEMENTATION QUEUE** in `docs/TODO.md` and the newest `docs/ACTIVE_REWORK_LEDGER.md` pointer so the next session sees this direction immediately.

Apply the compact-documentation workflow above: make `EXECUTION_PLAN.md`, `GAMEPLAY_RESUME_AFTER_UI.md`, and other active entry points link to the canonical queue and ledger instead of duplicating them. Put clear deferral notes on map/expansion work. Archive or label superseded scheduling without losing task IDs, constraints, evidence, or working links. Clean active routing now; maintain topic details as they change rather than starting an exhaustive archive rewrite.

Use a small practical distinction:

- **Active:** a concrete next implementation or needed dependency for this pass.
- **Deferred:** worthwhile later, with a reason and a sensible revisit condition.
- **Retired/superseded:** the old instruction no longer needs execution; keep the evidence.
- **Historical unresolved:** evidence is incomplete; not silently declared fixed.
- **Owned/blocked/review pending:** the exact external dependency is named.

These are working labels, not a new management framework. Reuse equivalent existing statuses.

Merge duplicate entries for the same outcome. Movement/feet/FPP, can impact, audio, and retrieval presentation should feed the same complete-exchange work, not become competing implementations in five documents. The improvement plan already calls for a complete representative Eskinita sequence; build on that rather than inventing another parallel initiative. [R10]

Do not pick tasks because they are easiest to mark green. Pick them because they remove a real blocker, unlock the next coherent feature, or produce a meaningful improvement a player can experience.

### A practical stop rule for unproductive investigations

Before extending an investigation, be able to state the current symptom or risk, the evidence behind it, and the next experiment's discriminating question.

If there is no current reproduction or concrete supporting trace after a reasonable focused attempt, preserve the limitation and park it with a reopen condition. Do not cycle through seeds or rewrite code until an old number happens to change. A concretely established serious defect is still worth fixing even if reproducing it is difficult.

A measurement that says “leave this code alone” is a legitimate completed result. A test harness failure is not automatically a production defect. Correct a misleading test only after establishing the real expected behavior; do not weaken assertions simply to obtain green results.

Select related checks using the risk-based workflow above; combine them into coherent runs and reuse still-valid evidence. Larger integrated checks belong at meaningful milestones or cross-cutting changes. Preserve failures and scope limits. Do not rerun completed suites after unrelated art adjustments, and do not use “visible progress” to skip a critical ownership, scoring, timing, or network dependency.

### Show progress that I can actually judge

Each substantial checkpoint should answer:

**What can I now see, feel, or do differently in the real game?**

Use representative normal-speed before/after footage or captures, and a current runnable build when the environment supports it. Include the actual gameplay route, not only a showcase scene. A tag cinematic should demonstrate the taya continuing to act; an ultimate should demonstrate the return into actual play and its consequence.

Keep a concise record of the current feature, real changes, validation, significant creative decisions, deferred distractions, and the next implementation step. Group detailed receipts in existing reports rather than repeatedly appending many competing “LATEST” paragraphs.

Do not require a full release build for every micro-adjustment. Use the current guarded development workflow for iteration, then deliver coherent native checkpoints. Do not claim a feature is shipped because its isolated preview looks good, or that the whole pass is complete because one hero works.

The initial cleanup should be brief. Once the queue is aligned, **continue the implementation, not another round of planning**. Finish the current presentation pass across the existing game. Do not automatically resume maps/new characters merely because an old file says to complete everything; leave them as a separate later phase until this work is demonstrably integrated and the priority is deliberately revisited.

> **Make the repeated gameplay and signature moments visibly better before spending more time on stale investigations, exhaustive secondary coverage, new maps, or new characters. Integrate and improve useful friend-work as you go.**

---

### Review references — evidence, not another mandatory reading marathon

These are the documents actually reviewed for the scheduling decisions above, pinned to `a6f8b275`. They establish recorded state and scope, not a new hands-on Unity playtest. Read newer versions and only the relevant sections as needed. No repository files were changed by the external reviewer.

[R1]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/commit/a6f8b275d0083ca2066793e447dca3a92b07c29d
[R2]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/blob/a6f8b275d0083ca2066793e447dca3a92b07c29d/docs/ACTIVE_REWORK_LEDGER.md
[R3]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/blob/a6f8b275d0083ca2066793e447dca3a92b07c29d/docs/GAMEPLAY_RESUME_AFTER_UI.md
[R4]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/blob/a6f8b275d0083ca2066793e447dca3a92b07c29d/docs/CLAUDE_ENGINEERING_LANE.md
[R5]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/blob/a6f8b275d0083ca2066793e447dca3a92b07c29d/docs/TODO.md#current-implementation-queue
[R6]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/blob/a6f8b275d0083ca2066793e447dca3a92b07c29d/docs/TODO.md#earlier-execution-index-and-supporting-reasoning
[R7]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/blob/a6f8b275d0083ca2066793e447dca3a92b07c29d/docs/MAP_FINAL_PASS.md
[R8]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/blob/a6f8b275d0083ca2066793e447dca3a92b07c29d/docs/reports/spectator-review-2026-09-16/README.md
[R9]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/blob/a6f8b275d0083ca2066793e447dca3a92b07c29d/docs/reports/claude-request-safety-2026-09-15/README.md
[R10]: https://github.com/DOST-GameDEV/TumbangPreso-Unity/blob/a6f8b275d0083ca2066793e447dca3a92b07c29d/docs/IMPROVEMENT_PLAN.md

---

## The actual goal

We are preparing TUMP for a national game-development competition. A professional developer who judged the previous competition told us that improving the actual gameplay experience and making it exciting and understandable to watch would be especially valuable.

I want someone unfamiliar with the game to understand:

- What matters, who did something, and what changed.
- Who is in danger and what they need to do next.
- Why a particular play deserves a reaction.

The intended reaction is not just “the effects look nice.” It is:

> “GET OUT, GET OUT!”
>
> “He almost caught them.”
>
> “That actually hit?”
>
> “No way they escaped that.”

**Do not make TUMP mechanically busier. Make the game treat its existing actions like they matter.**

The central exchange is:

**Throw → knock down lata → slipper lands → retrieve → taya responds → escape or get caught.**

The can knockdown often starts the interesting situation rather than ending it. Present the entire exchange, not a collection of isolated impact effects.

Hero Strike adds powers to that exchange. It must not bury the can, slipper, retrieval, and chase beneath those powers. Classic is equally important and must receive excellent shared game feel, not become the neglected mode without cinematics.

### The creative baseline to carry forward and improve

| Area | Intended experience / starting direction |
|---|---|
| Physical actions | Much stronger coordinated animation, contact, sound, camera, VFX, and aftermath. |
| Retrieval | Make the actual change in vulnerability and the eventual escape intuitive and tense. |
| Tags | A victim-specific third-person catch cinematic inside existing recovery; satisfying taya confirmation without extra lockout. |
| Ultimates | Real hero-specific character moments, initially around **2–3.5 seconds**, followed by readable live execution and an appropriate payoff. Not a half-second substitute. |
| Slippers | **Own-slipper-only retrieval/use**, with legitimate existing hero behavior preserved. |
| Item readability | Start with the requested **low, soft, short vertical slipper beacon/locator**. Improve its form in-engine; the slipper must be easy to find without a towering beam or visual clutter. |
| Objective readability | A small floating lata icon/arrow that stays findable and communicates state. |
| Identity | Persistent player color, number, and a compact symbol across the world, HUD, and broadcast. |
| Event reporting | A subtle small side feed, separate from an important-moment announcement beneath the timer. |
| Chains | Meaningful per-player streaks, escalating recognition, and **small capped score bonuses**. No permanent hype meter. |
| Spectator | Better framing of the developing play, not constant cuts or identical player-camera treatment. |
| Replay and pacing | Best-play halftime replay after round four, shorter ordinary transitions, and a credible all-player playback path. |

Use these as the baseline for a complete implementation, with freedom to improve the realization. A better locator, more expressive tag shot, stronger ultimate structure, or cleaner underlying architecture is welcome. Carry the function and ambition forward, record significant changes briefly, and evaluate them in the game instead of treating this table as a literal recipe. Keep modest capped chain bonuses in the finished scope rather than indefinitely postponing them.

### What we do not want

No new hype currency, permanent excitement bar, arbitrary combat system, extra dodge button, obligatory timing minigame, constant giant text, or persistent effects covering the arena. Do not give streaks speed, damage, or other power buffs.

Own-only retrieval and modest chain bonuses are specifically approved rule changes. Shared cinematic and halftime phases are also authorized, with their timing implications handled deliberately. Make necessary integration/correctness fixes and tune the proposed presentation and bonus values yourself. Flag genuinely different core mechanics or major balance redesigns separately rather than slipping them into this pass.

---

## Inspect the current game and establish the truth

At initial intake, read the current repository entry instructions, the active ledger/queue, and the relevant `docs/VISION.md`, design/ability, and implementation sections. After compaction, use the compact resume path and inspect changed assumptions instead of repeating the entire intake. Record the starting commit and working-tree state; preserve uncommitted work and use the latest `ASTRAReworks` plus identified friend-work. Check whether relevant friend changes are already integrated before implementing them again. Recent explicit owner decisions and verified current behavior matter more than stale prose. Investigate unexplained drift rather than automatically preferring either old or new behavior.

Previous reviews inspected commit:

```text
a6f8b275d0083ca2066793e447dca3a92b07c29d
```

**The following are prior source-review findings, not a promise that the latest branch remains identical. Recheck them as you touch the relevant systems; they are navigation and risk notes, not a requirement to complete an exhaustive fresh audit before the first implementation.**

- Both default modes used eight rounds, with 90-second rounds and a five-second between-round warmup buffer. Round four completed one taya rotation. Do not mistake an old three-second constant or historical fifteen-second buffer for the active transition path.
- An empty-handed attacker inside the box could not be tagged. Carrying a slipper inside the box was a key eligibility condition, alongside the actual role, round, and immunity checks. Tag resolution additionally required the lata to be upright.
- Being stunned did not automatically make an otherwise eligible attacker immune. Read the real predicates; do not invent simplified eligibility rules.
- Restoration protection lasted 1.25 seconds. It protected the **can from another knockdown**, not an armed attacker from being tagged. A can that is upright but protected can still make the return journey dangerous.
- A normal tag applied a five-second stagger and teleported the victim to safety. A cinematic must fit inside actual remaining recovery, not add a second punishment.
- Slipper pickup had a shared eligibility helper but did not require matching ownership. `OwnerSlot` and stable `SeatOfOrigin` served different purposes.
- Zack's Magnet intentionally recalled his own loose slipper. Practice also had intentionally ownerless spare slippers, which strict ownership would require handling explicitly.
- `MatchFlair`, `HitFeel`, `Hitstop`, camera/viewmodel systems, objective-state visuals, score events, audio, and a spectator director already existed. There was substantial presentation to improve, not an empty project.
- An empty `MatchFlair.LataDown` presentation branch did not mean can-hit effects were absent. Other feedback originated in the lata state-change path.
- The can-hit path already requested several things, including micro-hitstop, a caption, hitmarker, burst, confetti, voice, and camera response. More layers are not automatically the answer.
- `Hitstop` changed global time scale for short bounded intervals. `CameraRig.HoldFrame` held camera-follow behavior while the world continued, rather than freezing a captured image. These are different mechanisms.
- The first-person aim ray used the camera-rig transform. Verify how cosmetic camera changes interact with aim before intensifying them.
- The spectator director already had story/interest logic and several shot types. Improve and test it rather than presenting “add a director” as a discovery.
- The inspected replay path lived in `SpectatorCamera`: 100 captured frames, sampled at ten frames per second, at 640 × 360. That established recent-view replay, not arbitrary-angle reconstruction or all-player halftime delivery.
- Retrieval highlight recording was host-side while other markers rode replicated flair. Independently selecting the same highlight on every peer was therefore not established.
- Knockdown highlight distance used the thrower's position at impact, not the actual release position. Precise distance awards need the real throw origin.

Useful implementation navigation hints include `MatchDirector`, `RoundDirector`, `Slipper`, `Lata`, `Carrier`, `CombatVerbs`, `MatchFlair`, `HitFeel`, `Hitstop`, `CameraRig`, `ViewmodelArms`, `SpectatorCamera`, `SpectatorDirector`, `SpectatorInterest`, `MatchHighlights`, `Balance`, `MatchRules`, and the six hero kits. Find the current paths rather than assuming every file or function is unchanged.

Distinguish code-established behavior, historical comments, directly observed presentation, measured performance, and proposed design. Reading code does not prove something currently feels mistimed. Do not claim a successful build or hands-on playtest without doing it.

Check which engineering reservations are still active, and preserve other contributors' work and genuinely approved assets. Historical task notes are context, not permission to resume unrelated work. Necessary in-scope animation, VFX, audio, camera, and integration changes are authorized, including substantial refinement or replacement of the friend's identified placeholders under the friend-work instructions; unrelated finalized menu or broad roster redesign is not the task.

---

## Use focused research to improve what you are building

**SEPAK-U is the primary emotional reference.** I want comparable or better coordinated impact and watchability, translated into TUMP's 3D and often first-person format. This is a target to evaluate, not a claim you can certify from a feature checklist.

Study actual sequences where accessible and useful to the next implementation decision. Examine anticipation, contact, aftermath, camera, sound, character reaction, UI, control implications, and what remains readable, then apply the insight to a playable prototype. Do not simply list games or make research completion a gate that delays all implementation.

The lesson is:

> Animation + camera + VFX + SFX + UI + timing + reaction should support one recognizable event.

SEPAK-U's 2D composition is not permission to cover the center of a first-person player's escape route with an equivalent-sized graphic.

Useful secondary references include Blue Lock Rivals, Genshin Impact, fighting games, Rocket League and sports broadcasts, League of Legends, Mobile Legends, VALORANT, Overwatch, Fortnite, and PUBG. Use the strongest examples, not all of them for the sake of name-dropping.

Separate a character cut-in, local camera takeover, animation lock, impact hitstop, slow motion, and a genuine multiplayer-wide pause. Do not assert that different reference games treat these identically.

Use primary material where possible: developer gameplay, official pages, talks, technical documentation, and design breakdowns. The prior review did not establish a frame-by-frame analysis of SEPAK-U or exact Genshin/Blue Lock timings. Do not inherit those as verified observations. If footage cannot be inspected, state the limitation and keep the proposed choreography clearly labeled as your design.

---

## One presentation language, different experiences for different viewers

### Importance, relevance, and available attention

An event earns presentation intensity through its importance, but its treatment also depends on who is watching and whether that viewer can afford an interruption.

Walking and routine movement establish a quiet, convincing baseline. Throws get clear action feedback. Blocks and near misses get distinct physical feedback. Can knockdowns and tags get major contact treatment. Dangerous retrieval creates tension. Exceptional chains receive broadcast recognition. Ultimates get proper character performances.

Do not make every event equally loud. Do not mistake restraint for removing the exciting part.

### Viewer-specific direction

| Event | Actor | Recipient or threatened player | Other active players | Spectator |
|---|---|---|---|---|
| Throw / can hit | Responsive release, contact confirmation, credited score, then attention toward the slipper. | Taya reads the objective change and can respond immediately. | Can state, sound, ownership, restrained feed. | Contact followed by the retrieval it creates. |
| Restoration | Satisfying completion with immediate freedom to chase. | Actually catchable attackers recognize danger returning. | The same truthful can/protection state. | Keep can, taya, and exposed attacker connected. |
| Pickup / escape | Possession confirmation, appropriate tension, then relief. | Taya recognizes the actual legal target and its route. | Relevant possession and ownership without clutter. | Retriever, pursuer, and safety boundary remain legible. |
| Block / whiff | Specific result and direction, not generic failure. | Blocker feels contact; threatened attacker perceives a real near miss. | Mostly world motion and sound. | Show deflection or missed reach and its consequence. |
| Tag | Crisp taya confirmation without extra recovery or forced celebration. | Short reconstructed third-person catch inside actual recovery. | No camera takeover; concise attribution. | Show the catch, but do not abandon a more urgent developing chase. |
| Ultimate | Full hero-specific performance on a committed, accepted cast. | Intact threat information and live counterplay. | Clear caster identity and shared timing. | Hero composition that returns to the actual battlefield outcome. |
| Chain | Escalating recognition and capped points. | No fabricated additional hit or punishment. | Central announcement when it will not hide an immediate threat. | Player identity and readable achievement, then continue the play. |

All views agree on the gameplay result. Different shots are not permission for different pause durations, earlier control, private-information leaks, or fabricated outcomes.

### Build authored moments, not unrelated callbacks

An accepted event should provide enough common identity, timing, actor, subject, position, direction, and outcome information for animation, sound, VFX, score acknowledgment, and replay marking to describe the same event.

This does not require a giant new “Moment Engine.” Extend existing architecture where it is sound; refactor or replace a weak piece when that produces a cleaner result. Viewer-aware profiles and a small coordinator are suggestions, not required classes. Choose the structure that serves the actual game, with one clear focal point and complementary supporting effects.

Network arrival time is not automatically the right visual contact time. Align presentation with the event and the visible/interpolated state. Preserve immediate local preparation and release feedback, but do not issue a definitive success celebration for a result the authority has not accepted.

---

## Author a complete ordinary exchange

Use a coherent sequence as the quality standard, not one effect in an empty arena.

A useful test story is P1 as taya and P2 as the attacker: P2 knocks the lata down, approaches their slipper, picks it up, P1 restores the can, P1 commits to a catch, and P2 either escapes or is tagged. P3 and P4 remain active so this is not approved only in an empty fixture.

The critical information is:

> P2 now holds the slipper. The lata has gone upright. P1 can catch them even during the can's restoration protection. P2 needs to reach safety.

Stage controlled fixtures to inspect presentation, but do not force ordinary live outcomes to follow the script or represent a staged test as an unscripted match.

### Throw preparation and release

Improve the grip, anticipation, wrist/body motion, release sound, follow-through, and return to running. First-person and third-person actions must describe the same release.

Fit animation to the existing accepted action. Do not add latency or another charge/timing mechanic merely to make a pose longer.

Preserve the deliberate sight-line-based projectile launch and the agreement between aim preview and actual flight. Solve the visual hand-to-flight connection without quietly moving the authoritative origin, changing the trajectory, or making the slipper seem to teleport through a wall.

Use a short directional trail. The slipper remains the recognizable object; its full history does not need to remain painted over the arena.

### Can contact: rubber striking tin

The signature is a slipper striking a can, not a grenade detonating.

At confirmed contact, align the visible collision, sharp metallic transient, initial can reaction, and one compact directional impact shape. Give the thrower a satisfying response through hand follow-through, sound, and a restrained additive camera treatment, not the same hurt reaction as a victim.

A starting composition is a roughly **40–70 ms contact emphasis**, followed by about **150–350 ms of can motion and metallic aftermath**. These are tunable presentation tests, not mandatory gameplay freezes. Audit the existing micro-hitstop and avoid stacking independent global pauses or making every unrelated player stutter.

Let the can topple with convincing weight. A little stylized deformation, rotation, or compression can help if it suits the existing model. The slipper's continuation and eventual location remain truthful.

Use a camera-facing contact accent where useful, with fragments or streaks aligned to the impact direction. Clear them quickly enough to reveal the next decision. Do not enlarge the effect until the can is hidden.

Reserve routine confetti and large celebration graphics for something actually exceptional. Keep an ordinary hit satisfying through physical quality, not repeated congratulation.

Distinguish **successful knockdown, protected-can contact, body block, and genuine miss** in motion, audio, and confirmation. The player should not have to inspect their score to learn whether the hit worked.

### Flight-to-retrieval handoff

As the slipper lands, the trail resolves into a readable settling motion and contact sound. When it becomes loose, its low ownership locator takes over. This transition should naturally redirect attention from “watch my throw” to “there is what I need back.”

Do not leave the knockdown celebration covering the location the player now needs.

### Restoration as a reversal

Give the taya a purposeful preparation and finishing motion, then a decisive upright snap, distinctive tin sound, and consistent marker transition. The completion cue should remain recognizable to an attacker looking toward the exit.

Do not impose a celebration, animation root, or camera pause before the taya can chase. Communicate protected can separately from catchable attacker. Never use a generic shield effect that implies the whole encounter is safe.

### Retrieval, threat, and relief

Before pickup, communicate anticipation and the object to retrieve, not a false “you can be tagged” warning. Other hero threats still matter, but the catch warning must follow actual eligibility.

At accepted pickup, align reach/contact, possession sound, the slipper appearing held, and the locator disappearing. Preserve running and slide movement. Do not declare success before accepted possession or root the character to finish a reach.

After pickup, tension follows actual catchability, can state, and legitimate perceived threats. Preserve relevant footsteps, restoration cues, and approach sounds. Avoid a continuous red screen or an omniscient heartbeat/radar that reveals hidden players.

At safety, let the local danger accent and audio tension release. A small sense of relief belongs to ordinary successful escape; a major announcement requires a genuinely exceptional danger episode.

For a close-call award, use legal recent attack attempts, closest approach or equivalent real threat information, catchability, and a successful exit. Mere distance to an inactive taya, a downed can, or a threat blocked by geometry is insufficient. Trigger at most once per retrieval episode and prevent boundary-crossing farming.

---

## Ownership, the low slipper beacon, and the lata marker

### Own-only slippers are an actual rule

Implement ownership enforcement at the appropriate authoritative boundary in the current game, then cover normal pickup, slide pickup, AI choices, remote requests, host validation, throws, force equip, round reset, reconnect, disarm/recovery, practice, and tutorial paths. Reuse a shared eligibility helper where it is the correct boundary, rather than forcing the old file layout.

Do not only hide the wrong pickup prompt. A client request must not be able to bypass the rule.

Keep stable object identity separate from mutable ownership and holder state. Preserve one consistent possession relationship across the slipper, carrier, motor, viewmodel, and network replicas.

Preserve legitimate existing abilities such as Zack's own-slipper Magnet. Observe the balance consequences of removing other players' ability to deny him by taking his slipper. This is not permission to remove or redesign his kit automatically.

Resolve ownerless practice items deliberately by assigning intended training equipment or supplying a clear practice reset. Do not weaken live-match ownership globally just to keep an old practice fixture working.

### Start with the requested low vertical locator, then tune it in-engine

The primary candidate is a **short, soft, owner-colored vertical beacon above a loose slipper**, optionally supported by a modest ground accent and silhouette treatment.

Start around one or two slipper lengths in visible height, or roughly 0.3–0.5 world meters only if that fits the actual asset scale. Tune in-engine. It must not resemble a giant legendary-loot beam or a bloom-covered magic pillar.

The locator:

- Appears only when the slipper is relevant and loose; disappears on holding, flight, parking, or removal.
- Is more prominent for its owner locally, without changing the global ownership color.
- Remains readable at useful distances and in compressed footage.
- Becomes quieter nearby so it does not obscure the physical pickup target.
- Works without bloom, respects depth/occlusion intentionally, and does not float through ceilings or unrelated geometry.

A restrained ownership symbol or off-screen direction can support it when justified. Do not introduce permanent screen-edge clutter.

Start with the low beacon because that is the direction I liked. You may reshape it, combine it with a symbol or outline, or replace the rendering technique when the result better satisfies that intent. Use actual gameplay-distance views to choose. Explain a substantial change briefly, but do not ask permission for each visual iteration. The goal is excellent slipper location and ownership readability, not loyalty to a particular shader or mesh recipe.

### One player identity language

Give P1–P4 stable player color + number + compact symbol for the match. Reuse it across nameplate accents, slipper locator, score rail, feed, milestones, replay, and spectator overlays.

The taya role is a separate badge. The player does not change identity when roles rotate, and the temporary attackers are not automatically a permanent team.

Elemental effects retain their identity. Fire, ice, lightning, earth, ghosts, and witchcraft should remain recognizable. Use source badges or restrained ownership accents where needed, not wholesale recoloring that makes P2's fire look like ice.

There is one shared active lata. Its durable ownership accent can reflect the current taya's selected can/role; a knockdown can briefly attribute the scorer through impact/UI without suggesting permanent ownership changed. Preserve approved skins.

### One state-aware lata marker

Use a small floating can icon/chevron/arrow, above rather than directly on the aiming point. Keep readable minimum and maximum screen sizes.

Unify upright, down, restoring, and protected states. The common marker explains object state; the local throw affordance explains whether this particular player can legally release now.

Do not show “hit this now” while the action is unavailable. Equally, do not imply an armed attacker cannot be tagged simply because the restored can is protected.

Inspect and reconcile existing collar, rim, shell, objective card, crosshair, captions, and other messages. The new marker should replace conflicting information rather than become the eighth copy of the same state.

## Make getting tagged a signature cinematic without stopping the taya

This is a central creative feature, not a minor optional camera shake.

### Victim: show the actual catch

Start by testing approximately **1.0–1.4 seconds** of victim-specific presentation for a normal tag, but choose a shorter, longer, or context-sensitive treatment when the actual shot works better. Duration serves readability, repeated-use comfort, and the real remaining recovery deadline. Use the numbers as prototype guidance, not permission gates or fixed acceptance targets.

A concrete starting sequence:

1. **Contact, about 0–70 ms:** a compact impact beat and unmistakable catch transient. If a true frozen image is needed, use an actual presentation hold rather than assuming a camera-follow hold freezes the image.
2. **Reveal, about 70–220 ms:** cut or make a quick controlled transition to a three-quarter shot containing the taya's reaching hand and the victim's body. Choose the side from the real approach/contact geometry. Do not default to a full orbit.
3. **Reaction, about 220–850 ms:** show the accepted catch pose and a short directional recoil/stumble. The body carries the weight; the VFX support contact instead of hiding it.
4. **Recognition and return, roughly 850–1,200 ms:** one concise identity treatment such as `TAGGED BY P1`, then a clean return to the victim's real safe-zone position and accurate remaining recovery.

These beats are a starting storyboard, not four mandatory delays. Implement the version you believe will work, watch it in motion, and improve or replace the shots and timing. A stronger, simpler composition is better than reproducing the suggested timestamps.

Capture enough information **before the authoritative teleport** to preserve both characters' contact, approach, orientation, pose, and any relevant held object. A position alone may not be enough.

Use isolated non-interactive presentation copies, recorded poses, or a similarly faithful reconstruction. Do not reverse live bodies, postpone the authoritative tag, alter collisions, or extend the penalty to stage the shot. Keep these copies out of live collisions, scoring, normal spectator views, shadows/reflections where inappropriate, and world gameplay cameras.

Do not invent a dramatic extra punch, slam, or tackle that did not happen. The shot should explain the actual catch, not fabricate a finishing move. Retain TUMP's playful physical style.

A late event starts from the remaining valid recovery window, not a fresh full timer. Return before the player can act. Local dismissal or reduced-camera treatment must not shorten the authoritative penalty.

### Taya: confirmation without losing the next catch

Give the taya a crisp contact sound, strong hand/body follow-through, appropriate viewmodel or additive camera response, credited score, and a milestone only when earned.

Do not add a new animation lock, forced pose, camera cut, steering restriction, or recovery. Existing gameplay recovery remains; presentation adds none. Chaining legitimate catches must stay possible.

### Other attackers and spectator

Other active players receive a clear world reaction and feed attribution, not an involuntary camera sequence.

The spectator gets a readable catch and its consequence. If another attacker is now under threat, stay with the developing play rather than finishing a decorative close-up. An alternate-angle catch belongs in replay when it would otherwise hide live action.

Include repeated-use review of the integrated tag treatment, for example ten catches within one focused session/capture rather than ten separate builds. Repeat after a meaningful pacing/camera change or a new concern, not every cosmetic adjustment. Favor directional/contextual variety and a clean signature over increasingly elaborate punishment. Any increased treatment for an exceptional catch still fits its actual recovery window.

### Share the recording foundation

Design and implement victim catch reconstruction and alternate-angle halftime replay with their common history needs in mind. Shared data and non-authoritative playback primitives are the preferred starting point, not an architectural mandate. Reuse the current recorder or choose a better hybrid when justified; avoid accidental duplicate systems while proving real catch playback early.

---

## Ultimate cinematics: real character performances, then actual gameplay

### The creative standard

**An ultimate must be allowed to temporarily own the match.** Do not offer a 0.5-second hitstop as a substitute for the requested character moment.

Prototype around **2–3.5 seconds of authored signature presentation**, adjusted by character, frequency, and actual quality. These are not measured timings from Genshin or any reference. A purposeful shorter sequence may beat a padded longer one, but the answer must still feel like a real performance.

The camera, hero body, viewmodel transition, sound, lighting, moving effect geometry, and graphic composition should form one directed scene. Not one generic camera orbit and a different particle color for each hero.

### Separate cinematic time from playable warning time

Use this structure:

**Committed cast accepted → synchronized cinematic phase → orient players → preserve the actual live preparation/execution → deployment/contact → consequence.**

Do not trigger a full sequence on an invalid press or while Zack is still choosing an uncommitted aim point. Do not guarantee a hit because the character got a cinematic.

Do not spend an existing playable warning interval while everyone is frozen, then resume directly into the hit. That would remove counterplay. Conversely, do not make the cinematic look like a complete cast, reset the character to idle, and then repeat an unrelated second introduction.

**The final cinematic gesture must flow directly into the live ability.** Match pose, sound phrase, direction, and effect continuity across the boundary. The player should perceive one performance whose final portion is playable, not a movie followed by a second loading sequence.

Phaister is the key test. The prior review found an approximately 1.55-second live ritual build with an escape purpose. Reverify its current timing. Her theatrical invocation must lead into that full playable warning rather than replace it. During the frozen prelude, do not visually label a harmful field as already active when it is not.

Use roughly the final **250–350 ms** of the cinematic to restore normal battlefield orientation while gameplay is still paused. Return to the saved underlying aim and real location. A new camera view should not appear on the same instant an avoidable attack has already resolved.

### Concrete hero direction to improve, not merely paraphrase

Inspect the current rig, silhouette, kit, approved animations, and first-person assets before authoring. The following are staging proposals based on the earlier reviewed kits. Change them when the real assets suggest a stronger, more native performance.

| Hero / prior reviewed ultimate | Proposed authored character moment | Transition and actual payoff |
|---|---|---|
| **Sean — Supernova** | Begin close and low enough to read his planted stance. Compress the body and hands into a short loaded pose; use inward-moving ember accents and a tightening sound instead of an expanding explosion. Rise with the upper body to reveal a powerful launch silhouette. | Match into his actual leap. Do not perform a fake complete leap in the frozen scene and then repeat it live. The real descent, landing, displaced opponents, and burning aftermath deliver the main impact. |
| **Zack — Thunderstrike** | On the accepted aimed release, use an angular medium shot: a small controlled electrical buildup, an identifiable hand/arm gesture, then a decisive directional release. Contrast fine electrical motion with a briefly still body. | Re-establish the public committed strike area before action resumes. The sky-to-ground strike gets the contact beat. His subsequent charged throws stay visibly distinct; an initial miss does not mean the entire ultimate has no value. Never reveal private pre-commit aim. |
| **Dante — Titan Fissure** | Frame the stance, torso, and ground together. Show weight loading through the body into a planted forward action. A low, tightening ground response leads the eye toward the direction of force instead of surrounding him with a generic ring. | Match into the actual ground strike and forward fissure. Let the crack's travel and affected players explain the outcome. Preserve the real collision/area rules; do not draw a deceptive shape that excludes actual reach. |
| **Cheska — Glacial Nova** | Use controlled stillness and a readable hand/body silhouette. Fine ice formation follows one deliberate gesture; precise crystalline sounds build rather than a broad roar. One crisp release breaks that stillness. | Transition into the outward nova. Show who is actually frozen and the truthful motion of displaced loose/in-flight slippers. Do not fill the arena with white or attach a successful-freeze celebration to unaffected players. |
| **Nemu — Kuro Unbound** | Keep Nemu and Kuro in a shared composition. Establish the small familiar, use an unsettling held moment and inward-moving motifs, then reveal the existing familiar transforming into the devouring form. The relationship and transformation are the signature. | The transformation connects to the actual anchor and live pull. Give the sustained struggle readable inward motion and escape direction. Do not turn it into a generic explosion or replace the approved cute blocky ghost design with realistic horror. |
| **Phaister — Grand Coven** | Use a clear witch silhouette, a deliberate ritual gesture, and an eclipse motif that grows out of the gesture. Prefer one readable arc or compositional movement to many unrelated magical overlays. | The final gesture becomes the beginning of the full live ritual build without an idle reset. Preserve the escape opportunity. Formation, completed boundary, repeated curse applications, and leaving the circle are separate truthful states. |

For each hero, briefly settle the shot/beat direction and then actually author, integrate, run, and refine the body animation, framing, sound, VFX, live transition, and failure/deployment behavior. Change the proposed choreography when the current rig, kit, or your own creative judgment suggests something stronger. A description such as “plant his weight” is not a completed implementation.

Do not invent new anatomy, props, facial detail, or powers that violate the approved assets. Good animation comes from weight, posing, contact, timing, recovery, and personality within the blocky style.

### Different shots, common match phase

The caster can receive the fullest hero shot. Other players can see their frozen battlefield with a strong character cut-in, public cast identity, and an appropriate warning context. The spectator can receive a wider hero-and-arena composition.

All share the same gameplay pause and resume point. Reduced motion changes the presentation, not the ability timing. It cannot give earlier movement or extra aiming time unavailable to everyone else.

If a target or threat was not public information, do not reveal it through another viewer's cinematic. Do not leak unseen players via a cinematic camera move.

### Activation is not the entire payoff

Distinguish **activation, deployment, contact, and consequence**.

A slam earns a discrete heavy impact. A nova earns an outward release and truthful reactions. A pull or curse field earns a strong deployment and developing pressure, not an invented finishing blow before it affects anyone.

Use stronger contact emphasis, physical reactions, audio weight, and selective screen treatment for a genuine ultimate impact. Let affected characters, displacement, the can, and the escape routes remain visible.

If it misses, show the actual miss. A narrow escape may become the highlight. If a field shapes the round without directly hitting somebody, show its actual function rather than falsely celebrating or falsely calling it useless.

---

## Shared time, simultaneous casts, and consecutive cinematics

A multi-second synchronized cinematic is a deliberate gameplay-affecting presentation change. Do not implement it by extending the current micro-hitstop timer and hoping the other systems follow.

Separate simulation time, cinematic presentation time, and replay time. The host owns accepted casts and the phase. Define how movement, projectiles, slipper flight, physics, scoring, round time, cooldowns, statuses, existing ability phases, and anti-stall clocks stop and resume together. UI, necessary networking, and presentation continue appropriately.

Use a shared phase identity and authoritative start/end information. A late client joins the correct point or falls back safely; it must not begin an independent full-length pause on receipt.

Audit all writes to time scale and camera ownership, including pause menus and existing cinematic/presentation systems. Restore the correct previous state, not blindly `1.0` or an assumed normal camera after an interruption. Provide bounded timeout and teardown behavior for round end, match end, disconnect, scene transition, and failed presentation.

### Simultaneous accepted casts

Casts legitimately accepted at the same authoritative boundary should share one bounded cinematic interval and fixed end, with multiple identities or a combined composition as appropriate. Preserve actual accepted effects, costs, and resolution ordering. Do not silently cancel the second ability because it lost camera priority.

Do not create a playlist of four full cinematic phases. Do not indefinitely extend a phase whenever a late request arrives. Define whether input during the paused phase is ignored, explicitly refused without cost, or handled by an existing safe buffer; it must not create surprise actions on resume.

### Back-to-back casts after resumption

This is a separate problem from simultaneous casts. One player may legitimately ult immediately after another scene ends.

The baseline must preserve valid casts without an invented gameplay cooldown, silent delay, or cancellation. Test a full rapid-succession sequence deliberately. Do not assume the simultaneous grouping policy solves it.

If full consecutive scenes become tiring, choose, implement, and test a **presentation-only** repeat policy: for example, a connected ensemble treatment, a full-duration in-world signature with less camera takeover, or a better solution you develop. Briefly record the tradeoff rather than leaving this as an unresolved design question. Preserve the feeling of a real character moment and never hide a punishable player's live gameplay behind an exclusive camera cut.

There is a real tradeoff between a full separate global movie for every cast and avoiding repeated global interruptions. State the chosen policy and show it in a stress test rather than pretending architecture eliminates that design choice.

## Two separate HUD channels, with deliberate priority

### Small side feed: what just happened?

Keep the subtle side-of-screen feed. Use a few recent entries, recognizable icons, consistent player identity, and very short wording. Examples:

```text
P2 [can] DOWNED LATA
P1 [restore] RESTORED LATA
P1 [catch] CAUGHT P3
P1 [block] BLOCKED P2
```

A starting budget is around three entries with brief, readable lifetimes. Tune to actual event frequency. Do not queue stale entries indefinitely.

Normal pickups can remain local; publicly report retrieval only when it improves understanding. Do not include every release, footstep, defense tick, status pulse, or idle penalty.

This is a screen-space reporting system, not more words floating over the can and players.

### Central announcement: why was that special?

Use a separate treatment beneath the timer or another safe central HUD location. It should feel like a deserved League/Mobile Legends-style milestone without copying unrelated kill terminology.

Working labels include:

```text
P2 · 3-HIT STREAK
P1 · TRIPLE CATCH
P3 · CLOSE CALL
```

Test TUMP-specific alternatives such as `THREE STRAIGHT` or `SWEEP` for clarity and natural tone. Do not force Tagalog branding, generic “epic” slogans, or a literal pentakill in a game with three opposing attackers.

Escalate through rhythmic UI entrance, player identity, a short sound motif, and selective voice. Higher milestones can have richer motion and sonic resolution, not merely more volume.

Do not place the announcement over the crosshair, interaction target, or immediate escape information. One central announcement is active at a time. A higher milestone upgrades/replaces the lower one rather than waiting behind stale celebrations. A current local threat, recovery instruction, or required cast information outranks a celebration.

The arena explains contact. The HUD explains significance. Remove redundant old world words where the new presentation already does the job. Do not place “CAN DOWN,” a score toast, a central alert, a giant world caption, and a second celebratory graphic over the same ordinary hit.

No announcement adds animation lockout or changes control.

---

## Chains and capped bonus points remain in scope

Do not replace this with “recognition now, points maybe someday.” You may implement/test the detector before wiring rewards, but **modest capped bonus points are part of the agreed finished direction**.

### Attacker streak

The following definitions are a coherent starting model. Refine eligibility, episode boundaries, and timings against the actual game while retaining understandable skill recognition, fairness, and modest capped rewards. Keep the finalized rules consistent between gameplay, UI, tests, and documentation.

Track the player's real throw attempts and outcomes, not a shared cosmetic highlight counter.

- A credited legal can knockdown advances that player's streak once.
- A genuine miss or successful defensive block breaks it.
- Another player scoring between your own attempts does not erase your sequence.
- A throw already in flight whose can cycle was consumed by another player's knockdown can be neutral/no-contest: no advance or bonus, but not a fabricated accuracy failure.
- A tag and new round reset the sequence. Invalid input or refused release is not a launched miss.
- One throw and one upright-to-down cycle cannot award multiple advances through duplicate collision or network messages.

Do not use a short countdown that pressures attackers to skip the retrieval journey. For exact long-shot or bank awards, keep the real release origin and accepted path/contact history. Distinguish direct throw success from an ultimate or area effect knocking the can over; preserve ordinary base scoring and define which event actually qualifies for the named accuracy streak.

### Taya chain

Recognize catching different attackers within a short active-play window, initially about **eight seconds between qualifying catches**. Tune against actual movement and recovery.

A repeated catch of the same victim does not advance or refresh that distinct-victim chain. A can knockdown need not erase an otherwise valid sequence; a restore followed by another catch can be an excellent play.

Use active gameplay time so a global cinematic neither consumes nor freely extends the opportunity incorrectly. Reset appropriately at round boundaries. A triple catch is the natural major milestone with three attackers.

### Proposed starting rewards

The reviewed base can-hit and tag values were 100 points. Reverify them before tuning these additive bonuses.

| Sequence | Prototype bonus |
|---|---:|
| First qualifying action | +0 |
| Second consecutive can hit / second distinct catch | +10 |
| Third consecutive can hit | +20 |
| Fourth and later consecutive can hit | +25 maximum per qualifying hit |
| Third distinct catch | +25 |

These are starting numbers, not an untouchable balance decision. Keep the cap modest and test total scoring influence across real matches.

Do not add infinite multipliers, speed/damage buffs, extra resources, or multiplied ultimate charge. A base action earns its normal ultimate progress once; the chain bonus is separate score, not another fake can-hit/tag event.

Authoritative scoring remains in the existing host-side scoring authority. Keep base points, chain bonus, reason, and total consistent for clients, score displays, reconnects, and final standings. Avoid applying a bonus locally and then adding it again from a replicated total.

Recognition, gameplay score, ultimate charge, and replay ranking are four distinct consumers. Do not let a replay selector award points or a cosmetic log become a game-rule authority. Boundary-crossing close calls and ordinary near misses do not automatically earn score bonuses.

---

## Sound, cameras, motion, and physical continuity

### Audio is a major creative deliverable

Build distinctive material-based signatures for slipper release, landing, pickup, rubber/body block, tin contact/topple/roll, restoration, lunge, close fly-by, tag, escape, chain milestones, and each hero's activation and consequence.

Separate three layers:

**World sound:** the actual event at its physical source.

**Personal feedback:** concise confirmation or reaction for the relevant listener.

**Broadcast recognition:** sparse announcer/milestone treatment.

They should cooperate, not make one contact sound like three different hits. Avoid relaying someone else's personal announcer as a positional world sound.

Compose impact attack, body, and tail. Use frequency separation, believable material, restrained variation, and deliberate silence/ducking rather than simply turning everything up. A can hit needs tin identity; a block must not sound like a successful can score.

During dangerous retrieval, prioritize restoration, approach, and footstep information above decorative music and voices. A short reduction of noncritical ambience before a huge impact can be effective, but do not duck away an actionable threat.

Provide escalating chain motifs and bespoke ultimate phrases. Do not speak over every successful action or let old voice lines queue past relevance. Audio variation must not perturb gameplay random state.

Do not invent loud crowds throughout the match solely to tell players that something is exciting. Earn the response through the action first; crowd/broadcast flourishes, if used, belong to genuinely exceptional moments.

### Give each camera response a physical reason

A small directed punch, recoil, vertical landing weight, FOV movement, pose hold, controlled cut, orbit, or no motion can all be appropriate. Do not reuse generic shake for every event.

Preserve underlying gameplay aim and input. Inspect the actual rig/aim dependency before adding cosmetic translation, rotation, FOV changes, or cinematics. A “cosmetic” camera must not secretly redirect a throw or lunge.

Keep look, movement, and control responsive during live ordinary actions. Do not hide a live actionable world behind a frozen full-screen frame. Use stronger camera takeovers during an actual shared pause or a confirmed non-actionable recovery window.

Restore aim, lens, camera mode, self-hide, viewmodel visibility, carried-object rendering, shadows, and input ownership after every transition. Do not reintroduce doubled held slippers, invisible third-person bodies, detached first-person shadows, or emote/cinematic camera states that survive a seat change.

Do not silently narrow the normal field of view to obtain prettier screenshots. Preserve gameplay awareness while making presentation adjustments explicit and testable.

### Improve the connective actions

Inspect ordinary movement, stopping, jumps, landings, charge cancellation, hand grip, pickup reach, slide, shove, punch, lunge, block reaction, stagger, and recovery. Better anticipation and follow-through should fit the real action timings and not add new rooting or input delay.

For a lunge, visibly communicate preparation → committed reach → contact or whiff → recovery. A near miss is exciting because the reach visibly passed the target, not because a giant word says so.

For blocks and banks, show the real contact and redirected path. Exaggeration must not point toward a different pickup location or imply a collision that did not happen. Preserve intentional physics and knockback/stun rules. Correct genuine integration or presentation bugs within this assignment, explaining the difference between a bug fix and a broader balance redesign.

In first person, hand motion and world action must remain connected. In third person, body language must communicate the same event. No constant ragdolls or generic flailing.

Preserve the cute blocky cast, face language, approved rigs, character personality, environment identity, and already-approved menu artwork. Do not convert the game to realistic anatomy, generic anime, or a new art style. This protects the established identity and truly finalized assets, not the rough presentation of the friend-authored placeholders approved for refinement in the friend-work instructions.

### Ordinary hero skills also need complete presentation

Do not leave the two regular skills per hero looking generic while only their ultimates improve. Inspect every current skill's preparation, actual cast/release, active behavior, contact or failure, and recovery. Give distinct actions appropriate body, hand/viewmodel, moving effect geometry, and sound rather than reusing a hand raise for unrelated powers.

Preserve the current skill mechanics, inputs, immunity conditions, charge/cooldown behavior, and meaningful role differences. A dash should communicate movement; a recall should connect the real slipper source to the hand; a wall should read as an obstacle; a field should show its actual boundary and duration. Support these actions without making every ordinary skill compete with an ultimate.

Do not enlarge footprints, prolong stun, or quietly retune movement to make an effect seem stronger. A discovered correctness bug can be identified and fixed within the authorized implementation scope, but distinguish that from a new balance decision. Test ordinary skills during the central retrieval/chase exchange, not only in a showcase.

### Clarity and comfort are part of quality

Use a visible primary focus, supporting secondary motion, and sparse tertiary polish. Preserve the lata, slipper, players, chalk, and actual danger boundaries during overlapping effects. Bigger ability footprint is not a substitute for better VFX.

Audit background contrast and visual competition on real maps. Keep the Filipino street character of the setting while reducing accidental competition with tiny gameplay objects. Do not solve every visibility issue by increasing bloom. Keep any map edits here limited to directly needed visibility, playability, or measured performance fixes; this is not a restart of broad map beautification.

Provide useful independent controls for shake, cinematic camera movement, flash intensity, and announcer volume. Reduced effects must leave alternative information. Color must have number/symbol/contrast support. Avoid repeated full-screen flashes, heavy chromatic distortion, camera roll, and continuous FOV pumping.

Use representative captures to check close-up clipping, minimum on-screen sizes, occlusion, compression, and relevant window/aspect/hardware conditions for the changed presentation. Batch compatible cases; expand when evidence or shared changes warrant it rather than repeating every device/aspect permutation per tweak. Do not claim visibility from a perfect isolated screenshot.

---

## Spectator direction and a shared replay foundation

### Improve the existing broadcast director

Start with a stable elevated view that makes the relationship between can, taya, retriever, slipper, and safety understandable. Use closer or wider compositions when the story warrants them.

Do not cut to a scorer's face while their retrieval is still developing. Do not cut on every score notification. Follow the consequence, not merely the most recent event ID.

Test the existing interest and shot-selection logic against real matches. Verify that subjects are actually visible, the can is not occluded, the escape direction stays legible, and the shot does not leave before the outcome.

Respect manual spectator input. Autopilot should not fight an operator or reactivate after every manual adjustment. Camera transitions, overlays, and temporary emphasis should improve comprehension, not create a music video.

Do not bring back unsolicited automatic **live-match** replays or spectator-induced global pauses. Halftime is the specifically requested automatic replay location. Clearly label any historical footage rather than presenting it as live play.

### Build a coherent recording foundation for tag scenes and cinematic highlights

The desired final quality needs faithful reconstruction, not merely a bigger overlay around the old low-resolution frame buffer.

Implement an appropriate bounded recording/playback path for the current game. Player/prop poses, possession, can state/protection, animation transitions, ability/cast events, exact contacts, identities, and timestamps are likely inputs; capture only what the selected design actually needs. Choose pose samples, animation parameters, retained events, captured footage, or a hybrid based on fidelity, reliability, and the existing architecture, not because this guide named one preferred technique.

Use non-authoritative playback copies with gameplay scripts/colliders/scoring disabled. Do not rerun physics to rediscover a result. Exact contact and state transitions must remain true even if intermediate movement is interpolated.

Prefer shared history and reconstruction where they serve both victim catches and alternate-angle replays well. Separate their presentation policies. A hybrid or specialized component is fine when it has a real quality or engineering advantage; explain that choice rather than forcing an unsuitable one-size-fits-all system.

The existing captured-frame replay remains useful as a prototype, comparison, or honest fallback. Do not label ten-frame-per-second 360p footage as final-quality slow motion without evaluating it. Baked frames cannot provide a genuinely new angle.

### Preserve candidates during play

A great round-one sequence must survive until halftime after round four. A recent-history ring alone does not achieve that.

Preserve a bounded shortlist of complete candidate sequences as they occur, including enough lead-in and aftermath. Retain the actual recording, not only an event timestamp that points at overwritten data. Preserve first-half history across ordinary round resets; clear at the correct match/rematch boundary.

Select clips for **readable setup and consequence before spectacle score**. Strong candidates include a meaningful bank hit, a dangerous retrieval and escape, a restore into multiple catches, a triple catch, a lead-changing play, or an ultimate that actually converted into an important result.

A transparent event/episode scoring heuristic is enough to start. Avoid an opaque AI system. Replay selection never affects gameplay points.

A long chain may not fit the short halftime package. Choose the most readable complete portion or a different candidate instead of speeding through an incomprehensible montage or falsely labeling a partial sequence as the whole achievement.

### All-player halftime playback is a real requirement

A replay that works only for someone in `SpectatorCamera` is not completed halftime replay for the match participants.

Choose and implement the strongest delivery path for the actual project: a captured/encoded clip distributed within a bounded budget, a shared state/event clip rendered locally, or a justified hybrid. Evaluate bandwidth, latency, quality, memory, asset, audio, and determinism implications. Do not end by listing the options for me to select; make a supported choice and test real participant playback.

Do not assume every client recorded the same view or received the same retrieval markers. The host can choose a canonical highlight and identify/distribute the actual required content. Use common match/round/clip identity and synchronized start/end information. Local rendering differences must not create different game results or pause lengths.

Keep all recording, readbacks, queues, and clip preservation bounded. Preserve asynchronous capture safeguards where used. Avoid a capture-induced frame hitch at the busiest moment.

Use a preloaded fallback when content is incomplete or a client cannot play it: a truthful still/contact summary and standings, not fabricated footage, an indefinite loading pause, or an empty “best play” label.

### Replay presentation

Show **setup → action → consequence**, with a clear replay label, player identity, and one short explanation of what mattered. Slow only the key part when useful, not the whole clip. Preserve readable contact and the actual escape/catch result.

Use broadcast graphics that belong to TUMP. No loud full-screen package for every ordinary tag. At final results, a compact best-play option or appropriately timed final highlight can reuse this system without hiding the standings behind another long mandatory movie.

---

## Match pacing and score comprehension

Test ordinary between-round transitions around **three seconds**. Keep the initial readiness/loading gate separate; faster ceremony is not permission to start before players are ready.

For an eight-round default match, use round four for a roughly **ten-second halftime** containing one strong replay, brief standings context, and orientation toward the next taya. Ten to twelve seconds is a test range when the actual package needs it, not permission to append more separate countdowns.

With six three-second ordinary gaps and one ten-second halftime, between-round time is **28 seconds**, compared with seven five-second gaps totaling **35 seconds**. A twelve-second halftime would total 30 seconds. These calculations assume the proposed transition includes the entire role card/countdown rather than being added to it.

Keep next-role information, colors, and spawn orientation clear. Respect custom lengths; do not trigger halftime after the final round or impose an eight-round assumption everywhere. Default role fairness must remain intact.

Explain why somebody is winning without turning the HUD into a spreadsheet. Use a restrained active-defense scoring indicator and a brief halftime score breakdown. Do not fill the side feed with passive score ticks.

A genuine lead change can earn recognition if it is rare enough to matter, with anti-flutter handling for ties. Do not automatically celebrate every tiny ranking change during continuous defense scoring.

---

## Technical shape: extend the existing game coherently

Keep gameplay outcomes under the current authority boundaries. `MatchDirector`/`RoundDirector` and the relevant gameplay objects decide scores, tags, ownership, and state. Presentation responds to accepted results.

Add the minimum extra data needed to correlate one moment across the selected implementation: match and round identity, event/attempt/cast sequence, authoritative timing, actor and subject, contact position/direction, outcome, and specific additional data only where required. Tag staging needs faithful contact information; precise shot awards need the true release origin; replay needs retained history. Reuse fields and contracts that already provide those facts.

Prefer proving a real exchange over building a universal framework with dozens of speculative event classes. Keep one coherent owner for each responsibility. Extend working systems, or refactor/replace them when necessary, without leaving competing score, camera, announcer, or replay paths active. The existing class names are navigation hints, not mandatory architecture.

Give existing presentation dispatch viewer-aware profiles and deliberate priority. Correlate the can-state, score, sound, and flair pathways so one outcome does not produce contradictory or duplicated presentation. An absent branch in one dispatcher is not proof the feature is missing elsewhere.

Keep important invariants explicit:

- **One accepted outcome, one reward.** Duplicate packets, repeated contacts, prediction, and replay do not award twice.
- **State restoration is not reenactment.** Reconnect restores current state and does not replay old celebration/cast introductions.
- **Live control has a deadline.** Cinematics cannot extend stun, delay legitimate taya follow-up, or conceal an actionable world.
- **Camera effects do not own aim.** Cinematic/impact transforms do not silently change gameplay targeting.
- **Presentation randomness is separate.** Viewer-specific effects, skipped scenes, and reduced effects do not perturb simulation or AI random streams.
- **Identity survives role and scene transitions.** Stable slipper identity, holder state, player color, and round-specific role are not conflated.
- **Replay observes, never resolves.** Playback cannot collide, score, consume abilities, or change the active match.

Audit object lifecycle, pooled effect reset, material ownership, static subscriptions, camera handoff, and memory caps. Preserve shared material safety for per-player colors. No growing list of old clips, pending readbacks, stale renderers, or dead event listeners across rounds/rematches.

If new messages, score representations, or event enums change the network contract, version and validate it appropriately. Do not silently renumber existing wire values or accept incompatible clients. Respect the current reserved networking work and coordinate through the owner's authorized workflow rather than overwriting it.

Do not claim that the host seeing an effect proves clients receive it. Validate actor, victim, uninvolved client, and spectator paths separately.

---

## Suggested execution route and quality checks

The stages below are a suggested dependency map, not seven mandatory passes or a test matrix to run after every task. Reorder or merge them when the current project supports a better route. Apply the efficient-validation and isolated-workspace workflow above: batch checks around coherent features and reuse valid evidence. The backlog priorities determine scope; deferred/retired tasks are not prerequisites. Integrate relevant friend-work where it helps. Keep checkpoints playable and progress toward the whole experience; an isolated prototype is not the endpoint.

| Suggested stage | Working outcome | Evidence to seek |
|---|---|---|
| **1. Inspect and establish contracts** | Focused current-branch inspection, minimal design decisions, and a first in-engine implementation/prototype to validate the chosen route. | Verified current rules and dependencies; source findings distinguished from observation; research converts into actual changes. |
| **2. Readability foundation** | Own-only possession, player identity, requested low beacon, unified lata state, useful failed-action feedback, separate side feed, redundant-message cleanup. | All pickup routes agree; practice remains usable; owner and wrong-owner tests; a new viewer can identify objective, taya, and slipper ownership. |
| **3. One excellent complete exchange** | Release, can impact, landing, pickup, restoration, threat, block/whiff, escape, tag; coordinated sound and motion from all relevant views. | Comparable before/after footage with other players active; physical events stay truthful; no artificial input delay or taya lockout. |
| **4. Perspective-specific signatures** | Victim catch reconstruction, taya confirmation, milestone presentation, legal close-call recognition, authoritative chains and capped bonus points. | Real remaining-recovery cutoff, faithful contact, repeated-use comfort, per-player streak independence, duplicate protection, no bonus-generated extra ultimate charge. |
| **5. Full ultimate presentation** | Complete distinct sequences for the current six heroes. Sean and Phaister are useful contrasting first tests, but choose the order that best exposes risks and proves the architecture. | Accepted casts only; no duplicate prelude; live warning/counterplay preserved; actual deployment/contact payoff; simultaneous and rapid-consecutive casts; restoration after interruption; all roles/cameras. |
| **6. Broadcast and halftime** | Refined existing director, preserved meaningful candidates, quality replay path, all-player playback, shorter ordinary breaks, standings context. | A round-one clip survives to round four; readable setup/consequence; live and replay unmistakable; clients receive the selected content; bounded recording/transfer and truthful fallback. |
| **7. Full-match refinement** | Busy Classic and Hero Strike matches on intended hardware, relevant window/aspect sizes, presentation equipment, and network conditions. | Frame-time spikes, repeat comfort, control integrity, audio hierarchy, compressed-video clarity, manual spectator behavior, reconnect/round/rematch cleanup. |

An automated test proving that an object spawned does not prove the scene feels good. A staged cinematic is not freeform gameplay validation. A single isolated ultimate does not prove four-player readability.

### Important regression and stress situations

Treat these as feature-level coverage obligations, not a suite to repeat after every edit: wrong-owner pickups through each route; restoration while an attacker carries inside the box; protected-can contact; late tag events near recovery expiry; close taya catches; concurrent can-hit attempts; round end during a cinematic; simultaneous and back-to-back ultimates; reconnect during pause/playback; manual spectator takeover; and repeated rematches. Select the cases affected by the current changes, reuse existing valid coverage, and batch compatible scenarios. Establish needed host/client behavior with real peers where supported; label simulated evidence accurately. Preserve remaining gaps explicitly when tools/devices are unavailable, and keep independent implementation moving.

Test replay audio and UI too. A reconstructed scene should not speak every original announcer message twice or replay score awards into the live scoreboard.

Measure actual ability frequency and cumulative cinematic interruption per match. Do not assume the ultimate economy makes repetition harmless, and do not change that economy merely to hide presentation problems.

### The perceptual test that matters most

When human testers are available, show a new viewer the retrieval sequence without explanation: can they identify who is in danger, why, and what made the outcome impressive? Show the same sequence to an experienced player and check whether presentation helps them perceive and act.

Until then, inspect representative captures yourself and produce the comparison footage and short test notes for later human review. Do not invent human feedback or hold all progress for it; distinguish visual inspection from genuine player acceptance.

Test muted footage for visual comprehension and separately check whether sound communicates the major event identities. Review compressed footage because subtle locators and contact accents must survive ordinary sharing and tournament projection.

Compare repeated before/after exchanges and full matches. The target is to make people care before the announcement appears, not to make text tell them to care afterward.

---

## Execution, completion, and reporting

### Start implementing in this assignment

Inspect the current state, identify relevant friend-work, briefly align the active queue, and establish or reuse the efficient validation and compact-doc workflow above. Give me a short approach, then implement. Do not spend the assignment rewriting this document, building elaborate test infrastructure, or asking whether you should implement. That is already the request.

Use a practical loop: **inspect → choose → implement → run → review → refine**. Research just in time when it changes a decision. Let in-engine observations improve the working direction, and update the existing ledger so the implementation stays coherent across sessions.

Author and integrate the actual assets and behavior. A new script, prefab, animation clip, sound, or shader that is not wired into the normal game route is not the feature. Remove or reconcile superseded behavior rather than leaving duplicate effects and dispatch paths. Keep tests, settings, tutorial expectations, and documentation aligned where the changes affect them.

### What completion means

Deliver a working, integrated pass across Classic and Hero Strike that achieves the intent of this guide, including the shared exchange, ownership/readability, viewer-specific tags, real hero performances, distinct event/milestone channels, capped chain rewards, and participant-visible halftime highlights. The implementation can differ from the example recipes when the result is stronger.

Run the risk-appropriate builds, focused automated checks, and runtime/visual reviews needed to establish the delivered feature—not every available check. Address regressions and inspect normal play as well as staged fixtures. Identify the exact tested snapshot/artifact and any later unverified changes. Do not call a feature complete because it compiles, an object spawned, or only the host saw it. A working first hero or replay is a foundation for completing the rest, not permission to stop.

When the environment supports it, provide representative before/after gameplay clips or captures, a runnable build or the project's normal build output, and reproducible test evidence. Show both the close player experience and enough busy gameplay to judge readability. A cinematic-only showcase is useful, but does not replace live exchanges.

If a required tool, engine, asset, device, connection, or permission is unavailable, identify the exact blocker. Complete independent work, leave reproducible instructions for the unavailable check, and clearly distinguish implemented, integrated, verified, and still pending. Do not fake execution, silently downgrade the goal, or label a recommendation as a finished change.

### Keep the report useful and short

Start the final chat response with a short summary of what visibly changed. Put detailed technical results in the project's existing report/ledger format or a linked Markdown file rather than flooding the chat.

Include:

- **Implemented:** actual changes, relevant commits/checkpoints, and the build/artifacts produced.
- **Creative improvements:** significant departures from the guide and why they worked better in the game; identify which friend-authored starters you retained, substantially refined, or replaced and why.
- **Verified:** exact tested snapshot/build, the focused checks and visual reviews performed, valid earlier evidence reused, and remaining unverified changes. Keep commands/log details in linked run receipts rather than the whole chat.
- **Remaining:** real blockers, unfinished work, human taste checks, and the next concrete step where necessary.

Do not provide only a new plan, only a list of files edited, or only an instruction for another agent to finish the work. Deliver the implementation you can substantiate and a usable continuation state for anything genuinely incomplete. No unsupported claims of competition readiness, SEPAK-U superiority, or human approval.

---

## Useful research references

Consult these primary-source starting points when they help a concrete implementation decision. Verify their contents and relevant version details before relying on them; the links alone do not establish measured gameplay or video timings:

```text
SEPAK-U developer/publisher Steam page
https://store.steampowered.com/app/4566850/Sepak_U__Sports_Fighting_Game/

Riot: Clarity in League
https://www.leagueoflegends.com/en-us/news/dev/clarity-in-league/

Riot: The Craft and Fantasy of VALORANT Weapon Skins
https://playvalorant.com/en-us/news/dev/the-craft-and-fantasy-of-valorant-weapon-skins/

Riot: VALORANT Shaders and Gameplay Clarity
https://www.riotgames.com/en/news/valorant-shaders-and-gameplay-clarity

Blizzard presentation: Overwatch — The Elusive Goal: Play by Sound
https://www.gdcvault.com/play/1023010/Overwatch-The-Elusive-Goal-Play

Blizzard presentation: Replay Technology in Overwatch: Kill Cam, Gameplay, and Highlights
https://www.gdcvault.com/play/1024053/Replay-Technology-in-Overwatch-Kill

Unity documentation: Time.timeScale
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-timeScale.html

Unity documentation: AsyncGPUReadback
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.AsyncGPUReadback.html

Microsoft Xbox Accessibility Guideline 117: Motion
https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117

Git worktree: isolated working directories and detached validation checkouts
https://git-scm.com/docs/git-worktree

Unity Test Framework command-line filtering (use the matching installed version)
https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/reference-command-line.html

Unity default project directories and local Library cache
https://docs.unity.com/en-us/engine/6000.3/manual/get-started/project-configuration/default-directories

Unity persistent data paths (folder copies do not by themselves isolate player data)
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html
```

Search for additional official footage and developer explanations where needed. Verify the Unity/package versions in the actual repository before relying on version-specific implementation advice. Do not use unrelated current-game assumptions from memory as evidence.

### The final standard

**Keep TUMP's recognizable game. Make its ordinary exchange physically satisfying, its danger intuitive, its outcomes memorable, and its biggest moments spectacular.**

The audience should already be saying **“GET OUT!”** before the game confirms the escape. The player should want another throw because hitting the lata felt good, not because another meter told them to. The taya should enjoy landing a catch and immediately pursuing the next one. An ultimate should be a character performance that flows into real play, not a pretty interruption detached from the result.

**Use this guide consistently for its purpose, not rigidly for its recipes. Your creativity and current project knowledge should improve it as you work. Build the stronger version, test it honestly, and keep carrying the implementation forward.**

---

## Keep a usable continuation state

Use the canonical queue and compact active ledger described above. Before a handoff or likely compaction, update the current feature, meaningful design decisions, friend-work integration, development and tested revisions, dirty work, running job handles/result paths, blockers, and next action. Do not create another master task system or repeat full reports. Preserve deferred work and retired evidence behind current pointers.

Recheck relevant changed assumptions when resuming work. The older reports, exact timing prototypes, and all source hashes are historical anchors, not a command to reproduce an old checkout or to undo better intervening work. The friend's implementation may already cover some of the intended functionality by then. Improve and finish it rather than resetting its progress or accepting its placeholder quality as the finish line.

## Start implementing

**Inspect the actual integration state and relevant friend-work, preserve the working baseline, clean up active doc routing/priority, then implement the next coherent playable improvement.** Reuse or establish a safely isolated validation snapshot so selected tests can run while development continues when tools/resources permit. Do not stop after cleanup, branch comparison, runner setup, a new architecture diagram, or a passing test on an unintegrated component.

Develop the strongest version of the full presentation pass across the existing game. Keep the approved new-map/new-character and broad map-polish work available for its later phase without letting it displace the current priorities. Keep necessary reliability work attached to the feature it enables.

Show real progress: an exchange that feels better, a tag that reads and returns control correctly, an ultimate that flows into its outcome, or a replay that the match participants can actually watch. Carry sound, motion, cameras, effects, UI, and gameplay truth together.

> **Use this guide consistently for its purpose, not rigidly for its recipes. Improve the ideas and my friend's placeholders with your own creativity and knowledge of the actual game. Preserve the useful work, not its limitations. Build, test, observe, refine, and leave a coherent continuation state.**
