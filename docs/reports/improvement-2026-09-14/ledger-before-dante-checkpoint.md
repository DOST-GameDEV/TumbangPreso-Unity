# Active TUMP rework ledger

## Current checkpoint: distinct armor markings and passed peer cases

All five actual3process Dante v3 cases PASS with150ms each direction: Stomp,
Long Tremor, Carapace, Heavy Plating and Fissure. RuntimeSHA
88f1cae6fca88b4ae23966da6af2ac8c2778981356afce1cddf44cc632f9d11f.
Controller20292 and its processes have exited. Reports/CSV copied to dante-evidence.
Latest clean-orbit/quake/initial-overlap pass7/7, profile59e73222ceaa.

Fitted armor now has distinct role-specific cracks, plain shoulder planes and no
noisy texture. Orbiting shield shape/drawing/material/motion stays unchanged from
the approved baseline (source saved Logs/dante-approved-orbit-source.cs). User now
allows optional shield refinements if useful; none were needed for this correction.
Distinct-armor-v1 PASSED1/1, profilec92092c0fb7d. Its front angle was obscured by an
orbiting shield, so rerender the same single case from front-left to actually see
the fitted armor. Current run Logs/dante-distinct-armor-front-v2; pid file same stem.
No C#/imported edits until it exits. Then inspect images, make a fresh INTERNAL v4
binary with these cosmetic markings, save/push bounded checkpoint and continue
Phaister/remaining queue. Do not rerun all5mechanical matrices merely for marks.

Standing no-repeated-pattern/kit-art rules are in all relevant design docs and
requested persistent memory update notes under memories/extensions/ad_hoc/notes:
2026-09-14-distinct-tump-skill-art.md plus 2026-09-14-tump-shield-permission-update.md.
When eventually giving a final response, cite these memory files if used; current
work remains active, no final/handoff requested. No other chats or agents.


**Latest shield permission:** the owner allows visual refinement of Dante's
orbiting shields if it clearly improves them. The approved current design remains
the baseline; do not change it gratuitously. Fitted armor's repeated markings are
the active correction. This supersedes older absolute shield-lock language below.
The distinct-skill/part-design rule and rejection of noisy textures still apply.

PERSISTENT MEMORY SAVED AT OWNER REQUEST: each hero kit may share a theme, but its
abilities must have distinct forms/motion/staging/details. Never paste one texture
or pattern over all skills. Distinct markings per component also apply. The user
rejected Dante's noisy repeated texture; approved orbiting shields stay locked.
Saved memory update: C:/Users/matth/.codex/memories/extensions/ad_hoc/notes/2026-09-14-distinct-tump-skill-art.md.
Also recorded in AGENTS, Art_Direction, ABILITY_REWORK_PLAN and cultural direction.

## NEWEST PRIORITY: Inday deferred by owner

The owner explicitly says park Inday FPP in TODO and do other work NOW. It is
recorded at the top of TODO.md and AGENTS.md. Do not keep iterating those arms.
Direct-source copy v1 passed2/2 character switching + actual quick/held/moving
throws. Uniform full-reach source copy v2 authored successfully, scale2.961006;
its framing is not visually reviewed, and remains deferred. Author has exited,
profile receipt7e665ccf9969. All Inday guard reconstructions below are superseded.

NEWEST USER CORRECTION: ORBITING SHIELDS are approved and stay unchanged. The fitted
armor's repeated Y/zigzag drawing is reopened. User explicitly forbids reusing one
pattern/drawing across everything and asks this in AGENTS and all relevant docs.
Recorded in AGENTS, Art_Direction, ABILITY_REWORK_PLAN, PHILIPPINE_ABILITY_DIRECTION.
Next source edit after current Editor exits: preserve profile4/its crack drawing
and orbit motion EXACTLY; change only MakeStone's fitted-armor seam layouts by
piece role, using distinct fractures and some plain shoulder areas. No newtexture.
Current approved source will be archived in Logs before editing for comparison.

