# Active TUMP rework ledger

## Current resume, world VISUAL-1, 2026-09-23

**Exact working state after owner sleeps:** current HEAD/remote
f018173c4320c65028d7bcdafd4beeae1f1c0a5d (UX-1 intake only, published/verified).
The world1.1 code below is LOCAL, UNCOMPILED and UNVERIFIED. No Unity/native job,
server or browser preview is active. Do not redo the required document reads,
fetch/pull or UX intake. Do not resume older accessibility/HUD work from history.

Authored pending1.1 files: WorldCueProfile + Editor/WorldCueProfileAuthor (creates
one authorable default resource only if absent); CourtSignal.shader;
CourtBoundaryPresentation (four exact rule edges, sampled heights, off restores
recognised authored Chalk boundary renderers, camera-local exit, epoch/round-safe
escape); CourtEscapePuff (inked billboard); AudioDirector.CourtDanger partial and
AudioDirector hooks; AudioCues court_escape alias; MatchInstaller install;
RecordedWorldView shared can-state boundary and recorded escape cues, no local exit.
The profile.asset/metas do not exist yet; Unity's author generates them on first run.
Do not claim these features finished without compilation and inspected captures.

Next action: finish a small WorldCourtCueTests fixture using actual scene/motor/can
state, capture same-camera off/on rest/armed/exit/restore frames, and test escape vs
teleport/round reset plus replay independence and muted personal audio. Record the
single question/stopping condition BEFORE launching guarded Unity in background.
No.cs edits during it. Inspect normal and25percent-grey captures, with960x540 and
reduced/high-contrast/HUD120 settings; at most3subjective variants. Restore ONLY
known run-generated churn after checking pre-run state. Then update TODO1.1, this
ledger and findings in the same scoped commit and proceed to1.2. No native build.
Original2PNG-meta hashes: Logs/visual1-world-protected-metas.json; never stage them.

Owner explicitly asks for compaction-proof records while asleep. Maintain this
exact-state block at each meaningful checkpoint, with job handle/output paths when
running, honest test/render limits and the immediate next action. Old sections
below are history unless consistent with this current block and the latest queue.


Owner addition: UX-1 front-end brief/sketches saved in TODO and
reports/front-end-flow-2026-09-23; all images preserved in ArtSource. Three pasted
briefs are identical. Latest direct correction: DO NOT TOUCH LOGIN OR MAIN MENU;
new Home opens from existing TAP TO START. This overrides the brief's replacement
entry flow. Queued P5.5 before backlog/final gate, with its own five-stage order.
Continue current world VISUAL-1; no in-game HUD redo, no task deletion.


Fetched and pulled first: local/remote090d8c4af3fbcf79b21fe6dfe8bb9837f353c492.
Only the original home-court.png.meta/loading-street.png.meta are dirty at intake;
never stage them. Required AGENTS,VISION,TODO queue/VISUAL-1,ledger,implementation
plan,findings and NATIONALS VISUAL-1 design have been read in the owner's order.
The completed in-game UI/HUD pass is excluded. Start1.1 world boundary/exit/escape,
then1.2 world target/restore cues,1.5 leftovers,1.7,B,P3,C,D,P6,P7. No current Unity
or native player process at intake. Latest owner goal supersedes the plan's old
per-batch build instruction: NO intermediate builds; native/full regression at P7.
No.cs edits while Unity runs. Use only guarded Unity with the named profile.

Current implementation design: exact square from ConfinementRadius; replace only
recognised authored Chalk boundary renderers while enabled (restore at off/teardown),
retain throwing/home marks and all collision. Sample court heights once, including
Ilalim's raised kerb, so a flat new line cannot disappear below it. Shared rest/armed/
restore sweep; local viewer's closest exit only. Observe real boundary crossings with
MovementEpoch/round guards so tags, teleports and role resets cannot fake escape.
Existing AudioDirector owns a quiet filtered air layer and the soft escape swish.
Per-viewer cues stay out of replay; recorded can flags drive its shared boundary.
A small authorable world-cue profile supplies off values; no new HUD framework.

