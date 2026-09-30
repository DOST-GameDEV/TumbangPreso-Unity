# Active Rework Checkpoint

Updated 2026-09-30. Branch: ASTRAReworks. Current unit: FEEDBACK-0930.
Current protocol:97 in [NetSession](../Assets/TumbangPreso/Runtime/Net/NetSession.cs).
Read [AGENTS](../AGENTS.md), [task routes](README.md) and the [queue](TODO.md).
TODO is the work-status queue; this file records resumption and evidence boundaries.


## Shared Ownership

Diddler cloud unit complete: Nemu's 10 percent basic-cooldown movement bonus
shipped in 770162fed59f241de4e07d8e1d8599a861ff4639. Native before/after checks
reproduced 7 failures, then passed 8/8 after the correction, with frozen inputs.
No actual-peer or rendered-player qualification is claimed. The broader Wiki
Feedback row remains open; the owner has now approved short tested-fix notes.

Shared 25-second basic cooldowns shipped in 6b4e1d4ffa02d3976d57efee55b932d97d9c8d2c.
Four native cases reproduced separate 40/30/30 clocks; the final 18-case fixture
passes, including accepted-cast callbacks, latest refusals and reordered receipts.
Actual peers remain unqualified. HeroKit and SkillReceipts edits for that unit are
finished and released.

Kuro: Sit shipped in 9d2cc308ad70e6cc96cac7c6af208b3bc4f49ab7 and was integrated
with the unchanged incoming tutorial tree in 9a32f528. Ten native lifecycle and
real-companion cases pass, plus 18 cooldown/receipt regressions. Protocol94 is
required. The combined tutorial candidate is not yet natively qualified.

Kuro Fetch delivery shipped in e1ed3d775e9a920049ae8b658bc20bc46a012545.
Nine focused native checks pass, including real pickup, cancellation, changed
ownership, defender interception and observer authority. No actual peers claimed.

Owner update: all ability work is now reserved to the owner for manual work.
Diddler releases NemuHeroKit, NetSession compatibility, all Nemu fixtures and
ability-contract paths. Do not continue Fetch/Catch/Haunt or other hero changes.
The owner directly approved short Feedback-row updates; the Nemu partial fix note
is written and verified, with the broader row left unchecked.

Map-vote correction76cd07b7 shipped through d54488bb, remote HEAD verified.
Thirteen packet cases and four existing rematch/intermission cases pass. Incoming
controls8a85bb12 are preserved unchanged; two ready/countdown cases pass on the
integrated candidate. No job is running. Actual peers remain unqualified. The new
Feedback row was added after explicit owner approval, preserving a native checklist
and hidden QA_TUMP_0038 range. Its shipped note governs intake; no duplicate work.
Map-vote handler/test paths are released.

Tagged replay duration shipped in cebc6963c20160169c385aca6d22bdbf95fae436.
Five native timing/exit cases pass; first cold-scene timeout and one bounded retry
are recorded. Capture-helper frames show recovery, not verified replay composition.
The duration Feedback row has its short shipped note. Contact fidelity stays open.
The live Feedback completion-destination row now contains the human-labelled answer
"IT SHOULD RETURN TO THE LOBBY INSTEAD". Preserve the local tutorial reservation;
its next contributor can reconcile this existing answer instead of asking again.

Live pose-history discontinuity fix shipped in86ad3ef777b6e02a3a3377f20373f6dd0915bf76.
Five native failures reproduced phantom teleport movement and early visibility;
all9 final cases pass. The same contact Feedback row has a partial-resolution note.
Full visible-contact proof remains open. Pose-history paths are released.

Ordinary throw charge shipped in cc6476be2c2f112c8b088510e5e3433e4aea896a:
1.25 seconds to full power, with8 native carrier/observed/release cases passing in
both modes. Protocol96 requires matching builds. Its Feedback row is updated;
landing-circle replacement and clearer charge feedback remain open. Charge and
NetSession compatibility paths are released.

Practice attention correction ae7a4aa8 is validated:8 native attention cases and
an additional1/1 actual-popup capture check after incoming181015d3 integration.
Both window shapes were inspected; automatic Tutorial selection stays visually
neutral until pointer/navigation attention. Incoming glyph/loading code is intact.
Practice paths are released after this integration is published. No job is running.

Owner-requested Feedback table extension now includes Human verified between
Done and Bug or feedback. All43 current report rows have unchecked native human
checkboxes; only the owner/testers confirm those after playing. The header width
was repaired after the owner's screenshot showed mid-word wrapping. Existing
Done controls, reports, screenshots and hidden IDs were preserved. The tab's wide
layout exceeds portrait PDF width; the requested new header/control column was
visually checked, not a claim that the entire wiki is print-ready.

