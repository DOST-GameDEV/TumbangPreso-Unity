# Active TUMP rework ledger

## Current resume, 2026-09-24: Sa Bubong distance haze

Overall goal ACTIVE, NOT done. DEV/QUAL and remote verified at07a387779. The
tracked lighting branch through a28037622 is merged. One conflict preserved our
explicit-sun/preview API and cached restoration while adding per-camera InkFloor.
New native floor-choice1/1 in19.144s; source/local comparisons and grey inspected.
Default0 retained. Report: lighting-integration-2026-09-24/hull-floor/report.md.
No Unity job running; known churn backed up/restored. Protected DEV metas intact.

Current local unit: SaBubong preview court contrast. Saved plan:
map-by-map-refinement-2026-09-23/rooftop-court-contrast-plan.md. Native same-camera
current/ivory/dark study1/1 in1.740s; actual images/25percent grey inspected.
Dark warm-neutral(.23,.19,.15) is clearest; ivory barely improves it. Current
SaBubongBuilder change gives only the six court lines a separate material and
updates rebuilds; notice-paper material, lights, geometry/rules are preserved.
Guarded author8012 completed; exactly six scene material references changed.
Scene/new material/meta copied to DEV, known churn backed up/restored. Final
preview9009 completed:1/1 in1.287s, actual authored frame and grey inspected.
Logs/roof-court-contrast-final.xml/.log and frames. Report in
map-by-map-refinement-2026-09-23/rooftop-court-contrast/report.md. Known churn
restored. Court material/scene/builder/report published in07a387779. Study is
the matched A/B; final checks serialization. No fixture repair used.

New local plan: rooftop-haze-plan.md. Current high preview loses lower streets/
middle facades in peach haze. Compare SaBubong distances60-300,90-380,120-480m
only, holding colour/light/materials and camera fixed. Guarded job29799 runs
RooftopPreviewComparesDistanceHaze: Logs/roof-haze-study-v1.xml/.log and frames
in the named directory. Study reaped:1/1 in1.438s, actual colour/grey inspected.
Selected90-380m, more identity in mid facades while far skyline stays softer.
Only two SaBubong haze distances changed; lower ground material warmth is not
claimed fixed by haze alone. Existing card route now accepts a single-map selector.
Final card v1 passed its export check but actual inspection caught old haze:
Resources/WorldLookProfile.asset overrides code defaults. This is an incomplete
production data update, not a fixture failure. Preserve that v1 as unselected.
WorldCueProfileAuthor.ApplyRooftopHaze now updates only the saved roof distances
from the chosen defaults. Guarded author runs in Logs/roof-haze-author-profile.log;
Author67188 reaped; saved asset diff is exactly roof FogStart90/FogEnd380 plus
explicitly serializing the unchanged CastInkFloor0. Asset copied to DEV. Final
v2 card capture45522 reaped:1/1 in1.155s, actual image/grey inspected. SaBubong
PNG replaced by final actual scene; original importer/GUID hash and previous-card
preserved. Report: rooftop-haze/report.md. Known churn restored, no Unity running.
Publish scoped profile/author/card/evidence; then review remaining map-context/
contrast and the old actionable queue without redoing the completed micro-pass.
Fixture0/1, no extra study variants.

Bot fixes published:5de3a78f3 reaction timing;043cf804c chase patience;273e5e669
per-seat difficulty. All five maps have initial four-bot/two-mode ordinary samples.
Full role/roster/tier and bot water/roof recovery remain actionable final gates.
Recovery fixture failed twice BEFORE bot handoff, last(16.92,-2.01,16.38), not the
intended lip. Existing ExerciseEdge also sets camera follow/movement aim; omitted
input-basis setup is the next diagnostic. Allowance1/1 exhausted: no more fixture
polishing during feature work. Draft/failures preserved in bot-map-coverage/;
original compiled LagoonRecoveryProbe restored. No production failure claimed.

