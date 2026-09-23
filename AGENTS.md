# TUMP repository instructions

## Latest owner direction, 2026-09-23: visual communication and appeal

The owner asked for the game to look better and feel more satisfying WITHOUT more
realism, for a minimalist, professional in-game HUD that is easy to look at, and for
on-screen effects and indicators that work together without overwhelming the player
(Sepak U named as the example). This is TODO VISUAL-1. It supersedes the 2026-09-01
"do not touch the in-match HUD" scope note for that pass; no rule, timing, authority or
network contract changes. Design, reasoning and file-level routes:
[NATIONALS_POLISH, VISUAL-1](docs/NATIONALS_POLISH.md#visual-communication-and-appeal-pass-visual-1-2026-09-23).
Priority was rethought the same day: P0 finish in-flight work, then VISUAL-1 batches,
with backlog paperwork moved behind visible work. See
[the priority order](docs/TODO.md#current-implementation-queue).
The owner then widened it: "u can edit all UI and hud in the actual game btw including
the match end and mid round report and icon", "thoroughly revamp it too and make it more
visually pleasing + good to look at". Every in-match surface is in scope (VISUAL-1.4,
1.16, 1.17, 1.18); supplied artwork is still never repainted.

**Docs layout after the 2026-09-23 cleanup.** `docs/TODO.md` is the only status queue and
carries an index row per numbered entry; the open entries' whole bodies are in
`docs/TODO_Backlog.md`; finished ones stay in `docs/TODO_Archive.md`; superseded plans and
status files moved to `docs/archive/` (old-path table in `docs/archive/README.md`).
`docs/README.md` indexes every document with its status. Nothing was deleted.

## Explicit current UI additions, owner2026-09-23

Finish the assigned UI before later map refinement. The owner explicitly authorizes
reworking the Terms popup and its consent control within the otherwise protected
login: consent is an empty or solid-filled square, never a tick. Other login/title
art stays protected. Loading tips are inline, with rotating replacement artwork.
New arrival-flow and other built surfaces are tracked in the UX-1 UI inventory.

## Concise UI copy, owner2026-09-23

Do not explain obvious navigation in the interface. Back uses an arrow, without
redundant BACK text or a sentence explaining what Back does. Visible scrollbars do
not need SCROLL TO READ. Remove generic press/click instructions when the control
already communicates the action. Keep meaningful game rules, constraints, errors,
state and useful device bindings. Preserve internal control identities and operation.

## Targeted quality, owner2026-09-23

Do not improve for the sake of improving. Judge the existing result first and name
the concrete visual, usability or functional problem. Improve those weak parts;
preserve parts that already work. This applies to UX-1 and the later map, asset and
animation reviews. Inspect every assigned part, but do not manufacture changes to
make a review look busy or mistake a passing test for aesthetic approval.

## Supersession check, owner2026-09-23

The owner clarified that old TODOs must not contradict newer changes, especially
retired FUTURE.md plans. Completing the TODO means resolving current requirements,
not reviving every historical proposal. New owner instructions and current adopted
designs win. Check the retirement/supersession notices, later implementation and
current scope before acting on an old entry; an OPEN heading or missing old class
is not enough to make it an active task. Classify replaced, retired and already
implemented entries with their replacement/evidence, preserving IDs and history.
When current relevance is uncertain, finish the source/design reconciliation first.
Do not turn that uncertainty into a new feature, a speculative refactor or an
invented external blocker. In particular, the completed in-game HUD must not regain
retired layouts, extra text/timers or other superseded UI through the backlog audit.

## Follow-on refinement mandate,2026-09-23

The owner added REFINE-2 after the older actionable queue: research good-game
qualities and pleasing/satisfying stylised visuals first, then genuinely improve
each of the five maps individually, natural animal behaviour, bot inactivity and
actual gameplay/animation. Realism is not the target and 3d-asset.com is only one
reference. Sean's walking arms, the other characters' walking, throwing and both
pektus directions are explicitly rejected for further refinement. Do not treat
earlier passing checks as final motion/art acceptance. Work one map, character,
movement or aspect at a time, with a local plan; no universal texture/animation
patch as a substitute. Preserve every older task. Exact requests and the queued
[research/execution plan](docs/reports/map-by-map-refinement-2026-09-23/research-and-execution-plan.md)
are saved; status remains in TODO and the resume point in the active ledger.

## Standing in-game UI outline rule,2026-09-22

The owner explicitly rejects red UI outlines: "replace all red outline with black
in ingame ui". Gameplay HUD, spectator controls, notifications, prompts, countdown,
ability readouts, training/replay overlays and in-match chat use BLACK text/icon
strokes. Use UiTheme.InGameOutline, not the painted front-end DeepInk/brand red.
Keep meaningful role/ability/text fills and supplied artwork intact. This is a
standing art rule, not permission to recolour character or environment outlines.

## Current owner direction, 2026-09-21

### Full remaining backlog, latest correction

The owner rejected ending the goal at the presentation-only boundary and said
"finish everything note yet done". Complete the remaining actionable project
backlog, including the formerly deferred maps, Rafi/seventh hero and lagoon map,
Inday work, and remaining UI/qualification. Earlier "later" labels determine
order, not exclusion from this assignment. Preserve completed work and task IDs.
Never close the overall goal merely because a phase, build or checkpoint passed.
Reconcile old open entries with actual source/evidence; do not redo superseded
designs or count unimplemented features as human-review blockers. Continue every
independent implementable item before stopping for genuine external dependencies.
This correction supersedes the narrower presentation-only queue and stop rule.
The owner then made the scope explicit: "i want every single thing in todo to be
done pls mark that in todo and shit". The full TODO, including preserved numbered
entries, is assigned. Account for every unfinished requirement with implementation
and evidence or a concrete external dependency; no silent exclusions by section.

### Two premature stops: explicit owner correction

On 2026-09-21 the owner said they were deeply disappointed that this agent stopped
mid-task **twice in one day**, despite repeated instructions to work autonomously
until the assignment was finished. Their latest instruction was: "log this in
agents md how i was sk disappointed that u stopped mid task twice today already
even tho ur not done with everythung".

These were agent mistakes, not pauses requested by the owner. The popup redesign,
a passing test, a build, a published batch or a documentation checkpoint does not
complete the original implementation prompt and research-expanded presentation
queue. A status question or a new correction does not cancel that assignment.

- Continue the unfinished authorized implementation in this conversation without
  requiring another "continue" from the owner. Keep progress updates in commentary.
- Do not end the turn with a final delivery while independent in-scope work remains.
- Check completion against each task's actual requirements. Do not mark a broad
  task done because a subset passed, an old system exists or captures were saved.
- Human taste and unavailable devices limit specific validation claims; they do
  not justify stopping other implementation or checks that can still be done.
- If an actual external blocker prevents all remaining progress, record exactly
  what is blocked and why. Do not invent a blocker or hide unfinished work behind
  a polished completion report.
- Preserve every existing task and its evidence. The separately deferred backlog
  remains preserved; do not silently redefine the active phase as complete.

The owner asked to implement the full gameplay-feel/watchability prompt and then
said: "DO NOT ask me questions do not stop for any reason and just get this done".
Continue autonomously while the owner is away in THIS conversation through coherent
implementation, runtime inspection and refinement. Do not stop at a plan, one hero,
a passing test or a checkpoint. Record a real blocker and continue independent work.
No paid services, usage resets, subagents, other conversations or Desktop replacement.
Do not infer permission to spend credits from the autonomous-work instruction.

**Latest steering:** complete the thorough plan and documentation reconciliation
before further gameplay edits. The detailed design lives in
[the current Nationals plan](docs/NATIONALS_POLISH.md#current-delivery-design-2026-09-21).
Preserve implemented progress. Once this planning correction is complete, continue
through the canonical queue without another approval gate for ordinary in-scope work.

**Latest AFK instruction,2026-09-21:** the owner is at school all day and asks for
autonomous work, with questions saved for later. Continue from the completed plan.
Record non-routine decisions/dependencies in the ledger for their return. This does
not authorize paid work, resets, delegation or crossing contributor reservations.

## UI correction, 2026-09-21

The owner rejected the authored UI, especially the green full-screen halftime
board: halftime should be a popup over the game. Use image generation for visual
ideation as explicitly requested, then implement a compact animated treatment.
Keep the court visible. Remove the green backdrop and full-screen standings page.
Preserve the shared replay/timing/gameplay work and all remaining task IDs; this
correction changes presentation, not the authorized gameplay scope. Use the
built-in image tool; do not switch to a paid API/CLI path or spend reset credits.

## Start and resume

Read [active ledger](docs/ACTIVE_REWORK_LEDGER.md), then the
[current queue](docs/TODO.md#current-implementation-queue). They are the only active
resume/task pointers. [docs/README.md](docs/README.md) maps every document; an entry's
body is in [TODO_Backlog.md](docs/TODO_Backlog.md) when its index row says so. The Nationals plan owns detailed feature design, not a second
status queue. Read topic details as needed: [vision](docs/VISION.md),
[play feel](docs/PLAY_FEEL_REWORK_PLAN.md), [ability plan](docs/ABILITY_REWORK_PLAN.md),
[kit decisions](docs/HERO_KIT_REWORK_DECISIONS.md),
[ability direction](docs/PHILIPPINE_ABILITY_DIRECTION.md), [art](docs/Art_Direction.md),
[rigs](ASTRA.md). The supplied implementation brief is saved under
[the current report](docs/reports/presentation-pass-2026-09-21/implementation-brief.md).

The 2026-09-23 priority order in TODO governs. The paragraph below records the
2026-09-21 presentation priorities, which are implemented and retained.

Prioritize the complete throw, can, retrieval, restore, chase, escape/tag exchange;
ownership/identity/locators/feed; victim catch reconstruction and capped chains;
all six heroes' distinct full ultimate performances; spectator and all-player
halftime highlights; integrated Classic and Hero Strike validation. That software
checkpoint is retained. Maps, new characters, Inday and remaining secondary UI
work now follow it in the same assignment. Historical demo-day scheduling and
superseded designs do not override the current queue or justify repeating done work.

## Home-screen animation

- Owner, 2026-09-23: any home-screen, menu-background, season or hero-showcase animation starts
  from [docs/HOME_SCREEN_ANIMATION_METHOD.md](docs/HOME_SCREEN_ANIMATION_METHOD.md). Reuse its
  method (research, real posed models, unaltered faces, re-timed beats, frame-by-frame review);
  never copy the existing Zack loop's content. The current loop's source and doc are
  `ArtSource/home-scene/` and `docs/reports/home-scene/README.md`.

## Workspace, ownership and delivery

- Owner update, 2026-09-23: Claude is also working on a different feature that is
  not yet in TODO. Continue this queue normally. Treat unexpected edits as possible
  concurrent contributor work: preserve them, inspect direct overlap, and do not
  revert or stage them as generated churn without confirming provenance. This
  supersedes the earlier assumption that this is the only active contributor.

- Work/deliver on ASTRAReworks. Fetch and inspect dirty/diverged state. Never edit,
  merge into or push main; never reset, clean, force-push or discard another's work.
  Relevant friend branches may be read, tested and scoped-integrated. Preserve authorship.
- Owner handback2026-09-21: "get everything done u are the only agent working on this".
  All remaining implementation is assigned to this task. The old C4 contributor
  reservation no longer blocks necessary MatchRpc/NetSession/authority integration.
  Preserve its evidence and already completed work; fix feature-relevant defects
  and qualify compatibility. This does not authorize delegation or unrelated audits.
- Preserve controller backend ownership: GenericPadBridge, MenuNav and mapping.
  Keep the controller artwork, callouts and connections. No input backend replacement.
- Preserve all profiles, saves, IDs, source art, unfinished local work and unrelated apps.
  Handoffs belong in chat; repository/developer material never goes to Google Drive.
- Push stable verified batches to origin ASTRAReworks, fetch before push and verify
  remote HEAD. Use git commit -F, sole author, no attribution trailers or em dashes.

## Latest owner additions, 2026-09-22

- Latest Rafi correction: no gills. Remove them from the active model, concept,
  animation hooks and lore. Earlier gill requirements are superseded; preserve
  historical evidence but do not reintroduce the feature on a later model pass.

- Rafi model correction: the owner explicitly rejected the separate box-recipe
  model as unlike the existing characters. Find the repository's old voxel guide/
  builder, make a dedicated copy and retrofit that copy for Rafi. The identified
  sources are docs/Voxel_Person_Guide.md and tools/build_person_voxel.py; the new
  recipe is tools/build_rafi_voxel.py. Preserve the original builder and approved
  characters. Use its actual native donor face/skull, family proportions, chamfer
  and outline-normal pipeline, not merely its rig underneath unrelated geometry.
  Retain selected B identity, kit, map and other progress. The rejected attempt is
  archived under ArtSource/rafi/rejected-box-recipe-20260922 and is not a baseline
  to defend or ship. Do not spend more native capture work on the rejected model.
  The owner clarified that the reference is the HERO cast (Sean, Cheska, Dante,
  etc.), and explicitly requested CANONICAL_RENDERING_PIPELINE.md plus related
  modelling docs. Use image-generation ideation as requested, then the copied
  builder and current native hero lineup for the actual model. Make Rafi his own
  character/lore; never edit the existing characters or original builders.
- Lagoon correction: the owner rejected the sparse regular dock village. Cross-
  reference the supplied water-village photo and documented Sama Dilaut/Sama-Bajau
  architecture. Refine house construction, layout and life, and add detached houses
  standing over the water on piles. Keep the newly requested islands/mountains.
  Preserve playable court/recovery rules. Fixed houses do not levitate or bob;
  distinct houseboats may float. The reference photo is not a shipped texture.

- Animate the sky: slow per-map cloud drift, steady sun/horizon, correct pause and
  replay sampling/return. A static sky does not satisfy the latest request.
- Inday: remove the pogo-like arm attachments from every model and first-person
  view; give her plain brown arms. This explicitly supersedes the older coral-guard
  and exact-source-arm preservation requirement. Preserve the rest of her model,
  simple hands, rig, animation, palette identity and all unrelated character work.
- Owner is sleeping; continue the entire TODO in this conversation. These additions
  modify the active queue and do not replace or close its remaining requirements.

## Standing product and art contracts

- Latest map feedback2026-09-21: blank/poorly textured buildings and empty skies
  require thorough material/sky refinement on every map. Plan construction-specific
  treatments; do not spam one texture/noise across all buildings. Shaders are allowed.
  Keep native style, supplied imagery, different map identities and readable action.
  Label diagnostic/false-color images before showing them; they are inspection
  aids, not proposed replacements for the user's buildings or palette.

- Both modes ship. Both default to eight rounds; custom lengths remain configurable.
  Classic people are cosmetic with neutral stats. No Street Hype mechanic or meter.
- Preserve approved cute blocky people, flat faces, simple no-thumb hands, rigs,
  bone paths, action names and GUIDs. No roster redesign. Body and FPP must agree.
- Preserve owner/girlfriend supplied final menu/login art, pixels and aspect ratios.
  No version stamps in UI; keep internal version/protocol identity. Use Darumadrop
  at readable sizes and supporting type for dense text. Existing project-generated
  icons are swappable placeholders, not approved final art; no gratuitous icon pass.
- Each hero/action needs distinct form, silhouette, casting, motion and sound.
  Do not reuse noisy patterns/cracks/symbols as design. Dante's orbiting protectors
  are the retained baseline: refine only with a demonstrated improvement. Fitted
  armor markings remain open. No noisy basalt overlay or rejected Inday studies.
- Keep actual can/slipper/players/chalk/routes readable on Low and during overlaps.
  TUMP stays playful sport, without killing, guns, lasting destruction or realistic horror.
- Aim direction remains the established sight-line launch. The guide intentionally
  communicates direction/settling with bounded movement/early-hold inaccuracy.
- Animation covers preparation, release, contact, interruption and recovery without
  inventing hits or adding rooting. Preserve keyboard/mouse, controller and touch.

## Engineering and verification

- Owner reiterated on2026-09-24: prioritize the quality of the product over
  perfecting tests or capture fixtures. This model tends to overinvest in validation.
  Use existing checks/render routes where possible; do not grow a new test framework
  for every reversible art change. Inspect the real result, check the relevant risk,
  then return to implementation. Quality remains required; extensive regression is
  the final gate after the assigned features, not the main activity of every batch.

- Owner correction, 2026-09-23: the agent again spent excessive time and tokens
  repairing capture fixtures, camera staging and test assumptions while the main
  feature queue remained unfinished. The owner explicitly rejected this repeated
  verification loop. That was an execution failure, not feature delivery.
- For each coherent implementation batch, run one focused verification pass.
  Capture/test tooling gets at most ONE bounded repair and retry for that batch,
  including across compaction; restarting context does not reset the allowance.
  If the tooling still fails, preserve the failure, record the exact unverified
  claim and next diagnostic in the ledger, and continue independent feature work.
  Do not spend another cycle perfecting screenshots or recreating existing proof.
- Fix genuine game defects exposed by verification and run their smallest relevant
  check. This exception does not authorize unrelated fixture work or broader suites.
  Never mark a failed/unverified requirement complete. Final integration owns the
  deferred evidence; quality and honest status remain mandatory.
- Record the check's question, stop condition and tooling retry count before a run.
  After it passes, implement the next queued feature. Do not seek more reassurance.

- Owner correction, 2026-09-22: this agent has spent too much time in verification
  and test-tool repair loops while required features remained unimplemented.
  Treat that as a known failure tendency. Quality remains the highest priority;
  this is a sequencing correction, not permission to skip meaningful validation.
- During implementation, choose the smallest check that resolves a concrete risk
  introduced by the change. State its question and stopping condition before running.
  After it passes, return to the next unfinished feature. Repeat or broaden only
  for new changes, an actual failure, or a specific unresolved concern.
- Do not expand diagnostics, repair unrelated stale fixtures, regenerate unchanged
  evidence, or chase exhaustive coverage while major planned features are missing.
  Preserve deferred checks and failures in TODO/ledger with their exact scope.
  Comprehensive integration and regression testing belongs after ALL features are
  integrated. Never trade away correctness, visual quality, or honest completion claims.
- Current order correction: the completed rehost check closes this network pass.
  Implement Rafi and the lagoon now; remaining recall/overlap/whole-backlog checks
  move to final integration. They are deferred, not deleted or marked passed.

- Core package is engine-free; Core compiles the same sources. Host owns outcomes;
  MatchDirector.AddScore owns points. Preserve square confinement and deduplication.
  Bots submit InputIntent. Overlapping stuns use Max. Stable identity is not ownership.
- Current source is network protocol truth. Wire changes require compatibility
  validation; never renumber existing values or use historical version literals.
- Use `python tools/run_unity_guarded.py` for every Unity launch and a named test
  profile. Discover installed Unity from ProjectVersion, never copied paths/PIDs.
  No tests against real profiles/services. Keep one Editor per project directory.
- Freeze inputs only in the workspace under test. Development can continue in a
  separate isolated workspace. Never share writable Assets/Library/Temp/obj, profile
  guards, ports or output paths. Prefer one heavy workload; keep caches warm.
- Choose focused tests by changed behavior and realistic risk. Require fresh nonzero
  XML and expected cases. No routine full suite, clean rebuild or zero-test pass.
  Preserve failures. Do not weaken assertions to make tests green.
- Inspect real normal-speed owner/observer motion and actual target builds. Compilation,
  object spawning, staged captures and host-only views establish different limits.
  No unsupported human approval, freeform-play or multiplayer claims.
- Build with guarded GameBuilder.BuildWindows when justified, into a named internal
  output. Verify actual executable/data and run that exact build. Do not replace Desktop.
- Keep the ledger concise: current feature/decision, source/tested revision, dirty
  work, owned jobs/process/result paths, blockers and next step. Detailed receipts
  go in reports. Stop only task-owned helpers/tabs when done; preserve unrelated work.

All earlier instructions, detailed approvals, rejected approaches, task IDs and
historical receipts are preserved in [the intake archive](docs/reports/presentation-pass-2026-09-21/intake/AGENTS.md).
Read relevant details before touching their topic. Historical scheduling is superseded;
standing preservation/safety constraints remain binding.