Before the focused Unity run, record its exact question and stopping result here.
Batch the completed1.1 source changes; inspect same-camera off/on states and25percent
greyscale images before moving to1.2. No unrelated fixture repairs or widened suites.

## Previous session resume, 2026-09-23

Docs cleanup published on top of a1110a9e (docs only, no code). TODO.md is the queue plus
an index row per numbered entry; open bodies are in TODO_Backlog.md; superseded plans are
in docs/archive/ (old-path table in its README); docs/README.md indexes every file.
Priority was rethought: P0 finish the in-flight 134.10 reduced-effects link (the dirty
source listed below), then VISUAL-1 batch A (communication), batch B (world), remaining
expansion, batches C and D with existing scope, backlog disposition, final qualification.
Owner 2026-09-23: Codex is not editing this branch; this session took over the dirty
134.10 work. P0 is published: reduced-effects link incl. camera shake, EditMode 19/19
(Logs/visual-p0-editmode-v1.xml), Core 615/615, native v57 accessibility 15/15. Open:
the v57 chat clip re-check and the 0xC0000005 shutdown exit (TODO). Next: VISUAL-1.4 HUD
and 1.18 icons, then 1.6, 1.1. The owner widened scope to every in-match UI surface.
Design: NATIONALS_POLISH.md, VISUAL-1 section. Research: reports/visual-research-2026-09-23/
findings.md. Build plan, tooling traps and remaining steps: implementation-plan.md beside it.
VISUAL-1.4 first slice published at 4d85395c (3/4 HUD cases; the red is the pre-existing
round-swap NextRole floor fault owned by 1.17). Second slice (feed pictograms, score pops, hit
mark, toast brush, HUD area 3.72/5.30 percent, owner window captures) in look-1.4-v3, 6/7 with
the same NextRole red. Owner 2026-09-23: this session's scope is the in-game UI and HUD only;
everything else (world, lighting, court, viewmodel, effects, maps) is handed to the next
agent in a chat handoff. Batch A HUD (1.18, 1.6, 1.1 frame, 1.3, 1.5 HUD parts, 1.17, 1.4
accessibility captures) in look-batchA-v4, tests 8/8. 1.16 match end and the 1.5 tint priority in look-1.16-v1 (10/10 with HUD and
exchange). This session's UI and HUD scope is complete apart from an optional prompt
keycap (draft idea only). Owner: no build this pass. Everything else is in the handoff.


## Mandate and source

Complete EVERY autonomously actionable unfinished TODO in this conversation.
Never stop/finalize/close the active goal at a build, publication or phase checkpoint.
No subagents, other chats, paid APIs/CLI services, resets, main edits, Desktop
replacement, destructive cleanup or questions while owner is AFK. Preserve task
IDs, original dirty work, models/builders/rigs/GUIDs, profiles and failed evidence.
Quality matters; avoid verification/tool-repair loops. Once a changed risk passes,
return to remaining implementation. Full regression follows integrated features.

DEV C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks,ASTRAReworks.
Last publisheda1110a9e6b583e787f6efe0d508755efb3469d03,remote verified. Accessibility batch
published on top of74ac6e9a (black UI outlines/standing AGENTS rule). Raw failed
XML whitespace is preserved; authored-source diff check passed. Preserve/exclude original unrelated
Resources/UI/composition-redesign/home-court.png.meta and loading-street.png.meta.
Never blanket add/reset/clean. Fetch before push; verify remote afterward.

## Newest owner rule: BLACK in-game UI outlines

