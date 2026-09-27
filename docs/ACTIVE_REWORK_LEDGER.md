# Active Rework Checkpoint

Updated 2026-09-28. Branch: ASTRAReworks. Incoming integration:
`10c0e28e` plus the completed first-person preload unit `d7ad338d`.
Read [AGENTS](../AGENTS.md), [task routes](README.md) and the [queue](TODO.md).
This is current state, not another backlog.

## Current Unit

The incoming integration merges cleanly with the preload unit. Its authored assets,
per-side bounds, shared regression repairs and protocol87 map-index compatibility
are preserved. Before this checkpoint update, the merged index differed from the
incoming tree only by the eleven reviewed preload-unit paths. No source conflict
or manual map alteration. Local native integration/build is NOT RUN: remaining disk
space is below the5GiB launch reserve. The existing per-unit receipts remain valid
only for their frozen inputs, not this whole merged candidate. Continue independent
implementation without retrying a blocked import/build at unchanged headroom.

First-person source meshes now load asynchronously during boot and share a retained
cache with all seven ViewmodelArms lookup sites. Paths follow roster IDs; authored
meshes/materials/poses and missing fallback stay unchanged. One new native case
passes 1/1: 50 retained meshes/50 async requests, exact source identity, repeated
retention and no actor/viewmodel creation. Full base plus five frozen inputs, no
drift or retry. Player hitch/frame timings remain OPEN.
[Evidence](reports/stability-2026-09-27/loading-audit.md#first-person-mesh-preload).

Protocol86 ages generic timed and familiar recovery by remaining-round-clock
progress, not server wall time. Pauses/introduction holds and slow motion therefore
preserve gameplay lifetime; inactive rounds reject nonempty recovery. Four changed
native codec/application cases pass 4/4 on full base plus seven frozen inputs,
no drift or retry. Actual peer timing remains OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#simulation-clock-recovery).

Protocol85 binds live seance recovery to world/body and accepted ultimate identity.
Active duplicates, old and completed phases cannot recreate its field or interrupt
the role skill again. Missing companions remain retryable; authored visuals stay.
Two new native codec/real-companion cases pass 2/2 on full base plus eight frozen
inputs, no drift or retry. Its former wall-clock aging is superseded by protocol86
above. Actual peers remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#scoped-familiar-recovery).

Protocol84 binds generic timed skill recovery to world/body scope, sequence and
both owning ability IDs. Bounded codecs and complete validation precede aged
restoration; valid no-ops close older state and new transport resets receive cursors.
Existing specialized flight recovery and authored skills are unchanged. Two new
native codec/application cases pass 2/2 on full base plus eight frozen inputs,
no drift or retry. Actual peers/ranked/reconnect remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#bound-timed-recovery).

SceneFlow now uses owned asynchronous arena loading on every peer after an initial
curtain frame, with repeated-target deduplication and an explicit observation-only
option. Boot retains the existing loading illustration deck through async reads.
Two new native cases pass 2/2 on full base plus six frozen inputs, no drift or retry:
actual scene loads under offline/controlled network roles, installer/HUD readiness,
retained artwork and pre-load cancellation. Protocol83 unchanged. No real peers,
cold-player timing or hitch-free claim. [Evidence](reports/stability-2026-09-27/loading-audit.md#asynchronous-arena-entry).

Protocol83 scopes READY and countdown messages to a match, rejects non-seated voters,
and holds valid quorum until host loading finishes. Manual votes retry until the
countdown acknowledges them; completed countdowns cannot restart from duplicates.
Two new native cases pass 2/2 on full base plus six frozen inputs, no drift or retry.
Actual peers, ranked and physical input remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#scoped-loaded-readiness).

The retained creator requests a fresh preview before relative scale/dress so repeated
edits cannot compound proportions. Normal player preview reuse stays default and
the retired creator door stays closed. One new native case passes1/1 on full base
plus3inputs,no drift,no retry. [Evidence](reports/stability-2026-09-27/loading-audit.md#mutating-preview-callers).

Fresh handler binding resets received cohort/pending-ultimate state but preserves
host lifetime sequence. Local stop/disconnect cancels ultimate/halftime/arrival
owners and hitstop before normal speed. Arrival iterators use generation/finally
cleanup. Two new native cases pass2/2 on full committed base plus7inputs,no drift,
no retry. Protocol82 unchanged; actual sockets/reconnect/hardware remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#transport-presentation-cleanup).

Protocol82 binds live sentry host masks to accepted ultimate cohorts,including
pre-birth delivery,late bodies,snapshot identity and duplicate isolation. Replicas
never choose local victims or catch; late fresh cue is once-only and restoration
does not replay it. Two new native cases pass2/2 on full committed base plus14inputs,
no drift,no retry. Actual transport/ranked/staged visuals remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#live-sentry-target-delivery).
QA-19's comment row now agrees with its already-published native field-state pass.

Existing grounded introduction work now stages behind match loading before arena
draws,with retained variants and cancellation-safe preparation ownership. Views
wait for model/cache results; known unsupported rigs warn once instead of cloning
each idle frame. Two new native cases pass2/2 on full committed base plus4inputs,
no drift,no retry. No authored motion/assets/map-render changes or player-hitch claim.
[Evidence](reports/stability-2026-09-27/loading-audit.md#explicit-introduction-preparation).

Protocol81 names the committed hero/ability and scopes requests to the body epoch.
Host identity validation,matching-kit preparation and changed-kit execution guard
preserve authored presentation/resource rules. Two new cases plus the changed
duration-wire case pass3/3 on full committed base plus7inputs,no drift,no retry.
Live host-request/peer/ranked qualification stays OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#ultimate-identity-and-body-scope).

Protocol80 carries the host-sealed ultimate cohort duration rather than deriving
it from possibly missing local caster kits. New actual receiver case passes1/1 on
full committed base plus5inputs,no drift,no retry: host5s stays active past local
2.8s fallback,invalid durations/duplicates reject,and terminal identity stays closed.
No authored timing changes; actual peers remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#host-owned-ultimate-duration).

Boot menu failure now exposes one focused pointer-accessible EXIT GAME control
above the retained curtain,using the existing Cancel quit path. New failure-exit
and first original boot activation/handoff checks pass2/2 on full committed base
plus3inputs,no drift,no retry. Actual device exit,physical inputs and visual judgment
remain separate. [Evidence](reports/stability-2026-09-27/loading-audit.md#boot-failure-controls).

First HOME decoder check reproduced a30-second readiness timeout. Explicit
prepare/play until first frameReady,then pause,now completes the same readiness/
adoption/cleanup case in0.3296335s Editor. One failed-case-only retry passes on
full committed base plus1runtime input,no drift. Reduced-motion case passed in
the initial batch and was not rerun. No video/art changes or whole-player timing claim.
[Evidence](reports/stability-2026-09-27/loading-audit.md#decoded-home-handoff).

LOGIN-0927's FIRST native field-state case passes1/1 after a fixture-only colour
baseline timing correction. Runtime was unchanged; required/edit/server-error and
reduced-motion/hitbox checks remain intact. Full committed base plus1corrected test,
no drift. Sound/visual/physical-input acceptance stays separate.
[Evidence](reports/stability-2026-09-27/login-feedback.md).

Protocol79 fixes Paete ultimate Reset destroying every caster's sentry. Exact
fresh references and recovered owner bindings now govern cleanup. New native
two-caster/unused-kit/recovery/reset case passes1/1 on full committed base plus
4inputs,no drift,no retry. No authored attack/visual changes; fresh target
convergence and actual peers remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#sentry-cleanup-ownership).

Shader warmup now checks elapsed time after each variant,yielding at a2ms target
or10variants while retaining full completion. The modified native stage case
passes1/1 on full committed base plus2inputs,no drift,no retry:97variants,10turns,
max1.493ms in this cached Editor run. One native compile may overrun; no cold-player
or handset guarantee. [Evidence](reports/stability-2026-09-27/loading-audit.md#shader-turn-budget).

Title/login artwork and avatar warmups now await cold Resources.LoadAsync requests
per item before retaining their existing texture/sprite caches. Supplied art and
staged order stay unchanged. One new native request/cache/fallback case passes1/1
on full committed base plus3inputs,no drift,no retry. Player timings remain OPEN.
[Evidence](reports/stability-2026-09-27/loading-audit.md#asynchronous-menu-art).

Protocol78 resources use stable hero/ability IDs,world scope,sequence and both
roles instead of mutable skill slots. Validate the whole identity set before
mutation; preserve owner-live anti-refund behavior. Two new native cases pass on
full committed base plus9inputs,no drift,after one test-only CS1657 reader-lifetime
repair. Actual peers/ranked/reconnect remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#ability-resource-identity).

Protocol77 scopes SyncUnit body pose/status to match/round before accepting its
serial. Existing GameplayActionScope replaces the bare epoch; movement-epoch and
resource/status rules stay. The changed receiver case passes1/1 on full committed
base plus4inputs,no drift. Actual peers and QA-15 remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#body-snapshot-world-scope).

CharacterAnimator now owns and releases only the valid avatars it generated for
its binding,after graph disposal,on rebind/clear/teardown. Borrowed/imported avatars
survive. New native real-rig lifecycle case passes1/1 on full committed base plus
2inputs,no drift. No authored animation change or player-heap claim.
[Evidence](reports/stability-2026-09-27/loading-audit.md#generated-avatar-lifetime).

Ability-icon preload now uses asynchronous resource requests and one fallback
bake/upload per yielded turn,including the previously first-use cooldown graphic.
Default-avatar hashing safely handles int.MinValue without changing other defaults.
New native cases2/2 pass on full committed base plus4inputs,no drift; no retry.
Player timings remain OPEN. [Evidence](reports/stability-2026-09-27/loading-audit.md#ability-icon-preparation).

Unchanged ModelPreview selections retain their live model/materials/pose; changed
clip/palette snapshots,prefab/pet or shading mode rebuild,and outgoing subjects
hide before deferred destruction. Null selection clears Subject immediately.
One new native case passes1/1 on full committed base plus2inputs,no drift. Player
timings remain OPEN. [Evidence](reports/stability-2026-09-27/loading-audit.md#repeated-preview-selection).

Protocol76 carries match/round/sequence on requested pause/speed and sends recovery
rate after SyncWorld. Local hitstop stays local; unchanged refreshes preserve it,
and a cinematic hold retains its requested release rate. Three new native cases
pass on the full clean committed base plus10inputs,no drift. Actual peers and
QA-15 remain OPEN. [Evidence](reports/stability-2026-09-27/multiplayer.md#requested-match-clock).

Opaque HOME video/poster now suspends the covered court camera/surface and restores
fallback when needed,without discarding prepared objects. New native case passes1/1
on the full clean committed base plus4changed inputs,no drift. Player GPU/FPS delta
is not measured. [Evidence](reports/stability-2026-09-27/loading-audit.md#hidden-home-render).

Existing particle meshes and catalog-driven status icons now prepare behind
loading. Rooted's omitted live-status entry is restored. Separate new native cases
pass1/1 each with no drift241inputs; the status check launched once after a disk
preflight hold. No effects/artwork/gameplay change or old-case rerun.
[Evidence](reports/stability-2026-09-27/loading-audit.md#remaining-first-use-data).

ToonSkin now caches base/overlay material roles separately. Map-preview resized
targets are destroyed/rebound and detached at teardown. Two new native cases pass
on240inputs with no drift; no art/shading-value change or repeated old tests.
[Evidence](reports/stability-2026-09-27/loading-audit.md#preview-cache-ownership).

Incoming doll v20/art and Voodoo tick/reset/cleanse/passive-speed/HUD wiring are
preserved. New native received-clock/authority case passes1/1 on239inputs,no drift:
client clocks expire without resolving host-only outcomes. Protocol75 rejects older
unwired bodies. Incoming author tests are not counted as extra runs here. Final kit,
doll entity ownership/lifetime and actual peers remain OPEN.
[Integration evidence](reports/stability-2026-09-27/input-integration.md).

Known converted-menu scenes now load asynchronously behind the existing curtain,
await real initialization/layout,and hand the same owner into hub preview setup.
The root surface blocks pointer input; duplicate/stale requests preserve ownership.
New native title/hub case passes1/1 on222inputs,no drift,after one test correction
to sample raycasts after the first rendered frame. No old map-cycle/film rerun.
[Exact evidence](reports/stability-2026-09-27/loading-audit.md#converted-menu-transitions).

Custom-map previews now finish scene/setup/first draw behind the hub's loading
curtain and retain their scenes/look instances. One real-hub native check passes:
five maps,two cycles,zero new loads,0.090-8.241ms Editor selection calls. Preparation
took38.52s upfront; five nonblank views were inspected. Frozen210inputs,no drift.
A one-line menu-heading warning correction subsequently compiled in Runtime only.
[Evidence and limits](reports/stability-2026-09-27/loading-audit.md#custom-preview-loading).
Player frame/GPU/peak-memory measurements and a new player build remain OPEN.
Incoming doll v11-v19 artwork/source,cloth/reference choices,glow/spill shaders and
material helper are preserved across18 paths. Runtime/Editor compile on220 frozen
inputs; no creative edits,unchanged native reruns or inferred art/shader approval.

Sentry snapshots now preserve the host's captured seat mask rather than guessing
from current local distance. Late bodies bind once; restoration never reapplies
the catch. Protocol74,70byte world-field base. New native receiver/restore pass2/2,
no drift on206frozen inputs,minimum free6,369,206,272bytes. No earlier cases repeated.
[Exact evidence and limits](reports/stability-2026-09-27/multiplayer.md#sentry-target-recovery).

Splash audio preparation now loads and retains deferred SFX/voice sample data,
yielding between clips. Native1/1 confirms the original reference-only gap and
prepared samples without playback. Music/import settings are unchanged. The first
UI reuse pass also completed3/3 for range controls,settings and preview resizing.
Both runs used206 frozen inputs with no drift; no earlier cases were repeated.
[Loading evidence](reports/stability-2026-09-27/loading-audit.md) records memory and
Editor slice timing,not player hitch-free acceptance.

Incoming controls,GUID repairs,doll v8/source and Voodoo API remain preserved.
Protocol73 Voodoo snapshots carry explicit reach outcome and preserve resource
correction ordering; native2/2 passed. The new body wiring is integrated above;
new-kit and actual peers remain OPEN. [Network evidence](reports/stability-2026-09-27/multiplayer.md#voodoo-body-snapshot).

DOCS-0927 is published as ce0edc7a; [preservation/media record](reports/documentation-cleanup-2026-09-27/README.md).
No task-owned Unity/compiler/player process is running. Preserve unrelated contributor
changes and protected UI metadata; stage only owned paths. Inspect actual Git state
and current owner reservations after resuming.

## Recent Published Work

| Revision | Change | Evidence boundary |
|---|---|---|
| b394c7ac | Retained roster outline preparation before first skin application | Two native identity/cache/geometry cases pass; player timing not measured |
| cc21bdce | Sentry target masks through world recovery | Two native receiver/restore cases pass; real peers and fresh-cast convergence pending |
| 03f1c741 | Deferred SFX/voice samples prepared behind loading; UI-flow evidence recorded | Audio1/1 and first UI3/3 native cases pass; player timings and visual/physical input remain open |
| de426a0e | Voodoo body status/mark/reach state and explicit outcome | Native codec/receiver2/2 pass; incoming kit gameplay wiring and peers remain open |
| 0a28bf13 | Merge owner controls/GUID repairs with networking/loading work | Four assemblies compile;7/7 new focused native cases pass; actual peers/player timings pending |
| 22f71a9c | Received Rooted restraint ownership, dedup, late facing and cleanup | Included in the7/7 native integration pass; actual peers/reconnect pending |
| e1f21c5f | Coalesced character-preview render-target resizing and invalid-avatar cleanup | Native resize/reuse/capture case passes; visual and player allocation checks pending |
| b93d2a4a | Remove whole-screen settings reflow/chip scans on ordinary value changes | Native reuse/text-size reflow case passes; visual/physical input/player timings pending |
| 2b588964 | Async retained supplementary baked-motion data for roster/body/introduction binding | Native retention case passes; first-use/memory timings pending |
| fe3a93eb | Existing ultimate victim-camera feedback through scoped shared match events | Native scoped receive case passes; actual peers pending |
| ad19d440 | Accepted plant lifetime identity through cast/removal/recovery and bounded retirement | Native retirement/late-install case passes; delayed peers/reconnect pending |
| 70ee0152 | Host-timed leased Interact holds and scoped escape/uproot notifications | Native lease/epoch/host-time case passes; lossy peers pending |
| c90f7ef8 | Destination-owned setup readiness, failure/return surface, cancellation and loading input isolation | Native readiness/cancellation case passes; full physical-input journey pending |
| a6111f72 | Explicit offline range, independent local resources, prepared controls and reusable target bots | Seven Core cases and native control/reuse case pass; visual/physical input pending |
| 6f128705 | Frozen restraint follows received status; dedup, refresh/thaw/disable cleanup; four meshes preload | Native lifecycle case passes; peers and QA-15 pending |
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

Current protocol is defined by NetSession.cs, now75. Matching clients are required.
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

The first native candidate froze186 source/dependency inputs,19 incoming paths over
the previous layer. Subsequent preserved doll/Core/body layers and snapshot wiring
bring the set to206 paths. Six new Voodoo Core cases and two snapshot native cases
pass. Latest separate UI/audio runs pass3/3 and1/1 with no frozen drift; minimum
free storage6,864,703,488 and6,720,446,464bytes respectively. Native runs compiled
their changed inputs; no duplicate direct compiler passes were needed.
[Initial receipt](reports/stability-2026-09-27/checks/input-integration-native.json),
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
