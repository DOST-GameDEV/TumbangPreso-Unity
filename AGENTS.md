# TUMP repository instructions

## Current owner direction, 2026-09-21

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

## Start and resume

Read [active ledger](docs/ACTIVE_REWORK_LEDGER.md), then the
[current queue](docs/TODO.md#current-implementation-queue). They are the only active
resume/task pointers. The Nationals plan owns detailed feature design, not a second
status queue. Read topic details as needed: [vision](docs/VISION.md),
[play feel](docs/PLAY_FEEL_REWORK_PLAN.md), [ability plan](docs/ABILITY_REWORK_PLAN.md),
[kit decisions](docs/HERO_KIT_REWORK_DECISIONS.md),
[ability direction](docs/PHILIPPINE_ABILITY_DIRECTION.md), [art](docs/Art_Direction.md),
[rigs](ASTRA.md). The supplied implementation brief is saved under
[the current report](docs/reports/presentation-pass-2026-09-21/implementation-brief.md).

Prioritize the complete throw, can, retrieval, restore, chase, escape/tag exchange;
ownership/identity/locators/feed; victim catch reconstruction and capped chains;
all six heroes' distinct full ultimate performances; spectator and all-player
halftime highlights; integrated Classic and Hero Strike validation. Maps, new
characters, Inday reconstruction and broad secondary/device UI coverage are deferred.
Preserve their work. Historical demo-day, maps-first and empty-the-entire-TODO
instructions do not control the current queue.

## Workspace, ownership and delivery

- Work/deliver on ASTRAReworks. Fetch and inspect dirty/diverged state. Never edit,
  merge into or push main; never reset, clean, force-push or discard another's work.
  Relevant friend branches may be read, tested and scoped-integrated. Preserve authorship.
- C1-C3 have an explicit handback in [their lane](docs/CLAUDE_ENGINEERING_LANE.md).
  C4 [request safety](docs/CLAUDE_REQUEST_SAFETY_LANE.md) remains reserved: MatchRpc.cs,
  NetSession.cs and request-guard helpers/tests. Do not duplicate its audit or edit
  those files without an actual handback. Record exact dependencies and keep working.
- Preserve controller backend ownership: GenericPadBridge, MenuNav and mapping.
  Keep the controller artwork, callouts and connections. No input backend replacement.
- Preserve all profiles, saves, IDs, source art, unfinished local work and unrelated apps.
  Handoffs belong in chat; repository/developer material never goes to Google Drive.
- Push stable verified batches to origin ASTRAReworks, fetch before push and verify
  remote HEAD. Use git commit -F, sole author, no attribution trailers or em dashes.

## Standing product and art contracts

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
