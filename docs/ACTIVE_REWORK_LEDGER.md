# Active TUMP rework ledger

## Current resume,2026-09-24: tricycle published; finish Eskinita then other maps

LATEST OWNER DIRECTION: track lighting/peak-bright-overhaul and MERGE IT into
ASTRAReworks; its purpose is improved lighting/shaders. This explicitly supersedes
the earlier hold on merging that separate lane. Preserve current map work first,
fetch/inspect current branch changes, merge safely, resolve real integration issues
and perform a bounded compile/visual check. Keep tracking its remote updates during
the ongoing queue. No other conversation contacted or delegated.
Owner clarified the old look was too dark to see and the merged lighting/shaders
MUST inform art decisions. Silhouette clarity is approved. Reassess dark/hidden
facades, trees and materials under the adopted look; retain good details that now
read, and do not add extra geometry or local brightness to compensate for old
lighting. Research/materials/place detail remain central alongside clear forms.

Lighting50f1fc255merged cleanly into the working tree. Preserve the original
map/house work and the parallel lane's open LIGHT-1items. Current branch contains
the committed match-lighting/shader work; preview integration1.8is still noted as
uncommitted work in its lane, so do not race it by inventing a parallel preview
implementation. Next bounded integration check uses the two existing Stage/ramp
cases. No new test framework. Fetch this tracked branch before later published
batches and merge newly available work when it advances.

Local merge HEAD241e13bb586b16261498fb1a605faf5710d7de6a. QUAL
advanced cleanly to that head after preserving rejected masonry candidate in
stashb8a0ff20ae1710db37d09905df1bb21eb09ec758. Revised clipped-coating source is
in DEVstash67c02608f65817b2e604c69cd208e30b7abd3d3e. Do not pop the rejected
generated scene over the merged candidate. Integration79220completed/reaped:
2/2passed61.384s. Five-map after/Eskinita paired frames and selectedgrey25inspected;
visibility is much improved. Reports/lighting-integration-2026-09-24has receipts.
Known churn restored after patch backup, no jobs running. Publish merge/evidence
then resume revised art; do not repeat these unchanged checks. Owner asked status;
answered merged locally,
not yet pushed, and clarified silhouettes means recognizable forms, not the
entire map-improvement plan. Materials/textures/place detail remain central.

Lighting merge/evidence now PUBLISHED: DEV/verified remote52686a6e194372ca2fb952266b1ff0f1f66e7507,
containing origin/lighting/peak-bright-overhaul50f1fc255. QUAL advanced cleanly to
52686a6e1. Restored only revised masonry source from stash67c02608(stash retained),
NOT the rejected generated scene. Current revised author runs
Logs/refine2-masonry-bright-v2/author.log. Original broad plaster stays; only base
coating/roof/foliage finish is generated. Use the adopted bright look for its next
existing focused capture. Inputs frozen during author; no new test machinery.
Author91273/check50548completed/reaped: revised total2568vertices across five local
finishes;1/1passed6.720s. Five matched near views and small/grey inspected under the
adopted bright look. Original plaster retained; corrected output/evidence copied
to DEV/eskinita-masonry. Known generated churn restored; no active jobs. Publish
this unit and move to canopy/planting. No more unchanged masonry checks.
Next local canopy plan/actual five source-mesh records saved in eskinita-canopy-plan.md
and MapSource/environment/layouts/eskinita-canopy-refinement-20260924.json. Not
implemented yet. Keep original roots/colliders and other trees; reuse render routes.

Owner's latest AFK instruction: "ok ill go to sleep now js give me one final
acknowledgement that ull go autonomous and do everything i want pls save it on ledger".
Acknowledged in the active conversation; keep pursuing the FULL queue, with product
quality ahead of validation tooling. No checkpoint stop or automatic completion.

Overall goal ACTIVE and NOT complete. Owner is asleep and reiterated autonomous
completion of the full queue. Continue after status answers/checkpoints without
questions for independent work. No delegation/other chats, paid services/resets,
main edits or Desktop replacement. Preserve all older tasks and successful work;
current owner instructions/designs supersede old OPEN headings and retired FUTURE.
Do not treat a small pass, green check or publish as full completion.

DEV C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks, ASTRAReworks.
HEAD/verified remote1b8fc2ee866ee0a0c7087ae8faa6432b58ac6781. Only original dirty
home-court.png.meta/loading-street.png.meta under composition-redesign remained
before this ledger edit. NEVER restore/commit those two in DEV. Explicit staging,
sole authorM4tyu633, no trailers/AI mentions/em dashes. Fetch before every push;
merge incoming work safely and verify remote HEAD. No reset/clean/force push.

QUAL C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-world-qualification-20260923:
owned detached1b8fc2ee8 after26vehicle candidate paths matched published source
and were preserved in a named stash. Independent warm Library.
Prior owned candidates preserved in named stashes and Logs patches. Do not drop
those blindly. Other validation checkout has111dirty paths: never reset/clean it
or main/home/net/ilalim worktrees. Current masonry author is running; see next step.

