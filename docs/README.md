# Documentation routes

Read [AGENTS](../AGENTS.md), [project context](UnityProjectContext.md),
[TODO](TODO.md) and the short [checkpoint](ACTIVE_REWORK_LEDGER.md).
Do not read every report or historical queue at session start.

| Work | Current guide | Source to inspect |
|---|---|---|
| Rules, scoring, formats, bots | [Vision](VISION.md), [Design](Design.md), [Formats](Formats.md) | Core package, MatchDirector, RoundDirector, AIController |
| Spectator, follow/POV, controls | [Spectator](SPECTATOR.md), [UI method](UI_DESIGN_METHOD.md) | SpectatorCamera, SpectatorDirector, SpectatorInterest, TumpMatchReadout, CameraRig |
| Network, reconnect, remote skills | [Networking](NETWORKING.md), [skill contract](SKILL_NETWORK_CONTRACT.md) | NetSession, LobbySession, MatchRpc partials |
| Tests, build, new machine | [Testing](TESTING.md), [setup](WORKSTATION_SETUP.md) | ProjectVersion, guard, test fixtures, GameBuilder |
| Loading and performance | [Loading](LOADING_AND_PERFORMANCE.md) | Current boot/cache owners; profile before changing |
| UI, input, typography | [UI method](UI_DESIGN_METHOD.md), [owner artwork](OWNER_UI_AUTHORING.md), [fonts](FONT_USAGE.md) | Current screen/theme, InputCatalogue, ScreenInputCatalogue |
| Character art and motion | [Character method](CHARACTER_MODEL_METHOD.md), [art direction](Art_Direction.md), [clothing](CAST_CLOTHING_STYLE.md) | Per-character source, current rig, authored clips |
| Skills, VFX, SFX, ultimates | [Hero method](HERO_KIT_METHOD.md), [VFX method](VFX_BENCH_METHOD.md), network contract | HeroAbilitySystem, kit, shared presentation; `.claude/skills/tump-sfx/SKILL.md` for audio |
| Map films and previews | [Films](MAP_FILM_METHOD.md), [recording](MAP_PREVIEW_RECORDING.md) | MapFilm, MapPreviewVideo, source receipts |
| Trailer | [Silent trailer delivery](trailer/DELIVERY.md), [accepted brief](trailer/PRODUCTION_BRIEF.md), [checkpoint](trailer/PRODUCTION_CHECKPOINT.md) | TrailerOpeningFilm, TrailerStreetPayoffFilm, TrailerGameCapture, run_trailer_job.py |
| Maps and source art | [Reference index](REFERENCE_INDEX.md) | Current scene, builder, authored sources |
| Recorded matches | [Local replays](LOCAL_REPLAYS.md) | RecordedMatchClip, RecordedWorldView, replay codecs |
| Native rendering | [Rendering pipeline](CANONICAL_RENDERING_PIPELINE.md) | Existing probe and actual graphics output |

## Document roles

- **Live:** short entry points, rules and methods. Update their owning document when behavior changes.
- **Reference:** art/source/design details, opened only for that topic. Source wins over stale numbers.
- **Evidence:** `reports/`; conclusions apply only to the dated tested commit and scenario.
- **History:** `archive/`, retired pointers and old queue bodies; no automatic assignments.
- **Vendor:** embedded package docs/licenses; preserve their upstream meaning.

[Reference index](REFERENCE_INDEX.md) lists topic references.
[Retirement decisions](RETIRED_WORK.md) explain completed or superseded plans.
[Cleanup audit](reports/repository-cleanup-2026-10-25.md) records coverage and checks.
Use `python tools/audit_docs.py --list` to locate Markdown by role without loading it all.