Current controller20292 still builds/runs v3 mechanics; read
Logs/dante-finalize-v3-state.json. Do not edit C#/imported assets while Editor runs.
The later armor-only cosmetic change does not invalidate v3 mechanical evidence,
but requires its own image check and an updated internal binary.

SHIELDS NOW APPROVED AND LOCKED BY OWNER: "the shields are good u dont have to
touch them anymore". Preserve the3orbiting carved shields exactly. Their design, texture, size and
animation are locked. The later user correction reopens fitted armor markings. Latest
approved reference: Logs/dante-clean-orbit-ward/{normal,heavy}-body.png and clean
orbit motion. Continue earthquake/functional/network and remaining full queue.

CURRENT: Dante skills, uncommitted, newest owner steering applies.
OWNER EXPLICITLY REJECTS the added noisy stone texture. Prefers the preceding clean
charcoal shapes, asks to improve their form/motion instead. DanteStoneSurface.cs
and meta are DELETED; all overlays/calls removed. NEVER restore that layer after
compaction. Owner references saved under ArtSource/dante/owner-feedback-2026-09-14.

OWNER ALSO REQUESTS more obvious orbiting protectors ON TOP OF fitted Carapace.
Implemented3closed carved shield stones orbiting a sibling root under the motor,
with opening/closing movement, bob, molten seams. Retains fitted8piece armor. All
pieces hide only from wearer FPP. Main ward OnDestroy retires sibling orbit root.
Latest test checks3moving protectors, no colliders, radius, all-camera visibility
and complete cleanup. Plain cubes remain rejected; authored orbit is now explicit.

OWNER REQUESTS restrained earthquake feeling for nearby people. Implemented
CameraRig.BeginGroundRumble with2.4s smooth translation under1.1cm, no aim rotation,
no timescale or gameplay RNG. Two decaying aftershocks at.95/1.65s. Fissure proximity
falloff18->2m, contact punch reduced.8->.30. Pillars rock at.16degree maximum and
four low fault stones rattle in place. Original character/environment geometry is
not moved. Dedicated camera/hold regression and real ability capture pending.

COMPLETED: Logs/dante-clean-orbit-earthquake-v1 PASSED7/7, guard59e73222ceaa. Tests:
new orbit/ward, quake bounds/aim/expiry, actual Q including outward shoe flight,
pillar gap/expiry, initial-ground-overlap/wall response, existing hit-freeze anchor,
all3Dante actions with ordinary motion. Editor exited. Corrected actual outward
shoe travel10.79999m (not the prior invalid8.13m teleport); caster0, target.848m,
contact.30364s. LongTremor target/shoe0, triptrue, contact.30481s.

ACTIVE FINALIZATION CONTROLLER Python20292: Logs/run_dante_finalize_v3.py. It
builds INTERNAL DanteSkillReview v3 and runs the five delayed three-player cases,
stopping on any failure. Exact status Logs/dante-finalize-v3-state.json and
stdout/stderr; network output folders net-dante-{case}-v3. Do not edit imported
assets/C# during its current/queued Editor build. No Desktop update.
Encoding current clean ordinary footage is tool session44841 if still running.


IMPORTANT REAL BUG FOUND: the prior claimed8.13m loose-shoe movement was NOT valid
outward flight. SphereCastAll starts overlapping the floor, returns distance0,
reverse normal and point0. BounceOffObstacles treated that as a wall and snapped
the shoe near(0.25,0). New code resolves actual local contact for convex colliders,
skips ground/moving-away contacts, uses collider ray for nonconvex, and preserves
wall rebounds. New focused test checks no world-origin teleport and real near-wall
bounce/escape. Actual Q test now requires OUTWARD signed travel, not any distance.
Do not reuse earlier8.13m as successful flight evidence. Needs current validation.

Network v2 first Stomp case still failed because observer-owned target's late spawn
overwrote host-only initial placement. Fixed probe to initialize seat2's own motor
as well. Shared server-clock windows and actual player3 shoe setup already fixed.
NetDanteProbe additionally records orbit counts and local quake amplitudes. No
current successful five-case network matrix yet. All prior controllers have exited;
10288 stopped on the v2 failure. Latest internal binary at Builds/DanteSkillReview
is v2 with REJECTED texture, no orbit/quake or overlap fix. MUST rebuild before
current player claims. v2runtimeSHA84ca14d23dc34de4c9df144d76c2d54143f70217d972977eb3e57965f66deb3a.

