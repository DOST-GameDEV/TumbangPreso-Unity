# Game improvement plan

Status: IN PROGRESS. Requested 2026-09-09. Execution entry: TODO section 152.

## Mandate and continuity

Continue the remaining game improvements, including implementation, models,
animation, SFX, VFX, UI and documentation, on `ASTRAReworks` only. The starting
checkpoint is `0028b3a901b8c338538d31251380c6433a7f6aa6`.

The current request supersedes the older one-task stopping rules, usage-sized
batches, animation-only edit limits and blanket UI exclusion in the polish
roadmap. Work continues through the plan in the current conversation, without
subagents or delegation to other tasks. Keep small reviewable commits, but a
commit is a checkpoint rather than a reason to stop.

Read the repository rulebook, VISION and TODO first. This is the durable execution
plan; NATIONALS_POLISH remains the artistic direction, TODO and its archive own
numbered findings, ASTRA owns animation evidence, and Attention owns actual human
judgments. Do not recreate the backlog from historical FUTURE proposals after a
context reset. Resume from the status table and execution record below. Change
the plan only for new evidence or user direction, recording the reason here.

No work on, merge into, or push to `main`. No usage reset. Ask before paid external
work. Preserve user profiles and unrelated checkouts. Controller and touch must
work with every affected feature; do not replace the separately maintained device
mapping architecture as an incidental UI cleanup.

## What better means

The player should understand the opening created by a falling can, track their
slipper, choose ordinary pickup or committed retrieval, and understand why they
escaped or were caught. This repeated sequence gets first priority. Classic
remains a complete street game; Hero Strike makes the same encounter richer
through six recognizable characters rather than more simultaneous clutter.

The improvement targets are:

1. Coherent preparation, contact and recovery across body, first-person arms,
   sound and camera, visible to opponents as well as the owner.
2. Quiet playable ground framed by three distinct Filipino places, with grounded
   characters and readable shoe, can and role silhouettes at normal play distance.
3. Six hero presentations with different motion and material language, one clear
   ultimate peak, and prompt return of attention to the retrieval contest.
4. Predictable menu journeys with a clear primary action, reliable back/focus,
   readable labels and reachable touch targets. An ending that explains the result
   and recalls actual play without becoming another dashboard.
5. Reliable matches and honest release evidence: no duplicate authority effects,
   misleading network state, contaminated test claims or unverified player builds.

The user explicitly wants the game to feel more Filipino and better to play, with
full creative freedom. Express place through the contact of rubber and tin,
neighborhood-specific materials and reactions, character gestures and readable
street rivalry. Decorative symbols alone do not meet that request. The user's
girlfriend is supplying a UI plan; integrate it before major visual or structural
UI redesign. Confirmed usability repairs can proceed while that reference is pending.

Preserve established character identities and skin tones, the logo-derived menu
palette, preferred can recordings, authored asset detail and the Generic rigs.
Changes to art are authorized; that does not make replacement inherently better.
Keep anything that passes comparison. Balance changes require a demonstrated rule
defect or an explicit gameplay judgment, never bot usage alone. No new modes,
heroes, progression systems or controls are implied by this polish request.

## Execution order and acceptance

`pending` means not verified at this checkpoint. `kept` requires evidence just as
`changed` does. A blocked item names its dependency while independent work continues.

