# Active TUMP rework ledger

## Scope and quality rule

Complete EVERY unfinished autonomously actionable TODO in this conversation.
Owner is AFK. No questions, subagents/other chats, paid services, resets, main edits
or Desktop replacement. Preserve IDs, source art/rigs, original dirty work and
failed evidence. AGENTS.md is authoritative. Do not end at a phase/checkpoint.

Owner correction 2026-09-22: recognize this model's tendency to get stuck in
verification loops. Quality still matters most. During implementation, run focused
checks against specific changed risks, fix actual failures, then move on. Do not
expand diagnostics or repeat passing evidence without a new reason. Broad regression
belongs after ALL features are integrated. Deferred checks remain in TODO.

## NEWEST owner steering: Rafi identity, face/hair and FPP

The owner rejects Rafi's current expression/hair and says he feels like a retexture.
A major original silhouette/garment/face refinement now takes priority. Detailed
six-step plan saved at TOP of BADJAO_EXPANSION.md; TODO art checkbox reopened.
No existing heroes/builders may change. No gills. Rejected copied-builder draft
saved under ArtSource/rafi/rejected-face-hair-20260922. New concept saved as rafi-distinctive-hero-v3.png. Rafi-only recipe IMPLEMENTED:
68body/20head boxes, swept tilted hair/headwrap/float clip, asymmetric flap/sash/
sailcloth/rope coil, left wrist cord, short mouth/native brows;816680bytes/33clips.
All12protected originals unchanged. No rig/bone contract or other character edits.

FPP ROOT CAUSE FOUND: ApplyCharacterStyle never called UseRosterArms for Rafi;
he fell into generic hands/accessories despite his baked meshes existing. DEV
ViewmodelArms.cs now routes only Rafi through UseRosterArms and the body palette.
Copied to validation with the new model, not yet runtime-verified. Native v3
review completed0,guard3a38a0315acd,job7820 reaped. Actual images show new silhouette
but rectangular locks and a round badge-like coil. Refined native taper/tilts and
elongated rope loops; current876968bytes/76body20head boxes. V4 native review completed0,guard595303bb8589,job95214 reaped. Four angles inspected:
tapered sweep and elongated coil now read correctly. Recovered only Rafi generated
portrait/palette/left arm changes in rafi-distinct-v4-transfer.json. LIVE internal
Builds/rafi-refined-v50,job30634,Logs/rafi-refined-v50-build/editor.log. It includes
actual source-arm routing plus native variant footage with real throw/release and
clearer Rafi camera staging. Next run selected owner and observer cases on Lagoon,
inspect holding/empty/cast/throw, then continue preserved full TODO. No mechanics rerun.

Independent recall Classic finally PASSED actual five native peers onv49c; job85316
reaped. Direct arena boot marks lobby in-progress even before READY, so four peers
plus a seat-change fixture cannot create a spectator. Runner now uses actual fifth
join; no gameplay change. v49/v49b failures preserved. Hero counterpart also PASSED,job88571 reaped. Both-mode five-peer evidence saved
under network-evidence. No remaining native recall run. Resume Rafi art now.

## Exact current action: native Rafi retrofit and village refinement

Owner rejected the standalone box-recipe Rafi AND sparse regular lagoon. Their
latest directions: use a copy of the original voxel builder, match HERO cast
Sean/Cheska/Dante/etc., never edit existing characters/builders, use image ideation,
refine village from supplied photo/primary sources, add detached offshore homes.
LATEST: NO GILLS. Removed component/hook from DEV and validation. New model has none.

Copied tools/build_person_voxel.py -> tools/build_rafi_voxel.py and RETROFITTED eight
recipe tables/constants plus its native curved mouth. READY is now TRUE. Uses native
donor skull/face, family remap, chamfer/smoothing/packing/retarget pipeline; original
builder unchanged. build_rafi.py only wraps it. Generated590776byte model,44body/13head boxes, height.775m (23/22/56% bands). All33 source
clips retained;25translation tracks retargeted, so DO NOT claim byte identity.
Protected hashes for six existing heroes/builders match. Provenance under
ArtSource/badjao/rafi-refinement-20260922. Rejected separate recipe is archived.

