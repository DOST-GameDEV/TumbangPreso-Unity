# Repository Entry Point

Read [AGENTS.md](AGENTS.md), [VISION](docs/VISION.md), the
[current TODO queue](docs/TODO.md#current-implementation-queue), then
[ACTIVE_REWORK_LEDGER](docs/ACTIVE_REWORK_LEDGER.md). Choose the topic in
[docs/README](docs/README.md). These are the current rules and routes.

This file is intentionally a short compatibility entry point. Standing contracts
live once in [WORKING_RULES](docs/WORKING_RULES.md), not in two competing instruction
files. New owner instructions win; old prompts and dated schedules do not assign work.

## Important Routes

| Work | Read first |
|---|---|
| Multiplayer, ranked, skills, remote presentation | [Network contract](docs/SKILL_NETWORK_CONTRACT.md), [network guide](docs/NETWORKING.md) |
| Character model/rework | [Character method](docs/CHARACTER_MODEL_METHOD.md), [clothing constraints](docs/CAST_CLOTHING_STYLE.md), [voxel guide](docs/Voxel_Person_Guide.md) |
| Skill, animation, VFX, audio, cutscene | [Hero-kit method](docs/HERO_KIT_METHOD.md), then network contract |
| Main menu/HOME/showcase animation | [Home animation method](docs/HOME_SCREEN_ANIMATION_METHOD.md), [HOME evidence](docs/reports/home-scene/README.md) |
| UI, login, HUD, navigation | [UI method](docs/UI_DESIGN_METHOD.md), [UI authoring](docs/OWNER_UI_AUTHORING.md), [font roles](docs/FONT_USAGE.md) |
| Rendering, character proof, builds | [Canonical pipeline](docs/CANONICAL_RENDERING_PIPELINE.md), [testing](docs/TESTING.md) |
| Loading/performance | [Loading guide](docs/LOADING_AND_PERFORMANCE.md) |
| New machine | [Workstation setup](docs/WORKSTATION_SETUP.md) |

## Legacy Section Pointers

Older code comments refer to numbered CLAUDE.md sections. Their rules were not
discarded; the complete wording and incident receipts remain in the
[2026-09-27 snapshot](docs/archive/snapshots-2026-09-27/CLAUDE.md) and
[2026-09-24 full history](docs/archive/CLAUDE_full_2026-09-24.md).

| Old section | Current authority |
|---|---|
| 0: routing and owner notes | AGENTS, TODO and the current task's instructions |
| 1: what the repo is | VISION and WORKING_RULES: Gameplay And Authority |
| 2: session, TODO, handoff | AGENTS: Autonomy, Shared Workspace, Documentation |
| 3: writing/commits | AGENTS: Shared Workspace And Publication |
| 4: architecture | WORKING_RULES: Gameplay And Authority |
| 4a: three devices | WORKING_RULES: Input And Access |
| 5: design/port ledger | Current code/design reconciliation; port records are historical parity evidence |
| 6,6.0,6.1: art/model iteration | Character/hero-kit methods and canonical rendering pipeline |
| 6.2,6.2a,6.2b,6.2c,6.3: screens/journeys | UI_DESIGN_METHOD and WORKING_RULES: UI And Journeys |
| 6.4,6.5: palette/surfaces | Current supplied artwork, theme source, OWNER_UI_AUTHORING and font roles |
| 7,7.1: machines/verification | TESTING and WORKSTATION_SETUP |

Superseded instructions to run broad tests after every change, write the Desktop
build, display UI versions, or revive exclusive old contributor lanes are NOT current.
Use focused checks and internal builds, preserve current authorship/reservations,
and keep working through checkpoints without repeating unchanged evidence.