Offline menu pause shipped in88b0f2f4, integrated with the unchanged incoming
tutorial exit inafb79502, remote HEAD verified. Six native cases pass; the same
Feedback report contains its short shipped note and hidden QA_TUMP_0039. Human
verification remains unchecked. Repeated intake recognizes the evidence and does
not create another fix. PausePanel/Hitstop/test paths are released.

Continuous replay motion shipped28582ac7 through10dd3b7c, remote HEAD verified.
The same Feedback row contains the motion result; contact-gap qualification stays
open. Final and integrated native cases pass with inspected early/contact frames.

Current Diddler reservation: owner rejected the shared contact picture because
the tag does not visibly hit and requested refinement. Diagnose the actual reaching
hand, body animation and replay camera side/timing before changing motion. Preserve
authoritative hit outcomes and both views; no ability or landing-circle overlap.
Owned paths:
- Assets/TumbangPreso/Runtime/Camera/CatchReconstruction.cs
- Assets/TumbangPreso/Runtime/Visual/CharacterAnimator.TagBody.cs
- Assets/TumbangPreso/Runtime/Visual/MatchFlair.cs (accepted tag-contact presentation only)
- Assets/TumbangPreso/Runtime/Camera/ViewmodelArms.TagReach.cs (only if matching first-person correction is needed)
- Assets/TumbangPreso/Tests/PlayMode/CatchReconstructionTests.cs
- docs/reports/feedback-2026-09-30/tag-contact-readability.md
- docs/reports/feedback-2026-09-30/tag-contact-close.png
- docs/reports/feedback-2026-09-30/tag-contact-far.png
- docs/reports/feedback-2026-09-30/tag-contact-native.mp4
- docs/reports/feedback-2026-09-30/checks/tag-contact-far-before.xml
- docs/reports/feedback-2026-09-30/checks/tag-contact-final.xml
- docs/reports/feedback-2026-09-30/checks/tag-contact-lifecycle.xml
- docs/reports/feedback-2026-09-30/catch-replay-motion.md (capture limitation correction)
- docs/NETWORKING.md (contact-presentation contract only)
- docs/TODO.md and docs/ACTIVE_REWORK_LEDGER.md (this issue only)
Grounded diagnosis: camera already shows the correct hand side. At a valid1.65m
accepted tag, the copied hand still misses even the visible body bounds by0.4777m.
The arm-axis hypothesis is ruled out (palm/axis dot0.9693). Refine the real tag
pose toward its existing accepted event contact, with bounded arm extension and
matching first-person timing; preserve gameplay hit range, score and teleport.
The original motion fixture had disabled motors/default capsules; it now uses
actual grounding and the shipped capsule dimensions. Manual same-frame seeks were
also insufficient for skinned-pose review; the new film uses real completed frames.
Near/far contacts pass, exact one Tag event and limb-scale restoration pass; receipt
ordering rejects stale-contact reuse and cannot create scores. Continuous motion
remains passing. Real-time960x720 film and close/far stills were inspected; human,
all-roster, full-map and actual-peer approval are not claimed. No native job is
running. Commit this unit, integrate current incoming work, verify remote and update
the existing contact report. Preserve the local charge/input/tutorial reservation.

Local tutorial batch 2fbee1fb1 is complete and being integrated with this Nemu unit.
Its source reservation is released except the pending human completion destination.
Doc formatting is complete; contributors may update their own feedback rows after
verified shipping. Local next reservation: Resources/TumbangPreso.inputactions,
Runtime/Settings/Rebinding.cs, Runtime/PlayerInputReader.cs and Runtime/UI/Hud.cs
(shared Ready/Shove/Lunge input-context guard only), and the
relevant InputMapAndAbilityTests/InputContractTests/PlayMode InputReaderTests.
All paths are under Assets/TumbangPreso. Preserve the Nemu reservation above and
these input paths when updating the checkpoint. No cross-chat contact is required.

## Current Unit

### 2026-09-30 continuation

Owner's newest assignment is [FEEDBACK-0930](TODO.md#feedback-0930-owner-document-and-engineering-follow-through),
followed by reasonable engineering/UI/UX fixes and applicable existing TODO work.
Loading is reserved for the owner's friend (latest owner direction); do not edit
loading paths. Continue other optimization later. Latest restrictions: no subagents, resets, cross-chat work or credit spending without
approval; VFX/SFX/animation/models/maps/lighting only for demonstrated bug fixes.
Phaister/Paete abilities are protected, descriptions only. The wiki's other complete
ability/status definitions supersede old docs; blank cells are unspecified.

