# Reserved engineering work on the separate Claude PC

Owner assigned this lane on 2026-09-15. Status: RESERVED, NOT STARTED.
This is a shared ownership record, not a claim that another worker is running.
Codex continues the other work and must leave these tasks and files alone until
the owner releases the reservation or Claude records an explicit handback here.

Repository: https://github.com/DOST-GameDEV/TumbangPreso-Unity.git
Shared destination branch: ASTRAReworks. Never push main or force-push.
Reservation was prepared against b0820b2b; pull the latest branch, not that old hash.
The other PC has its own clone, credentials, tools, profiles and process ownership.
Historical absolute paths and PIDs are not instructions for that machine.

## Assigned tasks, in order

- [ ] **C1: Ilalim unretrieved-slipper outlier, TODO151.9.** Locate the historical
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
- [ ] **C2: Bot lunge decisions, TODO151.9.** Investigate the historical5hits in
  125attempts using AiDiagnosticProbe at ordinary1x. Record target motion, facing,
  intent, accepted lunge, distance, cooldown/role and authority outcome. Establish
  whether DoHunt aims/chooses poorly, whether the fixture miscounts, or whether the
  misses follow the rules. Fix demonstrated AI or diagnostic faults, never tune
  human lunge/slide reach, speed, duration, cooldown or damage to raise a ratio.
  Completion requires causal evidence, a focused regression where code changes,
  and comparable before/after traces with both modes and run-to-run noise stated.
  Human judgment of slide feel remains outside this task.
- [ ] **C3: Measured AI/combat slipper lookups, TODO149.5.** Recheck actual current
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