## Latest actual work

Map research/design/owner requests: reports/map-by-map-refinement-2026-09-23/.
All five maps need their own meaningful surroundings, construction/material detail,
Filipino place identity, animated sky/background and actual-camera review. Owner
explicitly added tricycles and researched street-life objects. No generic cultural
prop pack or uniform texture/noise pass. Generated references are inspiration only;
critique/keep/reject decisions and full prompts/provenance are retained in ArtSource.

Published Eskinita components, not the whole map:
-40ca8a1b9initial26outer houses/4streets. Mint roofs rejected/fixed; inspected.
-2f2c55917deeper96street blocks:104retained middle-distance/448simpler far homes,
 24yards,315742vertices, one408x416atlas/material,97meshes, no new colliders or
 distant shadows. Matched real preview/small/grey inspected;1/1pass1.781s.43.9MB
 source assets, no native FPS claim. Static map-card renders still need refresh
 after the map is stable. Context beyond the first row is no longer an empty band.
-ccd4285f5home4_W: fitted plaster/roof/foliage finish and measured supported shade.
 1509vertices/one renderer/material. Variant1rejected, variant2inspected;1/1pass4.408s.
-3d80ffbb5horizontal timber at5_W/2_E/5_E/6_E:12local material derivatives remove
 incorrect vertical grid over actual boards. Geometry/colors/windows preserved.
 Separate5_W1/1pass4.670s and east1/1pass6.204s; matched lit/street/small/grey inspected.
 These close material mismatches, not the complete houses or shadow/occlusion issue.
-62268c257home3_Wdomestic window: raised shade/basket/cloth on measured sill;
 only two product submeshes removed,0_Wshop and all other original details kept.
 1176addedvertices/one renderer/material/no collision.1/1pass4.995s, inspected.
 Also fixes real timber reauthor persistence defect: configure a fresh material
 completely, THEN copy final serialized data to the existing asset. CopySerialized
 before setters and CopyPropertiesFromMaterialalone reverted saved_DeckSurface.
 All12saved modes checked after reload. Rejected XML/stash/differences preserved.
-1b8fc2ee8Eskinita tricycle07in private vehicle bay2_W. New map-local model/native
 source from copied original terminal author; Bayan retained. Refined nose/glass,
 mirrors/octagonal lamp, stripe, supported driver shade,3grounded tyres. Old delivery
 renderers hidden; original sources/transforms/colliders and bay boundaries kept.
 Variant1placement too far back. Variant2moves forward/turns12degrees inside old
 footprint.1/1pass5.088s; street before/after,small,grey25and native4views inspected.
 Pole still overlaps part of fixed street view; whole-map camera review remains.
 New bounds center(-11.00,1.01,7.79),extents(1.20,.91,1.26). GLB126324bytes/9primitives.
 Existing model diagnostic1/1pass6.043s; its side frame had neighbor near-fade
 occlusion, so do not claim unobstructed baseline side approval. No recapture needed.

All reports/XML/images in named subfolders under the map refinement report folder.
Last vehicle author18047/check11660completed and reaped. Copied qualified artifacts
into DEV and published; full patches in QUALLogs/refine2-tricycle-v2. Generated
churn restored. No unchanged vehicle/house/timber/context rechecks or native builds.

## Current next implementation

Continue Eskinita's remaining masonry fronts and roof/construction decisions,
then tree/house visibility, planting/ordinary props, sky/background composition.
Five named finishes are now drafted for0_W/1_W/3_W/1_E/3_Ewith individual base/wash/
roof decisions in eskinita-masonry-finishes.md. Existing canopies, shop/domestic
sill, source paint/roof palette, solids and openings stay. Guarded author58519
completed:4542finish vertices/five renderers/materials, original collision intact.
Check85614completed1/1in7.221s, but inspected draft replaces too much broad plaster
and flattens it. Keep original wall/grain; revised source clips the coating to each
base course, retaining roof and foliage improvements. That revision is not yet
authored/qualified. Do not publish the rejected whole-wall assets as final work.
Owner now prioritizes integrating lighting/peak-bright-overhaul. Preserve this
masonry draft/captures, merge lighting first, then generate the revised finish
against the adopted look. No active jobs, no new test machinery needed.
Primary lots/source inventory and actual per-house judgments: eskinita-decisions.md.
0_Wstore remains;3_Wis domestic.1_W/1_E/3_Ehave existing supported entry canopies;
do not duplicate them. Broad plaster still needs distinct fitted finish choices.
5_W/6_Eand parts of2_E/5_Eare masked by tree crowns; inspect/adjust locally, not
all trees in every map. Existing nine urban-tree placements are generated by
MapFinalPassAuthor.ReplaceTrees/PlaceTree. Preserve good construction/models.
Source assets, houses, routes and collisions must not change incidentally.