Prior qualification: final-geometry-v1 PASSED3/3 (both Q warning radii, actual
contact/variants, physical separating pillar gap/expiry, realR). Wardv3 PASSED1/1.
Surface-v1 passed2/2 technically but its texture was REJECTED by owner. Its wide
observercamera was inside a tree; current review offset is(-4,4,-6) within street.
Inday stays DEFERRED. No new commits since b106c6ac. Origin fetched and matches.

Latest owner permits more demonic design and
Filipino cultural connection. Verified CCP Bernardo Carpio mountain-struggle and
earthquake motif; demonic basalt/horns/molten fractures belong to fictional Dante.
Inday remains deferred. Latest active run: Logs/dante-ward-fit-v3, Python21804.
No imported/C# edits until it exits. v1 mountain design passed8/8 related checks;
v2 passed ground, physical pillar and real3skill capture, but ward bounds1.27247m
exceeded1.25 after shoulder additions. Tightened shoulders1.10->1.02 anchor and
.48->.37 width; v3 repeats ONLY the ward fit/visibility/cleanup case.

New DanteSeismicVisual/DanteFissurePillar: fixed draped crack meshes, contact heat
and cooling, small grounded chips/dust, forward-clipped warning, actual convex
hewn/hooked pillars retaining5s collision and clear central gap. Removed duplicate
magma bursts, green flash/column, residual held-key green reticle. Dante ultimate
camera impulse now occurs at contact without early heavy chromatic/hitstop wash.
Audio refinement APPLIED to7Resources/Sfx cues from pinned b106c6ac inputs using
tools/refine_dante_audio.py --write-assets. Logsignal checks passed; no auditory
approval claimed. Source attribution unchanged. v2 ordinary owner/body clips cover
all3skills including6.5s ultimate expiry; encoding task may still be finishing.

New NetDanteProbe + tools/net_dante_matrix.py prepared for actual owner-client
stomp/tremor/ward/plating/fissure. NO binary includes it yet. Fixture now configures
only its named profile's selected build + challenge counters before normal pick
replication; profiles will be backed/restored by runner. Matrix gate checks actual
variants, charge, impact/trip, ward immunity/ownmovement/expiry, pillars/gap, peer
positions. Need fresh INTERNAL build and actual runs, not a source-only claim.
Potential next refinement: paired pillar halves could translate outward slightly
while rising to communicate resisting/dividing mountains; only after current Editor
exits and only if preserving footprint/gap. No such translation is implemented yet.


Q mechanics corrected and passed: contact .303..304s; caster rise0; default target
shove .848m and loose shoe8.13m; Long Tremor trips without launching target/shoe.
Round reset before contact cancels effect/root. Mobile Carapace cast now retains
masked walking legs;3focused contracts pass. Custom carved torso ward both variants
passes settled fit, wearer-only FPP hide, no colliders and slowdown cleanup.

R mechanics also corrected: caster excluded, fallback blast clipped forward.
Baseline caster1.621m/rear1.647m; corrected caster/rear0 and forward target3.159m.
Logs/dante-directional-impact-v1.xml2/2 passed Q and R. No binary includes these yet.

ComicPopup camera callbacks FAILED and are removed. Current PrepareView explicitly
faces popups and updates Canvas BEFORE manual Camera.Render in GameplayShots and
ImprovementEvidenceProbe. Opposite-camera front/rear/front-again pixel test PASSED
in Logs/dante-fissure-baseline-caption-v4.xml. Normal Update facing retained.

NEXT: authored ground fracture warning/contact/decay, paired hewn horned rock faces,
remove duplicate eruptions and generic green flash/column for Dante, then ordinary
owner/body motion and focused collision/lifetime/cancel verification. Current E
sculpted six-piece armor is a candidate, still needs demonic refinement and critique.
No skill is artistically complete merely because its tests pass.

