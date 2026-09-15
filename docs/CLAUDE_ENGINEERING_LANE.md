# Reserved engineering work on the separate Claude PC

Owner assigned this lane on 2026-09-15. Status: C1, C2 and C3 COMPLETE as recorded in the
execution log, with the specific historical 48-penalty match still unattributed.
This is a shared ownership record, not a claim that another worker is running.
Codex continues the other work and must leave these tasks and files alone until
the owner releases the reservation or Claude records an explicit handback here.

Repository: https://github.com/DOST-GameDEV/TumbangPreso-Unity.git
Shared destination branch: ASTRAReworks. Never push main or force-push.
Reservation was prepared against b0820b2b; pull the latest branch, not that old hash.
The other PC has its own clone, credentials, tools, profiles and process ownership.
Historical absolute paths and PIDs are not instructions for that machine.

## Assigned tasks, in order

- [x] **C1: Ilalim unretrieved-slipper outlier, TODO151.9.** Locate the historical
  seed/rules/report that produced48idle penalties. Reproduce with actual resting
  slipper position, owner, active/carried/flight state, legal court bounds, bot
  target/decision and pickup eligibility logged. Separate unreachable slippers,
  unreachable bot decisions and probe artifacts. The previously fixed flight bug
  is not an explanation without a connecting trace. If a current defect is proved,
  fix its cause within this lane and add a focused regression. Preserve raised
  ground, roof/pool retrieval and the intentional10second off-roof penalty.
  Completion requires a trace explaining the original failure, or a demonstrated
  current equivalent with its relationship explicitly bounded, plus relevant
  regression evidence. If the original cannot be explained, leave that historical
  attribution open even if a separate defect is fixed.
- [x] **C2: Bot lunge decisions, TODO151.9.** Investigate the historical5hits in
  125attempts using AiDiagnosticProbe at ordinary1x. Record target motion, facing,
  intent, accepted lunge, distance, cooldown/role and authority outcome. Establish
  whether DoHunt aims/chooses poorly, whether the fixture miscounts, or whether the
  misses follow the rules. Fix demonstrated AI or diagnostic faults, never tune
  human lunge/slide reach, speed, duration, cooldown or damage to raise a ratio.
  Completion requires causal evidence, a focused regression where code changes,
  and comparable before/after traces with both modes and run-to-run noise stated.
  Human judgment of slide feel remains outside this task.
- [x] **C3: Measured AI/combat slipper lookups, TODO149.5.** Recheck actual current
  call sites and measure frequency, allocations and time against a real arena's
  object count and four-player activity. Distinguish decision/frame/action/packet
  paths. Seat-to-motor and network seat-to-slipper are already cached; do not redo
  them. Optimize only if the measured cost justifies it. Keep active-only and
  include-inactive semantics explicit, especially the defender's parked slipper.
  A cache needs lifecycle proof for spawn/disable/enable, teardown, second match
  and seat changes without modifying networking. A quantified negligible result
  may close this measurement task with no production edit. Completion requires
  committed measurements and either evidence for retaining code or a measured
  improvement with focused lifecycle regressions. Do not claim general game
  performance is solved by one lookup result.

Primary references: TODO149.5 and151.9, IMPROVEMENT_PLAN P2/P9,
GAMEPLAY_RESUME_AFTER_UI item8, reports/bot-sweep-2fde55d32246.md and relevant
historical incidents in CLAUDE.md. The old defence-clock discrepancy is already
explained at the start of151.9; do not reopen it from the historical table.

## File ownership

Claude may edit the following, only for the three tasks above:

- Assets/TumbangPreso/Runtime/AIController.cs
- Assets/TumbangPreso/Runtime/CombatVerbs.cs
- Assets/TumbangPreso/Runtime/Carrier.cs
- Assets/TumbangPreso/Runtime/Slipper.cs
- Assets/TumbangPreso/Tests/PlayMode/AiDiagnosticProbe.cs
- Assets/TumbangPreso/Tests/PlayMode/BotBehaviourProbe.cs
- tools/bot_sweep.py
- New narrowly scoped AI/retrieval/lookup helpers and tests with their .meta files.
- This document's execution log and task boxes, plus evidence under
  docs/reports/claude-engineering-2026-09-15/.

