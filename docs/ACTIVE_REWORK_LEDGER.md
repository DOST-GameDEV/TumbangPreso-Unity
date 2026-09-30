# Active Rework Checkpoint

Updated 2026-09-30. Branch: ASTRAReworks. Current unit: FEEDBACK-0930.
Current protocol:96 in [NetSession](../Assets/TumbangPreso/Runtime/Net/NetSession.cs).
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

Current Diddler reservation: owner-prioritized offline Esc/menu pause. Opening
an offline match menu must stop simulation and closing it must resume safely,
including repeated opens and nested settings; online menus stay live.
Owned paths:
- Assets/TumbangPreso/Runtime/UI/PausePanel.cs
- Assets/TumbangPreso/Runtime/UI/PausePanel.LiveMenu.cs (truthful pause notice only)
- Assets/TumbangPreso/Runtime/Hitstop.cs (do not unpause a stopped clock)
- Assets/TumbangPreso/Tests/PlayMode/OfflineMenuPauseTests.cs and its .meta
- docs/UI_DESIGN_METHOD.md, docs/TODO.md, docs/ACTIVE_REWORK_LEDGER.md
- docs/reports/feedback-2026-09-30/offline-menu-pause.md

No code has been edited yet. Current code deliberately leaves every menu live;
verify the offline lifecycle before applying the owner's revised behavior.
Continuous-motion caught replay remains the next independent request, unclaimed.

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