AGENTS cleanup dc10b68f2 preserves every important instruction in39.7percent fewer
words plus exact original/archive mapping. SaBubong birds57cf74a9c complete locally.
All21 ambient actors have local map/species placement coverage; ordinary-camera/
replay/combined checks remain open. Preserve every older task and evidence.
New character/ability/ultimate animation direction belongs to the owner-run cloud
lane; its origin branch exists, but do not merge unfinished work or contact a task.
Final native/peer/replay/performance/build remains2.10/P7. No checkpoint stops goal.

## Parallel lane, 2026-09-24: HOME loop and gameplay animation (separate from the map resume above)

Published 4255265c and 175cac0d on ASTRAReworks. UX-1.20 (Phaister's HOME loop, shipped crf 23,
35.1 MB) and UX-1.21 (Kuro) DONE. REFINE-2.9 progress: arms while moving, throw and pektus, the can
raise, the tag, and all 21 hero casts refined in both views; REFINE-2.9c (every body) done. Evidence and
reasoning: docs/reports/gameplay-animation-2026-09-24/. Probes: LocomotionArmsProbe, GameplayActionShots,
CastAndMotionReel. Shipping casts are the glb tables in tools/author_hero_action.py, NOT HeroAbilityClips
(editor fallback only). Last checks: PlayMode 11/11, EditMode 44/44, 18 glb casts verified. Not yet
seen: a live networked match, including an observer's estimate of the can raise.

## Published progress, do not redo

Ground-animal local units all published: Eskinita dog01d93272b/cat08ac1a178,
Bayan dog4e2c5bc6d/cat2bcfd0581, Ilalim dog81054255e/catd914a3497. Each has its
own plan/author/native result/actual frame review under the named report folder.
Bird visits: Eskinita ad7d57955, Bayan c61bb0318, Ilalim fdce3ebf8 and current
SaBubong unit complete locally; final integrated review remains open.
Lighting follow-ups237012904 and43c851b6f include source through429643416; keep
Windows evidence separate from the incoming Mac native performance receipts.


- All 11 Ilalim storefronts and both Eskinita shop signs have individual authored
  work/native evidence. Sign register and each report preserve exact source,
  rejected candidates and review limits. Pares v1 owner-rejected; Lugaw King
  reference was actually inspected and v2 wood fascia/simple bowl shipped in
  065ad6480. No owner approval of the replacement is implied.
- 02617e9d2 refreshed Eskinita/Ilalim map cards from actual current scenes. Ilalim
  axial under-bridge thumbnail selected; runtime UI/camera descriptors unchanged.
- 59b8dc129 roof rail catch/hang/mash/same-lip climb and Lagoon swim/near-bridge
  Jump/mash climb. Protocol 51; host trajectories/epochs/reliable poses, cast-palm
  fitting, local native evidence. Real peers/loss/rejoin/replay remain final gates.
- 01d93272b Eskinita tan aspin: 129 nodes/768 links, investigate/watch/rare marking,
  eased stride/turn/arrival, close or fast-approach retreat, stationary-person
  tolerance. Focused native 1/1 in 32.339s; actual frames/grey inspected.
- 08ac1a178 Eskinita tabby: 111 nodes/669 links, three east-side sites, quieter tail,
  own pace/head dip/holds. Native v2 1/1 in 32.072s. V1 wrong-side observer/camera
  fixed once; failed evidence retained. Actual before/new/activity frames/grey
  inspected. MP4s retain recorded timing; no continuous-playback claim from the
  image-only review tool. Detailed reports in eskinita-dog/ and eskinita-cat/.
- 21efec0c7 merged concurrent remote 4255265c3 without conflicts. It adds the
  owner-approved Phaister HOME loop/Kuro work and new body/FPP locomotion, throws,
  pektus and can raise. Preserve it. Before REFINE-2.9 read
  reports/gameplay-animation-2026-09-24/research-and-analysis.md and latest TODO;
  inspect/fix remaining per-character/contact/integration gaps, do not rewrite
  these new implementations from stale queue wording. Cat evidence predates merge;
  combined qualification and body/edge-recovery overlay review remain open.

QUAL modified candidates were byte-compared to DEV, preserved in named stashes
and advanced. Keep those stashes and all unrelated worktrees. Known generated
Inday/meta/ProjectAuditor churn was backed up before restore after every run.