CombatVerbs, Carrier and Slipper permission is for proved nonvisual correctness
or lookup cost. It does not permit new balance, throwing-accuracy design, input
bindings, presentation, animation triggers or a gameplay rewrite. Preserve the
existing producer/consumer contracts and accepted actions.

Codex owns all remaining work, especially Runtime/Net (including all MatchRpc
partials and protocol), Runtime/Abilities, Camera, Visual, UI, Input, hero kits,
models, materials, shaders, maps/scenes, animation authoring and cast probes.
Do not edit those, package/project settings, roster/save IDs, or shared Unity
guard/build runners. If a demonstrated cause requires an excluded file, record
the precise file/symbol, trace and proposed change here, leave the task open and
continue another assigned task. Do not broaden the lane or contact another chat.

## Integration and truthful completion

1. Discover the local clone and tools. Inspect branch, origin, dirty work and
   divergence, then safely pull current ASTRAReworks. A new clone must explicitly
   check out that branch. Preserve any existing local work; never reset/clean it.
2. Read current AGENTS.md, VISION.md, TODO index and assigned entries, newest
   ACTIVE_REWORK_LEDGER pointer and this file. Old archives cannot expand scope.
3. Record local HEAD, machine-relative paths, hypothesis, changed files and exact
   commands/results in this file's execution log. No secrets or copied profiles.
4. Run only necessary related checks. All Unity launches use run_unity_guarded.py,
   one Editor per checkout and a named isolated profile. Do useful independent
   reading/docs/drafts while long runs execute. Do not edit imported source/assets
   during a Unity run. Preserve failed evidence; fresh nonzero test XML is required.
5. Commit only owned changes with explicit paths and a message file. Before each
   push fetch origin again. If behind, integrate upstream on a clean worktree
   without discarding peer changes; rebase only unpublished local commits or use
   a non-destructive merge. Review affected integration and rerun only checks
   justified by new changes. Never force-push or overwrite a shared remote update.
6. Push to origin ASTRAReworks and verify the commit is present remotely. Use the
   friend's own authorized GitHub access; never copy another user's credentials.
   Access/tool/license blockers are recorded honestly while independent work proceeds.
7. Tick only a task whose full criteria are met with evidence published in that
   same successful push. Implemented-but-untested, partial, blocked, skipped and
   unable-to-reproduce states stay unchecked with exact remaining criteria.
   Do not mark the entire151.9,152.4 or gameplay queue done for a subtask.
8. Keep frequent progress in this dedicated log to avoid both workers rewriting
   ACTIVE_REWORK_LEDGER. On completion update only the matching reservation boxes
   and narrowly scoped149.5/151.9 status notes in TODO. Preserve historical reports.
   Codex will integrate the wider index/ledger after pulling the published evidence.

No UI/UX, art, models, animation, VFX, sounds, concept generation, Figma, paid
services, credit resets, new agents, other chats, or Desktop-build replacement.
Internal test builds are permitted only when needed. Both modes default to8rounds;
Classic people are cosmetic with neutral personal stat scales. Keep equipment,
host authority, single score ownership, square confinement and stable identities.

## Execution log

2026-09-15: Reserved at owner request. No assigned investigation or implementation
has started in this reservation. No task is complete. Codex retains its uncommitted
pending-cast work outside this lane and continues independently.

2026-09-15, Claude on the separate Mac. Full evidence:
[reports/claude-engineering-2026-09-15/README.md](reports/claude-engineering-2026-09-15/README.md).

