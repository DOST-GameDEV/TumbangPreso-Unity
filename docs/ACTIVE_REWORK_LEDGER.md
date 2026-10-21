# Four-round halftime checkpoint, October21

Owner corrected current symptom to no replay, not HIGHLIGHT UNAVAILABLE, in the
Unity Editor. Actual Eskinita round2/4 opens Active=true, IsHalftime=false, ClipId0
despite retained footage. Old default minimum6 explains this ordinary break.
Owner explicitly approved halftime in four-round matches. Default minimum now4;
Payo retains2. New native case calls the real host BeginIntermission path and
checks nomination, drawing, standings, round3 and final-round exclusion. Existing
eight-round shared-deadline control remains. Evidence:
docs/reports/four-round-halftime-2026-10-21.md.
Native guarded job session54758 is terminal, profile halftime-four-round-1021,
Logs/halftime-followup-1021/four-round-fixed.xml/.log:2/2 passed, no failures or
skips. Inspected native replay frame. Guard exit0 restored1 shared Editor input
preference; the new named profile had no existing files. Concurrent Nemu edits,
staged deletions/renames, Auditor and Recovery are unrelated and preserved.
No player build. The two owned source/test file hashes stayed unchanged.
Passive observer stopped. Normal Editor was clean and idle on SplashScreen,
closed for tests; reopen after the guard restores profiles/preferences.
Next: publish only owned halftime files to origin/BugFix, verify remote HEAD and
reopen the normal Editor. Actual peers and saved older-clip art remain open.

# Actual halftime rendering checkpoint, October21

BugFix after184bc9612. Actual owner host log reproduces Unsupported recorded
field with saved Blindfold data. Added disabled playback-only Blindfold drawing
and refreshed prop pose tracks when generated hierarchy changes; combined native
test exposed live Boulder children missing from the old root-only track.
Final graphics PlayMode5/5 pass, including combined highlight through standings,
registry/seek isolation and three timing/fallback controls. Inspected native PNG.
Evidence: docs/reports/halftime-live-blindfold-2026-10-21.md;
XML: Logs/halftime-live-1021/complete-fixed.xml. All test jobs terminal; guarded
session97884 restored72 profile files and1 shared Editor input preference.
No build. Actual new host/joiner acceptance remains open. Standalone saved clips
still hit preserved changed-Dante/missing-Boulder art guards; not claimed fixed.
Unrelated ProjectAuditorSettings and Assets/_Recovery remain untouched.
HALFTIME-HIGHLIGHT-1021 stays IN PROGRESS. Owner Editor was closed idle on
SplashScreen for tests. Next: publish this coherent fix to origin/BugFix, verify
remote HEAD and reopen the normal Editor without entering Play.

# Train announcement removal checkpoint, October21

Owner requests deleting all train/Overclock announcement code, including the
Hero Strike advance warning. BugFix after490e628e8 removes Announce,
OnWindowOpened, their calls and all three toast strings from LrtTrainFlyby.
Train/audio/shake and OverheadPassWindow cooldown behavior remain unchanged.
Owner also requested removing the distant-can reset instruction. Its assignment
is deleted; the existing reach/touch/channel test now expects an empty prompt
outside reach, with the reachable Reset Can and cancellation checks retained.
Final runtime direct compilation exits0; source absence/diff checks pass.
No Unity launch or native-test/player acceptance; preserve the recorded reservation.
All compiler jobs are terminal. Log: Logs/train-toast-removal-1021/compile-final.log.
Unrelated Nemu/replay, ProjectAuditorSettings and Assets/_Recovery stay untouched.
TRAIN-TOAST-REMOVAL-1021 is DONE. Evidence:
docs/reports/hud-text-removal-2026-10-21.md.
Next: publish this coherent removal to origin/BugFix and verify remote HEAD.

# Personal penalty feedback checkpoint, October21