## Remaining scope and constraints

All five map parents and REFINE-2.7 remain OPEN for outstanding coverage and final
integration. Older building/material/coast/sky/island/bird/sign work stays intact.
Lighting source 50f1fc255 is integrated; preserve its bright look. LIGHT-1.6/1.9
remaining tuning/native performance are still open, not a reason to darken blindly.

Still assigned: remaining animal species/maps, all-bot stalls, per-character
motion/contact/observer review against the NEW merged implementation, chosen-map
rematch defect, native D3D12 shutdown 0xC0000005, RafiV9/peer/platform gates and
all older actionable IDs. The owner-run cloud lane may now tackle
Ultimate REFINE-2.11 alongside local work: research multiple praised games first,
individual hero plans next, implementation last. Phaister laughter/flight example; 2.8 seconds is not a cap. Full requirements
are saved in ultimate-performance-research-plan.md. No task silently removed.

Final coherent native/peer/replay/performance regression and internal build are
REFINE-2.10/P7 after features. No intermediate build or Desktop replacement.
UGS credentials, physical devices and owner taste limit only their specific claims.
No paid services, resets, other chats, subagents, main edits or forced git actions.
Fetch before each push; sole-author -F commits, explicit paths, verify remote HEAD.
Product quality over validation loops; no repeated unchanged evidence/fixture work.

## Cleanup and preserved history

An earlier task-owned hidden IAB tab15 remains stuck on PNA's certificate-error
data URL. Documented close/navigation/alternate close all rejected by URL policy.
Do not bypass security, kill the browser or loop on the same rejection. Other
research tabs closed. No new browser was opened for the current animal work.

Full preceding ledger: [through Eskinita animals](reports/map-by-map-refinement-2026-09-23/ledger-through-eskinita-animals-20260924.md).
It links every earlier full archive. TODO is canonical status; this is the exact
resume point. Continue without treating this checkpoint as completion.

Bayan cream aspin implementation ready. Guarded targeted author next, then its
one native case. Check purposeful connected sites, real observer/retreat/recovery
and pause, same-camera native frames/grey. Stop on pass and inspection. Retry 0/1.

Bayan first bake found 89 nodes / 691 links but no reachable bench/tree. Adjusted
to planting plus two distinct court watches and rare marking; require three
ordinary sites so cooldown cannot reduce behavior to two alternating stops.
Reauthor before first native test; no fixture retry used. No .cs edits in flight.

Lighting conflicts resolved: kept existing preview API/cache property-block fix,
ported HDR target, surviving root/sun choice, active-scene handback guard. Incoming
five-map case now also captures same-camera LDR/HDR and asserts new-scene settings
survive preview destruction. Next focused run with existing cached-map lift case
(two cases total). Stop on XML and actual colour/grey review; retry 0/1.

Edge-key v1 passed 1/1 in 8.235s, but cross-run image comparison was NOT camera
matched because preview idle orbit advanced differently. Images retained as
unmatched v1, not causal visual proof. One bounded capture correction: editor-only
legacy edge-key switch reproduces the old fallback with a portrait sun, then new
selection renders immediately through the same camera without yielding. Shipping
player has no switch. Retry 1/1; next v2 same single case, no fixture expansion.

Ilalim cat author completed: 53 nodes / 310 links, Pisonet watch (9.5,3.5),
bakeryward watch (9.5,9.5), seam investigation (10,3), y0.212. Before the first
run, movement assertion fitted to its shortest real trip (0.707m), not the other
cats fixed0.75m threshold. No failure/retry yet; actual successful movement still
required. Next only this cat native case. No .cs edits while running.

Bird verification question: do the real visitors retain original models, land
on supported alternatives, react and keep flying beyond the old four-metre hide
point, with distinct foraging/fantail lookout and frozen pause? Stop after native
XML and actual legacy/new perch/departure/arrival frames plus grey. Retry 0/1.
No .cs edits during the author/test run.

Bird author v1 stopped on one compile error in the new review file: missing
TumbangPreso.UI import for SceneFlow. Added that import only; this consumes the
unit fixture repair allowance (1/1). Preserve failed log. Reauthor, then the
same focused native case; no extra fixture refactor/reassurance runs.