Same-hero loadout refresh is already implemented in MatchRpc.UpdateLoadout(build);
older plans were stale. Preserve working behavior and qualify scenarios later.

Latest pushed HEADb106c6ac (Align backpedaling foot motion with observed travel).
Backpedal actual foot reversal baseline2/2 failed; corrected6gait contracts pass,
ordinary movement1/1 passes216/219samples. Source/evidence saved in backpedal-motion.md.
No physical movement/control changes. Strafe/turning/other motion remain open.
Internal DestinationEquipmentReview binary predates backpedal and Dante changes.

Same-hero loadout refresh is ALREADY IMPLEMENTED in current source: MatchRpc
RebindKitIfHeroChanged calls abilities.UpdateLoadout(build), with existing focused
HeroLoadoutRefreshTests. Older plans calling this an unimplemented gap are stale;
retain it and qualify actual scenarios when appropriate, do not reset live kits.

Baseline directional-locomotion-baseline-v1 passed1/1, profile5ae0402d61c2.
Berto carry/empty forward,backward,strafe,sprint,turn/start-stop each212frames,
normal-speed owner/body MP4s and pose sheets in corresponding Logs folder.

Prior SAVED AND PUSHED checkpointbc8a5f00ed88ffc8284673ed48c24cf78002d350 on ASTRAReworks,
Improve throw handling and preserve requested character restorations. Origin is
current with this checkpoint; main and other checkouts untouched. This includes
all prior source and evidence plus the explicitly DEFERRED Inday source-copy WIP.
Do not mark Inday framing accepted. No new Desktop build/update.

Throw/equipment/ghost checkpoint is now qualified for its tested scope:
- final-carry-clearance1/1 passed all18x10x6x3 actual head-surface samples.
- Fresh INTERNAL DestinationEquipmentReview build succeeded1057MB/132seconds.
  EXE6f44fe53090dad3edd9f86f5b5691b2cc8cba07deb4efd4791d7e32135385cb8.
  Runtimec49a540168f800fcde948114e9d9f3b1fc208379be0d815091d1fbfbded82b17.
- Classic3process throw and normal staged familiar matrices pass.
- Hero150msone-way delay+observer rejoin passes133active rejoined samples.
- Familiar150ms+owner reconnect passes24active field samples, matchingfield/yaw,
  normalexpiry/no charge refund. First parallel6player run loaded after expiry;
  isolated repeat fits actual7secondfield. No production rejoin change was needed.
- net_familiar_matrix now restores only3namedprofiles. Both helpers corrected
  narrow observation-window bugs; no relaxed matching/field assertions.
- No leftover Editor/player/network helper from completed matrices.
- Evidence/report: reports/improvement-2026-09-14/destination-validation.md and
  destination-evidence. Only test/helper/report edits follow that binary so far.

Next collect directional baseline, inspect owner/body ordinary motion, make only
justified movement corrections and targeted tests. Continue bounded validated checkpoints on ASTRAReworks; latest pushed is bc8a5f00. No Desktop update, agents, other chats, Figma, paid calls or resets.

## Prior Inday implementation checkpoint, now deferred

Owner rejected guard-reconstruction v6 and explicitly said to copy her actual
restored arms into FPP. This supersedes the generic block-hand frame for INDAY.
Use source left/right sleeve+hand+guard meshes/material/palette; only rigid frame
rotation, translation and ONE uniform scale for camera fit. No clamping, axis
stretching, reconstructed coral details or replacement hands. Existing actions stay.
Source's long built-in pink held object is separated from clothing; don't duplicate
it into FPP. Original restored body is unchanged. AGENTS.md top holds this request.