UiTheme.InGameOutline is black across live HUD, spectator controls, replay/training,
notifications, ability prompts and match chat. Meaningful fills/art/models retained.
Native v55 colours inspected. Its apparent held-item spectator failure is resolved:
that screenshot shows RETRIEVING/empty hands and a dropped shoe. Another bot had
disarmed the fixture. Re-establishing actual held ownership before the assertion
fixes the diagnostic. Native v56 now passes the unchanged real visibility assertions,
release/re-equip/restoration, autopilot/manual/bookmark/pause and replay at960x720
and1680x720. Input/profile preservation passed. No game renderer rewrite was needed.
Reports: black-ui-outlines.md and accessibility-completion.md under
reports/full-backlog-2026-09-21. Keep failed v55 evidence with its scope correction.

## Current feature:127.3 accessibility

Implemented opt-in toggle sprint/can restore, FPP FOV75..110(original95), HUD scale
100..120%, larger settings/HUD text, high contrast, reduced particles/flashes and
English captions for8delivered announcer IDs, including muted announcer. Same input
maps/intent/network protocol50; defaults unchanged. Real restore check exposed a
solo handover bug: bot seats could lack PlayerInputReader. Assign now creates one.
Settings rows reflow beneath labels; previews/discard keep touch target floor.
Training, match chat and replay labels scale around anchors. All3ability descriptions
stay visible; larger reference fits its longest real body text. Last tiny divider
fix anchors its vertical rules to panel bounds instead of leaving fixed374height.

Taya annotation: old same-frame greyscale showed the hole covered by feet. Only
its floor marker now uses a wider8-sided ring at1.95capsule radius; attackers keep
1.375disc. Eight-metre native/greyscale-v3 shows open angular front vs filled disc.
No character geometry, rigs, physics, supplied artwork or original builders changed.

Evidence (all jobs reaped):
- Core toggle6/6,9ms. Unity sprint and FOV/discard passed in initial2/3; actual
  restore start/cancel/finish passed6.311s,v4,guard05b0136db61e.
- Settings/HUD layout/discard2/2,5.598s,guardabf16841b173. Legacy/default/bounds9/9,
  .0789s,guard8369deb55e89.
- Reduced status particles passed. Muted real announcer and role frame2/2,6.212s,
  guardde021d9ff768. Wider ring native capture3.648s,guard79f8fa115c53; grey inspected.
- Seven actual hero-name/21description large-reference assertions6.504s,v4,
  guard7edba0fd7ebe. Compact Rafi/Phaister frames inspected; then simple divider
  anchoring touch-up made. STOP repeating this layout check. Native overlay gate
  below can capture that final cosmetic alignment.
- Native Windows v56 built1211MB/85s,guard269227d5d75b,job98670. Actual spectator
  reviewjob6694 passed all stages; no owned player remains. v56 precedes the final
  dynamic reference-height/divider refinements, which remain local source.

Failed/misleading receipts preserved: protection precondition; missing local reader;
hidden VoiceDirector found via GameServices, not normal object search; touch168row
floor ignored by early assertion; reference v1 falsely labelled Zack captures for
all heroes because Hud.Tick overwrote fixture text. V2 hid its own canvas. V3/v4
use the actual UI builder on a dedicated test canvas and assert the displayed kit.
Only v3/v4 prove seven-hero copy. These are not full gameplay/build qualification.
Portable evidence and exact limits: reports/full-backlog-2026-09-21/accessibility-evidence
and accessibility-completion.md. Full TODO and127.3 remain ACTIVE.

## Next order

1. Accessibility implementation/evidence published and remote verified. Continue.
2. Finish the bounded native accessibility integration: actual larger settings and
   owner HUD, settings controls/save/discard, training/match-chat/replay label views.
   Reuse current native runner infrastructure; avoid repeating unrelated113screens.
   Latest source must first reach validation, including final reference divider.
   No new native accessibility-specific route exists yet. Current runner supports
   --spectator-only but not --accessibility-only; do not invent a working flag.
3. Continue253unreviewed numbered TODO headings; all366preserved. Newly reconciled
   128/128.1/128.2,138/138.1/.2/.3/.5,139/139.1..7; original138.4 retained. LastTsinelas
   and network map ballot already exist; do not rebuild them from stale128.2 text.
   Old4-page settings is superseded by current5sections; human acceptance remains.
   Dispositions: reports/full-backlog-2026-09-21/todo-disposition.json.