| ID | Status | Outcome and work | Acceptance / evidence |
|---|---|---|---|
| P0 | in progress | Establish current branch, tools, baseline and reconcile open claims | Exact starting SHA, clean isolated checkout, required reads, discovered test partition, current failures separated from historical ones |
| P1 | pending | Make the core retrieval sequence coherent | Successful and failed slides at full speed; entry/contact/recovery and possession alignment; owner and remote authored body; female-a geometry identified; keep passing clips |
| P2 | pending | Repair confirmed release and correctness defects | TODO 151.6 hazard bound, 151.18 roster orphans, 151.21 probe subject, 151.15 audio reach, 149.4 duplicate requests; targeted behavioral evidence and required audits |
| P3 | pending | Make menus and match state understandable | Reproduce touch/result obstruction and surviving layout failures; fix relevant layers; review boot/play/settings/loadout/pause/results journeys; network feedback and one useful highlight reader where current paths lack them |
| P4 | pending | Establish Eskinita's visual and sonic reference | Three ordinary gameplay viewpoints and spectator view; coherent throw/tin/retrieval/chase with OST; comparable before/after views, stable can/shoe/role readability |
| P5 | pending | Finish Sean's complete hero presentation | Three live casts in body and FPP, effects and sound; custom kit inheritance; accepted/refused/interrupted and remote playback; ultimate staging contract selected from actual working hooks |
| P6 | pending | Carry distinct finish across the remaining heroes | Zack, Dante, Cheska, Nemu, Phaister reviewed separately; correct visible defects including Zack's ground star; matching body/FPP timing and live ultimate path; no mandatory reauthoring of passing clips |
| P7 | pending | Finish Bayan Plaza and Ilalim presentation and investigate route anomaly | Distinct lighting/depth/material treatment, ordinary/reverse views; recorded Ilalim seed traced; no speculative collision or balance edits; sourced jeepney preserved |
| P8 | pending | Connect start, role changes, final action and ending | No stale effects or stranded input after interruption/rematch; existing victory/audio path verified; useful recorded highlights subordinate to result; observer/replay framing checked |
| P9 | pending | Close remaining actionable queue work and documentation drift | Review every current implementation row; 149.5 lookup cost measured before caching, 151.9 clocks/lunge/outlier investigated, 149.7 removals justified by stronger coverage; archive complete sections whole |
| P10 | pending | Qualify and deliver the resulting game | Core, EditMode, isolated PlayMode gate twice, editor checks, required source audits; clean Windows build and exact-player smoke/multiplayer evidence; Android build if installed toolchain supports it; push branch |

Correctness blockers found at any stage move ahead of visual polish. P2 and P3 can
proceed while an asset inspection or baseline run is in flight. Never edit C# or
assets being imported while the same checkout's Unity run is active. Read, assess
evidence and update documentation instead. Use one Unity process per project.

### P0: establish facts before edits

- Fetch first and inspect local changes. Use an isolated checkout at the fetched
  branch head. Confirm editor version from ProjectVersion.txt and installed tools.
- Read the latest rules and queue rather than the older local desktop checkout.
- Inspect NATIONALS_POLISH, ASTRA task 3/6/8, Attention 17.2/18 and archived
  151.22/151.23. Correct stale claims only with an explicit current-state note.
- Discover the gate groups with `tools/playmode_suite.py --plan`; run appropriate
  baseline coverage. A single-process aggregate is diagnostic, never a release gate.
- Inventory existing player capture, network, render and audio tools before adding
  narrowly necessary probes. Do not build a general capture framework.

### P1/P4: core sequence and sensory priorities

Review throw wind-up, release, bank/bounce/rest, knockdown, ordinary pickup,
successful/failed slide, missed lunge, tag and recovery as connected actions.
Sean's committed strip and dedicated FPP slide are the baseline, not missing work.
Compare contact samples at 0.140, 0.250 and 0.342 seconds without turning those
review samples into new gameplay timers. Identify female-a's floor contact from
mesh ownership and visible geometry before deciding whether to edit it.

Separate the semantic sound of slide, shove and incidental shop reactions where
the current recording implies the wrong action. Retain preferred tin sounds.
Favor clearer attack/decay and less masking over greater volume. Review camera
distance and recovery; no forced live-player cut, aim displacement or longer
global freeze. Human taste approval remains separate from technical correctness.

### P3/P8: user journeys and result hierarchy

For each changed screen, write its primary purpose, first press, hidden secondary
information and one-press escape. Use existing MenuKit/ConvertedScreen, PaperKit,
UiRows and ScreenFocus. Check actual background and always-on chrome, boot and
nested states, short wide window, narrow desktop and phone shapes. Validate mouse,
controller focus/submit/back, and actual raycast targets for touch.