ACTIVE: IndayFppArmAuthor v1, Python PID18968, log/stdout/stderr/pid at
Logs/inday-source-arm-author-v1. No imported/C# edits until exit. New author copies
all rigid arm triangles except connected long prop, keeps original normals/UVs,
uniformly frames at .40m cross-section, tip at .84m animation reach. Runtime
UseIndaySourceArms uses those whole arms and returns before generic reconstruction.
Resources/Models/FppDetails/inday_{left,right}_arm.asset are the new outputs.
Old FppGuardAuthor and guard assets/build method are still present but superseded;
archive is Logs/rejected-inday-guard-reconstruction. Remove their inactive path
once the direct source mesh is verified so it cannot reappear.

Newest completed hands-guide-final-v4 passed2/2 (profile f89e9af7468e): all18 FPP/
body captures and direction guide through real ReadyGate countdown in all3maps.
Images Logs/fpp-restored-guard-v6 are REJECTED for Inday, other characters useful.
Nemu cuff overlap corrected; Phaister stripe gold, matching body, not red.
Guide visible contributes562..672 changed pixels at1280x720; actual on/off files
and active-match HUD in Logs/aim-guide-active-v3. No endpoint landing square.

Nemu carry solved: lowering -22/-32 failed. Explicit original-clip orientation
sweep found outward Y+20 clears all10gear/charge/spin samples. Baked author v4
(profile f0ec72c9bec8). Runtime quick/held-left/moving-right pass with zero actual
head-surface penetrations (6/82/78 held samples) in hands-guide-carry-v3.xml.
Logs/nemu-outward-carry-motion-v1 includes timestamped owner/body MP4s. Still run
normal all180 surface regression once against final bake, not the removed sweep.
Diagnostic source/results preserved at Logs/nemu-carry-orientation-*.

Ghost KuroIdleReviewProbe passed: all11fidgets, current-monster portrait, full old
mini -> retained current monster -> mini ordinary-speed sequence6.945seconds.
Logs/restored-kuro-motion-v1 has frames/CSV/MP4s. Inspected transition and idle
frames; palette helper shared with actual CharacterVisual. Earlier five targeted
mechanics checks passed. Real-process familiar cases remain before final signoff.

Latest owner model question answered from current turn_context record: model
GPT-6 Astra, effort xhigh. No configuration changes. Current session continues.

This ledger is authoritative for compaction. Continue the whole queue in this
conversation; owner repeatedly says finish. No new commit/push/build this PC yet.

### Workspace and strict boundaries

- Checkout: C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks.
- Branch/push target: ASTRAReworks only. Remote DOST-GameDEV/TumbangPreso-Unity.
- Clean destination0028b3a9 was fetched/fast-forwarded84commits to transfer
  986542f42a2129126508b20877d55d4ff35c6cec. No new commit/push yet this run.
- Other checkouts, including dirty main, remain untouched. No branch reset.
- NO agents, other conversations, usage resets, Figma, paid calls, Desktop update.
- Only tests necessary for changed behavior. Parallelize independent tools/read/
  preparation and do useful work during long runs. One Editor at a time; no C#
  or imported-asset edits during it. Every launch uses tools/run_unity_guarded.py
  with named profile equipment-destination-review. Explicit INTERNAL buildOutput.
- Preserve controller-mapping/MenuNav ownership, both first-class modes, saves,
  stable IDs, simple controls and all retained faces/outfits except specific
  owner restoration/correction requests below. No fingers or thumbs on body/FPP.

### Latest owner requests and immediate priority

1. Restore OLD Inday model from backup files. This supersedes preserving the
   newer Inday or our interim hand pilot. Preserve current animations.
2. Restore OLD Nemu SMALL ghost from backups, improve expressions and bugs, and
   transition it into the CURRENT monster. Keep the current monster design.
3. Fix both FPP hands for ALL18people to the owner's clean solid block-hand
   reference, while matching each character's skin/sleeves/details. Preserve ALL
   existing hand animations/pivots/action IDs. User also reported Inday's body
   arms broken, Zack's arms skinny and apparent thumbs in game. Fix real defects
   without redesigning the cast. Reference pictures are permanently copied to
   ArtSource/fpp/owner-hands-2026-09-14 (approved basis and two rejected captures).