Owner reports unexplained penalty markers, including attacker orange while
defending. BugFix source now routes penalty markers/sound through Hud.OnScored
after local-slot/spectator checks, instead of flashing every simulation penalty
on the current HUD. Timers, scoring, warning copy and assets are unchanged.
Original-source native baseline: 2/4 failed (other defender's marker on attacker;
missing received-own marker). Fixed-source controlled native checks: 5/5 pass,
including both roles' own warning/clearing, correct score ownership and spectator
suppression. Named profile 27 files and one input preference restored. No build
or real-peer claim. PENALTY-FEEDBACK-1021 is DONE. Evidence:
docs/reports/penalty-feedback-2026-10-21.md. All penalty test jobs are terminal;
last fixed run session23779, XML Logs/penalty-feedback-1021/fixed.xml.
Unrelated ProjectAuditorSettings, Assets/_Recovery and ReplayRetentionTests work
remain untouched. The other contributor reserves the next Unity slot for their
halftime investigation; keep the Editor closed for that reservation.
Next action: publish this coherent fix to origin/BugFix and verify remote HEAD.

# Halftime highlight timing checkpoint, October21

On BugFix after3bc970443, owner reports Online/LAN HIGHLIGHT UNAVAILABLE every
halftime. Actual retained catch plus0.8s-old host stamp reproduces rejection.
Removed0.6s receipt/1s setup rejection in favor of actual footage fitting before
the unchanged5.8s standings boundary; retry nominated bytes and preserve lead-in.
Final native4/4 pass: delayed stamp, delayed bytes, missing bytes, shared end.
Separate existing throw control failed before replay and is retained in report.
Named profile48 files and1 shared input preference restored. No player build.
Next: real host/joiner retest of the reported match, HALFTIME-HIGHLIGHT-1021.
[Evidence](reports/halftime-highlight-2026-10-21.md).

# Main integration and all-map preview checkpoint, October20

Owner explicitly requests main integration and every preview current/working.
Main 2fa53e3ee includes QoLUpdates adcd0f957, ASTRAReworks 1735f1d4b and backup
1fcb64d12's recoverable snapshot history. Current playable tree remains intact.
After initial clip restoration, the stronger all-map audit rejected all six
source receipts. Re-recorded every selectable map with stable source hashes;
installed six H.264 loops/posters, synchronized fallback cards and updated
captured-source receipts with existing GUIDs. All six current-source/media
checks and full-file decodes pass. Native 1/1 covers all six maps' actual pixels,
looping and decoder disposal. Core 796/796 reused unchanged. Test-only settings
and named profile/input preferences restored. Publication targets main.
No new player build or broad gameplay acceptance. MAIN-MAP-PREVIEW-1020 is DONE.
[Evidence](reports/main-merge-preview-2026-10-20.md).

# Glass match HUD checkpoint, October20

Request: "overhaul the ingame ui. make it similar to overwatch's simple ingame hud", pushed to QoLUpdates.
The live match HUD (TumpMatchReadout, TumpPowerReadout, MatchEventFeed, MatchMomentBanner) moves from
Toy Block tiles to smoked-glass leaning panels: slim team bar, bottom-left vitals plate with ten stamina
segments, skills bottom right, the ultimate split into UltimateSeal at the bottom centre, feed top right.
Toy Block stays for pause, round reports, match end and the held reference. Evidence:
docs/reports/hud-overwatch-2026-10-20. Open: owner verdict in play; physical phone and pad checks.
Next: HUD-OVERWATCH-1020.

# LAN over Hamachi checkpoint, October20

Report: a LAN row over Hamachi "stuck on joining". This PC is the joiner; Windows files Hamachi as Public.
A read-only audit agent traced the path: the beacon is outbound so a firewalled host is still listed, the
join ends after about 24 s with no progress shown, failures read as transport envelopes, and HubJoin.Join
could freeze on a throw. Core.JoinFailureRules now writes the waiting and failure lines; HubJoin always
ends. Core 11/11 new, join PlayMode 12/12 across three fixtures. Main checkout switched to QoLUpdates
8c4164b2a; a protocol 174 player is at C:/Users/Andrei/TumbangPreso-Builds/qol-174-8c4164b. Open: the
host PC's firewall, beacon v3 with protocol and map. Evidence: docs/reports/lan-vpn-join-2026-10-20.

# Room merged into QoLUpdates checkpoint, October19

player-room-depth-and-bots (380430039) merged onto QoLUpdates c3cd5fba8, protocol 174 (per-seat bots
change which bodies exist; Windows and Android rebuilt together). One conflict, the arrival trace lines.
Core 756/756, EditMode 44/44, PlayMode 66/75 with every failure reproduced on pure QoLUpdates or
explained. Open: two native peers in a room with an OPEN seat; the seatless snapshot request; the stale
QoLUpdates expectations. Evidence: docs/reports/room-qol-merge-2026-10-19. Next: the two-peer run.

