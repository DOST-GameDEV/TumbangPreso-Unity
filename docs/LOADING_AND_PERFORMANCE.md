# Loading And Performance: Where To Work

Target: expensive loading and reusable initialization complete behind responsive,
work-driven loading,not on the next click/cast. Respect memory and authored assets.
Read the current queue,[loading evidence](reports/stability-2026-09-27/loading-audit.md),
[working rules](WORKING_RULES.md) and current source before changing a stage.

## Current Entry Points

Runtime files are under `Assets/TumbangPreso/Runtime/`.

| Responsibility | Source |
|---|---|
| Boot stages and progress | UI/SplashScreen.cs; shader slices,rosters,audio,menu art,input,glyphs and retained dependencies |
| Real menu activation barrier | UI/SplashScreen.MenuActivation.cs and ConvertedMainMenu.IsPrepared; retain existing canvas through Wire/layout,then reveal login/input |
| Title/login art and avatars | UI/OwnerMenuArt.cs,Avatars.cs; yielded preparation and retained resources |
| Hub/HUD portraits and mode cards | UI/OwnerPortraitArt.cs; async roster-driven warmup and shared cache used by HubKit/TumpUiFactory |
| First HOME loop | UI/Hub/HubSceneVideo.Warmup.cs; async metadata/selected poster,one paused decoded frame behind boot,then adoption of the same player/target |
| Ability prop source prefabs | Visual/HeroPropAssets.cs; current Paete/Rework/Phaister folders,no gameplay spawn during asset preload |
| Supplementary baked motion | Visual/GeneratedMotionAssets.cs; yielded per-rig data preload shared by CharacterAnimator and rooted introduction lookups,no clip/graph generation |
| Effect sheets and authored intro data | Visual/VfxFlipbook.cs,UltimatePerformance and existing per-kit warmups |
| Match loading surface | UI/Hub/HubLoading.cs and MatchInstaller.IsPrepared; destination-scoped installation, failure/return and cancellation instead of timed success |
| Training controls/targets | PracticeRange.cs,UI/PausePanel.TrainingRange.cs; prebuilt inactive menu and target bodies reused without changing saved preferences |
| First-use/runtime costs | Existing profiler markers,FrameRateHistogram,tools/cold_start.py and current internal player |
| Settings value changes | UI/TumpSettingsView partials; whole-row reflow only for new sections/text size,cached chips and one unsaved-state calculation per notification |

## Rules For Changes

- Progress follows completed work. A timeout or decorative percentage is not proof
  of readiness. Scene load90% is not menu Awake/Start/layout completion.
- Yield between slices; avoid one global Shader.WarmupAllShaders or unbounded
  synchronous catalog sweep. Retain the objects consumers actually reuse.
- Loading a prefab is not instance/material/GPU preparation. Parsing a clip table
  is not rendering the first animation frame. Name the precise work moved.
- Warmup must not cast abilities,create hazards,spend resources,award progress,
  enter live services or mutate the player's profile. Preserve local fallback.
- Cache against real lifetime and invalidation. Do not make art smaller or replace
  supplied materials merely to report a faster launch.
- Measure representative player first-use/frame timings when the environment permits.
  Record source/build,hardware,path and memory tradeoffs; no hitch-free claims from
  compilation or a warm cache. Test changed stages,not the same whole boot repeatedly.

## Current Qualification Boundary

Recent menu activation,portrait and hero-prop units compile; their dedicated native
cases remain pending under the recorded editor disk-space limitation. Earlier
shader/art evidence remains separate. There is no current complete before/after
player hitch table. Consult the ledger for current environment headroom/processes,
not historical absolute paths. Do not relaunch unchanged blocked workloads in a loop.