4. Complete remaining local requirements, then coherent whole-source qualification
   and final build. Core604checkpoint plus later5peer-intent/6toggle cases is NOT a
   newly run615case full suite. New partial wire handlers need audit coverage.
5. Source hygiene still matters: editor/importer whitespace and generated animation
   outputs can dirty build inputs. QualityLevelStamp fixes old AA write-through,
   not every new provenance case. Never weaken dirty stamps or claim a clean build.
6. Exact owner valid-mark export and OAuth are external: resource absent and
   TUMP_GOOGLE_CLIENT_ID absent in Process/User/Machine,checked2026-09-22. UGS Rafi
   mastery source updated; live deployment unperformed. Hardware/WAN/human listening/
   balance/art acceptance are not inferred. Continue independent work first.

## Workspaces and preservation

Validation sibling TumbangPreso-Unity-validation:7c028dda +recorded v54,black outlines,
C1..C4 and overlay inputs/editor output. Final dynamic reference height present;
last divider anchoring change still only DEV. No active Editor/player/helper job,
preview server or task-owned browser tab. Unity6000.5.8f1,Built-in renderer.
Every Editor via run_unity_guarded.py and named profile; current
presentation-validation-20260921. Use graphical EditMode,never-nographics.
Every build MUST pass a fresh internal -buildOutput; default targets Desktop.
Freeze inputs only in the workspace currently running a job. One heavy workload.

Preserved stashes/inputs,do not discard/apply blindly:
-6f716645127f374ead0a910c4a90d49cf4cb191b:6432old mixed paths,db976126.
-Logs/qualification-2e90-source-mutations.json:8path mutation stash.
-1ab3cba5fedb6d12f161bc7afe3c57c00c8e86e8:123scoped qualification/author paths.
-Logs/peer-departure-compile-stash.json:inputs before7c028dda.
-Validation Logs/accessibility-c1-input-backup,accessibility-c2-input-backup,
 accessibility-c3-input-backup and accessibility-overlays-input-backup.
Only four new Rafi swimming/recovery asset/meta paths were recovered earlier;
old-rig animation diffs with0text delta are importer/stat churn,not model changes.

## Retained character direction

Rafi is HERO-only,index6,blocky ten-box cropped hair,no eyebrows,no gills.714012byte
GLB/.744m. Body/arms/cord/accessories/rig/33base curves and12protected inputs unchanged
by the hair pass. Actual copied builder tools/build_rafi_voxel.py,original preserved.
Native part/head/HERO lineup reviewed; rejected drafts archived. Own indigo UI,
4Bilis/2Lakas/4Tatag,three serialized casts plus base33clips; correct nativeFPP arms.
No other character design edits. See rafi-block-hair.md and rafi-parts-and-motion.md.

## Other completed checkpoints retained

Core604/604 at previous integration checkpoint; five new intent cases now added.
Initial clean2e90 failures preserved. Focused repairs40/43, then32/36, remaining4/4;
settings lifecycle5/5,37.159s. All those cases are resolved without assertion loss.
Rafi dance now ships; Kuro idle clips match retained original calm geometry;
pre-Awake model binding fixed;7heroes/21abilities/8rounds and copy limits accurate.
Rafi stats4/2/4 avoid duplicate Zack4/3/3. Rafi UI accent indigo avoids role/Cheska
colour ambiguity; physical model/water palettes retained. Audits handle partial
teardown, multi-write lines, Resources shaders and actual validation delegates.
Six diagnostic startup pairs refuse tournament mode. RafiExpansionProbe belongs
to match PlayMode group; partition plan passes. Final coherent qualification pending.