Working checkout: C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks.
Fetched and fast forwarded 2bacb97c3 to aecc0ee2c before editing. Existing unrelated
RosterArms assets, two protected UI metadata files, HeroHazards.cs, QualitySettings,
motion captures and untracked art/skill-FX work are preserved and not task-owned.

Shipped ef4f9a729, remote HEAD verified: Tasks retains unchanged rows through wallet refresh; ordinary
round cards hide the live HUD, center standings, fit score values and remove the
duplicate warmup line. Two focused native cases pass, with all eight frozen source
inputs unchanged. Inspected Tasks and 960x540/1600x680 standings captures are in
[the feedback report](reports/feedback-2026-09-30/README.md). Do not rerun its
unchanged cases. No task-owned gameplay edits are uncommitted at this checkpoint.

No test/build/helper is running at this checkpoint. The managed worktree tool is
unavailable from this projectless chat (Not a git repository). Validation checkout:
C:/Users/matth/Documents/Codex/work/tump-feedback-0930, detached aecc0ee2c plus
eight explicit overlays. Its native asset-import churn stays there. The first long
path failed; one retry at this short path succeeded. Old validation dirt is preserved.
Native checks use run_unity_guarded.py, a named profile, graphics, fresh XML and
frozen current inputs. Main disk had about 204 GB free on the current check.

Wiki read: document 1jvr7NLzhHrbw-wrG676AeOkoTxJf4GokkfmxpO0ddLg,
modified 2026-09-29T17:03:02.235Z. Tabs read include abilities (t.0), statuses
(t.ybspp2s3qclp), feedback (t.3aojrspccof8). Owner then authorized organizing only
the Feedback tab for QA/DOTS intake and clickable completion. Preserve original
reports, screenshots, comments and the other tabs. Owner rejected verbose forms,
requested a simple table and created How to use Feedback (t.mj90cfojs1si). Current
Feedback table: Done, Bug or feedback, Notes/screenshot. Repeated generated forms
are removed; 39 concise entries and original screenshots remain. Native named
ranges preserve IDs without exposing tracking jargon to testers. Instructions are
five short paragraphs in How to use Feedback. Native table/checkmarks and the seven
yellow human-needed rows are visually verified. The two shipped fixes are checked;
their short fix notes name ef4f9a729. Original five image IDs remain intact. Generated
forms were reduced from roughly 50,000 to 7,700 characters. F0930-21 is complete.

Latest queue rule: humans add new rows at the very top under the header. Process
unchecked rows from bottom upward. Yellow rows need a human and are skipped until
answered; the agent must continue other actionable rows. Record a short tested/shipped
fix note before checking Done. No status/priority/owner forms in the tester view.
DOTS setup and scope handoffs go in chat for manual copying; no automation or other
conversation is contacted. Tutorial return destination is still pending from the
owner; other tutorial fixes can proceed. Xelu's prompt source declares CC0.

Doc formatting/control reservation is released. Tutorial source batch 2fbee1fb1
passes its real-jump/common-completion
case and final Tab/ultimate case. Native combined captures at 960x540/1600x680 were
inspected; the earlier focus and coordinate-measurement fixture failures are recorded
in the feedback report. Runtime source stayed frozen. It is committed and being
merged with incoming authored Nemu changes, preserved byte-for-byte. The next input
native check will compile the merged candidate; no whole-candidate qualification
is inferred from the tutorial's earlier source snapshot.
The completion destination remains human-needed. Tutorial 2fbee1fb1 is shipped
through the verified 625af762b integration. Do not repeat passing cases.
Other network/bot files remain unclaimed by this unit. Reconcile explicit
ownership before overlapping work. No subagents or cross-chat messaging.

Next: default binding correction (inputactions asset and relevant input tests), then gameplay/network/bots
and other optimization. Loading remains friend-owned. Do not repeat the first unit.

Current local input edits: six keyboard/mouse default paths corrected without
changing binding IDs (Grab/Interact right mouse, Lunge/Ready F, curve up-left/down-right).
The Ready input guard consumes only a physically shared control and requires its
release before gameplay use; differently rebound controls keep their behavior.
The merged native candidate passes default collision and legacy override checks
(2/2 EditMode), plus real device/intent/context checks (1/1 PlayMode). The input
unit is being committed/pushed; no previous tutorial checks were repeated.

