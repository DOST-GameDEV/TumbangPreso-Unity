# Active TUMP rework ledger

## Resume pointer, 2026-09-23

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
Design: NATIONALS_POLISH.md, VISUAL-1 section.


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