Rafi v52 owner8/observer8/shared ultimate/replay and3delayed peers proved actual
three serialized body-cast clips and exact FPP source arms. Latest hair correction
preserves those body/clip bytes. Lagoon8connected/10detached stilt homes, boats,
residents,islands/mountains/animated water/sky/recovery; nativev47 both modes/quality,
v49water shader fix and deck live-v3 thin-board outline repair retained. Older four
maps19families/2686renderers/nativev39; sky/plainInday armsv37; UI113pass/1UGSskip,
nativev41,338capabilities mapped and96variant/role cases. Recall actual5peer v49c
both modes. Request/rehost/outage evidence retained. No WAN, physical-device,
human listening/balance/art approval is implied.


Detailed prior state is retained in reports/full-backlog-2026-09-21/
ledger-before-accessibility-publication.md and earlier ledger snapshots. Their
active-job lines are historical. Use THIS ledger for current state.

## Current next gate, after a1110a9e publication

New bounded native --accessibility-only route is authored locally in
OwnerUiPlayerReview.Accessibility.cs and existing runner dispatch; its new
-tp-accessibility-review-only flag is documented in TournamentPreset.NotModifiers.
It uses shipped focus-following scrolling and actual raycast/click controls for
larger text, HUD size, hold/toggle choices, FOV, captions/contrast/effects, then
save and actual discard dialog. Captures owner HUD/caption and training; reuses
actual spectator/replay route with larger preferences. No scene/UI feature rewrite.
Transfer this route, runner, core flag list and final divider tweak to validation;
build fresh internal accessibility-v57, then run this bounded route. No job started
yet. Stop testing this gate after it passes and resume253unreviewed TODO entries.

Native v57 built1211MB/73s,guard2aa2d43a9ec6. ACTIVE bounded --accessibility-only
runjob62773,process27908,Logs/accessibility-native-v57. Native input/profile guard
owns cleanup. Validation source frozen for now. New source discovery in134.10:
its explicit contract includes SkyEvent,Hitstop and ultimate cards, beyond the
new particle/vignette consumers. DEV now needs that shared reduced-effects link;
keep shared ultimate phase timing/authority unchanged, retain tells, attenuate
world/body/replay flashes consistently, suppress optional micro-hitstop/camera
motion, and retain default behavior. This extension is not in nativev57. Finish it
without restarting unrelated whole-game checks. Existing253unreviewed count intact.

## Focused1.1 run, look-1.1-v1

Question: do the complete world court cues follow the actual square/can state,
camera ownership and physical escape (excluding teleports), stay on all five map
surfaces, and keep replay state/audio independent of the current viewer?
Stopping result: three fresh WorldCourtCueTests cases pass, shader supported,
then personally inspect the same-camera off/armed/rest/restore captures and their
25percent greyscale thumbnails plus owner-exit frame. No unrelated test repairs
or broadened suites. Own failures get corrected; unrelated/environmental failures
have at most one retry and are recorded. Current source hashes saved in
Logs/look-1.1-v1/source.json. Known4generated files clean before launch; original
2meta bytes backed up under protected-inputs. Expected XML:world.xml, log:world.log.
Unity is about to launch in the background in ASTRAReworks, named profile
presentation-validation-20260921. Freeze.cs edits until its actual handle completes.
No native build. Profile author may create WorldCueProfile.asset and Unity metas.

look-1.1-v1 ended:3failed/0passed,all blocked by OUR new MonoBehaviour field
initializer creating MaterialPropertyBlock. No visual evidence accepted. Moved
native allocations into Build/Play for both boundary and puff; no assertion change.
Run48239 reaped,guard2c9e8bed583b. Known4churn files restored;2protected meta bytes
verified/restored from pre-run backups. Profile asset/metas now generated by Unity.
Next run keeps the same question and3cases: only verify this corrected1.1 batch,
then inspect its fresh pictures; expected output look-1.1-v2/world.xml and world.log.
Freeze.cs edits during the next guarded run. This is an own-code repair, not an
unrelated fixture retry. No native build, no broadened suite.