Native review: v1 failed an Object alias in NEW review code, retained; fixed alias.
V2 exited0,guarddb4db4e0baac,job67224 reaped. Actual front/quarter HERO lineups and
Rafi four-angle images in validation Logs/rafi-native-voxel-v2. Face/skull now match
native family. Initial lineup clipped Phaister's tall hat; the comparison camera now fits the
computed full bounds. Rafi tie visibility/expression refined without changing others.
Native owner/observer choice routes now pass; DEV roster/FPP/portrait were refreshed
from this recipe. The rejected earlier art is retained only as historical evidence.

Active image concept: ArtSource/badjao/rafi-refinement-20260922/rafi-hero-concept-v2-no-gills.png.
Built-in tool has no model-version selector; never claim verified2.5. Concept supplies
costume/hair/personality; copied builder and native hero lineup govern proportions.
Gills removed from concept, model, runtime hook and active lore. Earlier references
are superseded history. Original models/builders remain unchanged.

Village refinement is IMPLEMENTED IN SOURCE:8connected +10detached stilt homes,
raised household floors with world-height piles/bracing, distinct open/wood/woven/
thatch/hip/metal structures, offset spurs, ladders/household/work details,4residents,
4curved canoes +2houseboats, irregular islands/ranges, shallower water/caustics.
Original court/water levels and4stair exits retained. Rejected source is archived.
First native editor art pass village-refinement-v1 exited0,guardcc392c9efe92; images
show the new village but also TWO REAL MESH DEFECTS: negative floating-point sin(pi)
raised to fractional power made canoe tip coordinates/bounds NaN; slab rendering
was coplanar with deck boards after aligning the collision top. Fixed by clamping
sin and separating invisible walking collider from the lower visible slab. Added
finite mesh guard before saves. No other feature tests are being expanded.

Village-refinement-v2 exited0,guard693feac3994e, finite canoe geometry and collider/
slab separation fixed. Its warm-session images still displayed old combined-mesh
buffers. A NEW EDITOR saved-scene-only render (village-saved-review-v3,guard9e63c5d96438)
confirmed the saved canoe hulls and clean deck are correct. Reaped jobs73974/30522.
Use a separate saved-scene render after generation; do not chase stale warm images.

Latest included in v47: raised the canoe inner floor slightly above the water layer while
retaining the curved submerged outer shell; alternate Rafi speed/radius/duration/
earlier-echo-reveal now derive from declared35% gain/cost. Defaults unchanged.
v47 BUILD COMPLETED:1210MB/85s,guard efc8f180facf,job98461 reaped exit0.
Selected Rafi owner8/8 and observer8/8 role/choice routes passed; jobs53905/9203
reaped. Lagoon native both-mode/three-quality route passed; job3759 reaped.
All shared input preferences unchanged. Actual native introduction shows the
copied-builder Rafi with normal skin/no gills. Existing models/builders unchanged.
Latest194changed/new asset paths transferred to DEV with0conflicts via
expansion-asset-transfer-v6.json; includes latest village, Rafi roster/FPP/portrait.

REAL FAILURE: v47 shared introduction/live execution passed, but retained replay
was rejected by MatchReplayArchive's old six-hero coverage list. Failed job95685
reaped; Logs/rafi-native-ultimate-v47 preserved. DEV + validation now include Rafi
in the explicit list, using the existing water-field recording/visual-only reader.
v48 rebuilt1210MB/69s,guard3dc36647b653,job45508 reaped. Selected native shared/replay
passed (job80052 reaped): retained8675bytes/8objects/1field. However actual frame112
shows a BLACK wave. Source front/back triangles shared vertices and cancelled normals;
same defect affected introduction ribbons. This is NOT a passing art result.
DEV+validation now use single-winding meshes and a dedicated transparent CullOff
RafiWater shader, also for the echo/foam; no existing hero materials changed.
Rafi also stops using the legacy generic ultimate column. Normal skin/no gills retained.
v49 built1210MB/59s,guard4721244d7d1e,job9645 reaped. Native shared/replay passed,
job5904 reaped. Actual frame30/99 confirm translucent ribbons/wave and visible court.
Three real peers on Lagoon with150ms simulated delay passed,job26732 reaped:
447/441/439rows, all3event IDs/path geometry, exact fees, one ultimate,6repeated
snapshots,0final fields, max expiry offset.236s. Evidence copied to expansion-evidence.

