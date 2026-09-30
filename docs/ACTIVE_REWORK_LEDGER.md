# Active Rework Checkpoint

Updated 2026-09-30. Branch: ASTRAReworks. Current unit: FEEDBACK-0930.
Current protocol:94 in [NetSession](../Assets/TumbangPreso/Runtime/Net/NetSession.cs).
Read [AGENTS](../AGENTS.md), [task routes](README.md) and the [queue](TODO.md).
TODO is the work-status queue; this file records resumption and evidence boundaries.


## Shared Ownership

Diddler cloud unit complete: Nemu's 10 percent basic-cooldown movement bonus
shipped in 770162fed59f241de4e07d8e1d8599a861ff4639. Native before/after checks
reproduced 7 failures, then passed 8/8 after the correction, with frozen inputs.
No actual-peer or rendered-player qualification is claimed. The broader Wiki
Feedback row remains open; its Doc tabs are still reserved locally.

Shared 25-second basic cooldowns shipped in 6b4e1d4ffa02d3976d57efee55b932d97d9c8d2c.
Four native cases reproduced separate 40/30/30 clocks; the final 18-case fixture
passes, including accepted-cast callbacks, latest refusals and reordered receipts.
Actual peers remain unqualified. HeroKit and SkillReceipts edits for that unit are
finished and released.

Next Diddler cloud reservation: implement the Wiki Kuro: Sit signature, keeping
its stable ability ID, a 10-second stationary recall anchor, existing authored
presentation and shared cooldown. Use the existing prepared-world recovery route
and qualify recall, expiry, cancellation and restored state. Raise compatibility
for the changed gameplay meaning; no Phaister or Paete changes.

Owned edit paths:
- Assets/TumbangPreso/Runtime/Abilities/NemuHeroKit.cs
- Packages/com.tumbangpreso.core/Runtime/RosterReworkRules.cs (NecroRules Sit constants only)
- Assets/TumbangPreso/Runtime/Net/NetSession.cs (protocol compatibility constant only)
- Assets/TumbangPreso/Tests/PlayMode/NemuSitContractTests.cs
- Assets/TumbangPreso/Tests/PlayMode/NemuSitContractTests.cs.meta
- docs/SKILL_NETWORK_CONTRACT.md (Kuro Sit recovery contract only)
- docs/reports/feedback-2026-09-30/nemu-kuro-sit.md
- docs/TODO.md (only this unit's evidence pointer)
- docs/ACTIVE_REWORK_LEDGER.md (only this reservation and its result)

Local contributor reservation supplied by owner: GuidedTraining.cs,
GuidedTrainingHud.OwnerPainted.cs, OwnerTrainingUiTests.cs,
TutorialLessonHonestyProbe.cs, TutorialDefenderProbe.cs and DeadFeatureAudit.cs.
The local contributor also owns final review of Feedback and How to use Feedback
in the shared Wiki. Do not edit those paths or tabs until released. Preserve this
reservation when updating the checkpoint. No cross-chat contact is required.

## Current Unit

### 2026-09-30 continuation

Owner's newest assignment is [FEEDBACK-0930](TODO.md#feedback-0930-owner-document-and-engineering-follow-through),
followed by reasonable engineering/UI/UX fixes and applicable existing TODO work.
Latest restrictions: no subagents, resets, cross-chat work or credit spending without
approval; VFX/SFX/animation/models/maps/lighting only for demonstrated bug fixes.
Phaister/Paete abilities are protected, descriptions only. The wiki's other complete
ability/status definitions supersede old docs; blank cells are unspecified.

Working checkout: C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks.
Fetched and fast forwarded 2bacb97c3 to aecc0ee2c before editing. Existing unrelated
RosterArms assets, two protected UI metadata files, HeroHazards.cs, QualitySettings,
motion captures and untracked art/skill-FX work are preserved and not task-owned.

First unit complete: Tasks retains unchanged rows through wallet refresh; ordinary
round cards hide the live HUD, center standings, fit score values and remove the
duplicate warmup line. Two focused native cases pass, with all eight frozen source
inputs unchanged. Inspected Tasks and 960x540/1600x680 standings captures are in
[the feedback report](reports/feedback-2026-09-30/README.md). This batch is being
committed/pushed with explicit owned paths; do not rerun its unchanged cases.

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
reports, screenshots, comments and the other tabs. Explicit status/evidence must
remain readable to automation. DOTS setup prompt was given in chat for manual use;
no automation/cross-chat action occurred. Native Doc checklist state may not be
available through the connector. Xelu's prompt source declares CC0.

Next: finish Feedback-tab organization alongside tutorial assessment/overlap and
binding reconciliation. Native first unit passed; no repeat validation is needed.

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