look-1.1-v2:2/3passed,9.359s,guard0db79aa439a5,run25810reaped. Physical escape,
teleport exclusion and per-camera exit passed. The off check found our new renderer
only disabled during camera culling; it now updates enabled in LateUpdate as well.
New fixture now uses the real ready countdown, current native HUD capture helper
for the owner frame (old generic helper clipped the unrelated HUD), and requires
audible live danger before checking replay silence. Existing fixtures/assertions
were not weakened or repaired. v2owner frame inspected but not accepted as final.
Known4churn files restored and2protectedmeta bytes retained. v3 uses the SAME
question/3cases plus that stronger audio precondition; stop after pass/inspection.
Outputs Logs/look-1.1-v3/world.xml,world.log,shots. No.cs writes during run.

look-1.1-v3 ended2/3passed,62.843s,guardd2ccff328a0a. All5maps geometry/state
and20fresh frames passed; replay with actual audible-precondition passed. Inspected
all20grey25 thumbnails plus full Bayan/Ilalim armed frames. First look too subtle
at thumbnail scale. Authored edge names confirmed correct (no prefix repair needed).
Variant2 widens the visual strip only (.32m envelope), narrows/dims rest chalk,
stronger armed ink/chalk; rule centre and collision stay unchanged. Limit3looks.
Escape test moved during BeginSpawnSettle, which deliberately pins the body for
SpawnSettleFrames; new fixture waits that real guard. Code review also found
MovementEpoch is network-only, so offline teleports need a presentation-only serial
incremented by BeginSpawnSettle. Escape now checks both. Test waits beyond the escape
cooldown before a teleport so it cannot falsely pass through cooldown suppression.

Next v4 question: does the clearer strip replace exactly4authored edges on all5maps,
and does a real settled crossing trigger once while later teleports do not?
Run ONLY the2changed cases; retain the v3replay/audio pass. Stop at fresh2case pass
and inspected same-camera/grey25 images; no native build. Output look-1.1-v4.
Known4churn files already restored; protected2metas verified. No.cs edits in flight.

look-1.1-v4 ended1/2passed,13.098s,guardffa068e924c1. Settled physical crossing and
later offline teleport rejection PASSED. Exact-edge replacement failed:0found.
Actual scene YAML shows Mat_chalk batches; the old editor builder's separate node
names were not the shipped representation. Runtime now recognises chalk batches,
removes only triangles on the four rule-edge strips from a render-only mesh clone,
retains all non-boundary/home/throw triangles, preserves materials and colliders,
and restores the original renderer at off/teardown. No source mesh asset mutation.
AuthoredBoundaryCount now counts the four logical edges, not renderer containers;
the4edge assertion is retained. Variant2 widths unchanged (no third look yet).

Next v5 question: are all four actual batched edges replaced while retaining other
chalk, with the already-chosen stronger rest/armed distinction on all5maps?
Run ONLY CourtMatchesAllFiveMapsAndCapturesRestArmedAndOff; retain v4escape andv3
replay/audio passes. Stop after fresh1case pass and inspected20frames/grey25 sheet.
Outputs Logs/look-1.1-v5/world.xml,world.log,shots. No.cs writes during run, no build.
Known4churn restored afterv4;2protectedmeta bytes retained. Previous failures stay.

look-1.1-v5 ended0/1,6.738s,guarde0366c351d4b:still0logicaledges, no images.
Source YAML inspection clarified Mat_chalk is the MATERIAL name, not necessarily
renderer/node name. Discovery now also reads material assignments, and masks only
chalk submeshes when a renderer has multiple materials. The exact4edge assertion
is retained and its failure now reports installed/current renderer counts, scene,
readability and bounded chalk-source names/bounds. No blind unchanged retry.
Next v6 runs the SAME single failing geometry/capture case; question and stop
condition unchanged. No.cs writes while running, no native build.

look-1.1-v6 compile failed before tests: own nested material local shadowed the
Build material variable (CS0136). Renamed it and the analogous new diagnostic
lambda parameter. No assertions changed. Run35902reaped,guard6f5d25be2698; no v6
XML or frames counted. v7 repeats only the SAME geometry/capture case after this
compile repair; outputs look-1.1-v7. No.cs writes in flight, no native build.