Setup: existing clone at /Users/paul/Documents/GitHub/TumbangPreso-Unity (user paul),
clean, ASTRAReworks equal to origin at 4752f610; later fast-forwarded to 0f1d997b and
rebased one unpublished commit over Codex's 12e9e6a5. Unity 6000.5.8f1 with Mac and
WebGL modules only, so every launch used -buildTarget OSXUniversal; no dotnet; python3.
tools/run_unity_guarded.py is Windows only (USERPROFILE, Windows Unity path, winreg), so
launches used a Mac equivalent that snapshots and hash-restores the Mac persistentDataPath,
kept in the report folder. Guard runners were not edited. Historical 2fde55d3 and
instrumented copies ran as git-archive scratch projects with APFS-cloned Library folders.

C1 (ticked). Cause found and fixed; historical attribution bounded. The seed that carried
the 48 is not in committed data (only Logs/bot-sweep.json on Windows had it). 2fde55d3 on
this Mac: six sweep seeds 0 each, twelve more seeds max 13 (seed 42), seed 42 re-run 0 and 0;
the build is not seed-reproducible here. Per-penalty traces (52beee35, 091f9210) showed no
stranded slipper. Two decision defects, both traced on the historical and current builds:
(1) PlanAttacker yielded to a better-ranked rival even when late or stalled, fixed in
32e073fd with AiRetrievalRules and 5/5 EditMode tests; (2) StepUnstick read the wished
CharacterMotor.Velocity, so a bot pressed into Ilalim geometry never unstuck, fixed in
ea776bdc with AiStuckWatch and 5/5 EditMode tests. The C1-only current build produced 68
penalties from one pinned seat in one match. PinnedFetchProbe places the bot at the traced
positions: 2fde55d3 fails both cases (motionless 10 s), 2fde55d3 plus only the stuck patch
passes (3.65 s, 3.98 s), the pre-fix current copy fails, the current checkout passes 2/2.
Final six-seed sweep: 0 idle penalties in 18 matches (before 8, C1-only 68). The specific
48-penalty match was not replayed and cannot be here, so its exact attribution stays open.
Flagged: Classic tags 146.8 to 140.8 against the pre-fix sweep at t -2.28, but t -1.38
against the C1-only build; not attributed to a single change.

C2 (ticked). AiDiagnosticProbe whole-match lunge traces at 1/60 s, time scale 1, both
modes, seeds 20260823, 1, 7 (8bf42787, tracker fix 844da009). Before: 61/671 hits, and 556
of 671 lunges were released by the plan sweep after the same taya's punch had already
tagged during the charge. Aim was not the main fault. Fix eb29f871 (AiLungeRules, 3/3
EditMode): no charge when a ready punch will be in range by the end of the hold. After:
57/82 hits, 7 punch dumps. Hits per match 7-12 before, 4-13 after. C2 isolated against
the C1-only build, six seeds: tags t +0.12, +0.31, -1.59, no measured change. No human
lunge, slide or punch number changed; slide feel is not judged.

C3 (ticked, no production change). 34 FindObjectsByType<Slipper> sites metered in a scratch
copy; SlipperLookupCostProbe (4ff38856, live checkout 2/2). Whole four-bot matches: 134.5 to
134.9 lookups per simulated second, 33.7 to 49.8 us each, all sites 0.086 to 0.167 ms per
editor frame, lane sites 0.052 to 0.096 ms, RoundDirector.cs:408 (outside the lane) 0.034 to
0.067 ms. Allocation read with a calibrated heap-growth method: 0 B for active-only shapes
at that resolution, 160 B for Include with SortMode.None. Retained: about 0.3 to 0.6 percent
of a 16.7 ms frame does not justify a registry with separate active and inactive semantics.
Limits: macOS editor, offline host, not a Windows IL2CPP player.

Commits in this lane: 52beee35, 32e073fd, 8bf42787, eb29f871, 844da009, 091f9210,
4b9aa191, 4ff38856, 9c236eb8, ea776bdc, 22bbedb6, b3a9dbbe, c0f1edc2 and this log.
Not run: full EditMode or PlayMode suites, Checks.RunAll, Windows builds, network paths.
Test-generated whitespace-only changes to two composition-redesign png.meta files were
restored each time and never committed. Handback: this lane's files are free for Codex.