# Match opening on joiners checkpoint, October19

Report: the start cutscene played for the host but not for joining players (room-branch online test).
Session branch claude/game-networking-lan-relay-d56db9 on ASTRAReworks 1735f1d4b; the same trace also on
claude/room-arrival-trace (room branch 3a98b6678). Eight real host/joiner player pairs (LAN and Relay,
Arena/Tondo/Kanto, ASTRAReworks and the room branch, bots and empty seats, a slowed joiner) all played the
joiner's own film in full before the host's countdown: not a network defect. The directed courts close on
round 1's taya (seat 0, usually the host). Built: an attacker's own 1.6 s closing beat ("YOU · ATTACKER"),
same film length on every peer, no wire change, protocol 172. DirectedMapArrivalTests 18/20 (two
pre-existing sole failures, identical on the baseline), arrival/ready fixtures 23/23, Relay pair on Kanto
verified. Open: verdict on the shot; Tondo/Arena parity; ARENA-SCENERY-IDENTITY-1019;
DIRECTED-ARRIVAL-SOLE-1019. Internal players Builds/cutscene-trace, cutscene-room and arrival-own are
task-owned in this worktree. Evidence: docs/reports/arrival-own-beat-2026-10-19. Next: owner verdict.

# Map preview refresh checkpoint, October18

Latest owner correction: display Tondo, Ermita and Sitangkai in place of Eskinita,
Maynila and Tawi-Tawi. BGC and The Philippine Arena stay. Both registries and
their expected-name check agree; IDs/order/camera/media are unchanged. The32-case
native evidence below predates this label-only correction.

Owner requested old Eskinita host preview replacement, an all-map preview check
and the current map names. Pulled fe1daa0d8 on ASTRAReworks. Seven selectable maps
all have recorded media; the old Lagoon is vaulted. Owner chose Eskinita, Maynila,
Tawi-Tawi, BGC and The Philippine Arena. Bayan Plaza/Sa Bubong leave selection
and enabled build scenes; source/assets stay. Protocol172 fences changed indices.
Owner requires the current stepped-alley Eskinita everywhere, including posters
and fallback cards. Its old scene is already vaulted with no active GUID route.
Names delivered in chat. Existing local art
metadata and ultimate-intro edits are unrelated and preserved. This task owns
map-preview media/receipts/cards, the five-map registries/build flags/protocol,
their focused tests and alley_chalk.mat's invalid keyword normalization. Baseline
found all active footage stale. Recorded all five at1080p/30/780frames; the first
Eskinita install caught Unity removing its invalid emission keyword at exit.
Normalized that material and recaptured Eskinita; all five receipts now match.
Primary media/cards are installed with existing GUIDs. 6,986 source dependencies
matched between primary and isolated inputs before capture.
Isolated project C:/Users/matth/Documents/Codex/work/tump-map1018 has its own
company/product and named profile. No user profile or Desktop replacement.
Final32 distinct native cases pass: registry22, media5 and selection5. The first
new EditMode fixture used a runtime API; fixed to enabled EditorBuildSettings.
Visual review found the long Arena caption outside its thumbnail; widened the
five vote/result cards, retained the font floor, moved status/confirmation clear
and added caption-width/status-overlap assertions. Final ballot2/2 repeat pass.
Current720p/1080p UI and all five native-decoded map images inspected.
All task jobs exited; no browser tabs opened. Automatic approval review blocked
temporary-project/duplicate-capture cleanup, so those verified task-owned files
remain. Logs/map-preview-refresh-1018/cleanup-receipt.json records the boundary.
Evidence: docs/reports/map-preview-refresh-2026-10-18. Source/media commit
5ac4491c2 is pushed to origin/ASTRAReworks with matching local/remote HEAD verified.
The local publication receipt retains the full SHA. MAP-PREVIEW-REFRESH-1018 is
DONE. Bayan Plaza and Sa Bubong stay disabled pending the owner's future rework.

# Dante Mountain Form checkpoint, October18