Next local reservation after input shipping: Runtime/UI/InputGlyphs.cs,
Runtime/UI/TumpPowerReadout.OwnerDeck.cs (binding pictures only),
Tests/InputGlyphTests.cs, Tests/PlayMode/OwnerTrainingUiTests.cs (prompt display case),
Editor/InputGlyphImport.cs (xelu-only import filtering), new Resources/UI/input/xelu prompt textures/metadata and
docs/Asset_Sourcing.md. Import only needed CC0 Xelu keyboard/mouse/pad prompts,
preserving fallback labels and device families. Loading stays friend-owned.

Current local Xelu edits: 198 original supplied PNGs copied unchanged (437,359 bytes
on disk), valid unique 32-hex metadata, whole-image caching and correct Xbox/PS
label resolution. Light/dark keyboard variants follow the background; absent wheel
variants retain the existing fallback. Bilinear filtering is scoped only to xelu.
Native import/family/cache/fallback checks pass 5/5. The actual training/live-HUD
display case passes; 960x540/1600x680 captures were inspected. The Xelu unit is
being shipped. No broad suite or unchanged input/tutorial checks were repeated.

Next local unit: F0930-07 general defender lunge, not hero abilities. Review the
current 1-metre dash and derived bot/network reach before choosing a longer safe
travel target. Candidate target 3 metres preserves the existing 0.45-second active
window and 28m/s movement-budget margin. Do not edit ability, loading or map lanes.

Latest human answer verified directly in the Feedback table: tutorial completion
returns to the lobby. Local small follow-up reserved: GuidedTraining.cs and
Tests/PlayMode/OwnerTrainingUiTests.cs. Finish/quit now use the existing cleaned
match-exit route, which opens MatchSetup's hub. No loading/navigation implementation
was edited. The real Finish-button case reproduced surviving offline match state;
the tutorial now clears its round/match directors before the existing hub exit.
Final native check passes 1/1. This follow-up is being shipped; tutorial is complete.

Latest Doc intake adds a Human verified column and new rows. Preserve that column
and all human text. The latest requested wheel directions are UP RIGHT / DOWN LEFT,
with Hold Interact removed from the visible Actions settings. Reconcile this newest
request after the current longer-lunge unit. Tagged replay now also requests three
seconds of motion rather than a short replay held on its final frame.

