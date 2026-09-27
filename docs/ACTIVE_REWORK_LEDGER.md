# Active Rework Checkpoint

Updated 2026-09-27. Branch: ASTRAReworks. Integrated base checkpoint:
`a6111f72b0c24909685a9f752b3b7e5d46d7f9bf`; generic match-loading handoff follows it.
Read [AGENTS](../AGENTS.md), [task routes](README.md) and the [queue](TODO.md).
This is current state, not another backlog.

## Current Unit

Generic match loading now requires destination-owned setup success, not an eight-
second timeout or an old global round. Same-scene network reloads wait for a fresh
Scene; failures retain an error/return surface instead of claiming readiness.
Replacement/non-match transitions cancel the old curtain and dispose its existing
preparation iterator. Gameplay input clears behind loading and waits for held menu
actions to release on handoff. No map/warmup implementation or protocol changed.
Four assemblies compile; new native lifecycle/input/UI qualification remains OPEN.
[Evidence](reports/stability-2026-09-27/loading-audit.md#destination-setup-readiness).

DOCS-0927 is published as ce0edc7a; [preservation/media record](reports/documentation-cleanup-2026-09-27/README.md).
No task-owned Unity/compiler/player process is running. Preserve unrelated contributor
changes and protected UI metadata; stage only owned paths. Inspect actual Git state
and current owner reservations after resuming.

## Recent Published Work

| Revision | Change | Evidence boundary |
|---|---|---|
| a6111f72 | Explicit offline range, independent local resources, prepared controls and reusable target bots | Five assemblies compile; seven Core gate cases pass; native range/UI/physical input checks pending |
| 6f128705 | Frozen restraint follows received status; dedup, refresh/thaw/disable cleanup; four meshes preload | Four assemblies compile; native lifecycle/peer checks and QA-15 pending |
| a60d3f67 | First-HOME paused decoder/first-frame preparation during boot | Four assemblies compile; native handoff/reduced-motion and timing checks pending |
| 4a9f9cf2 | Correlated combat refusals and authoritative resource correction | Two new Core cases pass; five assemblies compile; native/peer qualification pending |
| 99ce5338 | Remote host resource clocks and held-sprint snapshot continuity | Two new Core cases pass; five assemblies compile; native/peer resource checks pending |
| 8e20a495 | Ordinary action/request/refusal/charge context scoping | Four assemblies compile; one new pure case passes; native/peer cases pending |
| ce0edc7a | Current docs/routes,whole history archives and conservative generated-media cleanup |33active docs link-check clear;55root docs cataloged;9historical bodies preserved |
| 9535fc21 | Login field messages persist; field-only error pulse; pair-credential error clears when either input changes | Four assemblies compile; dedicated native/sound/visual case pending |
| 55aca6cb | Async roster portrait/mode-card preload and one shared retained cache | Four assemblies compile; native cache/first-use timing pending |
| 210cd801 | Boot curtain survives menu activation, construction and layout; hidden input/welcome/music deferred | Four assemblies compile; native handoff/cleanup pending |
| 01346a80 | Shared held-aim body presentation, private target separation, scoped hold closure | Four assemblies compile; one pure token case passes; native codec/peer cases pending |
| 9a44be97 | Ranked/casual compatibility/capacity filtering and failed-allocation backoff/retry | Three managed checks and compile; live queue/party/results pending |
| b7d26bcf | Prepared recovery integrated with incoming Phaister rework; screen-effect lifecycle cleanup | Frozen merged source compiles; native/peers pending |

Current protocol is defined by NetSession.cs, now69. Matching clients are required.
The prior full ledger, including every older feature, contributor record, source
revision and receipt, remains in [the dated snapshot](archive/snapshots-2026-09-27/docs/ACTIVE_REWORK_LEDGER.md).

## Implementation And Qualification

- Networking uses stable ability identities, explicit delivery/command intent,
  independent receipts, typed pose/skill payloads and ability-owned prepared recovery.
  Ranked and spectators share match paths. [Contract](SKILL_NETWORK_CONTRACT.md),
  [source route](NETWORKING.md), [evidence](reports/stability-2026-09-27/multiplayer.md).
- Preserve incoming character reworks; do not tie transport to temporary art.
  Cosmetic swaps should reuse current hooks. New gameplay state still needs an
  explicit contract. Persistent world collections and real peer/ranked/reconnect/
  results qualification remain OPEN.
- Loading now includes progressive shaders, menu art/avatars/portraits, retained
  hero props/effect data and actual menu activation. First-use instance/material/
  GPU preparation and measured player hitch qualification are not all complete.
  [Route](LOADING_AND_PERFORMANCE.md), [evidence](reports/stability-2026-09-27/loading-audit.md).
- [QA comments](reports/stability-2026-09-27/qa-comments.md) retain19 distinct reports.
  Existing focused native fixes stay qualified only for their recorded cases.
  Sean/Cheska freeze and title blur remain open; latest login feedback is compiler-only.
- Other current TODO requirements remain assigned as reconciled against newer owner
  scope. Do not reactivate historical proposals or claim completion from a class existing.

## Validation Environment

Use the task's isolated validation checkout and named profile, not another worker's
caches or the real player profile. Existing native launches hit the disk reserve
before tests; repeated launches at unchanged headroom are not justified. Current
direct Roslyn checks compile Runtime,Editor,Tests and PlayTests but do not run native
lifecycle,rendering,IL postprocessing,actual peers or a player build.

Latest frozen implementation candidate:129 source/dependency inputs,5changed for frozen presentation.
Four Unity assemblies compiled; session33753 ended. Unaffected Core evidence reused.
Receipts: [frozen presentation](reports/stability-2026-09-27/checks/frozen-presentation-compile.json),
[HOME warmup](reports/stability-2026-09-27/checks/home-video-compile.json),
[verb receipts](reports/stability-2026-09-27/checks/verb-receipts-compile.json),
[stamina](reports/stability-2026-09-27/checks/network-stamina-compile.json),
[action scope](reports/stability-2026-09-27/checks/action-scope-managed.json),
[login](reports/stability-2026-09-27/checks/login-feedback-compile.json),
[portrait preload](reports/stability-2026-09-27/checks/menu-portraits-compile.json),
[menu activation](reports/stability-2026-09-27/checks/menu-activation-compile.json),
[aim](reports/stability-2026-09-27/checks/aim-replication-managed.json).
Native cases remain pending; no broad suites or unchanged films repeated.

Earlier broad survey:555 cases,468passed,72failed,15skipped. Do not turn that old
survey into a repair loop or call it current source qualification. No successful
current player build, cross-platform acceptance or complete before/after hitch table.

## Next Action

Continue network/flow correctness and loading improvements after publishing this unit.
Correlated refusals are implemented with native/peer qualification pending;
persistent state coverage and first-use performance remain open. Reuse existing tools and evidence;
make actual fixes, not repeated audits. No subagents or cross-chat delegation.
