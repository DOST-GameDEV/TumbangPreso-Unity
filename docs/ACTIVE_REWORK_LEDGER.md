# Active Rework Checkpoint

Updated 2026-09-27. Branch: ASTRAReworks. Integrated base checkpoint:
`0a28bf13` plus incoming `d42f6e7e`; doll asset integration follows those parents.
Read [AGENTS](../AGENTS.md), [task routes](README.md) and the [queue](TODO.md).
This is current state, not another backlog.

## Current Unit

Incoming owner keybinds,shared Shove/Lunge,mouse-wheel glyphs and Phaister v3 plan
are merged with loading/host-hold/status work. Two incoming script-GUID repairs
are preserved. Four assemblies compile and one guarded native pass completed7/7
focused state/loading cases with no frozen-input drift. These cover the new
root/freeze/plant/hold/victim-feedback/loading-readiness/motion-data paths only.
Protocol remains72. Actual peers,player performance and other native UI checks
remain OPEN; no new authored kit or map work is included.
[Evidence and scope](reports/stability-2026-09-27/input-integration.md).
Phaister's incoming doll model,v5 source and review tool are also preserved unchanged.
The Editor consumer compiles; no runtime change or native-case rerun was needed.

DOCS-0927 is published as ce0edc7a; [preservation/media record](reports/documentation-cleanup-2026-09-27/README.md).
No task-owned Unity/compiler/player process is running. Preserve unrelated contributor
changes and protected UI metadata; stage only owned paths. Inspect actual Git state
and current owner reservations after resuming.

## Recent Published Work

| Revision | Change | Evidence boundary |
|---|---|---|
| 0a28bf13 | Merge owner controls/GUID repairs with networking/loading work | Four assemblies compile;7/7 new focused native cases pass; actual peers/player timings pending |
| 22f71a9c | Received Rooted restraint ownership, dedup, late facing and cleanup | Included in the7/7 native integration pass; actual peers/reconnect pending |
| e1f21c5f | Coalesced character-preview render-target resizing and invalid-avatar cleanup | Four assemblies compile; native DPI/resize/visual and player allocation checks pending |
| b93d2a4a | Remove whole-screen settings reflow/chip scans on ordinary value changes | Four assemblies compile; native transaction/layout and player timings pending |
| 2b588964 | Async retained supplementary baked-motion data for roster/body/introduction binding | Four assemblies compile; native retention and first-use/memory timings pending |
| fe3a93eb | Existing ultimate victim-camera feedback through scoped shared match events | Four assemblies compile; native camera/receive and actual peer checks pending |
| ad19d440 | Accepted plant lifetime identity through cast/removal/recovery and bounded retirement | Four assemblies compile; native lifetime and delayed-peer/reconnect checks pending |
| 70ee0152 | Host-timed leased Interact holds and scoped escape/uproot notifications | Four assemblies compile; native and lossy-peer hold checks pending |
| c90f7ef8 | Destination-owned setup readiness, failure/return surface, cancellation and loading input isolation | Four assemblies compile; native loading lifecycle/input/UI checks pending |
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

Current protocol is defined by NetSession.cs, now72. Matching clients are required.
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

Use the isolated validation checkout and named profile, not another worker's caches
or the real player profile. Earlier native launches hit the disk reserve. After the
incoming metadata repairs, one guarded changed-candidate run completed7/7 native
state/loading cases with a6,767,398,912-byte minimum free-space sample. This establishes
that focused native work is possible now, not that a player build has enough space.

The native candidate froze186 source/dependency inputs,19 incoming paths over the
previous layer; no frozen-input drift during execution. The asset-only follow-up
adds8 paths,194 total, and compiles its new Editor consumer. Four native-candidate assemblies
also compile directly. [Native receipt](reports/stability-2026-09-27/checks/input-integration-native.json),
[case XML](reports/stability-2026-09-27/checks/input-integration-native.xml) and
[exact scope](reports/stability-2026-09-27/input-integration.md). Earlier individual
compiler/managed receipts remain in the network/loading reports and their checks
directory; do not repeat their unchanged cases or equate compilation with runtime.
The guard restored the named-profile files/shared input preferences. No active job
remains. Recheck headroom before any new heavy job and stop at the existing reserve.

Earlier broad survey:555 cases,468passed,72failed,15skipped. Do not turn that old
survey into a repair loop or call it current source qualification. No successful
current player build, cross-platform acceptance or complete before/after hitch table.

## Next Action

Continue network/flow correctness and loading improvements after publishing this unit.
Correlated refusals are implemented with native/peer qualification pending;
persistent state coverage and first-use performance remain open. Reuse existing tools and evidence;
make actual fixes, not repeated audits. No subagents or cross-chat delegation.