4. Fix the THROW TRAJECTORY LINE, not black world/character edges (explicitly
   clarified). Show general direction, NOT an exact landing prediction. Stationary
   full hold may converge closely; early hold/movement should have meaningful
   bounded error and small shaking. This supersedes the transfer's exact guide.
5. Investigate floating slippers; do not infer a clipping failure from screenshots.
6. Then continue equipment/movement/kits/network/spectator/engineering; UI LAST.

### Setup solved on this PC

Unity6000.5.8f1/5cb7df797b7d and .NET9.0.317 match. Python3.12.8, Blender5.2.0
are installed (source PC had3.12.10/5.2.1). Needed Pillow/numpy/imageio_ffmpeg/
requests/pypdf imports exist. Official Unity CLI beta.5 is checksum-installed at
C:/Users/matth/AppData/Local/Unity/bin/unity.exe. Bare unity.cmd is an old Editor
wrapper, not the CLI. All16 portable skills extracted to Logs/portable-skills-
2026-09-14 and all225 file hashes/sizes match manifest; current equivalents already
installed. No newer skills overwritten and no Figma calls.

Unity UPM failed before compilation because this task shell lacks ALLUSERSPROFILE.
Supplying existing local ProgramData fixed it with the original cache. Cache move
did not help and was reversed. Guard now repairs that alias only in child env;
6focused runner/profile-preservation tests pass. Manifest/lock/local core+transport
packages unchanged. WORKSTATION_SETUP.md records actual paths.

### Current implemented source, still under verification

Restorations: tools/restore_backup_characters.py reads backup4 pinned82524c7537fc5fcff00ebb845ee4c360acd468cb.
Live backup/backup-2/backup-3/backup4 refs all contain identical chosen assets.
Inday old mesh geometry is restored with current33animation clips, identical
nodes/skins/bind matrices/palette. Digest46c7921d3937b14cf6ebf9c8c714de9660e11d75ae729046bf5c799aa4b71050.
Metadata preserveOwnerBackupGeometry prevents the two rework authors from
changing her back. Full originals are backed up in Logs/before-owner-backup-restoration.

Old small ghost408vertices/204triangles restored at original rest positions,
split into its original17named boxes for expressions. RestoredCalm lives below
CalmForm in the CURRENT combined pet GLB. Eyes parent glints/pupils for complete
blinks; existing expression overlays repositioned. CharacterVisual applies the
old Nemu palette to this atlas-based calm form. Entire current RageForm subtree,
geometry/materials/skin preserved: ef5a0d18520256b81d287bd191c6bda64f945e9c86c51b71b00fbd957fc44373.
Rage animation/source clip unchanged. Do NOT replace whole pet GLB with old file.
Logs/owner-backup-restoration.json contains exact receipts.

FPP candidate: ApplyCharacterStyle now uses retained Models/viewmodel_arm clean
two-block frame and existing character-specific skin/sleeve/accessory builders,
not raw extracted body gauntlets. Extra Sean muscular mesh and duplicate
Nemu/Phaister hand blocks disabled. Pivots/actions/.84m reach unchanged. Actual
body identity still resolves in MatchCharacter. This needs fresh visual approval;
no quality claim yet. Raw UseRosterArms/ApplyRosterArm private methods and derived
assets remain retained but inactive; clean unused code after accepting new route.

Zack body hand-depth pilot retained: tools/author_hand_volume.py increases
depth/width .47 to .84, refits existing wristband, preserves all animation bytes.
Only Zack remains changed by this pilot; Inday pilot is superseded by restoration.
Hand source backups/receipts in Logs/hand-volume-source-backup and hand-volume-applied.json.
Other18inspection is a dry-run, not a mandate to rewrite other bodies. Sean hand
selection was corrected to avoid a thin skin-colored inset masquerading as hand.

Equipment WIP: stable10IDs; Alpombra2/2/5, Heels3/5/1, Loafers2/4/3 remove
dominance. All rows total9; other slipper rows unchanged, standard3/3/3. Recovery
.18/point now shortens pickup lock and visible aim settle time; flight stays5%.
Can rebound .22/point now reaches actual host can-hit return velocity/lift,
including protected bounce without score. Reset/stance/person tuning unchanged.
Descriptions corrected. Core15/15 and original Unity equipment3/3 passed.

