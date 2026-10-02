# Documentation: Start Here

HOME pinned-rules preservation: [native causal and control evidence](reports/reliability-2026-10-03/hub-pinned-rules/README.md).

Current Windows integration build and normal-lobby mode/client-save findings: [1002m evidence](reports/reliability-2026-10-02/windows-candidate1002m/README.md).

Slide prediction and retrieval inventory optimization: [native timing and behavior evidence](reports/reliability-2026-10-02/slide-inventory/README.md).

Read only the current route needed for the task. Full history is retained, but
old prompts, counts and work orders are not current instructions.

1. [AGENTS](../AGENTS.md): current working instructions.
2. [VISION](VISION.md): what the game is for and what must remain true.
3. [TODO](TODO.md#current-implementation-queue): the only current work-status queue.
4. [ACTIVE_REWORK_LEDGER](ACTIVE_REWORK_LEDGER.md): current checkpoint and evidence limits.
5. The topic below, plus the relevant [working rules](WORKING_RULES.md).

[CLAUDE.md](../CLAUDE.md) is a short compatibility entry point, not another rulebook.
[REFERENCE_INDEX](REFERENCE_INDEX.md) lists every root document and its status.
[Archive](archive/README.md) maps historical documents to their current replacements.

Current owner-priority research/design: [full hero quality brief](reports/hero-quality-2026-10-01/brief.md).

## Pick The Task

| Task | Read first | Then inspect |
|---|---|---|
| Network bugs, ranked, reconnect, spectator, client-only skills | [Networking](NETWORKING.md), [skill contract](SKILL_NETWORK_CONTRACT.md) | MatchRpc/NetSession/Matchmaker and [current evidence](reports/stability-2026-09-27/multiplayer.md) |
| Loading, first-click/cast hitches, optimization | [Loading/performance](LOADING_AND_PERFORMANCE.md) | SplashScreen, retained caches and [loading evidence](reports/stability-2026-09-27/loading-audit.md) |
| Main menu/HOME/season/showcase animation | [HOME animation method](HOME_SCREEN_ANIMATION_METHOD.md) | [Zack](reports/home-scene/README.md), [Phaister](reports/home-scene/phaister.md), ArtSource/home-scene and HubSceneVideo |
| Character building/remodel | [Character method](CHARACTER_MODEL_METHOD.md), [art direction](Art_Direction.md), [clothing constraints](CAST_CLOTHING_STYLE.md) | [Voxel guide](Voxel_Person_Guide.md), that hero's builder/ArtSource, [rig notes](../ASTRA.md) |
| Character motion, skills, VFX, sound, ultimate cutscene | [Hero-kit method](HERO_KIT_METHOD.md), [network contract](SKILL_NETWORK_CONTRACT.md) | [Kit decisions](HERO_KIT_REWORK_DECISIONS.md), current ability source and hero-specific reports |
| Native rendering, lineup, motion samples, player build | [Canonical pipeline](CANONICAL_RENDERING_PIPELINE.md), [testing](TESTING.md) | Existing probes and exact source/asset candidate; preserve authored materials |
| Login, menu, HUD, navigation, accessibility | [UI method](UI_DESIGN_METHOD.md), [authorship](OWNER_UI_AUTHORING.md), [fonts](FONT_USAGE.md) | Current UI owner/helpers, [UX-1 routes](reports/front-end-flow-2026-09-23/ux1-plan.md), [surface inventory](reports/front-end-flow-2026-09-23/ui-authorship-inventory.md) |
| Rules, scoring, retrieval, formats, bots | [VISION](VISION.md), [Design](Design.md), [Formats](Formats.md), working rules | Core package, MatchDirector, RoundDirector, CharacterMotor and AIController |
| Setup, tests, profiles, target platforms | [Workstation setup](WORKSTATION_SETUP.md), [testing](TESTING.md) | ProjectVersion, Packages, guarded runners and current ledger |
| Environment/map reference | [Reference index](REFERENCE_INDEX.md#environment-references) | Current contributor ownership, actual source and dated evidence before interpreting old plans |
| Kanto or Lagoon Cove (Blender-built maps) | [Kanto guide](KANTO_DESIGN_GUIDE.md), [Lagoon rework guide](LAGOON_REWORK_GUIDE.md) (its CURRENT STATE block first) | The authoring tools they name (tools/author_kanto_*.py, tools/author_lagoon_cove.py and the lagoon kits), Editor/MapKit builders, TODO KANTO-1 and LAGOON-1 |
| Ilalim ng Tulay rebuild (the next Blender map) | [Ilalim rework guide](ILALIM_REWORK_GUIDE.md) (CURRENT STATE first), [map design](Ilalim_Ng_Tulay.md) | IlalimNgTulayBuilder and the Ilalim*Author passes, LrtTrainFlyby, BridgeHoop, [map-integration issues](reports/map-integration-2026-09-28/issues.md), TODO ILALIM-1 |
| Lore, story, human voice/reference | [Origins](CHARACTER_ORIGINS.md), [LORE](../LORE.md), [HUMAN](HUMAN.md) | Current hero records and human-recording requirements |
| Historical decision or retired implementation | [Archive index](archive/README.md) | Original document/section and the linked current replacement; do not execute old prompts |

## Where Information Belongs

- **Instructions:** AGENTS and WORKING_RULES. No duplicated phase schedules.
- **Status:** TODO; open numbered detail in TODO_Backlog, finished bodies in TODO_Archive.
  Preserve IDs and history. A report is not a new open task.
- **Checkpoint:** a concise ledger entry with revision, changes, jobs, limits and next action.
  Move accumulated old entries to dated history instead of making every session read them.
- **Methods:** the live topic method, including pitfalls and source ownership.
- **Evidence:** dated reports, exact revisions/receipts and clearly labelled limits.
  Do not rewrite old results to imply qualification of newer source.
  [September 30 feedback fixes](reports/feedback-2026-09-30/README.md) records the
  current Tasks/round-standings batch; its status lives in TODO FEEDBACK-0930.
  [Cheska expiry presentation](reports/cheska-expiry-2026-09-30/plan.md) records
  the scoped field-melt/wall-shatter plan and current reference limits.
  [Dante barrier visibility](reports/dante-visibility-2026-09-30/result.md) records
  the palette-aware half-alpha fix and native capture limits.
  [Dante active ward cue](reports/dante-ward-2026-09-30/result.md) records
  the clock-bound shield badge and native camera/lifecycle check.
- **History:** archive, with original-path pointers and a replacement/reason in its index.
  Old documents keep their useful context; reusable methods remain live.
- **Media:** latest two generated iterations per coherent subject/action/view when pruning
  authorized old captures. Keep shipped/source/supplied and essential method/decision assets.
  Log removals so old evidence references can be traced through Git.

## Avoid Stale Instructions

A dated OPEN label is not proof the feature is absent. Compare source, current
owner direction and newer evidence. The systems roadmap, old port phases and old
exclusive contributor lanes are historical. Broad tests after every edit, raw Unity
launches, Desktop replacement and UI version stamps are not the current workflow.

The pre-cleanup index/rules/ledger are preserved in
[snapshots-2026-09-27](archive/snapshots-2026-09-27/README.md). Documents originating
from Godot now live here as Unity-owned references; never overwrite them from the
frozen Godot repository.

Bank Shot mechanics and validation: [current report](reports/feedback-2026-09-30/bank-shot.md).

Rebuilt Ilalim generated-scenery preview isolation: [native evidence](reports/reliability-2026-10-02/ilalim-preview-scope/README.md).

Rebuilt Ilalim under-viaduct preview framing: [native evidence](reports/reliability-2026-10-02/ilalim-preview-framing/README.md).

Replay trail capture inventory reuse: [native evidence](reports/reliability-2026-10-02/replay-shoe-lookup/README.md).

Zack bot Overclock decision repair: [native evidence](reports/reliability-2026-10-02/bot-overclock/README.md).

Current129 Windows rebuilt-Ilalim player observations: [evidence](reports/reliability-2026-10-02/ilalim-player129/README.md).

Rebuilt Ilalim late preview-audio playback fix: [native evidence](reports/reliability-2026-10-02/ilalim-preview-audio/README.md).

Empty replay sample allocation removal: [native evidence](reports/reliability-2026-10-02/replay-empty-capture/README.md).

Queue connection startup exception recovery: [native evidence](reports/reliability-2026-10-02/queue-start-fault/README.md).

Career submission acknowledgement and saved-result retention: [native evidence](reports/reliability-2026-10-02/career-submit-ack/README.md).

Shared Cloud Code missing-output failure boundary: [native evidence](reports/reliability-2026-10-02/cloud-output/README.md).

Bounded shared service-request timeout: [native evidence](reports/reliability-2026-10-02/http-timeout/README.md).

Late career submission identity and pending-result preservation: [native evidence](reports/reliability-2026-10-02/career-submit-identity/README.md).

Refreshed129 Windows Classic tournament-context observations: [evidence](reports/reliability-2026-10-02/current-tournament129/README.md).

Career refresh account-cache ownership: [native evidence](reports/reliability-2026-10-02/career-refresh-owner/README.md).

Abandon/history account-response ownership: [native evidence](reports/reliability-2026-10-02/career-async-owner/README.md).

Queued-result verification alignment after refusals: [native evidence](reports/reliability-2026-10-02/career-witness/README.md).

Late history UI completion and navigation safety: [native evidence](reports/reliability-2026-10-02/history-navigation/README.md).

Automatic career sync after pending older-account work: [native evidence](reports/reliability-2026-10-02/career-account-sync/README.md).
All-nine-hero official footage research and implementation plans: [October 2 review](reports/hero-reference-footage-2026-10-02/README.md).

Displayed match and career-dispute identity: [native evidence](reports/reliability-2026-10-02/result-verdict/README.md).

Late career acknowledgements refresh visible result details: [native evidence](reports/reliability-2026-10-02/result-late-ack/README.md).

Wallet account ownership and deferred refresh: [native evidence](reports/reliability-2026-10-02/wallet-owner/README.md).

Full current Windows Classic tournament-context match: [actual peer evidence](reports/reliability-2026-10-02/full-classic-match/README.md).

Inactive bot-body observation isolation: [native evidence](reports/reliability-2026-10-02/bot-inactive-body/README.md).

Social reply and handle lookup account ownership: [native evidence](reports/reliability-2026-10-02/social-owner/README.md).

Sean held-shoe body preparation: [evidence](reports/sean-empowered-presentation-2026-10-02/README.md).

Amihan presentation: Airburst and cutscene, Drift, her light body and floating run: [report](reports/amihan-presentation-2026-10-02/README.md).

Cheska held Frostbite surface cue and low-memory qualification: [October 2 result](reports/cheska-frostbite-load-2026-10-02/result.md).

Distinct Cheska held-shoe cast: [native review and preservation proof](reports/cheska-frostbite-motion-2026-10-02/README.md).

Dante Boulder weight-bearing preparation: [native review and preserved assets](reports/dante-boulder-motion-2026-10-02/README.md).

- [Boulder loaded-shoe surface and lifecycle](reports/dante-boulder-load-2026-10-02/README.md): affinity-bound world/owner inlay, retained baseline and serial checks.