Then full Eskinita lobby/intro/eye/spectator/small/Low review and static map-card
refresh, then Bayan, Ilalim, SaBubong and Lagoon one at a time. Each needs its own
street/landscape/community context. Lagoon is specifically Sama Bajau; no tricycles
on stilt decks or rooftop courts. Animal behavior, bot stalls, per-character walk,
ordinary throw and both pektus directions remain assigned in REFINE-2. Not done.

## Concurrent updates and older qualification

Incoming UI/Rafi commits were merged without overwriting either lane. Latest
integrated5686da5cb/da57b7b72includes UI follow-up and Rafi v9, through123652274.
UI-REVIEW section in TODO and parallel lane below retain that work. Do not redo
those models/UI, impose a repo UI-work ban, or treat older Rafi evidence as v9proof.
Manual Claude prompt was delivered locally in Downloads; no task was contacted.

Lighting branch was last observed50f1fc255, not yet merged at this pointer. Owner
now explicitly requests integration and tracking. Refresh its current HEAD first.
Earlier LIGHT-1.6/1.8/1.9remaining notes must be reconciled with its latest source;
do not assume all lighting work complete or duplicate the lane's shader/grade work.

P6 accounting52ef62443preserved366original headings,0unreviewed; disposition is
not blanket implementation completion.127.3first chat fixed7a0ae3363/native proof
a7c4d1b35: full first message at1680x720/960x540largeHUD, exit0/shared input unchanged.
UX-1 HOME/external lobby/arrival/map ballot/8sintro/autostart/Terms/loading/BHlogo
and related corrections are implemented with dated evidence in front-end completion.
Core629/629, requested EditMode36/36, profile13/13,Checks8/8,14gating audits are
prior receipts, not current full regression. Native address/code/rejoin/arrival/
rematch receipts exist, not true multi-peer UGSqueue proof. Wallet exact source
version1published and native LOADproved; live buy/claim unverified.

Still open: true multi-peer UGS queue/lockin/ballot; browser interaction/live buy-claim;
native physical typing/chat/rename/running-match rejoin; final regression/build
identity; earlier D3D12shutdown0xC0000005cause. UI lane also reports chosen-arena
rematch retaining Eskinita; trace actual current path later, do not dismiss it with
old receipts or repair its fixture blindly. Physical Android/pad unavailable;
human listening separate. Continue independent tasks instead of inventing blockers.
Latest native build remains QUALBuilds/p7-chat-7a0ae3363/TumbangPreso.exe,1278MB,
157s, honest DIRTY identity. Four repeated recovery/swim clip generation deltas
remain unreconciled; no false clean stamp. No native build during these map units.

## Local execution limits and restart safety

Owner restarted PC because of lag, then explicitly said there is no need to limit
PC resources while asleep and to go all out. Use normal available workers/priority
again for unattended work; the prior2job/1GC/1import limits were temporary launch
overrides, never project settings. Keep one Unity run per checkout and freeze its
inputs. This changes local resource use, not the standing no-paid-services,
no-reset/no-delegation scope. Unity still only through the guarded named profile.

No.cs edits during an active Unity run. Never guard --help or -nographics. Read
fresh nonzero XML, not exit code. One focused check per coherent changed risk;
no unchanged reassurance loops. Native desktop capture repair budget is spent:
use existing in-engine path, never build a new capture framework. All rendering
follows CANONICAL_RENDERING_PIPELINE; Blender only authors mesh/native source.

On interruption verify process/handle and fresh artifacts first. Tricycle check
18728was missing after restart, no process/XML, stopped startup00:10:08Sep24;
snapshot2936fae2f301matched all4files/shared input. Resumed35623completed11.040s
before changed-placement variant2. Do not restart completed authors/mesh generation.
After each run save complete generated diff then restore only task-owned churn.
Keep DEVoriginal twoPNGmetas. No task-owned browser tabs/preview servers remain;
last two photo references were inspected in one hidden IAB tab, closed/verified[].

Full previous ledger preserved in
[history through tricycle](reports/map-by-map-refinement-2026-09-23/ledger-through-tricycle-20260924.md),
which links older history. Old pending/running notes there are historical only.

## Parallel lane: UI and HUD review (second machine), 2026-09-23

Checkout C:/Users/Matthew/dev/TumbangPreso-Unity-ASTRAReworks (worktree of
C:/Users/Matthew/dev/TumbangPreso-Unity), branch ASTRAReworks. Status and checklist:
TODO "UI-REVIEW". Research and plan: docs/reports/ui-hud-review-2026-09-23/.
Pushed 23deea41 (UI batch 39373c32 merged over the map lane's 2f2c5591, no overlap).
Guarded Unity only, profile presentation-validation-20260921; runs so far:
ui-batch1 11/11, ui-batch2 12/12, art-batch3 (Rafi v6 lineup/head written, then the
turnaround step needed -rig; bounded repair used), art-batch4 running (Rafi v7 review
plus ModeCardPoseAuthor poses to Logs/mode-card-poses). Never commit the two original
composition-redesign .meta files. Next: avatar compile and picker capture, posters,
Rafi v7 inspection, five-shape captures of the changed screens, push.