LIVE: deferred recall-render Classic, Logs/net-recall-classic-v49b,job72450.
First v49 attempt passed3playing peers but spectator failed: runner started its
seat-change after the match quorum, when lobby correctly refuses it. Preserved
failure. Fixed only runner launch order: host,spectator(wait for-1),owner,other.
After it completes, run Hero counterpart, inspect changed Q/E water material in
selected observer route, encode new recordings, then source publication/whole-TODO
reconciliation/coherent qualification. Do not stop or rerun unchanged green work.

v46 built0 but contains REJECTED art; labelled obsolete.
Do not spend native capture suites on that model/map. Keep all implemented kit/Q/E/
wave, alternatives, host/wire/replay, body/FPP/intro/glyph/audio/bot/registry work.
Initial narrow gameplay evidence remains3pass plus corrected stair1pass. A camera-
relative fixture caused the stair failure; no buoyancy workaround was needed.
Review hero/map filters allow targeted new-feature native checks after the art is
integrated. Broad regression/full TODO dispositions remain final integration work.
Never end at an art checkpoint; all remaining actionable TODO stays assigned.

## Workspace and published state

Dev: C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks, ASTRAReworks.
Verified published HEAD 3b4fb19c81ea06dd7bbd8a1e42d4b0d707d77421.
Map surface/sky/outline and plain Inday arms: e50f8c37,4a8a51ec.
UI/controller/whole-kit fixes and native checks: 3b4fb19c.
Validation sibling TumbangPreso-Unity-validation remains detached db976126 with
explicitly copied owned inputs and separate Assets/Library/Temp/obj. Built-in render
pipeline. Protocol49 / replay11 in the new expansion sources. Preserve compatible append-only IDs.
Unity6000.5.8f1, every Editor via tools/run_unity_guarded.py with named profile
presentation-validation-20260921. One heavy job. Keep caches. Freeze active inputs.

## Latest completed checkpoint

Internal v45 Builds/network-final-v45/TumbangPreso.exe,1193MB/52s,guardffd0419f6100.
Network rehost Logs/net-current-rehost-v1 PASSED two matches and fresh session epochs,
old-epoch ability refusal, subsequent valid requests and host exit0. Job50052 reaped.
Owned clients cleaned, original profiles restored. Receipt/raw compressed evidence:
reports/full-backlog-2026-09-21/network-evidence/rehost-v1.
Other current request clean/impaired/outage results and initial fixture failures
are preserved under network-evidence. No claim of a final coherent release.

## Remaining work and deferred checks

1. Rafi B: native chunky model, simple no-thumb hands, green rolled shirt/navy shorts,
   tied hair, normal skin without gills; body/FPP/actions. Crosscurrent, Mirrorwake and
   Breakwater with alternatives, host authority, replay/presentation/roster/UI/audio.
2. Lagoon C: supported fixed stilt houses around a generous central sporting deck,
   broad bridges, distinct wood/roof/repair details, boats/laundry/community life;
   water/edge recovery for every character and Classic. Original reference photo
   is internal reference only, never a shipped texture.
3. Complete any other actual missing feature found while reconciling preserved TODO.
4. Final coherent integration: prepared recall owner/other/spectator checks in both
   modes, remaining overlaps/network/native routes, all-map/roster qualification,
   full TODO dispositions. Compiled NetRecallRenderProbe has NOT been run. Preserve it.

## Owned dirty work / evidence

Uncommitted network diagnostics: NetRequestSafetyProbe.cs, tools/net_request_safety.py,
net_matrix.py, NetRecallRenderProbe.cs+meta, tools/net_recall_render.py and reports.
Latest owner rule/doc changes in AGENTS,TODO,BADJAO_EXPANSION and this ledger.
Exclude unrelated original home-court.png.meta and loading-street.png.meta.
All initial26 dirty inputs backed up under Logs/presentation-pass-2026-09-21/intake.
Fetch before explicit-owned-file staging/push to ASTRAReworks; verify origin HEAD.
No git add-all/reset/clean/force. No task-owned browser tabs were opened.

## Historical details

Full prior ledger archived in reports/full-backlog-2026-09-21/ledger-through-rehost-20260922.md.
Earlier UI/map archives and source/hash/runtime receipts remain in the report folder.
Read relevant evidence on demand; do not resume archived jobs. 98 existing UI/kit
movies include96 with engine audio; audio has not been listened to. Conditional
service UI construction is not real auth/matchmaking proof. Physical hardware,
human feel and unavailable external services remain precise qualification limits.