Investigate ResultCanvas being covered by LookArea as a concrete first target.
Check current code before resurrecting the older missing-label/inventory reports.
For network state, tell the player what is happening and what they can do without
inventing reconnect or forfeit policy. If highlights have no reader, place a small
truthful selection under the result, not over live play; never award extra score.

### P5/P6: hero and model direction

Review Sean's three casts first, then each remaining hero. Keep the established
motion language: Sean propels, Zack snaps/carves, Dante plants, Cheska draws/holds,
Nemu drifts/pulls, Phaister performs. Resolve ultimate presentation through the
existing accepted-cast flow where possible. An unreachable cinematic asset is not
progress. No extra input lock, altered hit timing or longer shared freeze.

Model edits address visible silhouette, clothing overlap or material separation
at gameplay distance. Preserve identity and sourced detail. Every changed GLB gets
new versioned in-engine views, clip/import validation and roster regeneration when
references change. Review custom rigs that inherit hero kits as well.

### P7/P9: map, simulation and engineering work

Eskinita is intimate neighborhood warmth; Bayan Plaza is open civic space; Ilalim
is bridge shadow and selective shop light. Use existing builders and shaders.
Compare thrower, retrieval, defender reverse and spectator views. Do not substitute
an isolated beauty shot for ordinary play. No new hazards or blanket asset repaint.

Trace Ilalim's recorded idle seed and log resting slippers before changing routes.
Measure round, defence and simulation clocks across hitstop before touching scores.
Separate taya lunge aim from balance with actual decisions at real time. Benchmark
scene lookup cost before introducing caches; inactive taya slippers have distinct
visibility semantics that must survive any registry. Inspect one-shot requests
for reachable duplicate effects, with focused tests rather than a protocol rewrite.

## Verification and completion rules

- Save before/after evidence with new filenames, commit/working state, view, map,
  mode and measured question. Use the real Unity look for visual decisions.
- Run focused tests for new behavior, then required Core, EditMode, PlayMode group
  gate, Checks.RunAll and authority/audio/relay/presentation/request/wire audits.
- Assert XML is fresh, contains the intended cases, total is nonzero and failed is
  zero. Do not dismiss a repeatable isolated failure as aggregate contamination.
- Keep a baseline defect separate from a regression but investigate actionable
  baseline failures. Do not lower readability bounds or omit fixtures to go green.
- Confirm the 12% white-frame ceiling and visible gameplay objects for VFX edits.
  Record warm performance as a machine observation, not a handset certification.
- Build after the implementation batches. GameBuilder purges only its validated
  previous player output. Verify new files and launch that exact executable.
- Exercise local Classic/Hero Strike and real host/joiner or observer paths relevant
  to changes. Physical pad, handset, venue acoustics and human feel are unclaimed
  until actually performed.
- Update TODO in every work commit. Archive completed numbered sections whole with
  index pointers. Sole-authored commit messages go through `git commit -F`; no
  tooling attribution, coauthor trailer or em dash. Push only ASTRAReworks.

## Execution record

- 2026-09-09: fetched origin from the existing repository. Remote ASTRAReworks is
  the requested `0028b3a901b8c338538d31251380c6433a7f6aa6`. Created a clean isolated
  worktree at `work/TumbangPreso-Unity`; left the existing checkout's three modified
  ProjectSettings files untouched. Unity 6000.5.8f1 and Blender are installed.
- 2026-09-09: current user direction expands art, engineering and UI scope and
  explicitly forbids subagents. This plan replaces the old stop-after-one workflow.
- 2026-09-09: Filipino identity and tactile play are explicit priorities. Major UI
  design waits for the forthcoming supplied plan; independent game work continues.
- Next: complete baseline/tool inspection, inspect committed slide evidence and
  current code, then work P1/P2/P3 from observed failures. No implementation or
  fresh test/build result is claimed by this initial planning checkpoint.