ACTIVE Unity handle78366,look-1.1-v7,confirmed running by write_stdin. No.cs edits
since launch. While it runs, inspected the7zip UX references and saved sheet-to-
feature mapping plus precedence of the final Home and later corrections in UX
intake. Original art unchanged. World1.1 remains the implementation priority.

look-1.1-v7 ended0/1,6.875s,guard34f042309ba7. The new exact runtime diagnostic
settles the cause: nodes are named default, material identifies chalk, and imported
meshes are NOT readable. Four edges are separate renderer bounds centred exactly
on +/-7, unlike throwing lines at+/-8, outer court at+/-12.5 and centre ring. No
batch splitting is needed. Removed the unused triangle-clone approach. Register
only complete thin chalk edge renderers by material plus world bounds; restore
those exact renderers at off/teardown. Original mesh assets/colliders never change.
Next v8: SAME single geometry/capture case, same4logicaledge assertion and five
maps. This fixes the measured own-code discovery defect; no fixture relaxation.
No.cs writes in flight. Output look-1.1-v8; no native build. Stop at pass/inspection.

look-1.1-v8 PASSED all5maps/4logicaledge replacement,38.242s,guard06285741e63f.
Inspected all20same-camera25percent-grey frames and full Bayan armed/rest,Ilalim,
Lagoon. Keep visual variant2: broader armed ink/chalk, clearly quieter rest, preserved
home/throw marks and no beacon. Known4churn restored;2metas preserved. No more
geometry/state reruns. Retain v4escape and v3replay/audio proofs.
One remaining required visual question: is the LOCAL nearest exit clear with the
completed HUD/comfort options, and does the actually-triggered escape puff read?
Added one visual-study case only:3same-camera owner images (off,on,comfort) plus
13samples of the actual crossing's puff at30fps. These form a muted study clip,
not a claim of free-play timing or human taste approval. Stop after fresh pass,
inspect full/grey frames and the clip sheet, then commit1.1 and proceed1.2.
Run look-1.1-v9 only LocalExitAndEscapeBeatVisualStudy; no.cs writes in flight,
no native build. Existing20map frames are not regenerated.

look-1.1-v9 appearance case PASSED8.952s,guard7feb0da3ee76. Personally inspected
owner before/after/comfort and all13escape samples as grey25 plus full frames.
Picture found a real visual fault: central puff was hidden by chibi feet. It now
uses two small side wisps outside the foot silhouette. Near chalk also read as
heavy rails: final look caps near width and retains the exact far width beyond8m.
The existing20wide map frames are beyond8m and unchanged, so do not regenerate them.
Local capture now runs at1x because blocking input during a staged time freeze can
hide the personal exit cue. This is visual variant3 (last subjective boundary look).
Next v10 runs ONLY the same appearance case; question: are the local exit and actual
triggered dust readable with regular/comfort settings and grey25? Stop at pass plus
inspection. Outputs look-1.1-v10. No.cs edits while running; no native build.
Known4churn restored afterv9 and2protectedmetas verified. All failures preserved.

look-1.1-v10 appearance case passed9.111s,guard58330d09b510, but picture review
still found misplaced/invisible wisps. The shader builds camera-facing vertices
from each object's origin, so dynamic batching must be disabled (otherwise vertices
are pretransformed and lose that origin). Added DisableBatching, no new look variant.
Also closed a genuine per-viewer scope weakness: nested preview/replay cameras can
overwrite the local exit property block. PreCull/PostRender now save/restore blocks;
the existing ownership case adds a nested-camera restore assertion. No HUD edits.
The local study asserts an actual active local viewer before capture.
Next v11 checks ONLY that changed ownership/escape case and the appearance case;
retains all5map/geometry and replay/audio evidence. Same stopping requirement: pass
and inspected local/escape frames. No.cs writes in flight; no build. Output v11.