New aim WIP: fresh drift1.45degrees, movement2.4/unit, residual.07, equipment
settling retained. Nominal guide separated from actual release aim. Reticle shows
nominal aim. New short .24..74second guide removes landing square, uses real scene
segment hits, ignores disabled bot brains, and has fine dark-edged warm stroke
with a Resources/UI/AimGuide.shader. Core13/13, PlayMode4/4 passed, but captured
guide was visually weak/almost invisible. Need actual on/off rendered evidence,
post-render enabled-state checks and genuine active-match capture before acceptance.

Body motion WIP: lowered/wider preparation(-16,38-22spin,-44-10spin) replaces
original(-26,35-22spin,-28-10spin). Original actual-surface test found Maring's
full-left shoe intersections; candidate removes them. Nemu neutral hold had8shoe
intersections, so new CarryPoseAuthor bakes isolated holding-right correction at
Resources/CarryMotion/team-nemu.asset; CharacterAnimator loads it. Still unverified.
Earlier AABB tests falsely counted air inside hats/hair. Actual HeadSurfaceVolume
test helper has a known separated-cubes inside/outside/gap regression passing.
Tests/Support test-only assembly is shared by Edit/Play; motion probe now uses
actual rigid surfaces. Remove unused old HeadVolume helper methods after acceptance.

Floating slipper finding: FppHandsReviewProbe called HostDisarm directly, which
clears possession/velocity but intentionally does not land. Its own capture left
the shoe at hand height. Fixture now places it on nearby ground. Production
HostForceEquip already grounds displaced shoes; round reset owns its own placement.
Do not patch flight clipping to cure a test-created hover. Actual drop/retrieval
checks and ordinary play remain required.

### Active processes and exact next step

1. Collect IndayFppArmAuthor v1 (PID18968). Inspect real whole-arm FPP/body capture.
2. Preserve source correspondence and existing actions, remove rejected guard
   reconstruction fallback. Run narrow all180gear/head and roster/action contracts.
3. Build fresh INTERNAL Windows player with explicit -buildOutput, then targeted
   real-process throw/equipment/ghost/drop-retrieval in both modes. No Desktop update.
4. Clean only verified test dirt, save bounded validated source/evidence commits
   on ASTRAReworks, fetch/review remote before push. No branch reset.
5. Continue movement/kits/network/spectator/engineering and UI last, below.

Prepared Logs/clean_destination_test_dirt.py is DRY RUN only. It excludes changed
Inday/Zack baked meshes, verifies all non-tangent channels, and normalizes only
byte-proven line endings. Apply only outside Editor runs. Never blanket-restore.

### Remaining work after immediate art/aim/equipment batch

Remaining movement/carry/sprint/backpedal/turning/foot/recovery/interruptions;
all6hero kits+alternatives (preserve polished work, distinct ultimate moments);
actual multiplayer/loadout/reconnect/rematch/host-loss and recovery input paths;
Phaister warning/curse/unbinding/11m moon under8m guideway, restored-mini/Kuro
staging/visibility; tournament free/follow/POV and replays; relevant TODO152/152.4
request/event/lookup/AI retrieval/lunge/performance. Then finish inherited UI LAST.
UI pending f5b10 patch still unapplied; PauseEscape child-settings flow still
uninvestigated. Transfer-checkpoint.md and UI_REMAINING_TODO.md retain full scope.
No routine full suites and no new Desktop build. A fresh INTERNAL build with
explicit buildOutput is required for new real-process claims. No new player
build/commit/push yet; commit/push stable verified batches to ASTRAReworks.

Other evidence: rejected FPP baseline all18 in Logs/fpp-hands-review-v1;
roster-arm-audit.csv/material diagnostics; source-PC bad Inday/Zack FPP images
also exist in throw-aim-evidence, so the rendering defect predates this PC.
Pure tangents-only import dirt in RosterArms must be byte/semantic-verified and
backed up before restoring; never blanket-reset generated assets or package data.