Rematches now allocate a fresh host world identity before reload, adopt a matching
previous/next pair on clients, scope votes/tallies and acknowledge seated voters.
Existing rotation/result policies remain. Core, Runtime and Editor compile on the
frozen managed candidate; the disk guard stopped BEFORE Tests, PlayTests and the
new focused Core case. Those checks and two authored native cases are NOT RUN.
Do not repeat the completed compiler stages. [Details](reports/stability-2026-09-27/multiplayer.md#rematch-identity-and-voting).

Intermission voting now accepts client requests instead of requiring a host sender.
Match/round scope, seated quorum and acknowledged host tally repair the existing
client workflow; buffer state mirrors without raising authoritative round events.
All five assemblies compile on1233 frozen managed files, no drift or retry. Two new
native cases are authored but NOT RUN; actual peers remain unqualified.
[Details](reports/stability-2026-09-27/multiplayer.md#intermission-voting).

The async roster-catalogue implementation is pushed. Boot awaits one shared
Resources.LoadAsync request before visiting referenced art/clips. Direct fallback,
once-only missing warning and cancelled-request handoff are retained. Its focused
test is authored but NOT RUN natively. After an initial storage-preflight block,
space recovered enough for one full managed-source check: Core, Runtime, Editor,
Tests and PlayTests compile, with1233 frozen files and no drift. This is not Unity
import/IL postprocessing, native execution or a player build.
[Exact state](reports/stability-2026-09-27/loading-audit.md#asynchronous-roster-catalogue).

The preceding first-person preload unit passed one native case on its frozen
candidate:50 source meshes retained through the consumer cache, no actors created.
The teammate integration then landed cleanly as `68bc1008`, preserving its authored
assets, shared fixes and protocol87 compatibility. Only the eleven reviewed preload
paths differed from the incoming tree before the checkpoint update. The whole
merged candidate has NOT received a new native pass.

## Implementation And Qualification

- Loading: work-driven boot/menu/match curtains, async arena entry on every peer,
  retained menu/HOME/effect/mesh data and preparation ownership are implemented.
  The first HOME decoder's reproduced30-second timeout was fixed and its focused
  retry passed. [Current loading route](LOADING_AND_PERFORMANCE.md) and
  [per-unit evidence](reports/stability-2026-09-27/loading-audit.md).
- Networking: shared identity/authority, ordered receipts, scoped ready/countdown,
  ultimate identity/duration, sentry targets, timed/familiar recovery and
  simulation-clock aging have focused evidence. [Contract](SKILL_NETWORK_CONTRACT.md),
  [source route](NETWORKING.md), [exact coverage](reports/stability-2026-09-27/multiplayer.md).
  A cosmetic rework reuses stable IDs/hooks; a new gameplay mechanic needs a contract.
- QA: [19 deduplicated comments](reports/stability-2026-09-27/qa-comments.md).
  QA-01 title blur and QA-15 Sean/Cheska freeze remain unresolved. Login feedback now
  has native field-state evidence, not merely compilation; live service, sound,
  visual judgment and physical input are separate.
- No current successful player build, whole-player hitch table or blanket
  ranked/reconnect/cross-platform qualification. Do not infer these from local tests,
  source inspection or the incoming contributor's different candidate.

## Validation Environment

No task-owned job is active at this checkpoint. The lightweight managed check
completed after external headroom recovery, using about20.24MB of frozen source
and less than its32MB output budget above a5GiB reserve. A full asset import/build
still needs additional headroom; do not retry an unchanged blocked workload.
Check actual space once when deciding
a new run, not in a polling loop. [Validation rules](TESTING.md).

Freeze a complete current candidate, isolate writable caches/profiles and use the
guarded runner. Do not reuse old partial overlays as if they were the merged source.
Run only changed cases; preserve fresh XML/hashes and the distinction between
source compilation, native state, rendered output and actual peers.

## Next Action

Continue actual loading/network/flow fixes within the latest owner scope. When
storage permits, qualify the roster handoff and the merged candidate. Keep pushing
coherent owned units; do not stop at a passing case or create a validation loop.
Keep temporary ownership notes and private overlays out of commits.

## History

The complete341-line checkpoint and every older receipt it referenced remain in
[ledger history through71396c97](archive/ledger-through-71396c97-2026-09-28.md).
Its stale counts/protocol literals are historical. Earlier instruction/method context
remains in [the2026-09-27 snapshot](archive/snapshots-2026-09-27/README.md).

Local longer-lunge reservation: embedded Core Balance/Combat/AiTuning/MatchRecord/MoveBudget/Sabotage, NetSession compatibility only, Core balance/bot/move-budget/stat tests and DefenderLungeTravelTests. Travel is 3m with speed derived from friction, tier approach and safe-emote bounds follow reach, packet ceiling remains28m/s. Native local input travel (both modes), host travel/repeat refusal and near/far sweep pass. Initial target fixture lacked held-shoe/live-match preconditions; only its2changed cases were rerun. Focused Core56passed and corrected pressure literal passes1/1. No peer claim. This unit is being shipped; its source reservation will release after push. Loading/abilities and Diddler pause paths stay reserved.

Local lunge bfe1c96ca shipped through72c7ee64c, remote HEAD verified. Lunge source paths released. Incoming offline pause is intact; continuous caught replay is Diddler-owned. Current local unit: newest Actions order/wheel request, inputactions, Rebinding, InputReaderTests/InputMapAndAbilityTests only. Hold Interact is removed from settings, retaining the actual action/saved overrides and protected kit behavior. Three native settings-contract cases and the real input case pass. This unit is being shipped. No task-owned job is running. Browser3 remains unavailable; short shipped notes are written via the Doc connector, but Done checkbox UI ticks for the newer local units are pending. Human verified stays untouched.

Newest Actions correction67554a912 shipped through82731889c. Current local reservation: TumpPowerReadout.OwnerDeck.cs and TimedPowerUiTests, UI only. Active ultimate ring currently shows objective bank instead of live duration; reactivatable skills hide lifetime behind Again. Fix reads existing shared clocks and leaves kit behavior intact. One native UI/shared-clock case passes, with2inspected captures. Main-checkout launch was stopped before testing, prior dirt preserved. Isolated fixture needed one NetworkMode correction. No gameplay/ability reservation overlap. This unit is being shipped. Next local work is the requested landing-circle UI and throw-charge clarity; Diddler owns continuous replay.

Local F0930-18 reservation: TrajectoryPreview.cs, Carrier aiming-preview accessors only, Slipper shared flight-velocity/support queries only, ThrowAimIntegrationProbe and LandingCircleTests. Replace the line with a ground circle, predict fixed-step flight/world banks, retain local camera/charge lifecycle. Shared support uses64-hit nonalloc buffer with full-query overflow fallback. Loading/abilities/replay/art remain contributor-owned. Native flight-equivalence/render/ground-allocation checks pending. No done claim.

Landing-circle runtime inputs remain frozen. Four initial native flight/support cases pass, including0allocated bytes across100warm support queries and dense overflow. Initial visual fixture was inadequate: camera faced away, then charge was attempted inside the box; direct Rebuild bypassed the real charge predicate. 858pixels verified the ring once oriented, but live-charge retention correctly failed. Current isolated job tests legal z=-8 start and asserts CanThrow/IsCharging before the actual render and release. Active session38816, output Logs/feedback-0930/landing-legal-final.xml. Do not mark circle done until this expected1case passes and its photo is inspected. Preserve initial failures and do not repeat the four unchanged cases. Charge-clarity feedback remains open after circle. Goal service now reads active, so automatic continuation is available again.

Landing circle final legal-charge/render/release passes1/1 in8.9177248s. Gameplay capture inspected;106visible pixels at1280x720. Runtime hashes unchanged. Four physics/support cases are reused, not repeated. The circle/shared floor-allocation optimization is being shipped. F0930-18 remains open for charge clarity. Next action: clearer charge feedback, then F0930-13/14 existing-feedback qualification and reasonable network/bot bugs. Current goal active. No job/helper/browser tab was created or left running; browser3 unavailable, newer Done ticks pending despite shipped notes. Human verified stays untouched.

Publication checkpoint: a458c5f4c is shipped through d7f47474337deddf541d153a89fa790f3f00198d; remote HEAD matches. Continuous-motion replay28582ac70 is integrated with its source/tests intact. All local circle/controls/lifetime/tutorial code is committed; only the recorded unrelated dirt remains. The Doc has concise shipped notes, and the answered tutorial row is no longer yellow. No owned native job is active. Local circle paths released. Next local reservation: F0930-18 charge clarity in UI/HudReticle.cs and UI/TumpMatchReadout.Reticle.cs plus a focused UI input/readout test. Read the latest ownership section before expanding into network/bot work. Keep goal active until authorized actionable work is complete; no all-feedback completion claim.

Local charge-clarity unit: actual power percent, FULL RELEASE and WAIT at the reticle; refused state now follows actual throw legality, not only restoration protection. UI only, carrier/kit behavior unchanged. One native actual charge/can-down/protection/release case with2captures pending. Latest live Feedback binding correction now requests Left Throw/Tag, Middle Shove/Lunge, Right Retrieve/Reset, wheel UP RIGHT/DOWN LEFT, F Interact/Ready. This supersedes the preceding F-lunge layout; reconcile that next with IDs/overrides/context retained.

Charge clarity accepted:1/1 native actual-charge/protection/hidden/release case passes12.0026813s; two captures inspected. Corrected the initial mistaken can-down diagnosis: current ThrowRules allows it, now explicitly asserted. Protection plus real round/actor gates feed WAIT. No throw behavior changed. This unit is being shipped; next latest binding correction is MMB Lunge, F Interact/Ready, with saved IDs retained. No job running.

Charge clarity cda86fd29 shipped throughd3e21c9d2; remote HEAD verified. Current local ownership: inputactions, Rebinding, PlayerInputReader, InputMapAndAbilityTests/InputReaderTests. Newest Doc binding request: MMB Shove/Lunge, F Interact/Ready and requested Actions order. Shared Ready consumption now applies only to the actual same control for either interaction or saved lunge, requiring release before gameplay. Other controls/pad/touch/saved IDs unchanged. Native EditMode/default-ID and real-device cases pending. Diddler tag-contact paths remain reserved.

Latest MMB/F layout is validated:4distinct native EditMode/default/order/conflict/older-override cases pass; actual mouse/wheel/F Ready hold/release case passes1/1. Initial conflict filter named an absent method, so only the omitted case was run separately; no unchanged cases repeated. All5frozen inputs unchanged. This unit is being shipped. Next F0930-13/14 existing-feedback qualification, then actual-peer network qualification with a fresh internal player if the build route preserves frozen assets. Source reservations: loading friend, all abilities owner, tag-contact Diddler. No job running.

Tag-contact candidate7eb030f7 passes its near/far grounded real-time checks and
metadata lifecycle; continuous motion is retained. Incoming80ff8b58 controls and
charge-reticle source are integrated unchanged. Final combined native check2/2
passes with frozen inputs. No native job is running. Verify remote publication
and update the existing contact row; human and broader qualification stay open.

Latest controls80ff8b583 shipped, remote HEAD verified. Existing F0930-14 can-down fix passes1/1 native upright/down/reset path on this candidate, capture inspected; no duplicate effect code. Local next reservation: Editor/GameBuilder.cs and new AuthoredAnimationBuildCheck.cs plus focused build-input test. GameBuilder currently invokes swim/recovery authoring on every build, and both author tools overwrite retained clips via CopySerialized. Change the build to validate existing sets without reauthoring; manual authoring tools remain explicit. No animation/model/map/loading files will be edited. This engineering bug fix precedes an internal Windows build and two-process LAN check. No native job running.

Build-input baseline native failure identifies missing team-amihan swim set, not a validator/tool availability problem. Runtime GeneratedMotionAssets has no fallback set for that key. Explicit missing-only repair is authorized as a concrete animation bug fix under the owner exception: SwimmingAnimationAuthor/RecoveryAnimationAuthor gain EnsureMissing, skipped existing sets, and a separate repair entry point. Regular GameBuilder still only validates. Frozen motion-file hashes record every existing set/meta before repair. Correct the synthetic test to select its x curve (Unity creates additional position bindings). Native missing-only repair and focused rerun pending; do not claim build ready yet.

Previous automatic goal turn made progress: charge clarity and latest MMB/F controls shipped, current can-down indicator qualified. Build repair is the current coherent unit. Active isolated session14616 runs explicit RepairMissing, with84retained asset/meta hashes saved in Logs/feedback-0930/retained-motion-before.json. Only missing swim/recovery sets may be copied back after the repair; no existing set may change. Do not run a regular Windows build until prerequisite tests pass. Native initial2failures are preserved: missing team-amihan set and synthetic x-curve selection. No paid service or other conversation used.

Missing-only repair succeeded. Four new Amihan/Paete swim/recovery sets and metadata copied back; all84existing set/meta hashes unchanged. Protected ability behavior and every other animation/model/effect/map remain intact. Final prerequisites/refusal checks pass2/2; actual repaired fourteen clips bind/move native model bones1/1. Current build-input unit is being shipped. Next action: create a fresh clean short-path validation checkout at the shipped commit, seed its own Library cache, run the guarded internal Windows build with explicit Builds/feedback-0930-network/TumbangPreso.exe, then direct two-process LAN using preserved isolated profiles. No job running; user Desktop build is not a target.

Owner review of the tag film: "make it look like person body actually tries to
reach not js arm extending". Diddler retains F0930-04 and owns
Runtime/Visual/CharacterAnimator.TagBody.cs, CharacterAnimator.cs restoration
ordering only, Tests/PlayMode/CatchReconstructionTests.cs, this ledger, TODO,
NETWORKING and reports/feedback-2026-09-30/tag-contact*. The next revision uses
hip weight transfer, a supporting step and shoulder turn; limb extension is
bounded close to authored proportions. Live motor positions, hit range and
scoring remain unchanged. No kit, loading, input or local build paths are owned.
Native near/far real-time films and interruption/restoration checks will judge
the change. No job running; no new animation acceptance claimed.

Build-input fix4e230aab1 shipped throughc55574cd634b7c9745bd66a2a0f7702af39118c8, remote HEAD verified; incoming visible-tag code preserved. New clean detached validation checkout: C:/Users/matth/Documents/Codex/work/tump-net-0930 atc55574cd6, with its own copied Library cache. Frozen15330Assets/Packages/ProjectSettings inputs are in Logs/feedback-0930-network/build-inputs.json. Guarded internal Windows build is LIVE: unified exec session30949, Unity PID21560, profilefeedback-0930-network-build, output Builds/feedback-0930-network/TumbangPreso.exe, log Logs/feedback-0930-network/build.log. Poll this exact handle; do not restart on timeout. Last state compiling scripts, no completed build claim. After completion verify executable/data, inspect preparation diffs and motion hashes, then run tools/run_demo_lan.py with that exact internal binary, fresh Logs output and isolated profile prefix. Desktop is untouched. No other heavy job may run alongside it. Goal remains active.

Whole-body tag revision: near/far real-time contact cases pass with body weight
transfer and close-distance adaptation; arm extension is capped10percent. Actual
capsules stay fixed, normal recovery restores root/scale, and the new film was
sent for owner review. The added missed/interrupted-lunge probe is inconclusive:
its coroutine sampled before LateUpdate, then its one bounded callback repair
received no render events outside replay. Incomplete probe removed from shipping
source, both failure receipts retained; no interruption pass claimed. No job is
running. Publish the checked runtime and update the same Feedback contact row.
Loading, hero kits and the local build/input/tutorial reservations stay untouched.

Whole-body tag5f758976 shipped through824cdfb4; remote HEAD verified and the same
Feedback row updated, Human verified untouched. The full-Eskinita follow-up was
terminated during loading (exit247, no XML) despite6.9GiB available at launch.
It provides no additional runtime evidence. No unchanged launch retry is planned.
Source stays checked by the close/far isolated native film and normal recovery.
Latest live tutorial draft explicitly says DO NOT WORK ON THIS YET; preserve that
hold. Latest input row now asks Curve Left Up/Right Down and Sprint renamed Run;
these remain with the input contributor, not the tag unit.

Diddler next F0930-20 investigation reserves only MatchRpc.cs OnScoreMsg and
OnTsinelasMsg payload-bound checks, new Tests/PlayMode/ScoreStockPacketTests.cs
and metadata, related focused evidence, TODO/NETWORKING/ledger. No wire layout,
stock rules, scoring rules, kit, input, loading or build paths are owned. Suspected
truncated host payload exceptions and trailing payload acceptance need native
reproduction before runtime edits. No test or defect claim yet; no heavy job running.

Incoming whole-body tag5f7589760 is preserved in the main integration. The active internal build stays frozen atc55574cd6; it does not qualify this later presentation revision. Do not restart the live build for an unrelated visual integration. Source/unit evidence remain separate; continue the planned actual-peer network check on the frozen binary first.

Frozen c55574cd6 player build and direct LAN complete:12scenes/2141MB/206s, both managed assemblies and exe/data verified;92motion files unchanged. Real150s client/163s host reach round2 with matching structural D34EC66E/protocol97 and replicated movement/objective state, no hard faults. Shared input unchanged; owned processes exited. Zero skills/ults exercised, so no hero-effects claim. Preparation churn remains only in tump-net-0930; later main tag/packet integration is separate. Raw inputs/logs and exact binary remain in that validation checkout. Next: existing F0930-13 routes, then a focused AI lunge/punch approach-speed correction using actual motor multipliers, plus remaining network qualification. Goal active; no heavy job running.

Score/stock envelope reproduction confirmed10native failures before the fix;
the same16cases now pass with frozen inputs. Score requires exactly12bytes and
Last Tsinelas validates its header/full declared table before mutation. Valid
payload semantics, totals, stocks and protocol97 stay unchanged. The two new
Feedback rows are recorded separately under hidden QA_TUMP_0040/0041; both await
the shipped note, Human verified untouched. No heavy cloud job running. Publish
explicit handler/test/report paths, verify remote, update those rows and release
this reservation. The local Windows build remains independently frozen.

Score/stock de3b3113 is published and remote verified; both Feedback rows now have
its shipped note. Sixteen native cases pass and all original ten failures remain
in evidence. Handler reservation released. Diddler next investigates a bot that
starts a lunge, then finds a punch target close: the Hunt sweep may release the
charge on the same frame as its punch, spending both tags. Reserve AIController.cs
StepLungeIntent only, new Tests/PlayMode/AiTagCommitmentTests.cs and metadata,
focused report and queue/ledger/NETWORKING notes. CombatVerbs is read-only for the
reproduction, not a gameplay-change target. No defect claim until native proof;
no kit, local input/tutorial/build or friend loading overlap.

Bot tag commitment is confirmed: the charged close-target case spent both punch
and lunge cooldowns; the fresh target case passed. Requiring no existing lunge
before selecting punch fixes the release-sweep interaction. Same2native controlled
planner/input cases now pass, no fixture changes. Human combat rules and protocol
unchanged. Publish explicit AI/test/report paths, verify remote and release those
paths. Feedback-row insertion was rejected against an older reservation even after
checking the owner's add-row permission; a fresh confirmation is pending in chat.
Do not bypass that Doc block. Existing score/stock rows are already updated.

Local optimization reservation: AIController.ShoveRouteIsClear and new AiObstacleQueryTests only. StepLungeIntent remains the other contributor lane; no overlap. Reuse32ray hits with complete-query fallback at capacity, preserving existing component/trigger filters. Native behavior/dense-wall/100query GC cases pending. Actual direct LAN remains qualified only on c555 frozen binary; no other heavy job running.

Allocation evidence correction: Windows GC.GetAllocatedBytesForCurrentThread also reports0 for a deliberate4096-byte array. Earlier landing-circle support-query zero result is withdrawn as performance evidence; native flight/support behavior remains valid. Bot obstacle behavior/dense-fallback cases pass. One native ProfilerRecorder GC.Alloc attempt is active (session36833 replaced by completed calibration87954, now the current recorder job); use its explicit tool handle and fresh XML, not old zero claims. Do not repeat the two unchanged behavior cases.

Bot obstacle optimization complete: two native behavior/dense-wall cases pass; calibrated native GC.Alloc sees a deliberate allocation and records0events across100warmed bot queries. Ground support/fallback also passes with the calibrated recorder. Old Windows GC.GetAllocatedBytes zero evidence is corrected, not reused. Final tests/source bytes match the frozen candidate. This unit is being shipped. No native job running. Next: F0930-13 actual title/Back/menu/queue checks, then independent remaining network/bot work. Do not overlap any newer contributor reservation.