Bird author v2 completed: all three species have three supported sites; distinct
beat/wait/forage/fan settings saved. Next single native Eskinita case records all
three legacy/new departures and arrivals, supported contact, reaction and pause.
Only prior missing-namespace fixture fix consumed retry 1/1. No .cs edits in flight.

Bayan bird author v1 rejected fantail northern landings near the monument. No
scene saved. Revised only that bird candidate list to clear court-facing/southern
paving; clearance unchanged. Reauthor v2 before first native case. No fixture
repair used (0/1); no .cs edits during run. Failed log/patch retained.

Bayan author v2 completed with three supported sites for each species, corrected
fantu alternatives at (+/-1.8,8.6). Only the Bayan bird native case next, with
original species clips, larger paving spread/longer holds. No fixture retries
used; no .cs edits while it runs.

Bayan native v1 failed clear-arrival assumption for pigeon. Trace shows arrival
turning into phase3 at(-.193,2.714,-4.993), near=True: correct avoidance of a
parked player, not failed landing interpolation. The taya cannot remain at the
requested(0,-13). One bounded fixture correction parks all actors at the opposite
legal court corner for arrival and asserts actual distance>12m. Runtime unchanged;
retry 1/1. Preserve failed XML/trace, rerun same case v2 only.

Owner steering during Ilalim bird unit: prepare paste-ready Claude Code cloud
configuration and a broad ALL-animation/research handoff, including independent
ability animations and freely directed ultimate performances. Do not contact
another conversation. The owner will paste it. This newer lane assignment lets
that animation work start without waiting for the older map-first schedule.
Preserve current map work and avoid duplicating the animation lane. Ilalim bird
author 42324 completed with three supported sites/species and bounded street-axis
flight; native test has NOT run yet. Source/scene/meta changes remain local at
c61bb0318. No Unity process active. Resume map validation after delivering setup.

Cloud setup deliverable prepared as four chat blocks: environment values, empty
API credentials, Ubuntu bootstrap with deferred heavy Unity install, broad owner-
directed animation/research prompt. Exact repo Unity6000.5.8f1 / changeset verified;
local Blender5.2.0LTS verified; Core.Tests targets net9.0. Setup Bash syntax checked
locally only, not executed in a Claude VM. Unity archive URL HEAD200 (4.35GB);
Blender primary download returned403 from this connection, so no successful
cloud download claim. Current map-bird author complete, native validation next.

Requested cloud environment/API/setup/handoff blocks delivered in the conversation.
Owner explicitly said continue local work afterward. Animation ownership/intended
parallel sequencing recorded in TODO/ultimate plan; no other task contacted.
Local work resumes the already-authored Ilalim bird flight corridor native check,
then remaining environment/behavior work. New hero/ability/ultimate animation
direction belongs to the owner-run cloud lane. No animation work marked done.

Concurrent incoming animation integration: origin175cac0dd/6bba9d0a4 added tag
reaches, cast readability/first-person gestures and per-body review receipts.
Merged without source conflicts; preserve its separate ledger section and TODO
2.9c status/evidence. New cloud lane must review this newest baseline, not redo
old weak-cast findings automatically. Local bird evidence predates this merge.
Incoming QualitySettings changed only five Ultra slots to the exact runtime
GraphicsProfiles.Balanced values (2 lights, Medium shadows, 2 cascades, 40m,
soft particles false). Restored the prior serialized baseline under the standing
runtime-churn rule; all authored animation/models/arm assets retained. No native
build or gameplay-animation requalification claimed in this map merge.

Tracked lighting source7d3171549 merged without conflicts. Adds real-window
captures/on-off measurements to the existing native graphics probe and scoped
Mac1600x680/2940x1912 evidence. tools/graphics_review.py syntax-checked locally;
no new Windows player/performance run claimed. Windows D3D qualification stays
P7, and newly recorded LIGHT-1.10 Mac fullscreen hitching remains open with its
actual source evidence. No previous queue row removed. Combined C# compilation
will be exercised by the next necessary map author/run, not an extra build.