On ASTRAReworks after 4d79a33cb (protocol 171). Checked the last session's work before building on it: two real faults
(the suit read every skin under the body's root and threw MissingReferenceException after a same-frame model swap; Dante's
timed-kit passive fields broke SeanSteadyEmberTests' pins) and three test-setup lessons (bodies inside the first 0.25 s of
a throw are not tested; offline a non-local human takes knocks through the relay; the taya is clamped into the can's box).
DanteMountainFormTests 12/12; DanteDrift and DanteDriftPresentation green; Dante's UltimateIntroductionProbe grounding green
after the probe subtracts the baked lift. EditMode failures left are the pre-existing roster-drift list (Cheska, Zack, Nemu,
Sean, Phaister). The suit is plates (Logs/mf/poses_v7), forms in the cutscene on its clock, the slam was rebuilt, his
first-person hands carry the form, and his deck shows its seconds. Films sent: Logs/mf/dante_mountain_cutscene_v1 and v3.
Owner approved v3. Then: his carry, fatigue and slide clips; four real players passing every Mountain Form check direct and at
80 ms (Logs/mf/net-v2, net-v3-delay80); an in-play film (Logs/mf/dante_mountain_play_v1.mp4). Dante PlayMode 68/68; EditMode
48 failures, all on this morning's baseline list, none new. Next: his pick of the sound drafts (tools/draft_dante_mountain_sfx.py),
heavier casts, bot Dante, balance. Open items: docs/TODO.md DANTE-MOUNTAIN-FORM-1018.

# Ghost network checkpoint, October15

Pulled3fe64. Protocol163 now sends complete host-owned Blindfold/Skitter/Lookout
visual lifetimes instead of peer guesses, plus late hydration and scoped cleanup.
Actual UDP and real Relay DTLS host+2client delivery pass. Mixed9 authored models,
27 participants and6 samples each reproduce/fix render-copy bindpose order; final
native12/12, admission2/2, Skitter2/2, Core744/744. 52 distinct passing native
cases across retained runs. No active Unity/player or Desktop replacement.
[Evidence and limits](reports/nemu-network-2026-10-15/README.md). Next qualify matching163
full natural matches on separate devices; broader network acceptance stays open.
Publish this checked source unit immediately, preserving authored hero direction.

# Network refinement checkpoint, October15

Pulled ASTRAReworks26def. Fixed remote crouch/curse-reach sprint stamina and the
309-byte maximum SyncUnit allocation. Protocol160 layout retained. Native original
three defects reproduced; candidate20/21 and corrected standing-fixture1/1 cover
21 distinct cases. Core742/742. Named profile and3 shared input prefs restored;
task-generated resource/settings bytes preserved then restored. No active Unity
job or build/Desktop replacement. [Exact evidence](reports/network-refinement-2026-10-15/README.md).
Next qualify matching actual peers; hop recovery/crouch collision and broad
NET-SKILLS-1 acceptance remain open. Publish this coherent source unit immediately.

# Latest integration and owner scope, October11

Owner requests QoLUpdates merge then finish/push primary checkout's219 local
entries and provide a short chat prompt for Opus5.5 Nemu redesign on this PC.
Only Nemu ultimate+cutscene now: thorough research/direction first then implementation;
ghost parade, grey/muted world, all-player POSSESSED15gameplay seconds, retained
agency against ghost steering and eerie HUD/audio. Soraya/Paete quality benchmark.
Prompt already delivered for research; code editing begins after merged primary pull.

Resolved QoL eb2dc1556 + ASTRAa773 into protocol159. Native14876passes21/21;
22,351 inputs/preferences restored and private3files unchanged. No build/Desktop
replacement. Primary219 status entries expand224files:202format-only metadata,
14meaningful map/mode importer settings,1material and7historical review files.
202 whitespace-only files normalized with exact originals archived in localLogs.
Next publish merge immediately, fast-forward primary preserving its22 remaining
known files then commit/push substantive local changes and label historical evidence.
No new agents/chats/paid services, reset, clean, force push or old deletion retry.

# Current AI social source unit, October9

Original69472 reproduces unavailable-rig StepSocial frame consumed/no animation.
Preflight now preserves movement/gameplay keys and retires missing motion with
normal cooldown. Candidate66348passes6/6: unavailable, actual Rafi dance, bounded
pending delivery and3lesson gates. All21,455inputs/prefs restore. Existing250ms
playable grace, authored motion and network contract unchanged. No player/peer/
all-natural-rig claim. Source pending immediate publication; ground1fbc already
published/12nativePASS. Desktop/G still e1c1 ordinary/protocol154/runtime4a315c5e.
Next batch actual-player surface/social observation using existing normal entry,
qualify then replace SAME G/Desktop only. Continue all natural AI/camera/lifecycle/
stutter work. No newworkers/chats/paidservices/reset/clean/force/foreground control.
Root soleowner; humanpaused laptop and retired reviewer stay undisturbed.

# Current ground-support source unit, October9

Native final74308 passes12:30new3/10shoe mesh-floor contacts at1mm,10shoe/3rotation
support invariance,8prediction/Skim and2Arena ramp/recovery controls. All21,453
inputs/preferences restore. Original30pairs fail45-91.8mm; firstgetter candidate
30pairs pass but broader9PASS1FAIL exposes held-rotation dependence. Final projects
mesh support into upright root space. Initial wrong3companion selector excluded.
No map/hero art/kit change or new build. Desktop/G still ordinarye1c1/protocol154.
Publish ground source/proof immediately, then investigate missing-emote/social
holds as a concrete AI idle hypothesis before the next batched build. Actual
Windows/slope/natural attachment, bots/cameras/lifecycle/stutter acceptance open.
Root soleowner; laptop humanpaused/reviewer retired. Private3files preserved.

# Current Desktop and next PC unit, October9

Desktop and SAME internalG now ordinarye1c1/protocol154/runtime4a315c5e,
261files/3,572,707,876bytes. Only17 Desktop files changed, all261SHA verified.
Actual startup5.725s/firstHome4to40, all7 first/repeat previews, rooted36/9,
old/scenery/FX/fresh replays and full191windows/74accepted/26consequences pass.
Actual saved doll seat5 body+shoe11objects/48held samples,0invalid object warnings.
Controlled record incomplete/staged player-history warnings stay explicit.
Two completed short natural records have10segments each/no warnings.
All21,451 build/source inputs and player preferences restore; jobs all terminal.
Performance remains open: loading peaks646/873ms, firstHero35ms. No tournament claim.
Next import the prepared actual-mesh/new3 loose-shoe contact probe from Logs,
run baseline then fix only a demonstrated ground float. Preserve old-map art,
finalized heroes, private3files, profiles/saves and current Desktop. Natural doll
lifecycle/Windows doll-phase drawing, bot efficacy/camera/full-match/peer/pacing
remain open. Root soleowner, laptop humanpaused/reviewer retired. No newworkers.

# Current PC unit, October9

Companion recording source is ready for publication: actual original fifth-seat
slipper1FAIL, candidate/final3PASS PlayMode plus separate connection admission1PASS.
Format15 records doll bodies/holders; protocol154 excludes old decoders. All21,451
source inputs/preferences restore. No active Unity/player/helper remains.
Desktop001d/runtime2005 and internalG0e98/runtime3e6b remain unchanged.
Next publish this coherent unit, replace SAME G only including ef6 title timer,
then qualify actual Windows startup/replays/previews before Desktop replacement.
Natural doll birth/despawn/all-companion/peer and full tournament acceptance stay
open. Root owns work; laptop human-paused and reviewer retired. Preserve private
3files and old-map art. No reset/clean/stash/force/newagents/foreground control.

# Active competition checkpoint, October 8

Goal ACTIVE. Root is the sole owner after the human paused the laptop and
transferred its work here. The requested Astra reviewer is finished and retired.
Do not wake either or create agents/chats. No LAN, foreground control, resets,
cleans, stashes or force pushes. Preserve finalized kits and old-map art.

Current G is ordinary0e98b4721/runtime3e6b,261files in the same folder.
Current Windows69040 passes36copied-rooted clips/9rigs/no sampling warnings.
Full44388 passes191windows/74accepted/26required effects/zero sampling warnings.
FreshHero639270754435174732 andClassic639270754060454868 each have10clean scenery
segments. Controlled invalid-recorded-object warnings also occur in older routes;
source identifies a fifth-seat doll slipper incompatible with four-seat codec.
Qualify and repair that recording path; do not redesign Soraya's authored kit.

Currentstartup72768 FAILS before fresh playback: main visible only4.788572seconds.
Desktop001d/runtime2005 is preserved. Timer now starts after the admission frame;
native66104 passes2normal/500ms-delayed admission controls and first-Home4to40.
All21,449 inputs/prefs restore. Five-second constant/assertion tolerance unchanged.
Publish this source unit; ordinary Windows rerun and current replay/previews plus
Desktop replacement remain pending. See visible-title-admission-2026-10-09.

Held tsinelas source0e98 fixes the7.87mm extra spacer. Native71516passes6controls
and90pairs/9heroes/10selectable shoes have0extra mesh-to-anchor gap. Ground/air/all
kit transitions, full natural attachment, AI/cameras/creative new3 and stutter
acceptance remain open. Old-map art/private3files are untouched. No activejobs.

ba28 ordinaryWindows G/runtimeea534 is REJECTED after actual191route/74accepted/
26required consequences:1,991 new nonlegacy sampling warnings in Amihan copied
victims, versus0previous001d. Desktop001d/runtime2005 remains installed. Focused
Track.Clone fix retains only disabled controller-free Animator endpoints at the
source sampling roots. Native65784 passes9controls/36current-rig copied clip
samples/noautomatic pose changes/no gameplay scripts; all21,447inputs restore.
Actual fixed-player zero warnings and moving bones plus status/carry/replay and
package performance remain pending before another SAME G replacement/installation.
No active Unity/player/helper remains. Evidence: reports/render-copy-sampling-2026-10-08.

New source/data unit restores omitted yielded roster outline/motion preparation
before Home readiness and adds9 absent RootedAnimation sets using existing
authoring code. All23 previous sets retain exact bytes. Original68016 reproduces
ready-before-outline failure; candidate48600 retains2PASS2FAIL for missing motion.
Candidate70708 and final timing-enabled69820 each pass5 native controls:41models,
145readablemeshes,84retainedmotion sets,no actors/graphs/material changes,36actual
current-rig pose samples and actualstartup5secondgate/firstHome4to40. Final native
inputs21,445 restore; no new build/Desktop update. Qualify current-player latency,
natural status/carry/replay and remaining fullscope before next existing-G release.
Evidence: reports/home-roster-preparation-2026-10-08.

Current Desktop and internal G are ordinary source `001d80b568`, protocol 153,
runtime `2005a5d0`. G replaced the previous diagnostic in the same folder; Desktop was
updated in place with 34 changed files and all 261 file hashes verified. No new
build/Desktop folder or profile/save change. Build/native runs restored all
21,425 frozen inputs/preferences. Evidence: reports/windows-batched-replacement-2026-10-08.

Current same-binary native menu ABA runs66940/28080/53308 are all terminal0,
14PASS and preferences restored. Actual logs confirm normal ClientWorkerJobs,
experimental Direct and normal again. Loading maxima1099.490/818.523/832.293ms
and fresh first-Hero39.683/44.624/73.407ms reject adopting Direct as a remedy.
No production/settings/build/Desktop mutation; prior35.362ms is not a consistent
upper bound. Older Development Render Thread has individual215-218ms texture
uploads, but main/render frame indices and current resource identity remain
unproved. Next isolate preload resources/upload paths; all tournament scope stays
open. Evidence: reports/loading-render-modes-2026-10-08.

That ordinary candidate completed the 191-window controlled release route: 74 accepted actions,
26 required consequences and two distinct natural custom one-round/30-second
completion records. Rago's two owned empowered throws include load, flight,
matched contact, consumption and 2.291 m movement. Both completed recordings
contain ten scenery segments without recorder warnings. This does not establish
full default matches, all-map/kit efficacy or tournament performance.

Original G at1570 failed actual self-recorded scenery playback with 1,389 owner
exceptions despite its narrow UI probe passing. This failure is retained. The
replacement passes actual old-recording playback with all five animal hashes
matching and zero exceptions, startup5.693s/firstHome4to40, legacy/fresh-effect
replay and all seven first/repeat1080p previews. Windows four hand/copy controls
also pass. These graphical/native checks do not establish human or peer quality.

This unit binds a shifted sibling path to a unique matching authored AmbientLife
owner, retaining model/art checks and rejecting ambiguity. Rendering exceptions
pause and report once; package observation checks viewer errors after opening.
Three native owner/GPU controls and the corrected warning-count control pass on
identical production bytes across two runs. All 21,411 inputs/preferences restored.
Compiler-helper, unrelated round-start and informational-log fixture failures
are retained. Exact evidence: reports/replay-owner-binding-2026-10-08.

Editor/player art mismatch is attributed: 28 distinct aspin component rows agree
except five material CRC values and aggregate. Mesh streams, indices and texture
identities match. Actual Windows values match the saved Windows recording;
strict validation/serialization are unchanged. Two independent editor processes
match each other. Future engine/shader compatibility remains unproven.

The earlier G Development diagnostic was `cf9afad38`, runtime
`df1e584b`, 403 files and 3,647,235,596 bytes. Deferred Hero preparation reduced
its measured first-open peak from118.399ms to50.302ms CPU, still a noticeable
spike. The method-entry experiment worsened the peak and stays off. Candidate3
now renders the actual hidden preview before reveal and preserves Home ambient
lighting. Native56940 passes four graphical/lifecycle/routing controls and all
21,425 frozen inputs/preferences restore. Actual player43760 completes14 timing
windows: first Hero now36.968ms wall/36.883ms CPU with no frame over50ms and one
over33ms. Its worst frame is still34.735ms inside PrepareScreen, with120 retained
Mono.JIT samples totaling24.882ms; nested samples are not additive. The completed
14-trace exporter also restores all21,425 inputs/preferences. These diagnostics
include collection overhead and do not establish ordinary-release smoothness.
Candidate4 source prepares UI under an active transparent CanvasGroup, with input
blocked and the preview component/camera disabled. It replays the authored
entrance at reveal. Native49756 passes all four graphical/input/focus/lifetime
controls and all21,425 inputs/preferences restore. The completed trace's199
Mono.JIT rows have no method metadata; the metadata exporter is qualified but
the remaining activation cause is still a hypothesis. Separate MoveNext, Reveal
and Focus profiler markers will resolve it in the next batched comparison.
These changes are now in the batched ordinary release. Player60716 completes191
controlled timing windows with74 accepted actions and26 required consequences.
Hero first opening35.362ms and loading919.843ms remain smoothness failures.
The startup/old-scenery, legacy, fresh-FX, current new natural-Hero playback and
all7 first/repeat1080p movie gates pass. The fresh Classic/Hero records each have
ten scenery segments and no warnings; controlled-rebind warnings remain separate.
Main loading view5.651s and first Home video4to40 frames are observed. All current
jobs/preferences restore; build restores21,425 frozen inputs. Desktop is replaced
after those gates pass. Evidence: reports/windows-batched-replacement-2026-10-08.
Continue loading/frame pacing, continuous new3 previews/openings and broader goal
acceptance. Old-map art/presentation remains untouched.
Preserve Desktop. Previous ordinary first-Hero134.53ms/preload877.67ms remain
failures; functional checks alone do not close performance or AI quality.

Only replace the existing G build folder for future builds. No additional named
or numbered output directories. The cleanup of 38 verified superseded internal
builds, 103.71 GiB, was rejected by tool policy: deleted zero, reclaimed zero.
Do not bypass the rejection. Preserve G and Desktop until a qualified swap.

The unqualified laptop cache snapshot `455444bc` is preserved and unmerged.
New3 entrance cadence now follows actual visual-root travel using the existing
body stride calibration, fixing measured cumulative overstepping on all twelve
observed paths. Candidate50836 passes three automatic Hero openings and22380
passes three existing rendered support/grip/Taya-framing/return/cancel controls.
All21,425 frozen inputs/preferences restore. Original2green/1red per-frame checks
were too weak; the original CSV totals expose1.06–4.27m drift on all twelve paths.
The compiler-fixture failure is retained. Old-map/Arena presentation is untouched.
No build for this unit; full stance-foot, roster, continuous taste and current
player/performance acceptance remain open. See reports/new-map-walk-cadence-2026-10-08.
Continue AI engagement, every role/feature/map, natural slipper/air/slope and taya
camera behavior, creative new-map openings and preview work. TODO is the only
status queue. Exact refs, jobs, dirty work and next action remain in
Logs/catch-framing1007/active-checkpoint.json. Private Auditor/voxel and Arrival
status-only work are untouched. All current native/player jobs are terminal and
restored; no task-owned browser or server remains.
